using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XConnect.Models.Hotel;
using XConnect.Models;

namespace XConnect.Supplier.Restel.Interface
{
    public interface IRestel
    {
        void GetRestelRequestOnly_Async(DataRow[] commonCode, List<StarRating> listRating, HotelSearchParameter SearchPara, List<SupplierDetails> lstRestelDetails, List<SupplierSearchConfig> lstRestelSearchConfig, DataTable dtPreferredDataHotel, ref ConcurrentBag<HotelX> AllTask, int Hit, DataTable dtROE, TokenData tokenData, DataSet dtXMLCity);

        Task<List<Options>>Restel_HotelSearchWithRooms(string searchkey, HotelX Item, CommonSearch commonSearch, string currency, string ResponseData, string CityCode, HotelSearchParameter SearchParam, string ContryCode, double CurrencyFactorRestel, string Provider, string XmlNationality, Int32 processTimeout, string ResProccessTimerLogActive, string ResProccessTimeValueforSaveLog);
        //recheck - 300525
        Task<CancellationDto> Get_Restel_CancellationPolicy(string rating, string ContractID, string Type, DataSet dsHotel, string hotelcode, string ownsystemid, string RoomTypeId_Own, string MealID, string Board_Text, DataTable dtAgentDetail, DataTable dtSuplierMarkup, DataTable dtSearchConfig, HotelSearchParameter objSearPera, string UserId, string Password, string roomno, string provider, int AgentId, DataTable dtMarkupDetails, string SearchKey, string GiataId = "0", int hotelRating = 0, double FinalSellPrice = 0, string TechName = "");

        //prebook 01-07-25
        Task<PriceCheckCMDto> Get_Restel_PriceCheck(DataSet dsCartDetails, DataRow[] RestelDetails, int i, int j, string UniqueNo, string AgentUniqueNo, double FinalPrice, string a, double CompanyBuyingPrice,  string PreBookingToken, DataTable dtCheckPriceReqStructure, string Searchkey, int AgentId = 0,TokenData tokenData = null, string TechName = "");

        string Get_Restel_BookingRequest(DataSet dsCartDetails, DataTable dtPassengers, DataRow[] RestelDetails, string UniqueNo, bool IsPriceUpdate, int i, int j, string PreBookingToken, string UserID, string Password, string AgentUniqueNo);
        string Get_Restel_BookingRequest_newversion(BookRequest_newversion bookRequest, DataTable dtPassengers, DataRow[] RestelDetails, string UniqueNo, bool IsPriceUpdate, int i, int j, string PreBookingToken, string UserID, string Password, string AgentUniqueNo);

        Task<ResponseDto> GetRestelResponse(string request, string TravcoURL, Int32 TimeOut, string Provider = "", HotelSearchParameter para = null, bool DebugMode = false, string Searchkey = "",string TechName="");

        string FinalRestelBookingRequest(string localizer, string confi);

        string Get_Restel_PreCancelRQRS(string BookingReference, DataRow[] RestelDetails, string UserID, string Password, DataTable DtCancellationError, ref double CancellationCost, ref string CancellationToken);
        string Get_Restel_PreCancelRQRS_newversion(string BookingReference, DataRow[] RestelDetails, string UserID, string Password, ref double CancellationCost, ref string CancellationToken);
    }
}
