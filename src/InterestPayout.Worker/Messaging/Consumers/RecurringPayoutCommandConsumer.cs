using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using AzureStorage;
using InterestPayout.Common.Application;
using InterestPayout.Common.Domain;
using InterestPayout.Common.Persistence;
using InterestPayout.Common.Persistence.ExternalEntities.Balances;
using InterestPayout.Common.Persistence.ExternalEntities.Clients;
using InterestPayout.Common.Persistence.ExternalEntities.Wallets;
using InterestPayout.Common.Utils;
using InterestPayout.Worker.ExternalResponseModels.Assets;
using Lykke.Cqrs;
using Lykke.InterestPayout.MessagingContract;
using Lykke.MatchingEngine.Connector.Models.Api;
using Lykke.MatchingEngine.Connector.Services;
using Lykke.Service.Assets.Client;
using MassTransit;
using MassTransit.Util;
using Microsoft.Extensions.Logging;
using Swisschain.Extensions.Idempotency;

namespace InterestPayout.Worker.Messaging.Consumers
{
    public class RecurringPayoutCommandConsumer : IConsumer<RecurringPayoutCommand>
    {
        private static readonly Guid MatchingEngineFixedNamespace = new Guid("27b5f4ff-13e9-493d-b763-8519c1bbfe47");

        private readonly ILogger<RecurringPayoutCommandConsumer> _logger;
        private readonly IWalletRepository _walletRepository;
        private readonly INoSQLTableStorage<ClientAccountEntity> _clientStorage;
        private readonly IBalanceRepository _balanceRepository;
        private readonly TcpMatchingEngineClient _matchingEngineClient;
        private readonly IUnitOfWorkManager<UnitOfWork> _unitOfWorkManager;
        private readonly IAssetsService _assetsService;
        private readonly ICqrsEngine _cqrsEngine;

        public RecurringPayoutCommandConsumer(ILogger<RecurringPayoutCommandConsumer> logger,
            IWalletRepository walletRepository,
            INoSQLTableStorage<ClientAccountEntity> clientStorage,
            IBalanceRepository balanceRepository,
            TcpMatchingEngineClient matchingEngineClient,
            IUnitOfWorkManager<UnitOfWork> unitOfWorkManager,
            IAssetsService assetsService,
            ICqrsEngine cqrsEngine)
        {
            _logger = logger;
            _walletRepository = walletRepository;
            _clientStorage = clientStorage;
            _balanceRepository = balanceRepository;
            _matchingEngineClient = matchingEngineClient;
            _unitOfWorkManager = unitOfWorkManager;
            _assetsService = assetsService;
            _cqrsEngine = cqrsEngine;
        }

        public async Task Consume(ConsumeContext<RecurringPayoutCommand> context)
        {
            
            var scheduledDateTime = context.Headers.Get<DateTimeOffset>("MT-Quartz-Scheduled");
            if (!scheduledDateTime.HasValue)
                throw new InvalidOperationException("Cannot obtain original scheduled time from the header 'MT-Quartz-Scheduled'");
            
            if (await IsMessageExpired(scheduledDateTime.Value, context.Message))
                return;
            
            var assetServiceResponse = await _assetsService.GetAssetWithHttpMessagesAsync(context.Message.PayoutAssetId);
            if (!assetServiceResponse.Response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    $"Cannot get asset information from asset service for asset '{context.Message.PayoutAssetId}': {assetServiceResponse.Response.StatusCode}:{assetServiceResponse.Response.ReasonPhrase}");
            }
            var assetInfo = await assetServiceResponse.Response.Content.ReadFromJsonAsync<AssetInfo>();
            _logger.LogInformation("Obtained asset information from asset service {@context}", new
            {
                command = context.Message,
                asset = assetInfo,
            });
            
            var partitionKey = ClientAccountEntity.GeneratePartitionKey();
            await _clientStorage.GetDataByChunksAsync(partitionKey, entities =>
            {
                var clients = entities.ToList();

                _logger.LogInformation("Starting to process chunk of clients (" + clients.Count + ") {@context}", new
                {
                    command = context.Message,
                    context.Message.AssetId
                });
                foreach (var client in clients)
                {
                    TaskUtil.Await(ProcessAllWalletsByClient(client.Id,
                        context.Message,
                        scheduledDateTime.Value.ToString("O"),
                        assetInfo.Accuracy));
                }
                _logger.LogInformation("Processed chunk of clients (" + clients.Count + ")");
            });
            _logger.LogInformation("Finished processing all chunks {@context}", new
            {
                context.Message
            });
        }

        private async Task ProcessAllWalletsByClient(string clientId,
            RecurringPayoutCommand command,
            string scheduledTimeStamp,
            int assetAccuracy)
        {
            _logger.LogInformation("Starting processing recurring payout for asset for client {@context}", new
            {
                ClientId = clientId,
                command,
                scheduledTimeStamp,
            });

            var wallets = await _walletRepository.GetAllByClient(clientId);
            
            _logger.LogDebug($"Found {wallets.Count} wallets for client '{clientId}'.");
            if (!wallets.Any())
                return;
            
            var walletIds = wallets.Select(x => x.Id).ToArray();
            var balances = await _balanceRepository.GetBalances(clientId, walletIds, command.AssetId);
            if (!balances.Any())
            {
                _logger.LogDebug($"Not found any non-zero balances for client '{clientId}' and assetId '{command.AssetId}'.");
                return;
            }

            var creditedAmounts = new List<double>();
            foreach (var balance in balances)
            {
                await ProcessWallet(clientId,
                    balance,
                    command,
                    scheduledTimeStamp,
                    assetAccuracy,
                    creditedAmounts);
            }

            var totalCreditedAmount = creditedAmounts.Sum();
            var numberOfCashInOutOperations = creditedAmounts.Count;
            _logger.LogInformation("Finished processing recurring payout for client {@context}", new
            {
                Command = command,
                ClientId = clientId,
                TotalCreditedAmount = totalCreditedAmount,
                NumberOfCashInOutOperations = numberOfCashInOutOperations,
            });
        }

        private async Task ProcessWallet(string clientId,
            ClientBalance balance,
            RecurringPayoutCommand command,
            string scheduledTimestamp,
            int assetAccuracy,
            List<double> creditedAmounts)
        {
            var idempotencyId = $"{command.AssetId}:{scheduledTimestamp}:{balance.WalletId}";
            // ME can only accept "regular" guids as operationID (idempotency ID),
            // so we create deterministic GUID, based on longer natural idempotency ID
            var operationId = NamespaceGuid.Create(MatchingEngineFixedNamespace, idempotencyId, version: 5);
            var amount = InterestCalculator.CalculateInterest(balance.Balance, command.InterestRate, assetAccuracy);

            _logger.LogInformation("Attempting to perform payout {@context}", new
            {
                IdempotencyId = idempotencyId,
                OperationId = operationId,
                ClientId = clientId,
                WalletId = balance.WalletId,
                command,
                balance.WalletType,
                balance.Balance,
                Amount = amount,
                AssetAccuracy = assetAccuracy
            });

            if (amount == 0)
            {
                _logger.LogInformation("Target payout amount rounded down to zero {@context}", new
                {
                    IdempotencyId = idempotencyId,
                    OperationId = operationId,
                    Amount = amount,
                    command
                });
                return;
            }
            
            await using var unitOfWork = await _unitOfWorkManager.Begin(idempotencyId);
            if (!unitOfWork.Outbox.IsClosed)
            {
                MeResponseModel matchingEngineResponse;
                try
                {
                     matchingEngineResponse = await _matchingEngineClient.CashInOutAsync(operationId.ToString(),
                        clientId,
                        command.PayoutAssetId,
                        amount,
                        new CancellationTokenSource(TimeSpan.FromMinutes(2)).Token);
                }
                catch (NullReferenceException nre)
                {
                    throw new InvalidOperationException("Network-related problem occurred while connecting to the Matching Engine.", nre);
                }

                if (matchingEngineResponse.Status == MeStatusCodes.Ok)
                {
                    _logger.LogInformation("Successfully issued cashin/cashout request to ME {@context}", new
                    {
                        IdempotencyId = idempotencyId,
                        OperationId = operationId,
                        matchingEngineResponse.TransactionId,
                        matchingEngineResponse.Message,
                        matchingEngineResponse.Status,
                        Amount = amount,
                        command
                    });
                    await unitOfWork.Commit();
                    _cqrsEngine.PublishEvent(
                        new PayoutCompletedEvent
                        {
                            OperationId = operationId.ToString(),
                            PayoutAssetId = command.PayoutAssetId,
                            AssetId = command.AssetId,
                            ClientId = clientId,
                            WalletId = balance.WalletId,
                            Amount = Convert.ToDecimal(amount)
                        },
                        InterestPayoutBoundedContext.Name);
                    creditedAmounts.Add(amount);
                }
                else if (matchingEngineResponse.Status == MeStatusCodes.Duplicate)
                {
                    _logger.LogError("Unexpected duplication of requests to ME {@context}", new
                    {
                        IdempotencyId = idempotencyId,
                        OperationId = operationId,
                        matchingEngineResponse.TransactionId,
                        matchingEngineResponse.Message,
                        matchingEngineResponse.Status,
                        Amount = amount,
                        command
                    });
                }
                else if (matchingEngineResponse.Status == MeStatusCodes.InvalidVolumeAccuracy)
                {
                    _logger.LogError("Interest payout failed because of invalid interest or accuracy settings {@context}", new
                    {
                        IdempotencyId = idempotencyId,
                        OperationId = operationId,
                        matchingEngineResponse.TransactionId,
                        matchingEngineResponse.Message,
                        matchingEngineResponse.Status,
                        Amount = amount,
                        command
                    });
                    throw new InvalidOperationException(
                        $"Unexpected duplication of requests to ME. IdempotencyId: '{idempotencyId}', TransactionId: '{matchingEngineResponse.TransactionId}'");
                }
                else
                {
                    _logger.LogError("Unexpected response from ME {@context}", new
                    {
                        IdempotencyId = idempotencyId,
                        OperationId = operationId,
                        matchingEngineResponse.TransactionId,
                        matchingEngineResponse.Message,
                        matchingEngineResponse.Status,
                        Amount = amount,
                        command
                    });
                    throw new InvalidOperationException(
                        $"Unexpected response from ME. IdempotencyId: '{idempotencyId}', TransactionId: '{matchingEngineResponse.TransactionId}'");
                }
            }
        }

        private async Task<bool> IsMessageExpired(DateTimeOffset originalScheduledTimestamp, RecurringPayoutCommand message)
        {
            // if scheduled time is more than one full interval earlier than current timestamp, then it is too old, skipping
            var currentTimeStamp = DateTimeOffset.UtcNow;
            var latestAcceptableExecutionTimeStamp = originalScheduledTimestamp + message.CronScheduleInterval;
            
            if (latestAcceptableExecutionTimeStamp < currentTimeStamp)
            {
                _logger.LogInformation("recurring message is expired will be skipped {@context}", new
                {
                    originalScheduledTimestamp,
                    currentTimeStamp,
                    latestAcceptableExecutionTimeStamp,
                    CronScheduleIntervalInSeconds = message.CronScheduleInterval.TotalSeconds,
                    message
                });
                return true;
            }
            
            await using var unitOfWork = await _unitOfWorkManager.Begin();

            var schedule = await unitOfWork.PayoutSchedules.GetByIdOrDefault(message.InternalScheduleId);
            
            // schedule is deleted
            if (schedule == null)
            {
                _logger.LogInformation("internal schedule corresponding to the recurring message no longer exists, message will be skipped {@context}",
                    new
                    {
                        originalScheduledTimestamp,
                        currentTimeStamp,
                        latestAcceptableExecutionTimeStamp,
                        CronScheduleIntervalInSeconds = message.CronScheduleInterval.TotalSeconds,
                        message
                    });
                return true;
            }

            // schedule has been modified after current message was scheduled, but before it was consumed
            if (schedule.Sequence != message.InternalScheduleSequence)
            {
                _logger.LogInformation(
                    "internal schedule corresponding to the recurring message was modified after scheduling, message will be skipped {@context}",
                    new
                    {
                        originalScheduledTimestamp,
                        currentTimeStamp,
                        latestAcceptableExecutionTimeStamp,
                        CronScheduleIntervalInSeconds = message.CronScheduleInterval.TotalSeconds,
                        message
                    });
                return true;
            }

            return false;
        }
    }
}
