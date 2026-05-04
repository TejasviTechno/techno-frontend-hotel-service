using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using XConnect.CommonUtilities;
using XConnect.CommonUtilities.Interface;
using XConnect.ErrorLogHandler.Interface;
using XConnect.Markup.Interface;
using XConnect.Models;
using XConnect.Models.Hotel;
using XConnect.Supplier.Webhoteliter.Interface;
using static Azure.Core.HttpHeader;
using static XConnect.Models.ErrorLog;
using Options = XConnect.Models.Options;

namespace XConnect.Supplier.Webhoteliter
{
    public class WebhoteliterRepo : IWebhoteliter
    {
        private readonly ICommonMethod _commonMethod;
        private readonly IException _exception;
        private readonly IMarkup _markup;
        private readonly ISupplierRes _supplierRes;
        private readonly KeySettings _keySettings;
        private readonly IVCN _vcn;
        public WebhoteliterRepo(ICommonMethod commonMethod, IMarkup markup, ISupplierRes supplierRes, IException exception, IOptions<KeySettings> keySettings, IVCN vcn)
        {
            _commonMethod = commonMethod;
            _markup = markup;
            _supplierRes = supplierRes;
            _exception = exception;
            _keySettings = keySettings.Value;
            _vcn = vcn;
        }

        public void WebHotellierRequestOnly_async(DataRow[] commonCode, List<StarRating> listRating, HotelSearchParameter SearchPara, List<SupplierDetails> WebHotellierDetails, List<SupplierSearchConfig> WebHotellierSearchConfig, DataTable dtPreferredDataHotel, ref ConcurrentBag<HotelX> AllTask, string XMLCity, string XMLCountry, string XMLNationality, int Hit, DataTable dtROE, TokenData tokenData)
        {
            try
            {
                string Ids = string.Empty;
                string xmlbase = "";
                _commonMethod.GetPrefferedHotelCodes(ref Ids, ref xmlbase, ref WebHotellierSearchConfig, "WebHotelier", dtPreferredDataHotel, SearchPara.IsSearchFor);
                if (WebHotellierSearchConfig != null)
                {
                    double CurrencyFactorwebhotellier = 0;
                    string currency = WebHotellierDetails[0].CurrencyShortName;
                    if (currency.ToLower() != tokenData.SiteCurrency.ToLower())
                    {
                        CurrencyFactorwebhotellier = _commonMethod.GetROEByCurrency(currency, dtROE);

                    }
                    for (int k = 0; k < WebHotellierSearchConfig.Count; k++)
                    {
                        bool DebugMode = (Convert.ToInt32(WebHotellierSearchConfig[k].DebugMode) == 0 ? false : true);
                        string UserName = Convert.ToString(WebHotellierDetails[0].UserName);
                        string Password = Convert.ToString(WebHotellierDetails[0].Password);
                        {
                            int IsCitywisesearch = Convert.ToInt32(WebHotellierSearchConfig[k].City);
                            if (IsCitywisesearch == 0)//(DtHotelData.DefaultView.Count > 0)
                            {
                                if (Convert.ToInt32(WebHotellierSearchConfig[k].CityID) == 0 || Convert.ToInt32(WebHotellierSearchConfig[k].CityID) == SearchPara.CityID)
                                {
                                    //List<HotelCodeByCity> DtHotelData1 = FilterFinalList(commonCode, listRating);
                                    if (commonCode.Length > 0)
                                    {
                                        int Noofrecords = Convert.ToInt32(WebHotellierSearchConfig[k].Noofrecords);
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
                                                string IdsForSearch = Convert.ToString(paramValues1);//Convert.ToString(GetHotelCodeByCOmma(lstxml[a]));
                                                HotelX Xhotel = new HotelX();
                                                Xhotel.Supplier = "webhotelier";
                                                Xhotel.DebugeMode = DebugMode;
                                                Xhotel.SearchTimeOut = Convert.ToInt32(WebHotellierSearchConfig[k].HotelSearchTimeOut);
                                                if (Hit > 0)
                                                    Xhotel.SearchTimeOut = Convert.ToInt32(WebHotellierSearchConfig[k].HotelSearchTimeOut);
                                                string strCheckIn = Convert.ToString(SearchPara.CheckInDate);
                                                string strCheckOut = Convert.ToString(SearchPara.CheckOutDate);
                                                Xhotel.URL = Convert.ToString(WebHotellierDetails[0].WebSiteURL) + WebHotellierStaticInfo.WebHotellier_HotelSearch;
                                                Xhotel.UserName = UserName;
                                                Xhotel.PassWord = Password;
                                                Xhotel.Version = Convert.ToString(WebHotellierDetails[0].version);
                                                Xhotel.UserId = Convert.ToString(WebHotellierDetails[0].UserID);
                                                Xhotel.CurrencyCode = Convert.ToString(currency);
                                                Xhotel.CurrencyFactor = CurrencyFactorwebhotellier;
                                                Xhotel.XmlCity = XMLCity;
                                                Xhotel.XmlCountry = "";
                                                Xhotel.XmlNationality = XMLNationality;
                                                string PaxPassport = XMLNationality;
                                                Xhotel.AgentWiseMarkup = WebHotellierDetails[0].AgentWiseMarkup;
                                                Xhotel.IsShowSupplier = Convert.ToInt16(WebHotellierDetails[0].IsShowSupplier) == 1 ? true : false;
                                                Xhotel.SupplierID = WebHotellierDetails[0].SupplierID;
                                                Xhotel.MasterSupplierID = WebHotellierDetails[0].MasterSupplierId;
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
                                                    objrq.Request = XMLWebHotellierRequestOnly_async(Xhotel.URL, UserName, Xhotel.PassWord, Xhotel.SearchTimeOut, SearchPara, DebugMode, XMLNationality, XMLCity, XMLCountry, SearchPara.UserName, Xhotel.CurrencyCode, Hit, Xhotel.hotelRating, Xhotel.Version, IdsForSearch);

                                                    objrq.Response = "";
                                                    RQRS.Add(objrq);
                                                }
                                                Xhotel.RQRS = RQRS;
                                                #endregion

                                                AllTask.Add(Xhotel);
                                            }
                                        }
                                        else
                                        {
                                            for (int a = 0; a < splittedtables.Length; a++)
                                            {
                                                var paramValues1 = String.Join(",", splittedtables[a].Select().Select(x => String.Join(",", Convert.ToString(x.ItemArray[0]))));
                                                string IdsForSearch = Convert.ToString(paramValues1);//Convert.ToString(GetHotelCodeByCOmma(lstxml[a]));
                                                HotelX Xhotel = new HotelX();
                                                Xhotel.Supplier = "webhotelier";
                                                Xhotel.DebugeMode = DebugMode;
                                                Xhotel.SearchTimeOut = Convert.ToInt32(WebHotellierSearchConfig[k].HotelSearchTimeOut);
                                                if (Hit > 0)
                                                    Xhotel.SearchTimeOut = Convert.ToInt32(WebHotellierSearchConfig[k].HotelSearchTimeOut);
                                                string strCheckIn = Convert.ToString(SearchPara.CheckInDate);
                                                string strCheckOut = Convert.ToString(SearchPara.CheckOutDate);
                                                //Xhotel.URL = Convert.ToString(WebHotellierDetails[0].WebSiteURL) + WebConfigurationManager.AppSettings["WebHotellier_RoomSearch"] + "/" + IdsForSearch + "?";
                                                Xhotel.URL = Convert.ToString(WebHotellierDetails[0].WebSiteURL) + WebHotellierStaticInfo.WebHotellier_RoomSearch;

                                                Xhotel.UserName = UserName;
                                                Xhotel.PassWord = Password;
                                                Xhotel.Version = Convert.ToString(WebHotellierDetails[0].version);
                                                Xhotel.UserId = Convert.ToString(WebHotellierDetails[0].UserID);
                                                Xhotel.CurrencyCode = Convert.ToString(currency);
                                                Xhotel.CurrencyFactor = CurrencyFactorwebhotellier;
                                                Xhotel.XmlCity = XMLCity;
                                                Xhotel.XmlCountry = "";
                                                Xhotel.XmlNationality = XMLNationality;
                                                string PaxPassport = XMLNationality;
                                                Xhotel.AgentWiseMarkup = WebHotellierDetails[0].AgentWiseMarkup;
                                                Xhotel.IsShowSupplier = Convert.ToInt16(WebHotellierDetails[0].IsShowSupplier) == 1 ? true : false;
                                                Xhotel.SupplierID = WebHotellierDetails[0].SupplierID;
                                                Xhotel.MasterSupplierID = WebHotellierDetails[0].MasterSupplierId;
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
                                                    objrq.Request = XMLWebHotellierRequestOnly_async_Rooms(SearchPara, XMLCity, XMLCountry, Xhotel.URL, UserName, Xhotel.PassWord, XMLNationality, Xhotel.SearchTimeOut, Xhotel.CurrencyCode, IdsForSearch);

                                                    objrq.Response = "";
                                                    RQRS.Add(objrq);
                                                }
                                                Xhotel.RQRS = RQRS;
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
            catch (Exception ex)
            {
                _exception.Error(ex);
            }


        }

        public void WebHotellierRequestOnly_async_new(DataRow[] commonCode, List<StarRating> listRating, HotelSearchParameter SearchPara, List<SupplierDetails> lstWebHotellierDetails, List<SupplierSearchConfig> lstWebHotellierSearchConfig, DataTable dtPreferredDataHotel, ref ConcurrentBag<HotelX> AllTask, int Hit, DataTable dtROE, TokenData tokenData, DataSet dtXMLCity)
        {
            try
            {
                List<StarRating> lstRating = new List<StarRating>();
                lstRating = _commonMethod.GetStarRatingStringList_xtg(SearchPara);

                List<HotelCodeByCity> DtHotelDAta = HtmlExtensions.ToList<HotelCodeByCity>(commonCode.CopyToDataTable());
                int ratingcount = lstRating.Count;
                int hotelcount = DtHotelDAta.Count;

                foreach (SupplierDetails WebHotellierDetails in lstWebHotellierDetails)
                {
                    try
                    {
                        int SupplierID = WebHotellierDetails.SupplierID;
                        string Supplier = WebHotellierDetails.SupplierName;

                        if (lstWebHotellierSearchConfig.Count > 0)
                        {
                            List<SupplierSearchConfig> WebHotellierSearchConfig = lstWebHotellierSearchConfig
                                        .Where(x => x != null && x.SupplierID == SupplierID)
                                        .ToList();

                            if (WebHotellierSearchConfig.Count > 0)
                            {
                                DataForSearch dataForSearch = _commonMethod.GetXmlDataForSearchTechwise(SearchPara.Nationality, dtXMLCity, Supplier, "WebHotelier");
                                if (dataForSearch.XmlNationality != "")
                                {
                                    List<HotelCodeByCity> filter = new List<HotelCodeByCity>();
                                    filter = _commonMethod.FilterWithOutCity_Code(ratingcount, hotelcount, DtHotelDAta, lstRating, Supplier);

                                    if (filter.Count > 0)
                                    {
                                        string Ids = string.Empty;
                                        string xmlbase = "";
                                        _commonMethod.GetPrefferedHotelCodes(ref Ids, ref xmlbase, ref WebHotellierSearchConfig, "WebHotelier", dtPreferredDataHotel, SearchPara.IsSearchFor);

                                        if (WebHotellierSearchConfig != null)
                                        {
                                            double CurrencyFactorwebhotellier = 0;
                                            string currency = WebHotellierDetails.CurrencyShortName;
                                            if (currency.ToLower() != tokenData.SiteCurrency.ToLower())
                                            {
                                                CurrencyFactorwebhotellier = _commonMethod.GetROEByCurrency(currency, dtROE);

                                            }
                                            for (int k = 0; k < WebHotellierSearchConfig.Count; k++)
                                            {
                                                bool DebugMode = (Convert.ToInt32(WebHotellierSearchConfig[k].DebugMode) == 0 ? false : true);
                                                string UserName = Convert.ToString(WebHotellierDetails.UserName);
                                                string Password = Convert.ToString(WebHotellierDetails.Password);
                                                {
                                                    int IsCitywisesearch = Convert.ToInt32(WebHotellierSearchConfig[k].City);
                                                    if (IsCitywisesearch == 0)//(DtHotelData.DefaultView.Count > 0)
                                                    {
                                                        if (Convert.ToInt32(WebHotellierSearchConfig[k].CityID) == 0 || Convert.ToInt32(WebHotellierSearchConfig[k].CityID) == SearchPara.CityID)
                                                        {
                                                            //List<HotelCodeByCity> DtHotelData1 = FilterFinalList(commonCode, listRating);
                                                            if (commonCode.Length > 0)
                                                            {
                                                                int Noofrecords = Convert.ToInt32(WebHotellierSearchConfig[k].Noofrecords);
                                                                double count = Convert.ToDouble(filter.Count);
                                                                int NoOfDiv = Convert.ToInt32(Math.Ceiling(count / Convert.ToDouble(Noofrecords)));
                                                                List<List<XMLID_Search>> lstxml = _commonMethod.GetXMLIDString(NoOfDiv, Noofrecords, count, filter);

                                                                for (int a = 0; a < NoOfDiv; a++)
                                                                {
                                                                    string IdsForSearch = Convert.ToString(_commonMethod.GetHotelCodeByCOmma(lstxml[a]));

                                                                    HotelX Xhotel = new HotelX();
                                                                    Xhotel.Supplier = Supplier;
                                                                    Xhotel.DebugeMode = DebugMode;
                                                                    Xhotel.SearchTimeOut = Convert.ToInt32(WebHotellierSearchConfig[k].HotelSearchTimeOut);
                                                                    //if (Hit > 0)
                                                                        Xhotel.ProcessTimeout = Convert.ToInt32(WebHotellierSearchConfig[k].HotelSearchTimeOut2);
                                                                    string strCheckIn = Convert.ToString(SearchPara.CheckInDate);
                                                                    string strCheckOut = Convert.ToString(SearchPara.CheckOutDate);

                                                                    if (SearchPara.Filters.IsShowRooms == 0)
                                                                    {
                                                                        if (_keySettings.SupplierProxy == "0")
                                                                            Xhotel.URL = Convert.ToString(WebHotellierDetails.WebSiteURL) + WebHotellierStaticInfo.WebHotellier_HotelSearch;
                                                                        else
                                                                            Xhotel.URL = Convert.ToString(WebHotellierDetails.WebSiteURLforAPI) + WebHotellierStaticInfo.WebHotellier_HotelSearch;
                                                                    }
                                                                    else
                                                                    {
                                                                        if (_keySettings.SupplierProxy == "0")
                                                                            Xhotel.URL = Convert.ToString(WebHotellierDetails.WebSiteURL) + WebHotellierStaticInfo.WebHotellier_RoomSearch;
                                                                        else
                                                                            Xhotel.URL = Convert.ToString(WebHotellierDetails.WebSiteURLforAPI) + WebHotellierStaticInfo.WebHotellier_RoomSearch;
                                                                    }

                                                                    Xhotel.UserName = UserName;
                                                                    Xhotel.PassWord = Password;
                                                                    Xhotel.Version = Convert.ToString(WebHotellierDetails.version);
                                                                    Xhotel.UserId = Convert.ToString(WebHotellierDetails.UserID);
                                                                    Xhotel.CurrencyCode = Convert.ToString(currency);
                                                                    Xhotel.CurrencyFactor = CurrencyFactorwebhotellier;
                                                                    Xhotel.XmlCity = dataForSearch.xmlCity;
                                                                    Xhotel.XmlCountry = "";
                                                                    Xhotel.XmlNationality = dataForSearch.XmlNationality;
                                                                    string PaxPassport = dataForSearch.XmlNationality;
                                                                    Xhotel.AgentWiseMarkup = WebHotellierDetails.AgentWiseMarkup;
                                                                    Xhotel.IsShowSupplier = Convert.ToInt16(WebHotellierDetails.IsShowSupplier) == 1 ? true : false;
                                                                    Xhotel.SupplierID = WebHotellierDetails.SupplierID;
                                                                    Xhotel.MasterSupplierID = WebHotellierDetails.MasterSupplierId;
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
                                                                        if (SearchPara.Filters.IsShowRooms == 0)
                                                                        {
                                                                            objrq.Request = XMLWebHotellierRequestOnly_async(Xhotel.URL, UserName, Xhotel.PassWord, Xhotel.SearchTimeOut, SearchPara, DebugMode, dataForSearch.XmlNationality, dataForSearch.xmlCity, dataForSearch.xmlCountry, SearchPara.UserName, Xhotel.CurrencyCode, Hit, Xhotel.hotelRating, Xhotel.Version, IdsForSearch);
                                                                        }
                                                                        else
                                                                        {
                                                                            objrq.Request = XMLWebHotellierRequestOnly_async_Rooms(SearchPara, dataForSearch.xmlCity, dataForSearch.xmlCountry, Xhotel.URL, UserName, Xhotel.PassWord, dataForSearch.XmlNationality, Xhotel.SearchTimeOut, Xhotel.CurrencyCode, IdsForSearch);

                                                                        }


                                                                        objrq.Response = "";
                                                                        RQRS.Add(objrq);
                                                                    }
                                                                    Xhotel.ReturnRooms = WebHotellierDetails.ReturnRooms;
                                                                    Xhotel.TechName = WebHotellierDetails.TechName;
                                                                    Xhotel.RQRS = RQRS;
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

        public string XMLWebHotellierRequestOnly_async(string WebHotellierRoomurl, string UserName, string Password, int SearchTimeOut, HotelSearchParameter searchParam, bool DebugMode, string Nationality, string CityName, string CountryId, string UserId, string CurrencyCode, int IsHit, int hotelRating, string Version, string HotelID)
        {
            try
            {
                string checkin = System.Convert.ToString(searchParam.CheckInDate);
                string checkout = Convert.ToString(searchParam.CheckOutDate);
                int NoOfNight = Convert.ToInt32(searchParam.NoofNights);
                checkin = checkin.Substring(6, 4) + "-" + checkin.Substring(3, 2) + "-" + checkin.Substring(0, 2);
                checkout = checkout.Substring(6, 4) + "-" + checkout.Substring(3, 2) + "-" + checkout.Substring(0, 2);
                int NoOfNights = searchParam.NoofNights;
                int NoofRoom = searchParam.Rooms.Count;
                string CityName1 = "Athens";
                try
                {
                    StringBuilder WebHotellierhotellist = new StringBuilder();
                    string uniqid = Guid.NewGuid().ToString().Substring(0, 8);
                    WebHotellierhotellist.Append("&checkin=" + checkin + "");
                    WebHotellierhotellist.Append("&checkout=" + checkout + "");
                    WebHotellierhotellist.Append("&nights=" + NoOfNights + "");
                    WebHotellierhotellist.Append("&rooms=" + NoofRoom + "");
                    WebHotellierhotellist.Append("&remote_country=" + Nationality + "");

                    #region Adults Child Count

                    WebHotellierhotellist.Append("&party=");
                    WebHotellierhotellist.Append("[");
                    for (int ser = 0; ser < searchParam.Rooms.Count; ser++)
                    {
                        int NoOfadult = searchParam.Rooms[ser].NoofAdults;
                        if (ser != 0)
                        {
                            WebHotellierhotellist.Append(",{");
                        }
                        else
                        {
                            WebHotellierhotellist.Append("{");
                        }
                        WebHotellierhotellist.Append("adults=" + NoOfadult + "");
                        if (searchParam.Rooms[ser].NoofChild > 0)
                        {
                            WebHotellierhotellist.Append(",children=");
                            WebHotellierhotellist.Append("[");
                            for (int i = 0; i < searchParam.Rooms[ser].NoofChild; i++)
                            {

                                if (i == 0)
                                    WebHotellierhotellist.Append(searchParam.Rooms[ser].Child1Age.ToString());
                                else
                                    WebHotellierhotellist.Append("," + searchParam.Rooms[ser].Child2Age.ToString());
                            }
                            WebHotellierhotellist.Append("]");

                        }
                        WebHotellierhotellist.Append("}");

                    }
                    WebHotellierhotellist.Append("]");
                    #endregion

                    WebHotellierhotellist.Append("&properties=" + HotelID + ""); /////static hotelcode
                    string WebhotellierReq = WebHotellierRoomurl + WebHotellierhotellist.ToString();
                    return WebhotellierReq;
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

        public string XMLWebHotellierRequestOnly_async_Rooms(HotelSearchParameter SearchPara, string CityCode, string CountryCode, string webhotelier_URL, string webhotelier_User_Name, string Webhotelier_Password, string Nationality, Int32 TimeOut, string saltoursCurrency, string hotelId)
        {
            try
            {
                string checkin = System.Convert.ToString(SearchPara.CheckInDate);
                string checkout = Convert.ToString(SearchPara.CheckOutDate);
                int NoOfNight = Convert.ToInt32(SearchPara.NoofNights);
                checkin = checkin.Substring(6, 4) + "-" + checkin.Substring(3, 2) + "-" + checkin.Substring(0, 2);
                checkout = checkout.Substring(6, 4) + "-" + checkout.Substring(3, 2) + "-" + checkout.Substring(0, 2);
                int NoOfNights = SearchPara.NoofNights;
                int NoofRoom = SearchPara.Rooms.Count;
                try
                {
                    StringBuilder WebHotellierhotellist = new StringBuilder();
                    string uniqid = Guid.NewGuid().ToString().Substring(0, 8);
                    WebHotellierhotellist.Append("&checkin=" + checkin + "");
                    WebHotellierhotellist.Append("&checkout=" + checkout + "");
                    WebHotellierhotellist.Append("&nights=" + NoOfNights + "");
                    WebHotellierhotellist.Append("&rooms=" + NoofRoom + "");
                    #region Adults Child Count
                    WebHotellierhotellist.Append("&party=");
                    WebHotellierhotellist.Append("[");
                    for (int ser = 0; ser < SearchPara.Rooms.Count; ser++)
                    {
                        int NoOfadult = SearchPara.Rooms[ser].NoofAdults;
                        if (ser != 0)
                        {
                            WebHotellierhotellist.Append(",{");
                        }
                        else
                        {
                            WebHotellierhotellist.Append("{");
                        }
                        WebHotellierhotellist.Append("adults=" + NoOfadult + "");
                        if (SearchPara.Rooms[ser].NoofChild > 0)
                        {
                            WebHotellierhotellist.Append(",children=");
                            WebHotellierhotellist.Append("[");
                            for (int i = 0; i < SearchPara.Rooms[ser].NoofChild; i++)
                            {

                                if (i == 0)
                                    WebHotellierhotellist.Append(SearchPara.Rooms[ser].Child1Age.ToString());
                                else
                                    WebHotellierhotellist.Append("," + SearchPara.Rooms[ser].Child2Age.ToString());
                            }
                            WebHotellierhotellist.Append("]");

                        }
                        WebHotellierhotellist.Append("}");
                    }
                    WebHotellierhotellist.Append("]");
                    #endregion

                    WebHotellierhotellist.Append("&remote_country=" + Nationality + "");
                    WebHotellierhotellist.Append("&offline=1");
                    WebHotellierhotellist.Append("&payments=1");
                    WebHotellierhotellist.Append("&taxes=1");
                    WebHotellierhotellist.Append("&properties=" + hotelId + "");



                    string WebhotellierReq = webhotelier_URL + WebHotellierhotellist.ToString();
                    return WebhotellierReq;
                }
                catch (Exception ex)
                {
                    _exception.Error(ex);
                    return "";
                }
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
            }
            return "";
        }

        public async Task<List<Options>> WebHotellier_HotelSearchWithRooms(HotelX item, CommonSearch commonSearch, string ResponseData, HotelSearchParameter SearchParam, string XMLCITY, string XMLCountry, string currency, string supplierURL, string Useraname, string password, string xmlhotelcode, double CurrencyFactor, string Provider, DataSet dsAgentDetail, DataTable dtCountryCityMarkup, Int32 AgentId, Int32 TariffID, double AgentPercentage, Boolean IsShowSupplier, int SupplierID, int MasterSupplierID, DataTable dtROE, string XmlNationality = "")
        {
            List<Options> options = new List<Options>();
            DataTable DtHotel = _commonMethod.GenerateDataTableNewV2();


            try
            {


                int CutOffDays = XMLHotelBookingCommon.CutOffDays;
                CutOffDays = _commonMethod.GetCutOffDays(commonSearch.dtSuplierMarkup, "" + Provider + "");
                double SubAgentMarkup = 0;
                int IsParentId = 1, agentID = 0;
                if (commonSearch.dtAgentDetail != null)
                {
                    IsParentId = Convert.ToInt32(commonSearch.dtAgentDetail.Rows[0]["IsParentId"]);
                    SubAgentMarkup = Convert.ToDouble(commonSearch.dtAgentDetail.Rows[0]["B2CMarkupPer"]);
                    agentID = Convert.ToInt32(commonSearch.dtAgentDetail.Rows[0]["AgentID"]);
                }
                item.ResponseType = "NoCount";
                #region When Not Need Rooms

                //{
                //DataRow[] dtdetails = dsBulkdata.Tables["MappingDetails"].Select("XmlId='" + WHResponse.data.hotels[i].code + "' and SupplierName='" + Provider + "' ");
                if (SearchParam.Filters.IsShowRooms == 0)
                {

                    WebHotellier.WebHotellierAvailResponse.Root WHResponse = null;
                    try
                    {

                        WHResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<WebHotellier.WebHotellierAvailResponse.Root>(ResponseData); //140525
                       // WHResponse = JsonSerializer.Deserialize<WebHotellier.WebHotellierAvailResponse.Root>(ResponseData);

                    }
                    catch (Exception ex)
                    {
                        await _exception.Error(ex);
                    }
                    if (WHResponse.error_code.ToString().ToUpper() == "NO_AVAILABILITY" || WHResponse.error_code.ToString().ToUpper() == "NO_HOTELS_FOUND")
                    {
                        return options;
                    }

                    if (WHResponse.data == null)
                    {
                        return options;
                    }

                    if (WHResponse.data.hotels.Count < 1)
                    {
                        item.ResponseType = "NoCount";
                    }

                    for (int i = 0; i < WHResponse.data.hotels.Count; i++)
                    {
                        item.ResponseType = "Success";
                        DataRow drXmlHotel = DtHotel.NewRow();



                        //drXmlHotel["ContractID"] = 0;
                        drXmlHotel["SupplierId"] = SupplierID;
                        //drXmlHotel["AgentPercentage"] = AgentPercentage;//need to change

                        drXmlHotel["MasterSupplierId"] = Convert.ToInt32(MasterSupplierID);

                        drXmlHotel["IsRecomondedHotel"] = false;

                        drXmlHotel["HotelFrontImage"] = "";
                        drXmlHotel["HotelName"] = "";
                        drXmlHotel["Description"] = "";
                        drXmlHotel["EssentialInformation"] = "";
                        drXmlHotel["OfficeID"] = 0;
                        drXmlHotel["ContractID"] = 0;
                        drXmlHotel["IsShowSupplier"] = false;
                        drXmlHotel["OwnSystemHotelId"] = 0;
                        drXmlHotel["Address"] = "";
                        drXmlHotel["Favorite"] = 0;
                        drXmlHotel["XmlHotelCity"] = XMLCITY;
                        drXmlHotel["XMLCountry"] = XMLCountry;
                        drXmlHotel["XMLCity"] = XMLCITY;
                        drXmlHotel["SupplierName"] = Provider;
                        drXmlHotel["BookingStatus"] = "Available";
                        drXmlHotel["SupplierCurrency"] = currency;

                        drXmlHotel["UniqueNo"] = Guid.NewGuid().ToString();
                        drXmlHotel["AvailToken"] = WHResponse.data.hotels[i].code;
                        drXmlHotel["VatdiffAmount"] = 0;
                        drXmlHotel["XMLHotelCode"] = WHResponse.data.hotels[i].code;

                        double FinalTotal = 0;


                        for (int J = 0; J < WHResponse.data.hotels[i].rates.Count; J++)
                        {
                            if (FinalTotal == 0)
                            {
                                FinalTotal = Convert.ToDouble(WHResponse.data.hotels[i].rates[J].price);
                            }
                            else
                            {
                                if (Convert.ToDouble(WHResponse.data.hotels[i].rates[J].price) < FinalTotal)
                                {
                                    FinalTotal = Convert.ToDouble(WHResponse.data.hotels[i].rates[J].price);
                                }
                            }
                        }
                        currency = WHResponse.data.hotels[i].currency;
                        if (currency.ToLower() != commonSearch.tokenData.SiteCurrency.ToLower())
                        {
                            //DataTable dtInrCurrency = Common.GetCountryCurrency(currency);
                            CurrencyFactor = _commonMethod.GetROEByCurrency(currency, dtROE);
                            //CurrencyFactor = Convert.ToDouble(dtInrCurrency.Rows[0]["AmtToExch"]);
                        }
                        FinalTotal = FinalTotal * CurrencyFactor;
                        if (FinalTotal == 0)

                            FinalTotal = 1; //Common.setHotelListMarkup(SearchParam, dtCountryCityMarkup, Convert.ToInt32(dsAgentDetails.Tables[0].Rows[0]["IsParentId"]), Convert.ToDouble(dsAgentDetails.Tables[0].Rows[0]["B2CMarkupPer"]), AgentId, TariffID, AgentPercentage, FinalTotal, Convert.ToString(drGiataDetails[0]["GiataId"]), SupplierID, Convert.ToString(drGiataDetails[0]["Rating"]));
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
                #region When Need Rooms
                else
                {

                    string JsonResponse = ResponseData;
                    if (JsonResponse == "")
                    {
                        return options;
                    }
                    WebHotellierRoom.WebHotelierRoomResponse.Root WebhotelierResponse = new WebHotellierRoom.WebHotelierRoomResponse.Root();
                    WebhotelierResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<WebHotellierRoom.WebHotelierRoomResponse.Root>(JsonResponse);
                   // WebhotelierResponse = JsonSerializer.Deserialize<WebHotellierRoom.WebHotelierRoomResponse.Root>(JsonResponse);
                    if (WebhotelierResponse == null)
                    {
                        return options;
                    }
                    if (WebhotelierResponse.data == null)
                        return options;
                    if (WebhotelierResponse.data.hotels == null)
                        return options;

                    for (int h = 0; h < WebhotelierResponse.data.hotels.Count; h++)
                    {
                        item.ResponseType = "Success";
                        DataTable dtWebhotelierRoom = _commonMethod.GenerateRoomtypeDataTableNewV2();
                        DataTable dtWebHotelierRoomRateBreakup = _commonMethod.GenerateoomRateBreackUpDataTableNewV2();
                        DataTable dtWebhotelierCancellationPolicy = _commonMethod.GenerateCancellationDataTableNewV2();

                        currency = WebhotelierResponse.data.hotels[h].currency;
                        #region Hotels
                        DataRow drXmlHotel = DtHotel.NewRow();
                        drXmlHotel["ContractID"] = 0;
                        drXmlHotel["SupplierId"] = SupplierID;
                        //drXmlHotel["AgentPercentage"] = AgentPercentage;

                        drXmlHotel["MasterSupplierId"] = Convert.ToInt32(MasterSupplierID);

                        drXmlHotel["IsRecomondedHotel"] = false;

                        drXmlHotel["HotelFrontImage"] = "";
                        drXmlHotel["HotelName"] = "";
                        drXmlHotel["Description"] = "";
                        drXmlHotel["EssentialInformation"] = "";
                        drXmlHotel["OfficeID"] = 0;

                        drXmlHotel["IsShowSupplier"] = false;
                        drXmlHotel["OwnSystemHotelId"] = 0;
                        drXmlHotel["Address"] = "";
                        drXmlHotel["Favorite"] = 0;
                        drXmlHotel["XmlHotelCity"] = XMLCITY;
                        drXmlHotel["XMLCountry"] = XMLCountry;
                        drXmlHotel["XMLCity"] = XMLCITY;
                        drXmlHotel["SupplierName"] = Provider;
                        drXmlHotel["BookingStatus"] = "Available";
                        drXmlHotel["SupplierCurrency"] = currency;

                        drXmlHotel["UniqueNo"] = Guid.NewGuid().ToString();
                        drXmlHotel["AvailToken"] = WebhotelierResponse.data.hotels[h].code;
                        drXmlHotel["VatdiffAmount"] = 0;
                        drXmlHotel["XMLHotelCode"] = WebhotelierResponse.data.hotels[h].code;
                        double FinalTotal = 0;

                        //for (int J = 0; J < WebhotelierResponse.data.hotels[h].rates.Count; J++)
                        //{
                        //    if (FinalTotal == 0)
                        //    {
                        //        FinalTotal = Convert.ToDouble(WebhotelierResponse.data.hotels[h].rates[J].price);
                        //    }
                        //    else
                        //    {
                        //        if (Convert.ToDouble(WebhotelierResponse.data.hotels[h].rates[J].price) < FinalTotal)
                        //        {
                        //            FinalTotal = Convert.ToDouble(WebhotelierResponse.data.hotels[h].rates[J].price);
                        //        }
                        //    }
                        //}
                        currency = WebhotelierResponse.data.hotels[h].currency;
                        if (currency.ToLower() != commonSearch.tokenData.SiteCurrency.ToLower())
                        {

                            CurrencyFactor = _commonMethod.GetROEByCurrency(currency, dtROE);
                        }
                        //FinalTotal = FinalTotal * CurrencyFactor;
                        //if (FinalTotal == 0)
                        //    ;
                        FinalTotal = 1; //Common.setHotelListMarkup(SearchParam, dtCountryCityMarkup, Convert.ToInt32(dsAgentDetails.Tables[0].Rows[0]["IsParentId"]), Convert.ToDouble(dsAgentDetails.Tables[0].Rows[0]["B2CMarkupPer"]), AgentId, TariffID, AgentPercentage, FinalTotal, Convert.ToString(drGiataDetails[0]["GiataId"]), SupplierID, Convert.ToString(drGiataDetails[0]["Rating"]));
                        drXmlHotel["SellingPrice"] = FinalTotal;
                        drXmlHotel["FinalSellingPrice"] = FinalTotal;
                        DtHotel.Rows.Add(drXmlHotel);
                        #region "Hotel Block here"
                        #endregion
                        #region "GetRooms for this hotel"
                        bool willAddOrNot = true;
                        Int64 RaynaID = 0;
                        Int64 GiataId = 0;
                        int RatingHotel = 0;
                        Int64 OwnsystemHotelId = 0;
                        // Provider = "travellanda";
                        double MinPrice = FinalTotal;
                        _markup.GetHotelDataCommon(ref MinPrice, ref willAddOrNot, ref OwnsystemHotelId, ref RaynaID, ref GiataId, ref RatingHotel, ref drXmlHotel, commonSearch.AgentId, commonSearch.TariffID, commonSearch.dsAgentDetails, commonSearch.supplierGiataMaster, SearchParam, commonSearch.dtCountryCityMarkup, commonSearch.B2CMarkup, commonSearch.IsParentId, commonSearch.SubAgentMarkup, commonSearch.SearchByHotel, commonSearch.HotelIDS);
                        if (willAddOrNot)
                        {
                            //DataTable dtHotelRoom = Common.GenerateRoomtypeDataTableNewV2();
                            //DataTable dtCancellation = Common.GenerateCancellationDataTableNewV2();
                            //DataTable DtRoomRateBreackUp = Common.GenerateoomRateBreackUpDataTableNewV2();

                            #region "Room Logic"


                            Boolean blnIsShowSupplier = false;
                            double AgentMarkupAmount = 0.0;
                            Boolean isPerce = true;
                            string HotelDetails = "";

                            //#region Hoteldetails
                            //try
                            //{
                            //    string IWTXUrl = "https://rest.reserve-online.net";
                            //    //Password = "WiThin3@rTh";
                            //    //Username = "WithinEarth";
                            //    string Request = supplierURL + "/property/" + xmlhotelcode;

                            //    string Error = "";

                            //    //string Request = ""; 
                            //    string url = supplierURL; string UserName = Useraname; string Password = password;
                            //    int TimeOut = 120000;
                            //    ResponseData = Common.WebhotelierBookingRS_HotelDeatil(ref Error, Request, Request, UserName, Password, TimeOut = 120000);// (IWTXUrl, Request, "iwtx");
                            //                                                                                                                              //Common.XMLRequestResponseTrack_HotelBeds(IWTXUrl + "/" + Request, ResponseData, "iwtx", "Proper", "");
                            //    webHotelliearHotelDetails.Root WebhotelierResponseHTD = new webHotelliearHotelDetails.Root();
                            //    WebhotelierResponseHTD = JsonConvert.DeserializeObject<webHotelliearHotelDetails.Root>(ResponseData);

                            //    HotelDetails = WebhotelierResponseHTD.data.general_terms;
                            //}
                            //catch (Exception ex)
                            //{
                            //    Common.Error(ex, "true");

                            //    //return dsTemp;
                            //}
                            //#endregion
                            int hotelRating = 0;
                            string GIATACode = "";
                            //_markup.getAgetMarkupSetting(Provider, GIATACode, commonSearch.dtSuplierMarkup, commonSearch.dtMarkupDetails, ref AgentPercentage, ref AgentMarkupAmount, ref blnIsShowSupplier, ref isPerce, agentID, hotelRating, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), Convert.ToString(SearchParam.CheckInDate));

                            _markup.getAgetMarkupSettingNew_new(Provider, Convert.ToString(RaynaID), commonSearch.dtSuplierMarkup, commonSearch.dsMarkupDetails, ref AgentPercentage, ref AgentMarkupAmount, ref blnIsShowSupplier, ref isPerce, agentID, hotelRating, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), SearchParam.CheckInDate);
                            double AgentCompanyPercentage = Convert.ToDouble(SearchParam.AgentCompanyPercentage);
                            bool IsB2C = Convert.ToBoolean(Convert.ToByte(SearchParam.IsB2BOrB2C));
                            double B2CMarkup = (IsB2C == false ? AgentCompanyPercentage : XMLHotelBookingCommon.B2CMarkup);
                            if (IsB2C == true)
                                AgentPercentage = 0;
                            //
                            int totalnight = Convert.ToInt32(SearchParam.NoofNights);
                            try
                            {

                                //else
                                {

                                    int totalAdult = 0;
                                    int totalChild = 0;
                                    for (int b = 0; b < SearchParam.Rooms.Count; b++)
                                    {
                                        totalAdult = Convert.ToInt32(SearchParam.Rooms[b].NoofAdults) + totalAdult;
                                        totalChild = Convert.ToInt32(SearchParam.Rooms[b].NoofChild) + totalChild;
                                    }
                                    string HotelCode = WebhotelierResponse.data.hotels[h].code;
                                    string HotelName = WebhotelierResponse.data.hotels[h].name;
                                    string WebHotelierCurrency = WebhotelierResponse.data.hotels[h].currency;
                                    string Meal_desc = "";
                                    for (int W = 0; W < WebhotelierResponse.data.hotels[h].rates.Count(); W++)
                                    {
                                        //if (WebhotelierResponse.data.hotels[h].rates[W].status.ToUpper() == "AVL")
                                        {
                                            int RoomId = WebhotelierResponse.data.hotels[h].rates[W].id;
                                            string RoomType = WebhotelierResponse.data.hotels[h].rates[W].type;
                                            string RoomName = WebhotelierResponse.data.hotels[h].rates[W].room;
                                            string cancellation_policy = WebhotelierResponse.data.hotels[h].rates[W].cancellation_policy;

                                            if (WebhotelierResponse.data.hotels[h].rates[W].rate_desc != null)
                                            {
                                                Meal_desc = WebhotelierResponse.data.hotels[h].rates[W].rate_desc;
                                            }
                                            double TotalPrice = WebhotelierResponse.data.hotels[h].rates[W].price;

                                            string IncludeTax = "";
                                            //for (int tx = 0; tx < WebhotelierResponse.data.taxes.Count(); tx++)
                                            {
                                                //if (WebhotelierResponse.data.hotels[h].rates[W].taxes.included == 0 || WebhotelierResponse.data.taxes[tx].included == -1)
                                                //{
                                                //    if (WebhotelierResponse.data.taxes[tx].type == "ABS")
                                                //    {

                                                //    }
                                                //    IncludeTax += IncludeTax + "</BR>" + WebhotelierResponse.data.taxes[tx].description + " : " + WebhotelierResponse.data.taxes[tx].value + " " + WebHotelierCurrency;
                                                //}
                                                if (WebhotelierResponse.data.hotels[h].rates[W].taxes != 0)
                                                {
                                                    IncludeTax += IncludeTax + "</BR> taxes:" + WebhotelierResponse.data.hotels[h].rates[W].taxes + " " + WebHotelierCurrency;
                                                }
                                                if (WebhotelierResponse.data.hotels[h].rates[W].extras != 0)
                                                {
                                                    IncludeTax += IncludeTax + "</BR> extras:" + WebhotelierResponse.data.hotels[h].rates[W].extras + " " + WebHotelierCurrency;
                                                }
                                                if (WebhotelierResponse.data.hotels[h].rates[W].excluded_charges != 0)
                                                {
                                                    IncludeTax += IncludeTax + "</BR> excluded charges:" + WebhotelierResponse.data.hotels[h].rates[W].excluded_charges + " " + WebHotelierCurrency;
                                                }
                                            }
                                            if (IncludeTax != "")
                                            {
                                                IncludeTax = "Pay at hotel per room per night " + IncludeTax;
                                            }
                                            //string EssentialInformation = Meal_desc + "</br>" + cancellation_policy;
                                            string EssentialInformation = HotelDetails + "</br>" + Meal_desc + "</br>" + IncludeTax;

                                            double BookFinalPrice = TotalPrice;
                                            double CurrencyFactorWebhotelier = 0;
                                            if (WebHotelierCurrency.ToLower() != commonSearch.tokenData.SiteCurrency.ToLower())
                                            {
                                                //DataTable dtInrCurrency = Common.GetCountryCurrency(WebHotelierCurrency);
                                                CurrencyFactorWebhotelier = _commonMethod.GetROEByCurrency(WebHotelierCurrency, dtROE);

                                            }

                                            #region BordName
                                            string BordName = "";
                                            if (WebhotelierResponse.data.hotels[h].rates[W].board != null)
                                            {
                                                int BordCode = WebhotelierResponse.data.hotels[h].rates[W].board;

                                                switch (BordCode)
                                                {
                                                    case 1:
                                                        BordName = "All inclusive";
                                                        break;
                                                    case 2:
                                                        BordName = "American";
                                                        break;
                                                    case 3:
                                                        BordName = "Bed & breakfast";
                                                        break;
                                                    case 4:
                                                        BordName = "Buffet breakfast";
                                                        break;
                                                    case 5:
                                                        BordName = "Caribbean breakfast";
                                                        break;
                                                    case 6:
                                                        BordName = "Continental breakfast";
                                                        break;
                                                    case 7:
                                                        BordName = "English breakfast";
                                                        break;
                                                    case 8:
                                                        BordName = "European plan";
                                                        break;
                                                    case 9:
                                                        BordName = "Family plan";
                                                        break;
                                                    case 10:
                                                        BordName = "Full board";
                                                        break;
                                                    case 11:
                                                        BordName = "Full breakfast";
                                                        break;
                                                    case 12:
                                                        BordName = "Half board";
                                                        break;
                                                    case 13:
                                                        BordName = "As brochured";
                                                        break;
                                                    case 14:
                                                        BordName = "Room only";
                                                        break;
                                                    case 15:
                                                        BordName = "Self catering";
                                                        break;
                                                    case 16:
                                                        BordName = "Bermuda";
                                                        break;
                                                    case 17:
                                                        BordName = "Dinner bed and breakfast plan";
                                                        break;
                                                    case 18:
                                                        BordName = "Family America";
                                                        break;
                                                    case 19:
                                                        BordName = "Breakfast";
                                                        break;
                                                    case 20:
                                                        BordName = "Modified";
                                                        break;
                                                    case 21:
                                                        BordName = "Lunch";
                                                        break;
                                                    case 22:
                                                        BordName = "Dinner";
                                                        break;
                                                    case 23:
                                                        BordName = "Breakfast & lunch";
                                                        break;
                                                    default:
                                                        BordName = "Room Only";
                                                        break;
                                                }
                                            }
                                            #endregion
                                            //Int64 OwnsystemHotelId = 0;
                                            TotalPrice = TotalPrice * CurrencyFactorWebhotelier;
                                            string uniqid = Guid.NewGuid().ToString();
                                            string Totalfinalprice = Convert.ToString(TotalPrice);
                                            string RoomTypeId = BookFinalPrice + "$$$" + RoomId;
                                            #region Ratebreakup
                                            for (int n = 0; n < SearchParam.NoofNights; n++)
                                            {
                                                string CheckInDate = Convert.ToString(SearchParam.CheckInDate);
                                                string CheckOutDate = Convert.ToString(SearchParam.CheckOutDate);
                                                DateTime dtCheckInDate = DateTime.ParseExact(CheckInDate, "dd/MM/yyyy", null);
                                                DateTime dtCheckOutDate = DateTime.ParseExact(CheckOutDate, "dd/MM/yyyy", null);
                                                TimeSpan t = dtCheckOutDate - dtCheckInDate;

                                                double ratePerNight = TotalPrice;
                                                #region Vat
                                                double VatOnPerNight = 0;
                                                ratePerNight = _markup.ProcessHotelRateBreakupPrice_WithMarkup(1, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), ratePerNight, B2CMarkup, IsParentId, isPerce, AgentPercentage, AgentMarkupAmount, SubAgentMarkup, ref VatOnPerNight);
                                                #endregion

                                                ratePerNight = ratePerNight / SearchParam.NoofNights;
                                                DataRow drRateBreackUp = dtWebHotelierRoomRateBreakup.NewRow();
                                                drRateBreackUp["SupplierName"] = Provider;
                                                drRateBreackUp["OwnsystemHotelId"] = OwnsystemHotelId;
                                                drRateBreackUp["ContractID"] = 0;
                                                drRateBreackUp["RoomNo"] = Convert.ToInt32(1);
                                                drRateBreackUp["RoomTypeId"] = RoomTypeId;
                                                drRateBreackUp["MealId"] = BordName;
                                                drRateBreackUp["Meal"] = BordName;
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
                                                drRateBreackUp["BuyingPrice"] = ratePerNight;
                                                dtWebHotelierRoomRateBreakup.Rows.Add(drRateBreackUp);
                                            }
                                            #endregion

                                            #region Rooms
                                            DataRow drRoom = dtWebhotelierRoom.NewRow();
                                            drRoom["OwnsystemHotelId"] = OwnsystemHotelId;
                                            drRoom["RoomTypeCode"] = "";// RoomTypeCord;
                                            drRoom["BoardType"] = BordName;
                                            drRoom["MealName"] = BordName;
                                            drRoom["MealId"] = BordName;
                                            drRoom["Type"] = "";
                                            drRoom["Board"] = BordName;
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
                                            drRoom["EssentialInformation"] = EssentialInformation;
                                            drRoom["Characteristic"] = "";
                                            bool IsCombine = true;
                                            bool IsDynamicPolicy = false;
                                            #region Vat
                                            double itemprice = TotalPrice;
                                            _markup.ProcessHotelRoom_WithMarkup(CurrencyFactorWebhotelier, WebHotelierCurrency, IsDynamicPolicy, IsCombine, SearchParam.NoofNights * SearchParam.Rooms.Count, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), SearchParam.CheckInDate, SearchParam.CheckOutDate, ref itemprice, ref drRoom, isPerce, AgentPercentage, AgentMarkupAmount, SubAgentMarkup, B2CMarkup, IsParentId);
                                            #endregion
                                            drRoom["SupplierName"] = Provider;
                                            drRoom["RoomTypeName"] = RoomName;
                                            drRoom["RoomTypeId"] = RoomTypeId;
                                            dtWebhotelierRoom.Rows.Add(drRoom);
                                            #endregion
                                            #region  "Cancellationpolicy"

                                            if (WebhotelierResponse.data.hotels[h].rates[W].cancellation_fees != null)
                                            {
                                                int Can = 0;
                                                //for (int Can = 0; Can < WebhotelierResponse.data.rates[W].cancellation_fees.Count(); Can++)
                                                {
                                                    bool Istrue = true;
                                                    DateTime CancelletiondaysAfter = DateTime.Now;
                                                    if (WebhotelierResponse.data.hotels[h].rates[W].cancellation_expiry != "")//need to check
                                                    {
                                                        CancelletiondaysAfter = Convert.ToDateTime(WebhotelierResponse.data.hotels[h].rates[W].cancellation_expiry);// WebhotelierResponse.data.rates[W].cancellation_fees[Can].after;
                                                    }
                                                    //double CancelPricenew = WebhotelierResponse.data.rates[W].cancellation_fees[Can].fee;
                                                    double CancelPrice = TotalPrice;
                                                    string ownsystemid = Convert.ToString(OwnsystemHotelId);
                                                    double CompanyBuy = CancelPrice;// * CurrencyFactorWebhotelier;// CancellationPrice;
                                                    DateTime curDate = _commonMethod.GetTimeZoneDate(SearchParam.CurrentDate);
                                                    double Penalty = Convert.ToDouble(CompanyBuy);
                                                    DateTime MainDate = new DateTime();
                                                    DateTime StartDate = MainDate;
                                                    DateTime CancelDate = CancelletiondaysAfter;
                                                    #region Vat
                                                    double VatOnPolicy = 0;
                                                    double MainAgentCharge = CancelPrice;
                                                    double CompanyChargeAmount = CancelPrice;
                                                    Penalty = _markup.ProcessHotelCancelationPrice_WithMarkup(ref MainAgentCharge, SearchParam.NoofNights, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), CancelPrice, isPerce, B2CMarkup, AgentPercentage, AgentMarkupAmount, IsParentId, SubAgentMarkup, ref VatOnPolicy);
                                                    #endregion

                                                    DateTime NowDate = DateTime.Now.Date;
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
                                                        if (Istrue == true)
                                                        {
                                                            double zero = 0;
                                                            dtWebhotelierCancellationPolicy.Rows.Add(_commonMethod.setCancellationPolicyRow(ref zero, dtWebhotelierCancellationPolicy.NewRow(), CurrencyFactorWebhotelier, WebHotelierCurrency, Provider, ownsystemid, Convert.ToString(1),
                                                                               Convert.ToString(RoomTypeId), RoomName, BordName, Convert.ToDateTime(NowDate), Convert.ToDateTime(CancelDate).AddDays(-1), 0, 0, "0", VatOnPolicy, uniqid, "NO", "", "", "", ""));
                                                            dtWebhotelierCancellationPolicy.AcceptChanges();
                                                            Istrue = false;
                                                        }
                                                        dtWebhotelierCancellationPolicy.Rows.Add(_commonMethod.setCancellationPolicyRow(ref CompanyChargeAmount, dtWebhotelierCancellationPolicy.NewRow(), CurrencyFactorWebhotelier, WebHotelierCurrency, Provider, ownsystemid, Convert.ToString(1),
                                                                                           Convert.ToString(RoomTypeId), RoomName, BordName, Convert.ToDateTime(CancelDate), DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null), MainAgentCharge, Penalty, "0", VatOnPolicy, uniqid, "NO", "", "", "", ""));
                                                        dtWebhotelierCancellationPolicy.AcceptChanges();
                                                    }
                                                    else
                                                    {
                                                        dtWebhotelierCancellationPolicy.Rows.Add(_commonMethod.setCancellationPolicyRow(ref CompanyChargeAmount, dtWebhotelierCancellationPolicy.NewRow(), CurrencyFactorWebhotelier, WebHotelierCurrency, Provider, ownsystemid, Convert.ToString(1),
                                                                                Convert.ToString(RoomTypeId), RoomName, BordName, NowDate, DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null), MainAgentCharge, Penalty, "0", VatOnPolicy, uniqid, "Yes", "", ""));
                                                        dtWebhotelierCancellationPolicy.AcceptChanges();
                                                    }
                                                }
                                            }
                                            #endregion
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                await _exception.Error(ex);
                            }
                            #endregion


                            #endregion
                            Options ops = (await _supplierRes.GetCommonRoomForSuppliers(GiataId, commonSearch.tokenData, commonSearch.advancedOptions, MinPrice, drXmlHotel, dtWebhotelierRoom, dtWebhotelierCancellationPolicy, dtWebHotelierRoomRateBreakup, SearchParam));
                            if (ops != null)
                                options.Add(ops);
                        }
                    }
                    #endregion
                    #endregion



                }


            }
            catch (Exception ex)
            {
                await _exception.Error(ex);

                //return Common.GenerateDataTableNewV2();
            }
            return options;
        }

        public async Task<string> Webhotelier_EssentialInfoOnly(string supplierURL, string xmlhotelcode, string Useraname, string password,string SearchKey)
        {
            string HotelDetails = "";
            string ResponseData = "";
            #region Hoteldetails
            try
            {
                //string IWTXUrl = "https://rest.reserve-online.net";
                //Password = "WiThin3@rTh";
                //Username = "WithinEarth";
                string Request = supplierURL + "/property/" + xmlhotelcode;

                string Error = "";

                //string Request = ""; 
                string url = supplierURL; string UserName = Useraname; string Password = password;
                int TimeOut = 120000;

                ResponseDto responseDto = await WebhotelierBookingRS_HotelDeatil( Request, Request, UserName, Password, TimeOut = 120000);// (IWTXUrl, Request, "iwtx");
                                                                                                                                    //Common.XMLRequestResponseTrack_HotelBeds(IWTXUrl + "/" + Request, ResponseData, "iwtx", "Proper", "");
                ResponseData = responseDto.Response;
                Error = responseDto.Error;

                webHotelliearHotelDetails.Root WebhotelierResponseHTD = new webHotelliearHotelDetails.Root();
                WebhotelierResponseHTD = Newtonsoft.Json.JsonConvert.DeserializeObject<webHotelliearHotelDetails.Root>(ResponseData);

                HotelDetails = WebhotelierResponseHTD.data.general_terms;

                if (HotelDetails != "")
                    HotelDetails = "<br><br>" + HotelDetails;
            }
            catch (Exception ex)
            {
                _ = _exception.ErrorNew(ex, _LogAPI.Recheck, SearchKey);

                //return dsTemp;
            }
            #endregion
            return HotelDetails;
        }

        public async Task<ResponseDto> WebhotelierBookingRS_HotelDeatil(string Request, string url, string UserName, string Password, Int32 TimeOut = 120000)
        {
            ResponseDto res = new ResponseDto();
            #region old
            string strOutput = "";
            string Error = "";
            System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
            timer.Start();
            try
            {

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                string Key = "";
                request.Method = "GET";
                Key = Convert.ToBase64String(Encoding.ASCII.GetBytes(UserName + ":" + Password));
                request.Headers.Add("Authorization", "Basic " + Key);
                request.ContentType = "application/json";
                request.Accept = "application/json";
                //request.Headers.Add("Accept-Encoding", "gzip");
                request.KeepAlive = true;
                request.Timeout = TimeOut;

                ServicePointManager.DefaultConnectionLimit = 2000;
                byte[] postBytes = Encoding.ASCII.GetBytes(Request);
                //using (Stream dataStream = request.GetRequestStream())
                //{
                //    dataStream.Write(postBytes, 0, postBytes.Length);
                //}
                try
                {
                    HttpWebResponse Response = (HttpWebResponse)await request.GetResponseAsync();
                    Stream responseStream = Response.GetResponseStream();
                    StreamReader Reader = new StreamReader(responseStream, Encoding.UTF8);
                    strOutput = await Reader.ReadToEndAsync();
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
                            String errorText = reader.ReadToEnd();
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

                res.Response = Error;
                res.Error = Error;
                return res;
            }

            res.Response = strOutput;
            res.Error = Error;

            return res;
            #endregion

        }

        public async Task<ResponseDto> WebhotelierBookingRS(string Request, string url, string UserName, string Password, Int32 TimeOut = 120000)
        {
            ResponseDto responseDto = new ResponseDto();
            string Error = "";
            #region old
            string strOutput = "";
            System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
            timer.Start();
            try
            {

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                string Key = "";
                request.Method = "POST";
                Key = Convert.ToBase64String(Encoding.ASCII.GetBytes(UserName + ":" + Password));
                request.Headers.Add("Authorization", "Basic " + Key);
                request.ContentType = "application/x-www-form-urlencoded";
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
                    StreamReader Reader = new StreamReader(responseStream, Encoding.UTF8);
                    strOutput =await Reader.ReadToEndAsync();
                    responseStream.Close();
                    Reader.Close();
                    Response.Close();
                    timer.Stop();
                }
                catch (WebException ex)
                {
                    _ =_exception.Error(ex);

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
                _ =_exception.Error(ex);
                Error = ex.Message.ToString();
                responseDto.Error = Error;
                return responseDto;
            }

            responseDto.Response = strOutput;
            responseDto.Error = Error;
            return responseDto;
            #endregion

        }

        public string WebHotelierBookingRQ(DataSet dsCartDetails, DataRow[] WelcomebedsDetails, int i, int j, double resPrice, DataTable dtPassengers, ref string AgtRef, string CheckIn, string CheckOut, int UniqueNo, string BookToken, string searchkey, bool IsPruvo,string AgentCod="")
        {
            string RequestString = "";
            try
            {
                string roomtypeid = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomTypeId"]);
                string[] spitid1 = roomtypeid.Split(new String[] { "$$$" }, StringSplitOptions.None);
                double Price = Convert.ToDouble(spitid1[0]);
                int RateId = Convert.ToInt32(spitid1[1]);
                string Bookingtoken = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["purchaseToken"]);
                string RoomNo = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomNumber"]);
                string AgentCode = "Book_" + Convert.ToString(AgentCod + "_" + UniqueNo);
                int NoOfRoom = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView.Count);
                int NoOfNights = Convert.ToInt32(dsCartDetails.Tables[0].Rows[i]["NoOfNight"]);

                int Adults = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[j]["NoOfAdults"]);

                StringBuilder WebHotellierhotellist = new StringBuilder();
                string uniqid = Guid.NewGuid().ToString().Substring(0, 8);
                WebHotellierhotellist.Append("&checkin=" + CheckIn + "");
                WebHotellierhotellist.Append("&checkout=" + CheckOut + "");
                WebHotellierhotellist.Append("&rate=" + Convert.ToInt32(RateId) + "");
                WebHotellierhotellist.Append("&price=" + Convert.ToDouble(Price) + "");
                WebHotellierhotellist.Append("&rooms=" + NoOfRoom + "");
                WebHotellierhotellist.Append("&adults=" + Adults + "");
                #region Adults Child Count
                WebHotellierhotellist.Append("&party=");
                WebHotellierhotellist.Append("[");
                dtPassengers.DefaultView.Sort = "IsLead desc";
                if (dtPassengers.DefaultView.Count > 0)
                {
                    for (int x = 0; x < dsCartDetails.Tables[1].DefaultView.Count; x++)
                    {
                        int NoOfAdults = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[x]["NoOfAdults"]);

                        dtPassengers.DefaultView.RowFilter = "PickUp='" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[x]["RoomTypeId"]) + "' and UserCartID='" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[x]["UserCartID"]) + "' and ServiceType='Hotel' and RoomNo='" + dsCartDetails.Tables[1].DefaultView[x]["RoomNumber"] + "'";
                        //for (int k = 0; k < dtPassengers.DefaultView.Count; k++)
                        {
                            if (x != 0)
                            {
                                WebHotellierhotellist.Append(",{");
                            }
                            else
                            {
                                WebHotellierhotellist.Append("{");
                            }
                            WebHotellierhotellist.Append("adults:" + NoOfAdults + "");

                            if (Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[x]["NoOfChild"]) > 0)
                            {
                                WebHotellierhotellist.Append(",children:");
                                WebHotellierhotellist.Append("[");
                                //for (int Child = 0; Child < Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[x]["NoOfChild"]); Child++)
                                {
                                    int NoofChild = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[x]["NoOfChild"]);
                                    if (NoofChild > 0)
                                    {
                                        switch (NoofChild)
                                        {
                                            case 1:
                                                int ChildAge1 = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[x]["ChildAge1"]);
                                                WebHotellierhotellist.Append(ChildAge1);
                                                break;
                                            case 2:
                                                ChildAge1 = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[x]["ChildAge1"]);
                                                int ChildAge2 = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[x]["ChildAge2"]);

                                                string Childages = ChildAge1 + "," + ChildAge2;
                                                WebHotellierhotellist.Append(Childages);
                                                break;
                                        }
                                    }

                                    //if (Child==0)
                                    //{
                                    //    WebHotellierhotellist.Append(ChildAge1);
                                    //}
                                    //else
                                    //{
                                    //    string Childages = ChildAge1 + "," + ChildAge2;
                                    //    WebHotellierhotellist.Append(Childages);
                                    //}
                                }
                                WebHotellierhotellist.Append("]");
                            }
                            WebHotellierhotellist.Append("}");
                        }
                    }
                }
                WebHotellierhotellist.Append("]");

                #endregion

                string firstname = Convert.ToString(dtPassengers.DefaultView[0]["FirstName"]).Trim();
                string lastname = Convert.ToString(dtPassengers.DefaultView[0]["LastName"]).Trim();
                //string Email = "Test@gmail.com";
                //string Email = "ashish@technoheaven.com";
                string Email = "barahim@withinearth.com";//added on 21-MAY-2024 (as per k2&ashish)



                #region APISO VC
                int usercartdetailsid = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[j]["UserCartDetailId"]);
                int UserCartId = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[j]["UserCartID"]);
                //get bookingid and detailsid
                //DBConnection db = new DBConnection();
                //SqlParameter[] param = new SqlParameter[]
                //{
                //new SqlParameter("@usercartdetailsid",usercartdetailsid)
                //};
                //DataTable dtbookingdata = db.GetDataTable("getApisobookingdata", param);

                DataTable dtbookingdata = _commonMethod.GetApisoBookingData(usercartdetailsid);

                string Pan = "";
                string CVV = "0", Expiry = "0";

                if (dtbookingdata != null)
                {
                    _vcn.GetApisoServiceDatatoCreateVCN("", Convert.ToInt32(dtbookingdata.Rows[0]["OnlineBookingMasterId"]), Convert.ToInt32(dtbookingdata.Rows[0]["OnlineBookingDetailsId"]), ref Pan, ref CVV, ref Expiry, IsPruvo);
                    //PANN = Pan;
                }

                #endregion

                string CardNumber = Pan;// 5425233430109903;
                string CardType = "MC";
                string CardName = "WithinEarth";
                string ExpiryRegex = Convert.ToString(Expiry).Substring(2, 2);
                string ExpiryRegexFisrt = Convert.ToString(Expiry).Substring(0, 2);
                string CardMonth = ExpiryRegex;// 04;
                string CardYear = "20" + ExpiryRegexFisrt;// 2026;
                string CardCVV = CVV;// 123;

                WebHotellierhotellist.Append("&cardNumber=" + CardNumber + "");
                WebHotellierhotellist.Append("&cardType=" + CardType + "");
                WebHotellierhotellist.Append("&cardName=" + CardName + "");
                WebHotellierhotellist.Append("&cardMonth=" + CardMonth + "");
                WebHotellierhotellist.Append("&cardYear=" + CardYear + "");
                WebHotellierhotellist.Append("&cardCVV=" + CardCVV + "");
                WebHotellierhotellist.Append("&firstName=" + firstname + "");
                WebHotellierhotellist.Append("&lastName=" + lastname + "");
                WebHotellierhotellist.Append("&email=" + Email + "");

                string WebhotellierReq = WebHotellierhotellist.ToString();
                return WebhotellierReq;
            }
            catch (Exception ex)
            {
                _ = _exception.ErrorBooking(ex, "", "", searchkey);
            }
            return RequestString;
        }
    }
}
