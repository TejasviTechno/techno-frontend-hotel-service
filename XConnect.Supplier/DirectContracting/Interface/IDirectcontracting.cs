using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XConnect.Models.Hotel;
using XConnect.Models;
using XConnect.Supplier.DirectContracting;
using System.Collections.Concurrent;
using System.Data;


namespace XConnect.Supplier.DirectContracting.Interface
{
    public interface IDirectcontracting
    {
        void GetDirectcontractingRequestOnly_Async(DataRow[] commonCode, List<StarRating> listRating, DataTable dtPreferredDataHotel, HotelSearchParameter SearchPara, List<SupplierSearchConfig> lstLetsFlySearchConfig, List<SupplierDetails> lstLetsFlyDetails, ref ConcurrentBag<HotelX> AllTask, string AgentCode, int Hit, TokenData tokenData, DataSet dtXMLCity,string Platform);
        Task<List<Options>> Directcontracting_HotelSearchWithRooms(HotelX item, CommonSearch commonSearch, string ResponseData, string WSurl, string Username, string Password, Int32 SearchTimeOut, HotelSearchParameter SearchParam, bool DebugMode, string Currency, double CurrencyFactor, DataSet dsAgentDetails, string Provider, DataTable dtCountryCityMarkup, Int32 AgentId, Int32 TariffID, double AgentPercentage, Boolean IsShowSupplier, int SupplierID, int MasterSupplierID, Int32 processTimeout, string ProccessCutOffTime, string ResProccessTimerLogActive, string ResProccessTimeValueforSaveLog, string StoreSupplierResErrorLog, DataTable dtROE, DataTable dtSuplierMarkup);
        Task<PriceCheckCMDto> GetDCEPriceCheckREQRES(DataSet dsCartDetails, DataRow[] DCE_Details, int j, DataTable dtCheckPriceReqStructure, string UniqueNo, string AgentUniqueNo, double MainPrice, int UserCartId, double CompanyBuyingPrice, int AgentId, TokenData tokenData);

    }
}
