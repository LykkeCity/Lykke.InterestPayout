﻿using System;
using System.Collections.Concurrent;
using System.Linq;
using Common.Log;
using Lykke.Common.Log;
using Lykke.Messaging;
using Lykke.Messaging.Transports;

namespace InterestPayout.Common.Cqrs.RabbitMq
{
    /// <summary>
    /// Implementation of <see cref="ITransportFactory"/> interface for RabbitMQ
    /// </summary>
    public class RabbitMqTransportFactory : ITransportFactory
    {
        private readonly ILogFactory _logFactory;
        private readonly bool m_ShuffleBrokers;
        private readonly TimeSpan? m_AutomaticRecoveryInterval;
        private readonly ConcurrentDictionary<TransportInfo, RabbitMqTransport> _transports = new ConcurrentDictionary<TransportInfo, RabbitMqTransport>();

        public string Name => "RabbitMq";

        /// <summary>
        /// Creates new instance of <see cref="RabbitMqTransportFactory"/> with RabbitMQ native automatic recovery disabled
        /// </summary>
        [Obsolete]
        public RabbitMqTransportFactory()
            : this(true, default(TimeSpan?))
        {
        }

        /// <summary>
        /// Creates new instance of <see cref="RabbitMqTransportFactory"/> with RabbitMQ native automatic recovery enabled
        /// </summary>
        /// <param name="automaticRecoveryInterval">TimeStamp to enable auto recovery for underlying RabbitMQ client. Use TimeStamp.FromSeconds(5).</param>
        [Obsolete]
        public RabbitMqTransportFactory(TimeSpan automaticRecoveryInterval)
             : this(true, automaticRecoveryInterval)
        {
        }

        /// <summary>
        /// Creates new instance of <see cref="RabbitMqTransportFactory"/>
        /// </summary>
        /// <param name="shuffleBrokers">True to shuffle brokers, False to iterate brokers in default order</param>
        /// <param name="automaticRecoveryInterval">Interval for automatic recover if set to null automaitc recovery is disabled, 
        /// if set to some value automatic recovery is enabled and NetworkRecoveryInterval of RabbitMQ client is set provided valie
        /// </param>
        [Obsolete]
        internal RabbitMqTransportFactory(bool shuffleBrokers, TimeSpan? automaticRecoveryInterval = default(TimeSpan?))
        {
            m_ShuffleBrokers = shuffleBrokers;
            m_AutomaticRecoveryInterval = automaticRecoveryInterval;
        }

        /// <summary>
        /// Creates new instance of <see cref="RabbitMqTransportFactory"/>
        /// </summary>
        /// <param name="logFactory"></param>
        /// <param name="shuffleBrokers">True to shuffle brokers, False to iterate brokers in default order</param>
        /// <param name="automaticRecoveryInterval">Interval for automatic recover if set to null automaitc recovery is disabled, 
        /// if set to some value automatic recovery is enabled and NetworkRecoveryInterval of RabbitMQ client is set provided valie
        /// </param>
        public RabbitMqTransportFactory(ILogFactory logFactory, bool shuffleBrokers = true, TimeSpan? automaticRecoveryInterval = null)
        {
            _logFactory = logFactory ?? throw new ArgumentNullException(nameof(logFactory));
            m_ShuffleBrokers = shuffleBrokers;
            m_AutomaticRecoveryInterval = automaticRecoveryInterval;
        }

        [Obsolete]
        public ITransport Create(ILog log, TransportInfo transportInfo, Action onFailure)
        {
            var brokers = transportInfo.Broker.Split(',').Select(b => b.Trim()).ToArray();
            return new RabbitMqTransport(
                log,
                brokers,
                transportInfo.Login,
                transportInfo.Password,
                m_ShuffleBrokers,
                m_AutomaticRecoveryInterval
            );
        }

        public ITransport Create(TransportInfo transportInfo, Action onFailure)
        {
            return _transports.GetOrAdd(transportInfo, ti =>
            {
                var brokers = ti.Broker
                    .Split(',')
                    .Select(b => b.Trim())
                    .ToArray();

                return new RabbitMqTransport(
                    _logFactory,
                    brokers,
                    ti.Login,
                    ti.Password,
                    m_ShuffleBrokers,
                    m_AutomaticRecoveryInterval
                );
            });
        }
    }
}
