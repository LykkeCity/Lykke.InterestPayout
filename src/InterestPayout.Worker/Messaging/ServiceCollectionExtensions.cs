using System;
using InterestPayout.Common.Configuration;
using InterestPayout.Common.Domain;
using InterestPayout.Worker.Messaging.Consumers;
using MassTransit;
using MassTransit.RabbitMqTransport;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Swisschain.Extensions.MassTransit;

namespace InterestPayout.Worker.Messaging
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddMessaging(this IServiceCollection services, RabbitMqConfig rabbitMqConfig)
        {
            EndpointConvention.Map<RecurringPayoutCommand>(new Uri("queue:lykke-interest-payout-recurring-payout-consumer"));

            services.AddTransient<RecurringPayoutCommandConsumer>();
            
            services.AddLogging(configure =>
            {
                configure.AddConsole();
                configure.AddDebug();
                configure.SetMinimumLevel(LogLevel.Trace);
            });

            services.AddMassTransit(x =>
            {
                var schedulerEndpoint = new Uri("rabbitmq://localhost/quartz");

                x.AddMessageScheduler(schedulerEndpoint);

                x.AddConsumer<RecurringPayoutCommandConsumer>();
                
                x.UsingRabbitMq((context, cfg) =>
                {
                    cfg.Host(rabbitMqConfig.HostUrl,
                        host =>
                        {
                            host.Username(rabbitMqConfig.Username);
                            host.Password(rabbitMqConfig.Password);
                        });
                        
                    cfg.UseMessageScheduler(schedulerEndpoint);

                    cfg.UseDefaultRetries(context);

                    ConfigureReceivingEndpoints(cfg, context);
                });
            });
            
            services.AddMassTransitBusHost();
            
            return services;
        }

        private static void ConfigureReceivingEndpoints(IRabbitMqBusFactoryConfigurator configurator, IBusRegistrationContext context)
        {
             configurator.ReceiveEndpoint("lykke-interest-payout-recurring-payout-consumer",
                 endpoint => { endpoint.Consumer(context.GetRequiredService<RecurringPayoutCommandConsumer>); });
        }
    }
}
