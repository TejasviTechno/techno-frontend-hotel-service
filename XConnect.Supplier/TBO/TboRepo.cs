using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Formats.Asn1;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Azure;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using MongoDB.Bson.Serialization.Serializers;
using Newtonsoft.Json;
using XConnect.CommonUtilities;
using XConnect.CommonUtilities.Interface;
using XConnect.DAL;
using XConnect.ErrorLogHandler.Interface;
using XConnect.Markup.Interface;
using XConnect.Models;
using XConnect.Models.Hotel;
using XConnect.Supplier.Letsfly;
using XConnect.Supplier.TBO.Interface;
using static Azure.Core.HttpHeader;
using static XConnect.Models.ErrorLog;
using static XConnect.Supplier.Booking.Com.preebookrs;
using static XConnect.Supplier.TBO.TBOClass;
using Options = XConnect.Models.Options;

namespace XConnect.Supplier.TBO
{
    public class TboRepo : ITbo
    {
        private readonly ICommonMethod _commonMethod;
        private readonly IException _exception;
        private readonly IMarkup _markup;
        private readonly ISupplierRes _supplierRes;
        private readonly KeySettings _keySettings;
        private readonly IMemoryCache _cache;
        private readonly SupplierConfiguration _supplierConfig;

        public TboRepo(ICommonMethod commonMethod, IMarkup markup, ISupplierRes supplierRes, IException exception, IOptions<KeySettings> keySettings, IMemoryCache cache, IOptions<SupplierConfiguration> supplierConfig)
        {
            _commonMethod = commonMethod;
            _markup = markup;
            _supplierRes = supplierRes;
            _exception = exception;
            _keySettings = keySettings.Value;
            _cache = cache;
            _supplierConfig = supplierConfig.Value;
        }
        public void GetTBO_V1HolidayRequestOnly_async(DataRow[] commonCode, List<StarRating> listRating, DataTable dtROE, bool IsHotelWiseSearch, bool HitFromAll, HotelSearchParameter SearchPara, List<SupplierDetails> lsttboDetails, List<SupplierSearchConfig> lsttboSearchConfig, DataTable dtPreferredDataHotel, ref ConcurrentBag<HotelX> AllTask, int Hit, TokenData tokenData, DataSet dtXMLCity)
        {
            try
            {
                List<StarRating> lstRating = new List<StarRating>();
                lstRating = _commonMethod.GetStarRatingStringList_xtg(SearchPara);

                List<HotelCodeByCity> DtHotelDAta = HtmlExtensions.ToList<HotelCodeByCity>(commonCode.CopyToDataTable());
                int ratingcount = lstRating.Count;
                int hotelcount = DtHotelDAta.Count;

                foreach (SupplierDetails tboDetails in lsttboDetails)
                {
                    int SupplierID = tboDetails.SupplierID;
                    string Supplier = tboDetails.SupplierName;

                    if (commonCode != null)
                    {
                        if (lsttboSearchConfig.Count > 0)
                        {
                            List<SupplierSearchConfig> tboSearchConfig = lsttboSearchConfig
                                  .Where(x => x != null && x.SupplierID == SupplierID)
                                  .ToList();

                            if (tboSearchConfig != null && tboSearchConfig.Count > 0)
                            {
                                DataForSearch dataForSearch = _commonMethod.GetXmlDataForSearchTechwise(SearchPara.Nationality, dtXMLCity, Supplier, tboDetails.TechName);
                                if (dataForSearch.XmlNationality != "")
                                {
                                    string Ids = string.Empty;
                                    string xmlbase = "";
                                    _commonMethod.GetPrefferedHotelCodes(ref Ids, ref xmlbase, ref tboSearchConfig, Supplier, dtPreferredDataHotel, SearchPara.IsSearchFor);

                                    if (tboSearchConfig != null)
                                    {
                                        List<HotelCodeByCity> filter = new List<HotelCodeByCity>();
                                        filter = _commonMethod.FilterWithOutCity_Code(ratingcount, hotelcount, DtHotelDAta, lstRating, Supplier);

                                        if (filter.Count > 0)
                                        {
                                            string UserName = Convert.ToString(tboDetails.UserName);
                                            string Password = Convert.ToString(tboDetails.Password);

                                            double CurrencyFactorTBO = 1;
                                            string Currency = Convert.ToString(tboDetails.CurrencyShortName);

                                            if (Currency.ToLower() != tokenData.SiteCurrency.ToLower())
                                            {
                                                CurrencyFactorTBO = _commonMethod.GetROEByCurrency(Currency, dtROE);

                                            }

                                            for (int k = 0; k < tboSearchConfig.Count; k++)
                                            {
                                                bool DebugMode = (Convert.ToInt32(tboSearchConfig[k].DebugMode) == 0 ? false : true);
                                                int IsCitywisesearch = Convert.ToInt32(tboSearchConfig[k].City);
                                                if (IsCitywisesearch == 0)//(DtHotelData.DefaultView.Count > 0)
                                                {
                                                    if (Convert.ToInt32(tboSearchConfig[k].CityID) == 0 || Convert.ToInt32(tboSearchConfig[k].CityID) == SearchPara.CityID)
                                                    {
                                                        int Noofrecords = Convert.ToInt32(tboSearchConfig[k].Noofrecords);
                                                        double count = Convert.ToDouble(filter.Count);
                                                        int NoOfDiv = Convert.ToInt32(Math.Ceiling(count / Convert.ToDouble(Noofrecords)));
                                                        List<List<XMLID_Search>> lstxml = _commonMethod.GetXMLIDString(NoOfDiv, Noofrecords, count, filter);

                                                        for (int a = 0; a < NoOfDiv; a++)
                                                        {

                                                            string IdsForSearch = Convert.ToString(_commonMethod.GetHotelCodeByCOmma(lstxml[a]));
                                                            HotelX Xhotel = new HotelX();
                                                            Xhotel.Supplier = Supplier;
                                                            Xhotel.DebugeMode = DebugMode;
                                                            Xhotel.SearchTimeOut = Convert.ToInt32(tboSearchConfig[k].HotelSearchTimeOut);
                                                            //if (Hit > 0)
                                                            Xhotel.ProcessTimeout = Convert.ToInt32(tboSearchConfig[k].HotelSearchTimeOut2);

                                                            if (_keySettings.SupplierProxy == "0")
                                                                Xhotel.URL = Convert.ToString(tboDetails.WebSiteURL) + _supplierConfig.TBO.V1Search;
                                                            // Xhotel.URL = Convert.ToString(tboDetails.WebSiteURL) + TboInfo.Tbo_V1Search;
                                                            else
                                                                Xhotel.URL = Convert.ToString(tboDetails.WebSiteURLforAPI) + _supplierConfig.TBO.V1Search;

                                                            Xhotel.UserId = Convert.ToString(tboDetails.UserID);
                                                            Xhotel.UserName = UserName;
                                                            Xhotel.PassWord = Password;
                                                            Xhotel.CurrencyCode = Currency;
                                                            Xhotel.CurrencyFactor = CurrencyFactorTBO;
                                                            Xhotel.XmlCity = dataForSearch.xmlCity;
                                                            Xhotel.XmlCountry = "";
                                                            Xhotel.XmlNationality = dataForSearch.XmlNationality;
                                                            string PaxPassport = dataForSearch.XmlNationality;
                                                            Xhotel.AgentWiseMarkup = tboDetails.AgentWiseMarkup;
                                                            Xhotel.IsShowSupplier = Convert.ToInt16(tboDetails.IsShowSupplier) == 1 ? true : false;
                                                            Xhotel.SupplierID = tboDetails.SupplierID;
                                                            //Xhotel.MasterSupplierID = tbo_v1Details[0].MasterSupplierId;
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
                                                                objrq.Request = (XMLTBO_v1HolidaysRequestOnly_async(Xhotel.URL, Xhotel.UserId, Xhotel.UserName, Xhotel.PassWord, PaxPassport, Xhotel.SearchTimeOut, SearchPara, IdsForSearch, CurrencyFactorTBO, DebugMode, Xhotel.XmlCity, Xhotel.XmlCountry, Xhotel.CurrencyCode));

                                                                objrq.Response = ""; RQRS.Add(objrq);
                                                            }
                                                            Xhotel.RQRS = RQRS;
                                                            Xhotel.ReturnRooms = Convert.ToInt32(tboDetails.ReturnRooms);
                                                            Xhotel.TechName = Convert.ToString(tboDetails.TechName);
                                                            #endregion
                                                            AllTask.Add(Xhotel);
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    string IdsForSearch = "";
                                                    HotelX Xhotel = new HotelX();
                                                    Xhotel.Supplier = Supplier;
                                                    Xhotel.DebugeMode = DebugMode;
                                                    Xhotel.SearchTimeOut = Convert.ToInt32(tboSearchConfig[k].HotelSearchTimeOut);
                                                    //if (Hit > 0)
                                                    Xhotel.ProcessTimeout = Convert.ToInt32(tboSearchConfig[k].HotelSearchTimeOut2);


                                                    if (_keySettings.SupplierProxy == "0")
                                                        Xhotel.URL = Convert.ToString(tboDetails.WebSiteURL) + _supplierConfig.TBO.V1Search;
                                                    else
                                                        Xhotel.URL = Convert.ToString(tboDetails.WebSiteURLforAPI) + _supplierConfig.TBO.V1Search;

                                                    Xhotel.UserId = Convert.ToString(tboDetails.UserID);
                                                    Xhotel.UserName = UserName;
                                                    Xhotel.PassWord = Password;
                                                    Xhotel.CurrencyCode = Currency;
                                                    Xhotel.CurrencyFactor = CurrencyFactorTBO;
                                                    Xhotel.XmlCity = dataForSearch.xmlCity;
                                                    Xhotel.XmlCountry = "";
                                                    Xhotel.XmlNationality = dataForSearch.XmlNationality;
                                                    string PaxPassport = dataForSearch.XmlNationality;
                                                    Xhotel.AgentWiseMarkup = tboDetails.AgentWiseMarkup;
                                                    Xhotel.IsShowSupplier = Convert.ToInt16(tboDetails.IsShowSupplier) == 1 ? true : false;
                                                    Xhotel.SupplierID = tboDetails.SupplierID;
                                                    //Xhotel.MasterSupplierID = tbo_v1Details[0].MasterSupplierId;
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
                                                        objrq.Request = (XMLTBO_v1HolidaysRequestOnly_async(Xhotel.URL, Xhotel.UserId, Xhotel.UserName, Xhotel.PassWord, PaxPassport, Xhotel.SearchTimeOut, SearchPara, IdsForSearch, CurrencyFactorTBO, DebugMode, Xhotel.XmlCity, Xhotel.XmlCountry, Xhotel.CurrencyCode));

                                                        objrq.Response = ""; RQRS.Add(objrq);
                                                    }
                                                    Xhotel.RQRS = RQRS;
                                                    Xhotel.ReturnRooms = Convert.ToInt32(tboDetails.ReturnRooms);
                                                    Xhotel.TechName = Convert.ToString(tboDetails.TechName);
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
            catch (Exception ex)
            {
                _exception.Error(ex);
            }
        }

        public string XMLTBO_v1HolidaysRequestOnly_async(string JCUrl, string TBOClientID, string TBOUserName, string TBOPassword, string PaxPassport, Int32 SearchTimeOut, HotelSearchParameter SearchParam, string TBOHotelIds, double CurrencyFactorTBO, bool DebugMode, string CityCode, string CountryCode, string CurrencyCode)
        {
            string TBOPriceOnly = "";
            try
            {
                #region PriceOnly RQ
                TBOPriceOnly = Get_TBOHolidays_PriceOnlyjson(SearchParam, SearchTimeOut, TBOClientID, TBOUserName, TBOPassword, PaxPassport, CityCode, CountryCode, TBOHotelIds, JCUrl, CurrencyCode);

                #endregion
            }
            catch (Exception ex)
            {
                _exception.Error(ex);

            }
            return TBOPriceOnly;
        }

        public string Get_TBOHolidays_PriceOnlyjson(HotelSearchParameter SearchParam, Int32 SearchTimeOut, string TBOClientID, string TBOUserName, string TBOPassword, string GTANatinality, string TBOHoliday_RegionID, string CountryCode, string TBOHotelIds, string JCUrl, string CurrencyCode)
        {
            try
            {
                #region Price Only and Room Only RQ
                string CheckIn = SearchParam.CheckInDate;
                //CheckIn = CheckIn.Substring(6, 4) + "-" + CheckIn.Substring(0, 2) + "-" + CheckIn.Substring(3, 2);
                CheckIn = CheckIn.Substring(6, 4) + "-" + CheckIn.Substring(3, 2) + "-" + CheckIn.Substring(0, 2);
                //CheckIn = CheckIn.Substring(0, 2) + "-" + CheckIn.Substring(3, 2) + "-" + CheckIn.Substring(6, 4);

                string CheckOut = SearchParam.CheckOutDate;
                //CheckOut = CheckOut.Substring(6, 4) + "-" + CheckOut.Substring(0, 2) + "-" + CheckOut.Substring(3, 2);
                CheckOut = CheckOut.Substring(6, 4) + "-" + CheckOut.Substring(3, 2) + "-" + CheckOut.Substring(0, 2);
                // CheckOut = CheckOut.Substring(0, 2) + "-" + CheckOut.Substring(3, 2) + "-" + CheckOut.Substring(6, 4);

                string XMLCountryName = SearchParam.CountryCode;
                string XMLCityName = SearchParam.CityName;
                int XMLCITY = SearchParam.CityID;
                bool IsDetailResponse = true;
                int MinValue = SearchParam.StarStart;
                #region Price Only and Room Only RQ
                string Request = "";
                // JsonSerializerSettings Setting = new JsonSerializerSettings();
                TBOClass.hotelserach.Root RootObject = new TBOClass.hotelserach.Root();
                List<TBOClass.hotelserach.PaxRoom> RoomGuest = new List<TBOClass.hotelserach.PaxRoom>();

                RootObject.CheckIn = CheckIn;//SearchParam.CheckInDate;
                RootObject.CheckOut = CheckOut; //SearchParam.CheckOutDate;
                string HID = "";
                if (TBOHotelIds.Trim() != string.Empty)
                {
                    string[] HotelIds = Convert.ToString(TBOHotelIds).Split(',');
                    StringBuilder ItemCodeString = new StringBuilder();
                    for (int htid = 0; htid < HotelIds.Length; htid++)
                    {
                        if (htid == 0)
                            HID = HotelIds[htid];
                        else
                            HID = HID + "," + HotelIds[htid];
                    }
                }
                RootObject.HotelCodes = HID;
                if (TBOHotelIds.Trim() == string.Empty)
                {
                    RootObject.CityCode = TBOHoliday_RegionID;
                }
                RootObject.CityName = XMLCityName;
                RootObject.CountryName = XMLCountryName;
                RootObject.GuestNationality = GTANatinality;
                RootObject.PreferredCurrencyCode = CurrencyCode;
                List<TBOClass.hotelserach.PaxRoom> RoomGuests = new List<TBOClass.hotelserach.PaxRoom>();
                for (int room = 0; room < SearchParam.Rooms.Count; room++)
                {

                    int Adult = SearchParam.Rooms[room].NoofAdults;
                    int NoofChild = SearchParam.Rooms[room].NoofChild;
                    //string childage = SearchParam.Rooms[room].;
                    TBOClass.hotelserach.PaxRoom child = new TBOClass.hotelserach.PaxRoom();
                    child.Adults = Adult;

                    int age = SearchParam.Rooms[room].NoofChild;
                    int child1 = Convert.ToInt32(SearchParam.Rooms[room].Child1Age);
                    int child2 = Convert.ToInt32(SearchParam.Rooms[room].Child2Age);

                    List<int> childAge = new List<int>();
                    if (NoofChild > 0)
                    {

                        switch (age)
                        {
                            case 1:
                                {
                                    childAge.Add(Convert.ToInt32(SearchParam.Rooms[room].Child1Age));
                                    break;
                                }
                            case 2:
                                {
                                    childAge.Add(Convert.ToInt32(SearchParam.Rooms[room].Child1Age));
                                    childAge.Add(Convert.ToInt32(SearchParam.Rooms[room].Child2Age));
                                    break;
                                }

                        }
                        child.Children = NoofChild;
                        child.ChildrenAges = childAge;
                    }
                    RoomGuests.Add(child);
                }
                RootObject.PaxRooms = RoomGuests;
                RootObject.IsDetailedResponse = IsDetailResponse;
                RootObject.ResponseTime = SearchTimeOut / 1000;
                TBOClass.hotelserach.Filters Filter = new TBOClass.hotelserach.Filters();

                Filter.MealType = "All";
                Filter.NoOfRooms = 0;//SearchParam.Rooms.Count();
                Filter.StarRating = "All";
                RootObject.Filters = Filter;

                JsonSerializerSettings Setting = new JsonSerializerSettings();
                Setting.NullValueHandling = NullValueHandling.Ignore;
                Setting.DefaultValueHandling = DefaultValueHandling.Ignore;
                Request = JsonConvert.SerializeObject(RootObject, Setting);
                return Request;

                #endregion
                #endregion
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
            }
            return string.Empty;
        }

        public async Task<List<Options>> TBO_V1HotelSearchWithRooms(HotelX Item, CommonSearch commonSearch, string ResponseData, HotelSearchParameter SearchParam, string XMLCITY, string XMLCOUNTRY, string Currency, double CurrencyFactor, Int32 processTimeout, string ResProccessTimerLogActive, string ResProccessTimeValueforSaveLog, string StoreSupplierResErrorLog)
        {
            System.Diagnostics.Stopwatch timerroomx = new System.Diagnostics.Stopwatch();
            timerroomx.Start();
            List<Options> options = new List<Options>();
            try
            {
                DataTable DtHotel = _commonMethod.GenerateDataTableNewV2();
                {
                    TBOClass.hotelroomsearchres.Rootobject DS = null;
                    try
                    {

                        DS = Newtonsoft.Json.JsonConvert.DeserializeObject<TBOClass.hotelroomsearchres.Rootobject>(ResponseData);

                    }
                    catch (Exception ex)
                    {
                        await _exception.Error(ex);
                    }
                    if (DS == null || DS.HotelResult == null)
                    {
                        Item.ResponseType = "NoCount";
                        return options;
                    }

                    string Supplier = Item.Supplier;
                    double NetPrice = 0;
                    string currency = "", BookingCode = "";
                    string RoomtypeID = ""; // + "%%%" + strroomcombination;
                    string RoomID = "";

                    string RoomName = "";
                    string SuppChargeType = "";

                    string SupplementName = string.Empty;
                    //SuppId = 0;
                    SuppChargeType = string.Empty;

                    string Type = "";

                    string Board_Text = "";
                    int PropertyID = 0;
                    string OfferCode = string.Empty;
                    string OfferDescription = string.Empty;
                    string EssentialInformation = string.Empty;
                    string uniqid = Guid.NewGuid().ToString();

                    double CompanyBuying = 0;

                    bool IsRefundable = false;
                    string LastCancellationDeadline = "";

                    DateTime CancellationDeadline = DateTime.Now.Date;

                    for (int k = 0; k < DS.HotelResult.Length; k++)
                    {
                        Item.ResponseType = "Success";
                        double FinalMinPrice = 0;
                        #region "When not Need Room"  
                        // add hotel code from price only and room code blank here
                        if (SearchParam.Filters.IsShowRooms == 0)
                        {
                            #region "Hotel Code Block "
                            DataRow drXmlHotel = DtHotel.NewRow();
                            currency = DS.HotelResult[k].Currency;
                            // LowestRate = 0;
                            drXmlHotel["XMLCountry"] = XMLCOUNTRY;
                            drXmlHotel["XMLCity"] = XMLCITY;
                            drXmlHotel["XmlHotelCity"] = XMLCITY;
                            drXmlHotel["XMLHotelCode"] = Convert.ToString(DS.HotelResult[k].HotelCode);
                            drXmlHotel["HotelName"] = "";
                            drXmlHotel["SupplierName"] = Supplier;
                            drXmlHotel["OfficeID"] = 0;
                            drXmlHotel["ContractID"] = 0;
                            drXmlHotel["Rating"] = 0;
                            drXmlHotel["Reviews"] = 0;
                            drXmlHotel["SupplierCurrency"] = currency;
                            drXmlHotel["UniqueNo"] = Convert.ToString(DS.HotelResult[k].HotelCode);
                            drXmlHotel["IsShowSupplier"] = false;
                            drXmlHotel["BookingStatus"] = "Available";

                            for (int off = 0; off < DS.HotelResult[k].Rooms.Length; off++)
                            {
                                BookingCode = Convert.ToString(DS.HotelResult[k].Rooms[off].BookingCode);


                                double Pricenew = Convert.ToDouble(Convert.ToDouble(DS.HotelResult[k].Rooms[off].TotalFare) * CurrencyFactor);

                                if (off == 0)
                                {
                                    NetPrice = Pricenew;
                                }
                                else if (NetPrice > Pricenew)
                                {
                                    NetPrice = Pricenew;
                                }
                                // NetPrice = NetPrice * Convert.ToInt32(SearchParam.NoofNights) * Convert.ToInt32(SearchParam.Rooms.Count);


                            }

                            drXmlHotel["FinalSellingPrice"] = NetPrice;
                            drXmlHotel["SellingPrice"] = NetPrice;
                            drXmlHotel["IsRecomondedHotel"] = false;
                            //// if (FinalPrice > 0)
                            DtHotel.Rows.Add(drXmlHotel);

                            #endregion


                            if (NetPrice > 0)
                            {
                                #region "Room code Block"
                                bool willAddOrNot = true;
                                Int64 RaynaID = 0;
                                Int64 GiataId = 0;
                                int Rating = 0;
                                Int64 OwnsystemHotelId = 0;
                                double MinPrice = NetPrice;
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
                        #endregion
                        #region "When Need Rooms"

                        //add Hotel code blank and rooms code in room block
                        else
                        {
                            #region Hotels

                            #region "Hotel Block here"
                            DataRow drXmlHotel = DtHotel.NewRow();
                            // LowestRate = 0;
                            drXmlHotel["XMLCountry"] = XMLCOUNTRY;
                            drXmlHotel["XMLCity"] = XMLCITY;
                            drXmlHotel["XmlHotelCity"] = XMLCITY;
                            drXmlHotel["XMLHotelCode"] = Convert.ToString(DS.HotelResult[k].HotelCode);
                            drXmlHotel["HotelName"] = "";
                            drXmlHotel["SupplierName"] = Supplier;
                            drXmlHotel["OfficeID"] = 0;
                            drXmlHotel["ContractID"] = 0;
                            drXmlHotel["Rating"] = 0;
                            drXmlHotel["Reviews"] = 0;
                            drXmlHotel["SupplierCurrency"] = DS.HotelResult[k].Currency;
                            drXmlHotel["UniqueNo"] = Convert.ToString(DS.HotelResult[k].HotelCode);
                            drXmlHotel["IsShowSupplier"] = false;
                            drXmlHotel["BookingStatus"] = "Available";

                            drXmlHotel["FinalSellingPrice"] = 1;
                            drXmlHotel["SellingPrice"] = 1;
                            drXmlHotel["IsRecomondedHotel"] = false;
                            //// if (FinalPrice > 0)
                            DtHotel.Rows.Add(drXmlHotel);


                            #endregion
                            #region "GetRooms for this hotel"
                            bool willAddOrNot = true;
                            Int64 RaynaID = 0;
                            Int64 GiataId = 0;
                            int RatingHotel = 0;
                            Int64 OwnsystemHotelId = 0;
                            string Provider = Supplier;
                            double MinPrice = 1;

                            _markup.GetHotelDataCommon(ref MinPrice, ref willAddOrNot, ref OwnsystemHotelId, ref RaynaID, ref GiataId, ref RatingHotel, ref drXmlHotel, commonSearch.AgentId, commonSearch.TariffID, commonSearch.dsAgentDetails, commonSearch.supplierGiataMaster, SearchParam, commonSearch.dtCountryCityMarkup, commonSearch.B2CMarkup, commonSearch.IsParentId, commonSearch.SubAgentMarkup, commonSearch.SearchByHotel, commonSearch.HotelIDS);
                            if (willAddOrNot)
                            {
                                DataTable dtHotelRoom = _commonMethod.GenerateRoomtypeDataTableNewV2();
                                DataTable dtCancellation = _commonMethod.GenerateCancellationDataTableNewV2();
                                DataTable DtRoomRateBreackUp = _commonMethod.GenerateoomRateBreackUpDataTableNewV2();

                                int CutOffDays = CacheMaster.CutOffDays;
                                CutOffDays = _commonMethod.GetCutOffDays(commonSearch.dtSuplierMarkup, Provider);
                                int NoOfNight = SearchParam.NoofNights;
                                long tickNow = DateTime.Now.Ticks;
                                #region "Room Logic"
                                if (SearchParam.Filters.IsShowRooms == 1)
                                {
                                    double AgentPercentage = 0.0;
                                    // Boolean blnIsShowSupplier = false;
                                    double AgentMarkupAmount = 0.0;
                                    Boolean isPerce = true;

                                    double AgentCompanyPercentage = Convert.ToDouble(SearchParam.AgentCompanyPercentage);
                                    bool IsB2C = Convert.ToBoolean(Convert.ToByte(SearchParam.IsB2BOrB2C));
                                    bool blnIsShowSupplier = false;
                                    _markup.getAgetMarkupSetting(Provider, GiataId.ToString(), commonSearch.dtSuplierMarkup, commonSearch.dtMarkupDetails, ref AgentPercentage, ref AgentMarkupAmount, ref blnIsShowSupplier, ref isPerce, commonSearch.AgentId, RatingHotel, SearchParam.CityID, SearchParam.CountryID.ToString(), SearchParam.CheckInDate);
                                    int maxOcupancy = 0;
                                    int noofadults = 0;
                                    int noofchilds = 0;
                                    for (int x = 0; x < SearchParam.Rooms.Count; x++)
                                    {
                                        int ADT = SearchParam.Rooms[x].NoofAdults;
                                        int CHD = SearchParam.Rooms[x].NoofChild;
                                        noofadults = ADT + noofadults;
                                        noofchilds = noofchilds + CHD;
                                    }

                                    for (int i = 0; i < SearchParam.Rooms.Count; i++)
                                        maxOcupancy = maxOcupancy + SearchParam.Rooms[i].NoofChild + SearchParam.Rooms[i].NoofAdults;

                                    //for (int k = 0; k < DS.HotelResult.Count(); k++)
                                    {
                                        Currency = DS.HotelResult[k].Currency;
                                        for (int R = 0; R < DS.HotelResult[k].Rooms.Count(); R++)
                                        {

                                            string RoomName1 = "";
                                            for (int dr = 0; dr < DS.HotelResult[k].Rooms[R].Name.Length; dr++)
                                            {
                                                string RoomNme = Convert.ToString(DS.HotelResult[k].Rooms[R].Name[dr]);
                                                RoomName1 = RoomName1 + "(" + (dr + 1) + ")" + RoomNme + " ";
                                            }


                                            RoomName = RoomName1;

                                            RoomID = Convert.ToString(DS.HotelResult[k].Rooms[R].BookingCode);
                                            Board_Text = Convert.ToString(DS.HotelResult[k].Rooms[R].MealType);
                                            CompanyBuying = Convert.ToDouble(DS.HotelResult[k].Rooms[R].TotalFare);
                                            IsRefundable = Convert.ToBoolean(DS.HotelResult[k].Rooms[R].IsRefundable);


                                            if (DS.HotelResult[k].Rooms[R].RoomPromotion != null)
                                            {
                                                if (DS.HotelResult[k].Rooms[R].RoomPromotion.Length > 0)
                                                {
                                                    OfferDescription = Convert.ToString(DS.HotelResult[k].Rooms[R].RoomPromotion[0]);
                                                }
                                            }
                                            RoomtypeID = RoomID + "$$$" + Board_Text + "$$$" + tickNow;//+"$$$"+ IsSupplement+"$$$"+SuppId+"$$$"+ SupplementPrice+"$$$"+ SuppChargeType+"$$$"+ SupplementName+"$$$"+ SupplementCurrency+"$$$"+SupplementDeatils;
                                            #region RateBreakUp
                                            //for (int n = 0; n < NoOfNight; n++)
                                            ////for (int n = 0; n < DS.HotelResult[k].Rooms[R].DayRates[0].Length; n++)
                                            //{

                                            //    DateTime Day = DateTime.ParseExact(SearchParam.CheckInDate, "MM/dd/yyyy", null);
                                            //    Day = Day.AddDays(n);
                                            //    string Days = Convert.ToString(Day);
                                            //    string rateWeekColumn = Convert.ToString(Day.Date.DayOfWeek).Substring(0, 3);
                                            //    // Double dayilyrates = DS.HotelResult[k].Rooms[R].DayRates[0][n].BasePrice;
                                            //    #region Vat
                                            //    Double VatOnRateBrackUp = 0;
                                            //    double PriceRoom = _markup.ProcessHotelRateBreakupPrice_WithMarkup(1, SearchParam.CityID, SearchParam.CountryID.ToString(), /*dayilyrates*/ CompanyBuying / NoOfNight, commonSearch.B2CMarkup, commonSearch.IsParentId, isPerce, AgentPercentage, AgentMarkupAmount, commonSearch.SubAgentMarkup, ref VatOnRateBrackUp);
                                            //    #endregion

                                            //    DataRow drWHL_RateBreackUp = DtRoomRateBreackUp.NewRow();
                                            //    drWHL_RateBreackUp["SupplierName"] = Provider;
                                            //    drWHL_RateBreackUp["OwnsystemHotelId"] = Convert.ToString(DS.HotelResult[k].HotelCode); ;
                                            //    drWHL_RateBreackUp["ContractID"] = PropertyID;
                                            //    drWHL_RateBreackUp["RoomTypeName"] = RoomName;
                                            //    drWHL_RateBreackUp["RoomTypeId"] = RoomtypeID;
                                            //    drWHL_RateBreackUp["MealId"] = Board_Text;
                                            //    drWHL_RateBreackUp["Meal"] = Board_Text;
                                            //    drWHL_RateBreackUp["DateOfWeek"] = rateWeekColumn;
                                            //    drWHL_RateBreackUp["Date"] = Days;
                                            //    drWHL_RateBreackUp["Rate"] = PriceRoom;
                                            //    drWHL_RateBreackUp["RateType"] = "A Rate";
                                            //    drWHL_RateBreackUp["RoomNo"] = "1";
                                            //    drWHL_RateBreackUp["RoomSequenceNo"] = uniqid;
                                            //    drWHL_RateBreackUp["BookingStatus"] = "Available";
                                            //    drWHL_RateBreackUp["SupplierCurrency"] = Currency;
                                            //    drWHL_RateBreackUp["BuyingPrice"] = PriceRoom;
                                            //    DtRoomRateBreackUp.Rows.Add(drWHL_RateBreackUp);
                                            //}
                                            #endregion

                                            #region Rooms
                                            DataRow drWHLRoom = dtHotelRoom.NewRow();
                                            //drWHLRoom["OwnSystemHotelId"] = Convert.ToString(DS.HotelResult[k].HotelCode);//yushi
                                            //string OwnsystemHotelId = Convert.ToString(DS.HotelResult[k].HotelCode); yushi
                                            drWHLRoom["OwnSystemHotelId"] = OwnsystemHotelId;
                                            drWHLRoom["RoomTypeName"] = RoomName;
                                            drWHLRoom["RoomTypeCode"] = RoomtypeID;
                                            drWHLRoom["MealId"] = Board_Text;
                                            drWHLRoom["MealName"] = Board_Text;
                                            drWHLRoom["ContractID"] = PropertyID;
                                            drWHLRoom["BoardType"] = Board_Text;
                                            drWHLRoom["RoomTypeId"] = RoomtypeID;
                                            drWHLRoom["AdultsOccupancy"] = noofadults;
                                            drWHLRoom["ChildOccupancy"] = noofchilds;
                                            drWHLRoom["MaxExtraBed"] = "0";
                                            drWHLRoom["RoomNo"] = "1";
                                            drWHLRoom["RoomSequenceNo"] = uniqid;
                                            drWHLRoom["Type"] = Type;
                                            drWHLRoom["BookingStatus"] = "Available";
                                            drWHLRoom["UniqueNo"] = uniqid;
                                            drWHLRoom["OfferDescription"] = OfferDescription;
                                            drWHLRoom["OfferCode"] = OfferCode;
                                            drWHLRoom["EssentialInformation"] = EssentialInformation;
                                            double ItemPrice = Convert.ToDouble(CompanyBuying);

                                            _markup.ProcessHotelRoom_WithMarkup(CurrencyFactor, Currency, false, true, SearchParam.NoofNights, SearchParam.CityID, SearchParam.CountryID.ToString(), SearchParam.CheckInDate, SearchParam.CheckOutDate, ref ItemPrice, ref drWHLRoom, isPerce, AgentPercentage, AgentMarkupAmount, commonSearch.SubAgentMarkup, commonSearch.B2CMarkup, commonSearch.IsParentId);

                                            drWHLRoom["SupplierName"] = Provider;
                                            dtHotelRoom.Rows.Add(drWHLRoom);
                                            #endregion

                                            #region cancellation policy New Logic
                                            string CheckIn = SearchParam.CheckInDate;
                                            CheckIn = CheckIn.Substring(6, 4) + "-" + CheckIn.Substring(0, 2) + "-" + CheckIn.Substring(3, 2);
                                            bool isrefundable = DS.HotelResult[k].Rooms[R].IsRefundable;
                                            if (!isrefundable)
                                            {

                                                dtCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref ItemPrice, dtCancellation.NewRow(), CurrencyFactor, Currency, Provider, Convert.ToString(OwnsystemHotelId), Convert.ToString("1"),
                                                    Convert.ToString(RoomtypeID), RoomName, Board_Text, DateTime.Now.Date, DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null), ItemPrice, ItemPrice, "0", 0, uniqid, "Yes", "", Type, "", ""));
                                                dtCancellation.AcceptChanges();

                                            }
                                            else
                                            {
                                                if (DS.HotelResult[k].Rooms[R].CancelPolicies != null)
                                                {
                                                    // var sortedPolicies = DS.HotelResult[k].Rooms[R].CancelPolicies.OrderBy(p => p.FromDate).ToList();
                                                    var sortedPolicies = DS.HotelResult[k].Rooms[R].CancelPolicies.Where(p => p.CancellationCharge != 0).OrderBy(p => p.FromDate).ToList();
                                                    //var sortedPolicies = DS.HotelResult[k].Rooms[R].CancelPolicies.Where(p => p.CancellationCharge != 0).OrderBy(p => p.FromDate).ThenBy(p => p.CancellationCharge).ToList();

                                                    DateTime lastdate = DateTime.Now.Date;
                                                    double CancellationPrice = ItemPrice;


                                                    for (int v = 0; v < sortedPolicies.Count(); v++)
                                                    {
                                                        //string ChargeType = Convert.ToString(sortedPolicies[v].ChargeType);
                                                        //DateTime dateString1 = DateTime.ParseExact(sortedPolicies[v].FromDate, "dd/MM/yyyy", CultureInfo.InvariantCulture).Date; //Convert.ToDateTime(sortedPolicies[v].FromDate);
                                                        string dateString = Convert.ToString(sortedPolicies[v].FromDate);
                                                        dateString = dateString.Substring(0, 2) + "/" + dateString.Substring(3, 2) + "/" + dateString.Substring(6, 4);
                                                        DateTime dateString11 = DateTime.ParseExact(dateString, "dd/MM/yyyy", CultureInfo.InvariantCulture).Date; //Convert.ToDateTime(sortedPolicies[v].FromDate);

                                                        if (v == 0)
                                                        {
                                                            lastdate = dateString11;
                                                        }
                                                        else
                                                        {
                                                            if (lastdate > dateString11)
                                                            {
                                                                lastdate = dateString11;
                                                            }
                                                        }
                                                    }


                                                    LastCancellationDeadline = lastdate.ToString("dd/MM/yyyy");//Convert.ToString(lastdate.Date);

                                                    #region Vat
                                                    double VatOnPolicy = 0;
                                                    double CompanyChargeAmount = CancellationPrice;
                                                    double MainAgentCharge = CancellationPrice;
                                                    //CancellationPrice = _markup.ProcessHotelCancelationPrice_WithMarkup(ref MainAgentCharge, SearchParam.NoofNights, SearchParam.CityID, SearchParam.CountryID.ToString(), CancellationPrice, isPerce, commonSearch.B2CMarkup, AgentPercentage, AgentMarkupAmount, commonSearch.IsParentId, commonSearch.SubAgentMarkup, ref VatOnPolicy);

                                                    #endregion

                                                    DateTime NowDate = DateTime.Now;

                                                    CancellationDeadline = DateTime.ParseExact(LastCancellationDeadline, "dd/MM/yyyy", null);
                                                    //if (CancellationDeadline < DateTime.Now.Date && Convert.ToDouble(sortedPolicies[v].CancellationCharge) == 0)
                                                    //{
                                                    //    CancellationDeadline = DateTime.Now.Date;
                                                    //}
                                                    DateTime CancelDate = CancellationDeadline;
                                                    if (CancelDate.AddDays(CutOffDays).Date > DateTime.Now.Date)
                                                        CancelDate = CancelDate.AddDays(CutOffDays).Date;
                                                    else
                                                    {
                                                        CancelDate = DateTime.Now.Date;
                                                        MainAgentCharge = ItemPrice;
                                                        CancellationPrice = ItemPrice;
                                                        CompanyChargeAmount = ItemPrice;
                                                    }



                                                    if (NowDate.Date < CancelDate.Date)
                                                    {
                                                        //if (CancellationPrice == 0)
                                                        {
                                                            double zero = 0;
                                                            dtCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref zero, dtCancellation.NewRow(), CurrencyFactor, Currency, Provider, Convert.ToString(OwnsystemHotelId), Convert.ToString("1"),
                                                                   Convert.ToString(RoomtypeID), RoomName, Board_Text, NowDate, CancelDate.AddDays(-1), 0, 0, "0", VatOnPolicy, uniqid, "NO", LastCancellationDeadline, Type, "", ""));
                                                            dtCancellation.AcceptChanges();
                                                        }

                                                        dtCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref MainAgentCharge, dtCancellation.NewRow(), CurrencyFactor, Currency, Provider, Convert.ToString(OwnsystemHotelId), Convert.ToString("1"),
                                                               Convert.ToString(RoomtypeID), RoomName, Board_Text, Convert.ToDateTime(CancelDate).Date, DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null), CompanyChargeAmount, MainAgentCharge, "0", VatOnPolicy, uniqid, "NO", LastCancellationDeadline, Type, "", ""));
                                                        dtCancellation.AcceptChanges();
                                                        //break;

                                                    }
                                                    else
                                                    {
                                                        dtCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref MainAgentCharge, dtCancellation.NewRow(), CurrencyFactor, Currency, Provider, Convert.ToString(OwnsystemHotelId), Convert.ToString("1"),
                                                        Convert.ToString(RoomtypeID), RoomName, Board_Text, DateTime.Now.Date, DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null), CompanyChargeAmount, MainAgentCharge, "0", VatOnPolicy, uniqid, "Yes", "", Type, "", ""));
                                                        dtCancellation.AcceptChanges();
                                                        //break;
                                                    }
                                                    //}
                                                }
                                                else
                                                {
                                                    dtCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref ItemPrice, dtCancellation.NewRow(), CurrencyFactor, Currency, Provider, Convert.ToString(OwnsystemHotelId), Convert.ToString("1"),
                                                        Convert.ToString(RoomtypeID), RoomName, Board_Text, DateTime.Now.Date, DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null), ItemPrice, ItemPrice, "0", 0, uniqid, "Yes", "", Type, "", ""));
                                                    dtCancellation.AcceptChanges();
                                                }
                                            }
                                            #endregion
                                        }

                                    }


                                }
                                #endregion
                                Options ops = (await _supplierRes.GetCommonRoomForSuppliers(GiataId, commonSearch.tokenData, commonSearch.advancedOptions, MinPrice, drXmlHotel, dtHotelRoom, dtCancellation, DtRoomRateBreackUp, SearchParam));
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
                await _exception.Error(ex);
                return options;
            }
            finally
            {
                #region ResProcessTimer
                try
                {
                    if (SearchParam != null && ResProccessTimerLogActive == "1" && timerroomx.ElapsedMilliseconds > Convert.ToInt32(ResProccessTimeValueforSaveLog))
                    {
                        timerroomx.Stop();
                        SearchParam.SupplierTime += ",|Finish:TBO Process Response  " + "|Time :" + timerroomx.Elapsed.ToString();
                        _ = Task.Run(() => _commonMethod.FireAway_SQL_ResPorcessTimeLog("", timerroomx.Elapsed.ToString(), "TBO", SearchParam.SearchKey, 1, SearchParam.AgentID));
                    }
                }
                catch (Exception x)
                {
                    await _exception.Error(x);
                }
                #endregion


            }
            //});

        }

        public async Task<ResponseRecheckTbo> TboApi_cancellation_For_Recheck_OnlyRead_EssInfo(string Type, string ownsystemid, string roomtypeid, DataTable dtSuplierMarkup, string provider, string CompanyBuyPrice, int AgentId
        , string TBOUrl, string Username, string password, string hotelid, string CheckIn, HotelSearchParameter objSearPera,string SearchKey)
        {
            ResponseRecheckTbo responseRecheckTbo = new ResponseRecheckTbo();
            string ISBooking = "NotAvailable";
            string pricecheck = "";
            try
            {


                string responsetype = "Success";
                string time = "";
                string SupplierName = provider;
                string Error = "";
                string PriceCheckRequest = "";
                string ResponseData = "";
                int SearchTimeOut = 20000;

                var roomid = roomtypeid;
                DataRow[] drSuplierMarkup = (dtSuplierMarkup.Select("SupplierName='" + provider + "'"));
                int NoOfRoom = objSearPera.Rooms.Count();
                string RoomName = "";


                string xslRequest = Get_TBO_PriceCheckRQ(TBOUrl, drSuplierMarkup, Username, password, hotelid, CheckIn, "", roomid, "", ref RoomName, "", 0, hotelid, NoOfRoom);
                PriceCheckRequest = xslRequest;
                _commonMethod.ThirdPartyGetPolicyHistory(Convert.ToString(roomtypeid), Convert.ToString(ownsystemid), xslRequest, Convert.ToString("-"), "Before Get Policy & Price Check", "tbo", "Response", "-", "");

                if (xslRequest != "")
                {
                    System.Diagnostics.Stopwatch timerroomprocess = new System.Diagnostics.Stopwatch();
                    timerroomprocess.Start();

                    //string ResponseData1 =  _commonMethod.GetTrvcoResponse_report(ref Error, xslRequest, TBOUrl, SearchTimeOut, "TBO");
                    ResponseDto responseDto = await _commonMethod.GetTrvcoResponse_report(xslRequest, TBOUrl, SearchTimeOut, "TBO");

                    string ResponseData1 = responseDto.Response;
                    ResponseData = responseDto.Response;
                    Error = responseDto.Error;


                    timerroomprocess.Stop();
                    time = timerroomprocess.Elapsed.ToString();
                    DataSet TBOCanPolResponse = _commonMethod.GetDataSetFromXMLString(ResponseData1);

                    if (Convert.ToString(TBOCanPolResponse.Tables["Status"].Rows[0]["StatusCode"]) != "01")
                    {
                        try
                        {
                            responsetype = "GeneralError";
                            string abc = await _commonMethod.SupplierPerformanceReportForRecheckOnly(SupplierName, "PreBook", AgentId, time, responsetype, PriceCheckRequest, ResponseData, Error);
                        }
                        catch (Exception e)
                        {
                            _ = _exception.ErrorNew(e, _LogAPI.Recheck, SearchKey);
                        }
                        responseRecheckTbo.Response = "";
                        responseRecheckTbo.ISBooking = ISBooking;
                        responseRecheckTbo.PurchaseToken = pricecheck;

                        return responseRecheckTbo;
                    }
                    else
                    {

                        if (TBOCanPolResponse.Tables.Contains("HotelNorms") == true)
                        {
                            //bool type = false;
                            if (TBOCanPolResponse.Tables["HotelNorms"].Columns.Contains("string"))
                            {
                                Type = Convert.ToString(TBOCanPolResponse.Tables["HotelNorms"].Rows[0]["string"]);
                            }
                            else
                            {
                                for (int x = 0; x < TBOCanPolResponse.Tables["string"].Rows.Count; x++)
                                {
                                    Type = Type + "<br>" + Convert.ToString(TBOCanPolResponse.Tables["string"].Rows[x]["string_Text"]);
                                }
                            }

                        }

                        string[] Arr = roomid.ToString().Split(new String[] { "%%%" }, StringSplitOptions.None);
                        string DefaultPolicy = TBOCanPolResponse.Tables["CancelPolicies"].Columns.Contains("DefaultPolicy") ? "<br>" + Convert.ToString(TBOCanPolResponse.Tables["CancelPolicies"].Rows[0]["DefaultPolicy"]) : "";
                        string essitialninfo = Arr[12];
                        Type = essitialninfo + "<br>" + DefaultPolicy + Type;
                        // string pricecheck = "";
                        #region Pricecheck

                        double FinalPrice = Convert.ToDouble(CompanyBuyPrice);
                        string Cartroomprice = "";
                        double totalfinalprice = 0;
                        bool pricechange = false;
                        if (ResponseData1 != null && TBOCanPolResponse.Tables.Count > 0 && TBOCanPolResponse.Tables["PriceVerification"] != null && TBOCanPolResponse.Tables["PriceVerification"].Rows.Count > 0)
                        {
                            bool availableforbook = Convert.ToBoolean(TBOCanPolResponse.Tables["AvailabilityAndPricingResponse"].Rows[0]["AvailableForBook"]);
                            bool AvailableForConfirmBook = Convert.ToBoolean(TBOCanPolResponse.Tables["AvailabilityAndPricingResponse"].Rows[0]["AvailableForConfirmBook"]);
                            bool cancellationpolicies = Convert.ToBoolean(TBOCanPolResponse.Tables["AvailabilityAndPricingResponse"].Rows[0]["CancellationPoliciesAvailable"]);

                            //if (availableforbook == false || AvailableForConfirmBook == false || cancellationpolicies == false)
                            if (availableforbook == false)
                            {
                                FinalPrice = 0;
                                pricechange = true;
                                pricecheck = pricechange + "$" + FinalPrice;
                                try
                                {
                                    responsetype = "GeneralError";
                                    string abc = await _commonMethod.SupplierPerformanceReportForRecheckOnly(SupplierName, "PreBook", AgentId, time, responsetype, PriceCheckRequest, ResponseData, Error);
                                }
                                catch (Exception e)
                                {
                                    _ = _exception.ErrorNew(e, _LogAPI.Recheck, SearchKey);
                                }
                                responseRecheckTbo.Response = "";
                                responseRecheckTbo.ISBooking = ISBooking;
                                responseRecheckTbo.PurchaseToken = pricecheck;

                                return responseRecheckTbo;
                            }
                            else
                            {
                                if (Convert.ToString(TBOCanPolResponse.Tables["PriceVerification"].Rows[0]["PriceChanged"]) == "true")
                                {
                                    FinalPrice = 0;
                                    pricechange = true;
                                    pricecheck = pricechange + "$" + FinalPrice;
                                    try
                                    {
                                        responsetype = "GeneralError";
                                        string abc = await _commonMethod.SupplierPerformanceReportForRecheckOnly(SupplierName, "PreBook", AgentId, time, responsetype, PriceCheckRequest, ResponseData, Error);
                                    }
                                    catch (Exception e)
                                    {
                                        _ = _exception.ErrorNew(e, _LogAPI.Recheck, SearchKey);
                                    }


                                    responseRecheckTbo.Response = "";
                                    responseRecheckTbo.ISBooking = ISBooking;
                                    responseRecheckTbo.PurchaseToken = pricecheck;

                                    return responseRecheckTbo;

                                }
                                else
                                {
                                    try
                                    {
                                        string abc = await _commonMethod.SupplierPerformanceReportForRecheckOnly(SupplierName, "PreBook", AgentId, time, responsetype, PriceCheckRequest, ResponseData, Error);
                                    }
                                    catch (Exception e)
                                    {
                                        _ = _exception.ErrorNew(e, _LogAPI.Recheck, SearchKey);
                                    }
                                    pricecheck = pricechange + "$" + CompanyBuyPrice;
                                    ISBooking = "Available";


                                    responseRecheckTbo.Response = Type;
                                    responseRecheckTbo.ISBooking = ISBooking;
                                    responseRecheckTbo.PurchaseToken = pricecheck;

                                    return responseRecheckTbo;
                                }

                            }
                        }
                        else
                        {
                            try
                            {
                                responsetype = "GeneralError";
                                string abc = await _commonMethod.SupplierPerformanceReportForRecheckOnly(SupplierName, "PreBook", AgentId, time, responsetype, PriceCheckRequest, ResponseData, Error);
                            }
                            catch (Exception e)
                            {
                                _ = _exception.ErrorNew(e, _LogAPI.Recheck, SearchKey);
                            }
                            FinalPrice = 0;
                            pricechange = true;
                            pricecheck = pricechange + "$" + FinalPrice;
                        }
                        #endregion

                        #region New Logic
                        string[] Arr1 = roomtypeid.ToString().Split(new String[] { "%%%" }, StringSplitOptions.None);
                        DateTime PriviousCancellationPolicy = Convert.ToDateTime(Arr1[16]);
                        DateTime lastdate = Convert.ToDateTime(TBOCanPolResponse.Tables["CancelPolicies"].Rows[0]["LastCancellationDeadline"]);

                        if (PriviousCancellationPolicy.Date != lastdate.Date)
                        {
                            try
                            {
                                responsetype = "GeneralError";
                                string abc = await _commonMethod.SupplierPerformanceReportForRecheckOnly(SupplierName, "PreBook", AgentId, time, responsetype, PriceCheckRequest, ResponseData, Error);
                            }
                            catch (Exception e)
                            {
                                _ = _exception.ErrorNew(e, _LogAPI.Recheck, SearchKey);
                            }
                            FinalPrice = 0;
                            pricechange = true;
                            pricecheck = pricechange + "$" + FinalPrice;
                        }
                        #endregion



                        responseRecheckTbo.Response = Type;
                        responseRecheckTbo.ISBooking = ISBooking;
                        responseRecheckTbo.PurchaseToken = pricecheck;
                        return responseRecheckTbo;
                    }

                }
                responseRecheckTbo.Response = "";
                responseRecheckTbo.ISBooking = ISBooking;
                responseRecheckTbo.PurchaseToken = pricecheck;
                return responseRecheckTbo;

            }
            catch (Exception ex)
            {
                _ = _exception.ErrorNew(ex, _LogAPI.Recheck, SearchKey);
                pricecheck = 1 + "$" + 0;
                responseRecheckTbo.Response = "";
                responseRecheckTbo.ISBooking = ISBooking;
                responseRecheckTbo.PurchaseToken = pricecheck;

                return responseRecheckTbo;
            }
        }

        public string Get_TBO_PriceCheckRQ(string TBOUrl, DataRow[] TBODetails, string TBOUserName, string TBOPassword, string PropertyId, string CheckIn, string checkout, string RoomTypeId, string MealID, ref string RoomName, string Type, int RoomNo, string xmlhotelid, int NoOfRoom)
        {
            try
            {
                string HotelCode, RoomTypeCode, RatePlanCode, SessionId, ResultIndex, RoomTypeName, Nationality;

                string[] Arr = RoomTypeId.ToString().Split(new String[] { "%%%" }, StringSplitOptions.None);
                RoomTypeCode = Arr[0];
                RatePlanCode = Arr[1];
                SessionId = Arr[2];
                ResultIndex = Arr[3];
                string RoomIndex1 = Convert.ToString(Arr[4]);
                RoomTypeName = Arr[5];
                Nationality = Arr[6];
                HotelCode = Arr[14];
                string[] RoomIndex = Convert.ToString(RoomIndex1).Split(new String[] { "$$$" }, StringSplitOptions.None);

                StringBuilder objRequest = new StringBuilder();
                //objRequest.Append(XMLHotelBookingCommon.HotelTrvacoProvider);
                objRequest.Append("<soap:Envelope xmlns:soap=\"http://www.w3.org/2003/05/soap-envelope\" xmlns:hot=\"http://TekTravel/HotelBookingApi\">");
                objRequest.Append("<soap:Header xmlns:wsa=\"http://www.w3.org/2005/08/addressing\">");
                objRequest.Append("<hot:Credentials UserName=\"" + TBOUserName + "\" Password=\"" + TBOPassword + "\">");
                objRequest.Append("</hot:Credentials>");
               // objRequest.Append("<wsa:Action>" + TboInfo.TBOAvailabilityAndPricingAction + "</wsa:Action>");
                objRequest.Append("<wsa:Action>" + _supplierConfig.TBO.AvailabilityAndPricingAction + "</wsa:Action>");
                objRequest.Append("<wsa:To>" + TBOUrl + "</wsa:To>");
                objRequest.Append("</soap:Header>");
                objRequest.Append("<soap:Body>");
                objRequest.Append("<hot:AvailabilityAndPricingRequest>");
                objRequest.Append("<hot:ResultIndex>" + ResultIndex + "</hot:ResultIndex>");
                objRequest.Append("<hot:HotelCode>" + HotelCode + "</hot:HotelCode>");
                objRequest.Append("<hot:SessionId>" + SessionId + "</hot:SessionId>");
                objRequest.Append("<hot:OptionsForBooking>");
                objRequest.Append("<hot:FixedFormat>true</hot:FixedFormat>");
                objRequest.Append("<hot:RoomCombination>");
                // objRequest.Append("<hot:RoomIndex>" + RoomIndex + "</hot:RoomIndex>");
                for (int i = 1; i < RoomIndex.Length; i++)
                {
                    objRequest.Append("<hot:RoomIndex>" + RoomIndex[i] + "</hot:RoomIndex>");
                }
                //for (int i = 0; i < RoomDetails.NoofRooms.Count; i++)
                //{
                //    string[] strIndex = RoomDetails.NoofRooms[i].RoomTypeCode.ToString().Split(new String[] { "$$$" }, StringSplitOptions.None);
                //    RoomIndex = strIndex[1];
                //    if (string.IsNullOrEmpty(RoomIndex))
                //    {
                //        strIndex = RoomDetails.NoofRooms[i].RoomTypeCode.ToString().Split(new String[] { "%%%" }, StringSplitOptions.None);
                //        RoomIndex = strIndex[4];
                //    }
                //    objRequest.Append("<hot:RoomIndex>" + RoomIndex + "</hot:RoomIndex>");
                //}
                objRequest.Append("</hot:RoomCombination>");
                objRequest.Append("</hot:OptionsForBooking>");
                objRequest.Append("</hot:AvailabilityAndPricingRequest>");
                objRequest.Append("</soap:Body>");
                objRequest.Append("</soap:Envelope>");
                return objRequest.ToString();
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
            }
            return string.Empty;
        }

        public async Task<CancellationDto> Get_TBO_CancellationPolicy(string Type, DataSet dsHotel, string hotelcode, string ownsystemid, string RoomTypeId_Own, string MealID, string Board_Text, DataTable dtAgentDetail, DataTable dtSuplierMarkup, DataTable dtSearchConfig, HotelSearchParameter objSearPera, string roomno, string provider, int AgentId, DataTable dtMarkupDetails, string SearchKey, string GiataId = "0", int hotelRating = 0, double roomprice = 0, string Provider = "tbo_api", string SiteCurrency = "USD")
        {
            CancellationDto cancellationDto = new CancellationDto();
            string ISBooking = "NotAvailable";


            try
            {
                string responsetype = "Success";
                string time = "";
                string SupplierName = Provider;
                string Error = "";
                string PriceCheckRequest = "";
                string ResponseData = "";


                var hotelid = hotelcode;
                var roomid = RoomTypeId_Own;
                var available_id = ownsystemid;

                DataTable dt_TBO_Cancellation = _commonMethod.GenerateCancellationDataTableNewV2();
                double CurrencyFactorTBO = 1;
                string TBOCurrency = "USD";
                // Common.GetWHLCurrencyByCountry("WHL", objSearPera.CountryCode, ref TBOCurrency, "");
                if (TBOCurrency != Convert.ToString(SiteCurrency))
                {
                    DataTable dtInrCurrency = _commonMethod.GetCountryCurrency(TBOCurrency);
                    CurrencyFactorTBO = Convert.ToDouble(dtInrCurrency.Rows[0]["AmtToExch"]);
                }

                //DataTable dtInrCurrency = Common.GetCountryCurrency(XMLHotelBookingCommon.TBOCurrency);
                //double CurrencyFactorTBO = Convert.ToDouble(dtInrCurrency.Rows[0]["AmtToExch"]);

                #region Cancellation Policy RQ
                Boolean isPerce = true;
                double AgentCompanyPercentage = Convert.ToDouble(objSearPera.AgentCompanyPercentage);
                bool IsB2C = Convert.ToBoolean(Convert.ToByte(objSearPera.IsB2BOrB2C));
                double B2CMarkup = (IsB2C == false ? AgentCompanyPercentage : XMLHotelBookingCommon.B2CMarkup);
                double AgentPercentage = 0.0;
                double AgentMarkupAmount = 0.0;
                double SubAgentMarkup = 0;
                int IsParentId = 1;
                Boolean blnIsShowSupplier = false;
                //Int32 hotelRating = 0;
                //hotelRating = Convert.ToInt32(rating);
                _markup.getAgetMarkupSetting(Provider, GiataId, dtSuplierMarkup, dtMarkupDetails, ref AgentPercentage, ref AgentMarkupAmount, ref blnIsShowSupplier, ref isPerce, AgentId, hotelRating, objSearPera.CityID, Convert.ToString(objSearPera.CountryID), objSearPera.CheckInDate);
                if (dtAgentDetail != null)
                {
                    IsParentId = Convert.ToInt32(dtAgentDetail.Rows[0]["IsParentId"]);
                    SubAgentMarkup = Convert.ToDouble(dtAgentDetail.Rows[0]["B2CMarkupPer"]);
                }
                int CutOffDays = XMLHotelBookingCommon.CutOffDays;
                CutOffDays = _commonMethod.GetCutOffDays(dtSuplierMarkup, Provider);
                DataRow[] drSuplierMarkup = (dtSuplierMarkup.Select("SupplierName='" + Provider + "'"));
                //string UserId = Convert.ToString(drSuplierMarkup[0]["UserName"]);
                //string Password = Convert.ToString(drSuplierMarkup[0]["Password"]);
                string CheckIn = objSearPera.CheckInDate;
                CheckIn = CheckIn.Substring(6, 4) + "-" + CheckIn.Substring(3, 2) + "-" + CheckIn.Substring(0, 2);
                string TBOUrl = Convert.ToString(drSuplierMarkup[0]["WebSiteURL"]);
                string UserID = Convert.ToString(drSuplierMarkup[0]["UserID"]);
                string Username = Convert.ToString(drSuplierMarkup[0]["UserName"]);
                string password = Convert.ToString(drSuplierMarkup[0]["Password"]);
                string version = Convert.ToString(drSuplierMarkup[0]["version"]);
                int SID = Convert.ToInt32(drSuplierMarkup[0]["SupplierID"]);
                DataRow[] drSearchConfig = (dtSearchConfig.Select("SupplierID='" + SID + "'"));
                int SearchTimeOut = 20000;// Convert.ToInt32(drSearchConfig[0]["BookingConfirmationTimeout"]);
                                          //string PropertyId = ContractID;
                int NoOfNights = objSearPera.NoofNights;
                int NoOfRoom = objSearPera.Rooms.Count();
                string RoomName = "";
                #endregion
                //string CancelPolicyRQ = Get_TBO_Policy_PriceCheckRQ(TBOUrl, UserID, Username, password, hotelid, roomid, available_id, version, NoOfRoom);               

                //if (CancelPolicyRQ != "")
                //{
                //#region Cancellation Policy RS            
                //string ResponseData = XMLHotelBookingCommon.GetTrvcoResponse(CancelPolicyRQ, TBOUrl, SearchTimeOut, "TBO");

                //Common.ThirdPartyGetPolicyHistory(Convert.ToString(RoomTypeId_Own), Convert.ToString(ownsystemid), CancelPolicyRQ, Convert.ToString(ResponseData), "After Get Policy", "tbo", "Response", "-", "");


                ////ResponseData = System.IO.File.ReadAllText(@"E:\cancellationpolicyWHL.txt");

                //if (ResponseData == string.Empty || ResponseData.Contains("Error"))
                //    return dsHotel;

                //DataSet TBOCanPolResponse = Common.GetDataSetFromXMLString(ResponseData);
                //#endregion


                #region Cancellation policy in price check

                string xslRequest = Get_TBO_PriceCheckRQ(TBOUrl, drSuplierMarkup, Username, password, hotelid, CheckIn, "", roomid, "", ref RoomName, "", 0, hotelid, NoOfRoom);
                PriceCheckRequest = xslRequest;
                _commonMethod.ThirdPartyGetPolicyHistory(Convert.ToString(RoomTypeId_Own), Convert.ToString(ownsystemid), xslRequest, Convert.ToString("-"), "Before Get Policy & Price Check", "tbo", "Response", "-", "");

                if (xslRequest != "")
                {
                    System.Diagnostics.Stopwatch timerroomprocess = new System.Diagnostics.Stopwatch();
                    timerroomprocess.Start();

                    //string ResponseData1 = _commonMethod.GetTrvcoResponse_report(ref Error, xslRequest, TBOUrl, SearchTimeOut, "TBO");
                    ResponseDto responseDto = await _commonMethod.GetTrvcoResponse_report(xslRequest, TBOUrl, SearchTimeOut, "TBO");
                    string ResponseData1 = responseDto.Response;
                    ResponseData = responseDto.Response;
                    Error = responseDto.Error;

                    timerroomprocess.Stop();
                    time = timerroomprocess.Elapsed.ToString();

                    // System.IO.File.WriteAllText(@"C:\Users\karnave\Desktop\TBO\TBO Certification Case\TestCase\TBO_AvailabilityAndPricingRequest.txt", xslRequest);
                    //System.IO.File.WriteAllText(@"C:\Users\karnave\Desktop\TBO\TBO Certification Case\TestCase\TBO_AvailabilityAndPricingResponse.txt", ResponseData1);

                    // Common.ThirdPartyGetPolicyHistory(Convert.ToString(RoomTypeId_Own), Convert.ToString(ownsystemid), CancelPolicyRQ, Convert.ToString(ResponseData1), "After Get Policy", "tbo", "Response", "-", "");

                    //ResponseData = System.IO.File.ReadAllText(@"E:\cancellationpolicy_response_whl.txt");

                    _commonMethod.ThirdPartyGetPolicyHistory(Convert.ToString(RoomTypeId_Own), Convert.ToString(ownsystemid), xslRequest, Convert.ToString(ResponseData1), "After Get Policy & Price Check", "tbo", "Response", "-", "");

                    DataSet TBOCanPolResponse = _commonMethod.GetDataSetFromXMLString(ResponseData1);
                    #endregion
                    if (Convert.ToString(TBOCanPolResponse.Tables["Status"].Rows[0]["StatusCode"]) != "01")
                    {
                        try
                        {
                            responsetype = "GeneralError";
                            string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, PriceCheckRequest, ResponseData, Error);
                        }
                        catch (Exception e)
                        {
                            _ = _exception.ErrorNew(e, _LogAPI.Recheck, SearchKey);
                        }

                        cancellationDto.dsHotelData = dsHotel;
                        cancellationDto.ISBooking = ISBooking;
                        return cancellationDto;
                    }
                    else
                    {

                        if (TBOCanPolResponse.Tables.Contains("HotelNorms") == true)
                        {
                            //bool type = false;
                            if (TBOCanPolResponse.Tables["HotelNorms"].Columns.Contains("string"))
                            {
                                Type = Convert.ToString(TBOCanPolResponse.Tables["HotelNorms"].Rows[0]["string"]);
                            }
                            else
                            {
                                for (int x = 0; x < TBOCanPolResponse.Tables["string"].Rows.Count; x++)
                                {
                                    Type = Type + "<br>" + Convert.ToString(TBOCanPolResponse.Tables["string"].Rows[x]["string_Text"]);
                                }
                            }
                        }

                        string[] Arr = roomid.ToString().Split(new String[] { "%%%" }, StringSplitOptions.None);
                        string DefaultPolicy = TBOCanPolResponse.Tables["CancelPolicies"].Columns.Contains("DefaultPolicy") ? "<br>" + Convert.ToString(TBOCanPolResponse.Tables["CancelPolicies"].Rows[0]["DefaultPolicy"]) : "";
                        string essitialninfo = Arr[12];
                        Type = essitialninfo + "<br>" + DefaultPolicy + Type;
                        string pricecheck = "";
                        #region Pricecheck

                        double FinalPrice = roomprice;
                        string Cartroomprice = "";
                        double totalfinalprice = 0;
                        bool pricechange = false;
                        if (ResponseData1 != null && TBOCanPolResponse.Tables.Count > 0 && TBOCanPolResponse.Tables["PriceVerification"] != null && TBOCanPolResponse.Tables["PriceVerification"].Rows.Count > 0)
                        {
                            bool availableforbook = Convert.ToBoolean(TBOCanPolResponse.Tables["AvailabilityAndPricingResponse"].Rows[0]["AvailableForBook"]);
                            bool AvailableForConfirmBook = Convert.ToBoolean(TBOCanPolResponse.Tables["AvailabilityAndPricingResponse"].Rows[0]["AvailableForConfirmBook"]);
                            bool cancellationpolicies = Convert.ToBoolean(TBOCanPolResponse.Tables["AvailabilityAndPricingResponse"].Rows[0]["CancellationPoliciesAvailable"]);

                            //if (availableforbook == false || AvailableForConfirmBook == false || cancellationpolicies == false)
                            if (availableforbook == false)
                            {
                                FinalPrice = 0;
                                pricechange = true;
                                pricecheck = pricechange + "$" + FinalPrice;
                                try
                                {
                                    responsetype = "GeneralError";
                                    string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, PriceCheckRequest, ResponseData, Error);
                                }
                                catch (Exception e)
                                {
                                    _ = _exception.ErrorNew(e, _LogAPI.Recheck, SearchKey);

                                }
                            }
                            else
                            {
                                if (Convert.ToString(TBOCanPolResponse.Tables["PriceVerification"].Rows[0]["PriceChanged"]) == "true")
                                {
                                    FinalPrice = 0;
                                    pricechange = true;
                                    pricecheck = pricechange + "$" + FinalPrice;
                                    try
                                    {
                                        responsetype = "GeneralError";
                                        string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, PriceCheckRequest, ResponseData, Error);
                                    }
                                    catch (Exception e)
                                    {
                                        _ = _exception.ErrorNew(e, _LogAPI.Recheck, SearchKey);

                                    }
                                    //for (int K = 0; K < 1; K++)
                                    //{
                                    //    //Now filter room
                                    //    //dsTBOPrice.Tables["HotelRoom"].DefaultView.RowFilter = "RoomIndex=" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[K]["RoomNumber"]);
                                    //    if (TBOCanPolResponse.Tables["HotelRoom"].DefaultView.Count > 0)
                                    //    {
                                    //        string HotelRoom_Id = Convert.ToString(TBOCanPolResponse.Tables["HotelRoom"].Rows[0]["HotelRoom_Id"]);
                                    //        DataRow[] drPrice = TBOCanPolResponse.Tables["RoomRate"].Select("HotelRoom_Id='" + HotelRoom_Id + "'");
                                    //        #region "Price update logic with markup"
                                    //        roomprice = Math.Round(Convert.ToDouble(drPrice[0]["TotalFare"]));
                                    //        Cartroomprice = Cartroomprice + "&&&" + roomprice;
                                    //        totalfinalprice = Math.Round(Convert.ToDouble(drPrice[0]["TotalFare"]) * CurrencyFactorTBO, 2);
                                    //        FinalPrice = FinalPrice + totalfinalprice;
                                    //        pricecheck = pricechange + "$" + FinalPrice;
                                    //        #endregion
                                    //        //return true;
                                    //    }
                                    //}
                                }
                                else
                                {
                                    try
                                    {
                                        string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, PriceCheckRequest, ResponseData, Error);
                                    }
                                    catch (Exception e)
                                    {
                                        _ = _exception.ErrorNew(e, _LogAPI.Recheck, SearchKey);

                                    }
                                    pricecheck = pricechange + "$" + roomprice;

                                    ISBooking = "Available";
                                }
                            }
                        }
                        else
                        {
                            try
                            {
                                responsetype = "GeneralError";
                                string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, PriceCheckRequest, ResponseData, Error);
                            }
                            catch (Exception e)
                            {
                                _ = _exception.ErrorNew(e, _LogAPI.Recheck, SearchKey);

                            }
                            FinalPrice = 0;
                            pricechange = true;
                            pricecheck = pricechange + "$" + FinalPrice;
                        }
                        #endregion

                        #region New Logic
                        string[] Arr1 = RoomTypeId_Own.ToString().Split(new String[] { "%%%" }, StringSplitOptions.None);
                        DateTime PriviousCancellationPolicy = Convert.ToDateTime(Arr1[16]);
                        DateTime lastdate = Convert.ToDateTime(TBOCanPolResponse.Tables["CancelPolicies"].Rows[0]["LastCancellationDeadline"]);
                        if (PriviousCancellationPolicy.Date > DateTime.Now.Date)
                        {
                            if (PriviousCancellationPolicy.Date != lastdate.Date)
                            {
                                try
                                {
                                    responsetype = "GeneralError";
                                    string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, PriceCheckRequest, ResponseData, Error);
                                }
                                catch (Exception e)
                                {
                                    _ = _exception.ErrorNew(e, _LogAPI.Recheck, SearchKey);

                                }
                                FinalPrice = 0;
                                pricechange = true;
                                pricecheck = pricechange + "$" + FinalPrice;
                            }
                        }
                        string LastCancellationDeadline = Convert.ToString(lastdate.Date);

                        DateTime CancelDate = PriviousCancellationPolicy;
                        CancelDate = CancelDate.AddDays(CutOffDays).Date;
                        //int totalcount = drCencelatioPolicy.Length;
                        string uniqid = Guid.NewGuid().ToString();
                        double MainAgentCharge = roomprice;
                        double VatOnPolicy = 0;


                        if (DateTime.Now.Date < CancelDate.Date)
                        {
                            if (dt_TBO_Cancellation.Rows.Count == 0)
                            {
                                double zero = 0;
                                dt_TBO_Cancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref zero, dt_TBO_Cancellation.NewRow(), CurrencyFactorTBO, TBOCurrency, provider, ownsystemid, Convert.ToString(roomno),
                                       Convert.ToString(RoomTypeId_Own), RoomName, MealID, DateTime.Now.Date, CancelDate.AddDays(-1), 0, 0, "0", VatOnPolicy, uniqid, "NO", pricecheck, Type, "", ""));
                                dt_TBO_Cancellation.AcceptChanges();
                            }
                            dt_TBO_Cancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref MainAgentCharge, dt_TBO_Cancellation.NewRow(), CurrencyFactorTBO, TBOCurrency, provider, ownsystemid, Convert.ToString(roomno),
                                   Convert.ToString(RoomTypeId_Own), RoomName, MealID, Convert.ToDateTime(CancelDate), Convert.ToDateTime(CheckIn), roomprice, roomprice, "0", VatOnPolicy, uniqid, "NO", pricecheck, Type, "", ""));
                            dt_TBO_Cancellation.AcceptChanges();
                        }
                        else
                        {
                            dt_TBO_Cancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref MainAgentCharge, dt_TBO_Cancellation.NewRow(), CurrencyFactorTBO, TBOCurrency, provider, ownsystemid, Convert.ToString(roomno),
                            Convert.ToString(RoomTypeId_Own), RoomName, MealID, DateTime.Now.Date, Convert.ToDateTime(CheckIn), roomprice, roomprice, "0", VatOnPolicy, uniqid, "Yes", pricecheck, Type, "", ""));
                            dt_TBO_Cancellation.AcceptChanges();
                        }
                        #endregion

                        #region old logic
                        //int totalcount = drCencelatioPolicy.Length;
                        //DateTime Date = DateTime.Now.Date;
                        //for (int i = 0; i < totalcount; i++)
                        //{
                        //    DateTime CurentDay = DateTime.Now.Date;
                        //    DateTime fromdate = Convert.ToDateTime(drCencelatioPolicy[i]["FromDate"]);
                        //    DateTime todate = Convert.ToDateTime(drCencelatioPolicy[i]["ToDate"]);
                        //    //string FromDate = Convert.ToString(drCencelatioPolicy[i]["FromDate"]);
                        //    //string ToDate = Convert.ToString(drCencelatioPolicy[i]["ToDate"]);

                        //    string ChargeType = Convert.ToString(drCencelatioPolicy[i]["ChargeType"]).ToLower();

                        //    double Penalty = 0.0;
                        //    bool fixedcharge = false;
                        //    if (ChargeType == "percentage")
                        //    {
                        //        double Percentage = Convert.ToDouble(drCencelatioPolicy[i]["CancellationCharge"]);
                        //        Penalty = (roomprice * Percentage) / 100;
                        //    }
                        //    else if (ChargeType == "fixed")
                        //    {
                        //        double Amount = Convert.ToDouble(drCencelatioPolicy[i]["CancellationCharge"]);
                        //        Penalty = Amount;
                        //        if (Penalty > 0)
                        //        {
                        //            fixedcharge = true;
                        //        }
                        //    }
                        //    else if (ChargeType == "night")
                        //    {
                        //        double night = Convert.ToDouble(drCencelatioPolicy[i]["CancellationCharge"]);
                        //        Penalty = (roomprice / night);
                        //    }
                        //    else
                        //    {
                        //        Penalty = (roomprice);
                        //    }
                        //    //if (Date == fromdate)
                        //    //{
                        //    //    continue;
                        //    //}
                        //    Date = fromdate;

                        //    DateTime curDate = Common.GetTimeZoneDate(objSearPera.CurrentDate);
                        //    string uniqid = Guid.NewGuid().ToString();


                        //    #region Cancellation Policy Logic and Row
                        //    DateTime CancelDate = DateTime.Now.Date;
                        //    if (Penalty == 0)
                        //        CancelDate = todate;
                        //    else
                        //        CancelDate = fromdate;

                        //    #region Vat
                        //    //double Penaltywithmarkup = 0;
                        //    double MainAgentCharge = Penalty;
                        //    double VatOnPolicy = 0;
                        //    if (ChargeType == "fixed")
                        //    {
                        //        Penalty = Common.ProcessHotelCancelationPrice_WithMarkup(ref MainAgentCharge, objSearPera.NoofNights, objSearPera.CityID, objSearPera.CountryId, Penalty, isPerce, B2CMarkup, AgentPercentage, AgentMarkupAmount, IsParentId, SubAgentMarkup, ref VatOnPolicy);
                        //    }
                        //    #endregion


                        //    DateTime NowDate = DateTime.Now.Date;
                        //    if (CancelDate.AddDays(CutOffDays).Date > DateTime.Now.Date)
                        //    {
                        //        todate = todate.AddDays(CutOffDays).Date;
                        //        fromdate = fromdate.AddDays(CutOffDays).Date;
                        //        CancelDate = CancelDate.AddDays(CutOffDays).Date;
                        //    }

                        //    if (NowDate.Date < CancelDate.Date)
                        //    {
                        //        //if (fixedcharge)
                        //        //{
                        //        //    dt_TBO_Cancellation.Rows.Add(Common.setCancellationPolicyRow(dt_TBO_Cancellation.NewRow(), CurrencyFactorTBO, TBOCurrency, provider, ownsystemid, Convert.ToString(roomno),
                        //        //   Convert.ToString(RoomTypeId_Own), RoomName, MealID, DateTime.Now.Date, CancelDate.AddDays(-1), 0, 0, "0", VatOnPolicy, uniqid, "NO", "", Type, "", ""));
                        //        //    dt_TBO_Cancellation.AcceptChanges();
                        //        //}

                        //        if (Penalty == 0)
                        //        {
                        //            if (dt_TBO_Cancellation.Rows.Count == 0)
                        //            {
                        //                double zero = 0;
                        //                dt_TBO_Cancellation.Rows.Add(Common.setCancellationPolicyRow(ref zero, dt_TBO_Cancellation.NewRow(), CurrencyFactorTBO, TBOCurrency, provider, ownsystemid, Convert.ToString(roomno),
                        //                Convert.ToString(RoomTypeId_Own), RoomName, MealID, DateTime.Now.Date, CancelDate, 0, 0, "0", VatOnPolicy, uniqid, "NO", pricecheck, Type, "", ""));
                        //                dt_TBO_Cancellation.AcceptChanges();
                        //            }
                        //        }
                        //        else
                        //        {
                        //            dt_TBO_Cancellation.Rows.Add(Common.setCancellationPolicyRow(ref MainAgentCharge, dt_TBO_Cancellation.NewRow(), CurrencyFactorTBO, TBOCurrency, provider, ownsystemid, Convert.ToString(roomno),
                        //            Convert.ToString(RoomTypeId_Own), RoomName, MealID, Convert.ToDateTime(CancelDate), CancelDate, Penalty, Penalty, "0", VatOnPolicy, uniqid, "NO", pricecheck, Type, "", ""));
                        //            dt_TBO_Cancellation.AcceptChanges();
                        //        }
                        //    }
                        //    else
                        //    {
                        //        if (dt_TBO_Cancellation.Rows.Count == 0)
                        //        {
                        //            dt_TBO_Cancellation.Rows.Add(Common.setCancellationPolicyRow(ref MainAgentCharge, dt_TBO_Cancellation.NewRow(), CurrencyFactorTBO, TBOCurrency, provider, ownsystemid, Convert.ToString(roomno),
                        //            Convert.ToString(RoomTypeId_Own), RoomName, MealID, curDate, todate, Penalty, Penalty, "0", VatOnPolicy, uniqid, "Yes", pricecheck, Type, "", ""));
                        //            dt_TBO_Cancellation.AcceptChanges();
                        //        }
                        //    }
                        //}
                        //#endregion
                        #endregion
                    }
                }
                dsHotel.Tables[2].Merge(dt_TBO_Cancellation);
                //return dsHotel;

                cancellationDto.dsHotelData = dsHotel;
                cancellationDto.ISBooking = ISBooking;
                return cancellationDto;
            }
            catch (Exception ex)
            {
                _ = _exception.ErrorNew(ex, _LogAPI.Recheck, SearchKey);


                cancellationDto.dsHotelData = dsHotel;
                cancellationDto.ISBooking = ISBooking;
                return cancellationDto;
            }
        }

        public async Task<CancellationDto> Get_TBO_Json_CancellationPolicy(string Type, DataSet dsHotel, string hotelcode, string ownsystemid, string RoomTypeId_Own, string MealID, string Board_Text, DataTable dtAgentDetail, DataTable dtSuplierMarkup, DataTable dtSearchConfig, HotelSearchParameter objSearPera, string roomno, string provider, int AgentId, DataTable dtMarkupDetails, string SearchKey, TokenData TokenData , string GiataId = "0", int hotelRating = 0, double roomprice = 0, string Provider = "tbo_v1", string SiteCurrency = "USD")
        {
            CancellationDto cancellationDto = new CancellationDto();
            string IsBooking = "Available";
            try
            {
                string responsetype = "Success";
                string time = "";
                string SupplierName = Provider;
                string Error = "";
                string PriceCheckRequest = "";
                string ResponseData = "";
                string RoomName = "";
                DateTime CancellationDeadline = DateTime.Now.Date;


                var hotelid = hotelcode;
                var roomid = RoomTypeId_Own;
                var available_id = ownsystemid;

                DataTable dt_TBO_Cancellation = _commonMethod.GenerateCancellationDataTableNewV2();
                double CurrencyFactorTBO = 1;
                
                DataRow[] drSuplierMarkup = (dtSuplierMarkup.Select("SupplierName='" + Provider + "'"));
                //string TBOUrl = Convert.ToString(drSuplierMarkup[0]["WebSiteURL"]) + TboInfo.Tbo_V1Preebook;
                string TBOUrl = Convert.ToString(drSuplierMarkup[0]["WebSiteURL"]) + _supplierConfig.TBO.V1PreBook;
                string UserID = Convert.ToString(drSuplierMarkup[0]["UserID"]);
                string Username = Convert.ToString(drSuplierMarkup[0]["UserName"]);
                string password = Convert.ToString(drSuplierMarkup[0]["Password"]);
                string version = Convert.ToString(drSuplierMarkup[0]["version"]);
                string Currency = Convert.ToString(drSuplierMarkup[0]["CurrencyShortName"]);
                // Common.GetWHLCurrencyByCountry("WHL", objSearPera.CountryCode, ref TBOCurrency, "");
                if (Currency.ToLower() != Convert.ToString(SiteCurrency).ToLower())
                {
                    //DataTable dtInrCurrency = _commonMethod.GetCountryCurrency(Currency);
                    //CurrencyFactorTBO = Convert.ToDouble(dtInrCurrency.Rows[0]["AmtToExch"]);
                    CurrencyFactorTBO = _commonMethod.GetROEByCurrency(Currency, TokenData.dtROE);
                }

                int SID = Convert.ToInt32(drSuplierMarkup[0]["SupplierID"]);
                DataRow[] drSearchConfig = (dtSearchConfig.Select("SupplierID='" + SID + "'"));
                string CheckIn = objSearPera.CheckInDate;
                CheckIn = CheckIn.Substring(6, 4) + "-" + CheckIn.Substring(3, 2) + "-" + CheckIn.Substring(0, 2);
                int SearchTimeOut = 20000;// Convert.ToInt32(drSearchConfig[0]["BookingConfirmationTimeout"]);
                                          //string PropertyId = ContractID;
                int NoOfNights = objSearPera.NoofNights;
                int NoOfRoom = objSearPera.Rooms.Count();
                double ItemPrice = roomprice;
                string EssentialInformation = "";
                string xslRequest = Get_TBO_PriceCheckRQJson(TBOUrl, drSuplierMarkup, Username, password, hotelid, CheckIn, "", roomid, "", "", 0, hotelid, NoOfRoom);
                PriceCheckRequest = xslRequest;
                _commonMethod.ThirdPartyGetPolicyHistory(Convert.ToString(RoomTypeId_Own), Convert.ToString(ownsystemid), xslRequest, Convert.ToString("-"), "Before Get Policy & Price Check", Provider, "Response", "-", "");

                if (xslRequest != "")
                {
                    System.Diagnostics.Stopwatch timerroomprocess = new System.Diagnostics.Stopwatch();
                    timerroomprocess.Start();

                    ResponseDto responseDto = await GetTrvcoResponseTBOJson(UserID, password, xslRequest, TBOUrl, 120000, Provider);
                    string ResponseData1 = responseDto.Response;
                    Error = responseDto.Error;

                    string uniqid = Guid.NewGuid().ToString();

                    timerroomprocess.Stop();
                    time = timerroomprocess.Elapsed.ToString();
                    _commonMethod.ThirdPartyGetPolicyHistory(Convert.ToString(RoomTypeId_Own), Convert.ToString(ownsystemid), xslRequest, Convert.ToString(ResponseData1), "After Get Policy & Price Check", Provider, "Response", "-", "");

                    Preebookres.Rootobject DS = null;
                    try
                    {
                        DS = Newtonsoft.Json.JsonConvert.DeserializeObject<Preebookres.Rootobject>(ResponseData1);
                        string incl = Convert.ToString(DS.HotelResult[0].Rooms[0].Inclusion);
                        Type = "<b> incl :</b>" + incl + "<br/>";
                        if (DS.HotelResult[0].RateConditions != null)
                        {
                            for (int y = 0; y < DS.HotelResult[0].RateConditions.Length; y++)
                            {
                                Type = Type + "<br/>" + Convert.ToString(DS.HotelResult[0].RateConditions[y]);

                            }
                        }
                        if (DS.HotelResult[0].Rooms[0].Amenities != null)
                        {
                            for (int x = 0; x < DS.HotelResult[0].Rooms[0].Amenities.Length; x++)
                            {
                                Type = Type + "<br/><b>Amenities:</b>" + Convert.ToString(DS.HotelResult[0].Rooms[0].Amenities[x]);
                            }
                        }
                        try
                        {
                            if (DS.HotelResult[0].Rooms[0].Supplements != null)
                            {
                                if (DS.HotelResult[0].Rooms[0].Supplements.Length > 0)
                                {
                                    for (int s = 0; s < DS.HotelResult[0].Rooms[0].Supplements.Length; s++)
                                    {
                                        EssentialInformation += "<br><b>Index: </b>" + Convert.ToString(DS.HotelResult[0].Rooms[0].Supplements[s][0].Index);
                                        EssentialInformation += "<br><b>Type: </b>" + Convert.ToString(DS.HotelResult[0].Rooms[0].Supplements[s][0].Type);
                                        EssentialInformation += "<br><b>Description: </b>" + Convert.ToString(DS.HotelResult[0].Rooms[0].Supplements[s][0].Description);
                                        EssentialInformation += "<br><b>Price: </b>" + Convert.ToString(DS.HotelResult[0].Rooms[0].Supplements[s][0].Price);
                                        EssentialInformation += "<br><b>Currency: </b>" + Convert.ToString(DS.HotelResult[0].Rooms[0].Supplements[s][0].Currency);
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            //ExceptionManager.LogException(ex, true);
                        }
                        if (EssentialInformation != "")
                        {
                            Type = Type + "<br/><b>Supplements:(Supplements to be paid by customer directly at the hotel)</b>" + EssentialInformation;
                        }
                    }
                    catch (Exception ex)
                    {
                        _ = _exception.ErrorNew(ex, _LogAPI.Recheck, SearchKey);

                    }
                    #region cancellation policy New Logic


                    if (Convert.ToInt32(DS.Status.Code) == 200)
                    {
                        //DataSet dsTBOPrice1 = dsTBOPrice;
                        if (DS.HotelResult[0].Rooms != null)
                        {
                            bool isrefundable = DS.HotelResult[0].Rooms[0].IsRefundable;
                            double TboPrice = DS.HotelResult[0].Rooms[0].TotalFare;
                            if (!isrefundable)
                            {

                                dt_TBO_Cancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref ItemPrice, dt_TBO_Cancellation.NewRow(), CurrencyFactorTBO, Currency, Provider, Convert.ToString(ownsystemid), Convert.ToString("1"),
                                Convert.ToString(RoomTypeId_Own), RoomName, Board_Text, DateTime.Now.Date, Convert.ToDateTime(CheckIn), ItemPrice, ItemPrice, "0", 0, uniqid, "Yes", Convert.ToString(TboPrice), Type, "", ""));
                                dt_TBO_Cancellation.AcceptChanges();

                            }
                            else
                            {
                                if (DS.HotelResult[0].Rooms[0].CancelPolicies != null)
                                {
                                    // var sortedPolicies = DS.HotelResult[k].Rooms[R].CancelPolicies.OrderBy(p => p.FromDate).ToList();
                                    var sortedPolicies = DS.HotelResult[0].Rooms[0].CancelPolicies.Where(p => p.CancellationCharge != 0).OrderBy(p => p.FromDate).ToList(); //uncmnt #25-June-25
                                    // var sortedPolicies = DS.HotelResult[0].Rooms[0].CancelPolicies.Where(p => p.CancellationCharge != 0).OrderBy(p => p.FromDate).ThenBy(p => p.CancellationCharge).ToList(); //cmnt #25-June-25

                                    DateTime lastdate = DateTime.Now.Date;
                                    double CancellationPrice = ItemPrice;///Convert.ToDouble(sortedPolicies[v].CancellationCharge);

                                    for (int v = 0; v < sortedPolicies.Count(); v++)
                                    {
                                        //string ChargeType = Convert.ToString(sortedPolicies[v].ChargeType);
                                        //DateTime dateString1 = DateTime.ParseExact(sortedPolicies[v].FromDate, "dd/MM/yyyy", CultureInfo.InvariantCulture).Date; //Convert.ToDateTime(sortedPolicies[v].FromDate);
                                        string dateString = Convert.ToString(sortedPolicies[v].FromDate);
                                        dateString = dateString.Substring(0, 2) + "/" + dateString.Substring(3, 2) + "/" + dateString.Substring(6, 4);
                                        DateTime dateString11 = DateTime.ParseExact(dateString, "dd/MM/yyyy", CultureInfo.InvariantCulture).Date; //Convert.ToDateTime(sortedPolicies[v].FromDate);

                                        if (v == 0)
                                        {
                                            lastdate = dateString11;
                                        }
                                        else
                                        {
                                            if (lastdate > dateString11)
                                            {
                                                lastdate = dateString11;
                                            }
                                        }
                                    }
                                    DateTime NowDate = DateTime.Now.Date;
                                    //switch (ChargeType.ToLower())
                                    //{


                                    //    case "percentage":
                                    //        double Percentage1 = Convert.ToDouble(sortedPolicies[v].CancellationCharge);
                                    //        CancellationPrice = Convert.ToDouble(CancellationPrice * Percentage1 / 100);
                                    //        break;
                                    //    case "fixed":
                                    //        //double night = Convert.ToDouble(sortedPolicies[v].CancellationCharge);
                                    //        CancellationPrice = Convert.ToDouble(sortedPolicies[v].CancellationCharge);

                                    //        break;

                                    //    default:
                                    //        break;
                                    //}

                                    string LastCancellationDeadline = lastdate.ToString("dd/MM/yyyy");//Convert.ToString(lastdate.Date);

                                    #region Vat
                                    double VatOnPolicy = 0;
                                    double CompanyChargeAmount = CancellationPrice;
                                    double MainAgentCharge = CancellationPrice;
                                    Boolean isPerce = true;
                                    double AgentCompanyPercentage = Convert.ToDouble(objSearPera.AgentCompanyPercentage);
                                    bool IsB2C = Convert.ToBoolean(Convert.ToByte(objSearPera.IsB2BOrB2C));
                                    double B2CMarkup = (IsB2C == false ? AgentCompanyPercentage : XMLHotelBookingCommon.B2CMarkup);
                                    double AgentPercentage = 0.0;
                                    double AgentMarkupAmount = 0.0;
                                    double SubAgentMarkup = 0;
                                    int IsParentId = 1;
                                    CancellationPrice = _markup.ProcessHotelCancelationPrice_WithMarkup(ref MainAgentCharge, objSearPera.NoofNights, objSearPera.CityID,Convert.ToString(objSearPera.CountryID), CancellationPrice, isPerce, B2CMarkup, AgentPercentage, AgentMarkupAmount, IsParentId, SubAgentMarkup, ref VatOnPolicy);

                                    #endregion
                                    int CutOffDays = XMLHotelBookingCommon.CutOffDays;
                                    CutOffDays = _commonMethod.GetCutOffDays(dtSuplierMarkup, Provider);

                                    CancellationDeadline = DateTime.ParseExact(LastCancellationDeadline, "dd/MM/yyyy", null);
                                    //if (CancellationDeadline < DateTime.Now.Date && Convert.ToDouble(sortedPolicies[v].CancellationCharge) == 0)
                                    //{
                                    //    CancellationDeadline = DateTime.Now.Date;
                                    //}
                                    DateTime CancelDate = CancellationDeadline;
                                    if (CancelDate.AddDays(CutOffDays).Date > DateTime.Now.Date)
                                        CancelDate = CancelDate.AddDays(CutOffDays).Date;
                                    else
                                    {
                                        CancelDate = DateTime.Now.Date;
                                        MainAgentCharge = roomprice;
                                        CancellationPrice = ItemPrice;
                                        CompanyChargeAmount = roomprice;
                                    }



                                    if (NowDate.Date < CancelDate.Date)
                                    {
                                        //if (CancellationPrice == 0)
                                        {
                                            double zero = 0;
                                            dt_TBO_Cancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref zero, dt_TBO_Cancellation.NewRow(), CurrencyFactorTBO, Currency, Provider, Convert.ToString(ownsystemid), Convert.ToString("1"),
                                                   Convert.ToString(RoomTypeId_Own), RoomName, Board_Text, NowDate, CancelDate.AddDays(-1), 0, 0, "0", VatOnPolicy, uniqid, "NO", Convert.ToString(TboPrice), Type, "", ""));
                                            dt_TBO_Cancellation.AcceptChanges();
                                        }

                                        dt_TBO_Cancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref MainAgentCharge, dt_TBO_Cancellation.NewRow(), CurrencyFactorTBO, Currency, Provider, Convert.ToString(ownsystemid), Convert.ToString("1"),
                                               Convert.ToString(RoomTypeId_Own), RoomName, Board_Text, Convert.ToDateTime(CancelDate).Date, Convert.ToDateTime(CheckIn), CompanyChargeAmount, MainAgentCharge, "0", VatOnPolicy, uniqid, "NO", Convert.ToString(TboPrice), Type, "", ""));
                                        dt_TBO_Cancellation.AcceptChanges();
                                        //break;

                                    }
                                    else
                                    {
                                        dt_TBO_Cancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref MainAgentCharge, dt_TBO_Cancellation.NewRow(), CurrencyFactorTBO, Currency, Provider, Convert.ToString(ownsystemid), Convert.ToString("1"),
                                        Convert.ToString(RoomTypeId_Own), RoomName, Board_Text, DateTime.Now.Date, Convert.ToDateTime(CheckIn), CompanyChargeAmount, MainAgentCharge, "0", VatOnPolicy, uniqid, "Yes", Convert.ToString(TboPrice), Type, "", ""));
                                        dt_TBO_Cancellation.AcceptChanges();
                                        //break;
                                    }
                                    //}
                                }
                                else
                                {
                                    dt_TBO_Cancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref ItemPrice, dt_TBO_Cancellation.NewRow(), CurrencyFactorTBO, Currency, Provider, Convert.ToString(ownsystemid), Convert.ToString("1"),
                                        Convert.ToString(RoomTypeId_Own), RoomName, Board_Text, DateTime.Now.Date, Convert.ToDateTime(CheckIn), roomprice, roomprice, "0", 0, uniqid, "Yes", Convert.ToString(TboPrice), Type, "", ""));
                                    dt_TBO_Cancellation.AcceptChanges();
                                }
                            }
                        }
                    }
                }
                #endregion
                dsHotel.Tables[2].Merge(dt_TBO_Cancellation);

                cancellationDto.dsHotelData = dsHotel;
                cancellationDto.ISBooking = IsBooking;

                return cancellationDto;
            }
            catch (Exception ex)
            {
                _ = _exception.ErrorNew(ex, _LogAPI.Recheck, SearchKey);

                cancellationDto.dsHotelData = dsHotel;
                cancellationDto.ISBooking = "NotAvailable";

                return cancellationDto;
            }
        }

        public string Get_TBO_PriceCheckRQJson(string TBOUrl, DataRow[] TBODetails, string TBOUserName, string TBOPassword, string PropertyId, string CheckIn, string checkout, string RoomTypeId, string MealID, string Type, int RoomNo, string xmlhotelid, int NoOfRoom)
        {
            try
            {
                string Request = "", RoomTypeCode;

                string[] Arr = RoomTypeId.ToString().Split(new String[] { "$$$" }, StringSplitOptions.None);
                RoomTypeCode = Arr[0];

                string PaymentMode = "Limit";
                JsonSerializerSettings Setting = new JsonSerializerSettings();
                Preebook.Root RootObject = new Preebook.Root();
                RootObject.BookingCode = RoomTypeCode;
                RootObject.PaymentMode = PaymentMode;

                Setting.NullValueHandling = NullValueHandling.Ignore;
                Setting.DefaultValueHandling = DefaultValueHandling.Ignore;
                Request = JsonConvert.SerializeObject(RootObject, Setting);
                return Request;

            }
            catch (Exception ex)
            {
                _ = _exception.Error(ex);
            }
            return string.Empty;
        }

        public async Task<ResponseDto> GetTrvcoResponseTBOJson(string UsernameTbo, string PasswordTbo, string Request, string TravcoURL, Int32 TimeOut, string Provider = "", HotelSearchParameter para = null, bool DebugMode = false)
        {
            System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
            timer.Start();
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls
                   | SecurityProtocolType.Tls11
                   | SecurityProtocolType.Tls12;

            ResponseDto responseDto = new ResponseDto();

            string response = "";
            string Error = "";
            HttpWebRequest httpWebRequest = null;
            HttpWebResponse httpWebResponse = null;
            //Creates an HttpWebRequest for the specified URL.
            //httpWebRequest = (HttpWebRequest)WebRequest.Create("http://www.v3.staging.illusionswebservices.iwtx.com/illusionsHotelSearch.ashx");
            httpWebRequest = (HttpWebRequest)WebRequest.Create(TravcoURL);

            try
            {
                byte[] bytes;
                bytes = System.Text.Encoding.UTF8.GetBytes(Request);
                //Set HttpWebRequest properties
                httpWebRequest.Method = "POST";
                httpWebRequest.Timeout = TimeOut;
                httpWebRequest.ContentLength = bytes.Length;
                //ServicePointManager.DefaultConnectionLimit = 1000;

                string Key = Convert.ToBase64String(Encoding.ASCII.GetBytes(UsernameTbo + ":" + PasswordTbo));
                httpWebRequest.Method = "POST";
                httpWebRequest.ContentType = "application/json";
                httpWebRequest.Headers.Add("Accept-Encoding", "gzip, deflate");
                httpWebRequest.Headers.Add("Authorization", "Basic " + Key);

                using (Stream requestStream = httpWebRequest.GetRequestStream())
                {
                    //Writes a sequence of bytes to the current stream                     
                    requestStream.Write(bytes, 0, bytes.Length);
                    requestStream.Close();//Close stream
                }
                //Sends the HttpWebRequest, and waits for a response.
                httpWebResponse = (HttpWebResponse)await httpWebRequest.GetResponseAsync();
                if (httpWebResponse.StatusCode == HttpStatusCode.OK)
                {
                    //Get response stream into StreamReader 
                    using (Stream responseStream = httpWebResponse.GetResponseStream())
                    {
                        using (StreamReader reader = new StreamReader(responseStream))
                            response = await reader.ReadToEndAsync();

                    }
                    timer.Stop();
                    if (para != null)
                    {
                        para.SupplierTime += ",SupplierName :" + Provider + " : " + "Execution Time :" + timer.Elapsed.ToString();
                        if (DebugMode)
                            _ = _commonMethod.XMLRequestResponseTrack(Request, response, Provider, "Proper");
                    }
                    if (_keySettings.IsLogTrack == "1")
                    {
                        TimeSpan timeTaken = timer.Elapsed;
                        _exception.ErrorLog(Convert.ToString(_keySettings.LogTrackPath), "Trvaco->" + "TBO" + "-> Execution Time : " + timer.Elapsed.ToString());
                        _ = _commonMethod.XMLRequestResponseTrack(Request, response, Provider == "" ? "TBO_V1" : Provider, "Proper");
                    }
                }
            }
            catch (WebException we)
            {   //TODO: Add custom exception handling
                Error = we.Message + we.InnerException;

                HttpWebResponse httpResponse = (HttpWebResponse)we.Response;
                string result = string.Empty;
                string result1 = string.Empty;
                if (httpResponse != null && httpResponse.StatusDescription != null)
                {
                    result = httpResponse.StatusDescription.ToString();
                    result1 = new StreamReader(we.Response.GetResponseStream()).ReadToEnd();
                }

                _ = _commonMethod.XMLRequestResponseTrack(Request, result1, Provider == "" ? "TBO_V1" : Provider, "Proper");


            }
            catch (Exception ex)
            {
                Error = ex.Message + ex.InnerException;
                _ = _commonMethod.XMLRequestResponseTrack(Request, ex.Message, Provider == "" ? "TBO_V1" : Provider, "Proper");
                //ExceptionManager.LogException(ex);
            }

            finally
            {
                if (httpWebResponse != null)
                {
                    httpWebResponse.Close();

                    httpWebResponse = null;

                    httpWebRequest = null;
                }
            }

            responseDto.Response = response;
            responseDto.Error = Error;

            return responseDto;

        }

        public async Task<PriceCheckTboDto> TBO_PriceCheckRQRSJson(DataSet dsAgentDetails, DataSet dsCartDetails, DataRow[] TBODetails, string UserId, string Password, int i, int j, DataTable dtCheckPriceReqStructure, string UniqueNo, string AgentUniqueNo, double FinalPrice, bool isForBooking, double CompanyBuyingPrice, string PreBookingToken, string xmlhotelid, bool IsPolicyChangeTBO, TokenData tokenData, int AgentId)
        {
            PriceCheckTboDto priceCheckTboDto = new PriceCheckTboDto();
            string responsetype = "Success";
            string time = "";
            string Error = "";
            string xslRequest = "";
            string ResponseData = "";
            string SupplierName = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["SupplierName"]).ToLower();

            try
            {
                #region Price Check RQ and Declaration

                double CurrencyFactorTBO = 1;
                string countrycode = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomTypeId"]);
                string TBOCurrency = Convert.ToString(TBODetails[0]["CurrencyShortName"]);
                //Common.GetWHLCurrencyByCountry("WHL", countrycode.Split('$')[2], ref WHLCurrency, "");
                if (TBOCurrency.ToLower() != Convert.ToString(tokenData.SiteCurrency.ToLower()))
                {
                    CurrencyFactorTBO = _commonMethod.GetROEByCurrency(TBOCurrency, tokenData.dtROE);
                }
                //DataTable dtInrCurrency = Common.GetCountryCurrency(XMLHotelBookingCommon.WHLCurrency);
                //CurrencyFactorWHL = Convert.ToDouble(dtInrCurrency.Rows[0]["AmtToExch"]);

                DataSet dsTemp = new DataSet();
                //string TBOUrl = Convert.ToString(TBODetails[0]["WebSiteURL"]) + Convert.ToString(TboInfo.Tbo_V1Preebook);
                string TBOUrl = Convert.ToString(TBODetails[0]["WebSiteURL"]) + Convert.ToString(_supplierConfig.TBO.V1PreBook);
                string ownsystemhotelId = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["ServiceId"]);// Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["ownsystemhotelId"]);
                UserId = Convert.ToString(TBODetails[0]["UserName"]);
                Password = Convert.ToString(TBODetails[0]["Password"]);
                string SearchTimeOut = Convert.ToString(TBODetails[0]["Password"]);
                string HotelId = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["ContractID"]);
                string UserCartId = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["UserCartID"]);
                int UserCartDetailId = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[j]["UserCartDetailId"]);
                string RoomTypeId = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomTypeID"]);
                string CheckIn = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["CheckInDate"]);//"20180327"
                CheckIn = CheckIn.Substring(0, 4) + "-" + CheckIn.Substring(4, 2) + "-" + CheckIn.Substring(6, 2);
                string CheckOut = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["CheckOutDate"]);
                CheckOut = CheckOut.Substring(0, 4) + "-" + CheckOut.Substring(4, 2) + "-" + CheckOut.Substring(6, 2);
                int NoOfNoghts = Convert.ToInt32(dsCartDetails.Tables[0].Rows[i]["NoOfNight"]);
                string MealId = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["MealPlanCode"]);
                string RoomName = "";
                string TypeSPUI = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["spui"]);
                int RoomNo = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[j]["RoomNumber"]);
                int NoOfRoom = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView.Count);

                xslRequest = Get_TBO_PriceCheckRQJson(TBOUrl, TBODetails, UserId, Password, HotelId, CheckIn, CheckOut, RoomTypeId, MealId, TypeSPUI, RoomNo, xmlhotelid, NoOfRoom);

                _commonMethod.ThirdPartyBookingHistory(Convert.ToString(UniqueNo), Convert.ToString(AgentUniqueNo), xslRequest, "", "Before get price " + SupplierName + " request hit " + j + "is for Price Check" + isForBooking, SupplierName, "response confirm", "-", "WithinEarth");
                #endregion

                if (xslRequest != "")
                {
                    if (!isForBooking)
                    {
                        dtCheckPriceReqStructure.Rows.Add(_commonMethod.AddCheckPriceReq(dtCheckPriceReqStructure.NewRow(), j + 1, Convert.ToInt32(UserCartId), UserCartDetailId, RoomTypeId, SupplierName, xslRequest));
                    }
                    #region Price Check RS

                    // response = await XMLHotelBookingCommon.GetSupplierResponsenew1("", "", xslRequest, WHLUrl, 120000, "WHL", "", "", "", "","").ConfigureAwait(false);



                    System.Diagnostics.Stopwatch timerroomprocess = new System.Diagnostics.Stopwatch();
                    timerroomprocess.Start();

                    ResponseDto responseDto = await GetTrvcoResponseTBOJson(UserId, Password, xslRequest, TBOUrl, 120000, SupplierName);
                    ResponseData = responseDto.Response;
                    Error = responseDto.Error;

                    timerroomprocess.Stop();
                    time = timerroomprocess.Elapsed.ToString();


                    //ResponseData = File.ReadAllText(@"E:\pricechangetbo.txt");



                    _commonMethod.ThirdPartyBookingHistory(Convert.ToString(UniqueNo), Convert.ToString(AgentUniqueNo), xslRequest, ResponseData + Error, "After get price " + SupplierName + " request hit " + j + "is for Price Check" + isForBooking, SupplierName, "response confirm", "-", "WithinEarth");

                    if (ResponseData == string.Empty || ResponseData.Contains("Error") || ResponseData.Contains("Given rate is not Available for booking anymore"))
                    {
                        try
                        {
                            responsetype = "GeneralError";
                            string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, xslRequest, ResponseData, Error);
                        }
                        catch (Exception e)
                        {
                            _ = _exception.Error(e);
                        }
                        priceCheckTboDto.Flag = false;
                        return priceCheckTboDto;
                    }

                    Preebookres.Rootobject DS = null;
                    try
                    {

                        DS = Newtonsoft.Json.JsonConvert.DeserializeObject<Preebookres.Rootobject>(ResponseData);

                    }
                    catch (Exception ex)
                    {
                        _ = _exception.Error(ex);
                    }
                    #endregion
                    double totalfinalprice = 0;
                    double finalprices = Convert.ToDouble(dsCartDetails.Tables[1].DefaultView[j]["FinalSellingPrice"]);
                    string cancellationpolicydate = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["purchaseToken"]);
                    double roomprice = finalprices;
                    string Cartroomprice = "";
                    string Type = "", Board_Text = "", EssentialInformation = "";
                    string uniqid = Guid.NewGuid().ToString();
                    bool type = false;
                    if (Convert.ToInt32(DS.Status.Code) == 200)
                    {
                        //DataSet dsTBOPrice1 = dsTBOPrice;
                        if (DS.HotelResult[0].Rooms != null)
                        {
                            FinalPrice = Convert.ToDouble(DS.HotelResult[0].Rooms[0].TotalFare) * CurrencyFactorTBO;
                            priceCheckTboDto.ItemPrice = FinalPrice;//15/09/25

                            Board_Text = Convert.ToString(DS.HotelResult[0].Rooms[0].MealType);
                            if (DS.HotelResult[0].RateConditions != null)
                            {

                                for (int y = 0; y < DS.HotelResult[0].RateConditions.Length; y++)
                                {
                                    Type = Type + "<br/>" + Convert.ToString(DS.HotelResult[0].RateConditions[y]);

                                }
                            }
                            if (DS.HotelResult[0].Rooms[0].Amenities != null)
                            {
                                for (int x = 0; x < DS.HotelResult[0].Rooms[0].Amenities.Length; x++)
                                {
                                    Type = Type + "<br/>" + Convert.ToString(DS.HotelResult[0].Rooms[0].Amenities[x]);
                                }
                            }
                            try
                            {
                                if (DS.HotelResult[0].Rooms[0].Supplements != null)
                                {
                                    if (DS.HotelResult[0].Rooms[0].Supplements.Length > 0)
                                    {

                                        for (int s = 0; s < DS.HotelResult[0].Rooms[0].Supplements.Length; s++)
                                        {


                                            EssentialInformation += "<br><b>Index: </b>" + Convert.ToString(DS.HotelResult[0].Rooms[0].Supplements[s][0].Index);


                                            EssentialInformation += "<br><b>Type: </b>" + Convert.ToString(DS.HotelResult[0].Rooms[0].Supplements[s][0].Type);

                                            EssentialInformation += "<br><b>Description: </b>" + Convert.ToString(DS.HotelResult[0].Rooms[0].Supplements[s][0].Description);

                                            EssentialInformation += "<br><b>Price: </b>" + Convert.ToString(DS.HotelResult[0].Rooms[0].Supplements[s][0].Price);
                                            EssentialInformation += "<br><b>Currency: </b>" + Convert.ToString(DS.HotelResult[0].Rooms[0].Supplements[s][0].Currency);

                                        }


                                    }
                                }

                            }
                            catch (Exception ex)
                            {
                                _ = _exception.ErrorNew(ex,_LogAPI.Prebook);

                            }
                            if (EssentialInformation != "")
                            {
                                Type = Type + "<br/><b>Supplements:</b>" + EssentialInformation;
                            }
                            //type = HotelXML_Suppliers.GetTypeUpdateStaus(Convert.ToInt32(UserCartId), Type);



                            #region Cancrellation policy


                            #region DataRaw
                            string RoomTypeName = string.Empty;
                            DataRow[] drCencelatioPolicy = null;

                            #endregion

                            string LastCancellationDeadline = "";
                            //DateTime lastdate = Convert.ToDateTime(dsTBOPrice.Tables["CancelPolicies"].Rows[0]["LastCancellationDeadline"]);
                            //string LastCancellationDeadline = Convert.ToString(lastdate.Date);
                            DateTime CancelDate = DateTime.Now.Date;
                            // DataTable dtMergeCancelationPolicy = (DataTable)HttpContext.Current.Session["AddCartHotelCancelationPolicy"];
                            //DataRow[] drpolicy = dtMergeCancelationPolicy.Select("CartID='" + UserCartId + "' and SupplierName='TBO_V1' and  OwnsystemHotelId='" + ownsystemhotelId + "' and RoomTypeId='" + RoomTypeId + "' and MealId ='" + MealId + "' and  RoomNo='" + (j + 1).ToString() + "'");
                            // DataRow[] drpolicy = dtMergeCancelationPolicy.Select("CartID='" + UserCartId + "' and SupplierName='TBO_V1' and  ServiceId='" + ownsystemhotelId + "' and RoomTypeId='" + RoomTypeId + "' and MealId ='" + MealId + "' and  RoomNo='" + (j + 1).ToString() + "'");
                            string purchaseToken1 = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["purchaseToken"]);
                            //if (drpolicy.Count() > 0)
                            //{
                            //    cancellationpolicydate = drpolicy[0]["purchaseToken"].ToString();//   ["purchaseToken"].ToString();
                            //}
                            //else
                            //{
                            //    cancellationpolicydate = "";
                            //}
                            DataTable dt_TBO_Cancellation = _commonMethod.GenerateCancellationDataTableNewV2();
                            DataRow drHotelCancellation = dt_TBO_Cancellation.NewRow();
                            DateTime CancellationDeadline = DateTime.Now.Date;
                            double ItemPrice = roomprice;
                            double MainAgentCharge = ItemPrice;
                            double VatOnPolicy = 0;
                            double CancellationPrice = roomprice;
                            //if (purchaseToken1.ToLower() == "true")
                            if (purchaseToken1 != "")
                            {
                                bool isrefundable = DS.HotelResult[0].Rooms[0].IsRefundable;
                                if (!isrefundable)
                                {
                                    //dt_TBO_Cancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref MainAgentCharge, dt_TBO_Cancellation.NewRow(), CurrencyFactorTBO, TBOCurrency, SupplierName, ownsystemhotelId, Convert.ToString("1"),
                                    //       Convert.ToString(RoomTypeId), RoomName, Board_Text, DateTime.Now.Date, Convert.ToDateTime(CheckIn), CancellationPrice, MainAgentCharge, "0", VatOnPolicy, uniqid, "Yes", LastCancellationDeadline, Type, "", ""));
                                    //dt_TBO_Cancellation.AcceptChanges();
                                    //IsPolicyChangeTBO = true;
                                    FinalPrice = 0;
                                    priceCheckTboDto.ItemPrice = FinalPrice;
                                }
                                else
                                {
                                    if (DS.HotelResult[0].Rooms[0].CancelPolicies != null)
                                    {
                                        var sortedPolicies = DS.HotelResult[0].Rooms[0].CancelPolicies.Where(p => p.CancellationCharge != 0).OrderBy(p => p.FromDate).ToList(); //uncmnt #25-June-25
                                        //var sortedPolicies = DS.HotelResult[0].Rooms[0].CancelPolicies.OrderBy(p => p.FromDate).ToList();
                                        // var sortedPolicies = DS.HotelResult[0].Rooms[0].CancelPolicies.OrderBy(p => p.FromDate).ThenBy(p => p.CancellationCharge).ThenBy(p => p.Index).ToList();
                                        //var sortedPolicies = DS.HotelResult[0].Rooms[0].CancelPolicies.Where(p => p.CancellationCharge != 0).OrderBy(p => p.FromDate).ThenBy(p => p.CancellationCharge).ToList(); //cmnt #25-June-25
                                        //for (int v=0;v< DS.HotelResult[k].Rooms[R].CancelPolicies.Length;v++)
                                        DateTime lastdate = DateTime.Now.Date;
                                        for (int v = 0; v < sortedPolicies.Count(); v++)   //wholeloopchange #25-June-25
                                        {
                                            //string ChargeType = Convert.ToString(sortedPolicies[v].ChargeType);
                                            //DateTime dateString1 = DateTime.ParseExact(sortedPolicies[v].FromDate, "dd/MM/yyyy", CultureInfo.InvariantCulture).Date; //Convert.ToDateTime(sortedPolicies[v].FromDate);
                                            string dateString = Convert.ToString(sortedPolicies[v].FromDate);
                                            dateString = dateString.Substring(0, 2) + "/" + dateString.Substring(3, 2) + "/" + dateString.Substring(6, 4);
                                            DateTime dateString11 = DateTime.ParseExact(dateString, "dd/MM/yyyy", CultureInfo.InvariantCulture).Date; //Convert.ToDateTime(sortedPolicies[v].FromDate);

                                            if (v == 0)
                                            {
                                                lastdate = dateString11;
                                            }
                                            else
                                            {
                                                if (lastdate > dateString11)
                                                {
                                                    lastdate = dateString11;
                                                }
                                            }
                                        }

                                        //DateTime lastdate = Convert.ToDateTime(sortedPolicies[v].FromDate);
                                        //DateTime lastdate = DateTime.ParseExact(sortedPolicies[v].FromDate, "dd/MM/yyyy", null);
                                        //switch (ChargeType.ToLower())
                                        //{


                                        //    case "percentage":
                                        //        double Percentage1 = Convert.ToDouble(sortedPolicies[v].CancellationCharge);
                                        //        CancellationPrice = Convert.ToDouble(CancellationPrice * Percentage1 / 100);
                                        //        break;
                                        //    case "fixed":
                                        //        //double night = Convert.ToDouble(sortedPolicies[v].CancellationCharge);
                                        //        CancellationPrice = Convert.ToDouble(sortedPolicies[v].CancellationCharge);

                                        //        break;

                                        //    default:
                                        //        break;
                                        //}


                                        //LastCancellationDeadline = Convert.ToString(lastdate.Date);

                                        // LastCancellationDeadline = lastdate.Date.ToString("dd/MM/yyyy");
                                        LastCancellationDeadline = lastdate.ToString("dd/MM/yyyy");
                                        //LastCancellationDeadline = Convert.ToString(lastdate.Date);Common.GetCutOffDays(dsAgentDetails.Tables[1], "IWTX");
                                        //int CutOffDays = XMLHotelBookingCommon.CutOffDays;
                                        //CutOffDays = _commonMethod.GetCutOffDays(dsAgentDetails.Tables[1], SupplierName);
                                        //CancellationDeadline = Convert.ToDateTime(LastCancellationDeadline);
                                        ////if (CancellationDeadline < DateTime.Now.Date && Convert.ToDouble(sortedPolicies[v].CancellationCharge) == 0)
                                        ////{
                                        ////    CancellationDeadline = DateTime.Now.Date;
                                        ////}

                                        //CancelDate = CancellationDeadline;
                                        //if (CancelDate.AddDays(CutOffDays).Date > DateTime.Now.Date)
                                        //    CancelDate = CancelDate.AddDays(CutOffDays).Date;
                                        //else
                                        //{
                                        //    CancelDate = DateTime.Now.Date;
                                        //    MainAgentCharge = roomprice;
                                        //    CancellationPrice = roomprice;
                                        //    //CompanyChargeAmount = ItemPrice;
                                        //}





                                        //if (DateTime.Now.Date < CancelDate.Date)
                                        //{
                                        //    // if (CancellationPrice == 0)
                                        //    {
                                        //        double zero = 0;
                                        //        dt_TBO_Cancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref zero, dt_TBO_Cancellation.NewRow(), CurrencyFactorTBO, TBOCurrency, SupplierName, ownsystemhotelId, Convert.ToString("1"),
                                        //               Convert.ToString(RoomTypeId), RoomName, Board_Text, DateTime.Now.Date, CancelDate.AddDays(-1), 0, 0, "0", VatOnPolicy, uniqid, "NO", LastCancellationDeadline, Type, "", ""));
                                        //        dt_TBO_Cancellation.AcceptChanges();
                                        //    }

                                        //    dt_TBO_Cancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref MainAgentCharge, dt_TBO_Cancellation.NewRow(), CurrencyFactorTBO, TBOCurrency, SupplierName, ownsystemhotelId, Convert.ToString("1"),
                                        //           Convert.ToString(RoomTypeId), RoomName, Board_Text, Convert.ToDateTime(CancelDate), Convert.ToDateTime(CheckIn), CancellationPrice, MainAgentCharge, "0", VatOnPolicy, uniqid, "NO", LastCancellationDeadline, Type, "", ""));
                                        //    dt_TBO_Cancellation.AcceptChanges();
                                        //    // break;

                                        //}
                                        //else
                                        //{
                                        //    dt_TBO_Cancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref MainAgentCharge, dt_TBO_Cancellation.NewRow(), CurrencyFactorTBO, TBOCurrency, SupplierName, ownsystemhotelId, Convert.ToString("1"),
                                        //    Convert.ToString(RoomTypeId), RoomName, Board_Text, DateTime.Now.Date, Convert.ToDateTime(CheckIn), CancellationPrice, MainAgentCharge, "0", VatOnPolicy, uniqid, "Yes", LastCancellationDeadline, Type, "", ""));
                                        //    dt_TBO_Cancellation.AcceptChanges();
                                        //    IsPolicyChangeTBO = true;
                                        //    // break;
                                        //}
                                        //DataRow[] drpolicy = dtMergeCancelationPolicy.Select("CartID='" + UserCartId + "' and SupplierName='TBO' and  OwnsystemHotelId='" + ownsystemhotelId + "' and RoomTypeId='" + RoomTypeId + "' and MealId ='" + MealId + "' and  RoomNo='" + (j + 1).ToString() + "'");


                                        // }
                                        //if (IsPolicyChangeTBO)
                                        //{
                                        //    FinalPrice = 0;
                                        //    priceCheckTboDto.ItemPrice = FinalPrice;
                                        //}

                                        if (LastCancellationDeadline != purchaseToken1)
                                        {

                                            try
                                            {
                                                responsetype = "GeneralError";
                                                ResponseData = "#Policy Changed " + "#Old Policy = " + purchaseToken1 + " #New Policy = " + LastCancellationDeadline + " #Response= " + ResponseData;
                                               // int AgentId = Convert.ToInt32(Common.getSession("UserId") == "" ? "0" : Common.getSession("UserId"));
                                               _= _commonMethod.SupplierPerformanceReportForPrebookAndBook("TBO_V1", "PreBook", AgentId, time, responsetype, xslRequest, ResponseData, Error);
                                            }
                                            catch (Exception e)
                                            {
                                                _ = _exception.ErrorNew(e, _LogAPI.Prebook);
                                            }
                                            //IsPolicyChangeTBO = true;
                                            FinalPrice = 0;
                                            priceCheckTboDto.ItemPrice = FinalPrice;
                                            return priceCheckTboDto;
                                        }
                                    }
                                }
                            }

                            //priceCheckTboDto.IsPolicyChangeTBO = IsPolicyChangeTBO;

                            #endregion

                            if (Convert.ToDouble(Math.Round(FinalPrice.TruncateDecimalPlaces(2))) > Math.Round(CompanyBuyingPrice.TruncateDecimalPlaces(2)))
                            {

                                try
                                {
                                    responsetype = "GeneralError";
                                    ResponseData = "#Price Changed " + "#Main Price = " + CompanyBuyingPrice + " #New Price = " + FinalPrice + " #Response= " + ResponseData;
                                    string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, xslRequest, ResponseData, Error);
                                }
                                catch (Exception e)
                                {
                                    _ = _exception.ErrorNew(e,_LogAPI.Prebook);
                                }

                                priceCheckTboDto.ItemPrice = FinalPrice;
                                priceCheckTboDto.Flag = true;
                                return priceCheckTboDto;
                            }

                        }
                        else
                        {
                            try
                            {
                                responsetype = "GeneralError";
                                string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, xslRequest, ResponseData, Error);
                            }
                            catch (Exception e)
                            {
                                _ = _exception.ErrorNew(e, _LogAPI.Prebook);

                            }
                            FinalPrice = 0;

                            priceCheckTboDto.ItemPrice = FinalPrice;
                            priceCheckTboDto.Flag = false;
                            return priceCheckTboDto;
                        }
                    }
                    else
                    {
                        try
                        {
                            responsetype = "GeneralError";
                            string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, xslRequest, ResponseData, Error);
                        }
                        catch (Exception e)
                        {
                            _ = _exception.ErrorNew(e, _LogAPI.Prebook);

                        }
                        FinalPrice = 0;
                        priceCheckTboDto.ItemPrice = FinalPrice;
                        priceCheckTboDto.Flag = false;
                        return priceCheckTboDto;
                    }
                }
            }
            catch (Exception e)
            {
                try
                {
                    Error = e.Message.ToString() + "#=" + e.StackTrace.ToString();
                    responsetype = "GeneralError";
                    string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, xslRequest, ResponseData, Error);
                }
                catch (Exception ex)
                {
                    _ = _exception.ErrorNew(ex, _LogAPI.Prebook);

                }
                _ = _exception.ErrorNew(e, _LogAPI.Prebook);

                FinalPrice = 0;
                priceCheckTboDto.ItemPrice = FinalPrice;
                priceCheckTboDto.Flag = false;
                return priceCheckTboDto;
            }

            try
            {
                responsetype = "Success";
                string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, xslRequest, ResponseData, Error);
            }
            catch (Exception e)
            {
                _ = _exception.ErrorNew(e, _LogAPI.Prebook);

            }

            priceCheckTboDto.Flag = false;
            return priceCheckTboDto;
        }

        public async Task<TBO_BookingProcess> Get_TBO_BookingProcess_json(DataTable dtAgentVoucher, DataSet dsCartDetails, DataTable dtCheckPriceReqStructure, DataTable dtPassengers, DataTable dtCAncellationPolicy, DataRow[] TBODetails, string AgentVoucherNo, int bookingStatus, string UniqueNo, string AgentUniqueNo, bool isNonRefundable, int i, int j, string referenceNumber, TokenData tokenData, int AgentId = 0,string SearchKey="")
        {
            TBO_BookingProcess tBO_BookingProcess = new TBO_BookingProcess();
            //int AgentId = Convert.ToInt32(Common.getSession("UserId") == "" ? "0" : Common.getSession("UserId"));
            string responsetype = "Success";
            string RequestSPR = "";
            string ReponseSPR = "";
            string time = "";
            string Error = "";
            try
            {
                #region Declaration
                AgentVoucherNo = "";
                bookingStatus = 2;
                double FinalPrices = 0;
                string RatePlanCode = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomTypeID"]);
                string SupplierName = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["SupplierName"]);
                //string HotelId = Convert.ToString(dsCartDetails.Tables[0].DefaultView[j]["XMLHotelId"]);
                int UserCartId = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[0]["UserCartID"]);
                string XmlHotelid = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["XMLHotelId"]);
                int UserCartDetailId = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[j]["UserCartDetailId"]);
                // int UserCartDetailId = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[0]["UserCartDetailId"]);
                double CompanyBuyingPrices = Convert.ToDouble(dsCartDetails.Tables[1].DefaultView[j]["Rate"]);
                bool IsPriceUpdate = true;
                UniqueNo = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["UserCartDetailId"]);
                string BookingRequest = "";
                string PreBookingToken = "";
                #endregion

                #region PriceCkeck, Booking RQ, Booking URL
                string Nationality = "";
                //string RoomTypeId = Common.Get_JacTravel_RoomTypeID(RatePlanCode, ref Nationality);
                string CountryID = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["NationalityName"]);
                string UserName = Convert.ToString(TBODetails[0]["UserName"]); ;
                string Password = Convert.ToString(TBODetails[0]["Password"]); ;
               // string Url = Convert.ToString(TBODetails[0]["WebSiteURL"]) + Convert.ToString(TboInfo.Tbo_V1BookUrl);
                string Url = Convert.ToString(TBODetails[0]["WebSiteURL"]) + Convert.ToString(_supplierConfig.TBO.V1BookUrl);
                // IsPriceUpdate = TBO_PriceCheckRQRS(dsCartDetails, TBODetails, UserName, Password, i, j, ref dtCheckPriceReqStructure, UniqueNo, AgentUniqueNo, ref FinalPrices, true, CompanyBuyingPrices, ref PreBookingToken, XmlHotelid);

                string UniqueString = Convert.ToString(String.Concat(Guid.NewGuid().ToString("N").Select(c => (char)(c + 17)))).Substring(0, 4);
                string ClientReferancreNo = DateTime.Now.ToString("ddMMyyHHmmssfff") + "#" + UniqueString;

                bool IsNrf = false;
                if (true)
                {
                    if (dtCAncellationPolicy != null && dtCAncellationPolicy.Rows.Count > 0)
                    {
                        //dtCAncellationPolicy.DefaultView.RowFilter = "RoomTypeId='" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomTypeID"]) + "' and AgentCancellationPrice='0'";
                        string ownsystemhotelId = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["ServiceId"]);
                        dtCAncellationPolicy.DefaultView.RowFilter = ("CartID='" + UserCartId + "' and SupplierName='"+ SupplierName + "' and  ServiceId='" + ownsystemhotelId + "' and RoomTypeId='" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomTypeID"]) + "'");
                        if (dtCAncellationPolicy.DefaultView.Count <= 1)
                            isNonRefundable = true;
                    }
                    if (isNonRefundable == true)
                    {
                        IsNrf = true;
                        //DateTime CancelatonDate = Convert.ToDateTime(dtCAncellationPolicy.Rows[0]["PurchaseToken"]);
                        //DateTime todaydate = DateTime.Now.Date;
                        //if (todaydate <= CancelatonDate)
                        //{
                        //    IsNrf = true;
                        //}
                        //else
                        //{
                        //    IsNrf = false;
                        //}
                    }
                }

                BookingRequest = Get_TBO_BookingRequest_json(Url, dsCartDetails, dtPassengers, TBODetails, UniqueNo, IsPriceUpdate, i, j, PreBookingToken, UserName, Password, ClientReferancreNo, IsNrf, tokenData);

                _commonMethod.ThirdPartyBookingHistory(Convert.ToString(UniqueNo), Convert.ToString(AgentUniqueNo), BookingRequest, "-", "Before Request hit booking", Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["SupplierName"]), "Request Confirm", "-", "WithinEarth");



                string ResponseDatatest = "";
                isNonRefundable = false;
                #endregion

                #region Fraud Case





                System.Diagnostics.Stopwatch timerroomprocess = new System.Diagnostics.Stopwatch();
                timerroomprocess.Start();

                ResponseDto responseDto = await GetTrvcoResponseTBOJson(UserName, Password, BookingRequest, Url, 120000, SupplierName);

                ResponseDatatest = responseDto.Response;
                Error = responseDto.Error;

                timerroomprocess.Stop();
                time = timerroomprocess.Elapsed.ToString();

                _commonMethod.ThirdPartyBookingHistory(Convert.ToString(UniqueNo), Convert.ToString(AgentUniqueNo), BookingRequest, ResponseDatatest + Error, "After Request hit booking", Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["SupplierName"]), "Response Confirm", "-", "WithinEarth");
                RequestSPR = BookingRequest;
                ReponseSPR = ResponseDatatest + Error;


                //DataTable dtCheckPriceRe = (DataTable)HttpContext.Current.Session["CheckPriceReq"];
                //if (dtCheckPriceRe != null)
                //{
                //    dtCheckPriceRe.DefaultView.RowFilter = "UserCartDetailsID=" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["UserCartDetailID"]);
                //    if (dtCheckPriceRe.DefaultView.Count > 0)
                //        dtCheckPriceReqStructure.ImportRow(dtCheckPriceRe.DefaultView[0].Row);
                //}

                #endregion

                #region Booking Confirm, On Request, Rejected

                //string text = System.IO.File.ReadAllText(@"E:\BookingRS.txt");
                //ResponseDatatest = text;


                //bool IsTrue = Convert.ToBoolean(WHLBook_Response.Tables["ReturnStatus"].Rows[0]["Success"]);
                string SupplierRef = "";
                if (ResponseDatatest == "" || ResponseDatatest == null)
                {

                    _commonMethod.delayManual(12000);
                    string BookingDetailsRequest = "";

                    BookingDetailsRequest = Get_TBO_BookingDeatilsjson(TBODetails, ClientReferancreNo);
                    _commonMethod.ThirdPartyBookingHistory(Convert.ToString(UniqueNo), Convert.ToString(AgentUniqueNo), BookingDetailsRequest, "-", "BookingDetails Request", Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["SupplierName"]), "Request Confirm", "-", "WithinEarth");
                    Url = Convert.ToString(TBODetails[0]["WebSiteURL"]) + Convert.ToString(TboInfo.Tbo_V1BookingDetail);
                    string ResponseDetailsBooking = "";
                    ResponseDto responseDto1 = await GetTrvcoResponseTBOJson(UserName, Password, BookingRequest, Url, 120000, SupplierName);
                    ResponseDetailsBooking = responseDto1.Response;
                    Error = responseDto1.Error;

                    _commonMethod.ThirdPartyBookingHistory(Convert.ToString(UniqueNo), Convert.ToString(AgentUniqueNo), BookingDetailsRequest, ResponseDetailsBooking + Error, "BookingDetails Response", Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["SupplierName"]), "Request Confirm", "-", "WithinEarth");
                    RequestSPR = RequestSPR + "#BookingDetails= " + BookingDetailsRequest;
                    ReponseSPR = ReponseSPR + "#BookingDetails= " + ResponseDetailsBooking;
                    //Common.SaveRequestResponseOnLocal(BookingDetailsRequest, "TBO", "Request", "BookingDetails");
                    //Common.SaveRequestResponseOnLocal(ResponseDetailsBooking, "TBO", "Response", "BookingDetails");
                    if (ResponseDetailsBooking == "" || ResponseDetailsBooking == null)
                    {
                        AgentVoucherNo = "";
                        bookingStatus = 1;
                    }
                    else
                    {
                        TBOClass.BookingDetailTbores.Root DS = null;
                        try
                        {

                            DS = Newtonsoft.Json.JsonConvert.DeserializeObject<TBOClass.BookingDetailTbores.Root>(ResponseDetailsBooking);

                        }
                        catch (Exception ex)
                        {
                            _ = _exception.ErrorNew(ex, _LogAPI.Book, SearchKey);
                        }
                        if (DS.Status.Code == 200)
                        {
                            if (DS.BookingDetail != null)
                            {
                                //DataRow[] status = WHLBookDetails_Response.Tables["status"].Select("response_Id=" + BookingId + "");
                                var BookStatus = Convert.ToString(DS.BookingDetail.BookingStatus);
                                if (BookStatus.ToLower() == "confirmed" || BookStatus.ToLower() == "vouchered")
                                {
                                    SupplierRef = Convert.ToString(DS.BookingDetail.ConfirmationNumber);

                                    bookingStatus = 6;
                                    AgentVoucherNo = ClientReferancreNo + "|" + SupplierRef;
                                }
                                else
                                {
                                    AgentVoucherNo = "";
                                    bookingStatus = 2;
                                }
                            }
                            else
                            {
                                AgentVoucherNo = "";
                                bookingStatus = 2;
                            }
                        }
                        else
                        {
                            AgentVoucherNo = "";
                            bookingStatus = 2;
                        }
                    }
                }
                else
                {
                    TBOClass.BookingresTbo.Root DS1 = null;
                    try
                    {

                        DS1 = Newtonsoft.Json.JsonConvert.DeserializeObject<TBOClass.BookingresTbo.Root>(ResponseDatatest);

                    }
                    catch (Exception ex)
                    {
                        _ = _exception.ErrorNew(ex, _LogAPI.Book, SearchKey);

                    }

                    //if (Convert.ToString(TBOBook_Response.Tables["Status"].Rows[0]["StatusCode"]) == "01")
                    if (DS1.Status.Code == 200 && DS1.Status.Description != null)
                    {
                        if (DS1.ConfirmationNumber != null)
                        {
                            //DataRow[] status = WHLBookDetails_Response.Tables["status"].Select("response_Id=" + BookingId + "");
                            string ConfirmationNo = Convert.ToString(DS1.ConfirmationNumber);
                            string ClientReferenceId = Convert.ToString(DS1.ClientReferenceId);
                            bookingStatus = 6;
                            AgentVoucherNo = ClientReferenceId + "|" + ConfirmationNo;

                        }
                        else
                        {
                            _commonMethod.delayManual(12000);
                            string BookingDetailsRequest = "";

                            BookingDetailsRequest = Get_TBO_BookingDeatilsjson(TBODetails, ClientReferancreNo);
                            _commonMethod.ThirdPartyBookingHistory(Convert.ToString(UniqueNo), Convert.ToString(AgentUniqueNo), BookingDetailsRequest, "-", "BookingDetails Request", Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["SupplierName"]), "Request Confirm", "-", "WithinEarth");

                            string ResponseDetailsBooking = "";
                            Url = Convert.ToString(TBODetails[0]["WebSiteURL"]) + Convert.ToString(TboInfo.Tbo_V1BookingDetail);
                            ResponseDto responseDto2 = await GetTrvcoResponseTBOJson(UserName, Password, BookingRequest, Url, 120000, SupplierName);

                            ResponseDetailsBooking = responseDto2.Response;
                            Error = responseDto2.Error;

                            _commonMethod.ThirdPartyBookingHistory(Convert.ToString(UniqueNo), Convert.ToString(AgentUniqueNo), BookingDetailsRequest, ResponseDetailsBooking, "BookingDetails Response", Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["SupplierName"]), "Request Confirm", "-", "WithinEarth");

                            //Common.SaveRequestResponseOnLocal(BookingDetailsRequest, "TBO", "Request", "BookingDetails");
                            //Common.SaveRequestResponseOnLocal(ResponseDetailsBooking, "TBO", "Response", "BookingDetails");
                            if (ResponseDetailsBooking == "" || ResponseDetailsBooking == null)
                            {
                                AgentVoucherNo = "";
                                bookingStatus = 1;
                            }
                            else
                            {
                                TBOClass.BookingDetailTbores.Root DS = null;
                                try
                                {

                                    DS = Newtonsoft.Json.JsonConvert.DeserializeObject<TBOClass.BookingDetailTbores.Root>(ResponseDetailsBooking);

                                }
                                catch (Exception ex)
                                {
                                    _ = _exception.ErrorNew(ex, _LogAPI.Book, SearchKey);
                                }
                                if (DS.Status.Code == 200)
                                {
                                    if (DS.BookingDetail != null)
                                    {
                                        //DataRow[] status = WHLBookDetails_Response.Tables["status"].Select("response_Id=" + BookingId + "");
                                        var BookStatus = Convert.ToString(DS.BookingDetail.BookingStatus);
                                        if (BookStatus.ToLower() == "confirmed" || BookStatus.ToLower() == "vouchered")
                                        {
                                            SupplierRef = Convert.ToString(DS.BookingDetail.ConfirmationNumber);

                                            bookingStatus = 6;
                                            AgentVoucherNo = "" + "|" + SupplierRef;
                                        }
                                        else
                                        {
                                            AgentVoucherNo = "";
                                            bookingStatus = 2;
                                        }
                                    }
                                    else
                                    {
                                        AgentVoucherNo = "";
                                        bookingStatus = 2;
                                    }
                                }
                                else
                                {
                                    AgentVoucherNo = "";
                                    bookingStatus = 2;
                                }
                            }
                        }
                    }
                    else
                    {
                        AgentVoucherNo = "";
                        bookingStatus = 2;
                    }
                }

                if (IsNrf)
                {
                  await _commonMethod.UpdateTBOVoucherStatus(referenceNumber, UserCartDetailId, 1);
                }


                #endregion

                #region SupplierPR Booking
                if (bookingStatus == 6)
                {
                    responsetype = "Success";
                }
                else
                {
                    responsetype = "GeneralError";
                }
                try
                {
                    _ = _commonMethod.SupplierPerformanceReportForPrebookAndBook("TBO_V1", "Book", AgentId, time, responsetype, BookingRequest, ResponseDatatest, Error);
                }
                catch (Exception e)
                {
                    _ = _exception.ErrorNew(e, _LogAPI.Book, SearchKey);
                }
                #endregion

                #region Booking DataRaw
                DataRow newRow_TBO = dtAgentVoucher.NewRow();
                newRow_TBO["UserCartId"] = dsCartDetails.Tables[0].Rows[i]["UserCartId"];
                newRow_TBO["UserCartDetailId"] = dsCartDetails.Tables[1].DefaultView[j]["UserCartDetailId"];
                newRow_TBO["AgentVoucherNo"] = AgentVoucherNo;
                newRow_TBO["XMLRequest"] = BookingRequest;
                newRow_TBO["XMLResponse"] = ResponseDatatest;
                newRow_TBO["RoomStatus"] = bookingStatus;
                newRow_TBO["SupplierName"] = dsCartDetails.Tables[1].DefaultView[j]["SupplierName"];
                newRow_TBO["PackageId"] = Convert.ToInt32(dsCartDetails.Tables[0].Rows[i]["PackageId"]);
                dtAgentVoucher.Rows.Add(newRow_TBO);
                #endregion

            }
            catch (Exception ex)
            {
                #region SupplierPerformanceReport Catch
                try
                {
                    Error = ex.Message.ToString() + "#= " + ex.StackTrace.ToString();
                    responsetype = "GeneralError";
                    _ = _commonMethod.SupplierPerformanceReportForPrebookAndBook("TBO_V1", "Book", AgentId, time, responsetype, RequestSPR, ReponseSPR, Error);
                }
                catch (Exception e)
                {
                    _ = _exception.ErrorNew(e, _LogAPI.Book, SearchKey);
                }
                #endregion
                _ = _exception.ErrorNew(ex, _LogAPI.Book, SearchKey);
            }

            tBO_BookingProcess.AgentVoucherNo = AgentVoucherNo;
            tBO_BookingProcess.UniqueNo = UniqueNo;
            tBO_BookingProcess.AgentUniqueNo = AgentUniqueNo;
            tBO_BookingProcess.bookingStatus = bookingStatus;
            tBO_BookingProcess.isNonRefundable = isNonRefundable;
            tBO_BookingProcess.dtPassengers = dtPassengers;
            tBO_BookingProcess.dtAgentVoucher = dtAgentVoucher;
            tBO_BookingProcess.dsCartDetails = dsCartDetails;
            tBO_BookingProcess.dtCAncellationPolicy = dtCAncellationPolicy;
            return tBO_BookingProcess;
        }
     //   public async Task<TBO_BookingProcess> Get_TBO_BookingProcess_json_newversion(BookResponse_newversion bookResponse_1, List<AgentVoucher> agentVouchers, BookRequest_newversion bookRequest, DataTable dtCheckPriceReqStructure, DataTable dtPassengers, DataTable dtCAncellationPolicy, DataRow[] TBODetails, string AgentVoucherNo, int bookingStatus, string UniqueNo, string AgentUniqueNo, bool isNonRefundable, int i, int j, string referenceNumber, TokenData tokenData, int AgentId = 0, string SearchKey = "")
     //   {
     //       TBO_BookingProcess tBO_BookingProcess = new TBO_BookingProcess();
     //       //int AgentId = Convert.ToInt32(Common.getSession("UserId") == "" ? "0" : Common.getSession("UserId"));
     //       string responsetype = "Success";
     //       string RequestSPR = "";
     //       string ReponseSPR = "";
     //       string time = "";
     //       string Error = "";
     //       try
     //       {
     //           #region Declaration
     //           AgentVoucherNo = "";
     //           bookingStatus = 2;
     //           double FinalPrices = 0;
     //           string RatePlanCode = Convert.ToString(bookRequest.userCart.UserCartDetails[j].RoomTypeId);
     //           string SupplierName = Convert.ToString(bookRequest.userCart.UserCartDetails[j].SupplierName);
               
     //           int UserCartId = Convert.ToInt32(bookRequest.userCart.UserCartDetails[j].UserCartId);
     //           //string XmlHotelid = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["XMLHotelId"]);//need
     //           int UserCartDetailId = Convert.ToInt32(bookRequest.userCart.UserCartDetails[j].UserCartDetailId);
                
     //           //double CompanyBuyingPrices = Convert.ToDouble(dsCartDetails.Tables[1].DefaultView[j]["Rate"]);
     //           double CompanyBuyingPrices = Convert.ToDouble(bookRequest.userCart.UserCartDetails[j].BaseServiceBuyingAmount);
     //           bool IsPriceUpdate = true;
     //           UniqueNo = Convert.ToString(bookRequest.userCart.UserCartDetails[j].UserCartDetailId);
     //           string BookingRequest = "";
     //           string PreBookingToken = "";
     //           #endregion

     //           #region PriceCkeck, Booking RQ, Booking URL
     //           string Nationality = "";
     //           //string CountryID = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["NationalityName"]);//need
     //           string UserName = Convert.ToString(TBODetails[0]["UserName"]); ;
     //           string Password = Convert.ToString(TBODetails[0]["Password"]); ;
     //           //string Url = Convert.ToString(TBODetails[0]["WebSiteURL"]) + Convert.ToString(TboInfo.Tbo_V1BookUrl);
     //           string Url = Convert.ToString(TBODetails[0]["WebSiteURL"]) + Convert.ToString(_supplierConfig.TBO.V1BookUrl);

     //           string UniqueString = Convert.ToString(String.Concat(Guid.NewGuid().ToString("N").Select(c => (char)(c + 17)))).Substring(0, 4);
     //           string ClientReferancreNo = DateTime.Now.ToString("ddMMyyHHmmssfff") + "#" + UniqueString;

     //           bool IsNrf = false;
     //           if (true)
     //           {
     //               if (dtCAncellationPolicy != null && dtCAncellationPolicy.Rows.Count > 0)
     //               {
     //                   string ownsystemhotelId = Convert.ToString(bookRequest.userCart.ServiceId);
     //                   dtCAncellationPolicy.DefaultView.RowFilter = ("CartID='" + UserCartId + "' and SupplierName='" + SupplierName + "' and  ServiceId='" + ownsystemhotelId + "' and RoomTypeId='" + Convert.ToString(bookRequest.userCart.UserCartDetails[j].RoomTypeId) + "'");
     //                   if (dtCAncellationPolicy.DefaultView.Count <= 1)
     //                       isNonRefundable = true;
     //               }
     //               if (isNonRefundable == true)
     //               {
     //                   IsNrf = true;
                       
     //               }
     //           }

     //           BookingRequest = Get_TBO_BookingRequest_json_newversion(Url, bookRequest, dtPassengers, TBODetails, UniqueNo, IsPriceUpdate, i, j, PreBookingToken, UserName, Password, ClientReferancreNo, IsNrf, tokenData);

     //           _commonMethod.ThirdPartyBookingHistory(Convert.ToString(UniqueNo), Convert.ToString(AgentUniqueNo), BookingRequest, "-", "Before Request hit booking", Convert.ToString(bookRequest.userCart.UserCartDetails[j].SupplierName), "Request Confirm", "-", "WithinEarth");



     //           string ResponseDatatest = "";
     //           isNonRefundable = false;
     //           #endregion

     //           #region Fraud Case





     //           System.Diagnostics.Stopwatch timerroomprocess = new System.Diagnostics.Stopwatch();
     //           timerroomprocess.Start();

     //           ResponseDto responseDto = await GetTrvcoResponseTBOJson(UserName, Password, BookingRequest, Url, 120000, SupplierName);

     //           ResponseDatatest = responseDto.Response;
     //           Error = responseDto.Error;

     //           timerroomprocess.Stop();
     //           time = timerroomprocess.Elapsed.ToString();

     //           _commonMethod.ThirdPartyBookingHistory(Convert.ToString(UniqueNo), Convert.ToString(AgentUniqueNo), BookingRequest, ResponseDatatest + Error, "After Request hit booking", Convert.ToString(bookRequest.userCart.UserCartDetails[j].SupplierName), "Response Confirm", "-", "WithinEarth");
     //           RequestSPR = BookingRequest;
     //           ReponseSPR = ResponseDatatest + Error;


     //           #endregion

     //           #region Booking Confirm, On Request, Rejected

     //           //string text = System.IO.File.ReadAllText(@"E:\BookingRS.txt");
     //           //ResponseDatatest = text;


     //           //bool IsTrue = Convert.ToBoolean(WHLBook_Response.Tables["ReturnStatus"].Rows[0]["Success"]);
     //           string SupplierRef = "";
     //           if (ResponseDatatest == "" || ResponseDatatest == null)
     //           {

     //               _commonMethod.delayManual(12000);
     //               string BookingDetailsRequest = "";

     //               BookingDetailsRequest = Get_TBO_BookingDeatilsjson(TBODetails, ClientReferancreNo);
     //               _commonMethod.ThirdPartyBookingHistory(Convert.ToString(UniqueNo), Convert.ToString(AgentUniqueNo), BookingDetailsRequest, "-", "BookingDetails Request", Convert.ToString(bookRequest.userCart.UserCartDetails[j].SupplierName), "Request Confirm", "-", "WithinEarth");
     //               Url = Convert.ToString(TBODetails[0]["WebSiteURL"]) + Convert.ToString(TboInfo.Tbo_V1BookingDetail);
     //               string ResponseDetailsBooking = "";
     //               ResponseDto responseDto1 = await GetTrvcoResponseTBOJson(UserName, Password, BookingRequest, Url, 120000, SupplierName);
     //               ResponseDetailsBooking = responseDto1.Response;
     //               Error = responseDto1.Error;

     //               _commonMethod.ThirdPartyBookingHistory(Convert.ToString(UniqueNo), Convert.ToString(AgentUniqueNo), BookingDetailsRequest, ResponseDetailsBooking + Error, "BookingDetails Response", Convert.ToString(bookRequest.userCart.UserCartDetails[j].SupplierName), "Request Confirm", "-", "WithinEarth");
     //               RequestSPR = RequestSPR + "#BookingDetails= " + BookingDetailsRequest;
     //               ReponseSPR = ReponseSPR + "#BookingDetails= " + ResponseDetailsBooking;
     //               //Common.SaveRequestResponseOnLocal(BookingDetailsRequest, "TBO", "Request", "BookingDetails");
     //               //Common.SaveRequestResponseOnLocal(ResponseDetailsBooking, "TBO", "Response", "BookingDetails");
     //               if (ResponseDetailsBooking == "" || ResponseDetailsBooking == null)
     //               {
     //                   AgentVoucherNo = "";
     //                   bookingStatus = 1;
     //               }
     //               else
     //               {
     //                   TBOClass.BookingDetailTbores.Root DS = null;
     //                   try
     //                   {

     //                       DS = Newtonsoft.Json.JsonConvert.DeserializeObject<TBOClass.BookingDetailTbores.Root>(ResponseDetailsBooking);

     //                   }
     //                   catch (Exception ex)
     //                   {
     //                       _ = _exception.ErrorNew(ex, _LogAPI.Book, SearchKey);
     //                   }
     //                   if (DS.Status.Code == 200)
     //                   {
     //                       if (DS.BookingDetail != null)
     //                       {
     //                           //DataRow[] status = WHLBookDetails_Response.Tables["status"].Select("response_Id=" + BookingId + "");
     //                           var BookStatus = Convert.ToString(DS.BookingDetail.BookingStatus);
     //                           if (BookStatus.ToLower() == "confirmed" || BookStatus.ToLower() == "vouchered")
     //                           {
     //                               SupplierRef = Convert.ToString(DS.BookingDetail.ConfirmationNumber);

     //                               bookingStatus = 6;
     //                               AgentVoucherNo = ClientReferancreNo + "|" + SupplierRef;
     //                           }
     //                           else
     //                           {
     //                               AgentVoucherNo = "";
     //                               bookingStatus = 2;
     //                           }
     //                       }
     //                       else
     //                       {
     //                           AgentVoucherNo = "";
     //                           bookingStatus = 2;
     //                       }
     //                   }
     //                   else
     //                   {
     //                       AgentVoucherNo = "";
     //                       bookingStatus = 2;
     //                   }
     //               }
     //           }
     //           else
     //           {
     //               TBOClass.BookingresTbo.Root DS1 = null;
     //               try
     //               {

     //                   DS1 = Newtonsoft.Json.JsonConvert.DeserializeObject<TBOClass.BookingresTbo.Root>(ResponseDatatest);

     //               }
     //               catch (Exception ex)
     //               {
     //                   _ = _exception.ErrorNew(ex, _LogAPI.Book, SearchKey);

     //               }

     //               //if (Convert.ToString(TBOBook_Response.Tables["Status"].Rows[0]["StatusCode"]) == "01")
     //               if (DS1.Status.Code == 200 && DS1.Status.Description != null)
     //               {
     //                   if (DS1.ConfirmationNumber != null)
     //                   {
     //                       //DataRow[] status = WHLBookDetails_Response.Tables["status"].Select("response_Id=" + BookingId + "");
     //                       string ConfirmationNo = Convert.ToString(DS1.ConfirmationNumber);
     //                       string ClientReferenceId = Convert.ToString(DS1.ClientReferenceId);
     //                       bookingStatus = 6;
     //                       AgentVoucherNo = ClientReferenceId + "|" + ConfirmationNo;

     //                   }
     //                   else
     //                   {
     //                       _commonMethod.delayManual(12000);
     //                       string BookingDetailsRequest = "";

     //                       BookingDetailsRequest = Get_TBO_BookingDeatilsjson(TBODetails, ClientReferancreNo);
     //                       _commonMethod.ThirdPartyBookingHistory(Convert.ToString(UniqueNo), Convert.ToString(AgentUniqueNo), BookingDetailsRequest, "-", "BookingDetails Request", Convert.ToString(bookRequest.userCart.UserCartDetails[j].SupplierName), "Request Confirm", "-", "WithinEarth");

     //                       string ResponseDetailsBooking = "";
     //                       Url = Convert.ToString(TBODetails[0]["WebSiteURL"]) + Convert.ToString(TboInfo.Tbo_V1BookingDetail);
     //                       ResponseDto responseDto2 = await GetTrvcoResponseTBOJson(UserName, Password, BookingRequest, Url, 120000, SupplierName);

     //                       ResponseDetailsBooking = responseDto2.Response;
     //                       Error = responseDto2.Error;

     //                       _commonMethod.ThirdPartyBookingHistory(Convert.ToString(UniqueNo), Convert.ToString(AgentUniqueNo), BookingDetailsRequest, ResponseDetailsBooking, "BookingDetails Response", Convert.ToString(bookRequest.userCart.UserCartDetails[j].SupplierName), "Request Confirm", "-", "WithinEarth");

     //                       //Common.SaveRequestResponseOnLocal(BookingDetailsRequest, "TBO", "Request", "BookingDetails");
     //                       //Common.SaveRequestResponseOnLocal(ResponseDetailsBooking, "TBO", "Response", "BookingDetails");
     //                       if (ResponseDetailsBooking == "" || ResponseDetailsBooking == null)
     //                       {
     //                           AgentVoucherNo = "";
     //                           bookingStatus = 1;
     //                       }
     //                       else
     //                       {
     //                           TBOClass.BookingDetailTbores.Root DS = null;
     //                           try
     //                           {

     //                               DS = Newtonsoft.Json.JsonConvert.DeserializeObject<TBOClass.BookingDetailTbores.Root>(ResponseDetailsBooking);

     //                           }
     //                           catch (Exception ex)
     //                           {
     //                               _ = _exception.ErrorNew(ex, _LogAPI.Book, SearchKey);
     //                           }
     //                           if (DS.Status.Code == 200)
     //                           {
     //                               if (DS.BookingDetail != null)
     //                               {
     //                                   //DataRow[] status = WHLBookDetails_Response.Tables["status"].Select("response_Id=" + BookingId + "");
     //                                   var BookStatus = Convert.ToString(DS.BookingDetail.BookingStatus);
     //                                   if (BookStatus.ToLower() == "confirmed" || BookStatus.ToLower() == "vouchered")
     //                                   {
     //                                       SupplierRef = Convert.ToString(DS.BookingDetail.ConfirmationNumber);

     //                                       bookingStatus = 6;
     //                                       AgentVoucherNo = "" + "|" + SupplierRef;
     //                                   }
     //                                   else
     //                                   {
     //                                       AgentVoucherNo = "";
     //                                       bookingStatus = 2;
     //                                   }
     //                               }
     //                               else
     //                               {
     //                                   AgentVoucherNo = "";
     //                                   bookingStatus = 2;
     //                               }
     //                           }
     //                           else
     //                           {
     //                               AgentVoucherNo = "";
     //                               bookingStatus = 2;
     //                           }
     //                       }
     //                   }
     //               }
     //               else
     //               {
     //                   AgentVoucherNo = "";
     //                   bookingStatus = 2;
     //               }
     //           }

     //           if (IsNrf)
     //           {
     //               await _commonMethod.UpdateTBOVoucherStatus(referenceNumber, UserCartDetailId, 1);
     //           }


     //           #endregion

     //           #region SupplierPR Booking
     //           if (bookingStatus == 6)
     //           {
     //               responsetype = "Success";
     //           }
     //           else
     //           {
     //               responsetype = "GeneralError";
     //           }
     //           try
     //           {
     //               _ = _commonMethod.SupplierPerformanceReportForPrebookAndBook("TBO_V1", "Book", AgentId, time, responsetype, BookingRequest, ResponseDatatest, Error);
     //           }
     //           catch (Exception e)
     //           {
     //               _ = _exception.ErrorNew(e, _LogAPI.Book, SearchKey);
     //           }
     //           #endregion

     //           //#region Booking DataRaw
     //           //DataRow newRow_TBO = dtAgentVoucher.NewRow();
     //           //newRow_TBO["UserCartId"] = bookRequest.userCart.UserCartDetails[j].UserCartId;
     //           //newRow_TBO["UserCartDetailId"] = bookRequest.userCart.UserCartDetails[j].UserCartDetailId;
     //           //newRow_TBO["AgentVoucherNo"] = AgentVoucherNo;
     //           //newRow_TBO["XMLRequest"] = BookingRequest;
     //           //newRow_TBO["XMLResponse"] = ResponseDatatest;
     //           //newRow_TBO["RoomStatus"] = bookingStatus;
     //           //newRow_TBO["SupplierName"] = bookRequest.userCart.UserCartDetails[j].SupplierName;
     //           ////newRow_TBO["PackageId"] = Convert.ToInt32(dsCartDetails.Tables[0].Rows[i]["PackageId"]);
     //           //dtAgentVoucher.Rows.Add(newRow_TBO);
     //           //#endregion

     //           #region BookingData
     //           AgentVoucher agentVoucher = new AgentVoucher();
     //           agentVoucher.UserCartId = Convert.ToInt32(bookRequest.userCart.UserCartDetails[j].UserCartId);
     //           agentVoucher.UserCartDetailId = Convert.ToInt32(bookRequest.userCart.UserCartDetails[j].UserCartDetailId);
     //           agentVoucher.AgentVoucherNo = AgentVoucherNo;
     //           agentVoucher.XMLRequest = BookingRequest;
     //           agentVoucher.XMLResponse = ResponseDatatest;
     //           agentVoucher.RoomStatus = Convert.ToString(bookingStatus);
     //           agentVoucher.SupplierName = bookRequest.userCart.UserCartDetails[j].SupplierName;
     //           agentVouchers.Add(agentVoucher);
     //           bookResponse_1.agentVouchers = agentVouchers;
     //           #endregion

     //       }
     //       catch (Exception ex)
     //       {
     //           #region SupplierPerformanceReport Catch
     //           try
     //           {
     //               Error = ex.Message.ToString() + "#= " + ex.StackTrace.ToString();
     //               responsetype = "GeneralError";
     //               _ = _commonMethod.SupplierPerformanceReportForPrebookAndBook("TBO_V1", "Book", AgentId, time, responsetype, RequestSPR, ReponseSPR, Error);
     //           }
     //           catch (Exception e)
     //           {
     //               _ = _exception.ErrorNew(e, _LogAPI.Book, SearchKey);
     //           }
     //           #endregion
     //           _ = _exception.ErrorNew(ex, _LogAPI.Book, SearchKey);
     //       }

     //       tBO_BookingProcess.AgentVoucherNo = AgentVoucherNo;
     //       tBO_BookingProcess.UniqueNo = UniqueNo;
     //       tBO_BookingProcess.AgentUniqueNo = AgentUniqueNo;
     //       tBO_BookingProcess.bookingStatus = bookingStatus;
     //       tBO_BookingProcess.isNonRefundable = isNonRefundable;
     //       tBO_BookingProcess.dtPassengers = dtPassengers;
     //       //tBO_BookingProcess.dtAgentVoucher = agentVouchers;
     //      // tBO_BookingProcess.dtAgentVoucher = ConvertToDataTable(agentVouchers);

     //       DataSet ds = new DataSet();

     ////       if (bookRequest?.userCart?.UserCartDetails != null
     ////&& bookRequest.userCart.UserCartDetails.Any())
     ////       {
     ////           DataTable dtCart = ToDataTable(bookRequest.userCart.UserCartDetails);
     ////           dtCart.TableName = "UserCartDetails"; // ✅ correct table name
     ////           ds.Tables.Add(dtCart);
     ////       }

     //       // assign
     //       tBO_BookingProcess.dsCartDetails = ds;
     //       //tBO_BookingProcess.dsCartDetails = bookRequest;//need
     //       tBO_BookingProcess.dtCAncellationPolicy = dtCAncellationPolicy;
     //       return tBO_BookingProcess;
     //   }
        public string Get_TBO_BookingRequest_json(string Url, DataSet dsCartDetails, DataTable dtPassengers, DataRow[] TBODetails, string UniqueNo, bool IsPriceUpdate, int i, int j, string PreBookingToken, string UserID, string Password, string ClientReferancreNo, bool IsNrf, TokenData tokenData)
        {
            try
            {
                string RequestString = "";
                dsCartDetails.Tables[1].DefaultView.RowFilter = "UserCartID=" + Convert.ToString(dsCartDetails.Tables[0].Rows[i]["UserCartID"]);
                if (dsCartDetails.Tables[1].DefaultView.Count > 0)
                {
                    #region TboCurrencyFactor
                    double TBOCurrencyFactor = 1;
                    string TboCurrency = Convert.ToString(TBODetails[0]["CurrencyShortName"]);

                    if (TboCurrency.ToLower() != tokenData.SiteCurrency.ToLower())
                    {
                        TBOCurrencyFactor = _commonMethod.GetROEByCurrency(TboCurrency, tokenData.dtROE);
                    }


                    #endregion
                    string RegionID, CheckIn, CheckOut, HotelCode, ChainCode, CityCode, Nationality, BookingCode;

                    /// HotelCode = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["XMLHotelId"]);

                    CheckIn = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["CheckInDate"]);
                    CheckIn = CheckIn.Substring(0, 4) + "-" + CheckIn.Substring(4, 2) + "-" + CheckIn.Substring(6, 2);
                    CheckOut = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["CheckOutDate"]);
                    CheckOut = CheckOut.Substring(0, 4) + "-" + CheckOut.Substring(4, 2) + "-" + CheckOut.Substring(6, 2);

                    double RoomTotalFare = Convert.ToDouble(dsCartDetails.Tables[1].Rows[j]["CompanyBuyingPrice"]) / TBOCurrencyFactor;

                    string SessionId, ResultIndex;

                    string[] Arr = Convert.ToString(dsCartDetails.Tables[1].DefaultView[0]["RoomTypeId"]).Split(new String[] { "$$$" }, StringSplitOptions.None);
                    //RoomTypeCode = Arr[0];
                    BookingCode = Arr[0];

                    string hotelname = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["ServiceName"]);
                    hotelname = hotelname.Replace("&", "And");

                    CityCode = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["XMLCityId"]);
                    string TBOUserName = Convert.ToString(TBODetails[0]["UserName"]);
                    string TBOPassword = Convert.ToString(TBODetails[0]["Password"]);
                    List<BookingTbo.CustomerDetail> CustomerDetails = new List<BookingTbo.CustomerDetail>();
                    for (int rr = 0; rr < dsCartDetails.Tables[1].DefaultView.Count; rr++)
                    {
                        //dtPassengers.DefaultView.RowFilter = "PickUp='" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[0]["RoomTypeId"]) + "' and UserCartID=" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[0]["UserCartID"]) + "' and RoomNo='" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[rr]["RoomNumber"]) + "'";
                        dtPassengers.DefaultView.RowFilter = "PickUp='" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[0]["RoomTypeID"]) + "' and RoomNo='" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[rr]["RoomNumber"]) + "'";

                        DataView dv = dtPassengers.DefaultView;
                        // dv.Sort = "IsLead asc";
                        DataTable sortedPassangerDT = dv.ToTable();


                        BookingTbo.CustomerDetail CustomerDetail = new BookingTbo.CustomerDetail();

                        List<BookingTbo.CustomerName> CustomerName = new List<BookingTbo.CustomerName>();
                        for (int p = 0; p < sortedPassangerDT.Rows.Count; p++)
                        {

                            BookingTbo.CustomerName CustomerNames = new BookingTbo.CustomerName();
                            string IsLead = "false";

                            if (Convert.ToString(dtPassengers.DefaultView[p]["IsLead"]) == "1")
                            {
                                IsLead = "true";
                            }
                            else
                            {
                                IsLead = "false";
                            }
                            //if (p == 0 && Convert.ToString(dtPassengers.DefaultView[p]["PaxType"]).ToLower() != "child")
                            //    IsLead = "true";

                            string firstname = dtPassengers.DefaultView[p]["FirstName"].ToString();
                            firstname = firstname.Replace("     ", " ");
                            firstname = firstname.Replace("    ", " ");
                            firstname = firstname.Replace("   ", " ");
                            firstname = firstname.Replace("  ", " ");

                            string lastname = dtPassengers.DefaultView[p]["LastName"].ToString();
                            lastname = lastname.Replace("     ", " ");
                            lastname = lastname.Replace("    ", " ");
                            lastname = lastname.Replace("   ", " ");
                            lastname = lastname.Replace("  ", " ");
                            if (IsLead == "true")
                            {
                                CustomerNames.Title = dtPassengers.DefaultView[p]["Prefix"].ToString().Replace(".", "");
                                CustomerNames.FirstName = firstname;
                                CustomerNames.LastName = lastname;
                                CustomerNames.Type = dtPassengers.DefaultView[p]["PaxType"].ToString();

                            }
                            else
                            {
                                CustomerNames.Title = dtPassengers.DefaultView[p]["Prefix"].ToString().Replace(".", "");
                                CustomerNames.FirstName = firstname;
                                CustomerNames.LastName = lastname;
                                CustomerNames.Type = dtPassengers.DefaultView[p]["PaxType"].ToString();
                            }
                            CustomerName.Add(CustomerNames);

                        }
                        CustomerDetail.CustomerNames = CustomerName;
                        CustomerDetails.Add(CustomerDetail);

                    }


                    JsonSerializerSettings Setting = new JsonSerializerSettings();
                    BookingTbo.Root Settings = new BookingTbo.Root();
                    Settings.BookingCode = BookingCode;
                    Settings.CustomerDetails = CustomerDetails;
                    Settings.ClientReferenceId = ClientReferancreNo;
                    Settings.BookingReferenceId = ClientReferancreNo;
                    Settings.TotalFare = RoomTotalFare;
                    Settings.EmailId = "";
                    Settings.PhoneNumber = "";
                    //if (IsNrf)
                    Settings.BookingType = "Voucher";

                    //else
                    //    Settings.BookingType = "Confirm";
                    Settings.PaymentMode = "Limit";


                    Setting.NullValueHandling = NullValueHandling.Ignore;
                    Setting.DefaultValueHandling = DefaultValueHandling.Ignore;
                    // RequestString = JsonConvert.SerializeObject(Settings, Setting);
                    RequestString = JsonConvert.SerializeObject(Settings);

                    return RequestString;
                }


            }
            catch (Exception ex)
            {
                _exception.ErrorNew(ex, _LogAPI.Book);
            }
            return string.Empty;
        }
        public string Get_TBO_BookingRequest_json_newversion(string Url, BookRequest_newversion bookRequest, DataTable dtPassengers, DataRow[] TBODetails, string UniqueNo, bool IsPriceUpdate, int i, int j, string PreBookingToken, string UserID, string Password, string ClientReferancreNo, bool IsNrf, TokenData tokenData)
        {
            try
            {
                string RequestString = "";

                int userCartId = bookRequest.userCart.UserCartDetails[j].UserCartId;

                var filteredCart = bookRequest.userCart.UserCartDetails
                    .Where(x => x.UserCartId == userCartId)
                    .ToList();
                //dsCartDetails.Tables[1].DefaultView.RowFilter = "UserCartID=" + Convert.ToString(dsCartDetails.Tables[0].Rows[i]["UserCartID"]);
                if (filteredCart?.Any() == true)
                {
                    #region TboCurrencyFactor
                    double TBOCurrencyFactor = 1;
                    string TboCurrency = Convert.ToString(TBODetails[0]["CurrencyShortName"]);

                    if(TboCurrency.ToLower() != tokenData.SiteCurrency.ToLower())
                    {
                        TBOCurrencyFactor = _commonMethod.GetROEByCurrency(TboCurrency, tokenData.dtROE);
                    }
                 

                    #endregion
                    string RegionID, CheckIn, CheckOut, HotelCode, ChainCode, CityCode, Nationality, BookingCode;

                    /// HotelCode = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["XMLHotelId"]);

                    CheckIn = Convert.ToString(bookRequest.userCart.UserCartDetails[j].CheckInDate);
                    CheckIn = CheckIn.Substring(0, 4) + "-" + CheckIn.Substring(4, 2) + "-" + CheckIn.Substring(6, 2);
                    CheckOut = Convert.ToString(bookRequest.userCart.UserCartDetails[j].CheckOutDate);
                    CheckOut = CheckOut.Substring(0, 4) + "-" + CheckOut.Substring(4, 2) + "-" + CheckOut.Substring(6, 2);

                    double RoomTotalFare = Convert.ToDouble(bookRequest.userCart.UserCartDetails[j].BaseServiceBuyingAmount) / TBOCurrencyFactor;

                    string SessionId, ResultIndex;

                    string[] Arr = Convert.ToString(bookRequest.userCart.UserCartDetails[j].RoomTypeId).Split(new String[] { "$$$" }, StringSplitOptions.None);
                    //RoomTypeCode = Arr[0];
                    BookingCode = Arr[0];

                    string hotelname = Convert.ToString(bookRequest.userCart.ServiceName);
                    hotelname = hotelname.Replace("&", "And");

                   // CityCode = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["XMLCityId"]);
                    CityCode = Convert.ToString(bookRequest.userCart.CityId);//need
                    string TBOUserName = Convert.ToString(TBODetails[0]["UserName"]);
                    string TBOPassword = Convert.ToString(TBODetails[0]["Password"]);
                    List<BookingTbo.CustomerDetail> CustomerDetails = new List<BookingTbo.CustomerDetail>();
                    for (int rr = 0; rr < bookRequest.userCart.UserCartDetails.Count; rr++)
                    {
                        dtPassengers.DefaultView.RowFilter = "PickUp='" + Convert.ToString(bookRequest.userCart.UserCartDetails[j].RoomTypeId) + "' and RoomNo='" + Convert.ToString(bookRequest.userCart.UserCartDetails[j].RoomNo) + "'";

                        DataView dv = dtPassengers.DefaultView;
                        // dv.Sort = "IsLead asc";
                        DataTable sortedPassangerDT = dv.ToTable();


                        BookingTbo.CustomerDetail CustomerDetail = new BookingTbo.CustomerDetail();

                        List<BookingTbo.CustomerName> CustomerName = new List<BookingTbo.CustomerName>();
                        for (int p = 0; p < sortedPassangerDT.Rows.Count; p++)
                        {

                            BookingTbo.CustomerName CustomerNames = new BookingTbo.CustomerName();
                            string IsLead = "false";

                            if (Convert.ToString(dtPassengers.DefaultView[p]["IsLead"]) == "1")
                            {
                                IsLead = "true";
                            }
                            else
                            {
                                IsLead = "false";
                            }
                            //if (p == 0 && Convert.ToString(dtPassengers.DefaultView[p]["PaxType"]).ToLower() != "child")
                            //    IsLead = "true";

                            string firstname = dtPassengers.DefaultView[p]["FirstName"].ToString();
                            firstname = firstname.Replace("     ", " ");
                            firstname = firstname.Replace("    ", " ");
                            firstname = firstname.Replace("   ", " ");
                            firstname = firstname.Replace("  ", " ");

                            string lastname = dtPassengers.DefaultView[p]["LastName"].ToString();
                            lastname = lastname.Replace("     ", " ");
                            lastname = lastname.Replace("    ", " ");
                            lastname = lastname.Replace("   ", " ");
                            lastname = lastname.Replace("  ", " ");
                            if (IsLead == "true")
                            {
                                CustomerNames.Title = dtPassengers.DefaultView[p]["Prefix"].ToString().Replace(".", "");
                                CustomerNames.FirstName = firstname;
                                CustomerNames.LastName = lastname;
                                CustomerNames.Type = dtPassengers.DefaultView[p]["PaxType"].ToString();

                            }
                            else
                            {
                                CustomerNames.Title = dtPassengers.DefaultView[p]["Prefix"].ToString().Replace(".", "");
                                CustomerNames.FirstName = firstname;
                                CustomerNames.LastName = lastname;
                                CustomerNames.Type = dtPassengers.DefaultView[p]["PaxType"].ToString();
                            }
                            CustomerName.Add(CustomerNames);

                        }
                        CustomerDetail.CustomerNames = CustomerName;
                        CustomerDetails.Add(CustomerDetail);

                    }


                    JsonSerializerSettings Setting = new JsonSerializerSettings();
                    BookingTbo.Root Settings = new BookingTbo.Root();
                    Settings.BookingCode = BookingCode;
                    Settings.CustomerDetails = CustomerDetails;
                    Settings.ClientReferenceId = ClientReferancreNo;
                    Settings.BookingReferenceId = ClientReferancreNo;
                    Settings.TotalFare = RoomTotalFare;
                    Settings.EmailId = "";
                    Settings.PhoneNumber = "";
                    //if (IsNrf)
                    Settings.BookingType = "Voucher";

                    //else
                    //    Settings.BookingType = "Confirm";
                    Settings.PaymentMode = "Limit";


                    Setting.NullValueHandling = NullValueHandling.Ignore;
                    Setting.DefaultValueHandling = DefaultValueHandling.Ignore;
                    // RequestString = JsonConvert.SerializeObject(Settings, Setting);
                    RequestString = JsonConvert.SerializeObject(Settings);

                    return RequestString;
                }


            }
            catch (Exception ex)
            {
                _exception.ErrorNew(ex, _LogAPI.Book);
            }
            return string.Empty;
        }
        public string Get_TBO_BookingDeatilsjson(DataRow[] TBODetails, string ClientReferancreNo)
        {
            try
            {


                string PaymentMode = "Limit", Request = "";
                JsonSerializerSettings Setting = new JsonSerializerSettings();
                BookingDetailTbo.Root RootObject = new BookingDetailTbo.Root();
                RootObject.BookingReferenceId = ClientReferancreNo;
                RootObject.PaymentMode = PaymentMode;

                Setting.NullValueHandling = NullValueHandling.Ignore;
                Setting.DefaultValueHandling = DefaultValueHandling.Ignore;
                Request = JsonConvert.SerializeObject(RootObject, Setting);
                return Request;
            }
            catch (Exception ex)
            {
                _ = _exception.ErrorNew(ex, _LogAPI.Book);
            }
            return string.Empty;
        }

        public string Get_TBO_BookingCancelRQ_json(string AgentVoucherNo, DataRow[] TBODetails, string cancelreason)
        {
            string Request = "";
            try
            {
                string[] strAgentVoucherNo = AgentVoucherNo.Split('|');
                string ConfirmationNo = Convert.ToString(strAgentVoucherNo[1]);
                JsonSerializerSettings Setting = new JsonSerializerSettings();
                BookingCancelTbo.Root RootObject = new BookingCancelTbo.Root();
                RootObject.ConfirmationNumber = ConfirmationNo;


                Setting.NullValueHandling = NullValueHandling.Ignore;
                Setting.DefaultValueHandling = DefaultValueHandling.Ignore;
                Request = JsonConvert.SerializeObject(RootObject, Setting);
                return Request;
            }
            catch (Exception ex)
            {
                _ = _exception.ErrorNew(ex, _LogAPI.CancelBooking);
            }
            return string.Empty;
        }

       
    }
}
