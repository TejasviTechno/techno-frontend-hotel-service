using System.Collections.Concurrent;
using System.Data;
using XConnect.Models;
using XConnect.Models.Hotel;

namespace XConnect.Supplier.Exely.Interface
{
    public interface IExely
    {
        void GetExelyRequestOnly_Async(DataRow[] commonCode, List<StarRating> listRating, DataTable dtPreferredDataHotel, HotelSearchParameter searchPara, List<SupplierSearchConfig> lstExelySearchConfig, List<SupplierDetails> lstExelyDetails, ref ConcurrentBag<HotelX> allTask, string agentCode, int hit, TokenData tokenData, DataSet dtXMLCity, AdvancedOptions advancedOptions);
        Task<List<Options>> Exely_HotelSearchWithRooms(HotelX item, CommonSearch commonSearch, string responseData, HotelSearchParameter searchParam, string xmlCity, string xmlCountry, string xmlNationality, string currency, double currencyFactor, DataTable dtROE, string proccessCutOffTime, Int32 processTimeout, string resProccessTimerLogActive, string resProccessTimeValueforSaveLog, string storeSupplierResErrorLog);
        Task<CancellationDto> Get_Exely_CancellationPolicy(int roomno, string hotelcode, DataSet dsHotel, string ownsystemid, string roomtypeid, DataTable dtAgentDetail, DataTable dtSuplierMarkup, DataTable dtMarkupDetails, DataTable dtSearchConfig, HotelSearchParameter searchPara, string optionCode, int AgentId, double roomprice, double companybuy, string searchKey, string supplier, string giataId = "0", int hotelRating = 0);
        Task<PriceCheckCMDto> Get_Exely_PriceCheckRQRS(DataSet dsCartDetails, DataRow[] ExelyDetails, int i, int j, DataTable dtCheckPriceReqStructure, string uniqueBookingToken, string agentUniqueNo, bool isForBooking, double companyBuyingPriceA, string xmlhotelid, string isFor, bool issErrorY, double itempriceY, string searchkey, int agentId = 0, TokenData tokenData = null);
        string Get_Exely_BookingRequest(DataSet dsCartDetails, DataTable dtPassengers, string uniqueBookingToken, int i, int j, string hotelcode, string checkIn, string checkOut, string currency, string nationality, string agtCode, string checksum);
        string Get_Exely_BookingRequest(DataTable dtPassengers, string uniqueBookingToken, string hotelcode, string checkIn, string checkOut, string currency, string nationality, string agtCode, string checksum, string roomTypeId, int noOfAdults, int noOfChild);
        string Get_Exely_PriceCheckRQ(int noofadult, int noofchild, string hotelcode, string CheckIn, string CheckOut, string RoomTypeId, string XmlNationality, string checksum = "", DataTable? dtPassengers = null);
        Task<ResponseDto> ExelyResponseNew(string xmlRQRS, string strHost, int searchTimeOut, string password, string searchKey = "");
        string Get_Exely_GetCancellationChargesRQ(string agentRef);
        string Get_Exely_CancelBooking(double amount, string currency, string agentRef);
        ExelyBookRsDto Exely_ProcessBookingResponse(string responseData);
        Task<string> Get_Exely_EInfo(string hotelcode, DataTable dtSuplierMarkup, DataTable dtSearchConfig, string provider, string searchKey, string roomTypeId);
    }
}
