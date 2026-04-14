using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XConnect.Models.Hotel;
using XConnect.Models;

namespace XConnect.Supplier.ETG.Interface
{
    public interface IEtg
    {
        void GetOstrovokRequestOnly_Async(DataRow[] commonCode, List<StarRating> listRating, HotelSearchParameter SearchPara, List<SupplierDetails> lstOstrovok_Details, List<SupplierSearchConfig> lstOstrovokSearchConfig, DataTable dtPreferredDataHotel, ref ConcurrentBag<HotelX> AllTask, int Hit, DataTable dtROE, TokenData tokenData, DataSet dtXMLCity);

        Task<List<Options>> Ostrovok_HotelSearchWithRoomswithBookHash(HotelX Item, CommonSearch commonSearch, string Currency, string ResponseData, string CityCode, HotelSearchParameter SearchParam, double CurrencyFactorOstrovok, String Provider, string XMLnationality, string URLandRequest, string UserName, string PassWord, string URL, Int32 SearchTimeOut, DataTable dtROE, string ProccessCutOffTime, Int32 processTimeout);

        //recheck 11-07-25
        Task<string> RateHawk_EssentialInfo(HotelSearchParameter SearchPara, string Supplier, string xmlhotelcode, string UserName, string PassWord, string Currency, string UserID, string RoomTypeId,string URL,string SearchKey);
        Task<RecheckETG> RateHawk_EssentialInfoNew(HotelSearchParameter SearchPara, string Supplier, string xmlhotelcode, string UserName, string PassWord, string Currency, string UserID, string RoomTypeId,string URL,string SearchKey);

        //prebook 23-07-25
        Task<PriceCheckCMDto> Get_Ostrovok_Prebook(DataSet dsCartDetails, DataRow[] OstrovokDetails, double FinalPrice, double CompanyBuyingPrice, TokenData TokenData, string Searchkey, int AgentId, int i = 0,string Supplier="");
        Task<PriceCheckCMDto> Get_Ostrovok_Prebook_new(Shopping_CartDataForHotelXML objdata, DataSet dsCartDetails, DataRow[] OstrovokDetails, double FinalPrice, double CompanyBuyingPrice, TokenData TokenData, string Searchkey, int AgentId, int i = 0,string Supplier="");

        string OSTVBookingRequest(DataSet dsCartDetails, DataTable dtPassengers, int i, int j, string Password, string UniqueNo, int NoOfRoom, string RoomTypeId, string HotelCode, string SellingAmount, string type, string Currency);
        string OSTVBookingRequest_newvesrion(BookRequest_newversion bookRequest, DataTable dtPassengers, int i, int j, string Password, string UniqueNo, int NoOfRoom, string RoomTypeId, string HotelCode, string SellingAmount, string type, string Currency);
        string OSTVBookingFinishStatusRequest(string UniqueNo);

        Task<ETGPriceCheck> Get_OSTV_PriceCheck(DataSet dsCartDetails, DataRow[] Ostrovok_Details, int i, int j, DataTable dtCheckPriceReqStructure, string UniqueNo, bool isForBooking, string IsFor, string URl, TokenData TokenData, string xslRequest, string ResponseData, string order_id,string UserPlatform);
        Task<ETGPriceCheck> Get_OSTV_PriceCheck_newversion(BookRequest_newversion bookRequest, DataRow[] Ostrovok_Details, int i, int j, string UniqueNo, bool isForBooking, string IsFor, string URl, TokenData TokenData, string xslRequest, string ResponseData, string order_id, string UserPlatform);
        Task<string> GetRatehawkRoomResponse(string strxmlRQRS, string strHost, string Key, string UniqueNo = "",int Timeout=60000);
        Task<ResponseDto> GetETGRoomResponse(string strxmlRQRS, string strHost, string Key, string UniqueNo = "");

        string EtgBookingCancelRQ(string Id);
        Task<DataSet> Ratehawk_cancellation( string ISBooking, double CompanyBuy, string MealCode, DataSet dsHotel, string hotelcode, string ownsystemid, string roomtypeid, DataTable dtAgentDetail, DataTable dtSuplierMarkup, DataTable dtSearchConfig, HotelSearchParameter SearchPara, string roomno, string provider, int AgentId, DataTable dtMarkupDetails, TokenData TokenData, string SearchKey = "", string GiataId = "0", int hotelRating = 0, string xmlcity = "", string TechName = "");
    }
}
