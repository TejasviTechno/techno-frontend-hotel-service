using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using XConnect.CommonUtilities;
using XConnect.CommonUtilities.Interface;
using XConnect.ErrorLogHandler.Interface;
using XConnect.Expedia;
using XConnect.Markup.Interface;
using XConnect.Models;
using XConnect.Models.Hotel;
using XConnect.Supplier.Darina.Interface;
using static Azure.Core.HttpHeader;
using static XConnect.Models.ErrorLog;
using static XConnect.Supplier.Booking.Com.detailres;
using Options = XConnect.Models.Options;

namespace XConnect.Supplier.Darina
{
    public class DarinaRepo : IDarina
    {
        private readonly ICommonMethod _commonMethod;
        private readonly IException _exception;
        private readonly IMarkup _markup;
        private readonly ISupplierRes _supplierRes;
        private readonly KeySettings _keySettings;
        private readonly IMemoryCache _cache;




        public DarinaRepo(ICommonMethod commonMethod, IMarkup markup, ISupplierRes supplierRes, IException exception, IOptions<KeySettings> keySettings, IMemoryCache cache)
        {
            _commonMethod = commonMethod;
            _markup = markup;
            _supplierRes = supplierRes;
            _exception = exception;
            _keySettings = keySettings.Value;
            _cache = cache;
        }
        public void GetDarinaRequestOnly_async(DataRow[] commonCode, List<StarRating> listRating, DataTable dtPreferredDataHotel, List<SupplierSearchConfig> lstDarinaSearchConfig, List<SupplierDetails> lstDarinaDetails, HotelSearchParameter SearchPara, bool IsHotelWiseSearch, ref ConcurrentBag<HotelX> AllTask, string AgentCode, int Hit, TokenData tokenData, DataTable dtROE, DataSet dtXMLCity)
        {
            try
            {
                if (lstDarinaDetails.Count > 0)
                {
                    List<StarRating> lstRating = new List<StarRating>();
                    lstRating = _commonMethod.GetStarRatingStringList_xtg(SearchPara);

                    List<HotelCodeByCity> DtHotelDAta = HtmlExtensions.ToList<HotelCodeByCity>(commonCode.CopyToDataTable());
                    int ratingcount = lstRating.Count;
                    int hotelcount = DtHotelDAta.Count;

                    foreach (SupplierDetails DarinaDetails in lstDarinaDetails)
                    {
                        try
                        {
                            int SupplierID = DarinaDetails.SupplierID;
                            string Supplier = DarinaDetails.SupplierName;

                            if (lstDarinaSearchConfig.Count > 0)
                            {
                                List<SupplierSearchConfig> DarinaSearchConfig = lstDarinaSearchConfig
                                        .Where(x => x != null && x.SupplierID == SupplierID)
                                        .ToList();

                                if (DarinaSearchConfig != null && DarinaSearchConfig.Count > 0)
                                {
                                    DataForSearch dataForSearch = _commonMethod.GetXmlDataForSearchTechwise(SearchPara.Nationality, dtXMLCity, Supplier, DarinaDetails.TechName);
                                    if (dataForSearch.XmlNationality != "")
                                    {
                                        string Ids = string.Empty;
                                        string xmlbase = "";
                                        _commonMethod.GetPrefferedHotelCodes(ref Ids, ref xmlbase, ref DarinaSearchConfig, Supplier, dtPreferredDataHotel, SearchPara.IsSearchFor);

                                        List<HotelCodeByCity> filter = new List<HotelCodeByCity>();
                                        filter = _commonMethod.FilterWithOutCity_Code(ratingcount, hotelcount, DtHotelDAta, lstRating, Supplier);

                                        if (filter.Count > 0)
                                        {
                                            #region "Darina"
                                            if (DarinaSearchConfig != null && SearchPara.Rooms.Count == 1)
                                            {
                                                string WebSiteURL = Convert.ToString(DarinaDetails.WebSiteURL);
                                                if (_keySettings.SupplierProxy == "0")//Added on 9-APR-2025
                                                {
                                                    //if (SearchPara.Filters.IsShowRooms == 1)
                                                    //    WebSiteURL = Convert.ToString(DarinaStaticInfo.Darina_BookUrl);
                                                    //else
                                                        WebSiteURL = Convert.ToString(DarinaDetails.WebSiteURL);
                                                }
                                                else
                                                {
                                                    //if (SearchPara.Filters.IsShowRooms == 1)
                                                    //    WebSiteURL = Convert.ToString(DarinaStaticInfo.Darina_BookUrl);
                                                    //else
                                                        WebSiteURL = Convert.ToString(DarinaDetails.WebSiteURLforAPI);//Added on 3-MAR-2025
                                                }
                                                string UserID = Convert.ToString(DarinaDetails.UserID);
                                                string UserName = Convert.ToString(DarinaDetails.UserName);
                                                string Password = Convert.ToString(DarinaDetails.Password);
                                                string SecurityKey = Convert.ToString(DarinaDetails.version);


                                                string DarinaCurrency = DarinaDetails.CurrencyShortName;
                                                double CurrencyFactorDarina = 1;
                                                if (DarinaCurrency.ToLower() != tokenData.SiteCurrency.ToLower())
                                                {
                                                    CurrencyFactorDarina = _commonMethod.GetROEByCurrency(DarinaCurrency, dtROE);
                                                }

                                                for (int k = 0; k < DarinaSearchConfig.Count; k++)
                                                {
                                                    //if (Convert.ToInt32(DarinaSearchConfig[k].Hit) != Hit)
                                                    //    continue;
                                                    int Noofrecords = Convert.ToInt32(DarinaSearchConfig[k].Noofrecords);
                                                    int HotelSearchTimeOut = Convert.ToInt32(DarinaSearchConfig[k].HotelSearchTimeOut);
                                                    //if (Hit > 0)
                                                    int ProcessTimeout = Convert.ToInt32(DarinaSearchConfig[k].HotelSearchTimeOut2);

                                                    bool DebugMode = (Convert.ToInt32(DarinaSearchConfig[k].DebugMode) == 0 ? false : true);

                                                    //if (AgentCode.ToLower() == "")
                                                    //{
                                                    //    tsDarina1 = GetDarinaDataset("0", "5", WebSiteURL, UserID, UserName, Password, Noofrecords, HotelSearchTimeOut, SearchPara, Ids, SecurityKey, DebugMode);
                                                    //    Array.Resize(ref AllTask, (AllTask != null ? AllTask.Length + 1 : 1));
                                                    //    AllTask[AllTask.Length - 1] = tsDarina1;
                                                    //}
                                                    //else
                                                    {

                                                        if (IsHotelWiseSearch)
                                                        {
                                                            //tsDarina1 = GetDarinaDataset("1", "5", WebSiteURL, UserID, UserName, Password, Noofrecords, HotelSearchTimeOut, SearchPara, Ids, SecurityKey, DebugMode);
                                                            //Array.Resize(ref AllTask, (AllTask != null ? AllTask.Length + 1 : 1));
                                                            //AllTask[AllTask.Length - 1] = tsDarina1;
                                                        }
                                                        else
                                                        {
                                                            //DataTable dtDarina = new DataTable();
                                                            //if (Ids == string.Empty)
                                                            //    dtDarina = Common.GetAgodaHotelCodeByCityID(SearchPara.CityID, false, "Darina");

                                                            if (Convert.ToInt32(DarinaSearchConfig[k].CityID) == 0 || Convert.ToInt32(DarinaSearchConfig[k].CityID) == SearchPara.CityID)//All City means 0
                                                            {
                                                                //DarinaSearchConfig[k].HotelSearchTimeOut = 10000;
                                                                //  List<HotelCodeByCity> DtHotelDAta = FilterFinalList(commonCode, listRating);
                                                                if (commonCode.Length > 0)

                                                                {
                                                                    {
                                                                        double count = Convert.ToDouble(filter.Count);
                                                                        int NoOfDiv = Convert.ToInt32(Math.Ceiling(count / Convert.ToDouble(Noofrecords)));
                                                                        List<List<XMLID_Search>> lstxml = _commonMethod.GetXMLIDString(NoOfDiv, Noofrecords, count, filter);

                                                                        for (int a = 0; a < NoOfDiv; a++)
                                                                        {
                                                                            string IdsForSearch = Convert.ToString(_commonMethod.GetHotelCodeByCOmma(lstxml[a]));

                                                                            HotelX xhotel = new HotelX();
                                                                            xhotel.Supplier = Supplier;
                                                                            xhotel.DebugeMode = DebugMode;
                                                                            xhotel.SearchTimeOut = HotelSearchTimeOut;
                                                                            xhotel.ProcessTimeout = ProcessTimeout;
                                                                            xhotel.URL = WebSiteURL;
                                                                            xhotel.UserId = UserID;
                                                                            xhotel.UserName = UserName;
                                                                            xhotel.PassWord = Password;
                                                                            xhotel.CurrencyCode = DarinaCurrency;
                                                                            xhotel.CurrencyFactor = CurrencyFactorDarina;
                                                                            xhotel.XmlNationality = dataForSearch.XmlNationality;
                                                                            xhotel.XmlCity = dataForSearch.xmlCity;
                                                                            xhotel.XmlCountry = dataForSearch.xmlCountry;


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
                                                                                objrq.Request = (XMLDarinaRequestOnly("0", "5", WebSiteURL, UserID, UserName, Password, Noofrecords, HotelSearchTimeOut, SearchPara, IdsForSearch, SecurityKey, DebugMode, dataForSearch.xmlCity, dataForSearch.xmlCountry, dataForSearch.XmlNationality));
                                                                                objrq.Response = ""; RQRS.Add(objrq);
                                                                            }
                                                                            xhotel.RQRS = RQRS;
                                                                            #endregion
                                                                            xhotel.TechName = DarinaDetails.TechName;
                                                                            xhotel.ReturnRooms = DarinaDetails.ReturnRooms;
                                                                            AllTask.Add(xhotel);
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

        public string XMLDarinaRequestOnly(string MinValue, string MaxValue, string DarinaUrl, string DarinaClientID, string DarinaUserName, string DarinaPassword, Int32 NoofRecords, Int32 SearchTimeOut, HotelSearchParameter SearchParam, string DarinaHotelIds, string SecurityKey, bool DebugeMode, string Darina_CityCode, string Darina_CountryCode, string Darina_NationalityCode)
        {
            try
            {
                string SearchLevel = "CheckAvailability";
                if (DarinaHotelIds != string.Empty)
                    SearchLevel = "CheckAvailability_ViaPropertiesIds";
                string xslRequest = GetDarinaSearchRequest(SearchLevel, SearchParam, Darina_CountryCode, Darina_CityCode, Darina_NationalityCode, MinValue, DarinaClientID, DarinaUserName, DarinaPassword, SecurityKey, DarinaHotelIds);

                return xslRequest;
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
                return "";
            }
        }

        public string GetDarinaSearchRequest_130525(string SearchLevel, HotelSearchParameter SearchParam, string Darina_CountryCode, string Darina_CityCode, string Darina_NationalityCode, string MinValue, string DarinaClientID, string DarinaUserName, string DarinaPassword, string SecurityKey, string Hotelids)
        {
            try
            {
                #region Old Code
                //         StringBuilder HotelIdstring = new StringBuilder();
                //         HotelIdstring.Append("<HotelsID_List>");
                //         if (Hotelids != string.Empty)
                //         {
                //             string[] HotesIdnew = Hotelids.Split(',');
                //             for (int hid = 0; hid < HotesIdnew.GetLength(0); hid++)
                //                 HotelIdstring.Append("<int>" + HotesIdnew[hid] + "</int>");
                //         }
                //         HotelIdstring.Append("</HotelsID_List>");


                //         string xslRequest = "<soap:Envelope xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns:soap=\"http://schemas.xmlsoap.org/soap/envelope/\">" +
                // "  <soap:Body>" +
                // "    <" + SearchLevel + " xmlns =\"http://travelcontrol.softexsw.us/\">" +
                // "      <SecStr>" + SecurityKey + "</SecStr>" +
                // "      <AccountName>" + XMLHotelBookingCommon.Darina_AccountName + "</AccountName>" +
                // "      <UserName>" + DarinaUserName + "</UserName>" +
                //  "     <Password>" + DarinaPassword + "</Password>" +
                // "      <AgentID>" + DarinaClientID + "</AgentID>" +
                // "      <FromDate>" + SearchParam.CheckInDate + "</FromDate>" +
                // "      <ToDate>" + SearchParam.CheckOutDate + "</ToDate>" +
                // "      <CountryID>" + Darina_CountryCode + "</CountryID>" +
                // "      <CityID>" + Darina_CityCode + "</CityID>" +
                // (MinValue == "" ? "" : " <MinLandCategory>" + MinValue + "</MinLandCategory> <MaxLandCategory>" + MinValue + "</MaxLandCategory>") +
                // "      <AreaID>0</AreaID>" +
                // "      <AreaName></AreaName>" +
                // "      <PropertyType>0</PropertyType>" +
                //          Convert.ToString(HotelIdstring) +
                // "      <HotelName></HotelName>" +
                // "      <OccupancyID>0</OccupancyID>" +
                // "      <AdultPax>" + SearchParam.Rooms[0].NoofAdults + "</AdultPax>" +
                // "      <ChildPax>" + SearchParam.Rooms[0].NoofChild + "</ChildPax>" +
                // "      <ChildrenAges>" +
                // (SearchParam.Rooms[0].NoofChild == 0 ? "" : // child 0
                // SearchParam.Rooms[0].NoofChild == 1 ? "<int>" + SearchParam.Rooms[0].Child1Age + "</int>" :                 // for 1 child
                // "<int>" + SearchParam.Rooms[0].Child1Age + "</int> <int>" + SearchParam.Rooms[0].Child2Age + "</int>") +  // for two child

                // "      </ChildrenAges>" +
                // "      <ExtraBedAdult>false</ExtraBedAdult>" +
                // "      <ExtraBedChild>false</ExtraBedChild>" +
                // "      <Nationality_CountryID>" + Darina_NationalityCode + "</Nationality_CountryID>" +
                // "      <CurrencyID>" + XMLHotelBookingCommon.Darina_Currency + "</CurrencyID>" +
                // "      <MaxOverallPrice>0</MaxOverallPrice>" +
                // "      <Availability>ShowAvailableOnly</Availability>" +//ShowAll or ShowAvailableOnly
                // "      <RoomType>0</RoomType>" +
                // "      <MealPlan>0</MealPlan>" +
                // "      <GetHotelImageLink>true</GetHotelImageLink>" +
                // "      <GetHotelMapLink>true</GetHotelMapLink>" +
                // // "      <Source>1</Source>" +
                // //"      <LimitRoomTypesInResult>1</LimitRoomTypesInResult>" +
                // "    </" + SearchLevel + ">" +
                // "  </soap:Body>" +
                // "</soap:Envelope>";
                //return xslRequest;
                #endregion

                StringBuilder ST_HotelSearch_rq = new StringBuilder();

                ST_HotelSearch_rq.Append("<soap:Envelope xmlns:xsi =\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns:soap=\"http://schemas.xmlsoap.org/soap/envelope/\">");
                ST_HotelSearch_rq.Append("<soap:Body>");
                ST_HotelSearch_rq.Append("<" + SearchLevel + " xmlns =\"http://travelcontrol.softexsw.us/\">");
                ST_HotelSearch_rq.Append("<SecStr>" + SecurityKey + "</SecStr>");
                ST_HotelSearch_rq.Append("<AccountName>" + DarinaStaticInfo.Darina_AccountName + "</AccountName>");
                ST_HotelSearch_rq.Append("<UserName>" + DarinaUserName + "</UserName>");
                ST_HotelSearch_rq.Append("<Password>" + DarinaPassword + "</Password>");
                ST_HotelSearch_rq.Append("<AgentID>" + DarinaClientID + "</AgentID>");
                ST_HotelSearch_rq.Append("<FromDate>" + SearchParam.CheckInDate + "</FromDate>");
                ST_HotelSearch_rq.Append("<ToDate>" + SearchParam.CheckOutDate + "</ToDate>");
                ST_HotelSearch_rq.Append("<CountryID>" + Darina_CountryCode + "</CountryID>");
                if (Hotelids == string.Empty)
                {
                    ST_HotelSearch_rq.Append("<CityID>" + Darina_CityCode + "</CityID>");
                }
                ST_HotelSearch_rq.Append((MinValue == "" ? "" : " <MinLandCategory> " + MinValue + " </MinLandCategory> <MaxLandCategory> " + MinValue + " </MaxLandCategory>"));
                ST_HotelSearch_rq.Append("<AreaID>0</AreaID>");
                ST_HotelSearch_rq.Append("<AreaName></AreaName>");
                if (MinValue == "0")
                    ST_HotelSearch_rq.Append("<PropertyType>7</PropertyType>");
                else if (MinValue == "")
                    ST_HotelSearch_rq.Append("<PropertyType>0</PropertyType>");
                else
                    ST_HotelSearch_rq.Append("<PropertyType>6</PropertyType>");
                ST_HotelSearch_rq.Append("<HotelName></HotelName>");
                ST_HotelSearch_rq.Append("<OccupancyID>0</OccupancyID>");
                ST_HotelSearch_rq.Append("<AdultPax>" + SearchParam.Rooms[0].NoofAdults + "</AdultPax>");
                ST_HotelSearch_rq.Append("<ChildPax>" + SearchParam.Rooms[0].NoofChild + "</ChildPax>");
                ST_HotelSearch_rq.Append("<ChildrenAges>");
                ST_HotelSearch_rq.Append((SearchParam.Rooms[0].NoofChild == 0 ? "" : SearchParam.Rooms[0].NoofChild == 1 ?
                 "<int>" + SearchParam.Rooms[0].Child1Age + "</int>" :
                 "<int>" + SearchParam.Rooms[0].Child1Age + "</int><int>" + SearchParam.Rooms[0].Child2Age + "</int>"));
                ST_HotelSearch_rq.Append("</ChildrenAges>");
                ST_HotelSearch_rq.Append("<ExtraBedAdult>false</ExtraBedAdult>");
                ST_HotelSearch_rq.Append("<ExtraBedChild>false</ExtraBedChild>");
                ST_HotelSearch_rq.Append("<Nationality_CountryID>" + Darina_NationalityCode + "</Nationality_CountryID>");
                ST_HotelSearch_rq.Append("<CurrencyID>" + DarinaStaticInfo.Darina_Currency + "</CurrencyID>");
                ST_HotelSearch_rq.Append("<MaxOverallPrice>0</MaxOverallPrice>");
                ST_HotelSearch_rq.Append("<Availability>ShowAvailableOnly</Availability>");
                ST_HotelSearch_rq.Append("<RoomType>0</RoomType>");
                ST_HotelSearch_rq.Append("<MealPlan>0</MealPlan>");
                ST_HotelSearch_rq.Append("<GetHotelImageLink>true</GetHotelImageLink>");
                ST_HotelSearch_rq.Append("<GetHotelMapLink>true</GetHotelMapLink>");
                if (SearchLevel == "CheckAvailabilityWithCancellation_NoCache_LiveCalculation")
                {
                    ST_HotelSearch_rq.Append("<HotelID>" + Hotelids + "</HotelID>");
                }
                else
                {
                    if (Hotelids != string.Empty)
                    {
                        ST_HotelSearch_rq.Append("<HotelsID_List>");

                        string[] hid = Hotelids.Split(',');
                        for (int i = 0; i < hid.Length; i++)
                        {
                            ST_HotelSearch_rq.Append("<int>" + hid[i] + "</int>");
                        }
                        ST_HotelSearch_rq.Append("</HotelsID_List>");
                    }
                }
                ST_HotelSearch_rq.Append("</" + SearchLevel + ">");
                ST_HotelSearch_rq.Append("</soap:Body>");
                ST_HotelSearch_rq.Append("</soap:Envelope>");

                return ST_HotelSearch_rq.ToString().Replace("\\", "");
            }
            catch (Exception ex)
            {
                return "";
            }
        }

        public string GetDarinaSearchRequest(string SearchLevel, HotelSearchParameter SearchParam, string Darina_CountryCode, string Darina_CityCode, string Darina_NationalityCode, string MinValue, string DarinaClientID, string DarinaUserName, string DarinaPassword, string SecurityKey, string Hotelids)
        {
            try
            {
                #region Old Code
                //         StringBuilder HotelIdstring = new StringBuilder();
                //         HotelIdstring.Append("<HotelsID_List>");
                //         if (Hotelids != string.Empty)
                //         {
                //             string[] HotesIdnew = Hotelids.Split(',');
                //             for (int hid = 0; hid < HotesIdnew.GetLength(0); hid++)
                //                 HotelIdstring.Append("<int>" + HotesIdnew[hid] + "</int>");
                //         }
                //         HotelIdstring.Append("</HotelsID_List>");


                //         string xslRequest = "<soap:Envelope xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns:soap=\"http://schemas.xmlsoap.org/soap/envelope/\">" +
                // "  <soap:Body>" +
                // "    <" + SearchLevel + " xmlns =\"http://travelcontrol.softexsw.us/\">" +
                // "      <SecStr>" + SecurityKey + "</SecStr>" +
                // "      <AccountName>" + XMLHotelBookingCommon.Darina_AccountName + "</AccountName>" +
                // "      <UserName>" + DarinaUserName + "</UserName>" +
                //  "     <Password>" + DarinaPassword + "</Password>" +
                // "      <AgentID>" + DarinaClientID + "</AgentID>" +
                // "      <FromDate>" + SearchParam.CheckInDate + "</FromDate>" +
                // "      <ToDate>" + SearchParam.CheckOutDate + "</ToDate>" +
                // "      <CountryID>" + Darina_CountryCode + "</CountryID>" +
                // "      <CityID>" + Darina_CityCode + "</CityID>" +
                // (MinValue == "" ? "" : " <MinLandCategory>" + MinValue + "</MinLandCategory> <MaxLandCategory>" + MinValue + "</MaxLandCategory>") +
                // "      <AreaID>0</AreaID>" +
                // "      <AreaName></AreaName>" +
                // "      <PropertyType>0</PropertyType>" +
                //          Convert.ToString(HotelIdstring) +
                // "      <HotelName></HotelName>" +
                // "      <OccupancyID>0</OccupancyID>" +
                // "      <AdultPax>" + SearchParam.Rooms[0].NoofAdults + "</AdultPax>" +
                // "      <ChildPax>" + SearchParam.Rooms[0].NoofChild + "</ChildPax>" +
                // "      <ChildrenAges>" +
                // (SearchParam.Rooms[0].NoofChild == 0 ? "" : // child 0
                // SearchParam.Rooms[0].NoofChild == 1 ? "<int>" + SearchParam.Rooms[0].Child1Age + "</int>" :                 // for 1 child
                // "<int>" + SearchParam.Rooms[0].Child1Age + "</int> <int>" + SearchParam.Rooms[0].Child2Age + "</int>") +  // for two child

                // "      </ChildrenAges>" +
                // "      <ExtraBedAdult>false</ExtraBedAdult>" +
                // "      <ExtraBedChild>false</ExtraBedChild>" +
                // "      <Nationality_CountryID>" + Darina_NationalityCode + "</Nationality_CountryID>" +
                // "      <CurrencyID>" + XMLHotelBookingCommon.Darina_Currency + "</CurrencyID>" +
                // "      <MaxOverallPrice>0</MaxOverallPrice>" +
                // "      <Availability>ShowAvailableOnly</Availability>" +//ShowAll or ShowAvailableOnly
                // "      <RoomType>0</RoomType>" +
                // "      <MealPlan>0</MealPlan>" +
                // "      <GetHotelImageLink>true</GetHotelImageLink>" +
                // "      <GetHotelMapLink>true</GetHotelMapLink>" +
                // // "      <Source>1</Source>" +
                // //"      <LimitRoomTypesInResult>1</LimitRoomTypesInResult>" +
                // "    </" + SearchLevel + ">" +
                // "  </soap:Body>" +
                // "</soap:Envelope>";
                //return xslRequest;
                #endregion

                StringBuilder ST_HotelSearch_rq = new StringBuilder();

                //if (SearchParam.Filters.IsShowRooms == 1) //new added
                //{
                //    SearchLevel = "CheckAvailabilityWithCancellation_NoCache_LiveCalculation";
                //}

                ST_HotelSearch_rq.Append("<soap:Envelope xmlns:xsi =\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns:soap=\"http://schemas.xmlsoap.org/soap/envelope/\">");
                ST_HotelSearch_rq.Append("<soap:Body>");
                ST_HotelSearch_rq.Append("<" + SearchLevel + " xmlns =\"http://travelcontrol.softexsw.us/\">");
                ST_HotelSearch_rq.Append("<SecStr>" + SecurityKey + "</SecStr>");
                ST_HotelSearch_rq.Append("<AccountName>" + DarinaStaticInfo.Darina_AccountName + "</AccountName>");
                ST_HotelSearch_rq.Append("<UserName>" + DarinaUserName + "</UserName>");
                ST_HotelSearch_rq.Append("<Password>" + DarinaPassword + "</Password>");
                ST_HotelSearch_rq.Append("<AgentID>" + DarinaClientID + "</AgentID>");
                ST_HotelSearch_rq.Append("<FromDate>" + SearchParam.CheckInDate + "</FromDate>");
                ST_HotelSearch_rq.Append("<ToDate>" + SearchParam.CheckOutDate + "</ToDate>");
                ST_HotelSearch_rq.Append("<CountryID>" + Darina_CountryCode + "</CountryID>");
                if (Hotelids == string.Empty)
                {
                    ST_HotelSearch_rq.Append("<CityID>" + Darina_CityCode + "</CityID>");
                }
                ST_HotelSearch_rq.Append((MinValue == "" ? "" : " <MinLandCategory> " + MinValue + " </MinLandCategory> <MaxLandCategory> " + MinValue + " </MaxLandCategory>"));
                ST_HotelSearch_rq.Append("<AreaID>0</AreaID>");
                ST_HotelSearch_rq.Append("<AreaName></AreaName>");
                if (MinValue == "0")
                    ST_HotelSearch_rq.Append("<PropertyType>7</PropertyType>");
                else if (MinValue == "")
                    ST_HotelSearch_rq.Append("<PropertyType>0</PropertyType>");
                else
                    ST_HotelSearch_rq.Append("<PropertyType>6</PropertyType>");
                ST_HotelSearch_rq.Append("<HotelName></HotelName>");
                ST_HotelSearch_rq.Append("<OccupancyID>0</OccupancyID>");
                ST_HotelSearch_rq.Append("<AdultPax>" + SearchParam.Rooms[0].NoofAdults + "</AdultPax>");
                ST_HotelSearch_rq.Append("<ChildPax>" + SearchParam.Rooms[0].NoofChild + "</ChildPax>");
                ST_HotelSearch_rq.Append("<ChildrenAges>");
                ST_HotelSearch_rq.Append((SearchParam.Rooms[0].NoofChild == 0 ? "" : SearchParam.Rooms[0].NoofChild == 1 ?
                 "<int>" + SearchParam.Rooms[0].Child1Age + "</int>" :
                 "<int>" + SearchParam.Rooms[0].Child1Age + "</int><int>" + SearchParam.Rooms[0].Child2Age + "</int>"));
                ST_HotelSearch_rq.Append("</ChildrenAges>");
                ST_HotelSearch_rq.Append("<ExtraBedAdult>false</ExtraBedAdult>");
                ST_HotelSearch_rq.Append("<ExtraBedChild>false</ExtraBedChild>");
                ST_HotelSearch_rq.Append("<Nationality_CountryID>" + Darina_NationalityCode + "</Nationality_CountryID>");
                ST_HotelSearch_rq.Append("<CurrencyID>" + DarinaStaticInfo.Darina_Currency + "</CurrencyID>");
                ST_HotelSearch_rq.Append("<MaxOverallPrice>0</MaxOverallPrice>");
                ST_HotelSearch_rq.Append("<Availability>ShowAvailableOnly</Availability>");
                ST_HotelSearch_rq.Append("<RoomType>0</RoomType>");
                ST_HotelSearch_rq.Append("<MealPlan>0</MealPlan>");
                ST_HotelSearch_rq.Append("<GetHotelImageLink>true</GetHotelImageLink>");
                ST_HotelSearch_rq.Append("<GetHotelMapLink>true</GetHotelMapLink>");
                if (SearchLevel == "CheckAvailabilityWithCancellation_NoCache_LiveCalculation")
                {
                    ST_HotelSearch_rq.Append("<HotelID>" + Hotelids + "</HotelID>");
                }
                else
                {
                    if (Hotelids != string.Empty)
                    {
                        ST_HotelSearch_rq.Append("<HotelsID_List>");

                        string[] hid = Hotelids.Split(',');
                        for (int i = 0; i < hid.Length; i++)
                        {
                            ST_HotelSearch_rq.Append("<int>" + hid[i] + "</int>");
                        }
                        ST_HotelSearch_rq.Append("</HotelsID_List>");
                    }
                }
                ST_HotelSearch_rq.Append("</" + SearchLevel + ">");
                ST_HotelSearch_rq.Append("</soap:Body>");
                ST_HotelSearch_rq.Append("</soap:Envelope>");

                return ST_HotelSearch_rq.ToString().Replace("\\", "");
            }
            catch (Exception ex)
            {
                return "";
            }
        }

        public async Task<List<Options>> Darina_HotelSearchWithRooms(HotelX Item, CommonSearch commonSearch, string ResponseData, HotelSearchParameter SearchParam, string currency, double currencyfactor, string xmlcity, string xmlcountry, string xmlnationality, string ProccessCutOffTime, Int32 processTimeout, string ResProccessTimerLogActive, string ResProccessTimeValueforSaveLog)
        {
            System.Diagnostics.Stopwatch timer_room_Darina = new System.Diagnostics.Stopwatch();
            timer_room_Darina.Start();
            List<Options> options = new List<Options>();
            DataTable DtHotel = _commonMethod.GenerateDataTableNewV2();
            try
            {
                string Darina_CityCode = xmlcity;


                if (ResponseData != null)
                {
                    if (ResponseData == string.Empty || ResponseData.Contains("ErrorTxt"))
                    {
                        Item.ResponseType = "NoCount";
                        return options;
                    }
                    DataSet DarinaResponse = _commonMethod.GetDataSetFromXMLString(ResponseData);

                    if (DarinaResponse == null || DarinaResponse.Tables["hotels"] == null)
                    {
                        Item.ResponseType = "NoCount";
                        return options;
                    }
                    DataSet dsMealPlan = null;

                    if (!_cache.TryGetValue("DarinaMealPlan", out dsMealPlan))
                    {
                        dsMealPlan = _commonMethod.ReadXmlFile("Hotel/Darina_MealPlan.xml");

                        _cache.Set("DarinaMealPlan", dsMealPlan);
                    }


                    DataSet dsOccupancy = null;

                    if (!_cache.TryGetValue("DarinaOccupancy", out dsOccupancy))
                    {
                        dsOccupancy = _commonMethod.ReadXmlFile("Hotel/Darina_Occupancy.xml");

                        _cache.Set("DarinaOccupancy", dsOccupancy);
                    }

                    for (int h = 0; h <= DarinaResponse.Tables["hotels"].Rows.Count - 1; h++)
                    {
                        Item.ResponseType = "Success";
                        //Check Time If High then return NoCount
                        if (ProccessCutOffTime == "1" && timer_room_Darina.ElapsedMilliseconds > processTimeout)
                        {
                            //timer_room_Darina.Stop();
                            break;
                        }
                        //--end
                        //string ItemCode = Convert.ToString(DarinaResponse.Tables["hotels"].Rows[h]["hotelid"]);
                        // string HotelName = Convert.ToString(DarinaResponse.Tables["hotels"].Rows[h]["Name"]);
                        double RatePerNight = 1;

                        #region "When not Need Room"  
                        // add hotel code from price only and room code blank here
                        if (SearchParam.Filters.IsShowRooms == 0)
                        {
                            #region "Hotel Code Block "
                            DataRow drXmlHotel = DtHotel.NewRow();
                            // string InternalHotelId = Convert.ToString(ItemCode);
                            drXmlHotel["XMLHotelCode"] = DarinaResponse.Tables["hotels"].Rows[h]["hotelid"];
                            drXmlHotel["HotelName"] = "";
                            drXmlHotel["SupplierName"] = "Darina";
                            drXmlHotel["OfficeID"] = 0;
                            drXmlHotel["ContractID"] = 0;
                            drXmlHotel["Rating"] = 0;
                            drXmlHotel["Reviews"] = 0;
                            drXmlHotel["UniqueNo"] = DarinaResponse.Tables["hotels"].Rows[h]["hotelid"];
                            drXmlHotel["IsShowSupplier"] = false;
                            drXmlHotel["BookingStatus"] = "Available";
                            drXmlHotel["XMLCity"] = Darina_CityCode;
                            drXmlHotel["SupplierCurrency"] = currency;

                            if (commonSearch.tokenData.AgentId != 1899)
                            {
                                DataRow[] drLowest = DarinaResponse.Tables["AccRates"].Select("Hotel='" + DarinaResponse.Tables["hotels"].Rows[h]["hotelid"] + "'");
                                for (int low = 0; low < drLowest.Length; low++)
                                {
                                    if (low == 0)
                                    {
                                        RatePerNight = Convert.ToDouble(drLowest[0]["RatePerStay"]);
                                        drXmlHotel["FinalSellingPrice"] = RatePerNight;
                                        drXmlHotel["SellingPrice"] = RatePerNight;
                                    }
                                    //string Offer = Convert.ToString(drLowest[0]["Offers"]);
                                    //if (Offer != string.Empty)
                                    //{
                                    //    drXmlHotel["OfferText"] = Offer;
                                    //}
                                }
                            }
                            else
                            {
                                drXmlHotel["FinalSellingPrice"] = 1;
                                drXmlHotel["SellingPrice"] = 1;
                            }

                            #endregion


                            if (RatePerNight > 0)
                            {
                                #region "Room code Block"
                                bool willAddOrNot = true;
                                Int64 RaynaID = 0;
                                Int64 GiataId = 0;
                                int Rating = 0;
                                Int64 OwnsystemHotelId = 0;
                                double MinPrice = RatePerNight;
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

                            string Provider = Item.Supplier;

                            #region "Hotel Block here"
                            DataRow drXmlHotel = DtHotel.NewRow();
                            // string InternalHotelId = Convert.ToString(ItemCode);
                            drXmlHotel["XMLHotelCode"] = DarinaResponse.Tables["hotels"].Rows[h]["hotelid"];
                            drXmlHotel["HotelName"] = "";
                            drXmlHotel["SupplierName"] = Provider;
                            drXmlHotel["OfficeID"] = 0;
                            drXmlHotel["ContractID"] = 0;
                            drXmlHotel["Rating"] = 0;
                            drXmlHotel["Reviews"] = 0;
                            drXmlHotel["UniqueNo"] = DarinaResponse.Tables["hotels"].Rows[h]["hotelid"];
                            drXmlHotel["IsShowSupplier"] = false;
                            drXmlHotel["BookingStatus"] = "Available";
                            drXmlHotel["XMLCity"] = Darina_CityCode;
                            drXmlHotel["SupplierCurrency"] = currency;
                            drXmlHotel["FinalSellingPrice"] = RatePerNight;
                            #endregion
                            #region "GetRooms for this hotel"
                            bool willAddOrNot = true;
                            Int64 RaynaID = 0;
                            Int64 GiataId = 0;
                            int RatingHotel = 0;
                            Int64 OwnsystemHotelId = 0;

                            double MinPrice = RatePerNight;
                            _markup.GetHotelDataCommon(ref MinPrice, ref willAddOrNot, ref OwnsystemHotelId, ref RaynaID, ref GiataId, ref RatingHotel, ref drXmlHotel, commonSearch.AgentId, commonSearch.TariffID, commonSearch.dsAgentDetails, commonSearch.supplierGiataMaster, SearchParam, commonSearch.dtCountryCityMarkup, commonSearch.B2CMarkup, commonSearch.IsParentId, commonSearch.SubAgentMarkup, commonSearch.SearchByHotel, commonSearch.HotelIDS);
                            if (willAddOrNot)
                            {
                                DataTable dtHotelRoom = _commonMethod.GenerateRoomtypeDataTableNewV2();
                                DataTable dtCancellation = _commonMethod.GenerateCancellationDataTableNewV2();
                                DataTable DtRoomRateBreackUp = _commonMethod.GenerateoomRateBreackUpDataTableNewV2();
                                int CutOffDays = XMLHotelBookingCommon.CutOffDays;
                                CutOffDays = _commonMethod.GetCutOffDays(commonSearch.dtSuplierMarkup, Provider);
                                #region "Room Logic"
                                if (SearchParam.Filters.IsShowRooms == 1)
                                {
                                    double AgentPercentage = 0.0;
                                    Boolean blnIsShowSupplier = false;
                                    double AgentMarkupAmount = 0.0;
                                    Boolean isPerce = true;

                                    double AgentCompanyPercentage = Convert.ToDouble(SearchParam.AgentCompanyPercentage);
                                    bool IsB2C = Convert.ToBoolean(Convert.ToByte(SearchParam.IsB2BOrB2C));

                                    //_markup.getAgetMarkupSetting(Provider, GiataId.ToString(), commonSearch.dtSuplierMarkup, commonSearch.dtMarkupDetails, ref AgentPercentage, ref AgentMarkupAmount, ref blnIsShowSupplier, ref isPerce, commonSearch.AgentId, RatingHotel, SearchParam.CityID, SearchParam.CountryID.ToString(), SearchParam.CheckInDate);

                                    _markup.getAgetMarkupSettingNew_new(Provider, Convert.ToString(RaynaID), commonSearch.dtSuplierMarkup, commonSearch.dsMarkupDetails, ref AgentPercentage, ref AgentMarkupAmount, ref blnIsShowSupplier, ref isPerce, commonSearch.AgentId, RatingHotel, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), SearchParam.CheckInDate);
                                    int maxOcupancy = 0;
                                    for (int i = 0; i < SearchParam.Rooms.Count; i++)
                                        maxOcupancy = maxOcupancy + SearchParam.Rooms[i].NoofChild + SearchParam.Rooms[i].NoofAdults;
                                    #region Supplier Room Code block
                                    DataRow[] drdtRooms = DarinaResponse.Tables["AccRates"].Select("Hotel='" + DarinaResponse.Tables["hotels"].Rows[h]["hotelid"] + "'");

                                    DataSet ExtraResponse = new DataSet();
                                    ExtraResponse = GetDarinaAdditionDataCacheOnly(Convert.ToString(DarinaResponse.Tables["hotels"].Rows[h]["hotelid"]));

                                    string remark = "";
                                    if (ExtraResponse != null && ExtraResponse.Tables.Count > 0 && ExtraResponse.Tables["d"] != null) //added not null cnd on 13055
                                    {
                                        DataRow[] dataRow = ExtraResponse.Tables["d"].Select("fieldname='REMARKS'");
                                        for (int i = 0; i < dataRow.Length; i++)
                                        {
                                            remark += Convert.ToString(dataRow[0]["Value"]);
                                        }

                                    }
                                    for (int DtRooms = 0; DtRooms <= drdtRooms.GetLength(0) - 1; DtRooms++)  // This block for Room wise 
                                    {
                                        //int Acc_SerialId = 0; //130525
                                        int Acc_SerialId = Convert.ToInt32(drdtRooms[DtRooms]["Serial"]);//130525

                                        // string RoomType = Convert.ToString(drdtRooms[DtRooms]["RoomType"]);
                                        DataRow[] DtRoomType = DarinaResponse.Tables["RoomTypes"].Select("RoomTypeId='" + drdtRooms[DtRooms]["RoomType"] + "'");
                                        string RoomTypeName = Convert.ToString(DtRoomType[0]["RoomTypeName"]);

                                        string MealPlan = Convert.ToString(drdtRooms[DtRooms]["MealPlan"]);
                                        string MealText = GetDarinaStaticDataCache(dsMealPlan, "MealPlan", "DarinaId", MealPlan);
                                        if (MealText == string.Empty)
                                            MealText = "RO";
                                        string Offers = "";
                                        if (DarinaResponse.Tables["AccRates"].Columns.Contains("Offers"))
                                        {
                                            Offers = Convert.ToString(drdtRooms[DtRooms]["Offers"]);

                                        }
                                        string Offertext = Offers;
                                        string RequestId = Convert.ToString(drdtRooms[DtRooms]["RequestId"]);
                                        string Availability = Convert.ToString(drdtRooms[DtRooms]["Availability"]);
                                        string HasMealUpgrade = Convert.ToString(drdtRooms[DtRooms]["HasMealUpgrade"]);

                                        string Occupancy = Convert.ToString(drdtRooms[DtRooms]["Occupancy"]);
                                        string OccupancyText = GetDarinaStaticDataCache(dsOccupancy, "Occupancy", "DarinaId", Occupancy);

                                        RatePerNight = Convert.ToDouble(drdtRooms[DtRooms]["RatePerNight"]);
                                        double FirstNightCharge = 0;
                                        Double RatePerStay = Convert.ToDouble(drdtRooms[DtRooms]["RatePerStay"]);

                                        if (DarinaStaticInfo.Darina_Currency == "7")
                                        { // if currency is in USD
                                            RatePerStay = RatePerStay * DarinaStaticInfo.Darina_CurrencyFactor;
                                            RatePerNight = RatePerNight * DarinaStaticInfo.Darina_CurrencyFactor;
                                        }
                                        RatePerStay = RatePerStay + 1;
                                        RatePerNight = RatePerNight + 1;
                                        string uniqid = Guid.NewGuid().ToString();
                                        string FinalRoomType = RequestId + ":" + Acc_SerialId + ":" + xmlnationality + ":" + Guid.NewGuid().ToString().Substring(0, 5) + ":" + RatePerStay;
                                        try
                                        {
                                            #region "new logic for rate breack"
                                            string WeekColumn = "";
                                            int noofnight = Convert.ToInt32(SearchParam.NoofNights);
                                            //DataRow[] drAnalysis = DarinaResponse.Tables["Analysis"].Select("Acc_Serial='" + Acc_SerialId + "'");
                                            for (int dtdate = 0; dtdate < noofnight; dtdate++)
                                            {
                                                //if (DarinaResponse.Tables["Analysis"].Columns.Contains("Code"))
                                                //{
                                                //    Offertext = Convert.ToString(drAnalysis[dtdate]["Code"]);
                                                //}
                                                //DateTime dt = Convert.ToDateTime(SearchParam.CheckInDate);
                                                string dtcc = SearchParam.CheckInDate.Substring(3, 2) + "/" + SearchParam.CheckInDate.Substring(0, 2) + "/" + SearchParam.CheckInDate.Substring(6, 4);
                                                DateTime dt = Convert.ToDateTime(dtcc);
                                                string datetime = Convert.ToString(dt.Date.AddDays(dtdate));
                                                WeekColumn = Convert.ToString(dt.Date.AddDays(dtdate).DayOfWeek).Substring(0, 3);

                                                double DateGross = 0;
                                                DateGross = RatePerNight;
                                                if (dtdate == 0)
                                                {
                                                    FirstNightCharge = DateGross;
                                                }
                                                if (DarinaStaticInfo.Darina_Currency == "7") // if currency is in USD
                                                    DateGross = DateGross * DarinaStaticInfo.Darina_CurrencyFactor;
                                                #region Vat
                                                double VatOnRateBrackup = 0;
                                                DateGross = _markup.ProcessHotelRateBreakupPrice_WithMarkup(1, SearchParam.CityID, SearchParam.CountryID.ToString(), DateGross, commonSearch.B2CMarkup, commonSearch.IsParentId, isPerce, AgentPercentage, AgentMarkupAmount, commonSearch.SubAgentMarkup, ref VatOnRateBrackup);
                                                #endregion

                                                #region Old Code

                                                //double b2cDateGrossMarkup = Math.Round((Math.Round((DateGross * B2CMarkup), 2) / 100), 2);
                                                //DateGross = Math.Round(DateGross + b2cDateGrossMarkup, 2);
                                                //if (1 == 1)
                                                //{
                                                //	if (IsParentId == 0)
                                                //	{
                                                //		double b2bSubDateGrossMarkup = Math.Round((Math.Round((DateGross * SubAgentMarkup), 2) / 100), 2);
                                                //		DateGross = Math.Round(DateGross + b2bSubDateGrossMarkup, 2);
                                                //	}
                                                //	if (isPerce)
                                                //	{
                                                //		double b2bDateGrossMarkup = Math.Round((Math.Round((DateGross * AgentPercentage), 2) / 100), 2);
                                                //		DateGross = Math.Round(DateGross + b2bDateGrossMarkup, 2);
                                                //	}
                                                //	else
                                                //		DateGross = (DateGross == 0 ? DateGross : Math.Round(DateGross + AgentMarkupAmount, 2));
                                                //}

                                                //DataRow drDarinaHotelRoomRateBreackUp = DtDarinaHotelRoomRateBreackUp.NewRow();
                                                //drDarinaHotelRoomRateBreackUp["RoomNo"] = Convert.ToString("1");
                                                //drDarinaHotelRoomRateBreackUp["SupplierName"] = "Darina";
                                                //drDarinaHotelRoomRateBreackUp["OwnsystemHotelId"] = OwnsystemHotelId;
                                                //drDarinaHotelRoomRateBreackUp["ContractID"] = 0;
                                                //drDarinaHotelRoomRateBreackUp["RoomTypeName"] = RoomTypeName;
                                                //drDarinaHotelRoomRateBreackUp["RoomTypeId"] = FinalRoomType;
                                                //drDarinaHotelRoomRateBreackUp["MealId"] = MealText;
                                                //drDarinaHotelRoomRateBreackUp["Meal"] = MealText;
                                                //drDarinaHotelRoomRateBreackUp["BookingStatus"] = Availability == "1" ? "Available" : "OnRequest";
                                                //drDarinaHotelRoomRateBreackUp["DateOfWeek"] = WeekColumn;
                                                //drDarinaHotelRoomRateBreackUp["Date"] = datetime;
                                                //drDarinaHotelRoomRateBreackUp["Rate"] = DateGross;
                                                //drDarinaHotelRoomRateBreackUp["VatdiffAmount"] = VatOnRateBrackup;
                                                //drDarinaHotelRoomRateBreackUp["RateType"] = "A Rate";
                                                //drDarinaHotelRoomRateBreackUp["OrderNo"] = 1;
                                                //drDarinaHotelRoomRateBreackUp["RoomSequenceNo"] = uniqid;
                                                //drDarinaHotelRoomRateBreackUp["BoardTypeID"] = "";
                                                //drDarinaHotelRoomRateBreackUp["SupplierCurrency"] = currency;
                                                //drDarinaHotelRoomRateBreackUp["BuyingPrice"] = DateGross / currencyfactor;
                                                //DtDarinaHotelRoomRateBreackUp.Rows.Add(drDarinaHotelRoomRateBreackUp);
                                                //DtDarinaHotelRoomRateBreackUp.AcceptChanges();
                                                #endregion

                                            }
                                            #endregion
                                        }
                                        catch (Exception ex)
                                        {
                                            await _exception.Error(ex);
                                        }


                                        #region "Room Details"
                                        DataRow drDarinaHotelRoom = dtHotelRoom.NewRow();
                                        if (Offertext != string.Empty && Offers == string.Empty)
                                        {
                                            Offers = Offertext;
                                        }
                                        if (Offers != string.Empty && Offertext == string.Empty)
                                        {
                                            Offertext = Offers;

                                        }

                                        drDarinaHotelRoom["OwnsystemHotelId"] = OwnsystemHotelId;
                                        drDarinaHotelRoom["ContractID"] = 0;
                                        drDarinaHotelRoom["UniqueNo"] = OwnsystemHotelId;
                                        drDarinaHotelRoom["RoomSequenceNo"] = uniqid;
                                        drDarinaHotelRoom["MealName"] = MealText;
                                        drDarinaHotelRoom["allocationDetails"] = "";
                                        drDarinaHotelRoom["minStay"] = 0;
                                        drDarinaHotelRoom["DateApplyMinStay"] = "";
                                        drDarinaHotelRoom["MealId"] = MealText;
                                        drDarinaHotelRoom["RoomNo"] = Convert.ToString("1");
                                        drDarinaHotelRoom["EssentialInformation"] = remark;
                                        drDarinaHotelRoom["RoomTypeName"] = RoomTypeName;
                                        drDarinaHotelRoom["RoomTypeId"] = FinalRoomType;
                                        drDarinaHotelRoom["BookingStatus"] = Availability == "1" ? "Available" : "OnRequest";
                                        drDarinaHotelRoom["AdultsOccupancy"] = SearchParam.Rooms[0].NoofAdults;
                                        drDarinaHotelRoom["ChildOccupancy"] = SearchParam.Rooms[0].NoofChild;
                                        drDarinaHotelRoom["MaxExtrabed"] = 0;
                                        drDarinaHotelRoom["BoardTypeID"] = "";
                                        drDarinaHotelRoom["CompanyBuyingPrice"] = RatePerStay;
                                        drDarinaHotelRoom["OfferDescription"] = Offertext;
                                        drDarinaHotelRoom["offerCode"] = Offers;

                                        double AgentBuyingPrice = RatePerStay;

                                        #region Vat
                                        Double VatOnTotal = 0;
                                        _markup.ProcessHotelRoom_WithMarkup(currencyfactor, currency, true, true, SearchParam.NoofNights, SearchParam.CityID, SearchParam.CountryID.ToString(), SearchParam.CheckInDate, SearchParam.CheckOutDate, ref RatePerStay, ref drDarinaHotelRoom, isPerce, AgentPercentage, AgentMarkupAmount, commonSearch.SubAgentMarkup, commonSearch.B2CMarkup, commonSearch.IsParentId);
                                        VatOnTotal = Convert.ToDouble(drDarinaHotelRoom["VatdiffAmount"]);
                                        #endregion

                                        drDarinaHotelRoom["SupplierName"] = Provider;
                                        dtHotelRoom.Rows.Add(drDarinaHotelRoom);
                                        dtHotelRoom.AcceptChanges();
                                        #endregion

                                        #region Cancellation Policy New Added not used
                                        if (false)
                                        {
                                            DataRow[] DrCancellation = DarinaResponse.Tables["Cancellation"].Select("Acc_Serial='" + Acc_SerialId + "'");
                                            DataTable dts = DrCancellation.CopyToDataTable();
                                            dts = dts.Rows.Cast<DataRow>().OrderBy(row => Convert.ToDecimal(row["Amount"])).CopyToDataTable();
                                            dts.Columns.Add("DateFilter", typeof(DateTime));

                                            for (int it = 0; it < dts.Rows.Count; it++)
                                            {
                                                string dtfrom = Convert.ToString(dts.Rows[it]["FromDate"]);
                                                dtfrom = dtfrom.Substring(3, 2) + "-" + dtfrom.Substring(0, 2) + "-" + dtfrom.Substring(6, 4);
                                                dts.Rows[it]["DateFilter"] = Convert.ToDateTime(Convert.ToString(dtfrom));
                                            }

                                            dts.DefaultView.Sort = "DateFilter asc";
                                            DataTable DTdrRoomcancellationRule = dts.Clone();
                                            for (int cr = 0; cr < dts.DefaultView.Count; cr++)
                                            {
                                                if (Convert.ToInt32(dts.DefaultView[cr]["Acc_serial"]) == Acc_SerialId)
                                                {
                                                    DTdrRoomcancellationRule.ImportRow(dts.DefaultView[cr].Row);
                                                }
                                            }
                                            int intRoomCancellation = 0;

                                            for (int c = 0; c < DTdrRoomcancellationRule.Rows.Count; c++)
                                            {
                                                string dtfrom = Convert.ToString(DTdrRoomcancellationRule.Rows[c]["FromDate"]);
                                                dtfrom = dtfrom.Substring(3, 2) + "/" + dtfrom.Substring(0, 2) + "/" + dtfrom.Substring(6, 4);
                                                DateTime FromDate = Convert.ToDateTime((Convert.ToString(dtfrom).Replace("/", "-")));
                                                string dtTO = Convert.ToString(DTdrRoomcancellationRule.Rows[c]["ToDate"]);
                                                dtTO = dtTO.Substring(3, 2) + "/" + dtTO.Substring(0, 2) + "/" + dtTO.Substring(6, 4);
                                                DateTime ToDate = Convert.ToDateTime((Convert.ToString(dtTO).Replace("/", "-")));

                                                double ChargeAmount = Convert.ToDouble(DTdrRoomcancellationRule.Rows[c]["Amount"]);
                                                string AmoutnType = Convert.ToString(DTdrRoomcancellationRule.Rows[c]["AmountType"]).Replace(",", "");

                                                string essen = Convert.ToString(DTdrRoomcancellationRule.Rows[c]["Description"]);

                                                int NoofNights = SearchParam.NoofNights;

                                                if (AmoutnType.ToLower() == "night")
                                                {
                                                    if (ChargeAmount > NoofNights)
                                                        ChargeAmount = NoofNights;
                                                    ChargeAmount = RatePerNight * ChargeAmount;
                                                }
                                                if (AmoutnType.ToLower() == "percentage")
                                                {

                                                    ChargeAmount = (RatePerNight * NoofNights * ChargeAmount) / 100;
                                                }
                                                #region Vat
                                                double VatOnPolicy = 0;
                                                double MainAgentCharge = ChargeAmount;
                                                double CompanyChargeAmount = ChargeAmount;
                                                ChargeAmount = _markup.ProcessHotelCancelationPrice_WithMarkup(ref MainAgentCharge, NoofNights, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), ChargeAmount, isPerce, commonSearch.B2CMarkup, AgentPercentage, AgentMarkupAmount, commonSearch.IsParentId, commonSearch.SubAgentMarkup, ref VatOnPolicy);
                                                #endregion
                                                FromDate = FromDate.AddDays(CutOffDays);
                                                ToDate = ToDate.AddDays(CutOffDays);


                                                if (ChargeAmount == 0)
                                                    FromDate = DateTime.Now.Date;


                                                if (ToDate < DateTime.Now.Date)
                                                {
                                                    continue;
                                                }

                                                if (c == DTdrRoomcancellationRule.Rows.Count - 1)
                                                {
                                                    string Ck = SearchParam.CheckInDate.Substring(3, 2) + "-" + SearchParam.CheckInDate.Substring(0, 2) + "-" + SearchParam.CheckInDate.Substring(6, 4);
                                                    ToDate = Convert.ToDateTime(Ck);
                                                }

                                                if (FromDate >= DateTime.Now.Date || ToDate >= DateTime.Now.Date)
                                                {
                                                    if (FromDate < DateTime.Now.Date)
                                                    {
                                                        FromDate = DateTime.Now.Date;
                                                    }
                                                    intRoomCancellation++;
                                                    //RoomId
                                                    if (FromDate == ToDate)
                                                        FromDate = FromDate.AddDays(-1);

                                                    DataRow drDarinaHotelCancellation = dtCancellation.NewRow();
                                                    drDarinaHotelCancellation["SupplierName"] = Provider;
                                                    drDarinaHotelCancellation["OwnsystemHotelId"] = OwnsystemHotelId;
                                                    drDarinaHotelCancellation["RoomNo"] = "1";
                                                    drDarinaHotelCancellation["RoomTypeId"] = FinalRoomType;
                                                    drDarinaHotelCancellation["RoomTypeName"] = "";
                                                    drDarinaHotelCancellation["MealId"] = MealText;
                                                    drDarinaHotelCancellation["FromDate"] = FromDate;
                                                    //drDarinaHotelCancellation["ToDate"] = ToDate.AddDays(-1).AddHours(23).AddMinutes(59).AddSeconds(59);
                                                    drDarinaHotelCancellation["ToDate"] = ToDate;
                                                    drDarinaHotelCancellation["CompanyCancellationPrice"] = MainAgentCharge;
                                                    drDarinaHotelCancellation["AgentCancellationPrice"] = ChargeAmount;
                                                    drDarinaHotelCancellation["VatdiffAmount"] = VatOnPolicy;
                                                    drDarinaHotelCancellation["BoardTypeID"] = "";
                                                    drDarinaHotelCancellation["SPUI"] = essen;
                                                    drDarinaHotelCancellation["SupplierCurrency"] = currency;
                                                    drDarinaHotelCancellation["BuyingPrice"] = CompanyChargeAmount;
                                                    //drDarinaHotelCancellation["PurchaseToken"] = RequestID + "$$$" + Acc_SerialId + "$$$" + CompanyBuyingPrice;

                                                    dtCancellation.Rows.Add(drDarinaHotelCancellation);
                                                    dtCancellation.AcceptChanges();
                                                }
                                            }
                                        }
                                        #endregion

                                    }
                                    #endregion

                                }
                                #endregion
                                Options ops = (await _supplierRes.GetCommonRoomForSuppliers(GiataId, commonSearch.tokenData, commonSearch.advancedOptions, RatePerNight, drXmlHotel, dtHotelRoom, dtCancellation, DtRoomRateBreackUp, SearchParam));
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
                    if (SearchParam != null && ResProccessTimerLogActive == "1" && timer_room_Darina.ElapsedMilliseconds > Convert.ToInt32(ResProccessTimeValueforSaveLog))
                    {
                        timer_room_Darina.Stop();
                        SearchParam.SupplierTime += ",|Finish:Darina Process Response  " + "|Time :" + timer_room_Darina.Elapsed.ToString();
                        _ = Task.Run(() => _commonMethod.FireAway_SQL_ResPorcessTimeLog("", timer_room_Darina.Elapsed.ToString(), "Darina", SearchParam.SearchKey, 1, SearchParam.AgentID));
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

        public DataSet GetDarinaAdditionDataCacheOnly(string xmlhotelcode)
        {
            DataSet ExtraResponse = new DataSet();
            try
            {
                if (!_cache.TryGetValue("staticHoteldata_darina_" + xmlhotelcode, out ExtraResponse))
                {
                    string path = Convert.ToString(_keySettings.staticHoteldata) + "Darina\\HotelInfomation_" + xmlhotelcode + ".json";

                    if (System.IO.File.Exists(path))
                    {
                        FileStream fs1 = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                        try
                        {
                            using (StreamReader r = new StreamReader(fs1))
                            {
                                string json = r.ReadToEnd();
                                ExtraResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<DataSet>(json);
                            }
                        }
                        finally
                        {
                            fs1.Close();
                            fs1.Dispose();
                            //GC.Collect();
                        }
                    }
                    _cache.Set("staticHoteldata_darina_" + xmlhotelcode, ExtraResponse, new MemoryCacheEntryOptions()
                           .SetAbsoluteExpiration(DateTime.Now.AddHours(48)));
                }

            }
            catch (Exception ex)
            {
                _exception.Error(ex);
            }
            return ExtraResponse;
        }

        public string GetDarinaStaticDataCache(DataSet dsMeal, string FileName, string FieldToSearch, string FieldSearchFor)
        {
            try
            {
                string ReturnText = "";
                DataRow[] drdsMeal = null;

                foreach (DataRow row in dsMeal.Tables[0].Rows)
                {
                    if (Convert.ToString(row[FieldToSearch]).ToLower() == Convert.ToString(FieldSearchFor).ToLower())
                    {
                        drdsMeal = new DataRow[1];
                        drdsMeal[0] = row;
                        break;
                    }
                }
                if (drdsMeal != null)
                {
                    ReturnText = Convert.ToString(drdsMeal[0]["DarinaText"]);
                }

                return ReturnText;
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
                return "";
            }

        }
        //recheck
        public async Task<CancellationDto> Darina_Prebook(DataRow[] DarinaDetails, double AgentCompanyPercentage,
   string IsB2BOrB2C, int CityID, string CountryId, int NoofNights, string mealid, string Currency, double Factor, string checkin, string checkout, int ChildAge1, int ChildAge2, int NoofChild, int NoOfAdult, DataSet dsHotel, string hotelcode, string ownsystemid, string roomtypeid, DataTable dtAgentDetail, DataTable dtSuplierMarkup, string roomno, string provider, int AgentId, DataTable dtMarkupDetails, double CompanyBuy, string SearchKey, string GiataId = "0", int hotelRating = 0)
        {
            CancellationDto cancellationDto = new CancellationDto();
            string IsBooking = "Available";


            string responsetype = "Success";
            string time = "";
            string SupplierName = provider;
            string Error = "";
            string PriceCheckRequest = "";
            string ResponseData = "";
            double RatePerStayPriceCheck = 0;


            try
            {
                DataTable DarinaPolicy = _commonMethod.GenerateCancellationDataTableNewV2();
                {
                    int CutOffDays = XMLHotelBookingCommon.CutOffDays;
                    CutOffDays = _commonMethod.GetCutOffDays(dtSuplierMarkup, provider);

                    string RequestID = "";

                    string xslRequest = CheckAvailabilityWithCancellation(DarinaDetails, checkin, checkout, ChildAge1, ChildAge2, NoofChild, NoOfAdult, hotelcode, roomtypeid, ref RequestID);
                    PriceCheckRequest = xslRequest;

                    if (xslRequest != "")
                    {
                        System.Diagnostics.Stopwatch timerroomprocess = new System.Diagnostics.Stopwatch();
                        timerroomprocess.Start();

                        //string ResponseData_Ex = GetSupplierResponse_darina(ref Error, "", xslRequest, DarinaStaticInfo.Darina_BookUrl, 60000, "darina");
                        ResponseDto responseDto = await GetSupplierResponse_darina("", xslRequest, DarinaStaticInfo.Darina_BookUrl, 60000, provider);
                        string ResponseData_Ex = responseDto.Response;
                        Error = responseDto.Error;

                        ResponseData = ResponseData_Ex;

                        timerroomprocess.Stop();
                        time = timerroomprocess.Elapsed.ToString();

                        //string ResponseData_Ex = XMLHotelBookingCommon.GetSupplierResponse("", xslRequest, XMLHotelBookingCommon.Darina_SearchUrl, 60000, "darina");
                        _commonMethod.ThirdPartyGetPolicyHistory(Convert.ToString(roomtypeid), provider, xslRequest, ResponseData_Ex, "After Prebook darina Request hit booking", provider, "Response Confirm", "-", "RaynaB2B");

                        if (ResponseData_Ex.Trim() != string.Empty && ResponseData_Ex.ToLower().Contains("resulttxt") == false && ResponseData_Ex.Contains("AccRates"))
                        {
                            double CompanyBuyingPrice = 0;
                            try
                            {
                                double SubAgentMarkup = 0;
                                int IsParentId = 1, agentID = 0;
                                if (dtAgentDetail != null)
                                {
                                    IsParentId = Convert.ToInt32(dtAgentDetail.Rows[0]["IsParentId"]);
                                    SubAgentMarkup = Convert.ToDouble(dtAgentDetail.Rows[0]["B2CMarkupPer"]);
                                    agentID = Convert.ToInt32(dtAgentDetail.Rows[0]["AgentID"]);
                                }
                                double AgentPercentage = 0.0;
                                double AgentMarkupAmount = 0.0;
                                Boolean isPerce = true;
                                Boolean blnIsShowSupplier = false;
                                int SupplierID = 0;

                                if (dtSuplierMarkup != null)
                                {
                                    dtSuplierMarkup.DefaultView.RowFilter = "SupplierName='"+ provider + "'";
                                    if (dtSuplierMarkup.DefaultView.Count > 0)
                                    {
                                        AgentPercentage = Convert.ToDouble(dtSuplierMarkup.DefaultView[0]["AgentWiseMarkup"]);
                                        blnIsShowSupplier = (Convert.ToInt16(dtSuplierMarkup.DefaultView[0]["IsShowSupplier"]) == 1 ? true : false);
                                        SupplierID = Convert.ToInt32(dtSuplierMarkup.DefaultView[0]["SupplierID"]);
                                    }
                                }
                                _markup.getAgetMarkupSetting(provider, GiataId, dtSuplierMarkup, dtMarkupDetails, ref AgentPercentage, ref AgentMarkupAmount, ref blnIsShowSupplier, ref isPerce, agentID, hotelRating, CityID, CountryId, checkin);

                                bool IsB2C = Convert.ToBoolean(Convert.ToByte(IsB2BOrB2C));
                                double B2CMarkup = (IsB2C == false ? AgentCompanyPercentage : XMLHotelBookingCommon.B2CMarkup);
                                DataSet DarinaResponse = _commonMethod.GetDataSetFromXMLString(ResponseData_Ex);
                                #region Price calculation
                                string Room_id = Convert.ToString(0);
                                DataRow[] drdtRooms = DarinaResponse.Tables["AccRates"].Select("D_Id='" + 0 + "'");
                                Double RatePerNight = Convert.ToDouble(drdtRooms[0]["RatePerNight"]);
                                Double RatePerStay = Convert.ToDouble(drdtRooms[0]["RatePerStay"]);
                                int Acc_SerialId = Convert.ToInt32(drdtRooms[0]["Serial"]);
                                CompanyBuyingPrice = RatePerStay;

                                //if (AgentId == 1297)
                                {
                                    //ExManager.Exception.LogException("IND 1 Darina Case");
                                    RatePerStayPriceCheck = RatePerStay;
                                    string ReqIdPriceCheck = Convert.ToString(drdtRooms[0]["RequestID"]);

                                    if (ReqIdPriceCheck != RequestID)
                                    {
                                        RatePerStayPriceCheck = RatePerStayPriceCheck + 1;
                                        //if (RatePerStayPriceCheck > CompanyBuy)
                                        {
                                            RequestID = ReqIdPriceCheck;
                                            CompanyBuyingPrice = RatePerStayPriceCheck;
                                        }
                                    }
                                }

                                if (RatePerStayPriceCheck.TruncateDecimalPlaces(0) > CompanyBuy.TruncateDecimalPlaces(0))
                                {
                                    responsetype = "GeneralError";
                                    ResponseData = "CompanyBuyingPrices=" + CompanyBuy + " ResponsePrice=" + RatePerStayPriceCheck + " #Response : " + ResponseData;
                                }
                                else
                                {
                                    responsetype = "Success";
                                }
                                #region "Cancellation Policy"

                                DataTable dts = DarinaResponse.Tables["cancellation"].Rows.Cast<DataRow>().OrderBy(row => Convert.ToDecimal(row["Amount"])).CopyToDataTable();
                                dts.Columns.Add("DateFilter", typeof(DateTime));

                                for (int it = 0; it < dts.Rows.Count; it++)
                                {
                                    string dtfrom = Convert.ToString(dts.Rows[it]["FromDate"]);
                                    dtfrom = dtfrom.Substring(3, 2) + "-" + dtfrom.Substring(0, 2) + "-" + dtfrom.Substring(6, 4);
                                    dts.Rows[it]["DateFilter"] = Convert.ToDateTime(Convert.ToString(dtfrom));
                                }

                                dts.DefaultView.Sort = "DateFilter asc";
                                DataTable DTdrRoomcancellationRule = dts.Clone();
                                for (int cr = 0; cr < dts.DefaultView.Count; cr++)
                                {
                                    if (Convert.ToInt32(dts.DefaultView[cr]["Acc_serial"]) == Acc_SerialId)
                                    {
                                        DTdrRoomcancellationRule.ImportRow(dts.DefaultView[cr].Row);
                                    }
                                }
                                //DataRow[] DTdrRoomcancellationRule = dts.Select("Acc_serial='" + Acc_SerialId + "'");
                                //System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-GB");
                                int intRoomCancellation = 0;

                                for (int c = 0; c < DTdrRoomcancellationRule.Rows.Count; c++)
                                {

                                    //DateTime FromDate = Convert.ToDateTime((Convert.ToString(DTdrRoomcancellationRule[c]["FromDate"]).Replace("/", "-")));
                                    //DateTime ToDate = Convert.ToDateTime((Convert.ToString(DTdrRoomcancellationRule[c]["ToDate"]).Replace("/", "-")));
                                    string dtfrom = Convert.ToString(DTdrRoomcancellationRule.Rows[c]["FromDate"]);
                                    dtfrom = dtfrom.Substring(3, 2) + "/" + dtfrom.Substring(0, 2) + "/" + dtfrom.Substring(6, 4);
                                    DateTime FromDate = Convert.ToDateTime((Convert.ToString(dtfrom).Replace("/", "-")));
                                    string dtTO = Convert.ToString(DTdrRoomcancellationRule.Rows[c]["ToDate"]);
                                    dtTO = dtTO.Substring(3, 2) + "/" + dtTO.Substring(0, 2) + "/" + dtTO.Substring(6, 4);
                                    DateTime ToDate = Convert.ToDateTime((Convert.ToString(dtTO).Replace("/", "-")));

                                    double ChargeAmount = Convert.ToDouble(DTdrRoomcancellationRule.Rows[c]["Amount"]);
                                    string AmoutnType = Convert.ToString(DTdrRoomcancellationRule.Rows[c]["AmountType"]).Replace(",", "");

                                    if (AmoutnType.ToLower() == "night")
                                    {
                                        if (ChargeAmount > NoofNights)
                                            ChargeAmount = NoofNights;
                                        ChargeAmount = RatePerNight * ChargeAmount;
                                    }
                                    if (AmoutnType.ToLower() == "percentage")
                                    {

                                        ChargeAmount = (RatePerNight * NoofNights * ChargeAmount) / 100;
                                    }
                                    #region Vat
                                    double VatOnPolicy = 0;
                                    double MainAgentCharge = ChargeAmount;
                                    double CompanyChargeAmount = ChargeAmount;
                                    ChargeAmount = _markup.ProcessHotelCancelationPrice_WithMarkup(ref MainAgentCharge, NoofNights, CityID, CountryId, ChargeAmount, isPerce, B2CMarkup, AgentPercentage, AgentMarkupAmount, IsParentId, SubAgentMarkup, ref VatOnPolicy);
                                    #endregion
                                    FromDate = FromDate.AddDays(CutOffDays);
                                    ToDate = ToDate.AddDays(CutOffDays);


                                    if (ChargeAmount == 0)
                                        FromDate = DateTime.Now.Date;


                                    if (ToDate < DateTime.Now.Date)
                                    {
                                        continue;
                                    }

                                    if (FromDate >= DateTime.Now.Date || ToDate >= DateTime.Now.Date)
                                    {
                                        if (FromDate < DateTime.Now.Date)
                                        {
                                            FromDate = DateTime.Now.Date;
                                        }
                                        intRoomCancellation++;
                                        //RoomId
                                        if (FromDate == ToDate)
                                            FromDate = FromDate.AddDays(-1);

                                        DataRow drDarinaHotelCancellation = DarinaPolicy.NewRow();
                                        drDarinaHotelCancellation["SupplierName"] = provider;
                                        drDarinaHotelCancellation["OwnsystemHotelId"] = ownsystemid;
                                        drDarinaHotelCancellation["RoomNo"] = "1";
                                        drDarinaHotelCancellation["RoomTypeId"] = roomtypeid;
                                        drDarinaHotelCancellation["RoomTypeName"] = "";
                                        drDarinaHotelCancellation["MealId"] = mealid;
                                        drDarinaHotelCancellation["FromDate"] = FromDate;
                                        drDarinaHotelCancellation["ToDate"] = ToDate.AddDays(-1).AddHours(23).AddMinutes(59).AddSeconds(59);
                                        drDarinaHotelCancellation["CompanyCancellationPrice"] = MainAgentCharge;
                                        drDarinaHotelCancellation["AgentCancellationPrice"] = ChargeAmount;
                                        drDarinaHotelCancellation["VatdiffAmount"] = VatOnPolicy;
                                        drDarinaHotelCancellation["BoardTypeID"] = "";
                                        drDarinaHotelCancellation["SupplierCurrency"] = Currency;
                                        drDarinaHotelCancellation["BuyingPrice"] = CompanyChargeAmount;
                                        drDarinaHotelCancellation["PurchaseToken"] = RequestID + "$$$" + Acc_SerialId + "$$$" + CompanyBuyingPrice;

                                        DarinaPolicy.Rows.Add(drDarinaHotelCancellation);
                                        DarinaPolicy.AcceptChanges();
                                    }
                                }
                                try
                                {

                                    string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, PriceCheckRequest, ResponseData, Error);
                                }
                                catch (Exception e)
                                {
                                    _ = _exception.ErrorNew(e, _LogAPI.Prebook, SearchKey);
                                }
                                #endregion
                                #endregion
                            }
                            catch (Exception ex)
                            {
                                try
                                {
                                    Error = ex.Message.ToString();
                                    responsetype = "GeneralError";
                                    string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, PriceCheckRequest, ResponseData, Error);
                                }
                                catch (Exception e)
                                {
                                    _ = _exception.ErrorNew(e, _LogAPI.Prebook, SearchKey);
                                }
                                _ = _exception.ErrorNew(ex, _LogAPI.Prebook, SearchKey);
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
                                _ = _exception.ErrorNew(e, _LogAPI.Prebook, SearchKey);
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
                            _ = _exception.ErrorNew(e, _LogAPI.Prebook, SearchKey);
                        }
                    }
                }
                dsHotel.Tables[2].Merge(DarinaPolicy);
            }
            catch (Exception ex)
            {
                try
                {
                    Error = ex.Message.ToString();
                    responsetype = "GeneralError";
                    string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, PriceCheckRequest, ResponseData, Error);
                }
                catch (Exception e)
                {
                    _ = _exception.ErrorNew(e, _LogAPI.Prebook, SearchKey);
                }
                _ = _exception.ErrorNew(ex, _LogAPI.Prebook, SearchKey);

                cancellationDto.dsHotelData = null;
                cancellationDto.ISBooking = "NotAvailable";

                return cancellationDto;
            }

            cancellationDto.dsHotelData = dsHotel;
            cancellationDto.ISBooking = "Available";
            return cancellationDto;
        }

        public async Task<ResponseDto> GetSupplierResponse_darina(string strUniqueId, string strxmlRQRS, string strHost, Int32 TimeOut, string Provider = "", HotelSearchParameter para = null, bool DebugMode = false, string Searchkey = "")
        {
            ResponseDto responseDto = new ResponseDto();
            string Error = "";

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

            string strOutput = "";
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
                Request.Headers.Add("Accept-Encoding", "gzip");
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
                    HttpWebResponse Response = (HttpWebResponse)await Request.GetResponseAsync();


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

                    strOutput = await Reader.ReadToEndAsync();

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

                        //_exception.ErrorLog(Convert.ToString(_keySettings.LogTrackPath), Provider + "->" + strHost + "-> Execution Time : " + timer.Elapsed.ToString());
                        _ = _exception.ErrorLogAsync(Convert.ToString(_keySettings.LogTrackPath), Provider + "->" + strHost + "-> Execution Time : " + timer.Elapsed.ToString());
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
                            Error = result1;
                            _ = _commonMethod.XMLRequestResponseTrackError(strxmlRQRS, result1, Provider, Searchkey);

                            responseDto.Response = result1;
                            responseDto.Error = result1;

                            return responseDto;
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

                    //return result1;
                }
            }
            catch (Exception ex)
            {
                Error = ex.Message.ToString();
                _ = _exception.Error(ex, Searchkey);
                //strOutput = ex.StackTrace;
                //Common.XMLRequestResponseTrackError(strxmlRQRS, strOutput, Provider, "Proper");
                //SqlConnection conn = new SqlConnection(System.Web.Configuration.WebConfigurationManager.ConnectionStrings["ConnectionString"].ToString());
                //conn.Open();
                //SqlCommand comm = new SqlCommand();
                //comm.Connection = conn;
                //comm.CommandText = "insert into XMLRequestResponseTrackError (XmlRequest,XmlResponse,ResponseDate,Provider,TypeRequest) values('" + strxmlRQRS + "','" + ex.Message.Replace("'", "''").Replace(",", " ") + "',getdate(),'','Proper')";
                //comm.ExecuteScalar();
                //conn.Close();

                //bool rethrow = ExceptionPolicy.HandleException(ex, EnumExceptionPolicyTypes.Type.Propagate_Policy); if (rethrow) { throw; }
            }
            //trackRequest(strxmlRQRS, strOutput);

            responseDto.Response = strOutput;
            responseDto.Error = Error;
            return responseDto;
        }

        public async Task<string> Darina_Prebook_WithoutPolicy(DataRow[] DarinaDetails, string checkin, string checkout, int ChildAge1, int ChildAge2, int NoofChild, int NoOfAdult, string hotelcode, string roomtypeid, string provider, int AgentId, double CompanyBuy)
        {
            string responsetype = "Success";
            string time = "";
            string SupplierName = "darina";
            string Error = "";
            string PriceCheckRequest = "-";
            string ResponseData = "-";
            string Request_ID = "";
            double RatePerStayPriceCheck = 0;
            try
            {
                DataTable DarinaPolicy = _commonMethod.GenerateCancellationDataTableNewV2();
                {

                    string RequestID = "";

                    string xslRequest = CheckAvailabilityWithCancellation(DarinaDetails, checkin,  checkout,  ChildAge1,  ChildAge2,  NoofChild,  NoOfAdult,  hotelcode,  roomtypeid,ref RequestID);
                    PriceCheckRequest = xslRequest;
                    if (xslRequest != "")
                    {
                        System.Diagnostics.Stopwatch timerroomprocess = new System.Diagnostics.Stopwatch();
                        timerroomprocess.Start();

                      
                        ResponseDto responseDto = await GetSupplierResponse_darina("", xslRequest, DarinaStaticInfo.Darina_BookUrl, 60000, "darina");
                        string ResponseData_Ex = responseDto.Response;
                        Error = responseDto.Error;

                        ResponseData = ResponseData_Ex;

                        timerroomprocess.Stop();
                        time = timerroomprocess.Elapsed.ToString();

                        //string ResponseData_Ex = XMLHotelBookingCommon.GetSupplierResponse("", xslRequest, XMLHotelBookingCommon.Darina_SearchUrl, 60000, "darina");
                        _commonMethod.ThirdPartyGetPolicyHistory(Convert.ToString(roomtypeid), Convert.ToString("darina"), xslRequest, ResponseData_Ex, "After Prebook darina Request hit booking", "darina", "Response Confirm", "-", "RaynaB2B");

                        if (ResponseData_Ex.Trim() != string.Empty && ResponseData_Ex.ToLower().Contains("resulttxt") == false && ResponseData_Ex.Contains("AccRates"))
                        {
                            double CompanyBuyingPrice = 0;
                            try
                            {
                                //double SubAgentMarkup = 0;
                                //int IsParentId = 1, agentID = 0;
                                //if (dtAgentDetail != null)
                                //{
                                //    IsParentId = Convert.ToInt32(dtAgentDetail.Rows[0]["IsParentId"]);
                                //    SubAgentMarkup = Convert.ToDouble(dtAgentDetail.Rows[0]["B2CMarkupPer"]);
                                //    agentID = Convert.ToInt32(dtAgentDetail.Rows[0]["AgentID"]);
                                //}
                                //double AgentPercentage = 0.0;
                                //double AgentMarkupAmount = 0.0;
                                //Boolean isPerce = true;
                                //Boolean blnIsShowSupplier = false;
                                //int SupplierID = 0;

                                //if (dtSuplierMarkup != null)
                                //{
                                //    dtSuplierMarkup.DefaultView.RowFilter = "SupplierName='darina'";
                                //    if (dtSuplierMarkup.DefaultView.Count > 0)
                                //    {
                                //        AgentPercentage = Convert.ToDouble(dtSuplierMarkup.DefaultView[0]["AgentWiseMarkup"]);
                                //        blnIsShowSupplier = (Convert.ToInt16(dtSuplierMarkup.DefaultView[0]["IsShowSupplier"]) == 1 ? true : false);
                                //        SupplierID = Convert.ToInt32(dtSuplierMarkup.DefaultView[0]["SupplierID"]);
                                //    }
                                //}
                                //Common.getAgetMarkupSetting("darina", GiataId, dtSuplierMarkup, dtMarkupDetails, ref AgentPercentage, ref AgentMarkupAmount, ref blnIsShowSupplier, ref isPerce, agentID, hotelRating, CityID, CountryId, checkin);

                                //bool IsB2C = Convert.ToBoolean(Convert.ToByte(IsB2BOrB2C));
                                //double B2CMarkup = (IsB2C == false ? AgentCompanyPercentage : XMLHotelBookingCommon.B2CMarkup);
                                DataSet DarinaResponse = _commonMethod.GetDataSetFromXMLString(ResponseData_Ex);
                                #region Price calculation
                                string Room_id = Convert.ToString(0);
                                DataRow[] drdtRooms = DarinaResponse.Tables["AccRates"].Select("D_Id='" + 0 + "'");
                                Double RatePerNight = Convert.ToDouble(drdtRooms[0]["RatePerNight"]);
                                Double RatePerStay = Convert.ToDouble(drdtRooms[0]["RatePerStay"]);
                                int Acc_SerialId = Convert.ToInt32(drdtRooms[0]["Serial"]);
                                CompanyBuyingPrice = RatePerStay;

                                //if (AgentId == 1297)
                                {
                                    //ExManager.Exception.LogException("IND 1 Darina Case");
                                    RatePerStayPriceCheck = RatePerStay;
                                    string ReqIdPriceCheck = Convert.ToString(drdtRooms[0]["RequestID"]);

                                    if (ReqIdPriceCheck != RequestID)
                                    {
                                        RatePerStayPriceCheck = RatePerStayPriceCheck + 1;
                                        //if (RatePerStayPriceCheck > CompanyBuy)
                                        {
                                            RequestID = ReqIdPriceCheck;
                                            CompanyBuyingPrice = RatePerStayPriceCheck;
                                        }
                                    }
                                }
                                if (RatePerStayPriceCheck.TruncateDecimalPlaces(0) > CompanyBuy.TruncateDecimalPlaces(0))
                                {
                                    responsetype = "GeneralError";
                                    ResponseData = "CompanyBuyingPrices=" + CompanyBuy + " ResponsePrice=" + RatePerStayPriceCheck + " #Response : " + ResponseData;
                                }
                                else
                                {
                                    responsetype = "Success";
                                }
                                try
                                {
                                    string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, PriceCheckRequest, ResponseData, Error);
                                }
                                catch (Exception e)
                                {
                                    _ = _exception.Error(e, "");
                                }
                                Request_ID = RequestID + "$$$" + Acc_SerialId + "$$$" + CompanyBuyingPrice;
                                #endregion
                            }
                            catch (Exception ex)
                            {
                                try
                                {
                                    Error = ex.Message.ToString();
                                    responsetype = "GeneralError";
                                    string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, PriceCheckRequest, ResponseData, Error);
                                }
                                catch (Exception e)
                                {
                                    _ = _exception.Error(e, "");
                                }
                                _ = _exception.Error(ex);
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
                                _ = _exception.Error(e, "");
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
                            _ = _exception.Error(e, "");
                        }
                    }
                }
                //dsHotel.Tables[2].Merge(DarinaPolicy);
            }
            catch (Exception ex)
            {
                try
                {
                    Error = ex.Message.ToString();
                    responsetype = "GeneralError";
                    string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, PriceCheckRequest, ResponseData, Error);
                }
                catch (Exception e)
                {
                    _ = _exception.Error(e, "");
                }
                _ = _exception.Error(ex);
                return "";
            }
            return Request_ID;
        }

        public string CheckAvailabilityWithCancellation(DataRow[] DarinaDetails, string checkin, string checkout, int ChildAge1, int ChildAge2, int NoofChild, int NoOfAdult, string hotelcode, string roomtypeid, ref string RequestID)
        {
            string SearchLevel = "CheckAvailabilityWithCancellation_NoCache_LiveCalculation";
            string[] Arr = Convert.ToString(roomtypeid).Split(':');
            RequestID = Arr[0];
            //string Acc_SerialId = Arr[1];
            string Darina_NationalityCode = Arr[2];
            string[] arrRequest = RequestID.Split('|');
            string RoomType = arrRequest[1];
            string MealPlan = arrRequest[2];
            string OccupancyID = arrRequest[3];
            string Availability = arrRequest[8];

            string xslRequest = "<soap:Envelope xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns:soap=\"http://schemas.xmlsoap.org/soap/envelope/\">" +
                                        "  <soap:Body>" +
                                        "    <" + SearchLevel + " xmlns =\"http://travelcontrol.softexsw.us/\">" +
                                        "      <SecStr>" + Convert.ToString(DarinaDetails[0]["Version"]) + "</SecStr>" +
                                        "      <AccountName>" + DarinaStaticInfo.Darina_AccountName + "</AccountName>" +
                                        "      <UserName>" + Convert.ToString(DarinaDetails[0]["UserName"]) + "</UserName>" +
                                        "     <Password>" + Convert.ToString(DarinaDetails[0]["Password"]) + "</Password>" +
                                        "      <AgentID>" + Convert.ToString(DarinaDetails[0]["UserID"]) + "</AgentID>" +
                                        "      <FromDate>" + checkin + "</FromDate>" +
                                        "      <ToDate>" + checkout + "</ToDate>" +

                                        "<HotelID>" + hotelcode + "</HotelID>" +
                                        "      <AdultPax>" + NoOfAdult + "</AdultPax>" +
                                        "      <ChildPax>" + NoofChild + "</ChildPax>" +
                                        "      <ChildrenAges>" +
                                        (NoofChild == 0 ? "" : // child 0
                                        NoofChild == 1 ? "<int>" + ChildAge1 + "</int>" :                 // for 1 child
                                        "<int>" + ChildAge1 + "</int> <int>" + ChildAge2 + "</int>") +  // for two child

                                        "      </ChildrenAges>" +
                                        "      <ExtraBedAdult>false</ExtraBedAdult>" +
                                        "      <ExtraBedChild>false</ExtraBedChild>" +
                                        "      <Nationality_CountryID>" + Darina_NationalityCode + "</Nationality_CountryID>" +
                                        "      <CurrencyID>" + DarinaStaticInfo.Darina_Currency + "</CurrencyID>" +
                                        "      <RoomType>" + RoomType + "</RoomType>" +
                                        "      <MealPlan>" + MealPlan + "</MealPlan>" +
                                        "      <OccupancyID>" + OccupancyID + "</OccupancyID>" +
                                        "      <Availability>ShowAvailableOnly</Availability>" +//ShowAll or ShowAvailableOnly

                                        "      <GetHotelImageLink>false</GetHotelImageLink>" +
                                        "      <GetHotelMapLink>false</GetHotelMapLink>" +

                                        "    </" + SearchLevel + ">" +
                                        "  </soap:Body>" +
                                        "</soap:Envelope>";


            return xslRequest;
        }

        public string GetDarinaBookingRequest(DataSet dsCartDetails, DataTable dtPassengers, int i, int j, String ClientID, string UserName, string Password, string SecurityKey, string AgentCode,string SearchKey)
        {

            try
            {
                string Roomtypeid = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomTypeID"]);
                string OptionRefId = Convert.ToString(dsCartDetails.Tables[1].DefaultView[0]["purchaseToken"]);
                string[] spitid = OptionRefId.Split(new String[] { "$$$" }, StringSplitOptions.None);
                //drDarinaHotelCancellation["PurchaseToken"] = RequestID + "$$" + Acc_SerialId + "$$" + CompanyBuyingPrice;


                string RequestId = spitid[0].ToString();
                string Acc_SerialId = spitid[1].ToString();
                // ClientID = ""; Password=""; RequestId = "";Acc_SerialId = "";

                string[] arr = Roomtypeid.Split(':');
                string Darina_NationalityCode = arr[2];
                StringBuilder sbFinalBook = new StringBuilder();

                sbFinalBook.Append("<soap:Envelope xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns:soap=\"http://schemas.xmlsoap.org/soap/envelope/\">");
                sbFinalBook.Append("<soap:Body>");
                sbFinalBook.Append("<SubmitBooking_Accomodation xmlns=\"http://travelcontrol.softexsw.us/\">");
                sbFinalBook.Append("<SecStr>" + SecurityKey + "</SecStr>");
                sbFinalBook.Append("<AccountName>" + DarinaStaticInfo.Darina_AccountName + "</AccountName>");
                sbFinalBook.Append("<UserName>" + UserName + "</UserName>");
                sbFinalBook.Append("<Password>" + Password + "</Password>");
                sbFinalBook.Append("<AgentID>" + ClientID + "</AgentID>");
                string checkindate = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["CheckInDate"]);
                checkindate = checkindate.Substring(6, 2) + "/" + checkindate.Substring(4, 2) + "/" + checkindate.Substring(0, 4);

                string checkoutdate = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["CheckOutDate"]);
                checkoutdate = checkoutdate.Substring(6, 2) + "/" + checkoutdate.Substring(4, 2) + "/" + checkoutdate.Substring(0, 4);

                int NoOfChild = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[j]["NoOfChild"]);
                int NoOfAdults = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[j]["NoOfAdults"]);
                sbFinalBook.Append("<FromDate>" + checkindate + "</FromDate>");
                sbFinalBook.Append("<ToDate>" + checkoutdate + "</ToDate>");
                string internalref = "API_" + AgentCode + "_" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["UserCartDetailId"]);
                sbFinalBook.Append("<AgentRef>" + internalref + "</AgentRef>");
                //sbFinalBook.Append("<AgentRef>>Book-" + Common.getSession("AgentCode") + "-" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["UserCartDetailId"]) + "</AgentRef>");
                sbFinalBook.Append("<CurrencyID>" + DarinaStaticInfo.Darina_Currency + "</CurrencyID>");
                sbFinalBook.Append("<Nationality_CountryID>" + Darina_NationalityCode + "</Nationality_CountryID>");
                sbFinalBook.Append("<AdultPax>" + Convert.ToString(NoOfAdults) + "</AdultPax>");
                sbFinalBook.Append("<ChildPax>" + Convert.ToString(NoOfChild) + "</ChildPax>");
                sbFinalBook.Append("<ChildrenAges>");
                if (NoOfChild > 0)
                {
                    if (NoOfChild == 1)
                        sbFinalBook.Append("<int>" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["ChildAge1"]) + "</int>");
                    else
                    {
                        sbFinalBook.Append("<int>" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["ChildAge1"]) + "</int>");
                        sbFinalBook.Append("<int>" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["ChildAge2"]) + "</int>");
                    }
                }
                sbFinalBook.Append("</ChildrenAges>");
                sbFinalBook.Append("<Remarks></Remarks>");
                sbFinalBook.Append("<Passengers>");

                string isLead = string.Empty;
                dtPassengers.DefaultView.RowFilter = "PickUp='" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomTypeID"]) + "' and RoomNo='" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomNumber"]) + "' and IsLead=1";
                if (dtPassengers.DefaultView.Count == 0)
                    isLead = "true";
                dtPassengers.DefaultView.RowFilter = "";
                dtPassengers.DefaultView.RowFilter = "PickUp='" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomTypeID"]) + "' and RoomNo='" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomNumber"]) + "' and UserCartId='" + dsCartDetails.Tables[0].DefaultView[i]["UserCartId"] + "'";
                // dtPassengers.DefaultView.RowFilter = "PickUp='" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomTypeID"]) + "' and RoomNo='" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomNumber"]) + "'";
                dtPassengers.DefaultView.Sort = "IsLead desc";
                if (dtPassengers.DefaultView.Count > 0)
                {
                    for (int k = 0; k < dtPassengers.DefaultView.Count; k++)
                    {
                        sbFinalBook.Append("<PassInfo>");
                        sbFinalBook.Append("<Serial xmlns=\"http://dtcws.softexsw.us\">" + (k + 1) + "</Serial>");
                        sbFinalBook.Append("<Acc_Serial xmlns=\"http://dtcws.softexsw.us\">" + Acc_SerialId + "</Acc_Serial>");
                        sbFinalBook.Append("<Name xmlns=\"http://dtcws.softexsw.us\">" + Convert.ToString(dtPassengers.DefaultView[k]["FirstName"]).Trim() + " " + Convert.ToString(dtPassengers.DefaultView[k]["LastName"]).Trim() + "</Name>");
                        sbFinalBook.Append("<Type xmlns=\"http://dtcws.softexsw.us\">" + Convert.ToString(dtPassengers.DefaultView[k]["PaxType"]).Trim() + "</Type>");
                        sbFinalBook.Append("<Primary xmlns=\"http://dtcws.softexsw.us\">" + (Convert.ToInt16(dtPassengers.DefaultView[k]["IsLead"]) == 1 ? "true" : (isLead == "yes" && k == 0 ? isLead : "false")) + "</Primary>");
                        //sbFinalBook.Append("<Age xmlns=\"http://dtcws.softexsw.us\">" + Convert.ToString(dtPassengers.DefaultView[j]["ChildAge"]) + "</Age>");
                        if (Convert.ToString(dtPassengers.DefaultView[k]["PaxType"]).Trim().ToLower() == "adult")
                            sbFinalBook.Append("<Age xmlns=\"http://dtcws.softexsw.us\">35</Age>");
                        else
                            sbFinalBook.Append("<Age xmlns=\"http://dtcws.softexsw.us\">" + Convert.ToString(dtPassengers.DefaultView[k]["ChildAge"]) + "</Age>");
                        sbFinalBook.Append("<NeedVisa xmlns=\"http://dtcws.softexsw.us\">false</NeedVisa>");
                        sbFinalBook.Append("</PassInfo>");

                    }
                }


                sbFinalBook.Append(" </Passengers>");
                sbFinalBook.Append("  <RequestID>" + RequestId + "</RequestID>");
                sbFinalBook.Append("   <IsTest>false</IsTest>");
                sbFinalBook.Append("  </SubmitBooking_Accomodation>");
                sbFinalBook.Append(" </soap:Body> ");
                sbFinalBook.Append("</soap:Envelope>");
                return sbFinalBook.ToString();
            }
            catch (Exception ex)
            {
                _exception.ErrorNew(ex, _LogAPI.Book,SearchKey);
                return "";
            }

        }

        public string GetDarinaSecurityData(DataRow[] DarinaDetails)
        {
            try
            {
                string str = "<SecStr>" + Convert.ToString(DarinaDetails[0]["Version"]) + "</SecStr>" +
                              "<AccountName>" + DarinaStaticInfo.Darina_AccountName + "</AccountName>" +
                              "<UserName>" + Convert.ToString(DarinaDetails[0]["UserName"]) + "</UserName>" +
                              "<Password>" + Convert.ToString(DarinaDetails[0]["Password"]) + "</Password>" +
                             "<AgentID>" + Convert.ToString(DarinaDetails[0]["UserID"]) + "</AgentID>";
                return str;
            }
            catch (Exception ex)
            {
                return "";
            }

        }
    }
}
