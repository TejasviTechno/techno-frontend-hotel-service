using System.Data;
using System.Text;
using System.Collections.Concurrent;
using XConnect.ErrorLogHandler.Interface;
using XConnect.DAL.Interface;
using Microsoft.Data.SqlClient;
using XConnect.Models;
using XConnect.Models.Hotel;
using MathNet.Numerics.Random;
using MongoDB.Driver;
using Microsoft.Extensions.Caching.Memory;
using XConnect.CommonUtilities.Interface;
using Microsoft.Extensions.Options;
using MongoDB.Bson.IO;
using System.Text.Json;
using System.Xml;
using Microsoft.IdentityModel.Protocols;
using MongoDB.Bson;
using static Azure.Core.HttpHeader;
using Microsoft.AspNetCore.Http;
using XConnect.DAL;
using System.Net;
using Newtonsoft.Json.Linq;
using System.IO.Compression;
using ZstdSharp.Unsafe;
using System.Data.Common;
using MongoDB.Bson.Serialization;
using System.ComponentModel;
using MongoDB.Bson.Serialization.Serializers;
using static XConnect.Models.ErrorLog;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;
using System.Net.Http.Headers;
using System.Net.Http;
using XConnect.Analytics;
using XConnect.Analytics.Interface;
using System.Diagnostics;




namespace XConnect.CommonUtilities
{
    public class CommonMethodRepo : ICommonMethod
    {
        private readonly IException _exception;
        private readonly IDLConnection _dlConnection;
        private readonly IMemoryCache _cache;
        private readonly ISupplierRes _supplierRes;
        //private readonly IMongoClient _mongoClient;
        private readonly KeySettings _keySettings;
        private readonly ConnectionStrings _connectionStrings;
        private readonly MongoSettings _mongoSettings;
        private readonly RabitMQSettings _rabitmqSettings;
        private readonly IAnalyticsSupplier _analyticSupplier;


        public static string SEARCH_KEY = "";
        public static string HotelTrvacoProvider = "http://www.V3.illusionswebservices.iwtx.com/illusionsHotelSearch.ashx";


        private readonly IMongoDatabase _main1;
        private readonly IMongoDatabase _main2;
        private readonly IMongoDatabase _main3;
        private readonly IMongoDatabase _supp;
        private readonly IMongoDatabase _roomMapping;
        private readonly IMongoDatabase _RowroomMapping;
        private readonly IMongoDatabase _QRSLog;
        private readonly IMongoDatabase _SPRLog;
        private readonly IMongoDatabase _roomBookingC;
        private readonly IHttpClientFactory _httpClientFactory; //200525

        private readonly RabbitMQPublisher _publisher;

        private readonly IExceptionLogStore _stores;
        private readonly ILogger<CommonMethodRepo> _logger;

        public CommonMethodRepo(IException exception, IDLConnection dlConnection, IMemoryCache cache, ISupplierRes supplierRes, IOptions<KeySettings> keySettings, IOptions<ConnectionStrings> connectionStrings, IOptions<MongoSettings> mongoSettings, MongoDbFactoryNew factory, RabbitMQPublisher publisher, IOptions<RabitMQSettings> rabitmqSettings, IHttpClientFactory httpClientFactory, IExceptionLogStore stores, ILogger<CommonMethodRepo> logger, IAnalyticsSupplier analyticSupplier)
        {
            _exception = exception;
            _dlConnection = dlConnection;
            _cache = cache;
            _supplierRes = supplierRes;
            _keySettings = keySettings.Value;
            _connectionStrings = connectionStrings.Value;
            _mongoSettings = mongoSettings.Value;
            _rabitmqSettings = rabitmqSettings.Value;

            //_mongoClient = mongoClient;

            _main1 = factory.GetDatabase("Main1");
            _main2 = factory.GetDatabase("Main2");
            _main3 = factory.GetDatabase("Main3");
            _supp = factory.GetDatabase("SuppLog");
            _roomMapping = factory.GetDatabase("RoomMapping");
            _RowroomMapping = factory.GetDatabase("Row_RoomsForRoomMapping");
            _QRSLog = factory.GetDatabase("QRSLog");
            _SPRLog = factory.GetDatabase("SPR");
            _roomBookingC = factory.GetDatabase("BookingCRoomMapping");
            _publisher = publisher;
            _httpClientFactory = httpClientFactory;
            _stores = stores;
            _logger = logger;
            _analyticSupplier = analyticSupplier;
        }

        //15-JAN-26
        public DataTable UserCartTableStructure()
        {
            try
            {
                DataTable dt = new DataTable();
                dt.Columns.Add("UserCartID", typeof(int));
                dt.Columns.Add("GuestUserId", typeof(string));
                dt.Columns.Add("CheckInDate", typeof(string));
                dt.Columns.Add("StrCheckInDate", typeof(string));
                dt.Columns.Add("CheckOutDate", typeof(string));
                dt.Columns.Add("ServiceId", typeof(int));
                dt.Columns.Add("ServiceName", typeof(string));
                dt.Columns.Add("ServiceType", typeof(string));
                dt.Columns.Add("XMLHotelId", typeof(string));
                dt.Columns.Add("OfficeId", typeof(string));
                dt.Columns.Add("ContractId", typeof(string));
                dt.Columns.Add("XMLCountryId", typeof(string));
                dt.Columns.Add("XMLCityId", typeof(string));
                dt.Columns.Add("Nationality", typeof(string));
                dt.Columns.Add("NationalityName", typeof(string));
                dt.Columns.Add("GiataId", typeof(int));
                dt.Columns.Add("NoOfAdults", typeof(int));
                dt.Columns.Add("NoOfChild", typeof(int));
                dt.Columns.Add("IsB2B", typeof(bool));
                dt.Columns.Add("CountryId", typeof(int));
                dt.Columns.Add("CityId", typeof(int));
                dt.Columns.Add("StarRating", typeof(int));
                dt.Columns.Add("SupplierCurrency", typeof(string));
                dt.Columns.Add("NoOfNight", typeof(string));
                dt.Columns.Add("PackageId", typeof(string));
                dt.Columns.Add("IsVouchered", typeof(string));
                dt.Columns.Add("Percentage", typeof(double));
                return dt;
            }
            catch (Exception ex)
            {
                _ = _exception.Error(ex);
            }
            return null;
        }
        //15-JAN-26
        public DataTable UserCartDetailsTableStructure()
        {
            try
            {
                DataTable dt = new DataTable();
                dt.Columns.Add("UserCartID", typeof(int));
                dt.Columns.Add("UserCartDetailID", typeof(int));
                dt.Columns.Add("CompanyBuyingPrice", typeof(double));
                dt.Columns.Add("AgentBuyingPrice", typeof(double));
                dt.Columns.Add("FinalSellingPrice", typeof(double));
                dt.Columns.Add("RoomTypeId", typeof(string));
                dt.Columns.Add("RoomTypeCode", typeof(string));
                dt.Columns.Add("SupplierName", typeof(string));
                dt.Columns.Add("RoomType", typeof(string));
                dt.Columns.Add("XmlHotelId", typeof(string));
                dt.Columns.Add("ServiceId", typeof(int));
                dt.Columns.Add("HotelName", typeof(string));
                dt.Columns.Add("RoomNumber", typeof(int));
                dt.Columns.Add("MealPlanCode", typeof(string));
                dt.Columns.Add("ContractID", typeof(int));
                dt.Columns.Add("NoOfAdults", typeof(int));
                dt.Columns.Add("NoOfChild", typeof(int));
                dt.Columns.Add("ChildAge1", typeof(int));
                dt.Columns.Add("ChildAge2", typeof(int));
                dt.Columns.Add("spui", typeof(string));
                dt.Columns.Add("CheckInDate", typeof(string));
                dt.Columns.Add("CheckOutDate", typeof(string));
                dt.Columns.Add("EachoToken", typeof(string));
                dt.Columns.Add("IsCombineRoom", typeof(bool));
                dt.Columns.Add("XmlNationality", typeof(string));
                dt.Columns.Add("purchaseToken", typeof(string));
                dt.Columns.Add("IsVouchered", typeof(int));
                dt.Columns.Add("MappedMeal", typeof(string));
                return dt;
            }
            catch (Exception ex)
            {
                _ = _exception.Error(ex);
            }
            return null;
        }

        public async Task<T?> SendRequestAsync<T>(string baseUrl, string endpoint, HttpMethod method, object? parameters = null, string? token = null)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();

                // configure base URL & headers at runtime
                client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Add("Accept", "application/json");

                var request = new HttpRequestMessage(method, endpoint);

                if (!string.IsNullOrEmpty(token))
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                if (parameters != null && (method == HttpMethod.Post || method == HttpMethod.Put || method.Method == "PATCH"))
                {
                    var json = Newtonsoft.Json.JsonConvert.SerializeObject(parameters);
                    request.Content = new StringContent(json, Encoding.UTF8, "application/json");
                }

                var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var jsonResponse = await response.Content.ReadAsStringAsync();
                return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(jsonResponse);
            }
            catch (Exception ex)
            {
                _ = _exception.Error(ex, "GetHotelNameForRedis");
                //Console.WriteLine($"[ApiService Error] {ex.Message}");
                return default;
            }
        }
        public async Task<string> GetHotelNameForRedis(int HotelId)
        {
            try
            {
                SqlParameter[] para = new SqlParameter[]{
                new SqlParameter("@HotelId",  HotelId),
                    };

                string data = Convert.ToString(await _dlConnection.GetExecuteScalarSP("GetHotelNameForRedis", para));
                return data;
            }
            catch (Exception ex)
            {
                _ = _exception.Error(ex, "GetHotelNameForRedis");
                return "";
            }

        }

        public async Task<DataTable> GetEnableRedisAsync()
        {
            DataTable dt = new DataTable();

            try
            {
                // Cache key
                string key = "EnableRedisFlag";

                // Return if in cache
                if (_cache.TryGetValue(key, out dt))
                    return dt;

                // Not in cache → fetch from DB
                SqlParameter[] para = new SqlParameter[] { };
                dt = await _dlConnection.GetDataTableAsync("GetRedisConfig_MappingReplica_New", para);

                // Store in memory cache for 5 minutes
                _cache.Set(key, dt, TimeSpan.FromMinutes(5));
                return dt;
            }
            catch (Exception ex)
            {
                _ = _exception.Error(ex, "GetEnableRedisAsync");
            }
            return dt;
        }

        public async Task<List<string>> ActiveSupplierByAgentId(int AgentId, int Timeout = 5, int DB1CacheTime = 10)
        {
            List<string> supplierNames = new List<string>();
            try
            {

                string keydata_Common = "ActiveSupplier_" + Convert.ToString(AgentId);
                if (!_cache.TryGetValue(keydata_Common, out supplierNames))
                {
                    supplierNames = new List<string>(); // initialize here

                    SqlParameter[] para = new SqlParameter[]{
                          new SqlParameter("@AgentID", AgentId),
                            };


                    DataTable dtActiveSupplier = new DataTable();
                    dtActiveSupplier = await _dlConnection.GetDataTable_Replica("ActiveSupplierByAgentId", para, Timeout);


                    foreach (DataRow row in dtActiveSupplier.Rows)
                    {
                        supplierNames.Add(row["SupplierName"].ToString());
                    }


                    if (supplierNames.Count > 0)
                    {
                        //_cache.Set(keydata_Common, dsIsSubAgent_Agent, new MemoryCacheEntryOptions().SetAbsoluteExpiration(DateTime.Now.AddHours(1)); //// Set cache to expire in 12 hours
                        _cache.Set(keydata_Common, supplierNames, new MemoryCacheEntryOptions().SetAbsoluteExpiration(DateTime.Now.AddMinutes(DB1CacheTime)));

                    }
                }

                return supplierNames;
            }
            catch (Exception ex)
            {
                _ = _exception.Error(ex, "ActiveSupplierByAgentId");

            }

            return supplierNames;
        }
        public DataTable GenerateGiataDetails()
        {
            try
            {
                DataTable dtHotel = new DataTable("GiataDetails");
                dtHotel.Columns.AddRange(new DataColumn[]
                {
                    new DataColumn("RaynaId", typeof(int)),
                    new DataColumn("Rating", typeof(int)),
                    new DataColumn("CountryId", typeof(int)),
                    new DataColumn("CityId", typeof(int)),
                    new DataColumn("GiataId", typeof(int)),
                    new DataColumn("IsRecomondedHotel", typeof(int)),
                    new DataColumn("HotelContactNo", typeof(string)),
                    new DataColumn("ReservationEmail", typeof(string)),
                    new DataColumn("HotelName", typeof(string))
                });

                return dtHotel;
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
            }
            return null;
        }

        public DataTable GenerateMappingDetails()
        {
            try
            {
                DataTable dtHotel = new DataTable("MappingDetails");
                dtHotel.Columns.AddRange(new DataColumn[]
                {
                    new DataColumn("XmlId", typeof(string)),
                    new DataColumn("RaynaId", typeof(int)),
                    new DataColumn("OwnsystemHotelId", typeof(int)),
                    new DataColumn("XmlbaseId", typeof(string)),
                    new DataColumn("supplierName", typeof(string)),
                    new DataColumn("SupplierId", typeof(int)),
                    new DataColumn("GiataId", typeof(int)),
                    new DataColumn("CityId", typeof(int)),
                    new DataColumn("TechName", typeof(string))
                });

                return dtHotel;
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
            }
            return null;
        }
        public async Task<bool> GetbookingkeyUpdateStaus(string pricecheck_BookingKey, int USercartID)
        {
            try
            {
                SqlParameter[] para = new SqlParameter[]{
 new SqlParameter("@EachoToken",  pricecheck_BookingKey),
 new SqlParameter("@UserCartId", USercartID),
};

                await _dlConnection.GetExecuteScalarQry("Update UserCartDetails set EachoToken = @EachoToken where UserCartId = @UserCartId", para);
                return true;
            }
            catch (Exception ex)
            {
                _ = _exception.ErrorNew(ex, _LogAPI.Prebook);
                return false;
            }
        }
        public async Task<DataSet> GetRatebreakup_Stahh(string AgentVoucherNo)
        {

            try
            {
                SqlParameter[] para = new SqlParameter[]{
                new SqlParameter("@BookingDetailId",AgentVoucherNo)
            };
                DataSet Ratebreakups = await _dlConnection.GetDataSet("GetRatebreakup_Staah", para);
                return Ratebreakups;
            }
            catch (Exception ex)
            {
                _ = _exception.ErrorNew(ex, _LogAPI.CancelBooking);
                return null;
            }
        }

        public async Task<DataSet> GetRatebreakup_Dedge(string AgentVoucherNo)
        {

            try
            {
                SqlParameter[] para = new SqlParameter[]{
                new SqlParameter("@BookingDetailId",AgentVoucherNo)
            };
                DataSet Ratebreakups = await _dlConnection.GetDataSet("GetRatebreakup_Dedge", para);
                return Ratebreakups;
            }
            catch (Exception ex)
            {
                _ = _exception.ErrorNew(ex, _LogAPI.CancelBooking);
                return null;
            }
        }

        public async Task<DataTable> GetPassengerDetail_Staah(int bookingDetailId)
        {
            try
            {
                SqlParameter[] para = new SqlParameter[]{
        new SqlParameter("@BookingDetailId",bookingDetailId)
        };
                DataTable dt = await _dlConnection.GetDataTableAsync("GetPassengerDetails_staah", para);
                return dt;

            }
            catch (Exception ex)
            {
                _ = _exception.ErrorNew(ex, _LogAPI.CancelBooking);
                return null;
            }

        }

        public async Task<DataTable> GetPassengerDetail_Dedge(int DetailId)
        {
            try
            {
                DataTable dt = new DataTable();
                SqlParameter[] para1 = new SqlParameter[]{
                     new SqlParameter ("@BookingDetailId",DetailId)
                };
                dt = await _dlConnection.GetDataTableAsync("GetPassengerDetails_Dedge", para1);

                return dt;
            }
            catch (Exception ex)
            {
                _ = _exception.ErrorNew(ex, _LogAPI.CancelBooking);
                return null;
            }
            finally
            {
                //dbCommand = null;
                //  dbHelper = null;
            }
        }

        public async Task<DataTable> GetPassengerDetail_Rategain(int bookingDetailId)
        {

            try
            {
                SqlParameter[] para = new SqlParameter[]{
                new SqlParameter("@BookingDetailId",bookingDetailId)
            };
                DataTable AgentMasterEmailds = await _dlConnection.GetDataTableAsync("GetPassengerDetails_rategain", para);
                return AgentMasterEmailds;
            }
            catch (Exception ex)
            {
                _ = _exception.ErrorNew(ex, _LogAPI.CancelBooking);
                return null;
            }
        }


        public async Task<DataTable> GetPassengerDetail_Travelclick(int bookingDetailId)
        {

            try
            {
                SqlParameter[] para = new SqlParameter[]{
                new SqlParameter("@BookingDetailId",bookingDetailId)
            };
                DataTable AgentMasterEmailds = await _dlConnection.GetDataTableAsync("GetPassengerDetails_Travelclick", para);
                return AgentMasterEmailds;
            }
            catch (Exception ex)
            {
                _ = _exception.ErrorNew(ex, _LogAPI.CancelBooking);

                return null;
            }
        }


        public async Task<DataTable> GetPassengerDetail_RateTiger(int bookingDetailId)
        {

            try
            {
                SqlParameter[] para = new SqlParameter[]{
                new SqlParameter("@BookingDetailId",bookingDetailId)
            };
                DataTable AgentMasterEmailds = await _dlConnection.GetDataTableAsync("GetPassengerDetails_RateTiger", para);
                return AgentMasterEmailds;
            }
            catch (Exception ex)
            {
                _ = _exception.ErrorNew(ex, _LogAPI.CancelBooking);

                return null;
            }
        }

        public async Task<DataTable> GetPassengerDetail_HotelRunner(int bookingDetailId)
        {

            try
            {
                SqlParameter[] para = new SqlParameter[]{
                new SqlParameter("@BookingDetailId",bookingDetailId)
            };
                DataTable AgentMasterEmailds = await _dlConnection.GetDataTableAsync("GetPassengerDetails_HotelRunner", para);
                return AgentMasterEmailds;
            }
            catch (Exception ex)
            {
                _ = _exception.ErrorNew(ex, _LogAPI.CancelBooking);

                return null;
            }
        }

        public async Task<DataTable> GetPassengerDetail(int bookingDetailId)
        {
            try
            {
                SqlParameter[] para = new SqlParameter[]{
                new SqlParameter("@BookingDetailId",bookingDetailId)
            };
                DataTable AgentMasterEmailds = new DataTable("GetCompanySettingForAgent");
                AgentMasterEmailds = await _dlConnection.GetDataTableAsync("GetPassengerDetails_siteminder", para);
                return AgentMasterEmailds;
            }
            catch (Exception ex)
            {
                _ = _exception.ErrorNew(ex, _LogAPI.CancelBooking);
                return null;
            }
        }
        public void ThirdPartyErrorInCancellationHistory(string UniqueNo, string AgentUniqueNo, string XMLRequest, string XMLResponse, string Stage, string Provider, string StatusBooking, string ReferenceNo, string Website)
        {
            if (true)//10-MAR-2025
            {
                try
                {
                    Task.Run(() => SaveThirdPartyErrorInCancellationHistory(UniqueNo, AgentUniqueNo, XMLRequest, XMLResponse, Stage, Provider, StatusBooking, ReferenceNo, Website));
                }
                catch (Exception x)
                {
                    _ = _exception.ErrorNew(x, _LogAPI.CancelBooking, "", "ThirdPartyErrorInCancellationHistory_task");
                }
            }
            else
            {
                //SqlConnection conn1 = new SqlConnection(System.Web.Configuration.WebConfigurationManager.ConnectionStrings["ConnectionString"].ToString());
                SqlConnection conn1 = new SqlConnection(_connectionStrings.connectionStr_Logserver2_TP);//@Newadded on 12-mar-2024
                try
                {
                    Website = Website + " [API]";//Added on 4-APR-2024
                    conn1.Open();
                    SqlCommand comm1 = new SqlCommand();
                    comm1.Connection = conn1;
                    //comm1.CommandText = "insert into ThirdPartyErrorInCancellationHistory (TokenNo,AgentTokenNo,XMLRequest,XMLResponse,Stage,Provider,StatusBooking,ReferenceNo,Website,BookingDate) values('" + UniqueNo + "','" + AgentUniqueNo + "','" + ConfirmationBookingRequest + "','" + "-" + "','" + "Before Request hit booking" + "','" + SupplierData + "','" + "Request Confirm" + "','" + Convert.ToString(_objHotel.PNRNo) + "','" + "B2C" + "',getdate())";
                    comm1.CommandText = "insert into ThirdPartyErrorInCancellationHistory (TokenNo,AgentTokenNo,XMLRequest,XMLResponse,Stage,Provider,StatusBooking,ReferenceNo,Website,BookingDate) values('" + UniqueNo + "','" + AgentUniqueNo + "','" + XMLRequest.Replace("'", "''").Replace(",", "") + "','" + XMLResponse.Replace("'", "''").Replace(",", "") + "','" + Stage + "','" + Provider + "','" + StatusBooking + "','" + ReferenceNo + "','" + Website + "',getdate())";
                    comm1.ExecuteScalar();
                    //conn1.Close();
                }
                catch (Exception Ex)
                {
                    _ = _exception.ErrorNew(Ex, _LogAPI.CancelBooking, "", "ThirdPartyErrorInCancellationHistory");

                }
                finally
                {
                    conn1.Close();
                }
            }
        }

        public void SaveThirdPartyErrorInCancellationHistory(string UniqueNo, string AgentUniqueNo, string XMLRequest, string XMLResponse, string Stage, string Provider, string StatusBooking, string ReferenceNo, string Website)
        {
            //SqlConnection conn1 = new SqlConnection(System.Web.Configuration.WebConfigurationManager.ConnectionStrings["ConnectionString"].ToString());
            SqlConnection conn1 = new SqlConnection(_connectionStrings.connectionStr_Logserver2_TP);//@Newadded on 12-mar-2024
            try
            {
                Website = Website + " [API]";//Added on 4-APR-2024
                conn1.Open();
                SqlCommand comm1 = new SqlCommand();
                comm1.Connection = conn1;
                //comm1.CommandText = "insert into ThirdPartyErrorInCancellationHistory (TokenNo,AgentTokenNo,XMLRequest,XMLResponse,Stage,Provider,StatusBooking,ReferenceNo,Website,BookingDate) values('" + UniqueNo + "','" + AgentUniqueNo + "','" + ConfirmationBookingRequest + "','" + "-" + "','" + "Before Request hit booking" + "','" + SupplierData + "','" + "Request Confirm" + "','" + Convert.ToString(_objHotel.PNRNo) + "','" + "B2C" + "',getdate())";
                comm1.CommandText = "insert into ThirdPartyErrorInCancellationHistory (TokenNo,AgentTokenNo,XMLRequest,XMLResponse,Stage,Provider,StatusBooking,ReferenceNo,Website,BookingDate) values('" + UniqueNo + "','" + AgentUniqueNo + "','" + XMLRequest.Replace("'", "''").Replace(",", "") + "','" + XMLResponse.Replace("'", "''").Replace(",", "") + "','" + Stage + "','" + Provider + "','" + StatusBooking + "','" + ReferenceNo + "','" + Website + "',getdate())";
                comm1.ExecuteScalar();
                //conn1.Close();
            }
            catch (Exception Ex)
            {
                _ = _exception.ErrorNew(Ex, _LogAPI.CancelBooking, "", "SaveThirdPartyErrorInCancellationHistory_Task...");
            }
            finally
            {
                conn1.Close();
            }
        }
        public async Task GetBookingCancellationMail_new(string ReferenceNo, int BookingId, int countryid, string Subject, string AgentEmailId, string SubagentEmailId, string SubUserEmailId, int AgentId, int CancelOldBooking, string hostname, string branchemail, string CCEmail, string toEmail)
        {
            string finalstring = string.Empty;
            try
            {
                //string SubagentEmailId = string.Empty;
                //string SubUserEmailId = string.Empty;
                //string AgentEmailId = string.Empty;

                string FromEmail = _keySettings.B2CBookingEmailId.ToString();
                DataSet ds = new DataSet();
                ds = await GetVoucher(62);
                DataSet dsSupplierTemplate = await GetVoucher(126);
                List<Email_Details> emailList = new List<Email_Details>();
                string EmpToEmailId = "";
                if (ds.Tables[0].Rows.Count > 0)
                {
                    string StringHtml = ds.Tables[0].Rows[0]["NotificationBody"].ToString().Replace("&lt;", "<").Replace("&gt;", ">").Replace("&nbsp;", "").Replace("<p>", "").Replace("</p>", "");
                    Subject = ds.Tables[0].Rows[0]["Subject"].ToString();
                    string[] alltemplatedata = StringHtml.Split(new String[] { "%%%" }, StringSplitOptions.None);
                    string commonTemplate = alltemplatedata[0].ToString();
                    //string HotelTemplate = alltemplatedata[1].ToString();
                    //string[] HotelSubTemplatelst = HotelTemplate.Split(new String[] { "$$$" }, StringSplitOptions.None);
                    string HotelTemplate1 = alltemplatedata[1].ToString();
                    string HotelTemplate2 = alltemplatedata[2].ToString();
                    string TransferTemplate = alltemplatedata[3].ToString();
                    string TourTemplate = alltemplatedata[4].ToString();
                    string MealTemplate = alltemplatedata[5].ToString();
                    string FooterTemplate = alltemplatedata[6].ToString();

                    string SupplierStringHtml = dsSupplierTemplate.Tables[0].Rows[0]["NotificationBody"].ToString().Replace("&lt;", "<").Replace("&gt;", ">").Replace("&nbsp;", "").Replace("<p>", "").Replace("</p>", "");
                    string SuppplierSubject = dsSupplierTemplate.Tables[0].Rows[0]["Subject"].ToString();
                    string[] Supplieralltemplatedata = SupplierStringHtml.Split(new String[] { "%%%" }, StringSplitOptions.None);
                    string SuppliercommonTemplate = Supplieralltemplatedata[0].ToString();
                    string SupplierDetailTemplate = Supplieralltemplatedata[1].ToString();
                    string SupplierTransferTemplate = Supplieralltemplatedata[2].ToString();
                    string SupplierTourTemplate = Supplieralltemplatedata[3].ToString();
                    string SupplierMealTemplate = Supplieralltemplatedata[4].ToString();

                    DataSet dsv = await GetVoucherListNew(ReferenceNo, BookingId, AgentId, CancelOldBooking);
                    DataSet dsSupplierEmail = await GetSupplierEmail(ReferenceNo, BookingId);

                    SubagentEmailId = Convert.ToString(dsv.Tables[0].Rows[0]["SubAgentEmailId"]);
                    SubUserEmailId = Convert.ToString(dsv.Tables[0].Rows[0]["SubUserEmailId"]);
                    AgentEmailId = Convert.ToString(dsv.Tables[0].Rows[0]["AgentEmailId"]);

                    string strRefNo = Convert.ToString(dsv.Tables[0].Rows[0]["ReferenceNo"]);
                    Subject = Subject.Replace("#ReferenceNo", ReferenceNo).Replace("#GuestName", Convert.ToString(dsv.Tables[0].Rows[0]["Name"]));
                    SuppplierSubject = SuppplierSubject.Replace("#RefNo", ReferenceNo).Replace("#MainGuestName", Convert.ToString(dsv.Tables[0].Rows[0]["Name"]));
                    SuppplierSubject = SuppplierSubject.Replace("#Title", "Booking Cancellation");

                    DataRow[] drCityTour = dsv.Tables[0].Select("ServiceType='City Tour' and ComboId=0");
                    DataRow[] drHotel = dsv.Tables[14].Select("1=1");
                    DataRow[] drCityTourCombo = dsv.Tables[0].Select("ServiceType='City Tour' and ComboId>0");
                    DataRow[] drCombo = dsv.Tables[7].Select();
                    //DataRow[] drvisa = dsv.Tables[0].Select("ServiceType='Visa'");
                    DataRow[] drMealDetail = dsv.Tables[0].Select("ServiceType='Meal'");
                    DataRow[] drTransferDetail = dsv.Tables[0].Select("ServiceType='Transfer'");

                    #region Common
                    commonTemplate = commonTemplate.Replace("#AgentName", Convert.ToString(dsv.Tables[0].Rows[0]["AgentName"]));
                    commonTemplate = commonTemplate.Replace("#ReferenceNo", strRefNo);
                    commonTemplate = commonTemplate.Replace("#AgentRefNo", Convert.ToString(dsv.Tables[0].Rows[0]["AgentInternalRef"]));
                    commonTemplate = commonTemplate.Replace("#GuestName", Convert.ToString(dsv.Tables[0].Rows[0]["Name"]));
                    commonTemplate = commonTemplate.Replace("#PaymentBy", Convert.ToString(dsv.Tables[0].Rows[0]["PaymentMethod"]));
                    #endregion

                    #region City Tour
                    string CityTourHtmlMain = string.Empty;
                    string CityTourHtmlMainSupplier = string.Empty;
                    if (drCityTour.Length > 0)
                    {
                        string CityTourHtml = string.Empty;
                        string CityTourHtmlSupplier = string.Empty;
                        string SupplierTourDetailTemplate = string.Empty;
                        foreach (DataRow drTourdata in drCityTour)
                        {
                            countryid = Convert.ToInt32(drTourdata["CountryId"]);
                            CityTourHtml = TourTemplate;
                            CityTourHtmlSupplier = SupplierTourTemplate;
                            SupplierTourDetailTemplate = SupplierDetailTemplate;
                            CityTourHtml = CityTourHtml.Replace("#TourBookingStatusColor", "red");
                            CityTourHtml = CityTourHtml.Replace("#TourBookingStatus", Convert.ToString(drTourdata["BookingStatus"]));

                            string pickupdata = "-";
                            if (!string.IsNullOrEmpty(Convert.ToString(drTourdata["pickup"])))
                                pickupdata = Convert.ToString(Convert.ToString(drTourdata["pickup"]).Split('/')[0].Split(':')[1]);


                            CityTourHtml = CityTourHtml.Replace("#TourName", Convert.ToString(drTourdata["ServiceName"]));
                            CityTourHtml = CityTourHtml.Replace("#TourCityName", Convert.ToString(drTourdata["CityName"]));
                            CityTourHtml = CityTourHtml.Replace("#TourType", Convert.ToString(drTourdata["RoomType"]));
                            CityTourHtml = CityTourHtml.Replace("#TourDate", Convert.ToString(drTourdata["TravelDate"]));
                            CityTourHtml = CityTourHtml.Replace("#TourPickup", Convert.ToString(pickupdata));
                            CityTourHtml = CityTourHtml.Replace("#TourUnit", "1");
                            CityTourHtml = CityTourHtml.Replace("#TourAdult", Convert.ToString(drTourdata["Adult"]));
                            CityTourHtml = CityTourHtml.Replace("#TourChild", Convert.ToString(drTourdata["Children"]));

                            DataRow[] tourCancellation = dsv.Tables[3].Select("BookingDetailId='" + Convert.ToString(drTourdata["BookingDetailId"]) + "'");
                            string strTourCancellation = string.Empty;
                            foreach (DataRow drtourCancellation in tourCancellation)
                            {
                                strTourCancellation += Convert.ToString(drTourdata["ServiceName"]) + " From " + Convert.ToString(string.Format("{0:d/M/yyyy HH:mm:ss tt}", drtourCancellation["FromDate"])) + " To" + Convert.ToString(string.Format("{0:d/M/yyyy HH:mm:ss tt}", drtourCancellation["ToDate"])) + " the cancellation charges are " + Convert.ToString(Math.Round(Convert.ToDouble(drtourCancellation["CancellationAmount"]), 2)) + " " + Convert.ToString(drTourdata["AgentCurrencyId"]) + "<br/>";
                            }
                            CityTourHtml = CityTourHtml.Replace("#TourCancellationPolicy", strTourCancellation);

                            CityTourHtmlMain += CityTourHtml;

                            #region "for Supplier"
                            SupplierTourDetailTemplate = SupplierTourDetailTemplate.Replace("#BookingStatus", Convert.ToString(drTourdata["BookingStatus"]));
                            SupplierTourDetailTemplate = SupplierTourDetailTemplate.Replace("#SupplierName", Convert.ToString(drTourdata["SupplierName"]));
                            SupplierTourDetailTemplate = SupplierTourDetailTemplate.Replace("#GuestName", Convert.ToString(drTourdata["Name"]));
                            SupplierTourDetailTemplate = SupplierTourDetailTemplate.Replace("#FileNo", strRefNo);
                            SupplierTourDetailTemplate = SupplierTourDetailTemplate.Replace("#IssueDate", Convert.ToString(drTourdata["IssueDate"]));
                            SupplierTourDetailTemplate = SupplierTourDetailTemplate.Replace("#BookingService", "Tour");


                            CityTourHtmlSupplier = CityTourHtmlSupplier.Replace("#TourBookingService", "Tour");
                            CityTourHtmlSupplier = CityTourHtmlSupplier.Replace("#TourBookingStatus", Convert.ToString(drTourdata["BookingStatus"]));
                            CityTourHtmlSupplier = CityTourHtmlSupplier.Replace("#TourName", Convert.ToString(drTourdata["ServiceName"]));
                            CityTourHtmlSupplier = CityTourHtmlSupplier.Replace("#TourType", Convert.ToString(drTourdata["RoomType"]));
                            CityTourHtmlSupplier = CityTourHtmlSupplier.Replace("#Pickup", Convert.ToString(pickupdata));
                            CityTourHtmlSupplier = CityTourHtmlSupplier.Replace("#TourCityName", Convert.ToString(drTourdata["CityName"]));
                            CityTourHtmlSupplier = CityTourHtmlSupplier.Replace("#TourServiceType", "Tour");
                            CityTourHtmlSupplier = CityTourHtmlSupplier.Replace("#TourServiceDate", Convert.ToString(drTourdata["TravelDate"]));
                            CityTourHtmlSupplier = CityTourHtmlSupplier.Replace("#TourNoofAdult", Convert.ToString(drTourdata["Adult"]));
                            CityTourHtmlSupplier = CityTourHtmlSupplier.Replace("#TourNoofChild", Convert.ToString(drTourdata["Children"]));
                            CityTourHtmlSupplier = CityTourHtmlSupplier.Replace("#TourTotalPax", Convert.ToString(Convert.ToInt32(drTourdata["Adult"]) + Convert.ToInt32(drTourdata["Children"])));
                            CityTourHtmlSupplier = CityTourHtmlSupplier.Replace("#TourRemark", "");
                            CityTourHtmlMainSupplier += SupplierTourDetailTemplate + CityTourHtmlSupplier;
                            CityTourHtmlMainSupplier = SuppliercommonTemplate + CityTourHtmlMainSupplier;
                            try
                            {
                                try
                                {

                                    DataTable dtMail1 = await GetEmailIdsBranchWise(Convert.ToInt32(AgentId), countryid);
                                    if (dtMail1.Rows.Count > 0)
                                    {
                                        if (!String.IsNullOrEmpty(Convert.ToString(dtMail1.Rows[0]["BranchEmail"])))
                                            FromEmail = Convert.ToString(dtMail1.Rows[0]["BranchEmail"]);
                                        if (!String.IsNullOrEmpty(Convert.ToString(dtMail1.Rows[0]["EmployeeEmailId"])))
                                            EmpToEmailId = Convert.ToString(dtMail1.Rows[0]["EmployeeEmailId"]);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    _ = _exception.ErrorNew(ex, _LogAPI.CancelBooking);
                                }
                                DataRow[] drCityTourSupplier = dsSupplierEmail.Tables[0].Select("ServiceType='Tour'");
                                if (drCityTourSupplier.Length > 0)
                                {
                                    if (!string.IsNullOrEmpty(FromEmail))
                                        FromEmail = FromEmail + "," + EmpToEmailId;
                                    else
                                        FromEmail = EmpToEmailId;
                                    string supplieremailid = Convert.ToString(drCityTourSupplier[0]["EmailId"]);
                                    emailList.Add(new Email_Details
                                    {
                                        FromId = EmpToEmailId,
                                        ToId = supplieremailid,
                                        CcId = FromEmail,
                                        BccId = "",
                                        Subject = SuppplierSubject,
                                        EmailText = CityTourHtmlMainSupplier,
                                        EmailType = "Booking Cancellation Mail",
                                        ServiceType = "City Tour",
                                        //Website = Convert.ToString(HttpContext.Current.Request.Url.Host),
                                        Website = Convert.ToString(hostname),
                                        CompanyId = 1,// Convert.ToInt32(System.Web.HttpContext.Current.Session["CompanyId"]),
                                        IsRayna = 1,// Convert.ToInt32(Common.getSession("IsRayna") == "true" ? 1 : 0),
                                        UserHost = _keySettings.WLUserHost,
                                        UserPassword = _keySettings.WLUserPassword,
                                        UserPort = _keySettings.WLUserPort,
                                        UserEmail = _keySettings.WLUserEmail,
                                        CompanyName = _keySettings.WLUserCompanyName,
                                        refno = strRefNo
                                    });
                                }
                            }
                            catch (Exception ex)
                            {
                                _ = _exception.ErrorNew(ex, _LogAPI.CancelBooking);
                            }
                            #endregion
                        }


                    }
                    #endregion

                    #region Hotel
                    string HotelHtmlMain = string.Empty;
                    // ExManager.Exception.LogException(drHotel.Length + "Booking Error Mail length");
                    if (drHotel.Length > 0)
                    {
                        //int TotalNoOfNights = 0;
                        int TotalNoofAdult = 0;
                        int TotalNoofChild = 0;
                        string RoomCategory = "";
                        foreach (DataRow drHoteldata in drHotel)
                        {
                            int room = 0;
                            int TotalRoom = 0;
                            int TotalNoOfNights = 0;
                            DataRow[] drhotelnew = dsv.Tables[0].Select("ServiceType='Hotel' and BookingId='" + Convert.ToString(drHoteldata["OnlineBookingMasterId"]) + "'");
                            string HotelTemplate1New = string.Empty;

                            string RoomTemplate = string.Empty;
                            HotelTemplate1New = HotelTemplate1;
                            foreach (DataRow drHoteldatanew in drhotelnew)
                            {
                                if (countryid == 0)
                                    countryid = Convert.ToInt32(drHoteldatanew["CountryId"]);
                                string HotelTemplate2New = string.Empty;
                                HotelTemplate2New = HotelTemplate2;
                                room += 1;
                                TotalRoom += Convert.ToInt32(drHoteldatanew["NoOfNights"]);
                                TotalNoOfNights = Convert.ToInt32(drHoteldatanew["NoOfNights"]);
                                TotalNoofAdult += Convert.ToInt32(drHoteldatanew["Adult"]);
                                TotalNoofChild += Convert.ToInt32(drHoteldatanew["Children"]);
                                RoomCategory += Convert.ToString(drHoteldatanew["RoomType"]) + ",";

                                //DataRow[] drpassenger = dsv.Tables[1].Select("BookingDetailId='" + Convert.ToString(drHoteldatanew["BookingDetailId"]) + "'");

                                HotelTemplate1New = HotelTemplate1New.Replace("#HotelBookingStatusColor", "red");
                                HotelTemplate1New = HotelTemplate1New.Replace("#HotelBookingStatus", Convert.ToString(drHoteldatanew["BookingStatus"]));

                                HotelTemplate2New = HotelTemplate2New.Replace("#RoomNo", Convert.ToString(room));
                                HotelTemplate2New = HotelTemplate2New.Replace("#RoomName", Convert.ToString(drHoteldatanew["RoomType"]));
                                HotelTemplate2New = HotelTemplate2New.Replace("#RoomAdult", Convert.ToString(drHoteldatanew["Adult"]));
                                HotelTemplate2New = HotelTemplate2New.Replace("#RoomChild", Convert.ToString(drHoteldatanew["Children"]));
                                HotelTemplate2New = HotelTemplate2New.Replace("#Board", Convert.ToString(drHoteldatanew["Board"]));
                                HotelTemplate2New = HotelTemplate2New.Replace("#RoomGuestName", Convert.ToString(drHoteldatanew["Name"]));

                                DataRow[] hotelCancellation = dsv.Tables[2].Select("BookingDetailId='" + Convert.ToString(drHoteldatanew["BookingDetailId"]) + "'");
                                string strHotelCancellation = string.Empty;

                                foreach (DataRow drhotelCancellation in hotelCancellation)
                                {
                                    strHotelCancellation += Convert.ToString(drHoteldatanew["ServiceName"]) + " From " + Convert.ToString(drhotelCancellation["FromDate"]) + " To" + Convert.ToString(drhotelCancellation["ToDate"]) + " the cancellation charges are " + Convert.ToString(Math.Round(Convert.ToDouble(drhotelCancellation["CancellationAmount"]), 2)) + " " + Convert.ToString(drHoteldatanew["AgentCurrencyId"]) + "<br/>";
                                }
                                HotelTemplate2New = HotelTemplate2New.Replace("#HotelCancellationPolicy", strHotelCancellation);

                                if (room == drhotelnew.Length)
                                {
                                    HotelTemplate1New = HotelTemplate1New.Replace("#HotelName", Convert.ToString(drHoteldatanew["ServiceName"]));
                                    HotelTemplate1New = HotelTemplate1New.Replace("#CityName", Convert.ToString(drHoteldatanew["CityName"]));
                                    HotelTemplate1New = HotelTemplate1New.Replace("#CityName", Convert.ToString(drHoteldatanew["CityName"]));
                                    HotelTemplate1New = HotelTemplate1New.Replace("#Checkindate", Convert.ToString(drHoteldatanew["TravelDate"]));
                                    HotelTemplate1New = HotelTemplate1New.Replace("#Checkoutdate", Convert.ToString(drHoteldatanew["CheckOutDate"]));
                                    HotelTemplate1New = HotelTemplate1New.Replace("#Nights", Convert.ToString(TotalNoOfNights));
                                    HotelTemplate1New = HotelTemplate1New.Replace("#RoomCategory", RoomCategory);
                                    HotelTemplate1New = HotelTemplate1New.Replace("#NoofRoom", Convert.ToString(TotalRoom));
                                    HotelTemplate1New = HotelTemplate1New.Replace("#HotelAdult", Convert.ToString(TotalNoofAdult));
                                    HotelTemplate1New = HotelTemplate1New.Replace("#HotelChild", Convert.ToString(TotalNoofChild));
                                }

                                RoomTemplate += HotelTemplate2New;
                            }
                            HotelHtmlMain += HotelTemplate1New + RoomTemplate;
                        }
                        await SendEmailToSupplier(ReferenceNo, 127, dsv, AgentId, "", CancelOldBooking);
                    }
                    #endregion


                    //if (Convert.ToInt32(_keySettings.IsLive) == 1)
                    //    await SaveMailSendingDetails(emailList, 0);


                    commonTemplate = commonTemplate + HotelHtmlMain + CityTourHtmlMain + FooterTemplate;
                    finalstring = commonTemplate;

                    DataTable dtMail2 = await GetEmailIdsBranchWise(Convert.ToInt32(AgentId), countryid);

                    if (dtMail2.Rows.Count > 0)
                    {
                        if (!String.IsNullOrEmpty(Convert.ToString(dtMail2.Rows[0]["BranchEmail"])))
                            branchemail = Convert.ToString(dtMail2.Rows[0]["BranchEmail"]);
                        if (!String.IsNullOrEmpty(Convert.ToString(dtMail2.Rows[0]["EmployeeEmailId"])))
                            CCEmail = Convert.ToString(dtMail2.Rows[0]["EmployeeEmailId"]);
                    }
                    if (!string.IsNullOrEmpty(SubagentEmailId))
                    {
                        toEmail = SubagentEmailId;
                        if (!string.IsNullOrEmpty(CCEmail))
                            CCEmail = CCEmail + "," + AgentEmailId;
                        else
                            CCEmail = AgentEmailId;
                    }
                    else if (!string.IsNullOrEmpty(SubUserEmailId))
                    {
                        toEmail = SubUserEmailId;
                        if (!string.IsNullOrEmpty(CCEmail))
                            CCEmail = CCEmail + "," + AgentEmailId;
                        else
                            CCEmail = AgentEmailId;
                    }
                    else
                        toEmail = AgentEmailId;


                    emailList.Add(new Email_Details
                    {
                        FromId = branchemail,
                        ToId = toEmail,
                        CcId = CCEmail,
                        BccId = "",
                        Subject = Subject,
                        EmailText = finalstring,
                        EmailType = "Booking",
                        ServiceType = "Hotel",
                        Website = Convert.ToString(hostname),
                        CompanyId = 1,// Convert.ToInt32(System.Web.HttpContext.Current.Session["CompanyId"]),
                        IsRayna = 1,// Convert.ToInt32(Common.getSession("IsRayna") == "true" ? 1 : 0),
                        UserHost = _keySettings.WLUserHost,
                        UserPassword = _keySettings.WLUserPassword,
                        UserPort = _keySettings.WLUserPort,
                        UserEmail = _keySettings.WLUserEmail,
                        CompanyName = _keySettings.WLUserCompanyName,
                        refno = ReferenceNo
                    });


                    if (Convert.ToInt32(_keySettings.IsLive) == 1)
                    {
                        if (AgentId == 9576 && CancelOldBooking == 1)
                        {
                        }
                        else
                        {
                            await SaveMailSendingDetails(emailList);
                        }
                    }
                }


            }

            catch (Exception ex)
            {
                _ = _exception.ErrorNew(ex, _LogAPI.CancelBooking);
            }
        }

        public async Task<DataSet> GetCancellationData(int BookingId)
        {
            try
            {
                SqlParameter[] para = new SqlParameter[]{
                new SqlParameter("@BookingId",BookingId)
                };
                return await _dlConnection.GetDataSet("GetCancellationPolicyDetailsRecord_B2B", para);
            }
            catch (Exception ex)
            {
                //var trace = new System.Diagnostics.StackTrace(ex, true); var stackFrame = trace.GetFrame(trace.FrameCount - 1);
                //Common.getException(ex, "Page:" + stackFrame.GetFileName() + " Method:" + stackFrame.GetMethod().Name + " Line No:" + stackFrame.GetFileLineNumber(), Convert.ToString(ex.GetType().Name), "TMS_Mobile 2015" + ex.Message);
                _ = _exception.ErrorNew(ex, _LogAPI.CancelBooking);
                return null;
            }
        }

        public void ThirdPartyCancellationHistory(string UniqueNo, string AgentUniqueNo, string XMLRequest, string XMLResponse, string Stage, string Provider, string StatusBooking, string ReferenceNo, string Website)
        {
            if (true)
            {
                try
                {

                    Task.Run(() => SaveThirdPartyCancellationHistory(UniqueNo, AgentUniqueNo, XMLRequest, XMLResponse, Stage, Provider, StatusBooking, ReferenceNo, Website));

                }
                catch (Exception x)
                {
                    _ = _exception.ErrorBooking(x, "", "ThirdPartyCancellationHistory_task");
                }
            }
            else
            {
                //SqlConnection conn1 = new SqlConnection(System.Web.Configuration.WebConfigurationManager.ConnectionStrings["ConnectionString"].ToString());
                SqlConnection conn1 = new SqlConnection(_connectionStrings.connectionStr_Logserver2_TP);//@Newadded on 12-mar-2024
                try
                {
                    Website = Website + " [API]";//Added on 4-APR-2024
                    conn1.Open();
                    SqlCommand comm1 = new SqlCommand();
                    comm1.Connection = conn1;
                    //comm1.CommandText = "insert into ThirdPartyCancellationHistory (TokenNo,AgentTokenNo,XMLRequest,XMLResponse,Stage,Provider,StatusBooking,ReferenceNo,Website,BookingDate) values('" + UniqueNo + "','" + AgentUniqueNo + "','" + ConfirmationBookingRequest + "','" + "-" + "','" + "Before Request hit booking" + "','" + SupplierData + "','" + "Request Confirm" + "','" + Convert.ToString(_objHotel.PNRNo) + "','" + "B2C" + "',getdate())";
                    comm1.CommandText = "insert into ThirdPartyCancellationHistory (TokenNo,AgentTokenNo,XMLRequest,XMLResponse,Stage,Provider,StatusBooking,ReferenceNo,Website,BookingDate) values('" + UniqueNo + "','" + AgentUniqueNo + "','" + XMLRequest.Replace("'", "''").Replace(",", "") + "','" + XMLResponse.Replace("'", "''").Replace(",", "") + "','" + Stage + "','" + Provider + "','" + StatusBooking + "','" + ReferenceNo + "','" + Website + "',getdate())";
                    comm1.ExecuteScalar();
                    //conn1.Close();
                }
                catch (Exception Ex)
                {
                    _ = _exception.ErrorBooking(Ex, "", "ThirdPartyCancellationHistory");
                }
                finally
                {
                    conn1.Close();
                }
            }
        }

        public void SaveThirdPartyCancellationHistory(string UniqueNo, string AgentUniqueNo, string XMLRequest, string XMLResponse, string Stage, string Provider, string StatusBooking, string ReferenceNo, string Website)
        {
            //SqlConnection conn1 = new SqlConnection(System.Web.Configuration.WebConfigurationManager.ConnectionStrings["ConnectionString"].ToString());
            SqlConnection conn1 = new SqlConnection(_connectionStrings.connectionStr_Logserver2_TP);//@Newadded on 12-mar-2024
            try
            {
                Website = Website + " [API]";//Added on 4-APR-2024
                conn1.Open();
                SqlCommand comm1 = new SqlCommand();
                comm1.Connection = conn1;
                //comm1.CommandText = "insert into ThirdPartyCancellationHistory (TokenNo,AgentTokenNo,XMLRequest,XMLResponse,Stage,Provider,StatusBooking,ReferenceNo,Website,BookingDate) values('" + UniqueNo + "','" + AgentUniqueNo + "','" + ConfirmationBookingRequest + "','" + "-" + "','" + "Before Request hit booking" + "','" + SupplierData + "','" + "Request Confirm" + "','" + Convert.ToString(_objHotel.PNRNo) + "','" + "B2C" + "',getdate())";
                comm1.CommandText = "insert into ThirdPartyCancellationHistory (TokenNo,AgentTokenNo,XMLRequest,XMLResponse,Stage,Provider,StatusBooking,ReferenceNo,Website,BookingDate) values('" + UniqueNo + "','" + AgentUniqueNo + "','" + XMLRequest.Replace("'", "''").Replace(",", "") + "','" + XMLResponse.Replace("'", "''").Replace(",", "") + "','" + Stage + "','" + Provider + "','" + StatusBooking + "','" + ReferenceNo + "','" + Website + "',getdate())";
                comm1.ExecuteScalar();
                //conn1.Close();
            }
            catch (Exception Ex)
            {
                _ = _exception.ErrorBooking(Ex, "", "ThirdPartyCancellationHistory");
            }
            finally
            {
                conn1.Close();
            }
        }

        public async Task<bool> CancelHotelBooking(int bookingId, decimal cancellationAmount, string cancelReason, decimal AgentCancellationAmount, string XMLRequest, string XMLResponse, bool IsAmend = false, string IP = "")
        {
            try
            {
                SqlParameter[] para = new SqlParameter[]{
                  new SqlParameter("@BookingDetailId",bookingId),
                  new SqlParameter("@CancellationAmount",cancellationAmount),
                  new SqlParameter("@cancelReason",Convert.ToString(cancelReason)==""||cancelReason==null?"change of plan ":cancelReason),
                  new SqlParameter("@AgentCancellationAmount",AgentCancellationAmount),
                  new SqlParameter("@XMLCancellationRequest",XMLRequest),
                  new SqlParameter("@XMLCancellationResponse",XMLResponse),
                  new SqlParameter("@IsAmend",IsAmend),
                  new SqlParameter("@HostIp",IP)//added on 27-sep-2023
                };
                await _dlConnection.GetExecuteScalarSP("CancelHotelBooking", para);
                return true;
            }
            catch (Exception ex)
            {
                _ = _exception.ErrorBooking(ex, "", Convert.ToString(bookingId));//Added on 30-DEC-2024
                //var trace = new System.Diagnostics.StackTrace(ex, true); var stackFrame = trace.GetFrame(trace.FrameCount - 1);
                //_ = _exception.getException(ex, "Page:" + stackFrame.GetFileName() + " Method:" + stackFrame.GetMethod().Name + " Line No:" + stackFrame.GetFileLineNumber(), Convert.ToString(ex.GetType().Name), "TMS_Mobile 2015" + ex.Message);
                _ = _exception.ErrorNew(ex, _LogAPI.CancelBooking);
                return false;
            }
        }

        public async Task<bool> CancelHotelBooking_PruvoBooking(int bookingId, decimal cancellationAmount, string cancelReason, decimal AgentCancellationAmount, string XMLRequest, string XMLResponse, bool IsAmend = false, string IP = "")
        {
            try
            {
                SqlParameter[] para = new SqlParameter[]{
                  new SqlParameter("@BookingDetailId",bookingId),
                  new SqlParameter("@CancellationAmount",cancellationAmount),
                  new SqlParameter("@cancelReason",Convert.ToString(cancelReason)==""||cancelReason==null?"change of plan ":cancelReason),
                  new SqlParameter("@AgentCancellationAmount",AgentCancellationAmount),
                  new SqlParameter("@XMLCancellationRequest",XMLRequest),
                  new SqlParameter("@XMLCancellationResponse",XMLResponse),
                  new SqlParameter("@IsAmend",IsAmend),
                  new SqlParameter("@HostIp",IP)//added on 27-sep-2023
                };
                await _dlConnection.GetExecuteScalarSP("CancelHotelBooking_PruvoBooking", para);
                return true;
            }
            catch (Exception ex)
            {
                //var trace = new System.Diagnostics.StackTrace(ex, true); var stackFrame = trace.GetFrame(trace.FrameCount - 1);
                //_ = _exception.getException(ex, "Page:" + stackFrame.GetFileName() + " Method:" + stackFrame.GetMethod().Name + " Line No:" + stackFrame.GetFileLineNumber(), Convert.ToString(ex.GetType().Name), "TMS_Mobile 2015" + ex.Message);
                _ = _exception.ErrorNew(ex, _LogAPI.CancelBooking);
                return false;
            }
        }

        public async Task<DataTable> GetCheckHotelCancellationCharges(int AgentId, Int64 bookingId, string SearchBy, string ReferenceNo, string APIInternalReference)
        {
            try
            {
                SqlParameter[] para = new SqlParameter[]{
                new SqlParameter("@AgentId",AgentId),
                new SqlParameter("@ReferenceNo",ReferenceNo),

                new SqlParameter("@SearchBy",SearchBy),
                new SqlParameter("@Bookingid",bookingId),
                new SqlParameter("@APIInternalReference",APIInternalReference)

                };
                DataTable dt = await _dlConnection.GetDataTableAsync("CheckHotelCancellationCharges_Xconnect", para);
                return dt;
            }
            catch (Exception ex)
            {
                _ = _exception.ErrorNew(ex, _LogAPI.CancelBooking);
                return null;
            }
        }
        public DataSet ReadDataFromJson(string jsonString)
        {
            var xd = new XmlDocument();
            jsonString = "{ \"rootNode\": {" + jsonString.Trim().TrimStart('{').TrimEnd('}') + @"} }";
            xd = Newtonsoft.Json.JsonConvert.DeserializeXmlNode(jsonString);
            var result = new DataSet();
            result.ReadXml(new XmlNodeReader(xd));
            return result;
        }
        public async Task<DataTable> ChangeBookingLimit_Staah(int RaynaId, string checkindate, string checkoutdate, string RoomNo, string RatePlanCode, string RoomTypeId)
        {
            DataTable dt = new DataTable();
            try
            {
                SqlParameter[] para = new SqlParameter[] {
                new SqlParameter("@RaynaId",RaynaId),
                new SqlParameter("@CheckInDate",Convert.ToDateTime(checkindate)),
                new SqlParameter("@CheckOutDate",Convert.ToDateTime(checkoutdate)),
                new SqlParameter("@RoomNo",RoomNo),
                new SqlParameter("@RatePlanCode",RatePlanCode),
                new SqlParameter("@RoomTypeId",RoomTypeId)
            };
                dt = await _dlConnection.GetDataTable_cm2("ChangeBookingLimit_Staah", para);
                return dt;
            }
            catch (System.Exception ex)
            {
                _ = _exception.Error(ex);
                return dt;

            }
        }

        public async Task<DataTable> ChangeBookingLimit_TravelClick(int RaynaId, string checkindate, string checkoutdate, string RoomNo, string RatePlanCode, string RoomTypeId)
        {
            DataTable dt = new DataTable();
            try
            {
                SqlParameter[] para = new SqlParameter[] {
                new SqlParameter("@RaynaId",RaynaId),
                new SqlParameter("@CheckInDate",Convert.ToDateTime(checkindate)),
                new SqlParameter("@CheckOutDate",Convert.ToDateTime(checkoutdate)),
                new SqlParameter("@RoomNo",RoomNo),
                new SqlParameter("@RatePlanCode",RatePlanCode),
                new SqlParameter("@RoomTypeId",RoomTypeId)
            };

                dt = await _dlConnection.GetDataTable_cm2("ChangeBookingLimit_TravelClick", para);
                return dt;
            }
            catch (Exception ex)
            {
                _ = _exception.Error(ex);
                return dt;
            }
        }

        public async Task<DataSet> GetShoppingCartTravelCart(string refno, Int64 Bookingid, string ApiInternalRef, string SearchBy, int AgentId)
        {
            try
            {
                SqlParameter[] para = new SqlParameter[]{
                new SqlParameter("@ReferenceNo",  refno),
                new SqlParameter("@Bookingid",  Bookingid),
                new SqlParameter("@AgentId",  AgentId),
                new SqlParameter("@SearchBy",  SearchBy),
                new SqlParameter("@APIInternalReference",  ApiInternalRef)
            };
                //return db.GetDataSet("GetShoppingTravelCart_Details_B2B", para);
                // return db.GetDataSet("GetShoppingTravelCart_Details_B2B_V4_Xconnect", para);
                // return db.GetDataSet("GetShoppingTravelCart_Details_B2B_V4_Xconnect_2023", para);
                return await _dlConnection.GetDataSet("GetShoppingTravelCart_Details_B2B_V4_Xconnect_2023_V1", para);//Optimization on 5-Aug-2023
            }
            catch (Exception ex)
            {
                _ = _exception.Error(ex);
                return null;
            }
        }

        public Int64 GetRandomNo()
        {
            Random r = new Random();
            return r.Next(10000, 99999);
        }
        public async Task<DataTable> ChangeBookingLimit_Siteminder(int RaynaId, string checkindate, string checkoutdate, string RoomNo, string RatePlanCode, string RoomTypeId)
        {
            DataTable dt = new DataTable();
            try
            {
                SqlParameter[] para = new SqlParameter[] {
                new SqlParameter("@RaynaId",RaynaId),
                new SqlParameter("@CheckInDate",Convert.ToDateTime(checkindate)),
                new SqlParameter("@CheckOutDate",Convert.ToDateTime(checkoutdate)),
                new SqlParameter("@RoomNo",RoomNo),
                new SqlParameter("@RatePlanCode",RatePlanCode),
                new SqlParameter("@RoomTypeId",RoomTypeId)
            };
                dt = await _dlConnection.GetDataTable_cm("ChangeBookingLimit_Siteminder", para); //13-Nov-2024 move to cm db
                return dt;
            }
            catch (System.Exception ex)
            {
                _ = _exception.Error(ex);
                return dt;

            }
        }

        public async Task<DataTable> GetHotelAllocationData_SiteMinder(int RaynaId, string checkindate, string checkoutdate, string RoomNo, string RatePlanCode, string RoomTypeId)
        {
            DataTable dt = new DataTable();
            try
            {
                SqlParameter[] para = new SqlParameter[] {
                new SqlParameter("@RaynaId",RaynaId),
                new SqlParameter("@CheckInDate",Convert.ToDateTime(checkindate)),
                new SqlParameter("@CheckOutDate",Convert.ToDateTime(checkoutdate)),
                new SqlParameter("@RoomNo",RoomNo),
                new SqlParameter("@RatePlanCode",RatePlanCode),
                new SqlParameter("@RoomTypeId",RoomTypeId)
            };
                dt = await _dlConnection.GetDataTable_cm("GetAllocationStatusForHotel_SiteMinder", para);
                return dt;
            }
            catch (System.Exception ex)
            {
                _ = _exception.Error(ex);
                return dt;

            }
        }
        public DataSet ReadXmlFileToDataSet(string Path = "")
        {
            DataSet dsReadXmlFile = new DataSet();
            try
            {
                dsReadXmlFile = ReadXmlFile(Path);
                return dsReadXmlFile;
            }
            catch (Exception ex)
            {
                return dsReadXmlFile;
            }
        }

        public DataTable GetApisoBookingData(int usercartdetailsid)
        {
            try
            {
                SqlParameter[] param = new SqlParameter[]
                 {
                new SqlParameter("@usercartdetailsid",usercartdetailsid)
                 };


                DataTable dtbookingdata = _dlConnection.GetDataTable("getApisobookingdata", param);
                return dtbookingdata;
            }
            catch (Exception ex)
            {
                _ = _exception.ErrorNew(ex, _LogAPI.Book);
                return null;
            }

        }

        public async Task UpdateTBOVoucherStatus(string referenceNumber, int UserCartDetailId, int TBOVoucherStatus)
        {
            try
            {
                SqlParameter[] para = new SqlParameter[]{
                new SqlParameter("@RTNo",  referenceNumber),
                new SqlParameter("@UserCartDetailId",  UserCartDetailId),
                new SqlParameter("@TBOVoucherStatus",  TBOVoucherStatus)

           };
                DataTable dtTBO1 = await _dlConnection.GetDataTableAsync("UpdateTBOVoucherStatus", para);
            }
            catch (Exception ex)
            {
                _ = _exception.ErrorNew(ex, _LogAPI.Book);
            }

        }


        public async Task SaveBillingPostingDetails(string refNo, int agentId, string bookingType, int bookingId, int detailId)
        {
            try
            {
                SqlParameter[] para = new SqlParameter[]{
                    new SqlParameter("@RefNo",refNo),
                    new SqlParameter("@AgentId",agentId),
                    new SqlParameter("@BookingType",bookingType),
                    new SqlParameter("@BookingId",bookingId),
                    new SqlParameter("@DetailId",detailId),
                 };

                await _dlConnection.ExecuteNonQuery("SaveBillPostingDetails", para);
            }
            catch (Exception ex)
            {
                _ = _exception.ErrorNew(ex, _LogAPI.Book);
            }
        }



        public async Task<string> AutoSettleCreditCardBooking(string rerefenceNo, string HostId = "")
        {
            try
            {
                SqlParameter[] para = new SqlParameter[]{
                 new SqlParameter("@RefNo", rerefenceNo),
                 new SqlParameter("@HostIP",HostId)
            };


                return Convert.ToString(await _dlConnection.GetExecuteScalarSP("AutoSettleCreditCardBooking", para));
            }
            catch (Exception ex)
            {
                _ = _exception.ErrorNew(ex, _LogAPI.Book);
                return "";
            }
        }

        public async Task updateHotelAllocation_OTHReplica(string referenceNo)
        {
            DataTable dtAllocation = generateHotelAllocationStructure_OTHReplica();
            try
            {
                SqlParameter[] para = new SqlParameter[]{
                 new SqlParameter("@ReferenceNo", referenceNo)
            };
                DataTable dt = (await _dlConnection.GetDataTableAsync("getHotelAllocation_OTHReplica", para));
                if (dt != null && dt.Rows.Count > 0)
                {
                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        DataRow _dr = dtAllocation.NewRow();
                        _dr["BookingDetailID"] = Convert.ToInt32(dt.Rows[i]["BookingDetailID"]);
                        _dr["RaynaID"] = Convert.ToInt32(dt.Rows[i]["RaynaID"]);
                        _dr["RoomNo"] = Convert.ToInt32(dt.Rows[i]["RoomNo"]);
                        _dr["AllocationDate"] = Convert.ToDateTime(dt.Rows[i]["AllocationDate"]);
                        _dr["AllocationSlabID"] = Convert.ToInt32(dt.Rows[i]["AllocationSlabID"]);
                        _dr["RoomTypeID"] = Convert.ToInt32(dt.Rows[i]["RoomTypeID"]);
                        _dr["ChannelId"] = Convert.ToInt32(dt.Rows[i]["ChannelId"]);
                        dtAllocation.Rows.Add(_dr);
                    }
                    #region "Insert allocation"
                    SqlParameter[] para1 = new SqlParameter[]{
                 new SqlParameter("@dtAllocation", dt)
                };
                    await _dlConnection.ExecuteNonQuery_OthReplica("InsertHotelAllocation_OTHReplica", para1);
                    #endregion
                }
            }
            catch (Exception ex)
            {
                _ = _exception.ErrorBooking(ex, "", "");
            }
        }

        public static DataTable generateHotelAllocationStructure_OTHReplica()
        {
            DataTable dt = new DataTable("HotelAllocation");
            dt.Columns.Add("BookingDetailID", typeof(int));
            dt.Columns.Add("RaynaID", typeof(int));
            dt.Columns.Add("RoomNo", typeof(int));
            dt.Columns.Add("AllocationDate", typeof(DateTime));
            dt.Columns.Add("AllocationSlabID", typeof(int));
            dt.Columns.Add("RoomTypeID", typeof(int));
            dt.Columns.Add("ChannelId", typeof(int));
            return dt;
        }

        public async Task<string> UpdateShoppingCart(DataTable AgentVoucher, DataTable dtCheckPriceReq, string refeno, DataTable dtallocation, DataTable dtRedeemPoint, string IsAmend, int BookingId = 0)
        {
            try
            {
                SqlParameter[] para = new SqlParameter[]{
                 new SqlParameter("@RefNo", refeno),
                 new SqlParameter("@AgentVoucher", AgentVoucher),
                 new SqlParameter("@CheckPriceReq", dtCheckPriceReq),
                  new SqlParameter("@HotelAllocationDetails", dtallocation),
                 new SqlParameter("@RedeemPoint", dtRedeemPoint),
                 new SqlParameter("@IsAmend", IsAmend),
                  new SqlParameter("@BookingId", BookingId)

            };

                //return Convert.ToString(db.GetExecuteScalarSP("UpdateShoppingCart_B2B_V4_Xconnect", para));
                return Convert.ToString(await _dlConnection.GetExecuteScalarSP("UpdateShoppingCart_B2B_V4_Xconnect_V1", para));//Optimized on 25-july-2023
            }
            catch (Exception ex)
            {
                _ = _exception.ErrorBooking(ex, "", "", refeno);
                return "Error in Update Booking";
            }
        }

        #region VCN

        public void updateCardOldData_Fail_ReBooking(string referenceNo, int bookingId, int detailId)
        {
            try
            {
                //DLConnection _db = new DLConnection();
                //SqlParameter[] para = new SqlParameter[] {
                //        new SqlParameter("@referenceNo",referenceNo),
                //        new SqlParameter("@bookingId",bookingId),
                //        new SqlParameter("@detailId",detailId),
                //    };
                //_db.ExecuteNonQuery("updateCardOldData_Fail_ReBooking", para);

            }
            catch (Exception ex)
            {

            }
        }
        public async Task<string> getWebResponse(string payload, string apiname)
        {
            try
            {
                string weburl = Convert.ToString(_keySettings.VCNAPI_EndPoint) + "/" + apiname;
                WebRequest req = WebRequest.Create(weburl);
                req.ContentType = "application/json";
                req.Method = "POST";
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                string data = payload;

                Stream reqStr = req.GetRequestStream();
                StreamWriter sw = new StreamWriter(reqStr);
                sw.Write(data);
                sw.Close();
                string strResult = "";

                try
                {
                    WebResponse res = await req.GetResponseAsync();
                    Stream resStr = res.GetResponseStream();
                    StreamReader sr = new StreamReader(resStr);
                    strResult = await sr.ReadToEndAsync();
                    sr.Close();
                    res.Close();
                }
                catch (WebException ex)
                {
                    string errMessage = ex.Message;
                    strResult = errMessage;
                    if (ex.Response != null)
                    {
                        System.IO.Stream resStr = ex.Response.GetResponseStream();
                        System.IO.StreamReader sr = new System.IO.StreamReader(resStr);
                        string soapResponse = await sr.ReadToEndAsync();
                        sr.Close();
                        resStr.Close();
                        errMessage += Environment.NewLine + soapResponse;
                        strResult = errMessage;
                        ex.Response.Close();
                    }
                }
                return strResult;
            }
            catch (System.Exception ex)
            {
                // Common.Exception.LogException("getWebResponse", ex.ToString());
                return "";
            }
        }
        #endregion
        public async Task<string> UpdateShoppingCart_Pruvo(DataTable AgentVoucher, DataTable dtCheckPriceReq, string refeno, DataTable dtallocation, DataTable dtRedeemPoint, string IsAmend, int BookingId = 0)
        {
            try
            {
                SqlParameter[] para = new SqlParameter[]{
                 new SqlParameter("@RefNo", refeno),
                 new SqlParameter("@AgentVoucher", AgentVoucher),
                 new SqlParameter("@CheckPriceReq", dtCheckPriceReq),
                  new SqlParameter("@HotelAllocationDetails", dtallocation),
                 new SqlParameter("@RedeemPoint", dtRedeemPoint),
                 new SqlParameter("@IsAmend", IsAmend),
                  new SqlParameter("@BookingId", BookingId)

            };

                //return Convert.ToString(db.GetExecuteScalarSP("UpdateShoppingCart_B2B_V4_Xconnect", para));
                return Convert.ToString(await _dlConnection.GetExecuteScalarSP("UpdateShoppingCart_B2B_V4_Xconnect_V1_Pruvo", para));//Optimized on 25-july-2023
            }
            catch (Exception ex)
            {
                _ = _exception.ErrorBooking(ex, "", "", refeno);
                return "Error in Update Booking";
            }
        }
        public async Task<bool> GetTypeUpdateRateKey_PriceTravel(int USercartID, string Type)
        {
            try
            {
                SqlParameter[] para = new SqlParameter[]{
           new SqlParameter("@Type", Type),
           new SqlParameter("@UserCartId", USercartID),
        };

                //dl.GetExecuteScalarQry("Insert into Log_Details_MB (servicename,title,status,details,datetime,source)values(@servicename,@title,@status,@details,@datetime,@source)", para);
                await _dlConnection.GetExecuteScalarQry("Update UserCartDetails set purchaseToken = @Type where UserCartId = @UserCartId", para);
                return true;
            }
            catch (Exception ex)
            {
                _ = _exception.ErrorNew(ex, _LogAPI.Prebook);
                return false;
            }
        }

        public async Task<bool> GetTypeUpdateStaus_PriceTravel(int USercartID, string Type)
        {
            try
            {
                SqlParameter[] para = new SqlParameter[]{
           new SqlParameter("@Type", Type),
           new SqlParameter("@UserCartId", USercartID),
        };

                //dl.GetExecuteScalarQry("Insert into Log_Details_MB (servicename,title,status,details,datetime,source)values(@servicename,@title,@status,@details,@datetime,@source)", para);
                await _dlConnection.GetExecuteScalarQry("Update UserCartDetails set EssentialInformation = @Type where UserCartId = @UserCartId", para);
                return true;
            }
            catch (Exception ex)
            {
                _ = _exception.ErrorNew(ex, _LogAPI.Prebook);
                return false;
            }
        }
        public async Task<bool> UpdatePricechangeNuitee(string HasKey, int USercartID)
        {
            try
            {
                SqlParameter[] para = new SqlParameter[]{
         new SqlParameter("@EachoToken",  HasKey),
         new SqlParameter("@UserCartId", USercartID),
        };

                await _dlConnection.GetExecuteScalarQry("Update UserCartDetails set EachoToken = @EachoToken where UserCartId = @UserCartId", para);
                return true;
            }
            catch (Exception ex)
            {
                _ = _exception.ErrorNew(ex, _LogAPI.Prebook);
                return false;
            }
        }
        public async Task<DataTable> GetRaynaAllocationData(int RaynaId, string checkindate, string checkoutdate, string RoomNo)
        {
            DataTable dt = new DataTable();
            try
            {
                SqlParameter[] para = new SqlParameter[] {
                new SqlParameter("@RaynaId",RaynaId),
                new SqlParameter("@CheckInDate",Convert.ToDateTime(checkindate)),
                new SqlParameter("@CheckOutDate",Convert.ToDateTime(checkoutdate)),
                new SqlParameter("@RoomNo",RoomNo)
            };
                dt = await _dlConnection.GetDataTable_OTHDB("GetAllocationStatusForHotel", para);
                return dt;
            }
            catch (System.Exception ex)
            {
                _ = _exception.ErrorNew(ex, _LogAPI.Book);
                return dt;

            }
        }

        public async Task<int> GetSupplierforgimmonix(string SupplierName)
        {
            int supplieridgimmonix = 0;
            try
            {
                SqlParameter[] para = new SqlParameter[]{
                 new SqlParameter("@SupplierName",  SupplierName)

            };


                DataSet ds = await _dlConnection.GetDataSet("GetSupplierIdGimmonix", para);
                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                {

                    supplieridgimmonix = Convert.ToInt32(ds.Tables[0].Rows[0]["gimmonixid"]);
                }

            }
            catch (Exception ex)
            {
                _ = _exception.ErrorBooking(ex, "");

                return supplieridgimmonix;
            }
            return supplieridgimmonix;
        }
        public static DateTime GetTimeZoneDateTour(string strDate)
        {
            string[] str = strDate.Split('/');
            DateTime tmpEntryDate = new DateTime(Convert.ToInt32(str[0]), Convert.ToInt32(str[1]), Convert.ToInt32(str[2]), Convert.ToInt32(str[3]), Convert.ToInt32(str[4]), 0);
            return tmpEntryDate;
        }

        public DataTable GeneratepackagePolicy()
        {
            try
            {
                DataTable dtHotel = new DataTable();
                DataColumn dcColumn;
                dcColumn = new DataColumn("FromDate", typeof(DateTime));
                dtHotel.Columns.Add(dcColumn);
                dcColumn = null;

                dcColumn = new DataColumn("ToDate", typeof(DateTime));
                dtHotel.Columns.Add(dcColumn);

                dcColumn = new DataColumn("Amount", typeof(decimal));
                dtHotel.Columns.Add(dcColumn);

                dcColumn = new DataColumn("Service", typeof(string));
                dtHotel.Columns.Add(dcColumn);

                dcColumn = new DataColumn("ServiceId", typeof(int));
                dtHotel.Columns.Add(dcColumn);


                return dtHotel;
            }
            catch (Exception ex)
            {
                _ = _exception.Error(ex);
            }
            return null;
        }

        public async Task<string> SaveShoppingCart(string APIInternalReference, SaveBookingDatabase item, DataTable dtPassengers, DataTable dtTourCancellation, TokenData TokenData, DataTable dtAgentVoucher, DataTable dtHotelAllocation, string CurrentDate, DataTable DtHotelRateBreakeUp, DataTable dtCheckPriceReqStructure)
        {
            try
            {
                DateTime curDate = GetTimeZoneDateTour(CurrentDate);
                //string CreditCardCharges = Convert.ToString(WebConfigurationManager.AppSettings["CreditCardCharges"]);
                ////var aa 1=b2b,2=b2c,0=both;
                //if (Convert.ToInt32(item.IsBothB2BB2C) == 2)
                //{
                //	CreditCardCharges = "0";
                //}
                DataTable DtPolicy = GeneratepackagePolicy();
                string CreditCardCharges = Convert.ToString(_keySettings.CreditCardCharges);// "3"// Convert.ToString(WebConfigurationManager.AppSettings["CreditCardCharges"]); //need to add key in future

                if (item.CreditCardCharges == null)
                {
                    if ((item.IsBothB2BB2C) == 2)
                    {
                        CreditCardCharges = "0";
                    }
                }
                else
                {
                    CreditCardCharges = Convert.ToString(item.CreditCardCharges);
                }

                SqlParameter[] para = new SqlParameter[]{
                 new SqlParameter("@UserCartId", item.GuestUserId),
                 new SqlParameter("@LocalCurrency", item.LocalCurrency),
                 new SqlParameter("@LocalCurrencyFactor", item.LocalCurrencyFactor),
                 new SqlParameter("@GuestUserId",item.GuestUserId),
                 new SqlParameter("@AgentId",item.AgentId ),
                 new SqlParameter("@Passengers", dtPassengers),
                 new SqlParameter("@TourCancellation", dtTourCancellation),
                 new SqlParameter("@TransactionId", item.ShoppingTransNo),
                 new SqlParameter("@PayId", ""),
                 new SqlParameter("@CardType", ""),
                 new SqlParameter("@PaymentType", (item.PaymentType !=null ? item.PaymentType : "1")),//#here1,
                 new SqlParameter("@PaymentComesFrom",""),
                 new SqlParameter("@CreditCardCharges",CreditCardCharges),
                 new SqlParameter("@InCancellation",0),
                 new SqlParameter("@AgentVoucher", dtAgentVoucher),
                 new SqlParameter("@HotelAllocationDetails", dtHotelAllocation),
                 new SqlParameter("@SubUserId", (item.SubUserId)),
                 new SqlParameter("@IsMobile",false),
                 new SqlParameter("@HostIp",item.HostIp),
                 new SqlParameter("@HostCity",item.HostCountry),
                 new SqlParameter("@IsVouchered",item.IsVouchered),
                 new SqlParameter("@BookingDate",curDate),//#here1,
				  new SqlParameter("@RateBreakUp", DtHotelRateBreakeUp),
                  new SqlParameter("@CheckPriceReq", dtCheckPriceReqStructure),
                 new SqlParameter("@IsConfirm",item.IsConfirm),//add
                 new SqlParameter("@QuotationName",item.QuotationName),//add
                   new SqlParameter("@packagePolicy",DtPolicy),//add
                  new SqlParameter("@AmendRef",""),//add
                  new SqlParameter("@APIInternalReference",APIInternalReference),//add
                  new SqlParameter("@FromAPIOut",1)//add



			};

                //return Convert.ToString(db.GetExecuteScalarSP("SaveShoppingCart_B2B_V11_Xconnect", para));    //Live on 03-Nov-2022 TickeID#23866
                //return Convert.ToString(db.GetExecuteScalarSP("SaveShoppingCart_B2B_V10", para));    //SaveShoppingCart_B2B_V9_api
                //return Convert.ToString(db.GetExecuteScalarSP("SaveShoppingCart_B2B_V13", para));    //optimized on 25 July 2023
                //return Convert.ToString(db.GetExecuteScalarSP("SaveShoppingCart_B2B_V14", para));    //16_Sep_2023 TicketID#32665
                //return Convert.ToString(db.GetExecuteScalarSP("SaveShoppingCart_B2B_V14", para));    //25_Oct_2023 #Tejas -for change regading agentwiseratebreakeup table entry if cityid not in _parentchildmaster
                //return Convert.ToString(db.GetExecuteScalarSP("SaveShoppingCart_B2B_V14_2024", para));    //18_JAN_2024 #Tejas -for change regading Duplicate referenceNo genrate Ticket#39421
                //return Convert.ToString(db.GetExecuteScalarSP("SaveShoppingCart_B2B_V14_2024", para)); //Regarding Booking.com K2 29_JAN-2024  //18_JAN_2024 #Tejas -for change regading Duplicate referenceNo genrate Ticket#39421
                return Convert.ToString(await _dlConnection.GetExecuteScalarSP("SaveShoppingCart_B2B_API_Live", para)); //Due to Ticket#60616 Added on 9-JUL-2025

            }
            catch (Exception ex)
            {
                _ = _exception.ErrorBooking(ex, TokenData.AgentId + TokenData.AgentCode);
                throw;
            }
        }

        public async Task<string> SaveShoppingCart_Pruvo(string APIInternalReference, SaveBookingDatabase item, DataTable dtPassengers, DataTable dtTourCancellation, TokenData TokenData, DataTable dtAgentVoucher, DataTable dtHotelAllocation, string CurrentDate, DataTable DtHotelRateBreakeUp, DataTable dtCheckPriceReqStructure, string OldBookRef, int OldBookingId)
        {
            try
            {
                DateTime curDate = GetTimeZoneDateTour(CurrentDate);
                //string CreditCardCharges = Convert.ToString(WebConfigurationManager.AppSettings["CreditCardCharges"]);
                ////var aa 1=b2b,2=b2c,0=both;
                //if (Convert.ToInt32(item.IsBothB2BB2C) == 2)
                //{
                //	CreditCardCharges = "0";
                //}
                DataTable DtPolicy = GeneratepackagePolicy();
                string CreditCardCharges = "3";// Convert.ToString(WebConfigurationManager.AppSettings["CreditCardCharges"]); //need to add key in future
                                               //
                if (item.CreditCardCharges == null)
                {
                    if ((item.IsBothB2BB2C) == 2)
                    {
                        CreditCardCharges = "0";
                    }
                }
                else
                {
                    CreditCardCharges = Convert.ToString(item.CreditCardCharges);
                }

                SqlParameter[] para = new SqlParameter[]{
                 new SqlParameter("@UserCartId", item.GuestUserId),
                 new SqlParameter("@LocalCurrency", item.LocalCurrency),
                 new SqlParameter("@LocalCurrencyFactor", item.LocalCurrencyFactor),
                 new SqlParameter("@GuestUserId",item.GuestUserId),
                 new SqlParameter("@AgentId",item.AgentId ),
                 new SqlParameter("@Passengers", dtPassengers),
                 new SqlParameter("@TourCancellation", dtTourCancellation),
                 new SqlParameter("@TransactionId", item.ShoppingTransNo),
                 new SqlParameter("@PayId", ""),
                 new SqlParameter("@CardType", ""),
                 new SqlParameter("@PaymentType", (item.PaymentType !=null ? item.PaymentType : "1")),//#here1,
                 new SqlParameter("@PaymentComesFrom",""),
                 new SqlParameter("@CreditCardCharges",CreditCardCharges),
                 new SqlParameter("@InCancellation",0),
                 new SqlParameter("@AgentVoucher", dtAgentVoucher),
                 new SqlParameter("@HotelAllocationDetails", dtHotelAllocation),
                 new SqlParameter("@SubUserId", (item.SubUserId)),
                 new SqlParameter("@IsMobile",false),
                 new SqlParameter("@HostIp",item.HostIp),
                 new SqlParameter("@HostCity",item.HostCountry),
                 new SqlParameter("@IsVouchered",item.IsVouchered),
                 new SqlParameter("@BookingDate",curDate),//#here1,
				  new SqlParameter("@RateBreakUp", DtHotelRateBreakeUp),
                  new SqlParameter("@CheckPriceReq", dtCheckPriceReqStructure),
                 new SqlParameter("@IsConfirm",item.IsConfirm),//add
                 new SqlParameter("@QuotationName",item.QuotationName),//add
                   new SqlParameter("@packagePolicy",DtPolicy),//add
                  new SqlParameter("@AmendRef",""),//add
                  new SqlParameter("@APIInternalReference",APIInternalReference),//add
                  new SqlParameter("@FromAPIOut",1),//add
                  new SqlParameter("@OldBookRef",OldBookRef),//add
                  new SqlParameter("@OldBookingId",OldBookingId)//added on 22-APR-2024


			};


                //return Convert.ToString(db.GetExecuteScalarSP("SaveShoppingCart_B2B_V11_Xconnect", para));    //Live on 03-Nov-2022 TickeID#23866
                //return Convert.ToString(db.GetExecuteScalarSP("SaveShoppingCart_B2B_V10", para));    //SaveShoppingCart_B2B_V9_api
                //return Convert.ToString(db.GetExecuteScalarSP("SaveShoppingCart_B2B_V13", para));    //optimized on 25 July 2023
                //return Convert.ToString(db.GetExecuteScalarSP("SaveShoppingCart_B2B_V14", para));    //16_Sep_2023 TicketID#32665
                //return Convert.ToString(db.GetExecuteScalarSP("SaveShoppingCart_B2B_V14_Pruvo", para));    //25_Oct_2023 #Tejas -for change regading agentwiseratebreakeup table entry if cityid not in _parentchildmaster
                return Convert.ToString(await _dlConnection.GetExecuteScalarSP("SaveShoppingCart_B2B_Pruvo_Live", para));   //Due to Ticket#60616 Added on 9-JUL-2025

            }
            catch (Exception ex)
            {
                _ = _exception.ErrorBooking(ex, TokenData.AgentId + TokenData.AgentCode);
                throw;
            }
        }

        public async Task<DataTable> GetEmailIds(int agentID)
        {
            try
            {
                SqlParameter[] para = new SqlParameter[]{
                  new SqlParameter("@AgentID", agentID)
             };
                return await _dlConnection.GetDataTableAsync("GetEmailIds", para);
            }
            catch (Exception ex)
            {
                _ = _exception.ErrorNew(ex, _LogAPI.Book);
                return null;
            }
        }

        public async Task<DataSet> GetUserCartDetailsBook(int GuestUserID, int AgentId, int UserCartId, string IP)
        {
            try
            {
                SqlParameter[] para = new SqlParameter[]{
                new SqlParameter("@GuestUserID",GuestUserID) ,
                new SqlParameter("@QuotationName", ""),
                  new SqlParameter("@AgentId",AgentId),
                  new SqlParameter("@UserCartId",UserCartId),
                  new SqlParameter("@Ip",IP),

                };

                //DataSet ds = db.GetDataSet("GetUserCartDetails", para);
                //DataSet ds = db.GetDataSet("GetUserCartDetails_V1_Xconnect_op", para);
                DataSet ds = await _dlConnection.GetDataSet_Limit("GetUserCartDetails_V1_Xconnect_op", para, 20);//Added on 4-JUN-2025 due to SP take time
                return ds;
            }
            catch (Exception ex)
            {
                _ = _exception.ErrorBooking(ex, Convert.ToString(AgentId), Convert.ToString(GuestUserID));
            }
            return null;
        }

        public async Task<int> GetReferenceByOtherIds_XConnect_Op(string refno, Int64 Bookingid, string ApiInternalRef, string SearchBy, int AgentId)
        {
            try
            {
                SqlParameter[] para = new SqlParameter[]{
                new SqlParameter("@ReferenceNo",  refno),
                new SqlParameter("@Bookingid",  Bookingid),
                new SqlParameter("@AgentId",  AgentId),
                new SqlParameter("@SearchBy",  SearchBy),
                new SqlParameter("@APIInternalReference",  ApiInternalRef)
                };
                //return Convert.ToInt32(db.GetExecuteScalarSP("GetReferenceByOtherIds_XConnect_Op", para));
                return Convert.ToInt32(await _dlConnection.GetExecuteScalarSP("GetReferenceByOtherIds_XConnect_Op_AgentWise", para));//@added on 24-JULY-2024
            }
            catch (Exception ex)
            {
                _ = _exception.ErrorNew(ex, _LogAPI.Recheck);
                return 1; // Exists or error
            }
        }
        public async Task<bool> UpdateBookingv3Token(string UserCartDetailId, string UserCartId, string SupplierName, string RoomTypeId, string token)
        {
            try
            {
                SqlParameter[] para = new SqlParameter[]{
            new SqlParameter("@UserCartDetailId", UserCartDetailId),
            new SqlParameter("@UserCartId", UserCartId),
            new SqlParameter("@SupplierName", SupplierName),
            new SqlParameter("@RoomTypeId",  RoomTypeId),
            new SqlParameter("@purchaseToken", token),

                };
                await _dlConnection.GetExecuteScalarQry("update usercartdetails  set purchaseToken = @purchaseToken where UserCartDetailId=@UserCartDetailId and UserCartId=@UserCartId and  SupplierName=@SupplierName and RoomTypeId=@RoomTypeId", para);

                return true;
            }
            catch (Exception ex)
            {
                _ = _exception.Error(ex);
                return false;
            }
        }

        public async Task<bool> UpdateDerbySoftPrice(string HasKey, int USercartID)
        {
            try
            {
                SqlParameter[] para = new SqlParameter[]{
                 new SqlParameter("@EachoToken",  HasKey),
                 new SqlParameter("@UserCartId", USercartID),
                    };

                await _dlConnection.GetExecuteScalarQry("Update UserCartDetails set EachoToken = @EachoToken where UserCartId = @UserCartId", para);
                return true;
            }
            catch (Exception ex)
            {
                _ = _exception.Error(ex);
                return false;
            }
        }



        public async Task<bool> GetEANBookLinkUpdate(string BookLink, int USercartID)
        {
            try
            {
                SqlParameter[] para = new SqlParameter[]{
      new SqlParameter("@purchaseToken",  BookLink),
      new SqlParameter("@UserCartId", USercartID),
     };

                await _dlConnection.GetExecuteScalarQry("Update UserCartDetails set purchaseToken = @purchaseToken where UserCartId = @UserCartId", para);
                return true;
            }
            catch (Exception ex)
            {
                _ = _exception.Error(ex);
                return false;
            }
        }

        public async Task<bool> GetEANOurProfitUpdate_mk(double OurProfit, int UserCartDetailId)
        {
            try
            {
                SqlParameter[] para = new SqlParameter[]{
      new SqlParameter("@OurProfit",  OurProfit),
      new SqlParameter("@UserCartDetailId", UserCartDetailId),
     };

                await _dlConnection.GetExecuteScalarQry("Update UserCartDetails set EachoToken = @OurProfit where UserCartDetailId = @UserCartDetailId", para);
                return true;
            }
            catch (Exception ex)
            {
                _ = _exception.Error(ex);
                return false;
            }
        }

        public async Task<bool> GetEANOurProfitUpdate(double OurProfit, int USercartID)
        {
            try
            {
                SqlParameter[] para = new SqlParameter[]{
      new SqlParameter("@OurProfit",  OurProfit),
      new SqlParameter("@UserCartId", USercartID),
     };

                await _dlConnection.GetExecuteScalarQry("Update UserCartDetails set OurProfit = @OurProfit where UserCartId = @UserCartId", para);
                return true;
            }
            catch (Exception ex)
            {
                _ = _exception.Error(ex);
                return false;
            }
        }

        public void MergePriceCheckData(ref DataSet DsPriceUpdate, int UserCartId, DataTable dtCartUpdateTable, DataTable dtPriceUpdateTable, double serviceTot)
        {
            DataRow drCartUpdate = dtCartUpdateTable.NewRow();
            drCartUpdate["UserCartId"] = UserCartId;// Convert.ToInt32(dsCartDetails.Tables[0].Rows[i]["UserCartID"]);
            drCartUpdate["ServiceTotal"] = serviceTot;
            dtCartUpdateTable.Rows.Add(drCartUpdate);
            DsPriceUpdate.Tables[0].Merge(dtCartUpdateTable);
            DsPriceUpdate.Tables[1].Merge(dtPriceUpdateTable);
        }

        public async Task<bool> GetHasKeyUpdateStaus(string HasKey, int USercartID)
        {
            try
            {
                SqlParameter[] para = new SqlParameter[]{
      new SqlParameter("@EachoToken",  HasKey),
      new SqlParameter("@UserCartId", USercartID),
     };


                await _dlConnection.GetExecuteScalarQry("Update UserCartDetails set EachoToken = @EachoToken where UserCartId = @UserCartId", para);
                return true;
            }
            catch (Exception ex)
            {
                _ = _exception.Error(ex);
                return false;
            }
        }

        public async Task<bool> GetBookingCodeUpdateStatus(string BookingCode, int UsercartID)
        {
            try
            {
                SqlParameter[] para = new SqlParameter[]
                {
                new SqlParameter("@spui",BookingCode),
                new SqlParameter("UserCartId",UsercartID)
                };

                await _dlConnection.GetExecuteScalarQry("Update UserCartDetails set spui=@spui where UserCartId=@UserCartId", para);
                return true;
            }
            catch (Exception ex)
            {
                _ = _exception.Error(ex);
                return false;
            }
        }

        public async Task<bool> GetHasKeyUpdateStaus_xtg(string HasKey, int USercartID)
        {
            try
            {
                SqlParameter[] para = new SqlParameter[]{
                        new SqlParameter("@EachoToken",  HasKey),
                        new SqlParameter("@UserCartId", USercartID),
            };

                await _dlConnection.GetExecuteScalarQry("Update UserCartDetails set purchaseToken = @EachoToken where usercartdetailid = @UserCartId", para);
                return true;
            }
            catch (Exception ex)
            {
                _ = _exception.Error(ex);
                return false;
            }
        }

        public async Task<string> SupplierPerformanceReportForPrebookForAutocutoff(string SupplierName, string ReportType, int AgentId, string ResponseTimeout, string ResponseType, string Request = "", string Response = "", string Error = "")
        {
            try
            {

                if (ReportType.ToLower() == "prebook")
                {
                    if (Request == "")
                    {
                        SqlParameter[] para = new SqlParameter[] {
                     new SqlParameter("@SupplierName",SupplierName),
                 new SqlParameter("@ReportType",ReportType),
                  new SqlParameter("@AgentID",AgentId),
                new SqlParameter("@ResponseTimeout",ResponseTimeout),
                new SqlParameter("@ResponseType",ResponseType),
                 new SqlParameter("@FromApi",1)

                };
                        DataSet abc = await _dlConnection.GetDataSet("SupplierPerformanceReportDataForBook_PreBook", para);
                        return "";
                    }
                    else
                    {
                        if (Error.ToLower().Contains("task was canceled") || Error.ToLower().Contains("no response was received during the time-out period for a request") || Error.ToLower().Contains("the request was aborted: the request was canceled."))
                        {
                            ResponseType = "Timeout";
                        }
                        else
                        {
                            if (Error != "")
                            {
                                ResponseType = "GeneralError";
                                Response = Response == "" ? Error : Error + " #Supplier Response" + Response;
                            }
                        }

                        SqlParameter[] para = new SqlParameter[] {
                        new SqlParameter("@SupplierName",SupplierName),
                        new SqlParameter("@ReportType",ReportType),
                        new SqlParameter("@AgentID",AgentId),
                        new SqlParameter("@ResponseTimeout",ResponseTimeout),
                        new SqlParameter("@ResponseType",ResponseType),
                        new SqlParameter("@Request",Request),
                        new SqlParameter("@Response",Response),
                        new SqlParameter("@FromPrebook",1),
                         new SqlParameter("@FromApi",1)
                };

                        DataSet abc = await _dlConnection.GetDataSet("SupplierPerformanceReportDataForBook_PreBook_v1", para);
                        return "";
                    }
                }
                else
                {
                    if (Error.ToLower().Contains("task was canceled") || Error.ToLower().Contains("no response was received during the time-out period for a request") || Error.ToLower().Contains("the request was aborted: the request was canceled."))
                    {
                        ResponseType = "Timeout";
                    }
                    else
                    {
                        if (Error != "")
                        {
                            ResponseType = "GeneralError";
                            Response = Response == "" ? Error : Error + " #Supplier Response" + Response;
                        }
                    }
                    //if (Request == "")
                    //{
                    //    SqlParameter[] para = new SqlParameter[] {
                    //         new SqlParameter("@SupplierName",SupplierName),
                    //     new SqlParameter("@ReportType",ReportType),
                    //      new SqlParameter("@AgentID",AgentId),
                    //    new SqlParameter("@ResponseTimeout",ResponseTimeout),
                    //    new SqlParameter("@ResponseType",ResponseType)
                    //    };
                    //    DBConnection db = new DBConnection();
                    //    //System.Diagnostics.st timerroomprocess1 = new System.Diagnostics.Stopwatch();
                    //    DataSet abc = db.GetDataSet("SupplierPerformanceReportDataForBook_PreBook", para);
                    //    return "";
                    //}
                    //else
                    {
                        SqlParameter[] para = new SqlParameter[] {
                        new SqlParameter("@SupplierName",SupplierName),
                        new SqlParameter("@ReportType",ReportType),
                        new SqlParameter("@AgentID",AgentId),
                        new SqlParameter("@ResponseTimeout",ResponseTimeout),
                        new SqlParameter("@ResponseType",ResponseType),
                        new SqlParameter("@Request",Request),
                        new SqlParameter("@Response",Response),
                         new SqlParameter("@FromApi",1)
                    };

                        DataSet abc = await _dlConnection.GetDataSet("SupplierPerformanceReportDataForBook_PreBook_v1", para);
                        return "";
                    }
                }
            }
            catch (Exception)
            {
                return "";
            }
            return "";
        }

        public DataRow AddCheckPriceReq(DataRow drTemp, int srno, int usercartid, int usercartdetailid, string roomtype, string suppliername, string req)
        {
            drTemp["PriceReqID"] = srno;
            drTemp["UserCartID"] = usercartid;
            drTemp["UserCartDetailsID"] = usercartdetailid;
            drTemp["RoomTypeID"] = roomtype;
            drTemp["SupplierName"] = suppliername;
            drTemp["CheckPriceReq"] = req;
            return drTemp;
        }

        public async Task<bool> GetRoomNameUpdate(string RoomName, int USercartID)
        {
            try
            {
                SqlParameter[] para = new SqlParameter[]{
      new SqlParameter("@purchaseToken",  RoomName),
      new SqlParameter("@UserCartId", USercartID),
     };

                //dl.GetExecuteScalarQry("Insert into Log_Details_MB (servicename,title,status,details,datetime,source)values(@servicename,@title,@status,@details,@datetime,@source)", para);
                await _dlConnection.GetExecuteScalarQry("Update UserCartDetails set purchaseToken = @purchaseToken where UserCartId = @UserCartId", para);
                return true;
            }
            catch (Exception ex)
            {
                _ = _exception.Error(ex);
                return false;
            }
        }

        public async Task<bool> DotwUpdateStaus(string NewAllocation, int USercartID)
        {
            try
            {
                SqlParameter[] para = new SqlParameter[]{
      new SqlParameter("@purchaseToken",  NewAllocation),
      new SqlParameter("@UserCartDeatilsId", USercartID),
     };

                //dl.GetExecuteScalarQry("Insert into Log_Details_MB (servicename,title,status,details,datetime,source)values(@servicename,@title,@status,@details,@datetime,@source)", para);
                await _dlConnection.GetExecuteScalarQry("Update UserCartDetails set purchaseToken = @purchaseToken where UserCartDetailId = @UserCartDeatilsId", para);
                return true;
            }
            catch (Exception ex)
            {
                _ = _exception.Error(ex);
                return false;
            }
        }

        public DataTable getPriceUpdateTable()
        {
            try
            {
                DataTable PriceUpdateTable = new DataTable();
                DataColumn dtClm = new DataColumn("UserCartId", typeof(int));
                PriceUpdateTable.Columns.Add(dtClm);

                dtClm = new DataColumn("UserCartDetailsId", typeof(int));
                PriceUpdateTable.Columns.Add(dtClm);

                dtClm = new DataColumn("SupplierName", typeof(string));
                PriceUpdateTable.Columns.Add(dtClm);

                dtClm = new DataColumn("RoomTypeID", typeof(string));
                PriceUpdateTable.Columns.Add(dtClm);

                dtClm = new DataColumn("CompanyBuyingPrice", typeof(double));
                PriceUpdateTable.Columns.Add(dtClm);
                dtClm = new DataColumn("CompanySellingPrice", typeof(double));
                PriceUpdateTable.Columns.Add(dtClm);
                dtClm = new DataColumn("AgentBuyingPrice", typeof(double));
                PriceUpdateTable.Columns.Add(dtClm);
                dtClm = new DataColumn("AgentSellingPrice", typeof(double));
                PriceUpdateTable.Columns.Add(dtClm);
                dtClm = new DataColumn("SubAgentBuyingPice", typeof(double));
                PriceUpdateTable.Columns.Add(dtClm);

                dtClm = new DataColumn("SubAgentSellingPrice", typeof(double));
                PriceUpdateTable.Columns.Add(dtClm);
                dtClm = new DataColumn("FinalSellingPrice", typeof(double));
                PriceUpdateTable.Columns.Add(dtClm);
                dtClm = new DataColumn("VatdiffAmount", typeof(string));
                PriceUpdateTable.Columns.Add(dtClm);


                dtClm = new DataColumn("PriceRQ", typeof(string));
                PriceUpdateTable.Columns.Add(dtClm);
                dtClm = new DataColumn("PriceRS", typeof(string));
                PriceUpdateTable.Columns.Add(dtClm);
                return PriceUpdateTable;
            }
            catch (Exception ex)
            {
                _ = _exception.Error(ex);
            }
            return null;
        }

        public DataTable getCardUpdateTable()
        {
            try
            {
                DataTable CardUpdateTable = new DataTable();
                DataColumn dtClm = new DataColumn("UserCartId", typeof(int));
                CardUpdateTable.Columns.Add(dtClm);
                dtClm = new DataColumn("ServiceTotal", typeof(double));
                CardUpdateTable.Columns.Add(dtClm);
                return CardUpdateTable;
            }
            catch (Exception ex)
            {
                _ = _exception.Error(ex);
            }
            return null;
        }

        public DataTable GetRoomPriceTableStructure()
        {
            try
            {
                DataTable dt = new DataTable();
                dt.Columns.Add("HotelName", typeof(string));
                dt.Columns.Add("RoomType", typeof(string));
                dt.Columns.Add("OldPrice", typeof(double));
                dt.Columns.Add("NewPrice", typeof(double));
                dt.Columns.Add("SupplierCurrency", typeof(string));

                dt.Columns.Add("IsRemove", typeof(bool));
                dt.Columns.Add("PackageId", typeof(int));
                dt.Columns.Add("UserCartId", typeof(int));
                dt.Columns.Add("ItemCount", typeof(int));
                DataColumn objSeqNo4 = new DataColumn();
                objSeqNo4.ColumnName = "ServiceType";
                objSeqNo4.DataType = typeof(string);
                objSeqNo4.DefaultValue = Convert.ToString("Hotel");
                dt.Columns.Add(objSeqNo4);
                return dt;
            }
            catch (Exception ex)
            {
                _ = _exception.Error(ex);
            }
            return null;
        }
        public async Task<DataSet> getCartHotelPrice(Int64 GuestUserID, int Agentid = 0, int UserCartID = 0)
        {
            try
            {
                SqlParameter[] para = new SqlParameter[]{
                 new SqlParameter("@GuestUserID",  GuestUserID),
                  new SqlParameter("@AgentId", Agentid),
                  new SqlParameter("@UserCartId", UserCartID),      // After change
            };
                DataSet ds = await _dlConnection.GetDataSet("GetCartDetails_Xconnect", para);
                return ds;
            }
            catch (Exception ex)
            {
                _ = _exception.Error(ex);
            }
            return null;
        }

        public async Task AUTO_DeactiveHotel(int Rayna_ID, int AgentId, string SupplierName, string APIName)
        {
            try
            {
                SqlParameter[] paraNew = new SqlParameter[]{
                                    new SqlParameter("@HotelId",  Rayna_ID),
                                    new SqlParameter("@AgentId",  AgentId),
                                    new SqlParameter("@SupplierName",  SupplierName),
                                    new SqlParameter("@APIName",  APIName)
                                    };
                int i = Convert.ToInt32(await _dlConnection.GetExecuteScalarSP("SaveDeactivehotelsupplierdata", paraNew)); // need to create
            }
            catch (Exception x)
            {
                _ = _exception.Error(x);
            }

            try
            {
                SqlParameter[] paraNewRP1 = new SqlParameter[]{
                                    new SqlParameter("@HotelId",  Rayna_ID),
                                    new SqlParameter("@AgentId",  AgentId),
                                    new SqlParameter("@SupplierName",  SupplierName),
                                    new SqlParameter("@APIName",  APIName)
                                    };
                int iRP1 = Convert.ToInt32(await _dlConnection.GetExecuteScalarSP_RP1("SaveDeactivehotelsupplierdata", paraNewRP1)); // need to create
            }
            catch (Exception x)
            {
                _ = _exception.Error(x);
            }
            try
            {
                SqlParameter[] paraNewRP2 = new SqlParameter[]{
                                    new SqlParameter("@HotelId",  Rayna_ID),
                                    new SqlParameter("@AgentId",  AgentId),
                                    new SqlParameter("@SupplierName",  SupplierName),
                                    new SqlParameter("@APIName",  APIName)
                                    };
                int iRP2 = Convert.ToInt32(await _dlConnection.GetExecuteScalarSP_RP2("SaveDeactivehotelsupplierdata", paraNewRP2)); // need to create
            }
            catch (Exception x)
            {
                _ = _exception.Error(x);
            }
            try
            {
                SqlParameter[] paraNewRP3 = new SqlParameter[]{
                                    new SqlParameter("@HotelId",  Rayna_ID),
                                    new SqlParameter("@AgentId",  AgentId),
                                    new SqlParameter("@SupplierName",  SupplierName),
                                    new SqlParameter("@APIName",  APIName)
                                    };
                int iRP3 = Convert.ToInt32(await _dlConnection.GetExecuteScalarSP_RP3("SaveDeactivehotelsupplierdata", paraNewRP3)); // need to create
            }
            catch (Exception x)
            {
                _ = _exception.Error(x);
            }
            try
            {
                SqlParameter[] paraNewRP4 = new SqlParameter[]{
                                    new SqlParameter("@HotelId",  Rayna_ID),
                                    new SqlParameter("@AgentId",  AgentId),
                                    new SqlParameter("@SupplierName",  SupplierName),
                                    new SqlParameter("@APIName",  APIName)
                                    };
                int iRP4 = Convert.ToInt32(await _dlConnection.GetExecuteScalarSP_RP4("SaveDeactivehotelsupplierdata", paraNewRP4)); // need to create
            }
            catch (Exception x)
            {
                _ = _exception.Error(x);
            }
        }

        public async Task<DataTable> CheckPrebookExistOrNot(DataTable dtCartTable, string SearchKey, string UniquiId)
        {
            try
            {
                SqlParameter[] paranew = new SqlParameter[]{
                    new SqlParameter("@dtCartTable",dtCartTable),
                    new SqlParameter("@SearchKey",SearchKey),
                    new SqlParameter("@UniquiId",UniquiId)
                };
                DataTable dt = await _dlConnection.GetDataTableAsync("CheckPrebookExistOrNot_Xconnect", paranew);
                return dt;
            }
            catch (Exception ex)
            {
                _ = _exception.Error(ex);
                return null;
            }
        }

        public DataTable GenerateCartTableForDatabase()
        {
            try
            {
                DataTable dt = new DataTable();
                dt.Columns.Add("UniqueID");
                dt.Columns.Add("SearchKey");
                dt.Columns.Add("RoomToken");
                dt.Columns.Add("PolicyToken");
                dt.Columns.Add("RateToken");
                dt.Columns.Add("RoomNo");
                dt.Columns.Add("UserCartId");
                dt.Columns.Add("GuestUserId");
                return dt;

            }
            catch (Exception ex)
            {
                _exception.Error(ex);
                return null;
            }
        }

        public async Task<string> GetUserCartId(int agentid)
        {
            var X = new MersenneTwister();
            int wait = X.Next(1, 500);
            Thread.Sleep(wait); //Max wait 500 Milisecond

            var r = new MersenneTwister(); //@new             
                                           //Random r = new Random();
            var randomId = r.Next(0, 2147483646);
            int i = await GetValidGuestUserId(Convert.ToInt32(randomId));
            while (i > 0)
            {
                randomId = r.Next(0, 2147483646);
                i = await GetValidGuestUserId(Convert.ToInt32(randomId));
            }
            return Convert.ToString(randomId);
        }

        public async Task<int> GetValidGuestUserId(int guestUserId)
        {
            try
            {
                SqlParameter[] para = new SqlParameter[]{
                 new SqlParameter("@GuestUserId",  Convert.ToInt32(guestUserId)),
            };
                int i = Convert.ToInt32(await _dlConnection.GetExecuteScalarSP("CheckValidGuestUserId", para));
                return i;
            }
            catch (System.Exception ex)
            {
                _ = _exception.Error(ex);
                return 0;
            }
        }
        public async Task<DataTable> GetDerbysoftPolicy(string HotelId, string RoomId, string RateId)
        {
            DataTable ds = new DataTable();
            try
            {
                SqlParameter[] para = new SqlParameter[]{
                 new SqlParameter("@HotelId",  HotelId),
                 new SqlParameter("@RoomId", RoomId),
                 new SqlParameter("@RateId", RateId),
                };

                ds = await _dlConnection.GetDataTable_Derbysoft("GetDerbysoftHotelPolicyAPI", para);
                return ds;
            }
            catch (Exception ex)
            {
                _ = _exception.Error(ex);
                return ds;
            }
        }




        public async Task<List<RoomBooking>> MongoDB_BookingCList(List<string> xmlhotelcode1)
        {

            try
            {
                //// Initialize MongoDB client and get the collection
                //var client = new MongoClient(mongoConnectionString);
                //var database = client.GetDatabase(mongoDatabaseName);
                var collection = _roomBookingC.GetCollection<BsonDocument>(_mongoSettings.MongoDB_CollectionName_BookingC);

                // Create filter using $in operator
                var filter = Builders<BsonDocument>.Filter.In("XmlId", xmlhotelcode1);

                // Fetch the matching records
                var results = collection.Find(filter).ToList();


                List<RoomBooking> roomBookings = results
         .Select(doc => BsonSerializer.Deserialize<RoomBooking>(doc))
         .ToList();







                return roomBookings;
            }
            catch (Exception ex)
            {
                //DtHotelInformation = new DataTable();
                await _exception.Error(ex);
                return null;
            }
        }

        public async Task<Dictionary<long, RoomBooking>> MongoDB_BookingCDictionary(List<string> xmlhotelcode1)
        {

            try
            {
                //// Initialize MongoDB client and get the collection
                //var client = new MongoClient(mongoConnectionString);
                //var database = client.GetDatabase(mongoDatabaseName);
                var collection = _roomBookingC.GetCollection<BsonDocument>(_mongoSettings.MongoDB_CollectionName_BookingC);

                // Create filter using $in operator
                var filter = Builders<BsonDocument>.Filter.In("XmlId", xmlhotelcode1);

                // Fetch the matching records
                var results = collection.Find(filter).ToList();


                var roomDict = results
        .Select(doc => BsonSerializer.Deserialize<RoomBooking>(doc))
        .ToDictionary(r => r.roomid);


                return roomDict;
            }
            catch (Exception ex)
            {
                //DtHotelInformation = new DataTable();
                await _exception.Error(ex);
                return null;
            }
        }

        public async Task<DataTable> MongoDB_BookingC(List<string> xmlhotelcode1)
        {

            try
            {
                //// Initialize MongoDB client and get the collection
                //var client = new MongoClient(mongoConnectionString);
                //var database = client.GetDatabase(mongoDatabaseName);
                var collection = _roomBookingC.GetCollection<BsonDocument>(_mongoSettings.MongoDB_CollectionName_BookingC);

                // Create filter using $in operator
                var filter = Builders<BsonDocument>.Filter.In("XmlId", xmlhotelcode1);

                // Fetch the matching records
                var results = collection.Find(filter).ToList();

                /// Insert results into a DataTable
                DataTable DtHotelInformation = ConvertToDataTable(results);




                return DtHotelInformation;
            }
            catch (Exception ex)
            {
                //DtHotelInformation = new DataTable();
                await _exception.Error(ex);
                return null;
            }
        }

        public async Task<ResponseDto> GetSupplierResponse(string strUniqueId, string strxmlRQRS, string strHost, Int32 TimeOut, string Provider = "", HotelSearchParameter para = null, bool DebugMode = false, string Searchkey = "")
        {
            System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
            timer.Start();
            //WebClient wc = new WebClient();
            //// wc.DownloadData(strHost, strxmlRQRS);
            //string resultData = wc.UploadString(strHost, "POST", strxmlRQRS);
            //return resultData;

            //SqlConnection conn1 = new SqlConnection(System.Web.Configuration.WebConfigurationManager.ConnectionStrings["ConnectionString"].ToString());
            //conn1.Open();
            //SqlCommand comm1 = new SqlCommand();
            //comm1.Connection = conn1;
            //comm1.CommandText = "insert into XMLRequestResponseTrack (XmlRequest,XmlResponse,ResponseDate,Provider,TypeRequest) values('" + strxmlRQRS + "','" + "" + "',getdate(),'','" + "Proper" + "')";
            //comm1.ExecuteScalar();
            //conn1.Close();

            string strOutput = "";
            string Error = "";
            ResponseDto responseDto = new ResponseDto();
            try
            {

                // App.Logger.Logger.Log("Request ::  " + strSupplier + " - " + strService);
                //strHost = strHost+ strxmlRQRS;

                HttpWebRequest Request;




                //strxmlRQRS = strxmlRQRS.Replace("&amp;", "&");
                //strxmlRQRS = strxmlRQRS.Replace("\n", string.Empty);
                //strxmlRQRS = strxmlRQRS.Replace("\t", string.Empty);
                //strxmlRQRS = strxmlRQRS.Replace(" ", string.Empty);




                Request = (HttpWebRequest)HttpWebRequest.Create(strHost);
                //string replacement = Regex.Replace(s, @"\t|\n|\r", ""); 

                Request.Timeout = TimeOut;
                Request.Accept = "text/xml";
                if (Provider.ToLower() == "gta")
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                Request.Method = "POST";

                Request.ContentType = "text/xml;charset=utf-8";

                Request.UserAgent = "Mozilla/4.0 (compatible; MSIE 6.0; MS Web Services Client Protocol 1.1.4322.2300";
                //----------Animesh Aox start


                if (strHost == XMLHotelBookingCommon.AOXSearch)
                {
                    Request.Headers.Add("SOAPAction", XMLHotelBookingCommon.AOXSearchAction);

                }
                if (strHost == XMLHotelBookingCommon.AOXReserve)
                {
                    Request.Headers.Add("SOAPAction", XMLHotelBookingCommon.AOXCancelAction);
                }
                ////------------Animesh Aox end

                //if (strSoapAction != string.Empty)
                //{
                //    Request.Headers.Add("SoapAction", strSoapAction);
                //}

                //if (strGzipCompression.Equals("Y"))
                //{
                Request.Headers.Add("Accept-Encoding", "gzip");
                //Request.Timeout = 10000;
                //  }




                //Forming the request
                Stream newStream = null;

                //if (strMethod.ToLower().Equals("post") && !strSoapAction.ToLower().Equals("application/x-www-form-urlencoded"))
                //{

                //bhai Request

                //Travoco Change Start

                //Travoco Change End



                byte[] data = Encoding.ASCII.GetBytes(strxmlRQRS);
                Request.ContentLength = data.Length;

                newStream = Request.GetRequestStream();
                newStream.Write(data, 0, data.Length);

                // }

                //Getting the response
                try
                {
                    HttpWebResponse Response = (HttpWebResponse)await Request.GetResponseAsync();


                    Stream responseStream = Response.GetResponseStream();

                    //if (Provider.ToLower() != "gta")
                    //{
                    //   // if (strHost != Darina_BookUrl)
                    //        responseStream = new GZipStream(responseStream, CompressionMode.Decompress);
                    //}
                    if (Response.ContentEncoding.ToLower().Contains("gzip"))
                        responseStream = new System.IO.Compression.GZipStream(responseStream, System.IO.Compression.CompressionMode.Decompress);
                    else if (Response.ContentEncoding.ToLower().Contains("deflate"))
                        responseStream = new System.IO.Compression.DeflateStream(responseStream, System.IO.Compression.CompressionMode.Decompress);

                    //Read the output

                    StreamReader Reader = new StreamReader(responseStream, Encoding.UTF8);

                    strOutput = await Reader.ReadToEndAsync();

                    responseStream.Close();
                    Reader.Close();
                    Response.Close();
                    timer.Stop();
                    if (para != null)
                    {
                        para.SupplierTime += ",SupplierName :" + Provider + " : " + "Execution Time :" + timer.Elapsed.ToString();
                        if (DebugMode)
                            _ = XMLRequestResponseTrack(strxmlRQRS, strOutput, Provider, "Proper");
                    }
                    if (_keySettings.IsLogTrack == "1")
                    {
                        TimeSpan timeTaken = timer.Elapsed;

                        _exception.ErrorLog(Convert.ToString(_keySettings.LogTrackPath), Provider + "->" + strHost + "-> Execution Time : " + timer.Elapsed.ToString());
                        _ = XMLRequestResponseTrack(strxmlRQRS, strOutput, Provider, "Proper");
                    }


                    //SqlConnection conn = new SqlConnection(System.Web.Configuration.WebConfigurationManager.ConnectionStrings["ConnectionString"].ToString());
                    //conn.Open();
                    //SqlCommand comm = new SqlCommand();
                    //comm.Connection = conn;
                    ////comm.CommandText = "insert into XMLRequestResponseTrack (XmlRequest,XmlResponse,ResponseDate,Provider,TypeRequest) values('" + strxmlRQRS + "','" + strOutput.Replace("'", "''") + "',getdate(),'','" + "Proper" + "')";
                    //comm.CommandText = "insert into XMLRequestResponseTrack (XmlRequest,XmlResponse,ResponseDate,Provider,TypeRequest) values('" + strxmlRQRS + "','" + strHost + "',getdate(),'','" + "Proper" + "')";
                    //comm.ExecuteScalar();
                    //conn.Close();
                }
                catch (WebException webExcp)
                {
                    try
                    {
                        HttpWebResponse httpResponse = (HttpWebResponse)webExcp.Response;
                        if (httpResponse.StatusDescription != null)
                        {
                            string result = httpResponse.StatusDescription.ToString();
                            string result1 = new StreamReader(webExcp.Response.GetResponseStream()).ReadToEnd();

                            //SqlConnection conn = new SqlConnection(System.Web.Configuration.WebConfigurationManager.ConnectionStrings["ConnectionString"].ToString());
                            //conn.Open();
                            //SqlCommand comm = new SqlCommand();
                            //comm.Connection = conn;
                            //comm.CommandText = "insert into XMLRequestResponseTrackError (XmlRequest,XmlResponse,ResponseDate,Provider,TypeRequest) values('" + strxmlRQRS + "','" + result1.Replace("'", "''").Replace(",", " ") + "',getdate(),'','" + "Proper" + "')";
                            //comm.ExecuteScalar();
                            //conn.Close();
                            _ = XMLRequestResponseTrackError(strxmlRQRS, result1, Provider, Searchkey);
                            Error = result1.ToString();

                            responseDto.Response = result1;
                            responseDto.Error = Error;
                            return responseDto;
                        }
                    }
                    catch (Exception ex)
                    {
                        _exception.Error(ex, Searchkey);
                        Error = ex.Message.ToString();

                        responseDto.Response = "";
                        responseDto.Error = Error;
                        return responseDto;
                    }

                    //return result1;
                }
            }
            catch (Exception ex)
            {
                Error = ex.Message.ToString();
                _ = _exception.Error(ex, Searchkey);
                //strOutput = ex.StackTrace;
                //Common.XMLRequestResponseTrackError(strxmlRQRS, strOutput, Provider, "Proper");
                //SqlConnection conn = new SqlConnection(System.Web.Configuration.WebConfigurationManager.ConnectionStrings["ConnectionString"].ToString());
                //conn.Open();
                //SqlCommand comm = new SqlCommand();
                //comm.Connection = conn;
                //comm.CommandText = "insert into XMLRequestResponseTrackError (XmlRequest,XmlResponse,ResponseDate,Provider,TypeRequest) values('" + strxmlRQRS + "','" + ex.Message.Replace("'", "''").Replace(",", " ") + "',getdate(),'','Proper')";
                //comm.ExecuteScalar();
                //conn.Close();

                //bool rethrow = ExceptionPolicy.HandleException(ex, EnumExceptionPolicyTypes.Type.Propagate_Policy); if (rethrow) { throw; }
            }
            //trackRequest(strxmlRQRS, strOutput);

            responseDto.Response = strOutput;
            responseDto.Error = Error;
            return responseDto;
        }

        public async Task<ResponseDto> AbreuOnlineResponse_Supplier(string Request_Text, string APIUrl, int SearchTimeOut, string SearchKey = "", string Provider = "", string UserId = "", string Stage = "")
        {
            ResponseDto responseDto = new ResponseDto();
            string responseString = "";
            string Error = "";
            try
            {
                string reqUrl = APIUrl;
                var request = (HttpWebRequest)WebRequest.Create(reqUrl);
                var postData = Request_Text;
                var data = Encoding.ASCII.GetBytes(postData);
                request.Proxy = null;
                request.UserAgent = "Mozilla/4.0 (compatible; MSIE 6.0; MS Web Services Client Protocol 1.1.4322.2300)";
                request.Method = "POST";
                request.Timeout = SearchTimeOut;
                request.ContentType = "text/xml;charset=utf-8;";
                request.KeepAlive = false;
                request.Headers.Add("Accept-Encoding", "gzip");
                request.ContentLength = data.Length;

                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

                if (Provider.ToLower() == "goglobal")
                {
                    request.KeepAlive = true;
                    ServicePointManager.Expect100Continue = true;
                    // ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12 | SecurityProtocolType.Ssl3;
                    request.ContentType = "application/soap+xml; charset=utf-8";
                    request.Headers.Add("Accept-Encoding", "gzip");
                    if (Stage.ToLower() == "policy")
                        request.Headers.Add("API-Operation", Convert.ToString(XMLHotelBookingCommon.PriceBreakDown));
                    else if (Stage.ToLower() == "checkprice")
                        request.Headers.Add("API-Operation", Convert.ToString(XMLHotelBookingCommon.BookingValuation));
                    else if (Stage.ToLower() == "booking")
                        request.Headers.Add("API-Operation", Convert.ToString(XMLHotelBookingCommon.BookingInsert));
                    else if (Stage.ToLower() == "cancel")
                        request.Headers.Add("API-Operation", Convert.ToString(XMLHotelBookingCommon.BookingCancellation));
                    else if (Stage.ToLower() == "voucher")
                        request.Headers.Add("API-Operation", Convert.ToString(XMLHotelBookingCommon.VoucherDetails));
                    else if (Stage.ToLower() == "bookingstatus")
                        request.Headers.Add("API-Operation", Convert.ToString(XMLHotelBookingCommon.BookingStatus));

                    request.Headers.Add("API-AgencyID", UserId);
                }

                using (var stream = request.GetRequestStream())
                {
                    stream.Write(data, 0, data.Length);
                }
                var response = (HttpWebResponse)await request.GetResponseAsync();
                Stream st = response.GetResponseStream();
                if (response.ContentEncoding.ToLower().Contains("gzip"))
                    st = new System.IO.Compression.GZipStream(st, System.IO.Compression.CompressionMode.Decompress);
                else if (response.ContentEncoding.ToLower().Contains("deflate"))
                    st = new System.IO.Compression.DeflateStream(st, System.IO.Compression.CompressionMode.Decompress);
                StreamReader stIn = new StreamReader(st, Encoding.Default);
                string respXml = Convert.ToString(await stIn.ReadToEndAsync());
                responseString = respXml;
                stIn.Close();
            }
            catch (Exception ex)
            {
                _ = _exception.Error(ex, SearchKey);
                Error = ex.Message;
            }
            responseDto.Response = responseString;
            responseDto.Error = Error;


            return responseDto;
        }

        public string GenerateGTARequest(HotelSearchParameter objSearchPara)
        {
            try
            {
                StringBuilder objRequest = new StringBuilder();
                objRequest.Append("<Hotel_Availability_GetAvailability>");
                objRequest.Append("<Param>");
                objRequest.Append("<RequestSection>");
                objRequest.Append("<Hotel>");
                objRequest.Append("<StayDateRange Start='" + objSearchPara.CheckInDate + "' End='" + objSearchPara.CheckOutDate + "' TotalNights='" + objSearchPara.NoofNights + "' />");
                //objRequest.Append("<StayDateRange Start='30/11/2015' End='01/12/2015' TotalNights='" + objSearchPara.NoofNights + "' />");
                objRequest.Append("<Currency>0</Currency>");
                objRequest.Append("<AgentPercentage>" + Convert.ToString(objSearchPara.AgentMarkup) + "</AgentPercentage>");
                objRequest.Append("<AgentCompanyPercentage>" + Convert.ToString(objSearchPara.AgentCompanyPercentage) + "</AgentCompanyPercentage>");
                objRequest.Append("<NoOfRecords>200</NoOfRecords>");
                objRequest.Append("<PageIndex>1</PageIndex>");
                objRequest.Append("<HotelSearchCriteria>");

                objRequest.Append("<HotelRef HotelCityName='" + objSearchPara.CityName + "' HotelCityCode='" + objSearchPara.CityID + "' HotelCountryCode='" + objSearchPara.CountryCode + "' Rating='' />");

                objRequest.Append("<Rooms>");
                //Loop through Rooms.
                var result = objSearchPara.Rooms.GroupBy(x => new { x.RoomName, x.NoofAdults, x.NoofChild, x.Child1Age, x.Child2Age }).Select(n => new { RoomName = n.Key.RoomName, NoofAdults = n.Key.NoofAdults, NoofChild = n.Key.NoofChild, ChildAge = n.ToList(), Child1Age = n.Key.Child1Age, Child2Age = n.Key.Child2Age, Count = n.Count() });
                foreach (var item in result)//For each Item
                {
                    for (int i = 0; i < item.Count; i++)
                    {
                        objRequest.Append("<Room Type='" + GetType(item.NoofAdults) + "' ChildCount='" + item.NoofChild + "' AdultsCount='" + item.NoofAdults + "' ExtraBed='0' RateBasis='10785'>");//Question: What is 10785
                        switch (item.NoofChild)
                        {
                            case 0:
                                objRequest.Append("<Ages></Ages>");
                                break;
                            case 1:
                                objRequest.Append("<Ages><Age>" + item.Child1Age.ToString() + "</Age></Ages>");
                                break;
                            case 2:
                                objRequest.Append("<Ages>");
                                objRequest.Append("<Age>" + item.Child1Age.ToString() + "</Age>");
                                objRequest.Append("<Age>" + item.Child2Age.ToString() + "</Age>");
                                objRequest.Append("</Ages>");
                                break;
                        }
                        objRequest.Append("</Room>");
                    }
                }
                //foreach (RoomDetails room in objSearchPara.Rooms)
                //{
                //    objRequest.Append("<Room Type='" + Common.GetType(room.NoofAdults) + "' ChildCount='" + room.NoofChild + "' AdultsCount='" + room.NoofAdults + "' ExtraBed='0' RateBasis='10785'>");//Question: What is 10785
                //    switch (room.NoofChild)
                //    {
                //        case 0:
                //            objRequest.Append("<Ages></Ages>");
                //            break;
                //        case 1:
                //            objRequest.Append("<Ages><Age>" + room.Child1Age.ToString() + "</Age></Ages>");
                //            break;
                //        case 2:
                //            objRequest.Append("<Ages>");
                //            objRequest.Append("<Age>" + room.Child1Age.ToString() + "</Age>");
                //            objRequest.Append("<Age>" + room.Child2Age.ToString() + "</Age>");
                //            objRequest.Append("</Ages>");
                //            break;
                //    }
                //    objRequest.Append("</Room>");
                //}
                objRequest.Append("</Rooms>");
                objRequest.Append("</HotelSearchCriteria>");

                objRequest.Append("<HotelAdvanacedSearchCriteria>");
                objRequest.Append("<HotelId>" + Convert.ToString(objSearchPara.HotelID) + "</HotelId>");
                objRequest.Append("<HotelName>" + objSearchPara.HotelName + "</HotelName>");
                objRequest.Append("<Available>2</Available>");
                objRequest.Append("<StarRatingId></StarRatingId>");
                objRequest.Append("<StarRating></StarRating>");

                objRequest.Append("<LocationId>" + objSearchPara.LocationID + "</LocationId>");
                objRequest.Append("<Location>" + objSearchPara.Location + " </Location>");

                objRequest.Append("</HotelAdvanacedSearchCriteria>");

                objRequest.Append("<IndianResident>true</IndianResident>");

                objRequest.Append("</Hotel>");
                objRequest.Append("</RequestSection>");

                objRequest.Append("<Header>");
                objRequest.Append("<IBNUniqueId>2131271b-e47f-4871-8daf-1d0093342169</IBNUniqueId>");
                if (objSearchPara.UserName == null)
                    objSearchPara.UserName = "";
                if (objSearchPara.UserName.ToLower() == "guest user")
                    objRequest.Append("<UserId></UserId>");
                else
                    objRequest.Append("<UserId>" + objSearchPara.UserName + "</UserId>");
                objRequest.Append("<Username>" + objSearchPara.BookForUserCode + "</Username>");
                objRequest.Append("<Password>abc@123</Password>");
                objRequest.Append("<UserTokenId>IBM</UserTokenId>");
                objRequest.Append("<IsB2bB2c>" + Convert.ToString(objSearchPara.IsB2BOrB2C) + "</IsB2bB2c>");
                objRequest.Append("<Nationality>" + objSearchPara.Nationality + "</Nationality>");
                objRequest.Append("<Residency>" + objSearchPara.Residency + "</Residency>");
                objRequest.Append("<Supplier>GT</Supplier>");
                objRequest.Append("</Header>");
                objRequest.Append("</Param>");
                objRequest.Append("</Hotel_Availability_GetAvailability>");
                return objRequest.ToString();
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
            }
            return string.Empty;
        }

        public async Task<ResponseDto> Response_Supplier(string Request_Text, string APIUrl, int SearchTimeOut, string SupplieName, HotelSearchParameter para = null, bool DebugMode = false, string Searchkey = "")
        {
            ResponseDto responseDto = new ResponseDto();
            string Error = "";
            try
            {

                System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
                timer.Start();


                WebRequest reqPOST = System.Net.WebRequest.Create(@"" + APIUrl + "");
                reqPOST.Method = "POST"; // Setting up Data transmitting method  into POST
                if (SupplieName.ToLower() == "travellanda" || SupplieName.ToLower() == "mgholiday")
                    reqPOST.Headers.Add("Accept-Encoding", "gzip");
                reqPOST.Timeout = SearchTimeOut; // Setting up connection time out
                reqPOST.ContentType = "application/x-www-form-urlencoded"; // show content type
                byte[] sentData = Encoding.UTF8.GetBytes(Request_Text); // Parameters
                reqPOST.ContentLength = sentData.Length;
                Stream sendStream = reqPOST.GetRequestStream();
                sendStream.Write(sentData, 0, sentData.Length);
                sendStream.Close();
                WebResponse result = reqPOST.GetResponse();
                String responseString;
                using (Stream stream = result.GetResponseStream())
                {
                    if (SupplieName.ToLower() == "travellanda" || SupplieName.ToLower() == "mgholiday")
                    {
                        HttpWebResponse Response = (HttpWebResponse)await reqPOST.GetResponseAsync();
                        Stream responseStream = Response.GetResponseStream();
                        responseStream = new GZipStream(responseStream, CompressionMode.Decompress);
                        StreamReader Reader = new StreamReader(responseStream, Encoding.UTF8);
                        responseString = await Reader.ReadToEndAsync();
                        responseStream.Close();
                        Reader.Close();
                        Response.Close();
                        timer.Stop();
                        if (para != null)
                        {
                            para.SupplierTime += ",SupplierName :" + SupplieName + " : " + "Execution Time :" + timer.Elapsed.ToString();
                            if (DebugMode)
                                _ = XMLRequestResponseTrack(Request_Text, responseString, SupplieName, "Proper");
                        }

                    }
                    else
                    {
                        StreamReader reader = new StreamReader(stream, Encoding.UTF8);
                        responseString = reader.ReadToEnd();
                        JObject jsonObject = JObject.Parse(responseString);
                    }
                }

                responseDto.Response = responseString;
                responseDto.Error = "";
                return responseDto;
            }
            catch (Exception ex)
            {
                Error = ex.Message;
                _ = _exception.Error(ex, Searchkey);
                responseDto.Error = "";

            }
            responseDto.Response = null;
            return responseDto;
        }

        public async Task<ResponseDto> GetTrvcoResponse_report(string request, string TravcoURL, Int32 TimeOut, string Provider = "", HotelSearchParameter para = null, bool DebugMode = false, string SearchKey = "")
        {
            ResponseDto responseDto = new ResponseDto();

            string Error = "";

            System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
            timer.Start();
            string StoreSupplierResErrorLog_TBO_API = Convert.ToString(_keySettings.StoreSupplierResErrorLog_TBO_API);

            string response = null;
            HttpWebRequest httpWebRequest = null;
            HttpWebResponse httpWebResponse = null;
            //Creates an HttpWebRequest for the specified URL.
            //httpWebRequest = (HttpWebRequest)WebRequest.Create("http://www.v3.staging.illusionswebservices.iwtx.com/illusionsHotelSearch.ashx");
            httpWebRequest = (HttpWebRequest)WebRequest.Create(TravcoURL);

            try
            {
                byte[] bytes;
                bytes = System.Text.Encoding.UTF8.GetBytes(request);
                //Set HttpWebRequest properties
                httpWebRequest.Method = "POST";
                httpWebRequest.Timeout = TimeOut;
                httpWebRequest.ContentLength = bytes.Length;
                ServicePointManager.DefaultConnectionLimit = 1000;
                httpWebRequest.ContentType = "application/x-www-form-urlencoded";
                if (Provider.ToLower() == "jactravel")
                {
                    //httpWebRequest.Headers.Add("Accept-Encoding", "gzip");
                    httpWebRequest.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
                }
                if (Provider.ToLower() == "tbo" || Provider.ToLower() == "tbo_api")
                {
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                    httpWebRequest.Method = "POST";
                    httpWebRequest.ContentType = "application/soap+xml; charset=UTF-8";
                    httpWebRequest.Headers.Add("Accept-Encoding", "gzip, deflate");
                    httpWebRequest.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
                }
                using (Stream requestStream = httpWebRequest.GetRequestStream())
                {
                    //Writes a sequence of bytes to the current stream                     
                    requestStream.Write(bytes, 0, bytes.Length);
                    requestStream.Close();//Close stream
                }
                //Sends the HttpWebRequest, and waits for a response.
                httpWebResponse = (HttpWebResponse)await httpWebRequest.GetResponseAsync();
                if (httpWebResponse.StatusCode == HttpStatusCode.OK)
                {
                    //Get response stream into StreamReader 
                    using (Stream responseStream = httpWebResponse.GetResponseStream())
                    {
                        using (StreamReader reader = new StreamReader(responseStream))
                            response = await reader.ReadToEndAsync();

                    }
                    timer.Stop();
                    if (para != null)
                    {
                        para.SupplierTime += ",SupplierName :" + Provider + " : " + "Execution Time :" + timer.Elapsed.ToString();
                        if (DebugMode)
                            _ = XMLRequestResponseTrack(request, response, Provider, "Proper");
                    }
                    if (Convert.ToBoolean(_keySettings.IsLogTrack))
                    {
                        TimeSpan timeTaken = timer.Elapsed;
                        _exception.ErrorLog(Convert.ToString(_keySettings.IsLogTrack), "Trvaco->" + HotelTrvacoProvider + "-> Execution Time : " + timer.Elapsed.ToString());
                        _ = XMLRequestResponseTrack(request, response, Provider == "" ? "Travco" : Provider, "Proper");
                    }
                }
            }
            catch (WebException we)
            {   //TODO: Add custom exception handling
                //response = we.Message + we.InnerException;

                HttpWebResponse httpResponse = (HttpWebResponse)we.Response;
                string result = string.Empty;
                string result1 = string.Empty;
                if (httpResponse != null && httpResponse.StatusDescription != null)
                {
                    result = httpResponse.StatusDescription.ToString();
                    result1 = new StreamReader(we.Response.GetResponseStream()).ReadToEnd();
                }
                if (Provider.ToLower() == "tbo_api" && StoreSupplierResErrorLog_TBO_API == "1")
                {
                    _ = XMLRequestResponseTrack(request, we.Message.ToString(), Provider, timer.Elapsed.ToString(), "Room", "");
                }
                else
                {
                    _ = XMLRequestResponseTrackError(request, result1, Provider == "" ? "Travco" : Provider, "Proper");
                }
                Error = result1;
            }
            catch (Exception ex)
            {
                if (Provider.ToLower() == "tbo_api" && StoreSupplierResErrorLog_TBO_API == "1")
                {
                    _ = XMLRequestResponseTrack(request, ex.Message.ToString(), Provider, timer.Elapsed.ToString(), "Room", "");
                }
                else
                {
                    //response = ex.Message + ex.InnerException;
                    _ = XMLRequestResponseTrack(request, ex.Message, Provider == "" ? "Travco" : Provider, "Proper");
                }
                Error = ex.Message.ToString();
                _ = _exception.Error(ex, SearchKey);
            }

            finally
            {
                if (httpWebResponse != null)
                {
                    httpWebResponse.Close();

                    httpWebResponse = null;

                    httpWebRequest = null;
                }
            }
            responseDto.Response = response;
            responseDto.Error = Error;

            return responseDto;
        }
        public async Task<string> SupplierPerformanceReportForRecheckOnly(string SupplierName, string ReportType, int AgentId, string ResponseTimeout, string ResponseType, string Request = "", string Response = "", string Error = "")
        {
            try
            {

                if (ReportType.ToLower() == "prebook")
                {
                    if (Request == "")
                    {
                        SqlParameter[] para = new SqlParameter[] {
                     new SqlParameter("@SupplierName",SupplierName),
                 new SqlParameter("@ReportType",ReportType),
                  new SqlParameter("@AgentID",AgentId),
                new SqlParameter("@ResponseTimeout",ResponseTimeout),
                new SqlParameter("@ResponseType",ResponseType),
                 new SqlParameter("@FromApi",1)
                };
                        //System.Diagnostics.st timerroomprocess1 = new System.Diagnostics.Stopwatch();
                        //DataSet abc = _dlConnection.GetDataSetv1("SupplierPerformanceReportDataForBook_PreBook", para);
                        DataSet abc = await _dlConnection.GetDataSet("SupplierPerformanceReportDataForBook_PreBook", para);
                        return "";
                    }
                    else
                    {
                        if (Error.ToLower().Contains("task was canceled") || Error.ToLower().Contains("no response was received during the time-out period for a request") || Error.ToLower().Contains("the request was aborted: the request was canceled."))
                        {
                            ResponseType = "Timeout";
                        }
                        else
                        {
                            if (Error != "")
                            {
                                ResponseType = "GeneralError";
                                Response = Response == "" ? Error : Error + " #Supplier Response" + Response;
                            }
                        }

                        SqlParameter[] para = new SqlParameter[] {
                        new SqlParameter("@SupplierName",SupplierName),
                        new SqlParameter("@ReportType",ReportType),
                        new SqlParameter("@AgentID",AgentId),
                        new SqlParameter("@ResponseTimeout",ResponseTimeout),
                        new SqlParameter("@ResponseType",ResponseType),
                        new SqlParameter("@Request",Request),
                        new SqlParameter("@Response",Response),
                        new SqlParameter("@FromPrebook",2),
                         new SqlParameter("@FromApi",1)
                };
                        //System.Diagnostics.st timerroomprocess1 = new System.Diagnostics.Stopwatch();
                        //DataSet abc = _dlConnection.GetDataSetv1("SupplierPerformanceReportDataForBook_PreBook_v1", para);
                        DataSet abc = await _dlConnection.GetDataSet("SupplierPerformanceReportDataForBook_PreBook_v1", para);
                        return "";
                    }
                }
                else
                {
                    if (Error.ToLower().Contains("task was canceled") || Error.ToLower().Contains("no response was received during the time-out period for a request") || Error.ToLower().Contains("the request was aborted: the request was canceled."))
                    {
                        ResponseType = "Timeout";
                    }
                    else
                    {
                        if (Error != "")
                        {
                            ResponseType = "GeneralError";
                            Response = Response == "" ? Error : Error + " #Supplier Response" + Response;
                        }
                    }
                    //if (Request == "")
                    //{
                    //    SqlParameter[] para = new SqlParameter[] {
                    //         new SqlParameter("@SupplierName",SupplierName),
                    //     new SqlParameter("@ReportType",ReportType),
                    //      new SqlParameter("@AgentID",AgentId),
                    //    new SqlParameter("@ResponseTimeout",ResponseTimeout),
                    //    new SqlParameter("@ResponseType",ResponseType)
                    //    };
                    //    DBConnection db = new DBConnection();
                    //    //System.Diagnostics.st timerroomprocess1 = new System.Diagnostics.Stopwatch();
                    //    DataSet abc = db.GetDataSet("SupplierPerformanceReportDataForBook_PreBook", para);
                    //    return "";
                    //}
                    //else
                    {
                        SqlParameter[] para = new SqlParameter[] {
                        new SqlParameter("@SupplierName",SupplierName),
                        new SqlParameter("@ReportType",ReportType),
                        new SqlParameter("@AgentID",AgentId),
                        new SqlParameter("@ResponseTimeout",ResponseTimeout),
                        new SqlParameter("@ResponseType",ResponseType),
                        new SqlParameter("@Request",Request),
                        new SqlParameter("@Response",Response),
                         new SqlParameter("@FromApi",1)
                    };
                        //System.Diagnostics.st timerroomprocess1 = new System.Diagnostics.Stopwatch();
                        //DataSet abc = _dlConnection.GetDataSetv1("SupplierPerformanceReportDataForBook_PreBook_v1", para);
                        DataSet abc = await _dlConnection.GetDataSet("SupplierPerformanceReportDataForBook_PreBook_v1", para);
                        return "";
                    }
                }
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
                return "";
            }
            return "";
        }

        public async Task XMLRequestResponseTrackError(string XmlRequest, string XmlResponse, string Provider, string TypeRequest)
        {
            try
            {
                SqlParameter[] para = new SqlParameter[]{
            new SqlParameter("@XmlRequest",XmlRequest),
            new SqlParameter("@XmlResponse",XmlResponse),
            new SqlParameter("@Provider",Provider),
            new SqlParameter("@TypeRequest",TypeRequest)
            };
                await _dlConnection.GetExecuteScalarSP("InsertXMLRequestResponseTrackError", para);
            }
            catch (Exception ex)
            {
                _ = _exception.Error(ex);
            }
        }

        public void XMLRequestResponseTrack_MainDB(string XmlRequest, string XmlResponse, string Provider, string TypeRequest, string reqId = "", string NewSearchKey = "")
        {
            try
            {
                if (true)//Added on 10-MAR-2025
                {
                    Task.Run(() => SaveXMLRequestResponseTrack_MainDB(XmlRequest, XmlResponse, Provider, TypeRequest, reqId, NewSearchKey));
                }
                else
                {
                    SqlParameter[] para = new SqlParameter[]{
                    new SqlParameter("@XmlRequest",XmlRequest),
                    new SqlParameter("@XmlResponse",XmlResponse),
                    new SqlParameter("@Provider",Provider),
                    new SqlParameter("@TypeRequest",TypeRequest),
                    new SqlParameter("@ReqId",reqId),
                    new SqlParameter("@SearchKey",NewSearchKey)
                    };
                    _dlConnection.GetExecuteScalarSP_Logserver2_TP("InsertXMLRequestResponseTrack_Xconnect_MainDB", para);//@Newadded on 12-mar-2024
                }
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
            }

        }

        public void SaveXMLRequestResponseTrack_MainDB(string XmlRequest, string XmlResponse, string Provider, string TypeRequest, string reqId = "", string NewSearchKey = "")
        {
            try
            {
                SqlParameter[] para = new SqlParameter[]{
            new SqlParameter("@XmlRequest",XmlRequest),
            new SqlParameter("@XmlResponse",XmlResponse),
            new SqlParameter("@Provider",Provider),
            new SqlParameter("@TypeRequest",TypeRequest),
            new SqlParameter("@ReqId",reqId),
            new SqlParameter("@SearchKey",NewSearchKey)
            };
                _dlConnection.GetExecuteScalarSP_Logserver2_TP("InsertXMLRequestResponseTrack_Xconnect_MainDB", para);//@Newadded on 12-mar-2024
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
            }
        }


        public DataTable getCheckPriceReqTableStructure()
        {
            try
            {
                DataTable dtCheckPriceReq = new DataTable();
                DataColumn dtClm = new DataColumn("PriceReqID", typeof(int));
                dtCheckPriceReq.Columns.Add(dtClm);

                dtClm = new DataColumn("UserCartID", typeof(int));
                dtCheckPriceReq.Columns.Add(dtClm);

                dtClm = new DataColumn("UserCartDetailsID", typeof(int));
                dtCheckPriceReq.Columns.Add(dtClm);

                dtClm = new DataColumn("CheckPriceReq", typeof(string));
                dtCheckPriceReq.Columns.Add(dtClm);

                dtClm = new DataColumn("RoomTypeID", typeof(string));
                dtCheckPriceReq.Columns.Add(dtClm);

                dtClm = new DataColumn("SupplierName", typeof(string));
                dtCheckPriceReq.Columns.Add(dtClm);
                return dtCheckPriceReq;
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
            }
            return null;
        }



        public void ThirdPartyGetPolicyHistory(string UniqueNo, string AgentUniqueNo, string XMLRequest, string XMLResponse, string Stage, string Provider, string StatusBooking, string ReferenceNo, string Website)
        {
            try
            {
                Task.Run(() => SaveThirdPartyPolicyHistory(UniqueNo, AgentUniqueNo, XMLRequest, XMLResponse, Stage, Provider, StatusBooking, ReferenceNo, Website));//Added on 10-MAR-2025 #NoDependancy on VM 10.32.8.16
                if (false)
                {
                    //SqlConnection conn1 = new SqlConnection(System.Web.Configuration.WebConfigurationManager.ConnectionStrings["ConnectionString"].ToString());
                    SqlConnection conn1 = new SqlConnection(_connectionStrings.connectionStr_Logserver2_TP);//@Newadded on 12-mar-2024
                    conn1.Open();
                    SqlCommand comm1 = new SqlCommand();
                    comm1.Connection = conn1;
                    //comm1.CommandText = "insert into ThirdPartyBookingHistory_XMLOUT (TokenNo,AgentTokenNo,XMLRequest,XMLResponse,Stage,Provider,StatusBooking,ReferenceNo,Website,BookingDate) values('" + UniqueNo + "','" + AgentUniqueNo + "','" + ConfirmationBookingRequest + "','" + "-" + "','" + "Before Request hit booking" + "','" + SupplierData + "','" + "Request Confirm" + "','" + Convert.ToString(_objHotel.PNRNo) + "','" + "B2C" + "',getdate())";
                    comm1.CommandText = "insert into ThirdPartyPolicyHistory_XMLOUT (TokenNo,AgentTokenNo,XMLRequest,XMLResponse,Stage,Provider,StatusBooking,ReferenceNo,Website,BookingDate) values('" + UniqueNo + "','" + AgentUniqueNo + "','" + XMLRequest.Replace("'", "''").Replace(",", "") + "','" + XMLResponse.Replace("'", "''").Replace(",", "") + "','" + Stage + "','" + Provider + "','" + StatusBooking + "','" + ReferenceNo + "','" + Website + "',getdate())";
                    comm1.ExecuteScalar();
                    conn1.Close();
                }
            }
            catch (Exception Ex)
            {
                _exception.Error(Ex);
            }
            finally
            {
            }
        }

        public void SaveThirdPartyPolicyHistory(string UniqueNo, string AgentUniqueNo, string XMLRequest, string XMLResponse, string Stage, string Provider, string StatusBooking, string ReferenceNo, string Website)//Added on 10-MAR-2025 #NoDependancy on VM 10.32.8.16
        {
            try
            {
                //SqlConnection conn1 = new SqlConnection(System.Web.Configuration.WebConfigurationManager.ConnectionStrings["ConnectionString"].ToString());
                SqlConnection conn1 = new SqlConnection(_connectionStrings.connectionStr_Logserver2_TP);//@Newadded on 12-mar-2024
                conn1.Open();
                SqlCommand comm1 = new SqlCommand();
                comm1.Connection = conn1;
                //comm1.CommandText = "insert into ThirdPartyBookingHistory_XMLOUT (TokenNo,AgentTokenNo,XMLRequest,XMLResponse,Stage,Provider,StatusBooking,ReferenceNo,Website,BookingDate) values('" + UniqueNo + "','" + AgentUniqueNo + "','" + ConfirmationBookingRequest + "','" + "-" + "','" + "Before Request hit booking" + "','" + SupplierData + "','" + "Request Confirm" + "','" + Convert.ToString(_objHotel.PNRNo) + "','" + "B2C" + "',getdate())";
                comm1.CommandText = "insert into ThirdPartyPolicyHistory_XMLOUT (TokenNo,AgentTokenNo,XMLRequest,XMLResponse,Stage,Provider,StatusBooking,ReferenceNo,Website,BookingDate) values('" + UniqueNo + "','" + AgentUniqueNo + "','" + XMLRequest.Replace("'", "''").Replace(",", "") + "','" + XMLResponse.Replace("'", "''").Replace(",", "") + "','" + Stage + "','" + Provider + "','" + StatusBooking + "','" + ReferenceNo + "','" + Website + "',getdate())";
                comm1.ExecuteScalar();
                conn1.Close();
            }
            catch (Exception Ex)
            {
                _exception.Error(Ex);
            }
        }


        public void ThirdPartyBookingHistory(string UniqueNo, string AgentUniqueNo, string XMLRequest, string XMLResponse, string Stage, string Provider, string StatusBooking, string ReferenceNo, string Website,string Searchkey = "")
        {
            Website = "API Out";
            SqlConnection conn1 = new SqlConnection(_connectionStrings.connectionStr_Logserver2_TP);//@Newadded on 12-mar-2024
            try
            {

                #region Get SEARCH_KEY
                //string Searchkey = "";
                //try
                //{
                //    Searchkey = SEARCH_KEY;
                //}
                //catch (Exception x)
                //{
                //    _exception.Error(x);
                //}
                #endregion

                Task.Run(() => SaveThirdPartyBookingHistoryLog(UniqueNo, AgentUniqueNo, XMLRequest, XMLResponse, Stage, Provider, StatusBooking, ReferenceNo, Website, Searchkey));//Added on 10-MAR-2025 #NoDependancy on VM 10.32.8.16

                if (false)//(Provider.ToLower() == "booking.com") //Do not remove this case, B'cause it's using in log in bookingtally
                {
                    //try
                    //{
                    //    //SP added on 31-MAY-2024
                    //    SqlParameter[] para = new SqlParameter[]{
                    //     new SqlParameter("@TokenNo",UniqueNo),
                    //     new SqlParameter("@AgentTokenNo",AgentUniqueNo),
                    //     new SqlParameter("@XMLRequest",XMLRequest),
                    //     new SqlParameter("@XMLResponse",XMLResponse),
                    //     new SqlParameter("@Stage",Stage),
                    //     new SqlParameter("@Provider",Provider),
                    //     new SqlParameter("@StatusBooking",StatusBooking),
                    //     new SqlParameter("@ReferenceNo",ReferenceNo),
                    //     new SqlParameter("@Website",Website),
                    //     new SqlParameter("@Searchkey",Searchkey)
                    //     };

                    //    DLConnection con = new DLConnection();
                    //    con.GetExecuteScalarSP_Logserver2_TP("SaveThirdPartyBookingHistory_XMLOUT", para);

                    //}
                    //catch (Exception ex)
                    //{
                    //    Common.Error(ex);
                    //    conn1.Open();
                    //    SqlCommand comm1 = new SqlCommand();
                    //    comm1.Connection = conn1;
                    //    try
                    //    {
                    //        comm1.CommandText = "insert into ThirdPartyBookingHistory_XMLOUT (TokenNo,AgentTokenNo,XMLRequest,XMLResponse,Stage,Provider,StatusBooking,ReferenceNo,Website,BookingDate) values('" + UniqueNo + "','" + AgentUniqueNo + "','" + XMLRequest.Replace("'", "''").Replace(",", "") + "','" + XMLResponse.Replace("'", "''").Replace(",", "") + "','" + Stage + "','" + Provider + "','" + StatusBooking + "','" + ReferenceNo + "','" + ex.Message.ToString() + "',getdate())";
                    //        comm1.ExecuteScalar();
                    //    }
                    //    catch (Exception)
                    //    {

                    //        XMLRequest = Convert.ToBase64String(Encoding.UTF8.GetBytes(XMLRequest));
                    //        XMLResponse = Convert.ToBase64String(Encoding.UTF8.GetBytes(XMLResponse));
                    //        comm1.CommandText = "insert into ThirdPartyBookingHistory_XMLOUT (TokenNo,AgentTokenNo,XMLRequest,XMLResponse,Stage,Provider,StatusBooking,ReferenceNo,Website,BookingDate) values('" + UniqueNo + "','" + AgentUniqueNo + "','" + XMLRequest.Replace("'", "''").Replace(",", "") + "','" + XMLResponse.Replace("'", "''").Replace(",", "") + "','" + Stage + "','" + Provider + "','" + StatusBooking + "','" + ReferenceNo + "','" + Website + "',getdate())";
                    //        comm1.ExecuteScalar();
                    //    }

                    //}

                }
            }
            catch (Exception Ex)
            {
                _exception.Error(Ex);
            }
            finally
            {
                conn1.Close();
            }
        }

        public void SaveThirdPartyBookingHistoryLog(string UniqueNo, string AgentUniqueNo, string XMLRequest, string XMLResponse, string Stage, string Provider, string StatusBooking, string ReferenceNo, string Website, string Searchkey)//Added on 10-MAR-2025 #NoDependancy on VM 10.32.8.16
        {
            Website = "API Out";
            SqlConnection conn1 = new SqlConnection(_connectionStrings.connectionStr_Logserver2_TP);//@Newadded on 12-mar-2024
            #region Get SEARCH_KEY
            //string Searchkey = "";
            try
            {
                Searchkey = SEARCH_KEY;
            }
            catch (Exception x)
            {
                _exception.Error(x);
            }
            #endregion
            try
            {
                //SP added on 31-MAY-2024
                SqlParameter[] para = new SqlParameter[]{
                         new SqlParameter("@TokenNo",UniqueNo),
                         new SqlParameter("@AgentTokenNo",AgentUniqueNo),
                         new SqlParameter("@XMLRequest",XMLRequest),
                         new SqlParameter("@XMLResponse",XMLResponse),
                         new SqlParameter("@Stage",Stage),
                         new SqlParameter("@Provider",Provider),
                         new SqlParameter("@StatusBooking",StatusBooking),
                         new SqlParameter("@ReferenceNo",ReferenceNo),
                         new SqlParameter("@Website",Website),
                         new SqlParameter("@Searchkey",Searchkey)
                         };


                _dlConnection.GetExecuteScalarSP_Logserver2_TP("SaveThirdPartyBookingHistory_XMLOUT", para);

            }
            catch (Exception ex)
            {
                _exception.Error(ex);
                conn1.Open();
                SqlCommand comm1 = new SqlCommand();
                comm1.Connection = conn1;
                try
                {
                    comm1.CommandText = "insert into ThirdPartyBookingHistory_XMLOUT (TokenNo,AgentTokenNo,XMLRequest,XMLResponse,Stage,Provider,StatusBooking,ReferenceNo,Website,BookingDate) values('" + UniqueNo + "','" + AgentUniqueNo + "','" + XMLRequest.Replace("'", "''").Replace(",", "") + "','" + XMLResponse.Replace("'", "''").Replace(",", "") + "','" + Stage + "','" + Provider + "','" + StatusBooking + "','" + ReferenceNo + "','" + ex.Message.ToString() + "',getdate())";
                    comm1.ExecuteScalar();
                }
                catch (Exception)
                {

                    XMLRequest = Convert.ToBase64String(Encoding.UTF8.GetBytes(XMLRequest));
                    XMLResponse = Convert.ToBase64String(Encoding.UTF8.GetBytes(XMLResponse));
                    comm1.CommandText = "insert into ThirdPartyBookingHistory_XMLOUT (TokenNo,AgentTokenNo,XMLRequest,XMLResponse,Stage,Provider,StatusBooking,ReferenceNo,Website,BookingDate) values('" + UniqueNo + "','" + AgentUniqueNo + "','" + XMLRequest.Replace("'", "''").Replace(",", "") + "','" + XMLResponse.Replace("'", "''").Replace(",", "") + "','" + Stage + "','" + Provider + "','" + StatusBooking + "','" + ReferenceNo + "','" + Website + "',getdate())";
                    comm1.ExecuteScalar();
                }

            }
        }
        #region RoomMapping
        public async Task<DataTable> GetVervoRoomMappingdata(int HotelId, string HotelIds = "")
        {
            try
            {
                SqlParameter[] para = new SqlParameter[]{
     new SqlParameter("@HotelId",  HotelId),
     new SqlParameter("@HotelIds",  HotelIds)
                };

                // return db.GetDataTable("GetCancellationPolicyForBooking_New", para);
                DataTable RoomData = await _dlConnection.GetDataTableAsync("GetVervoRoomMappingData", para);
                return RoomData;
            }
            catch (Exception ex)
            {
                await _exception.Error(ex);
                return null;
            }

        }

        public string getMappedRoom_rlm(string originalRoomName, DataTable DtHotelInformation, int HotelId, ref string S_RoomNo)
        {
            try
            {
                if (DtHotelInformation != null && DtHotelInformation.Rows.Count > 0)
                {

                    DataRow[] dtr = DtHotelInformation.Select("SupplierRoomName='" + originalRoomName + "' and HotelId='" + HotelId + "'");
                    if (dtr.Length > 0)
                    {
                        originalRoomName = Convert.ToString(dtr[0]["standardName"]) /*+ "|" + " AMapped" + "(" + originalRoomName + ")"*/;
                        S_RoomNo = Convert.ToString(dtr[0]["RoomId"]) /*+ "|" + " AMapped" + "(" + originalRoomName + ")"*/;  // ADD ON 29-JAN-2025 
                        return originalRoomName;
                    }
                    else
                    {
                        S_RoomNo = ""; // ADD ON 29-JAN-2025
                        return originalRoomName /*+ "|" + " Not"*/;
                    }
                }
                else
                {
                    S_RoomNo = ""; // ADD ON 29-JAN-2025
                    return originalRoomName /*+ "|" + " Not"*/;
                }
            }
            catch (Exception ex)
            {
                // Log the exception
                //ExceptionManager.LogException(ex);

                // Return the original room name in case of an exception
                S_RoomNo = ""; // ADD ON 29-JAN-2025
                return originalRoomName;
            }

        }
        public string getMappedRoom_rlmOLDD(string originalRoomName, DataTable DtHotelInformation, int HotelId, ref string S_RoomNo)
        {
            try
            {
                DataRow[] dtr = DtHotelInformation.Select("SupplierRoomName='" + originalRoomName + "' and HotelId='" + HotelId + "'");
                if (dtr.Length > 0)
                {
                    originalRoomName = Convert.ToString(dtr[0]["standardName"]) /*+ "|" + " AMapped" + "(" + originalRoomName + ")"*/;
                    S_RoomNo = Convert.ToString(dtr[0]["RoomId"]) /*+ "|" + " AMapped" + "(" + originalRoomName + ")"*/;  // ADD ON 29-JAN-2025 
                    return originalRoomName;
                }
                else
                {
                    S_RoomNo = ""; // ADD ON 29-JAN-2025
                    return originalRoomName /*+ "|" + " Not"*/;
                }
            }
            catch (Exception ex)
            {
                // Log the exception
                //ExceptionManager.LogException(ex);

                // Return the original room name in case of an exception
                S_RoomNo = ""; // ADD ON 29-JAN-2025
                return originalRoomName;
            }

        }

        public string getMappedRoom_rlm_old(string originalRoomName, DataTable DtHotelInformation, int HotelId)
        {
            try
            {
                DataRow[] dtr = DtHotelInformation.Select("SupplierRoomName='" + originalRoomName + "' and HotelId='" + HotelId + "'");
                if (dtr.Length > 0)
                {
                    originalRoomName = Convert.ToString(dtr[0]["standardName"]) /*+ "|" + " AMapped" + "(" + originalRoomName + ")"*/;
                    return originalRoomName;
                }
                else
                {
                    return originalRoomName /*+ "|" + " Not"*/;
                }
            }
            catch (Exception ex)
            {
                // Log the exception
                //ExceptionManager.LogException(ex);

                // Return the original room name in case of an exception
                return originalRoomName;
            }

        }



        #endregion RoomMapping

        public async Task<string> SupplierPerformanceReportForPrebookAndBook(string SupplierName, string ReportType, int AgentId, string ResponseTimeout, string ResponseType, string Request = "", string Response = "", string Error = "")
        {
            return "";

            try
            {

                if (ReportType.ToLower() == "prebook")
                {
                    if (Request == "")
                    {
                        SqlParameter[] para = new SqlParameter[] {
                     new SqlParameter("@SupplierName",SupplierName),
                 new SqlParameter("@ReportType",ReportType),
                  new SqlParameter("@AgentID",AgentId),
                new SqlParameter("@ResponseTimeout",ResponseTimeout),
                new SqlParameter("@ResponseType",ResponseType),
                new SqlParameter("@FromApi",1)
                };
                        //System.Diagnostics.st timerroomprocess1 = new System.Diagnostics.Stopwatch();
                        //DataSet abc = _dlConnection.GetDataSetv1("SupplierPerformanceReportDataForBook_PreBook", para);
                        DataSet abc = await _dlConnection.GetDataSet("SupplierPerformanceReportDataForBook_PreBook", para);
                        return "";
                    }
                    else
                    {
                        if (Error.ToLower().Contains("task was canceled") || Error.ToLower().Contains("no response was received during the time-out period for a request") || Error.ToLower().Contains("the request was aborted: the request was canceled."))
                        {
                            ResponseType = "Timeout";
                        }
                        else
                        {
                            if (Error != "")
                            {
                                ResponseType = "GeneralError";
                                Response = Response == "" ? Error : Error + " #Supplier Response" + Response;
                            }
                        }

                        SqlParameter[] para = new SqlParameter[] {
                     new SqlParameter("@SupplierName",SupplierName),
                 new SqlParameter("@ReportType",ReportType),
                  new SqlParameter("@AgentID",AgentId),
                new SqlParameter("@ResponseTimeout",ResponseTimeout),
                new SqlParameter("@ResponseType",ResponseType),
                 new SqlParameter("@Request",Request),
                  new SqlParameter("@Response",Response),
                   new SqlParameter("@FromApi",1)
                };
                        //System.Diagnostics.st timerroomprocess1 = new System.Diagnostics.Stopwatch();
                        //DataSet abc = _dlConnection.GetDataSetv1("SupplierPerformanceReportDataForBook_PreBook_v1", para);
                        DataSet abc = await _dlConnection.GetDataSet("SupplierPerformanceReportDataForBook_PreBook_v1", para);
                        return "";
                    }
                }
                else
                {
                    if (Error.ToLower().Contains("task was canceled") || Error.ToLower().Contains("no response was received during the time-out period for a request") || Error.ToLower().Contains("the request was aborted: the request was canceled."))
                    {
                        ResponseType = "Timeout";
                    }
                    else
                    {
                        if (Error != "")
                        {
                            ResponseType = "GeneralError";
                            Response = Response == "" ? Error : Error + " #Supplier Response" + Response;
                        }
                    }
                    //if (Request == "")
                    //{
                    //    SqlParameter[] para = new SqlParameter[] {
                    //         new SqlParameter("@SupplierName",SupplierName),
                    //     new SqlParameter("@ReportType",ReportType),
                    //      new SqlParameter("@AgentID",AgentId),
                    //    new SqlParameter("@ResponseTimeout",ResponseTimeout),
                    //    new SqlParameter("@ResponseType",ResponseType)
                    //    };
                    //    DBConnection db = new DBConnection();
                    //    //System.Diagnostics.st timerroomprocess1 = new System.Diagnostics.Stopwatch();
                    //    DataSet abc = db.GetDataSet("SupplierPerformanceReportDataForBook_PreBook", para);
                    //    return "";
                    //}
                    //else
                    {
                        SqlParameter[] para = new SqlParameter[] {
                     new SqlParameter("@SupplierName",SupplierName),
                 new SqlParameter("@ReportType",ReportType),
                  new SqlParameter("@AgentID",AgentId),
                new SqlParameter("@ResponseTimeout",ResponseTimeout),
                new SqlParameter("@ResponseType",ResponseType),
                 new SqlParameter("@Request",Request),
                  new SqlParameter("@Response",Response),
                   new SqlParameter("@FromApi",1)
                };
                        //System.Diagnostics.st timerroomprocess1 = new System.Diagnostics.Stopwatch();
                        //DataSet abc = _dlConnection.GetDataSetv1("SupplierPerformanceReportDataForBook_PreBook_v1", para);
                        DataSet abc = await _dlConnection.GetDataSet("SupplierPerformanceReportDataForBook_PreBook_v1", para);
                        return "";
                    }
                }
            }
            catch (Exception)
            {
                return "";
            }
            return "";
        }

        public double TruncateDecimalPlacesNew(double val)
        {
            try
            {
                if (Convert.ToString(val).Contains('.'))
                {
                    string NewVal = Convert.ToString(val).Split('.')[0];
                    return Convert.ToDouble(NewVal);
                }
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
                return val;
            }
            return val;
        }

        public string ReadCacheFile(string filepath, ref bool isError)
        {
            isError = false;
            string json = "";
            try
            {
                if (File.Exists(filepath))
                {
                    FileStream fs1 = new FileStream(filepath, FileMode.Open, FileAccess.Read, FileShare.Read);
                    try
                    {
                        using (StreamReader r = new StreamReader(fs1))
                        {
                            json = r.ReadToEnd();
                            // finalResponse = JsonConvert.DeserializeObject<AvailHotelList>(json);
                        }
                    }
                    catch (Exception ex)
                    {
                        _exception.Error(ex);
                        var random = new MersenneTwister(); //
                        int id = random.Next(500, 1500);
                        Thread.Sleep(id);
                        json = ReadCacheFile2(filepath, ref isError);
                        //isError = true;
                    }
                    finally
                    {
                        fs1.Close();
                        fs1.Dispose();
                        //GC.Collect();
                    }

                }
            }
            catch (Exception ex)
            {
                _exception.Error(ex);

                var random = new MersenneTwister(); //
                int id = random.Next(500, 1500);
                Thread.Sleep(id);
                return ReadCacheFile2(filepath, ref isError);

                //isError = true;
            }
            return json;
        }

        public string ReadCacheFile2(string filepath, ref bool isError)
        {
            isError = false;
            string json = "";
            try
            {
                if (File.Exists(filepath))
                {
                    FileStream fs1 = new FileStream(filepath, FileMode.Open, FileAccess.Read, FileShare.Read);
                    try
                    {
                        using (StreamReader r = new StreamReader(fs1))
                        {
                            json = r.ReadToEnd();
                            // finalResponse = JsonConvert.DeserializeObject<AvailHotelList>(json);
                        }
                    }
                    catch (Exception ex)
                    {
                        _exception.Error(ex);
                        isError = true;
                    }
                    finally
                    {
                        fs1.Close();
                        fs1.Dispose();
                        //GC.Collect();
                    }

                }
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
                isError = true;
            }
            return json;
        }

        public void delayManual(int Time_delay)
        {
            int i = 0;
            //  ameTir = new System.Timers.Timer();
            System.Timers.Timer _delayTimer = new System.Timers.Timer();
            _delayTimer.Interval = Time_delay;
            _delayTimer.AutoReset = false; //so that it only calls the method once
            _delayTimer.Elapsed += (s, args) => i = 1;
            _delayTimer.Start();
            while (i == 0) { };
        }

        public HotelSearchParameter ResetSearchParameter(HotelSearchParameter objSearPera)
        {
            HotelSearchParameter para = new HotelSearchParameter();

            try
            {
                para.AgentCompanyPercentage = objSearPera.AgentCompanyPercentage;
                para.AgentMarkup = objSearPera.AgentMarkup;
                para.BookForUserCode = objSearPera.BookForUserCode;
                para.CheckInDate = objSearPera.CheckInDate;
                para.CheckOutDate = objSearPera.CheckOutDate;
                para.CityID = objSearPera.CityID;
                para.CityName = objSearPera.CityName;
                para.CountryCode = objSearPera.CountryCode;
                para.CountryID = objSearPera.CountryID;
                para.CurrentDate = objSearPera.CurrentDate;
                para.HotelType = objSearPera.HotelType;
                para.IsB2BOrB2C = objSearPera.IsB2BOrB2C;
                para.Location = objSearPera.Location;
                para.LocationID = objSearPera.LocationID;
                para.Nationality = objSearPera.Nationality;
                para.NoofNights = objSearPera.NoofNights;
                para.Residency = objSearPera.Residency;
                para.Rooms = objSearPera.Rooms;
                para.SelectedStart = objSearPera.SelectedStart;
                para.StarEnd = objSearPera.StarEnd;
                para.StarStart = objSearPera.StarStart;
                para.UserName = objSearPera.UserName;
                para.SupplierTime = objSearPera.SupplierTime;
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
            }
            return para;


        }

        public string GetType(int NoofAdults)
        {
            string rmtype = string.Empty;
            switch (NoofAdults)
            {
                case 1:
                    rmtype = "Single";
                    break;
                case 2:
                    rmtype = "Double";
                    break;
                case 3:
                    rmtype = "Triple";
                    break;
                case 4:
                    rmtype = "Quad";
                    break;
            }
            return rmtype;
        }

        public List<HotelCodeByCity> FilterWithOutCity_Code(int ratingcount, int hotelcount, List<HotelCodeByCity> dtHotelCommon, List<StarRating> lstRating, string Supplier)
        {
            List<HotelCodeByCity> filter = new List<HotelCodeByCity>();
            try
            {
                //for (int r = 0; r < ratingcount; r++)
                {
                    for (int h = 0; h < hotelcount; h++)
                    {
                        if (dtHotelCommon[h].supplierName.ToLower() == Supplier.ToLower())
                            filter.Add(dtHotelCommon[h]);
                    }
                }
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
            }
            return filter;
        }

        public async Task XMLRequestResponseTrack_CMLog(string XmlRequest, string XmlResponse, string Provider, string TypeRequest, string reqId = "", string NewSearchKey = "")
        {
            try
            {
                //DLConnection con = new DLConnection();
                SqlParameter[] para = new SqlParameter[]{
            new SqlParameter("@XmlRequest",XmlRequest),
            new SqlParameter("@XmlResponse",XmlResponse),
            new SqlParameter("@Provider",Provider),
            new SqlParameter("@TypeRequest",TypeRequest),
            new SqlParameter("@ReqId",reqId),
            new SqlParameter("@SearchKey",NewSearchKey)
            };
                await _dlConnection.GetExecuteScalarSP("InsertXMLRequestResponseTrack_CMLog_Xconnect", para);
            }
            catch (Exception ex)
            {
                await _exception.Error(ex);
            }
        }

        public decimal GetcalVatAmount(decimal amount, decimal percentage)
        {

            return (amount * percentage / 100);
        }

        public async Task<DataTable> GetVatCountrylist(int CountryId, int CityID, string ServiceType = "")
        {
            DataTable dt = new DataTable();
            try
            {
                //string cacheKey = "GetVatCountrylist_" + CountryId + "_" + CityID + "_" + ServiceType;
                string cacheKey = "GetVatCountrylist_" + CountryId + "_" + CityID + "_" + ServiceType;

                // if (System.Web.HttpRuntime.Cache[cacheKey] == null)  // if cache is empty
                if (!_cache.TryGetValue(cacheKey, out dt))
                // if cache is empty
                {
                    SqlParameter[] para = new SqlParameter[]{
                new SqlParameter("@CityId", CityID),
                new SqlParameter("@CountryId", CountryId),
                new SqlParameter("@ServiceType", ServiceType)
            };

                    dt = await _dlConnection.GetDataTableAsync("GetVatCountrylist", para);

                    // Log the data if necessary
                    // System.Web.HttpRuntime.Cache.Insert(cacheKey, dt, null, DateTime.Now.AddHours(100), TimeSpan.Zero);
                    var cacheEntryOptions = new MemoryCacheEntryOptions()
                  .SetAbsoluteExpiration(DateTime.Now.AddHours(100));
                }


                //else // load from cache
                //{
                //    // dt = (DataTable)(System.Web.HttpRuntime.Cache[cacheKey]);
                //    //Take data from cache

                //    dt = (DataTable)(_cache.Get(cacheKey));
                //    // _cache.Set(_cache, dt, cacheEntryOptions);
                //}
                //else // load from cache
                //{
                //    dt = (DataTable)(_cache.Get(cacheKey));
                //    if (dt == null) // In case cache is expired or is missing
                //    {
                //        // Refresh cache
                //        dt = await _dlConnection.GetDataTable("GetVatCountrylist", para);
                //        _cache.Set(cacheKey, dt, cacheEntryOptions);
                //    }
                //}


                return dt;
            }
            catch (Exception e)
            {
                await _exception.Error(e); // Log the error for debugging
                return dt;
            }
        }

        public DataTable GenerateHotelAllocationTableNewV2()
        {
            try
            {
                DataTable dtAllocation = new DataTable("HotelAllocation");
                DataColumn dcColumn;
                dcColumn = new DataColumn("OwnsystemHotelId", typeof(string));
                dtAllocation.Columns.Add(dcColumn);

                dcColumn = new DataColumn("RoomNo", typeof(Int32));
                dtAllocation.Columns.Add(dcColumn);

                dcColumn = new DataColumn("AllocationSlabID", typeof(Int32));
                dtAllocation.Columns.Add(dcColumn);

                dcColumn = new DataColumn("AllocationDate", typeof(DateTime));
                dtAllocation.Columns.Add(dcColumn);

                dcColumn = new DataColumn("RoomTypeId", typeof(string));
                dtAllocation.Columns.Add(dcColumn);
                dcColumn = new DataColumn("GIITACode", typeof(string));
                dtAllocation.Columns.Add(dcColumn);


                return dtAllocation;
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
                return null;
            }
        }

        public DataTable GenerateComplementaryTableNewV2()
        {
            try
            {
                DataTable DtComplementry = new DataTable();
                DtComplementry.Columns.Add("OwnsystemHotelId");
                DtComplementry.Columns.Add("ComplementaryName");
                DtComplementry.Columns.Add("ComplementaryText");
                DtComplementry.Columns.Add("ImagePath");
                DtComplementry.Columns.Add("GIITACode");
                return DtComplementry;
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
            }
            return null;
        }

        public DataTable GenerateCommonInfoTableNewV2()
        {
            try
            {
                DataTable DtCommonInfo = new DataTable();
                DtCommonInfo.Columns.Add("OwnsystemHotelId");
                DtCommonInfo.Columns.Add("CommonPolicyName");
                DtCommonInfo.Columns.Add("CommonPolicyManagementText");
                DtCommonInfo.Columns.Add("GIITACode");
                return DtCommonInfo;
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
            }
            return null;
        }

        public DataTable GenerateHotelMinStayNewV2()
        {
            try
            {
                DataTable dtMinStay = new DataTable("MinStay");
                DataColumn dcColumn;
                dcColumn = new DataColumn("OwnsystemHotelId", typeof(string));
                dtMinStay.Columns.Add(dcColumn);

                dcColumn = new DataColumn("RoomTypeID", typeof(string));
                dtMinStay.Columns.Add(dcColumn);

                dcColumn = new DataColumn("Count", typeof(Int32));
                dtMinStay.Columns.Add(dcColumn);

                dcColumn = new DataColumn("MinStayText", typeof(string));
                dtMinStay.Columns.Add(dcColumn);
                dcColumn = new DataColumn("GIITACode", typeof(string));
                dtMinStay.Columns.Add(dcColumn);


                return dtMinStay;
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
                return null;
            }
        }

        public List<StarRating> GetStarRatingStringList(HotelSearchParameter SearchPara)
        {
            List<StarRating> rating = new List<StarRating>(); ;

            try
            {
                if (SearchPara.StarStart <= 1 && SearchPara.StarEnd >= 1)
                    rating.Add(new StarRating { Star = 1 });
                if (SearchPara.StarStart <= 2 && SearchPara.StarEnd >= 2)
                    rating.Add(new StarRating { Star = 2 });

                if (SearchPara.StarStart <= 3 && SearchPara.StarEnd >= 3)
                    rating.Add(new StarRating { Star = 3 });

                if (SearchPara.StarStart <= 4 && SearchPara.StarEnd >= 4)
                    rating.Add(new StarRating { Star = 4 });

                if (SearchPara.StarStart <= 5 && SearchPara.StarEnd >= 5)
                    rating.Add(new StarRating { Star = 5 });

                if (SearchPara.StarStart <= 0 && SearchPara.StarEnd >= 0)
                    rating.Add(new StarRating { Star = 0 });
                return rating;
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
                return rating;
            }
        }
        public string GetHotelCodeByCOmma(List<XMLID_Search> lstxml)
        {
            string paramValues1 = "";

            try
            {
                for (int h = 0; h < lstxml.Count; h++)
                {
                    paramValues1 = paramValues1 + lstxml[h].XMLID + ",";
                }
                if (paramValues1.Length > 0)
                    paramValues1 = paramValues1.Substring(0, paramValues1.Length - 1);//Remove last comma(,) 
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
            }
            return paramValues1;
        }

        public List<List<XMLID_Search>> GetXMLIDString(double NoOfDiv, int Noofrecords, double count, List<HotelCodeByCity> DtHotelDAta)
        {
            List<List<XMLID_Search>> lstxml = new List<List<XMLID_Search>>();

            try
            {
                int first = 0;
                for (int xm = 0; xm < NoOfDiv; xm++)
                {
                    List<XMLID_Search> tempid = new List<XMLID_Search>();
                    for (int i = 0; i < Noofrecords; i++)
                    {
                        if (first < count)
                        {
                            tempid.Add(new XMLID_Search { XMLID = Convert.ToString(DtHotelDAta[first].XmlId) });
                            first++;
                        }
                        else
                            break;
                    }
                    lstxml.Add(tempid);
                }
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
            }
            return lstxml;

        }
        public List<HotelCodeByCity> FilterWithCityCode(List<HotelCodeByCity> DtHotelDAta, string XMLBaseID)
        {
            List<HotelCodeByCity> filterwithCity = new List<HotelCodeByCity>();
            try
            {
                for (int c = 0; c < DtHotelDAta.Count; c++)
                {
                    if (DtHotelDAta[c].XmlbaseId.ToLower() == XMLBaseID.ToLower())
                        filterwithCity.Add(DtHotelDAta[c]);
                }
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
            }
            return filterwithCity;
        }

        public List<XMLBaseID_Search> GetXMLBAsedIdString(List<HotelCodeByCity> DtHotelDAta)
        {
            List<XMLBaseID_Search> lst = new List<XMLBaseID_Search>();

            try
            {
                for (int x = 0; x < DtHotelDAta.Count; x++)
                {
                    List<XMLBaseID_Search> temp = lst.FindAll(delegate (XMLBaseID_Search obj) { return Convert.ToString(DtHotelDAta[x].XmlbaseId).ToLower() == obj.XMLBaseID.ToLower(); });
                    if (temp.Count == 0)
                    {
                        lst.Add(new XMLBaseID_Search { XMLBaseID = Convert.ToString(DtHotelDAta[x].XmlbaseId) });
                    }

                }

            }
            catch (Exception ex)
            {
                _exception.Error(ex);
            }
            return lst;
        }

        public List<StarRating> GetStarRatingStringList_xtg(HotelSearchParameter SearchPara)
        {
            List<StarRating> rating = new List<StarRating>();

            try
            {
                for (int star = SearchPara.StarStart; star <= SearchPara.StarEnd; star++)
                {
                    if (star >= 1 && star <= 5)
                    {
                        rating.Add(new StarRating { Star = star });
                    }
                }

                return rating;
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
                return new List<StarRating>(); // Return an empty list in case of an error
            }
        }

        public DataForSearch GetXmlDataForSearchNew(string Natinality, DataSet dtXMLCity, string Supplier)
        {
            DataForSearch dataFor = new DataForSearch();
            dataFor.xmlCity = "";
            dataFor.XmlNationality = "";
            dataFor.xmlCountry = "";

            try
            {
                //get xml city and country
                if (dtXMLCity.Tables[0].Rows.Count > 0)
                {

                    DataRow[] drCity = dtXMLCity.Tables[0].Select("Supplier='" + Supplier + "'");
                    if (drCity.Length > 0)
                    {
                        dataFor.xmlCity = Convert.ToString(drCity[0]["XmlCity"]);
                        dataFor.xmlCountry = Convert.ToString(drCity[0]["XmlCountry"]);
                    }
                }
                //nationality part
                if (dtXMLCity != null && dtXMLCity.Tables.Count > 0 && dtXMLCity.Tables[1] != null && dtXMLCity.Tables[1].Rows.Count > 0)
                {
                    switch (Supplier.ToLower())
                    {
                        case "dotw":
                            dataFor.XmlNationality = Convert.ToString(GetThirdPartyStaticData_New(dtXMLCity.Tables[1], "Desc", Convert.ToString(Natinality), "XmlNationality", "Dotw"));
                            break;
                        case "gta":
                        case "miki":

                            dataFor.XmlNationality = Convert.ToString(GetThirdPartyStaticData_New(dtXMLCity.Tables[1], "Desc", Convert.ToString(Natinality), "XmlNationality", "Gta"));
                            break;
                        case "travellanda":
                        case "hotelbedsv1":
                        case "w2m":
                        case "gte":
                        case "alphatours":
                        case "egyptexpress":
                        case "xtg":
                        case "hotelspro":
                        case "roomsxml":
                        case "genting":
                        case "fastpayhotels":
                        case "lotsofhotels":
                        case "escalabeds":
                        case "bookohotel":
                        case "travcoegypt":
                        case "fitruums":
                        case "whl":
                        case "mercan":
                        case "worldavenues":
                        case "rts":
                        case "dida":
                        case "agoda":
                        case "yalago":
                        case "rezlive":
                        case "bonotel":
                        case "mgbedbank":
                        case "goglobal":
                        case "3fs":
                        case "adonis":
                        case "booking.com":
                        case "traveloka":
                        case "ezconnect":
                        case "rakuten":
                        case "getaroom_api":
                        case "hoteltrader":
                        case "peakpoint":
                        case "hyperguest":
                        case "itrip":
                        case "ean":
                        case "agoda_v1":
                        case "primetravel":
                        case "webhotelier":
                        case "saltours":
                        case "ixpira":
                        case "welcomebeds":
                        case "destinationitalia":
                        case "teamamerica":
                        case "elevatedmc":
                        case "agoda_chain":
                        case "hotelbedsv1_new":

                            dataFor.XmlNationality = Convert.ToString(GetThirdPartyStaticData_New(dtXMLCity.Tables[1], "Desc", Convert.ToString(Natinality), "XmlNationality", "Agoda"));

                            break;
                        case "asianoverland":
                        case "nirvana":
                            dataFor.XmlNationality = Convert.ToString(GetThirdPartyStaticData_New(dtXMLCity.Tables[1], "Desc", Convert.ToString(Natinality), "XmlNationality", "asianoverland"));
                            break;
                        case "darina":
                            dataFor.XmlNationality = Convert.ToString(GetThirdPartyStaticData_New(dtXMLCity.Tables[1], "Desc", Convert.ToString(Natinality), "XmlNationality", "darina"));

                            break;
                        case "jactravel":
                            // XmlNationality = Convert.ToString((Common.GetThirdPartyStaticData_Xml("Nationality", "Desc", System.Convert.ToString(Natinality), "SystemMasterId", "Agoda")));
                            dataFor.XmlNationality = Convert.ToString(GetThirdPartyStaticData_New(dtXMLCity.Tables[1], "Desc", Convert.ToString(Natinality), "Desc", "Agoda"));

                            break;
                        case "mgholiday":
                            // XmlNationality = Convert.ToString((Common.GetThirdPartyStaticData_Xml("Nationality", "Desc", System.Convert.ToString(Natinality), "SystemMasterId", "Agoda")));
                            dataFor.XmlNationality = Convert.ToString(GetThirdPartyStaticData_New(dtXMLCity.Tables[1], "Desc", Convert.ToString(Natinality), "XmlNationality", "MgHoliday"));

                            break;

                        case "travstore":
                        case "methabook":
                        case "youtravel":
                        case "olympia":
                        case "intratour":
                        case "abreuonline":
                        case "infinitehotelsfrance":
                        case "restel":
                        case "eref":
                        case "hoteldo":
                        case "gtbeds":
                        case "tbo":
                        case "tbo_api":
                        case "iwtx":
                        case "omnibees":
                        case "robios":
                        case "fastpayhotelsv3":
                        case "pricetravel":
                        case "paximum":
                            dataFor.XmlNationality = Convert.ToString(GetThirdPartyStaticData_New(dtXMLCity.Tables[1], "Desc", Convert.ToString(Natinality), "XmlNationality", "Agoda"));

                            break;
                        case "whitesandsuae":
                            dataFor.XmlNationality = Convert.ToString(GetThirdPartyStaticData_New(dtXMLCity.Tables[1], "Desc", Convert.ToString(Natinality), "XmlNationality", "MgHoliday"));
                            break;
                        case "nuitee":
                            dataFor.XmlNationality = Convert.ToString(GetThirdPartyStaticData_New(dtXMLCity.Tables[1], "Desc", Convert.ToString(Natinality), "XmlNationality", "Agoda"));
                            break;
                        case "grnc":
                            dataFor.XmlNationality = Convert.ToString(GetThirdPartyStaticData_New(dtXMLCity.Tables[1], "Desc", Convert.ToString(Natinality), "XmlNationality", "grnc"));

                            break;


                        default:
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
            }
            return dataFor;
        }

        public DataForSearch GetXmlDataForSearchTechwise(string Natinality, DataSet dtXMLCity, string Supplier, string TechName)
        {
            DataForSearch dataFor = new DataForSearch();
            dataFor.xmlCity = "";
            dataFor.XmlNationality = "";
            dataFor.xmlCountry = "";

            try
            {
                //get xml city and country
                if (dtXMLCity.Tables[0].Rows.Count > 0)
                {

                    DataRow[] drCity = dtXMLCity.Tables[0].Select("Supplier='" + Supplier + "'");
                    if (drCity.Length > 0)
                    {
                        dataFor.xmlCity = Convert.ToString(drCity[0]["XmlCity"]);
                        dataFor.xmlCountry = Convert.ToString(drCity[0]["XmlCountry"]);
                    }
                }
                //nationality part
                if (dtXMLCity != null && dtXMLCity.Tables.Count > 0 && dtXMLCity.Tables[1] != null && dtXMLCity.Tables[1].Rows.Count > 0)
                {
                    switch (TechName.ToLower())
                    {
                        case "dotw":
                        case "webbeds":
                            dataFor.XmlNationality = Convert.ToString(GetThirdPartyStaticData_New(dtXMLCity.Tables[1], "Desc", Convert.ToString(Natinality), "XmlNationality", "Dotw"));
                            break;
                        case "gta":
                        case "miki":

                            dataFor.XmlNationality = Convert.ToString(GetThirdPartyStaticData_New(dtXMLCity.Tables[1], "Desc", Convert.ToString(Natinality), "XmlNationality", "Gta"));
                            break;
                        case "travellanda":
                        case "hotelbedsv1":
                        case "w2m":
                        case "gte":
                        case "alphatours":
                        case "egyptexpress":
                        case "xtg":
                        case "hotelspro":
                        case "roomsxml":
                        case "genting":
                        case "fastpayhotels":
                        case "lotsofhotels":
                        case "escalabeds":
                        case "bookohotel":
                        case "travcoegypt":
                        case "fitruums":
                        case "whl":
                        case "mercan":
                        case "worldavenues":
                        case "rts":
                        case "dida":
                        case "agoda":
                        case "yalago":
                        case "rezlive":
                        case "bonotel":
                        case "mgbedbank":
                        case "goglobal":
                        case "3fs":
                        case "adonis":
                        case "booking.com":
                        case "traveloka":
                        case "ezconnect":
                        case "rakuten":
                        case "getaroom_api":
                        case "hoteltrader":
                        case "peakpoint":
                        case "hyperguest":
                        case "itrip":
                        case "ean":
                        case "agoda_v1":
                        case "primetravel":
                        case "webhotelier":
                        case "saltours":
                        case "ixpira":
                        case "welcomebeds":
                        case "destinationitalia":
                        case "teamamerica":
                        case "elevatedmc":
                        case "agoda_chain":
                        case "hotelbedsv1_new":
                        case "hotelbeds":
                        case "juniper":
                        case "expedia":
                        case "booking.com_v3":
                        case "etg":
                        case "pkfair":
                        case "letsfly":
                            dataFor.XmlNationality = Convert.ToString(GetThirdPartyStaticData_New(dtXMLCity.Tables[1], "Desc", Convert.ToString(Natinality), "XmlNationality", "Agoda"));

                            break;
                        case "asianoverland":
                        case "nirvana":
                        case "qtech":
                            dataFor.XmlNationality = Convert.ToString(GetThirdPartyStaticData_New(dtXMLCity.Tables[1], "Desc", Convert.ToString(Natinality), "XmlNationality", "asianoverland"));
                            break;
                        case "darina":
                            dataFor.XmlNationality = Convert.ToString(GetThirdPartyStaticData_New(dtXMLCity.Tables[1], "Desc", Convert.ToString(Natinality), "XmlNationality", "darina"));

                            break;
                        case "jactravel":
                            // XmlNationality = Convert.ToString((Common.GetThirdPartyStaticData_Xml("Nationality", "Desc", System.Convert.ToString(Natinality), "SystemMasterId", "Agoda")));
                            dataFor.XmlNationality = Convert.ToString(GetThirdPartyStaticData_New(dtXMLCity.Tables[1], "Desc", Convert.ToString(Natinality), "Desc", "Agoda"));

                            break;
                        case "mgholiday":
                            // XmlNationality = Convert.ToString((Common.GetThirdPartyStaticData_Xml("Nationality", "Desc", System.Convert.ToString(Natinality), "SystemMasterId", "Agoda")));
                            dataFor.XmlNationality = Convert.ToString(GetThirdPartyStaticData_New(dtXMLCity.Tables[1], "Desc", Convert.ToString(Natinality), "XmlNationality", "MgHoliday"));

                            break;

                        case "travstore":
                        case "methabook":
                        case "youtravel":
                        case "olympia":
                        case "intratour":
                        case "abreuonline":
                        case "infinitehotelsfrance":
                        case "restel":
                        case "eref":
                        case "hoteldo":
                        case "gtbeds":
                        case "tbo":
                        case "tbo_api":
                        case "iwtx":
                        case "omnibees":
                        case "robios":
                        case "fastpayhotelsv3":
                        case "pricetravel":
                        case "paximum":
                        case "iboosy":
                        case "parsec":
                            dataFor.XmlNationality = Convert.ToString(GetThirdPartyStaticData_New(dtXMLCity.Tables[1], "Desc", Convert.ToString(Natinality), "XmlNationality", "Agoda"));

                            break;
                        case "whitesandsuae":
                            dataFor.XmlNationality = Convert.ToString(GetThirdPartyStaticData_New(dtXMLCity.Tables[1], "Desc", Convert.ToString(Natinality), "XmlNationality", "MgHoliday"));
                            break;
                        case "nuitee":
                            dataFor.XmlNationality = Convert.ToString(GetThirdPartyStaticData_New(dtXMLCity.Tables[1], "Desc", Convert.ToString(Natinality), "XmlNationality", "Agoda"));
                            break;
                        case "grnc":
                            dataFor.XmlNationality = Convert.ToString(GetThirdPartyStaticData_New(dtXMLCity.Tables[1], "Desc", Convert.ToString(Natinality), "XmlNationality", "grnc"));

                            break;
                        case "uk_travco":
                            dataFor.XmlNationality = Convert.ToString(GetThirdPartyStaticData_New(dtXMLCity.Tables[1], "Desc", Convert.ToString(Natinality), "XmlNationality", "Agoda"));
                            DataSet DSNationalityData = new DataSet();
                            if (!_cache.TryGetValue("uk_travco_Nationality", out DSNationalityData))
                            {
                                DSNationalityData = ReadXmlFileToDataSet("Hotel/UkTravcoNationalityXMLFile.xml");
                                _cache.Set("uk_travco_Nationality", DSNationalityData, new MemoryCacheEntryOptions().SetAbsoluteExpiration(DateTime.Now.AddHours(12)));
                            }
                            break;
                        case "hotelstrack":
                            dataFor.XmlNationality = Convert.ToString(GetThirdPartyStaticData_New(dtXMLCity.Tables[1], "Desc", Convert.ToString(Natinality), "XmlNationality", "hotelstrack"));

                            break;

                        default:
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
            }
            return dataFor;
        }



        public string GetThirdPartyStaticData_New(DataTable dt, string FieldToSearch, string FieldSearchFor, string ReturnField, string Supplier)
        {
            try
            {
                string ReturnText = "";


                DataRow[] drdsMeal = null;
                if (dt != null)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        if (Convert.ToString(row[FieldToSearch]).ToLower() == FieldSearchFor.ToLower() && Supplier.ToLower() == Convert.ToString(row["Supplier"]).ToLower())
                        {
                            drdsMeal = new DataRow[1];
                            drdsMeal[0] = row;
                            break;
                        }
                    }
                }
                if (drdsMeal != null)
                {
                    ReturnText = Convert.ToString(drdsMeal[0][ReturnField]);
                }

                return ReturnText;
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
                return "";
            }

        }

        public string Star_Rating(string HotelStar)
        {
            switch (HotelStar)
            {
                case "1 Star": { HotelStar = "1"; } break;
                case "1 Star +": { HotelStar = "2"; } break;
                case "2 Star": { HotelStar = "2"; } break;
                case "2 Star +": { HotelStar = "3"; } break;
                case "3 Star": { HotelStar = "3"; } break;
                case "3 Star +": { HotelStar = "4"; } break;
                case "4 Star": { HotelStar = "4"; } break;
                case "4 Star +": { HotelStar = "5"; } break;
                case "5 Star": { HotelStar = "5"; } break;
                case "5 Star +": { HotelStar = "5"; } break;
            }
            return HotelStar;
        }

        public DataSet ReadXmlFile(string fileName)
        {
            DataSet dsDT = new DataSet();
            string filePath = Path.Combine(AppContext.BaseDirectory, fileName);
            //string filePath = Path.Combine(Directory.GetCurrentDirectory(), fileName);

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"File not found: {filePath}");
            }

            using (FileStream fs1 = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                dsDT.ReadXml(fs1);
            }

            return dsDT;
        }
        public DateTime GetTimeZoneDate(string strDate)
        {
            //strDate = DateTime.Now.ToString();
            string[] str = strDate.Split('/');
            DateTime tmpEntryDate = new DateTime(Convert.ToInt32(str[0]), Convert.ToInt32(str[1]), Convert.ToInt32(str[2]), Convert.ToInt32(str[3]), Convert.ToInt32(str[4]), 0);
            return tmpEntryDate;
        }

        public string GetStarRatingString(HotelSearchParameter SearchPara)
        {
            try
            {
                string Rating = string.Empty;
                if (SearchPara.StarStart <= 0 && SearchPara.StarEnd >= 0)
                    Rating = "0,";

                if (SearchPara.StarStart <= 1 && SearchPara.StarEnd >= 1)
                    Rating = "1,";
                if (SearchPara.StarStart <= 2 && SearchPara.StarEnd >= 2)
                    Rating = Rating + "2,";
                if (SearchPara.StarStart <= 3 && SearchPara.StarEnd >= 3)
                    Rating = Rating + "3,";
                if (SearchPara.StarStart <= 4 && SearchPara.StarEnd >= 4)
                    Rating = Rating + "4,";
                if (SearchPara.StarStart <= 5 && SearchPara.StarEnd >= 5)
                    Rating = Rating + "5,";

                Rating = Rating.Substring(0, Rating.Length - 1);
                return Rating;
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
                return "";
            }
        }

        public DataSet GetDataSetFromXMLString(string xmlString)
        {
            DataSet HotelBedsResponse = new DataSet();
            byte[] buffer = Encoding.UTF8.GetBytes(xmlString);
            using (MemoryStream stream = new MemoryStream(buffer))
            {
                XmlReader reader = XmlReader.Create(stream);
                HotelBedsResponse.ReadXml(reader);
            }
            return HotelBedsResponse;
        }

        public async Task FireAway_APILOG(string RequestId, string Request, string Response, string Token, string ApiName, string IpAddress, int AgentId,
            string ElapsedTime, string ErrorCode, string CityId, string CustomerIpAddress, Int64 HotelId, string HotelName, string provider
            , string HotelID = "", string CheckInDate = "", string CheckOutDate = "", int NoofNights = 0, int Rooms = 0, int LogId = 0, string Search_Key = "", DateTime? EntryTime = null
            , string ReferenceNo = "", string InternalReference = "", string hostName = "", string MainURL = "", string ActiveMongoDB_ForAPI_SearchLogs = "")
        {
            //System.Diagnostics.Stopwatch timerroom3 = new System.Diagnostics.Stopwatch();
            //timerroom3.Start();


            string RQ = "";
            string RS = "";
            //bool ActiveMongoDB_ForAPI_SearchLogs = true;
            if (ActiveMongoDB_ForAPI_SearchLogs == "1")//Added on 19-FEB-2025
            {
                await Task.Run(async () => await Write_API_RQRS_MongoDB(Request, Response, Search_Key, EntryTime));
            }
            else
            {
                RQ = Request;
                RS = Response;
            }


            #region //@NEW For Save Supplier Log
            try
            {
                SqlParameter[] para = new SqlParameter[]{
                   new SqlParameter("@RequestId",""),
                   new SqlParameter("@Request",RQ),
                   new SqlParameter("@Response",RS),
                   //new SqlParameter("@Token",Token),
                   new SqlParameter("@Token",""),
                   new SqlParameter("@ApiName",ApiName),
                   new SqlParameter("@IpAddress",IpAddress),
                   new SqlParameter("@AgentId",AgentId),
                   new SqlParameter("@ElapsedTime",ElapsedTime),
                   new SqlParameter("@ErrorCode",ErrorCode),
                   new SqlParameter("@CityId",CityId),
                   new SqlParameter("@CustomerIpAddress",CustomerIpAddress),
                   new SqlParameter("@HotelName",HotelName),
                   new SqlParameter("@HotelId",HotelId),
                    new SqlParameter("@Provider",""),
                   new SqlParameter("@IsLive",Convert.ToInt32(_keySettings.IsLive)),
                    new SqlParameter("@CheckInDate",CheckInDate),
                    new SqlParameter("@CheckOutDate",CheckOutDate),
                    new SqlParameter("@NoofNights",NoofNights),
                    new SqlParameter("@Rooms",Rooms),
                   new SqlParameter("@Hotelids",HotelID),
                    new SqlParameter("@LogId",LogId),
                    new SqlParameter("@Search_Key",Search_Key),
                    new SqlParameter("@EntryTime",EntryTime),
                     new SqlParameter("@ReferenceNo",ReferenceNo),
                    new SqlParameter("@InternalReference",InternalReference),
                    new SqlParameter("@hostName",hostName),
                    new SqlParameter("@MainURL",MainURL),
                };

                //if (ApiName.ToLower() == "availability" || ApiName.ToLower() == "availabilitywithcancellation")
                //    db.ExecuteNonQuerywithconnection("EnterAPILogs_XMLOUT_New", para, Convert.ToString(ConfigurationManager.ConnectionStrings["ConnectionStringforlogtrack_81"].ConnectionString));
                //else
                //    db.ExecuteNonQuerywithconnection("EnterAPILogs_XMLOUT_New", para, Convert.ToString(ConfigurationManager.ConnectionStrings["ConnectionStringforlogtrack"].ConnectionString));

                //if (ApiName.ToLower() == "availability" || ApiName.ToLower() == "availabilitywithcancellation")
                //    db.ExecuteNonQuerywithconnection("EnterAPILogs_XMLOUT_New_hour", para, Convert.ToString(ConfigurationManager.ConnectionStrings["ConnectionStringforlogtrack_81"].ConnectionString));
                //else
                //    db.ExecuteNonQuerywithconnection("EnterAPILogs_XMLOUT_New_hour", para, Convert.ToString(ConfigurationManager.ConnectionStrings["ConnectionStringforlogtrack"].ConnectionString));

                if (false)// (_rabitmqSettings.IsRabbitMQActive == "1")
                {
                    var messageData = para.ToDictionary(p => p.ParameterName, p => p.Value ?? DBNull.Value);

                    // Send to RabbitMQ
                    await _publisher.SendMessageAsync(messageData, "apilogqueue");
                }
                else
                {
                    await _dlConnection.ExecuteNonQuerywithconnection("EnterAPILogs_XMLOUT_New_hour", para, Convert.ToString(_connectionStrings.ConnectionStringforlogtrack_APILogVM));

                }



            }
            catch (Exception ex)
            {
                await _exception.Error(ex);
            }
            #endregion

            //timerroom3.Stop();
            //string ExecutionTime = "Execution Time :" + timerroom3.Elapsed.ToString();
            //// Write a file                
            //System.IO.File.WriteAllText(@"D:\\FTP\ANKIT\TXT\time.txt", ExecutionTime);
        }


        public async Task EnterApiLogsnew(string RequestId, string Request, string Response, string Token, string ApiName, string IpAddress, int AgentId,
            string ElapsedTime, string ErrorCode, string CityId, string CustomerIpAddress, Int64 HotelId, string HotelName, string provider
            , string HotelID = "", string CheckInDate = "", string CheckOutDate = "", int NoofNights = 0, int Rooms = 0, int LogId = 0, string Search_Key = "", DateTime? EntryTime = null
            , string ReferenceNo = "", string InternalReference = "", string hostName = "")
        {
            //System.Diagnostics.Stopwatch timerroom3 = new System.Diagnostics.Stopwatch();
            //timerroom3.Start();

            #region //@NEW For Save Supplier Log
            try
            {
                if (ApiName.ToLower() == "recheck" || ApiName.ToLower() == "bookingdetail_full")
                {
                    if (ApiName.ToLower() == "recheck")
                        HotelId = Convert.ToInt32(HotelID);

                    SqlParameter[] para = new SqlParameter[]{
                   //new SqlParameter("@RequestId",RequestId),
                   new SqlParameter("@RequestId",""),
                   new SqlParameter("@Request",Request),
                   new SqlParameter("@Response",Response),
                   //new SqlParameter("@Token",Token),
                   new SqlParameter("@Token",""),
                   new SqlParameter("@ApiName",ApiName),
                   new SqlParameter("@IpAddress",IpAddress),
                   new SqlParameter("@AgentId",AgentId),
                   new SqlParameter("@ElapsedTime",ElapsedTime),
                   new SqlParameter("@ErrorCode",ErrorCode),
                   new SqlParameter("@CityId",CityId),
                   new SqlParameter("@CustomerIpAddress",CustomerIpAddress),
                   new SqlParameter("@HotelName",HotelName),
                   new SqlParameter("@HotelId",HotelId),
                    new SqlParameter("@Provider",provider),
                   new SqlParameter("@IsLive",Convert.ToInt32(_keySettings.IsLive)),
                    new SqlParameter("@CheckInDate",CheckInDate),
                    new SqlParameter("@CheckOutDate",CheckOutDate),
                    new SqlParameter("@NoofNights",NoofNights),
                    new SqlParameter("@Rooms",Rooms),
                   new SqlParameter("@Hotelids",HotelID),
                    new SqlParameter("@LogId",LogId),
                    new SqlParameter("@Search_Key",Search_Key),
                    new SqlParameter("@EntryTime",EntryTime),
                     new SqlParameter("@ReferenceNo",ReferenceNo),
                    new SqlParameter("@InternalReference",InternalReference),
                    new SqlParameter("@hostName",hostName)

                };

                    //if (ApiName.ToLower() == "availability" || ApiName.ToLower() == "availabilitywithcancellation")
                    //    db.ExecuteNonQuerywithconnection("EnterAPILogs_XMLOUT_New", para, Convert.ToString(ConfigurationManager.ConnectionStrings["ConnectionStringforlogtrack_81"].ConnectionString));
                    //else
                    //    db.ExecuteNonQuerywithconnection("EnterAPILogs_XMLOUT_New", para, Convert.ToString(ConfigurationManager.ConnectionStrings["ConnectionStringforlogtrack"].ConnectionString));

                    //if (ApiName.ToLower() == "availability" || ApiName.ToLower() == "availabilitywithcancellation")
                    //    db.ExecuteNonQuerywithconnection("EnterAPILogs_XMLOUT_New_hour", para, Convert.ToString(ConfigurationManager.ConnectionStrings["ConnectionStringforlogtrack_81"].ConnectionString));
                    //else
                    //    db.ExecuteNonQuerywithconnection("EnterAPILogs_XMLOUT_New_hour", para, Convert.ToString(ConfigurationManager.ConnectionStrings["ConnectionStringforlogtrack"].ConnectionString));

                    await _dlConnection.ExecuteNonQuerywithconnection("EnterAPILogs_XMLOUT_New", para, Convert.ToString(_connectionStrings.ConnectionStringforlogtrack_180VM));
                }
                else
                {


                    SqlParameter[] para = new SqlParameter[]{
                   new SqlParameter("@RequestId",RequestId),
                   new SqlParameter("@Request",Request),
                   new SqlParameter("@Response",Response),
                   //new SqlParameter("@Token",Token),
                   new SqlParameter("@Token",""),
                   new SqlParameter("@ApiName",ApiName),
                   new SqlParameter("@IpAddress",IpAddress),
                   new SqlParameter("@AgentId",AgentId),
                   new SqlParameter("@ElapsedTime",ElapsedTime),
                   new SqlParameter("@ErrorCode",ErrorCode),
                   new SqlParameter("@CityId",CityId),
                   new SqlParameter("@CustomerIpAddress",CustomerIpAddress),
                   new SqlParameter("@HotelName",HotelName),
                   new SqlParameter("@HotelId",HotelId),
                    new SqlParameter("@Provider",provider),
                   new SqlParameter("@IsLive",Convert.ToInt32(_keySettings.IsLive)),
                    new SqlParameter("@CheckInDate",CheckInDate),
                    new SqlParameter("@CheckOutDate",CheckOutDate),
                    new SqlParameter("@NoofNights",NoofNights),
                    new SqlParameter("@Rooms",Rooms),
                   new SqlParameter("@Hotelids",HotelID),
                    new SqlParameter("@LogId",LogId),
                    new SqlParameter("@Search_Key",Search_Key),
                    new SqlParameter("@EntryTime",EntryTime),
                     new SqlParameter("@ReferenceNo",ReferenceNo),
                    new SqlParameter("@InternalReference",InternalReference),
                    new SqlParameter("@hostName",hostName)

                };

                    //if (ApiName.ToLower() == "availability" || ApiName.ToLower() == "availabilitywithcancellation")
                    //    db.ExecuteNonQuerywithconnection("EnterAPILogs_XMLOUT_New", para, Convert.ToString(ConfigurationManager.ConnectionStrings["ConnectionStringforlogtrack_81"].ConnectionString));
                    //else
                    //    db.ExecuteNonQuerywithconnection("EnterAPILogs_XMLOUT_New", para, Convert.ToString(ConfigurationManager.ConnectionStrings["ConnectionStringforlogtrack"].ConnectionString));

                    //if (ApiName.ToLower() == "availability" || ApiName.ToLower() == "availabilitywithcancellation")
                    //    db.ExecuteNonQuerywithconnection("EnterAPILogs_XMLOUT_New_hour", para, Convert.ToString(ConfigurationManager.ConnectionStrings["ConnectionStringforlogtrack_81"].ConnectionString));
                    //else
                    //    db.ExecuteNonQuerywithconnection("EnterAPILogs_XMLOUT_New_hour", para, Convert.ToString(ConfigurationManager.ConnectionStrings["ConnectionStringforlogtrack"].ConnectionString));

                    await _dlConnection.ExecuteNonQuerywithconnection("EnterAPILogs_XMLOUT_New", para, Convert.ToString(_connectionStrings.ConnectionStringforlogtrack));
                }

            }
            catch (Exception ex)
            {
                await _exception.Error(ex);
            }
            #endregion

            //timerroom3.Stop();
            //string ExecutionTime = "Execution Time :" + timerroom3.Elapsed.ToString();
            //// Write a file                
            //System.IO.File.WriteAllText(@"D:\\FTP\ANKIT\TXT\time.txt", ExecutionTime);
        }

        public DataRow setCancellationPolicyRow(ref double CompanyChargeAmount, DataRow drHotelCancellation, double CurrencyFactor, string Currency, string SupplierName, string ownsystemhotelId, string RoomNo, string RoomTypeId,
        string RoomTypeName, string MealId, DateTime FromDate, DateTime ToDate, double CompanyCancellationPrice, double AgentCancellationPrice, string BoardTypeID, double VatOnPolicy, string RoomSequenceNo = "", string WithInCancellationDeadLine = "", string PurchaseToken = "", string SPUI = "", string CartID = "", string GIITACode = "")
        {
            drHotelCancellation["SupplierName"] = SupplierName;
            drHotelCancellation["OwnsystemHotelId"] = ownsystemhotelId;
            drHotelCancellation["RoomNo"] = RoomNo;
            drHotelCancellation["RoomTypeId"] = RoomTypeId;
            drHotelCancellation["RoomTypeName"] = RoomTypeName;
            drHotelCancellation["MealId"] = MealId;
            drHotelCancellation["FromDate"] = FromDate;
            drHotelCancellation["ToDate"] = ToDate;
            drHotelCancellation["CompanyCancellationPrice"] = CompanyCancellationPrice;
            drHotelCancellation["AgentCancellationPrice"] = AgentCancellationPrice;
            drHotelCancellation["BoardTypeID"] = BoardTypeID;
            drHotelCancellation["RoomSequenceNo"] = RoomSequenceNo;
            drHotelCancellation["WithInCancellationDeadLine"] = WithInCancellationDeadLine;
            drHotelCancellation["PurchaseToken"] = PurchaseToken;
            drHotelCancellation["SPUI"] = SPUI;
            drHotelCancellation["CartID"] = CartID;
            drHotelCancellation["GIITACode"] = GIITACode;
            drHotelCancellation["VatdiffAmount"] = VatOnPolicy;
            drHotelCancellation["SupplierCurrency"] = Currency;
            drHotelCancellation["BuyingPrice"] = CompanyChargeAmount;
            return drHotelCancellation;
        }

        public DataTable GetCountryCurrency_OLD(string CountryCode)
        {
            DataTable dt = new DataTable();
            try
            {
                SqlParameter[] para = new SqlParameter[]{
     new SqlParameter("@CurrencyId",  CountryCode),
     new SqlParameter("@WithMarkup",  0)

   };
                dt = _dlConnection.GetDataTable("GetCurrencyFactor_IOH", para);
                return dt;
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
                return dt;
            }

        }

        public DataTable GetCountryCurrency(string CountryCode)
        {
            DataTable dt = new DataTable();
            try
            {
                var keydata = "GetCurrencyFactor_IOH_9";
                DataTable dtAll = new DataTable();



                if (!_cache.TryGetValue(keydata, out dtAll))
                {
                    SqlParameter[] para1 = new SqlParameter[]{
     new SqlParameter("@CurrencyId",  "all"),
     new SqlParameter("@WithMarkup",  0)

   };
                    dtAll = _dlConnection.GetDataTable("GetCurrencyFactor_IOH_Xconnect_today", para1);

                    if (dtAll.Rows.Count > 0)
                    {
                        // Set cache with expiration (example: 12 hours)
                        var cacheEntryOptions = new MemoryCacheEntryOptions()
                            .SetAbsoluteExpiration(DateTime.Now.AddHours(1));  // Set cache to expire in 12 hours

                        _cache.Set(keydata, dtAll, cacheEntryOptions);
                    }
                }

                DataRow[] drROE = dtAll.Select("currencyshortname='" + CountryCode + "'");

                return drROE.CopyToDataTable();
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
                return dt;
            }

        }

        public int GetCutOffDays(DataTable dtSuplierMarkup, string SupplierName)
        {
            int CutOffDays = 1;
            if (dtSuplierMarkup.Columns.Contains("CutOffDays"))
            {
                DataRow[] drCutOffDays = null;
                foreach (DataRow row in dtSuplierMarkup.Rows)
                {
                    if (Convert.ToString(row["SupplierName"]).ToLower().Replace(" ", "") == SupplierName.ToLower())
                    {
                        drCutOffDays = new DataRow[1];
                        drCutOffDays[0] = row;
                        break;
                    }
                }
                // DataRow[] drCutOffDays = dtSuplierMarkup.Select("SupplierName='"+ SupplierName + "'");

                if (drCutOffDays != null)
                {
                    CutOffDays = Convert.ToInt32(drCutOffDays[0]["CutOffDays"]);
                }
            }
            return -CutOffDays;
        }

        public bool IsSuppliersWithSeperateRoomRquest(string Supplier)
        {
            try
            {
                switch (Supplier.ToLower())
                {
                    case "genting":
                    case "goodwin":
                    case "quantum":
                        //case "hotelbedsv1":
                        //case "travellanda":
                        return true;
                        break;
                    default:
                        return false;
                        break;
                }

            }
            catch (Exception ex)
            {
                _exception.Error(ex);
            }
            return false;
        }

        public double GetROEByCurrencyBackup030226(string Currency, DataTable dtROE)
        {
            double exchnage = 5;
            try
            {
                if (Currency != "" && dtROE.Rows.Count > 0)
                {

                    DataRow[] drROE = dtROE.Select("currencyshortname='" + Currency + "'");
                    if (drROE.Length > 0)
                    {
                        exchnage = Convert.ToDouble(drROE[0]["AmtToExch"]);
                        if (exchnage == 0)
                            exchnage = 5;
                    }
                }

            }
            catch (Exception ex)
            {
                _exception.Error(ex);
            }

            return exchnage;

        }


        public double GetROEByCurrency(string Currency, DataTable dtROE)
        {
            double exchnage = 5;
            try
            {
                if (Currency != "" && dtROE.Rows.Count > 0)
                {

                    DataRow[] drROE = dtROE.Select("currencyCode='" + Currency + "'");
                    if (drROE.Length > 0)
                    {
                        exchnage = Convert.ToDouble(drROE[0]["buyingROE"]);
                        if (exchnage == 0)
                            exchnage = 5;
                    }
                }

            }
            catch (Exception ex)
            {
                _exception.Error(ex);
            }

            return exchnage;

        }

        public int GetCurrencyIdCurrencyCode(string Currency, DataTable dtROE)
        {
            int currencyId = 5;
            try
            {
                if (Currency != "" && dtROE.Rows.Count > 0)
                {

                    DataRow[] drROE = dtROE.Select("currencyCode='" + Currency + "'");
                    if (drROE.Length > 0)
                    {
                        currencyId = Convert.ToInt32(drROE[0]["currencyId"]);
                        if (currencyId == 0)
                            currencyId = 5;
                    }
                }

            }
            catch (Exception ex)
            {
                _exception.Error(ex);
            }

            return currencyId;

        }

        public string GetCurrencyCodeByCurrencyID(int CurrencyId, DataTable dtROE)
        {
            string currencyCode = "";
            try
            {
                if (CurrencyId != 0 && dtROE.Rows.Count > 0)
                {

                    DataRow[] drROE = dtROE.Select("currencyId='" + CurrencyId + "'");
                    if (drROE.Length > 0)
                    {
                        currencyCode = Convert.ToString(drROE[0]["currencyCode"]);
                        if (currencyCode == "")
                            currencyCode = "USD";
                    }
                }

            }
            catch (Exception ex)
            {
                _exception.Error(ex);
            }

            return currencyCode;

        }

        public void GetPrefferedHotelCodes(ref string Ids, ref string xmlbase, ref List<SupplierSearchConfig> SearchConfig, string SupplierName, DataTable dtPreferredDataHotel, string IsSearchFor)
        {
            try
            {
                DataRow[] drgta = dtPreferredDataHotel.Select("ServiceName='" + SupplierName + "'");
                if (drgta.Count() > 0)
                {
                    var paramValues1 = String.Join(",", drgta.Select(x => String.Join(",", Convert.ToString(x.ItemArray[0]))));
                    Ids = Convert.ToString(paramValues1);
                    if (Ids.Split(',').Length > 1 && SupplierName.ToLower() == "gta")
                        xmlbase = Convert.ToString(drgta[0]["XmlBaseId"]);
                }

                if (IsSearchFor.ToLower().IndexOf("preferred") >= 0 && drgta.Count() == 0)
                {
                    SearchConfig = null;
                }
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
            }
        }

        public void GetXtgCurrencyByCountry(string Supplier, string CountryId, ref string currency, string Agt = "")
        {
            try
            {
                currency = "USD";
                #region Welcombeds
                if (Supplier.ToLower() == "welcomebeds_xtg")
                {
                    switch (CountryId.ToLower())
                    {
                        case "united kingdom":
                            currency = "GBP";
                            break;
                        case "switzerland":
                            currency = "EUR";
                            break;
                        case "united states":
                        case "brazil":
                        case "mexico":
                        case "colombia":
                        case "argentina":
                        case "peru":
                        case "venezuela":
                        case "chile":
                        case "ecuador":
                        case "guatemala":
                        case "cuba":
                        case "haiti":
                        case "bolivia":
                        case "dominican republic":
                        case "honduras":
                        case "paraguay":
                        case "nicaragua":
                        case "el salvador":
                        case "costa rica":
                        case "panama":
                        case "puerto rico":
                        case "uruguay":
                        case "guadeloupe":
                        case "martinique":
                        case "saint martin (french part)":
                            currency = "USD";
                            break;
                        default:
                            currency = "EUR";
                            break;
                    }
                }
                #endregion
                #region "WHL"
                if (Supplier.ToLower() == "whl_xtg")
                {
                    switch (CountryId.ToLower())
                    {
                        #region //WHL Office
                        case "italy"://ITALY  15403 
                        case "turkey"://TURKEY   15491
                        case "croatia"://CROATIA
                        case "germany"://GERMANY
                        case "mexico"://MEXICO
                        case "austria"://AUSTRIA
                        case "france"://FRANCE
                        case "hungary"://HUNGARY
                        case "malta"://MALTA
                        case "czech republic"://CZECH REPUBLIC
                        case "greece"://GREECE
                        case "netherlands"://NETHERLANDS
                        case "united arab emirates"://UNITED ARAB EMIRATES
                        case "cuba"://CUBA
                        case "belgium"://BELGIUM
                        case "seychelles"://SEYCHELLES
                        case "cyprus"://CYPRUS
                        case "mauritius"://MAURITIUS
                        case "morocco"://MOROCCO
                        case "maldives"://MALDIVES
                        case "albania"://ALBANIA
                        case "jamaica"://JAMAICA
                        case "slovenia"://SLOVENIA
                            currency = "EUR";
                            break;
                        #endregion
                        #region//China Office
                        case "china"://CHINA
                            currency = "EUR";
                            break;
                        #endregion
                        #region//spain Office 
                        case "spain"://SPAIN
                        case "portugal"://PORTUGAL
                        case "dominican republic"://DOMINICAN REPUBLIC
                        case "cape verde"://CAPO VERDE
                            currency = "EUR";
                            break;
                        #endregion
                        #region//Uk Office 
                        case "united kingdom"://UNITED KINGDOM
                        case "ireland"://IRELAND
                            currency = "GBP";
                            break;
                        #endregion
                        #region//usa office
                        case "aruba"://ARUBA
                        case "united states"://UNITED STATES
                        case "bahamas"://BAHAMAS
                        case "barbados"://BARBADOS
                        case "virgin islands (usa)"://VIRGIN ISLANDS (US)
                        case "puerto rico"://PUERTO RICO
                        case "anguilla"://ANGUILLA
                        case "saint lucia"://ST LUCIA
                            currency = "USD";
                            break;
                        #endregion
                        default:
                            currency = "EUR";
                            break;
                    }
                }
                #endregion
                if (Supplier.ToLower() == "smyrooms_xtg")
                {
                    currency = "USD"; //03-Dec-2024 As per ahmed
                    #region Comment Code
                    //GBP for  UK Destination  
                    //Euro : Europe Destinations
                    //Cuba :  Euro
                    //USD Currency:  for REST OF THE WORLD DESTINATION
                    //switch (CountryId.ToLower())
                    //{
                    //    #region//Uk Office 
                    //    case "united kingdom"://UNITED KINGDOM
                    //    case "ireland"://IRELAND
                    //        currency = "GBP";
                    //        break;
                    //    #endregion
                    //    case "cuba"://CUBA
                    //        currency = "EUR";
                    //        break;
                    //    #region //Europe Office
                    //    case "albania":
                    //    case "andorra":
                    //    case "armenia":
                    //    case "austria":
                    //    case "azerbaijan":
                    //    case "belarus":
                    //    case "belgium":
                    //    case "bosnia herzegovina":
                    //    case "bulgaria":
                    //    case "croatia":
                    //    case "cyprus":
                    //    case "czech republic":
                    //    case "denmark":
                    //    case "estonia":
                    //    case "finland":
                    //    case "france":
                    //    case "georgia":
                    //    case "germany":
                    //    case "greece":
                    //    case "hungary":
                    //    case "iceland":
                    //    case "italy":
                    //    case "kazakhstan":
                    //    case "kosovo":
                    //    case "latvia":
                    //    case "liechtenstein":
                    //    case "lithuania":
                    //    case "luxembourg":
                    //    case "macedonia":
                    //    case "malta":
                    //    case "moldova":
                    //    case "monaco":
                    //    case "montenegro":
                    //    case "netherlands":
                    //    case "norway":
                    //    case "poland":
                    //    case "portugal":
                    //    case "romania":
                    //    case "russia":
                    //    case "san marino (republic of)":
                    //    case "serbia":
                    //    case "slovakia":
                    //    case "slovenia":
                    //    case "spain":
                    //    case "sweden":
                    //    case "switzerland":
                    //    case "turkey":
                    //    case "ukraine":
                    //    case "vatican city state (holy see)":
                    //        currency = "EUR";
                    //        break;
                    //    #endregion

                    //    default:
                    //        currency = "USD";
                    //        break;
                    //}
                    #endregion
                }
                if (Supplier.ToLower() == "italcamel_xtg" || Supplier.ToLower() == "oswald_xtg" || Supplier.ToLower() == "rategain_xtg")
                {
                    currency = "EUR";
                }
                if (Supplier.ToLower().IndexOf("xpedia") >= 0)
                {
                    currency = "USD";


                    switch (CountryId.ToUpper())
                    {
                        case "malaysia":
                            currency = "MYR";
                            break;
                        case "saudi arabia":
                            currency = "SAR";
                            break;
                        case "united arab emirates":
                            currency = "AED";
                            break;
                        #region//Uk Office 
                        case "united kingdom"://UNITED KINGDOM
                        case "ireland"://IRELAND
                            currency = "GBP";
                            break;
                        #endregion
                        #region //Europe Office
                        case "albania":
                        case "andorra":
                        case "armenia":
                        case "austria":
                        case "azerbaijan":
                        case "belarus":
                        case "belgium":
                        case "bosnia herzegovina":
                        case "bulgaria":
                        case "croatia":
                        case "cyprus":
                        case "czech republic":
                        case "denmark":
                        case "estonia":
                        case "finland":
                        case "france":
                        case "georgia":
                        case "germany":
                        case "greece":
                        case "hungary":
                        case "iceland":
                        case "italy":
                        case "kazakhstan":
                        case "kosovo":
                        case "latvia":
                        case "liechtenstein":
                        case "lithuania":
                        case "luxembourg":
                        case "macedonia":
                        case "malta":
                        case "moldova":
                        case "monaco":
                        case "montenegro":
                        case "netherlands":
                        case "norway":
                        case "poland":
                        case "portugal":
                        case "romania":
                        case "russia":
                        case "san marino (republic of)":
                        case "serbia":
                        case "slovakia":
                        case "slovenia":
                        case "spain":
                        case "sweden":
                        case "switzerland":
                        //case "turkey":
                        case "ukraine":
                        case "vatican city state (holy see)":
                            currency = "EUR";
                            break;
                        #endregion

                        default:
                            currency = "USD";
                            break;
                    }


                }
                #region "AC Tours"
                if (Supplier.ToLower() == "actours_xtg")
                {
                    currency = "USD";


                    switch (CountryId.ToLower())
                    {
                        #region//Uk Office 
                        case "united kingdom"://UNITED KINGDOM
                        case "ireland"://IRELAND
                            currency = "GBP";
                            break;
                        #endregion
                        #region //Europe Office
                        case "albania":
                        case "andorra":
                        case "armenia":
                        case "austria":
                        case "azerbaijan":
                        case "belarus":
                        case "belgium":
                        case "bosnia herzegovina":
                        case "bulgaria":
                        case "croatia":
                        case "cyprus":
                        case "czech republic":
                        case "denmark":
                        case "estonia":
                        case "finland":
                        case "france":
                        case "georgia":
                        case "germany":
                        case "greece":
                        case "hungary":
                        case "iceland":
                        case "italy":
                        case "kazakhstan":
                        case "kosovo":
                        case "latvia":
                        case "liechtenstein":
                        case "lithuania":
                        case "luxembourg":
                        case "macedonia":
                        case "malta":
                        case "moldova":
                        case "monaco":
                        case "montenegro":
                        case "netherlands":
                        case "norway":
                        case "poland":
                        case "portugal":
                        case "romania":
                        case "russia":
                        case "san marino (republic of)":
                        case "serbia":
                        case "slovakia":
                        case "slovenia":
                        case "spain":
                        case "sweden":
                        case "switzerland":
                        case "turkey":
                        case "ukraine":
                        case "vatican city state (holy see)":
                            currency = "EUR";
                            break;
                        #endregion
                        default:
                            currency = "USD";
                            break;
                    }


                }
                #endregion
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
            }
        }

        public DataTable GenerateDataTableNewV2()
        {
            try
            {
                DataTable dtHotel = new DataTable("Hotel");
                DataColumn dcColumn;
                dcColumn = new DataColumn("OwnsystemHotelId", typeof(string));
                dtHotel.Columns.Add(dcColumn);
                dcColumn = null;

                dcColumn = new DataColumn("XmlHotelCity", typeof(string));
                dtHotel.Columns.Add(dcColumn);

                dcColumn = new DataColumn("XMLHotelCode", typeof(string));
                dtHotel.Columns.Add(dcColumn);

                dcColumn = new DataColumn("HotelName", typeof(string));
                dtHotel.Columns.Add(dcColumn);

                dcColumn = new DataColumn("HotelFrontImage", typeof(string));
                dtHotel.Columns.Add(dcColumn);

                dcColumn = new DataColumn("Address", typeof(string));
                dtHotel.Columns.Add(dcColumn);

                dcColumn = new DataColumn("IsRecomondedHotel", typeof(Boolean));
                dtHotel.Columns.Add(dcColumn);

                dcColumn = new DataColumn("Location", typeof(string));
                dtHotel.Columns.Add(dcColumn);

                dcColumn = new DataColumn("XMLCountry", typeof(string));
                dtHotel.Columns.Add(dcColumn);

                dcColumn = new DataColumn("XMLCity", typeof(string));
                dtHotel.Columns.Add(dcColumn);

                dcColumn = new DataColumn("XMLCode", typeof(string));
                dtHotel.Columns.Add(dcColumn);

                dcColumn = new DataColumn("Rating", typeof(int));
                dtHotel.Columns.Add(dcColumn);

                dcColumn = new DataColumn("latitude", typeof(string));
                dtHotel.Columns.Add(dcColumn);

                dcColumn = new DataColumn("longitute", typeof(string));
                dtHotel.Columns.Add(dcColumn);

                dcColumn = new DataColumn("IsOffer", typeof(string));
                dtHotel.Columns.Add(dcColumn);

                dcColumn = new DataColumn("OfferText", typeof(string));
                dtHotel.Columns.Add(dcColumn);

                dcColumn = new DataColumn("MealName", typeof(string));
                dtHotel.Columns.Add(dcColumn);

                dcColumn = new DataColumn("BeforeDiscountPrice", typeof(decimal));
                dtHotel.Columns.Add(dcColumn);

                dcColumn = new DataColumn("SellingPrice", typeof(decimal));
                dtHotel.Columns.Add(dcColumn);

                dcColumn = new DataColumn("CompanyBuyingPrice", typeof(decimal));
                dtHotel.Columns.Add(dcColumn);

                dcColumn = new DataColumn("CompanySellingPrice", typeof(decimal));
                dtHotel.Columns.Add(dcColumn);

                dcColumn = new DataColumn("AgentBuyingPrice", typeof(decimal));
                dtHotel.Columns.Add(dcColumn);

                dcColumn = new DataColumn("AgentSellingPrice", typeof(decimal));
                dtHotel.Columns.Add(dcColumn);

                dcColumn = new DataColumn("SubAgentBuyingPice", typeof(decimal));
                dtHotel.Columns.Add(dcColumn);

                dcColumn = new DataColumn("SubAgentSellingPrice", typeof(decimal));
                dtHotel.Columns.Add(dcColumn);

                dcColumn = new DataColumn("FinalSellingPrice", typeof(decimal));
                dtHotel.Columns.Add(dcColumn);

                dcColumn = new DataColumn("IsRefundable", typeof(Boolean));
                dtHotel.Columns.Add(dcColumn);

                dcColumn = new DataColumn("SupplierName", typeof(string));
                dtHotel.Columns.Add(dcColumn);

                dcColumn = new DataColumn("OfficeID", typeof(string));
                dtHotel.Columns.Add(dcColumn);

                dcColumn = new DataColumn("ContractID", typeof(int));
                dtHotel.Columns.Add(dcColumn);

                dcColumn = new DataColumn("ContractName", typeof(string));
                dtHotel.Columns.Add(dcColumn);

                dcColumn = new DataColumn("Reviews", typeof(int));
                dtHotel.Columns.Add(dcColumn);

                dcColumn = new DataColumn("EssentialInformation", typeof(string));
                dtHotel.Columns.Add(dcColumn);

                dcColumn = new DataColumn("Description", typeof(string));
                dtHotel.Columns.Add(dcColumn);

                dcColumn = new DataColumn("AvailToken", typeof(string));
                dtHotel.Columns.Add(dcColumn);

                dcColumn = new DataColumn("UniqueNo", typeof(string));//To display hotel room,generate id
                dtHotel.Columns.Add(dcColumn);

                dcColumn = new DataColumn("IsShowSupplier", typeof(Boolean));//Show hide Supplier Code
                dtHotel.Columns.Add(dcColumn);

                dcColumn = new DataColumn("BookingStatus", typeof(string));//Show hide Supplier Code
                dtHotel.Columns.Add(dcColumn);

                dcColumn = new DataColumn("ReviewRating", typeof(int));
                dcColumn.DefaultValue = 0;
                dtHotel.Columns.Add(dcColumn);

                dcColumn = new DataColumn("ReviewCount", typeof(int));
                dcColumn.DefaultValue = 0;
                dtHotel.Columns.Add(dcColumn);

                dcColumn = new DataColumn("SortSequence", typeof(int));
                dcColumn.DefaultValue = 9999;
                dtHotel.Columns.Add(dcColumn);

                dcColumn = new DataColumn("GroupName", typeof(string));
                dtHotel.Columns.Add(dcColumn);

                dcColumn = new DataColumn("Favorite", typeof(int));
                dtHotel.Columns.Add(dcColumn);

                dcColumn = new DataColumn("GIITACode", typeof(string));
                dtHotel.Columns.Add(dcColumn);

                //dcColumn = new DataColumn("MappingId", typeof(string));
                //dtHotel.Columns.Add(dcColumn);

                dcColumn = new DataColumn("RaynaId", typeof(string));

                dtHotel.Columns.Add(dcColumn);

                dcColumn = new DataColumn("SupplierId", typeof(string));
                dtHotel.Columns.Add(dcColumn);

                dcColumn = new DataColumn("VatdiffAmount", typeof(decimal));

                dtHotel.Columns.Add(dcColumn);

                dcColumn = new DataColumn("SupplierCurrency", typeof(string));
                dtHotel.Columns.Add(dcColumn);
                dcColumn = new DataColumn("IsSearchFor", typeof(string));
                dtHotel.Columns.Add(dcColumn);

                dcColumn = new DataColumn("HotelPriority", typeof(string));
                dtHotel.Columns.Add(dcColumn);

                dcColumn = new DataColumn("MasterSupplierId", typeof(int));
                dcColumn.DefaultValue = 0;
                dtHotel.Columns.Add(dcColumn);

                dcColumn = new DataColumn("ID", typeof(int));
                dcColumn.DefaultValue = 0;
                dtHotel.Columns.Add(dcColumn);

                dcColumn = new DataColumn("ReservationEmail", typeof(string));
                dtHotel.Columns.Add(dcColumn);
                dcColumn = new DataColumn("HotelContactNo", typeof(string));
                dtHotel.Columns.Add(dcColumn);

                return dtHotel;
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
            }
            return null;
        }

        public DataTable GenerateRoomtypeDataTableNewV2()
        {
            try
            {
                DataTable DtHotelRoom = new DataTable();

                DtHotelRoom.Columns.Add("OwnsystemHotelId");
                DtHotelRoom.Columns.Add("RoomNo");
                DtHotelRoom.Columns.Add("RoomTypeName");
                DtHotelRoom.Columns.Add("MealName");
                //New columns start
                DtHotelRoom.Columns.Add("SHRUI");
                DtHotelRoom.Columns.Add("Board");
                DtHotelRoom.Columns.Add("Type");
                DtHotelRoom.Columns.Add("BoardType");
                DtHotelRoom.Columns.Add("RoomTypeCode");
                DtHotelRoom.Columns.Add("Characteristic");
                //New columns end
                DtHotelRoom.Columns.Add("RoomTypeId");
                DtHotelRoom.Columns.Add("MealId");
                DtHotelRoom.Columns.Add("CompanyBuyingPrice");
                DtHotelRoom.Columns.Add("CompanySellingPrice");
                DtHotelRoom.Columns.Add("AgentBuyingPrice");
                DtHotelRoom.Columns.Add("AgentSellingPrice");
                DtHotelRoom.Columns.Add("SubAgentBuyingPice");
                DtHotelRoom.Columns.Add("SubAgentSellingPrice");
                DtHotelRoom.Columns.Add("FinalSellingPrice", typeof(decimal));
                DtHotelRoom.Columns.Add("IsRefundable");
                DtHotelRoom.Columns.Add("SupplierName");
                DtHotelRoom.Columns.Add("CancellationDeadline");

                DtHotelRoom.Columns.Add("EssentialInformation");

                DtHotelRoom.Columns.Add("allocationDetails");
                DtHotelRoom.Columns.Add("minStay");
                DtHotelRoom.Columns.Add("DateApplyMinStay");

                DtHotelRoom.Columns.Add("OfferCode");
                DtHotelRoom.Columns.Add("IncudedOfferDiscount");
                DtHotelRoom.Columns.Add("GrossWitohutDiscount");
                DtHotelRoom.Columns.Add("DiscountPer");
                DtHotelRoom.Columns.Add("DiscountPerText");

                DtHotelRoom.Columns.Add("AdultsOccupancy");
                DtHotelRoom.Columns.Add("ChildOccupancy");
                DtHotelRoom.Columns.Add("MaxExtrabed");
                DtHotelRoom.Columns.Add("ContractID");
                DtHotelRoom.Columns.Add("RoomSequenceNo");
                //By manish
                DtHotelRoom.Columns.Add("OfferDescription");

                DtHotelRoom.Columns.Add("UniqueNo");
                DtHotelRoom.Columns.Add("BookingStatus");
                DtHotelRoom.Columns.Add("BoardTypeID");
                DtHotelRoom.Columns.Add("TotalSurcharge");
                DtHotelRoom.Columns.Add("GIITACode");

                DtHotelRoom.Columns.Add("ContractName");
                DtHotelRoom.Columns.Add("OfficeId");

                DtHotelRoom.Columns.Add("CompanyBuyingPriceExpedia", typeof(decimal));
                DtHotelRoom.Columns.Add("GrossProfitOnline", typeof(decimal));
                DtHotelRoom.Columns.Add("OurProfit", typeof(decimal));
                DtHotelRoom.Columns.Add("CurrencyFactor", typeof(decimal));

                DtHotelRoom.Columns.Add("VatdiffAmount", typeof(decimal));
                DtHotelRoom.Columns.Add("MasterMealId");
                DtHotelRoom.Columns.Add("CheckInDate");
                DtHotelRoom.Columns.Add("CheckOutDate");
                DtHotelRoom.Columns.Add("CityId");
                DtHotelRoom.Columns.Add("IsCombineRoom", typeof(bool));
                DtHotelRoom.Columns.Add("IsPolicyDynamic", typeof(bool));
                DtHotelRoom.Columns.Add("SupplierCurrency");
                DtHotelRoom.Columns.Add("BuyingPrice");
                DtHotelRoom.Columns.Add("OTHPromoCode");
                DtHotelRoom.Columns.Add("FinalFromDate", typeof(DateTime));
                return DtHotelRoom;
            }
            catch (Exception ex)
            {
                //Common.Error(ex);
            }
            return null;
        }

        public DataTable GenerateCancellationDataTableNewV2()
        {
            try
            {
                DataTable dtCancellation = new DataTable();
                dtCancellation.Columns.Add("SupplierName");
                dtCancellation.Columns.Add("OwnsystemHotelId");
                dtCancellation.Columns.Add("RoomNo");
                dtCancellation.Columns.Add("RoomTypeId");
                dtCancellation.Columns.Add("RoomTypeName");
                dtCancellation.Columns.Add("MealId");
                dtCancellation.Columns.Add("FromDate");
                dtCancellation.Columns.Add("ToDate");
                dtCancellation.Columns.Add("CompanyCancellationPrice");
                dtCancellation.Columns.Add("AgentCancellationPrice");
                dtCancellation.Columns.Add("PurchaseToken");
                dtCancellation.Columns.Add("SPUI");
                dtCancellation.Columns.Add("RoomSequenceNo");
                dtCancellation.Columns.Add("CartID");
                dtCancellation.Columns.Add("BoardTypeID");
                dtCancellation.Columns.Add("WithInCancellationDeadLine");
                dtCancellation.Columns.Add("GIITACode");
                dtCancellation.Columns.Add("VatdiffAmount");

                dtCancellation.Columns.Add("SupplierCurrency");
                dtCancellation.Columns.Add("BuyingPrice");

                return dtCancellation;
            }
            catch (Exception ex)
            {
                // Common.Error(ex);
            }
            return null;
        }

        public DataTable GenerateoomRateBreackUpDataTableNewV2()
        {
            try
            {
                DataTable DtHotelRoomRateBreackUp = new DataTable();
                DtHotelRoomRateBreackUp.Columns.Add("SupplierName");
                DtHotelRoomRateBreackUp.Columns.Add("OwnsystemHotelId");
                DtHotelRoomRateBreackUp.Columns.Add("RoomNo");
                DtHotelRoomRateBreackUp.Columns.Add("RoomTypeName");
                DtHotelRoomRateBreackUp.Columns.Add("RateType");
                DtHotelRoomRateBreackUp.Columns.Add("RoomTypeId");
                DtHotelRoomRateBreackUp.Columns.Add("MealId");
                DtHotelRoomRateBreackUp.Columns.Add("Date");
                DtHotelRoomRateBreackUp.Columns.Add("DateOfWeek");
                DtHotelRoomRateBreackUp.Columns.Add("Meal");
                DtHotelRoomRateBreackUp.Columns.Add("Rate");
                DtHotelRoomRateBreackUp.Columns.Add("ContractID");
                DtHotelRoomRateBreackUp.Columns.Add("RoomSequenceNo");
                DtHotelRoomRateBreackUp.Columns.Add("OrderNo");
                DtHotelRoomRateBreackUp.Columns.Add("BookingStatus");
                DtHotelRoomRateBreackUp.Columns.Add("BoardTypeID");
                //for expedia
                DtHotelRoomRateBreackUp.Columns.Add("Surcharge");
                DtHotelRoomRateBreackUp.Columns.Add("CompanyCharge");
                DtHotelRoomRateBreackUp.Columns.Add("Total");
                DtHotelRoomRateBreackUp.Columns.Add("SalesTaxAmount");
                DtHotelRoomRateBreackUp.Columns.Add("HotelOccupancyTaxAmount");
                DtHotelRoomRateBreackUp.Columns.Add("HotelFessDescription");
                DtHotelRoomRateBreackUp.Columns.Add("HotelFeeAmount");
                DtHotelRoomRateBreackUp.Columns.Add("CancellationText");
                DtHotelRoomRateBreackUp.Columns.Add("TotalNight");
                DtHotelRoomRateBreackUp.Columns.Add("GIITACode");
                DtHotelRoomRateBreackUp.Columns.Add("FinalRateType");
                DtHotelRoomRateBreackUp.Columns.Add("CheckIninstruction");
                DtHotelRoomRateBreackUp.Columns.Add("SpecialCheckIninstruction");
                DtHotelRoomRateBreackUp.Columns.Add("VatdiffAmount");
                DtHotelRoomRateBreackUp.Columns.Add("CompanyRate");
                DtHotelRoomRateBreackUp.Columns.Add("SupplierCurrency");
                DtHotelRoomRateBreackUp.Columns.Add("BuyingPrice");
                return DtHotelRoomRateBreackUp;
            }
            catch (Exception ex)
            {
                //Common.Error(ex);
            }
            return null;
        }

        public async Task XMLRequestResponseTrack_DebugMode(string XmlRequest, string XmlResponse, string Provider, string TypeRequest, string reqId = "", string NewSearchKey = "")
        {
            try
            {


                if (false)
                {
                    // Also log to Serilog
                    _logger.LogInformation(
            "Third-Party API Call | Provider: {Provider} | Type: {TypeRequest} | ReqId: {ReqId} | SearchKey: {SearchKey} | Request: {XmlRequest} | Response: {XmlResponse}",
            Provider, TypeRequest, reqId, NewSearchKey, XmlRequest, XmlResponse);

                    //var log = new ResLog
                    //{
                    //    XmlRequest = XmlRequest,
                    //    XmlResponse = XmlResponse,
                    //    Provider = Provider,
                    //    TypeRequest = TypeRequest,
                    //    ReqId = reqId,
                    //    SearchKey = NewSearchKey
                    //};

                    //await _stores.SaveLogAsync(log, "DebugLog");

                }
                else
                {

                    SqlParameter[] para = new SqlParameter[]{
            new SqlParameter("@XmlRequest",XmlRequest),
            new SqlParameter("@XmlResponse",XmlResponse),
            new SqlParameter("@Provider",Provider),
            new SqlParameter("@TypeRequest",TypeRequest),
            new SqlParameter("@ReqId",reqId),
            new SqlParameter("@SearchKey",NewSearchKey)
            };
                    await _dlConnection.GetExecuteScalarSP("InsertXMLRequestResponseTrack_Xconnect", para);
                }
            }
            catch (Exception ex)
            {
                await _exception.Error(ex);
            }
        }

        public async Task XMLRequestResponseTrack(string XmlRequest, string XmlResponse, string Provider, string TypeRequest, string reqId = "", string NewSearchKey = "")
        {
            try
            {

                SqlParameter[] para = new SqlParameter[]{
            new SqlParameter("@XmlRequest",XmlRequest),
            new SqlParameter("@XmlResponse",XmlResponse),
            new SqlParameter("@Provider",Provider),
            new SqlParameter("@TypeRequest",TypeRequest),
            new SqlParameter("@ReqId",reqId),
            new SqlParameter("@SearchKey",NewSearchKey)
            };
                await _dlConnection.GetExecuteScalarSP("InsertXMLRequestResponseTrack_Xconnect", para);
            }
            catch (Exception ex)
            {
                await _exception.Error(ex);
            }
        }

        //public  void XMLRequestResponseTrackError(string XmlRequest, string XmlResponse, string Provider, string TypeRequest)
        //{
        //    try
        //    {

        //        SqlParameter[] para = new SqlParameter[]{
        //    new SqlParameter("@XmlRequest",XmlRequest),
        //    new SqlParameter("@XmlResponse",XmlResponse),
        //    new SqlParameter("@Provider",Provider),
        //    new SqlParameter("@TypeRequest",TypeRequest)
        //    };
        //        _dlConnection.GetExecuteScalarSP("InsertXMLRequestResponseTrackError", para);
        //    }
        //    catch (Exception ex)
        //    {
        //        //ExManager.Exception.LogException(ex.Message);
        //        _exception.Error(ex);
        //    }
        //}

        public async Task XMLRequestResponseTrack_Error(string XmlRequest, string XmlResponse, string Provider, string TypeRequest, string reqId = "", string NewSearchKey = "")
        {
            try
            {

                SqlParameter[] para = new SqlParameter[]{
            new SqlParameter("@XmlRequest",XmlRequest),
            new SqlParameter("@XmlResponse",XmlResponse),
            new SqlParameter("@Provider",Provider),
            new SqlParameter("@TypeRequest",TypeRequest),
            new SqlParameter("@ReqId",reqId),
            new SqlParameter("@SearchKey",NewSearchKey)
            };
                await _dlConnection.GetExecuteScalarSP_140Server("InsertXMLRequestResponseTrack_Error_Xconnect", para);
            }
            catch (Exception ex)
            {
                await _exception.Error(ex);
            }
        }

        public async Task FireAway_SQL(string Request, string Response, string Supplier, string NewSearchKey, Int32 SSTimeout, int AgentId)
        {
            //System.Diagnostics.Stopwatch timerroom3 = new System.Diagnostics.Stopwatch();
            //timerroom3.Start();

            #region //@NEW For Save Supplier Log
            try
            {

                string encodedStr_RQ = Convert.ToBase64String(Encoding.UTF8.GetBytes(Request));
                string encodedStr_RS = Convert.ToBase64String(Encoding.UTF8.GetBytes(Response));
                //string Sb = "{\"Token\":\"test\", \"Request\":\"" + encodedStr_RQ + "\",\"Response\":\"" + encodedStr_RS + "\",\"Provider\":\"" + Supplier + "\",\"SearchKey\":\"" + NewSearchKey + "\"}";
                //string str = Common.GetSendSupplierResponseLogAPI("http://savesupplierlog.withinearth.com/api/APIStoreSupplierLog/SaveLog", Sb, NewSearchKey, SSTimeout);
                try
                {
                    SqlParameter[] para = new SqlParameter[]{
                    new SqlParameter("@Request",encodedStr_RQ),
                    new SqlParameter("@Response",encodedStr_RS),
                  new SqlParameter("@Provider",Supplier),
                  new SqlParameter("@SearchKey",NewSearchKey),
                  new SqlParameter("@AgentId",AgentId)

                 };
                    await _dlConnection.ExecuteNonQuery_SaveSupplierLog("savesuppliersearchlog", para);

                }
                catch (Exception ex)
                {
                    if (Convert.ToString(_keySettings.SaveSupplier_Error_LogActive_95) == "1")
                        await _exception.Error(ex, NewSearchKey);
                }
            }
            catch (Exception ex)
            {
                if (Convert.ToString(_keySettings.SaveSupplier_Error_LogActive_95) == "1")
                    await _exception.Error(ex, NewSearchKey);
            }
            #endregion
        }

        public async Task FireAway_SQL_ResPorcessTimeLog(string Request, string Response, string Supplier, string NewSearchKey, Int32 SSTimeout, int AgentId)
        {
            #region //@NEW For Save SupplierResProcess time Log

            try
            {
                SqlParameter[] para = new SqlParameter[]{
                    new SqlParameter("@Request",Request),
                    new SqlParameter("@ProcessTime",Response),
                  new SqlParameter("@Provider",Supplier),
                  new SqlParameter("@SearchKey",NewSearchKey),
                  new SqlParameter("@AgentId",AgentId)

                 };

                await _dlConnection.ExecuteNonQuery_timeout("SaveSupplierResProcessLog", para, SSTimeout);
            }
            catch (Exception ex)
            {
                await _exception.Error(ex, NewSearchKey);
            }
            #endregion
        }

        public async Task FireAway_MongoDB_New(string Request, string Response, string Supplier, string NewSearchKey, int SSTimeout)
        {
            #region //@NEW For Save Supplier Log
            try
            {
                string encodedStr_RQ = Convert.ToBase64String(Encoding.UTF8.GetBytes(Request));
                string encodedStr_RS = Convert.ToBase64String(Encoding.UTF8.GetBytes(Response));

                SavesupplierLog_MongoDB json = new SavesupplierLog_MongoDB();
                json.SearchKey = NewSearchKey;
                json.Request = encodedStr_RQ;
                json.Response = encodedStr_RS;
                json.Supplier = Supplier;
                json.Date = DateTime.Now;
                try
                {
                    //MongoClient client = new MongoClient(_mongoSettings.MongoDB_ConnectionStr_SupplierLog);
                    //IMongoDatabase database = client.GetDatabase(_mongoSettings.MongoDB_DatabaseName);
                    //IMongoCollection<SavesupplierLog_MongoDB> _searchlogs = database.GetCollection<SavesupplierLog_MongoDB>(_mongoSettings.MongoDB_CollectionName_SuppLog);
                    //_searchlogs.InsertOne(json);

                    //var database = _mongoClient.GetDatabase(_mongoSettings.MongoDB_DatabaseName);
                    //var _searchlogs = database.GetCollection<SavesupplierLog_MongoDB>(_mongoSettings.MongoDB_CollectionName_SuppLog);
                    //await _searchlogs.InsertOneAsync(json);


                    //MongoClient client = new MongoClient(_mongoSettings.MongoDB_ConnectionStr_SupplierLog);
                    // IMongoDatabase database = client.GetDatabase(_mongoSettings.MongoDB_DatabaseName);
                    // IMongoCollection<SavesupplierLog_MongoDB> _searchlogs = database.GetCollection<SavesupplierLog_MongoDB>(_mongoSettings.MongoDB_CollectionName_SuppLog);
                    IMongoCollection<SavesupplierLog_MongoDB> _searchlogs = _supp.GetCollection<SavesupplierLog_MongoDB>(_mongoSettings.MongoDB_CollectionName_SuppLog); //200525

                    await _searchlogs.InsertOneAsync(json);
                }
                catch (Exception ex)
                {
                    await _exception.Error(ex);
                }
            }
            catch (Exception ex)
            {
                await _exception.Error(ex, NewSearchKey, "MongoDBError");
            }
            #endregion
        }

        public async Task Write_API_RQRS_MongoDB(string RQ, string RS, string SearchKey, DateTime? EntryTime)//Added on 19-FEB-2025
        {
            try
            {
                //string encodedStr_RQ = Convert.ToBase64String(Encoding.UTF8.GetBytes(RQ)); //Comment on 18-DEC-2025
                // string encodedStr_RS = Convert.ToBase64String(Encoding.UTF8.GetBytes(RS)); //Comment on 18-DEC-2025

                SaveAPIRQRSLog_MongoDB jsonobject = new SaveAPIRQRSLog_MongoDB();
                jsonobject.Request = RQ;// encodedStr_RQ; //Comment on 18-DEC-2025
                jsonobject.Response = RS;// encodedStr_RS; //Comment on 18-DEC-2025
                jsonobject.SearchKey = SearchKey;
                jsonobject.Date = EntryTime;

                //MongoClient client = new MongoClient(_mongoSettings.MongoDB_ConStr_SaveAPIRQRSLog);
                //IMongoDatabase database = client.GetDatabase(_mongoSettings.MongoDB_DatabaseName_SaveAPIRQRSLog);
                //IMongoCollection<SaveAPIRQRSLog_MongoDB> _searchlogs = database.GetCollection<SaveAPIRQRSLog_MongoDB>(_mongoSettings.MongoDB_CollectionName_SaveAPIRQRSLog);
                IMongoCollection<SaveAPIRQRSLog_MongoDB> _searchlogs = _QRSLog.GetCollection<SaveAPIRQRSLog_MongoDB>(_mongoSettings.MongoDB_CollectionName_SaveAPIRQRSLog); //200525

                await _searchlogs.InsertOneAsync(jsonobject);
            }
            catch (Exception ex)
            {
                await _exception.Error(ex);
            }
        }
        public async Task FireAway_SPL_SQL(ConcurrentBag<HotelX> AllTask, int AgentId, HotelSearchParameter searchpara, bool isapi = true)
        {
            try
            {
                DataTable Supplierreport = HtmlExtensions.ConvertToDataTableforConcurrentBag(AllTask);
                DataTable SupplierPerformancereport1 = Supplierreport.DefaultView.ToTable(true, "AgentID", "Supplier", "ReportType", "ResponseType", "RSTimeout", "ResponseStatus", "StrHotelIds", "Host"); //Host Added on 17-Dec-2025

                #region Analytics
                try
                {
                    string stage = "Search";
                    if (isapi)
                    {
                        stage = "Search";
                    }
                    else
                    {
                        stage = "SearchById";
                    }
                    _analyticSupplier.PushHotelSearchSupplierDataForAnalyticsAsyc(new AnalyticsData
                    {
                        Result = AllTask,
                        Action = "SupplierReportsearch",
                        Version = "v1",
                        HttpStatusCode = Convert.ToInt32(HttpStatusCode.OK),
                        Stage = stage,
                        Payload = searchpara,
                        ElapsedTime = 0,
                        Description = "Supplier request",
                        StaticResult = null,
                        Search_Key = searchpara.SearchKey,
                    });
                }
                catch (Exception ex)
                {
                    await _exception.Error(ex);
                }
                #endregion

                if (_mongoSettings.SupplierPerformanceReport_MongoDB == "1")//Update on 10-MAY-2025
                {
                    if (SupplierPerformancereport1 != null && SupplierPerformancereport1.Rows.Count > 0)//Correction 10-MAY-2025 add saprate if and remove from above if condition.
                    {

                        var collection = _SPRLog.GetCollection<SaveSPR_MongoDB>(_mongoSettings.MongoDB_CollectionName_SPR);//080525
                        var documents = new List<SaveSPR_MongoDB>();
                        DateTime EntryTime = DateTime.Now;
                        //foreach (DataRow row in SupplierPerformancereport1.Rows)
                        //{
                        //    TimeSpan time = TimeSpan.TryParse(Convert.ToString(row["RSTimeout"]), out var parsed) ? parsed : TimeSpan.Zero; //added on 07-07-25

                        //    // Convert to double (seconds.milliseconds)
                        //    double roundedRSTimeout = Math.Round(time.TotalMilliseconds / 1000.0, 3);

                        //    SaveSPR_MongoDB DataX = new SaveSPR_MongoDB
                        //    {
                        //        AgentId = AgentId,
                        //        Supplier = Convert.ToString(row["Supplier"]),
                        //        ResponseType = Convert.ToString(row["ResponseType"]),
                        //        Date = EntryTime,
                        //        RSTimeout = roundedRSTimeout//Added on 17-MAR-2025

                        //    };
                        //    await collection.InsertOneAsync(DataX);
                        //}

                        string hostName = System.Net.Dns.GetHostName(); //Added on 17-Dec-2025
                        switch (hostName)
                        {
                            case "s12526947":
                                hostName = "1";
                                break;
                            case "s12526948":
                                hostName = "2";
                                break;
                            case "API1":
                                hostName = "1";
                                break;
                            case "API2":
                                hostName = "2";
                                break;
                            case "API3":
                                hostName = "3";
                                break;
                            case "API4":
                                hostName = "4";
                                break;
                            case "API5":
                                hostName = "5";
                                break;
                            case "API6":
                                hostName = "6";
                                break;
                            case "API-7":
                                hostName = "7";
                                break;
                            default:
                                break;
                        }

                        foreach (DataRow row in SupplierPerformancereport1.Rows) // Update on 13-NOV-2025 //Update ResStatusCod,Host on 17-Dec-2025
                        {
                            TimeSpan time = TimeSpan.TryParse(Convert.ToString(row["RSTimeout"]), out var parsed) ? parsed : TimeSpan.Zero;

                            double roundedRSTimeout = Math.Round(time.TotalMilliseconds / 1000.0, 3);

                            documents.Add(new SaveSPR_MongoDB
                            {
                                AgentId = AgentId,
                                Supplier = Convert.ToString(row["Supplier"]),
                                ResponseType = Convert.ToString(row["ResponseType"]),
                                Date = EntryTime,
                                RSTimeout = roundedRSTimeout,
                                ResStatusCod = Convert.ToInt32(row["ResponseStatus"]),
                                Host = Convert.ToString(hostName)
                            });
                        }

                        if (documents.Count > 0)
                        {
                            await collection.InsertManyAsync(documents);
                        }
                    }
                }
                else
                {
                    SqlParameter[] para = new SqlParameter[] {
                    new SqlParameter("@ReportType","SearchAvailability"),
                    new SqlParameter("@AgentID",AgentId),
                    new SqlParameter("@SupplierPerformancereport",SupplierPerformancereport1)
                    };
                    //DLConnection db = new DLConnection();
                    //System.Diagnostics.Stopwatch timerroomprocess = new System.Diagnostics.Stopwatch();
                    //timerroomprocess.Start();
                    //DataSet abc = db.GetDataSet_Limit("SupplierPerformanceReportData_api", para, 1);

                    //dl.ExecuteNonQuery_SupplierLog_report("SupplierPerformanceReportData_api", para);
                    //dl.ExecuteNonQuery_SupplierLog_report("SupplierPerformanceReportData_API_v2", para); // Adding Response Status Code
                    //await _dlConnection.ExecuteNonQuery_SupplierLog_report("SupplierPerformanceReportData_API_v3", para); // for save Hotelids for CM and OTH
                    _ = _dlConnection.ExecuteNonQuery_SupplierLog_report("SupplierPerformanceReportData_API_v3", para); // safe fire-and-forget



                    //timerroomprocess.Stop();
                    //string time = timerroomprocess.Elapsed.ToString();
                }


            }
            catch (Exception ex)
            {
                await SaveErrorLogForSPReport("1# " + ex.Message + " #AgentID:" + Convert.ToString(AgentId));
                if (Convert.ToString(_keySettings.SaveSupplier_Error_LogActive_95) == "1")
                    await _exception.Error(ex);
            }
        }

        public async Task SaveErrorLogForSPReport(string Error)//@Added on 22-AUG-2024
        {
            try
            {
                SqlParameter[] para = new SqlParameter[]{
                     new SqlParameter("@Error",  Error),
                    };
                await _dlConnection.ExecuteNonQuery("SaveErrorLogForSPReport_SP", para);
            }
            catch (Exception x)
            {
                await _exception.Error(x, "", "SaveErrorLogForSPReport_SP");
            }
        }

        public static string CacheKey_Search = "SearchKeyList_";
        public async Task<AvailHotelList> MakeCacheForHotelListMaster_OLD(string filepath, string Extension, string NewSearchKey, TokenData Tokendata, AvailHotelList finalResponse, int operation)
        {
            try
            {
                AvailHotelList availHotelList = new AvailHotelList();
                bool isError = false;
                string folderPath = GenerateCacheFilePath(Tokendata);

                if (!_cache.TryGetValue(CacheMaster.CacheKey_Search + NewSearchKey, out availHotelList))
                {
                    {
                        if (operation == Convert.ToInt32(CacheMaster._CacheOperation.save))
                        {
                            if (!Directory.Exists(folderPath))
                                Directory.CreateDirectory(folderPath);
                            try
                            {
                                List<List<_HotelListNew>> tempList = new List<List<_HotelListNew>>();

                                //AvailHotelList availHotelList = new AvailHotelList();
                                availHotelList.Search_Key = NewSearchKey;
                                availHotelList.Time = DateTime.Now;
                                AvailabilityRS availabilityRS = new AvailabilityRS();
                                availabilityRS.SearchKey = finalResponse.AvailabilityRS.SearchKey;
                                availabilityRS.Count = finalResponse.AvailabilityRS.Count;
                                availHotelList.AvailabilityRS = availabilityRS;
                                availHotelList.AvailabilityRS.HotelResult = new List<_HotelListNew>();
                                if (!string.IsNullOrEmpty(NewSearchKey))
                                {
                                    if (_mongoSettings.MongoDB_Active_for_Cachefile == "1")
                                    {
                                        availHotelList.AvailabilityRS.HotelResult = finalResponse.AvailabilityRS.HotelResult;
                                        await WriteDataToFile_MongoDB(NewSearchKey, availHotelList);//#MongoDB
                                    }
                                    else
                                    {
                                        //tempList = splitList<_HotelListNew>(finalResponse.AvailabilityRS.HotelResult, CacheMaster.CacheKey_Devider).ToList();

                                        //for (int i = 0; i < tempList.Count; i++)
                                        //{
                                        //    availHotelList.AvailabilityRS.HotelResult = tempList[i];
                                        //    string json = Newtonsoft.Json.JsonConvert.SerializeObject(availHotelList);
                                        //    string tempPath = folderPath + "\\" + filepath + "_" + (i + 1) + Extension;
                                        //    bool error = WriteDataToFile(tempPath, json);
                                        //    if (error)
                                        //        isError = true;
                                        //}
                                    }
                                }

                                return availHotelList;
                            }
                            catch (Exception ex)
                            {
                                await _exception.Error(ex);
                                isError = true;
                            }

                            //HttpRuntime.Cache.Insert(NewSearchKey + NewSearchKey, NewSearchKey, null, DateTime.Now.AddHours(24), TimeSpan.Zero);
                            //return Convert.ToInt32(CacheMaster._CacheError.success);
                            return availHotelList;
                        }
                    }
                }
                if (isError)
                    return null;// Convert.ToInt32(CacheMaster._CacheError.error);
                else
                    return null;// Convert.ToInt32(CacheMaster._CacheError.success);

            }
            catch (Exception ex)
            {
                await _exception.Error(ex);
                return null;// Convert.ToInt32(CacheMaster._CacheError.error);
            }

        }

        public async Task<AvailHotelList> MakeCacheForHotelListMaster(string filepath, string Extension, string NewSearchKey, TokenData Tokendata, AvailHotelList finalResponse, int operation)
        {
            try
            {
                AvailHotelList availHotelList = new AvailHotelList();
                bool isError = false;

                if (true)// (_cache.TryGetValue(CacheMaster.CacheKey_Search + NewSearchKey,out availHotelList))
                {
                    {
                        if (operation == Convert.ToInt32(CacheMaster._CacheOperation.save))
                        {
                            //if (!Directory.Exists(folderPath))
                            //    Directory.CreateDirectory(folderPath);
                            try
                            {
                                List<List<_HotelListNew>> tempList = new List<List<_HotelListNew>>();

                                //AvailHotelList availHotelList = new AvailHotelList();
                                availHotelList.Search_Key = NewSearchKey;
                                availHotelList.Time = DateTime.Now;
                                AvailabilityRS availabilityRS = new AvailabilityRS();
                                availabilityRS.SearchKey = finalResponse.AvailabilityRS.SearchKey;
                                availabilityRS.Count = finalResponse.AvailabilityRS.Count;
                                availHotelList.AvailabilityRS = availabilityRS;
                                availHotelList.AvailabilityRS.HotelResult = new List<_HotelListNew>();
                                if (!string.IsNullOrEmpty(NewSearchKey))
                                {
                                    if (_mongoSettings.MongoDB_Active_for_Cachefile == "1")
                                    {
                                        availHotelList.AvailabilityRS.HotelResult = finalResponse.AvailabilityRS.HotelResult;
                                        await WriteDataToFile_MongoDB(NewSearchKey, availHotelList);//#MongoDB
                                    }
                                    else
                                    {
                                        //string folderPath = GenerateCacheFilePath(Tokendata);


                                        //if (!Directory.Exists(folderPath))
                                        //    Directory.CreateDirectory(folderPath);

                                        //tempList = splitList<_HotelListNew>(finalResponse.AvailabilityRS.HotelResult, CacheMaster.CacheKey_Devider).ToList();

                                        //for (int i = 0; i < tempList.Count; i++)
                                        //{
                                        //    availHotelList.AvailabilityRS.HotelResult = tempList[i];
                                        //    string json = Newtonsoft.Json.JsonConvert.SerializeObject(availHotelList);
                                        //    string tempPath = folderPath + "\\" + filepath + "_" + (i + 1) + Extension;
                                        //    bool error = WriteDataToFile(tempPath, json);
                                        //    if (error)
                                        //        isError = true;
                                        //}
                                    }
                                }

                                return availHotelList;
                            }
                            catch (Exception ex)
                            {
                                await _exception.Error(ex);
                                isError = true;
                            }

                            //HttpRuntime.Cache.Insert(NewSearchKey + NewSearchKey, NewSearchKey, null, DateTime.Now.AddHours(24), TimeSpan.Zero);
                            //return Convert.ToInt32(CacheMaster._CacheError.success);
                            return availHotelList;
                        }
                    }
                }
                if (isError)
                    return null;// Convert.ToInt32(CacheMaster._CacheError.error);
                else
                    return null;// Convert.ToInt32(CacheMaster._CacheError.success);

            }
            catch (Exception ex)
            {
                await _exception.Error(ex);
                return null;// Convert.ToInt32(CacheMaster._CacheError.error);
            }

        }
        public bool WriteDataToFile(string path, string json)
        {
            bool isError = true;
            try
            {

                using (FileStream fs1 = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Write))
                {
                    fs1.SetLength(0);
                    using (StreamWriter sw = new StreamWriter(fs1, Encoding.UTF8, json.Length))
                    {
                        sw.WriteLine(json);
                    }
                    return isError;
                }
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
                var random = new MersenneTwister(); //
                int id = random.Next(500, 1500);
                Thread.Sleep(id);
                return WriteDataToFile2(path, json);
            }

        }

        public bool WriteDataToFile2(string path, string json)
        {
            bool isError = true;
            try
            {

                using (FileStream fs1 = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Write))
                {
                    fs1.SetLength(0);
                    using (StreamWriter sw = new StreamWriter(fs1, Encoding.UTF8, json.Length))
                    {
                        sw.WriteLine(json);
                    }
                    return isError;
                }
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
                return false;
            }

        }

        public string GenerateCacheFilePath(TokenData tokenData)
        {
            try
            {
                string[] paths = { Convert.ToString(_keySettings.xconnectCache),
                                   Convert.ToString(_keySettings.IsLive),
                                   DateTime.Now.Year.ToString(),
                                   DateTime.Now.Month.ToString() ,
                                   DateTime.Now.Day.ToString(),
                                   DateTime.Now.Hour.ToString(),
                                   tokenData.AgentCode
                                    };
                string fullPath = Path.Combine(paths);
                return fullPath;
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
            }
            return "";
        }
        //public static IEnumerable<List<T>> splitList<T>(List<T> locations, int nSize = 30)
        //{
        //    for (int i = 0; i < locations.Count; i += nSize)
        //    {
        //        yield return locations.GetRange(i, Math.Min(nSize, locations.Count - i));
        //    }
        //}

        public async Task WriteDataToFile_MongoDB(string searchKey, AvailHotelList jsonobject)
        {
            try
            {
                //MongoClient client = new MongoClient(_mongoSettings.MongoDB_ConnectionStr);
                //IMongoDatabase database = client.GetDatabase(_mongoSettings.MongoDB_DatabaseName);
                //IMongoCollection<AvailHotelList> _searchlogs = database.GetCollection<AvailHotelList>(_mongoSettings.MongoDB_CollectionName);
                //await _searchlogs.InsertOneAsync(jsonobject);

                //var database = _mongoClient.GetDatabase(_mongoSettings.MongoDB_DatabaseName);
                //var _searchlogs = database.GetCollection<AvailHotelList>(_mongoSettings.MongoDB_CollectionName);
                //await _searchlogs.InsertOneAsync(jsonobject);


                //MongoClient client = new MongoClient(_mongoSettings.MongoDB_ConnectionStr1);
                //IMongoDatabase database = client.GetDatabase(_mongoSettings.MongoDB_DatabaseName);
                //IMongoCollection<AvailHotelList> _searchlogs = database.GetCollection<AvailHotelList>(_mongoSettings.MongoDB_CollectionName);
                IMongoCollection<AvailHotelList> _searchlogs = _main1.GetCollection<AvailHotelList>(_mongoSettings.MongoDB_CollectionName);//200525

                await _searchlogs.InsertOneAsync(jsonobject);
            }
            catch (Exception ex)
            {
                await _exception.Error(ex);
            }
        }

        public async Task<AvailHotelList_Comp> ConvertToFinalResponseList_RoomMapping_old(AvailHotelList finalResponse, string NewSearchKey, bool withcancellation = false, HotelSearchParameter SearchPara = null, string AgentCode = "")
        {
            AvailabilityRS_Comp availHotelList_Comp = new AvailabilityRS_Comp();
            AvailHotelList_Comp finalResponse_Comp = new AvailHotelList_Comp();
            availHotelList_Comp.HotelResult = new List<_HotelListNew_Comp>();

            try
            {
                availHotelList_Comp.SearchKey = NewSearchKey;
                int ab = 0;
                // Global dictionary to track unique roomKeys across all HotelOptions
                HashSet<string> globalRoomKeySet = new HashSet<string>();
                DataTable DtHotelInformation = new DataTable();
                System.Diagnostics.Stopwatch timerx = new System.Diagnostics.Stopwatch();
                timerx.Start();
                string[] sr = SearchPara.Filters.HotelIDs.Split(',');
                if (sr.Length == 1)
                    DtHotelInformation = await GetVervoRoomMappingdata(Convert.ToInt32(sr[0]));

                if (sr.Length > 1)
                    DtHotelInformation = await GetVervoRoomMappingdata(ab, SearchPara.Filters.HotelIDs);
                //DataTable DtHotelInformation = Common.GetVervoRoomMappingdata(gitaid);
                if (SearchPara != null)
                {
                    timerx.Stop();
                    SearchPara.SupplierTime += ",|RoomMappingSP" + "|Time :" + timerx.Elapsed.ToString();
                }

                for (int giataSeq = 0; giataSeq < finalResponse.AvailabilityRS.HotelResult.Count; giataSeq++)
                {
                    _HotelListNew_Comp finallist = new _HotelListNew_Comp
                    {
                        HotelId = finalResponse.AvailabilityRS.HotelResult[giataSeq].HotelId,
                        StartPrice = finalResponse.AvailabilityRS.HotelResult[giataSeq].StartPrice
                    };

                    // Dictionary to store unique options based on room configurations for the current hotel
                    Dictionary<string, Options_Comp> uniqueOptionsMap = new Dictionary<string, Options_Comp>();
                    string gitaid = finalResponse.AvailabilityRS.HotelResult[giataSeq].HotelCode;


                    for (int hotelSeq = 0; hotelSeq < finalResponse.AvailabilityRS.HotelResult[giataSeq].HotelOption.Count; hotelSeq++)
                    {
                        _HotelList hotel = finalResponse.AvailabilityRS.HotelResult[giataSeq].HotelOption[hotelSeq].HotelOptionId.DecryptHotelToken();

                        // Sort rooms in this HotelOption by price
                        var sortedRoomGroups = finalResponse.AvailabilityRS.HotelResult[giataSeq].HotelOption[hotelSeq].HotelRooms
                            .SelectMany(roomGroup => roomGroup) // Flatten the groups for sorting
                            .OrderBy(room => room.Price)
                            .GroupBy(room => room.RoomNo) // Regroup by RoomNo after sorting
                            .ToList();

                        // Dictionary to hold the lowest-priced unique rooms in the current HotelOption
                        var uniqueRoomMap = new Dictionary<string, _FinalRoomList_Comp>();
                        int totalroom = 0;
                        foreach (var roomGroup in sortedRoomGroups)
                        {
                            var roomList = roomGroup.ToList(); // Convert roomGroup to a list to access indices
                            for (int roomIndex = 0; roomIndex < roomList.Count; roomIndex++)
                            {
                                var currentRoom = roomList[roomIndex];

                                // Process RoomTypeName and Refundability
                                string processedRoomTypeName = getMappedRoom_rlm_old(currentRoom.RoomTypeName.Split('|')[0].Trim(), DtHotelInformation, Convert.ToInt32(hotel.OwnsystemHotelId));
                                string refundability = "non-refundable";
                                List<_FinalPolicy> cancellationPolicies = new List<_FinalPolicy>();

                                if (withcancellation && currentRoom.CancellationPolicy != null)
                                {
                                    foreach (var policy in currentRoom.CancellationPolicy)
                                    {
                                        var cancellationPolicy = new _FinalPolicy
                                        {
                                            FromDate = Convert.ToDateTime(policy.FromDate).ToString("MM-dd-yyyy"),
                                            ToDate = Convert.ToDateTime(policy.ToDate).ToString("MM-dd-yyyy"),
                                            CancellationPrice = Convert.ToDecimal(policy.CancellationPrice)
                                        };
                                        cancellationPolicies.Add(cancellationPolicy);

                                        if (DateTime.TryParse(policy.FromDate, out DateTime fromDate) &&
                                            fromDate == DateTime.Today &&
                                            policy.CancellationPrice == 0.0m)
                                        {
                                            refundability = "refundable";
                                        }
                                    }
                                }
                                string roomKey = "";
                                if (SearchPara.Rooms.Count == 1)
                                {
                                    if (_supplierRes.SwapMealName(AgentCode.ToLower()))//Added on 17-DEC-2024
                                                                                       // Generate roomKey based on RoomNo, RoomTypeName, MappedMealName, and Refundability

                                        roomKey = $"{currentRoom.RoomNo}_{processedRoomTypeName.Split('|')[0]}_{currentRoom.MealName}_{refundability}";
                                    else
                                        roomKey = $"{currentRoom.RoomNo}_{processedRoomTypeName.Split('|')[0]}_{currentRoom.MappedMealName}_{refundability}";
                                }
                                else
                                {
                                    if (_supplierRes.SwapMealName(AgentCode.ToLower()))//Added on 17-DEC-2024
                                                                                       // Generate roomKey based on RoomNo, RoomTypeName, MappedMealName, and Refundability

                                        roomKey = $"{currentRoom.RoomNo}_{processedRoomTypeName.Split('|')[0]}_{currentRoom.MealName}_{refundability}_{finalResponse.AvailabilityRS.HotelResult[giataSeq].HotelOption[hotelSeq].IsCombineRoom}";
                                    else
                                        roomKey = $"{currentRoom.RoomNo}_{processedRoomTypeName.Split('|')[0]}_{currentRoom.MappedMealName}_{refundability}_{finalResponse.AvailabilityRS.HotelResult[giataSeq].HotelOption[hotelSeq].IsCombineRoom}";
                                }
                                // Check global map to ensure no duplicate roomKey across all HotelOptionId
                                if (globalRoomKeySet.Contains(roomKey))
                                {
                                    continue; // Skip adding this room as it is a duplicate
                                }

                                // Initialize RoomList for this unique room
                                _FinalRoomList_Comp RoomList = new _FinalRoomList_Comp
                                {
                                    RoomToken = new CacheRoomToken
                                    {
                                        giataid_Seq = giataSeq,
                                        hotelid_Seq = hotelSeq,
                                        Room_seqno = roomIndex,
                                        roomno = totalroom
                                    }.EncryptCacheRoomToken(),
                                    RoomNo = currentRoom.RoomNo,
                                    RoomTypeName = processedRoomTypeName,
                                    MealName = currentRoom.MealName,
                                    Price = currentRoom.Price,
                                    BookingStatus = currentRoom.BookingStatus,
                                    MappedMealName = currentRoom.MappedMealName,
                                    CancellationPolicy = cancellationPolicies
                                };

                                // Add room to uniqueRoomMap if it’s the lowest-priced or if not already added
                                if (!uniqueRoomMap.ContainsKey(roomKey) || currentRoom.Price < uniqueRoomMap[roomKey].Price)
                                {
                                    uniqueRoomMap[roomKey] = RoomList;
                                    globalRoomKeySet.Add(roomKey); // Track globally to avoid duplicate roomKey
                                }
                            }

                            totalroom++;
                        }

                        // Create the HotelOption with unique rooms only if uniqueRoomMap has values
                        //if (uniqueRoomMap.Values.Any())
                        //{
                        //    Options_Comp option = new Options_Comp
                        //    {
                        //        HotelOptionId = $"{giataSeq}|{hotel.OwnsystemHotelId}|{hotel.SupplierId}|{hotelSeq}",
                        //        MinPrice = uniqueRoomMap.Values.Min(r => r.Price),
                        //        IsCombineRoom = finalResponse.AvailabilityRS.HotelResult[giataSeq].HotelOption[hotelSeq].IsCombineRoom,
                        //        HotelRooms = new List<List<_FinalRoomList_Comp>> { uniqueRoomMap.Values.ToList() }
                        //    };

                        //    // Unique key for HotelOption based on the sorted unique rooms
                        //    string optionKey = string.Join(",", uniqueRoomMap.Values.Select(r => $"{r.RoomNo}_{r.RoomTypeName.Split('|')[0]}_{r.MappedMealName}_{(r.CancellationPolicy.Any(p => p.CancellationPrice == 0) ? "refundable" : "non-refundable")}"));

                        //    // Add the option if it’s cheaper or does not already exist in uniqueOptionsMap
                        //    if (!uniqueOptionsMap.ContainsKey(optionKey) || option.MinPrice < uniqueOptionsMap[optionKey].MinPrice)
                        //    {
                        //        uniqueOptionsMap[optionKey] = option;
                        //    }
                        //}

                        // Create the HotelOption with unique rooms only if uniqueRoomMap has values
                        if (uniqueRoomMap.Values.Any())
                        {
                            if (finalResponse.AvailabilityRS.HotelResult[giataSeq].HotelOption[hotelSeq].IsCombineRoom == false && SearchPara.Rooms.Count > 1)
                            {
                                //Options_Comp option = new Options_Comp
                                //{
                                //    HotelOptionId = $"{giataSeq}|{hotel.OwnsystemHotelId}|{hotel.SupplierId}|{hotelSeq}",
                                //    MinPrice = uniqueRoomMap.Values.Sum(r => r.Price),
                                //    IsCombineRoom = finalResponse.AvailabilityRS.HotelResult[giataSeq].HotelOption[hotelSeq].IsCombineRoom,

                                //    // Group all rooms in a single outer array
                                //    HotelRooms = new List<List<_FinalRoomList_Comp>>
                                //    {
                                //        uniqueRoomMap.Values.OrderBy(r => r.RoomNo).ToList() // Ensure rooms are sorted by RoomNo
                                //    }
                                //};


                                Options_Comp option = new Options_Comp
                                {
                                    HotelOptionId = $"{giataSeq}|{hotel.OwnsystemHotelId}|{hotel.SupplierId}|{hotelSeq}",
                                    // Set MinPrice to the sum of prices for the RoomTypeName with the cheapest RoomNo 1
                                    MinPrice = uniqueRoomMap.Values
                                                    .Where(r => Convert.ToInt32(r.RoomNo) == 1) // Filter RoomNo 1
                                                    .OrderBy(r => r.Price)     // Find the cheapest RoomNo 1
                                                    .Select(r => uniqueRoomMap.Values
                                                        .Where(room => room.RoomTypeName == r.RoomTypeName && room.MappedMealName == r.MappedMealName) // Match the RoomTypeName
                                                        .Sum(room => room.Price)) // Sum prices of all rooms with the same RoomTypeName
                                                    .FirstOrDefault(), // Get the sum for the RoomTypeName with the cheapest RoomNo 1
                                    IsCombineRoom = finalResponse.AvailabilityRS.HotelResult[giataSeq].HotelOption[hotelSeq].IsCombineRoom,

                                    // Group rooms by RoomTypeName
                                    HotelRooms = uniqueRoomMap.Values
                                                .GroupBy(r => (r.RoomTypeName, r.MappedMealName))
                                                .Select(group => group.OrderBy(r => r.RoomNo).ToList()) // Sort rooms by RoomNo in each group
                                                .ToList()
                                };


                                // Unique key for HotelOption based on the sorted unique rooms
                                string optionKey = string.Join(",", uniqueRoomMap.Values.Select(r => $"{r.RoomNo}_{r.RoomTypeName.Split('|')[0]}_{r.MappedMealName}_{(r.CancellationPolicy.Any(p => p.CancellationPrice == 0) ? "refundable" : "non-refundable")}"));

                                // Add the option if it’s cheaper or does not already exist in uniqueOptionsMap
                                if (!uniqueOptionsMap.ContainsKey(optionKey) || option.MinPrice < uniqueOptionsMap[optionKey].MinPrice)
                                {
                                    uniqueOptionsMap[optionKey] = option;
                                }
                            }
                            else
                            {
                                if (uniqueRoomMap.Values.Any())
                                {
                                    Options_Comp option = new Options_Comp
                                    {
                                        HotelOptionId = $"{giataSeq}|{hotel.OwnsystemHotelId}|{hotel.SupplierId}|{hotelSeq}",
                                        MinPrice = uniqueRoomMap.Values.Min(r => r.Price),
                                        IsCombineRoom = finalResponse.AvailabilityRS.HotelResult[giataSeq].HotelOption[hotelSeq].IsCombineRoom,
                                        HotelRooms = uniqueRoomMap.Values
                                            .Select(room => new List<_FinalRoomList_Comp> { room }) // Wrap each room in its own list
                                            .ToList()
                                    };

                                    // Unique key for HotelOption based on the sorted unique rooms
                                    string optionKey = string.Join(",", uniqueRoomMap.Values.Select(r => $"{r.RoomNo}_{r.RoomTypeName.Split('|')[0]}_{r.MappedMealName}_{(r.CancellationPolicy.Any(p => p.CancellationPrice == 0) ? "refundable" : "non-refundable")}"));

                                    // Add the option if it’s cheaper or does not already exist in uniqueOptionsMap
                                    if (!uniqueOptionsMap.ContainsKey(optionKey) || option.MinPrice < uniqueOptionsMap[optionKey].MinPrice)
                                    {
                                        uniqueOptionsMap[optionKey] = option;
                                    }
                                }

                            }

                        }



                    }

                    // Add deduplicated options to the final list for the hotel
                    finallist.HotelOption = uniqueOptionsMap.Values.ToList();
                    availHotelList_Comp.HotelResult.Add(finallist);
                }

                availHotelList_Comp.Count = finalResponse.AvailabilityRS.Count;
                availHotelList_Comp.Currency = finalResponse.AvailabilityRS.Currency;
                finalResponse_Comp.AvailabilityRS = availHotelList_Comp;
                return finalResponse_Comp;
            }
            catch (Exception ex)
            {
                await _exception.Error(ex);
            }
            return null;
        }

        public async Task<AvailHotelList_Comp> ConvertToFinalResponseList_RoomMapping(AvailHotelList finalResponse, string NewSearchKey, bool withcancellation = false, HotelSearchParameter SearchPara = null, string AgentCode = "", int Roommapping = 1) //Add int Roommapping=0 on 30MAY2025
        {
            AvailabilityRS_Comp availHotelList_Comp = new AvailabilityRS_Comp();
            AvailHotelList_Comp finalResponse_Comp = new AvailHotelList_Comp();
            availHotelList_Comp.HotelResult = new List<_HotelListNew_Comp>();

            try
            {
                availHotelList_Comp.SearchKey = NewSearchKey;
                int ab = 0;
                // Global dictionary to track unique roomKeys across all HotelOptions
                HashSet<string> globalRoomKeySet = new HashSet<string>();
                DataTable DtHotelInformation = new DataTable();
                System.Diagnostics.Stopwatch timerx = new System.Diagnostics.Stopwatch();
                timerx.Start();

                string mongoConnectionString = _mongoSettings.MongoDB_ConnectionStr_RoomMapping;// ConfigurationManager.AppSettings["MongoDB_ConnectionStr_RoomMapping"];
                string databaseName = _mongoSettings.MongoDB_DatabaseName_RoomMapping;// ConfigurationManager.AppSettings["MongoDB_DatabaseName_RoomMapping"];
                string collectionName = "";
                if (Roommapping == 1) //Add ON 30MAY2025
                {
                    collectionName = _mongoSettings.MongoDB_CollectionName_RoomMapping;// ConfigurationManager.AppSettings["MongoDB_CollectionName_RoomMapping"];
                }
                if (Roommapping == 2) //Add ON 30MAY2025
                {
                    collectionName = _mongoSettings.GimmonoxMongoDB_CollectionName_RoomMapping;// ConfigurationManager.AppSettings["MongoDB_CollectionName_RoomMapping"];
                }


                #region NotNeed
                //// Split the string by commas and convert to a list of integers
                //var hotelIds = SearchPara.Filters.HotelIDs.Split(',').Select(int.Parse).ToList();

                //try
                //{
                //    // Initialize MongoDB client and get the collection
                //    //var client = new MongoClient(mongoConnectionString);
                //    //var database = client.GetDatabase(databaseName);
                //    // var collection = database.GetCollection<BsonDocument>(collectionName);
                //    var collection = _roomMapping.GetCollection<BsonDocument>(collectionName); //200525

                //    // Create filter using $in operator
                //    var filter = Builders<BsonDocument>.Filter.In("HotelId", hotelIds);

                //    // Fetch the matching records
                //    var results = collection.Find(filter).ToList();

                //    /// Insert results into a DataTable
                //    DtHotelInformation = ConvertToDataTable(results);

                //}
                //catch (Exception ex)
                //{
                //    DtHotelInformation = new DataTable();
                //    await _exception.Error(ex);
                //}
                #endregion NotNeed


                var hotelIds = finalResponse.AvailabilityRS.HotelResult
                .Select(h => h.HotelId)
                .ToList();

                try
                {
                    var collection = _roomMapping.GetCollection<BsonDocument>(collectionName);

                    FilterDefinition<BsonDocument> filter;

                    if (hotelIds.Count == 1)
                    {
                        // Best performance for single ID
                        filter = Builders<BsonDocument>.Filter.Eq("HotelId", hotelIds[0]);
                    }
                    else
                    {
                        // Normal IN filter for multiple HotelIds
                        filter = Builders<BsonDocument>.Filter.In("HotelId", hotelIds);
                    }

                    var projection = Builders<BsonDocument>.Projection
                        .Include("HotelId")
                        .Include("RoomId")
                        .Include("SupplierRoomName")
                        .Include("standardName")
                        .Include("supplierid")
                        .Exclude("_id");

                    var results = await collection.Find(filter)
                                                  .Project(projection)
                                                  .ToListAsync();

                    DtHotelInformation = ConvertToDataTable(results);
                }
                catch (Exception ex)
                {
                    DtHotelInformation = new DataTable();
                    await _exception.Error(ex);
                }

                if (SearchPara != null)
                {
                    timerx.Stop();
                    SearchPara.SupplierTime += ",|RoomMappingSP_UsingMongo" + "|Time :" + timerx.Elapsed.ToString();
                }

                if (SearchPara != null)
                {
                    timerx.Stop();
                    SearchPara.SupplierTime += ",|RoomMappingSP" + "|Time :" + timerx.Elapsed.ToString();
                }

                for (int giataSeq = 0; giataSeq < finalResponse.AvailabilityRS.HotelResult.Count; giataSeq++)
                {
                    _HotelListNew_Comp finallist = new _HotelListNew_Comp
                    {
                        HotelId = finalResponse.AvailabilityRS.HotelResult[giataSeq].HotelId,
                        StartPrice = finalResponse.AvailabilityRS.HotelResult[giataSeq].StartPrice
                    };

                    // Dictionary to store unique options based on room configurations for the current hotel
                    Dictionary<string, Options_Comp> uniqueOptionsMap = new Dictionary<string, Options_Comp>();
                    string gitaid = finalResponse.AvailabilityRS.HotelResult[giataSeq].HotelCode;


                    for (int hotelSeq = 0; hotelSeq < finalResponse.AvailabilityRS.HotelResult[giataSeq].HotelOption.Count; hotelSeq++)
                    {
                        _HotelList hotel = finalResponse.AvailabilityRS.HotelResult[giataSeq].HotelOption[hotelSeq].HotelOptionId.DecryptHotelToken();

                        int sequence = 0; // Initialize a sequence counter // ADD on 29-JAN-2025
                        if (finalResponse.AvailabilityRS.HotelResult[giataSeq].HotelOption[hotelSeq].HotelRooms != null) // Add on 10 June 2025
                        {
                            foreach (var roomGroup in finalResponse.AvailabilityRS.HotelResult[giataSeq].HotelOption[hotelSeq].HotelRooms) // ADD on 29-JAN-2025
                            {
                                //foreach (var room in roomGroup)
                                //{


                                //    room.Sequence = sequence++; // Assign a unique sequence number
                                //}
                                int seq = sequence;
                                for (int ir = 0; ir < roomGroup.Count(); ir++) // Use Count(), Count, or Length based on type
                                {
                                    //sequence = roomGroup[ir].Sequence; // Access the room using index
                                    if (ir == 0)
                                    {
                                        seq = sequence;
                                        roomGroup[ir].Sequence = sequence++;
                                    }
                                    else
                                    {
                                        roomGroup[ir].Sequence = seq;
                                    }

                                }


                            }

                            //// Sort rooms in this HotelOption by price
                            //var sortedRoomGroups = finalResponse.AvailabilityRS.HotelResult[giataSeq].HotelOption[hotelSeq].HotelRooms
                            //    .SelectMany(roomGroup => roomGroup) // Flatten the groups for sorting
                            //    .OrderBy(room => room.Price)
                            //    .GroupBy(room => room.RoomNo) // Regroup by RoomNo after sorting
                            //    .ToList(); //cmt on 18/July/2024

                            var sortedRoomGroups = finalResponse.AvailabilityRS.HotelResult[giataSeq].HotelOption[hotelSeq].HotelRooms
                                       .Where(roomGroup =>
                                       {
                                           var first = roomGroup.First();
                                           bool isRefundable = first.CancellationPolicy.Any(p => Convert.ToDecimal(p.CancellationPrice) == 0);

                                           return roomGroup.All(r =>
                                               r.Sequence == first.Sequence &&
                                               r.RoomTypeName == first.RoomTypeName &&
                                               r.MealName == first.MealName &&
                                               r.CancellationPolicy.Any(p => Convert.ToDecimal(p.CancellationPrice) == 0) == isRefundable
                                           );
                                       })
                                       .SelectMany(group => group) // flatten the kept groups
                                       .OrderBy(room => room.Price)
                                       .GroupBy(room => room.RoomNo)
                                       .ToList();   // Add on 18/July/2025


                            // Dictionary to hold the lowest-priced unique rooms in the current HotelOption
                            var uniqueRoomMap = new Dictionary<string, _FinalRoomList_Comp>();
                            int totalroom = 0;
                            foreach (var roomGroup in sortedRoomGroups)
                            {
                                var roomList = roomGroup.ToList(); // Convert roomGroup to a list to access indices
                                for (int roomIndex = 0; roomIndex < roomList.Count; roomIndex++)
                                {
                                    var currentRoom = roomList[roomIndex];

                                    int Roomseq = roomList[roomIndex].Sequence;
                                    string S_RoomNo = null; // ADD ON 29-JAN-2025
                                                            // Process RoomTypeName and Refundability
                                    string processedRoomTypeName = getMappedRoom_rlm(currentRoom.RoomTypeName.Split('|')[0].Trim(), DtHotelInformation, Convert.ToInt32(hotel.OwnsystemHotelId), ref S_RoomNo); // ADD ON 29-JAN-2025
                                    string refundability = "non-refundable";
                                    List<_FinalPolicy> cancellationPolicies = new List<_FinalPolicy>();

                                    if (withcancellation && currentRoom.CancellationPolicy != null)
                                    {
                                        foreach (var policy in currentRoom.CancellationPolicy)
                                        {
                                            var cancellationPolicy = new _FinalPolicy
                                            {
                                                FromDate = Convert.ToDateTime(policy.FromDate).ToString("MM-dd-yyyy"),
                                                ToDate = Convert.ToDateTime(policy.ToDate).ToString("MM-dd-yyyy"),
                                                CancellationPrice = Convert.ToDecimal(policy.CancellationPrice)
                                            };
                                            cancellationPolicies.Add(cancellationPolicy);

                                            if (DateTime.TryParse(policy.FromDate, out DateTime fromDate) &&
                                                fromDate == DateTime.Today &&
                                                policy.CancellationPrice == 0.0m)
                                            {
                                                refundability = "refundable";
                                            }
                                        }
                                    }
                                    string roomKey = "";
                                    if (SearchPara.Rooms.Count == 1)
                                    {
                                        if (_supplierRes.SwapMealName(AgentCode.ToLower()))//Added on 17-DEC-2024
                                                                                           // Generate roomKey based on RoomNo, RoomTypeName, MappedMealName, and Refundability

                                            roomKey = $"{currentRoom.RoomNo}_{processedRoomTypeName.Split('|')[0]}_{currentRoom.MealName}_{refundability}";
                                        else
                                            roomKey = $"{currentRoom.RoomNo}_{processedRoomTypeName.Split('|')[0]}_{currentRoom.MappedMealName}_{refundability}";
                                    }
                                    else
                                    {
                                        if (_supplierRes.SwapMealName(AgentCode.ToLower()))//Added on 17-DEC-2024
                                                                                           // Generate roomKey based on RoomNo, RoomTypeName, MappedMealName, and Refundability

                                            roomKey = $"{currentRoom.RoomNo}_{processedRoomTypeName.Split('|')[0]}_{currentRoom.MealName}_{refundability}_{finalResponse.AvailabilityRS.HotelResult[giataSeq].HotelOption[hotelSeq].IsCombineRoom}";
                                        else
                                            roomKey = $"{currentRoom.RoomNo}_{processedRoomTypeName.Split('|')[0]}_{currentRoom.MappedMealName}_{refundability}_{finalResponse.AvailabilityRS.HotelResult[giataSeq].HotelOption[hotelSeq].IsCombineRoom}";
                                    }

                                    //creating room token

                                    string RoomToken_New = new CacheRoomToken // ADD ON 29-JAN-2025
                                    {
                                        giataid_Seq = giataSeq,
                                        hotelid_Seq = hotelSeq,
                                        Room_seqno = Roomseq,
                                        roomno = Convert.ToInt32(currentRoom.RoomNo) - 1 //totalroom //ADD ON 11-FEB-2025
                                    }.EncryptCacheRoomToken();

                                    // Check global map to ensure no duplicate roomKey across all HotelOptionId
                                    if (globalRoomKeySet.Contains(roomKey))
                                    {
                                        continue; // Skip adding this room as it is a duplicate
                                    }

                                    // Initialize RoomList for this unique room
                                    _FinalRoomList_Comp RoomList = new _FinalRoomList_Comp
                                    {
                                        RoomToken = RoomToken_New,
                                        RoomNo = currentRoom.RoomNo,
                                        RoomTypeName = processedRoomTypeName,
                                        MealName = currentRoom.MealName,
                                        Price = currentRoom.Price,
                                        BookingStatus = currentRoom.BookingStatus,
                                        MappedMealName = currentRoom.MappedMealName,
                                        CancellationPolicy = cancellationPolicies,
                                        SEQNO = Roomseq  // ADD On 4-FEB-2025
                                    };

                                    // Add room to uniqueRoomMap if it’s the lowest-priced or if not already added
                                    if (!uniqueRoomMap.ContainsKey(roomKey) || currentRoom.Price < uniqueRoomMap[roomKey].Price)
                                    {
                                        uniqueRoomMap[roomKey] = RoomList;
                                        globalRoomKeySet.Add(roomKey); // Track globally to avoid duplicate roomKey
                                    }
                                }

                                totalroom++;
                            }

                            // Create the HotelOption with unique rooms only if uniqueRoomMap has values
                            if (uniqueRoomMap.Values.Any())
                            {
                                if (finalResponse.AvailabilityRS.HotelResult[giataSeq].HotelOption[hotelSeq].IsCombineRoom == false && SearchPara.Rooms.Count > 1)
                                {
                                    Options_Comp option = new Options_Comp
                                    {
                                        HotelOptionId = $"{giataSeq}|{hotel.OwnsystemHotelId}|{hotel.SupplierId}|{hotelSeq}",
                                        // Set MinPrice to the sum of prices for the RoomTypeName with the cheapest RoomNo 1
                                        MinPrice = uniqueRoomMap.Values
                                                        .Where(r => Convert.ToInt32(r.RoomNo) == 1) // Filter RoomNo 1
                                                        .OrderBy(r => r.Price)     // Find the cheapest RoomNo 1
                                                        .Select(r => uniqueRoomMap.Values
                                                            .Where(room => room.RoomTypeName == r.RoomTypeName && room.MappedMealName == r.MappedMealName) // Match the RoomTypeName
                                                            .Sum(room => room.Price)) // Sum prices of all rooms with the same RoomTypeName
                                                        .FirstOrDefault(), // Get the sum for the RoomTypeName with the cheapest RoomNo 1
                                        IsCombineRoom = finalResponse.AvailabilityRS.HotelResult[giataSeq].HotelOption[hotelSeq].IsCombineRoom,

                                        //// Group rooms by RoomTypeName
                                        //HotelRooms = uniqueRoomMap.Values
                                        //            .GroupBy(r => (r.RoomTypeName, r.MappedMealName))
                                        //            .Select(group => group.OrderBy(r => r.RoomNo).ToList()) // Sort rooms by RoomNo in each group
                                        //            .ToList()

                                        //// Group rooms by RoomTypeName    //ADD ON 30-JAN-2025
                                        //HotelRooms = uniqueRoomMap.Values
                                        //                .GroupBy(r => (r.RoomTypeName, r.MappedMealName,
                                        //                               r.CancellationPolicy.Any(p => p.CancellationPrice == 0) ? "refundable" : "non-refundable"))
                                        //                .Select(group => group.OrderBy(r => r.RoomNo).ToList()) // Sort rooms by RoomNo in each group
                                        //                .ToList()

                                        //SEQNO

                                        HotelRooms = uniqueRoomMap.Values
                                                                .GroupBy(r => (r.SEQNO, r.MappedMealName
                                                                               , r.CancellationPolicy.Any(p => p.CancellationPrice == 0) ? "refundable" : "non-refundable")
                                                                               )
                                                                .Select(group => group.OrderBy(r => r.RoomNo).ToList()) // Sort rooms by RoomNo in each group
                                                                .ToList()  // //ADD ON 4-FEB-2025




                                    };


                                    // Unique key for HotelOption based on the sorted unique rooms
                                    string optionKey = string.Join(",", uniqueRoomMap.Values.Select(r => $"{r.RoomNo}_{r.RoomTypeName.Split('|')[0]}_{r.MappedMealName}_{(r.CancellationPolicy.Any(p => p.CancellationPrice == 0) ? "refundable" : "non-refundable")}"));

                                    // Add the option if it’s cheaper or does not already exist in uniqueOptionsMap
                                    if (!uniqueOptionsMap.ContainsKey(optionKey) || option.MinPrice < uniqueOptionsMap[optionKey].MinPrice)
                                    {
                                        uniqueOptionsMap[optionKey] = option;
                                    }
                                }
                                else
                                {
                                    if (uniqueRoomMap.Values.Any())
                                    {
                                        Options_Comp option = new Options_Comp
                                        {
                                            HotelOptionId = $"{giataSeq}|{hotel.OwnsystemHotelId}|{hotel.SupplierId}|{hotelSeq}",
                                            MinPrice = uniqueRoomMap.Values.Min(r => r.Price),
                                            IsCombineRoom = finalResponse.AvailabilityRS.HotelResult[giataSeq].HotelOption[hotelSeq].IsCombineRoom,
                                            HotelRooms = uniqueRoomMap.Values
                                                .Select(room => new List<_FinalRoomList_Comp> { room }) // Wrap each room in its own list
                                                .ToList()
                                        };

                                        // Unique key for HotelOption based on the sorted unique rooms
                                        string optionKey = string.Join(",", uniqueRoomMap.Values.Select(r => $"{r.RoomNo}_{r.RoomTypeName.Split('|')[0]}_{r.MappedMealName}_{(r.CancellationPolicy.Any(p => p.CancellationPrice == 0) ? "refundable" : "non-refundable")}"));

                                        // Add the option if it’s cheaper or does not already exist in uniqueOptionsMap
                                        if (!uniqueOptionsMap.ContainsKey(optionKey) || option.MinPrice < uniqueOptionsMap[optionKey].MinPrice)
                                        {
                                            uniqueOptionsMap[optionKey] = option;
                                        }
                                    }

                                }

                            }


                        }
                        else // Add on 10 June 2025 
                        {
                            // If HotelRooms is null, we still want to return a default HotelOption entry
                            Options_Comp option = new Options_Comp
                            {
                                HotelOptionId = $"{giataSeq}|{hotel.OwnsystemHotelId}|{hotel.SupplierId}|{hotelSeq}",
                                MinPrice = finalResponse.AvailabilityRS.HotelResult[giataSeq].StartPrice,
                                IsCombineRoom = finalResponse.AvailabilityRS.HotelResult[giataSeq].HotelOption[hotelSeq].IsCombineRoom,
                                HotelRooms = new List<List<_FinalRoomList_Comp>>() // Empty list
                            };

                            // Add default option to map (key doesn't matter much in this case)
                            string optionKey = $"{option.HotelOptionId}_default_empty";
                            if (!uniqueOptionsMap.ContainsKey(optionKey))
                            {
                                uniqueOptionsMap[optionKey] = option;
                            }
                        }


                    }

                    // Add deduplicated options to the final list for the hotel
                    finallist.HotelOption = uniqueOptionsMap.Values.ToList();
                    availHotelList_Comp.HotelResult.Add(finallist);
                }

                availHotelList_Comp.Count = finalResponse.AvailabilityRS.Count;
                availHotelList_Comp.Currency = finalResponse.AvailabilityRS.Currency;
                finalResponse_Comp.AvailabilityRS = availHotelList_Comp;
                return finalResponse_Comp;
            }
            catch (Exception ex)
            {
                await _exception.Error(ex);
            }
            return null;
        }
        public async Task<AvailHotelList_Comp> ConvertToFinalResponseList_RoomMappingv2(AvailHotelList finalResponse, string NewSearchKey, bool withcancellation = false, HotelSearchParameter SearchPara = null, string AgentCode = "", int Roommapping = 1)  // ADD on 29-JAN-2025
        {
            AvailabilityRS_Comp availHotelList_Comp = new AvailabilityRS_Comp();
            AvailHotelList_Comp finalResponse_Comp = new AvailHotelList_Comp();
            availHotelList_Comp.HotelResult = new List<_HotelListNew_Comp>();

            try
            {
                availHotelList_Comp.SearchKey = NewSearchKey;
                int ab = 0;
                // Global dictionary to track unique roomKeys across all HotelOptions
                HashSet<string> globalRoomKeySet = new HashSet<string>();
                DataTable DtHotelInformation = new DataTable();
                System.Diagnostics.Stopwatch timerx = new System.Diagnostics.Stopwatch();
                timerx.Start();

                string mongoConnectionString = _mongoSettings.MongoDB_ConnectionStr_RoomMapping;//  ConfigurationManager.AppSettings["MongoDB_ConnectionStr_RoomMapping"]; // ADD ON 29-JAN-2025
                string databaseName = _mongoSettings.MongoDB_DatabaseName_RoomMapping;// ConfigurationManager.AppSettings["MongoDB_DatabaseName_RoomMapping"]; // ADD ON 29-JAN-2025
                                                                                      //string collectionName = _mongoSettings.MongoDB_CollectionName_RoomMapping;// ConfigurationManager.AppSettings["MongoDB_CollectionName_RoomMapping"]; // ADD ON 29-JAN-2025
                string collectionName = "";
                if (Roommapping == 1) //Add ON 30MAY2025
                {
                    collectionName = _mongoSettings.MongoDB_CollectionName_RoomMapping;// ConfigurationManager.AppSettings["MongoDB_CollectionName_RoomMapping"];
                }
                if (Roommapping == 0 || Roommapping == 2) //Add ON 30MAY2025
                {
                    collectionName = _mongoSettings.GimmonoxMongoDB_CollectionName_RoomMapping;// ConfigurationManager.AppSettings["MongoDB_CollectionName_RoomMapping"];
                }

                #region NotNeed
                //// Split the string by commas and convert to a list of integers
                //var hotelIds = SearchPara.Filters.HotelIDs.Split(',').Select(int.Parse).ToList();

                //try
                //{
                //    // Initialize MongoDB client and get the collection
                //    //var client = new MongoClient(mongoConnectionString);
                //    //var database = client.GetDatabase(databaseName);
                //    // var collection = database.GetCollection<BsonDocument>(collectionName);
                //    var collection = _roomMapping.GetCollection<BsonDocument>(collectionName); //200525

                //    // Create filter using $in operator
                //    var filter = Builders<BsonDocument>.Filter.In("HotelId", hotelIds);

                //    // Fetch the matching records
                //    var results = collection.Find(filter).ToList();

                //    /// Insert results into a DataTable
                //    DtHotelInformation = ConvertToDataTable(results);

                //}
                //catch (Exception ex)
                //{
                //    DtHotelInformation = new DataTable();
                //    await _exception.Error(ex);
                //}
                #endregion NotNeed


                var hotelIds = finalResponse.AvailabilityRS.HotelResult
                .Select(h => h.HotelId)
                .ToList();

                try
                {
                    var collection = _roomMapping.GetCollection<BsonDocument>(collectionName);

                    FilterDefinition<BsonDocument> filter;

                    if (hotelIds.Count == 1)
                    {
                        // Best performance for single ID
                        filter = Builders<BsonDocument>.Filter.Eq("HotelId", hotelIds[0]);
                    }
                    else
                    {
                        // Normal IN filter for multiple HotelIds
                        filter = Builders<BsonDocument>.Filter.In("HotelId", hotelIds);
                    }

                    var projection = Builders<BsonDocument>.Projection
                        .Include("HotelId")
                        .Include("RoomId")
                        .Include("SupplierRoomName")
                        .Include("standardName")
                        .Include("supplierid")
                        .Exclude("_id");

                    var results = await collection.Find(filter)
                                                  .Project(projection)
                                                  .ToListAsync();

                    DtHotelInformation = ConvertToDataTable(results);
                }
                catch (Exception ex)
                {
                    DtHotelInformation = new DataTable();
                    await _exception.Error(ex);
                }


                if (SearchPara != null)
                {
                    timerx.Stop();
                    SearchPara.SupplierTime += ",|RoomMappingSP_UsingMongo" + "|Time :" + timerx.Elapsed.ToString();
                }

                if (SearchPara != null)
                {
                    timerx.Stop();
                    SearchPara.SupplierTime += ",|RoomMappingSP" + "|Time :" + timerx.Elapsed.ToString();
                }

                for (int giataSeq = 0; giataSeq < finalResponse.AvailabilityRS.HotelResult.Count; giataSeq++)
                {
                    _HotelListNew_Comp finallist = new _HotelListNew_Comp
                    {
                        HotelId = finalResponse.AvailabilityRS.HotelResult[giataSeq].HotelId,
                        StartPrice = finalResponse.AvailabilityRS.HotelResult[giataSeq].StartPrice
                    };

                    // Dictionary to store unique options based on room configurations for the current hotel
                    Dictionary<string, Options_Comp> uniqueOptionsMap = new Dictionary<string, Options_Comp>();
                    string gitaid = finalResponse.AvailabilityRS.HotelResult[giataSeq].HotelCode;


                    for (int hotelSeq = 0; hotelSeq < finalResponse.AvailabilityRS.HotelResult[giataSeq].HotelOption.Count; hotelSeq++)
                    {
                        _HotelList hotel = finalResponse.AvailabilityRS.HotelResult[giataSeq].HotelOption[hotelSeq].HotelOptionId.DecryptHotelToken();

                        int sequence = 0; // Initialize a sequence counter
                        if (finalResponse.AvailabilityRS.HotelResult[giataSeq].HotelOption[hotelSeq].HotelRooms != null) // Add on 10 June 2025
                        {
                            foreach (var roomGroup in finalResponse.AvailabilityRS.HotelResult[giataSeq].HotelOption[hotelSeq].HotelRooms) // ADD on 29-JAn-2025
                            {
                                //foreach (var room in roomGroup)
                                //{


                                //    room.Sequence = sequence++; // Assign a unique sequence number
                                //}
                                int seq = sequence;
                                for (int ir = 0; ir < roomGroup.Count(); ir++) // Use Count(), Count, or Length based on type
                                {
                                    //sequence = roomGroup[ir].Sequence; // Access the room using index
                                    if (ir == 0)
                                    {
                                        seq = sequence;
                                        roomGroup[ir].Sequence = sequence++;
                                    }
                                    else
                                    {
                                        roomGroup[ir].Sequence = seq;
                                    }

                                }


                            }

                            //// Sort rooms in this HotelOption by price
                            //var sortedRoomGroups = finalResponse.AvailabilityRS.HotelResult[giataSeq].HotelOption[hotelSeq].HotelRooms
                            //    .SelectMany(roomGroup => roomGroup) // Flatten the groups for sorting
                            //    .OrderBy(room => room.Price)
                            //    .GroupBy(room => room.RoomNo) // Regroup by RoomNo after sorting
                            //    .ToList();   ////Cmt on 18/July/2025

                            var sortedRoomGroups = finalResponse.AvailabilityRS.HotelResult[giataSeq].HotelOption[hotelSeq].HotelRooms
                                       .Where(roomGroup =>
                                       {
                                           var first = roomGroup.First();
                                           bool isRefundable = first.CancellationPolicy.Any(p => Convert.ToDecimal(p.CancellationPrice) == 0);

                                           return roomGroup.All(r =>
                                               r.Sequence == first.Sequence &&
                                               r.RoomTypeName == first.RoomTypeName &&
                                               r.MealName == first.MealName &&
                                               r.CancellationPolicy.Any(p => Convert.ToDecimal(p.CancellationPrice) == 0) == isRefundable
                                           );
                                       })
                                       .SelectMany(group => group) // flatten the kept groups
                                       .OrderBy(room => room.Price)
                                       .GroupBy(room => room.RoomNo)
                                       .ToList();   // Add on 18/July/2025


                            // Dictionary to hold the lowest-priced unique rooms in the current HotelOption
                            var uniqueRoomMap = new Dictionary<string, _FinalRoomList_Comp>();
                            int totalroom = 0;
                            foreach (var roomGroup in sortedRoomGroups)
                            {
                                var roomList = roomGroup.ToList(); // Convert roomGroup to a list to access indices
                                for (int roomIndex = 0; roomIndex < roomList.Count; roomIndex++)
                                {
                                    var currentRoom = roomList[roomIndex];

                                    int Roomseq = roomList[roomIndex].Sequence;  // ADD ON 29-JAN-2025

                                    string S_RoomNo = null; // ADD ON 29-JAN-2025
                                                            // Process RoomTypeName and Refundability
                                    string processedRoomTypeName = getMappedRoom_rlm(currentRoom.RoomTypeName.Split('|')[0].Trim(), DtHotelInformation, Convert.ToInt32(hotel.OwnsystemHotelId), ref S_RoomNo); // ADD ON 29-JAN-2025
                                    string refundability = "non-refundable";
                                    List<_FinalPolicy> cancellationPolicies = new List<_FinalPolicy>();

                                    if (withcancellation && currentRoom.CancellationPolicy != null)
                                    {
                                        foreach (var policy in currentRoom.CancellationPolicy)
                                        {
                                            var cancellationPolicy = new _FinalPolicy
                                            {
                                                FromDate = Convert.ToDateTime(policy.FromDate).ToString("MM-dd-yyyy"),
                                                ToDate = Convert.ToDateTime(policy.ToDate).ToString("MM-dd-yyyy"),
                                                CancellationPrice = Convert.ToDecimal(policy.CancellationPrice)
                                            };
                                            cancellationPolicies.Add(cancellationPolicy);

                                            if (DateTime.TryParse(policy.FromDate, out DateTime fromDate) &&
                                                fromDate == DateTime.Today &&
                                                policy.CancellationPrice == 0.0m)
                                            {
                                                refundability = "refundable";
                                            }
                                        }
                                    }
                                    string roomKey = "";
                                    if (SearchPara.Rooms.Count == 1)
                                    {
                                        if (_supplierRes.SwapMealName(AgentCode.ToLower()))//Added on 17-DEC-2024
                                                                                           // Generate roomKey based on RoomNo, RoomTypeName, MappedMealName, and Refundability

                                            roomKey = $"{currentRoom.RoomNo}_{processedRoomTypeName.Split('|')[0]}_{currentRoom.MealName}_{refundability}";
                                        else
                                            roomKey = $"{currentRoom.RoomNo}_{processedRoomTypeName.Split('|')[0]}_{currentRoom.MappedMealName}_{refundability}";
                                    }
                                    else
                                    {
                                        if (_supplierRes.SwapMealName(AgentCode.ToLower()))//Added on 17-DEC-2024
                                                                                           // Generate roomKey based on RoomNo, RoomTypeName, MappedMealName, and Refundability

                                            roomKey = $"{currentRoom.RoomNo}_{processedRoomTypeName.Split('|')[0]}_{currentRoom.MealName}_{refundability}_{finalResponse.AvailabilityRS.HotelResult[giataSeq].HotelOption[hotelSeq].IsCombineRoom}";
                                        else
                                            roomKey = $"{currentRoom.RoomNo}_{processedRoomTypeName.Split('|')[0]}_{currentRoom.MappedMealName}_{refundability}_{finalResponse.AvailabilityRS.HotelResult[giataSeq].HotelOption[hotelSeq].IsCombineRoom}";
                                    }

                                    //creating room token

                                    string RoomToken_New = new CacheRoomToken // ADD ON 29-JAN-2025
                                    {
                                        giataid_Seq = giataSeq,
                                        hotelid_Seq = hotelSeq,
                                        Room_seqno = Roomseq,
                                        roomno = Convert.ToInt32(currentRoom.RoomNo) - 1 //totalroom //ADD ON 11-FEB-2025
                                    }.EncryptCacheRoomToken();

                                    // Check global map to ensure no duplicate roomKey across all HotelOptionId
                                    if (globalRoomKeySet.Contains(roomKey))
                                    {
                                        continue; // Skip adding this room as it is a duplicate
                                    }

                                    // Initialize RoomList for this unique room
                                    _FinalRoomList_Comp RoomList = new _FinalRoomList_Comp
                                    {
                                        RoomToken = RoomToken_New,
                                        RoomNo = currentRoom.RoomNo,
                                        RoomTypeName = processedRoomTypeName,
                                        RoomId = S_RoomNo, // ADD On 4-FEB-2025
                                        MealName = currentRoom.MealName,
                                        Price = currentRoom.Price,
                                        BookingStatus = currentRoom.BookingStatus,
                                        MappedMealName = currentRoom.MappedMealName,
                                        CancellationPolicy = cancellationPolicies,
                                        SEQNO = Roomseq  // ADD On 4-FEB-2025
                                    };

                                    // Add room to uniqueRoomMap if it’s the lowest-priced or if not already added
                                    if (!uniqueRoomMap.ContainsKey(roomKey) || currentRoom.Price < uniqueRoomMap[roomKey].Price)
                                    {
                                        uniqueRoomMap[roomKey] = RoomList;
                                        globalRoomKeySet.Add(roomKey); // Track globally to avoid duplicate roomKey
                                    }
                                }

                                totalroom++;
                            }

                            // Create the HotelOption with unique rooms only if uniqueRoomMap has values
                            if (uniqueRoomMap.Values.Any())
                            {
                                if (finalResponse.AvailabilityRS.HotelResult[giataSeq].HotelOption[hotelSeq].IsCombineRoom == false && SearchPara.Rooms.Count > 1)
                                {
                                    Options_Comp option = new Options_Comp
                                    {
                                        HotelOptionId = $"{giataSeq}|{hotel.OwnsystemHotelId}|{hotel.SupplierId}|{hotelSeq}",
                                        // Set MinPrice to the sum of prices for the RoomTypeName with the cheapest RoomNo 1
                                        MinPrice = uniqueRoomMap.Values
                                                        .Where(r => Convert.ToInt32(r.RoomNo) == 1) // Filter RoomNo 1
                                                        .OrderBy(r => r.Price)     // Find the cheapest RoomNo 1
                                                        .Select(r => uniqueRoomMap.Values
                                                            .Where(room => room.RoomTypeName == r.RoomTypeName && room.MappedMealName == r.MappedMealName) // Match the RoomTypeName
                                                            .Sum(room => room.Price)) // Sum prices of all rooms with the same RoomTypeName
                                                        .FirstOrDefault(), // Get the sum for the RoomTypeName with the cheapest RoomNo 1
                                        IsCombineRoom = finalResponse.AvailabilityRS.HotelResult[giataSeq].HotelOption[hotelSeq].IsCombineRoom,

                                        //// Group rooms by RoomTypeName
                                        //HotelRooms = uniqueRoomMap.Values
                                        //            .GroupBy(r => (r.RoomTypeName, r.MappedMealName))
                                        //            .Select(group => group.OrderBy(r => r.RoomNo).ToList()) // Sort rooms by RoomNo in each group
                                        //            .ToList()

                                        //// Group rooms by RoomTypeName    //ADD ON 30-JAN-2025
                                        //HotelRooms = uniqueRoomMap.Values
                                        //         .GroupBy(r => (r.RoomTypeName, r.MappedMealName,
                                        //                        r.CancellationPolicy.Any(p => p.CancellationPrice == 0) ? "refundable" : "non-refundable"))
                                        //         .Select(group => group.OrderBy(r => r.RoomNo).ToList()) // Sort rooms by RoomNo in each group
                                        //         .ToList()

                                        HotelRooms = uniqueRoomMap.Values
                                         .GroupBy(r => (r.SEQNO, r.MappedMealName, //ADD ON 4-FEB-2025
                                                        r.CancellationPolicy.Any(p => p.CancellationPrice == 0) ? "refundable" : "non-refundable"))
                                         .Select(group => group.OrderBy(r => r.RoomNo).ToList()) // Sort rooms by RoomNo in each group
                                         .ToList()
                                    };


                                    // Unique key for HotelOption based on the sorted unique rooms
                                    string optionKey = string.Join(",", uniqueRoomMap.Values.Select(r => $"{r.RoomNo}_{r.RoomTypeName.Split('|')[0]}_{r.MappedMealName}_{(r.CancellationPolicy.Any(p => p.CancellationPrice == 0) ? "refundable" : "non-refundable")}"));

                                    // Add the option if it’s cheaper or does not already exist in uniqueOptionsMap
                                    if (!uniqueOptionsMap.ContainsKey(optionKey) || option.MinPrice < uniqueOptionsMap[optionKey].MinPrice)
                                    {
                                        uniqueOptionsMap[optionKey] = option;
                                    }
                                }
                                else
                                {
                                    if (uniqueRoomMap.Values.Any())
                                    {
                                        Options_Comp option = new Options_Comp
                                        {
                                            HotelOptionId = $"{giataSeq}|{hotel.OwnsystemHotelId}|{hotel.SupplierId}|{hotelSeq}",
                                            MinPrice = uniqueRoomMap.Values.Min(r => r.Price),
                                            IsCombineRoom = finalResponse.AvailabilityRS.HotelResult[giataSeq].HotelOption[hotelSeq].IsCombineRoom,
                                            HotelRooms = uniqueRoomMap.Values
                                                .Select(room => new List<_FinalRoomList_Comp> { room }) // Wrap each room in its own list
                                                .ToList()
                                        };

                                        // Unique key for HotelOption based on the sorted unique rooms
                                        string optionKey = string.Join(",", uniqueRoomMap.Values.Select(r => $"{r.RoomNo}_{r.RoomTypeName.Split('|')[0]}_{r.MappedMealName}_{(r.CancellationPolicy.Any(p => p.CancellationPrice == 0) ? "refundable" : "non-refundable")}"));

                                        // Add the option if it’s cheaper or does not already exist in uniqueOptionsMap
                                        if (!uniqueOptionsMap.ContainsKey(optionKey) || option.MinPrice < uniqueOptionsMap[optionKey].MinPrice)
                                        {
                                            uniqueOptionsMap[optionKey] = option;
                                        }
                                    }

                                }

                            }
                        }
                        else // Add on 10 June 2025
                        {
                            // If HotelRooms is null, we still want to return a default HotelOption entry
                            Options_Comp option = new Options_Comp
                            {
                                HotelOptionId = $"{giataSeq}|{hotel.OwnsystemHotelId}|{hotel.SupplierId}|{hotelSeq}",
                                MinPrice = finalResponse.AvailabilityRS.HotelResult[giataSeq].StartPrice,
                                IsCombineRoom = finalResponse.AvailabilityRS.HotelResult[giataSeq].HotelOption[hotelSeq].IsCombineRoom,
                                HotelRooms = new List<List<_FinalRoomList_Comp>>() // Empty list
                            };

                            // Add default option to map (key doesn't matter much in this case)
                            string optionKey = $"{option.HotelOptionId}_default_empty";
                            if (!uniqueOptionsMap.ContainsKey(optionKey))
                            {
                                uniqueOptionsMap[optionKey] = option;
                            }
                        }
                    }

                    // Add deduplicated options to the final list for the hotel
                    finallist.HotelOption = uniqueOptionsMap.Values.ToList();
                    availHotelList_Comp.HotelResult.Add(finallist);
                }

                availHotelList_Comp.Count = finalResponse.AvailabilityRS.Count;
                availHotelList_Comp.Currency = finalResponse.AvailabilityRS.Currency;
                finalResponse_Comp.AvailabilityRS = availHotelList_Comp;
                return finalResponse_Comp;
            }
            catch (Exception ex)
            {
                await _exception.Error(ex);
            }
            return null;
        }

        public DataTable ConvertToDataTable(List<BsonDocument> documents) // Add on 29-JAN-2025
        {
            DataTable dataTable = new DataTable();

            if (documents.Count > 0)
            {
                // Add columns to the DataTable based on the keys in the first document
                foreach (var element in documents[0])
                {
                    dataTable.Columns.Add(element.Name, typeof(string)); // Assuming all values can be converted to strings
                }

                // Add rows to the DataTable
                foreach (var doc in documents)
                {
                    DataRow row = dataTable.NewRow();
                    foreach (var element in doc)
                    {
                        row[element.Name] = element.Value?.ToString() ?? DBNull.Value.ToString(); // Handle null values
                    }
                    dataTable.Rows.Add(row);
                }
            }

            return dataTable;
        }

        public async Task<AvailHotelList_Comp> ConvertToFinalResponseList(AvailHotelList finalResponse, string NewSearchKey, bool withcancellation = false, bool IsFromAPI = true)
        {
            AvailabilityRS_Comp availHotelList_Comp = new AvailabilityRS_Comp();
            AvailHotelList_Comp finalResponse_Comp = new AvailHotelList_Comp();

            availHotelList_Comp.HotelResult = new List<_HotelListNew_Comp>();
            try
            {

                availHotelList_Comp.SearchKey = NewSearchKey;



                for (int giataSeq = 0; giataSeq < finalResponse.AvailabilityRS.HotelResult.Count; giataSeq++)// each giata id
                {
                    _HotelListNew_Comp finallist = new _HotelListNew_Comp();
                    finallist.HotelId = finalResponse.AvailabilityRS.HotelResult[giataSeq].HotelId;
                    finallist.StartPrice = finalResponse.AvailabilityRS.HotelResult[giataSeq].StartPrice;

                    List<Options_Comp> finalOptionList = new List<Options_Comp>();


                    string gitaid = finalResponse.AvailabilityRS.HotelResult[giataSeq].HotelCode;
                    for (int hotelSeq = 0; hotelSeq < finalResponse.AvailabilityRS.HotelResult[giataSeq].HotelOption.Count; hotelSeq++)//each hotels
                    {
                        _HotelList hotel = finalResponse.AvailabilityRS.HotelResult[giataSeq].HotelOption[hotelSeq].HotelOptionId.DecryptHotelToken();
                        string ownhotelid = hotel.OwnsystemHotelId;
                        string supplierId = hotel.SupplierId;
                        Options_Comp finalOption = new Options_Comp();

                        List<List<_FinalRoomList_Comp>> finalRoom = new List<List<_FinalRoomList_Comp>>();

                        string SupplierName = "";//170326

                        for (int roomNoSeq = 0; roomNoSeq < finalResponse.AvailabilityRS.HotelResult[giataSeq].HotelOption[hotelSeq].HotelRooms?.Count; roomNoSeq++)//each rooms
                        {
                            List<_FinalRoomList_Comp> finalRoomList = new List<_FinalRoomList_Comp>();

                            for (int noOfRooms = 0; noOfRooms < finalResponse.AvailabilityRS.HotelResult[giataSeq].HotelOption[hotelSeq].HotelRooms[roomNoSeq].Count; noOfRooms++)
                            {
                                _FinalRoomList_Comp RoomList = new _FinalRoomList_Comp();

                                CacheRoomToken cacheRoomToken = new CacheRoomToken();
                                cacheRoomToken.giataid_Seq = giataSeq;
                                cacheRoomToken.hotelid_Seq = hotelSeq;
                                cacheRoomToken.Room_seqno = roomNoSeq;
                                cacheRoomToken.roomno = noOfRooms;

                                RoomList.RoomToken = cacheRoomToken.EncryptCacheRoomToken();

                                RoomList.MealName = finalResponse.AvailabilityRS.HotelResult[giataSeq].HotelOption[hotelSeq].HotelRooms[roomNoSeq][noOfRooms].MealName;
                                RoomList.Price = finalResponse.AvailabilityRS.HotelResult[giataSeq].HotelOption[hotelSeq].HotelRooms[roomNoSeq][noOfRooms].Price;
                                RoomList.RoomNo = finalResponse.AvailabilityRS.HotelResult[giataSeq].HotelOption[hotelSeq].HotelRooms[roomNoSeq][noOfRooms].RoomNo;
                                RoomList.RoomTypeName = finalResponse.AvailabilityRS.HotelResult[giataSeq].HotelOption[hotelSeq].HotelRooms[roomNoSeq][noOfRooms].RoomTypeName;
                                RoomList.BookingStatus = finalResponse.AvailabilityRS.HotelResult[giataSeq].HotelOption[hotelSeq].HotelRooms[roomNoSeq][noOfRooms].BookingStatus;
                                RoomList.MappedMealName = finalResponse.AvailabilityRS.HotelResult[giataSeq].HotelOption[hotelSeq].HotelRooms[roomNoSeq][noOfRooms].MappedMealName;

                                if (!IsFromAPI)
                                {
                                    RoomList.SupplierCurrency = finalResponse.AvailabilityRS.HotelResult[giataSeq].HotelOption[hotelSeq].HotelRooms[roomNoSeq][noOfRooms].SupplierCurrency;//Added on 04-FEB-26
                                    SupplierName = finalResponse.AvailabilityRS.HotelResult[giataSeq].HotelOption[hotelSeq].HotelRooms[roomNoSeq][noOfRooms].SupplierName;//170326
                                }
                                else
                                {
                                    RoomList.SupplierCurrency = null;
                                    SupplierName = null;

                                }

                                if (withcancellation)
                                {
                                    // RoomList.EssentialInformation = finalResponse.AvailabilityRS.HotelResult[giataSeq].HotelOption[hotelSeq].HotelRooms[roomNoSeq][noOfRooms].EssentialInformation;
                                    List<_FinalPolicy> lstPolicy = new List<_FinalPolicy>();

                                    for (int i = 0; i < finalResponse.AvailabilityRS.HotelResult[giataSeq].HotelOption[hotelSeq].HotelRooms[roomNoSeq][noOfRooms].CancellationPolicy.Count; i++)
                                    {
                                        _FinalPolicy obj = new _FinalPolicy();
                                        obj.FromDate = Convert.ToDateTime(finalResponse.AvailabilityRS.HotelResult[giataSeq].HotelOption[hotelSeq].HotelRooms[roomNoSeq][noOfRooms].CancellationPolicy[i].FromDate).ToString("MM-dd-yyyy");
                                        obj.ToDate = Convert.ToDateTime(finalResponse.AvailabilityRS.HotelResult[giataSeq].HotelOption[hotelSeq].HotelRooms[roomNoSeq][noOfRooms].CancellationPolicy[i].ToDate).ToString("MM-dd-yyyy");
                                        obj.CancellationPrice = Convert.ToDecimal(finalResponse.AvailabilityRS.HotelResult[giataSeq].HotelOption[hotelSeq].HotelRooms[roomNoSeq][noOfRooms].CancellationPolicy[i].CancellationPrice);
                                        //token += finalResponse.AvailabilityRS.HotelResult[giataSeq].HotelOption[hotelSeq].HotelRooms[roomNoSeq][noOfRooms].CancellationPolicy[i].EncryptPolicyToken();
                                        //obj.CancellationPrice = obj.CancellationPrice.CalculatePrice(tokenData, advancedOptions, room.SupplierCurrency);
                                        lstPolicy.Add(obj);
                                    }
                                    RoomList.CancellationPolicy = lstPolicy;
                                }
                                finalRoomList.Add(RoomList);
                            }

                            finalRoom.Add(finalRoomList);
                        }

                        finalOption.HotelRooms = finalRoom;
                        finalOption.MinPrice = finalResponse.AvailabilityRS.HotelResult[giataSeq].HotelOption[hotelSeq].MinPrice;
                        finalOption.IsCombineRoom = finalResponse.AvailabilityRS.HotelResult[giataSeq].HotelOption[hotelSeq].IsCombineRoom;

                        finalOption.HotelOptionId = giataSeq + "|" + ownhotelid + "|" + supplierId + "|" + hotelSeq;
                        finalOption.SupplierName = SupplierName;//170326

                        finalOptionList.Add(finalOption);
                    }
                    finallist.HotelOption = finalOptionList;
                    availHotelList_Comp.HotelResult.Add(finallist);
                }
                availHotelList_Comp.Count = finalResponse.AvailabilityRS.Count;
                availHotelList_Comp.Currency = finalResponse.AvailabilityRS.Currency;
                finalResponse_Comp.AvailabilityRS = availHotelList_Comp;
                return finalResponse_Comp;
            }
            catch (Exception ex)
            {
                await _exception.Error(ex);
            }
            return null;
        }

        //Add On 17SEP2025  ConvertToFinalResponseList_Dag2
        public async Task<AvailHotelList_Comp> ConvertToFinalResponseList_Dag2(AvailHotelList finalResponse, string NewSearchKey, bool withcancellation = false, HotelSearchParameter SearchPara = null)
        {
            AvailabilityRS_Comp availHotelList_Comp = new AvailabilityRS_Comp();
            AvailHotelList_Comp finalResponse_Comp = new AvailHotelList_Comp();
            List<RoomInfo> missingRoomList = new List<RoomInfo>(); //Add On 17SEP2025
            availHotelList_Comp.HotelResult = new List<_HotelListNew_Comp>();
            try
            {

                availHotelList_Comp.SearchKey = NewSearchKey;
                DataTable DtHotelInformation = new DataTable();

                string mongoConnectionString = _mongoSettings.MongoDB_ConnectionStr_RoomMapping;//  ConfigurationManager.AppSettings["MongoDB_ConnectionStr_RoomMapping"]; // ADD ON 29-JAN-2025
                string databaseName = _mongoSettings.MongoDB_DatabaseName_RoomMapping;// ConfigurationManager.AppSettings["MongoDB_DatabaseName_RoomMapping"]; // ADD ON 29-JAN-2025
                                                                                      //string collectionName = _mongoSettings.MongoDB_CollectionName_RoomMapping;// ConfigurationManager.AppSettings["MongoDB_CollectionName_RoomMapping"]; // ADD ON 29-JAN-2025
                string collectionName = "";

                collectionName = _mongoSettings.GimmonoxMongoDB_CollectionName_RoomMapping_DAG2; //Add On 17SEP2025


                #region NotNeed
                //var hotelIds = SearchPara.Filters.HotelIDs.Split(',').Select(int.Parse).ToList();

                //try
                //{
                //    // Initialize MongoDB client and get the collection
                //    //var client = new MongoClient(mongoConnectionString);
                //    //var database = client.GetDatabase(databaseName);
                //    // var collection = database.GetCollection<BsonDocument>(collectionName);
                //    var collection = _roomMapping.GetCollection<BsonDocument>(collectionName); //200525

                //    // Create filter using $in operator
                //    var filter = Builders<BsonDocument>.Filter.In("HotelId", hotelIds);

                //    // Fetch the matching records
                //    var results = collection.Find(filter).ToList();

                //    /// Insert results into a DataTable
                //    DtHotelInformation = ConvertToDataTable(results);

                //}
                //catch (Exception ex)
                //{
                //    DtHotelInformation = new DataTable();
                //    await _exception.Error(ex);
                //}
                #endregion Notneed

                var hotelIds = finalResponse.AvailabilityRS.HotelResult
               .Select(h => h.HotelId)
               .ToList();

                try
                {
                    var collection = _roomMapping.GetCollection<BsonDocument>(collectionName);

                    FilterDefinition<BsonDocument> filter;

                    if (hotelIds.Count == 1)
                    {
                        // Best performance for single ID
                        filter = Builders<BsonDocument>.Filter.Eq("HotelId", hotelIds[0]);
                    }
                    else
                    {
                        // Normal IN filter for multiple HotelIds
                        filter = Builders<BsonDocument>.Filter.In("HotelId", hotelIds);
                    }

                    var projection = Builders<BsonDocument>.Projection
                        .Include("HotelId")
                        .Include("RoomId")
                        .Include("SupplierRoomName")
                        .Include("standardName")
                        .Include("supplierid")
                        .Exclude("_id");

                    var results = await collection.Find(filter)
                                                  .Project(projection)
                                                  .ToListAsync();

                    DtHotelInformation = ConvertToDataTable(results);
                }
                catch (Exception ex)
                {
                    DtHotelInformation = new DataTable();
                    await _exception.Error(ex);
                }


                for (int giataSeq = 0; giataSeq < finalResponse.AvailabilityRS.HotelResult.Count; giataSeq++)// each giata id
                {
                    _HotelListNew_Comp finallist = new _HotelListNew_Comp();
                    finallist.HotelId = finalResponse.AvailabilityRS.HotelResult[giataSeq].HotelId;
                    finallist.StartPrice = finalResponse.AvailabilityRS.HotelResult[giataSeq].StartPrice;

                    List<Options_Comp> finalOptionList = new List<Options_Comp>();


                    string gitaid = finalResponse.AvailabilityRS.HotelResult[giataSeq].HotelCode;
                    for (int hotelSeq = 0; hotelSeq < finalResponse.AvailabilityRS.HotelResult[giataSeq].HotelOption.Count; hotelSeq++)//each hotels
                    {
                        _HotelList hotel = finalResponse.AvailabilityRS.HotelResult[giataSeq].HotelOption[hotelSeq].HotelOptionId.DecryptHotelToken();
                        string ownhotelid = hotel.OwnsystemHotelId;
                        string supplierId = hotel.SupplierId;
                        Options_Comp finalOption = new Options_Comp();

                        List<List<_FinalRoomList_Comp>> finalRoom = new List<List<_FinalRoomList_Comp>>();

                        for (int roomNoSeq = 0; roomNoSeq < finalResponse.AvailabilityRS.HotelResult[giataSeq].HotelOption[hotelSeq].HotelRooms?.Count; roomNoSeq++)//each rooms
                        {
                            List<_FinalRoomList_Comp> finalRoomList = new List<_FinalRoomList_Comp>();

                            for (int noOfRooms = 0; noOfRooms < finalResponse.AvailabilityRS.HotelResult[giataSeq].HotelOption[hotelSeq].HotelRooms[roomNoSeq].Count; noOfRooms++)
                            {
                                string S_RoomNo = null;
                                string processedRoomTypeName = getMappedRoom_rlm(finalResponse.AvailabilityRS.HotelResult[giataSeq].HotelOption[hotelSeq].HotelRooms[roomNoSeq][noOfRooms].RoomTypeName.Split('|')[0].Trim(), DtHotelInformation, Convert.ToInt32(hotel.OwnsystemHotelId), ref S_RoomNo); //Add On 17SEP2025

                                if (string.IsNullOrEmpty(S_RoomNo)) //Add On 17SEP2025
                                {
                                    missingRoomList.Add(new RoomInfo
                                    {
                                        ProcessedRoomTypeName = processedRoomTypeName,
                                        OwnsystemHotelId = Convert.ToInt32(hotel.OwnsystemHotelId),
                                        Time = DateTime.Now
                                    });
                                }

                                _FinalRoomList_Comp RoomList = new _FinalRoomList_Comp();

                                CacheRoomToken cacheRoomToken = new CacheRoomToken();
                                cacheRoomToken.giataid_Seq = giataSeq;
                                cacheRoomToken.hotelid_Seq = hotelSeq;
                                cacheRoomToken.Room_seqno = roomNoSeq;
                                cacheRoomToken.roomno = noOfRooms;

                                RoomList.RoomToken = cacheRoomToken.EncryptCacheRoomToken();
                                RoomList.RoomId = S_RoomNo;//Added on 18-11-25 As per karnav //Add Gymonix RoomId In the response
                                RoomList.MealName = finalResponse.AvailabilityRS.HotelResult[giataSeq].HotelOption[hotelSeq].HotelRooms[roomNoSeq][noOfRooms].MealName;
                                RoomList.Price = finalResponse.AvailabilityRS.HotelResult[giataSeq].HotelOption[hotelSeq].HotelRooms[roomNoSeq][noOfRooms].Price;
                                RoomList.RoomNo = finalResponse.AvailabilityRS.HotelResult[giataSeq].HotelOption[hotelSeq].HotelRooms[roomNoSeq][noOfRooms].RoomNo;
                                RoomList.RoomTypeName = processedRoomTypeName; //Add On 17SEP2025 //finalResponse.AvailabilityRS.HotelResult[giataSeq].HotelOption[hotelSeq].HotelRooms[roomNoSeq][noOfRooms].RoomTypeName;
                                RoomList.BookingStatus = finalResponse.AvailabilityRS.HotelResult[giataSeq].HotelOption[hotelSeq].HotelRooms[roomNoSeq][noOfRooms].BookingStatus;
                                RoomList.MappedMealName = finalResponse.AvailabilityRS.HotelResult[giataSeq].HotelOption[hotelSeq].HotelRooms[roomNoSeq][noOfRooms].MappedMealName;
                                if (withcancellation)
                                {
                                    // RoomList.EssentialInformation = finalResponse.AvailabilityRS.HotelResult[giataSeq].HotelOption[hotelSeq].HotelRooms[roomNoSeq][noOfRooms].EssentialInformation;
                                    List<_FinalPolicy> lstPolicy = new List<_FinalPolicy>();

                                    for (int i = 0; i < finalResponse.AvailabilityRS.HotelResult[giataSeq].HotelOption[hotelSeq].HotelRooms[roomNoSeq][noOfRooms].CancellationPolicy.Count; i++)
                                    {
                                        _FinalPolicy obj = new _FinalPolicy();
                                        obj.FromDate = Convert.ToDateTime(finalResponse.AvailabilityRS.HotelResult[giataSeq].HotelOption[hotelSeq].HotelRooms[roomNoSeq][noOfRooms].CancellationPolicy[i].FromDate).ToString("MM-dd-yyyy");
                                        obj.ToDate = Convert.ToDateTime(finalResponse.AvailabilityRS.HotelResult[giataSeq].HotelOption[hotelSeq].HotelRooms[roomNoSeq][noOfRooms].CancellationPolicy[i].ToDate).ToString("MM-dd-yyyy");
                                        obj.CancellationPrice = Convert.ToDecimal(finalResponse.AvailabilityRS.HotelResult[giataSeq].HotelOption[hotelSeq].HotelRooms[roomNoSeq][noOfRooms].CancellationPolicy[i].CancellationPrice);
                                        //token += finalResponse.AvailabilityRS.HotelResult[giataSeq].HotelOption[hotelSeq].HotelRooms[roomNoSeq][noOfRooms].CancellationPolicy[i].EncryptPolicyToken();
                                        //obj.CancellationPrice = obj.CancellationPrice.CalculatePrice(tokenData, advancedOptions, room.SupplierCurrency);
                                        lstPolicy.Add(obj);
                                    }
                                    RoomList.CancellationPolicy = lstPolicy;
                                }
                                finalRoomList.Add(RoomList);
                            }

                            finalRoom.Add(finalRoomList);
                        }

                        finalOption.HotelRooms = finalRoom;
                        finalOption.MinPrice = finalResponse.AvailabilityRS.HotelResult[giataSeq].HotelOption[hotelSeq].MinPrice;
                        finalOption.IsCombineRoom = finalResponse.AvailabilityRS.HotelResult[giataSeq].HotelOption[hotelSeq].IsCombineRoom;

                        finalOption.HotelOptionId = giataSeq + "|" + ownhotelid + "|" + supplierId + "|" + hotelSeq;
                        finalOptionList.Add(finalOption);
                    }
                    finallist.HotelOption = finalOptionList;
                    availHotelList_Comp.HotelResult.Add(finallist);
                }
                availHotelList_Comp.Count = finalResponse.AvailabilityRS.Count;
                availHotelList_Comp.Currency = finalResponse.AvailabilityRS.Currency;
                finalResponse_Comp.AvailabilityRS = availHotelList_Comp;

                if (missingRoomList.Any()) //Add On 17SEP2025
                {
                    try
                    {
                        var UnMappedmongoConnectionString = _mongoSettings.MongoDB_ConnectionStr_UmMappedRoomMapping;//  ConfigurationManager.AppSettings["MongoDB_ConnectionStr_RoomMapping"]; // ADD ON 29-JAN-2025
                        var UnMappeddatabaseName = _mongoSettings.MongoDB_DatabaseName_UmMappedRoomMapping;// ConfigurationManager.AppSettings["MongoDB_DatabaseName_RoomMapping"]; // ADD ON 29-JAN-2025
                        var UnMappedcollectionName = _mongoSettings.MongoDB_CollectionName_UmMappedRoomMapping;// ConfigurationManager.AppSettings["MongoDB_CollectionName_RoomMapping"];

                        var collection = _RowroomMapping.GetCollection<RoomInfo>(UnMappedcollectionName);

                        _ = InsertRoomsBulkAsync(collection, missingRoomList);
                    }
                    catch (Exception ex)
                    {
                        await _exception.Error(ex);
                    }

                }

                return finalResponse_Comp;
            }
            catch (Exception ex)
            {
                await _exception.Error(ex);
            }
            return null;
        }

        public async Task InsertRoomsBulkAsync(IMongoCollection<RoomInfo> collection, List<RoomInfo> list) //Add On 17SEP2025
        {
            if (list == null || list.Count == 0) return;

            // Deduplicate by OwnsystemHotelId + ProcessedRoomTypeName
            var distinctList = list
                .GroupBy(r => new { r.OwnsystemHotelId, r.ProcessedRoomTypeName })
                .Select(g => g.First())
                .ToList();

            var models = new List<WriteModel<RoomInfo>>();

            foreach (var room in distinctList)
            {
                var filter = Builders<RoomInfo>.Filter.And(
                    Builders<RoomInfo>.Filter.Eq(r => r.OwnsystemHotelId, room.OwnsystemHotelId),
                    Builders<RoomInfo>.Filter.Eq(r => r.ProcessedRoomTypeName, room.ProcessedRoomTypeName)
                );

                var update = Builders<RoomInfo>.Update
                    .SetOnInsert(r => r.OwnsystemHotelId, room.OwnsystemHotelId)
                    .SetOnInsert(r => r.ProcessedRoomTypeName, room.ProcessedRoomTypeName)
                    .SetOnInsert(r => r.Time, DateTime.UtcNow);

                models.Add(new UpdateOneModel<RoomInfo>(filter, update) { IsUpsert = true });
            }

            var bulkOptions = new BulkWriteOptions { IsOrdered = false };
            await collection.BulkWriteAsync(models, bulkOptions);
        }

        public async Task<int> InsertHotelSearchData(SaveParameter data, int count, bool from, string SupplierTime, int agentid, string SearchKey, string IP)
        {
            try
            {
                if (false)
                {
                    var log = new RequestTrackingLog
                    {
                        SearchData = JsonSerializer.Serialize(data),
                        IP = IP,
                        Agentid = agentid,
                        TotalRecord = count,
                        CityID = data.CityID,
                        from = from,
                        SupplierTime = Convert.ToString(SupplierTime),
                        SearchKey = SearchKey
                    };

                    await _stores.SaveRequestTrackingLogAsync(log, "RequestTrackingLog");
                    return 1;
                }
                else
                {


                    SqlParameter[] para = new SqlParameter[]{
                    new SqlParameter("@SearchData", JsonSerializer.Serialize(data)),
                    new SqlParameter("@Ip", IP),
                    new SqlParameter("@Agentid",agentid),
                    new SqlParameter("@TotalRecord",count),
                    new SqlParameter("@CityID",data.CityID),
                    new SqlParameter("@from",from),
                    new SqlParameter("@SupplierTime",Convert.ToString(SupplierTime)),
                    new SqlParameter("@SearchKey",SearchKey),



                    };
                    //db.ExecuteNonQuerywithconnection("InsertHotelSearchData", para, Convert.ToString(ConfigurationManager.ConnectionStrings["ConnectionStringforlogtrack"].ConnectionString));
                    int id = Convert.ToInt32(await _dlConnection.GetExecuteScalarSP_WithConStr_1SecTimeout("InsertHotelSearchData_New", para));
                    return id;
                }
            }
            catch (Exception e)
            {
                _ = _exception.Error(e);
                return -1;

            }
        }

        public async Task<int> InsertRedisHotelSearchData(string error, string remark, int AgentId, int DB, int timeoutMs)
        {
            try
            {
                SqlParameter[] para = new SqlParameter[]{
new SqlParameter("@Error", error),
new SqlParameter("@Remark", remark),
new SqlParameter("@AgentId", AgentId),
new SqlParameter("@DB", DB),
new SqlParameter("@timeoutMs", timeoutMs)
};
                //db.ExecuteNonQuerywithconnection("InsertHotelSearchData", para, Convert.ToString(ConfigurationManager.ConnectionStrings["ConnectionStringforlogtrack"].ConnectionString));
                int id = Convert.ToInt32(await _dlConnection.GetExecuteScalarSP_WithConStr_1SecTimeout_Log("InsertRedisHotelSearchData", para));
                return id;
            }
            catch (Exception e)
            {
                _ = _exception.Error(e);
                return -1;

            }
        }


        #region Booking Email
        public async Task GetBookingMailDetail(string ReferenceNo, string ToEmail, string FromEmail, string EmpToEmailId, string EmpFromEmailId, int isConformationMail, int BookingId, int AgentId, int CompanyId, string HostName = "")
        {
            try
            {
                DataSet ds = new DataSet();
                List<Email_Details> emailList = new List<Email_Details>();
                int emailsendstatus = 0;
                int IstodayBooking = 0;
                ds = await GetVoucher(125);
                DataSet dsSupplierTemplate = await GetVoucher(126);
                string BranchEmployeeEmailId = string.Empty;
                string SubagentEmailId = string.Empty;
                string SubUserEmailId = string.Empty;
                string AgentEmailId = string.Empty;
                string AgentCurrency = string.Empty;
                if (ds.Tables[0].Rows.Count > 0)
                {
                    int countryId = 0;
                    string StringHtml = ds.Tables[0].Rows[0]["NotificationBody"].ToString().Replace("&lt;", "<").Replace("&gt;", ">").Replace("&nbsp;", "").Replace("<p>", "").Replace("</p>", "");
                    string Subject = ds.Tables[0].Rows[0]["Subject"].ToString();
                    string[] alltemplatedata = StringHtml.Split(new String[] { "%%%" }, StringSplitOptions.None);
                    string commonTemplate = alltemplatedata[0].ToString();
                    //string HotelTemplate = alltemplatedata[1].ToString();
                    //string[] HotelSubTemplatelst = HotelTemplate.Split(new String[] { "$$$" }, StringSplitOptions.None);
                    string HotelTemplate1 = alltemplatedata[1].ToString();
                    string HotelTemplate2 = alltemplatedata[2].ToString();
                    string TransferTemplate = alltemplatedata[3].ToString();
                    string TourTemplate = alltemplatedata[4].ToString();
                    string MealTemplate = alltemplatedata[5].ToString();
                    string FooterTemplate = alltemplatedata[6].ToString();

                    string SupplierStringHtml = dsSupplierTemplate.Tables[0].Rows[0]["NotificationBody"].ToString().Replace("&lt;", "<").Replace("&gt;", ">").Replace("&nbsp;", "").Replace("<p>", "").Replace("</p>", "");
                    string SuppplierSubject = dsSupplierTemplate.Tables[0].Rows[0]["Subject"].ToString();
                    string[] Supplieralltemplatedata = SupplierStringHtml.Split(new String[] { "%%%" }, StringSplitOptions.None);
                    string SuppliercommonTemplate = Supplieralltemplatedata[0].ToString();
                    string SupplierDetailTemplate = Supplieralltemplatedata[1].ToString();
                    string SupplierTransferTemplate = Supplieralltemplatedata[2].ToString();
                    string SupplierTourTemplate = Supplieralltemplatedata[3].ToString();
                    string SupplierMealTemplate = Supplieralltemplatedata[4].ToString();
                    string SupplierFooterTemplate = Supplieralltemplatedata[5].ToString();

                    DataSet dsv = await GetVoucherListNew(ReferenceNo, BookingId, AgentId);
                    DataSet dsSupplierEmail = await GetSupplierEmail(ReferenceNo, BookingId);

                    string strRefNo = Convert.ToString(dsv.Tables[0].Rows[0]["ReferenceNo"]);
                    Subject = Subject.Replace("#ReferenceNo", ReferenceNo).Replace("#GuestName", Convert.ToString(dsv.Tables[0].Rows[0]["Name"]));
                    SuppplierSubject = SuppplierSubject.Replace("#RefNo", ReferenceNo).Replace("#MainGuestName", Convert.ToString(dsv.Tables[0].Rows[0]["Name"]));
                    SuppplierSubject = SuppplierSubject.Replace("#Title", "New Booking");

                    SubagentEmailId = Convert.ToString(dsv.Tables[0].Rows[0]["SubAgentEmailId"]);
                    SubUserEmailId = Convert.ToString(dsv.Tables[0].Rows[0]["SubUserEmailId"]);
                    AgentEmailId = Convert.ToString(dsv.Tables[0].Rows[0]["AgentEmailId"]);
                    AgentCurrency = Convert.ToString(dsv.Tables[0].Rows[0]["AgentCurrencyId"]);

                    //DataRow[] drCityTour = dsv.Tables[0].Select("ServiceType='City Tour' and ComboId=0");
                    //DataRow[] drHotel = dsv.Tables[14].Select("1=1");
                    //DataRow[] drCityTourCombo = dsv.Tables[0].Select("ServiceType='City Tour' and ComboId>0");
                    //DataRow[] drCombo = dsv.Tables[7].Select();
                    ////DataRow[] drvisa = dsv.Tables[0].Select("ServiceType='Visa'");
                    //DataRow[] drMealDetail = dsv.Tables[0].Select("ServiceType='Meal'");
                    //DataRow[] drTransferDetail = dsv.Tables[0].Select("ServiceType='Transfer'");

                    #region Common
                    commonTemplate = commonTemplate.Replace("#AgentName", Convert.ToString(dsv.Tables[0].Rows[0]["AgentName"]));
                    commonTemplate = commonTemplate.Replace("#ReferenceNo", strRefNo);
                    commonTemplate = commonTemplate.Replace("#AgentRefNo", Convert.ToString(dsv.Tables[0].Rows[0]["AgentInternalRef"]));
                    commonTemplate = commonTemplate.Replace("#GuestName", Convert.ToString(dsv.Tables[0].Rows[0]["Name"]));
                    commonTemplate = commonTemplate.Replace("#PaymentBy", Convert.ToString(dsv.Tables[0].Rows[0]["PaymentMethod"]));
                    #endregion
                    string FinalAgentTemplate = string.Empty;
                    string FinalAgentMailBodyTemplate = string.Empty;
                    try
                    {

                        for (int i = 0; i < dsv.Tables[15].Rows.Count; i++)
                        {
                            string CityTourHtmlMain = string.Empty;
                            string MealHtmlMain = string.Empty;
                            string TransferHtmlMain = string.Empty;
                            string TransferHtmlMainRoundTrip = string.Empty;
                            string HotelHtmlMain = string.Empty;

                            if (Convert.ToString(dsv.Tables[15].Rows[i]["ServiceType"]) == "City Tour" && Convert.ToInt32(dsv.Tables[15].Rows[i]["ComboId"]) == 0)
                            {
                                DataRow[] drCityTour = dsv.Tables[0].Select("ServiceType='City Tour' and ComboId=0 and BookingId='" + Convert.ToString(dsv.Tables[15].Rows[i]["BookingId"]) + "'");
                                #region City Tour
                                // string CityTourHtmlMain = string.Empty;
                                if (drCityTour.Length > 0)
                                {
                                    string TourBookingSubject = string.Empty;
                                    string CityTourHtml = string.Empty;
                                    foreach (DataRow drTourdata in drCityTour)
                                    {
                                        if (!string.IsNullOrEmpty(Convert.ToString(drTourdata["CountryId"])) && countryId == 0)
                                            countryId = Convert.ToInt32(drTourdata["CountryId"]);
                                        if (IstodayBooking == 0 && Convert.ToInt32(drTourdata["IsLastTimeBooking"]) == 1)
                                            IstodayBooking = 1;

                                        CityTourHtml = TourTemplate;
                                        #region "for Agent"
                                        if (Convert.ToString(drTourdata["BookingStatus"]) == "On Request")
                                        {
                                            CityTourHtml = CityTourHtml.Replace("#TourBookingStatusColor", "#f47920");
                                            CityTourHtml = CityTourHtml.Replace("#TourBookingStatus", "On Request");
                                        }
                                        else if (Convert.ToString(drTourdata["BookingStatus"]) == "Not Confirmed")
                                        {
                                            CityTourHtml = CityTourHtml.Replace("#TourBookingStatusColor", "#0f1d42");
                                            CityTourHtml = CityTourHtml.Replace("#TourBookingStatus", "Not Confirmed");
                                        }
                                        else if (Convert.ToString(drTourdata["BookingStatus"]) == "Vouchered")
                                        {
                                            CityTourHtml = CityTourHtml.Replace("#TourBookingStatusColor", "#5e3ea0");
                                            CityTourHtml = CityTourHtml.Replace("#TourBookingStatus", "Vouchered");
                                        }

                                        else
                                        {
                                            CityTourHtml = CityTourHtml.Replace("#TourBookingStatusColor", "green");
                                            CityTourHtml = CityTourHtml.Replace("#TourBookingStatus", "Confirmed");
                                        }
                                        string pickupdata = "-";
                                        if (!string.IsNullOrEmpty(Convert.ToString(drTourdata["pickup"])))
                                            pickupdata = Convert.ToString(Convert.ToString(drTourdata["pickup"]).Split('/')[0].Split(':')[1]);


                                        CityTourHtml = CityTourHtml.Replace("#TourName", Convert.ToString(drTourdata["ServiceName"]));
                                        CityTourHtml = CityTourHtml.Replace("#TourCityName", Convert.ToString(drTourdata["CityName"]));
                                        CityTourHtml = CityTourHtml.Replace("#TourType", Convert.ToString(drTourdata["RoomType"]));
                                        CityTourHtml = CityTourHtml.Replace("#TourDate", Convert.ToString(drTourdata["TravelDate"]));
                                        CityTourHtml = CityTourHtml.Replace("#TourPickup", Convert.ToString(pickupdata));
                                        CityTourHtml = CityTourHtml.Replace("#TourUnit", "1");
                                        CityTourHtml = CityTourHtml.Replace("#TourAdult", Convert.ToString(drTourdata["Adult"]));
                                        CityTourHtml = CityTourHtml.Replace("#TourChild", Convert.ToString(drTourdata["Children"]));

                                        DataRow[] tourCancellation = dsv.Tables[3].Select("BookingDetailId='" + Convert.ToString(drTourdata["BookingDetailId"]) + "'");
                                        string strTourCancellation = string.Empty;
                                        foreach (DataRow drtourCancellation in tourCancellation)
                                        {
                                            strTourCancellation += Convert.ToString(drTourdata["ServiceName"]) + " From " + Convert.ToString(string.Format("{0:d/M/yyyy HH:mm:ss tt}", drtourCancellation["FromDate"])) + " To" + Convert.ToString(string.Format("{0:d/M/yyyy HH:mm:ss tt}", drtourCancellation["ToDate"])) + " the cancellation charges are " + Convert.ToString(Math.Round(Convert.ToDouble(drtourCancellation["CancellationAmount"]), 2)) + " " + Convert.ToString(AgentCurrency) + "<br/>";
                                        }
                                        CityTourHtml = CityTourHtml.Replace("#TourCancellationPolicy", strTourCancellation);

                                        CityTourHtmlMain += CityTourHtml;
                                        #endregion
                                    }

                                }
                                #endregion

                            }
                            if (Convert.ToString(dsv.Tables[15].Rows[i]["ServiceType"]) == "Meal")
                            {
                                DataRow[] drMealDetail = dsv.Tables[0].Select("ServiceType='Meal' and BookingId='" + Convert.ToString(dsv.Tables[15].Rows[i]["BookingId"]) + "'");
                                #region Meal
                                // string MealHtmlMain = string.Empty;
                                if (drMealDetail.Length > 0)
                                {
                                    // string Subject = "Booking Confirmation East Avenue LLC : " + Convert.ToString(drMealDetail[0]["ReferenceNo"]);

                                    foreach (DataRow drMealdata in drMealDetail)
                                    {
                                        if (countryId == 0)
                                            countryId = Convert.ToInt32(drMealdata["CountryId"]);
                                        if (IstodayBooking == 0 && Convert.ToInt32(drMealdata["IsLastTimeBooking"]) == 1)
                                            IstodayBooking = 1;
                                        string MealHtml = string.Empty;

                                        MealHtml = MealTemplate;
                                        #region "for agent"
                                        if (Convert.ToString(drMealdata["BookingStatus"]) == "On Request")
                                        {
                                            MealHtml = MealHtml.Replace("#MealBookingStatusColor", "#f47920");
                                            MealHtml = MealHtml.Replace("#MealBookingStatus", "On Request");
                                        }
                                        else if (Convert.ToString(drMealdata["BookingStatus"]) == "Vouchered")
                                        {
                                            MealHtml = MealHtml.Replace("#MealBookingStatusColor", "#5e3ea0");
                                            MealHtml = MealHtml.Replace("#MealBookingStatus", "Vouchered");
                                        }
                                        else if (Convert.ToString(drMealdata["BookingStatus"]) == "Not Confirmed")
                                        {
                                            MealHtml = MealHtml.Replace("#MealBookingStatusColor", "#0f1d42");
                                            MealHtml = MealHtml.Replace("#MealBookingStatus", "Not Confirmed");
                                        }
                                        else
                                        {
                                            MealHtml = MealHtml.Replace("#BookingStatusColor", "green");
                                            MealHtml = MealHtml.Replace("#MealBookingStatus", "Confirmed");
                                        }


                                        MealHtml = MealHtml.Replace("#RestaurantName", Convert.ToString(drMealdata["ServiceName"]));
                                        MealHtml = MealHtml.Replace("#RestaurantCityName", Convert.ToString(drMealdata["CityName"]));
                                        MealHtml = MealHtml.Replace("#MealType", "" + Convert.ToString(drMealdata["RoomType"]));
                                        MealHtml = MealHtml.Replace("#MealDate", Convert.ToString(drMealdata["TravelDate"]));
                                        MealHtml = MealHtml.Replace("#MealUnit", "1");
                                        MealHtml = MealHtml.Replace("#MealAdult", Convert.ToString(drMealdata["Adult"]));
                                        MealHtml = MealHtml.Replace("#MealChild", Convert.ToString(drMealdata["Children"]));

                                        DataRow[] MealCancellation = dsv.Tables[8].Select("BookingDetailId='" + Convert.ToString(drMealdata["BookingDetailId"]) + "'");
                                        string strMealCancellation = string.Empty;
                                        foreach (DataRow drmealCancellation in MealCancellation)
                                        {
                                            strMealCancellation += Convert.ToString(drMealdata["ServiceName"]) + " From " + Convert.ToString(drmealCancellation["FromDate"]) + " To" + Convert.ToString(drmealCancellation["ToDate"]) + " the cancellation charges are " + Convert.ToString(Math.Round(Convert.ToDouble(drmealCancellation["CancellationAmount"]), 2)) + " " + Convert.ToString(AgentCurrency) + "<br/>";
                                        }
                                        MealHtml = MealHtml.Replace("#MealCancellationPolicy", strMealCancellation);
                                        MealHtmlMain += MealHtml;
                                        #endregion

                                    }
                                }
                                #endregion
                            }
                            if (Convert.ToString(dsv.Tables[15].Rows[i]["ServiceType"]) == "Transfer")
                            {
                                DataRow[] drTransferDetail = dsv.Tables[0].Select("ServiceType='Transfer' and BookingId='" + Convert.ToString(dsv.Tables[15].Rows[i]["BookingId"]) + "'");
                                #region Transfer
                                // string TransferHtmlMain = string.Empty;
                                //string TransferHtmlMainRoundTrip = string.Empty;
                                if (drTransferDetail.Length > 0)
                                {

                                    foreach (DataRow drTransferdata in drTransferDetail)
                                    {
                                        if (countryId == 0)
                                            countryId = Convert.ToInt32(drTransferdata["CountryId"]);
                                        if (IstodayBooking == 0 && Convert.ToInt32(drTransferdata["IsLastTimeBooking"]) == 1)
                                            IstodayBooking = 1;
                                        string TransferHtml = string.Empty;
                                        string TransferHtmlRoundTrip = string.Empty;

                                        TransferHtml = TransferTemplate;
                                        TransferHtmlRoundTrip = TransferTemplate;

                                        #region "for agent"
                                        if (Convert.ToString(drTransferdata["BookingStatus"]) == "On Request")
                                        {
                                            TransferHtml = TransferHtml.Replace("#TransferBookingStatusColor", "#f47920");
                                            TransferHtml = TransferHtml.Replace("#TransferBookingStatus", "On Request");
                                        }
                                        if (Convert.ToString(drTransferdata["BookingStatus"]) == "Vouchered")
                                        {
                                            TransferHtml = TransferHtml.Replace("#TransferBookingStatusColor", "#5e3ea0");
                                            TransferHtml = TransferHtml.Replace("#TransferBookingStatus", "Vouchered");
                                        }
                                        if (Convert.ToString(drTransferdata["BookingStatus"]) == "Not Confirmed")
                                        {
                                            TransferHtml = TransferHtml.Replace("#TransferBookingStatusColor", "#0f1d42");
                                            TransferHtml = TransferHtml.Replace("#TransferBookingStatus", "Not Confirmed");
                                        }
                                        else
                                        {
                                            TransferHtml = TransferHtml.Replace("#TransferBookingStatusColor", "green");
                                            TransferHtml = TransferHtml.Replace("#TransferBookingStatus", "Confirmed");
                                        }

                                        string FromLocation = "";
                                        string ToLocation = "";
                                        string roomtype = Convert.ToString(drTransferdata["RoomType"]);
                                        if (!string.IsNullOrEmpty(Convert.ToString(drTransferdata["RoomType"])))
                                        {
                                            FromLocation = Convert.ToString(roomtype.Split(new String[] { "$$" }, StringSplitOptions.None)[0]);
                                            ToLocation = Convert.ToString(roomtype.Split(new String[] { "$$" }, StringSplitOptions.None)[1]);
                                        }

                                        TransferHtml = TransferHtml.Replace("#TransferName ", Convert.ToString(drTransferdata["ServiceName"]));
                                        TransferHtml = TransferHtml.Replace("#TransferCityName", Convert.ToString(drTransferdata["CityName"]));
                                        TransferHtml = TransferHtml.Replace("#TransferType", Convert.ToString(drTransferdata["VehicleType"]));
                                        TransferHtml = TransferHtml.Replace("#TransferDate", Convert.ToString(drTransferdata["TravelDate"]));
                                        TransferHtml = TransferHtml.Replace("#FromLocation", FromLocation);
                                        TransferHtml = TransferHtml.Replace("#ToLocation", ToLocation);
                                        TransferHtml = TransferHtml.Replace("#TransferAdult", Convert.ToString(drTransferdata["Adult"]));
                                        TransferHtml = TransferHtml.Replace("#TransferChild", Convert.ToString(drTransferdata["Children"]));
                                        TransferHtml = TransferHtml.Replace("#TransferUnit", Convert.ToString(drTransferdata["NoOfNights"]));

                                        DataRow[] transferCancellation = dsv.Tables[13].Select("BookingDetailId='" + Convert.ToString(drTransferdata["BookingDetailId"]) + "'");
                                        string strTransferCancellation = string.Empty;
                                        foreach (DataRow drtransferCancellation in transferCancellation)
                                        {
                                            strTransferCancellation += Convert.ToString(drTransferdata["ServiceName"]) + " From " + Convert.ToString(string.Format("{0:d/M/yyyy HH:mm:ss tt}", drtransferCancellation["FromDate"])) + " To" + Convert.ToString(string.Format("{0:d/M/yyyy HH:mm:ss tt}", drtransferCancellation["ToDate"])) + " the cancellation charges are " + Convert.ToString(Math.Round(Convert.ToDouble(drtransferCancellation["CancellationAmount"]), 2)) + " " + Convert.ToString(AgentCurrency) + "<br/>";
                                        }
                                        TransferHtml = TransferHtml.Replace("#TransferCancellationPolicy", strTransferCancellation);

                                        TransferHtmlMain += TransferHtml;
                                        #region ISRoundTrip
                                        try
                                        {
                                            if (Convert.ToString(drTransferdata["dropup"]) == "1")
                                            {
                                                if (Convert.ToString(drTransferdata["BookingStatus"]) == "On Request")
                                                {
                                                    TransferHtmlRoundTrip = TransferHtmlRoundTrip.Replace("#TransferBookingStatusColor", "#f47920");
                                                    TransferHtmlRoundTrip = TransferHtmlRoundTrip.Replace("#TransferBookingStatus", "On Request");
                                                }
                                                if (Convert.ToString(drTransferdata["BookingStatus"]) == "Vouchered")
                                                {
                                                    TransferHtmlRoundTrip = TransferHtmlRoundTrip.Replace("#TransferBookingStatusColor", "#5e3ea0");
                                                    TransferHtmlRoundTrip = TransferHtmlRoundTrip.Replace("#TransferBookingStatus", "Vouchered");
                                                }
                                                if (Convert.ToString(drTransferdata["BookingStatus"]) == "Not Confirmed")
                                                {
                                                    TransferHtmlRoundTrip = TransferHtmlRoundTrip.Replace("#TransferBookingStatusColor", "#0f1d42");
                                                    TransferHtmlRoundTrip = TransferHtmlRoundTrip.Replace("#TransferBookingStatus", "Not Confirmed");
                                                }
                                                else
                                                {
                                                    TransferHtmlRoundTrip = TransferHtmlRoundTrip.Replace("#TransferBookingStatusColor", "green");
                                                    TransferHtmlRoundTrip = TransferHtmlRoundTrip.Replace("#TransferBookingStatus", "Confirmed");
                                                }
                                                TransferHtmlRoundTrip = TransferHtmlRoundTrip.Replace("#TransferName ", Convert.ToString(drTransferdata["ServiceName"]));
                                                TransferHtmlRoundTrip = TransferHtmlRoundTrip.Replace("#TransferCityName", Convert.ToString(drTransferdata["CityName"]));
                                                TransferHtmlRoundTrip = TransferHtmlRoundTrip.Replace("#TransferType", Convert.ToString(drTransferdata["VehicleType"]));
                                                TransferHtmlRoundTrip = TransferHtmlRoundTrip.Replace("#TransferDate", Convert.ToString(drTransferdata["CheckOutDate"]));
                                                TransferHtmlRoundTrip = TransferHtmlRoundTrip.Replace("#FromLocation", ToLocation);
                                                TransferHtmlRoundTrip = TransferHtmlRoundTrip.Replace("#ToLocation", FromLocation);
                                                TransferHtmlRoundTrip = TransferHtmlRoundTrip.Replace("#TransferAdult", Convert.ToString(drTransferdata["Adult"]));
                                                TransferHtmlRoundTrip = TransferHtmlRoundTrip.Replace("#TransferChild", Convert.ToString(drTransferdata["Children"]));
                                                TransferHtmlRoundTrip = TransferHtmlRoundTrip.Replace("#TransferUnit", Convert.ToString(drTransferdata["NoOfNights"]));
                                                TransferHtmlRoundTrip = TransferHtmlRoundTrip.Replace("#TransferCancellationPolicy", strTransferCancellation);

                                                TransferHtmlMain += TransferHtmlRoundTrip;
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            _ = _exception.ErrorBooking(ex, Convert.ToString(AgentId));
                                        }
                                        #endregion

                                        #endregion
                                    }
                                }
                                #endregion
                            }
                            if (Convert.ToString(dsv.Tables[15].Rows[i]["ServiceType"]) == "City Tour" && Convert.ToInt32(dsv.Tables[15].Rows[i]["ComboId"]) > 0)
                            {
                                DataRow[] drCityTour = dsv.Tables[0].Select("ServiceType='City Tour' and ComboId=0 and BookingId='" + Convert.ToString(dsv.Tables[15].Rows[i]["BookingId"]) + "'");
                                #region City Tour
                                //string CityTourHtmlMain = string.Empty;
                                if (drCityTour.Length > 0)
                                {
                                    string TourBookingSubject = string.Empty;
                                    string CityTourHtml = string.Empty;
                                    foreach (DataRow drTourdata in drCityTour)
                                    {
                                        if (!string.IsNullOrEmpty(Convert.ToString(drTourdata["CountryId"])) && countryId == 0)
                                            countryId = Convert.ToInt32(drTourdata["CountryId"]);
                                        if (IstodayBooking == 0 && Convert.ToInt32(drTourdata["IsLastTimeBooking"]) == 1)
                                            IstodayBooking = 1;

                                        CityTourHtml = TourTemplate;
                                        #region "for Agent"
                                        if (Convert.ToString(drTourdata["BookingStatus"]) == "On Request")
                                        {
                                            CityTourHtml = CityTourHtml.Replace("#TourBookingStatusColor", "#f47920");
                                            CityTourHtml = CityTourHtml.Replace("#TourBookingStatus", "On Request");
                                        }
                                        else if (Convert.ToString(drTourdata["BookingStatus"]) == "Not Confirmed")
                                        {
                                            CityTourHtml = CityTourHtml.Replace("#TourBookingStatusColor", "#0f1d42");
                                            CityTourHtml = CityTourHtml.Replace("#TourBookingStatus", "Not Confirmed");
                                        }
                                        else if (Convert.ToString(drTourdata["BookingStatus"]) == "Vouchered")
                                        {
                                            CityTourHtml = CityTourHtml.Replace("#TourBookingStatusColor", "#5e3ea0");
                                            CityTourHtml = CityTourHtml.Replace("#TourBookingStatus", "Vouchered");
                                        }

                                        else
                                        {
                                            CityTourHtml = CityTourHtml.Replace("#TourBookingStatusColor", "green");
                                            CityTourHtml = CityTourHtml.Replace("#TourBookingStatus", "Confirmed");
                                        }
                                        string pickupdata = "-";
                                        if (!string.IsNullOrEmpty(Convert.ToString(drTourdata["pickup"])))
                                            pickupdata = Convert.ToString(Convert.ToString(drTourdata["pickup"]).Split('/')[0].Split(':')[1]);


                                        CityTourHtml = CityTourHtml.Replace("#TourName", Convert.ToString(drTourdata["ServiceName"]));
                                        CityTourHtml = CityTourHtml.Replace("#TourCityName", Convert.ToString(drTourdata["CityName"]));
                                        CityTourHtml = CityTourHtml.Replace("#TourType", Convert.ToString(drTourdata["RoomType"]));
                                        CityTourHtml = CityTourHtml.Replace("#TourDate", Convert.ToString(drTourdata["TravelDate"]));
                                        CityTourHtml = CityTourHtml.Replace("#TourPickup", Convert.ToString(pickupdata));
                                        CityTourHtml = CityTourHtml.Replace("#TourUnit", "1");
                                        CityTourHtml = CityTourHtml.Replace("#TourAdult", Convert.ToString(drTourdata["Adult"]));
                                        CityTourHtml = CityTourHtml.Replace("#TourChild", Convert.ToString(drTourdata["Children"]));

                                        DataRow[] tourCancellation = dsv.Tables[3].Select("BookingDetailId='" + Convert.ToString(drTourdata["BookingDetailId"]) + "'");
                                        string strTourCancellation = string.Empty;
                                        foreach (DataRow drtourCancellation in tourCancellation)
                                        {
                                            strTourCancellation += Convert.ToString(drTourdata["ServiceName"]) + " From " + Convert.ToString(string.Format("{0:d/M/yyyy HH:mm:ss tt}", drtourCancellation["FromDate"])) + " To" + Convert.ToString(string.Format("{0:d/M/yyyy HH:mm:ss tt}", drtourCancellation["ToDate"])) + " the cancellation charges are " + Convert.ToString(Math.Round(Convert.ToDouble(drtourCancellation["CancellationAmount"]), 2)) + " " + Convert.ToString(AgentCurrency) + "<br/>";
                                        }
                                        CityTourHtml = CityTourHtml.Replace("#TourCancellationPolicy", strTourCancellation);

                                        CityTourHtmlMain += CityTourHtml;
                                        #endregion
                                    }

                                }
                                #endregion
                            }
                            if (Convert.ToString(dsv.Tables[15].Rows[i]["ServiceType"]) == "Hotel")
                            {
                                #region Hotel
                                // string HotelHtmlMain = string.Empty;
                                int room = 0;
                                int TotalRoom = 0;
                                int TotalNoOfNights = 0;
                                int TotalNoofAdult = 0;
                                int TotalNoofChild = 0;
                                string RoomCategory = "";
                                DataRow[] drhotelnew = dsv.Tables[0].Select("ServiceType='Hotel' and BookingId='" + Convert.ToString(dsv.Tables[15].Rows[i]["BookingId"]) + "'");
                                string HotelTemplate1New = string.Empty;

                                string RoomTemplate = string.Empty;
                                HotelTemplate1New = HotelTemplate1;
                                foreach (DataRow drHoteldatanew in drhotelnew)
                                {
                                    TotalRoom += Convert.ToInt32(drHoteldatanew["NoOfRooms"]);
                                    if (countryId == 0)
                                        countryId = Convert.ToInt32(drHoteldatanew["CountryId"]);
                                    if (IstodayBooking == 0 && Convert.ToInt32(drHoteldatanew["IsLastTimeBooking"]) == 1)
                                        IstodayBooking = 1;
                                    string HotelTemplate2New = string.Empty;
                                    HotelTemplate2New = HotelTemplate2;
                                    room += 1;
                                    TotalNoOfNights = Convert.ToInt32(drHoteldatanew["NoOfNights"]);
                                    TotalNoofAdult += Convert.ToInt32(drHoteldatanew["Adult"]);
                                    TotalNoofChild += Convert.ToInt32(drHoteldatanew["Children"]);
                                    RoomCategory += Convert.ToString(drHoteldatanew["RoomType"]) + ",";

                                    //DataRow[] drpassenger = dsv.Tables[1].Select("BookingDetailId='" + Convert.ToString(drHoteldatanew["BookingDetailId"]) + "'");

                                    if (Convert.ToString(drHoteldatanew["BookingStatus"]) == "On Request")
                                    {
                                        HotelTemplate1New = HotelTemplate1New.Replace("#HotelBookingStatusColor", "#f47920");
                                        HotelTemplate1New = HotelTemplate1New.Replace("#HotelBookingStatus", "On Request");
                                    }
                                    else if (Convert.ToString(drHoteldatanew["BookingStatus"]) == "Vouchered")
                                    {
                                        HotelTemplate1New = HotelTemplate1New.Replace("#HotelBookingStatusColor", "#5e3ea0");
                                        HotelTemplate1New = HotelTemplate1New.Replace("#HotelBookingStatus", "Vouchered");
                                    }
                                    else if (Convert.ToString(drHoteldatanew["BookingStatus"]) == "Not Confirmed")//----------
                                    {
                                        HotelTemplate1New = HotelTemplate1New.Replace("#HotelBookingStatusColor", "#0f1d42");
                                        HotelTemplate1New = HotelTemplate1New.Replace("#HotelBookingStatus", "Not Confirmed");
                                    }
                                    else
                                    {
                                        HotelTemplate1New = HotelTemplate1New.Replace("#HotelBookingStatusColor", "green");
                                        HotelTemplate1New = HotelTemplate1New.Replace("#HotelBookingStatus", "Confirmed");
                                    }

                                    HotelTemplate2New = HotelTemplate2New.Replace("#RoomNo", Convert.ToString(room));
                                    HotelTemplate2New = HotelTemplate2New.Replace("#RoomName", Convert.ToString(drHoteldatanew["RoomType"]));
                                    HotelTemplate2New = HotelTemplate2New.Replace("#RoomAdult", Convert.ToString(drHoteldatanew["Adult"]));
                                    HotelTemplate2New = HotelTemplate2New.Replace("#RoomChild", Convert.ToString(drHoteldatanew["Children"]));
                                    HotelTemplate2New = HotelTemplate2New.Replace("#Board", Convert.ToString(drHoteldatanew["Board"]));
                                    HotelTemplate2New = HotelTemplate2New.Replace("#RoomGuestName", Convert.ToString(drHoteldatanew["Name"]));

                                    DataRow[] hotelCancellation = dsv.Tables[2].Select("BookingDetailId='" + Convert.ToString(drHoteldatanew["BookingDetailId"]) + "'");
                                    string strHotelCancellation = string.Empty;

                                    foreach (DataRow drhotelCancellation in hotelCancellation)
                                    {
                                        strHotelCancellation += Convert.ToString(drHoteldatanew["ServiceName"]) + " From " + Convert.ToString(drhotelCancellation["FromDate"]) + " To" + Convert.ToString(drhotelCancellation["ToDate"]) + " the cancellation charges are " + Convert.ToString(Math.Round(Convert.ToDouble(drhotelCancellation["CancellationAmount"]), 2)) + " " + Convert.ToString(AgentCurrency) + "<br/>";
                                    }
                                    HotelTemplate2New = HotelTemplate2New.Replace("#HotelCancellationPolicy", strHotelCancellation);

                                    if (room == drhotelnew.Length)
                                    {
                                        HotelTemplate1New = HotelTemplate1New.Replace("#HotelName", Convert.ToString(drHoteldatanew["ServiceName"]));
                                        HotelTemplate1New = HotelTemplate1New.Replace("#CityName", Convert.ToString(drHoteldatanew["CityName"]));
                                        HotelTemplate1New = HotelTemplate1New.Replace("#CityName", Convert.ToString(drHoteldatanew["CityName"]));
                                        HotelTemplate1New = HotelTemplate1New.Replace("#Checkindate", Convert.ToString(drHoteldatanew["TravelDate"]));
                                        HotelTemplate1New = HotelTemplate1New.Replace("#Checkoutdate", Convert.ToString(drHoteldatanew["CheckOutDate"]));
                                        HotelTemplate1New = HotelTemplate1New.Replace("#Nights", Convert.ToString(TotalNoOfNights));
                                        HotelTemplate1New = HotelTemplate1New.Replace("#RoomCategory", RoomCategory);
                                        HotelTemplate1New = HotelTemplate1New.Replace("#NoofRoom", Convert.ToString(Convert.ToInt32(drHoteldatanew["NoOfRooms"])));
                                        HotelTemplate1New = HotelTemplate1New.Replace("#HotelAdult", Convert.ToString(TotalNoofAdult));
                                        HotelTemplate1New = HotelTemplate1New.Replace("#HotelChild", Convert.ToString(TotalNoofChild));
                                    }

                                    RoomTemplate += HotelTemplate2New;
                                }
                                HotelHtmlMain += HotelTemplate1New + RoomTemplate;



                                #endregion
                            }
                            FinalAgentMailBodyTemplate = FinalAgentMailBodyTemplate + HotelHtmlMain + TransferHtmlMain + CityTourHtmlMain + MealHtmlMain;
                        }
                        FinalAgentTemplate = commonTemplate + FinalAgentMailBodyTemplate + FooterTemplate;
                    }
                    catch (Exception ex)
                    {
                        _ = _exception.ErrorBooking(ex, Convert.ToString(AgentId));
                    }
                    #region Supplier Email

                    await SendEmailToSupplier(ReferenceNo, 127, dsv, AgentId, HostName);
                    try
                    {
                        await SendEmailToSupplierForConfirmationNo(ReferenceNo, dsv, HostName); //by Pratik
                    }
                    catch (Exception ex)
                    {

                        _ = _exception.ErrorNew(ex, _LogAPI.Book);
                    }
                    // }
                    string SupplierCommonTemplate = string.Empty;
                    string SupplierFromId = string.Empty;
                    string SupplierCCId = string.Empty;
                    string SupplierToEmailId = string.Empty;
                    if (dsSupplierEmail != null && dsSupplierEmail.Tables.Count > 0)
                    {
                        foreach (DataRow drsup in dsSupplierEmail.Tables[1].Rows)
                        {
                            string SupplierCommonDetailTemplate = string.Empty;
                            DataRow[] drCityToursupplier = dsv.Tables[0].Select("ServiceType='City Tour' and ComboId=0 and SupplierId='" + Convert.ToString(drsup["SupplierId"]) + "'");
                            //DataRow[] drHotel = dsv.Tables[14].Select("1=1");
                            //DataRow[] drCityTourCombo = dsv.Tables[0].Select("ServiceType='City Tour' and ComboId>0");
                            //DataRow[] drCombo = dsv.Tables[7].Select();
                            DataRow[] drMealDetailsupplier = dsv.Tables[0].Select("ServiceType='Meal' and SupplierId='" + Convert.ToString(drsup["SupplierId"]) + "'");
                            DataRow[] drTransferDetailsupplier = dsv.Tables[0].Select("ServiceType='Transfer' and SupplierId='" + Convert.ToString(drsup["SupplierId"]) + "'");

                            #region City Tour
                            string CityTourHtmlMainSupplier = string.Empty;
                            if (drCityToursupplier.Length > 0)
                            {
                                string TourBookingSubject = string.Empty;
                                string CityTourHtml = string.Empty;
                                string CityTourHtmlSupplier = string.Empty;
                                string SupplierTourDetailTemplate = string.Empty;
                                foreach (DataRow drTourdata in drCityToursupplier)
                                {
                                    if (!string.IsNullOrEmpty(Convert.ToString(drTourdata["CountryId"])) && countryId == 0)
                                        countryId = Convert.ToInt32(drTourdata["CountryId"]);

                                    string pickupdata = "-";
                                    if (!string.IsNullOrEmpty(Convert.ToString(drTourdata["pickup"])))
                                        pickupdata = Convert.ToString(Convert.ToString(drTourdata["pickup"]).Split('/')[0].Split(':')[1]);


                                    CityTourHtml = TourTemplate;
                                    CityTourHtmlSupplier = SupplierTourTemplate;
                                    SupplierTourDetailTemplate = SupplierDetailTemplate;

                                    #region "for Supplier"
                                    SupplierTourDetailTemplate = SupplierTourDetailTemplate.Replace("#SupplierName", Convert.ToString(drTourdata["SupplierName"]));
                                    SupplierTourDetailTemplate = SupplierTourDetailTemplate.Replace("#GuestName", Convert.ToString(drTourdata["Name"]));
                                    SupplierTourDetailTemplate = SupplierTourDetailTemplate.Replace("#FileNo", strRefNo);
                                    SupplierTourDetailTemplate = SupplierTourDetailTemplate.Replace("#IssueDate", Convert.ToString(drTourdata["IssueDate"]));
                                    //SupplierTourDetailTemplate = SupplierTourDetailTemplate.Replace("#BookingService", "Tour");
                                    //SupplierTourDetailTemplate = SupplierTourDetailTemplate.Replace("#BookingStatus", Convert.ToString(drTourdata["BookingStatus"]));

                                    CityTourHtmlSupplier = CityTourHtmlSupplier.Replace("#TourBookingService", "Tour");
                                    CityTourHtmlSupplier = CityTourHtmlSupplier.Replace("#TourBookingStatus", Convert.ToString(drTourdata["BookingStatus"]));
                                    CityTourHtmlSupplier = CityTourHtmlSupplier.Replace("#TourName", Convert.ToString(drTourdata["ServiceName"]));
                                    CityTourHtmlSupplier = CityTourHtmlSupplier.Replace("#TourType", Convert.ToString(drTourdata["RoomType"]));
                                    CityTourHtmlSupplier = CityTourHtmlSupplier.Replace("#Pickup", Convert.ToString(pickupdata));
                                    CityTourHtmlSupplier = CityTourHtmlSupplier.Replace("#TourCityName", Convert.ToString(drTourdata["CityName"]));
                                    CityTourHtmlSupplier = CityTourHtmlSupplier.Replace("#TourServiceType", "Tour");
                                    CityTourHtmlSupplier = CityTourHtmlSupplier.Replace("#TourServiceDate", Convert.ToString(drTourdata["TravelDate"]));
                                    CityTourHtmlSupplier = CityTourHtmlSupplier.Replace("#TourNoofAdult", Convert.ToString(drTourdata["Adult"]));
                                    CityTourHtmlSupplier = CityTourHtmlSupplier.Replace("#TourNoofChild", Convert.ToString(drTourdata["Children"]));
                                    CityTourHtmlSupplier = CityTourHtmlSupplier.Replace("#TourTotalPax", Convert.ToString(Convert.ToInt32(drTourdata["Adult"]) + Convert.ToInt32(drTourdata["Children"])));
                                    CityTourHtmlSupplier = CityTourHtmlSupplier.Replace("#TourRemark", "");
                                    //CityTourHtmlMainSupplier += SupplierTourDetailTemplate + CityTourHtmlSupplier;
                                    CityTourHtmlMainSupplier += CityTourHtmlSupplier;
                                    #endregion
                                    DataRow[] drCityTourSupplier = dsSupplierEmail.Tables[0].Select("ServiceType='Tour' and BookingDetailId='" + Convert.ToString(drTourdata["BookingDetailId"]) + "'");
                                    if (drCityTourSupplier.Length > 0)
                                    {
                                        SupplierToEmailId = Convert.ToString(drCityTourSupplier[0]["EmailId"]);
                                        try
                                        {
                                            DataTable dtMail1 = await GetEmailIdsBranchWise(Convert.ToInt32(AgentId), Convert.ToInt32(drTourdata["CountryId"]));
                                            if (dtMail1.Rows.Count > 0)
                                            {
                                                if (!String.IsNullOrEmpty(Convert.ToString(dtMail1.Rows[0]["BranchEmail"])))
                                                {
                                                    if (!string.IsNullOrEmpty(SupplierCCId))
                                                        SupplierCCId = SupplierCCId + "," + Convert.ToString(dtMail1.Rows[0]["BranchEmail"]);
                                                    else
                                                        SupplierCCId = Convert.ToString(dtMail1.Rows[0]["BranchEmail"]);
                                                }
                                                if (!String.IsNullOrEmpty(Convert.ToString(dtMail1.Rows[0]["EmployeeEmailId"])))
                                                    SupplierFromId = Convert.ToString(dtMail1.Rows[0]["EmployeeEmailId"]);
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            _ = _exception.ErrorBooking(ex, Convert.ToString(AgentId));
                                        }
                                        if (!string.IsNullOrEmpty(SupplierCCId))
                                            SupplierCCId = SupplierCCId + "," + SupplierFromId;
                                        else
                                            SupplierCCId = SupplierFromId;
                                        if (!string.IsNullOrEmpty(Convert.ToString(drCityTourSupplier[0]["CCEmailId"])))
                                        {
                                            SupplierCCId = SupplierCCId + "," + Convert.ToString(drCityTourSupplier[0]["CCEmailId"]);
                                        }
                                    }
                                    if (string.IsNullOrEmpty(SupplierCommonDetailTemplate))
                                        SupplierCommonDetailTemplate = SupplierTourDetailTemplate;
                                }
                            }
                            #endregion

                            #region Meal
                            string MealHtmlMainSupplier = string.Empty;
                            if (drMealDetailsupplier.Length > 0)
                            {
                                // string Subject = "Booking Confirmation East Avenue LLC : " + Convert.ToString(drMealDetail[0]["ReferenceNo"]);
                                string SupplierMealDetailTemplate = string.Empty;
                                foreach (DataRow drMealdata in drMealDetailsupplier)
                                {
                                    if (countryId == 0)
                                        countryId = Convert.ToInt32(drMealdata["CountryId"]);
                                    string MealHtml = string.Empty;
                                    string MealHtmlSupplier = string.Empty;

                                    MealHtmlSupplier = SupplierMealTemplate;
                                    SupplierMealDetailTemplate = SupplierDetailTemplate;

                                    #region "for Supplier"
                                    SupplierMealDetailTemplate = SupplierMealDetailTemplate.Replace("#SupplierName", Convert.ToString(drMealdata["SupplierName"]));
                                    SupplierMealDetailTemplate = SupplierMealDetailTemplate.Replace("#GuestName", Convert.ToString(drMealdata["Name"]));
                                    SupplierMealDetailTemplate = SupplierMealDetailTemplate.Replace("#FileNo", strRefNo);
                                    SupplierMealDetailTemplate = SupplierMealDetailTemplate.Replace("#IssueDate", Convert.ToString(drMealdata["IssueDate"]));
                                    // SupplierMealDetailTemplate = SupplierMealDetailTemplate.Replace("#BookingService", "Meal");
                                    //SupplierMealDetailTemplate = SupplierMealDetailTemplate.Replace("#BookingStatus", Convert.ToString(drMealdata["BookingStatus"]));

                                    MealHtmlSupplier = MealHtmlSupplier.Replace("#MealBookingService", "Meal");
                                    MealHtmlSupplier = MealHtmlSupplier.Replace("#MealBookingStatus", Convert.ToString(drMealdata["BookingStatus"]));
                                    MealHtmlSupplier = MealHtmlSupplier.Replace("#MealName", Convert.ToString(drMealdata["ServiceName"]));
                                    MealHtmlSupplier = MealHtmlSupplier.Replace("#MealCityName", Convert.ToString(drMealdata["CityName"]));
                                    MealHtmlSupplier = MealHtmlSupplier.Replace("#MealType", Convert.ToString(drMealdata["RoomType"]));
                                    MealHtmlSupplier = MealHtmlSupplier.Replace("#MealServiceType", "Meal");
                                    MealHtmlSupplier = MealHtmlSupplier.Replace("#MealServiceDate", Convert.ToString(drMealdata["TravelDate"]));
                                    MealHtmlSupplier = MealHtmlSupplier.Replace("#MealNoofAdult", Convert.ToString(drMealdata["Adult"]));
                                    MealHtmlSupplier = MealHtmlSupplier.Replace("#MealNoofChild", Convert.ToString(drMealdata["Children"]));
                                    MealHtmlSupplier = MealHtmlSupplier.Replace("#MealTotalPax", Convert.ToString(Convert.ToInt32(drMealdata["Adult"]) + Convert.ToInt32(drMealdata["Children"])));

                                    MealHtmlSupplier = MealHtmlSupplier.Replace("#MealRemark", "");
                                    MealHtmlMainSupplier += MealHtmlSupplier;
                                    #endregion
                                    try
                                    {
                                        DataRow[] drMealSupplier = dsSupplierEmail.Tables[0].Select("ServiceType='Meal' and BookingDetailId='" + Convert.ToString(drMealdata["BookingDetailId"]) + "'");
                                        if (drMealSupplier.Length > 0)
                                        {
                                            SupplierToEmailId = Convert.ToString(drMealSupplier[0]["EmailId"]);
                                            try
                                            {
                                                DataTable dtMail1 = await GetEmailIdsBranchWise(Convert.ToInt32(AgentId), Convert.ToInt32(drMealdata["CountryId"]));
                                                if (dtMail1.Rows.Count > 0)
                                                {
                                                    if (!String.IsNullOrEmpty(Convert.ToString(dtMail1.Rows[0]["BranchEmail"])))
                                                    {
                                                        if (!string.IsNullOrEmpty(SupplierCCId))
                                                            SupplierCCId = SupplierCCId + "," + Convert.ToString(dtMail1.Rows[0]["BranchEmail"]);
                                                        else
                                                            SupplierCCId = Convert.ToString(dtMail1.Rows[0]["BranchEmail"]);
                                                    }
                                                    if (!String.IsNullOrEmpty(Convert.ToString(dtMail1.Rows[0]["EmployeeEmailId"])))
                                                        SupplierFromId = Convert.ToString(dtMail1.Rows[0]["EmployeeEmailId"]);
                                                }
                                            }
                                            catch (Exception ex)
                                            {
                                                _ = _exception.ErrorBooking(ex, Convert.ToString(AgentId));
                                            }
                                            if (!string.IsNullOrEmpty(SupplierCCId))
                                                SupplierCCId = SupplierCCId + "," + SupplierFromId;
                                            else
                                                SupplierCCId = SupplierFromId;
                                            if (!string.IsNullOrEmpty(Convert.ToString(drMealSupplier[0]["CCEmailId"])))
                                            {
                                                SupplierCCId = SupplierCCId + "," + Convert.ToString(drMealSupplier[0]["CCEmailId"]);
                                            }
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        _ = _exception.ErrorBooking(ex, Convert.ToString(AgentId));
                                    }
                                }
                                if (string.IsNullOrEmpty(SupplierCommonDetailTemplate))
                                    SupplierCommonDetailTemplate = SupplierMealDetailTemplate;
                            }
                            #endregion

                            #region Transfer
                            string TransferHtmlMainSupplier = string.Empty;
                            string TransferHtmlMainSupplierRoundTrip = string.Empty;
                            if (drTransferDetailsupplier.Length > 0)
                            {
                                string SupplierTransferDetailTemplate = string.Empty;
                                string SupplierTransferDetailTemplateRoundTrip = string.Empty;
                                foreach (DataRow drTransferdata in drTransferDetailsupplier)
                                {
                                    if (countryId == 0)
                                        countryId = Convert.ToInt32(drTransferdata["CountryId"]);
                                    string TransferHtmlSupplier = string.Empty;
                                    string TransferHtmlSupplierRoundTrip = string.Empty;

                                    TransferHtmlSupplier = SupplierTransferTemplate;
                                    SupplierTransferDetailTemplate = SupplierDetailTemplate;

                                    TransferHtmlSupplierRoundTrip = SupplierTransferTemplate;
                                    SupplierTransferDetailTemplateRoundTrip = SupplierDetailTemplate;

                                    string FromLocation = "";
                                    string ToLocation = "";
                                    string roomtype = Convert.ToString(drTransferdata["RoomType"]);
                                    if (!string.IsNullOrEmpty(Convert.ToString(drTransferdata["RoomType"])))
                                    {
                                        FromLocation = Convert.ToString(roomtype.Split(new String[] { "$$" }, StringSplitOptions.None)[0]);
                                        ToLocation = Convert.ToString(roomtype.Split(new String[] { "$$" }, StringSplitOptions.None)[1]);
                                    }

                                    #region "for Supplier"
                                    string FlightTime = string.Empty;
                                    string FlightNo = string.Empty;
                                    try
                                    {
                                        if (!string.IsNullOrEmpty(Convert.ToString(drTransferdata["pickup"])))
                                        {
                                            if (Convert.ToString(drTransferdata["pickup"]).Split('/')[1].Contains("Flight No"))
                                            {
                                                //FlightNo = Convert.ToString(Convert.ToString(drTransferdata["pickup"]).Split('/')[1].Split(':')[1]);
                                                if (Convert.ToString(drTransferdata["pickup"]).Split('/')[1].Split(':').Length > 1)
                                                {
                                                    for (int i = 1; i < Convert.ToString(drTransferdata["pickup"]).Split('/')[1].Split(':').Length; i++)
                                                    {
                                                        if (!string.IsNullOrEmpty(FlightNo))
                                                            FlightNo = FlightNo + ":" + (Convert.ToString(drTransferdata["pickup"]).Split('/')[1].Split(':')[i]);
                                                        else
                                                            FlightNo = (Convert.ToString(drTransferdata["pickup"]).Split('/')[1].Split(':')[i]);
                                                    }
                                                }
                                            }
                                            if (Convert.ToString(drTransferdata["pickup"]).Split('/')[1].Contains("Flight Time"))
                                            {
                                                // FlightTime = Convert.ToString(Convert.ToString(drTransferdata["pickup"]).Split('/')[1].Split(':')[1]);
                                                if (Convert.ToString(drTransferdata["pickup"]).Split('/')[1].Split(':').Length > 1)
                                                {
                                                    for (int i = 1; i < Convert.ToString(drTransferdata["pickup"]).Split('/')[1].Split(':').Length; i++)
                                                    {
                                                        if (!string.IsNullOrEmpty(FlightTime))
                                                            FlightTime = FlightTime + ":" + (Convert.ToString(drTransferdata["pickup"]).Split('/')[1].Split(':')[i]);
                                                        else
                                                            FlightTime = (Convert.ToString(drTransferdata["pickup"]).Split('/')[1].Split(':')[i]);
                                                    }
                                                }
                                            }
                                            if (Convert.ToString(drTransferdata["pickup"]).Split('/')[0].Contains("Flight No"))
                                            {
                                                // FlightNo = Convert.ToString(Convert.ToString(drTransferdata["pickup"]).Split('/')[0].Split(':')[1]);
                                                if (Convert.ToString(drTransferdata["pickup"]).Split('/')[0].Split(':').Length > 1)
                                                {
                                                    for (int i = 1; i < Convert.ToString(drTransferdata["pickup"]).Split('/')[0].Split(':').Length; i++)
                                                    {
                                                        if (!string.IsNullOrEmpty(FlightNo))
                                                            FlightNo = FlightNo + ":" + (Convert.ToString(drTransferdata["pickup"]).Split('/')[0].Split(':')[i]);
                                                        else
                                                            FlightNo = (Convert.ToString(drTransferdata["pickup"]).Split('/')[0].Split(':')[i]);
                                                    }
                                                }
                                            }
                                            if (Convert.ToString(drTransferdata["pickup"]).Split('/')[0].Contains("Flight Time"))
                                            {
                                                // FlightTime = Convert.ToString(Convert.ToString(drTransferdata["pickup"]).Split('/')[0].Split(':')[1]);
                                                if (Convert.ToString(drTransferdata["pickup"]).Split('/')[0].Split(':').Length > 1)
                                                {
                                                    for (int i = 1; i < Convert.ToString(drTransferdata["pickup"]).Split('/')[0].Split(':').Length; i++)
                                                    {
                                                        if (!string.IsNullOrEmpty(FlightTime))
                                                            FlightTime = FlightTime + ":" + (Convert.ToString(drTransferdata["pickup"]).Split('/')[0].Split(':')[i]);
                                                        else
                                                            FlightTime = (Convert.ToString(drTransferdata["pickup"]).Split('/')[0].Split(':')[i]);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        _ = _exception.ErrorBooking(ex, Convert.ToString(AgentId));
                                    }
                                    // SupplierTransferDetailTemplate = SupplierTransferDetailTemplate.Replace("#BookingStatus", Convert.ToString(drTransferdata["BookingStatus"]));
                                    SupplierTransferDetailTemplate = SupplierTransferDetailTemplate.Replace("#SupplierName", Convert.ToString(drTransferdata["SupplierName"]));
                                    SupplierTransferDetailTemplate = SupplierTransferDetailTemplate.Replace("#GuestName", Convert.ToString(drTransferdata["Name"]));
                                    SupplierTransferDetailTemplate = SupplierTransferDetailTemplate.Replace("#FileNo", strRefNo);
                                    SupplierTransferDetailTemplate = SupplierTransferDetailTemplate.Replace("#IssueDate", Convert.ToString(drTransferdata["IssueDate"]));
                                    //SupplierTransferDetailTemplate = SupplierTransferDetailTemplate.Replace("#BookingService", "Transfer");

                                    TransferHtmlSupplier = TransferHtmlSupplier.Replace("#TransferBookingService", "Transfer");
                                    TransferHtmlSupplier = TransferHtmlSupplier.Replace("#TransferBookingStatus", Convert.ToString(drTransferdata["BookingStatus"]));
                                    TransferHtmlSupplier = TransferHtmlSupplier.Replace("#TransferName", Convert.ToString(drTransferdata["ServiceName"]));
                                    TransferHtmlSupplier = TransferHtmlSupplier.Replace("#TransferCityName", Convert.ToString(drTransferdata["CityName"]));
                                    TransferHtmlSupplier = TransferHtmlSupplier.Replace("#TransferType", Convert.ToString(drTransferdata["ServiceName"]));
                                    TransferHtmlSupplier = TransferHtmlSupplier.Replace("#FromLocation", Convert.ToString(FromLocation));
                                    TransferHtmlSupplier = TransferHtmlSupplier.Replace("#ToLocation", Convert.ToString(ToLocation));
                                    TransferHtmlSupplier = TransferHtmlSupplier.Replace("#TransferServiceType", "Transfer");
                                    TransferHtmlSupplier = TransferHtmlSupplier.Replace("#TransferServiceDate", Convert.ToString(drTransferdata["TravelDate"]));
                                    TransferHtmlSupplier = TransferHtmlSupplier.Replace("#TransferNoofAdult", Convert.ToString(drTransferdata["Adult"]));
                                    TransferHtmlSupplier = TransferHtmlSupplier.Replace("#TransferNoofChild", Convert.ToString(drTransferdata["Children"]));
                                    TransferHtmlSupplier = TransferHtmlSupplier.Replace("#TransferTotalPax", Convert.ToString(Convert.ToInt32(drTransferdata["Adult"]) + Convert.ToInt32(drTransferdata["Children"])));
                                    TransferHtmlSupplier = TransferHtmlSupplier.Replace("#VehicalType", Convert.ToString(drTransferdata["VehicleType"]));
                                    TransferHtmlSupplier = TransferHtmlSupplier.Replace("#FlightNo", FlightNo);
                                    TransferHtmlSupplier = TransferHtmlSupplier.Replace("#FlightTime", FlightTime);
                                    TransferHtmlSupplier = TransferHtmlSupplier.Replace("#TransferRemark", "");
                                    // TransferHtmlMainSupplier += SupplierTransferDetailTemplate + TransferHtmlSupplier;
                                    TransferHtmlMainSupplier += TransferHtmlSupplier;

                                    #region Is RoundTrip
                                    try
                                    {
                                        string FlightTimeRoundTrip = string.Empty;
                                        string FlightNoRoundTrip = string.Empty;
                                        if (Convert.ToString(drTransferdata["dropup"]) == "1")
                                        {
                                            try
                                            {
                                                if (!string.IsNullOrEmpty(Convert.ToString(drTransferdata["pickup"])))
                                                {

                                                    if (Convert.ToString(drTransferdata["pickup"]).Split('/').Length > 2)
                                                    {
                                                        if (Convert.ToString(drTransferdata["pickup"]).Split('/')[2].Contains("Flight Time"))
                                                        {
                                                            if (Convert.ToString(drTransferdata["pickup"]).Split('/')[2].Split(':').Length > 1)
                                                            {
                                                                for (int i = 1; i < Convert.ToString(drTransferdata["pickup"]).Split('/')[2].Split(':').Length; i++)
                                                                {
                                                                    if (!string.IsNullOrEmpty(FlightTimeRoundTrip))
                                                                        FlightTimeRoundTrip = FlightTimeRoundTrip + ":" + (Convert.ToString(drTransferdata["pickup"]).Split('/')[2].Split(':')[i]);
                                                                    else
                                                                        FlightTimeRoundTrip = (Convert.ToString(drTransferdata["pickup"]).Split('/')[2].Split(':')[i]);
                                                                }
                                                            }
                                                        }
                                                        if (Convert.ToString(drTransferdata["pickup"]).Split('/')[2].Contains("Flight No"))
                                                        {
                                                            if (Convert.ToString(drTransferdata["pickup"]).Split('/')[2].Split(':').Length > 1)
                                                            {
                                                                for (int i = 1; i < Convert.ToString(drTransferdata["pickup"]).Split('/')[2].Split(':').Length; i++)
                                                                {
                                                                    if (!string.IsNullOrEmpty(FlightNoRoundTrip))
                                                                        FlightNoRoundTrip = FlightNoRoundTrip + ":" + (Convert.ToString(drTransferdata["pickup"]).Split('/')[2].Split(':')[i]);
                                                                    else
                                                                        FlightNoRoundTrip = (Convert.ToString(drTransferdata["pickup"]).Split('/')[2].Split(':')[i]);
                                                                }
                                                            }
                                                            // FlightNoRoundTrip = Convert.ToString(Convert.ToString(drTransferdata["pickup"]).Split('/')[2].Split(':')[1]);
                                                        }
                                                        if (Convert.ToString(drTransferdata["pickup"]).Split('/')[3].Contains("Flight No"))
                                                        {
                                                            if (Convert.ToString(drTransferdata["pickup"]).Split('/')[3].Split(':').Length > 1)
                                                            {
                                                                for (int i = 1; i < Convert.ToString(drTransferdata["pickup"]).Split('/')[3].Split(':').Length; i++)
                                                                {
                                                                    if (!string.IsNullOrEmpty(FlightNoRoundTrip))
                                                                        FlightNoRoundTrip = FlightNoRoundTrip + ":" + (Convert.ToString(drTransferdata["pickup"]).Split('/')[3].Split(':')[i]);
                                                                    else
                                                                        FlightNoRoundTrip = (Convert.ToString(drTransferdata["pickup"]).Split('/')[3].Split(':')[i]);
                                                                }
                                                            }
                                                        }
                                                        // FlightNoRoundTrip = Convert.ToString(Convert.ToString(drTransferdata["pickup"]).Split('/')[3].Split(':')[1]);
                                                        if (Convert.ToString(drTransferdata["pickup"]).Split('/')[3].Contains("Flight Time"))
                                                        {
                                                            if (Convert.ToString(drTransferdata["pickup"]).Split('/')[3].Split(':').Length > 1)
                                                            {
                                                                for (int i = 1; i < Convert.ToString(drTransferdata["pickup"]).Split('/')[3].Split(':').Length; i++)
                                                                {
                                                                    if (!string.IsNullOrEmpty(FlightTimeRoundTrip))
                                                                        FlightTimeRoundTrip = FlightTimeRoundTrip + ":" + (Convert.ToString(drTransferdata["pickup"]).Split('/')[3].Split(':')[i]);
                                                                    else
                                                                        FlightTimeRoundTrip = (Convert.ToString(drTransferdata["pickup"]).Split('/')[3].Split(':')[i]);
                                                                }
                                                            }
                                                            // FlightTimeRoundTrip = Convert.ToString(Convert.ToString(drTransferdata["pickup"]).Split('/')[3].Split(':')[1]);
                                                        }

                                                    }
                                                }

                                            }
                                            catch (Exception ex)
                                            {
                                                _ = _exception.ErrorBooking(ex, Convert.ToString(AgentId));
                                            }
                                            TransferHtmlSupplierRoundTrip = TransferHtmlSupplierRoundTrip.Replace("#TransferBookingService", "Transfer");
                                            TransferHtmlSupplierRoundTrip = TransferHtmlSupplierRoundTrip.Replace("#TransferBookingStatus", Convert.ToString(drTransferdata["BookingStatus"]));
                                            TransferHtmlSupplierRoundTrip = TransferHtmlSupplierRoundTrip.Replace("#TransferName", Convert.ToString(drTransferdata["ServiceName"]));
                                            TransferHtmlSupplierRoundTrip = TransferHtmlSupplierRoundTrip.Replace("#TransferCityName", Convert.ToString(drTransferdata["CityName"]));
                                            TransferHtmlSupplierRoundTrip = TransferHtmlSupplierRoundTrip.Replace("#TransferType", Convert.ToString(drTransferdata["ServiceName"]));
                                            TransferHtmlSupplierRoundTrip = TransferHtmlSupplierRoundTrip.Replace("#FromLocation", Convert.ToString(ToLocation));
                                            TransferHtmlSupplierRoundTrip = TransferHtmlSupplierRoundTrip.Replace("#ToLocation", Convert.ToString(FromLocation));
                                            TransferHtmlSupplierRoundTrip = TransferHtmlSupplierRoundTrip.Replace("#TransferServiceType", "Transfer");
                                            TransferHtmlSupplierRoundTrip = TransferHtmlSupplierRoundTrip.Replace("#TransferServiceDate", Convert.ToString(drTransferdata["CheckOutDate"]));
                                            TransferHtmlSupplierRoundTrip = TransferHtmlSupplierRoundTrip.Replace("#TransferNoofAdult", Convert.ToString(drTransferdata["Adult"]));
                                            TransferHtmlSupplierRoundTrip = TransferHtmlSupplierRoundTrip.Replace("#TransferNoofChild", Convert.ToString(drTransferdata["Children"]));
                                            TransferHtmlSupplierRoundTrip = TransferHtmlSupplierRoundTrip.Replace("#TransferTotalPax", Convert.ToString(Convert.ToInt32(drTransferdata["Adult"]) + Convert.ToInt32(drTransferdata["Children"])));
                                            TransferHtmlSupplierRoundTrip = TransferHtmlSupplierRoundTrip.Replace("#VehicalType", Convert.ToString(drTransferdata["VehicleType"]));
                                            TransferHtmlSupplierRoundTrip = TransferHtmlSupplierRoundTrip.Replace("#FlightNo", FlightNoRoundTrip);
                                            TransferHtmlSupplierRoundTrip = TransferHtmlSupplierRoundTrip.Replace("#FlightTime", FlightTimeRoundTrip);
                                            TransferHtmlSupplierRoundTrip = TransferHtmlSupplierRoundTrip.Replace("#TransferRemark", "");
                                            // TransferHtmlMainSupplier += SupplierTransferDetailTemplate + TransferHtmlSupplier;
                                            TransferHtmlMainSupplier += TransferHtmlSupplierRoundTrip;
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        _ = _exception.ErrorBooking(ex, Convert.ToString(AgentId));
                                    }
                                    #endregion
                                    #endregion
                                    try
                                    {
                                        DataRow[] drCityTourSupplier = dsSupplierEmail.Tables[0].Select("ServiceType='Transfer' and BookingDetailId='" + Convert.ToString(drTransferdata["BookingDetailId"]) + "'");
                                        if (drCityTourSupplier.Length > 0)
                                        {
                                            SupplierToEmailId = Convert.ToString(drCityTourSupplier[0]["EmailId"]);

                                            try
                                            {
                                                DataTable dtMail1 = await GetEmailIdsBranchWise(Convert.ToInt32(AgentId), Convert.ToInt32(drTransferdata["CountryId"]));
                                                if (dtMail1.Rows.Count > 0)
                                                {
                                                    if (!String.IsNullOrEmpty(Convert.ToString(dtMail1.Rows[0]["BranchEmail"])))
                                                    {
                                                        if (!string.IsNullOrEmpty(SupplierCCId))
                                                            SupplierCCId = SupplierCCId + "," + Convert.ToString(dtMail1.Rows[0]["BranchEmail"]);
                                                        else
                                                            SupplierCCId = Convert.ToString(dtMail1.Rows[0]["BranchEmail"]);
                                                    }
                                                    if (!String.IsNullOrEmpty(Convert.ToString(dtMail1.Rows[0]["EmployeeEmailId"])))
                                                        SupplierFromId = Convert.ToString(dtMail1.Rows[0]["EmployeeEmailId"]);
                                                }
                                            }
                                            catch (Exception ex)
                                            {
                                                _ = _exception.ErrorBooking(ex, Convert.ToString(AgentId));
                                            }
                                            if (!string.IsNullOrEmpty(SupplierCCId))
                                                SupplierCCId = SupplierCCId + "," + SupplierFromId;
                                            else
                                                SupplierCCId = SupplierFromId;

                                            if (!string.IsNullOrEmpty(Convert.ToString(drCityTourSupplier[0]["CCEmailId"])))
                                            {
                                                SupplierCCId = SupplierCCId + "," + Convert.ToString(drCityTourSupplier[0]["CCEmailId"]);
                                            }
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        _ = _exception.ErrorBooking(ex, Convert.ToString(AgentId));
                                    }
                                }
                                if (string.IsNullOrEmpty(SupplierCommonDetailTemplate))
                                    SupplierCommonDetailTemplate = SupplierTransferDetailTemplate;
                            }
                            #endregion

                            SupplierCommonTemplate = SuppliercommonTemplate + SupplierCommonDetailTemplate + CityTourHtmlMainSupplier + TransferHtmlMainSupplier + MealHtmlMainSupplier + SupplierFooterTemplate;
                            emailList.Add(new Email_Details
                            {
                                FromId = SupplierFromId,
                                ToId = SupplierToEmailId,
                                CcId = SupplierCCId,
                                BccId = "",
                                Subject = SuppplierSubject,
                                EmailText = SupplierCommonTemplate,
                                EmailType = "Booking Mail Supplier",
                                ServiceType = "Booking Mail Supplier",
                                Website = Convert.ToString(HostName),
                                CompanyId = CompanyId,
                                IsRayna = 1,
                                UserHost = _keySettings.WLUserHost,
                                UserPassword = _keySettings.WLUserPassword,
                                UserPort = _keySettings.WLUserPort,
                                UserEmail = _keySettings.WLUserEmail,
                                CompanyName = _keySettings.WLUserCompanyName,

                                refno = strRefNo
                            });
                        }

                    }
                    #endregion

                    // commonTemplate = commonTemplate + HotelHtmlMain + TransferHtmlMain + CityTourHtmlMain + MealHtmlMain + FooterTemplate;
                    try
                    {
                        DataTable dtMail1 = await GetEmailIdsBranchWise(Convert.ToInt32(AgentId), countryId);
                        if (dtMail1.Rows.Count > 0)
                        {
                            if (!String.IsNullOrEmpty(Convert.ToString(dtMail1.Rows[0]["BranchEmail"])))
                                FromEmail = Convert.ToString(dtMail1.Rows[0]["BranchEmail"]);
                            if (!String.IsNullOrEmpty(Convert.ToString(dtMail1.Rows[0]["EmployeeEmailId"])))
                                EmpToEmailId = Convert.ToString(dtMail1.Rows[0]["EmployeeEmailId"]);
                        }
                    }
                    catch (Exception ex)
                    {
                        _ = _exception.ErrorBooking(ex, Convert.ToString(AgentId));
                    }
                    string Bccemail = string.Empty;
                    if (IstodayBooking == 1)
                    {
                        if (!string.IsNullOrEmpty(EmpToEmailId))
                            EmpToEmailId = EmpToEmailId + "," + _keySettings.support_mailid;
                        else
                            EmpToEmailId = _keySettings.support_mailid;
                    }
                    // Bccemail = "support@withinearth.com";
                    if (!string.IsNullOrEmpty(SubagentEmailId))
                    {
                        ToEmail = SubagentEmailId;
                        if (!string.IsNullOrEmpty(EmpToEmailId))
                            EmpToEmailId = EmpToEmailId + "," + AgentEmailId;
                        else
                            EmpToEmailId = AgentEmailId;
                    }
                    else if (!string.IsNullOrEmpty(SubUserEmailId))
                    {
                        ToEmail = SubUserEmailId;
                        if (!string.IsNullOrEmpty(EmpToEmailId))
                            EmpToEmailId = EmpToEmailId + "," + AgentEmailId;
                        else
                            EmpToEmailId = AgentEmailId;
                    }
                    else
                        ToEmail = AgentEmailId;

                    emailList.Add(new Email_Details
                    {
                        FromId = FromEmail,
                        ToId = ToEmail,
                        CcId = EmpToEmailId,
                        BccId = Bccemail,
                        Subject = Subject,
                        EmailText = FinalAgentTemplate,
                        EmailType = "Booking Mail",
                        ServiceType = "Booking Mail",
                        Website = Convert.ToString(HostName),
                        CompanyId = CompanyId,
                        IsRayna = 1,
                        UserHost = _keySettings.WLUserHost,
                        UserPassword = _keySettings.WLUserPassword,
                        UserPort = _keySettings.WLUserPort,
                        UserEmail = _keySettings.WLUserEmail,
                        CompanyName = _keySettings.WLUserCompanyName,
                        refno = strRefNo
                    });
                }
                await SaveMailSendingDetails(emailList, emailsendstatus);
            }

            catch (Exception ex)
            {
                //Common.Error(ex);
                _ = _exception.ErrorBooking(ex, Convert.ToString(AgentId));
            }
        }

        public async Task<DataSet> GetVoucher(int TemplateId)
        {

            try
            {
                SqlParameter[] para = new SqlParameter[]{
                    new SqlParameter("@TemplateId",TemplateId),
                 };

                DataSet ds = await _dlConnection.GetDataSet("GetTemplateVoucherWise", para);
                return ds;
            }
            catch (Exception ex)
            {
                _ = _exception.ErrorNew(ex, _LogAPI.Book);
            }
            return null;
        }

        public async Task<DataSet> GetVoucherListNew(string ReferenceNo, int BookingId, int agentId, int AmendId = 0, int CancelOldBooking = 0)
        {
            try
            {
                //string agentId = Common.getSession("UserId") == "" ? "0" : Common.getSession("UserId");

                SqlParameter[] para = new SqlParameter[]{
                    new SqlParameter("@ReferenceNo",ReferenceNo),
                    new SqlParameter("@Bookingid",BookingId),
                    new SqlParameter("@AgentId",agentId),
                    new SqlParameter("@AmendId",AmendId),

                      };
                if (agentId != 9576 || CancelOldBooking == 1)
                    return await _dlConnection.GetDataSet("GetShoppingTravelCart_Details_B2B_Mail_2025", para); // ADD ON 23-JUL-2025
                                                                                                                //return dLConnection.GetDataSet("GetShoppingTravelCart_Details_B2B_Mail", para); //cmt on 23-JUL-2025
                else
                    return await _dlConnection.GetDataSet("GetShoppingTravelCart_Details_B2B_Mail_Pruvo", para);

            }
            catch (Exception ex)
            {
                _ = _exception.ErrorNew(ex, _LogAPI.Book);
                return null;
            }

        }

        public async Task<DataSet> GetSupplierEmail(string RefNo, int BookingId)
        {
            try
            {

                SqlParameter[] para = new SqlParameter[]{
                    new SqlParameter("@RefNo",RefNo),
                     new SqlParameter("@BookingId",BookingId),
                      };

                return await _dlConnection.GetDataSet("getSupplierEmailId", para);
            }
            catch (Exception ex)
            {
                _ = _exception.ErrorNew(ex, _LogAPI.Book);
                return null;
            }
            finally
            {

            }
        }

        public async Task SendEmailToSupplier(string rerefenceNo = "", int TempleteId = 0, DataSet dsv = null, int AgentId = 0, string HotelDetailId = "", int CancelOldBooking = 0, string HostName = "")
        {
            try
            {
                if (dsv != null && dsv.Tables[14].Rows.Count > 0)
                {
                    DataSet mailbody = new DataSet();
                    List<Email_Details> emailListsupplier = new List<Email_Details>();
                    //DataSet dtemail = newvou.GetReservationDetails(rerefenceNo, HotelDetailId, "Confirmed");
                    mailbody = await GetVoucher(TempleteId);

                    string commonTemplate = string.Empty;
                    string fromEmailnew = _keySettings.FromEmailId.ToString();
                    string BranchEmailId = string.Empty;
                    if (mailbody.Tables[0].Rows.Count > 0)
                    {
                        commonTemplate = Convert.ToString(mailbody.Tables[0].Rows[0]["NotificationBody"]);
                    }


                    string Subject = mailbody.Tables[0].Rows[0]["Subject"].ToString();
                    string[] alltemplatedata = commonTemplate.Split(new String[] { "$$$$" }, StringSplitOptions.None);
                    string othhoteltemplate = alltemplatedata[0].ToString();
                    string xmlHotelTemplate = alltemplatedata[1].ToString();

                    string[] othHotelTemplatelst = othhoteltemplate.Split(new String[] { "%%%" }, StringSplitOptions.None);
                    string othHotelTemplateHeader = othHotelTemplatelst[0].ToString();
                    string othHotelTemplateBody = othHotelTemplatelst[1].ToString();
                    string othHotelTemplateFooter = othHotelTemplatelst[2].ToString();
                    //  string othHotelTemplateFooternew = othHotelTemplatelst[3].ToString();


                    string[] xmlHotelTemplatelst = xmlHotelTemplate.Split(new String[] { "%%%%" }, StringSplitOptions.None);
                    string xmlHotelTemplateHeader = xmlHotelTemplatelst[0].ToString();
                    string xmlHotelTemplateBody = xmlHotelTemplatelst[1].ToString();
                    string xmlHotelTemplateFooter = xmlHotelTemplatelst[2].ToString();

                    DataRow[] drHotel = dsv.Tables[14].Select("1=1");

                    #region Hotel
                    int status = 0;//@
                                   // ExManager.Exception.LogException(drHotel.Length + "Booking Error Mail length");
                    if (drHotel.Length > 0)
                    {
                        int TotalNoOfNights = 0;
                        int TotalNoofAdult = 0;
                        int TotalNoofChild = 0;
                        string RoomCategory = "";
                        foreach (DataRow drHoteldata in drHotel)
                        {
                            Subject = mailbody.Tables[0].Rows[0]["Subject"].ToString();
                            string HotelHtmlMain = string.Empty;
                            string HotelHtmlMainXml = string.Empty;
                            int countryId = 0;
                            int room = 0;
                            DataRow[] drhotelnew = dsv.Tables[0].Select("ServiceType='Hotel' and BookingId='" + Convert.ToString(drHoteldata["OnlineBookingMasterId"]) + "'");
                            string othHotelTemplateHeaderNew = string.Empty;
                            string othHotelTemplateFooterNew = string.Empty;
                            othHotelTemplateFooter = othHotelTemplatelst[2].ToString();
                            string RoomTemplate = string.Empty;
                            othHotelTemplateHeaderNew = othHotelTemplateHeader;
                            string xmlHotelTemplateHeaderNew = string.Empty;
                            xmlHotelTemplateHeaderNew = xmlHotelTemplateHeader;
                            decimal BookingTotalAmount = 0;
                            string ReservationEmail = string.Empty;
                            if (Convert.ToString(drhotelnew[0]["BookingStatus"]).ToLower() != "not confirmed")
                            {
                                if (Convert.ToString(drhotelnew[0]["Provider"]).ToLower() == "oth" || Convert.ToString(drhotelnew[0]["Provider"]).ToLower() == "withinearth" || Convert.ToString(drhotelnew[0]["Provider"]).ToLower() == "siteminder" || Convert.ToString(drhotelnew[0]["Provider"]).ToLower() == "travelclick" || Convert.ToString(drhotelnew[0]["Provider"]).ToLower() == "reseliva" || Convert.ToString(drhotelnew[0]["Provider"]).ToLower() == "hotelrunner" || Convert.ToString(drhotelnew[0]["Provider"]).ToLower() == "ratetiger" || Convert.ToString(drhotelnew[0]["Provider"]).ToLower() == "dedge" || Convert.ToString(drhotelnew[0]["Provider"]).ToLower() == "rategain" || Convert.ToString(drhotelnew[0]["Provider"]).ToLower() == "staah")
                                {
                                    othHotelTemplateHeaderNew = othHotelTemplateHeaderNew.Replace("#RefNo", rerefenceNo);
                                    othHotelTemplateHeaderNew = othHotelTemplateHeaderNew.Replace("#IssueDate", Convert.ToString(drhotelnew[0]["IssueDate"]));
                                    othHotelTemplateHeaderNew = othHotelTemplateHeaderNew.Replace("#GuestName", Convert.ToString(drhotelnew[0]["Name"]));
                                    othHotelTemplateHeaderNew = othHotelTemplateHeaderNew.Replace("#Nationality", Convert.ToString(drhotelnew[0]["Nationality"]));
                                    othHotelTemplateHeaderNew = othHotelTemplateHeaderNew.Replace("#HotelName", Convert.ToString(drhotelnew[0]["ServiceName"]));
                                    othHotelTemplateHeaderNew = othHotelTemplateHeaderNew.Replace("#TelePhoneNo", Convert.ToString(drhotelnew[0]["HotelContactNo"]));
                                    othHotelTemplateHeaderNew = othHotelTemplateHeaderNew.Replace("#EmailId", Convert.ToString(drhotelnew[0]["ReservationEmail"]));

                                    Subject = Subject.Replace("#RefNo", rerefenceNo).Replace("#GuestName", Convert.ToString(dsv.Tables[0].Rows[0]["Name"]));
                                    #region "for Oth"
                                    foreach (DataRow drHoteldatanew in drhotelnew)
                                    {
                                        if (Convert.ToString(drHoteldatanew["BookingStatus"]).ToLower() == "not confirmed")
                                        {
                                            continue;
                                        }
                                        if (countryId == 0)
                                            countryId = Convert.ToInt32(drHoteldatanew["CountryId"]);
                                        string othHotelTemplateBodyNew = string.Empty;
                                        othHotelTemplateBodyNew = othHotelTemplateBody;
                                        room += 1;
                                        // BookingTotalAmount += Convert.ToDecimal(drHoteldatanew["TotalAmount"]);
                                        TotalNoOfNights += Convert.ToInt32(drHoteldatanew["NoOfNights"]);
                                        TotalNoofAdult += Convert.ToInt32(drHoteldatanew["Adult"]);
                                        TotalNoofChild += Convert.ToInt32(drHoteldatanew["Children"]);
                                        RoomCategory += Convert.ToString(drHoteldatanew["RoomType"]) + ",";
                                        ReservationEmail = Convert.ToString(drHoteldatanew["ReservationEmail"]);
                                        //DataRow[] drpassenger = dsv.Tables[1].Select("BookingDetailId='" + Convert.ToString(drHoteldatanew["BookingDetailId"]) + "'");

                                        if (Convert.ToString(drHoteldatanew["BookingStatus"]) == "On Request")
                                        {
                                            othHotelTemplateHeaderNew = othHotelTemplateHeaderNew.Replace("#BookingStatus", "On Request");
                                        }
                                        else if (Convert.ToString(drHoteldatanew["BookingStatus"]) == "Vouchered")
                                        {
                                            othHotelTemplateHeaderNew = othHotelTemplateHeaderNew.Replace("#BookingStatus", "Vouchered");
                                        }
                                        else if (Convert.ToString(drHoteldatanew["BookingStatus"]) == "Not Confirmed")//----------
                                        {
                                            othHotelTemplateHeaderNew = othHotelTemplateHeaderNew.Replace("#BookingStatus", "Not Confirmed");
                                        }
                                        else if (Convert.ToString(drHoteldatanew["BookingStatus"]) == "Cancelled" || Convert.ToString(drHoteldatanew["BookingStatus"]) == "Cancel Charge" || CancelOldBooking == 1)//-------------------
                                        {
                                            othHotelTemplateHeaderNew = othHotelTemplateHeaderNew.Replace("#BookingStatus", "REQUEST TO CANCEL BOOKING");
                                            Subject = Subject.Replace("#Title", "Booking Cancellation");
                                        }
                                        else
                                        {
                                            othHotelTemplateHeaderNew = othHotelTemplateHeaderNew.Replace("#BookingStatus", "Confirmed");
                                        }
                                        Subject = Subject.Replace("#Title", "New Booking");
                                        Subject = Subject.Replace("#OwnNo", "");

                                        if (Convert.ToString(drhotelnew[0]["Provider"]).ToLower() == "siteminder")
                                            othHotelTemplateHeaderNew = othHotelTemplateHeaderNew.Replace("<p id=\"directct\"></p>", "<p>(SiteMinder)</p>");
                                        if (Convert.ToString(drhotelnew[0]["Provider"]).ToLower() == "hotelrunner")
                                            othHotelTemplateHeaderNew = othHotelTemplateHeaderNew.Replace("<p id=\"directct\"></p>", "<p>(HotelRunner)</p>");
                                        if (Convert.ToString(drhotelnew[0]["Provider"]).ToLower() == "reseliva")
                                            othHotelTemplateHeaderNew = othHotelTemplateHeaderNew.Replace("<p id=\"directct\"></p>", "<p>(Reseliva)</p>");
                                        if (Convert.ToString(drhotelnew[0]["Provider"]).ToLower() == "ratetiger")
                                            othHotelTemplateHeaderNew = othHotelTemplateHeaderNew.Replace("<p id=\"directct\"></p>", "<p>(RateTiger)</p>");
                                        if (Convert.ToString(drhotelnew[0]["Provider"]).ToLower() == "rategain")
                                            othHotelTemplateHeaderNew = othHotelTemplateHeaderNew.Replace("<p id=\"directct\"></p>", "<p>(RateGain)</p>");
                                        if (Convert.ToString(drhotelnew[0]["Provider"]).ToLower() == "dedge")
                                            othHotelTemplateHeaderNew = othHotelTemplateHeaderNew.Replace("<p id=\"directct\"></p>", "<p>(Dedge)</p>");
                                        if (Convert.ToString(drhotelnew[0]["Provider"]).ToLower() == "staah")
                                            othHotelTemplateHeaderNew = othHotelTemplateHeaderNew.Replace("<p id=\"directct\"></p>", "<p>(Staah)</p>");
                                        if (Convert.ToString(drhotelnew[0]["Provider"]).ToLower() == "travelclick")
                                            othHotelTemplateHeaderNew = othHotelTemplateHeaderNew.Replace("<p id=\"directct\"></p>", "<p>(TravelClick)</p>");

                                        othHotelTemplateBodyNew = othHotelTemplateBodyNew.Replace("#RoomNo", Convert.ToString(room));
                                        othHotelTemplateBodyNew = othHotelTemplateBodyNew.Replace("#FromDate", Convert.ToString(drHoteldatanew["TravelDate"]));
                                        othHotelTemplateBodyNew = othHotelTemplateBodyNew.Replace("#ToDate", Convert.ToString(drHoteldatanew["CheckOutDate"]));
                                        othHotelTemplateBodyNew = othHotelTemplateBodyNew.Replace("#NoofNights", Convert.ToString(drHoteldatanew["NoOfNights"]));
                                        //othHotelTemplateBodyNew = othHotelTemplateBodyNew.Replace("#RoomType", Convert.ToString(drHoteldatanew["RoomType"]));  // Cmt on 23-JUL-2025
                                        othHotelTemplateBodyNew = othHotelTemplateBodyNew.Replace("#RoomType", Convert.ToString(drHoteldatanew["OriginalRoomType"])); // Add ON 23-JUL-2025 for send OTH Original room to Hotel only
                                        othHotelTemplateBodyNew = othHotelTemplateBodyNew.Replace("#Board", Convert.ToString(drHoteldatanew["Board"]));
                                        othHotelTemplateBodyNew = othHotelTemplateBodyNew.Replace("#TotalAdult", Convert.ToString(drHoteldatanew["Adult"]));
                                        othHotelTemplateBodyNew = othHotelTemplateBodyNew.Replace("#TotalChild", Convert.ToString(drHoteldatanew["Children"]));
                                        othHotelTemplateBodyNew = othHotelTemplateBodyNew.Replace("#Allotment", Convert.ToString(drHoteldatanew["BookingStatus"]) == "On Request" ? "N" : "Y");
                                        // othHotelTemplateBodyNew = othHotelTemplateBodyNew.Replace("#TotalPrice", Convert.ToString(drHoteldatanew["CurrencyId"]) + " " + Convert.ToString(drHoteldatanew["TotalAmount"]));
                                        othHotelTemplateBodyNew = othHotelTemplateBodyNew.Replace("#RoomAdult", Convert.ToString(drHoteldatanew["Adult"]));
                                        othHotelTemplateBodyNew = othHotelTemplateBodyNew.Replace("#RoomChild", Convert.ToString(drHoteldatanew["Children"]));
                                        if (Convert.ToString(drHoteldatanew["BookingStatus"]) == "Cancelled" || Convert.ToString(drHoteldatanew["BookingStatus"]) == "Cancel Charge" || CancelOldBooking == 1)
                                            othHotelTemplateBodyNew = othHotelTemplateBodyNew.Replace("#ConfirmationNo", "Confirmation No: " + Convert.ToString(drHoteldatanew["PromoCode"]));
                                        else
                                            othHotelTemplateBodyNew = othHotelTemplateBodyNew.Replace("#ConfirmationNo", "Promo Code: " + Convert.ToString(drHoteldatanew["PromoCode"]));
                                        string guestnamelist = string.Empty;


                                        DataRow[] hotelAdult = dsv.Tables[1].Select("BookingDetailId='" + Convert.ToString(drHoteldatanew["BookingDetailId"]) + "' and PassangerType='Adult'");
                                        DataRow[] hotelChild = dsv.Tables[1].Select("BookingDetailId='" + Convert.ToString(drHoteldatanew["BookingDetailId"]) + "' and PassangerType='Child'");
                                        int no = 0;
                                        if (hotelAdult.GetLength(0) > 0)
                                        {
                                            foreach (DataRow drHotelAdult in hotelAdult)
                                            {
                                                no += 1;
                                                guestnamelist += "Guest Name " + no + " " + Convert.ToString(drHotelAdult["Name"]) + " - (Adult)<br/>";
                                            }
                                        }
                                        int noc = 0;
                                        if (hotelChild.GetLength(0) > 0)
                                        {
                                            foreach (DataRow drhotelChild in hotelChild)
                                            {
                                                noc += 1;
                                                guestnamelist += "Child Name " + noc + " " + Convert.ToString(drhotelChild["Name"]) + " - (Child," + Convert.ToString(drhotelChild["ChildAge"]) + " Years)<br/>";
                                            }
                                        }
                                        othHotelTemplateBodyNew = othHotelTemplateBodyNew.Replace("#GuestNameList", guestnamelist);
                                        DataRow[] hotelRateBreak = dsv.Tables[10].Select("HotelDetailId='" + Convert.ToString(drHoteldatanew["BookingDetailId"]) + "'");
                                        if (hotelRateBreak.GetLength(0) > 0)
                                        {
                                            string Sunday = string.Empty;
                                            string Monday = string.Empty;
                                            string Tuesday = string.Empty;
                                            string Wednesday = string.Empty;
                                            string Thursday = string.Empty;
                                            string Friday = string.Empty;
                                            string Saturday = string.Empty;
                                            try
                                            {
                                                decimal TotalRoomAmount = 0;
                                                foreach (DataRow drhotelRateBreak in hotelRateBreak)
                                                {
                                                    switch (Convert.ToString(drhotelRateBreak["DateOfWeek"]).ToLower())
                                                    {
                                                        case "sun":
                                                            if (Convert.ToString(drhotelRateBreak["RateType"]) != "A Rate")
                                                            {
                                                                Sunday = Sunday + "<br/>" + Convert.ToString(drhotelRateBreak["RateType"]) + " - " + Convert.ToString(drhotelRateBreak["CurrencyShortName"]) + " " + Convert.ToString(drhotelRateBreak["Rate"]);
                                                            }
                                                            else
                                                            {
                                                                Sunday = Sunday + Convert.ToString(drhotelRateBreak["CurrencyShortName"]) + " " + Convert.ToString(drhotelRateBreak["Rate"]);
                                                            }
                                                            TotalRoomAmount += Convert.ToDecimal(drhotelRateBreak["Rate"]);
                                                            break;
                                                        case "mon":
                                                            if (Convert.ToString(drhotelRateBreak["RateType"]) != "A Rate")
                                                            {
                                                                Monday = Monday + "<br/>" + Convert.ToString(drhotelRateBreak["RateType"]) + " - " + Convert.ToString(drhotelRateBreak["CurrencyShortName"]) + " " + Convert.ToString(drhotelRateBreak["Rate"]);
                                                            }
                                                            else
                                                            {
                                                                Monday = Monday + Convert.ToString(drhotelRateBreak["CurrencyShortName"]) + " " + Convert.ToString(drhotelRateBreak["Rate"]);
                                                            }
                                                            TotalRoomAmount += Convert.ToDecimal(drhotelRateBreak["Rate"]);
                                                            break;
                                                        case "tue":
                                                            if (Convert.ToString(drhotelRateBreak["RateType"]) != "A Rate")
                                                            {
                                                                Tuesday = Tuesday + "<br/>" + Convert.ToString(drhotelRateBreak["RateType"]) + " - " + Convert.ToString(drhotelRateBreak["CurrencyShortName"]) + " " + Convert.ToString(drhotelRateBreak["Rate"]);
                                                            }
                                                            else
                                                            {
                                                                Tuesday = Tuesday + Convert.ToString(drhotelRateBreak["CurrencyShortName"]) + " " + Convert.ToString(drhotelRateBreak["Rate"]);
                                                            }
                                                            TotalRoomAmount += Convert.ToDecimal(drhotelRateBreak["Rate"]);
                                                            break;
                                                        case "wed":
                                                            if (Convert.ToString(drhotelRateBreak["RateType"]) != "A Rate")
                                                            {
                                                                Wednesday = Wednesday + "<br/>" + Convert.ToString(drhotelRateBreak["RateType"]) + " - " + Convert.ToString(drhotelRateBreak["CurrencyShortName"]) + " " + Convert.ToString(drhotelRateBreak["Rate"]);
                                                            }
                                                            else
                                                            {
                                                                Wednesday = Wednesday + Convert.ToString(drhotelRateBreak["CurrencyShortName"]) + " " + Convert.ToString(drhotelRateBreak["Rate"]);
                                                            }
                                                            TotalRoomAmount += Convert.ToDecimal(drhotelRateBreak["Rate"]);
                                                            break;
                                                        case "thu":
                                                            if (Convert.ToString(drhotelRateBreak["RateType"]) != "A Rate")
                                                            {
                                                                Thursday = Thursday + "<br/>" + Convert.ToString(drhotelRateBreak["RateType"]) + " - " + Convert.ToString(drhotelRateBreak["CurrencyShortName"]) + " " + Convert.ToString(drhotelRateBreak["Rate"]);
                                                            }
                                                            else
                                                            {
                                                                Thursday = Thursday + Convert.ToString(drhotelRateBreak["CurrencyShortName"]) + " " + Convert.ToString(drhotelRateBreak["Rate"]);
                                                            }
                                                            TotalRoomAmount += Convert.ToDecimal(drhotelRateBreak["Rate"]);
                                                            break;
                                                        case "fri":
                                                            if (Convert.ToString(drhotelRateBreak["RateType"]) != "A Rate")
                                                            {
                                                                Friday = Friday + "<br/>" + Convert.ToString(drhotelRateBreak["RateType"]) + " - " + Convert.ToString(drhotelRateBreak["CurrencyShortName"]) + " " + Convert.ToString(drhotelRateBreak["Rate"]);
                                                            }
                                                            else
                                                            {
                                                                Friday = Friday + Convert.ToString(drhotelRateBreak["CurrencyShortName"]) + " " + Convert.ToString(drhotelRateBreak["Rate"]);
                                                            }
                                                            TotalRoomAmount += Convert.ToDecimal(drhotelRateBreak["Rate"]);
                                                            break;
                                                        case "sat":
                                                            if (Convert.ToString(drhotelRateBreak["RateType"]) != "A Rate")
                                                            {
                                                                Saturday = Saturday + "<br/>" + Convert.ToString(drhotelRateBreak["RateType"]) + " - " + Convert.ToString(drhotelRateBreak["CurrencyShortName"]) + " " + Convert.ToString(drhotelRateBreak["Rate"]);
                                                            }
                                                            else
                                                            {
                                                                Saturday = Saturday + Convert.ToString(drhotelRateBreak["CurrencyShortName"]) + " " + Convert.ToString(drhotelRateBreak["Rate"]);
                                                            }
                                                            TotalRoomAmount += Convert.ToDecimal(drhotelRateBreak["Rate"]);
                                                            break;
                                                    }
                                                }
                                                BookingTotalAmount += TotalRoomAmount;
                                                othHotelTemplateBodyNew = othHotelTemplateBodyNew.Replace("#TotalPrice", Convert.ToString(drHoteldatanew["CurrencyId"]) + " " + Convert.ToString(TotalRoomAmount));
                                                othHotelTemplateBodyNew = othHotelTemplateBodyNew.Replace("#Sun", Sunday);
                                                othHotelTemplateBodyNew = othHotelTemplateBodyNew.Replace("#Mon", Monday);
                                                othHotelTemplateBodyNew = othHotelTemplateBodyNew.Replace("#Tue", Tuesday);
                                                othHotelTemplateBodyNew = othHotelTemplateBodyNew.Replace("#Wed", Wednesday);
                                                othHotelTemplateBodyNew = othHotelTemplateBodyNew.Replace("#Thu", Thursday);
                                                othHotelTemplateBodyNew = othHotelTemplateBodyNew.Replace("#Fri", Friday);
                                                othHotelTemplateBodyNew = othHotelTemplateBodyNew.Replace("#Sat", Saturday);
                                            }
                                            catch (Exception)
                                            {
                                                othHotelTemplateBodyNew = othHotelTemplateBodyNew.Replace("#Sun", "");
                                                othHotelTemplateBodyNew = othHotelTemplateBodyNew.Replace("#Mon", "");
                                                othHotelTemplateBodyNew = othHotelTemplateBodyNew.Replace("#Tue", "");
                                                othHotelTemplateBodyNew = othHotelTemplateBodyNew.Replace("#Wed", "");
                                                othHotelTemplateBodyNew = othHotelTemplateBodyNew.Replace("#Thu", "");
                                                othHotelTemplateBodyNew = othHotelTemplateBodyNew.Replace("#Fri", "");
                                                othHotelTemplateBodyNew = othHotelTemplateBodyNew.Replace("#Sat", "");
                                            }
                                        }
                                        else
                                        {
                                            othHotelTemplateBodyNew = othHotelTemplateBodyNew.Replace("#TotalPrice", Convert.ToString(drHoteldatanew["CurrencyId"]) + " " + Convert.ToString(drHoteldatanew["TotalAmount"]));
                                            othHotelTemplateBodyNew = othHotelTemplateBodyNew.Replace("#Sun", "");
                                            othHotelTemplateBodyNew = othHotelTemplateBodyNew.Replace("#Mon", "");
                                            othHotelTemplateBodyNew = othHotelTemplateBodyNew.Replace("#Tue", "");
                                            othHotelTemplateBodyNew = othHotelTemplateBodyNew.Replace("#Wed", "");
                                            othHotelTemplateBodyNew = othHotelTemplateBodyNew.Replace("#Thu", "");
                                            othHotelTemplateBodyNew = othHotelTemplateBodyNew.Replace("#Fri", "");
                                            othHotelTemplateBodyNew = othHotelTemplateBodyNew.Replace("#Sat", "");
                                        }
                                        String Token = drHoteldatanew["BookingId"] + "$|$" + Convert.ToString(drHoteldatanew["ReferenceNo"]) + "$|$" + Convert.ToString(drHoteldatanew["BookingDetailId"]) + "$|$" + Convert.ToString(drHoteldatanew["Provider"]);
                                        var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(Token);
                                        String TOkenEcode = System.Convert.ToBase64String(plainTextBytes);

                                        //string DynamicURL = ConfigurationManager.AppSettings["website_url"] + "/HotelConfirmationForSupplierPVT?htlcnftoken=" + TOkenEcode;
                                        string DynamicURL = _keySettings.website_url_B2Bbh + "/HotelConfirmationForSupplierPVT?htlcnftoken=" + TOkenEcode;//Added on 23-JUL-2025
                                        if (Convert.ToString(drHoteldatanew["BookingStatus"]) == "Cancelled" || Convert.ToString(drHoteldatanew["BookingStatus"]) == "Cancel Charge" || CancelOldBooking == 1)
                                            othHotelTemplateBodyNew = othHotelTemplateBodyNew.Replace("#UpdatexmlConfirmationUrl", "");
                                        else
                                            othHotelTemplateBodyNew = othHotelTemplateBodyNew.Replace("#UpdatexmlConfirmationUrl", "<a href =" + DynamicURL + " style=\"color:blue; font-weight:bold;\" >Update Confirmation number here </a>");
                                        RoomTemplate += othHotelTemplateBodyNew;

                                        if (room == drhotelnew.Length)
                                        {
                                            string remark = "-";
                                            othHotelTemplateFooter = othHotelTemplateFooter.Replace("#TotalBookingAmount", Convert.ToString(drHoteldatanew["CurrencyId"]) + " " + Convert.ToString(BookingTotalAmount));
                                            try
                                            {

                                                if (!string.IsNullOrEmpty(Convert.ToString(drHoteldatanew["Pickup"])))
                                                {
                                                    remark = Convert.ToString(Convert.ToString(drHoteldatanew["Pickup"]).Split(new String[] { "/" }, StringSplitOptions.None)[0].Split(new String[] { ":" }, StringSplitOptions.None)[1]);
                                                    remark = remark.Replace("$$", ",");
                                                }
                                            }
                                            catch (Exception ex)
                                            {
                                                _ = _exception.ErrorNew(ex, _LogAPI.Book);
                                            }
                                            othHotelTemplateFooter = othHotelTemplateFooter.Replace("#Remark", remark);
                                        }
                                    }
                                    //othHotelTemplateFooter = othHotelTemplateFooter.Replace("#TotalBookingAmount", Convert.ToString(drHoteldatanew["CurrencyId"]) + " " + Convert.ToString(BookingTotalAmount));

                                    string apisoURLstring = string.Empty;
                                    try
                                    {
                                        apisoURLstring = await getApisoURL_string(Convert.ToInt32(drHoteldata["OnlineBookingMasterId"]));
                                    }
                                    catch (Exception ex)
                                    {
                                        //ExManager.Exception.LogException("VCN String creation error" + ex.ToString());
                                        _ = _exception.ErrorBooking(ex, "");
                                    }



                                    HotelHtmlMain += othHotelTemplateHeaderNew + RoomTemplate + apisoURLstring + othHotelTemplateFooter;
                                    #endregion


                                    // string Subject = "Booking " + Convert.ToString(row["BookingStatus"]) == "On Request" ? "On Request " : "Confirmed" + HttpContext.Current.Session["WLUserCompanyName"] + " : " + Convert.ToString(rerefenceNo);
                                    List<string> tomaillist = new List<string>();
                                    //const string MatchEmailPattern =
                                    //                         @"(([\w-]+\.)+[\w-]+|([a-zA-Z]{1}|[\w-]{2,}))@"
                                    //                         + @"((([0-1]?[0-9]{1,2}|25[0-5]|2[0-4][0-9])\.([0-1]?[0-9]{1,2}|25[0-5]|2[0-4][0-9])\."
                                    //                            + @"([0-1]?[0-9]{1,2}|25[0-5]|2[0-4][0-9])\.([0-1]?[0-9]{1,2}|25[0-5]|2[0-4][0-9])){1}|"
                                    //                         + @"([a-zA-Z]+[\w-]+\.)+[a-zA-Z]{2,4})";
                                    const string MatchEmailPattern = @"[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*@(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?";
                                    Regex rx = new Regex(MatchEmailPattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
                                    if (string.IsNullOrEmpty(ReservationEmail))
                                        ReservationEmail = _keySettings.noemailfound;
                                    MatchCollection matches = rx.Matches(ReservationEmail);
                                    int noOfMatches = matches.Count;

                                    foreach (Match match in matches)
                                    {
                                        tomaillist.Add(match.Value.ToString());
                                    }
                                    var toemailliststring = string.Empty;

                                    if (tomaillist.Count > 0)
                                    {
                                        toemailliststring = string.Join(",", tomaillist);
                                    }
                                    try
                                    {
                                        DataTable dtMail1 = await GetEmailIdsBranchWise(Convert.ToInt32(AgentId), countryId);

                                        if (dtMail1.Rows.Count > 0)
                                        {
                                            if (!String.IsNullOrEmpty(Convert.ToString(dtMail1.Rows[0]["BranchEmail"])))
                                                BranchEmailId = Convert.ToString(dtMail1.Rows[0]["BranchEmail"]);
                                            if (!String.IsNullOrEmpty(Convert.ToString(dtMail1.Rows[0]["EmployeeEmailId"])))
                                                fromEmailnew = Convert.ToString(dtMail1.Rows[0]["EmployeeEmailId"]);
                                            // ExManager.Exception.LogException("HotelBooking Supplier mail before=" + countryId + "BranchEmailId=" + BranchEmailId + "fromEmailnew=" + fromEmailnew);
                                        }
                                        //   ExManager.Exception.LogException("HotelBooking Supplier mail=" + countryId + "BranchEmailId=" + BranchEmailId + "fromEmailnew=" + fromEmailnew);
                                    }
                                    catch (Exception ex)
                                    {
                                        _ = _exception.ErrorNew(ex, _LogAPI.Book); //ExManager.Exception.LogException(ex.ToString() + "GetEmailIdsBranchWise");
                                    }
                                    if (!string.IsNullOrEmpty(BranchEmailId))
                                        BranchEmailId = BranchEmailId + "," + fromEmailnew;
                                    else
                                        BranchEmailId = fromEmailnew;

                                    try
                                    {
                                        if (toemailliststring.Contains("aymankadry@withinearth.com") == false) //#20-08-2025
                                        {
                                            toemailliststring = toemailliststring + ",aymankadry@withinearth.com"; //#20-08-2025
                                        }
                                    }
                                    catch (Exception)
                                    {

                                    }


                                    emailListsupplier.Add(new Email_Details
                                    {
                                        FromId = fromEmailnew,
                                        ToId = toemailliststring,
                                        CcId = BranchEmailId,
                                        BccId = "",
                                        Subject = Subject,
                                        EmailText = HotelHtmlMain,
                                        EmailType = "Booking Mail",// _" + (AgentId == 1899 ? "1899" : "0"),
                                        ServiceType = "Hotel",
                                        Website = HostName, //need to add host
                                        CompanyId = 1,// Convert.ToInt32(System.Web.HttpContext.Current.Session["CompanyId"]),
                                        IsRayna = 1,// Convert.ToInt32(Common.getSession("IsRayna") == "true" ? 1 : 0),
                                        UserHost = _keySettings.WLUserHost,
                                        UserPassword = _keySettings.WLUserPassword,
                                        UserPort = _keySettings.WLUserPort,
                                        UserEmail = _keySettings.WLUserEmail,
                                        CompanyName = _keySettings.WLUserCompanyName,
                                        refno = rerefenceNo
                                    });
                                    //Common.SaveMailSendingDetails(emailListsupplier, 0);
                                }
                                else
                                {
                                    status = (AgentId == 1899 ? 1 : 0);//@ for EGHT Agent Mail is disable from WE API and Send it from Travab

                                    if (Convert.ToString(drhotelnew[0]["Provider"]).ToLower() != "intratour")
                                    {
                                        int IsCancellMail = 0;
                                        Subject = Subject.Replace("#Title", "Request For Confirmation No.");
                                        Subject = Subject.Replace("#OwnNo", rerefenceNo);
                                        Subject = Subject.Replace("#GuestName", Convert.ToString(dsv.Tables[0].Rows[0]["Name"]));
                                        string suprefno = string.Empty;
                                        #region "for xml"
                                        foreach (DataRow drHoteldatanew in drhotelnew)
                                        {
                                            if (Convert.ToString(drHoteldatanew["BookingStatus"]).ToLower() == "not confirmed")
                                            {
                                                continue;
                                            }
                                            suprefno += Convert.ToString(drHoteldatanew["SupplierRefNo"]) + ",";
                                            if (countryId == 0)
                                                countryId = Convert.ToInt32(drHoteldatanew["CountryId"]);
                                            string xmlHotelTemplateBodyNew = string.Empty;
                                            xmlHotelTemplateBodyNew = xmlHotelTemplateBody;
                                            room += 1;
                                            ReservationEmail = Convert.ToString(drHoteldatanew["ReservationEmail"]);
                                            xmlHotelTemplateHeaderNew = xmlHotelTemplateHeaderNew.Replace("#HotelName", Convert.ToString(drHoteldatanew["ServiceName"]));
                                            xmlHotelTemplateHeaderNew = xmlHotelTemplateHeaderNew.Replace("#HotelName", Convert.ToString(drHoteldatanew["ServiceName"]));
                                            xmlHotelTemplateHeaderNew = xmlHotelTemplateHeaderNew.Replace("#TelePhoneNo", Convert.ToString(drHoteldatanew["HotelContactNo"]));
                                            xmlHotelTemplateHeaderNew = xmlHotelTemplateHeaderNew.Replace("#EmailId", Convert.ToString(drHoteldatanew["ReservationEmail"]));

                                            if (Convert.ToString(drHoteldatanew["BookingStatus"]) == "Cancelled" || Convert.ToString(drHoteldatanew["BookingStatus"]) == "Cancel Charge")
                                            {
                                                IsCancellMail = 1;
                                                xmlHotelTemplateHeaderNew = xmlHotelTemplateHeaderNew.Replace("#Title", "REQUEST FOR HOTEL CANCELLATION");
                                            }
                                            else
                                                xmlHotelTemplateHeaderNew = xmlHotelTemplateHeaderNew.Replace("#Title", "REQUEST FOR HOTEL CONFIRMATION N.O");
                                            xmlHotelTemplateHeaderNew = xmlHotelTemplateHeaderNew.Replace("#SupplierName", Convert.ToString(drHoteldatanew["Provider"]));
                                            xmlHotelTemplateBodyNew = xmlHotelTemplateBodyNew.Replace("#SupplierRefNo", Convert.ToString(drHoteldatanew["SupplierRefNo"]));
                                            xmlHotelTemplateBodyNew = xmlHotelTemplateBodyNew.Replace("#GuestName", Convert.ToString(drHoteldatanew["Name"]));
                                            xmlHotelTemplateBodyNew = xmlHotelTemplateBodyNew.Replace("#Nationality", Convert.ToString(drHoteldatanew["Nationality"]));
                                            xmlHotelTemplateBodyNew = xmlHotelTemplateBodyNew.Replace("#Supplier", Convert.ToString(drHoteldatanew["Provider"]));
                                            xmlHotelTemplateBodyNew = xmlHotelTemplateBodyNew.Replace("#CheckinDate", Convert.ToString(drHoteldatanew["TravelDate"]));
                                            xmlHotelTemplateBodyNew = xmlHotelTemplateBodyNew.Replace("#CheckoutDate", Convert.ToString(drHoteldatanew["CheckoutDate"]));
                                            xmlHotelTemplateBodyNew = xmlHotelTemplateBodyNew.Replace("#RoomNo", Convert.ToString(drHoteldatanew["NoOfRooms"]));
                                            xmlHotelTemplateBodyNew = xmlHotelTemplateBodyNew.Replace("#Nights", Convert.ToString(drHoteldatanew["NoOfNights"]));
                                            xmlHotelTemplateBodyNew = xmlHotelTemplateBodyNew.Replace("#NoofAdult", Convert.ToString(drHoteldatanew["Adult"]));
                                            xmlHotelTemplateBodyNew = xmlHotelTemplateBodyNew.Replace("#NoofChild", Convert.ToString(drHoteldatanew["Children"]));
                                            //xmlHotelTemplateBodyNew = xmlHotelTemplateBodyNew.Replace("#RoomType", Convert.ToString(drHoteldatanew["RoomType"]));
                                            xmlHotelTemplateBodyNew = xmlHotelTemplateBodyNew.Replace("#RoomType", Convert.ToString(drHoteldatanew["RoomType"]));

                                            String Token = drHoteldatanew["BookingId"] + "$|$" + Convert.ToString(drHoteldatanew["ReferenceNo"]) + "$|$" + Convert.ToString(drHoteldatanew["BookingDetailId"]) + "$|$" + Convert.ToString(drHoteldatanew["Provider"]);
                                            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(Token);
                                            String TOkenEcode = System.Convert.ToBase64String(plainTextBytes);

                                            //string DynamicURL = ConfigurationManager.AppSettings["website_url"] + "/HotelConfirmationForSupplierPVT?htlcnftoken=" + TOkenEcode;
                                            string DynamicURL = _keySettings.website_url_B2Bbh + "/HotelConfirmationForSupplierPVT?htlcnftoken=" + TOkenEcode; //Added on 23-JUL-2025
                                            if (Convert.ToString(drHoteldatanew["BookingStatus"]) == "Cancelled" || Convert.ToString(drHoteldatanew["BookingStatus"]) == "Cancel Charge")
                                                xmlHotelTemplateBodyNew = xmlHotelTemplateBodyNew.Replace("#UpdatexmlConfirmationUrl", "");
                                            else
                                                xmlHotelTemplateBodyNew = xmlHotelTemplateBodyNew.Replace("#UpdatexmlConfirmationUrl", "<a href =" + DynamicURL + " style=\"color:blue; font-weight:bold;\" >Update Confirmation number here </a>");
                                            RoomTemplate += xmlHotelTemplateBodyNew;
                                        }
                                        Subject = Subject.Replace("#RefNo", suprefno);



                                        string apisoURLstring = string.Empty;
                                        try
                                        {
                                            apisoURLstring = await getApisoURL_string(Convert.ToInt32(drHoteldata["OnlineBookingMasterId"]));
                                        }
                                        catch (Exception ex)
                                        {
                                            //ExManager.Exception.LogException("VCN String creation error" + ex.ToString());
                                            _ = _exception.ErrorBooking(ex, "");
                                        }


                                        HotelHtmlMainXml += xmlHotelTemplateHeaderNew + RoomTemplate + apisoURLstring + xmlHotelTemplateFooter;
                                        #endregion

                                        List<string> tomaillist = new List<string>();
                                        //const string MatchEmailPattern =
                                        //                         @"(([\w-]+\.)+[\w-]+|([a-zA-Z]{1}|[\w-]{2,}))@"
                                        //                         + @"((([0-1]?[0-9]{1,2}|25[0-5]|2[0-4][0-9])\.([0-1]?[0-9]{1,2}|25[0-5]|2[0-4][0-9])\."
                                        //                            + @"([0-1]?[0-9]{1,2}|25[0-5]|2[0-4][0-9])\.([0-1]?[0-9]{1,2}|25[0-5]|2[0-4][0-9])){1}|"
                                        //                         + @"([a-zA-Z]+[\w-]+\.)+[a-zA-Z]{2,4})";
                                        const string MatchEmailPattern = @"[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*@(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?";
                                        Regex rx = new Regex(MatchEmailPattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
                                        if (string.IsNullOrEmpty(ReservationEmail))
                                            ReservationEmail = _keySettings.noemailfound;
                                        MatchCollection matches = rx.Matches(ReservationEmail);
                                        int noOfMatches = matches.Count;
                                        foreach (Match match in matches)
                                        {
                                            tomaillist.Add(match.Value.ToString());
                                        }
                                        var toemailliststring = string.Empty;
                                        if (tomaillist.Count > 0)
                                        {
                                            toemailliststring = string.Join(",", tomaillist);
                                        }
                                        try
                                        {
                                            DataTable dtMail1 = await GetEmailIdsBranchWise(AgentId, countryId);
                                            if (dtMail1.Rows.Count > 0)
                                            {
                                                if (!String.IsNullOrEmpty(Convert.ToString(dtMail1.Rows[0]["BranchEmail"])))
                                                    BranchEmailId = Convert.ToString(dtMail1.Rows[0]["BranchEmail"]);
                                                if (!String.IsNullOrEmpty(Convert.ToString(dtMail1.Rows[0]["EmployeeEmailId"])))
                                                    fromEmailnew = Convert.ToString(dtMail1.Rows[0]["EmployeeEmailId"]);
                                                // ExManager.Exception.LogException("HotelBooking Supplier xml mail before=" + countryId + "BranchEmailId=" + BranchEmailId + "fromEmailnew=" + fromEmailnew);
                                            }
                                            // ExManager.Exception.LogException("HotelBooking Supplier xml mail=" + countryId + "BranchEmailId=" + BranchEmailId + "fromEmailnew=" + fromEmailnew);
                                        }
                                        catch (Exception ex)
                                        {
                                            _ = _exception.ErrorNew(ex, _LogAPI.Book); //ExManager.Exception.LogException(ex.ToString() + "GetEmailIdsBranchWise");
                                        }
                                        if (!string.IsNullOrEmpty(BranchEmailId))
                                            BranchEmailId = BranchEmailId + "," + fromEmailnew;
                                        else
                                            BranchEmailId = fromEmailnew;
                                        if (IsCancellMail == 0)
                                        {
                                            emailListsupplier.Add(new Email_Details
                                            {
                                                FromId = fromEmailnew,
                                                ToId = toemailliststring,
                                                CcId = BranchEmailId,
                                                BccId = "",
                                                Subject = Subject,
                                                EmailText = HotelHtmlMainXml,
                                                EmailType = "Booking Mail_" + (AgentId == 1899 ? "1899" : "0"),
                                                ServiceType = "Hotel",
                                                Website = HostName,
                                                CompanyId = 1,// Convert.ToInt32(System.Web.HttpContext.Current.Session["CompanyId"]),
                                                IsRayna = 1,//Convert.ToInt32(Common.getSession("IsRayna") == "true" ? 1 : 0),
                                                UserHost = _keySettings.WLUserHost,
                                                UserPassword = _keySettings.WLUserPassword,
                                                UserPort = _keySettings.WLUserPort,
                                                UserEmail = _keySettings.WLUserEmail,
                                                CompanyName = _keySettings.WLUserCompanyName,
                                                refno = rerefenceNo
                                            });
                                            //Common.SaveMailSendingDetails(emailListsupplier, 0);
                                        }
                                    }
                                }
                            }
                        }

                    }
                    #endregion

                    await SaveMailSendingDetails(emailListsupplier, status);

                }
            }
            catch (Exception ex)
            {
                _ = _exception.ErrorNew(ex, _LogAPI.Book);

                return;
            }

        }

        public async Task SaveMailSendingDetails(List<Email_Details> email, int emailstatus = 0)
        {
            try
            {
                DataTable dt = HtmlExtensions.ConvertToDataTable(email);
                SqlParameter[] para = new SqlParameter[]{
                 new SqlParameter("@dt",  dt),
                 new SqlParameter("@emailstatus",  emailstatus),
            };
                await _dlConnection.ExecuteNonQuery("SaveEmailDetails", para);

            }
            catch (System.Exception ex)
            {
                _ = _exception.ErrorNew(ex, _LogAPI.Book);
            }
        }

        private async Task<string> getApisoURL_string(int BookingId)
        {
            string html_vcn = string.Empty;
            try
            {
                int IsHotel = 0;
                int EmployeeId = 0;
                string VCNID = "";
                SqlParameter[] para_getCred = new SqlParameter[] {
                new SqlParameter("@hotelbookingid", BookingId),
                        };
                DataTable dt_det = await _dlConnection.GetDataTableAsync("gethotelCredentials_web_withallowLink", para_getCred); //booking has vcn and allow vcn link 
                if (dt_det.Rows.Count > 0)
                {
                    #region add vcn link here              
                    String Token = dt_det.Rows[0]["onlineBookingMasterId"] + "$|$" + Convert.ToString(dt_det.Rows[0]["ReferenceNo"]) + "$|$" + Convert.ToString(0) + "$|$" + Convert.ToString(dt_det.Rows[0]["SupplierName"]);
                    var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(Token);
                    String TOkenEcode = System.Convert.ToBase64String(plainTextBytes);
                    string DynamicURL = "https://b2bbh.withinearth.com/VCPayment?htlcnftoken=" + TOkenEcode; //Added 02-jun-2025 B2B to B2BBH
                    html_vcn = "<span style=\"color:red;font-family:Calibri;font-size:18px\" valign=\"top\" height=\"40\" align=\"left\"><b>To charge Payment, Please download the VCC Link:<span> <a target=\"_blank\" href =\"" + DynamicURL + "\" style=\"color:blue; font-weight:bold;\" >Click here </a>  <span></span>";
                    #endregion add vcn link her
                }
                else
                {
                    return "";
                }
            }
            catch (Exception ex)
            {
                _ = _exception.ErrorBooking(ex, "");
                return "";

            }

            return html_vcn;
        }

        public async Task<DataTable> GetEmailIdsBranchWise(int agentID, int CountryId)
        {
            try
            {
                SqlParameter[] para = new SqlParameter[]{
                  new SqlParameter("@AgentId", agentID),
                  new SqlParameter("@CountryId", CountryId)
             };
                return await _dlConnection.GetDataTableAsync("GetBranchEmail", para);
            }
            catch (Exception ex)
            {
                _ = _exception.ErrorNew(ex, _LogAPI.Book);
                return null;
            }
        }

        public async Task SendEmailToSupplierForConfirmationNo(string rerefenceNo = "", DataSet dsv = null, string HostName = "")
        {
            try
            {
                DataSet mailbody = new DataSet();
                // List<Email_Details> emailList = new List<Email_Details>();
                List<Email_Details_HCN> emailList = new List<Email_Details_HCN>(); // added on 25-10-2024
                                                                                   // DataSet dtemail = newvou.GetVoucherListNew(rerefenceNo, BookingId);
                DataSet dtemail = dsv;


                int emailsendstatus = 0;

                if (dtemail.Tables[0].Rows.Count > 0)
                {

                    DataRow[] drSupplierDetail = dtemail.Tables[0].Select("ServiceType='Hotel'");
                    if (drSupplierDetail.Length > 0)
                    {
                        mailbody = await GetVoucher(301);
                        foreach (DataRow drSupplierdata in drSupplierDetail)
                        {
                            string commonTemplate = string.Empty;
                            string Subject = string.Empty;
                            string Bccemail = string.Empty;
                            string _serviceType = "";

                            if (Convert.ToInt32(drSupplierdata["IsSendSupplierMail"]) == 1 && Convert.ToString(drSupplierdata["BookingStatus"]).ToLower() != "not confirmed")
                            {
                                commonTemplate = Convert.ToString(mailbody.Tables[0].Rows[0]["NotificationBody"]);
                                Subject = Convert.ToString(mailbody.Tables[0].Rows[0]["Subject"]);
                                Subject = Subject.Replace("#GuestName", Convert.ToString(drSupplierdata["Name"]));
                                Subject = Subject.Replace("#AgentVoucherNo", Convert.ToString(drSupplierdata["AgentVoucherNo"]));
                                Subject = Subject.Replace("#RefNo", rerefenceNo);

                                string CCEmailId = "global-rsvn@withinearth.com";
                                if (!string.IsNullOrEmpty(Convert.ToString(drSupplierdata["SupplierCCEmail"])))
                                    CCEmailId += ',' + Convert.ToString(drSupplierdata["SupplierCCEmail"]);
                                string SupplierFromEmailId = "support@withinearth.com";
                                string SupplierEmailId = Convert.ToString(drSupplierdata["SupplierEmailId"]);

                                commonTemplate = commonTemplate.Replace("#ReferenceNo", Convert.ToString(drSupplierdata["AgentVoucherNo"]));
                                commonTemplate = commonTemplate.Replace("#HotelName", Convert.ToString(drSupplierdata["ServiceName"]));
                                commonTemplate = commonTemplate.Replace("#GuestName", Convert.ToString(drSupplierdata["Name"]));
                                commonTemplate = commonTemplate.Replace("#Nationality", Convert.ToString(drSupplierdata["Nationality"]));
                                commonTemplate = commonTemplate.Replace("#SupplierName", Convert.ToString(drSupplierdata["SupplierName"]));
                                commonTemplate = commonTemplate.Replace("#SupplierRefNo", Convert.ToString(drSupplierdata["SupplierRefNo"]));
                                commonTemplate = commonTemplate.Replace("#CheckInDate", Convert.ToString(drSupplierdata["TravelDate"]));
                                commonTemplate = commonTemplate.Replace("#CheckOutDate", Convert.ToString(drSupplierdata["CheckOutDate"]));
                                commonTemplate = commonTemplate.Replace("#NoOfRoom", Convert.ToString(drSupplierdata["NoOfRooms"]));
                                commonTemplate = commonTemplate.Replace("#NoOfNight", Convert.ToString(drSupplierdata["NoOfNights"]));
                                commonTemplate = commonTemplate.Replace("#NoOfPax", Convert.ToString(drSupplierdata["Adult"]));
                                commonTemplate = commonTemplate.Replace("#RoomType", Convert.ToString(drSupplierdata["RoomType"]));
                                //rerefenceNo = Convert.ToString(drSupplierdata["ReferenceNo"]);

                                _serviceType = Convert.ToString(drSupplierdata["BookingId"]) + "-" + Convert.ToString(drSupplierdata["BookingDetailId"]); // added on 25-10-2024
                                DateTime _emailWaitingDate = Convert.ToDateTime(drSupplierdata["TravelDate"]).AddDays(-7); // added on 25-10-2024
                                emailList.Add(new Email_Details_HCN
                                {
                                    FromId = SupplierFromEmailId,
                                    ToId = SupplierEmailId,
                                    CcId = CCEmailId,
                                    BccId = Bccemail,
                                    Subject = Subject,
                                    EmailText = commonTemplate,
                                    EmailType = "Supplier Confirmation No Mail",
                                    ServiceType = _serviceType,
                                    Website = HostName,
                                    CompanyId = 1,// Convert.ToInt32(System.Web.HttpContext.Current.Session["CompanyId"]),
                                    IsRayna = 1,// Convert.ToInt32(Common.getSession("IsRayna") == "true" ? 1 : 0),
                                    UserHost = _keySettings.WLUserHost,
                                    UserPassword = _keySettings.WLUserPassword,
                                    UserPort = _keySettings.WLUserPort,
                                    UserEmail = _keySettings.WLUserEmail,
                                    CompanyName = _keySettings.WLUserCompanyName,
                                    refno = rerefenceNo,
                                    emailWaitingDate = _emailWaitingDate // added on 25-10-2024
                                });

                            }
                        }
                        //  Common.SaveMailSendingDetails(emailList, emailsendstatus);
                        await SaveMailSendingDetails_HCN(emailList, emailsendstatus); // added on 25-10-2024
                    }
                }
            }

            catch (Exception ex)
            {
                _ = _exception.Error(ex);
                return;
            }

        }

        public async Task SaveMailSendingDetails_HCN(List<Email_Details_HCN> email, int emailstatus = 0)
        {
            try
            {
                DataTable dt = HtmlExtensions.ConvertToDataTable(email);
                SqlParameter[] para = new SqlParameter[]{
                 new SqlParameter("@dt",  dt),
                 new SqlParameter("@emailstatus",  emailstatus),
            };
                await _dlConnection.ExecuteNonQuery("SaveEmailDetails_HCN", para);

            }
            catch (System.Exception ex)
            {
                _ = _exception.ErrorBooking(ex, "");
            }
        }


        public async Task SendNotConfirmBooking_MultiPleRoom(string ReferenceNo, string HostName = "")
        {
            try
            {
                SqlParameter[] para = new SqlParameter[]{
                new SqlParameter("@PNRNo",ReferenceNo),
            };

                DataSet ds = await _dlConnection.GetDataSet("getBooking_MultiRoom_Notconfirm", para);
                if (ds != null && ds.Tables[0].Rows.Count > 0)
                {
                    string Subject = Convert.ToString(ds.Tables[1].Rows[0]["Subject"]);
                    string Body = Convert.ToString(ds.Tables[1].Rows[0]["NotificationBody"]);
                    Subject = Subject.Replace("#ReferenceNo", ReferenceNo);
                    List<Email_Details> emailList = new List<Email_Details>();
                    for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                    {
                        string finalbody = Body;
                        finalbody = finalbody.Replace("#ReferenceNo", ReferenceNo);
                        finalbody = finalbody.Replace("#SupplierName", Convert.ToString(ds.Tables[0].Rows[i]["Provider"]));
                        finalbody = finalbody.Replace("#CheckInDate", Convert.ToString(ds.Tables[0].Rows[i]["CheckInDate"]));
                        finalbody = finalbody.Replace("#CheckoutDate", Convert.ToString(ds.Tables[0].Rows[i]["CheckoutDate"]));
                        finalbody = finalbody.Replace("#Buying", Convert.ToString(ds.Tables[0].Rows[i]["CompanyBuyingPrice"]));
                        finalbody = finalbody.Replace("#Selling", Convert.ToString(ds.Tables[0].Rows[i]["AgentBuyingPrice"]));

                        emailList.Add(new Email_Details
                        {
                            FromId = Convert.ToString(_keySettings.B2CBookingEmailId),
                            ToId = Convert.ToString(ds.Tables[1].Rows[i]["ToEmailId"]),
                            CcId = Convert.ToString(ds.Tables[1].Rows[i]["CCEmailId"]),
                            BccId = "",
                            Subject = Subject,
                            EmailText = finalbody,
                            EmailType = "Multiple Rooms Bookings – NotConfirm",
                            ServiceType = "Multiple Rooms Bookings – NotConfirm",
                            Website = HostName,
                            CompanyId = 1,
                            IsRayna = 1,
                            //UserHost = Convert.ToString(HttpContext.Current.Session["WLUserHost"]),
                            //UserPassword = Convert.ToString(HttpContext.Current.Session["WLUserPassword"]),
                            //UserPort = Convert.ToString(HttpContext.Current.Session["WLUserPort"]),
                            //UserEmail = Convert.ToString(HttpContext.Current.Session["WLUserEmail"]),
                            UserHost = _keySettings.WLUserHost,
                            UserPassword = _keySettings.WLUserPassword,
                            UserPort = _keySettings.WLUserPort,
                            UserEmail = _keySettings.WLUserEmail,
                            CompanyName = _keySettings.WLUserCompanyName,
                            //CompanyName = Common.getSession("WLUserCompanyName"),
                            refno = ReferenceNo
                        });
                    }
                    await SaveMailSendingDetails(emailList, 0);
                }
            }
            catch (Exception ex)
            {
                _ = _exception.ErrorNew(ex, _LogAPI.Book);
            }
        }
        #endregion

       // public async Task<bool> CheckVervopatchBookingTime(string AgentCode)
       public bool CheckVervopatchBookingTime(string AgentCode)
        {
            switch (AgentCode.ToLower())
            {
                case "tb01":
                case "aura":
                    return true;
                default:
                    return false;
            }
        }
        public async Task<DataTable> GetVervoTechCredentailAtRoomRequest(int CacheMinutes = 5)
        {
            DataTable dt = new DataTable();

            try
            {
                string key = "VervoTechCredential"; // ✅ correct key name

                // Try get from cache
                if (_cache.TryGetValue(key, out DataTable cachedDt))
                    return cachedDt;

                SqlParameter[] para = new SqlParameter[] { };

                dt = await _dlConnection.GetDataTableAsync("GetVervoTechCredentail_API", para);

                if (dt != null && dt.Rows.Count > 0)
                {
                    _cache.Set(key, dt, TimeSpan.FromMinutes(CacheMinutes));
                }

                return dt;
            }
            catch (Exception ex)
            {
                _ = _exception.Error(ex, "GetVervoTechCredentailAtRoomRequest");
            }

            return dt;
        }
        public  bool ExcludeSupplierFromVervoMappingCheck(string SupplierName)
        {
            switch (SupplierName.ToLower())
            {
                case "oth":
                case "siteminder":
                case "derbysoft":
                case "reseliva":
                case "hotelrunner":
                case "ratetiger":
                case "dedge":
                case "rategain":
                case "travelclick":
                case "staah":
                case "staah_oth":
                case "derbysoft_push":
                case "hoteliersguru":
                case "hoteliersguru_oth":
                case "siteminder_oth":
                case "axisrooms":
                    return true;
                default:
                    return false;
            }
        }
        public async Task<string> GetHotelSupplierCodeVervo(string supplierName, int Timeout = 5, int CacheMinutes = 60)
        {
            string supplierCode = "";
            try
            {
                string keydata_Common = "SupplierCode_vervo";

                // Try get from cache
                if (!_cache.TryGetValue(keydata_Common, out DataTable dtSupplierCode))
                {
                    SqlParameter[] para = new SqlParameter[] { };

                    dtSupplierCode = await _dlConnection.GetDataTable_Replica(
                        "getHotelSupplierCode_vervo",
                        para,
                        Timeout
                    );

                    if (dtSupplierCode != null && dtSupplierCode.Rows.Count > 0)
                    {
                        _cache.Set(
                            keydata_Common,
                            dtSupplierCode,
                            new MemoryCacheEntryOptions()
                                .SetAbsoluteExpiration(TimeSpan.FromMinutes(CacheMinutes))
                        );
                    }
                }

                // Lookup supplier code
                if (dtSupplierCode != null && dtSupplierCode.Rows.Count > 0)
                {
                    DataRow[] rows = dtSupplierCode.Select("SupplierName='" + supplierName + "'");
                    if (rows.Length > 0)
                    {
                        supplierCode = Convert.ToString(rows[0]["SupplierCode"]);
                    }
                }
            }
            catch (Exception ex)
            {
                _ = _exception.Error(ex, "GetHotelSupplierCodeVervo");
            }

            return supplierCode;
        }
        public  string GetProvidersdataversion3(string Url, string UserName, string Password, List<int> HotelId, string SupplierName, int GiataId, string Searchkey, int AgentId, string MethodName, ref string VervoRequest)
        {

            string Response = "";
            try
            {
                VervoRequest = Url;
                Stopwatch timer = new Stopwatch();
                timer.Start();
                Response = GetsupplierResponseversion3("", Url, Password, UserName);
                timer.Stop();
                try
                {
                    Task.Run(() => InsertVervoTechLogs(AgentId, GiataId, Url, Response, Searchkey, MethodName, timer.Elapsed.ToString()));
                }
                catch (Exception ex)
                {
                   // Common.ErrorBooking(ex, AgentId.ToString());
                }
            }
            catch (Exception ex)
            {
                //Common.ErrorBooking(ex, "GetProvidersdataversion3");
                _ = _exception.Error(ex, "GetProvidersdataversion3");
            }
            return Response;
        }

        public  string GetsupplierResponseversion3(string strxmlRQRS, string strHost, string password, string UserName)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            ServicePointManager.Expect100Continue = false;

            string strOutput = "";
            try
            {
                HttpWebRequest Request;
                Request = (HttpWebRequest)HttpWebRequest.Create(strHost);
                Request.Headers.Add("Accept-Encoding", "gzip");
                Request.ContentType = "application/json;charset=utf-8";
                Request.Method = "GET";
                //Request.Accept = "johndoe";
                Request.Headers.Add("accountId", UserName);
                Request.Headers.Add("apikey", password);
                Request.KeepAlive = true;
                //ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                //ServicePointManager.Expect100Continue = false;
                Request.Timeout = 10000;
                Request.UseDefaultCredentials = true;
                Stream newStream = null;
                byte[] data = Encoding.ASCII.GetBytes(strxmlRQRS);
                Request.ContentLength = data.Length;
                HttpWebResponse Response = (HttpWebResponse)Request.GetResponse();
                Stream responseStream = Response.GetResponseStream();
                //responseStream = new GZipStream(responseStream, CompressionMode.Decompress);
                responseStream = new System.IO.Compression.GZipStream(responseStream, System.IO.Compression.CompressionMode.Decompress);
                StreamReader Reader = new StreamReader(responseStream, Encoding.Default);
                strOutput = Reader.ReadToEnd();
                responseStream.Close();
                Reader.Close();
                Response.Close();
            }
            catch (Exception ex)
            {
                _ = _exception.Error(ex, "GetVervoTechCredentailAtRoomRequest");
            }

            return strOutput;
        }

        public async Task InsertVervoTechLogs(int AgentId, int GiataId, string Req, string Response, string Searchkey, string Status, string timer)
        {
            try
            {
                SqlParameter[] para = new SqlParameter[]
                {
            new SqlParameter("@AgentId", AgentId),
            new SqlParameter("@GiataId", GiataId),
            new SqlParameter("@UnicaId", 0),
            new SqlParameter("@Our_xmlhotelid", "-"),
            new SqlParameter("@Vervo_xmlhotelid", "-"),
            new SqlParameter("@Our_SupplierName", "-"),
            new SqlParameter("@Vervo_SupplierName", "-"),
            new SqlParameter("@Vervo_Res", $"###Request = {Req} ###Response = {Response}"),
            new SqlParameter("@SearchKey", Searchkey),
            new SqlParameter("@LogType", Status),
            new SqlParameter("@ElapsedTime", timer),
                };

                // ✅ Use injected dependency
                await _dlConnection.GetDataTable_Replica("SaveVervoValidationLogs_Xconnect", para,5);
            }
            catch (Exception ex)
            {
                _ = _exception.Error(ex, "InsertVervoTechLogs");
            }
        }
    }
}
