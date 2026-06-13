using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Data;
using XConnect.Models;
using XConnect.Models.Hotel;


namespace XConnect.Supplier.Dingus.Interface


{
    public interface IDingus
    {
        void DingusRequestOnly_async(DataRow[] commonCode, List<StarRating> listRating, DataTable dtROE, HotelSearchParameter SearchPara, List<SupplierDetails> DingusDetails, List<SupplierSearchConfig> DingusSearchConfig, DataTable dtPreferredDataHotel, ref ConcurrentBag<HotelX> AllTask, string XMLCity, string XMLCountry, string XMLNationality, int Hit, string supplierName, TokenData tokenData);
        void DingusRequestOnly_async_new(DataRow[] commonCode, List<StarRating> listRating, DataTable dtROE, HotelSearchParameter SearchPara, List<SupplierDetails> DingusDetails, List<SupplierSearchConfig> DingusSearchConfig, DataTable dtPreferredDataHotel, ref ConcurrentBag<HotelX> AllTask, int Hit, TokenData tokenData, DataSet dtXMLCity);

        Task<List<Options>> XMLDingusPriceOnly_Async(HotelX Item, CommonSearch commonSearch, string ResponseData, HotelSearchParameter SearchParam, string XMLCITY, string XMLCountry, string Currency, double CurrencyFactor, string XmlNationality, DataTable dtAgentDetail, string Provider, DataSet dsAgentDetails, DataTable dtVatHotelList, int SupplierID, string GIATACode, DataTable dtROE);
        Task<List<Options>> XMLDingusPriceOnly_Async_Multiroom(HotelX Item, CommonSearch commonSearch, string ResponseData, HotelSearchParameter SearchParam, string XMLCITY, string XMLCountry, string Currency, double CurrencyFactor, string XmlNationality, DataTable dtAgentDetail, string Provider, DataSet dsAgentDetails, DataTable dtVatHotelList, int SupplierID, string GIATACode, DataTable dtROE);


        Task<PriceCheckCMDto> PriceUpdateDingus(string supplierName, DataTable dtCheckPriceReqStructure, DataSet dsCartDetails, DataRow[] DingusDetails, string UniqueNo, string AgentUniqueNo, int i, int j, string IsFor, double CompanyBuyPrice, bool IsError, double itemprice, TokenData tokenData, string Searchkey, int AgentId = 0, string TechName = "");

        string Get_Dingus_BookingRequestNew(BookRequest_newversion bookRequest, DataTable dtPassengers, DataRow[] DingusDetails, string UniqueNo, int i, int jj, string ReferenceNo, string SpecialRequestToPass, string CheckIn, string CheckOut);
       
        Task<ResponseDto> Get_Dingus_Response_timeout(string DingusURL, string request, string Provider, string referenceNumber, string CheckIn, string CheckOut, string Error, string HostName = "");
        string Get_Dingus_BookingCancelRQ(string AgentVoucher, string Username, string Password, string HotelCode, string ChainCode, string language);
        Task<ResponseDto> Get_Dingus_Response_v1(string DingusURL, string request, string Provider, string referenceNumber, string CheckIn, string CheckOut, string Error, string HostName = "");


    }
}

