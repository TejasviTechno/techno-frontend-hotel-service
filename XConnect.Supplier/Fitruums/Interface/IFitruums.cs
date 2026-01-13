using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XConnect.Models;
using XConnect.Models.Hotel;


namespace XConnect.Supplier.Fitruums.Interface
{
    public interface IFitruums
    {
        void GetfitruumsRequestOnly_Async_new(DataRow[] commonCode, List<StarRating> listRating, HotelSearchParameter SearchPara, List<SupplierDetails> fitruumsDetails, List<SupplierSearchConfig> fitruumsSearchConfig, DataTable dtPreferredDataHotel, ref ConcurrentBag<HotelX> AllTask, string citycode, string Nationality, int Hit, string supplierName, TokenData tokenData);
        Task<List<Options>> Fitruums_HotelSearchWithRooms(HotelX Item, CommonSearch commonSearch, string NationalityCode, HotelSearchParameter SearchParam, string TLResponse, string XMLCITY, string XMLCOUNTRY, string Currency, double CurrencyFactor, DataTable dtroe,string ProccessCutOffTime, Int32 processTimeout, string ResProccessTimerLogActive, string ResProccessTimeValueforSaveLog, string StoreSupplierResErrorLog);

        //130325
        void GetfitruumsRequestOnly_Async_new_v1(DataRow[] commonCode, List<StarRating> listRating, HotelSearchParameter SearchPara, List<SupplierDetails> fitruumsDetails, List<SupplierSearchConfig> fitruumsSearchConfig, DataTable dtPreferredDataHotel, ref ConcurrentBag<HotelX> AllTask, int Hit, TokenData tokenData, DataSet dtXMLCity, bool HitFromAll = true);

        //Recheck - 30-05-25
        Task<CancellationDto> GetFitruumsCancelationpolicy(int AgentId, DataSet dsHotel, string ownsystemid, string RoomTypeId_Own, DataTable dtAgentDetail, DataTable dtSuplierMarkup, DataTable dtMarkupDetails, DataTable dtSearchConfig, HotelSearchParameter objSearPera, string roomno, string provider,string SearchKey, int hotelRating = 0, string XmlNationality = "", string RoomTypeName = "", string MealId = "", string Companybuy = "");
        Task<string> GetFitruums_EInfo(string RoomTypeId_Own, DataTable dtSuplierMarkup, DataTable dtSearchConfig, HotelSearchParameter objSearPera, string provider, string SearchKey, string Currency, string XmlNationality = "", string MealId = "", string Companybuy = "");

        //prebook
        string GetfitruumsPreBookingRequest(DataTable UsercartData, string Url, string UserName, string Password,string FitruumsCurrency);
        Task<ResponseDto> GetSupplierResponseFitruums(string strUniqueId, string strxmlRQRS, string strHost, Int32 TimeOut, string Provider = "", HotelSearchParameter para = null, bool DebugMode = false, string SearchKey = "");

        string Get_fitruums_BookingRequest(DataSet dsCartDetails, DataTable dtPassengers, DataRow[] JacTravelDetails, string UniqueNo, bool IsPriceUpdate, int i, int j, string PreBookingToken, string UserName, string Password, string AgentCode);

        Task<FitruumsBookRes> Get_fitruums_BookingDetailsProcess(int bookingStatus, string AgentVoucherNo, string UserName, string Password, string UniqueNo, string AgentUniqueNo, string Url, DataSet dsCartDetails, int j, string AgentCode);

        string Get_fitruums_BookingCancelRQ(string AgentVoucherNo, DataRow[] SupplierDetails);
    }
}
