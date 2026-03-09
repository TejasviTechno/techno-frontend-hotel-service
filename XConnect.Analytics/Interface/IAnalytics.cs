using QueueManager.Contracts.Models.Hotel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XConnect.Analytics.Interface
{
    //internal interface IAnalytics
    //{
    //}
    public interface IPushAnalytics
    {
        void PushClientDataForAnalyticsAsyc(ClientData clientRequest);
    }
    public interface IAnalytics
    {
        void PushHotelDetailsClientDataForAnalyticsAsyc(AnalyticsData analyticsData);
        //void PushRestaurantSearchClientDataForAnalyticsAsyc(AnalyticsData analyticsData);
    }
   
    public interface IPushAnalyticsSupplier
    {
        void PushSupplierDataForAnalyticsAsyc(List<SupplierData> supplierRequest);
    }
    public interface IAnalyticsSupplier
    {
        void PushHotelSearchSupplierDataForAnalyticsAsyc(AnalyticsData analyticsData);
        //void PushRestaurantDetailsSupplierDataForAnalyticsAsyc(AnalyticsData analyticsData);
    }
}
