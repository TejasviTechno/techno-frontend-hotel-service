using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using XConnect.CommonUtilities.Interface;
using XConnect.CommonUtilities;
using XConnect.ErrorLogHandler.Interface;
using XConnect.Markup.Interface;
using XConnect.Models.Hotel;
using XConnect.Models;
using XConnect.Supplier.Hyperguest.Interface;
using Options = XConnect.Models.Options;
using System.Text.Json;
using System.Globalization;
using static Azure.Core.HttpHeader;
using Microsoft.IdentityModel.Protocols;
using static XConnect.Supplier.Hyperguest.HyperguestClass;
using static XConnect.Supplier.Booking.Com.preebookrs;
using System.Net;
using Newtonsoft.Json;

namespace XConnect.Supplier.Hyperguest
{
    public class HyperguestRepository : IHyperguest
    {
        private readonly ICommonMethod _commonMethod;
        private readonly IException _exception;
        private readonly IMarkup _markup;
        private readonly ISupplierRes _supplierRes;
        private readonly KeySettings _keySettings;
        private readonly IVCN _vcn;
        private readonly SupplierConfiguration _supplierConfig;

        public HyperguestRepository(ICommonMethod commonMethod, IException exception, IMarkup markup, ISupplierRes supplierRes, IOptions<KeySettings> keySettings, IVCN vcn, IOptions<SupplierConfiguration> supplierConfig)
        {
            _commonMethod = commonMethod;
            _exception = exception;
            _markup = markup;
            _supplierRes = supplierRes;
            _keySettings = keySettings.Value;
            _vcn = vcn;
            _supplierConfig = supplierConfig.Value;
        }

        public void GethyperguestRequestOnly_Async(DataRow[] commonCode, List<StarRating> listRating, HotelSearchParameter SearchPara, List<SupplierDetails> hyperguestDetails, List<SupplierSearchConfig> hyperguestSearchConfig, DataTable dtPreferredDataHotel, ref ConcurrentBag<HotelX> AllTask, string XMLCity, string XMLCountry, string XMLNationality, int Hit, DataTable dtROE, string Provider, TokenData tokenData)
        {
            try
            {
                string Ids = string.Empty;
                string xmlbase = "";
                _commonMethod.GetPrefferedHotelCodes(ref Ids, ref xmlbase, ref hyperguestSearchConfig, "hyperguest", dtPreferredDataHotel, SearchPara.IsSearchFor);
                if (hyperguestSearchConfig != null)
                {
                    double CurrencyFactorhyperguest = 1;
                    string Currency = Convert.ToString(hyperguestDetails[0].CurrencyShortName);


                    if (Currency.ToLower() != tokenData.SiteCurrency.ToLower())
                    {
                        CurrencyFactorhyperguest = _commonMethod.GetROEByCurrency(Currency, dtROE);// Convert.ToDouble(dtInrCurrency.Rows[0]["AmtToExch"]);
                    }

                    for (int k = 0; k < hyperguestSearchConfig.Count; k++)
                    {
                        bool DebugMode = (Convert.ToInt32(hyperguestSearchConfig[k].DebugMode) == 0 ? false : true);
                        string UserName = Convert.ToString(hyperguestDetails[0].UserName);
                        string Password = Convert.ToString(hyperguestDetails[0].Password);
                        {
                            int IsCitywisesearch = Convert.ToInt32(hyperguestSearchConfig[k].City);

                            if (Convert.ToInt32(hyperguestSearchConfig[k].CityID) == 0 || Convert.ToInt32(hyperguestSearchConfig[k].CityID) == SearchPara.CityID)
                            {
                                if (commonCode.Length > 0)
                                {
                                    int Noofrecords = Convert.ToInt32(hyperguestSearchConfig[k].Noofrecords);
                                    DataTable[] splittedtables = commonCode.AsEnumerable()
                                                .Select((row, index) => new { row, index })
                                                .GroupBy(x => x.index / Noofrecords)  // integer division, the fractional part is truncated
                                                .Select(g => g.Select(x => x.row).CopyToDataTable())
                                                .ToArray();

                                    if (SearchPara.Filters.IsShowRooms == 0)
                                    {
                                        for (int a = 0; a < splittedtables.Length; a++)
                                        {
                                            var paramValues1 = String.Join(",", splittedtables[a].Select().Select(x => String.Join(",", Convert.ToString(x.ItemArray[0]))));
                                            string IdsForSearch = Convert.ToString(paramValues1);
                                            HotelX Xhotel = new HotelX();
                                            Xhotel.Supplier = Provider;
                                            Xhotel.DebugeMode = DebugMode;
                                            Xhotel.SearchTimeOut = Convert.ToInt32(hyperguestSearchConfig[k].HotelSearchTimeOut);
                                            //if (Hit > 0)
                                            Xhotel.ProcessTimeout = Convert.ToInt32(hyperguestSearchConfig[k].HotelSearchTimeOut2);

                                            Xhotel.URL = Convert.ToString(hyperguestDetails[0].WebSiteURL);
                                            Xhotel.UserId = Convert.ToString(hyperguestDetails[0].UserID);
                                            Xhotel.UserName = UserName;
                                            Xhotel.PassWord = Password;
                                            Xhotel.CurrencyCode = Currency;
                                            Xhotel.CurrencyFactor = CurrencyFactorhyperguest;
                                            Xhotel.XmlCity = XMLCity;
                                            Xhotel.XmlCountry = XMLCountry;
                                            Xhotel.XmlNationality = XMLNationality;
                                            string PaxPassport = XMLNationality;
                                            string checkin = Convert.ToString(SearchPara.CheckInDate);
                                            string checkout = Convert.ToString(SearchPara.CheckOutDate);

                                            checkin = checkin.Substring(6, 4) + "-" + checkin.Substring(3, 2) + "-" + checkin.Substring(0, 2);
                                            checkout = checkout.Substring(6, 4) + "-" + checkout.Substring(3, 2) + "-" + checkout.Substring(0, 2);

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
                                                //objrq.Request = XMLhyperguestRequestOnly_Async(Xhotel.URL, SearchPara, checkin, checkout, IdsForSearch, XMLNationality, Currency, 1);
                                                objrq.Request = XMLhyperguestRequestOnly_Async(SearchPara, Xhotel.URL, Xhotel.XmlNationality, IdsForSearch);
                                                objrq.Response = "";
                                                RQRS.Add(objrq);
                                            }
                                            Xhotel.RQRS = RQRS;
                                            Xhotel.ReturnRooms = 5;
                                            Xhotel.TechName = "Hyperguest";
                                            #endregion
                                            AllTask.Add(Xhotel);
                                        }
                                    }
                                    else  // && count == 1)
                                    {
                                        for (int a = 0; a < splittedtables.Length; a++)
                                        {
                                            var paramValues1 = String.Join(",", splittedtables[a].Select().Select(x => String.Join(",", Convert.ToString(x.ItemArray[0]))));
                                            string IdsForSearch = Convert.ToString(paramValues1);
                                            HotelX Xhotel = new HotelX();
                                            Xhotel.Supplier = Provider;
                                            Xhotel.DebugeMode = DebugMode;
                                            Xhotel.SearchTimeOut = Convert.ToInt32(hyperguestSearchConfig[k].HotelSearchTimeOut);
                                            //if (Hit > 0)
                                            Xhotel.ProcessTimeout = Convert.ToInt32(hyperguestSearchConfig[k].HotelSearchTimeOut2);

                                            Xhotel.URL = Convert.ToString(hyperguestDetails[0].WebSiteURL);
                                            Xhotel.UserId = Convert.ToString(hyperguestDetails[0].UserID);
                                            Xhotel.UserName = UserName;
                                            Xhotel.PassWord = Password;
                                            Xhotel.CurrencyCode = Currency;
                                            Xhotel.CurrencyFactor = CurrencyFactorhyperguest;
                                            Xhotel.XmlCity = XMLCity;
                                            Xhotel.XmlCountry = "";
                                            Xhotel.XmlNationality = XMLNationality;
                                            string PaxPassport = XMLNationality;
                                            string checkin = Convert.ToString(SearchPara.CheckInDate);
                                            string checkout = Convert.ToString(SearchPara.CheckOutDate);

                                            checkin = checkin.Substring(6, 4) + "-" + checkin.Substring(3, 2) + "-" + checkin.Substring(0, 2);
                                            checkout = checkout.Substring(6, 4) + "-" + checkout.Substring(3, 2) + "-" + checkout.Substring(0, 2);

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
                                                // objrq.Request = XMLhyperguestRequestOnly_Async(SearchPara, Xhotel.XmlCity, XMLCountry, Xhotel.URL, Xhotel.UserName, Xhotel.PassWord, Xhotel.XmlNationality, Timeout, hyperguestCurrenc, Ids);
                                                objrq.Request = XMLhyperguestRequestOnly_Async(SearchPara, Xhotel.URL, Xhotel.XmlNationality, IdsForSearch);
                                                objrq.Response = "";
                                                RQRS.Add(objrq);
                                            }
                                            Xhotel.RQRS = RQRS;
                                            Xhotel.ReturnRooms = 5;
                                            Xhotel.TechName = "Hyperguest";
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
            catch (Exception ex)
            {
                _exception.Error(ex);
            }
        }

        public void GethyperguestRequestOnly_Async_new(DataRow[] commonCode, List<StarRating> listRating, HotelSearchParameter SearchPara, List<SupplierDetails> lsthyperguestDetails, List<SupplierSearchConfig> lsthyperguestSearchConfig, DataTable dtPreferredDataHotel, ref ConcurrentBag<HotelX> AllTask, int Hit, DataTable dtROE, TokenData tokenData, DataSet dtXMLCity)
        {
            try
            {
                List<StarRating> lstRating = new List<StarRating>();
                lstRating = _commonMethod.GetStarRatingStringList_xtg(SearchPara);

                List<HotelCodeByCity> DtHotelDAta = HtmlExtensions.ToList<HotelCodeByCity>(commonCode.CopyToDataTable());
                int ratingcount = lstRating.Count;
                int hotelcount = DtHotelDAta.Count;

                foreach (SupplierDetails hyperguestDetails in lsthyperguestDetails)
                {
                    try
                    {
                        int SupplierID = hyperguestDetails.SupplierID;
                        string Supplier = hyperguestDetails.SupplierName;

                        if (DtHotelDAta.Count > 0)
                        {
                            if (lsthyperguestSearchConfig.Count > 0)
                            {
                                List<SupplierSearchConfig> hyperguestSearchConfig = lsthyperguestSearchConfig
                                   .Where(x => x != null && x.SupplierID == SupplierID)
                                   .ToList();

                                if (hyperguestSearchConfig != null && hyperguestSearchConfig.Count > 0)
                                {
                                    if (SearchPara.PastDate == false) //Added on 28-AUG-2024 as per mustafa sir
                                    {
                                        DataForSearch dataForSearch = _commonMethod.GetXmlDataForSearchTechwise(SearchPara.Nationality, dtXMLCity, Supplier, hyperguestDetails.TechName);

                                        if (dataForSearch.XmlNationality != "")
                                        {
                                            List<HotelCodeByCity> filter = new List<HotelCodeByCity>();
                                            filter = _commonMethod.FilterWithOutCity_Code(ratingcount, hotelcount, DtHotelDAta, lstRating, Supplier);

                                            if (filter.Count > 0)
                                            {
                                                string Ids = string.Empty;
                                                string xmlbase = "";
                                                _commonMethod.GetPrefferedHotelCodes(ref Ids, ref xmlbase, ref hyperguestSearchConfig, Supplier, dtPreferredDataHotel, SearchPara.IsSearchFor);

                                                double CurrencyFactorhyperguest = 1;
                                                string Currency = Convert.ToString(hyperguestDetails.CurrencyShortName);


                                                if (Currency.ToLower() != tokenData.SiteCurrency.ToLower())
                                                {
                                                    CurrencyFactorhyperguest = _commonMethod.GetROEByCurrency(Currency, dtROE);// Convert.ToDouble(dtInrCurrency.Rows[0]["AmtToExch"]);
                                                }

                                                for (int k = 0; k < hyperguestSearchConfig.Count; k++)
                                                {
                                                    bool DebugMode = (Convert.ToInt32(hyperguestSearchConfig[k].DebugMode) == 0 ? false : true);
                                                    string UserName = Convert.ToString(hyperguestDetails.UserName);
                                                    string Password = Convert.ToString(hyperguestDetails.Password);

                                                    int IsCitywisesearch = Convert.ToInt32(hyperguestSearchConfig[k].City);

                                                    if (Convert.ToInt32(hyperguestSearchConfig[k].CityID) == 0 || Convert.ToInt32(hyperguestSearchConfig[k].CityID) == SearchPara.CityID)
                                                    {
                                                        if (commonCode.Length > 0)
                                                        {
                                                            int NoOfRecord = Convert.ToInt32(hyperguestSearchConfig[k].Noofrecords);
                                                            double count = Convert.ToDouble(filter.Count);
                                                            int NoOfDiv = Convert.ToInt32(Math.Ceiling(count / Convert.ToDouble(NoOfRecord)));
                                                            List<List<XMLID_Search>> lstxml = _commonMethod.GetXMLIDString(NoOfDiv, NoOfRecord, count, filter);


                                                            if (SearchPara.Filters.IsShowRooms == 0)
                                                            {
                                                                for (int a = 0; a < NoOfDiv; a++)
                                                                {
                                                                    string IdsForSearch = Convert.ToString(_commonMethod.GetHotelCodeByCOmma(lstxml[a]));

                                                                    HotelX Xhotel = new HotelX();
                                                                    Xhotel.Supplier = Supplier;
                                                                    Xhotel.DebugeMode = DebugMode;
                                                                    Xhotel.SearchTimeOut = Convert.ToInt32(hyperguestSearchConfig[k].HotelSearchTimeOut);
                                                                    //if (Hit > 0)
                                                                    Xhotel.ProcessTimeout = Convert.ToInt32(hyperguestSearchConfig[k].HotelSearchTimeOut2);

                                                                    //Xhotel.URL = Convert.ToString(hyperguestDetails.WebSiteURL);
                                                                    if (_keySettings.SupplierProxy == "0")
                                                                        Xhotel.URL = Convert.ToString(hyperguestDetails.WebSiteURL);
                                                                    else
                                                                        Xhotel.URL = Convert.ToString(hyperguestDetails.WebSiteURLforAPI);


                                                                    Xhotel.UserId = Convert.ToString(hyperguestDetails.UserID);
                                                                    Xhotel.UserName = UserName;
                                                                    Xhotel.PassWord = Password;
                                                                    Xhotel.CurrencyCode = Currency;
                                                                    Xhotel.CurrencyFactor = CurrencyFactorhyperguest;
                                                                    Xhotel.XmlCity = dataForSearch.xmlCity;
                                                                    Xhotel.XmlCountry = dataForSearch.xmlCountry;
                                                                    Xhotel.XmlNationality = dataForSearch.XmlNationality;
                                                                    string PaxPassport = dataForSearch.XmlNationality;
                                                                    string checkin = Convert.ToString(SearchPara.CheckInDate);
                                                                    string checkout = Convert.ToString(SearchPara.CheckOutDate);

                                                                    checkin = checkin.Substring(6, 4) + "-" + checkin.Substring(3, 2) + "-" + checkin.Substring(0, 2);
                                                                    checkout = checkout.Substring(6, 4) + "-" + checkout.Substring(3, 2) + "-" + checkout.Substring(0, 2);

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
                                                                        //objrq.Request = XMLhyperguestRequestOnly_Async(Xhotel.URL, SearchPara, checkin, checkout, IdsForSearch, XMLNationality, Currency, 1);
                                                                        objrq.Request = XMLhyperguestRequestOnly_Async(SearchPara, Xhotel.URL, Xhotel.XmlNationality, IdsForSearch);
                                                                        objrq.Response = "";
                                                                        RQRS.Add(objrq);
                                                                    }
                                                                    Xhotel.RQRS = RQRS;
                                                                    Xhotel.ReturnRooms = Convert.ToInt16(hyperguestDetails.ReturnRooms);
                                                                    Xhotel.TechName = Convert.ToString(hyperguestDetails.TechName);
                                                                    #endregion
                                                                    AllTask.Add(Xhotel);
                                                                }
                                                            }
                                                            else  // && count == 1)
                                                            {
                                                                for (int a = 0; a < NoOfDiv; a++)
                                                                {
                                                                    string IdsForSearch = Convert.ToString(_commonMethod.GetHotelCodeByCOmma(lstxml[a]));

                                                                    HotelX Xhotel = new HotelX();
                                                                    Xhotel.Supplier = Supplier;
                                                                    Xhotel.DebugeMode = DebugMode;
                                                                    Xhotel.SearchTimeOut = Convert.ToInt32(hyperguestSearchConfig[k].HotelSearchTimeOut);
                                                                    //if (Hit > 0)
                                                                    Xhotel.ProcessTimeout = Convert.ToInt32(hyperguestSearchConfig[k].HotelSearchTimeOut2);

                                                                    if (_keySettings.SupplierProxy == "0")
                                                                        Xhotel.URL = Convert.ToString(hyperguestDetails.WebSiteURL);
                                                                    else
                                                                        Xhotel.URL = Convert.ToString(hyperguestDetails.WebSiteURLforAPI);

                                                                    Xhotel.UserId = Convert.ToString(hyperguestDetails.UserID);
                                                                    Xhotel.UserName = UserName;
                                                                    Xhotel.PassWord = Password;
                                                                    Xhotel.CurrencyCode = Currency;
                                                                    Xhotel.CurrencyFactor = CurrencyFactorhyperguest;
                                                                    Xhotel.XmlCity = dataForSearch.xmlCity;
                                                                    Xhotel.XmlCountry = "";
                                                                    Xhotel.XmlNationality = dataForSearch.XmlNationality;
                                                                    string PaxPassport = dataForSearch.XmlNationality;
                                                                    string checkin = Convert.ToString(SearchPara.CheckInDate);
                                                                    string checkout = Convert.ToString(SearchPara.CheckOutDate);

                                                                    checkin = checkin.Substring(6, 4) + "-" + checkin.Substring(3, 2) + "-" + checkin.Substring(0, 2);
                                                                    checkout = checkout.Substring(6, 4) + "-" + checkout.Substring(3, 2) + "-" + checkout.Substring(0, 2);

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
                                                                        // objrq.Request = XMLhyperguestRequestOnly_Async(SearchPara, Xhotel.XmlCity, XMLCountry, Xhotel.URL, Xhotel.UserName, Xhotel.PassWord, Xhotel.XmlNationality, Timeout, hyperguestCurrenc, Ids);
                                                                        objrq.Request = XMLhyperguestRequestOnly_Async(SearchPara, Xhotel.URL, Xhotel.XmlNationality, IdsForSearch);
                                                                        objrq.Response = "";
                                                                        RQRS.Add(objrq);
                                                                    }
                                                                    Xhotel.RQRS = RQRS;
                                                                    Xhotel.ReturnRooms = Convert.ToInt16(hyperguestDetails.ReturnRooms);
                                                                    Xhotel.TechName = Convert.ToString(hyperguestDetails.TechName);
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
                    catch (Exception ex)
                    {
                        _exception.Error(ex);
                    }
                }



            }
            catch (Exception ex)
            {
                _exception.Error(ex);
            }
        }

        public string XMLhyperguestRequestOnly_Async(HotelSearchParameter SearchPara, string hyperguest_URL, string Nationality, string hotelId)

        {
            try
            {

                string checkIn = System.Convert.ToString(SearchPara.CheckInDate);
                string checkout = Convert.ToString(SearchPara.CheckOutDate);
                int nights = SearchPara.NoofNights;
                checkIn = checkIn.Substring(6, 4) + "-" + checkIn.Substring(3, 2) + "-" + checkIn.Substring(0, 2);
                checkout = checkout.Substring(6, 4) + "-" + checkout.Substring(3, 2) + "-" + checkout.Substring(0, 2);
                try
                {

                    #region Adults Child Count

                    StringBuilder guests = new StringBuilder();

                    guests.Append("&guests");
                    string guestcount = "";
                    for (int ser = 0; ser < SearchPara.Rooms.Count; ser++)
                    {
                        string guest = "";
                        int NoofAdults = SearchPara.Rooms[ser].NoofAdults;
                        int NoofChild = SearchPara.Rooms[ser].NoofChild;
                        string childage = "";

                        if (NoofChild > 0)
                        {
                            switch (NoofChild)
                            {
                                case 1:
                                    childage = "-" + SearchPara.Rooms[ser].Child1Age;
                                    break;
                                case 2:
                                    childage = "-" + SearchPara.Rooms[ser].Child1Age + "," + SearchPara.Rooms[ser].Child2Age;
                                    break;
                            }
                        }
                        if (ser == 0)
                        {
                            guest = NoofAdults + childage;
                        }
                        else
                        {
                            guest = "." + NoofAdults + childage;
                        }
                        guestcount = guestcount + guest;
                    }
                    #endregion

                    #region Req 
                    StringBuilder SearchRQ = new StringBuilder();

                    SearchRQ.Append("?checkIn=" + checkIn + "");
                    SearchRQ.Append("&nights=" + nights + "");
                    SearchRQ.Append("&guests=" + guestcount + "");
                    SearchRQ.Append("&hotelIds=" + hotelId + "");
                    SearchRQ.Append("&customerNationality=" + Nationality + "");
                    string hyperguestRoomReq = hyperguest_URL + SearchRQ.ToString();
                    return hyperguestRoomReq;

                    #endregion
                }
                catch (Exception ex)
                {
                    _exception.Error(ex);
                    return "";
                }


            }
            catch (Exception e)
            {
                _exception.Error(e);
            }
            return "";
        }
        public async Task<List<Options>> hyperguestHotelSearchWithRooms_within(HotelX Item, CommonSearch commonSearch, string ResponseData, string CityCode, HotelSearchParameter SearchParam, double CurrencyFactorHyperGuest, string Supplier, DataSet dsAgentDetails, DataTable dtMarkupDetails, string CurrencyCode, DataTable dtROE, string XmlNationality, Int32 processTimeout, string ResProccessTimerLogActive, string ResProccessTimeValueforSaveLog, string StoreSupplierResErrorLog)
        {
            System.Diagnostics.Stopwatch timerroomx = new System.Diagnostics.Stopwatch();
            timerroomx.Start();
            List<Options> options = new List<Options>();
            DataTable DtHotel = _commonMethod.GenerateDataTableNewV2();
            try
            {
                if (ResponseData != string.Empty)
                {
                    int totalAdult = 0;
                    int totalChild = 0;
                    for (int b = 0; b < SearchParam.Rooms.Count; b++)
                    {
                        totalAdult = Convert.ToInt32(SearchParam.Rooms[b].NoofAdults) + totalAdult;
                        totalChild = Convert.ToInt32(SearchParam.Rooms[b].NoofChild) + totalChild;
                    }
                    var result = SearchParam.Rooms.GroupBy(x => new { x.NoofAdults, x.NoofChild, x.Child1Age, x.Child2Age }).Select(n => new { NoofAdults = n.Key.NoofAdults, NoofChild = n.Key.NoofChild, ChildAge = n.ToList(), Count = n.Count() });

                    HyperguestClass.hyperguest.RoomList.Root hyperguestResponse = null;
                    try
                    {
                        hyperguestResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<HyperguestClass.hyperguest.RoomList.Root>(ResponseData);
                        //hyperguestResponse = JsonSerializer.Deserialize<HyperguestClass.hyperguest.RoomList.Root>(ResponseData);
                    }
                    catch (Exception ex)
                    {
                        await _exception.Error(ex);
                    }
                    if (hyperguestResponse == null)
                    {
                        Item.ResponseType = "NoCount";
                        return options;
                    }
                    if (hyperguestResponse.results.Count < 1)
                    {
                        Item.ResponseType = "NoCount";
                        return options;
                    }
                    for (int h = 0; h < hyperguestResponse.results.Count; h++)
                    {
                        if (hyperguestResponse.results[h].rooms.Count == 0)
                            continue;  //newchange
                        #region "When not Need Room"  
                        if (SearchParam.Filters.IsShowRooms == 0)
                        {
                            #region "Hotel Code Block "
                            {
                                if (hyperguestResponse.results[h].rooms.Count() == 0)
                                {
                                    continue;
                                }
                                //double CurrencyFactor = 0;
                                double CurrencyFactor = 1;
                                string HotelID = Convert.ToString(hyperguestResponse.results[h].hotelId);
                                string currency = hyperguestResponse.results[h].rooms[0].ratePlans[0].inclusivePrice.currency;
                                //if (currency.ToLower() != "usd")
                                {
                                    //DataTable dtInrCurrency = Common.GetCountryCurrency(currency);
                                    CurrencyFactor = _commonMethod.GetROEByCurrency(currency, dtROE);   // newnewchange
                                                                                                        // CurrencyFactor = Convert.ToDouble(dtInrCurrency.Rows[0]["AmtToExch"]);
                                }
                                double NetPrice = Convert.ToDouble(Convert.ToDouble(hyperguestResponse.results[h].rooms[0].ratePlans[0].inclusivePrice.amount * Convert.ToInt32(SearchParam.Rooms.Count)) * CurrencyFactor);

                                #region "Hotel Code Block "
                                DataRow drXmlHotel = DtHotel.NewRow();
                                drXmlHotel["XMLHotelCode"] = HotelID;
                                drXmlHotel["HotelName"] = "";
                                drXmlHotel["SupplierName"] = Supplier;
                                drXmlHotel["OfficeID"] = 0;
                                drXmlHotel["ContractID"] = 0;
                                drXmlHotel["Rating"] = 0;
                                drXmlHotel["Reviews"] = 0;
                                drXmlHotel["UniqueNo"] = 0;
                                drXmlHotel["IsShowSupplier"] = false;
                                drXmlHotel["BookingStatus"] = "Available";
                                drXmlHotel["XMLCity"] = "";
                                drXmlHotel["SupplierCurrency"] = currency;
                                drXmlHotel["FinalSellingPrice"] = NetPrice;
                                drXmlHotel["SellingPrice"] = NetPrice;
                                drXmlHotel["OfferText"] = "";
                                DtHotel.Rows.Add(drXmlHotel);
                                Double FinalTotal = NetPrice;
                                #endregion
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
                                        decimal LowestPrice = Convert.ToDecimal(MinPrice).CalculatePrice(commonSearch.tokenData, commonSearch.advancedOptions, currency);
                                        Options ops = (await _supplierRes.GetCommonRoomForSuppliers(GiataId, commonSearch.tokenData, commonSearch.advancedOptions, Convert.ToDouble(LowestPrice), drXmlHotel, dt_HotelRoom, dt_Cancellation, dt_RateBreackUp, SearchParam));
                                        if (ops != null)
                                            options.Add(ops);
                                    }
                                    #endregion
                                }
                                #endregion
                            }
                            #endregion
                        }
                        #region"When Room need"
                        else
                        {
                            string xmlhotelcode = hyperguestResponse.results[h].hotelId;
                            bool willAddOrNot = true;
                            Int64 RaynaID = 0;
                            Int64 GiataId = 0;
                            int RatingHotel = 0;
                            Int64 OwnsystemHotelId = 0;
                            double MinPrice = 1;
                            double AgentPercentage = 0;
                            double AgentMarkupAmount = 0;
                            Boolean blnIsShowSupplier = false;
                            Boolean isPerce = true;
                            string currency = hyperguestResponse.results[h].rooms[0].ratePlans[0].inclusivePrice.currency;
                            //if (currency.ToLower() != "usd")
                            {
                                CurrencyFactorHyperGuest = _commonMethod.GetROEByCurrency(currency, dtROE);
                            }

                            Item.ResponseType = "Success";
                            string HotelID = xmlhotelcode;
                            double FinalMinPrice = 0;
                            DataRow drXmlHotel = DtHotel.NewRow();
                            drXmlHotel["XMLHotelCode"] = HotelID;
                            drXmlHotel["HotelName"] = "";
                            drXmlHotel["SupplierName"] = Supplier;
                            drXmlHotel["OfficeID"] = 0;
                            drXmlHotel["ContractID"] = 0;
                            drXmlHotel["Rating"] = 0;
                            drXmlHotel["Reviews"] = 0;
                            drXmlHotel["UniqueNo"] = 0;
                            drXmlHotel["IsShowSupplier"] = false;
                            drXmlHotel["BookingStatus"] = "Available";
                            drXmlHotel["XMLCity"] = "";
                            drXmlHotel["SupplierCurrency"] = currency;
                            drXmlHotel["FinalSellingPrice"] = 1;
                            drXmlHotel["SellingPrice"] = 1;
                            drXmlHotel["OfferText"] = "";
                            DtHotel.Rows.Add(drXmlHotel);

                            #region "Room code Block"
                            int Rating = 0;
                            DataTable dthyperguestHotelRoom = _commonMethod.GenerateRoomtypeDataTableNewV2();
                            DataTable dthyperguestCancellation = _commonMethod.GenerateCancellationDataTableNewV2();
                            DataTable dthyperguestRoomRateBreakup = _commonMethod.GenerateoomRateBreackUpDataTableNewV2();
                            _markup.GetHotelDataCommon(ref MinPrice, ref willAddOrNot, ref OwnsystemHotelId, ref RaynaID, ref GiataId, ref Rating, ref drXmlHotel, commonSearch.AgentId, commonSearch.TariffID, commonSearch.dsAgentDetails, commonSearch.supplierGiataMaster, SearchParam, commonSearch.dtCountryCityMarkup, commonSearch.B2CMarkup, commonSearch.IsParentId, commonSearch.SubAgentMarkup, commonSearch.SearchByHotel, commonSearch.HotelIDS);
                            if (willAddOrNot)
                            {
                                _markup.getAgetMarkupSetting(Supplier, GiataId.ToString(), commonSearch.dtSuplierMarkup, commonSearch.dtMarkupDetails, ref AgentPercentage, ref AgentMarkupAmount, ref blnIsShowSupplier, ref isPerce, commonSearch.AgentId, RatingHotel, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), Convert.ToString(SearchParam.CheckInDate));
                                string Hotelremark = "";
                                for (int i = 0; i < hyperguestResponse.results[h].remarks.Count; i++)
                                {
                                    string remark1 = Convert.ToString(hyperguestResponse.results[h].remarks[i]);
                                    if (i == 0)
                                        Hotelremark = remark1;
                                    else
                                        Hotelremark = Hotelremark + "<br>" + remark1;
                                }
                                _markup.GetHotelDataCommon(ref MinPrice, ref willAddOrNot, ref OwnsystemHotelId, ref RaynaID, ref GiataId, ref RatingHotel, ref drXmlHotel, commonSearch.AgentId, commonSearch.TariffID, commonSearch.dsAgentDetails, commonSearch.supplierGiataMaster, SearchParam, commonSearch.dtCountryCityMarkup, commonSearch.B2CMarkup, commonSearch.IsParentId, commonSearch.SubAgentMarkup, commonSearch.SearchByHotel, commonSearch.HotelIDS);
                                for (int ro = 0; ro < hyperguestResponse.results[h].rooms.Count; ro++)
                                {
                                    foreach (var item in result)
                                    {
                                        var aa = item.NoofAdults;
                                        var cc = item.NoofChild;
                                        var RCount = item.Count;

                                        int TotalA = Convert.ToInt32(hyperguestResponse.results[h].rooms[ro].pax.adults);
                                        int TotalC = Convert.ToInt32(hyperguestResponse.results[h].rooms[ro].pax.children.Count);
                                        string Roomnumber = "";
                                        for (int kk = 0; kk < SearchParam.Rooms.Count; kk++)
                                        {
                                            totalAdult = Convert.ToInt32(SearchParam.Rooms[kk].NoofAdults);
                                            totalChild = Convert.ToInt32(SearchParam.Rooms[kk].NoofChild);
                                            if (totalAdult == aa && cc == totalChild)
                                            {
                                                int RNo = Convert.ToInt32(SearchParam.Rooms[kk].RoomNo);
                                                if (Roomnumber == "")
                                                {
                                                    Roomnumber = Convert.ToString(RNo);
                                                }
                                                else
                                                {
                                                    Roomnumber = Roomnumber + "$" + RNo;
                                                }
                                            }
                                        }
                                        string[] Rnumber = Roomnumber.Split('$');
                                        for (int r = 0; r < Rnumber.Length; r++)
                                        {
                                            string RoomNo = Convert.ToString(Rnumber[r]);
                                            string RoomName = hyperguestResponse.results[h].rooms[ro].roomName;
                                            string RoomTypeCord = hyperguestResponse.results[h].rooms[ro].roomTypeCode;
                                            int NumberOfAvailableRooms = hyperguestResponse.results[h].rooms[ro].numberOfAvailableRooms;
                                            int RoomId = hyperguestResponse.results[h].rooms[ro].roomId;

                                            totalAdult = 0;
                                            totalChild = 0;
                                            int Rno = Convert.ToInt16(Rnumber[r]) - 1;
                                            try
                                            {
                                                totalAdult = Convert.ToInt32(SearchParam.Rooms[Rno].NoofAdults) + totalAdult;
                                                totalChild = Convert.ToInt32(SearchParam.Rooms[Rno].NoofChild) + totalChild;
                                            }
                                            catch (Exception)
                                            {
                                                totalAdult = Convert.ToInt32(SearchParam.Rooms[r].NoofAdults) + totalAdult;
                                                totalChild = Convert.ToInt32(SearchParam.Rooms[r].NoofChild) + totalChild;
                                            }

                                            for (int roo = 0; roo < hyperguestResponse.results[h].rooms[ro].ratePlans.Count; roo++)
                                            {
                                                string RemarkRoom = Hotelremark;
                                                string remark = "";
                                                string RatePlanCode = hyperguestResponse.results[h].rooms[ro].ratePlans[roo].ratePlanCode;
                                                string Board = hyperguestResponse.results[h].rooms[ro].ratePlans[roo].board;
                                                string Payment = hyperguestResponse.results[h].rooms[ro].ratePlans[roo].payment;
                                                string pricetype = hyperguestResponse.results[h].rooms[ro].ratePlans[roo].priceType;
                                                string Charge = hyperguestResponse.results[h].rooms[ro].ratePlans[roo].charge;
                                                double price = Convert.ToDouble(hyperguestResponse.results[h].rooms[ro].ratePlans[roo].inclusivePrice.amount);
                                                double RSprice = Convert.ToDouble(hyperguestResponse.results[h].rooms[ro].ratePlans[roo].inclusivePrice.amount);
                                                double exclusiveprice = hyperguestResponse.results[h].rooms[ro].ratePlans[roo].exclusivePrice.amount;
                                                currency = hyperguestResponse.results[h].rooms[ro].ratePlans[roo].inclusivePrice.currency;
                                                string remarkdisply = "";
                                                try
                                                {
                                                    for (int f = 0; f < hyperguestResponse.results[h].rooms[ro].ratePlans[roo].fees.mandatoryTaxes.Count; f++)
                                                    {
                                                        string ab = hyperguestResponse.results[h].rooms[ro].ratePlans[roo].fees.mandatoryTaxes[f].relation;
                                                        if (ab.ToLower() == "display")
                                                        {
                                                            string madentorydescription = hyperguestResponse.results[h].rooms[ro].ratePlans[roo].fees.mandatoryTaxes[f].description;
                                                            string Mcurrency = hyperguestResponse.results[h].rooms[ro].ratePlans[roo].fees.mandatoryTaxes[f].currency;
                                                            double MFee = hyperguestResponse.results[h].rooms[ro].ratePlans[roo].fees.mandatoryTaxes[f].amount;
                                                            remark = RemarkRoom + "<br>" + remarkdisply + "<br>" + madentorydescription + ":" + MFee + " " + Mcurrency;
                                                        }
                                                    }
                                                }
                                                catch (Exception e)
                                                {
                                                    await _exception.Error(e);
                                                }
                                                isPerce = true;
                                                //if (currency.ToLower() != "usd")
                                                {
                                                    CurrencyFactorHyperGuest = _commonMethod.GetROEByCurrency(currency, dtROE);
                                                }
                                                int MasterSupplierID = 0;
                                                double SubAgentMarkup = 0;
                                                remark = RemarkRoom + "<br>" + remark;
                                                price = price * CurrencyFactorHyperGuest;
                                                double CancellationPrice = price;// * CurrencyFactorhyperguest;

                                                string boardType = hyperguestResponse.results[h].rooms[ro].ratePlans[roo].board.Replace("'", "");
                                                string uniqid = Guid.NewGuid().ToString();

                                                string RoomTypeId = RoomTypeCord + "###" + RatePlanCode + "###" + xmlhotelcode + "###" + RSprice + "###" + currency + "###" + XmlNationality + "###" + Guid.NewGuid().ToString().Substring(0, 4) + "###" + CurrencyFactorHyperGuest;
                                                double TotalAmount = price;

                                                #region Ratebreakup
                                                for (int n = 0; n < SearchParam.NoofNights; n++)
                                                {
                                                    string CheckInDate = Convert.ToString(SearchParam.CheckInDate);
                                                    string CheckOutDate = Convert.ToString(SearchParam.CheckOutDate);
                                                    DateTime dtCheckInDate = DateTime.ParseExact(CheckInDate, "dd/MM/yyyy", null);
                                                    DateTime dtCheckOutDate = DateTime.ParseExact(CheckOutDate, "dd/MM/yyyy", null);
                                                    TimeSpan t = dtCheckOutDate - dtCheckInDate;
                                                    double ratePerNight = TotalAmount;

                                                    #region Vat
                                                    double VatOnPerNight = 0;
                                                    ratePerNight = _markup.ProcessHotelRateBreakupPrice_WithMarkup(1, SearchParam.CityID, SearchParam.CountryCode, ratePerNight, commonSearch.B2CMarkup, commonSearch.IsParentId, isPerce, Convert.ToDouble(commonSearch.AgentPercent), AgentMarkupAmount, SubAgentMarkup, ref VatOnPerNight);
                                                    #endregion

                                                    ratePerNight = ratePerNight / SearchParam.NoofNights;
                                                    DataRow drRateBreackUp = dthyperguestRoomRateBreakup.NewRow();
                                                    drRateBreackUp["SupplierName"] = Supplier;
                                                    drRateBreackUp["OwnsystemHotelId"] = OwnsystemHotelId;
                                                    drRateBreackUp["ContractID"] = 0;
                                                    drRateBreackUp["RoomNo"] = Convert.ToInt32(RoomNo);
                                                    drRateBreackUp["RoomTypeId"] = RoomTypeId;
                                                    drRateBreackUp["MealId"] = Board;
                                                    drRateBreackUp["Meal"] = Board;
                                                    string rateWeekColumn = Convert.ToString(dtCheckInDate.Date.AddDays(n).DayOfWeek).Substring(0, 3);
                                                    drRateBreackUp["DateOfWeek"] = rateWeekColumn;
                                                    DateTime rateDate = dtCheckInDate.Date.AddDays(n);
                                                    drRateBreackUp["Date"] = rateDate;
                                                    drRateBreackUp["Rate"] = ratePerNight;
                                                    drRateBreackUp["OrderNo"] = 1;
                                                    drRateBreackUp["RoomTypeName"] = RoomName;
                                                    drRateBreackUp["DateOfWeek"] = rateWeekColumn;
                                                    drRateBreackUp["Date"] = rateDate;
                                                    drRateBreackUp["BookingStatus"] = "Available";
                                                    drRateBreackUp["BoardTypeID"] = "";
                                                    drRateBreackUp["VatdiffAmount"] = 0;
                                                    drRateBreackUp["RateType"] = RoomName;
                                                    drRateBreackUp["SupplierCurrency"] = currency;
                                                    drRateBreackUp["BuyingPrice"] = ratePerNight / CurrencyFactorHyperGuest;
                                                    dthyperguestRoomRateBreakup.Rows.Add(drRateBreackUp);

                                                }
                                                #endregion
                                                #region Rooms
                                                DataRow drRoom = dthyperguestHotelRoom.NewRow();
                                                drRoom["OwnsystemHotelId"] = OwnsystemHotelId;
                                                drRoom["RoomTypeCode"] = RoomTypeCord;
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

                                                string EssentialInformation = remark;
                                                string Enn = "";

                                                drRoom["EssentialInformation"] = EssentialInformation;
                                                drRoom["Characteristic"] = "";
                                                bool IsCombine = false;
                                                bool IsDynamicPolicy = true;
                                                #region Vat
                                                double itemprice = price;
                                                _markup.ProcessHotelRoom_WithMarkup(CurrencyFactorHyperGuest, currency, IsDynamicPolicy, IsCombine, SearchParam.NoofNights * SearchParam.Rooms.Count, SearchParam.CityID, SearchParam.CountryCode, SearchParam.CheckInDate, SearchParam.CheckOutDate, ref price, ref drRoom, isPerce, AgentPercentage, AgentMarkupAmount, SubAgentMarkup, commonSearch.B2CMarkup, commonSearch.IsParentId);
                                                #endregion
                                                drRoom["SupplierName"] = Supplier;
                                                drRoom["RoomTypeName"] = RoomName;
                                                drRoom["RoomTypeId"] = RoomTypeId;
                                                dthyperguestHotelRoom.Rows.Add(drRoom);
                                                #endregion

                                                //18-01-2022aaa

                                                #region  "Cancellationpolicy"
                                                int dayys = 365;


                                                if (hyperguestResponse.results[h].rooms[ro].ratePlans[roo].cancellationPolicies != null)
                                                {
                                                    bool Istrue = true;
                                                    //for (int c = 0; c < hyperguestResponse.results[h].rooms[ro].ratePlans[roo].cancellationPolicies.Count(); c++)
                                                    for (int c = 0; c < hyperguestResponse.results[h].rooms[ro].ratePlans[roo].cancellationPolicies.Count(); c++)
                                                    {
                                                        double CancelletionPrice = Convert.ToDouble(hyperguestResponse.results[h].rooms[ro].ratePlans[roo].cancellationPolicies[c].amount);
                                                        string PenaltyType = (hyperguestResponse.results[h].rooms[ro].ratePlans[roo].cancellationPolicies[c].penaltyType);
                                                        int CancelletiondaysBefore = Convert.ToInt32(hyperguestResponse.results[h].rooms[ro].ratePlans[roo].cancellationPolicies[c].daysBefore);
                                                        if (c == 0)
                                                        {
                                                            dayys = CancelletiondaysBefore;
                                                        }
                                                        else
                                                        {
                                                            if (CancelletiondaysBefore > dayys)
                                                            {
                                                                dayys = CancelletiondaysBefore;
                                                            }
                                                        }
                                                    }
                                                    //for (int c = 0; c < 1; c++)
                                                    {

                                                        //    double CancelletionPrice =  Convert.ToDouble(hyperguestResponse.results[h].rooms[ro].ratePlans[roo].cancellationPolicies[c].amount);
                                                        //    string PenaltyType = (hyperguestResponse.results[h].rooms[ro].ratePlans[roo].cancellationPolicies[c].penaltyType);
                                                        //    int CancelletiondaysBefore = Convert.ToInt32(hyperguestResponse.results[h].rooms[ro].ratePlans[roo].cancellationPolicies[c].daysBefore);

                                                        //DateTime CancellationDate = DateTime.ParseExact(objSearPera.CheckInDate, "dd/MM/yyyy", CultureInfo.InvariantCulture).Date;
                                                        DateTime CancellationDate = DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", CultureInfo.InvariantCulture).Date;

                                                        string ownsystemid = Convert.ToString(OwnsystemHotelId);
                                                        string roomno = RoomNo;
                                                        double CompanyBuy = price;// CancellationPrice;
                                                        DateTime curDate = _commonMethod.GetTimeZoneDate(SearchParam.CurrentDate);
                                                        double Penalty = Convert.ToDouble(CompanyBuy);

                                                        DateTime MainDate = new DateTime();
                                                        DateTime StartDate = MainDate;

                                                        DateTime CancelDate = CancellationDate.Date.AddDays(-dayys);// DateTime.Now.Date;

                                                        //string Remark = "";// Convert.ToString(hyperguestResponse.Tables["HotelDTO"].Rows[0]["Remark"]);
                                                        //remark = "";
                                                        #region Vat
                                                        double VatOnPolicy = 0;
                                                        double MainAgentCharge = Penalty;
                                                        double CompanyChargeAmount = itemprice;
                                                        //Penalty = Common.ProcessHotelCancelationPrice_WithMarkup(ref MainAgentCharge, objSearPera.NoofNights, objSearPera.CityID, objSearPera.CountryId, Penalty, isPerce, B2CMarkup, AgentPercentage, AgentMarkupAmount, IsParentId, SubAgentMarkup, ref VatOnPolicy);
                                                        #endregion
                                                        int CutOffDays = XMLHotelBookingCommon.CutOffDays;
                                                        CutOffDays = _commonMethod.GetCutOffDays(commonSearch.dtSuplierMarkup, Supplier);
                                                        DateTime NowDate = DateTime.Now.Date;
                                                        if (CancelDate.AddDays(CutOffDays).Date > DateTime.Now.Date)
                                                        {
                                                            //StartDate = StartDate.AddDays(CutOffDays).Date;
                                                            CancelDate = CancelDate.AddDays(CutOffDays).Date;
                                                        }
                                                        else
                                                        {
                                                            CancelDate = DateTime.Now.Date;
                                                            //Penalty = Common.ProcessHotelCancelationPrice_WithMarkup(ref MainAgentCharge, objSearPera.NoofNights, objSearPera.CityID, objSearPera.CountryId, CompanyBuy, isPerce, B2CMarkup, AgentPercentage, AgentMarkupAmount, IsParentId, SubAgentMarkup, ref VatOnPolicy);
                                                        }

                                                        //if (CancelletionPrice > 0)
                                                        //{
                                                        //    CancelDate = DateTime.Now.Date;
                                                        //}

                                                        if (NowDate < CancelDate)
                                                        {
                                                            if (Istrue == true)
                                                            {
                                                                double zero = 0;
                                                                dthyperguestCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref zero, dthyperguestCancellation.NewRow(), CurrencyFactorHyperGuest, currency, Supplier, ownsystemid, Convert.ToString(roomno),
                                                                                                                                                  Convert.ToString(RoomTypeId), RoomName, Board, Convert.ToDateTime(curDate), CancelDate.AddDays(-1), 0, 0, "0", VatOnPolicy, uniqid, "NO", "", remark != string.Empty ? remark : "No Essential Information Available", "", ""));
                                                                dthyperguestCancellation.AcceptChanges();
                                                                Istrue = false;
                                                            }

                                                            dthyperguestCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref CompanyChargeAmount, dthyperguestCancellation.NewRow(), CurrencyFactorHyperGuest, currency, Supplier, ownsystemid, Convert.ToString(roomno),
                                                                                           Convert.ToString(RoomTypeId), RoomName, Board, Convert.ToDateTime(CancelDate), DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null), MainAgentCharge, Penalty, "0", VatOnPolicy, uniqid, "NO", "", remark != string.Empty ? remark : "No Essential Information Available", "", ""));
                                                            dthyperguestCancellation.AcceptChanges();
                                                        }
                                                        else
                                                        {
                                                            dthyperguestCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref CompanyChargeAmount, dthyperguestCancellation.NewRow(), CurrencyFactorHyperGuest, currency, Supplier, ownsystemid, Convert.ToString(roomno),
                                                                                           Convert.ToString(RoomTypeId), RoomName, Board, curDate, DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null), MainAgentCharge, Penalty, "0", VatOnPolicy, uniqid, "Yes", "", remark != string.Empty ? remark : "No Essential Information Available"));
                                                            dthyperguestCancellation.AcceptChanges();
                                                        }

                                                        //}
                                                    }

                                                }
                                                #endregion

                                            }
                                        }
                                    }
                                }
                            }
                            Options ops = ((await _supplierRes.GetCommonRoomForSuppliers(GiataId, commonSearch.tokenData, commonSearch.advancedOptions, MinPrice, drXmlHotel, dthyperguestHotelRoom, dthyperguestCancellation, dthyperguestRoomRateBreakup, SearchParam)));
                            if (ops != null)
                                options.Add(ops);
                            // here
                            #endregion
                        }
                        #endregion
                    }
                }
                return options;
            }
            catch (Exception e)
            {
                await _exception.Error(e);
            }
            finally
            {
                #region ResProcessTimer
                try
                {
                    if (SearchParam != null && ResProccessTimerLogActive == "1" && timerroomx.ElapsedMilliseconds > Convert.ToInt32(ResProccessTimeValueforSaveLog))
                    {
                        timerroomx.Stop();
                        SearchParam.SupplierTime += ",|Finish:hyperguest Process Response  " + "|Time :" + timerroomx.Elapsed.ToString();
                        await Task.Run(async () => await _commonMethod.FireAway_SQL_ResPorcessTimeLog("", timerroomx.Elapsed.ToString(), Supplier, SearchParam.SearchKey, 1, SearchParam.AgentID));
                    }
                }
                catch (Exception x)
                {
                    await _exception.Error(x);
                }
                #endregion

                if (Item.ResponseType == "NoCount" && StoreSupplierResErrorLog == "1")
                    await _commonMethod.XMLRequestResponseTrack_Error(Item.RQRS[0].Request, Item.RQRS[0].Response, Item.Supplier, timerroomx.Elapsed.ToString(), "", SearchParam.SearchKey);
            }
            return options;
        }

        public async Task<List<Options>> hyperguestHotelSearchWithRooms_Multiroom_within(HotelX Item, CommonSearch commonSearch, string ResponseData, string CityCode, HotelSearchParameter SearchParam, double CurrencyFactorHyperGuest, string Supplier, DataSet dsAgentDetails, DataTable dtMarkupDetails, string CurrencyCode, DataTable dtROE, string XmlNationality, Int32 processTimeout, string ResProccessTimerLogActive, string ResProccessTimeValueforSaveLog, string StoreSupplierResErrorLog)
        {
            System.Diagnostics.Stopwatch timerroomx = new System.Diagnostics.Stopwatch();
            timerroomx.Start();
            List<Options> options = new List<Options>();
            DataTable DtHotel = _commonMethod.GenerateDataTableNewV2();
            try
            {
                if (ResponseData != string.Empty)
                {
                    int totalAdult = 0;
                    int totalChild = 0;
                    for (int b = 0; b < SearchParam.Rooms.Count; b++)
                    {
                        totalAdult = Convert.ToInt32(SearchParam.Rooms[b].NoofAdults) + totalAdult;
                        totalChild = Convert.ToInt32(SearchParam.Rooms[b].NoofChild) + totalChild;
                    }
                    var result = SearchParam.Rooms.GroupBy(x => new { x.NoofAdults, x.NoofChild, x.Child1Age, x.Child2Age }).Select(n => new { NoofAdults = n.Key.NoofAdults, NoofChild = n.Key.NoofChild, ChildAge = n.ToList(), Count = n.Count() });

                    HyperguestClass.hyperguest.RoomList.Root hyperguestResponse = null;
                    try
                    {
                        hyperguestResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<HyperguestClass.hyperguest.RoomList.Root>(ResponseData);

                    }
                    catch (Exception ex)
                    {
                        await _exception.Error(ex);
                    }
                    if (hyperguestResponse == null)
                    {
                        Item.ResponseType = "NoCount";
                        return options;
                    }
                    if (hyperguestResponse.results.Count < 1)
                    {
                        Item.ResponseType = "NoCount";
                        return options;
                    }
                    for (int h = 0; h < hyperguestResponse.results.Count; h++)
                    {
                        String currency = "";
                        double LowRate = 0;
                        if (hyperguestResponse.results[h].rooms.Count == 0)
                            continue;  //newchange
                        #region "When not Need Room"  
                        if (SearchParam.Filters.IsShowRooms == 0)
                        {
                            Item.ResponseType = "Success";

                            var distinctRoomTypeCodes = hyperguestResponse.results[h]
                              .rooms
                              //.SelectMany(resulta => resulta.rooms)
                              .Select(room => room.roomTypeCode)
                              .Distinct();
                            foreach (var roomTypeCode1 in distinctRoomTypeCodes)
                            {
                                List<HyperguestClass.hyperguest.RoomList.Room> filteredRooms = hyperguestResponse.results[h].rooms
                                            .Where(x => Convert.ToString(x.roomTypeCode) == roomTypeCode1)
                                            .ToList();


                                int Allroomavailable = 0;
                                List<int> roomlist1 = new List<int>();

                                // Reduce nested loop by pre-checking availability
                                if (filteredRooms.Count >= 1)
                                {
                                    int numberOfAvailableRooms = filteredRooms[0].numberOfAvailableRooms;
                                    if (numberOfAvailableRooms >= SearchParam.Rooms.Count)
                                    {
                                        for (int b = 0; b < SearchParam.Rooms.Count; b++)
                                        {
                                            for (int a = 0; a < filteredRooms.Count; a++)
                                            {
                                                // Simplified matching conditions
                                                var room = filteredRooms[a];
                                                if (SearchParam.Rooms[b].NoofAdults == room.pax.adults && SearchParam.Rooms[b].NoofChild == room.pax.children.Count)
                                                {
                                                    if (SearchParam.Rooms[b].NoofChild == 1 && SearchParam.Rooms[b].Child1Age != Convert.ToInt16(room.pax.children[0]))
                                                    {
                                                        continue;
                                                    }
                                                    if (SearchParam.Rooms[b].NoofChild == 2 && (SearchParam.Rooms[b].Child1Age != Convert.ToInt16(room.pax.children[0]) || SearchParam.Rooms[b].Child2Age != Convert.ToInt16(room.pax.children[1])))
                                                    {
                                                        continue;
                                                    }

                                                    if (room.numberOfAvailableRooms != 0)
                                                    {
                                                        Allroomavailable++;
                                                        roomlist1.Add(a);
                                                    }
                                                }
                                            }
                                        }
                                        if (Allroomavailable == SearchParam.Rooms.Count)
                                        {

                                            // Calculate price after checking availability
                                            for (int f = 0; f < filteredRooms[0].ratePlans.Count; f++)
                                            {
                                                string rateplancode = filteredRooms[0].ratePlans[f].ratePlanCode;
                                                string board = filteredRooms[0].ratePlans.FirstOrDefault(rp => rp.ratePlanCode == rateplancode).board;
                                                bool sameMealOptionForAll = filteredRooms.All(room => room.ratePlans.Any(rp => rp.ratePlanCode == rateplancode && rp.board == board));
                                                string rateplanId = Convert.ToString(filteredRooms[0].ratePlans[f].ratePlanId);
                                                if (sameMealOptionForAll)
                                                {
                                                    string RoomTypeCord = filteredRooms[0].roomTypeCode;
                                                    int RoomId = filteredRooms[0].roomId;
                                                    string uniqid = Guid.NewGuid().ToString();
                                                    double Price = 0;
                                                    double CurrencyFactorhyperguest = 1;
                                                    currency = filteredRooms[0].ratePlans[0].inclusivePrice.currency;
                                                    if (currency.ToLower() != commonSearch.tokenData.SiteCurrency.ToLower())
                                                    {

                                                        CurrencyFactorhyperguest = _commonMethod.GetROEByCurrency(currency, dtROE);
                                                    }
                                                    if (roomlist1.Count >= Convert.ToInt32(SearchParam.Rooms.Count))
                                                    {

                                                        for (int x = 0; x < SearchParam.Rooms.Count; x++)
                                                        {
                                                            List<HyperguestClass.hyperguest.RoomList.RatePlan> roomlist = filteredRooms[roomlist1[x]].ratePlans.Where(rp1 => rp1.ratePlanCode == rateplancode).ToList();

                                                            Price = Price + Convert.ToDouble(roomlist[0].inclusivePrice.amount) * CurrencyFactorhyperguest;

                                                        }
                                                        LowRate = (LowRate == 0) ? Price : Math.Min(LowRate, Price);
                                                    }

                                                }
                                            }
                                        }
                                    }
                                }
                            }

                            #region "Hotel Code Block "
                            {
                                Double NetPrice = LowRate;

                                #region "Hotel Code Block "
                                DataRow drXmlHotel = DtHotel.NewRow();
                                drXmlHotel["XMLHotelCode"] = Convert.ToString(hyperguestResponse.results[h].hotelId);
                                drXmlHotel["HotelName"] = "";
                                drXmlHotel["SupplierName"] = Supplier;
                                drXmlHotel["OfficeID"] = 0;
                                drXmlHotel["ContractID"] = 0;
                                drXmlHotel["Rating"] = 0;
                                drXmlHotel["Reviews"] = 0;
                                drXmlHotel["UniqueNo"] = 0;
                                drXmlHotel["IsShowSupplier"] = false;
                                drXmlHotel["BookingStatus"] = "Available";
                                drXmlHotel["XMLCity"] = "";
                                drXmlHotel["SupplierCurrency"] = currency;
                                drXmlHotel["FinalSellingPrice"] = NetPrice;
                                drXmlHotel["SellingPrice"] = NetPrice;
                                drXmlHotel["OfferText"] = "";
                                DtHotel.Rows.Add(drXmlHotel);
                                Double FinalTotal = NetPrice;
                                #endregion
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
                                        decimal LowestPrice = Convert.ToDecimal(MinPrice).CalculatePrice(commonSearch.tokenData, commonSearch.advancedOptions, currency);
                                        Options ops = (await _supplierRes.GetCommonRoomForSuppliers(GiataId, commonSearch.tokenData, commonSearch.advancedOptions, Convert.ToDouble(LowestPrice), drXmlHotel, dt_HotelRoom, dt_Cancellation, dt_RateBreackUp, SearchParam));
                                        if (ops != null)
                                            options.Add(ops);
                                    }
                                    #endregion
                                }
                                #endregion
                            }
                            #endregion
                        }
                        #region"When Room need"
                        else
                        {
                            string xmlhotelcode = hyperguestResponse.results[h].hotelId;
                            bool willAddOrNot = true;
                            Int64 RaynaID = 0;
                            Int64 GiataId = 0;
                            int RatingHotel = 0;
                            Int64 OwnsystemHotelId = 0;
                            double MinPrice = 1;
                            double AgentPercentage = 0;
                            double AgentMarkupAmount = 0;
                            Boolean blnIsShowSupplier = false;
                            Boolean isPerce = true;
                            string HotelID = xmlhotelcode;
                            double FinalMinPrice = 0;
                            DataRow drXmlHotel = DtHotel.NewRow();
                            drXmlHotel["XMLHotelCode"] = HotelID;
                            drXmlHotel["HotelName"] = "";
                            drXmlHotel["SupplierName"] = Supplier;
                            drXmlHotel["OfficeID"] = 0;
                            drXmlHotel["ContractID"] = 0;
                            drXmlHotel["Rating"] = 0;
                            drXmlHotel["Reviews"] = 0;
                            drXmlHotel["UniqueNo"] = 0;
                            drXmlHotel["IsShowSupplier"] = false;
                            drXmlHotel["BookingStatus"] = "Available";
                            drXmlHotel["XMLCity"] = "";
                            drXmlHotel["SupplierCurrency"] = currency;
                            drXmlHotel["FinalSellingPrice"] = 1;
                            drXmlHotel["SellingPrice"] = 1;
                            drXmlHotel["OfferText"] = "";
                            DtHotel.Rows.Add(drXmlHotel);

                            #region "Room code Block"
                            int Rating = 0;
                            DataTable dthyperguestHotelRoom = _commonMethod.GenerateRoomtypeDataTableNewV2();
                            DataTable dthyperguestCancellation = _commonMethod.GenerateCancellationDataTableNewV2();
                            DataTable dthyperguestRoomRateBreakup = _commonMethod.GenerateoomRateBreackUpDataTableNewV2();
                            _markup.GetHotelDataCommon(ref MinPrice, ref willAddOrNot, ref OwnsystemHotelId, ref RaynaID, ref GiataId, ref Rating, ref drXmlHotel, commonSearch.AgentId, commonSearch.TariffID, commonSearch.dsAgentDetails, commonSearch.supplierGiataMaster, SearchParam, commonSearch.dtCountryCityMarkup, commonSearch.B2CMarkup, commonSearch.IsParentId, commonSearch.SubAgentMarkup, commonSearch.SearchByHotel, commonSearch.HotelIDS);
                            int CutOffDays = CacheMaster.CutOffDays;
                            CutOffDays = _commonMethod.GetCutOffDays(commonSearch.dtSuplierMarkup, Supplier);
                            if (willAddOrNot)
                            {
                                _markup.getAgetMarkupSetting(Supplier, GiataId.ToString(), commonSearch.dtSuplierMarkup, commonSearch.dtMarkupDetails, ref AgentPercentage, ref AgentMarkupAmount, ref blnIsShowSupplier, ref isPerce, commonSearch.AgentId, RatingHotel, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), Convert.ToString(SearchParam.CheckInDate));
                                string Hotelremark = "";
                                for (int i = 0; i < hyperguestResponse.results[h].remarks.Count; i++)
                                {
                                    string remark1 = Convert.ToString(hyperguestResponse.results[h].remarks[i]);
                                    if (i == 0)
                                        Hotelremark = remark1;
                                    else
                                        Hotelremark = Hotelremark + "<br>" + remark1;
                                }

                                _markup.GetHotelDataCommon(ref MinPrice, ref willAddOrNot, ref OwnsystemHotelId, ref RaynaID, ref GiataId, ref RatingHotel, ref drXmlHotel, commonSearch.AgentId, commonSearch.TariffID, commonSearch.dsAgentDetails, commonSearch.supplierGiataMaster, SearchParam, commonSearch.dtCountryCityMarkup, commonSearch.B2CMarkup, commonSearch.IsParentId, commonSearch.SubAgentMarkup, commonSearch.SearchByHotel, commonSearch.HotelIDS);

                                var distinctRoomTypeCodes = hyperguestResponse.results[h]
                                       .rooms
                                       //.SelectMany(resulta => resulta.rooms)
                                       .Select(room => room.roomTypeCode)
                                       .Distinct();
                                foreach (var roomTypeCode1 in distinctRoomTypeCodes)
                                {
                                    List<HyperguestClass.hyperguest.RoomList.Room> filteredRooms = hyperguestResponse.results[h].rooms
                                                .Where(x => Convert.ToString(x.roomTypeCode) == roomTypeCode1)
                                                .ToList();

                                    int Allroomavailable = 0;
                                    List<int> roomlist1 = new List<int>();
                                    if (filteredRooms.Count >= 1)
                                    {
                                        int numberOfAvailableRooms = filteredRooms[0].numberOfAvailableRooms;
                                        if (numberOfAvailableRooms >= SearchParam.Rooms.Count)
                                        {
                                            if (filteredRooms.Count == result.ToList().Count)
                                            {
                                                for (int b = 0; b < SearchParam.Rooms.Count; b++)
                                                {

                                                    for (int a = 0; a < filteredRooms.Count; a++)
                                                    {

                                                        if (SearchParam.Rooms[b].NoofAdults == filteredRooms[a].pax.adults)
                                                        {
                                                            if (SearchParam.Rooms[b].NoofChild == filteredRooms[a].pax.children.Count)
                                                            {
                                                                if (SearchParam.Rooms[b].NoofChild == 1)
                                                                {
                                                                    if (SearchParam.Rooms[b].Child1Age != Convert.ToInt16(filteredRooms[a].pax.children[0]))
                                                                    {
                                                                        continue;
                                                                    }
                                                                }
                                                                if (SearchParam.Rooms[b].NoofChild == 2)
                                                                {
                                                                    if (SearchParam.Rooms[b].Child1Age != Convert.ToInt16(filteredRooms[a].pax.children[0])
                                                                        && SearchParam.Rooms[b].Child2Age != Convert.ToInt16(filteredRooms[a].pax.children[1]))
                                                                    {
                                                                        continue;
                                                                    }
                                                                }
                                                                if (filteredRooms[a].numberOfAvailableRooms != 0)
                                                                {
                                                                    Allroomavailable = Allroomavailable + 1;
                                                                    roomlist1.Add(a);
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                                //if (Allroomavailable == result.ToList().Count)
                                                if (Allroomavailable == SearchParam.Rooms.Count)
                                                {
                                                    for (int f = 0; f < filteredRooms[0].ratePlans.Count; f++)
                                                    {
                                                        string rateplancode = filteredRooms[0].ratePlans[f].ratePlanCode;
                                                        string rateplanId = Convert.ToString(filteredRooms[0].ratePlans[f].ratePlanId);


                                                        string board = filteredRooms[0].ratePlans.FirstOrDefault(rp => rp.ratePlanCode == rateplancode).board;

                                                        bool sameMealOptionForAll = filteredRooms.All(room =>
                                                                room.ratePlans.Any(rp => rp.ratePlanCode == rateplancode && rp.board == board));

                                                        if (sameMealOptionForAll)
                                                        {
                                                            double Price = 0;
                                                            double RSprice = 0;
                                                            string RSpriceNew = "";
                                                            double exclusiveprice = 0;
                                                            string RoomNamee = "";
                                                            string Allremark = "";
                                                            string RoomTypeCord = filteredRooms[0].roomTypeCode;
                                                            currency = filteredRooms[0].ratePlans[0].inclusivePrice.currency;
                                                            int RoomId = filteredRooms[0].roomId;
                                                            int cancelday = 365;
                                                            string uniqid = Guid.NewGuid().ToString();
                                                            int CancelDayAll = 0;
                                                            double CurrencyFactorhyperguest = 1;

                                                            if (currency.ToLower() != commonSearch.tokenData.SiteCurrency.ToLower())
                                                            {

                                                                CurrencyFactorhyperguest = _commonMethod.GetROEByCurrency(currency, dtROE);
                                                            }


                                                            for (int x = 0; x < SearchParam.Rooms.Count; x++)
                                                            {
                                                                List<HyperguestClass.hyperguest.RoomList.RatePlan> roomlist = filteredRooms[roomlist1[x]].ratePlans.Where(rp1 => rp1.ratePlanCode == rateplancode).ToList();


                                                                Price = Price + Convert.ToDouble(roomlist[0].inclusivePrice.amount) * CurrencyFactorhyperguest;
                                                                RSprice = Convert.ToDouble(roomlist[0].inclusivePrice.amount);

                                                                RSpriceNew = RSpriceNew + Convert.ToString(RSprice) + "$$$";

                                                                exclusiveprice = exclusiveprice + Convert.ToDouble(roomlist[0].exclusivePrice.amount);
                                                                int RoomNo = x + 1;

                                                                if (SearchParam.Rooms.Count == 1)
                                                                {
                                                                    RoomNamee = filteredRooms[roomlist1[x]].roomName;
                                                                }
                                                                else
                                                                {
                                                                    RoomNamee = RoomNamee + '[' + RoomNo + ']' + filteredRooms[roomlist1[x]].roomName + " ";
                                                                }
                                                                string remarkdisply = "";
                                                                string RoomWiseRemark = "";
                                                                string remark = "";
                                                                string RemarkRoom = Hotelremark;
                                                                int policyday = 0;
                                                                try
                                                                {
                                                                    //for (int r = 0; r < roomlist[0].fees.mandatoryTaxes.Count; r++)
                                                                    //for (int r = 0; r < roomlist[0].prices.fees.Count; r++)
                                                                    //{
                                                                    //    string ab = roomlist[0].prices.fees[r].relation;
                                                                    //    if (ab.ToLower() == "display")
                                                                    //    {
                                                                    //        string madentorydescription = roomlist[0].prices.fees[r].description;
                                                                    //        string Mcurrency = roomlist[0].prices.fees[r].currency;
                                                                    //        double MFee = roomlist[0].prices.fees[r].amount;
                                                                    //        remarkdisply = "<br/>" + madentorydescription + ":" + MFee + " " + Mcurrency;
                                                                    //    }
                                                                    //    if (remarkdisply != "")
                                                                    //    {
                                                                    //        RoomWiseRemark = RoomWiseRemark + remarkdisply + "<br>";
                                                                    //    }
                                                                    //}
                                                                    if (RemarkRoom != "" || RoomWiseRemark != "")
                                                                    {
                                                                        remark = RemarkRoom + "<br/>" + RoomWiseRemark;

                                                                        if (SearchParam.Rooms.Count == 1)
                                                                            Allremark = remark;

                                                                        if (SearchParam.Rooms.Count > 1)
                                                                        {
                                                                            Allremark = Allremark + "[Room: " + RoomNo + "]" + remark;
                                                                        }
                                                                    }
                                                                    #region Policy
                                                                    if (roomlist[0].cancellationPolicies.Count != 0)
                                                                    {

                                                                        for (int d = 0; d < roomlist[0].cancellationPolicies.Count; d++)
                                                                        {
                                                                            policyday = Convert.ToInt32(roomlist[0].cancellationPolicies[d].daysBefore);
                                                                            if (d == 0)
                                                                            {
                                                                                cancelday = policyday;
                                                                            }
                                                                            else
                                                                            {
                                                                                if (cancelday < policyday)
                                                                                {
                                                                                    cancelday = policyday;
                                                                                }
                                                                            }
                                                                        }
                                                                    }

                                                                    if (SearchParam.Rooms.Count > 1)
                                                                    {
                                                                        if (x == 0)
                                                                        {
                                                                            CancelDayAll = cancelday;
                                                                        }
                                                                        else
                                                                        {
                                                                            if (CancelDayAll < cancelday)
                                                                            {
                                                                                CancelDayAll = cancelday;
                                                                            }
                                                                        }
                                                                    }
                                                                    else
                                                                    {
                                                                        CancelDayAll = cancelday;
                                                                    }


                                                                    #endregion
                                                                }
                                                                catch (Exception)
                                                                {
                                                                    throw;
                                                                }

                                                            }


                                                            string RoomTypeId = RoomTypeCord + "###" + rateplancode + "###" + xmlhotelcode + "###" + RSpriceNew + "###" + currency + "###" + XmlNationality + "###" + uniqid + "###" + rateplanId + "###" + RoomId + "###" + CurrencyFactorhyperguest;


                                                            double TotalAmount = Price;
                                                            double SubAgentMarkup = 0;
                                                            #region Ratebreakup
                                                            for (int n = 0; n < SearchParam.NoofNights; n++)
                                                            {
                                                                string CheckInDate = Convert.ToString(SearchParam.CheckInDate);
                                                                string CheckOutDate = Convert.ToString(SearchParam.CheckOutDate);
                                                                DateTime dtCheckInDate = DateTime.ParseExact(CheckInDate, "dd/MM/yyyy", null);
                                                                DateTime dtCheckOutDate = DateTime.ParseExact(CheckOutDate, "dd/MM/yyyy", null);
                                                                TimeSpan t = dtCheckOutDate - dtCheckInDate;
                                                                double ratePerNight = TotalAmount;

                                                                #region Vat
                                                                double VatOnPerNight = 0;
                                                                ratePerNight = _markup.ProcessHotelRateBreakupPrice_WithMarkup(1, SearchParam.CityID, SearchParam.CountryCode, ratePerNight, commonSearch.B2CMarkup, commonSearch.IsParentId, isPerce, Convert.ToDouble(commonSearch.AgentPercent), AgentMarkupAmount, SubAgentMarkup, ref VatOnPerNight);
                                                                #endregion

                                                                ratePerNight = ratePerNight / SearchParam.NoofNights;
                                                                DataRow drRateBreackUp = dthyperguestRoomRateBreakup.NewRow();
                                                                drRateBreackUp["SupplierName"] = Supplier;
                                                                drRateBreackUp["OwnsystemHotelId"] = OwnsystemHotelId;
                                                                drRateBreackUp["ContractID"] = 0;
                                                                drRateBreackUp["RoomNo"] = Convert.ToInt32(1);
                                                                drRateBreackUp["RoomTypeId"] = RoomTypeId;
                                                                drRateBreackUp["MealId"] = board;
                                                                drRateBreackUp["Meal"] = board;
                                                                string rateWeekColumn = Convert.ToString(dtCheckInDate.Date.AddDays(n).DayOfWeek).Substring(0, 3);
                                                                drRateBreackUp["DateOfWeek"] = rateWeekColumn;
                                                                DateTime rateDate = dtCheckInDate.Date.AddDays(n);
                                                                drRateBreackUp["Date"] = rateDate;
                                                                drRateBreackUp["Rate"] = ratePerNight;
                                                                drRateBreackUp["OrderNo"] = 1;
                                                                drRateBreackUp["RoomTypeName"] = RoomNamee;
                                                                drRateBreackUp["DateOfWeek"] = rateWeekColumn;
                                                                drRateBreackUp["Date"] = rateDate;
                                                                drRateBreackUp["BookingStatus"] = "Available";
                                                                drRateBreackUp["BoardTypeID"] = "";
                                                                drRateBreackUp["VatdiffAmount"] = 0;
                                                                drRateBreackUp["RateType"] = RoomNamee;
                                                                drRateBreackUp["SupplierCurrency"] = currency;
                                                                drRateBreackUp["BuyingPrice"] = ratePerNight / CurrencyFactorHyperGuest;
                                                                dthyperguestRoomRateBreakup.Rows.Add(drRateBreackUp);

                                                            }
                                                            #endregion
                                                            #region Rooms
                                                            DataRow drRoom = dthyperguestHotelRoom.NewRow();
                                                            drRoom["OwnsystemHotelId"] = OwnsystemHotelId;
                                                            drRoom["RoomTypeCode"] = RoomTypeCord;
                                                            drRoom["BoardType"] = board;
                                                            drRoom["MealName"] = board;
                                                            drRoom["MealId"] = board;
                                                            drRoom["Type"] = "";
                                                            drRoom["Board"] = "";
                                                            drRoom["BookingStatus"] = "Available";
                                                            drRoom["UniqueNo"] = Guid.NewGuid().ToString();
                                                            drRoom["ContractID"] = 0;
                                                            drRoom["RoomNo"] = Convert.ToInt32(1);
                                                            drRoom["BoardTypeID"] = "";
                                                            drRoom["AdultsOccupancy"] = totalAdult;
                                                            drRoom["ChildOccupancy"] = totalChild;
                                                            drRoom["OfferCode"] = "";
                                                            drRoom["OfferDescription"] = "";
                                                            drRoom["RoomSequenceNo"] = "";

                                                            string EssentialInformation = Allremark;
                                                            string Enn = "";

                                                            drRoom["EssentialInformation"] = EssentialInformation;
                                                            drRoom["Characteristic"] = "";
                                                            bool IsCombine = true;//new CHANGE
                                                            bool IsDynamicPolicy = true;
                                                            #region Vat
                                                            double itemprice = Price;
                                                            _markup.ProcessHotelRoom_WithMarkup(CurrencyFactorHyperGuest, currency, IsDynamicPolicy, IsCombine, SearchParam.NoofNights * SearchParam.Rooms.Count, SearchParam.CityID, SearchParam.CountryCode, SearchParam.CheckInDate, SearchParam.CheckOutDate, ref Price, ref drRoom, isPerce, AgentPercentage, AgentMarkupAmount, SubAgentMarkup, commonSearch.B2CMarkup, commonSearch.IsParentId);
                                                            #endregion
                                                            drRoom["SupplierName"] = Supplier;
                                                            drRoom["RoomTypeName"] = RoomNamee;
                                                            drRoom["RoomTypeId"] = RoomTypeId;
                                                            dthyperguestHotelRoom.Rows.Add(drRoom);
                                                            #endregion

                                                            #region cancellationpolicy


                                                            DateTime CancellationDate = DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", CultureInfo.InvariantCulture).Date;
                                                            double Penalty = Convert.ToDouble(Price);
                                                            double CompanyChargeAmount = Price;
                                                            double MainAgentCharge = Price;
                                                            double MainAgentCharge_wl = Price;
                                                            double CancelCharge_wl = Price;

                                                            DateTime MainDate = new DateTime();
                                                            DateTime StartDate = MainDate;
                                                            DateTime CancelDate = CancellationDate.Date.AddDays(-CancelDayAll);
                                                            DateTime NowDate = DateTime.Now.Date;
                                                            //int CutOffDays = -1;
                                                            double VatOnPolicy = 0;
                                                            string remark1 = "";

                                                            if (CancelDate.AddDays(CutOffDays).Date > DateTime.Now.Date)
                                                            {
                                                                CancelDate = CancelDate.AddDays(CutOffDays).Date;
                                                            }
                                                            else
                                                            {
                                                                CancelDate = DateTime.Now.Date;
                                                            }
                                                            if (NowDate < CancelDate)
                                                            {


                                                                double zero = 0;
                                                                dthyperguestCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref zero, dthyperguestCancellation.NewRow(), CurrencyFactorhyperguest, currency, Supplier, Convert.ToString(OwnsystemHotelId), Convert.ToString(1), Convert.ToString(RoomTypeId), RoomNamee, board, Convert.ToDateTime(NowDate), CancelDate.AddDays(-1), 0, 0, "0", VatOnPolicy, uniqid, "NO", "", remark1 != string.Empty ? remark1 : "No Essential Information Available", "", ""));

                                                                //dtParsecCacelationPolicy.Rows.Add(Common.setCancellationPolicyRow(0, 0, ref zero, dtParsecCacelationPolicy.NewRow(), CurrencyFactorhyperguest, currency, Supplier, OwnsystemHotelId, Convert.ToString(1), Convert.ToString(RoomTypeId), RoomNamee, board, Convert.ToDateTime(NowDate), CancelDate.AddDays(-1), 0, 0, "0", VatOnPolicy, uniqid, "NO", "", remark1 != string.Empty ? remark1 : "No Essential Information Available", "", ""));
                                                                dthyperguestCancellation.AcceptChanges();

                                                                dthyperguestCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref MainAgentCharge_wl, dthyperguestCancellation.NewRow(), CurrencyFactorhyperguest, currency, Supplier, Convert.ToString(OwnsystemHotelId), Convert.ToString(1),
                                                                                     Convert.ToString(RoomTypeId), RoomNamee, board, Convert.ToDateTime(CancelDate), DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null), MainAgentCharge, Penalty, "0", VatOnPolicy, uniqid, "NO", "", remark1 != string.Empty ? remark1 : "No Essential Information Available", "", ""));
                                                                dthyperguestCancellation.AcceptChanges();
                                                            }
                                                            else
                                                            {
                                                                // dtParsecCacelationPolicy.Rows.Add(Common.setCancellationPolicyRow(MainAgentCharge_wl, CancelCharge_wl, ref CompanyChargeAmount, dtParsecCacelationPolicy.NewRow(), CurrencyFactorhyperguest, currency, Supplier, OwnsystemHotelId, Convert.ToString(1), Convert.ToString(RoomTypeId), RoomNamee, board, NowDate, DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null), MainAgentCharge, Penalty, "0", VatOnPolicy, uniqid, "Yes", "", remark1 != string.Empty ? remark1 : "No Essential Information Available"));
                                                                dthyperguestCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref MainAgentCharge_wl, dthyperguestCancellation.NewRow(), CurrencyFactorhyperguest, currency, Supplier, Convert.ToString(OwnsystemHotelId), Convert.ToString(1), Convert.ToString(RoomTypeId), RoomNamee, board, NowDate, DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null), MainAgentCharge, Penalty, "0", VatOnPolicy, uniqid, "Yes", "", remark1 != string.Empty ? remark1 : "No Essential Information Available"));

                                                                dthyperguestCancellation.AcceptChanges();
                                                            }
                                                            #endregion


                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            Options ops = ((await _supplierRes.GetCommonRoomForSuppliers(GiataId, commonSearch.tokenData, commonSearch.advancedOptions, MinPrice, drXmlHotel, dthyperguestHotelRoom, dthyperguestCancellation, dthyperguestRoomRateBreakup, SearchParam)));
                            if (ops != null)
                                options.Add(ops);
                            // here
                            #endregion
                        }
                        #endregion

                    }
                    return options;




                }
            }
            catch (Exception e)

            {
                await _exception.Error(e);
            }
            finally
            {
                #region ResProcessTimer
                try
                {
                    if (SearchParam != null && ResProccessTimerLogActive == "1" && timerroomx.ElapsedMilliseconds > Convert.ToInt32(ResProccessTimeValueforSaveLog))
                    {
                        timerroomx.Stop();
                        SearchParam.SupplierTime += ",|Finish:hyperguest Process Response  " + "|Time :" + timerroomx.Elapsed.ToString();
                        _= Task.Run(() => _commonMethod.FireAway_SQL_ResPorcessTimeLog("", timerroomx.Elapsed.ToString(), Supplier, SearchParam.SearchKey, 1, SearchParam.AgentID));
                    }
                }
                catch (Exception x)
                {
                    await _exception.Error(x);
                }
                #endregion

                if (Item.ResponseType == "NoCount" && StoreSupplierResErrorLog == "1")
                    await _commonMethod.XMLRequestResponseTrack_Error(Item.RQRS[0].Request, Item.RQRS[0].Response, Item.Supplier, timerroomx.Elapsed.ToString(), "", SearchParam.SearchKey);
            }
            return options;
        }


        public async Task<List<Options>> hyperguestHotelSearchWithRooms_Multiroom(HotelX Item, CommonSearch commonSearch, string ResponseData, string CityCode, HotelSearchParameter SearchParam, double CurrencyFactorHyperGuest, string Supplier, DataSet dsAgentDetails, DataTable dtMarkupDetails, string CurrencyCode, DataTable dtROE, string XmlNationality, Int32 processTimeout, string ResProccessTimerLogActive, string ResProccessTimeValueforSaveLog, string StoreSupplierResErrorLog)
        {
            System.Diagnostics.Stopwatch timerroomx = new System.Diagnostics.Stopwatch();
            timerroomx.Start();
            List<Options> options = new List<Options>();
            DataTable DtHotel = _commonMethod.GenerateDataTableNewV2();
            try
            {
                HyperguestClass.hyperguest.HotelList1.Root DS = null;
                try
                {
                    DS = Newtonsoft.Json.JsonConvert.DeserializeObject<HyperguestClass.hyperguest.HotelList1.Root>(ResponseData);

                }

                catch (Exception ex)
                {
                    Item.SupplierResponseError = ResponseData;
                    Item.ResponseStatus = 404;
                    Item.ResponseType = "NoCount";
                    _exception.Error(ex);
                }
                if (DS == null)
                {
                    Item.ResponseStatus = 204;
                    Item.ResponseType = "NoCount";
                    return options;
                }

                if (DS.results.Count < 1)
                {
                    Item.SupplierResponseError = ResponseData;
                    Item.ResponseStatus = 404;
                    Item.ResponseType = "NoCount";
                    return options;
                }
                var result = SearchParam.Rooms.GroupBy(x => new
                {
                    x.NoofAdults,
                    x.NoofChild,
                    x.Child1Age,
                    x.Child2Age
                }).Select(n => new
                {
                    NoofAdults = n.Key.NoofAdults,
                    NoofChild = n.Key.NoofChild,
                    ChildAge = n.ToList(),
                    Count = n.Count()
                });
                String currency = "";

                for (int i = 0; i < DS.results.Count; i++)
                {
                    {
                        double Pricenew = 0;
                        double LowRate = 0;
                        bool israte = true;
                        string BoardType = "";
                        //if (DS.results[i].rooms.Count() == 0)
                        //{
                        //    continue;
                        //}

                        if (DS.results[i].rooms.Count > 0)
                        {
                            LowRate = 0;
                            #region "When not Need Room"  
                            if (SearchParam.Filters.IsShowRooms == 0)
                            {
                                #region "Hotel Code Block "
                                var distinctRoomTypeCodes = DS.results[i].rooms.Select(x => x.roomTypeCode).Distinct().ToList();
                                var roomLookup = distinctRoomTypeCodes.ToDictionary(roomTypeCode => roomTypeCode, roomTypeCode => DS.results[i].rooms.Where(x => x.roomTypeCode == roomTypeCode).ToList());

                                for (int ro = 0; ro < DS.results[i].rooms.Count; ro++)
                                {
                                    Item.ResponseType = "Success";
                                    int numberOfAvailablerooms1 = DS.results[i].rooms[ro].numberOfAvailableRooms;
                                    if (numberOfAvailablerooms1 < SearchParam.Rooms.Count)
                                    {
                                        continue;
                                    }

                                    for (int RT = 0; RT < DS.results[i].rooms[ro].ratePlans.Count(); RT++)
                                    {
                                        string rateplancode = DS.results[i].rooms[ro].ratePlans[RT].ratePlanCode;
                                        string board = DS.results[i].rooms[ro].ratePlans[RT].board;

                                        currency = DS.results[i].rooms[ro].ratePlans[RT].prices.net.currency;
                                        double CurrencyFactor = 1;
                                        //added on 09-02-2024 #40209
                                        //if (currency.ToLower() == "ars" || currency.ToLower() == "usd" || currency.ToLower() == "eur" || currency.ToLower() == "brl")
                                        //{
                                        //    if (currency.ToLower() != "usd")
                                        //    {
                                        //        if (currency.ToLower() == "ars")
                                        //        {
                                        //            currency = "AR$";
                                        //        }
                                        //        CurrencyFactor = _commonMethod.GetROEByCurrency(currency, dtROE);
                                        //    }

                                        //}
                                        //else
                                        //{
                                        //    continue;
                                        //}
                                        //end
                                        if (currency.ToLower() != commonSearch.tokenData.SiteCurrency.ToLower())
                                        {
                                            CurrencyFactor = _commonMethod.GetROEByCurrency(currency, dtROE);
                                        }

                                       

                                        foreach (var roomTypeCode1 in distinctRoomTypeCodes)
                                        {
                                            var filteredRooms = roomLookup[roomTypeCode1];
                                            int Allroomavailable = 0;
                                            List<int> roomlist1 = new List<int>();

                                            // Reduce nested loop by pre-checking availability
                                            if (filteredRooms.Count >= 1)
                                            {
                                                int numberOfAvailableRooms = filteredRooms[0].numberOfAvailableRooms;
                                                if (numberOfAvailableRooms >= SearchParam.Rooms.Count)
                                                {
                                                    for (int b = 0; b < SearchParam.Rooms.Count; b++)
                                                    {
                                                        for (int a = 0; a < filteredRooms.Count; a++)
                                                        {
                                                            // Simplified matching conditions
                                                            var room = filteredRooms[a];
                                                            if (SearchParam.Rooms[b].NoofAdults == room.searchedPax.adults && SearchParam.Rooms[b].NoofChild == room.searchedPax.children.Count)
                                                            {
                                                                if (SearchParam.Rooms[b].NoofChild == 1 && SearchParam.Rooms[b].Child1Age != Convert.ToInt16(room.searchedPax.children[0]))
                                                                {
                                                                    continue;
                                                                }
                                                                if (SearchParam.Rooms[b].NoofChild == 2 && (SearchParam.Rooms[b].Child1Age != Convert.ToInt16(room.searchedPax.children[0]) || SearchParam.Rooms[b].Child2Age != Convert.ToInt16(room.searchedPax.children[1])))
                                                                {
                                                                    continue;
                                                                }

                                                                if (room.numberOfAvailableRooms != 0)
                                                                {
                                                                    Allroomavailable++;
                                                                    roomlist1.Add(a);
                                                                }
                                                            }
                                                        }
                                                    }
                                                    if (Allroomavailable == SearchParam.Rooms.Count)
                                                    {
                                                        bool sameMealOptionForAll = filteredRooms.All(room => room.ratePlans.Any(rp => rp.ratePlanCode == rateplancode && rp.board == board));
                                                        // Calculate price after checking availability
                                                        for (int f = 0; f < filteredRooms[0].ratePlans.Count; f++)
                                                        {
                                                            string rateplanId = Convert.ToString(filteredRooms[0].ratePlans[f].ratePlanId);
                                                            if (sameMealOptionForAll)
                                                            {
                                                                string RoomTypeCord = filteredRooms[0].roomTypeCode;
                                                                int RoomId = filteredRooms[0].roomId;
                                                                string uniqid = Guid.NewGuid().ToString();


                                                                Pricenew = 0;
                                                                if (roomlist1.Count >= Convert.ToInt32(SearchParam.Rooms.Count))
                                                                {
                                                                    Pricenew = roomlist1.Sum(x =>
                                                                    {
                                                                        try
                                                                        {
                                                                            var room = filteredRooms[x];
                                                                            List<HyperguestClass.hyperguest.HotelList1.RatePlan> roomlist = room.ratePlans.Where(rp1 => rp1.ratePlanCode == rateplancode).ToList();
                                                                            return Convert.ToDouble(roomlist[0].prices.net.price) * CurrencyFactor;
                                                                        }
                                                                        catch (Exception e1)
                                                                        {
                                                                            _exception.Error(e1);
                                                                            return 0;
                                                                        }
                                                                    });

                                                                    LowRate = (LowRate == 0) ? Pricenew : Math.Min(LowRate, Pricenew);
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }

                                Double NetPrice = LowRate;
                                #region "Hotel Code Block "
                                DataRow drXmlHotel = DtHotel.NewRow();
                                drXmlHotel["XMLHotelCode"] = Convert.ToString(DS.results[i].propertyId);
                                drXmlHotel["HotelName"] = "";
                                drXmlHotel["SupplierName"] = Supplier;
                                drXmlHotel["OfficeID"] = 0;
                                drXmlHotel["ContractID"] = 0;
                                drXmlHotel["Rating"] = 0;
                                drXmlHotel["Reviews"] = 0;
                                drXmlHotel["UniqueNo"] = 0;
                                drXmlHotel["IsShowSupplier"] = false;
                                drXmlHotel["BookingStatus"] = "Available";
                                drXmlHotel["XMLCity"] = "";
                                drXmlHotel["SupplierCurrency"] = currency;
                                drXmlHotel["FinalSellingPrice"] = NetPrice;
                                drXmlHotel["SellingPrice"] = NetPrice;
                                drXmlHotel["OfferText"] = "";
                                DtHotel.Rows.Add(drXmlHotel);
                                Double FinalTotal = NetPrice;
                                #endregion
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
                                        decimal LowestPrice = Convert.ToDecimal(MinPrice).CalculatePrice(commonSearch.tokenData, commonSearch.advancedOptions, currency);
                                        Options ops = (await _supplierRes.GetCommonRoomForSuppliers(GiataId, commonSearch.tokenData, commonSearch.advancedOptions, Convert.ToDouble(LowestPrice), drXmlHotel, dt_HotelRoom, dt_Cancellation, dt_RateBreackUp, SearchParam));
                                        if (ops != null)
                                            options.Add(ops);
                                    }
                                    #endregion
                                }


                                #endregion
                            }
                            #endregion
                            #region"When Room need"
                            else
                            {
                                string xmlhotelcode = Convert.ToString(DS.results[i].propertyId);
                                Item.ResponseType = "Success";
                                string HotelID = xmlhotelcode;
                                double FinalMinPrice = 0;
                                DataRow drXmlHotel = DtHotel.NewRow();
                                drXmlHotel["XMLHotelCode"] = HotelID;
                                drXmlHotel["HotelName"] = "";
                                drXmlHotel["SupplierName"] = Supplier;
                                drXmlHotel["OfficeID"] = 0;
                                drXmlHotel["ContractID"] = 0;
                                drXmlHotel["Rating"] = 0;
                                drXmlHotel["Reviews"] = 0;
                                drXmlHotel["UniqueNo"] = 0;
                                drXmlHotel["IsShowSupplier"] = false;
                                drXmlHotel["BookingStatus"] = "Available";
                                drXmlHotel["XMLCity"] = "";
                                drXmlHotel["SupplierCurrency"] = currency;
                                drXmlHotel["FinalSellingPrice"] = 1;
                                drXmlHotel["SellingPrice"] = 1;
                                drXmlHotel["OfferText"] = "";
                                DtHotel.Rows.Add(drXmlHotel);

                                int CutOffDays = XMLHotelBookingCommon.CutOffDays;
                                CutOffDays = _commonMethod.GetCutOffDays(commonSearch.dtSuplierMarkup, Supplier);
                                int totalAdult = 0;
                                int totalChild = 0;
                                for (int b = 0; b < SearchParam.Rooms.Count; b++)
                                {
                                    totalAdult = Convert.ToInt32(SearchParam.Rooms[b].NoofAdults) + totalAdult;
                                    totalChild = Convert.ToInt32(SearchParam.Rooms[b].NoofChild) + totalChild;
                                }
                                bool willAddOrNot = true;
                                Int64 RaynaID = 0;
                                Int64 GiataId = 0;
                                int RatingHotel = 0;
                                Int64 OwnsystemHotelId = 0;
                                double MinPrice = 1;
                                double AgentPercentage = 0;
                                double AgentMarkupAmount = 0;
                                Boolean blnIsShowSupplier = false;
                                Boolean isPerce = true;


                                #region "Room code Block"
                                int Rating = 0;
                                DataTable dtParsecRoom = _commonMethod.GenerateRoomtypeDataTableNewV2();
                                DataTable dtParsecCacelationPolicy = _commonMethod.GenerateCancellationDataTableNewV2();
                                DataTable dtParsecRoomRateBreakup = _commonMethod.GenerateoomRateBreackUpDataTableNewV2();
                                _markup.GetHotelDataCommon(ref MinPrice, ref willAddOrNot, ref OwnsystemHotelId, ref RaynaID, ref GiataId, ref Rating, ref drXmlHotel, commonSearch.AgentId, commonSearch.TariffID, commonSearch.dsAgentDetails, commonSearch.supplierGiataMaster, SearchParam, commonSearch.dtCountryCityMarkup, commonSearch.B2CMarkup, commonSearch.IsParentId, commonSearch.SubAgentMarkup, commonSearch.SearchByHotel, commonSearch.HotelIDS);
                                if (willAddOrNot)
                                {
                                    //_markup.getAgetMarkupSetting("hyperguest", GiataId.ToString(), commonSearch.dtSuplierMarkup, commonSearch.dtMarkupDetails, ref AgentPercentage, ref AgentMarkupAmount, ref blnIsShowSupplier, ref isPerce, commonSearch.AgentId, RatingHotel, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), Convert.ToString(SearchParam.CheckInDate));

                                    _markup.getAgetMarkupSettingNew_new(Supplier, Convert.ToString(RaynaID), commonSearch.dtSuplierMarkup, commonSearch.dsMarkupDetails, ref AgentPercentage, ref AgentMarkupAmount, ref blnIsShowSupplier, ref isPerce, commonSearch.AgentId, RatingHotel, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), SearchParam.CheckInDate);
                                    string Hotelremark = "";
                                    for (int m = 0; m < DS.results[i].remarks.Count; m++)
                                    {
                                        string remark1 = Convert.ToString(DS.results[i].remarks[m]);
                                        if (m == 0)
                                            Hotelremark = remark1;
                                        else
                                            Hotelremark = Hotelremark + "<br>" + remark1;
                                    }

                                    _markup.GetHotelDataCommon(ref MinPrice, ref willAddOrNot, ref OwnsystemHotelId, ref RaynaID, ref GiataId, ref RatingHotel, ref drXmlHotel, commonSearch.AgentId, commonSearch.TariffID, commonSearch.dsAgentDetails, commonSearch.supplierGiataMaster, SearchParam, commonSearch.dtCountryCityMarkup, commonSearch.B2CMarkup, commonSearch.IsParentId, commonSearch.SubAgentMarkup, commonSearch.SearchByHotel, commonSearch.HotelIDS);
                                    var distinctRoomTypeCodes = DS.results[i]
                                        .rooms
                                        //.SelectMany(resulta => resulta.rooms)
                                        .Select(room => room.roomTypeCode)
                                        .Distinct();

                                    foreach (var roomTypeCode1 in distinctRoomTypeCodes)
                                    {
                                        List<HyperguestClass.hyperguest.HotelList1.Room> filteredRooms = DS.results[i].rooms
                                                    .Where(x => Convert.ToString(x.roomTypeCode) == roomTypeCode1)
                                                    .ToList();

                                        int Allroomavailable = 0;
                                        List<int> roomlist1 = new List<int>();

                                        if (filteredRooms != null && filteredRooms.Count > 0)
                                        {
                                            int numberOfAvailableRooms = filteredRooms[0].numberOfAvailableRooms;
                                            if (numberOfAvailableRooms >= SearchParam.Rooms.Count)
                                            {
                                                if (filteredRooms.Count == result.ToList().Count)
                                                {
                                                    for (int b = 0; b < SearchParam.Rooms.Count; b++)
                                                    {

                                                        for (int a = 0; a < filteredRooms.Count; a++)
                                                        {

                                                            if (SearchParam.Rooms[b].NoofAdults == filteredRooms[a].searchedPax.adults)
                                                            {
                                                                if (SearchParam.Rooms[b].NoofChild == filteredRooms[a].searchedPax.children.Count)
                                                                {
                                                                    if (SearchParam.Rooms[b].NoofChild == 1)
                                                                    {
                                                                        if (SearchParam.Rooms[b].Child1Age != Convert.ToInt16(filteredRooms[a].searchedPax.children[0]))
                                                                        {
                                                                            continue;
                                                                        }
                                                                    }
                                                                    if (SearchParam.Rooms[b].NoofChild == 2)
                                                                    {
                                                                        if (SearchParam.Rooms[b].Child1Age != Convert.ToInt16(filteredRooms[a].searchedPax.children[0])
                                                                            && SearchParam.Rooms[b].Child2Age != Convert.ToInt16(filteredRooms[a].searchedPax.children[1]))
                                                                        {
                                                                            continue;
                                                                        }
                                                                    }
                                                                    if (filteredRooms[a].numberOfAvailableRooms != 0)
                                                                    {
                                                                        Allroomavailable = Allroomavailable + 1;
                                                                        roomlist1.Add(a);
                                                                    }
                                                                }
                                                            }
                                                        }
                                                    }
                                                    //if (Allroomavailable == result.ToList().Count)
                                                    if (Allroomavailable == SearchParam.Rooms.Count)
                                                    {
                                                        for (int f = 0; f < filteredRooms[0].ratePlans.Count; f++)
                                                        {
                                                            string rateplancode = filteredRooms[0].ratePlans[f].ratePlanCode;
                                                            string rateplanId = Convert.ToString(filteredRooms[0].ratePlans[f].ratePlanId);


                                                            string board = filteredRooms[0].ratePlans.FirstOrDefault(rp => rp.ratePlanCode == rateplancode).board;

                                                            bool sameMealOptionForAll = filteredRooms.All(room =>
                                                                    room.ratePlans.Any(rp => rp.ratePlanCode == rateplancode && rp.board == board));

                                                            if (sameMealOptionForAll)
                                                            {
                                                                double Price = 0;
                                                                double RSprice = 0;
                                                                string RSpriceNew = "";
                                                                double exclusiveprice = 0;
                                                                string RoomNamee = "";
                                                                string Allremark = "";
                                                                string RoomTypeCord = filteredRooms[0].roomTypeCode;
                                                                currency = filteredRooms[0].ratePlans[0].prices.net.currency;
                                                                int RoomId = filteredRooms[0].roomId;
                                                                int cancelday = 365;
                                                                string uniqid = Guid.NewGuid().ToString();
                                                                int CancelDayAll = 0;
                                                                double CurrencyFactorhyperguest = 1;
                                                                //added on 09-02-2024 #40209
                                                                //if (currency.ToLower() == "ars" || currency.ToLower() == "usd" || currency.ToLower() == "eur" || currency.ToLower() == "brl")
                                                                //{
                                                                //    if (currency.ToLower() != "usd")
                                                                //    {
                                                                //        if (currency.ToLower() == "ars")
                                                                //        {
                                                                //            currency = "AR$";
                                                                //        }
                                                                //        CurrencyFactorhyperguest = _commonMethod.GetROEByCurrency(currency, dtROE);
                                                                //    }
                                                                //}
                                                                //else
                                                                //{
                                                                //    continue;
                                                                //}
                                                                //end
                                                                if (currency.ToLower() != commonSearch.tokenData.SiteCurrency.ToLower())
                                                                {
                                                                    CurrencyFactorhyperguest = _commonMethod.GetROEByCurrency(currency, dtROE);
                                                                }
                                                                
                                                                for (int x = 0; x < SearchParam.Rooms.Count; x++)
                                                                {
                                                                    List<HyperguestClass.hyperguest.HotelList1.RatePlan> roomlist = filteredRooms[roomlist1[x]].ratePlans.Where(rp1 => rp1.ratePlanCode == rateplancode).ToList();
                                                                    //List<hyperguest.HotelList.RatePlan> roomlist = filteredRooms[roomlist1[x]].ratePlans.Where(rp1 => rp1.ratePlanCode == rateplancode).ToList();

                                                                    Price = Price + Convert.ToDouble(roomlist[0].prices.net.price) * CurrencyFactorhyperguest;
                                                                    RSprice = Convert.ToDouble(roomlist[0].prices.net.price);

                                                                    RSpriceNew = RSpriceNew + Convert.ToString(RSprice) + "$$$";

                                                                    exclusiveprice = exclusiveprice + Convert.ToDouble(roomlist[0].prices.sell.price);
                                                                    int RoomNo = x + 1;

                                                                    if (SearchParam.Rooms.Count == 1)
                                                                    {
                                                                        RoomNamee = filteredRooms[roomlist1[x]].roomName;
                                                                    }
                                                                    else
                                                                    {
                                                                        RoomNamee = RoomNamee + '[' + RoomNo + ']' + filteredRooms[roomlist1[x]].roomName + " ";
                                                                    }
                                                                    string remarkdisply = "";
                                                                    string RoomWiseRemark = "";
                                                                    string remark = "";
                                                                    string RemarkRoom = Hotelremark;
                                                                    int policyday = 0;
                                                                    try
                                                                    {
                                                                        if (RemarkRoom != "" || RoomWiseRemark != "")
                                                                        {
                                                                            remark = RemarkRoom + "<br/>" + RoomWiseRemark;

                                                                            if (SearchParam.Rooms.Count == 1)
                                                                                Allremark = remark;

                                                                            if (SearchParam.Rooms.Count > 1)
                                                                            {
                                                                                Allremark = Allremark + "[Room: " + RoomNo + "]" + remark;
                                                                            }
                                                                        }
                                                                        #region Policy
                                                                        if (roomlist[0].cancellationPolicies.Count != 0)
                                                                        {

                                                                            for (int d = 0; d < roomlist[0].cancellationPolicies.Count; d++)
                                                                            {
                                                                                policyday = Convert.ToInt32(roomlist[0].cancellationPolicies[d].daysBefore);
                                                                                if (d == 0)
                                                                                {
                                                                                    cancelday = policyday;
                                                                                }
                                                                                else
                                                                                {
                                                                                    if (cancelday < policyday)
                                                                                    {
                                                                                        cancelday = policyday;
                                                                                    }
                                                                                }
                                                                            }
                                                                        }

                                                                        if (SearchParam.Rooms.Count > 1)
                                                                        {
                                                                            if (x == 0)
                                                                            {
                                                                                CancelDayAll = cancelday;
                                                                            }
                                                                            else
                                                                            {
                                                                                if (CancelDayAll < cancelday)
                                                                                {
                                                                                    CancelDayAll = cancelday;
                                                                                }
                                                                            }
                                                                        }
                                                                        else
                                                                        {
                                                                            CancelDayAll = cancelday;
                                                                        }


                                                                        #endregion
                                                                    }
                                                                    catch (Exception)
                                                                    {
                                                                        throw;
                                                                    }

                                                                }

                                                                string RoomTypeId = RoomTypeCord + "###" + rateplancode + "###" + xmlhotelcode + "###" + RSpriceNew + "###" + currency + "###" + XmlNationality + "###" + uniqid + "###" + rateplanId + "###" + RoomId;

                                                                double TotalAmount = Price;

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
                                                                //    double Pricenew_wl = 0;
                                                                //    ratePerNight = _commonMethod.ProcessHotelRateBreakupPrice_WithMarkup(1, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), ratePerNight, commonSearch.B2CMarkup, commonSearch.IsParentId, isPerce, AgentPercentage, AgentMarkupAmount, commonSearch.SubAgentMarkup, ref VatOnPerNight);
                                                                //    // ratePerNight = ratePerNight;
                                                                //    #endregion

                                                                //    ratePerNight = ratePerNight / SearchParam.NoofNights;
                                                                //    DataRow drRateBreackUp = dtParsecRoomRateBreakup.NewRow();
                                                                //    drRateBreackUp["SupplierName"] = Supplier;
                                                                //    drRateBreackUp["OwnsystemHotelId"] = OwnsystemHotelId;
                                                                //    drRateBreackUp["ContractID"] = 0;
                                                                //    drRateBreackUp["RoomNo"] = Convert.ToInt32(1);
                                                                //    drRateBreackUp["RoomTypeId"] = RoomTypeId;
                                                                //    drRateBreackUp["MealId"] = board;
                                                                //    drRateBreackUp["Meal"] = board;
                                                                //    string rateWeekColumn = Convert.ToString(dtCheckInDate.Date.AddDays(n).DayOfWeek).Substring(0, 3);
                                                                //    drRateBreackUp["DateOfWeek"] = rateWeekColumn;
                                                                //    DateTime rateDate = dtCheckInDate.Date.AddDays(n);
                                                                //    drRateBreackUp["Date"] = rateDate;
                                                                //    drRateBreackUp["Rate"] = ratePerNight;
                                                                //    drRateBreackUp["OrderNo"] = 1;
                                                                //    drRateBreackUp["RoomTypeName"] = RoomNamee;
                                                                //    drRateBreackUp["DateOfWeek"] = rateWeekColumn;
                                                                //    drRateBreackUp["Date"] = rateDate;
                                                                //    drRateBreackUp["BookingStatus"] = "Available";
                                                                //    drRateBreackUp["BoardTypeID"] = "";
                                                                //    drRateBreackUp["VatdiffAmount"] = 0;
                                                                //    drRateBreackUp["RateType"] = RoomNamee;
                                                                //    drRateBreackUp["SupplierCurrency"] = currency;
                                                                //    drRateBreackUp["BuyingPrice"] = ratePerNight / CurrencyFactorhyperguest;
                                                                //    //drRateBreackUp["BuyingPrice"] = ratePerNight;
                                                                //    dtParsecRoomRateBreakup.Rows.Add(drRateBreackUp);
                                                                //}
                                                                #endregion

                                                                #region Rooms
                                                                DataRow drRoom = dtParsecRoom.NewRow();
                                                                drRoom["OwnsystemHotelId"] = OwnsystemHotelId;
                                                                drRoom["RoomTypeCode"] = RoomTypeCord;
                                                                drRoom["BoardType"] = board;
                                                                drRoom["MealName"] = board;
                                                                drRoom["MealId"] = board;
                                                                drRoom["Type"] = "";
                                                                drRoom["Board"] = "";
                                                                drRoom["BookingStatus"] = "Available";
                                                                drRoom["UniqueNo"] = uniqid;
                                                                drRoom["ContractID"] = 0;
                                                                drRoom["RoomNo"] = Convert.ToInt32(1);
                                                                drRoom["BoardTypeID"] = "";
                                                                drRoom["AdultsOccupancy"] = totalAdult;
                                                                drRoom["ChildOccupancy"] = totalChild;
                                                                drRoom["OfferCode"] = "";
                                                                drRoom["OfferDescription"] = "";
                                                                drRoom["RoomSequenceNo"] = "";

                                                                string EssentialInformation = Allremark;

                                                                drRoom["EssentialInformation"] = EssentialInformation;
                                                                drRoom["Characteristic"] = "";
                                                                #region Vat
                                                                double itemprice = Price;
                                                                _markup.ProcessHotelRoom_WithMarkup(CurrencyFactorhyperguest, currency, false, true, SearchParam.NoofNights, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), SearchParam.CheckInDate, SearchParam.CheckOutDate, ref Price, ref drRoom, isPerce, AgentPercentage, AgentMarkupAmount, commonSearch.SubAgentMarkup, commonSearch.B2CMarkup, commonSearch.IsParentId); //added taxPer 13-07-2024
                                                                                                                                                                                                                                                                                                                                                                                                                                                // _markup.ProcessHotelRoom_WithMarkup(CurrencyFactorhyperguest, currency, false, true, SearchParam.NoofNights, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), SearchParam.CheckInDate, SearchParam.CheckOutDate, ref Price, ref drRoom, isPerce, AgentPercentage, AgentMarkupAmount, commonSearch.SubAgentMarkup, commonSearch.B2CMarkup, commonSearch.IsParentId, SupplierID, GiataId, dsAgentDetails, dtVatHotelList); //added taxPer 13-07-2024
                                                                #endregion
                                                                drRoom["SupplierName"] = Supplier;
                                                                drRoom["RoomTypeName"] = RoomNamee;
                                                                drRoom["RoomTypeId"] = RoomTypeId;
                                                                //drRoom["JKGUIDID"] = JKGUIDID; //added new
                                                                //drRoom["MarkupappliedDts"] = MarkupappliedDts; // added on 06-08-2024 #49746
                                                                dtParsecRoom.Rows.Add(drRoom);
                                                                #endregion

                                                                #region cancellationpolicy
                                                                DateTime CancellationDate = DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", CultureInfo.InvariantCulture).Date;
                                                                double Penalty = Convert.ToDouble(Price);
                                                                double CompanyChargeAmount = Price;
                                                                double MainAgentCharge = Price;
                                                                double MainAgentCharge_wl = Price;
                                                                double CancelCharge_wl = Price;

                                                                DateTime MainDate = new DateTime();
                                                                DateTime StartDate = MainDate;
                                                                DateTime CancelDate = CancellationDate.Date.AddDays(-CancelDayAll);
                                                                DateTime NowDate = DateTime.Now.Date;
                                                                //int CutOffDays = -1;
                                                                double VatOnPolicy = 0;
                                                                string remark1 = "";

                                                                if (CancelDate.AddDays(CutOffDays).Date > DateTime.Now.Date)
                                                                {
                                                                    CancelDate = CancelDate.AddDays(CutOffDays).Date;
                                                                }
                                                                else
                                                                {
                                                                    CancelDate = DateTime.Now.Date;
                                                                }
                                                                if (NowDate < CancelDate)
                                                                {


                                                                    double zero = 0;
                                                                    dtParsecCacelationPolicy.Rows.Add(_commonMethod.setCancellationPolicyRow(ref zero, dtParsecCacelationPolicy.NewRow(), CurrencyFactorhyperguest, currency, Supplier, Convert.ToString(OwnsystemHotelId), Convert.ToString(1), Convert.ToString(RoomTypeId), RoomNamee, board, Convert.ToDateTime(NowDate), CancelDate.AddDays(-1), 0, 0, "0", VatOnPolicy, uniqid, "NO", "", remark1 != string.Empty ? remark1 : "No Essential Information Available", "", ""));

                                                                    //dtParsecCacelationPolicy.Rows.Add(Common.setCancellationPolicyRow(0, 0, ref zero, dtParsecCacelationPolicy.NewRow(), CurrencyFactorhyperguest, currency, Supplier, OwnsystemHotelId, Convert.ToString(1), Convert.ToString(RoomTypeId), RoomNamee, board, Convert.ToDateTime(NowDate), CancelDate.AddDays(-1), 0, 0, "0", VatOnPolicy, uniqid, "NO", "", remark1 != string.Empty ? remark1 : "No Essential Information Available", "", ""));
                                                                    dtParsecCacelationPolicy.AcceptChanges();

                                                                    dtParsecCacelationPolicy.Rows.Add(_commonMethod.setCancellationPolicyRow(ref MainAgentCharge_wl, dtParsecCacelationPolicy.NewRow(), CurrencyFactorhyperguest, currency, Supplier, Convert.ToString(OwnsystemHotelId), Convert.ToString(1),
                                                                                         Convert.ToString(RoomTypeId), RoomNamee, board, Convert.ToDateTime(CancelDate), DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null), MainAgentCharge, Penalty, "0", VatOnPolicy, uniqid, "NO", "", remark1 != string.Empty ? remark1 : "No Essential Information Available", "", ""));
                                                                    dtParsecCacelationPolicy.AcceptChanges();
                                                                }
                                                                else
                                                                {
                                                                    // dtParsecCacelationPolicy.Rows.Add(Common.setCancellationPolicyRow(MainAgentCharge_wl, CancelCharge_wl, ref CompanyChargeAmount, dtParsecCacelationPolicy.NewRow(), CurrencyFactorhyperguest, currency, Supplier, OwnsystemHotelId, Convert.ToString(1), Convert.ToString(RoomTypeId), RoomNamee, board, NowDate, DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null), MainAgentCharge, Penalty, "0", VatOnPolicy, uniqid, "Yes", "", remark1 != string.Empty ? remark1 : "No Essential Information Available"));
                                                                    dtParsecCacelationPolicy.Rows.Add(_commonMethod.setCancellationPolicyRow(ref MainAgentCharge_wl, dtParsecCacelationPolicy.NewRow(), CurrencyFactorhyperguest, currency, Supplier, Convert.ToString(OwnsystemHotelId), Convert.ToString(1), Convert.ToString(RoomTypeId), RoomNamee, board, NowDate, DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null), MainAgentCharge, Penalty, "0", VatOnPolicy, uniqid, "Yes", "", remark1 != string.Empty ? remark1 : "No Essential Information Available"));

                                                                    dtParsecCacelationPolicy.AcceptChanges();
                                                                }
                                                                #endregion
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }

                                }
                                Options ops = (await _supplierRes.GetCommonRoomForSuppliers(GiataId, commonSearch.tokenData, commonSearch.advancedOptions, MinPrice, drXmlHotel, dtParsecRoom, dtParsecCacelationPolicy, dtParsecRoomRateBreakup, SearchParam));
                                if (ops != null)
                                    options.Add(ops);
                                // here
                                #endregion

                            }
                            #endregion
                        }
                        else
                        {
                            continue;
                        }

                    }
                }
                return options;
            }
            catch (Exception e)
            {
                Item.ResponseStatus = 404;
                Item.ResponseType = "NoCount";
                await _exception.Error(e);
            }
            finally
            {
                #region ResProcessTimer
                try
                {
                    timerroomx.Stop();
                    Item.CodeProcessingTimeSupplier = timerroomx.ElapsedMilliseconds;
                    //if (SearchParam != null && ResProccessTimerLogActive == "1" && timerroomx.ElapsedMilliseconds > Convert.ToInt32(ResProccessTimeValueforSaveLog))
                    {
                        
                        SearchParam.SupplierTime += ",|Finish:hyperguest Process Response  " + "|Time :" + timerroomx.Elapsed.ToString();
                        //Task.Run(() => FireAway_SQL_ResPorcessTimeLog("", "", "hyperguest", SearchParam.SearchKey, Convert.ToInt32(timerroomx.Elapsed.ToString()), 1));
                    }
                }
                catch (Exception x)
                {
                    await _exception.Error(x);
                }
                #endregion
            }
            return options;
        }
        //prebook

        public async Task<PriceCheckDto> hyperguestPriceCheck(DataSet dsCartDetails, DataRow[] hyperguestDetails, int j, double FinalPrice, double CurrencyFactorhyperguest, string UniqueNo, string AgentUniqueNo, double ResPrice, string SupplierName, int i, double CompanyBuyingPrice, int AgentId,TokenData tokenData)
        {
            PriceCheckDto priceCheckDto = new PriceCheckDto();
            string CancellationPolicyRQ = "";
            string responsetype = "Success";
            string ResponseData = "";
            string time = "";
            string Error = "";
            string request = "";
            try
            {
                string username = Convert.ToString(hyperguestDetails[0]["UserName"]);
                string password = Convert.ToString(hyperguestDetails[0]["Password"]);
                string roomtypeid = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomTypeId"]);
                string CheckIn = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["CheckInDate"]);
                CheckIn = CheckIn.Substring(0, 4) + "-" + CheckIn.Substring(4, 2) + "-" + CheckIn.Substring(6, 2);
                string CheckOut = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["CheckOutDate"]);
                CheckOut = CheckOut.Substring(0, 4) + "-" + CheckOut.Substring(4, 2) + "-" + CheckOut.Substring(6, 2);

                string BookUrl = Convert.ToString(HyperguestInfo.BookingURL);//"http://booking-node.server/booking/create";
                string URL = BookUrl + "booking/pre-book";

                request = hyperguestPriceCheckRequest(roomtypeid, j, CheckIn, CheckOut, dsCartDetails);

                _commonMethod.ThirdPartyBookingHistory(Convert.ToString(UniqueNo), Convert.ToString(AgentUniqueNo), request, "-", "Before Block Request hit Price check", Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["SupplierName"]), "Request Confirm j=" + j + "", "-", "WithinEarth");

                System.Diagnostics.Stopwatch timerroomprocess = new System.Diagnostics.Stopwatch();
                timerroomprocess.Start();

                ResponseDto responseDto = await GethyperguestResponse(request, URL, password, 120000);
                ResponseData = responseDto.Response;
                Error = responseDto.Error;

                _commonMethod.ThirdPartyBookingHistory(Convert.ToString(UniqueNo), Convert.ToString(AgentUniqueNo), request, ResponseData + Error, "After Block Request hit Price check", Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["SupplierName"]), "Request Confirm j=" + j + "", "-", "WithinEarth");

                timerroomprocess.Stop();
                time = timerroomprocess.Elapsed.ToString();

                if (ResponseData == "" || ResponseData == null)
                {
                    try
                    {
                        responsetype = "GeneralError";
                        string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, request, ResponseData, Error);//"purchaseToken"
                    }
                    catch (Exception e)
                    {
                        _ = _exception.Error(e);
                    }

                    priceCheckDto.Flag = 2;
                    return priceCheckDto;
                }
                if (ResponseData.ToLower().Contains("errorCode")) //#09-OCT-2024 K2
                {
                    FinalPrice = 0;

                    priceCheckDto.Flag = 0;
                    priceCheckDto.FinalPrice = FinalPrice;
                    return priceCheckDto;
                }
                if (ResponseData.ToLower().Contains("error")) //#04-NOV-2024 K2
                {
                    FinalPrice = 0;
                    priceCheckDto.Flag = 0;
                    priceCheckDto.FinalPrice = FinalPrice;
                    return priceCheckDto;
                }
                hyperguest.PriceCheckResp.Root DS = new hyperguest.PriceCheckResp.Root();
                try
                {
                    DS = Newtonsoft.Json.JsonConvert.DeserializeObject<hyperguest.PriceCheckResp.Root>(ResponseData);
                }
                catch (Exception ex)
                {
                    FinalPrice = 0;
                    _ = _exception.Error(ex);
                    
                    priceCheckDto.Flag = 0;
                    priceCheckDto.FinalPrice = FinalPrice;
                    return priceCheckDto;
                }
                if (DS.content == null) //#09-OCT-2024 K2
                {
                    FinalPrice = 0;

                    priceCheckDto.Flag = 0;
                    priceCheckDto.FinalPrice = FinalPrice;
                    return priceCheckDto;
                }
                if (ResponseData.Contains("priceChange") == false)
                {
                    FinalPrice = CompanyBuyingPrice;
                    priceCheckDto.Flag = 0;
                    priceCheckDto.FinalPrice = FinalPrice;
                    return priceCheckDto;
                }
                else
                {
                    int adultcount = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[0]["NoOfAdults"]);
                    int childage1 = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[0]["ChildAge1"]);
                    int childage2 = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[0]["ChildAge2"]);
                    var NoOfChild = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[0]["NoOfChild"]);

                    string[] spitid1 = roomtypeid.Split(new String[] { "###" }, StringSplitOptions.None);

                    double Finalprice1 = Convert.ToDouble(DS.content.rooms[0].priceChange.toAmount.amount);
                    string currency = Convert.ToString(DS.content.rooms[0].priceChange.toAmount.currency);
                    double CurrencyFactor = 1;
                    if (currency.ToLower() != tokenData.SiteCurrency.ToLower())
                    {
                        CurrencyFactor = _commonMethod.GetROEByCurrency(currency, tokenData.dtROE);
                    }

                    FinalPrice = Finalprice1 * CurrencyFactor;
                    priceCheckDto.FinalPrice = FinalPrice;

                    if (Math.Round(CompanyBuyingPrice, 2) < Math.Round(FinalPrice, 2))
                    {
                        try
                        {
                            ResponseData = "#Price Changed " + "#Main Price = " + CompanyBuyingPrice + " #New Price = " + FinalPrice + " #Response= " + ResponseData;
                            responsetype = "GeneralError";
                            string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, request, ResponseData, Error);//"purchaseToken"
                        }
                        catch (Exception e)
                        {
                            _ = _exception.Error(e);
                        }
                        priceCheckDto.Flag = 1;
                        return priceCheckDto;
                    }
                }
            }
            catch (Exception ex)
            {
                try
                {
                    FinalPrice = 0;
                    Error = ex.Message + ex.StackTrace.ToString();
                    responsetype = "GeneralError";
                    string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, request, ResponseData, Error);//"purchaseToken"
                }
                catch (Exception e)
                {
                    _ = _exception.Error(ex);
                }
                _ = _exception.Error(ex);

                priceCheckDto.FinalPrice = FinalPrice;
                priceCheckDto.Flag = 1;
                return priceCheckDto;
            }
            try
            {
                responsetype = "Success";
                string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, request, ResponseData, Error); //"purchaseToken"
            }
            catch (Exception e)
            {
                _ = _exception.Error(e);
            }
            priceCheckDto.Flag = 0;
            return priceCheckDto;
        }

        public async Task<PriceCheckDto> hyperguestPriceCheck_new(DataSet dsCartDetails, DataRow[] hyperguestDetails, int j, double FinalPrice, string UniqueNo, string AgentUniqueNo, double ResPrice, string SupplierName, int i, double CompanyBuyingPrice, int AgentId, TokenData tokenData, string Searchkey = "", string TechName = "")
        {
            PriceCheckDto priceCheckDto = new PriceCheckDto();
            string CancellationPolicyRQ = "";
            string responsetype = "Success";
            string ResponseData = "";
            string time = "";
            string Error = "";
            string request = "";
            double Finalprice1 = 0;
            string currency = "";
            double CurrencyFactor = 1;
            int srphotelid = 0;
            try
            {
                srphotelid = Convert.ToInt32(dsCartDetails.Tables[0].Rows[0]["ServiceId"]);
            }
            catch (Exception ex)
            {
                _ = _exception.Error(ex, Searchkey);
            }
            try
            {
                string username = Convert.ToString(hyperguestDetails[0]["UserName"]);
                string password = Convert.ToString(hyperguestDetails[0]["Password"]);
                string roomtypeid = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomTypeId"]);
                string CheckIn = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["CheckInDate"]);
                CheckIn = CheckIn.Substring(0, 4) + "-" + CheckIn.Substring(4, 2) + "-" + CheckIn.Substring(6, 2);
                string CheckOut = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["CheckOutDate"]);
                CheckOut = CheckOut.Substring(0, 4) + "-" + CheckOut.Substring(4, 2) + "-" + CheckOut.Substring(6, 2);

                string BookUrl = _supplierConfig.Hyperguest.HyperguestBookingURL;//"http://booking-node.server/booking/create";
                string URL = BookUrl + _supplierConfig.Hyperguest.Prebook_book;

                request = hyperguestPriceCheckRequest_new(roomtypeid, j, CheckIn, CheckOut, dsCartDetails);

                _commonMethod.ThirdPartyBookingHistory(Convert.ToString(UniqueNo), Convert.ToString(AgentUniqueNo), request, "-", "Before Block Request hit Price check", Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["SupplierName"]), "Request Confirm j=" + j + "", Searchkey, "WithinEarth");

                System.Diagnostics.Stopwatch timerroomprocess = new System.Diagnostics.Stopwatch();
                timerroomprocess.Start();

                ResponseDto responseDto = await GethyperguestResponse(request, URL, password, 120000);
                ResponseData = responseDto.Response;
                Error = responseDto.Error;

                _commonMethod.ThirdPartyBookingHistory(Convert.ToString(UniqueNo), Convert.ToString(AgentUniqueNo), request, ResponseData + Error, "After Block Request hit Price check", Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["SupplierName"]), "Request Confirm j=" + j + "", Searchkey, "WithinEarth");

                timerroomprocess.Stop();
                time = timerroomprocess.Elapsed.ToString();

                if (ResponseData == "" || ResponseData == null)
                {
                    try
                    {
                        responsetype = "GeneralError";
                        string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, request, ResponseData, Error,Searchkey,TechName,srphotelid);//"purchaseToken"
                    }
                    catch (Exception e)
                    {
                        _ = _exception.Error(e);
                    }

                    priceCheckDto.Flag = 2;
                    return priceCheckDto;
                }
                if (ResponseData.ToLower().Contains("errorCode")) //#09-OCT-2024 K2
                {
                    FinalPrice = 0;

                    priceCheckDto.Flag = 0;
                    priceCheckDto.FinalPrice = FinalPrice;
                    return priceCheckDto;
                }
                if (ResponseData.ToLower().Contains("error")) //#04-NOV-2024 K2
                {
                    FinalPrice = 0;
                    priceCheckDto.Flag = 0;
                    priceCheckDto.FinalPrice = FinalPrice;
                    return priceCheckDto;
                }
                hyperguest.PriceCheckRespNew.Root DS = new hyperguest.PriceCheckRespNew.Root();
                try
                {
                    DS = Newtonsoft.Json.JsonConvert.DeserializeObject<hyperguest.PriceCheckRespNew.Root>(ResponseData);
                }
                catch (Exception ex)
                {
                    FinalPrice = 0;
                    _ = _exception.Error(ex);

                    priceCheckDto.Flag = 0;
                    priceCheckDto.FinalPrice = FinalPrice;
                    return priceCheckDto;
                }
                if (DS.content == null) //#09-OCT-2024 K2
                {
                    FinalPrice = 0;

                    priceCheckDto.Flag = 0;
                    priceCheckDto.FinalPrice = FinalPrice;
                    return priceCheckDto;
                }
                if (ResponseData.Contains("priceChange") == false)
                {
                    FinalPrice = CompanyBuyingPrice;
                    priceCheckDto.Flag = 0;
                    priceCheckDto.FinalPrice = FinalPrice;
                    return priceCheckDto;
                }
                else
                {
                    int adultcount = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[0]["NoOfAdults"]);
                    int childage1 = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[0]["ChildAge1"]);
                    int childage2 = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[0]["ChildAge2"]);
                    int childage3 = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[0]["ChildAge3"]);
                    int childage4 = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[0]["ChildAge4"]);
                    var NoOfChild = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[0]["NoOfChild"]);

                    string[] spitid1 = roomtypeid.Split(new String[] { "###" }, StringSplitOptions.None);
                    string RSpriceNew = "";
                    for (int t = 0; t < DS.content.paymentOptions.Count; t++)
                    {
                        if (DS.content.paymentOptions[t].priceChange != null)
                        {
                            Finalprice1 = Convert.ToDouble(DS.content.paymentOptions[t].priceChange.toAmount.amount);
                            currency = Convert.ToString(DS.content.paymentOptions[t].priceChange.toAmount.currency);
                        }
                        else
                        {
                            Finalprice1 = Convert.ToDouble(DS.content.rooms[t].prices.net.price);
                            currency = Convert.ToString(DS.content.rooms[t].prices.net.currency);
                        }

                        RSpriceNew = RSpriceNew + Convert.ToString(Finalprice1) + "$$$";
                        if (currency.ToLower() != tokenData.SiteCurrency.ToLower())
                        {
                            CurrencyFactor = _commonMethod.GetROEByCurrency(currency, tokenData.dtROE);
                        }


                        ////added on 09-02-2024 #40209
                        //if (currency.ToLower() == "ars" || currency.ToLower() == "usd" || currency.ToLower() == "eur" || currency.ToLower() == "brl")
                        //{

                        //    if (currency.ToLower() == "ars")
                        //    {
                        //        currency = "AR$";
                        //    }
                        //    DataTable dtInrCurrency = Common.GetCountryCurrency(currency);
                        //    CurrencyFactor = Convert.ToDouble(dtInrCurrency.Rows[0]["AmtToExch"]);
                        //}
                        //else
                        //{
                        //    return 1;
                        //}
                        //end
                        Finalprice1 = Finalprice1 * CurrencyFactor;

                        FinalPrice = FinalPrice + Finalprice1;
                    }
                    // bool IsSuccess = Common.GetHasKeyUpdateStaus(RSpriceNew, Convert.ToInt32(userCartId));


                    if (Math.Round(CompanyBuyingPrice, 2) < Math.Round(FinalPrice, 2))
                    {
                        try
                        {
                            ResponseData = "#Price Changed " + "#Main Price = " + CompanyBuyingPrice + " #New Price = " + FinalPrice + " #Response= " + ResponseData;
                            responsetype = "GeneralError";
                            string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, request, ResponseData, Error, Searchkey, TechName, srphotelid);//"purchaseToken"
                        }
                        catch (Exception e)
                        {
                            _ = _exception.Error(e);
                        }
                        priceCheckDto.Flag = 1;
                        return priceCheckDto;
                    }
                }
            }
            catch (Exception ex)
            {
                try
                {
                    FinalPrice = 0;
                    Error = ex.Message + ex.StackTrace.ToString();
                    responsetype = "GeneralError";
                    string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, request, ResponseData, Error, Searchkey, TechName, srphotelid);//"purchaseToken"
                }
                catch (Exception e)
                {
                    _ = _exception.Error(ex);
                }
                _ = _exception.Error(ex);

                priceCheckDto.FinalPrice = FinalPrice;
                priceCheckDto.Flag = 1;
                return priceCheckDto;
            }
            try
            {
                responsetype = "Success";
                string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, request, ResponseData, Error, Searchkey, TechName, srphotelid); //"purchaseToken"
            }
            catch (Exception e)
            {
                _ = _exception.Error(e);
            }
            priceCheckDto.Flag = 0;
            return priceCheckDto;
        }

        public string hyperguestPriceCheckRequest(string roomtypeid, int j, string checkIn, string checkout, DataSet dsCartDetails)
        {
            string RequestString = "";
            try
            {
                string[] spitid1 = roomtypeid.Split(new String[] { "###" }, StringSplitOptions.None);
                double expectedPrice = Convert.ToDouble(spitid1[3]);
                string currency = spitid1[4];

                int adultcount = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[j]["NoOfAdults"]);
                int childage1 = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[0]["ChildAge1"]);
                int childage2 = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[0]["ChildAge2"]);
                var NoOfChild = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[0]["NoOfChild"]);


                List<hyperguest.Pricecheck.Pax> objNewPax = new List<hyperguest.Pricecheck.Pax>();
                hyperguest.Pricecheck.Pax objpax = new hyperguest.Pricecheck.Pax();


                //paxs.adults = adultcount;


                //string chld = "[]";
                //List<hyperguest.Pricecheck.Pax> AddChild = new List<hyperguest.Pricecheck.Pax>();

                List<int> t = new List<int>();
                switch (NoOfChild)
                {
                    case 1:
                        t.Add(childage1);
                        break;
                    case 2:
                        t.Add(childage1);
                        t.Add(childage2);
                        //chld = "[" + childage1 + "," + childage2 + "]";
                        break;

                }


                hyperguest.Pricecheck.Root hr = new hyperguest.Pricecheck.Root();
                //hr.Root hh = new hr.Root();
                List<hyperguest.Pricecheck.Pax> paxlist = new List<hyperguest.Pricecheck.Pax>();
                hyperguest.Pricecheck.Pax paxs = new hyperguest.Pricecheck.Pax();

                //List<hr.Pax> pax = new List<hr.Pax>();

                //paxlist. c = new hr.Pax();
                paxs.adults = adultcount;
                paxs.children = t;
                paxlist.Add(paxs);
                //List<object> ChilderAge1 = new List<object>();
                //objpax.children = ChilderAge1;

                //hr.Add(paxlist);

                hyperguest.Pricecheck.ExpectedPrice objExpectedPrice = new hyperguest.Pricecheck.ExpectedPrice();
                objExpectedPrice.amount = expectedPrice;
                objExpectedPrice.currency = currency;

                List<hyperguest.Pricecheck.Room> ObjRoomNew = new List<hyperguest.Pricecheck.Room>();
                hyperguest.Pricecheck.Room objroomnew = new hyperguest.Pricecheck.Room();
                objroomnew.roomCode = spitid1[0];
                objroomnew.rateCode = spitid1[1];
                objroomnew.expectedPrice = objExpectedPrice;//.Add(objExpectedPrice);
                ObjRoomNew.Add(objroomnew);
                //objroomnew.expectedPrice = spitid1[3];


                List<hyperguest.Pricecheck.Dates> objDates = new List<hyperguest.Pricecheck.Dates>();
                hyperguest.Pricecheck.Dates objDateNew = new hyperguest.Pricecheck.Dates();
                objDateNew.from = checkIn;
                objDateNew.to = checkout;
                objDates.Add(objDateNew);

                List<hyperguest.Pricecheck.Search> objNewSearch = new List<hyperguest.Pricecheck.Search>();
                hyperguest.Pricecheck.Search objsearchNew = new hyperguest.Pricecheck.Search();
                objsearchNew.dates = objDateNew;
                objsearchNew.hotelId = Convert.ToInt32(spitid1[2]);
                objsearchNew.pax = paxlist;
                objNewSearch.Add(objsearchNew);


                hyperguest.Pricecheck.Root main = new hyperguest.Pricecheck.Root();
                main.search = objsearchNew;
                main.rooms = ObjRoomNew;


                //HotelTrader.HotelTraderRequest_Quote.Root main = new HotelTrader.HotelTraderRequest_Quote.Root();
                //main.Quote = QuoteList;// quote;

                RequestString = Newtonsoft.Json.JsonConvert.SerializeObject(main);
                return RequestString;
            }
            catch (Exception ex)
            {
                _ = _exception.Error(ex);

            }
            return RequestString;
        }
        public string hyperguestPriceCheckRequest_new(string roomtypeid, int j, string checkIn, string checkout, DataSet dsCartDetails)
        {
            string RequestString = "";
            try
            {
                string[] spitid1 = roomtypeid.Split(new String[] { "###" }, StringSplitOptions.None);
                string[] spitid2 = spitid1[3].Split(new String[] { "$$$" }, StringSplitOptions.None);
                string currency = spitid1[4];

                if (currency == "AR$")
                {
                    currency = "ARS";
                }

                string RatePlanId = spitid1[7];
                int RoomId = Convert.ToInt32(spitid1[8]);
                string Nationality = spitid1[5];
                StringBuilder RQ = new StringBuilder();

                RQ.Append("{");
                RQ.Append("\"search\": {");
                RQ.Append(" \"dates\": {");
                RQ.Append("\"from\":\"" + checkIn + "\",");
                RQ.Append("\"to\":\"" + checkout + "\"");
                RQ.Append("},");
                RQ.Append("\"propertyId\":\"" + spitid1[2] + "\",");
                RQ.Append("\"nationality\":\"" + Nationality + "\",");
                RQ.Append("\"pax\": [");
                for (int a = 0; a < spitid2.Length - 1; a++)
                {
                    int adultcount = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[a]["NoOfAdults"]);
                    int childage1 = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[a]["ChildAge1"]);
                    int childage2 = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[a]["ChildAge2"]);

                    var NoOfChild = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[a]["NoOfChild"]);

                    if (a == 0)
                        RQ.Append("{");
                    else
                        RQ.Append(",{");

                    RQ.Append(" \"adults\":" + adultcount + ",");
                    if (NoOfChild > 0)
                    {

                        if (NoOfChild == 1)
                        {
                            RQ.Append("\"children\":[" + childage1 + "]");
                        }
                        if (NoOfChild == 2)
                        {
                            RQ.Append("\"children\":[" + childage1 + "," + childage2 + "]");
                        }

                    }
                    else
                    {
                        RQ.Append("\"children\":[]");
                    }
                    RQ.Append("}");

                }
                RQ.Append("]");
                RQ.Append("},");
                RQ.Append("\"rooms\": [");
                for (int b = 0; b < spitid2.Length - 1; b++)
                {
                    if (b == 0)
                        RQ.Append("{");
                    else
                        RQ.Append(",{");

                    RQ.Append("\"roomCode\":\"" + spitid1[0] + "\",");
                    RQ.Append("\"roomId\":\"" + RoomId + "\",");
                    RQ.Append("\"rateCode\":\"" + spitid1[1] + "\",");
                    RQ.Append("\"ratePlanId\":\"" + RatePlanId + "\",");
                    RQ.Append("\"expectedPrice\": {");
                    RQ.Append("\"amount\":" + spitid2[b] + ",");
                    RQ.Append("\"currency\":\"" + currency + "\"");
                    RQ.Append("}");
                    RQ.Append("}");
                }

                RQ.Append("]");
                RQ.Append("}");

                RequestString = RQ.ToString();

            }
            catch (Exception ex)
            {
                _ = _exception.Error(ex);

            }
            return RequestString;
        }
        public string hyperguestBookingRequest(DataSet dsCartDetails, DataRow[] hyperguestDetails, int i, int j, double resPrice, DataTable dtPassengers, ref string AgtRef, string CheckIn, string CheckOut, int UniqueNo, string AgentCode ,bool IsPruvo)
        {
            string RequestString = "";
            try
            {

                //string currency = Common.hyperguest.hyperguest_Currency;
                string roomtypeid = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomTypeId"]);
                string[] spitid1 = roomtypeid.Split(new String[] { "###" }, StringSplitOptions.None);

                string rateCode = spitid1[1];
                string roomCode = spitid1[0];
                double expectedPrice = Convert.ToDouble(spitid1[3]);
                string currency = spitid1[4];
                string XmlNationality = spitid1[5];

                //string HotelCode = Convert.ToString(dsCartDetails.Tables[0].DefaultView[i]["XMLHotelId"]);
                string HotelCode = Convert.ToString(dsCartDetails.Tables[0].DefaultView[i]["XMLHotelId"]);

                string ExternalReference = "Book_" + Convert.ToString(AgentCode + "_" + UniqueNo);

                hyperguest.hyperguestBookingReq.LeadGuest objLeadPaxName = new hyperguest.hyperguestBookingReq.LeadGuest();

                List<hyperguest.hyperguestBookingReq.Guest> objGuests = new List<hyperguest.hyperguestBookingReq.Guest>();

                hyperguest.hyperguestBookingReq.Contact objcontect = new hyperguest.hyperguestBookingReq.Contact();
                objcontect.address = "N/A";
                objcontect.city = "N/A";
                objcontect.email = "sup@withinearth.com";
                //objcontect.email = "Chintan@technoheaven.net";
                objcontect.phone = "N/A";
                objcontect.state = "N/A";
                objcontect.zip = "N/A";
                objcontect.country = XmlNationality;

                string isLead = string.Empty;
                dtPassengers.DefaultView.RowFilter = "PickUp='" + roomtypeid + "' and RoomNo='" + Convert.ToString(dsCartDetails.Tables[0].DefaultView[i]["RoomNumber"]) + "' and IsLead=1";
                if (dtPassengers.DefaultView.Count == 0)
                    isLead = "true";
                dtPassengers.DefaultView.RowFilter = "";
                dtPassengers.DefaultView.RowFilter = "PickUp='" + roomtypeid + "' and RoomNo='" + Convert.ToString(dsCartDetails.Tables[0].DefaultView[i]["RoomNumber"]) + "' and UserCartId='" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["UserCartId"]) + "'";
                dtPassengers.DefaultView.Sort = "IsLead desc";
                if (dtPassengers.DefaultView.Count > 0)
                {
                    for (int k = 0; k < dtPassengers.DefaultView.Count; k++)
                    {
                        DataRow[] drPassengers = dtPassengers.DefaultView.ToTable().Select("IsLead='1'");
                        if (drPassengers.Length == 0)
                        {
                            if (Convert.ToString(dtPassengers.DefaultView[k]["PaxType"]).Trim().ToLower() == "adult")
                                dtPassengers.DefaultView[k]["IsLead"] = 1;
                        }
                        if (Convert.ToInt16(dtPassengers.DefaultView[k]["IsLead"]) == 1)
                        {
                            hyperguest.hyperguestBookingReq.Name objLeadGuestName = new hyperguest.hyperguestBookingReq.Name();
                            objLeadGuestName.first = Convert.ToString(dtPassengers.DefaultView[k]["FirstName"]).Trim();
                            objLeadGuestName.last = Convert.ToString(dtPassengers.DefaultView[k]["LastName"]).Trim();
                            objLeadPaxName.title = Convert.ToString(dtPassengers.DefaultView[k]["Prefix"]).Trim().Replace(".", "");
                            objLeadPaxName.name = objLeadGuestName;

                            string BirthDay1 = Convert.ToString(DateTime.Now.Date.AddYears(-30));
                            DateTime Dt1 = (Convert.ToDateTime(BirthDay1));
                            string TD1 = Dt1.ToString("yyyy-MM-dd");
                            objLeadPaxName.birthDate = Convert.ToString(TD1); //"1990-12-24";
                            objLeadPaxName.contact = objcontect;
                        }
                        //else
                        {
                            hyperguest.hyperguestBookingReq.Guest objGuestNew = new hyperguest.hyperguestBookingReq.Guest();
                            hyperguest.hyperguestBookingReq.Name objguests = new hyperguest.hyperguestBookingReq.Name();
                            objguests.first = Convert.ToString(dtPassengers.DefaultView[k]["FirstName"]).Trim();
                            objguests.last = Convert.ToString(dtPassengers.DefaultView[k]["LastName"]).Trim();
                            objGuestNew.title = Convert.ToString(dtPassengers.DefaultView[k]["Prefix"]).Trim().Replace(".", "");
                            objGuestNew.birthDate = "1990-12-24";
                            if (Convert.ToString(dtPassengers.DefaultView[k]["PaxType"]).Trim().ToLower() == "child")
                            {
                                int ChildAge = Convert.ToInt32(dtPassengers.DefaultView[k]["ChildAge"]);
                                string BirthDay = Convert.ToString(DateTime.Now.Date.AddYears(-ChildAge));
                                DateTime Dt = (Convert.ToDateTime(BirthDay));
                                string TD = Dt.ToString("yyyy-MM-dd");
                                objGuestNew.birthDate = Convert.ToString(TD);
                            }
                            else
                            {
                                string BirthDay = Convert.ToString(DateTime.Now.Date.AddYears(-30));
                                DateTime Dt = (Convert.ToDateTime(BirthDay));
                                string TD = Dt.ToString("yyyy-MM-dd");
                                //objLeadPaxName.birthDate = Convert.ToString(TD);
                                objGuestNew.birthDate = Convert.ToString(TD);
                            }
                            objGuestNew.name = objguests;
                            if (k == 0)
                                objGuestNew.contact = objcontect;
                            //objGuestNew.contact = objcontect;
                            objGuests.Add(objGuestNew);
                        }

                    }
                }

                hyperguest.hyperguestBookingReq.ExpectedPrice objExpectedPrice = new hyperguest.hyperguestBookingReq.ExpectedPrice();

                objExpectedPrice.amount = expectedPrice;
                objExpectedPrice.currency = currency;

                List<hyperguest.hyperguestBookingReq.Room> objRoom = new List<hyperguest.hyperguestBookingReq.Room>();
                hyperguest.hyperguestBookingReq.Room objRoomNew = new hyperguest.hyperguestBookingReq.Room();

                objRoomNew.rateCode = rateCode;
                objRoomNew.roomCode = roomCode;
                objRoomNew.expectedPrice = objExpectedPrice;
                objRoomNew.guests = objGuests;
                objRoom.Add(objRoomNew);


                #region APISO VC
                int usercartdetailsid = Convert.ToInt32(dsCartDetails.Tables[0].DefaultView[i]["UserCartDetailId"]); 
                string Pan = "";
                string CVV = "0", Expiry = "0";

                DataTable dtbookingdata = _commonMethod.GetApisoBookingData(usercartdetailsid);
                if (dtbookingdata != null)
                {
                    _vcn.GetApisoServiceDatatoCreateVCN("", Convert.ToInt32(dtbookingdata.Rows[0]["OnlineBookingMasterId"]), Convert.ToInt32(dtbookingdata.Rows[0]["OnlineBookingDetailsId"]), ref Pan, ref CVV, ref Expiry, IsPruvo);
                    
                }
                #endregion


                string ExpiryRegex = Convert.ToString(Expiry).Substring(2, 2);
                string ExpiryRegexFisrt = Convert.ToString(Expiry).Substring(0, 2);

                hyperguest.hyperguestBookingReq.Name objName = new hyperguest.hyperguestBookingReq.Name();
                objName.first = "Within Earth";
                objName.last = "WithinEarth";

                hyperguest.hyperguestBookingReq.Expiry objExpiry = new hyperguest.hyperguestBookingReq.Expiry();
                objExpiry.month = ExpiryRegex;
                objExpiry.year = "20" + ExpiryRegexFisrt;
                //objExpiry.month = "12";
                //objExpiry.year = "2025";

                hyperguest.hyperguestBookingReq.CreditCard objCreditCard = new hyperguest.hyperguestBookingReq.CreditCard();
                //objCreditCard.cvv = "123";
                //objCreditCard.expiry = objExpiry;
                //objCreditCard.name = objName;
                //objCreditCard.number = "4111111111111111";
                objCreditCard.cvv = CVV;
                objCreditCard.expiry = objExpiry;
                objCreditCard.name = objName;
                objCreditCard.number = Pan;

                hyperguest.hyperguestBookingReq.Reference objReference = new hyperguest.hyperguestBookingReq.Reference();
                objReference.agency = ExternalReference;

                hyperguest.hyperguestBookingReq.Dates objDates = new hyperguest.hyperguestBookingReq.Dates();
                objDates.from = CheckIn;
                objDates.to = CheckOut;

                hyperguest.hyperguestBookingReq.Root main = new hyperguest.hyperguestBookingReq.Root();
                main.dates = objDates;
                main.hotelId = Convert.ToInt32(HotelCode);
                //main.reference.agency = ExternalReference;
                main.reference = objReference;
                main.creditCard = objCreditCard;
                main.leadGuest = objLeadPaxName;
                main.rooms = objRoom;

                //RequestString = JsonConvert.SerializeObject(main);


                JsonSerializerSettings settings = new JsonSerializerSettings();
                settings.NullValueHandling = NullValueHandling.Ignore;
                //settings.DefaultValueHandling = DefaultValueHandling.Ignore;
                RequestString = JsonConvert.SerializeObject(main, settings);
                return RequestString;


                

            }
            catch (Exception ex)
            {
                _ =_exception.ErrorNew(ex, ErrorLog._LogAPI.Book);
            }
            return RequestString;
        }


        public string hyperguestBookingRequest_new(BookRequest_newversion bookRequest, DataRow[] hyperguestDetails, int i, int j, double resPrice, DataTable dtPassengers, ref string AgtRef, string CheckIn, string CheckOut, int UniqueNo, string AgentCode, bool IsPruvo)
        {
            string RequestString = "";
            #region APISO VC
            //int usercartdetailsid = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[j]["UserCartDetailId"]);
            //string Pan = "";
            //string CVV = "0", Expiry = "0";
            //GetApisoDetail(usercartdetailsid, "", ref Pan, ref CVV, ref Expiry);

            //string ExpiryRegex = Convert.ToString(Expiry).Substring(2, 2);
            //string ExpiryRegexFisrt = Convert.ToString(Expiry).Substring(0, 2);
            #endregion
            try
            {



                string AllRoomPrice = Convert.ToString(bookRequest.userCart.UserCartDetails[j].EachoToken);//added by jay
                string[] TotalRoom = AllRoomPrice.Split(new String[] { "$$$" }, StringSplitOptions.None);

                string roomtypeid = Convert.ToString(bookRequest.userCart.UserCartDetails[j].RoomTypeId);
                string[] spitid1 = roomtypeid.Split(new String[] { "###" }, StringSplitOptions.None);

                string rateCode = spitid1[1];
                string roomCode = spitid1[0];

                string currency = spitid1[4];

                if (currency == "AR$")
                {
                    currency = "ARS";
                }



                string XmlNationality = spitid1[5];

                string HotelCode = spitid1[2]; //Convert.ToString(bookRequest.userCart.XMLHotelId);
                //string ExternalReference = "Book_" + Convert.ToString(Common.getSession("AgentCode") + "_" + UniqueNo);
                string ExternalReference = "Book_" + Convert.ToString(AgentCode + "_" + UniqueNo);

                hyperguest.hyperguestBookingReq.LeadGuest objLeadPaxNameCommon = new hyperguest.hyperguestBookingReq.LeadGuest();
                hyperguest.hyperguestBookingReq.LeadGuest objLeadPaxName = new hyperguest.hyperguestBookingReq.LeadGuest();


                List<hyperguest.hyperguestBookingReq.Room> Rooms = new List<hyperguest.hyperguestBookingReq.Room>();
                for (int a = 0; a < TotalRoom.Length - 1; a++)
                {
                    List<hyperguest.hyperguestBookingReq.Guest> objGuests = new List<hyperguest.hyperguestBookingReq.Guest>();
                    hyperguest.hyperguestBookingReq.Contact objcontect = new hyperguest.hyperguestBookingReq.Contact();
                    objcontect.address = "N/A";
                    objcontect.city = "N/A";
                    objcontect.email = "reservas@nites.travel";
                    objcontect.phone = "N/A";
                    objcontect.state = "N/A";
                    objcontect.zip = "N/A";
                    objcontect.country = XmlNationality;

                    int RoomNo = a + 1;
                    string isLead = string.Empty;
                    dtPassengers.DefaultView.RowFilter = "PickUp='" + Convert.ToString(bookRequest.userCart.UserCartDetails[j].RoomTypeId) + "' and RoomNo='" + Convert.ToString(RoomNo) + "' and IsLead=1";
                    if (dtPassengers.DefaultView.Count == 0)
                        isLead = "true";
                    dtPassengers.DefaultView.RowFilter = "";
                    dtPassengers.DefaultView.RowFilter = "PickUp='" + Convert.ToString(bookRequest.userCart.UserCartDetails[j].RoomTypeId) + "' and RoomNo='" + Convert.ToString(RoomNo) + "' and UserCartId='" + bookRequest.userCart.UserCartId + "'";
                    dtPassengers.DefaultView.Sort = "IsLead desc";
                    if (dtPassengers.DefaultView.Count > 0)
                    {
                        for (int k = 0; k < dtPassengers.DefaultView.Count; k++)
                        {
                            DataRow[] drPassengers = dtPassengers.DefaultView.ToTable().Select("IsLead='1'");
                            if (drPassengers.Length == 0)
                            {
                                if (Convert.ToString(dtPassengers.DefaultView[k]["PaxType"]).Trim().ToLower() == "adult")
                                    dtPassengers.DefaultView[k]["IsLead"] = 1;
                            }
                            if (Convert.ToInt16(dtPassengers.DefaultView[k]["IsLead"]) == 1)
                            {
                                hyperguest.hyperguestBookingReq.Name objLeadGuestName = new hyperguest.hyperguestBookingReq.Name();
                                objLeadGuestName.first = Convert.ToString(dtPassengers.DefaultView[k]["FirstName"]).Trim();
                                objLeadGuestName.last = Convert.ToString(dtPassengers.DefaultView[k]["LastName"]).Trim();
                                objLeadPaxName.title = Convert.ToString(dtPassengers.DefaultView[k]["Prefix"]).Trim().Replace(".", "");
                                objLeadPaxName.name = objLeadGuestName;

                                string BirthDay1 = Convert.ToString(DateTime.Now.Date.AddYears(-30));
                                DateTime Dt1 = (Convert.ToDateTime(BirthDay1));
                                string TD1 = Dt1.ToString("yyyy-MM-dd");
                                objLeadPaxName.birthDate = Convert.ToString(TD1); //"1990-12-24";
                                objLeadPaxName.contact = objcontect;

                                if (RoomNo == 1)
                                {
                                    objLeadPaxNameCommon.title = Convert.ToString(dtPassengers.DefaultView[k]["Prefix"]).Trim().Replace(".", "");
                                    objLeadPaxNameCommon.name = objLeadGuestName;
                                    objLeadPaxNameCommon.birthDate = Convert.ToString(TD1); //"1990-12-24";
                                    objLeadPaxNameCommon.contact = objcontect;
                                }
                            }
                            //else
                            {
                                hyperguest.hyperguestBookingReq.Guest objGuestNew = new hyperguest.hyperguestBookingReq.Guest();
                                hyperguest.hyperguestBookingReq.Name objguests = new hyperguest.hyperguestBookingReq.Name();
                                objguests.first = Convert.ToString(dtPassengers.DefaultView[k]["FirstName"]).Trim();
                                objguests.last = Convert.ToString(dtPassengers.DefaultView[k]["LastName"]).Trim();
                                objGuestNew.title = Convert.ToString(dtPassengers.DefaultView[k]["Prefix"]).Trim().Replace(".", "");
                                objGuestNew.birthDate = "1990-12-24";
                                if (Convert.ToString(dtPassengers.DefaultView[k]["PaxType"]).Trim().ToLower() == "child")
                                {
                                    int ChildAge = Convert.ToInt32(dtPassengers.DefaultView[k]["ChildAge"]);
                                    string BirthDay = Convert.ToString(DateTime.Now.Date.AddYears(-ChildAge));
                                    DateTime Dt = (Convert.ToDateTime(BirthDay));
                                    string TD = Dt.ToString("yyyy-MM-dd");
                                    objGuestNew.birthDate = Convert.ToString(TD);
                                }
                                else
                                {
                                    string BirthDay = Convert.ToString(DateTime.Now.Date.AddYears(-30));
                                    DateTime Dt = (Convert.ToDateTime(BirthDay));
                                    string TD = Dt.ToString("yyyy-MM-dd");
                                    objGuestNew.birthDate = Convert.ToString(TD);
                                }
                                objGuestNew.name = objguests;
                                if (k == 0)
                                    objGuestNew.contact = objcontect;
                                objGuests.Add(objGuestNew);
                            }

                        }
                    }

                    hyperguest.hyperguestBookingReq.ExpectedPrice ExpectedPriceForRoom = new hyperguest.hyperguestBookingReq.ExpectedPrice();

                    ExpectedPriceForRoom.amount = Convert.ToDouble(TotalRoom[a]);
                    ExpectedPriceForRoom.currency = currency;

                    hyperguest.hyperguestBookingReq.Room Room = new hyperguest.hyperguestBookingReq.Room();
                    Room.rateCode = rateCode;
                    Room.roomCode = roomCode;
                    Room.expectedPrice = ExpectedPriceForRoom;
                    Room.guests = objGuests;
                    Rooms.Add(Room);

                }

                //hyperguest.hyperguestBookingReq.Name objName = new hyperguest.hyperguestBookingReq.Name();
                //objName.first = "Dwant Travel";
                //objName.last = "Dwant Travel";

                //hyperguest.hyperguestBookingReq.Expiry objExpiry = new hyperguest.hyperguestBookingReq.Expiry();
                //objExpiry.month = ExpiryRegex;
                //objExpiry.year = "20" + ExpiryRegexFisrt;

                //hyperguest.hyperguestBookingReq.CreditCard objCreditCard = new hyperguest.hyperguestBookingReq.CreditCard();
                //objCreditCard.cvv = CVV;  //Convert.ToString(ConfigurationManager.AppSettings["Hyperguestcraditcardcvv"]); //"123";
                //objCreditCard.expiry = objExpiry;
                //objCreditCard.name = objName;
                //objCreditCard.number = Pan; // Convert.ToString(ConfigurationManager.AppSettings["Hyperguestcraditcardnumber"]);

                hyperguest.hyperguestBookingReq.PaymentDetails objpaymantdetail = new hyperguest.hyperguestBookingReq.PaymentDetails();
                objpaymantdetail.type = "external";


                hyperguest.hyperguestBookingReq.Reference objReference = new hyperguest.hyperguestBookingReq.Reference();
                objReference.agency = ExternalReference;

                hyperguest.hyperguestBookingReq.Dates objDates = new hyperguest.hyperguestBookingReq.Dates();
                objDates.from = CheckIn;
                objDates.to = CheckOut;

                hyperguest.hyperguestBookingReq.Root main = new hyperguest.hyperguestBookingReq.Root();
                main.dates = objDates;
                main.propertyId = Convert.ToInt32(HotelCode);
                //main.reference.agency = ExternalReference;
                main.reference = objReference;
                //main.creditCard = objCreditCard;
                main.leadGuest = objLeadPaxNameCommon;
                main.rooms = Rooms;
                main.paymentDetails = objpaymantdetail;

                JsonSerializerSettings settings = new JsonSerializerSettings();
                settings.NullValueHandling = NullValueHandling.Ignore;
                RequestString = JsonConvert.SerializeObject(main, settings);
                return RequestString;

            }
            catch (Exception ex)
            {
                _ = _exception.Error(ex);
            }
            return RequestString;
        }

        public async Task<ResponseDto> GethyperguestResponse(string Request, string url, string Password, Int32 TimeOut = 180000)//Added on 7-JUN-2024 #Due to 45004
        {
            ResponseDto responseDto = new ResponseDto();
            string Error = "";
            string strOutput = "";
            System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
            timer.Start();
            try
            {

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "POST";
                request.Headers.Add("Authorization", "Bearer " + Password + "");
                request.Headers.Add("Accept-Encoding", "gzip,deflate");
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
                    HttpWebResponse Response = (HttpWebResponse)await request.GetResponseAsync();
                    Stream responseStream = Response.GetResponseStream();
                    if (Response.ContentEncoding == "gzip")
                    {

                        Stream st = Response.GetResponseStream();
                        if (Response.ContentEncoding.ToLower().Contains("gzip"))
                            st = new System.IO.Compression.GZipStream(st, System.IO.Compression.CompressionMode.Decompress);
                        else if (Response.ContentEncoding.ToLower().Contains("deflate"))
                            st = new System.IO.Compression.DeflateStream(st, System.IO.Compression.CompressionMode.Decompress);
                        StreamReader stIn = new StreamReader(st, Encoding.Default);
                        strOutput = await stIn.ReadToEndAsync();
                        stIn.Close();
                        st.Close();
                    }
                    else
                    {
                        StreamReader stIn = new StreamReader(Response.GetResponseStream());
                        strOutput = stIn.ReadToEnd();
                        stIn.Close();

                    }
                    //StreamReader Reader = new StreamReader(responseStream, Encoding.UTF8);
                    //strOutput = Reader.ReadToEnd();
                    //responseStream.Close();
                    ////Reader.Close();
                    //Response.Close();
                    //timer.Stop();
                }
                catch (WebException ex)
                {
                    _ = _exception.Error(ex);

                    if (ex.Response != null)
                    {
                        WebResponse errorResponse = ex.Response;
                        using (Stream responseStream = errorResponse.GetResponseStream())
                        {
                            StreamReader reader = new StreamReader(responseStream, Encoding.GetEncoding("utf-8"));
                            String errorText = reader.ReadToEnd();
                            strOutput = errorText;
                            Error = strOutput;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _ = _exception.Error(ex);
                Error = ex.Message.ToString();
                //ExceptionManager.LogException(ex);

                responseDto.Error = Error;
                return responseDto;
            }

            responseDto.Error = Error;
            responseDto.Response = strOutput;
            return responseDto;
        }

        public string Gethyperguest_BookingCancelRQ(string AgentVoucherNo, DataRow[] hyperguestDetails)
        {
            string RequestString = "";
            try
            {
                hyperguest.BookingCancelationReq.Root main = new hyperguest.BookingCancelationReq.Root();
                main.bookingId = Convert.ToInt32(AgentVoucherNo);
                main.reason = "N/A";
                main.simulation = false;

                RequestString = Newtonsoft.Json.JsonConvert.SerializeObject(main);
                return RequestString;
            }
            catch (Exception ex)
            {
                _ =_exception.ErrorNew(ex,ErrorLog._LogAPI.CancelBooking);
            }
            return RequestString;
        }
    }
}
