using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XConnect.Models.Hotel;
using XConnect.Models;

namespace XConnect.Supplier.PriceTravel.Interface
{
    public interface IPriceTravel
    {
        void GetPriceTravelRequestOnly_async(DataRow[] commonCode, DataTable dtROE, List<StarRating> listRating, DataTable dtPreferredDataHotel, HotelSearchParameter SearchPara, List<SupplierSearchConfig> lstPrice_TravelSearchConfig, List<SupplierDetails> lstPrice_TravelDetails, ref ConcurrentBag<HotelX> AllTask, int hit, string AgentCode, TokenData tokenData, DataSet dtXMLCity);

        Task<List<Options>> XMLPriceTravelPriceOnly_async(HotelX Item, CommonSearch commonSearch, double AgentPercentage, int SearchTimeOut, string Supplier, DataTable dtCountryCityMarkup, int AgentId, DataSet dsAgentDetails, int SupplierID, string ResponseData, HotelSearchParameter SearchParam, string xmlcity, string xmlcountry, string xmlnationality, double CurrencyFactor, Boolean IsShowSupplier, string HotelID, DataTable dtROE, DataTable dtSuplierMarkup, string currency, string StoreSupplierResErrorLog);

        Task<string> PriceTravel_EssentialInfoOnly(string RoomTypeId, DataRow[] PriceTravelDetails, TokenData tokenData);

        Task<PriceCheckCMDto> PriceUpdateHotelPriceTravel(string[] HTL_RoomCode, DataTable dtCheckPriceReqStructure, DataSet dsCartDetails, DataRow[] PriceTravelDetails, string UniqueNo, string AgentUniqueNo, int i, string IsFor, double CompanyBuyPrice, bool IsError, double itemprice, string RequestSPR, string ReponseSPR,TokenData tokenData);

        string PriceTravelTokenGeneration(SupplierDetails Price_TravelDetails);

        Task<ResponseDto> GetPriceTravelSupplierResponse(string Error_str, string priceTravelToken, string strHost, string Username, string Password, Int32 TimeOut, string strxmlRQRS, string methodtype, string Error, string Provider = "", HotelSearchParameter para = null, bool DebugMode = false);

        Task<PriceTravelBookRes> PriceTravelBookingHoldVerified(string[] DsCartDetailsArray, DataTable dtCheckPriceReqStructure, DataTable dtCurrentCartDetails/*DataSet dsCartDetails*/, DataRow[] PriceTravelDetails, string UniqueNo, string AgentUniqueNo, int i, string IsFor, bool IsError, string purchaseToken_pricetravel, TokenData tokenData);
        string PriceTravelBookindgRequestModified(string BookingKey, DataTable dtPassengers, DataSet dsCartDetails, bool IsError, int j, string AgentCode, int AgentId = 0);
    }
}
