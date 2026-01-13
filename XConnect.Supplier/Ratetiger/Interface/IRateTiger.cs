using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XConnect.Models.Hotel;
using XConnect.Models;

namespace XConnect.Supplier.Ratetiger.Interface
{
    public interface IRateTiger
    {
        void GetRateTigerRequestOnly(bool SearchByHotel, DataTable dtPreferredDataHotel, HotelSearchParameter SearchPara, List<SupplierDetails> RateTigerDetails, List<SupplierSearchConfig> RateTigerSearchConfig, ref ConcurrentBag<HotelX> AllTask, DataTable dtAgentDetail, DataSet dsAgentDetails, DataTable dtMarkupDetails, int Hit, DataTable OwnIDSList, DataRow[] commonCode);
        Task<List<Options>> RateTiger_HotelSearchWithRooms(HotelX Item, CommonSearch commonSearch, DataSet SiteminderResponse, HotelSearchParameter SearchParam, DataTable dtAgentDetail, DataTable dtSuplierMarkup, DataTable dtMarkupDetails, string currency, double currencyfactor, DataTable dtROE, Int32 processTimeout, string ResProccessTimerLogActive, string ResProccessTimeValueforSaveLog);

        string Get_RateTRiger_BookingRequest(DataSet dsCartDetails, DataTable dtPassengers, DataRow[] RateTIgerDetails, string UniqueNo, int i, int jj, ref string ReferenceNo);
        string GetRateTiger_Response(ref string Error, string url, string Request);

        string Get_RateTiger_BookingCancelRQ(string AgentVoucherNo, string Username, string Password, string Provider, string RoomTypeID, DataTable DtCancellation, DataTable DtPassenger, TokenData tokenData);

    }
}
