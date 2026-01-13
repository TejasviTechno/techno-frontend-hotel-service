using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XConnect.Models.Hotel;
using XConnect.Models;

namespace XConnect.Supplier.Traveloka.Interface
{
    public interface ITraveloka
    {
        void TravelokaRequestOnly_async(DataRow[] commonCode, List<StarRating> listRating, HotelSearchParameter SearchPara, List<SupplierDetails> TravelokaDetails, List<SupplierSearchConfig> TravelokaSearchConfig, DataTable dtPreferredDataHotel, ref ConcurrentBag<HotelX> AllTask, string XMLCity, string XMLCountry, string XMLNationality, int Hit, string custIP, DataTable dtROE, TokenData tokenData);

        void TravelokaRequestOnly_async_new(DataRow[] commonCode, List<StarRating> listRating, HotelSearchParameter SearchPara, List<SupplierDetails> lstTravelokaDetails, List<SupplierSearchConfig> lstTravelokaSearchConfig, DataTable dtPreferredDataHotel, ref ConcurrentBag<HotelX> AllTask, string custIP, DataTable dtROE, TokenData tokenData, DataSet dtXMLCity);
        void Get_Traveloka_Token(ref string TravelokaToken, string Url);
        Task<List<Options>> Traveloka_HotelSearchWithRooms(HotelX Item, CommonSearch commonSearch, string Currency, string ResponseData, string CityCode, HotelSearchParameter SearchParam, double CurrencyFactorTBO, string XMLnationality, string URLandRequest, string UserName, string PassWord, string URL, Int32 SearchTimeOut, string custIP, Int32 processTimeout, string ResProccessTimerLogActive, string ResProccessTimeValueforSaveLog);

        Task<PriceCheckTravelokaDto> Get_Traveloka_PriceCheckRQRS(DataSet dsCartDetails, DataRow[] TravelokaDetails, int i, int j, DataTable dtCheckPriceReqStructure, double FinalPrice, bool isFromBooking, string time, string responsetype, double CompanyBuyingPrice, TokenData tokenData,string Supplier, int AgentId = 0,string platform ="");

        string Get_Traveloka_BookingRequest(DataSet dsCartDetails, DataTable dtPassengers, string RoomTypeId, string ReferenceNo, int i, int UserCartDetailId, string TravelokaCurrency, string Platform);

        string Get_Traveloka_BookingCancelRQ(string Ref, string Reason);

        Task<ResponseDto> GetSupplierResponseTraveloka(string strHost, string strxmlRQRS, string Type);

        string Get_Traveloka_ViewItinerary(string ReferenceNo, string TimeStamp, string Platform, string TravelokaCurrency);
    }
}
