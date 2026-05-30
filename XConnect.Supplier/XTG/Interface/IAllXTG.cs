using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XConnect.Models.Hotel;
using XConnect.Models;
using static XConnect.Supplier.XTG.XTGRepository;


namespace XConnect.Supplier.XTG.Interface
{
    public interface IAllXTG //added trupti
    {
        void GetXTGRequestOnly_async(DataRow[] commonCode, List<StarRating> listRating, DataTable dtROE, bool HitFromAll, HotelSearchParameter SearchPara, List<SupplierDetails> XTGDetails, List<SupplierSearchConfig> XTGSearchConfig, DataTable dtPreferredDataHotel, ref ConcurrentBag<HotelX> AllTask, string Natinality, int Hit,TokenData tokenData);

        bool CheckRoomValidation(string Supplier, HotelSearchParameter SearchPara);
        Task<List<Options>> XMLXTG_RoomOnly(HotelX Item, CommonSearch commonSearch, string RQ, string currencyCode, DataTable dtAgentDetail, DataTable dtMarkupDetails, HotelSearchParameter SearchParam, string ResponseData, string Provider, string xmlcity, DataTable dtROE ,string ProccessCutOffTime, int processTimeout, string ResProccessTimerLogActive, string ResProccessTimeValueforSaveLog, double CurrencyFactor = 4);

        void GetMealRoomData_Xtg(string Type, string Code, ref string returnString, string SupplierName, string supplierCode, string AccessCode);

        #region  Recheck - 28-05-25
        string XTG_EssentialInfoOnly(DataRow[] XTGDetails, DataRow XTGChild, HotelSearchParameter SearchParam, string RoomTypeId, int SearchTimeout, string SearchKey);

        Task<CancellationDto> XMLXTG_Quote(TokenData tokenData,string SearchKey, int agentid, bool ForQuote, double CompanyBuy, string MealId, string RoomTypeId, string RoomTypeName, string language, string currencyCode, DataSet dsHotel, string ownsystemid, DataTable dtAgentDetail, DataTable dtSuplierMarkup, DataTable dtMarkupDetails, HotelSearchParameter SearchParam, DataRow[] XTGDetails, DataRow XTGChild, int SearchTimeout, string HotelIds, DataTable dtROE, string GiataId = "0", int hotelRating = 0);
        #endregion

        //prebook 25-06-25
        string GetQuoteRequest_XTG(string RoomtypeID, string client, int Timeout, string SupplierCode);

        Task<ResponseXTGDto> GetXTGResponseAsync(string url, string query, object variables, string Apikey, int TimeOut, HotelSearchParameter para = null, string Provider = "", bool DebugMode = false);

        //book 210825
        Task<string> GetXTGBookingRequest(DataTable dtPassengers, DataSet dsCartDetails, string internalRef, string client, int Timeout, string SupplierCode, string UniqueNo, string AgentUniqueNo, string XTGSiteID, string XTGWebUrl, string XTGApiKey, TokenData tokenData);
        Task<string> GetXTGBookingRequest_newversion(DataTable dtPassengers, BookRequest_newversion bookRequest, string internalRef, string client, int Timeout, string SupplierCode, string UniqueNo, string AgentUniqueNo, string XTGSiteID, string XTGWebUrl, string XTGApiKey, TokenData tokenData);

        GraphQLQuery_Response GetXTGResponse(ref string Error, string url, string query, object variables, string Apikey, int TimeOut, HotelSearchParameter para = null, string Provider = "", bool DebugMode = false);

        void GetXTGBookingByReference(ref string NewReference, ref int BookingStatus, string UniqNo, string SupplierName, string PnrNo, bool IsByOurReference, string Reference, string xtgclient, string XTGURL, string ApikEy, string context, string AccessCode, string hotelCode, string currency, int AgentId);
    }
}
