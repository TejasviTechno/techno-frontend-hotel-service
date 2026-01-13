using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XConnect.Models.Hotel;
using XConnect.Models;

namespace XConnect.Supplier.RateGain.Interface
{
    public interface IRateGain
    {
        void GetRategainRequestOnly(bool SearchByHotel, DataTable dtPreferredDataHotel, HotelSearchParameter SearchPara, List<SupplierDetails> RateGainDetails, List<SupplierSearchConfig> RateGainSearchConfig, ref ConcurrentBag<HotelX> AllTask, DataTable dtAgentDetail, DataSet dsAgentDetails, DataTable dtMarkupDetails, int Hit, DataTable OwnIDSList, DataRow[] commonCode);

        Task<List<Options>> XMLHotelRategainPriceOnly_Async(HotelX Item, CommonSearch commonSearch, DataSet SiteminderResponse, HotelSearchParameter SearchParam, DataTable dtAgentDetail, DataTable dtSuplierMarkup, DataTable dtMarkupDetails, string currency, double currencyfactor, DataTable dtROE, Int32 processTimeout, string ResProccessTimerLogActive, string ResProccessTimeValueforSaveLog, string StoreSupplierResErrorLog);
        string Get_Rategain_BookingCancelRQ(string AgentVoucherNo, string Username, string Password, string Provider, string RoomTypeID, string RoomTypeName, DataTable DtCancellation, DataTable DtPassenger, TokenData tokenData);

        string Get_RateGain_BookingRequest(DataSet dsCartDetails, DataTable dtPassengers, DataRow[] RateGainDetails, string UniqueNo, int i, int j, string ReferenceNo, string BookUrl, TokenData tokenData);

        Task<ResponseDto> GetRateGain_SupplierResponse(string request, string TravcoURL, Int32 TimeOut, string Provider = "", HotelSearchParameter SearchParam = null, bool DebugMode = false);
    }
}
