using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XConnect.Models.Hotel;
using XConnect.Models;

namespace XConnect.Supplier.Parsec.Interface
{
    public interface IParsec
    {
        void GetParsesRequestOnly_Async(DataRow[] commonCode, List<StarRating> listRating, DataTable dtROE, HotelSearchParameter SearchPara, List<SupplierDetails> lstparsecDetails, List<SupplierSearchConfig> lstparsecSearchConfig, DataTable dtPreferredDataHotel, ref ConcurrentBag<HotelX> AllTask,  int Hit, DataSet dtXMLCity, TokenData tokenData);
        Task<List<Options>> Parsec_HotelSearchWithRooms(HotelX Item, CommonSearch commonSearch, double CurrencyFactor, string Currency, string Response, string XMLCITY, string XMLCOUNTRY, string Supplier, HotelSearchParameter SearchParam, Int32 processTimeout, string ResProccessTimerLogActive, string ResProccessTimeValueforSaveLog, DataTable dtROE);

        Task<PriceCheckCMDto> PriceCheckParsec(int i, DataTable dtCheckPriceReqStructure, DataSet dsCartDetails, DataRow[] AbreuDetails, string UniqueNo, string AgentUniqueNo, string IsFor, bool IsError, string supplier, string AgentCode, string Searchkey = "", int AgentId = 0);

        string ParsecOlympiaBooking(string AgentCode, DataTable dtPassengers, DataSet dsCartDetails, string UserName, string Password, string UserId, string UniqueNo, int j);

        string ParsecOlympiaBooking_new(string AgentCode, DataTable dtPassengers, BookRequest_newversion bookRequest, string UserName, string Password, string UserId, string UniqueNo, int j);

        string ParsecBooking(string AgentCode, DataTable dtPassengers, DataSet dsCartDetails, string UserName, string Password, string UserId, string UniqueNo);
        string ParsecBooking_new(string AgentCode, DataTable dtPassengers, BookRequest_newversion bookRequest, string UserName, string Password, string UserId, string UniqueNo);

        string ParsecCancellationRequest(string Booking_Ref, string UserName, string Password, string UserId);

        
    }
}
