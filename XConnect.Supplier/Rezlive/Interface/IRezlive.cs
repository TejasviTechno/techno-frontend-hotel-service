using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XConnect.Models.Hotel;
using XConnect.Models;

namespace XConnect.Supplier.Rezlive.Interface
{
    public interface IRezlive
    {
        void GetRezliveRequestOnly_async(DataRow[] commonCode, List<StarRating> listRating, bool HitFromAll, HotelSearchParameter SearchPara, List<SupplierDetails> lstRezliveDetails, List<SupplierSearchConfig> lstRezliveSearchConfig, DataTable dtPreferredDataHotel, ref ConcurrentBag<HotelX> AllTask, int Hit, TokenData tokenData, DataSet dtXMLCity);
        Task<List<Options>> Rezlive_HotelSearchWithRooms(HotelX item, string giitaCode, double AgentPercentage, CommonSearch commonSearch, string currency, string ResponseData, string CityCode, HotelSearchParameter SearchParam, string ContryCode, string XMLNationality, double CurrencyFactorRezlive, string Provider, DataSet dsAgentDetails, DataTable dtCountryCityMarkup, Int32 AgentId, Int32 TariffID, int SupplierID, Int32 processTimeout, string ResProccessTimerLogActive, string ResProccessTimeValueforSaveLog, string ProccessCutOffTime);

        Task<DataSet> Get_Rezlive_CancellationPolicy(string rating, string incommingofficecode, string Type, DataSet dsHotel, string hotelcode, string ownsystemid, string roomtypecode, string RoomTypeId_Own, double RoomTotalPrice, string MealID, string Board_Text, DataTable dtAgentDetail, DataTable dtSuplierMarkup, DataTable dtSearchConfig, HotelSearchParameter objSearPera, string PaxPassport, string roomno, string Provider, int AgentId, DataTable dtMarkupDetails, TokenData tokenData, string GiataId = "0", int hotelRating = 0);
        string Get_Rezlive_PriceCheckRQ(string UserId, string UserName, string Password, string PropertyId, string RoomTypeId, string CheckIn, string CheckOut, string BookingKey, string RezliveCurrency);

        Task<ResponseDto> Rezlive_Response_Supplier_new(string Request_Text, string APIUrl, int SearchTimeOut, string SupplieName, string SearchKey = "", HotelSearchParameter para = null, bool DebugMode = false);

        Task<PriceCheckRezliveDto> Get_Rezlive_PriceCheckRQRS(DataSet dsCartDetails, DataRow[] RezliveDetails, string PaxPassPort, int i, int j, DataTable dtCheckPriceReqStructure, string UniqueNo, string AgentUniqueNo, double FinalPrice, bool isForBooking, double CompanyBuyingPrice, string HasKey_IntCode, string IsPrcCheck_Book, string Supplier, int AgentId, TokenData tokenData);

        string Get_Rezlive_BookingRequest(DataSet dsCartDetails, DataTable dtPassengers, DataRow[] RezliveDetails, string ExternalReference, bool IsPriceUpdate, int i, int j, string BookingKey);

        string Get_Rezlive_BookingCancelRQ(string AgentVoucherNo, DataRow[] SupplierDetails);
    }
}
