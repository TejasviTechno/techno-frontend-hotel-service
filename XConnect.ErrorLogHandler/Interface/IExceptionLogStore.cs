using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XConnect.Models;

namespace XConnect.ErrorLogHandler.Interface
{
    public interface IExceptionLogStore
    {
        Task SaveAsync(ExceptionLogDocument doc);

        Task SaveLogAsync(ResLog log, string CollectionName);
        Task SaveRequestTrackingLogAsync(RequestTrackingLog log, string CollectionName);

        Task SaveLogRabbitMQAsync(ExceptionLogDocument doc);
    }
}
