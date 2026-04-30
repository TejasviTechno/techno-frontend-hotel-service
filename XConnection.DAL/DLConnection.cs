using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using static Azure.Core.HttpHeader;
using System.Runtime.InteropServices;
using XConnect.DAL.Interface;
using XConnect.Models;
using Microsoft.Extensions.Options;

namespace XConnect.DAL
{
    public class DLConnection : IDLConnection
    {
        #region Old Code
        //private readonly IConfiguration _configuration;

        //public DLConnection(IConfiguration configuration)
        //{
        //    _configuration = configuration;
        //}

        //private readonly string _connectionString;
        //private readonly string _connectionString_ReplicationDB;
        //private readonly string _connectionStringBalancer;



        //// Constructor accepts the connection string
        //public DLConnection(string connectionString, string connectionString_ReplicationDB = null, string connectionStringBalancer = null)
        //{
        //    _connectionString = connectionString;
        //    _connectionString_ReplicationDB = connectionString_ReplicationDB;
        //    _connectionStringBalancer = connectionStringBalancer;
        //}
        #endregion

        private readonly ConnectionStrings _connectionStrings;

        public DLConnection(IOptions<ConnectionStrings> connectionStrings)
        {
            _connectionStrings = connectionStrings.Value;
        }

        public async Task<DataTable> GetDataTable_cm2(string SPName, SqlParameter[] para)
        {


            using (SqlConnection con = new SqlConnection(_connectionStrings.ConnectionString_cm2))
            {
                DataTable dt = new DataTable();

                using (SqlCommand cmd = new SqlCommand(SPName, con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    foreach (SqlParameter par in para)
                    {
                        cmd.Parameters.Add(par);
                    }
                    try
                    {
                        await con.OpenAsync();
                        using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                        {
                            await Task.Run(() => da.Fill(dt));  // Use Task.Run to execute the filling operation asynchronously
                        }
                    }
                    catch (SqlException ex)
                    {

                    }
                    catch (Exception ex)
                    {

                    }
                }
                return dt;

            }

        }

        public async Task<int> ExecuteNonQuery_OthReplica(string spName, SqlParameter[] para)
        {


            using (SqlConnection con = new SqlConnection(_connectionStrings.ConnectionString_OTH_Replica))
            {
                int x = 0;
                using (SqlCommand cmd = new SqlCommand(spName, con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    foreach (SqlParameter par in para)
                    {
                        cmd.Parameters.Add(par);
                    }
                    try
                    {
                        await con.OpenAsync();
                        x = await cmd.ExecuteNonQueryAsync();
                    }
                    catch (SqlException ex)
                    {

                    }
                    catch (Exception ex)
                    {

                    }
                    return x;
                }
            }
        }
        public async Task<DataTable> GetDataTable_OTHDB(string SPName, SqlParameter[] para)
        {


            using (SqlConnection con = new SqlConnection(_connectionStrings.ConnectionString_OTH_Replica))
            {
                DataTable dt = new DataTable();

                using (SqlCommand cmd = new SqlCommand(SPName, con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    foreach (SqlParameter par in para)
                    {
                        cmd.Parameters.Add(par);
                    }
                    try
                    {
                        await con.OpenAsync();
                        using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                        {
                            await Task.Run(() => da.Fill(dt));  // Use Task.Run to execute the filling operation asynchronously
                        }
                    }
                    catch (SqlException ex)
                    {

                    }
                    catch (Exception ex)
                    {

                    }
                }
                return dt;

            }

        }
        public async Task<DataSet> GetDataSet_OTHDB(string SPName, SqlParameter[] para)
        {


            using (SqlConnection con = new SqlConnection(_connectionStrings.ConnectionString_OTH_Replica))
            {
                DataSet ds = new DataSet();

                using (SqlCommand cmd = new SqlCommand(SPName, con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;


                    foreach (SqlParameter par in para)
                    {
                        cmd.Parameters.Add(par);
                    }
                    try
                    {
                        await con.OpenAsync();
                        using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                        {
                            await Task.Run(() => da.Fill(ds));  // Use Task.Run to execute the filling operation asynchronously
                        }
                    }
                    catch (SqlException ex)
                    {

                    }
                    catch (Exception ex)
                    {

                    }
                }
                return ds;
            }


        }

        public async Task<DataTable> GetDataTable_Derbysoft(string SPName, SqlParameter[] para)
        {


            using (SqlConnection con = new SqlConnection(_connectionStrings.ConnectionStringforDerbysoft))
            {
                DataTable dt = new DataTable();

                using (SqlCommand cmd = new SqlCommand(SPName, con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    foreach (SqlParameter par in para)
                    {
                        cmd.Parameters.Add(par);
                    }
                    try
                    {
                        await con.OpenAsync();
                        using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                        {
                            await Task.Run(() => da.Fill(dt));  // Use Task.Run to execute the filling operation asynchronously
                        }
                    }
                    catch (SqlException ex)
                    {

                    }
                    catch (Exception ex)
                    {

                    }
                }
                return dt;

            }

        }


        public object GetExecuteScalarSP_Logserver2_TP(String SPName, SqlParameter[] para)
        {
            using (SqlConnection con = new SqlConnection(_connectionStrings.connectionStr_Logserver2_TP))
            {
                SqlCommand cmd = new SqlCommand();
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = SPName;
                foreach (SqlParameter par in para)
                {
                    cmd.Parameters.Add(par);
                }
                cmd.Connection = con;
                con.Open();
                object obj = cmd.ExecuteScalar();
                con.Close();
                con.Dispose();
                return obj;
            }
        }

        public DataSet GetDataSetv1(String SPName, SqlParameter[] para)
        {
            using (SqlConnection con = new SqlConnection(_connectionStrings.ConnectionString))
            {
                SqlCommand cmd = new SqlCommand();
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = SPName;
                foreach (SqlParameter par in para)
                {
                    cmd.Parameters.Add(par);
                }
                cmd.Connection = con;
                cmd.CommandTimeout = 600;
                con.Open();
                DataSet ds = new DataSet();
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                da.Fill(ds);
                con.Close();
                con.Dispose();
                return ds;
            }
        }

        public async Task<DataTable> GetDataTableAsync(string SPName, SqlParameter[] para)
        {


            using (SqlConnection con = new SqlConnection(_connectionStrings.ConnectionString))
            {
                DataTable dt = new DataTable();

                using (SqlCommand cmd = new SqlCommand(SPName, con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    foreach (SqlParameter par in para)
                    {
                        cmd.Parameters.Add(par);
                    }
                    try
                    {
                        await con.OpenAsync();
                        using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                        {
                            await Task.Run(() => da.Fill(dt));  // Use Task.Run to execute the filling operation asynchronously
                        }
                    }
                    catch (SqlException ex)
                    {

                    }
                    catch (Exception ex)
                    {

                    }
                }
                return dt;

            }

        }
        public async Task<DataTable> GetDataTableAsync_rep(string SPName, SqlParameter[] para)
        {


            using (SqlConnection con = new SqlConnection(_connectionStrings.ConnectionString_ReplicationDB))
            {
                DataTable dt = new DataTable();

                using (SqlCommand cmd = new SqlCommand(SPName, con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    foreach (SqlParameter par in para)
                    {
                        cmd.Parameters.Add(par);
                    }
                    try
                    {
                        await con.OpenAsync();
                        using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                        {
                            await Task.Run(() => da.Fill(dt));  // Use Task.Run to execute the filling operation asynchronously
                        }
                    }
                    catch (SqlException ex)
                    {

                    }
                    catch (Exception ex)
                    {

                    }
                }
                return dt;

            }

        }

        public async Task<DataTable> GetDataTable_cm(string SPName, SqlParameter[] para)
        {


            using (SqlConnection con = new SqlConnection(_connectionStrings.ConnectionString_cm))
            {
                DataTable dt = new DataTable();

                using (SqlCommand cmd = new SqlCommand(SPName, con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    foreach (SqlParameter par in para)
                    {
                        cmd.Parameters.Add(par);
                    }
                    try
                    {
                        await con.OpenAsync();
                        using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                        {
                            await Task.Run(() => da.Fill(dt));  // Use Task.Run to execute the filling operation asynchronously
                        }
                    }
                    catch (SqlException ex)
                    {

                    }
                    catch (Exception ex)
                    {

                    }
                }
                return dt;

            }

        }
        public DataTable GetDataTable(String SPName, SqlParameter[] para)
        {
            using (SqlConnection con = new SqlConnection(_connectionStrings.ConnectionString))
            {
                SqlCommand cmd = new SqlCommand();
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = SPName;

                foreach (SqlParameter par in para)
                {
                    cmd.Parameters.Add(par);
                }
                cmd.Connection = con;
                con.Open();
                cmd.CommandTimeout = 2000;
                DataTable dt = new DataTable();
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                da.Fill(dt);
                con.Close();
                con.Dispose();
                return dt;
            }
        }

        public DataSet GetDataSet_Limit_CM(String SPName, SqlParameter[] para, int Timeout = 2)
        {
            using (SqlConnection con = new SqlConnection(_connectionStrings.ConnectionString_cm))
            {
                SqlCommand cmd = new SqlCommand();
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = SPName;
                foreach (SqlParameter par in para)
                {
                    cmd.Parameters.Add(par);
                }
                cmd.Connection = con;
                cmd.CommandTimeout = Timeout;
                con.Open();
                DataSet ds = new DataSet();
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                da.Fill(ds);
                con.Close();
                con.Dispose();
                return ds;
            }
        }

        public DataSet GetDataSet_Limit_CM_2(String SPName, SqlParameter[] para, int Timeout = 2)
        {
            using (SqlConnection con = new SqlConnection(_connectionStrings.ConnectionString_cm2))
            {
                SqlCommand cmd = new SqlCommand();
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = SPName;
                foreach (SqlParameter par in para)
                {
                    cmd.Parameters.Add(par);
                }
                cmd.Connection = con;
                cmd.CommandTimeout = Timeout;
                con.Open();
                DataSet ds = new DataSet();
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                da.Fill(ds);
                con.Close();
                con.Dispose();
                return ds;
            }
        }
        public Task<DataSet> GetDataSetAsync_Derbysoft(String SPName, SqlParameter[] para, CancellationToken cts, Int32 Timeout)
        {
            //Timeout = Timeout / 1000;
            //if (Timeout > 5)
            //    Timeout = 2;
            return Task.Run(() =>
            {
                using (SqlConnection con = new SqlConnection(_connectionStrings.ConnectionStringforDerbysoft))
                {
                    cts.ThrowIfCancellationRequested();
                    SqlCommand cmd = new SqlCommand();
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandText = SPName;
                    foreach (SqlParameter par in para)
                    {
                        cmd.Parameters.Add(par);
                    }
                    cmd.Connection = con;
                    cmd.CommandTimeout = Timeout;
                    con.Open();
                    DataSet ds = new DataSet();
                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    da.Fill(ds);
                    con.Close();
                    con.Dispose();
                    return ds;
                }
            }, cts);

        }

        public async Task<object> GetExecuteScalarSP(String SPName, SqlParameter[] para)
        {
            //using (SqlConnection con = new SqlConnection(_connectionStrings.ConnectionString))
            using (SqlConnection con = new SqlConnection(_connectionStrings.ConnectionStringforlogtrack_APILogVM))//020426
            {
                SqlCommand cmd = new SqlCommand();
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = SPName;
                foreach (SqlParameter par in para)
                {
                    cmd.Parameters.Add(par);
                }
                cmd.Connection = con;
                con.Open();
                object obj = cmd.ExecuteScalar();
                con.Close();
                con.Dispose();
                return obj;
            }
        }

        public async Task<object> GetExecuteScalarSP_RP1(String SPName, SqlParameter[] para)
        {
            using (SqlConnection con = new SqlConnection(_connectionStrings.ConnectionString_ReplicationDB1))
            {
                SqlCommand cmd = new SqlCommand();
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = SPName;
                foreach (SqlParameter par in para)
                {
                    cmd.Parameters.Add(par);
                }
                cmd.Connection = con;
                con.Open();
                object obj = cmd.ExecuteScalar();
                con.Close();
                con.Dispose();
                return obj;
            }
        }

        public async Task<object> GetExecuteScalarSP_RP2(String SPName, SqlParameter[] para)
        {
            using (SqlConnection con = new SqlConnection(_connectionStrings.ConnectionString_ReplicationDB2))
            {
                SqlCommand cmd = new SqlCommand();
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = SPName;
                foreach (SqlParameter par in para)
                {
                    cmd.Parameters.Add(par);
                }
                cmd.Connection = con;
                con.Open();
                object obj = cmd.ExecuteScalar();
                con.Close();
                con.Dispose();
                return obj;
            }
        }

        public async Task<object> GetExecuteScalarSP_RP3(String SPName, SqlParameter[] para)
        {
            using (SqlConnection con = new SqlConnection(_connectionStrings.ConnectionString_ReplicationDB3))
            {
                SqlCommand cmd = new SqlCommand();
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = SPName;
                foreach (SqlParameter par in para)
                {
                    cmd.Parameters.Add(par);
                }
                cmd.Connection = con;
                con.Open();
                object obj = cmd.ExecuteScalar();
                con.Close();
                con.Dispose();
                return obj;
            }
        }

        public async Task<object> GetExecuteScalarSP_RP4(String SPName, SqlParameter[] para)
        {
            using (SqlConnection con = new SqlConnection(_connectionStrings.ConnectionString_ReplicationDB4))
            {
                SqlCommand cmd = new SqlCommand();
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = SPName;
                foreach (SqlParameter par in para)
                {
                    cmd.Parameters.Add(par);
                }
                cmd.Connection = con;
                con.Open();
                object obj = cmd.ExecuteScalar();
                con.Close();
                con.Dispose();
                return obj;
            }
        }

        public Task<DataSet> GetDataSetAsync_Timeout_CM(String SPName, SqlParameter[] para, CancellationToken cts, Int32 Timeout)
        {
            Timeout = Timeout / 1000;
            if (Timeout > 4)
                Timeout = 4;
            return Task.Run(() =>
            {
                using (SqlConnection con = new SqlConnection(_connectionStrings.ConnectionString_cm))

                {
                    cts.ThrowIfCancellationRequested();
                    SqlCommand cmd = new SqlCommand();
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandText = SPName;
                    foreach (SqlParameter par in para)
                    {
                        cmd.Parameters.Add(par);
                    }
                    cmd.Connection = con;
                    cmd.CommandTimeout = Timeout;
                    con.Open();
                    DataSet ds = new DataSet();
                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    da.Fill(ds);
                    con.Close();
                    con.Dispose();
                    return ds;
                }
            }, cts);

        }

        public async Task<DataSet> GetDataSet_CM(string SPName, SqlParameter[] para)
        {
            using (SqlConnection con = new SqlConnection(_connectionStrings.ConnectionString_cm))
            {
                using (SqlCommand cmd = new SqlCommand(SPName, con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    if (para != null)
                    {
                        cmd.Parameters.AddRange(para);
                    }

                    await con.OpenAsync();

                    return await Task.Run(() =>
                    {
                        using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                        {
                            DataSet ds = new DataSet();
                            da.Fill(ds);
                            return ds;
                        }
                    });
                }
            }
        }

        public async Task<DataSet> GetDataSet_CM2(string SPName, SqlParameter[] para)
        {
            using (SqlConnection con = new SqlConnection(_connectionStrings.ConnectionString_cm2))
            {
                using (SqlCommand cmd = new SqlCommand(SPName, con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    if (para != null)
                    {
                        cmd.Parameters.AddRange(para);
                    }

                    await con.OpenAsync();

                    return await Task.Run(() =>
                    {
                        using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                        {
                            DataSet ds = new DataSet();
                            da.Fill(ds);
                            return ds;
                        }
                    });
                }
            }
        }


        public Task<DataSet> GetDataSetAsync_Timeout_CM_2(String SPName, SqlParameter[] para, CancellationToken cts, Int32 Timeout)
        {
            Timeout = Timeout / 1000;
            if (Timeout > 4)
                Timeout = 4;
            return Task.Run(() =>
            {
                using (SqlConnection con = new SqlConnection(_connectionStrings.ConnectionString_cm2))

                {
                    cts.ThrowIfCancellationRequested();
                    SqlCommand cmd = new SqlCommand();
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandText = SPName;
                    foreach (SqlParameter par in para)
                    {
                        cmd.Parameters.Add(par);
                    }
                    cmd.Connection = con;
                    cmd.CommandTimeout = Timeout;
                    con.Open();
                    DataSet ds = new DataSet();
                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    da.Fill(ds);
                    con.Close();
                    con.Dispose();
                    return ds;
                }
            }, cts);

        }
        public async Task<DataSet> GetDataSetAsync_Limit_OTHDB(String SPName, SqlParameter[] para, CancellationToken cts, int Timeout = 2)
        {
            Timeout = Timeout / 1000;
            if (Timeout > 4)
                Timeout = 4;
            // return Task.Run(() =>
            {
                //using (SqlConnection con = new SqlConnection(connectionStr_OTHDB))
                using (SqlConnection con = new SqlConnection(_connectionStrings.ConnectionString_OTH_Replica))

                {
                    cts.ThrowIfCancellationRequested();
                    SqlCommand cmd = new SqlCommand();
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandText = SPName;
                    foreach (SqlParameter par in para)
                    {
                        cmd.Parameters.Add(par);
                    }
                    cmd.Connection = con;
                    cmd.CommandTimeout = Timeout;
                    con.Open();
                    DataSet ds = new DataSet();
                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    da.Fill(ds);
                    con.Close();
                    con.Dispose();
                    return ds;
                }
            }
            //, cts);

        }
        public async Task<DataSet> GetDataSetAsync_Limit_OTHDB_2(String SPName, SqlParameter[] para, CancellationToken cts, int Timeout = 2)
        {
            Timeout = Timeout / 1000;
            if (Timeout > 4)
                Timeout = 4;
            // return Task.Run(() =>
            {
                //using (SqlConnection con = new SqlConnection(connectionStr_OTHDB))
                using (SqlConnection con = new SqlConnection(_connectionStrings.ConnectionString_OTH2_Replica))

                {
                    cts.ThrowIfCancellationRequested();
                    SqlCommand cmd = new SqlCommand();
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandText = SPName;
                    foreach (SqlParameter par in para)
                    {
                        cmd.Parameters.Add(par);
                    }
                    cmd.Connection = con;
                    cmd.CommandTimeout = Timeout;
                    con.Open();
                    DataSet ds = new DataSet();
                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    da.Fill(ds);
                    con.Close();
                    con.Dispose();
                    return ds;
                }
            }
            //, cts);

        }

        public async Task<DataSet> GetDataSet_cache(string SPName, SqlParameter[] para, Int32 Timeout)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(_connectionStrings.CacheConnectionString))
                {
                    DataSet ds = new DataSet();
                    using (SqlCommand cmd = new SqlCommand(SPName, con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.CommandTimeout = Timeout;

                        foreach (SqlParameter par in para)
                        {
                            cmd.Parameters.Add(par);
                        }
                        try
                        {
                            await con.OpenAsync();
                            using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                            {
                                await Task.Run(() => da.Fill(ds));  // Use Task.Run to execute the filling operation asynchronously
                            }
                        }
                        catch (SqlException ex)
                        {

                        }
                        catch (Exception ex)
                        {

                        }
                    }

                    return ds;
                }

            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("Invalid object"))
                {
                    using (SqlConnection con = new SqlConnection(_connectionStrings.ConnectionString_ReplicationDB))
                    {
                        DataSet ds = new DataSet();

                        using (SqlCommand cmd = new SqlCommand(SPName, con))
                        {
                            cmd.CommandType = CommandType.StoredProcedure;
                            cmd.CommandTimeout = Timeout;

                            foreach (SqlParameter par in para)
                            {
                                cmd.Parameters.Add(par);
                            }
                            try
                            {
                                await con.OpenAsync();
                                using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                                {
                                    await Task.Run(() => da.Fill(ds));  // Use Task.Run to execute the filling operation asynchronously
                                }
                            }
                            catch (SqlException ex1)
                            {

                            }
                            catch (Exception ex2)
                            {
                                //Common.Error(x, "", "GetDataSet_cache_ex2");
                            }
                        }

                        return ds;
                    }
                }

            }
            //using (SqlConnection con = new SqlConnection(_connectionString_ReplicationDB))

            #region ExParaSave
            string ParaStr = "";
            try
            {
                for (int i = 0; i < para.Length; i++)
                {
                    if (i == 0)
                        ParaStr = para[i].ParameterName + "=" + Convert.ToString(para[i].Value);
                    else
                        ParaStr = ParaStr + "," + para[i].ParameterName + "=" + Convert.ToString(para[i].Value);

                }
            }
            catch (Exception)
            {

            }
            string Str = "exec " + SPName + " " + ParaStr;
            //Common.Error(ex, "Timeout=" + Timeout + "", Str);
            #endregion

            return null;

        }

        public async Task<DataSet> GetDataSet(string SPName, SqlParameter[] para)
        {


            using (SqlConnection con = new SqlConnection(_connectionStrings.ConnectionString))
            {
                DataSet ds = new DataSet();

                using (SqlCommand cmd = new SqlCommand(SPName, con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;


                    foreach (SqlParameter par in para)
                    {
                        cmd.Parameters.Add(par);
                    }
                    try
                    {
                        await con.OpenAsync();
                        using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                        {
                            await Task.Run(() => da.Fill(ds));  // Use Task.Run to execute the filling operation asynchronously
                        }
                    }
                    catch (SqlException ex)
                    {

                    }
                    catch (Exception ex)
                    {

                    }
                }
                return ds;
            }


        }

        public async Task<DataSet> GetDataSet_Limit(string SPName, SqlParameter[] para, int Timeout = 2)
        {

            using (SqlConnection con = new SqlConnection(_connectionStrings.ConnectionString))
            {
                DataSet ds = new DataSet();

                using (SqlCommand cmd = new SqlCommand(SPName, con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandTimeout = Timeout;

                    foreach (SqlParameter par in para)
                    {
                        cmd.Parameters.Add(par);
                    }
                    try
                    {
                        await con.OpenAsync();
                        using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                        {
                            await Task.Run(() => da.Fill(ds));  // Use Task.Run to execute the filling operation asynchronously
                        }
                    }
                    catch (SqlException ex)
                    {

                    }
                    catch (Exception ex)
                    {

                    }
                }
                return ds;
            }


        }

        public async Task<DataSet> GetDataSet_Limitbalancer(string SPName, SqlParameter[] para)
        {


            using (SqlConnection con = new SqlConnection(_connectionStrings.ConnectionStringBalancer))
            {
                DataSet ds = new DataSet();
                using (SqlCommand cmd = new SqlCommand(SPName, con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    foreach (SqlParameter par in para)
                    {
                        cmd.Parameters.Add(par);
                    }
                    try
                    {
                        await con.OpenAsync();
                        using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                        {
                            await Task.Run(() => da.Fill(ds));  // Use Task.Run to execute the filling operation asynchronously
                        }
                    }
                    catch (SqlException ex)
                    {

                    }
                    catch (Exception ex)
                    {

                    }
                }
                return ds;
            }


        }

        public async Task<DataSet> GetDataSet_ReplicationDB(string SPName, SqlParameter[] para, Int32 Timeout)
        {


            //using (SqlConnection con = new SqlConnection(_connectionString_ReplicationDB))
            using (SqlConnection con = new SqlConnection(_connectionStrings.ConnectionString_ReplicationDB))
            {
                DataSet ds = new DataSet();

                using (SqlCommand cmd = new SqlCommand(SPName, con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandTimeout = Timeout;

                    foreach (SqlParameter par in para)
                    {
                        cmd.Parameters.Add(par);
                    }
                    try
                    {
                        await con.OpenAsync();
                        using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                        {
                            await Task.Run(() => da.Fill(ds));  // Use Task.Run to execute the filling operation asynchronously
                        }
                    }
                    catch (SqlException ex)
                    {

                    }
                    catch (Exception ex)
                    {

                    }
                }
                return ds;
            }


        }
        public async Task<DataSet> GetDataSet_ReplicationDB_Host(string SPName, SqlParameter[] para, Int32 Timeout)
        {


            //using (SqlConnection con = new SqlConnection(_connectionString_ReplicationDB))
            using (SqlConnection con = new SqlConnection(_connectionStrings.ConnectionString_staticData))
            {
                DataSet ds = new DataSet();

                using (SqlCommand cmd = new SqlCommand(SPName, con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandTimeout = Timeout;

                    foreach (SqlParameter par in para)
                    {
                        cmd.Parameters.Add(par);
                    }
                    try
                    {
                        await con.OpenAsync();
                        using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                        {
                            await Task.Run(() => da.Fill(ds));  // Use Task.Run to execute the filling operation asynchronously
                        }
                    }
                    catch (SqlException ex)
                    {

                    }
                    catch (Exception ex)
                    {

                    }
                }
                return ds;
            }


        }

        public async Task<DataTable> GetDataTableAsync(string SPName)
        {
            using (SqlConnection con = new SqlConnection(_connectionStrings.ConnectionString))
            using (SqlCommand cmd = new SqlCommand(SPName, con))
            {
                cmd.CommandType = CommandType.StoredProcedure;

                await con.OpenAsync(); // Open connection asynchronously

                DataTable dt = new DataTable();
                using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                {
                    da.Fill(dt); // SqlDataAdapter.Fill() is synchronous
                }

                return dt;
            }
        }

        public async Task<DataTable> GetDataTable_Replica(string SPName, SqlParameter[] para, int Timeout)
        {

            using (SqlConnection con = new SqlConnection(_connectionStrings.ConnectionString_ReplicationDB))
            {
                DataTable dt = new DataTable();

                using (SqlCommand cmd = new SqlCommand(SPName, con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandTimeout = Timeout;

                    foreach (SqlParameter par in para)
                    {
                        cmd.Parameters.Add(par);
                    }
                    try
                    {
                        await con.OpenAsync();
                        using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                        {
                            await Task.Run(() => da.Fill(dt));  // Use Task.Run to execute the filling operation asynchronously
                        }
                    }
                    catch (SqlException ex)
                    {

                    }
                    catch (Exception ex)
                    {

                    }
                }
                return dt;

            }

        }

        public async Task<DataTable> GetDataTableAsync(string SPName, SqlParameter[] para,int timeout=30)
        {


            using (SqlConnection con = new SqlConnection(_connectionStrings.ConnectionString))
            {
                DataTable dt = new DataTable();

                using (SqlCommand cmd = new SqlCommand(SPName, con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandTimeout = timeout;

                    foreach (SqlParameter par in para)
                    {
                        cmd.Parameters.Add(par);
                    }
                    try
                    {
                        await con.OpenAsync();
                        using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                        {
                            await Task.Run(() => da.Fill(dt));  // Use Task.Run to execute the filling operation asynchronously
                        }
                    }
                    catch (SqlException ex)
                    {

                    }
                    catch (Exception ex)
                    {

                    }
                }
                return dt;

            }

        }

        public async Task<DataTable> GetDataTable_Time(string SPName, SqlParameter[] para, int Timeout)
        {

            using (SqlConnection con = new SqlConnection(_connectionStrings.ConnectionString))
            {
                DataTable dt = new DataTable();

                using (SqlCommand cmd = new SqlCommand(SPName, con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandTimeout = Timeout;

                    foreach (SqlParameter par in para)
                    {
                        cmd.Parameters.Add(par);
                    }
                    try
                    {
                        await con.OpenAsync();
                        using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                        {
                            await Task.Run(() => da.Fill(dt));  // Use Task.Run to execute the filling operation asynchronously
                        }
                    }
                    catch (SqlException ex)
                    {

                    }
                    catch (Exception ex)
                    {

                    }
                }
                return dt;

            }

        }

        public async Task<object> GetExecuteScalarQry(string Qry, SqlParameter[] para)
        {

            try
            {
                using (SqlConnection con = new SqlConnection(_connectionStrings.ConnectionString))
                {
                    object obj = null;

                    using (SqlCommand cmd = new SqlCommand(Qry, con))
                    {
                        cmd.CommandType = CommandType.Text;

                        foreach (SqlParameter par in para)
                        {
                            cmd.Parameters.Add(par);
                        }

                        await con.OpenAsync();

                        obj = await cmd.ExecuteScalarAsync();
                        return obj;
                    }
                }
            }
            catch (SqlException ex)
            {
                return null;
            }
            catch (Exception ex)
            {
                return null;
            }


        }



        public async Task<object> GetExecuteScalarSP_WithConStr_1SecTimeout(string Qry, SqlParameter[] para)
        {

            try
            {
                //using (SqlConnection con = new SqlConnection(_connectionStrings.ConnectionStringforlogtrack))
                using (SqlConnection con = new SqlConnection(_connectionStrings.ConnectionStringforlogtrack_180VM))//Added on 16 OCT 2025
                {
                    object obj = null;

                    using (SqlCommand cmd = new SqlCommand(Qry, con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        foreach (SqlParameter par in para)
                        {
                            cmd.Parameters.Add(par);
                        }

                        await con.OpenAsync();

                        obj = await cmd.ExecuteScalarAsync();
                        return obj;
                    }
                }
            }
            catch (SqlException ex)
            {
                return null;
            }
            catch (Exception ex)
            {
                return null;
            }


        }

        public async Task<object> GetExecuteScalarSP_WithConStr_1SecTimeout_Log(string Qry, SqlParameter[] para)
        {

            try
            {
                //using (SqlConnection con = new SqlConnection(_connectionStrings.ConnectionStringforlogtrack))
                using (SqlConnection con = new SqlConnection(_connectionStrings.ConnectionStringforlog_180VM))//Added on 16 OCT 2025
                {
                    object obj = null;

                    using (SqlCommand cmd = new SqlCommand(Qry, con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        foreach (SqlParameter par in para)
                        {
                            cmd.Parameters.Add(par);
                        }

                        await con.OpenAsync();

                        obj = await cmd.ExecuteScalarAsync();
                        return obj;
                    }
                }
            }
            catch (SqlException ex)
            {
                return null;
            }
            catch (Exception ex)
            {
                return null;
            }


        }

        public async Task<object> GetExecuteScalarSP1(string SPName, SqlParameter[] para)
        {


            using (SqlConnection con = new SqlConnection(_connectionStrings.ConnectionStringforlogtrack))
            {
                object obj = null;
                using (SqlCommand cmd = new SqlCommand(SPName, con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    foreach (SqlParameter par in para)
                    {
                        cmd.Parameters.Add(par);
                    }
                    try
                    {
                        await con.OpenAsync();
                        obj = await cmd.ExecuteScalarAsync();
                    }
                    catch (SqlException ex)
                    {

                    }
                    catch (Exception ex)
                    {

                    }
                    return obj;
                }
            }
        }

        public async Task<object> GetExecuteScalarSP_140Server(string SPName, SqlParameter[] para)
        {

            using (SqlConnection con = new SqlConnection(_connectionStrings.ConnectionString_SaveSupplierLog))
            {
                object obj = null;

                using (SqlCommand cmd = new SqlCommand(SPName, con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    foreach (SqlParameter par in para)
                    {
                        cmd.Parameters.Add(par);
                    }
                    try
                    {
                        await con.OpenAsync();
                        obj = await cmd.ExecuteScalarAsync();
                    }
                    catch (SqlException ex)
                    {

                    }
                    catch (Exception ex)
                    {

                    }
                    return obj;
                }
            }
        }

        public async Task<int> ExecuteNonQuery_SaveSupplierLog(string spName, SqlParameter[] para)
        {


            using (SqlConnection con = new SqlConnection(_connectionStrings.ConnectionString_SaveSupplierLog))
            {
                int x = 0;
                using (SqlCommand cmd = new SqlCommand(spName, con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    foreach (SqlParameter par in para)
                    {
                        cmd.Parameters.Add(par);
                    }
                    try
                    {
                        await con.OpenAsync();
                        x = await cmd.ExecuteNonQueryAsync();
                    }
                    catch (SqlException ex)
                    {

                    }
                    catch (Exception ex)
                    {

                    }
                    return x;
                }
            }
        }

        public async Task<int> ExecuteNonQuerywithconnection(string spName, SqlParameter[] para, string connectionStr)
        {
            using (SqlConnection con = new SqlConnection(connectionStr))
            {
                int x = 0;
                using (SqlCommand cmd = new SqlCommand(spName, con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    foreach (SqlParameter par in para)
                    {
                        cmd.Parameters.Add(par);
                    }
                    try
                    {
                        await con.OpenAsync();
                        x = await cmd.ExecuteNonQueryAsync();
                    }
                    catch (SqlException ex)
                    {

                    }
                    catch (Exception ex)
                    {

                    }
                    return x;
                }
            }
        }

        public async Task<int> ExecuteNonQuery_timeout(string spName, SqlParameter[] para, int timeout)
        {


            using (SqlConnection con = new SqlConnection(_connectionStrings.ConnectionString))
            {
                int x = 0;

                using (SqlCommand cmd = new SqlCommand(spName, con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandTimeout = timeout;

                    foreach (SqlParameter par in para)
                    {
                        cmd.Parameters.Add(par);
                    }
                    try
                    {
                        await con.OpenAsync();
                        x = await cmd.ExecuteNonQueryAsync();
                    }
                    catch (SqlException ex)
                    {

                    }
                    catch (Exception ex)
                    {

                    }
                    return x;
                }
            }
        }

        public async Task<int> ExecuteNonQuery(string spName, SqlParameter[] para)
        {

            using (SqlConnection con = new SqlConnection(_connectionStrings.ConnectionString))
            {
                int x = 0;


                using (SqlCommand cmd = new SqlCommand(spName, con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    foreach (SqlParameter par in para)
                    {
                        cmd.Parameters.Add(par);
                    }
                    try
                    {
                        await con.OpenAsync();
                        x = await cmd.ExecuteNonQueryAsync();
                    }
                    catch (SqlException ex)
                    {

                    }
                    catch (Exception ex)
                    {

                    }
                    return x;
                }
            }
        }

        public async Task ExecuteNonQuery_SupplierLog_report(string spName, SqlParameter[] para)
        {

            using (SqlConnection con = new SqlConnection(_connectionStrings.ConnectionString_SupplierLog_report))
            {
                using (SqlCommand cmd = new SqlCommand(spName, con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;


                    foreach (SqlParameter par in para)
                    {
                        cmd.Parameters.Add(par);
                    }
                    try
                    {
                        await con.OpenAsync();
                        await cmd.ExecuteNonQueryAsync();
                    }
                    catch (SqlException ex)
                    {

                    }
                    catch (Exception ex)
                    {

                    }
                }
            }
        }

        public async Task<int> ExecuteNonQuery_Logserver2(string spName, SqlParameter[] para)
        {

            using (SqlConnection con = new SqlConnection(_connectionStrings.connectionStr_Logserver2))
            {
                int x = 0;


                using (SqlCommand cmd = new SqlCommand(spName, con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    foreach (SqlParameter par in para)
                    {
                        cmd.Parameters.Add(par);
                    }
                    try
                    {
                        await con.OpenAsync();
                        x = await cmd.ExecuteNonQueryAsync();
                    }
                    catch (SqlException ex)
                    {

                    }
                    catch (Exception ex)
                    {

                    }
                    return x;
                }
            }
        }

        public async Task<object> GetExecuteScalarQry_Logserver2(string Qry, SqlParameter[] para)
        {

            try
            {
                using (SqlConnection con = new SqlConnection(_connectionStrings.connectionStr_Logserver2))
                {
                    object obj = null;

                    using (SqlCommand cmd = new SqlCommand(Qry, con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        foreach (SqlParameter par in para)
                        {
                            cmd.Parameters.Add(par);
                        }

                        await con.OpenAsync();

                        obj = await cmd.ExecuteScalarAsync();
                        return obj;
                    }
                }
            }
            catch (SqlException ex)
            {
                return null;
            }
            catch (Exception ex)
            {
                return null;
            }


        }

        public async Task<DataSet> GetDataSetAsync_DataReader(string SPName, SqlParameter[] para, int Timeout = 2)
        {
            using (SqlConnection con = new SqlConnection(_connectionStrings.ConnectionString))
            using (SqlCommand cmd = new SqlCommand(SPName, con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandTimeout = Timeout;

                if (para != null)
                {
                    cmd.Parameters.AddRange(para);
                }

                await con.OpenAsync();

                using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                {
                    DataSet ds = new DataSet();
                    ds.Load(reader, LoadOption.PreserveChanges, new string[] { "Table1", "Table2" , "Table3", "Table4" }); // Adjust table names if needed
                    return ds;
                }
            }
        }

        public async Task<DataSet> GetDataSetAsync_Limit_OTHDB_2_Redis(String SPName, SqlParameter[] para, CancellationToken cts, int Timeout = 2)
        {
            using (SqlConnection con = new SqlConnection(_connectionStrings.ConnectionString_OTH2_Replica))
            {
                cts.ThrowIfCancellationRequested();
                SqlCommand cmd = new SqlCommand();
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = SPName;
                foreach (SqlParameter par in para)
                {
                    cmd.Parameters.Add(par);
                }
                cmd.Connection = con;
                cmd.CommandTimeout = Timeout;
                await con.OpenAsync(cts);
                DataSet ds = new DataSet();
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                da.Fill(ds);
                con.Close();
                con.Dispose();
                return ds;
            }
        }

        #region MobileTeamNewVersion
        public async Task<DataSet> GetDataSet_staticdata(string SPName, SqlParameter[] para)
        {


            using (SqlConnection con = new SqlConnection(_connectionStrings.ConnectionString_staticData))
            {
                DataSet ds = new DataSet();

                using (SqlCommand cmd = new SqlCommand(SPName, con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;


                    foreach (SqlParameter par in para)
                    {
                        cmd.Parameters.Add(par);
                    }
                    try
                    {
                        await con.OpenAsync();
                        using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                        {
                            await Task.Run(() => da.Fill(ds));  // Use Task.Run to execute the filling operation asynchronously
                        }
                    }
                    catch (SqlException ex)
                    {

                    }
                    catch (Exception ex)
                    {

                    }
                }
                return ds;
            }


        }

        public async Task<DataTable> GetDataTable_staticdata(string SPName, SqlParameter[] para, int Timeout)
        {

            using (SqlConnection con = new SqlConnection(_connectionStrings.ConnectionString_staticData))
            {
                DataTable dt = new DataTable();

                using (SqlCommand cmd = new SqlCommand(SPName, con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandTimeout = Timeout;

                    foreach (SqlParameter par in para)
                    {
                        cmd.Parameters.Add(par);
                    }
                    try
                    {
                        await con.OpenAsync();
                        using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                        {
                            await Task.Run(() => da.Fill(dt));  // Use Task.Run to execute the filling operation asynchronously
                        }
                    }
                    catch (SqlException ex)
                    {

                    }
                    catch (Exception ex)
                    {

                    }
                }
                return dt;

            }

        }
        #endregion MobileTeamNewVersion

        public async Task<object> GetExecuteScalarQry_new(string Qry, SqlParameter[] para)
        {

            try
            {
                using (SqlConnection con = new SqlConnection(_connectionStrings.ConnectionString))
                {
                    object obj = null;

                    using (SqlCommand cmd = new SqlCommand(Qry, con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        foreach (SqlParameter par in para)
                        {
                            cmd.Parameters.Add(par);
                        }

                        await con.OpenAsync();

                        obj = await cmd.ExecuteScalarAsync();
                        return obj;
                    }
                }
            }
            catch (SqlException ex)
            {
                return null;
            }
            catch (Exception ex)
            {
                return null;
            }


        }
        public async Task<DataTable> GetDataTableByConnectionStringAsync(string SPName, SqlParameter[] para, string connectionString)
        {
            DataTable dt = new DataTable();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(SPName, conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    if (para != null && para.Length > 0)
                    {
                        cmd.Parameters.AddRange(para);
                    }

                    await conn.OpenAsync();

                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        dt.Load(reader);
                    }
                }
            }

            return dt;
        }

        #region agoda


        public object GetExecuteScalarSP_88(String SPName, SqlParameter[] para)
        {
            using (SqlConnection con = new SqlConnection(_connectionStrings.ConnectionStringforlogtrack))
            {
                SqlCommand cmd = new SqlCommand();
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = SPName;
                foreach (SqlParameter par in para)
                {
                    cmd.Parameters.Add(par);
                }
                cmd.Connection = con;
                con.Open();
                object obj = cmd.ExecuteScalar();
                con.Close();
                con.Dispose();
                return obj;
            }
        }

        #endregion
    }
}
