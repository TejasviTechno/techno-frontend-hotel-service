using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XConnect.Models.Hotel;
using XConnect.Models;

namespace XConnect.Supplier.Hyperguest.Interface
{
    public interface IHyperguest
    {
        void GethyperguestRequestOnly_Async(DataRow[] commonCode, List<StarRating> listRating, HotelSearchParameter SearchPara, List<SupplierDetails> hyperguestDetails, List<SupplierSearchConfig> hyperguestSearchConfig, DataTable dtPreferredDataHotel, ref ConcurrentBag<HotelX> AllTask, string XMLCity, string XMLCountry, string XMLNationality, int Hit, DataTable dtROE, string Provider, TokenData tokenData);
        void GethyperguestRequestOnly_Async_new(DataRow[] commonCode, List<StarRating> listRating, HotelSearchParameter SearchPara, List<SupplierDetails> hyperguestDetails, List<SupplierSearchConfig> hyperguestSearchConfig, DataTable dtPreferredDataHotel, ref ConcurrentBag<HotelX> AllTask, int Hit, DataTable dtROE, TokenData tokenData,DataSet dtXMLCity);
        Task<List<Options>> hyperguestHotelSearchWithRooms_within(HotelX Item, CommonSearch commonSearch, string ResponseData, string CityCode, HotelSearchParameter SearchParam, double CurrencyFactorHyperGuest, string Supplier, DataSet dsAgentDetails, DataTable dtMarkupDetails, string CurrencyCode, DataTable dtROE, string XmlNationality, Int32 processTimeout, string ResProccessTimerLogActive, string ResProccessTimeValueforSaveLog, string StoreSupplierResErrorLog);
        Task<List<Options>> hyperguestHotelSearchWithRooms_Multiroom_within(HotelX Item, CommonSearch commonSearch, string ResponseData, string CityCode, HotelSearchParameter SearchParam, double CurrencyFactorHyperGuest, string Supplier, DataSet dsAgentDetails, DataTable dtMarkupDetails, string CurrencyCode, DataTable dtROE, string XmlNationality, Int32 processTimeout, string ResProccessTimerLogActive, string ResProccessTimeValueforSaveLog, string StoreSupplierResErrorLog);

       Task<List<Options>> hyperguestHotelSearchWithRooms_Multiroom(HotelX Item, CommonSearch commonSearch, string ResponseData, string CityCode, HotelSearchParameter SearchParam, double CurrencyFactorHyperGuest, string Supplier, DataSet dsAgentDetails, DataTable dtMarkupDetails, string CurrencyCode, DataTable dtROE, string XmlNationality, Int32 processTimeout, string ResProccessTimerLogActive, string ResProccessTimeValueforSaveLog, string StoreSupplierResErrorLog);
        //prebook - 22-07-25
       Task<PriceCheckDto> hyperguestPriceCheck(DataSet dsCartDetails, DataRow[] hyperguestDetails, int j, double FinalPrice, double CurrencyFactorhyperguest, string UniqueNo, string AgentUniqueNo, double ResPrice, string SupplierName, int i, double CompanyBuyingPrice, int AgentId, TokenData tokenData);

        string Gethyperguest_BookingCancelRQ(string AgentVoucherNo, DataRow[] hyperguestDetails);

        Task<ResponseDto> GethyperguestResponse(string Request, string url, string Password, Int32 TimeOut = 180000);
        string hyperguestBookingRequest(DataSet dsCartDetails, DataRow[] hyperguestDetails, int i, int j, double resPrice, DataTable dtPassengers, ref string AgtRef, string CheckIn, string CheckOut, int UniqueNo, String AgentCode, bool IsPruvo);
    }
}
