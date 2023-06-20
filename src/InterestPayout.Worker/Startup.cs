using InterestPayout.Common.Application;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using InterestPayout.Common.Configuration;
using InterestPayout.Common.Domain;
using InterestPayout.Common.Extensions;
using InterestPayout.Common.Persistence;
using InterestPayout.Worker.HostedServices;
using InterestPayout.Worker.Messaging;
using InterestPayout.Worker.Messaging.Consumers;
using Lykke.Logs;
using Lykke.SettingsReader;
using Swisschain.Extensions.EfCore;
using Swisschain.Sdk.Server.Common;
using Microsoft.EntityFrameworkCore;
using Swisschain.Extensions.Idempotency;
using Swisschain.Extensions.Idempotency.EfCore;
using Swisschain.Extensions.Idempotency.MassTransit;

namespace InterestPayout.Worker
{
    public sealed class Startup : SwisschainStartup<AppConfig>
    {
        public Startup(IConfiguration configuration)
            : base(configuration)
        {
        }

        protected override void ConfigureServicesExt(IServiceCollection services)
        {
            base.ConfigureServicesExt(services);

            var reloadingManagerSettings = ConfigRoot.LoadSettings();
            
            services
                .AddHttpClient()
                .AddLykkeLogging(
                    reloadingManagerSettings.ConnectionString(x => x.Azure.LogConnectionString),
                    "InterestPayout",
                    Config.Azure.SlackConnString,
                    Config.Azure.SlackNotificationsQueue)
                .AddLykkeCqrs(Config.RabbitMq)
                .AddSingleton<NotificationConfig>(Config.Notifications)
                .AddSingleton<IPayoutConfigService>(new PayoutConfigService(Config.Payouts, Config.SmallestPayoutScheduleInterval))
                .AddTransient<IRecurringPayoutsScheduler, RecurringPayoutsScheduler>()
                .AddPersistence(Config.Db)
                .AddExternalDataSources(Config.ExternalServices)
                .AddIdempotency<UnitOfWork>(c =>
                {
                    c.DispatchWithMassTransit();
                    c.PersistWithEfCore(s =>
                        {
                            var optionsBuilder = s.GetRequiredService<DbContextOptionsBuilder<DatabaseContext>>();

                            return new DatabaseContext(optionsBuilder.Options);
                        },
                        o =>
                        {
                            o.OutboxDeserializer.AddAssembly(typeof(RecurringPayoutCommand).Assembly);
                            o.OutboxDeserializer.AddAssembly(typeof(RecurringPayoutCommandConsumer).Assembly);
                        });
                })
                .AddEfCoreDbMigration(options =>
                {
                    options.UseDbContextFactory(factory =>
                    {
                        var builder = factory.GetRequiredService<DbContextOptionsBuilder<DatabaseContext>>();
                        var context = new DatabaseContext(builder.Options);
                        return context;
                    });
                })
                .AddHostedService<CqrsMessagingInitializer>()
                .AddMessaging(Config.RabbitMq)
                .AddHostedService<RecurringPayoutsScheduleInitializer>();
        }
    }
}
