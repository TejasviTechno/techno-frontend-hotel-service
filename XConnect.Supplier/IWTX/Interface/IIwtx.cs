using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XConnect.Models.Hotel;
using XConnect.Models;
using Microsoft.IdentityModel.Protocols;
using static Azure.Core.HttpHeader;
using System.Xml.Linq;
using XConnect.CommonUtilities;

namespace XConnect.Supplier.IWTX.Interface
{
    public interface IIwtx
    {
        void IWTXRequestOnly_async(DataRow[] commonCode, List<StarRating> listRating, HotelSearchParameter SearchPara, List<SupplierDetails> IWTXDetails, List<SupplierSearchConfig> IWTXSearchConfig, DataTable dtPreferredDataHotel, ref ConcurrentBag<HotelX> AllTask, string XMLCity, string XMLCountry, string XMLNationality, int Hit, DataTable dtROE, TokenData tokenData);

        void IWTXRequestOnly_async_new(DataRow[] commonCode, List<StarRating> listRating, HotelSearchParameter SearchPara, List<SupplierDetails> IWTXDetails, List<SupplierSearchConfig> IWTXSearchConfig, DataTable dtPreferredDataHotel, ref ConcurrentBag<HotelX> AllTask, int Hit, DataTable dtROE, TokenData tokenData, DataSet dtXMLCity);

        Task<List<Options>> IWTX_HotelSearchWithRooms(HotelX Item, CommonSearch commonSearch, string currency, string ResponseData, string CityCode, HotelSearchParameter SearchParam, double CurrencyFactorJacTravel, string Supplier, string action, DataSet dsAgentDetails, Int32 processTimeout, string ResProccessTimerLogActive, string ResProccessTimeValueforSaveLog, string StoreSupplierResErrorLog, DataTable DtROE, string ProccessCutOffTime);

        #region Recheck - 28-05-25
        Task<string> Get_IWTX_EInfo(string hotelcode, string ownsystemid, string roomtypeid, HotelSearchParameter SearchPara, string Password, string Action, string Username, string SearchKey,string provider);

        Task<CancellationDto> IWTX_cancellation(string SearchKey, string OwnsystemHotelId, string roomtypecode, DataSet dsHotel, string hotelcode, string ownsystemid, string roomtypeid, DataTable dtAgentDetail, DataTable dtSuplierMarkup,DataSet dsCountryCityMarkup, DataTable dtSearchConfig, HotelSearchParameter SearchPara, string roomno, string provider, int AgentId, DataTable dtMarkupDetails,TokenData tokenData, string GiataId = "0", int hotelRating = 0, string CompanyBuyPrice = "", int RoomNo = 0, string XmlCity = "", string Uniqueid = "", string XmlNationality = "", string ContractTokenId = "", string MealPlanCode = "", string TechName = "");
        #endregion
        //Prebook - 22-07-25
        Task<PriceCheckCMDto> PriceUpdate_IWTX(string RoomTypeCode, string MealPlanCode, string RoomType, string ContractTokenId, string XmlCity, string Uniqueid, int NoOfAdults, int NoofChild, int Child1Age, int Child2Age, int RoomConfigurationId, DataTable dtCheckPriceReqStructure, DataSet dsCartDetails, DataRow[] IWTXDetails, string UniqueNo, string AgentUniqueNo, int i, int j, string IsFor, double CompanyBuyPrice, bool IsError, double itemprice, string iwtxprice, int AgentId, string Searchkey,TokenData tokenData,string Supplier,string TechName= "");

        string BookingRequest_IWTX(DataSet dsCartDetails, DataTable dtPassengers, DataRow[] IWTXDetails, string UniqueNo, int i, int jj, string ReferenceNo);
        string BookingRequest_IWTX_newversion(BookRequest_newversion bookRequest, DataTable dtPassengers, DataRow[] IWTXDetails, string UniqueNo, int i, int jj, string ReferenceNo);

        Task<ResponseDto> IWTXResponse(string URL, string request, string Provider, string SearchKey);

        string CancelationRequest_IWTX(DataRow[] IWTXDetails, string AgentVoucherNo, string Uniqueid);
        string RetrieveHotelBookingCancelationRequest_IWTX(DataRow[] IWTXDetails, string AgentVoucherNo, string Uniqueid);
    }
}
