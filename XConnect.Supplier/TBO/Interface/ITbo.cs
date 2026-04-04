using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XConnect.Models.Hotel;
using XConnect.Models;

namespace XConnect.Supplier.TBO.Interface
{
    public interface ITbo
    {
        void GetTBO_V1HolidayRequestOnly_async(DataRow[] commonCode, List<StarRating> listRating, DataTable dtROE, bool IsHotelWiseSearch, bool HitFromAll, HotelSearchParameter SearchPara, List<SupplierDetails> lsttboDetails, List<SupplierSearchConfig> lsttboSearchConfig, DataTable dtPreferredDataHotel, ref ConcurrentBag<HotelX> AllTask, int Hit, TokenData tokenData, DataSet dtXMLCity);

        Task<List<Options>> TBO_V1HotelSearchWithRooms(HotelX Item, CommonSearch commonSearch, string ResponseData, HotelSearchParameter SearchParam, string XMLCITY, string XMLCOUNTRY, string Currency, double CurrencyFactor, Int32 processTimeout, string ResProccessTimerLogActive, string ResProccessTimeValueforSaveLog, string StoreSupplierResErrorLog);

        #region Recheck 29-05-25
        Task<ResponseRecheckTbo> TboApi_cancellation_For_Recheck_OnlyRead_EssInfo(string Type, string ownsystemid, string roomtypeid, DataTable dtSuplierMarkup, string provider, string CompanyBuyPrice, int AgentId
        , string TBOUrl, string Username, string password, string hotelid, string CheckIn, HotelSearchParameter objSearPera, string SearchKey);

        Task<CancellationDto> Get_TBO_CancellationPolicy(string Type, DataSet dsHotel, string hotelcode, string ownsystemid, string RoomTypeId_Own, string MealID, string Board_Text, DataTable dtAgentDetail, DataTable dtSuplierMarkup, DataTable dtSearchConfig, HotelSearchParameter objSearPera, string roomno, string provider, int AgentId, DataTable dtMarkupDetails, string SearchKey, string GiataId = "0", int hotelRating = 0, double roomprice = 0, string Provider = "tbo_api", string SiteCurrency = "USD");

        Task<CancellationDto> Get_TBO_Json_CancellationPolicy(string Type, DataSet dsHotel, string hotelcode, string ownsystemid, string RoomTypeId_Own, string MealID, string Board_Text, DataTable dtAgentDetail, DataTable dtSuplierMarkup, DataTable dtSearchConfig, HotelSearchParameter objSearPera, string roomno, string provider, int AgentId, DataTable dtMarkupDetails, string SearchKey, TokenData TokenData, string GiataId = "0", int hotelRating = 0, double roomprice = 0, string Provider = "tbo_v1", string SiteCurrency = "USD");
        #endregion

        //prebook - 27-06-25

        Task<PriceCheckTboDto> TBO_PriceCheckRQRSJson(DataSet dsAgentDetails, DataSet dsCartDetails, DataRow[] TBODetails, string UserId, string Password, int i, int j, DataTable dtCheckPriceReqStructure, string UniqueNo, string AgentUniqueNo, double FinalPrice, bool isForBooking, double CompanyBuyingPrice, string PreBookingToken, string xmlhotelid, bool IsPolicyChangeTBO, TokenData tokenData, int AgentId);

        Task<TBO_BookingProcess> Get_TBO_BookingProcess_json(DataTable dtAgentVoucher, DataSet dsCartDetails, DataTable dtCheckPriceReqStructure, DataTable dtPassengers, DataTable dtCAncellationPolicy, DataRow[] TBODetails, string AgentVoucherNo, int bookingStatus, string UniqueNo, string AgentUniqueNo, bool isNonRefundable, int i, int j, string referenceNumber, TokenData tokenData, int AgentId = 0,string SearchKey="");
        //Task<TBO_BookingProcess> Get_TBO_BookingProcess_json_newversion(DataTable dtAgentVoucher, BookRequest_newversion bookRequest, DataTable dtCheckPriceReqStructure, DataTable dtPassengers, DataTable dtCAncellationPolicy, DataRow[] TBODetails, string AgentVoucherNo, int bookingStatus, string UniqueNo, string AgentUniqueNo, bool isNonRefundable, int i, int j, string referenceNumber, TokenData tokenData, int AgentId = 0,string SearchKey="");

        string Get_TBO_BookingCancelRQ_json(string AgentVoucherNo, DataRow[] TBODetails, string cancelreason);

        string Get_TBO_BookingDeatilsjson(DataRow[] TBODetails, string ClientReferancreNo);

        Task<ResponseDto> GetTrvcoResponseTBOJson(string UsernameTbo, string PasswordTbo, string Request, string TravcoURL, Int32 TimeOut, string Provider = "", HotelSearchParameter para = null, bool DebugMode = false);
         string Get_TBO_BookingRequest_json_newversion(string Url, BookRequest_newversion bookRequest, DataTable dtPassengers, DataRow[] TBODetails, string UniqueNo, bool IsPriceUpdate, int i, int j, string PreBookingToken, string UserID, string Password, string ClientReferancreNo, TokenData tokenData);
    }
}
