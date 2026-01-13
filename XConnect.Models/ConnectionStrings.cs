using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XConnect.Models
{
    public class ConnectionStrings
    {
        public string? ConnectionString { get; set; } = String.Empty;
        public string? ConnectionStringControl { get; set; } = String.Empty;
        public string? ConnectionStringBalancer { get; set; } = String.Empty;
        public string? ConnectionStringforlogtrack { get; set; } = String.Empty;
        public string? ConnectionString_SaveSupplierLog { get; set; } = String.Empty;
        public string? ConnectionString_SupplierLog_report { get; set; } = String.Empty;
        public string? CacheConnectionString { get; set; } = String.Empty;

        public string? ConnectionString_ReplicationDB { get; set; } = String.Empty;
        public string? ConnectionString_ReplicationDB1 { get; set; } = String.Empty;
        public string? ConnectionString_ReplicationDB2 { get; set; } = String.Empty;
        public string? ConnectionString_ReplicationDB3 { get; set; } = String.Empty;
        public string? ConnectionString_ReplicationDB4 { get; set; } = String.Empty;
        public string? ConnectionStringforMappingVervo { get; set; } = String.Empty;
        public string? ConnectionStringforlogtrack_APILogVM { get; set; } = String.Empty;
        //public string? ConnectionString_cm { get; set; } = String.Empty;
        public string? ConnectionString_cm { get; set; } = String.Empty;
        public string? ConnectionString_cm2 { get; set; } = String.Empty;

        public string? ConnectionString_OTH_Replica { get; set; } = String.Empty;
        public string? ConnectionString_OTH2_Replica { get; set; } = String.Empty;
        public string? ConnectionStringforDerbysoft { get; set; } = String.Empty;
        public string? connectionStr_Logserver2 { get; set; } = String.Empty; //210425
        public string? connectionStr_Logserver2_TP { get; set; } = String.Empty; //Recheck - 270525
        public string? ConnectionStringforlogtrack_180VM { get; set; } = String.Empty; //Recheck - 270525
        public string? ConnectionStringforlog_180VM { get; set; } = String.Empty; //16/12/25

    }
}
