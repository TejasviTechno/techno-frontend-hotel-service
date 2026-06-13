using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XConnect.Models.Hotel;
using XConnect.Models;

namespace XConnect.Supplier.Juniper.Interface
{
    public interface IJuniper
    {
        void GetJuniperNewRequestOnly_async_new(DataRow[] commonCode, List<StarRating> listRating, DataTable dtROE, bool IsHotelWiseSearch, bool HitFromAll, HotelSearchParameter SearchPara, List<SupplierDetails> JuniperDetails, List<SupplierSearchConfig> JuniperSearchConfig, DataTable dtPreferredDataHotel, ref ConcurrentBag<HotelX> AllTask, int Hit, TokenData tokenData, DataSet dtXMLCity, bool SearchByOneID);
        //void GetJuniperNewRequestOnly_async(DataRow[] commonCode, List<StarRating> listRating, DataTable dtROE, bool IsHotelWiseSearch, bool HitFromAll, HotelSearchParameter SearchPara, List<SupplierDetails> JuniperDetails, List<SupplierSearchConfig> JuniperSearchConfig, DataTable dtPreferredDataHotel, ref ConcurrentBag<HotelX> AllTask, int Hit, TokenData tokenData, DataSet dtXMLCity);
        Task<List<Options>> Juniper_HotelSearchWithRooms(HotelX Item, CommonSearch commonSearch, string ResponseData, HotelSearchParameter SearchParam, string Provider, string CurrencyCode, double CurrencyFactorAlphaTours, string xmlcity, string xmlcountry, string xmlnationality, string ProccessCutOffTime, Int32 processTimeout, string ResProccessTimerLogActive, string ResProccessTimeValueforSaveLog, string StoreSupplierResErrorLog, bool SearchByOneID);

        #region Recheck 28-05-25
        Task<string> Get_JuniperNew_EssentialInfoOnly(HotelSearchParameter Searchpara, string hotelcode, string RoomTypeId_Own, DataRow[] drSuplierMarkupTA, DataTable dtSearchConfig, string Provider, DataTable dtSuplierMarkup,string SupplierCurrency, string SearchKey);
        Task<string> Get_JuniperNew_Robios_EssentialInfoOnly(HotelSearchParameter Searchpara, string hotelcode, string RoomTypeId_Own, DataRow[] drSuplierMarkupTA, DataTable dtSearchConfig, string Provider, DataTable dtSuplierMarkup, string Currency); //Added on 10-APR-2025

        Task<CancellationDto> Get_JuniperNew_CancellationPolicy(DataSet dsHotel, string hotelcode, string ownsystemid, string RoomTypeId_Own, DataTable dtAgentDetail, DataTable dtSuplierMarkup, DataTable dtMarkupDetails, DataTable dtSearchConfig, HotelSearchParameter objSearPera, string roomno, string provider, int AgentId,string SearchKey, string GiataId = "0", int hotelRating = 0, TokenData tokenData = null);
        Task<string> Get_JuniperNew_PriceCheckInRecheck(TokenData tokenData, bool IsPriceChange, string CheckIn, string CheckOut, DataRow[] SupplierDetails, string SupplierName, int j, DataTable dtCheckPriceReqStructure, string UniqueNo, string AgentUniqueNo, double FinalPrice, bool isForBooking, string HotelId, int UserCartId, int UserCartDetailId, string RoomTypeId, double CompanyBuyingPrice, string NewRatePlan, bool IsError, string Currency, string ISBooking, string Searchkey = "", int AgentId = 0); //new added on 03-May-2025
        #endregion

        //prebook 25-06-25
        Task<PriceCheckCMDto> Get_JuniperNew_PriceCheckRQRS(int i, DataSet dsCartDetails, DataRow[] SupplierDetails, string SupplierName, int j, DataTable dtCheckPriceReqStructure, string UniqueNo, string AgentUniqueNo,double FinalPrices_JuniperNew, bool isForBooking, string HotelId, int UserCartId, int UserCartDetailId, string RoomTypeId, double CompanyBuyingPrice,  string NewRatePlan,bool IsError_JuniperNew,  string Currency, string Searchkey = "", int AgentId = 0,string TechName="");

        //book 210825
        void Get_Juniper_BookingProcess(string AgentCode, ref DataTable dtAgentVoucher, ref DataSet dsCartDetails, ref DataTable dtCheckPriceReqStructure, ref DataTable dtPassengers, ref DataTable dtCAncellationPolicy, HotelSearchParameter SearchPara, DataRow[] SupplierDetails,TokenData tokenData, ref string AgentVoucherNo, ref int bookingStatus, ref string UniqueNo, ref string AgentUniqueNo, ref bool isNonRefundable, int i, string SearchKey, int AgentId, bool IsPruvo);

        string Get_JuniperNew_BookingCancelRQ(string AgentVoucherNo, DataRow[] SupplierDetails, string Provider = "");
        string Get_LOH_RoomTypeID(string RoomTypeId, ref string Nationality);
        string Get_Juniper_HotelBookingRulesRQ_newversion(bool isNonRefundable, double CurrencyFactor, string Currency, BookRequest_newversion bookRequest, DataRow[] SupplierDetails, string RatePlanCode, string HotelId, int i, int j, string SupplierName, string UniqueNo, string AgentUniqueNo, bool IsPriceUpdate, ref double Gross, ref string Paxdetails,bool IsError);
        string Get_Juniper_BookingRequest_newversion(int i, BookRequest_newversion bookRequest, string AgentCode, DataTable dtPassengers, DataRow[] SupplierDetails, string BookingCode, double Gross, string UniqueNo, string Provider, string RatePlanCode, string Currency, string Paxdetails, bool IsPruvo);
         string GetSupplierResponse_report(ref string Error, string strUniqueId, string strxmlRQRS, string strHost, Int32 TimeOut, string Provider = "", HotelSearchParameter para = null, bool DebugMode = false, string Searchkey = "");
        Task<ResponseDto> GetSupplierResponse_report_new(string Error, string strUniqueId, string strxmlRQRS, string strHost, Int32 TimeOut, string Provider = "", HotelSearchParameter para = null, bool DebugMode = false, string Searchkey = "");

    }
}
