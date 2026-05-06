using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using MongoDB.Driver.Core.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using XConnect.CommonUtilities;
using XConnect.CommonUtilities.Interface;
using XConnect.DAL;
using XConnect.DAL.Interface;
using XConnect.ErrorLogHandler.Interface;
using XConnect.Markup.Interface;
using XConnect.Models;
using XConnect.Models.Hotel;
using XConnect.Supplier.Letsfly;
using XConnect.Supplier.Webhoteliter;
using XConnect.Supplier.Welcomebeds.Interface;
using static Azure.Core.HttpHeader;
using static System.Runtime.InteropServices.JavaScript.JSType;
using static XConnect.Supplier.Booking.Com.BookingDotComRoomEss;
using static XConnect.Supplier.Booking.Com.bookingrq;
using static XConnect.Supplier.Booking.Com.preebookrs;
using static XConnect.Supplier.PkFare.PkfInfoRes;
using static XConnect.Supplier.Webhoteliter.WebHotellier.WebHotelierRoomResponse;
using Options = XConnect.Models.Options;


namespace XConnect.Supplier.Welcomebeds
{
    public class WelcomebedsRepository : IWelcomebeds
    {
        private readonly ICommonMethod _commonMethod;
        private readonly IException _exception;
        private readonly IMarkup _markup;
        private readonly ISupplierRes _supplierRes;
        private readonly KeySettings _keySettings;
        private readonly IVCN _vcn;
        private readonly IDLConnection _dlConnection;
        private readonly SupplierConfiguration _supplierConfig;//Added on 26-FEB-26


        public WelcomebedsRepository(ICommonMethod commonMethod, IMarkup markup, ISupplierRes supplierRes, IException exception, IOptions<KeySettings> keySettings, IVCN vcn, IDLConnection dlConnection,IOptions<SupplierConfiguration> supplierConfig)
        {
            _commonMethod = commonMethod;
            _markup = markup;
            _supplierRes = supplierRes;
            _exception = exception;
            _keySettings = keySettings.Value;
            _vcn = vcn;
            _dlConnection = dlConnection;
            _supplierConfig = supplierConfig.Value;
        }

        public void GetwelcomebedsRequestOnly_Async(DataRow[] commonCode, List<StarRating> listRating, HotelSearchParameter SearchParam, List<SupplierDetails> ListWelcomebedsDetails, List<SupplierSearchConfig> lstWelcomebedsSearchConfig, DataTable dtPreferredDataHotel, ref ConcurrentBag<HotelX> AllTask, int Hit, DataTable dtROE, DataSet dtXMLCity, TokenData tokenData)
        {
            try
            {
                if (ListWelcomebedsDetails.Count > 0)
                {
                    List<StarRating> lstRating = new List<StarRating>();
                    lstRating = _commonMethod.GetStarRatingStringList_xtg(SearchParam);

                    List<HotelCodeByCity> DtHotelDAta = HtmlExtensions.ToList<HotelCodeByCity>(commonCode.CopyToDataTable());
                    int ratingcount = lstRating.Count;
                    int hotelcount = DtHotelDAta.Count;
                    foreach (SupplierDetails WelcomebedsDetails in ListWelcomebedsDetails)
                    {
                        try
                        {
                            int SupplierID = WelcomebedsDetails.SupplierID;
                            string Supplier = WelcomebedsDetails.SupplierName;

                            if (lstWelcomebedsSearchConfig.Count > 0)
                            {
                                List<SupplierSearchConfig> WelcomebedsSearchConfig = lstWelcomebedsSearchConfig
                                       .Where(x => x != null && x.SupplierID == SupplierID)
                                       .ToList();

                                if (WelcomebedsSearchConfig != null && WelcomebedsSearchConfig.Count > 0)
                                {
                                    DataForSearch dataForSearch = _commonMethod.GetXmlDataForSearchTechwise(SearchParam.Nationality, dtXMLCity, Supplier, WelcomebedsDetails.TechName);

                                    List<HotelCodeByCity> filter = new List<HotelCodeByCity>();
                                    filter = _commonMethod.FilterWithOutCity_Code(ratingcount, hotelcount, DtHotelDAta, lstRating, Supplier);

                                    if (WelcomebedsSearchConfig != null)
                                    {
                                        double CurrencyFactorwelcomebeds = 1;

                                        string Currency = Convert.ToString(WelcomebedsDetails.CurrencyShortName);


                                        if (Currency.ToLower() != tokenData.SiteCurrency.ToLower())
                                        {
                                            CurrencyFactorwelcomebeds = _commonMethod.GetROEByCurrency(Currency, dtROE);
                                        }

                                        for (int k = 0; k < WelcomebedsSearchConfig.Count; k++)
                                        {
                                            bool DebugMode = (Convert.ToInt32(WelcomebedsSearchConfig[k].DebugMode) == 0 ? false : true);

                                            #region HotelCodeWise

                                            if (commonCode.Length > 0)
                                            {
                                                if (Convert.ToInt32(WelcomebedsSearchConfig[k].CityID) == 0 || Convert.ToInt32(WelcomebedsSearchConfig[k].CityID) == SearchParam.CityID)//All City means 0
                                                {

                                                    int Noofrecords = Convert.ToInt32(WelcomebedsSearchConfig[k].Noofrecords);
                                                    double count = Convert.ToDouble(filter.Count);
                                                    int NoOfDiv = Convert.ToInt32(Math.Ceiling(count / Convert.ToDouble(Noofrecords)));
                                                    List<List<XMLID_Search>> lstxml = _commonMethod.GetXMLIDString(NoOfDiv, Noofrecords, count, filter);
                                                    try
                                                    {

                                                        for (int a = 0; a < NoOfDiv; a++)
                                                        {
                                                            string hotelId = Convert.ToString(_commonMethod.GetHotelCodeByCOmma(lstxml[a]));
                                                            HotelX xhotels = new HotelX();

                                                            if (_keySettings.SupplierProxy == "0")
                                                                xhotels.URL = Convert.ToString(WelcomebedsDetails.WebSiteURL) + _supplierConfig.Welcomebeds.Welcomebeds_HotelSearch;
                                                            else
                                                                xhotels.URL = Convert.ToString(WelcomebedsDetails.WebSiteURLforAPI) + _supplierConfig.Welcomebeds.Welcomebeds_HotelSearch;

                                                            xhotels.UserName = Convert.ToString(WelcomebedsDetails.UserName);
                                                            xhotels.PassWord = Convert.ToString(WelcomebedsDetails.Password);
                                                            xhotels.SearchTimeOut = Convert.ToInt32(WelcomebedsSearchConfig[k].HotelSearchTimeOut);
                                                            if (Hit > 0)
                                                                xhotels.SearchTimeOut = Convert.ToInt32(WelcomebedsSearchConfig[k].HotelSearchTimeOut2);
                                                            xhotels.IsShowSupplier = Convert.ToInt16(WelcomebedsDetails.IsShowSupplier) == 1 ? true : false;
                                                            xhotels.DebugeMode = DebugMode;
                                                            xhotels.MethoodType = "GET"; //need to ask
                                                            xhotels.Supplier = Supplier;
                                                            xhotels.CurrencyCode = Currency;
                                                            xhotels.CurrencyFactor = CurrencyFactorwelcomebeds;
                                                            xhotels.XmlNationality = dataForSearch.XmlNationality;
                                                            xhotels.XmlCity = dataForSearch.xmlCity;
                                                            xhotels.XmlCountry = dataForSearch.xmlCountry;
                                                            xhotels.AgentWiseMarkup = WelcomebedsDetails.AgentWiseMarkup;
                                                            xhotels.IsShowSupplier = Convert.ToInt16(WelcomebedsDetails.IsShowSupplier) == 1 ? true : false;
                                                            xhotels.SupplierID = SupplierID;
                                                            xhotels.MasterSupplierID = WelcomebedsDetails.MasterSupplierId;

                                                            #region "CoomonCode"
                                                            bool IsHitSeperate = _commonMethod.IsSuppliersWithSeperateRoomRquest(xhotels.Supplier);
                                                            int NoOfRoomsSearch = 1;
                                                            if (IsHitSeperate)
                                                                NoOfRoomsSearch = SearchParam.Rooms.Count;
                                                            List<RQRS> RQRS = new List<RQRS>();
                                                            xhotels.IsSeperate = IsHitSeperate;
                                                            for (int rr = 0; rr < NoOfRoomsSearch; rr++)
                                                            {
                                                                RQRS objrq = new RQRS();
                                                                List<RoomDetails> serchRooms = new List<RoomDetails>();
                                                                serchRooms.Add(SearchParam.Rooms[rr]);
                                                                objrq.Request = XMLwelcomebedsRequestOnly_Async(SearchParam, dataForSearch.xmlCity, dataForSearch.xmlCountry, _supplierConfig.Welcomebeds.Welcomebeds_HotelSearch, Convert.ToString(WelcomebedsDetails.UserName), Convert.ToString(WelcomebedsDetails.Password), dataForSearch.XmlNationality, Convert.ToInt32(xhotels.SearchTimeOut), WelcomebedsDetails.CurrencyShortName, hotelId);
                                                                objrq.Response = "";
                                                                RQRS.Add(objrq);
                                                            }
                                                            xhotels.ReturnRooms = WelcomebedsDetails.ReturnRooms;
                                                            xhotels.TechName = WelcomebedsDetails.TechName;
                                                            xhotels.RQRS = RQRS;
                                                            #endregion

                                                            AllTask.Add(xhotels);

                                                        }
                                                    }
                                                    catch (Exception ex)
                                                    {
                                                        _ = _exception.ErrorNew(ex, ErrorLog._LogAPI.Availability);

                                                    }

                                                }
                                            }

                                            #endregion

                                        }

                                    }

                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _ = _exception.ErrorNew(ex, ErrorLog._LogAPI.Availability);
                        }

                    }
                }
            }

            catch (Exception ex)
            {
                _ = _exception.ErrorNew(ex, ErrorLog._LogAPI.Availability);
            }
        }

        public string XMLwelcomebedsRequestOnly_Async(HotelSearchParameter SearchParam, string CityCode, string CountryCode, string URL, string User_Name, string Password, string Nationality, Int32 TimeOut, string Currency, string hotelId)
        {

            try
            {
                string echotoken = Convert.ToString(Guid.NewGuid()).Substring(0, 13);
                int roomno = Convert.ToInt32(SearchParam.Rooms[0].RoomNo);
                string checkin = System.Convert.ToString(SearchParam.CheckInDate);
                string checkout = Convert.ToString(SearchParam.CheckOutDate);
                checkin = checkin.Substring(6, 4) + "-" + checkin.Substring(3, 2) + "-" + checkin.Substring(0, 2);
                checkout = checkout.Substring(6, 4) + "-" + checkout.Substring(3, 2) + "-" + checkout.Substring(0, 2);
                StringBuilder wb1 = new StringBuilder();
                wb1.Append("{");
                wb1.Append("\"hotelAvailability\":");
                wb1.Append("{");
                wb1.Append("\"searchAvail\":");
                wb1.Append("{");
                wb1.Append("\"market\":\"" + Nationality + "\",");
                wb1.Append("\"checkIn\":\"" + checkin + "\",");
                wb1.Append("\"checkOut\":\"" + checkout + "\",");
                wb1.Append("\"destination\":");
                wb1.Append("{");
                if (hotelId.Trim() != string.Empty)
                {
                    wb1.Append("\"hotelCodes\":[\"");
                    hotelId = hotelId.Replace(",", "\",\"");
                    wb1.Append(hotelId);
                    wb1.Append("\"]");

                }

                wb1.Append("},");
                wb1.Append("\"rooms\":");
                wb1.Append("[");
                for (int ser = 0; ser < SearchParam.Rooms.Count; ser++)
                {
                    if (ser == 0)
                        wb1.Append("{");
                    else
                        wb1.Append(",{");
                    roomno = ser + 1;
                    wb1.Append("\"index\":" + roomno + " ,");
                    wb1.Append("\"passengerAges\":");
                    wb1.Append("[");
                    #region adults child count
                    StringBuilder guest = new StringBuilder();


                    for (int i = 0; i < SearchParam.Rooms[ser].NoofAdults; i++)
                    {
                        if (i == 0)
                            guest.Append("30");
                        else
                            guest.Append(",30");
                    }
                    for (int i = 0; i < SearchParam.Rooms[ser].NoofChild; i++)
                    {
                        if (i == 0)
                            guest.Append("," + SearchParam.Rooms[ser].Child1Age.ToString());
                        else
                            guest.Append("," + SearchParam.Rooms[ser].Child2Age.ToString());
                    }


                    #endregion
                    wb1.Append(guest);
                    wb1.Append("]");
                    wb1.Append("}");
                }
                wb1.Append("]");
                wb1.Append("},");
                wb1.Append("\"timeout\":" + TimeOut + "");
                wb1.Append("},");
                wb1.Append("\"token\":\"" + echotoken + "\"");
                wb1.Append("}");
                string welcomebedsstring = wb1.ToString();
                return welcomebedsstring;
            }
            catch (Exception e)
            {
                _exception.Error(e);
            }
            return "";

        }
        public async Task<List<Options>> Welcomebeds_HotelSearchWithRooms(HotelX item, CommonSearch commonSearch, string ResponseData, string WSurl, string Username, string Password, Int32 SearchTimeOut, HotelSearchParameter SearchParam, bool DebugMode, string Currency, double CurrencyFactor, DataSet dsAgentDetails, string Provider, DataTable dtCountryCityMarkup, Int32 AgentId, Int32 TariffID, double AgentPercentage, bool IsShowSupplier, int SupplierID, int MasterSupplierID, Int32 processTimeout, string ProccessCutOffTime, string ResProccessTimerLogActive, string ResProccessTimeValueforSaveLog, string StoreSupplierResErrorLog, DataTable dtROE, DataTable dtSuplierMarkup, string xmlhotelcode)
        {
            List<Options> options = new List<Options>();
            DataTable DtHotel = _commonMethod.GenerateDataTableNewV2();

            System.Diagnostics.Stopwatch timer_room_Welcomebeds = new System.Diagnostics.Stopwatch();
            timer_room_Welcomebeds.Start();
            WelcomebedsClass.Welcomebeds.welcomebedsavailabilityresponse.Root welResponse = null;

            try
            {
                if (ResponseData == null || ResponseData.Trim() == "")
                {
                    item.ResponseStatus = 204;
                    return options;
                }
                try
                {
                    welResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<WelcomebedsClass.Welcomebeds.welcomebedsavailabilityresponse.Root>(ResponseData);

                }
                catch (Exception ex)
                {
                    _exception.Error(ex);
                }

                if (ResponseData.ToLower().Contains("error"))
                {
                    item.SupplierResponseError = ResponseData;
                    item.ResponseStatus = 404;
                    item.ResponseType = "NoCount";
                    return options;
                }

                if (ResponseData == null || ResponseData == string.Empty)
                {
                    item.ResponseStatus = 204;
                    return options;
                }
                    
                else
                {

                    if (welResponse == null || welResponse.hotels == null || welResponse.hotels.Count < 1)
                    {
                        item.SupplierResponseError = ResponseData;
                        item.ResponseStatus = 404;
                        item.ResponseType = "NoCount";
                    }
                    for (int i = 0; i < welResponse.hotels.Count; i++)
                    {
                        item.ResponseType = "Success";
                        if (ProccessCutOffTime == "1" && timer_room_Welcomebeds.ElapsedMilliseconds > processTimeout)
                        {
                            timer_room_Welcomebeds.Stop();
                            break;
                        }

                        if (SearchParam.Filters.IsShowRooms == 0)
                        {
                            {
                                DataRow drXmlHotel = DtHotel.NewRow();

                                drXmlHotel["ContractID"] = 0;
                                drXmlHotel["SupplierId"] = SupplierID;
                                drXmlHotel["HotelName"] = "";
                                drXmlHotel["MasterSupplierId"] = Convert.ToInt32(MasterSupplierID);
                                drXmlHotel["IsShowSupplier"] = false;
                                drXmlHotel["UniqueNo"] = Guid.NewGuid().ToString();
                                drXmlHotel["OwnsystemHotelId"] = 0;
                                drXmlHotel["XMLHotelCode"] = welResponse.hotels[i].hotelCode;
                                drXmlHotel["SupplierName"] = Provider;
                                drXmlHotel["SupplierCurrency"] = Currency;
                                double FinalTotal = 0;
                                for (int j = 0; j < welResponse.hotels[i].roomRates.Count; j++)
                                {
                                    if (FinalTotal == 0)
                                    {
                                        FinalTotal = Convert.ToDouble(welResponse.hotels[i].roomRates[j].pricing.sell.price);
                                    }
                                    else
                                    {
                                        if (Convert.ToDouble(welResponse.hotels[i].roomRates[j].pricing.sell.price) < FinalTotal)
                                        {
                                            FinalTotal = Convert.ToDouble(welResponse.hotels[i].roomRates[j].pricing.sell.price);
                                        }
                                    }
                                }

                                if (Currency.ToLower() != commonSearch.tokenData.SiteCurrency.ToLower())
                                {
                                    CurrencyFactor = _commonMethod.GetROEByCurrency(Currency, dtROE);

                                }

                                FinalTotal = Convert.ToDouble(Convert.ToDouble(FinalTotal * CurrencyFactor));

                                if (FinalTotal == 0)
                                    continue;
                                FinalTotal = 1;
                                drXmlHotel["SellingPrice"] = FinalTotal;
                                drXmlHotel["FinalSellingPrice"] = FinalTotal;
                                DtHotel.Rows.Add(drXmlHotel);
                                if (FinalTotal > 0)
                                {
                                    #region "Room code Block"
                                    bool willAddOrNot = true;
                                    Int64 RaynaID = 0;
                                    Int64 GiataId = 0;
                                    int Rating = 0;
                                    Int64 OwnsystemHotelId = 0;
                                    double MinPrice = FinalTotal;
                                    _markup.GetHotelDataCommon(ref MinPrice, ref willAddOrNot, ref OwnsystemHotelId, ref RaynaID, ref GiataId, ref Rating, ref drXmlHotel, commonSearch.AgentId, commonSearch.TariffID, commonSearch.dsAgentDetails, commonSearch.supplierGiataMaster, SearchParam, commonSearch.dtCountryCityMarkup, commonSearch.B2CMarkup, commonSearch.IsParentId, commonSearch.SubAgentMarkup, commonSearch.SearchByHotel, commonSearch.HotelIDS);
                                    if (willAddOrNot)
                                    {
                                        #region "Room Logic"
                                        DataTable dt_HotelRoom = _commonMethod.GenerateRoomtypeDataTableNewV2();
                                        DataTable dt_Cancellation = _commonMethod.GenerateCancellationDataTableNewV2();
                                        DataTable dt_RateBreackUp = _commonMethod.GenerateoomRateBreackUpDataTableNewV2();
                                        #endregion
                                        decimal LowestPrice = Convert.ToDecimal(MinPrice).CalculatePrice(commonSearch.tokenData, commonSearch.advancedOptions, Currency);
                                        Options ops = (await _supplierRes.GetCommonRoomForSuppliers(GiataId, commonSearch.tokenData, commonSearch.advancedOptions, Convert.ToDouble(LowestPrice), drXmlHotel, dt_HotelRoom, dt_Cancellation, dt_RateBreackUp, SearchParam));
                                        if (ops != null)
                                            options.Add(ops);
                                    }
                                    #endregion

                                }
                            }
                        }
                        else
                        {
                            DataRow drXmlHotel = DtHotel.NewRow();

                            drXmlHotel["ContractID"] = 0;
                            drXmlHotel["SupplierId"] = SupplierID;
                            drXmlHotel["HotelName"] = "";
                            drXmlHotel["MasterSupplierId"] = Convert.ToInt32(MasterSupplierID);
                            drXmlHotel["IsShowSupplier"] = false;
                            drXmlHotel["UniqueNo"] = Guid.NewGuid().ToString();
                            drXmlHotel["OwnsystemHotelId"] = 0;
                            drXmlHotel["XMLHotelCode"] = welResponse.hotels[i].hotelCode;
                            drXmlHotel["SupplierName"] = Provider;
                            drXmlHotel["SupplierCurrency"] = Currency;
                            #region "comment"
                            //double FinalTotal = 0;
                            //for (int j = 0; j < welResponse.hotels[i].roomRates.Count; j++)
                            //{
                            //    if (FinalTotal == 0)
                            //    {
                            //        FinalTotal = Convert.ToDouble(welResponse.hotels[i].roomRates[j].pricing.sell.price);
                            //    }
                            //    else
                            //    {
                            //        if (Convert.ToDouble(welResponse.hotels[i].roomRates[j].pricing.sell.price) < FinalTotal)
                            //        {
                            //            FinalTotal = Convert.ToDouble(welResponse.hotels[i].roomRates[j].pricing.sell.price);
                            //        }
                            //    }
                            //}

                            //if (Currency.ToLower() != commonSearch.tokenData.SiteCurrency.ToLower())
                            //{
                            //    CurrencyFactor = _commonMethod.GetROEByCurrency(Currency, dtROE);

                            //}
                            //FinalTotal = Convert.ToDouble(Convert.ToDouble(FinalTotal * CurrencyFactor));

                            //if (FinalTotal == 0)
                            //    continue;
                            #endregion

                            double FinalTotal = 1;
                            drXmlHotel["SellingPrice"] = FinalTotal;
                            drXmlHotel["FinalSellingPrice"] = FinalTotal;
                            DtHotel.Rows.Add(drXmlHotel);
                            if (FinalTotal > 0)
                            {
                                #region "Room code Block"
                                bool willAddOrNot = true;
                                Int64 RaynaID = 0;
                                Int64 GiataId = 0;
                                int Rating = 0;
                                Int64 OwnsystemHotelId = 0;
                                double MinPrice = FinalTotal;
                                _markup.GetHotelDataCommon(ref MinPrice, ref willAddOrNot, ref OwnsystemHotelId, ref RaynaID, ref GiataId, ref Rating, ref drXmlHotel, commonSearch.AgentId, commonSearch.TariffID, commonSearch.dsAgentDetails, commonSearch.supplierGiataMaster, SearchParam, commonSearch.dtCountryCityMarkup, commonSearch.B2CMarkup, commonSearch.IsParentId, commonSearch.SubAgentMarkup, commonSearch.SearchByHotel, commonSearch.HotelIDS);
                                if (willAddOrNot)
                                {
                                    #region "Room Logic"
                                    DataTable dt_HotelRoom = _commonMethod.GenerateRoomtypeDataTableNewV2();
                                    DataTable dt_Cancellation = _commonMethod.GenerateCancellationDataTableNewV2();
                                    DataTable dt_RateBreackUp = _commonMethod.GenerateoomRateBreackUpDataTableNewV2();
                                    double AgentMarkupAmount = 0.0;
                                    bool blnIsShowSupplier = false;
                                    int RatingHotel = 0;

                                    bool isPerce = true;
                                    //_markup.getAgetMarkupSetting(Provider, GiataId.ToString(), commonSearch.dtSuplierMarkup, commonSearch.dtMarkupDetails, ref AgentPercentage, ref AgentMarkupAmount, ref blnIsShowSupplier, ref isPerce, commonSearch.AgentId, RatingHotel, SearchParam.CityID, SearchParam.CountryID.ToString(), SearchParam.CheckInDate);


                                    _markup.getAgetMarkupSettingNew_new(Provider, Convert.ToString(RaynaID), commonSearch.dtSuplierMarkup, commonSearch.dsMarkupDetails, ref AgentPercentage, ref AgentMarkupAmount, ref blnIsShowSupplier, ref isPerce, commonSearch.AgentId, RatingHotel, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), SearchParam.CheckInDate);

                                    for (int h = 0; h < welResponse.hotels[i].roomRates.Count; h++)
                                    {
                                        int totalAdult = 0;
                                        int totalChild = 0;
                                        string remark = "";

                                        for (int b = 0; b < SearchParam.Rooms.Count; b++)
                                        {
                                            totalAdult = Convert.ToInt32(SearchParam.Rooms[b].NoofAdults) + totalAdult;
                                            totalChild = Convert.ToInt32(SearchParam.Rooms[b].NoofChild) + totalChild;
                                        }
                                        string RateId = welResponse.hotels[i].roomRates[h].rateID;
                                        string RateName = welResponse.hotels[i].roomRates[h].rateName;
                                        string BookToken = welResponse.hotels[i].roomRates[h].bookToken;
                                        string Board = welResponse.hotels[i].roomRates[h].meal.name;
                                        string MealName = welResponse.hotels[i].roomRates[h].meal.name;
                                        double TotalPriceNEW = welResponse.hotels[i].roomRates[h].pricing.sell.price;
                                        string currency = welResponse.hotels[i].roomRates[h].pricing.currency;
                                        string RoomName = "";

                                        for (int ro = 0; ro < welResponse.hotels[i].roomRates[h].rooms.Count; ro++)
                                        {
                                            string RoomName1 = welResponse.hotels[i].roomRates[h].rooms[ro].name;
                                            RoomName = RoomName + "(" + (ro + 1) + ")" + RoomName1;
                                        }
                                        int RoomNo = 1;


                                        double CurrencyFactorwelcomebeds = 1;
                                        if (Currency.ToLower() != commonSearch.tokenData.SiteCurrency.ToLower())
                                        {
                                            CurrencyFactorwelcomebeds = _commonMethod.GetROEByCurrency(Currency, dtROE);
                                        }

                                        double TotalPrice = TotalPriceNEW * CurrencyFactorwelcomebeds;

                                        string uniqid = Guid.NewGuid().ToString();
                                        double CancellationPricenew = TotalPrice;
                                        string RoomTypeId = RateId + "###" + xmlhotelcode + "###" + TotalPrice + "###" + BookToken;
                                        double TotalAmount = TotalPrice;

                                        #region Ratebreakup
                                        //for (int n = 0; n < SearchParam.NoofNights; n++)
                                        //{
                                        //    string CheckInDate = Convert.ToString(SearchParam.CheckInDate);
                                        //    string CheckOutDate = Convert.ToString(SearchParam.CheckOutDate);
                                        //    DateTime dtCheckInDate = DateTime.ParseExact(CheckInDate, "dd/MM/yyyy", null);
                                        //    DateTime dtCheckOutDate = DateTime.ParseExact(CheckOutDate, "dd/MM/yyyy", null);
                                        //    TimeSpan t = dtCheckOutDate - dtCheckInDate;
                                        //    double ratePerNight = TotalAmount;


                                        //    #region Vat
                                        //    double VatOnPerNight = 0;
                                        //    ratePerNight = _markup.ProcessHotelRateBreakupPrice_WithMarkup(1, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), ratePerNight, commonSearch.B2CMarkup, commonSearch.IsParentId, isPerce, AgentPercentage, AgentMarkupAmount, commonSearch.SubAgentMarkup, ref VatOnPerNight);
                                        //    #endregion

                                        //    ratePerNight = ratePerNight / SearchParam.NoofNights;
                                        //    DataRow drRateBreackUp = dt_RateBreackUp.NewRow();
                                        //    drRateBreackUp["SupplierName"] = Provider;
                                        //    drRateBreackUp["OwnsystemHotelId"] = OwnsystemHotelId;
                                        //    drRateBreackUp["ContractID"] = 0;
                                        //    drRateBreackUp["RoomNo"] = Convert.ToInt32(RoomNo);
                                        //    drRateBreackUp["RoomTypeId"] = RoomTypeId;
                                        //    drRateBreackUp["MealId"] = Board;
                                        //    drRateBreackUp["Meal"] = MealName;
                                        //    string rateWeekColumn = Convert.ToString(dtCheckInDate.Date.AddDays(n).DayOfWeek).Substring(0, 3);
                                        //    drRateBreackUp["DateOfWeek"] = rateWeekColumn;
                                        //    DateTime rateDate = dtCheckInDate.Date.AddDays(n);
                                        //    drRateBreackUp["Date"] = rateDate;
                                        //    drRateBreackUp["Rate"] = ratePerNight;
                                        //    drRateBreackUp["OrderNo"] = 1;
                                        //    drRateBreackUp["RoomTypeName"] = RoomName;
                                        //    drRateBreackUp["DateOfWeek"] = rateWeekColumn;
                                        //    drRateBreackUp["Date"] = rateDate;
                                        //    drRateBreackUp["BookingStatus"] = "Available";
                                        //    drRateBreackUp["BoardTypeID"] = "";
                                        //    drRateBreackUp["VatdiffAmount"] = 0;
                                        //    drRateBreackUp["RateType"] = RoomName;
                                        //    drRateBreackUp["SupplierCurrency"] = Currency;
                                        //    drRateBreackUp["BuyingPrice"] = ratePerNight;
                                        //    dt_RateBreackUp.Rows.Add(drRateBreackUp);
                                        //}
                                        #endregion

                                        #region Rooms
                                        DataRow drRoom = dt_HotelRoom.NewRow();
                                        drRoom["OwnsystemHotelId"] = OwnsystemHotelId;
                                        drRoom["BoardType"] = Board;
                                        drRoom["MealName"] = Board;
                                        drRoom["MealId"] = Board;
                                        drRoom["Type"] = "";
                                        drRoom["Board"] = "";
                                        drRoom["BookingStatus"] = "Available";
                                        drRoom["UniqueNo"] = Guid.NewGuid().ToString();
                                        drRoom["ContractID"] = 0;
                                        drRoom["RoomNo"] = Convert.ToInt32(RoomNo);
                                        drRoom["BoardTypeID"] = "";
                                        drRoom["AdultsOccupancy"] = totalAdult;
                                        drRoom["ChildOccupancy"] = totalChild;
                                        drRoom["OfferCode"] = "";
                                        drRoom["OfferDescription"] = "";
                                        drRoom["RoomSequenceNo"] = "";
                                        drRoom["TotalSurcharge"] = 0;
                                        string EssentialInformation = remark;

                                        drRoom["EssentialInformation"] = EssentialInformation;
                                        drRoom["Characteristic"] = "";
                                        bool IsCombine = true;
                                        bool IsDynamicPolicy = false;
                                        #region Vat
                                        double itemprice = TotalPrice;
                                        _markup.ProcessHotelRoom_WithMarkup(CurrencyFactorwelcomebeds, Currency, IsDynamicPolicy, IsCombine, SearchParam.NoofNights * SearchParam.Rooms.Count, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), SearchParam.CheckInDate, SearchParam.CheckOutDate, ref TotalPrice, ref drRoom, isPerce, AgentPercentage, AgentMarkupAmount, commonSearch.SubAgentMarkup, commonSearch.B2CMarkup, commonSearch.IsParentId);
                                        #endregion
                                        drRoom["SupplierName"] = Provider;
                                        drRoom["RoomTypeName"] = RoomName;
                                        drRoom["RoomTypeId"] = RoomTypeId;
                                        dt_HotelRoom.Rows.Add(drRoom);
                                        #endregion

                                        #region New 3906
                                        {
                                            int c = 0;
                                            double CancelletionPrice = (welResponse.hotels[i].roomRates[h].cancellationPolicies[c].pricing.net.price);
                                            string FromDate = (welResponse.hotels[i].roomRates[h].cancellationPolicies[c].from);
                                            string ToDate = (welResponse.hotels[i].roomRates[h].cancellationPolicies[c].to);
                                            double Penalty = 0;
                                            DateTime CancelDate = DateTime.Now.Date;

                                            DateTime OnlyDate = Convert.ToDateTime(ToDate);
                                            #region Cancellation Policy Logic and Row

                                            CancelDate = OnlyDate;
                                            #region Vat
                                            double VatOnPolicy = 0;
                                            double MainAgentCharge = CancellationPricenew;
                                            double CompanyChargeAmount = CancellationPricenew;

                                            Penalty = _markup.ProcessHotelCancelationPrice_WithMarkup(ref MainAgentCharge, SearchParam.NoofNights, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), CancellationPricenew, isPerce, commonSearch.B2CMarkup, AgentPercentage, AgentMarkupAmount, commonSearch.IsParentId, commonSearch.SubAgentMarkup, ref VatOnPolicy);

                                            #endregion
                                            int CutOffDays = _commonMethod.GetCutOffDays(dtSuplierMarkup, Provider);
                                            DateTime NowDate = DateTime.Now.Date;
                                            if (CancelDate.AddDays(CutOffDays).Date > DateTime.Now.Date)
                                                CancelDate = CancelDate.AddDays(CutOffDays).Date;
                                            else
                                                CancelDate = DateTime.Now.Date;

                                            DateTime olddate;

                                            if (NowDate < CancelDate && CancelletionPrice == 0)
                                            {
                                                double zero = 0;
                                                #region new
                                                {
                                                    dt_Cancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref zero, dt_Cancellation.NewRow(), CurrencyFactorwelcomebeds, currency, Provider, Convert.ToString(OwnsystemHotelId), Convert.ToString(RoomNo),
                                                                       Convert.ToString(RoomTypeId), RoomName, Board, Convert.ToDateTime(NowDate), Convert.ToDateTime(CancelDate).AddDays(-1), 0, 0, "0", VatOnPolicy, uniqid, "NO", "", "", "", ""));
                                                    dt_Cancellation.AcceptChanges();
                                                    dt_Cancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref CompanyChargeAmount, dt_Cancellation.NewRow(), CurrencyFactorwelcomebeds, currency, Provider, Convert.ToString(OwnsystemHotelId), Convert.ToString(RoomNo),
                                                                                 Convert.ToString(RoomTypeId), RoomName, Board, Convert.ToDateTime(CancelDate), DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null), MainAgentCharge, Penalty, "0", VatOnPolicy, uniqid, "NO", "", "", "", ""));
                                                    dt_Cancellation.AcceptChanges();
                                                }

                                                #endregion

                                            }
                                            else
                                            {
                                                dt_Cancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref CompanyChargeAmount, dt_Cancellation.NewRow(), CurrencyFactorwelcomebeds, currency, Provider, Convert.ToString(OwnsystemHotelId), Convert.ToString(RoomNo),
                                                                      Convert.ToString(RoomTypeId), RoomName, Board, NowDate, DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null), MainAgentCharge, Penalty, "0", VatOnPolicy, uniqid, "Yes", "", ""));
                                                dt_Cancellation.AcceptChanges();
                                            }

                                            #endregion

                                        }
                                        #endregion

                                    }

                                    #endregion
                                    decimal LowestPrice = Convert.ToDecimal(MinPrice).CalculatePrice(commonSearch.tokenData, commonSearch.advancedOptions, Currency);
                                    Options ops = (await _supplierRes.GetCommonRoomForSuppliers(GiataId, commonSearch.tokenData, commonSearch.advancedOptions, Convert.ToDouble(LowestPrice), drXmlHotel, dt_HotelRoom, dt_Cancellation, dt_RateBreackUp, SearchParam));
                                    if (ops != null)
                                        options.Add(ops);
                                }
                                #endregion

                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                item.ResponseStatus = 404;
                _ = _exception.Error(e);
                return options;
            }
            finally
            {
                #region ResProcessTimer
                try
                {
                    timer_room_Welcomebeds.Stop();
                    item.CodeProcessingTimeSupplier = timer_room_Welcomebeds.ElapsedMilliseconds;
                    if (SearchParam != null && ResProccessTimerLogActive == "1" && timer_room_Welcomebeds.ElapsedMilliseconds > Convert.ToInt32(ResProccessTimeValueforSaveLog))
                    {
                       
                        SearchParam.SupplierTime += ",|Finish:Agoda Process Response  " + "|Time :" + timer_room_Welcomebeds.Elapsed.ToString();
                        _ = Task.Run(() => _commonMethod.FireAway_SQL_ResPorcessTimeLog("", timer_room_Welcomebeds.Elapsed.ToString(), Provider, SearchParam.SearchKey, 1, SearchParam.AgentID));
                    }
                }
                catch (Exception x)
                {
                    _ = _exception.Error(x);
                }
                #endregion

                if (item.ResponseType == "NoCount" && StoreSupplierResErrorLog == "1")
                    _ = _commonMethod.XMLRequestResponseTrack_Error(item.RQRS[0].Request, ResponseData, item.Supplier, timer_room_Welcomebeds.Elapsed.ToString(), "", SearchParam.SearchKey);
            }
            return options;

        }
        public async Task<PriceCheckWelcomebedsDto> welcomebedsPriceCheck(DataSet dsCartDetails, DataRow[] welcombedsDetails, int j, double FinalPrice, double CurrencyFactorwelcombeds, string UniqueNo, string AgentUniqueNo, string SupplierName, int i, double CompanyBuyingPrice, DataTable dtRoE, int AgentId, TokenData  tokenData,string Searchkey = "")
        {
            string request = "";
            string responsetype = "Success";
            string ResponseData = "";
            string time = "";
            string Error = "";
            PriceCheckWelcomebedsDto priceCheckWelcomebedsDto = new PriceCheckWelcomebedsDto();

            try
            {
                string username = Convert.ToString(welcombedsDetails[0]["UserName"]);
                string password = Convert.ToString(welcombedsDetails[0]["Password"]);
                string Url1 = Convert.ToString(welcombedsDetails[0]["WebSiteURL"]);
                string roomtypeid = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomTypeId"]);

                string BookUrl = _supplierConfig.Welcomebeds.Welcomebeds_PreBookingURL;
                string URL = Url1 + BookUrl;

                request = welcombedsPriceCheckRequest(roomtypeid);

                _commonMethod.ThirdPartyBookingHistory(Convert.ToString(UniqueNo), Convert.ToString(AgentUniqueNo), request, "-", "Before Block Request hit Price check", Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["SupplierName"]), "Request Confirm j=" + j + "", "-", "WithinEarth",Searchkey);

                System.Diagnostics.Stopwatch timerroomprocess = new System.Diagnostics.Stopwatch();
                timerroomprocess.Start();

                ResponseData = WBPriceCheckGetSupplierParallel(ref Error, request, URL, username, password, Searchkey, 120000);

                timerroomprocess.Stop();
                time = timerroomprocess.Elapsed.ToString();

                if (ResponseData == "" || ResponseData == null)
                {
                    try
                    {
                        responsetype = "GeneralError";
                        string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, request, ResponseData, Error);

                    }
                    catch (Exception e)
                    {
                        _ = _exception.Error(e, Searchkey);
                    }
                    //return 2;
                    priceCheckWelcomebedsDto.Flag = false;
                    return priceCheckWelcomebedsDto;
                }

                if (ResponseData == "" || ResponseData == null)
                {
                    try
                    {
                        responsetype = "GeneralError";
                        //int AgentId = Convert.ToInt32(Common.getSession("UserId") == "" ? "0" : Common.getSession("UserId"));
                        string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, request, ResponseData, Error);//"purchaseToken"
                    }
                    catch (Exception e)
                    {
                        _ = _exception.Error(e, Searchkey);
                    }
                    //return 2;
                    priceCheckWelcomebedsDto.Flag = false;
                    return priceCheckWelcomebedsDto;
                }

                _commonMethod.ThirdPartyBookingHistory(Convert.ToString(UniqueNo), Convert.ToString(AgentUniqueNo), request, ResponseData, "After Block Request hit Price check", Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["SupplierName"]), "Request Confirm j=" + j + "", "-", "WithinEarth",Searchkey);

                WelcomebedsClass.Welcomebeds.welcomebedsprebookresponse.Root DS = new WelcomebedsClass.Welcomebeds.welcomebedsprebookresponse.Root();
                try
                {
                    DS = JsonConvert.DeserializeObject<WelcomebedsClass.Welcomebeds.welcomebedsprebookresponse.Root>(ResponseData);
                }
                catch (Exception ex)
                {
                    FinalPrice = 0;
                    _ = _exception.Error(ex, Searchkey);
                }

                string BookToken = "";
                {
                    int adultcount = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[0]["NoOfAdults"]);
                    int childage1 = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[0]["ChildAge1"]);
                    int childage2 = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[0]["ChildAge2"]);
                    var NoOfChild = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[0]["NoOfChild"]);

                    string[] spitid1 = roomtypeid.Split(new string[] { "###" }, StringSplitOptions.None);


                    double Finalprice1 = Convert.ToDouble(DS.hotel.roomRates[0].pricing.sell.price);
                    string currency = Convert.ToString(DS.hotel.roomRates[0].rooms[0].pricing.currency);
                    BookToken = Convert.ToString(DS.hotel.roomRates[0].bookToken);

                    CurrencyFactorwelcombeds = 1;
                    if (currency.ToLower() != tokenData.SiteCurrency.ToLower())
                    {
                        CurrencyFactorwelcombeds = _commonMethod.GetROEByCurrency(currency, dtRoE);
                    }

                    FinalPrice = Finalprice1 * CurrencyFactorwelcombeds;
                    priceCheckWelcomebedsDto.FinalPrice = FinalPrice;

                    if (Math.Round(CompanyBuyingPrice, 2) < Math.Round(FinalPrice, 2))
                    {
                        try
                        {
                            responsetype = "GeneralError";
                            string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, request, ResponseData, Error);//"purchaseToken"
                        }
                        catch (Exception e)
                        {
                            _ = _exception.Error(e, Searchkey);
                        }
                        FinalPrice = Finalprice1;
                        //return 1;
                        priceCheckWelcomebedsDto.Flag = false;
                        return priceCheckWelcomebedsDto;
                    }
                }


                //string EssentionalInfoFinal = EssentionalInfo + "<br>" + EssentionalInfoNew;
                #region Para
                await _commonMethod.UpdateBookingv3Token(
                    Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["UserCartDetailId"]),
                    Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["UserCartId"]),
                    Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["SupplierName"]),
                    Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomTypeId"]), BookToken);
                #endregion
            }
            catch (Exception ex)
            {
                try
                {
                    FinalPrice = 0;
                    responsetype = "GeneralError";
                    string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, request, ResponseData, ex.Message.ToString());//"purchaseToken"
                }
                catch (Exception e)
                {
                    _ = _exception.Error(e, Searchkey);
                }
                _exception.ErrorLogAsync("", "");
                //return 2;
                priceCheckWelcomebedsDto.Flag = false;
                return priceCheckWelcomebedsDto;
            }
            try
            {
                responsetype = "Success";
                string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, request, ResponseData, Error); //"purchaseToken"
                priceCheckWelcomebedsDto.Flag = true;
                return priceCheckWelcomebedsDto;
            }
            catch (Exception ex)
            {
                _ = _exception.Error(ex, Searchkey);
            }

            return priceCheckWelcomebedsDto;
        }

        public string welcombedsPriceCheckRequest(string roomtypeid)
        {
            string RequestString = "";
            try
            {

                string EchoToken = Convert.ToString(Guid.NewGuid()).Substring(0, 13);
                string[] spitid1 = roomtypeid.Split(new string[] { "###" }, StringSplitOptions.None);
                string BookToken = spitid1[3];


                WelcomebedsClass.Welcomebeds.welcombedsprebook.Root main = new WelcomebedsClass.Welcomebeds.welcombedsprebook.Root();
                main.token = EchoToken;
                main.bookToken = BookToken;


                JsonSerializerSettings settings = new JsonSerializerSettings();
                settings.NullValueHandling = NullValueHandling.Ignore;
                RequestString = JsonConvert.SerializeObject(main, settings);
                return RequestString;
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
            }
            return RequestString;
        }
        public string WBPriceCheckGetSupplierParallel(ref string Error, string Request, string url, string UserName, string Password, string SearchKey, Int32 TimeOut = 120000)
        {
            string strOutput = "";
            System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
            timer.Start();
            try
            {

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                string Key = Convert.ToBase64String(Encoding.ASCII.GetBytes(UserName + ":" + Password));
                request.Method = "POST";
                request.Headers.Add("Authorization", "Basic " + Key);
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                request.ContentType = "application/json";
                request.Accept = "application/json";
                request.KeepAlive = true;

                ServicePointManager.DefaultConnectionLimit = 2000;

                byte[] postBytes = Encoding.ASCII.GetBytes(Request);
                using (Stream dataStream = request.GetRequestStream())
                {
                    dataStream.Write(postBytes, 0, postBytes.Length);
                }
                try
                {
                    HttpWebResponse Response = (HttpWebResponse)request.GetResponse();
                    Stream responseStream = Response.GetResponseStream();
                    StreamReader Reader = new StreamReader(responseStream, Encoding.UTF8);
                    strOutput = Reader.ReadToEnd();
                    responseStream.Close();
                    Reader.Close();
                    Response.Close();
                    timer.Stop();
                }
                catch (WebException ex)
                {
                    _ = _exception.Error(ex, SearchKey);


                    if (ex.Response != null)
                    {
                        WebResponse errorResponse = ex.Response;
                        using (Stream responseStream = errorResponse.GetResponseStream())
                        {
                            StreamReader reader = new StreamReader(responseStream, Encoding.GetEncoding("utf-8"));
                            string errorText = reader.ReadToEnd();
                            strOutput = errorText;
                            Error = strOutput;
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                _ = _exception.Error(ex, SearchKey);

                Error = ex.Message.ToString();
                return ex.Message.ToString();
            }
            return strOutput;
        }
        public string welcomebedsBookingRequest(DataSet dsCartDetails, DataRow[] WelcomebedsDetails, int i, int j, double resPrice, DataTable dtPassengers, ref string AgtRef, string CheckIn, string CheckOut, int UniqueNo, string BookToken, string AgentCode, string SearchKey = "")
        {
            string RequestString = "";
            try
            {
                string roomtypeid = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomTypeId"]);
                string[] spitid1 = roomtypeid.Split(new string[] { "###" }, StringSplitOptions.None);
                string RateId = spitid1[0];
                string HotelCode = spitid1[1];
                string Price = spitid1[2];
                string Bookingtoken = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["purchaseToken"]);
                int AdultAge = 30;

                string BookAgentCode = ("Book_" + AgentCode + "_" + UniqueNo);
                string remarks = "";

                StringBuilder bookingRQ = new StringBuilder();
                bookingRQ.Append("{");
                bookingRQ.Append("\"bookToken\":\"" + Bookingtoken + "\",");
                bookingRQ.Append("\"bookingReference\": {");
                bookingRQ.Append("\"requestReferenceID\":\"" + BookAgentCode + "\"");
                bookingRQ.Append("},");

                bookingRQ.Append("\"remarks\":\"" + remarks + "\",");

                bookingRQ.Append("\"travellers\": [");

                dtPassengers.DefaultView.RowFilter = "PickUp='" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[0]["RoomTypeId"]) + "' and UserCartID='" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[0]["UserCartID"]) + "' and ServiceType='Hotel'";

                dtPassengers.DefaultView.Sort = "IsLead desc";
                if (dtPassengers.DefaultView.Count > 0)
                {
                    for (int x = 0; x < dsCartDetails.Tables[1].DefaultView.Count; x++)

                    {
                        if (x == 0)
                            bookingRQ.Append("{");
                        else
                            bookingRQ.Append(",{");

                        dtPassengers.DefaultView.RowFilter = "PickUp='" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[0]["RoomTypeId"]) + "' and UserCartID='" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[0]["UserCartID"]) + "' and ServiceType='Hotel' and RoomNo='" + dsCartDetails.Tables[1].DefaultView[x]["RoomNumber"] + "'";
                        for (int k = 0; k < dtPassengers.DefaultView.Count; k++)
                        {
                            if (k != 0)
                                bookingRQ.Append(",{");


                            DataTable dtPax = dtPassengers.DefaultView.ToTable();
                            {
                                string LastName = Convert.ToString(dtPassengers.DefaultView[k]["LastName"]).Trim();
                                string FirstName = Convert.ToString(dtPassengers.DefaultView[k]["FirstName"]).Trim();
                                int ChildAge = Convert.ToInt32(dtPassengers.DefaultView[k]["ChildAge"]);
                                string RoomNo = Convert.ToString(dtPassengers.DefaultView[k]["RoomNo"]);


                                if (dtPax.Rows[k]["PaxType"].ToString() == "Adult")
                                    bookingRQ.Append("\"age\":\"" + AdultAge + "\",");
                                else
                                    bookingRQ.Append("\"age\":\"" + ChildAge + "\",");

                                bookingRQ.Append("\"index\":\"" + RoomNo + "\",");
                                bookingRQ.Append("\"person\": {");
                                bookingRQ.Append("\"lastName\":\"" + LastName + "\",");
                                bookingRQ.Append("\"name\":\"" + FirstName + "\"");
                                bookingRQ.Append("}");


                            }

                            bookingRQ.Append("}");
                        }
                    }

                }
                bookingRQ.Append("]");
                bookingRQ.Append("}");


                string BookingRQ = bookingRQ.ToString();
                return BookingRQ;



            }
            catch (Exception ex)
            {
                _ = _exception.Error(ex, SearchKey);

            }
            return RequestString;
        }
        public string WelcomebedsBookingResponse(ref string Error, string Request, string url, string UserName, string Password, string SearchKey = "", Int32 TimeOut = 120000)
        {

            string strOutput = "";
            System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
            timer.Start();
            try
            {

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                string Key = Convert.ToBase64String(Encoding.ASCII.GetBytes(UserName + ":" + Password));
                request.Method = "POST";
                request.Headers.Add("Authorization", "Basic " + Key);
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                request.ContentType = "application/json";
                request.Accept = "application/json";
                request.KeepAlive = true;
                request.Timeout = TimeOut;

                ServicePointManager.DefaultConnectionLimit = 2000;

                byte[] postBytes = Encoding.ASCII.GetBytes(Request);
                using (Stream dataStream = request.GetRequestStream())
                {
                    dataStream.Write(postBytes, 0, postBytes.Length);
                }
                try
                {
                    HttpWebResponse Response = (HttpWebResponse)request.GetResponse();
                    Stream responseStream = Response.GetResponseStream();
                    StreamReader Reader = new StreamReader(responseStream, Encoding.UTF8);
                    strOutput = Reader.ReadToEnd();
                    responseStream.Close();
                    Reader.Close();
                    Response.Close();
                    timer.Stop();
                }
                catch (WebException ex)
                {
                    _ = _exception.Error(ex, SearchKey);

                    if (ex.Response != null)
                    {
                        WebResponse errorResponse = ex.Response;
                        using (Stream responseStream = errorResponse.GetResponseStream())
                        {
                            StreamReader reader = new StreamReader(responseStream, Encoding.GetEncoding("utf-8"));
                            string errorText = reader.ReadToEnd();
                            strOutput = errorText;
                            Error = strOutput;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _ = _exception.Error(ex, SearchKey);
                Error = ex.Message.ToString();
                return ex.Message.ToString();
            }
            return strOutput;


        }
        public string Getwelcombeds_BookingCancelRQ(string AgentVoucherNo, DataRow[] WelcomebedsDetails, string requestReferenceID, string Searchkey = "")
        {
            string RequestString = "";
            try
            {


                string Token = Guid.NewGuid().ToString().Substring(0, 13);
                WelcomebedsClass.Welcomebeds.welcomebedscancelrequest.BookingReference objbooking = new WelcomebedsClass.Welcomebeds.welcomebedscancelrequest.BookingReference();
                objbooking.requestReferenceID = requestReferenceID;
                objbooking.bookingReferenceID = AgentVoucherNo;


                WelcomebedsClass.Welcomebeds.welcomebedscancelrequest.Root main = new WelcomebedsClass.Welcomebeds.welcomebedscancelrequest.Root();
                main.bookingReference = objbooking;
                main.token = Token;



                RequestString = JsonConvert.SerializeObject(main);
                return RequestString;
            }
            catch (Exception ex)
            {
                _ = _exception.Error(ex, Searchkey);
            }
            return RequestString;
        }
        public string GetwelcomebedsResponse(ref string Error, string Request, string url, string UserName, string Password, int Timeout = 120000)
        {
            string strOutput = "";
            System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
            timer.Start();
            try
            {

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                string Key = Convert.ToBase64String(Encoding.ASCII.GetBytes(UserName + ":" + Password));
                request.Method = "POST";
                request.Headers.Add("Authorization", "Basic " + Key);
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                request.ContentType = "application/json";
                request.Accept = "application/json";
                request.KeepAlive = true;
                request.Timeout = Timeout;

                ServicePointManager.DefaultConnectionLimit = 2000;

                byte[] postBytes = Encoding.ASCII.GetBytes(Request);
                using (Stream dataStream = request.GetRequestStream())
                {
                    dataStream.Write(postBytes, 0, postBytes.Length);
                }
                try
                {
                    HttpWebResponse Response = (HttpWebResponse)request.GetResponse();
                    Stream responseStream = Response.GetResponseStream();
                    StreamReader Reader = new StreamReader(responseStream, Encoding.UTF8);
                    strOutput = Reader.ReadToEnd();
                    responseStream.Close();
                    Reader.Close();
                    Response.Close();
                    timer.Stop();
                }
                catch (WebException ex)
                {
                    _exception.Error(ex);

                    if (ex.Response != null)
                    {
                        WebResponse errorResponse = ex.Response;
                        using (Stream responseStream = errorResponse.GetResponseStream())
                        {
                            StreamReader reader = new StreamReader(responseStream, Encoding.GetEncoding("utf-8"));
                            string errorText = reader.ReadToEnd();
                            strOutput = errorText;
                            Error = strOutput;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
                Error = ex.Message.ToString();
                return ex.Message.ToString();
            }
            return strOutput;
        }
        public async Task<string> Welcomebeds_EssentialInfoOnly(string Url1, string password, string username, string roomtypeid, string CheckIn, string CheckOut, string SearchKey = "")
        {
            string EssentionalInfoNew = "";

            try
            {

                string ResponseData = "";
                string Error = "";
                string time = "";


                CheckIn = CheckIn.Substring(0, 4) + "-" + CheckIn.Substring(4, 2) + "-" + CheckIn.Substring(6, 2);
                CheckOut = CheckOut.Substring(0, 4) + "-" + CheckOut.Substring(4, 2) + "-" + CheckOut.Substring(6, 2);
                string BookUrl = _supplierConfig.Welcomebeds.Welcomebeds_PreBookingURL;

                string URL = Url1 + BookUrl;

                string request = welcombedsPriceCheckRequest(roomtypeid);
                System.Diagnostics.Stopwatch timerroomprocess = new System.Diagnostics.Stopwatch();
                timerroomprocess.Start();

                ResponseData = WBPriceCheckGetSupplierParallel(ref Error, request, URL, username, password, SearchKey, 120000);
                //ResponseData = WBPriceCheckGetSupplierParallel(request, URL, ref Error, username, password, 180000, "welcomebeds");

                timerroomprocess.Stop();
                time = timerroomprocess.Elapsed.ToString();
                WelcomebedsClass.Welcomebeds.welcomebedsprebookresponse.Root DS = new WelcomebedsClass.Welcomebeds.welcomebedsprebookresponse.Root();
                try
                {
                    DS = JsonConvert.DeserializeObject<WelcomebedsClass.Welcomebeds.welcomebedsprebookresponse.Root>(ResponseData);
                }

                catch (Exception ex)
                {
                    _exception.Error(ex);
                }
                string EssentionalInfo = "";
                if (DS.hotel.roomRates[0].observations != null)
                {
                    for (int E = 0; E < DS.hotel.roomRates[0].observations.Count(); E++)
                    {
                        if (E == 0)
                        {
                            EssentionalInfo = DS.hotel.roomRates[0].observations[E].txt;
                        }
                        else
                        {
                            string EssentionalInfo1 = DS.hotel.roomRates[0].observations[E].txt;
                            EssentionalInfo = EssentionalInfo + "<br>" + EssentionalInfo1;
                        }


                    }
                }
                if (DS.hotel.hotelDetails.annotations.observations != null)
                {
                    for (int S = 0; S < DS.hotel.hotelDetails.annotations.observations.Count(); S++)
                    {
                        if (S == 0)
                        {
                            EssentionalInfoNew = DS.hotel.hotelDetails.annotations.observations[S].txt;
                        }
                        else
                        {
                            string EssentionalInfo1 = DS.hotel.hotelDetails.annotations.observations[S].txt;
                            EssentionalInfoNew = EssentionalInfoNew + "<br>" + EssentionalInfo1;
                        }


                    }
                }
                string EssentionalInfoFinal = EssentionalInfo + "<br>" + EssentionalInfoNew;
            }
            catch (Exception e)
            {
                _exception.Error(e);
            }
            return EssentionalInfoNew;
        }
    }
}
