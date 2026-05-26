using System.Collections.Concurrent;
using System.Data;
using XConnect.Models;
using XConnect.Models.Hotel;
using static XConnect.Expedia.ExpediaRepository;



namespace XConnect.Supplier.Interface
{
    public interface IExpedia
    {
        string EAN_NationalityCode(string Nationality);

        void GetEANRequestOnly_async(bool SearchByOneID, DataRow[] commonCode, List<StarRating> listRating, List<SupplierSearchConfig> EAN_B2BSearchConfig, List<SupplierDetails> EAN_B2BDetails, DataTable dtPreferredDataHotel, string EchoToken, string Platform, string AgentCode, HotelSearchParameter SearchPara, bool IsB2B, bool IsHotelWiseSearch, ref ConcurrentBag<HotelX> AllTask, string XMLCOUNTRYCode, string XMLCITY, DataTable dtROE, TokenData tokenData, bool ISPKG = false, int EANProfit = 55);

        Task<List<Options>> ean_HotelSearchWithRooms(HotelX ItemEN, CommonSearch commonSearch, string xmlcity, string xmlcountry, HotelSearchParameter SearchParam, double ExpediaCurrencyFactor, DataTable dtROE, string Suppliername, string ResponseData, string CID, string Currency, DataRow[] ExpDetail, string xmlhotelcode, string platform, string echotoken, string Sign, string ApiKey, string Version, string URL, string Provider, string ProccessCutOffTime, Int32 processTimeout, string ResProccessTimerLogActive, string ResProccessTimeValueforSaveLog, string StoreSupplierResErrorLog);

        //130325
        void GetEANRequestOnly_async_new(bool SearchByOneID, DataRow[] commonCode, List<StarRating> listRating, List<SupplierSearchConfig> EAN_SearchConfig, List<SupplierDetails> EAN_Details, DataTable dtPreferredDataHotel, string EchoToken, string Platform, string AgentCode, HotelSearchParameter SearchPara, bool IsB2B, bool IsHotelWiseSearch, ref ConcurrentBag<HotelX> AllTask, string XMLCOUNTRYCode, string XMLCITY, DataTable dtROE, TokenData tokenData, bool ISPKG = false, int EANProfit = 55);


        #region Recheck - 270525
        Task<ResponseRecheck> Get_EAN_PropertyContent_WithPreBook_Async(bool HitPrebook,string HotelId, string platform, string echotoken, string Sign, string ApiKey, string Version, string URL, string SupplierName, DataTable dtROE, HotelSearchParameter SearchPara, double CompanyBuyingPrices_EANB2B, int AgentId, string RoomTypeID, string IP,TokenData tokenData,double Tolerance);

        #endregion

        //prebook - 070125
        string getPriceRequestEAN(string RoomTypeId, string ItemCode);

        Task<ResponseDto> GetEANResponseEANAsync(string strHost, int timeout, string provider = "", string userName = "", string password = "", string echoToken = "", string platform = "", string isBooking = "GET", string requestString = "", string searchKey = "", string ip = "");
        Task<ResponseDto> GetEANResponse(string strHost, Int32 TimeOut, string Provider = "", string UserName = "", string Password = "", string EchoToken = "", string PlatForm = "", string IsBooking = "GET", string RequestString = "", string Searchkey = "", string ip = "");
        Task<DataSet> ReturnEANDatatable(DataSet ds, string XMLHotelCode, string XMLCityCode, string UrlName, string CompanyID, string UserName, string Password, string platform, string provider = "", string IP = "");

        string EANStringForBookV3(DataSet dsCartDetails, DataTable dtPassengers, int i, int jj, string Ex_Guid, string SupplierName = "");

        Task<ResponseDto> GetEANResponseBooking(string strHost, Int32 TimeOut, string Provider = "", string UserName = "", string Password = "", string EchoToken = "", string PlatForm = "", string IsBooking = "GET", string RequestString = "", string SearchKey = "", string IP = "");

        //new version
        string EANStringForBookV3_newversion(BookRequest_newversion bookRequest, DataTable dtPassengers, int i, string Ex_Guid, string SupplierName = "");
        string EANStringForBookV3_newversion_gujju(BookRequest_newversion bookRequest, DataTable dtPassengers, int i, string Ex_Guid, string SupplierName = "");
    }
}
