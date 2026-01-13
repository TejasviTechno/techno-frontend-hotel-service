using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XConnect.Models.Hotel;
using XConnect.Models;

namespace XConnect.Supplier.Hoteltrader.Interface
{
    public interface IHoteltrader
    {
        void GetHotelTraderRequestOnly_Async(DataRow[] commonCode, List<StarRating> listRating, HotelSearchParameter SearchPara, List<SupplierDetails> lstHotelTraderDetails, List<SupplierSearchConfig> lstHotelTraderSearchConfig, DataTable dtPreferredDataHotel, ref ConcurrentBag<HotelX> AllTask, int Hit, DataSet dtXMLCity,TokenData tokenData,DataTable dtROE);

        Task<List<Options>> HotelTrader_HotelSearchWithRooms(HotelX Item, CommonSearch commonSearch, string currency, string ResponseData1, string CityCode, HotelSearchParameter SearchParam, double CurrencyFactorHotelTader, string Supplier, DataSet dsAgentDetails, Int32 processTimeout, string ResProccessTimerLogActive, string ResProccessTimeValueforSaveLog, string StoreSupplierResErrorLog, DataTable DtROE);

        //Prebook
        Task<PriceCheckDto> HotelTraderPriceCheck(DataSet dsCartDetails, DataRow[] HotelTraderDetails, int j, double CompanyBuyingPrice, double FinalPrice, double CurrencyFactorHotelTrader, string UniqueNo, string AgentUniqueNo, double ResPrice, string SupplierName, int AgentId, TokenData tokenData);
        string HotelTraderBookingRequest(DataSet dsCartDetails, DataRow[] hotelTraderDetails, int i, int j, double resPrice, DataTable dtPassengers, string AgentCode, ref string AgtRef);

        Task<ResponseDto> GetHotelTraderResponse(string Request, string url, string Password, Int32 TimeOut = 120000);

        string GetHotelTrader_BookingCancelRQ(string bookingid);
    }
}
