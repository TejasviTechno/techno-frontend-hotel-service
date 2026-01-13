using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XConnect.Models.Hotel;
using XConnect.Models;

namespace XConnect.Supplier.Staah.Interface
{
    public interface IStaah
    {
        void GetStaahRequestOnly_async(List<SupplierDetails> StaahDetails, DataRow[] commonCode, List<StarRating> listRating, DataTable dtPreferredDataHotel, HotelSearchParameter SearchPara, List<SupplierSearchConfig> StaahSearchConfig, ref ConcurrentBag<HotelX> AllTask, DataTable dtAgentDetail, DataSet dsAgentDetails, DataTable dtMarkupDetails, decimal agentPercent, DataTable OwnIDSList, int Hit);

        Task<List<Options>> Staah_HotelSearchWithRooms(HotelX Item, CommonSearch commonSearch, DataSet SiteminderResponse, HotelSearchParameter SearchParam, DataTable dtAgentDetail, DataTable dtSuplierMarkup, DataTable dtMarkupDetails, string currency, double currencyfactor, DataTable dtROE, Int32 processTimeout, string ResProccessTimerLogActive, string ResProccessTimeValueforSaveLog, string StoreSupplierResErrorLog);

        //Prebook 23-07-25
        Task<PriceCheckDto> StaahPreBookingReq(string supplier, DataSet StaahPreeBookingResponse, string ReferenceNo, string ID, string Password, string HotelCode, string RCode, int NoOfAdults, int NoOfChild, string CheckInDate, string CheckOutDate, double Rate, double FinalPrice, int UserCartId, int UserCartDetailId, DataTable dtCheckPriceReqStructure, int j, string UniqueNo, string AgentUniqueNo, bool isForBooking, int ChildAge1, int ChildAge2, int NoOfRoom, string RateId, int ExtrabedQuantity, string SupplierCurrency, double CurrencyFactorStaah, double CommissionRate, double Offer,int AgentId);

        string Get_Staah_BookingRequest(DataSet dsCartDetails, DataTable dtPassengers, DataRow[] DerbySoftDetails, string UniqueNo, bool IsPriceUpdate, int i, int j, string PreBookingToken, string UserID, string Password, string ExternalReference, string Bookingtokan);

        Task<ResponseDto> GetStaahResponse(string Error, string request, string TravcoURL, Int32 TimeOut, string Provider = "", HotelSearchParameter para = null, bool DebugMode = false);

        string Get_Staah_BookingCancelRQ(string AgentVoucherNo, string Username, string Password, string version, string Provider, string RoomTypeID, DataTable DtCancellation, DataTable DtPassenger, string ReferenceNo, DataSet dsratebreakup, TokenData tokenData);
    }
}
