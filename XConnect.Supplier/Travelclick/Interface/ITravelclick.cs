using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XConnect.Models.Hotel;
using XConnect.Models;

namespace XConnect.Supplier.Travelclick.Interface
{
    public interface ITravelclick
    {
        void TravelClickRequestOnly(bool SearchByHotel, DataTable dtPreferredDataHotel, HotelSearchParameter SearchPara, List<SupplierDetails> TravelClickDetails, List<SupplierSearchConfig> TravelClickSearchConfig, ref ConcurrentBag<HotelX> AllTask, DataTable dtAgentDetail, DataSet dsAgentDetails, DataTable dtMarkupDetails, int Hit, DataTable OwnIDSList, DataRow[] commonCode);

        Task<List<Options>> Travelclick_HotelSearchWithRooms(HotelX Item, CommonSearch commonSearch, DataSet SiteminderResponse, HotelSearchParameter SearchParam, DataTable dtAgentDetail, DataTable dtSuplierMarkup, DataTable dtMarkupDetails, string currency, double currencyfactor, DataTable dtROE, Int32 processTimeout, string ResProccessTimerLogActive, string ResProccessTimeValueforSaveLog, string StoreSupplierResErrorLog);

        string Get_TravelClick_BookingRequest(DataSet dsCartDetails, DataTable dtPassengers, DataRow[] TravelClickDetails, string UniqueNo, int i, int jj, string ReferenceNo);

        string GetTravelClick_SupplierResponse(ref string Error, string _url, string Request);
        string Get_TravelClick_BookingCancelRQ(string AgentVoucherNo, string Username, string Password, string Provider, string RoomTypeID, DataTable DtCancellation, DataTable DtPassenger, string ReferenceNo, TokenData tokenData);
    }
}
