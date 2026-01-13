using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XConnect.Models.Hotel;
using XConnect.Models;

namespace XConnect.Supplier.Webbeds.Interface
{
    public interface IWebbeds
    {
        void GetDotwRequestOnly_Async(DataRow[] commonCode, List<StarRating> listRating, HotelSearchParameter SearchPara, string XMLReq, List<SupplierDetails> DOTWDetails, List<SupplierSearchConfig> DOTWSearchConfig, DataTable dtPreferredDataHotel, ref ConcurrentBag<HotelX> AllTask, string xmlCity, string xmlCountry, string XmlNationality, int Hit, DataTable dtROE, TokenData tokenData);

        void GetDotwRequestOnly_Async_new(DataRow[] commonCode, List<StarRating> listRating, HotelSearchParameter SearchPara, string XMLReq, List<SupplierDetails> lstDOTWDetails, List<SupplierSearchConfig> lstDOTWSearchConfig, DataTable dtPreferredDataHotel, ref ConcurrentBag<HotelX> AllTask, int Hit, DataTable dtROE, TokenData tokenData, DataSet dtXMLCity);
        Task<List<Options>> DOTW_HotelSearchWithRooms(HotelX Item, CommonSearch commonSearch, string ResponseData, HotelSearchParameter SearchParam, string XMLCITY, string XMLCOUNTRY, string Currency, double CurecncyFactor, Int32 processTimeout, string ResProccessTimerLogActive, string ResProccessTimeValueforSaveLog, string StoreSupplierResErrorLog, DataTable DtROE);

        //prebook - 240625
        string getRooms_DotwBooking_Preebook(DataSet dsCartDetails, int i, int j);

        string GetDOTWGerRoomZBlockRequestV3(DataSet dsCartDetails, int i, String ClientID, string UserName, string Password, string RoomBlock);

        //book 13/08/25
        string GetDotwRoomBlockFor_SaveBooking(DataTable dtPassengers, DataSet dsCartDetails, int i, int j, string NewAllocationId, string ChangedOccupancy);
        string GetDOtwSaveBookingRequest(DataSet dsCartDetails, int i, String ClientID, string UserName, string Password, string BookingType, string RoomBlockFOrSaveBooking, string AgentCode);

        void DotwRejectAll(ref string Error, int bookingstatus, int i, string DOTWRQ, string DOTWRS, ref DataTable dtAgentVoucher, int total, DataRow[] DOTWDetails, string returnedCode, string UniqueNo, string AgentUniqueNo, DataSet dsCartDetails);

        void DeleteItineraryDOTW(DataRow[] DOTWDetails, string returnedCode, string Type, string Confirm, string UniqueNo, string AgentUniqueNo, string SupplierName, int BookingConfirmationTimeout, ref string Error);
        string GetDotwBookItineraryRequest(DataRow[] DOTWDetails, string Confirm, string bookingType, string bookingCode, string TestPriceAndAllocation);
        Task<string> GetBookingSupplierResponseNew(string Error, string strUniqueId, string strxmlRQRS, string strHost, Int32 TimeOut, string SearchKey = "");
        string GetBookingSupplierResponse(string Error, string strUniqueId, string strxmlRQRS, string strHost, Int32 TimeOut, string SearchKey = "");

        //Cancel Booking
        string GetDotwCancelRequest(string AgentVoucherNo, DataRow[] DOTWDetails, string CancellationAmount, bool IsFinal);
    }
}
