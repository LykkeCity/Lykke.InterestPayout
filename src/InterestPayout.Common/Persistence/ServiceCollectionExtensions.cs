using System;
using System.Net;
using System.Net.Http;
using InterestPayout.Common.Configuration;
using InterestPayout.Common.Persistence.ReadModels.AssetInterests;
using InterestPayout.Common.Persistence.ReadModels.PayoutSchedules;
using Lykke.Common.Log;
using Lykke.HttpClientGenerator;
using Lykke.MatchingEngine.Connector.Services;
using Lykke.Service.Assets.Client;
using Lykke.Service.Balances.Client;
using Lykke.Service.ClientAccount.Client;
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
            var defaultTimeout = TimeSpan.FromSeconds(30);
            
            var clientServiceUrl = externalServicesConfig.ClientAccountService.ServiceUrl ??
                          throw new ArgumentNullException(nameof(externalServicesConfig.ClientAccountService));
            services.AddTransient<IClientAccountClient>(s =>
            {
                var httpClientGenerator = HttpClientGenerator.BuildForUrl(clientServiceUrl)
                    .WithTimeout(externalServicesConfig.ClientAccountService.Timeout ?? defaultTimeout)
                    .WithRequestErrorLogging(s.GetService<ILogFactory>())
                    .Create();
                    
                return new ClientAccountClient(httpClientGenerator);
            });
            
            services.AddTransient<TcpMatchingEngineClient>(s =>
            {
                var matchingEngineConnection = externalServicesConfig.MatchingEngineConnectionString.Split(':');
                var matchingEngineIp = Dns.GetHostAddressesAsync(matchingEngineConnection[0]).Result;
                var matchingEnginePort = int.Parse(matchingEngineConnection[1]);

                var matchingEngineEndpoint = new IPEndPoint(matchingEngineIp[0], matchingEnginePort);
                // without enableRetries flag client starts to return null response after 10 seconds being idle
                var tcpMeClient = new TcpMatchingEngineClient(matchingEngineEndpoint, s.GetService<ILogFactory>(), enableRetries: true);
                tcpMeClient.Start();
                return tcpMeClient;
            });
            
            var assetServiceUrl = externalServicesConfig.AssetsService.ServiceUrl ??
                          throw new ArgumentNullException(nameof(externalServicesConfig.AssetsService));
            services.AddTransient<IAssetsService>(s =>
            {
                var timeout = externalServicesConfig.AssetsService.Timeout ?? defaultTimeout;
                return new AssetsService(new Uri(assetServiceUrl), new HttpClient { Timeout = timeout });
            });
            
            var balancesServiceUrl = externalServicesConfig.BalancesService.ServiceUrl ??
                          throw new ArgumentNullException(nameof(externalServicesConfig.BalancesService));
            services.AddTransient<IBalancesClient>(_ => new BalancesClient(balancesServiceUrl));

            return services;
        }

        public static IServiceCollection AddPersistence(this IServiceCollection services,
            DbConfig dbConfig)
        {
            services.AddTransient<IPayoutScheduleRepository, PayoutScheduleRepository>();
            services.AddTransient<IAssetInterestRepository, AssetInterestRepository>();
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
