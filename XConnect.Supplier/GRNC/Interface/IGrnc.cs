using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XConnect.Models.Hotel;
using XConnect.Models;

namespace XConnect.Supplier.GRNC.Interface
{
    public interface IGrnc
    {
        void GRNCRequestOnly_async(DataRow[] commonCode, List<StarRating> listRating, HotelSearchParameter SearchPara, List<SupplierDetails> lstGRNCDetails, List<SupplierSearchConfig> lstGRNCSearchConfig, DataTable dtPreferredDataHotel, ref ConcurrentBag<HotelX> AllTask, int Hit, DataTable dtROE, TokenData tokenData, DataSet dtXMLCity);

        Task<List<Options>> GRNC_HotelSearchWithRooms(HotelX Item, CommonSearch commonSearch, string Currency, string ResponseData, string CityCode, HotelSearchParameter SearchParam, double CurrencyFactorTBO, string XMLnationality, string URLandRequest, string UserName, string PassWord, string URL, Int32 SearchTimeOut, Int32 processTimeout, string ResProccessTimerLogActive, string ResProccessTimeValueforSaveLog);

        //recheck 30-05-25
        Task<CancellationDto> GRNCCancelltionPolicy(DataSet dsHotel, string ownsystemid, string RoomTypeId_Own, DataTable dtAgentDetail, DataTable dtSuplierMarkup, DataTable dtMarkupDetails, DataTable dtSearchConfig, HotelSearchParameter objSearPera, string roomno, string HotelId, string Provider, string roomtypecode, string type, string GiataId, int AgentId, int hotelRating/*, string CurrencyCode, string amount_type*/, string checkin, string checkout, double roomprice, string MealId,string SearchKey, string CompanyBuying = "");

        string GRNCBookingRequest1(DataSet dsCartDetails, DataTable dtPassengers, int i, int j, int UniqueNo, string PreBookRef, int NoOfRoom, string HotelId, string TID, string ContryId, string CITY_CODE, string AgentCode);

        string GRNCBookingResponse(ref string Error, string sid, string Xmlrequest, string Url, string username, string password, int Timeout, string UserId);

        string GRNCBookingfetch(DataSet dsCartDetails, DataTable dtPassengers, int i, int j, int UniqueNo, string PreBookRef, int NoOfRoom, string HotelId, string TID, string ContryId, string CITY_CODE);

        string GRNCBookingrefetchResponse(string sid, string Xmlrequest, string Url, string username, string password, int Timeout, string UserId);

        string GRNCCancellationResponse(string sid, string strxmlRQRS, string url, string Key);
    }
}
