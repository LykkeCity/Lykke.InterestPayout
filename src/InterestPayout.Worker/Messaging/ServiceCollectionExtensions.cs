using System;
using GreenPipes;
using InterestPayout.Common.Configuration;
using InterestPayout.Common.Domain;
using InterestPayout.Worker.Messaging.Consumers;
using MassTransit;
using MassTransit.RabbitMqTransport;
using Microsoft.Extensions.DependencyInjection;
using Swisschain.Extensions.MassTransit;

namespace InterestPayout.Worker.Messaging
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddMessaging(this IServiceCollection services, RabbitMqConfig rabbitMqConfig)
        {
            EndpointConvention.Map<RecurringPayoutCommand>(new Uri("queue:lykke-interest-payout-recurring-payout-consumer"));

            services.AddTransient<RecurringPayoutCommandConsumer>();
            
            services.AddMassTransit(x =>
            {
                var schedulerEndpoint = new Uri("queue:lykke-pulsar");

                x.AddMessageScheduler(schedulerEndpoint);

                x.AddConsumer<RecurringPayoutCommandConsumer>();
                
                x.UsingRabbitMq((context, cfg) =>
                {
                    if (rabbitMqConfig.HostPort.HasValue)
                    {
                        cfg.Host(rabbitMqConfig.HostUrl,
                            port: rabbitMqConfig.HostPort.Value,
                            virtualHost: "/",
                            host =>
                            {
                                host.Username(rabbitMqConfig.Username);
                                host.Password(rabbitMqConfig.Password);
                            });
                    }
                    else
                    {
                        cfg.Host(rabbitMqConfig.HostUrl,
                            host =>
                            {
                                host.Username(rabbitMqConfig.Username);
                                host.Password(rabbitMqConfig.Password);
                            });
                    }

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
                 endpoint =>
                 {
                     endpoint.UseConcurrencyLimit(1);
                     endpoint.Consumer(context.GetRequiredService<RecurringPayoutCommandConsumer>);
                 });
        }
    }
}
