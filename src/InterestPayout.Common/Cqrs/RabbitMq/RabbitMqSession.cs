﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reactive.Disposables;
using System.Text;
using Common.Log;
using Lykke.Common.Log;
using Lykke.Messaging.Contract;
using Lykke.Messaging.RabbitMq;
using Lykke.Messaging.Transports;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace InterestPayout.Common.Cqrs.RabbitMq
{
    internal class RabbitMqSession : IMessagingSession
    {
        private readonly ILog _log;
        private readonly IConnection m_Connection;
        private readonly IModel m_Model;
        private readonly CompositeDisposable m_Subscriptions = new CompositeDisposable();
        private readonly Dictionary<string, DefaultBasicConsumer> m_Consumers = new Dictionary<string, DefaultBasicConsumer>();
        private readonly Action<RabbitMqSession, PublicationAddress, Exception> m_OnSendFail;

        private bool m_ConfirmedSending = false;
        private readonly ILogFactory _logFactory;

        [Obsolete]
        public RabbitMqSession(
            ILog log,
            IConnection connection,
            bool confirmedSending = false,
            Action<RabbitMqSession, PublicationAddress, Exception> onSendFail = null)
        {
            _log = log;
            m_OnSendFail = onSendFail??((s,d,e) => { });
            m_Connection = connection;
            m_Model = m_Connection.CreateModel();
            if(confirmedSending)
                m_Model.ConfirmSelect();
            //NOTE: looks like publish confirm is required for guaranteed delivery
            //smth like:
            //  m_Model.ConfirmSelect();
            //and publish like this:
            //  m_Model.BasicPublish()
            //  m_Model.WaitForConfirmsOrDie();
            //it will wait for ack from server and throw exception if message failed to persist ons srever side (e.g. broker reboot)
            //more info here: http://rianjs.net/2013/12/publisher-confirms-with-rabbitmq-and-c-sharp

            m_Model.BasicQos(0, 300, false);
            connection.ConnectionShutdown += (connection1, reason) =>
                {
                    lock (m_Consumers)
                    {
                        foreach (IDisposable consumer in m_Consumers.Values)
                        {
                            consumer.Dispose();
                        }
                    }
                };
        }

        public RabbitMqSession(
            ILogFactory logFactory,
            IConnection connection,
            bool confirmedSending = false,
            Action<RabbitMqSession, PublicationAddress, Exception> onSendFail = null)
        {

            _logFactory = logFactory ?? throw new ArgumentNullException(nameof(logFactory));
            _log = logFactory.CreateLog(this);

            m_OnSendFail = onSendFail ?? ((s, d, e) => { });
            m_Connection = connection;
            m_Model = m_Connection.CreateModel();
            if (confirmedSending)
                m_Model.ConfirmSelect();
            //NOTE: looks like publish confirm is required for guaranteed delivery
            //smth like:
            //  m_Model.ConfirmSelect();
            //and publish like this:
            //  m_Model.BasicPublish()
            //  m_Model.WaitForConfirmsOrDie();
            //it will wait for ack from server and throw exception if message failed to persist ons srever side (e.g. broker reboot)
            //more info here: http://rianjs.net/2013/12/publisher-confirms-with-rabbitmq-and-c-sharp

            m_Model.BasicQos(0, 300, false);
            connection.ConnectionShutdown += (connection1, reason) =>
            {
                lock (m_Consumers)
                {
                    foreach (var consumer in m_Consumers.Values.OfType<IDisposable>())
                    {
                        consumer.Dispose();
                    }
                }
            };
        }

        public Destination CreateTemporaryDestination()
        {
            var queueName = m_Model.QueueDeclare().QueueName;
            return new Destination { Subscribe = queueName, Publish = new PublicationAddress("direct", "", queueName).ToString() };
        }

        public void Send(string destination, BinaryMessage message, int ttl)
        {
            Send(destination, message, properties =>
                {
                    if (ttl > 0) properties.Expiration = ttl.ToString(CultureInfo.InvariantCulture);
                });
        }

        private void Send(string destination, BinaryMessage message, Action<IBasicProperties> tuneMessage = null)
        {
            var publicationAddress = PublicationAddress.Parse(destination) ?? new PublicationAddress("direct", destination, "");
            Send(publicationAddress, message, tuneMessage);
        }

        private void Send(PublicationAddress destination, BinaryMessage message, Action<IBasicProperties> tuneMessage = null)
        {
            try
            {
                var properties = m_Model.CreateBasicProperties();

                properties.Headers = new Dictionary<string, object>();
                properties.DeliveryMode = 2; //persistent
                foreach (var header in message.Headers)
                {
                    properties.Headers[header.Key] = header.Value;
                }
                if (message.Type != null)
                    properties.Type = message.Type;
                tuneMessage?.Invoke(properties);

                properties.Headers.Add("initialRoute", destination.ToString());
                lock (m_Model)
                {
                    m_Model.BasicPublish(destination.ExchangeName, destination.RoutingKey, true, properties, message.Bytes);
                    if (m_ConfirmedSending)
                        m_Model.WaitForConfirmsOrDie();
                }
            }
            catch (AlreadyClosedException e)
            {
                m_OnSendFail(this,destination,e);
                throw;
            }
        }

        public RequestHandle SendRequest(string destination, BinaryMessage message, Action<BinaryMessage> callback)
        {
            string queue;
            lock(m_Model)
                queue = m_Model.QueueDeclare().QueueName;
            var request = new RequestHandle(callback, () => { }, cb => Subscribe(queue, (binaryMessage, acknowledge) => { 
                cb(binaryMessage);
                acknowledge(true);
            }, null));
            m_Subscriptions.Add(request);
// ReSharper disable ImplicitlyCapturedClosure
            Send(destination, message, p => p.ReplyTo = new PublicationAddress("direct", "", queue).ToString());
// ReSharper restore ImplicitlyCapturedClosure
            return request;
        }

        public IDisposable RegisterHandler(string destination, Func<BinaryMessage, BinaryMessage> handler, string messageType)
        {
           
            var subscription = Subscribe(destination, (properties, bytes, acknowledge) =>
            {
                var correlationId = properties.CorrelationId;
                var responseBytes = handler(ToBinaryMessage(properties, bytes));
                //If replyTo is not parsable we treat it as queue name and message is sent via default exchange  (http://www.rabbitmq.com/tutorials/amqp-concepts.html#exchange-default)
                var publicationAddress = PublicationAddress.Parse(properties.ReplyTo) ?? new PublicationAddress("direct", "", properties.ReplyTo);
                Send(publicationAddress, responseBytes, p =>
                    {
                        if (correlationId != null)
                            p.CorrelationId = correlationId;
                    });
                acknowledge(true);
            }, messageType);

            return subscription;
        }

        public IDisposable Subscribe(string destination, Action<BinaryMessage, Action<bool>> callback, string messageType)
        {
            return Subscribe(destination, (properties, bytes, acknowledge) => callback(ToBinaryMessage(properties, bytes), acknowledge), messageType);
        }

        private BinaryMessage ToBinaryMessage(IBasicProperties properties, ReadOnlyMemory<byte> bytes)
        {
            
            var binaryMessage = new BinaryMessage {Bytes = bytes.ToArray(), Type = properties.Type};
            if (properties.Headers != null)
            {
                foreach (var header in properties.Headers)
                {
                    var value = header.Value as byte[];
                    binaryMessage.Headers[header.Key] = value == null ? null : Encoding.UTF8.GetString(value);
                }
            }
            return binaryMessage;
        }

        private IDisposable Subscribe(string destination,
            Action<IBasicProperties, ReadOnlyMemory<byte>, Action<bool>> callback,
            string messageType)
        {
            lock (m_Consumers)
            {
                DefaultBasicConsumer basicConsumer;
                m_Consumers.TryGetValue(destination, out basicConsumer);
                if (messageType == null)
                {
                    if (basicConsumer is SharedConsumer)
                        throw new InvalidOperationException("Attempt to subscribe for shared destination without specifying message type. It should be a bug in MessagingEngine");
                    if (basicConsumer != null)
                        throw new InvalidOperationException("Attempt to subscribe for same destination twice.");
                    return SubscribeNonShared(destination, callback);
                }

                if (basicConsumer is Consumer)
                    throw new InvalidOperationException("Attempt to subscribe for non shared destination with specific message type. It should be a bug in MessagingEngine");

                return SubscribeShared(
                    destination,
                    callback,
                    messageType,
                    basicConsumer as SharedConsumer);
            }
        }

        private IDisposable SubscribeShared(
            string destination,
            Action<IBasicProperties, ReadOnlyMemory<byte>, Action<bool>> callback,
            string messageType,
            SharedConsumer consumer)
        {
            throw new NotImplementedException();
        }

        private IDisposable SubscribeNonShared(string destination, Action<IBasicProperties, ReadOnlyMemory<byte>, Action<bool>> callback)
        {
            var consumer = _logFactory == null 
                ? new Consumer(_log, m_Model, callback)
                : new Consumer(_logFactory, m_Model, callback);

            lock (m_Model)
                m_Model.BasicConsume(destination, false, consumer);
            m_Consumers[destination] = consumer;
            // ReSharper disable ImplicitlyCapturedClosure
            return Disposable.Create(() =>
                {
                    lock (m_Consumers)
                    {
                        consumer.Dispose();
                        m_Consumers.Remove(destination);
                    }
                });
            // ReSharper restore ImplicitlyCapturedClosure
        }

        public void Dispose()
        {
            lock (m_Consumers)
            {
                foreach (IDisposable consumer in m_Consumers.Values)
                {
                    consumer.Dispose();
                }
            }
            lock (m_Model)
            {
                try
                {
                    m_Model.Close(200, "Goodbye");
                    m_Model.Dispose();
                }
                catch (Exception e)
                {
                    _log.WriteError(nameof(RabbitMqSession), nameof(Dispose), e);
                }
            }

            try
            {
                m_Connection.Close();
                m_Connection.Dispose();
            }
            catch (Exception e)
            {
                _log.WriteError(nameof(RabbitMqSession), nameof(Dispose), e);
            }
        }
    }
}
