using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;
using System.Diagnostics;
using XConnect.Supplier.IWTX.Interface;
using XConnect.CommonUtilities.Interface;
using XConnect.ErrorLogHandler.Interface;
using XConnect.Markup.Interface;
using XConnect.Models;
using XConnect.Models.Hotel;
using XConnect.CommonUtilities;
using System.Text.Json.Serialization;
using Azure;
using System.Text.Json;
using XConnect.Supplier.Agoda;
using static XConnect.Supplier.IWTX.IWTXClass;
using Microsoft.Extensions.Options;
using Options = XConnect.Models.Options;
using Newtonsoft.Json;
using static Azure.Core.HttpHeader;
using System.Xml.Linq;
using RestSharp;
using Microsoft.IdentityModel.Protocols;
using XConnect.Supplier.XTG;
using System.Net.Http.Headers;
using System.Net;
using System.IO.Compression;
using static System.Net.Mime.MediaTypeNames;
using Microsoft.Extensions.Configuration;
using static XConnect.Supplier.Booking.Com.preebookrs;
using static XConnect.Models.ErrorLog;
using MongoDB.Driver.Core.Events;


namespace XConnect.Supplier.IWTX
{
    public class IWTXRepository : IIwtx
    {
        private readonly ICommonMethod _commonMethod;
        private readonly IException _exception;
        private readonly IMarkup _markup;
        private readonly ISupplierRes _supplierRes;
        private readonly KeySettings _keySettings; 
        private readonly SupplierConfiguration _supplierConfig;



        public IWTXRepository(ICommonMethod commonMethod, IMarkup markup, ISupplierRes supplierRes, IOptions<KeySettings> keySettings, IException exception, IOptions<SupplierConfiguration> supplierConfig)
        {
            _commonMethod = commonMethod;

            _markup = markup;
            _supplierRes = supplierRes;
            _keySettings = keySettings.Value;
            _exception = exception;
            _supplierConfig = supplierConfig.Value;

        }

        public void IWTXRequestOnly_async(DataRow[] commonCode, List<StarRating> listRating, HotelSearchParameter SearchPara, List<SupplierDetails> IWTXDetails, List<SupplierSearchConfig> IWTXSearchConfig, DataTable dtPreferredDataHotel, ref ConcurrentBag<HotelX> AllTask, string XMLCity, string XMLCountry, string XMLNationality, int Hit, DataTable dtROE, TokenData tokenData)
        {
            try
            {
                string Ids = string.Empty;
                string xmlbase = "";
                _commonMethod.GetPrefferedHotelCodes(ref Ids, ref xmlbase, ref IWTXSearchConfig, "iwtx", dtPreferredDataHotel, SearchPara.IsSearchFor);
                if (IWTXSearchConfig != null)
                {
                    string Supplier = Convert.ToString(IWTXDetails[0].SupplierName);
                    double CurrencyFactorIWTX = 1;
                    // string Currency = "USD";
                    // string Currency = Convert.ToString(ConfigurationManager.AppSettings["IWTXCurrency"]);
                    // string Currency = (Convert.ToString(_configuration["AppSettings:IWTXCurrency"]));
                    //if (_configuration["AppSettings:Use_ReplicationDB"] == "1")


                    for (int k = 0; k < IWTXSearchConfig.Count; k++)
                    {
                        bool DebugMode = (Convert.ToInt32(IWTXSearchConfig[k].DebugMode) == 0 ? false : true);
                        string UserName = Convert.ToString(IWTXDetails[0].UserName);
                        string Password = Convert.ToString(IWTXDetails[0].Password);

                        string Iwtx_currency = Convert.ToString(IWTXDetails[0].CurrencyShortName);
                        string Sitecurrency = tokenData.SiteCurrency;

                        if (Iwtx_currency.ToLower() != Sitecurrency.ToLower()) // change systembase currency 
                        {
                            CurrencyFactorIWTX = _commonMethod.GetROEByCurrency(Iwtx_currency, dtROE);
                        }
                        {
                            int IsCitywisesearch = Convert.ToInt32(IWTXSearchConfig[k].City);

                            if (Convert.ToInt32(IWTXSearchConfig[k].CityID) == 0 || Convert.ToInt32(IWTXSearchConfig[k].CityID) == SearchPara.CityID)
                            {
                                if (commonCode.Length > 0)
                                {
                                    int Noofrecords = Convert.ToInt32(IWTXSearchConfig[k].Noofrecords);
                                    DataTable[] splittedtables = commonCode.AsEnumerable()
                                                .Select((row, index) => new { row, index })
                                                .GroupBy(x => x.index / Noofrecords)  // integer division, the fractional part is truncated
                                                .Select(g => g.Select(x => x.row).CopyToDataTable())
                                                .ToArray();

                                    if (SearchPara.Filters.IsShowRooms == 0)
                                    {
                                        for (int a = 0; a < splittedtables.Length; a++)
                                        {
                                            string Uniqueid = Guid.NewGuid().ToString().Substring(0, 8);
                                            var paramValues1 = String.Join(",", splittedtables[a].Select().Select(x => String.Join(",", Convert.ToString(x.ItemArray[0]))));
                                            string IdsForSearch = Convert.ToString(paramValues1);
                                            HotelX Xhotel = new HotelX();
                                            Xhotel.Supplier = Supplier;
                                            Xhotel.DebugeMode = DebugMode;
                                            Xhotel.SearchTimeOut = Convert.ToInt32(IWTXSearchConfig[k].HotelSearchTimeOut);
                                            //if (Hit > 0)
                                            Xhotel.ProcessTimeout = Convert.ToInt32(IWTXSearchConfig[k].HotelSearchTimeOut2);

                                            //Xhotel.URL = Convert.ToString(IWTXDetails[0].WebSiteURL) + IWTXInfo.IWTX_HotelSearch;
                                            Xhotel.URL = Convert.ToString(IWTXDetails[0].WebSiteURL) + _supplierConfig.IWTX.IWTX_HotelSearch;
                                            // Xhotel.URL = Convert.ToString(IWTXDetails[0].WebSiteURL) + "hotel/api/v1/search";
                                            // Xhotel.URL = Convert.ToString(IWTXDetails[0].WebSiteURL) + Convert.ToString(ConfigurationManager.AppSettings["IWTX_HotelSearch"]);
                                            Xhotel.UserId = Convert.ToString(IWTXDetails[0].UserID);
                                            Xhotel.UserName = UserName;
                                            Xhotel.PassWord = Password;
                                            Xhotel.CurrencyCode = Iwtx_currency;
                                            //Xhotel.CurrencyCode = Currency;
                                            Xhotel.CurrencyFactor = CurrencyFactorIWTX;
                                            Xhotel.XmlCity = XMLCity;
                                            Xhotel.XmlCountry = "";
                                            Xhotel.XmlNationality = XMLNationality;
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
                                                objrq.Request = HotelSearchRequest_IWTX(SearchPara, IdsForSearch, UserName, Password, XMLNationality, Uniqueid);
                                                objrq.Response = "";
                                                RQRS.Add(objrq);
                                            }
                                            Xhotel.RQRS = RQRS;
                                            Xhotel.ReturnRooms = 5; // IWTXDetails[0].ReturnRooms; //190225
                                            Xhotel.TechName = "IWTX"; // IWTXDetails[0].TechName; //190225
                                            #endregion
                                            AllTask.Add(Xhotel);
                                        }
                                    }
                                    else  // && count == 1)
                                    {
                                        for (int a = 0; a < splittedtables.Length; a++)
                                        {
                                            string Uniqueid = Guid.NewGuid().ToString().Substring(0, 8);
                                            var paramValues1 = String.Join(",", splittedtables[a].Select().Select(x => String.Join(",", Convert.ToString(x.ItemArray[0]))));
                                            string IdsForSearch = Convert.ToString(paramValues1);
                                            HotelX Xhotel = new HotelX();
                                            Xhotel.Supplier = Supplier;
                                            Xhotel.DebugeMode = DebugMode;
                                            Xhotel.SearchTimeOut = Convert.ToInt32(IWTXSearchConfig[k].HotelSearchTimeOut);
                                            //if (Hit > 0)
                                            Xhotel.ProcessTimeout = Convert.ToInt32(IWTXSearchConfig[k].HotelSearchTimeOut2);

                                            //Xhotel.URL = Convert.ToString(IWTXDetails[0].WebSiteURL) + IWTXInfo.IWTX_HotelSearch;
                                            Xhotel.URL = Convert.ToString(IWTXDetails[0].WebSiteURL) + _supplierConfig.IWTX.IWTX_HotelSearch;
                                            // Xhotel.URL = Convert.ToString(IWTXDetails[0].WebSiteURL) + Convert.ToString(ConfigurationManager.AppSettings["IWTX_HotelSearch"]);
                                            Xhotel.UserId = Convert.ToString(IWTXDetails[0].UserID);
                                            Xhotel.UserName = UserName;
                                            Xhotel.PassWord = Password;
                                            Xhotel.CurrencyCode = Iwtx_currency;
                                            // Xhotel.CurrencyCode = Currency;
                                            Xhotel.CurrencyFactor = CurrencyFactorIWTX;
                                            Xhotel.XmlCity = XMLCity;
                                            Xhotel.XmlCountry = "";
                                            Xhotel.XmlNationality = XMLNationality;
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
                                                objrq.Request = RoomSearchRequest_IWTX(SearchPara, IdsForSearch, UserName, Password, XMLNationality, Uniqueid);
                                                objrq.Response = "";
                                                RQRS.Add(objrq);
                                            }
                                            Xhotel.RQRS = RQRS;
                                            Xhotel.ReturnRooms = 5; // IWTXDetails[0].ReturnRooms; //190225
                                            Xhotel.TechName = "IWTX";// IWTXDetails[0].TechName; //190225
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

        public void IWTXRequestOnly_async_new(DataRow[] commonCode, List<StarRating> listRating, HotelSearchParameter SearchPara, List<SupplierDetails> lstIWTXDetails, List<SupplierSearchConfig> lstIWTXSearchConfig, DataTable dtPreferredDataHotel, ref ConcurrentBag<HotelX> AllTask, int Hit, DataTable dtROE, TokenData tokenData, DataSet dtXMLCity)
        {
            try
            {


                List<StarRating> lstRating = new List<StarRating>();
                lstRating = _commonMethod.GetStarRatingStringList_xtg(SearchPara);

                List<HotelCodeByCity> DtHotelDAta = HtmlExtensions.ToList<HotelCodeByCity>(commonCode.CopyToDataTable());
                int ratingcount = lstRating.Count;
                int hotelcount = DtHotelDAta.Count;


                foreach (SupplierDetails IWTXChild in lstIWTXDetails)
                {
                    try
                    {
                        int SupplierID = IWTXChild.SupplierID;
                        string Supplier = IWTXChild.SupplierName;

                        //if (Supplier.ToLower() != "iwtx2" || Convert.ToString(_keySettings.ActiveIsXConnectSupplier) == "1")
                        {
                            if (lstIWTXSearchConfig.Count > 0)
                            {
                                List<SupplierSearchConfig> IWTXSearchConfig = lstIWTXSearchConfig
                                       .Where(x => x != null && x.SupplierID == SupplierID)
                                       .ToList();
                                if (IWTXSearchConfig != null && IWTXSearchConfig.Count > 0)
                                {
                                    if ((SearchPara.IsSearchFor.ToLower().IndexOf(Supplier.ToLower()) >= 0 || SearchPara.IsSearchFor.ToLower().IndexOf("multi") >= 0))
                                    {
                                        DataForSearch dataForSearch = _commonMethod.GetXmlDataForSearchTechwise(SearchPara.Nationality, dtXMLCity, Supplier, IWTXChild.TechName);

                                        if (dataForSearch.XmlNationality != "" && SearchPara.PastDate == false)
                                        {
                                            string Ids = string.Empty;
                                            string xmlbase = "";
                                            _commonMethod.GetPrefferedHotelCodes(ref Ids, ref xmlbase, ref IWTXSearchConfig, Supplier, dtPreferredDataHotel, SearchPara.IsSearchFor);

                                            List<HotelCodeByCity> filter = new List<HotelCodeByCity>();
                                            filter = _commonMethod.FilterWithOutCity_Code(ratingcount, hotelcount, DtHotelDAta, lstRating, IWTXChild.HotelSupplier);


                                            string Iwtx_currency = Convert.ToString(IWTXChild.CurrencyShortName);
                                            string Sitecurrency = tokenData.SiteCurrency;

                                            double CurrencyFactorIWTX = 1;

                                            if (Iwtx_currency.ToLower() != Sitecurrency.ToLower()) // change systembase currency 
                                            {
                                                CurrencyFactorIWTX = _commonMethod.GetROEByCurrency(Iwtx_currency, dtROE);
                                            }

                                            if (IWTXSearchConfig != null)
                                            {

                                                for (int k = 0; k < IWTXSearchConfig.Count; k++)
                                                {
                                                    bool DebugMode = (Convert.ToInt32(IWTXSearchConfig[k].DebugMode) == 0 ? false : true);
                                                    string UserName = Convert.ToString(IWTXChild.UserName);
                                                    string Password = Convert.ToString(IWTXChild.Password);


                                                    {
                                                        int IsCitywisesearch = Convert.ToInt32(IWTXSearchConfig[k].City);

                                                        if (Convert.ToInt32(IWTXSearchConfig[k].CityID) == 0 || Convert.ToInt32(IWTXSearchConfig[k].CityID) == SearchPara.CityID)
                                                        {
                                                            if (filter.Count > 0)
                                                            {
                                                                int NoOfRecord = Convert.ToInt32(IWTXSearchConfig[k].Noofrecords);
                                                                double count = Convert.ToDouble(filter.Count);
                                                                int NoOfDiv = Convert.ToInt32(Math.Ceiling(count / Convert.ToDouble(NoOfRecord)));
                                                                List<List<XMLID_Search>> lstxml = _commonMethod.GetXMLIDString(NoOfDiv, NoOfRecord, count, filter);

                                                                if (SearchPara.Filters.IsShowRooms == 0)
                                                                {
                                                                    for (int a = 0; a < NoOfDiv; a++)
                                                                    {
                                                                        string Uniqueid = Guid.NewGuid().ToString().Substring(0, 8);
                                                                        //var paramValues1 = String.Join(",", splittedtables[a].Select().Select(x => String.Join(",", Convert.ToString(x.ItemArray[0]))));
                                                                        //string IdsForSearch = Convert.ToString(paramValues1);
                                                                        string IdsForSearch = Convert.ToString(_commonMethod.GetHotelCodeByCOmma(lstxml[a]));

                                                                        HotelX Xhotel = new HotelX();
                                                                        Xhotel.Supplier = Supplier;
                                                                        Xhotel.DebugeMode = DebugMode;
                                                                        Xhotel.SearchTimeOut = Convert.ToInt32(IWTXSearchConfig[k].HotelSearchTimeOut);
                                                                        //if (Hit > 0)
                                                                        Xhotel.ProcessTimeout = Convert.ToInt32(IWTXSearchConfig[k].HotelSearchTimeOut2);

                                                                        //Xhotel.URL = Convert.ToString(IWTXChild.WebSiteURL) + IWTXInfo.IWTX_HotelSearch;
                                                                        if (_keySettings.SupplierProxy == "0")
                                                                            //Xhotel.URL = Convert.ToString(IWTXChild.WebSiteURL) + IWTXInfo.IWTX_HotelSearch;
                                                                            Xhotel.URL = Convert.ToString(IWTXChild.WebSiteURL) + _supplierConfig.IWTX.IWTX_HotelSearch;
                                                                        else
                                                                            Xhotel.URL = Convert.ToString(IWTXChild.WebSiteURLforAPI) + _supplierConfig.IWTX.IWTX_HotelSearch;


                                                                        // Xhotel.URL = Convert.ToString(IWTXDetails[0].WebSiteURL) + "hotel/api/v1/search";
                                                                        // Xhotel.URL = Convert.ToString(IWTXDetails[0].WebSiteURL) + Convert.ToString(ConfigurationManager.AppSettings["IWTX_HotelSearch"]);
                                                                        Xhotel.UserId = Convert.ToString(IWTXChild.UserID);
                                                                        Xhotel.UserName = UserName;
                                                                        Xhotel.PassWord = Password;
                                                                        Xhotel.CurrencyCode = Iwtx_currency;
                                                                        //Xhotel.CurrencyCode = Currency;
                                                                        Xhotel.CurrencyFactor = CurrencyFactorIWTX;
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
                                                                            objrq.Request = HotelSearchRequest_IWTX(SearchPara, IdsForSearch, UserName, Password, dataForSearch.XmlNationality, Uniqueid);
                                                                            objrq.Response = "";
                                                                            RQRS.Add(objrq);
                                                                        }
                                                                        Xhotel.RQRS = RQRS;
                                                                        Xhotel.ReturnRooms = IWTXChild.ReturnRooms; //190225
                                                                        Xhotel.TechName = IWTXChild.TechName; //190225
                                                                        #endregion
                                                                        AllTask.Add(Xhotel);
                                                                    }
                                                                }
                                                                else  // && count == 1)
                                                                {
                                                                    for (int a = 0; a < NoOfDiv; a++)
                                                                    {
                                                                        string Uniqueid = Guid.NewGuid().ToString().Substring(0, 8);
                                                                        //var paramValues1 = String.Join(",", splittedtables[a].Select().Select(x => String.Join(",", Convert.ToString(x.ItemArray[0]))));
                                                                        //string IdsForSearch = Convert.ToString(paramValues1);
                                                                        string IdsForSearch = Convert.ToString(_commonMethod.GetHotelCodeByCOmma(lstxml[a]));

                                                                        HotelX Xhotel = new HotelX();
                                                                        Xhotel.Supplier = Supplier;
                                                                        Xhotel.DebugeMode = DebugMode;
                                                                        Xhotel.SearchTimeOut = Convert.ToInt32(IWTXSearchConfig[k].HotelSearchTimeOut);
                                                                        //if (Hit > 0)
                                                                        Xhotel.ProcessTimeout = Convert.ToInt32(IWTXSearchConfig[k].HotelSearchTimeOut2);

                                                                        //Xhotel.URL = Convert.ToString(IWTXChild.WebSiteURL) + IWTXInfo.IWTX_HotelSearch;
                                                                        if (_keySettings.SupplierProxy == "0")
                                                                            Xhotel.URL = Convert.ToString(IWTXChild.WebSiteURL) + _supplierConfig.IWTX.IWTX_HotelSearch;
                                                                        else
                                                                            Xhotel.URL = Convert.ToString(IWTXChild.WebSiteURLforAPI) + _supplierConfig.IWTX.IWTX_HotelSearch;

                                                                        // Xhotel.URL = Convert.ToString(IWTXDetails[0].WebSiteURL) + Convert.ToString(ConfigurationManager.AppSettings["IWTX_HotelSearch"]);
                                                                        Xhotel.UserId = Convert.ToString(IWTXChild.UserID);
                                                                        Xhotel.UserName = UserName;
                                                                        Xhotel.PassWord = Password;
                                                                        Xhotel.CurrencyCode = Iwtx_currency;
                                                                        // Xhotel.CurrencyCode = Currency;
                                                                        Xhotel.CurrencyFactor = CurrencyFactorIWTX;
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
                                                                            objrq.Request = RoomSearchRequest_IWTX(SearchPara, IdsForSearch, UserName, Password, dataForSearch.XmlNationality, Uniqueid);
                                                                            objrq.Response = "";
                                                                            RQRS.Add(objrq);
                                                                        }
                                                                        Xhotel.RQRS = RQRS;
                                                                        Xhotel.ReturnRooms = IWTXChild.ReturnRooms; //190225
                                                                        Xhotel.TechName = IWTXChild.TechName; //190225
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
        public string HotelSearchRequest_IWTX(HotelSearchParameter searchParam, string HotelIds, string Username, string Password, string XmlNationality, string Uniqueid)
        {
            try
            {

                string CheckIn = searchParam.CheckInDate;
                CheckIn = searchParam.CheckInDateSubstring; //DateTime.ParseExact(searchParam.CheckInDate, "dd/MM/yyyy", CultureInfo.InvariantCulture).ToString("yyyyMMdd");
                string CheckOut = searchParam.CheckOutDate;
                CheckOut = searchParam.CheckOutDateSubstring; //DateTime.ParseExact(searchParam.CheckOutDate, "dd/MM/yyyy", CultureInfo.InvariantCulture).ToString("yyyyMMdd");
                string Request = "";
                StringBuilder HSreq = new StringBuilder();
                HSreq.Append("<HotelSearchRequest>");
                //HSreq.Append("<OutputFormat>XML</OutputFormat>");
                HSreq.Append("<OutputFormat>JSon</OutputFormat>"); //change 18022025
                HSreq.Append("<Profile>");
                HSreq.Append("<Password>" + Password + "</Password>");
                HSreq.Append("<Code>" + Username + "</Code>");
                HSreq.Append("<TokenNumber>" + Uniqueid + "</TokenNumber>");
                HSreq.Append("</Profile>");
                HSreq.Append("<SearchCriteria>");
                HSreq.Append("<CancellationPolicy>N</CancellationPolicy>");
                HSreq.Append("<EndDate>" + CheckOut + "</EndDate>");
                HSreq.Append("<HotelCode>" + HotelIds + "</HotelCode>");
                HSreq.Append("<IncludeHotelData>N</IncludeHotelData>");
                HSreq.Append("<IncludeOnRequest>N</IncludeOnRequest>");
                HSreq.Append("<Nationality>" + XmlNationality + "</Nationality>");
                HSreq.Append("<RoomConfiguration>");
                #region Room Config
                for (int i = 0; i < searchParam.Rooms.Count; i++)
                {
                    HSreq.Append("<Room>");
                    for (int j = 0; j < searchParam.Rooms[i].NoofAdults; j++)
                    {
                        HSreq.Append("<Adult>");
                        HSreq.Append("<Age>" + 30 + "</Age>");
                        HSreq.Append("</Adult>");
                    }
                    if (searchParam.Rooms[i].NoofChild == 1)
                    {
                        HSreq.Append("<Child>");
                        HSreq.Append("<Age>" + searchParam.Rooms[i].Child1Age + "</Age>");
                        HSreq.Append("</Child>");
                    }
                    else if (searchParam.Rooms[i].NoofChild == 2)
                    {
                        HSreq.Append("<Child>");
                        HSreq.Append("<Age>" + searchParam.Rooms[i].Child1Age + "</Age>");
                        HSreq.Append("</Child>");
                        HSreq.Append("<Child>");
                        HSreq.Append("<Age>" + searchParam.Rooms[i].Child2Age + "</Age>");
                        HSreq.Append("</Child>");
                    }
                    HSreq.Append("<RoomConfigurationId>" + (i + 1) + "</RoomConfigurationId>");
                    HSreq.Append("</Room>");
                }
                #endregion
                HSreq.Append("</RoomConfiguration>");
                HSreq.Append("<StartDate>" + CheckIn + "</StartDate>");
                HSreq.Append("</SearchCriteria>");
                HSreq.Append("</HotelSearchRequest>");
                Request = HSreq.ToString();
                Request = HSreq.ToString();
                return Request;
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
                return "";
            }
        }

        public string RoomSearchRequest_IWTX(HotelSearchParameter searchParam, string HotelIds, string Username, string Password, string XmlNationality, string Uniqueid)
        {
            try
            {
                string HotelRequest = "";
                string CheckIn = searchParam.CheckInDate;
                CheckIn = DateTime.ParseExact(searchParam.CheckInDate, "dd/MM/yyyy", CultureInfo.InvariantCulture).ToString("yyyyMMdd");
                string CheckOut = searchParam.CheckOutDate;
                CheckOut = DateTime.ParseExact(searchParam.CheckOutDate, "dd/MM/yyyy", CultureInfo.InvariantCulture).ToString("yyyyMMdd");
                StringBuilder RSreq = new StringBuilder();
                if (HotelIds != null)
                {
                    RSreq.Append("<HotelSearchRequest>");
                    //RSreq.Append("<OutputFormat>XML</OutputFormat>");
                    RSreq.Append("<OutputFormat>JSON</OutputFormat>"); // change 18022025
                    RSreq.Append("<Profile>");
                    RSreq.Append("<Password>" + Password + "</Password>");
                    RSreq.Append("<Code>" + Username + "</Code>");
                    RSreq.Append("<TokenNumber>" + Uniqueid + "</TokenNumber>");
                    RSreq.Append("</Profile>");
                    RSreq.Append("<SearchCriteria>");
                    RSreq.Append("<RoomConfiguration>");
                    #region Room Config
                    for (int i = 0; i < searchParam.Rooms.Count; i++)
                    {
                        RSreq.Append("<Room>");
                        for (int j = 0; j < searchParam.Rooms[i].NoofAdults; j++)
                        {
                            RSreq.Append("<Adult>");
                            RSreq.Append("<Age>" + 30 + "</Age>");
                            RSreq.Append("</Adult>");
                        }
                        if (searchParam.Rooms[i].NoofChild == 1)
                        {
                            RSreq.Append("<Child>");
                            RSreq.Append("<Age>" + searchParam.Rooms[i].Child1Age + "</Age>");
                            RSreq.Append("</Child>");
                        }
                        else if (searchParam.Rooms[i].NoofChild == 2)
                        {
                            RSreq.Append("<Child>");
                            RSreq.Append("<Age>" + searchParam.Rooms[i].Child1Age + "</Age>");
                            RSreq.Append("</Child>");
                            RSreq.Append("<Child>");
                            RSreq.Append("<Age>" + searchParam.Rooms[i].Child2Age + "</Age>");
                            RSreq.Append("</Child>");
                        }
                        RSreq.Append("</Room>");
                    }
                    #endregion
                    RSreq.Append("</RoomConfiguration>");
                    RSreq.Append("<StartDate>" + CheckIn + "</StartDate>");
                    RSreq.Append("<EndDate>" + CheckOut + "</EndDate>");
                    RSreq.Append("<HotelCode>" + HotelIds + "</HotelCode>");
                    RSreq.Append("<Nationality>" + XmlNationality + "</Nationality>");
                    RSreq.Append("<GroupByRooms>" + "N" + "</GroupByRooms>");
                    RSreq.Append("</SearchCriteria>");
                    RSreq.Append("</HotelSearchRequest>");
                }
                HotelRequest = RSreq.ToString();
                return HotelRequest;
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
                return "";
            }
        }
        #region new 
        public async Task<List<Options>> IWTX_HotelSearchWithRooms(HotelX Item, CommonSearch commonSearch, string currency, string ResponseData, string CityCode, HotelSearchParameter SearchParam, double CurrencyFactorJacTravel, string Supplier, string action, DataSet dsAgentDetails, Int32 processTimeout, string ResProccessTimerLogActive, string ResProccessTimeValueforSaveLog, string StoreSupplierResErrorLog, DataTable DtROE, string ProccessCutOffTime)
        {
            System.Diagnostics.Stopwatch timerroomx = new System.Diagnostics.Stopwatch();
            timerroomx.Start();
            List<Options> options = new List<Options>();

            DataTable DtHotel = _commonMethod.GenerateDataTableNewV2();
            try
            {
                if (ResponseData != string.Empty)
                {
                    //DataSet Iwtx_Response = new DataSet();
                    //ResponseData = ResponseData.Replace("<CancellationPolicyDetails xsi:nil=\"true\" />", "<CancellationPolicyDetails/>");
                    //Adonis_Response = Common.GetDataSetFromXMLString(ResponseData);

                    //Iwtxclass.Iwtx_main Iwtx_Response = JsonConvert.DeserializeObject<Iwtxclass.Iwtx_main>(ResponseData);
                    //IWIXHotelList.Iwtx_main Iwtx_Response = JsonSerializer.Deserialize<IWIXHotelList.Iwtx_main>(ResponseData);
                    IWIXHotelList.Iwtx_main Iwtx_Response = Newtonsoft.Json.JsonConvert.DeserializeObject<IWIXHotelList.Iwtx_main>(ResponseData); //140525

                    if (Iwtx_Response == null)
                    {
                        Item.ResponseStatus = 204;
                        Item.ResponseType = "NoCount";
                        return options;
                    }
                    if (Iwtx_Response.Hotels.Hotel == null)
                    {
                        Item.ResponseStatus = 204;
                        Item.ResponseType = "NoCount";
                        return options;
                    }

                    //string Token = Convert.ToString(Adonis_Response.Tables["AdonisHotelSearchResultDTO"].Rows[0]["Token"]);
                    //DataTable DtAdonis_HotelRoom = _commonMethod.GenerateRoomtypeDataTableNewV2();

                    #region new
                    //for (int h = 0; h <= Adonis_Response.Tables["Hotel"].Rows.Count - 1; h++)
                    for (int h = 0; h < Iwtx_Response.Hotels.Hotel.Count; h++)
                    {
                        //Check Time If High then return NoCount
                        if (ProccessCutOffTime == "1" && timerroomx.ElapsedMilliseconds > processTimeout)
                        {
                            timerroomx.Stop();
                            break;
                        }
                        //--end
                        Item.ResponseType = "Success";

                        string HotelCode = Convert.ToString(Iwtx_Response.Hotels.Hotel[h].HotelCode);
                        string XmlCity = Convert.ToString(Iwtx_Response.Hotels.Hotel[h].City);
                        // int Hotel_Id = Convert.ToInt32(Adonis_Response.Tables["Hotel"].Rows[h]["Hotel_Id"]);
                        int Hotel_Id = Convert.ToInt32(Iwtx_Response.Hotels.Hotel[h].HotelId);
                        double FinalTotal = 0;

                        #region "When not Need Room"  
                        // add hotel code from price only and room code blank here
                        if (SearchParam.Filters.IsShowRooms == 0)
                        {
                            #region "Hotel Code Block "
                            DataRow drXmlHotel = DtHotel.NewRow();
                            drXmlHotel["XMLHotelCode"] = HotelCode;
                            drXmlHotel["HotelName"] = "";
                            drXmlHotel["SupplierName"] = Item.Supplier;
                            drXmlHotel["OfficeID"] = 0;
                            drXmlHotel["ContractID"] = 0;
                            drXmlHotel["Rating"] = 0;
                            drXmlHotel["Reviews"] = 0;
                            drXmlHotel["UniqueNo"] = 0;
                            drXmlHotel["IsShowSupplier"] = false;
                            drXmlHotel["BookingStatus"] = "Available";
                            drXmlHotel["XMLCity"] = "";

                            double rate = 0, minRate = 0;
                            bool ProcessSkip = false;

                            //DataRow[] drRoomTypeDetails = Adonis_Response.Tables["RoomTypeDetails"].Select("Hotel_Id=" + Hotel_Id + "");
                            //int RoomTypeDetails_Id = Convert.ToInt32(drRoomTypeDetails[0]["RoomTypeDetails_Id"]);

                            //DataRow[] drRooms = Adonis_Response.Tables["Rooms"].Select("RoomTypeDetails_Id=" + RoomTypeDetails_Id + "");
                            //int Rooms_Id = Convert.ToInt32(drRooms[0]["Rooms_Id"]);

                            //DataRow[] drRoom = Adonis_Response.Tables["Room"].Select("Rooms_Id=" + Rooms_Id + "");
                            ////Added on 22-JULY-2024
                            //if (drRoom == null || drRoom.Length == 0)
                            //{
                            //    continue;
                            //}
                            ////End
                            //DataView view = new DataView(drRoom.CopyToDataTable());
                            //minRate = GetChepestPriceIWTX(drRoom.CopyToDataTable(), SearchParam.Rooms.Count);




                            try
                            {
                                minRate = GetChepestPriceIWTXnew(Iwtx_Response, SearchParam.Rooms.Count, h);

                                currency = Iwtx_Response.Hotels.Hotel[h].RoomTypeDetails.Rooms.Room[0].CurrCode; // added 
                            }
                            catch (Exception ex)
                            {
                                continue;
                            }

                            double CurrencyFactor = 1;
                            if (currency.ToLower() != commonSearch.tokenData.SiteCurrency.ToLower())
                            {
                                CurrencyFactor = _commonMethod.GetROEByCurrency(currency, DtROE);
                            }
                            minRate = minRate * CurrencyFactor;

                            if (minRate == 0)
                                continue;

                            #region Old Code
                            //DataTable distinctValues = view.ToTable(true, "RatePlanId");

                            //for (int r = 0; r < distinctValues.Rows.Count; r++)
                            //{
                            //    DataRow[] drRoomRatePlan = Adonis_Response.Tables["Room"].Select("Rooms_Id='" + Rooms_Id + "' and RatePlanId='" + distinctValues.Rows[r]["RatePlanId"] + "'");
                            //    for (int p = 0; p < drRoomRatePlan.Length; p++)
                            //    {
                            //        rate = rate + Convert.ToDouble(drRoomRatePlan[p]["Rate"]);
                            //    }
                            //    if (r == 0)
                            //        minRate = rate;
                            //    else
                            //        if (rate < minRate)
                            //        minRate = rate;
                            //}
                            #endregion
                            drXmlHotel["SupplierCurrency"] = currency;
                            drXmlHotel["FinalSellingPrice"] = minRate;
                            drXmlHotel["SellingPrice"] = minRate;
                            drXmlHotel["OfferText"] = "";
                            DtHotel.Rows.Add(drXmlHotel);
                            FinalTotal = minRate;
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
                                    decimal LowestPrice = Convert.ToDecimal(MinPrice);
                                    if(commonSearch.tokenData.IsFromAPI)
                                         LowestPrice = Convert.ToDecimal(MinPrice).CalculatePrice(commonSearch.tokenData, commonSearch.advancedOptions, currency);
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
                            #region "Hotel Code Block "
                            DataRow drXmlHotel = DtHotel.NewRow();
                            drXmlHotel["XMLHotelCode"] = HotelCode;
                            drXmlHotel["HotelName"] = "";
                            drXmlHotel["SupplierName"] = Item.Supplier;
                            drXmlHotel["OfficeID"] = 0;
                            drXmlHotel["ContractID"] = 0;
                            drXmlHotel["Rating"] = 0;
                            drXmlHotel["Reviews"] = 0;
                            drXmlHotel["UniqueNo"] = 0;
                            drXmlHotel["IsShowSupplier"] = false;
                            drXmlHotel["BookingStatus"] = "Available";
                            drXmlHotel["XMLCity"] = "";

                            double rate = 0, minRate = 0;
                            bool ProcessSkip = false;

                            //DataRow[] drRoomTypeDetails = Iwtx_Response.Tables["RoomTypeDetails"].Select("Hotel_Id=" + Hotel_Id + "");
                            //int RoomTypeDetails_Id = Convert.ToInt32(drRoomTypeDetails[0]["RoomTypeDetails_Id"]);

                            //DataRow[] drRooms = Iwtx_Response.Tables["Rooms"].Select("RoomTypeDetails_Id=" + RoomTypeDetails_Id + "");
                            //int Rooms_Id = Convert.ToInt32(drRooms[0]["Rooms_Id"]);

                            //DataRow[] drRoom = Iwtx_Response.Tables["Room"].Select("Rooms_Id=" + Rooms_Id + "");

                            //if (drRoom == null || drRoom.Length == 0)
                            //{
                            //    continue;//Added on 22-JULY-2024
                            //}

                            //DataView view = new DataView(drRoom.CopyToDataTable());

                            //DataTable distinctValues = view.ToTable(true, "RatePlanId");


                            //for (int r = 0; r < distinctValues.Rows.Count; r++)
                            //{
                            //    DataRow[] drRoomRatePlan = Adonis_Response.Tables["Room"].Select("Rooms_Id='" + Rooms_Id + "' and RatePlanId='" + distinctValues.Rows[r]["RatePlanId"] + "'");
                            //    for (int p = 0; p < drRoomRatePlan.Length; p++)
                            //    {
                            //        rate = rate + Convert.ToDouble(drRoomRatePlan[p]["Rate"]);
                            //    }
                            //    if (r == 0)
                            //        minRate = rate;
                            //    else
                            //        if (rate < minRate)
                            //        minRate = rate;
                            //}
                            minRate = 1;
                            drXmlHotel["SupplierCurrency"] = currency;
                            drXmlHotel["FinalSellingPrice"] = minRate;
                            drXmlHotel["SellingPrice"] = minRate;
                            drXmlHotel["OfferText"] = "";
                            DtHotel.Rows.Add(drXmlHotel);
                            FinalTotal = minRate;
                            #endregion
                            double Pricenew = minRate;
                            #region "GetRooms for this hotel"
                            bool willAddOrNot = true;
                            Int64 RaynaID = 0;
                            Int64 GiataId = 0;
                            int RatingHotel = 0;
                            Int64 OwnsystemHotelId = 0;
                            string Provider = Item.Supplier;
                            double MinPrice = Pricenew;

                            int CutOffDays = CacheMaster.CutOffDays;
                            CutOffDays = _commonMethod.GetCutOffDays(commonSearch.dtSuplierMarkup, Provider);

                            _markup.GetHotelDataCommon(ref MinPrice, ref willAddOrNot, ref OwnsystemHotelId, ref RaynaID, ref GiataId, ref RatingHotel, ref drXmlHotel, commonSearch.AgentId, commonSearch.TariffID, commonSearch.dsAgentDetails, commonSearch.supplierGiataMaster, SearchParam, commonSearch.dtCountryCityMarkup, commonSearch.B2CMarkup, commonSearch.IsParentId, commonSearch.SubAgentMarkup, commonSearch.SearchByHotel, commonSearch.HotelIDS);
                            if (willAddOrNot)
                            {
                                DataTable dtHotelBedsV1Room = _commonMethod.GenerateRoomtypeDataTableNewV2();
                                DataTable dtHotelBedsV1RoomCacelationPolicy = _commonMethod.GenerateCancellationDataTableNewV2();
                                DataTable DtRoomRateBreackUp = _commonMethod.GenerateoomRateBreackUpDataTableNewV2();

                                #region "Room Logic"
                                if (SearchParam.Filters.IsShowRooms == 1)
                                {
                                    #region
                                    double AgentPercentage = 0.0;
                                    Boolean blnIsShowSupplier = false;
                                    double AgentMarkupAmount = 0.0;
                                    Boolean isPerce = true;
                                    double ratePerNight = 0;
                                    double VatonfirstnightPrice = 0;
                                    double VatOnPerNight = 0;

                                    double AgentCompanyPercentage = Convert.ToDouble(SearchParam.AgentCompanyPercentage);
                                    bool IsB2C = Convert.ToBoolean(Convert.ToByte(SearchParam.IsB2BOrB2C));

                                    _markup.getAgetMarkupSetting(Provider, GiataId.ToString(), commonSearch.dtSuplierMarkup, commonSearch.dtMarkupDetails, ref AgentPercentage, ref AgentMarkupAmount, ref blnIsShowSupplier, ref isPerce, commonSearch.AgentId, RatingHotel, SearchParam.CityID, SearchParam.CountryID.ToString(), SearchParam.CheckInDate);
                                    int maxOcupancy = 0;

                                    int totalAdult = 0;
                                    int totalChild = 0;

                                    for (int b = 0; b < SearchParam.Rooms.Count; b++)
                                    {
                                        totalAdult = Convert.ToInt32(SearchParam.Rooms[b].NoofAdults) + totalAdult;
                                        totalChild = Convert.ToInt32(SearchParam.Rooms[b].NoofChild) + totalChild;
                                        maxOcupancy = maxOcupancy + SearchParam.Rooms[b].NoofChild + SearchParam.Rooms[b].NoofAdults;
                                    }
                                    #endregion
                                    #region Supplier Room Code block

                                    #region Room List



                                    var DistinctRoomConfigurationId = Iwtx_Response.Hotels.Hotel.SelectMany(hotel => hotel.RoomTypeDetails.Rooms.Room).Select(room => room.RoomConfigurationId).Distinct().ToList();



                                    //DataView view1 = new DataView(drRoom.CopyToDataTable());
                                    //DataTable DistinctRoomConfigurationId = view1.ToTable(true, "RoomConfigurationId");
                                    //DistinctRoomConfigurationId.DefaultView.ToTable(true, "RoomConfigurationId");
                                    //for (int n = 0; n < DistinctRoomConfigurationId.DefaultView.Count; n++)

                                    for (int n = 0; n < DistinctRoomConfigurationId?.Count; n++)
                                    {
                                        int maxPax = 0;
                                        //int b =  ;
                                        int Adult_meal = 0;
                                        int Child_meal = 0;

                                        //for (int b = 0; b < SearchParam.Rooms.Count; b++)
                                        {
                                            Adult_meal = Convert.ToInt32(SearchParam.Rooms[n].NoofAdults) + Adult_meal;
                                            Child_meal = Convert.ToInt32(SearchParam.Rooms[n].NoofChild) + Child_meal;
                                            maxPax = maxPax + SearchParam.Rooms[n].NoofChild + SearchParam.Rooms[n].NoofAdults;
                                        }

                                        int RoomCount = n + 1;

                                        int RoomConfigurationId = Convert.ToInt32(DistinctRoomConfigurationId[n]);
                                        //int RoomConfigurationId = Convert.ToInt32(DistinctRoomConfigurationId.DefaultView[n]["RoomConfigurationId"]);

                                        //var filteredRooms = Iwtx_Response.Hotels.Hotel.SelectMany(hotel => hotel.RoomTypeDetails.Rooms.Room).Where(room => room.RoomConfigurationId == RoomConfigurationId).ToList();
                                        var filteredRooms = Iwtx_Response.Hotels.Hotel[h].RoomTypeDetails.Rooms.Room.Where(room => room.RoomConfigurationId == RoomConfigurationId).ToList();

                                        //  DataRow[] drRoomRate = drRoom.CopyToDataTable().Select("RoomConfigurationId = '" + RoomConfigurationId + "'");
                                        // for (int i = 0; i < drRoomRate.Length; i++)

                                        for (int i = 0; i < filteredRooms?.Count; i++)
                                        {
                                            //Added on 2-JAN-25

                                            if (_keySettings.IsRoomReturnActive && SearchParam.Rooms.Count == 1 && (i + 1) > Item.ReturnRooms && (dtHotelBedsV1Room.Rows.Count == Item.ReturnRooms)) //18-07-25
                                                break;

                                            string RoomStatus = Convert.ToString(filteredRooms[i].RoomStatus);

                                            if (RoomStatus == "OK")
                                            {
                                                // int RoomId = Convert.ToInt32(drRoomRate[i]["Room_Id"]);
                                                int RoomNo = Convert.ToInt32(filteredRooms[i].RoomNo);
                                                //int ContractTokenId = Convert.ToInt32(drRoomRate[i]["ContractTokenId"]);  
                                                string ContractTokenId = Convert.ToString(filteredRooms[i].ContractTokenId);  //29-Oct-2024
                                                string RoomTypeCode = Convert.ToString(filteredRooms[i].RoomTypeCode);
                                                string MealPlanCode = Convert.ToString(filteredRooms[i].MealPlanCode);
                                                string MealPlan = Convert.ToString(filteredRooms[i].MealPlan);


                                                if (MealPlan.ToLower() == "breakfast")
                                                {
                                                    int Adult_meal1 = 0;
                                                    int Child_meal1 = 0;
                                                    bool AdultCheckforBreakfast = false;
                                                    for (int r = 0; r < SearchParam.Rooms.Count; r++)
                                                    {
                                                        Adult_meal1 = Convert.ToInt32(SearchParam.Rooms[r].NoofAdults);
                                                        Child_meal1 = Convert.ToInt32(SearchParam.Rooms[r].NoofChild);
                                                        int totalpax = Adult_meal1 + Child_meal1;
                                                        if (totalpax > 2)
                                                        {
                                                            AdultCheckforBreakfast = true;
                                                            break;
                                                        }
                                                        //maxPax1 = maxPax1 + SearchParam.Rooms[r].NoofChild + SearchParam.Rooms[r].NoofAdults;                                                       
                                                        //if (maxPax1 > 2)
                                                        //    AdultCheckforBreakfast = true;
                                                    }
                                                    if (AdultCheckforBreakfast)
                                                    {
                                                        MealPlan = "Breakfast For Two";
                                                    }
                                                }


                                                //if (maxPax > 2 && MealPlan == "Breakfast")//  Due Ticket #32352 
                                                //{
                                                //    MealPlan = "Breakfast For Two";
                                                //}//changee...
                                                //if (DistinctRoomConfigurationId.DefaultView.Count > 1 && maxPax <= 2 && MealPlan == "Breakfast")
                                                //{
                                                //    int maxPax1 = 0;
                                                //    //int b =  ;
                                                //    int Adult_meal1 = 0;
                                                //    int Child_meal1 = 0;
                                                //    bool AdultCheckforBreakfast = false;
                                                //    for (int r = 0; r < SearchParam.Rooms.Count; r++)
                                                //    {
                                                //        Adult_meal1 = Convert.ToInt32(SearchParam.Rooms[r].NoofAdults) + Adult_meal1;
                                                //        Child_meal1 = Convert.ToInt32(SearchParam.Rooms[r].NoofChild) + Child_meal1; 
                                                //        //maxPax1 = maxPax1 + SearchParam.Rooms[r].NoofChild + SearchParam.Rooms[r].NoofAdults;                                                       
                                                //        //if (maxPax1 > 2)
                                                //        //    AdultCheckforBreakfast = true;
                                                //    }
                                                //    if (AdultCheckforBreakfast)
                                                //    {
                                                //        MealPlan = "Breakfast For Two";
                                                //    }
                                                //}


                                                string RoomType = Convert.ToString(filteredRooms[i].RoomType).Replace("'", "");
                                                double TotalRate = Convert.ToDouble(filteredRooms[i].TotalRate);
                                                string IWTX_Currency = Convert.ToString(filteredRooms[i].CurrCode);
                                                double room1Price = Convert.ToDouble(filteredRooms[i].Rate);
                                                string ContractLabel = "";
                                                if (filteredRooms[i].ContractLabel != null)
                                                    ContractLabel = Convert.ToString(filteredRooms[i].ContractLabel);

                                                //if (Iwtx_Response.Tables["Room"].Columns.Contains("ContractLabel"))
                                                //ContractLabel = Convert.ToString(filteredRooms[i].ContractLabel);
                                                string Uniqueid = Guid.NewGuid().ToString().Substring(0, 8);
                                                string roomTypeId = Convert.ToString(RoomTypeCode + "$$$" + MealPlanCode + "$$$" + i + "$$$" + RoomType + "$$$" + HotelCode + "$$$" + ContractTokenId + "$$$" + XmlCity + "$$$" + IWTX_Currency + "$$$" + room1Price + "$$$" + action + "$$$" + RoomConfigurationId + "$$$" + Uniqueid + "$$$" + Item.XmlNationality);//+ "$$$" + Guid.NewGuid().ToString().Substring(0, 8)
                                                                                                                                                                                                                                                                                                                                                                      //string roomTypeId = Convert.ToString(RoomTypeCode + "$$$" + MealPlanCode + "$$$" + RoomId + "$$$" + RoomType + "$$$" + HotelCode + "$$$" + ContractTokenId + "$$$" + XmlCity + "$$$" + IWTX_Currency + "$$$" + room1Price + "$$$" + action + "$$$" + RoomConfigurationId + "$$$" + Uniqueid + "$$$" + Item.XmlNationality);//+ "$$$" + Guid.NewGuid().ToString().Substring(0, 8) 
                                                                                                                                                                                                                                                                                                                                                                      //change RoomId - i

                                                if (IWTX_Currency.ToLower() != commonSearch.tokenData.SiteCurrency.ToLower())
                                                {
                                                    //DataTable dtInrCurrency = Common.GetCountryCurrency(IWTX_Currency);
                                                    //double CurrencyFactor1 = Convert.ToDouble(dtInrCurrency.Rows[0]["AmtToExch"]);
                                                    double CurrencyFactor1 = _commonMethod.GetROEByCurrency(IWTX_Currency, DtROE);
                                                    TotalRate = TotalRate * CurrencyFactor1;
                                                }
                                                else
                                                {
                                                    TotalRate = TotalRate * CurrencyFactorJacTravel;
                                                }

                                                int Adult = Convert.ToInt32(SearchParam.Rooms[n].NoofAdults);
                                                int Child = Convert.ToInt32(SearchParam.Rooms[n].NoofChild);


                                                #region RateBrekUp
                                                double Night_Price = 0.0;

                                                //for (int m = 0; m < SearchParam.NoofNights; m++)
                                                //{
                                                //    Night_Price = TotalRate / SearchParam.NoofNights;
                                                //    string newCheckInDate = Convert.ToString(SearchParam.CheckInDate);
                                                //    string newCheckOutDate = Convert.ToString(SearchParam.CheckOutDate);
                                                //    DateTime dtCheckInDate = DateTime.ParseExact(newCheckInDate, "dd/MM/yyyy", null);
                                                //    DateTime dtCheckOutDate = DateTime.ParseExact(newCheckOutDate, "dd/MM/yyyy", null);
                                                //    TimeSpan t = dtCheckOutDate - dtCheckInDate;
                                                //    #region Vat
                                                //    ratePerNight = _markup.ProcessHotelRateBreakupPrice_WithMarkup(1, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), Night_Price, commonSearch.B2CMarkup, commonSearch.IsParentId, isPerce, AgentPercentage, AgentMarkupAmount, commonSearch.SubAgentMarkup, ref VatOnPerNight);
                                                //    #endregion

                                                //    VatonfirstnightPrice += VatOnPerNight;
                                                //    DataRow drHotelBedsV1RoomRateBreackUp = DtRoomRateBreackUp.NewRow();
                                                //    drHotelBedsV1RoomRateBreackUp["SupplierName"] = "IWTX";
                                                //    drHotelBedsV1RoomRateBreackUp["OwnsystemHotelId"] = OwnsystemHotelId;
                                                //    drHotelBedsV1RoomRateBreackUp["ContractID"] = 0;
                                                //    drHotelBedsV1RoomRateBreackUp["RoomNo"] = RoomNo;
                                                //    drHotelBedsV1RoomRateBreackUp["RoomTypeId"] = roomTypeId;
                                                //    drHotelBedsV1RoomRateBreackUp["MealId"] = MealPlanCode;
                                                //    drHotelBedsV1RoomRateBreackUp["Meal"] = MealPlan;
                                                //    string rateWeekColumn = Convert.ToString(dtCheckInDate.Date.AddDays(m).DayOfWeek).Substring(0, 3);
                                                //    drHotelBedsV1RoomRateBreackUp["DateOfWeek"] = rateWeekColumn;
                                                //    DateTime rateDate = dtCheckInDate.Date.AddDays(m);
                                                //    drHotelBedsV1RoomRateBreackUp["Date"] = rateDate;
                                                //    drHotelBedsV1RoomRateBreackUp["Rate"] = ratePerNight;
                                                //    drHotelBedsV1RoomRateBreackUp["OrderNo"] = 1;
                                                //    drHotelBedsV1RoomRateBreackUp["RoomTypeName"] = RoomType;
                                                //    drHotelBedsV1RoomRateBreackUp["DateOfWeek"] = rateWeekColumn;
                                                //    drHotelBedsV1RoomRateBreackUp["Date"] = rateDate;
                                                //    drHotelBedsV1RoomRateBreackUp["BookingStatus"] = "Available";
                                                //    drHotelBedsV1RoomRateBreackUp["BoardTypeID"] = "";
                                                //    drHotelBedsV1RoomRateBreackUp["VatdiffAmount"] = VatOnPerNight;
                                                //    drHotelBedsV1RoomRateBreackUp["RateType"] = RoomType;
                                                //    drHotelBedsV1RoomRateBreackUp["SupplierCurrency"] = CurrencyFactorJacTravel;
                                                //    drHotelBedsV1RoomRateBreackUp["BuyingPrice"] = ratePerNight / CurrencyFactorJacTravel;
                                                //    DtRoomRateBreackUp.Rows.Add(drHotelBedsV1RoomRateBreackUp);
                                                //}

                                                #endregion

                                                #region Room     
                                                DataRow drHotelBedsV1Room = dtHotelBedsV1Room.NewRow();
                                                drHotelBedsV1Room["OwnsystemHotelId"] = OwnsystemHotelId;
                                                drHotelBedsV1Room["RoomTypeCode"] = "";
                                                drHotelBedsV1Room["BoardType"] = MealPlanCode;

                                                #region Vat
                                                double mainbuy = TotalRate;

                                                _markup.ProcessHotelRoom_WithMarkup(CurrencyFactorJacTravel, IWTX_Currency,
                        true, false, SearchParam.NoofNights, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), SearchParam.CheckInDate, SearchParam.CheckOutDate, ref TotalRate, ref drHotelBedsV1Room, isPerce, AgentPercentage, AgentMarkupAmount, commonSearch.SubAgentMarkup, commonSearch.B2CMarkup, commonSearch.IsParentId);
                                                #endregion

                                                #region essentiailinfo



                                                #endregion

                                                drHotelBedsV1Room["MealName"] = MealPlan;
                                                drHotelBedsV1Room["MealId"] = MealPlanCode;
                                                drHotelBedsV1Room["Type"] = "";
                                                drHotelBedsV1Room["Board"] = "";
                                                drHotelBedsV1Room["BookingStatus"] = "Available";
                                                drHotelBedsV1Room["UniqueNo"] = "";//Guid.NewGuid().ToString()
                                                drHotelBedsV1Room["ContractID"] = 0;

                                                drHotelBedsV1Room["RoomNo"] = RoomNo;
                                                drHotelBedsV1Room["BoardTypeID"] = "";
                                                drHotelBedsV1Room["AdultsOccupancy"] = Adult;
                                                drHotelBedsV1Room["ChildOccupancy"] = Child;
                                                drHotelBedsV1Room["RoomSequenceNo"] = "";
                                                //drHotelBedsV1Room["EssentialInformation"] = essentinfo;
                                                drHotelBedsV1Room["EssentialInformation"] = ContractLabel;
                                                drHotelBedsV1Room["Characteristic"] = "";
                                                drHotelBedsV1Room["SupplierName"] = Item.Supplier;

                                                drHotelBedsV1Room["OfferCode"] = "";
                                                drHotelBedsV1Room["OfferDescription"] = "";
                                                drHotelBedsV1Room["RoomTypeName"] = RoomType;
                                                drHotelBedsV1Room["RoomTypeId"] = roomTypeId;


                                                dtHotelBedsV1Room.Rows.Add(drHotelBedsV1Room);
                                                #endregion

                                                #region Policy
                                                string EssentialInfo = "";
                                                //string FromDate = "";
                                                DateTime MainDate = new DateTime();
                                                string NonRefPolicy1 = Convert.ToString(filteredRooms[i].NonRefundable);//Add on 16-JAN-2025 k2
                                                                                                                        //string NonRefPolicy1 = Convert.ToString(drRoomRate[i]["NonRefundable"]);//Add on 16-JAN-2025 k2

                                                if (NonRefPolicy1.ToLower() == "n")//Add on 16-JAN-2025 k2
                                                {
                                                    //if (Iwtx_Response.Tables.Contains("CancellationPolicyDetails"))
                                                    if (filteredRooms[i].CancellationPolicyDetails != null)
                                                    {
                                                        var cancellationList = filteredRooms[i].CancellationPolicyDetails.Cancellation;
                                                        //DataRow[] drCancelPolicies = Iwtx_Response.Tables["CancellationPolicyDetails"].Select("Room_Id='" + RoomId + "'");
                                                        if (cancellationList?.Count > 0)
                                                        {
                                                            //int CancelPolicies_Id = Convert.ToInt32(drCancelPolicies[0]["CancellationPolicyDetails_Id"]);
                                                            //DataRow[] drCancelPenalty = Iwtx_Response.Tables["Cancellation"].Select("CancellationPolicyDetails_Id='" + CancelPolicies_Id + "'");
                                                            //if (drCancelPenalty.Length > 0)
                                                            if (cancellationList?.Count > 0)
                                                            {
                                                                //for (int p = 0; p < drCancelPenalty.Length; p++)
                                                                for (int p = 0; p < cancellationList?.Count; p++)
                                                                {
                                                                    string FromDate = Convert.ToString(cancellationList[p].FromDate);
                                                                    string FD = String.Format("{0:MM/dd/yyyy}", DateTime.ParseExact(FromDate, "yyyyMMdd", null));
                                                                    DateTime OnlyDate = DateTime.ParseExact(FD, "MM/dd/yyyy", null);
                                                                    if (p == 0)
                                                                        MainDate = OnlyDate;
                                                                    else if (OnlyDate < MainDate)
                                                                        MainDate = OnlyDate;
                                                                }
                                                            }
                                                            else
                                                            {
                                                                MainDate = DateTime.Now.Date;
                                                            }
                                                        }
                                                        else
                                                        {
                                                            string NonRefPolicy = Convert.ToString(filteredRooms[i].NonRefundable);
                                                            //string NonRefPolicy = Convert.ToString(drRoomRate[i]["NonRefundable"]);
                                                            if (false)// (NonRefPolicy.ToLower() == "n")//Add false on 16-JAN-2025 k2
                                                            {
                                                                MainDate = DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null);
                                                            }
                                                            else
                                                            {
                                                                MainDate = DateTime.Now.Date;
                                                            }

                                                        }
                                                    }
                                                    else
                                                    {
                                                        string NonRefPolicy = Convert.ToString(filteredRooms[i].NonRefundable);
                                                        //string NonRefPolicy = Convert.ToString(drRoomRate[i]["NonRefundable"]);
                                                        if (false)// (NonRefPolicy.ToLower() == "n")//Add false on 16-JAN-2025 k2
                                                        {
                                                            MainDate = DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null);
                                                        }
                                                        else
                                                        {
                                                            MainDate = DateTime.Now.Date;
                                                        }

                                                    }
                                                }
                                                else
                                                {
                                                    MainDate = DateTime.Now.Date;
                                                }


                                                DateTime StartDate = MainDate;
                                                double Penalty = Convert.ToDouble(mainbuy);


                                                #region Cancellation Policy Logic and Row
                                                DateTime CancelDate = DateTime.Now.Date;
                                                CancelDate = StartDate;

                                                #region Vat
                                                double VatOnPolicy = 0;
                                                double MainAgentCharge = Penalty;
                                                double CompanyChargeAmount = Penalty;
                                                Penalty = _markup.ProcessHotelCancelationPrice_WithMarkup(ref MainAgentCharge, SearchParam.NoofNights, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), Penalty, isPerce, commonSearch.B2CMarkup, AgentPercentage, AgentMarkupAmount, commonSearch.IsParentId, commonSearch.SubAgentMarkup, ref VatOnPolicy);
                                                #endregion



                                                DateTime NowDate = DateTime.Now.Date;
                                                if (CancelDate.AddDays(CutOffDays).Date > DateTime.Now.Date)
                                                {
                                                    StartDate = StartDate.AddDays(CutOffDays).Date;
                                                    CancelDate = CancelDate.AddDays(CutOffDays).Date;
                                                }
                                                else
                                                {
                                                    CancelDate = DateTime.Now.Date;
                                                }
                                                if (NowDate < CancelDate)
                                                {
                                                    double zero = 0;

                                                    dtHotelBedsV1RoomCacelationPolicy.Rows.Add(_commonMethod.setCancellationPolicyRow(ref zero, dtHotelBedsV1RoomCacelationPolicy.NewRow(), 1, IWTX_Currency, Item.Supplier, Convert.ToString(OwnsystemHotelId), RoomNo.ToString(), roomTypeId, RoomType, MealPlanCode, DateTime.Now.Date, CancelDate.AddDays(-1), 0, 0, "0", VatOnPolicy, "", "NO", "", EssentialInfo, "", ""));
                                                    dtHotelBedsV1RoomCacelationPolicy.AcceptChanges();

                                                    dtHotelBedsV1RoomCacelationPolicy.Rows.Add(_commonMethod.setCancellationPolicyRow(ref CompanyChargeAmount, dtHotelBedsV1RoomCacelationPolicy.NewRow(), 1, IWTX_Currency, Item.Supplier, Convert.ToString(OwnsystemHotelId), RoomNo.ToString(), roomTypeId, RoomType, MealPlanCode, Convert.ToDateTime(CancelDate), DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null), MainAgentCharge, Penalty, "0", VatOnPolicy, "", "NO", "", EssentialInfo, "", ""));
                                                    dtHotelBedsV1RoomCacelationPolicy.AcceptChanges();
                                                }
                                                else
                                                {
                                                    dtHotelBedsV1RoomCacelationPolicy.Rows.Add(_commonMethod.setCancellationPolicyRow(ref CompanyChargeAmount, dtHotelBedsV1RoomCacelationPolicy.NewRow(), 1, IWTX_Currency, Item.Supplier, Convert.ToString(OwnsystemHotelId), RoomNo.ToString(), roomTypeId, RoomType, MealPlanCode, DateTime.Now.Date, DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null), MainAgentCharge, Penalty, "0", VatOnPolicy, "", "Yes", "", EssentialInfo, "", ""));
                                                    dtHotelBedsV1RoomCacelationPolicy.AcceptChanges();
                                                }

                                                #endregion



                                            }
                                        }
                                    }




                                    #endregion




                                    #endregion
                                    #endregion
                                }
                                #endregion
                                #endregion

                                //Options ops = (await _supplierRes.GetCommonRoomForSuppliers(GiataId, commonSearch.tokenData, commonSearch.advancedOptions, MinPrice, drXmlHotel, dtHotelBedsV1Room, dtHotelBedsV1RoomCacelationPolicy, DtRoomRateBreackUp, SearchParam));
                                Options ops = (await _supplierRes.GetCommonRoomForSuppliers(GiataId, commonSearch.tokenData, commonSearch.advancedOptions, MinPrice, drXmlHotel, dtHotelBedsV1Room, dtHotelBedsV1RoomCacelationPolicy, DtRoomRateBreackUp, SearchParam, _keySettings.IsRoomReturnActive, Item.ReturnRooms, 1));//Added on 2-JAN-25
                                if (ops != null)
                                    options.Add(ops);
                            }
                            #endregion

                        }
                        #endregion
                    }
                    #endregion
                }
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
                    if (SearchParam != null && ResProccessTimerLogActive == "1" && timerroomx.ElapsedMilliseconds > Convert.ToInt32(ResProccessTimeValueforSaveLog))
                    {
                        timerroomx.Stop();
                        SearchParam.SupplierTime += ",|Finish:IWTX Process Response  " + "|Time :" + timerroomx.Elapsed.ToString();
                        await Task.Run(async () => await _commonMethod.FireAway_SQL_ResPorcessTimeLog("", timerroomx.Elapsed.ToString(), "IWTX", SearchParam.SearchKey, 1, SearchParam.AgentID));
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
        public async Task<List<Options>> IWTX_HotelSearchWithRooms_old(HotelX Item, CommonSearch commonSearch, string currency, string ResponseData, string CityCode, HotelSearchParameter SearchParam, double CurrencyFactorJacTravel, string Supplier, string action, DataSet dsAgentDetails, Int32 processTimeout, string ResProccessTimerLogActive, string ResProccessTimeValueforSaveLog, string StoreSupplierResErrorLog, DataTable DtROE, string ProccessCutOffTime)
        {
            System.Diagnostics.Stopwatch timerroomx = new System.Diagnostics.Stopwatch();
            timerroomx.Start();
            List<Options> options = new List<Options>();

            DataTable DtHotel = _commonMethod.GenerateDataTableNewV2();
            try
            {
                if (ResponseData != string.Empty)
                {
                    DataSet Iwtx_Response = new DataSet();
                    try
                    {
                        ResponseData = ResponseData.Replace("<CancellationPolicyDetails xsi:nil=\"true\" />", "<CancellationPolicyDetails/>");
                        Iwtx_Response = _commonMethod.GetDataSetFromXMLString(ResponseData);
                    }
                    catch (Exception)
                    {
                    }
                    if (Iwtx_Response == null)
                    {
                        Item.ResponseType = "NoCount";
                        return options;
                    }
                    if (!Iwtx_Response.Tables.Contains("Hotel"))
                    {
                        Item.ResponseType = "NoCount";
                        return options;
                    }

                    //  string Token = Convert.ToString(Adonis_Response.Tables["AdonisHotelSearchResultDTO"].Rows[0]["Token"]);
                    DataTable DtAdonis_HotelRoom = _commonMethod.GenerateRoomtypeDataTableNewV2();

                    for (int h = 0; h <= Iwtx_Response.Tables["Hotel"].Rows.Count - 1; h++)
                    {
                        //Check Time If High then return NoCount
                        if (ProccessCutOffTime == "1" && timerroomx.ElapsedMilliseconds > processTimeout)
                        {
                            timerroomx.Stop();
                            break;
                        }
                        //--end
                        Item.ResponseType = "Success";
                        string HotelCode = Convert.ToString(Iwtx_Response.Tables["Hotel"].Rows[h]["HotelCode"]);
                        string XmlCity = Convert.ToString(Iwtx_Response.Tables["Hotel"].Rows[h]["City"]);
                        int Hotel_Id = Convert.ToInt32(Iwtx_Response.Tables["Hotel"].Rows[h]["Hotel_Id"]);
                        double FinalTotal = 0;
                        #region "When not Need Room"  
                        // add hotel code from price only and room code blank here
                        if (SearchParam.Filters.IsShowRooms == 0)
                        {
                            #region "Hotel Code Block "
                            DataRow drXmlHotel = DtHotel.NewRow();
                            drXmlHotel["XMLHotelCode"] = HotelCode;
                            drXmlHotel["HotelName"] = "";
                            drXmlHotel["SupplierName"] = "iwtx";
                            drXmlHotel["OfficeID"] = 0;
                            drXmlHotel["ContractID"] = 0;
                            drXmlHotel["Rating"] = 0;
                            drXmlHotel["Reviews"] = 0;
                            drXmlHotel["UniqueNo"] = 0;
                            drXmlHotel["IsShowSupplier"] = false;
                            drXmlHotel["BookingStatus"] = "Available";
                            drXmlHotel["XMLCity"] = "";

                            double rate = 0, minRate = 0;
                            bool ProcessSkip = false;

                            DataRow[] drRoomTypeDetails = Iwtx_Response.Tables["RoomTypeDetails"].Select("Hotel_Id=" + Hotel_Id + "");
                            int RoomTypeDetails_Id = Convert.ToInt32(drRoomTypeDetails[0]["RoomTypeDetails_Id"]);

                            DataRow[] drRooms = Iwtx_Response.Tables["Rooms"].Select("RoomTypeDetails_Id=" + RoomTypeDetails_Id + "");
                            int Rooms_Id = Convert.ToInt32(drRooms[0]["Rooms_Id"]);

                            DataRow[] drRoom = Iwtx_Response.Tables["Room"].Select("Rooms_Id=" + Rooms_Id + "");

                            //Added on 22-JULY-2024
                            if (drRoom == null || drRoom.Length == 0)
                            {
                                continue;
                            }
                            //End

                            DataView view = new DataView(drRoom.CopyToDataTable());




                            minRate = GetChepestPriceIWTX(drRoom.CopyToDataTable(), SearchParam.Rooms.Count);

                            currency = Iwtx_Response.Tables["Room"].Rows[0]["CurrCode"].ToString(); // added 
                            double CurrencyFactor = 1;
                            if (currency.ToLower() != "usd")
                            {
                                CurrencyFactor = _commonMethod.GetROEByCurrency(currency, DtROE);
                            }
                            minRate = minRate * CurrencyFactor;

                            if (minRate == 0)
                                continue;

                            #region Old Code
                            //DataTable distinctValues = view.ToTable(true, "RatePlanId");

                            //for (int r = 0; r < distinctValues.Rows.Count; r++)
                            //{
                            //    DataRow[] drRoomRatePlan = Adonis_Response.Tables["Room"].Select("Rooms_Id='" + Rooms_Id + "' and RatePlanId='" + distinctValues.Rows[r]["RatePlanId"] + "'");
                            //    for (int p = 0; p < drRoomRatePlan.Length; p++)
                            //    {
                            //        rate = rate + Convert.ToDouble(drRoomRatePlan[p]["Rate"]);
                            //    }
                            //    if (r == 0)
                            //        minRate = rate;
                            //    else
                            //        if (rate < minRate)
                            //        minRate = rate;
                            //}
                            #endregion
                            drXmlHotel["SupplierCurrency"] = currency;
                            drXmlHotel["FinalSellingPrice"] = minRate;
                            drXmlHotel["SellingPrice"] = minRate;
                            drXmlHotel["OfferText"] = "";
                            DtHotel.Rows.Add(drXmlHotel);
                            FinalTotal = minRate;
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
                        }
                        #endregion
                        #region "When Need Rooms"
                        //add Hotel code blank and rooms code in room block
                        else
                        {
                            #region Hotels
                            #region "Hotel Code Block "
                            DataRow drXmlHotel = DtHotel.NewRow();
                            drXmlHotel["XMLHotelCode"] = HotelCode;
                            drXmlHotel["HotelName"] = "";
                            drXmlHotel["SupplierName"] = "iwtx";
                            drXmlHotel["OfficeID"] = 0;
                            drXmlHotel["ContractID"] = 0;
                            drXmlHotel["Rating"] = 0;
                            drXmlHotel["Reviews"] = 0;
                            drXmlHotel["UniqueNo"] = 0;
                            drXmlHotel["IsShowSupplier"] = false;
                            drXmlHotel["BookingStatus"] = "Available";
                            drXmlHotel["XMLCity"] = "";

                            double rate = 0, minRate = 0;
                            bool ProcessSkip = false;

                            DataRow[] drRoomTypeDetails = Iwtx_Response.Tables["RoomTypeDetails"].Select("Hotel_Id=" + Hotel_Id + "");
                            int RoomTypeDetails_Id = Convert.ToInt32(drRoomTypeDetails[0]["RoomTypeDetails_Id"]);

                            DataRow[] drRooms = Iwtx_Response.Tables["Rooms"].Select("RoomTypeDetails_Id=" + RoomTypeDetails_Id + "");
                            int Rooms_Id = Convert.ToInt32(drRooms[0]["Rooms_Id"]);

                            DataRow[] drRoom = Iwtx_Response.Tables["Room"].Select("Rooms_Id=" + Rooms_Id + "");

                            if (drRoom == null || drRoom.Length == 0)
                            {
                                continue;//Added on 22-JULY-2024
                            }

                            //DataView view = new DataView(drRoom.CopyToDataTable());

                            //DataTable distinctValues = view.ToTable(true, "RatePlanId");

                            //for (int r = 0; r < distinctValues.Rows.Count; r++)
                            //{
                            //    DataRow[] drRoomRatePlan = Adonis_Response.Tables["Room"].Select("Rooms_Id='" + Rooms_Id + "' and RatePlanId='" + distinctValues.Rows[r]["RatePlanId"] + "'");
                            //    for (int p = 0; p < drRoomRatePlan.Length; p++)
                            //    {
                            //        rate = rate + Convert.ToDouble(drRoomRatePlan[p]["Rate"]);
                            //    }
                            //    if (r == 0)
                            //        minRate = rate;
                            //    else
                            //        if (rate < minRate)
                            //        minRate = rate;
                            //}
                            minRate = 1;
                            drXmlHotel["SupplierCurrency"] = currency;
                            drXmlHotel["FinalSellingPrice"] = minRate;
                            drXmlHotel["SellingPrice"] = minRate;
                            drXmlHotel["OfferText"] = "";
                            DtHotel.Rows.Add(drXmlHotel);
                            FinalTotal = minRate;
                            #endregion
                            double Pricenew = minRate;
                            #region "GetRooms for this hotel"
                            bool willAddOrNot = true;
                            Int64 RaynaID = 0;
                            Int64 GiataId = 0;
                            int RatingHotel = 0;
                            Int64 OwnsystemHotelId = 0;
                            string Provider = "iwtx";
                            double MinPrice = Pricenew;

                            int CutOffDays = CacheMaster.CutOffDays;
                            CutOffDays = _commonMethod.GetCutOffDays(commonSearch.dtSuplierMarkup, Provider);

                            _markup.GetHotelDataCommon(ref MinPrice, ref willAddOrNot, ref OwnsystemHotelId, ref RaynaID, ref GiataId, ref RatingHotel, ref drXmlHotel, commonSearch.AgentId, commonSearch.TariffID, commonSearch.dsAgentDetails, commonSearch.supplierGiataMaster, SearchParam, commonSearch.dtCountryCityMarkup, commonSearch.B2CMarkup, commonSearch.IsParentId, commonSearch.SubAgentMarkup, commonSearch.SearchByHotel, commonSearch.HotelIDS);
                            if (willAddOrNot)
                            {
                                DataTable dtHotelBedsV1Room = _commonMethod.GenerateRoomtypeDataTableNewV2();
                                DataTable dtHotelBedsV1RoomCacelationPolicy = _commonMethod.GenerateCancellationDataTableNewV2();
                                DataTable DtRoomRateBreackUp = _commonMethod.GenerateoomRateBreackUpDataTableNewV2();

                                #region "Room Logic"
                                if (SearchParam.Filters.IsShowRooms == 1)
                                {
                                    #region
                                    double AgentPercentage = 0.0;
                                    Boolean blnIsShowSupplier = false;
                                    double AgentMarkupAmount = 0.0;
                                    Boolean isPerce = true;
                                    double ratePerNight = 0;
                                    double VatonfirstnightPrice = 0;
                                    double VatOnPerNight = 0;

                                    double AgentCompanyPercentage = Convert.ToDouble(SearchParam.AgentCompanyPercentage);
                                    bool IsB2C = Convert.ToBoolean(Convert.ToByte(SearchParam.IsB2BOrB2C));

                                    _markup.getAgetMarkupSetting(Provider, GiataId.ToString(), commonSearch.dtSuplierMarkup, commonSearch.dtMarkupDetails, ref AgentPercentage, ref AgentMarkupAmount, ref blnIsShowSupplier, ref isPerce, commonSearch.AgentId, RatingHotel, SearchParam.CityID, SearchParam.CountryID.ToString(), SearchParam.CheckInDate);
                                    int maxOcupancy = 0;

                                    int totalAdult = 0;
                                    int totalChild = 0;

                                    for (int b = 0; b < SearchParam.Rooms.Count; b++)
                                    {
                                        totalAdult = Convert.ToInt32(SearchParam.Rooms[b].NoofAdults) + totalAdult;
                                        totalChild = Convert.ToInt32(SearchParam.Rooms[b].NoofChild) + totalChild;
                                        maxOcupancy = maxOcupancy + SearchParam.Rooms[b].NoofChild + SearchParam.Rooms[b].NoofAdults;
                                    }
                                    #endregion
                                    #region Supplier Room Code block

                                    #region Room List


                                    DataView view1 = new DataView(drRoom.CopyToDataTable());
                                    DataTable DistinctRoomConfigurationId = view1.ToTable(true, "RoomConfigurationId");
                                    DistinctRoomConfigurationId.DefaultView.ToTable(true, "RoomConfigurationId");

                                    for (int n = 0; n < DistinctRoomConfigurationId.DefaultView.Count; n++)
                                    {
                                        int maxPax = 0;
                                        //int b =  ;
                                        int Adult_meal = 0;
                                        int Child_meal = 0;

                                        //for (int b = 0; b < SearchParam.Rooms.Count; b++)
                                        {
                                            Adult_meal = Convert.ToInt32(SearchParam.Rooms[n].NoofAdults) + Adult_meal;
                                            Child_meal = Convert.ToInt32(SearchParam.Rooms[n].NoofChild) + Child_meal;
                                            maxPax = maxPax + SearchParam.Rooms[n].NoofChild + SearchParam.Rooms[n].NoofAdults;
                                        }

                                        int RoomCount = n + 1;

                                        int RoomConfigurationId = Convert.ToInt32(DistinctRoomConfigurationId.DefaultView[n]["RoomConfigurationId"]);

                                        DataRow[] drRoomRate = drRoom.CopyToDataTable().Select("RoomConfigurationId = '" + RoomConfigurationId + "'");

                                        for (int i = 0; i < drRoomRate.Length; i++)
                                        {
                                            string RoomStatus = Convert.ToString(drRoomRate[i]["RoomStatus"]);

                                            if (RoomStatus == "OK")
                                            {
                                                int RoomId = Convert.ToInt32(drRoomRate[i]["Room_Id"]);
                                                int RoomNo = Convert.ToInt32(drRoomRate[i]["RoomNo"]);
                                                //int ContractTokenId = Convert.ToInt32(drRoomRate[i]["ContractTokenId"]);  
                                                string ContractTokenId = Convert.ToString(drRoomRate[i]["ContractTokenId"]);  //29-Oct-2024
                                                string RoomTypeCode = Convert.ToString(drRoomRate[i]["RoomTypeCode"]);
                                                string MealPlanCode = Convert.ToString(drRoomRate[i]["MealPlanCode"]);
                                                string MealPlan = Convert.ToString(drRoomRate[i]["MealPlan"]);


                                                if (MealPlan.ToLower() == "breakfast")
                                                {
                                                    int Adult_meal1 = 0;
                                                    int Child_meal1 = 0;
                                                    bool AdultCheckforBreakfast = false;
                                                    for (int r = 0; r < SearchParam.Rooms.Count; r++)
                                                    {
                                                        Adult_meal1 = Convert.ToInt32(SearchParam.Rooms[r].NoofAdults);
                                                        Child_meal1 = Convert.ToInt32(SearchParam.Rooms[r].NoofChild);
                                                        int totalpax = Adult_meal1 + Child_meal1;
                                                        if (totalpax > 2)
                                                        {
                                                            AdultCheckforBreakfast = true;
                                                            break;
                                                        }
                                                        //maxPax1 = maxPax1 + SearchParam.Rooms[r].NoofChild + SearchParam.Rooms[r].NoofAdults;                                                       
                                                        //if (maxPax1 > 2)
                                                        //    AdultCheckforBreakfast = true;
                                                    }
                                                    if (AdultCheckforBreakfast)
                                                    {
                                                        MealPlan = "Breakfast For Two";
                                                    }
                                                }


                                                //if (maxPax > 2 && MealPlan == "Breakfast")//  Due Ticket #32352 
                                                //{
                                                //    MealPlan = "Breakfast For Two";
                                                //}//changee...
                                                //if (DistinctRoomConfigurationId.DefaultView.Count > 1 && maxPax <= 2 && MealPlan == "Breakfast")
                                                //{
                                                //    int maxPax1 = 0;
                                                //    //int b =  ;
                                                //    int Adult_meal1 = 0;
                                                //    int Child_meal1 = 0;
                                                //    bool AdultCheckforBreakfast = false;
                                                //    for (int r = 0; r < SearchParam.Rooms.Count; r++)
                                                //    {
                                                //        Adult_meal1 = Convert.ToInt32(SearchParam.Rooms[r].NoofAdults) + Adult_meal1;
                                                //        Child_meal1 = Convert.ToInt32(SearchParam.Rooms[r].NoofChild) + Child_meal1; 
                                                //        //maxPax1 = maxPax1 + SearchParam.Rooms[r].NoofChild + SearchParam.Rooms[r].NoofAdults;                                                       
                                                //        //if (maxPax1 > 2)
                                                //        //    AdultCheckforBreakfast = true;
                                                //    }
                                                //    if (AdultCheckforBreakfast)
                                                //    {
                                                //        MealPlan = "Breakfast For Two";
                                                //    }
                                                //}


                                                string RoomType = Convert.ToString(drRoomRate[i]["RoomType"]).Replace("'", "");
                                                double TotalRate = Convert.ToDouble(drRoomRate[i]["TotalRate"]);
                                                string IWTX_Currency = Convert.ToString(drRoomRate[i]["CurrCode"]);
                                                double room1Price = Convert.ToDouble(drRoomRate[i]["Rate"]);
                                                string ContractLabel = "";
                                                if (Iwtx_Response.Tables["Room"].Columns.Contains("ContractLabel"))
                                                    ContractLabel = Convert.ToString(drRoomRate[i]["ContractLabel"]);
                                                string Uniqueid = Guid.NewGuid().ToString().Substring(0, 8);
                                                string roomTypeId = Convert.ToString(RoomTypeCode + "$$$" + MealPlanCode + "$$$" + RoomId + "$$$" + RoomType + "$$$" + HotelCode + "$$$" + ContractTokenId + "$$$" + XmlCity + "$$$" + IWTX_Currency + "$$$" + room1Price + "$$$" + action + "$$$" + RoomConfigurationId + "$$$" + Uniqueid + "$$$" + Item.XmlNationality);//+ "$$$" + Guid.NewGuid().ToString().Substring(0, 8)

                                                {
                                                    if (IWTX_Currency.ToLower() != "usd")
                                                    {
                                                        //DataTable dtInrCurrency = Common.GetCountryCurrency(IWTX_Currency);
                                                        //double CurrencyFactor1 = Convert.ToDouble(dtInrCurrency.Rows[0]["AmtToExch"]);
                                                        double CurrencyFactor1 = _commonMethod.GetROEByCurrency(IWTX_Currency, DtROE);
                                                        TotalRate = TotalRate * CurrencyFactor1;
                                                    }
                                                    else
                                                    {
                                                        TotalRate = TotalRate * CurrencyFactorJacTravel;
                                                    }
                                                }

                                                int Adult = Convert.ToInt32(SearchParam.Rooms[n].NoofAdults);
                                                int Child = Convert.ToInt32(SearchParam.Rooms[n].NoofChild);


                                                #region RateBrekUp
                                                double Night_Price = 0.0;

                                                for (int m = 0; m < SearchParam.NoofNights; m++)
                                                {
                                                    Night_Price = TotalRate / SearchParam.NoofNights;
                                                    string newCheckInDate = Convert.ToString(SearchParam.CheckInDate);
                                                    string newCheckOutDate = Convert.ToString(SearchParam.CheckOutDate);
                                                    DateTime dtCheckInDate = DateTime.ParseExact(newCheckInDate, "dd/MM/yyyy", null);
                                                    DateTime dtCheckOutDate = DateTime.ParseExact(newCheckOutDate, "dd/MM/yyyy", null);
                                                    TimeSpan t = dtCheckOutDate - dtCheckInDate;
                                                    #region Vat
                                                    ratePerNight = _markup.ProcessHotelRateBreakupPrice_WithMarkup(1, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), Night_Price, commonSearch.B2CMarkup, commonSearch.IsParentId, isPerce, AgentPercentage, AgentMarkupAmount, commonSearch.SubAgentMarkup, ref VatOnPerNight);
                                                    #endregion

                                                    VatonfirstnightPrice += VatOnPerNight;
                                                    DataRow drHotelBedsV1RoomRateBreackUp = DtRoomRateBreackUp.NewRow();
                                                    drHotelBedsV1RoomRateBreackUp["SupplierName"] = "IWTX";
                                                    drHotelBedsV1RoomRateBreackUp["OwnsystemHotelId"] = OwnsystemHotelId;
                                                    drHotelBedsV1RoomRateBreackUp["ContractID"] = 0;
                                                    drHotelBedsV1RoomRateBreackUp["RoomNo"] = RoomNo;
                                                    drHotelBedsV1RoomRateBreackUp["RoomTypeId"] = roomTypeId;
                                                    drHotelBedsV1RoomRateBreackUp["MealId"] = MealPlanCode;
                                                    drHotelBedsV1RoomRateBreackUp["Meal"] = MealPlan;
                                                    string rateWeekColumn = Convert.ToString(dtCheckInDate.Date.AddDays(m).DayOfWeek).Substring(0, 3);
                                                    drHotelBedsV1RoomRateBreackUp["DateOfWeek"] = rateWeekColumn;
                                                    DateTime rateDate = dtCheckInDate.Date.AddDays(m);
                                                    drHotelBedsV1RoomRateBreackUp["Date"] = rateDate;
                                                    drHotelBedsV1RoomRateBreackUp["Rate"] = ratePerNight;
                                                    drHotelBedsV1RoomRateBreackUp["OrderNo"] = 1;
                                                    drHotelBedsV1RoomRateBreackUp["RoomTypeName"] = RoomType;
                                                    drHotelBedsV1RoomRateBreackUp["DateOfWeek"] = rateWeekColumn;
                                                    drHotelBedsV1RoomRateBreackUp["Date"] = rateDate;
                                                    drHotelBedsV1RoomRateBreackUp["BookingStatus"] = "Available";
                                                    drHotelBedsV1RoomRateBreackUp["BoardTypeID"] = "";
                                                    drHotelBedsV1RoomRateBreackUp["VatdiffAmount"] = VatOnPerNight;
                                                    drHotelBedsV1RoomRateBreackUp["RateType"] = RoomType;
                                                    drHotelBedsV1RoomRateBreackUp["SupplierCurrency"] = CurrencyFactorJacTravel;
                                                    drHotelBedsV1RoomRateBreackUp["BuyingPrice"] = ratePerNight / CurrencyFactorJacTravel;
                                                    DtRoomRateBreackUp.Rows.Add(drHotelBedsV1RoomRateBreackUp);
                                                }

                                                #endregion

                                                #region Room     
                                                DataRow drHotelBedsV1Room = dtHotelBedsV1Room.NewRow();
                                                drHotelBedsV1Room["OwnsystemHotelId"] = OwnsystemHotelId;
                                                drHotelBedsV1Room["RoomTypeCode"] = "";
                                                drHotelBedsV1Room["BoardType"] = MealPlanCode;

                                                #region Vat
                                                double mainbuy = TotalRate;

                                                _markup.ProcessHotelRoom_WithMarkup(CurrencyFactorJacTravel, IWTX_Currency,
                        true, false, SearchParam.NoofNights, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), SearchParam.CheckInDate, SearchParam.CheckOutDate, ref TotalRate, ref drHotelBedsV1Room, isPerce, AgentPercentage, AgentMarkupAmount, commonSearch.SubAgentMarkup, commonSearch.B2CMarkup, commonSearch.IsParentId);
                                                #endregion

                                                #region essentiailinfo



                                                #endregion

                                                drHotelBedsV1Room["MealName"] = MealPlan;
                                                drHotelBedsV1Room["MealId"] = MealPlanCode;
                                                drHotelBedsV1Room["Type"] = "";
                                                drHotelBedsV1Room["Board"] = "";
                                                drHotelBedsV1Room["BookingStatus"] = "Available";
                                                drHotelBedsV1Room["UniqueNo"] = "";//Guid.NewGuid().ToString()
                                                drHotelBedsV1Room["ContractID"] = 0;

                                                drHotelBedsV1Room["RoomNo"] = RoomNo;
                                                drHotelBedsV1Room["BoardTypeID"] = "";
                                                drHotelBedsV1Room["AdultsOccupancy"] = Adult;
                                                drHotelBedsV1Room["ChildOccupancy"] = Child;
                                                drHotelBedsV1Room["RoomSequenceNo"] = "";
                                                //drHotelBedsV1Room["EssentialInformation"] = essentinfo;
                                                drHotelBedsV1Room["EssentialInformation"] = ContractLabel;
                                                drHotelBedsV1Room["Characteristic"] = "";
                                                drHotelBedsV1Room["SupplierName"] = "IWTX";

                                                drHotelBedsV1Room["OfferCode"] = "";
                                                drHotelBedsV1Room["OfferDescription"] = "";
                                                drHotelBedsV1Room["RoomTypeName"] = RoomType;
                                                drHotelBedsV1Room["RoomTypeId"] = roomTypeId;


                                                dtHotelBedsV1Room.Rows.Add(drHotelBedsV1Room);
                                                #endregion

                                                #region Policy
                                                string EssentialInfo = "";
                                                //string FromDate = "";
                                                DateTime MainDate = new DateTime();
                                                string NonRefPolicy1 = Convert.ToString(drRoomRate[i]["NonRefundable"]);//Add on 16-JAN-2025 k2
                                                if (NonRefPolicy1.ToLower() == "n")//Add on 16-JAN-2025 k2
                                                {
                                                    if (Iwtx_Response.Tables.Contains("CancellationPolicyDetails"))
                                                    {
                                                        DataRow[] drCancelPolicies = Iwtx_Response.Tables["CancellationPolicyDetails"].Select("Room_Id='" + RoomId + "'");
                                                        if (drCancelPolicies.Length > 0)
                                                        {
                                                            int CancelPolicies_Id = Convert.ToInt32(drCancelPolicies[0]["CancellationPolicyDetails_Id"]);
                                                            DataRow[] drCancelPenalty = Iwtx_Response.Tables["Cancellation"].Select("CancellationPolicyDetails_Id='" + CancelPolicies_Id + "'");

                                                            if (drCancelPenalty.Length > 0)
                                                            {
                                                                for (int p = 0; p < drCancelPenalty.Length; p++)
                                                                {
                                                                    string FromDate = Convert.ToString(drCancelPenalty[p]["FromDate"]);
                                                                    string FD = String.Format("{0:MM/dd/yyyy}", DateTime.ParseExact(FromDate, "yyyyMMdd", null));
                                                                    DateTime OnlyDate = DateTime.ParseExact(FD, "MM/dd/yyyy", null);
                                                                    if (p == 0)
                                                                        MainDate = OnlyDate;
                                                                    else if (OnlyDate < MainDate)
                                                                        MainDate = OnlyDate;
                                                                }
                                                            }
                                                            else
                                                            {
                                                                MainDate = DateTime.Now.Date;
                                                            }
                                                        }
                                                        else
                                                        {
                                                            string NonRefPolicy = Convert.ToString(drRoomRate[i]["NonRefundable"]);
                                                            if (false)// (NonRefPolicy.ToLower() == "n")//Add false on 16-JAN-2025 k2
                                                            {
                                                                MainDate = DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null);
                                                            }
                                                            else
                                                            {
                                                                MainDate = DateTime.Now.Date;
                                                            }

                                                        }
                                                    }
                                                    else
                                                    {
                                                        string NonRefPolicy = Convert.ToString(drRoomRate[i]["NonRefundable"]);
                                                        if (false)// (NonRefPolicy.ToLower() == "n")//Add false on 16-JAN-2025 k2
                                                        {
                                                            MainDate = DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null);
                                                        }
                                                        else
                                                        {
                                                            MainDate = DateTime.Now.Date;
                                                        }

                                                    }
                                                }
                                                else
                                                {
                                                    MainDate = DateTime.Now.Date;
                                                }


                                                DateTime StartDate = MainDate;
                                                double Penalty = Convert.ToDouble(mainbuy);


                                                #region Cancellation Policy Logic and Row
                                                DateTime CancelDate = DateTime.Now.Date;
                                                CancelDate = StartDate;

                                                #region Vat
                                                double VatOnPolicy = 0;
                                                double MainAgentCharge = Penalty;
                                                double CompanyChargeAmount = Penalty;
                                                Penalty = _markup.ProcessHotelCancelationPrice_WithMarkup(ref MainAgentCharge, SearchParam.NoofNights, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), Penalty, isPerce, commonSearch.B2CMarkup, AgentPercentage, AgentMarkupAmount, commonSearch.IsParentId, commonSearch.SubAgentMarkup, ref VatOnPolicy);
                                                #endregion



                                                DateTime NowDate = DateTime.Now.Date;
                                                if (CancelDate.AddDays(CutOffDays).Date > DateTime.Now.Date)
                                                {
                                                    StartDate = StartDate.AddDays(CutOffDays).Date;
                                                    CancelDate = CancelDate.AddDays(CutOffDays).Date;
                                                }
                                                else
                                                {
                                                    CancelDate = DateTime.Now.Date;
                                                }
                                                if (NowDate < CancelDate)
                                                {
                                                    double zero = 0;

                                                    dtHotelBedsV1RoomCacelationPolicy.Rows.Add(_commonMethod.setCancellationPolicyRow(ref zero, dtHotelBedsV1RoomCacelationPolicy.NewRow(), 1, IWTX_Currency, "IWTX", Convert.ToString(OwnsystemHotelId), RoomNo.ToString(), roomTypeId, RoomType, MealPlanCode, DateTime.Now.Date, CancelDate.AddDays(-1), 0, 0, "0", VatOnPolicy, "", "NO", "", EssentialInfo, "", ""));
                                                    dtHotelBedsV1RoomCacelationPolicy.AcceptChanges();

                                                    dtHotelBedsV1RoomCacelationPolicy.Rows.Add(_commonMethod.setCancellationPolicyRow(ref CompanyChargeAmount, dtHotelBedsV1RoomCacelationPolicy.NewRow(), 1, IWTX_Currency, "IWTX", Convert.ToString(OwnsystemHotelId), RoomNo.ToString(), roomTypeId, RoomType, MealPlanCode, Convert.ToDateTime(CancelDate), DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null), MainAgentCharge, Penalty, "0", VatOnPolicy, "", "NO", "", EssentialInfo, "", ""));
                                                    dtHotelBedsV1RoomCacelationPolicy.AcceptChanges();
                                                }
                                                else
                                                {
                                                    dtHotelBedsV1RoomCacelationPolicy.Rows.Add(_commonMethod.setCancellationPolicyRow(ref CompanyChargeAmount, dtHotelBedsV1RoomCacelationPolicy.NewRow(), 1, IWTX_Currency, "IWTX", Convert.ToString(OwnsystemHotelId), RoomNo.ToString(), roomTypeId, RoomType, MealPlanCode, DateTime.Now.Date, DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null), MainAgentCharge, Penalty, "0", VatOnPolicy, "", "Yes", "", EssentialInfo, "", ""));
                                                    dtHotelBedsV1RoomCacelationPolicy.AcceptChanges();
                                                }

                                                #endregion

                                                #endregion

                                            }
                                        }
                                    }




                                    #endregion
                                    #endregion
                                }
                                #endregion
                                #endregion

                                Options ops = (await _supplierRes.GetCommonRoomForSuppliers(GiataId, commonSearch.tokenData, commonSearch.advancedOptions, MinPrice, drXmlHotel, dtHotelBedsV1Room, dtHotelBedsV1RoomCacelationPolicy, DtRoomRateBreackUp, SearchParam));
                                if (ops != null)
                                    options.Add(ops);
                            }
                            #endregion

                        }
                        #endregion
                    }
                }
            }
            catch (Exception e)
            {
                Item.ResponseType = "NoCount";
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
                        SearchParam.SupplierTime += ",|Finish:IWTX Process Response  " + "|Time :" + timerroomx.Elapsed.ToString();
                        _ = Task.Run(() => _commonMethod.FireAway_SQL_ResPorcessTimeLog("", timerroomx.Elapsed.ToString(), "IWTX", SearchParam.SearchKey, 1, SearchParam.AgentID));
                    }
                }
                catch (Exception x)
                {
                    await _exception.Error(x);
                }
                #endregion

                if (Item.ResponseType == "NoCount" && StoreSupplierResErrorLog == "1")
                    _ = Task.Run(() => _commonMethod.XMLRequestResponseTrack_Error(Item.RQRS[0].Request, Item.RQRS[0].Response, Item.Supplier, timerroomx.Elapsed.ToString(), "", SearchParam.SearchKey));
            }
            return options;
        }

        public static double GetChepestPriceIWTX(DataTable IWTXRoom, int NoOfRooms)
        {
            double NetPrice = 0;
            double Price = 0;
            double NewPrice = 0;
            double LastPrice = 0;
            bool isValid = true;
            bool IsDefault = true;
            bool Ischeck = true;
            int combination = 0;
            double PriceDIff = 0;
            bool IscheckNew = true;



            if (NoOfRooms == 1)
            {
                DataRow[] drRoomIWTX = IWTXRoom.Select("RoomNumber=" + 1);

                if (drRoomIWTX.Length > 0)
                {
                    for (int j = 0; j < drRoomIWTX.Length; j++)
                    {
                        Price = Convert.ToDouble(drRoomIWTX[j]["Rate"]);
                        if (j == 0)
                        {
                            NetPrice = Price;
                        }
                        else if (NetPrice > Price)
                        {
                            NetPrice = Price;
                        }
                    }
                }

            }
            else if (NoOfRooms == 2)
            {
                DataRow[] drRoom1IWTX = IWTXRoom.Select("RoomNumber=" + 1);
                DataRow[] drRoom2IWTX = IWTXRoom.Select("RoomNumber=" + 2);


                if (drRoom1IWTX.Length > 0 && drRoom2IWTX.Length > 0)
                {

                    for (int j = 0; j < drRoom1IWTX.Length; j++)
                    {
                        for (int k = 0; k < drRoom2IWTX.Length; k++)
                        {

                            if (Convert.ToString(drRoom1IWTX[j]["MealPlan"]) == Convert.ToString(drRoom2IWTX[k]["MealPlan"]))
                            {
                                Price = Convert.ToDouble(drRoom1IWTX[j]["Rate"]) +
                                               Convert.ToDouble(drRoom1IWTX[k]["Rate"]);
                                combination = combination + 1;
                                if (k == 0 || IsDefault == true)
                                {
                                    if (Price == 0)
                                    {
                                        IsDefault = true;
                                    }
                                    else
                                    {
                                        NewPrice = Price;
                                        IsDefault = false;
                                    }

                                }
                                else if (NewPrice > Price)
                                {
                                    NewPrice = Price;
                                }
                            }

                        }
                        if (j == 0 || isValid == true)
                        {
                            if (NewPrice == 0)
                            {
                                isValid = true;
                            }
                            else
                            {
                                NetPrice = NewPrice;
                                isValid = false;
                            }
                        }
                        else if (NetPrice > NewPrice)
                        {
                            NetPrice = NewPrice;
                        }
                    }
                }
            }
            else if (NoOfRooms == 3)
            {
                DataRow[] drRoom1IWTX = IWTXRoom.Select("RoomNumber=" + 1);
                DataRow[] drRoom2IWTX = IWTXRoom.Select("RoomNumber=" + 2);
                DataRow[] drRoom3IWTX = IWTXRoom.Select("RoomNumber=" + 3);

                if (drRoom1IWTX.Length > 0 && drRoom2IWTX.Length > 0 && drRoom3IWTX.Length > 0)
                {
                    for (int j = 0; j < drRoom1IWTX.Length; j++)
                    {
                        for (int k = 0; k < drRoom2IWTX.Length; k++)
                        {
                            for (int l = 0; l < drRoom3IWTX.Length; l++)
                            {
                                if (Convert.ToString(drRoom1IWTX[j]["MealPlan"]) == Convert.ToString(drRoom2IWTX[k]["MealPlan"]) &&
                                    Convert.ToString(drRoom2IWTX[k]["MealPlan"]) == Convert.ToString(drRoom3IWTX[l]["MealPlan"]) &&
                                    Convert.ToString(drRoom3IWTX[l]["MealPlan"]) == Convert.ToString(drRoom1IWTX[j]["MealPlan"]))
                                {
                                    Price = Convert.ToDouble(drRoom1IWTX[j]["Rate"]) +
                                               Convert.ToDouble(drRoom1IWTX[k]["Rate"]) +
                                               Convert.ToDouble(drRoom1IWTX[l]["Rate"]);
                                    combination = combination + 1;
                                    if (l == 0 || Ischeck == true)
                                    {
                                        if (Price == 0)
                                        {
                                            Ischeck = true;
                                        }
                                        else
                                        {
                                            NewPrice = Price;
                                            Ischeck = false;
                                        }

                                    }
                                    else if (NewPrice > Price)
                                    {
                                        NewPrice = Price;
                                    }
                                }
                            }
                            if (k == 0 || isValid == true)
                            {
                                if (NewPrice == 0)
                                {
                                    isValid = true;
                                }
                                else
                                {
                                    LastPrice = NewPrice;
                                    isValid = false;
                                }

                            }
                            else if (LastPrice > NewPrice)
                            {
                                LastPrice = NewPrice;
                            }
                        }


                        if (j == 0 || IsDefault == true)
                        {
                            if (LastPrice == 0)
                            {
                                IsDefault = true;
                            }
                            else
                            {
                                NetPrice = LastPrice;
                                IsDefault = false;
                            }

                        }
                        else if (NetPrice > LastPrice)
                        {
                            NetPrice = LastPrice;
                        }
                    }
                }
            }
            else
            {
                DataRow[] drRoom1IWTX = IWTXRoom.Select("RoomNumber=" + 1);
                DataRow[] drRoom2IWTX = IWTXRoom.Select("RoomNumber=" + 2);
                DataRow[] drRoom3IWTX = IWTXRoom.Select("RoomNumber=" + 3);
                DataRow[] drRoom4IWTX = IWTXRoom.Select("RoomNumber=" + 4);


                if (drRoom1IWTX.Length > 0 && drRoom2IWTX.Length > 0 && drRoom3IWTX.Length > 0 && drRoom4IWTX.Length > 0)
                {
                    for (int j = 0; j < drRoom1IWTX.Length; j++)
                    {
                        for (int k = 0; k < drRoom2IWTX.Length; k++)
                        {
                            for (int l = 0; l < drRoom3IWTX.Length; l++)
                            {
                                for (int m = 0; m < drRoom4IWTX.Length; m++)
                                {
                                    if (Convert.ToString(drRoom1IWTX[j]["MealPlan"]) == Convert.ToString(drRoom2IWTX[k]["MealPlan"]) &&
                                   Convert.ToString(drRoom2IWTX[k]["MealPlan"]) == Convert.ToString(drRoom3IWTX[l]["MealPlan"]) &&
                                   Convert.ToString(drRoom3IWTX[l]["MealPlan"]) == Convert.ToString(drRoom4IWTX[m]["MealPlan"]))
                                    {
                                        PriceDIff = Convert.ToDouble(drRoom1IWTX[j]["Rate"]) +
                                                   Convert.ToDouble(drRoom2IWTX[k]["Rate"]) +
                                                   Convert.ToDouble(drRoom3IWTX[l]["Rate"]) +
                                                   Convert.ToDouble(drRoom4IWTX[m]["Rate"]);
                                        combination = combination + 1;

                                        if (m == 0 || IscheckNew == true)
                                        {
                                            if (PriceDIff == 0)
                                            {
                                                IscheckNew = true;
                                            }
                                            else
                                            {
                                                Price = PriceDIff;
                                                IscheckNew = false;
                                            }

                                        }
                                        else if (Price > PriceDIff)
                                        {
                                            Price = PriceDIff;
                                        }
                                    }
                                }
                                if (l == 0 || Ischeck == true)
                                {
                                    if (Price == 0)
                                    {
                                        Ischeck = true;
                                    }
                                    else
                                    {
                                        NewPrice = Price;
                                        Ischeck = false;
                                    }

                                }
                                else if (NewPrice > Price)
                                {
                                    NewPrice = Price;
                                }

                            }
                            if (k == 0 || isValid == true)
                            {
                                if (NewPrice == 0)
                                {
                                    isValid = true;
                                }
                                else
                                {
                                    LastPrice = NewPrice;
                                    isValid = false;
                                }

                            }
                            else if (LastPrice > NewPrice)
                            {
                                LastPrice = NewPrice;
                            }
                        }


                        if (j == 0 || IsDefault == true)
                        {
                            if (LastPrice == 0)
                            {
                                IsDefault = true;
                            }
                            else
                            {
                                NetPrice = LastPrice;
                                IsDefault = false;
                            }

                        }
                        else if (NetPrice > LastPrice)
                        {
                            NetPrice = LastPrice;
                        }
                    }
                }
            }
            return NetPrice;
        }

        public static double GetChepestPriceIWTXnew(IWIXHotelList.Iwtx_main Iwtx_Response, int NoOfRooms, int h)
        {
            var DistinctRoomNumber = Iwtx_Response.Hotels.Hotel.SelectMany(hotel => hotel.RoomTypeDetails.Rooms.Room).Select(room => room.RoomNumber).Distinct().ToList();

            double NetPrice = 0;
            double Price = 0.0;
            double totalprice = 0.0;
            bool IsDefault = true;
            double NewPrice = 0;
            int combination = 0;
            bool isValid = true;
            bool Ischeck = true;
            double LastPrice = 0;

            if (NoOfRooms == 1) //need to change 
            {
                for (int rtj = 0; rtj < DistinctRoomNumber?.Count; rtj++)
                {
                    int RoomCount = rtj + 1;

                    int Roomnum = Convert.ToInt32(DistinctRoomNumber[rtj]);

                    var filteredRoomslist = Iwtx_Response.Hotels.Hotel[h].RoomTypeDetails.Rooms.Room.Where(room => room.RoomNumber == 1).ToList();
                    for (int j = 0; j < filteredRoomslist?.Count; j++)
                    {
                        Price = Convert.ToDouble(filteredRoomslist[j].Rate);
                        if (j == 0)
                        {
                            NetPrice = Price;
                        }
                        else if (NetPrice > Price)
                        {
                            NetPrice = Price;
                        }

                    }
                }



            }
            else if (NoOfRooms == 2) //need to change 
            {


                for (int rtj = 0; rtj < DistinctRoomNumber?.Count; rtj++)
                {
                    int RoomCount = rtj + 1;

                    int Roomnum = Convert.ToInt32(DistinctRoomNumber[rtj]);

                    var filteredRoomslist = Iwtx_Response.Hotels.Hotel[h].RoomTypeDetails.Rooms.Room.Where(room => room.RoomNumber == 1).ToList();
                    var filteredRoomslist2 = Iwtx_Response.Hotels.Hotel[h].RoomTypeDetails.Rooms.Room.Where(room => room.RoomNumber == 2).ToList();

                    for (int j = 0; j < filteredRoomslist?.Count; j++)
                    {
                        for (int k = 0; k < filteredRoomslist2?.Count; k++)
                        {
                            string mealplan = Convert.ToString(filteredRoomslist[j].MealPlan);
                            string mealplan2 = Convert.ToString(filteredRoomslist[k].MealPlan);
                            if (mealplan == mealplan2)
                            {
                                Price = Convert.ToDouble(filteredRoomslist[j].Rate) + Convert.ToDouble(filteredRoomslist[k].Rate);
                                combination = combination + 1;
                                if (k == 0 || IsDefault == true)
                                {
                                    if (Price == 0)
                                    {
                                        IsDefault = true;
                                    }
                                    else
                                    {
                                        NewPrice = Price;
                                        IsDefault = false;
                                    }

                                }
                                else if (NewPrice > Price)
                                {
                                    NewPrice = Price;
                                }
                            }
                        }
                        if (j == 0 || isValid == true)
                        {
                            if (NewPrice == 0)
                            {
                                isValid = true;
                            }
                            else
                            {
                                NetPrice = NewPrice;
                                isValid = false;
                            }
                        }
                        else if (NetPrice > NewPrice)
                        {
                            NetPrice = NewPrice;
                        }
                    }
                }
            }
            else
            {
                for (int rtj = 0; rtj < DistinctRoomNumber?.Count; rtj++)
                {
                    int RoomCount = rtj + 1;

                    int Roomnum = Convert.ToInt32(DistinctRoomNumber[rtj]);

                    var filteredRoomslist = Iwtx_Response.Hotels.Hotel[h].RoomTypeDetails.Rooms.Room.Where(room => room.RoomNumber == 1).ToList();
                    var filteredRoomslist2 = Iwtx_Response.Hotels.Hotel[h].RoomTypeDetails.Rooms.Room.Where(room => room.RoomNumber == 2).ToList();
                    var filteredRoomslist3 = Iwtx_Response.Hotels.Hotel[h].RoomTypeDetails.Rooms.Room.Where(room => room.RoomNumber == 3).ToList();

                    for (int j = 0; j < filteredRoomslist?.Count; j++)
                    {
                        for (int k = 0; k < filteredRoomslist2?.Count; k++)
                        {
                            for (int l = 0; l < filteredRoomslist3?.Count; l++)
                            {
                                string mealplan = Convert.ToString(filteredRoomslist[j].MealPlan);
                                string mealplan2 = Convert.ToString(filteredRoomslist[k].MealPlan);
                                string mealplan3 = Convert.ToString(filteredRoomslist[l].MealPlan);
                                if (mealplan == mealplan2 && mealplan2 == mealplan3 && mealplan3 == mealplan)
                                {
                                    Price = Convert.ToDouble(filteredRoomslist[j].Rate) + Convert.ToDouble(filteredRoomslist[k].Rate) + Convert.ToDouble(filteredRoomslist[l].Rate);
                                    combination = combination + 1;
                                    if (l == 0 || Ischeck == true)
                                    {
                                        if (Price == 0)
                                        {
                                            Ischeck = true;
                                        }
                                        else
                                        {
                                            NewPrice = Price;
                                            Ischeck = false;
                                        }

                                    }
                                    else if (NewPrice > Price)
                                    {
                                        NewPrice = Price;
                                    }
                                }
                            }
                            if (k == 0 || isValid == true)
                            {
                                if (NewPrice == 0)
                                {
                                    isValid = true;
                                }
                                else
                                {
                                    LastPrice = NewPrice;
                                    isValid = false;
                                }

                            }
                            else if (LastPrice > NewPrice)
                            {
                                LastPrice = NewPrice;
                            }

                        }
                        if (j == 0 || IsDefault == true)
                        {
                            if (LastPrice == 0)
                            {
                                IsDefault = true;
                            }
                            else
                            {
                                NetPrice = LastPrice;
                                IsDefault = false;
                            }

                        }
                        else if (NetPrice > LastPrice)
                        {
                            NetPrice = LastPrice;
                        }


                    }
                }
            }
            return NetPrice;
        }

        //Recheck
        public async Task<string> Get_IWTX_EInfo(string hotelcode, string ownsystemid, string roomtypeid, HotelSearchParameter SearchPara, string Password, string Action, string Username, string SearchKey, string provider)
        {
            ResponseDto responseDto = new ResponseDto();

            string Essen = "";
            try
            {
                string essentinfo = "";

                #region Hoteldetails
                try
                {
                    //string IWTXUrl = "https://api.iwtxconnect.com/api/v1/details";

                    string Request = HotelDetailsRequest_IWTX(hotelcode, Username, Password, Action);
                   
                    //responseDto = await IWTXResponseNeAsync(IWTXInfo.IWTXUrl, Request, provider, SearchKey, "xml", true);
                    responseDto = await IWTXResponseNeAsync(_supplierConfig.IWTX.IWTXUrl, Request, provider, SearchKey, "xml", true);
                    string ResponseData = responseDto.Response;
                    _commonMethod.XMLRequestResponseTrack_MainDB(Request, ResponseData, provider, Action, "IWTX_HoteldetailsResponse", SearchKey);
                    #region RemoveNode

                    string I = ResponseData;
                    var doc = XDocument.Parse(I);
                    var nodeToRemove = doc.Descendants("Images");
                    nodeToRemove.Remove();
                    ResponseData = doc.ToString();

                    string W = ResponseData;
                    var doc1 = XDocument.Parse(W);
                    var nodeToRemove1 = doc1.Descendants("HotelFacilities");
                    nodeToRemove1.Remove();
                    ResponseData = doc1.ToString();

                    string T = ResponseData;
                    var doc2 = XDocument.Parse(T);
                    var nodeToRemove2 = doc2.Descendants("GeoLocation");
                    nodeToRemove2.Remove();
                    ResponseData = doc2.ToString();

                    #endregion

                    DataSet IWTX_Response1 = _commonMethod.GetDataSetFromXMLString(ResponseData);

                    if (IWTX_Response1.Tables.Contains("Message") == true)
                    {
                        for (int HD = 0; HD < IWTX_Response1.Tables["Message"].Rows.Count; HD++)
                        {
                            string MessageShort = Convert.ToString(IWTX_Response1.Tables["Message"].Rows[HD]["MessageShort"]);
                            string MessageFull = Convert.ToString(IWTX_Response1.Tables["Message"].Rows[HD]["MessageFull"]);
                            string ChargeBase = "";
                            for (int r = 0; r < SearchPara.Rooms.Count(); r++)
                            {
                                int EXAdult = Convert.ToInt32(SearchPara.Rooms[r].NoofAdults);
                                int EXChild = Convert.ToInt32(SearchPara.Rooms[r].NoofChild);
                                int Totaloccupancy = EXAdult + EXChild;
                                if (Totaloccupancy > 2)
                                {
                                    try//try added on 31-DEC-2024 K2
                                    {
                                        ChargeBase = "ChargeBase : " + Convert.ToString(IWTX_Response1.Tables["Message"].Rows[HD]["MessageChargeBase"]) + " " + Convert.ToString(IWTX_Response1.Tables["Message"].Rows[HD]["Value"]) + " " + Convert.ToString(IWTX_Response1.Tables["Message"].Rows[HD]["Currency"]);
                                    }
                                    catch (Exception ex)
                                    {
                                        //_ = _exception.Error(ex, SearchKey);

                                        //_ = _exception.ErrorNew(ex, _LogAPI.Recheck, SearchKey);
                                    }
                                }
                            }
                            essentinfo = essentinfo + "<br>" + MessageShort + " : " + MessageFull + "<br>" + ChargeBase;
                        }
                    }
                    if (IWTX_Response1.Tables.Contains("Details") == true)
                    {
                        if (IWTX_Response1.Tables["Details"].Columns.Contains("Description") == true)
                        {
                            essentinfo = essentinfo + "<br>" + Convert.ToString(IWTX_Response1.Tables["Details"].Rows[0]["Description"]);
                        }
                    }
                    return essentinfo;
                }
                catch (Exception ex)
                {
                    _ = _exception.ErrorNew(ex, _LogAPI.Recheck, SearchKey);
                }

                #endregion
            }
            catch (Exception ex)
            {
                _ = _exception.ErrorNew(ex, _LogAPI.Recheck, SearchKey);

            }

            return Essen;
        }

        public string HotelDetailsRequest_IWTX(string HotelId, string Username, string Password, string Uniqueid)
        {
            try
            {
                StringBuilder HotelDetails = new StringBuilder();
                if (HotelId != string.Empty)
                {
                    HotelDetails.Append("<HotelDetailsRequest>");

                    HotelDetails.Append("<OutputFormat>XML</OutputFormat>");

                    HotelDetails.Append("<Profile>");
                    HotelDetails.Append("<Password>" + Password + "</Password>");
                    HotelDetails.Append("<Code>" + Username + "</Code>");
                    //HotelDetails.Append("<Password>D3V_1234</Password>");
                    //HotelDetails.Append("<Code>DEV_IWTX</Code>");
                    HotelDetails.Append("<TokenNumber>" + Uniqueid + "</TokenNumber>");
                    HotelDetails.Append("</Profile>");

                    HotelDetails.Append("<SearchCriteria>");
                    HotelDetails.Append("<HotelCode>" + HotelId + "</HotelCode>");
                    HotelDetails.Append("</SearchCriteria>");
                    HotelDetails.Append("</HotelDetailsRequest>");
                }
                string HotelRequest = HotelDetails.ToString();
                return HotelRequest;
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
                return "";
            }
        }

        public string IWTX_HoteldetailsResponse(string URL, string request, string Provider, string SearchKey)
        {
            //System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
            //timer.Start();

            string Response = "";
            try
            {
                //var client = new RestClient(URL);
                //var Request = new RestRequest(Method.POST);
                //Request.Timeout = 5000;


                var options = new RestClientOptions(URL)
                {
                    Timeout = TimeSpan.FromMilliseconds(5000)
                };

                //var client = new RestClient(options);
                //var Request = new RestRequest("", Method.Post);
                //Request.AddHeader("postman-token", "07c32c3d-9e12-32d0-85d5-e6e9cc3555ec");
                //Request.AddHeader("cache-control", "no-cache");
                //Request.AddHeader("content-type", "application/xml");
                //Request.AddParameter("application/xml", request, ParameterType.RequestBody);
                //IRestResponse response = client.Execute(Request);
                //Response = response.Content.ToString();

                var client = new RestClient(options);
                var Request = new RestRequest("", Method.Post);

                // Add headers
                Request.AddHeader("postman-token", "07c32c33555ec");
                Request.AddHeader("cache-control", "no-cache");
                Request.AddHeader("content-type", "application/xml");

                // Add XML body
                Request.AddStringBody(request, DataFormat.Xml); // <-- assuming 'requestXmlString' is your XML payload

                // Execute request
                var response = client.Execute(Request);
                Response = response.Content.ToString();

            }
            catch (Exception ex)
            {
                _commonMethod.XMLRequestResponseTrackError(request, ex.Message.ToString(), Provider == "" ? "Travco" : Provider, SearchKey);
                //Common.Error(ex);
            }
            return Response;
        }

        public async Task<CancellationDto> IWTX_cancellation(string SearchKey, string OwnsystemHotelId, string roomtypecode, DataSet dsHotel, string hotelcode, string ownsystemid, string roomtypeid, DataTable dtAgentDetail, DataTable dtSuplierMarkup, DataTable dtSearchConfig, HotelSearchParameter SearchPara, string roomno, string provider, int AgentId, DataTable dtMarkupDetails, TokenData tokenData, string GiataId = "0", int hotelRating = 0, string CompanyBuyPrice = "", int RoomNo = 0, string XmlCity = "", string Uniqueid = "", string XmlNationality = "", string ContractTokenId = "", string MealPlanCode = "", string TechName = "")
        {

            CancellationDto cancellationDto = new CancellationDto();
            string Error = "";
            string time = "";
            string ResponseData = "";
            string Request = "";
            string responsetype = "Success";
            double CompanyBuy = Convert.ToDouble(CompanyBuyPrice);
            string ISBooking = "NotAvailable";
            try
            {
                DataTable dtpaximumCancellationPolicy = _commonMethod.GenerateCancellationDataTableNewV2();
                int CutOffDays = XMLHotelBookingCommon.CutOffDays;
                CutOffDays = _commonMethod.GetCutOffDays(dtSuplierMarkup, provider);



                DataRow[] drSuplierMarkup = (dtSuplierMarkup.Select("SupplierName='" + provider + "'"));
                int SupplierId = Convert.ToInt32(drSuplierMarkup[0]["SupplierID"]);
                string PaximumCurrency = Convert.ToString(drSuplierMarkup[0]["CurrencyShortName"]);
                double CurrencyFactorPaximum = 1;
                if (PaximumCurrency.ToLower() != tokenData.SiteCurrency.ToLower())
                {
                    //DataTable dtInrCurrency = _commonMethod.GetCountryCurrency(PaximumCurrency);
                    //double CurrencyFactorPaximum = Convert.ToDouble(dtInrCurrency.Rows[0]["AmtToExch"]);
                    CurrencyFactorPaximum = _commonMethod.GetROEByCurrency(PaximumCurrency, tokenData.dtROE);
                }


                DataRow[] drSearchConfig = (dtSearchConfig.Select("SupplierID='" + SupplierId + "'"));
               // string IWTXURL = Convert.ToString(drSuplierMarkup[0]["WebSiteURL"]) + Convert.ToString(IWTXInfo.IWTX_PriceCheck); 
                string IWTXURL = Convert.ToString(drSuplierMarkup[0]["WebSiteURL"]) + Convert.ToString(_supplierConfig.IWTX.IWTX_PriceCheck); 
                //string URL = Cancelpolicy_Url + "search/checkAvailability";
                string Password = Convert.ToString(drSuplierMarkup[0]["Password"]);
                string Username = Convert.ToString(drSuplierMarkup[0]["Username"]);


                string essential = await Get_IWTX_EInfo(hotelcode, ownsystemid, roomtypeid, SearchPara, Password, Convert.ToString(drSuplierMarkup[0]["WebSiteURL"]), Username, SearchKey, provider);


                string roomtypename = "";
                string mealid = "";
                string token_pax = "";
                string Notes = "";

                string[] HBArray = Convert.ToString(roomtypeid).Split(new String[] { "$$$" }, StringSplitOptions.None);
                roomtypename = HBArray[3];

                int RoomConfigurationId = 0;
                Request = PriceCheckRequest_ForCancellationPolicy_IWTX(SearchPara.CheckInDate, SearchPara.CheckOutDate, hotelcode, Username, Password, SearchPara.Rooms[RoomNo - 1].NoofAdults, SearchPara.Rooms[RoomNo - 1].NoofChild, SearchPara.Rooms[RoomNo - 1].Child1Age, SearchPara.Rooms[RoomNo - 1].Child2Age, ref RoomConfigurationId, XmlCity, Uniqueid, XmlNationality, roomtypeid, ContractTokenId, MealPlanCode);

                System.Diagnostics.Stopwatch timerroomprocess = new System.Diagnostics.Stopwatch();
                timerroomprocess.Start();
                //ResponseDto responseDto = await IWTXResponseNeAsync(IWTXURL, Request, "IWTX", SearchKey);
                ResponseDto responseDto = await IWTXResponse(IWTXURL, Request, provider, SearchKey);
                ResponseData = responseDto.Response;
                Error = responseDto.Error;

                timerroomprocess.Stop();
                time = timerroomprocess.Elapsed.ToString();

                _commonMethod.ThirdPartyGetPolicyHistory(Convert.ToString(roomtypeid), Convert.ToString(ownsystemid), Request, Convert.ToString(ResponseData), "After Get Policy", provider, Error + "Response", SearchKey, "API");


                //if (paxresponse.cancellationPolicies.Count > 0)
                {
                    #region Remove Node

                    string I = ResponseData;
                    var doc = XDocument.Parse(I);
                    var nodeToRemove = doc.Descendants("BuyRate");
                    nodeToRemove.Remove();
                    ResponseData = doc.ToString();

                    string W = ResponseData;
                    var doc1 = XDocument.Parse(W);
                    var nodeToRemove1 = doc1.Descendants("CommissionSellCharges");
                    nodeToRemove1.Remove();
                    ResponseData = doc1.ToString();

                    string T = ResponseData;
                    var doc2 = XDocument.Parse(T);
                    var nodeToRemove2 = doc2.Descendants("TaxSellCharges");
                    nodeToRemove2.Remove();
                    ResponseData = doc2.ToString();

                    #endregion

                    ResponseData = ResponseData.Replace("<CancellationPolicyDetails xsi:nil=\"true\" />", "<CancellationPolicyDetails/>");
                    DataSet IWTX_Response = _commonMethod.GetDataSetFromXMLString(ResponseData);




                    #region canpolicy


                    //DataRow[] drRoomRate = IWTX_Response.Tables["Room"].Select("RoomConfigurationId = '" + RoomConfigurationId + "'");
                    DataRow[] drRoomRate = IWTX_Response.Tables["Room"].Select("RoomConfigurationId = '" + 1 + "'");//Added on 8-MAR-2025 k2
                    if (drRoomRate.Length > 0)
                    {
                        ISBooking = "Available";//Added on 4-4-2024
                    }
                    Boolean isPerce = true;
                    double AgentCompanyPercentage = Convert.ToDouble(SearchPara.AgentCompanyPercentage);
                    bool IsB2C = Convert.ToBoolean(Convert.ToByte(SearchPara.IsB2BOrB2C));
                    double B2CMarkup = (IsB2C == false ? AgentCompanyPercentage : XMLHotelBookingCommon.B2CMarkup);
                    double AgentPercentage = 0.0;
                    double AgentMarkupAmount = 0.0;
                    double SubAgentMarkup = 0;
                    int IsParentId = 1;
                    Boolean blnIsShowSupplier = false;
                    _markup.getAgetMarkupSetting(provider, GiataId, dtSuplierMarkup, dtMarkupDetails, ref AgentPercentage, ref AgentMarkupAmount, ref blnIsShowSupplier, ref isPerce, AgentId, hotelRating, SearchPara.CityID, Convert.ToString(SearchPara.CountryID) == null ? "0" : Convert.ToString(SearchPara.CountryID), Convert.ToString(SearchPara.CheckInDate));

                    if (dtAgentDetail != null)
                    {
                        IsParentId = Convert.ToInt32(dtAgentDetail.Rows[0]["IsParentId"]);
                        SubAgentMarkup = Convert.ToDouble(dtAgentDetail.Rows[0]["B2CMarkupPer"]);
                    }

                    string currency = Convert.ToString(PaximumCurrency);
                    double CancelAmount = 0;
                    DateTime Canceldate = DateTime.Now; ;

                    #region Policy
                    string EssentialInfo = essential;
                    //string FromDate = "";
                    DateTime MainDate = new DateTime();

                    #region For PriceCheck // as per Ahmed, date 12-APR-2024
                    string CrruncyFct = Convert.ToString(drRoomRate[0]["CurrCode"]);
                    double FinalPriceTemp = Convert.ToDouble(drRoomRate[0]["Rate"]);

                    if (CrruncyFct.ToLower() != tokenData.SiteCurrency.ToLower())
                    {
                        //DataTable dtInrCurrency1 = _commonMethod.GetCountryCurrency(CrruncyFct);
                        //double CurrencyFact = Convert.ToDouble(dtInrCurrency1.Rows[0]["AmtToExch"]);
                        double CurrencyFact = _commonMethod.GetROEByCurrency(CrruncyFct, tokenData.dtROE);
                        FinalPriceTemp = FinalPriceTemp * CurrencyFact;


                    }

                    FinalPriceTemp = Convert.ToDouble(Math.Round(FinalPriceTemp, 2));
                    if (Convert.ToDouble(Math.Round(FinalPriceTemp.TruncateDecimalPlaces(0))) > Math.Round(Convert.ToDouble(CompanyBuyPrice).TruncateDecimalPlaces(0)))
                    {
                        #region RPT GeneralError
                        try
                        {
                            ResponseData = "#Price Changed at Recheck " + "#Main Price = " + CompanyBuyPrice + " #New Price = " + FinalPriceTemp + " #Response= " + ResponseData;
                            responsetype = "GeneralError";
                            string abcd = await _commonMethod.AnalyticsRecheckData(provider, responsetype, time, SearchKey, TechName);
                            string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(provider, "PreBook", AgentId, time, responsetype, Request, ResponseData, Error);
                        }
                        catch (Exception e)
                        {
                            _exception.Error(e, "");
                        }
                        #endregion
                        ISBooking = "NotAvailable";
                    }
                    #endregion

                    string NonRefPolicy1 = Convert.ToString(drRoomRate[0]["NonRefundable"]);//Add on 16-JAN-2025 k2
                    if (NonRefPolicy1.ToLower() == "n")//Add on 16-JAN-2025 k2
                    {
                        if (IWTX_Response.Tables.Contains("CancellationPolicyDetails"))
                        {
                            DataRow[] drCancelPolicies = IWTX_Response.Tables["CancellationPolicyDetails"].Select("Room_Id='" + 0 + "'");
                            if (drCancelPolicies.Length > 0)
                            {
                                int CancelPolicies_Id = Convert.ToInt32(drCancelPolicies[0]["CancellationPolicyDetails_Id"]);
                                DataRow[] drCancelPenalty = IWTX_Response.Tables["Cancellation"].Select("CancellationPolicyDetails_Id='" + CancelPolicies_Id + "'");

                                if (drCancelPenalty.Length > 0)
                                {
                                    for (int p = 0; p < drCancelPenalty.Length; p++)
                                    {
                                        string FromDate = Convert.ToString(drCancelPenalty[p]["FromDate"]);
                                        string FD = String.Format("{0:MM/dd/yyyy}", DateTime.ParseExact(FromDate, "yyyyMMdd", null));
                                        DateTime OnlyDate = DateTime.ParseExact(FD, "MM/dd/yyyy", null);
                                        if (p == 0)
                                            MainDate = OnlyDate;
                                        else if (OnlyDate < MainDate)
                                            MainDate = OnlyDate;
                                    }
                                }
                                else
                                {
                                    MainDate = DateTime.Now.Date;
                                }
                            }
                            else
                            {
                                string NonRefPolicy = Convert.ToString(drRoomRate[0]["NonRefundable"]);
                                if (false)// (NonRefPolicy.ToLower() == "n") //Add false on 16-JAN-2025 k2
                                {
                                    MainDate = DateTime.ParseExact(SearchPara.CheckInDate, "dd/MM/yyyy", null);
                                }
                                else
                                {
                                    MainDate = DateTime.Now.Date;
                                }

                            }
                        }
                        else
                        {
                            string NonRefPolicy = Convert.ToString(drRoomRate[0]["NonRefundable"]);
                            if (false)// (NonRefPolicy.ToLower() == "n")//Add false on 16-JAN-2025 k2
                            {
                                MainDate = DateTime.ParseExact(SearchPara.CheckInDate, "dd/MM/yyyy", null);
                            }
                            else
                            {
                                MainDate = DateTime.Now.Date;
                            }
                        }
                    }
                    else//Add on 16-JAN-2025 k2
                    {
                        MainDate = DateTime.Now.Date;
                    }


                    DateTime StartDate = MainDate;
                    double Penalty = Convert.ToDouble(CompanyBuy);


                    #region Cancellation Policy Logic and Row
                    DateTime CancelDate = DateTime.Now.Date;
                    CancelDate = StartDate;

                    #region Vat
                    double VatOnPolicy = 0;
                    double MainAgentCharge = Penalty;
                    double CompanyChargeAmount = Penalty;
                    Penalty = _markup.ProcessHotelCancelationPrice_WithMarkup(ref MainAgentCharge, SearchPara.NoofNights, SearchPara.CityID, Convert.ToString(SearchPara.CountryID), Penalty, isPerce, B2CMarkup, AgentPercentage, AgentMarkupAmount, IsParentId, SubAgentMarkup, ref VatOnPolicy);
                    #endregion



                    DateTime NowDate = DateTime.Now.Date;
                    if (CancelDate.AddDays(CutOffDays).Date > DateTime.Now.Date)
                    {
                        StartDate = StartDate.AddDays(CutOffDays).Date;
                        CancelDate = CancelDate.AddDays(CutOffDays).Date;
                    }
                    else
                    {
                        CancelDate = DateTime.Now.Date;
                    }
                    if (NowDate < CancelDate)
                    {
                        double zero = 0;

                        dtpaximumCancellationPolicy.Rows.Add(_commonMethod.setCancellationPolicyRow(ref zero, dtpaximumCancellationPolicy.NewRow(), 1, currency, provider, Convert.ToString(OwnsystemHotelId), RoomNo.ToString(), roomtypeid, roomtypename, MealPlanCode, DateTime.Now.Date, CancelDate.AddDays(-1), 0, 0, "0", VatOnPolicy, "", "NO", "", EssentialInfo, "", ""));
                        dtpaximumCancellationPolicy.AcceptChanges();

                        dtpaximumCancellationPolicy.Rows.Add(_commonMethod.setCancellationPolicyRow(ref CompanyChargeAmount, dtpaximumCancellationPolicy.NewRow(), 1, currency, provider, Convert.ToString(OwnsystemHotelId), RoomNo.ToString(), roomtypeid, roomtypename, MealPlanCode, Convert.ToDateTime(CancelDate), DateTime.ParseExact(SearchPara.CheckInDate, "dd/MM/yyyy", null), MainAgentCharge, Penalty, "0", VatOnPolicy, "", "NO", "", EssentialInfo, "", ""));
                        dtpaximumCancellationPolicy.AcceptChanges();
                    }
                    else
                    {
                        dtpaximumCancellationPolicy.Rows.Add(_commonMethod.setCancellationPolicyRow(ref CompanyChargeAmount, dtpaximumCancellationPolicy.NewRow(), 1, currency, provider, Convert.ToString(OwnsystemHotelId), RoomNo.ToString(), roomtypeid, roomtypename, MealPlanCode, DateTime.Now.Date, DateTime.ParseExact(SearchPara.CheckInDate, "dd/MM/yyyy", null), MainAgentCharge, Penalty, "0", VatOnPolicy, "", "Yes", "", EssentialInfo, "", ""));
                        dtpaximumCancellationPolicy.AcceptChanges();
                    }

                    #endregion

                    #endregion

                    #region old

                    //#region Caculation
                    //#region Vat
                    //double VatOnPolicy = 0;
                    //double MainAgentCharge = CancelAmount;
                    //double CompanyChargeAmount = CancelAmount;
                    //double cancellationChargeAmount = Common.ProcessHotelCancelationPrice_WithMarkup(ref MainAgentCharge, SearchPara.Rooms.Count * SearchPara.NoofNights, SearchPara.CityID, SearchPara.CountryId, CancelAmount, isPerce, B2CMarkup, AgentPercentage, AgentMarkupAmount, IsParentId, SubAgentMarkup, ref VatOnPolicy);
                    //#endregion

                    //DateTime CancellationDate = DateTime.ParseExact(SearchPara.CheckInDate, "dd/MM/yyyy", CultureInfo.InvariantCulture).Date;
                    //DateTime NowDate = DateTime.Now.Date;
                    //double CancelCharge = cancellationChargeAmount;
                    //int CancelDAY = Canceldate.Date.Day;
                    //CancellationDate = Canceldate.AddDays(-CancelDAY);

                    //#endregion

                    //if (CancellationDate.AddDays(0).Date > DateTime.Now.Date)
                    //    CancellationDate = Canceldate.AddDays(CutOffDays).Date;
                    //if (NowDate.Date < CancellationDate.Date)
                    //{
                    //    double charge = 0;
                    //    dtpaximumCancellationPolicy.Rows.Add(Common.setCancellationPolicyRow(ref charge, dtpaximumCancellationPolicy.NewRow(), CurrencyFactorPaximum, currency, provider, ownsystemid, Convert.ToString(1),
                    //    roomtypeid, Convert.ToString(roomtypename), Convert.ToString(mealid), DateTime.Now.Date, CancellationDate.AddDays(-1).AddHours(23).AddMinutes(59).AddSeconds(59), 0, 0, "", VatOnPolicy, "", "", token_pax, Notes, "", ""));
                    //    dtpaximumCancellationPolicy.AcceptChanges();

                    //    dtpaximumCancellationPolicy.Rows.Add(Common.setCancellationPolicyRow(ref CompanyChargeAmount, dtpaximumCancellationPolicy.NewRow(), CurrencyFactorPaximum, currency, provider, ownsystemid, Convert.ToString(1),
                    //    roomtypeid, Convert.ToString(roomtypename), Convert.ToString(mealid), CancellationDate.Date, DateTime.ParseExact(SearchPara.CheckInDate, "dd/MM/yyyy", null), CancelCharge, CancelCharge, "", VatOnPolicy, "", "", token_pax, Notes, "", ""));
                    //    dtpaximumCancellationPolicy.AcceptChanges();
                    //}
                    //else
                    //{
                    //    DataRow drMercanHotelCancellation_Temp = dtpaximumCancellationPolicy.NewRow();
                    //    dtpaximumCancellationPolicy.Rows.Add(Common.setCancellationPolicyRow(ref CompanyChargeAmount, dtpaximumCancellationPolicy.NewRow(), CurrencyFactorPaximum, currency, provider, ownsystemid, Convert.ToString(1),
                    //   roomtypeid, Convert.ToString(roomtypename), Convert.ToString(mealid), DateTime.Now.Date, DateTime.ParseExact(SearchPara.CheckInDate, "dd/MM/yyyy", null), CancelCharge, CancelCharge, "", VatOnPolicy, "", "", token_pax, Notes, "", ""));
                    //    dtpaximumCancellationPolicy.AcceptChanges();
                    //}
                    #endregion

                    #endregion

                }
                dsHotel.Tables[2].Merge(dtpaximumCancellationPolicy);


                try
                {
                    responsetype = "Success";
                    string abcd = await _commonMethod.AnalyticsRecheckData(provider, responsetype, time, SearchKey, TechName);
                    string abc = await _commonMethod.SupplierPerformanceReportForRecheckOnly(provider, "PreBook", AgentId, time, responsetype, Request, ResponseData, Error);
                }
                catch (Exception e)
                {
                    _ = _exception.Error(e);
                }

            }
            catch (Exception ex)
            {
                try
                {
                    ISBooking = "NotAvailable";
                    responsetype = "GeneralError";
                    string abcd = await _commonMethod.AnalyticsRecheckData(provider, responsetype, time, SearchKey, TechName);
                    //int AgentId = Convert.ToInt32(Common.getSession("UserId") == "" ? "0" : Common.getSession("UserId"));
                    string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(provider, "PreBook", AgentId, time, responsetype, Request, ResponseData, Error + ex.Message);
                }
                catch (Exception e)
                {
                    _ = _exception.Error(e, SearchKey);
                }
                _ = _exception.Error(ex, SearchKey);
                return null;
            }

            cancellationDto.dsHotelData = dsHotel;
            cancellationDto.ISBooking = ISBooking;

            return cancellationDto;
        }

        public string PriceCheckRequest_ForCancellationPolicy_IWTX(string checkIn, string checkOut, string HotelId, string Username, string Password, int NoOfAdults, int NoofChild, int Child1Age, int Child2Age, ref int RoomConfigurationId, string XmlCity, string Uniqueid, string XmlNationality, string RoomTypeCode, string ContractTokenId, string MealPlanCode)
        {
            try
            {

                //string StartDate = String.Format("{0:yyyyMMdd}", DateTime.ParseExact(checkIn, "yyyy-MM-dd", null));
                string StartDate = checkIn.Substring(6, 4) + checkIn.Substring(3, 2) + checkIn.Substring(0, 2);
                string EndDate = checkOut.Substring(6, 4) + checkOut.Substring(3, 2) + checkOut.Substring(0, 2);

                StringBuilder objRequest = new StringBuilder();
                if (HotelId != string.Empty)
                {
                    string[] HBArray = Convert.ToString(RoomTypeCode).Split(new String[] { "$$$" }, StringSplitOptions.None);
                    string RoomType_Code = HBArray[0];
                    MealPlanCode = HBArray[1];
                    ContractTokenId = HBArray[5];
                    RoomConfigurationId = Convert.ToInt32(HBArray[10]);
                    string CityCode = HBArray[6];
                    string Unique_id = HBArray[11];
                    XmlNationality = HBArray[12];

                    objRequest.Append("<HotelSearchRequest>");
                    objRequest.Append("<OutputFormat>XML</OutputFormat>");

                    objRequest.Append("<Profile>");
                    objRequest.Append("<Password>" + Password + "</Password>");
                    objRequest.Append("<Code>" + Username + "</Code>");
                    //objRequest.Append("<Password>D3V_1234</Password>");
                    //objRequest.Append("<Code>DEV_IWTX</Code>");
                    objRequest.Append("<TokenNumber>" + Unique_id + "</TokenNumber>");
                    objRequest.Append("</Profile>");

                    objRequest.Append("<SearchCriteria>");
                    objRequest.Append("<CancellationPolicy>Y</CancellationPolicy>");


                    objRequest.Append("<RoomConfiguration>");
                    //for (int j = 0; j < dsCartDetails.Tables[1].DefaultView.Count; j++)
                    //{



                    //Child1Age = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[j]["ChildAge1"]);
                    //Child2Age = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[j]["ChildAge2"]);
                    ////RoomConfigurationId = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[j]["RoomNumber"]);
                    //NoOfAdults = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[j]["NoOfAdults"]);
                    //NoofChild = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[j]["NoofChild"]);

                    objRequest.Append("<Room>");
                    objRequest.Append("<RoomTypeCode>" + RoomType_Code + "</RoomTypeCode>");
                    objRequest.Append("<ContractTokenId>" + ContractTokenId + "</ContractTokenId>");
                    objRequest.Append("<RoomConfigurationId>" + RoomConfigurationId + "</RoomConfigurationId>");
                    objRequest.Append("<MealPlanCode>" + MealPlanCode + "</MealPlanCode>");
                    for (int i = 0; i < NoOfAdults; i++)
                    {
                        objRequest.Append("<Adult>");
                        objRequest.Append("<Age>" + 30 + "</Age>");
                        objRequest.Append("</Adult>");
                    }
                    if (NoofChild == 1)
                    {
                        objRequest.Append("<Child>");
                        objRequest.Append("<Age>" + Child1Age + "</Age>");
                        objRequest.Append("</Child>");
                    }
                    else if (NoofChild == 2)
                    {
                        objRequest.Append("<Child>");
                        objRequest.Append("<Age>" + Child1Age + "</Age>");
                        objRequest.Append("</Child>");
                        objRequest.Append("<Child>");
                        objRequest.Append("<Age>" + Child2Age + "</Age>");
                        objRequest.Append("</Child>");
                    }
                    objRequest.Append("</Room>");
                    //}
                    objRequest.Append("</RoomConfiguration>");


                    objRequest.Append("<StartDate>" + StartDate + "</StartDate>");
                    objRequest.Append("<EndDate>" + EndDate + "</EndDate>");
                    objRequest.Append("<City>" + CityCode + "</City>");
                    objRequest.Append("<HotelCode>" + HotelId + "</HotelCode>");
                    objRequest.Append("<IncludeOnRequest>N</IncludeOnRequest>");
                    objRequest.Append("<IncludeRateDetails>Y</IncludeRateDetails>");
                    objRequest.Append("<Nationality>" + XmlNationality + "</Nationality>");
                    objRequest.Append("</SearchCriteria>");
                    objRequest.Append("</HotelSearchRequest>");
                }
                string HotelRequest = objRequest.ToString();

                return HotelRequest;
            }
            catch (Exception ex)
            {
                _exception.ErrorNew(ex, _LogAPI.Recheck);
                return "";
            }
        }

        public async Task<ResponseDto> IWTXResponse(string URL, string request, string Provider, string SearchKey)
        {
            ResponseDto responseDto = new ResponseDto();
            string Error = "";
            System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
            timer.Start();

            //string IWTXURL = "https://secure.illusionswebservices.iolcloud.com/illusionsHotelSearch.ashx";
            string Response = "-";
            try
            {
                //var client = new RestClient("https://api.iwtxconnect.com/illusionsHotelSearch.ashx");
                ////client.Timeout = -1;
                //var Request = new RestRequest(Method.POST);
                //Request.Timeout = 60000;
                //Request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
                //Request.AddHeader("Cookie", "ApplicationGatewayAffinityCORS=801ef04732b03ca9de8640eb774ff903; ApplicationGatewayAffinity=801ef04732b03ca9de8640eb774ff903");
                //Request.AddParameter("data", request);
                //IRestResponse response = client.Execute(Request);
                ////Console.WriteLine(response.Content);
                //Response = response.Content.ToString();


                var options = new RestClientOptions("https://api.iwtxconnect.com/illusionsHotelSearch.ashx")
                {
                    Timeout = TimeSpan.FromMilliseconds(60000)
                };

                var client = new RestClient(options);
                var Request = new RestRequest("", Method.Post);

                // Add headers
                Request.AddHeader("Cookie", "ApplicationGatewayAffinityCORS=801ef04732b03ca9de8640eb774ff903; ApplicationGatewayAffinity=801ef04732b03ca9de8640eb774ff903");
                Request.AddHeader("cache-control", "no-cache");
                Request.AddHeader("content-type", "application/x-www-form-urlencoded");
                Request.AddParameter("data", request);
                // Add XML body
                // Request.AddStringBody(request, DataFormat.Json); // <-- assuming 'requestXmlString' is your XML payload

                // Execute request
                var response = await client.ExecuteAsync(Request);
                Response = response.Content.ToString();

                responseDto.Response = Response;
                responseDto.Error = "";

            }
            catch (Exception ex)
            {
                Response = "";
                Error = ex.Message;
                _ = _commonMethod.XMLRequestResponseTrack(request, ex.Message, Provider == "" ? "IWTX" : Provider, "Proper", "", SearchKey);
                _ = _exception.ErrorBooking(ex, "", "", SearchKey);

                responseDto.Response = Response;
                responseDto.Error = Error;
            }
            return responseDto;
        }



        public async Task<ResponseDto> IWTXResponseNeAsync(string strHost, string strxmlRQRS, string Provider, string SearchKey, string ResponseType = "xml", bool detailsApi = false)
        {
            ResponseDto responseDto = new ResponseDto();
            var timer = new System.Diagnostics.Stopwatch();
            timer.Start();
            string Error = "";
            string Response = "-";

            string type = "";
            string MediaType = "";
            if (ResponseType == "xml")
            {
                type = "application/xml";
                if (detailsApi)
                    MediaType = "text/xml";
                else
                    MediaType = "application/x-www-form-urlencoded";
            }
            else
            {
                type = "application/json";
                MediaType = "application/json";
            }

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    var request = new HttpRequestMessage(HttpMethod.Post, strHost)
                    {
                        Content = new StringContent(strxmlRQRS, System.Text.Encoding.UTF8)
                    };
                    //client.Timeout = 60000;

                    // Accept header
                    request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("text/xml"));
                    request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/xml");


                    //request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/x-www-form-urlencoded"));
                    //request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");

                    //request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(MediaType));
                    request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
                    request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("deflate"));

                    using (var responseMessage = await client.SendAsync(request))
                    {
                        var statusCode = (int)responseMessage.StatusCode;
                        Stream responseStream = await responseMessage.Content.ReadAsStreamAsync();

                        // Decompress if needed
                        if (responseMessage.Content.Headers.ContentEncoding.Contains("gzip"))
                            responseStream = new GZipStream(responseStream, CompressionMode.Decompress);
                        else if (responseMessage.Content.Headers.ContentEncoding.Contains("deflate"))
                            responseStream = new DeflateStream(responseStream, CompressionMode.Decompress);

                        using (StreamReader reader = new StreamReader(responseStream))
                        {
                            Response = await reader.ReadToEndAsync();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Response = "";
                Error = ex.Message;
                _commonMethod.XMLRequestResponseTrack(strxmlRQRS, ex.Message, string.IsNullOrEmpty(Provider) ? "IWTX" : Provider, "Proper", "", SearchKey);
                _exception.ErrorBooking(ex, "", "", SearchKey);
            }

            responseDto.Response = Response;
            responseDto.Error = Error;
            return responseDto;
        }


        public async Task<PriceCheckCMDto> PriceUpdate_IWTX(string RoomTypeCode, string MealPlanCode, string RoomType, string ContractTokenId, string XmlCity, string Uniqueid, int NoOfAdults, int NoofChild, int Child1Age, int Child2Age, int RoomConfigurationId, DataTable dtCheckPriceReqStructure, DataSet dsCartDetails, DataRow[] IWTXDetails, string UniqueNo, string AgentUniqueNo, int i, int j, string IsFor, double CompanyBuyPrice, bool IsError, double itemprice, string iwtxprice, int AgentId, string Searchkey, TokenData tokenData, string Supplier)
        {
            PriceCheckCMDto priceCheckCMDto = new PriceCheckCMDto();
            string responsetype = "Success";
            string time = "";
            string SupplierName = Supplier;
            string Error = "";
            string Request = "-";
            string ResponseData = "-";

            try
            {
                string IWTXCurrency = Convert.ToString(IWTXDetails[0]["CurrencyShortName"]);
                double CurrencyFactorIWTX = 1;
                if (IWTXCurrency.ToLower() != tokenData.SiteCurrency.ToLower())
                {
                    CurrencyFactorIWTX = _commonMethod.GetROEByCurrency(IWTXCurrency, tokenData.dtROE);
                }

                DataSet dsTemp = new DataSet();
                string UserId = Convert.ToString(IWTXDetails[0]["UserID"]);
                string UserName = Convert.ToString(IWTXDetails[0]["UserName"]);
                string Password = Convert.ToString(IWTXDetails[0]["Password"]);
                string RoomId = "";
                //string IWTXURL = "https://secure.illusionswebservices.iolcloud.com/illusionsHotelSearch.ashx";
               // string IWTXURL = Convert.ToString(IWTXDetails[0]["WebSiteURL"]) + Convert.ToString(IWTXInfo.IWTX_PriceCheck);
                string IWTXURL = Convert.ToString(IWTXDetails[0]["WebSiteURL"]) + Convert.ToString(_supplierConfig.IWTX.IWTX_PriceCheck);
                string HotelCode = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["XMLHotelId"]);
                string CheckIn = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["CheckInDate"]);
                CheckIn = CheckIn.Substring(0, 4) + "-" + CheckIn.Substring(4, 2) + "-" + CheckIn.Substring(6, 2);
                string CheckOut = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["CheckOutDate"]);
                CheckOut = CheckOut.Substring(0, 4) + "-" + CheckOut.Substring(4, 2) + "-" + CheckOut.Substring(6, 2);
                string XmlNationality = Convert.ToString(dsCartDetails.Tables[1].Rows[i]["XmlNationality"]);

                Request = PriceCheckRequest_IWTX(CheckIn, CheckOut, HotelCode, UserName, Password, NoOfAdults, NoofChild, Child1Age, Child2Age, RoomConfigurationId, XmlCity, Uniqueid, XmlNationality, RoomTypeCode, ContractTokenId, MealPlanCode, dsCartDetails);

                _commonMethod.ThirdPartyBookingHistory(Convert.ToString(UniqueNo), Convert.ToString(AgentUniqueNo), Request, "-", "Before Request hit Price Check" + IsFor, Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["SupplierName"]), "Request Confirm", "-", "WithInEarth",Searchkey);

                dtCheckPriceReqStructure.Rows.Add(_commonMethod.AddCheckPriceReq(dtCheckPriceReqStructure.NewRow(), j + 1, Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[j]["UserCartID"]), Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[j]["UserCartDetailId"]), Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomTypeID"]), Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["SupplierName"]).ToLower(), Request));

                ResponseDto responseDto = await IWTXResponse(IWTXURL, Request, SupplierName, Searchkey);
                ResponseData = responseDto.Response;
                Error = responseDto.Error;

                _commonMethod.ThirdPartyBookingHistory(Convert.ToString(UniqueNo), Convert.ToString(AgentUniqueNo), Request, ResponseData + Error, "After Request hit Price Check" + IsFor, Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["SupplierName"]), "Response Confirm", "-", "WithInEarth",Searchkey);

                if (ResponseData.ToLower().Contains("errors") || ResponseData.Trim() == "")
                {
                    #region RPT GeneralError
                    try
                    {
                        responsetype = "GeneralError";
                        string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, Request, ResponseData, Error);
                    }
                    catch (Exception e)
                    {
                        _ = _exception.Error(e, Searchkey);
                    }
                    #endregion
                    priceCheckCMDto.Flag = true;
                    return priceCheckCMDto;
                }


                #region Remove Node

                string I = ResponseData;
                var doc = XDocument.Parse(I);
                var nodeToRemove = doc.Descendants("BuyRate");
                nodeToRemove.Remove();
                ResponseData = doc.ToString();

                string W = ResponseData;
                var doc1 = XDocument.Parse(W);
                var nodeToRemove1 = doc1.Descendants("CommissionSellCharges");
                nodeToRemove1.Remove();
                ResponseData = doc1.ToString();

                string T = ResponseData;
                var doc2 = XDocument.Parse(T);
                var nodeToRemove2 = doc2.Descendants("TaxSellCharges");
                nodeToRemove2.Remove();
                ResponseData = doc2.ToString();

                #endregion

                ResponseData = ResponseData.Replace("<CancellationPolicyDetails xsi:nil=\"true\" />", "<CancellationPolicyDetails/>");
                DataSet IWTX_Response = _commonMethod.GetDataSetFromXMLString(ResponseData);

                if (IWTX_Response == null)
                {
                    #region RPT GeneralError
                    try
                    {
                        responsetype = "GeneralError";
                        string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, Request, ResponseData, Error);
                    }
                    catch (Exception e)
                    {
                        _ = _exception.Error(e, Searchkey);
                    }
                    #endregion
                    priceCheckCMDto.Flag = true;
                    return priceCheckCMDto;
                }

                if (IWTX_Response.Tables["Room"] != null)
                {
                    DataRow[] drRoomRate = IWTX_Response.Tables["Room"].Select("RoomTypeCode='" + RoomTypeCode + "' and MealPlanCode='" + MealPlanCode + "' and ContractTokenId='" + ContractTokenId + "'and RoomConfigurationId='" + RoomConfigurationId + "'");

                    for (int r = 0; r < drRoomRate.Count(); r++)
                    {

                        if (IWTX_Response.Tables["Room"].Columns.Contains("MealPlanCode"))

                        {
                            DataRow[] drMealPlanCode = IWTX_Response.Tables["Room"].Select();
                            string MealPlanCode1 = Convert.ToString(drMealPlanCode[r]["MealPlanCode"]);
                            if (MealPlanCode != MealPlanCode1)
                            {
                                #region RPT GeneralError
                                try
                                {
                                    responsetype = "GeneralError";
                                    string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, Request, ResponseData, Error);
                                }
                                catch (Exception e)
                                {
                                    _ = _exception.Error(e, Searchkey);
                                }
                                #endregion
                                priceCheckCMDto.Flag = true;
                                return priceCheckCMDto;
                            }
                            else
                            {
                                if (drRoomRate.Length > 0)
                                {
                                    double FinalPriceMain = 0;
                                    double FinalPriceTemp = Convert.ToDouble(drRoomRate[r]["Rate"]);
                                    string Currency = Convert.ToString(drRoomRate[r]["CurrCode"]);

                                    if (Currency.ToLower() != tokenData.SiteCurrency.ToLower())
                                    {
                                        double CurrencyFactorAdonis1 = _commonMethod.GetROEByCurrency(Currency, tokenData.dtROE);
                                        FinalPriceMain = FinalPriceTemp * CurrencyFactorAdonis1;
                                    }
                                    else
                                    {
                                        FinalPriceMain = FinalPriceTemp * CurrencyFactorIWTX;
                                    }
                                    itemprice = Convert.ToDouble(FinalPriceMain);
                                    //iwtxprice = iwtxprice + "$" + itemprice;
                                    string status = Convert.ToString(drRoomRate[r]["RoomStatus"]);
                                    if (status.ToLower() == "ok")
                                    {
                                        //DataRow[] drRoomRate = IWTX_Response.Tables["CancellationPolicyDetails"].Select();
                                        if (true)//(Currency.ToLower() != "usd")
                                        {
                                            itemprice = Convert.ToDouble(Math.Round(itemprice, 2));
                                            priceCheckCMDto.ItemPrice = itemprice;
                                            if (Convert.ToDouble(Math.Round(itemprice.TruncateDecimalPlaces(0))) > Math.Round(CompanyBuyPrice.TruncateDecimalPlaces(0)))
                                            {
                                                #region RPT GeneralError
                                                try
                                                {
                                                    ResponseData = "#Price Changed " + "#Main Price = " + CompanyBuyPrice + " #New Price = " + itemprice + " #Response= " + ResponseData;
                                                    responsetype = "GeneralError";
                                                    string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, Request, ResponseData, Error);
                                                }
                                                catch (Exception e)
                                                {
                                                    _ = _exception.Error(e, Searchkey);
                                                }
                                                #endregion
                                                priceCheckCMDto.Flag = true;
                                                return priceCheckCMDto;
                                            }
                                        }
                                        else
                                        {
                                            FinalPriceMain = FinalPriceTemp * CurrencyFactorIWTX;
                                        }
                                    }
                                    else
                                    {
                                        #region RPT GeneralError
                                        try
                                        {
                                            responsetype = "GeneralError";
                                            string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, Request, ResponseData, Error);
                                        }
                                        catch (Exception e)
                                        {
                                            _ = _exception.Error(e, Searchkey);
                                        }
                                        #endregion
                                        itemprice = 0;
                                        priceCheckCMDto.Flag = true;
                                        priceCheckCMDto.ItemPrice = itemprice;
                                        return priceCheckCMDto;
                                    }

                                }
                            }
                        }
                        else
                        {
                            #region RPT GeneralError
                            try
                            {
                                responsetype = "GeneralError";
                                string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, Request, ResponseData, Error);
                            }
                            catch (Exception e)
                            {
                                _ = _exception.Error(e, Searchkey);
                            }
                            #endregion
                            priceCheckCMDto.Flag = true;
                            return priceCheckCMDto;
                        }
                    }
                }
                else
                {
                    #region RPT GeneralError
                    try
                    {
                        responsetype = "GeneralError";
                        string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, Request, ResponseData, Error);
                    }
                    catch (Exception e)
                    {
                        _ = _exception.Error(e, Searchkey);
                    }
                    #endregion
                    priceCheckCMDto.Flag = true;
                    return priceCheckCMDto;
                }
            }
            catch (Exception ex)
            {
                #region RPT GeneralError
                try
                {
                    string Error1 = ex.Message + ex.StackTrace.ToString();
                    responsetype = "GeneralError";
                    string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, Request, ResponseData, "ex= " + Error1 + " Error=" + Error);
                }
                catch (Exception x)
                {
                    _ = _exception.Error(x, Searchkey);
                }
                #endregion
                _ = _exception.Error(ex);
                IsError = true;
                priceCheckCMDto.Flag = true;
                priceCheckCMDto.IsError = true;
                return priceCheckCMDto;
            }

            #region RPT Success
            try
            {
                responsetype = "Success";
                string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, Request, ResponseData, Error);
            }
            catch (Exception e)
            {
                _ = _exception.Error(e, Searchkey);
            }
            #endregion
            priceCheckCMDto.Flag = false;
            return priceCheckCMDto;
        }

        public string PriceCheckRequest_IWTX(string checkIn, string checkOut, string HotelId, string Username, string Password, int NoOfAdults, int NoofChild, int Child1Age, int Child2Age, int RoomConfigurationId, string XmlCity, string Uniqueid, string XmlNationality, string RoomTypeCode, string ContractTokenId, string MealPlanCode, DataSet dsCartDetails)
        {
            try
            {

                string StartDate = String.Format("{0:yyyyMMdd}", DateTime.ParseExact(checkIn, "yyyy-MM-dd", null));
                string EndDate = String.Format("{0:yyyyMMdd}", DateTime.ParseExact(checkOut, "yyyy-MM-dd", null));

                StringBuilder objRequest = new StringBuilder();
                if (HotelId != string.Empty)
                {
                    objRequest.Append("<HotelSearchRequest>");
                    objRequest.Append("<OutputFormat>XML</OutputFormat>");

                    objRequest.Append("<Profile>");
                    objRequest.Append("<Password>" + Password + "</Password>");
                    objRequest.Append("<Code>" + Username + "</Code>");
                    //objRequest.Append("<Password>D3V_1234</Password>");
                    //objRequest.Append("<Code>DEV_IWTX</Code>");
                    objRequest.Append("<TokenNumber>" + Uniqueid + "</TokenNumber>");
                    objRequest.Append("</Profile>");

                    objRequest.Append("<SearchCriteria>");
                    objRequest.Append("<CancellationPolicy>Y</CancellationPolicy>");


                    objRequest.Append("<RoomConfiguration>");
                    for (int j = 0; j < dsCartDetails.Tables[1].DefaultView.Count; j++)
                    {


                        string[] HBArray = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomTypeId"]).Split(new String[] { "$$$" }, StringSplitOptions.None);
                        RoomTypeCode = HBArray[0];
                        MealPlanCode = HBArray[1];
                        ContractTokenId = HBArray[5];
                        RoomConfigurationId = Convert.ToInt32(HBArray[10]);
                        XmlNationality = Convert.ToString(HBArray[12]); //Added on 03-FEB-26

                        Child1Age = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[j]["ChildAge1"]);
                        Child2Age = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[j]["ChildAge2"]);
                        //RoomConfigurationId = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[j]["RoomNumber"]);
                        NoOfAdults = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[j]["NoOfAdults"]);
                        NoofChild = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[j]["NoofChild"]);

                        objRequest.Append("<Room>");
                        objRequest.Append("<RoomTypeCode>" + RoomTypeCode + "</RoomTypeCode>");
                        objRequest.Append("<ContractTokenId>" + ContractTokenId + "</ContractTokenId>");
                        objRequest.Append("<RoomConfigurationId>" + RoomConfigurationId + "</RoomConfigurationId>");
                        objRequest.Append("<MealPlanCode>" + MealPlanCode + "</MealPlanCode>");
                        for (int i = 0; i < NoOfAdults; i++)
                        {
                            objRequest.Append("<Adult>");
                            objRequest.Append("<Age>" + 30 + "</Age>");
                            objRequest.Append("</Adult>");
                        }
                        if (NoofChild == 1)
                        {
                            objRequest.Append("<Child>");
                            objRequest.Append("<Age>" + Child1Age + "</Age>");
                            objRequest.Append("</Child>");
                        }
                        else if (NoofChild == 2)
                        {
                            objRequest.Append("<Child>");
                            objRequest.Append("<Age>" + Child1Age + "</Age>");
                            objRequest.Append("</Child>");
                            objRequest.Append("<Child>");
                            objRequest.Append("<Age>" + Child2Age + "</Age>");
                            objRequest.Append("</Child>");
                        }

                        //NoofChild = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[j]["NoofChild"]);
                        //string childAgeData = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["ChildAge"]);

                        //objRequest.Append("<Room>");
                        //objRequest.Append("<RoomTypeCode>" + RoomTypeCode + "</RoomTypeCode>");
                        //objRequest.Append("<ContractTokenId>" + ContractTokenId + "</ContractTokenId>");
                        //objRequest.Append("<RoomConfigurationId>" + RoomConfigurationId + "</RoomConfigurationId>");
                        //objRequest.Append("<MealPlanCode>" + MealPlanCode + "</MealPlanCode>");

                        //// Adults
                        //for (int i = 0; i < NoOfAdults; i++)
                        //{
                        //    objRequest.Append("<Adult>");
                        //    objRequest.Append("<Age>30</Age>");
                        //    objRequest.Append("</Adult>");
                        //}

                        //// Children (Dynamic)
                        //if (!string.IsNullOrEmpty(childAgeData))
                        //{
                        //    var childAges = childAgeData
                        //        .Split(',')
                        //        .Where(x => int.TryParse(x.Trim(), out _))
                        //        .Select(x => x.Trim());

                        //    foreach (var age in childAges)
                        //    {
                        //        objRequest.Append("<Child>");
                        //        objRequest.Append("<Age>" + age + "</Age>");
                        //        objRequest.Append("</Child>");
                        //    }
                        //}

                       
                        objRequest.Append("</Room>");
                    }
                    objRequest.Append("</RoomConfiguration>");


                    objRequest.Append("<StartDate>" + StartDate + "</StartDate>");
                    objRequest.Append("<EndDate>" + EndDate + "</EndDate>");
                    objRequest.Append("<City>" + XmlCity + "</City>");
                    objRequest.Append("<HotelCode>" + HotelId + "</HotelCode>");
                    objRequest.Append("<IncludeOnRequest>N</IncludeOnRequest>");
                    objRequest.Append("<IncludeRateDetails>Y</IncludeRateDetails>");
                    objRequest.Append("<Nationality>" + XmlNationality + "</Nationality>");
                    objRequest.Append("</SearchCriteria>");
                    objRequest.Append("</HotelSearchRequest>");
                }
                string HotelRequest = objRequest.ToString();

                return HotelRequest;
            }
            catch (Exception ex)
            {
                _ = _exception.Error(ex);
                return "";
            }
        }


        public string BookingRequest_IWTX(DataSet dsCartDetails, DataTable dtPassengers, DataRow[] IWTXDetails, string UniqueNo, int i, int jj, string ReferenceNo)
        {
            try
            {
                #region Booking RQ

                string CheckIn = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["CheckInDate"]);
                CheckIn = CheckIn.Substring(0, 4) + "-" + CheckIn.Substring(4, 2) + "-" + CheckIn.Substring(6, 2);
                string CheckOut = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["CheckOutDate"]);
                CheckOut = CheckOut.Substring(0, 4) + "-" + CheckOut.Substring(4, 2) + "-" + CheckOut.Substring(6, 2);

                string PropertyId = Convert.ToString(dsCartDetails.Tables[1].DefaultView[jj]["ContractID"]);
                string Meal = Convert.ToString(dsCartDetails.Tables[1].DefaultView[jj]["MealPlanCode"]);
                string Roomidsplit = Convert.ToString(dsCartDetails.Tables[1].DefaultView[jj]["RoomTypeId"]);
                string[] Roomids = Roomidsplit.Split(new String[] { "$$$" }, StringSplitOptions.None);
                string RoomTypeCode = Roomids[0].ToString();
                string MealPlanCodes = Roomids[1].ToString();
                string RoomId = Roomids[2].ToString();
                string RoomType = Roomids[3].ToString();
                string HotelCode = Roomids[4].ToString();
                string ContractTokenId = Roomids[5].ToString();
                string XmlCity = Roomids[6].ToString();
                string IWTX_Currency = Roomids[7].ToString();
                string Price1 = Roomids[8].ToString();
                string Uniqueid = Roomids[9].ToString();
                string RoomConfigurationId = Roomids[10].ToString();
                //string RoomConfiNo = Convert.ToString(dsCartDetails.Tables[1].DefaultView[n]["RoomNumber"]);
                string XmlNationality = Convert.ToString(dsCartDetails.Tables[1].Rows[i]["XmlNationality"]);
                string usercartid = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["UserCartID"]);
                int UserCartDetailId = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[jj]["UserCartDetailId"]);

                //string AgentReference = Convert.ToString(ReferenceNo + ":" + (i + 1)); //"Book_" + Convert.ToString(Common.getSession("AgentCode") + "_" + ReferenceNo) + "_" + UniqueNo;

                Random rnd = new Random();
                int randomNumber = rnd.Next(1, 100);
                string AgentReference = Convert.ToString(UserCartDetailId + ":" + randomNumber);

                double Price = Convert.ToDouble(Price1);



                string UserName = Convert.ToString(IWTXDetails[0]["UserName"]);
                string Password = Convert.ToString(IWTXDetails[0]["Password"]);


                string Prefix = "", Name = "", SurName = "";
                bool PrimaryIndicator = false;

                string AgentVoucherNo = Convert.ToString(UserCartDetailId);

                #endregion

                #region"RQ"

                string Request = "";
                StringBuilder BookRQ = new StringBuilder();
                BookRQ.Append("<HotelBookingRequest>");

                BookRQ.Append("<Profile>");
                BookRQ.Append("<Password>" + Password + "</Password>");
                BookRQ.Append("<Code>" + UserName + "</Code>");
                //BookRQ.Append("<Password>D3V_1234</Password>");
                //BookRQ.Append("<Code>DEV_IWTX</Code>");
                BookRQ.Append("<TokenNumber>" + Uniqueid + "</TokenNumber>");
                BookRQ.Append("</Profile>");

                BookRQ.Append("<PassengerDetails>");

                int noofpacks = 0;

                for (int k = 0; k < dsCartDetails.Tables[1].DefaultView.Count; k++)
                //for (int k = 0; k < 1; k++)
                {
                    //Now filter passanger table

                    if (dtPassengers.Rows.Count > 0)
                    {
                        dtPassengers.DefaultView.RowFilter = "PickUp='" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[k]["RoomTypeID"]) + "' and RoomNo='" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[k]["RoomNumber"]) + "'";
                        dtPassengers.DefaultView.Sort = "IsLead desc";
                        if (dtPassengers.DefaultView.Count > 0)
                        {
                            for (int n = 0; n < dtPassengers.DefaultView.Count; n++)
                            {
                                noofpacks++;
                                BookRQ.Append("<Passenger>");
                                BookRQ.Append("<PaxNumber>" + noofpacks.ToString() + "</PaxNumber>");
                                //BookRQ.Append("<RoomNo>" + RoomConfigurationId + "</RoomNo>");
                                BookRQ.Append("<RoomNo>" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[k]["RoomNumber"]) + "</RoomNo>");
                                //BookRQ.Append("<RoomNo>1</RoomNo>");
                                BookRQ.Append("<Title>" + Convert.ToString(dtPassengers.DefaultView[n]["Prefix"]).Replace(".", "") + "</Title>");
                                BookRQ.Append("<PassengerType>" + (Convert.ToString(dtPassengers.DefaultView[n]["PaxType"]).ToLower() == "adult" ? "ADT" : "CHD") + "</PassengerType>");
                                if (Convert.ToString(dtPassengers.DefaultView[n]["PaxType"]).ToLower() == "adult")
                                    BookRQ.Append("<Age>30</Age>");
                                //BookRQ.Append("<DateOfBirth>19890121</DateOfBirth>");//Optional
                                if (Convert.ToString(dtPassengers.DefaultView[n]["PaxType"]).ToLower() == "child")
                                    BookRQ.Append("<Age>" + Convert.ToString(dtPassengers.DefaultView[n]["ChildAge"]) + "</Age>");//Optional for Adult                            
                                BookRQ.Append("<FirstName>" + Convert.ToString(dtPassengers.DefaultView[n]["FirstName"]).Trim() + "</FirstName>");
                                BookRQ.Append("<LastName>" + Convert.ToString(dtPassengers.DefaultView[n]["LastName"]).Trim() + "</LastName>");
                                BookRQ.Append("<Nationality>" + XmlNationality + "</Nationality>");//Optional
                                BookRQ.Append("<Gender>" + (Convert.ToString(dtPassengers.DefaultView[n]["Prefix"]).ToLower() == "mrs." || Convert.ToString(dtPassengers.DefaultView[n]["Prefix"]).ToLower() == "mr." ? "M" : "F") + "</Gender>");
                                BookRQ.Append("</Passenger>");
                            }
                        }
                    }
                }
                BookRQ.Append("</PassengerDetails>");

                //Hotel Details

                BookRQ.Append("<HotelDetails>");
                BookRQ.Append("<StartDate>" + Convert.ToString(dsCartDetails.Tables[0].Rows[i]["CheckInDate"]) + "</StartDate>");
                BookRQ.Append("<EndDate>" + Convert.ToString(dsCartDetails.Tables[0].Rows[i]["CheckOutDate"]) + "</EndDate>");
                BookRQ.Append("<HotelCode>" + Convert.ToString(dsCartDetails.Tables[0].Rows[i]["XMLHotelId"]) + "</HotelCode>");//XMLHotelId
                BookRQ.Append("<CityCode>" + XmlCity + "</CityCode>");
                BookRQ.Append("<AgencyRef>" + AgentReference + "</AgencyRef>");
                //Room Details
                BookRQ.Append("<RoomDetails>");

                for (int j = 0; j < dsCartDetails.Tables[1].DefaultView.Count; j++)
                //for (int j = 0; j < 1; j++)
                {
                    Roomidsplit = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomTypeId"]);
                    Roomids = Roomidsplit.Split(new String[] { "$$$" }, StringSplitOptions.None);
                    RoomTypeCode = Roomids[0].ToString();
                    MealPlanCodes = Roomids[1].ToString();
                    //RoomId = Roomids[2].ToString();
                    //RoomType = Roomids[3].ToString();
                    //HotelCode = Roomids[4].ToString();
                    ContractTokenId = Roomids[5].ToString();
                    //XmlCity = Roomids[6].ToString();
                    //IWTX_Currency = Roomids[7].ToString();
                    Price1 = Roomids[8].ToString();
                    //Uniqueid = Roomids[9].ToString();
                    RoomConfigurationId = Roomids[10].ToString();
                    Price = Convert.ToDouble(Price1);

                    BookRQ.Append("<Room>");
                    BookRQ.Append("<RoomConfigurationId>" + RoomConfigurationId + "</RoomConfigurationId>");//Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomNumber"])
                    BookRQ.Append("<RoomTypeCode>" + RoomTypeCode + "</RoomTypeCode>");
                    BookRQ.Append("<ContractTokenId>" + ContractTokenId + "</ContractTokenId>");
                    BookRQ.Append("<MealPlanCode>" + MealPlanCodes + "</MealPlanCode>");
                    BookRQ.Append("<Rate>" + Price + "</Rate>");
                    BookRQ.Append("</Room>");


                }
                BookRQ.Append("</RoomDetails>");

                BookRQ.Append("</HotelDetails>");
                BookRQ.Append("</HotelBookingRequest>");
                string BookingRQ = BookRQ.ToString();
                return BookingRQ;
                #endregion
            }
            catch (Exception ex)
            {
                _ = _exception.Error(ex);
                return "";
            }
        }
        public string BookingRequest_IWTX_newversion(BookRequest_newversion bookRequest, DataTable dtPassengers, DataRow[] IWTXDetails, string UniqueNo, int i, int jj, string ReferenceNo)
        {
            try
            {
                #region Booking RQ

               
                string CheckIn = Convert.ToString(bookRequest.userCart.UserCartDetails[0].CheckInDate);
                //CheckIn = CheckIn.Substring(0, 4) + "-" + CheckIn.Substring(4, 2) + "-" + CheckIn.Substring(6, 2);
                string CheckOut = Convert.ToString(bookRequest.userCart.UserCartDetails[0].CheckOutDate);
                //CheckOut = CheckOut.Substring(0, 4) + "-" + CheckOut.Substring(4, 2) + "-" + CheckOut.Substring(6, 2);

                // Parse the strings to DateTime
                DateTime checkInDate = DateTime.Parse(CheckIn);
                DateTime checkOutDate = DateTime.Parse(CheckOut);

                // Convert to "yyyy-MM-dd" format
                string checkin = checkInDate.ToString("yyyyMMdd");
                string checkout = checkOutDate.ToString("yyyyMMdd");

                //string PropertyId = Convert.ToString(dsCartDetails.Tables[1].DefaultView[jj]["ContractID"]);
                string Meal = Convert.ToString(bookRequest.userCart.UserCartDetails[0].MealPlan);
                string Roomidsplit = Convert.ToString(bookRequest.userCart.UserCartDetails[0].RoomTypeId);
                string[] Roomids = Roomidsplit.Split(new String[] { "$$$" }, StringSplitOptions.None);
                string RoomTypeCode = Roomids[0].ToString();
                string MealPlanCodes = Roomids[1].ToString();
                string RoomId = Roomids[2].ToString();
                string RoomType = Roomids[3].ToString();
                string HotelCode = Roomids[4].ToString();
                string ContractTokenId = Roomids[5].ToString();
                string XmlCity = Roomids[6].ToString();
                string IWTX_Currency = Roomids[7].ToString();
                string Price1 = Roomids[8].ToString();
                string Uniqueid = Roomids[9].ToString();
                string RoomConfigurationId = Roomids[10].ToString();
                //string RoomConfiNo = Convert.ToString(dsCartDetails.Tables[1].DefaultView[n]["RoomNumber"]);
               // string XmlNationality = "";//Convert.ToString(bookRequest.userCart.UserCartDetails[0].XmlNationality);
                string XmlNationality = Roomids[Roomids.Length - 1];  // IN
                string usercartid = Convert.ToString(bookRequest.userCart.UserCartDetails[0].UserCartId);
                int UserCartDetailId = Convert.ToInt32(bookRequest.userCart.UserCartDetails[0].UserCartDetailId);

                //string AgentReference = Convert.ToString(ReferenceNo + ":" + (i + 1)); //"Book_" + Convert.ToString(Common.getSession("AgentCode") + "_" + ReferenceNo) + "_" + UniqueNo;

                Random rnd = new Random();
                int randomNumber = rnd.Next(1, 100);
                string AgentReference = Convert.ToString(ReferenceNo + ":" +UserCartDetailId + ":" + randomNumber);

                double Price = Convert.ToDouble(Price1);



                string UserName = Convert.ToString(IWTXDetails[0]["UserName"]);
                string Password = Convert.ToString(IWTXDetails[0]["Password"]);


                string Prefix = "", Name = "", SurName = "";
                bool PrimaryIndicator = false;

                string AgentVoucherNo = Convert.ToString(UserCartDetailId);

                #endregion

                #region"RQ"

                string Request = "";
                StringBuilder BookRQ = new StringBuilder();
                BookRQ.Append("<HotelBookingRequest>");

                BookRQ.Append("<Profile>");
                BookRQ.Append("<Password>" + Password + "</Password>");
                BookRQ.Append("<Code>" + UserName + "</Code>");
                //BookRQ.Append("<Password>D3V_1234</Password>");
                //BookRQ.Append("<Code>DEV_IWTX</Code>");
                BookRQ.Append("<TokenNumber>" + Uniqueid + "</TokenNumber>");
                BookRQ.Append("</Profile>");

                BookRQ.Append("<PassengerDetails>");

                int noofpacks = 0;

                for (int k = 0; k < bookRequest.userCart.UserCartDetails.Count; k++)
                //for (int k = 0; k < 1; k++)
                {
                    //Now filter passanger table

                    if (dtPassengers.Rows.Count > 0)
                    {
                        dtPassengers.DefaultView.RowFilter = "PickUp='" + Convert.ToString(bookRequest.userCart.UserCartDetails[k].RoomTypeId) + "' and RoomNo='" + Convert.ToString(bookRequest.userCart.UserCartDetails[k].RoomNo) + "'";
                        dtPassengers.DefaultView.Sort = "IsLead desc";
                        if (dtPassengers.DefaultView.Count > 0)
                        {
                            for (int n = 0; n < dtPassengers.DefaultView.Count; n++)
                            {
                                noofpacks++;
                                BookRQ.Append("<Passenger>");
                                BookRQ.Append("<PaxNumber>" + noofpacks.ToString() + "</PaxNumber>");
                                //BookRQ.Append("<RoomNo>" + RoomConfigurationId + "</RoomNo>");
                                BookRQ.Append("<RoomNo>" + Convert.ToString(bookRequest.userCart.UserCartDetails[k].RoomNo) + "</RoomNo>");
                                //BookRQ.Append("<RoomNo>1</RoomNo>");
                                BookRQ.Append("<Title>" + Convert.ToString(dtPassengers.DefaultView[n]["Prefix"]).Replace(".", "") + "</Title>");
                                BookRQ.Append("<PassengerType>" + (Convert.ToString(dtPassengers.DefaultView[n]["PaxType"]).ToLower() == "adult" ? "ADT" : "CHD") + "</PassengerType>");
                                if (Convert.ToString(dtPassengers.DefaultView[n]["PaxType"]).ToLower() == "adult")
                                    BookRQ.Append("<Age>30</Age>");
                                //BookRQ.Append("<DateOfBirth>19890121</DateOfBirth>");//Optional
                                if (Convert.ToString(dtPassengers.DefaultView[n]["PaxType"]).ToLower() == "child")
                                    BookRQ.Append("<Age>" + Convert.ToString(dtPassengers.DefaultView[n]["ChildAge"]) + "</Age>");//Optional for Adult                            
                                BookRQ.Append("<FirstName>" + Convert.ToString(dtPassengers.DefaultView[n]["FirstName"]).Trim() + "</FirstName>");
                                BookRQ.Append("<LastName>" + Convert.ToString(dtPassengers.DefaultView[n]["LastName"]).Trim() + "</LastName>");
                                BookRQ.Append("<Nationality>" + XmlNationality + "</Nationality>");//240326
                                BookRQ.Append("<Gender>" + (Convert.ToString(dtPassengers.DefaultView[n]["Prefix"]).ToLower() == "mrs." || Convert.ToString(dtPassengers.DefaultView[n]["Prefix"]).ToLower() == "mr." ? "M" : "F") + "</Gender>");
                                BookRQ.Append("</Passenger>");
                            }
                        }
                    }
                }
                BookRQ.Append("</PassengerDetails>");

                //Hotel Details

                BookRQ.Append("<HotelDetails>");
                BookRQ.Append("<StartDate>" + Convert.ToString(checkin) + "</StartDate>");
                BookRQ.Append("<EndDate>" + Convert.ToString(checkout) + "</EndDate>");
                BookRQ.Append("<HotelCode>" + Convert.ToString(HotelCode) + "</HotelCode>");//240326
                BookRQ.Append("<CityCode>" + XmlCity + "</CityCode>");
                BookRQ.Append("<AgencyRef>" + AgentReference + "</AgencyRef>");
                //Room Details
                BookRQ.Append("<RoomDetails>");

                for (int j = 0; j < bookRequest.userCart.UserCartDetails.Count; j++)
                //for (int j = 0; j < 1; j++)
                {
                    Roomidsplit = Convert.ToString(bookRequest.userCart.UserCartDetails[j].RoomTypeId);
                    Roomids = Roomidsplit.Split(new String[] { "$$$" }, StringSplitOptions.None);
                    RoomTypeCode = Roomids[0].ToString();
                    MealPlanCodes = Roomids[1].ToString();
                    //RoomId = Roomids[2].ToString();
                    //RoomType = Roomids[3].ToString();
                    //HotelCode = Roomids[4].ToString();
                    ContractTokenId = Roomids[5].ToString();
                    //XmlCity = Roomids[6].ToString();
                    //IWTX_Currency = Roomids[7].ToString();
                    Price1 = Roomids[8].ToString();
                    //Uniqueid = Roomids[9].ToString();
                    RoomConfigurationId = Roomids[10].ToString();
                    Price = Convert.ToDouble(Price1);

                    BookRQ.Append("<Room>");
                    BookRQ.Append("<RoomConfigurationId>" + RoomConfigurationId + "</RoomConfigurationId>");//Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomNumber"])
                    BookRQ.Append("<RoomTypeCode>" + RoomTypeCode + "</RoomTypeCode>");
                    BookRQ.Append("<ContractTokenId>" + ContractTokenId + "</ContractTokenId>");
                    BookRQ.Append("<MealPlanCode>" + MealPlanCodes + "</MealPlanCode>");
                    BookRQ.Append("<Rate>" + Price + "</Rate>");
                    BookRQ.Append("</Room>");


                }
                BookRQ.Append("</RoomDetails>");

                BookRQ.Append("</HotelDetails>");
                BookRQ.Append("</HotelBookingRequest>");
                string BookingRQ = BookRQ.ToString();
                return BookingRQ;
                #endregion
            }
            catch (Exception ex)
            {
                _ = _exception.Error(ex);
                return "";
            }
        }
        public string CancelationRequest_IWTX(DataRow[] IWTXDetails, string AgentVoucherNo, string Uniqueid)
        {
            try
            {
                string UserName = Convert.ToString(IWTXDetails[0]["UserName"]);
                string Password = Convert.ToString(IWTXDetails[0]["Password"]);

                StringBuilder sbReq = new StringBuilder();



                string[] spitid = AgentVoucherNo.Split(new String[] { "|" }, StringSplitOptions.None);
                string sourceid = spitid[0].ToString();
                string bookingnumber = spitid[1].ToString();
                string subresno = spitid[2].ToString();
                //string Uniqueid = spitid[3].ToString();

                sbReq.Append("<CancelHotelBookingRequest>");

                sbReq.Append("<Profile>");
                sbReq.Append("<Password>" + Password + "</Password>");
                sbReq.Append("<Code>" + UserName + "</Code>");
                //sbReq.Append("<Password>D3V_1234</Password>");
                //sbReq.Append("<Code>DEV_IWTX</Code>");
                sbReq.Append("<TokenNumber>" + Uniqueid + "</TokenNumber>");
                sbReq.Append("</Profile>");

                sbReq.Append("<BookingDetails>");
                sbReq.Append("<Source>" + sourceid + "</Source>");
                sbReq.Append("<BookingNumber>" + bookingnumber + "</BookingNumber>");
                sbReq.Append("<SubResNo>" + subresno + "</SubResNo>");
                sbReq.Append("</BookingDetails>");
                sbReq.Append("</CancelHotelBookingRequest>");

                return sbReq.ToString();
            }
            catch (Exception ex)
            {
                _ = _exception.ErrorNew(ex, _LogAPI.CancelBooking);
            }
            return string.Empty;
        }

        public string RetrieveHotelBookingCancelationRequest_IWTX(DataRow[] IWTXDetails, string AgentVoucherNo, string Uniqueid)
        {
            try
            {
                string UserName = Convert.ToString(IWTXDetails[0]["UserName"]);
                string Password = Convert.ToString(IWTXDetails[0]["Password"]);

                StringBuilder sbReq = new StringBuilder();

                string[] spitid = AgentVoucherNo.Split(new String[] { "|" }, StringSplitOptions.None);

                string sourceid = spitid[0].ToString();
                string bookingnumber = spitid[1].ToString();
                string subresno = spitid[2].ToString();


                sbReq.Append("<RetrieveBookingRequest>");

                sbReq.Append("<Profile>");
                sbReq.Append("<Password>" + Password + "</Password>");
                sbReq.Append("<Code>" + UserName + "</Code>");
                //sbReq.Append("<Password>D3V_1234</Password>");
                //sbReq.Append("<Code>DEV_IWTX</Code>");
                sbReq.Append("<TokenNumber>" + Uniqueid + "</TokenNumber>");
                sbReq.Append("</Profile>");

                sbReq.Append("<BookingDetails>");
                sbReq.Append("<BookingNo>" + bookingnumber + "</BookingNo>");
                sbReq.Append("<Source>" + sourceid + "</Source>");
                sbReq.Append("<SubResNo>" + subresno + "</SubResNo>");
                sbReq.Append("</BookingDetails>");
                sbReq.Append("</RetrieveBookingRequest>");
                return sbReq.ToString();
            }
            catch (Exception ex)
            {
                _ = _exception.ErrorNew(ex, _LogAPI.CancelBooking);

            }
            return string.Empty;
        }
    }
}
#endregion