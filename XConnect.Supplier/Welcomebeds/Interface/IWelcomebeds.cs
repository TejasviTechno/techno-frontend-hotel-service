using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XConnect.Models;
using XConnect.Models.Hotel;

namespace XConnect.Supplier.Welcomebeds.Interface
{
    public interface IWelcomebeds
    {
        void GetwelcomebedsRequestOnly_Async(DataRow[] commonCode, List<StarRating> listRating, HotelSearchParameter SearchPara, List<SupplierDetails> WebHotellierDetails, List<SupplierSearchConfig> WebHotellierSearchConfig, DataTable dtPreferredDataHotel, ref ConcurrentBag<HotelX> AllTask, int Hit, DataTable dtROE, DataSet dtXMLCity, TokenData tokenData);
        Task<List<Options>> Welcomebeds_HotelSearchWithRooms(HotelX item, CommonSearch commonSearch, string ResponseData, string WSurl, string Username, string Password, Int32 SearchTimeOut, HotelSearchParameter SearchParam, bool DebugMode, string Currency, double CurrencyFactor, DataSet dsAgentDetails, string Provider, DataTable dtCountryCityMarkup, Int32 AgentId, Int32 TariffID, double AgentPercentage, bool IsShowSupplier, int SupplierID, int MasterSupplierID, Int32 processTimeout, string ProccessCutOffTime, string ResProccessTimerLogActive, string ResProccessTimeValueforSaveLog, string StoreSupplierResErrorLog, DataTable dtROE, DataTable dtSuplierMarkup, string xmlhotelcode);

        Task<PriceCheckWelcomebedsDto> welcomebedsPriceCheck(DataSet dsCartDetails, DataRow[] welcombedsDetails, int j, double FinalPrice, double CurrencyFactorwelcombeds, string UniqueNo, string AgentUniqueNo, string SupplierName, int i, double CompanyBuyingPrice, DataTable dtRoE, int AgentId, TokenData TokenData, string Searchkey = "", string TechName = "");
        string welcomebedsBookingRequest(DataSet dsCartDetails, DataRow[] WelcomebedsDetails, int i, int j, double resPrice, DataTable dtPassengers, ref string AgtRef, string CheckIn, string CheckOut, int UniqueNo, string BookToken, string AgentCode, string SearchKey = "");

        string welcomebedsBookingRequest_new(BookRequest_newversion bookRequest, DataRow[] WelcomebedsDetails, int i, int j, double resPrice, DataTable dtPassengers, ref string AgtRef, string CheckIn, string CheckOut, int UniqueNo, string BookToken, string AgentCode, string SearchKey = "");
        string WelcomebedsBookingResponse(ref string Error, string Request, string url, string UserName, string Password, string Searchkey = "", Int32 TimeOut = 120000);
        string Getwelcombeds_BookingCancelRQ(string AgentVoucherNo, DataRow[] WelcomebedsDetails, string requestReferenceID, string Searchkey = "");
        string GetwelcomebedsResponse(ref string Error, string Request, string url, string UserName, string Password, int Timeout = 120000);
        Task<string> Welcomebeds_EssentialInfoOnly(string Url1, string password, string username, string roomtypeid, string CheckIn, string CheckOut, string SearchKey = "");

    }
}
