using System;
using System.Net;
using System.Net.Http;
using AzureStorage;
using AzureStorage.Tables;
using InterestPayout.Common.Configuration;
using InterestPayout.Common.Persistence.ExternalEntities.Balances;
using InterestPayout.Common.Persistence.ExternalEntities.Clients;
using InterestPayout.Common.Persistence.ExternalEntities.Wallets;
using InterestPayout.Common.Persistence.ReadModels.PayoutSchedules;
using Lykke.Logs;
using Lykke.MatchingEngine.Connector.Services;
using Lykke.Service.Assets.Client;
using Lykke.SettingsReader.ReloadingManager;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace InterestPayout.Common.Persistence
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddExternalDataSources(this IServiceCollection services,
            ExternalServicesConfig externalServicesConfig)
        {
            // is this an empty log which logs to nowhere or a default log?
            var lykkeLog = LogFactory.Create();
            
            services.AddTransient<INoSQLTableStorage<WalletEntity>>(s =>
            {
                var implementation = AzureTableStorage<WalletEntity>.Create(
                    ConstantReloadingManager.From(externalServicesConfig.WalletsConnectionString),
                    "Wallets",
                    lykkeLog);
                return implementation;
            });
            
            services.AddTransient<INoSQLTableStorage<ClientAccountEntity>>(s =>
            {
                var implementation = AzureTableStorage<ClientAccountEntity>.Create(
                    ConstantReloadingManager.From(externalServicesConfig.ClientPersonalInfoConnectionString),
                    "Traders",
                    lykkeLog);
                return implementation;
            });
            
            services.AddTransient<INoSQLTableStorage<BalanceEntity>>(s =>
            {
                var implementation = AzureTableStorage<BalanceEntity>.Create(
                    ConstantReloadingManager.From(externalServicesConfig.BalancesConnectionString),
                    "Balances",
                    lykkeLog);
                return implementation;
            });
            
            services.AddTransient<TcpMatchingEngineClient>(s =>
            {
                var matchingEngineConnection = externalServicesConfig.MatchingEngineConnectionString.Split(':');
                var matchingEngineIp = Dns.GetHostAddressesAsync(matchingEngineConnection[0]).Result;
                var matchingEnginePort = int.Parse(matchingEngineConnection[1]);

                var matchingEngineEndpoint = new IPEndPoint(matchingEngineIp[0], matchingEnginePort);
                // without enableRetries flag client starts to return null response after 10 seconds being idle
                var tcpMeClient = new TcpMatchingEngineClient(matchingEngineEndpoint, lykkeLog, enableRetries: true);
                tcpMeClient.Start();
                return tcpMeClient;
            });
            
            services.AddSingleton<IAssetsService>(_ => new AssetsService(
                    new Uri(externalServicesConfig.AssetsService.ServiceUrl),
                    new HttpClient()));

            return services;
        }

        public static IServiceCollection AddPersistence(this IServiceCollection services,
            DbConfig dbConfig)
        {
            services.AddTransient<IPayoutScheduleRepository, PayoutScheduleRepository>();
            services.AddTransient<IWalletRepository, WalletRepository>();
            services.AddTransient<IBalanceRepository, BalanceRepository>();
            services.AddSingleton(x =>
            {
                var optionsBuilder = new DbContextOptionsBuilder<DatabaseContext>();
                optionsBuilder
                    .UseLoggerFactory(x.GetRequiredService<ILoggerFactory>())
                    .UseNpgsql(dbConfig.ConnectionString,
                        builder => builder.MigrationsHistoryTable(
                            DatabaseContext.MigrationHistoryTable,
                            DatabaseContext.SchemaName));

                return optionsBuilder;
            });

            return services;
        }
    }
}
