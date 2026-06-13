using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XConnect.Models.Hotel;
using XConnect.Models;

namespace XConnect.Supplier.Booking.Com.Interface
{
    public interface IBooking
    {
        void GetBookingDotComRequestOnly_async(DataRow[] commonCode, List<StarRating> listRating, string SupplierName, HotelSearchParameter SearchPara, List<SupplierDetails> bookingDetails, List<SupplierSearchConfig> bookingSearchConfig, DataTable dtPreferredDataHotel, ref ConcurrentBag<HotelX> AllTask, string XMLCity, string XMLCountry, string Xmlnationality, int Hit, CommonSearchList commonSearchList, DataTable dtROE, TokenData tokendata, double BookingDotComCommison, bool SearchByOneID = true);

        void GetBookingDotComRequestOnly_async_new(DataRow[] commonCode, List<StarRating> listRating, HotelSearchParameter SearchPara, List<SupplierDetails> lstbookingDetails, List<SupplierSearchConfig> lstbookingSearchConfig, DataTable dtPreferredDataHotel, ref ConcurrentBag<HotelX> AllTask, int Hit, CommonSearchList commonSearchList, DataTable dtROE, TokenData tokenData, double BookingDotComCommison, DataSet dtXMLCity, bool SearchByOneID = true, bool HitFromAll = false);
        Task<List<Options>> BookingDotCom_HotelSearchWithRooms(HotelX Item, CommonSearch commonSearch, string WebSiteURL, int SearchTimeOut, string Supplier, DataTable dtAgentDetail, DataTable dtSuplierMarkup, DataTable dtMarkupDetails, bool DebugeMode, string UserName, string PassWord, string MethoodType, string ResponseData, HotelSearchParameter SearchParam, string xmlcity, string xmlcountry, string xmlnationality, string currency, double currencyfactor, AdvancedOptions advancedOptions, Int32 processTimeout, string ResProccessTimerLogActive, string ResProccessTimeValueforSaveLog, DataTable dtROE, double Commision = 7);

        #region Recheck - Added on 28-05-25
        Task<string> Get_bookingDotcom_EInfo(string hotelcode, string ownsystemid, string roomtypeid, HotelSearchParameter SearchPara, string Password, string Action, string Username, string SearchKey);

        Task<string> Get_bookingDotcom_V3_EInfo(string hotelcode, string URL, string XmlCity, string ownsystemid, string roomtypeid, HotelSearchParameter SearchPara, string Password, string Action, string Username, string SearchKey, string Provider, string XmlNationality, string CurrencyCode, DataTable dtROE);
        #endregion

        void GetBookingDotComRequestOnly_async_new_v3(DataRow[] commonCode, List<StarRating> listRating, HotelSearchParameter SearchPara, List<SupplierDetails> lstbookingDetails, List<SupplierSearchConfig> lstbookingSearchConfig, DataTable dtPreferredDataHotel, ref ConcurrentBag<HotelX> AllTask, int Hit, CommonSearchList commonSearchList, DataTable dtROE, TokenData tokenData, double BookingDotComCommison, DataSet dtXMLCity, bool SearchByOneID = true, bool HitFromAll = false);

        Task<List<Options>> BookingDotCom_v3_HotelsearchWithRooms(HotelX item, CommonSearch commonSearch, string xmlCountry, string CityCode, string ResponseData, HotelSearchParameter SearchParam, double CurrencyFactor, DataSet dsAgentDetails, string Provider, DataTable dtCountryCityMarkup, Int32 AgentId, Int32 TariffID, double AgentPercentage, Boolean IsShowSupplier, int SupplierID, int MasterSupplierID, Int32 processTimeout, string ProccessCutOffTime, string ResProccessTimerLogActive, string ResProccessTimeValueforSaveLog, string StoreSupplierResErrorLog, DataTable dtROE, string xmlhotelcode, string UserName, string Password, string currencycode = "USD");


        //prebook 02-07-25

        Task<PriceCheckBDCDto> Get_BookingDotCom_PriceCheck(DataSet dsCartDetails, DataRow[] BDCDetails, string UserName, string Password, int i, int j, DataTable dtCheckPriceReqStructure, string UniqueNo, string AgentUniqueNo, double FinalPrice, bool isForBooking, double CompanyBuyingPrice, string IsFor, double commission, TokenData tokenData, int AgentId, string SearchKey = "", string TechName = "");

        //prebook 23-07-25
        Task<PriceCheckBDCDto> Get_BookingDotCom_PriceCheck_v3(DataSet dsCartDetails, DataRow[] BDCDetails, string UserName, string Password, int i, int j, DataTable dtCheckPriceReqStructure, string UniqueNo, string AgentUniqueNo, double FinalPrice, bool isForBooking, double CompanyBuyingPrice, string IsFor, double commission, TokenData tokenData, int AgentId, string Searchkey = "", string TechName = "");

        Task<ResponseDto> BookingDotComv3Response(string Request, string Url, string UserName, string Password, int TimeOut);

        string Get_BookingDotCom_BookingRequest_v3(DataTable dtPassengers, DataSet dsCartDetails, bool IsPriceUpdate, int i, string HotelId, string UserID, string Password, string[] spitid, double Prices, int j, ref string PANN, string UniqueNo, bool IsPruvo);

        string Get_BookingDotCom_BookingCancelRQ_v3(string AgentVoucherNo, string CancelRes);

        Task<bool> Get_BookingDotCom_RecheckHit_v3(string UserName, string Password, DataTable dtCheckPriceReqStructure, string AgentUniqueNo, double FinalPrice, bool isForBooking, double CompanyBuyingPrice, string IsFor, string Provider, string RoomTypeId, string Currency, string hotelid, string Url, string UserId, string CheckIn, string CheckOut, int Rooms, HotelSearchParameter SearchPara, bool IsPriceChange, string searchKey, TokenData tokenData);
    }
}
