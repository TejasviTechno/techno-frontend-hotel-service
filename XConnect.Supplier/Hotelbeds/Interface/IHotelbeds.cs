using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XConnect.Models.Hotel;
using XConnect.Models;

namespace Xconnect.Hotelbeds.Interface
{
    public interface IHotelbeds
    {
        
        void GetHotelbedsRequestOnly_async(DataRow[] commonCode, List<StarRating> listRating, DataTable dtPreferredDataHotel, HotelSearchParameter SearchPara, List<SupplierSearchConfig> HBV1SearchConfig, List<SupplierDetails> HBV1Details, bool IsHotelWiseSearch, ref ConcurrentBag<HotelX> AllTask, string xmlNationality, int Hit, string supplierName, DataTable dtROE,TokenData tokenData);
        void GetHotelbedsRequestOnly_async_new(DataRow[] commonCode, List<StarRating> listRating, DataTable dtPreferredDataHotel, HotelSearchParameter SearchPara, List<SupplierSearchConfig> lstHotelbedsSearchConfig, List<SupplierDetails> lstHotelbedsDetails, bool IsHotelWiseSearch, ref ConcurrentBag<HotelX> AllTask, int Hit, DataTable dtROE, TokenData tokenData, DataSet dtXMLCity);


        Task<List<Options>> HotelbedsV1_HotelSearchWithRooms(bool SearchByOneID, HotelX Item, CommonSearch commonSearch, List<RQRS> rQRS, string WSurl, string Username, string Password, Int32 SearchTimeOut, HotelSearchParameter SearchParam, bool DebugMode, string Currency, double CurrencyFactor, string ProccessCutOffTime, Int32 processTimeout, string ResProccessTimerLogActive, string ResProccessTimeValueforSaveLog, bool withcancellation, string StoreSupplierResErrorLog);

        Task<List<Options>> hotelbedsv1_new_HotelSearchWithRooms(bool SearchByOneID, HotelX Item, CommonSearch commonSearch, List<RQRS> rQRS, string WSurl, string Username, string Password, Int32 SearchTimeOut, HotelSearchParameter SearchParam, bool DebugMode, string Currency, double CurrencyFactor, string ProccessCutOffTime, Int32 processTimeout, string ResProccessTimerLogActive, string ResProccessTimeValueforSaveLog, bool withcancellation, string StoreSupplierResErrorLog);

        #region Recheck -28-05-25
        Task<ResponseRecheck> HotelBedsV1_cancellation_For_Recheck_OnlyRead_EssInfo(string roomtypeid, DataTable dtSuplierMarkup, string provider, string CompanyBuyPrice, int AgentId, string SearchKey);

        Task<CancellationDto> HotelBedsV1_cancellation(string roomtypecode, DataSet dsHotel, string hotelcode, string ownsystemid, string roomtypeid, DataTable dtAgentDetail, DataTable dtSuplierMarkup, DataTable dtSearchConfig, HotelSearchParameter SearchPara, string roomno, string provider, int AgentId, DataTable dtMarkupDetails, string GiataId = "0", int hotelRating = 0, string SearchKey = "");

        Task<CancellationDto> HotelBedsV1_cancellation_new(TokenData tokenData, string roomtypecode, DataSet dsHotel, string hotelcode, string ownsystemid, string roomtypeid, DataTable dtAgentDetail, DataTable dtSuplierMarkup, DataTable dtSearchConfig, HotelSearchParameter SearchPara, string roomno, string provider, int AgentId, DataTable dtMarkupDetails, string GiataId = "0", int hotelRating = 0, string SearchKey = "", string CompanyBuyPrice = "", string Mealid = "");

        #endregion

        //prebook - 24-06-25
        Task<PriceCheckCMDto> PriceUpdateHotelBedsV1(string HTL_RoomCode, DataTable dtCheckPriceReqStructure, DataSet dsCartDetails, DataRow[] HotelBedsV1Details, string UniqueNo, string AgentUniqueNo, int i, string IsFor, double CompanyBuyPrice, bool IsError, double itemprice, string Searchkey = "", int AgentId = 0);

        //book 210825
        string HotelBedsdV1BookindgRequest(DataTable dtPassengers, DataSet dsCartDetails, bool IsPRiceUpdate, bool IsError, int j, string AgentCode);
        string HotelBedsdV1BookindgRequest_newversion(DataTable dtPassengers, BookRequest_newversion bookRequest, bool IsPRiceUpdate, bool IsError, int j, string AgentCode);

        Task<ResponseDto> GetHotelBedsSupplierResponse(string strHost, string Username, string Password, Int32 TimeOut, string strxmlRQRS, string methodtype, string Provider = "", HotelSearchParameter para = null, bool DebugMode = false, string Searchkey = "", bool fromCancel = false);
    }
}
