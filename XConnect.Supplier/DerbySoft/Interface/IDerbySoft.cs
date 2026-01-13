using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XConnect.Models.Hotel;
using XConnect.Models;

namespace XConnect.Supplier.DerbySoft.Interface
{
    public interface IDerbySoft
    {
        void GetDerbySoftRequestOnly_async(List<SupplierDetails> DerbySoftDetails, DataRow[] commonCode, List<StarRating> listRating, DataTable dtPreferredDataHotel, HotelSearchParameter SearchPara, List<SupplierSearchConfig> DerbySoftSearchConfig, ref ConcurrentBag<HotelX> AllTask, DataTable dtAgentDetail, DataSet dsAgentDetails, DataTable dtMarkupDetails, int Hit);

        string GenerateGTARequestAsync(HotelSearchParameter objSearchPara, decimal AgentPercentage);
        Task<List<Options>> Derbysoft_HotelSearchWithRooms_prtk(HotelX Item, CommonSearch commonSearch, string currency, string Response, HotelSearchParameter SearchParam, DataTable dtAgentDetail, DataTable dtSuplierMarkup, DataTable dtMarkupDetails, string Supplier, DataSet dsAgentDetails, DataTable dtCountryCityMarkup, DataTable dtROE, DataSet DerbyResponse, Int32 processTimeout, string ResProccessTimerLogActive, string ResProccessTimeValueforSaveLog);

        //Recheck - 300525

        Task<CancellationDto> Get_derbysoft_CancellationPolicy(double RoomPrice, string rating, string ContractID, string Type, DataSet dsHotel, string hotelcode, string citycode, string OwnsystemHotelId, string RoomTypeId, string MealID, string Board_Text, DataTable dtAgentDetail, DataTable dtSuplierMarkup, DataTable dtSearchConfig, HotelSearchParameter SearchParam, string roomno, string provider, int AgentId, DataTable dtMarkupDetails, string SearchKey, string GiataId = "0", int hotelRating = 0,string SiteCurrency= "USD");

        //prebook - 070725
        Task<PriceCheckCMDto> DerbySoft_PriceCheckRQRS(DataSet dsCartDetails, DataRow[] DerbySoftDetails, string UserId, string Password, int i, int j, DataTable dtCheckPriceReqStructure, string UniqueNo, string AgentUniqueNo, double FinalPrice, bool isForBooking, double CompanyBuyingPrice, string PreBookingToken, string xmlhotelid, string Searchkey = "", int AgentId = 0);

        string Get_DerbySoft_PriceCheckRQ(DataSet dsCartDetails, DataRow[] DerbySoftDetails, string DerbySoftUserId, string DerbySoftUserName, string DerbySoftPassword, string PropertyId, string CheckIn, string checkout, string RoomTypeId, string MealID, ref string RoomName, string Type, int RoomNo, string xmlhotelid, int NoOfRoom, string version, int j);
        Task<ResponseDto> GetDerbySoftResponse(string Error, string request, string TravcoURL, Int32 TimeOut, string Provider = "", string Password = "", HotelSearchParameter para = null, bool DebugMode = false);

        public string DerbySoft_PriBookingRQ(DataSet dsCartDetails, DataTable dtPassengers, DataRow[] DerbySoftDetails, string UniqueNo, bool IsPriceUpdate, int i, int j, string PreBookingToken, string UserName, string Password, string ExternalReference);
        string Get_DerbySoft_BookingRequest(DataSet dsCartDetails, DataTable dtPassengers, DataRow[] DerbySoftDetails, string UniqueNo, bool IsPriceUpdate, int i, int j, string PreBookingToken, string UserID, string Password, string ExternalReference, string Bookingtokan, bool IsPruvo);

        string Get_DerbySoft_BookingDetailsRQ(string AgentVoucherNo, DataRow[] SupplierDetails, DataSet dsCartDetails);

        string Get_DerbySoft_BookingCancelRQ(string AgentVoucherNo, DataRow[] SupplierDetails, string RoomTypeId);

        string Get_DerbySoft_BookingDetailsRQ_AFTC(string AgentVoucherNo, DataRow[] SupplierDetails, string RoomTypeId);
    }
}
