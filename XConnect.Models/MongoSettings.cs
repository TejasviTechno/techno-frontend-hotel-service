using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XConnect.Models
{
    public class MongoSettings
    {
        public string MongoDB_Active_for_Cachefile { get; set; }
        //public string MongoDB_ConnectionStr { get; set; }
        public string MongoDB_ConnectionStr1 { get; set; }
        public string MongoDB_ConnectionStr2 { get; set; }
        public string MongoDB_ConnectionStr3 { get; set; }
        public string MongoDB_DatabaseName { get; set; }
        public string MongoDB_CollectionName { get; set; }
        public string MongoDB_ConnectionStr_SupplierLog { get; set; }
        public string MongoDB_CollectionName_SuppLog { get; set; }
        public string MongoDB_ConnectionStr_RoomMapping { get; set; }
        public string MongoDB_ConnectionStr_RoomMapping2 { get; set; } //12NOV2025
        public string MongoDB_DatabaseName_RoomMapping { get; set; }
        public string MongoDB_DatabaseName_BookingC { get; set; } //130625

        public string MongoDB_CollectionName_RoomMapping { get; set; }
        public string MongoDB_CollectionName_BookingC { get; set; } //130625
        public string GimmonoxMongoDB_CollectionName_RoomMapping { get; set; } //Add On 30MAY2025
        public string GimmonoxMongoDB_CollectionName_RoomMapping_DAG2 { get; set; } //Add On 17SEP2025
        public string MongoDB_ConStr_SaveAPIRQRSLog { get; set; }
        public string MongoDB_DatabaseName_SaveAPIRQRSLog { get; set; }
        public string MongoDB_CollectionName_SaveAPIRQRSLog { get; set; }
        public string MongoDB_ConStr_SPR { get; set; }
        public string MongoDB_DatabaseName_SPR { get; set; }
        public string MongoDB_CollectionName_SPR { get; set; }
        public string ActiveMongoDB_ForAPI_SearchLogs { get; set; }
        public string SupplierPerformanceReport_MongoDB { get; set; }

        public string MongoDB_ConnectionStr_UmMappedRoomMapping { get; set; } //Add On 17SEP2025 
        public string MongoDB_DatabaseName_UmMappedRoomMapping { get; set; } //Add On 17SEP2025
        public string MongoDB_CollectionName_UmMappedRoomMapping { get; set; } //Add On 17SEP2025
        public string MongoDB_Active_for_Loadbalance { get; set; } //Add On 12NOV2025

        public string MongoDB_ConnectionStr_ExceptionLog { get; set; } //Add On 31-DEC-25
        public string MongoDB_DatabaseName_ExceptionLog { get; set; } //Add On 31-DEC-25

    }
}
