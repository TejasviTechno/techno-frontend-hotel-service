using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XConnect.Models.Hotel;
using XConnect.Models;

namespace XConnect.Supplier.Nuitee.Interface
{
    public interface INuitee
    {
        void GetNuiteeRequestOnly_async(DataRow[] commonCode, List<StarRating> listRating, HotelSearchParameter SearchPara, List<SupplierDetails> lstNuiteeDetails, List<SupplierSearchConfig> lstNuiteeSearchConfig, DataTable dtPreferredDataHotel, ref ConcurrentBag<HotelX> AllTask, int Hit, DataTable dtROE, TokenData tokenData, DataSet dtXMLCity);
        Task<List<Options>> Nuitee_HotelSearchWithRooms(HotelX Item, CommonSearch commonSearch, string ResponseData, HotelSearchParameter SearchPara, string XmlCity, string XmlCountry, string XmlNationality, string Currency, double CurrencyFactor, string Action, DataSet dsAgentDetails, string SupplierName, DataTable dtCountryCityMarkup, Int32 processTimeout, string ResProccessTimerLogActive, string ResProccessTimeValueforSaveLog);

        //recheck - 300525
        Task<CancellationDto> Get_Nuitee_CancellationPolicy(double roomprice, string rating, string ContractID, string Type, DataSet dsHotel, string hotelcode, string citycode, string ownsystemid, string RoomTypeId_Own, string MealID, string Board_Text, DataTable dtAgentDetail, DataTable dtSuplierMarkup, DataTable dtSearchConfig, HotelSearchParameter objSearPera, string roomno, string provider, int AgentId, DataTable dtMarkupDetails, string SearchKey, string GiataId = "0", int hotelRating = 0,string SiteCurrency = "USD");

        //prebook - 070725
        Task<PriceCheckNuiteeDto> NuiteePriceCheckRQRS(DataSet dsCartDetails, DataRow[] nuiteeDetails, string UserId, string Password, int i, int j, DataTable dtCheckPriceReqStructure, string UniqueNo, string AgentUniqueNo, double FinalPrice, bool isForBooking, double CompanyBuyingPrice, string PreBookingToken, string xmlhotelid, string Ipaddress, TokenData tokenData, string Searchkey = "", int AgentId = 0, string TechName = "");

        string Get_NuiteeBookingRequest(string Url, DataSet dsCartDetails, DataTable dtPassengers, DataRow[] nuiteeDetails, string UniqueNo, bool IsPriceUpdate, int i, int j, string PreBookingToken, string RoomTypeId, string RoomName, double Roomprice, string Currency, int NoOfRoom, string email, string EmailID, string Firstname, string LastName, string phoneno, string Paymenttype, string referenceNumber);

        Task<ResponseDto> GetSupplierResponse(string strUniqueId, string strxmlRQRS, string strHost, Int32 TimeOut, string Provider = "", HotelSearchParameter para = null, bool DebugMode = false, string Searchkey = "");

        string Get_NuiteeBookingDetail(int Booking, DataRow[] nuiteeDetails);

        string Get_NuiteeBookingCancelRQ(string AgentVoucherNo, string RoomtypeId, DataRow[] nuiteeDetails, string cancelreason, string Ipaddress);
    }
}
