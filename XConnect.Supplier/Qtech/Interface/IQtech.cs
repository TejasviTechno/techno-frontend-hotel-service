using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XConnect.Models.Hotel;
using XConnect.Models;

namespace XConnect.Supplier.Qtech.Interface
{
    public interface IQtech
    {
        void GetQTechRequestOnly_Async(CommonSearchList commonSearchList, List<StarRating> listRating, DataTable dtROE, bool IsHotelWiseSearch, bool HitFromAll, HotelSearchParameter SearchPara, List<SupplierDetails> QTechDetails, List<SupplierSearchConfig> QTechSearchConfig, DataTable dtPreferredDataHotel, ref ConcurrentBag<HotelX> AllTask, int Hit, TokenData tokenData, DataSet dtXMLCity);
        void GetQTechRequestOnly_Async(DataRow[] commonCode, List<StarRating> listRating, DataTable dtROE, bool IsHotelWiseSearch, bool HitFromAll, HotelSearchParameter SearchPara, List<SupplierDetails> QTechDetails, List<SupplierSearchConfig> QTechSearchConfig, DataTable dtPreferredDataHotel, ref ConcurrentBag<HotelX> AllTask, int Hit, TokenData tokenData, DataSet dtXMLCity);

        Task<List<Options>> QTech_HotelSearchWithRooms(HotelX Item, CommonSearch commonSearch, string ResponseData, HotelSearchParameter SearchParam, string XMLCITY, string XMLCountry, string Currency, double CurrencyFactor, string Provider, Int32 processTimeout, string ResProccessTimerLogActive, string ResProccessTimeValueforSaveLog, string XmlNationality = "", string SaveNoCountSupplog = "");

        //recheck - 300525
        Task<CancellationDto> GetQTechCancelationPolicy(DataSet dsHotel, string ownsystemid, string RoomTypeId_Own, DataTable dtAgentDetail, DataTable dtSuplierMarkup, DataTable dtMarkupDetails, DataTable dtSearchConfig, HotelSearchParameter objSearPera, string roomno, string HotelId, string Provider, int AgentId, string GiataId, int hotelRating, string RoomTypename, string mealId, string type,string Searchkey);

        Task<PriceCheckQTDto> QTechPriceCheck(DataSet dsCartDetails, DataRow[] MercanDetails, int j, double FinalPrice, double CurrencyFactorQTech, string UniqueNo, string AgentUniqueNo, double ResPrice, int AgentId, string Searchkey = "",string TechName="");

        string QTechBookingRequest(ref string Error, string agentcode, DataSet dsCartDetails, DataRow[] MercanDetails, int i, int j, double ResPrice, DataTable dtPassengers, ref string AGTRef,string SearchKey= "");

        Task<ResponseDto> GetQTechResponse(string Request, Int32 TimeOut = 120000, string Searchkey = "");

        Task<QtechBookRes> GetBookingDetail(string AGTRef, DataRow[] QTechDetails, string ReqBookingDetail);

        Task<QtechCancelCharge> QTechGet_cancellation_charges(string username, string password, string bookingid, string ChargesRequest, string url);

        string QTechCancellationRQ(string username, string password, string BookingRef, string url);
    }
}
