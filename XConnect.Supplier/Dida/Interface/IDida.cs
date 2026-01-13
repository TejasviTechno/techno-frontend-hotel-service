using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XConnect.Models;
using XConnect.Models.Hotel;

namespace XConnect.Supplier.Dida.Interface
{
    public interface IDida
    {
        void GetDidaRequestOnly_Async(DataRow[] commonCode, List<StarRating> listRating, string SupplierName, HotelSearchParameter SearchPara, List<SupplierDetails> DidaDetails, List<SupplierSearchConfig> DidaSearchConfig, DataTable dtPreferredDataHotel, ref ConcurrentBag<HotelX> AllTask, string XMLCity, string XMLCountry, string Xmlnationality, int Hit, string AgentCode, DataTable dtROE, TokenData tokenData);

        void GetDidaRequestOnly_Async_new(DataRow[] commonCode, List<StarRating> listRating, HotelSearchParameter SearchPara, List<SupplierDetails> lstDidaDetails, List<SupplierSearchConfig> lstDidaSearchConfig, DataTable dtPreferredDataHotel, ref ConcurrentBag<HotelX> AllTask, int Hit, string AgentCode, DataTable dtROE, TokenData tokenData, DataSet dtXMLCity);
        bool DidaCheckRoomPax(HotelSearchParameter objSearPera);
        Task<List<Options>> XMLDidaPriceOnly_Async(HotelX Item, CommonSearch commonSearch, string ResponseData, HotelSearchParameter SearchParam, string SupplierName, string XMLCITY, string XMLCountry, string Currency, double CurrencyFactor, string XmlNationality, Int32 processTimeout, string ResProccessTimerLogActive, string ResProccessTimeValueforSaveLog, DataTable dtROE);

        #region Recheck - 270525
        Task<int> CheckDidaPrebookOnRecheck(string hotelcode, string ownsystemid, DataTable dtCheckPriceReqStructure, string roomtypeid, HotelSearchParameter SearchPara, string DidaPassword, string DidaURL, string Username, string SearchKey, string Provider, string checkindate, string checkoutdate, int Adults, int Childs,  int Rooms,  string AgodaSearchUrl,  string AgodaApiKey,  string ChildAge, bool isForBooking, double Rate, string AgentUniqueNo, string ISBooking, DataTable dtROE, bool IsPriceChange, double FinalPrice = 0,string SupplierCurrency = "USD");////new added on 03-May-2025
        #endregion

        //prebook - 30/06/25
        Task<PriceCheckDidaDto> DidaPreBookingReq(string ReferenceNo, string ID, string Password, string HotelCode, string RCode, int NoOfAdults, int NoOfChild, string CheckInDate, string CheckOutDate, string URL, double Rate, double FinalPrice, int UserCartId, int UserCartDetailId, DataTable dtCheckPriceReqStructure, int j, string UniqueNo, string AgentUniqueNo, bool isForBooking, int ChildAge1, int ChildAge2, int NoOfRoom, string Nationality, string CDate, string Searchkey = "", int AgentId = 0, string DidaCurrency="USD");

        string DidaBookingRequest(DataSet dsCartDetails, DataTable dtPassengers, int i, int j, string ClientID, string UserName, string Password, int UniqueNo, string PreBookRef, int NoOfRoom, string AgentCode);

        Task<ResponseDto> GetDidaResponse(string strxmlRQRS, string strHost, string UniqueNo = "", string Searchkey = "", int timeout = 200000);

        string DidaPreCancelltionReq(string BookingId, string Id, string password, string url);

        string DidaCancelConfirmReq(string BookingId, string ConfirmId, string Id, string password);
    }
}
