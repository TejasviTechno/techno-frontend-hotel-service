using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XConnect.Models.Hotel;
using XConnect.Models;

namespace XConnect.Supplier.InfiniteHotelsFrance.Interface
{
    public interface IInfiniteHotelsFrance
    {
        void GetInfiniteHotelsFrance_async(DataRow[] commonCode, List<StarRating> listRating, HotelSearchParameter SearchPara, List<SupplierDetails> lstInfiniteHotelsFranceDetails, List<SupplierSearchConfig> lstInfiniteHotelsFranceSearchConfig, DataTable dtPreferredDataHotel, ref ConcurrentBag<HotelX> AllTask, int Hit, DataTable dtROE, TokenData tokenData, bool HitFromAll, DataSet dtXMLCity);
        Task<List<Options>> infinitehotelsfrance_HotelSearchWithRooms(HotelX Item, CommonSearch commonSearch, string WebSiteURL, int SearchTimeOut, string Supplier, bool DebugeMode, string UserName, string PassWord, string MethoodType, string ResponseData, HotelSearchParameter SearchParam, string xmlcity, string xmlcountry, string xmlnationality, string currency, double currencyfactor, Int32 processTimeout, string ResProccessTimerLogActive, string ResProccessTimeValueforSaveLog);
        //Recheck 02-06-25
        Task<CancellationDto> Get_Infinitehotelsfrance_CancellationPolicy(string rating, string ContractID, string Type, DataSet dsHotel, string hotelcode, string ownsystemid, string RoomTypeId_Own, string MealID, string Board_Text, DataTable dtAgentDetail, DataTable dtSuplierMarkup, DataTable dtSearchConfig, HotelSearchParameter objSearPera, string roomno, string provider, int AgentId, DataTable dtMarkupDetails, string clientId, string customerKey, string GiataId = "0", int hotelRating = 0, double RoomPrice = 0, string CompanyBuyingPrice = "0");
        Task<string> Get_Infinitehotelsfrance_EInfo(string hotelcode, string ownsystemid, string RoomTypeId_Own, DataTable dtSuplierMarkup, DataTable dtSearchConfig, HotelSearchParameter objSearPera, string provider, string SearchKey);
        //prebook 02-07-25

       Task<PriceCheckDidaDto> Get_Infinitehotelsfrance_PreBookingReqRes(TokenData tokenData, string Currency, string ID, string Password, string HotelCode, string RCode, string CheckInDate, string CheckOutDate, string URL, double Rate, double FinalPrice, int UserCartId, int UserCartDetailId, DataTable dtCheckPriceReqStructure, int j, string UniqueNo, string AgentUniqueNo, bool isForBooking, int NoOfRoom, string UserName, string Newoffercode, string Searchkey, int AgentId = 0, string TechName = "", int srphotelid = 0);

        string Get_Infinitehotelsfrance_BookingRequest(string password, string customerkey, string hotelCode, string code, DataSet dsCartDetails, DataTable dtPassengers, DataRow[] InfinitehotelsfranceDetails, string UniqueNo, int i, int j, string PreBookingToken, string UserName, string Newoffercode, string AgentCode);

        string Get_InfiniteHotelsFrance_BookingDetails_RQ(DataRow[] IHFDetails, string clientId, string password, string customerKey, string AgentRefrance);

        string Get_InfiniteHotelsFrance_CancelationRQ(string RefNO, DataRow[] InfiniteHotelsFranceDetails);
    }
}
