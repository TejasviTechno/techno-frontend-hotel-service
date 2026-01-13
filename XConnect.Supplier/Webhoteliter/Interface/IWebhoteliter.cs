using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XConnect.Models.Hotel;
using XConnect.Models;

namespace XConnect.Supplier.Webhoteliter.Interface
{
    public interface IWebhoteliter
    {
        void WebHotellierRequestOnly_async(DataRow[] commonCode, List<StarRating> listRating, HotelSearchParameter SearchPara, List<SupplierDetails> WebHotellierDetails, List<SupplierSearchConfig> WebHotellierSearchConfig, DataTable dtPreferredDataHotel, ref ConcurrentBag<HotelX> AllTask, string XMLCity, string XMLCountry, string XMLNationality, int Hit, DataTable dtROE, TokenData tokenData);
        void WebHotellierRequestOnly_async_new(DataRow[] commonCode, List<StarRating> listRating, HotelSearchParameter SearchPara, List<SupplierDetails> WebHotellierDetails, List<SupplierSearchConfig> WebHotellierSearchConfig, DataTable dtPreferredDataHotel, ref ConcurrentBag<HotelX> AllTask, int Hit, DataTable dtROE,TokenData tokenData, DataSet dtXMLCity);

        Task<List<Options>> WebHotellier_HotelSearchWithRooms(HotelX item, CommonSearch commonSearch, string ResponseData, HotelSearchParameter SearchParam, string XMLCITY, string XMLCountry, string currency, string supplierURL, string Useraname, string password, string xmlhotelcode, double CurrencyFactor, string Provider, DataSet dsAgentDetail, DataTable dtCountryCityMarkup, Int32 AgentId, Int32 TariffID, double AgentPercentage, Boolean IsShowSupplier, int SupplierID, int MasterSupplierID, DataTable dtROE, string XmlNationality = "");

        #region Recheck - 28-05-25
        Task<string> Webhotelier_EssentialInfoOnly(string supplierURL, string xmlhotelcode, string Useraname, string password, string SearchKey);
        #endregion

        string WebHotelierBookingRQ(DataSet dsCartDetails, DataRow[] WelcomebedsDetails, int i, int j, double resPrice, DataTable dtPassengers, ref string AgtRef, string CheckIn, string CheckOut, int UniqueNo, string BookToken, string searchkey, bool IsPruvo, string AgentCod = "");

        Task<ResponseDto> WebhotelierBookingRS(string Request, string url, string UserName, string Password, Int32 TimeOut = 120000);
    }
}
