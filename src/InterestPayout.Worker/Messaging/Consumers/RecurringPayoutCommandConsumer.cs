using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AzureStorage;
using InterestPayout.Common.Application;
using InterestPayout.Common.Domain;
using InterestPayout.Common.Persistence;
using InterestPayout.Common.Persistence.ReadModels.Balances;
using InterestPayout.Common.Persistence.ReadModels.Clients;
using InterestPayout.Common.Persistence.ReadModels.Wallets;
using Lykke.MatchingEngine.Connector.Models.Api;
using Lykke.MatchingEngine.Connector.Services;
using MassTransit;
using MassTransit.Util;
using Microsoft.Extensions.Logging;
using Swisschain.Extensions.Idempotency;

namespace InterestPayout.Worker.Messaging.Consumers
{
    public class RecurringPayoutCommandConsumer : IConsumer<RecurringPayoutCommand>
    {
        private readonly ILogger<RecurringPayoutCommandConsumer> _logger;
        private readonly IWalletRepository _walletRepository;
        private readonly INoSQLTableStorage<ClientAccountEntity> _clientStorage;
        private readonly IBalanceRepository _balanceRepository;
        private readonly TcpMatchingEngineClient _matchingEngineClient;
        private readonly IUnitOfWorkManager<UnitOfWork> _unitOfWorkManager;

        public RecurringPayoutCommandConsumer(ILogger<RecurringPayoutCommandConsumer> logger,
            IWalletRepository walletRepository,
            INoSQLTableStorage<ClientAccountEntity> clientStorage,
            IBalanceRepository balanceRepository,
            TcpMatchingEngineClient matchingEngineClient,
            IUnitOfWorkManager<UnitOfWork> unitOfWorkManager)
        {
            _logger = logger;
            _walletRepository = walletRepository;
            _clientStorage = clientStorage;
            _balanceRepository = balanceRepository;
            _matchingEngineClient = matchingEngineClient;
            _unitOfWorkManager = unitOfWorkManager;
        }

        public async Task Consume(ConsumeContext<RecurringPayoutCommand> context)
        {
            var scheduledDateTime = context.Headers.Get<DateTimeOffset>("MT-Quartz-Scheduled");
            if (!scheduledDateTime.HasValue)
                throw new InvalidOperationException("Cannot obtain original scheduled time from the header 'MT-Quartz-Scheduled'");
            
            if (await IsMessageExpired(scheduledDateTime.Value, context.Message))
                return;
            
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
                    TaskUtil.Await(ProcessAllWalletsByClient(client.Id, context.Message, scheduledDateTime.Value.ToString("O")));
                }
                _logger.LogInformation("Processed chunk of clients (" + clients.Count + ")");
            });
            _logger.LogInformation("Finished processing all chunks {@context}", new
            {
                context.Message
            });
        }

        private async Task ProcessAllWalletsByClient(string clientId, RecurringPayoutCommand command, string scheduledTimeStamp)
        {
            _logger.LogInformation("Starting processing recurring payout for asset for client {@context}", new
            {
                ClientId = clientId,
                command,
                scheduledTimeStamp
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
            List<double> creditedAmounts)
        {
            var idempotencyId = $"{command.AssetId}:{scheduledTimestamp}:{balance.WalletId}";
            var amount = InterestCalculator.CalculateInterest(balance.Balance, command.InterestRate, command.Accuracy);

            _logger.LogInformation("Attempting to perform payout {@context}", new
            {
                IdempotencyId = idempotencyId,
                ClientId = clientId,
                WalletId = balance.WalletId,
                command,
                balance.WalletType,
                balance.Balance,
                Amount = amount
            });
            
            await using var unitOfWork = await _unitOfWorkManager.Begin(idempotencyId);
            if (!unitOfWork.Outbox.IsClosed)
            {
                MeResponseModel matchingEngineResponse;
                try
                {
                     matchingEngineResponse = await _matchingEngineClient.CashInOutAsync(idempotencyId,
                        clientId,
                        command.AssetId,
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
                        matchingEngineResponse.TransactionId,
                        matchingEngineResponse.Message,
                        matchingEngineResponse.Status,
                        Amount = amount,
                        command
                    });
                    await unitOfWork.Commit();
                    creditedAmounts.Add(amount);
                }
                else if (matchingEngineResponse.Status == MeStatusCodes.Duplicate)
                {
                    _logger.LogError("Unexpected duplication of requests to ME {@context}", new
                    {
                        IdempotencyId = idempotencyId,
                        matchingEngineResponse.TransactionId,
                        matchingEngineResponse.Message,
                        matchingEngineResponse.Status,
                        Amount = amount,
                        command
                    });
                    throw new InvalidOperationException(
                        $"Unexpected duplication of requests to ME. IdempotencyId: '{idempotencyId}', TransactionId: '{matchingEngineResponse.TransactionId}'");
                }
                else if (matchingEngineResponse.Status == MeStatusCodes.InvalidVolumeAccuracy)
                {
                    _logger.LogError("Interest payout failed because of invalid interest or accuracy settings {@context}", new
                    {
                        IdempotencyId = idempotencyId,
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
            var needSkip = latestAcceptableExecutionTimeStamp < currentTimeStamp ? "Yes" : "No";
            _logger.LogInformation($"Need skip? {needSkip}. Scheduled: {originalScheduledTimestamp:T}, latestOkExecution: {latestAcceptableExecutionTimeStamp:T}, now: {currentTimeStamp:T}");
            
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
