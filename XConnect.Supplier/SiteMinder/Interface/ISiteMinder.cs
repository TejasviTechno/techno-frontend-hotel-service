using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XConnect.Models.Hotel;
using XConnect.Models;

namespace XConnect.Supplier.SiteMinder.Interface
{
    public interface ISiteMinder
    {
        void GetSiteMinderRequestOnlyNew(bool SearchByHotel, DataTable dtPreferredDataHotel, HotelSearchParameter SearchPara, List<SupplierSearchConfig> SiteMinderSearchConfig, ref ConcurrentBag<HotelX> AllTask, DataTable dtAgentDetail, DataSet dsAgentDetails, DataTable dtMarkupDetails, int Hit, DataTable OwnIDSList, DataRow[] commonCode);

        Task<List<Options>> Siteminder_HotelSearchWithRooms(HotelX Item, CommonSearch commonSearch, DataSet SiteminderResponse, HotelSearchParameter SearchParam, DataTable dtAgentDetail, DataTable dtSuplierMarkup, DataTable dtMarkupDetails, string currency, double currencyfactor, DataTable dtROE, Int32 processTimeout, string ResProccessTimerLogActive, string ResProccessTimeValueforSaveLog, string StoreSupplierResErrorLog);

        string Get_SiteMinder_BookURL(string CountryId, string HotelCode = "");

        string Get_SiteMinder_BookingRequest(DataSet dsCartDetails, DataTable dtPassengers, DataRow[] SiteMinderDetails, string UniqueNo, int i, int jj, string ReferenceNo);

        string GetSiteminder_SupplierResponse(ref string Error, string _url, string Request);

        string Get_SiteMinder_BookingCancelRQ(string AgentVoucherNo, string Username, string Password, string Provider, string RoomTypeID, DataTable DtCancellation, DataTable DtPassenger, TokenData tokenData);
    }
}
