using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XConnect.Models;
using XConnect.Models.Hotel;


namespace XConnect.Supplier.Agoda.Interface
{
    public interface IAgoda
    {
        void GetAgoda_NewRequestOnly_async_New(bool SearchByOneID, CommonSearchList commonSearchList, List<StarRating> listRating, bool IsHotelWiseSearch, bool HitFromAll, HotelSearchParameter SearchPara, List<SupplierDetails> lstAgodaDetails, List<SupplierSearchConfig> lstAgodaSearchConfig, DataTable dtPreferredDataHotel, ref ConcurrentBag<HotelX> AllTask, int Hit, DataTable dtROE, TokenData tokenData, DataSet dtXMLCity);
        void GetAgoda_NewRequestOnly_async(bool SearchByOneID, DataRow[] commonCode, List<StarRating> listRating, string Supplier, bool IsHotelWiseSearch, bool HitFromAll, HotelSearchParameter SearchPara, List<SupplierDetails> AgodaV1Details, List<SupplierSearchConfig> AgodaV1SearchConfig, DataTable dtPreferredDataHotel, ref ConcurrentBag<HotelX> AllTask, string Xmlcity, string XmlNationality, int Hit, DataTable dtROE,TokenData token);
        Task<List<Options>> XMLAgoda_V1_HotelSearchWithRooms(HotelX Item, CommonSearch commonSearch, string Response, string CityCode, HotelSearchParameter SearchParam, double CurrencyFactorAgoda, string Agoda_NationalityCode, string ProccessCutOffTime, Int32 processTimeout, string ResProccessTimerLogActive, string ResProccessTimeValueforSaveLog, string StoreSupplierResErrorLog, string Provider);


        //130325
        void GetAgoda_NewRequestOnly_async_New_v1(bool SearchByOneID, DataRow[] commonCode, List<StarRating> listRating, bool IsHotelWiseSearch, bool HitFromAll, HotelSearchParameter SearchPara, List<SupplierDetails> lstAgodaDetails, List<SupplierSearchConfig> lstAgodaSearchConfig, DataTable dtPreferredDataHotel, ref ConcurrentBag<HotelX> AllTask, int Hit, DataTable dtROE, TokenData tokenData, DataSet dtXMLCity);

        //Task<List<Options>> XMLAgoda_Chain_HotelSearchWithRooms(HotelX Item, CommonSearch commonSearch, string Response, string CityCode, HotelSearchParameter SearchParam, double CurrencyFactorAgoda, string Agoda_NationalityCode, string ProccessCutOffTime, Int32 processTimeout, string ResProccessTimerLogActive, string ResProccessTimeValueforSaveLog, string StoreSupplierResErrorLog, string Provider);

        #region Recheck 28-05-25

        Task<ResponseRecheck> CheckAgoda_NewBeforeBooking_Recheck(DataRow[] drSuplierMarkup,HotelSearchParameter SearchParam, string RoomTypeId, string agodapassword,string agodaURL,int AgentId);

        string Get_Agoda_EInfo(string HotelId, string Username, string agodapassword, string platform, string URL, string SupplierName,string SearchKey);
        Task<string> Get_Agoda_v1_EInfo(string HotelId, string Username, string agodapassword, string platform, string URL, string SupplierName, string RoomTypeId, string SearchKey);

        void PrepareAgoda_NewBeforeBooking_Recheck(DataRow[] AgodaV1Details, ref string checkindate, ref string checkoutdate, ref int Adults,
    ref int Childs, ref int Rooms, ref string AgodaSearchUrl, ref string AgodaSiteID, ref string AgodaApiKey, ref string ChildAge, HotelSearchParameter SearchPara);

        string Agoda_NewPriceCheckRQ(string checkin, string checkout, int rooms, int adult, int child, string siteid, string apiKey, string roomtypeID, ref double FinalPrice, int i, string childrenAges);

        Task<ResponseDto> GetSupplierResponseAgoda_new(string strUniqueId, string strxmlRQRS, string strHost, Int32 TimeOut, bool IsSecure, string Provider = "", HotelSearchParameter para = null, bool DebugMode = false);
        #endregion
        //prebook 30-06-25
         Task<PriceCheckQTDto> DoAgodaPriceCheck(string supplier, bool IsFromBook, DataTable dtCheckPriceReqStructure, DataRow[] AgodaDetails, DataSet dsCartDetails, DataSet dsAgentDetails, int i, string UniqueNo, string AgentUniqueNo, DataRow[] AgodaSearchConfig, DataTable dtCountryCityMarkup, bool isPerce, double AgentMarkupAmount, bool IsB2C, double B2CMarkup, double SubAgentMarkup, int IsParentId, double FinalPrice, string Searchkey = "", int AgentId = 0);

        Task<PriceCheckDto> DoAgoda_NewPriceCheck(string supplier,bool IsFromBook, DataTable dtCheckPriceReqStructure, DataTable dtPriceUpdateTable, DataTable dtPriceDetail, double serviceTot, DataRow[] AgodaV1Details, DataSet dsCartDetails, DataSet dsAgentDetails, int i, string UniqueNo, string AgentUniqueNo, DataRow[] AgodaSearchConfig, DataTable dtCountryCityMarkup, bool isPerce, double AgentMarkupAmount, bool IsB2C, double B2CMarkup, double SubAgentMarkup, int IsParentId, double FinalPrice, int AgentId);

        void PrepareAgoda_NewBeforeBooking(DataRow[] AgodaV1Details, DataSet dsCartDetails, int i, ref string checkindate, ref string checkoutdate, ref int Adults,
      ref int Childs, ref int Rooms, ref string XmlHotelId, ref string AgodaSearchUrl, ref string AgodaSiteID, ref string AgodaApiKey, ref string ChildAge);

        string GetAgoda_NewSearchRequest_BeforeBooking(string CheckInDate, string CheckInOut, int Rooms, int Adult, int child, string AgodaSiteID, string AgodaApiKey, string AgodaLaguage, string AgodaCurrency, int SearchTimeout, string HotelIds, string Nationality, string ChildAge);

        string GetAgoda_NewBookingRequest(ref bool isTrue, string GetRoomResponse, DataSet dsCartDetails, DataTable dtPassengers, string AgodaSiteID, string AgodaApiKey, string RoomTypeId, string XmlHotelId, string checkindate, string checkoutdate, string AgodaCurrency, int Adults, int Childs, int Rooms, string Nationality, int i, int j, string RefNo, ref string ChildAge);

        string GetAgoda_newBookingCancelRQ(string AgentVoucherNo);
        string ConfirmCancellation(string AgentVoucherNo, int Reference, string Currency, double inclusive);
    }
}
