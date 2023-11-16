﻿namespace Message.RPC.RabbitMQ
{
    public interface IMessagePublisher
    {
        void SendMessage<T>(T message, string exchangeName, string routingKey);
    }
}
