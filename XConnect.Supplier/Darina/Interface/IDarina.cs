using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XConnect.Models.Hotel;
using XConnect.Models;

namespace XConnect.Supplier.Darina.Interface
{
    public interface IDarina
    {
        void GetDarinaRequestOnly_async(DataRow[] commonCode, List<StarRating> listRating, DataTable dtPreferredDataHotel, List<SupplierSearchConfig> lstDarinaSearchConfig, List<SupplierDetails> lstDarinaDetails, HotelSearchParameter SearchPara, bool IsHotelWiseSearch, ref ConcurrentBag<HotelX> AllTask, string AgentCode, int Hit, TokenData tokenData, DataTable dtROE, DataSet dtXMLCity);
        Task<List<Options>> Darina_HotelSearchWithRooms(HotelX Item, CommonSearch commonSearch, string ResponseData, HotelSearchParameter SearchParam, string currency, double currencyfactor, string xmlcity, string xmlcountry, string xmlnationality, string ProccessCutOffTime, Int32 processTimeout, string ResProccessTimerLogActive, string ResProccessTimeValueforSaveLog);

        //recheck - 30-05-25
        Task<CancellationDto> Darina_Prebook(DataRow[] DarinaDetails, double AgentCompanyPercentage, string IsB2BOrB2C, int CityID, string CountryId, int NoofNights, string mealid, string Currency, double Factor, string checkin, string checkout, int ChildAge1, int ChildAge2, int NoofChild, int NoOfAdult, DataSet dsHotel, string hotelcode, string ownsystemid, string roomtypeid, DataTable dtAgentDetail, DataTable dtSuplierMarkup, string roomno, string provider, int AgentId, DataTable dtMarkupDetails, double CompanyBuy, string SearchKey, string GiataId = "0", int hotelRating = 0);

        //prebook - 240625
        Task<string> Darina_Prebook_WithoutPolicy(DataRow[] DarinaDetails, string checkin, string checkout, int ChildAge1, int ChildAge2, int NoofChild, int NoOfAdult, string hotelcode, string roomtypeid, string provider, int AgentId, double CompanyBuy);

        //book 14/8/25
        string GetDarinaBookingRequest(DataSet dsCartDetails, DataTable dtPassengers, int i, int j, String ClientID, string UserName, string Password, string SecurityKey, string AgentCode, string SearchKey);

        Task<ResponseDto> GetSupplierResponse_darina(string strUniqueId, string strxmlRQRS, string strHost, Int32 TimeOut, string Provider = "", HotelSearchParameter para = null, bool DebugMode = false, string Searchkey = "");

        string GetDarinaSecurityData(DataRow[] DarinaDetails);
    }
}
