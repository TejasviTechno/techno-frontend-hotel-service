using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XConnect.Models;
using XConnect.Models.Hotel;

namespace XConnect.Supplier.Iboosy.Interface
{
    public interface IiBoosy
    {
        //void GetMethaBookRequestOnly_async(DataRow[] commonCode, List<StarRating> listRating, DataTable dtROE, HotelSearchParameter SearchPara, List<SupplierDetails> MethaBookDetails, List<SupplierSearchConfig> MethaBookSearchConfig, DataTable dtPreferredDataHotel, ref ConcurrentBag<HotelX> AllTask, string XMLCity, string XmlNationality, int Hit, TokenData tokenData,string Provider);
        void GetMethaBookRequestOnly_async(CommonSearchList commonSearchList, List<StarRating> listRating, DataTable dtROE, HotelSearchParameter SearchPara, List<SupplierDetails> MethaBookDetails, List<SupplierSearchConfig> MethaBookSearchConfig, DataTable dtPreferredDataHotel, ref ConcurrentBag<HotelX> AllTask, int Hit, TokenData tokenData,string Provider,DataSet dtXMLCity);
        void GetMethaBookRequestOnly_async(DataRow[] commonCode, List<StarRating> listRating, DataTable dtROE, HotelSearchParameter SearchPara, List<SupplierDetails> MethaBookDetails, List<SupplierSearchConfig> MethaBookSearchConfig, DataTable dtPreferredDataHotel, ref ConcurrentBag<HotelX> AllTask, int Hit, TokenData tokenData, string Provider, DataSet dtXMLCity);

        Task<List<Options>> Methabook_HotelSearchWithRooms(HotelX item, CommonSearch commonSearch, DataTable dtROE,string currencyCode, string response, string xmlCity, HotelSearchParameter? searchPara, double currencyFactor, string? proccessCutOffTime, int processTimeout, string? resProccessTimerLogActive, string? resProccessTimeValueforSaveLog, string Suppliername,string BookingStatus,string RS_NOCOUNT, TokenData tokenData, string RS_Success);

        //recheck  - 300525
        Task<CancellationDto> MethaBook_Cancellation(string MealCode, DataSet dsHotel, string hotelcode, string ownsystemid, string roomtypeid, DataTable dtAgentDetail, DataTable dtSuplierMarkup, DataTable dtSearchConfig, HotelSearchParameter SearchPara, string roomno, string provider, int AgentId, DataTable dtMarkupDetails, string SearchKey,string GiataId = "0", int hotelRating = 0);

        //prebook - 260625
        Task<PriceCheckCMDto> MethaBookPriceCheck(string AvailabilityCode, DataTable dtCheckPriceReqStructure, DataSet dsCartDetails, DataRow[] MethaBookDetails, string UniqueNo, string AgentUniqueNo, int i, string IsFor, double CompanyBuyPrice, bool IsError, double itemprice, string BookingCode, string MealCode, TokenData tokenData, string Searchkey = "", int AgentId = 0);

        string MethaBookBookingRequest(string BookingCode, List<string> RoomCode, string HotelCode, DataTable dtPassengers, DataSet dsCartDetails, string ClientReference, string UniqueNo, bool IsErrors);
        string MethaBookBookingRequest_newversion(string BookingCode, List<string> RoomCode, string HotelCode, DataTable dtPassengers, BookRequest_newversion bookRequest, string ClientReference, string UniqueNo, bool IsErrors);

        Task<ResponseDto> MethaBookResponseNew(string Request, string Url, string UserName, string Password, int TimeOut, string Method, string Searchkey = "", bool frombook = false);

        Task<MethaBookBookingDetail> MethaBookBookingDetail(string ClientReference, int bookingStatus, string AgtVoucherNo, DataRow[] MethaBookDetails, DataSet dsCartDetails, int i, int TimeOut, string refno);
        Task<MethaBookBookingDetail> MethaBookBookingDetail_newversion(string ClientReference, int bookingStatus, string AgtVoucherNo, DataRow[] MethaBookDetails, BookRequest_newversion bookRequest, int i, int TimeOut, string refno);

        Task<int> MethaBookCancellationProcess(string AgentVoucheNo, DataRow[] MethaBookDetails, string rerefenceNo, DataSet dsCartDetails, int bookingStatus);
        Task<int> MethaBookCancellationProcess_newversion(string AgentVoucheNo, DataRow[] MethaBookDetails, string rerefenceNo, BookRequest_newversion bookRequest, int bookingStatus);
    }
}
