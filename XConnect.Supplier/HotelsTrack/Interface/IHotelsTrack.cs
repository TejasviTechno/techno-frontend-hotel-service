using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XConnect.Models.Hotel;
using XConnect.Models;

namespace XConnect.Supplier.HotelsTrack.Interface
{
    public interface IHotelsTrack
    {
        void GetHotelsTrackRequestOnly_async(DataRow[] commonCode, List<StarRating> listRating, bool HitFromAll, HotelSearchParameter SearchPara, List<SupplierDetails> lstHotelsTrackDetails, List<SupplierSearchConfig> lstHotelsTrackSearchConfig, DataTable dtPreferredDataHotel, ref ConcurrentBag<HotelX> AllTask, int Hit, TokenData tokenData, DataSet dtXMLCity);

        Task<List<Options>> HotelsTrack_HotelSearchWithRooms(HotelX item, string giitaCode, double AgentPercentage, CommonSearch commonSearch, string currency, string ResponseData, string CityCode, HotelSearchParameter SearchParam, string ContryCode, string XMLNationality, double CurrencyFactorRezlive, string Provider, DataSet dsAgentDetails, DataTable dtCountryCityMarkup, Int32 AgentId, Int32 TariffID, int SupplierID, Int32 processTimeout, string ResProccessTimerLogActive, string ResProccessTimeValueforSaveLog);

        Task<DataSet> GetHotelsTrackCancellationPolicy(double RoomPrice, string incommingofficecode, string Type, DataSet dsHotel, string hotelcode, string ownsystemid, string RoomTypeId_Own, double RoomTotalPrice, string MealID, string Board_Text, DataTable dtAgentDetail, DataTable dtSuplierMarkup, DataTable dtSearchConfig, HotelSearchParameter objSearPera, string roomno, string Provider, int AgentId, DataTable dtMarkupDetails, TokenData tokenData, string GiataId = "0", int hotelRating = 0, string SupplierProvider = "");
        string Get_HotelsTrack_Booking_RQ(DataSet dsCartDetails, DataTable dtPassenger, DataRow[] HotelsTrackDetails, string ExternalRef, bool IsPriceUpdate, int i, int j, string HasKey_IntCode, string Nationality);
        Task<ResponseDto> Response_SupplierHotelsTrack(string Request_Text, string APIUrl, int SearchTimeOut, string SupplieName, bool DebugMode = false);

        Task<PriceCheckHTDto> Get_HotelsTrack_PriceCheck_RQRS(DataSet dsCartDetails, DataRow[] HotelsTrackDetails, string PaxPassPort, int i, int j, DataTable dtCheckPriceReqStructure, string UniqueNo, string AgentUniqueNo, double FinalPrice, bool isForBooking, double CompanyBuyingPrice, string HasKey_IntCode, string IsPrcCheck_Book, string Supplier, int AgentId, TokenData tokenData, string Searchkey = "", string TechName = "");
    }
}
