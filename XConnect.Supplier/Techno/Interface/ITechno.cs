using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XConnect.Models.Hotel;
using XConnect.Models;

namespace XConnect.Supplier.Techno.Interface
{
    public interface ITechno
    {
        void GetWE_DMCRequestOnly_Async(bool isHotelId, AdvancedOptions advance, DataRow[] commonCode, DataTable dtPreferredDataHotel, HotelSearchParameter SearchPara, List<SupplierSearchConfig> lstWE_DMCSearchConfig, List<SupplierDetails> lstWE_DMCDetails, ref ConcurrentBag<HotelX> AllTask, string AgentCode, int Hit, List<StarRating> listRating, DataTable dtROE,TokenData tokenData, DataSet dtXMLCity,string Platform,bool SearchByOneID);

        Task<List<Options>> WE_techno_HotelSearchWithRooms(HotelX Item, CommonSearch commonSearch, string ResponseData, string CityCode, string XmlCountry, HotelSearchParameter SearchParam, string currency, double CurrencyFactorWE_DMC, string Provider, Int32 processTimeout, string ResProccessTimerLogActive, string ResProccessTimeValueforSaveLog);

        Task<CancellationDto> Get_Withinearth_DMC_CancelationPolicy(DataSet dsHotel, string ownsystemid, string RoomTypeId_Own, DataTable dtAgentDetail, DataTable dtSuplierMarkup, DataTable dtMarkupDetails, DataTable dtSearchConfig, HotelSearchParameter objSearPera, string roomno, string HotelId, string Provider, int AgentId, string GiataId, int hotelRating, string roomtypename, string Mealid, DataTable dtROE, string Search_Key,string IP, TokenData tokenData,string TechName="");
        Task<CancellationDto> Get_Withinearth_DMC_CancelationPolicy_New(DataSet dsHotel, string ownsystemid, string RoomTypeId_Own, DataTable dtAgentDetail, DataTable dtSuplierMarkup, DataTable dtMarkupDetails, DataTable dtSearchConfig, HotelSearchParameter objSearPera, string roomno, string HotelId, string Provider, int AgentId, string GiataId, int hotelRating, string roomtypename, string Mealid, DataTable dtROE, string Search_Key, string IP, TokenData tokenData, string TechName = "");
        Task<ResponseDto> GetSupplierResponseWE_DMC(string strxmlRQRS, string strHost, int TimeOut, string Provider = "", HotelSearchParameter para = null, bool DebugMode = false, string UniqueNo = "", string Search_key = "", string password = "", string Techname = "");

        Task<PriceCheckDidaDto> Get_Withinearth_DMC_PreBookingReqRes(string Provider, bool IsCombine, DataSet dsCartDetails, string ReferenceNo, string ID, string Password, int HotelCode, string RoomToken, string CheckInDate, string CheckOutDate, string URL, double Rate, double FinalPrice, int UserCartId, int UserCartDetailId, DataTable dtCheckPriceReqStructure, int j, string UniqueNo, string AgentUniqueNo, bool isForBooking, int RoomNo, string SearchKey, string HotelOptionId, string Currency, double CurrencyFactorWithinearth_DMC, string Searchkey, int AgentId = 0,string IP="",string TechName="");

        string Withinearth_DMCBookingRequest(DataRow[] Withinearth_DMCDetails, string SearchKey, string BToken, double TotalPrice, string InternalReference, string RoomNo, DataSet dsCartDetails, DataTable dtPassengers, string UniqueNo, string AgentUniqueNo, string WE_DMCCurrency,string IP);
        string Withinearth_DMCBookingRequest_newversion(DataRow[] Withinearth_DMCDetails, string SearchKey, string BToken, double TotalPrice, string InternalReference, string RoomNo, BookRequest_newversion bookRequest, DataTable dtPassengers, string UniqueNo, string AgentUniqueNo, string WE_DMCCurrency,string IP);

        Task<WE_BookingDetail> GetBookingDetailWE(string AGTRef, string UserID, string BookingId, string URL, string UniqueNo, string AgentUniqueNo, string Currency, string Request, string Search_Key, string IP, string Supplier);

        string CheckHotelCancellationChargesRequest(DataRow[] Withinearth_DMCDetails, string BookingRef, string Currency, string IP);
        string Withinearth_DMCBookingDetailRequest(DataRow[] Withinearth_DMCDetails, string BookingId, string InternalReference, string Currency = "USD", string IP = "");
        string Withinearth_DMCCancelBookingRequest(DataRow[] Withinearth_DMCDetails, string BookingRef, string Reason, Int64 BookingId, string BookingDetailId, string CancelCode, string Currency, string IP);
    }
}
