
//using Newtonsoft.Json;
//using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using XConnect.CommonUtilities.Interface;
using XConnect.ErrorLogHandler.Interface;
using XConnect.Markup.Interface;
using XConnect.Models;
using XConnect.Models.Hotel;
using XConnect.CommonUtilities;
using System.IO.Compression;
using System.Net;
using System.Security.Policy;
//using XConnect.Xconnect.Auth.Interface;
using Microsoft.Extensions.Caching.Memory;
using System.Net.Http;
using XConnect.Supplier.XTG.Interface;
using System.Text.Json;
using System.Xml;
using System.Text.Json.Nodes;
using MongoDB.Bson.IO;
using System.Net.NetworkInformation;
using Microsoft.Extensions.Options;
using Options = XConnect.Models.Options;
using static XConnect.Supplier.Dida.DidaClass;
using static Azure.Core.HttpHeader;
using static XConnect.Supplier.XTG.GraphQLClient;
using Microsoft.Data.SqlClient;
using XConnect.DAL;
using static XConnect.Models.ErrorLog;
using static XConnect.Supplier.Booking.Com.preebookrs;
using Microsoft.AspNetCore.Http;


namespace XConnect.Supplier.XTG
{
    public class XTGRepository : IAllXTG //added trupti
    {
        private readonly ICommonMethod _commonMethod;
        private readonly IException _exception;
        private readonly IMarkup _markup;
        private readonly ISupplierRes _supplierRes;
        private readonly IMemoryCache _cache;
        private readonly KeySettings _keySettings;
        private readonly SupplierConfiguration _supplierConfig;


        public XTGRepository(ICommonMethod commonMethod, IMarkup markup, ISupplierRes supplierRes, IException exception, IMemoryCache cache, IOptions<KeySettings> keySettings, IOptions<SupplierConfiguration> supplierConfig)
        {
            _commonMethod = commonMethod;
            _markup = markup;
            _supplierRes = supplierRes;
            _exception = exception;
            _cache = cache;
            _keySettings = keySettings.Value;
            _supplierConfig = supplierConfig.Value;
        }
        public void GetXTGRequestOnly_async(DataRow[] commonCode, List<StarRating> listRating, DataTable dtROE, bool HitFromAll, HotelSearchParameter SearchPara, List<SupplierDetails> XTGDetails, List<SupplierSearchConfig> XTGSearchConfig, DataTable dtPreferredDataHotel, ref ConcurrentBag<HotelX> AllTask, string Natinality, int Hit, TokenData tokenData)
        {

            try
            {
                #region "XTG"

                if (HitFromAll && (SearchPara.IsSearchFor.ToLower().IndexOf("xtg") >= 0 || SearchPara.IsSearchFor.ToLower().IndexOf("multi") >= 0))
                {
                    if (XTGDetails != null && XTGDetails.Count > 0)
                    {
                        if (XTGDetails.Count > 0)
                        {
                            List<SupplierDetails> XTGMain = new List<SupplierDetails>();
                            List<SupplierDetails> XTGChild = new List<SupplierDetails>();
                            List<StarRating> lstRating = new List<StarRating>();

                            lstRating = _commonMethod.GetStarRatingStringList_xtg(SearchPara);
                            List<HotelCodeByCity> DtHotelDAta = HtmlExtensions.ToList<HotelCodeByCity>(commonCode.CopyToDataTable());


                            int ratingcount = lstRating.Count;
                            int hotelcount = DtHotelDAta.Count;
                            foreach (SupplierDetails item in XTGDetails)
                            {
                                if (item.SupplierName.ToLower() == "xtg")
                                    XTGMain.Add(item);
                                else
                                    XTGChild.Add(item);

                            }
                            //if (XTGMain.Count > 0)
                            {
                                for (int x = 0; x < XTGChild.Count; x++)//need to change
                                                                        //for (int x = 0; x < 2; x++)
                                {
                                    string Ids = "";//"23,1,389,1619,2,10236,63017,1610,9369";
                                    string xmlbase = "";
                                    string Supplier = Convert.ToString(XTGChild[x].SupplierName);
                                    if ("AbreuOnline_XTG".ToLower() == Supplier.ToLower())
                                        continue;
                                    string SupplierID = Convert.ToString(XTGChild[x].SupplierID);
                                    string SupplierCode = Convert.ToString(XTGChild[x].UserID);
                                    string AccessCode = Convert.ToString(XTGChild[x].UserName);

                                    List<SupplierSearchConfig> ChildXTGSearchConfig = new List<SupplierSearchConfig>();

                                    foreach (SupplierSearchConfig item in XTGSearchConfig)
                                    {
                                        if (item != null && item.SupplierID == XTGChild[x].SupplierID)
                                            ChildXTGSearchConfig.Add(item);

                                    }

                                    if (Ids == "")
                                        _commonMethod.GetPrefferedHotelCodes(ref Ids, ref xmlbase, ref ChildXTGSearchConfig, Supplier, dtPreferredDataHotel, SearchPara.IsSearchFor);

                                    if (!CheckRoomValidation(Supplier, SearchPara))
                                        ChildXTGSearchConfig = null;

                                    if (ChildXTGSearchConfig != null)
                                    {
                                        List<HotelCodeByCity> filter = new List<HotelCodeByCity>();
                                        //if (Supplier.ToLower() == "trip.com_xtg")
                                        //{
                                        //    filter = FilterWithOutCity_Xtg_Trip(ratingcount, hotelcount, DtHotelDAta, lstRating, Supplier);
                                        //}
                                        //else
                                        //{
                                        //    filter = FilterWithOutCity_Xtg(1, hotelcount, DtHotelDAta, lstRating, Supplier);
                                        filter = FilterWithOutCity_Xtg_new(ratingcount, hotelcount, DtHotelDAta, lstRating, XTGChild[x].HotelSupplier);//Added on 16-JAN-2025
                                        //}


                                        //string currency = XtgInfo.XTG_Currency;
                                        string currency = Convert.ToString(XTGChild[x].CurrencyShortName);//2502

                                        string language = XtgInfo.XTG_Language; double CurrencyFactor = 1;
                                        GetXtgCurrencyByCountry(Supplier, SearchPara.CountryCode, ref currency);
                                        if (currency.ToLower() != tokenData.SiteCurrency.ToLower())
                                        {
                                            CurrencyFactor = _commonMethod.GetROEByCurrency(currency, dtROE);
                                        }

                                        for (int k = 0; k < ChildXTGSearchConfig.Count; k++)
                                        //for (int k = 0; k < 1; k++)//need to cchannge
                                        {
                                            bool DebugMode = Convert.ToInt32(ChildXTGSearchConfig[k].DebugMode) == 0 ? false : true;
                                            {
                                                if (Convert.ToInt32(ChildXTGSearchConfig[k].CityID) == 0 || Convert.ToInt32(ChildXTGSearchConfig[k].CityID) == SearchPara.CityID)//All City means 0
                                                {
                                                    if (filter.Count > 0)
                                                    {
                                                        int NoOfRecord = Convert.ToInt32(ChildXTGSearchConfig[k].Noofrecords);
                                                        if (Supplier.ToLower() == "rezlive_xtg" || Supplier.ToLower() == "rts_xtg")
                                                        {
                                                            List<XMLBaseID_Search> lst = _commonMethod.GetXMLBAsedIdString(filter);
                                                            for (int gt = 0; gt < lst.Count; gt++)
                                                            //for (int gt = 0; gt < 1; gt++)

                                                            {
                                                                List<HotelCodeByCity> filterwithCity = _commonMethod.FilterWithCityCode(filter, lst[gt].XMLBaseID.ToLower());
                                                                if (commonCode.Length > 0)
                                                                {
                                                                    double count = Convert.ToDouble(filterwithCity.Count);
                                                                    int NoOfDiv = Convert.ToInt32(Math.Ceiling(count / Convert.ToDouble(NoOfRecord)));
                                                                    DataTable[] splittedtables = commonCode.AsEnumerable()
                                                         .Select((row, index) => new { row, index })
                                                         .GroupBy(m => m.index / NoOfRecord)  // integer division, the fractional part is truncated
                                                         .Select(g => g.Select(m => m.row).CopyToDataTable())
                                                         .ToArray();
                                                                    List<List<XMLID_Search>> lstxml = _commonMethod.GetXMLIDString(NoOfDiv, NoOfRecord, count, filterwithCity);
                                                                    //for (int a = 0; a < 1; a++)
                                                                    for (int a = 0; a < NoOfDiv; a++)
                                                                    {
                                                                        string IdsForSearch = Convert.ToString(_commonMethod.GetHotelCodeByCOmma(lstxml[a]));

                                                                        string XTGSiteID = Convert.ToString(XTGMain[0].UserID);
                                                                        string XTGApiKey = Convert.ToString(XTGMain[0].Password);
                                                                        //string XTGWebUrl = Convert.ToString(XTGMain[0].WebSiteURL);

                                                                        string SupplierName = Convert.ToString(XTGChild[x].SupplierName);
                                                                        HotelX xhotel = new HotelX();
                                                                        xhotel.Supplier = SupplierName;
                                                                        xhotel.UserId = XTGSiteID;
                                                                        //xhotel.URL = Convert.ToString(XTGMain[0].WebSiteURL);
                                                                        if (_keySettings.SupplierProxy == "0")
                                                                            xhotel.URL = Convert.ToString(XTGMain[0].WebSiteURL);
                                                                        else
                                                                            xhotel.URL = Convert.ToString(XTGMain[0].WebSiteURLforAPI);
                                                                        xhotel.PassWord = XTGApiKey;
                                                                        xhotel.CurrencyCode = currency;
                                                                        xhotel.CurrencyFactor = CurrencyFactor;
                                                                        xhotel.DebugeMode = DebugMode;
                                                                        xhotel.SearchTimeOut = Convert.ToInt32(ChildXTGSearchConfig[k].HotelSearchTimeOut);
                                                                        //if (Hit > 0)
                                                                        xhotel.ProcessTimeout = Convert.ToInt32(ChildXTGSearchConfig[k].HotelSearchTimeOut2);
                                                                        xhotel.XmlNationality = Natinality;


                                                                        #region "CoomonCode"
                                                                        bool IsHitSeperate = _commonMethod.IsSuppliersWithSeperateRoomRquest(xhotel.Supplier);
                                                                        int NoOfRoomsSearch = 1;
                                                                        if (IsHitSeperate)
                                                                            NoOfRoomsSearch = SearchPara.Rooms.Count;
                                                                        List<RQRS> RQRS = new List<RQRS>();
                                                                        xhotel.IsSeperate = IsHitSeperate;
                                                                        for (int rr = 0; rr < NoOfRoomsSearch; rr++)
                                                                        {
                                                                            RQRS objrq = new RQRS();
                                                                            List<RoomDetails> serchRooms = new List<RoomDetails>();
                                                                            serchRooms.Add(SearchPara.Rooms[rr]);
                                                                            objrq.Request = GetXTGSearchRequest(SearchPara, IdsForSearch, language, currency, XTGSiteID, xhotel.SearchTimeOut, SupplierCode, AccessCode, DebugMode, Convert.ToInt32(ChildXTGSearchConfig[k].HotelSearchTimeOut), Natinality);
                                                                            objrq.Response = ""; RQRS.Add(objrq);
                                                                        }
                                                                        xhotel.RQRS = RQRS;
                                                                        xhotel.ReturnRooms = XTGChild[x].ReturnRooms;
                                                                        xhotel.TechName = XTGChild[x].TechName;

                                                                        #endregion
                                                                        AllTask.Add(xhotel);
                                                                    }
                                                                }
                                                            }
                                                        }
                                                        else
                                                        {
                                                            double count = Convert.ToDouble(filter.Count);
                                                            int NoOfDiv = Convert.ToInt32(Math.Ceiling(count / Convert.ToDouble(NoOfRecord)));
                                                            List<List<XMLID_Search>> lstxml = _commonMethod.GetXMLIDString(NoOfDiv, NoOfRecord, count, filter);
                                                            for (int a = 0; a < NoOfDiv; a++)
                                                            {

                                                                string IdsForSearch = Convert.ToString(_commonMethod.GetHotelCodeByCOmma(lstxml[a]));

                                                                string XTGSiteID = Convert.ToString(XTGMain[0].UserID);
                                                                string XTGApiKey = Convert.ToString(XTGMain[0].Password);
                                                                //string XTGWebUrl = Convert.ToString(XTGMain[0].WebSiteURL);

                                                                string SupplierName = Convert.ToString(XTGChild[x].SupplierName);
                                                                //string SupplierID = Convert.ToString(XTGChild["SupplierID"]);
                                                                //string SupplierCode = Convert.ToString(XTGChild["UserId"]);
                                                                //string AccessCode = Convert.ToString(XTGChild["UserName"]);

                                                                HotelX xhotel = new HotelX();
                                                                xhotel.Supplier = SupplierName;
                                                                xhotel.UserId = XTGSiteID;
                                                                //xhotel.URL = Convert.ToString(XTGMain[0].WebSiteURL);
                                                                if (_keySettings.SupplierProxy == "0")
                                                                    xhotel.URL = Convert.ToString(XTGMain[0].WebSiteURL);
                                                                else
                                                                    xhotel.URL = Convert.ToString(XTGMain[0].WebSiteURLforAPI);

                                                                xhotel.PassWord = XTGApiKey;
                                                                xhotel.CurrencyCode = currency;
                                                                xhotel.CurrencyFactor = CurrencyFactor;
                                                                xhotel.DebugeMode = DebugMode;
                                                                xhotel.AgentWiseMarkup = XTGChild[x].AgentWiseMarkup;
                                                                xhotel.IsShowSupplier = Convert.ToInt16(XTGChild[x].IsShowSupplier) == 1 ? true : false;
                                                                xhotel.SupplierID = XTGChild[x].SupplierID;
                                                                // xhotel.MasterSupplierID = XTGChild[x].MasterSupplierId;
                                                                xhotel.SearchTimeOut = Convert.ToInt32(ChildXTGSearchConfig[k].HotelSearchTimeOut);
                                                                //if (Hit > 0)
                                                                xhotel.ProcessTimeout = Convert.ToInt32(ChildXTGSearchConfig[k].HotelSearchTimeOut2);
                                                                xhotel.XmlNationality = Natinality;
                                                                #region "CoomonCode"
                                                                bool IsHitSeperate = _commonMethod.IsSuppliersWithSeperateRoomRquest(xhotel.Supplier);
                                                                int NoOfRoomsSearch = 1;
                                                                if (IsHitSeperate)
                                                                    NoOfRoomsSearch = SearchPara.Rooms.Count;
                                                                List<RQRS> RQRS = new List<RQRS>();
                                                                xhotel.IsSeperate = IsHitSeperate;
                                                                for (int rr = 0; rr < NoOfRoomsSearch; rr++)
                                                                {
                                                                    RQRS objrq = new RQRS();
                                                                    List<RoomDetails> serchRooms = new List<RoomDetails>();
                                                                    serchRooms.Add(SearchPara.Rooms[rr]);
                                                                    objrq.Request = GetXTGSearchRequest(SearchPara, IdsForSearch, language, currency, XTGSiteID, xhotel.SearchTimeOut, SupplierCode, AccessCode, DebugMode, Convert.ToInt32(ChildXTGSearchConfig[k].HotelSearchTimeOut), Natinality);
                                                                    objrq.Response = ""; RQRS.Add(objrq);
                                                                }
                                                                xhotel.RQRS = RQRS;
                                                                xhotel.ReturnRooms = XTGChild[x].ReturnRooms;
                                                                xhotel.TechName = XTGChild[x].TechName;
                                                                #endregion
                                                                AllTask.Add(xhotel);
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
                #endregion
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
            }
        }
        public string GetXTGSearchRequest(HotelSearchParameter SearchParam, string HotelIds, string language, string currencyCode, string client, int Timeout, string SupplierCode, string AccessCode, bool DebugMode, int TimeOut, string Natinality = "")
        {
            try
            {
                string CheckInDate = string.Format("{0:yyyy-MM-dd}", DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null));
                string CheckInOut = string.Format("{0:yyyy-MM-dd}", DateTime.ParseExact(SearchParam.CheckOutDate, "dd/MM/yyyy", null));


                XTGSearchRQ main = new XTGSearchRQ();
                CriteriaSearch criteriaSearch = new CriteriaSearch();
                Settings settings = new Settings();
                settings.client = client;
                //settings.timeout = Timeout;
                //settings.timeout = 10000; 
                settings.timeout = TimeOut; // changed on 011024
                settings.context = SupplierCode;
                settings.testMode = false;
                //settings.auditTransactions = DebugMode;
                settings.auditTransactions = DebugMode;

                Filter filter = new Filter();
                Access access = new Access();
                List<string> IncludeSupplier = new List<string>();
                IncludeSupplier.Add(AccessCode);
                access.includes = IncludeSupplier;
                filter.access = access;

                List<string> hotels = new List<string>();
                List<Occupancy> occupancies = new List<Occupancy>();

                foreach (RoomDetails room in SearchParam.Rooms)
                {
                    List<Pax> paxes = new List<Pax>();
                    Occupancy obj = new Occupancy();
                    for (int i = 0; i < room.NoofAdults; i++)
                    {
                        Pax objPx = new Pax();
                        objPx.age = XtgInfo.AdultAge;
                        paxes.Add(objPx);

                    }
                    for (int i = 0; i < room.NoofChild; i++)
                    {
                        Pax objPx = new Pax();
                        if (i == 0)
                            objPx.age = room.Child1Age;
                        if (i == 1)
                            objPx.age = room.Child2Age;
                        paxes.Add(objPx);
                    }
                    obj.paxes = paxes;
                    occupancies.Add(obj);
                }
                if (HotelIds != string.Empty)
                {
                    string[] hid = HotelIds.Split(',');
                    for (int i = 0; i < hid.Length; i++)
                        hotels.Add(hid[i]);
                }
                if (Natinality == "")
                {
                    return "";
                }

                BusinessRules businessRules = new BusinessRules();
                businessRules.businessRulesType = "CHEAPER_AMOUNT";
                //businessRules.optionsQuota = 1;
                settings.businessRules = businessRules;

                criteriaSearch.checkIn = CheckInDate;
                criteriaSearch.checkOut = CheckInOut;
                criteriaSearch.hotels = hotels;
                criteriaSearch.occupancies = occupancies;
                criteriaSearch.language = language;
                criteriaSearch.currency = currencyCode;
                criteriaSearch.nationality = Natinality;
                criteriaSearch.market = Natinality;

                main.criteriaSearch = criteriaSearch;
                main.settings = settings;
                main.filter = filter;
                string RequestString = System.Text.Json.JsonSerializer.Serialize(main);
                return RequestString;
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
                return "";

            }

        }

        public bool CheckRoomValidation(string Supplier, HotelSearchParameter SearchPara)
        {
            try
            {
                if (Supplier.ToLower() == "fitruums_xtg" || Supplier.ToLower() == "welcomebeds_xtg")
                {
                    var result = SearchPara.Rooms.GroupBy(ss => new { ss.RoomName, ss.NoofAdults, ss.NoofChild, ss.Child1Age, ss.Child2Age }).Select(n => new { n.Key.RoomName, n.Key.NoofAdults, n.Key.NoofChild, ChildAge = n.ToList(), Count = n.Count() });
                    int NoOfRooms = result.Count();
                    if (NoOfRooms > 1)
                    {
                        if (Supplier.ToLower() == "welcomebeds_xtg")
                        {
                            bool isChild = false;
                            foreach (var item in result)//For each Item
                            {
                                if (item.NoofChild > 0)
                                    isChild = true;
                            }
                            if (isChild)
                                return false;
                        }
                        else
                            return false;
                    }
                }
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
            }
            return true;
        }


        //public GraphQLQuery_Response GetXTGResponse(ref string Error, string query, object variables, string Apikey, int TimeOut, HotelSearchParameter para = null, string Provider = "", bool DebugMode = false)
        //{
        //    GraphQLQuery_Response final = new GraphQLQuery_Response();
        //    System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
        //    timer.Start();


        //    try
        //    {

        //        var fullQuery = new GraphQLClient.GraphQLQuery()
        //        {
        //            query = query,
        //            variables = variables,
        //        };
        //        string jsonContent = System.Text.Json.JsonSerializer.Serialize(fullQuery);

        //        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
        //        request.Method = "POST";
        //        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
        //        UTF8Encoding encoding = new UTF8Encoding();
        //        byte[] byteArray = encoding.GetBytes(jsonContent.Trim());
        //        ServicePointManager.DefaultConnectionLimit = 1000;
        //        request.ContentLength = byteArray.Length;
        //        request.ContentType = @"application/json";
        //        request.Headers.Add("Accept-Encoding", "gzip");
        //        request.Timeout = TimeOut;
        //        request.Headers.Add("Authorization", "Apikey " + Apikey + "");
        //        using (Stream dataStream = request.GetRequestStream())
        //        {
        //            dataStream.Write(byteArray, 0, byteArray.Length);
        //        }
        //        try
        //        {
        //            HttpWebResponse Response = (HttpWebResponse)request.GetResponse();
        //            Stream responseStream = null;
        //            if (Response.Headers.Get("Content-Encoding") == "gzip")
        //                responseStream = new GZipStream(Response.GetResponseStream(), CompressionMode.Decompress);
        //            else
        //                responseStream = Response.GetResponseStream();
        //            //Read the output
        //            StreamReader Reader = new StreamReader(responseStream, Encoding.UTF8);
        //            string strOutput = Reader.ReadToEnd();
        //            responseStream.Close();
        //            Reader.Close();
        //            Response.Close();
        //            final.Response = strOutput;
        //            final.IsError = false;
        //            timer.Stop();
        //            if (para != null)
        //            {
        //                para.SupplierTime += ",SupplierName :" + Provider + ": " + "Execution Time :" + timer.Elapsed.ToString();
        //                if (DebugMode)
        //                    _commonMethod.XMLRequestResponseTrack(Convert.ToString(variables), strOutput, Provider, "Proper");
        //            }

        //        }
        //        catch (WebException ex)
        //        {
        //            if (ex.Response != null)
        //            {
        //                WebResponse errorResponse = ex.Response;
        //                using (Stream responseStream = errorResponse.GetResponseStream())
        //                {
        //                    StreamReader reader = new StreamReader(responseStream, Encoding.GetEncoding("utf-8"));
        //                    string errorText = reader.ReadToEnd();
        //                    //  Console.WriteLine(errorText);
        //                    final.Response = errorText;
        //                    final.IsError = true;
        //                    // return new GraphQLQueryResult(null, ex);
        //                }
        //            }
        //            else
        //            {
        //                final.Response = "timeout";
        //                final.IsError = true;
        //            }
        //            Error = final.Response + "#Message= " + ex.Message;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        final.Response = ex.Message;
        //        final.IsError = true;
        //        _exception.Error(ex);
        //    }

        //    return final;
        //}
        //private string url;

        public List<HotelCodeByCity> FilterWithOutCity_Xtg_Trip(int ratingcount, int hotelcount, List<HotelCodeByCity> dtHotelCommon, List<StarRating> lstRating, string Supplier)
        {
            List<HotelCodeByCity> filter = new List<HotelCodeByCity>();
            try
            {
                //for (int r = 0; r < ratingcount; r++)
                {
                    for (int h = 0; h < hotelcount; h++)
                    {
                        if (dtHotelCommon[h].supplierName.ToLower() == Supplier.ToLower())
                            filter.Add(dtHotelCommon[h]);
                    }
                }
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
            }
            return filter;
        }

        public List<HotelCodeByCity> FilterWithOutCity_Xtg(int ratingcount, int hotelcount, List<HotelCodeByCity> dtHotelCommon, List<StarRating> lstRating, string Supplier)
        {
            List<HotelCodeByCity> filter = new List<HotelCodeByCity>();
            try
            {
                for (int r = 0; r < ratingcount; r++)
                {
                    for (int h = 0; h < hotelcount; h++)
                    {
                        if (dtHotelCommon[h].supplierName.ToLower() == Supplier.ToLower())
                            filter.Add(dtHotelCommon[h]);
                    }
                }
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
            }
            return filter;
        }

        public List<HotelCodeByCity> FilterWithOutCity_Xtg_new(int ratingcount, int hotelcount, List<HotelCodeByCity> dtHotelCommon, List<StarRating> lstRating, string Supplier)
        {
            List<HotelCodeByCity> filter = new List<HotelCodeByCity>();
            try
            {
                //for (int r = 0; r < ratingcount; r++)
                {
                    for (int h = 0; h < hotelcount; h++)
                    {
                        if (dtHotelCommon[h].supplierName.ToLower() == Supplier.ToLower())
                            filter.Add(dtHotelCommon[h]);
                    }
                }
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
            }
            return filter;
        }

        public void GetXtgCurrencyByCountry(string Supplier, string CountryId, ref string currency, string Agt = "")
        {
            try
            {
                currency = "USD";
                #region Welcombeds
                if (Supplier.ToLower() == "welcomebeds_xtg")
                {
                    switch (CountryId.ToLower())
                    {
                        case "united kingdom":
                            currency = "GBP";
                            break;
                        case "switzerland":
                            currency = "EUR";
                            break;
                        case "united states":
                        case "brazil":
                        case "mexico":
                        case "colombia":
                        case "argentina":
                        case "peru":
                        case "venezuela":
                        case "chile":
                        case "ecuador":
                        case "guatemala":
                        case "cuba":
                        case "haiti":
                        case "bolivia":
                        case "dominican republic":
                        case "honduras":
                        case "paraguay":
                        case "nicaragua":
                        case "el salvador":
                        case "costa rica":
                        case "panama":
                        case "puerto rico":
                        case "uruguay":
                        case "guadeloupe":
                        case "martinique":
                        case "saint martin (french part)":
                            currency = "USD";
                            break;
                        default:
                            currency = "EUR";
                            break;
                    }
                }
                #endregion
                #region "WHL"
                if (Supplier.ToLower() == "whl_xtg")
                {
                    switch (CountryId.ToLower())
                    {
                        #region //WHL Office
                        case "italy"://ITALY  15403 
                        case "turkey"://TURKEY   15491
                        case "croatia"://CROATIA
                        case "germany"://GERMANY
                        case "mexico"://MEXICO
                        case "austria"://AUSTRIA
                        case "france"://FRANCE
                        case "hungary"://HUNGARY
                        case "malta"://MALTA
                        case "czech republic"://CZECH REPUBLIC
                        case "greece"://GREECE
                        case "netherlands"://NETHERLANDS
                        case "united arab emirates"://UNITED ARAB EMIRATES
                        case "cuba"://CUBA
                        case "belgium"://BELGIUM
                        case "seychelles"://SEYCHELLES
                        case "cyprus"://CYPRUS
                        case "mauritius"://MAURITIUS
                        case "morocco"://MOROCCO
                        case "maldives"://MALDIVES
                        case "albania"://ALBANIA
                        case "jamaica"://JAMAICA
                        case "slovenia"://SLOVENIA
                            currency = "EUR";
                            break;
                        #endregion
                        #region//China Office
                        case "china"://CHINA
                            currency = "EUR";
                            break;
                        #endregion
                        #region//spain Office 
                        case "spain"://SPAIN
                        case "portugal"://PORTUGAL
                        case "dominican republic"://DOMINICAN REPUBLIC
                        case "cape verde"://CAPO VERDE
                            currency = "EUR";
                            break;
                        #endregion
                        #region//Uk Office 
                        case "united kingdom"://UNITED KINGDOM
                        case "ireland"://IRELAND
                            currency = "GBP";
                            break;
                        #endregion
                        #region//usa office
                        case "aruba"://ARUBA
                        case "united states"://UNITED STATES
                        case "bahamas"://BAHAMAS
                        case "barbados"://BARBADOS
                        case "virgin islands (usa)"://VIRGIN ISLANDS (US)
                        case "puerto rico"://PUERTO RICO
                        case "anguilla"://ANGUILLA
                        case "saint lucia"://ST LUCIA
                            currency = "USD";
                            break;
                        #endregion
                        default:
                            currency = "EUR";
                            break;
                    }
                }
                #endregion
                if (Supplier.ToLower() == "smyrooms_xtg")
                {
                    currency = "USD";

                }
                if (Supplier.ToLower() == "allbedsv1_xtg" || Supplier.ToLower() == "hotelston_xtg" || Supplier.ToLower() == "italcamel_xtg" || Supplier.ToLower() == "oswald_xtg" || Supplier.ToLower() == "rategain_xtg")
                {
                    currency = "EUR";
                }
                if (Supplier.ToLower().IndexOf("xpedia") >= 0)
                {
                    currency = "USD";


                    switch (CountryId.ToUpper())
                    {
                        case "malaysia":
                            currency = "MYR";
                            break;
                        case "saudi arabia":
                            currency = "SAR";
                            break;
                        case "united arab emirates":
                            currency = "AED";
                            break;
                        #region//Uk Office 
                        case "united kingdom"://UNITED KINGDOM
                        case "ireland"://IRELAND
                            currency = "GBP";
                            break;
                        #endregion
                        #region //Europe Office
                        case "albania":
                        case "andorra":
                        case "armenia":
                        case "austria":
                        case "azerbaijan":
                        case "belarus":
                        case "belgium":
                        case "bosnia herzegovina":
                        case "bulgaria":
                        case "croatia":
                        case "cyprus":
                        case "czech republic":
                        case "denmark":
                        case "estonia":
                        case "finland":
                        case "france":
                        case "georgia":
                        case "germany":
                        case "greece":
                        case "hungary":
                        case "iceland":
                        case "italy":
                        case "kazakhstan":
                        case "kosovo":
                        case "latvia":
                        case "liechtenstein":
                        case "lithuania":
                        case "luxembourg":
                        case "macedonia":
                        case "malta":
                        case "moldova":
                        case "monaco":
                        case "montenegro":
                        case "netherlands":
                        case "norway":
                        case "poland":
                        case "portugal":
                        case "romania":
                        case "russia":
                        case "san marino (republic of)":
                        case "serbia":
                        case "slovakia":
                        case "slovenia":
                        case "spain":
                        case "sweden":
                        case "switzerland":
                        //case "turkey":
                        case "ukraine":
                        case "vatican city state (holy see)":
                            currency = "EUR";
                            break;
                        #endregion

                        default:
                            currency = "USD";
                            break;
                    }


                }
                #region "AC Tours"
                if (Supplier.ToLower() == "actours_xtg")
                {
                    currency = "USD";


                    switch (CountryId.ToLower())
                    {
                        #region//Uk Office 
                        case "united kingdom"://UNITED KINGDOM
                        case "ireland"://IRELAND
                            currency = "GBP";
                            break;
                        #endregion
                        #region //Europe Office
                        case "albania":
                        case "andorra":
                        case "armenia":
                        case "austria":
                        case "azerbaijan":
                        case "belarus":
                        case "belgium":
                        case "bosnia herzegovina":
                        case "bulgaria":
                        case "croatia":
                        case "cyprus":
                        case "czech republic":
                        case "denmark":
                        case "estonia":
                        case "finland":
                        case "france":
                        case "georgia":
                        case "germany":
                        case "greece":
                        case "hungary":
                        case "iceland":
                        case "italy":
                        case "kazakhstan":
                        case "kosovo":
                        case "latvia":
                        case "liechtenstein":
                        case "lithuania":
                        case "luxembourg":
                        case "macedonia":
                        case "malta":
                        case "moldova":
                        case "monaco":
                        case "montenegro":
                        case "netherlands":
                        case "norway":
                        case "poland":
                        case "portugal":
                        case "romania":
                        case "russia":
                        case "san marino (republic of)":
                        case "serbia":
                        case "slovakia":
                        case "slovenia":
                        case "spain":
                        case "sweden":
                        case "switzerland":
                        case "turkey":
                        case "ukraine":
                        case "vatican city state (holy see)":
                            currency = "EUR";
                            break;
                        #endregion
                        default:
                            currency = "USD";
                            break;
                    }


                }
                #endregion
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
            }
        }

        public async Task<List<Options>> XMLXTG_RoomOnly_bck_2705(HotelX Item, CommonSearch commonSearch, string RQ, string currencyCode, DataTable dtAgentDetail, DataTable dtMarkupDetails, HotelSearchParameter SearchParam, string ResponseData, string Provider, string xmlcity, string ProccessCutOffTime, int processTimeout, string ResProccessTimerLogActive, string ResProccessTimeValueforSaveLog, double CurrencyFactor = 4)

        {
            System.Diagnostics.Stopwatch timer_room_Xtg = new System.Diagnostics.Stopwatch();
            timer_room_Xtg.Start();

            DataTable DtHotel = _commonMethod.GenerateDataTableNewV2();
            DataTable DtHotelRoom = _commonMethod.GenerateRoomtypeDataTableNewV2();
            DataTable DtHotelPolicy = _commonMethod.GenerateCancellationDataTableNewV2();

            string reqid = Convert.ToString(DateTime.Now.Ticks);
            System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
            timer.Start();

            List<Options> Options = new List<Options>();
            try
            {
                //Xtg_hotellist.Root ds = null;
                //try
                //{
                //    ds = JsonSerializer.Deserialize<Xtg_hotellist.Root>(ResponseData);
                //}

                //catch (Exception ex)
                //{
                //    _exception.Error(ex);
                //}

                if (ResponseData == "")
                {
                    Item.ResponseType = "NoCount";
                    return Options;
                }

                Xtg_Roomlist.Root dsRM = null;
                try
                {
                    //dsRM = JsonSerializer.Deserialize<Xtg_Roomlist.Root>(ResponseData);
                    dsRM = Newtonsoft.Json.JsonConvert.DeserializeObject<Xtg_Roomlist.Root>(ResponseData);
                }

                catch (Exception ex)
                {
                    await _exception.Error(ex);
                }

                if (dsRM.data.hotelX.search.errors != null)
                {
                    Item.ResponseType = "NoCount";
                    return Options;
                }

                if (dsRM != null && dsRM.data.hotelX.search.options != null)
                {

                    // DataSet dt = _commonMethod.GetDataSetFromXMLString(dx);


                    //string context = Convert.ToString(dt.Tables["search"].Rows[0]["context"]);

                    string context = Convert.ToString(dsRM.data.hotelX.search.context);

                    //DataTable dtoptions = dt.Tables["options"].DefaultView.ToTable(true, "hotelCode");
                    //for (int h = 0; h < dt.Tables["options"].Rows.Count; h++)


                    var distinctHotels = dsRM.data.hotelX.search.options.Where(h => h.status.ToLower() == "ok")
                                        .Select(g => g.hotelCode).Distinct().ToList();


                    // for (int h = 0; h < distinctHotels.Count; h++)
                    for (int j = 0; j < distinctHotels.Count; j++)
                    {
                        var listroom = dsRM.data.hotelX.search.options.Where(h => h.hotelCode == distinctHotels[j] && h.status.ToLower() == "ok").ToList();


                        if (listroom.Count > 0)
                        {
                            Item.ResponseType = "Success";

                            if (ProccessCutOffTime == "1" && timer_room_Xtg.ElapsedMilliseconds > processTimeout)
                            {
                                timer_room_Xtg.Stop();
                                break;
                            }

                            #region When Not Need Rooms

                            if (SearchParam.Filters.IsShowRooms == 0)
                            {
                                #region 040425
                                var allPrices = listroom
                                                 .SelectMany(lr => lr.rooms)
                                                 .Select(r => Convert.ToDouble(r.roomPrice.price.net))
                                                 .ToList();

                                double minPrice = allPrices.Min();

                                #endregion

                                #region "Hotel Code Block "
                                #region "No Of Hotels "
                                DataRow drXmlHotel = DtHotel.NewRow();

                                double NetPrice = 1;
                                double Lowest = 0;
                                //drXmlHotel["XMLHotelCode"] = Convert.ToString(ds.data.hotelX.search.options[h].hotelCode);
                                //drXmlHotel["XMLHotelCode"] = Convert.ToString(dtoptions.Rows[h]["hotelCode"]);
                                drXmlHotel["XMLHotelCode"] = Convert.ToString(distinctHotels[j]);
                                drXmlHotel["HotelName"] = "";
                                drXmlHotel["SupplierName"] = Provider;
                                drXmlHotel["OfficeID"] = 0;
                                drXmlHotel["ContractID"] = 0;
                                drXmlHotel["Rating"] = 0;
                                drXmlHotel["Reviews"] = 0;
                                drXmlHotel["UniqueNo"] = 0;
                                drXmlHotel["SupplierCurrency"] = currencyCode;

                                drXmlHotel["IsShowSupplier"] = false;
                                drXmlHotel["BookingStatus"] = "Available";
                                drXmlHotel["XMLCity"] = "DXB";

                                //DataRow[] drroomss = dt.Tables["rooms"].Select("options_Id=" + Convert.ToInt32(dt.Tables["options"].Rows[h]["options_Id"]) + "");
                                if (listroom[0].rooms.Count == 0)
                                    continue;
                                //DataRow[] drprice = dt.Tables["price"].Select("roomPrice_Id=" + Convert.ToInt32(dt.Tables["options"].Rows[h]["options_Id"]) + "");
                                //double Pricenew = Convert.ToDouble(Convert.ToDouble(drprice[0]["net"]) * Convert.ToInt32(SearchParam.Rooms.Count)) * CurrencyFactor;
                                double Pricenew = Convert.ToDouble(Convert.ToDouble(minPrice) * Convert.ToInt32(SearchParam.Rooms.Count)) * CurrencyFactor;
                                if (Pricenew == 0)
                                {
                                    Item.ResponseType = "NoCount";
                                    continue;
                                }
                                //Pricenew = NetPrice; 
                                drXmlHotel["FinalSellingPrice"] = Pricenew;
                                drXmlHotel["SellingPrice"] = Pricenew;


                                DtHotel.Rows.Add(drXmlHotel);
                                if (NetPrice > 0)
                                {
                                    #region "GetRooms for this hotel"
                                    bool willAddOrNot = true;
                                    long RaynaID = 0;
                                    long GiataId = 0;
                                    int Rating = 0;
                                    long OwnsystemHotelId = 0;
                                    double MinPrice = NetPrice;
                                    _markup.GetHotelDataCommon(ref MinPrice, ref willAddOrNot, ref OwnsystemHotelId, ref RaynaID, ref GiataId, ref Rating, ref drXmlHotel, commonSearch.AgentId, commonSearch.TariffID, commonSearch.dsAgentDetails, commonSearch.supplierGiataMaster, SearchParam, commonSearch.dtCountryCityMarkup, commonSearch.B2CMarkup, commonSearch.IsParentId, commonSearch.SubAgentMarkup, commonSearch.SearchByHotel, commonSearch.HotelIDS);
                                    if (willAddOrNot)
                                    {
                                        #region "Room Logic"
                                        DataTable dt_HotelRoom = _commonMethod.GenerateRoomtypeDataTableNewV2();
                                        DataTable dt_Cancellation = _commonMethod.GenerateCancellationDataTableNewV2();
                                        DataTable dt_RateBreackUp = _commonMethod.GenerateoomRateBreackUpDataTableNewV2();
                                        #endregion
                                        decimal LowestPrice = Convert.ToDecimal(MinPrice).CalculatePrice(commonSearch.tokenData, commonSearch.advancedOptions, currencyCode);
                                        Options ops = await _supplierRes.GetCommonRoomForSuppliers(GiataId, commonSearch.tokenData, commonSearch.advancedOptions, Convert.ToDouble(LowestPrice), drXmlHotel, DtHotelRoom, DtHotelPolicy, dt_RateBreackUp, SearchParam);
                                        if (ops != null)
                                            Options.Add(ops);
                                    }
                                    #endregion

                                }


                                #endregion
                                #endregion
                            }
                            #endregion
                            #region When Need Rooms
                            else
                            {
                                //Xtg_Roomlist.Root dsRM = null;
                                //try
                                //{
                                //    dsRM = JsonSerializer.Deserialize<Xtg_Roomlist.Root>(ResponseData);
                                //}

                                //catch (Exception ex)
                                //{
                                //    _exception.Error(ex);
                                //}

                                //var listroom = dsRM.data.hotelX.search.options.Where(h => h.hotelCode == distinctHotels[j] && h.status.ToLower() == "ok").ToList();

                                //if (listroom.Count > 0)
                                {
                                    string GIATACode = "";
                                    #region "Hotel Code Block "
                                    string ResponseString = "";
                                    double AgentPercentage = 0.0;
                                    double AgentMarkupAmount = 0.0;
                                    bool isPerce = true;

                                    DataRow[] XTGChild = commonSearch.dtSuplierMarkup.Select("SupplierName='" + Provider + "' ");

                                    string AccessCode = Convert.ToString(XTGChild[0]["UserName"]);

                                    string SupplierCode = Convert.ToString(XTGChild[0]["UserId"]);
                                    string checkin = SearchParam.CheckInDate.Substring(6, 4) + "-" + SearchParam.CheckInDate.Substring(3, 2) + "-" + SearchParam.CheckInDate.Substring(0, 2);
                                    string checkout = SearchParam.CheckOutDate.Substring(6, 4) + "-" + SearchParam.CheckOutDate.Substring(3, 2) + "-" + SearchParam.CheckOutDate.Substring(0, 2);
                                    int CutOffDays = CacheMaster.CutOffDays;
                                    CutOffDays = _commonMethod.GetCutOffDays(commonSearch.dtSuplierMarkup, Item.Supplier);
                                    //DataRow[] droptions = dt.Tables["options"].Select("search_Id='" + dt.Tables["search"].Rows[h]["search_Id"] + "'");
                                    DataRow drXmlHotel = DtHotel.NewRow();

                                    double NetPrice = 1;
                                    double Lowest = 0;
                                    // drXmlHotel["XMLHotelCode"] = Convert.ToString(dtoptions.Rows[h]["hotelCode"]);
                                    drXmlHotel["XMLHotelCode"] = Convert.ToString(listroom[0].hotelCode);
                                    drXmlHotel["HotelName"] = "";
                                    drXmlHotel["SupplierName"] = Provider;
                                    drXmlHotel["OfficeID"] = 0;
                                    drXmlHotel["ContractID"] = 0;
                                    drXmlHotel["Rating"] = 0;
                                    drXmlHotel["Reviews"] = 0;
                                    drXmlHotel["UniqueNo"] = 0;
                                    drXmlHotel["SupplierCurrency"] = currencyCode;

                                    drXmlHotel["IsShowSupplier"] = false;
                                    drXmlHotel["BookingStatus"] = "Available";
                                    drXmlHotel["XMLCity"] = "DXB";

                                    // DataRow[] drroomss = dt.Tables["rooms"].Select("options_Id=" + Convert.ToInt32(dt.Tables["options"].Rows[h]["options_Id"]) + "");
                                    //if (drroomss.Length == 0)
                                    //  continue;
                                    if (listroom[0].rooms.Count == 0)
                                        continue;
                                    //DataRow[] drprice = dt.Tables["price"].Select("roomPrice_Id=" + Convert.ToInt32(dt.Tables["options"].Rows[h]["options_Id"]) + "");
                                    // double Pricenew = Convert.ToDouble(Convert.ToDouble(drprice[0]["net"]) * Convert.ToInt32(SearchParam.Rooms.Count)) * CurrencyFactor;
                                    double Pricenew = Convert.ToDouble(Convert.ToDouble(listroom[0].price.net) * Convert.ToInt32(SearchParam.Rooms.Count)) * CurrencyFactor;
                                    if (Pricenew == 0)
                                    {
                                        Item.ResponseType = "NoCount";
                                        continue;
                                    }
                                    //Pricenew = NetPrice; 
                                    drXmlHotel["FinalSellingPrice"] = Pricenew;
                                    drXmlHotel["SellingPrice"] = Pricenew;


                                    DtHotel.Rows.Add(drXmlHotel);
                                    if (NetPrice > 0)
                                    {
                                        #region "GetRooms for this hotel"
                                        bool willAddOrNot = true;
                                        long RaynaID = 0;
                                        long GiataId = 0;
                                        int Rating = 0;
                                        int RatingHotel = 0;
                                        long OwnsystemHotelId = 0;
                                        double MinPrice = NetPrice;
                                        _markup.GetHotelDataCommon(ref MinPrice, ref willAddOrNot, ref OwnsystemHotelId, ref RaynaID, ref GiataId, ref Rating, ref drXmlHotel, commonSearch.AgentId, commonSearch.TariffID, commonSearch.dsAgentDetails, commonSearch.supplierGiataMaster, SearchParam, commonSearch.dtCountryCityMarkup, commonSearch.B2CMarkup, commonSearch.IsParentId, commonSearch.SubAgentMarkup, commonSearch.SearchByHotel, commonSearch.HotelIDS);
                                        if (willAddOrNot)
                                        {
                                            #region "Room Logic"
                                            //DataTable dt_HotelRoom = Common.GenerateRoomtypeDataTableNewV2();
                                            //DataTable dt_Cancellation = Common.GenerateCancellationDataTableNewV2();

                                            DataTable dtXTGHotelRoom = _commonMethod.GenerateRoomtypeDataTableNewV2();
                                            DataTable dtXTGHotelRoomRateBreakup = _commonMethod.GenerateoomRateBreackUpDataTableNewV2();
                                            DataTable dtXTGHotelRoomCacelationPolicy = _commonMethod.GenerateCancellationDataTableNewV2();

                                            #endregion
                                            bool blnIsShowSupplier = false;
                                            decimal lowestPrice = Convert.ToDecimal(MinPrice).CalculatePrice(commonSearch.tokenData, commonSearch.advancedOptions, currencyCode);
                                            _markup.getAgetMarkupSetting(Provider, GiataId.ToString(), commonSearch.dtSuplierMarkup, commonSearch.dtMarkupDetails, ref AgentPercentage, ref AgentMarkupAmount, ref blnIsShowSupplier, ref isPerce, commonSearch.AgentId, RatingHotel, SearchParam.CityID, SearchParam.CountryID.ToString(), SearchParam.CheckInDate);

                                            #region "Options"
                                            //DataRow[] droptions = dt.Tables["options"].Select("hotelCode='" + Convert.ToString(dtoptions.Rows[h]["hotelCode"]) + "'");
                                            // if (droptions.Length > 0)

                                            //                            var distinctOptions = dsRM.data.hotelX.search.options
                                            //.GroupBy(option => option.hotelCode) 
                                            //.Select(group => group.First()) 
                                            //.ToList();



                                            //if (distinctHotels.Count > 0)
                                            //{
                                            //  for (int j = 0; j < distinctHotels.Count; j++)
                                            //{

                                            //var listroom = dsRM.data.hotelX.search.options.Where(h => h.hotelCode == distinctHotels[j] && h.status.ToLower() == "ok").ToList();

                                            //if (listroom.Count > 0)
                                            //{
                                            for (int rr = 0; rr < listroom.Count; rr++)
                                            {

                                                //string options_Id = Convert.ToString(droptions[j]["options_Id"]);
                                                //string status = Convert.ToString(dsRM.data.hotelX.search.options[j].status);
                                                string status = Convert.ToString(listroom[rr].status);
                                                if (status.ToLower() != "ok")
                                                {
                                                    continue;
                                                }
                                                string rateRules = "";
                                                //if (dt.Tables["options"].Columns.Contains("rateRules"))
                                                // rateRules = Convert.ToString(droptions[j]["rateRules"]);
                                                if (listroom[rr].rateRules != null)
                                                { // rateRules = Convert.ToString(dsRM.data.hotelX.search.options[j].rateRules);
                                                    rateRules = Convert.ToString(listroom[rr].rateRules[0]);
                                                    //Convert.ToString(listroom[rr].rateRules);
                                                }

                                                if (Provider.ToLower() == "trip.com_xtg")
                                                {
                                                    rateRules = "";
                                                }
                                                string remarks = "<table>";
                                                /////string remarksRoom = Convert.ToString(droptions[j]["remarks"]);
                                                //  if (remarksRoom != "")
                                                //  remarks += "<tr><td>SupplierNote:</td><td>" + remarksRoom + "</td></tr>";
                                                //string remarks = Convert.ToString(droptions[j]["remarks"]);
                                                //string token = Convert.ToString(droptions[j]["token"]);
                                                // string token = Convert.ToString(dsRM.data.hotelX.search.options[j].token);
                                                string token = Convert.ToString(listroom[rr].token);
                                                //string id = Convert.ToString(droptions[j]["id"]) + "$$" + Guid.NewGuid().ToString().Substring(0, 4);
                                                //string accessCode = Convert.ToString(droptions[j]["accessCode"]);
                                                // string accessCode = Convert.ToString(dsRM.data.hotelX.search.options[j].accessCode);
                                                string accessCode = Convert.ToString(listroom[rr].accessCode);
                                                //string supplierCode = Convert.ToString(droptions[j]["supplierCode"]);
                                                // string supplierCode = Convert.ToString(dsRM.data.hotelX.search.options[j].supplierCode);
                                                string supplierCode = Convert.ToString(listroom[rr].supplierCode);
                                                //string hotelCode = Convert.ToString(droptions[j]["hotelCode"]);
                                                // string hotelCode = Convert.ToString(dsRM.data.hotelX.search.options[j].hotelCode);
                                                string hotelCode = Convert.ToString(listroom[rr].hotelCode);
                                                // string context = Convert.ToString(droptions[j]["context"]);
                                                string id = Convert.ToString(listroom[rr].id) + "$$" + supplierCode + "$$" + accessCode + "$$" + hotelCode + "$$" + currencyCode + "$$" + Guid.NewGuid().ToString().Substring(0, 4);
                                                id = id.Replace("'", "");
                                                // string boardCode = Convert.ToString(droptions[j]["boardCode"]);
                                                //string boardCode = Convert.ToString(dsRM.data.hotelX.search.options[j].boardCode);
                                                string boardCode = Convert.ToString(listroom[rr].boardCode);


                                                #region "Option Variable"
                                                double OptionPrice = 0;
                                                string RoomTypeName = "";
                                                string suppliment = "";
                                                int AdultsOccupancy = 0;
                                                int ChildOccupancy = 0;
                                                string mealName = "Room Only";

                                                #region"MealNAme"
                                                GetMealRoomData_Xtg("Meal", boardCode, ref mealName, Provider, supplierCode, AccessCode);
                                                #endregion
                                                #endregion
                                                #region"Option Price"



                                                //DataRow[] drPriceOption = dt.Tables["price"].Select("options_Id='" + options_Id + "'");
                                                string currency = "";
                                                //if (drPriceOption.Length > 0)
                                                if (listroom[rr].price != null)
                                                {
                                                    // bool binding = Convert.ToBoolean(drPriceOption[0]["binding"]);
                                                    //bool binding = dsRM.data.hotelX.search.options[j].price.binding;
                                                    bool binding = Convert.ToBoolean(listroom[rr].price.binding);
                                                    // OptionPrice = Convert.ToDouble(drPriceOption[0]["net"]) * CurrencyFactor;
                                                    //OptionPrice =(dsRM.data.hotelX.search.options[j].price.net) * CurrencyFactor;
                                                    OptionPrice = Convert.ToDouble(listroom[rr].price.net) * CurrencyFactor;
                                                    // currency = Convert.ToString(drPriceOption[0]["currency"]);
                                                    //currency = dsRM.data.hotelX.search.options[j].price.currency;
                                                    currency = Convert.ToString(listroom[rr].price.currency);

                                                }
                                                if (currencyCode.ToLower() != currency.ToLower())
                                                {
                                                    continue;
                                                }
                                                #endregion

                                                // DataRow[] drrooms = dt.Tables["rooms"].Select("options_Id='" + options_Id + "'");
                                                #region "Rooms"
                                                //if (drrooms.Length > 0)
                                                // if (dsRM.data.hotelX.search.options[j].rooms != null)
                                                if (listroom[rr].rooms != null)
                                                {
                                                    #region"BedDesc"
                                                    //DataRow[] droccupancies = dt.Tables["occupancies"].Select("options_Id='" + options_Id + "' ");
                                                    //if (droccupancies.Length > 0)
                                                    if (listroom[rr].occupancies != null)
                                                    {
                                                        for (int ocu = 0; ocu < listroom[rr].occupancies.Count; ocu++)
                                                        {
                                                            // string occupancies_Id = Convert.ToString(droccupancies[ocu]["occupancies_Id"]);
                                                            string occupancies_Id = Convert.ToString(listroom[rr].occupancies[ocu].id);
                                                            // DataRow[] drpaxes = dt.Tables["paxes"].Select("occupancies_Id='" + occupancies_Id + "' ");
                                                            if (listroom[rr].occupancies[ocu].paxes != null)
                                                            {
                                                                for (int px = 0; px < listroom[rr].occupancies[ocu].paxes.Count; px++)
                                                                {
                                                                    if (listroom[rr].occupancies[ocu].paxes[px].age < 35)
                                                                    {
                                                                        ChildOccupancy += 1;
                                                                    }
                                                                    else
                                                                    {
                                                                        AdultsOccupancy += 1;
                                                                    }
                                                                }
                                                            }

                                                        }
                                                    }
                                                    #endregion
                                                    DataRow drXTGHotelRoom = dtXTGHotelRoom.NewRow();

                                                    drXTGHotelRoom["OwnsystemHotelId"] = OwnsystemHotelId;
                                                    drXTGHotelRoom["ContractID"] = "0";
                                                    drXTGHotelRoom["UniqueNo"] = OwnsystemHotelId;
                                                    drXTGHotelRoom["MealName"] = mealName;
                                                    drXTGHotelRoom["MealId"] = boardCode;
                                                    drXTGHotelRoom["SHRUI"] = token;
                                                    drXTGHotelRoom["Board"] = boardCode;
                                                    drXTGHotelRoom["Type"] = "";
                                                    drXTGHotelRoom["RoomNo"] = "1";
                                                    drXTGHotelRoom["BookingStatus"] = rateRules == "" ? "Available" : rateRules;
                                                    drXTGHotelRoom["BoardType"] = "";
                                                    drXTGHotelRoom["Characteristic"] = "";
                                                    //drXTGHotelRoom["RoomTypeId"] = id;
                                                    drXTGHotelRoom["RoomTypeCode"] = 0;
                                                    drXTGHotelRoom["CompanyBuyingPrice"] = OptionPrice;
                                                    drXTGHotelRoom["AdultsOccupancy"] = AdultsOccupancy;
                                                    drXTGHotelRoom["ChildOccupancy"] = ChildOccupancy;
                                                    drXTGHotelRoom["MaxExtrabed"] = 0;
                                                    drXTGHotelRoom["OfferDescription"] = "";
                                                    drXTGHotelRoom["BoardTypeID"] = "";
                                                    #region Vat
                                                    _markup.ProcessHotelRoom_WithMarkup(CurrencyFactor, currencyCode, true, true, SearchParam.Rooms.Count * SearchParam.NoofNights, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), SearchParam.CheckInDate, SearchParam.CheckOutDate, ref OptionPrice, ref drXTGHotelRoom, isPerce, AgentPercentage, AgentMarkupAmount, commonSearch.SubAgentMarkup, commonSearch.B2CMarkup, commonSearch.IsParentId);
                                                    #endregion
                                                    List<Room_NameMerge> roomlist = new List<Room_NameMerge>();
                                                    #region"Suppliment"
                                                    //if (dt.Tables.Contains("supplements"))
                                                    if (listroom[rr].supplements != null)
                                                    {
                                                        //DataRow[] drsupplements = dt.Tables["supplements"].Select("options_Id='" + options_Id + "' and code<>''");
                                                        //if (drsupplements.Length > 0)
                                                        {
                                                            //string supplements_Code = Convert.ToString(drsupplements[0]["code"]);
                                                            //string supplements_supplementType = Convert.ToString(drsupplements[0]["supplementType"]);
                                                            //string supplements_chargeType = Convert.ToString(drsupplements[0]["chargeType"]);
                                                            //string supplements_mandatory = Convert.ToString(drsupplements[0]["mandatory"]);
                                                            //string supplements_quantity = Convert.ToString(drsupplements[0]["quantity"]);
                                                            //string supplements_unit = Convert.ToString(drsupplements[0]["unit"]);
                                                            //string supplements_effectiveDate = Convert.ToString(drsupplements[0]["effectiveDate"]);
                                                            //string supplements_expireDate = Convert.ToString(drsupplements[0]["expireDate"]);
                                                            //string supplements_Id = Convert.ToString(drsupplements[0]["supplements_Id"]);
                                                            //need
                                                            string supplements_Code = Convert.ToString(listroom[rr].supplements.code);
                                                            string supplements_supplementType = Convert.ToString(listroom[rr].supplements.supplementType);
                                                            string supplements_chargeType = Convert.ToString(listroom[rr].supplements.chargeType);
                                                            string supplements_mandatory = Convert.ToString(listroom[rr].supplements.mandatory);
                                                            string supplements_quantity = Convert.ToString(listroom[rr].supplements.quantity);
                                                            string supplements_unit = Convert.ToString(listroom[rr].supplements.unit);
                                                            string supplements_effectiveDate = Convert.ToString(listroom[rr].supplements.effectiveDate);
                                                            string supplements_expireDate = Convert.ToString(listroom[rr].supplements.expireDate);
                                                            //string supplements_Id = Convert.ToString(drsupplements[0]["supplements_Id"]);

                                                            //DataRow[] drresort = dt.Tables["resort"].Select("supplements_Id='" + supplements_Id + "'");
                                                            string resort_description = "";
                                                            // if (drresort.Length > 0)
                                                            if (listroom[rr].supplements.resort != null)
                                                            {
                                                                // resort_description = Convert.ToString(drresort[0]["description"]);
                                                                resort_description = Convert.ToString(listroom[rr].supplements.resort.description);
                                                            }
                                                            suppliment += "Code :" + supplements_Code + "<br/>" +
                                                                "supplement :" + supplements_supplementType + " " + resort_description + "<br/>" +
                                                                (supplements_chargeType.ToUpper() == "INCLUDE" ? "Include in total" : "not include in total") +
                                                                "<br/>mandatory :" + (supplements_mandatory.ToLower() == "true" ? "Yes" : "NO") + "<br/>" +
                                                                "For :" + supplements_quantity + " " + supplements_unit + "<br/>" +
                                                                "effectiveDate :" + (supplements_effectiveDate == "" ? "-" : supplements_effectiveDate) + "<br/>" +
                                                                "expireDate :" + (supplements_expireDate == "" ? "-" : supplements_expireDate) + "<br/>";
                                                            if (suppliment != "")
                                                                remarks += "<tr><td>supplement:</td><td>" + suppliment + "</td></tr>";
                                                        }
                                                    }
                                                    #endregion
                                                    //for (int room = 0; room < drrooms.Length; room++)
                                                    for (int room = 0; room < listroom[rr].rooms.Count; room++)
                                                    {
                                                        //string rooms_Id = Convert.ToString(drrooms[room]["rooms_Id"]);
                                                        // string code = Convert.ToString(drrooms[room]["code"]);
                                                        string code = Convert.ToString(listroom[rr].rooms[room].code);
                                                        string description = Convert.ToString(listroom[rr].rooms[room].description);
                                                        if (description == "" || SupplierCode.ToLower() == "fit" || SupplierCode.ToLower() == "mtbk" || SupplierCode.ToLower() == "whl")
                                                        {
                                                            #region"RoomName"
                                                            GetMealRoomData_Xtg("Room", code, ref description, Provider, supplierCode, AccessCode);
                                                            #endregion
                                                        }
                                                        Room_NameMerge obj = new Room_NameMerge();
                                                        obj.RoomCount = 1;
                                                        obj.RoomName = description;
                                                        obj.status = true;
                                                        roomlist.Add(obj);
                                                        bool IsRoomRefundable = false;

                                                        // string refundable = Convert.ToString(drrooms[room]["refundable"]);
                                                        string refundable = Convert.ToString(listroom[rr].rooms[room].refundable);
                                                        if (refundable.ToLower() == "true")
                                                            IsRoomRefundable = true;

                                                        #region "Room Variable initialise"
                                                        double RoomPrice = 0;
                                                        string BedDescription = "";
                                                        string RatePlan = "";
                                                        string OfferDecription = "";
                                                        string OfferCode = "";

                                                        #endregion

                                                        // DataRow[] drroomPrice = dt.Tables["roomPrice"].Select("rooms_Id='" + rooms_Id + "'");
                                                        // if (drroomPrice.Length > 0)
                                                        if (listroom[rr].rooms[room] != null)
                                                        {
                                                            //string roomPrice_Id = Convert.ToString(drroomPrice[0]["roomPrice_Id"]);
                                                            // DataRow[] drPrice = dt.Tables["price"].Select("roomPrice_Id='" + roomPrice_Id + "'");
                                                            // if (drPrice.Length > 0)
                                                            if (listroom[rr].price != null)
                                                            {
                                                                //RoomPrice = Convert.ToDouble(drPrice[0]["net"]) * CurrencyFactor;
                                                                RoomPrice = Convert.ToDouble(listroom[rr].price.net) * CurrencyFactor;
                                                            }
                                                        }
                                                        if (RoomPrice > 0)
                                                        {
                                                            #region"BedDesc"
                                                            if (listroom[rr].rooms[room].beds != null)
                                                            {
                                                                //DataRow[] drbeds = dt.Tables["beds"].Select("rooms_Id='" + rooms_Id + "'");
                                                                // if (drbeds.Length > 0)
                                                                // if (drbeds.Length > 0)
                                                                {
                                                                    //string Bed_Type = Convert.ToString(drbeds[0]["type"]);
                                                                    //string Bed_counte = Convert.ToString(drbeds[0]["count"]);
                                                                    //string Bed_shared = Convert.ToString(drbeds[0]["shared"]);
                                                                    string Bed_Type = Convert.ToString(listroom[rr].rooms[room].beds[0].type);
                                                                    string Bed_counte = Convert.ToString(listroom[rr].rooms[room].beds[0].count);
                                                                    string Bed_shared = listroom[rr].rooms[room].beds[0].shared != null ? Convert.ToString(listroom[rr].rooms[room].beds[0].shared) : "";
                                                                    if (Bed_Type != "")
                                                                        BedDescription = "<b>Bed type:</b> " + Bed_Type + "<b> count: </b>" + Bed_counte + (Bed_shared.ToLower() == "true" ? " (Shared)" : "");
                                                                    else
                                                                        BedDescription = "<b>" + Bed_counte + (Bed_shared.ToLower() == "true" ? " (Shared)" : "</b> Bed Requested");

                                                                    if (BedDescription != "")
                                                                        remarks += "<tr><td>Bed Description:</td><td>" + BedDescription + "</td></tr>";
                                                                }
                                                            }
                                                            #endregion
                                                            #region"RatePlans"
                                                            //if (dt.Tables.Contains("ratePlans"))
                                                            if (listroom[rr].rooms[room].ratePlans != null)
                                                            {
                                                                // DataRow[] drratePlans = dt.Tables["ratePlans"].Select("rooms_Id='" + rooms_Id + "'");
                                                                // if (drratePlans.Length > 0)
                                                                {
                                                                    //  string ratePlans_Code = Convert.ToString(drratePlans[0]["code"]);
                                                                    string ratePlans_Code = Convert.ToString(listroom[rr].rooms[room].ratePlans[0].code);
                                                                    //string ratePlans_effectiveDate = Convert.ToString(drratePlans[0]["effectiveDate"]);
                                                                    string ratePlans_effectiveDate = Convert.ToString(listroom[rr].rooms[room].ratePlans[0].effectiveDate);
                                                                    //string ratePlans_expireDate = Convert.ToString(drratePlans[0]["expireDate"]);
                                                                    string ratePlans_expireDate = Convert.ToString(listroom[rr].rooms[room].ratePlans[0].expireDate);
                                                                    RatePlan = "<b>Rate Plan:</b>" + ratePlans_Code + " " + "<b> effectiveDate:</b> " + (ratePlans_effectiveDate == "" ? "-" : ratePlans_effectiveDate) + "<b> expireDate:</b> " + (ratePlans_expireDate == "" ? "-" : ratePlans_expireDate);
                                                                    if (RatePlan != "")
                                                                        remarks += "<tr><td>Rate PlanDetails:</td><td>" + RatePlan + "</td></tr>";
                                                                }
                                                            }
                                                            #endregion
                                                            #region"Promotions"
                                                            // if (dt.Tables.Contains("promotions"))
                                                            {
                                                                // DataRow[] drpromotions = dt.Tables["promotions"].Select("rooms_Id='" + rooms_Id + "' and code<>''");
                                                                // if (drpromotions.Length > 0)
                                                                if (listroom[rr].rooms[room].promotions != null && listroom[rr].rooms[room].promotions.Count > 0)
                                                                {
                                                                    // string promotions_Code = Convert.ToString(drpromotions[0]["code"]);
                                                                    string promotions_Code = Convert.ToString(listroom[rr].rooms[room].promotions[0].code);
                                                                    // string promotions_name = Convert.ToString(drpromotions[0]["name"]);
                                                                    string promotions_name = Convert.ToString(listroom[rr].rooms[room].promotions[0].name);
                                                                    //string promotions_effectiveDate = Convert.ToString(drpromotions[0]["effectiveDate"]);
                                                                    string promotions_effectiveDate = Convert.ToString(listroom[rr].rooms[room].promotions[0].effectiveDate);
                                                                    //string promotions_expireDate = Convert.ToString(drpromotions[0]["expireDate"]);
                                                                    string promotions_expireDate = Convert.ToString(listroom[rr].rooms[room].promotions[0].expireDate);
                                                                    OfferCode = promotions_Code;
                                                                    if (promotions_Code != "")
                                                                        OfferDecription = "Code: " + promotions_Code;
                                                                    if ("" != promotions_name)
                                                                        OfferDecription += "<b> Name:</b> " + promotions_name;
                                                                    if (promotions_effectiveDate != "")
                                                                        OfferDecription += "<b> effectiveDate: </b>" + promotions_effectiveDate;
                                                                    if (promotions_expireDate != "")
                                                                        OfferDecription += "<b> expireDate :</b>" + promotions_expireDate;

                                                                    drXTGHotelRoom["OfferDescription"] = OfferDecription;
                                                                    drXTGHotelRoom["OfferCode"] = promotions_Code == "" ? promotions_name : promotions_Code;
                                                                    if (OfferDecription != "")
                                                                        remarks += "<tr><td>Offer Description:</td><td>" + OfferDecription + "</td></tr>";

                                                                }
                                                            }
                                                            #endregion

                                                            #region"Ratebreakup"
                                                            //DateTime dtCheckInDateForRate = DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null);
                                                            //#region Vat
                                                            //double VatOnRateBrackUp = 0;
                                                            //double RoomPrice1 = _markup.ProcessHotelRateBreakupPrice_WithMarkup(1, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), RoomPrice / SearchParam.NoofNights, commonSearch.B2CMarkup, commonSearch.IsParentId, isPerce, AgentPercentage, AgentMarkupAmount, commonSearch.SubAgentMarkup, ref VatOnRateBrackUp);
                                                            //#endregion
                                                            //for (int night = 0; night < SearchParam.NoofNights; night++)
                                                            //{
                                                            //    string rateWeekColumn = Convert.ToString(dtCheckInDateForRate.Date.AddDays(night).DayOfWeek).Substring(0, 3);
                                                            //    DataRow drXTGRoomRateBreackUp = dtXTGHotelRoomRateBreakup.NewRow();
                                                            //    drXTGRoomRateBreackUp["RoomNo"] = Convert.ToString("1");
                                                            //    drXTGRoomRateBreackUp["SupplierName"] = Provider;
                                                            //    drXTGRoomRateBreackUp["OwnsystemHotelId"] = OwnsystemHotelId;
                                                            //    drXTGRoomRateBreackUp["ContractID"] = "0";
                                                            //    drXTGRoomRateBreackUp["RoomTypeName"] = description;
                                                            //    drXTGRoomRateBreackUp["RoomTypeId"] = id;
                                                            //    drXTGRoomRateBreackUp["MealId"] = boardCode;
                                                            //    drXTGRoomRateBreackUp["Meal"] = mealName;
                                                            //    drXTGRoomRateBreackUp["BookingStatus"] = rateRules == "" ? "Available" : rateRules;
                                                            //    drXTGRoomRateBreackUp["DateOfWeek"] = rateWeekColumn;
                                                            //    drXTGRoomRateBreackUp["Date"] = dtCheckInDateForRate.Date.AddDays(night).Date;
                                                            //    drXTGRoomRateBreackUp["Rate"] = RoomPrice1;
                                                            //    drXTGRoomRateBreackUp["RateType"] = SearchParam.Rooms.Count == 1 ? "A Rate" : room + 1 + ". " + description;
                                                            //    // drXTGRoomRateBreackUp["GIITACode"] = GiataId;

                                                            //    string HotelFessDescription = "";
                                                            //    if (RatePlan != string.Empty)
                                                            //        HotelFessDescription = RatePlan;
                                                            //    if (BedDescription != string.Empty)
                                                            //        HotelFessDescription += "</br>" + BedDescription;
                                                            //    drXTGRoomRateBreackUp["HotelFessDescription"] = HotelFessDescription;

                                                            //    drXTGRoomRateBreackUp["SupplierCurrency"] = currencyCode;
                                                            //    drXTGRoomRateBreackUp["BuyingPrice"] = RoomPrice1 / CurrencyFactor;
                                                            //    dtXTGHotelRoomRateBreakup.Rows.Add(drXTGRoomRateBreackUp);
                                                            //    dtXTGHotelRoomRateBreakup.AcceptChanges();
                                                            //}
                                                            #endregion
                                                            //if (dt.Tables.Contains("features"))
                                                            if (listroom[rr].rooms[room].features != null)
                                                            {
                                                                //DataRow[] drbeds = dt.Tables["features"].Select("rooms_Id='" + rooms_Id + "'");
                                                                //if (drbeds.Length > 0)
                                                                {
                                                                    //string Bed_Type = Convert.ToString(drbeds[0]["code"]);
                                                                    string Bed_Type = Convert.ToString(listroom[rr].rooms[room].features.code);
                                                                    if (Bed_Type != "")
                                                                        remarks += "<tr><td>Room Note:</td><td>" + Bed_Type + "</td></tr>";
                                                                }
                                                            }

                                                        }
                                                    }
                                                    #region "Logic For Room Name "
                                                    for (int rm = 0; rm < roomlist.Count; rm++)
                                                    {
                                                        for (int r2 = rm + 1; r2 < roomlist.Count; r2++)
                                                        {
                                                            if (roomlist[rm].RoomName == roomlist[r2].RoomName && roomlist[r2].status)
                                                            {
                                                                roomlist[rm].RoomCount = roomlist[rm].RoomCount + roomlist[r2].RoomCount;
                                                                roomlist[r2].RoomCount = roomlist[rm].RoomCount;
                                                                roomlist[r2].status = false;
                                                            }
                                                        }
                                                        if (RoomTypeName.IndexOf(roomlist[rm].RoomName) < 0 || RoomTypeName == "")
                                                            RoomTypeName = RoomTypeName + roomlist[rm].RoomCount + " X " + roomlist[rm].RoomName + " ";
                                                    }

                                                    #endregion


                                                    #region CancelltionPolicy - OLD
                                                    //                                                    DateTime curDate = _commonMethod.GetTimeZoneDate(SearchParam.CurrentDate);
                                                    //                                                    #region Vat
                                                    //                                                    double VatOnPolicy = 0;
                                                    //                                                    double ChargeAmount = 0;
                                                    //                                                    DateTime DeadLineDate = DateTime.Now;

                                                    //                                                    //if (dt.Tables.Contains("cancelPolicy"))
                                                    //                                                    if (listroom[rr].cancelPolicy != null)
                                                    //                                                    {
                                                    //                                                        bool isRefundable = Convert.ToBoolean(listroom[rr].cancelPolicy.refundable);
                                                    //                                                        if (isRefundable)
                                                    //                                                        {


                                                    //                                                            //DataRow[] drRatePlanCancellationPolicyList = dt.Tables["cancelPolicy"].Select("options_Id='" + options_Id + "'");
                                                    //                                                            // if (drRatePlanCancellationPolicyList.Length > 0)
                                                    //                                                            if (listroom[rr].cancelPolicy.cancelPenalties != null)
                                                    //                                                            {
                                                    //                                                                for (int c = 0; c < listroom[rr].cancelPolicy.cancelPenalties.Count; c++)
                                                    //                                                                {
                                                    //                                                                    //if (dt.Tables.Contains("cancelPenalties"))
                                                    //                                                                    {
                                                    //                                                                        // DataRow[] cancelPenalties = dt.Tables["cancelPenalties"].Select("cancelPolicy_Id='" + drRatePlanCancellationPolicyList[c][1] + "'");

                                                    //                                                                        //if (cancelPenalties.Length > 0)
                                                    //                                                                        {
                                                    //                                                                            // bool isRefundable = Convert.ToBoolean(drRatePlanCancellationPolicyList[c][0]);
                                                    //                                                                            {
                                                    //                                                                                //if (cancelPenalties[c][0] != DBNull.Value)
                                                    //                                                                                if (listroom[rr].cancelPolicy.cancelPenalties[c].value != null)
                                                    //                                                                                {
                                                    //                                                                                    DeadLineDate = Convert.ToDateTime(checkin).AddHours(-Convert.ToDouble(listroom[rr].cancelPolicy.cancelPenalties[c].hoursBefore));
                                                    //                                                                                }
                                                    //                                                                                else
                                                    //                                                                                {
                                                    //                                                                                    RoomTypeName += " [Non Refundable]";
                                                    //                                                                                }
                                                    //                                                                            }


                                                    //                                                                            #endregion
                                                    //                                                                            if (DeadLineDate.AddDays(CutOffDays).Date > DateTime.Now.Date)
                                                    //                                                                                DeadLineDate = DeadLineDate.AddDays(CutOffDays).Date;
                                                    //                                                                            else
                                                    //                                                                                DeadLineDate = DateTime.Now.Date;
                                                    //                                                                            if (curDate.Date < DeadLineDate.Date)
                                                    //                                                                            {
                                                    //                                                                                if (c == 0)
                                                    //                                                                                {
                                                    //                                                                                    double Zero = 0;
                                                    //                                                                                    dtXTGHotelRoomCacelationPolicy.Rows.Add(_commonMethod.setCancellationPolicyRow(ref Zero, dtXTGHotelRoomCacelationPolicy.NewRow(), CurrencyFactor, currency, Item.Supplier, Convert.ToString(OwnsystemHotelId), Convert.ToString(1), Convert.ToString(id)
                                                    //    , RoomTypeName, Convert.ToString(boardCode), curDate, DeadLineDate.AddDays(-1).AddHours(23).AddMinutes(59).AddSeconds(59), 0, 0, "", 0, "", "NO", Convert.ToString(DeadLineDate.Date)));
                                                    //                                                                                    dtXTGHotelRoomCacelationPolicy.AcceptChanges();
                                                    //                                                                                }
                                                    //                                                                                dtXTGHotelRoomCacelationPolicy.Rows.Add(_commonMethod.setCancellationPolicyRow(ref OptionPrice, dtXTGHotelRoomCacelationPolicy.NewRow(), CurrencyFactor, currency, Item.Supplier, Convert.ToString(OwnsystemHotelId), Convert.ToString(1), Convert.ToString(id)
                                                    //    , RoomTypeName, Convert.ToString(boardCode), DeadLineDate, DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null), OptionPrice, OptionPrice, "", 0, "", "Yes", Convert.ToString(DeadLineDate.Date)));
                                                    //                                                                                dtXTGHotelRoomCacelationPolicy.AcceptChanges();
                                                    //                                                                            }
                                                    //                                                                            else
                                                    //                                                                            {
                                                    //                                                                                dtXTGHotelRoomCacelationPolicy.Rows.Add(_commonMethod.setCancellationPolicyRow(ref OptionPrice, dtXTGHotelRoomCacelationPolicy.NewRow(), CurrencyFactor, currency, Item.Supplier, Convert.ToString(OwnsystemHotelId), Convert.ToString(1), Convert.ToString(id)
                                                    //    , RoomTypeName, Convert.ToString(boardCode), curDate, DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null), OptionPrice, OptionPrice, "", 0, "", "Yes", Convert.ToString(DeadLineDate.Date)));
                                                    //                                                                                dtXTGHotelRoomCacelationPolicy.AcceptChanges();
                                                    //                                                                            }
                                                    //                                                                        }
                                                    //                                                                    }
                                                    //                                                                }
                                                    //                                                            }
                                                    //                                                        }
                                                    //                                                        else
                                                    //                                                        {
                                                    //                                                            RoomTypeName += " [Non Refundable]";

                                                    //                                                            dtXTGHotelRoomCacelationPolicy.Rows.Add(_commonMethod.setCancellationPolicyRow(ref OptionPrice, dtXTGHotelRoomCacelationPolicy.NewRow(), CurrencyFactor, currency, Item.Supplier, Convert.ToString(OwnsystemHotelId), Convert.ToString(1), Convert.ToString(id)
                                                    //    , RoomTypeName, Convert.ToString(boardCode), curDate, DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null), OptionPrice, OptionPrice, "", 0, "", "Yes", Convert.ToString(DeadLineDate.Date)));
                                                    //                                                            dtXTGHotelRoomCacelationPolicy.AcceptChanges();
                                                    //                                                        }

                                                    //                                                    }
                                                    //                                                    else
                                                    //                                                    {
                                                    //                                                        RoomTypeName += " [Non Refundable]";

                                                    //                                                        dtXTGHotelRoomCacelationPolicy.Rows.Add(_commonMethod.setCancellationPolicyRow(ref OptionPrice, dtXTGHotelRoomCacelationPolicy.NewRow(), CurrencyFactor, currency, Item.Supplier, Convert.ToString(OwnsystemHotelId), Convert.ToString(1), Convert.ToString(id)
                                                    //, RoomTypeName, Convert.ToString(boardCode), curDate, DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null), OptionPrice, OptionPrice, "", 0, "", "Yes", Convert.ToString(DeadLineDate.Date)));
                                                    //                                                        dtXTGHotelRoomCacelationPolicy.AcceptChanges();
                                                    //                                                    }

                                                    #endregion

                                                    #region CancelltionPolicy
                                                    DateTime curDate = _commonMethod.GetTimeZoneDate(SearchParam.CurrentDate);
                                                    #region Vat
                                                    double VatOnPolicy = 0;
                                                    double ChargeAmount = 0;
                                                    DateTime DeadLineDate = DateTime.Now;
                                                    Double hoursBefore = 0;
                                                    string RoomTyypeId = "";

                                                    //if (dt.Tables.Contains("cancelPolicy"))
                                                    if (listroom[rr].cancelPolicy != null)
                                                    {
                                                        bool isRefundable = Convert.ToBoolean(listroom[rr].cancelPolicy.refundable);
                                                        if (isRefundable)
                                                        {


                                                            //DataRow[] drRatePlanCancellationPolicyList = dt.Tables["cancelPolicy"].Select("options_Id='" + options_Id + "'");
                                                            // if (drRatePlanCancellationPolicyList.Length > 0)
                                                            if (listroom[rr].cancelPolicy.cancelPenalties != null)
                                                            {
                                                                //for (int c = 0; c < listroom[rr].cancelPolicy.cancelPenalties.Count; c++)
                                                                {
                                                                    int c = 0;
                                                                    //if (dt.Tables.Contains("cancelPenalties"))
                                                                    {
                                                                        // DataRow[] cancelPenalties = dt.Tables["cancelPenalties"].Select("cancelPolicy_Id='" + drRatePlanCancellationPolicyList[c][1] + "'");

                                                                        //if (cancelPenalties.Length > 0)
                                                                        {
                                                                            // bool isRefundable = Convert.ToBoolean(drRatePlanCancellationPolicyList[c][0]);
                                                                            {
                                                                                //if (cancelPenalties[c][0] != DBNull.Value)
                                                                                if (listroom[rr].cancelPolicy.cancelPenalties[c].value != null)
                                                                                {
                                                                                    hoursBefore = Convert.ToDouble(listroom[rr].cancelPolicy.cancelPenalties[c].hoursBefore);
                                                                                    DeadLineDate = Convert.ToDateTime(checkin).AddHours(-hoursBefore);
                                                                                    //DeadLineDate = Convert.ToDateTime(checkin).AddHours(-Convert.ToDouble(listroom[rr].cancelPolicy.cancelPenalties[c].hoursBefore));
                                                                                }
                                                                                else
                                                                                {
                                                                                    RoomTypeName += " [Non Refundable]";
                                                                                }
                                                                            }
                                                                            RoomTyypeId = id + "$$" + hoursBefore;

                                                                            #endregion
                                                                            if (DeadLineDate.AddDays(CutOffDays).Date > DateTime.Now.Date)
                                                                                DeadLineDate = DeadLineDate.AddDays(CutOffDays).Date;
                                                                            else
                                                                                DeadLineDate = DateTime.Now.Date;
                                                                            if (curDate.Date < DeadLineDate.Date)
                                                                            {
                                                                                if (c == 0)
                                                                                {
                                                                                    double Zero = 0;
                                                                                    dtXTGHotelRoomCacelationPolicy.Rows.Add(_commonMethod.setCancellationPolicyRow(ref Zero, dtXTGHotelRoomCacelationPolicy.NewRow(), CurrencyFactor, currency, Item.Supplier, Convert.ToString(OwnsystemHotelId), Convert.ToString(1), Convert.ToString(RoomTyypeId)
    , RoomTypeName, Convert.ToString(boardCode), curDate, DeadLineDate.AddDays(-1).AddHours(23).AddMinutes(59).AddSeconds(59), 0, 0, "", 0, "", "NO", Convert.ToString(DeadLineDate.Date)));
                                                                                    dtXTGHotelRoomCacelationPolicy.AcceptChanges();
                                                                                }
                                                                                dtXTGHotelRoomCacelationPolicy.Rows.Add(_commonMethod.setCancellationPolicyRow(ref OptionPrice, dtXTGHotelRoomCacelationPolicy.NewRow(), CurrencyFactor, currency, Item.Supplier, Convert.ToString(OwnsystemHotelId), Convert.ToString(1), Convert.ToString(RoomTyypeId)
    , RoomTypeName, Convert.ToString(boardCode), DeadLineDate, DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null), OptionPrice, OptionPrice, "", 0, "", "Yes", Convert.ToString(DeadLineDate.Date)));
                                                                                dtXTGHotelRoomCacelationPolicy.AcceptChanges();
                                                                            }
                                                                            else
                                                                            {
                                                                                dtXTGHotelRoomCacelationPolicy.Rows.Add(_commonMethod.setCancellationPolicyRow(ref OptionPrice, dtXTGHotelRoomCacelationPolicy.NewRow(), CurrencyFactor, currency, Item.Supplier, Convert.ToString(OwnsystemHotelId), Convert.ToString(1), Convert.ToString(RoomTyypeId)
    , RoomTypeName, Convert.ToString(boardCode), curDate, DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null), OptionPrice, OptionPrice, "", 0, "", "Yes", Convert.ToString(DeadLineDate.Date)));
                                                                                dtXTGHotelRoomCacelationPolicy.AcceptChanges();
                                                                            }
                                                                        }
                                                                    }
                                                                }
                                                            }
                                                        }
                                                        else
                                                        {
                                                            RoomTypeName += " [Non Refundable]";
                                                            //hoursBefore = Convert.ToDouble(listroom[rr].cancelPolicy.cancelPenalties[0].hoursBefore);

                                                            RoomTyypeId = id + "$$" + hoursBefore;

                                                            dtXTGHotelRoomCacelationPolicy.Rows.Add(_commonMethod.setCancellationPolicyRow(ref OptionPrice, dtXTGHotelRoomCacelationPolicy.NewRow(), CurrencyFactor, currency, Item.Supplier, Convert.ToString(OwnsystemHotelId), Convert.ToString(1), Convert.ToString(RoomTyypeId)
    , RoomTypeName, Convert.ToString(boardCode), curDate, DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null), OptionPrice, OptionPrice, "", 0, "", "Yes", Convert.ToString(DeadLineDate.Date)));
                                                            dtXTGHotelRoomCacelationPolicy.AcceptChanges();
                                                        }

                                                    }
                                                    else
                                                    {
                                                        RoomTypeName += " [Non Refundable]";
                                                        RoomTyypeId = id + "$$" + hoursBefore;

                                                        dtXTGHotelRoomCacelationPolicy.Rows.Add(_commonMethod.setCancellationPolicyRow(ref OptionPrice, dtXTGHotelRoomCacelationPolicy.NewRow(), CurrencyFactor, currency, Item.Supplier, Convert.ToString(OwnsystemHotelId), Convert.ToString(1), Convert.ToString(RoomTyypeId)
, RoomTypeName, Convert.ToString(boardCode), curDate, DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null), OptionPrice, OptionPrice, "", 0, "", "Yes", Convert.ToString(DeadLineDate.Date)));
                                                        dtXTGHotelRoomCacelationPolicy.AcceptChanges();
                                                    }

                                                    #endregion

                                                    if (Provider.ToLower() == "welcomebeds_xtg")
                                                    {
                                                        bool ischhild = false;
                                                        for (int ser = 0; ser < SearchParam.Rooms.Count; ser++)
                                                        {
                                                            if (SearchParam.Rooms[ser].NoofChild > 0)
                                                                ischhild = true;
                                                        }
                                                        if (ischhild)
                                                            remarks += "<tr><td>Note : </td><td>The child's bed may not confirmed and it may be shared</td></tr>";

                                                    }
                                                    if (Provider.ToLower() == "italcamel_xtg")
                                                    {
                                                        bool isAge12 = false;
                                                        for (int ser = 0; ser < SearchParam.Rooms.Count; ser++)
                                                        {
                                                            if (SearchParam.Rooms[ser].Child1Age == 12 || SearchParam.Rooms[ser].Child2Age == 12)
                                                                isAge12 = true;
                                                        }
                                                        if (isAge12)
                                                            remarks += "<tr><td>Note : </td><td>Note: child of 12 years may considered as an Adult</td></tr>";

                                                    }
                                                    if (Provider.ToLower() == "teamamerica_xtg")
                                                    {
                                                        bool isAge12 = false;
                                                        for (int ser = 0; ser < SearchParam.Rooms.Count; ser++)
                                                        {
                                                            if (SearchParam.Rooms[ser].NoofChild > 0)
                                                                isAge12 = true;
                                                        }
                                                        if (isAge12)
                                                            remarks += "<tr><td>Note : </td><td>Note: child  may considered as an Adult</td></tr>";

                                                    }
                                                    remarks = remarks + "</table>";
                                                    if (remarks == "<table></table>")
                                                        remarks = " ";

                                                    drXTGHotelRoom["RoomTypeId"] = Convert.ToString(RoomTyypeId);
                                                    drXTGHotelRoom["EssentialInformation"] = remarks;// == "" && suppliment == "" ? "" : remarks + "<br/>" + suppliment;
                                                    drXTGHotelRoom["RoomTypeName"] = RoomTypeName;
                                                    drXTGHotelRoom["SupplierName"] = Provider;
                                                    dtXTGHotelRoom.Rows.Add(drXTGHotelRoom);
                                                }

                                                #endregion
                                            }

                                            Options ops = await _supplierRes.GetCommonRoomForSuppliers(GiataId, commonSearch.tokenData, commonSearch.advancedOptions, Convert.ToDouble(lowestPrice), drXmlHotel, dtXTGHotelRoom, dtXTGHotelRoomCacelationPolicy, dtXTGHotelRoomRateBreakup, SearchParam);
                                            if (ops != null)
                                                Options.Add(ops);
                                        }

                                        //  }

                                    }
                                    #endregion

                                }
                                #endregion
                            }
                        }
                        #endregion
                    }
                    #endregion

                }


                return Options;

            }

            catch (Exception ex)
            {
                await _exception.Error(ex);
                return Options;
            }
            finally
            {
                #region ResProcessTimer
                try
                {
                    if (SearchParam != null && ResProccessTimerLogActive == "1" && timer_room_Xtg.ElapsedMilliseconds > Convert.ToInt32(ResProccessTimeValueforSaveLog))
                    {
                        timer_room_Xtg.Stop();
                        SearchParam.SupplierTime += ",|Finish:" + Provider + " Process Response  " + "|Time :" + timer_room_Xtg.Elapsed.ToString();
                        _ = Task.Run(() => _commonMethod.FireAway_SQL_ResPorcessTimeLog("", timer_room_Xtg.Elapsed.ToString(), Provider, SearchParam.SearchKey, 1, SearchParam.AgentID));
                        //Task.Run(async () =>await _commonMethod.FireAway_SQL_ResPorcessTimeLog("", timer_room_Xtg.Elapsed.ToString(), Provider, SearchParam.SearchKey, 1, SearchParam.AgentID));
                    }
                }
                catch (Exception x)
                {
                    await _exception.Error(x);
                }
                #endregion
            }
            return Options;
        }

        public async Task<List<Options>> XMLXTG_RoomOnly(HotelX Item, CommonSearch commonSearch, string RQ, string currencyCode, DataTable dtAgentDetail, DataTable dtMarkupDetails, HotelSearchParameter SearchParam, string ResponseData, string Provider, string xmlcity, string ProccessCutOffTime, int processTimeout, string ResProccessTimerLogActive, string ResProccessTimeValueforSaveLog, double CurrencyFactor = 4)
        {
            System.Diagnostics.Stopwatch timer_room_Xtg = new System.Diagnostics.Stopwatch();
            timer_room_Xtg.Start();

            DataTable DtHotel = _commonMethod.GenerateDataTableNewV2();
            DataTable DtHotelRoom = _commonMethod.GenerateRoomtypeDataTableNewV2();
            DataTable DtHotelPolicy = _commonMethod.GenerateCancellationDataTableNewV2();

            string reqid = Convert.ToString(DateTime.Now.Ticks);
            System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
            timer.Start();

            List<Options> Options = new List<Options>();
            try
            {
                //Xtg_hotellist.Root ds = null;
                //try
                //{
                //    ds = JsonSerializer.Deserialize<Xtg_hotellist.Root>(ResponseData);
                //}

                //catch (Exception ex)
                //{
                //    _exception.Error(ex);
                //}

                if (ResponseData == "")
                {
                    Item.ResponseStatus = 204;
                    Item.ResponseType = "NoCount";
                    return Options;
                }

                Xtg_Roomlist.Root dsRM = null;
                try
                {
                    //dsRM = JsonSerializer.Deserialize<Xtg_Roomlist.Root>(ResponseData);
                    dsRM = Newtonsoft.Json.JsonConvert.DeserializeObject<Xtg_Roomlist.Root>(ResponseData);
                }

                catch (Exception ex)
                {
                    await _exception.Error(ex);
                }

                if (dsRM.data.hotelX.search.errors != null)
                {
                    Item.SupplierResponseError = ResponseData;
                    Item.ResponseStatus = 404;
                    Item.ResponseType = "NoCount";
                    return Options;
                }

                if (dsRM != null && dsRM.data.hotelX.search.options != null)
                {

                    // DataSet dt = _commonMethod.GetDataSetFromXMLString(dx);


                    //string context = Convert.ToString(dt.Tables["search"].Rows[0]["context"]);

                    string context = Convert.ToString(dsRM.data.hotelX.search.context);

                    //DataTable dtoptions = dt.Tables["options"].DefaultView.ToTable(true, "hotelCode");
                    //for (int h = 0; h < dt.Tables["options"].Rows.Count; h++)


                    var distinctHotels = dsRM.data.hotelX.search.options.Where(h => h.status.ToLower() == "ok")
                                        .Select(g => g.hotelCode).Distinct().ToList();


                    // for (int h = 0; h < distinctHotels.Count; h++)
                    for (int j = 0; j < distinctHotels.Count; j++)
                    {
                        var listroom = dsRM.data.hotelX.search.options.Where(h => h.hotelCode == distinctHotels[j] && h.status.ToLower() == "ok").ToList();


                        if (listroom.Count > 0)
                        {
                            Item.ResponseType = "Success";

                            if (ProccessCutOffTime == "1" && timer_room_Xtg.ElapsedMilliseconds > processTimeout)
                            {
                                timer_room_Xtg.Stop();
                                break;
                            }

                            #region When Not Need Rooms

                            if (SearchParam.Filters.IsShowRooms == 0)
                            {
                                #region 040425
                                var allPrices = listroom
                                                 .SelectMany(lr => lr.rooms)
                                                 .Select(r => Convert.ToDouble(r.roomPrice.price.net))
                                                 .ToList();

                                double minPrice = allPrices.Min();

                                #endregion

                                #region "Hotel Code Block "
                                #region "No Of Hotels "
                                DataRow drXmlHotel = DtHotel.NewRow();

                                double NetPrice = 1;
                                double Lowest = 0;
                                //drXmlHotel["XMLHotelCode"] = Convert.ToString(ds.data.hotelX.search.options[h].hotelCode);
                                //drXmlHotel["XMLHotelCode"] = Convert.ToString(dtoptions.Rows[h]["hotelCode"]);
                                drXmlHotel["XMLHotelCode"] = Convert.ToString(distinctHotels[j]);
                                drXmlHotel["HotelName"] = "";
                                drXmlHotel["SupplierName"] = Provider;
                                drXmlHotel["OfficeID"] = 0;
                                drXmlHotel["ContractID"] = 0;
                                drXmlHotel["Rating"] = 0;
                                drXmlHotel["Reviews"] = 0;
                                drXmlHotel["UniqueNo"] = 0;
                                drXmlHotel["SupplierCurrency"] = currencyCode;

                                drXmlHotel["IsShowSupplier"] = false;
                                drXmlHotel["BookingStatus"] = "Available";
                                drXmlHotel["XMLCity"] = "DXB";

                                //DataRow[] drroomss = dt.Tables["rooms"].Select("options_Id=" + Convert.ToInt32(dt.Tables["options"].Rows[h]["options_Id"]) + "");
                                if (listroom[0].rooms.Count == 0)
                                    continue;
                                //DataRow[] drprice = dt.Tables["price"].Select("roomPrice_Id=" + Convert.ToInt32(dt.Tables["options"].Rows[h]["options_Id"]) + "");
                                //double Pricenew = Convert.ToDouble(Convert.ToDouble(drprice[0]["net"]) * Convert.ToInt32(SearchParam.Rooms.Count)) * CurrencyFactor;
                                double Pricenew = Convert.ToDouble(Convert.ToDouble(minPrice) * Convert.ToInt32(SearchParam.Rooms.Count)) * CurrencyFactor;
                                if (Pricenew == 0)
                                {
                                    Item.ResponseType = "NoCount";
                                    continue;
                                }
                                //Pricenew = NetPrice; 
                                drXmlHotel["FinalSellingPrice"] = Pricenew;
                                drXmlHotel["SellingPrice"] = Pricenew;


                                DtHotel.Rows.Add(drXmlHotel);
                                if (NetPrice > 0)
                                {
                                    #region "GetRooms for this hotel"
                                    bool willAddOrNot = true;
                                    long RaynaID = 0;
                                    long GiataId = 0;
                                    int Rating = 0;
                                    long OwnsystemHotelId = 0;
                                    double MinPrice = NetPrice;
                                    _markup.GetHotelDataCommon(ref MinPrice, ref willAddOrNot, ref OwnsystemHotelId, ref RaynaID, ref GiataId, ref Rating, ref drXmlHotel, commonSearch.AgentId, commonSearch.TariffID, commonSearch.dsAgentDetails, commonSearch.supplierGiataMaster, SearchParam, commonSearch.dtCountryCityMarkup, commonSearch.B2CMarkup, commonSearch.IsParentId, commonSearch.SubAgentMarkup, commonSearch.SearchByHotel, commonSearch.HotelIDS);
                                    if (willAddOrNot)
                                    {
                                        #region "Room Logic"
                                        DataTable dt_HotelRoom = _commonMethod.GenerateRoomtypeDataTableNewV2();
                                        DataTable dt_Cancellation = _commonMethod.GenerateCancellationDataTableNewV2();
                                        DataTable dt_RateBreackUp = _commonMethod.GenerateoomRateBreackUpDataTableNewV2();
                                        #endregion
                                        decimal LowestPrice = Convert.ToDecimal(MinPrice).CalculatePrice(commonSearch.tokenData, commonSearch.advancedOptions, currencyCode);
                                        Options ops = await _supplierRes.GetCommonRoomForSuppliers(GiataId, commonSearch.tokenData, commonSearch.advancedOptions, Convert.ToDouble(LowestPrice), drXmlHotel, DtHotelRoom, DtHotelPolicy, dt_RateBreackUp, SearchParam);
                                        if (ops != null)
                                            Options.Add(ops);
                                    }
                                    #endregion

                                }


                                #endregion
                                #endregion
                            }
                            #endregion
                            #region When Need Rooms
                            else
                            {
                                //Xtg_Roomlist.Root dsRM = null;
                                //try
                                //{
                                //    dsRM = JsonSerializer.Deserialize<Xtg_Roomlist.Root>(ResponseData);
                                //}

                                //catch (Exception ex)
                                //{
                                //    _exception.Error(ex);
                                //}

                                //var listroom = dsRM.data.hotelX.search.options.Where(h => h.hotelCode == distinctHotels[j] && h.status.ToLower() == "ok").ToList();

                                //if (listroom.Count > 0)
                                {
                                    string GIATACode = "";
                                    #region "Hotel Code Block "
                                    string ResponseString = "";
                                    double AgentPercentage = 0.0;
                                    double AgentMarkupAmount = 0.0;
                                    bool isPerce = true;

                                    DataRow[] XTGChild = commonSearch.dtSuplierMarkup.Select("SupplierName='" + Provider + "' ");

                                    string AccessCode = Convert.ToString(XTGChild[0]["UserName"]);

                                    string SupplierCode = Convert.ToString(XTGChild[0]["UserId"]);
                                    string checkin = SearchParam.CheckInDate.Substring(6, 4) + "-" + SearchParam.CheckInDate.Substring(3, 2) + "-" + SearchParam.CheckInDate.Substring(0, 2);
                                    string checkout = SearchParam.CheckOutDate.Substring(6, 4) + "-" + SearchParam.CheckOutDate.Substring(3, 2) + "-" + SearchParam.CheckOutDate.Substring(0, 2);
                                    int CutOffDays = CacheMaster.CutOffDays;
                                    CutOffDays = _commonMethod.GetCutOffDays(commonSearch.dtSuplierMarkup, Item.Supplier);
                                    //DataRow[] droptions = dt.Tables["options"].Select("search_Id='" + dt.Tables["search"].Rows[h]["search_Id"] + "'");
                                    DataRow drXmlHotel = DtHotel.NewRow();

                                    double NetPrice = 1;
                                    double Lowest = 0;
                                    // drXmlHotel["XMLHotelCode"] = Convert.ToString(dtoptions.Rows[h]["hotelCode"]);
                                    drXmlHotel["XMLHotelCode"] = Convert.ToString(listroom[0].hotelCode);
                                    drXmlHotel["HotelName"] = "";
                                    drXmlHotel["SupplierName"] = Provider;
                                    drXmlHotel["OfficeID"] = 0;
                                    drXmlHotel["ContractID"] = 0;
                                    drXmlHotel["Rating"] = 0;
                                    drXmlHotel["Reviews"] = 0;
                                    drXmlHotel["UniqueNo"] = 0;
                                    drXmlHotel["SupplierCurrency"] = currencyCode;

                                    drXmlHotel["IsShowSupplier"] = false;
                                    drXmlHotel["BookingStatus"] = "Available";
                                    drXmlHotel["XMLCity"] = "DXB";

                                    // DataRow[] drroomss = dt.Tables["rooms"].Select("options_Id=" + Convert.ToInt32(dt.Tables["options"].Rows[h]["options_Id"]) + "");
                                    //if (drroomss.Length == 0)
                                    //  continue;
                                    if (listroom[0].rooms.Count == 0)
                                        continue;
                                    //DataRow[] drprice = dt.Tables["price"].Select("roomPrice_Id=" + Convert.ToInt32(dt.Tables["options"].Rows[h]["options_Id"]) + "");
                                    // double Pricenew = Convert.ToDouble(Convert.ToDouble(drprice[0]["net"]) * Convert.ToInt32(SearchParam.Rooms.Count)) * CurrencyFactor;
                                    double Pricenew = Convert.ToDouble(Convert.ToDouble(listroom[0].price.net) * Convert.ToInt32(SearchParam.Rooms.Count)) * CurrencyFactor;
                                    if (Pricenew == 0)
                                    {
                                        Item.ResponseType = "NoCount";
                                        continue;
                                    }
                                    //Pricenew = NetPrice; 
                                    drXmlHotel["FinalSellingPrice"] = Pricenew;
                                    drXmlHotel["SellingPrice"] = Pricenew;


                                    DtHotel.Rows.Add(drXmlHotel);
                                    if (NetPrice > 0)
                                    {
                                        #region "GetRooms for this hotel"
                                        bool willAddOrNot = true;
                                        long RaynaID = 0;
                                        long GiataId = 0;
                                        int Rating = 0;
                                        int RatingHotel = 0;
                                        long OwnsystemHotelId = 0;
                                        double MinPrice = NetPrice;
                                        _markup.GetHotelDataCommon(ref MinPrice, ref willAddOrNot, ref OwnsystemHotelId, ref RaynaID, ref GiataId, ref Rating, ref drXmlHotel, commonSearch.AgentId, commonSearch.TariffID, commonSearch.dsAgentDetails, commonSearch.supplierGiataMaster, SearchParam, commonSearch.dtCountryCityMarkup, commonSearch.B2CMarkup, commonSearch.IsParentId, commonSearch.SubAgentMarkup, commonSearch.SearchByHotel, commonSearch.HotelIDS);
                                        if (willAddOrNot)
                                        {
                                            #region "Room Logic"
                                            //DataTable dt_HotelRoom = Common.GenerateRoomtypeDataTableNewV2();
                                            //DataTable dt_Cancellation = Common.GenerateCancellationDataTableNewV2();

                                            DataTable dtXTGHotelRoom = _commonMethod.GenerateRoomtypeDataTableNewV2();
                                            DataTable dtXTGHotelRoomRateBreakup = _commonMethod.GenerateoomRateBreackUpDataTableNewV2();
                                            DataTable dtXTGHotelRoomCacelationPolicy = _commonMethod.GenerateCancellationDataTableNewV2();

                                            #endregion
                                            bool blnIsShowSupplier = false;
                                            decimal lowestPrice = Convert.ToDecimal(MinPrice).CalculatePrice(commonSearch.tokenData, commonSearch.advancedOptions, currencyCode);
                                            //_markup.getAgetMarkupSetting(Provider, GiataId.ToString(), commonSearch.dtSuplierMarkup, commonSearch.dtMarkupDetails, ref AgentPercentage, ref AgentMarkupAmount, ref blnIsShowSupplier, ref isPerce, commonSearch.AgentId, RatingHotel, SearchParam.CityID, SearchParam.CountryID.ToString(), SearchParam.CheckInDate);
                                            _markup.getAgetMarkupSettingNew_new(Provider, Convert.ToString(GiataId), commonSearch.dtSuplierMarkup, commonSearch.dsMarkupDetails, ref AgentPercentage, ref AgentMarkupAmount, ref blnIsShowSupplier, ref isPerce, commonSearch.AgentId, RatingHotel, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), SearchParam.CheckInDate);



                                            #region "Options"
                                            //DataRow[] droptions = dt.Tables["options"].Select("hotelCode='" + Convert.ToString(dtoptions.Rows[h]["hotelCode"]) + "'");
                                            // if (droptions.Length > 0)

                                            //                            var distinctOptions = dsRM.data.hotelX.search.options
                                            //.GroupBy(option => option.hotelCode) 
                                            //.Select(group => group.First()) 
                                            //.ToList();



                                            //if (distinctHotels.Count > 0)
                                            //{
                                            //  for (int j = 0; j < distinctHotels.Count; j++)
                                            //{

                                            //var listroom = dsRM.data.hotelX.search.options.Where(h => h.hotelCode == distinctHotels[j] && h.status.ToLower() == "ok").ToList();

                                            //if (listroom.Count > 0)
                                            //{
                                            for (int rr = 0; rr < listroom.Count; rr++)
                                            {

                                                //string options_Id = Convert.ToString(droptions[j]["options_Id"]);
                                                //string status = Convert.ToString(dsRM.data.hotelX.search.options[j].status);
                                                string status = Convert.ToString(listroom[rr].status);
                                                if (status.ToLower() != "ok")
                                                {
                                                    continue;
                                                }
                                                string rateRules = "";
                                                //if (dt.Tables["options"].Columns.Contains("rateRules"))
                                                // rateRules = Convert.ToString(droptions[j]["rateRules"]);
                                                if (listroom[rr].rateRules != null)
                                                { // rateRules = Convert.ToString(dsRM.data.hotelX.search.options[j].rateRules);
                                                    rateRules = Convert.ToString(listroom[rr].rateRules[0]);
                                                    //Convert.ToString(listroom[rr].rateRules);
                                                }

                                                if (Provider.ToLower() == "trip.com_xtg")
                                                {
                                                    rateRules = "";
                                                }
                                                string remarks = "<table>";
                                                /////string remarksRoom = Convert.ToString(droptions[j]["remarks"]);
                                                //  if (remarksRoom != "")
                                                //  remarks += "<tr><td>SupplierNote:</td><td>" + remarksRoom + "</td></tr>";
                                                //string remarks = Convert.ToString(droptions[j]["remarks"]);
                                                //string token = Convert.ToString(droptions[j]["token"]);
                                                // string token = Convert.ToString(dsRM.data.hotelX.search.options[j].token);
                                                string token = Convert.ToString(listroom[rr].token);
                                                //string id = Convert.ToString(droptions[j]["id"]) + "$$" + Guid.NewGuid().ToString().Substring(0, 4);
                                                //string accessCode = Convert.ToString(droptions[j]["accessCode"]);
                                                // string accessCode = Convert.ToString(dsRM.data.hotelX.search.options[j].accessCode);
                                                string accessCode = Convert.ToString(listroom[rr].accessCode);
                                                //string supplierCode = Convert.ToString(droptions[j]["supplierCode"]);
                                                // string supplierCode = Convert.ToString(dsRM.data.hotelX.search.options[j].supplierCode);
                                                string supplierCode = Convert.ToString(listroom[rr].supplierCode);
                                                //string hotelCode = Convert.ToString(droptions[j]["hotelCode"]);
                                                // string hotelCode = Convert.ToString(dsRM.data.hotelX.search.options[j].hotelCode);
                                                string hotelCode = Convert.ToString(listroom[rr].hotelCode);
                                                // string context = Convert.ToString(droptions[j]["context"]);
                                                string id = Convert.ToString(listroom[rr].id) + "$$" + supplierCode + "$$" + accessCode + "$$" + hotelCode + "$$" + currencyCode + "$$" + Guid.NewGuid().ToString().Substring(0, 4);
                                                id = id.Replace("'", "");
                                                // string boardCode = Convert.ToString(droptions[j]["boardCode"]);
                                                //string boardCode = Convert.ToString(dsRM.data.hotelX.search.options[j].boardCode);
                                                string boardCode = Convert.ToString(listroom[rr].boardCode);


                                                #region "Option Variable"
                                                double OptionPrice = 0;
                                                string RoomTypeName = "";
                                                string suppliment = "";
                                                int AdultsOccupancy = 0;
                                                int ChildOccupancy = 0;
                                                string mealName = "Room Only";

                                                #region"MealNAme"
                                                GetMealRoomData_Xtg("Meal", boardCode, ref mealName, Provider, supplierCode, AccessCode);
                                                #endregion
                                                #endregion
                                                #region"Option Price"



                                                //DataRow[] drPriceOption = dt.Tables["price"].Select("options_Id='" + options_Id + "'");
                                                string currency = "";
                                                //if (drPriceOption.Length > 0)
                                                if (listroom[rr].price != null)
                                                {
                                                    // bool binding = Convert.ToBoolean(drPriceOption[0]["binding"]);
                                                    //bool binding = dsRM.data.hotelX.search.options[j].price.binding;
                                                    bool binding = Convert.ToBoolean(listroom[rr].price.binding);
                                                    // OptionPrice = Convert.ToDouble(drPriceOption[0]["net"]) * CurrencyFactor;
                                                    //OptionPrice =(dsRM.data.hotelX.search.options[j].price.net) * CurrencyFactor;
                                                    OptionPrice = Convert.ToDouble(listroom[rr].price.net) * CurrencyFactor;
                                                    // currency = Convert.ToString(drPriceOption[0]["currency"]);
                                                    //currency = dsRM.data.hotelX.search.options[j].price.currency;
                                                    currency = Convert.ToString(listroom[rr].price.currency);

                                                }
                                                if (currencyCode.ToLower() != currency.ToLower())
                                                {
                                                    continue;
                                                }
                                                #endregion

                                                // DataRow[] drrooms = dt.Tables["rooms"].Select("options_Id='" + options_Id + "'");
                                                #region "Rooms"
                                                //if (drrooms.Length > 0)
                                                // if (dsRM.data.hotelX.search.options[j].rooms != null)
                                                if (listroom[rr].rooms != null)
                                                {
                                                    #region"BedDesc"
                                                    //DataRow[] droccupancies = dt.Tables["occupancies"].Select("options_Id='" + options_Id + "' ");
                                                    //if (droccupancies.Length > 0)
                                                    if (listroom[rr].occupancies != null)
                                                    {
                                                        for (int ocu = 0; ocu < listroom[rr].occupancies.Count; ocu++)
                                                        {
                                                            // string occupancies_Id = Convert.ToString(droccupancies[ocu]["occupancies_Id"]);
                                                            string occupancies_Id = Convert.ToString(listroom[rr].occupancies[ocu].id);
                                                            // DataRow[] drpaxes = dt.Tables["paxes"].Select("occupancies_Id='" + occupancies_Id + "' ");
                                                            if (listroom[rr].occupancies[ocu].paxes != null)
                                                            {
                                                                for (int px = 0; px < listroom[rr].occupancies[ocu].paxes.Count; px++)
                                                                {
                                                                    if (listroom[rr].occupancies[ocu].paxes[px].age < 35)
                                                                    {
                                                                        ChildOccupancy += 1;
                                                                    }
                                                                    else
                                                                    {
                                                                        AdultsOccupancy += 1;
                                                                    }
                                                                }
                                                            }

                                                        }
                                                    }
                                                    #endregion
                                                    DataRow drXTGHotelRoom = dtXTGHotelRoom.NewRow();

                                                    drXTGHotelRoom["OwnsystemHotelId"] = OwnsystemHotelId;
                                                    drXTGHotelRoom["ContractID"] = "0";
                                                    drXTGHotelRoom["UniqueNo"] = OwnsystemHotelId;
                                                    drXTGHotelRoom["MealName"] = mealName;
                                                    drXTGHotelRoom["MealId"] = boardCode;
                                                    drXTGHotelRoom["SHRUI"] = token;
                                                    drXTGHotelRoom["Board"] = boardCode;
                                                    drXTGHotelRoom["Type"] = "";
                                                    drXTGHotelRoom["RoomNo"] = "1";
                                                    drXTGHotelRoom["BookingStatus"] = rateRules == "" ? "Available" : rateRules;
                                                    drXTGHotelRoom["BoardType"] = "";
                                                    drXTGHotelRoom["Characteristic"] = "";
                                                    //drXTGHotelRoom["RoomTypeId"] = id;
                                                    drXTGHotelRoom["RoomTypeCode"] = 0;
                                                    drXTGHotelRoom["CompanyBuyingPrice"] = OptionPrice;
                                                    drXTGHotelRoom["AdultsOccupancy"] = AdultsOccupancy;
                                                    drXTGHotelRoom["ChildOccupancy"] = ChildOccupancy;
                                                    drXTGHotelRoom["MaxExtrabed"] = 0;
                                                    drXTGHotelRoom["OfferDescription"] = "";
                                                    drXTGHotelRoom["BoardTypeID"] = "";
                                                    #region Vat
                                                    _markup.ProcessHotelRoom_WithMarkup(CurrencyFactor, currencyCode, true, true, SearchParam.Rooms.Count * SearchParam.NoofNights, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), SearchParam.CheckInDate, SearchParam.CheckOutDate, ref OptionPrice, ref drXTGHotelRoom, isPerce, AgentPercentage, AgentMarkupAmount, commonSearch.SubAgentMarkup, commonSearch.B2CMarkup, commonSearch.IsParentId);
                                                    #endregion
                                                    List<Room_NameMerge> roomlist = new List<Room_NameMerge>();
                                                    #region"Suppliment"
                                                    //if (dt.Tables.Contains("supplements"))
                                                    if (listroom[rr].supplements != null)
                                                    {
                                                        //DataRow[] drsupplements = dt.Tables["supplements"].Select("options_Id='" + options_Id + "' and code<>''");
                                                        //if (drsupplements.Length > 0)
                                                        {
                                                            //string supplements_Code = Convert.ToString(drsupplements[0]["code"]);
                                                            //string supplements_supplementType = Convert.ToString(drsupplements[0]["supplementType"]);
                                                            //string supplements_chargeType = Convert.ToString(drsupplements[0]["chargeType"]);
                                                            //string supplements_mandatory = Convert.ToString(drsupplements[0]["mandatory"]);
                                                            //string supplements_quantity = Convert.ToString(drsupplements[0]["quantity"]);
                                                            //string supplements_unit = Convert.ToString(drsupplements[0]["unit"]);
                                                            //string supplements_effectiveDate = Convert.ToString(drsupplements[0]["effectiveDate"]);
                                                            //string supplements_expireDate = Convert.ToString(drsupplements[0]["expireDate"]);
                                                            //string supplements_Id = Convert.ToString(drsupplements[0]["supplements_Id"]);
                                                            //need
                                                            string supplements_Code = Convert.ToString(listroom[rr].supplements.code);
                                                            string supplements_supplementType = Convert.ToString(listroom[rr].supplements.supplementType);
                                                            string supplements_chargeType = Convert.ToString(listroom[rr].supplements.chargeType);
                                                            string supplements_mandatory = Convert.ToString(listroom[rr].supplements.mandatory);
                                                            string supplements_quantity = Convert.ToString(listroom[rr].supplements.quantity);
                                                            string supplements_unit = Convert.ToString(listroom[rr].supplements.unit);
                                                            string supplements_effectiveDate = Convert.ToString(listroom[rr].supplements.effectiveDate);
                                                            string supplements_expireDate = Convert.ToString(listroom[rr].supplements.expireDate);
                                                            //string supplements_Id = Convert.ToString(drsupplements[0]["supplements_Id"]);

                                                            //DataRow[] drresort = dt.Tables["resort"].Select("supplements_Id='" + supplements_Id + "'");
                                                            string resort_description = "";
                                                            // if (drresort.Length > 0)
                                                            if (listroom[rr].supplements.resort != null)
                                                            {
                                                                // resort_description = Convert.ToString(drresort[0]["description"]);
                                                                resort_description = Convert.ToString(listroom[rr].supplements.resort.description);
                                                            }
                                                            suppliment += "Code :" + supplements_Code + "<br/>" +
                                                                "supplement :" + supplements_supplementType + " " + resort_description + "<br/>" +
                                                                (supplements_chargeType.ToUpper() == "INCLUDE" ? "Include in total" : "not include in total") +
                                                                "<br/>mandatory :" + (supplements_mandatory.ToLower() == "true" ? "Yes" : "NO") + "<br/>" +
                                                                "For :" + supplements_quantity + " " + supplements_unit + "<br/>" +
                                                                "effectiveDate :" + (supplements_effectiveDate == "" ? "-" : supplements_effectiveDate) + "<br/>" +
                                                                "expireDate :" + (supplements_expireDate == "" ? "-" : supplements_expireDate) + "<br/>";
                                                            if (suppliment != "")
                                                                remarks += "<tr><td>supplement:</td><td>" + suppliment + "</td></tr>";
                                                        }
                                                    }
                                                    #endregion
                                                    //for (int room = 0; room < drrooms.Length; room++)
                                                    for (int room = 0; room < listroom[rr].rooms.Count; room++)
                                                    {
                                                        //string rooms_Id = Convert.ToString(drrooms[room]["rooms_Id"]);
                                                        // string code = Convert.ToString(drrooms[room]["code"]);
                                                        string code = Convert.ToString(listroom[rr].rooms[room].code);
                                                        string description = Convert.ToString(listroom[rr].rooms[room].description);
                                                        if (description == "" || SupplierCode.ToLower() == "fit" || SupplierCode.ToLower() == "mtbk" || SupplierCode.ToLower() == "whl")
                                                        {
                                                            #region"RoomName"
                                                            GetMealRoomData_Xtg("Room", code, ref description, Provider, supplierCode, AccessCode);
                                                            #endregion
                                                        }
                                                        Room_NameMerge obj = new Room_NameMerge();
                                                        obj.RoomCount = 1;
                                                        obj.RoomName = description;
                                                        obj.status = true;
                                                        roomlist.Add(obj);
                                                        bool IsRoomRefundable = false;

                                                        // string refundable = Convert.ToString(drrooms[room]["refundable"]);
                                                        string refundable = Convert.ToString(listroom[rr].rooms[room].refundable);
                                                        if (refundable.ToLower() == "true")
                                                            IsRoomRefundable = true;

                                                        #region "Room Variable initialise"
                                                        double RoomPrice = 0;
                                                        string BedDescription = "";
                                                        string RatePlan = "";
                                                        string OfferDecription = "";
                                                        string OfferCode = "";

                                                        #endregion

                                                        // DataRow[] drroomPrice = dt.Tables["roomPrice"].Select("rooms_Id='" + rooms_Id + "'");
                                                        // if (drroomPrice.Length > 0)
                                                        if (listroom[rr].rooms[room] != null)
                                                        {
                                                            //string roomPrice_Id = Convert.ToString(drroomPrice[0]["roomPrice_Id"]);
                                                            // DataRow[] drPrice = dt.Tables["price"].Select("roomPrice_Id='" + roomPrice_Id + "'");
                                                            // if (drPrice.Length > 0)
                                                            if (listroom[rr].price != null)
                                                            {
                                                                //RoomPrice = Convert.ToDouble(drPrice[0]["net"]) * CurrencyFactor;
                                                                RoomPrice = Convert.ToDouble(listroom[rr].price.net) * CurrencyFactor;
                                                            }
                                                        }
                                                        if (RoomPrice > 0)
                                                        {
                                                            #region"BedDesc"
                                                            if (listroom[rr].rooms[room].beds != null)
                                                            {
                                                                //DataRow[] drbeds = dt.Tables["beds"].Select("rooms_Id='" + rooms_Id + "'");
                                                                // if (drbeds.Length > 0)
                                                                // if (drbeds.Length > 0)
                                                                {
                                                                    //string Bed_Type = Convert.ToString(drbeds[0]["type"]);
                                                                    //string Bed_counte = Convert.ToString(drbeds[0]["count"]);
                                                                    //string Bed_shared = Convert.ToString(drbeds[0]["shared"]);
                                                                    string Bed_Type = Convert.ToString(listroom[rr].rooms[room].beds[0].type);
                                                                    string Bed_counte = Convert.ToString(listroom[rr].rooms[room].beds[0].count);
                                                                    string Bed_shared = listroom[rr].rooms[room].beds[0].shared != null ? Convert.ToString(listroom[rr].rooms[room].beds[0].shared) : "";
                                                                    if (Bed_Type != "")
                                                                        BedDescription = "<b>Bed type:</b> " + Bed_Type + "<b> count: </b>" + Bed_counte + (Bed_shared.ToLower() == "true" ? " (Shared)" : "");
                                                                    else
                                                                        BedDescription = "<b>" + Bed_counte + (Bed_shared.ToLower() == "true" ? " (Shared)" : "</b> Bed Requested");

                                                                    if (BedDescription != "")
                                                                        remarks += "<tr><td>Bed Description:</td><td>" + BedDescription + "</td></tr>";
                                                                }
                                                            }
                                                            #endregion
                                                            #region"RatePlans"
                                                            //if (dt.Tables.Contains("ratePlans"))
                                                            if (listroom[rr].rooms[room].ratePlans != null)
                                                            {
                                                                // DataRow[] drratePlans = dt.Tables["ratePlans"].Select("rooms_Id='" + rooms_Id + "'");
                                                                // if (drratePlans.Length > 0)
                                                                {
                                                                    //  string ratePlans_Code = Convert.ToString(drratePlans[0]["code"]);
                                                                    string ratePlans_Code = Convert.ToString(listroom[rr].rooms[room].ratePlans[0].code);
                                                                    //string ratePlans_effectiveDate = Convert.ToString(drratePlans[0]["effectiveDate"]);
                                                                    string ratePlans_effectiveDate = Convert.ToString(listroom[rr].rooms[room].ratePlans[0].effectiveDate);
                                                                    //string ratePlans_expireDate = Convert.ToString(drratePlans[0]["expireDate"]);
                                                                    string ratePlans_expireDate = Convert.ToString(listroom[rr].rooms[room].ratePlans[0].expireDate);
                                                                    RatePlan = "<b>Rate Plan:</b>" + ratePlans_Code + " " + "<b> effectiveDate:</b> " + (ratePlans_effectiveDate == "" ? "-" : ratePlans_effectiveDate) + "<b> expireDate:</b> " + (ratePlans_expireDate == "" ? "-" : ratePlans_expireDate);
                                                                    if (RatePlan != "")
                                                                        remarks += "<tr><td>Rate PlanDetails:</td><td>" + RatePlan + "</td></tr>";
                                                                }
                                                            }
                                                            #endregion
                                                            #region"Promotions"
                                                            // if (dt.Tables.Contains("promotions"))
                                                            {
                                                                // DataRow[] drpromotions = dt.Tables["promotions"].Select("rooms_Id='" + rooms_Id + "' and code<>''");
                                                                // if (drpromotions.Length > 0)
                                                                if (listroom[rr].rooms[room].promotions != null && listroom[rr].rooms[room].promotions.Count > 0)
                                                                {
                                                                    // string promotions_Code = Convert.ToString(drpromotions[0]["code"]);
                                                                    string promotions_Code = Convert.ToString(listroom[rr].rooms[room].promotions[0].code);
                                                                    // string promotions_name = Convert.ToString(drpromotions[0]["name"]);
                                                                    string promotions_name = Convert.ToString(listroom[rr].rooms[room].promotions[0].name);
                                                                    //string promotions_effectiveDate = Convert.ToString(drpromotions[0]["effectiveDate"]);
                                                                    string promotions_effectiveDate = Convert.ToString(listroom[rr].rooms[room].promotions[0].effectiveDate);
                                                                    //string promotions_expireDate = Convert.ToString(drpromotions[0]["expireDate"]);
                                                                    string promotions_expireDate = Convert.ToString(listroom[rr].rooms[room].promotions[0].expireDate);
                                                                    OfferCode = promotions_Code;
                                                                    if (promotions_Code != "")
                                                                        OfferDecription = "Code: " + promotions_Code;
                                                                    if ("" != promotions_name)
                                                                        OfferDecription += "<b> Name:</b> " + promotions_name;
                                                                    if (promotions_effectiveDate != "")
                                                                        OfferDecription += "<b> effectiveDate: </b>" + promotions_effectiveDate;
                                                                    if (promotions_expireDate != "")
                                                                        OfferDecription += "<b> expireDate :</b>" + promotions_expireDate;

                                                                    drXTGHotelRoom["OfferDescription"] = OfferDecription;
                                                                    drXTGHotelRoom["OfferCode"] = promotions_Code == "" ? promotions_name : promotions_Code;
                                                                    if (OfferDecription != "")
                                                                        remarks += "<tr><td>Offer Description:</td><td>" + OfferDecription + "</td></tr>";

                                                                }
                                                            }
                                                            #endregion

                                                            #region"Ratebreakup"
                                                            //DateTime dtCheckInDateForRate = DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null);
                                                            //#region Vat
                                                            //double VatOnRateBrackUp = 0;
                                                            //double RoomPrice1 = _markup.ProcessHotelRateBreakupPrice_WithMarkup(1, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), RoomPrice / SearchParam.NoofNights, commonSearch.B2CMarkup, commonSearch.IsParentId, isPerce, AgentPercentage, AgentMarkupAmount, commonSearch.SubAgentMarkup, ref VatOnRateBrackUp);
                                                            //#endregion
                                                            //for (int night = 0; night < SearchParam.NoofNights; night++)
                                                            //{
                                                            //    string rateWeekColumn = Convert.ToString(dtCheckInDateForRate.Date.AddDays(night).DayOfWeek).Substring(0, 3);
                                                            //    DataRow drXTGRoomRateBreackUp = dtXTGHotelRoomRateBreakup.NewRow();
                                                            //    drXTGRoomRateBreackUp["RoomNo"] = Convert.ToString("1");
                                                            //    drXTGRoomRateBreackUp["SupplierName"] = Provider;
                                                            //    drXTGRoomRateBreackUp["OwnsystemHotelId"] = OwnsystemHotelId;
                                                            //    drXTGRoomRateBreackUp["ContractID"] = "0";
                                                            //    drXTGRoomRateBreackUp["RoomTypeName"] = description;
                                                            //    drXTGRoomRateBreackUp["RoomTypeId"] = id;
                                                            //    drXTGRoomRateBreackUp["MealId"] = boardCode;
                                                            //    drXTGRoomRateBreackUp["Meal"] = mealName;
                                                            //    drXTGRoomRateBreackUp["BookingStatus"] = rateRules == "" ? "Available" : rateRules;
                                                            //    drXTGRoomRateBreackUp["DateOfWeek"] = rateWeekColumn;
                                                            //    drXTGRoomRateBreackUp["Date"] = dtCheckInDateForRate.Date.AddDays(night).Date;
                                                            //    drXTGRoomRateBreackUp["Rate"] = RoomPrice1;
                                                            //    drXTGRoomRateBreackUp["RateType"] = SearchParam.Rooms.Count == 1 ? "A Rate" : room + 1 + ". " + description;
                                                            //    // drXTGRoomRateBreackUp["GIITACode"] = GiataId;

                                                            //    string HotelFessDescription = "";
                                                            //    if (RatePlan != string.Empty)
                                                            //        HotelFessDescription = RatePlan;
                                                            //    if (BedDescription != string.Empty)
                                                            //        HotelFessDescription += "</br>" + BedDescription;
                                                            //    drXTGRoomRateBreackUp["HotelFessDescription"] = HotelFessDescription;

                                                            //    drXTGRoomRateBreackUp["SupplierCurrency"] = currencyCode;
                                                            //    drXTGRoomRateBreackUp["BuyingPrice"] = RoomPrice1 / CurrencyFactor;
                                                            //    dtXTGHotelRoomRateBreakup.Rows.Add(drXTGRoomRateBreackUp);
                                                            //    dtXTGHotelRoomRateBreakup.AcceptChanges();
                                                            //}
                                                            #endregion
                                                            //if (dt.Tables.Contains("features"))
                                                            if (listroom[rr].rooms[room].features != null)
                                                            {
                                                                //DataRow[] drbeds = dt.Tables["features"].Select("rooms_Id='" + rooms_Id + "'");
                                                                //if (drbeds.Length > 0)
                                                                {
                                                                    //string Bed_Type = Convert.ToString(drbeds[0]["code"]);
                                                                    string Bed_Type = Convert.ToString(listroom[rr].rooms[room].features.code);
                                                                    if (Bed_Type != "")
                                                                        remarks += "<tr><td>Room Note:</td><td>" + Bed_Type + "</td></tr>";
                                                                }
                                                            }

                                                        }
                                                    }
                                                    #region "Logic For Room Name "
                                                    for (int rm = 0; rm < roomlist.Count; rm++)
                                                    {
                                                        for (int r2 = rm + 1; r2 < roomlist.Count; r2++)
                                                        {
                                                            if (roomlist[rm].RoomName == roomlist[r2].RoomName && roomlist[r2].status)
                                                            {
                                                                roomlist[rm].RoomCount = roomlist[rm].RoomCount + roomlist[r2].RoomCount;
                                                                roomlist[r2].RoomCount = roomlist[rm].RoomCount;
                                                                roomlist[r2].status = false;
                                                            }
                                                        }
                                                        if (RoomTypeName.IndexOf(roomlist[rm].RoomName) < 0 || RoomTypeName == "")
                                                            RoomTypeName = RoomTypeName + roomlist[rm].RoomCount + " X " + roomlist[rm].RoomName + " ";
                                                    }

                                                    #endregion


                                                    #region CancelltionPolicy - OLD
                                                    //                                                    DateTime curDate = _commonMethod.GetTimeZoneDate(SearchParam.CurrentDate);
                                                    //                                                    #region Vat
                                                    //                                                    double VatOnPolicy = 0;
                                                    //                                                    double ChargeAmount = 0;
                                                    //                                                    DateTime DeadLineDate = DateTime.Now;

                                                    //                                                    //if (dt.Tables.Contains("cancelPolicy"))
                                                    //                                                    if (listroom[rr].cancelPolicy != null)
                                                    //                                                    {
                                                    //                                                        bool isRefundable = Convert.ToBoolean(listroom[rr].cancelPolicy.refundable);
                                                    //                                                        if (isRefundable)
                                                    //                                                        {


                                                    //                                                            //DataRow[] drRatePlanCancellationPolicyList = dt.Tables["cancelPolicy"].Select("options_Id='" + options_Id + "'");
                                                    //                                                            // if (drRatePlanCancellationPolicyList.Length > 0)
                                                    //                                                            if (listroom[rr].cancelPolicy.cancelPenalties != null)
                                                    //                                                            {
                                                    //                                                                for (int c = 0; c < listroom[rr].cancelPolicy.cancelPenalties.Count; c++)
                                                    //                                                                {
                                                    //                                                                    //if (dt.Tables.Contains("cancelPenalties"))
                                                    //                                                                    {
                                                    //                                                                        // DataRow[] cancelPenalties = dt.Tables["cancelPenalties"].Select("cancelPolicy_Id='" + drRatePlanCancellationPolicyList[c][1] + "'");

                                                    //                                                                        //if (cancelPenalties.Length > 0)
                                                    //                                                                        {
                                                    //                                                                            // bool isRefundable = Convert.ToBoolean(drRatePlanCancellationPolicyList[c][0]);
                                                    //                                                                            {
                                                    //                                                                                //if (cancelPenalties[c][0] != DBNull.Value)
                                                    //                                                                                if (listroom[rr].cancelPolicy.cancelPenalties[c].value != null)
                                                    //                                                                                {
                                                    //                                                                                    DeadLineDate = Convert.ToDateTime(checkin).AddHours(-Convert.ToDouble(listroom[rr].cancelPolicy.cancelPenalties[c].hoursBefore));
                                                    //                                                                                }
                                                    //                                                                                else
                                                    //                                                                                {
                                                    //                                                                                    RoomTypeName += " [Non Refundable]";
                                                    //                                                                                }
                                                    //                                                                            }


                                                    //                                                                            #endregion
                                                    //                                                                            if (DeadLineDate.AddDays(CutOffDays).Date > DateTime.Now.Date)
                                                    //                                                                                DeadLineDate = DeadLineDate.AddDays(CutOffDays).Date;
                                                    //                                                                            else
                                                    //                                                                                DeadLineDate = DateTime.Now.Date;
                                                    //                                                                            if (curDate.Date < DeadLineDate.Date)
                                                    //                                                                            {
                                                    //                                                                                if (c == 0)
                                                    //                                                                                {
                                                    //                                                                                    double Zero = 0;
                                                    //                                                                                    dtXTGHotelRoomCacelationPolicy.Rows.Add(_commonMethod.setCancellationPolicyRow(ref Zero, dtXTGHotelRoomCacelationPolicy.NewRow(), CurrencyFactor, currency, Item.Supplier, Convert.ToString(OwnsystemHotelId), Convert.ToString(1), Convert.ToString(id)
                                                    //    , RoomTypeName, Convert.ToString(boardCode), curDate, DeadLineDate.AddDays(-1).AddHours(23).AddMinutes(59).AddSeconds(59), 0, 0, "", 0, "", "NO", Convert.ToString(DeadLineDate.Date)));
                                                    //                                                                                    dtXTGHotelRoomCacelationPolicy.AcceptChanges();
                                                    //                                                                                }
                                                    //                                                                                dtXTGHotelRoomCacelationPolicy.Rows.Add(_commonMethod.setCancellationPolicyRow(ref OptionPrice, dtXTGHotelRoomCacelationPolicy.NewRow(), CurrencyFactor, currency, Item.Supplier, Convert.ToString(OwnsystemHotelId), Convert.ToString(1), Convert.ToString(id)
                                                    //    , RoomTypeName, Convert.ToString(boardCode), DeadLineDate, DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null), OptionPrice, OptionPrice, "", 0, "", "Yes", Convert.ToString(DeadLineDate.Date)));
                                                    //                                                                                dtXTGHotelRoomCacelationPolicy.AcceptChanges();
                                                    //                                                                            }
                                                    //                                                                            else
                                                    //                                                                            {
                                                    //                                                                                dtXTGHotelRoomCacelationPolicy.Rows.Add(_commonMethod.setCancellationPolicyRow(ref OptionPrice, dtXTGHotelRoomCacelationPolicy.NewRow(), CurrencyFactor, currency, Item.Supplier, Convert.ToString(OwnsystemHotelId), Convert.ToString(1), Convert.ToString(id)
                                                    //    , RoomTypeName, Convert.ToString(boardCode), curDate, DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null), OptionPrice, OptionPrice, "", 0, "", "Yes", Convert.ToString(DeadLineDate.Date)));
                                                    //                                                                                dtXTGHotelRoomCacelationPolicy.AcceptChanges();
                                                    //                                                                            }
                                                    //                                                                        }
                                                    //                                                                    }
                                                    //                                                                }
                                                    //                                                            }
                                                    //                                                        }
                                                    //                                                        else
                                                    //                                                        {
                                                    //                                                            RoomTypeName += " [Non Refundable]";

                                                    //                                                            dtXTGHotelRoomCacelationPolicy.Rows.Add(_commonMethod.setCancellationPolicyRow(ref OptionPrice, dtXTGHotelRoomCacelationPolicy.NewRow(), CurrencyFactor, currency, Item.Supplier, Convert.ToString(OwnsystemHotelId), Convert.ToString(1), Convert.ToString(id)
                                                    //    , RoomTypeName, Convert.ToString(boardCode), curDate, DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null), OptionPrice, OptionPrice, "", 0, "", "Yes", Convert.ToString(DeadLineDate.Date)));
                                                    //                                                            dtXTGHotelRoomCacelationPolicy.AcceptChanges();
                                                    //                                                        }

                                                    //                                                    }
                                                    //                                                    else
                                                    //                                                    {
                                                    //                                                        RoomTypeName += " [Non Refundable]";

                                                    //                                                        dtXTGHotelRoomCacelationPolicy.Rows.Add(_commonMethod.setCancellationPolicyRow(ref OptionPrice, dtXTGHotelRoomCacelationPolicy.NewRow(), CurrencyFactor, currency, Item.Supplier, Convert.ToString(OwnsystemHotelId), Convert.ToString(1), Convert.ToString(id)
                                                    //, RoomTypeName, Convert.ToString(boardCode), curDate, DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null), OptionPrice, OptionPrice, "", 0, "", "Yes", Convert.ToString(DeadLineDate.Date)));
                                                    //                                                        dtXTGHotelRoomCacelationPolicy.AcceptChanges();
                                                    //                                                    }

                                                    #endregion

                                                    #region CancelltionPolicy
                                                    DateTime curDate = _commonMethod.GetTimeZoneDate(SearchParam.CurrentDate);
                                                    #region Vat
                                                    double VatOnPolicy = 0;
                                                    double ChargeAmount = 0;
                                                    DateTime DeadLineDate = DateTime.Now;
                                                    Double hoursBefore = 0;
                                                    string RoomTyypeId = "";

                                                    //if (dt.Tables.Contains("cancelPolicy"))
                                                    if (listroom[rr].cancelPolicy != null)
                                                    {
                                                        bool isRefundable = Convert.ToBoolean(listroom[rr].cancelPolicy.refundable);

                                                        if (listroom[rr].cancelPolicy.cancelPenalties != null && listroom[rr].cancelPolicy.cancelPenalties.Count > 0)
                                                        {
                                                            int c = 0;
                                                            if (isRefundable)
                                                            {

                                                                //if (cancelPenalties[c][0] != DBNull.Value)
                                                                if (listroom[rr].cancelPolicy.cancelPenalties[c].value != null)
                                                                {
                                                                    hoursBefore = Convert.ToDouble(listroom[rr].cancelPolicy.cancelPenalties[c].hoursBefore);
                                                                    //DeadLineDate = Convert.ToDateTime(checkin).AddHours(-hoursBefore);//cm by 30/07/25
                                                                    //DeadLineDate = Convert.ToDateTime(checkin).AddHours(-Convert.ToDouble(listroom[rr].cancelPolicy.cancelPenalties[c].hoursBefore));

                                                                    //Added on 30/07/25
                                                                    List<int> lstHoursBefore = listroom[rr].cancelPolicy.cancelPenalties.Where(hours => hours.value > 0).Select(hours => hours.hoursBefore).ToList();
                                                                    Double hoursBeforee = lstHoursBefore.Max();
                                                                    DeadLineDate = Convert.ToDateTime(checkin).AddHours(-hoursBeforee);
                                                                }
                                                                else
                                                                {
                                                                    RoomTypeName += " [Non Refundable]";
                                                                }
                                                            }
                                                            else
                                                            {
                                                                if (listroom[rr].cancelPolicy.cancelPenalties[c].value != null)
                                                                {
                                                                    hoursBefore = Convert.ToDouble(listroom[rr].cancelPolicy.cancelPenalties[c].hoursBefore);
                                                                    RoomTypeName += " [Non Refundable]";
                                                                }
                                                                else
                                                                {
                                                                    RoomTypeName += " [Non Refundable]";
                                                                }
                                                            }
                                                            //for (int c = 0; c < listroom[rr].cancelPolicy.cancelPenalties.Count; c++)
                                                            RoomTyypeId = id + "$$" + hoursBefore;

                                                            #endregion
                                                            if (DeadLineDate.AddDays(CutOffDays).Date > DateTime.Now.Date)
                                                                DeadLineDate = DeadLineDate.AddDays(CutOffDays).Date;
                                                            else
                                                                DeadLineDate = DateTime.Now.Date;
                                                            if (curDate.Date < DeadLineDate.Date)
                                                            {
                                                                if (c == 0)
                                                                {
                                                                    double Zero = 0;
                                                                    dtXTGHotelRoomCacelationPolicy.Rows.Add(_commonMethod.setCancellationPolicyRow(ref Zero, dtXTGHotelRoomCacelationPolicy.NewRow(), CurrencyFactor, currency, Item.Supplier, Convert.ToString(OwnsystemHotelId), Convert.ToString(1), Convert.ToString(RoomTyypeId)
, RoomTypeName, Convert.ToString(boardCode), curDate, DeadLineDate.AddDays(-1).AddHours(23).AddMinutes(59).AddSeconds(59), 0, 0, "", 0, "", "NO", Convert.ToString(DeadLineDate.Date)));
                                                                    dtXTGHotelRoomCacelationPolicy.AcceptChanges();
                                                                }
                                                                dtXTGHotelRoomCacelationPolicy.Rows.Add(_commonMethod.setCancellationPolicyRow(ref OptionPrice, dtXTGHotelRoomCacelationPolicy.NewRow(), CurrencyFactor, currency, Item.Supplier, Convert.ToString(OwnsystemHotelId), Convert.ToString(1), Convert.ToString(RoomTyypeId)
, RoomTypeName, Convert.ToString(boardCode), DeadLineDate, DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null), OptionPrice, OptionPrice, "", 0, "", "Yes", Convert.ToString(DeadLineDate.Date)));
                                                                dtXTGHotelRoomCacelationPolicy.AcceptChanges();
                                                            }
                                                            else
                                                            {
                                                                dtXTGHotelRoomCacelationPolicy.Rows.Add(_commonMethod.setCancellationPolicyRow(ref OptionPrice, dtXTGHotelRoomCacelationPolicy.NewRow(), CurrencyFactor, currency, Item.Supplier, Convert.ToString(OwnsystemHotelId), Convert.ToString(1), Convert.ToString(RoomTyypeId)
, RoomTypeName, Convert.ToString(boardCode), curDate, DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null), OptionPrice, OptionPrice, "", 0, "", "Yes", Convert.ToString(DeadLineDate.Date)));
                                                                dtXTGHotelRoomCacelationPolicy.AcceptChanges();
                                                            }
                                                        }
                                                        else
                                                        {
                                                            RoomTyypeId = id + "$$" + hoursBefore;
                                                            RoomTypeName += " [Non Refundable]";

                                                            dtXTGHotelRoomCacelationPolicy.Rows.Add(_commonMethod.setCancellationPolicyRow(ref OptionPrice, dtXTGHotelRoomCacelationPolicy.NewRow(), CurrencyFactor, currency, Item.Supplier, Convert.ToString(OwnsystemHotelId), Convert.ToString(1), Convert.ToString(RoomTyypeId)
, RoomTypeName, Convert.ToString(boardCode), curDate, DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null), OptionPrice, OptionPrice, "", 0, "", "Yes", Convert.ToString(DeadLineDate.Date)));
                                                            dtXTGHotelRoomCacelationPolicy.AcceptChanges();
                                                        }

                                                    }

                                                    else
                                                    {
                                                        RoomTypeName += " [Non Refundable]";
                                                        RoomTyypeId = id + "$$" + hoursBefore;

                                                        dtXTGHotelRoomCacelationPolicy.Rows.Add(_commonMethod.setCancellationPolicyRow(ref OptionPrice, dtXTGHotelRoomCacelationPolicy.NewRow(), CurrencyFactor, currency, Item.Supplier, Convert.ToString(OwnsystemHotelId), Convert.ToString(1), Convert.ToString(RoomTyypeId)
, RoomTypeName, Convert.ToString(boardCode), curDate, DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null), OptionPrice, OptionPrice, "", 0, "", "Yes", Convert.ToString(DeadLineDate.Date)));
                                                        dtXTGHotelRoomCacelationPolicy.AcceptChanges();
                                                    }

                                                    #endregion

                                                    if (Provider.ToLower() == "welcomebeds_xtg")
                                                    {
                                                        bool ischhild = false;
                                                        for (int ser = 0; ser < SearchParam.Rooms.Count; ser++)
                                                        {
                                                            if (SearchParam.Rooms[ser].NoofChild > 0)
                                                                ischhild = true;
                                                        }
                                                        if (ischhild)
                                                            remarks += "<tr><td>Note : </td><td>The child's bed may not confirmed and it may be shared</td></tr>";

                                                    }
                                                    if (Provider.ToLower() == "italcamel_xtg")
                                                    {
                                                        bool isAge12 = false;
                                                        for (int ser = 0; ser < SearchParam.Rooms.Count; ser++)
                                                        {
                                                            if (SearchParam.Rooms[ser].Child1Age == 12 || SearchParam.Rooms[ser].Child2Age == 12)
                                                                isAge12 = true;
                                                        }
                                                        if (isAge12)
                                                            remarks += "<tr><td>Note : </td><td>Note: child of 12 years may considered as an Adult</td></tr>";

                                                    }
                                                    if (Provider.ToLower() == "teamamerica_xtg")
                                                    {
                                                        bool isAge12 = false;
                                                        for (int ser = 0; ser < SearchParam.Rooms.Count; ser++)
                                                        {
                                                            if (SearchParam.Rooms[ser].NoofChild > 0)
                                                                isAge12 = true;
                                                        }
                                                        if (isAge12)
                                                            remarks += "<tr><td>Note : </td><td>Note: child  may considered as an Adult</td></tr>";

                                                    }
                                                    remarks = remarks + "</table>";
                                                    if (remarks == "<table></table>")
                                                        remarks = " ";

                                                    drXTGHotelRoom["RoomTypeId"] = Convert.ToString(RoomTyypeId);
                                                    drXTGHotelRoom["EssentialInformation"] = remarks;// == "" && suppliment == "" ? "" : remarks + "<br/>" + suppliment;
                                                    drXTGHotelRoom["RoomTypeName"] = RoomTypeName;
                                                    drXTGHotelRoom["SupplierName"] = Provider;
                                                    dtXTGHotelRoom.Rows.Add(drXTGHotelRoom);
                                                }

                                                #endregion
                                            }

                                            Options ops = await _supplierRes.GetCommonRoomForSuppliers(GiataId, commonSearch.tokenData, commonSearch.advancedOptions, Convert.ToDouble(lowestPrice), drXmlHotel, dtXTGHotelRoom, dtXTGHotelRoomCacelationPolicy, dtXTGHotelRoomRateBreakup, SearchParam);
                                            if (ops != null)
                                                Options.Add(ops);
                                        }

                                        //  }

                                    }
                                    #endregion

                                }
                                #endregion
                            }
                        }
                        #endregion
                    }
                    #endregion

                }
                else
                {
                    Item.ResponseStatus = 404;
                }


                return Options;

            }

            catch (Exception ex)
            {
                Item.ResponseStatus = 404;
                await _exception.Error(ex);
                return Options;
            }
            finally
            {
                #region ResProcessTimer
                try
                {
                    timer_room_Xtg.Stop();
                    Item.CodeProcessingTimeSupplier = timer_room_Xtg.ElapsedMilliseconds;
                    if (SearchParam != null && ResProccessTimerLogActive == "1" && timer_room_Xtg.ElapsedMilliseconds > Convert.ToInt32(ResProccessTimeValueforSaveLog))
                    {
                        
                        SearchParam.SupplierTime += ",|Finish:" + Provider + " Process Response  " + "|Time :" + timer_room_Xtg.Elapsed.ToString();
                        _ = Task.Run(() => _commonMethod.FireAway_SQL_ResPorcessTimeLog("", timer_room_Xtg.Elapsed.ToString(), Provider, SearchParam.SearchKey, 1, SearchParam.AgentID));
                        //Task.Run(async () =>await _commonMethod.FireAway_SQL_ResPorcessTimeLog("", timer_room_Xtg.Elapsed.ToString(), Provider, SearchParam.SearchKey, 1, SearchParam.AgentID));
                    }
                }
                catch (Exception x)
                {
                    await _exception.Error(x);
                }
                #endregion
            }
            return Options;
        }

        public void GetMealRoomData_Xtg_2705(string Type, string Code, ref string returnString, string SupplierName, string supplierCode, string AccessCode)
        {
            try
            {
                DataSet ds = new DataSet();
                if (!_cache.TryGetValue("HotelInfomation_xtg_" + Type + "_" + SupplierName, out ds))
                {
                    ds = GetXTGHotelData(supplierCode, AccessCode, Type);
                    //System.Web.HttpRuntime.Cache.Insert("HotelInfomation_xtg_" + Type + "_" + SupplierName, ds, null, DateTime.Now.AddDays(30), TimeSpan.Zero);
                    _cache.Set(Type + "_" + SupplierName, ds, DateTime.Now.AddDays(30));
                    // _cache.Set(Type + "_" + SupplierName, ds, TimeSpan.FromMinutes(30)));

                }
                //else
                //{
                //    ds = (DataSet)_cache.Get("HotelInfomation_xtg_" + Type + "_" + SupplierName);
                //}
                DataRow[] drboardData = null;
                string TableTOSearch = "";
                string FieldToSearchId = "";
                string fieldTOCompare = "";

                switch (Type.ToLower())
                {
                    case "meal":
                        TableTOSearch = "boardData";
                        FieldToSearchId = "boardData_Id";
                        fieldTOCompare = "boardCode";
                        break;
                    case "room":
                        TableTOSearch = "roomData";
                        FieldToSearchId = "roomData_Id";
                        fieldTOCompare = "roomCode";
                        break;
                    default:
                        returnString = "";
                        break;
                }

                if (ds != null && ds.Tables[TableTOSearch] != null && ds.Tables[TableTOSearch].Rows.Count > 0)
                    foreach (DataRow row in ds.Tables[TableTOSearch].Rows)
                    {
                        if (Convert.ToString(row[fieldTOCompare]) == Code)
                        {
                            drboardData = new DataRow[1];
                            drboardData[0] = row;
                            break;
                        }
                    }
                DataRow[] drMealName = null;
                if (drboardData != null && drboardData.Length > 0)
                {
                    string boardData_Id = Convert.ToString(drboardData[0][FieldToSearchId]);
                    foreach (DataRow row in ds.Tables["texts"].Rows)
                    {
                        if (Convert.ToString(row[FieldToSearchId]) == boardData_Id && Convert.ToString(row["language"]).ToLower() == "en")
                        {
                            drMealName = new DataRow[1];
                            drMealName[0] = row;
                            break;
                        }
                    }
                }
                if (drMealName != null && drMealName.Length > 0)
                    returnString = Convert.ToString(drMealName[0]["text"]).Replace("-", " ");
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
            }
        }

        public void GetMealRoomData_Xtg(string type, string code, ref string returnString, string supplierName, string supplierCode, string accessCode)
        {
            try
            {
                string cacheKey = $"HotelInfomation_xtg_{type}_{supplierName}";
                if (!_cache.TryGetValue(cacheKey, out DataSet ds))
                {
                    ds = GetXTGHotelData(supplierCode, accessCode, type);
                    _cache.Set(cacheKey, ds, DateTime.Now.AddDays(30));
                }

                string tableToSearch;
                string fieldToSearchId;
                string fieldToCompare;

                switch (type.ToLower())
                {
                    case "meal":
                        tableToSearch = "boardData";
                        fieldToSearchId = "boardData_Id";
                        fieldToCompare = "boardCode";
                        break;
                    case "room":
                        tableToSearch = "roomData";
                        fieldToSearchId = "roomData_Id";
                        fieldToCompare = "roomCode";
                        break;
                    default:
                        returnString = string.Empty;
                        return;
                }

                if (ds?.Tables[tableToSearch] == null || ds.Tables[tableToSearch].Rows.Count == 0)
                {
                    //returnString = string.Empty;
                    return;
                }

                // Find the matching data row
                DataRow matchedRow = ds.Tables[tableToSearch]
                    .AsEnumerable()
                    .FirstOrDefault(row => string.Equals(row[fieldToCompare]?.ToString(), code, StringComparison.OrdinalIgnoreCase));

                if (matchedRow == null)
                {
                    //returnString = string.Empty;
                    return;
                }

                string dataId = matchedRow[fieldToSearchId]?.ToString();
                if (string.IsNullOrEmpty(dataId) || ds.Tables["texts"] == null)
                {
                    //returnString = string.Empty;
                    return;
                }

                // Find the English text row
                DataRow textRow = ds.Tables["texts"]
                    .AsEnumerable()
                    .FirstOrDefault(row => string.Equals(row[fieldToSearchId]?.ToString(), dataId, StringComparison.OrdinalIgnoreCase)
                                           && string.Equals(row["language"]?.ToString(), "en", StringComparison.OrdinalIgnoreCase));

                returnString = textRow?["text"]?.ToString().Replace("-", " ") ?? string.Empty;
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
            }
        }

        public DataSet GetXTGHotelData(string supplierCode, string AccessCode, string Type, int RefreshCache = 0)
        {

            try
            {
                //countryid = 0;
                DataSet CityWiseHotelInformation = new DataSet();
                string path = Convert.ToString(XtgInfo.XTGHoteldata) + Type + "\\" + supplierCode.ToLower() + AccessCode + ".xml";
                DataSet dsDT = new DataSet();

                if (File.Exists(path))
                {
                    FileStream fs1 = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                    try
                    {
                        using (StreamReader r = new StreamReader(fs1))
                        {
                            string json = r.ReadToEnd();
                            CityWiseHotelInformation = _commonMethod.GetDataSetFromXMLString(json);
                        }
                    }
                    finally
                    {
                        fs1.Close();
                        fs1.Dispose();
                        //GC.Collect();
                    }
                }
                return CityWiseHotelInformation;
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
                //Common.Error(ex);
                return null;
            }
        }

        //recheck
        public string XTG_EssentialInfoOnly(DataRow[] XTGDetails, DataRow XTGChild, HotelSearchParameter SearchParam, string RoomTypeId, int SearchTimeout, string SearchKey)
        {
            string remarks = "";
            try
            {

                string Error = "";
                string XTGSiteID = Convert.ToString(XTGDetails[0]["UserID"]);
                string SupplierCode = Convert.ToString(XTGChild["UserId"]);
                string XTGWebUrl = Convert.ToString(XTGDetails[0]["WebSiteURL"]);
                string XTGApiKey = Convert.ToString(XTGDetails[0]["Password"]);
                string RequestString = GetQuoteRequest_XTG(RoomTypeId, XTGSiteID, SearchTimeout, SupplierCode);
                //var client = new XMLTravelGate.GraphQLClient(XTGWebUrl);
                GraphQLQuery_Response objResponse = new GraphQLQuery_Response();
                //objResponse = client.GetXTGResponse(ref Error, XMLTravelGate.GraphQLClient.XTG_Quote_Query, Newtonsoft.Json.Linq.JObject.Parse(RequestString), XTGApiKey, SearchTimeout, SearchParam);
                objResponse = GetXTGResponse(ref Error, XTGWebUrl, XMLTravelGate.GraphQLClient.XTG_Quote_Query, Newtonsoft.Json.Linq.JObject.Parse(RequestString), XTGApiKey, SearchTimeout, SearchParam);
                string ResponseString = "";
                try
                {
                    ResponseString = Newtonsoft.Json.JsonConvert.DeserializeXmlNode(objResponse.Response).InnerXml;

                }
                catch (Exception ex)
                {
                    _ = _exception.ErrorNew(ex, _LogAPI.Recheck, SearchKey);
                }
                DataSet ds = _commonMethod.GetDataSetFromXMLString(ResponseString);
                if (ds.Tables["optionQuote"] != null && ds.Tables["optionQuote"].Rows.Count > 0)
                {
                    string optionRefId = Convert.ToString(ds.Tables["optionQuote"].Rows[0]["optionRefId"]);
                    remarks = Convert.ToString(ds.Tables["optionQuote"].Rows[0]["remarks"]);


                }
            }
            catch (Exception ex)
            {
                _ = _exception.ErrorNew(ex, _LogAPI.Recheck, SearchKey);
            }
            return remarks;
        }

        public class ResponseXTGDto
        {
            public GraphQLQuery_Response GraphQLQuery_Response { get; set; }
            public string Error { get; set; }
        }



        public GraphQLQuery_Response GetXTGResponse(ref string Error, string url, string query, object variables, string Apikey, int TimeOut, HotelSearchParameter para = null, string Provider = "", bool DebugMode = false)
        {
            GraphQLQuery_Response final = new GraphQLQuery_Response();
            System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
            timer.Start();


            try
            {
                var fullQuery = new GraphQLQuery()
                {
                    query = query,
                    variables = variables,
                };
                string jsonContent = Newtonsoft.Json.JsonConvert.SerializeObject(fullQuery);

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "POST";
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                UTF8Encoding encoding = new UTF8Encoding();
                Byte[] byteArray = encoding.GetBytes(jsonContent.Trim());
                ServicePointManager.DefaultConnectionLimit = 1000;
                request.ContentLength = byteArray.Length;
                request.ContentType = @"application/json";
                request.Headers.Add("Accept-Encoding", "gzip");
                request.Timeout = TimeOut;
                request.Headers.Add("Authorization", "Apikey " + Apikey + "");
                using (Stream dataStream = request.GetRequestStream())
                {
                    dataStream.Write(byteArray, 0, byteArray.Length);
                }
                try
                {
                    HttpWebResponse Response = (HttpWebResponse)request.GetResponse();
                    Stream responseStream = null;
                    if (Response.Headers.Get("Content-Encoding") == "gzip")
                        responseStream = new GZipStream(Response.GetResponseStream(), CompressionMode.Decompress);
                    else
                        responseStream = Response.GetResponseStream();
                    //Read the output
                    StreamReader Reader = new StreamReader(responseStream, Encoding.UTF8);
                    string strOutput = Reader.ReadToEnd();
                    responseStream.Close();
                    Reader.Close();
                    Response.Close();
                    final.Response = strOutput;
                    final.IsError = false;
                    timer.Stop();
                    if (para != null)
                    {
                        para.SupplierTime += ",SupplierName :" + Provider + ": " + "Execution Time :" + timer.Elapsed.ToString();
                        if (DebugMode)
                            _commonMethod.XMLRequestResponseTrack(Convert.ToString(variables), strOutput, Provider, "Proper");
                    }
                    //using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                    //{
                    //    length = response.ContentLength;
                    //    using (Stream responseStream = response.GetResponseStream())
                    //    {
                    //        StreamReader reader = new StreamReader(responseStream, Encoding.UTF8);
                    //        string json = reader.ReadToEnd();
                    //        return json;
                    //        // return new GraphQLQueryResult(json);
                    //    }
                    //}
                }
                catch (WebException ex)
                {
                    if (ex.Response != null)
                    {
                        WebResponse errorResponse = ex.Response;
                        using (Stream responseStream = errorResponse.GetResponseStream())
                        {
                            StreamReader reader = new StreamReader(responseStream, Encoding.GetEncoding("utf-8"));
                            String errorText = reader.ReadToEnd();
                            //  Console.WriteLine(errorText);
                            final.Response = errorText;
                            final.IsError = true;
                            // return new GraphQLQueryResult(null, ex);
                        }
                    }
                    else
                    {
                        final.Response = "timeout";
                        final.IsError = true;
                    }
                    Error = final.Response + "#Message= " + ex.Message;
                }
            }
            catch (Exception ex)
            {

                _exception.Error(ex);
                final.Response = ex.Message;
                final.IsError = true;
            }

            return final;
        }

        public async Task<ResponseXTGDto> GetXTGResponseAsync(string url, string query, object variables, string Apikey, int TimeOut, HotelSearchParameter para = null, string Provider = "", bool DebugMode = false)
        {
            ResponseXTGDto responseXTGDto = new ResponseXTGDto();

            GraphQLQuery_Response final = new GraphQLQuery_Response();
            System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
            timer.Start();

            string Error = "";

            try
            {
                var fullQuery = new GraphQLQuery()
                {
                    query = query,
                    variables = variables,
                };
                string jsonContent = Newtonsoft.Json.JsonConvert.SerializeObject(fullQuery);

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "POST";
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                UTF8Encoding encoding = new UTF8Encoding();
                Byte[] byteArray = encoding.GetBytes(jsonContent.Trim());
                ServicePointManager.DefaultConnectionLimit = 1000;
                request.ContentLength = byteArray.Length;
                request.ContentType = @"application/json";
                request.Headers.Add("Accept-Encoding", "gzip");
                request.Timeout = TimeOut;
                request.Headers.Add("Authorization", "Apikey " + Apikey + "");
                using (Stream dataStream = request.GetRequestStream())
                {
                    dataStream.Write(byteArray, 0, byteArray.Length);
                }
                try
                {
                    HttpWebResponse Response = (HttpWebResponse)await request.GetResponseAsync();
                    Stream responseStream = null;
                    if (Response.Headers.Get("Content-Encoding") == "gzip")
                        responseStream = new GZipStream(Response.GetResponseStream(), CompressionMode.Decompress);
                    else
                        responseStream = Response.GetResponseStream();
                    //Read the output
                    StreamReader Reader = new StreamReader(responseStream, Encoding.UTF8);
                    string strOutput = await Reader.ReadToEndAsync();
                    responseStream.Close();
                    Reader.Close();
                    Response.Close();
                    final.Response = strOutput;
                    final.IsError = false;
                    timer.Stop();
                    if (para != null)
                    {
                        para.SupplierTime += ",SupplierName :" + Provider + ": " + "Execution Time :" + timer.Elapsed.ToString();
                        if (DebugMode)
                            _ = _commonMethod.XMLRequestResponseTrack(Convert.ToString(variables), strOutput, Provider, "Proper");
                    }
                    //using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                    //{
                    //    length = response.ContentLength;
                    //    using (Stream responseStream = response.GetResponseStream())
                    //    {
                    //        StreamReader reader = new StreamReader(responseStream, Encoding.UTF8);
                    //        string json = reader.ReadToEnd();
                    //        return json;
                    //        // return new GraphQLQueryResult(json);
                    //    }
                    //}
                }
                catch (WebException ex)
                {
                    if (ex.Response != null)
                    {
                        WebResponse errorResponse = ex.Response;
                        using (Stream responseStream = errorResponse.GetResponseStream())
                        {
                            StreamReader reader = new StreamReader(responseStream, Encoding.GetEncoding("utf-8"));
                            String errorText = reader.ReadToEnd();
                            //  Console.WriteLine(errorText);
                            final.Response = errorText;
                            final.IsError = true;
                            // return new GraphQLQueryResult(null, ex);
                        }
                    }
                    else
                    {
                        final.Response = "timeout";
                        final.IsError = true;
                    }
                    Error = final.Response + "#Message= " + ex.Message;
                }
            }
            catch (Exception ex)
            {

                _exception.Error(ex);
                final.Response = ex.Message;
                final.IsError = true;
            }

            responseXTGDto.Error = Error;
            responseXTGDto.GraphQLQuery_Response = final;

            return responseXTGDto;
        }


        public string GetQuoteRequest_XTG(string RoomtypeID, string client, int Timeout, string SupplierCode)
        {
            try
            {
                string[] spitid = RoomtypeID.Split(new String[] { "$$" }, StringSplitOptions.None);
                string optionRefId = spitid[0].ToString();
                XMLTravelGate.QuoteRequest quoteRequest = new XMLTravelGate.QuoteRequest();
                XMLTravelGate.CriteriaQuote criteriaQuote = new XMLTravelGate.CriteriaQuote();
                XMLTravelGate.Settings settings = new XMLTravelGate.Settings();
                settings.client = client;
                settings.timeout = Timeout;
                settings.context = SupplierCode;
                settings.testMode = false;
                if (SupplierCode.ToLower() == "wtetest")
                    settings.testMode = true;
                settings.auditTransactions = true;

                quoteRequest.settings = settings;
                criteriaQuote.optionRefId = optionRefId;
                quoteRequest.criteriaQuote = criteriaQuote;
                string RequestString = Newtonsoft.Json.JsonConvert.SerializeObject(quoteRequest);
                return RequestString;
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
            }
            return string.Empty;
        }
        //public GraphQLQuery_Response GetXTGResponse(ref string Error, string query, object variables, string Apikey, int TimeOut, HotelSearchParameter para = null, string Provider = "", bool DebugMode = false)
        //{
        //    GraphQLQuery_Response final = new GraphQLQuery_Response();
        //    System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
        //    timer.Start();


        //    try
        //    {
        //        var fullQuery = new GraphQLQuery()
        //        {
        //            query = query,
        //            variables = variables,
        //        };
        //        string jsonContent = JsonConvert.SerializeObject(fullQuery);

        //        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
        //        request.Method = "POST";
        //        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
        //        UTF8Encoding encoding = new UTF8Encoding();
        //        Byte[] byteArray = encoding.GetBytes(jsonContent.Trim());
        //        ServicePointManager.DefaultConnectionLimit = 1000;
        //        request.ContentLength = byteArray.Length;
        //        request.ContentType = @"application/json";
        //        request.Headers.Add("Accept-Encoding", "gzip");
        //        request.Timeout = TimeOut;
        //        request.Headers.Add("Authorization", "Apikey " + Apikey + "");
        //        using (Stream dataStream = request.GetRequestStream())
        //        {
        //            dataStream.Write(byteArray, 0, byteArray.Length);
        //        }
        //        try
        //        {
        //            HttpWebResponse Response = (HttpWebResponse)request.GetResponse();
        //            Stream responseStream = null;
        //            if (Response.Headers.Get("Content-Encoding") == "gzip")
        //                responseStream = new GZipStream(Response.GetResponseStream(), CompressionMode.Decompress);
        //            else
        //                responseStream = Response.GetResponseStream();
        //            //Read the output
        //            StreamReader Reader = new StreamReader(responseStream, Encoding.UTF8);
        //            string strOutput = Reader.ReadToEnd();
        //            responseStream.Close();
        //            Reader.Close();
        //            Response.Close();
        //            final.Response = strOutput;
        //            final.IsError = false;
        //            timer.Stop();
        //            if (para != null)
        //            {
        //                para.SupplierTime += ",SupplierName :" + Provider + ": " + "Execution Time :" + timer.Elapsed.ToString();
        //                if (DebugMode)
        //                    _commonMethod.XMLRequestResponseTrack(Convert.ToString(variables), strOutput, Provider, "Proper");
        //            }
        //            //using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
        //            //{
        //            //    length = response.ContentLength;
        //            //    using (Stream responseStream = response.GetResponseStream())
        //            //    {
        //            //        StreamReader reader = new StreamReader(responseStream, Encoding.UTF8);
        //            //        string json = reader.ReadToEnd();
        //            //        return json;
        //            //        // return new GraphQLQueryResult(json);
        //            //    }
        //            //}
        //        }
        //        catch (WebException ex)
        //        {
        //            if (ex.Response != null)
        //            {
        //                WebResponse errorResponse = ex.Response;
        //                using (Stream responseStream = errorResponse.GetResponseStream())
        //                {
        //                    StreamReader reader = new StreamReader(responseStream, Encoding.GetEncoding("utf-8"));
        //                    String errorText = reader.ReadToEnd();
        //                    //  Console.WriteLine(errorText);
        //                    final.Response = errorText;
        //                    final.IsError = true;
        //                    // return new GraphQLQueryResult(null, ex);
        //                }
        //            }
        //            else
        //            {
        //                final.Response = "timeout";
        //                final.IsError = true;
        //            }
        //            Error = final.Response + "#Message= " + ex.Message;
        //        }
        //    }
        //    catch (Exception ex)
        //    {

        //        _exception.Error(ex);
        //        final.Response = ex.Message;
        //        final.IsError = true;
        //    }

        //    return final;
        //}

        public async Task<CancellationDto> XMLXTG_Quote(TokenData tokenData, string SearchKey, int agentid, bool ForQuote, double CompanyBuy, string MealId, string RoomTypeId, string RoomTypeName, string language, string currencyCode, DataSet dsHotel, string ownsystemid, DataTable dtAgentDetail, DataTable dtSuplierMarkup, DataTable dtMarkupDetails, HotelSearchParameter SearchParam, DataRow[] XTGDetails, DataRow XTGChild, int SearchTimeout, string HotelIds, DataTable dtROE, string GiataId = "0", int hotelRating = 0)
        {
            string ISBooking = "Available";
            CancellationDto cancellationDto = new CancellationDto();
            string Error = "";
            string responsetype = "";
            string ResponseString = "";
            string RequestString = "";
            string SupplierName = "";
            try
            {
                //DataRow[] hotelXTG = dsHotel.Tables[0].Select("OwnsystemHotelId='" + ownsystemid + "'");
                // DataRow[] RoomXTG = dsHotel.Tables[1].Select("OwnsystemHotelId='" + ownsystemid + "' and RoomTypeId='" + RoomTypeId + "'");
                //DataRow[] RoomRateXTG = dsHotel.Tables[3].Select("OwnsystemHotelId='" + ownsystemid + "' and RoomTypeId='" + RoomTypeId + "'");
                DataTable dtXTGHotelRoom = _commonMethod.GenerateRoomtypeDataTableNewV2();
                DataTable dtXTGHotelRoomCacelationPolicy = _commonMethod.GenerateCancellationDataTableNewV2();
                DataTable dtXTGHotelRoomRateBreakup = _commonMethod.GenerateoomRateBreackUpDataTableNewV2();
                try
                {
                    string XTGSiteID = Convert.ToString(XTGDetails[0]["UserID"]);
                    string XTGApiKey = Convert.ToString(XTGDetails[0]["Password"]);
                    string XTGWebUrl = Convert.ToString(XTGDetails[0]["WebSiteURL"]);
                    SupplierName = Convert.ToString(XTGChild["SupplierName"]);
                    string SupplierCode = Convert.ToString(XTGChild["UserId"]);
                    string AccessCode = Convert.ToString(XTGChild["UserName"]);


                    string[] spitid = RoomTypeId.Split(new String[] { "$$" }, StringSplitOptions.None);
                    Double HoursBeforee = Convert.ToDouble(spitid[6]);
                    currencyCode = Convert.ToString(spitid[4]);
                    ISBooking = "NotAvailable";
                    //DataTable dtInrCurrency = Common.GetCountryCurrency(currencyCode);
                    double CurrencyFactor = 1;
                    if (currencyCode.ToLower() != tokenData.SiteCurrency.ToLower())
                    {
                        //DataTable dtInrCurrency = Common.GetCountryCurrency(currency);
                        CurrencyFactor = _commonMethod.GetROEByCurrency(currencyCode, dtROE);
                    }
                    double SubAgentMarkup = 0;
                    int IsParentId = 1;
                    if (dtAgentDetail != null)
                    {
                        IsParentId = Convert.ToInt32(dtAgentDetail.Rows[0]["IsParentId"]);
                        SubAgentMarkup = Convert.ToDouble(dtAgentDetail.Rows[0]["B2CMarkupPer"]);
                    }
                    double AgentPercentage = 0.0;
                    Boolean blnIsShowSupplier = false;
                    double AgentMarkupAmount = 0.0;
                    Boolean isPerce = true;
                    //Int32 hotelRating = 0;
                    //hotelRating = Convert.ToInt32(dsHotel.Tables[0].Rows[0]["Rating"]);
                    _markup.getAgetMarkupSetting(SupplierName, GiataId, dtSuplierMarkup, dtMarkupDetails, ref AgentPercentage, ref AgentMarkupAmount, ref blnIsShowSupplier, ref isPerce, agentid, hotelRating, SearchParam.CityID, SearchParam.CountryID.ToString(), SearchParam.CheckInDate);
                    //Common.getAgetMarkupSetting(SupplierName, ownsystemid, dtSuplierMarkup, dtMarkupDetails, ref AgentPercentage, ref AgentMarkupAmount, ref blnIsShowSupplier, ref isPerce);
                    double AgentCompanyPercentage = Convert.ToDouble(SearchParam.AgentCompanyPercentage);
                    bool IsB2C = Convert.ToBoolean(Convert.ToByte(SearchParam.IsB2BOrB2C));
                    double B2CMarkup = (IsB2C == false ? AgentCompanyPercentage : XMLHotelBookingCommon.B2CMarkup);
                    if (IsB2C == true)
                        AgentPercentage = 0;

                    double PriceNF = CompanyBuy;
                    double vatOnPriceNF = 0;
                    #region Vat
                    double vatOnPriceNFPriceNF = PriceNF;

                    PriceNF = _markup.ProcessHotelCancelationPrice_WithMarkup(ref vatOnPriceNFPriceNF, SearchParam.Rooms.Count * SearchParam.NoofNights, SearchParam.CityID, SearchParam.CountryID.ToString(), PriceNF, isPerce, B2CMarkup, AgentPercentage, AgentMarkupAmount, IsParentId, SubAgentMarkup, ref vatOnPriceNF);
                    #endregion



                    RequestString = GetQuoteRequest_XTG(RoomTypeId, XTGSiteID, SearchTimeout, SupplierCode);


                    if (RequestString == "")
                    {
                        dtXTGHotelRoomCacelationPolicy.Rows.Add(_commonMethod.setCancellationPolicyRow(ref CompanyBuy, dtXTGHotelRoomCacelationPolicy.NewRow(), CurrencyFactor, currencyCode, SupplierName, ownsystemid, Convert.ToString(1),
                                                       Convert.ToString(RoomTypeId), RoomTypeName, MealId, DateTime.Now.Date, DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null), vatOnPriceNFPriceNF, PriceNF, "", vatOnPriceNF, ownsystemid, "NO", ""));
                        dtXTGHotelRoomCacelationPolicy.AcceptChanges();
                    }

                    var client = new XMLTravelGate.GraphQLClient(XTGWebUrl);

                    //XMLTravelGate.GraphQLQuery_Response objResponse = new XMLTravelGate.GraphQLQuery_Response();
                    //objResponse = client.GetXTGResponse(ref Error, XMLTravelGate.GraphQLClient.XTG_Quote_Query, Newtonsoft.Json.Linq.JObject.Parse(RequestString), XTGApiKey, SearchTimeout, SearchParam);

                    XMLTravelGate.GQDto objResponse = new XMLTravelGate.GQDto();
                    objResponse = await client.GetXTGResponseAsync(XMLTravelGate.GraphQLClient.XTG_Quote_Query, Newtonsoft.Json.Linq.JObject.Parse(RequestString), XTGApiKey, SearchTimeout, SearchParam);
                    //objResponse = GetXTGResponse(ref Error, XMLTravelGate.GraphQLClient.XTG_Quote_Query, Newtonsoft.Json.Linq.JObject.Parse(RequestString), XTGApiKey, SearchTimeout, SearchParam);
                    _commonMethod.ThirdPartyGetPolicyHistory(Convert.ToString(RoomTypeId), Convert.ToString(MealId), RequestString, Convert.ToString(objResponse.GQRes.Response + objResponse.Error), "After Get Policy", SupplierName, "Response", SearchKey, "API");
                    if (objResponse.GQRes.IsError == true)
                    {
                        dtXTGHotelRoomCacelationPolicy.Rows.Add(_commonMethod.setCancellationPolicyRow(ref CompanyBuy, dtXTGHotelRoomCacelationPolicy.NewRow(), CurrencyFactor, currencyCode, SupplierName, ownsystemid, Convert.ToString(1),
                                                       Convert.ToString(RoomTypeId), RoomTypeName, MealId, DateTime.Now.Date, DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null), vatOnPriceNFPriceNF, PriceNF, "", vatOnPriceNF, ownsystemid, "NO", ""));
                        dtXTGHotelRoomCacelationPolicy.AcceptChanges();
                    }



                    try
                    {
                        ResponseString = Newtonsoft.Json.JsonConvert.DeserializeXmlNode(objResponse.GQRes.Response).InnerXml;

                    }
                    catch (Exception ex)
                    {
                        _ = _exception.ErrorNew(ex, _LogAPI.Recheck, SearchKey);
                    }
                    DataSet ds = _commonMethod.GetDataSetFromXMLString(ResponseString);

                    if ((ds.Tables["error"] != null && ds.Tables["error"].Rows.Count > 0) || ResponseString == "" || ds.Tables["optionQuote"] == null)
                    {
                        dtXTGHotelRoomCacelationPolicy.Rows.Add(_commonMethod.setCancellationPolicyRow(ref CompanyBuy, dtXTGHotelRoomCacelationPolicy.NewRow(), CurrencyFactor, currencyCode, SupplierName, ownsystemid, Convert.ToString(1),
                                                       Convert.ToString(RoomTypeId), RoomTypeName, MealId, DateTime.Now.Date, DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null), vatOnPriceNFPriceNF, PriceNF, "", vatOnPriceNF, ownsystemid, "NO", ""));
                        dtXTGHotelRoomCacelationPolicy.AcceptChanges();
                    }
                    string remarks = "";
                    if (ds.Tables["optionQuote"] != null && ds.Tables["optionQuote"].Rows.Count > 0)
                    {
                        string optionRefId = Convert.ToString(ds.Tables["optionQuote"].Rows[0]["optionRefId"]);
                        remarks = Convert.ToString(ds.Tables["optionQuote"].Rows[0]["remarks"]);

                        #region Added on 03-07-25
                        if (ds.Tables.Contains("surcharges") && ds.Tables["surcharges"].Rows.Count > 0)
                        {
                            //DataRow[] drcancelPolicy = ds.Tables["cancelPolicy"].Select("optionQuote_Id='" + 0 + "'");
                            for (int i = 0; i < ds.Tables["surcharges"].Rows.Count; i++)
                            {
                                string surcharges_Id = Convert.ToString(ds.Tables["surcharges"].Rows[i]["surcharges_Id"]);
                                string chargeType = Convert.ToString(ds.Tables["surcharges"].Rows[i]["chargeType"]);
                                if (chargeType.ToLower() == "exclude")
                                {
                                    DataRow[] surprices = ds.Tables["price"].Select("surcharges_Id='" + surcharges_Id + "'");
                                    if (surprices.Length > 0)
                                    {
                                        remarks += "<br/><br/>chargeType:" + chargeType + "<br/>description:" + Convert.ToString(ds.Tables["surcharges"].Rows[i]["description"]);
                                        remarks += "<br/> currency:" + Convert.ToString(surprices[0]["currency"]);
                                        // bool binding = Convert.ToBoolean(surprices[0]["binding"]);
                                        remarks += "<br/> net Price(Pay at Hotel):" + Convert.ToDouble(surprices[0]["net"]);
                                        //double gross = Convert.ToDouble(surprices[0]["gross"]);

                                    }
                                }
                            }
                        }
                        #endregion

                        double Price = Convert.ToDouble(ds.Tables["price"].Rows[0]["net"]) * CurrencyFactor;
                        double priceBuy = Price;
                        //if (Math.Round(Price, 2) > Math.Round(CompanyBuy, 2))
                        //    optionRefId = "";

                        #region Vat
                        double VatonPolicyTotal = 0;
                        double Main = Price;
                        double VatOnPolicy = 0;
                        double ChargeAmount = 0;
                        DateTime DeadLineDate = DateTime.Now;
                        DateTime curDate = _commonMethod.GetTimeZoneDate(SearchParam.CurrentDate);
                        int cutooff = -1;
                        cutooff = _commonMethod.GetCutOffDays(dtSuplierMarkup, SupplierName);
                        Price = _markup.ProcessHotelCancelationPrice_WithMarkup(ref Main, SearchParam.Rooms.Count * SearchParam.NoofNights, SearchParam.CityID, SearchParam.CountryID.ToString(), Price, isPerce, B2CMarkup, AgentPercentage, AgentMarkupAmount, IsParentId, SubAgentMarkup, ref VatonPolicyTotal);
                        #endregion
                        bool IsRefunable = Convert.ToBoolean(ds.Tables["cancelPolicy"].Rows[0]["refundable"]);
                        string checkin = (SearchParam.CheckInDate).Substring(6, 4) + "-" + (SearchParam.CheckInDate).Substring(3, 2) + "-" + (SearchParam.CheckInDate).Substring(0, 2);
                        string optionQuote_Id = Convert.ToString(ds.Tables["optionQuote"].Rows[0]["optionQuote_Id"]);// need to be checked
                        //ds.Tables["cancelPolicy"].Select("optionQuote_Id='" + 0 + "'");

                        if (ds.Tables.Contains("cancelPolicy"))
                        {
                            DataRow[] drRatePlanCancellationPolicyList = ds.Tables["cancelPolicy"].Select("optionQuote_Id='" + optionQuote_Id + "'");
                            if (drRatePlanCancellationPolicyList.Length > 0)
                            {
                                for (int c = 0; c < drRatePlanCancellationPolicyList.Length; c++)
                                {
                                    if (ds.Tables.Contains("cancelPolicy"))
                                    {
                                        if (ds.Tables.Contains("cancelPenalties"))
                                        {
                                            if (Convert.ToDouble(ds.Tables["cancelPenalties"].Rows[0]["hoursBefore"]) == HoursBeforee)
                                            {
                                                DataRow[] cancelPenalties = ds.Tables["cancelPenalties"].Select("cancelPolicy_Id='" + drRatePlanCancellationPolicyList[c][1] + "'");
                                                //Double HoursBefore = Convert.ToDouble(ds.Tables["cancelPenalties"].Rows[0]["hoursBefore"]);
                                                if (cancelPenalties.Length > 0)
                                                {
                                                    bool isRefundable = Convert.ToBoolean(drRatePlanCancellationPolicyList[c][0]);

                                                    if (isRefundable)
                                                    {
                                                        if (cancelPenalties[c][0] != DBNull.Value)
                                                        {
                                                            DeadLineDate = Convert.ToDateTime(checkin).AddHours(-Convert.ToDouble(cancelPenalties[c][0]));
                                                        }
                                                        else
                                                        {
                                                            RoomTypeName += " [Non Refundable]";
                                                        }
                                                    }
                                                    else
                                                    {
                                                        RoomTypeName += " [Non Refundable]";
                                                    }

                                                    //#endregion
                                                    if (DeadLineDate.AddDays(cutooff).Date > DateTime.Now.Date)
                                                        DeadLineDate = DeadLineDate.AddDays(cutooff).Date;
                                                    else
                                                        DeadLineDate = DateTime.Now.Date;
                                                    if (curDate.Date < DeadLineDate.Date)
                                                    {
                                                        if (c == 0)
                                                        {
                                                            double Zero = 0;
                                                            ISBooking = "Available";
                                                            dtXTGHotelRoomCacelationPolicy.Rows.Add(_commonMethod.setCancellationPolicyRow(ref Zero, dtXTGHotelRoomCacelationPolicy.NewRow(), CurrencyFactor, currencyCode, SupplierName, ownsystemid, Convert.ToString(1)
    , Convert.ToString(RoomTypeId), RoomTypeName, MealId, curDate, DeadLineDate.AddDays(-1).AddHours(23).AddMinutes(59).AddSeconds(59), 0, 0, "", 0, "", "NO", Convert.ToString(DeadLineDate.Date), remarks));
                                                            dtXTGHotelRoomCacelationPolicy.AcceptChanges();
                                                        }
                                                        ISBooking = "Available";
                                                        dtXTGHotelRoomCacelationPolicy.Rows.Add(_commonMethod.setCancellationPolicyRow(ref priceBuy, dtXTGHotelRoomCacelationPolicy.NewRow(), CurrencyFactor, currencyCode, SupplierName, ownsystemid, Convert.ToString(1)
    , Convert.ToString(RoomTypeId), RoomTypeName, MealId, DeadLineDate, DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null), Main, Price, "", 0, "", "Yes", Convert.ToString(DeadLineDate.Date), remarks));
                                                        dtXTGHotelRoomCacelationPolicy.AcceptChanges();
                                                    }
                                                    else
                                                    {
                                                        ISBooking = "Available";
                                                        dtXTGHotelRoomCacelationPolicy.Rows.Add(_commonMethod.setCancellationPolicyRow(ref priceBuy, dtXTGHotelRoomCacelationPolicy.NewRow(), CurrencyFactor, currencyCode, SupplierName, ownsystemid, Convert.ToString(1)
    , Convert.ToString(RoomTypeId), RoomTypeName, MealId, curDate, DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null), Main, Price, "", 0, "", "Yes", Convert.ToString(DeadLineDate.Date), remarks));
                                                        dtXTGHotelRoomCacelationPolicy.AcceptChanges();
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                ISBooking = "NotAvailable";

                                                cancellationDto.dsHotelData = null;
                                                cancellationDto.ISBooking = ISBooking;

                                                return cancellationDto;
                                            }
                                        }
                                        else
                                        {
                                            RoomTypeName += " [Non Refundable]";
                                            ISBooking = "Available";
                                            dtXTGHotelRoomCacelationPolicy.Rows.Add(_commonMethod.setCancellationPolicyRow(ref priceBuy, dtXTGHotelRoomCacelationPolicy.NewRow(), CurrencyFactor, currencyCode, SupplierName, ownsystemid, Convert.ToString(1)
, Convert.ToString(RoomTypeId), RoomTypeName, MealId, curDate, DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null), Main, Price, "", 0, "", "Yes", Convert.ToString(DeadLineDate.Date), remarks));
                                            dtXTGHotelRoomCacelationPolicy.AcceptChanges();
                                        }
                                    }
                                    else
                                    {
                                        RoomTypeName += " [Non Refundable]";
                                        ISBooking = "Available";
                                        dtXTGHotelRoomCacelationPolicy.Rows.Add(_commonMethod.setCancellationPolicyRow(ref priceBuy, dtXTGHotelRoomCacelationPolicy.NewRow(), CurrencyFactor, currencyCode, SupplierName, ownsystemid, Convert.ToString(1)
, Convert.ToString(RoomTypeId), RoomTypeName, MealId, curDate, DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null), Main, Price, "", 0, "", "Yes", Convert.ToString(DeadLineDate.Date), remarks));
                                        dtXTGHotelRoomCacelationPolicy.AcceptChanges();
                                    }
                                }
                            }
                        }
                        else
                        {
                            RoomTypeName += " [Non Refundable]";
                            ISBooking = "Available";
                            dtXTGHotelRoomCacelationPolicy.Rows.Add(_commonMethod.setCancellationPolicyRow(ref priceBuy, dtXTGHotelRoomCacelationPolicy.NewRow(), CurrencyFactor, currencyCode, SupplierName, ownsystemid, Convert.ToString(1)
, Convert.ToString(RoomTypeId), "", MealId, curDate, DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null), Main, Price, "", 0, "", "Yes", Convert.ToString(DeadLineDate.Date), remarks));
                            dtXTGHotelRoomCacelationPolicy.AcceptChanges();
                        }


                    }
                    else //make non ref
                    {
                        ISBooking = "NotAvailable";
                        dtXTGHotelRoomCacelationPolicy.Rows.Add(_commonMethod.setCancellationPolicyRow(ref CompanyBuy, dtXTGHotelRoomCacelationPolicy.NewRow(), CurrencyFactor, currencyCode, SupplierName, ownsystemid, Convert.ToString(1),
                                                        Convert.ToString(RoomTypeId), "", MealId, DateTime.Now.Date, DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null), vatOnPriceNFPriceNF, PriceNF, "", vatOnPriceNF, ownsystemid, "NO", "", remarks));
                        dtXTGHotelRoomCacelationPolicy.AcceptChanges();

                    }


                }
                catch (Exception ex)
                {
                    try
                    {
                        ISBooking = "NotAvailable";
                        responsetype = "GeneralError";
                        //int AgentId = Convert.ToInt32(Common.getSession("UserId") == "" ? "0" : Common.getSession("UserId"));
                        string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", agentid, Convert.ToString(DateTime.Now.Date), responsetype, RequestString, ResponseString, Error + ex.Message);
                    }
                    catch (Exception e)
                    {
                        _ = _exception.ErrorNew(e, _LogAPI.Recheck, SearchKey);
                    }
                    _ = _exception.ErrorNew(ex, _LogAPI.Recheck, SearchKey);

                    cancellationDto.dsHotelData = null;
                    cancellationDto.ISBooking = ISBooking;

                    return cancellationDto;
                }
                dsHotel.Tables[2].Merge(dtXTGHotelRoomCacelationPolicy);
                try
                {
                    responsetype = "Success";
                    ISBooking = "Available";
                    string abc = await _commonMethod.SupplierPerformanceReportForRecheckOnly(SupplierName, "PreBook", agentid, Convert.ToString(DateTime.Now.Date), responsetype, RequestString, ResponseString, Error);
                }
                catch (Exception e)
                {
                    _ = _exception.ErrorNew(e, _LogAPI.Recheck, SearchKey);
                }


                cancellationDto.dsHotelData = dsHotel;
                cancellationDto.ISBooking = ISBooking;

                return cancellationDto;

            }
            catch (Exception ex)
            {
                try
                {
                    ISBooking = "NotAvailable";
                    responsetype = "GeneralError";
                    //int AgentId = Convert.ToInt32(Common.getSession("UserId") == "" ? "0" : Common.getSession("UserId"));
                    string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", agentid, Convert.ToString(DateTime.Now.Date), responsetype, RequestString, ResponseString, Error + ex.Message);
                }
                catch (Exception e)
                {
                    _ = _exception.ErrorNew(e, _LogAPI.Recheck, SearchKey);
                }
                _ = _exception.ErrorNew(ex, _LogAPI.Recheck, SearchKey);

                cancellationDto.dsHotelData = null;
                cancellationDto.ISBooking = ISBooking;

                return cancellationDto;
            }
        }

        public async Task<string> GetXTGBookingRequest(DataTable dtPassengers, DataSet dsCartDetails, string internalRef, string client, int Timeout, string SupplierCode, string UniqueNo, string AgentUniqueNo, string XTGSiteID, string XTGWebUrl, string XTGApiKey,TokenData tokenData)
        {
            try
            {

                string OptionRefId = Convert.ToString(dsCartDetails.Tables[1].DefaultView[0]["purchaseToken"]);
                #region "Price CHeck Before booking"
                try
                {
                    string ownroomtypeid = Convert.ToString(dsCartDetails.Tables[1].DefaultView[0]["RoomTypeId"]);
                    string[] spitid = ownroomtypeid.Split(new String[] { "$$" }, StringSplitOptions.None);
                    string RoomTypeId = spitid[0].ToString();
                    string Currency = spitid[4].ToString();
                    string Error = "";
                    string RequestStringQuote = GetQuoteRequest_XTG(RoomTypeId, XTGSiteID, 60000, SupplierCode);
                    _commonMethod.ThirdPartyBookingHistory(Convert.ToString(UniqueNo), Convert.ToString(AgentUniqueNo), RequestStringQuote, "-", "Before Get Price Request hit booking", Convert.ToString(dsCartDetails.Tables[1].DefaultView[0]["SupplierName"]).ToLower(), "Response Confirm", "-", "API Out");
                    var clientXTG = new XMLTravelGate.GraphQLClient(XTGWebUrl);

                    GraphQLQuery_Response objResponse = new GraphQLQuery_Response();
                    //XMLTravelGate.GraphQLQuery_Response objResponse = new XMLTravelGate.GraphQLQuery_Response();
                    objResponse = GetXTGResponse(ref Error, XTGWebUrl, XMLTravelGate.GraphQLClient.XTG_Quote_Query, Newtonsoft.Json.Linq.JObject.Parse(RequestStringQuote), XTGApiKey, Timeout);

                    _commonMethod.ThirdPartyBookingHistory(Convert.ToString(UniqueNo), Convert.ToString(AgentUniqueNo), RequestStringQuote, objResponse.Response, "After Get Price Request hit booking ", Convert.ToString(dsCartDetails.Tables[1].DefaultView[0]["SupplierName"]).ToLower(), "Response Confirm", "-", "API Out");
                    if (objResponse.IsError == true)
                    {
                    }
                    else
                    {
                        DataSet ResponseDataXTG = null;
                        try
                        {
                            string ResponseString = Newtonsoft.Json.JsonConvert.DeserializeXmlNode(objResponse.Response).InnerXml;
                            ResponseDataXTG = _commonMethod.GetDataSetFromXMLString(ResponseString);
                        }
                        catch (Exception ex)
                        {
                            _ = _exception.ErrorNew(ex, _LogAPI.Book);
                        }
                        if (ResponseDataXTG == null || (ResponseDataXTG.Tables["error"] != null && ResponseDataXTG.Tables["error"].Rows.Count > 0) || ResponseDataXTG.Tables["optionQuote"] == null)
                        {

                        }
                        else
                        {
                            if (ResponseDataXTG.Tables["optionQuote"].Rows.Count > 0)
                            {
                                //DataTable dtInrCurrencyXTG = 1;// Common.GetCountryCurrency(XMLTravelGate.XTG_Currency);
                                double CurrencyFactor = 1;// Convert.ToDouble(dtInrCurrencyXTG.Rows[0]["AmtToExch"]);
                                if (Currency.ToLower() != tokenData.SiteCurrency.ToLower())
                                {
                                    //DataTable dtInrCurrencyXTG = _commonMethod.GetCountryCurrency(Currency);
                                    //CurrencyFactor = Convert.ToDouble(dtInrCurrencyXTG.Rows[0]["AmtToExch"]);

                                    CurrencyFactor = _commonMethod.GetROEByCurrency(Currency, tokenData.dtROE);
                                }
                                double Price = Convert.ToDouble(ResponseDataXTG.Tables["price"].Rows[0]["net"]) * CurrencyFactor;
                                if (Math.Round(Price, 2) > Math.Round(Convert.ToDouble(dsCartDetails.Tables[1].DefaultView[0]["CompanyBuyingPrice"]), 2))
                                {
                                    OptionRefId = "";
                                }

                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _ = _exception.ErrorNew(ex,_LogAPI.Book);

                }
                #endregion
                #region Booking Req_Old
                //XMLTravelGate.XTG_BookRQ xTG_BookRQ = new XMLTravelGate.XTG_BookRQ();
                //XMLTravelGate.Settings settings = new XMLTravelGate.Settings();
                //settings.client = client;
                //settings.timeout = Timeout;
                //settings.auditTransactions = true;
                //settings.context = SupplierCode;

                //xTG_BookRQ.settings = settings;
                //XMLTravelGate.BookInput bookInput = new XMLTravelGate.BookInput();
                //bookInput.clientReference = internalRef;
                //bookInput.optionRefId = OptionRefId;
                ////deltaprice
                //XMLTravelGate.DeltaPrice deltaPrice = new XMLTravelGate.DeltaPrice();
                //deltaPrice.amount = 0;
                //deltaPrice.percent = 0;
                //deltaPrice.applyBoth = true;
                ////end deltaprice
                ////holder
                //XMLTravelGate.Holder holder = new XMLTravelGate.Holder();
                //dtPassengers.DefaultView.RowFilter = "PickUp='" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[0]["RoomTypeId"]) + "' and UserCartId=" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[0]["UserCartID"]);
                //dtPassengers.DefaultView.Sort = "IsLead desc";
                //holder.name = Convert.ToString(dtPassengers.DefaultView[0]["FirstName"]).Trim();
                //holder.surname = Convert.ToString(dtPassengers.DefaultView[0]["LastName"]).Trim();

                ////

                ////rooms
                //List<XMLTravelGate.Room> roomlist = new List<XMLTravelGate.Room>();
                //for (int rm = 0; rm < dsCartDetails.Tables[1].DefaultView.Count; rm++)
                //{
                //    dtPassengers.DefaultView.RowFilter = "RoomNo='" + (rm + 1) + "' and PickUp='" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[rm]["RoomTypeId"]) + "' and UserCartId=" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[rm]["UserCartID"]);

                //    dtPassengers.DefaultView.Sort = "IsLead desc";

                //    XMLTravelGate.Room room = new XMLTravelGate.Room();
                //    room.occupancyRefId = (rm + 1);
                //    List<XMLTravelGate.Paxbook> paxesList = new List<XMLTravelGate.Paxbook>();

                //    for (int j = 0; j < dtPassengers.DefaultView.Count; j++)
                //    {
                //        XMLTravelGate.Paxbook paxes = new XMLTravelGate.Paxbook();
                //        paxes.name = Convert.ToString(dtPassengers.DefaultView[j]["FirstName"]).Trim();
                //        paxes.surname = Convert.ToString(dtPassengers.DefaultView[j]["LastName"]).Trim();
                //        paxes.age = (Convert.ToString(dtPassengers.DefaultView[j]["PaxType"]).ToLower() == "child" ? Convert.ToInt32(dtPassengers.DefaultView[j]["ChildAge"]) : XMLTravelGate.AdultAge);
                //        paxesList.Add(paxes);
                //    }
                //    room.paxes = paxesList;
                //    roomlist.Add(room);
                //}
                ////Loop Passengers

                ////end loop

                //bookInput.deltaPrice = deltaPrice;
                //bookInput.rooms = roomlist;
                //bookInput.holder = holder;
                //xTG_BookRQ.BookInput = bookInput;
                //string RequestString = JsonConvert.SerializeObject(xTG_BookRQ);
                #endregion
                #region Booking Req
                XMLTravelGate.XTG_BookRQ xTG_BookRQ = new XMLTravelGate.XTG_BookRQ();
                XMLTravelGate.Settings settings = new XMLTravelGate.Settings();
                settings.client = client;
                settings.timeout = Timeout;
                settings.auditTransactions = true;
                settings.context = SupplierCode;
                settings.testMode = false;
                if (SupplierCode.ToLower() == "wtetest")
                    settings.testMode = true;
                xTG_BookRQ.settings = settings;
                XMLTravelGate.BookInput bookInput = new XMLTravelGate.BookInput();
                bookInput.clientReference = internalRef;
                bookInput.optionRefId = OptionRefId;
                //deltaprice
                XMLTravelGate.DeltaPrice deltaPrice = new XMLTravelGate.DeltaPrice();
                deltaPrice.amount = 0;
                deltaPrice.percent = 0;
                deltaPrice.applyBoth = true;
                //end deltaprice
                //holder
                XMLTravelGate.Holder holder = new XMLTravelGate.Holder();
                dtPassengers.DefaultView.RowFilter = "PickUp='" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[0]["RoomTypeId"]) + "' and UserCartId=" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[0]["UserCartID"]);
                dtPassengers.DefaultView.Sort = "IsLead desc";
                holder.title = Convert.ToString(dtPassengers.DefaultView[0]["Prefix"]).Trim().Replace(".", "").ToUpper();
                holder.name = Convert.ToString(dtPassengers.DefaultView[0]["FirstName"]).Trim();
                holder.surname = Convert.ToString(dtPassengers.DefaultView[0]["LastName"]).Trim();

                //

                //rooms
                List<XMLTravelGate.Room> roomlist = new List<XMLTravelGate.Room>();
                for (int rm = 0; rm < dsCartDetails.Tables[1].DefaultView.Count; rm++)
                {
                    dtPassengers.DefaultView.RowFilter = "RoomNo='" + (rm + 1) + "' and PickUp='" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[rm]["RoomTypeId"]) + "' and UserCartId=" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[rm]["UserCartID"]);

                    dtPassengers.DefaultView.Sort = "IsLead desc";

                    XMLTravelGate.Room room = new XMLTravelGate.Room();
                    room.occupancyRefId = (rm + 1);
                    List<XMLTravelGate.Paxbook> paxesList = new List<XMLTravelGate.Paxbook>();

                    for (int j = 0; j < dtPassengers.DefaultView.Count; j++)
                    {
                        XMLTravelGate.Paxbook paxes = new XMLTravelGate.Paxbook();
                        paxes.name = Convert.ToString(dtPassengers.DefaultView[j]["FirstName"]).Trim();
                        paxes.surname = Convert.ToString(dtPassengers.DefaultView[j]["LastName"]).Trim();
                        paxes.age = (Convert.ToString(dtPassengers.DefaultView[j]["PaxType"]).ToLower() == "child" ? Convert.ToInt32(dtPassengers.DefaultView[j]["ChildAge"]) : XMLTravelGate.AdultAge);
                        paxes.title = Convert.ToString(dtPassengers.DefaultView[j]["Prefix"]).Trim().Replace(".", "").ToUpper();
                        paxesList.Add(paxes);
                    }
                    room.paxes = paxesList;
                    roomlist.Add(room);
                }
                //Loop Passengers

                //end loop

                bookInput.deltaPrice = deltaPrice;
                bookInput.rooms = roomlist;
                bookInput.holder = holder;
                xTG_BookRQ.BookInput = bookInput;
                string RequestString = Newtonsoft.Json.JsonConvert.SerializeObject(xTG_BookRQ);
                #endregion
                return RequestString;


            }
            catch (Exception ex)
            {
                _ = _exception.ErrorNew(ex, _LogAPI.Book);
            }
            return string.Empty;
        }
        public async Task<string> GetXTGBookingRequest_newversion(DataTable dtPassengers, BookRequest_newversion bookRequest, string internalRef, string client, int Timeout, string SupplierCode, string UniqueNo, string AgentUniqueNo, string XTGSiteID, string XTGWebUrl, string XTGApiKey, TokenData tokenData)
        {
            try
            {

                string OptionRefId = Convert.ToString(bookRequest.userCart.UserCartDetails[0].PurchaseToken);
                #region "Price CHeck Before booking"
                try
                {
                    string ownroomtypeid = Convert.ToString(bookRequest.userCart.UserCartDetails[0].RoomTypeId);
                    string[] spitid = ownroomtypeid.Split(new String[] { "$$" }, StringSplitOptions.None);
                    string RoomTypeId = spitid[0].ToString();
                    string Currency = spitid[4].ToString();
                    string Error = "";
                    string RequestStringQuote = GetQuoteRequest_XTG(RoomTypeId, XTGSiteID, 60000, SupplierCode);
                    _commonMethod.ThirdPartyBookingHistory(Convert.ToString(UniqueNo), Convert.ToString(AgentUniqueNo), RequestStringQuote, "-", "Before Get Price Request hit booking", Convert.ToString(bookRequest.userCart.UserCartDetails[0].SupplierName).ToLower(), "Response Confirm", "-", "API Out");
                    var clientXTG = new XMLTravelGate.GraphQLClient(XTGWebUrl);

                    GraphQLQuery_Response objResponse = new GraphQLQuery_Response();
                    //XMLTravelGate.GraphQLQuery_Response objResponse = new XMLTravelGate.GraphQLQuery_Response();
                    objResponse = GetXTGResponse(ref Error, XTGWebUrl, XMLTravelGate.GraphQLClient.XTG_Quote_Query, Newtonsoft.Json.Linq.JObject.Parse(RequestStringQuote), XTGApiKey, Timeout);

                    _commonMethod.ThirdPartyBookingHistory(Convert.ToString(UniqueNo), Convert.ToString(AgentUniqueNo), RequestStringQuote, objResponse.Response, "After Get Price Request hit booking ", Convert.ToString(bookRequest.userCart.UserCartDetails[0].SupplierName).ToLower(), "Response Confirm", "-", "API Out");
                    if (objResponse.IsError == true)
                    {
                    }
                    else
                    {
                        DataSet ResponseDataXTG = null;
                        try
                        {
                            string ResponseString = Newtonsoft.Json.JsonConvert.DeserializeXmlNode(objResponse.Response).InnerXml;
                            ResponseDataXTG = _commonMethod.GetDataSetFromXMLString(ResponseString);
                        }
                        catch (Exception ex)
                        {
                            _ = _exception.ErrorNew(ex, _LogAPI.Book);
                        }
                        if (ResponseDataXTG == null || (ResponseDataXTG.Tables["error"] != null && ResponseDataXTG.Tables["error"].Rows.Count > 0) || ResponseDataXTG.Tables["optionQuote"] == null)
                        {

                        }
                        else
                        {
                            if (ResponseDataXTG.Tables["optionQuote"].Rows.Count > 0)
                            {
                                //DataTable dtInrCurrencyXTG = 1;// Common.GetCountryCurrency(XMLTravelGate.XTG_Currency);
                                double CurrencyFactor = 1;// Convert.ToDouble(dtInrCurrencyXTG.Rows[0]["AmtToExch"]);
                                if (Currency.ToLower() != tokenData.SiteCurrency.ToLower())
                                {
                                    //DataTable dtInrCurrencyXTG = _commonMethod.GetCountryCurrency(Currency);
                                    //CurrencyFactor = Convert.ToDouble(dtInrCurrencyXTG.Rows[0]["AmtToExch"]);

                                    CurrencyFactor = _commonMethod.GetROEByCurrency(Currency, tokenData.dtROE);
                                }
                                double Price = Convert.ToDouble(ResponseDataXTG.Tables["price"].Rows[0]["net"]) * CurrencyFactor;
                               // if (Math.Round(Price, 2) > Math.Round(Convert.ToDouble(dsCartDetails.Tables[1].DefaultView[0]["CompanyBuyingPrice"]), 2))
                                if (Math.Round(Price, 2) > Math.Round(Convert.ToDouble(bookRequest.userCart.UserCartDetails[0].BaseServiceBuyingAmount), 2))
                                {
                                    OptionRefId = "";
                                }

                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _ = _exception.ErrorNew(ex, _LogAPI.Book);

                }
                #endregion
                #region Booking Req_Old
                
                #endregion
                #region Booking Req
                XMLTravelGate.XTG_BookRQ xTG_BookRQ = new XMLTravelGate.XTG_BookRQ();
                XMLTravelGate.Settings settings = new XMLTravelGate.Settings();
                settings.client = client;
                settings.timeout = Timeout;
                settings.auditTransactions = true;
                settings.context = SupplierCode;
                settings.testMode = false;
                if (SupplierCode.ToLower() == "wtetest")
                    settings.testMode = true;
                xTG_BookRQ.settings = settings;
                XMLTravelGate.BookInput bookInput = new XMLTravelGate.BookInput();
                bookInput.clientReference = internalRef;
                bookInput.optionRefId = OptionRefId;
                //deltaprice
                XMLTravelGate.DeltaPrice deltaPrice = new XMLTravelGate.DeltaPrice();
                deltaPrice.amount = 0;
                deltaPrice.percent = 0;
                deltaPrice.applyBoth = true;
                //end deltaprice
                //holder
                XMLTravelGate.Holder holder = new XMLTravelGate.Holder();
                dtPassengers.DefaultView.RowFilter = "PickUp='" + Convert.ToString(bookRequest.userCart.UserCartDetails[0].RoomTypeId) + "' and UserCartId=" + Convert.ToString(bookRequest.userCart.UserCartDetails[0].UserCartId);
                dtPassengers.DefaultView.Sort = "IsLead desc";
                holder.title = Convert.ToString(dtPassengers.DefaultView[0]["Prefix"]).Trim().Replace(".", "").ToUpper();
                holder.name = Convert.ToString(dtPassengers.DefaultView[0]["FirstName"]).Trim();
                holder.surname = Convert.ToString(dtPassengers.DefaultView[0]["LastName"]).Trim();

                //

                //rooms
                List<XMLTravelGate.Room> roomlist = new List<XMLTravelGate.Room>();
                for (int rm = 0; rm < bookRequest.userCart.UserCartDetails.Count; rm++)
                {
                    dtPassengers.DefaultView.RowFilter = "RoomNo='" + (rm + 1) + "' and PickUp='" + Convert.ToString(bookRequest.userCart.UserCartDetails[rm].RoomTypeId) + "' and UserCartId=" + Convert.ToString(bookRequest.userCart.UserCartDetails[rm].UserCartId);

                    dtPassengers.DefaultView.Sort = "IsLead desc";

                    XMLTravelGate.Room room = new XMLTravelGate.Room();
                    room.occupancyRefId = (rm + 1);
                    List<XMLTravelGate.Paxbook> paxesList = new List<XMLTravelGate.Paxbook>();

                    for (int j = 0; j < dtPassengers.DefaultView.Count; j++)
                    {
                        XMLTravelGate.Paxbook paxes = new XMLTravelGate.Paxbook();
                        paxes.name = Convert.ToString(dtPassengers.DefaultView[j]["FirstName"]).Trim();
                        paxes.surname = Convert.ToString(dtPassengers.DefaultView[j]["LastName"]).Trim();
                        paxes.age = (Convert.ToString(dtPassengers.DefaultView[j]["PaxType"]).ToLower() == "child" ? Convert.ToInt32(dtPassengers.DefaultView[j]["ChildAge"]) : XMLTravelGate.AdultAge);
                        paxes.title = Convert.ToString(dtPassengers.DefaultView[j]["Prefix"]).Trim().Replace(".", "").ToUpper();
                        paxesList.Add(paxes);
                    }
                    room.paxes = paxesList;
                    roomlist.Add(room);
                }
                //Loop Passengers

                //end loop

                bookInput.deltaPrice = deltaPrice;
                bookInput.rooms = roomlist;
                bookInput.holder = holder;
                xTG_BookRQ.BookInput = bookInput;
                string RequestString = Newtonsoft.Json.JsonConvert.SerializeObject(xTG_BookRQ);
                #endregion
                return RequestString;


            }
            catch (Exception ex)
            {
                _ = _exception.ErrorNew(ex, _LogAPI.Book);
            }
            return string.Empty;
        }

        public void GetXTGBookingByReference(ref string NewReference, ref int BookingStatus, string UniqNo, string SupplierName, string PnrNo, bool IsByOurReference, string Reference, string xtgclient, string XTGURL, string ApikEy, string context, string AccessCode, string hotelCode, string currency,int AgentId)
        {
            BookingStatus = 1; NewReference = "";
            string Error = "";
            try
            {

                XTG_ReadRQ rq = new XTG_ReadRQ();

                SettingsRead settings = new SettingsRead();
                settings.client = xtgclient;
                settings.auditTransactions = true;
                settings.context = context;
                settings.testMode = false;

                CriteriaBookingRead criteriaBookingRead = new CriteriaBookingRead();
                References references = new References();

                List<Reference> lst = new List<Reference>();

                Reference referenceRead = new Reference();
                if (IsByOurReference)
                {
                    referenceRead.client = Reference;
                    referenceRead.supplier = "";
                }
                else
                {
                    referenceRead.client = "";
                    referenceRead.supplier = Reference;
                }
                lst.Add(referenceRead);

                references.hotelCode = hotelCode;
                references.currency = currency;
                references.references = lst;


                criteriaBookingRead.accessCode = AccessCode;
                criteriaBookingRead.language = "en";
                criteriaBookingRead.typeSearch = "REFERENCES";
                criteriaBookingRead.references = references;

                rq.criteriaBookingRead = criteriaBookingRead;
                rq.settings = settings;
                string RequestString = Newtonsoft.Json.JsonConvert.SerializeObject(rq);
                if (IsByOurReference)
                    _commonMethod.ThirdPartyBookingHistory(IsByOurReference ? UniqNo : Reference, Convert.ToString(AgentId), Convert.ToString(RequestString), "", "before Request hit Booking Read", SupplierName, "Request Confirm", PnrNo, "WithinEarth");
                else
                    _commonMethod.ThirdPartyCancellationHistory(IsByOurReference ? UniqNo : Reference, Convert.ToString(AgentId), Convert.ToString(RequestString), "", "before Request hit Booking Read", SupplierName, "Request Confirm", PnrNo, "WithinEarth");

                var client = new XMLTravelGate.GraphQLClient(XTGURL);
                XMLTravelGate.GraphQLQuery_Response objResponse = new XMLTravelGate.GraphQLQuery_Response();
                objResponse = client.GetXTGResponse(ref Error, XMLTravelGate.GraphQLClient.XTG_ReadQuery, Newtonsoft.Json.Linq.JObject.Parse(RequestString), ApikEy, Convert.ToInt32(180000));
                //(ref string Error, string query, object variables, string Apikey, int TimeOut, HotelSearchParameter para = null, string Provider = "", bool DebugMode = false)
                //(ref Error, XMLTravelGate.GraphQLClient.XTG_Quote_Query, Newtonsoft.Json.Linq.JObject.Parse(RequestString), XTGApiKey, Convert.ToInt32(180000));
                if (IsByOurReference)
                    _commonMethod.ThirdPartyBookingHistory(IsByOurReference ? UniqNo : Reference, Convert.ToString(AgentId), Convert.ToString(RequestString), objResponse.IsError + " Response =" + objResponse.Response, "after Request hit Booking Read", SupplierName, "Request Confirm", PnrNo, "WithinEarth");
                else
                    _commonMethod.ThirdPartyCancellationHistory(IsByOurReference ? UniqNo : Reference, Convert.ToString(AgentId), Convert.ToString(RequestString), objResponse.IsError + " Response =" + objResponse.Response, "after Request hit Booking Read", SupplierName, "Request Confirm", PnrNo, "WithinEarth");

                if (objResponse.IsError != true && (objResponse.Response != ""))
                {
                    BookingByInternalID responseBookingList = new BookingByInternalID();
                    try
                    {
                        responseBookingList = Newtonsoft.Json.JsonConvert.DeserializeObject<BookingByInternalID>(objResponse.Response);
                    }
                    catch (Exception ex)
                    {
                        _ = _exception.Error(ex);
                        //return "";
                    }
                    if (responseBookingList.data.hotelX.booking.bookings != null && responseBookingList.data.hotelX.booking.bookings.Count > 0)
                    {
                        int b = responseBookingList.data.hotelX.booking.bookings.Count;
                        for (int bk = 0; bk < b; bk++)
                        {
                            if (IsByOurReference)
                            {
                                if (Convert.ToString(responseBookingList.data.hotelX.booking.bookings[bk].reference.client).ToLower() == Reference.ToString().ToLower())
                                {
                                    NewReference = Convert.ToString(responseBookingList.data.hotelX.booking.bookings[bk].reference.supplier);
                                }
                            }
                            string Status = Convert.ToString(responseBookingList.data.hotelX.booking.bookings[bk].status.ToUpper());
                            switch (Status.ToUpper())
                            {
                                case "ok":
                                    BookingStatus = 6;
                                    break;
                                case "ko":
                                    BookingStatus = 2;
                                    break;
                                case "CANCELLED":
                                    BookingStatus = IsByOurReference ? 2 : 3;
                                    break;
                                case "UNKNOWN":
                                    BookingStatus = 1;
                                    break;
                                default:
                                    break;
                            }
                        }
                    }


                }

            }
            catch (Exception ex)
            {
                _=_exception.ErrorNew(ex, _LogAPI.CancelBooking);
                //return "";
            }
        }
    }
}
