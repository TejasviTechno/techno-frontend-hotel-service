using Microsoft.Extensions.Logging;
using QueueManager.Contracts.Interfaces;
using QueueManager.Contracts.Models.Hotel;
using XConnect.Analytics.Interface;

namespace XConnect.Analytics
{
    //public class Analytics
    //{

    //}
    public class Analytic : IPushAnalytics
    {
        private readonly IQueueManager _analyticMapper;
        private readonly ILogger<Analytic> _logger;


        public Analytic(IQueueManager analyticMapper, ILogger<Analytic> logger)
        {
            _analyticMapper = analyticMapper;

            _logger = logger;


        }
        public void PushClientDataForAnalyticsAsyc(ClientData clientRequest)
        {

            _ = Task.Run(async () =>
            {
                try
                {

                    if (clientRequest is not null)
                    {
                        await _analyticMapper.SendModelToQueueAsync(clientRequest);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Errot at PushClientDataForAnalyticsAsyc");
                }
            });


        }





    }

    public class AnalyticSupplier : IPushAnalyticsSupplier
    {
        private readonly IQueueSupplierManager _analyticMapper;
        private readonly ILogger<AnalyticSupplier> _logger;


        public AnalyticSupplier(IQueueSupplierManager analyticMapper, ILogger<AnalyticSupplier> logger)
        {
            _analyticMapper = analyticMapper;
            _logger = logger;
        }
        public void PushSupplierDataForAnalyticsAsyc(List<SupplierData> supplierRequest)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    if (supplierRequest is not null)
                    {
                        await _analyticMapper.SendBulkModelToQueueAsync(supplierRequest);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Errot at PushSupplierDataForAnalyticsAsyc");
                }
            });
        }
    }
}
