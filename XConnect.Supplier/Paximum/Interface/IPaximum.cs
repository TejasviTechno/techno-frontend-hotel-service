using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XConnect.Models.Hotel;
using XConnect.Models;

namespace XConnect.Supplier.Paximum.Interface
{
    public interface IPaximum
    {
        void GetpaximumRequestOnly_Async(DataRow[] commonCode, List<StarRating> listRating, DataTable dtPreferredDataHotel, HotelSearchParameter SearchPara, List<SupplierSearchConfig> lstPaximumSearchConfig, List<SupplierDetails> lstPaximumDetails, ref ConcurrentBag<HotelX> AllTask, string AgentCode, DataTable dtROE, TokenData tokenData, DataSet dtXMLCity);

        Task<List<Options>> Paximum_HotelSearchWithRooms(HotelX Item, CommonSearch commonSearch, string Response, string CityCode, HotelSearchParameter SearchParam, double CurrencyFactorpaximum, string Paximum_NationalityCode, string HotelID, string ProccessCutOffTime, Int32 processTimeout, string ResProccessTimerLogActive, string ResProccessTimeValueforSaveLog, DataTable dtROE, string StoreSupplierResErrorLog, DataSet dsAgentDetails);

        //recheck on 02-06-25
        Task<CancellationDto> paximum_cancellation(string roomtypecode, DataSet dsHotel, string hotelcode, string ownsystemid, string roomtypeid, DataTable dtAgentDetail, DataTable dtSuplierMarkup, DataTable dtSearchConfig, HotelSearchParameter SearchPara, string roomno, string provider, int AgentId, DataTable dtMarkupDetails, string Notes, string GiataId = "0", int hotelRating = 0, string CompanyBuyPrice = "");

        string paximumBookingRequest(DataSet dsCartDetails, DataRow[] paximumDetails, int i, int j, double resPrice, DataTable dtPassengers, ref string AgtRef, string CheckIn, string CheckOut, int UniqueNo);
        Task<ResponseDto> GetpaximumbookingResponse(string strxmlRQRS, string strHost, Int32 TimeOut, string TechName = "", string Password = "");

        string Getpaximum_BookingCancelRQ(string AgentVoucherNo, DataRow[] PaximumDetails);
    }
}
