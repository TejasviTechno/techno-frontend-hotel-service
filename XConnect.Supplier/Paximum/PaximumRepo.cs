using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XConnect.CommonUtilities.Interface;
using XConnect.CommonUtilities;
using XConnect.ErrorLogHandler.Interface;
using XConnect.Markup.Interface;
using XConnect.Models.Hotel;
using XConnect.Models;
using XConnect.Supplier.Paximum.Interface;
using Newtonsoft.Json;
using Microsoft.Extensions.Options;
using Options = XConnect.Models.Options;
using static Azure.Core.HttpHeader;
using System.Net;

namespace XConnect.Supplier.Paximum
{
    public class PaximumRepo : IPaximum
    {
        private readonly ICommonMethod _commonMethod;
        private readonly IException _exception;
        private readonly IMarkup _markup;
        private readonly ISupplierRes _supplierRes;
        private readonly KeySettings _keySettings;


        public PaximumRepo(ICommonMethod commonMethod, IMarkup markup, ISupplierRes supplierRes, IOptions<KeySettings> keySettings)
        {
            _commonMethod = commonMethod;
            _exception = null;
            _markup = markup;
            _supplierRes = supplierRes;
            _keySettings = keySettings.Value;
        }


        public void GetpaximumRequestOnly_Async(DataRow[] commonCode, List<StarRating> listRating, DataTable dtPreferredDataHotel, HotelSearchParameter SearchPara, List<SupplierSearchConfig> lstPaximumSearchConfig, List<SupplierDetails> lstPaximumDetails, ref ConcurrentBag<HotelX> AllTask, string AgentCode, DataTable dtROE, TokenData tokenData, DataSet dtXMLCity)
        {
            try
            {
                if (lstPaximumDetails != null && lstPaximumDetails.Count > 0)
                {
                 
                    List<StarRating> lstRating = new List<StarRating>();
                    lstRating = _commonMethod.GetStarRatingStringList_xtg(SearchPara);

                    List<HotelCodeByCity> DtHotelDAta = HtmlExtensions.ToList<HotelCodeByCity>(commonCode.CopyToDataTable());
                    int ratingcount = lstRating.Count;
                    int hotelcount = DtHotelDAta.Count;

                 

                    foreach (SupplierDetails PaximumDetails in lstPaximumDetails)
                    {
                        int SupplierID = PaximumDetails.SupplierID;
                        string Supplier = PaximumDetails.SupplierName;


                        // if (commonCode != null)
                        {
                            if (lstPaximumSearchConfig != null && lstPaximumSearchConfig.Count > 0)
                            {
                                List<SupplierSearchConfig> PaximumSearchConfig = lstPaximumSearchConfig
                                    .Where(x => x != null && x.SupplierID == SupplierID)
                                    .ToList();

                                if (PaximumSearchConfig != null && PaximumSearchConfig.Count > 0)
                                {
                                    DataForSearch dataForSearch = _commonMethod.GetXmlDataForSearchTechwise(SearchPara.Nationality, dtXMLCity, Supplier, PaximumDetails.TechName);

                                    if (dataForSearch.XmlNationality != "")
                                    {
                                        string Ids = string.Empty;
                                        string xmlbase = "";
                                        _commonMethod.GetPrefferedHotelCodes(ref Ids, ref xmlbase, ref PaximumSearchConfig, Supplier, dtPreferredDataHotel, SearchPara.IsSearchFor);

                                        List<HotelCodeByCity> filter = new List<HotelCodeByCity>();
                                        filter = _commonMethod.FilterWithOutCity_Code(ratingcount, hotelcount, DtHotelDAta, lstRating, Supplier);
                                       
                                        if (PaximumSearchConfig != null)
                                        {
                                            double CurrencyFactorPaximum = 1;

                                            string Paximun_currency = Convert.ToString(PaximumDetails.CurrencyShortName);
                                            string Sitecurrency = tokenData.SiteCurrency;
                                            if (Paximun_currency.ToLower() != Sitecurrency.ToLower()) // change systembase currency 
                                            {
                                                CurrencyFactorPaximum = _commonMethod.GetROEByCurrency(Paximun_currency, dtROE);
                                            }

                                            for (int k = 0; k < PaximumSearchConfig.Count; k++)
                                            {
                                                bool DebugMode = (Convert.ToInt32(PaximumSearchConfig[k].DebugMode) == 0 ? false : true);
                                                string UserName = Convert.ToString(PaximumDetails.UserName);
                                                string Password = Convert.ToString(PaximumDetails.Password);
                                                {
                                                    int IsCitywisesearch = Convert.ToInt32(PaximumSearchConfig[k].City);
                                                    if (filter.Count > 0)
                                                    {
                                                        if (Convert.ToInt32(PaximumSearchConfig[k].CityID) == 0 || Convert.ToInt32(PaximumSearchConfig[k].CityID) == SearchPara.CityID)
                                                        {
                                                            int Noofrecords = Convert.ToInt32(PaximumSearchConfig[k].Noofrecords);
                                                            double count = Convert.ToDouble(filter.Count);
                                                            int NoOfDiv = Convert.ToInt32(Math.Ceiling(count / Convert.ToDouble(Noofrecords)));
                                                            List<List<XMLID_Search>> lstxml = _commonMethod.GetXMLIDString(NoOfDiv, Noofrecords, count, filter);

                                                            if (SearchPara.Filters.IsShowRooms == 0)
                                                            {
                                                                for (int a = 0; a < NoOfDiv; a++)
                                                                {
                                                                    string Uniqueid = Guid.NewGuid().ToString().Substring(0, 8);
                                                                    string IdsForSearch = Convert.ToString(_commonMethod.GetHotelCodeByCOmma(lstxml[a]));

                                                                    HotelX Xhotel = new HotelX();
                                                                    Xhotel.Supplier = Supplier;
                                                                    Xhotel.DebugeMode = DebugMode;
                                                                    Xhotel.SearchTimeOut = Convert.ToInt32(PaximumSearchConfig[k].HotelSearchTimeOut);
                                                                    //if (Hit > 0)
                                                                    Xhotel.ProcessTimeout = Convert.ToInt32(PaximumSearchConfig[k].HotelSearchTimeOut2);
                                                                    if (_keySettings.SupplierProxy == "0")
                                                                        Xhotel.URL = Convert.ToString(PaximumDetails.WebSiteURL) + "search/hotels";
                                                                    else
                                                                        Xhotel.URL = Convert.ToString(PaximumDetails.WebSiteURLforAPI) + "search/hotels";

                                                                    Xhotel.UserId = Convert.ToString(PaximumDetails.UserID);
                                                                    Xhotel.UserName = UserName;
                                                                    Xhotel.PassWord = Password;
                                                                    Xhotel.CurrencyCode = Paximun_currency;
                                                                    Xhotel.CurrencyFactor = CurrencyFactorPaximum;
                                                                    Xhotel.XmlCity = dataForSearch.xmlCity;
                                                                    Xhotel.XmlCountry = "";
                                                                    Xhotel.XmlNationality = dataForSearch.XmlNationality;
                                                                    Xhotel.Action = Uniqueid;


                                                                    #region "CoomonCode"
                                                                    bool IsHitSeperate = _commonMethod.IsSuppliersWithSeperateRoomRquest(Xhotel.Supplier);
                                                                    int NoOfRoomsSearch = 1;
                                                                    if (IsHitSeperate)
                                                                        NoOfRoomsSearch = SearchPara.Rooms.Count;
                                                                    List<RQRS> RQRS = new List<RQRS>();
                                                                    Xhotel.IsSeperate = IsHitSeperate;
                                                                    for (int rr = 0; rr < NoOfRoomsSearch; rr++)
                                                                    {
                                                                        RQRS objrq = new RQRS();
                                                                        List<RoomDetails> serchRooms = new List<RoomDetails>();
                                                                        serchRooms.Add(SearchPara.Rooms[rr]);
                                                                        objrq.Request = XMLpaximumRequestOnly_Async(SearchPara, dataForSearch.xmlCity, dataForSearch.xmlCountry, Xhotel.URL, Xhotel.UserName, Xhotel.PassWord, Xhotel.XmlNationality, Xhotel.SearchTimeOut, Convert.ToString(Paximun_currency), IdsForSearch);

                                                                        objrq.Response = "";
                                                                        RQRS.Add(objrq);
                                                                    }
                                                                    Xhotel.RQRS = RQRS;
                                                                    Xhotel.ReturnRooms = PaximumDetails.ReturnRooms;
                                                                    Xhotel.TechName = PaximumDetails.TechName;
                                                                    #endregion
                                                                    AllTask.Add(Xhotel);
                                                                }
                                                            }
                                                            else  // && count == 1)
                                                            {

                                                                for (int a = 0; a < NoOfDiv; a++)
                                                                {
                                                                    string Uniqueid = Guid.NewGuid().ToString().Substring(0, 8);
                                                                    string IdsForSearch = Convert.ToString(_commonMethod.GetHotelCodeByCOmma(lstxml[a]));
                                                                    HotelX Xhotel = new HotelX();
                                                                    Xhotel.Supplier = Supplier;
                                                                    Xhotel.DebugeMode = DebugMode;
                                                                    Xhotel.SearchTimeOut = Convert.ToInt32(PaximumSearchConfig[k].HotelSearchTimeOut);
                                                                    //if (Hit > 0)
                                                                    Xhotel.ProcessTimeout = Convert.ToInt32(PaximumSearchConfig[k].HotelSearchTimeOut2);
                                                                    if (_keySettings.SupplierProxy == "0")
                                                                        Xhotel.URL = Convert.ToString(PaximumDetails.WebSiteURL) + "search/hotels";
                                                                    else
                                                                        Xhotel.URL = Convert.ToString(PaximumDetails.WebSiteURLforAPI) + "search/hotels";

                                                                    Xhotel.UserId = Convert.ToString(PaximumDetails.UserID);
                                                                    Xhotel.UserName = UserName;
                                                                    Xhotel.PassWord = Password;
                                                                    Xhotel.CurrencyCode = Paximun_currency;
                                                                    Xhotel.CurrencyFactor = CurrencyFactorPaximum;
                                                                    Xhotel.XmlCity = dataForSearch.xmlCity;
                                                                    Xhotel.XmlCountry = "";
                                                                    Xhotel.XmlNationality = dataForSearch.XmlNationality;
                                                                    Xhotel.Action = Uniqueid;


                                                                    #region "CoomonCode"
                                                                    bool IsHitSeperate = _commonMethod.IsSuppliersWithSeperateRoomRquest(Xhotel.Supplier);
                                                                    int NoOfRoomsSearch = 1;
                                                                    if (IsHitSeperate)
                                                                        NoOfRoomsSearch = SearchPara.Rooms.Count;
                                                                    List<RQRS> RQRS = new List<RQRS>();
                                                                    Xhotel.IsSeperate = IsHitSeperate;
                                                                    for (int rr = 0; rr < NoOfRoomsSearch; rr++)
                                                                    {
                                                                        RQRS objrq = new RQRS();
                                                                        List<RoomDetails> serchRooms = new List<RoomDetails>();
                                                                        serchRooms.Add(SearchPara.Rooms[rr]);
                                                                        objrq.Request = XMLpaximumRequestOnly_Async(SearchPara, dataForSearch.xmlCity, dataForSearch.xmlCountry, Xhotel.URL, Xhotel.UserName, Xhotel.PassWord, Xhotel.XmlNationality, Xhotel.SearchTimeOut, Convert.ToString(Paximun_currency), IdsForSearch);
                                                                        objrq.Response = "";
                                                                        RQRS.Add(objrq);
                                                                    }
                                                                    Xhotel.RQRS = RQRS;
                                                                    Xhotel.ReturnRooms = PaximumDetails.ReturnRooms;
                                                                    Xhotel.TechName = PaximumDetails.TechName;
                                                                    #endregion
                                                                    AllTask.Add(Xhotel);
                                                                }
                                                            }

                                                        }
                                                    }
                                                }

                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
            }
        }
        public string XMLpaximumRequestOnly_Async(HotelSearchParameter SearchPara, string CityCode, string CountryCode, string paximum_URL, string paximum_User_Name, string paximum_Password, string PaximumNationality, Int32 TimeOut, string paximumCurrency, string hotelId)
        {
            try
            {

                string checkIn = System.Convert.ToString(SearchPara.CheckInDate);
                string checkout = Convert.ToString(SearchPara.CheckOutDate);
                int nights = SearchPara.NoofNights;
                checkIn = checkIn.Substring(6, 4) + "-" + checkIn.Substring(3, 2) + "-" + checkIn.Substring(0, 2);
                checkout = checkout.Substring(6, 4) + "-" + checkout.Substring(3, 2) + "-" + checkout.Substring(0, 2);
                int TotalNoofAdults = 0;
                int NoofAdults = 0;
                int NoofChild = 0;
                try
                {

                    #region Req 
                    StringBuilder PRQ = new StringBuilder();

                    PRQ.Append("{");
                    PRQ.Append("\"destinations\":");
                    PRQ.Append("[");

                    if (hotelId.Trim() != string.Empty)
                    {
                        string[] hid = hotelId.Split(',');
                        for (int h = 0; h < hid.Length; h++)
                        {
                            PRQ.Append("{");
                            PRQ.Append("\"type\":\"hotel\",");
                            PRQ.Append("\"id\":\"" + hid[h] + "\"");
                            PRQ.Append("},");
                        }
                        PRQ.Remove(PRQ.Length - 1, 1);

                    }

                    else if (CityCode.Trim() != string.Empty)
                    {

                        PRQ.Append("\"type\":\" city \",");
                        PRQ.Append("\"id\":\"" + CityCode + "\" ");

                    }
                    PRQ.Append("],");
                    PRQ.Append("\"rooms\":");
                    PRQ.Append("[");

                    #region adults child count



                    #endregion
                    #region adults child count
                    StringBuilder guest = new StringBuilder();

                    for (int ser = 0; ser < SearchPara.Rooms.Count; ser++)
                    {
                        NoofAdults = SearchPara.Rooms[ser].NoofAdults;
                        if (ser == 0)
                            guest.Append("{");
                        else
                            guest.Append(",{");
                        NoofChild = SearchPara.Rooms[ser].NoofChild;
                        //for (int i = 0; i < SearchPara.Rooms[ser].NoofChild; i++)
                        //{
                        //    NoofChild = SearchPara.Rooms[ser].NoofChild;

                        //}
                        TotalNoofAdults = NoofAdults + NoofChild;

                        guest.Append("\"adults\":" + TotalNoofAdults + " ");
                        guest.Append("}");
                    }
                    PRQ.Append(guest);
                    PRQ.Append("],");

                    PRQ.Append("\"checkinDate\":\"" + checkIn + "\",");
                    PRQ.Append("\"checkoutDate\":\"" + checkout + "\",");
                    PRQ.Append("\"currency\":\"" + paximumCurrency + "\",");
                    PRQ.Append("\"customerNationality\":\"" + PaximumNationality + "\",");
                    PRQ.Append("\"filterUnavailable\":" + "false" + ",");
                    //PRQ.Append("\"withPromotion\":" + "false" + ",");
                    PRQ.Append("\"withPromotion\":" + "true" + ",");//changes on 22-sep-2023 as per mustafa Sir(k2) no ticket given
                    PRQ.Append("\"language\":\"en\"");
                    PRQ.Append("}");
                    string paximumstring = PRQ.ToString();
                    return paximumstring;
                    #endregion
                    #endregion
                }
                catch (Exception ex)
                {
                    _exception.Error(ex);
                }


            }
            catch (Exception ex)
            {
                _exception.Error(ex);

            }
            return "";

        }
        public async Task<List<Options>> Paximum_HotelSearchWithRooms(HotelX Item, CommonSearch commonSearch, string Response, string CityCode, HotelSearchParameter SearchParam, double CurrencyFactorpaximum, string Paximum_NationalityCode, string HotelID, string ProccessCutOffTime, Int32 processTimeout, string ResProccessTimerLogActive, string ResProccessTimeValueforSaveLog, DataTable dtROE, string StoreSupplierResErrorLog, DataSet dsAgentDetails)
        {
            System.Diagnostics.Stopwatch timer_room_Paximum = new System.Diagnostics.Stopwatch();
            timer_room_Paximum.Start();
            List<Options> options = new List<Options>();
            try
            {
                #region DataTable
                DataTable DtHotel = _commonMethod.GenerateDataTableNewV2();
                #endregion
                // Paximum.Root paximumResponse = null;
                if (Response == null || Response == "")
                {
                    Item.ResponseType = "NoCount";
                    return options;
                }
                Paximum.Root paximumResponse = null;
                try
                {
                    paximumResponse = JsonConvert.DeserializeObject<Paximum.Root>(Response);
                }
                catch (Exception ex)
                {
                    await _exception.Error(ex);
                }

                //if (DS.results.Count < 1)
                if (paximumResponse.hotels.Count < 1)
                {
                    Item.ResponseType = "NoCount";
                    return options;
                }
                if (Response != string.Empty)
                {
                    string Provider = Item.Supplier;
                    for (int h = 0; h < paximumResponse.hotels.Count; h++)
                    {
                        Item.ResponseType = "Success";
                        //Check Time If High then return NoCount
                        if (ProccessCutOffTime == "1" && timer_room_Paximum.ElapsedMilliseconds > processTimeout)
                        {
                            timer_room_Paximum.Stop();
                            break;
                        }
                        //--end
                        string ItemCode = Convert.ToString(paximumResponse.hotels[h].id);
                        #region "When not Need Room"  
                        // add hotel code from price only and room code blank here
                        if (SearchParam.Filters.IsShowRooms == 0)
                        {
                            #region "Hotel Code Block "
                            double NetPrice = 0;
                            int off = 0;
                            double TotalCharges = 0;
                            {
                                DataRow drXmlHotel = DtHotel.NewRow();
                                string InternalHotelId = Convert.ToString(ItemCode);
                                drXmlHotel["XMLHotelCode"] = ItemCode;
                                drXmlHotel["HotelName"] = "";
                                drXmlHotel["SupplierName"] = Provider;
                                drXmlHotel["OfficeID"] = 0;
                                drXmlHotel["ContractID"] = 0;
                                drXmlHotel["Rating"] = 0;
                                drXmlHotel["Reviews"] = 0;
                                drXmlHotel["UniqueNo"] = ItemCode;
                                drXmlHotel["IsShowSupplier"] = false;
                                drXmlHotel["BookingStatus"] = "Available";
                                drXmlHotel["XMLCity"] = CityCode;
                                //drXmlHotel["SupplierCurrency"] = paximumResponse.hotels[h].offers[off].price.currency;
                                string currency = paximumResponse.hotels[h].offers[off].price.currency;
                                drXmlHotel["SupplierCurrency"] = currency;

                                double Pricenew = Convert.ToDouble(paximumResponse.hotels[h].offers[off].price.amount) * CurrencyFactorpaximum;
                                if (off == 0)
                                {
                                    NetPrice = Pricenew;
                                }
                                else if (NetPrice > Pricenew)
                                {
                                    NetPrice = Pricenew;
                                }
                                drXmlHotel["FinalSellingPrice"] = NetPrice;
                                drXmlHotel["SellingPrice"] = NetPrice;
                                if (Pricenew == 0)
                                {
                                    Item.ResponseType = "NoCount";
                                    continue;
                                }

                                TotalCharges = NetPrice;
                                DtHotel.Rows.Add(drXmlHotel);
                                #endregion
                                if (TotalCharges > 0)
                                {
                                    #region "Room code Block"
                                    bool willAddOrNot = true;
                                    Int64 RaynaID = 0;
                                    Int64 GiataId = 0;
                                    int Rating = 0;
                                    Int64 OwnsystemHotelId = 0;
                                    double MinPrice = TotalCharges;
                                    _markup.GetHotelDataCommon(ref MinPrice, ref willAddOrNot, ref OwnsystemHotelId, ref RaynaID, ref GiataId, ref Rating, ref drXmlHotel, commonSearch.AgentId, commonSearch.TariffID, commonSearch.dsAgentDetails, commonSearch.supplierGiataMaster, SearchParam, commonSearch.dtCountryCityMarkup, commonSearch.B2CMarkup, commonSearch.IsParentId, commonSearch.SubAgentMarkup, commonSearch.SearchByHotel, commonSearch.HotelIDS);
                                    if (willAddOrNot)
                                    {
                                        #region "Room Logic"
                                        DataTable dt_HotelRoom = _commonMethod.GenerateRoomtypeDataTableNewV2();
                                        DataTable dt_Cancellation = _commonMethod.GenerateCancellationDataTableNewV2();
                                        DataTable dt_RateBreackUp = _commonMethod.GenerateoomRateBreackUpDataTableNewV2();
                                        #endregion
                                        decimal LowestPrice = Convert.ToDecimal(MinPrice).CalculatePrice(commonSearch.tokenData, commonSearch.advancedOptions, currency);
                                        Options ops = (await _supplierRes.GetCommonRoomForSuppliers(GiataId, commonSearch.tokenData, commonSearch.advancedOptions, Convert.ToDouble(LowestPrice), drXmlHotel, dt_HotelRoom, dt_Cancellation, dt_RateBreackUp, SearchParam));
                                        if (ops != null)
                                            options.Add(ops);
                                    }
                                    #endregion
                                }
                            }
                        }
                        #endregion
                        #region "When Need Rooms"
                        //add Hotel code blank and rooms code in room block
                        else
                        {
                            double FinalMinPrice = 0;
                            int off = 0;
                            #region Hotels
                            #region "Hotel Block here"
                            double TotalCharges = 1;
                            DataRow drXmlHotel = DtHotel.NewRow();
                            string InternalHotelId = Convert.ToString(ItemCode);
                            drXmlHotel["XMLHotelCode"] = ItemCode;
                            drXmlHotel["HotelName"] = "";
                            drXmlHotel["SupplierName"] = Provider;
                            drXmlHotel["OfficeID"] = 0;
                            drXmlHotel["ContractID"] = 0;
                            drXmlHotel["Rating"] = 0;
                            drXmlHotel["Reviews"] = 0;
                            drXmlHotel["UniqueNo"] = ItemCode;
                            drXmlHotel["IsShowSupplier"] = false;
                            drXmlHotel["BookingStatus"] = "Available";
                            drXmlHotel["XMLCity"] = CityCode;
                            string currency = paximumResponse.hotels[h].offers[off].price.currency;
                            drXmlHotel["SupplierCurrency"] = currency;
                            //drXmlHotel["SupplierCurrency"] = currency;
                            drXmlHotel["FinalSellingPrice"] = TotalCharges;
                            drXmlHotel["SellingPrice"] = TotalCharges;


                            DtHotel.Rows.Add(drXmlHotel);
                            FinalMinPrice = TotalCharges;
                            #endregion
                            #region "GetRooms for this hotel"
                            bool willAddOrNot = true;
                            Int64 RaynaID = 0;
                            Int64 GiataId = 0;
                            int RatingHotel = 0;
                            Int64 OwnsystemHotelId = 0;
                            double MinPrice = 1;

                            _markup.GetHotelDataCommon(ref MinPrice, ref willAddOrNot, ref OwnsystemHotelId, ref RaynaID, ref GiataId, ref RatingHotel, ref drXmlHotel, commonSearch.AgentId, commonSearch.TariffID, commonSearch.dsAgentDetails, commonSearch.supplierGiataMaster, SearchParam, commonSearch.dtCountryCityMarkup, commonSearch.B2CMarkup, commonSearch.IsParentId, commonSearch.SubAgentMarkup, commonSearch.SearchByHotel, commonSearch.HotelIDS);
                            if (willAddOrNot)
                            {
                                DataTable dtHotelRoom = _commonMethod.GenerateRoomtypeDataTableNewV2();
                                DataTable dtCancellation = _commonMethod.GenerateCancellationDataTableNewV2();
                                DataTable DtRoomRateBreackUp = _commonMethod.GenerateoomRateBreackUpDataTableNewV2();

                                int CutOffDays = CacheMaster.CutOffDays;
                                CutOffDays = _commonMethod.GetCutOffDays(commonSearch.dtSuplierMarkup, Item.Supplier);

                                //int CutOffDays = XMLHotelBookingCommon.CutOffDays;
                                // CutOffDays = Common.GetCutOffDays(commonSearch.dtSuplierMarkup, Provider);
                                #region "Room Logic"
                                //DataTable XMLTBORoomOnly = Common.GenerateRoomtypeDataTableNewV2();

                                if (SearchParam.Filters.IsShowRooms == 1)
                                {
                                    #region Data
                                    double AgentPercentage = 0.0;
                                    double AgentMarkupAmount = 0.0;
                                    string nationality = PaximumInfo.PaximumNationality;
                                    string RoomType = "";
                                    string FinalRoomtype = "";
                                    int roomNo = 0;
                                    //int off = 0;
                                    Boolean isPerce = true;
                                    bool blnIsShowSupplier = false;
                                    _markup.getAgetMarkupSetting(Provider, GiataId.ToString(), commonSearch.dtSuplierMarkup, commonSearch.dtMarkupDetails, ref AgentPercentage, ref AgentMarkupAmount, ref blnIsShowSupplier, ref isPerce, commonSearch.AgentId, RatingHotel, SearchParam.CityID, SearchParam.CountryID.ToString(), SearchParam.CheckInDate);
                                    string Description = Convert.ToString(paximumResponse.hotels[h].description);

                                    if (paximumResponse.hotels[h].offers.Count > 0)
                                    {
                                        for (int i = 0; i < paximumResponse.hotels[h].offers.Count; i++)
                                        {
                                            if (commonSearch.AgentId == 4440 || commonSearch.AgentId == 4901)//For API
                                            {
                                                if (i > CacheMaster.RoomLimit_20)
                                                    break;
                                            }
                                            else
                                            {
                                                if (i > CacheMaster.RoomLimit_10)
                                                    break;
                                            }
                                            string OfferId = Convert.ToString(paximumResponse.hotels[h].offers[i].id);
                                            int BoardId = Convert.ToInt32(paximumResponse.hotels[h].offers[i].boardId);
                                            string BoardName = Convert.ToString(paximumResponse.hotels[h].offers[i].board);
                                            string Board = Convert.ToString(paximumResponse.hotels[h].offers[i].boardCategories);
                                            //List<string> l = new List<string>(paximumResponse.hotels[h].offers[i].boardCategories);
                                            //string BoardName = string.Join(" ", l.ToArray());

                                            if (Board != null)
                                            {
                                                List<string> l = new List<string>(paximumResponse.hotels[h].offers[i].boardCategories);
                                                BoardName = string.Join(" ", l.ToArray());
                                            }
                                            else
                                            {
                                                BoardName = "Room only";
                                            }
                                            //string RoomName = Convert.ToString(paximumResponse.hotels[i].rooms[roomNo].name);
                                            //string RoomId = Convert.ToString(paximumResponse.hotels[i].rooms[roomNo].id);
                                            string RoomId = "";
                                            //object Occupancy = paximumResponse.hotels[h].offers[i].boardCategories;
                                            //JToken[] str1 = Newtonsoft.Json.Linq.JToken.Parse(Board.ToString()).ToArray();


                                            if (paximumResponse.hotels[h].offers[i].rooms.Count > 0)
                                            {
                                                for (int j = 0; j < paximumResponse.hotels[h].offers[i].rooms.Count; j++)
                                                {
                                                    string roomtypeid = "";
                                                    if (j == 0)
                                                    {
                                                        roomtypeid = Convert.ToString(paximumResponse.hotels[h].offers[i].rooms[j].id);
                                                        RoomId = roomtypeid;
                                                    }
                                                    else
                                                    {
                                                        roomtypeid = Convert.ToString(paximumResponse.hotels[h].offers[i].rooms[j].id);
                                                        RoomId = RoomId + "$$" + roomtypeid;
                                                    }

                                                    int TypeId = Convert.ToInt32(paximumResponse.hotels[h].offers[i].rooms[j].typeId);
                                                    RoomType = Convert.ToString(paximumResponse.hotels[h].offers[i].rooms[j].type);
                                                    if (j == 0)
                                                        FinalRoomtype = "[1]" + RoomType;
                                                    else
                                                        FinalRoomtype = FinalRoomtype + "[" + (j + 1) + "]" + RoomType;

                                                    double price = Convert.ToDouble(paximumResponse.hotels[h].offers[i].rooms[j].price.amount);
                                                    //string currency = DS.hotels[h].offers[off].price.currency;
                                                    currency = Convert.ToString(paximumResponse.hotels[h].offers[i].rooms[j].price.currency);
                                                    if (paximumResponse.hotels[h].offers[i].rooms[j].travellers.Count > 0)
                                                    {
                                                        for (int k = 0; k < paximumResponse.hotels[h].offers[i].rooms[j].travellers.Count; k++)
                                                        {
                                                            string typr_traveller = Convert.ToString(paximumResponse.hotels[h].offers[i].rooms[j].travellers[k].type);
                                                            int Age_traveller = Convert.ToInt32(paximumResponse.hotels[h].offers[i].rooms[j].travellers[k].age);
                                                            nationality = Convert.ToString(paximumResponse.hotels[h].offers[i].rooms[j].travellers[k].nationality);

                                                        }
                                                    }
                                                }
                                                double TotalPrice = Convert.ToDouble(paximumResponse.hotels[h].offers[i].price.amount);
                                                string Currency_pax = Convert.ToString(paximumResponse.hotels[h].offers[i].price.currency);

                                                TotalPrice = TotalPrice * CurrencyFactorpaximum;

                                                string RoomTypeId = OfferId + "###" + RoomId + "###" + TotalPrice + "###" + nationality;

                                                #region Ratebreakup
                                                //for (int n = 0; n < SearchParam.NoofNights; n++)
                                                //{
                                                //    string CheckInDate = Convert.ToString(SearchParam.CheckInDate);
                                                //    string CheckOutDate = Convert.ToString(SearchParam.CheckOutDate);
                                                //    DateTime dtCheckInDate = DateTime.ParseExact(CheckInDate, "dd/MM/yyyy", null);
                                                //    DateTime dtCheckOutDate = DateTime.ParseExact(CheckOutDate, "dd/MM/yyyy", null);
                                                //    TimeSpan t = dtCheckOutDate - dtCheckInDate;
                                                //    double ratePerNight = TotalPrice;

                                                //    #region Vat
                                                //    double VatOnPerNight = 0;
                                                //    ratePerNight = _markup.ProcessHotelRateBreakupPrice_WithMarkup(1, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), ratePerNight, commonSearch.B2CMarkup, commonSearch.IsParentId, isPerce, AgentPercentage, AgentMarkupAmount, commonSearch.SubAgentMarkup, ref VatOnPerNight);
                                                //    #endregion

                                                //    ratePerNight = ratePerNight / SearchParam.NoofNights;
                                                //    DataRow drRateBreackUp = DtRoomRateBreackUp.NewRow();
                                                //    drRateBreackUp["SupplierName"] = Provider;
                                                //    drRateBreackUp["OwnsystemHotelId"] = OwnsystemHotelId;
                                                //    drRateBreackUp["ContractID"] = 0;
                                                //    drRateBreackUp["RoomNo"] = Convert.ToString(1);
                                                //    drRateBreackUp["RoomTypeId"] = RoomTypeId;
                                                //    drRateBreackUp["MealId"] = BoardId;
                                                //    drRateBreackUp["Meal"] = BoardName;
                                                //    string rateWeekColumn = Convert.ToString(dtCheckInDate.Date.AddDays(n).DayOfWeek).Substring(0, 3);
                                                //    drRateBreackUp["DateOfWeek"] = rateWeekColumn;
                                                //    DateTime rateDate = dtCheckInDate.Date.AddDays(n);
                                                //    drRateBreackUp["Date"] = rateDate;
                                                //    drRateBreackUp["Rate"] = ratePerNight;
                                                //    drRateBreackUp["OrderNo"] = 1;
                                                //    drRateBreackUp["RoomTypeName"] = FinalRoomtype;
                                                //    drRateBreackUp["DateOfWeek"] = rateWeekColumn;
                                                //    drRateBreackUp["Date"] = rateDate;
                                                //    drRateBreackUp["BookingStatus"] = "Available";
                                                //    drRateBreackUp["BoardTypeID"] = "";
                                                //    drRateBreackUp["VatdiffAmount"] = 0;
                                                //    drRateBreackUp["RateType"] = "A Rate";
                                                //    //drRateBreackUp["SupplierCurrency"] = currency;
                                                //    drRateBreackUp["BuyingPrice"] = TotalPrice;
                                                //    DtRoomRateBreackUp.Rows.Add(drRateBreackUp);
                                                //}
                                                #endregion

                                                #region Rooms
                                                DataRow drRoom = dtHotelRoom.NewRow();
                                                drRoom["OwnsystemHotelId"] = OwnsystemHotelId;
                                                drRoom["RoomTypeCode"] = RoomTypeId;
                                                drRoom["BoardType"] = BoardName;
                                                drRoom["MealName"] = BoardName;
                                                drRoom["MealId"] = BoardId;
                                                drRoom["Type"] = "";
                                                drRoom["Board"] = "";
                                                drRoom["BookingStatus"] = "Available";
                                                drRoom["UniqueNo"] = Guid.NewGuid().ToString();
                                                drRoom["ContractID"] = 0;
                                                drRoom["RoomNo"] = Convert.ToString(1);
                                                drRoom["BoardTypeID"] = "";
                                                //drRoom["AdultsOccupancy"] = TotalAdult;
                                                //drRoom["ChildOccupancy"] = TotalChild;
                                                drRoom["OfferCode"] = "";
                                                drRoom["OfferDescription"] = "";
                                                drRoom["RoomSequenceNo"] = "";
                                                string EssentialInformation = Description;
                                                drRoom["EssentialInformation"] = EssentialInformation;
                                                drRoom["Characteristic"] = "";
                                                bool IsCombine = true;
                                                bool IsDynamicPolicy = true;
                                                #region Vat
                                                double itemprice = TotalPrice;
                                                _markup.ProcessHotelRoom_WithMarkup(CurrencyFactorpaximum, currency, IsDynamicPolicy, IsCombine, SearchParam.NoofNights * SearchParam.Rooms.Count, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), SearchParam.CheckInDate, SearchParam.CheckOutDate, ref itemprice, ref drRoom, isPerce, AgentPercentage, AgentMarkupAmount, commonSearch.SubAgentMarkup, commonSearch.B2CMarkup, commonSearch.IsParentId);
                                                #endregion
                                                drRoom["SupplierName"] = Provider;
                                                drRoom["RoomTypeName"] = FinalRoomtype;
                                                drRoom["RoomTypeId"] = RoomTypeId;
                                                dtHotelRoom.Rows.Add(drRoom);
                                                #endregion

                                                #region canpolicy
                                               

                                                bool isAvailable = Convert.ToBoolean(paximumResponse.hotels[h].offers[i].isAvailable);
                                                double changeamount = Convert.ToDouble(paximumResponse.hotels[h].offers[i].price.amount);
                                                string token_pax = Convert.ToString(isAvailable + "$$" + changeamount);

                                                if (paximumResponse.hotels[h].offers[i].cancellationPolicies?.Count != null)
                                                {
                                                    int count_clp = paximumResponse.hotels[h].offers[i].cancellationPolicies.Count;
                                                    DateTime EndDate = DateTime.Now.Date;
                                                    for (int cp = 0; cp < count_clp; cp++)
                                                    {
                                                        double CancelAmount = 1;
                                                        DateTime Canceldate = DateTime.Now.Date;
                                                        if (paximumResponse.hotels[h].offers[i].cancellationPolicies[cp]?.afterDate != null)
                                                        {
                                                            Canceldate = Convert.ToDateTime(paximumResponse.hotels[h].offers[i].cancellationPolicies[cp].afterDate);
                                                        }
                                                        if (paximumResponse.hotels[h].offers[i].cancellationPolicies[cp].fee != null)
                                                        {
                                                            CancelAmount = Convert.ToDouble(paximumResponse.hotels[h].offers[i].cancellationPolicies[cp].fee.amount);
                                                        }
                                                        #region Caculation
                                                        #region Vat
                                                        double VatOnPolicy = 0;
                                                        double MainAgentCharge = CancelAmount;
                                                        double CompanyChargeAmount = CancelAmount;
                                                        double cancellationChargeAmount = _markup.ProcessHotelCancelationPrice_WithMarkup(ref MainAgentCharge, SearchParam.Rooms.Count * SearchParam.NoofNights, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), CancelAmount, isPerce, commonSearch.B2CMarkup, AgentPercentage, AgentMarkupAmount, commonSearch.IsParentId, commonSearch.SubAgentMarkup, ref VatOnPolicy);
                                                        #endregion

                                                        DateTime CancellationDate = DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", CultureInfo.InvariantCulture).Date;
                                                        DateTime NowDate = DateTime.Now.Date;
                                                        double CancelCharge = cancellationChargeAmount;
                                                        int CancelDAY = Canceldate.Date.Day;
                                                        CancellationDate = Canceldate.AddDays(-CancelDAY);

                                                        #endregion

                                                        if (CancellationDate.AddDays(0).Date > DateTime.Now.Date)
                                                            CancellationDate = Canceldate.AddDays(CutOffDays).Date;

                                                        if (NowDate.Date < CancellationDate.Date)
                                                        {
                                                            double charge = 0;
                                                            dtCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref charge, dtCancellation.NewRow(), CurrencyFactorpaximum, currency, Provider, Convert.ToString(OwnsystemHotelId), Convert.ToString(1),
                                                            RoomTypeId, Convert.ToString(FinalRoomtype), Convert.ToString(BoardId), DateTime.Now.Date, CancellationDate.AddDays(-1).AddHours(23).AddMinutes(59).AddSeconds(59), 0, 0, "", VatOnPolicy, "", "", token_pax, EssentialInformation, "", ""));
                                                            dtCancellation.AcceptChanges();

                                                            dtCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref CompanyChargeAmount, dtCancellation.NewRow(), CurrencyFactorpaximum, currency, Provider, Convert.ToString(OwnsystemHotelId), Convert.ToString(1),
                                                            RoomTypeId, Convert.ToString(FinalRoomtype), Convert.ToString(BoardId), CancellationDate.Date, DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null), CancelCharge, CancelCharge, "", VatOnPolicy, "", "", token_pax, EssentialInformation, "", ""));
                                                            dtCancellation.AcceptChanges();
                                                        }
                                                        else
                                                        {
                                                            DataRow drMercanHotelCancellation_Temp = dtCancellation.NewRow();
                                                            dtCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref CompanyChargeAmount, dtCancellation.NewRow(), CurrencyFactorpaximum, currency, Provider, Convert.ToString(OwnsystemHotelId), Convert.ToString(1),
                                                           RoomTypeId, Convert.ToString(FinalRoomtype), Convert.ToString(BoardId), DateTime.Now.Date, DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null), CancelCharge, CancelCharge, "", VatOnPolicy, "", "", token_pax, EssentialInformation, "", ""));
                                                            dtCancellation.AcceptChanges();
                                                        }

                                                    }
                                                }
                                                #endregion


                                            }

                                        }
                                    }

                                    #endregion



                                }
                                #endregion
                                Options ops = (await _supplierRes.GetCommonRoomForSuppliers(GiataId, commonSearch.tokenData, commonSearch.advancedOptions, FinalMinPrice, drXmlHotel, dtHotelRoom, dtCancellation, DtRoomRateBreackUp, SearchParam));
                                if (ops != null)
                                    options.Add(ops);
                            }
                            #endregion
                            #endregion
                        }

                        #endregion
                    }

                }


                return options;
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
                return options;
            }
            //});
            finally
            {
                #region ResProcessTimer
                try
                {
                    if (SearchParam != null && ResProccessTimerLogActive == "1" && timer_room_Paximum.ElapsedMilliseconds > Convert.ToInt32(ResProccessTimeValueforSaveLog))
                    {
                        timer_room_Paximum.Stop();
                        SearchParam.SupplierTime += ",|Finish:Paximum Process Response  " + "|Time :" + timer_room_Paximum.Elapsed.ToString();
                        _= Task.Run(() => _commonMethod.FireAway_SQL_ResPorcessTimeLog("", timer_room_Paximum.Elapsed.ToString(), "Paximum", SearchParam.SearchKey, 1, SearchParam.AgentID));
                    }
                }
                catch (Exception x)
                {
                    await _exception.Error(x);
                }
                #endregion
            }
        }

        //recheck
        public async Task<CancellationDto> paximum_cancellation(string roomtypecode, DataSet dsHotel, string hotelcode, string ownsystemid, string roomtypeid, DataTable dtAgentDetail, DataTable dtSuplierMarkup, DataTable dtSearchConfig, HotelSearchParameter SearchPara, string roomno, string provider, int AgentId, DataTable dtMarkupDetails, string Notes, string GiataId = "0", int hotelRating = 0, string CompanyBuyPrice = "")
        {
            string IsBooking = "Available";
            string Error = "";
            string time = "";
            string ResponseData = "";
            string Request = "";
            string responsetype = "";
            double CompanyBuy = Convert.ToDouble(CompanyBuyPrice);

            CancellationDto cancellationDto = new CancellationDto();    

            try
            {
                DataTable dtpaximumCancellationPolicy = _commonMethod.GenerateCancellationDataTableNewV2();
                int CutOffDays = XMLHotelBookingCommon.CutOffDays;
                CutOffDays = _commonMethod.GetCutOffDays(dtSuplierMarkup, provider);

                
                DataRow[] drSuplierMarkup = (dtSuplierMarkup.Select("SupplierName='" + provider + "'"));
                int SupplierId = Convert.ToInt32(drSuplierMarkup[0]["SupplierID"]);

                string PaximumCurrency = Convert.ToString(drSuplierMarkup[0]["CurrencyShortName"]);
                DataTable dtInrCurrency = _commonMethod.GetCountryCurrency(PaximumCurrency);
                double CurrencyFactorPaximum = Convert.ToDouble(dtInrCurrency.Rows[0]["AmtToExch"]);



                DataRow[] drSearchConfig = (dtSearchConfig.Select("SupplierID='" + SupplierId + "'"));

                string request = paximumPriceCheckRequest(roomtypeid);

                System.Diagnostics.Stopwatch timerroomprocess = new System.Diagnostics.Stopwatch();
                timerroomprocess.Start();

                string Cancelpolicy_Url = Convert.ToString(drSuplierMarkup[0]["WebSiteURL"]);
                string URL = Cancelpolicy_Url + "search/checkAvailability";
                string Password = Convert.ToString(drSuplierMarkup[0]["Password"]);


                ResponseDto responseDto = await GetpaximumCancelResponse(request, URL, 300000, "paximum", Password);
                ResponseData = responseDto.Response;
                Error = responseDto.Error;

                timerroomprocess.Stop();
                time = timerroomprocess.Elapsed.ToString();

                _commonMethod.ThirdPartyGetPolicyHistory(Convert.ToString(roomtypeid), Convert.ToString(ownsystemid), request, Convert.ToString(ResponseData), "After Get Policy", "paximum", "Response", "-", "");

                Paximum_prebook.Root paxresponse = JsonConvert.DeserializeObject<Paximum_prebook.Root>(ResponseData);

                if (paxresponse == null || paxresponse.cancellationPolicies == null || paxresponse.rooms == null || paxresponse.rooms[0].price == null)
                {
                    cancellationDto.dsHotelData = dsHotel;
                    return cancellationDto;
                }

                string mealid = paxresponse.boardId;
                string roomtypename = paxresponse.rooms[0].type;
                if (paxresponse.cancellationPolicies.Count > 0)
                {

                    Notes = "";
                    if (paxresponse.notes != null)
                    {

                        for (int h = 0; h < paxresponse.notes.Count; h++)
                        {

                            Notes = Notes + "<br>" + Convert.ToString(paxresponse.notes[h]);
                        }
                    }



                    #region canpolicy

                    //double AgentPercentage = 0.0;
                    //double AgentMarkupAmount = 0.0;
                    //double SubAgentMarkup = 0;
                    //Boolean isPerce = true;
                    //double B2CMarkup = 0;
                    //int IsParentId = 1;

                    Boolean isPerce = true;
                    double AgentCompanyPercentage = Convert.ToDouble(SearchPara.AgentCompanyPercentage);
                    bool IsB2C = Convert.ToBoolean(Convert.ToByte(SearchPara.IsB2BOrB2C));
                    double B2CMarkup = (IsB2C == false ? AgentCompanyPercentage : XMLHotelBookingCommon.B2CMarkup);
                    double AgentPercentage = 0.0;
                    double AgentMarkupAmount = 0.0;
                    double SubAgentMarkup = 0;
                    int IsParentId = 1;
                    Boolean blnIsShowSupplier = false;
                    _markup.getAgetMarkupSetting("Paximum", GiataId, dtSuplierMarkup, dtMarkupDetails, ref AgentPercentage, ref AgentMarkupAmount, ref blnIsShowSupplier, ref isPerce, AgentId, hotelRating, SearchPara.CityID, Convert.ToString(SearchPara.CountryID) == null ? "0" : Convert.ToString(SearchPara.CountryID), Convert.ToString(SearchPara.CheckInDate));

                    if (dtAgentDetail != null)
                    {
                        IsParentId = Convert.ToInt32(dtAgentDetail.Rows[0]["IsParentId"]);
                        SubAgentMarkup = Convert.ToDouble(dtAgentDetail.Rows[0]["B2CMarkupPer"]);
                    }

                    string currency = Convert.ToString(PaximumCurrency);
                    double CancelAmount = Convert.ToDouble(paxresponse.cancellationPolicies[0].fee.amount);
                    DateTime Canceldate = Convert.ToDateTime(paxresponse.cancellationPolicies[0].afterDate);

                    bool isAvailable = Convert.ToBoolean(paxresponse.isAvailable);
                    double changeamount = Convert.ToDouble(paxresponse.price.amount);
                    string token_pax = Convert.ToString(isAvailable + "$$" + changeamount);

                    #region Caculation
                    #region Vat
                    double VatOnPolicy = 0;
                    double MainAgentCharge = CancelAmount;
                    double CompanyChargeAmount = CancelAmount;
                    double cancellationChargeAmount = _markup.ProcessHotelCancelationPrice_WithMarkup(ref MainAgentCharge, SearchPara.Rooms.Count * SearchPara.NoofNights, SearchPara.CityID,Convert.ToString(SearchPara.CountryID), CancelAmount, isPerce, B2CMarkup, AgentPercentage, AgentMarkupAmount, IsParentId, SubAgentMarkup, ref VatOnPolicy);
                    #endregion

                    DateTime CancellationDate = DateTime.ParseExact(SearchPara.CheckInDate, "dd/MM/yyyy", CultureInfo.InvariantCulture).Date;
                    DateTime NowDate = DateTime.Now.Date;
                    double CancelCharge = cancellationChargeAmount;
                    int CancelDAY = Canceldate.Date.Day;
                    CancellationDate = Canceldate.AddDays(-CancelDAY);

                    #endregion

                    if (CancellationDate.AddDays(0).Date > DateTime.Now.Date)
                        CancellationDate = Canceldate.AddDays(CutOffDays).Date;
                    if (NowDate.Date < CancellationDate.Date)
                    {
                        double charge = 0;
                        dtpaximumCancellationPolicy.Rows.Add(_commonMethod.setCancellationPolicyRow(ref charge, dtpaximumCancellationPolicy.NewRow(), CurrencyFactorPaximum, currency, provider, ownsystemid, Convert.ToString(1),
                        roomtypeid, Convert.ToString(roomtypename), Convert.ToString(mealid), DateTime.Now.Date, CancellationDate.AddDays(-1).AddHours(23).AddMinutes(59).AddSeconds(59), 0, 0, "", VatOnPolicy, "", "", token_pax, Notes, "", ""));
                        dtpaximumCancellationPolicy.AcceptChanges();

                        dtpaximumCancellationPolicy.Rows.Add(_commonMethod.setCancellationPolicyRow(ref CompanyChargeAmount, dtpaximumCancellationPolicy.NewRow(), CurrencyFactorPaximum, currency, provider, ownsystemid, Convert.ToString(1),
                        roomtypeid, Convert.ToString(roomtypename), Convert.ToString(mealid), CancellationDate.Date, DateTime.ParseExact(SearchPara.CheckInDate, "dd/MM/yyyy", null), CancelCharge, CancelCharge, "", VatOnPolicy, "", "", token_pax, Notes, "", ""));
                        dtpaximumCancellationPolicy.AcceptChanges();
                    }
                    else
                    {
                        DataRow drMercanHotelCancellation_Temp = dtpaximumCancellationPolicy.NewRow();
                        dtpaximumCancellationPolicy.Rows.Add(_commonMethod.setCancellationPolicyRow(ref CompanyChargeAmount, dtpaximumCancellationPolicy.NewRow(), CurrencyFactorPaximum, currency, provider, ownsystemid, Convert.ToString(1),
                       roomtypeid, Convert.ToString(roomtypename), Convert.ToString(mealid), DateTime.Now.Date, DateTime.ParseExact(SearchPara.CheckInDate, "dd/MM/yyyy", null), CancelCharge, CancelCharge, "", VatOnPolicy, "", "", token_pax, Notes, "", ""));
                        dtpaximumCancellationPolicy.AcceptChanges();
                    }
                    #endregion

                }
                dsHotel.Tables[2].Merge(dtpaximumCancellationPolicy);
            }
            catch (Exception ex)
            {
                try
                {
                    responsetype = "GeneralError";
                    //int AgentId = Convert.ToInt32(Common.getSession("UserId") == "" ? "0" : Common.getSession("UserId"));
                    string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook("paximum", "PreBook", AgentId, time, responsetype, Request, ResponseData, Error);
                }
                catch (Exception e)
                {
                    _ = _exception.Error(e);
                }
                //ExceptionManager.LogException(e);
                cancellationDto.dsHotelData = null;
                return cancellationDto;
            }
            cancellationDto.dsHotelData = dsHotel;
            cancellationDto.ISBooking = IsBooking;
            return cancellationDto;
        }

        public string paximumPriceCheckRequest(string roomtypeid)
        {
            string RequestString = "";
            try
            {

                string[] spitid1 = roomtypeid.Split(new String[] { "###" }, StringSplitOptions.None);

                string offerid = spitid1[0];

                StringBuilder paxprice = new StringBuilder();
                paxprice.Append("{");
                paxprice.Append("\"offerId\" :\"" + offerid + "\"");
                paxprice.Append("}");
                string Paximumprice = paxprice.ToString();

                return Paximumprice;
            }
            catch (Exception e)
            {
                _exception.Error(e);
            }
            return "";
        }

        public async Task<ResponseDto> GetpaximumCancelResponse(string strxmlRQRS, string strHost, Int32 TimeOut, string Provider = "", string Password = "")
        {
            System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
            timer.Start();
            string response = null;
            string Error = "";
            ResponseDto responseDto = new ResponseDto();
            string reqId = Convert.ToString(DateTime.Now.Ticks);
            try
            {
                string Key = "";
                HttpWebRequest Request;
                Request = (HttpWebRequest)HttpWebRequest.Create(strHost);
                Request.Timeout = TimeOut;
                Request.ReadWriteTimeout = TimeOut;

                string ProviderNew = Provider.ToLower();
                switch (ProviderNew)
                {
                    case "paximum":
                        Request.Accept = "application/json";
                        Request.Method = "POST";
                        Request.Headers.Add("Authorization", "Bearer " + Password + "");
                        Request.ContentType = "application/json; charset=utf-8";
                        Request.Host = "api.paximum.com"; //WebConfigurationManager.AppSettings["PaximumHost"]; 
                        Request.Headers.Add("Accept-Encoding", "gzip,deflate");
                        Request.KeepAlive = true;
                        break;
                    default:

                        Request.Headers.Add("Accept-Encoding", "gzip");

                        break;
                }


                using (Stream requestStream = Request.GetRequestStream())
                {
                    byte[] postBytes = Encoding.ASCII.GetBytes(strxmlRQRS);
                    //Writes a sequence of bytes to the current stream                     
                    requestStream.Write(postBytes, 0, postBytes.Length);
                    requestStream.Close();//Close stream
                }

                HttpWebResponse response1 = (HttpWebResponse)await Request.GetResponseAsync();
                Stream responseStream = response1.GetResponseStream();

                if (response1.ContentEncoding.ToLower().Contains("gzip"))
                    responseStream = new System.IO.Compression.GZipStream(responseStream, System.IO.Compression.CompressionMode.Decompress);
                else if (response1.ContentEncoding.ToLower().Contains("deflate"))
                    responseStream = new System.IO.Compression.DeflateStream(responseStream, System.IO.Compression.CompressionMode.Decompress);
                StreamReader streamReader = new StreamReader(responseStream);


                response = await streamReader.ReadToEndAsync();
                string strOutput = response.ToString();
                responseStream.Close();
                streamReader.Close();
                response1.Close();
            }
            //catch (Exception ee)
            //{
            //    ExceptionManager.LogException(ee);
            //    Error = ee.Message.ToString();
            //    return null;
            //}
            catch (Exception e)
            {
                Error = e.Message.ToString();
                _ = _exception.ErrorBooking(e, "", "paximum");
            }

            responseDto.Response = response;
            responseDto.Error = Error;

            return responseDto;

        }

        public string paximumBookingRequest(DataSet dsCartDetails, DataRow[] paximumDetails, int i, int j, double resPrice, DataTable dtPassengers, ref string AgtRef, string CheckIn, string CheckOut, int UniqueNo)
        {
            #region Booking Requestold
            //try
            //{
            //    string roomtypeid = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomTypeId"]);
            //    //string RoomTypeId = offerid + "###" + roomid + "###" + amount;
            //    string[] spitid1 = roomtypeid.Split(new String[] { "###" }, StringSplitOptions.None);

            //    string roomid = spitid1[1];
            //    string offerid = spitid1[0];
            //    double amount = Convert.ToDouble(spitid1[2]);
            //    string nationality = spitid1[3];
            //    string HotelCode = Convert.ToString(dsCartDetails.Tables[0].DefaultView[i]["XMLHotelId"]);
            //    string title = Convert.ToString(dtPassengers.DefaultView[0]["Prefix"]);
            //    string BirthDay1 = Convert.ToString(DateTime.Now.Date.AddYears(-30));
            //    DateTime Dt1 = (Convert.ToDateTime(BirthDay1));
            //    string ExternalReference = "";
            //    string FinalExternalReference = "";
            //    int UserCartId = Convert.ToInt32(dsCartDetails.Tables[0].Rows[i]["UserCartId"]);

            //    //string ExternalReference = "Book_" + Convert.ToString(Common.getSession("AgentCode") + "_" + uniqid);
            //    string Email = WebConfigurationManager.AppSettings["email"];
            //    string reservationNote = "";
            //    StringBuilder bookingreq = new StringBuilder();
            //    bookingreq.Append("{");
            //    bookingreq.Append("\"travellers\":[");

            //    dtPassengers.DefaultView.RowFilter = "PickUp='" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomTypeID"]) + "' and RoomNo='" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomNumber"]) + "' and IsLead=1";
            //    if (dtPassengers.DefaultView.Count == 0)

            //        dtPassengers.DefaultView.RowFilter = "";

            //    for (int PS = 0; PS < dsCartDetails.Tables[1].DefaultView.Count; PS++)
            //    {
            //        dtPassengers.DefaultView.RowFilter = "PickUp='" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomTypeID"]) + "' and RoomNo='" + (dsCartDetails.Tables[1].DefaultView[PS]["RoomNumber"]) + "' and UserCartId='" + dsCartDetails.Tables[0].Rows[i]["UserCartId"] + "'";
            //        //dtPassengers.DefaultView.RowFilter = "PickUp='" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomTypeID"]) + "' and UserCartId='" + dsCartDetails.Tables[0].Rows[i]["UserCartId"] + "'";

            //        dtPassengers.DefaultView.Sort = "IsLead desc";
            //        string abc = "";
            //        if (dtPassengers.DefaultView.Count > 0)
            //        {
            //            for (int k = 0; k < dtPassengers.DefaultView.Count; k++)
            //            {
            //                string Firstname = Convert.ToString(dtPassengers.DefaultView[k]["FirstName"]).Trim();
            //                string Lastname = Convert.ToString(dtPassengers.DefaultView[k]["LastName"]).Trim();
            //                string uniqid = Guid.NewGuid().ToString().Substring(0, 8);
            //                ExternalReference = "Book" + Convert.ToString(Common.getSession("AgentCode") + uniqid);
            //                //ExternalReference = ExternalReference.Replace(",", "");
            //                int RoomNo1 = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[PS]["RoomNumber"]);
            //                //abc = UserCartId + "_" + RoomNo1 + "_" + (k + 1);
            //                abc = UserCartId + "_" + RoomNo1 + "_" + (k + 1);

            //                DataRow[] drPassengers = dtPassengers.DefaultView.ToTable().Select("IsLead='1'");
            //                if (drPassengers.Length == 0)
            //                {
            //                    if (Convert.ToString(dtPassengers.DefaultView[k]["PaxType"]).Trim().ToLower() == "adult")
            //                        dtPassengers.DefaultView[k]["IsLead"] = 1;
            //                }
            //                if (PS == 0 && k == 0)//if (Convert.ToInt16(dtPassengers.DefaultView[k]["IsLead"]) == 1)
            //                {
            //                    bookingreq.Append("{");
            //                    bookingreq.Append("\"travellerNo\":\"" + abc + "\",");
            //                    bookingreq.Append("\"type\":\"adult \",");
            //                    bookingreq.Append("\"title\":\"" + title + "\",");
            //                    bookingreq.Append("\"name\":\"" + Firstname + "\",");
            //                    bookingreq.Append("\"surname\":\"" + Lastname + "\",");
            //                    // bookingreq.Append("\"birthDate\":\"" + Dt1 + "\"");
            //                    bookingreq.Append("\"isLead\":\"true \",");
            //                    // bookingreq.Append("\"email\":\"" + Email + "\"");
            //                    // bookingreq.Append("\"phone\":\" 5354443322 \",");
            //                    // bookingreq.Append("\"mobile\":\" 4545454545 \",");
            //                    bookingreq.Append("\"nationality\":\"" + nationality + "\"");
            //                    bookingreq.Append("}");
            //                }
            //                else
            //                {
            //                    bookingreq.Append(",{");
            //                    bookingreq.Append("\"travellerNo\":\"" + abc + "\",");
            //                    bookingreq.Append("\"type\":\"adult \",");
            //                    bookingreq.Append("\"title\":\"" + title + "\",");
            //                    bookingreq.Append("\"name\":\"" + Firstname + "\",");
            //                    bookingreq.Append("\"surname\":\"" + Lastname + "\",");
            //                    //bookingreq.Append("\"birthDate\":\"" + Dt1 + "\"");
            //                    bookingreq.Append("\"isLead\":\"false \",");
            //                    bookingreq.Append("\"nationality\":\"" + nationality + "\"");
            //                    bookingreq.Append("}");
            //                }
            //            }
            //        }
            //    }
            //    bookingreq.Append("],");
            //    bookingreq.Append("\"hotelBookings\":[{");
            //    bookingreq.Append("\"offerId\":\"" + offerid + "\",");
            //    bookingreq.Append("\"reservationNote\":\"" + reservationNote + "\",");

            //    bookingreq.Append("\"rooms\":[");
            //    for (int x = 0; x < dsCartDetails.Tables[1].DefaultView.Count; x++)
            //    {
            //        //if (x == 0)
            //        //    FinalExternalReference = ExternalReference;
            //        //else
            //        //    FinalExternalReference = FinalExternalReference + "," + ExternalReference;

            //        string[] Roomtypeid = roomid.Split(new String[] { "$$" }, StringSplitOptions.None);
            //        if (x == 0)
            //            bookingreq.Append("{");
            //        else
            //            bookingreq.Append("},{");

            //        bookingreq.Append("\"roomid\":\"" + Roomtypeid[x] + "\",");

            //        dtPassengers.DefaultView.RowFilter = "PickUp='" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomTypeID"]) + "' and RoomNo='" + (dsCartDetails.Tables[1].DefaultView[x]["RoomNumber"]) + "' and UserCartId='" + dsCartDetails.Tables[0].Rows[i]["UserCartId"] + "'";
            //        //dtPassengers.DefaultView.RowFilter = "PickUp='" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomTypeID"]) + "' and UserCartId='" + dsCartDetails.Tables[0].Rows[i]["UserCartId"] + "'";

            //        dtPassengers.DefaultView.Sort = "IsLead desc";
            //        FinalExternalReference = "";
            //        string abc = "";
            //        for (int ab = 0; ab < dtPassengers.DefaultView.Count; ab++)
            //        {
            //            int RoomNo1 = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[x]["RoomNumber"]);
            //            abc = UserCartId + "_" + RoomNo1 + "_" + (ab + 1);
            //            if (ab == 0)
            //            {
            //                FinalExternalReference = abc;
            //            }
            //            else
            //            {
            //                FinalExternalReference = FinalExternalReference + "," + abc;
            //            }
            //        }
            //        bookingreq.Append("\"travellers\":[\"" + FinalExternalReference + "\"]");
            //    }
            //    bookingreq.Append("}]");
            //    bookingreq.Append("}]");
            //    bookingreq.Append("}");
            //    string bookingrequest = bookingreq.ToString();
            //    return bookingrequest;

            //}
            //catch (Exception ex)
            //{
            //    ExceptionManager.LogException(ex);
            //}
            //return null;
            #endregion


            #region Booking Request 
            try
            {
                string roomtypeid = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomTypeId"]);
                //string RoomTypeId = offerid + "###" + roomid + "###" + amount;
                string[] spitid1 = roomtypeid.Split(new String[] { "###" }, StringSplitOptions.None);

                string roomid = spitid1[1];
                string offerid = spitid1[0];
                double amount = Convert.ToDouble(spitid1[2]);
                string nationality = spitid1[3];
                string HotelCode = Convert.ToString(dsCartDetails.Tables[0].DefaultView[i]["XMLHotelId"]);
                string title = Convert.ToString(dtPassengers.DefaultView[0]["Prefix"]);
                string BirthDay1 = Convert.ToString(DateTime.Now.Date.AddYears(-30));
                DateTime Dt1 = (Convert.ToDateTime(BirthDay1));
                string ExternalReference = "";
                string FinalExternalReference = "";
                int UserCartId = Convert.ToInt32(dsCartDetails.Tables[0].Rows[i]["UserCartId"]);

                //string ExternalReference = "Book_" + Convert.ToString(Common.getSession("AgentCode") + "_" + uniqid);
                //string Email = WebConfigurationManager.AppSettings["email"];
                string reservationNote = "";
                StringBuilder bookingreq = new StringBuilder();
                bookingreq.Append("{");
                bookingreq.Append("\"travellers\":[");

                dtPassengers.DefaultView.RowFilter = "PickUp='" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomTypeID"]) + "' and RoomNo='" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomNumber"]) + "' and IsLead=1";
                if (dtPassengers.DefaultView.Count == 0)

                    dtPassengers.DefaultView.RowFilter = "";

                for (int PS = 0; PS < dsCartDetails.Tables[1].DefaultView.Count; PS++)
                {

                    dtPassengers.DefaultView.RowFilter = "PickUp='" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomTypeID"]) + "' and RoomNo='" + (dsCartDetails.Tables[1].DefaultView[PS]["RoomNumber"]) + "' and UserCartId='" + dsCartDetails.Tables[0].Rows[i]["UserCartId"] + "'";
                    //dtPassengers.DefaultView.RowFilter = "PickUp='" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomTypeID"]) + "' and UserCartId='" + dsCartDetails.Tables[0].Rows[i]["UserCartId"] + "'";


                    dtPassengers.DefaultView.Sort = "IsLead desc";
                    string abc = "";
                    if (dtPassengers.DefaultView.Count > 0)
                    {
                        for (int k = 0; k < dtPassengers.DefaultView.Count; k++)
                        {

                            string Firstname = Convert.ToString(dtPassengers.DefaultView[k]["FirstName"]).Trim();
                            string Lastname = Convert.ToString(dtPassengers.DefaultView[k]["LastName"]).Trim();
                            string uniqid = Guid.NewGuid().ToString().Substring(0, 8);
                            //ExternalReference = "Book" + Convert.ToString(Common.getSession("AgentCode") + uniqid);
                            //ExternalReference = ExternalReference.Replace(",", "");
                            int RoomNo1 = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[PS]["RoomNumber"]);
                            //abc = UserCartId + "_" + RoomNo1 + "_" + (k + 1);
                            abc = UserCartId + "_" + RoomNo1 + "_" + (k + 1);

                            DataRow[] drPassengers = dtPassengers.DefaultView.ToTable().Select("IsLead='1'");
                            if (drPassengers.Length == 0)
                            {
                                if (Convert.ToString(dtPassengers.DefaultView[k]["PaxType"]).Trim().ToLower() == "adult")
                                    dtPassengers.DefaultView[k]["IsLead"] = 1;
                            }
                            if (PS == 0 && k == 0) // (Convert.ToInt16(dtPassengers.DefaultView[k]["IsLead"]) == 1)
                            {
                                bookingreq.Append("{");
                                bookingreq.Append("\"travellerNo\":\"" + abc + "\",");
                                bookingreq.Append("\"type\":\"adult\",");
                                bookingreq.Append("\"title\":\"" + title + "\",");
                                bookingreq.Append("\"name\":\"" + Firstname + "\",");
                                bookingreq.Append("\"surname\":\"" + Lastname + "\",");
                                // bookingreq.Append("\"birthDate\":\"" + Dt1 + "\"");
                                bookingreq.Append("\"isLead\":\"true\",");
                                // bookingreq.Append("\"email\":\"" + Email + "\"");
                                // bookingreq.Append("\"phone\":\" 5354443322 \",");
                                // bookingreq.Append("\"mobile\":\" 4545454545 \",");
                                bookingreq.Append("\"nationality\":\"" + nationality + "\"");
                                bookingreq.Append("}");
                            }
                            else
                            {
                                bookingreq.Append(",{");
                                bookingreq.Append("\"travellerNo\":\"" + abc + "\",");
                                bookingreq.Append("\"type\":\"adult\",");
                                bookingreq.Append("\"title\":\"" + title + "\",");
                                bookingreq.Append("\"name\":\"" + Firstname + "\",");
                                bookingreq.Append("\"surname\":\"" + Lastname + "\",");
                                //bookingreq.Append("\"birthDate\":\"" + Dt1 + "\"");
                                bookingreq.Append("\"isLead\":\"false\",");
                                bookingreq.Append("\"nationality\":\"" + nationality + "\"");
                                bookingreq.Append("}");
                            }
                            //bookingreq.Append("}");
                        }
                    }
                }
                bookingreq.Append("],");
                bookingreq.Append("\"hotelBookings\":[{");
                bookingreq.Append("\"offerId\":\"" + offerid + "\",");
                bookingreq.Append("\"reservationNote\":\"" + reservationNote + "\",");

                bookingreq.Append("\"rooms\":[");
                for (int x = 0; x < dsCartDetails.Tables[1].DefaultView.Count; x++)
                {
                    //if (x == 0)
                    //    FinalExternalReference = ExternalReference;
                    //else
                    //    FinalExternalReference = FinalExternalReference + "," + ExternalReference;

                    string[] Roomtypeid = roomid.Split(new String[] { "$$" }, StringSplitOptions.None);
                    if (x == 0)
                        bookingreq.Append("{");
                    else
                        bookingreq.Append("},{");

                    bookingreq.Append("\"roomid\":\"" + Roomtypeid[x] + "\",");

                    dtPassengers.DefaultView.RowFilter = "PickUp='" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomTypeID"]) + "' and RoomNo='" + (dsCartDetails.Tables[1].DefaultView[x]["RoomNumber"]) + "' and UserCartId='" + dsCartDetails.Tables[0].Rows[i]["UserCartId"] + "'";
                    //dtPassengers.DefaultView.RowFilter = "PickUp='" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomTypeID"]) + "' and UserCartId='" + dsCartDetails.Tables[0].Rows[i]["UserCartId"] + "'";

                    dtPassengers.DefaultView.Sort = "IsLead desc";
                    FinalExternalReference = "";
                    string abc = "";
                    for (int ab = 0; ab < dtPassengers.DefaultView.Count; ab++)
                    {
                        int RoomNo1 = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[x]["RoomNumber"]);
                        abc = UserCartId + "_" + RoomNo1 + "_" + (ab + 1);
                        if (ab == 0)
                        {
                            FinalExternalReference = "\"" + abc + "\"";
                        }
                        else
                        {
                            FinalExternalReference = FinalExternalReference + "," + "\"" + abc + "\"";
                        }
                    }
                    bookingreq.Append("\"travellers\":[" + FinalExternalReference + "]");
                }
                bookingreq.Append("}]");
                // bookingreq.Remove(bookingreq.Length - 1, 1);
                //PRQ.Remove(PRQ.Length - 1, 1);
                bookingreq.Append("}]");
                bookingreq.Append("}");
                string bookingrequest = bookingreq.ToString();
                return bookingrequest;

            }
            catch (Exception e)
            {
                _ =_exception.Error(e);
            }
            return null;
            #endregion
        }

        public async Task<ResponseDto> GetpaximumbookingResponse(string strxmlRQRS, string strHost, Int32 TimeOut, string TechName = "", string Password = "")
        {
            ResponseDto responseDto = new ResponseDto();
            string Error = "";
            System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
            timer.Start();
            string response = null;


            string reqId = Convert.ToString(DateTime.Now.Ticks);
            try
            {
                string Key = "";
                HttpWebRequest Request;
                Request = (HttpWebRequest)HttpWebRequest.Create(strHost);
                Request.Timeout = TimeOut;
                Request.ReadWriteTimeout = TimeOut;

                string TechNameNew = TechName.ToLower();
                switch (TechNameNew)
                {
                    case "paximum":
                        Request.Accept = "application/json";
                        Request.Method = "POST";
                        Request.Headers.Add("Authorization", "Bearer " + Password + "");
                        Request.ContentType = "application/json; charset=utf-8";
                        Request.Host = "api.paximum.com"; //WebConfigurationManager.AppSettings["PaximumHost"]; 
                        Request.Headers.Add("Accept-Encoding", "gzip,deflate");
                        Request.KeepAlive = true;
                        break;
                    default:

                        Request.Headers.Add("Accept-Encoding", "gzip");

                        break;
                }


                using (Stream requestStream = Request.GetRequestStream())
                {
                    byte[] postBytes = Encoding.ASCII.GetBytes(strxmlRQRS);
                    //Writes a sequence of bytes to the current stream                     
                    requestStream.Write(postBytes, 0, postBytes.Length);
                    requestStream.Close();//Close stream
                }

                HttpWebResponse response1 = (HttpWebResponse)await Request.GetResponseAsync();
                Stream responseStream = response1.GetResponseStream();

                if (response1.ContentEncoding.ToLower().Contains("gzip"))
                    responseStream = new System.IO.Compression.GZipStream(responseStream, System.IO.Compression.CompressionMode.Decompress);
                else if (response1.ContentEncoding.ToLower().Contains("deflate"))
                    responseStream = new System.IO.Compression.DeflateStream(responseStream, System.IO.Compression.CompressionMode.Decompress);
                StreamReader streamReader = new StreamReader(responseStream);


                response =await streamReader.ReadToEndAsync();
                string strOutput = response.ToString();
                responseStream.Close();
                streamReader.Close();
                response1.Close();
            }
            catch (Exception ee)
            {
                _ = _exception.ErrorBooking(ee, "", "Paximum");
                //ExceptionManager.LogException(ee);
                Error = ee.Message.ToString();

                responseDto.Error = Error;
                return responseDto;
            }
            responseDto.Response = response;
            return responseDto;

        }

        public  string Getpaximum_BookingCancelRQ(string AgentVoucherNo, DataRow[] PaximumDetails)
        {
            string RequestString = "";
            try
            {
                Paximum_CancelBooking.Root paxmain = new Paximum_CancelBooking.Root();
                paxmain.bookingId = AgentVoucherNo;


                RequestString = JsonConvert.SerializeObject(paxmain);
                return RequestString;
            }
            catch (Exception ex)
            {
                _=_exception.ErrorNew(ex,ErrorLog._LogAPI.CancelBooking);
            }
            return RequestString;
        }
    }
}
