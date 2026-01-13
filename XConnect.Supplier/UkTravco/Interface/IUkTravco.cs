using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XConnect.Models.Hotel;
using XConnect.Models;

namespace XConnect.Supplier.UkTravco.Interface
{
    public interface IUkTravco
    {
        void GetUkTravcoRequestOnly_Async(DataRow[] commonCode, List<StarRating> listRating, DataTable dtPreferredDataHotel, HotelSearchParameter SearchPara, List<SupplierSearchConfig> lstUk_travcoSearchConfig, List<SupplierDetails> lstUk_travcoDetails, ref ConcurrentBag<HotelX> AllTask, string AgentCode, int Hit, TokenData tokenData, DataSet dtXMLCity);

        Task<List<Options>> UkTravco_HotelSearchWithRooms(HotelX Item, CommonSearch commonSearch, string ResponseData, HotelSearchParameter SearchParam, string XMLCITY, string XMLCountry, string Currency, double CurrencyFactoruk_travco, Int32 processTimeout, string ResProccessTimerLogActive, string ResProccessTimeValueforSaveLog, string XmlNationality = "");

        //prebook  - 24-06-25
        Task<PriceCheckDto> AllocationEnqiry(string AgentCode, string AgentPassword, string HotelCode, string RCode, int NoOfAdults, int NoOfChild, string CheckInDate, string CheckOutDate, string uk_travco_URL, double Rate,double FinalPrice, int UserCartId, int UserCartDetailId,DataTable dtCheckPriceReqStructure, int j, string UniqueNo, string AgentUniqueNo, bool isForBooking, int ChildAge1, int ChildAge2, string Searchkey = "", int AgentId = 0,string Supplier="");

        string Getuk_travcoBookingRequest_combineBook(DataSet dsCartDetails, DataTable dtPassengers, int i, int j, string ClientID, string UserName, string Password, string UniqueNo,string searchKey);

        Task<ResponseDto> Getuk_travco_Response_report(string _url, string _action, string Request, Int32 Timeout, string Provider = "ukTravco", HotelSearchParameter para = null, string Searchkey = "");

        string BookingDetailsRQ(string Ref, DataRow[] uk_travcoDetails);

        string Getuk_travcoCancelationRQ(string Ref, DataRow[] uk_travcoDetails, int NoOfAdults, int NoOfChildren, int Duration, string CheckInDate, string HotelCode, string RoomCode, string PriceCode, string PassengerName, string AgentReference, int Infants);
    }
}
