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

        //prebook 23-07-25
        Task<PriceCheckCMDto> Get_Ostrovok_Prebook(DataSet dsCartDetails, DataRow[] OstrovokDetails, double FinalPrice, double CompanyBuyingPrice, TokenData TokenData, string Searchkey, int AgentId, int i = 0,string Supplier="");

        string OSTVBookingRequest(DataSet dsCartDetails, DataTable dtPassengers, int i, int j, string Password, string UniqueNo, int NoOfRoom, string RoomTypeId, string HotelCode, string SellingAmount, string type, string Currency);
        string OSTVBookingFinishStatusRequest(string UniqueNo);

        Task<ETGPriceCheck> Get_OSTV_PriceCheck(DataSet dsCartDetails, DataRow[] Ostrovok_Details, int i, int j, DataTable dtCheckPriceReqStructure, string UniqueNo, bool isForBooking, string IsFor, string URl, TokenData TokenData, string xslRequest, string ResponseData, string order_id,string UserPlatform);

        Task<string> GetRatehawkRoomResponse(string strxmlRQRS, string strHost, string Key, string UniqueNo = "");
        Task<ResponseDto> GetETGRoomResponse(string strxmlRQRS, string strHost, string Key, string UniqueNo = "");

        string EtgBookingCancelRQ(string Id);
    }
}
