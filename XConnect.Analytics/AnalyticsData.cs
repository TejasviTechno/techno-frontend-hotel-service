using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XConnect.Analytics
{
    //internal class AnalyticsData
    //{
    //}
    public class AnalyticsData
    {
        public string Action { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public int HttpStatusCode { get; set; }
        public string Stage { get; set; } = string.Empty;
        public object? Result { get; set; }
        public object? StaticResult { get; set; }

        public object Payload { get; set; }
        public long ElapsedTime { get; set; }
        public required string Description { get; set; } = string.Empty;
        public string Errorcode { get; set; } = string.Empty; // new added

        public string Search_Key { get; set; } = string.Empty; // new added
        public string SupplierName { get; set; } = string.Empty; // new added
        public string TechName { get; set; } = string.Empty; // new added
        public string source { get; set; } = string.Empty; // new added
        public double CodeProcessingTimenew { get; set; } // new added 30042026
        public string SuppliersearchResponseError { get; set; } = string.Empty; // new added 05052026
        public string SupplierRequest { get; set; } = string.Empty; // new added 05052026
        public string AgentName { get; set; } = string.Empty; // new added 21052026
        public string ActualDescription { get; set; } = string.Empty; // new added 21052026
        public int srphotelid { get; set; }  // new added 26052026
        public string srphotelhotelname { get; set; } = string.Empty; // new added 26052026

        //public string ErrorCode { get; set; } = string.Empty;
        //public string CheckIn { get; set; } = string.Empty;
        //public string CheckOut { get; set; } = string.Empty;
        //public int NoOfChild { get; set; }
        //public int NoOfAdult { get; set; }
        //public string ApiName { get; set; } = string.Empty;
        //public string Search_Key { get; set; }= string.Empty;

    }
}
