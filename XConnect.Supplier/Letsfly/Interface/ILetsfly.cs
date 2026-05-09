using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XConnect.Models.Hotel;
using XConnect.Models;

namespace XConnect.Supplier.Letsfly.Interface
{
    public interface ILetsfly
    {
        void GetLetsflyRequestOnly_Async(DataRow[] commonCode, List<StarRating> listRating, DataTable dtPreferredDataHotel, HotelSearchParameter SearchPara, List<SupplierSearchConfig> lstLetsFlySearchConfig, List<SupplierDetails> lstLetsFlyDetails, ref ConcurrentBag<HotelX> AllTask, string AgentCode,  int Hit, TokenData tokenData, DataSet dtXMLCity);

        Task<List<Options>> Letsfly_HotelSearchWithRooms(HotelX item, CommonSearch commonSearch, string ResponseData, string WSurl, string Username, string Password, Int32 SearchTimeOut, HotelSearchParameter SearchParam, bool DebugMode, string Currency, double CurrencyFactor, DataSet dsAgentDetails, string Provider, DataTable dtCountryCityMarkup, Int32 AgentId, Int32 TariffID, double AgentPercentage, Boolean IsShowSupplier, int SupplierID, int MasterSupplierID, Int32 processTimeout, string ProccessCutOffTime, string ResProccessTimerLogActive, string ResProccessTimeValueforSaveLog, string StoreSupplierResErrorLog, DataTable dtROE, DataTable dtSuplierMarkup);

        Task<PriceCheckCMDto> GetLetsflyPriceCheckREQRES(DataSet dsCartDetails, DataRow[] letsflyDetails, int j, DataTable dtCheckPriceReqStructure, string UniqueNo, string AgentUniqueNo, double MainPrice, int UserCartId, double CompanyBuyingPrice, int AgentId, TokenData tokenData);
        Task<PriceCheckCMDto> GetLetsflyPriceCheckREQRES_newversion(Shopping_CartDataForHotelXML objdata, DataSet dsCartDetails, DataRow[] letsflyDetails, int j, DataTable dtCheckPriceReqStructure, string UniqueNo, string AgentUniqueNo, double MainPrice, int UserCartId, double CompanyBuyingPrice, int AgentId, TokenData tokenData, string Searchkey,string TechName="");



        string Letsfly_BookingRequest(DataTable dtPassengers, DataSet dsCartDetails, string RoomTypeId, string sessionid, string Rate, ref string ReferenceNo, int i, int j, string roomname);
        string Letsfly_BookingRequest_newversion(DataTable dtPassengers, BookRequest_newversion bookRequest, string RoomTypeId, string sessionid, string Rate, ref string ReferenceNo, int i, int j, string roomname,string hotelid,string nationality);
        Task<ResponseDto> GetletsflyResponse(string strHost, string UserID, string UserName, string Password, string strxmlRQRS, string Error, string UniqueNo = "");

        string LetsflyBookingCancelReq(string AgentVoucherNo);
    }
}
