using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XConnect.Models.Hotel;
using XConnect.Models;

namespace XConnect.Supplier.HotelsPro.Interface
{
    public interface IHotelsPro
    {
        void GetHotesProRequestOnly_async(DataRow[] commonCode, List<StarRating> listRating, DataTable dtPreferredDataHotel, HotelSearchParameter SearchPara, List<SupplierSearchConfig> lstHPSearchConfig, List<SupplierDetails> lstHPDetails, bool IsHotelWiseSearch, ref ConcurrentBag<HotelX> AllTask, string AgentCode, int Hit, TokenData tokenData, DataSet dtXMLCity, DataTable dtROE);

        Task<List<Options>> HotelsPro_HotelSearchWithRooms(HotelX Item, CommonSearch commonSearch, string WebSiteURL, int SearchTimeOut, string Supplier, bool DebugeMode, string UserName, string PassWord, string MethoodType, string ResponseData, HotelSearchParameter SearchParam, string xmlcity, string xmlcountry, string xmlnationality, string currency, double currencyfactor, Int32 processTimeout, string ResProccessTimerLogActive, string ResProccessTimeValueforSaveLog);

        #region Recheck 29-05-25
        Task<CancellationDto> HotelsPro_cancellation(string roomtypecode, DataSet dsHotel, string hotelcode, string ownsystemid, string roomtypeid, DataTable dtAgentDetail, DataTable dtSuplierMarkup, DataTable dtSearchConfig, HotelSearchParameter SearchPara, string roomno, string provider, int AgentId, DataTable dtMarkupDetails, string GiataId = "0", int hotelRating = 0,string SearchKey="");
        Task<string> HotelsPro_EInfo(string ownsystemid, string roomtypeid, DataTable dtSuplierMarkup, DataTable dtSearchConfig, string provider,string SearchKey);
        #endregion

        //prebook 24-06-25
        Task<PriceCheckhotelsproDto> GetPriceUpdateRQRS(string supplierName, string HTL_RoomCode, DataTable dtCheckPriceReqStructure, DataSet dsCartDetails, DataRow[] HotelsProDetails, string UniqueNo, string AgentUniqueNo, int i, string IsFor, double CompanyBuyPrice,  bool IsError,  double itemprice,  string roomCode, string Searchkey = "", int AgentId = 0,string TechName="");

        string HotelsProBookindgRequestNEW(DataTable dtPassengers, DataSet dsCartDetails, string UniqueNo, bool IsPRiceUpdate, bool IsError, string AgentCode, string roomCodeHP, string SearchKey);

        Task<ResponseDto> ProResponse(string Xmlrequest, string Url, string username, string password, int Timeout, string Searchkey = "", bool ForRoom = false,bool FromBooking = false, string RequestBody = "");
    }
}
