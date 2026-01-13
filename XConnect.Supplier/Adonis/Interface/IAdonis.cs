using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XConnect.Models.Hotel;
using XConnect.Models;

namespace XConnect.Supplier.Adonis.Interface
{
    public interface IAdonis
    {
        void GetAdonisRequestOnly_async(DataRow[] commonCode, List<StarRating> listRating, HotelSearchParameter SearchPara, List<SupplierDetails> lstAdonisDetails, List<SupplierSearchConfig> lstAdonisSearchConfig, DataTable dtPreferredDataHotel, ref ConcurrentBag<HotelX> AllTask, int Hit, DataSet dtXMLCity, DataTable dtROE, TokenData tokenData);
        Task<List<Options>> Adonis_HotelSearchWithRooms(HotelX Item, CommonSearch commonSearch, string currency, string ResponseData, string CityCode, HotelSearchParameter SearchParam, double CurrencyFactorJacTravel, string Supplier, DataSet dsAgentDetails, Int32 processTimeout, string ResProccessTimerLogActive, string ResProccessTimeValueforSaveLog);

        //recheck 30-05-25

        Task<CancellationDto> Get_Adonis_CancellationPolicy(double CompanyBuy, string Type, DataSet dsHotel, string hotelcode, string ownsystemid, string RoomTypeId_Own, string MealID, string Board_Text, DataTable dtAgentDetail, DataTable dtSuplierMarkup, DataTable dtSearchConfig, HotelSearchParameter objSearPera, string roomno, string provider, int AgentId, DataTable dtMarkupDetails, string SearchKey,TokenData tokenData, string GiataId = "0", int hotelRating = 0, DataTable dtXmlNationality = null);

        Task<PriceCheckAdonisDto> Get_Adonis_PriceCheckRQRS(DataSet dsCartDetails, DataRow[] AdonisDetails, int j, DataTable dtCheckPriceReqStructure, string UniqueNo, string AgentUniqueNo, double FinalPrice, bool isForBooking, double CompanyBuyingPrice, string PreBookingToken, string Searchkey = "", int AgentId = 0,TokenData tokenData = null);

        string Get_Adonis_BookingRequest(DataSet dsCartDetails, DataTable dtPassengers, DataRow[] JacTravelDetails, string UniqueNo, bool IsPriceUpdate, int i, int j, string PreBookingToken, string AgentCode);

        Task<ResponseDto> Get_Adonis_Response(string request, string TravcoURL, Int32 TimeOut, string Provider = "");

        string Get_Adonis_BookingDetailRQ(string AgentVoucherNo, DataRow[] SupplierDetails);

        string Get_Adonis_BookingCancelRQ(string AgentVoucherNo, DataRow[] SupplierDetails);    
    }
}
