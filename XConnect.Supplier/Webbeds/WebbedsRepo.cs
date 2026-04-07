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
using Options = XConnect.Models.Options;
using XConnect.Supplier.Webbeds.Interface;
using static Azure.Core.HttpHeader;
using static XConnect.Models.ErrorLog;
using ZstdSharp.Unsafe;
using System.Net;

namespace XConnect.Supplier.Webbeds
{
    public class WebbedsRepo : IWebbeds
    {
        private readonly ICommonMethod _commonMethod;
        private readonly IException _exception;
        private readonly IMarkup _markup;
        private readonly ISupplierRes _supplierRes;
        private readonly KeySettings _keySettings;

        public WebbedsRepo(ICommonMethod commonMethod, IException exception, IMarkup markup, ISupplierRes supplierRes, IOptions<KeySettings> keySettings)
        {
            _commonMethod = commonMethod;
            _exception = exception;
            _markup = markup;
            _supplierRes = supplierRes;
            _keySettings = keySettings.Value;
        }

        public void GetDotwRequestOnly_Async(DataRow[] commonCode, List<StarRating> listRating, HotelSearchParameter SearchPara, string XMLReq, List<SupplierDetails> DOTWDetails, List<SupplierSearchConfig> DOTWSearchConfig, DataTable dtPreferredDataHotel, ref ConcurrentBag<HotelX> AllTask, string xmlCity, string xmlCountry, string XmlNationality, int Hit, DataTable dtROE, TokenData tokenData)
        {


            try
            {
                string Ids = string.Empty;
                string xmlbase = "";
                _commonMethod.GetPrefferedHotelCodes(ref Ids, ref xmlbase, ref DOTWSearchConfig, "dotw", dtPreferredDataHotel, SearchPara.IsSearchFor);
                #region "DOTW"
                if (DOTWSearchConfig != null && DOTWDetails.Count > 0)
                {

                    //string WebSiteURL = Convert.ToString(DOTWDetails[0].WebSiteURL);
                    string UserID = Convert.ToString(DOTWDetails[0].UserID);
                    string UserName = Convert.ToString(DOTWDetails[0].UserName);
                    string Password = Convert.ToString(DOTWDetails[0].Password);

                    string Supplier = Convert.ToString(DOTWDetails[0].SupplierName);
                    string dotw_currency = Convert.ToString(DOTWDetails[0].CurrencyShortName);
                    string Sitecurrency = tokenData.SiteCurrency;
                    double CurrencyFactordotw = 1;
                    //if (Currencydb.ToLower() != "usd") // change systembase currency 
                    if (dotw_currency.ToLower() != Sitecurrency.ToLower()) // change systembase currency 
                    {
                        CurrencyFactordotw = _commonMethod.GetROEByCurrency(dotw_currency, dtROE);
                    }

                    for (int k = 0; k < DOTWSearchConfig.Count; k++)
                    {
                        //if (Convert.ToInt32(DOTWSearchConfig[k].Hit) != Hit)
                        //    continue;
                        int Noofrecords = Convert.ToInt32(DOTWSearchConfig[k].Noofrecords);
                        int HotelSearchTimeOut = Convert.ToInt32(DOTWSearchConfig[k].HotelSearchTimeOut);
                        //if (Hit > 0)
                        int HotelSearchTimeOut2 = Convert.ToInt32(DOTWSearchConfig[k].HotelSearchTimeOut2);
                        bool DebugMode = (Convert.ToInt32(DOTWSearchConfig[k].DebugMode) == 0 ? false : true);

                        if (Convert.ToInt32(DOTWSearchConfig[k].CityID) == 0 || Convert.ToInt32(DOTWSearchConfig[k].CityID) == SearchPara.CityID)//All City means 0
                        {


                            //DataTable dtDotW = new DataTable();
                            //if (Ids == string.Empty)
                            //    dtDotW = Common.GetAgodaHotelCodeByCityID(SearchPara.CityID, false, "dotw");
                            //GTASearchConfig[k]["Noofrecords = 100;
                            // List<HotelCodeByCity> DtHotelDAta = FilterFinalList(commonCode, listRating);

                            if (commonCode.Length > 0)
                            {
                                List<HotelCodeByCity> DtHotelDAta = HtmlExtensions.ToList<HotelCodeByCity>(commonCode.CopyToDataTable());

                                {
                                    List<WebbedsClass.XMLBaseID_search> lst = GetXMLBAsedIdString(DtHotelDAta);
                                    for (int gt = 0; gt < lst.Count; gt++)
                                    {
                                        List<HotelCodeByCity> filterwithCity = FilterWithCityCode(DtHotelDAta, lst[gt].XMLBaseID.ToLower());
                                        if (filterwithCity.Count > 0)
                                        {
                                            double count = Convert.ToDouble(filterwithCity.Count);
                                            int NoOfDiv = Convert.ToInt32(Math.Ceiling(count / Convert.ToDouble(Noofrecords)));
                                            List<List<WebbedsClass.XMLID_search>> lstxml = GetXMLIDString(NoOfDiv, Noofrecords, count, filterwithCity);

                                            if (SearchPara.Filters.IsShowRooms == 0)
                                            {


                                                for (int a = 0; a < NoOfDiv; a++)
                                                {
                                                    // var paramValues1 = String.Join(",", lstxml[a].Select(x => String.Join(",", Convert.ToString(x.XMLID))));
                                                    xmlbase = lst[gt].XMLBaseID;
                                                    string IdsForSearch = Convert.ToString(GetHotelCodeByCOmma(lstxml[a]));
                                                    HotelX hotelX = new HotelX();
                                                    hotelX.Supplier = Supplier;
                                                    if (_keySettings.SupplierProxy == "0")
                                                        hotelX.URL = Convert.ToString(DOTWDetails[0].WebSiteURL);
                                                    else
                                                        hotelX.URL = Convert.ToString(DOTWDetails[0].WebSiteURLforAPI);

                                                    hotelX.SearchTimeOut = HotelSearchTimeOut;
                                                    hotelX.ProcessTimeout = HotelSearchTimeOut2;
                                                    hotelX.DebugeMode = DebugMode;
                                                    hotelX.CurrencyCode = dotw_currency;
                                                    hotelX.CurrencyFactor = CurrencyFactordotw;
                                                    hotelX.XmlNationality = XmlNationality;
                                                    hotelX.XmlCity = xmlbase;
                                                    hotelX.XmlCountry = xmlCountry;
                                                    #region "CoomonCode"
                                                    bool IsHitSeperate = _commonMethod.IsSuppliersWithSeperateRoomRquest(hotelX.Supplier);
                                                    int NoOfRoomsSearch = 1;
                                                    if (IsHitSeperate)
                                                        NoOfRoomsSearch = SearchPara.Rooms.Count;
                                                    List<RQRS> RQRS = new List<RQRS>();
                                                    hotelX.IsSeperate = IsHitSeperate;
                                                    for (int rr = 0; rr < NoOfRoomsSearch; rr++)
                                                    {
                                                        RQRS objrq = new RQRS();
                                                        List<RoomDetails> serchRooms = new List<RoomDetails>();
                                                        serchRooms.Add(SearchPara.Rooms[rr]);
                                                        objrq.Request = (XMLDOTWRequestOnly_Async(XMLReq, "1", "5", 0, hotelX.URL, UserID, UserName, Password, Noofrecords, HotelSearchTimeOut, DebugMode, SearchPara, IdsForSearch, xmlbase, xmlCountry, XmlNationality));
                                                        objrq.Response = ""; RQRS.Add(objrq);
                                                    }
                                                    hotelX.RQRS = RQRS;
                                                    hotelX.ReturnRooms = Convert.ToInt16(DOTWDetails[0].ReturnRooms);
                                                    hotelX.TechName = Convert.ToString(DOTWDetails[0].TechName);
                                                    #endregion
                                                    AllTask.Add(hotelX);
                                                }
                                            }
                                            else if (SearchPara.Filters.IsShowRooms == 1 && count == 1)
                                            //else if (SearchPara.Filters.IsShowRooms == 1 )
                                            {
                                                xmlbase = lst[gt].XMLBaseID;
                                                string IdsForSearch = Convert.ToString(GetHotelCodeByCOmma(lstxml[0]));
                                                HotelX hotelX = new HotelX();
                                                hotelX.Supplier = Supplier;
                                                //hotelX.URL = Convert.ToString(DOTWDetails[0].WebSiteURL);

                                                if (_keySettings.SupplierProxy == "0")
                                                    hotelX.URL = Convert.ToString(DOTWDetails[0].WebSiteURL);
                                                else
                                                    hotelX.URL = Convert.ToString(DOTWDetails[0].WebSiteURLforAPI);

                                                hotelX.SearchTimeOut = HotelSearchTimeOut;
                                                hotelX.ProcessTimeout = HotelSearchTimeOut2;
                                                hotelX.DebugeMode = DebugMode;
                                                hotelX.CurrencyCode = dotw_currency;
                                                hotelX.CurrencyFactor = CurrencyFactordotw;
                                                hotelX.XmlNationality = XmlNationality;
                                                hotelX.XmlCity = xmlbase;
                                                hotelX.XmlCountry = xmlCountry;
                                                #region "CoomonCode"
                                                bool IsHitSeperate = _commonMethod.IsSuppliersWithSeperateRoomRquest(hotelX.Supplier);
                                                int NoOfRoomsSearch = 1;
                                                if (IsHitSeperate)
                                                    NoOfRoomsSearch = SearchPara.Rooms.Count;
                                                List<RQRS> RQRS = new List<RQRS>();
                                                hotelX.IsSeperate = IsHitSeperate;
                                                for (int rr = 0; rr < NoOfRoomsSearch; rr++)
                                                {
                                                    RQRS objrq = new RQRS();
                                                    List<RoomDetails> serchRooms = new List<RoomDetails>();
                                                    serchRooms.Add(SearchPara.Rooms[rr]);
                                                    DataSet dsOriginalRequest = new DataSet();
                                                    objrq.Request = getDOTW_SearchRoomRequestV3(dsOriginalRequest, SearchPara, DOTWDetails, XmlNationality, IdsForSearch);
                                                    objrq.Response = ""; RQRS.Add(objrq);
                                                }
                                                hotelX.RQRS = RQRS;
                                                hotelX.ReturnRooms = Convert.ToInt16(DOTWDetails[0].ReturnRooms);
                                                hotelX.TechName = Convert.ToString(DOTWDetails[0].TechName);
                                                #endregion
                                                AllTask.Add(hotelX);
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

        public void GetDotwRequestOnly_Async_new(DataRow[] commonCode, List<StarRating> listRating, HotelSearchParameter SearchPara, string XMLReq, List<SupplierDetails> lstDOTWDetails, List<SupplierSearchConfig> lstDOTWSearchConfig, DataTable dtPreferredDataHotel, ref ConcurrentBag<HotelX> AllTask, int Hit, DataTable dtROE, TokenData tokenData, DataSet dtXMLCity)
        {


            try
            {

                List<StarRating> lstRating = new List<StarRating>();
                lstRating = _commonMethod.GetStarRatingStringList_xtg(SearchPara);

                List<HotelCodeByCity> DtHotelDAta = HtmlExtensions.ToList<HotelCodeByCity>(commonCode.CopyToDataTable());
                int ratingcount = lstRating.Count;
                int hotelcount = DtHotelDAta.Count;

                foreach (SupplierDetails DOTWDetails in lstDOTWDetails)
                {
                    try
                    {
                        int SupplierID = DOTWDetails.SupplierID;
                        string Supplier = DOTWDetails.SupplierName;

                        if (lstDOTWSearchConfig.Count > 0)
                        {
                            List<SupplierSearchConfig> DOTWSearchConfig = lstDOTWSearchConfig
                                   .Where(x => x != null && x.SupplierID == SupplierID)
                                   .ToList();

                            if (DOTWSearchConfig != null && DOTWSearchConfig.Count > 0)
                            {
                                DataForSearch dataForSearch = _commonMethod.GetXmlDataForSearchTechwise(SearchPara.Nationality, dtXMLCity,Supplier, DOTWDetails.TechName);

                                if (dataForSearch.XmlNationality != "")
                                {
                                    string Ids = string.Empty;
                                    string xmlbase = "";
                                    _commonMethod.GetPrefferedHotelCodes(ref Ids, ref xmlbase, ref DOTWSearchConfig, Supplier, dtPreferredDataHotel, SearchPara.IsSearchFor);
                                    #region "DOTW"
                                    if (DOTWSearchConfig != null)
                                    {

                                        //string WebSiteURL = Convert.ToString(DOTWDetails[0].WebSiteURL);
                                        string UserID = Convert.ToString(DOTWDetails.UserID);
                                        string UserName = Convert.ToString(DOTWDetails.UserName);
                                        string Password = Convert.ToString(DOTWDetails.Password);

                                        string dotw_currency = Convert.ToString(DOTWDetails.CurrencyShortName);
                                        string Sitecurrency = tokenData.SiteCurrency;
                                        double CurrencyFactordotw = 1;
                                        //if (Currencydb.ToLower() != "usd") // change systembase currency 
                                        if (dotw_currency.ToLower() != Sitecurrency.ToLower()) // change systembase currency 
                                        {
                                            CurrencyFactordotw = _commonMethod.GetROEByCurrency(dotw_currency, dtROE);
                                        }

                                        for (int k = 0; k < DOTWSearchConfig.Count; k++)
                                        {
                                            //if (Convert.ToInt32(DOTWSearchConfig[k].Hit) != Hit)
                                            //    continue;
                                            int Noofrecords = Convert.ToInt32(DOTWSearchConfig[k].Noofrecords);
                                            int HotelSearchTimeOut = Convert.ToInt32(DOTWSearchConfig[k].HotelSearchTimeOut);
                                            //if (Hit > 0)
                                            int HotelSearchTimeOut2 = Convert.ToInt32(DOTWSearchConfig[k].HotelSearchTimeOut2);
                                            bool DebugMode = (Convert.ToInt32(DOTWSearchConfig[k].DebugMode) == 0 ? false : true);

                                            if (Convert.ToInt32(DOTWSearchConfig[k].CityID) == 0 || Convert.ToInt32(DOTWSearchConfig[k].CityID) == SearchPara.CityID)//All City means 0
                                            {


                                                //DataTable dtDotW = new DataTable();
                                                //if (Ids == string.Empty)
                                                //    dtDotW = Common.GetAgodaHotelCodeByCityID(SearchPara.CityID, false, "dotw");
                                                //GTASearchConfig[k]["Noofrecords = 100;
                                                // List<HotelCodeByCity> DtHotelDAta = FilterFinalList(commonCode, listRating);

                                                if (commonCode.Length > 0)
                                                {
                                                    {
                                                        List<WebbedsClass.XMLBaseID_search> lst = GetXMLBAsedIdStringNew(DtHotelDAta, Supplier);
                                                        for (int gt = 0; gt < lst.Count; gt++)
                                                        {
                                                            List<HotelCodeByCity> filterwithCity = FilterWithCityCode_New(DtHotelDAta, lst[gt].XMLBaseID.ToLower(),Supplier);

                                                           
                                                            if (filterwithCity.Count > 0)
                                                            {
                                                                double count = Convert.ToDouble(filterwithCity.Count);
                                                                int NoOfDiv = Convert.ToInt32(Math.Ceiling(count / Convert.ToDouble(Noofrecords)));
                                                                List<List<WebbedsClass.XMLID_search>> lstxml = GetXMLIDString(NoOfDiv, Noofrecords, count, filterwithCity);

                                                                if (SearchPara.Filters.IsShowRooms == 0)
                                                                {


                                                                    for (int a = 0; a < NoOfDiv; a++)
                                                                    {
                                                                        // var paramValues1 = String.Join(",", lstxml[a].Select(x => String.Join(",", Convert.ToString(x.XMLID))));
                                                                        xmlbase = lst[gt].XMLBaseID;
                                                                        string IdsForSearch = Convert.ToString(GetHotelCodeByCOmma(lstxml[a]));
                                                                        HotelX hotelX = new HotelX();
                                                                        hotelX.Supplier = Supplier;
                                                                        if (_keySettings.SupplierProxy == "0")
                                                                            hotelX.URL = Convert.ToString(DOTWDetails.WebSiteURL);
                                                                        else
                                                                            hotelX.URL = Convert.ToString(DOTWDetails.WebSiteURLforAPI);

                                                                        hotelX.SearchTimeOut = HotelSearchTimeOut;
                                                                        hotelX.ProcessTimeout = HotelSearchTimeOut2;
                                                                        hotelX.DebugeMode = DebugMode;
                                                                        hotelX.CurrencyCode = dotw_currency;
                                                                        hotelX.CurrencyFactor = CurrencyFactordotw;
                                                                        hotelX.XmlNationality = dataForSearch.XmlNationality;
                                                                        hotelX.XmlCity = xmlbase;
                                                                        hotelX.XmlCountry = dataForSearch.xmlCountry;
                                                                        #region "CoomonCode"
                                                                        bool IsHitSeperate = _commonMethod.IsSuppliersWithSeperateRoomRquest(hotelX.Supplier);
                                                                        int NoOfRoomsSearch = 1;
                                                                        if (IsHitSeperate)
                                                                            NoOfRoomsSearch = SearchPara.Rooms.Count;
                                                                        List<RQRS> RQRS = new List<RQRS>();
                                                                        hotelX.IsSeperate = IsHitSeperate;
                                                                        for (int rr = 0; rr < NoOfRoomsSearch; rr++)
                                                                        {
                                                                            RQRS objrq = new RQRS();
                                                                            List<RoomDetails> serchRooms = new List<RoomDetails>();
                                                                            serchRooms.Add(SearchPara.Rooms[rr]);
                                                                            objrq.Request = (XMLDOTWRequestOnly_Async(XMLReq, "1", "5", 0, hotelX.URL, UserID, UserName, Password, Noofrecords, HotelSearchTimeOut, DebugMode, SearchPara, IdsForSearch, xmlbase, dataForSearch.xmlCountry, dataForSearch.XmlNationality));
                                                                            objrq.Response = ""; RQRS.Add(objrq);
                                                                        }
                                                                        hotelX.RQRS = RQRS;
                                                                        hotelX.ReturnRooms = Convert.ToInt16(DOTWDetails.ReturnRooms);
                                                                        hotelX.TechName = Convert.ToString(DOTWDetails.TechName);
                                                                        #endregion
                                                                        AllTask.Add(hotelX);
                                                                    }
                                                                }
                                                                else if (SearchPara.Filters.IsShowRooms == 1 && count == 1)
                                                                //else if (SearchPara.Filters.IsShowRooms == 1 )
                                                                {
                                                                    xmlbase = lst[gt].XMLBaseID;
                                                                    string IdsForSearch = Convert.ToString(GetHotelCodeByCOmma(lstxml[0]));
                                                                    HotelX hotelX = new HotelX();
                                                                    hotelX.Supplier = Supplier;
                                                                    //hotelX.URL = Convert.ToString(DOTWDetails[0].WebSiteURL);

                                                                    if (_keySettings.SupplierProxy == "0")
                                                                        hotelX.URL = Convert.ToString(DOTWDetails.WebSiteURL);
                                                                    else
                                                                        hotelX.URL = Convert.ToString(DOTWDetails.WebSiteURLforAPI);

                                                                    hotelX.SearchTimeOut = HotelSearchTimeOut;
                                                                    hotelX.ProcessTimeout = HotelSearchTimeOut2;
                                                                    hotelX.DebugeMode = DebugMode;
                                                                    hotelX.CurrencyCode = dotw_currency;
                                                                    hotelX.CurrencyFactor = CurrencyFactordotw;
                                                                    hotelX.XmlNationality = dataForSearch.XmlNationality;
                                                                    hotelX.XmlCity = xmlbase;
                                                                    hotelX.XmlCountry = dataForSearch.xmlCountry;
                                                                    #region "CoomonCode"
                                                                    bool IsHitSeperate = _commonMethod.IsSuppliersWithSeperateRoomRquest(hotelX.Supplier);
                                                                    int NoOfRoomsSearch = 1;
                                                                    if (IsHitSeperate)
                                                                        NoOfRoomsSearch = SearchPara.Rooms.Count;
                                                                    List<RQRS> RQRS = new List<RQRS>();
                                                                    hotelX.IsSeperate = IsHitSeperate;
                                                                    for (int rr = 0; rr < NoOfRoomsSearch; rr++)
                                                                    {
                                                                        RQRS objrq = new RQRS();
                                                                        List<RoomDetails> serchRooms = new List<RoomDetails>();
                                                                        serchRooms.Add(SearchPara.Rooms[rr]);
                                                                        DataSet dsOriginalRequest = new DataSet();
                                                                        objrq.Request = getDOTW_SearchRoomRequestV4(dsOriginalRequest, SearchPara, DOTWDetails, dataForSearch.XmlNationality, IdsForSearch);
                                                                        objrq.Response = ""; RQRS.Add(objrq);
                                                                    }
                                                                    hotelX.RQRS = RQRS;
                                                                    hotelX.ReturnRooms = Convert.ToInt16(DOTWDetails.ReturnRooms);
                                                                    hotelX.TechName = Convert.ToString(DOTWDetails.TechName);
                                                                    #endregion
                                                                    AllTask.Add(hotelX);
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
                            }
                        }
                    }
                    catch (Exception)
                    {

                        throw;
                    }
                }

                   

            }
            catch (Exception ex)
            {
                _exception.Error(ex);
            }
        }

        public List<WebbedsClass.XMLBaseID_search> GetXMLBAsedIdString(List<HotelCodeByCity> DtHotelDAta)
        {
            List<WebbedsClass.XMLBaseID_search> lst = new List<WebbedsClass.XMLBaseID_search>();

            try
            {
                for (int x = 0; x < DtHotelDAta.Count; x++)
                {
                    List<WebbedsClass.XMLBaseID_search> temp = lst.FindAll(delegate (WebbedsClass.XMLBaseID_search obj) { return Convert.ToString(DtHotelDAta[x].XmlbaseId).ToLower() == obj.XMLBaseID.ToLower() ; });
                    if (temp.Count == 0)
                    {
                        lst.Add(new WebbedsClass.XMLBaseID_search { XMLBaseID = Convert.ToString(DtHotelDAta[x].XmlbaseId) });
                    }

                }

            }
            catch (Exception ex)
            {
                _exception.Error(ex);
            }
            return lst;
        }

        public List<WebbedsClass.XMLBaseID_search> GetXMLBAsedIdStringNew(List<HotelCodeByCity> DtHotelDAta,string Supplier)
        {
            List<WebbedsClass.XMLBaseID_search> lst = new List<WebbedsClass.XMLBaseID_search>();

            try
            {
                for (int x = 0; x < DtHotelDAta.Count; x++)
                {
                    if (Convert.ToString(DtHotelDAta[x].supplierName).ToLower() == Supplier.ToLower())
                    {
                        List<WebbedsClass.XMLBaseID_search> temp = lst.FindAll(delegate (WebbedsClass.XMLBaseID_search obj) { return Convert.ToString(DtHotelDAta[x].XmlbaseId).ToLower() == obj.XMLBaseID.ToLower(); });
                        if (temp.Count == 0)
                        {
                            lst.Add(new WebbedsClass.XMLBaseID_search { XMLBaseID = Convert.ToString(DtHotelDAta[x].XmlbaseId) });
                        }
                    }
                    

                }

            }
            catch (Exception ex)
            {
                _exception.Error(ex);
            }
            return lst;
        }

        public List<HotelCodeByCity> FilterWithCityCode(List<HotelCodeByCity> DtHotelDAta, string XMLBaseID)
        {
            List<HotelCodeByCity> filterwithCity = new List<HotelCodeByCity>();
            try
            {
                for (int c = 0; c < DtHotelDAta.Count; c++)
                {
                    if (DtHotelDAta[c].XmlbaseId.ToLower() == XMLBaseID.ToLower())
                        filterwithCity.Add(DtHotelDAta[c]);
                }
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
            }
            return filterwithCity;
        }

        public List<HotelCodeByCity> FilterWithCityCode_New(List<HotelCodeByCity> DtHotelDAta, string XMLBaseID,string Supplier)
        {
            List<HotelCodeByCity> filterwithCity = new List<HotelCodeByCity>();
            try
            {
                for (int c = 0; c < DtHotelDAta.Count; c++)
                {
                    if (DtHotelDAta[c].XmlbaseId.ToLower() == XMLBaseID.ToLower() && DtHotelDAta[c].supplierName.ToLower() == Supplier.ToLower())
                        filterwithCity.Add(DtHotelDAta[c]);
                }
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
            }
            return filterwithCity;
        }

        public List<List<WebbedsClass.XMLID_search>> GetXMLIDString(double NoOfDiv, int Noofrecords, double count, List<HotelCodeByCity> DtHotelDAta)
        {
            List<List<WebbedsClass.XMLID_search>> lstxml = new List<List<WebbedsClass.XMLID_search>>();

            try
            {
                int first = 0;
                for (int xm = 0; xm < NoOfDiv; xm++)
                {
                    List<WebbedsClass.XMLID_search> tempid = new List<WebbedsClass.XMLID_search>();
                    for (int i = 0; i < Noofrecords; i++)
                    {
                        if (first < count)
                        {
                            tempid.Add(new WebbedsClass.XMLID_search { XMLID = Convert.ToString(DtHotelDAta[first].XmlId) });
                            first++;
                        }
                        else
                            break;
                    }
                    lstxml.Add(tempid);
                }
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
            }
            return lstxml;

        }

        public string GetHotelCodeByCOmma(List<WebbedsClass.XMLID_search> lstxml)
        {
            string paramValues1 = "";

            try
            {
                for (int h = 0; h < lstxml.Count; h++)
                {
                    paramValues1 = paramValues1 + lstxml[h].XMLID + ",";
                }
                if (paramValues1.Length > 0)
                    paramValues1 = paramValues1.Substring(0, paramValues1.Length - 1);//Remove last comma(,) 
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
            }
            return paramValues1;
        }

        public string XMLDOTWRequestOnly_Async(string XmlRQRS, string MinValue, string MaxValue, int pageindex, string DOTWUrl, string DOTWClientID, string DOTWUserName, string DOTWPassword, Int32 NoofRecords, Int32 SearchTimeOut, bool DebugMode, HotelSearchParameter objSearPera, string DotwhotelIds, string xmlCity, string xmlCountry, string XmlNationality)
        {
            try
            {
                string CurrencyCode = "520";
                string checkindate = objSearPera.CheckInDate; //Convert.ToString(dsOriginalRequest.Tables["StayDateRange"].Rows[0]["Start"]);
                string checkOutdate = objSearPera.CheckOutDate;//Convert.ToString(dsOriginalRequest.Tables["StayDateRange"].Rows[0]["End"]);
                string[] rangeIn = checkindate.Split('/');
                checkindate = rangeIn[2] + rangeIn[1] + rangeIn[0];

                string[] rangeOut = checkOutdate.Split('/');
                checkOutdate = rangeOut[2] + rangeOut[1] + rangeOut[0];

                StringBuilder sbDOTWBlockReq = new StringBuilder();
                sbDOTWBlockReq.Append("<customer>");
                sbDOTWBlockReq.Append(getDOTWProfileString(DOTWClientID, DOTWUserName, DOTWPassword));
                sbDOTWBlockReq.Append("<request command=\"searchhotels\" >");
                sbDOTWBlockReq.Append("<bookingDetails>");
                sbDOTWBlockReq.Append("<fromDate>" + checkindate.Substring(0, 4) + "-" + checkindate.Substring(4, 2) + "-" + checkindate.Substring(6, 2) + "</fromDate>");
                sbDOTWBlockReq.Append("<toDate>" + checkOutdate.Substring(0, 4) + "-" + checkOutdate.Substring(4, 2) + "-" + checkOutdate.Substring(6, 2) + "</toDate>");
                sbDOTWBlockReq.Append("<currency>" + CurrencyCode + "</currency>");
                sbDOTWBlockReq.Append("<rooms no=\"" + objSearPera.Rooms.Count + "\">");
                for (int room = 0; room < objSearPera.Rooms.Count; room++)
                {
                    int Adult = Convert.ToInt32(objSearPera.Rooms[room].NoofAdults);
                    int Child = Convert.ToInt32(objSearPera.Rooms[room].NoofChild);

                    sbDOTWBlockReq.Append("<room runno=\"" + room + "\">");
                    sbDOTWBlockReq.Append("<adultsCode>" + Convert.ToString(Adult) + "</adultsCode>");
                    sbDOTWBlockReq.Append("<children no=\"" + Child + "\">");

                    if (Child > 0)
                    {
                        switch (Child)
                        {
                            case 1:
                                sbDOTWBlockReq.Append(" <child runno=\"0\">" + Convert.ToInt32(objSearPera.Rooms[room].Child1Age) + "</child>");

                                break;
                            case 2:
                                sbDOTWBlockReq.Append(" <child runno=\"0\">" + Convert.ToInt32(objSearPera.Rooms[room].Child1Age) + "</child>");
                                sbDOTWBlockReq.Append(" <child runno=\"1\">" + Convert.ToInt32(objSearPera.Rooms[room].Child2Age) + "</child>");
                                break;
                        }
                    }
                    sbDOTWBlockReq.Append("</children>");
                    //sbDOTWBlockReq.Append("<extraBed>0</extraBed>");
                    sbDOTWBlockReq.Append("<rateBasis>-1</rateBasis>");
                    sbDOTWBlockReq.Append("<passengerNationality>" + XmlNationality + "</passengerNationality>");
                    sbDOTWBlockReq.Append("<passengerCountryOfResidence>" + XmlNationality + "</passengerCountryOfResidence>");
                    sbDOTWBlockReq.Append("</room>");
                }
                sbDOTWBlockReq.Append("</rooms>");
                sbDOTWBlockReq.Append("</bookingDetails>");


                sbDOTWBlockReq.Append("<return><filters xmlns:a=\"http://us.dotwconnect.com/xsd/atomicCondition\" xmlns:c=\"http://us.dotwconnect.com/xsd/complexCondition\">");
                sbDOTWBlockReq.Append("<c:condition><a:condition><fieldName>hotelId</fieldName><fieldTest>in</fieldTest>");
                sbDOTWBlockReq.Append("<fieldValues>");
                if (DotwhotelIds != "")
                {
                    string[] Hid = DotwhotelIds.Split(',');
                    StringBuilder sbDotwRQ = new StringBuilder();
                    for (int i = 0; i < Hid.Length; i++)
                    {
                        sbDOTWBlockReq.Append("<fieldValue>");
                        sbDOTWBlockReq.Append(Hid[i]);
                        sbDOTWBlockReq.Append("</fieldValue>");
                    }
                }

                sbDOTWBlockReq.Append("</fieldValues></a:condition></c:condition></filters></return></request>");
                sbDOTWBlockReq.Append("</customer>");
                return sbDOTWBlockReq.ToString();
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
                return "";
            }
        }

        public string getDOTWProfileString(string ClientID, string UserName, string Password)
        {
            try
            {
                StringBuilder sbDOTWProfile = new StringBuilder();
                sbDOTWProfile.Append("<username>" + UserName + "</username>");
                sbDOTWProfile.Append("<password>" + Password + "</password>");
                sbDOTWProfile.Append("<id>" + ClientID + "</id>");
                sbDOTWProfile.Append("<source>1</source>");
                sbDOTWProfile.Append("<product>hotel</product>");
                return sbDOTWProfile.ToString();
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
            }
            return string.Empty;
        }

        public string getDOTW_SearchRoomRequestV3(DataSet dsOriginalRequest, HotelSearchParameter objSearPera, List<SupplierDetails> DOTWDetails, string Nationality, string xmlhotelcode)
        {
            try
            {
                string CurrencyCode = "520";
                string checkindate = objSearPera.CheckInDate; //Convert.ToString(dsOriginalRequest.Tables["StayDateRange"].Rows[0]["Start"]);
                string checkOutdate = objSearPera.CheckOutDate;//Convert.ToString(dsOriginalRequest.Tables["StayDateRange"].Rows[0]["End"]);
                string[] rangeIn = checkindate.Split('/');
                checkindate = rangeIn[2] + rangeIn[1] + rangeIn[0];

                string[] rangeOut = checkOutdate.Split('/');
                checkOutdate = rangeOut[2] + rangeOut[1] + rangeOut[0];

                StringBuilder sbDOTWBlockReq = new StringBuilder();
                sbDOTWBlockReq.Append("<customer>");
                sbDOTWBlockReq.Append(getDOTWProfileString(Convert.ToString(DOTWDetails[0].UserID), Convert.ToString(DOTWDetails[0].UserName), Convert.ToString(DOTWDetails[0].Password)));
                sbDOTWBlockReq.Append("<request command=\"getrooms\" debug=\"0\">");
                sbDOTWBlockReq.Append("<bookingDetails>");
                sbDOTWBlockReq.Append("<fromDate>" + checkindate.Substring(0, 4) + "-" + checkindate.Substring(4, 2) + "-" + checkindate.Substring(6, 2) + "</fromDate>");
                sbDOTWBlockReq.Append("<toDate>" + checkOutdate.Substring(0, 4) + "-" + checkOutdate.Substring(4, 2) + "-" + checkOutdate.Substring(6, 2) + "</toDate>");
                sbDOTWBlockReq.Append("<currency>" + CurrencyCode + "</currency>");
                sbDOTWBlockReq.Append("<rooms no=\"" + objSearPera.Rooms.Count + "\">");
                for (int room = 0; room < objSearPera.Rooms.Count; room++)
                {
                    int Adult = Convert.ToInt32(objSearPera.Rooms[room].NoofAdults);
                    int Child = Convert.ToInt32(objSearPera.Rooms[room].NoofChild);

                    sbDOTWBlockReq.Append("<room runno=\"" + room + "\">");
                    sbDOTWBlockReq.Append("<adultsCode>" + Convert.ToString(Adult) + "</adultsCode>");
                    sbDOTWBlockReq.Append("<children no=\"" + Child + "\">");

                    if (Child > 0)
                    {
                        switch (Child)
                        {
                            case 1:
                                sbDOTWBlockReq.Append(" <child runno=\"0\">" + Convert.ToInt32(objSearPera.Rooms[room].Child1Age) + "</child>");

                                break;
                            case 2:
                                sbDOTWBlockReq.Append(" <child runno=\"0\">" + Convert.ToInt32(objSearPera.Rooms[room].Child1Age) + "</child>");
                                sbDOTWBlockReq.Append(" <child runno=\"1\">" + Convert.ToInt32(objSearPera.Rooms[room].Child2Age) + "</child>");
                                break;
                        }
                    }
                    sbDOTWBlockReq.Append("</children>");
                    //sbDOTWBlockReq.Append("<extraBed>0</extraBed>");
                    sbDOTWBlockReq.Append("<rateBasis>-1</rateBasis>");
                    sbDOTWBlockReq.Append("<passengerNationality>" + Nationality + "</passengerNationality>");
                    sbDOTWBlockReq.Append("<passengerCountryOfResidence>" + Nationality + "</passengerCountryOfResidence>");
                    sbDOTWBlockReq.Append("</room>");
                }
                sbDOTWBlockReq.Append("</rooms>");
                sbDOTWBlockReq.Append("<productId>" + xmlhotelcode + "</productId>");
                sbDOTWBlockReq.Append("</bookingDetails>");
                sbDOTWBlockReq.Append("</request>");
                sbDOTWBlockReq.Append("</customer>");
                return sbDOTWBlockReq.ToString();
            }
            catch (Exception)
            {

                return "";
            }




        }

        public string getDOTW_SearchRoomRequestV4(DataSet dsOriginalRequest, HotelSearchParameter objSearPera, SupplierDetails DOTWDetails, string Nationality, string xmlhotelcode)
        {
            try
            {
                string CurrencyCode = "520";
                string checkindate = objSearPera.CheckInDate; //Convert.ToString(dsOriginalRequest.Tables["StayDateRange"].Rows[0]["Start"]);
                string checkOutdate = objSearPera.CheckOutDate;//Convert.ToString(dsOriginalRequest.Tables["StayDateRange"].Rows[0]["End"]);
                string[] rangeIn = checkindate.Split('/');
                checkindate = rangeIn[2] + rangeIn[1] + rangeIn[0];

                string[] rangeOut = checkOutdate.Split('/');
                checkOutdate = rangeOut[2] + rangeOut[1] + rangeOut[0];

                StringBuilder sbDOTWBlockReq = new StringBuilder();
                sbDOTWBlockReq.Append("<customer>");
                sbDOTWBlockReq.Append(getDOTWProfileString(Convert.ToString(DOTWDetails.UserID), Convert.ToString(DOTWDetails.UserName), Convert.ToString(DOTWDetails.Password)));
                sbDOTWBlockReq.Append("<request command=\"getrooms\" debug=\"0\">");
                sbDOTWBlockReq.Append("<bookingDetails>");
                sbDOTWBlockReq.Append("<fromDate>" + checkindate.Substring(0, 4) + "-" + checkindate.Substring(4, 2) + "-" + checkindate.Substring(6, 2) + "</fromDate>");
                sbDOTWBlockReq.Append("<toDate>" + checkOutdate.Substring(0, 4) + "-" + checkOutdate.Substring(4, 2) + "-" + checkOutdate.Substring(6, 2) + "</toDate>");
                sbDOTWBlockReq.Append("<currency>" + CurrencyCode + "</currency>");
                sbDOTWBlockReq.Append("<rooms no=\"" + objSearPera.Rooms.Count + "\">");
                for (int room = 0; room < objSearPera.Rooms.Count; room++)
                {
                    int Adult = Convert.ToInt32(objSearPera.Rooms[room].NoofAdults);
                    int Child = Convert.ToInt32(objSearPera.Rooms[room].NoofChild);

                    sbDOTWBlockReq.Append("<room runno=\"" + room + "\">");
                    sbDOTWBlockReq.Append("<adultsCode>" + Convert.ToString(Adult) + "</adultsCode>");
                    sbDOTWBlockReq.Append("<children no=\"" + Child + "\">");

                    if (Child > 0)
                    {
                        switch (Child)
                        {
                            case 1:
                                sbDOTWBlockReq.Append(" <child runno=\"0\">" + Convert.ToInt32(objSearPera.Rooms[room].Child1Age) + "</child>");

                                break;
                            case 2:
                                sbDOTWBlockReq.Append(" <child runno=\"0\">" + Convert.ToInt32(objSearPera.Rooms[room].Child1Age) + "</child>");
                                sbDOTWBlockReq.Append(" <child runno=\"1\">" + Convert.ToInt32(objSearPera.Rooms[room].Child2Age) + "</child>");
                                break;
                        }
                    }
                    sbDOTWBlockReq.Append("</children>");
                    //sbDOTWBlockReq.Append("<extraBed>0</extraBed>");
                    sbDOTWBlockReq.Append("<rateBasis>-1</rateBasis>");
                    sbDOTWBlockReq.Append("<passengerNationality>" + Nationality + "</passengerNationality>");
                    sbDOTWBlockReq.Append("<passengerCountryOfResidence>" + Nationality + "</passengerCountryOfResidence>");
                    sbDOTWBlockReq.Append("</room>");
                }
                sbDOTWBlockReq.Append("</rooms>");
                sbDOTWBlockReq.Append("<productId>" + xmlhotelcode + "</productId>");
                sbDOTWBlockReq.Append("</bookingDetails>");
                sbDOTWBlockReq.Append("</request>");
                sbDOTWBlockReq.Append("</customer>");
                return sbDOTWBlockReq.ToString();
            }
            catch (Exception)
            {

                return "";
            }




        }

        public async Task<List<Options>> DOTW_HotelSearchWithRooms(HotelX Item, CommonSearch commonSearch, string ResponseData, HotelSearchParameter SearchParam, string XMLCITY, string XMLCOUNTRY, string Currency, double CurecncyFactor, Int32 processTimeout, string ResProccessTimerLogActive, string ResProccessTimeValueforSaveLog, string StoreSupplierResErrorLog, DataTable DtROE)
        {
            System.Diagnostics.Stopwatch timerroomx = new System.Diagnostics.Stopwatch();
            timerroomx.Start();
            List<Options> options = new List<Options>();
            try
            {
                DataTable DtHotel = _commonMethod.GenerateDataTableNewV2();
                {
                    ResponseData = ResponseData.Replace("<?xml version=\"1.0\" encoding=\"UTF-8\"?>", "");
                    if (ResponseData == null || ResponseData == "")
                    {
                        Item.ResponseType = "NoCount";
                        return options;
                    }
                    if (ResponseData.IndexOf("<extraMeals") > 0)
                    {
                        String Data2 = ResponseData.Substring(ResponseData.IndexOf("<extraMeals"), ResponseData.IndexOf("</extraMeals>") - ResponseData.IndexOf("<extraMeals")) + "</extraMeals>";
                        ResponseData = ResponseData.Replace(Data2, "");
                    }
                    DataSet DOTWResponse = new DataSet();
                    try
                    {
                        DOTWResponse = _commonMethod.GetDataSetFromXMLString(ResponseData);
                    }
                    catch (Exception ex)
                    {
                        _exception.Error(ex);
                    }
                    if (DOTWResponse == null || DOTWResponse.Tables["hotel"] == null)
                    {
                        Item.ResponseType = "NoCount";
                        return options;
                    }
                    if (SearchParam.Filters.IsShowRooms == 0)
                    {
                        if (!DOTWResponse.Tables["rateBasis"].Columns.Contains("price"))
                        {
                            DOTWResponse.Tables["rateBasis"].Columns.Add("price", typeof(double));

                            for (int i = 0; i < DOTWResponse.Tables["rateBasis"].Rows.Count; i++)
                            {
                                DOTWResponse.Tables["rateBasis"].Rows[i]["price"] = Convert.ToDouble(Convert.ToString(DOTWResponse.Tables["rateBasis"].Rows[i]["total"]).Replace(",", ""));
                            }
                        }
                    }
                    for (int h = 0; h <= DOTWResponse.Tables["hotel"].Rows.Count - 1; h++)
                    {
                        Item.ResponseType = "Success";
                        double FinalMinPrice = 0;
                        #region "When not Need Room"  
                        // add hotel code from price only and room code blank here
                        if (SearchParam.Filters.IsShowRooms == 0)
                        {
                            #region "Hotel Code Block "
                            DataRow drXmlHotel = DtHotel.NewRow();
                            // LowestRate = 0;
                            drXmlHotel["XMLCountry"] = XMLCOUNTRY;
                            drXmlHotel["XMLCity"] = XMLCITY;
                            drXmlHotel["XmlHotelCity"] = XMLCITY;
                            drXmlHotel["XMLHotelCode"] = Convert.ToString(DOTWResponse.Tables["hotel"].Rows[h]["hotelid"]);
                            drXmlHotel["HotelName"] = "";
                            drXmlHotel["SupplierName"] = Item.Supplier;
                            drXmlHotel["OfficeID"] = 0;
                            drXmlHotel["ContractID"] = 0;
                            drXmlHotel["Rating"] = 0;
                            drXmlHotel["Reviews"] = 0;
                            drXmlHotel["SupplierCurrency"] = Currency;
                            drXmlHotel["UniqueNo"] = Convert.ToString(DOTWResponse.Tables["hotel"].Rows[h]["hotelid"]);
                            drXmlHotel["IsShowSupplier"] = false;
                            drXmlHotel["BookingStatus"] = "Available";
                            DataRow[] drrooms = DOTWResponse.Tables["rooms"].Select("hotel_Id=" + DOTWResponse.Tables["hotel"].Rows[h]["hotel_Id"] + "");
                            if (drrooms.GetLength(0) > 0)
                            {
                                string rooms_Id = Convert.ToString(drrooms[0]["rooms_Id"]);
                                DataRow[] drroom = DOTWResponse.Tables["room"].Select("rooms_Id=" + rooms_Id + "");
                                if (drroom.Length > 0)
                                {
                                    if (commonSearch.tokenData.AgentId != 1899)
                                    {
                                        double FinalPrice = 0.0;
                                        ///animesh changes code  21 feb 2019 for price 2 comments
                                        for (int s = 0; s < drroom.Length; s++)
                                        {
                                            DataRow[] drroomType = DOTWResponse.Tables["roomType"].Select("room_Id=" + drroom[s]["room_Id"] + "");
                                            string roomType_Id = string.Empty;
                                            for (int rm = 0; rm < drroomType.Length; rm++)
                                                roomType_Id = roomType_Id + "'" + Convert.ToString(drroomType[rm]["roomType_Id"]) + "',";
                                            //Remove Last comma
                                            roomType_Id = roomType_Id.Substring(0, roomType_Id.Length - 1);
                                            DataRow[] drRateBases = DOTWResponse.Tables["rateBases"].Select("roomType_Id in(" + roomType_Id + ")");



                                            string rateBases_Id = string.Empty;
                                            for (int rm = 0; rm < drRateBases.Length; rm++)
                                                rateBases_Id = rateBases_Id + "'" + Convert.ToString(drRateBases[rm]["rateBases_Id"]) + "',";
                                            //Remove Last comma
                                            rateBases_Id = rateBases_Id.Substring(0, rateBases_Id.Length - 1);
                                            DataRow[] drrateBasis = DOTWResponse.Tables["rateBasis"].Select("rateBases_Id in(" + rateBases_Id + ")");

                                            double MinPrice = Convert.ToDouble(DOTWResponse.Tables["rateBasis"].Compute("min(price)", "rateBases_Id in(" + rateBases_Id + ")"));

                                            FinalPrice = FinalPrice + MinPrice;
                                        }

                                        FinalMinPrice = Math.Round(FinalPrice, 2);
                                    }
                                    else
                                    {
                                        FinalMinPrice = 1;
                                    }
                                    FinalMinPrice = FinalMinPrice * CurecncyFactor;
                                    drXmlHotel["FinalSellingPrice"] = FinalMinPrice;
                                    drXmlHotel["SellingPrice"] = FinalMinPrice;
                                    drXmlHotel["IsRecomondedHotel"] = false;
                                    //// if (FinalPrice > 0)
                                    DtHotel.Rows.Add(drXmlHotel);
                                }


                            }

                            #endregion


                            if (FinalMinPrice > 0)
                            {
                                #region "Room code Block"
                                bool willAddOrNot = true;
                                Int64 RaynaID = 0;
                                Int64 GiataId = 0;
                                int Rating = 0;
                                Int64 OwnsystemHotelId = 0;
                                double MinPrice = FinalMinPrice;
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
                            drXmlHotel["XMLHotelCode"] = Convert.ToString(DOTWResponse.Tables["hotel"].Rows[h]["id"]);
                            drXmlHotel["HotelName"] = "";
                            drXmlHotel["SupplierName"] = Item.Supplier;
                            drXmlHotel["OfficeID"] = 0;
                            drXmlHotel["ContractID"] = 0;
                            drXmlHotel["Rating"] = 0;
                            drXmlHotel["Reviews"] = 0;
                            drXmlHotel["SupplierCurrency"] = Currency;
                            drXmlHotel["UniqueNo"] = Convert.ToString(DOTWResponse.Tables["hotel"].Rows[h]["id"]);
                            drXmlHotel["IsShowSupplier"] = false;
                            drXmlHotel["BookingStatus"] = "Available";
                            // DataRow[] drrooms = DOTWResponse.Tables["rooms"].Select("hotel_Id=" + InternalHotelId + "");
                            //if (drrooms.GetLength(0) > 0)
                            {
                                // string rooms_Id = Convert.ToString(drrooms[0]["rooms_Id"]);
                                // DataRow[] drroom = DOTWResponse.Tables["room"].Select("rooms_Id=" + rooms_Id + "");
                                // if (drroom.Length > 0)
                                {
                                    // double FinalPrice = 0.0;
                                    // animesh changes code  21 feb 2019 for price 2 comments
                                    //////for (int s = 0; s < drroom.Length; s++)
                                    //////{
                                    //////    DataRow[] drroomType = DOTWResponse.Tables["roomType"].Select("room_Id=" + drroom[s]["room_Id"] + "");
                                    //////    string roomType_Id = string.Empty;
                                    //////    for (int rm = 0; rm < drroomType.Length; rm++)
                                    //////        roomType_Id = roomType_Id + "'" + Convert.ToString(drroomType[rm]["roomType_Id"]) + "',";
                                    //////    //Remove Last comma
                                    //////    roomType_Id = roomType_Id.Substring(0, roomType_Id.Length - 1);
                                    //////    DataRow[] drRateBases = DOTWResponse.Tables["rateBases"].Select("roomType_Id in(" + roomType_Id + ")");



                                    //////    string rateBases_Id = string.Empty;
                                    //////    for (int rm = 0; rm < drRateBases.Length; rm++)
                                    //////        rateBases_Id = rateBases_Id + "'" + Convert.ToString(drRateBases[rm]["rateBases_Id"]) + "',";
                                    //////    //Remove Last comma
                                    //////    rateBases_Id = rateBases_Id.Substring(0, rateBases_Id.Length - 1);
                                    //////    DataRow[] drrateBasis = DOTWResponse.Tables["rateBasis"].Select("rateBases_Id in(" + rateBases_Id + ")");

                                    //////    double MinPrice = Convert.ToDouble(DOTWResponse.Tables["rateBasis"].Compute("min(price)", "rateBases_Id in(" + rateBases_Id + ")"));

                                    //////    FinalPrice = FinalPrice + MinPrice;
                                    //////}

                                    //LowestRate = Math.Round(LowestRate / SearchParam.NoofNights, 2);
                                    drXmlHotel["FinalSellingPrice"] = 1;
                                    drXmlHotel["SellingPrice"] = 1;
                                    drXmlHotel["IsRecomondedHotel"] = false;
                                    //// if (FinalPrice > 0)
                                    DtHotel.Rows.Add(drXmlHotel);
                                }


                            }

                            #endregion
                            #region "GetRooms for this hotel"
                            bool willAddOrNot = true;
                            Int64 RaynaID = 0;
                            Int64 GiataId = 0;
                            int RatingHotel = 0;
                            Int64 OwnsystemHotelId = 0;
                            string Provider = Item.Supplier;
                            double MinPrice = 1;

                            _markup.GetHotelDataCommon(ref MinPrice, ref willAddOrNot, ref OwnsystemHotelId, ref RaynaID, ref GiataId, ref RatingHotel, ref drXmlHotel, commonSearch.AgentId, commonSearch.TariffID, commonSearch.dsAgentDetails, commonSearch.supplierGiataMaster, SearchParam, commonSearch.dtCountryCityMarkup, commonSearch.B2CMarkup, commonSearch.IsParentId, commonSearch.SubAgentMarkup, commonSearch.SearchByHotel, commonSearch.HotelIDS);
                            if (willAddOrNot)
                            {
                                DataTable dtHotelRoom = _commonMethod.GenerateRoomtypeDataTableNewV2();
                                DataTable dtCancellation = _commonMethod.GenerateCancellationDataTableNewV2();
                                DataTable DtRoomRateBreackUp = _commonMethod.GenerateoomRateBreackUpDataTableNewV2();
                                //int CutOffDays = XMLHotelBookingCommon.CutOffDays;
                                //CutOffDays = Common.GetCutOffDays(commonSearch.dtSuplierMarkup, Provider);
                                int CutOffDays = CacheMaster.CutOffDays;
                                CutOffDays = _commonMethod.GetCutOffDays(commonSearch.dtSuplierMarkup, Provider);
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
                                    for (int i = 0; i < SearchParam.Rooms.Count; i++)
                                        maxOcupancy = maxOcupancy + SearchParam.Rooms[i].NoofChild + SearchParam.Rooms[i].NoofAdults;
                                    #region Supplier Room Code block
                                    DataRow[] drdtRooms = DOTWResponse.Tables["rooms"].Select("hotel_Id=" + DOTWResponse.Tables["hotel"].Rows[h]["hotel_Id"] + "");
                                    for (int DtRooms = 0; DtRooms <= drdtRooms.GetLength(0) - 1; DtRooms++)  // This block for Room wise 
                                    {
                                        //if (DtRooms > CacheMaster.RoomLimit_10)
                                        //    break;


                                        if (commonSearch.AgentId == 4440 || commonSearch.AgentId == 4901)
                                        {
                                            if (DtRooms > CacheMaster.RoomLimit_20)
                                                break;
                                        }
                                        else
                                        {
                                            if (DtRooms > CacheMaster.RoomLimit_10)
                                                break;
                                        }

                                        int RoomTypeCount = 0;
                                        int dtrooms_Id = Convert.ToInt32(drdtRooms[DtRooms]["rooms_Id"]);
                                        DataRow[] dtdrroom = DOTWResponse.Tables["room"].Select("rooms_Id=" + dtrooms_Id + "");
                                        int LogicalCounter = 0;
                                        int PreviousData = -1;
                                        for (int dtroom = 0; dtroom <= dtdrroom.GetLength(0) - 1; dtroom++) // Mlutiple room for that rooms
                                        {
                                            while (PreviousData != dtroom)
                                            {
                                                LogicalCounter = 0;
                                                PreviousData = dtroom;
                                            }
                                            int dtroom_Id = Convert.ToInt32(dtdrroom[dtroom]["room_Id"]);
                                            DataRow[] drdtRoomType = DOTWResponse.Tables["roomType"].Select("room_Id=" + dtroom_Id + "");
                                            for (int dtroomtype = 0; dtroomtype <= drdtRoomType.GetLength(0) - 1; dtroomtype++)
                                            {
                                                int dtRoomType_Id = Convert.ToInt32(drdtRoomType[dtroomtype]["roomType_Id"]);

                                                string dotwRoomType = Convert.ToString(drdtRoomType[dtroomtype]["name"]);
                                                string dotwRoomTypeTwin = Convert.ToString(drdtRoomType[dtroomtype]["twin"]);
                                                string dotwroomtypecode = Convert.ToString(drdtRoomType[dtroomtype]["roomtypecode"]);
                                                int AdultsCapacity = 0, ChildCapacity = 0, MaxExtrabed = 0;
                                                DataRow[] dtdrroomCapacity = DOTWResponse.Tables["roomInfo"].Select("roomType_Id=" + dtRoomType_Id + "");
                                                if (dtdrroomCapacity.GetLength(0) > 0)
                                                {
                                                    if (Convert.ToString(dtdrroomCapacity[0]["maxAdult"]) != string.Empty)
                                                        AdultsCapacity = Convert.ToInt32(Convert.ToString(dtdrroomCapacity[0]["maxAdult"]) == "" ? "0" : (dtdrroomCapacity[0]["maxAdult"]));
                                                    ChildCapacity = Convert.ToInt32(Convert.ToString(dtdrroomCapacity[0]["maxChildren"]) == "" ? "0" : Convert.ToString(dtdrroomCapacity[0]["maxChildren"]));
                                                    MaxExtrabed = Convert.ToInt32(Convert.ToString(dtdrroomCapacity[0]["maxExtraBed"]) == "" ? "0" : dtdrroomCapacity[0]["maxExtraBed"]);
                                                }
                                                DataRow[] dtrateBases = DOTWResponse.Tables["rateBases"].Select("roomType_Id=" + dtRoomType_Id + "");
                                                int rateBases_Id = -1;
                                                if (dtrateBases.GetLength(0) > 0)
                                                    rateBases_Id = Convert.ToInt32(dtrateBases[0]["rateBases_Id"]);
                                                DataRow[] dtraterateBasis = DOTWResponse.Tables["rateBasis"].Select("rateBases_Id=" + rateBases_Id + "");

                                                for (int RateBasiscounter = 0; RateBasiscounter <= dtraterateBasis.GetLength(0) - 1; RateBasiscounter++)
                                                {
                                                    #region "Process Room Data"
                                                    int runno = Convert.ToInt32(dtraterateBasis[RateBasiscounter]["runno"]);
                                                    //MaxExtrabed = 0;
                                                    if (DOTWResponse.Tables["rateBasis"].Columns.Contains("paymentMode"))
                                                    {
                                                        string IsCreditRate = Convert.ToString(dtraterateBasis[RateBasiscounter]["paymentMode"]);
                                                        if (IsCreditRate != "")
                                                        {
                                                            // continue;
                                                        }
                                                    }
                                                    RoomTypeCount++;
                                                    LogicalCounter++;
                                                    //end change
                                                    int rateBasis_Id = Convert.ToInt32(dtraterateBasis[RateBasiscounter]["rateBasis_Id"]);
                                                    string ChangedOccupancy = "";
                                                    string ExtraName = "";
                                                    if (DOTWResponse.Tables["rateBasis"].Columns.Contains("changedoccupancy"))
                                                        ChangedOccupancy = Convert.ToString(dtraterateBasis[RateBasiscounter]["ChangedOccupancy"]);

                                                    if (ChangedOccupancy != "")
                                                    {
                                                        AdultsCapacity = Convert.ToInt32(ChangedOccupancy.Split(',')[0]);
                                                        ChildCapacity = Convert.ToInt32(ChangedOccupancy.Split(',')[1]);
                                                        MaxExtrabed = Convert.ToInt32(ChangedOccupancy.Split(',')[3]);
                                                        ExtraName += " [ChangedOccupancy]";
                                                    }
                                                    string allocationDetails = Convert.ToString(dtraterateBasis[RateBasiscounter]["allocationDetails"]);
                                                    string minStay = Convert.ToString(dtraterateBasis[RateBasiscounter]["minStay"]);
                                                    string DateApplyMinStay = Convert.ToString(dtraterateBasis[RateBasiscounter]["DateApplyMinStay"]);
                                                    string withCancellationDeadLine = Convert.ToString(dtraterateBasis[RateBasiscounter]["withINCancellationDeadLine"]);
                                                    string tariffNotes = Convert.ToString(dtraterateBasis[RateBasiscounter]["tariffNotes"]);
                                                    //Minimus stay policy added to Essentials information
                                                    if (minStay != string.Empty && minStay != "0")
                                                    {
                                                        tariffNotes = tariffNotes + " Minimum stay - " + minStay;
                                                        //if (ispackage == 1)
                                                        //{
                                                        //    continue;
                                                        //}
                                                    }
                                                    // tariffNotes = tariffNotes + minStay;
                                                    string IsBookable = Convert.ToString(dtraterateBasis[RateBasiscounter]["IsBookable"]);
                                                    string leftToSell = Convert.ToString(dtraterateBasis[RateBasiscounter]["leftToSell"]);
                                                    string id = Convert.ToString(dtraterateBasis[RateBasiscounter]["id"]);
                                                    //string description = Convert.ToString(dtraterateBasis[RateBasiscounter]["description"]);
                                                    string description = GetShortMealName(Convert.ToString(dtraterateBasis[RateBasiscounter]["description"]));
                                                    // if (description == "") // comment 03022025 //uncomment it
                                                    //     XMLHotelBookingCommon.LogMealName(Convert.ToString(System.Web.Configuration.WebConfigurationManager.AppSettings["LogMealNamePath"]), Convert.ToString(dtraterateBasis[RateBasiscounter]["description"]));
                                                    int rc = (dtroomtype + (RateBasiscounter));
                                                    DataRow[] DTRoomTotalAomunt = DOTWResponse.Tables["total"].Select("rateBasis_Id=" + rateBasis_Id + "");

                                                    if (IsB2C && DOTWResponse.Tables.Contains("totalMinimumSelling"))
                                                    {
                                                        DataRow[] DTRoomTotalAomuntMSP = DOTWResponse.Tables["totalMinimumSelling"].Select("rateBasis_Id=" + rateBasis_Id + "");
                                                        if (DTRoomTotalAomuntMSP.Length > 0)
                                                        {
                                                            DTRoomTotalAomunt = DTRoomTotalAomuntMSP;
                                                            ExtraName += " [minStay]";

                                                        }
                                                    }
                                                    string apr = "no";
                                                    if (DOTWResponse.Tables["rateType"].Columns.Contains("nonrefundable"))
                                                    {
                                                        DataRow[] DrrateType = DOTWResponse.Tables["rateType"].Select("rateBasis_Id=" + rateBasis_Id + "");
                                                        string nonrefundable = Convert.ToString(DrrateType[0]["nonrefundable"]);
                                                        if (nonrefundable.ToLower() == "yes")
                                                        {
                                                            ExtraName += " [APR]";
                                                            apr = "yes";
                                                        }
                                                    }
                                                    if (apr == "yes" && SearchParam.Rooms.Count > 1)
                                                        continue;
                                                    double ItemPrice = Math.Round(Convert.ToDouble(Convert.ToString(DTRoomTotalAomunt[0]["formatted"]).Replace(",", "")), 2);

                                                    //double CurrencyFactordotw = 1;
                                                    string dotw_currency = Convert.ToString(DOTWResponse.Tables["result"].Rows[0]["currencyShort"]);
                                                    if (dotw_currency.ToLower() != commonSearch.tokenData.SiteCurrency.ToLower()) // change systembase currency 
                                                    {
                                                        CurecncyFactor = _commonMethod.GetROEByCurrency(dotw_currency, DtROE);
                                                    }
                                                    ItemPrice = ItemPrice * CurecncyFactor;

                                                    double cancellationPrice = 0;
                                                    double VatonPolicy = 0;
                                                    if (minStay == "")
                                                        minStay = "0";
                                                    // string FinalDotwRoomTypeId = dotwroomtypecode + ":" + id + ":" + ChangedOccupancy + ":" + Guid.NewGuid().ToString().Substring(0, 5) + ":" + withCancellationDeadLine + ":" + apr;
                                                    string FinalDotwRoomTypeId = dotwroomtypecode + ":" + id + ":" + ChangedOccupancy + ":" + Guid.NewGuid().ToString().Substring(0, 5) + ":" + withCancellationDeadLine + ":" + apr + ":" + allocationDetails + ":" + rateBasis_Id + ":" + Item.XmlNationality; //04-NOV-24

                                                    ChangedOccupancy = "";
                                                    string uniqid = Guid.NewGuid().ToString();
                                                    string FinalDotwRoomTypeName = dotwRoomType + "-" + description;
                                                    double MainAgentCharge = ItemPrice;
                                                    if (withCancellationDeadLine.ToLower() == "yes")
                                                    {
                                                        FinalDotwRoomTypeName = FinalDotwRoomTypeName + " [Non Refundable]";
                                                        description = description + " [Non Refundable]";
                                                        //double b22ItemPriceMarkup = 0;
                                                        //if (isPerce)
                                                        //{
                                                        //	b22ItemPriceMarkup = Math.Round((Math.Round((Convert.ToDouble(ItemPrice) * AgentPercentage), 2) / 100), 2);
                                                        //}
                                                        //else
                                                        //	b22ItemPriceMarkup = AgentMarkupAmount;

                                                        //cancellationPrice = Math.Round(ItemPrice + b22ItemPriceMarkup, 2);
                                                        #region Vat

                                                        cancellationPrice = _markup.ProcessHotelCancelationPrice_WithMarkup(ref MainAgentCharge, SearchParam.NoofNights, SearchParam.CityID, SearchParam.CountryID.ToString(), cancellationPrice, isPerce, commonSearch.B2CMarkup, AgentPercentage, AgentMarkupAmount, commonSearch.IsParentId, commonSearch.SubAgentMarkup, ref VatonPolicy);
                                                        #endregion
                                                    }
                                                    string Offer_Text = "";
                                                    string offerdesc = "";
                                                    if (DOTWResponse.Tables.Contains("specialsApplied"))
                                                    {
                                                        DataRow[] drSpecialapplied = DOTWResponse.Tables["specialsApplied"].Select("rateBasis_Id=" + rateBasis_Id + "");

                                                        if (drSpecialapplied.Length > 0 && DOTWResponse.Tables["specialsApplied"].Columns.Contains("special"))
                                                        {
                                                            DataRow[] drOffer = DOTWResponse.Tables["specials"].Select("roomType_Id=" + dtRoomType_Id + "");
                                                            if (drOffer.Length > 0)
                                                            {
                                                                if (DOTWResponse.Tables["specials"].Columns.Contains("specials_Id"))
                                                                {

                                                                    for (int offercount = 0; offercount <= drOffer.GetLength(0) - 1; offercount++)
                                                                    {
                                                                        DataRow[] drOfferdetails = DOTWResponse.Tables["special"].Select("specials_Id='" + drOffer[offercount]["specials_Id"] + "' AND runno='" + drSpecialapplied[0]["special"] + "'");
                                                                        if (drOfferdetails.Length > 0)
                                                                        {
                                                                            Offer_Text = Convert.ToString(drOfferdetails[0]["specialName"]);
                                                                            if (DOTWResponse.Tables["special"].Columns.Contains("discountInfoText"))
                                                                                offerdesc = Convert.ToString(drOfferdetails[0]["specialName"]) + " (" + Convert.ToString(drOfferdetails[0]["discountInfoText"]) + ")";
                                                                            else
                                                                                offerdesc = Convert.ToString(drOfferdetails[0]["specialName"]);
                                                                        }
                                                                    }
                                                                }
                                                            }
                                                        }
                                                    }
                                                    #endregion
                                                    #region Comment Code
                                                    //try
                                                    //{
                                                    //    #region "new logic for rate breack"
                                                    //    //new logic for rate breack up 
                                                    //    // Meal Date wise for that room 
                                                    //    DataRow[] DTdrdates = DOTWResponse.Tables["dates"].Select("rateBasis_Id=" + rateBasis_Id + "");

                                                    //    int ActualNoOfNight = Convert.ToInt32(DOTWResponse.Tables["dates"].Rows[0]["count"]);
                                                    //    int NightSplitCounter = 0;
                                                    //    int datesId = Convert.ToInt32(DTdrdates[0]["dates_Id"]);
                                                    //    DataRow[] DTdrdate = DOTWResponse.Tables["date"].Select("dates_Id=" + datesId + "");
                                                    //    string WeekColumn = "";

                                                    //    int noofnight = Convert.ToInt32(SearchParam.NoofNights);
                                                    //    for (int dtdate = 0; dtdate <= DTdrdate.GetLength(0) - 1; dtdate++)
                                                    //    {
                                                    //        //if (dtdate +1>noofnight)
                                                    //        //{
                                                    //        //    continue;
                                                    //        //}
                                                    //        int date_Id = Convert.ToInt32(DTdrdate[dtdate]["date_Id"]);
                                                    //        string freeStay = Convert.ToString(DTdrdate[dtdate]["freeStay"]);
                                                    //        string datetime = Convert.ToString(DTdrdate[dtdate]["datetime"]);
                                                    //        string day = Convert.ToString(DTdrdate[dtdate]["day"]);
                                                    //        string wday = Convert.ToString(DTdrdate[dtdate]["wday"]);
                                                    //        if (dtdate == 0)
                                                    //        {
                                                    //            if (Convert.ToInt32(DTdrdate.GetLength(0)) > ActualNoOfNight)
                                                    //                ActualNoOfNight = DTdrdate.GetLength(0);
                                                    //            for (int ratetemplate = 0; ratetemplate <= (ActualNoOfNight > 7 ? 7 : ActualNoOfNight) - 1; ratetemplate++)
                                                    //                WeekColumn = Convert.ToString(Convert.ToDateTime(DTdrdate[dtdate]["datetime"]).Date.AddDays(ratetemplate).DayOfWeek).Substring(0, 3);
                                                    //        }
                                                    //        DataRow[] dayprice = DOTWResponse.Tables["price"].Select("date_Id=" + date_Id + "");

                                                    //        if (IsB2C && DOTWResponse.Tables.Contains("priceMinimumSelling"))
                                                    //        {
                                                    //            DataRow[] daypriceMSP = DOTWResponse.Tables["priceMinimumSelling"].Select("date_Id=" + date_Id + "");
                                                    //            if (daypriceMSP.Length > 0)
                                                    //            {
                                                    //                dayprice = daypriceMSP;
                                                    //            }
                                                    //        }
                                                    //        double DateGross = 0;
                                                    //        double VatonBrackup = 0;
                                                    //        if (freeStay == "no" || freeStay.Trim() == "")
                                                    //        {
                                                    //            if (dayprice.GetLength(0) > 0)
                                                    //                DateGross = Math.Round(Convert.ToDouble(dayprice[0]["formatted"].ToString().Replace(",", "")), 2);
                                                    //            #region Vat
                                                    //            DateGross = Common.ProcessHotelRateBreakupPrice_WithMarkup(1, SearchParam.CityID, SearchParam.CountryID.ToString(), DateGross, commonSearch.B2CMarkup, commonSearch.IsParentId, isPerce, AgentPercentage, AgentMarkupAmount, commonSearch.SubAgentMarkup, ref VatonBrackup);
                                                    //            #endregion
                                                    //            //double b2cDateGrossMarkup = Math.Round((Math.Round((DateGross * B2CMarkup), 2) / 100), 2);
                                                    //            //DateGross = Math.Round(DateGross + b2cDateGrossMarkup, 2);
                                                    //            //if (1 == 1)
                                                    //            //{
                                                    //            //	if (IsParentId == 0)
                                                    //            //	{
                                                    //            //		double b2bSubDateGrossMarkup = Math.Round((Math.Round((DateGross * SubAgentMarkup), 2) / 100), 2);
                                                    //            //		DateGross = Math.Round(DateGross + b2bSubDateGrossMarkup, 2);
                                                    //            //	}
                                                    //            //	if (isPerce)
                                                    //            //	{
                                                    //            //		double b2bDateGrossMarkup = Math.Round((Math.Round((DateGross * AgentPercentage), 2) / 100), 2);
                                                    //            //		DateGross = Math.Round(DateGross + b2bDateGrossMarkup, 2);
                                                    //            //	}
                                                    //            //	else
                                                    //            //		DateGross = (DateGross == 0 ? DateGross : Math.Round(DateGross + AgentMarkupAmount, 2));
                                                    //            //}
                                                    //        }

                                                    //        string MealCodeData = "";
                                                    //        string MealText = "";
                                                    //        DataRow[] dtdrMeal = DOTWResponse.Tables["including"].Select("date_Id=" + date_Id + "");
                                                    //        if (DOTWResponse.Tables["includedMeal"] != null)
                                                    //        {
                                                    //            DataRow[] dtdrincludedMeal = DOTWResponse.Tables["includedMeal"].Select("including_Id=" + Convert.ToInt32(dtdrMeal[0]["including_Id"]) + "");
                                                    //            if (dtdrincludedMeal.GetLength(0) > 0)
                                                    //            {
                                                    //                DataRow[] dtdrmealType = DOTWResponse.Tables["mealType"].Select("includedMeal_Id=" + Convert.ToInt32(dtdrincludedMeal[0]["includedMeal_Id"]) + "");
                                                    //                for (int intdtmeal = 0; intdtmeal <= dtdrmealType.GetLength(0) - 1; intdtmeal++)
                                                    //                {
                                                    //                    MealCodeData = Convert.ToString(dtdrmealType[intdtmeal]["code"]);
                                                    //                    MealText = Common.GetShortMealName(Convert.ToString(dtdrmealType[intdtmeal]["mealType_Text"]));
                                                    //                    if (MealText == "")
                                                    //                        XMLHotelBookingCommon.LogMealName(Convert.ToString(System.Web.Configuration.WebConfigurationManager.AppSettings["LogMealNamePath"]), Convert.ToString(dtdrmealType[intdtmeal]["mealType_Text"]));
                                                    //                }
                                                    //            }
                                                    //        }

                                                    //        DataRow drDOTWHotelRoomRateBreackUp = DtDOTWHotelRoomRateBreackUp.NewRow();
                                                    //        drDOTWHotelRoomRateBreackUp["RoomNo"] = Convert.ToString(dtroom + 1);
                                                    //        drDOTWHotelRoomRateBreackUp["SupplierName"] = "dotw";
                                                    //        drDOTWHotelRoomRateBreackUp["OwnsystemHotelId"] = ownsystemhotelId;
                                                    //        drDOTWHotelRoomRateBreackUp["ContractID"] = 0;
                                                    //        drDOTWHotelRoomRateBreackUp["RoomTypeName"] = FinalDotwRoomTypeName;
                                                    //        drDOTWHotelRoomRateBreackUp["RoomTypeId"] = FinalDotwRoomTypeId;
                                                    //        drDOTWHotelRoomRateBreackUp["MealId"] = FinalDotwRoomTypeName.Replace("'", " ") + ":" + RateBasiscounter;
                                                    //        drDOTWHotelRoomRateBreackUp["Meal"] = MealText;
                                                    //        drDOTWHotelRoomRateBreackUp["BookingStatus"] = "Available";
                                                    //        drDOTWHotelRoomRateBreackUp["DateOfWeek"] = Convert.ToString(Convert.ToDateTime(datetime).Date.DayOfWeek).Substring(0, 3);
                                                    //        drDOTWHotelRoomRateBreackUp["Date"] = datetime;
                                                    //        drDOTWHotelRoomRateBreackUp["Rate"] = DateGross;
                                                    //        drDOTWHotelRoomRateBreackUp["RateType"] = FinalDotwRoomTypeName;
                                                    //        drDOTWHotelRoomRateBreackUp["VatdiffAmount"] = VatonBrackup;
                                                    //        //drDOTWHotelRoomRateBreackUp["RateType"] = "A Rate";
                                                    //        drDOTWHotelRoomRateBreackUp["OrderNo"] = 1;
                                                    //        drDOTWHotelRoomRateBreackUp["RoomSequenceNo"] = uniqid;
                                                    //        drDOTWHotelRoomRateBreackUp["BoardTypeID"] = "";
                                                    //        // DtDOTWHotelRoomRateBreackUp.Rows.Add(drDOTWHotelRoomRateBreackUp);
                                                    //        drDOTWHotelRoomRateBreackUp["SupplierCurrency"] = Currency;
                                                    //        drDOTWHotelRoomRateBreackUp["BuyingPrice"] = DateGross / Factor;
                                                    //        DtDOTWHotelRoomRateBreackUp.Rows.Add(drDOTWHotelRoomRateBreackUp);
                                                    //        DtDOTWHotelRoomRateBreackUp.AcceptChanges();

                                                    //        NightSplitCounter++;
                                                    //    }
                                                    //    #endregion
                                                    //}
                                                    //catch (Exception ex)
                                                    //{
                                                    //    Common.Error(ex);
                                                    //}
                                                    #endregion
                                                    #region "Room cancellation logic old"
                                                    //// Room cancellation logic need to added for hotel dotw cancellation 
                                                    //if (DOTWResponse.Tables.Contains("Charge"))
                                                    //{
                                                    //    DataRow[] DTRoomcancellationRules = DOTWResponse.Tables["cancellationRules"].Select("rateBasis_Id=" + rateBasis_Id + "");
                                                    //    int cancellationRules_Id = Convert.ToInt32(DTRoomcancellationRules[0]["cancellationRules_Id"]);
                                                    //    //Cancellation Logic for dotw 

                                                    //    DataRow[] DTdrRoomcancellationRule = DOTWResponse.Tables["rule"].Select("cancellationRules_Id=" + cancellationRules_Id + "");
                                                    //    if (DOTWResponse.Tables["rule"].Columns.Contains("toDate") == false)
                                                    //    {
                                                    //        DOTWResponse.Tables["rule"].Columns.Add("toDate");
                                                    //    }

                                                    //    int intRoomCancellation = 0;

                                                    //    for (int dtc = 0; dtc <= DTdrRoomcancellationRule.GetLength(0) - 1; dtc++)
                                                    //    {
                                                    //        if (Convert.ToString(DTdrRoomcancellationRule[dtc]["fromDate"]).Trim() == "" || (Convert.ToDateTime(DTdrRoomcancellationRule[dtc]["fromDate"]) <= DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null).AddHours(23)))
                                                    //        {
                                                    //            int reducefromdate = 1;
                                                    //            if (Convert.ToString(DTdrRoomcancellationRule[dtc]["fromDate"]) == "")
                                                    //            {
                                                    //                DTdrRoomcancellationRule[dtc]["fromDate"] = DateTime.Now.Date;
                                                    //                if (DateTime.Now.Date >= Convert.ToDateTime(Convert.ToDateTime(DTdrRoomcancellationRule[dtc]["toDate"]).ToString("yyyy-MM-dd 00:00:00")))
                                                    //                    DTdrRoomcancellationRule[dtc]["fromDate"] = Convert.ToDateTime(DTdrRoomcancellationRule[dtc]["toDate"]).ToString("yyyy-MM-dd 00:00:00");
                                                    //                else
                                                    //                    reducefromdate = 0;
                                                    //            }

                                                    //            if (DTdrRoomcancellationRule[dtc]["toDate"] != null && Convert.ToString(DTdrRoomcancellationRule[dtc]["toDate"]) == "")
                                                    //            {
                                                    //                DateTime dtCheckInDate = DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null);
                                                    //                DTdrRoomcancellationRule[dtc]["toDate"] = dtCheckInDate;
                                                    //            }

                                                    //            DateTime dtFromDate = Convert.ToDateTime(DTdrRoomcancellationRule[dtc]["fromDate"]);
                                                    //            DateTime dtToDate = Convert.ToDateTime(DTdrRoomcancellationRule[dtc]["toDate"]);
                                                    //            if (reducefromdate == 1)
                                                    //                dtFromDate = dtFromDate.AddDays(CutOffDays);

                                                    //            if (dtc != DTdrRoomcancellationRule.GetLength(0) - 1)
                                                    //            {
                                                    //                if (dtFromDate <= dtToDate.AddDays(CutOffDays))
                                                    //                    dtToDate = dtToDate.AddDays(CutOffDays);
                                                    //                else
                                                    //                    dtToDate = dtFromDate;
                                                    //            }

                                                    //            if (dtc == DTdrRoomcancellationRule.GetLength(0) - 1)
                                                    //            {
                                                    //                DateTime dtCheckInDate = DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null);
                                                    //                if (dtToDate < dtCheckInDate)
                                                    //                    dtToDate = dtCheckInDate;
                                                    //            }
                                                    //            int rule_Id = Convert.ToInt32(DTdrRoomcancellationRule[dtc]["rule_Id"]);

                                                    //            DataRow[] DTdrRoomcancellationAmount1 = DOTWResponse.Tables["charge"].Select("rule_Id=" + rule_Id + "");
                                                    //            if (DTdrRoomcancellationAmount1.GetLength(0) == 0)
                                                    //            {
                                                    //                DataRow dradd = DOTWResponse.Tables["charge"].NewRow();
                                                    //                dradd["rule_Id"] = rule_Id;
                                                    //                DataRow[] drpreviousdata = DOTWResponse.Tables["charge"].Select("rule_Id=" + Convert.ToString(Convert.ToInt32(rule_Id) - 1) + "");
                                                    //                if (drpreviousdata.GetLength(0) > 0)
                                                    //                {
                                                    //                    dradd["formatted"] = drpreviousdata[0]["formatted"].ToString().Replace(",", "");
                                                    //                    DOTWResponse.Tables["charge"].Rows.Add(dradd);
                                                    //                    DOTWResponse.Tables["charge"].AcceptChanges();
                                                    //                }
                                                    //                else
                                                    //                {
                                                    //                    dradd["formatted"] = ItemPrice;
                                                    //                    DOTWResponse.Tables["charge"].Rows.Add(dradd);
                                                    //                    DOTWResponse.Tables["charge"].AcceptChanges();
                                                    //                }
                                                    //            }


                                                    //            DataRow[] DTdrRoomcancellationAmount = DOTWResponse.Tables["charge"].Select("rule_Id=" + rule_Id + "");
                                                    //            if (DTdrRoomcancellationAmount.GetLength(0) > 0)
                                                    //            {
                                                    //                string CancellationAmount = string.Empty;
                                                    //                DataRow dr = DTdrRoomcancellationRule[0];
                                                    //                if (dr.Table.Columns.Contains("cancelRestricted"))
                                                    //                {
                                                    //                    if (Convert.ToString(DTdrRoomcancellationRule[dtc]["cancelRestricted"]).ToLower() == "true")
                                                    //                        CancellationAmount = ItemPrice.ToString();
                                                    //                    else
                                                    //                    {
                                                    //                        CancellationAmount = Convert.ToString(DTdrRoomcancellationAmount[0]["formatted"]).Replace(",", "");

                                                    //                    }
                                                    //                }
                                                    //                else
                                                    //                {
                                                    //                    CancellationAmount = Convert.ToString(DTdrRoomcancellationAmount[0]["formatted"]).Replace(",", "");

                                                    //                }
                                                    //                double ChargeAmount = Math.Round(Convert.ToDouble(CancellationAmount), 2);
                                                    //                double CompanyChargeAmount = ChargeAmount;
                                                    //                #region Vat
                                                    //                double VatOnChargeAmount = 0;
                                                    //                double MainAgentChargeMain = ChargeAmount;
                                                    //                ChargeAmount = Common.ProcessHotelCancelationPrice_WithMarkup(ref MainAgentChargeMain, SearchParam.NoofNights, SearchParam.CityID, SearchParam.CountryID.ToString(), ChargeAmount, isPerce, commonSearch.B2CMarkup, AgentPercentage, AgentMarkupAmount, commonSearch.IsParentId, commonSearch.SubAgentMarkup, ref VatOnChargeAmount);
                                                    //                #endregion
                                                    //                //double AgentChargeAmount = 0;
                                                    //                //Double CancellationMarkup = 0.0;
                                                    //                //if (isPerce)
                                                    //                //{
                                                    //                //	if ((B2CMarkup == 0 || (AgentPercentage > 0)))
                                                    //                //		CancellationMarkup = Math.Round((Math.Round((ChargeAmount * AgentPercentage), 2) / 100), 2);
                                                    //                //}
                                                    //                //else
                                                    //                //	CancellationMarkup = (ChargeAmount == 0 ? ChargeAmount : AgentMarkupAmount);
                                                    //                //ChargeAmount = Math.Round(ChargeAmount + CancellationMarkup, 2);
                                                    //                //AgentChargeAmount = ChargeAmount;
                                                    //                //if (IsParentId == 0)
                                                    //                //{
                                                    //                //	Double SubCancellationMarkup = Math.Round(Math.Round((ChargeAmount * SubAgentMarkup), 2) / 100, 2);
                                                    //                //	ChargeAmount = Math.Round(ChargeAmount + SubCancellationMarkup, 2);
                                                    //                //}
                                                    //                //Double CancellationAgentMarkup = 0;
                                                    //                //ChargeAmount = Math.Round(ChargeAmount + CancellationAgentMarkup, 2);

                                                    //                if (withCancellationDeadLine.ToLower() == "yes" && ChargeAmount != 0)
                                                    //                {
                                                    //                    DateTime dtFromDate1 = Convert.ToDateTime(DTdrRoomcancellationRule[dtc]["fromDate"]);
                                                    //                    DateTime dtToDate1 = Convert.ToDateTime(DTdrRoomcancellationRule[dtc]["toDate"]);

                                                    //                    DataRow drDOTWHotelCancellation = dtCancellation.NewRow();
                                                    //                    drDOTWHotelCancellation["SupplierName"] = "dotw";
                                                    //                    drDOTWHotelCancellation["OwnsystemHotelId"] = OwnsystemHotelId;
                                                    //                    drDOTWHotelCancellation["RoomNo"] = Convert.ToString(dtroom + 1);
                                                    //                    drDOTWHotelCancellation["RoomTypeId"] = FinalDotwRoomTypeId;
                                                    //                    drDOTWHotelCancellation["MealId"] = FinalDotwRoomTypeName.Replace("'", " ") + ":" + RateBasiscounter;
                                                    //                    drDOTWHotelCancellation["FromDate"] = dtFromDate1.Date;
                                                    //                    drDOTWHotelCancellation["ToDate"] = dtToDate1.Date;//DateTime.ParseExact(objSearPera.CheckInDate, "dd/MM/yyyy", null);
                                                    //                    drDOTWHotelCancellation["CompanyCancellationPrice"] = MainAgentChargeMain;
                                                    //                    drDOTWHotelCancellation["AgentCancellationPrice"] = ChargeAmount;
                                                    //                    drDOTWHotelCancellation["VatdiffAmount"] = VatOnChargeAmount;

                                                    //                    drDOTWHotelCancellation["RoomTypeName"] = FinalDotwRoomTypeName;
                                                    //                    drDOTWHotelCancellation["RoomSequenceNo"] = uniqid;
                                                    //                    drDOTWHotelCancellation["WithInCancellationDeadLine"] = withCancellationDeadLine.ToLower();
                                                    //                    drDOTWHotelCancellation["SupplierCurrency"] = Currency;
                                                    //                    drDOTWHotelCancellation["BuyingPrice"] = CompanyChargeAmount;
                                                    //                    dtCancellation.Rows.Add(drDOTWHotelCancellation);
                                                    //                    dtCancellation.AcceptChanges();
                                                    //                }
                                                    //                else
                                                    //                {
                                                    //                    if (dtFromDate >= DateTime.Now.Date || dtToDate >= DateTime.Now.Date)
                                                    //                    {
                                                    //                        if (dtFromDate <= DateTime.Now.Date)
                                                    //                            dtFromDate = DateTime.Now.Date;
                                                    //                        bool postcanceldate = true;
                                                    //                        if (Convert.ToDateTime(dtFromDate).ToString("yyyy-MM-dd 00:00:00") == Convert.ToDateTime(dtToDate).ToString("yyyy-MM-dd 00:00:00") && Convert.ToDateTime(dtFromDate).ToString("yyyy-MM-dd 00:00:00") == DateTime.Now.Date.ToString("yyyy-MM-dd 00:00:00") && ChargeAmount == 0)
                                                    //                            postcanceldate = false;
                                                    //                        if (withCancellationDeadLine.ToLower() == "yes")
                                                    //                        {
                                                    //                            DataRow drDOTWHotelCancellation = dtCancellation.NewRow();
                                                    //                            drDOTWHotelCancellation["SupplierName"] = "dotw";
                                                    //                            drDOTWHotelCancellation["OwnsystemHotelId"] = OwnsystemHotelId;
                                                    //                            drDOTWHotelCancellation["RoomNo"] = Convert.ToString(dtroom + 1);
                                                    //                            drDOTWHotelCancellation["RoomTypeId"] = FinalDotwRoomTypeId;
                                                    //                            drDOTWHotelCancellation["MealId"] = FinalDotwRoomTypeName.Replace("'", " ") + ":" + RateBasiscounter;
                                                    //                            drDOTWHotelCancellation["FromDate"] = DateTime.Now.Date;
                                                    //                            drDOTWHotelCancellation["ToDate"] = DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null);
                                                    //                            drDOTWHotelCancellation["CompanyCancellationPrice"] = MainAgentCharge;
                                                    //                            drDOTWHotelCancellation["AgentCancellationPrice"] = cancellationPrice;
                                                    //                            drDOTWHotelCancellation["VatdiffAmount"] = VatonPolicy;

                                                    //                            drDOTWHotelCancellation["RoomTypeName"] = FinalDotwRoomTypeName;
                                                    //                            drDOTWHotelCancellation["RoomSequenceNo"] = uniqid;
                                                    //                            drDOTWHotelCancellation["WithInCancellationDeadLine"] = withCancellationDeadLine.ToLower();
                                                    //                            drDOTWHotelCancellation["SupplierCurrency"] = Currency;
                                                    //                            drDOTWHotelCancellation["BuyingPrice"] = CompanyChargeAmount;
                                                    //                            dtCancellation.Rows.Add(drDOTWHotelCancellation);
                                                    //                            dtCancellation.AcceptChanges();
                                                    //                        }
                                                    //                        else
                                                    //                        {
                                                    //                            if (postcanceldate == true)
                                                    //                            {
                                                    //                                intRoomCancellation++;
                                                    //                                int entryflag = 1;
                                                    //                                if (withCancellationDeadLine.ToLower() == "yes" && ChargeAmount == 0)
                                                    //                                    entryflag = 0;
                                                    //                                if (entryflag == 1)
                                                    //                                {
                                                    //                                    DataRow drDOTWHotelCancellation = dtCancellation.NewRow();
                                                    //                                    drDOTWHotelCancellation["SupplierName"] = "dotw";
                                                    //                                    drDOTWHotelCancellation["OwnsystemHotelId"] = OwnsystemHotelId;
                                                    //                                    drDOTWHotelCancellation["RoomNo"] = Convert.ToString(dtroom + 1);
                                                    //                                    drDOTWHotelCancellation["RoomTypeId"] = FinalDotwRoomTypeId;
                                                    //                                    drDOTWHotelCancellation["MealId"] = FinalDotwRoomTypeName.Replace("'", " ") + ":" + RateBasiscounter;
                                                    //                                    drDOTWHotelCancellation["FromDate"] = dtFromDate.Date;
                                                    //                                    drDOTWHotelCancellation["ToDate"] = dtToDate.Date.AddDays(-1).AddHours(23).AddMinutes(59).AddSeconds(59);
                                                    //                                    drDOTWHotelCancellation["CompanyCancellationPrice"] = MainAgentChargeMain;
                                                    //                                    drDOTWHotelCancellation["AgentCancellationPrice"] = ChargeAmount;
                                                    //                                    drDOTWHotelCancellation["VatdiffAmount"] = VatOnChargeAmount;
                                                    //                                    drDOTWHotelCancellation["RoomTypeName"] = FinalDotwRoomTypeName;
                                                    //                                    drDOTWHotelCancellation["RoomSequenceNo"] = uniqid;
                                                    //                                    drDOTWHotelCancellation["BoardTypeID"] = "";
                                                    //                                    drDOTWHotelCancellation["WithInCancellationDeadLine"] = withCancellationDeadLine.ToLower();
                                                    //                                    drDOTWHotelCancellation["SupplierCurrency"] = Currency;
                                                    //                                    drDOTWHotelCancellation["BuyingPrice"] = CompanyChargeAmount;
                                                    //                                    dtCancellation.Rows.Add(drDOTWHotelCancellation);
                                                    //                                    dtCancellation.AcceptChanges();
                                                    //                                }
                                                    //                            }
                                                    //                        }
                                                    //                    }
                                                    //                }
                                                    //            }
                                                    //        }
                                                    //    }
                                                    //}

                                                    #endregion

                                                    #region "Room cancellation logic"
                                                    // Room cancellation logic need to added for hotel dotw cancellation 
                                                    if (DOTWResponse.Tables.Contains("Charge"))
                                                    {
                                                        DataRow[] DTRoomcancellationRules = DOTWResponse.Tables["cancellationRules"].Select("rateBasis_Id=" + rateBasis_Id + "");
                                                        int cancellationRules_Id = Convert.ToInt32(DTRoomcancellationRules[0]["cancellationRules_Id"]);
                                                        //Cancellation Logic for dotw 

                                                        DataRow[] DTdrRoomcancellationRule = DOTWResponse.Tables["rule"].Select("cancellationRules_Id=" + cancellationRules_Id + "");
                                                        if (DOTWResponse.Tables["rule"].Columns.Contains("toDate") == false)
                                                        {
                                                            DOTWResponse.Tables["rule"].Columns.Add("toDate");
                                                        }

                                                        int intRoomCancellation = 0;

                                                        for (int dtc = 0; dtc <= DTdrRoomcancellationRule.GetLength(0) - 1; dtc++)
                                                        {
                                                            if (dtc < 2)
                                                            {
                                                                if (Convert.ToString(DTdrRoomcancellationRule[dtc]["fromDate"]).Trim() == "" || (Convert.ToDateTime(DTdrRoomcancellationRule[dtc]["fromDate"]) <= DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null).AddHours(23)))
                                                                {
                                                                    int reducefromdate = 1;
                                                                    if (Convert.ToString(DTdrRoomcancellationRule[dtc]["fromDate"]) == "")
                                                                    {
                                                                        DTdrRoomcancellationRule[dtc]["fromDate"] = DateTime.Now.Date;
                                                                        if (DateTime.Now.Date >= Convert.ToDateTime(Convert.ToDateTime(DTdrRoomcancellationRule[dtc]["toDate"]).ToString("yyyy-MM-dd 00:00:00")))
                                                                            DTdrRoomcancellationRule[dtc]["fromDate"] = Convert.ToDateTime(DTdrRoomcancellationRule[dtc]["toDate"]).ToString("yyyy-MM-dd 00:00:00");
                                                                        else
                                                                            reducefromdate = 0;
                                                                    }

                                                                    if (DTdrRoomcancellationRule[dtc]["toDate"] != null && Convert.ToString(DTdrRoomcancellationRule[dtc]["toDate"]) == "")
                                                                    {
                                                                        DateTime dtCheckInDate = DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null);
                                                                        DTdrRoomcancellationRule[dtc]["toDate"] = dtCheckInDate;
                                                                    }

                                                                    DateTime dtFromDate = Convert.ToDateTime(DTdrRoomcancellationRule[dtc]["fromDate"]);
                                                                    DateTime dtToDate = Convert.ToDateTime(DTdrRoomcancellationRule[dtc]["toDate"]);
                                                                    if (reducefromdate == 1)
                                                                        dtFromDate = dtFromDate.AddDays(CutOffDays);

                                                                    if (dtc != DTdrRoomcancellationRule.GetLength(0) - 1)
                                                                    {
                                                                        if (dtFromDate <= dtToDate.AddDays(CutOffDays))
                                                                            dtToDate = dtToDate.AddDays(CutOffDays);
                                                                        else
                                                                            dtToDate = dtFromDate;
                                                                    }

                                                                    if (dtc == DTdrRoomcancellationRule.GetLength(0) - 1)
                                                                    {
                                                                        DateTime dtCheckInDate = DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null);
                                                                        if (dtToDate < dtCheckInDate)
                                                                            dtToDate = dtCheckInDate;
                                                                    }
                                                                    int rule_Id = Convert.ToInt32(DTdrRoomcancellationRule[dtc]["rule_Id"]);

                                                                    DataRow[] DTdrRoomcancellationAmount1 = DOTWResponse.Tables["charge"].Select("rule_Id=" + rule_Id + "");
                                                                    if (DTdrRoomcancellationAmount1.GetLength(0) == 0)
                                                                    {
                                                                        DataRow dradd = DOTWResponse.Tables["charge"].NewRow();
                                                                        dradd["rule_Id"] = rule_Id;
                                                                        DataRow[] drpreviousdata = DOTWResponse.Tables["charge"].Select("rule_Id=" + Convert.ToString(Convert.ToInt32(rule_Id) - 1) + "");
                                                                        if (drpreviousdata.GetLength(0) > 0)
                                                                        {
                                                                            dradd["formatted"] = drpreviousdata[0]["formatted"].ToString().Replace(",", "");
                                                                            DOTWResponse.Tables["charge"].Rows.Add(dradd);
                                                                            DOTWResponse.Tables["charge"].AcceptChanges();
                                                                        }
                                                                        else
                                                                        {
                                                                            dradd["formatted"] = ItemPrice;
                                                                            DOTWResponse.Tables["charge"].Rows.Add(dradd);
                                                                            DOTWResponse.Tables["charge"].AcceptChanges();
                                                                        }
                                                                    }


                                                                    DataRow[] DTdrRoomcancellationAmount = DOTWResponse.Tables["charge"].Select("rule_Id=" + rule_Id + "");
                                                                    if (DTdrRoomcancellationAmount.GetLength(0) > 0)
                                                                    {
                                                                        string CancellationAmount = string.Empty;
                                                                        DataRow dr = DTdrRoomcancellationRule[0];
                                                                        if (dr.Table.Columns.Contains("cancelRestricted"))
                                                                        {
                                                                            if (Convert.ToString(DTdrRoomcancellationRule[dtc]["cancelRestricted"]).ToLower() == "true")
                                                                                CancellationAmount = ItemPrice.ToString();
                                                                            else
                                                                            {
                                                                                if (dtc == 0)
                                                                                {
                                                                                    if (withCancellationDeadLine.ToLower() != "yes")
                                                                                        CancellationAmount = Convert.ToString(DTdrRoomcancellationAmount[0]["formatted"]).Replace(",", "");
                                                                                    else
                                                                                        CancellationAmount = ItemPrice.ToString();
                                                                                }
                                                                                else
                                                                                    CancellationAmount = ItemPrice.ToString();
                                                                                //CancellationAmount = ItemPrice.ToString(); //Convert.ToString(DTdrRoomcancellationAmount[0]["formatted"]).Replace(",", "");

                                                                            }
                                                                        }
                                                                        else //Added on 22-AUG-2025
                                                                        {
                                                                            if (dtc == 0)
                                                                            {
                                                                                if (withCancellationDeadLine.ToLower() != "yes")
                                                                                    CancellationAmount = Convert.ToString(DTdrRoomcancellationAmount[0]["formatted"]).Replace(",", "");
                                                                                else
                                                                                    CancellationAmount = ItemPrice.ToString();
                                                                            }
                                                                            else
                                                                                CancellationAmount = ItemPrice.ToString();
                                                                            //CancellationAmount = ItemPrice.ToString(); //Convert.ToString(DTdrRoomcancellationAmount[0]["formatted"]).Replace(",", "");

                                                                        }
                                                                        //else
                                                                        //{
                                                                        //    CancellationAmount = ItemPrice.ToString(); //Convert.ToString(DTdrRoomcancellationAmount[0]["formatted"]).Replace(",", "");

                                                                        //}
                                                                        double ChargeAmount = Math.Round(Convert.ToDouble(CancellationAmount), 2);
                                                                        double CompanyChargeAmount = ChargeAmount;
                                                                        #region Vat
                                                                        double VatOnChargeAmount = 0;
                                                                        double MainAgentChargeMain = ChargeAmount;
                                                                        ChargeAmount = _markup.ProcessHotelCancelationPrice_WithMarkup(ref MainAgentChargeMain, SearchParam.NoofNights, SearchParam.CityID, SearchParam.CountryID.ToString(), ChargeAmount, isPerce, commonSearch.B2CMarkup, AgentPercentage, AgentMarkupAmount, commonSearch.IsParentId, commonSearch.SubAgentMarkup, ref VatOnChargeAmount);
                                                                        #endregion
                                                                        //double AgentChargeAmount = 0;
                                                                        //Double CancellationMarkup = 0.0;
                                                                        //if (isPerce)
                                                                        //{
                                                                        //	if ((B2CMarkup == 0 || (AgentPercentage > 0)))
                                                                        //		CancellationMarkup = Math.Round((Math.Round((ChargeAmount * AgentPercentage), 2) / 100), 2);
                                                                        //}
                                                                        //else
                                                                        //	CancellationMarkup = (ChargeAmount == 0 ? ChargeAmount : AgentMarkupAmount);
                                                                        //ChargeAmount = Math.Round(ChargeAmount + CancellationMarkup, 2);
                                                                        //AgentChargeAmount = ChargeAmount;
                                                                        //if (IsParentId == 0)
                                                                        //{
                                                                        //	Double SubCancellationMarkup = Math.Round(Math.Round((ChargeAmount * SubAgentMarkup), 2) / 100, 2);
                                                                        //	ChargeAmount = Math.Round(ChargeAmount + SubCancellationMarkup, 2);
                                                                        //}
                                                                        //Double CancellationAgentMarkup = 0;
                                                                        //ChargeAmount = Math.Round(ChargeAmount + CancellationAgentMarkup, 2);

                                                                        if (withCancellationDeadLine.ToLower() == "yes" && ChargeAmount != 0)
                                                                        {
                                                                            DateTime dtFromDate1 = Convert.ToDateTime(DTdrRoomcancellationRule[dtc]["fromDate"]);
                                                                            DateTime dtToDate1 = Convert.ToDateTime(DTdrRoomcancellationRule[dtc]["toDate"]);

                                                                            if (DateTime.Now.Date > dtToDate1)
                                                                            {
                                                                                dtToDate1 = DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null);
                                                                            }

                                                                            DataRow drDOTWHotelCancellation = dtCancellation.NewRow();
                                                                            drDOTWHotelCancellation["SupplierName"] = Provider;
                                                                            drDOTWHotelCancellation["OwnsystemHotelId"] = OwnsystemHotelId;
                                                                            drDOTWHotelCancellation["RoomNo"] = Convert.ToString(dtroom + 1);
                                                                            drDOTWHotelCancellation["RoomTypeId"] = FinalDotwRoomTypeId;
                                                                            drDOTWHotelCancellation["MealId"] = FinalDotwRoomTypeName.Replace("'", " ") + ":" + RateBasiscounter;
                                                                            drDOTWHotelCancellation["FromDate"] = DateTime.Now.Date; //dtFromDate1.Date;
                                                                            drDOTWHotelCancellation["ToDate"] = dtToDate1.Date;//DateTime.ParseExact(objSearPera.CheckInDate, "dd/MM/yyyy", null);
                                                                            drDOTWHotelCancellation["CompanyCancellationPrice"] = MainAgentChargeMain;
                                                                            drDOTWHotelCancellation["AgentCancellationPrice"] = ChargeAmount;
                                                                            drDOTWHotelCancellation["VatdiffAmount"] = VatOnChargeAmount;

                                                                            drDOTWHotelCancellation["RoomTypeName"] = FinalDotwRoomTypeName;
                                                                            drDOTWHotelCancellation["RoomSequenceNo"] = uniqid;
                                                                            drDOTWHotelCancellation["WithInCancellationDeadLine"] = withCancellationDeadLine.ToLower();
                                                                            drDOTWHotelCancellation["SupplierCurrency"] = Currency;
                                                                            drDOTWHotelCancellation["BuyingPrice"] = CompanyChargeAmount;
                                                                            dtCancellation.Rows.Add(drDOTWHotelCancellation);
                                                                            dtCancellation.AcceptChanges();
                                                                        }
                                                                        else
                                                                        {
                                                                            if (dtFromDate >= DateTime.Now.Date || dtToDate >= DateTime.Now.Date)
                                                                            {
                                                                                if (dtFromDate <= DateTime.Now.Date)
                                                                                    dtFromDate = DateTime.Now.Date;
                                                                                bool postcanceldate = true;
                                                                                if (Convert.ToDateTime(dtFromDate).ToString("yyyy-MM-dd 00:00:00") == Convert.ToDateTime(dtToDate).ToString("yyyy-MM-dd 00:00:00") && Convert.ToDateTime(dtFromDate).ToString("yyyy-MM-dd 00:00:00") == DateTime.Now.Date.ToString("yyyy-MM-dd 00:00:00") && ChargeAmount == 0)
                                                                                    postcanceldate = false;
                                                                                if (withCancellationDeadLine.ToLower() == "yes")
                                                                                {
                                                                                    DataRow drDOTWHotelCancellation = dtCancellation.NewRow();
                                                                                    drDOTWHotelCancellation["SupplierName"] = Provider;
                                                                                    drDOTWHotelCancellation["OwnsystemHotelId"] = OwnsystemHotelId;
                                                                                    drDOTWHotelCancellation["RoomNo"] = Convert.ToString(dtroom + 1);
                                                                                    drDOTWHotelCancellation["RoomTypeId"] = FinalDotwRoomTypeId;
                                                                                    drDOTWHotelCancellation["MealId"] = FinalDotwRoomTypeName.Replace("'", " ") + ":" + RateBasiscounter;
                                                                                    drDOTWHotelCancellation["FromDate"] = DateTime.Now.Date;
                                                                                    drDOTWHotelCancellation["ToDate"] = DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null);
                                                                                    drDOTWHotelCancellation["CompanyCancellationPrice"] = MainAgentCharge;
                                                                                    drDOTWHotelCancellation["AgentCancellationPrice"] = cancellationPrice;
                                                                                    drDOTWHotelCancellation["VatdiffAmount"] = VatonPolicy;

                                                                                    drDOTWHotelCancellation["RoomTypeName"] = FinalDotwRoomTypeName;
                                                                                    drDOTWHotelCancellation["RoomSequenceNo"] = uniqid;
                                                                                    drDOTWHotelCancellation["WithInCancellationDeadLine"] = withCancellationDeadLine.ToLower();
                                                                                    drDOTWHotelCancellation["SupplierCurrency"] = Currency;
                                                                                    drDOTWHotelCancellation["BuyingPrice"] = CompanyChargeAmount;
                                                                                    dtCancellation.Rows.Add(drDOTWHotelCancellation);
                                                                                    dtCancellation.AcceptChanges();
                                                                                }
                                                                                else
                                                                                {
                                                                                    if (postcanceldate == true)
                                                                                    {
                                                                                        intRoomCancellation++;
                                                                                        int entryflag = 1;
                                                                                        if (dtc != 0)
                                                                                            dtToDate = DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null);
                                                                                        else
                                                                                            dtToDate = dtToDate.Date.AddDays(-1).AddHours(23).AddMinutes(59).AddSeconds(59);
                                                                                        if (withCancellationDeadLine.ToLower() == "yes" && ChargeAmount == 0)
                                                                                            entryflag = 0;
                                                                                        if (entryflag == 1)
                                                                                        {
                                                                                            DataRow drDOTWHotelCancellation = dtCancellation.NewRow();
                                                                                            drDOTWHotelCancellation["SupplierName"] = Provider;
                                                                                            drDOTWHotelCancellation["OwnsystemHotelId"] = OwnsystemHotelId;
                                                                                            drDOTWHotelCancellation["RoomNo"] = Convert.ToString(dtroom + 1);
                                                                                            drDOTWHotelCancellation["RoomTypeId"] = FinalDotwRoomTypeId;
                                                                                            drDOTWHotelCancellation["MealId"] = FinalDotwRoomTypeName.Replace("'", " ") + ":" + RateBasiscounter;
                                                                                            drDOTWHotelCancellation["FromDate"] = dtFromDate.Date;
                                                                                            drDOTWHotelCancellation["ToDate"] = dtToDate;// dtToDate.Date.AddDays(-1).AddHours(23).AddMinutes(59).AddSeconds(59);
                                                                                            drDOTWHotelCancellation["CompanyCancellationPrice"] = MainAgentChargeMain;
                                                                                            drDOTWHotelCancellation["AgentCancellationPrice"] = ChargeAmount;
                                                                                            drDOTWHotelCancellation["VatdiffAmount"] = VatOnChargeAmount;
                                                                                            drDOTWHotelCancellation["RoomTypeName"] = FinalDotwRoomTypeName;
                                                                                            drDOTWHotelCancellation["RoomSequenceNo"] = uniqid;
                                                                                            drDOTWHotelCancellation["BoardTypeID"] = "";
                                                                                            drDOTWHotelCancellation["WithInCancellationDeadLine"] = withCancellationDeadLine.ToLower();
                                                                                            drDOTWHotelCancellation["SupplierCurrency"] = Currency;
                                                                                            drDOTWHotelCancellation["BuyingPrice"] = CompanyChargeAmount;
                                                                                            dtCancellation.Rows.Add(drDOTWHotelCancellation);
                                                                                            dtCancellation.AcceptChanges();
                                                                                        }
                                                                                    }
                                                                                }
                                                                            }
                                                                        }
                                                                    }
                                                                }
                                                            }
                                                            else
                                                                continue;
                                                        }
                                                    }

                                                    #endregion

                                                    #region "Room Details"
                                                    DataRow drDOTWHotelRoom = dtHotelRoom.NewRow();

                                                    drDOTWHotelRoom["OwnsystemHotelId"] = OwnsystemHotelId;
                                                    drDOTWHotelRoom["RoomTypeCode"] = dotwroomtypecode;
                                                    drDOTWHotelRoom["ContractID"] = 0;
                                                    drDOTWHotelRoom["UniqueNo"] = OwnsystemHotelId;
                                                    drDOTWHotelRoom["RoomSequenceNo"] = uniqid;
                                                    drDOTWHotelRoom["MealName"] = description;
                                                    drDOTWHotelRoom["allocationDetails"] = allocationDetails;
                                                    drDOTWHotelRoom["minStay"] = minStay;
                                                    drDOTWHotelRoom["DateApplyMinStay"] = DateApplyMinStay;
                                                    drDOTWHotelRoom["MealId"] = FinalDotwRoomTypeName.Replace("'", " ") + ":" + RateBasiscounter;
                                                    drDOTWHotelRoom["RoomNo"] = Convert.ToString(dtroom + 1);
                                                    if (ExtraName != "")
                                                    {
                                                        tariffNotes = tariffNotes + " " + ExtraName;
                                                        //dotwRoomType += " " + ExtraName;
                                                    }
                                                    drDOTWHotelRoom["EssentialInformation"] = tariffNotes;
                                                    drDOTWHotelRoom["MealId"] = FinalDotwRoomTypeName.Replace("'", " ") + ":" + RateBasiscounter;
                                                    drDOTWHotelRoom["RoomTypeName"] = dotwRoomType + ExtraName;
                                                    drDOTWHotelRoom["RoomTypeId"] = FinalDotwRoomTypeId;
                                                    drDOTWHotelRoom["BookingStatus"] = "Available";
                                                    drDOTWHotelRoom["AdultsOccupancy"] = AdultsCapacity;
                                                    drDOTWHotelRoom["ChildOccupancy"] = ChildCapacity;
                                                    drDOTWHotelRoom["MaxExtrabed"] = MaxExtrabed;
                                                    drDOTWHotelRoom["BoardTypeID"] = "";
                                                    drDOTWHotelRoom["CompanyBuyingPrice"] = ItemPrice;


                                                    drDOTWHotelRoom["OfferDescription"] = offerdesc;
                                                    drDOTWHotelRoom["offerCode"] = Offer_Text;
                                                    double AgentBuyingPrice = ItemPrice;


                                                    #region Vat
                                                    _markup.ProcessHotelRoom_WithMarkup(CurecncyFactor, Currency, false, false, SearchParam.NoofNights, SearchParam.CityID, SearchParam.CountryID.ToString(), SearchParam.CheckInDate, SearchParam.CheckOutDate, ref ItemPrice, ref drDOTWHotelRoom, isPerce, AgentPercentage, AgentMarkupAmount, commonSearch.SubAgentMarkup, commonSearch.B2CMarkup, commonSearch.IsParentId);
                                                    #endregion

                                                    #region OLD Code
                                                    //double b2cItemPriceMarkup = 0;
                                                    //if (isPerce)
                                                    //{
                                                    //    b2cItemPriceMarkup = Math.Round((Math.Round((Convert.ToDouble(ItemPrice) * AgentPercentage), 2) / 100), 2);
                                                    //}
                                                    //else
                                                    //    b2cItemPriceMarkup = AgentMarkupAmount;

                                                    //ItemPrice = Math.Round(ItemPrice + b2cItemPriceMarkup, 2);

                                                    //drDOTWHotelRoom["CompanySellingPrice"] = ItemPrice;

                                                    //if (1 == 1)
                                                    //{
                                                    //    if (IsParentId == 0)
                                                    //    {
                                                    //        drDOTWHotelRoom["AgentBuyingPrice"] = ItemPrice;
                                                    //        double subb2bMarkup = Math.Round((Math.Round((ItemPrice * SubAgentMarkup), 2) / 100), 2);
                                                    //        ItemPrice = Math.Round(ItemPrice + subb2bMarkup, 2);
                                                    //        drDOTWHotelRoom["AgentSellingPrice"] = ItemPrice;
                                                    //    }

                                                    //    if (IsParentId == 1)
                                                    //        drDOTWHotelRoom["AgentBuyingPrice"] = ItemPrice;
                                                    //    else
                                                    //        drDOTWHotelRoom["SubAgentBuyingPice"] = ItemPrice;

                                                    //    double b2bMarkup = Math.Round((Math.Round((ItemPrice * B2CMarkup), 2) / 100), 2);
                                                    //    ItemPrice = Math.Round(ItemPrice + b2bMarkup, 2);
                                                    //    if (IsParentId == 1)
                                                    //    {
                                                    //        drDOTWHotelRoom["AgentSellingPrice"] = ItemPrice;
                                                    //    }
                                                    //    else
                                                    //        drDOTWHotelRoom["SubAgentSellingPrice"] = ItemPrice;
                                                    //}

                                                    //drDOTWHotelRoom["FinalSellingPrice"] = ItemPrice;
                                                    #endregion

                                                    drDOTWHotelRoom["SupplierName"] = Provider;
                                                    dtHotelRoom.Rows.Add(drDOTWHotelRoom);
                                                    dtHotelRoom.AcceptChanges();
                                                    #endregion
                                                    try //if no cancellation data found
                                                    {
                                                        DataRow[] drcancellationCheck = dtCancellation.Select("RoomNo='" + Convert.ToString(dtroom + 1) + "' and  OwnsystemHotelId='" + OwnsystemHotelId + "' and RoomTypeId='" + FinalDotwRoomTypeId + "'");
                                                        if (drcancellationCheck.GetLength(0) == 0)
                                                        {
                                                            DataRow drDOTWHotelCancellation = dtCancellation.NewRow();
                                                            drDOTWHotelCancellation["SupplierName"] = Provider;
                                                            drDOTWHotelCancellation["OwnsystemHotelId"] = OwnsystemHotelId;
                                                            drDOTWHotelCancellation["RoomNo"] = Convert.ToString(dtroom + 1);
                                                            drDOTWHotelCancellation["RoomSequenceNo"] = uniqid;
                                                            drDOTWHotelCancellation["RoomTypeId"] = FinalDotwRoomTypeId;
                                                            drDOTWHotelCancellation["RoomTypeName"] = FinalDotwRoomTypeName;
                                                            drDOTWHotelCancellation["MealId"] = FinalDotwRoomTypeName.Replace("'", " ") + ":" + RateBasiscounter;
                                                            drDOTWHotelCancellation["FromDate"] = DateTime.Now.Date;
                                                            drDOTWHotelCancellation["ToDate"] = DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null);
                                                            drDOTWHotelCancellation["CompanyCancellationPrice"] = AgentBuyingPrice;
                                                            drDOTWHotelCancellation["AgentCancellationPrice"] = AgentBuyingPrice;
                                                            drDOTWHotelCancellation["WithInCancellationDeadLine"] = "yes";
                                                            drDOTWHotelCancellation["VatdiffAmount"] = VatonPolicy;
                                                            drDOTWHotelCancellation["BoardTypeID"] = "";
                                                            drDOTWHotelCancellation["SupplierCurrency"] = Currency;
                                                            drDOTWHotelCancellation["BuyingPrice"] = AgentBuyingPrice;
                                                            dtCancellation.Rows.Add(drDOTWHotelCancellation);
                                                            dtCancellation.AcceptChanges();
                                                        }
                                                    }
                                                    catch (Exception ex)
                                                    {
                                                        await _exception.Error(ex);
                                                    }

                                                }
                                            }
                                        }
                                    }
                                    #endregion

                                }
                                #endregion
                                Options ops = (await _supplierRes.GetCommonRoomForSuppliers(GiataId, commonSearch.tokenData, commonSearch.advancedOptions, MinPrice, drXmlHotel, dtHotelRoom, dtCancellation, DtRoomRateBreackUp, SearchParam));
                                //Options ops = (await _supplierRes.GetCommonRoomForSuppliers(GiataId, commonSearch.tokenData, commonSearch.advancedOptions, FinalMinPrice, drXmlHotel, dtHotelRoom, dtCancellation, DtRoomRateBreackUp, SearchParam));
                                if (ops != null)
                                    options.Add(ops);
                            }
                            #endregion
                            #endregion
                        }

                        #endregion




                    }

                }

                // DtHotel.DefaultView.RowFilter = "FinalSellingPrice>0";
                // if (DtHotel.DefaultView.Count > 0)
                return options;//.DefaultView.ToTable();
                               // else
                               //  return Common.GenerateDataTableNewV2();
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
                        SearchParam.SupplierTime += ",|Finish:Dotw Process Response  " + "|Time :" + timerroomx.Elapsed.ToString();
                        _ = Task.Run(() => _commonMethod.FireAway_SQL_ResPorcessTimeLog("", timerroomx.Elapsed.ToString(), "Dotw", SearchParam.SearchKey, 1, SearchParam.AgentID));
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

        public string GetShortMealName(string lognMealName)
        {
            try
            {
                string shormealname = string.Empty;
                switch (lognMealName.ToLower())
                {
                    case "room only":
                        shormealname = "RO";
                        break;
                    case "half board":
                    case "full breakfast":
                    case "boxed breakfast and dinner":
                        shormealname = "HB";
                        break;
                    case "full board":
                    case "full breakfast and dinner":
                    case "full breakfast, lunch and dinner":
                    case "breakfast, lunch and dinner":
                        shormealname = "FB";
                        break;
                    case "bed & breakfast":
                    case "bed and breakfast":
                    case "breakfast":
                    case "breakfast - buffet":
                    case "breakfast buffet":
                    case "buffet breakfast"://
                    case "room and breakfast":
                    case "english breakfast":
                        shormealname = "BB";
                        break;
                    case "all inclusive":
                        shormealname = "ALL INCL";
                        break;
                    default:
                        shormealname = lognMealName;
                        break;
                }
                return shormealname;
            }
            catch (Exception ex)
            {
            }
            return lognMealName;
        }

        public string getRooms_DotwBooking_Preebook(DataSet dsCartDetails, int i, int j)
        {
            try
            {
                StringBuilder sbDOTWBlockReq = new StringBuilder();

                sbDOTWBlockReq.Append("<room runno=\"" + j + "\">");
                sbDOTWBlockReq.Append(getDOTWAdultChildNationalityStringV3_preebook(dsCartDetails, i, j));
                //sbDOTWBlockReq.Append("<passengersDetails>");
                // sbDOTWBlockReq.Append("</passengersDetails>");
                sbDOTWBlockReq.Append("</room>");
                return sbDOTWBlockReq.ToString();
            }
            catch (Exception ex)
            {
                //ExceptionManager.LogException(ex);
                _ = _exception.Error(ex);
            }
            return string.Empty;
        }

        public string getDOTWAdultChildNationalityStringV3_preebook(DataSet dsCartDetails, int i, int j)
        {
            try
            {
                string roomid = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomTypeID"]);
                string[] spitid = roomid.Split(new String[] { ":" }, StringSplitOptions.None);
                //drDarinaHotelCancellation["PurchaseToken"] = RequestID + "$$" + Acc_SerialId + "$$" + CompanyBuyingPrice;


                string dotwroomtypecode = spitid[0].ToString();
                string allocationDetails = spitid[6].ToString();
                string Nationality = spitid[8].ToString();//added
                int ratebasis = Convert.ToInt32(spitid[1]);
                StringBuilder sbDOTWBlockReq = new StringBuilder();
                sbDOTWBlockReq.Append("<adultsCode>" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["NoOfAdults"]) + "</adultsCode>");
                sbDOTWBlockReq.Append("<children no=\"" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["NoOfChild"]) + "\">");

                if (Convert.ToInt16(dsCartDetails.Tables[1].DefaultView[j]["NoOfChild"]) > 0)
                {
                    if (Convert.ToInt16(dsCartDetails.Tables[1].DefaultView[j]["NoOfChild"]) == 1)
                        sbDOTWBlockReq.Append("<child runno=\"0\">" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["ChildAge1"]) + "</child>");
                    if (Convert.ToInt16(dsCartDetails.Tables[1].DefaultView[j]["NoOfChild"]) == 2)
                    {
                        sbDOTWBlockReq.Append("<child runno=\"0\">" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["ChildAge1"]) + "</child>");
                        sbDOTWBlockReq.Append("<child runno=\"1\">" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["ChildAge2"]) + "</child>");
                    }
                }
                sbDOTWBlockReq.Append("</children>");
                sbDOTWBlockReq.Append("<rateBasis>-1</rateBasis>");
                sbDOTWBlockReq.Append("<passengerNationality>" + Convert.ToString(Nationality) + "</passengerNationality>");
                //sbDOTWBlockReq.Append("<passengerCountryOfResidence>" + Convert.ToString(dsCartDetails.Tables[0].Rows[i]["Nationality"]) + "</passengerCountryOfResidence>");
                sbDOTWBlockReq.Append("<passengerCountryOfResidence>" + Convert.ToString(Nationality) + "</passengerCountryOfResidence>");
                sbDOTWBlockReq.Append("<roomTypeSelected>");
                sbDOTWBlockReq.Append("<code>" + Convert.ToString(dotwroomtypecode) + "</code>");
                sbDOTWBlockReq.Append("<selectedRateBasis>" + Convert.ToInt32(ratebasis) + "</selectedRateBasis>");
                sbDOTWBlockReq.Append("<allocationDetails>" + Convert.ToString(allocationDetails) + "</allocationDetails>");
                sbDOTWBlockReq.Append("</roomTypeSelected>");
                return sbDOTWBlockReq.ToString();
            }
            catch (Exception ex)
            {
                //ExceptionManager.LogException(ex);
                _ = _exception.Error(ex);
            }
            return string.Empty;
        }

        public string GetDOTWGerRoomZBlockRequestV3(DataSet dsCartDetails, int i, String ClientID, string UserName, string Password, string RoomBlock)
        {
            try
            {
                StringBuilder sbDOTWBlockReq = new StringBuilder();
                sbDOTWBlockReq.Append("<customer>");
                sbDOTWBlockReq.Append(getDOTWProfileString(ClientID, UserName, Password));
                sbDOTWBlockReq.Append("<request command=\"getrooms\" debug=\"0\">");
                sbDOTWBlockReq.Append("<bookingDetails>");
                string checkindate = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["CheckInDate"]);
                sbDOTWBlockReq.Append("<fromDate>" + checkindate.Substring(0, 4) + "-" + checkindate.Substring(4, 2) + "-" + checkindate.Substring(6, 2) + "</fromDate>");
                string checkoutdate = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["CheckOutDate"]);
                sbDOTWBlockReq.Append("<toDate>" + checkoutdate.Substring(0, 4) + "-" + checkoutdate.Substring(4, 2) + "-" + checkoutdate.Substring(6, 2) + "</toDate>");
                sbDOTWBlockReq.Append("<currency>520</currency>");
                sbDOTWBlockReq.Append("<rooms no=\"" + dsCartDetails.Tables[1].DefaultView.Count + "\">");
                sbDOTWBlockReq.Append(RoomBlock);
                sbDOTWBlockReq.Append("</rooms>");
                sbDOTWBlockReq.Append("<productId>" + Convert.ToString(dsCartDetails.Tables[0].Rows[i]["xmlHotelId"]) + "</productId>");
                sbDOTWBlockReq.Append("</bookingDetails>");
                sbDOTWBlockReq.Append("</request>");
                sbDOTWBlockReq.Append("</customer>");
                return sbDOTWBlockReq.ToString();
            }
            catch (Exception ex)
            {
                //ExceptionManager.LogException(ex);
                _ = _exception.Error(ex);
            }
            return string.Empty;
        }

        //book 13/08/25
        public string GetDotwRoomBlockFor_SaveBooking(DataTable dtPassengers, DataSet dsCartDetails, int i, int j, string NewAllocationId, string ChangedOccupancy)
        {
            try
            {

                //Here get Confirmation request
                StringBuilder sbDOTWBookingReq = new StringBuilder();

                #region Room Block
                int AdultsCapacity = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[j]["NoOfAdults"]);
                int ChildCapacity = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[j]["NoOfChild"]);
                string ChildAges = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["ChildAge1"] + "_" + dsCartDetails.Tables[1].DefaultView[j]["ChildAge2"]);

                int MaxExtrabed = 0;

                if (ChangedOccupancy != "")
                {
                    AdultsCapacity = Convert.ToInt32(ChangedOccupancy.Split(',')[0]);
                    ChildCapacity = Convert.ToInt32(ChangedOccupancy.Split(',')[1]);
                    ChildAges = Convert.ToString(ChangedOccupancy.Split(',')[2]);
                    MaxExtrabed = Convert.ToInt32(ChangedOccupancy.Split(',')[3]);

                }
                sbDOTWBookingReq.Append("<room runno=\"" + j + "\">");
                sbDOTWBookingReq.Append("<roomTypeCode>" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomTypeID"]).Split(':')[0] + "</roomTypeCode>");
                sbDOTWBookingReq.Append("<selectedRateBasis>" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomTypeID"]).Split(':')[1] + "</selectedRateBasis>");
                sbDOTWBookingReq.Append("<allocationDetails>" + NewAllocationId + "</allocationDetails>");
                //sbDOTWBookingReq.Append(Common.getDOTWAdultChildNationalityString(dsCartDetails, i, j));
                sbDOTWBookingReq.Append("<adultsCode>" + Convert.ToString(AdultsCapacity) + "</adultsCode>");
                sbDOTWBookingReq.Append("<actualAdults>" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["NoOfAdults"]) + "</actualAdults>");

                sbDOTWBookingReq.Append("<children no=\"" + Convert.ToString(ChildCapacity) + "\">");

                if ((ChildCapacity) > 0)
                {
                    if ((ChildCapacity) == 1)
                        sbDOTWBookingReq.Append("<child runno=\"0\">" + Convert.ToString(ChildAges.Split('_')[0]) + "</child>");
                    if (ChildCapacity == 2)
                    {
                        sbDOTWBookingReq.Append("<child runno=\"0\">" + Convert.ToString(ChildAges.Split('_')[0]) + "</child>");
                        sbDOTWBookingReq.Append("<child runno=\"1\">" + Convert.ToString(ChildAges.Split('_')[1]) + "</child>");
                    }
                }
                sbDOTWBookingReq.Append("</children>");
                sbDOTWBookingReq.Append("<actualChildren no=\"" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["NoOfChild"]) + "\">");

                if (Convert.ToInt16(dsCartDetails.Tables[1].DefaultView[j]["NoOfChild"]) > 0)
                {
                    if (Convert.ToInt16(dsCartDetails.Tables[1].DefaultView[j]["NoOfChild"]) == 1)
                        sbDOTWBookingReq.Append("<actualChild  runno=\"0\">" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["ChildAge1"]) + "</actualChild>");
                    if (Convert.ToInt16(dsCartDetails.Tables[1].DefaultView[j]["NoOfChild"]) == 2)
                    {
                        sbDOTWBookingReq.Append("<actualChild runno=\"0\">" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["ChildAge1"]) + "</actualChild>");
                        sbDOTWBookingReq.Append("<actualChild runno=\"1\">" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["ChildAge2"]) + "</actualChild>");
                    }
                }
                sbDOTWBookingReq.Append("</actualChildren>");
                if (MaxExtrabed > 0)
                    MaxExtrabed = 1;

                sbDOTWBookingReq.Append("<extraBed>" + MaxExtrabed + "</extraBed>");

                sbDOTWBookingReq.Append("<passengerNationality>" + Convert.ToString(dsCartDetails.Tables[0].Rows[i]["Nationality"]) + "</passengerNationality>");
                sbDOTWBookingReq.Append("<passengerCountryOfResidence>" + Convert.ToString(dsCartDetails.Tables[0].Rows[i]["Nationality"]) + "</passengerCountryOfResidence>");
                sbDOTWBookingReq.Append("<passengersDetails>");
                //Passanger details loop
                string isLead = string.Empty;
                dtPassengers.DefaultView.RowFilter = "PickUp='" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomTypeID"]) + "' and RoomNo='" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomNumber"]) + "' and IsLead=1";
                if (dtPassengers.DefaultView.Count == 0)
                    isLead = "yes";
                dtPassengers.DefaultView.RowFilter = "";
                dtPassengers.DefaultView.RowFilter = "PickUp='" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomTypeID"]) + "' and RoomNo='" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomNumber"]) + "' and UserCartId='" + dsCartDetails.Tables[0].DefaultView[i]["UserCartId"] + "'";

                dtPassengers.DefaultView.Sort = "IsLead ASC";
                if (dtPassengers.DefaultView.Count > 0)
                {
                    for (int k = 0; k < dtPassengers.DefaultView.Count; k++)
                    {
                        //sbDOTWBookingReq.Append("<passenger leading=\"" + (Convert.ToInt16(dtPassengers.DefaultView[k]["IsLead"]) == 1 ? "yes" : "no") + "\">");
                        sbDOTWBookingReq.Append("<passenger leading=\"" + (Convert.ToInt16(dtPassengers.DefaultView[k]["IsLead"]) == 1 ? "yes" : (isLead == "yes" && k == 0 ? isLead : "no")) + "\">");
                        //isLead = (isLead != string.Empty ? isLead : (Convert.ToInt16(dtPassengers.DefaultView[k]["IsLead"]) == 1 ? "yes" : "no"));
                        //sbDOTWBookingReq.Append("<passenger leading=\"" + (Convert.ToInt16(dtPassengers.DefaultView[k]["IsLead"]) == 1 ? "yes" : "no") + "\">");
                        //sbDOTWBookingReq.Append("<passenger leading=\"" + isLead + "\">");

                        string salution = string.Empty;
                        if (dsCartDetails.Tables[2] != null)
                        {
                            //Filter third table with prefix passaenger name
                            dsCartDetails.Tables[2].DefaultView.RowFilter = "GroupValue='" + Convert.ToString(dtPassengers.DefaultView[k]["Prefix"]) + "'";
                            if (dsCartDetails.Tables[2].DefaultView.Count > 0)
                                salution = Convert.ToString(dsCartDetails.Tables[2].DefaultView[0]["XMLID"]);
                        }
                        sbDOTWBookingReq.Append("<salutation>" + salution + "</salutation>");
                        sbDOTWBookingReq.Append("<firstName>" + Convert.ToString(dtPassengers.DefaultView[k]["FirstName"]).Trim() + "</firstName>");
                        sbDOTWBookingReq.Append("<lastName>" + Convert.ToString(dtPassengers.DefaultView[k]["LastName"]).Trim() + "</lastName>");
                        sbDOTWBookingReq.Append("</passenger>");
                    }
                }
                sbDOTWBookingReq.Append("</passengersDetails>");

                //sbDOTWBookingReq.Append("<specialRequests count=\"1\">");
                //sbDOTWBookingReq.Append("<req runno=\"0\">1718</req>");
                //sbDOTWBookingReq.Append("</specialRequests>");

                sbDOTWBookingReq.Append("</room>");
                #endregion


                return sbDOTWBookingReq.ToString();
            }
            catch (Exception ex)
            {
                _ = _exception.ErrorNew(ex, _LogAPI.Book);
            }
            return string.Empty;
        }

        public string GetDotwRoomBlockFor_SaveBooking_newversion(DataTable dtPassengers, BookRequest_newversion bookRequest, int i, int j, string NewAllocationId, string ChangedOccupancy)
        {
            try
            {

                //Here get Confirmation request
                StringBuilder sbDOTWBookingReq = new StringBuilder();

                #region Room Block
                int AdultsCapacity = Convert.ToInt32(bookRequest.userCart.UserCartDetails[j].NoofAdult);
                int ChildCapacity = Convert.ToInt32(bookRequest.userCart.UserCartDetails[j].NoofChild);

                string ChildAges = Convert.ToString(bookRequest.userCart.UserCartDetails[j].ChildAge + "_" + bookRequest.userCart.UserCartDetails[j].ChildAge);//need change060426

                int MaxExtrabed = 0;

                if (ChangedOccupancy != "")
                {
                    AdultsCapacity = Convert.ToInt32(ChangedOccupancy.Split(',')[0]);
                    ChildCapacity = Convert.ToInt32(ChangedOccupancy.Split(',')[1]);
                    ChildAges = Convert.ToString(ChangedOccupancy.Split(',')[2]);
                    MaxExtrabed = Convert.ToInt32(ChangedOccupancy.Split(',')[3]);

                }
                sbDOTWBookingReq.Append("<room runno=\"" + j + "\">");
                sbDOTWBookingReq.Append("<roomTypeCode>" + Convert.ToString(bookRequest.userCart.UserCartDetails[j].RoomTypeId).Split(':')[0] + "</roomTypeCode>");
                sbDOTWBookingReq.Append("<selectedRateBasis>" + Convert.ToString(bookRequest.userCart.UserCartDetails[j].RoomTypeId).Split(':')[1] + "</selectedRateBasis>");
                sbDOTWBookingReq.Append("<allocationDetails>" + NewAllocationId + "</allocationDetails>");
                //sbDOTWBookingReq.Append(Common.getDOTWAdultChildNationalityString(dsCartDetails, i, j));
                sbDOTWBookingReq.Append("<adultsCode>" + Convert.ToString(AdultsCapacity) + "</adultsCode>");
                sbDOTWBookingReq.Append("<actualAdults>" + Convert.ToString(bookRequest.userCart.UserCartDetails[j].NoofAdult) + " </actualAdults>");

                sbDOTWBookingReq.Append("<children no=\"" + Convert.ToString(ChildCapacity) + "\">");

                if ((ChildCapacity) > 0)
                {
                    if ((ChildCapacity) == 1)
                        sbDOTWBookingReq.Append("<child runno=\"0\">" + Convert.ToString(ChildAges.Split('_')[0]) + "</child>");
                    if (ChildCapacity == 2)
                    {
                        sbDOTWBookingReq.Append("<child runno=\"0\">" + Convert.ToString(ChildAges.Split('_')[0]) + "</child>");
                        sbDOTWBookingReq.Append("<child runno=\"1\">" + Convert.ToString(ChildAges.Split('_')[1]) + "</child>");
                    }
                }
                sbDOTWBookingReq.Append("</children>");
                sbDOTWBookingReq.Append("<actualChildren no=\"" + Convert.ToString(bookRequest.userCart.UserCartDetails[j].NoofChild) + "\">");

                if (Convert.ToInt16(bookRequest.userCart.UserCartDetails[j].NoofChild) > 0)
                {
                    if (Convert.ToInt16(bookRequest.userCart.UserCartDetails[j].NoofChild) == 1)
                        sbDOTWBookingReq.Append("<actualChild  runno=\"0\">" + Convert.ToString(bookRequest.userCart.UserCartDetails[j].ChildAge) + "</actualChild>");
                    if (Convert.ToInt16(bookRequest.userCart.UserCartDetails[j].NoofChild) == 2)
                    {
                        sbDOTWBookingReq.Append("<actualChild runno=\"0\">" + Convert.ToString(bookRequest.userCart.UserCartDetails[j].ChildAge) + "</actualChild>");
                        sbDOTWBookingReq.Append("<actualChild runno=\"1\">" + Convert.ToString(bookRequest.userCart.UserCartDetails[j].ChildAge) + "</actualChild>");
                    }
                }
                sbDOTWBookingReq.Append("</actualChildren>");
                if (MaxExtrabed > 0)
                    MaxExtrabed = 1;

                sbDOTWBookingReq.Append("<extraBed>" + MaxExtrabed + "</extraBed>");

                sbDOTWBookingReq.Append("<passengerNationality>" + Convert.ToString(bookRequest.userCart.UserCartDetails[j].RoomTypeId).Split(':')[8] + "</passengerNationality>");
                sbDOTWBookingReq.Append("<passengerCountryOfResidence>" + Convert.ToString(bookRequest.userCart.UserCartDetails[j].RoomTypeId).Split(':')[8] + "</passengerCountryOfResidence>");
                sbDOTWBookingReq.Append("<passengersDetails>");
                //Passanger details loop
                string isLead = string.Empty;
                dtPassengers.DefaultView.RowFilter = "PickUp='" + Convert.ToString(Convert.ToString(bookRequest.userCart.UserCartDetails[j].RoomTypeId)) + "' and RoomNo='" + Convert.ToString(Convert.ToString(bookRequest.userCart.UserCartDetails.Count)) + "' and IsLead=1";
                if (dtPassengers.DefaultView.Count == 0)
                    isLead = "yes";
                dtPassengers.DefaultView.RowFilter = "";
                dtPassengers.DefaultView.RowFilter = "PickUp='" + Convert.ToString(bookRequest.userCart.UserCartDetails[j].RoomTypeId) + "' and RoomNo='" + Convert.ToString(bookRequest.userCart.UserCartDetails.Count) + "' and UserCartId='" + bookRequest.userCart.UserCartDetails[j].UserCartId + "'";

                dtPassengers.DefaultView.Sort = "IsLead ASC";
                if (dtPassengers.DefaultView.Count > 0)
                {
                    for (int k = 0; k < dtPassengers.DefaultView.Count; k++)
                    {
                        //sbDOTWBookingReq.Append("<passenger leading=\"" + (Convert.ToInt16(dtPassengers.DefaultView[k]["IsLead"]) == 1 ? "yes" : "no") + "\">");
                        sbDOTWBookingReq.Append("<passenger leading=\"" + (Convert.ToInt16(dtPassengers.DefaultView[k]["IsLead"]) == 1 ? "yes" : (isLead == "yes" && k == 0 ? isLead : "no")) + "\">");
                      
                        string salution = string.Empty;
                        if (bookRequest.userCart.UserCartDetails != null)
                        {

                            //060426
                            //dsCartDetails.Tables[2].DefaultView.RowFilter = "GroupValue='" + Convert.ToString(dtPassengers.DefaultView[k]["Prefix"]) + "'";
                            //if (bookRequest.userCart.UserCartDetails.Count > 0)
                            //    salution = Convert.ToString(dsCartDetails.Tables[2].DefaultView[0]["XMLID"]);
                        }
                        sbDOTWBookingReq.Append("<salutation>" + salution + "</salutation>");
                        sbDOTWBookingReq.Append("<firstName>" + Convert.ToString(dtPassengers.DefaultView[k]["FirstName"]).Trim() + "</firstName>");
                        sbDOTWBookingReq.Append("<lastName>" + Convert.ToString(dtPassengers.DefaultView[k]["LastName"]).Trim() + "</lastName>");
                        sbDOTWBookingReq.Append("</passenger>");
                    }
                }
                sbDOTWBookingReq.Append("</passengersDetails>");

                //sbDOTWBookingReq.Append("<specialRequests count=\"1\">");
                //sbDOTWBookingReq.Append("<req runno=\"0\">1718</req>");
                //sbDOTWBookingReq.Append("</specialRequests>");

                sbDOTWBookingReq.Append("</room>");
                #endregion


                return sbDOTWBookingReq.ToString();
            }
            catch (Exception ex)
            {
                _ = _exception.ErrorNew(ex, _LogAPI.Book);
            }
            return string.Empty;
        }
        public string GetDOtwSaveBookingRequest(DataSet dsCartDetails, int i, String ClientID, string UserName, string Password, string BookingType, string RoomBlockFOrSaveBooking, string AgentCode)
        {
            try
            {
                StringBuilder sbDOTWBookingReq = new StringBuilder();
                sbDOTWBookingReq.Append("<customer>");
                sbDOTWBookingReq.Append(getDOTWProfileString(Convert.ToString(ClientID), Convert.ToString(UserName), Convert.ToString(Password)));
                sbDOTWBookingReq.Append("<request command=\"" + BookingType + "\" debug=\"0\">");
                sbDOTWBookingReq.Append("<bookingDetails>");
                string checkindate = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["CheckInDate"]);
                sbDOTWBookingReq.Append("<fromDate>" + checkindate.Substring(0, 4) + "-" + checkindate.Substring(4, 2) + "-" + checkindate.Substring(6, 2) + "</fromDate>");
                string checkoutdate = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["CheckOutDate"]);
                sbDOTWBookingReq.Append("<toDate>" + checkoutdate.Substring(0, 4) + "-" + checkoutdate.Substring(4, 2) + "-" + checkoutdate.Substring(6, 2) + "</toDate>");
                sbDOTWBookingReq.Append("<currency>" + WebBedsStaticInfo.DOTWCurrency + "</currency>");
                sbDOTWBookingReq.Append("<productId>" + Convert.ToString(dsCartDetails.Tables[0].Rows[i]["xmlHotelId"]) + "</productId>");
                string CustRef = string.Empty;
                for (int rm = 0; rm < dsCartDetails.Tables[1].DefaultView.Count; rm++)
                    CustRef = CustRef + "'" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[rm]["UserCartDetailId"]) + "',";
                //Remove Last comma
                CustRef = CustRef.Substring(0, CustRef.Length - 1);
                CustRef = AgentCode + "-" + CustRef;
                if (CustRef.Length > 50)
                    CustRef = CustRef.Substring(0, 49);

                sbDOTWBookingReq.Append("<customerReference>" + CustRef + "</customerReference>");

                sbDOTWBookingReq.Append("<rooms no=\"" + Convert.ToString(dsCartDetails.Tables[1].DefaultView.Count) + "\">");

                sbDOTWBookingReq.Append(RoomBlockFOrSaveBooking);
                sbDOTWBookingReq.Append("</rooms>");
                sbDOTWBookingReq.Append("</bookingDetails>");
                sbDOTWBookingReq.Append("</request>");
                sbDOTWBookingReq.Append("</customer>");
                return sbDOTWBookingReq.ToString();
            }
            catch (Exception ex)
            {
                //Common.LogException(Convert.ToString(dsCartDetails.Tables[0].Rows[i]["xmlHotelId"]));
                //Common.LogException(Convert.ToString(dsCartDetails.Tables[1].DefaultView[0]["UserCartDetailId"]));
                //Common.LogException(Convert.ToString(dsCartDetails.Tables[1].DefaultView.Count));
                //Common.LogException(Convert.ToString("ClientID=" + ClientID));
                //Common.LogException(Convert.ToString("UserName=" + UserName));
                //Common.LogException(Convert.ToString("Password=" + Password));
                //Common.LogException(Convert.ToString("BookingType=" + BookingType));
                //Common.LogException(Convert.ToString(RoomBlockFOrSaveBooking));
                //Common.LogException(Convert.ToString("agentcodebooking=" + AgentCode));

                _exception.ErrorNew(ex, _LogAPI.Book);
            }
            return string.Empty;
        }
        public string GetDOtwSaveBookingRequest_newversion(BookRequest_newversion bookRequest, int i, String ClientID, string UserName, string Password, string BookingType, string RoomBlockFOrSaveBooking, string AgentCode)
        {
            try
            {
                StringBuilder sbDOTWBookingReq = new StringBuilder();
                sbDOTWBookingReq.Append("<customer>");
                sbDOTWBookingReq.Append(getDOTWProfileString(Convert.ToString(ClientID), Convert.ToString(UserName), Convert.ToString(Password)));
                sbDOTWBookingReq.Append("<request command=\"" + BookingType + "\" debug=\"0\">");
                sbDOTWBookingReq.Append("<bookingDetails>");
                string checkindate = Convert.ToString(bookRequest.userCart.UserCartDetails[i].CheckInDate);
                sbDOTWBookingReq.Append("<fromDate>" + checkindate.Substring(0, 4) + "-" + checkindate.Substring(4, 2) + "-" + checkindate.Substring(6, 2) + "</fromDate>");
                string checkoutdate = Convert.ToString(bookRequest.userCart.UserCartDetails[i].CheckOutDate);
                sbDOTWBookingReq.Append("<toDate>" + checkoutdate.Substring(0, 4) + "-" + checkoutdate.Substring(4, 2) + "-" + checkoutdate.Substring(6, 2) + "</toDate>");
                sbDOTWBookingReq.Append("<currency>" + WebBedsStaticInfo.DOTWCurrency + "</currency>");
                //sbDOTWBookingReq.Append("<productId>" + Convert.ToString(dsCartDetails.Tables[0].Rows[i]["xmlHotelId"]) + "</productId>");
                sbDOTWBookingReq.Append("<productId>" + Convert.ToString("") + "</productId>");//need060426
                string CustRef = string.Empty;
                for (int rm = 0; rm < bookRequest.userCart.UserCartDetails.Count; rm++)
                    CustRef = CustRef + "'" + Convert.ToString(bookRequest.userCart.UserCartDetails[i].UserCartDetailId) + "',";
                //Remove Last comma
                CustRef = CustRef.Substring(0, CustRef.Length - 1);
                CustRef = AgentCode + "-" + CustRef;
                if (CustRef.Length > 50)
                    CustRef = CustRef.Substring(0, 49);

                sbDOTWBookingReq.Append("<customerReference>" + CustRef + "</customerReference>");

                sbDOTWBookingReq.Append("<rooms no=\"" + Convert.ToString(bookRequest.userCart.UserCartDetails.Count) + "\">");

                sbDOTWBookingReq.Append(RoomBlockFOrSaveBooking);
                sbDOTWBookingReq.Append("</rooms>");
                sbDOTWBookingReq.Append("</bookingDetails>");
                sbDOTWBookingReq.Append("</request>");
                sbDOTWBookingReq.Append("</customer>");
                return sbDOTWBookingReq.ToString();
            }
            catch (Exception ex)
            {
                
                _exception.ErrorNew(ex, _LogAPI.Book);
            }
            return string.Empty;
        }

        public string GetDotwDeleteItineraryRQ(DataRow[] DOTWDetails, string bookingCode, string Type, string Confirm)
        {
            try
            {
                string UserID = Convert.ToString(DOTWDetails[0]["UserID"]), UserName = Convert.ToString(DOTWDetails[0]["UserName"]), Password = Convert.ToString(DOTWDetails[0]["Password"]);
                string ItineryCancelRequest = "<customer>" +
                                              "  <username>" + UserName + "</username>         " +
                                              "  <password>" + Password + "</password>" +
                                              "  <id>" + UserID + "</id>                                     " +
                                              "  <source>1</source>                                   " +
                                              "  <request command=\"deleteitinerary\">                " +
                                              "    <bookingDetails>                                   " +
                                              "      <bookingType>" + Type + "</bookingType>                     " +
                                              "      <bookingCode>" + bookingCode + "</bookingCode>            " +
                                              "      <confirm>" + Confirm + "</confirm>                            " +
                                              "                                                       " +
                                              "    </bookingDetails>                                  " +
                                              "  </request>                                           " +
                                              "</customer>                                            ";
                return ItineryCancelRequest;
            }
            catch (Exception ex)
            {
                _ = _exception.ErrorNew(ex, _LogAPI.Book);
            }
            return "";
        }

        public void DeleteItineraryDOTW(DataRow[] DOTWDetails, string returnedCode, string Type, string Confirm, string UniqueNo, string AgentUniqueNo, string SupplierName, int BookingConfirmationTimeout, ref string Error)
        {
            string DeleteItineryRQ = "";
            string Response_Itinery = "";
            try
            {
                DeleteItineryRQ = GetDotwDeleteItineraryRQ(DOTWDetails, returnedCode, Type, Confirm);
                _commonMethod.ThirdPartyBookingHistory(Convert.ToString(UniqueNo), Convert.ToString(AgentUniqueNo), DeleteItineryRQ, "", "before Booking Request deleteitinerary NO", SupplierName, "Response Confirm", "-", "API Out");
                Response_Itinery = ""; // XMLHotelBookingCommon.GetBookingSupplierResponse(ref Error, "", DeleteItineryRQ, Convert.ToString(DOTWDetails[0]["WebSiteURL"]), BookingConfirmationTimeout);
                _commonMethod.ThirdPartyBookingHistory(Convert.ToString(UniqueNo), Convert.ToString(AgentUniqueNo), DeleteItineryRQ, Response_Itinery, "after Booking Request bookitinerary NO", Convert.ToString(SupplierName), "Response Confirm", "-", "API Out");

            }
            catch (Exception ex)
            {
                Error = Error + " #DeleteItineryRQ= " + DeleteItineryRQ + " #Response_Itinery= " + Response_Itinery;
                _exception.ErrorNew(ex, _LogAPI.Book);
            }
        }
        public async Task<string> DeleteItineraryDOTWAsync(
     DataRow[] DOTWDetails,
     string returnedCode,
     string Type,
     string Confirm,
     string UniqueNo,
     string AgentUniqueNo,
     string SupplierName,
     int BookingConfirmationTimeout,
     string Error
 )
        {
            string DeleteItineryRQ = "";
            string Response_Itinery = "";

            try
            {
                DeleteItineryRQ = GetDotwDeleteItineraryRQ(DOTWDetails, returnedCode, Type, Confirm);

                _commonMethod.ThirdPartyBookingHistory(
                    UniqueNo, AgentUniqueNo, DeleteItineryRQ, "",
                    "before Booking Request deleteitinerary NO",
                    SupplierName, "Response Confirm", "-", "API Out"
                );

                // 👉 Your actual async API call should be here
                // Example:
                // Response_Itinery = await SomeHttpCall(...);

                Response_Itinery = "";

                _commonMethod.ThirdPartyBookingHistory(
                    UniqueNo, AgentUniqueNo, DeleteItineryRQ, Response_Itinery,
                    "after Booking Request bookitinerary NO",
                    SupplierName, "Response Confirm", "-", "API Out"
                );
            }
            catch (Exception ex)
            {
                Error = Error + " #DeleteItineryRQ= " + DeleteItineryRQ + " #Ex= " + ex.Message;
                _exception.ErrorNew(ex, _LogAPI.Book);
            }

            return Error;
        }
        public void DotwRejectAll(ref string Error, int bookingstatus, int i, string DOTWRQ, string DOTWRS, ref DataTable dtAgentVoucher, int total, DataRow[] DOTWDetails, string returnedCode, string UniqueNo, string AgentUniqueNo, DataSet dsCartDetails)
        {
            try
            {
                for (int mm = 0; mm < total; mm++)
                {
                    {
                        if (returnedCode != "")
                            DeleteItineraryDOTW(DOTWDetails, returnedCode, "2", "no", UniqueNo, AgentUniqueNo, "dotw", Convert.ToInt32(180000), ref Error);
                        DataRow newRowDOTW = dtAgentVoucher.NewRow();
                        newRowDOTW["UserCartId"] = dsCartDetails.Tables[0].Rows[i]["UserCartId"];
                        newRowDOTW["UserCartDetailId"] = dsCartDetails.Tables[1].DefaultView[mm]["UserCartDetailId"];
                        newRowDOTW["AgentVoucherNo"] = "";
                        newRowDOTW["XMLRequest"] = DOTWRQ;
                        newRowDOTW["XMLResponse"] = DOTWRS;
                        newRowDOTW["RoomStatus"] = bookingstatus;
                        newRowDOTW["SupplierName"] = dsCartDetails.Tables[1].DefaultView[mm]["SupplierName"];
                        newRowDOTW["PackageId"] = Convert.ToInt32(dsCartDetails.Tables[0].Rows[i]["PackageId"]);
                        dtAgentVoucher.Rows.Add(newRowDOTW);
                    }

                }
            }
            catch (Exception ex)
            {
                Error = Error + " #Ex= " + ex.Message;
                _ = _exception.ErrorNew(ex, _LogAPI.Book);
            }

        }

        public async Task<string> DotwRejectAllAsync(
     string Error,
     int bookingstatus,
     int i,
     string DOTWRQ,
     string DOTWRS,
     List<AgentVoucherClass> lstAgentVoucher,
     int total,
     DataRow[] DOTWDetails,
     string returnedCode,
     string UniqueNo,
     string AgentUniqueNo,
     BookRequest_newversion bookRequest
 )
        {
            try
            {
                var cart = bookRequest.userCart.UserCartDetails;

                for (int mm = 0; mm < total; mm++)
                {
                    if (!string.IsNullOrEmpty(returnedCode))
                    {
                        Error = await DeleteItineraryDOTWAsync(
                            DOTWDetails,
                            returnedCode,
                            "2",
                            "no",
                            UniqueNo,
                            AgentUniqueNo,
                            "dotw",
                            180000,
                            Error
                        );
                    }

                    var voucher = new AgentVoucherClass
                    {
                        UserCartId =Convert.ToString( cart[i].UserCartId),
                        UserCartDetailId = Convert.ToString(cart[mm].UserCartDetailId),
                        AgentVoucherNo = "",
                        XMLRequest = DOTWRQ,
                        XMLResponse = DOTWRS,
                        RoomStatus = bookingstatus.ToString(),
                        SupplierName = cart[mm].SupplierName
                    };

                    lstAgentVoucher.Add(voucher);
                }
            }
            catch (Exception ex)
            {
                Error = Error + " #Ex= " + ex.Message;
                _ = _exception.ErrorNew(ex, _LogAPI.Book);
            }

            return Error;
        }
        public string GetDotwBookItineraryRequest(DataRow[] DOTWDetails, string Confirm, string bookingType, string bookingCode, string TestPriceAndAllocation)
        {
            try
            {
                string UserID = Convert.ToString(DOTWDetails[0]["UserID"]), UserName = Convert.ToString(DOTWDetails[0]["UserName"]), Password = Convert.ToString(DOTWDetails[0]["Password"]);


                string sendCommunicationTo = "kalyani@technoheaven.net";
                string BookItineryRQ = "<customer>" +
                                        "<username>" + UserName + "</username>" +
                                        "  <password>" + Password + "</password>                          " +
                                        "  <id>" + UserID + "</id>                                       " +
                                        "  <source>1</source>                                          " +
                                        "  <request command=\"bookitinerary\">                         " +
                                        "    <bookingDetails>                                          " +
                                        "      <bookingType>" + bookingType + "</bookingType>                             " +
                                        "      <bookingCode>" + bookingCode + "</bookingCode>                             " +
                                        "      <confirm>" + Confirm + "</confirm>                                     " +
                                        "      <sendCommunicationTo>" + sendCommunicationTo + "</sendCommunicationTo>             " +
                                                      TestPriceAndAllocation +
                                        //"      <creditCardPaymentDetails>                              " +
                                        //"        <paymentMethod></paymentMethod>                       " +
                                        //"        <usedCredit></usedCredit>                             " +
                                        //"        <creditCardCharge></creditCardCharge>                 " +
                                        //"        <creditCardDetails>                                   " +
                                        //"          <creditCardType></creditCardType>                   " +
                                        //"          <creditCardNumber></creditCardNumber>               " +
                                        //"          <creditCardHolderName></creditCardHolderName>       " +
                                        //"          <creditCardExpMonth></creditCardExpMonth>           " +
                                        //"          <creditCardExpYear></creditCardExpYear>             " +
                                        //"          <creditCardCVC></creditCardCVC>                     " +
                                        //"          <avsDetails>                                        " +
                                        //"            <avsFirstName></avsFirstName>                     " +
                                        //"            <avsLastName></avsLastName>                       " +
                                        //"            <avsAddress></avsAddress>                         " +
                                        //"            <avsZip></avsZip>                                 " +
                                        //"            <avsCountry></avsCountry>                         " +
                                        //"            <avsCity></avsCity>                               " +
                                        //"            <avsEmail></avsEmail>                             " +
                                        //"            <avsPhone></avsPhone>                             " +
                                        //"          </avsDetails>                                       " +
                                        //"        </creditCardDetails>                                  " +
                                        // "      </creditCardPaymentDetails>                             " +
                                        "    </bookingDetails>                                         " +
                                        "  </request>                                                  " +
                                        "</customer>                                                   ";
                return BookItineryRQ;
            }
            catch (Exception ex)
            {
                _ = _exception.ErrorNew(ex, _LogAPI.Book);
            }
            return "";
        }

        public string GetBookingSupplierResponse(string Error, string strUniqueId, string strxmlRQRS, string strHost, Int32 TimeOut, string SearchKey = "")
        {
            //System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
            //timer.Start();
            WebClient wc = new WebClient();

            // wc.DownloadData(strHost, strxmlRQRS);
            
            string resultData = wc.UploadString(strHost, "POST", strxmlRQRS);
            return resultData;
        }

        public async Task<string> GetBookingSupplierResponseNew(string Error,string strUniqueId,string strxmlRQRS,string strHost, Int32 TimeOut,string SearchKey = "")
        {

            try
            {
                using (var httpClient = new HttpClient())
                {
                    httpClient.Timeout = TimeSpan.FromSeconds(TimeOut);
                    var content = new StringContent(strxmlRQRS, Encoding.UTF8, "application/xml");

                    var response = await httpClient.PostAsync(strHost, content);
                    response.EnsureSuccessStatusCode();

                    return await response.Content.ReadAsStringAsync();
                }
            }
            catch (Exception ex)
            {
               _ =  _exception.ErrorNew(ex, _LogAPI.Book, SearchKey);
                return "";
            }
        }

        public string GetDotwCancelRequest(string AgentVoucherNo, DataRow[] DOTWDetails, string CancellationAmount, bool IsFinal)
        {
            try
            {
                //ExManager.Exception.LogException("DotwCancel=" + AgentVoucherNo);
                string CancellationXML = "";
                if (!IsFinal)
                {
                    CancellationXML = "<customer>" +
               "<username>" + Convert.ToString(DOTWDetails[0]["UserName"]) + "</username>" +
               "<password>" + Convert.ToString(DOTWDetails[0]["Password"]) + "</password>" +
               "<id>" + Convert.ToString(DOTWDetails[0]["UserID"]) + "</id>" +
               "<source>1</source>" +
               "<request command=\"cancelbooking\">" +
                   "<bookingDetails>" +
                       "<bookingType>1</bookingType>" +
                       "<bookingCode>" + AgentVoucherNo.ToString().Substring(AgentVoucherNo.ToString().LastIndexOf("-") + 1) + "</bookingCode>" +
                       "<confirm>no</confirm>" +
                   "</bookingDetails>" +
               "</request>" +
           "</customer>";
                }
                if (IsFinal)
                {
                    CancellationXML = "<customer>" +
             "<username>" + Convert.ToString(DOTWDetails[0]["UserName"]) + "</username>" +
            "<password>" + Convert.ToString(DOTWDetails[0]["Password"]) + "</password>" +
            "<id>" + Convert.ToString(DOTWDetails[0]["UserID"]) + "</id>" +
            "<source>1</source>" +
            "<request command=\"cancelbooking\">" +
                "<bookingDetails>" +
                    "<bookingType>1</bookingType>" +
                    "<bookingCode>" + AgentVoucherNo.Substring(AgentVoucherNo.ToString().LastIndexOf("-") + 1) + "</bookingCode>" +
                    "<confirm>yes</confirm>" +
                    "<testPricesAndAllocation>" +
                        "<service referencenumber=\"" + AgentVoucherNo.Substring(AgentVoucherNo.LastIndexOf("-") + 1) + "\">" +
                            "<penaltyApplied>" + CancellationAmount + "</penaltyApplied>" +
                        "</service>" +
                    "</testPricesAndAllocation>" +
                "</bookingDetails>" +
            "</request>" +
        "</customer>";
                }
                return CancellationXML;

            }
            catch (Exception ex)
            {
                _ =_exception.ErrorNew(ex,_LogAPI.CancelBooking);
            }
            return string.Empty;
        }
    }
}
