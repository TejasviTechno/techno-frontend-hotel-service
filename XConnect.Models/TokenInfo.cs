using System.Data;

namespace XConnect.Models
{
    public class TokenInfo
    {
    }

    public class AuthData
    {
        public TokenData tokenData { get; set; }
        public CommonRequest commonRequest { get; set; }
    }

    public class TokenData
    {
        public int AgentId { get; set; }
        public int RateCategoryId { get; set; }
        public string URL { get; set; }
        public int ParentId { get; set; }
        public string Type { get; set; }
        public int SubUserId { get; set; }

        public int IsHotel { get; set; }
        public string HotelSupplier { get; set; }
        public string WhiteLableUrl { get; set; }
        public string AgentCode { get; set; }
        public int CreditLimitCurrency { get; set; }
        public string CreditLimitCurrencyName { get; set; }
        public CurrencyNames apiCurrency { get; set; }
        public int ApiTimeOut { get; set; }
        public int CompanyId { get; set; }
        public DataTable dtROE { get; set; }
        public int SpeedCategoryId { get; set; }
        public int IsSearchAllow { get; set; }
        public int IsSearchmail { get; set; }
        public int IsAutoVoucher { get; set; }
        public int CitycodeOverpass { get; set; }//@HID

        public string SearchType { get; set; }//@new


        public string hotelname { get; set; } //new 261224

        public Int64 hotelid { get; set; } //new 261224
        public int LogId { get; set; } //new 261224

        public string SiteCurrency { get; set; } //new 210225


        public string TanentId { get; set; } //24-DEC-25
        public bool CanBookWithinCancellation { get; set; } //10-JAN-26
        public List<DtRoeNew> dtROENew { get; set; } //051125

        public bool IsFromAPI { get; set; } //4-FEB-26

    }

    public class CurrencyNames
    {
        public string CurrencyCode { get; set; }
        public string CurrencyShortname { get; set; }
        public decimal BuyingCurrencyFactor { get; set; }
        public decimal CurrencyMarkup { get; set; }
        public decimal SellingCurrencyFactor { get; set; }
        public decimal SellingCurrencyMarkup { get; set; }
        public string CurrencySymbol { get; set; }
        public decimal ExchangeRate { get; set; }
        public string CurrencyFullName { get; set; }
    }

    #region Report
    public class GetRangeForAPI
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public string ApiName { get; set; }
    }

    public class GetRange
    {
        public int AgentId { get; set; }
        public Int64 MaxId { get; set; }
        public Int64 MinId { get; set; }

    }

    public class ApiOutLogs
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public string fromids { get; set; }
        public string toids { get; set; }
        public string ApiName { get; set; }
    }

    public class Agentlistlogs
    {
        public int AgentID { get; set; }

        public Int64 Id { get; set; }
        public string Request { get; set; }
        public string Response { get; set; }
        public DateTime EntryDate { get; set; }
        public string Token { get; set; }
        public string ApiName { get; set; }
        public string IpAddress { get; set; }
        public string ElapsedTime { get; set; }
        public string Provider { get; set; }
    }
    public class Supplierlogs
    {
        // public int AgentID { get; set; }

        public Int64 Id { get; set; }
        public string Request { get; set; }
        public string Response { get; set; }

        public string ErrorCode { get; set; }
        //public string Token { get; set; }
        //public string ApiName { get; set; }
        //public string IpAddress { get; set; }
        //public string ElapsedTime { get; set; }
        public string Refno { get; set; }
        public string Provider { get; set; }
        public DateTime EntryDate { get; set; }
    }
    public class ApiRequest
    {

        public string ReferenceNo { get; set; }
        public string SupplierType { get; set; }
        public string ApiName { get; set; }
    }
    public class ApiRequestRoomMapping
    {

        public string HotelId { get; set; }
        public string RoomName { get; set; }
    }

    public class RoomMappingReport
    {
        public Int64 Id { get; set; }
        public string RoomName { get; set; }
        public string Response { get; set; }
        public string Provider { get; set; }
        public string ProviderHotelId { get; set; }
        public string BoardBasis { get; set; }
        public string RateuaraHotelId { get; set; }
        public string CityId { get; set; }
        public string GVSupplierId { get; set; }
        public string GVRoomName { get; set; }
        public string RoomCode { get; set; }
       
    }
    public class RoomMappingMongo
    {
        public string _id { get; set; }
        public string s { get; set; }
        public string r { get; set; }
    }
    public class SupplierperformanceReport
    {
        public int Id { get; set; }
        public int AgentID { get; set; }

        public string SupplierName { get; set; }
        public string Request { get; set; }
        public string Response { get; set; }
        public string ResponseType { get; set; }
        public string ReportType { get; set; }
        public string RSTimeout { get; set; }
        public DateTime Entrydate { get; set; }
        public string IsApi { get; set; }
    }
    public class SprRequest
    {
        public string SupplierName { get; set; }
        public int AgentID { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime Todate { get; set; }

        public string ReportType { get; set; }

        public string IsApi { get; set; }
    }

    #endregion
}
