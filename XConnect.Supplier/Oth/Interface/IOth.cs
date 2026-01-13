using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XConnect.Models.Hotel;
using XConnect.Models;

namespace XConnect.Supplier.Oth.Interface
{
    public interface IOth
    {
        void GetWithinearthRequestOnlyNew(bool SearchByHotel, DataTable dtPreferredDataHotel, HotelSearchParameter SearchPara, List<SupplierDetails> OTHDetails, List<SupplierSearchConfig> OTHSearchConfig, ref ConcurrentBag<HotelX> AllTask, DataTable dtAgentDetail, DataSet dsAgentDetails, DataTable dtMarkupDetails, int Hit, DataTable OwnIDSList, DataRow[] commonCode);
        Task<List<Options>> Oth_HotelSearchWithRooms(HotelX item, AdvancedOptions advancedOptions, TokenData tokenData, DataSet GTAResponse, DataTable dtAgentDetail, DataTable dtSuplierMarkup, HotelSearchParameter SearchParam, DataTable dtMarkupDetails, int agent, CommonSearch commonSearch, string Response, string ProccessCutOffTime, Int32 processTimeout, string ResProccessTimerLogActive, string ResProccessTimeValueforSaveLog);
    }
}
