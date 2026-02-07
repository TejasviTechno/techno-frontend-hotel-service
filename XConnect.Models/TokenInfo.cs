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
}
