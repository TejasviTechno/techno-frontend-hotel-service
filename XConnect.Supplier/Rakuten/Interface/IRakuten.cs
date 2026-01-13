using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XConnect.Models.Hotel;
using XConnect.Models;

namespace XConnect.Supplier.Rakuten.Interface
{
    public interface IRakuten
    {
        void RakutenRequestOnly_async(DataRow[] commonCode, List<StarRating> listRating, HotelSearchParameter SearchPara, List<SupplierDetails> lstRakutenDetails, List<SupplierSearchConfig> lstRakutenSearchConfig, DataTable dtPreferredDataHotel, ref ConcurrentBag<HotelX> AllTask, int Hit, DataTable dtROE, TokenData tokenData, bool HitFromAll, DataSet dtXMLCity);

        Task<List<Options>> Rakuten_HotelSearchWithRooms(HotelX Item, CommonSearch commonSearch, string Currency, string ResponseData, string CityCode, HotelSearchParameter SearchParam, double CurrencyFactorTBO, string XMLnationality, string URLandRequest, string UserName, string PassWord, string URL, Int32 SearchTimeOut, string custIP, Int32 processTimeout, string ResProccessTimerLogActive, string ResProccessTimeValueforSaveLog);

        //recheck 30-05-25
        Task<CancellationDto> Rakuten_Cancellation(double CompanyBuy, string MealCode, DataSet dsHotel, string hotelcode, string ownsystemid, string roomtypeid, DataTable dtAgentDetail, DataTable dtSuplierMarkup, DataTable dtSearchConfig, HotelSearchParameter SearchPara, string roomno, string provider, int AgentId, DataTable dtMarkupDetails, string GiataId = "0", int hotelRating = 0);

        //Prebook 22-07-25
        Task<PriceCheckCMDto> PriceUpdateRakuten(DataTable dtCheckPriceReqStructure, DataSet dsCartDetails, DataRow[] RakutenDetails, string UniqueNo, string AgentUniqueNo, int i, int j, string IsFor, double CompanyBuyPrice, int AgentId, bool IsError, double itemprice, TokenData tokenData);

        string Rakuten_PriBookingRQ(DataSet dsCartDetails, DataTable dtPassengers, DataRow[] DerbySoftDetails, string UniqueNo, bool IsPriceUpdate, int i, int j, string PreBookingToken, string UserName, string Password, string ExternalReference, string xmlNationality);
        Task<ResponseDto> Response_RakutenSupplier(string request, string Password, string type, string name);
        Task<ResponseDto> Get_Rakuten_Response(string RakutenURL, string pass, string request, string Provider);
    }
}
