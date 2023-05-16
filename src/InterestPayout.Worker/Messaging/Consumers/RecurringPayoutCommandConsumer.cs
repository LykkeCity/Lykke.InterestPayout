using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using InterestPayout.Common.Application;
using InterestPayout.Common.Domain;
using InterestPayout.Common.Persistence;
using InterestPayout.Common.Utils;
using InterestPayout.Worker.ExternalResponseModels.Assets;
using Lykke.Cqrs;
using Lykke.InterestPayout.MessagingContract;
using Lykke.MatchingEngine.Connector.Models.Api;
using Lykke.MatchingEngine.Connector.Services;
using Lykke.Service.Assets.Client;
using Lykke.Service.Balances.AutorestClient.Models;
using Lykke.Service.Balances.Client;
using Lykke.Service.ClientAccount.Client;
using Lykke.Service.ClientAccount.Client.Models.Response.Wallets;
using MassTransit;
using Microsoft.Extensions.Logging;
using Swisschain.Extensions.Idempotency;
using WalletType = Lykke.Service.ClientAccount.Client.Models.WalletType;

namespace InterestPayout.Worker.Messaging.Consumers
{
    public class RecurringPayoutCommandConsumer : IConsumer<RecurringPayoutCommand>
    {
        private static readonly Guid MatchingEngineFixedNamespace = new Guid("27b5f4ff-13e9-493d-b763-8519c1bbfe47");

        private readonly ILogger<RecurringPayoutCommandConsumer> _logger;
        private readonly TcpMatchingEngineClient _matchingEngineClient;
        private readonly IUnitOfWorkManager<UnitOfWork> _unitOfWorkManager;
        private readonly IAssetsService _assetsService;
        private readonly ICqrsEngine _cqrsEngine;
        private readonly IClientAccountClient _clientAccountClient;
        private readonly IBalancesClient _balancesClient;

        public RecurringPayoutCommandConsumer(ILogger<RecurringPayoutCommandConsumer> logger,
            TcpMatchingEngineClient matchingEngineClient,
            IUnitOfWorkManager<UnitOfWork> unitOfWorkManager,
            IAssetsService assetsService,
            ICqrsEngine cqrsEngine,
            IClientAccountClient clientAccountClient,
            IBalancesClient balancesClient)
        {
            _logger = logger;
            _matchingEngineClient = matchingEngineClient;
            _unitOfWorkManager = unitOfWorkManager;
            _assetsService = assetsService;
            _cqrsEngine = cqrsEngine;
            _clientAccountClient = clientAccountClient;
            _balancesClient = balancesClient;
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
            
            string continuationToken = null;
            var chunkIndex = 0;
            do
            {
                var ids = await _clientAccountClient.Clients.GetIdsAsync(continuationToken);
                continuationToken = ids?.ContinuationToken;
                if (ids == null)
                    break;

                var clients = ids.Ids.ToList();
                
                _logger.LogInformation("Starting to process chunk with index" + chunkIndex + " of clients (" + clients.Count + ") {@context}", new
                {
                    command = context.Message,
                    context.Message.AssetId
                });
                foreach (var client in clients)
                {
                    await ProcessAllWalletsByClient(client,
                        context.Message,
                        scheduledDateTime.Value.ToString("O"),
                        assetInfo.Accuracy);
                }
                _logger.LogInformation($"Processed chunk index {chunkIndex} of clients ({clients.Count})");
                chunkIndex++;
            } while (continuationToken != null);
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
            
            var wallets = await _clientAccountClient.Wallets.GetClientWalletsFilteredAsync(clientId);
            
            _logger.LogDebug($"Found {wallets.Count()} wallets for client '{clientId}'.");
            if (!wallets.Any())
                return;
            
            var creditedAmounts = new List<double>();
            foreach (var wallet in wallets)
            {
                _logger.LogDebug(
                    $"Checking balance for wallet {wallet.Id}:{wallet.Type} for asset {command.AssetId}");
                var balance = await GetWalletBalance(wallet, command.AssetId);
                if (balance != null)
                {
                    await ProcessWallet(clientId,
                        balance.WalletId,
                        balance.Balance,
                        command,
                        scheduledTimeStamp,
                        assetAccuracy,
                        creditedAmounts);
                }
                else
                {
                    _logger.LogDebug(
                        $"Balance for wallet {wallet.Id}:{wallet.Type} for asset {command.AssetId} was not found");
                }
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
            string walletId,
            decimal balance,
            RecurringPayoutCommand command,
            string scheduledTimestamp,
            int assetAccuracy,
            List<double> creditedAmounts)
        {
            var idempotencyId = $"{command.AssetId}:{scheduledTimestamp}:{walletId}";
            // ME can only accept "regular" guids as operationID (idempotency ID),
            // so we create deterministic GUID, based on longer natural idempotency ID
            var operationId = NamespaceGuid.Create(MatchingEngineFixedNamespace, idempotencyId, version: 5);
            var amount = InterestCalculator.CalculateInterest(balance, command.InterestRate, assetAccuracy);

            _logger.LogInformation("Attempting to perform payout {@context}", new
            {
                IdempotencyId = idempotencyId,
                OperationId = operationId,
                ClientId = clientId,
                WalletId = walletId,
                command,
                balance,
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
                            WalletId = walletId,
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
                    _cqrsEngine.PublishEvent(
                        new PayoutCompletedEvent
                        {
                            OperationId = operationId.ToString(),
                            PayoutAssetId = command.PayoutAssetId,
                            AssetId = command.AssetId,
                            ClientId = clientId,
                            WalletId = walletId,
                            Amount = Convert.ToDecimal(amount)
                        },
                        InterestPayoutBoundedContext.Name);
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

        private async Task<WalletBalance> GetWalletBalance(WalletInfo walletInfo, string assetId)
        {
            if (walletInfo == null)
                return null;

            if (walletInfo.Type == WalletType.Trading)
            {
                var trading = await _balancesClient.GetClientBalanceByAssetId(
                    new ClientBalanceByAssetIdModel
                    {
                        AssetId = assetId,
                        ClientId = walletInfo.ClientId
                    });
                return trading == null ? null : new WalletBalance(walletInfo.Id, trading.Balance);
            }
            
            var balance = await _balancesClient.GetClientBalanceByAssetId(
                new ClientBalanceByAssetIdModel
                {
                    AssetId = assetId,
                    ClientId = walletInfo.Id
                });
            return balance == null ? null : new WalletBalance(walletInfo.Id, balance.Balance);
        }

        private record WalletBalance(string WalletId, decimal Balance);
    }
}
