using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XConnect.Models.Hotel;
using XConnect.Models;

namespace XConnect.Supplier.Dedge.Interface
{
    public interface IDedge
    {
        void GetDedgeRequestOnly(bool SearchByHotel, DataTable dtPreferredDataHotel, HotelSearchParameter SearchPara, List<SupplierDetails> DedgeDetails, List<SupplierSearchConfig> DedgeSearchConfig, ref ConcurrentBag<HotelX> AllTask, DataTable dtAgentDetail, DataSet dsAgentDetails, DataTable dtMarkupDetails, int Hit, DataTable OwnIDSList, DataRow[] commonCode);

        Task<List<Options>> XMLHotelDedgePriceOnly_Async(HotelX Item, CommonSearch commonSearch, DataSet SiteminderResponse, HotelSearchParameter SearchParam, DataTable dtAgentDetail, DataTable dtSuplierMarkup, DataTable dtMarkupDetails, string currency, double currencyfactor, DataTable dtROE, Int32 processTimeout, string ResProccessTimerLogActive, string ResProccessTimeValueforSaveLog);
        string Get_Dedge_BookingRequest(DataSet dsCartDetails, DataTable dtPassengers, DataRow[] DedgeDetails, string UniqueNo, int i, int jj, string ReferenceNo);

        Task<ResponseDto> GetDedge_SupplierResponse(string Username, string password, string request, string TravcoURL, Int32 TimeOut, string Provider = "", HotelSearchParameter para = null, bool DebugMode = false);

        string Get_Dedge_BookingCancelRequest(string AgentVoucherNo, string Username, string Password, string Provider, string RoomTypeID, DataTable DtCancellation, DataTable DtPassenger, DataSet dsratebreakup);
    }
}
