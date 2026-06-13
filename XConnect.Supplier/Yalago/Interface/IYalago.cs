using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XConnect.Models.Hotel;
using XConnect.Models;

namespace XConnect.Supplier.Yalago.Interface
{
    public interface IYalago
    {
        void GetYalagoRequestOnly_Async(DataRow[] commonCode, List<StarRating> listRating, HotelSearchParameter SearchPara, List<SupplierDetails> YalagoDetails, List<SupplierSearchConfig> YalagoSearchConfig, DataTable dtPreferredDataHotel, ref ConcurrentBag<HotelX> AllTask, int Hit,TokenData tokenData, DataSet dtXmlCity, DataTable dtROE);

        Task<List<Options>> Yalago_HotelSearchWithRooms(HotelX Item, CommonSearch commonSearch, string ResponseData, HotelSearchParameter SearchParam, string XmlCity, string XmlCountry, string XmlNationality, string Currency, double CurrencyFactor, DataTable dtROE, string ProccessCutOffTime, Int32 processTimeout, string ResProccessTimerLogActive, string ResProccessTimeValueforSaveLog, string StoreSupplierResErrorLog);

        //recheck added on 30-05-25

        Task<CancellationDto> Get_Yalago_CancellationPolicy(int roomno, string hotelcode, DataSet dsHotel, string ownsystemid, string roomtypeid, DataTable dtAgentDetail, DataTable dtSuplierMarkup, DataTable dtMarkupDetails, DataTable dtSearchConfig, HotelSearchParameter SearchPara, string OptionCode, int AgentId, double roomprice, double Companybuy,string Searchkey,string Supplier, string GiataId = "0", int hotelRating = 0);

        //prebook 02-07-25
        Task<PriceCheckCMDto> Get_Yalago_PriceCheckRQRS(DataSet dsCartDetails, DataRow[] YalagoDetails, int i, int j, DataTable dtCheckPriceReqStructure, string UniqueNo, string AgentUniqueNo, bool isForBooking, double CompanyBuyingPriceA, string xmlhotelid, string IsFor, bool IssErrorY, double itempriceY, string Searchkey, int AgentId = 0,TokenData tokenData= null, string TechName = "");

        string Get_Yalago_BookingRequest(DataSet dsCartDetails, DataTable dtPassengers, string UniqueNo, int i, int j, string hotelcode, string CheckIn, string CheckOut, string Currency, string Nationality, string agtCode);

        Task<ResponseDto> YalagoResponseNew(string xmlRQRS, string strHost, int SearchTimeOut, string Password, string SearchKey = "");

        string Get_Yalago_GetCancellationChargesRQ(string AgentRef);

        string Get_Yalago_CancelBooking(double Amount, string currency, string AgentRef);
    }
}
