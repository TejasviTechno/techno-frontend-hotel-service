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

        Task<List<Options>> XMLDingusPriceOnly_Async(HotelX Item, CommonSearch commonSearch, string ResponseData, HotelSearchParameter SearchParam, string XMLCITY, string XMLCountry, string Currency, double CurrencyFactor, string XmlNationality, DataTable dtAgentDetail, string Provider, DataSet dsAgentDetails, DataTable dtVatHotelList, int SupplierID, string GIATACode, DataTable dtROE);

        Task<PriceCheckCMDto> PriceUpdateDingus(List<DingusClass.DingusRoom> DRList, string supplierName, string RoomTypeCode, string MealPlanCodes, string RatePlanID, string RatePlanCode, DataTable dtCheckPriceReqStructure, DataSet dsCartDetails, DataRow[] DingusDetails, string UniqueNo, string AgentUniqueNo, int i, int j, string IsFor, double CompanyBuyPrice, bool IsError, double itemprice,  TokenData tokenData, string Searchkey, int AgentId = 0);
        string DingusPriceCheckRequest(List<DingusClass.DingusRoom> DR, string checkIn, string checkOut, string HotelId, string Username, string Password, string DingusUrl, string SupplierName, string language);

    }
}

