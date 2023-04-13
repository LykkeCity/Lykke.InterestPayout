using System;
using InterestPayout.Common.Configuration;
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
            // TODO: Register consumers
            // services.AddTransient<**Consumer>();

            services.AddMassTransit(x =>
            {
                var schedulerEndpoint = new Uri("queue:lykke-pulsar");

                x.AddMessageScheduler(schedulerEndpoint);

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

        private static void ConfigureReceivingEndpoints(IRabbitMqBusFactoryConfigurator configurator,
            IBusRegistrationContext context)
        {
            //TODO register consumer
            // configurator.ReceiveEndpoint("lykke-interest-payout-concrete-consumer-name",
            //     endpoint => { endpoint.Consumer(context.GetRequiredService<**Consumer>); });
        }
    }
}
