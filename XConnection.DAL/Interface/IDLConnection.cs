
using Microsoft.Data.SqlClient;
using System.Data;


namespace XConnect.DAL.Interface
{
    public interface IDLConnection
    {
        Task<DataSet> GetDataSet_ReplicationDB_Host(string SPName, SqlParameter[] para, Int32 Timeout); //Added on 21-JAN-26
        Task<object> GetExecuteScalarSP_WithConStr_1SecTimeout_Log(string Qry, SqlParameter[] para);//16/12/25
        Task<DataTable> GetDataTable_Replica(string SPName, SqlParameter[] para, int Timeout);
        Task<DataSet> GetDataSet_Limit(String SPName, SqlParameter[] para, int Timeout = 2);
        Task<DataTable> GetDataTableAsync(string SPName);
        DataTable GetDataTable(string SPName, SqlParameter[] para);

        Task<DataTable> GetDataTableAsync(string SPName, SqlParameter[] para);
        Task<DataTable> GetDataTableAsync_rep(string SPName, SqlParameter[] para);

        Task<DataTable> GetDataTableAsync(string SPName, SqlParameter[] para,int timeout=30);

        Task<object> GetExecuteScalarQry(String Qry, SqlParameter[] para);

        Task<object> GetExecuteScalarSP_WithConStr_1SecTimeout(String Qry, SqlParameter[] para);

        Task<DataTable> GetDataTable_Time(String SPName, SqlParameter[] para, Int32 Timeout);

        Task<DataSet> GetDataSet_ReplicationDB(String SPName, SqlParameter[] para,Int32 Timeout);

        Task<DataSet> GetDataSet(String SPName, SqlParameter[] para);

        Task<DataSet> GetDataSet_Limitbalancer(String SPName, SqlParameter[] para);

        Task<DataSet> GetDataSet_cache(String SPName, SqlParameter[] para, int Timeout = 3);

        Task<object> GetExecuteScalarSP1(String SPName, SqlParameter[] para);

        Task<object> GetExecuteScalarSP_140Server(String SPName, SqlParameter[] para);

        Task<int> ExecuteNonQuery_SaveSupplierLog(string spName, SqlParameter[] para);



        Task<int> ExecuteNonQuery_timeout(string spName, SqlParameter[] para, int timeout);

        Task ExecuteNonQuery_SupplierLog_report(string spName, SqlParameter[] para);

        Task<int> ExecuteNonQuery(string spName, SqlParameter[] para);

        Task<int> ExecuteNonQuerywithconnection(string spName, SqlParameter[] para, string connectionStr);

        Task<DataTable> GetDataTable_OTHDB(string SPName, SqlParameter[] para);
        Task<DataSet> GetDataSetAsync_Limit_OTHDB(String SPName, SqlParameter[] para, CancellationToken cts, int Timeout = 2); //channel
        Task<DataSet> GetDataSetAsync_Limit_OTHDB_2(String SPName, SqlParameter[] para, CancellationToken cts, int Timeout = 2); //channel

        Task<DataSet> GetDataSetAsync_Timeout_CM(String SPName, SqlParameter[] para, CancellationToken cts, Int32 Timeout);
        Task<DataSet> GetDataSetAsync_Timeout_CM_2(String SPName, SqlParameter[] para, CancellationToken cts, Int32 Timeout);
        Task<object> GetExecuteScalarSP(String SPName, SqlParameter[] para);

        Task<DataSet> GetDataSetAsync_Derbysoft(String SPName, SqlParameter[] para, CancellationToken cts, Int32 Timeout);

        DataSet GetDataSet_Limit_CM(String SPName, SqlParameter[] para, int Timeout = 2);
        DataSet GetDataSet_Limit_CM_2(String SPName, SqlParameter[] para, int Timeout = 2);

        Task<int> ExecuteNonQuery_Logserver2(string spName, SqlParameter[] para);

        Task<object >GetExecuteScalarQry_Logserver2(String Qry, SqlParameter[] para);


        #region Recheck - 270525
        DataSet GetDataSetv1(String SPName, SqlParameter[] para);

        object GetExecuteScalarSP_Logserver2_TP(String SPName, SqlParameter[] para);

        Task<DataTable> GetDataTable_Derbysoft(string SPName, SqlParameter[] para);

        #endregion

        #region Prebook 24-06-25
        Task<object> GetExecuteScalarSP_RP1(String SPName, SqlParameter[] para);
        Task<object> GetExecuteScalarSP_RP2(String SPName, SqlParameter[] para);
        Task<object> GetExecuteScalarSP_RP3(String SPName, SqlParameter[] para);
        Task<object> GetExecuteScalarSP_RP4(String SPName, SqlParameter[] para);

        Task<DataSet> GetDataSet_OTHDB(String SPName, SqlParameter[] para);

        Task<DataSet> GetDataSet_CM(string SPName, SqlParameter[] para);
        Task<DataSet> GetDataSet_CM2(string SPName, SqlParameter[] para);
        #endregion

        Task<DataSet> GetDataSetAsync_DataReader(string SPName, SqlParameter[] para, int timeout = 2);

        Task<DataSet> GetDataSetAsync_Limit_OTHDB_2_Redis(String SPName, SqlParameter[] para, CancellationToken cts, int Timeout = 2);

        Task<int> ExecuteNonQuery_OthReplica(string spName, SqlParameter[] para);

        Task<DataTable> GetDataTable_cm(string SPName, SqlParameter[] para);

        Task<DataTable> GetDataTable_cm2(string SPName, SqlParameter[] para);

        #region MobileTeamNewVersion
        Task<DataTable> GetDataTable_staticdata(String SPName, SqlParameter[] para, Int32 Timeout);
        Task<DataSet> GetDataSet_staticdata(String SPName, SqlParameter[] para);
        #endregion MobileTeamNewVersion

        Task<DataTable> GetDataTableByConnectionStringAsync(string SPName, SqlParameter[] para, string connectionString);
        Task<object> GetExecuteScalarQry_new(string SPName, SqlParameter[] para);

        #region adoda

        //Task<DataSet> GetDataSet_cache_agoda(string SPName, SqlParameter[] para, Int32 Timeout);

        // Task<DataSet> GetExecuteScalarSP_88(String SPName, SqlParameter[] para);
        object GetExecuteScalarSP_88(String SPName, SqlParameter[] para);
        Task<DataTable> GetDataTableAsync_RATEAURA_BookingServiceT1(string SPName, SqlParameter[] para);



        #endregion
    }
}
