using System.Data;
using Newtonsoft.Json;
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
        public bool IsPackage { get; set; } //20-may-26
        public bool IsFromB2B { get; set; } //20-may-26

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
        public int Id { get; set; }
        public int HotelId { get; set; }
        public int SupplierId { get; set; }
        public int RoomId { get; set; }
        public string SupplierRoomName { get; set; }
        public string StandardRoomName { get; set; }
    }
    public class RoomMappingMongo
    {
        public string _id { get; set; }
        public string s { get; set; }
        public string r { get; set; }
    }
    public class SupplierperformanceReport
    {
        public int totalcount { get; set; }
        public string suppliername { get; set; }
        public string NoCount { get; set; }
        public string SuccessCount { get; set; }
        //public string TooManyRQ { get; set; }
        public string TimeoutCount { get; set; }
        public string PriceChangeCount { get; set; }
        public string MappingCount { get; set; }
        public decimal NoCountPer { get; set; }
        public decimal SuccessPer { get; set; }
        public decimal TimeOutPer { get; set; }
        public decimal PriceChangePer { get; set; }
        public decimal MappingPer { get; set; }

        public string Avg { get; set; }
    }
    public class SprRequest
    {
        public string SupplierName { get; set; }
        public int AgentID { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime Todate { get; set; }

        public string ReportType { get; set; }
        public decimal TimeOut { get; set; }
        public string IsApi { get; set; }
    }

    #endregion
    public class HotelReviewRequest
    {
        public int HotelId { get; set; }
    }
    public class GuestReviewModel
    {
        public string ReviewerName { get; set; }
        public string Rating { get; set; }
        public string ReviewText { get; set; }
        public string VerificationSource { get; set; }
        public string StayDate { get; set; }
        public string TravelCompanion { get; set; }
        public string TripReason { get; set; }
        public string ReviewSubmitDate { get; set; }
        public string Title { get; set; }

        public List<ManagementResponse> ManagementResponses { get; set; }
    }

    public class ManagementResponse
    {
        public DateTime Date { get; set; }
        public string Text { get; set; }
    }

    #region getroomdecription
    public class GetHotelroomdata
    {
        public int HotelId { get; set; }
    }

    public class Property_ean
    {
        [JsonProperty("id")]
        public Dictionary<string, PropertyObject_ean> AnyKey { get; set; }

    }
    public class PropertyObject_ean
    {
        [JsonProperty("property_id")]
        public string PropertyId { get; set; }
        [JsonProperty("Name")]
        public string Name { get; set; }
        [JsonProperty("rooms")]
        public Dictionary<string, RoomInfo_ean> Rooms { get; set; }
    }
    public class RoomInfo_ean
    {
        [JsonProperty("id")]
        public string Id { get; set; }
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("descriptions")]
        public RoomDescriptions_ean RoomDescriptions { get; set; }
        [JsonProperty("amenities")]
        public Dictionary<string, RoomAmenity_ean> RoomAmenities { get; set; }
        [JsonProperty("images")]
        public List<RoomImages_ean> Images { get; set; }
        [JsonProperty("bed_groups")]
        public Dictionary<string, BedGroup_ean> BedGroups { get; set; }
        [JsonProperty("area")]
        public Area_ean Area { get; set; }
        [JsonProperty("views")]
        public Dictionary<string, ViewInfo_ean> Views { get; set; }
        [JsonProperty("occupancy")]
        public Occupancy Occupancy { get; set; }
    }
    public class RoomDescriptions_ean
    {
        [JsonProperty("overview")]
        public string Overview { get; set; }
    }
    public class RoomAmenity_ean
    {
        [JsonProperty("id")]
        public string Id { get; set; }
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("value")]
        public string Value { get; set; }
        [JsonProperty("categories")]
        public List<string> categories { get; set; }

    }
    public class RoomImages_ean
    {
        [JsonProperty("hero_image")]
        public string HeroImage { get; set; }
        [JsonProperty("category")]
        public int Category { get; set; }
        [JsonProperty("caption")]
        public string Caption { get; set; }
        [JsonProperty("links")]
        public Dictionary<string, RoomImageLink_ean> RoomImageLinks { get; set; }
    }
    public class RoomImageLink_ean
    {
        [JsonProperty("method")]
        public string Method { get; set; }
        [JsonProperty("href")]
        public string Href { get; set; }
        [JsonProperty("expires")]
        public string Expires { get; set; }
    }
    public class BedGroup_ean
    {
        [JsonProperty("id")]
        public string Id { get; set; }
        [JsonProperty("description")]
        public string Description { get; set; }
        [JsonProperty("configuration")]
        public List<BedConfiguration_ean> Configuration { get; set; }
    }
    public class BedConfiguration_ean
    {
        [JsonProperty("type")]
        public string Type { get; set; }
        [JsonProperty("size")]
        public string Size { get; set; }
        [JsonProperty("quantity")]
        public int Quantity { get; set; }
    }
    public class Area_ean
    {
        [JsonProperty("square_meters")]
        public decimal SquareMeters { get; set; }
        [JsonProperty("square_feet")]
        public decimal SquareFeet { get; set; }
    }
    public class ViewInfo_ean
    {
        [JsonProperty("id")]
        public string Id { get; set; }
        [JsonProperty("name")]
        public string Name { get; set; }
    }
    public class Occupancy_ean
    {
        [JsonProperty("max_allowed")]
        public MaxAllowed_ean MaxAllowed { get; set; }
        [JsonProperty("age_categories")]
        public Dictionary<string, AgeCategoryInfo_ean> AgeCategories { get; set; }
    }
    public class MaxAllowed_ean
    {
        [JsonProperty("total")]
        public int Total { get; set; }
        [JsonProperty("children")]
        public int Children { get; set; }
        [JsonProperty("adults")]
        public int Adults { get; set; }
    }
    public class AgeCategoryInfo_ean
    {
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("minimum_age")]
        public int MinimumAge { get; set; }
    }

    public class HotelRoomResult
    {
        public string HotelName { get; set; }
        public List<RoomResult> Rooms { get; set; }
    }

    public class RoomResult
    {
        public string Roomid { get; set; }
        public string RoomName { get; set; }

        public string Description { get; set; }
        public List<string> Images { get; set; }
    }

    #endregion
}
