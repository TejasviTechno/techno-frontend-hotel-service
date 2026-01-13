using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XConnect.Models.Hotel;
using XConnect.Models;

namespace XConnect.Supplier.Travellanda.Interface
{
    public interface ITravellanda
    {
        void GetTravelandaRequestOnly_Async(DataRow[] commonCode, List<StarRating> listRating, HotelSearchParameter SearchPara, List<SupplierDetails> lstTravelLandaDetails, List<SupplierSearchConfig> lstTravelLandaSearchConfig, DataTable dtPreferredDataHotel, ref ConcurrentBag<HotelX> AllTask, int Hit, DataTable dtROE, TokenData tokenData, DataSet dtXMLCity);

        Task<List<Options>> Travellanda_HotelSearchWithRooms(HotelX Item, CommonSearch commonSearch, HotelSearchParameter SearchParam, string TLResponse, string XMLCITY, string XMLCOUNTRY, string Currency, double CurrencyFactor, Int32 processTimeout, string ResProccessTimerLogActive, string ResProccessTimeValueforSaveLog);

        #region Recheck- 29-05-25
        Task<CancellationDto> GetCancellationPollicyTravellanda(string Provider,int roomno, DataSet dsHotel, string ownsystemid, string roomtypeid, DataTable dtAgentDetail, DataTable dtSuplierMarkup, DataTable dtMarkupDetails, DataTable dtSearchConfig, HotelSearchParameter SearchPara, string OptionCode, int AgentId,string SearchKey, string GiataId = "0", int hotelRating = 0);
        #endregion

        //prebook - 25-06-25
        Task<PriceCheckCMDto> PriceCheckTravelLada(string OptionCode, DataTable dtCheckPriceReqStructure, DataSet dsCartDetails, DataRow[] TravelLandaDetails, string UniqueNo, string AgentUniqueNo, int i, string IsFor, double CompanyBuyPrice, bool IsError, double itemprice, string Searchkey = "", int AgentId = 0);

        //book 210825

        string GetTravelandaBookingquest(string AgentCode, DataTable dtPassengers, DataSet dsCartDetails, string UserName, string Password, string UniqueNo, bool IsPRiceUpdate, bool IsError, int i);

        string GetTravelLandaCancellationRequest(string Booking_Ref, string UserName, string Password);
    }
}
