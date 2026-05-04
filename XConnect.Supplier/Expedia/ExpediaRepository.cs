//using Newtonsoft.Json;
//using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;
using System.Data;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Azure;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Newtonsoft.Json.Linq;
using RestSharp;
using XConnect.CommonUtilities;
using XConnect.CommonUtilities.Interface;
using XConnect.DAL.Interface;
using XConnect.ErrorLogHandler.Interface;
using XConnect.Markup.Interface;
using XConnect.Models;
using XConnect.Models.Hotel;
using XConnect.Supplier.Interface;

using static Azure.Core.HttpHeader;
using static XConnect.Expedia.EAN;
using static XConnect.Models.ErrorLog;
using static XConnect.Supplier.Booking.Com.BookingDotComRoom;
using static XConnect.Supplier.Omnibees.HotelSearchResponse;
using Options = XConnect.Models.Options;


namespace XConnect.Expedia
{
    public class ExpediaRepository : IExpedia
    {
        private readonly ICommonMethod _commonMethod;
        private readonly IException _exception;
        private readonly IMarkup _markup;
        private readonly ISupplierRes _supplierRes;
        private readonly KeySettings _keySettings;
        private readonly IDLConnection _dLConnection;
        private readonly SupplierConfiguration _supplierConfig;//Added on 26-FEB-26


        public ExpediaRepository(ICommonMethod commonMethod, IMarkup markup, ISupplierRes supplierRes, IOptions<KeySettings> keySettings, IDLConnection dLConnection, IException exception, IOptions<SupplierConfiguration> supplierConfig)
        {
            _commonMethod = commonMethod;
            _markup = markup;
            _supplierRes = supplierRes;
            _keySettings = keySettings.Value;
            _dLConnection = dLConnection;
            _exception = exception;
            _supplierConfig = supplierConfig.Value;
        }

        public string EAN_NationalityCode(string Nationality)
        {
            switch (Nationality.ToLower())// as per Ticket#34810
            {
                case "by":
                case "ru":
                case "ye":
                case "br":
                case "so":
                case "af":
                case "iq":
                case "ir":
                case "sy":
                case "ve":
                case "ps":
                case "xk":
                case "ht":
                case "cu":
                case "cc":
                case "vi":
                case "kp":
                case "": // blank
                case "an":
                case "ki":
                case "sm":
                    return "MY";
                default:
                    return Nationality;
            }
        }

        public void GetEANRequestOnly_async_new(bool SearchByOneID, DataRow[] commonCode, List<StarRating> listRating, List<SupplierSearchConfig> lstEAN_SearchConfig, List<SupplierDetails> lstEAN_Details, DataTable dtPreferredDataHotel, string EchoToken, string Platform, string AgentCode, HotelSearchParameter SearchPara, bool IsB2B, bool IsHotelWiseSearch, ref ConcurrentBag<HotelX> AllTask, string XMLCOUNTRYCode, string XMLCITY, DataTable dtROE, TokenData tokenData, bool ISPKG = false, int EANProfit = 55)
        {
            try
            {
                if (lstEAN_Details != null && lstEAN_Details.Count > 0)
                {
                    List<SupplierDetails> EANMain = new List<SupplierDetails>();
                    List<SupplierDetails> EANChild = new List<SupplierDetails>();

                    foreach (SupplierDetails item in lstEAN_Details)
                    {
                        if (item.SupplierName.ToLower() == "ean")
                            EANMain.Add(item);
                        else
                            EANChild.Add(item);
                    }


                    for (int x = 0; x < EANChild.Count; x++)
                    {
                        int SupplierID = EANChild[x].SupplierID;

                        List<SupplierSearchConfig> EXPSearchConfig = lstEAN_SearchConfig.Where(x => x != null && x.SupplierID == SupplierID).ToList();



                        if (EXPSearchConfig != null && EXPSearchConfig.Count > 0)
                        {


                            for (int k = 0; k < EXPSearchConfig.Count; k++)
                            {
                                if (Convert.ToInt32(EXPSearchConfig[k].CityID) == 0 || Convert.ToInt32(EXPSearchConfig[k].CityID) == SearchPara.CityID)//All City means 0
                                {
                                    if (IsB2B)
                                    {

                                        string Ids = string.Empty;
                                        string xmlbase = "";
                                        _commonMethod.GetPrefferedHotelCodes(ref Ids, ref xmlbase, ref lstEAN_SearchConfig, "Xpedia", dtPreferredDataHotel, SearchPara.IsSearchFor);

                                        //EANDynamicHit_async(SearchByOneID, IsHotelWiseSearch, ref AllTask, SearchPara, EANChild[x], lstEAN_SearchConfig, k, Ids, EchoToken, Platform, AgentCode, XMLCOUNTRYCode, XMLCITY, dtROE, tokenData, ISPKG, commonCode, EANProfit);
                                        try
                                        {
                                            if (Convert.ToInt32(EXPSearchConfig[k].Stars) == 0)
                                            {
                                                string Supplier = Convert.ToString(EANChild[x].SupplierName);

                                                if (Supplier.ToLower().IndexOf("_pkg") >= 0)
                                                {
                                                    ISPKG = true;
                                                }


                                                bool DebugMode = (Convert.ToInt32(EXPSearchConfig[k].DebugMode) == 0 ? false : true);
                                                if (commonCode != null && commonCode.Length > 0)
                                                {
                                                    //if (Convert.ToInt32(EXPSearchConfig[k].CityID) == 0 || Convert.ToInt32(EXPSearchConfig[k].CityID) == SearchPara.CityID)//All City means 0
                                                    {
                                                        {
                                                            //string currency = "USD";
                                                            string currency = Convert.ToString(EANChild[x].CurrencyShortName);

                                                            double CurrencyFactor = 1;
                                                            if (currency.ToLower() != tokenData.SiteCurrency.ToLower())
                                                            {
                                                                CurrencyFactor = _commonMethod.GetROEByCurrency(currency, dtROE);
                                                            }


                                                            int NoOfRecords = Convert.ToInt32(EXPSearchConfig[k].Noofrecords);
                                                            DataTable[] splittedtables = commonCode.AsEnumerable()
                                                                                      .Select((row, index) => new { row, index })
                                                                                      .GroupBy(x => x.index / NoOfRecords)  // integer division, the fractional part is truncated
                                                                                      .Select(g => g.Select(x => x.row).CopyToDataTable())
                                                                                      .ToArray();
                                                            for (int a = 0; a < splittedtables.Length; a++)
                                                            {

                                                                var paramValues1 = String.Join(",", splittedtables[a].Select().Select(x => String.Join(",", Convert.ToString(x.ItemArray[0]))));
                                                                string IdsForSearch = Convert.ToString(paramValues1);

                                                                Ids = IdsForSearch;
                                                                HotelX xhotel = new HotelX();

                                                                xhotel.EanProfit = EANProfit;
                                                                xhotel.Supplier = Convert.ToString(EANChild[x].SupplierName);
                                                                xhotel.DebugeMode = DebugMode;
                                                                xhotel.SearchTimeOut = Convert.ToInt32(EXPSearchConfig[k].HotelSearchTimeOut);
                                                                //if (Convert.ToInt32(EXPSearchConfig[k].Hit) > 0)
                                                                xhotel.ProcessTimeout = Convert.ToInt32(EXPSearchConfig[k].HotelSearchTimeOut2);



                                                                //if (Supplier.ToLower() == _supplierConfig.Expedia.EAN_B2BV3 || Supplier.ToLower() == _supplierConfig.Expedia.EAN_PKGV3)
                                                                //    xhotel.URL = Convert.ToString(EANChild[x].WebSiteURL) + _supplierConfig.Expedia.EAN_VersionV3 + _supplierConfig.Expedia.EAN_HotelList_Url;
                                                                //else
                                                                //    xhotel.URL = Convert.ToString(EANChild[x].WebSiteURL) + _supplierConfig.Expedia.EAN_Version + _supplierConfig.Expedia.EAN_HotelList_Url;

                                                                if (true)// (Supplier.ToLower() == _supplierConfig.Expedia.EAN_B2BV3 || Supplier.ToLower() == _supplierConfig.Expedia.EAN_PKGV3) //Added on 28-10-25
                                                                {
                                                                    if (_keySettings.SupplierProxy == "0")
                                                                        xhotel.URL = Convert.ToString(EANChild[x].WebSiteURL) + _supplierConfig.Expedia.EAN_VersionV3 + _supplierConfig.Expedia.EAN_HotelList_Url;
                                                                    else
                                                                        xhotel.URL = Convert.ToString(EANChild[x].WebSiteURLforAPI) + _supplierConfig.Expedia.EAN_VersionV3 + _supplierConfig.Expedia.EAN_HotelList_Url;
                                                                }
                                                                else
                                                                {
                                                                    if (_keySettings.SupplierProxy == "0")
                                                                        xhotel.URL = Convert.ToString(EANChild[x].WebSiteURL) + _supplierConfig.Expedia.EAN_Version + _supplierConfig.Expedia.EAN_HotelList_Url;
                                                                    else
                                                                        xhotel.URL = Convert.ToString(EANChild[x].WebSiteURLforAPI) + _supplierConfig.Expedia.EAN_Version + _supplierConfig.Expedia.EAN_HotelList_Url;
                                                                }
                                                                xhotel.UserId = Convert.ToString(EANChild[x].UserID);
                                                                xhotel.UserName = Convert.ToString(EANChild[x].UserName);
                                                                xhotel.PassWord = Convert.ToString(EANChild[x].Password);
                                                                xhotel.CurrencyCode = currency;
                                                                xhotel.CurrencyFactor = CurrencyFactor;
                                                                xhotel.Supplier = Supplier;
                                                                xhotel.EchoToken = EchoToken;
                                                                xhotel.PlatForm = Platform;
                                                                xhotel.XmlCity = XMLCITY;
                                                                xhotel.XmlCountry = XMLCOUNTRYCode;
                                                                xhotel.Version = Convert.ToString(EANChild[x].version);

                                                                xhotel.ReturnRooms = EANChild[x].ReturnRooms;//040225
                                                                xhotel.TechName = EANChild[x].TechName;//040225

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
                                                                    objrq.Request = XMLEANRequestOnly_async(SearchByOneID, "1", "5", xhotel.URL, xhotel.UserId, xhotel.UserName, xhotel.PassWord, xhotel.SearchTimeOut, SearchPara, Convert.ToInt32(EXPSearchConfig[k].Noofrecords), Convert.ToString(EANChild[x].version), Ids, EchoToken, Platform, Convert.ToString(EANChild[x].SupplierName), XMLCOUNTRYCode, XMLCITY, ISPKG, AgentCode, xhotel.ReturnRooms, xhotel.CurrencyCode);
                                                                    objrq.Response = ""; RQRS.Add(objrq);
                                                                }
                                                                xhotel.RQRS = RQRS;

                                                                #endregion
                                                                AllTask.Add(xhotel);
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
                }
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
            }

        }
        public void GetEANRequestOnly_async(bool SearchByOneID, DataRow[] commonCode, List<StarRating> listRating, List<SupplierSearchConfig> EAN_B2BSearchConfig, List<SupplierDetails> EAN_B2BDetails, DataTable dtPreferredDataHotel, string EchoToken, string Platform, string AgentCode, HotelSearchParameter SearchPara, bool IsB2B, bool IsHotelWiseSearch, ref ConcurrentBag<HotelX> AllTask, string XMLCOUNTRYCode, string XMLCITY, DataTable dtROE, TokenData tokenData, bool ISPKG = false, int EANProfit = 55)
        {
            try
            {
                string Ids = string.Empty;
                string xmlbase = "";
                _commonMethod.GetPrefferedHotelCodes(ref Ids, ref xmlbase, ref EAN_B2BSearchConfig, "Xpedia", dtPreferredDataHotel, SearchPara.IsSearchFor);
                if (EAN_B2BSearchConfig != null)
                {
                    //DataTable dtCurrency = Common.GetCountryCurrency("USD");
                    //double ExpediaCurrencyFactor = 1;// Convert.ToDouble(dtCurrency.Rows[0]["AmtToExch"]);
                    //if (ExpediaCurrencyFactor == 0)
                    //    ExpediaCurrencyFactor = XMLHotelBookingCommon.XpediaCurrancyFactor;
                    for (int k = 0; k < EAN_B2BSearchConfig.Count; k++)
                    {
                        //if (Convert.ToInt32(EXPSearchConfig[k]["Hit"]) != Hit)
                        //    continue;
                        if (Convert.ToInt32(EAN_B2BSearchConfig[k].CityID) == 0 || Convert.ToInt32(EAN_B2BSearchConfig[k].CityID) == SearchPara.CityID)//All City means 0
                        {
                            if (IsB2B)
                            {
                                //List<HotelCodeByCity> DtHotelDAta = FilterFinalList(commonCode, listRating);

                                // hit b2b sa 
                                // EXPDetails = dsAgentDetails.Tables[1].Select("SupplierName='Expedia_B2B'");//hit b2b sa first
                                if (EAN_B2BDetails.Count > 0 || commonCode.Length > 0)
                                    EANDynamicHit_async(SearchByOneID, IsHotelWiseSearch, ref AllTask, SearchPara, EAN_B2BDetails, EAN_B2BSearchConfig, k, Ids, EchoToken, Platform, AgentCode, XMLCOUNTRYCode, XMLCITY, dtROE, tokenData, ISPKG, commonCode, EANProfit);

                            }
                            //else
                            //{
                            //    //hit b2b 
                            //    EXPDetails = dsAgentDetails.Tables[1].Select("SupplierName='Expedia_B2C'");//for b2c
                            //    if (EXPDetails.Length > 0)
                            //        ExpediaDynamicHit_async(IsHotelWiseSearch, ref AllTask, SearchPara, EXPDetails, EXPSearchConfig, k, ExpediaCurrencyFactor, Ids, EchoToken, Platform, AgentCode, XMLCOUNTRYCode, XMLCITY, DtHotelDAta);
                            //}
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
            }
        }

        public void EANDynamicHit_async(bool SearchByOneID, bool IsHotelWiseSearch, ref ConcurrentBag<HotelX> AllTask, HotelSearchParameter SearchPara, List<SupplierDetails> EXPDetails, List<SupplierSearchConfig> EXPSearchConfig, int k,
          string Ids, string EchoToken, string Platform, string AgentCode, string XMLCOUNTRYCode, string XMLCITY, DataTable dtROE, TokenData tokenData, bool ISPKG = false, DataRow[] commonCode = null, int EANProfit = 55)
        {
            try
            {
                if (Convert.ToInt32(EXPSearchConfig[k].Stars) == 0)
                {
                    string Supplier = Convert.ToString(EXPDetails[0].SupplierName);

                    bool DebugMode = (Convert.ToInt32(EXPSearchConfig[k].DebugMode) == 0 ? false : true);
                    if (commonCode != null && commonCode.Length > 0)
                    {
                        if (Convert.ToInt32(EXPSearchConfig[k].CityID) == 0 || Convert.ToInt32(EXPSearchConfig[k].CityID) == SearchPara.CityID)//All City means 0
                        {
                            {
                                //string currency = _supplierConfig.Expedia.EANCurrency;
                                string currency = EXPDetails[0].CurrencyShortName; //210225
                                double CurrencyFactor = 1;
                                _commonMethod.GetXtgCurrencyByCountry(Supplier, SearchPara.CountryCode, ref currency, AgentCode);
                                if (currency.ToLower() != tokenData.SiteCurrency.ToLower())
                                {
                                    //DataTable dtInrCurrency = Common.GetCountryCurrency(currency);
                                    CurrencyFactor = _commonMethod.GetROEByCurrency(currency, dtROE); //Convert.ToDouble(dtInrCurrency.Rows[0]["AmtToExch"]);
                                }

                                //int Noofrecords = Convert.ToInt32(EXPSearchConfig[k].Noofrecords);
                                //double count = Convert.ToDouble(DtHotelDAta.Count);
                                //int NoOfDiv = Convert.ToInt32(Math.Ceiling(count / Convert.ToDouble(Noofrecords)));
                                //List<List<XMLID_search>> lstxml = GetXMLIDString(NoOfDiv, Noofrecords, count, DtHotelDAta);

                                int NoOfRecords = Convert.ToInt32(EXPSearchConfig[k].Noofrecords);
                                DataTable[] splittedtables = commonCode.AsEnumerable()
                                                          .Select((row, index) => new { row, index })
                                                          .GroupBy(x => x.index / NoOfRecords)  // integer division, the fractional part is truncated
                                                          .Select(g => g.Select(x => x.row).CopyToDataTable())
                                                          .ToArray();
                                for (int a = 0; a < splittedtables.Length; a++)
                                {

                                    var paramValues1 = String.Join(",", splittedtables[a].Select().Select(x => String.Join(",", Convert.ToString(x.ItemArray[0]))));
                                    string IdsForSearch = Convert.ToString(paramValues1);

                                    //ReqString, XMLHotelBookingCommon.Xpedia_HotelList_Url, CID, Sign, ApiKey, SearchTimeOut, version, echotoken, platform, true
                                    Ids = IdsForSearch;
                                    HotelX xhotel = new HotelX();

                                    xhotel.EanProfit = EANProfit;
                                    xhotel.Supplier = Convert.ToString(EXPDetails[0].SupplierName);
                                    xhotel.DebugeMode = DebugMode;
                                    xhotel.SearchTimeOut = Convert.ToInt32(EXPSearchConfig[k].HotelSearchTimeOut);
                                    //if (Convert.ToInt32(EXPSearchConfig[k].Hit) > 0)
                                    xhotel.ProcessTimeout = Convert.ToInt32(EXPSearchConfig[k].HotelSearchTimeOut2);

                                    xhotel.URL = Convert.ToString(EXPDetails[0].WebSiteURL);
                                    xhotel.UserId = Convert.ToString(EXPDetails[0].UserID);
                                    xhotel.UserName = Convert.ToString(EXPDetails[0].UserName);
                                    xhotel.PassWord = Convert.ToString(EXPDetails[0].Password);
                                    xhotel.CurrencyCode = currency;
                                    xhotel.CurrencyFactor = CurrencyFactor;
                                    xhotel.Supplier = Supplier;
                                    xhotel.EchoToken = EchoToken;
                                    xhotel.PlatForm = Platform;
                                    xhotel.XmlCity = XMLCITY;
                                    xhotel.XmlCountry = XMLCOUNTRYCode;
                                    xhotel.Version = Convert.ToString(EXPDetails[0].version);
                                    //xhotel.Request = Hotel.XMLEANRequestOnly_async("1", "5", xhotel.URL, xhotel.UserId, xhotel.UserName, xhotel.PassWord, xhotel.SearchTimeOut, SearchPara, EANCurrencyFactor, Convert.ToInt32(EXPSearchConfig[k].Noofrecords), Convert.ToString(EXPDetails[0].version), Ids, EchoToken, Platform, Convert.ToString(EXPDetails[0].SupplierName), XMLCOUNTRYCode, XMLCITY, ISPKG);
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
                                        objrq.Request = XMLEANRequestOnly_async(SearchByOneID, "1", "5", xhotel.URL, xhotel.UserId, xhotel.UserName, xhotel.PassWord, xhotel.SearchTimeOut, SearchPara, Convert.ToInt32(EXPSearchConfig[k].Noofrecords), Convert.ToString(EXPDetails[0].version), Ids, EchoToken, Platform, Convert.ToString(EXPDetails[0].SupplierName), XMLCOUNTRYCode, XMLCITY, ISPKG, AgentCode);
                                        objrq.Response = ""; RQRS.Add(objrq);
                                    }
                                    xhotel.RQRS = RQRS;
                                    xhotel.ReturnRooms = 5;
                                    xhotel.TechName = "Expedia";
                                    #endregion
                                    AllTask.Add(xhotel);
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

        public string XMLEANRequestOnly_async(bool SearchByOneID, string MinStar, string MaxStar, string WSUrl, string CID, string Sign, string ApiKey, Int32 SearchTimeOut, HotelSearchParameter SearchParam, int NoOfRecord, string version, string Ids, string echotoken, string platform, string Suppliername, string XMLCOUNTRYCode, string XMLCITY, bool ISPKG = false, string AgentCode = "", int ReturnRooms = 5, string EANCurrency = "USD")
        {
            try
            {
                string checkin = Convert.ToString(SearchParam.CheckInDate);
                string checkout = Convert.ToString(SearchParam.CheckOutDate);
                int NoOfNight = Convert.ToInt32(SearchParam.NoofNights);
                checkin = checkin.Substring(6, 4) + "-" + checkin.Substring(3, 2) + "-" + checkin.Substring(0, 2);
                checkout = checkout.Substring(6, 4) + "-" + checkout.Substring(3, 2) + "-" + checkout.Substring(0, 2);
                string cacheKey = ""; string cacheLocation = ""; bool moreResultsAvailable = false; int ResultPage = 1;
                string ReqString = GetEANHotelSearchRequest(SearchByOneID, checkin, checkout, NoOfRecord, NoOfNight, SearchParam, Ids, XMLCITY, XMLCOUNTRYCode, MinStar, MaxStar, Suppliername, cacheKey, cacheLocation, moreResultsAvailable, ISPKG, AgentCode, ReturnRooms, EANCurrency);
                return ReqString;
            }
            catch (IOException ex)
            {
                _exception.Error(ex);
            }
            catch (Exception ex)
            {
                _exception.Error(ex);

            }
            return "";
        }

        #region Comment Code
        //public string GetEANHotelSearchRequest_OLD(bool SearchByOneID, string checkin, string checkout, int NoOfRecord, int NoOfNight, HotelSearchParameter SearchParam, string Ids, string XMLCITY, string XMLCOUNTRYCode, string MinStar, string MaxStar, string SupplierName, string cacheKey, string cacheLocation, bool moreResultsAvailable, bool ISPKG = false, string AgentCode = "")
        //{
        //    try
        //    {
        //        StringBuilder JsonRQ = new StringBuilder();
        //        JsonRQ.Append("checkin=" + checkin + "");
        //        JsonRQ.Append("&checkout=" + checkout + "");
        //        JsonRQ.Append("&currency=" + _supplierConfig.Expedia.EANCurrency + "");
        //        JsonRQ.Append("&language=" + _supplierConfig.Expedia.EANLanguage + "");
        //        JsonRQ.Append("&country_code=" + XMLCOUNTRYCode + "");
        //        //JsonRQ.Append("&country_code=GB");
        //        //Ids = "119508,16150,19825,23504,24180,4285,7606,11715,7989,119508,4149877";
        //        Ids = Ids.Replace(",", "&property_id=");
        //        for (int room = 0; room < SearchParam.Rooms.Count; room++)
        //        {
        //            int Adultcount = Convert.ToInt32(SearchParam.Rooms[room].NoofAdults);
        //            int Childcount = Convert.ToInt32(SearchParam.Rooms[room].NoofChild);
        //            if (Childcount > 0)
        //            {
        //                switch (Childcount)
        //                {
        //                    case 1:
        //                        JsonRQ.Append("&occupancy=" + Adultcount + "-" + Convert.ToString(SearchParam.Rooms[room].Child1Age));
        //                        break;
        //                    case 2:
        //                        JsonRQ.Append("&occupancy=" + Adultcount + "-" + Convert.ToString(SearchParam.Rooms[room].Child1Age) + "," + Convert.ToString(SearchParam.Rooms[room].Child2Age));
        //                        break;
        //                }
        //            }
        //            else
        //                JsonRQ.Append("&occupancy=" + Adultcount);
        //        }
        //        //JsonRQ.Append("&sales_channel=website");
        //        JsonRQ.Append("&sales_channel=agent_tool");
        //        JsonRQ.Append("&property_id=" + Ids + "");
        //        if (ISPKG)
        //            JsonRQ.Append("&sales_environment=hotel_package");
        //        else
        //            JsonRQ.Append("&sales_environment=hotel_only");
        //        //JsonRQ.Append("&filter=refundable");
        //        JsonRQ.Append("&include=all_rates");
        //        //JsonRQ.Append("&rate_option=net_rates");
        //        if (SupplierName.ToLower() == "ean_b2bv3" || SupplierName.ToLower() == "ean_pkgv3")
        //        {
        //            //JsonRQ.Append("&rate_option=net_rates");
        //            JsonRQ.Append("&rate_option=member");
        //        }
        //        else
        //        {
        //            JsonRQ.Append("&rate_option=closed_user_group");
        //        }
        //        JsonRQ.Append("&sort_type=preferred");
        //        //if (Ids == string.Empty)
        //        //    JsonRQ.Append("&city=" + XMLCITY + "");
        //        //else
        //        //    JsonRQ.Append("&hotelIdList=" + Ids + "");
        //        JsonRQ.Append("&billing_terms=EAC");
        //        JsonRQ.Append("&payment_terms=0");
        //        JsonRQ.Append("&partner_point_of_sale=B2B_SA_PKG_MOD");
        //        if (SearchParam.Filters.IsShowRooms == 0)
        //            JsonRQ.Append("&rate_plan_count=1");
        //        else
        //        {
        //            if (SearchByOneID)
        //            {
        //                JsonRQ.Append("&rate_plan_count=200"); // added on 20-JUN-2024 as per mustafa sir
        //            }
        //            else
        //            {
        //                if (AgentCode.ToLower() == "nigm" || AgentCode.ToLower() == "amtj")
        //                    JsonRQ.Append("&rate_plan_count=10");
        //                else
        //                    JsonRQ.Append("&rate_plan_count=10");
        //            }
        //        }
        //        JsonRQ.Append("&platform_name=add654cd-ad7d-43c6-87f0-c43c36416126");
        //        // JsonRQ.Append("&includeSurrounding=false");
        //        //JsonRQ.Append("&confirmationExtras='GROSS_PROFIT_ONLINE,GROSS_PROFIT_OFFLINE'");
        //        //if (SupplierName.ToLower() == "expedia_pkg")
        //        //JsonRQ.Append("&rateType=MerchantPackage");
        //        //if (moreResultsAvailable == true)
        //        //{
        //        //    JsonRQ.Append("&cacheKey=" + cacheKey + "");
        //        //    JsonRQ.Append("&cacheLocation=" + cacheLocation + "");
        //        //}
        //        return JsonRQ.ToString();
        //    }
        //    catch (Exception Ex)
        //    {
        //        _exception.Error(Ex);
        //        return "";
        //    }
        //}

        #endregion

        public string GetEANHotelSearchRequest(bool SearchByOneID, string checkin, string checkout, int NoOfRecord, int NoOfNight, HotelSearchParameter SearchParam, string Ids, string XMLCITY, string XMLCOUNTRYCode, string MinStar, string MaxStar, string SupplierName, string cacheKey, string cacheLocation, bool moreResultsAvailable, bool ISPKG = false, string AgentCode = "", int ReturnRooms = 10, string EANCurrency = "USD") //040225 - ReturnRooms
        {
            try
            {
                StringBuilder JsonRQ = new StringBuilder();
                JsonRQ.Append("checkin=" + checkin + "");
                JsonRQ.Append("&checkout=" + checkout + "");
                //JsonRQ.Append("&currency=" + _supplierConfig.Expedia.EANCurrency + "");
                JsonRQ.Append("&currency=" + EANCurrency + "");
                JsonRQ.Append("&language=" + _supplierConfig.Expedia.EANLanguage + "");
                JsonRQ.Append("&country_code=" + XMLCOUNTRYCode + "");
                Ids = Ids.Replace(",", "&property_id=");
                for (int room = 0; room < SearchParam.Rooms.Count; room++)
                {
                    int Adultcount = Convert.ToInt32(SearchParam.Rooms[room].NoofAdults);
                    int Childcount = Convert.ToInt32(SearchParam.Rooms[room].NoofChild);
                    if (Childcount > 0)
                    {
                        switch (Childcount)
                        {
                            case 1:
                                JsonRQ.Append("&occupancy=" + Adultcount + "-" + Convert.ToString(SearchParam.Rooms[room].Child1Age));
                                break;
                            case 2:
                                JsonRQ.Append("&occupancy=" + Adultcount + "-" + Convert.ToString(SearchParam.Rooms[room].Child1Age) + "," + Convert.ToString(SearchParam.Rooms[room].Child2Age));
                                break;
                        }
                    }
                    else
                        JsonRQ.Append("&occupancy=" + Adultcount);
                }
                JsonRQ.Append("&sales_channel=agent_tool");
                JsonRQ.Append("&property_id=" + Ids + "");
                if (ISPKG)
                    JsonRQ.Append("&sales_environment=" + _supplierConfig.Expedia.EAN_Sales_Environment_Hotel_package);
                else
                    JsonRQ.Append("&sales_environment=" + _supplierConfig.Expedia.EAN_Sales_Environment_Hotel_Only);
                //JsonRQ.Append("&include=" + _supplierConfig.Expedia.EAN_Include_All_Rates);
                if (true)// (SupplierName.ToLower() == "ean_b2bv3" || SupplierName.ToLower() == "ean_pkgv3")
                {
                    JsonRQ.Append("&rate_option=" + _supplierConfig.Expedia.EAN_Rate_Option_Member);
                }
                else
                {
                    JsonRQ.Append("&rate_option=" + _supplierConfig.Expedia.EAN_Rate_Option_Closed_User_Group);
                }
                //JsonRQ.Append("&sort_type=" + _supplierConfig.Expedia.EAN_Sort_Type_Preferred);
                //JsonRQ.Append("&billing_terms=" + _supplierConfig.Expedia.EAN_Billing_Terms_EAC);
                JsonRQ.Append("&billing_terms=");
                if (SupplierName.ToLower() == "ean_pkgv3_retail") //Added on 29-Oct-2025
                {
                    JsonRQ.Append("&payment_terms=" + _supplierConfig.Expedia.EAN_Payment_Terms_2);
                    JsonRQ.Append("&partner_point_of_sale=" + _supplierConfig.Expedia.EAN_Partner_Point_Of_Sale_COMP);
                }
                else
                {
                    JsonRQ.Append("&payment_terms=" + _supplierConfig.Expedia.EAN_Payment_Terms_0);
                    JsonRQ.Append("&partner_point_of_sale=" + _supplierConfig.Expedia.EAN_Partner_Point_Of_Sale);
                }
                if (SearchParam.Filters.IsShowRooms == 0)
                {
                    if (ReturnRooms > 0)
                        JsonRQ.Append("&rate_plan_count=" + ReturnRooms);
                    else
                        JsonRQ.Append("&rate_plan_count=" + _supplierConfig.Expedia.EAN_Rate_Plan_Count_1);

                }
                else
                {
                    if (SearchByOneID)
                    {
                        if (ReturnRooms > 0)
                            JsonRQ.Append("&rate_plan_count=" + ReturnRooms); // added on 20-JUN-2024 as per mustafa sir //change to 250 As per Mustfa sir 16 OCT 2025
                        else
                            JsonRQ.Append("&rate_plan_count=" + _supplierConfig.Expedia.EAN_Rate_Plan_Count_250); // added on 20-JUN-2024 as per mustafa sir //change to 250 As per Mustfa sir 16 OCT 2025
                    }
                    else
                    {
                        if (AgentCode.ToLower() == "nigm" || AgentCode.ToLower() == "amtj")
                            JsonRQ.Append("&rate_plan_count=" + _supplierConfig.Expedia.EAN_Rate_Plan_Count_10);
                        else
                        {
                            if (ReturnRooms > 0) //040225
                                JsonRQ.Append("&rate_plan_count=" + ReturnRooms); //040225
                            else
                                JsonRQ.Append("&rate_plan_count=" + _supplierConfig.Expedia.EAN_Rate_Plan_Count_10);

                        }
                    }
                }
                JsonRQ.Append("&platform_name=" + _supplierConfig.Expedia.EAN_Platform_Name);
                return JsonRQ.ToString();
            }
            catch (Exception Ex)
            {
                _ = _exception.Error(Ex);
                return "";
            }
        }


        #region OLD Code

        //public async Task<List<Options>> ean_HotelSearchWithRooms_OLD(HotelX ItemEN, CommonSearch commonSearch, string xmlcity, string xmlcountry, HotelSearchParameter SearchParam, double ExpediaCurrencyFactor,DataTable dtROE, string Suppliername, string ResponseData, string CID, string Currency, DataRow[] ExpDetail, string xmlhotelcode, string platform, string echotoken, string Sign, string ApiKey, string Version, string URL, string Provider, string ProccessCutOffTime, int processTimeout, string ResProccessTimerLogActive, string ResProccessTimeValueforSaveLog, string StoreSupplierResErrorLog)
        //{
        //    System.Diagnostics.Stopwatch timer_room_ean = new System.Diagnostics.Stopwatch();
        //    timer_room_ean.Start();

        //    List<Options> options = new List<Options>();

        //    try
        //    {
        //        #region DataTable
        //        DataTable DtHotel = _commonMethod.GenerateDataTableNewV2();
        //        #endregion
        //        if (ResponseData != string.Empty)
        //        {
        //            #region PriceOnly RS
        //            //if (ResponseData.ToLower().Contains("eanwserror") || ResponseData.Trim() == "")
        //            //{
        //            //    ItemEN.ResponseType = "NoCount";
        //            //    return options;
        //            //}
        //            //DataSet Dsean = Common.GetDataSetFromXMLString(ResponseData);
        //            //string Provider = "ean";
        //            List<RootObject_EAN2> Root = JsonConvert.DeserializeObject<List<RootObject_EAN2>>(ResponseData);
        //            #endregion
        //            #region Pax Occupancy
        //            int AdultsOcc = 0;
        //            int ChildOcc = 0;
        //            int TotalAdult = 0;
        //            int TotalChild = 0;

        //            for (int a = 0; a < SearchParam.Rooms.Count; a++)
        //            {
        //                AdultsOcc = SearchParam.Rooms[a].NoofAdults;
        //                ChildOcc = SearchParam.Rooms[a].NoofChild;
        //                TotalAdult = TotalAdult + AdultsOcc;
        //                TotalChild = TotalChild + ChildOcc;
        //            }
        //            #endregion
        //            #region EAN Occupancy Block
        //            List<string> Occupancy1 = new List<string>();
        //            var resultEAN = SearchParam.Rooms.GroupBy(x => new { x.RoomName, x.NoofAdults, x.NoofChild, x.Child1Age, x.Child2Age }).Select(n => new { RoomName = n.Key.RoomName, NoofAdults = n.Key.NoofAdults, NoofChild = n.Key.NoofChild, ChildAge = n.ToList(), Count = n.Count() });
        //            foreach (var item in resultEAN)//For each Item
        //            {
        //                string TotalOcc = "";
        //                int ADT = 0;
        //                string CHD = "";
        //                ADT = item.NoofAdults;
        //                switch (item.NoofChild)
        //                {
        //                    case 0:
        //                        break;
        //                    case 1:
        //                        CHD = "-" + item.ChildAge[0].Child1Age;
        //                        break;
        //                    case 2:
        //                        CHD = "-" + item.ChildAge[0].Child1Age;
        //                        CHD = CHD + "," + item.ChildAge[0].Child2Age;
        //                        break;
        //                }
        //                TotalOcc = ADT.ToString() + CHD;
        //                Occupancy1.Add(TotalOcc + "$$$" + item.Count);
        //            }
        //            #endregion
        //            for (int h = 0; h < Root.Count; h++)
        //            {

        //                ItemEN.ResponseType = "Success";
        //                //Check Time If High then return NoCount
        //                if (ProccessCutOffTime == "1" && timer_room_ean.ElapsedMilliseconds > processTimeout)
        //                {
        //                    //timer_room_ean.Stop();
        //                    break;
        //                }
        //                //--end
        //                string ItemCode = Root[h].property_id;
        //                #region "When not Need Room"  
        //                // add hotel code from price only and room code blank here
        //                if (SearchParam.Filters.IsShowRooms == 0)
        //                {
        //                    #region "Hotel Code Block "
        //                    double TotalCharges = 0;
        //                    {
        //                        #region Add by jay


        //                        double MainPrice = 0;
        //                        double FinalPrice = 0;
        //                        for (int j = 0; j < Convert.ToInt32(Root[h].rooms.Count); j++)
        //                        {
        //                            for (int k = 0; k < Convert.ToInt32(Root[h].rooms[j].rates.Count); k++)
        //                            {
        //                                object Occupancy = Root[h].rooms[j].rates[k].occupancy_pricing;
        //                                JToken[] asdad1 = Newtonsoft.Json.Linq.JToken.Parse(Occupancy.ToString()).ToArray();
        //                                FinalPrice = 0;
        //                                for (int x = 0; x < Occupancy1.Count; x++)
        //                                {
        //                                    string[] spitid = Occupancy1[x].Split(new String[] { "$$$" }, StringSplitOptions.None);
        //                                    foreach (JToken item in asdad1)
        //                                    {
        //                                        #region DataRaw and Variable Declaration
        //                                        double OurProfit = 0;
        //                                        JProperty questionAnswerDetails = ((JProperty)item);

        //                                        if (questionAnswerDetails.Name != spitid[0])
        //                                            continue;
        //                                        else
        //                                        {
        //                                            ng acc = JsonConvert.DeserializeObject<ng>(Convert.ToString(questionAnswerDetails.Value));

        //                                            double Price = Convert.ToDouble(acc.totals.inclusive.billable_currency.value) * Convert.ToInt32(spitid[1]);
        //                                            if (acc.totals.gross_profit == null || acc.totals.gross_profit.billable_currency == null || acc.totals.gross_profit.billable_currency.value == null)//added on 30JAN2024
        //                                            {
        //                                                continue;//2-2-2024
        //                                            }
        //                                            //double Profit = Convert.ToDouble(acc.totals.marketing_fee.billable_currency.value) * Convert.ToInt32(spitid[1]);
        //                                            double Profit = Convert.ToDouble(acc.totals.gross_profit.billable_currency.value) * Convert.ToInt32(ItemEN.EanProfit) / 100 * Convert.ToInt32(spitid[1]);
        //                                            if (Convert.ToString(acc.totals.inclusive.billable_currency.currency).ToLower() != "usd")
        //                                            {
        //                                                continue;
        //                                            }
        //                                            else
        //                                                Price = Price * ExpediaCurrencyFactor;

        //                                            FinalPrice += Price;

        //                                            if (Convert.ToString(acc.totals.marketing_fee.billable_currency.currency).ToLower() != "usd")
        //                                            {
        //                                                continue;
        //                                            }
        //                                            else
        //                                                Profit = Profit * ExpediaCurrencyFactor;
        //                                            OurProfit += Profit;
        //                                            FinalPrice = FinalPrice - OurProfit;
        //                                            #endregion
        //                                        }
        //                                    }
        //                                }
        //                                if (MainPrice == 0)
        //                                    MainPrice = FinalPrice;
        //                                else
        //                                {
        //                                    if (MainPrice > FinalPrice)
        //                                        MainPrice = FinalPrice;
        //                                }
        //                            }
        //                        }
        //                        #endregion
        //                        DataRow drXmlHotel = DtHotel.NewRow();
        //                        drXmlHotel["XMLHotelCode"] = ItemCode;
        //                        drXmlHotel["Reviews"] = 0;
        //                        drXmlHotel["OfficeID"] = 0;
        //                        drXmlHotel["ContractID"] = CID;
        //                        drXmlHotel["IsShowSupplier"] = false;
        //                        drXmlHotel["OwnSystemHotelId"] = 0;
        //                        drXmlHotel["Favorite"] = 0;
        //                        drXmlHotel["XmlHotelCity"] = xmlcity;
        //                        drXmlHotel["XMLCountry"] = xmlcountry;
        //                        drXmlHotel["XMLCity"] = xmlcity;
        //                        drXmlHotel["XMLCode"] = xmlcountry;
        //                        drXmlHotel["SupplierName"] = Suppliername;
        //                        drXmlHotel["BookingStatus"] = "Available";
        //                        drXmlHotel["SupplierCurrency"] = Currency;
        //                        drXmlHotel["FinalSellingPrice"] = FinalPrice;
        //                        drXmlHotel["SellingPrice"] = FinalPrice;
        //                        DtHotel.Rows.Add(drXmlHotel);
        //                        TotalCharges = FinalPrice;
        //                        #endregion
        //                        if (TotalCharges > 0)
        //                        {
        //                            #region "Room code Block"
        //                            bool willAddOrNot = true;
        //                            Int64 RaynaID = 0;
        //                            Int64 GiataId = 0;
        //                            int Rating = 0;
        //                            Int64 OwnsystemHotelId = 0;
        //                            double MinPrice = TotalCharges;
        //                            _markup.GetHotelDataCommon(ref MinPrice, ref willAddOrNot, ref OwnsystemHotelId, ref RaynaID, ref GiataId, ref Rating, ref drXmlHotel, commonSearch.AgentId, commonSearch.TariffID, commonSearch.dsAgentDetails, commonSearch.supplierGiataMaster, SearchParam, commonSearch.dtCountryCityMarkup, commonSearch.B2CMarkup, commonSearch.IsParentId, commonSearch.SubAgentMarkup, commonSearch.SearchByHotel, commonSearch.HotelIDS);
        //                            if (willAddOrNot)
        //                            {
        //                                #region "Room Logic"
        //                                DataTable dt_HotelRoom = _commonMethod.GenerateRoomtypeDataTableNewV2();
        //                                DataTable dt_Cancellation = _commonMethod.GenerateCancellationDataTableNewV2();
        //                                DataTable dt_RateBreackUp = _commonMethod.GenerateoomRateBreackUpDataTableNewV2();
        //                                #endregion
        //                                decimal LowestPrice = Convert.ToDecimal(MinPrice).CalculatePrice(commonSearch.tokenData, commonSearch.advancedOptions, _supplierConfig.Expedia.EANCurrency);
        //                                Options ops = (await _supplierRes.GetCommonRoomForSuppliers(GiataId, commonSearch.tokenData, commonSearch.advancedOptions, Convert.ToDouble(LowestPrice), drXmlHotel, dt_HotelRoom, dt_Cancellation, dt_RateBreackUp, SearchParam));
        //                                if (ops != null)
        //                                    options.Add(ops);
        //                            }
        //                            else
        //                            {

        //                            }
        //                            #endregion
        //                        }
        //                    }
        //                }
        //                #endregion
        //                #region "When Need Rooms"
        //                //add Hotel code blank and rooms code in room block
        //                else
        //                {
        //                    double FinalMinPrice = 0;
        //                    #region Hotels
        //                    //object Occupancy = Root[h].rooms[0].rates[0].occupancy_pricing;
        //                    //JToken[] asdad = Newtonsoft.Json.Linq.JToken.Parse(Occupancy.ToString()).ToArray();

        //                    #region "Hotel Block here"

        //                    //var aaa = asdad[0];
        //                    //string ItemCode = Root[i].property_id;
        //                    double FinalPrice = 1;
        //                    //foreach (JToken item in asdad)
        //                    //{
        //                    //    JProperty questionAnswerDetails = ((JProperty)item);
        //                    //    string Name = questionAnswerDetails.Name;
        //                    //    string value = Convert.ToString(questionAnswerDetails.Value);
        //                    //    ng acc = JsonConvert.DeserializeObject<ng>(value);
        //                    //    double Price = Convert.ToDouble(acc.totals.inclusive.billable_currency.value);
        //                    //    if (FinalPrice == 0)
        //                    //        FinalPrice = Price;
        //                    //    else
        //                    //    {
        //                    //        if (FinalPrice > Price)
        //                    //            FinalPrice = Price;
        //                    //    }
        //                    //}
        //                    DataRow drXmlHotel = DtHotel.NewRow();
        //                    drXmlHotel["XMLHotelCode"] = ItemCode;
        //                    //drXmlHotel["HotelName"] = "";
        //                    //drXmlHotel["HotelFrontImage"] = "";
        //                    //drXmlHotel["Location"] = "";
        //                    //drXmlHotel["lat"] = "";
        //                    //drXmlHotel["longitute"] = "";
        //                    drXmlHotel["Rating"] = 0;
        //                    drXmlHotel["Reviews"] = 0;
        //                    //drXmlHotel["Description"] = "";
        //                    // drXmlHotel["EssentialInformation"] = "";
        //                    drXmlHotel["OfficeID"] = 0;
        //                    drXmlHotel["ContractID"] = CID;
        //                    drXmlHotel["IsShowSupplier"] = false;
        //                    drXmlHotel["OwnSystemHotelId"] = 0;
        //                    //drXmlHotel["Address"] = "";
        //                    drXmlHotel["Favorite"] = 0;
        //                    drXmlHotel["XmlHotelCity"] = xmlcity;
        //                    drXmlHotel["XMLCountry"] = xmlcountry;
        //                    drXmlHotel["XMLCity"] = xmlcity;
        //                    drXmlHotel["XMLCode"] = xmlcountry;
        //                    drXmlHotel["SupplierName"] = Suppliername;
        //                    drXmlHotel["BookingStatus"] = "Available";
        //                    drXmlHotel["SupplierCurrency"] = Currency;
        //                    drXmlHotel["FinalSellingPrice"] = FinalPrice;
        //                    drXmlHotel["SellingPrice"] = FinalPrice;
        //                    DtHotel.Rows.Add(drXmlHotel);
        //                    FinalMinPrice = FinalPrice;
        //                    #endregion
        //                    #region "GetRooms for this hotel"
        //                    bool willAddOrNot = true;
        //                    Int64 RaynaID = 0;
        //                    Int64 GiataId = 0;
        //                    int RatingHotel = 0;
        //                    Int64 OwnsystemHotelId = 0;
        //                    double MinPrice = 1;

        //                    _markup.GetHotelDataCommon(ref MinPrice, ref willAddOrNot, ref OwnsystemHotelId, ref RaynaID, ref GiataId, ref RatingHotel, ref drXmlHotel, commonSearch.AgentId, commonSearch.TariffID, commonSearch.dsAgentDetails, commonSearch.supplierGiataMaster, SearchParam, commonSearch.dtCountryCityMarkup, commonSearch.B2CMarkup, commonSearch.IsParentId, commonSearch.SubAgentMarkup, commonSearch.SearchByHotel, commonSearch.HotelIDS);
        //                    if (willAddOrNot)
        //                    {
        //                        DataTable dtHotelRoom = _commonMethod.GenerateRoomtypeDataTableNewV2();
        //                        DataTable dtCancellation = _commonMethod.GenerateCancellationDataTableNewV2();
        //                        DataTable DtRoomRateBreackUp = _commonMethod.GenerateoomRateBreackUpDataTableNewV2();
        //                        int CutOffDays = CacheMaster.CutOffDays;
        //                        CutOffDays = _commonMethod.GetCutOffDays(commonSearch.dtSuplierMarkup, Provider);
        //                        #region "Room Logic"
        //                        //DataTable XMLTBORoomOnly = Common.GenerateRoomtypeDataTableNewV2();

        //                        //if (SearchParam.Filters.IsShowRooms == 1)
        //                        {
        //                            #region "Hotel Block here"
        //                            double AgentPercentage = 0.0;
        //                            double AgentMarkupAmount = 0.0;
        //                            Boolean isPerce = true;
        //                            bool blnIsShowSupplier = false;
        //                            _markup.getAgetMarkupSetting(Provider, GiataId.ToString(), commonSearch.dtSuplierMarkup, commonSearch.dtMarkupDetails, ref AgentPercentage, ref AgentMarkupAmount, ref blnIsShowSupplier, ref isPerce, commonSearch.AgentId, RatingHotel, SearchParam.CityID, SearchParam.CountryID.ToString(), SearchParam.CheckInDate);
        //                            DataSet dsFinal = new DataSet();
        //                            DataTable dt_HotelRoom = _commonMethod.GenerateRoomtypeDataTableNewV2();
        //                            DataTable dt_Cancellation = _commonMethod.GenerateCancellationDataTableNewV2();
        //                            DataTable dt_RateBreackUp = _commonMethod.GenerateoomRateBreackUpDataTableNewV2();

        //                            //string Sign = Convert.ToString(ExpDetail[0]["UserName"]);
        //                            //string ApiKey = Convert.ToString(ExpDetail[0]["Password"]);
        //                            //string Version = Convert.ToString(ExpDetail[0]["version"]);
        //                            //string URL = Convert.ToString(ExpDetail[0]["WebSiteURL"]);
        //                            string EssInfo = "";// Get_EAN_PropertyContent(ItemCode, platform, echotoken, Sign, ApiKey, Version, URL, Provider);
        //                                                //var result = SearchParam.Rooms.GroupBy(x => new { x.RoomName, x.NoofAdults, x.NoofChild, x.Child1Age, x.Child2Age }).Select(n => new { RoomName = n.Key.RoomName, NoofAdults = n.Key.NoofAdults, NoofChild = n.Key.NoofChild, ChildAge = n.ToList(), Count = n.Count() });

        //                            //if (ResponseData.Contains("eanwserror") || ResponseData.ToString() == "" || ResponseData == null)
        //                            // return options;
        //                            //List<RootObject_EAN2> Root = JsonConvert.DeserializeObject<List<RootObject_EAN2>>(ResponseData);



        //                            //List<string> Occupancy1 = new List<string>();
        //                            //foreach (var item in result)//For each Item
        //                            //{
        //                            //    string TotalOcc = "";
        //                            //    //for (int i = 0; i < item.Count; i++)
        //                            //    {
        //                            //        int ADT = 0;
        //                            //        string CHD = "";
        //                            //        ADT = item.NoofAdults;
        //                            //        switch (item.NoofChild)
        //                            //        {
        //                            //            case 0:
        //                            //                break;
        //                            //            case 1:
        //                            //                CHD = "-" + item.ChildAge[0].Child1Age;
        //                            //                break;
        //                            //            case 2:
        //                            //                CHD = "-" + item.ChildAge[0].Child1Age;
        //                            //                CHD = CHD + "," + item.ChildAge[0].Child2Age;
        //                            //                break;
        //                            //        }
        //                            //        TotalOcc = ADT.ToString() + CHD;
        //                            //        Occupancy1.Add(TotalOcc + "$$$" + item.Count);
        //                            //    }
        //                            //}

        //                            //for (int i = 0; i < Root.Count; i++)
        //                            {
        //                                string Item_Code = Root[h].property_id;
        //                                int Rooms = Convert.ToInt32(Root[h].rooms.Count);
        //                                for (int j = 0; j < Rooms; j++)
        //                                {
        //                                    int Rate = Convert.ToInt32(Root[h].rooms[j].rates.Count);
        //                                    string RoomId = Root[h].rooms[j].id;
        //                                    string RoomName = Root[h].rooms[j].room_name.Replace("-", " ");
        //                                    for (int k = 0; k < Rate; k++)
        //                                    {
        //                                        object Occupancy_Room = Root[h].rooms[j].rates[k].occupancy_pricing;
        //                                        JToken[] asdad_Room = Newtonsoft.Json.Linq.JToken.Parse(Occupancy_Room.ToString()).ToArray();
        //                                        //var aaa = asdad[0];
        //                                        string RateId = Root[h].rooms[j].rates[k].id;
        //                                        //string RatePlamCode = RoomId + "$$$" + RateId + "$$$";
        //                                        //string PromoId = Root[h].rooms[j].rates[k].promo_id;
        //                                        Boolean IsRefundable = Convert.ToBoolean(Root[h].rooms[j].rates[k].refundable);
        //                                        string AmenitiesId = "";
        //                                        string AmenitiesName = "";
        //                                        string TotalAmenities = "";

        //                                        if (Root[h].rooms[j].rates[k].amenities != null)
        //                                        {
        //                                            //int Amenities = Convert.ToInt32(Root[h].rooms[j].rates[k].amenities.Count);
        //                                            //for (int o = 0; o < Amenities; o++)
        //                                            //{
        //                                            //    AmenitiesId = Convert.ToString(Root[h].rooms[j].rates[k].amenities[o].id);
        //                                            //    AmenitiesName = Convert.ToString(Root[h].rooms[j].rates[k].amenities[o].name);

        //                                            //    if (TotalAmenities == string.Empty)
        //                                            //        TotalAmenities = AmenitiesName;
        //                                            //    else
        //                                            //        TotalAmenities = TotalAmenities + " , " + AmenitiesName;

        //                                            //}
        //                                            foreach (var item in Root[h].rooms[j].rates[k].amenities)
        //                                            {
        //                                                //AmenitiesId = Convert.ToString(item.Value.id);
        //                                                AmenitiesName = Convert.ToString(item.Value.name);

        //                                                if (TotalAmenities == string.Empty)
        //                                                    TotalAmenities = AmenitiesName;
        //                                                else
        //                                                    TotalAmenities = TotalAmenities + " , " + AmenitiesName;
        //                                            }
        //                                        }

        //                                        string PromotionId = "";
        //                                        string PromotionName = "";
        //                                        string TotalPromotion = "";

        //                                        string DealId = "";
        //                                        string DealName = "";
        //                                        string TotalDeal = "";

        //                                        //if (Root[h].rooms[j].rates[k].promotions != null)
        //                                        //{
        //                                        //if (Root[h].rooms[j].rates[k].promotions.value_adds != null)
        //                                        //{
        //                                        //    foreach (var item in Root[h].rooms[j].rates[k].promotions.value_adds)
        //                                        //    {
        //                                        //        //PromotionId = Convert.ToString(item.Value.id);
        //                                        //        PromotionName = Convert.ToString(item.Value.description);

        //                                        //        if (TotalPromotion == string.Empty)
        //                                        //            TotalPromotion = PromotionName;
        //                                        //        else
        //                                        //            TotalPromotion = TotalPromotion + " , " + PromotionName;
        //                                        //    }
        //                                        //}

        //                                        //if (Root[h].rooms[j].rates[k].promotions.deal != null)
        //                                        //{
        //                                        //    {
        //                                        //        DealId = Convert.ToString(Root[h].rooms[j].rates[k].promotions.deal.id);
        //                                        //        DealName = Convert.ToString(Root[h].rooms[j].rates[k].promotions.deal.description);

        //                                        //        if (TotalDeal == string.Empty)
        //                                        //            TotalDeal = DealName;
        //                                        //        else
        //                                        //            TotalDeal = TotalDeal + " , " + DealName;
        //                                        //    }
        //                                        //}
        //                                        //}


        //                                        if (Root[h].rooms[j].rates[k].bed_groups != null)
        //                                        {
        //                                            int BedGroup = Root[h].rooms[j].rates[k].bed_groups.Count;
        //                                            string ESS = "";
        //                                            if (BedGroup > 1)
        //                                                ESS = "<br><b>Bed Type is not Guaranteed<b>";
        //                                            string BedName = "";
        //                                            //for (int m = 0; m < BedGroup; m++)
        //                                            foreach (var item1 in Root[h].rooms[j].rates[k].bed_groups)
        //                                            {
        //                                                //string BedGroupMethod = Convert.ToString(Root[h].rooms[j].rates[k].bed_groups[m].links.price_check.method);
        //                                                //string BedGroupLink = Convert.ToString(Root[h].rooms[j].rates[k].bed_groups[m].links.price_check.href);
        //                                                //if (Root[h].rooms[j].rates[k].bed_groups[m].configuration != null)
        //                                                //    BedName = Root[h].rooms[j].rates[k].bed_groups[m].configuration[0].type;
        //                                                //string BedGroupMethod = Convert.ToString(item1.Value.links.price_check.method);
        //                                                string BedGroupLink = Convert.ToString(item1.Value.links.price_check.href);
        //                                                if (item1.Value.description != null)
        //                                                    BedName = item1.Value.description.Replace("-", " ");
        //                                                string RatePlamCode = RoomId + "$$$" + RateId + "$$$" + BedGroupLink + "$$$" + Guid.NewGuid();
        //                                                double MainPrice = 0;
        //                                                double OurProfit = 0;
        //                                                double StrikeTroughMain = 0;
        //                                                string EssentialAdd = "";
        //                                                for (int x = 0; x < Occupancy1.Count; x++)
        //                                                {
        //                                                    string Occ = "";
        //                                                    string Count = "";
        //                                                    string OccID = Occupancy1[x];
        //                                                    string[] spitid = OccID.Split(new String[] { "$$$" }, StringSplitOptions.None);
        //                                                    Occ = spitid[0];
        //                                                    Count = spitid[1];
        //                                                    foreach (JToken item in asdad_Room)
        //                                                    {
        //                                                        #region DataRaw and Variable Declaration
        //                                                        JProperty questionAnswerDetails = ((JProperty)item);
        //                                                        string Name = questionAnswerDetails.Name;

        //                                                        if (Name != Occ)
        //                                                            continue;
        //                                                        else
        //                                                        {
        //                                                            string value = Convert.ToString(questionAnswerDetails.Value);
        //                                                            ng acc = JsonConvert.DeserializeObject<ng>(value);

        //                                                            double Price = Convert.ToDouble(acc.totals.inclusive.billable_currency.value);
        //                                                            //double Profit = Convert.ToDouble(acc.totals.marketing_fee.billable_currency.value);
        //                                                            if (acc.totals.gross_profit == null || acc.totals.gross_profit.billable_currency == null || acc.totals.gross_profit.billable_currency.value == null)//added on 30JAN2024
        //                                                            {
        //                                                                continue;
        //                                                            }
        //                                                            double Profit = Convert.ToDouble(acc.totals.gross_profit.billable_currency.value) * Convert.ToInt32(ItemEN.EanProfit) / 100;
        //                                                            double StrikeTrough = 0;
        //                                                            if (acc.totals.strikethrough != null)
        //                                                            {
        //                                                                StrikeTrough = Convert.ToDouble(acc.totals.strikethrough.billable_currency.value);

        //                                                            }

        //                                                            Price = Price * Convert.ToInt32(Count);
        //                                                            Profit = Profit * Convert.ToInt32(Count);
        //                                                            StrikeTrough = StrikeTrough * Convert.ToInt32(Count);

        //                                                            string Billable_Currency = Convert.ToString(acc.totals.inclusive.billable_currency.currency);
        //                                                            if (Billable_Currency.ToLower() != "usd")
        //                                                            {
        //                                                                double CurrencyFactor1 = _commonMethod.GetROEByCurrency(Billable_Currency, dtROE);//280125

        //                                                                //DataTable dtInrCurrency = await _commonMethod.GetCountryCurrency(Billable_Currency);
        //                                                                //double CurrencyFactor1 = Convert.ToDouble(dtInrCurrency.Rows[0]["AmtToExch"]);
        //                                                                Price = Price * CurrencyFactor1;
        //                                                            }
        //                                                            else
        //                                                                Price = Price * ExpediaCurrencyFactor;

        //                                                            MainPrice += Price;


        //                                                            string Billable_CurrencyProfit = Convert.ToString(acc.totals.marketing_fee.billable_currency.currency);
        //                                                            if (Billable_CurrencyProfit.ToLower() != "usd")
        //                                                            {
        //                                                                double CurrencyFactor2 = _commonMethod.GetROEByCurrency(Billable_CurrencyProfit, dtROE);//280125

        //                                                                //DataTable dtInrCurrency = await _commonMethod.GetCountryCurrency(Billable_CurrencyProfit);
        //                                                                //double CurrencyFactor2 = Convert.ToDouble(dtInrCurrency.Rows[0]["AmtToExch"]);
        //                                                                Profit = Profit * CurrencyFactor2;
        //                                                            }
        //                                                            else
        //                                                                Profit = Profit * ExpediaCurrencyFactor;

        //                                                            OurProfit += Profit;

        //                                                            if (acc.totals.strikethrough != null)
        //                                                            {
        //                                                                string StrikeTrough_Currency = Convert.ToString(acc.totals.strikethrough.billable_currency.currency);
        //                                                                if (StrikeTrough_Currency.ToLower() != "usd")
        //                                                                {
        //                                                                    double CurrencyFactor4 = _commonMethod.GetROEByCurrency(StrikeTrough_Currency, dtROE);//280125

        //                                                                    //DataTable dtInrCurrency = await _commonMethod.GetCountryCurrency(StrikeTrough_Currency);
        //                                                                    //double CurrencyFactor4 = Convert.ToDouble(dtInrCurrency.Rows[0]["AmtToExch"]);
        //                                                                    StrikeTrough = StrikeTrough * CurrencyFactor4;
        //                                                                }
        //                                                                else
        //                                                                    StrikeTrough = StrikeTrough * ExpediaCurrencyFactor;
        //                                                                StrikeTroughMain += StrikeTrough;

        //                                                            }


        //                                                            if (acc.fees != null)
        //                                                            {
        //                                                                if (acc.fees.mandatory_fee != null)
        //                                                                {
        //                                                                    string FeesDesc1 = "";
        //                                                                    string FeesCurrency1 = "";
        //                                                                    double FeesPrice1 = 0;

        //                                                                    FeesDesc1 = "<b>Mandatory Fees</b>";

        //                                                                    FeesPrice1 += Convert.ToDouble(acc.fees.mandatory_fee.billable_currency.value);
        //                                                                    FeesCurrency1 = Convert.ToString(acc.fees.mandatory_fee.billable_currency.currency);

        //                                                                    EssentialAdd = FeesDesc1 + "<b> : </b>" + FeesPrice1 + " (" + FeesCurrency1 + ") ";

        //                                                                }
        //                                                                if (acc.fees.mandatory_tax != null)
        //                                                                {
        //                                                                    string FeesDesc2 = "";
        //                                                                    string FeesCurrency2 = "";
        //                                                                    double FeesPrice2 = 0;

        //                                                                    FeesDesc2 = "<b>Mandatory Taxes</b>";

        //                                                                    FeesPrice2 += Convert.ToDouble(acc.fees.mandatory_tax.billable_currency.value);
        //                                                                    FeesCurrency2 = Convert.ToString(acc.fees.mandatory_tax.billable_currency.currency);

        //                                                                    if (EssentialAdd != string.Empty)
        //                                                                        EssentialAdd = " , " + FeesDesc2 + " : " + FeesPrice2 + " (" + FeesCurrency2 + ") ";
        //                                                                    else
        //                                                                        EssentialAdd = FeesDesc2 + "<b> : </b>" + FeesPrice2 + " (" + FeesCurrency2 + ") ";

        //                                                                }
        //                                                                if (acc.fees.resort_fee != null)
        //                                                                {
        //                                                                    string FeesDesc3 = "";
        //                                                                    string FeesCurrency3 = "";
        //                                                                    double FeesPrice3 = 0;

        //                                                                    FeesDesc3 = "<b>Resort Fees</b>";

        //                                                                    FeesPrice3 += Convert.ToDouble(acc.fees.resort_fee.billable_currency.value);
        //                                                                    FeesCurrency3 = Convert.ToString(acc.fees.resort_fee.billable_currency.currency);

        //                                                                    if (EssentialAdd != string.Empty)
        //                                                                        EssentialAdd = " , " + FeesDesc3 + " : " + FeesPrice3 + " (" + FeesCurrency3 + ") ";
        //                                                                    else
        //                                                                        EssentialAdd = FeesDesc3 + " : " + FeesPrice3 + " (" + FeesCurrency3 + ") ";
        //                                                                }
        //                                                            }

        //                                                            if (EssentialAdd != string.Empty)
        //                                                                EssentialAdd = EssentialAdd + "For" + RoomName + " " + BedName;
        //                                                            else
        //                                                                EssentialAdd = "";


        //                                                            #endregion

        //                                                            #region RateBreakUp
        //                                                            double markup = 0;

        //                                                            #endregion

        //                                                        }
        //                                                    }

        //                                                }

        //                                                double VatOnRateBrackUp = 0;
        //                                                double tempmin = MainPrice - OurProfit;
        //                                                tempmin = _markup.ProcessHotelRateBreakupPrice_WithMarkup(1, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), Convert.ToDouble(tempmin), commonSearch.B2CMarkup, commonSearch.IsParentId, isPerce, AgentPercentage, AgentMarkupAmount, commonSearch.SubAgentMarkup, ref VatOnRateBrackUp);

        //                                                //for (int n = 0; n < SearchParam.NoofNights; n++)
        //                                                //{
        //                                                //    DateTime Day = DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null);
        //                                                //    Day = Day.AddDays(n);
        //                                                //    string Days = Convert.ToString(Day);
        //                                                //    string rateWeekColumn = Convert.ToString(Day.Date.DayOfWeek).Substring(0, 3);

        //                                                //    DataRow drEAN_RateBreackUpMarkup = dt_RateBreackUp.NewRow();
        //                                                //    drEAN_RateBreackUpMarkup["SupplierName"] = Provider;
        //                                                //    drEAN_RateBreackUpMarkup["OwnsystemHotelId"] = OwnsystemHotelId;
        //                                                //    drEAN_RateBreackUpMarkup["ContractID"] = ItemCode;
        //                                                //    drEAN_RateBreackUpMarkup["RoomTypeName"] = RoomName + " " + BedName;
        //                                                //    drEAN_RateBreackUpMarkup["RoomTypeId"] = RatePlamCode;
        //                                                //    drEAN_RateBreackUpMarkup["MealId"] = TotalAmenities != "" ? TotalAmenities : "Room Only";
        //                                                //    drEAN_RateBreackUpMarkup["Meal"] = TotalAmenities != "" ? TotalAmenities : "Room Only";
        //                                                //    drEAN_RateBreackUpMarkup["DateOfWeek"] = rateWeekColumn;
        //                                                //    drEAN_RateBreackUpMarkup["Date"] = Days;
        //                                                //    drEAN_RateBreackUpMarkup["Rate"] = tempmin / SearchParam.NoofNights;
        //                                                //    drEAN_RateBreackUpMarkup["RateType"] = "A Rate"; //"WithinEarth Service Charge - " + RoomName + " " + BedName + "(" + Name + ")";
        //                                                //    drEAN_RateBreackUpMarkup["RoomNo"] = 1;
        //                                                //    drEAN_RateBreackUpMarkup["RoomSequenceNo"] = OwnsystemHotelId;
        //                                                //    drEAN_RateBreackUpMarkup["BookingStatus"] = "Available";
        //                                                //    drEAN_RateBreackUpMarkup["SupplierCurrency"] = XMLHotelBookingCommon.EANCurrency;
        //                                                //    drEAN_RateBreackUpMarkup["BuyingPrice"] = tempmin / SearchParam.NoofNights;
        //                                                //    //drEAN_RateBreackUp["TotalNight"] = acc.nightly.Count;
        //                                                //    //drEAN_RateBreackUp["Surcharge"] = (surcharge);
        //                                                //    //drEAN_RateBreackUp["CompanyCharge"] = (totalMarkup);
        //                                                //    //drEAN_RateBreackUp["Total"] = (DateGross);.no
        //                                                //    //drEAN_RateBreackUp["SalesTaxAmount"] = (salestaxAmount);
        //                                                //    //drEAN_RateBreackUp["HotelOccupancyTaxAmount"] = (hotelOccupancyTaxAmount);
        //                                                //    //drEAN_RateBreackUp["HotelFessDescription"] = FeesDesc;
        //                                                //    //drEAN_RateBreackUp["HotelFeeAmount"] = FeesPrice;
        //                                                //    //drEAN_RateBreackUp["RateType"] = "A Rate";
        //                                                //    //drEAN_RateBreackUp["VatdiffAmount"] = VatAmount;
        //                                                //    //if (CancellationText != "")
        //                                                //    //    drEAN_RateBreackUp["CancellationText"] = CancellationText;
        //                                                //    dt_RateBreackUp.Rows.Add(drEAN_RateBreackUpMarkup);
        //                                                //}

        //                                                #region Rooms
        //                                                //Boolean IsRefundable = Convert.ToBoolean(Root[h].rooms[j].rates[k].refundable);

        //                                                DataRow drEANRoom = dtHotelRoom.NewRow();
        //                                                drEANRoom["OwnSystemHotelId"] = OwnsystemHotelId;
        //                                                drEANRoom["RoomTypeName"] = IsRefundable ? RoomName + " " + BedName : RoomName + " " + BedName + "- Non Refundable";
        //                                                drEANRoom["RoomTypeCode"] = RatePlamCode;
        //                                                drEANRoom["MealId"] = TotalAmenities != "" ? TotalAmenities : "Room Only";
        //                                                drEANRoom["MealName"] = TotalAmenities != "" ? TotalAmenities : "Room Only";
        //                                                drEANRoom["ContractID"] = ItemCode;
        //                                                drEANRoom["BoardType"] = TotalAmenities != "" ? TotalAmenities : "Room Only";
        //                                                drEANRoom["RoomTypeId"] = RatePlamCode;
        //                                                drEANRoom["AdultsOccupancy"] = TotalAdult;
        //                                                drEANRoom["ChildOccupancy"] = TotalChild;
        //                                                drEANRoom["MaxExtraBed"] = "0";
        //                                                drEANRoom["RoomNo"] = 1;
        //                                                drEANRoom["RoomSequenceNo"] = Guid.NewGuid();
        //                                                drEANRoom["Type"] = "";
        //                                                drEANRoom["BookingStatus"] = "Available";
        //                                                drEANRoom["UniqueNo"] = OwnsystemHotelId;
        //                                                drEANRoom["OfferDescription"] = "";
        //                                                drEANRoom["OfferCode"] = "";
        //                                                drEANRoom["EssentialInformation"] = EssInfo + "<br>" + EssentialAdd + ESS;

        //                                                drEANRoom["CompanyBuyingPriceExpedia"] = MainPrice;
        //                                                drEANRoom["GrossProfitOnline"] = 0;
        //                                                double discountprice = 0;
        //                                                discountprice = StrikeTroughMain;
        //                                                if (discountprice > (MainPrice))
        //                                                {
        //                                                    discountprice = _markup.ProcessHotelRateBreakupPrice_WithMarkup(1, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), Convert.ToDouble(discountprice), commonSearch.B2CMarkup, commonSearch.IsParentId, isPerce, AgentPercentage, AgentMarkupAmount, commonSearch.SubAgentMarkup, ref VatOnRateBrackUp);

        //                                                    drEANRoom["GrossWitohutDiscount"] = discountprice;
        //                                                }

        //                                                MainPrice = MainPrice - OurProfit;

        //                                                double ItemPrice = Convert.ToDouble(MainPrice);

        //                                                drEANRoom["OurProfit"] = OurProfit;
        //                                                drEANRoom["CurrencyFactor"] = ExpediaCurrencyFactor;

        //                                                _markup.ProcessHotelRoom_WithMarkup(ExpediaCurrencyFactor, _supplierConfig.Expedia.EANCurrency, false, true, SearchParam.NoofNights, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), SearchParam.CheckInDate, SearchParam.CheckOutDate, ref ItemPrice, ref drEANRoom, isPerce, AgentPercentage, AgentMarkupAmount, commonSearch.SubAgentMarkup, commonSearch.B2CMarkup, commonSearch.IsParentId);

        //                                                drEANRoom["SupplierName"] = Provider;
        //                                                dtHotelRoom.Rows.Add(drEANRoom);
        //                                                #endregion



        //                                                if (Provider.ToLower() == "ean_b2b" || Provider.ToLower() == "ean_pkg")
        //                                                {
        //                                                    #region Cancellation Policy

        //                                                    if (IsRefundable)
        //                                                    {
        //                                                        DateTime Start = DateTime.Now.Date;
        //                                                        DateTime End = DateTime.Now.Date;
        //                                                        int Night = 0;
        //                                                        double CancellationAmount = 0;
        //                                                        double CancellationPecentage = 0;
        //                                                        double CancellationCharge = 0;
        //                                                        Currency = _supplierConfig.Expedia.EANCurrency;
        //                                                        if (Root[h].rooms[j].rates[k].cancel_penalties != null)
        //                                                        {
        //                                                            int CancellationPolicy = Convert.ToInt32(Root[h].rooms[j].rates[k].cancel_penalties.Count);
        //                                                            for (int l = 0; l < CancellationPolicy; l++)
        //                                                            {
        //                                                                Start = Convert.ToDateTime(Root[h].rooms[j].rates[k].cancel_penalties[l].start);
        //                                                                End = Convert.ToDateTime(Root[h].rooms[j].rates[k].cancel_penalties[l].end);
        //                                                                Currency = Convert.ToString(Root[h].rooms[j].rates[k].cancel_penalties[l].currency);

        //                                                                if (Root[h].rooms[j].rates[k].cancel_penalties[l].nights != null)
        //                                                                {
        //                                                                    Night = Convert.ToInt32(Root[h].rooms[j].rates[k].cancel_penalties[l].nights);
        //                                                                    CancellationCharge = MainPrice;// (MainPrice / SearchParam.NoofNights) * Night;
        //                                                                }
        //                                                                else if (Root[h].rooms[j].rates[k].cancel_penalties[l].amount != null)
        //                                                                {
        //                                                                    CancellationAmount = Convert.ToDouble(Root[h].rooms[j].rates[k].cancel_penalties[l].amount);
        //                                                                    CancellationCharge = CancellationCharge + CancellationAmount;
        //                                                                }
        //                                                                else if (Root[h].rooms[j].rates[k].cancel_penalties[l].percent != null)
        //                                                                {
        //                                                                    double CanelPercent = 0;
        //                                                                    CancellationPecentage = Convert.ToDouble(Root[h].rooms[j].rates[k].cancel_penalties[l].percent.Replace("%", ""));
        //                                                                    CanelPercent = MainPrice * CancellationPecentage;
        //                                                                    CancellationCharge = CancellationCharge + CanelPercent;

        //                                                                }
        //                                                            }
        //                                                        }

        //                                                        if (CancellationCharge > MainPrice)
        //                                                            CancellationCharge = MainPrice;


        //                                                        double CancelCharge = CancellationCharge;

        //                                                        DateTime CancelDate = DateTime.Now.Date;
        //                                                        CancelDate = Convert.ToDateTime(Start);
        //                                                        DateTime NowDate = DateTime.Now.Date;
        //                                                        if (CancelDate <= DateTime.Now.Date)
        //                                                            CancelDate = DateTime.Now.Date;
        //                                                        else
        //                                                            CancelDate = CancelDate.AddDays(CutOffDays).Date;

        //                                                        if (CancelDate.AddDays(-1) <= DateTime.Now.Date)
        //                                                            CancelDate = DateTime.Now.Date;
        //                                                        #region Vat
        //                                                        double VatOnPolicy = 0;
        //                                                        double MainAgentCharge = CancelCharge;
        //                                                        double CompanyChargeAmount = CancelCharge;
        //                                                        CancelCharge = _markup.ProcessHotelCancelationPrice_WithMarkup(ref MainAgentCharge, SearchParam.Rooms.Count * SearchParam.NoofNights, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), CancelCharge, isPerce, commonSearch.B2CMarkup, AgentPercentage, AgentMarkupAmount, commonSearch.IsParentId, commonSearch.SubAgentMarkup, ref VatOnPolicy);
        //                                                        #endregion
        //                                                        if (CancelDate.Date < DateTime.Now.Date)
        //                                                            CancelDate = DateTime.Now.Date;

        //                                                        if (NowDate < CancelDate && CancelDate.Date > DateTime.Now.Date)
        //                                                        {
        //                                                            double zero = 0;

        //                                                            dtCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref zero, dtCancellation.NewRow(), ExpediaCurrencyFactor, Currency, Provider, Convert.ToString(OwnsystemHotelId), Convert.ToString(1),
        //                                                        Convert.ToString(RatePlamCode), IsRefundable ? RoomName + " " + BedName : RoomName + " " + BedName + "- Non Refundable", TotalAmenities != "" ? TotalAmenities : "Room Only", DateTime.Now.Date, CancelDate.AddDays(-1), 0, 0, "", VatOnPolicy, "", "NO", "", "", "", ""));
        //                                                            dtCancellation.AcceptChanges();

        //                                                            dtCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref CompanyChargeAmount, dtCancellation.NewRow(), ExpediaCurrencyFactor, Currency, Provider, Convert.ToString(OwnsystemHotelId), Convert.ToString(1),
        //                                                        Convert.ToString(RatePlamCode), IsRefundable ? RoomName + " " + BedName : RoomName + " " + BedName + "- Non Refundable", TotalAmenities != "" ? TotalAmenities : "Room Only", Convert.ToDateTime(CancelDate), DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null), MainAgentCharge, CancelCharge, "", VatOnPolicy, "", "NO", "", "", "", ""));
        //                                                            dtCancellation.AcceptChanges();
        //                                                        }
        //                                                        else
        //                                                        {
        //                                                            #region Vat
        //                                                            VatOnPolicy = 0;
        //                                                            MainAgentCharge = MainPrice;
        //                                                            CompanyChargeAmount = MainPrice;
        //                                                            CancelCharge = _markup.ProcessHotelCancelationPrice_WithMarkup(ref MainAgentCharge, SearchParam.Rooms.Count * SearchParam.NoofNights, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), MainPrice, isPerce, commonSearch.B2CMarkup, AgentPercentage, AgentMarkupAmount, commonSearch.IsParentId, commonSearch.SubAgentMarkup, ref VatOnPolicy);
        //                                                            #endregion

        //                                                            if (CancelDate < DateTime.Now.Date)
        //                                                                CancelDate = DateTime.Now.Date;
        //                                                            dtCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref CompanyChargeAmount, dtCancellation.NewRow(), ExpediaCurrencyFactor, Currency, Provider, Convert.ToString(OwnsystemHotelId), Convert.ToString(1),
        //                                                                                                           Convert.ToString(RatePlamCode), IsRefundable ? RoomName + " " + BedName : RoomName + " " + BedName + "- Non Refundable", TotalAmenities != "" ? TotalAmenities : "Room Only", Convert.ToDateTime(CancelDate),
        //                                                                                                           DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null), MainAgentCharge, CancelCharge, "", 0, Guid.NewGuid().ToString(), "Yes", "", ""));
        //                                                            dtCancellation.AcceptChanges();
        //                                                        }
        //                                                    }
        //                                                    else
        //                                                    {
        //                                                        //if (CancelDate < DateTime.Now.Date)
        //                                                        //    CancelDate = DateTime.Now.Date;
        //                                                        #region Vat
        //                                                        double VatOnPolicy = 0;
        //                                                        double MainAgentCharge = MainPrice;
        //                                                        double CancelCharge = _markup.ProcessHotelCancelationPrice_WithMarkup(ref MainAgentCharge, SearchParam.Rooms.Count * SearchParam.NoofNights, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), MainPrice, isPerce, commonSearch.B2CMarkup, AgentPercentage, AgentMarkupAmount, commonSearch.IsParentId, commonSearch.SubAgentMarkup, ref VatOnPolicy);
        //                                                        #endregion


        //                                                        dtCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref MainPrice, dtCancellation.NewRow(), ExpediaCurrencyFactor, _supplierConfig.Expedia.EANCurrency, Provider, Convert.ToString(OwnsystemHotelId), Convert.ToString(1),
        //                                                                                                       Convert.ToString(RatePlamCode), IsRefundable ? RoomName + " " + BedName : RoomName + " " + BedName + "- Non Refundable", TotalAmenities != "" ? TotalAmenities : "Room Only", Convert.ToDateTime(DateTime.Now.Date),
        //                                                                                                       DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null), CancelCharge, CancelCharge, "", 0, Guid.NewGuid().ToString(), "Yes", "", ""));
        //                                                        dtCancellation.AcceptChanges();
        //                                                    }



        //                                                    #endregion
        //                                                }
        //                                                else
        //                                                {
        //                                                    #region Cancellation Policy New

        //                                                    if (IsRefundable)// (IsRefundable)
        //                                                    {
        //                                                        DateTime Start = DateTime.Now.Date;
        //                                                        DateTime End = DateTime.Now.Date;
        //                                                        int Night = 0;
        //                                                        double CancellationAmount = 0;
        //                                                        double CancellationPecentage = 0;
        //                                                        double CancellationCharge = 0;
        //                                                        bool IsNonRef = false;
        //                                                        Currency = _supplierConfig.Expedia.EANCurrency;
        //                                                        bool IsTrue = true;
        //                                                        bool ForPol = true;

        //                                                        if (Root[h].rooms[j].rates[k].cancel_penalties != null)
        //                                                        {
        //                                                            int CancellationPolicy = Convert.ToInt32(Root[h].rooms[j].rates[k].cancel_penalties.Count);
        //                                                            DateTime EndDate = DateTime.Now.Date;

        //                                                            for (int l = 0; l < CancellationPolicy; l++)
        //                                                            {
        //                                                                double CancelCharge = 0;
        //                                                                Start = Convert.ToDateTime(Root[h].rooms[j].rates[k].cancel_penalties[l].start);
        //                                                                End = Convert.ToDateTime(Root[h].rooms[j].rates[k].cancel_penalties[l].end);
        //                                                                Currency = Convert.ToString(Root[h].rooms[j].rates[k].cancel_penalties[l].currency);

        //                                                                if (Root[h].rooms[j].rates[k].cancel_penalties[l].nights != null)
        //                                                                {
        //                                                                    Night = Convert.ToInt32(Root[h].rooms[j].rates[k].cancel_penalties[l].nights);
        //                                                                    CancellationCharge = MainPrice;// (MainPrice / SearchParam.NoofNights) * Night;
        //                                                                    CancelCharge = CancellationCharge;
        //                                                                }
        //                                                                else if (Root[h].rooms[j].rates[k].cancel_penalties[l].amount != null)
        //                                                                {
        //                                                                    CancellationAmount = Convert.ToDouble(Root[h].rooms[j].rates[k].cancel_penalties[l].amount);
        //                                                                    CancellationCharge = CancellationCharge + CancellationAmount;
        //                                                                    CancelCharge = CancellationCharge * ExpediaCurrencyFactor;
        //                                                                }
        //                                                                else if (Root[h].rooms[j].rates[k].cancel_penalties[l].percent != null)
        //                                                                {
        //                                                                    double CanelPercentPrice = 0;
        //                                                                    CancellationPecentage = Convert.ToDouble(Root[h].rooms[j].rates[k].cancel_penalties[l].percent.Replace("%", ""));
        //                                                                    //CanelPercent = MainPrice * CancellationPecentage;
        //                                                                    CancellationCharge = (MainPrice * CancellationPecentage) / 100;
        //                                                                    //CancellationCharge = CancellationCharge + CanelPercentPrice;
        //                                                                    CancelCharge = CancellationCharge;
        //                                                                }


        //                                                                if (CancelCharge > MainPrice)
        //                                                                    CancellationCharge = MainPrice;


        //                                                                DateTime CancelDate = DateTime.Now.Date;
        //                                                                //CancelDate = Convert.ToDateTime(Start).AddDays(1).Date;
        //                                                                CancelDate = Convert.ToDateTime(Start);
        //                                                                DateTime NowDate = DateTime.Now.Date;
        //                                                                if (CancelDate <= DateTime.Now.Date)
        //                                                                    CancelDate = DateTime.Now.Date;
        //                                                                else
        //                                                                    CancelDate = CancelDate.AddDays(CutOffDays).Date;

        //                                                                if (CancelDate.AddDays(-1) <= DateTime.Now.Date)
        //                                                                    CancelDate = DateTime.Now.Date;

        //                                                                DateTime LastStart1 = DateTime.Now.Date;
        //                                                                DateTime NextStartDate = DateTime.Now.Date;

        //                                                                if (CancellationPolicy > 1 && ForPol && CancellationPolicy != (l + 1))
        //                                                                {
        //                                                                    NextStartDate = Convert.ToDateTime(Root[h].rooms[j].rates[k].cancel_penalties[l + 1].start);
        //                                                                    NextStartDate = NextStartDate.AddDays(CutOffDays).Date;
        //                                                                    if (NextStartDate.Date <= DateTime.Now.Date)
        //                                                                    {
        //                                                                        ForPol = false;
        //                                                                        continue;

        //                                                                    }

        //                                                                }

        //                                                                if (CancellationPolicy > 1 && CancellationPolicy != (l + 1))
        //                                                                {
        //                                                                    LastStart1 = Convert.ToDateTime(Root[h].rooms[j].rates[k].cancel_penalties[l + 1].start);

        //                                                                    if (End.Date == LastStart1.Date)
        //                                                                        End = End.AddDays(-1).Date;

        //                                                                    End = End.AddDays(CutOffDays).Date; //add on 5/5
        //                                                                }

        //                                                                if (CancellationPolicy == (l + 1))
        //                                                                {
        //                                                                    End = DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null);
        //                                                                    // CancelDate = Start;

        //                                                                    CancelDate = Convert.ToDateTime(Start);

        //                                                                    if (CancelDate <= DateTime.Now.Date)
        //                                                                        CancelDate = DateTime.Now.Date;
        //                                                                    else
        //                                                                        CancelDate = CancelDate.AddDays(CutOffDays).Date;

        //                                                                }
        //                                                                if (CancellationPolicy == 1)
        //                                                                    IsTrue = true;

        //                                                                if (CancellationPolicy >= 3 && l == 1)
        //                                                                {
        //                                                                    End = DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null);
        //                                                                    CancelCharge = MainPrice;
        //                                                                }


        //                                                                #region Vat
        //                                                                double VatOnPolicy = 0;
        //                                                                double MainAgentCharge = CancelCharge;
        //                                                                double CompanyChargeAmount = CancelCharge;
        //                                                                CancelCharge = _markup.ProcessHotelCancelationPrice_WithMarkup(ref MainAgentCharge, SearchParam.Rooms.Count * SearchParam.NoofNights, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), CancellationCharge, isPerce, commonSearch.B2CMarkup, AgentPercentage, AgentMarkupAmount, commonSearch.IsParentId, commonSearch.SubAgentMarkup, ref VatOnPolicy);
        //                                                                #endregion
        //                                                                if (CancelDate.Date < DateTime.Now.Date)
        //                                                                    CancelDate = DateTime.Now.Date;

        //                                                                if (NowDate < CancelDate && CancelDate.Date > DateTime.Now.Date && IsTrue)
        //                                                                {
        //                                                                    if (IsTrue && l == 0)
        //                                                                    {
        //                                                                        double zero = 0;
        //                                                                        dtCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref zero, dtCancellation.NewRow(), ExpediaCurrencyFactor, Currency, Provider, Convert.ToString(OwnsystemHotelId), Convert.ToString(1),
        //                                                                    Convert.ToString(RatePlamCode), IsRefundable ? RoomName + " " + BedName : RoomName + " " + BedName + "- Non Refundable", TotalAmenities != "" ? TotalAmenities : "Room Only", DateTime.Now.Date, CancelDate.AddDays(-1), 0, 0, "", VatOnPolicy, "", "NO", "", "", "", ""));
        //                                                                        dtCancellation.AcceptChanges();
        //                                                                    }

        //                                                                    dtCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref CompanyChargeAmount, dtCancellation.NewRow(), ExpediaCurrencyFactor, Currency, Provider, Convert.ToString(OwnsystemHotelId), Convert.ToString(1),
        //                                                                     Convert.ToString(RatePlamCode), IsRefundable ? RoomName + " " + BedName : RoomName + " " + BedName + "- Non Refundable", TotalAmenities != "" ? TotalAmenities : "Room Only", Convert.ToDateTime(CancelDate), End.Date, MainAgentCharge, CancelCharge, "", VatOnPolicy, "", "NO", "", "", "", ""));
        //                                                                    dtCancellation.AcceptChanges();


        //                                                                    IsTrue = true;
        //                                                                }
        //                                                                else
        //                                                                {
        //                                                                    //if (CancelDate < DateTime.Now.Date)
        //                                                                    //    CancelDate = DateTime.Now.Date;
        //                                                                    //DtEAN_Cancellation.Rows.Add(Common.setCancellationPolicyRow(ref CompanyChargeAmount, DtEAN_Cancellation.NewRow(), CurrencyFactor, Currency, SupplierName, ownsystemid, Convert.ToString(1),
        //                                                                    //                                               Convert.ToString(RatePlamCode), IsRefundable ? RoomName + " " + BedName : RoomName + " " + BedName + "- Non Refundable", TotalAmenities != "" ? TotalAmenities : "Room Only", Convert.ToDateTime(CancelDate),
        //                                                                    //                                               DateTime.ParseExact(objSearPera.CheckInDate, "dd/MM/yyyy", null), MainAgentCharge, CancelCharge, "", 0, Guid.NewGuid().ToString(), "Yes", "", ""));
        //                                                                    //DtEAN_Cancellation.AcceptChanges();

        //                                                                    #region Vat
        //                                                                    VatOnPolicy = 0;
        //                                                                    MainAgentCharge = CancellationCharge;
        //                                                                    CompanyChargeAmount = CancellationCharge;
        //                                                                    CancelCharge = _markup.ProcessHotelCancelationPrice_WithMarkup(ref MainAgentCharge, SearchParam.Rooms.Count * SearchParam.NoofNights, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), CancellationCharge, isPerce, commonSearch.B2CMarkup, AgentPercentage, AgentMarkupAmount, commonSearch.IsParentId, commonSearch.SubAgentMarkup, ref VatOnPolicy);
        //                                                                    #endregion

        //                                                                    if (CancelDate < DateTime.Now.Date)
        //                                                                        CancelDate = DateTime.Now.Date;
        //                                                                    dtCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref CancellationCharge, dtCancellation.NewRow(), ExpediaCurrencyFactor, Currency, Provider, Convert.ToString(OwnsystemHotelId), Convert.ToString(1),
        //                                                                                                       Convert.ToString(RatePlamCode), IsRefundable ? RoomName + " " + BedName : RoomName + " " + BedName + "- Non Refundable", TotalAmenities != "" ? TotalAmenities : "Room Only", Convert.ToDateTime(CancelDate),
        //                                                                                                       End.Date, MainAgentCharge, CancelCharge, "", 0, Guid.NewGuid().ToString(), "Yes", "", ""));
        //                                                                    dtCancellation.AcceptChanges();

        //                                                                    IsTrue = false;

        //                                                                    if (CancellationPolicy >= 3 && l == 1)
        //                                                                        break;

        //                                                                }

        //                                                            }
        //                                                        }
        //                                                        else
        //                                                        {


        //                                                            #region Vat
        //                                                            double VatOnPolicy = 0;
        //                                                            double MainAgentCharge = MainPrice;
        //                                                            double CompanyChargeAmount = MainPrice;
        //                                                            double CancelCharge = _markup.ProcessHotelCancelationPrice_WithMarkup(ref MainAgentCharge, SearchParam.Rooms.Count * SearchParam.NoofNights, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), MainPrice, isPerce, commonSearch.B2CMarkup, AgentPercentage, AgentMarkupAmount, commonSearch.IsParentId, commonSearch.SubAgentMarkup, ref VatOnPolicy);
        //                                                            #endregion

        //                                                            dtCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref MainPrice, dtCancellation.NewRow(), ExpediaCurrencyFactor, _supplierConfig.Expedia.EANCurrency, Provider, Convert.ToString(OwnsystemHotelId), Convert.ToString(1),
        //                                                                                                        Convert.ToString(RatePlamCode), IsRefundable ? RoomName + " " + BedName : RoomName + " " + BedName + "- Non Refundable", TotalAmenities != "" ? TotalAmenities : "Room Only", Convert.ToDateTime(DateTime.Now.Date),
        //                                                                                                        DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null), CancelCharge, CancelCharge, "", 0, Guid.NewGuid().ToString(), "Yes", "", ""));
        //                                                            dtCancellation.AcceptChanges();

        //                                                            break;
        //                                                        }

        //                                                        #region Cancellation Policy

        //                                                    }
        //                                                    else
        //                                                    {
        //                                                        //if (CancelDate < DateTime.Now.Date)
        //                                                        //    CancelDate = DateTime.Now.Date;

        //                                                        #region Vat
        //                                                        double VatOnPolicy = 0;
        //                                                        double MainAgentCharge = MainPrice;
        //                                                        double CancelCharge = _markup.ProcessHotelCancelationPrice_WithMarkup(ref MainAgentCharge, SearchParam.Rooms.Count * SearchParam.NoofNights, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), MainPrice, isPerce, commonSearch.B2CMarkup, AgentPercentage, AgentMarkupAmount, commonSearch.IsParentId, commonSearch.SubAgentMarkup, ref VatOnPolicy);
        //                                                        #endregion

        //                                                        dtCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref MainPrice, dtCancellation.NewRow(), ExpediaCurrencyFactor, _supplierConfig.Expedia.EANCurrency, Provider, Convert.ToString(OwnsystemHotelId), Convert.ToString(1),
        //                                                                                                         Convert.ToString(RatePlamCode), IsRefundable ? RoomName + " " + BedName : RoomName + " " + BedName + "- Non Refundable", TotalAmenities != "" ? TotalAmenities : "Room Only", Convert.ToDateTime(DateTime.Now.Date),
        //                                                                                                         DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null), CancelCharge, CancelCharge, "", 0, Guid.NewGuid().ToString(), "Yes", "", ""));
        //                                                        dtCancellation.AcceptChanges();
        //                                                    }
        //                                                    #endregion

        //                                                    #endregion
        //                                                }
        //                                            }
        //                                        }
        //                                    }
        //                                }
        //                                #region Tables Merge

        //                                Options ops = (await _supplierRes.GetCommonRoomForSuppliers(GiataId, commonSearch.tokenData, commonSearch.advancedOptions, FinalMinPrice, drXmlHotel, dtHotelRoom, dtCancellation, DtRoomRateBreackUp, SearchParam));
        //                                if (ops != null)
        //                                    options.Add(ops);
        //                                #endregion
        //                            }

        //                            #endregion
        //                        }
        //                        #endregion                                
        //                    }
        //                    #endregion
        //                    #endregion


        //                }
        //                #endregion

        //            }

        //        }
        //        return options;
        //    }
        //    catch (Exception ex)
        //    {

        //        await _exception.Error(ex);
        //        ItemEN.ResponseType = "NoCount";
        //        return options;
        //    }
        //    finally
        //    {
        //        #region ResProcessTimer
        //        try
        //        {
        //            if (SearchParam != null && ResProccessTimerLogActive == "1" && timer_room_ean.ElapsedMilliseconds > Convert.ToInt32(ResProccessTimeValueforSaveLog))
        //            {
        //                timer_room_ean.Stop();
        //                SearchParam.SupplierTime += ",|Finish:" + Provider + " Process Response  " + "|Time :" + timer_room_ean.Elapsed.ToString();
        //                await Task.Run(async() => await _commonMethod.FireAway_SQL_ResPorcessTimeLog("", timer_room_ean.Elapsed.ToString(), Provider, SearchParam.SearchKey, 1, SearchParam.AgentID));
        //            }
        //        }
        //        catch (Exception x)
        //        {
        //            await _exception.Error(x);
        //        }
        //        #endregion
        //    }
        //}

        #endregion

        public async Task<List<Options>> ean_HotelSearchWithRooms(HotelX ItemEN, CommonSearch commonSearch, string xmlcity, string xmlcountry, HotelSearchParameter SearchParam, double ExpediaCurrencyFactor, DataTable dtROE, string Suppliername, string ResponseData, string CID, string Currency, DataRow[] ExpDetail, string xmlhotelcode, string platform, string echotoken, string Sign, string ApiKey, string Version, string URL, string Provider, string ProccessCutOffTime, int processTimeout, string ResProccessTimerLogActive, string ResProccessTimeValueforSaveLog, string StoreSupplierResErrorLog)
        {
            System.Diagnostics.Stopwatch timer_room_ean = new System.Diagnostics.Stopwatch();
            timer_room_ean.Start();

            List<Options> options = new List<Options>();

            try
            {
                #region DataTable
                DataTable DtHotel = _commonMethod.GenerateDataTableNewV2();
                #endregion
                if (ResponseData != string.Empty)
                {
                    #region PriceOnly RS
                    //if (ResponseData.ToLower().Contains("eanwserror") || ResponseData.Trim() == "")
                    //{
                    //    ItemEN.ResponseType = "NoCount";
                    //    return options;
                    //}
                    //DataSet Dsean = Common.GetDataSetFromXMLString(ResponseData);
                    //string Provider = "ean";

                    List<EAN.RootObject_EAN2> Root = null;

                    try
                    {
                        var optionss = new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true,
                            ReadCommentHandling = JsonCommentHandling.Skip,
                            AllowTrailingCommas = true
                        };
                        Root = JsonSerializer.Deserialize<List<EAN.RootObject_EAN2>>(ResponseData, optionss);
                        //List<EAN.RootObject_EAN2> Root = JsonSerializer.Deserialize<List<EAN.RootObject_EAN2>>(ResponseData, optionss);
                    }
                    catch (Exception)
                    {
                        Root = Newtonsoft.Json.JsonConvert.DeserializeObject<List<EAN.RootObject_EAN2>>(ResponseData); //140525
                    }


                    //List<EAN.RootObject_EAN2> Root = Newtonsoft.Json.JsonConvert.DeserializeObject<List<EAN.RootObject_EAN2>>(ResponseData); //140525

                    #endregion
                    #region Pax Occupancy
                    int AdultsOcc = 0;
                    int ChildOcc = 0;
                    int TotalAdult = 0;
                    int TotalChild = 0;

                    for (int a = 0; a < SearchParam.Rooms.Count; a++)
                    {
                        AdultsOcc = SearchParam.Rooms[a].NoofAdults;
                        ChildOcc = SearchParam.Rooms[a].NoofChild;
                        TotalAdult = TotalAdult + AdultsOcc;
                        TotalChild = TotalChild + ChildOcc;
                    }
                    #endregion
                    #region EAN Occupancy Block
                    List<string> Occupancy1 = new List<string>();
                    var resultEAN = SearchParam.Rooms.GroupBy(x => new { x.RoomName, x.NoofAdults, x.NoofChild, x.Child1Age, x.Child2Age }).Select(n => new { RoomName = n.Key.RoomName, NoofAdults = n.Key.NoofAdults, NoofChild = n.Key.NoofChild, ChildAge = n.ToList(), Count = n.Count() });
                    foreach (var item in resultEAN)//For each Item
                    {
                        string TotalOcc = "";
                        int ADT = 0;
                        string CHD = "";
                        ADT = item.NoofAdults;
                        switch (item.NoofChild)
                        {
                            case 0:
                                break;
                            case 1:
                                CHD = "-" + item.ChildAge[0].Child1Age;
                                break;
                            case 2:
                                CHD = "-" + item.ChildAge[0].Child1Age;
                                CHD = CHD + "," + item.ChildAge[0].Child2Age;
                                break;
                        }
                        TotalOcc = ADT.ToString() + CHD;
                        Occupancy1.Add(TotalOcc + "$$$" + item.Count);
                    }
                    #endregion
                    for (int h = 0; h < Root.Count; h++)
                    {

                        ItemEN.ResponseType = "Success";
                        //Check Time If High then return NoCount
                        if (ProccessCutOffTime == "1" && timer_room_ean.ElapsedMilliseconds > processTimeout)
                        {
                            //timer_room_ean.Stop();
                            break;
                        }
                        //--end
                        string ItemCode = Root[h].property_id;
                        #region "When not Need Room"  
                        // add hotel code from price only and room code blank here
                        if (SearchParam.Filters.IsShowRooms == 0)
                        {
                            #region "Hotel Code Block "
                            double TotalCharges = 0;
                            {
                                #region Add by jay


                                double MainPrice = 0;
                                double FinalPrice = 0;

                                #region Old Code
                                for (int j = 0; j < Convert.ToInt32(Root[h].rooms.Count); j++)
                                {
                                    for (int k = 0; k < Convert.ToInt32(Root[h].rooms[j].rates.Count); k++)
                                    {
                                        Dictionary<string, EAN.OccupancyPricing> asdad1 = Root[h].rooms[j].rates[k].occupancy_pricing;
                                        FinalPrice = 0;

                                        for (int x = 0; x < Occupancy1.Count; x++)
                                        {
                                            string Occ = ""; //160425
                                            string Count = ""; //160425
                                            string[] spitid = Occupancy1[x].Split(new String[] { "$$$" }, StringSplitOptions.None);

                                            Occ = spitid[0]; //160425 
                                            Count = spitid[1]; //160425
                                            foreach (var kvp in asdad1)
                                            {
                                                if (kvp.Key != Occ) //160425 
                                                    continue;
                                                else
                                                {
                                                    #region DataRaw and Variable Declaration
                                                    double OurProfit = 0;

                                                    List<List<Nightly>> nightly = kvp.Value.nightly;
                                                    IEnumerable<Nightly> stay = kvp.Value.stay;
                                                    Dictionary<string, FeeDetail> fees = kvp.Value.fees;
                                                    Dictionary<string, FeeDetail> totals = kvp.Value.totals;

                                                    double Price = Convert.ToDouble(totals["inclusive"].billable_currency.value) * Convert.ToInt32(spitid[1]);
                                                    if (totals["gross_profit"] == null || totals["gross_profit"].billable_currency == null || totals["gross_profit"].billable_currency.value == null)//added on 30JAN2024
                                                    {
                                                        continue;//2-2-2024
                                                    }
                                                    //double Profit = Convert.ToDouble(acc.totals.marketing_fee.billable_currency.value) * Convert.ToInt32(spitid[1]);
                                                    double Profit = Convert.ToDouble(totals["gross_profit"].billable_currency.value) * Convert.ToInt32(ItemEN.EanProfit) / 100 * Convert.ToInt32(spitid[1]);
                                                    if (Convert.ToString(totals["inclusive"].billable_currency.currency).ToLower() != "usd")
                                                    {
                                                        continue;
                                                    }
                                                    else
                                                        Price = Price * ExpediaCurrencyFactor;

                                                    FinalPrice += Price;

                                                    if (Convert.ToString(totals["marketing_fee"].billable_currency.currency).ToLower() != "usd")
                                                    {
                                                        continue;
                                                    }
                                                    else
                                                        Profit = Profit * ExpediaCurrencyFactor;
                                                    OurProfit += Profit;
                                                    FinalPrice = FinalPrice - OurProfit;
                                                    #endregion
                                                }


                                            }
                                        }
                                        if (MainPrice == 0)
                                            MainPrice = FinalPrice;
                                        else
                                        {
                                            if (MainPrice > FinalPrice)
                                                MainPrice = FinalPrice;
                                        }
                                    }
                                }
                                #endregion


                                #endregion
                                DataRow drXmlHotel = DtHotel.NewRow();
                                drXmlHotel["XMLHotelCode"] = ItemCode;
                                drXmlHotel["Reviews"] = 0;
                                drXmlHotel["OfficeID"] = 0;
                                drXmlHotel["ContractID"] = 0; // CID;
                                drXmlHotel["IsShowSupplier"] = false;
                                drXmlHotel["OwnSystemHotelId"] = 0;
                                drXmlHotel["Favorite"] = 0;
                                drXmlHotel["XmlHotelCity"] = xmlcity;
                                drXmlHotel["XMLCountry"] = xmlcountry;
                                drXmlHotel["XMLCity"] = xmlcity;
                                drXmlHotel["XMLCode"] = xmlcountry;
                                drXmlHotel["SupplierName"] = Suppliername;
                                drXmlHotel["BookingStatus"] = "Available";
                                drXmlHotel["SupplierCurrency"] = Currency;
                                drXmlHotel["FinalSellingPrice"] = FinalPrice;
                                drXmlHotel["SellingPrice"] = FinalPrice;
                                DtHotel.Rows.Add(drXmlHotel);
                                TotalCharges = FinalPrice;
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
                                        decimal LowestPrice = Convert.ToDecimal(MinPrice).CalculatePrice(commonSearch.tokenData, commonSearch.advancedOptions, _supplierConfig.Expedia.EANCurrency);
                                        Options ops = (await _supplierRes.GetCommonRoomForSuppliers(GiataId, commonSearch.tokenData, commonSearch.advancedOptions, Convert.ToDouble(LowestPrice), drXmlHotel, dt_HotelRoom, dt_Cancellation, dt_RateBreackUp, SearchParam));
                                        if (ops != null)
                                            options.Add(ops);
                                    }
                                    else
                                    {

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
                            #region Hotels
                            //object Occupancy = Root[h].rooms[0].rates[0].occupancy_pricing;
                            //JToken[] asdad = Newtonsoft.Json.Linq.JToken.Parse(Occupancy.ToString()).ToArray();

                            #region "Hotel Block here"

                            //var aaa = asdad[0];
                            //string ItemCode = Root[i].property_id;
                            double FinalPrice = 1;
                            //foreach (JToken item in asdad)
                            //{
                            //    JProperty questionAnswerDetails = ((JProperty)item);
                            //    string Name = questionAnswerDetails.Name;
                            //    string value = Convert.ToString(questionAnswerDetails.Value);
                            //    ng acc = JsonConvert.DeserializeObject<ng>(value);
                            //    double Price = Convert.ToDouble(acc.totals.inclusive.billable_currency.value);
                            //    if (FinalPrice == 0)
                            //        FinalPrice = Price;
                            //    else
                            //    {
                            //        if (FinalPrice > Price)
                            //            FinalPrice = Price;
                            //    }
                            //}
                            DataRow drXmlHotel = DtHotel.NewRow();
                            drXmlHotel["XMLHotelCode"] = ItemCode;
                            //drXmlHotel["HotelName"] = "";
                            //drXmlHotel["HotelFrontImage"] = "";
                            //drXmlHotel["Location"] = "";
                            //drXmlHotel["lat"] = "";
                            //drXmlHotel["longitute"] = "";
                            drXmlHotel["Rating"] = 0;
                            drXmlHotel["Reviews"] = 0;
                            //drXmlHotel["Description"] = "";
                            // drXmlHotel["EssentialInformation"] = "";
                            drXmlHotel["OfficeID"] = 0;
                            drXmlHotel["ContractID"] = 0;// CID;
                            drXmlHotel["IsShowSupplier"] = false;
                            drXmlHotel["OwnSystemHotelId"] = 0;
                            //drXmlHotel["Address"] = "";
                            drXmlHotel["Favorite"] = 0;
                            drXmlHotel["XmlHotelCity"] = xmlcity;
                            drXmlHotel["XMLCountry"] = xmlcountry;
                            drXmlHotel["XMLCity"] = xmlcity;
                            drXmlHotel["XMLCode"] = xmlcountry;
                            drXmlHotel["SupplierName"] = Suppliername;
                            drXmlHotel["BookingStatus"] = "Available";
                            drXmlHotel["SupplierCurrency"] = Currency;
                            drXmlHotel["FinalSellingPrice"] = FinalPrice;
                            drXmlHotel["SellingPrice"] = FinalPrice;
                            DtHotel.Rows.Add(drXmlHotel);
                            FinalMinPrice = FinalPrice;
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
                                CutOffDays = _commonMethod.GetCutOffDays(commonSearch.dtSuplierMarkup, Provider);
                                #region "Room Logic"
                                //DataTable XMLTBORoomOnly = Common.GenerateRoomtypeDataTableNewV2();

                                //if (SearchParam.Filters.IsShowRooms == 1)
                                {
                                    #region "Hotel Block here"
                                    double AgentPercentage = 0.0;
                                    double AgentMarkupAmount = 0.0;
                                    Boolean isPerce = true;
                                    bool blnIsShowSupplier = false;
                                    //_markup.getAgetMarkupSetting(Provider, GiataId.ToString(), commonSearch.dtSuplierMarkup, commonSearch.dtMarkupDetails, ref AgentPercentage, ref AgentMarkupAmount, ref blnIsShowSupplier, ref isPerce, commonSearch.AgentId, RatingHotel, SearchParam.CityID, SearchParam.CountryID.ToString(), SearchParam.CheckInDate);
                                    _markup.getAgetMarkupSettingNew_new(Provider, Convert.ToString(GiataId), commonSearch.dtSuplierMarkup, commonSearch.dsMarkupDetails, ref AgentPercentage, ref AgentMarkupAmount, ref blnIsShowSupplier, ref isPerce, commonSearch.AgentId, RatingHotel, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), SearchParam.CheckInDate);
                                    DataSet dsFinal = new DataSet();
                                    DataTable dt_HotelRoom = _commonMethod.GenerateRoomtypeDataTableNewV2();
                                    DataTable dt_Cancellation = _commonMethod.GenerateCancellationDataTableNewV2();
                                    DataTable dt_RateBreackUp = _commonMethod.GenerateoomRateBreackUpDataTableNewV2();

                                    //string Sign = Convert.ToString(ExpDetail[0]["UserName"]);
                                    //string ApiKey = Convert.ToString(ExpDetail[0]["Password"]);
                                    //string Version = Convert.ToString(ExpDetail[0]["version"]);
                                    //string URL = Convert.ToString(ExpDetail[0]["WebSiteURL"]);
                                    string EssInfo = "";// Get_EAN_PropertyContent(ItemCode, platform, echotoken, Sign, ApiKey, Version, URL, Provider);
                                                        //var result = SearchParam.Rooms.GroupBy(x => new { x.RoomName, x.NoofAdults, x.NoofChild, x.Child1Age, x.Child2Age }).Select(n => new { RoomName = n.Key.RoomName, NoofAdults = n.Key.NoofAdults, NoofChild = n.Key.NoofChild, ChildAge = n.ToList(), Count = n.Count() });

                                    //if (ResponseData.Contains("eanwserror") || ResponseData.ToString() == "" || ResponseData == null)
                                    // return options;
                                    //List<RootObject_EAN2> Root = JsonConvert.DeserializeObject<List<RootObject_EAN2>>(ResponseData);



                                    //List<string> Occupancy1 = new List<string>();
                                    //foreach (var item in result)//For each Item
                                    //{
                                    //    string TotalOcc = "";
                                    //    //for (int i = 0; i < item.Count; i++)
                                    //    {
                                    //        int ADT = 0;
                                    //        string CHD = "";
                                    //        ADT = item.NoofAdults;
                                    //        switch (item.NoofChild)
                                    //        {
                                    //            case 0:
                                    //                break;
                                    //            case 1:
                                    //                CHD = "-" + item.ChildAge[0].Child1Age;
                                    //                break;
                                    //            case 2:
                                    //                CHD = "-" + item.ChildAge[0].Child1Age;
                                    //                CHD = CHD + "," + item.ChildAge[0].Child2Age;
                                    //                break;
                                    //        }
                                    //        TotalOcc = ADT.ToString() + CHD;
                                    //        Occupancy1.Add(TotalOcc + "$$$" + item.Count);
                                    //    }
                                    //}

                                    //for (int i = 0; i < Root.Count; i++)
                                    {
                                        string Item_Code = Root[h].property_id;
                                        int Rooms = Convert.ToInt32(Root[h].rooms.Count);
                                        for (int j = 0; j < Rooms; j++)
                                        {
                                            int Rate = Convert.ToInt32(Root[h].rooms[j].rates.Count);
                                            string RoomId = Root[h].rooms[j].id;
                                            string RoomName = Root[h].rooms[j].room_name.Replace("-", " ");
                                            for (int k = 0; k < Rate; k++)
                                            {
                                                Dictionary<string, EAN.OccupancyPricing> asdad_Room = Root[h].rooms[j].rates[k].occupancy_pricing;


                                                //var aaa = asdad[0];
                                                string RateId = Root[h].rooms[j].rates[k].id;
                                                //string RatePlamCode = RoomId + "$$$" + RateId + "$$$";
                                                //string PromoId = Root[h].rooms[j].rates[k].promo_id;
                                                Boolean IsRefundable = Convert.ToBoolean(Root[h].rooms[j].rates[k].refundable);
                                                string AmenitiesId = "";
                                                string AmenitiesName = "";
                                                string TotalAmenities = "";

                                                if (true)// (Convert.ToInt32(ConfigurationManager.AppSettings["EANMealLogic"]) == 1)
                                                {
                                                    if (Root[h].rooms[j].rates[k].amenities != null)
                                                    {
                                                        int Amenities = Convert.ToInt32(Root[h].rooms[j].rates[k].amenities.Count);
                                                        List<string> StrMealList = new List<string>();
                                                        foreach (var item in Root[h].rooms[j].rates[k].amenities)
                                                        {
                                                            AmenitiesId = Convert.ToString(item.Value.id);
                                                            AmenitiesName = Convert.ToString(item.Value.name);
                                                            if (MealNameCheck(AmenitiesName))
                                                            {
                                                                StrMealList.Add(AmenitiesName);
                                                            }
                                                        }
                                                        StrMealList.Sort();
                                                        for (int i = 0; i < StrMealList.Count; i++)
                                                        {
                                                            if (TotalAmenities == string.Empty)
                                                                TotalAmenities = StrMealList[i];
                                                            else
                                                                TotalAmenities = TotalAmenities + " , " + StrMealList[i];
                                                        }
                                                    }
                                                }
                                                //
                                                else
                                                {
                                                    if (Root[h].rooms[j].rates[k].amenities != null)
                                                    {
                                                        foreach (var item in Root[h].rooms[j].rates[k].amenities)
                                                        {
                                                            //AmenitiesId = Convert.ToString(item.Value.id);
                                                            AmenitiesName = Convert.ToString(item.Value.name);
                                                            if (TotalAmenities == string.Empty)
                                                                TotalAmenities = AmenitiesName;
                                                            else
                                                                TotalAmenities = TotalAmenities + " , " + AmenitiesName;
                                                        }
                                                    }
                                                }

                                                string PromotionId = "";
                                                string PromotionName = "";
                                                string TotalPromotion = "";

                                                string DealId = "";
                                                string DealName = "";
                                                string TotalDeal = "";

                                                #region Comment Code
                                                //if (Root[h].rooms[j].rates[k].promotions != null)
                                                //{
                                                //if (Root[h].rooms[j].rates[k].promotions.value_adds != null)
                                                //{
                                                //    foreach (var item in Root[h].rooms[j].rates[k].promotions.value_adds)
                                                //    {
                                                //        //PromotionId = Convert.ToString(item.Value.id);
                                                //        PromotionName = Convert.ToString(item.Value.description);

                                                //        if (TotalPromotion == string.Empty)
                                                //            TotalPromotion = PromotionName;
                                                //        else
                                                //            TotalPromotion = TotalPromotion + " , " + PromotionName;
                                                //    }
                                                //}

                                                //if (Root[h].rooms[j].rates[k].promotions.deal != null)
                                                //{
                                                //    {
                                                //        DealId = Convert.ToString(Root[h].rooms[j].rates[k].promotions.deal.id);
                                                //        DealName = Convert.ToString(Root[h].rooms[j].rates[k].promotions.deal.description);

                                                //        if (TotalDeal == string.Empty)
                                                //            TotalDeal = DealName;
                                                //        else
                                                //            TotalDeal = TotalDeal + " , " + DealName;
                                                //    }
                                                //}
                                                //}
                                                #endregion


                                                if (Root[h].rooms[j].rates[k].bed_groups != null)
                                                {
                                                    int BedGroup = Root[h].rooms[j].rates[k].bed_groups.Count;
                                                    string ESS = "";
                                                    if (BedGroup > 1)
                                                        ESS = "<br><b>Bed Type is not Guaranteed<b>";
                                                    string BedName = "";
                                                    //for (int m = 0; m < BedGroup; m++)
                                                    foreach (var item1 in Root[h].rooms[j].rates[k].bed_groups)
                                                    {
                                                        //string BedGroupMethod = Convert.ToString(Root[h].rooms[j].rates[k].bed_groups[m].links.price_check.method);
                                                        //string BedGroupLink = Convert.ToString(Root[h].rooms[j].rates[k].bed_groups[m].links.price_check.href);
                                                        //if (Root[h].rooms[j].rates[k].bed_groups[m].configuration != null)
                                                        //    BedName = Root[h].rooms[j].rates[k].bed_groups[m].configuration[0].type;
                                                        //string BedGroupMethod = Convert.ToString(item1.Value.links.price_check.method);
                                                        string BedGroupLink = Convert.ToString(item1.Value.links.price_check.href);
                                                        if (item1.Value.description != null)
                                                            BedName = item1.Value.description.Replace("-", " ");
                                                        string RatePlamCode = RoomId + "$$$" + RateId + "$$$" + BedGroupLink + "$$$" + Guid.NewGuid();
                                                        double MainPrice = 0;
                                                        double OurProfit = 0;
                                                        double StrikeTroughMain = 0;
                                                        string EssentialAdd = "";
                                                        for (int x = 0; x < Occupancy1.Count; x++)
                                                        {
                                                            string Occ = "";
                                                            string Count = "";
                                                            string OccID = Occupancy1[x];
                                                            string[] spitid = OccID.Split(new String[] { "$$$" }, StringSplitOptions.None);
                                                            Occ = spitid[0];
                                                            Count = spitid[1];
                                                            foreach (var kvp in asdad_Room)
                                                            {
                                                                if (kvp.Key != Occ)
                                                                    continue;
                                                                else
                                                                {


                                                                    List<List<Nightly>> nightly = kvp.Value.nightly;
                                                                    IEnumerable<Nightly> stay = kvp.Value.stay;
                                                                    Dictionary<string, FeeDetail> fees = kvp.Value.fees;
                                                                    Dictionary<string, FeeDetail> totals = kvp.Value.totals;

                                                                    #region DataRaw and Variable Declaration
                                                                    double Price = 0;
                                                                    if (totals.ContainsKey("inclusive"))
                                                                        Price = Convert.ToDouble(totals["inclusive"].billable_currency.value);

                                                                    if (!totals.ContainsKey("gross_profit") || totals["gross_profit"].billable_currency == null || totals["gross_profit"].billable_currency.value == null)//added on 30JAN2024
                                                                    {
                                                                        continue;
                                                                    }

                                                                    double Profit = 0;
                                                                    if (totals.ContainsKey("gross_profit"))
                                                                        Profit = Convert.ToDouble(totals["gross_profit"].billable_currency.value) * Convert.ToInt32(ItemEN.EanProfit) / 100;

                                                                    double StrikeTrough = 0;
                                                                    if (totals.ContainsKey("strikethrough"))
                                                                    {
                                                                        StrikeTrough = Convert.ToDouble(totals["strikethrough"].billable_currency.value);

                                                                    }
                                                                    Price = Price * Convert.ToInt32(Count);
                                                                    Profit = Profit * Convert.ToInt32(Count);
                                                                    StrikeTrough = StrikeTrough * Convert.ToInt32(Count);


                                                                    if (totals.ContainsKey("inclusive"))
                                                                    {
                                                                        string Billable_Currency = Convert.ToString(totals["inclusive"].billable_currency.currency);
                                                                        if (Billable_Currency.ToLower() != commonSearch.tokenData.SiteCurrency?.ToLower())
                                                                        {
                                                                            double CurrencyFactor1 = _commonMethod.GetROEByCurrency(Billable_Currency, dtROE);//280125
                                                                                                                                                              //DataTable dtInrCurrency = await _commonMethod.GetCountryCurrency(Billable_Currency);
                                                                                                                                                              //double CurrencyFactor1 = Convert.ToDouble(dtInrCurrency.Rows[0]["AmtToExch"]);
                                                                            Price = Price * CurrencyFactor1;
                                                                        }
                                                                        else
                                                                            Price = Price * ExpediaCurrencyFactor;
                                                                    }

                                                                    MainPrice += Price;

                                                                    if (totals.ContainsKey("marketing_fee"))
                                                                    {
                                                                        string Billable_CurrencyProfit = Convert.ToString(totals["marketing_fee"].billable_currency.currency);
                                                                        if (Billable_CurrencyProfit.ToLower() != commonSearch.tokenData.SiteCurrency?.ToLower())
                                                                        {
                                                                            double CurrencyFactor2 = _commonMethod.GetROEByCurrency(Billable_CurrencyProfit, dtROE);//280125
                                                                                                                                                                    //DataTable dtInrCurrency = await _commonMethod.GetCountryCurrency(Billable_CurrencyProfit);
                                                                                                                                                                    //double CurrencyFactor2 = Convert.ToDouble(dtInrCurrency.Rows[0]["AmtToExch"]);
                                                                            Profit = Profit * CurrencyFactor2;
                                                                        }
                                                                        else
                                                                            Profit = Profit * ExpediaCurrencyFactor;
                                                                    }

                                                                    OurProfit += Profit;

                                                                    if (totals.ContainsKey("strikethrough"))
                                                                    {
                                                                        string StrikeTrough_Currency = Convert.ToString(totals["strikethrough"].billable_currency.currency);
                                                                        if (StrikeTrough_Currency.ToLower() != commonSearch.tokenData.SiteCurrency?.ToLower())
                                                                        {
                                                                            double CurrencyFactor4 = _commonMethod.GetROEByCurrency(StrikeTrough_Currency, dtROE);//280125
                                                                                                                                                                  //DataTable dtInrCurrency = await _commonMethod.GetCountryCurrency(StrikeTrough_Currency);
                                                                                                                                                                  //double CurrencyFactor4 = Convert.ToDouble(dtInrCurrency.Rows[0]["AmtToExch"]);
                                                                            StrikeTrough = StrikeTrough * CurrencyFactor4;
                                                                        }
                                                                        else
                                                                            StrikeTrough = StrikeTrough * ExpediaCurrencyFactor;
                                                                        StrikeTroughMain += StrikeTrough;

                                                                    }


                                                                    if (fees != null)
                                                                    {
                                                                        if (fees.ContainsKey("mandatory_fee"))
                                                                        {
                                                                            string FeesDesc1 = "";
                                                                            string FeesCurrency1 = "";
                                                                            double FeesPrice1 = 0;

                                                                            FeesDesc1 = "<b>Mandatory Fees</b>";

                                                                            FeesPrice1 += Convert.ToDouble(fees["mandatory_fee"].billable_currency.value);
                                                                            FeesCurrency1 = Convert.ToString(fees["mandatory_fee"].billable_currency.currency);

                                                                            EssentialAdd = FeesDesc1 + "<b> : </b>" + FeesPrice1 + " (" + FeesCurrency1 + ") ";

                                                                        }
                                                                        if (fees.ContainsKey("mandatory_tax"))
                                                                        {
                                                                            string FeesDesc2 = "";
                                                                            string FeesCurrency2 = "";
                                                                            double FeesPrice2 = 0;

                                                                            FeesDesc2 = "<b>Mandatory Taxes</b>";

                                                                            FeesPrice2 += Convert.ToDouble(fees["mandatory_tax"].billable_currency.value);
                                                                            FeesCurrency2 = Convert.ToString(fees["mandatory_tax"].billable_currency.currency);

                                                                            if (EssentialAdd != string.Empty)
                                                                                EssentialAdd = " , " + FeesDesc2 + " : " + FeesPrice2 + " (" + FeesCurrency2 + ") ";
                                                                            else
                                                                                EssentialAdd = FeesDesc2 + "<b> : </b>" + FeesPrice2 + " (" + FeesCurrency2 + ") ";

                                                                        }
                                                                        if (fees.ContainsKey("resort_fee"))
                                                                        {
                                                                            string FeesDesc3 = "";
                                                                            string FeesCurrency3 = "";
                                                                            double FeesPrice3 = 0;

                                                                            FeesDesc3 = "<b>Resort Fees</b>";

                                                                            FeesPrice3 += Convert.ToDouble(fees["resort_fee"].billable_currency.value);
                                                                            FeesCurrency3 = Convert.ToString(fees["resort_fee"].billable_currency.currency);

                                                                            if (EssentialAdd != string.Empty)
                                                                                EssentialAdd = " , " + FeesDesc3 + " : " + FeesPrice3 + " (" + FeesCurrency3 + ") ";
                                                                            else
                                                                                EssentialAdd = FeesDesc3 + " : " + FeesPrice3 + " (" + FeesCurrency3 + ") ";
                                                                        }
                                                                    }

                                                                    if (EssentialAdd != string.Empty)
                                                                        EssentialAdd = EssentialAdd + "For" + RoomName + " " + BedName;
                                                                    else
                                                                        EssentialAdd = "";


                                                                    #endregion

                                                                    #region RateBreakUp
                                                                    double markup = 0;

                                                                    #endregion

                                                                }
                                                            }

                                                        }

                                                        double VatOnRateBrackUp = 0;
                                                        double tempmin = MainPrice - OurProfit;
                                                        tempmin = _markup.ProcessHotelRateBreakupPrice_WithMarkup(1, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), Convert.ToDouble(tempmin), commonSearch.B2CMarkup, commonSearch.IsParentId, isPerce, AgentPercentage, AgentMarkupAmount, commonSearch.SubAgentMarkup, ref VatOnRateBrackUp);



                                                        #region Rooms
                                                        //Boolean IsRefundable = Convert.ToBoolean(Root[h].rooms[j].rates[k].refundable);

                                                        DataRow drEANRoom = dtHotelRoom.NewRow();
                                                        drEANRoom["OwnSystemHotelId"] = OwnsystemHotelId;
                                                        drEANRoom["RoomTypeName"] = IsRefundable ? RoomName + " " + BedName : RoomName + " " + BedName + "- Non Refundable";
                                                        drEANRoom["RoomTypeCode"] = RatePlamCode;
                                                        drEANRoom["MealId"] = TotalAmenities != "" ? TotalAmenities : "Room Only";
                                                        drEANRoom["MealName"] = TotalAmenities != "" ? TotalAmenities : "Room Only";
                                                        drEANRoom["ContractID"] = ItemCode;
                                                        drEANRoom["BoardType"] = TotalAmenities != "" ? TotalAmenities : "Room Only";
                                                        drEANRoom["RoomTypeId"] = RatePlamCode;
                                                        drEANRoom["AdultsOccupancy"] = TotalAdult;
                                                        drEANRoom["ChildOccupancy"] = TotalChild;
                                                        drEANRoom["MaxExtraBed"] = "0";
                                                        drEANRoom["RoomNo"] = 1;
                                                        drEANRoom["RoomSequenceNo"] = Guid.NewGuid();
                                                        drEANRoom["Type"] = "";
                                                        drEANRoom["BookingStatus"] = "Available";
                                                        drEANRoom["UniqueNo"] = OwnsystemHotelId;
                                                        drEANRoom["OfferDescription"] = "";
                                                        drEANRoom["OfferCode"] = "";
                                                        drEANRoom["EssentialInformation"] = EssInfo + "<br>" + EssentialAdd + ESS;

                                                        drEANRoom["CompanyBuyingPriceExpedia"] = MainPrice;
                                                        drEANRoom["GrossProfitOnline"] = 0;
                                                        double discountprice = 0;
                                                        discountprice = StrikeTroughMain;
                                                        if (discountprice > (MainPrice))
                                                        {
                                                            discountprice = _markup.ProcessHotelRateBreakupPrice_WithMarkup(1, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), Convert.ToDouble(discountprice), commonSearch.B2CMarkup, commonSearch.IsParentId, isPerce, AgentPercentage, AgentMarkupAmount, commonSearch.SubAgentMarkup, ref VatOnRateBrackUp);

                                                            drEANRoom["GrossWitohutDiscount"] = discountprice;
                                                        }

                                                        MainPrice = MainPrice - OurProfit;

                                                        double ItemPrice = Convert.ToDouble(MainPrice);

                                                        drEANRoom["OurProfit"] = OurProfit;
                                                        drEANRoom["CurrencyFactor"] = ExpediaCurrencyFactor;

                                                        _markup.ProcessHotelRoom_WithMarkup(ExpediaCurrencyFactor, _supplierConfig.Expedia.EANCurrency, false, true, SearchParam.NoofNights, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), SearchParam.CheckInDate, SearchParam.CheckOutDate, ref ItemPrice, ref drEANRoom, isPerce, AgentPercentage, AgentMarkupAmount, commonSearch.SubAgentMarkup, commonSearch.B2CMarkup, commonSearch.IsParentId);

                                                        drEANRoom["SupplierName"] = Provider;
                                                        dtHotelRoom.Rows.Add(drEANRoom);
                                                        #endregion



                                                        if (Provider.ToLower() == "ean_b2b" || Provider.ToLower() == "ean_pkg")
                                                        {
                                                            #region Cancellation Policy

                                                            if (IsRefundable)
                                                            {
                                                                DateTime Start = DateTime.Now.Date;
                                                                DateTime End = DateTime.Now.Date;
                                                                int Night = 0;
                                                                double CancellationAmount = 0;
                                                                double CancellationPecentage = 0;
                                                                double CancellationCharge = 0;
                                                                Currency = _supplierConfig.Expedia.EANCurrency;
                                                                if (Root[h].rooms[j].rates[k].cancel_penalties != null)
                                                                {
                                                                    int CancellationPolicy = Convert.ToInt32(Root[h].rooms[j].rates[k].cancel_penalties.Count);
                                                                    for (int l = 0; l < CancellationPolicy; l++)
                                                                    {
                                                                        Start = Convert.ToDateTime(Root[h].rooms[j].rates[k].cancel_penalties[l].start);
                                                                        End = Convert.ToDateTime(Root[h].rooms[j].rates[k].cancel_penalties[l].end);
                                                                        Currency = Convert.ToString(Root[h].rooms[j].rates[k].cancel_penalties[l].currency);

                                                                        if (Root[h].rooms[j].rates[k].cancel_penalties[l].nights != null)
                                                                        {
                                                                            Night = Convert.ToInt32(Root[h].rooms[j].rates[k].cancel_penalties[l].nights);
                                                                            CancellationCharge = MainPrice;// (MainPrice / SearchParam.NoofNights) * Night;
                                                                        }
                                                                        else if (Root[h].rooms[j].rates[k].cancel_penalties[l].amount != null)
                                                                        {
                                                                            CancellationAmount = Convert.ToDouble(Root[h].rooms[j].rates[k].cancel_penalties[l].amount);
                                                                            CancellationCharge = CancellationCharge + CancellationAmount;
                                                                        }
                                                                        else if (Root[h].rooms[j].rates[k].cancel_penalties[l].percent != null)
                                                                        {
                                                                            double CanelPercent = 0;
                                                                            CancellationPecentage = Convert.ToDouble(Root[h].rooms[j].rates[k].cancel_penalties[l].percent.Replace("%", ""));
                                                                            CanelPercent = MainPrice * CancellationPecentage;
                                                                            CancellationCharge = CancellationCharge + CanelPercent;

                                                                        }
                                                                    }
                                                                }

                                                                if (CancellationCharge > MainPrice)
                                                                    CancellationCharge = MainPrice;


                                                                double CancelCharge = CancellationCharge;

                                                                DateTime CancelDate = DateTime.Now.Date;
                                                                CancelDate = Convert.ToDateTime(Start);
                                                                DateTime NowDate = DateTime.Now.Date;
                                                                if (CancelDate <= DateTime.Now.Date)
                                                                    CancelDate = DateTime.Now.Date;
                                                                else
                                                                    CancelDate = CancelDate.AddDays(CutOffDays).Date;

                                                                if (CancelDate.AddDays(-1) <= DateTime.Now.Date)
                                                                    CancelDate = DateTime.Now.Date;
                                                                #region Vat
                                                                double VatOnPolicy = 0;
                                                                double MainAgentCharge = CancelCharge;
                                                                double CompanyChargeAmount = CancelCharge;
                                                                CancelCharge = _markup.ProcessHotelCancelationPrice_WithMarkup(ref MainAgentCharge, SearchParam.Rooms.Count * SearchParam.NoofNights, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), CancelCharge, isPerce, commonSearch.B2CMarkup, AgentPercentage, AgentMarkupAmount, commonSearch.IsParentId, commonSearch.SubAgentMarkup, ref VatOnPolicy);
                                                                #endregion
                                                                if (CancelDate.Date < DateTime.Now.Date)
                                                                    CancelDate = DateTime.Now.Date;

                                                                if (NowDate < CancelDate && CancelDate.Date > DateTime.Now.Date)
                                                                {
                                                                    double zero = 0;

                                                                    dtCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref zero, dtCancellation.NewRow(), ExpediaCurrencyFactor, Currency, Provider, Convert.ToString(OwnsystemHotelId), Convert.ToString(1),
                                                                Convert.ToString(RatePlamCode), IsRefundable ? RoomName + " " + BedName : RoomName + " " + BedName + "- Non Refundable", TotalAmenities != "" ? TotalAmenities : "Room Only", DateTime.Now.Date, CancelDate.AddDays(-1), 0, 0, "", VatOnPolicy, "", "NO", "", "", "", ""));
                                                                    dtCancellation.AcceptChanges();

                                                                    dtCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref CompanyChargeAmount, dtCancellation.NewRow(), ExpediaCurrencyFactor, Currency, Provider, Convert.ToString(OwnsystemHotelId), Convert.ToString(1),
                                                                Convert.ToString(RatePlamCode), IsRefundable ? RoomName + " " + BedName : RoomName + " " + BedName + "- Non Refundable", TotalAmenities != "" ? TotalAmenities : "Room Only", Convert.ToDateTime(CancelDate), DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null), MainAgentCharge, CancelCharge, "", VatOnPolicy, "", "NO", "", "", "", ""));
                                                                    dtCancellation.AcceptChanges();
                                                                }
                                                                else
                                                                {
                                                                    #region Vat
                                                                    VatOnPolicy = 0;
                                                                    MainAgentCharge = MainPrice;
                                                                    CompanyChargeAmount = MainPrice;
                                                                    CancelCharge = _markup.ProcessHotelCancelationPrice_WithMarkup(ref MainAgentCharge, SearchParam.Rooms.Count * SearchParam.NoofNights, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), MainPrice, isPerce, commonSearch.B2CMarkup, AgentPercentage, AgentMarkupAmount, commonSearch.IsParentId, commonSearch.SubAgentMarkup, ref VatOnPolicy);
                                                                    #endregion

                                                                    if (CancelDate < DateTime.Now.Date)
                                                                        CancelDate = DateTime.Now.Date;
                                                                    dtCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref CompanyChargeAmount, dtCancellation.NewRow(), ExpediaCurrencyFactor, Currency, Provider, Convert.ToString(OwnsystemHotelId), Convert.ToString(1),
                                                                                                                   Convert.ToString(RatePlamCode), IsRefundable ? RoomName + " " + BedName : RoomName + " " + BedName + "- Non Refundable", TotalAmenities != "" ? TotalAmenities : "Room Only", Convert.ToDateTime(CancelDate),
                                                                                                                   DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null), MainAgentCharge, CancelCharge, "", 0, Guid.NewGuid().ToString(), "Yes", "", ""));
                                                                    dtCancellation.AcceptChanges();
                                                                }
                                                            }
                                                            else
                                                            {
                                                                //if (CancelDate < DateTime.Now.Date)
                                                                //    CancelDate = DateTime.Now.Date;
                                                                #region Vat
                                                                double VatOnPolicy = 0;
                                                                double MainAgentCharge = MainPrice;
                                                                double CancelCharge = _markup.ProcessHotelCancelationPrice_WithMarkup(ref MainAgentCharge, SearchParam.Rooms.Count * SearchParam.NoofNights, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), MainPrice, isPerce, commonSearch.B2CMarkup, AgentPercentage, AgentMarkupAmount, commonSearch.IsParentId, commonSearch.SubAgentMarkup, ref VatOnPolicy);
                                                                #endregion


                                                                dtCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref MainPrice, dtCancellation.NewRow(), ExpediaCurrencyFactor, _supplierConfig.Expedia.EANCurrency, Provider, Convert.ToString(OwnsystemHotelId), Convert.ToString(1),
                                                                                                               Convert.ToString(RatePlamCode), IsRefundable ? RoomName + " " + BedName : RoomName + " " + BedName + "- Non Refundable", TotalAmenities != "" ? TotalAmenities : "Room Only", Convert.ToDateTime(DateTime.Now.Date),
                                                                                                               DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null), CancelCharge, CancelCharge, "", 0, Guid.NewGuid().ToString(), "Yes", "", ""));
                                                                dtCancellation.AcceptChanges();
                                                            }



                                                            #endregion
                                                        }
                                                        else
                                                        {
                                                            #region Cancellation Policy New

                                                            if (IsRefundable)// (IsRefundable)
                                                            {
                                                                DateTime Start = DateTime.Now.Date;
                                                                DateTime End = DateTime.Now.Date;
                                                                int Night = 0;
                                                                double CancellationAmount = 0;
                                                                double CancellationPecentage = 0;
                                                                double CancellationCharge = 0;
                                                                bool IsNonRef = false;
                                                                Currency = _supplierConfig.Expedia.EANCurrency;
                                                                bool IsTrue = true;
                                                                bool ForPol = true;

                                                                if (Root[h].rooms[j].rates[k].cancel_penalties != null)
                                                                {
                                                                    int CancellationPolicy = Convert.ToInt32(Root[h].rooms[j].rates[k].cancel_penalties.Count);
                                                                    DateTime EndDate = DateTime.Now.Date;

                                                                    for (int l = 0; l < CancellationPolicy; l++)
                                                                    {
                                                                        double CancelCharge = 0;
                                                                        Start = Convert.ToDateTime(Root[h].rooms[j].rates[k].cancel_penalties[l].start);
                                                                        End = Convert.ToDateTime(Root[h].rooms[j].rates[k].cancel_penalties[l].end);
                                                                        Currency = Convert.ToString(Root[h].rooms[j].rates[k].cancel_penalties[l].currency);

                                                                        if (Root[h].rooms[j].rates[k].cancel_penalties[l].nights != null)
                                                                        {
                                                                            Night = Convert.ToInt32(Root[h].rooms[j].rates[k].cancel_penalties[l].nights);
                                                                            CancellationCharge = MainPrice;// (MainPrice / SearchParam.NoofNights) * Night;
                                                                            CancelCharge = CancellationCharge;
                                                                        }
                                                                        else if (Root[h].rooms[j].rates[k].cancel_penalties[l].amount != null)
                                                                        {
                                                                            CancellationAmount = Convert.ToDouble(Root[h].rooms[j].rates[k].cancel_penalties[l].amount);
                                                                            CancellationCharge = CancellationCharge + CancellationAmount;
                                                                            CancelCharge = CancellationCharge * ExpediaCurrencyFactor;
                                                                        }
                                                                        else if (Root[h].rooms[j].rates[k].cancel_penalties[l].percent != null)
                                                                        {
                                                                            double CanelPercentPrice = 0;
                                                                            CancellationPecentage = Convert.ToDouble(Root[h].rooms[j].rates[k].cancel_penalties[l].percent.Replace("%", ""));
                                                                            //CanelPercent = MainPrice * CancellationPecentage;
                                                                            CancellationCharge = (MainPrice * CancellationPecentage) / 100;
                                                                            //CancellationCharge = CancellationCharge + CanelPercentPrice;
                                                                            CancelCharge = CancellationCharge;
                                                                        }


                                                                        if (CancelCharge > MainPrice)
                                                                            CancellationCharge = MainPrice;


                                                                        DateTime CancelDate = DateTime.Now.Date;
                                                                        //CancelDate = Convert.ToDateTime(Start).AddDays(1).Date;
                                                                        CancelDate = Convert.ToDateTime(Start);
                                                                        DateTime NowDate = DateTime.Now.Date;
                                                                        if (CancelDate <= DateTime.Now.Date)
                                                                            CancelDate = DateTime.Now.Date;
                                                                        else
                                                                            CancelDate = CancelDate.AddDays(CutOffDays).Date;

                                                                        if (CancelDate.AddDays(-1) <= DateTime.Now.Date)
                                                                            CancelDate = DateTime.Now.Date;

                                                                        DateTime LastStart1 = DateTime.Now.Date;
                                                                        DateTime NextStartDate = DateTime.Now.Date;

                                                                        if (CancellationPolicy > 1 && ForPol && CancellationPolicy != (l + 1))
                                                                        {
                                                                            NextStartDate = Convert.ToDateTime(Root[h].rooms[j].rates[k].cancel_penalties[l + 1].start);
                                                                            NextStartDate = NextStartDate.AddDays(CutOffDays).Date;
                                                                            if (NextStartDate.Date <= DateTime.Now.Date)
                                                                            {
                                                                                ForPol = false;
                                                                                continue;

                                                                            }

                                                                        }

                                                                        if (CancellationPolicy > 1 && CancellationPolicy != (l + 1))
                                                                        {
                                                                            LastStart1 = Convert.ToDateTime(Root[h].rooms[j].rates[k].cancel_penalties[l + 1].start);

                                                                            if (End.Date == LastStart1.Date)
                                                                                End = End.AddDays(-1).Date;

                                                                            End = End.AddDays(CutOffDays).Date; //add on 5/5
                                                                        }

                                                                        if (CancellationPolicy == (l + 1))
                                                                        {
                                                                            End = DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null);
                                                                            // CancelDate = Start;

                                                                            CancelDate = Convert.ToDateTime(Start);

                                                                            if (CancelDate <= DateTime.Now.Date)
                                                                                CancelDate = DateTime.Now.Date;
                                                                            else
                                                                                CancelDate = CancelDate.AddDays(CutOffDays).Date;

                                                                        }
                                                                        if (CancellationPolicy == 1)
                                                                            IsTrue = true;

                                                                        if (CancellationPolicy >= 3 && l == 1)
                                                                        {
                                                                            End = DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null);
                                                                            CancelCharge = MainPrice;
                                                                        }


                                                                        #region Vat
                                                                        double VatOnPolicy = 0;
                                                                        double MainAgentCharge = CancelCharge;
                                                                        double CompanyChargeAmount = CancelCharge;
                                                                        CancelCharge = _markup.ProcessHotelCancelationPrice_WithMarkup(ref MainAgentCharge, SearchParam.Rooms.Count * SearchParam.NoofNights, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), CancellationCharge, isPerce, commonSearch.B2CMarkup, AgentPercentage, AgentMarkupAmount, commonSearch.IsParentId, commonSearch.SubAgentMarkup, ref VatOnPolicy);
                                                                        #endregion
                                                                        if (CancelDate.Date < DateTime.Now.Date)
                                                                            CancelDate = DateTime.Now.Date;

                                                                        if (NowDate < CancelDate && CancelDate.Date > DateTime.Now.Date && IsTrue)
                                                                        {
                                                                            if (IsTrue && l == 0)
                                                                            {
                                                                                double zero = 0;
                                                                                dtCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref zero, dtCancellation.NewRow(), ExpediaCurrencyFactor, Currency, Provider, Convert.ToString(OwnsystemHotelId), Convert.ToString(1),
                                                                            Convert.ToString(RatePlamCode), IsRefundable ? RoomName + " " + BedName : RoomName + " " + BedName + "- Non Refundable", TotalAmenities != "" ? TotalAmenities : "Room Only", DateTime.Now.Date, CancelDate.AddDays(-1), 0, 0, "", VatOnPolicy, "", "NO", "", "", "", ""));
                                                                                dtCancellation.AcceptChanges();
                                                                            }

                                                                            dtCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref CompanyChargeAmount, dtCancellation.NewRow(), ExpediaCurrencyFactor, Currency, Provider, Convert.ToString(OwnsystemHotelId), Convert.ToString(1),
                                                                             Convert.ToString(RatePlamCode), IsRefundable ? RoomName + " " + BedName : RoomName + " " + BedName + "- Non Refundable", TotalAmenities != "" ? TotalAmenities : "Room Only", Convert.ToDateTime(CancelDate), End.Date, MainAgentCharge, CancelCharge, "", VatOnPolicy, "", "NO", "", "", "", ""));
                                                                            dtCancellation.AcceptChanges();


                                                                            IsTrue = true;
                                                                        }
                                                                        else
                                                                        {
                                                                            //if (CancelDate < DateTime.Now.Date)
                                                                            //    CancelDate = DateTime.Now.Date;
                                                                            //DtEAN_Cancellation.Rows.Add(Common.setCancellationPolicyRow(ref CompanyChargeAmount, DtEAN_Cancellation.NewRow(), CurrencyFactor, Currency, SupplierName, ownsystemid, Convert.ToString(1),
                                                                            //                                               Convert.ToString(RatePlamCode), IsRefundable ? RoomName + " " + BedName : RoomName + " " + BedName + "- Non Refundable", TotalAmenities != "" ? TotalAmenities : "Room Only", Convert.ToDateTime(CancelDate),
                                                                            //                                               DateTime.ParseExact(objSearPera.CheckInDate, "dd/MM/yyyy", null), MainAgentCharge, CancelCharge, "", 0, Guid.NewGuid().ToString(), "Yes", "", ""));
                                                                            //DtEAN_Cancellation.AcceptChanges();

                                                                            #region Vat
                                                                            VatOnPolicy = 0;
                                                                            MainAgentCharge = CancellationCharge;
                                                                            CompanyChargeAmount = CancellationCharge;
                                                                            CancelCharge = _markup.ProcessHotelCancelationPrice_WithMarkup(ref MainAgentCharge, SearchParam.Rooms.Count * SearchParam.NoofNights, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), CancellationCharge, isPerce, commonSearch.B2CMarkup, AgentPercentage, AgentMarkupAmount, commonSearch.IsParentId, commonSearch.SubAgentMarkup, ref VatOnPolicy);
                                                                            #endregion

                                                                            if (CancelDate < DateTime.Now.Date)
                                                                                CancelDate = DateTime.Now.Date;
                                                                            dtCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref CancellationCharge, dtCancellation.NewRow(), ExpediaCurrencyFactor, Currency, Provider, Convert.ToString(OwnsystemHotelId), Convert.ToString(1),
                                                                                                               Convert.ToString(RatePlamCode), IsRefundable ? RoomName + " " + BedName : RoomName + " " + BedName + "- Non Refundable", TotalAmenities != "" ? TotalAmenities : "Room Only", Convert.ToDateTime(CancelDate),
                                                                                                               End.Date, MainAgentCharge, CancelCharge, "", 0, Guid.NewGuid().ToString(), "Yes", "", ""));
                                                                            dtCancellation.AcceptChanges();

                                                                            IsTrue = false;

                                                                            if (CancellationPolicy >= 3 && l == 1)
                                                                                break;

                                                                        }

                                                                    }
                                                                }
                                                                else
                                                                {


                                                                    #region Vat
                                                                    double VatOnPolicy = 0;
                                                                    double MainAgentCharge = MainPrice;
                                                                    double CompanyChargeAmount = MainPrice;
                                                                    double CancelCharge = _markup.ProcessHotelCancelationPrice_WithMarkup(ref MainAgentCharge, SearchParam.Rooms.Count * SearchParam.NoofNights, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), MainPrice, isPerce, commonSearch.B2CMarkup, AgentPercentage, AgentMarkupAmount, commonSearch.IsParentId, commonSearch.SubAgentMarkup, ref VatOnPolicy);
                                                                    #endregion

                                                                    dtCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref MainPrice, dtCancellation.NewRow(), ExpediaCurrencyFactor, _supplierConfig.Expedia.EANCurrency, Provider, Convert.ToString(OwnsystemHotelId), Convert.ToString(1),
                                                                                                                Convert.ToString(RatePlamCode), IsRefundable ? RoomName + " " + BedName : RoomName + " " + BedName + "- Non Refundable", TotalAmenities != "" ? TotalAmenities : "Room Only", Convert.ToDateTime(DateTime.Now.Date),
                                                                                                                DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null), CancelCharge, CancelCharge, "", 0, Guid.NewGuid().ToString(), "Yes", "", ""));
                                                                    dtCancellation.AcceptChanges();

                                                                    break;
                                                                }

                                                                #region Cancellation Policy

                                                            }
                                                            else
                                                            {
                                                                //if (CancelDate < DateTime.Now.Date)
                                                                //    CancelDate = DateTime.Now.Date;

                                                                #region Vat
                                                                double VatOnPolicy = 0;
                                                                double MainAgentCharge = MainPrice;
                                                                double CancelCharge = _markup.ProcessHotelCancelationPrice_WithMarkup(ref MainAgentCharge, SearchParam.Rooms.Count * SearchParam.NoofNights, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), MainPrice, isPerce, commonSearch.B2CMarkup, AgentPercentage, AgentMarkupAmount, commonSearch.IsParentId, commonSearch.SubAgentMarkup, ref VatOnPolicy);
                                                                #endregion

                                                                dtCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref MainPrice, dtCancellation.NewRow(), ExpediaCurrencyFactor, _supplierConfig.Expedia.EANCurrency, Provider, Convert.ToString(OwnsystemHotelId), Convert.ToString(1),
                                                                                                                 Convert.ToString(RatePlamCode), IsRefundable ? RoomName + " " + BedName : RoomName + " " + BedName + "- Non Refundable", TotalAmenities != "" ? TotalAmenities : "Room Only", Convert.ToDateTime(DateTime.Now.Date),
                                                                                                                 DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null), CancelCharge, CancelCharge, "", 0, Guid.NewGuid().ToString(), "Yes", "", ""));
                                                                dtCancellation.AcceptChanges();
                                                            }
                                                            #endregion

                                                            #endregion
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                        #region Tables Merge

                                        Options ops = (await _supplierRes.GetCommonRoomForSuppliers(GiataId, commonSearch.tokenData, commonSearch.advancedOptions, FinalMinPrice, drXmlHotel, dtHotelRoom, dtCancellation, DtRoomRateBreackUp, SearchParam));
                                        if (ops != null)
                                            options.Add(ops);
                                        #endregion
                                    }

                                    #endregion
                                }
                                #endregion                                
                            }
                            #endregion
                            #endregion


                        }
                        #endregion

                    }

                }
                else
                {
                    ItemEN.ResponseStatus = 204;
                }
                return options;
            }
            catch (Exception ex)
            {
                ItemEN.ResponseStatus = 404;
                await _exception.Error(ex);
                ItemEN.ResponseType = "NoCount";
                return options;
            }
            finally
            {
                #region ResProcessTimer
                try
                {
                    timer_room_ean.Stop();
                    ItemEN.CodeProcessingTimeSupplier = timer_room_ean.ElapsedMilliseconds;
                    if (SearchParam != null && ResProccessTimerLogActive == "1" && timer_room_ean.ElapsedMilliseconds > Convert.ToInt32(ResProccessTimeValueforSaveLog))
                    {
                       
                        SearchParam.SupplierTime += ",|Finish:" + Provider + " Process Response  " + "|Time :" + timer_room_ean.Elapsed.ToString();
                        _ = Task.Run(() => _commonMethod.FireAway_SQL_ResPorcessTimeLog("", timer_room_ean.Elapsed.ToString(), Provider, SearchParam.SearchKey, 1, SearchParam.AgentID));
                    }
                }
                catch (Exception x)
                {
                    await _exception.Error(x);
                }
                #endregion
            }
        }

        

        public bool MealNameCheck(string EANMealName)
        {
            switch (EANMealName.ToLower())
            {
                case "free breakfast":
                case "all meals":
                case "continental breakfast":
                case "full breakfast":
                case "english breakfast":
                case "free lunch":
                case "free dinner":
                case "food/beverage credit":
                case "all-inclusive (food/beverages/snacks)":
                case "continental breakfast for 2":
                case "breakfast for 2":
                case "2-for-1 buffet":
                case "breakfast buffet":
                case "half board":
                case "full board":
                case "free breakfast for 1 adult":
                case "free dinner for 1 adult":
                case "breakfast for 1":
                case "in-room dinner":
                case "japanese-style breakfast":
                case "taiwanese-style breakfast":
                case "vegetarian dinner":
                case "chinese-style dinner":
                case "kaiseki dinner":
                case "kaiseki seafood dinner":
                case "kaiseki meat dinner":
                case "kaiseki vegetarian dinner":
                case "buffet dinner":
                case "half board (chinese-style dinner)":
                case "half board (buffet dinner)":
                case "half board (kaiseki dinner)":

                    return true;
                default:
                    return false;
            }
        }

        #region Recheck



        public async Task<ResponseRecheck> Get_EAN_PropertyContent_WithPreBook_Async(bool HitPrebook, string HotelId, string platform, string echotoken, string Sign, string ApiKey, string Version, string URL, string SupplierName, DataTable dtROE, HotelSearchParameter SearchPara, double CompanyBuyingPrices_EANB2B, int AgentId, string RoomTypeID, string IP, TokenData tokenData, double Tolerance)
        {
            ResponseRecheck eANResponseResultNew = new ResponseRecheck();

            string Response = "";
            string Searchkey = "";
            //string RoomTypeID = "";
            string ENresponsetype = "Success";
            string ENtime = "";
            string ENError = "";
            string ChildSupplierEAN = "";
            string ResponseData_Ex_EAN = "";
            string ereqUrl = "";

            double CurrencyFactorEAN = 1;
            try
            {
                #region PropertyContent API
                //string ResponseData_Exx = "";
                ResponseDto ResponseData_Exx = new ResponseDto();
                if (true)// (SupplierName.ToLower() == "ean_b2bv3" || SupplierName.ToLower() == "ean_pkgv3")
                    //ResponseData_Exx = GetEANResponse(URL + _supplierConfig.Expedia.EAN_VersionV3 + "content?language=en-US&supply_source=expedia&property_id=" + HotelId, 60000, SupplierName, Sign, ApiKey, echotoken, platform, "", "", "", IP);
                    ResponseData_Exx = await GetEANResponse(URL + _supplierConfig.Expedia.EAN_VersionV3 + "content?language=en-US&supply_source=expedia&property_id=" + HotelId, 60000, SupplierName, Sign, ApiKey, echotoken, platform, "GET", "", "", IP);
                else
                    ResponseData_Exx = await GetEANResponse(URL + _supplierConfig.Expedia.EAN_Version + "content?language=en-US&property_id=" + HotelId, 60000, SupplierName, Sign, ApiKey, echotoken, platform, "GET", "", "", IP);

                //if (ResponseData_Exx != "" || ResponseData_Exx != null)
                if (ResponseData_Exx.Response != "" || ResponseData_Exx.Response != null)
                {
                    //ResponseData_Exx = ResponseData_Exx.Replace(HotelId, "HotelCode");
                    ResponseData_Exx.Response = ResponseData_Exx.Response.Replace(HotelId, "HotelCode");
                    RootObjectPropertyContent_EAN Root = Newtonsoft.Json.JsonConvert.DeserializeObject<RootObjectPropertyContent_EAN>(ResponseData_Exx.Response);

                    if (Root.HotelCode != null && Root.HotelCode.checkin != null)
                    {
                        if (Root.HotelCode.checkin.begin_time != null)
                            Response = "<b>Begin Time : </b>" + Convert.ToString(Root.HotelCode.checkin.begin_time);
                        if (Root.HotelCode.checkin.end_time != null)
                        {
                            if (Response != "")
                                Response = Response + "<br>" + "<b>End Time : </b>" + Convert.ToString(Root.HotelCode.checkin.end_time);
                            else
                                Response = "<b>End Time : </b>" + Convert.ToString(Root.HotelCode.checkin.end_time);
                        }
                        if (Root.HotelCode.checkin.instructions != null)
                        {
                            if (Response != "")
                                Response = Response + "<br>" + "<b>Instructions : </b>" + Convert.ToString(Root.HotelCode.checkin.instructions);
                            else
                                Response = "<b>Instructions : </b>" + Convert.ToString(Root.HotelCode.checkin.instructions);
                        }
                        if (Root.HotelCode.checkin.special_instructions != null)
                        {
                            if (Response != "")
                                Response = Response + "<br>" + "<b>Special Instructions : </b>" + Convert.ToString(Root.HotelCode.checkin.special_instructions);
                            else
                                Response = "<b>Special Instructions </b>: " + Convert.ToString(Root.HotelCode.checkin.special_instructions);
                        }
                        if (Root.HotelCode.checkin.min_age != null)
                        {
                            if (Response != "")
                                Response = Response + "<br>" + "<b>Minimum Age : </b>" + Convert.ToString(Root.HotelCode.checkin.min_age);
                            else
                                Response = "<b>Minimum Age : </b>" + Convert.ToString(Root.HotelCode.checkin.min_age);
                        }
                    }
                    if (Root.HotelCode.checkout != null)
                    {
                        if (Root.HotelCode.checkout.time != null)
                        {
                            if (Response != "")
                                Response = Response + "<br>" + "<b>Check Out Time : </b>" + Convert.ToString(Root.HotelCode.checkout.time);
                            else
                                Response = "<b>Check Out Time : </b>" + Convert.ToString(Root.HotelCode.checkout.time);
                        }
                    }
                    if (Root.HotelCode.fees != null)
                    {
                        if (Root.HotelCode.fees.mandatory != null)
                        {
                            if (Response != "")
                                Response = Response + "<br>" + "<b>Mandatory Fees : </b>" + Convert.ToString(Root.HotelCode.fees.mandatory);
                            else
                                Response = "<b>Mandatory Fees : </b>" + Convert.ToString(Root.HotelCode.fees.mandatory);
                        }
                        if (Root.HotelCode.fees.optional != null)
                        {
                            if (Response != "")
                                Response = Response + "<br>" + "<b>Optional Fees : </b>" + Convert.ToString(Root.HotelCode.fees.optional);
                            else
                                Response = "<b>Optional Fees : </b>" + Convert.ToString(Root.HotelCode.fees.optional);
                        }
                    }
                    if (Root.HotelCode.policies != null)
                    {
                        if (Root.HotelCode.policies.know_before_you_go != null)
                        {
                            if (Response != "")
                                Response = Response + "<br>" + "<b>Know Before You Go : </b>" + Convert.ToString(Root.HotelCode.policies.know_before_you_go);
                            else
                                Response = "<b>Know Before You Go : </b>" + Convert.ToString(Root.HotelCode.policies.know_before_you_go);
                        }
                    }
                    if (Root.HotelCode.all_inclusive != null)
                    {
                        if (Root.HotelCode.all_inclusive.details != null)
                        {
                            if (Response != "")
                                Response = Response + "<br>" + "<b>All Inclusive Details : </b>" + Convert.ToString(Root.HotelCode.all_inclusive.details);
                            else
                                Response = "<b>All Inclusive Details : </b>" + Convert.ToString(Root.HotelCode.all_inclusive.details);
                        }
                    }
                }
                #endregion

                if (HitPrebook)
                {
                    #region PreBook API


                    string Token = getPriceRequestEAN(RoomTypeID, "");
                    //string URL = URL;// Convert.ToString(XPDetails[0]["WebsiteURL"]);

                    ereqUrl = URL + Token;

                    //string Sign = Convert.ToString(XPDetails[0]["UserName"]);
                    //string ApiKey = Convert.ToString(XPDetails[0]["Password"]);
                    //string Version = Convert.ToString(XPDetails[0]["version"]);
                    //string platform = Common.GetUserPlatform(advancedoptions.CustomerIpAddress);
                    string echoToken = Guid.NewGuid().ToString();

                    System.Diagnostics.Stopwatch timerroomprocess = new System.Diagnostics.Stopwatch();
                    timerroomprocess.Start();

                    ResponseDto ResponseData_Ex = new ResponseDto();

                    //ResponseData_Ex = await GetEANResponseEANAsync(ereqUrl, 60000, ChildSupplierEAN, Sign, ApiKey, echoToken, platform, "GET", "", Searchkey);
                    ResponseData_Ex = await GetEANResponse(ereqUrl, 60000, ChildSupplierEAN, Sign, ApiKey, echoToken, platform, "GET", "", Searchkey, IP);
                    ResponseData_Ex_EAN = ResponseData_Ex.Response;
                    ENError = ResponseData_Ex.Error;
                    timerroomprocess.Stop();
                    ENtime = timerroomprocess.Elapsed.ToString();

                    if (ResponseData_Ex_EAN.ToLower().Contains("eanwserror") || ResponseData_Ex_EAN.ToLower().Contains("error"))
                    {
                        ENresponsetype = "GeneralError";
                        try
                        {
                            _ = Task.Run(() => _exception.getException_v1("1", "Ean PriceCheck", ResponseData_Ex_EAN, "XMLOUT", Searchkey, "Recheck"));
                        }
                        catch (Exception ex)
                        {
                            _ = Task.Run(() => _exception.ErrorNew(ex, _LogAPI.Recheck, Searchkey));
                        }
                        //continue;


                        eANResponseResultNew.Response = Response;
                        eANResponseResultNew.ISBooking = ENresponsetype;

                        return eANResponseResultNew;
                    }
                    if (ResponseData_Ex.Response != "")
                    {
                        try
                        {
                            //string EANCARDID = Convert.ToString(dsCartDetails.Tables[1].DefaultView[0]["UserCartId"]);
                            //DataRow[] drEAN = dsCartDetails.Tables[1].Select("UserCartId='" + EANCARDID + "'");

                            //var VarDT = drEAN.AsEnumerable().GroupBy(row => new
                            //{
                            //    NoOfAdults = row.Field<Int32>("NoOfAdults"),
                            //    NoOfChild = row.Field<Int32>("NoOfChild"),
                            //    ChildAge1 = row.Field<Int32>("ChildAge1"),
                            //    ChildAge2 = row.Field<Int32>("ChildAge2")

                            //}).Select(n => new { NoofAdults = n.Key.NoOfAdults, NoofChild = n.Key.NoOfChild, ChildAge = n.ToList(), Count = n.Count(), ChildAge1 = n.Key.ChildAge1, ChildAge2 = n.Key.ChildAge2 }).ToList();

                            var VarDT = SearchPara.Rooms.GroupBy(x => new { x.NoofAdults, x.NoofChild, x.Child1Age, x.Child2Age }).Select(n => new { Child1Age = n.Key.Child1Age, Child2Age = n.Key.Child2Age, NoofAdults = n.Key.NoofAdults, NoofChild = n.Key.NoofChild, ChildAge = n.ToList(), Count = n.Count() }).ToList();

                            List<string> Occupancy1 = new List<string>();
                            foreach (var item in VarDT)//For each Item
                            {
                                string TotalOcc = "";
                                //for (int i = 0; i < item.Count; i++)
                                {
                                    int ADT = 0;
                                    string CHD = "";
                                    ADT = item.NoofAdults;
                                    switch (item.NoofChild)
                                    {
                                        case 0:
                                            break;
                                        case 1:
                                            CHD = "-" + item.Child1Age;
                                            break;
                                        case 2:
                                            CHD = "-" + item.Child1Age;
                                            CHD = CHD + "," + item.Child2Age;
                                            break;
                                    }
                                    TotalOcc = ADT.ToString() + CHD;
                                    Occupancy1.Add(TotalOcc + "$$$" + item.Count);
                                }
                            }
                            EAN_Pro.RootObject1_EAN Root = Newtonsoft.Json.JsonConvert.DeserializeObject<EAN_Pro.RootObject1_EAN>(ResponseData_Ex_EAN);

                            if (Root.occupancy_pricing != null)
                            {
                                #region Ean Profit
                                int EANProfit = 55;

                                try
                                {
                                    SqlParameter[] paraean = new SqlParameter[]{
                                                                                        };
                                    DataTable Tokendata = _dLConnection.GetDataTable("getEanProfit", paraean);

                                    EANProfit = Convert.ToInt16(Tokendata.Rows[0]["Profit"]);
                                }
                                catch (Exception)
                                {
                                    EANProfit = 55;
                                }
                                #endregion

                                //object Occupancy = Root.occupancy_pricing;
                                //JToken[] asdad = Newtonsoft.Json.Linq.JToken.Parse(Occupancy.ToString()).ToArray();
                                string Status = Root.status;
                                string BookLink = Root.links.book.href;
                                string BookMethod = Root.links.book.method;
                                BookLink = BookLink + "$$$" + BookMethod;
                                //HotelXML_Suppliers.GetEANBookLinkUpdate(BookLink, UserCartID_EAN);

                                if (Status.ToLower() == "matched" || Status.ToLower() == "price_changed" || Status.ToLower() == "available")
                                //if (Status.ToLower() == "matched" )
                                {
                                    double MainPrice = 0;
                                    double OurProfit = 0;
                                    for (int x = 0; x < Occupancy1.Count; x++)
                                    {
                                        string Occ = "";
                                        string Count = "";
                                        string OccID = Occupancy1[x];
                                        string[] spitid = OccID.Split(new String[] { "$$$" }, StringSplitOptions.None);
                                        Occ = spitid[0];
                                        Count = spitid[1];
                                        foreach (var kvp in Root.occupancy_pricing)
                                        {
                                            #region DataRaw and Variable Declaration


                                            if (kvp.Key != Occ)
                                            {
                                                //eANResponseResultNew.Response = Response;
                                                //eANResponseResultNew.ENresponsetype = ENresponsetype;

                                                //return eANResponseResultNew;
                                                continue;
                                            }
                                            else
                                            {
                                                double Price = 0;
                                                if (kvp.Value.totals.TryGetValue("inclusive", out var inc) && inc.billable_currency?.value != null)
                                                {
                                                    Price = Convert.ToDouble(inc.billable_currency.value);
                                                    string Billable_Currency = Convert.ToString(inc.billable_currency.currency);
                                                    if (Billable_Currency.ToLower() != tokenData.SiteCurrency.ToLower())
                                                    {
                                                        //DataTable dtInrCurrency = Common.GetCountryCurrency(Billable_Currency);
                                                        //CurrencyFactorEAN = Convert.ToDouble(dtInrCurrency.Rows[0]["AmtToExch"]);
                                                        CurrencyFactorEAN = _commonMethod.GetROEByCurrency(Billable_Currency, dtROE);
                                                        Price = Price * CurrencyFactorEAN;
                                                    }
                                                    else
                                                        Price = Price * CurrencyFactorEAN;
                                                }

                                                double Profit = 0;
                                                if (kvp.Value.totals.TryGetValue("gross_profit", out var gp) && gp.billable_currency?.value != null)
                                                    Profit = Convert.ToDouble(gp.billable_currency.value) * Convert.ToInt32(EANProfit) / 100;

                                                //double Profit = Convert.ToDouble(acc.totals.marketing_fee.billable_currency.value);

                                                Price = Price * Convert.ToInt32(Count);
                                                Profit = Profit * Convert.ToInt32(Count);


                                                MainPrice = MainPrice + Price;

                                                if (kvp.Value.totals.TryGetValue("marketing_fee", out var mf) && mf.billable_currency?.currency != null)
                                                {
                                                    string Billable_CurrencyProfit = Convert.ToString(mf.billable_currency.currency);
                                                    if (Billable_CurrencyProfit.ToLower() != tokenData.SiteCurrency.ToLower())
                                                    {
                                                        //DataTable dtInrCurrency = Common.GetCountryCurrency(Billable_CurrencyProfit);
                                                        //double CurrencyFactorEAN2 = Convert.ToDouble(dtInrCurrency.Rows[0]["AmtToExch"]);
                                                        CurrencyFactorEAN = _commonMethod.GetROEByCurrency(Billable_CurrencyProfit, dtROE);
                                                        Profit = Profit * CurrencyFactorEAN;
                                                    }
                                                    else
                                                        Profit = Profit * CurrencyFactorEAN;
                                                }


                                                OurProfit = OurProfit + Profit;

                                            }
                                            #endregion
                                        }
                                    }
                                    #region If Price Update Block
                                    MainPrice = MainPrice - OurProfit;
                                    //MainPrice = MainPrice + 10; //#### Temp
                                    //double Tolorance = (CompanyBuyingPrices_EANB2B / 100) * 1;
                                    double Tolorance = (CompanyBuyingPrices_EANB2B / 100) * Tolerance; //Tolerance added dynamic by jay on 180625
                                    double TolorancePrice = Tolorance + CompanyBuyingPrices_EANB2B;
                                    ResponseData_Ex_EAN = "CompanyBuyingPrices_EAN=" + CompanyBuyingPrices_EANB2B + " Tolorance=" + Tolorance + " TolorancePrice=" + TolorancePrice + " MainPrice=" + MainPrice + " #EANResponse : " + ResponseData_Ex_EAN;
                                    if (_commonMethod.TruncateDecimalPlacesNew(TolorancePrice) < _commonMethod.TruncateDecimalPlacesNew(MainPrice))
                                    {
                                        ENresponsetype = "GeneralError";

                                        try
                                        {
                                            _ = Task.Run(() => _exception.getException_v1("2", "CompanyBuyingPrices_EAN=" + CompanyBuyingPrices_EANB2B + " Tolorance=" + Tolorance + " TolorancePrice=" + TolorancePrice + " MainPrice=" + MainPrice, ResponseData_Ex.Response == "" ? ENError : ResponseData_Ex.Response, "XMLOUT", Searchkey, "Recheck"));
                                        }
                                        catch (Exception ex)
                                        {
                                            _ = Task.Run(() => _exception.ErrorNew(ex, _LogAPI.Recheck, Searchkey));

                                        }
                                    }


                                    if (MainPrice == 0)
                                    {
                                        ENresponsetype = "GeneralError";
                                        try
                                        {
                                            _ = Task.Run(() => _exception.getException_v1("3", "MainPrice=" + Convert.ToString(MainPrice), ResponseData_Ex.Response == "" ? ENError : ResponseData_Ex.Response, "XMLOUT", Searchkey, "Recheck"));
                                        }
                                        catch (Exception ex)
                                        {
                                            _ = Task.Run(() => _exception.ErrorNew(ex, _LogAPI.Recheck, Searchkey));

                                        }
                                    }
                                    #endregion


                                }
                                else if (Status.ToLower() == "sold_out")
                                {
                                    ENresponsetype = "GeneralError";
                                    try
                                    {
                                        _ = Task.Run(() => _exception.getException_v1("4", "Status=" + Convert.ToString(Status), ResponseData_Ex.Response == "" ? ENError : ResponseData_Ex.Response, "XMLOUT", Searchkey, "Recheck"));
                                    }
                                    catch (Exception ex)
                                    {
                                        _ = Task.Run(() => _exception.ErrorNew(ex, _LogAPI.Recheck, Searchkey));
                                    }
                                }
                                else
                                {
                                    ENresponsetype = "GeneralError";
                                    try
                                    {
                                        _ = Task.Run(() => _exception.getException_v1("5", "Status=" + Convert.ToString(Status), ResponseData_Ex.Response == "" ? ENError : ResponseData_Ex.Response, "XMLOUT", Searchkey, "Recheck"));
                                    }
                                    catch (Exception ex)
                                    {
                                        _ = Task.Run(() => _exception.ErrorNew(ex, _LogAPI.Recheck, Searchkey));

                                    }
                                }
                            }
                            else
                            {
                                ENresponsetype = "GeneralError";
                                try
                                {
                                    _ = Task.Run(() => _exception.getException_v1("6", "Root.occupancy_pricing=null", ResponseData_Ex.Response == "" ? ENError : ResponseData_Ex.Response, "XMLOUT", Searchkey, "Recheck"));
                                }
                                catch (Exception ex)
                                {
                                    _ = Task.Run(() => _exception.ErrorNew(ex, _LogAPI.Recheck, Searchkey));

                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _ = Task.Run(() => _exception.ErrorNew(ex, _LogAPI.Recheck, Searchkey));
                            ENresponsetype = "GeneralError";
                            ENError = ENError + ex.Message;
                            try
                            {
                                _ = Task.Run(() => _exception.getException_v1("6.1", ENError, ResponseData_Ex.Response, "XMLOUT", Searchkey, "Recheck"));
                            }
                            catch (Exception ex1)
                            {
                                _ = Task.Run(() => _exception.ErrorNew(ex1, _LogAPI.Recheck, Searchkey));
                            }
                        }
                    }
                    else
                    {
                        ENresponsetype = "GeneralError";
                        try
                        {
                            _ = Task.Run(() => _exception.getException_v1("7", "ResponseData_Ex=null", ResponseData_Ex.Response == "" ? ENError : ResponseData_Ex.Response, "XMLOUT", Searchkey, "Recheck"));
                        }
                        catch (Exception ex)
                        {
                            _ = Task.Run(() => _exception.ErrorNew(ex, _LogAPI.Recheck, Searchkey));
                        }
                    }

                    #endregion
                }
            }
            catch (Exception Ex)
            {
                ENresponsetype = "GeneralError";
                _ = Task.Run(() => _exception.ErrorNew(Ex, _LogAPI.Recheck, Searchkey));

                ENError = ENError + Ex.Message;
                try
                {
                    _ = Task.Run(() => _exception.getException_v1("7.1", ENError, "", "XMLOUT", Searchkey, "Recheck"));
                }
                catch (Exception ex)
                {
                    _ = Task.Run(() => _exception.ErrorNew(ex, _LogAPI.Recheck, Searchkey));
                }
            }

            if (HitPrebook)
            {
                #region SupplierPerformanceReport Catch
                try
                {
                    //string abc = _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, ENtime, ENresponsetype, ereqUrl, ResponseData_Ex_EAN, ENError);
                    _ = Task.Run(() => _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, ENtime, ENresponsetype, ereqUrl, ResponseData_Ex_EAN, ENError));
                }
                catch (Exception e)
                {
                    await _exception.ErrorBooking(e, AgentId.ToString(), "", "");
                }
                #endregion
            }



            eANResponseResultNew.Response = Response;
            eANResponseResultNew.ISBooking = ENresponsetype;

            return eANResponseResultNew;

        }


        public async Task<ResponseDto> GetEANResponseEANAsync(string strHost, int timeout, string provider = "", string userName = "", string password = "", string echoToken = "", string platform = "", string isBooking = "GET", string requestString = "", string searchKey = "", string ip = "")
        {
            var result = new ResponseDto();

            try
            {
                string responseContent = "";
                try
                {

                    using (HttpClient client = new HttpClient())
                    {
                        client.Timeout = TimeSpan.FromMilliseconds(timeout);

                        TimeSpan epochTicks = new TimeSpan(new DateTime(1970, 1, 1).Ticks);
                        TimeSpan unixTicks = new TimeSpan(DateTime.UtcNow.Ticks) - epochTicks;
                        double unixTime = (int)unixTicks.TotalSeconds;
                        var toBeHashed = password + userName + unixTime;
                        var bytes = Encoding.UTF8.GetBytes(toBeHashed);

                        string authHeaderValue = "";
                        using (var hash = SHA512.Create())
                        {
                            var hashedInputBytes = hash.ComputeHash(bytes);
                            var hashedInputStringBuilder = new StringBuilder(128);
                            foreach (var b in hashedInputBytes)
                                hashedInputStringBuilder.Append(b.ToString("X2"));
                            var Esignature = hashedInputStringBuilder.ToString();
                            authHeaderValue = "EAN APIKey=" + password + ",Signature=" + Esignature + ",timestamp=" + unixTime;
                        }


                        string[] espitid = platform.Split(new string[] { "$$" }, StringSplitOptions.None);
                        string ePlatFormNew = espitid[0];
                        string eIP = espitid[1];



                        HttpMethod method = new HttpMethod(isBooking.ToUpper());
                        var request = new HttpRequestMessage(method, strHost);

                        if (isBooking.ToUpper() == "POST")
                            request.Content = new StringContent(requestString, System.Text.Encoding.UTF8);
                        else if (isBooking.ToLower() == "DELETE")
                            request.Method = HttpMethod.Delete;


                        request.Headers.Add("Authorization", authHeaderValue);
                        request.Headers.Add("User-Agent", ePlatFormNew);
                        request.Headers.Add("Customer-Session-Id", echoToken);
                        request.Headers.Add("Customer-Ip", eIP);
                        request.Headers.Add("Accept-Encoding", "gzip");
                        request.Headers.Add("ContentType", "application/x-www-form-urlencoded");


                        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                        HttpResponseMessage response = await client.SendAsync(request);
                        //response.EnsureSuccessStatusCode();

                        responseContent = await response.Content.ReadAsStringAsync();
                    }
                }
                catch (HttpRequestException ex)
                {
                    result.Response = "";
                    result.Error = ex.Message;
                    _ = Task.Run(() => _exception.Error(ex, searchKey, result.Error));
                    return result;
                }
                catch (TaskCanceledException ex)
                {
                    result.Response = "";
                    result.Error = "Request timed out.";
                    _ = Task.Run(() => _exception.Error(ex, searchKey, result.Error));
                    return result;
                }
                catch (Exception ex)
                {
                    result.Response = "";
                    result.Error = ex.Message;
                    _ = Task.Run(() => _exception.Error(ex, searchKey));
                    return result;
                }


                result.Response = responseContent;
                result.Error = "";
                // return responseContent;
            }
            catch (Exception ex)
            {
                result.Response = "";
                result.Error = ex.Message;
                _ = Task.Run(() => _exception.Error(ex, searchKey));
            }

            return result;
        }

        public async Task<ResponseDto> GetEANResponse(string strHost, Int32 TimeOut, string Provider = "", string UserName = "", string Password = "", string EchoToken = "", string PlatForm = "", string IsBooking = "GET", string RequestString = "", string Searchkey = "", string ip = "")
        {
            ResponseDto responseDto = new ResponseDto();
            string Response = "";
            try
            {
                string Key = "";
                HttpWebRequest Request;
                Request = (HttpWebRequest)HttpWebRequest.Create(strHost);
                Request.Timeout = TimeOut;
                if (EchoToken == "")
                {
                    EchoToken = Guid.NewGuid().ToString();
                }

                TimeSpan epochTicks = new TimeSpan(new DateTime(1970, 1, 1).Ticks);
                TimeSpan unixTicks = new TimeSpan(DateTime.UtcNow.Ticks) - epochTicks;
                double unixTime = (int)unixTicks.TotalSeconds;
                var toBeHashed = Password + UserName + unixTime;
                var bytes = System.Text.Encoding.UTF8.GetBytes(toBeHashed);
                string authHeaderValue = "";
                using (var hash = System.Security.Cryptography.SHA512.Create())
                {
                    var hashedInputBytes = hash.ComputeHash(bytes);
                    var hashedInputStringBuilder = new System.Text.StringBuilder(128);
                    foreach (var b in hashedInputBytes)
                        hashedInputStringBuilder.Append(b.ToString("X2"));
                    var Esignature = hashedInputStringBuilder.ToString();
                    authHeaderValue = "EAN APIKey=" + Password + ",Signature=" + Esignature + ",timestamp=" + unixTime;
                }
                Request.Headers.Add("Authorization", authHeaderValue);

                string[] espitid = PlatForm.Split(new String[] { "$$" }, StringSplitOptions.None);
                string ePlatFormNew = Convert.ToString(espitid[0]);
                ServicePointManager.Expect100Continue = true;
                //ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                //ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12 | SecurityProtocolType.Ssl3;

                string CustomerIP = ip;
                //string CustomerIP1 = Common.GetIPAddress();
                string eIP = Convert.ToString(espitid[1]);
                Request.Method = "GET";
                Request.ContentType = "application/x-www-form-urlencoded";
                //Request.Headers.Add("Test", "standard");
                //Request.UserAgent = "Mozilla/5.0 (Windows NT 6.1; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/78.0.3904.108 Safari/537.36";
                Request.UserAgent = ePlatFormNew;
                Request.Headers.Add("Customer-Session-Id", EchoToken);
                Request.Headers.Add("Customer-Ip", CustomerIP);
                Request.Headers.Add("Accept-Encoding", "gzip");
                Request.Accept = "application/json";
                if (IsBooking.ToLower() == "post")
                {
                    Request.ContentType = "application/json";
                    Request.Method = "POST";
                    //using (Stream postStream = Request.GetRequestStream())
                    //{
                    //    byte[] postBytes = Encoding.ASCII.GetBytes(RequestString);
                    //    postStream.Write(postBytes, 0, postBytes.Length);
                    //    postStream.Flush();
                    //}

                    Stream newStream = null;
                    byte[] data = Encoding.ASCII.GetBytes(RequestString);
                    Request.ContentLength = data.Length;

                    newStream = Request.GetRequestStream();
                    newStream.Write(data, 0, data.Length);

                }
                else if (IsBooking.ToLower() == "delete")
                    Request.Method = "DELETE";

                using (var response = (HttpWebResponse)await Request.GetResponseAsync())
                {
                    Stream streamResponse = response.GetResponseStream();

                    try
                    {
                        if (response.ContentEncoding.ToLower().Contains("gzip"))
                            streamResponse = new System.IO.Compression.GZipStream(streamResponse, System.IO.Compression.CompressionMode.Decompress);
                        else if (response.ContentEncoding.ToLower().Contains("deflate"))
                            streamResponse = new System.IO.Compression.DeflateStream(streamResponse, System.IO.Compression.CompressionMode.Decompress);
                        StreamReader streamReader = new StreamReader(streamResponse);
                        try
                        {
                            Response = await streamReader.ReadToEndAsync();
                        }
                        catch (WebException e)
                        {
                            _ = _exception.Error(e, Searchkey);

                            responseDto.Response = "";
                            return responseDto;
                        }
                        finally
                        {
                            if (streamReader != null)
                                streamReader.Close();
                        }
                    }
                    catch (WebException ex)
                    {
                        _ = _exception.Error(ex, Searchkey);
                        responseDto.Response = "";
                        return responseDto;
                    }
                    finally
                    {
                        if (streamResponse != null)
                        {
                            streamResponse.Close();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _ = _exception.Error(ex, Searchkey);
                responseDto.Response = "";
                return responseDto;
            }

            responseDto.Response = Response;
            return responseDto;
        }

        public async Task<ResponseDto> GetEANResponseBooking(string strHost, Int32 TimeOut, string Provider = "", string UserName = "", string Password = "", string EchoToken = "", string PlatForm = "", string IsBooking = "GET", string RequestString = "", string SearchKey = "", string IP = "")
        {
            ResponseDto responseDto = new ResponseDto();
            string Response = "";
            string Error = "";
            try
            {

                string Key = "";
                HttpWebRequest Request;
                Request = (HttpWebRequest)HttpWebRequest.Create(strHost);
                Request.Timeout = TimeOut;

                Uri myUri = new Uri(strHost);
                string host = myUri.Host;  // host is "www.contoso.com"

                TimeSpan epochTicks = new TimeSpan(new DateTime(1970, 1, 1).Ticks);
                TimeSpan unixTicks = new TimeSpan(DateTime.UtcNow.Ticks) - epochTicks;
                double unixTime = (int)unixTicks.TotalSeconds;
                var toBeHashed = Password + UserName + unixTime;
                var bytes = System.Text.Encoding.UTF8.GetBytes(toBeHashed);
                string authHeaderValue = "";
                using (var hash = System.Security.Cryptography.SHA512.Create())
                {
                    var hashedInputBytes = hash.ComputeHash(bytes);
                    var hashedInputStringBuilder = new System.Text.StringBuilder(128);
                    foreach (var b in hashedInputBytes)
                        hashedInputStringBuilder.Append(b.ToString("X2"));
                    var Esignature = hashedInputStringBuilder.ToString();
                    authHeaderValue = "EAN APIKey=" + Password + ",Signature=" + Esignature + ",timestamp=" + unixTime;
                }
                //Request.Headers.Add("Authorization", authHeaderValue);

                string[] espitid = PlatForm.Split(new String[] { "$$" }, StringSplitOptions.None);
                string ePlatFormNew = Convert.ToString(espitid[0]);
                ServicePointManager.Expect100Continue = true;
                //ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                //ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12 | SecurityProtocolType.Ssl3;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                string CustomerIP = IP;
                //string CustomerIP1 = Common.GetIPAddress();
                string eIP = Convert.ToString(espitid[1]);
                var request = new RestRequest("", Method.Post);

                //request.Method = Method.Post;
                if (RequestString != "")
                {
                    //byte[] data = Encoding.ASCII.GetBytes(RequestString);
                    //request.AddHeader("content-length", data.Length.ToString());
                    //request.AddParameter("application/json", RequestString, ParameterType.RequestBody);
                    request.AddStringBody(RequestString, ContentType.Json);


                }

                var options = new RestClientOptions(strHost)
                {
                    Timeout = TimeSpan.FromMilliseconds(TimeOut)
                };

                var client = new RestClient(options);

                //request.AddHeader("cache-control", "no-cache");
                //request.AddHeader("Connection", "keep-alive");
                //request.AddHeader("Host", "test.ean.com");
                //request.AddHeader("Host", host);
                //request.AddHeader("Postman-Token", "2768f2fc-8350-4173-b647-d6dc49fafbac,1de6ca2b-5c66-4127-adea-1a4d9dd6fba1");
                //request.AddHeader("Cache-Control", "no-cache");
                request.AddHeader("User-Agent", "Mozilla/5.0 (Windows NT 6.1; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/78.0.3904.108 Safari/537.36");
                request.AddHeader("Customer-Session-Id", EchoToken);
                request.AddHeader("Customer-Ip", CustomerIP);
                request.AddHeader("Authorization", authHeaderValue);
                request.AddHeader("Accept-Encoding", "gzip");
                //request.AddHeader("Content-Type", "application/json,{{Content-Type}}");
                request.AddHeader("Content-Type", "application/json");
                //request.AddHeader("Accept", "application/json,application/json");
                request.AddHeader("Accept", "application/json");

                //request.AddParameter("Request", JsonConvert.SerializeObject(RequestString), ParameterType.RequestBody);

                var response = await client.ExecuteAsync(request);

                responseDto.Response = response.Content.ToString();

            }
            catch (Exception ex)
            {
                Error = ex.Message.ToString();
                responseDto.Error = Error;

                _ = _exception.ErrorBooking(ex, "", "", SearchKey);

            }
            return responseDto;
        }


        public string getPriceRequestEAN(string RoomTypeId, string ItemCode)
        {
            try
            {
                string[] spitid = RoomTypeId.Split(new String[] { "$$$" }, StringSplitOptions.None);
                string RoomId = spitid[0].ToString();
                string RateId = spitid[1].ToString();
                string Token = "";
                if (spitid[2] != "" || spitid[2] != null || spitid[2] != string.Empty)
                    Token = spitid[2].ToString();

                return Token;
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
            }
            return string.Empty;
        }



        #endregion

        //prebook 01-07-25

        public static DataTable HotelList()
        {

            DataTable dt = new DataTable();
            dt.Columns.Add("ID", typeof(string));
            dt.Columns.Add("Name", typeof(string));
            dt.Columns.Add("Hcity", typeof(string));
            dt.Columns.Add("Hcountry_code", typeof(string));

            return dt;
        }
        public async Task<DataSet> ReturnEANDatatable(DataSet ds, string XMLHotelCode, string XMLCityCode, string UrlName, string CompanyID, string UserName, string Password, string platform, string provider = "", string IP = "")
        {
            string xmlResponsenew = "";
            try
            {
                string xmlResponse = "";
                string XMLRequest = "/properties/content?language=EN-US&property_id=" + XMLHotelCode + "";
                ResponseDto responseDto = await GetSupplierResponseEANHotelList(XMLRequest, UrlName + XMLRequest, UserName, Password, 5000000, CompanyID, platform, IP);
                //ResponseDto responseDto = await GetEANResponseEANAsync(UrlName + XMLRequest, 60000, provider, UserName, Password, "", platform, "GET", "", "");
                //ResponseDto responseDto = await GetEANResponse(UrlName + XMLRequest, 60000, provider, UserName, Password, "", platform, "GET", "", "");
                xmlResponse = responseDto.Response;


                DataTable Hotel = HotelList();
                var roottt = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(xmlResponse);
                JToken[] asdad = Newtonsoft.Json.Linq.JToken.Parse(xmlResponse.ToString()).ToArray();
                if (asdad.Length > 0)
                {
                    for (int i = 0; i < asdad.GetLength(0); i++)
                    {
                        foreach (var item in asdad[i])
                        {
                            JObject jb = JObject.Parse(Convert.ToString(item));

                            if (jb.Property("business_model") != null) { jb.Property("business_model").Remove(); }
                            if (jb.Property("checkin") != null) { jb.Property("checkin").Remove(); }
                            if (jb.Property("checkout") != null) { jb.Property("checkout").Remove(); }
                            if (jb.Property("fees") != null) { jb.Property("fees").Remove(); }
                            if (jb.Property("policies") != null) { jb.Property("policies").Remove(); }
                            if (jb.Property("attributes") != null) { jb.Property("attributes").Remove(); }
                            if (jb.Property("amenities") != null) { jb.Property("amenities").Remove(); }
                            if (jb.Property("images") != null) { jb.Property("images").Remove(); }
                            if (jb.Property("onsite_payments") != null) { jb.Property("onsite_payments").Remove(); }
                            if (jb.Property("rooms") != null) { jb.Property("rooms").Remove(); }
                            if (jb.Property("rates") != null) { jb.Property("rates").Remove(); }
                            if (jb.Property("dates") != null) { jb.Property("dates").Remove(); }

                            if (jb.Property("themes") != null) { jb.Property("themes").Remove(); }
                            if (jb.Property("statistics") != null) { jb.Property("statistics").Remove(); }
                            xmlResponse = jb.ToString();

                            string BeforeBlockResponse = Newtonsoft.Json.JsonConvert.DeserializeXmlNode(xmlResponse, "root").InnerXml;
                            DataSet RoomResponse = new DataSet();
                            RoomResponse = _commonMethod.GetDataSetFromXMLString(BeforeBlockResponse);
                            ds = RoomResponse;
                        }
                    }
                }
                return ds;
            }
            catch (System.Exception ex)
            {
                return ds;
            }
        }

        public async Task<ResponseDto> GetSupplierResponseEANHotelList(string Xmlrequest, string Url, string username, string password, int Timeout, string UserId, string PlatForm, string IP, string EchoToken = "")
        {
            ResponseDto responseDto = new ResponseDto();

            if (EchoToken == "")
            {
                EchoToken = Guid.NewGuid().ToString();
            }

            #region Authorization

            string authHeaderValue = "";

            TimeSpan epochTicks = new TimeSpan(new DateTime(1970, 1, 1).Ticks);
            TimeSpan unixTicks = new TimeSpan(DateTime.UtcNow.Ticks) - epochTicks;
            double unixTime = (int)unixTicks.TotalSeconds;
            string toBeHashed = password + username + unixTime;
            var bytes = System.Text.Encoding.UTF8.GetBytes(toBeHashed);
            using (var hash = System.Security.Cryptography.SHA512.Create())
            {
                var hashedInputBytes = hash.ComputeHash(bytes);
                var hashedInputStringBuilder = new System.Text.StringBuilder(128);
                foreach (var b in hashedInputBytes)
                    hashedInputStringBuilder.Append(b.ToString("X2"));
                string signature = hashedInputStringBuilder.ToString();
                authHeaderValue = "EAN APIKey=" + password + ",Signature=" + signature + ",timestamp=" + unixTime;
            }
            #endregion Authorization

            string[] espitid = PlatForm.Split(new String[] { "$$" }, StringSplitOptions.None);
            string ePlatFormNew = Convert.ToString(espitid[0]);

            string responseString = "";
            try
            {

                string reqUrl = Url;
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(reqUrl);
                request.Accept = "application/json";
                request.Headers.Add("Accept-Encoding", "gzip, deflate");
                //request.UserAgent = "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_11_6) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/63.0.3239.132 Safari/537.36";
                request.UserAgent = ePlatFormNew;

                request.Method = "GET";
                request.Timeout = Timeout;
                request.ContentType = "application/x-www-form-urlencoded";
                request.Headers.Add("Authorization", authHeaderValue);
                ServicePointManager.Expect100Continue = true;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                try
                {

                    HttpWebResponse Response1 = (HttpWebResponse)await request.GetResponseAsync();
                    Stream st = Response1.GetResponseStream();

                    if (Response1.ContentEncoding.ToLower().Contains("gzip"))
                        st = new System.IO.Compression.GZipStream(st, System.IO.Compression.CompressionMode.Decompress);
                    else if (Response1.ContentEncoding.ToLower().Contains("deflate"))
                        st = new System.IO.Compression.DeflateStream(st, System.IO.Compression.CompressionMode.Decompress);
                    StreamReader stIn = new StreamReader(st, Encoding.Default);
                    responseString = await stIn.ReadToEndAsync();
                    Response1.Dispose();
                }
                catch (WebException webExcp)
                {
                    HttpWebResponse httpResponse = (HttpWebResponse)webExcp.Response;
                    string result = httpResponse.StatusDescription.ToString();
                    string result1 = new StreamReader(webExcp.Response.GetResponseStream()).ReadToEnd();
                    responseDto.Response = "";
                    return responseDto;
                }

            }
            catch (System.Exception ex)
            {
                _ = _exception.Error(ex);
            }

            responseDto.Response = responseString;
            return responseDto;
        }

        //book
        public string EANStringForBookV3(DataSet dsCartDetails, DataTable dtPassengers, int i, int jj, string Ex_Guid, string SupplierName = "")
        {
            try
            {
                //string Email = Convert.ToString(ConfigurationManager.AppSettings["EAN_Eamil"]);

                string BookReq = "";
                List<EANV3BookRequest.Room> RoomObj = new List<EANV3BookRequest.Room>();

                //Phone PhoneObj = new Phone();
                //PhoneObj.country_code = XMLHotelBookingCommon.country_code_Xpedia;
                //PhoneObj.area_code = XMLHotelBookingCommon.area_code_Xpedia;
                //PhoneObj.number = XMLHotelBookingCommon.number_Xpedia;

                for (int j = 0; j < dsCartDetails.Tables[1].DefaultView.Count; j++)
                {
                    //string Prefix = "", Name = "", SurName = "";
                    //dtPassengers.DefaultView.RowFilter = "PickUp='" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomTypeId"]) + "' and (RoomNo=" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomNumber"]) + " and PaxType='Adult')";
                    //dtPassengers.DefaultView.RowFilter = "(RoomNo=" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomNumber"]) + " and PaxType='Adult')";
                    //dtPassengers.DefaultView.Sort = "IsLead DESC";
                    //int Islead = Convert.ToInt32(dtPassengers.DefaultView[0]["islead"]);
                    //if (Islead == 1)

                    string Prefix = "", Name = "", SurName = "";
                    dtPassengers.DefaultView.RowFilter = "PickUp='" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomTypeId"]) + "' and (RoomNo=" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomNumber"]) + " and PaxType='Adult')";
                    dtPassengers.DefaultView.Sort = "IsLead DESC";
                    int Islead = Convert.ToInt32(dtPassengers.DefaultView[0]["islead"]);
                    if (Islead == 1)
                    {
                        Prefix = Convert.ToString(dtPassengers.DefaultView[0]["Prefix"]).Replace(".", "");
                        Name = Convert.ToString(dtPassengers.DefaultView[0]["FirstName"]).Replace(".", "");
                        SurName = Convert.ToString(dtPassengers.DefaultView[0]["LastName"]).Replace(".", "");
                    }
                    else
                    {
                        Prefix = Convert.ToString(dtPassengers.DefaultView[0]["Prefix"]).Replace(".", "");
                        Name = Convert.ToString(dtPassengers.DefaultView[0]["FirstName"]).Replace(".", "");
                        SurName = Convert.ToString(dtPassengers.DefaultView[0]["LastName"]).Replace(".", "");
                    }

                    for (int x = 0; x < dtPassengers.DefaultView.Count; x++)
                    {
                        if (x == 0)
                        {
                            //Prefix = Convert.ToString(dtPassengers.DefaultView[0]["Prefix"]).Replace(".", "");
                            //Name = Convert.ToString(dtPassengers.DefaultView[0]["FirstName"]).Replace(".", "");
                            //SurName = Convert.ToString(dtPassengers.DefaultView[0]["LastName"]).Replace(".", "");

                            EANV3BookRequest.Room RoomObjNew = new EANV3BookRequest.Room();
                            //RoomObjNew.title = Prefix;
                            RoomObjNew.given_name = Name;
                            RoomObjNew.family_name = SurName;
                            //RoomObjNew.email = "chintan@technoheaven.net";
                            //RoomObjNew.email = XMLHotelBookingCommon.email_Xpedia;
                            //RoomObjNew.phone = PhoneObj;
                            RoomObjNew.smoking = false;
                            RoomObj.Add(RoomObjNew);
                        }
                    }
                }

                EANV3BookRequest.Address AddressObj = new EANV3BookRequest.Address();
                //AddressObj.line_1 = "555 1st St";
                AddressObj.line_1 = _supplierConfig.Expedia.address1_Xpedia;
                //AddressObj.city = "Seattle";
                AddressObj.city = _supplierConfig.Expedia.city_Xpedia;
                //AddressObj.state_province_code = "WA";
                //AddressObj.postal_code = "98121";
                AddressObj.postal_code = _supplierConfig.Expedia.postalCode_Xpedia;
                //AddressObj.country_code = "US";
                AddressObj.country_code = _supplierConfig.Expedia.countryCode_Xpedia;

                EANV3BookRequest.BillingContact BillingContactObj = new EANV3BookRequest.BillingContact();
                //BillingContactObj.given_name = "John";
                BillingContactObj.given_name = _supplierConfig.Expedia.firstName_Xpedia;
                //BillingContactObj.family_name = "Smith";
                BillingContactObj.family_name = _supplierConfig.Expedia.lastName_Xpedia;
                //BillingContactObj.email = "chintan@technoheaven.net";
                //BillingContactObj.email = XMLHotelBookingCommon.email_Xpedia;
                //BillingContactObj.phone = "919099493112";
                //BillingContactObj.phone = XMLHotelBookingCommon.homePhone_Xpedia;
                //BillingContactObj.phone = PhoneObj;
                BillingContactObj.address = AddressObj;

                List<EANV3BookRequest.Payment> PaymentObj = new List<EANV3BookRequest.Payment>();
                EANV3BookRequest.Payment PaymentObjNew = new EANV3BookRequest.Payment();

                PaymentObjNew.type = "affiliate_collect";
                //PaymentObjNew.card_type = "VI";
                //PaymentObjNew.number = "4111111111111111";
                //PaymentObjNew.security_code = "123";
                //PaymentObjNew.expiration_month = "08";
                //PaymentObjNew.expiration_year = "2025";

                //PaymentObjNew.card_type = "";
                //PaymentObjNew.number = "";
                //PaymentObjNew.security_code = "";
                //PaymentObjNew.expiration_month = "";
                //PaymentObjNew.expiration_year = "";

                PaymentObjNew.billing_contact = BillingContactObj;
                PaymentObj.Add(PaymentObjNew);

                EANV3BookRequest.RootObject RootObjBook = new EANV3BookRequest.RootObject();
                RootObjBook.affiliate_reference_id = Ex_Guid;
                RootObjBook.rooms = RoomObj;
                RootObjBook.payments = PaymentObj;

                EANV3BookRequest.Phone PhoneObj = new EANV3BookRequest.Phone();
                PhoneObj.country_code = _supplierConfig.Expedia.country_code_Xpedia;
                PhoneObj.area_code = _supplierConfig.Expedia.area_code_Xpedia;
                PhoneObj.number = _supplierConfig.Expedia.number_Xpedia;
                RootObjBook.phone = PhoneObj;
                RootObjBook.email = _supplierConfig.Expedia.email_Xpedia;

                BookReq = Newtonsoft.Json.JsonConvert.SerializeObject(RootObjBook);
                return BookReq.ToString();
            }
            catch (Exception ex)
            {
                _ = _exception.Error(ex);
            }
            return string.Empty;
        }

        public string EANStringForBookV3_newversion(BookRequest_newversion bookRequest, DataTable dtPassengers, int i, string Ex_Guid, string SupplierName = "")
        {
            try
            {

                string BookReq = "";
                List<EANV3BookRequest.Room> RoomObj = new List<EANV3BookRequest.Room>();



                for (int j = 0; j < bookRequest.userCart.UserCartDetails.Count; j++)
                {

                    string Prefix = "", Name = "", SurName = "";
                    dtPassengers.DefaultView.RowFilter = "PickUp='" + Convert.ToString(bookRequest.userCart.UserCartDetails[j].RoomTypeId) + "' and (RoomNo=" + Convert.ToString(bookRequest.userCart.UserCartDetails[j].RoomNo) + " and PaxType='Adult')";
                    dtPassengers.DefaultView.Sort = "IsLead DESC";
                    int Islead = Convert.ToInt32(dtPassengers.DefaultView[0]["islead"]);
                    if (Islead == 1)
                    {
                        Prefix = Convert.ToString(dtPassengers.DefaultView[0]["Prefix"]).Replace(".", "");
                        Name = Convert.ToString(dtPassengers.DefaultView[0]["FirstName"]).Replace(".", "");
                        SurName = Convert.ToString(dtPassengers.DefaultView[0]["LastName"]).Replace(".", "");
                    }
                    else
                    {
                        Prefix = Convert.ToString(dtPassengers.DefaultView[0]["Prefix"]).Replace(".", "");
                        Name = Convert.ToString(dtPassengers.DefaultView[0]["FirstName"]).Replace(".", "");
                        SurName = Convert.ToString(dtPassengers.DefaultView[0]["LastName"]).Replace(".", "");
                    }

                    for (int x = 0; x < dtPassengers.DefaultView.Count; x++)
                    {
                        if (x == 0)
                        {

                            EANV3BookRequest.Room RoomObjNew = new EANV3BookRequest.Room();
                            //RoomObjNew.title = Prefix;
                            RoomObjNew.given_name = Name;
                            RoomObjNew.family_name = SurName;
                            RoomObjNew.smoking = false;
                            RoomObj.Add(RoomObjNew);
                        }
                    }
                }

                EANV3BookRequest.Address AddressObj = new EANV3BookRequest.Address();
                AddressObj.line_1 = _supplierConfig.Expedia.address1_Xpedia;
                AddressObj.city = _supplierConfig.Expedia.city_Xpedia;
                AddressObj.postal_code = _supplierConfig.Expedia.postalCode_Xpedia;
                //AddressObj.country_code = "US";
                AddressObj.country_code = _supplierConfig.Expedia.countryCode_Xpedia;

                EANV3BookRequest.BillingContact BillingContactObj = new EANV3BookRequest.BillingContact();
                //BillingContactObj.given_name = "John";
                BillingContactObj.given_name = _supplierConfig.Expedia.firstName_Xpedia;
                //BillingContactObj.family_name = "Smith";
                BillingContactObj.family_name = _supplierConfig.Expedia.lastName_Xpedia;
                BillingContactObj.address = AddressObj;

                List<EANV3BookRequest.Payment> PaymentObj = new List<EANV3BookRequest.Payment>();
                EANV3BookRequest.Payment PaymentObjNew = new EANV3BookRequest.Payment();

                PaymentObjNew.type = "affiliate_collect";

                PaymentObjNew.billing_contact = BillingContactObj;
                PaymentObj.Add(PaymentObjNew);

                EANV3BookRequest.RootObject RootObjBook = new EANV3BookRequest.RootObject();
                RootObjBook.affiliate_reference_id = Ex_Guid;
                RootObjBook.rooms = RoomObj;
                RootObjBook.payments = PaymentObj;

                EANV3BookRequest.Phone PhoneObj = new EANV3BookRequest.Phone();
                PhoneObj.country_code = _supplierConfig.Expedia.country_code_Xpedia;
                PhoneObj.area_code = _supplierConfig.Expedia.area_code_Xpedia;
                PhoneObj.number = _supplierConfig.Expedia.number_Xpedia;
                RootObjBook.phone = PhoneObj;
                RootObjBook.email = _supplierConfig.Expedia.email_Xpedia;

                BookReq = Newtonsoft.Json.JsonConvert.SerializeObject(RootObjBook);
                return BookReq.ToString();
            }
            catch (Exception ex)
            {
                _ = _exception.Error(ex);
            }
            return string.Empty;
        }
    }
}
