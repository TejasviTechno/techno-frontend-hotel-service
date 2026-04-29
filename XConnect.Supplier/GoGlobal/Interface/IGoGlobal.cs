using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XConnect.Models.Hotel;
using XConnect.Models;

namespace XConnect.Supplier.GoGlobal.Interface
{
    public interface IGoGlobal
    {
        void GetGoGlobalRequestOnly_Async(DataRow[] commonCode, List<StarRating> listRating, HotelSearchParameter SearchPara, List<SupplierDetails> GGDetails, List<SupplierSearchConfig> GGSearchConfig, DataTable dtPreferredDataHotel, ref ConcurrentBag<HotelX> AllTask, string citycode, int Hit, string Nationality, string Supplier, DataTable dtROE, bool SearchByOneID,TokenData tokenData);
        void GetGoGlobalRequestOnly_Async_new(DataRow[] commonCode, List<StarRating> listRating, HotelSearchParameter SearchPara, List<SupplierDetails> GGDetails, List<SupplierSearchConfig> GGSearchConfig, DataTable dtPreferredDataHotel, ref ConcurrentBag<HotelX> AllTask, int Hit, DataTable dtROE, bool SearchByOneID,TokenData tokenData,DataSet dtXMLCity);

        Task<List<Options>> GoGlobal_HotelSearchWithRooms(HotelX Item, CommonSearch commonSearch, string WebSiteURL, int SearchTimeOut, string Supplier, bool DebugeMode, string UserName, string PassWord, string MethoodType, string ResponseData, HotelSearchParameter SearchParam, string xmlcity, string xmlcountry, string xmlnationality, string currency, double currencyfactor, Int32 processTimeout, string ResProccessTimerLogActive, string ResProccessTimeValueforSaveLog);

        //recheck
        Task<CancellationDto> GoGlobal_cancellation(TokenData TokenData,string Mealid, string roomtypecode, DataSet dsHotel, string hotelcode, string ownsystemid, string roomtypeid, DataTable dtAgentDetail, DataTable dtSuplierMarkup, DataTable dtSearchConfig, HotelSearchParameter SearchPara, string roomno, string provider, int AgentId, DataTable dtMarkupDetails, double roomprice, string GiataId = "0", int hotelRating = 0, string SearchKey = "");

        //prebook - 02-07-25
        Task<PriceCheckCMDto> PriceCheckGoGlobal(int i, DataTable dtCheckPriceReqStructure, DataSet dsCartDetails, DataRow[] GGDetails, string UniqueNo, string AgentUniqueNo, string IsFor,  bool IsError, string supplier, double CompanyBuyPrice, double itemprice, bool PolicyChange, int AgentId = 0, string SearchKey = "");

        string GoGlobalBooking(int i, DataTable dtPassengers, DataSet dsCartDetails, string UserName, string Password, string UserId, string UniqueNo, string AgentCode, string SpecialReqToPass);
        string GoGlobalBooking_newversion(int i, DataTable dtPassengers, BookRequest_newversion bookRequest, string UserName, string Password, string UserId, string UniqueNo, string AgentCode, string SpecialReqToPass);

        string VoucherBooking(string UserName, string Password, string UserId, string BookingCode);

        string GOGlobalBookingStatusRequest(string Booking_Ref, string UserName, string Password, string UserId);

        int GoGlobalBookingStatus(string BStatus);

        string GOGlobalCancellationRequest(string Booking_Ref, string UserName, string Password, string UserId);
    }
}
