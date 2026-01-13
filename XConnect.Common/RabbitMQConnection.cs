using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XConnect.Models;

namespace XConnect.CommonUtilities
{
    public class RabbitMQConnection : IRabbitMQConnection, IAsyncDisposable
    {
        private readonly ConnectionFactory _factory;
        private IConnection? _connection;
        private readonly RabitMQSettings _rabitmqSettings;


        public RabbitMQConnection(IConfiguration config, IOptions<RabitMQSettings> rabitmqSettings)
        {

            _rabitmqSettings = rabitmqSettings.Value;


            _factory = new ConnectionFactory
            {
                //HostName = config["RabbitMQ:HostName"] ?? "localhost",
                //UserName = config["RabbitMQ:UserName"] ?? "guest",
                //Password = config["RabbitMQ:Password"] ?? "guest"
               
                //HostName = config["RabbitMQ:HostName"] ?? "10.32.8.90", // No http:// and no port
                //Port = int.TryParse(config["RabbitMQ:Port"], out var port) ? port : 5672,
                //UserName = config["RabbitMQ:UserName"] ?? "admin",
                //Password = config["RabbitMQ:Password"] ?? "73547myr3p6YQ6t3md"

                HostName = _rabitmqSettings.HostName, // No http:// and no port
                Port = Convert.ToInt32(_rabitmqSettings.Port) ,
                UserName = _rabitmqSettings.UserName,
                Password = _rabitmqSettings.Password

            };
        }

        public async Task<IConnection> GetConnectionAsync()
        {
            if (_connection is { IsOpen: true })
                return _connection;

            _connection = await _factory.CreateConnectionAsync();
            return _connection;
        }

        public async ValueTask DisposeAsync()
        {
            if (_connection is { IsOpen: true })
            {
                await _connection.CloseAsync();
                await _connection.DisposeAsync();
            }
        }
    }
}
