using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Azure;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using XConnect.CommonUtilities;
using XConnect.CommonUtilities.Interface;
using XConnect.ErrorLogHandler.Interface;
using XConnect.Markup.Interface;
using XConnect.Models;
using XConnect.Models.Hotel;
using XConnect.Supplier.Nuitee.Interface;
using static Azure.Core.HttpHeader;
using static XConnect.Models.ErrorLog;
using Options = XConnect.Models.Options;

namespace XConnect.Supplier.Nuitee
{
    public class NuiteeRepo : INuitee
    {
        private readonly ICommonMethod _commonMethod;
        private readonly IException _exception;
        private readonly IMarkup _markup;
        private readonly ISupplierRes _supplierRes;
        private readonly KeySettings _keySettings;



        public NuiteeRepo(ICommonMethod commonMethod, IMarkup markup, ISupplierRes supplierRes, IException exception, IOptions<KeySettings> keySettings)
        {
            _commonMethod = commonMethod;
            _markup = markup;
            _supplierRes = supplierRes;
            _exception = exception;
            _keySettings = keySettings.Value;
        }
        public void GetNuiteeRequestOnly_async(DataRow[] commonCode, List<StarRating> listRating, HotelSearchParameter SearchPara, List<SupplierDetails> lstNuiteeDetails, List<SupplierSearchConfig> lstNuiteeSearchConfig, DataTable dtPreferredDataHotel, ref ConcurrentBag<HotelX> AllTask, int Hit, DataTable dtROE, TokenData tokenData, DataSet dtXMLCity)
        {
            try
            {
                if (lstNuiteeDetails.Count > 0)
                {
                    List<StarRating> lstRating = new List<StarRating>();
                    lstRating = _commonMethod.GetStarRatingStringList_xtg(SearchPara);

                    List<HotelCodeByCity> DtHotelDAta = HtmlExtensions.ToList<HotelCodeByCity>(commonCode.CopyToDataTable());
                    int ratingcount = lstRating.Count;
                    int hotelcount = DtHotelDAta.Count;

                    foreach (SupplierDetails NuiteeDetails in lstNuiteeDetails)
                    {
                        try
                        {
                            int SupplierID = NuiteeDetails.SupplierID;
                            string Supplier = NuiteeDetails.SupplierName;

                            if (lstNuiteeSearchConfig.Count > 0)
                            {
                                List<SupplierSearchConfig> NuiteeSearchConfig = lstNuiteeSearchConfig
                                   .Where(x => x != null && x.SupplierID == SupplierID)
                                   .ToList();

                                if (NuiteeSearchConfig != null && NuiteeSearchConfig.Count > 0)
                                {
                                    DataForSearch dataForSearch = _commonMethod.GetXmlDataForSearchTechwise(SearchPara.Nationality, dtXMLCity, Supplier, NuiteeDetails.TechName);

                                    if (dataForSearch.XmlNationality != "")
                                    {
                                        string Ids = string.Empty;
                                        string xmlbase = "";
                                        _commonMethod.GetPrefferedHotelCodes(ref Ids, ref xmlbase, ref NuiteeSearchConfig, Supplier, dtPreferredDataHotel, SearchPara.IsSearchFor);

                                        List<HotelCodeByCity> filter = new List<HotelCodeByCity>();
                                        filter = _commonMethod.FilterWithOutCity_Code(ratingcount, hotelcount, DtHotelDAta, lstRating, Supplier);


                                        if (NuiteeSearchConfig != null)
                                        {

                                            //DataTable dtInrCurrency = Common.GetCountryCurrency(XMLHotelBookingCommon.nuiteeCurrency);
                                            string nuiteeCurrency = NuiteeDetails.CurrencyShortName;
                                            double CurrencyFactor = 1;
                                            if (nuiteeCurrency.ToLower() != tokenData.SiteCurrency.ToLower())
                                            {
                                                CurrencyFactor = _commonMethod.GetROEByCurrency(nuiteeCurrency, dtROE);
                                            }

                                            for (int k = 0; k < NuiteeSearchConfig.Count; k++)
                                            {
                                                bool DebugMode = (Convert.ToInt32(NuiteeSearchConfig[k].DebugMode) == 0 ? false : true);
                                                //  List<HotelCodeByCity> DtHotelData = FilterFinalList(commonCode, listRating);
                                                int IsCitywisesearch = Convert.ToInt32(NuiteeSearchConfig[k].City);
                                                if (IsCitywisesearch == 0)
                                                {
                                                    if (Convert.ToInt32(NuiteeSearchConfig[k].CityID) == 0 || Convert.ToInt32(NuiteeSearchConfig[k].CityID) == SearchPara.CityID)
                                                    {
                                                        if (commonCode.Length > 0)
                                                        {
                                                            bool isCityWise = false;
                                                            int Noofrecords = Convert.ToInt32(NuiteeSearchConfig[k].Noofrecords);
                                                            double count = Convert.ToDouble(filter.Count);
                                                            int NoOfDiv = Convert.ToInt32(Math.Ceiling(count / Convert.ToDouble(Noofrecords)));
                                                            List<List<XMLID_Search>> lstxml = _commonMethod.GetXMLIDString(NoOfDiv, Noofrecords, count, filter);

                                                            if (SearchPara.Filters.IsShowRooms == 1)
                                                            {

                                                                for (int a = 0; a < NoOfDiv; a++)
                                                                {
                                                                    string IdsForSearch = Convert.ToString(_commonMethod.GetHotelCodeByCOmma(lstxml[a]));
                                                                    HotelX Xhotel = new HotelX();
                                                                    Xhotel.Supplier = Supplier;
                                                                    Xhotel.DebugeMode = DebugMode;
                                                                    Xhotel.SearchTimeOut = Convert.ToInt32(NuiteeSearchConfig[k].HotelSearchTimeOut);
                                                                    //if (Hit > 0)
                                                                    Xhotel.ProcessTimeout = Convert.ToInt32(NuiteeSearchConfig[k].HotelSearchTimeOut2);
                                                                    if (_keySettings.SupplierProxy == "0")
                                                                        Xhotel.URL = Convert.ToString(NuiteeDetails.WebSiteURL);
                                                                    else
                                                                        Xhotel.URL = Convert.ToString(NuiteeDetails.WebSiteURLforAPI);

                                                                    Xhotel.UserId = Convert.ToString(NuiteeDetails.UserID);
                                                                    Xhotel.UserName = Convert.ToString(NuiteeDetails.UserName);
                                                                    Xhotel.PassWord = Convert.ToString(NuiteeDetails.Password);
                                                                    Xhotel.CurrencyCode = nuiteeCurrency;
                                                                    //Xhotel.Action = "http://tempuri.org/IAvailabilityServices/GetHotelRoomAvailability";
                                                                    Xhotel.Action = NuiteeInfo.CityAvailabilityAction;
                                                                    Xhotel.CurrencyFactor = CurrencyFactor;
                                                                    Xhotel.XmlCity = dataForSearch.xmlCity;
                                                                    Xhotel.XmlCountry = dataForSearch.xmlCountry;
                                                                    Xhotel.XmlNationality = dataForSearch.XmlNationality;
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
                                                                        //objrq.Request = Common.Get_Nuitee_RoomOnlynew(SearchPara, Xhotel.UserName, Xhotel.PassWord, Xhotel.XmlCity, IdsForSearch, Xhotel.hotelRating, Xhotel.XmlNationality, Xhotel.XmlCountry, sessionId, ratedetailcode, Roomcode, Xhotel.SearchTimeOut);
                                                                        objrq.Request = XMLNuiteeRequestOnly_async(Xhotel.UserId, Xhotel.UserName, Xhotel.PassWord, SearchPara, IdsForSearch, CurrencyFactor, dataForSearch.xmlCity, Xhotel.XmlCountry, dataForSearch.XmlNationality, isCityWise, Xhotel.CurrencyCode, Xhotel.SearchTimeOut);
                                                                        objrq.Response = ""; RQRS.Add(objrq);
                                                                    }
                                                                    Xhotel.RQRS = RQRS;

                                                                    #endregion
                                                                    Xhotel.TechName = NuiteeDetails.TechName;
                                                                    Xhotel.ReturnRooms = NuiteeDetails.ReturnRooms;
                                                                    AllTask.Add(Xhotel);

                                                                }
                                                            }
                                                            else
                                                            {

                                                                for (int a = 0; a < NoOfDiv; a++)
                                                                {
                                                                    string IdsForSearch = Convert.ToString(_commonMethod.GetHotelCodeByCOmma(lstxml[a]));

                                                                    HotelX Xhotel = new HotelX();
                                                                    Xhotel.Supplier = Supplier;
                                                                    Xhotel.DebugeMode = DebugMode;
                                                                    Xhotel.SearchTimeOut = Convert.ToInt32(NuiteeSearchConfig[k].HotelSearchTimeOut);
                                                                    //if (Hit > 0)
                                                                    Xhotel.ProcessTimeout = Convert.ToInt32(NuiteeSearchConfig[k].HotelSearchTimeOut2);
                                                                    if (_keySettings.SupplierProxy == "0")
                                                                        Xhotel.URL = Convert.ToString(NuiteeDetails.WebSiteURL);
                                                                    else
                                                                        Xhotel.URL = Convert.ToString(NuiteeDetails.WebSiteURLforAPI);

                                                                    Xhotel.UserId = Convert.ToString(NuiteeDetails.UserID);
                                                                    Xhotel.UserName = Convert.ToString(NuiteeDetails.UserName);
                                                                    Xhotel.PassWord = Convert.ToString(NuiteeDetails.Password);
                                                                    Xhotel.CurrencyCode = nuiteeCurrency;
                                                                    //Xhotel.Action = "http://tempuri.org/IAvailabilityServices/GetHotelRoomAvailability";
                                                                    Xhotel.Action = NuiteeInfo.CityAvailabilityAction;
                                                                    Xhotel.CurrencyFactor = CurrencyFactor;
                                                                    Xhotel.XmlCity = dataForSearch.xmlCity;
                                                                    Xhotel.XmlCountry = dataForSearch.xmlCountry;
                                                                    Xhotel.XmlNationality = dataForSearch.XmlNationality;
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
                                                                        //objrq.Request = Common.Get_Nuitee_RoomOnlynew(SearchPara, Xhotel.UserName, Xhotel.PassWord, Xhotel.XmlCity, IdsForSearch, Xhotel.hotelRating, Xhotel.XmlNationality, Xhotel.XmlCountry, sessionId, ratedetailcode, Roomcode, Xhotel.SearchTimeOut);
                                                                        objrq.Request = XMLNuiteeRequestOnly_async(Xhotel.UserId, Xhotel.UserName, Xhotel.PassWord, SearchPara, IdsForSearch, CurrencyFactor, dataForSearch.xmlCity, Xhotel.XmlCountry, dataForSearch.XmlNationality, isCityWise, Xhotel.CurrencyCode, Xhotel.SearchTimeOut);
                                                                        objrq.Response = ""; RQRS.Add(objrq);
                                                                    }
                                                                    Xhotel.RQRS = RQRS;
                                                                    #endregion
                                                                    Xhotel.TechName = NuiteeDetails.TechName;
                                                                    Xhotel.ReturnRooms = NuiteeDetails.ReturnRooms;
                                                                    AllTask.Add(Xhotel);

                                                                }
                                                            }
                                                        }
                                                    }
                                                }

                                                else
                                                {
                                                    if (Convert.ToInt32(NuiteeSearchConfig[k].CityID) == 0 || Convert.ToInt32(NuiteeSearchConfig[k].CityID) == SearchPara.CityID)
                                                    {
                                                        if (commonCode.Length > 0)
                                                        {
                                                            bool isCityWise = true;

                                                            int Noofrecords = Convert.ToInt32(NuiteeSearchConfig[k].Noofrecords);
                                                            string IdsForSearch = "";//Convert.ToString(GetHotelCodeByCOmma(lstxml[a]));
                                                            HotelX Xhotel = new HotelX();
                                                            Xhotel.Supplier = Supplier;
                                                            Xhotel.DebugeMode = DebugMode;
                                                            Xhotel.SearchTimeOut = Convert.ToInt32(NuiteeSearchConfig[k].HotelSearchTimeOut);
                                                            //if (Hit > 0)
                                                            Xhotel.ProcessTimeout = Convert.ToInt32(NuiteeSearchConfig[k].HotelSearchTimeOut2);
                                                            if (_keySettings.SupplierProxy == "0")
                                                                Xhotel.URL = Convert.ToString(NuiteeDetails.WebSiteURL);
                                                            else
                                                                Xhotel.URL = Convert.ToString(NuiteeDetails.WebSiteURLforAPI);
                                                            Xhotel.UserId = Convert.ToString(NuiteeDetails.UserID);
                                                            Xhotel.UserName = Convert.ToString(NuiteeDetails.UserName);
                                                            Xhotel.PassWord = Convert.ToString(NuiteeDetails.Password);
                                                            Xhotel.CurrencyCode = nuiteeCurrency;
                                                            Xhotel.Action = NuiteeInfo.PropertyAvailabilityAction;
                                                            //Xhotel.Action = "http://tempuri.org/IAvailabilityServices/GetHotelRoomAvailabilityByHotels";
                                                            Xhotel.CurrencyFactor = CurrencyFactor;
                                                            Xhotel.XmlCity = dataForSearch.xmlCity;
                                                            Xhotel.XmlCountry = dataForSearch.xmlCountry;
                                                            Xhotel.XmlNationality = dataForSearch.XmlNationality;
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
                                                                objrq.Request = XMLNuiteeRequestOnly_async(Xhotel.UserId, Xhotel.UserName, Xhotel.PassWord, SearchPara, IdsForSearch, CurrencyFactor, dataForSearch.xmlCity, Xhotel.XmlCountry, dataForSearch.XmlNationality, isCityWise, Xhotel.CurrencyCode, Xhotel.SearchTimeOut);
                                                                objrq.Response = ""; RQRS.Add(objrq);
                                                            }
                                                            Xhotel.RQRS = RQRS;
                                                            #endregion
                                                            Xhotel.TechName = NuiteeDetails.TechName;
                                                            Xhotel.ReturnRooms = NuiteeDetails.ReturnRooms;
                                                            AllTask.Add(Xhotel);

                                                            //  }
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
            catch (Exception ex)
            {
                _exception.Error(ex);
            }

        }


        public string XMLNuiteeRequestOnly_async(string ClientID, string UserName, string Password, HotelSearchParameter SearchParam, string HotelIds, double CurrencyFactor, string CityCode, string CountryCode, string Nationality, bool IsCityWise, string dtInrCurrency, int SearchTimeOut)
        {
            try
            {
                #region PriceOnly RQ
                string PriceOnly = Get_Nuitee_PriceOnly(SearchParam, ClientID, UserName, Password, CityCode, CountryCode, Nationality, HotelIds, CurrencyFactor, IsCityWise, dtInrCurrency, SearchTimeOut);
                return PriceOnly;
                #endregion
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
                return "";
            }
        }

        public string Get_Nuitee_PriceOnly(HotelSearchParameter SearchParam, string ClientID, string UserName, string Password, string CityCode, string CountryCode, string Nationality, string HotelIds, double currencyFactor, bool IsCityWise, string Currency, int SearchTimeOut)
        {
            try
            {
                #region Price Only and Room Only RQ
                string CheckIn = SearchParam.CheckInDate;
                CheckIn = CheckIn.Substring(6, 4) + "-" + CheckIn.Substring(3, 2) + "-" + CheckIn.Substring(0, 2);


                string CheckOut = SearchParam.CheckOutDate;
                CheckOut = CheckOut.Substring(6, 4) + "-" + CheckOut.Substring(3, 2) + "-" + CheckOut.Substring(0, 2);



                string language = "en_US";


                StringBuilder nuitee = new StringBuilder();
                nuitee.Append("<soapenv:Envelope xmlns:soapenv=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:nuit=\"http://www.nuitee.ma\">");
                nuitee.Append("<soapenv:Header/>");
                nuitee.Append("<soapenv:Body>");
                nuitee.Append("<nuit:performGetCityAvailability>");
                nuitee.Append("<getCityAvailabilityReq>");
                nuitee.Append("<login>");
                nuitee.Append("<language>" + language + "</language>");
                nuitee.Append("<password>" + Password + "</password>");
                nuitee.Append("<userName>" + UserName + "</userName>");
                nuitee.Append("</login>");
                nuitee.Append("<checkInDate>" + CheckIn + "</checkInDate>");
                nuitee.Append("<checkOutDate> " + CheckOut + "</checkOutDate>");
                if (HotelIds != string.Empty)
                {
                    string[] hid = HotelIds.Split(',');
                    for (int i = 0; i < hid.Length; i++)
                    {
                        nuitee.Append("<hotelCodes>" + hid[i] + "</hotelCodes>");
                    }

                    // nuitee.Append("<hotelCodes>" + HotelIds + "</hotelCodes>");
                }
                else
                {
                    nuitee.Append("<cityCode>" + CityCode + "</cityCode>");
                }
                nuitee.Append("<guestNationality>" + Nationality + "</guestNationality>");
                nuitee.Append("<roomGuests>");

                for (int i = 0; i < SearchParam.Rooms.Count; i++)
                {
                    nuitee.Append("<roomGuests>");//              
                    int NoofAdults = SearchParam.Rooms[i].NoofAdults;
                    int NoOfChild = SearchParam.Rooms[i].NoofChild;
                    nuitee.Append("<adultCount>" + NoofAdults + "</adultCount>");
                    nuitee.Append("<childCount>" + NoOfChild + "</childCount>");
                    if (NoOfChild > 0)
                    {

                        if (NoOfChild == 1)
                            nuitee.Append("<childAges>" + SearchParam.Rooms[i].Child1Age + "</childAges>");
                        if (NoOfChild == 2)
                        {
                            nuitee.Append("<childAges>" + SearchParam.Rooms[i].Child1Age + "</childAges>");
                            nuitee.Append("<childAges>" + SearchParam.Rooms[i].Child2Age + "</childAges>");
                        }

                    }
                    else
                    {

                    }
                    nuitee.Append("</roomGuests>");
                }
                nuitee.Append("</roomGuests>");
                nuitee.Append("<currency>" + Currency + "</currency>");
                nuitee.Append("<languageCode>" + language + "</languageCode>");
                nuitee.Append("<timeout>" + SearchTimeOut + "</timeout>");
                nuitee.Append("</getCityAvailabilityReq>");
                nuitee.Append("</nuit:performGetCityAvailability>");
                nuitee.Append("</soapenv:Body>");
                nuitee.Append("</soapenv:Envelope>");
                string ReqString = nuitee.ToString();
                return ReqString;
                #endregion

            }
            catch (Exception ex)
            {
                _exception.Error(ex);
            }
            return string.Empty;
        }


        public async Task<List<Options>> Nuitee_HotelSearchWithRooms(HotelX Item, CommonSearch commonSearch, string ResponseData, HotelSearchParameter SearchPara, string XmlCity, string XmlCountry, string XmlNationality, string Currency, double CurrencyFactor, string Action, DataSet dsAgentDetails, string SupplierName, DataTable dtCountryCityMarkup, Int32 processTimeout, string ResProccessTimerLogActive, string ResProccessTimeValueforSaveLog)
        {
            System.Diagnostics.Stopwatch timerroomx = new System.Diagnostics.Stopwatch();
            timerroomx.Start();
            List<Options> options = new List<Options>();

            DataTable DtHotel = _commonMethod.GenerateDataTableNewV2();
            //    DataTable DtBDC_HotelRoom = Common.GenerateRoomtypeDataTableNewV2();
            DataTable dtInrCurrency = new DataTable();

            if (ResponseData == "")
            {
                Item.ResponseType = "NoCount";
                return options;
            }
            try
            {
                #region PriceOnly RS
                if (ResponseData == string.Empty)
                {
                    Item.ResponseType = "NoCount";
                    return options;
                }

                //ResponseData = File.ReadAllText(@"E:\Hardik\paris.xml");

                ResponseData = XElement.Parse(ResponseData).ToString(SaveOptions.DisableFormatting);
                ResponseData = ResponseData.Replace("<roomGuests><roomGuests>", "<roomGuests1><roomGuests>");
                ResponseData = ResponseData.Replace("</roomGuests></roomGuests>", "</roomGuests></roomGuests1>");

                ResponseData = ResponseData.Replace("<hotelResults><hotelResults>", "<hotelResults1><hotelResults>");
                ResponseData = ResponseData.Replace("</hotelResults></hotelResults>", "</hotelResults></hotelResults1>");

                ResponseData = ResponseData.Replace("<cancelPoliciesInfos><cancelPolicyInfos>", "<cancelPoliciesInfos1><cancelPolicyInfos>");
                ResponseData = ResponseData.Replace("</refundableTag></cancelPoliciesInfos>", "</refundableTag></cancelPoliciesInfos1>");

                ResponseData = ResponseData.Replace("<rateDetails><cancelPoliciesInfos>", "<rateDetails><cancelPoliciesInfos1>");

                ResponseData = ResponseData.Replace("<rooms><rooms>", "<rooms1><rooms>");
                ResponseData = ResponseData.Replace("</rooms></rooms>", "</rooms></rooms1>");

                ResponseData = ResponseData.Replace("</tax><taxesAndFees>", "</tax><taxesAndFees1>");
                ResponseData = ResponseData.Replace("</rooms1><taxesAndFees>", "</rooms1><taxesAndFees1>");

                ResponseData = ResponseData.Replace("</taxesAndFees><totalPrice>", "</taxesAndFees1><totalPrice>");

                ResponseData = ResponseData.Replace("<rateDetails><maxRate>", "<rateDetails1><maxRate>");
                ResponseData = ResponseData.Replace("</rateDetails></hotelResults>", "</rateDetails1></hotelResults>");


                DataSet NuiteeResponse = _commonMethod.GetDataSetFromXMLString(ResponseData);



                if (NuiteeResponse == null || NuiteeResponse.Tables.Contains("getCityAvailabilityRes") == false)
                {
                    Item.ResponseType = "NoCount";
                    return options;
                }
                #endregion
                if (NuiteeResponse.Tables["getCityAvailabilityRes"].Columns.Contains("currency") == false)
                {
                    Item.ResponseType = "NoCount";
                    return options;
                }
                string Currecy = Convert.ToString(NuiteeResponse.Tables["getCityAvailabilityRes"].Rows[0]["currency"]);
                string Seesionid = Convert.ToString(NuiteeResponse.Tables["getCityAvailabilityRes"].Rows[0]["sessionId"]);

                string ratedeatailcode1 = "";
                //string hotelName = "";
                string hotelcode = "";
                //double maxrate = 0;
                //int rating = 0;
                string roomcode1 = "";
                double minrate = 0;

                double FinalTotal = 0;
                for (int h = 0; h <= NuiteeResponse.Tables["hotelInfo"].Rows.Count - 1; h++)
                {

                    Item.ResponseType = "Success";

                    #region "When not Need Room"  
                    // add hotel code from price only and room code blank here
                    if (SearchPara.Filters.IsShowRooms == 0)
                    {
                        #region "Hotel Code Block "

                        string HotelCode = Convert.ToString(NuiteeResponse.Tables["hotelInfo"].Rows[h]["hotelCode"]);
                        string HotelName = Convert.ToString(NuiteeResponse.Tables["hotelInfo"].Rows[h]["hotelName"]);
                        double Pricenew = Convert.ToDouble(NuiteeResponse.Tables["hotelResults"].Rows[h]["minPrice"]);
                        DataRow drXmlHotel = DtHotel.NewRow();
                        drXmlHotel["XMLHotelCode"] = HotelCode;
                        drXmlHotel["HotelName"] = HotelName;
                        drXmlHotel["SupplierName"] = SupplierName;
                        drXmlHotel["OfficeID"] = 0;
                        drXmlHotel["ContractID"] = 0;
                        drXmlHotel["Rating"] = 0;
                        drXmlHotel["Reviews"] = 0;
                        drXmlHotel["UniqueNo"] = HotelCode;
                        drXmlHotel["IsShowSupplier"] = false;
                        drXmlHotel["BookingStatus"] = "Available";
                        drXmlHotel["XMLCity"] = XmlCity;
                        drXmlHotel["SupplierCurrency"] = Convert.ToString(NuiteeResponse.Tables["getCityAvailabilityRes"].Rows[0]["currency"]);
                        drXmlHotel["FinalSellingPrice"] = Pricenew;
                        drXmlHotel["SellingPrice"] = Pricenew;
                        DtHotel.Rows.Add(drXmlHotel);
                        FinalTotal = Pricenew;
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
                            _markup.GetHotelDataCommon(ref MinPrice, ref willAddOrNot, ref OwnsystemHotelId, ref RaynaID, ref GiataId, ref Rating, ref drXmlHotel, commonSearch.AgentId, commonSearch.TariffID, commonSearch.dsAgentDetails, commonSearch.supplierGiataMaster, SearchPara, commonSearch.dtCountryCityMarkup, commonSearch.B2CMarkup, commonSearch.IsParentId, commonSearch.SubAgentMarkup, commonSearch.SearchByHotel, commonSearch.HotelIDS);
                            if (willAddOrNot)
                            {
                                #region "Room Logic"
                                DataTable dt_HotelRoom = _commonMethod.GenerateRoomtypeDataTableNewV2();
                                DataTable dt_Cancellation = _commonMethod.GenerateCancellationDataTableNewV2();
                                DataTable dt_RateBreackUp = _commonMethod.GenerateoomRateBreackUpDataTableNewV2();
                                #endregion
                                decimal LowestPrice = Convert.ToDecimal(MinPrice).CalculatePrice(commonSearch.tokenData, commonSearch.advancedOptions, Currency);
                                Options ops = (await _supplierRes.GetCommonRoomForSuppliers(GiataId, commonSearch.tokenData, commonSearch.advancedOptions, Convert.ToDouble(LowestPrice), drXmlHotel, dt_HotelRoom, dt_Cancellation, dt_RateBreackUp, SearchPara));
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

                        string HotelCode = Convert.ToString(NuiteeResponse.Tables["hotelInfo"].Rows[h]["hotelCode"]);
                        string HotelName = Convert.ToString(NuiteeResponse.Tables["hotelInfo"].Rows[h]["hotelName"]);
                        double Pricenew = Convert.ToDouble(NuiteeResponse.Tables["hotelResults"].Rows[h]["minPrice"]);
                        DataRow drXmlHotel = DtHotel.NewRow();
                        drXmlHotel["XMLHotelCode"] = HotelCode;
                        drXmlHotel["HotelName"] = HotelName;
                        drXmlHotel["SupplierName"] = SupplierName;
                        drXmlHotel["OfficeID"] = 0;
                        drXmlHotel["ContractID"] = 0;
                        drXmlHotel["Rating"] = 0;
                        drXmlHotel["Reviews"] = 0;
                        drXmlHotel["UniqueNo"] = HotelCode;
                        drXmlHotel["IsShowSupplier"] = false;
                        drXmlHotel["BookingStatus"] = "Available";
                        drXmlHotel["XMLCity"] = XmlCity;
                        drXmlHotel["SupplierCurrency"] = Convert.ToString(NuiteeResponse.Tables["getCityAvailabilityRes"].Rows[0]["currency"]);
                        drXmlHotel["FinalSellingPrice"] = Pricenew;
                        drXmlHotel["SellingPrice"] = Pricenew;
                        DtHotel.Rows.Add(drXmlHotel);
                        FinalTotal = Pricenew;
                        if (FinalTotal > 0)
                        {
                            #region "Room code Block"
                            bool willAddOrNot = true;
                            Int64 RaynaID = 0;
                            Int64 GiataId = 0;
                            int Rating = 0;
                            Int64 OwnsystemHotelId = 0;
                            string Provider = SupplierName;
                            double MinPrice = FinalTotal;
                            _markup.GetHotelDataCommon(ref MinPrice, ref willAddOrNot, ref OwnsystemHotelId, ref RaynaID, ref GiataId, ref Rating, ref drXmlHotel, commonSearch.AgentId, commonSearch.TariffID, commonSearch.dsAgentDetails, commonSearch.supplierGiataMaster, SearchPara, commonSearch.dtCountryCityMarkup, commonSearch.B2CMarkup, commonSearch.IsParentId, commonSearch.SubAgentMarkup, commonSearch.SearchByHotel, commonSearch.HotelIDS);
                            if (willAddOrNot)
                            {
                                #region "Room Logic"
                                DataTable dt_HotelRoom = _commonMethod.GenerateRoomtypeDataTableNewV2();
                                DataTable dt_Cancellation = _commonMethod.GenerateCancellationDataTableNewV2();
                                DataTable dt_RateBreackUp = _commonMethod.GenerateoomRateBreackUpDataTableNewV2();
                                #endregion

                                double CurrencyFactorVOT = 1;//------------------------------------------need to change
                                if (Item.CurrencyCode.ToLower() != commonSearch.tokenData.SiteCurrency.ToLower())
                                {
                                    CurrencyFactorVOT = _commonMethod.GetROEByCurrency(Currency, commonSearch.tokenData.dtROE); ;
                                }

                                DataSet dsTemp = new DataSet();
                                #endregion

                                #region RoomOnly RQ
                                int CutOffDays = XMLHotelBookingCommon.CutOffDays;
                                CutOffDays = _commonMethod.GetCutOffDays(commonSearch.dtSuplierMarkup, SupplierName);

                                double SubAgentMarkup = 0;
                                int IsParentId = 1, agentID = 0;
                                //if (dtAgentDetail != null)
                                //{
                                //    IsParentId = Convert.ToInt32(dtAgentDetail.Rows[0]["IsParentId"]);
                                //    SubAgentMarkup = Convert.ToDouble(dtAgentDetail.Rows[0]["B2CMarkupPer"]);
                                //    agentID = Convert.ToInt32(dtAgentDetail.Rows[0]["AgentID"]);
                                //}
                                double AgentPercentage = 0.0;
                                Boolean blnIsShowSupplier = false;
                                double AgentMarkupAmount = 0.0;
                                Boolean isPerce = true;

                                double AgentCompanyPercentage = Convert.ToDouble(SearchPara.AgentCompanyPercentage);
                                bool IsB2C = Convert.ToBoolean(Convert.ToByte(SearchPara.IsB2BOrB2C));
                                double B2CMarkup = (IsB2C == false ? AgentCompanyPercentage : commonSearch.B2CMarkup);
                                if (IsB2C == true)
                                    AgentPercentage = 0;

                                int NoOfNight = SearchPara.NoofNights;
                                double Factor = 1;
                                //Currency = "USD";
                                Currency = Item.CurrencyCode;
                                _markup.getAgetMarkupSetting(Provider, GiataId.ToString(), commonSearch.dtSuplierMarkup, commonSearch.dtMarkupDetails, ref AgentPercentage, ref AgentMarkupAmount, ref blnIsShowSupplier, ref isPerce, commonSearch.AgentId, Rating, SearchPara.CityID, SearchPara.CountryID.ToString(), SearchPara.CheckInDate);
                                //Common.getAgetMarkupSetting(Provider, GIITACode, dtSuplierMarkup, dtMarkupDetails, ref AgentPercentage, ref AgentMarkupAmount, ref blnIsShowSupplier, ref isPerce, agentID, hotelRating, SearchParam.CityID, SearchParam.CountryId, CheckinDate);
                                #endregion
                                //  string Response = "NA";
                                {
                                    //if (ResponseData == string.Empty || ResponseData == "")
                                    //    return options;


                                    //ResponseData = XElement.Parse(ResponseData).ToString(SaveOptions.DisableFormatting);

                                    //ResponseData = ResponseData.Replace("<hotelResults><hotelResults>", "<hotelResults1><hotelResults>");
                                    //ResponseData = ResponseData.Replace("</hotelResults></hotelResults>", "</hotelResults></hotelResults1>");

                                    //ResponseData = ResponseData.Replace("<roomGuests><roomGuests>", "<roomGuests1><roomGuests>");
                                    //ResponseData = ResponseData.Replace("</roomGuests></roomGuests>", "</roomGuests></roomGuests1>");



                                    //ResponseData = ResponseData.Replace("<cancelPoliciesInfos><cancelPolicyInfos>", "<cancelPoliciesInfos1><cancelPolicyInfos>");
                                    //ResponseData = ResponseData.Replace("</refundableTag></cancelPoliciesInfos>", "</refundableTag></cancelPoliciesInfos1>");

                                    //ResponseData = ResponseData.Replace("<rateDetails><cancelPoliciesInfos>", "<rateDetails><cancelPoliciesInfos1>");

                                    //ResponseData = ResponseData.Replace("<rooms><rooms>", "<rooms1><rooms>");
                                    //ResponseData = ResponseData.Replace("</rooms></rooms>", "</rooms></rooms1>");

                                    //ResponseData = ResponseData.Replace("</tax><taxesAndFees>", "</tax><taxesAndFees1>");
                                    //ResponseData = ResponseData.Replace("</rooms1><taxesAndFees>", "</rooms1><taxesAndFees1>");
                                    //ResponseData = ResponseData.Replace("</taxesAndFees><totalPrice>", "</taxesAndFees1><totalPrice>");

                                    //ResponseData = ResponseData.Replace("<rateDetails><maxRate>", "<rateDetails1><maxRate>");
                                    //ResponseData = ResponseData.Replace("</rateDetails></hotelResults>", "</rateDetails1></hotelResults>");

                                    //DataSet DsResponse = Common.GetDataSetFromXMLString(ResponseData);
                                    string SessionId = Convert.ToString(NuiteeResponse.Tables["getCityAvailabilityRes"].Rows[0]["sessionId"]);
                                    string roomcode = "";
                                    roomcode1 = "";
                                    string roomdescription = "";
                                    string boardDescription = "";
                                    string boardId = "";
                                    double TotalPrice = 0;
                                    ratedeatailcode1 = "";
                                    string RoomTypeCode = "";
                                    string Remark = "";


                                    //for (int u = 0; u <= NuiteeResponse.Tables["ratedetails1"].Rows.Count - 1; u++)
                                    //{
                                    int ratedetail1 = Convert.ToInt32(NuiteeResponse.Tables["rateDetails1"].Rows[h]["rateDetails1_Id"]);
                                    DataRow[] drRoomType5 = NuiteeResponse.Tables["rateDetails"].Select("rateDetails1_Id='" + ratedetail1 + "'");
                                    for (int i = 0; i < drRoomType5.Length; i++)
                                    {

                                        bool istrue = true;
                                        string ratedeatailcode = "";
                                        string mealname = "";
                                        string RoomName = "";
                                        roomdescription = "";
                                        int ratedetail = Convert.ToInt32(drRoomType5[i]["rateDetails_Id"]);
                                        ratedeatailcode = Convert.ToString(drRoomType5[i]["rateDetailCode"]);
                                        //string remarks = Convert.ToString(drRoomType5[i]["remarks"]);
                                        TotalPrice = Convert.ToDouble(drRoomType5[i]["totalPrice"]);


                                        string TaxAndFeesCharges = "";
                                        if (NuiteeResponse.Tables.Contains("taxesAndFees1") && NuiteeResponse.Tables["taxesAndFees1"] != null)
                                        {
                                            DataRow[] drTaxFees1 = NuiteeResponse.Tables["taxesAndFees1"].Select("rateDetails_Id='" + ratedetail + "'");
                                            if (drTaxFees1.Count() > 0)
                                            {
                                                int taxesAndFees1_Id = Convert.ToInt32(drTaxFees1[0]["taxesAndFees1_Id"]);

                                                if (NuiteeResponse.Tables.Contains("taxesAndFees") && NuiteeResponse.Tables["taxesAndFees"] != null)
                                                {
                                                    DataRow[] drTaxFees = NuiteeResponse.Tables["taxesAndFees"].Select("taxesAndFees1_Id='" + taxesAndFees1_Id + "'");
                                                    if (drTaxFees.Count() > 0)
                                                    {
                                                        string TAXFEES = "";

                                                        for (int x = 0; x < drTaxFees.Count(); x++)
                                                        {

                                                            string TaxFeesIncluded = Convert.ToString(drTaxFees[x]["included"]);
                                                            if (TaxFeesIncluded.ToLower() == "false")
                                                            {
                                                                if (x == 0)
                                                                    TAXFEES = "Please note that customer has to pay below charges at Hotel : <br><br><b>" + Convert.ToString(drTaxFees[x]["description"]) + " : </b> " + Convert.ToDouble(drTaxFees[x]["amount"]) + " " + Convert.ToString(drTaxFees[x]["currency"]) + "";
                                                                else
                                                                    TAXFEES = TAXFEES + "<be><br><b>" + Convert.ToString(drTaxFees[x]["description"]) + " : </b> " + Convert.ToDouble(drTaxFees[x]["amount"]) + " " + Convert.ToString(drTaxFees[x]["currency"]) + "";
                                                            }
                                                        }
                                                        TaxAndFeesCharges = TAXFEES;
                                                    }
                                                }
                                            }
                                        }


                                        DataRow[] drRoomType = NuiteeResponse.Tables["rooms1"].Select("rateDetails_Id='" + ratedetail + "'");
                                        for (int m = 0; m < drRoomType.Length; m++)
                                        {
                                            if (istrue == false)
                                            {
                                                break;
                                            }
                                            int roomsId1 = 0;
                                            int roomsId = Convert.ToInt32(drRoomType[m]["rooms1_Id"]);

                                            DataRow[] drRoomType1 = NuiteeResponse.Tables["rooms"].Select("rooms1_Id='" + roomsId + "'");


                                            for (int k = 0; k < drRoomType1.Length; k++)
                                            {
                                                if (istrue == false)
                                                {
                                                    break;
                                                }
                                                int roomsId2 = 0;
                                                roomcode = "";
                                                if (k == 0)
                                                {
                                                    RoomTypeCode = Convert.ToString(drRoomType1[k]["roomCode"]);
                                                }
                                                else
                                                {
                                                    RoomTypeCode = RoomTypeCode + "###" + Convert.ToString(drRoomType1[k]["roomCode"]);
                                                }
                                                //roomcode = Convert.ToString(drRoomType1[k]["roomCode"]);
                                                //roomcode1 = roomcode1 + "###" + roomcode;
                                                RoomName = Convert.ToString(drRoomType1[k]["roomDescription"]);
                                                roomdescription = roomdescription + "&&&" + RoomName;

                                                //
                                                roomsId2 = Convert.ToInt32(drRoomType1[k]["rooms_Id"]);
                                                Remark = Convert.ToString(drRoomType1[k]["roomRemarks"]);
                                                DataRow[] drRoomType2 = NuiteeResponse.Tables["includedBoard"].Select("rooms_Id='" + roomsId2 + "'");

                                                for (int j = 0; j < drRoomType2.Length; j++)
                                                {
                                                    if (istrue == false)
                                                    {
                                                        break;
                                                    }
                                                    int roomsId3 = Convert.ToInt32(drRoomType2[j]["rooms_Id"]);
                                                    if (roomsId2 == roomsId3)
                                                    {
                                                        boardDescription = Convert.ToString(drRoomType2[j]["boardDescription"]);
                                                        boardId = Convert.ToString(drRoomType2[j]["boardId"]);
                                                        if (k == 0)
                                                        {
                                                            mealname = Convert.ToString(drRoomType2[0]["boardId"]);
                                                        }
                                                        if (mealname != boardId)
                                                        {
                                                            istrue = false;
                                                        }
                                                    }
                                                }
                                            }


                                        }
                                        string[] spitid = RoomTypeCode.Split(new String[] { "###" }, StringSplitOptions.None);
                                        if (istrue == false || (spitid.Length + 1) == SearchPara.Rooms.Count)
                                        {
                                            continue;
                                        }
                                        //string ratedetailc = ratedeatailcode1 +'$';
                                        string RoomType = HotelCode + "$$$" + SessionId + "$$$" + ratedeatailcode + "$$$" + RoomTypeCode + "$$$" + boardId;
                                        RoomType = RoomType.Replace("'", "");
                                        string[] RoomName2 = Convert.ToString(roomdescription).Split(new String[] { "&&&" }, StringSplitOptions.None);

                                        string RoomN = "";
                                        for (int k = 1; k < RoomName2.Length; k++)
                                        {
                                            RoomN = RoomN + " [" + k + "]" + RoomName2[k];
                                        }
                                        roomdescription = RoomN;

                                        #region"Ratebreakup"
                                        //DateTime dtCheckInDateForRate = DateTime.ParseExact(SearchPara.CheckInDate, "dd/MM/yyyy", null);
                                        //double RatebreakupPrice = TotalPrice / NoOfNight;
                                        //#region Vat
                                        //Double VatOnRateBrackUp = 0;
                                        //RatebreakupPrice = _markup.ProcessHotelRateBreakupPrice_WithMarkup(1, SearchPara.CityID, Convert.ToString(SearchPara.CountryID), RatebreakupPrice, B2CMarkup, IsParentId, isPerce, AgentPercentage, AgentMarkupAmount, SubAgentMarkup, ref VatOnRateBrackUp);
                                        //#endregion
                                        //for (int night = 0; night < SearchPara.NoofNights; night++)
                                        //{
                                        //    string rateWeekColumn = Convert.ToString(dtCheckInDateForRate.Date.AddDays(night).DayOfWeek).Substring(0, 3);
                                        //    DataRow drNuitee_RateBreackUp = dt_RateBreackUp.NewRow();
                                        //    drNuitee_RateBreackUp["RoomNo"] = "1";
                                        //    drNuitee_RateBreackUp["SupplierName"] = Provider;
                                        //    drNuitee_RateBreackUp["OwnsystemHotelId"] = OwnsystemHotelId;
                                        //    drNuitee_RateBreackUp["ContractID"] = 0;
                                        //    drNuitee_RateBreackUp["RoomTypeName"] = roomdescription;
                                        //    drNuitee_RateBreackUp["RoomTypeId"] = RoomType;
                                        //    drNuitee_RateBreackUp["MealId"] = boardId;
                                        //    drNuitee_RateBreackUp["Meal"] = boardId;
                                        //    drNuitee_RateBreackUp["BookingStatus"] = "Available";
                                        //    drNuitee_RateBreackUp["DateOfWeek"] = rateWeekColumn;
                                        //    drNuitee_RateBreackUp["Date"] = dtCheckInDateForRate.Date.AddDays(night).Date;
                                        //    drNuitee_RateBreackUp["Rate"] = RatebreakupPrice;
                                        //    drNuitee_RateBreackUp["RateType"] = "A Rate";
                                        //    drNuitee_RateBreackUp["SupplierCurrency"] = Currency;
                                        //    drNuitee_RateBreackUp["BuyingPrice"] = RatebreakupPrice;
                                        //    dt_RateBreackUp.Rows.Add(drNuitee_RateBreackUp);
                                        //    dt_RateBreackUp.AcceptChanges();
                                        //}
                                        #endregion
                                        #region Rooms
                                        DataRow drNuiteeRoom = dt_HotelRoom.NewRow();
                                        drNuiteeRoom["OwnSystemHotelId"] = OwnsystemHotelId;
                                        drNuiteeRoom["RoomTypeName"] = roomdescription;
                                        drNuiteeRoom["RoomTypeCode"] = roomcode;
                                        drNuiteeRoom["MealId"] = boardId;
                                        drNuiteeRoom["MealName"] = boardId;
                                        drNuiteeRoom["ContractID"] = 0;
                                        drNuiteeRoom["BoardType"] = boardId;
                                        drNuiteeRoom["RoomTypeId"] = RoomType;
                                        int noofadults = 0;
                                        int noofchilds = 0;
                                        for (int x = 0; x < SearchPara.Rooms.Count; x++)
                                        {
                                            int ADT = SearchPara.Rooms[x].NoofAdults;
                                            int CHD = SearchPara.Rooms[x].NoofChild;
                                            noofadults = ADT + noofadults;
                                            noofchilds = CHD + noofchilds;
                                        }
                                        drNuiteeRoom["OfficeId"] = 0;
                                        drNuiteeRoom["AdultsOccupancy"] = noofadults;
                                        drNuiteeRoom["ChildOccupancy"] = noofchilds;
                                        drNuiteeRoom["MaxExtraBed"] = "0";
                                        drNuiteeRoom["RoomNo"] = "1";
                                        drNuiteeRoom["RoomSequenceNo"] = "";
                                        drNuiteeRoom["Type"] = "A type";
                                        drNuiteeRoom["BookingStatus"] = "Available";
                                        drNuiteeRoom["UniqueNo"] = "";
                                        drNuiteeRoom["OfferDescription"] = "";
                                        drNuiteeRoom["OfferCode"] = "";
                                        drNuiteeRoom["EssentialInformation"] = Remark + "</br></br>" + TaxAndFeesCharges;
                                        drNuiteeRoom["Characteristic"] = XmlNationality;
                                        double CompanyBuyingPrice = Convert.ToDouble(TotalPrice);
                                        double ItemPrice = Convert.ToDouble(TotalPrice);


                                        _markup.ProcessHotelRoom_WithMarkup(Factor, Currency, true, true, SearchPara.NoofNights, SearchPara.CityID, Convert.ToString(SearchPara.CountryID), SearchPara.CheckInDate, SearchPara.CheckOutDate, ref ItemPrice, ref drNuiteeRoom, isPerce, AgentPercentage, AgentMarkupAmount, SubAgentMarkup, B2CMarkup, IsParentId);



                                        // Common.ProcessHotelRoom_WithMarkup(Factor, Currency, true, true, SearchParam.NoofNights, SearchParam.CityID, SearchParam.CountryId, SearchParam.CheckInDate, SearchParam.CheckOutDate, ref ItemPrice, ref drNuiteeRoom, isPerce, AgentPercentage, AgentMarkupAmount, SubAgentMarkup, B2CMarkup, IsParentId);

                                        drNuiteeRoom["SupplierName"] = Provider;
                                        dt_HotelRoom.Rows.Add(drNuiteeRoom);
                                        #endregion


                                        string Message = Convert.ToString(NuiteeResponse.Tables["responseStatus"].Rows[0]["statusMessage"]);

                                        #region CancellationPolicy --Added on 120525
                                        double Penalty = CompanyBuyingPrice;
                                        double VatOnPolicy = 0;
                                        double markup_penalty = 0;
                                        DateTime canceldate = DateTime.Now;
                                        double MainAgentCharge = 0;
                                        double CompanyChargeAmount = 0;
                                        string uniqid = Guid.NewGuid().ToString();

                                        if (NuiteeResponse.Tables.Contains("cancelPoliciesInfos1"))
                                        {
                                            DataRow[] drcancelPoliciesInfos1 = NuiteeResponse.Tables["cancelPoliciesInfos1"].Select("rateDetails_Id='" + ratedetail + "'");
                                            int cancelPoliciesInfos1_Id = Convert.ToInt32(drcancelPoliciesInfos1[0]["cancelPoliciesInfos1_Id"]);
                                            string tag = Convert.ToString(drcancelPoliciesInfos1[0]["refundableTag"]);

                                            DateTime fromDate = DateTime.Now.Date;
                                            DateTime CancelDate = fromDate;
                                            if (tag == "RFN")
                                            {
                                                DataRow[] drcancelPoliciesInfos = NuiteeResponse.Tables["cancelPolicyInfos"].Select("cancelPoliciesInfos1_Id='" + cancelPoliciesInfos1_Id + "'");

                                                canceldate = Convert.ToDateTime(drcancelPoliciesInfos[0]["cancelTime"]);
                                                double amount = Convert.ToDouble(drcancelPoliciesInfos[0]["amount"]);

                                                DateTime curDate = _commonMethod.GetTimeZoneDate(SearchPara.CurrentDate);
                                                DateTime checkdate = DateTime.ParseExact(SearchPara.CheckInDate, "dd/MM/yyyy", null);

                                              

                                                if (canceldate.AddDays(CutOffDays).Date > DateTime.Now.Date)
                                                {
                                                    CancelDate = canceldate.AddDays(CutOffDays).Date;
                                                    checkdate = checkdate.AddDays(CutOffDays).Date;
                                                }
                                                else
                                                {
                                                    CancelDate = DateTime.Now;
                                                }


                                                markup_penalty = _markup.ProcessHotelCancelationPrice_WithMarkup(ref MainAgentCharge, SearchPara.NoofNights, SearchPara.CityID, Convert.ToString(SearchPara.CountryID), Penalty, isPerce, B2CMarkup, AgentPercentage, AgentMarkupAmount, IsParentId, SubAgentMarkup, ref VatOnPolicy);


                                                if (curDate.Date < CancelDate)
                                                {
                                                    double vharge = 0;
                                                    dt_Cancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref vharge, dt_Cancellation.NewRow(), CurrencyFactor, Item.CurrencyCode, Provider, Convert.ToString(OwnsystemHotelId), Convert.ToString(1), Convert.ToString(RoomType), RoomName, boardId, DateTime.Now.Date, CancelDate.AddDays(-1), 0, 0, "", VatOnPolicy, uniqid, "NO", "", "", "", ""));
                                                    dt_Cancellation.AcceptChanges();

                                                    dt_Cancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref ItemPrice, dt_Cancellation.NewRow(), CurrencyFactor, Item.CurrencyCode, Provider, Convert.ToString(OwnsystemHotelId), Convert.ToString(1), Convert.ToString(RoomType), RoomName, boardId, Convert.ToDateTime(CancelDate), DateTime.ParseExact(SearchPara.CheckInDate, "dd/MM/yyyy", null), CompanyBuyingPrice, ItemPrice, "0", VatOnPolicy, uniqid, "Yes", "", "", "", ""));
                                                    dt_Cancellation.AcceptChanges();
                                                }
                                                else
                                                {
                                                    dt_Cancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref ItemPrice, dt_Cancellation.NewRow(), CurrencyFactor, Item.CurrencyCode, Provider, Convert.ToString(OwnsystemHotelId), Convert.ToString(1), Convert.ToString(RoomType), RoomName, boardId, DateTime.Now.Date, Convert.ToDateTime(CancelDate), CompanyBuyingPrice, ItemPrice, "0", VatOnPolicy, uniqid, "Yes", "", "", "", ""));
                                                    dt_Cancellation.AcceptChanges();
                                                }
                                            }
                                            else
                                            {
                                                dt_Cancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref ItemPrice, dt_Cancellation.NewRow(), CurrencyFactor, Item.CurrencyCode, Provider, Convert.ToString(OwnsystemHotelId), Convert.ToString(1), Convert.ToString(RoomType), RoomName, boardId, DateTime.Now.Date, Convert.ToDateTime(CancelDate), CompanyBuyingPrice, ItemPrice, "0", VatOnPolicy, uniqid, "Yes", "", "", "", ""));
                                                dt_Cancellation.AcceptChanges();
                                            }

                                                
                                        }
                                        else
                                        {

                                            DateTime CancelDate = DateTime.Now;
                                            dt_Cancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref ItemPrice, dt_Cancellation.NewRow(), CurrencyFactor, Item.CurrencyCode, Provider, Convert.ToString(OwnsystemHotelId), Convert.ToString(1), Convert.ToString(RoomType), RoomName, boardId, DateTime.Now.Date, Convert.ToDateTime(CancelDate), CompanyBuyingPrice, ItemPrice, "0", VatOnPolicy, uniqid, "Yes", "", "", "", ""));
                                            dt_Cancellation.AcceptChanges();
                                        }
                                        #endregion

                                    }
                                    // }
                                }
                                decimal LowestPrice = Convert.ToDecimal(MinPrice).CalculatePrice(commonSearch.tokenData, commonSearch.advancedOptions, Currency);
                                Options ops = (await _supplierRes.GetCommonRoomForSuppliers(GiataId, commonSearch.tokenData, commonSearch.advancedOptions, Convert.ToDouble(LowestPrice), drXmlHotel, dt_HotelRoom, dt_Cancellation, dt_RateBreackUp, SearchPara));
                                if (ops != null)
                                    options.Add(ops);
                            }
                            #endregion
                        }

                    }
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
                    if (SearchPara != null && ResProccessTimerLogActive == "1" && timerroomx.ElapsedMilliseconds > Convert.ToInt32(ResProccessTimeValueforSaveLog))
                    {
                        timerroomx.Stop();
                        SearchPara.SupplierTime += ",|Finish:Nuitee Process Response  " + "|Time :" + timerroomx.Elapsed.ToString();
                        _ =  Task.Run(() =>  _commonMethod.FireAway_SQL_ResPorcessTimeLog("", timerroomx.Elapsed.ToString(), SupplierName, SearchPara.SearchKey, 1, SearchPara.AgentID));
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

        public async Task<CancellationDto> Get_Nuitee_CancellationPolicy(double roomprice, string rating, string ContractID, string Type, DataSet dsHotel, string hotelcode, string citycode, string ownsystemid, string RoomTypeId_Own, string MealID, string Board_Text, DataTable dtAgentDetail, DataTable dtSuplierMarkup, DataTable dtSearchConfig, HotelSearchParameter objSearPera, string roomno, string provider, int AgentId, DataTable dtMarkupDetails,string SearchKey, string GiataId = "0", int hotelRating = 0,string SiteCurrency="USD")
        {
            CancellationDto cancellationDto = new CancellationDto();
            string IsBooking = "Available";
            string Error = "";
            try
            {
                DataTable dtNuitee_Cancellation = _commonMethod.GenerateCancellationDataTableNewV2();

                //DataTable dtInrCurrency = Common.GetCountryCurrency(XMLHotelBookingCommon.RTSCurrency);
                //double CurrencyFactorRTS = Convert.ToDouble(dtInrCurrency.Rows[0]["AmtToExch"]);
               
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
                // for (int c = 0; c < dsHotel.Tables[1].DefaultView.Count; c++)
                {
                    string CheckinDate = objSearPera.CheckInDate;
                    _markup.getAgetMarkupSetting(provider, GiataId, dtSuplierMarkup, dtMarkupDetails, ref AgentPercentage, ref AgentMarkupAmount, ref blnIsShowSupplier, ref isPerce, AgentId, hotelRating, objSearPera.CityID, Convert.ToString(objSearPera.CountryID), CheckinDate);
                    if (dtAgentDetail != null)
                    {
                        IsParentId = Convert.ToInt32(dtAgentDetail.Rows[0]["IsParentId"]);
                        SubAgentMarkup = Convert.ToDouble(dtAgentDetail.Rows[0]["B2CMarkupPer"]);
                    }
                    int CutOffDays = XMLHotelBookingCommon.CutOffDays;
                    CutOffDays = _commonMethod.GetCutOffDays(dtSuplierMarkup, provider);
                    DataRow[] drSuplierMarkup = (dtSuplierMarkup.Select("SupplierName='"+ provider+"'"));
                    string UserId = Convert.ToString(drSuplierMarkup[0]["UserName"]);
                    string Password = Convert.ToString(drSuplierMarkup[0]["Password"]);
                    string nuiteeCurrency = Convert.ToString(drSuplierMarkup[0]["CurrencyShortName"]);

                    double CurrencyFactor = 1;
                    if (nuiteeCurrency != Convert.ToString(SiteCurrency))
                    {
                        DataTable dtInrCurrency = _commonMethod.GetCountryCurrency(nuiteeCurrency);
                        CurrencyFactor = Convert.ToDouble(dtInrCurrency.Rows[0]["AmtToExch"]);
                    }
                    else
                        CurrencyFactor = 1;


                    string CheckIn = objSearPera.CheckInDate;
                    CheckIn = CheckIn.Substring(6, 4) + "-" + CheckIn.Substring(3, 2) + "-" + CheckIn.Substring(0, 2);
                    string CheckOut = objSearPera.CheckOutDate;
                    CheckOut = CheckOut.Substring(6, 4) + "-" + CheckOut.Substring(3, 2) + "-" + CheckOut.Substring(0, 2);
                    string VOTUrl = Convert.ToString(drSuplierMarkup[0]["WebSiteURL"]);
                    //string RTSRemarkurl = Convert.ToString(drSuplierMarkup[0]["WebSiteURL"]) + XMLHotelBookingCommon.RTSHotelSearchAction;
                    int SID = Convert.ToInt32(drSuplierMarkup[0]["SupplierID"]);
                    DataRow[] drSearchConfig = (dtSearchConfig.Select("SupplierID='" + SID + "'"));
                    int SearchTimeOut = Convert.ToInt32(drSearchConfig[0]["BookingConfirmationTimeout"]);
                    //string PropertyId = ContractID;
                    int NoOfNights = objSearPera.NoofNights;
                    string RoomName = "";// Convert.ToString(dsHotel.Tables[1].DefaultView[c]["RoomTypeName"]);
                    string TotalPrice = "";
                    string Remark = "";
                    int Noofroom = objSearPera.Rooms.Count;
                    //Convert.ToString(dsHotel.Tables[1].DefaultView[c]["EssentialInformation"]);
                    int RoomNo = Convert.ToInt32(roomno);
                    string Hotelcode = hotelcode;
                    int NoofAdults = objSearPera.Rooms[RoomNo - 1].NoofAdults;
                    int NoOfChild = objSearPera.Rooms[RoomNo - 1].NoofChild;

                    string CancelPolicyRQ = Get_Nuitee_CancellationPolicy_RQ(UserId, Password, RoomTypeId_Own, hotelcode, Noofroom, SearchTimeOut);

                    _commonMethod.ThirdPartyGetPolicyHistory(Convert.ToString(RoomTypeId_Own), Convert.ToString(ownsystemid), CancelPolicyRQ, "", "Before Hit Cancellation Policy Request", provider, "Response", "-", "");
                    #endregion

                    //if (CancelPolicyRQ != null)
                    //{
                    #region Cancellation Policy RS
                    //string ResponseData = GetSupplierResponse(ref Error, "", CancelPolicyRQ, VOTUrl, SearchTimeOut, provider, objSearPera);
                    //ResponseDto responseDto = await GetSupplierResponse("", CancelPolicyRQ, VOTUrl, SearchTimeOut, provider, objSearPera);
                    ResponseDto responseDto = await _commonMethod.GetSupplierResponse("", CancelPolicyRQ, VOTUrl, SearchTimeOut, provider, objSearPera);
                    string ResponseData = responseDto.Response;
                    Error = responseDto.Error;

                    _commonMethod.ThirdPartyGetPolicyHistory(Convert.ToString(RoomTypeId_Own), Convert.ToString(ownsystemid), CancelPolicyRQ, ResponseData, "After Hit Cancellation Policy Request", provider, "Response", "-", "");
                    //System.IO.File.WriteAllText(@"E:\Hardik\Nuitee Request Response\GetCancellationPolicyResponse.txt", ResponseData);

                    //ResponseData = XElement.Parse(ResponseData).ToString(SaveOptions.DisableFormatting);
                    //ResponseData = ResponseData.Replace("<cancelPoliciesInfos><cancelPolicyInfos>", "<cancelPoliciesInfos1><cancelPolicyInfos>");
                    //ResponseData = ResponseData.Replace("</refundableTag></cancelPoliciesInfos>", "</refundableTag></cancelPoliciesInfos1>");
                    ResponseData = XElement.Parse(ResponseData).ToString(SaveOptions.DisableFormatting);
                    ResponseData = ResponseData.Replace("<roomGuests><roomGuests>", "<roomGuests1><roomGuests>");
                    ResponseData = ResponseData.Replace("</roomGuests></roomGuests>", "</roomGuests></roomGuests1>");


                    ResponseData = ResponseData.Replace("<cancelPoliciesInfos><cancelPolicyInfos>", "<cancelPoliciesInfos1><cancelPolicyInfos>");
                    ResponseData = ResponseData.Replace("</refundableTag></cancelPoliciesInfos>", "</refundableTag></cancelPoliciesInfos1>");

                    ResponseData = ResponseData.Replace("<rooms><rooms>", "<rooms1><rooms>");
                    ResponseData = ResponseData.Replace("</rooms></rooms>", "</rooms></rooms1>");
                    ResponseData = ResponseData.Replace("<taxesAndFees><taxesAndFees>", "<taxesAndFees1><taxesAndFees>");
                    ResponseData = ResponseData.Replace("</taxesAndFees></taxesAndFees>", "</taxesAndFees></taxesAndFees1>");


                    string remark = "";
                    string uniqid = Guid.NewGuid().ToString();
                    double VatOnPolicy = 0;
                    double markup_penalty = 0;
                    DateTime canceldate = DateTime.Now;
                    double MainAgentCharge = 0;
                    double CompanyChargeAmount = 0;
                    DataSet DsResponse = _commonMethod.GetDataSetFromXMLString(ResponseData);
                    string Message = Convert.ToString(DsResponse.Tables["responseStatus"].Rows[0]["statusMessage"]);
                    if (DsResponse.Tables.Contains("cancelPolicyInfos") != true)
                    {
                        DateTime CancelDate = DateTime.Now;
                        TotalPrice = "0";
                        dtNuitee_Cancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref roomprice, dtNuitee_Cancellation.NewRow(), CurrencyFactor, nuiteeCurrency, provider, ownsystemid, Convert.ToString(roomno), Convert.ToString(RoomTypeId_Own), RoomName, MealID, DateTime.Now.Date, Convert.ToDateTime(CancelDate), roomprice, roomprice, "0", VatOnPolicy, uniqid, "Yes", TotalPrice, remark + "</br>", "", ""));
                        dtNuitee_Cancellation.AcceptChanges();
                    }
                    if (Message == "No availability was found for requested criteria.")
                    {
                        DateTime CancelDate = DateTime.Now;
                        TotalPrice = "0";
                        dtNuitee_Cancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref roomprice, dtNuitee_Cancellation.NewRow(), CurrencyFactor, nuiteeCurrency, provider, ownsystemid, Convert.ToString(roomno), Convert.ToString(RoomTypeId_Own), RoomName, MealID, Convert.ToDateTime(CancelDate), DateTime.Now.Date, roomprice, roomprice, "0", VatOnPolicy, uniqid, "Yes", TotalPrice, remark, "", ""));
                        dtNuitee_Cancellation.AcceptChanges();
                    }
                    else
                    {
                        canceldate = Convert.ToDateTime(DsResponse.Tables["cancelPolicyInfos"].Rows[0]["cancelTime"]);
                        double amount = Convert.ToDouble(DsResponse.Tables["cancelPolicyInfos"].Rows[0]["amount"]);
                        string tag = Convert.ToString(DsResponse.Tables["cancelPoliciesInfos1"].Rows[0]["refundableTag"]);
                        remark = Convert.ToString(DsResponse.Tables["confirmPropertyAvailabilityRes"].Rows[0]["remarks"]);
                        TotalPrice = Convert.ToString(DsResponse.Tables["roomAvailability"].Rows[0]["totalPrice"]);

                        MainAgentCharge = amount;

                        DateTime fDate = DateTime.Now.Date;
                        DateTime fromDate = DateTime.Now.Date;
                        bool ISRefundable = true;
                        CompanyChargeAmount = amount;

                        DateTime CancelDate = fromDate;
                        if (tag == "RFN")
                        {
                            #region DataRaw

                            double Penalty = 0;
                            // Penalty = Penalty * CurrencyFactorRTS;
                            DateTime curDate = _commonMethod.GetTimeZoneDate(objSearPera.CurrentDate);

                            #endregion

                            // CancelDate = CancelDeadlineDate.AddDays(15);
                            DateTime checkdate = DateTime.ParseExact(objSearPera.CheckInDate, "dd/MM/yyyy", null);
                            DateTime NowDate = DateTime.Now.Date;
                            if (canceldate.AddDays(CutOffDays).Date > DateTime.Now.Date)
                            {
                                CancelDate = canceldate.AddDays(CutOffDays).Date;
                                checkdate = checkdate.AddDays(CutOffDays).Date;
                            }
                            else
                            {
                                CancelDate = DateTime.Now;
                            }
                            #region Vat

                            markup_penalty = _markup.ProcessHotelCancelationPrice_WithMarkup(ref MainAgentCharge, objSearPera.NoofNights, objSearPera.CityID, Convert.ToString(objSearPera.CountryID), Penalty, isPerce, B2CMarkup, AgentPercentage, AgentMarkupAmount, IsParentId, SubAgentMarkup, ref VatOnPolicy);
                            #endregion

                            if (curDate.Date < CancelDate)
                            {
                                double vharge = 0;
                                dtNuitee_Cancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref vharge, dtNuitee_Cancellation.NewRow(), CurrencyFactor, nuiteeCurrency, provider, ownsystemid, Convert.ToString(roomno), Convert.ToString(RoomTypeId_Own), RoomName, MealID, DateTime.Now.Date, CancelDate.AddDays(-1), 0, 0, "", VatOnPolicy, uniqid, "NO", TotalPrice, remark + "</br>", "", ""));
                                dtNuitee_Cancellation.AcceptChanges();

                                dtNuitee_Cancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref roomprice, dtNuitee_Cancellation.NewRow(), CurrencyFactor, nuiteeCurrency, provider, ownsystemid, Convert.ToString(roomno), Convert.ToString(RoomTypeId_Own), RoomName, MealID, Convert.ToDateTime(CancelDate), DateTime.ParseExact(objSearPera.CheckInDate, "dd/MM/yyyy", null), roomprice, roomprice, "0", VatOnPolicy, uniqid, "Yes", TotalPrice, remark + "</br>", "", ""));
                                dtNuitee_Cancellation.AcceptChanges();
                            }
                            else
                            {
                                dtNuitee_Cancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref roomprice, dtNuitee_Cancellation.NewRow(), CurrencyFactor, nuiteeCurrency, provider, ownsystemid, Convert.ToString(roomno), Convert.ToString(RoomTypeId_Own), RoomName, MealID, DateTime.Now.Date, Convert.ToDateTime(CancelDate), roomprice, roomprice, "0", VatOnPolicy, uniqid, "Yes", TotalPrice, remark + "</br>", "", ""));
                                dtNuitee_Cancellation.AcceptChanges();
                            }
                        }
                        else
                        {
                            dtNuitee_Cancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref roomprice, dtNuitee_Cancellation.NewRow(), CurrencyFactor, nuiteeCurrency, provider, ownsystemid, Convert.ToString(roomno), Convert.ToString(RoomTypeId_Own), RoomName, MealID, DateTime.Now.Date, Convert.ToDateTime(CancelDate), roomprice, roomprice, "0", VatOnPolicy, uniqid, "Yes", TotalPrice, remark + "</br>", "", ""));
                            dtNuitee_Cancellation.AcceptChanges();
                        }
                    }
                }
                dsHotel.Tables[2].Merge(dtNuitee_Cancellation);

                cancellationDto.dsHotelData = dsHotel;
                cancellationDto.ISBooking = IsBooking;
                return cancellationDto;
                #endregion
            }
            catch (Exception ex)
            {
                _ = _exception.ErrorNew(ex, _LogAPI.Recheck, SearchKey);
                cancellationDto.dsHotelData = dsHotel;
                cancellationDto.ISBooking = "NotAvailable";
                return cancellationDto;
            }
        }

        public string Get_Nuitee_CancellationPolicy_RQ(string UserId, string Password, string RoomTypeId, string hotelcode, int Rooms, int SearchTimeOut)
        {
            try
            {
                #region Cancellation Policy and Price Check RQ

                //string[] spitid = RoomTypeId.Split(new String[] { "$$$" }, StringSplitOptions.None);
                //string HotelCode = spitid[0].ToString();
                //string SessionID = spitid[1].ToString();
                //string rateDetailCode1 = spitid[2];
                //string[] rateDetailCode = rateDetailCode1.ToString().Split(new String[] { "###" }, StringSplitOptions.None);
                //StringBuilder nuitee = new StringBuilder();
                //nuitee.Append("<soapenv:Envelope xmlns:soapenv=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:nuit=\"http://www.nuitee.ma\">");
                //nuitee.Append("<soapenv:Header/>");
                //nuitee.Append("<soapenv:Body>");
                //nuitee.Append("<nuit:performGetCancellationPolicy>");
                //nuitee.Append("<getCancellationPolicyReq>");
                //nuitee.Append("<sessionId>" + SessionID + "</sessionId>");
                //nuitee.Append("<hotelCode>" + HotelCode + "</hotelCode>");
                //nuitee.Append("<rateDetailCode>" + rateDetailCode[1] + "</rateDetailCode>");
                //nuitee.Append("<timeout>" + 20000 + "</timeout>");
                //nuitee.Append("</getCancellationPolicyReq>");
                //nuitee.Append("</nuit:performGetCancellationPolicy>");
                //nuitee.Append("</soapenv:Body>");
                //nuitee.Append("</soapenv:Envelope>");
                //string ReqString = nuitee.ToString();
                //return ReqString;


                string[] spitid = RoomTypeId.Split(new String[] { "$$$" }, StringSplitOptions.None);

                string HotelCode = spitid[0].ToString();
                string SessionID = spitid[1].ToString();
                string rateDetailCode1 = spitid[2];
                //string[] rateDetailCode = rateDetailCode1.ToString().Split(new String[] { "###" }, StringSplitOptions.None);
                string roomCode1 = spitid[3];
                string[] roomCode = roomCode1.ToString().Split(new String[] { "###" }, StringSplitOptions.None);
                string boardId = spitid[4];

                StringBuilder nuitee = new StringBuilder();
                nuitee.Append("<soapenv:Envelope xmlns:soapenv=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:nuit=\"http://www.nuitee.ma\">");
                nuitee.Append("<soapenv:Header/>");
                nuitee.Append("<soapenv:Body>");
                nuitee.Append("<nuit:performConfirmPropertyAvailability>");
                nuitee.Append("<confirmPropertyAvailabilityReq>");
                nuitee.Append("<sessionId>" + SessionID + "</sessionId>");
                nuitee.Append("<hotelCode>" + hotelcode + "</hotelCode>");
                nuitee.Append("<rateDetailCode>" + rateDetailCode1 + "</rateDetailCode>");
                nuitee.Append("<confirmedRooms>");
                for (int i = 0; i < Rooms; i++)
                {
                    nuitee.Append("<confirmedRooms>");
                    nuitee.Append("<boardId>" + boardId + "</boardId>");
                    nuitee.Append("<roomCode>" + roomCode[i] + "</roomCode>");
                    nuitee.Append("</confirmedRooms>");
                }
                nuitee.Append("</confirmedRooms>");
                nuitee.Append("<timeout>" + SearchTimeOut + "</timeout>");
                nuitee.Append("</confirmPropertyAvailabilityReq>");
                nuitee.Append("</nuit:performConfirmPropertyAvailability>");
                nuitee.Append("</soapenv:Body>");
                nuitee.Append("</soapenv:Envelope>");
                string ReqString = nuitee.ToString();
                return ReqString;
                #endregion
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
                return "";
            }

        }

        public async Task<ResponseDto> GetSupplierResponse(string strUniqueId, string strxmlRQRS, string strHost, Int32 TimeOut, string Provider = "", HotelSearchParameter para = null, bool DebugMode = false, string Searchkey = "")
        {
            System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
            timer.Start();
            //WebClient wc = new WebClient();
            //// wc.DownloadData(strHost, strxmlRQRS);
            //string resultData = wc.UploadString(strHost, "POST", strxmlRQRS);
            //return resultData;

            //SqlConnection conn1 = new SqlConnection(System.Web.Configuration.WebConfigurationManager.ConnectionStrings["ConnectionString"].ToString());
            //conn1.Open();
            //SqlCommand comm1 = new SqlCommand();
            //comm1.Connection = conn1;
            //comm1.CommandText = "insert into XMLRequestResponseTrack (XmlRequest,XmlResponse,ResponseDate,Provider,TypeRequest) values('" + strxmlRQRS + "','" + "" + "',getdate(),'','" + "Proper" + "')";
            //comm1.ExecuteScalar();
            //conn1.Close();
            string Error = "";
            string strOutput = "";

            ResponseDto responseDto = new ResponseDto();
            try
            {

                // App.Logger.Logger.Log("Request ::  " + strSupplier + " - " + strService);
                //strHost = strHost+ strxmlRQRS;

                HttpWebRequest Request;




                //strxmlRQRS = strxmlRQRS.Replace("&amp;", "&");
                //strxmlRQRS = strxmlRQRS.Replace("\n", string.Empty);
                //strxmlRQRS = strxmlRQRS.Replace("\t", string.Empty);
                //strxmlRQRS = strxmlRQRS.Replace(" ", string.Empty);




                Request = (HttpWebRequest)HttpWebRequest.Create(strHost);
                //string replacement = Regex.Replace(s, @"\t|\n|\r", ""); 

                Request.Timeout = TimeOut;
                Request.Accept = "text/xml";
                if (Provider.ToLower() == "gta")
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                Request.Method = "POST";

                Request.ContentType = "text/xml;charset=utf-8";

                Request.UserAgent = "Mozilla/4.0 (compatible; MSIE 6.0; MS Web Services Client Protocol 1.1.4322.2300";
                //----------Animesh Aox start


                if (strHost == XMLHotelBookingCommon.AOXSearch)
                {
                    Request.Headers.Add("SOAPAction", XMLHotelBookingCommon.AOXSearchAction);

                }
                if (strHost == XMLHotelBookingCommon.AOXReserve)
                {
                    Request.Headers.Add("SOAPAction", XMLHotelBookingCommon.AOXCancelAction);
                }
                ////------------Animesh Aox end

                //if (strSoapAction != string.Empty)
                //{
                //    Request.Headers.Add("SoapAction", strSoapAction);
                //}

                //if (strGzipCompression.Equals("Y"))
                //{
                //Request.Headers.Add("Accept-Encoding", "gzip");
                Request.Headers.Add("Accept-Encoding", "gzip,deflate");

                //Request.Timeout = 10000;
                //  }




                //Forming the request
                Stream newStream = null;

                //if (strMethod.ToLower().Equals("post") && !strSoapAction.ToLower().Equals("application/x-www-form-urlencoded"))
                //{

                //bhai Request

                //Travoco Change Start

                //Travoco Change End



                byte[] data = Encoding.ASCII.GetBytes(strxmlRQRS);
                Request.ContentLength = data.Length;

                newStream = Request.GetRequestStream();
                newStream.Write(data, 0, data.Length);

                // }

                //Getting the response
                try
                {
                    HttpWebResponse Response = (HttpWebResponse)Request.GetResponse();


                    Stream responseStream = Response.GetResponseStream();

                    //if (Provider.ToLower() != "gta")
                    //{
                    //   // if (strHost != Darina_BookUrl)
                    //        responseStream = new GZipStream(responseStream, CompressionMode.Decompress);
                    //}
                    if (Response.ContentEncoding.ToLower().Contains("gzip"))
                        responseStream = new System.IO.Compression.GZipStream(responseStream, System.IO.Compression.CompressionMode.Decompress);
                    else if (Response.ContentEncoding.ToLower().Contains("deflate"))
                        responseStream = new System.IO.Compression.DeflateStream(responseStream, System.IO.Compression.CompressionMode.Decompress);

                    //Read the output

                    StreamReader Reader = new StreamReader(responseStream, Encoding.UTF8);

                    strOutput = Reader.ReadToEnd();

                    responseStream.Close();
                    Reader.Close();
                    Response.Close();
                    timer.Stop();
                    if (para != null)
                    {
                        para.SupplierTime += ",SupplierName :" + Provider + " : " + "Execution Time :" + timer.Elapsed.ToString();
                        if (DebugMode)
                            _ = _commonMethod.XMLRequestResponseTrack(strxmlRQRS, strOutput, Provider, "Proper");
                    }
                    if (_keySettings.IsLogTrack == "1")
                    {
                        TimeSpan timeTaken = timer.Elapsed;

                        _exception.ErrorLog(Convert.ToString(_keySettings.LogTrackPath), Provider + "->" + strHost + "-> Execution Time : " + timer.Elapsed.ToString());
                        _ = _commonMethod.XMLRequestResponseTrack(strxmlRQRS, strOutput, Provider, "Proper");
                    }


                    //SqlConnection conn = new SqlConnection(System.Web.Configuration.WebConfigurationManager.ConnectionStrings["ConnectionString"].ToString());
                    //conn.Open();
                    //SqlCommand comm = new SqlCommand();
                    //comm.Connection = conn;
                    ////comm.CommandText = "insert into XMLRequestResponseTrack (XmlRequest,XmlResponse,ResponseDate,Provider,TypeRequest) values('" + strxmlRQRS + "','" + strOutput.Replace("'", "''") + "',getdate(),'','" + "Proper" + "')";
                    //comm.CommandText = "insert into XMLRequestResponseTrack (XmlRequest,XmlResponse,ResponseDate,Provider,TypeRequest) values('" + strxmlRQRS + "','" + strHost + "',getdate(),'','" + "Proper" + "')";
                    //comm.ExecuteScalar();
                    //conn.Close();
                }
                catch (WebException webExcp)
                {
                    try
                    {
                        HttpWebResponse httpResponse = (HttpWebResponse)webExcp.Response;
                        if (httpResponse.StatusDescription != null)
                        {
                            string result = httpResponse.StatusDescription.ToString();
                            string result1 = new StreamReader(webExcp.Response.GetResponseStream()).ReadToEnd();

                            //SqlConnection conn = new SqlConnection(System.Web.Configuration.WebConfigurationManager.ConnectionStrings["ConnectionString"].ToString());
                            //conn.Open();
                            //SqlCommand comm = new SqlCommand();
                            //comm.Connection = conn;
                            //comm.CommandText = "insert into XMLRequestResponseTrackError (XmlRequest,XmlResponse,ResponseDate,Provider,TypeRequest) values('" + strxmlRQRS + "','" + result1.Replace("'", "''").Replace(",", " ") + "',getdate(),'','" + "Proper" + "')";
                            //comm.ExecuteScalar();
                            //conn.Close();
                            _commonMethod.XMLRequestResponseTrackError(strxmlRQRS, result1, Provider, Searchkey);
                            Error = result1.ToString();

                            responseDto.Response = result1;
                            responseDto.Error = Error;
                            return responseDto;
                        }
                    }
                    catch (Exception ex)
                    {
                        _ = _exception.Error(ex, Searchkey);
                        Error = ex.Message.ToString();

                        responseDto.Response = "";
                        responseDto.Error = Error;
                        return responseDto;
                    }

                    //return result1;
                }
            }
            catch (Exception ex)
            {
                Error = ex.Message.ToString();
                _ = _exception.Error(ex, Searchkey);

                responseDto.Response = "";
                responseDto.Error = Error;
                return responseDto;
            }
            //trackRequest(strxmlRQRS, strOutput);
            responseDto.Response = strOutput;
            responseDto.Error = Error;
            return responseDto;
        }

        //prebook 07-07-25
        public async Task<PriceCheckNuiteeDto> NuiteePriceCheckRQRS(DataSet dsCartDetails, DataRow[] nuiteeDetails, string UserId, string Password, int i, int j, DataTable dtCheckPriceReqStructure, string UniqueNo, string AgentUniqueNo,  double FinalPrice, bool isForBooking, double CompanyBuyingPrice,  string PreBookingToken, string xmlhotelid, string Ipaddress,TokenData tokenData, string Searchkey = "", int AgentId = 0)
        {
            PriceCheckNuiteeDto priceCheckCMDto = new PriceCheckNuiteeDto();
            string responsetype = "Success";
            string time = "";
            string SupplierName = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["SupplierName"]).ToLower();
            string Error = "";
            string xslRequest = "";
            string ResponseData = "";
            //  string SupplierName = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["SupplierName"]).ToLower();
            try
            {
                #region Price Check RQ and Declaration

                string currency = Convert.ToString(nuiteeDetails[0]["CurrencyShortName"]);
                double CurrencyFactorNuitee = 1;
                //DataTable dtInrCurrency = Common.GetCountryCurrency(currency);
                if (currency.ToLower() != tokenData.SiteCurrency.ToLower())
                {
                    CurrencyFactorNuitee = _commonMethod.GetROEByCurrency(currency, tokenData.dtROE);

                }

                string countrycode = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomTypeId"]);
                //Common.GetWHLCurrencyByCountry("WHL", countrycode.Split('$')[2], ref WHLCurrency, "");
                //if (TBOCurrency != Convert.ToString(ConfigurationManager.AppSettings["SiteCurrency"]))
                //{
                //    DataTable dtInrCurrency = Common.GetCountryCurrency(TBOCurrency);
                //    CurrencyFactorTBO = Convert.ToDouble(dtInrCurrency.Rows[0]["AmtToExch"]);
                //}
                //DataTable dtInrCurrency = Common.GetCountryCurrency(XMLHotelBookingCommon.WHLCurrency);
                //CurrencyFactorWHL = Convert.ToDouble(dtInrCurrency.Rows[0]["AmtToExch"]);
                HotelSearchParameter objsearchpara = new HotelSearchParameter();
                DataSet dsTemp = new DataSet();
                string NuiteeUrl = Convert.ToString(nuiteeDetails[0]["WebSiteURL"]);
                string ownsystemhotelId = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["ServiceId"]);// Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["ownsystemhotelId"]);
                UserId = Convert.ToString(nuiteeDetails[0]["UserName"]);
                Password = Convert.ToString(nuiteeDetails[0]["Password"]);
                string SearchTimeOut = Convert.ToString(nuiteeDetails[0]["Password"]);
                string HotelId = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["ContractID"]);
                int UserCartId = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[j]["UserCartID"]);

                int UserCartDetailId = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[j]["UserCartDetailId"]);
                string RoomTypeId = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomTypeID"]);
                //string SupplierName = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["SupplierName"]).ToLower();
                string CheckIn = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["CheckInDate"]);//"20180327"
                CheckIn = CheckIn.Substring(0, 4) + "-" + CheckIn.Substring(4, 2) + "-" + CheckIn.Substring(6, 2);
                string CheckOut = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["CheckOutDate"]);
                CheckOut = CheckOut.Substring(0, 4) + "-" + CheckOut.Substring(4, 2) + "-" + CheckOut.Substring(6, 2);
                int NoOfNoghts = Convert.ToInt32(dsCartDetails.Tables[0].Rows[i]["NoOfNight"]);
                string MealId = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["MealPlanCode"]);
                string RoomName = "";// Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomType"]);
                string TypeSPUI = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["spui"]);
                int RoomNo = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[j]["RoomNumber"]);
                int NoOfRoom = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView.Count);
                string Confirmavability = "";

                xslRequest = NuiteePriceCheckRQ(NuiteeUrl, dsCartDetails, nuiteeDetails, RoomTypeId, MealId, ref RoomName, RoomNo, xmlhotelid, NoOfRoom, currency, CompanyBuyingPrice, Ipaddress, PreBookingToken);

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
                    //string ResponseData = XMLHotelBookingCommon.GetSupplierResponse(xslRequest, TBO_IndiaUrl, 120000, "Nuitee");

                    System.Diagnostics.Stopwatch timerroomprocess = new System.Diagnostics.Stopwatch();
                    timerroomprocess.Start();

                    ResponseDto responseDto = await GetSupplierResponse("", xslRequest, NuiteeUrl, 120000, SupplierName, objsearchpara);
                    ResponseData = responseDto.Response;
                    Error = responseDto.Error;
                    _commonMethod.ThirdPartyBookingHistory(Convert.ToString(UniqueNo), Convert.ToString(AgentUniqueNo), xslRequest, ResponseData + Error, "After get price " + SupplierName + " request hit " + j + "is for Price Check" + isForBooking, SupplierName, "response confirm", "-", "WithinEarth");

                    timerroomprocess.Stop();
                    time = timerroomprocess.Elapsed.ToString();

                    if (ResponseData == "" || ResponseData == null)
                    {
                        try
                        {
                            responsetype = "GeneralError";
                            string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, xslRequest, ResponseData, Error);
                        }
                        catch (Exception e)
                        {
                            _ = _exception.ErrorNew(e, _LogAPI.Prebook, Searchkey);

                        }
                        priceCheckCMDto.Flag = false;
                        return priceCheckCMDto;
                    }

                    //System.IO.File.WriteAllText(@"E:\Hardik\Nuitee Request Response\GetConfirmPropertyAvailabilityResponse.txt", ResponseData);

                    ResponseData = XElement.Parse(ResponseData).ToString(SaveOptions.DisableFormatting);
                    ResponseData = ResponseData.Replace("<roomGuests><roomGuests>", "<roomGuests1><roomGuests>");
                    ResponseData = ResponseData.Replace("</roomGuests></roomGuests>", "</roomGuests></roomGuests1>");


                    ResponseData = ResponseData.Replace("<cancelPoliciesInfos><cancelPolicyInfos>", "<cancelPoliciesInfos1><cancelPolicyInfos>");
                    ResponseData = ResponseData.Replace("</refundableTag></cancelPoliciesInfos>", "</refundableTag></cancelPoliciesInfos1>");

                    ResponseData = ResponseData.Replace("<rooms><rooms>", "<rooms1><rooms>");
                    ResponseData = ResponseData.Replace("</rooms></rooms>", "</rooms></rooms1>");

                    ResponseData = ResponseData.Replace("</tax><taxesAndFees>", "</tax><taxesAndFees1>");
                    ResponseData = ResponseData.Replace("</rooms1><taxesAndFees>", "</rooms1><taxesAndFees1>");
                    ResponseData = ResponseData.Replace("</taxesAndFees><totalPrice>", "</taxesAndFees1><totalPrice>");

                    ResponseData = ResponseData.Replace("<rateDetails><maxRate>", "<rateDetails1><maxRate>");
                    ResponseData = ResponseData.Replace("</rateDetails></hotelResults>", "</rateDetails1></hotelResults>");

                    //ResponseData = File.ReadAllText(@"E:\pricechangetbo.txt");

                    DataSet DsResponse = _commonMethod.GetDataSetFromXMLString(ResponseData);




                    string Message = Convert.ToString(DsResponse.Tables["responseStatus"].Rows[0]["statusMessage"]);
                    if (Message == "No availability was found for requested criteria.")
                    {
                        FinalPrice = 0;
                    }
                    Confirmavability = Convert.ToString(DsResponse.Tables["confirmPropertyAvailabilityRes"].Rows[0]["confirmPropertyCode"]);
                    PreBookingToken = Confirmavability;
                    priceCheckCMDto.PreBookingToken = PreBookingToken;

                    double FinalPrice1 = 0;
                    if (ResponseData != string.Empty && ResponseData != "" && DsResponse != null)
                    {
                        if (ResponseData == null)
                        {
                            FinalPrice = 0;
                            priceCheckCMDto.Flag = true;
                            priceCheckCMDto.ItemPrice = FinalPrice;

                            return priceCheckCMDto;
                        }
                        else
                        {

                            FinalPrice = Convert.ToDouble(DsResponse.Tables["roomAvailability"].Rows[0]["totalPrice"]) * CurrencyFactorNuitee;
                            if ((FinalPrice).TruncateDecimalPlaces(0) > (CompanyBuyingPrice).TruncateDecimalPlaces(0))
                            {
                                priceCheckCMDto.Flag = true;
                                priceCheckCMDto.ItemPrice = FinalPrice;
                                return priceCheckCMDto;
                            }
                            // Fina lPrice1 = FinalPrice1 + FinalPrice;
                            //FinalPrice = CompanyBuyingPrice;
                        }
                    }
                }
                bool confirm = await _commonMethod.UpdatePricechangeNuitee(Confirmavability, UserCartId);
            }
            #endregion
            catch (Exception e)
            {
                try
                {
                    Error = e.Message + e.StackTrace.ToString();
                    responsetype = "GeneralError";
                    string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, xslRequest, ResponseData, Error);
                }
                catch (Exception ex)
                {
                    _ = _exception.ErrorNew(e, _LogAPI.Prebook, Searchkey);

                }
                _ = _exception.ErrorNew(e, _LogAPI.Prebook, Searchkey);

            }
            if (FinalPrice == 0)
            {
                try
                {
                    responsetype = "GeneralError";
                    string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, xslRequest, ResponseData, Error);
                }
                catch (Exception e)
                {
                    _ = _exception.ErrorNew(e, _LogAPI.Prebook, Searchkey);

                }
            }
            else
            {
                try
                {
                    responsetype = "Success";
                    string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, xslRequest, ResponseData, Error);
                }
                catch (Exception e)
                {
                    _ =_exception.ErrorNew(e,_LogAPI.Prebook, Searchkey);
                }
            }

            priceCheckCMDto.Flag = false;
            priceCheckCMDto.ItemPrice = FinalPrice;
            return priceCheckCMDto;
        }

        public string NuiteePriceCheckRQ(string TBOUrl, DataSet dsCartDetails, DataRow[] nuiteeDetails, string RoomTypeId, string MealID, ref string RoomName, int RoomNo, string xmlhotelid, int NoOfRoom, string TBO_IndiaCurrency, double RooMPrice, string Ipaddress, string PreBookingToken)
        {
            try
            {
                //string ReqString = "";

                string[] spitid = RoomTypeId.Split(new String[] { "$$$" }, StringSplitOptions.None);

                string HotelCode = spitid[0].ToString();
                string SessionID = spitid[1].ToString();
                string rateDetailCode1 = spitid[2];
                //string[] rateDetailCode = rateDetailCode1.ToString().Split(new String[] { "###" }, StringSplitOptions.None);
                string roomCode1 = spitid[3];
                string[] roomCode = roomCode1.ToString().Split(new String[] { "###" }, StringSplitOptions.None);
                string boardId = spitid[4];

                StringBuilder nuitee = new StringBuilder();
                nuitee.Append("<soapenv:Envelope xmlns:soapenv=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:nuit=\"http://www.nuitee.ma\">");
                nuitee.Append("<soapenv:Header/>");
                nuitee.Append("<soapenv:Body>");
                nuitee.Append("<nuit:performConfirmPropertyAvailability>");
                nuitee.Append("<confirmPropertyAvailabilityReq>");
                nuitee.Append("<sessionId>" + SessionID + "</sessionId>");
                nuitee.Append("<hotelCode>" + xmlhotelid + "</hotelCode>");
                nuitee.Append("<rateDetailCode>" + rateDetailCode1 + "</rateDetailCode>");
                nuitee.Append("<confirmedRooms>");
                for (int i = 0; i < NoOfRoom; i++)
                {
                    nuitee.Append("<confirmedRooms>");
                    nuitee.Append("<boardId>" + boardId + "</boardId>");
                    nuitee.Append("<roomCode>" + roomCode[i] + "</roomCode>");
                    nuitee.Append("</confirmedRooms>");
                }
                nuitee.Append("</confirmedRooms>");
                nuitee.Append("<timeout>" + 50000 + "</timeout>");
                nuitee.Append("</confirmPropertyAvailabilityReq>");
                nuitee.Append("</nuit:performConfirmPropertyAvailability>");
                nuitee.Append("</soapenv:Body>");
                nuitee.Append("</soapenv:Envelope>");
                string ReqString = nuitee.ToString();
                return ReqString;

            }
            catch (Exception ex)
            {
                _ = _exception.ErrorNew(ex,_LogAPI.Prebook);
            }
            return string.Empty;
        }

        public string Get_NuiteeBookingRequest(string Url, DataSet dsCartDetails, DataTable dtPassengers, DataRow[] nuiteeDetails, string UniqueNo, bool IsPriceUpdate, int i, int j, string PreBookingToken, string RoomTypeId, string RoomName, double Roomprice, string Currency, int NoOfRoom, string email, string EmailID, string Firstname, string LastName, string phoneno, string Paymenttype, string referenceNumber)
        {
            try
            {
                StringBuilder nuitee = new StringBuilder();

                string[] spitid = RoomTypeId.Split(new String[] { "$$$" }, StringSplitOptions.None);
                string HotelCode = spitid[0].ToString();
                string SessionID = spitid[1].ToString();
                string rateDetailCode1 = spitid[2];
                //string[] rateDetailCode = rateDetailCode1.ToString().Split(new String[] { "###" }, StringSplitOptions.None);
                string roomCode1 = spitid[3];
                string[] roomCode = roomCode1.ToString().Split(new String[] { "###" }, StringSplitOptions.None);

                string UserName = Convert.ToString(nuiteeDetails[0]["UserName"]);
                string PassWord = Convert.ToString(nuiteeDetails[0]["Password"]);
                string roomCode2 = "";

                string Title = "";
                string Name = "";
                string SurName = "";
                //bool islead = false;

                nuitee.Append("<soapenv:Envelope xmlns:soapenv=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:nuit=\"http://www.nuitee.ma\">");
                nuitee.Append("<soapenv:Header/>");
                nuitee.Append("<soapenv:Body>");
                nuitee.Append("<nuit:performConfirmBooking>");
                nuitee.Append("<confirmBookingReq>");
                nuitee.Append("<sessionId>" + SessionID + "</sessionId>");
                nuitee.Append("<hotelCode>" + HotelCode + "</hotelCode>");
                nuitee.Append("<login>");
                nuitee.Append("<language>fr_FR</language>");
                nuitee.Append("<password>" + PassWord + "</password>");
                nuitee.Append("<userName>" + UserName + "</userName>");
                nuitee.Append("</login>");
                nuitee.Append("<confirmPropertyCode>" + PreBookingToken + "</confirmPropertyCode>");
                //nuitee.Append("<clientIdentifier> testReserve  </clientIdentifier>");
                nuitee.Append("<clientIdentifier>" + referenceNumber + "</clientIdentifier>");
                nuitee.Append("<holder>");
                nuitee.Append("<email>" + EmailID + "</email>");
                if ((Convert.ToInt32(dtPassengers.Rows[0]["IsLead"]) == 1))
                {
                    nuitee.Append("<firstName>" + Convert.ToString(dtPassengers.Rows[0]["FirstName"]) + "</firstName>");
                    nuitee.Append("<lastName>" + Convert.ToString(dtPassengers.DefaultView[0]["LastName"]) + "</lastName>");
                    nuitee.Append("<titulation>" + Convert.ToString(dtPassengers.DefaultView[0]["Prefix"]) + " </titulation>");
                }
                nuitee.Append("</holder>");
                nuitee.Append("<reservationInfo>");
                for (int x = 0; x < NoOfRoom; x++)
                {
                    var RoomNumber = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[x]["RoomNumber"]);
                    dtPassengers.DefaultView.RowFilter = "PickUp='" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[0]["RoomTypeId"]) + "' and UserCartID=" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[0]["UserCartID"] + "and RoomNo = " + RoomNumber + "");
                    for (int p = 0; p < dtPassengers.DefaultView.Count; p++)
                    {
                        nuitee.Append("<customerRoomInfos>");
                        nuitee.Append("<customerComment>no comment</customerComment>");
                        nuitee.Append("<firstName>" + Convert.ToString(dtPassengers.DefaultView[p]["FirstName"]) + "</firstName>");
                        nuitee.Append("<lastName>" + Convert.ToString(dtPassengers.DefaultView[p]["LastName"]) + "</lastName>");
                        nuitee.Append("<roomCode>" + roomCode[x] + "</roomCode>");
                        nuitee.Append("<titulation>" + Convert.ToString(dtPassengers.DefaultView[p]["Prefix"]) + "</titulation>");
                        nuitee.Append("</customerRoomInfos>");
                    }
                }
                nuitee.Append("</reservationInfo>");
                nuitee.Append("<billingInfo>");
                nuitee.Append("<email>" + email + "</email>");
                nuitee.Append("<firstName>" + Firstname + "</firstName>");
                nuitee.Append("<lastName>" + LastName + "</lastName>");
                nuitee.Append("<titulation>" + Convert.ToString(dtPassengers.DefaultView[0]["Prefix"]) + "</titulation>");
                nuitee.Append("<paymentMethodType>LIMIT</paymentMethodType>");
                nuitee.Append("</billingInfo>");
                nuitee.Append("<customercomment>comment</customercomment>");
                nuitee.Append("<timeout>" + 50000 + "</timeout>");
                nuitee.Append("</confirmBookingReq>");
                nuitee.Append("</nuit:performConfirmBooking>");
                nuitee.Append("</soapenv:Body>");
                nuitee.Append("</soapenv:Envelope>");
                string ReqString = nuitee.ToString();
                return ReqString;
            }
            catch (Exception ex)
            {
                _ = _exception.ErrorBooking(ex, "");
            }
            return string.Empty;
        }

        public string Get_NuiteeBookingDetail(int Booking, DataRow[] nuiteeDetails)
        {
            string UserName = Convert.ToString(nuiteeDetails[0]["UserName"]);
            string PassWord = Convert.ToString(nuiteeDetails[0]["Password"]);

            StringBuilder nuitee = new StringBuilder();
            nuitee.Append("<soapenv:Envelope xmlns:soapenv=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:nuit=\"http://www.nuitee.ma\">");
            nuitee.Append("<soapenv:Header/>");
            nuitee.Append("<soapenv:Body>");
            nuitee.Append("<nuit:performGetBookingDetails>");
            nuitee.Append("<getBookingDetailsReq>");
            nuitee.Append("<login>");
            nuitee.Append("<language>fr_FR</language>");
            nuitee.Append("<password>" + PassWord + "</password>");
            nuitee.Append("<userName>" + UserName + "</userName>");
            nuitee.Append("</login>");
            nuitee.Append("<bookingId>" + Booking + "</bookingId>");
            nuitee.Append("<timeout>" + 10000 + "</timeout>");
            nuitee.Append("</getBookingDetailsReq>");
            nuitee.Append("</nuit:performGetBookingDetails>");
            nuitee.Append("</soapenv:Body>");
            nuitee.Append("</soapenv:Envelope>");
            string ReqString = nuitee.ToString();
            return ReqString;
        }

        public string Get_NuiteeBookingCancelRQ(string AgentVoucherNo, string RoomtypeId, DataRow[] nuiteeDetails, string cancelreason, string Ipaddress)
        {
            try
            {

                string UserName = Convert.ToString(nuiteeDetails[0]["UserName"]);
                string PassWord = Convert.ToString(nuiteeDetails[0]["Password"]);
                string[] arr1 = AgentVoucherNo.ToString().Split(new String[] { "$$" }, StringSplitOptions.None);
                int bookingId = Convert.ToInt32(arr1[0]);

                StringBuilder nuitee = new StringBuilder();
                nuitee.Append("<soapenv:Envelope xmlns:soapenv=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:nuit=\"http://www.nuitee.ma\">");
                nuitee.Append("<soapenv:Header/>");
                nuitee.Append("<soapenv:Body>");
                nuitee.Append("<nuit:performCancelBooking>");
                nuitee.Append("<cancelBookingReq>");
                nuitee.Append("<login>");
                nuitee.Append("<language>  france  </language>");
                nuitee.Append("<password>" + PassWord + "</password>");
                nuitee.Append("<userName>" + UserName + "</userName>");
                nuitee.Append("</login>");
                nuitee.Append("<bookingId>" + bookingId + "</bookingId>");
                nuitee.Append("<reason>  test  </reason>");
                nuitee.Append("<timeout>" + 50000 + "</timeout>");
                nuitee.Append("</cancelBookingReq>");
                nuitee.Append("</nuit:performCancelBooking>");
                nuitee.Append("</soapenv:Body>");
                nuitee.Append("</soapenv:Envelope>");
                string ReqString = nuitee.ToString();
                return ReqString;
            }
            catch (Exception ex)
            {
                _ =_exception.ErrorNew(ex, _LogAPI.CancelBooking);
            }
            return string.Empty;
        }
    }
}
