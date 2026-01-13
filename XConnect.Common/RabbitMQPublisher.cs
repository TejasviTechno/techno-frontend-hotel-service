using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;

namespace XConnect.CommonUtilities
{

    public class RabbitMQPublisher : IAsyncDisposable
    {
        private readonly IRabbitMQConnection _connectionProvider;
        private IChannel? _channel;

        public RabbitMQPublisher(IRabbitMQConnection connectionProvider)
        {
            _connectionProvider = connectionProvider;
        }

        public async Task SendMessageAsync(object msg, string queueName)
        {
            if (_channel == null || !_channel.IsOpen)
            {
                var connection = await _connectionProvider.GetConnectionAsync();
                _channel = await connection.CreateChannelAsync();
            }

            await _channel.QueueDeclareAsync(queue: queueName,
                                            durable: true,
                                            exclusive: false,
                                            autoDelete: false);

            var json = Newtonsoft.Json.JsonConvert.SerializeObject(msg);
            var body = Encoding.UTF8.GetBytes(json);

            //var props = channel.CreateBasicProperties();

            await _channel.BasicPublishAsync(
                exchange: "",
                routingKey: queueName,
                body: body,
                cancellationToken: default);
        }
        public async ValueTask DisposeAsync()
        {
            if (_channel is { IsOpen: true })
            {
                await _channel.CloseAsync();
                await _channel.DisposeAsync();
            }
        }
    }
    //public class RabbitMQPublisher
    //{
    //    private readonly IRabbitMQConnection _connectionProvider;

    //    public RabbitMQPublisher(IRabbitMQConnection connectionProvider)
    //    {
    //        _connectionProvider = connectionProvider;
    //    }

    //    public async Task SendMessageAsync(object msg, string queueName)
    //    {
    //        var connection = await _connectionProvider.GetConnectionAsync();
    //        await using var channel = await connection.CreateChannelAsync();

    //        await channel.QueueDeclareAsync(queue: queueName,
    //                                        durable: true,
    //                                        exclusive: false,
    //                                        autoDelete: false);

    //        var json = Newtonsoft.Json.JsonConvert.SerializeObject(msg);
    //        var body = Encoding.UTF8.GetBytes(json);

    //        //var props = channel.CreateBasicProperties();

    //        await channel.BasicPublishAsync(
    //            exchange: "",
    //            routingKey: queueName,
    //            body: body,
    //            cancellationToken: default);
    //    }
    //}
}
