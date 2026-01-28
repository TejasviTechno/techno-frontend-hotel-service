using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using XConnect.DAL;
using XConnect.DAL.Interface;
using XConnect.ErrorLogHandler.Interface;
using XConnect.Models;
using static Azure.Core.HttpHeader;


namespace XConnect.ErrorLogHandler
{
    public class ExceptionLog : IException
    {
        private readonly IDLConnection _dLConnection;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<ExceptionLog> _logger;


        public ExceptionLog(IDLConnection dLConnection, IHttpContextAccessor httpContextAccessor, ILogger<ExceptionLog> logger)
        {
            _dLConnection = dLConnection;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        private string GetCollectionName(int apiName)
        {
            return apiName switch
            {
                1 => "Errorlog_AV",    // Availability
                2 => "Errorlog_RE",    // Recheck / CancellationPolicy
                3 => "Errorlog_PR",    // Prebook
                4 => "Errorlog_BO",    // Book / BookingDetail
                5 => "Errorlog_CA",    // CancelBooking / CheckHotelCancellationCharges
                _ => "Errorlog_OT"     // Other
            };
        }

        public void ErrorLog(string sPathName, string sErrMsg)
        {
            string sLogFormat;
            string sErrorTime;
            //sLogFormat used to create log files format :
            // dd/mm/yyyy hh:mm:ss AM/PM ==> Log Message
            sLogFormat = DateTime.Now.ToShortDateString().ToString() + " " + DateTime.Now.ToLongTimeString().ToString() + " ==> ";

            //this variable used to create log filename format "
            //for example filename : ErrorLogYYYYMMDD
            string sYear = DateTime.Now.Year.ToString();
            string sMonth = DateTime.Now.Month.ToString();
            string sDay = DateTime.Now.Day.ToString();
            sErrorTime = sYear + sMonth + sDay;
            StreamWriter sw = new StreamWriter(sPathName + sErrorTime, true);
            sw.WriteLine(sLogFormat + sErrMsg);
            sw.Flush();
            sw.Close();
            sw = null;
        }

        public async Task ErrorLogAsync(string sPathName, string sErrMsg)
        {
            string sLogFormat = DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToLongTimeString() + " ==> ";
            string sErrorTime = DateTime.Now.ToString("yyyyMMdd"); // Simplified date format

            string fullPath = Path.Combine(sPathName, $"ErrorLog{sErrorTime}.log");

            try
            {
                using (StreamWriter sw = new StreamWriter(fullPath, append: true))
                {
                    await sw.WriteLineAsync(sLogFormat + sErrMsg);
                }
            }
            catch (Exception ex)
            {
                // Optional: handle logging failure
                // For example: Console.WriteLine("Logging failed: " + ex.Message);
            }
        }


        public async Task Error(Exception ex, string SearchKey = "", string Info = "")
        {
            int lineNumber = 0;

            try
            {
                StackTrace st = new StackTrace(ex, true);
                StackFrame frame = st.GetFrame(0);
                lineNumber = frame.GetFileLineNumber();
                //string fileName = frame.GetFileName();
            }
            catch (Exception)
            {

            }
            Info = Convert.ToString(lineNumber);

            System.Diagnostics.StackTrace trace = new System.Diagnostics.StackTrace(ex, true);
            System.Diagnostics.StackFrame stackFrame = trace.GetFrame(trace.FrameCount - 1);
            System.Reflection.MethodBase site = ex.TargetSite;
            string fileName = stackFrame.GetFileName();
            string methodName = stackFrame.GetMethod().Name;
            int lineNo = stackFrame.GetFileLineNumber();
            string ErrorPageName1 = "Page:" + fileName + " Method:" + methodName + " Line No:" + lineNo + "";


            await getException(ex, ErrorPageName1, Convert.ToString(ex.GetType().Name), "XMLOUT", SearchKey, Info);
        }

        public async Task ErrorBooking(Exception ex, string AgentId, string ErrorNo = "", string SearchKey = "")
        {

            System.Diagnostics.StackTrace trace = new System.Diagnostics.StackTrace(ex, true);
            System.Diagnostics.StackFrame stackFrame = trace.GetFrame(trace.FrameCount - 1);
            System.Reflection.MethodBase site = ex.TargetSite;
            string fileName = stackFrame.GetFileName();
            string methodName = stackFrame.GetMethod().Name;
            int lineNo = stackFrame.GetFileLineNumber();
            string ErrorPageName1 = "Page:" + fileName + " Method:" + methodName + " Line No:" + lineNo + "";
            await getExceptionBooking(ex, ErrorPageName1, Convert.ToString(ex.GetType().Name), "XMLOUT", AgentId, ErrorNo, SearchKey);
        }

        public async Task getExceptionBooking(Exception ex, string file_name, string title, string provider, string AgentId, string ErrorNo = "", string SearchKey = "")
        {
            try
            {
                string Website = "";
                string timestamp = "";
                string hostName = "";
                try
                {
                    try
                    {
                        //Website = Convert.ToString(System.Web.HttpContext.Current.Request.Url.Host);
                        Website = System.Environment.GetEnvironmentVariable("APP_POOL_ID", EnvironmentVariableTarget.Process);
                        hostName = System.Net.Dns.GetHostName();
                    }
                    catch (Exception w)
                    {

                    }
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
                }
                catch (Exception x)
                {
                    timestamp = Convert.ToString(DateTime.Now);
                }
                if (true)
                {
                    await Save_getExceptionBooking(provider, AgentId, title, ex, timestamp, file_name, Website, hostName, ErrorNo, SearchKey);//Added on 10-MAR-2025
                }
                else
                {
                    SqlParameter[] para = new SqlParameter[]{
                     new SqlParameter("@servicename",  provider+"_" +AgentId), new SqlParameter("@title",  title), new SqlParameter("@status",  SqlDbType.Int){Value=0},
                     new SqlParameter("@details", ex.Message + ";"+ ex.StackTrace), new SqlParameter("@datetime",  timestamp), new SqlParameter("@source",  file_name),
                     new SqlParameter("@WebsiteName", Website+", Host="+ hostName),
                      new SqlParameter("@ErrorNo", ErrorNo),
                     new SqlParameter("@SearchKey", SearchKey),
                    };

                    
                    //dl.GetExecuteScalarQry("Insert into Log_Details_MB (servicename,title,status,details,datetime,source)values(@servicename,@title,@status,@details,@datetime,@source)", para);
                    //dl.GetExecuteScalarQry("Insert into Log_Details_Booking_APIOUT (servicename,title,status,details,datetime,source,WebsiteName,ErrorNo,SearchKey)values(@servicename,@title,@status,@details,@datetime,@source,@WebsiteName,@ErrorNo,@SearchKey)", para);
                     await _dLConnection.GetExecuteScalarQry_Logserver2("Insert into Log_Details_Booking_APIOUT (servicename,title,status,details,datetime,source,WebsiteName,ErrorNo,SearchKey)values(@servicename,@title,@status,@details,@datetime,@source,@WebsiteName,@ErrorNo,@SearchKey)", para);//@Newadded on 11-mar-2024
                }
            }
            catch (Exception x)
            {
               await Error(x, "BookingLogException");
            }
        }
        public async Task Save_getExceptionBooking(string provider, string AgentId, string title, Exception ex, string timestamp, string file_name, string Website, string hostName, string ErrorNo, string SearchKey)
        {
            SqlParameter[] para = new SqlParameter[]{
                     new SqlParameter("@servicename",  provider+"_" +AgentId), new SqlParameter("@title",  title), new SqlParameter("@status",  SqlDbType.Int){Value=0},
                     new SqlParameter("@details", ex.Message + ";"+ ex.StackTrace), new SqlParameter("@datetime",  timestamp), new SqlParameter("@source",  file_name),
                     new SqlParameter("@WebsiteName", Website+", Host="+ hostName),
                      new SqlParameter("@ErrorNo", ErrorNo),
                     new SqlParameter("@SearchKey", SearchKey),
                    };

            //dl.GetExecuteScalarQry("Insert into Log_Details_MB (servicename,title,status,details,datetime,source)values(@servicename,@title,@status,@details,@datetime,@source)", para);
            //dl.GetExecuteScalarQry("Insert into Log_Details_Booking_APIOUT (servicename,title,status,details,datetime,source,WebsiteName,ErrorNo,SearchKey)values(@servicename,@title,@status,@details,@datetime,@source,@WebsiteName,@ErrorNo,@SearchKey)", para);
            await _dLConnection.GetExecuteScalarQry_Logserver2("Insert into Log_Details_Booking_APIOUT (servicename,title,status,details,datetime,source,WebsiteName,ErrorNo,SearchKey)values(@servicename,@title,@status,@details,@datetime,@source,@WebsiteName,@ErrorNo,@SearchKey)", para);//@Newadded on 11-mar-2024
        }

        public async Task getException_OLD(Exception ex, string file_name, string title, string provider, string SearchKey = "", string Info = "")
        {
            try
            {
                string Website = "";
              
                SqlParameter[] para = new SqlParameter[]{
                     new SqlParameter("@servicename",  provider), new SqlParameter("@title",  title), new SqlParameter("@status",  SqlDbType.Int){Value=0},
                     new SqlParameter("@details", ex.Message + ";"+ ex.StackTrace), new SqlParameter("@datetime",  DateTime.Now), new SqlParameter("@source",  file_name),
                     new SqlParameter("@WebsiteName", Website),
                     new SqlParameter("@SearchKey", SearchKey),
                     new SqlParameter("@Info", Info),
                    };

                await _dLConnection.GetExecuteScalarQry("Insert into Log_Details_APIOUT (servicename,title,status,details,datetime,source,WebsiteName,SearchKey,Info)values(@servicename,@title,@status,@details,@datetime,@source,@WebsiteName,@SearchKey,@Info)", para);
            }
            catch (Exception)
            {

            }
        }

        public async Task getException(Exception ex, string file_name, string title, string provider, string SearchKey = "", string Info = "")
        {
            try
            {
                string Website = "";
                string timestamp = "";
                string hostName = "";
                try
                {
                    //Website = Convert.ToString(System.Web.HttpContext.Current.Request.Url.Host);
                    //string Website = "";
                    try
                    {
                        //Website = Convert.ToString(System.Web.HttpContext.Current.Request.Url.Host);
                        Website = System.Environment.GetEnvironmentVariable("APP_POOL_ID", EnvironmentVariableTarget.Process);
                        hostName = System.Net.Dns.GetHostName();
                    }
                    catch (Exception w)
                    {

                    }
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
                }
                catch (Exception x)
                {
                    timestamp = Convert.ToString(DateTime.Now);
                }

                string APIname = "";
                var host = "";
                try
                {
                    //APIname = HttpContext.Current.Request.Url.LocalPath.Split('/')[3];
                    //host = _httpContextAccessor.HttpContext?.Request.Host.ToString();
                    host = _httpContextAccessor.HttpContext?.Request.Path.ToString();

                    APIname = host.Split('/')[3];
                }
                catch (Exception)
                {
                }
                int APIName = 0;
                switch (APIname.ToLower())
                {
                    case "availability":
                    case "availabilitywithcancellation":
                        APIName = 1;
                        break;
                    case "recheck":
                    case "cancellationpolicy":
                    case "cancellationpolicymulti":
                        APIName = 2;
                        break;
                    case "prebook":
                        APIName = 3;
                        break;
                    case "book":
                    case "bookingdetail":
                        APIName = 4;
                        break;
                    case "checkhotelcancellationcharges":
                    case "cancelbooking":
                        APIName = 5;
                        break;
                    default:
                        {
                            try
                            {
                                if (host.ToLower().Contains("availability"))
                                    APIName = 1;
                                else
                                    APIName = 0;
                            }
                            catch (Exception)
                            {
                                APIName = 0;
                            }
                        }
                        break;
                }

                if (true)
                {
                    string actionName = GetCollectionName(APIName);

                     _logger.LogError(
                        ex,
                        "{Action} | Provider:{Provider} | API:{APIName} | Website:{Website} | Host:{HostName} | SearchKey:{SearchKey} | File:{FileName} | Info:{Info}",
                        actionName,
                        provider,
                        APIName,
                        Website,
                        hostName,
                        SearchKey,
                        file_name,
                        Info
                        );
                    // await SaveGetException(provider, title, ex, timestamp, file_name, Website, hostName, SearchKey, Info, APIName);//Added on 10-MAR-2025
                }
                else
                {
                    SqlParameter[] para = new SqlParameter[]{
                     new SqlParameter("@servicename",  provider),
                    new SqlParameter("@title",  title),
                    new SqlParameter("@status",  SqlDbType.Int){Value=0},
                     new SqlParameter("@details", ex.Message + ";"+ ex.StackTrace),
                    new SqlParameter("@datetime",  timestamp),
                    new SqlParameter("@source",  file_name),
                     new SqlParameter("@WebsiteName", Website+", Host="+ hostName),
                     new SqlParameter("@SearchKey", SearchKey),
                     new SqlParameter("@Info", Info),
                     new SqlParameter("@APIName", APIName),
                    };


                    await _dLConnection.ExecuteNonQuery_Logserver2("Log_Details_APIOUT_SP", para);//@Newadded on 11-mar-2024
                }
            }
            catch (Exception x)
            {
               await getException_v1(x.Message, "Log_Details_APIOUT_SP", "Log_Details_APIOUT_SP", "", "");
            }
        }

        public async Task SaveGetException(string provider, string title, Exception ex, string timestamp, string file_name, string Website, string hostName, string SearchKey, string Info, int APIName)
        {
            SqlParameter[] para = new SqlParameter[]{
                     new SqlParameter("@servicename",  provider),
                    new SqlParameter("@title",  title),
                    new SqlParameter("@status",  SqlDbType.Int){Value=0},
                     new SqlParameter("@details", ex.Message + ";"+ ex.StackTrace),
                    new SqlParameter("@datetime",  timestamp),
                    new SqlParameter("@source",  file_name),
                     new SqlParameter("@WebsiteName", Website+", Host="+ hostName),
                     new SqlParameter("@SearchKey", SearchKey),
                     new SqlParameter("@Info", Info),
                     new SqlParameter("@APIName", APIName),
                    };

            await _dLConnection.ExecuteNonQuery_Logserver2("Log_Details_APIOUT_SP", para);//@Newadded on 11-mar-2024
        }

      

        public async Task getException_v1(string ex, string file_name, string title, string provider, string SearchKey = "", string Info = "")
        {
            try
            {
                string Website = "";
                string timestamp = "";
                string hostName = "";

                try
                {
                    try
                    {
                        //Website = Convert.ToString(System.Web.HttpContext.Current.Request.Url.Host);
                        Website = System.Environment.GetEnvironmentVariable("APP_POOL_ID", EnvironmentVariableTarget.Process);
                        hostName = System.Net.Dns.GetHostName();
                    }
                    catch (Exception w)
                    {

                    }
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
                }
                catch (Exception x)
                {
                    timestamp = Convert.ToString(DateTime.Now);
                }
                SqlParameter[] para = new SqlParameter[]{
                     new SqlParameter("@servicename",  provider), new SqlParameter("@title",  title), new SqlParameter("@status",  SqlDbType.Int){Value=0},
                     new SqlParameter("@details", ex), new SqlParameter("@datetime",  timestamp), new SqlParameter("@source",  file_name),
                     new SqlParameter("@WebsiteName", Website+", Host="+ hostName),
                     new SqlParameter("@SearchKey", SearchKey),
                     new SqlParameter("@Info", Info),

                    };

                //dl.GetExecuteScalarQry("Insert into Log_Details_MB (servicename,title,status,details,datetime,source)values(@servicename,@title,@status,@details,@datetime,@source)", para);
                
                //23-JAN-26
                //await _dLConnection.GetExecuteScalarQry("Insert into Log_Details_APIOUT_V1 (servicename,title,status,details,datetime,source,WebsiteName,SearchKey,Info)values(@servicename,@title,@status,@details,@datetime,@source,@WebsiteName,@SearchKey,@Info)", para);

                int APIName = 6; //
                string actionName = GetCollectionName(APIName);

                _logger.LogError(
                   ex,
                   "{Action} | Provider:{Provider} | API:{APIName} | Website:{Website} | Host:{HostName} | SearchKey:{SearchKey} | File:{FileName} | Info:{Info}",
                   actionName,
                   provider,
                   APIName,
                   Website,
                   hostName,
                   SearchKey,
                   file_name,
                   Info
                   );
            }
            catch (Exception)
            {

            }
        }

        public async Task ErrorNew(Exception ex, ErrorLog._LogAPI api, string SearchKey = "", string Info = "")
        {
            int lineNumber = 0;
            int APIName = 1;
            try
            {
                StackTrace st = new StackTrace(ex, true);
                StackFrame frame = st.GetFrame(0);
                lineNumber = frame.GetFileLineNumber();
                //string fileName = frame.GetFileName();

                APIName = (int)api;
            }
            catch (Exception)
            {

            }
            Info = Convert.ToString(lineNumber);

            System.Diagnostics.StackTrace trace = new System.Diagnostics.StackTrace(ex, true);
            System.Diagnostics.StackFrame stackFrame = trace.GetFrame(trace.FrameCount - 1);
            System.Reflection.MethodBase site = ex.TargetSite;
            string fileName = stackFrame.GetFileName();
            string methodName = stackFrame.GetMethod().Name;
            int lineNo = stackFrame.GetFileLineNumber();
            string ErrorPageName1 = "Page:" + fileName + " Method:" + methodName + " Line No:" + lineNo + "";


            await getExceptionNew(ex, ErrorPageName1, Convert.ToString(ex.GetType().Name), "XMLOUT", SearchKey, Info, APIName);
        }
        public async Task getExceptionNew(Exception ex, string file_name, string title, string provider, string SearchKey = "", string Info = "", int APIName=1)
        {
            try
            {
                string Website = "";
                string timestamp = "";
                string hostName = "";
                try
                {
                    //Website = Convert.ToString(System.Web.HttpContext.Current.Request.Url.Host);
                    //string Website = "";
                    try
                    {
                        //Website = Convert.ToString(System.Web.HttpContext.Current.Request.Url.Host);
                        Website = System.Environment.GetEnvironmentVariable("APP_POOL_ID", EnvironmentVariableTarget.Process);
                        hostName = System.Net.Dns.GetHostName();
                    }
                    catch (Exception w)
                    {

                    }
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
                }
                catch (Exception x)
                {
                    timestamp = Convert.ToString(DateTime.Now);
                }

                string APIname = "";
                var host = "";
                try
                {
                    //APIname = HttpContext.Current.Request.Url.LocalPath.Split('/')[3];
                    //host = _httpContextAccessor.HttpContext?.Request.Host.ToString();
                    host = _httpContextAccessor.HttpContext?.Request.Path.ToString();

                    APIname = host.Split('/')[3];
                }
                catch (Exception)
                {
                }

                #region Not Used
                //APIName = 0;
                //switch (APIname.ToLower())
                //{
                //    case "availability":
                //    case "availabilitywithcancellation":
                //        APIName = 1;
                //        break;
                //    case "recheck":
                //    case "cancellationpolicy":
                //    case "cancellationpolicymulti":
                //        APIName = 2;
                //        break;
                //    case "prebook":
                //        APIName = 3;
                //        break;
                //    case "book":
                //    case "bookingdetail":
                //        APIName = 4;
                //        break;
                //    case "checkhotelcancellationcharges":
                //    case "cancelbooking":
                //        APIName = 5;
                //        break;
                //    default:
                //        {
                //            try
                //            {
                //                if (host.ToLower().Contains("availability"))
                //                    APIName = 1;
                //                else
                //                    APIName = 0;
                //            }
                //            catch (Exception)
                //            {
                //                APIName = 0;
                //            }
                //        }
                //        break;
                //}
                #endregion
                if (true)
                {
                    //await SaveGetException(provider, title, ex, timestamp, file_name, Website, hostName, SearchKey, Info, APIName);//Added on 10-MAR-2025

                    string actionName = GetCollectionName(APIName);

                    _logger.LogError(
                       ex,
                       "{Action} | Provider:{Provider} | API:{APIName} | Website:{Website} | Host:{HostName} | SearchKey:{SearchKey} | File:{FileName} | Info:{Info}",
                       actionName,
                       provider,
                       APIName,
                       Website,
                       hostName,
                       SearchKey,
                       file_name,
                       Info
                       );
                }
                else
                {
                    SqlParameter[] para = new SqlParameter[]{
                     new SqlParameter("@servicename",  provider),
                    new SqlParameter("@title",  title),
                    new SqlParameter("@status",  SqlDbType.Int){Value=0},
                     new SqlParameter("@details", ex.Message + ";"+ ex.StackTrace),
                    new SqlParameter("@datetime",  timestamp),
                    new SqlParameter("@source",  file_name),
                     new SqlParameter("@WebsiteName", Website+", Host="+ hostName),
                     new SqlParameter("@SearchKey", SearchKey),
                     new SqlParameter("@Info", Info),
                     new SqlParameter("@APIName", APIName),
                    };


                    await _dLConnection.ExecuteNonQuery_Logserver2("Log_Details_APIOUT_SP", para);//@Newadded on 11-mar-2024
                }
            }
            catch (Exception x)
            {
                await getException_v1(x.Message, "Log_Details_APIOUT_SP", "Log_Details_APIOUT_SP", "", "");
            }
        }

        public async Task getException_v2(string ex, string file_name, string title, string provider, string SearchKey = "", string Info = "")
        {
            try
            {
                string Website = "";
                string timestamp = "";
                string hostName = "";

                try
                {
                    try
                    {
                        //Website = Convert.ToString(System.Web.HttpContext.Current.Request.Url.Host);
                        Website = System.Environment.GetEnvironmentVariable("APP_POOL_ID", EnvironmentVariableTarget.Process);
                        hostName = System.Net.Dns.GetHostName();
                    }
                    catch (Exception w)
                    {

                    }
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
                }
                catch (Exception x)
                {
                    timestamp = Convert.ToString(DateTime.Now);
                }
                SqlParameter[] para = new SqlParameter[]{
                     new SqlParameter("@servicename",  provider), new SqlParameter("@title",  title), new SqlParameter("@status",  SqlDbType.Int){Value=0},
                     new SqlParameter("@details", ex), new SqlParameter("@datetime",  timestamp), new SqlParameter("@source",  file_name),
                     new SqlParameter("@WebsiteName", Website+", Host="+ hostName),
                     new SqlParameter("@SearchKey", SearchKey),
                     new SqlParameter("@Info", Info),

                    };

                //dl.GetExecuteScalarQry("Insert into Log_Details_MB (servicename,title,status,details,datetime,source)values(@servicename,@title,@status,@details,@datetime,@source)", para);
                //23-JAN-26
                //await _dLConnection.GetExecuteScalarQry_Logserver2("Insert into Log_Details_APIOUT_V2 (servicename,title,status,details,datetime,source,WebsiteName,SearchKey,Info)values(@servicename,@title,@status,@details,@datetime,@source,@WebsiteName,@SearchKey,@Info)", para);
                int APIName = 6; //
                string actionName = GetCollectionName(APIName);

                _logger.LogError(
                   ex,
                   "{Action} | Provider:{Provider} | API:{APIName} | Website:{Website} | Host:{HostName} | SearchKey:{SearchKey} | File:{FileName} | Info:{Info}",
                   actionName,
                   provider,
                   APIName,
                   Website,
                   hostName,
                   SearchKey,
                   file_name,
                   Info
                   );
            }
            catch (Exception)
            {

            }
        }
    }
}
