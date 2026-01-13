using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XConnect.Models.Hotel;
using XConnect.Models;

namespace XConnect.Supplier.MGBedBank.Interface
{
    public interface IMgbedbank
    {
        void GetMgBedBankRequestOnly_async(DataRow[] commonCode, List<StarRating> listRating, string SupplierName, HotelSearchParameter SearchPara, List<SupplierDetails> MgBedBankDetails, List<SupplierSearchConfig> MgBedBankSearchConfig, DataTable dtPreferredDataHotel, ref ConcurrentBag<HotelX> AllTask, string XMLCity, string XMLCountry, string Xmlnationality, int Hit, CommonSearchList commonSearchList);
        void GetMgBedBankRequestOnly_async_new(DataRow[] commonCode, List<StarRating> listRating, HotelSearchParameter SearchPara, List<SupplierDetails> MgBedBankDetails, List<SupplierSearchConfig> MgBedBankSearchConfig, DataTable dtPreferredDataHotel, ref ConcurrentBag<HotelX> AllTask, int Hit, CommonSearchList commonSearchList, DataSet dtXMLCity,TokenData tokenData,DataTable dtROE);

        Task<List<Options>> Mgbedbank_HotelSearchWithRooms(HotelX Item, CommonSearch commonSearch, string currency, string ResponseData, string xmlcity, HotelSearchParameter SearchParam, string xmlcountry, double currencyfactor, string supplier, string URL, string UserId, string UserName, string PassWord, int SearchTimeOut, string XMLNationality, string XmlHotelCode, string ProccessCutOffTime, Int32 processTimeout, string ResProccessTimerLogActive, string ResProccessTimeValueforSaveLog);

        //prebook 02-07-25
        Task<PriceCheckCMDto> Get_MgBedBank_PriceCheckRQRS(DataSet dsCartDetails, DataRow[] MgBedBankDetails, string PaxPassPort, int i, int j, DataTable dtCheckPriceReqStructure, string UniqueNo, string AgentUniqueNo, double FinalPrice, bool isForBooking, double CompanyBuyingPrice, string HasKey_IntCode, string IsPrcCheck_Book, string Supplier, string Searchkey = "", int AgentId = 0,TokenData tokenData = null);

        string Get_MgBedBank_BookingRequest(DataSet dsCartDetails, DataTable dtPassengers, DataRow[] MgBedBankDetails, string ExternalReference, bool IsPriceUpdate, int i, int j, string HasKey_IntCode, string PaxPassport, string cancallationpolicytype, string RoomTypeID, string RoomId, string SessionId, string BoardID, string Nationality, string IsAvail, string UniqNo);

        Task<ResponseDto> MGGetSupplierParallel(string strxmlRQRS, string strHost, string ErrorResponse, Int32 TimeOut, string Provider = "", string SearchKey = "");


    }
}
