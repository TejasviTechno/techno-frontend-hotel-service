using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XConnect.Models.Hotel;
using XConnect.Models;

namespace XConnect.Supplier.Jumeirah.Interface
{
    public interface IJumeirah
    {
        void GetJumeirahRequestOnly_async(DataRow[] commonCode, List<StarRating> listRating, HotelSearchParameter SearchPara, List<SupplierDetails> lstJumeirahDetails, List<SupplierSearchConfig> lstJumeirahSearchConfig, DataTable dtPreferredDataHotel, ref ConcurrentBag<HotelX> AllTask, int Hit, DataTable dtROE,TokenData tokenData);

        Task<List<Options>> JumeirahPriceOnly_Async(HotelX Item, CommonSearch commonSearch, double AgentPercentage, int SearchTimeOut, string Supplier, DataTable dtCountryCityMarkup, int AgentId, DataSet dsAgentDetails, int SupplierID, string ResponseData, HotelSearchParameter SearchParam, string xmlcity, string xmlcountry, string xmlnationality, string JumeirahCurrency, double CurrencyFactorJumeirah, Boolean IsShowSupplier, string HotelID, DataTable dtROE, DataTable dtSuplierMarkup, Int32 processTimeout, string ResProccessTimerLogActive, string ResProccessTimeValueforSaveLog, string PassWord);

        //prebook 23-07-25
        Task<PriceCheckCMDto> GetJumeirahPriceCheck(DataSet dsCartDetails, DataRow[] JumeirahDetails, int j, DataTable dtCheckPriceReqStructure, string UniqueNo, string AgentUniqueNo,  double MainPrice, string HotelId, int UserCartId, int UserCartDetailId, string RoomTypeId, string SupplierName, double CompanyBuyingPrice, int NoOfAdults, int NoOfChild, string CheckIn, string CheckOut, TokenData tokenData, int AgentId = 0);
    }
}
