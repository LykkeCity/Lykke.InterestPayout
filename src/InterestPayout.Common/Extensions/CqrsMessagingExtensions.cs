using System;
using System.Collections.Generic;
using InterestPayout.Common.Configuration;
using InterestPayout.Common.Cqrs;
using Lykke.Common.Log;
using Lykke.Cqrs;
using Lykke.Cqrs.Configuration;
using Lykke.Cqrs.Configuration.Routing;
using Lykke.Cqrs.Middleware.Logging;
using Lykke.InterestPayout.MessagingContract;
using Lykke.Messaging;
using Lykke.Messaging.Contract;
using Lykke.Messaging.Serialization;
using Microsoft.Extensions.DependencyInjection;
using RabbitMqTransportFactory = InterestPayout.Common.Cqrs.RabbitMq.RabbitMqTransportFactory;

namespace InterestPayout.Common.Extensions
{
    public static class CqrsMessagingExtensions
    {
        public static IServiceCollection AddLykkeCqrs(this IServiceCollection serviceCollection, RabbitMqConfig rabbitMqConfig)
        {
            serviceCollection.AddSingleton<IDependencyResolver, ServiceProviderDependencyResolver>();

            if (string.IsNullOrWhiteSpace(rabbitMqConfig?.CqrsConnString))
                throw new InvalidOperationException("RabbitMq.CqrsConnString is required.");
            
            var rabbitMqCqrsSettings = new RabbitMQ.Client.ConnectionFactory { Uri = new Uri(rabbitMqConfig.CqrsConnString) };
            serviceCollection.AddSingleton<IMessagingEngine>(c => new MessagingEngine(
                c.GetService<ILogFactory>(),
                new TransportResolver(new Dictionary<string, TransportInfo>
                {
                    {
                        "RabbitMq",
                        new TransportInfo(rabbitMqCqrsSettings.Endpoint.ToString(),
                            rabbitMqCqrsSettings.UserName,
                            rabbitMqCqrsSettings.Password,
                            "None",
                            "RabbitMq")
                    }
                }),
                new RabbitMqTransportFactory(c.GetService<ILogFactory>())));
            
            const string environment = "lykke";
            var rabbitMqConventionEndpointResolver =
                new RabbitMqConventionEndpointResolver("RabbitMq",
                    SerializationFormat.MessagePack,
                    environment: environment);
            
            serviceCollection.AddSingleton<ICqrsEngine>(c => new CqrsEngine(
                c.GetService<ILogFactory>(),
                c.GetService<IDependencyResolver>(),
                c.GetService<IMessagingEngine>(),
                new DefaultEndpointProvider(),
                true,
                Register.DefaultEndpointResolver(rabbitMqConventionEndpointResolver),
                Register.DefaultEndpointResolver(new RabbitMqConventionEndpointResolver(
                    "RabbitMq",
                    SerializationFormat.MessagePack,
                    environment: environment)),
                Register.EventInterceptors(new DefaultEventLoggingInterceptor(c.GetService<ILogFactory>())),
                Register.BoundedContext(InterestPayoutBoundedContext.Name)
                    .PublishingEvents(
                        typeof (PayoutCompletedEvent)
                    )
                    .With("events"))
            );

            return serviceCollection;
        }
    }
}
