using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MathNet.Numerics;
using XConnect.Models;
using XConnect.Models.Hotel;

namespace XConnect.Supplier.Omnibees.Interface
{
    public interface IOmnibees
    {
        void GetOmnibeesRequestOnly_async(DataRow[] commonCode, List<StarRating> listRating, HotelSearchParameter SearchPara, List<SupplierDetails> OmnibeesDetails, List<SupplierSearchConfig> OmnibeesSearchConfig, DataTable dtPreferredDataHotel, ref ConcurrentBag<HotelX> AllTask, string XMLCity, string Xmlnationality, int Hit, DataSet dtXMLCity, DataTable dtROE,TokenData tokenData);

        void GetOmnibeesRequestOnly_async_new(DataRow[] commonCode, List<StarRating> listRating, HotelSearchParameter SearchPara, List<SupplierDetails> OmnibeesDetails, List<SupplierSearchConfig> OmnibeesSearchConfig, DataTable dtPreferredDataHotel, ref ConcurrentBag<HotelX> AllTask, string XMLCity, string Xmlnationality,int Hit, DataSet dtXMLCity,DataTable dtROE,TokenData tokenData);
    
        Task<List<Options>> XMLOmnibeesPriceOnly_async(HotelX Item, CommonSearch commonSearch, double AgentPercentage, int SearchTimeOut, string Supplier, DataTable dtCountryCityMarkup, int AgentId, DataSet dsAgentDetails, int SupplierID, string ResponseData, HotelSearchParameter SearchParam, string xmlcity, string xmlcountry, string xmlnationality, string OmnibeesCurrency, double CurrencyFactorOmnibees, Boolean IsShowSupplier, DataTable DtROE, Int32 processTimeout, string ResProccessTimerLogActive, string ResProccessTimeValueforSaveLog);

        string Get_Omnibees_BookingRequest(DataSet dsCartDetails, DataTable dtPassengers, DataRow[] OmnibeesDetails, string UniqueNo, bool IsPriceUpdate, int i, int jj, string PreBookingToken, string UserID, string Password, string ExternalReference, string Bookingtokan, string referenceNumber, string AgentUniqueNo, ref string errorinrq, bool IsPruvo);

        string GetOmnibeesResponse(ref string Error, string request, string TravcoURL, Int32 TimeOut, string UserName, string Password, string Provider = "", HotelSearchParameter para = null, bool DebugMode = false);

        string Get_Omnibees_BookingCancelRQ(string AgentVoucherNo, DataRow[] SupplierDetails, string RoomTypeId, string CheckIn, string CheckOut);
    }
}
