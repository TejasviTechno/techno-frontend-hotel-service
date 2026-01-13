using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XConnect.Models.Hotel;
using XConnect.Models;

namespace XConnect.Supplier.Saltours.Interface
{
    public interface ISaltours
    {
        void GetSaltoursRequestOnly_Async(DataRow[] commonCode, List<StarRating> listRating, DataTable dtPreferredDataHotel, HotelSearchParameter SearchPara, List<SupplierSearchConfig> lstSaltoursSearchConfig, List<SupplierDetails> lstSaltoursDetails, ref ConcurrentBag<HotelX> AllTask, string AgentCode, int Hit, DataTable dtROE, TokenData tokenData, DataSet dtXMLCity);

        Task<List<Options>> Saltours_HotelSearchWithRooms(HotelX item, CommonSearch commonSearch, string ResponseData, HotelSearchParameter SearchParam, DataTable dtAgentDetail, DataTable dtSuplierMarkup, DataTable dtMarkupDetails, string XMLCITY, string XMLCountry, string Currency, string Provider, DataSet dsAgentDetails, DataTable dtCountryCityMarkup, int AgentId, Int32 TariffID, double AgentPercentage, Boolean IsShowSupplier, int SupplierID, int MasterSupplierID, string ProccessCutOffTime, Int32 processTimeout, string ResProccessTimerLogActive, string ResProccessTimeValueforSaveLog, DataTable dtROE, string XmlNationality = "", Double CurrencyFactor = 1);

        //prebook - 24-06-25
        Task<PriceCheckDto> SaltoursPriceCheck(DataSet dsCartDetails, DataRow[] saltoursDetails, int j, double FinalPrice, double CurrencyFactorsaltours, string UniqueNo, string AgentUniqueNo,double ResPrice, string SupplierName, int i, string Nationality, double saltoursCurrency,int AgentId, TokenData tokenData);

        string SaltoursBookingDetailRequest(string Password, string UserName, DataSet dsCartDetails, DataRow[] saltoursDetails, int i, int j, double resPrice, DataTable dtPassengers, ref string AgtRef, string CheckIn, string CheckOut, int UniqueNo, string AgentCode = "");
        string SaltoursBookingRequest(string Password, string UserName, DataSet dsCartDetails, DataRow[] saltoursDetails, int i, int j, double resPrice, DataTable dtPassengers, ref string AgtRef, string CheckIn, string CheckOut, int UniqueNo, string BookingId, string AgentCode = "");

        Task<ResponseDto> GetsaltoursResponse(string strxmlRQRS, string strHost, Int32 TimeOut, string Provider = "");

        string Get_Saltours_BookingCancelRQ(string AgentVoucherNo, DataRow[] SaltoursDetails, string ISOCountry, string Uniqu_Id, string Type, string Provider = "");
    }
}
