using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RabbitMQ.Client;

namespace XConnect.CommonUtilities
{
    public interface IRabbitMQConnection
    {
        Task<IConnection> GetConnectionAsync();
    }
}
