using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XConnect.Models.Hotel;
using XConnect.Models;

namespace XConnect.Supplier.HotelDo.Interface
{
    public interface IHoteldo
    {
        void GetHotelDoRequestOnly_async(DataRow[] commonCode, List<StarRating> listRating, DataTable dtROE, HotelSearchParameter SearchPara, List<SupplierDetails> HotelDoDetails, List<SupplierSearchConfig> HotelDoSearchConfig, DataTable dtPreferredDataHotel, ref ConcurrentBag<HotelX> AllTask, string XMLCity, string XMLCountry, string XMLNationality, int Hit, TokenData tokenData);

        void GetHotelDoRequestOnly_async_new(DataRow[] commonCode, List<StarRating> listRating, DataTable dtROE, HotelSearchParameter SearchPara, List<SupplierDetails> lstHotelDoDetails, List<SupplierSearchConfig> lstHotelDoSearchConfig, DataTable dtPreferredDataHotel, ref ConcurrentBag<HotelX> AllTask, int Hit, TokenData tokenData, DataSet dtXMLCity);
        Task<List<Options>> HotelDO_HotelSearchWithRooms(HotelX Item, CommonSearch commonSearch,string SupplierCurrency, string Currency, string ResponseData, string CityCode, string CountryCode, HotelSearchParameter SearchParam, double CurrencyFactorHotelDo, Int32 processTimeout, string ResProccessTimerLogActive, string ResProccessTimeValueforSaveLog, string XmlNationality);
        #region Recheck - 280525
        Task<string> Get_Hoteldo_EInfo(string hotelcode, string Username, string hoteldopassword, string hoteldoURL,string SearchKey);
        #endregion

        //prebook
        Task<PriceCheckCMDto> HotelDoPriceCheck_v2(DataTable dtCheckPriceReqStructure, DataSet dsCartDetails, DataRow[] HotelDoDetails, string UniqueNo, string AgentUniqueNo, int i, string IsFor, double CompanyBuyPrice,  bool IsError,  double itemprice, string Searchkey = "", int AgentId = 0,string TechName="");

        string HotelDoBookingRequest_v2(string PassWord, string AffiliateId, string HotelCode, DataTable dtPassengers, DataSet dsCartDetails, string UniqueNo, bool IsPriceUpdates, bool IsErrors, int i = 0);

        Task<ResponseDto> HotelDoResponseAsync(string Request, string Url, string UserName, string Password, int TimeOut, string Method, string Searchkey = "");

        string HotelDoBookingCancelRequest(string PassWord, string AffiliateId, string BookingId, string ConsecutiveId);
    }
}
