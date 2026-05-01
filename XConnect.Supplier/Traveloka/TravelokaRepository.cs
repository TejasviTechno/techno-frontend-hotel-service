using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using XConnect.CommonUtilities.Interface;
using XConnect.CommonUtilities;
using XConnect.ErrorLogHandler.Interface;
using XConnect.Markup.Interface;
using XConnect.Models.Hotel;
using XConnect.Models;
using XConnect.Supplier.Traveloka.Interface;
using Newtonsoft.Json;
using Microsoft.Extensions.Options;
using Options = XConnect.Models.Options;
using Microsoft.IdentityModel.Protocols;
using static Azure.Core.HttpHeader;
using static XConnect.Supplier.Booking.Com.preebookrs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Caching.Memory;

namespace XConnect.Supplier.Traveloka
{
    public class TravelokaRepository : ITraveloka
    {
        private readonly ICommonMethod _commonMethod;
        private readonly IException _exception;
        private readonly IMarkup _markup;
        private readonly ISupplierRes _supplierRes;
        private readonly KeySettings _keySettings;
        private readonly IMemoryCache _cache;


        public TravelokaRepository(ICommonMethod commonMethod, IMarkup markup, ISupplierRes supplierRes, IException exception, IOptions<KeySettings> keySettings, IMemoryCache cache)
        {
            _commonMethod = commonMethod;
            _markup = markup;
            _supplierRes = supplierRes;
            _exception = exception;
            _keySettings = keySettings.Value;
            _cache = cache;
        }
        public void TravelokaRequestOnly_async(DataRow[] commonCode, List<StarRating> listRating, HotelSearchParameter SearchPara, List<SupplierDetails> TravelokaDetails, List<SupplierSearchConfig> TravelokaSearchConfig, DataTable dtPreferredDataHotel, ref ConcurrentBag<HotelX> AllTask, string XMLCity, string XMLCountry, string XMLNationality, int Hit, string custIP, DataTable dtROE, TokenData tokenData)
        {
            try
            {
                string Ids = string.Empty;
                string xmlbase = "";
                _commonMethod.GetPrefferedHotelCodes(ref Ids, ref xmlbase, ref TravelokaSearchConfig, "traveloka", dtPreferredDataHotel, SearchPara.IsSearchFor);
                if (TravelokaSearchConfig != null)
                {
                    double CurrencyFactorTraveloka = 1;// Convert.ToDouble(dtInrCurrency.Rows[0]["AmtToExch"]);
                                                       // string Currency = Convert.ToString(ConfigurationManager.AppSettings["TravelokaCurrency"]);
                    string Currency = Convert.ToString(TravelokaDetails[0].CurrencyShortName);
                    string Sitecurrency = tokenData.SiteCurrency;
                    //if (Currency.ToLower() != "usd")
                    //{
                    //    // DataTable dtInrCurrency = Common.GetCountryCurrency(Currency);
                    //    CurrencyFactorTraveloka = _commonMethod.GetROEByCurrency(Currency, dtROE);
                    //}
                    if (Currency.ToLower() != Sitecurrency.ToLower()) // change systembase currency 
                    {
                        CurrencyFactorTraveloka = _commonMethod.GetROEByCurrency(Currency, dtROE);
                    }

                    for (int k = 0; k < TravelokaSearchConfig.Count; k++)
                    {
                        bool DebugMode = (Convert.ToInt32(TravelokaSearchConfig[k].DebugMode) == 0 ? false : true);
                        string UserName = Convert.ToString(TravelokaDetails[0].UserName);
                        string Password = Convert.ToString(TravelokaDetails[0].Password);
                        {

                            int IsCitywisesearch = Convert.ToInt32(TravelokaSearchConfig[k].City);
                            //if (SearchPara.Filters.IsShowRooms == 0)//(DtHotelData.DefaultView.Count > 0)
                            {
                                if (Convert.ToInt32(TravelokaSearchConfig[k].CityID) == 0 || Convert.ToInt32(TravelokaSearchConfig[k].CityID) == SearchPara.CityID)
                                {
                                    if (commonCode.Length > 0)
                                    {
                                        //int Noofrecords = Convert.ToInt32(TravelokaSearchConfig[k].Noofrecords);
                                        int Noofrecords = SearchPara.Filters.IsShowRooms == 1 ? Convert.ToInt32(TravelokaSearchConfig[k].Noofrecords) : 100;
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
                                                Xhotel.Supplier = "traveloka";
                                                Xhotel.TechName = "traveloka";
                                                Xhotel.DebugeMode = DebugMode;
                                                Xhotel.SearchTimeOut = Convert.ToInt32(TravelokaSearchConfig[k].HotelSearchTimeOut);
                                                //if (Hit > 0)
                                                Xhotel.ProcessTimeout = Convert.ToInt32(TravelokaSearchConfig[k].HotelSearchTimeOut2);

                                                Xhotel.URL = Convert.ToString(TravelokaDetails[0].WebSiteURL) + TravelokaInfo.Tarveloka_HotelList;
                                                //Xhotel.URL = Convert.ToString(TravelokaDetails[0].WebSiteURL) + Convert.ToString(_configuration["Tarveloka_HotelList"]);
                                                Xhotel.UserId = Convert.ToString(TravelokaDetails[0].UserID);
                                                Xhotel.UserName = UserName;
                                                Xhotel.PassWord = Password;
                                                Xhotel.CurrencyCode = Currency;
                                                Xhotel.CurrencyFactor = CurrencyFactorTraveloka;
                                                Xhotel.XmlCity = XMLCity;
                                                // Xhotel.HotelID = HotelId;
                                                Xhotel.XmlCountry = "";
                                                Xhotel.XmlNationality = XMLNationality;
                                                string PaxPassport = XMLNationality;

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
                                                    //objrq.Request = Hotel.Traveloka_RequestOnly_Async(Xhotel.URL, Xhotel.UserName, Xhotel.PassWord, SearchPara, XMLCity, IdsForSearch, custIP, Currency);
                                                    objrq.Request = Traveloka_AvailabilityRequest(Xhotel.URL, Xhotel.UserName, Xhotel.PassWord, SearchPara, XMLCity, IdsForSearch, custIP, Currency);
                                                    objrq.Response = ""; RQRS.Add(objrq);
                                                }
                                                Xhotel.RQRS = RQRS;
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
                                                Xhotel.Supplier = "traveloka";
                                                Xhotel.TechName = "traveloka";
                                                Xhotel.DebugeMode = DebugMode;
                                                Xhotel.SearchTimeOut = Convert.ToInt32(TravelokaSearchConfig[k].HotelSearchTimeOut);
                                                //if (Hit > 0)
                                                Xhotel.ProcessTimeout = Convert.ToInt32(TravelokaSearchConfig[k].HotelSearchTimeOut2);
                                                //Xhotel.URL = Convert.ToString(TravelokaDetails[0].WebSiteURL) + Convert.ToString(_configuration["Tarveloka_HotelBulk"]);
                                                Xhotel.URL = Convert.ToString(TravelokaDetails[0].WebSiteURL) + TravelokaInfo.Tarveloka_HotelBulk;
                                                Xhotel.UserId = Convert.ToString(TravelokaDetails[0].UserID);
                                                Xhotel.UserName = UserName;
                                                Xhotel.PassWord = Password;
                                                Xhotel.CurrencyCode = Currency;
                                                Xhotel.CurrencyFactor = CurrencyFactorTraveloka;
                                                Xhotel.XmlCity = XMLCity;
                                                // Xhotel.HotelID = HotelId;
                                                Xhotel.XmlCountry = "";
                                                Xhotel.XmlNationality = XMLNationality;
                                                string PaxPassport = XMLNationality;

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
                                                    objrq.Request = Traveloka_BulkRoomRequest(SearchPara, IdsForSearch, Xhotel.URL, UserName, Password, XMLNationality, custIP, Currency);
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

        public void TravelokaRequestOnly_async_new(DataRow[] commonCode, List<StarRating> listRating, HotelSearchParameter SearchPara, List<SupplierDetails> lstTravelokaDetails, List<SupplierSearchConfig> lstTravelokaSearchConfig, DataTable dtPreferredDataHotel, ref ConcurrentBag<HotelX> AllTask, string custIP, DataTable dtROE, TokenData tokenData, DataSet dtXMLCity)
        {
            if (lstTravelokaDetails != null && lstTravelokaDetails.Count > 0)
            {

                List<StarRating> lstRating = new List<StarRating>();
                lstRating = _commonMethod.GetStarRatingStringList_xtg(SearchPara);

                List<HotelCodeByCity> DtHotelDAta = HtmlExtensions.ToList<HotelCodeByCity>(commonCode.CopyToDataTable());
                int ratingcount = lstRating.Count;
                int hotelcount = DtHotelDAta.Count;

                foreach (SupplierDetails TravelokaDetails in lstTravelokaDetails)
                {
                    try
                    {
                        int SupplierID = TravelokaDetails.SupplierID;
                        string Supplier = TravelokaDetails.SupplierName;

                        if (commonCode != null)
                        {
                            if (lstTravelokaSearchConfig.Count > 0)
                            {
                                List<SupplierSearchConfig> TravelokaSearchConfig = lstTravelokaSearchConfig
                               .Where(x => x != null && x.SupplierID == SupplierID)
                               .ToList();

                                if (TravelokaSearchConfig != null && TravelokaSearchConfig.Count > 0)
                                {
                                    DataForSearch dataForSearch = _commonMethod.GetXmlDataForSearchTechwise(SearchPara.Nationality, dtXMLCity, Supplier, "traveloka");

                                    if (dataForSearch.XmlNationality != "")
                                    {
                                        try
                                        {
                                            string Ids = string.Empty;
                                            string xmlbase = "";
                                            _commonMethod.GetPrefferedHotelCodes(ref Ids, ref xmlbase, ref TravelokaSearchConfig, "traveloka", dtPreferredDataHotel, SearchPara.IsSearchFor);

                                            List<HotelCodeByCity> filter = new List<HotelCodeByCity>();
                                            filter = _commonMethod.FilterWithOutCity_Code(ratingcount, hotelcount, DtHotelDAta, lstRating, Supplier);

                                            if (filter.Count > 0)
                                            {
                                                if (TravelokaSearchConfig != null)
                                                {
                                                    double CurrencyFactorTraveloka = 1;// Convert.ToDouble(dtInrCurrency.Rows[0]["AmtToExch"]);
                                                                                       // string Currency = Convert.ToString(ConfigurationManager.AppSettings["TravelokaCurrency"]);
                                                    string Currency = Convert.ToString(TravelokaDetails.CurrencyShortName);
                                                    string Sitecurrency = tokenData.SiteCurrency;

                                                    if (Currency.ToLower() != Sitecurrency.ToLower()) // change systembase currency 
                                                    {
                                                        CurrencyFactorTraveloka = _commonMethod.GetROEByCurrency(Currency, dtROE);
                                                    }

                                                    for (int k = 0; k < TravelokaSearchConfig.Count; k++)
                                                    {
                                                        bool DebugMode = (Convert.ToInt32(TravelokaSearchConfig[k].DebugMode) == 0 ? false : true);
                                                        string UserName = Convert.ToString(TravelokaDetails.UserName);
                                                        string Password = Convert.ToString(TravelokaDetails.Password);
                                                        {

                                                            int IsCitywisesearch = Convert.ToInt32(TravelokaSearchConfig[k].City);
                                                            //if (SearchPara.Filters.IsShowRooms == 0)//(DtHotelData.DefaultView.Count > 0)
                                                            {
                                                                if (Convert.ToInt32(TravelokaSearchConfig[k].CityID) == 0 || Convert.ToInt32(TravelokaSearchConfig[k].CityID) == SearchPara.CityID)
                                                                {
                                                                    if (commonCode.Length > 0)
                                                                    {
                                                                        //int Noofrecords = Convert.ToInt32(TravelokaSearchConfig[k].Noofrecords);
                                                                        int Noofrecords = SearchPara.Filters.IsShowRooms == 1 ? Convert.ToInt32(TravelokaSearchConfig[k].Noofrecords) : 100;

                                                                        double count = Convert.ToDouble(filter.Count);
                                                                        int NoOfDiv = Convert.ToInt32(Math.Ceiling(count / Convert.ToDouble(Noofrecords)));
                                                                        List<List<XMLID_Search>> lstxml = _commonMethod.GetXMLIDString(NoOfDiv, Noofrecords, count, filter);


                                                                        if (SearchPara.Filters.IsShowRooms == 0)
                                                                        {
                                                                            for (int a = 0; a < NoOfDiv; a++)
                                                                            {
                                                                                string IdsForSearch = Convert.ToString(_commonMethod.GetHotelCodeByCOmma(lstxml[a]));

                                                                                HotelX Xhotel = new HotelX();
                                                                                Xhotel.Supplier = Supplier;

                                                                                Xhotel.DebugeMode = DebugMode;
                                                                                Xhotel.SearchTimeOut = Convert.ToInt32(TravelokaSearchConfig[k].HotelSearchTimeOut);
                                                                                //if (Hit > 0)
                                                                                Xhotel.ProcessTimeout = Convert.ToInt32(TravelokaSearchConfig[k].HotelSearchTimeOut2);
                                                                                if (_keySettings.SupplierProxy == "0")
                                                                                    Xhotel.URL = Convert.ToString(TravelokaDetails.WebSiteURL) + TravelokaInfo.Tarveloka_HotelList;
                                                                                else
                                                                                    Xhotel.URL = Convert.ToString(TravelokaDetails.WebSiteURLforAPI) + TravelokaInfo.Tarveloka_HotelList;

                                                                                //Xhotel.URL = Convert.ToString(TravelokaDetails[0].WebSiteURL) + Convert.ToString(_configuration["Tarveloka_HotelList"]);
                                                                                Xhotel.UserId = Convert.ToString(TravelokaDetails.UserID);
                                                                                Xhotel.UserName = UserName;
                                                                                Xhotel.PassWord = Password;
                                                                                Xhotel.CurrencyCode = Currency;
                                                                                Xhotel.CurrencyFactor = CurrencyFactorTraveloka;
                                                                                Xhotel.XmlCity = dataForSearch.xmlCity;
                                                                                // Xhotel.HotelID = HotelId;
                                                                                Xhotel.XmlCountry = "";
                                                                                Xhotel.XmlNationality = dataForSearch.XmlNationality;
                                                                                string PaxPassport = dataForSearch.XmlNationality;

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
                                                                                    //objrq.Request = Hotel.Traveloka_RequestOnly_Async(Xhotel.URL, Xhotel.UserName, Xhotel.PassWord, SearchPara, XMLCity, IdsForSearch, custIP, Currency);
                                                                                    objrq.Request = Traveloka_AvailabilityRequest(Xhotel.URL, Xhotel.UserName, Xhotel.PassWord, SearchPara, dataForSearch.xmlCity, IdsForSearch, custIP, Currency);
                                                                                    objrq.Response = ""; RQRS.Add(objrq);
                                                                                }
                                                                                Xhotel.RQRS = RQRS;
                                                                                Xhotel.TechName = TravelokaDetails.TechName;
                                                                                Xhotel.ReturnRooms = TravelokaDetails.ReturnRooms;
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
                                                                                Xhotel.SearchTimeOut = Convert.ToInt32(TravelokaSearchConfig[k].HotelSearchTimeOut);
                                                                                //if (Hit > 0)
                                                                                Xhotel.ProcessTimeout = Convert.ToInt32(TravelokaSearchConfig[k].HotelSearchTimeOut2);
                                                                                //Xhotel.URL = Convert.ToString(TravelokaDetails[0].WebSiteURL) + Convert.ToString(_configuration["Tarveloka_HotelBulk"]);
                                                                                if (_keySettings.SupplierProxy == "0")
                                                                                    Xhotel.URL = Convert.ToString(TravelokaDetails.WebSiteURL) + TravelokaInfo.Tarveloka_HotelBulk;
                                                                                else
                                                                                    Xhotel.URL = Convert.ToString(TravelokaDetails.WebSiteURLforAPI) + TravelokaInfo.Tarveloka_HotelBulk;
                                                                                Xhotel.UserId = Convert.ToString(TravelokaDetails.UserID);
                                                                                Xhotel.UserName = UserName;
                                                                                Xhotel.PassWord = Password;
                                                                                Xhotel.CurrencyCode = Currency;
                                                                                Xhotel.CurrencyFactor = CurrencyFactorTraveloka;
                                                                                Xhotel.XmlCity = dataForSearch.xmlCity;
                                                                                // Xhotel.HotelID = HotelId;
                                                                                Xhotel.XmlCountry = "";
                                                                                Xhotel.XmlNationality = dataForSearch.XmlNationality;
                                                                                string PaxPassport = dataForSearch.XmlNationality;

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
                                                                                    objrq.Request = Traveloka_BulkRoomRequest(SearchPara, IdsForSearch, Xhotel.URL, UserName, Password, dataForSearch.XmlNationality, custIP, Currency);
                                                                                    objrq.Response = "";
                                                                                    RQRS.Add(objrq);
                                                                                }
                                                                                Xhotel.RQRS = RQRS;
                                                                                Xhotel.TechName = TravelokaDetails.TechName;
                                                                                Xhotel.ReturnRooms = TravelokaDetails.ReturnRooms;
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


        }
        public string Traveloka_AvailabilityRequest(string Url, string UserName, string Password, HotelSearchParameter sp, string CityID, string HotelID, string custIP, string Currency)
        {
            try
            {
                string CheckInDate = sp.CheckInDate;
                CheckInDate = CheckInDate.Split('/')[2] + "-" + CheckInDate.Split('/')[1] + "-" + CheckInDate.Split('/')[0];
                string CheckOutDate = sp.CheckOutDate;
                CheckOutDate = CheckOutDate.Split('/')[2] + "-" + CheckOutDate.Split('/')[1] + "-" + CheckOutDate.Split('/')[0];

                string Request = "";
                TravelokaHotelListRequest.TravelokaListRequest t = new TravelokaHotelListRequest.TravelokaListRequest();
                t.checkInDate = CheckInDate;
                t.checkOutDate = CheckOutDate;

                List<string> lstIds = new List<string>();
                for (int id = 0; id < HotelID.Split(',').Length; id++)
                    lstIds.Add(HotelID.Split(',')[id]);

                t.hotelIds = lstIds;
                t.limit = "100";
                t.offset = "0";
                t.options = null;

                List<string> lstType = new List<string>();
                lstType.Add("PAY_NOW");
                t.rateTypes = lstType;

                TravelokaHotelListRequest.RoomGroup rg = new TravelokaHotelListRequest.RoomGroup();

                List<TravelokaHotelListRequest.Room> lstr = new List<TravelokaHotelListRequest.Room>();
                for (int rooms = 0; rooms < sp.Rooms.Count; rooms++)
                {
                    int total = 0;
                    total = sp.Rooms[rooms].NoofAdults + sp.Rooms[rooms].NoofChild;
                    TravelokaHotelListRequest.Room r = new TravelokaHotelListRequest.Room();
                    r.numOfAdults = Convert.ToString(total);
                    lstr.Add(r);
                }

                rg.rooms = lstr;
                t.roomGroup = rg;


                //add by jay
                List<bool> lstRating = new List<bool>();
                for (int star = 0; star < 5; star++)
                    lstRating.Add(true);

                //end

                //List<bool> lstRating = new List<bool>();
                //for (int star = 0; star < sp.SelectedStart.Split(',').Length; star++)
                //    lstRating.Add(true);

                t.stars = lstRating;

                TravelokaHotelListRequest.Metadata m = new TravelokaHotelListRequest.Metadata();
                m.clientInterface = "Desktop";
                m.currencyCode = Currency; // Convert.ToString(ConfigurationManager.AppSettings["TravelokaCurrency"]);
                m.customerIpAddress = custIP;
                m.customerSessionId = Guid.NewGuid().ToString();
                m.customerUserAgent = "Desktop";
                m.locale = "en_ID";

                t.metadata = m;
                Request = JsonConvert.SerializeObject(t);
                return Request;
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
                // return "";
            }
            return string.Empty;
        }

        public string Traveloka_BulkRoomRequest(HotelSearchParameter sp, string HotelId, string Url, string UserName, string Password, string XmlNationality, string custIP, string Currency)
        {
            try
            {
                string RoomRq = "";

                TravelokaBulkRoomRequest.Root r = new TravelokaBulkRoomRequest.Root();

                string CheckInDate = sp.CheckInDate;
                CheckInDate = CheckInDate.Split('/')[2] + "-" + CheckInDate.Split('/')[1] + "-" + CheckInDate.Split('/')[0];
                string CheckOutDate = sp.CheckOutDate;
                CheckOutDate = CheckOutDate.Split('/')[2] + "-" + CheckOutDate.Split('/')[1] + "-" + CheckOutDate.Split('/')[0];

                r.checkInDate = CheckInDate;
                r.checkOutDate = CheckOutDate;

                TravelokaBulkRoomRequest.Metadata m = new TravelokaBulkRoomRequest.Metadata();
                m.locale = "en_ID";
                m.customerUserAgent = "Desktop";
                m.customerSessionId = Guid.NewGuid().ToString();
                m.customerIpAddress = custIP;
                m.currencyCode = Currency;
                m.clientInterface = "Desktop";

                TravelokaBulkRoomRequest.RoomGroup rg = new TravelokaBulkRoomRequest.RoomGroup();
                List<TravelokaBulkRoomRequest.Room> lstRoom = new List<TravelokaBulkRoomRequest.Room>();
                for (int room = 0; room < sp.Rooms.Count; room++)
                {
                    int total = 0;
                    total = sp.Rooms[room].NoofAdults + sp.Rooms[room].NoofChild;
                    TravelokaBulkRoomRequest.Room rr = new TravelokaBulkRoomRequest.Room();
                    rr.numOfAdults = Convert.ToString(total);
                    lstRoom.Add(rr);
                }
                rg.rooms = lstRoom;

                List<string> lstRate = new List<string>();
                lstRate.Add("PAY_NOW");

                List<string> lst = new List<string>();
                lst.Add("ROOM_AMENITIES");
                lst.Add("PROPERTY_DETAILS");


                List<string> lstIds = new List<string>();
                for (int id = 0; id < HotelId.Split(',').Length; id++)
                    lstIds.Add(HotelId.Split(',')[id]);

                //t.hotelIds = lstIds;
                //ennd

                r.metadata = m;
                r.roomGroup = rg;
                r.propertyIds = lstIds;
                r.options = lst;
                r.rateTypes = lstRate;

                RoomRq = JsonConvert.SerializeObject(r);
                return RoomRq;
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
                return "";
            }
        }

        public void Get_Traveloka_Token(ref string TravelokaToken, string Url)
        {
            try
            {
                string res = GetSupplierResponseTraveloka_GenerateToken(Url);
                //TravelokaToken.TravelokaTokenResponse Response = JsonConvert.DeserializeObject<TravelokaToken.TravelokaTokenResponse>(res);
                TravelokaToken.TravelokaTokenResponse Response = Newtonsoft.Json.JsonConvert.DeserializeObject<TravelokaToken.TravelokaTokenResponse>(res); //140525
                TravelokaToken = Response.access_token;
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
            }
        }

        public string GetSupplierResponseTraveloka_GenerateToken(string strHost)
        {
            string strOutput = "";
            try
            {
                HttpWebRequest Request;
                Request = (HttpWebRequest)HttpWebRequest.Create(Convert.ToString(TravelokaInfo.TravelokaAuthURL) + "oauth/accesstoken");
                Request.Method = "POST";
                Request.KeepAlive = false;
                Request.ContentType = "application/x-www-form-urlencoded";
                Request.Accept = "application/json";

                var cryptoKey = new X509Certificate2(Convert.ToString(TravelokaInfo.TravelokaKeyPath), "4321");
                Request.ClientCertificates.Add(cryptoKey);

                Stream newStream = null;
                string r = TravelokaInfo.ProductionKey;
                byte[] data = Encoding.ASCII.GetBytes(r);
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                Request.ContentLength = data.Length;
                newStream = Request.GetRequestStream();
                newStream.Write(data, 0, data.Length);
                newStream.Close();

                HttpWebResponse Response = (HttpWebResponse)Request.GetResponse();
                Stream responseStream = Response.GetResponseStream();
                if (Response.ContentEncoding.ToLower().Contains("gzip"))
                    responseStream = new System.IO.Compression.GZipStream(responseStream, System.IO.Compression.CompressionMode.Decompress);
                else if (Response.ContentEncoding.ToLower().Contains("deflate"))
                    responseStream = new System.IO.Compression.DeflateStream(responseStream, System.IO.Compression.CompressionMode.Decompress);
                StreamReader Reader = new StreamReader(responseStream, Encoding.UTF8);
                strOutput = Reader.ReadToEnd();
                responseStream.Close();
                Reader.Close();
                Response.Close();
            }
            catch (WebException ex)
            {
                HttpWebResponse httpResponse = (HttpWebResponse)ex.Response;
                string result = httpResponse.StatusDescription.ToString();
                string result1 = new StreamReader(ex.Response.GetResponseStream()).ReadToEnd();
                return result + "|$|" + result1;
            }
            return strOutput;
        }

        public async Task<List<Options>> Traveloka_HotelSearchWithRooms(HotelX Item, CommonSearch commonSearch, string Currency, string ResponseData, string CityCode, HotelSearchParameter SearchParam, double CurrencyFactorTBO, string XMLnationality, string URLandRequest, string UserName, string PassWord, string URL, Int32 SearchTimeOut, string custIP, Int32 processTimeout, string ResProccessTimerLogActive, string ResProccessTimeValueforSaveLog)
        {
            System.Diagnostics.Stopwatch timerroomx = new System.Diagnostics.Stopwatch();
            timerroomx.Start();
            List<Options> options = new List<Options>();
            try
            {
                #region DataTable
                DataTable DtHotel = _commonMethod.GenerateDataTableNewV2();
                #endregion
                if (ResponseData != string.Empty)
                {
                    #region PriceOnly RS
                    string Provider = Item.Supplier;

                    Dictionary<string, Dictionary<string, TravelokaRoomResponse.RootObject>> d = null;
                    Dictionary<string, TravelokaRoomResponse.RootObject> h = null;
                    TravelokaListResponse.RootObject k = null;

                    #endregion

                    double TotalCharges = 0;

                    if (SearchParam.Filters.IsShowRooms == 0)
                    {
                        #region "When No Need Rooms"
                        //k = JsonConvert.DeserializeObject<TravelokaListResponse.RootObject>(ResponseData);
                        k = Newtonsoft.Json.JsonConvert.DeserializeObject<TravelokaListResponse.RootObject>(ResponseData); //140525
                        if (k == null || k.properties == null)
                        {
                            Item.ResponseStatus = 404;
                            Item.ResponseType = "NoCount";
                            return options;
                        }

                        if (k.properties.Count < 1)
                        {
                            Item.ResponseStatus = 404;
                            Item.ResponseType = "NoCount";
                            return options;
                        }

                        for (int u = 0; u < k.properties.Count; u++)
                        {
                            string ItemCode = Convert.ToString(k.properties[u].propertyId);
                            string HotelName = Convert.ToString(k.properties[u].propertySummary);

                            Item.ResponseType = "Success";

                            DataRow drXmlHotel = DtHotel.NewRow();
                            //string InternalHotelId = Convert.ToString(ItemCode);
                            drXmlHotel["XMLHotelCode"] = ItemCode;
                            drXmlHotel["HotelName"] = HotelName;
                            drXmlHotel["SupplierName"] = Provider;
                            drXmlHotel["OfficeID"] = 0;
                            drXmlHotel["ContractID"] = 0;
                            drXmlHotel["Rating"] = 0;
                            drXmlHotel["Reviews"] = 0;
                            drXmlHotel["UniqueNo"] = ItemCode;
                            drXmlHotel["IsShowSupplier"] = false;
                            drXmlHotel["BookingStatus"] = "Available";
                            drXmlHotel["XMLCity"] = CityCode;
                            drXmlHotel["SupplierCurrency"] = Currency;
                            drXmlHotel["AvailToken"] = "";

                            double Factor = 1;
                            if ((Convert.ToString(k.properties[u].cheapestRoom.chargeableRate.currencyCode)).ToLower() != "usd")
                            {
                                continue;
                                //DataTable dtInrCurrency = Common.GetCountryCurrency(Convert.ToString(k.properties[u].cheapestRoom.chargeableRate.currencyCode).ToString());
                                // Factor = Convert.ToDouble(dtInrCurrency.Rows[0]["AmtToExch"]);
                            }
                            double FinalTotal = Convert.ToDouble(k.properties[u].cheapestRoom.chargeableRate.chargeableRateInfo.netPrice) * Factor;

                            drXmlHotel["FinalSellingPrice"] = FinalTotal;
                            drXmlHotel["SellingPrice"] = FinalTotal;
                            DtHotel.Rows.Add(drXmlHotel);
                            TotalCharges = FinalTotal;

                            if (TotalCharges > 0)
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
                                    //Options ops = (HotelXML_Suppliers.GetCommonRoomForSuppliers(GiataId, commonSearch.tokenData, commonSearch.advancedOptions, Convert.ToDouble(LowestPrice), drXmlHotel, dt_HotelRoom, dt_Cancellation, dt_RateBreackUp, SearchParam));
                                    Options ops = (await _supplierRes.GetCommonRoomForSuppliers(GiataId, commonSearch.tokenData, commonSearch.advancedOptions, Convert.ToDouble(LowestPrice), drXmlHotel, dt_HotelRoom, dt_Cancellation, dt_RateBreackUp, SearchParam));
                                    if (ops != null)
                                        options.Add(ops);
                                }
                                #endregion
                            }

                        }
                        #endregion
                    }
                    else
                    {
                        #region "When Need Rooms"
                        //d = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, TravelokaRoomResponse.RootObject>>>(ResponseData);
                        d = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, TravelokaRoomResponse.RootObject>>>(ResponseData);//140525
                        if (d == null)
                        {
                            Item.ResponseStatus = 404;
                            return options;
                        }
                            

                        h = d.Values.ElementAt(0);

                        if (h == null)
                        {
                            Item.ResponseStatus = 404;
                            return options;
                        }
                            

                        if (h.Count < 1)
                        {
                            Item.ResponseStatus = 404;
                            return options;
                        }
                            

                        for (int i = 0; i < h.Count; i++)
                        {
                            TravelokaRoomResponse.RootObject TResponse = h.Values.ElementAt(i);

                            Item.ResponseType = "Success";

                            string ItemCode = Convert.ToString(TResponse.propertyId);
                            string HotelName = Convert.ToString(TResponse.propertySummary);



                            double FinalMinPrice = 0;
                            #region Hotels
                            #region "Hotel Block here"
                            DataRow drXmlHotel = DtHotel.NewRow();
                            //string InternalHotelId = Convert.ToString(ItemCode);
                            drXmlHotel["XMLHotelCode"] = ItemCode;
                            drXmlHotel["HotelName"] = HotelName;
                            drXmlHotel["SupplierName"] = Provider;
                            drXmlHotel["OfficeID"] = 0;
                            drXmlHotel["ContractID"] = 0;
                            drXmlHotel["Rating"] = 0;
                            drXmlHotel["Reviews"] = 0;
                            drXmlHotel["UniqueNo"] = ItemCode;
                            drXmlHotel["IsShowSupplier"] = false;
                            drXmlHotel["BookingStatus"] = "Available";
                            drXmlHotel["XMLCity"] = CityCode;
                            drXmlHotel["SupplierCurrency"] = Currency;
                            drXmlHotel["AvailToken"] = "";

                            //   DataRow[] drhotel = GRNCResponsedata.Tables["hotels"].Select("HotelCode='" + ItemCode + "'");
                            double Factor = 1;

                            if (TResponse.rooms == null || TResponse.rooms.Count == 0)
                                continue;

                            if ((Convert.ToString(TResponse.rooms[0].rates.convertedChargeableRate.currencyCode)).ToLower() != commonSearch.tokenData.SiteCurrency.ToLower())
                            {
                                continue;
                                //DataTable dtInrCurrency = Common.GetCountryCurrency(Convert.ToString(TResponse.rooms[0].rates.convertedChargeableRate.currencyCode).ToString());
                                //Factor = Convert.ToDouble(dtInrCurrency.Rows[0]["AmtToExch"]);
                            }
                            double FinalTotal = Convert.ToDouble(TResponse.rooms[0].rates.chargeableRate.chargeableRateInfo.netPrice) * Factor;


                            drXmlHotel["FinalSellingPrice"] = FinalTotal;
                            drXmlHotel["SellingPrice"] = FinalTotal;
                            DtHotel.Rows.Add(drXmlHotel);
                            TotalCharges = FinalTotal;
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
                                DataTable DtTraveloka_HotelRoom = _commonMethod.GenerateRoomtypeDataTableNewV2();
                                DataTable dtTraveloka_Cancellation = _commonMethod.GenerateCancellationDataTableNewV2();
                                DataTable DtTraveloka_RateBreackUp = _commonMethod.GenerateoomRateBreackUpDataTableNewV2();

                                int CutOffDays = CacheMaster.CutOffDays;
                                CutOffDays = _commonMethod.GetCutOffDays(commonSearch.dtSuplierMarkup, Provider);


                                #region "Room Logic"

                                string Type = null;
                                double SubAgentMarkup = 0;

                                int IsParentId = 1, agentid = 0;
                                double AgentPercentage = 0.0;
                                double AgentMarkupAmount = 0.0;
                                Boolean isPerce = true;

                                double AgentCompanyPercentage = Convert.ToDouble(SearchParam.AgentCompanyPercentage);
                                bool IsB2C = Convert.ToBoolean(Convert.ToByte(SearchParam.IsB2BOrB2C));
                                bool blnIsShowSupplier = false;
                                _markup.getAgetMarkupSetting(Provider, GiataId.ToString(), commonSearch.dtSuplierMarkup, commonSearch.dtMarkupDetails, ref AgentPercentage, ref AgentMarkupAmount, ref blnIsShowSupplier, ref isPerce, commonSearch.AgentId, RatingHotel, SearchParam.CityID, SearchParam.CountryID.ToString(), SearchParam.CheckInDate);
                                double B2CMarkup = (IsB2C == false ? AgentCompanyPercentage : XMLHotelBookingCommon.B2CMarkup);
                                #region Supplier Room Code block

                                DataRow[] drHotelDetails = DtHotel.Select("OwnsystemHotelId='" + OwnsystemHotelId + "'");
                                string[] HotelDetails = Convert.ToString(drHotelDetails[0]["AvailToken"]).Split(':');

                                string Error = "";
                                int Adult = 0;
                                int Child = 0;
                                for (int ad = 0; ad < SearchParam.Rooms.Count; ad++)
                                {
                                    Adult += SearchParam.Rooms[ad].NoofAdults;
                                    Child += SearchParam.Rooms[ad].NoofChild;
                                }
                                string Info = "";
                                if (TResponse.propertyDetail != null)
                                {
                                    Info += TResponse.propertyDetail.hotelPolicy == "" ? "" : "<p> <b> Hotel Policy:</b> " + TResponse.propertyDetail.hotelPolicy + " <p> <br/>";
                                    Info += TResponse.propertyDetail.checkInInstruction == "" ? "" : "<p> <b> CheckIn Instruction: </b>" + TResponse.propertyDetail.checkInInstruction + " <p> <br/>";
                                    Info += TResponse.propertyDetail.importanceNotice == "" ? "" : "<p> <b> Important Notice: </b>" + TResponse.propertyDetail.importanceNotice + " <p> <br/>";
                                    Info += TResponse.propertyDetail.checkInTime == "" ? "" : "<p> <b> Check In Time:</b> " + TResponse.propertyDetail.checkInTime + " <p> <br/>";
                                    Info += TResponse.propertyDetail.checkOutTime == "" ? "" : "<p> <b> Check Out Time: </b>" + TResponse.propertyDetail.checkOutTime + " <p> <br/>";
                                }

                                if (TResponse.rooms != null)
                                {
                                    for (int r = 0; r < TResponse.rooms.Count; r++)
                                    {
                                        double CompanyBuying = Convert.ToDouble(TResponse.rooms[r].rates.chargeableRate.chargeableRateInfo.netPrice) * Factor;
                                        string RoomTypeId = TResponse.rooms[r].rates.rateKey + "$" + TResponse.rooms[r].id + "$" + TResponse.rooms[r].quotedOccupancy;
                                        string RoomTypeName = TResponse.rooms[r].name;
                                        if (!string.IsNullOrEmpty(TResponse.rooms[r].bedroomSummary))
                                            RoomTypeName += "," + TResponse.rooms[r].bedroomSummary;
                                        string MealName = "RO";
                                        if (TResponse.rooms[r].breakfastIncluded)
                                            MealName = "BB";
                                        //  Info += TResponse.rooms[r].description + "<br/> <p>" + TResponse.rooms[r].bedroomSummary + "</p>";

                                        #region RateBreakUp
                                        //for (int n = 0; n < SearchParam.NoofNights; n++)
                                        //{
                                        //    DateTime dtCheckInDateForRate = DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null);
                                        //    string RateWeekColumn = Convert.ToString(dtCheckInDateForRate.Date.AddDays(n).DayOfWeek).Substring(0, 3);
                                        //    #region Vat
                                        //    Double VatOnRateBrackUp = 0;
                                        //    double PriceRoom = _markup.ProcessHotelRateBreakupPrice_WithMarkup(1, SearchParam.CityID, SearchParam.CountryID.ToString(), CompanyBuying / SearchParam.NoofNights, B2CMarkup, IsParentId, isPerce, AgentPercentage, AgentMarkupAmount, SubAgentMarkup, ref VatOnRateBrackUp);
                                        //    #endregion
                                        //    DataRow drRateBreackUp = DtTraveloka_RateBreackUp.NewRow();
                                        //    drRateBreackUp["SupplierName"] = Provider;
                                        //    drRateBreackUp["OwnsystemHotelId"] = OwnsystemHotelId;
                                        //    drRateBreackUp["ContractID"] = 0;
                                        //    drRateBreackUp["RoomTypeName"] = RoomTypeName;
                                        //    drRateBreackUp["RoomTypeId"] = RoomTypeId;
                                        //    drRateBreackUp["MealId"] = MealName;
                                        //    drRateBreackUp["Meal"] = MealName;
                                        //    drRateBreackUp["DateOfWeek"] = RateWeekColumn;
                                        //    drRateBreackUp["Date"] = dtCheckInDateForRate.Date.AddDays(n).Date;
                                        //    drRateBreackUp["Rate"] = PriceRoom;
                                        //    drRateBreackUp["RateType"] = "A Rate";
                                        //    drRateBreackUp["RoomNo"] = 1;
                                        //    drRateBreackUp["RoomSequenceNo"] = "";
                                        //    drRateBreackUp["BookingStatus"] = "Available";
                                        //    drRateBreackUp["SupplierCurrency"] = Currency;
                                        //    drRateBreackUp["BuyingPrice"] = PriceRoom / Factor;
                                        //    DtTraveloka_RateBreackUp.Rows.Add(drRateBreackUp);
                                        //}
                                        #endregion

                                        #region Rooms
                                        DataRow drRoom = DtTraveloka_HotelRoom.NewRow();
                                        drRoom["OwnsystemHotelId"] = OwnsystemHotelId;
                                        drRoom["RoomTypeName"] = RoomTypeName;
                                        drRoom["RoomTypeCode"] = TResponse.rooms[r].id;
                                        drRoom["MealId"] = MealName;
                                        drRoom["MealName"] = MealName;
                                        drRoom["ContractID"] = 0;
                                        drRoom["BoardType"] = MealName;
                                        drRoom["RoomTypeId"] = RoomTypeId;
                                        drRoom["AdultsOccupancy"] = Adult;
                                        drRoom["ChildOccupancy"] = Child;
                                        drRoom["MaxExtraBed"] = 0;
                                        drRoom["RoomNo"] = 1;
                                        drRoom["RoomSequenceNo"] = "";
                                        drRoom["BookingStatus"] = "Available";
                                        drRoom["UniqueNo"] = OwnsystemHotelId;
                                        string promoDes = "", promoName = "";
                                        if (TResponse.rooms[r].rates.promos != null)
                                        {
                                            for (int p = 0; p < TResponse.rooms[r].rates.promos.Count; p++)
                                            {
                                                promoDes += "<p>" + TResponse.rooms[r].rates.promos[p].description + "</p>";
                                                promoName += "<p>" + TResponse.rooms[r].rates.promos[p].name + "</p>";
                                            }
                                        }
                                        drRoom["OfferDescription"] = promoDes;
                                        drRoom["OfferCode"] = promoName;
                                        drRoom["OfficeId"] = 0;
                                        drRoom["EssentialInformation"] = Info + TResponse.rooms[r].description + "<br/> <p>" + TResponse.rooms[r].bedroomSummary + "</p>"; ;
                                        drRoom["SupplierName"] = Provider;

                                        double ItemPrice = CompanyBuying;
                                        _markup.ProcessHotelRoom_WithMarkup(Factor, Currency, false, true, SearchParam.NoofNights, SearchParam.CityID, SearchParam.CountryID.ToString(), SearchParam.CheckInDate, SearchParam.CheckOutDate, ref ItemPrice, ref drRoom, isPerce, AgentPercentage, AgentMarkupAmount, SubAgentMarkup, B2CMarkup, IsParentId);

                                        drRoom["SupplierName"] = Provider;
                                        DtTraveloka_HotelRoom.Rows.Add(drRoom);
                                        #endregion

                                        #region  "Cancellationpolicy"
                                        for (int c = 0; c < TResponse.rooms[r].rates.cancellationPolicies.Count; c++)
                                        {
                                            double CancellationPrice = Convert.ToDouble(TResponse.rooms[r].rates.cancellationPolicies[c].fees) * Factor;
                                            DateTime FromDate = Convert.ToDateTime(TResponse.rooms[r].rates.cancellationPolicies[c].cancelDateTime);
                                            DateTime ToDate = Convert.ToDateTime(TResponse.rooms[r].rates.cancellationPolicies[c].cancelDateTime);
                                            double AppliedAgentCharges = CancellationPrice;

                                            #region "Vat"
                                            double VatOnPolicy = 0;
                                            double MainAgentCharge = 0;
                                            AppliedAgentCharges = _markup.ProcessHotelCancelationPrice_WithMarkup(ref MainAgentCharge, SearchParam.NoofNights, SearchParam.CityID, SearchParam.CountryID.ToString(), CancellationPrice, isPerce, B2CMarkup, AgentPercentage, AgentMarkupAmount, IsParentId, SubAgentMarkup, ref VatOnPolicy);
                                            #endregion

                                            //int CutOffDays = -1;// XMLHotelBookingCommon.CutOffDays;
                                            //CutOffDays = Common.GetCutOffDays(dtSuplierMarkup, "Traveloka");
                                            DateTime CancellationDate = DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", CultureInfo.InvariantCulture).Date;
                                            DateTime NowDate = DateTime.Now.Date;

                                            if (FromDate.AddDays(CutOffDays).Date > DateTime.Now.Date)
                                                CancellationDate = FromDate.AddDays(CutOffDays).Date;
                                            else
                                                CancellationDate = DateTime.Now.Date;

                                            if (NowDate.Date < CancellationDate.Date)
                                            {
                                                double Zero = 0;
                                                if (AppliedAgentCharges == 0)
                                                {
                                                    dtTraveloka_Cancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref Zero, dtTraveloka_Cancellation.NewRow(), Factor, Currency, Provider, Convert.ToString(OwnsystemHotelId), Convert.ToString(1), RoomTypeId, RoomTypeName, MealName, NowDate, CancellationDate.AddDays(-1).AddHours(23).AddMinutes(59).AddSeconds(59), 0, 0, "", VatOnPolicy, "", "", "", "", "", ""));
                                                    dtTraveloka_Cancellation.AcceptChanges();
                                                }
                                                else
                                                {
                                                    dtTraveloka_Cancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref Zero, dtTraveloka_Cancellation.NewRow(), Factor, Currency, Provider, Convert.ToString(OwnsystemHotelId), Convert.ToString(1), RoomTypeId, RoomTypeName, MealName, CancellationDate.Date, DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null), AppliedAgentCharges, AppliedAgentCharges, "", VatOnPolicy, "", "", "", "", "", ""));
                                                    dtTraveloka_Cancellation.AcceptChanges();
                                                }
                                            }
                                            else
                                            {
                                                double Zero = 0;
                                                if (AppliedAgentCharges == 0)
                                                {
                                                    AppliedAgentCharges = _markup.ProcessHotelCancelationPrice_WithMarkup(ref MainAgentCharge, SearchParam.NoofNights, SearchParam.CityID, SearchParam.CountryID.ToString(), CompanyBuying, isPerce, B2CMarkup, AgentPercentage, AgentMarkupAmount, IsParentId, SubAgentMarkup, ref VatOnPolicy);
                                                }
                                                dtTraveloka_Cancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref Zero, dtTraveloka_Cancellation.NewRow(), Factor, Currency, Provider, Convert.ToString(OwnsystemHotelId), Convert.ToString(1), RoomTypeId, RoomTypeName, MealName, DateTime.Now.Date, DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null), AppliedAgentCharges, AppliedAgentCharges, "", VatOnPolicy, "", "", "", "", "", ""));
                                                dtTraveloka_Cancellation.AcceptChanges();
                                                break;
                                            }
                                        }

                                        if (TResponse.rooms[r].rates.cancellationPolicies.Count == 0)
                                        {

                                            double CancellationPrice = CompanyBuying;
                                            #region "Vat"
                                            double VatOnPolicy = 0;
                                            double MainAgentCharge = 0;
                                            double AppliedAgentCharge = _markup.ProcessHotelCancelationPrice_WithMarkup(ref MainAgentCharge, SearchParam.NoofNights, SearchParam.CityID, SearchParam.CountryID.ToString(), CancellationPrice, isPerce, B2CMarkup, AgentPercentage, AgentMarkupAmount, IsParentId, SubAgentMarkup, ref VatOnPolicy);
                                            #endregion
                                            dtTraveloka_Cancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref MainAgentCharge, dtTraveloka_Cancellation.NewRow(), Factor, Currency, Provider, Convert.ToString(OwnsystemHotelId), Convert.ToString(1), RoomTypeId, RoomTypeName, MealName, DateTime.Now.Date, DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null), AppliedAgentCharge, AppliedAgentCharge, "", VatOnPolicy, "", "", "", "", "", ""));
                                            dtTraveloka_Cancellation.AcceptChanges();
                                            break;

                                        }
                                        #endregion
                                    }
                                }

                                #endregion


                                #endregion
                                Options ops = (await _supplierRes.GetCommonRoomForSuppliers(GiataId, commonSearch.tokenData, commonSearch.advancedOptions, FinalMinPrice, drXmlHotel, DtTraveloka_HotelRoom, dtTraveloka_Cancellation, DtTraveloka_RateBreackUp, SearchParam));
                                //Options ops = (await _supplierRes.GetCommonRoomForSuppliers(GiataId, commonSearch.tokenData, commonSearch.advancedOptions, MinPrice, drXmlHotel, dtHotelBedsV1Room, dtHotelBedsV1RoomCacelationPolicy, DtRoomRateBreackUp, SearchParam));
                                if (ops != null)
                                    options.Add(ops);
                            }
                            #endregion
                            #endregion
                        }

                        #endregion

                    }

                }
                else
                {
                    Item.ResponseStatus = 204;
                }


                return options;
            }
            catch (Exception ex)
            {
                Item.ResponseStatus = 404;
                await _exception.Error(ex);
                Item.ResponseType = "NoCount";
                return options;
            }
            finally
            {
                #region ResProcessTimer
                try
                {
                    timerroomx.Stop();
                    Item.CodeProcessingTimeSupplier = timerroomx.ElapsedMilliseconds;
                    if (SearchParam != null && ResProccessTimerLogActive == "1" && timerroomx.ElapsedMilliseconds > Convert.ToInt32(ResProccessTimeValueforSaveLog))
                    {
                       
                        SearchParam.SupplierTime += ",|Finish:Traveloka Process Response  " + "|Time :" + timerroomx.Elapsed.ToString();
                        _ = Task.Run(() => _commonMethod.FireAway_SQL_ResPorcessTimeLog("", timerroomx.Elapsed.ToString(), Item.Supplier, SearchParam.SearchKey, 1, SearchParam.AgentID));

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

        //prebook 22-07-25

        public async Task<PriceCheckTravelokaDto> Get_Traveloka_PriceCheckRQRS(DataSet dsCartDetails, DataRow[] TravelokaDetails, int i, int j, DataTable dtCheckPriceReqStructure, double FinalPrice, bool isFromBooking, string time, string responsetype, double CompanyBuyingPrice, TokenData tokenData,string Supplier, int AgentId = 0, string platform = "")
        {
            PriceCheckTravelokaDto priceCheckTravelokaDto = new PriceCheckTravelokaDto();
            string SupplierName = Supplier;
            string Error = "";
            string Request = "";
            string ResponseData = "";
            try
            {
                #region Price Check RQ and Declaration
                string TravelokaCurrency = Convert.ToString(TravelokaDetails[0]["CurrencyShortName"]);
                double CurrencyFactor = 1;
                if (TravelokaCurrency.ToLower() != tokenData.SiteCurrency.ToLower())
                {
                    CurrencyFactor = _commonMethod.GetROEByCurrency(TravelokaCurrency, tokenData.dtROE);
                }

                string HotelId = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["XMLHotelId"]);
                int UserCartId = Convert.ToInt32(dsCartDetails.Tables[0].Rows[i]["UserCartID"]);
                int UserCartDetailId = Convert.ToInt32(dsCartDetails.Tables[1].Rows[j]["UserCartDetailId"]);
                string RoomTypeId = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomTypeID"]);

                string CheckIn = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["CheckInDate"]);
                CheckIn = CheckIn.Substring(0, 4) + "-" + CheckIn.Substring(4, 2) + "-" + CheckIn.Substring(6, 2);
                string CheckOut = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["CheckOutDate"]);
                CheckOut = CheckOut.Substring(0, 4) + "-" + CheckOut.Substring(4, 2) + "-" + CheckOut.Substring(6, 2);

                Request = Get_Traveloka_PreBookRQ(CheckIn, CheckOut, HotelId, RoomTypeId, dsCartDetails, TravelokaCurrency, platform);
                _commonMethod.ThirdPartyBookingHistory(Convert.ToString(UserCartId), Convert.ToString(UserCartDetailId), Request, "--", "Before request hit Price check", SupplierName, "response confirm", "-", "WE");
                #endregion

                if (Request != "")
                {
                    if (isFromBooking == false)
                        dtCheckPriceReqStructure.Rows.Add(_commonMethod.AddCheckPriceReq(dtCheckPriceReqStructure.NewRow(), j + 1, UserCartId, UserCartDetailId, RoomTypeId, SupplierName, Request));

                    #region Price Check RS

                    System.Diagnostics.Stopwatch timerroomprocess = new System.Diagnostics.Stopwatch();
                    timerroomprocess.Start();

                    ResponseDto responseDto = await GetSupplierResponseTraveloka(Convert.ToString(TravelokaDetails[0]["WebSiteURL"]), Request, "prebook");
                    ResponseData = responseDto.Response;
                    Error = responseDto.Error;

                    timerroomprocess.Stop();
                    time = timerroomprocess.Elapsed.ToString();

                    _commonMethod.ThirdPartyBookingHistory(Convert.ToString(UserCartId), Convert.ToString(UserCartDetailId), Request, ResponseData + Error, "After request hit Price check", SupplierName, "response confirm", "-", "WE");

                    if (ResponseData == "" || ResponseData == null)
                    {
                        responsetype = "Timeout";
                        FinalPrice = 0;
                        //return false;
                    }
                    else
                    {
                        if (!ResponseData.ToLower().Contains("error"))
                        {
                            TravelokaPreBookResponse.RootObject R = JsonConvert.DeserializeObject<TravelokaPreBookResponse.RootObject>(ResponseData);
                            List<TravelokaPreBookResponse.Room> RoomList = R.rooms.Where(x => x.id == RoomTypeId.Split('$')[1] && x.rates.rateKey == RoomTypeId.Split('$')[0]).ToList();

                            #region Price Check Logic
                            if (R.status.ToLower() == "available")
                            {
                                if (RoomList != null)
                                {
                                    FinalPrice = Convert.ToDouble(RoomList[0].rates.chargeableRate.chargeableRateInfo.netPrice);
                                    if (RoomList[0].rates.chargeableRate.currencyCode.ToLower() != tokenData.SiteCurrency.ToLower())
                                        FinalPrice = FinalPrice * CurrencyFactor;
                                    FinalPrice = Convert.ToDouble(Math.Round(FinalPrice, 2));
                                }
                                else
                                    FinalPrice = 0;
                            }
                            else
                                FinalPrice = 0;
                            #endregion
                        }
                        else
                        {
                            FinalPrice = 0;
                        }

                        if (FinalPrice == 0)
                        {
                            responsetype = "GeneralError";
                        }
                        else
                        {
                            responsetype = "Success";
                        }
                    }
                    #endregion
                }
                else
                {
                    FinalPrice = 0;
                }
            }
            catch (Exception e)
            {
                _ = _exception.Error(e);
                FinalPrice = 0;
            }
            if (FinalPrice == 0)
            {
                try
                {
                    responsetype = "GeneralError";
                    //int AgentId = Convert.ToInt32(Common.getSession("UserId") == "" ? "0" : Common.getSession("UserId"));
                    string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, Request, ResponseData, Error);
                }
                catch (Exception e)
                {
                    _ = _exception.Error(e);
                }
            }
            else
            {
                if ((FinalPrice).TruncateDecimalPlaces(0) > (CompanyBuyingPrice).TruncateDecimalPlaces(0))
                {
                    try
                    {
                        ResponseData = "#Price Changed " + "#Main Price = " + CompanyBuyingPrice + " #New Price = " + FinalPrice + " #Response= " + ResponseData;
                        responsetype = "GeneralError";
                        string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, Request, ResponseData, Error);
                    }
                    catch (Exception e)
                    {
                        _ = _exception.Error(e);
                    }
                }
                else
                {
                    try
                    {
                        responsetype = "Success";
                        string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, Request, ResponseData, Error);
                    }
                    catch (Exception e)
                    {
                        _ = _exception.Error(e);
                    }
                }
            }

            priceCheckTravelokaDto.time = time;
            priceCheckTravelokaDto.responsetype  =responsetype;
            priceCheckTravelokaDto.FinalPrice = FinalPrice;

            return priceCheckTravelokaDto;
        }

        public string Get_Traveloka_PreBookRQ(string checkIn, string checkOut, string HotelId, string RateKey, DataSet dsCartDetails, string TravelokaCurrency = "USD", string Platform = "")
        {
            try
            {
                //string Platform = GetUserPlatform();

                string Request = "";
                TravelokaPreBookRequest.RootObject R = new TravelokaPreBookRequest.RootObject();
                R.checkInDate = checkIn;
                R.checkOutDate = checkOut;
                R.propertyId = HotelId;

                List<string> lst = new List<string>();
                R.rateTypes = lst;

                TravelokaPreBookRequest.Metadata m = new TravelokaPreBookRequest.Metadata();
                m.clientInterface = "Desktop";
                m.currencyCode = Convert.ToString(TravelokaCurrency);
                m.customerIpAddress = Platform.Split(new String[] { "$$" }, StringSplitOptions.None)[1];
                m.customerSessionId = Guid.NewGuid().ToString();
                m.customerUserAgent = "Desktop";
                m.locale = "en_ID";

                List<TravelokaPreBookRequest.Room> lstRoom = new List<TravelokaPreBookRequest.Room>();
                for (int r = 0; r < dsCartDetails.Tables[1].DefaultView.Count; r++)
                {
                    TravelokaPreBookRequest.Room rr = new TravelokaPreBookRequest.Room();
                    rr.numOfAdults = RateKey.Split('$')[2];//Convert.ToString(dsCartDetails.Tables[1].DefaultView[r]["NoofAdults"]);
                    lstRoom.Add(rr);
                }

                TravelokaPreBookRequest.RoomGroup rg = new TravelokaPreBookRequest.RoomGroup();
                rg.rateKey = RateKey.Split('$')[0];
                rg.roomId = RateKey.Split('$')[1];
                rg.rooms = lstRoom;

                R.metadata = m;
                R.roomGroup = rg;

                Request = JsonConvert.SerializeObject(R);
                return Request;
            }
            catch (Exception ex)
            {
                _ = _exception.Error(ex);
                return "";
            }
        }

        public async Task<ResponseDto> GetSupplierResponseTraveloka(string strHost, string strxmlRQRS, string Type)
        {
            ResponseDto responseDto = new ResponseDto();
            string Error = "";
            string strOutput = "";
            try
            {
                string Traveloka_Token = "";
                if (!_cache.TryGetValue("Traveloka_Token", out Traveloka_Token))
                {
                    Traveloka_Token = "";
                    Get_Traveloka_Token(ref Traveloka_Token, strHost);
                    if (Traveloka_Token != "")
                        _cache.Set("Traveloka_Token", Traveloka_Token, new MemoryCacheEntryOptions().SetAbsoluteExpiration(DateTime.UtcNow.AddMinutes(60)));
                }

                switch (Type)
                {
                    case "room":
                        strHost += "accom/v1/rooms";
                        break;
                    case "prebook":
                        strHost += "accom/v1/avail";
                        break;
                    case "book":
                        strHost = Convert.ToString(TravelokaInfo.TravelokaBookingURL) + "accom/v1/itin/create";
                        break;
                    case "cancel":
                        strHost = Convert.ToString(TravelokaInfo.TravelokaBookingURL) + "accom/v1/cancellation/request";
                        break;
                    case "itin":
                        strHost = Convert.ToString(TravelokaInfo.TravelokaBookingURL) + "accom/v1/itin/list";
                        break;
                    default:
                        break;
                }
                HttpWebRequest Request;
                Request = (HttpWebRequest)HttpWebRequest.Create(strHost);
                Request.Method = "POST";
                Request.KeepAlive = false;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

                Request.Headers.Add("Authorization", Traveloka_Token);
                Request.Date = DateTime.Now.Date;
                Request.ContentType = "application/json";
                Request.Accept = "application/json";
                byte[] data = Encoding.ASCII.GetBytes(strxmlRQRS);
                Request.ContentLength = data.Length;
                var cryptoKey = new X509Certificate2(Convert.ToString(TravelokaInfo.TravelokaKeyPath), "4321");
                Request.ClientCertificates.Add(cryptoKey);
                Stream newStream = null;
                newStream = Request.GetRequestStream();
                newStream.Write(data, 0, data.Length);
                newStream.Close();

                HttpWebResponse Response = (HttpWebResponse)await Request.GetResponseAsync();
                Stream responseStream = Response.GetResponseStream();
                if (Response.ContentEncoding.ToLower().Contains("gzip"))
                    responseStream = new System.IO.Compression.GZipStream(responseStream, System.IO.Compression.CompressionMode.Decompress);
                else if (Response.ContentEncoding.ToLower().Contains("deflate"))
                    responseStream = new System.IO.Compression.DeflateStream(responseStream, System.IO.Compression.CompressionMode.Decompress);
                StreamReader Reader = new StreamReader(responseStream, Encoding.UTF8);
                strOutput = await Reader.ReadToEndAsync();
                responseStream.Close();
                Reader.Close();
                Response.Close();
            }
            catch (WebException ex)
            {
                Error = ex.Message.ToString();
                HttpWebResponse httpResponse = (HttpWebResponse)ex.Response;
                string result = httpResponse.StatusDescription.ToString();
                string result1 = new StreamReader(ex.Response.GetResponseStream()).ReadToEnd();
                strOutput = result1;

            }
            responseDto.Response = strOutput;
            responseDto.Error = Error;
            return responseDto;
        }

        public string Get_Traveloka_BookingRequest(DataSet dsCartDetails, DataTable dtPassengers, string RoomTypeId, string ReferenceNo, int i, int UserCartDetailId,string TravelokaCurrency,string Platform)
        {
            try
            {
                double CompanyBuyingPrice = Convert.ToDouble(dsCartDetails.Tables[1].DefaultView[0]["Rate"]);
                string CheckIn = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["CheckInDate"]);
                CheckIn = CheckIn.Substring(0, 4) + "-" + CheckIn.Substring(4, 2) + "-" + CheckIn.Substring(6, 2);
                string CheckOut = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["CheckOutDate"]);
                CheckOut = CheckOut.Substring(0, 4) + "-" + CheckOut.Substring(4, 2) + "-" + CheckOut.Substring(6, 2);
                //string Platform = GetUserPlatform();
                string HotelId = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["XMLHotelId"]);

                string Req = "";

                TravelokaBookRequest.RootObject R = new TravelokaBookRequest.RootObject();
                R.affiliateConfirmationIds = ReferenceNo + "-" + UserCartDetailId;

                TravelokaBookRequest.AffiliateContact a = new TravelokaBookRequest.AffiliateContact();
                a.affiliateEmail = "ops@withinearth.com";
                List<string> lstPhone = new List<string>();
                lstPhone.Add("1234567890");

                a.affiliatePhoneNumber = lstPhone;
                R.affiliateContact = a;

                TravelokaBookRequest.ChargeableRateInfo c = new TravelokaBookRequest.ChargeableRateInfo();
                c.netPrice = Convert.ToString(CompanyBuyingPrice);

                R.chargeableRateInfo = c;
                R.checkInDate = CheckIn;
                R.checkOutDate = CheckOut;

                TravelokaBookRequest.ExpectedTotalChargeableRate e = new TravelokaBookRequest.ExpectedTotalChargeableRate();
                e.amount = "0";
                e.currencyCode = TravelokaCurrency;// Convert.ToString(ConfigurationManager.AppSettings["TravelokaCurrency"]);

                R.expectedTotalChargeableRate = e;
                R.loginId = "ops@withinearth.com";
                R.loginType = "EMAIL";
                R.propertyId = HotelId;

                TravelokaBookRequest.Metadata m = new TravelokaBookRequest.Metadata();
                m.clientInterface = "Desktop";
                m.currencyCode = TravelokaCurrency;// Convert.ToString(ConfigurationManager.AppSettings["TravelokaCurrency"]);
                m.customerIpAddress = Platform.Split(new String[] { "$$" }, StringSplitOptions.None)[1];
                m.customerSessionId = Guid.NewGuid().ToString();
                m.customerUserAgent = "Desktop";
                m.locale = "en_ID";

                R.metadata = m;
                List<string> lstRate = new List<string>();
                lstRate.Add("PAY_NOW");
                R.rateTypes = lstRate;

                List<TravelokaBookRequest.Room> lstR = new List<TravelokaBookRequest.Room>();

                for (int rr = 0; rr < dsCartDetails.Tables[1].DefaultView.Count; rr++)
                {
                    TravelokaBookRequest.Room r = new TravelokaBookRequest.Room();

                    List<TravelokaBookRequest.Guest> lstP = new List<TravelokaBookRequest.Guest>();
                    dtPassengers.DefaultView.RowFilter = "PickUp='" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[0]["RoomTypeID"]) + "' and RoomNo='" + (dsCartDetails.Tables[1].DefaultView[0]["RoomNumber"]) + "' and UserCartId='" + dsCartDetails.Tables[0].Rows[i]["UserCartId"] + "'";
                    for (int p = 0; p < dtPassengers.DefaultView.Count; p++)
                    {
                        TravelokaBookRequest.Guest g = new TravelokaBookRequest.Guest();
                        g.firstName = Convert.ToString(dtPassengers.DefaultView[p]["FirstName"]);
                        g.lastName = Convert.ToString(dtPassengers.DefaultView[p]["LastName"]);
                        g.title = Convert.ToString(dtPassengers.DefaultView[p]["Prefix"]).Trim().Replace(".", "").ToUpper();
                        lstP.Add(g);
                    }
                    r.guests = lstP;
                    r.numOfAdult = RoomTypeId.Split('$')[2];
                    r.rateKey = RoomTypeId.Split('$')[0];
                    r.roomId = RoomTypeId.Split('$')[1];
                    lstR.Add(r);
                }
                R.rooms = lstR;
                R.specialRequest = "";

                TravelokaBookRequest.UserContact u = new TravelokaBookRequest.UserContact();
                u.email = "ops@withinearth.com";
                u.firstName = "WithinEarth";
                u.lastName = "Holidays";
                u.title = "MR";
                u.phones = lstPhone;

                R.userContact = u;
                R.userPaymentMethod = "OTHERS";

                Req = JsonConvert.SerializeObject(R);
                return Req;
            }
            catch (Exception ex)
            {
                _ =_exception.Error(ex);
                return "";
            }
        }

        public string Get_Traveloka_BookingCancelRQ(string Ref, string Reason)
        {
            try
            {
                string Request = "";
                TravelokaBookingCancelRQ.RootObject t = new TravelokaBookingCancelRQ.RootObject();
                t.email = "ops@withinearth.com";
                t.itineraryId = Ref.Split('|')[0];
                t.reason = Reason;

                Request = JsonConvert.SerializeObject(t);
                return Request;
            }
            catch (Exception ex)
            {
                _ =_exception.ErrorBooking(ex, Ref);
                return "";
            }
        }

        public string Get_Traveloka_ViewItinerary(string ReferenceNo, string TimeStamp,string Platform, string TravelokaCurrency)
        {
            try
            {
                
                string Request = "";
                TravelokaViewItineraryRQ.RootObject R = new TravelokaViewItineraryRQ.RootObject();

                TravelokaViewItineraryRQ.Metadata M = new TravelokaViewItineraryRQ.Metadata();
                M.currencyCode = Convert.ToString(TravelokaCurrency);
                M.customerIpAddress = Platform.Split(new String[] { "$$" }, StringSplitOptions.None)[1];
                M.customerSessionId = Guid.NewGuid().ToString();
                M.locale = "en_ID";

                //TravelokaViewItineraryRQ.BookingTimeRange B = new TravelokaViewItineraryRQ.BookingTimeRange();
                //B.startTime = TimeStamp;
                //B.offset = "10";
                //B.limit = "10";
                //B.endTime = TimeStamp;

                R.affiliateConfirmationId = ReferenceNo;
                // R.bookingTimeRange = B;
                R.email = "ops@withinearth.com";
                R.itineraryId = "";
                R.metadata = M;

                Request = JsonConvert.SerializeObject(R);
                return Request;
            }
            catch (Exception ex)
            {
                _ =_exception.ErrorNew(ex, ErrorLog._LogAPI.CancelBooking);
                return "";
            }
        }
    }
}
