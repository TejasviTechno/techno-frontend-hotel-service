
using System.ComponentModel;
using System.Data;
using System.Text.Json;
using System.Text.Json.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using XConnect.Models.Hotel;

namespace XConnect.Models
{
    public class HotelXML_Class
    {
    }

    public class RecheckETG
    {
        public string EsentialInfo { get; set; } = "";
        public string Book_Hash { get; set; } = "";
    }
    public class RequestTrackingLog
    {
        public string SearchData { get; set; }
        public string IP { get; set; }
        public int Agentid { get; set; }
        public int TotalRecord { get; set; }
        public int CityID { get; set; }
        public bool from { get; set; }
        public string SupplierTime { get; set; }
        public string SearchKey { get; set; }
    }
    public class ResLog
    {
        public string XmlRequest { get; set; }
        public string XmlResponse { get; set; }
        public string Provider { get; set; }
        public string TypeRequest { get; set; }
        public string ReqId { get; set; }
        public string SearchKey { get; set; }

    }
    public class ExceptionLogDocument
    {
        public string servicename { get; set; }
        public string title { get; set; }
        public int status { get; set; }
        public string details { get; set; }
        public string datetime { get; set; }
        public string source { get; set; }
        public string WebsiteName { get; set; }
        public string SearchKey { get; set; }
        public string Info { get; set; }
        public int ApiName { get; set; } = 0;//default 0 other
    }
    public class TokenDetails
    {
        public int agentId { get; set; }
        public int subAgentId { get; set; }
        public int subUserId { get; set; }
        public int userTypeId { get; set; }
        public Guid tenantId { get; set; }
        public int creditLimitCurrencyId { get; set; }
        public int mapCategoryId { get; set; }
        public string agentCode { get; set; }
        public string creditLimitCurrencyName { get; set; }
        public string userType { get; set; } //30/12/25
        public int parentId { get; set; } //30/12/25
        public string siteCurrency { get; set; } //30/12/25
        public bool CanBookWithinCancellation { get; set; } = false;
        public bool IsFromAPI { get; set; } = true;
    }
    public class DtRoeNew //041125
    {
        public int currencyId { get; set; }
        public string currencyCode { get; set; }
        public double buyingROE { get; set; }
        public double sellingROE { get; set; }

    }
    public class ApiResponseNew<T>
    {
        public int statusCode { get; set; }
        public object error { get; set; }
        public List<T> result { get; set; }
    }


    #region Redis Class
    public class GiataDetailsRow
    {
        public int RaynaId { get; set; }
        public int Rating { get; set; }
        public int CountryId { get; set; }
        public int CityId { get; set; }
        public long GiataId { get; set; }
        public int IsRecomondedHotel { get; set; }
        public string HotelContactNo { get; set; }
        public string ReservationEmail { get; set; }
        public string HotelName { get; set; }
    }

    public class MappingDetailsRow
    {
        public string XmlId { get; set; }
        public int RaynaId { get; set; }
        public int OwnsystemHotelId { get; set; }
        public string XmlBaseId { get; set; }
        public string supplierName { get; set; }
        public int SupplierId { get; set; }
        public long GiataId { get; set; }
        public int CityId { get; set; }
        public string TechName { get; set; }

    }
    public class SpeedCategory
    {
        public class Root
        {
            public int AgentId { get; set; }
            public int CurrentAITO_Status { get; set; }
            public int ActiveSelfLearning { get; set; }
            public int IncludeBaseID { get; set; }
            public int Agt_SupTopselling_Base { get; set; }
            public List<Supplier> Suppleir { get; set; }

        }

        public class Supplier
        {
            public int SupplierID { get; set; }
            public string SupplierName { get; set; }
            public int ActiveStatus { get; set; }
            public int Agt_SupTopselling { get; set; }

        }
    }
    public class RaynaXML
    {
        public class Root
        {
            public int RaynaId { get; set; }
            public int Rating { get; set; }
            public int CountryId { get; set; }
            public int CityId { get; set; }
            public int giataid { get; set; }
            public int IsRecomondedHotel { get; set; }
            public string HotelContactNo { get; set; }
            public string ReservationEmail { get; set; }
            public string HotelName { get; set; }

            public List<XMLMapping> XMLMappings { get; set; }
        }

        public class XMLMapping
        {
            public string XmlId { get; set; }
            public int RaynaId { get; set; }
            public int OwnsystemHotelId { get; set; }
            public string XmlBaseId { get; set; }
            public string supplierName { get; set; }
            public int SupplierId { get; set; }
            public int GiataId { get; set; }
            public int CityId { get; set; }
            public string TechName { get; set; }
            //public int IsRecomondedHotel { get; set; }


        }
    }
    #endregion
    public class priceTravelToken
    {
        public string access_token { get; set; }
        public string expires_in { get; set; }
        public string token_type { get; set; }
        public string scope { get; set; }
    }
    public class QtechCancelCharge
    {
        public string ChargesRequest { get; set; } = "";
        public string AllowCancel { get; set; } = "";
    }

    public class ETGPriceCheck
    {
        public bool ispriceupdate { get; set; } = false;
        public string xslRequest { get; set; } = "";
        public string RatehawkResponse { get; set; } = "";
        public string booking_form_order_id { get; set; } = "";
    }
    public class WE_BookingDetail
    {
        public string Request { get; set; } = "";
        public string Response { get; set; } = "";
    }

    public class TBO_BookingProcess
    {
        public DataTable dtAgentVoucher { get; set; }
        public DataSet dsCartDetails { get; set; }
        public DataTable dtPassengers { get; set; }
        public DataTable dtCAncellationPolicy { get; set; }
        public string AgentVoucherNo { get; set; }
        public int bookingStatus { get; set; }
        public string UniqueNo { get; set; }
        public string AgentUniqueNo { get; set; }
        public bool isNonRefundable { get; set; }

    }

    public class MethaBookBookingDetail
    {
        public int bookingStatus { get; set; } = 2;
        public string AgentVoucherNo { get; set; } = "";

    }
    public class Email_Details_HCN
    {
        public string FromId { get; set; }
        public string ToId { get; set; }
        public string CcId { get; set; }
        public string BccId { get; set; }
        public string Subject { get; set; }
        public string EmailText { get; set; }
        public string EmailType { get; set; }
        public string ServiceType { get; set; }
        public string Website { get; set; }
        public int CompanyId { get; set; }
        public int IsRayna { get; set; }
        public string UserHost { get; set; }
        public string UserPassword { get; set; }
        public string UserPort { get; set; }
        public string UserEmail { get; set; }
        public string CompanyName { get; set; }
        public string refno { get; set; }
        public DateTime? emailWaitingDate { get; set; }
    }
    public class Email_Details
    {
        public string FromId { get; set; }
        public string ToId { get; set; }
        public string CcId { get; set; }
        public string BccId { get; set; }
        public string Subject { get; set; }
        public string EmailText { get; set; }
        public string EmailType { get; set; }
        public string ServiceType { get; set; }
        public string Website { get; set; }
        public int CompanyId { get; set; }
        public int IsRayna { get; set; }
        public string UserHost { get; set; }
        public string UserPassword { get; set; }
        public string UserPort { get; set; }
        public string UserEmail { get; set; }
        public string CompanyName { get; set; }
        public string refno { get; set; }
    }

    public class XMLBaseID_search
    {
        public string XMLBaseID { get; set; }
    }
    public class QtechBookRes
    {
        public string Res { get; set; }
        public string ReqBookingDetail { get; set; }
    }
    public class FitruumsBookRes
    {
        public int bookingStatus { get; set; }
        public string AgentVoucherNo { get; set; }
    }
    

    public class RoomBooking
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string _id { get; set; } // matches MongoDB "_id"

        [BsonElement("Id")]
        public int Id { get; set; } // matches MongoDB "Id"

        public int Hotelid { get; set; }

        public string XmlId { get; set; }

        public long roomid { get; set; }

        public string roomname { get; set; }

        public int statusflag { get; set; }

        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime entrydate { get; set; }
    }

    public class PriceCheckTravelokaDto //30-06-25
    {
        public double FinalPrice { get; set; } = 0;
        public string time { get; set; } = "";
        public string responsetype { get; set; } = "";
    }
    public class PriceCheckWelcomebedsDto //02-12-25

    {
        public double FinalPrice { get; set; } = 0;

        public string HasKey_IntCode { get; set; } = "";

        public bool Flag { get; set; }

    }


    public class PriceCheckBDCDto //30-06-25
    {
        public double FinalPrice { get; set; } = 0;
        public bool Flag { get; set; } = false;
        public double commission { get; set; } = 0;
    }

    public class PriceCheckAdonisDto //30-06-25
    {
        public double FinalPrice { get; set; } = 0;
        public bool Flag { get; set; } = false;
        public string PreBookingToken { get; set; } = "";
    }

    public class PriceCheckDidaDto //30-06-25
    {
        public double FinalPrice { get; set; } = 0;
        public int Flag { get; set; } = 1;
        public string ReferenceNo { get; set; } = "";
    }
    public class PriceCheckTboDto //24-06-25
    {
        public double ItemPrice { get; set; } = 0;
        public bool Flag { get; set; }
        public bool IsError { get; set; } = false;
        public bool IsPolicyChangeTBO { get; set; } = false;
    }

    public class PriceCheckQTDto //24-06-25
    {
        public double FinalPrice { get; set; } = 0;
        public double ResPrice { get; set; } = 0;
        public int Flag { get; set; }
    }

    public class PriceCheckRezliveDto //02-12-25
    {
        public double FinalPrice { get; set; } = 0;
        public string HasKey_IntCode { get; set; } = "";
        public bool Flag { get; set; }
    }

    public class PriceTravelBookRes
    {
        public string bookingKey { get; set; } = "";
        public bool IsError { get; set; } = false;
    }

    public class PriceCheckCMDto //24-06-25
    {
        public double ItemPrice { get; set; } = 0;
        public bool Flag { get; set; } 
        public bool IsError { get; set; } = false;
    }

    public class PriceCheckNuiteeDto //24-06-25
    {
        public double ItemPrice { get; set; } = 0;
        public bool Flag { get; set; }
        public bool IsError { get; set; } = false;
        public string PreBookingToken { get; set; } = "";
    }

    public class PriceCheckhotelsproDto //24-06-25
    {
        public double FinalPrice { get; set; } = 0;
        public bool Flag { get; set; }
        public bool IsErrors { get; set; } = false;
        public string roomCodeHP { get; set; } = "";
    }

    public class PriceCheckHTDto //17-11-25
    {
        public double FinalPrice { get; set; } = 0;
        public bool Flag { get; set; }
    }
    public class PriceCheckDto //24-06-25
    {
        public double FinalPrice { get; set; } = 0;
        public int Flag { get; set; }
    }

    public class ResponseDto
    {
        public string Response { get; set; } = "";
        public string Error { get; set; } = "";

    }

    public class ResponseRecheck
    {
        public string Response { get; set; }
        public string ISBooking { get; set; }
    }

    public class ResponseRecheckTbo
    {
        public string Response { get; set; }
        public string ISBooking { get; set; }
        public string PurchaseToken { get; set; }
    }
    public class EssentialInfo
    {
        public static string Essentialinformation = " <br> Useful Information : Hotel In Case of No-Show or Early Check-Out, 100% charges will be imposed and no Partial/Full refund will be honored in time.Any cancellation after the local Check-In time of the hotel, 100% charges will be imposed.";


    }

    public class Room_EgyptExpress
    {
        public int RoomCount { get; set; }
        public string RoomName { get; set; }
        public bool status { get; set; }
    }
    public class ErrorCode
    {
        public static string InternalError = "INTERNAL_ERROR";
        public static string NorecordError = "NOCOUNT_ERROR";
        public static string NoPermission = "NOTPERMITTED_ERROR";
        public static string DUPLICATION = "DUPLICATION_ERROR";
        public static string Mismatched = "MISMATCH_ERROR";
        public static string RequestError = "INVALID_REQUEST";
        public static string CacheError = "SearchKey_Error";
        public static string NoError = "SUCCESSFULL";
        public static string SomeError = "NOT_SUCCESSFULL";

        public static string AutorizationError = "AUTHORIZATION_ERROR";


    }
    public class ErrorDescription
    {
        public static string UseFTP = "Please use FTP for hotel static data.";
        public static string InternalError = "the server was unable to process the request due to an internal error";
        public static string NorecordError_Search = "No availability found for given criteria try search with other criteria";
        public static string NoPermission = "Bookings within cancellation deadline are not permitted for this User.";
        public static string Duplicate_PrebookExist = "Booking token already exist for this criteria";
        public static string Duplicate_InternalReference = "Booking already exist for this internal reference ";
        public static string NoPermission_NotAllowed = "Bookings are not permitted for this User.";
        public static string PriceMismatched = "Pricing could not complete - rate missing";
        public static string DataMismatched = "Data mismatch from previous response";

        public static string NoPermission_Cancel = "This Booking can not be cancel or already cancelled.";
        public static string RequestError = "Request is not well formed please check and  try again !!";
        public static string NorecordError = "No record found for given criteria..";
        public static string CacheError = "Access to search key is no longer available";
        public static string PolicyNotFound = "Access to policy failed.Make sure you have CancellationPolicy";
        public static string NoAvailbleAtBookingTime = "This Rate is no longer available";
        public static string Norecord_HCNo = "Hotel ConfirmationNumber Not yet Updated please Try Again Later !!";
        public static string ServiceUnavailable = "Service Unavailable !!";
        public static string HotelIdRestriction = "Requested HotelId is not allowed for your account..";
        public static string creditlimit_NRF_not_allowed = "Your credit limit is insufficient or booking within cancellation deadline is not allowed";//Ticket#26129
        public static string Booking_Cancelled = "This Booking is already cancelled."; // #55391

    }
    public class ErrorType
    {
        public static string InternalError = "API_ERROR";
        public static string GeneralError = "GENERAL";
        public static string NoPermission = "Permission";
        public static string DUPLICATION = "Duplicate";
        public static string Mismatched = "Mismatch";
        public static string RequestError = "RequestError";
        public static string CacheError = "SearchKeyExpired";



    }
    public class CommonRequest
    {
        public string Token { get; set; }
        public object Request { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public AdvancedOptions AdvancedOptions { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public TokenData TokenData { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public int IsMobile { get; set; } = 0;
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public int IsSearch { get; set; } = 1;
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public int IsSendMail { get; set; } = 0;

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool IsPackage { get; set; } //added 20-may-2026
        public bool IsFromB2B { get; set; } //added 20-may-2026


    }

    public class AdvancedOptions

    {
        public string Currency { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool RefreshCache { get; set; } = false;
        public string CustomerIpAddress { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool SearchByOneID { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public DataTable dtMeal { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string HotelName { get; set; } = "";
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public Int64 hotelid { get; set; } = 0;
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string Provider { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public int Hit { get; set; } = 0;
        public string searchKeyAgoda { get; set; } //newadded 19-march-2026


    }

    public class IpList //Added on 04-MAY-26
    {
        public string IP { get; set; }
    }
    public class IPWhitelistRQ
    {
        public int AgentId { get; set; }
        public List<IPWhitelist> IPsList { get; set; }

        public int RateCategoryId { get; set; } //new 251224 
        public ConfigMain mainconfig { get; set; } //new 251224 
    }

    public class HotelCodeByCity
    {
        public string XmlId { get; set; }
        public string XmlbaseId { get; set; }
        public int GiataId { get; set; }
        public int CityID { get; set; }
        public int Rating { get; set; }
        public int Raynaid { get; set; }
        public string supplierName { get; set; }
        public Boolean IsRecomondedHotel { get; set; }

    }

    public class XMLBaseID_Search
    {
        public string XMLBaseID { get; set; }
    }
    public class XMLID_Search
    {
        public string XMLID { get; set; }
    }
    public class IPWhitelist
    {
        public int AgentID { get; set; }
        public int Type { get; set; }
        public string IP { get; set; }
        public string AddedBy { get; set; }
        public string UpdatedBy { get; set; }
        public int StatusFlag { get; set; }
        public int IsAutoVoucher { get; set; }
    }



    public class ConfigMain
    {

        public DataTable dtROE { get; set; }
        public List<DtRoeNew> dtROENew { get; set; }
        public int ismobile { get; set; }
        public int SpeedCategoryId { get; set; }
        public int IsSearchAllow { get; set; }
        public int IsSearchMail { get; set; }
        public int CitycodeOverpass { get; set; }//@HID //160525

    }

    public class Country
    {
        public string CountryId { get; set; }
        public string GroupValue { get; set; }
    }

    public class CitiesData
    {
        public int CityId { get; set; }
        public string Name { get; set; }
    }

    public class Countries
    {
        public string CountryId { get; set; }
        public string Name { get; set; }
    }

    public class _Error
    {
        public string type { get; set; }
        public string code { get; set; }
        public string description { get; set; }
    }

    #region 070525

    public class StaticData
    {
        public int HotelId { get; set; }
        public bool IsRecommand { get; set; }
    }

    public class StaticDataCity
    {

        public string CityId { get; set; }
        public string LocationId { get; set; }    
        public string Type { get; set; }    


    }
    #endregion

    
    
    public class StaticDataCity_Auto
    {
        public string CityName { get; set; }

    }

    public class HotelSync
    {
        public int CityId { get; set; }
        public string SupplierName { get; set; }
        public string HotelIds { get; set; }
        public string POI { get; set; }
        public string Propertytype { get; set; }
        public string Chain { get; set; }
        public string Bedprefrence { get; set; }
        public string AminitiesHotelIds { get; set; }
        public string Propertytheme { get; set; }
        public string Neighbouthood { get; set; }
        public int CountryId { get; set; }
        public int IsShowRooms { get; set; }
    }


    #region "Compact Response"

    public class AvailHotelList_Comp
    {
        public AvailabilityRS_Comp AvailabilityRS { get; set; }
        public List<_Error> Error { get; set; } = new List<_Error>();
    }
    public class AvailabilityRS_Comp
    {
        public string SearchKey { get; set; }
        public Int64 Count { get; set; }
        public string Currency { get; set; }

        public List<_HotelListNew_Comp> HotelResult { get; set; }
    }
    public class _HotelListNew_Comp
    {
        public decimal StartPrice { get; set; }
        public Int64 HotelId { get; set; }
        public List<Options_Comp> HotelOption { get; set; }
    }
    public class Options_Comp
    {
        public string HotelOptionId { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string SupplierName { get; set; } //170326
       
        public decimal MinPrice { get; set; }
        public bool IsCombineRoom { get; set; }
       
        public List<List<_FinalRoomList_Comp>> HotelRooms { get; set; }
    }

    public class _SEQNO { [JsonIgnore] public int SEQNO { get; set; } } //Add ON 4-FEB-2025
    public class _FinalRoomList_Comp : _SEQNO
    {
        //public string OwnsystemHotelId { get; set; }
        public string RoomNo { get; set; }
        public string RoomTypeName { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]

        public string RoomId { get; set; } //ADD On 29-JAN-2025

        public string MealName { get; set; }
        public decimal Price { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string SupplierCurrency { get; set; } //Added 04-FEB-26

        public string BookingStatus { get; set; }
        public string RoomToken { get; set; }
        public string MappedMealName { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        //public List<_FinalPolicy_Comp> CancellationPolicy { get; set; }


        public List<_FinalPolicy> CancellationPolicy { get; set; }
        //300526
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<_FRatebreakup> FinalRateBreakup { get; set; }
    }

    public class _FinalPolicy_Comp
    {
        public string FromDate { get; set; }
        public string ToDate { get; set; }
        public decimal CancellationPrice { get; set; }

    }
    #endregion

    public class RoomInfo //Add On 17SEP2025
    {
        public string ProcessedRoomTypeName { get; set; }
        public int OwnsystemHotelId { get; set; }
        public DateTime Time { get; set; }
    }
    public class SaveAPIRQRSLog_MongoDB
    {
        public string SearchKey { get; set; }
        public string Request { get; set; }
        public string Response { get; set; }
        public DateTime? Date { get; set; }
    }

    public class SaveSPR_MongoDB //added on 26-FEB-2025
    {
        public int AgentId { get; set; }
        public string Supplier { get; set; }
        public string ResponseType { get; set; }
        public DateTime Date { get; set; }
        public double RSTimeout { get; set; }//250325

        public int ResStatusCod { get; set; } //Added on 17-DEC-2025
        public string Host { get; set; } //Added on 17-DEC-2025
    }

    #region Response

    //Recheck - 270525
    public class AvailHotelList1 //#MongoDB
    {
        [BsonId]
        [BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
        public string Id { get; set; }
        public AvailabilityRS AvailabilityRS { get; set; }
        public string Search_Key { get; set; }
        public DateTime Time { get; set; }
    }
    //end
    public class AvailHotelList
    {
        public AvailabilityRS AvailabilityRS { get; set; }
        public string Search_Key { get; set; }//#MongoDB
        public DateTime Time { get; set; }//#MongoDB
        //public string checkIn { get; set; } // 23-march-2026
        //public string checkOut { get; set; } // 23-march-2026
    }

    public class AvailabilityRS
    {
        public string SearchKey { get; set; }
        public Int64 Count { get; set; }
        public string Currency { get; set; }

        public List<_HotelListNew> HotelResult { get; set; }
    }
    public class _HotelListNew
    {
        public decimal StartPrice { get; set; }
        public Int64 HotelId { get; set; }
        public string HotelCode { get; set; }

        public string HotelName { get; set; } // 24032026

        public List<Options> HotelOption { get; set; }
    }
    public class Options
    {
        public string HotelOptionId { get; set; }
        public string HotelCode { get; set; }
        public Int64 HotelId { get; set; }
        public decimal MinPrice { get; set; }
        public bool IsCombineRoom { get; set; }
        public string HotelName { get; set; } // newadded 24032026

        public List<List<_FinalRoomList>> HotelRooms { get; set; }
    }
    public class _FinalRoomList
    {
        public int Sequence { get; set; } = 0; // ADD ON 29-JAN-2025

        //public string OwnsystemHotelId { get; set; }
        public string RoomNo { get; set; }
        public string RoomTypeName { get; set; }
        public string MealName { get; set; }
        public decimal Price { get; set; }
        public string EssentialInformation { get; set; }
        public string BookingStatus { get; set; }
        public string RoomToken { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public List<_FRatebreakup>? RateBreakup { get; set; }
        public List<_FinalPolicy> CancellationPolicy { get; set; }
        public string CancellationToken { get; set; }
        //public List<_FinalRate> RateDetails { get; set; }
        public string RateDetailsToken { get; set; }
        public string MappedMealName { get; set; }
        public string SupplierCurrency { get; set; } //Added on 4-FEB-26
        public string SupplierName { get; set; } //170326

        public string Allocation { get; set; } //Added on 27-march-26
        public string RatePlanCode { get; set; } //Added on 27-march-26
        public string subSupplierId { get; set; } //Added on 27-march-26
        public string RoomTypeId { get; set; } //Added on 27-march-26
        public string channel { get; set; } //Added on 27-march-26
        public string MealFullname { get; set; } //Added on 28-march-26
    }
    public class _FinalPolicy
    {
        public string FromDate { get; set; }
        public string ToDate { get; set; }
        public decimal CancellationPrice { get; set; }
        public string daysBeforeCheckIn { get; set; } //Added on 27-march-26
        public bool isNRF { get; set; } //Added on 27-march-26
    }

    public class _FinalRoom
    {
        public List<List<_FinalRoomList>> _finalRoomList { get; set; }

        public decimal MinPrice { get; set; }
        public bool IsRoomCombine { get; set; }
    }
    #endregion

    public class CommonSearchList
    {
        public int CompanyId { get; set; }
        public CommonHotelCodes commonHotelCodes { get; set; }
        public bool HitForSearch { get; set; }
        public List<Int64> HotelIDS { get; set; }
        public bool SearchByHotel { get; set; }
    }

    public class CommonHotelCodes
    {
        public DataRow[] RateGain_Codes { get; set; }
        public DataRow[] Siteminder_Codes { get; set; }
        public DataRow[] hotelrunner_Codes { get; set; }
        public DataRow[] OTH_Codes { get; set; }
        public DataRow[] GTA_Codes { get; set; }
        public DataRow[] DOTW_Codes { get; set; }
        public DataRow[] EXP_Codes { get; set; }
        public DataRow[] TBO_Codes { get; set; }
        public DataRow[] TBOAPI_Codes { get; set; }
        public DataRow[] Darina_Codes { get; set; }
        public DataRow[] RoomsXml_Codes { get; set; }
        public DataRow[] Miki_Codes { get; set; }
        public DataRow[] Quantum_Codes { get; set; }
        public DataRow[] LotsOfHotels_Codes { get; set; }
        public DataRow[] W2M_Codes { get; set; }
        public DataRow[] JacTravel_Codes { get; set; }
        public DataRow[] uk_travco_Codes { get; set; }
        public DataRow[] Hotelspro_Codes { get; set; }
        public DataRow[] XTG_Codes { get; set; }
        public DataRow[] HotelBeds_Codes { get; set; }
        public DataRow[] TravelLanda_Codes { get; set; }
        public DataRow[] EgyptExpress_Codes { get; set; }
        public DataRow[] AlphaTours_Codes { get; set; }
        public DataRow[] Genting_Codes { get; set; }
        public DataRow[] GTE_Codes { get; set; }
        public DataRow[] GoodWin_Codes { get; set; }
        public DataRow[] FastPayHotels_Codes { get; set; }
        public DataRow[] MGHoliday_Codes { get; set; }
        public DataRow[] ExpediaB2b_Codes { get; set; }
        public DataRow[] ExpediaPKG_Codes { get; set; }
        public DataRow[] Escalabeds_Codes { get; set; }
        public DataRow[] BookoHotel_Codes { get; set; }
        public DataRow[] TravcoEgypt_Codes { get; set; }
        public DataRow[] fitruums_Codes { get; set; }
        public DataRow[] WHL_Codes { get; set; }
        public DataRow[] Mercan_Codes { get; set; }
        public DataRow[] WorldAvenues_Codes { get; set; }
        public DataRow[] AsianOverland_Codes { get; set; }
        public DataRow[] RTS_Codes { get; set; }
        public DataRow[] TravStore_Codes { get; set; }
        public DataRow[] MethaBook_Codes { get; set; }
        public DataRow[] Solole_Codes { get; set; }
        public DataRow[] WhiteSandsUAE_Codes { get; set; }
        public DataRow[] Youtravel_Codes { get; set; }
        public DataRow[] AbreuOnline_Codes { get; set; }
        public DataRow[] Olympia_Codes { get; set; }
        public DataRow[] IntraTour_Codes { get; set; }
        public DataRow[] InfiniteHotelsFrance_Codes { get; set; }
        public DataRow[] Restel_Codes { get; set; }
        public DataRow[] eref_Codes { get; set; }
        public DataRow[] HotelDo_Codes { get; set; }
        public DataRow[] gtbeds_Codes { get; set; }
        public DataRow[] mtc_Codes { get; set; }
        public DataRow[] Dida_Codes { get; set; }
        public DataRow[] Agoda_Codes { get; set; }
        public DataRow[] EAN_Codes { get; set; }
        public DataRow[] MgBedBank_Codes { get; set; }
        public DataRow[] GoGlobal_Codes { get; set; }
        public DataRow[] Rezlive_Codes { get; set; }
        public DataRow[] Bonotel_Codes { get; set; }
        public DataRow[] Yalago_Codes { get; set; }
        public DataRow[] travelbullz_techno_Codes { get; set; }
        public DataRow[] bedlinker_techno_Codes { get; set; }
        public DataRow[] hotelinx_techno_Codes { get; set; }
        public DataRow[] rayna_techno_Codes { get; set; }
        public DataRow[] khayi_techno_Codes { get; set; }

        public DataRow[] travab_dmc_Codes { get; set; }
        public DataRow[] tfs_Codes { get; set; }
        public DataRow[] Adonis_Codes { get; set; }
        public DataRow[] DerbySoft_Codes { get; set; }
        public DataRow[] booking_Codes { get; set; }
        public DataRow[] Nuitee_Codes { get; set; }
        public DataRow[] GRNC_Codes { get; set; }
        public DataRow[] Traveloka_Codes { get; set; }
        public DataRow[] EZConnect_Codes { get; set; }
        public DataRow[] fitruums_b2c_Codes { get; set; }
        public DataRow[] HotelsproApi_Codes { get; set; }
        public DataRow[] HotelBedsB2C_Codes { get; set; }
        public DataRow[] AgodaNew_Codes { get; set; }
        public DataRow[] Rakuten_Codes { get; set; }
        public DataRow[] HotelTrader_Codes { get; set; }
        public DataRow[] IWTX_Codes { get; set; }
        public DataRow[] GetaRoom_Codes { get; set; }
        public DataRow[] PeakPoint_Codes { get; set; }
        //public DataRow[] HotelRunner_Codes { get; set; }
        public DataRow[] Nirvana_Codes { get; set; }
        public DataRow[] Omnibees_Codes { get; set; }
        public DataRow[] hyperguest_Codes { get; set; }
        public DataRow[] ratetiger_Codes { get; set; }
        public DataRow[] Jumeirah_Codes { get; set; }
        public DataRow[] farhat_techno_Codes { get; set; }
        public DataRow[] Robios_Codes { get; set; }
        public DataRow[] Travelclick_Codes { get; set; }
        public DataRow[] FastPayHotelsV3_Codes { get; set; }
        public DataRow[] Dedge_Codes { get; set; }
        public DataRow[] Price_Travel_Codes { get; set; }
        public DataRow[] Paximum_Codes { get; set; }
        public DataRow[] iTrip_Codes { get; set; }
        public DataRow[] agoda_v1_Codes { get; set; }
        public DataRow[] primetravel_Codes { get; set; }
        public DataRow[] WebHotellier_Codes { get; set; }
        public DataRow[] saltours_Codes { get; set; }
        public DataRow[] Ixpira_Codes { get; set; }
        public DataRow[] Welcomebeds_Codes { get; set; }
        public DataRow[] Italia_Codes { get; set; }
        public DataRow[] TeamAmerica_Codes { get; set; }
        public DataRow[] ElevateDMC_Codes { get; set; }

        public DataRow[] Agoda_Chian_Codes { get; set; } //060325
        public DataRow[] HotelBeds_new_Codes { get; set; } //100325
        public DataRow[] Juniper_Codes { get; set; } //100325
        public DataRow[] Iboosy_Codes { get; set; } //100325
        public DataRow[] Qtech_Codes { get; set; } //100325

        public DataRow[] Staah_Codes { get; set; }
        public DataRow[] DiscoverSaudi_Codes { get; set; }
        public DataRow[] Techno_Codes { get; set; }
        public DataRow[] bookingv3_Codes { get; set; }
        public DataRow[] Parsec_Codes { get; set; }
        public DataRow[] ETG_Codes { get; set; }
        public DataRow[] HotelsTrack_Codes { get; set; }
        public DataRow[] PkFare_Codes { get; set; }

        public DataRow[] LetsFly_Codes { get; set; }
        public DataRow[] Akbartravel_codes { get; set; }//tjp

        public DataRow[] Dingus_codes { get; set; }//tjp
        public DataRow[] Elevateholidays_codes { get; set; }//tjp
        public DataRow[] Exely_Codes { get; set; }
        public DataRow[] Techno_New_Codes { get; set; }
        public DataRow[] DirectConracting_Codes { get; set; }//230326

    }

    public class SupplierDetails
    {
        public Int32 SupplierID { get; set; }
        public string SupplierName { get; set; }
        public Int32 CutOffDays { get; set; }
        public Int32 MarkupCategoryId { get; set; }
        public double AgentWiseMarkup { get; set; }
        public Int32 IsShowSupplier { get; set; }
        public Int32 IsVeryNonrefundable { get; set; }
        public bool ActiveStatus { get; set; }
        public string WebSiteURL { get; set; }
        public string UserID { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public string version { get; set; }
        public Int32 CurrencyId { get; set; }
        public string CurrencyShortName { get; set; }
        public double XE { get; set; }
        public bool IsDynamicCurrency { get; set; }
        public Int32 MasterSupplierId { get; set; }

        public bool IsPolicyDynamic { get; set; }
        public bool IsCombineRoom { get; set; }
        public int ReturnRooms { get; set; } //130325
        public string TechName { get; set; } //130325

        public string WebSiteURLforAPI { get; set; } //Added on 3-MAR-2025
        public string HotelSupplier { get; set; } //Added on 09 april 2026
        public int AgentType { get; set; } //Added on 01-may-26

    }

    public class SupplierSearchConfig
    {
        public Int32 SupplierID { get; set; }
        public Int32 CityID { get; set; }
        public Int32 Stars { get; set; }
        public Int32 IsSearchHotelCode { get; set; }
        public Int32 RoomNo { get; set; }
        public Int32 HotelCodeMaxSize { get; set; }
        public Int32 IsPrefferedHotelOnly { get; set; }
        public Int32 isAutoOrManual { get; set; }
        public Int32 Noofrecords { get; set; }
        public Int32 HotelSearchTimeOut { get; set; }
        public Int32 BookingConfirmationTimeout { get; set; }
        public Int32 isFirstHit { get; set; }
        public Int32 CountryId { get; set; }
        public Int32 DebugMode { get; set; }
        public Int32 HotelSearchTimeOut2 { get; set; }
        public Int32 RoomSearchTimeOut { get; set; }
        public Int32 RoomSearchTimeOut2 { get; set; }
        public Int32 City { get; set; }
        public Int32 StarSplit { get; set; }
        public Int32 IsRoomFirst { get; set; }
        public Int32 IsHotelFirst { get; set; }
        public Int32 Hit { get; set; }

    }

    public class StarRating
    {
        public int Star { get; set; }
    }

    public class DataForSearch
    {
        public String xmlCity { get; set; }
        public string xmlCountry { get; set; }
        public string XmlNationality { get; set; }
    }

    public class SavesupplierLog_MongoDB
    {
        public string SearchKey { get; set; }
        public string Request { get; set; }
        public string Response { get; set; }
        public string Supplier { get; set; }
        public DateTime Date { get; set; }
    }

    public class CommonSearch
    {

        public AdvancedOptions advancedOptions { get; set; }
        public TokenData tokenData { get; set; }
        public DataTable dtAgentDetail { get; set; }
        public DataTable dtSuplierMarkup { get; set; }
        public DataTable dtSearchConfig { get; set; }
        public DataTable dtMarkupDetails { get; set; }

        public DataSet dsMarkupDetails { get; set; } //Added on 01-may-26
        public decimal AgentPercent { get; set; }
        public DataSet dsAgentDetails { get; set; }
        public DataSet supplierGiataMaster { get; set; }
        public DataTable dtCountryCityMarkup { get; set; }
        public int TariffID { get; set; }
        public int AgentId { get; set; }
        public int IsParentId { get; set; }
        public double SubAgentMarkup { get; set; }
        public double B2CMarkup { get; set; }

        public List<Int64> HotelIDS { get; set; }
        public bool SearchByHotel { get; set; }
    }

    public class X_PolicyDetails
    {
        public string SupplierName { get; set; }
        public string OwnsystemHotelId { get; set; }
        public string RoomNo { get; set; }
        public string RoomTypeId { get; set; }
        public string RoomTypeName { get; set; }
        public string MealId { get; set; }
        public string FromDate { get; set; }
        public string ToDate { get; set; }
        public string CompanyCancellationPrice { get; set; }
        public string AgentCancellationPrice { get; set; }
        public string PurchaseToken { get; set; }
        public string SPUI { get; set; }
        public string RoomSequenceNo { get; set; }
        public string CartID { get; set; }
        public string BoardTypeID { get; set; }
        public string WithInCancellationDeadLine { get; set; }
        public string GIITACode { get; set; }
        public string VatdiffAmount { get; set; }
        public string CheckInDate { get; set; }
        public string CheckOutDate { get; set; }
        public string CityId { get; set; }
        public string SupplierCurrency { get; set; }
        public string BuyingPrice { get; set; }
        public string Hit { get; set; }
        public string BuyingCancellationPrice { get; set; } = "0";

        public string RoomTypeCode { get; set; } // 27-03-2026
        public string daysBeforeCheckIn { get; set; } // 28-03-2026
        public string isNRF { get; set; } // 28-03-2026
    }

    public class X_RateBreakupDetails
    {
        public string SupplierName { get; set; }
        public string OwnsystemHotelId { get; set; }
        public string RoomNo { get; set; }
        public string RoomTypeName { get; set; }
        public string RateType { get; set; }
        public string RoomTypeId { get; set; }
        public string MealId { get; set; }
        public string Date { get; set; }
        public string DateOfWeek { get; set; }
        public string Meal { get; set; }
        public string Rate { get; set; }
        public string ContractID { get; set; }
        public string RoomSequenceNo { get; set; }
        public string OrderNo { get; set; }
        public string BookingStatus { get; set; }
        public string BoardTypeID { get; set; }
        public string Surcharge { get; set; }
        public string CompanyCharge { get; set; }
        public string Total { get; set; }
        public string SalesTaxAmount { get; set; }
        public string HotelOccupancyTaxAmount { get; set; }
        public string HotelFessDescription { get; set; }
        public string HotelFeeAmount { get; set; }
        public string CancellationText { get; set; }
        public string TotalNight { get; set; }
        public string GIITACode { get; set; }
        public string FinalRateType { get; set; }
        public string CheckIninstruction { get; set; }
        public string SpecialCheckIninstruction { get; set; }
        public string VatdiffAmount { get; set; }
        public string CompanyRate { get; set; }
        public string CheckInDate { get; set; }
        public string CheckOutDate { get; set; }
        public string CityId { get; set; }

        public string SupplierCurrency { get; set; }
        public string BuyingPrice { get; set; }
        public string Hit { get; set; }
        public string ExpediaSurcharge { get; set; }
        public string CartID { get; set; }
        public string RoomTypeCode { get; set; }  // added on30-apr-2026

    }

    public class X_Roomdetails
    {
        public string OwnsystemHotelId { get; set; }
        public string RoomNo { get; set; }
        public string RoomTypeName { get; set; }
        public string SupplierRoomTypeName { get; set; } //Added on 22-NOV-2024
        public string MealName { get; set; }
        public string SHRUI { get; set; }
        public string Board { get; set; }
        public string Type { get; set; }
        public string BoardType { get; set; }
        public string RoomTypeCode { get; set; }
        public string Characteristic { get; set; }
        public string RoomTypeId { get; set; }
        public string MealId { get; set; }
        public string CompanyBuyingPrice { get; set; }
        public string CompanySellingPrice { get; set; }
        public string AgentBuyingPrice { get; set; }
        public string AgentSellingPrice { get; set; }
        public string SubAgentBuyingPice { get; set; }
        public string SubAgentSellingPrice { get; set; }
        public Decimal? FinalSellingPrice { get; set; }
        public string IsRefundable { get; set; }
        public string SupplierName { get; set; }
        public string CancellationDeadline { get; set; }
        public string EssentialInformation { get; set; }
        public string allocationDetails { get; set; }
        public string minStay { get; set; }
        public string DateApplyMinStay { get; set; }
        public string OfferCode { get; set; }
        public string IncudedOfferDiscount { get; set; }
        public string GrossWitohutDiscount { get; set; }
        public string DiscountPer { get; set; }
        public string DiscountPerText { get; set; }
        public string AdultsOccupancy { get; set; }
        public string ChildOccupancy { get; set; }
        public string MaxExtrabed { get; set; }
        public string ContractID { get; set; }
        public string RoomSequenceNo { get; set; }
        public string OfferDescription { get; set; }
        public string UniqueNo { get; set; }
        public string BookingStatus { get; set; }
        public string BoardTypeID { get; set; }
        public string TotalSurcharge { get; set; }
        public string GIITACode { get; set; }
        public string ContractName { get; set; }
        public string OfficeId { get; set; }
        public string XMLHotelId { get; set; }
        public Decimal? CompanyBuyingPriceExpedia { get; set; }
        public Decimal? GrossProfitOnline { get; set; }
        public Decimal? OurProfit { get; set; }
        public Decimal? CurrencyFactor { get; set; }
        public Decimal? VatdiffAmount { get; set; }
        public string MasterMealId { get; set; }
        public string CheckInDate { get; set; }
        public string CheckOutDate { get; set; }
        public string CityId { get; set; }
        public bool IsCombineRoom { get; set; }
        public bool IsPolicyDynamic { get; set; }
        public Int32 LoyaltyPoints { get; set; }
        public string LoyaltyPointsIcon { get; set; }
        public string Hit { get; set; }
        public string MealClass { get; set; }
        public string OriginalMeal { get; set; }
        public string cancellationText { get; set; }
        public List<RoomSearch> RoomSearch { get; set; }
        public string SupplierCurrency { get; set; }
        public string BuyingPrice { get; set; }

        public string SupplierId { get; set; }
        public string ServiceName { get; set; }
        public decimal RoomPrice { get; set; }
        public string XMLHotelCode { get; set; }
        public string RaynaId { get; set; }
        public string XMLCITY_ID { get; set; }
        public string OTHPromoCode { get; set; }
        public string MappedMealName { get; set; }
        public string MealTypeDate { get; set; }

        public Int32 OldBookingDetailID { get; set; } //#Pruvo added on 6-MAY-2024
        public string channel { get; set; } // 28-march-26

    }
    public class RoomSearch
    {
        public int NoOfAdult { get; set; }
        public int NoOfChild { get; set; }
        public int RoomNo { get; set; }
    }

    public class _HotelList
    {
        public string OwnsystemHotelId { get; set; }
        public string XmlHotelCity { get; set; }
        public string XMLHotelCode { get; set; }
        public string HotelName { get; set; }
        //public string HotelFrontImage { get; set; }
        //public string Address { get; set; }
        public bool IsRecomondedHotel { get; set; }
        //public string Location { get; set; }
        public string XMLCountry { get; set; }
        public string XMLCity { get; set; }
        public string XMLCode { get; set; }
        public int Rating { get; set; }
        //public string latitude { get; set; }
        //public string longitute { get; set; }
        //public string IsOffer { get; set; }
        //public string OfferText { get; set; }
        //public string MealName { get; set; }
        //public string BeforeDiscountPrice { get; set; }
        //public double SellingPrice { get; set; }
        //public string CompanyBuyingPrice { get; set; }
        //public string CompanySellingPrice { get; set; }
        //public string AgentBuyingPrice { get; set; }
        //public string AgentSellingPrice { get; set; }
        //public string SubAgentBuyingPice { get; set; }
        //public string SubAgentSellingPrice { get; set; }
        public double FinalSellingPrice { get; set; }
        //public bool IsRefundable { get; set; }
        public string SupplierName { get; set; }
        public string OfficeID { get; set; }
        public int ContractID { get; set; }
        public string ContractName { get; set; }
        //public string Reviews { get; set; }
        //public string EssentialInformation { get; set; }
        //public string Description { get; set; }
        public string AvailToken { get; set; }
        //public string UniqueNo { get; set; }
        public bool IsShowSupplier { get; set; }
        //public string BookingStatus { get; set; }
        //public int ReviewRating { get; set; }
        //public int ReviewCount { get; set; }
        //public int SortSequence { get; set; }
        //public string GroupName { get; set; }
        //public int Favorite { get; set; }
        public string GIITACode { get; set; }
        public string RaynaId { get; set; }
        public string SupplierId { get; set; }
        //public double VatdiffAmount { get; set; }
        public string SupplierCurrency { get; set; }
        //public string IsSearchFor { get; set; }
        //public string HotelPriority { get; set; }
        public int MasterSupplierId { get; set; }
        public int ID { get; set; }
        public string ReservationEmail { get; set; }
        public string HotelContactNo { get; set; }
    }

    public class CacheRoomToken
    {
        public int giataid_Seq { get; set; }
        public int hotelid_Seq { get; set; }
        public int Room_seqno { get; set; }
        public int roomno { get; set; }
    }

    public class SaveParameter
    {
        public SaveParameter()
        {
            Rooms = new List<RoomDetails>();
        }

        public string UserName { get; set; }

        public string CityName { get; set; }
        public int CityID { get; set; }

        public string LocationID { get; set; }

        public string Nationality { get; set; }

        public string Residency { get; set; }
        public string CheckInDate { get; set; }
        public string CheckOutDate { get; set; }
        public int NoofNights { get; set; }
        public List<RoomDetails> Rooms { get; set; }
        public int AgentMarkup { get; set; }
        public int AgentCompanyPercentage { get; set; }
        public int StarStart { get; set; }
        public int StarEnd { get; set; }



        public string CountryId { get; set; }



    }

    #region PolicyWithPrice

    public class CancellationPolicyRS_Multi_v1_Supp
    {
        public string Provider { get; set; }
        public string HotelId { get; set; }
        public CancellationPolicyRS_Multi_v1 CancellationPolicyRS_Multi_v1 { get; set; }
    }

    public class CancellationPolicyRS_Multi_v1_newapi //270525_ recheck
    {
        public int HotelId { get; set; }
        public CancellationPolicyRS_Multi_v1 CancellationPolicyRS_Multi_v1 { get; set; }
    }

    public class CancellationPolicyRS_Multi_v1
    {
        public string Currency { get; set; }
        public List<PolicyResponse_Multi_v1> ReCheckRS { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        //public string Search_Key { get; set; }//#MongoDB //160425 - remove 
        public List<_Error> Error = new List<_Error>();
    }
    public class PolicyResponse_Multi_v1
    {
        public string RoomNo { get; set; }
        public string RoomTypeName { get; set; }
        public decimal Price { get; set; }
        public string BookingStatus { get; set; }
        public string MealName { get; set; }
        public string MappedMealName { get; set; }
        public string EssentialInformation { get; set; }
        public List<_FinalPolicy> CancellationPolicy { get; set; }
        public string RoomToken { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string SupplierCurrency { get; set; } //Added 04-FEB-26
    }
    #endregion

    #region CancelPolicyMulti
    public class CancellationPolicyRS_Multi_newapi
    {
        public CancellationPolicyRS_Multi CancellationPolicyRS_Multi;
        public int HotelId { get; set; }
    }
    public class CancellationPolicyRS_Multi
    {
        public string Currency { get; set; }
        public List<PolicyResponse_Multi> CancellationRS { get; set; }
        public List<_Error> Error = new List<_Error>();
    }

    public class CancellationPolicyRS_Multi_Supp
    {
        public CancellationPolicyRS_Multi CancellationPolicyRS_Multi { get; set; }
        public string HotelId { get; set; }

    }
    public class PolicyResponse_Multi
    {
        public string RoomNo { get; set; }
        public string RoomTypeName { get; set; }
        public string MealName { get; set; }
        public string MappedMealName { get; set; }
        public string EssentialInformation { get; set; }
        public List<_FinalPolicy> CancellationPolicy { get; set; }
        public string RoomToken { get; set; }
    }
    #endregion

    #region CancelPolicy

    public class CancellationPolicyRQ
    {
        public string SearchKey { get; set; }
        public string RoomToken { get; set; }
        public string HotelOptionId { get; set; }

    }

    public class CancellationPilicyRS_newapi //added on 23-03-25
    {
        public CancellationPilicyRS CancellationPilicyRS { get; set; }
        public Int32 HotelId { get; set; }
    }
    public class CancellationPilicyRS
    {
        public PolicyResponse CancellationRS { get; set; }
        public List<_Error> Error = new List<_Error>();

    }

    public class CancellationPilicyRS_Supp
    {
        public CancellationPilicyRS CancellationPilicyRS { get; set; }
        public string HotelId { get; set; }

    }
    public class PolicyResponse
    {
        public string Currency { get; set; }

        //public string CancellationToken { get; set; }
        public string EssentialInformation { get; set; }
        public List<_FinalPolicy> CancellationPolicy { get; set; }
    }
    #endregion

    #region Prebook Class
    public class PriceChange
    {
        public decimal OldPrice { get; set; }
        public decimal NewPrice { get; set; }
    }

    public class HotelRoomPreBook
    {
        public int UniqueId { get; set; }
        public string RoomNo { get; set; }
        public string RoomTypeName { get; set; }
        public string MealName { get; set; }
        public decimal Price { get; set; }
        public PriceChange PriceChange { get; set; }
        public string MappedMealName { get; set; }
    }

    public class HotelOptionPreBook
    {
        public string HotelName { get; set; }
        public string Nationality { get; set; }

        public string BookingToken { get; set; }
        public string Status { get; set; }
        public int IsPriceChange { get; set; } = 0;
        public string CheckInDate { get; set; }
        public string CheckOutDate { get; set; }
        public decimal TotalPrice { get; set; }
        public List<HotelRoomPreBook> HotelRooms { get; set; }
    }

    #region Extra Field

    #endregion

    public class PreBookRS
    {
        public HotelOptionPreBook HotelOption { get; set; }

        public string Currency { get; set; }

    }

    public class PreBookResponse_newapi
    {
        public PreBookResponse PreBookResponse { get; set; }
        public DataforLog DataforLog { get; set; }

        
    }

    public class UserCart_Prebook //Added on 10-JAN-26
    {
        public double TotalSellingAmount { get; set; }
        public double BaseTotalSellingAmount { get; set; }
        public int SupplierCurrencyId { get; set; }
        public double TotalBuyingAmount { get; set; }
        public double BaseTotalBuyingAmount { get; set; }
        public int ServiceId { get; set; }
        public string ServiceName { get; set; }
        public string ImagePath { get; set; }
        public int CountryId { get; set; }
        public int CityId { get; set; }
        //new
        public int HotelId { get; set; }
        public string HotelAddress { get; set; }
        public string HotelContactNo { get; set; }

        public int NoofNights { get; set; }
        public int NoofRoom { get; set; }
        public string Nationality { get; set; }
        public bool IsCombineRoom { get; set; }//230326
        public string HotelEmail { get; set; }//100426
        

        public List<UserCartDetails_Prebook> userCartDetails_Prebooks { get; set; }
    }

    public class UserCartDetails_Prebook //Added on 10-JAN-26
    {
        public string SubServiceName { get; set; }
        public int NoofAdult { get; set; }
        public int NoofChild { get; set; }
        public int NoofInfant { get; set; }
        public string ChildAge { get; set; }
        public double ServiceSellingAmount { get; set; }
        public double BaseServiceSellingAmount { get; set; }
        public double ServiceBuyingAmount { get; set; }
        public double BaseServiceBuyingAmount { get; set; }
        public string CheckInDate { get; set; }
        public string CheckOutDate { get; set; }
        public string DueDate { get; set; }
        public string DeadlineDate { get; set; }
        public string SupplierName { get; set; }
        public double LoyaltyPoints { get; set; }
        public string TechName { get; set; }

        //new
        public string BedType { get; set; }
        public string MealPlan { get; set; }


        public string RoomTypeId { get; set; }
        public string RoomTypeName { get; set; }
        public string SepplierRefNo { get; set; }
        public string OfferCode { get; set; }
        public string OfferDescription { get; set; }
        public string RoomEssentialInformation { get; set; }
        public string RoomMealInfo { get; set; }
        public string PurchaseToken { get; set; }
        public string EachoToken { get; set; }
        public string SPUI { get; set; }
        public int RoomNo { get; set; }


        public SupplierRateBreakUp_Prebook supplierRateBreakUp_Prebook { get; set; }
        public AgentRateBreakUp_Prebook agentRateBreakUp_Prebook { get; set; }
        public List<HotelPolicy_Prebook> hotelPolicy_Prebooks { get; set; }
        public List<Ratebreakup_Prebook> ratebreakup_Prebooks { get; set; }
    }

    public class SupplierRateBreakUp_Prebook //Added on 10-JAN-26
    {
        public double BuyingPrice { get; set; }
        public double BaseBuyingPrice { get; set; }
        public double SellingPrice { get; set; }
        public double BaseSellingPrice { get; set; }
        public int SupplierId { get; set; }
        public int SuppliercurrencyId { get; set; }
        public string SupplierEmail { get; set; } //Added on 10-APR-26

    }

    public class AgentRateBreakUp_Prebook //Added on 10-JAN-26
    {
        public int AgentId { get; set; }
        public int AgentCurrencyId { get; set; }
        public double AgentBuyingPrice { get; set; }
        public double AgentSellingPrice { get; set; }
        public double SubagentBuyingPrice { get; set; }
        public double SubagentSellingPrice { get; set; }
        public double BaseAgentBuyingPrice { get; set; }
        public double BaseAgentSellingPrice { get; set; }
        public double BaseSubagentBuyingPrice { get; set; }
        public double BaseSubAgentSellingPrice { get; set; }

    }

    //public class HotelData_Prebook //Added on 10-JAN-26
    //{
    //    public string BedType { get; set; }
    //    public string MealPlan { get; set; }
    //    public int NoofNights { get; set; }
    //    public int NoofRoom { get; set; }
    //    public int HotelId { get; set; }
    //    public string RoomTypeId { get; set; }
    //    public string RoomTypeName { get; set; }
    //    public string SepplierRefNo { get; set; }
    //    public string SupplierName { get; set; }
    //    public string OfferCode { get; set; }
    //    public string OfferDescription { get; set; }
    //    public string RoomEssentialInformation { get; set; }
    //    public string HotelAddress { get; set; }
    //    public string RoomMealInfo { get; set; }
    //    public string HotelContactNo { get; set; }

    //}

    public class HotelPolicy_Prebook
    {
        public int RoomNumber { get; set; }
        public string FromDate { get; set; }
        public string Todate { get; set; }
        public double CancellationAmount { get; set; }
        public double AgentcancellationAmount { get; set; }
        public double CompanyCancellationAmount { get; set; }
        public double BaseCancellationAmount { get; set; }
        public double BaseAgentcancellationAmount { get; set; }
        public double BaseCompanyCancellationAmount { get; set; }
    }

    public class Ratebreakup_Prebook
    {
        public string RoomTypeName { get; set; }
        public string MealName { get; set; }
        public string RateType { get; set; }
        public DateTime Date { get; set; }
        public string DayOfWeek { get; set; }
        public double Rate { get; set; }
        public int SupplierCurrencyId { get; set; }
        public double BaseRate { get; set; }
    }
    public class PreBookResponse
    {
        public PreBookRS PreBookRS { get; set; }

        public List<_Error> Error = new List<_Error>();

        //new version class added on 10-JAN-26
        public UserCart_Prebook userCart_Prebook { get; set; }
        public List<UserCartDetails_Prebook> userCartDetails_Prebook { get; set; }
        //public SupplierRateBreakUp_Prebook supplierRateBreakUp_Prebook { get; set; }
        //public AgentRateBreakUp_Prebook agentRateBreakUp_Prebook { get; set; }
        //public HotelData_Prebook hotelData_Prebook { get; set; }
        //public HotelPolicy_Prebook hotelPolicy_Prebook { get; set; }
        //public Ratebreakup_Prebook ratebreakup_Prebook { get; set; }
    }


    public class PreBookStatus
    {
        public static string OK = "Bookable";
        public static string NotOk = "Error";
        public static string PreiceChange = "PriceChange";
        public static string NotAllowed = "NotAllowed";

    }

    #endregion

    public class PreBookResponse_Supp //230425
    {
        public PreBookResponse PreBookResponse { get; set; }
        public string Provider { get; set; }
    }

    #region Book Class
    public class BookRS
    {

        public Int64 BookingId { get; set; }
        public string ReferenceNo { get; set; }
        public string InternalReference { get; set; }
        public string Currency { get; set; }

    }

    public class BookResponse_newapi //010825
    {
        public BookResponse BookResponse { get; set; }
        public DataforLog DataforLog { get; set; }
    }
    public class BookResponse
    {
        public BookRS BookRS { get; set; }
        public List<_Error> Error = new List<_Error>();
    }

    public class BookResponse_Supp
    {
        public BookResponse BookResponse { get; set; }

        public string Ref { get; set; }
        public string IntRef { get; set; }
        public string Provider { get; set; }
    }

    #endregion

    #region BookingDetalsClass

    public class BookingDetailResponse_Supp
    {
        public BookingDetailResponse BookingDetailResponse { get; set; }
        public string Ref { get; set; }
        public string IntRef { get; set; }
        public string Provider { get; set; }
    }

    public class BookingDetailResponse_newapi
    {
        public BookingDetailResponse BookingDetailResponse { get; set; }
        public DataforLogBD DataforLogBD { get; set; }
        

    }
    public class BookingDetailResponse
    {
        public BookingDetailRS BookingDetailRS { get; set; }
        public List<_Error> Error = new List<_Error>();
    }

    public class BookingDetailRS
    {
        public HotelOption_Detail HotelOption { get; set; }
        public string Currency { get; set; }

    }

    public class HotelOption_Detail
    {
        public string HotelName { get; set; }
        public string HotelId { get; set; }
        public string Nationality { get; set; }
        public Int64 BookingId { get; set; }
        public string InternalReference { get; set; }
        public string ReferenceNo { get; set; }

        //public string Status { get; set; }
        public string CheckInDate { get; set; }
        public string CheckOutDate { get; set; }
        public string BookingDate { get; set; }
        //public string DeadLineDate { get; set; }
        public string LeadPaxName { get; set; }

        public decimal TotalPrice { get; set; }

        public List<HotelRoom_Detail> HotelRooms { get; set; }
    }

    public class HotelRoom_Detail
    {
        public string RoomNo { get; set; }
        public string RoomTypeName { get; set; }
        public string MealName { get; set; }
        public string MappedMealName { get; set; }
        public decimal Price { get; set; }
        public string BookingStatus { get; set; }

        public List<CancellationPolicy_Detail> CancellationPolicy { get; set; }
        public List<Passenger_Detail> Passenger { get; set; }
    }

    public class CancellationPolicy_Detail
    {
        public string FromDate { get; set; }
        public string ToDate { get; set; }
        public decimal CancellationPrice { get; set; }
    }

    public class Passenger_Detail
    {
        public int IsLead { get; set; }
        public string PaxType { get; set; }
        public string Name { get; set; }
        public string ChildAge { get; set; }
    }
    #endregion


    #region"Request"
    public class HotelRoom
    {
        public string RoomNo { get; set; }
        public string RoomToken { get; set; }
        public Int32 OldBookingDetailID { get; set; } //#Pruvo added on 6-MAY-2024
        //public string CancellationToken { get; set; }
        // public string RateDetailsToken { get; set; }
    }

    public class HotelOption
    {
        public string HotelOptionId { get; set; }
        public Int32 OldBookingId { get; set; } //#Pruvo added on 6-MAY-2024
        public List<HotelRoom> HotelRooms { get; set; }
    }

    public class PreBookRequest
    {
        public string Searchkey { get; set; }
        public HotelOption HotelOption { get; set; }
    }

    public class PreBookOption
    {
        public _HotelList Hotel { get; set; }
        public List<PreBookRoom> Rooms { get; set; }
    }
    public class PreBookRoom
    {
        public X_Roomdetails HotelRooms { get; set; }
        public List<X_PolicyDetails> PolicyDetails { get; set; }
        public List<X_RateBreakupDetails> RateBreakupDetails { get; set; }
    }

    #endregion


    #region "Booking Request"
    public class HotelRoomBookRQ
    {
        public int UniqueId { get; set; }
        //public int OldBookingDetailID { get; set; }//@#Pruvo new added on 4-MAY-2024
        public int RoomNo { get; set; }
        public int IsLead { get; set; }
        public string PaxType { get; set; }
        public string Prefix { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string ChildAge { get; set; }
    }

    public class BookRQ
    {
        public string BookingToken { get; set; }
        public decimal TotalPrice { get; set; } = 0;
        public string InternalReference { get; set; }
        public string OldBookRef { get; set; }//@#Pruvo
        public string OldBookingId { get; set; }//@#Pruvo new added on 22-APR-2024
        public List<HotelRoomBookRQ> HotelRooms { get; set; }
    }

    public class BookRequest
    {
        public string SearchKey { get; set; }
        public BookRQ BookRQ { get; set; }


    }

    public class BookRequest_newversion
    {
        public string SearchKey { get; set; }
        public BookRQ BookRQ { get; set; }

        public UserCart userCart { get; set; }
    }

    public class UserCart
    {
        public int UserCartId { get; set; }
        public int SupplierCurrencyId { get; set; }
        public int ServiceId { get; set; }
        public string ServiceName { get; set; }
        public int CountryId { get; set; }
        public int CityId { get; set; }
        //new
        public int HotelId { get; set; }
        public string Nationality { get; set; }
        public string XMLHotelId { get; set; }
        public int NoOfNight { get; set; }  

        public List<UserCartDetails> UserCartDetails { get; set; }
    }

    public class UserCartDetails //Added on 10-JAN-26
    {
        public int UserCartId { get; set; }
        public int UserCartDetailId { get; set; }
        public int NoofAdult { get; set; }
        public int NoofChild { get; set; }
        public int NoofInfant { get; set; }
        public string ChildAge { get; set; }//140326
        public double ServiceBuyingAmount { get; set; }
        public double BaseServiceBuyingAmount { get; set; }
        public string CheckInDate { get; set; }
        public string CheckOutDate { get; set; }
        public string SupplierName { get; set; }
        public string TechName { get; set; }
        //new
        public string MealPlan { get; set; }
        public string RoomTypeId { get; set; }
        public string PurchaseToken { get; set; }
        public string EachoToken { get; set; }
        public int RoomNo { get; set; }
        public string ChildAge1 { get; set; }//040526
        public string ChildAge2 { get; set; }//040526

    }

    public class BookResponse_newversion
    {
        public List<_Error> Error = new List<_Error>();
        public List<AgentVoucher> agentVouchers { get; set; }
    }

    public class AgentVoucher
    {
        public int UserCartId { get; set; }
        public int UserCartDetailId { get; set; }
        public string AgentVoucherNo { get; set; }
        public string XMLRequest { get; set; }
        public string XMLResponse { get; set; }
        public string SupplierName { get; set; }
        public string RoomStatus { get; set; }
    }


    #endregion
    #region "BookingResponse"
    public class HotelRoomBookRS
    {
        public int RoomNo { get; set; }
        public string Status { get; set; }
    }

    //public class BookRS
    //{
    //    public Int64 BookingId { get; set; }
    //    public decimal TotalPrice { get; set; }
    //    public string BookingStatus { get; set; }
    //    public string ReferenceNo { get; set; }
    //    public List<HotelRoomBookRS> HotelRooms { get; set; }
    //}

    #endregion

    #region BookingDetail
    #region "Request"
    public class BookingDetailRQ
    {
        public Int64 BookingId { get; set; }
        public string InternalReference { get; set; }
        public string ReferenceNo { get; set; }
        public int AgentId { get; set; }
    }

    public class BookingDetailRequest
    {
        public BookingDetailRQ BookingDetailRQ { get; set; }
    }
    #endregion

    #region "Response"

    public class HotelRoom_Detail_dmc
    {
        public string RoomNo { get; set; }
        public string RoomTypeName { get; set; }
        public string MealName { get; set; }
        public decimal Price { get; set; }
        public string BookingStatus { get; set; }
        public Int64 BookingDetailId { get; set; }


        public List<CancellationPolicy_Detail> CancellationPolicy { get; set; }
        public List<Passenger_Detail> Passenger { get; set; }
    }


    public class HotelOption_Detail_dmc
    {
        public string HotelName { get; set; }
        public string HotelId { get; set; }
        public string Nationality { get; set; }
        public Int64 BookingId { get; set; }
        public string InternalReference { get; set; }
        public string ReferenceNo { get; set; }

        //public string Status { get; set; }
        public string CheckInDate { get; set; }
        public string CheckOutDate { get; set; }
        public string BookingDate { get; set; }
        //public string DeadLineDate { get; set; }
        public string LeadPaxName { get; set; }

        public decimal TotalPrice { get; set; }

        public List<HotelRoom_Detail_dmc> HotelRooms { get; set; }
    }


    public class BookingDetailRS_dmc
    {
        public HotelOption_Detail_dmc HotelOption { get; set; }
        public string Currency { get; set; }

    }


    public class BookingDetailResponse_dmc
    {
        public BookingDetailRS_dmc BookingDetailRS { get; set; }
        public List<_Error> Error = new List<_Error>();
    }

    public class BookingDetailResponse_dmc_Supp
    {
        public BookingDetailResponse_dmc BookingDetailResponse_dmc { get; set; }
        public string Ref { get; set; }
        public string IntRef { get; set; }
        public string Provider { get; set; }
    }
    #endregion
    #endregion

    #region Request
    public class CheckHotelCancellationChargesRQ
    {
        public Int64 BookingId { get; set; }
        public string ReferenceNo { get; set; }
        public string InternalReference { get; set; }
    }

    public class CheckChargesRequest
    {
        public CheckHotelCancellationChargesRQ CheckHotelCancellationChargesRQ { get; set; }
    }
    #endregion
    #region "Response"
    public class HotelRoom_Policy
    {
        public Int64 BookingDetailId { get; set; }
        public string RoomTypeName { get; set; }
        public decimal Price { get; set; }
        public decimal RefundAmount { get; set; }
    }

    public class HotelOption_Policy
    {
        public Int64 BookingId { get; set; }
        public decimal TotalPrice { get; set; }
        public decimal TotalCharge { get; set; }
        public decimal TotalRefund { get; set; }
        public string CancelCode { get; set; }
        public List<HotelRoom_Policy> HotelRooms { get; set; }
    }

    public class CheckHotelCancellationChargesRS
    {
        public HotelOption_Policy HotelOption { get; set; }
        public string Currency { get; set; }

    }

    public class CheckChargesResponse
    {
        public CheckHotelCancellationChargesRS CheckHotelCancellationChargesRS { get; set; }
        public List<_Error> Error = new List<_Error>();

    }
    #endregion

    #region "CancelBooking"
    #region "Request"
    public class CancelRQ
    {
        public Int64 BookingId { get; set; }
        public string Reason { get; set; }
        public Int64 BookingDetailId { get; set; }

        public string ReferenceNo { get; set; }
        public string InternalReference { get; set; }
        public string CancelCode { get; set; }
        public int CancelAll { get; set; }
        public int CancelOldBooking { get; set; }//@#Pruvo
    }

    public class CancelReuest
    {
        public Int64 BookingId { get; set; }
        public Int64 BookingDetailId { get; set; }
        public string Reason { get; set; }
        public string Techname { get; set; }
        public string Provider { get; set; }
        public string AgentVoucherno { get; set; }

        public string ReferenceNo { get; set; }
        public string RoomtypId { get; set; }



        //public string AgentVoucherno { get; set; }
        //public string InternalReference { get; set; }
        //public string CancelCode { get; set; }
        //public int CancelAll { get; set; }
        //public int CancelOldBooking { get; set; }//@#Pruvo
    }

    public class CancelBookingRequest
    {
        public CancelRQ CancelRQ { get; set; }
    }
    public class CancelBookingRequest_new
    {
        public string Token { get; set; }
       
        public CancelReuest CancelRQ { get; set; }
    }
    #endregion
    #region "Response"
    public class HotelRoom_Cancel1
    {
        public string Message { get; set; }
        public int CancelStatus { get; set; }
        public string xmlrequest { get; set; } = "";
        public string xmlresponse { get; set; } = "";



    }
    public class HotelRoom_Cancel
    {
        public string RoomTypeName { get; set; }
        public string Message { get; set; }
        public int CancelStatus { get; set; }

    }

    public class HotelOption_Cancel
    {
        public Int64 BookingId { get; set; }
        public List<HotelRoom_Cancel> HotelRooms { get; set; }
    }

    public class CancelRS
    {
        public HotelOption_Cancel HotelOption { get; set; }
        public string Currency { get; set; }

    }

    public class CancelBookingResponse
    {
        public CancelRS CancelRS { get; set; }
        public List<_Error> Error = new List<_Error>();

    }

    public class CancelBookingResponse_Supp //230425
    {
        public CancelBookingResponse CancelBookingResponse { get; set; }
        public string PNRNo { get; set; }

    }

    public class CancelBookingResponse_newapi //230425
    {
        public CancelBookingResponse CancelBookingResponse { get; set; }
        public string PNRNo { get; set; } = "";

    }
    public class CancelBookingResponse_newversion //230425
    {
        public CancelBookingResponse CancelBookingResponse { get; set; }
        public string PNRNo { get; set; } = "";

    }

    public class CancelBookingResponse_newversion1
    {
        public HotelRoom_Cancel1 hotelRoom_Cancel { get; set; }
        public List<_Error> Error = new List<_Error>();

    }

    #endregion
    #endregion




    public class CancellationPolicyRSMulti //110425
    {
        public CancellationPolicyRS_Multi_v1 res { get; set; }
        public int StatusCode { get; set; }
    }

    public class ApiResponse //110425
    {
        public string response { get; set; }
        public int StatusCode { get; set; }

    }

    public class ApiRequest<T> //110425
    {
        public T Response { get; set; }
        public int StatusCode { get; set; }
    }


    public class GeoStaticDataRS
    {
        public int cityid { get; set; }
        public string cityname { get; set; }
        public int countryid { get; set; }
        public string countryname { get; set; }
    }
    public class GeoStaticDataRQ
    {
        public int onlyhotel { get; set; }
        public int refresh { get; set; }

    }

    #region recheck - 270525
    public class HotelCancelation
    {
        public DataTable dtHotelCancelation { get; set; }
        public string ISBooking { get; set; } = "Available";
    }

    public class CancellationDto
    {
        public DataSet dsHotelData { get; set; }
        public string ISBooking { get; set; } = "Available";
    }
    #endregion

    #region Pruvo

    public class DataforLog
    {
        public Int64 hotelid { get; set; } = 0;
        public string hotelname { get; set; } = "";
        public string provider { get; set; } = "";
    }

    public class Class_Pruvo
    {
        public class RQ
        {
            public string FromDate { get; set; }
            public string ToDate { get; set; }
            public string BookingId { get; set; }
            public string InternalReference { get; set; }
            public string ReferenceNo { get; set; }
            public int GetRebookingData { get; set; }
        }
        public class _Error
        {
            public string type { get; set; }
            public string code { get; set; }
            public string description { get; set; }
        }
        public class Rootobject
        {
            public List<_Error> Error = new List<_Error>();
            public BookingDetailRS BookingDetailRS { get; set; }
        }

        public class Rootobject_supp //added on 280525
        {
            public Rootobject Rootobject { get; set; }
            
            public string Provider { get; set; }
            public long hotelid { get; set; }
            public string HotelName { get; set; }
            public string Ref { get; set; }
            public string IntRef { get; set; }
        }
        public class BookingDetailRS
        {
            public List<HotelOption> HotelOption { get; set; }
            public string Currency { get; set; }
        }

        public class CancellationPolicy
        {
            public DateTime FromDate { get; set; }
            public DateTime ToDate { get; set; }
            public double CancellationPrice { get; set; }
        }

        public class HotelOption
        {
            public string HotelName { get; set; }
            public string HotelId { get; set; }
            public string Nationality { get; set; }
            public int BookingId { get; set; }
            public string InternalReference { get; set; }
            public string ReferenceNo { get; set; }
            public DateTime CheckInDate { get; set; }
            public DateTime CheckOutDate { get; set; }
            public string BookingDate { get; set; }
            public string LeadPaxName { get; set; }
            //public double TotalPrice { get; set; }
            public double BuyingPrice { get; set; }
            public List<HotelRoom> HotelRooms { get; set; }
        }

        public class HotelRoom
        {
            public string RoomNo { get; set; }
            public string RoomTypeName { get; set; }
            public string MealName { get; set; }
            public string MappedMealName { get; set; }
            //public double Price { get; set; }
            public double BuyingPrice { get; set; }
            public string BookingStatus { get; set; }
            public int BookingDetailsId { get; set; }
            public string EssentialInformation { get; set; }
            public bool IsCombineRoom { get; set; }
            public List<CancellationPolicy> CancellationPolicy { get; set; }
            public List<Passenger> Passenger { get; set; }
        }

        public class Passenger
        {
            public int RoomNo { get; set; }
            public int IsLead { get; set; }
            public string PaxType { get; set; }
            public string Name { get; set; }
            public string ChildAge { get; set; }
        }

        #region ReBook_PruvoMarketPlace
        public class ReBook_PruvoMarketPlace
        {
            public int OldBookingId { get; set; }
            public int OldBookingDetailID { get; set; }
            public double newPrice { get; set; }
            public string pruvoItinerary { get; set; }
            public string InternalReference { get; set; }
            public string pruvoUserid { get; set; }
            public string originalDocket { get; set; }
        }
        #endregion



    }

    #endregion

    #region BookingDetail
    public class DataforLogBD
    {
        public Int64 hotelid { get; set; } = 0;
        public string hotelname { get; set; } = "";
        public string provider { get; set; } = "";
        public string Ref { get; set; } = "";
        public string IntRef { get; set; } = "";
    }
    #endregion


    public class SaveBookingDatabase
    {
        public string CreditCardCharges { get; set; }
        public int IsBothB2BB2C { get; set; }
        public int GuestUserId { get; set; }
        public string LocalCurrency { get; set; }
        public decimal LocalCurrencyFactor { get; set; }
        public int AgentId { get; set; }
        public string ShoppingTransNo { get; set; }
        public string PaymentType { get; set; }
        public int SubUserId { get; set; }
        public string HostIp { get; set; }
        public string HostCountry { get; set; }
        public int IsVouchered { get; set; }
        public int IsConfirm { get; set; }
        public string QuotationName { get; set; }
        public List<PassengerDetailsFor> lstPassenger { get; set; }
    }
    public class PassengerDetailsFor
    {
        public int UserCartId { get; set; }
        public string ServiceType { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public List<PickUpPoints> lstPickup = new List<PickUpPoints>();
        public string PickUp { get; set; }
        public string Passport { get; set; }
        public string Photo { get; set; }
        public int IsLead { get; set; }
        public string Email { get; set; }
        public string Nationality { get; set; }
        public string Mobile { get; set; }
        public string Message { get; set; }
        public string Prefix { get; set; }
        public string Gender { get; set; }
        public DateTime BirthDate { get; set; }
        public int RoomNo { get; set; }
        public string RoomType { get; set; }
        public string PaxType { get; set; }
        public string ChildAge { get; set; }//140326
        public int IsCombo { get; set; }
        public string AgentReferenceNo { get; set; }
        public string TravelPNR { get; set; }
        public string PassPortExpiry { get; set; }

    }
    public class PickUpPoints
    {
        public int CartId { get; set; }
        public string Pickup { get; set; }
    }
    public class dsCartDetails_Table0
    {
        public int UserCartID { get; set; }
        public int GuestUserId { get; set; }
        public string CheckInDate { get; set; }
        public string CheckOutDate { get; set; }
        public string StrCheckInDate { get; set; }
        public string ServiceId { get; set; }
        public string ServiceName { get; set; }
        public string ServiceType { get; set; }
        public string XMLHotelId { get; set; }
        public string OfficeId { get; set; }
        public string ContractId { get; set; }
        public string XMLCountryId { get; set; }
        public string XMLCityId { get; set; }
        public string Nationality { get; set; }
        public int PackageId { get; set; }
        public string SupplierCurrency { get; set; }
        public int NoOfNight { get; set; }
        public string NationalityName { get; set; }
        public int IsVouchered { get; set; }
        public int CityId { get; set; }
        public string CountryId { get; set; }
    }

    public class dsCartDetails_Table1
    {
        public int UserCartID { get; set; }
        public int UserCartDetailId { get; set; }
        public int GuestUserId { get; set; }
        public int RoomNumber { get; set; }
        public string RoomTypeId { get; set; }
        public int NoOfAdults { get; set; }
        public int NoOfChild { get; set; }
        public int ChildAge1 { get; set; }
        public int ChildAge2 { get; set; }
        public string SupplierName { get; set; }
        public string MealPlanCode { get; set; }
        public string ContractID { get; set; }
        public string spui { get; set; }
        public string purchaseToken { get; set; }
        public double Rate { get; set; }
        public double FinalSellingPrice { get; set; }
        public string Occupancy { get; set; }
        public string EachoToken { get; set; }
        public double CompanyBuyingPriceExpedia { get; set; }
        public double OurProfit { get; set; }
        public double GrossProfitOnline { get; set; }
        public double CurrencyFactor { get; set; }
        public double CompanyBuyingPrice { get; set; }
        public string XmlNationality { get; set; }
        public int IsVouchered { get; set; }
    }


    public class AgentVoucherClass
    {
        public string UserCartId { get; set; }
        public string UserCartDetailId { get; set; }
        public string AgentVoucherNo { get; set; }
        public string XMLRequest { get; set; }
        public string XMLResponse { get; set; }
        public string RoomStatus { get; set; }
        public string SupplierName { get; set; }
        public string NewAllocationId { get; set; }
        public string CompanyBuyingPrice { get; set; }

    }

    #region vervo
    public class HotelProviderMappingV3

    {

        public int VervotechId

        {

            get; set;

        }

        public int Count

        {

            get; set;

        }

        public List<ProviderHotelV3> ProviderHotels

        {

            get; set;

        }

        public bool Status

        {

            get; set;

        }

        public string Message

        {

            get; set;

        }

        public int StatusCode

        {

            get; set;

        }

    }
    public class ProviderHotelV3

    {

        public string ProviderHotelId

        {

            get; set;

        }

        public string ProviderFamily

        {

            get; set;

        }

        public List<string> ChannelIds

        {

            get; set;

        }

    }
    #endregion
    public class ApiResponsecheckvervo
    {
        public int status { get; set; }
        public bool success { get; set; }
        public string message { get; set; }
        public string timestamp { get; set; }
        public bool isVervo { get; set; }
    }

    #region agoda

    public class AgodaSearchPara
    {
        public List<string> hotels { get; set; }
        public string checkIn { get; set; }
        public string checkOut { get; set; }
        public Occupancy occupancy { get; set; }
        public string requestType { get; set; }
        public string token { get; set; }
        public TokenData TokenData { get; set; }
        public string ratePlanCode { get; set; }
        public string currency { get; set; }
        public double totalPrice { get; set; }
        public string hotel { get; set; }
        public string metadata { get; set; }

    }

    public class Occupancy
    {
        public int rooms { get; set; }
        public int adults { get; set; }
        public int children { get; set; }
        public List<int> childrenAges { get; set; }
    }

    public class FilterUsed
    {
        public string ratePlanCode { get; set; }
        public double totalPrice { get; set; }
        public bool ratePlanSearch { get; set; }
        public string OriginalcheckOut { get; set; }
        public string OriginalcheckIn { get; set; }
        //public List<List<Roomarray>> rooms = new List<List<Roomarray>>();
    }

    public class AgodaSearchError
    {
        public string responseStatus { get; set; }
        public string version { get; set; }
        public string remark { get; set; }

    }
    public class AgodaVersion
    {
        public string Version { get; set; }
        public AgodaVersion()
        {

            //Version = "v1.9";
            Version = "v1.34";

        }

    }

    public class AgodaListResponse
    {


        public string responseStatus { get; set; }
        public string version { get; set; }
        public string checkIn { get; set; }
        public string checkOut { get; set; }
        public string currency { get; set; }
        public List<Hotelarraylist> hotels { get; set; }
        public AgodaListResponse()
        {
            responseStatus = "SUCCESS";
            version = new AgodaVersion().Version;
        }
    }

    public class Hotelarraylist
    {
        public string code { get; set; }
        public string name { get; set; }
        public List<Roomarray> rooms { get; set; }

        //public List<List<Roomarray>> rooms = new List<List<Roomarray>>();
    }
    public class Roomarray
    {
        public string code { get; set; }
        public string name { get; set; }
        public List<Ratearray> rates { get; set; }
    }
    public class Ratearray
    {
        public string subSupplierId { get; set; }
        public string boardCode { get; set; }
        public string ratePlanCode { get; set; }
        public int channel { get; set; }
        public int allotment { get; set; }
        public decimal price { get; set; }
        public CancellationPolicyarray cancellationPolicy { get; set; }
        public string metadata { get; set; }
        //public int confirmByMins { get; set; }

        public class CancellationPolicyarray
        {
            public List<Policyarray> policies { get; set; }
        }
        public class Policyarray
        {
            public double amount { get; set; }
            public int daysBeforeCheckIn { get; set; }
        }
    }

    public class AgodaBookingRQ
    {
        public string bookingReferenceId { get; set; }
        public string checkIn { get; set; }
        public string checkOut { get; set; }
        public string hotel { get; set; }
        public string roomCode { get; set; }
        public string boardCode { get; set; }
        public string ratePlanCode { get; set; }
        public int channel { get; set; }
        public string currency { get; set; }
        public int quantity { get; set; }
        public decimal totalPrice { get; set; }
        public Guest guest { get; set; }
        public List<RoomBook> rooms { get; set; }
        public string token { get; set; }
        public TokenData TokenData { get; set; }

        public string metadata { get; set; }
    }

    public class Guest
    {
        public string firstName { get; set; }
        public string lastName { get; set; }
    }

    public class RoomBook
    {
        public int adults { get; set; }
        public int children { get; set; }
        public List<int> childrenAges { get; set; }
        public int price { get; set; }
    }
    public class AgodaBookingRS
    {
        public string responseStatus { get; set; }
        public string version { get; set; }
        public string referenceId { get; set; }
        public string bookingReferenceId { get; set; }
        public string status { get; set; }
        public string remark { get; set; }




        public AgodaBookingRS()
        {
            responseStatus = "SUCCESS";
            version = new AgodaVersion().Version;
        }
    }
    public class AgodaBookingErrorRS_New
    {
        public string responseStatus { get; set; }
        public string version { get; set; }

        public string remark { get; set; }




        public AgodaBookingErrorRS_New()
        {
            // responseStatus = "FAILED";
            version = new AgodaVersion().Version;
        }
    }

    public class AgodaBookingErrorRS
    {
        public string responseStatus { get; set; }
        public string version { get; set; }
        public string referenceId { get; set; }
        public string bookingReferenceId { get; set; }
        public string status { get; set; }

        public string remark { get; set; }
        public AgodaBookingErrorRS()
        {
            // responseStatus = "FAILED";
            version = new AgodaVersion().Version;
        }
    }

    public class CommonXMLBookResponse
    {
        public List<_Error> Error { get; set; }
        // public BookResponse_newversion_new BookRS { get; set; }  //25042026
        public BookRS BookRS { get; set; }

    }

    #endregion

    #region "NEW API For AGODA"
    public class AgodaBookingIDsRS
    {
        public string responseStatus { get; set; }
        public string version { get; set; }
        public List<BookingId> bookingIds { get; set; }
    }
    public class BookingId
    {
        public string supplierBookingId { get; set; }
        public string bookingReferenceId { get; set; }
        public string bookingStatus { get; set; }
    }
    public class AgodaBookingIDsRQ
    {
        public DateTime? from { get; set; }
        public DateTime? to { get; set; }
    }


    public class AgodaBookingDetailRSF
    {

        public class AgodaBookingDetailRS
        {
            public string responseStatus { get; set; }
            public string version { get; set; }
            public string bookingReferenceId { get; set; }
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
            public string supplierBookingId { get; set; }
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
            public string bookingStatus { get; set; }
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
            public string hotel { get; set; }
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
            public string checkIn { get; set; }
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
            public string checkOut { get; set; }
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
            public string roomCode { get; set; }
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
            public string boardCode { get; set; }
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
            public string ratePlanCode { get; set; }
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
            public int channel { get; set; }
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
            public int quantity { get; set; }
            //[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
            //public string specialRequests { get; set; }
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
            public GuestCount GuestCount { get; set; }
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
            public Price Price { get; set; }
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
            public CancellationInfo CancellationInfo { get; set; }
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
            public List<int> benefits { get; set; }
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
            public string hotelConfirmationReference { get; set; }

            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
            public string remark { get; set; }
        }
        public class GuestCount
        {
            public int adults { get; set; }
            public List<int> childrenAges { get; set; }
        }
        public class Price
        {
            public double payoutPrice { get; set; }
            public string currency { get; set; }
            public double commissionPercentage { get; set; }
        }
        public class CancellationInfo
        {
            public decimal cancellationFee { get; set; }
            public string cancellationDate { get; set; }
        }
    }

    public class AgodaBookingDetailRQ

    {

        public string bookingReferenceId { get; set; }

    }


    #endregion

    #region agoda cancel
    //public class HotelRoom_Cancel_agoda
    //{
    //    public string RoomTypeName { get; set; }
    //    public string Message { get; set; }
    //    public int CancelStatus { get; set; }
    //    public double AgentCharge { get; set; }
    //}
    //public class HotelOption_Cancel_agoda
    //{
    //    public Int64 BookingId { get; set; }
    //    public List<HotelRoom_Cancel_agoda> HotelRooms { get; set; }
    //}
    //public class CancelRS_agoda
    //{
    //    public HotelOption_Cancel_agoda HotelOption { get; set; }
    //    public string Currency { get; set; }
    //}

    //public class CancelBookingResponse_agoda
    //{
    //    public CancelRS_agoda CancelRS { get; set; }
    //    public List<XConnect.Models._Error> Error = new List<XConnect.Models._Error>();
    //}
    //public class AgodaCancelReturn
    //{
    //    public string ReturnStatus { get; set; }
    //    public double AgentCancellationAmount { get; set; }
    //}
    //public class AgodaCancelRS
    //{
    //    public string responseStatus { get; set; }
    //    public string version { get; set; }
    //    public string referenceId { get; set; }
    //    public double cancellationFees { get; set; }

    //    public AgodaCancelRS()
    //    {
    //        responseStatus = "SUCCESS";
    //        version = new AgodaVersion().Version;
    //    }
    //}
    //public class AgodaCancelRQ
    //{
    //    public string referenceId { get; set; }

    //    public string token { get; set; }
    //    public TokenData TokenData { get; set; }

    //}
    #endregion

    public class _FRatebreakup
    {
      
        public string RateType { get; set; }
        public string Date { get; set; }
        public string DateOfWeek { get; set; }
        public string Rate { get; set; }
        public string  Surcharge { get; set; }
        public string  CompanyCharge { get; set; }
        public string  Total { get; set; }
        public string BuyingPrice { get; set; }
    }

}

