using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XConnect.Models.Hotel;
using XConnect.Models;

namespace XConnect.Supplier.PkFare.Interface
{
    public interface IPkFare
    {
        void GetPkFareRequestOnly_Async(DataRow[] commonCode, List<StarRating> listRating, DataTable dtPreferredDataHotel, HotelSearchParameter SearchPara, List<SupplierSearchConfig> PkFareSearchConfig, List<SupplierDetails> PkFareDetails, ref ConcurrentBag<HotelX> AllTask, string AgentCode, DataSet dtXMLCity, int Hit, DataTable dtROE, TokenData tokenData);
        Task<List<Options>> PkFare_HotelSearchWithRooms(DataTable dtROE, CommonSearch commonSearch, HotelX item, string ResponseData, string WSurl, string Username, string Password, Int32 SearchTimeOut, HotelSearchParameter SearchParam, bool DebugMode, string Currency, double CurrencyFactor, DataSet dsAgentDetails, string SupplierName, DataTable dtCountryCityMarkup, Int32 AgentId, Int32 TariffID, double AgentPercentage, Boolean IsShowSupplier, int SupplierID, int MasterSupplierID, Int32 processTimeout, string ResProccessTimerLogActive, string ResProccessTimeValueforSaveLog);

        Task<string> Get_PkFare_EInfo(string hotelcode, HotelSearchParameter SearchPara, string Password, string Action, string Username, string SearchKey, string UserID, string Provider,string URL);

        Task<PriceCheckCMDto> GetPkfarePriceCheckREQRES(DataSet dsCartDetails, DataRow[] PkFareDetails, int j, DataTable dtCheckPriceReqStructure, string UniqueNo, string AgentUniqueNo, double MainPrice, int UserCartId, double CompanyBuyingPrice, int AgentId, TokenData tokenData);

        string PkFare_BookingRequest(DataTable dtPassengers, DataSet dsCartDetails, string RoomTypeId, ref string ReferenceNo, int i, int j);

        Task<ResponseDto> GetPkFareResponse(string strHost, string UserID, string UserName, string Password, string strxmlRQRS, string Error, string Method, string UniqueNo = "", bool from = false);
    }
}
