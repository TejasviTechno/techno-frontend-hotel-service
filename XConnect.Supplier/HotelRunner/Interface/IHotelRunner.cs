using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XConnect.Models.Hotel;
using XConnect.Models;

namespace XConnect.Supplier.HotelRunner.Interface
{
    public interface IHotelRunner
    {
        void GetHotelRunnerRequestOnlyNew(bool SearchByHotel, DataTable dtPreferredDataHotel, HotelSearchParameter SearchPara, List<SupplierDetails> HotelRunnerDetails, List<SupplierSearchConfig> HotelRunnerSearchConfig, ref ConcurrentBag<HotelX> AllTask, DataTable dtAgentDetail, DataSet dsAgentDetails, DataTable dtMarkupDetails, int Hit, DataTable OwnIDSList, DataRow[] commonCode);

        Task<List<Options>> HotelRunner_HotelSearchWithRooms(HotelX Item, CommonSearch commonSearch, DataSet SiteminderResponse, HotelSearchParameter SearchParam, DataTable dtAgentDetail, DataTable dtSuplierMarkup, DataTable dtMarkupDetails, string currency, double currencyfactor, DataTable dtROE, Int32 processTimeout, string ResProccessTimerLogActive, string ResProccessTimeValueforSaveLog);

        string Get_HotelRunner_BookingRequest(DataSet dsCartDetails, DataTable dtPassengers, DataRow[] SiteMinderDetails, string UniqueNo, int i, int jj, string ReferenceNo);

        Task<ResponseDto> GetHotelRunner_SupplierResponse(string request, string TravcoURL, Int32 TimeOut, string Provider = "", HotelSearchParameter SearchParam = null, bool DebugMode = false);

        string Get_HotelRunner_BookingCancelRQ(string AgentVoucherNo, string Username, string Password, string Provider, string RoomTypeID, DataTable DtCancellation, DataTable DtPassenger);
    }
}
