using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using XConnect.CommonUtilities;
using XConnect.CommonUtilities.Interface;
using XConnect.ErrorLogHandler.Interface;
using XConnect.Expedia;
using XConnect.Markup.Interface;
using XConnect.Models;
using XConnect.Models.Hotel;
using XConnect.Supplier.Juniper.Interface;
using static Azure.Core.HttpHeader;
using static XConnect.Models.ErrorLog;
using static XConnect.Supplier.Agoda.Agoda_NewESS;
using static XConnect.Supplier.Booking.Com.preebookrs;
using Options = XConnect.Models.Options;

namespace XConnect.Supplier.Juniper
{
    public class JuniperRepo : IJuniper
    {

        private readonly ICommonMethod _commonMethod;
        private readonly IException _exception;
        private readonly IMarkup _markup;
        private readonly ISupplierRes _supplierRes;
        private readonly KeySettings _keySettings;
        private readonly IVCN _vcn;
        private readonly SupplierConfiguration _supplierConfig;



        public JuniperRepo(ICommonMethod commonMethod, IMarkup markup, ISupplierRes supplierRes, IException exception, IOptions<KeySettings> keySettings, IVCN vcn, IOptions<SupplierConfiguration> supplierConfig)
        {
            _commonMethod = commonMethod;
            _markup = markup;
            _supplierRes = supplierRes;
            _exception = exception;
            _keySettings = keySettings.Value;
            _vcn = vcn;
            _supplierConfig = supplierConfig.Value;
        }

        

        public void GetJuniperNewRequestOnly_async_new(DataRow[] commonCode, List<StarRating> listRating, DataTable dtROE, bool IsHotelWiseSearch, bool HitFromAll, HotelSearchParameter SearchPara, List<SupplierDetails> lstJuniperDetails, List<SupplierSearchConfig> lstJuniperSearchConfig, DataTable dtPreferredDataHotel, ref ConcurrentBag<HotelX> AllTask, int Hit, TokenData tokenData, DataSet dtXMLCity, bool SearchByOneID)
        {
            if (lstJuniperDetails != null && lstJuniperDetails.Count > 0)
            {

                List<StarRating> lstRating = new List<StarRating>();
                lstRating = _commonMethod.GetStarRatingStringList_xtg(SearchPara);

                List<HotelCodeByCity> DtHotelDAta = HtmlExtensions.ToList<HotelCodeByCity>(commonCode.CopyToDataTable());
                int ratingcount = lstRating.Count;
                int hotelcount = DtHotelDAta.Count;

                foreach (SupplierDetails JuniperChild in lstJuniperDetails)
                {
                    try
                    {
                        int SupplierID = JuniperChild.SupplierID;
                        string Supplier = JuniperChild.SupplierName;


                        if (commonCode != null)
                        {
                            if (lstJuniperSearchConfig.Count > 0)
                            {
                                List<SupplierSearchConfig> JuniperSearchConfig = lstJuniperSearchConfig
                                    .Where(x => x != null && x.SupplierID == SupplierID)
                                    .ToList();

                                if (JuniperSearchConfig != null && JuniperSearchConfig.Count > 0)
                                {
                                    DataForSearch dataForSearch = _commonMethod.GetXmlDataForSearchTechwise(SearchPara.Nationality, dtXMLCity, Supplier, "juniper");

                                    if (dataForSearch.XmlNationality != "")
                                    {
                                        #region juniper
                                        if ((SearchPara.IsSearchFor.ToLower().IndexOf(Supplier.ToLower()) >= 0 || SearchPara.IsSearchFor.ToLower().IndexOf("multi") >= 0) && HitFromAll)
                                        {
                                            string Ids = string.Empty;
                                            string xmlbase = "";
                                            _commonMethod.GetPrefferedHotelCodes(ref Ids, ref xmlbase, ref JuniperSearchConfig, Supplier, dtPreferredDataHotel, SearchPara.IsSearchFor);

                                            List<HotelCodeByCity> filter = new List<HotelCodeByCity>();
                                            filter = _commonMethod.FilterWithOutCity_Code(ratingcount, hotelcount, DtHotelDAta, lstRating, JuniperChild.HotelSupplier);


                                            string Currency = JuniperChild.CurrencyShortName;
                                            double CurrencyFactorJuniper = 1;
                                            if (Currency.ToLower() != tokenData.SiteCurrency.ToLower())
                                            {
                                                CurrencyFactorJuniper = _commonMethod.GetROEByCurrency(Currency, dtROE);
                                            }

                                            for (int k = 0; k < JuniperSearchConfig.Count; k++)
                                            {
                                                bool DebugMode = (Convert.ToInt32(JuniperSearchConfig[k].DebugMode) == 0 ? false : true);


                                                if (Convert.ToInt32(JuniperSearchConfig[k].CityID) == 0 || Convert.ToInt32(JuniperSearchConfig[k].CityID) == SearchPara.CityID)//All City means 0
                                                {
                                                    if (filter.Count > 0)
                                                    {
                                                        int NoOfRecord = Convert.ToInt32(JuniperSearchConfig[k].Noofrecords);
                                                        double count = Convert.ToDouble(filter.Count);
                                                        int NoOfDiv = Convert.ToInt32(Math.Ceiling(count / Convert.ToDouble(NoOfRecord)));
                                                        List<List<XMLID_Search>> lstxml = _commonMethod.GetXMLIDString(NoOfDiv, NoOfRecord, count, filter);

                                                        for (int a = 0; a < NoOfDiv; a++)
                                                        {

                                                            string IdsForSearch = Convert.ToString(_commonMethod.GetHotelCodeByCOmma(lstxml[a]));

                                                            if (!string.IsNullOrEmpty(IdsForSearch))
                                                            {
                                                                HotelX xhotel = new HotelX();
                                                                xhotel.Supplier = Supplier;
                                                                xhotel.DebugeMode = DebugMode;
                                                                xhotel.SearchTimeOut = Convert.ToInt32(JuniperSearchConfig[k].HotelSearchTimeOut);
                                                             
                                                                xhotel.ProcessTimeout = Convert.ToInt32(JuniperSearchConfig[k].HotelSearchTimeOut2);

                                                                if (_keySettings.SupplierProxy == "0")
                                                                 
                                                                    xhotel.URL = Convert.ToString(JuniperChild.WebSiteURL) + _supplierConfig.Juniper.HotelAvail;
                                                                else
                                                                    
                                                                    xhotel.URL = Convert.ToString(JuniperChild.WebSiteURLforAPI) + _supplierConfig.Juniper.HotelAvail;

                                                                xhotel.UserId = Convert.ToString(JuniperChild.UserID);
                                                                xhotel.UserName = Convert.ToString(JuniperChild.UserName);
                                                                xhotel.PassWord = Convert.ToString(JuniperChild.Password);
                                                                xhotel.CurrencyCode = Currency;
                                                                xhotel.CurrencyFactor = CurrencyFactorJuniper;

                                                                xhotel.XmlNationality = dataForSearch.XmlNationality;
                                                                xhotel.XmlCity = dataForSearch.xmlCity;
                                                                xhotel.XmlCountry = "";

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
                                                                    objrq.Request = (XML_JuniperNew_RequestOnly_async("1", xhotel.URL, xhotel.UserId, xhotel.PassWord, xhotel.SearchTimeOut, SearchPara, IdsForSearch, xhotel.Supplier, CurrencyFactorJuniper, DebugMode, dataForSearch.xmlCity, dataForSearch.XmlNationality, xhotel.CurrencyCode, SearchByOneID));
                                                                    objrq.Response = ""; RQRS.Add(objrq);
                                                                }
                                                                xhotel.RQRS = RQRS;
                                                                xhotel.ReturnRooms = Convert.ToInt32(JuniperChild.ReturnRooms);
                                                                xhotel.TechName = Convert.ToString(JuniperChild.TechName);
                                                                #endregion
                                                                AllTask.Add(xhotel);
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
                        _ = _exception.Error(ex);
                    }
                }
            }
        }

        public string XML_JuniperNew_RequestOnly_async(string star, string Url, string ClientID, string Password, Int32 SearchTimeOut, HotelSearchParameter SearchParam, string HotelId, string Provider, double CurrencyFactorAlphaTours, bool DebugMode, string CityCode, string NationalityCode, string Currency, bool SearchByOneID)
        {
            try
            {
                #region PriceOnly RQ
                string AlphaToursPriceOnly = Get_juniperNew_PriceOnly(SearchParam, ClientID, Password, CityCode, NationalityCode, HotelId, star, Provider, Currency, SearchTimeOut, SearchByOneID);
                return AlphaToursPriceOnly;
                #endregion

            }
            catch (Exception ex)
            {
                _ = _exception.Error(ex);
                return "";
            }
        }
        public string Get_juniperNew_PriceOnly(HotelSearchParameter SearchParam, String UserId, string Password, string CityCode, string NationalityCode, string HotelId, string star, string Provider, string Currency, int SearchTimeOut, bool SearchByOneID)
        {
            #region PriceOnly RQ
            int NoOfRoom = SearchParam.Rooms.Count();
            string CheckIn = SearchParam.CheckInDate;
            CheckIn = CheckIn.Substring(6, 4) + "-" + CheckIn.Substring(3, 2) + "-" + CheckIn.Substring(0, 2);
            string CheckOut = SearchParam.CheckOutDate;
            CheckOut = CheckOut.Substring(6, 4) + "-" + CheckOut.Substring(3, 2) + "-" + CheckOut.Substring(0, 2);
            int NoOfNights = Convert.ToInt32(SearchParam.NoofNights);

            StringBuilder objGuestRequest = new StringBuilder();
            int cnt = 0;
            for (int room = 0; room < SearchParam.Rooms.Count; room++)
            {
                int Adult = Convert.ToInt32(SearchParam.Rooms[room].NoofAdults);
                int Child = Convert.ToInt32(SearchParam.Rooms[room].NoofChild);
                for (int j = 0; j < Adult; j++)
                {
                    cnt = cnt + 1;
                    objGuestRequest.Append("<Pax IdPax=\"" + cnt + "\">");
                    objGuestRequest.Append("</Pax>");
                }
                if (Child > 0)
                {
                    for (int i = 0; i < Child; i++)
                    {
                        int ChildAge1 = SearchParam.Rooms[room].Child1Age;
                        int ChildAge2 = SearchParam.Rooms[room].Child2Age;
                        cnt = cnt + 1;
                        objGuestRequest.Append("<Pax IdPax=\"" + (cnt) + "\">");
                        if (i == 0)
                            objGuestRequest.Append("<Age>" + ChildAge1 + "</Age>");
                        else
                            objGuestRequest.Append("<Age>" + ChildAge2 + "</Age>");
                        objGuestRequest.Append("</Pax>");
                    }
                }
            }
            string Gueststring = objGuestRequest.ToString();
            StringBuilder AlphaTours_RQ = new StringBuilder();
            AlphaTours_RQ.Append("<soapenv:Envelope xmlns:soapenv=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns=\"http://www.juniper.es/webservice/2007/\">");
            AlphaTours_RQ.Append("<soapenv:Header/>");
            AlphaTours_RQ.Append("<soapenv:Body>");
            AlphaTours_RQ.Append("<HotelAvail>");
            // AlphaTours_RQ.Append("<HotelAvailRQ Version=\"1.1\" Language=\"en\">");
            if (SearchByOneID)
            {
                AlphaTours_RQ.Append("<HotelAvailRQ Version=\"1.1\" Context=\"SINGLEAVAIL\" Language=\"en\">");

            }
            else
            {
                AlphaTours_RQ.Append("<HotelAvailRQ Version=\"1.1\" Context=\"FULLAVAIL\" Language=\"en\">");

            }
            AlphaTours_RQ.Append("<Login Email=\"" + UserId + "\" Password=\"" + Password + "\"/>");
            AlphaTours_RQ.Append("<Paxes>");
            AlphaTours_RQ.Append(objGuestRequest.ToString());
            AlphaTours_RQ.Append("</Paxes>");
            AlphaTours_RQ.Append("<HotelRequest>");
            AlphaTours_RQ.Append("<SearchSegmentsHotels>");
            //AlphaTours_RQ.Append("<SearchSegmentHotels Start=\"" + CheckIn + "\" End=\"" + CheckOut + "\" DestinationZone=\"" + CityCode + "\"/>");
            if (HotelId != string.Empty)
                AlphaTours_RQ.Append("<SearchSegmentHotels Start=\"" + CheckIn + "\" End=\"" + CheckOut + "\"/>");
            else
                AlphaTours_RQ.Append("<SearchSegmentHotels Start=\"" + CheckIn + "\" End=\"" + CheckOut + "\" DestinationZone=\"" + CityCode + "\"/>");

            AlphaTours_RQ.Append("<HotelCategories>");

            if (HotelId == string.Empty)
            {
                star = star + "est";
                AlphaTours_RQ.Append("<HotelCategory Type=\"" + star + "\"/>");
            }
            AlphaTours_RQ.Append("</HotelCategories>");



            AlphaTours_RQ.Append("<Nights>" + NoOfNights + "</Nights>");

            if (HotelId != string.Empty)
            {
                string[] hid = HotelId.Split(',');
                AlphaTours_RQ.Append("<HotelCodes>");
                for (int i = 0; i < hid.Length; i++)
                    AlphaTours_RQ.Append("<HotelCode>" + hid[i] + "</HotelCode>");
                AlphaTours_RQ.Append("</HotelCodes>");
            }
            AlphaTours_RQ.Append("<CountryOfResidence>" + NationalityCode + "</CountryOfResidence>");

            if (Provider.ToLower() != "robios")
            {
                //ticket 39833
                string Packag = "Package";
                AlphaTours_RQ.Append("<PackageContracts>" + Packag + "</PackageContracts>");
                //
            }

            AlphaTours_RQ.Append("</SearchSegmentsHotels>");
            AlphaTours_RQ.Append("<RelPaxesDist>");
            int R = 0;
            for (int i = 0; i < SearchParam.Rooms.Count; i++)
            {
                AlphaTours_RQ.Append("<RelPaxDist>");
                AlphaTours_RQ.Append("<RelPaxes>");
                for (int j = 0; j < SearchParam.Rooms[i].NoofAdults; j++)
                {
                    R = R + 1;
                    AlphaTours_RQ.Append("<RelPax IdPax=\"" + R + "\"/>");
                }
                if (SearchParam.Rooms[i].NoofChild > 0)
                {
                    for (int k = 0; k < SearchParam.Rooms[i].NoofChild; k++)
                    {
                        R = R + 1;
                        AlphaTours_RQ.Append("<RelPax IdPax=\"" + R + "\"/>");
                    }
                }
                AlphaTours_RQ.Append("</RelPaxes>");
                AlphaTours_RQ.Append("</RelPaxDist>");
            }
            AlphaTours_RQ.Append("</RelPaxesDist>");
            AlphaTours_RQ.Append("</HotelRequest>");
            AlphaTours_RQ.Append("<AdvancedOptions>");
            //if (Provider == "alphatours")
            //    AlphaTours_RQ.Append("<UseCurrency>" + XMLHotelBookingCommon.AlphaCurrency + "</UseCurrency>");
            //else
            AlphaTours_RQ.Append("<UseCurrency>" + Currency + "</UseCurrency>");
            AlphaTours_RQ.Append("<ShowHotelInfo>false</ShowHotelInfo >");
            AlphaTours_RQ.Append("<ShowOnlyAvailable>true</ShowOnlyAvailable>");
            AlphaTours_RQ.Append("<ShowCompleteInfo>false</ShowCompleteInfo>");
            AlphaTours_RQ.Append("<TimeOut>" + SearchTimeOut + "</TimeOut>"); //#01-10-2024
            AlphaTours_RQ.Append("<ShowCancellationPolicies>true</ShowCancellationPolicies>"); //040425
            AlphaTours_RQ.Append("</AdvancedOptions>");
            AlphaTours_RQ.Append("</HotelAvailRQ>");
            AlphaTours_RQ.Append("</HotelAvail>");
            AlphaTours_RQ.Append("</soapenv:Body>");
            AlphaTours_RQ.Append("</soapenv:Envelope>");

            string AlphaTours_REQ = AlphaTours_RQ.ToString();

            return AlphaTours_REQ;
            #endregion
        }
        public string Get_JuniperNEW_Currency(string Provider)
        {
            string Currency = "";
            try
            {
                switch (Provider.ToLower())
                {
                    case "lotsofhotels":
                        Currency = JuniperInfo.LOHCurrency;
                        break;
                    case "escalabeds":
                        Currency = JuniperInfo.EscalabedsCurrency;
                        break;
                    case "bookohotel":
                        Currency = JuniperInfo.BookoHotelCurrency;
                        break;
                    case "travcoegypt":
                        Currency = JuniperInfo.TravcoEgyptCurrency;
                        break;
                    case "fastpayhotels":
                        Currency = JuniperInfo.FastPayHotelsCurrency;
                        break;
                    case "w2m":
                        Currency = (JuniperInfo.W2MCurrency);
                        break;
                    case "gte":
                        Currency = (JuniperInfo.GTECurrency);
                        break;
                    case "alphatours":
                        Currency = (JuniperInfo.AlphaCurrency);
                        break;
                    case "egyptexpress":
                        Currency = (JuniperInfo.EETCurrency);
                        break;
                    case "eref":
                        Currency = (JuniperInfo.EREFCurrency);
                        break;
                    case "peakpoint":
                        Currency = (JuniperInfo.PeakPointCurrency);
                        break;
                    case "itrip":
                        Currency = (JuniperInfo.iTripCurrency);
                        break;
                    case "primetravel":
                        Currency = (JuniperInfo.primetravelCurrency);
                        break;
                    case "ixpira":
                        Currency = JuniperInfo.ixpiraCurrency;
                        break;
                    case "destinationitalia":
                        Currency = JuniperInfo.italiaCurrency;
                        break;
                    case "elevatedmc":
                        Currency = JuniperInfo.elevatedmc;
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
                return "";
            }
            return Currency;
        }

        public async Task<List<Options>> Juniper_HotelSearchWithRooms(HotelX Item, CommonSearch commonSearch, string ResponseData, HotelSearchParameter SearchParam, string Provider, string CurrencyCode, double CurrencyFactorAlphaTours, string xmlcity, string xmlcountry, string xmlnationality, string ProccessCutOffTime, int processTimeout, string ResProccessTimerLogActive, string ResProccessTimeValueforSaveLog, string StoreSupplierResErrorLog, bool SearchByOneID)
        {
            System.Diagnostics.Stopwatch timer_room_Juniper = new System.Diagnostics.Stopwatch();
            timer_room_Juniper.Start();

            DataTable DtHotel = _commonMethod.GenerateDataTableNewV2();
            DataTable DtHotelRoom = _commonMethod.GenerateRoomtypeDataTableNewV2();
            DataTable DtHotelPolicy = _commonMethod.GenerateCancellationDataTableNewV2();
            List<Options> Options = new List<Options>();
            try
            {
                string CityCode = xmlcity;
                if (ResponseData != null)
                {
                    #region PriceOnly RS
                    if (ResponseData == string.Empty || ResponseData.Contains("Error"))
                    {
                        Item.SupplierResponseError = ResponseData;
                        Item.ResponseStatus = 404;
                        Item.ResponseType = "NoCount";

                        if (StoreSupplierResErrorLog == "1")
                            await _commonMethod.XMLRequestResponseTrack_Error(Item.RQRS[0].Request, ResponseData, Provider, timer_room_Juniper.Elapsed.ToString(), "", SearchParam.SearchKey);

                        return Options;
                    }
                    ResponseData = ResponseData.Replace("<Images>", "");
                    ResponseData = ResponseData.Replace("</Images>", "");
                    DataSet AlphaTours_Response = _commonMethod.GetDataSetFromXMLString(ResponseData);

                    if (AlphaTours_Response.Tables.Contains("Errors") || AlphaTours_Response.Tables.Contains("Error"))
                    {
                        if (StoreSupplierResErrorLog == "1")
                            await _commonMethod.XMLRequestResponseTrack_Error(Item.RQRS[0].Request, ResponseData, Provider, timer_room_Juniper.Elapsed.ToString(), "", SearchParam.SearchKey);

                        Item.SupplierResponseError = ResponseData;
                        Item.ResponseStatus = 404;
                        Item.ResponseType = "NoCount";
                        return Options;
                    }
                    if (AlphaTours_Response == null || AlphaTours_Response.Tables["HotelResult"] == null)
                    {
                        if (StoreSupplierResErrorLog == "1")
                            await _commonMethod.XMLRequestResponseTrack_Error(Item.RQRS[0].Request, ResponseData, Provider, timer_room_Juniper.Elapsed.ToString(), "", SearchParam.SearchKey);

                        Item.SupplierResponseError = ResponseData;
                        Item.ResponseStatus = 404;
                        Item.ResponseType = "NoCount";
                        return Options;
                    }
                    #endregion
                    #region "Giata Filter
                    //GiataMasterClass giataMasterClass = new GiataMasterClass();
                    //switch (Provider.ToLower())
                    //{

                    //    case "lotsofhotels":
                    //        giataMasterClass = commonSearch.supplierGiataMaster.lotsOfHotelsGiataInfo;
                    //        break;
                    //    case "escalabeds":
                    //        giataMasterClass = commonSearch.supplierGiataMaster.escalabedsGiataInfo;
                    //        break;
                    //    case "bookohotel":
                    //        giataMasterClass = commonSearch.supplierGiataMaster.bookoHotelGiataInfo;
                    //        break;
                    //    case "travcoegypt":
                    //        giataMasterClass = commonSearch.supplierGiataMaster.travcoEgyptGiataInfo;
                    //        break;
                    //    case "fastpayhotels":
                    //        giataMasterClass = commonSearch.supplierGiataMaster.fastPayHotelsGiataInfo;
                    //        break;
                    //    case "w2m":
                    //        giataMasterClass = commonSearch.supplierGiataMaster.w2mGiataInfo;
                    //        break;
                    //    case "gte":
                    //        giataMasterClass = commonSearch.supplierGiataMaster.gteGiataInfo;
                    //        break;
                    //    case "alphatours":
                    //        giataMasterClass = commonSearch.supplierGiataMaster.alphaToursGiataInfo;
                    //        break;
                    //    case "egyptexpress":
                    //        giataMasterClass = commonSearch.supplierGiataMaster.egyptExpressGiataInfo;
                    //        break;

                    //    default:
                    //        return Options;
                    //        break;
                    //}
                    #endregion
                    for (int h = 0; h <= AlphaTours_Response.Tables["HotelResult"].Rows.Count - 1; h++)
                    {
                        Item.ResponseType = "Success";
                        //Check Time If High then return NoCount
                        if (ProccessCutOffTime == "1" && timer_room_Juniper.ElapsedMilliseconds > processTimeout)
                        {
                            timer_room_Juniper.Stop();
                            break;
                        }
                        //--end
                        // int HotelResult_Id = Convert.ToInt32(AlphaTours_Response.Tables["HotelResult"].Rows[h]["HotelResult_Id"]);
                        // string ItemCode = Convert.ToString(AlphaTours_Response.Tables["HotelResult"].Rows[h]["Code"]);
                        #region "When not Need Room"  
                        // add hotel code from price only and room code blank here
                        if (SearchParam.Filters.IsShowRooms == 0)
                        {
                            // int HotelOptions_Id = Convert.ToInt32(AlphaTours_Response.Tables["HotelOptions"].Rows[h]["HotelOptions_Id"]);

                            DataRow drHotelOption = null;// (AlphaTours_Response.Tables["HotelOption"].Select("HotelOptions_Id='" + HotelOptions_Id + "'"));
                            for (int rm = 0; rm < (AlphaTours_Response.Tables["HotelOption"].Rows.Count); rm++)
                            {
                                if (Convert.ToString(AlphaTours_Response.Tables["HotelOption"].Rows[rm]["HotelOptions_Id"]) == Convert.ToString(AlphaTours_Response.Tables["HotelOptions"].Rows[h]["HotelOptions_Id"]))
                                {
                                    drHotelOption = AlphaTours_Response.Tables["HotelOption"].Rows[rm];
                                    break;
                                }

                            }
                            if (drHotelOption != null)
                            {
                                if (Convert.ToString(drHotelOption["Status"]).ToLower() != "ok")
                                    continue;
                                double NetPrice = 1;
                                // int HotelOption_Id = Convert.ToInt32(drHotelOption["HotelOption_Id"]);
                                DataRow drTotalFixAmounts = null;
                                DataRow drPrice = null; //250325

                                if (commonSearch.tokenData.AgentId != 1899)
                                {
                                    for (int t = 0; t < (AlphaTours_Response.Tables["TotalFixAmounts"].Rows.Count); t++)
                                    {
                                        if (Convert.ToString(AlphaTours_Response.Tables["TotalFixAmounts"].Rows[t]["TotalFixAmounts_Id"]) == drHotelOption["HotelOption_Id"].ToString())
                                        {
                                            drTotalFixAmounts = AlphaTours_Response.Tables["TotalFixAmounts"].Rows[t];
                                            break;
                                        }

                                    }

                                    if (Provider.ToLower() == "robios")
                                    {
                                        for (int t = 0; t < (AlphaTours_Response.Tables["Price"].Rows.Count); t++)
                                        {
                                            if (Convert.ToString(AlphaTours_Response.Tables["Price"].Rows[t]["Price_Id"]) == drHotelOption["HotelOption_Id"].ToString())
                                            {
                                                drPrice = AlphaTours_Response.Tables["Price"].Rows[t];
                                                break;
                                            }

                                        }
                                    }
                                }


                                if (drTotalFixAmounts != null || commonSearch.tokenData.AgentId == 1899)
                                {
                                    //int TotalFixAmounts_Id = Convert.ToInt32(drTotalFixAmounts["TotalFixAmounts_Id"]);
                                    if (commonSearch.tokenData.AgentId != 1899)
                                    {
                                        if (Provider.ToLower() == "robios")
                                        {
                                            CurrencyCode = Convert.ToString(drPrice["Currency"]);
                                            CurrencyFactorAlphaTours = 1;
                                            if (CurrencyCode.ToLower() != commonSearch.tokenData.SiteCurrency.ToLower())
                                            {
                                                CurrencyFactorAlphaTours = _commonMethod.GetROEByCurrency(CurrencyCode, commonSearch.tokenData.dtROE);
                                            }
                                        }

                                        NetPrice = Convert.ToDouble(drTotalFixAmounts["Gross"]) * CurrencyFactorAlphaTours;

                                    }
                                    // NetPrice = NetPrice ;

                                    // string Status = Convert.ToString(drHotelOption["Status"]);

                                    #region Hotels


                                    DataRow drXmlHotel = DtHotel.NewRow();
                                    drXmlHotel["XMLCountry"] = "";
                                    drXmlHotel["XMLCity"] = Convert.ToString(CityCode);
                                    drXmlHotel["XmlHotelCity"] = Convert.ToString(CityCode);
                                    drXmlHotel["XMLHotelCode"] = AlphaTours_Response.Tables["HotelResult"].Rows[h]["Code"];
                                    drXmlHotel["HotelName"] = "";
                                    drXmlHotel["Address"] = "";
                                    drXmlHotel["Rating"] = "0";
                                    drXmlHotel["EssentialInformation"] = "";
                                    drXmlHotel["Description"] = "";
                                    drXmlHotel["SupplierName"] = Provider;
                                    drXmlHotel["OfficeID"] = 0;
                                    drXmlHotel["ContractID"] = 0;
                                    drXmlHotel["UniqueNo"] = AlphaTours_Response.Tables["HotelResult"].Rows[h]["Code"];
                                    drXmlHotel["IsShowSupplier"] = false;
                                    drXmlHotel["BookingStatus"] = "Available";
                                    drXmlHotel["FinalSellingPrice"] = NetPrice;
                                    drXmlHotel["SellingPrice"] = NetPrice;
                                    drXmlHotel["SupplierCurrency"] = CurrencyCode;
                                    if (NetPrice > 0)
                                    {
                                        #region "GetRooms for this hotel"
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
                                            decimal LowestPrice = Convert.ToDecimal(MinPrice).CalculatePrice(commonSearch.tokenData, commonSearch.advancedOptions, CurrencyCode);
                                            Options ops = (await _supplierRes.GetCommonRoomForSuppliers(GiataId, commonSearch.tokenData, commonSearch.advancedOptions, Convert.ToDouble(LowestPrice), drXmlHotel, DtHotelRoom, DtHotelPolicy, dt_RateBreackUp, SearchParam));
                                            if (ops != null)
                                                Options.Add(ops);
                                        }
                                        #endregion

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
                            #region Hotels


                            DataRow drXmlHotel = DtHotel.NewRow();
                            drXmlHotel["XMLCountry"] = "";
                            drXmlHotel["XMLCity"] = Convert.ToString(CityCode);
                            drXmlHotel["XmlHotelCity"] = Convert.ToString(CityCode);
                            drXmlHotel["XMLHotelCode"] = AlphaTours_Response.Tables["HotelResult"].Rows[h]["Code"];
                            drXmlHotel["HotelName"] = "";
                            drXmlHotel["Address"] = "";
                            drXmlHotel["Rating"] = "0";
                            drXmlHotel["EssentialInformation"] = "";
                            drXmlHotel["Description"] = "";
                            drXmlHotel["SupplierName"] = Provider;
                            drXmlHotel["OfficeID"] = 0;
                            drXmlHotel["ContractID"] = 0;
                            drXmlHotel["UniqueNo"] = AlphaTours_Response.Tables["HotelResult"].Rows[h]["Code"];
                            drXmlHotel["IsShowSupplier"] = false;
                            drXmlHotel["BookingStatus"] = "Available";
                            drXmlHotel["FinalSellingPrice"] = 0;
                            drXmlHotel["SellingPrice"] = 0;
                            drXmlHotel["SupplierCurrency"] = CurrencyCode;


                            #region "GetRooms for this hotel"
                            bool willAddOrNot = true;
                            Int64 RaynaID = 0;
                            Int64 GiataId = 0;
                            int Rating = 0;
                            Int64 OwnsystemHotelId = 0;
                            double MinPrice = 0;
                            _markup.GetHotelDataCommon(ref MinPrice, ref willAddOrNot, ref OwnsystemHotelId, ref RaynaID, ref GiataId, ref Rating, ref drXmlHotel, commonSearch.AgentId, commonSearch.TariffID, commonSearch.dsAgentDetails, commonSearch.supplierGiataMaster, SearchParam, commonSearch.dtCountryCityMarkup, commonSearch.B2CMarkup, commonSearch.IsParentId, commonSearch.SubAgentMarkup, commonSearch.SearchByHotel, commonSearch.HotelIDS);
                            if (willAddOrNot)
                            {
                                #region "Room Logic"
                                DataTable Dt_JuniperNew_HotelRoom = _commonMethod.GenerateRoomtypeDataTableNewV2();
                                DataTable dt_JuniperNew_Cancellation = _commonMethod.GenerateCancellationDataTableNewV2();
                                DataTable Dt_JuniperNew_RateBreackUp = _commonMethod.GenerateoomRateBreackUpDataTableNewV2();
                                DataRow[] drOnlythatHotel = AlphaTours_Response.Tables["HotelOption"].Select("HotelOptions_Id='" + AlphaTours_Response.Tables["HotelResult"].Rows[h]["HotelResult_Id"] + "'");
                                if (SearchParam.Filters.IsShowRooms == 1)
                                {
                                    double AgentPercentage = 0.0;
                                    Boolean blnIsShowSupplier = false;
                                    double AgentMarkupAmount = 0.0;
                                    Boolean isPerce = true;

                                    double AgentCompanyPercentage = Convert.ToDouble(SearchParam.AgentCompanyPercentage);
                                    bool IsB2C = Convert.ToBoolean(Convert.ToByte(SearchParam.IsB2BOrB2C));

                                    //_markup.getAgetMarkupSetting(Provider, GiataId.ToString(), commonSearch.dtSuplierMarkup, commonSearch.dtMarkupDetails, ref AgentPercentage, ref AgentMarkupAmount, ref blnIsShowSupplier, ref isPerce, commonSearch.AgentId, Rating, SearchParam.CityID, SearchParam.CountryID.ToString(), SearchParam.CheckInDate);

                                    _markup.getAgetMarkupSettingNew_new(Provider, Convert.ToString(RaynaID), commonSearch.dtSuplierMarkup, commonSearch.dsMarkupDetails, ref AgentPercentage, ref AgentMarkupAmount, ref blnIsShowSupplier, ref isPerce, commonSearch.AgentId, Rating, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), SearchParam.CheckInDate);



                                    for (int i = 0; i < drOnlythatHotel.Length; i++)
                                    {
                                        if (commonSearch.AgentId == 4440 || commonSearch.AgentId == 4901)
                                        {
                                            if (i > CacheMaster.RoomLimit_20 && SearchByOneID == false)
                                                break;
                                        }
                                        else
                                        {
                                            if (i > CacheMaster.RoomLimit_10 && SearchByOneID == false)
                                                break;
                                        }

                                        #region DataRaw
                                        string HotelOption_Id = Convert.ToString(drOnlythatHotel[i]["HotelOption_Id"]); ;
                                        string RoomCode = Convert.ToString(drOnlythatHotel[i]["RatePlanCode"]);
                                        string NonRefundable = Convert.ToString(drOnlythatHotel[i]["NonRefundable"]); //260526
                                        string uniqid = AlphaTours_Response.Tables["HotelResult"].Rows[h]["Code"] + "_" + i + "_" + h;
                                        RoomCode = RoomCode + "$$$" + uniqid + "$$$" + xmlnationality + "$$$" + AlphaTours_Response.Tables["HotelResult"].Rows[h]["Code"];

                                        if (Convert.ToString(drOnlythatHotel[i]["Status"]).ToLower() != "ok")
                                            continue;

                                        DataRow[] drBoard = (AlphaTours_Response.Tables["Board"].Select("HotelOption_Id='" + drOnlythatHotel[i]["HotelOption_Id"] + "'"));
                                        string Board_Text = Convert.ToString(drBoard[0]["Board_Text"]);

                                        string Board_Type = Convert.ToString(drBoard[0]["Type"]); //added on 150525
                                        Board_Text = JuniperMeal(Board_Type);  //added on 150525

                                        DataRow[] drPrices = (AlphaTours_Response.Tables["Prices"].Select("Prices_Id='" + drOnlythatHotel[i]["HotelOption_Id"] + "'"));
                                       

                                        DataRow[] drPrice = (AlphaTours_Response.Tables["Price"].Select("Price_Id='" + drPrices[0]["Prices_Id"] + "'"));
                                      
                                        DataRow[] drTotalFixAmounts1 = (AlphaTours_Response.Tables["TotalFixAmounts"].Select("TotalFixAmounts_Id='" + drPrice[0]["Price_Id"] + "'"));
                                        
                                        DataRow[] drHotelRooms = (AlphaTours_Response.Tables["HotelRooms"].Select("HotelRooms_Id='" + drOnlythatHotel[i]["HotelOption_Id"] + "'"));
                                        DataRow[] drHotelRoom = (AlphaTours_Response.Tables["HotelRoom"].Select("HotelRooms_Id='" + drHotelRooms[0]["HotelRooms_Id"] + "'"));
                                      
                                        #endregion

                                            int Price_Id = Convert.ToInt32(drPrice[0]["Price_Id"]);
                                            CurrencyCode = Convert.ToString(drPrice[0]["Currency"]);
                                            CurrencyFactorAlphaTours = 1;
                                            if (CurrencyCode.ToLower() != commonSearch.tokenData.SiteCurrency.ToLower())
                                            {
                                                CurrencyFactorAlphaTours = _commonMethod.GetROEByCurrency(CurrencyCode, commonSearch.tokenData.dtROE);
                                            }
                                        
                                        #region Offers
                                        //if (AlphaTours_Response.Tables.Contains("AdditionalElements"))
                                        //{

                                        //    int AdditionalElements_Id = 0;
                                        //    DataRow[] drAdditionalElements = (AlphaTours_Response.Tables["AdditionalElements"].Select("HotelOption_Id='" + HotelOption_Id + "'"));
                                        //    if (drAdditionalElements.Length > 0)
                                        //    {
                                        //        AdditionalElements_Id = Convert.ToInt32(drAdditionalElements[0]["AdditionalElements_Id"]);
                                        //        if (AlphaTours_Response.Tables["HotelOffers"] != null)
                                        //        {
                                        //            DataRow[] drHotelOffers = (AlphaTours_Response.Tables["HotelOffers"].Select("AdditionalElements_Id='" + AdditionalElements_Id + "'"));
                                        //            if (drHotelOffers.Length != 0)
                                        //            {
                                        //                int HotelOffers_Id = Convert.ToInt32(drHotelOffers[0]["HotelOffers_Id"]);
                                        //                DataRow[] drHotelOffer = (AlphaTours_Response.Tables["HotelOffer"].Select("HotelOffers_Id='" + HotelOffers_Id + "'"));
                                        //                if (AlphaTours_Response.Tables["HotelOffer"].Columns.Contains("Name"))
                                        //                    Offer_Name = Convert.ToString(drHotelOffer[0]["Name"]);
                                        //                else
                                        //                    Offer_Name = "Offer";
                                        //                if (AlphaTours_Response.Tables["HotelOffer"].Columns.Contains("Description"))
                                        //                    Offer_Desc = Convert.ToString(drHotelOffer[0]["Description"]);
                                        //                else
                                        //                {
                                        //                    Offer_Name = "";
                                        //                    Offer_Desc = "";
                                        //                }
                                        //            }
                                        //        }
                                        //    }

                                        //}
                                        #endregion

                                        #region "Logic For Room Name "
                                        List<Room_W2M> roomlist = new List<Room_W2M>();
                                       
                                        for (int dr = 0; dr < drHotelRoom.Length; dr++) //#01-10-2024
                                        {
                                            int units = Convert.ToInt32(drHotelRoom[dr]["Units"]);
                                            if (units > 1)
                                            {
                                                for (int d = 0; d < units; d++)
                                                {
                                                    Room_W2M obj = new Room_W2M();
                                                    obj.RoomCount = 1;
                                                    obj.RoomName = Convert.ToString(drHotelRoom[dr]["Name"]);
                                                    obj.status = true;
                                                    roomlist.Add(obj);
                                                }
                                            }
                                            else
                                            {

                                                Room_W2M obj = new Room_W2M();
                                                obj.RoomCount = 1;
                                                obj.RoomName = Convert.ToString(drHotelRoom[dr]["Name"]);
                                                obj.status = true;
                                                roomlist.Add(obj);

                                            }

                                        }
                                        string RoomNameMain = ""; //#@change
                                        for (int p = 0; p < roomlist.Count; p++)
                                        {
                                            string RoomNme = roomlist[p].RoomName;
                                            RoomNameMain = RoomNameMain + "(" + (p + 1) + ")" + RoomNme + " ";
                                        }

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

                                        if (Provider.ToLower() == "robios")
                                            RoomCode = RoomCode + "$$$" + SearchParam.Nationality + "$$$" + CurrencyCode;
                                        else
                                            RoomCode = RoomCode + "$$$" + SearchParam.Nationality;

                                        #region Rooms

                                        DataRow drXmlHotelRoom = Dt_JuniperNew_HotelRoom.NewRow();
                                        drXmlHotelRoom["OwnSystemHotelId"] = OwnsystemHotelId;
                                        drXmlHotelRoom["RoomTypeName"] = RoomNameMain;
                                        drXmlHotelRoom["RoomTypeCode"] = "";
                                        drXmlHotelRoom["MealId"] = Board_Text; 
                                        drXmlHotelRoom["MealName"] = Board_Text; 
                                        drXmlHotelRoom["ContractID"] = 0;
                                        drXmlHotelRoom["BoardType"] = Board_Text;
                                        drXmlHotelRoom["RoomTypeId"] = RoomCode;
                                        drXmlHotelRoom["AdultsOccupancy"] = TotalAdult;
                                        drXmlHotelRoom["ChildOccupancy"] = TotalChild;
                                        drXmlHotelRoom["MaxExtraBed"] = "0";
                                        drXmlHotelRoom["RoomNo"] = 1;
                                        drXmlHotelRoom["RoomSequenceNo"] = uniqid;
                                        drXmlHotelRoom["Type"] = "";
                                        drXmlHotelRoom["BookingStatus"] = "Available";
                                        drXmlHotelRoom["UniqueNo"] = AlphaTours_Response.Tables["HotelResult"].Rows[h]["Code"];
                                        drXmlHotelRoom["OfferDescription"] = "";
                                        drXmlHotelRoom["OfferCode"] = "";
                                        drXmlHotelRoom["EssentialInformation"] = " ";

                                        double ItemPrice = Convert.ToDouble(drTotalFixAmounts1[0]["Gross"]) * CurrencyFactorAlphaTours;

                                        _markup.ProcessHotelRoom_WithMarkup(CurrencyFactorAlphaTours, CurrencyCode, true, true, SearchParam.NoofNights * SearchParam.Rooms.Count, SearchParam.CityID, SearchParam.CountryID.ToString(), SearchParam.CheckInDate, SearchParam.CheckOutDate, ref ItemPrice, ref drXmlHotelRoom, isPerce, AgentPercentage, AgentMarkupAmount, commonSearch.SubAgentMarkup, commonSearch.B2CMarkup, commonSearch.IsParentId);

                                        drXmlHotelRoom["SupplierName"] = Provider;
                                        Dt_JuniperNew_HotelRoom.Rows.Add(drXmlHotelRoom);
                                        #endregion


                                        #region policy

                                        int CutOffDays = XMLHotelBookingCommon.CutOffDays;
                                        CutOffDays = _commonMethod.GetCutOffDays(commonSearch.dtSuplierMarkup, "" + Provider + "");

                                        string FirstDayCostCancellation_Text = Convert.ToString(DateTime.Now.Date);
                                        if (AlphaTours_Response.Tables.Contains("CancellationPolicy"))
                                        {
                                            DataRow[] drCancellationPolicy = (AlphaTours_Response.Tables["CancellationPolicy"].Select("HotelOption_Id='" + HotelOption_Id + "'"));
                                            if (drCancellationPolicy.Length > 0)
                                            {
                                                int CancellationPolicy_Id = Convert.ToInt32(drCancellationPolicy[0]["CancellationPolicy_Id"]);

                                                if (AlphaTours_Response.Tables.Contains("FirstDayCostCancellation"))
                                                {
                                                    DataRow[] drFirstDayCostCancellation = (AlphaTours_Response.Tables["FirstDayCostCancellation"].Select("CancellationPolicy_Id='" + CancellationPolicy_Id + "'"));
                                                    FirstDayCostCancellation_Text = Convert.ToString(drFirstDayCostCancellation[0]["FirstDayCostCancellation_Text"]);
                                                }
                                                else
                                                {
                                                    FirstDayCostCancellation_Text = Convert.ToString(DateTime.Now.Date);
                                                }
                                            }
                                            else
                                            {
                                                FirstDayCostCancellation_Text = Convert.ToString(DateTime.Now.Date);
                                            }

                                        }
                                        else
                                        {
                                            FirstDayCostCancellation_Text = Convert.ToString(DateTime.Now.Date);
                                        }
                                        double CancelCharge = ItemPrice;

                                        string RoomTypeId_Own = RoomCode;


                                        DateTime CancelDate = DateTime.Now.Date;
                                        CancelDate = Convert.ToDateTime(FirstDayCostCancellation_Text);
                                        DateTime NowDate = DateTime.Now.Date;
                                        if (CancelDate <= DateTime.Now.Date)
                                            CancelDate = DateTime.Now.Date;
                                        else
                                            CancelDate = CancelDate.AddDays(CutOffDays).Date;

                                        #region Vat
                                        double VatOnPolicy = 0;
                                        double MainAgentCharge = CancelCharge;
                                        double CompanyChargeAmount = CancelCharge;
                                        CancelCharge = _markup.ProcessHotelCancelationPrice_WithMarkup(ref MainAgentCharge, SearchParam.Rooms.Count * SearchParam.NoofNights, SearchParam.CityID, SearchParam.CountryID.ToString(), CancelCharge, isPerce, commonSearch.B2CMarkup, AgentPercentage, AgentMarkupAmount, commonSearch.IsParentId, commonSearch.SubAgentMarkup, ref VatOnPolicy);
                                        #endregion
                                        if (CancelDate.Date < DateTime.Now.Date)
                                        {
                                            CancelDate = DateTime.Now.Date;
                                        }

                                        if (DateTime.Now.Date < CancelDate.Date)
                                        {
                                            double zero = 0;
                                           dt_JuniperNew_Cancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref zero, dt_JuniperNew_Cancellation.NewRow(), CurrencyFactorAlphaTours, CurrencyCode, Provider, Convert.ToString(OwnsystemHotelId), Convert.ToString(1),
               Convert.ToString(RoomTypeId_Own), RoomNameMain, Board_Text, DateTime.Now.Date, CancelDate.AddDays(-1), 0, 0, "", VatOnPolicy, "", "NO", NonRefundable, "", "", ""));
                                            dt_JuniperNew_Cancellation.AcceptChanges();


                                            dt_JuniperNew_Cancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref CompanyChargeAmount, dt_JuniperNew_Cancellation.NewRow(), CurrencyFactorAlphaTours, CurrencyCode, Provider, Convert.ToString(OwnsystemHotelId), Convert.ToString(1),
                Convert.ToString(RoomTypeId_Own), RoomNameMain, Board_Text, Convert.ToDateTime(CancelDate), DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null), MainAgentCharge, ItemPrice, "", VatOnPolicy, "", "NO", NonRefundable, "", "", ""));
                                            dt_JuniperNew_Cancellation.AcceptChanges();
                                        }
                                        else
                                        {
                                            if (CancelDate < DateTime.Now.Date)
                                                CancelDate = DateTime.Now.Date;

                                            dt_JuniperNew_Cancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref CompanyChargeAmount, dt_JuniperNew_Cancellation.NewRow(), CurrencyFactorAlphaTours, CurrencyCode, Provider, Convert.ToString(OwnsystemHotelId), Convert.ToString(1),
                                                                   Convert.ToString(RoomTypeId_Own), RoomNameMain, Board_Text, Convert.ToDateTime(CancelDate),
                                                                   DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null), MainAgentCharge, ItemPrice, "", 0, uniqid, "Yes", NonRefundable, ""));
                                            dt_JuniperNew_Cancellation.AcceptChanges();
                                        }

                                        #endregion

                                    }
                                }
                                #endregion
                                Options ops = (await _supplierRes.GetCommonRoomForSuppliers(GiataId, commonSearch.tokenData, commonSearch.advancedOptions, MinPrice, drXmlHotel, Dt_JuniperNew_HotelRoom, dt_JuniperNew_Cancellation, Dt_JuniperNew_RateBreackUp, SearchParam));
                                if (ops != null)
                                    Options.Add(ops);
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
                    timer_room_Juniper.Stop();
                    Item.CodeProcessingTimeSupplier = timer_room_Juniper.ElapsedMilliseconds;
                    if (SearchParam != null && ResProccessTimerLogActive == "1" && timer_room_Juniper.ElapsedMilliseconds > Convert.ToInt32(ResProccessTimeValueforSaveLog))
                    {
                       
                        SearchParam.SupplierTime += ",|Finish:" + Provider + " Process Response  " + "|Time :" + timer_room_Juniper.Elapsed.ToString();
                        _ = Task.Run(() => _commonMethod.FireAway_SQL_ResPorcessTimeLog("", timer_room_Juniper.Elapsed.ToString(), Provider, SearchParam.SearchKey, 1, SearchParam.AgentID));
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

        public string JuniperMeal(string mealCode)
        {
            string mealName = "";
            switch (mealCode)
            {
                case "SA":
                    mealName = "Accommodation Only";
                    break;
                case "TI":
                    mealName = "All Inclusive";
                    break;
                case "TL":
                    mealName = "All Inclusive Light";
                    break;
                case "AD":
                    mealName = "Bed & Breakfast";
                    break;
                case "AD2":
                    mealName = "Breakfast for 2";
                    break;
                case "BRU":
                    mealName = "Brunch";
                    break;
                case "PC":
                    mealName = "Full board";
                    break;
                case "MP":
                    mealName = "Half Board";
                    break;
                case "SC":
                    mealName = "Self Catering";
                    break;
                case "STI":
                    mealName = "Soft All inclusive";
                    break;
                case "UAI":
                    mealName = "Ultra All Inclusive";
                    break;
                case "AI":
                    mealName = "All inclusive";
                    break;
                case "AIS":
                    mealName = "All Inclusive Soft";
                    break;
                case "BB":
                    mealName = "Bed & Breakfast";
                    break;
                case "FB":
                    mealName = "Full board";
                    break;
                case "FB+":
                    mealName = "Full Board +";
                    break;
                case "HB":
                    mealName = "Half board";
                    break;
                case "HBL":
                    mealName = "Half board";
                    break;
                case "SAI":
                    mealName = "All inclusive Soft";
                    break;
                case "TIL":
                    mealName = "All Inclusive Light";
                    break;
                case "MPA":
                    mealName = "Half Board Lunch";
                    break;
                case "LAI":
                    mealName = "Luxury All Inclusive";
                    break;
                default:
                    mealName = "Room Only";
                    break;
            }
            return mealName;
        }

        public class Room_W2M
        {
            public int RoomCount { get; set; }
            public string RoomName { get; set; }
            public bool status { get; set; }
        }

        public async Task<string> Get_JuniperNew_EssentialInfoOnly(HotelSearchParameter Searchpara, string hotelcode, string RoomTypeId_Own, DataRow[] drSuplierMarkupTA, DataTable dtSearchConfig, string Provider, DataTable dtSuplierMarkup, string SupplierCurrency, string SearchKey)
        {
            string EssentialInfo = "";

            //string Currency = "";
            //string Currency = Common.Get_JuniperNEW_Currency(Provider); //Comment on 28-05-25
            string Currency = SupplierCurrency;  //Added on 28-05-25
            try
            {
                #region Cancellation Policy RQ

                double AgentCompanyPercentage = Convert.ToDouble(Searchpara.AgentCompanyPercentage);
                bool IsB2C = Convert.ToBoolean(Convert.ToByte(Searchpara.IsB2BOrB2C));
                double B2CMarkup = (IsB2C == false ? AgentCompanyPercentage : XMLHotelBookingCommon.B2CMarkup);

                int CutOffDays = XMLHotelBookingCommon.CutOffDays;
                //CutOffDays = Common.GetCutOffDays(dtSuplierMarkup, provider);
                DataRow[] drSuplierMarkup = (dtSuplierMarkup.Select("SupplierName='" + Provider + "'"));
                //string UserId = Convert.ToString(drSuplierMarkup[0]["UserId"]);
                //string Password = Convert.ToString(drSuplierMarkup[0]["Password"]);
                string CheckIn = Searchpara.CheckInDate;
                CheckIn = CheckIn.Substring(6, 4) + "-" + CheckIn.Substring(3, 2) + "-" + CheckIn.Substring(0, 2);
                string CheckOut = Searchpara.CheckOutDate;
                CheckOut = CheckOut.Substring(6, 4) + "-" + CheckOut.Substring(3, 2) + "-" + CheckOut.Substring(0, 2);
                string Url = Convert.ToString(drSuplierMarkup[0]["WebSiteURL"]);
                int SID = Convert.ToInt32(drSuplierMarkup[0]["SupplierID"]);
                DataRow[] drSearchConfig = (dtSearchConfig.Select("SupplierID='" + SID + "'"));
                int SearchTimeOut = Convert.ToInt32(drSearchConfig[0]["BookingConfirmationTimeout"]);
                string UserId = Convert.ToString(drSuplierMarkup[0]["UserId"]);
                string Password = Convert.ToString(drSuplierMarkup[0]["Password"]);
                string Nationality = "";
                string roomtypeid = Get_LOH_RoomTypeID(RoomTypeId_Own, ref Nationality);

                StringBuilder CancelPolicyRQ = new StringBuilder();
                CancelPolicyRQ.Append("<soapenv:Envelope xmlns:soapenv=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns=\"http://www.juniper.es/webservice/2007/\">");
                CancelPolicyRQ.Append("<soapenv:Header/>");
                CancelPolicyRQ.Append("<soapenv:Body>");
                CancelPolicyRQ.Append("<HotelBookingRules>");
                CancelPolicyRQ.Append("<HotelBookingRulesRQ Version=\"1.1\" Context=\"VALUATION\" Language=\"en\">");
                CancelPolicyRQ.Append("<Login Email=\"" + UserId + "\" Password=\"" + Password + "\"/>");
                CancelPolicyRQ.Append("<HotelBookingRulesRequest>");
                CancelPolicyRQ.Append("<HotelOption RatePlanCode=\"" + roomtypeid + "\"/>");
                CancelPolicyRQ.Append("<SearchSegmentsHotels>");
                CancelPolicyRQ.Append("<SearchSegmentHotels Start=\"" + CheckIn + "\" End=\"" + CheckOut + "\"/>");
                CancelPolicyRQ.Append("<HotelCodes>");
                CancelPolicyRQ.Append("<HotelCode>" + hotelcode + "</HotelCode>");
                CancelPolicyRQ.Append("</HotelCodes>");
                CancelPolicyRQ.Append("</SearchSegmentsHotels>");
                CancelPolicyRQ.Append("</HotelBookingRulesRequest>");
                CancelPolicyRQ.Append("<AdvancedOptions>");
                CancelPolicyRQ.Append("<UseCurrency>" + Currency + "</UseCurrency>");
                CancelPolicyRQ.Append("</AdvancedOptions>");
                CancelPolicyRQ.Append("</HotelBookingRulesRQ>");
                CancelPolicyRQ.Append("</HotelBookingRules>");
                CancelPolicyRQ.Append("</soapenv:Body>");
                CancelPolicyRQ.Append("</soapenv:Envelope>");
                string searchRQString = CancelPolicyRQ.ToString();
                #endregion
                if (searchRQString != null)
                {
                    #region Cancellation Policy RS
                    string Error = "";
                   // string URL = Url + JuniperInfo.CheckAvail;
                    string URL = Url + _supplierConfig.Juniper.CheckAvail;
                    //string ResponseData =  GetSupplierResponse(ref Error, "", searchRQString, URL, SearchTimeOut, Provider, Searchpara);
                    //ResponseDto responseDto = await GetSupplierResponse("", searchRQString, URL, SearchTimeOut, Provider, Searchpara);
                    ResponseDto responseDto = await _commonMethod.GetSupplierResponse("", searchRQString, URL, SearchTimeOut, Provider, Searchpara);


                    string ResponseData = responseDto.Response;
                    Error = responseDto.Error;

                    //Common.ThirdPartyGetPolicyHistory(Convert.ToString(RoomTypeId_Own), Convert.ToString(ownsystemid), searchRQString, Convert.ToString(ResponseData), "After Get Policy", provider, "Response", "-", "");
                    if (ResponseData == string.Empty || ResponseData.Contains("Error"))
                        //return dsHotel;
                        ResponseData = ResponseData.Replace("<Address>", "").Replace("</Address>", "");
                    String Data2 = ResponseData.Substring(ResponseData.IndexOf("<HotelContent"), ResponseData.IndexOf("</HotelContent>") - ResponseData.IndexOf("<HotelContent")) + "</HotelContent>";
                    ResponseData = ResponseData.Replace(Data2, "");

                    DataSet Response = _commonMethod.GetDataSetFromXMLString(ResponseData);
                    if (Response.Tables.Contains("Errors") || Response.Tables.Contains("Error"))
                        return null;
                    #endregion

                    for (int i = 0; i < Response.Tables["HotelOption"].Rows.Count; i++)
                    {
                        #region DataRaw
                        DataTable dtHotelOption = Response.Tables["HotelOption"];
                        DataRow[] drHotelOption = (Response.Tables["HotelOption"].Select("HotelOption_Id='" + i + "'"));
                        int HotelOption_Id = Convert.ToInt32(drHotelOption[0]["HotelOption_Id"]);

                        DataRow[] drPriceInformation = (Response.Tables["PriceInformation"].Select("PriceInformation_Id='" + HotelOption_Id + "'"));
                        int PriceInformation_Id = Convert.ToInt32(drPriceInformation[0]["PriceInformation_Id"]);

                        //DataRow[] drBoard = (Response.Tables["Board"].Select("PriceInformation_Id='" + PriceInformation_Id + "'"));
                        //string Board_Text = Convert.ToString(drBoard[0]["Board_Text"]);

                        //string Board_Text = "";
                        //if (Response.Tables.Contains("Board"))
                        //{
                        //    if (Response.Tables["Board"].Columns.Contains("PriceInformation_Id"))
                        //    {
                        //        DataRow[] drBoard = (Response.Tables["Board"].Select("PriceInformation_Id='" + PriceInformation_Id + "'"));
                        //        Board_Text = Convert.ToString(drBoard[0]["Board_Text"]);
                        //    }
                        //}

                        string Board_Text = "";
                        if (Response.Tables.Contains("Board"))
                        {
                            if (Response.Tables["Board"].Columns.Contains("PriceInformation_Id"))
                            {
                                DataRow[] drBoard = (Response.Tables["Board"].Select("PriceInformation_Id='" + PriceInformation_Id + "'"));
                                Board_Text = Convert.ToString(drBoard[0]["Board_Text"]);
                            }
                        }
                        else
                        {
                            if (Response.Tables.Contains("PriceInformation"))
                            {
                                if (Response.Tables["PriceInformation"].Columns.Contains("PriceInformation_Id"))
                                {
                                    Board_Text = Convert.ToString(Response.Tables["PriceInformation"].Select("PriceInformation_Id='" + PriceInformation_Id + "'")[0]["Board"]);
                                }

                            }
                        }

                        DataRow[] drHotelRooms = (Response.Tables["HotelRooms"].Select("HotelRooms_Id='" + PriceInformation_Id + "'"));
                        int HotelRooms_Id = Convert.ToInt32(drHotelRooms[0]["HotelRooms_Id"]);

                        DataRow[] drHotelRoom = (Response.Tables["HotelRoom"].Select("HotelRooms_Id='" + HotelRooms_Id + "'"));
                        string Name = Convert.ToString(drHotelRoom[0]["Name"]);

                        DataRow[] drPrices = (Response.Tables["Prices"].Select("Prices_Id='" + PriceInformation_Id + "'"));
                        int Prices_Id = Convert.ToInt32(drPrices[0]["Prices_Id"]);

                        DataRow[] drPrice = (Response.Tables["Price"].Select("Price_Id='" + Prices_Id + "'"));
                        int Price_Id = Convert.ToInt32(drPrice[0]["Price_Id"]);

                        DataRow[] drTotalFixAmounts = (Response.Tables["TotalFixAmounts"].Select("TotalFixAmounts_Id='" + Price_Id + "'"));
                        int TotalFixAmounts_Id = Convert.ToInt32(drTotalFixAmounts[0]["TotalFixAmounts_Id"]);
                        //double Gross = Convert.ToDouble(drTotalFixAmounts[0]["Gross"]);

                        //if (provider.ToLower() == provider)
                        //Gross = Gross * CurrencyFactor;

                        string FirstDayCostCancellation_Text = Convert.ToString(DateTime.Now.Date);
                        if (Response.Tables.Contains("CancellationPolicy"))
                        {
                            DataRow[] drCancellationPolicy = (Response.Tables["CancellationPolicy"].Select("CancellationPolicy_Id='" + HotelOption_Id + "'"));
                            int CancellationPolicy_Id = Convert.ToInt32(drCancellationPolicy[0]["CancellationPolicy_Id"]);

                            if (Response.Tables.Contains("FirstDayCostCancellation"))
                            {
                                DataRow[] drFirstDayCostCancellation = (Response.Tables["FirstDayCostCancellation"].Select("CancellationPolicy_Id='" + CancellationPolicy_Id + "'"));
                                FirstDayCostCancellation_Text = Convert.ToString(drFirstDayCostCancellation[0]["FirstDayCostCancellation_Text"]);
                            }
                            else
                            {
                                FirstDayCostCancellation_Text = Convert.ToString(DateTime.Now.Date);
                            }

                        }
                        else
                        {
                            FirstDayCostCancellation_Text = Convert.ToString(DateTime.Now.Date);
                        }


                        //DataRow[] drCancellationPolicy = (Response.Tables["CancellationPolicy"].Select("CancellationPolicy_Id='" + HotelOption_Id + "'"));
                        //int CancellationPolicy_Id = Convert.ToInt32(drCancellationPolicy[0]["CancellationPolicy_Id"]);

                        //DataRow[] drFirstDayCostCancellation = (Response.Tables["FirstDayCostCancellation"].Select("CancellationPolicy_Id='" + CancellationPolicy_Id + "'"));
                        //string FirstDayCostCancellation_Text = Convert.ToString(drFirstDayCostCancellation[0]["FirstDayCostCancellation_Text"]);

                        //DataRow[] drPolicyRules = (Response.Tables["PolicyRules"].Select("PolicyRules_Id='" + CancellationPolicy_Id + "'"));
                        //int PolicyRules_Id = Convert.ToInt32(drPolicyRules[0]["PolicyRules_Id"]);

                        //DateTime curDate = Common.GetTimeZoneDate(objSearPera.CurrentDate);
                        string uniqid = Guid.NewGuid().ToString();
                        #endregion

                        #region Cancellation Policy Logic and Row
                        //Response.Tables["Rule"].DefaultView.Sort = "DateFrom ASC";

                        //DataRow[] drRule = (Response.Tables["Rule"].DefaultView.ToTable().Select("PolicyRules_Id='" + PolicyRules_Id + "'"));
                        #region Essential Info
                        if (Response.Tables["Comment"] != null)
                        {
                            DataRow[] drOptionalElements = (Response.Tables["OptionalElements"].Select("OptionalElements_Id='" + HotelOption_Id + "'"));
                            int OptionalElements_Id = Convert.ToInt32(drOptionalElements[0]["OptionalElements_Id"]);

                            DataRow[] drComments = (Response.Tables["Comments"].Select("Comments_Id='" + OptionalElements_Id + "'"));
                            int Comments_Id = Convert.ToInt32(drComments[0]["Comments_Id"]);

                            DataRow[] drComment = (Response.Tables["Comment"].Select("Comments_Id='" + Comments_Id + "'"));

                            for (int z = 0; z < drComment.Length; z++)
                            {
                                string Comments_Text = Convert.ToString(drComment[0]["Comment_Text"]);
                                if (z == 0)
                                    EssentialInfo = EssentialInfo + Comments_Text;
                                else
                                    EssentialInfo = EssentialInfo + "<br>" + Comments_Text;
                            }
                        }
                        else

                            EssentialInfo = "No Essential Information Available";
                        if (Board_Text != string.Empty)
                        {
                            if (EssentialInfo == "No Essential Information Available")
                            {
                                EssentialInfo = Board_Text;
                            }
                            else
                            {
                                EssentialInfo = EssentialInfo + "<br>" + Board_Text;
                            }
                        }
                        #endregion

                        //}
                        //}

                        #endregion
                    }
                }
            }
            catch (Exception e)
            {
                await _exception.ErrorNew(e, _LogAPI.Recheck, SearchKey);
            }
            return EssentialInfo;
            //return EssentialinfoTA;
        }

        public async Task<string> Get_JuniperNew_Robios_EssentialInfoOnly(HotelSearchParameter Searchpara, string hotelcode, string RoomTypeId_Own, DataRow[] drSuplierMarkupTA, DataTable dtSearchConfig, string Provider, DataTable dtSuplierMarkup, string Currency) //Added on 10-APR-2025
        {
            string EssentialInfo = "";

            //string Currency = "";
            //string Currency = Common.Get_JuniperNEW_Currency(Provider);
            try
            {
                #region Cancellation Policy RQ

                double AgentCompanyPercentage = Convert.ToDouble(Searchpara.AgentCompanyPercentage);
                bool IsB2C = Convert.ToBoolean(Convert.ToByte(Searchpara.IsB2BOrB2C));
                double B2CMarkup = (IsB2C == false ? AgentCompanyPercentage : XMLHotelBookingCommon.B2CMarkup);

                int CutOffDays = XMLHotelBookingCommon.CutOffDays;
                //CutOffDays = Common.GetCutOffDays(dtSuplierMarkup, provider);
                DataRow[] drSuplierMarkup = (dtSuplierMarkup.Select("SupplierName='" + Provider + "'"));
                //string UserId = Convert.ToString(drSuplierMarkup[0]["UserId"]);
                //string Password = Convert.ToString(drSuplierMarkup[0]["Password"]);
                string CheckIn = Searchpara.CheckInDate;
                CheckIn = CheckIn.Substring(6, 4) + "-" + CheckIn.Substring(3, 2) + "-" + CheckIn.Substring(0, 2);
                string CheckOut = Searchpara.CheckOutDate;
                CheckOut = CheckOut.Substring(6, 4) + "-" + CheckOut.Substring(3, 2) + "-" + CheckOut.Substring(0, 2);
                string Url = Convert.ToString(drSuplierMarkup[0]["WebSiteURL"]);
                int SID = Convert.ToInt32(drSuplierMarkup[0]["SupplierID"]);
                DataRow[] drSearchConfig = (dtSearchConfig.Select("SupplierID='" + SID + "'"));
                int SearchTimeOut = Convert.ToInt32(drSearchConfig[0]["BookingConfirmationTimeout"]);
                string UserId = Convert.ToString(drSuplierMarkup[0]["UserId"]);
                string Password = Convert.ToString(drSuplierMarkup[0]["Password"]);
                string Nationality = "";
                string roomtypeid = Get_LOH_RoomTypeID(RoomTypeId_Own, ref Nationality);

                StringBuilder CancelPolicyRQ = new StringBuilder();
                CancelPolicyRQ.Append("<soapenv:Envelope xmlns:soapenv=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns=\"http://www.juniper.es/webservice/2007/\">");
                CancelPolicyRQ.Append("<soapenv:Header/>");
                CancelPolicyRQ.Append("<soapenv:Body>");
                CancelPolicyRQ.Append("<HotelBookingRules>");
                CancelPolicyRQ.Append("<HotelBookingRulesRQ Version=\"1.1\" Context=\"VALUATION\" Language=\"en\">");
                CancelPolicyRQ.Append("<Login Email=\"" + UserId + "\" Password=\"" + Password + "\"/>");
                CancelPolicyRQ.Append("<HotelBookingRulesRequest>");
                CancelPolicyRQ.Append("<HotelOption RatePlanCode=\"" + roomtypeid + "\"/>");
                CancelPolicyRQ.Append("<SearchSegmentsHotels>");
                CancelPolicyRQ.Append("<SearchSegmentHotels Start=\"" + CheckIn + "\" End=\"" + CheckOut + "\"/>");
                CancelPolicyRQ.Append("<HotelCodes>");
                CancelPolicyRQ.Append("<HotelCode>" + hotelcode + "</HotelCode>");
                CancelPolicyRQ.Append("</HotelCodes>");
                CancelPolicyRQ.Append("</SearchSegmentsHotels>");
                CancelPolicyRQ.Append("</HotelBookingRulesRequest>");
                CancelPolicyRQ.Append("<AdvancedOptions>");
                CancelPolicyRQ.Append("<UseCurrency>" + Currency + "</UseCurrency>");
                CancelPolicyRQ.Append("</AdvancedOptions>");
                CancelPolicyRQ.Append("</HotelBookingRulesRQ>");
                CancelPolicyRQ.Append("</HotelBookingRules>");
                CancelPolicyRQ.Append("</soapenv:Body>");
                CancelPolicyRQ.Append("</soapenv:Envelope>");
                string searchRQString = CancelPolicyRQ.ToString();
                #endregion
                if (searchRQString != null)
                {
                    #region Cancellation Policy RS
                    string Error = "";
                    //string URL = Url + JuniperInfo.CheckAvail;
                    string URL = Url + _supplierConfig.Juniper.CheckAvail;
                    //string ResponseData = GetSupplierResponse(ref Error, "", searchRQString, URL, SearchTimeOut, Provider, Searchpara);
                    //ResponseDto responseDto = await GetSupplierResponse("", searchRQString, URL, SearchTimeOut, Provider, Searchpara);
                    ResponseDto responseDto = await _commonMethod.GetSupplierResponse("", searchRQString, URL, SearchTimeOut, Provider, Searchpara);

                    string ResponseData = responseDto.Response;
                    Error = responseDto.Error;

                    //Common.ThirdPartyGetPolicyHistory(Convert.ToString(RoomTypeId_Own), Convert.ToString(ownsystemid), searchRQString, Convert.ToString(ResponseData), "After Get Policy", provider, "Response", "-", "");
                    if (ResponseData == string.Empty || ResponseData.Contains("Error"))
                        //return dsHotel;
                        ResponseData = ResponseData.Replace("<Address>", "").Replace("</Address>", "");
                    String Data2 = ResponseData.Substring(ResponseData.IndexOf("<HotelContent"), ResponseData.IndexOf("</HotelContent>") - ResponseData.IndexOf("<HotelContent")) + "</HotelContent>";
                    ResponseData = ResponseData.Replace(Data2, "");

                    DataSet Response = _commonMethod.GetDataSetFromXMLString(ResponseData);
                    if (Response.Tables.Contains("Errors") || Response.Tables.Contains("Error"))
                        return null;
                    #endregion

                    for (int i = 0; i < Response.Tables["HotelOption"].Rows.Count; i++)
                    {
                        #region DataRaw
                        DataTable dtHotelOption = Response.Tables["HotelOption"];
                        DataRow[] drHotelOption = (Response.Tables["HotelOption"].Select("HotelOption_Id='" + i + "'"));
                        int HotelOption_Id = Convert.ToInt32(drHotelOption[0]["HotelOption_Id"]);

                        DataRow[] drPriceInformation = (Response.Tables["PriceInformation"].Select("PriceInformation_Id='" + HotelOption_Id + "'"));
                        int PriceInformation_Id = Convert.ToInt32(drPriceInformation[0]["PriceInformation_Id"]);

                        //DataRow[] drBoard = (Response.Tables["Board"].Select("PriceInformation_Id='" + PriceInformation_Id + "'"));
                        //string Board_Text = Convert.ToString(drBoard[0]["Board_Text"]);

                        //string Board_Text = "";
                        //if (Response.Tables.Contains("Board"))
                        //{
                        //    if (Response.Tables["Board"].Columns.Contains("PriceInformation_Id"))
                        //    {
                        //        DataRow[] drBoard = (Response.Tables["Board"].Select("PriceInformation_Id='" + PriceInformation_Id + "'"));
                        //        Board_Text = Convert.ToString(drBoard[0]["Board_Text"]);
                        //    }
                        //}

                        string Board_Text = "";
                        if (Response.Tables.Contains("Board"))
                        {
                            if (Response.Tables["Board"].Columns.Contains("PriceInformation_Id"))
                            {
                                DataRow[] drBoard = (Response.Tables["Board"].Select("PriceInformation_Id='" + PriceInformation_Id + "'"));
                                Board_Text = Convert.ToString(drBoard[0]["Board_Text"]);
                            }
                        }
                        else
                        {
                            if (Response.Tables.Contains("PriceInformation"))
                            {
                                if (Response.Tables["PriceInformation"].Columns.Contains("PriceInformation_Id"))
                                {
                                    Board_Text = Convert.ToString(Response.Tables["PriceInformation"].Select("PriceInformation_Id='" + PriceInformation_Id + "'")[0]["Board"]);
                                }

                            }
                        }

                        DataRow[] drHotelRooms = (Response.Tables["HotelRooms"].Select("HotelRooms_Id='" + PriceInformation_Id + "'"));
                        int HotelRooms_Id = Convert.ToInt32(drHotelRooms[0]["HotelRooms_Id"]);

                        DataRow[] drHotelRoom = (Response.Tables["HotelRoom"].Select("HotelRooms_Id='" + HotelRooms_Id + "'"));
                        string Name = Convert.ToString(drHotelRoom[0]["Name"]);

                        DataRow[] drPrices = (Response.Tables["Prices"].Select("Prices_Id='" + PriceInformation_Id + "'"));
                        int Prices_Id = Convert.ToInt32(drPrices[0]["Prices_Id"]);

                        DataRow[] drPrice = (Response.Tables["Price"].Select("Price_Id='" + Prices_Id + "'"));
                        int Price_Id = Convert.ToInt32(drPrice[0]["Price_Id"]);

                        DataRow[] drTotalFixAmounts = (Response.Tables["TotalFixAmounts"].Select("TotalFixAmounts_Id='" + Price_Id + "'"));
                        int TotalFixAmounts_Id = Convert.ToInt32(drTotalFixAmounts[0]["TotalFixAmounts_Id"]);
                        //double Gross = Convert.ToDouble(drTotalFixAmounts[0]["Gross"]);

                        //if (provider.ToLower() == provider)
                        //Gross = Gross * CurrencyFactor;

                        string FirstDayCostCancellation_Text = Convert.ToString(DateTime.Now.Date);
                        if (Response.Tables.Contains("CancellationPolicy"))
                        {
                            DataRow[] drCancellationPolicy = (Response.Tables["CancellationPolicy"].Select("CancellationPolicy_Id='" + HotelOption_Id + "'"));
                            int CancellationPolicy_Id = Convert.ToInt32(drCancellationPolicy[0]["CancellationPolicy_Id"]);

                            if (Response.Tables.Contains("FirstDayCostCancellation"))
                            {
                                DataRow[] drFirstDayCostCancellation = (Response.Tables["FirstDayCostCancellation"].Select("CancellationPolicy_Id='" + CancellationPolicy_Id + "'"));
                                FirstDayCostCancellation_Text = Convert.ToString(drFirstDayCostCancellation[0]["FirstDayCostCancellation_Text"]);
                            }
                            else
                            {
                                FirstDayCostCancellation_Text = Convert.ToString(DateTime.Now.Date);
                            }

                        }
                        else
                        {
                            FirstDayCostCancellation_Text = Convert.ToString(DateTime.Now.Date);
                        }


                        //DataRow[] drCancellationPolicy = (Response.Tables["CancellationPolicy"].Select("CancellationPolicy_Id='" + HotelOption_Id + "'"));
                        //int CancellationPolicy_Id = Convert.ToInt32(drCancellationPolicy[0]["CancellationPolicy_Id"]);

                        //DataRow[] drFirstDayCostCancellation = (Response.Tables["FirstDayCostCancellation"].Select("CancellationPolicy_Id='" + CancellationPolicy_Id + "'"));
                        //string FirstDayCostCancellation_Text = Convert.ToString(drFirstDayCostCancellation[0]["FirstDayCostCancellation_Text"]);

                        //DataRow[] drPolicyRules = (Response.Tables["PolicyRules"].Select("PolicyRules_Id='" + CancellationPolicy_Id + "'"));
                        //int PolicyRules_Id = Convert.ToInt32(drPolicyRules[0]["PolicyRules_Id"]);

                        //DateTime curDate = Common.GetTimeZoneDate(objSearPera.CurrentDate);
                        string uniqid = Guid.NewGuid().ToString();
                        #endregion

                        #region Cancellation Policy Logic and Row
                        //Response.Tables["Rule"].DefaultView.Sort = "DateFrom ASC";

                        //DataRow[] drRule = (Response.Tables["Rule"].DefaultView.ToTable().Select("PolicyRules_Id='" + PolicyRules_Id + "'"));
                        #region Essential Info
                        if (Response.Tables["Comment"] != null)
                        {
                            DataRow[] drOptionalElements = (Response.Tables["OptionalElements"].Select("OptionalElements_Id='" + HotelOption_Id + "'"));
                            int OptionalElements_Id = Convert.ToInt32(drOptionalElements[0]["OptionalElements_Id"]);

                            DataRow[] drComments = (Response.Tables["Comments"].Select("Comments_Id='" + OptionalElements_Id + "'"));
                            int Comments_Id = Convert.ToInt32(drComments[0]["Comments_Id"]);

                            DataRow[] drComment = (Response.Tables["Comment"].Select("Comments_Id='" + Comments_Id + "'"));

                            for (int z = 0; z < drComment.Length; z++)
                            {
                                string Comments_Text = Convert.ToString(drComment[0]["Comment_Text"]);
                                if (z == 0)
                                    EssentialInfo = EssentialInfo + Comments_Text;
                                else
                                    EssentialInfo = EssentialInfo + "<br>" + Comments_Text;
                            }
                        }
                        else

                            EssentialInfo = "No Essential Information Available";
                        if (Board_Text != string.Empty)
                        {
                            if (EssentialInfo == "No Essential Information Available")
                            {
                                EssentialInfo = Board_Text;
                            }
                            else
                            {
                                EssentialInfo = EssentialInfo + "<br>" + Board_Text;
                            }
                        }
                        #endregion

                        //}
                        //}

                        #endregion
                    }
                }
            }
            catch (Exception e)
            {
                _exception.Error(e);
            }
            return EssentialInfo;
            //return EssentialinfoTA;
        }
        public string Get_LOH_RoomTypeID(string RoomTypeId, ref string Nationality)
        {
            try
            {
                string[] spitid = RoomTypeId.Split(new String[] { "$$$" }, StringSplitOptions.None);
                string LOH_RoomId = spitid[0].ToString();
                Nationality = spitid[2].ToString();
               
                return LOH_RoomId;
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
                return "";
            }
        }

        public async Task<ResponseDto> GetSupplierResponse(string strUniqueId, string strxmlRQRS, string strHost, Int32 TimeOut, string Provider = "", HotelSearchParameter para = null, bool DebugMode = false, string Searchkey = "")
        {
            ResponseDto responseDto = new ResponseDto();
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
            string Error = "";
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
                            await _commonMethod.XMLRequestResponseTrack(strxmlRQRS, strOutput, Provider, "Proper");
                    }
                    if (_keySettings.IsLogTrack == "1")
                    {
                        TimeSpan timeTaken = timer.Elapsed;

                        //ErrorLog(Convert.ToString(System.Web.Configuration.WebConfigurationManager.AppSettings["LogTrackPath"]), Provider + "->" + strHost + "-> Execution Time : " + timer.Elapsed.ToString());
                        await _commonMethod.XMLRequestResponseTrack(strxmlRQRS, strOutput, Provider, "Proper");
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
                            //_commonMethod.XMLRequestResponseTrackError(strxmlRQRS, result1, Provider, Searchkey);
                            Error = result1.ToString();
                            responseDto.Response = result1;
                            responseDto.Error = Error;
                            return responseDto;
                        }
                    }
                    catch (Exception ex)
                    {
                        _exception.Error(ex, Searchkey);
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
                _exception.Error(ex, Searchkey);
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

        public string GetSupplierResponse_report(ref string Error, string strUniqueId, string strxmlRQRS, string strHost, Int32 TimeOut, string Provider = "", HotelSearchParameter para = null, bool DebugMode = false, string Searchkey = "")
        {
            System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
            timer.Start();

            string strOutput = "";
            try
            {
                HttpWebRequest Request;
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

                Request.Headers.Add("Accept-Encoding", "gzip");

                Stream newStream = null;

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

                        //ErrorLog(Convert.ToString(System.Web.Configuration.WebConfigurationManager.AppSettings["LogTrackPath"]), Provider + "->" + strHost + "-> Execution Time : " + timer.Elapsed.ToString());
                        _ = _commonMethod.XMLRequestResponseTrack(strxmlRQRS, strOutput, Provider, "Proper");
                    }

                }
                catch (WebException webExcp)
                {
                    try
                    {
                        try//Added on 15-MAR-2025
                        {
                            _ = _exception.ErrorBooking(webExcp, "", Provider, Searchkey);
                        }
                        catch (Exception x)
                        {
                            _ = _exception.ErrorBooking(x, "", Provider, Searchkey);
                        }

                        HttpWebResponse httpResponse = (HttpWebResponse)webExcp.Response;
                        if (httpResponse.StatusDescription != null)
                        {
                            string result = httpResponse.StatusDescription.ToString();
                            string result1 = new StreamReader(webExcp.Response.GetResponseStream()).ReadToEnd();

                            _ = _commonMethod.XMLRequestResponseTrackError(strxmlRQRS, result1, Provider, Searchkey);
                            Error = result1;
                            return result1;
                        }
                    }
                    catch (Exception ex)
                    {
                        Error = ex.Message.ToString();
                        _ = _exception.ErrorBooking(ex, "", "", Searchkey);
                        return "";
                    }

                    //return result1;
                }
            }
            catch (Exception ex)
            {
                _exception.ErrorNew(ex, _LogAPI.Book, Searchkey);
                Error = ex.Message.ToString();

            }
            //trackRequest(strxmlRQRS, strOutput);
            return strOutput;
        }
        public async Task<ResponseDto> GetSupplierResponse_report_new(string Error, string strUniqueId, string strxmlRQRS, string strHost, Int32 TimeOut, string Provider = "", HotelSearchParameter para = null, bool DebugMode = false, string Searchkey = "")
        {
            ResponseDto responseDto = new ResponseDto();
            System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
            timer.Start();

            string strOutput = "";
            try
            {
                HttpWebRequest Request;
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

                Request.Headers.Add("Accept-Encoding", "gzip");

                Stream newStream = null;

                byte[] data = Encoding.ASCII.GetBytes(strxmlRQRS);
                Request.ContentLength = data.Length;

                newStream = Request.GetRequestStream();
                newStream.Write(data, 0, data.Length);

                try
                {
                    HttpWebResponse Response = (HttpWebResponse)Request.GetResponse();


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
                        _ = _commonMethod.XMLRequestResponseTrack(strxmlRQRS, strOutput, Provider, "Proper");
                    }

                }
                catch (WebException webExcp)
                {
                    try
                    {
                        try//Added on 15-MAR-2025
                        {
                            _ = _exception.ErrorBooking(webExcp, "", Provider, Searchkey);
                        }
                        catch (Exception x)
                        {
                            _ = _exception.ErrorBooking(x, "", Provider, Searchkey);
                        }

                        HttpWebResponse httpResponse = (HttpWebResponse)webExcp.Response;
                        if (httpResponse.StatusDescription != null)
                        {
                            string result = httpResponse.StatusDescription.ToString();
                            string result1 = new StreamReader(webExcp.Response.GetResponseStream()).ReadToEnd();

                            _ = _commonMethod.XMLRequestResponseTrackError(strxmlRQRS, result1, Provider, Searchkey);
                            Error = result1;

                            responseDto.Response = "";
                            responseDto.Error = Error;
                            return responseDto;
                        }
                    }
                    catch (Exception ex)
                    {
                        Error = ex.Message.ToString();
                        _ = _exception.ErrorBooking(ex, "", "", Searchkey);
                        responseDto.Response = "";
                        responseDto.Error = Error;
                        return responseDto;
                    }


                }
            }
            catch (Exception ex)
            {
                _exception.ErrorNew(ex, _LogAPI.Book, Searchkey);
                Error = ex.Message.ToString();

            }

            responseDto.Response = strOutput;
            responseDto.Error = Error;

            return responseDto;

        }


        //recheck
        public async Task<CancellationDto> Get_JuniperNew_CancellationPolicy(DataSet dsHotel, string hotelcode, string ownsystemid, string RoomTypeId_Own, DataTable dtAgentDetail, DataTable dtSuplierMarkup, DataTable dtMarkupDetails, DataTable dtSearchConfig, HotelSearchParameter objSearPera, string roomno, string provider, int AgentId, string SearchKey, string GiataId = "0", int hotelRating = 0, TokenData tokenData = null)
        {
            CancellationDto cancellationDto = new CancellationDto();
            string Error = "";
            try
            {
                DataTable dtJuniperNew_Cancellation = _commonMethod.GenerateCancellationDataTableNewV2();
                DataTable dtInrCurrency = new DataTable();



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
                //hotelRating = Convert.ToInt32(dsHotel.Tables[0].Rows[0]["Rating"]);
                _markup.getAgetMarkupSetting(provider, GiataId, dtSuplierMarkup, dtMarkupDetails, ref AgentPercentage, ref AgentMarkupAmount, ref blnIsShowSupplier, ref isPerce, AgentId, hotelRating, objSearPera.CityID, objSearPera.CountryID.ToString(), objSearPera.CheckInDate);
                if (dtAgentDetail != null)
                {
                    IsParentId = Convert.ToInt32(dtAgentDetail.Rows[0]["IsParentId"]);
                    SubAgentMarkup = Convert.ToDouble(dtAgentDetail.Rows[0]["B2CMarkupPer"]);
                }
                int CutOffDays = XMLHotelBookingCommon.CutOffDays;
                CutOffDays = _commonMethod.GetCutOffDays(dtSuplierMarkup, provider);
                DataRow[] drSuplierMarkup = (dtSuplierMarkup.Select("SupplierName='" + provider + "'"));
                string UserId = Convert.ToString(drSuplierMarkup[0]["UserId"]);
                string Password = Convert.ToString(drSuplierMarkup[0]["Password"]);

                string Currency = Convert.ToString(drSuplierMarkup[0]["CurrencyShortName"]);

                double CurrencyFactor = 1;
                if (Currency.ToLower() != tokenData.SiteCurrency.ToLower())
                {
                    
                    dtInrCurrency = _commonMethod.GetCountryCurrency(Currency); ;
                    CurrencyFactor = Convert.ToDouble(dtInrCurrency.Rows[0]["AmtToExch"]);
                }

                string CheckIn = objSearPera.CheckInDate;
                CheckIn = CheckIn.Substring(6, 4) + "-" + CheckIn.Substring(3, 2) + "-" + CheckIn.Substring(0, 2);
                string CheckOut = objSearPera.CheckOutDate;
                CheckOut = CheckOut.Substring(6, 4) + "-" + CheckOut.Substring(3, 2) + "-" + CheckOut.Substring(0, 2);
                string Url = Convert.ToString(drSuplierMarkup[0]["WebSiteURL"]);
                int SID = Convert.ToInt32(drSuplierMarkup[0]["SupplierID"]);
                DataRow[] drSearchConfig = (dtSearchConfig.Select("SupplierID='" + SID + "'"));
                int SearchTimeOut = Convert.ToInt32(drSearchConfig[0]["BookingConfirmationTimeout"]);
                string Nationality = "";
                string roomtypeid = Get_LOH_RoomTypeID(RoomTypeId_Own, ref Nationality);

                StringBuilder CancelPolicyRQ = new StringBuilder();
                CancelPolicyRQ.Append("<soapenv:Envelope xmlns:soapenv=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns=\"http://www.juniper.es/webservice/2007/\">");
                CancelPolicyRQ.Append("<soapenv:Header/>");
                CancelPolicyRQ.Append("<soapenv:Body>");
                CancelPolicyRQ.Append("<HotelBookingRules>");
                CancelPolicyRQ.Append("<HotelBookingRulesRQ Version=\"1.1\" Context=\"VALUATION\" Language=\"en\">");
                CancelPolicyRQ.Append("<Login Email=\"" + UserId + "\" Password=\"" + Password + "\"/>");
                CancelPolicyRQ.Append("<HotelBookingRulesRequest>");
                CancelPolicyRQ.Append("<HotelOption RatePlanCode=\"" + roomtypeid + "\"/>");
                CancelPolicyRQ.Append("<SearchSegmentsHotels>");
                CancelPolicyRQ.Append("<SearchSegmentHotels Start=\"" + CheckIn + "\" End=\"" + CheckOut + "\"/>");
                CancelPolicyRQ.Append("<HotelCodes>");
                CancelPolicyRQ.Append("<HotelCode>" + hotelcode + "</HotelCode>");
                CancelPolicyRQ.Append("</HotelCodes>");
                CancelPolicyRQ.Append("</SearchSegmentsHotels>");
                CancelPolicyRQ.Append("</HotelBookingRulesRequest>");
                CancelPolicyRQ.Append("<AdvancedOptions>");
                CancelPolicyRQ.Append("<UseCurrency>" + Currency + "</UseCurrency>");
                CancelPolicyRQ.Append("</AdvancedOptions>");
                CancelPolicyRQ.Append("</HotelBookingRulesRQ>");
                CancelPolicyRQ.Append("</HotelBookingRules>");
                CancelPolicyRQ.Append("</soapenv:Body>");
                CancelPolicyRQ.Append("</soapenv:Envelope>");
                string searchRQString = CancelPolicyRQ.ToString();
                #endregion

                if (searchRQString != null)
                {
                    #region Cancellation Policy RS
                    string URL = Url + _supplierConfig.Juniper.CheckAvail;
                   
                    ResponseDto responseDto = await GetSupplierResponse("", searchRQString, URL, SearchTimeOut, provider, objSearPera);
                    string ResponseData = responseDto.Response;
                    Error = responseDto.Error;

                    _commonMethod.ThirdPartyGetPolicyHistory(Convert.ToString(RoomTypeId_Own), Convert.ToString(ownsystemid), searchRQString, Convert.ToString(ResponseData), "After Get Policy", provider, "Response", SearchKey, "");
                    if (ResponseData == string.Empty || ResponseData.Contains("Error"))
                    {
                        cancellationDto.dsHotelData = null;
                        cancellationDto.ISBooking = "NotAvailable";
                        return cancellationDto;

                    }

                    String Data2 = ResponseData.Substring(ResponseData.IndexOf("<HotelContent"), ResponseData.IndexOf("</HotelContent>") - ResponseData.IndexOf("<HotelContent")) + "</HotelContent>";
                    ResponseData = ResponseData.Replace(Data2, "");

                    DataSet Response = _commonMethod.GetDataSetFromXMLString(ResponseData);
                    if (Response.Tables.Contains("Errors") || Response.Tables.Contains("Error"))
                    {
                        cancellationDto.dsHotelData = null;
                        cancellationDto.ISBooking = "NotAvailable";
                        return cancellationDto;
                    }

                    #endregion

                    for (int i = 0; i < Response.Tables["HotelOption"].Rows.Count; i++)
                    {
                        #region DataRaw
                        DataTable dtHotelOption = Response.Tables["HotelOption"];
                        DataRow[] drHotelOption = (Response.Tables["HotelOption"].Select("HotelOption_Id='" + i + "'"));
                        int HotelOption_Id = Convert.ToInt32(drHotelOption[0]["HotelOption_Id"]);

                        DataRow[] drPriceInformation = (Response.Tables["PriceInformation"].Select("PriceInformation_Id='" + HotelOption_Id + "'"));
                        int PriceInformation_Id = Convert.ToInt32(drPriceInformation[0]["PriceInformation_Id"]);

                        //DataRow[] drBoard = (Response.Tables["Board"].Select("PriceInformation_Id='" + PriceInformation_Id + "'"));
                        //string Board_Text = Convert.ToString(drBoard[0]["Board_Text"]);

                        //string Board_Text = "";
                        //if (Response.Tables.Contains("Board"))
                        //{
                        //    if (Response.Tables["Board"].Columns.Contains("PriceInformation_Id"))
                        //    {
                        //        DataRow[] drBoard = (Response.Tables["Board"].Select("PriceInformation_Id='" + PriceInformation_Id + "'"));
                        //        Board_Text = Convert.ToString(drBoard[0]["Board_Text"]);
                        //    }
                        //}

                        string Board_Text = "";
                        if (Response.Tables.Contains("Board"))
                        {
                            if (Response.Tables["Board"].Columns.Contains("PriceInformation_Id"))
                            {
                                DataRow[] drBoard = (Response.Tables["Board"].Select("PriceInformation_Id='" + PriceInformation_Id + "'"));
                                Board_Text = Convert.ToString(drBoard[0]["Board_Text"]);
                            }
                        }
                        else
                        {
                            if (Response.Tables.Contains("PriceInformation"))
                            {
                                if (Response.Tables["PriceInformation"].Columns.Contains("PriceInformation_Id"))
                                {
                                    Board_Text = Convert.ToString(Response.Tables["PriceInformation"].Select("PriceInformation_Id='" + PriceInformation_Id + "'")[0]["Board"]);
                                }

                            }
                        }

                        DataRow[] drHotelRooms = (Response.Tables["HotelRooms"].Select("HotelRooms_Id='" + PriceInformation_Id + "'"));
                        int HotelRooms_Id = Convert.ToInt32(drHotelRooms[0]["HotelRooms_Id"]);

                        DataRow[] drHotelRoom = (Response.Tables["HotelRoom"].Select("HotelRooms_Id='" + HotelRooms_Id + "'"));
                        string Name = Convert.ToString(drHotelRoom[0]["Name"]);

                        DataRow[] drPrices = (Response.Tables["Prices"].Select("Prices_Id='" + PriceInformation_Id + "'"));
                        int Prices_Id = Convert.ToInt32(drPrices[0]["Prices_Id"]);

                        DataRow[] drPrice = (Response.Tables["Price"].Select("Price_Id='" + Prices_Id + "'"));
                        int Price_Id = Convert.ToInt32(drPrice[0]["Price_Id"]);

                        DataRow[] drTotalFixAmounts = (Response.Tables["TotalFixAmounts"].Select("TotalFixAmounts_Id='" + Price_Id + "'"));
                        int TotalFixAmounts_Id = Convert.ToInt32(drTotalFixAmounts[0]["TotalFixAmounts_Id"]);
                        double Gross = Convert.ToDouble(drTotalFixAmounts[0]["Gross"]);

                        //if (provider.ToLower() == provider)
                        Gross = Gross * CurrencyFactor;

                        string FirstDayCostCancellation_Text = Convert.ToString(DateTime.Now.Date);
                        if (Response.Tables.Contains("CancellationPolicy"))
                        {
                            DataRow[] drCancellationPolicy = (Response.Tables["CancellationPolicy"].Select("CancellationPolicy_Id='" + HotelOption_Id + "'"));
                            int CancellationPolicy_Id = Convert.ToInt32(drCancellationPolicy[0]["CancellationPolicy_Id"]);

                            if (Response.Tables.Contains("FirstDayCostCancellation"))
                            {
                                DataRow[] drFirstDayCostCancellation = (Response.Tables["FirstDayCostCancellation"].Select("CancellationPolicy_Id='" + CancellationPolicy_Id + "'"));
                                FirstDayCostCancellation_Text = Convert.ToString(drFirstDayCostCancellation[0]["FirstDayCostCancellation_Text"]);
                            }
                            else
                            {
                                FirstDayCostCancellation_Text = Convert.ToString(DateTime.Now.Date);
                            }

                        }
                        else
                        {
                            FirstDayCostCancellation_Text = Convert.ToString(DateTime.Now.Date);
                        }

                        string uniqid = Guid.NewGuid().ToString();
                        #endregion

                        #region Cancellation Policy Logic and Row
                         #region Essential Info
                        string EssentialInfo = "";
                        if (Response.Tables["Comment"] != null)
                        {
                            DataRow[] drOptionalElements = (Response.Tables["OptionalElements"].Select("OptionalElements_Id='" + HotelOption_Id + "'"));
                            int OptionalElements_Id = Convert.ToInt32(drOptionalElements[0]["OptionalElements_Id"]);

                            DataRow[] drComments = (Response.Tables["Comments"].Select("Comments_Id='" + OptionalElements_Id + "'"));
                            int Comments_Id = Convert.ToInt32(drComments[0]["Comments_Id"]);

                            DataRow[] drComment = (Response.Tables["Comment"].Select("Comments_Id='" + Comments_Id + "'"));

                            for (int z = 0; z < drComment.Length; z++)
                            {
                                string Comments_Text = Convert.ToString(drComment[0]["Comment_Text"]);
                                if (z == 0)
                                    EssentialInfo = EssentialInfo + Comments_Text;
                                else
                                    EssentialInfo = EssentialInfo + "<br>" + Comments_Text;
                            }
                        }
                        else
                            EssentialInfo = "No Essential Information Available";
                        if (Board_Text != string.Empty)
                        {
                            if (EssentialInfo == "No Essential Information Available")
                            {
                                EssentialInfo = Board_Text;
                            }
                            else
                            {
                                EssentialInfo = EssentialInfo + "<br>" + Board_Text;
                            }
                        }
                        #endregion
                       
                        double CancelCharge = Gross;

                        DateTime CancelDate = DateTime.Now.Date;
                        CancelDate = Convert.ToDateTime(FirstDayCostCancellation_Text);
                        DateTime NowDate = DateTime.Now.Date;
                        if (CancelDate <= DateTime.Now.Date)
                            CancelDate = DateTime.Now.Date;
                        else
                            CancelDate = CancelDate.AddDays(CutOffDays).Date;
                        #region Vat
                        double VatOnPolicy = 0;
                        double MainAgentCharge = CancelCharge;
                        double CompanyChargeAmount = CancelCharge;
                        CancelCharge = _markup.ProcessHotelCancelationPrice_WithMarkup(ref MainAgentCharge, objSearPera.Rooms.Count * objSearPera.NoofNights, objSearPera.CityID, objSearPera.CountryID.ToString(), CancelCharge, isPerce, B2CMarkup, AgentPercentage, AgentMarkupAmount, IsParentId, SubAgentMarkup, ref VatOnPolicy);
                        #endregion
                        if (CancelDate.Date < DateTime.Now.Date)
                        {
                            CancelDate = DateTime.Now.Date;
                        }

                        if (NowDate < CancelDate && CancelDate.Date > DateTime.Now.Date)
                        {
                            double zero = 0;

                            dtJuniperNew_Cancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref zero, dtJuniperNew_Cancellation.NewRow(), CurrencyFactor, Currency, provider, ownsystemid, Convert.ToString(roomno),
                        Convert.ToString(RoomTypeId_Own), Name, Board_Text, DateTime.Now.Date, CancelDate.AddDays(-1), 0, 0, "", VatOnPolicy, "", "NO", "", EssentialInfo, "", ""));
                            dtJuniperNew_Cancellation.AcceptChanges();

                            dtJuniperNew_Cancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref CompanyChargeAmount, dtJuniperNew_Cancellation.NewRow(), CurrencyFactor, Currency, provider, ownsystemid, Convert.ToString(roomno),
                        Convert.ToString(RoomTypeId_Own), Name, Board_Text, Convert.ToDateTime(CancelDate), DateTime.ParseExact(objSearPera.CheckInDate, "dd/MM/yyyy", null), MainAgentCharge, CancelCharge, "", VatOnPolicy, "", "NO", "", EssentialInfo, "", ""));
                            dtJuniperNew_Cancellation.AcceptChanges();
                        }
                        else
                        {
                            if (CancelDate < DateTime.Now.Date)
                                CancelDate = DateTime.Now.Date;
                            dtJuniperNew_Cancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref CompanyChargeAmount, dtJuniperNew_Cancellation.NewRow(), CurrencyFactor, Currency, provider, ownsystemid, Convert.ToString(roomno),
                                                                           Convert.ToString(RoomTypeId_Own), Name, Board_Text, Convert.ToDateTime(CancelDate),
                                                                           DateTime.ParseExact(objSearPera.CheckInDate, "dd/MM/yyyy", null), MainAgentCharge, CancelCharge, "", 0, uniqid, "Yes", "", EssentialInfo));
                            dtJuniperNew_Cancellation.AcceptChanges();
                        }
                        
                        #endregion
                    }
                }
                dsHotel.Tables[2].Merge(dtJuniperNew_Cancellation);

                cancellationDto.dsHotelData = dsHotel;
                cancellationDto.ISBooking = "Available";

                return cancellationDto;
            }
            catch (Exception ex)
            {
                _ = _exception.ErrorNew(ex, _LogAPI.Recheck, SearchKey);
                cancellationDto.dsHotelData = null;
                cancellationDto.ISBooking = "NotAvailable";
                return cancellationDto;
            }
        }

        public async Task<PriceCheckCMDto> Get_JuniperNew_PriceCheckRQRS(int i, DataSet dsCartDetails, DataRow[] SupplierDetails, string SupplierName, int j, DataTable dtCheckPriceReqStructure, string UniqueNo, string AgentUniqueNo, double FinalPrice, bool isForBooking, string HotelId, int UserCartId, int UserCartDetailId, string RoomTypeId, double CompanyBuyingPrice, string NewRatePlan, bool IsError, string Currency, string Searchkey = "", int AgentId = 0)
        {
            PriceCheckCMDto priceCheckCMDto = new PriceCheckCMDto();

            string responsetype = "Success";
            string time = "";
            string Error = "";
            string xslrequest = "";
            string Responsedata_Ex = "";
            try
            {
                #region PriceCheck RQ
                string UserID = Convert.ToString(SupplierDetails[0]["UserId"]);
                string Password = Convert.ToString(SupplierDetails[0]["Password"]);
                string CheckIn = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["CheckInDate"]);
                CheckIn = CheckIn.Substring(0, 4) + "-" + CheckIn.Substring(4, 2) + "-" + CheckIn.Substring(6, 2);
                string CheckOut = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["CheckOutDate"]);
                CheckOut = CheckOut.Substring(0, 4) + "-" + CheckOut.Substring(4, 2) + "-" + CheckOut.Substring(6, 2);
                double CurrencyFactor = 1;
                if (Currency.ToLower() != "usd")
                {
                    DataTable dtInrCurrency = _commonMethod.GetCountryCurrency(Currency);
                    CurrencyFactor = Convert.ToDouble(dtInrCurrency.Rows[0]["AmtToExch"]);
                }

                string Nationality = "";
                string RoomTypeIds = Get_LOH_RoomTypeID(RoomTypeId, ref Nationality );

                xslrequest = JuniperNew_PriceCheck(UserID, Password, RoomTypeIds, CheckIn, CheckOut, HotelId, Currency);
                #endregion
                _commonMethod.ThirdPartyBookingHistory(Convert.ToString(UniqueNo), Convert.ToString(AgentUniqueNo), xslrequest, "", "Before get price " + SupplierName + " request hit " + j + "is for Price Check" + isForBooking, SupplierName, "response confirm", "-", "API Out",Searchkey);

                if (xslrequest != "")
                {
                    if (!isForBooking)
                        dtCheckPriceReqStructure.Rows.Add(_commonMethod.AddCheckPriceReq(dtCheckPriceReqStructure.NewRow(), j + 1, UserCartId, UserCartDetailId, RoomTypeId, SupplierName, xslrequest));

                    #region PriceCheck RS
                    
                    string URL = Convert.ToString(SupplierDetails[0]["websiteurl"]) + _supplierConfig.Juniper.CheckAvail;

                    System.Diagnostics.Stopwatch timerroomprocess = new System.Diagnostics.Stopwatch();
                    timerroomprocess.Start();

                    ResponseDto responseDto = await _commonMethod.GetSupplierResponse("", xslrequest, URL, 60000, SupplierName, null, false, Searchkey);
                    Responsedata_Ex = responseDto.Response;
                    Error = responseDto.Error;

                    timerroomprocess.Stop();
                    time = timerroomprocess.Elapsed.ToString();

                    _commonMethod.ThirdPartyBookingHistory(Convert.ToString(UniqueNo), Convert.ToString(AgentUniqueNo), xslrequest, Responsedata_Ex + Error, "After get price " + SupplierName + " request hit " + j + "is for Price Check" + isForBooking, SupplierName, "response confirm", "-", "API Out",Searchkey);

                    if (Responsedata_Ex == string.Empty || Responsedata_Ex.Contains("Error"))
                    {
                        try
                        {
                            responsetype = "GeneralError";
                            string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, xslrequest, Responsedata_Ex, Error);
                        }
                        catch (Exception e)
                        {
                            _ = _exception.Error(e, Searchkey);
                        }
                        IsError = true;
                        priceCheckCMDto.IsError = IsError;
                        priceCheckCMDto.Flag = false;

                        return priceCheckCMDto;
                    }

                    #endregion

                    if (Responsedata_Ex.Trim() != string.Empty)
                    {
                        try
                        {
                            DataSet Response = _commonMethod.GetDataSetFromXMLString(Responsedata_Ex);

                            #region DataRaw
                            if (Response.Tables.Contains("Errors") || Response.Tables.Contains("Error"))
                            {
                                try
                                {
                                    responsetype = "GeneralError";
                                    string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, xslrequest, Responsedata_Ex, Error);
                                }
                                catch (Exception e)
                                {
                                    _ = _exception.Error(e, Searchkey);
                                }

                                priceCheckCMDto.IsError = true;
                                priceCheckCMDto.Flag = false;
                                return priceCheckCMDto;
                            }
                            int Hid = 0;
                            DataTable dtHotelOption = Response.Tables["HotelOption"];
                            DataRow[] drHotelOption = (dtHotelOption.Select("HotelOption_Id='" + Hid + "'"));
                            int HotelOption_Id = Convert.ToInt32(drHotelOption[0]["HotelOption_Id"]);
                            string RoomCode = Convert.ToString(drHotelOption[0]["RatePlanCode"]);
                            string Status = Convert.ToString(drHotelOption[0]["Status"]);
                            #endregion

                            #region Price Update Logic
                            if (Status == "OK")
                            {
                                DataRow[] drPrices = (Response.Tables["Prices"].Select("Prices_Id='" + HotelOption_Id + "'"));
                                int Prices_Id = Convert.ToInt32(drPrices[0]["Prices_Id"]);

                                DataRow[] drPrice = (Response.Tables["Price"].Select("Price_Id='" + Prices_Id + "'"));
                                int Price_Id = Convert.ToInt32(drPrice[0]["Price_Id"]);

                                DataRow[] drTotalFixAmounts = (Response.Tables["TotalFixAmounts"].Select("TotalFixAmounts_Id='" + Price_Id + "'"));
                                int TotalFixAmounts_Id = Convert.ToInt32(drTotalFixAmounts[0]["TotalFixAmounts_Id"]);
                                FinalPrice = Convert.ToDouble(drTotalFixAmounts[0]["Gross"]);

                                //if (SupplierName.ToLower() == SupplierName)
                                FinalPrice = FinalPrice * CurrencyFactor;

                                NewRatePlan = RoomCode;

                                if (Convert.ToDouble(FinalPrice).TruncateDecimalPlaces(0) > CompanyBuyingPrice.TruncateDecimalPlaces(0))
                                {
                                    try
                                    {
                                        Responsedata_Ex = "#Price Changed " + "#Main Price = " + CompanyBuyingPrice + " #New Price = " + FinalPrice + " #Response= " + Responsedata_Ex;
                                        responsetype = "GeneralError";
                                        string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, xslrequest, Responsedata_Ex, Error);
                                    }
                                    catch (Exception e)
                                    {
                                        _ = _exception.Error(e, Searchkey);
                                    }

                                    priceCheckCMDto.Flag = true;
                                    priceCheckCMDto.ItemPrice = FinalPrice;
                                    priceCheckCMDto.IsError = false;
                                    return priceCheckCMDto;
                                }
                            }
                            else
                            {
                                try
                                {
                                    responsetype = "GeneralError";
                                    string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, xslrequest, Responsedata_Ex, Error);
                                }
                                catch (Exception e)
                                {
                                    _ = _exception.Error(e, Searchkey);
                                }
                                IsError = true;
                                priceCheckCMDto.IsError = IsError;
                            }
                            #endregion
                        }
                        catch (Exception ex)
                        {
                            try
                            {
                                responsetype = "GeneralError";
                                Error = ex.Message + ex.StackTrace.ToString();
                                string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, xslrequest, Responsedata_Ex, Error);
                            }
                            catch (Exception e)
                            {
                                _ = _exception.Error(e, Searchkey);
                            }
                            _ = _exception.Error(ex, Searchkey);
                            IsError = true;
                            priceCheckCMDto.IsError = IsError;
                            priceCheckCMDto.Flag = false;
                            return priceCheckCMDto;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                try
                {
                    Error = e.Message + e.StackTrace.ToString() + "#= " + Error;
                    responsetype = "GeneralError";
                    string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, xslrequest, Responsedata_Ex, Error);
                }
                catch (Exception ex)
                {
                    _ = _exception.Error(ex, Searchkey);
                }
                _ = _exception.Error(e, Searchkey);
                IsError = true;
                priceCheckCMDto.IsError = IsError;

            }

            try
            {
                responsetype = "Success";
                string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, xslrequest, Responsedata_Ex, Error);
            }
            catch (Exception e)
            {
                _ = _exception.Error(e, Searchkey);
            }

            priceCheckCMDto.IsError = false;
            priceCheckCMDto.Flag = false;
            priceCheckCMDto.ItemPrice = FinalPrice;
            return priceCheckCMDto;
        }

        public async Task<string> Get_JuniperNew_PriceCheckInRecheck(TokenData tokenData, bool IsPriceChange, string CheckIn, string CheckOut, DataRow[] SupplierDetails, string SupplierName, int j, DataTable dtCheckPriceReqStructure, string UniqueNo, string AgentUniqueNo, double FinalPrice, bool isForBooking, string HotelId, int UserCartId, int UserCartDetailId, string RoomTypeId, double CompanyBuyingPrice, string NewRatePlan, bool IsError, string Currency, string ISBooking, string Searchkey = "", int AgentId = 0) //new added on 03-May-2025
        {
            string responsetype = "Success";
            string time = "";
            string Error = "";
            string xslrequest = "";
            string Responsedata_Ex = "";
            try
            {
                #region PriceCheck RQ
                string UserID = Convert.ToString(SupplierDetails[0]["UserId"]);
                string Password = Convert.ToString(SupplierDetails[0]["Password"]);

                CheckIn = CheckIn.Substring(6, 4) + "-" + CheckIn.Substring(3, 2) + "-" + CheckIn.Substring(0, 2);
                CheckOut = CheckOut.Substring(6, 4) + "-" + CheckOut.Substring(3, 2) + "-" + CheckOut.Substring(0, 2);
                double CurrencyFactor = 1;
                if (Currency.ToLower() != tokenData.SiteCurrency.ToLower())
                {
                    CurrencyFactor = _commonMethod.GetROEByCurrency(Currency, tokenData.dtROE);

                }

                string Nationality = "";
                string RoomTypeIds = Get_LOH_RoomTypeID(RoomTypeId, ref Nationality);

                xslrequest = JuniperNew_PriceCheck(UserID, Password, RoomTypeIds, CheckIn, CheckOut, HotelId, Currency);
                #endregion
                //Common.ThirdPartyBookingHistory(Convert.ToString(UniqueNo), Convert.ToString(AgentUniqueNo), xslrequest, "", "Before get price " + SupplierName + " request hit " + j + "is for Price Check" + isForBooking, SupplierName, "response confirm", "-", "API Out");

                if (xslrequest != "")
                {
                    //Common.ThirdPartyBookingHistory(Convert.ToString(UniqueNo), Convert.ToString(AgentUniqueNo), xslrequest, "", "Before get price " + SupplierName + " request hit " + j + "is for Price Check" + isForBooking, SupplierName, "response confirm", "-", "API Out");
                    if (!isForBooking)
                        dtCheckPriceReqStructure.Rows.Add(_commonMethod.AddCheckPriceReq(dtCheckPriceReqStructure.NewRow(), j + 1, UserCartId, UserCartDetailId, RoomTypeId, SupplierName, xslrequest));

                    #region PriceCheck RS
                    //string URL = Convert.ToString(SupplierDetails[0]["websiteurl"]) + JuniperInfo.CheckAvail;
                    string URL = Convert.ToString(SupplierDetails[0]["websiteurl"]) + _supplierConfig.Juniper.CheckAvail;

                    System.Diagnostics.Stopwatch timerroomprocess = new System.Diagnostics.Stopwatch();
                    timerroomprocess.Start();

                    ResponseDto responseDto = await _commonMethod.GetSupplierResponse("", xslrequest, URL, 60000, SupplierName, null, false, Searchkey);
                    Responsedata_Ex = responseDto.Response;
                    Error = responseDto.Error;

                    timerroomprocess.Stop();
                    time = timerroomprocess.Elapsed.ToString();

                    _commonMethod.ThirdPartyBookingHistory(RoomTypeId, Convert.ToString(AgentUniqueNo), xslrequest, Responsedata_Ex + Error, "After get price " + SupplierName + " request hit " + j + "is for Price Check" + isForBooking, SupplierName, "response confirm", "-", "API Out");

                    if (Responsedata_Ex == string.Empty || Responsedata_Ex.Contains("Error"))
                    {
                        try
                        {
                            responsetype = "GeneralError";
                            string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, xslrequest, Responsedata_Ex, Error);
                        }
                        catch (Exception e)
                        {
                            _ = _exception.Error(e, Searchkey);
                        }
                        IsError = true;
                        return "";
                    }

                    #endregion

                    if (Responsedata_Ex.Trim() != string.Empty)
                    {
                        try
                        {
                            DataSet Response = _commonMethod.GetDataSetFromXMLString(Responsedata_Ex);

                            #region DataRaw
                            if (Response.Tables.Contains("Errors") || Response.Tables.Contains("Error"))
                            {
                                try
                                {
                                    responsetype = "GeneralError";
                                    string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, xslrequest, Responsedata_Ex, Error);
                                }
                                catch (Exception e)
                                {
                                    _ = _exception.Error(e, Searchkey);
                                }
                                return " ";
                            }
                            int Hid = 0;
                            DataTable dtHotelOption = Response.Tables["HotelOption"];
                            DataRow[] drHotelOption = (dtHotelOption.Select("HotelOption_Id='" + Hid + "'"));
                            int HotelOption_Id = Convert.ToInt32(drHotelOption[0]["HotelOption_Id"]);
                            string RoomCode = Convert.ToString(drHotelOption[0]["RatePlanCode"]);
                            string Status = Convert.ToString(drHotelOption[0]["Status"]);
                            #endregion

                            #region Price Update Logic
                            if (Status == "OK")
                            {
                                DataRow[] drPrices = (Response.Tables["Prices"].Select("Prices_Id='" + HotelOption_Id + "'"));
                                int Prices_Id = Convert.ToInt32(drPrices[0]["Prices_Id"]);

                                DataRow[] drPrice = (Response.Tables["Price"].Select("Price_Id='" + Prices_Id + "'"));
                                int Price_Id = Convert.ToInt32(drPrice[0]["Price_Id"]);

                                DataRow[] drTotalFixAmounts = (Response.Tables["TotalFixAmounts"].Select("TotalFixAmounts_Id='" + Price_Id + "'"));
                                int TotalFixAmounts_Id = Convert.ToInt32(drTotalFixAmounts[0]["TotalFixAmounts_Id"]);
                                FinalPrice = Convert.ToDouble(drTotalFixAmounts[0]["Gross"]);

                                //if (SupplierName.ToLower() == SupplierName)
                                FinalPrice = FinalPrice * CurrencyFactor;

                                NewRatePlan = RoomCode;
                                if (IsPriceChange == false)
                                {
                                    //FinalPrice = FinalPrice * 2; //temp to check
                                    if (Convert.ToDouble(FinalPrice).TruncateDecimalPlaces(0) > CompanyBuyingPrice.TruncateDecimalPlaces(0))
                                    {
                                        try
                                        {
                                            Responsedata_Ex = "#Price Changed " + "#Main Price = " + CompanyBuyingPrice + " #New Price = " + FinalPrice + " #Response= " + Responsedata_Ex;
                                            responsetype = "GeneralError";
                                            string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, xslrequest, Responsedata_Ex, Error);
                                        }
                                        catch (Exception e)
                                        {
                                            _ = _exception.Error(e, Searchkey);
                                        }

                                    }
                                }
                            }
                            else
                            {
                                try
                                {
                                    responsetype = "GeneralError";
                                    string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, xslrequest, Responsedata_Ex, Error);
                                }
                                catch (Exception e)
                                {
                                    _ = _exception.Error(e, Searchkey);
                                }
                                IsError = true;
                            }
                            #endregion
                        }
                        catch (Exception ex)
                        {
                            try
                            {
                                responsetype = "GeneralError";
                                Error = ex.Message + ex.StackTrace.ToString();
                                string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, xslrequest, Responsedata_Ex, Error);
                            }
                            catch (Exception e)
                            {
                                _ = _exception.Error(e, Searchkey);
                            }
                            _ = _exception.Error(ex, Searchkey);
                            IsError = true;
                            return "";
                        }
                    }
                }
            }
            catch (Exception e)
            {
                try
                {
                    Error = e.Message + e.StackTrace.ToString() + "#= " + Error;
                    responsetype = "GeneralError";
                    string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, xslrequest, Responsedata_Ex, Error);
                }
                catch (Exception ex)
                {
                    _ = _exception.Error(ex, Searchkey);
                }
                _ = _exception.Error(e, Searchkey);
                IsError = true;
            }

            try
            {
                if (responsetype == "Success")
                {
                    responsetype = "Success";
                    string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, xslrequest, Responsedata_Ex, Error);
                }
            }
            catch (Exception e)
            {
                _ = _exception.Error(e, Searchkey);
            }
            return responsetype;
        }


        public string JuniperNew_PriceCheck(string UserID, string Password, string RoomTypeIds, string CheckIn, string CheckOut, string HotelId, string Currency)
        {
            string xslrequest = "";

            try
            {
                StringBuilder CheckRQ = new StringBuilder();
                CheckRQ.Append("<soapenv:Envelope xmlns:soapenv=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns=\"http://www.juniper.es/webservice/2007/\">");
                CheckRQ.Append("<soapenv:Header/>");
                CheckRQ.Append("<soapenv:Body>");
                CheckRQ.Append("<HotelCheckAvail>");
                CheckRQ.Append("<HotelCheckAvailRQ Version=\"1.1\" Context=\"VALUATION\" Language=\"en\">");
                CheckRQ.Append("<Login Email=\"" + UserID + "\" Password=\"" + Password + "\"/>");
                CheckRQ.Append("<HotelCheckAvailRequest>");
                CheckRQ.Append("<HotelOption  RatePlanCode=\"" + RoomTypeIds + "\"/>");
                CheckRQ.Append("<SearchSegmentsHotels>");
                CheckRQ.Append("<SearchSegmentHotels Start=\"" + CheckIn + "\" End=\"" + CheckOut + "\"/>");
                CheckRQ.Append("<HotelCodes>");
                CheckRQ.Append("<HotelCode>" + HotelId + "</HotelCode>");
                CheckRQ.Append("</HotelCodes>");
                CheckRQ.Append("</SearchSegmentsHotels>");
                CheckRQ.Append("</HotelCheckAvailRequest>");
                CheckRQ.Append("<AdvancedOptions>");
                CheckRQ.Append("<UseCurrency>" + Currency + "</UseCurrency>");
                CheckRQ.Append("</AdvancedOptions>");
                CheckRQ.Append("</HotelCheckAvailRQ>");
                CheckRQ.Append("</HotelCheckAvail>");
                CheckRQ.Append("</soapenv:Body>");
                CheckRQ.Append("</soapenv:Envelope>");
                xslrequest = CheckRQ.ToString();
            }
            catch (Exception ex)
            {
                _ = _exception.Error(ex);
            }

            return xslrequest;
        }

        public  void Get_Juniper_BookingProcess(string AgentCode, ref DataTable dtAgentVoucher, ref DataSet dsCartDetails, ref DataTable dtCheckPriceReqStructure, ref DataTable dtPassengers, ref DataTable dtCAncellationPolicy, HotelSearchParameter SearchPara, DataRow[] SupplierDetails, TokenData tokenData, ref string AgentVoucherNo, ref int bookingStatus, ref string UniqueNo, ref string AgentUniqueNo, ref bool isNonRefundable, int i, string SearchKey, int AgentId, bool IsPruvo)
        {
            string time = "00:00:00.000000";
            string RequestSPR = "-";
            string ReponseSPR = "-";
            string responsetype = "Success";
            string Error = "";
            string SupplierName = Convert.ToString(dsCartDetails.Tables[1].DefaultView[0]["SupplierName"]);
            try
            {
                #region Declaration
                AgentVoucherNo = "";
                bookingStatus = 6;
                int j = 0;
                double FinalPrices = 0;
                string RatePlanCode = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomTypeID"]);

                string HotelId = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["XMLHotelId"]);
                int UserCartId = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[0]["UserCartID"]);
                int UserCartDetailId = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[0]["UserCartDetailId"]);
                double CompanyBuyingPrices = Convert.ToDouble(dsCartDetails.Tables[1].DefaultView[0]["Rate"]);
                string BookingCode = "";
                bool IsPriceUpdate = true;
                double Gross = 0;
                string NewRatePlan = "";
                UniqueNo = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["UserCartDetailId"]);
                bool IsError = false;
                string BookingRequest = "";

                #endregion

                #region PriceCkeck, Booking Rule, Booking RQ, Booking URL
                double CurrencyFactor = 1;
                
                string Currency = Convert.ToString(SupplierDetails[0]["CurrencyShortName"]);
                
                if (SupplierName.ToLower() == "robios")
                {
                    string[] spitid = RatePlanCode.Split(new String[] { "$$$" }, StringSplitOptions.None);
                    Currency = spitid[5].ToString();
                }

                if (Currency.ToLower() != tokenData.SiteCurrency.ToLower())
                {
                    CurrencyFactor = _commonMethod.GetROEByCurrency(Currency, tokenData.dtROE);
                }

                isNonRefundable = false;
                if (dtCAncellationPolicy != null && dtCAncellationPolicy.Rows.Count > 0)
                {
                    dtCAncellationPolicy.DefaultView.RowFilter = "RoomTypeId='" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomTypeID"]) + "' and AgentCancellationPrice='0'";
                    if (dtCAncellationPolicy.DefaultView.Count == 0)
                        isNonRefundable = true;
                }
                string NationalityCode = "";

                NewRatePlan = Get_LOH_RoomTypeID(RatePlanCode, ref NationalityCode);

                BookingCode = Get_Juniper_HotelBookingRulesRQ(isNonRefundable, CurrencyFactor, Currency, dsCartDetails, SearchPara, SupplierDetails, NewRatePlan, HotelId, i, j, SupplierName, UniqueNo, AgentUniqueNo, IsPriceUpdate, ref Gross, IsError);


                BookingRequest = Get_Juniper_BookingRequest(i, dsCartDetails, AgentCode, dtPassengers, SupplierDetails, BookingCode, Gross, UniqueNo, SupplierName, RatePlanCode, Currency,IsPruvo);

                _commonMethod.ThirdPartyBookingHistory(Convert.ToString(UniqueNo), Convert.ToString(AgentUniqueNo), BookingRequest, "-", "Before Request hit booking", Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["SupplierName"]), "Request Confirm", "-", "Within Earth");

                string Url = Convert.ToString(SupplierDetails[0]["WebSiteURL"]) +_supplierConfig.Juniper.Booking;
                string ResponseDatatest = "";
                isNonRefundable = false;
                #endregion
               
                {
                    
                    BookingRequest = BookingRequest.Replace("#BookingCode", BookingCode);

                    System.Diagnostics.Stopwatch timerroomprocess = new System.Diagnostics.Stopwatch();
                    timerroomprocess.Start();

                    ResponseDatatest =  GetSupplierResponse_report(ref Error, "", BookingRequest, Url, 180000, SupplierName, null, false, SearchKey);
                   
                   
                    RequestSPR = BookingRequest;
                    ReponseSPR = ResponseDatatest;

                    timerroomprocess.Stop();
                    time = timerroomprocess.Elapsed.ToString();

                    _commonMethod.ThirdPartyBookingHistory(Convert.ToString(UniqueNo), Convert.ToString(AgentUniqueNo), BookingRequest, ResponseDatatest, "After Request hit booking", Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["SupplierName"]), "Response Confirm", "-", "Within Earth");

                    DataTable dtCheckPriceRe = null; // (DataTable)HttpContext.Current.Session["CheckPriceReq"];
                    if (dtCheckPriceRe != null)
                    {
                        dtCheckPriceRe.DefaultView.RowFilter = "UserCartDetailsID=" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["UserCartDetailID"]);
                        if (dtCheckPriceRe.DefaultView.Count > 0)
                            dtCheckPriceReqStructure.ImportRow(dtCheckPriceRe.DefaultView[0].Row);
                    }
                }

                #region Booking Confirm, On Request, Rejected
                if (ResponseDatatest != "")
                {
                    DataSet W2M_Response = new DataSet();
                    try
                    {
                        W2M_Response = _commonMethod.GetDataSetFromXMLString(ResponseDatatest);
                    }
                    catch (Exception ex)
                    {

                        _ = _exception.ErrorNew(ex, _LogAPI.Book);
                    }
                    if (W2M_Response.Tables["Reservation"] == null)
                    {
                        responsetype = "GeneralError";
                        AgentVoucherNo = "";
                        bookingStatus = 2;
                        if (ResponseDatatest == "")
                            bookingStatus = 1;
                    }
                    else
                    {
                        string Status = Convert.ToString(W2M_Response.Tables["Reservation"].Rows[0]["Status"]).ToLower();
                        switch (Status)
                        {
                            case "cac":
                            case "can":
                                responsetype = "GeneralError";
                                AgentVoucherNo = "";
                                bookingStatus = 2;
                                break;
                            case "pre":
                            case "pdi":
                            case "quo":
                                AgentVoucherNo = Convert.ToString(W2M_Response.Tables["Reservation"].Rows[0]["Locator"]);
                                try
                                {
                                    if (SupplierName.ToLower() == "goldenglobe")
                                    {
                                        if (Convert.ToString(W2M_Response.Tables["ExternalInfo"].Rows[0]["ExternalLocator"]) != null)
                                        {
                                            if (AgentVoucherNo != "")
                                                AgentVoucherNo = AgentVoucherNo + "|" + Convert.ToString(W2M_Response.Tables["ExternalInfo"].Rows[0]["ExternalLocator"]);
                                            else
                                                AgentVoucherNo = Convert.ToString(W2M_Response.Tables["ExternalInfo"].Rows[0]["ExternalLocator"]);
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    _ = _exception.ErrorNew(ex, _LogAPI.Book);
                                }
                                responsetype = "GeneralError";
                                bookingStatus = 1;
                                break;
                            case "pag":
                            case "con":
                                AgentVoucherNo = Convert.ToString(W2M_Response.Tables["Reservation"].Rows[0]["Locator"]);
                                try
                                {
                                    if (SupplierName.ToLower() == "goldenglobe")
                                    {
                                        if (Convert.ToString(W2M_Response.Tables["ExternalInfo"].Rows[0]["ExternalLocator"]) != null)
                                        {
                                            if (AgentVoucherNo != "")
                                                AgentVoucherNo = AgentVoucherNo + "|" + Convert.ToString(W2M_Response.Tables["ExternalInfo"].Rows[0]["ExternalLocator"]);
                                            else
                                                AgentVoucherNo = Convert.ToString(W2M_Response.Tables["ExternalInfo"].Rows[0]["ExternalLocator"]);
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    _ = _exception.ErrorNew(ex, _LogAPI.Book);
                                }
                                bookingStatus = 6;
                                break;

                            default:
                                if (W2M_Response.Tables["Reservation"].Columns.Contains("Locator"))
                                {
                                    AgentVoucherNo = Convert.ToString(W2M_Response.Tables["Reservation"].Rows[0]["Locator"]);
                                }
                                else
                                {
                                    AgentVoucherNo = "";
                                }
                                bookingStatus = 1;
                                break;
                        }
                    }
                }
                else
                {
                    responsetype = "GeneralError";
                    AgentVoucherNo = "";
                    bookingStatus = 2;
                }
                #endregion

                #region SupplierPR Booking
                if (bookingStatus == 6)
                    responsetype = "Success";
                else
                {
                    responsetype = "GeneralError";
                    if(SupplierName.ToLower() == "robios")
                    {
                        if (bookingStatus != 6)
                        {
                            #region APISO VC for release if my booking is non con

                            int usercartdetailsid = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[j]["UserCartDetailId"]);
                            //get bookingid and detailsid
                            DataTable dtbookingdata = _commonMethod.GetApisoBookingData(usercartdetailsid);
                            if (dtbookingdata != null)
                            {
                               _vcn.GetApisoServiceDatatoReloadVCNDebitNonconfirmedbookings("", Convert.ToInt32(dtbookingdata.Rows[0]["OnlineBookingMasterId"]), Convert.ToInt32(dtbookingdata.Rows[0]["OnlineBookingDetailsId"]));
                            }
                            #endregion
                        }
                    }
                }

                try
                {
                    _ = _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "Book", AgentId, time, responsetype, BookingRequest, ResponseDatatest, Error);
                }
                catch (Exception e)
                {
                    _ = _exception.ErrorBooking(e, AgentId.ToString(), "", SearchKey);
                }
                #endregion

                #region Booking DataRaw
                DataRow newRowEgyptExpress = dtAgentVoucher.NewRow();
                newRowEgyptExpress["UserCartId"] = dsCartDetails.Tables[0].Rows[i]["UserCartId"];
                newRowEgyptExpress["UserCartDetailId"] = dsCartDetails.Tables[1].DefaultView[j]["UserCartDetailId"];
                newRowEgyptExpress["AgentVoucherNo"] = AgentVoucherNo;
                newRowEgyptExpress["XMLRequest"] = BookingRequest;
                newRowEgyptExpress["XMLResponse"] = ResponseDatatest;
                newRowEgyptExpress["RoomStatus"] = bookingStatus;
                newRowEgyptExpress["SupplierName"] = dsCartDetails.Tables[1].DefaultView[j]["SupplierName"];
                newRowEgyptExpress["PackageId"] = Convert.ToInt32(dsCartDetails.Tables[0].Rows[i]["PackageId"]);
                dtAgentVoucher.Rows.Add(newRowEgyptExpress);
                #endregion
                
            }
            catch (Exception e)
            {

                try
                {
                    if (SupplierName.ToLower() == "robios")
                    {
                        if (bookingStatus != 6)
                        {
                            #region APISO VC for release if my booking is non con

                            int usercartdetailsid = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[0]["UserCartDetailId"]);
                            
                            DataTable dtbookingdata = _commonMethod.GetApisoBookingData(usercartdetailsid);
                            if (dtbookingdata != null)
                            {
                               _vcn.GetApisoServiceDatatoReloadVCNDebitNonconfirmedbookings("", Convert.ToInt32(dtbookingdata.Rows[0]["OnlineBookingMasterId"]), Convert.ToInt32(dtbookingdata.Rows[0]["OnlineBookingDetailsId"]));
                            }
                            #endregion
                        }
                    }
                        
                }
                catch (Exception x)
                {
                    _ = _exception.ErrorNew(x, _LogAPI.Book);
                }

                _ = _exception.ErrorBooking(e, "", "", SearchKey);
                #region SupplierPerformanceReport Catch
                try
                {
                    Error = e.Message + e.StackTrace.ToString();
                    responsetype = "GeneralError";
                    _ = _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "Book", AgentId, time, responsetype, RequestSPR, ReponseSPR, Error);
                }
                catch (Exception ex)
                {
                    _ = _exception.ErrorBooking(ex, AgentId.ToString(), "", SearchKey);
                }
                #endregion
            }

        }

        public string Get_Juniper_HotelBookingRulesRQ(bool isNonRefundable, double CurrencyFactor, string Currency, DataSet dsCartDetails, HotelSearchParameter Searchpera, DataRow[] SupplierDetails, string RatePlanCode, string HotelId, int i, int j, string SupplierName, string UniqueNo, string AgentUniqueNo, bool IsPriceUpdate, ref double Gross, bool IsError)
        {
            string Error = "";
            try
            {
                {
                    #region BookingRule RQ

                    string UserID = Convert.ToString(SupplierDetails[0]["UserId"]);
                    string Password = Convert.ToString(SupplierDetails[0]["Password"]);
                    int CutOffDays = Convert.ToInt32(SupplierDetails[0]["CutOffDays"]);
                    string CheckIn = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["CheckInDate"]);
                    CheckIn = CheckIn.Substring(0, 4) + "-" + CheckIn.Substring(4, 2) + "-" + CheckIn.Substring(6, 2);
                    string CheckOut = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["CheckOutDate"]);
                    CheckOut = CheckOut.Substring(0, 4) + "-" + CheckOut.Substring(4, 2) + "-" + CheckOut.Substring(6, 2);
                    double FinalPrice = Math.Round(Convert.ToDouble(dsCartDetails.Tables[1].DefaultView[j]["Rate"]), 2);
                    string BookingCode = "";

                    StringBuilder BookingRuleRQ = new StringBuilder();
                    BookingRuleRQ.Append("<soapenv:Envelope xmlns:soapenv=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns=\"http://www.juniper.es/webservice/2007/\">");
                    BookingRuleRQ.Append("<soapenv:Header/>");
                    BookingRuleRQ.Append("<soapenv:Body>");
                    BookingRuleRQ.Append("<HotelBookingRules>");
                    BookingRuleRQ.Append("<HotelBookingRulesRQ Version=\"1.1\" Context=\"VALUATION\" Language=\"en\">");
                    BookingRuleRQ.Append("<Login Email=\"" + UserID + "\" Password=\"" + Password + "\"/>");
                    BookingRuleRQ.Append("<HotelBookingRulesRequest>");
                    BookingRuleRQ.Append("<HotelOption RatePlanCode=\"" + RatePlanCode + "\"/>");
                    BookingRuleRQ.Append("<SearchSegmentsHotels>");
                    BookingRuleRQ.Append("<SearchSegmentHotels Start=\"" + CheckIn + "\" End=\"" + CheckOut + "\"/>");
                    BookingRuleRQ.Append("<HotelCodes>");
                    BookingRuleRQ.Append("<HotelCode>" + HotelId + "</HotelCode>");
                    BookingRuleRQ.Append("</HotelCodes>");
                    BookingRuleRQ.Append("</SearchSegmentsHotels>");
                    BookingRuleRQ.Append("</HotelBookingRulesRequest>");
                    BookingRuleRQ.Append("<AdvancedOptions>");
                    BookingRuleRQ.Append("<UseCurrency>" + Currency + "</UseCurrency>");
                    BookingRuleRQ.Append("</AdvancedOptions>");
                    BookingRuleRQ.Append("</HotelBookingRulesRQ>");
                    BookingRuleRQ.Append("</HotelBookingRules>");
                    BookingRuleRQ.Append("</soapenv:Body>");
                    BookingRuleRQ.Append("</soapenv:Envelope>");
                    string xslrequest = BookingRuleRQ.ToString();
                    #endregion

                    _commonMethod.ThirdPartyBookingHistory(Convert.ToString(UniqueNo), Convert.ToString(AgentUniqueNo), xslrequest, "-", "Before Request hit BookingRule", Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["SupplierName"]), "Request Confirm", "-", "Within Earth");

                    if (xslrequest != "")
                    {
                        #region BookingRule RS
                        //string BookRuleURL = Convert.ToString(SupplierDetails[0]["WebSiteURL"]) + JuniperInfo.CheckAvail;
                        string BookRuleURL = Convert.ToString(SupplierDetails[0]["WebSiteURL"]) + _supplierConfig.Juniper.CheckAvail;
                        string HotelBookingRuleRS = GetSupplierResponse_report( ref Error, "", xslrequest, BookRuleURL, 120000, SupplierName);


                        _commonMethod.ThirdPartyBookingHistory(Convert.ToString(UniqueNo), Convert.ToString(AgentUniqueNo), xslrequest, HotelBookingRuleRS, "After Request hit BookingRule", Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["SupplierName"]), "Response Confirm", "-", "Within Earth");

                        if (HotelBookingRuleRS == string.Empty || HotelBookingRuleRS.Contains("Error"))
                            return "";

                        if (SupplierName.ToLower() == "robios")
                        {
                            HotelBookingRuleRS = HotelBookingRuleRS.Replace("<Address><Address>", "<Address>");
                            HotelBookingRuleRS = HotelBookingRuleRS.Replace("</Longitude></Address>", "</Longitude>");
                            HotelBookingRuleRS = HotelBookingRuleRS.Replace("<Address>", "<Address1>");
                            HotelBookingRuleRS = HotelBookingRuleRS.Replace("</Address>", "</Address1>");

                        }
                        else
                        {
                            //HotelBookingRuleRS = HotelBookingRuleRS.Replace("<Address>", "").Replace("</Address>", "");
                            if (HotelBookingRuleRS.IndexOf("<Address") > 0)
                            {
                                String Data2 = HotelBookingRuleRS.Substring(HotelBookingRuleRS.IndexOf("<Address"), HotelBookingRuleRS.IndexOf("</Address>") - HotelBookingRuleRS.IndexOf("<Address")) + "</Address>";
                                HotelBookingRuleRS = HotelBookingRuleRS.Replace(Data2, "");
                            }
                        }

                        if (HotelBookingRuleRS.IndexOf("<PhoneNumbers") > 0)
                        {
                            String Data3 = HotelBookingRuleRS.Substring(HotelBookingRuleRS.IndexOf("<PhoneNumbers"), HotelBookingRuleRS.IndexOf("</PhoneNumbers>") - HotelBookingRuleRS.IndexOf("<PhoneNumbers")) + "</PhoneNumbers>";
                            HotelBookingRuleRS = HotelBookingRuleRS.Replace(Data3, "");
                        }
                        if (HotelBookingRuleRS.IndexOf("<HotelContent") > 0)
                        {
                            String Data4 = HotelBookingRuleRS.Substring(HotelBookingRuleRS.IndexOf("<HotelContent"), HotelBookingRuleRS.IndexOf("</HotelContent>") - HotelBookingRuleRS.IndexOf("<HotelContent")) + "</HotelContent>";
                            HotelBookingRuleRS = HotelBookingRuleRS.Replace(Data4, "");
                        }


                        DataSet BookingRuleResponse = _commonMethod.GetDataSetFromXMLString(HotelBookingRuleRS);
                        #endregion

                        #region DataRaw
                        DataTable dtHotelOption = BookingRuleResponse.Tables["HotelOption"];
                        DataRow[] drHotelOption = (dtHotelOption.Select("HotelOption_Id='" + j + "'"));
                        int HotelOption_Id = Convert.ToInt32(drHotelOption[0]["HotelOption_Id"]);

                        DataRow[] drBookingCode = (BookingRuleResponse.Tables["BookingCode"].Select("HotelOption_Id='" + HotelOption_Id + "'"));
                        BookingCode = Convert.ToString(drBookingCode[0]["BookingCode_Text"]);

                        DataRow[] drPriceInformation = (BookingRuleResponse.Tables["PriceInformation"].Select("PriceInformation_Id='" + HotelOption_Id + "'"));
                        int PriceInformation_Id = Convert.ToInt32(drPriceInformation[0]["PriceInformation_Id"]);

                        DataRow[] drPrices = (BookingRuleResponse.Tables["Prices"].Select("Prices_Id='" + PriceInformation_Id + "'"));
                        int Prices_Id = Convert.ToInt32(drPrices[0]["Prices_Id"]);

                        DataRow[] drPrice = (BookingRuleResponse.Tables["Price"].Select("Price_Id='" + Prices_Id + "'"));
                        int Price_Id = Convert.ToInt32(drPrice[0]["Price_Id"]);

                        DataRow[] drTotalFixAmounts = (BookingRuleResponse.Tables["TotalFixAmounts"].Select("TotalFixAmounts_Id='" + Price_Id + "'"));
                        int TotalFixAmounts_Id = Convert.ToInt32(drTotalFixAmounts[0]["TotalFixAmounts_Id"]);
                        Gross = Math.Round(Convert.ToDouble(drTotalFixAmounts[0]["Gross"]), 2);
                        double GrossCheck = Gross * CurrencyFactor;
                        GrossCheck = Math.Round(GrossCheck, 2);

                        #endregion

                        #region BookingCode 
                        if (BookingCode != "")
                        {
                            if (Math.Round(GrossCheck.TruncateDecimalPlaces(0)) > Math.Round(FinalPrice.TruncateDecimalPlaces(0)))
                                BookingCode = "";
                            else
                            {
                                try
                                {
                                    if (BookingRuleResponse.Tables.Contains("CancellationPolicy"))
                                    {
                                        DataRow[] drCancellationPolicy = (BookingRuleResponse.Tables["CancellationPolicy"].Select("CancellationPolicy_Id='" + HotelOption_Id + "'"));
                                        int CancellationPolicy_Id = Convert.ToInt32(drCancellationPolicy[0]["CancellationPolicy_Id"]);
                                        DataRow[] drFirstDayCostCancellation = (BookingRuleResponse.Tables["FirstDayCostCancellation"].Select("CancellationPolicy_Id='" + CancellationPolicy_Id + "'"));
                                        string FirstDayCostCancellation_Text = Convert.ToString(drFirstDayCostCancellation[0]["FirstDayCostCancellation_Text"]);
                                        double CancelCharge = Gross;
                                        DateTime CancelDate = DateTime.Now.Date;
                                        CancelDate = Convert.ToDateTime(FirstDayCostCancellation_Text).Date;
                                        DateTime NowDate = DateTime.Now.Date;
                                        if (CancelDate <= DateTime.Now.Date)
                                            CancelDate = DateTime.Now.Date;
                                        else
                                            CancelDate = CancelDate.AddDays(CutOffDays).Date;
                                        if (CancelDate.Date < DateTime.Now.Date)
                                            CancelDate = DateTime.Now.Date;
                                        bool IsZeroCharge = false;
                                        if (NowDate < CancelDate && CancelDate.Date > DateTime.Now.Date)
                                        {
                                            IsZeroCharge = true;
                                            //0 charge
                                        }
                                        else
                                        {
                                            if (CancelDate < DateTime.Now.Date)
                                                CancelDate = DateTime.Now.Date;
                                        }
                                        if (!isNonRefundable && !IsZeroCharge)
                                            BookingCode = "";
                                    }
                                    else
                                    {
                                        if (!isNonRefundable)
                                            BookingCode = "";
                                    }
                                    return BookingCode;
                                }
                                catch (Exception ex)
                                {
                                    BookingCode = "";
                                    _ = _exception.ErrorNew(ex, _LogAPI.Book);
                                }
                            }
                        }
                        else
                            return "";
                        #endregion

                    }
                    return BookingCode;
                }
            }
            catch (Exception e)
            {
                _ = _exception.ErrorNew(e, _LogAPI.Book);
                return "";
            }
        }
        public string Get_Juniper_HotelBookingRulesRQ_newversion(bool isNonRefundable, double CurrencyFactor, string Currency, BookRequest_newversion bookRequest, DataRow[] SupplierDetails, string RatePlanCode, string HotelId, int i, int j, string SupplierName, string UniqueNo, string AgentUniqueNo, bool IsPriceUpdate, ref double Gross, ref string Paxdetails, bool IsError)
        {
            string Error = "";
            try
            {
                {
                    #region BookingRule RQ

                    string UserID = Convert.ToString(SupplierDetails[0]["UserId"]);
                    string Password = Convert.ToString(SupplierDetails[0]["Password"]);
                    int CutOffDays = Convert.ToInt32(SupplierDetails[0]["CutOffDays"]);
                    string CheckIn = Convert.ToString(bookRequest.userCart.UserCartDetails[j].CheckInDate );
                    string CheckOut = Convert.ToString(bookRequest.userCart.UserCartDetails[j].CheckOutDate);
                  
                    DateTime checkInDate = DateTime.Parse(CheckIn);
                    DateTime checkOutDate = DateTime.Parse(CheckOut);

                    // Convert to "yyyy-MM-dd" format
                     CheckIn = checkInDate.ToString("yyyy-MM-dd");
                     CheckOut = checkOutDate.ToString("yyyy-MM-dd");
                    double FinalPrice = Math.Round(Convert.ToDouble(bookRequest.userCart.UserCartDetails[j].BaseServiceBuyingAmount), 2);
                    string BookingCode = "";

                    StringBuilder BookingRuleRQ = new StringBuilder();
                    BookingRuleRQ.Append("<soapenv:Envelope xmlns:soapenv=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns=\"http://www.juniper.es/webservice/2007/\">");
                    BookingRuleRQ.Append("<soapenv:Header/>");
                    BookingRuleRQ.Append("<soapenv:Body>");
                    BookingRuleRQ.Append("<HotelBookingRules>");
                    BookingRuleRQ.Append("<HotelBookingRulesRQ Version=\"1.1\" Context=\"VALUATION\" Language=\"en\">");
                    BookingRuleRQ.Append("<Login Email=\"" + UserID + "\" Password=\"" + Password + "\"/>");
                    BookingRuleRQ.Append("<HotelBookingRulesRequest>");
                    BookingRuleRQ.Append("<HotelOption RatePlanCode=\"" + RatePlanCode + "\"/>");
                    BookingRuleRQ.Append("<SearchSegmentsHotels>");
                    BookingRuleRQ.Append("<SearchSegmentHotels Start=\"" + CheckIn + "\" End=\"" + CheckOut + "\"/>");
                    BookingRuleRQ.Append("<HotelCodes>");
                    BookingRuleRQ.Append("<HotelCode>" + HotelId + "</HotelCode>");
                    BookingRuleRQ.Append("</HotelCodes>");
                    BookingRuleRQ.Append("</SearchSegmentsHotels>");
                    BookingRuleRQ.Append("</HotelBookingRulesRequest>");
                    BookingRuleRQ.Append("<AdvancedOptions>");
                    BookingRuleRQ.Append("<UseCurrency>" + Currency + "</UseCurrency>");
                    BookingRuleRQ.Append("</AdvancedOptions>");
                    BookingRuleRQ.Append("</HotelBookingRulesRQ>");
                    BookingRuleRQ.Append("</HotelBookingRules>");
                    BookingRuleRQ.Append("</soapenv:Body>");
                    BookingRuleRQ.Append("</soapenv:Envelope>");
                    string xslrequest = BookingRuleRQ.ToString();
                    #endregion

                    _commonMethod.ThirdPartyBookingHistory(Convert.ToString(UniqueNo), Convert.ToString(AgentUniqueNo), xslrequest, "-", "Before Request hit BookingRule", Convert.ToString(bookRequest.userCart.UserCartDetails[j].SupplierName), "Request Confirm", "-", "Within Earth");

                    if (xslrequest != "")
                    {
                        #region BookingRule RS
                        //string BookRuleURL = Convert.ToString(SupplierDetails[0]["WebSiteURL"]) + JuniperInfo.CheckAvail;
                        string BookRuleURL = Convert.ToString(SupplierDetails[0]["WebSiteURL"]) + _supplierConfig.Juniper.CheckAvail;
                        string HotelBookingRuleRS = GetSupplierResponse_report(ref Error, "", xslrequest, BookRuleURL, 120000, SupplierName);


                        _commonMethod.ThirdPartyBookingHistory(Convert.ToString(UniqueNo), Convert.ToString(AgentUniqueNo), xslrequest, HotelBookingRuleRS, "After Request hit BookingRule", Convert.ToString(bookRequest.userCart.UserCartDetails[j].SupplierName), "Response Confirm", "-", "");

                        if (HotelBookingRuleRS == string.Empty || HotelBookingRuleRS.Contains("Error"))
                            return "";

                        if (SupplierName.ToLower() == "robios")
                        {
                            HotelBookingRuleRS = HotelBookingRuleRS.Replace("<Address><Address>", "<Address>");
                            HotelBookingRuleRS = HotelBookingRuleRS.Replace("</Longitude></Address>", "</Longitude>");
                            HotelBookingRuleRS = HotelBookingRuleRS.Replace("<Address>", "<Address1>");
                            HotelBookingRuleRS = HotelBookingRuleRS.Replace("</Address>", "</Address1>");

                        }
                        else
                        {
                            //HotelBookingRuleRS = HotelBookingRuleRS.Replace("<Address>", "").Replace("</Address>", "");
                            if (HotelBookingRuleRS.IndexOf("<Address") > 0)
                            {
                                String Data2 = HotelBookingRuleRS.Substring(HotelBookingRuleRS.IndexOf("<Address"), HotelBookingRuleRS.IndexOf("</Address>") - HotelBookingRuleRS.IndexOf("<Address")) + "</Address>";
                                HotelBookingRuleRS = HotelBookingRuleRS.Replace(Data2, "");
                            }
                        }

                        //if (HotelBookingRuleRS.IndexOf("<PhoneNumbers") > 0)
                        //{
                        //    String Data3 = HotelBookingRuleRS.Substring(HotelBookingRuleRS.IndexOf("<PhoneNumbers"), HotelBookingRuleRS.IndexOf("</PhoneNumbers>") - HotelBookingRuleRS.IndexOf("<PhoneNumbers")) + "</PhoneNumbers>";
                        //    HotelBookingRuleRS = HotelBookingRuleRS.Replace(Data3, "");
                        //}
                        if (HotelBookingRuleRS.IndexOf("<HotelContent") > 0)
                        {
                            String Data4 = HotelBookingRuleRS.Substring(HotelBookingRuleRS.IndexOf("<HotelContent"), HotelBookingRuleRS.IndexOf("</HotelContent>") - HotelBookingRuleRS.IndexOf("<HotelContent")) + "</HotelContent>";
                            HotelBookingRuleRS = HotelBookingRuleRS.Replace(Data4, "");
                        }

                        DataSet BookingRuleResponse = _commonMethod.GetDataSetFromXMLString(HotelBookingRuleRS);
                        #endregion
                       
                        #region DataRaw
                        DataTable dtHotelOption = BookingRuleResponse.Tables["HotelOption"];
                        DataRow[] drHotelOption = (dtHotelOption.Select("HotelOption_Id='" + j + "'"));
                        int HotelOption_Id = Convert.ToInt32(drHotelOption[0]["HotelOption_Id"]);

                        DataRow[] drBookingCode = (BookingRuleResponse.Tables["BookingCode"].Select("HotelOption_Id='" + HotelOption_Id + "'"));
                        BookingCode = Convert.ToString(drBookingCode[0]["BookingCode_Text"]);

                        DataRow[] drPriceInformation = (BookingRuleResponse.Tables["PriceInformation"].Select("PriceInformation_Id='" + HotelOption_Id + "'"));
                        int PriceInformation_Id = Convert.ToInt32(drPriceInformation[0]["PriceInformation_Id"]);

                        DataRow[] drPrices = (BookingRuleResponse.Tables["Prices"].Select("Prices_Id='" + PriceInformation_Id + "'"));
                        int Prices_Id = Convert.ToInt32(drPrices[0]["Prices_Id"]);

                        DataRow[] drPrice = (BookingRuleResponse.Tables["Price"].Select("Price_Id='" + Prices_Id + "'"));
                        int Price_Id = Convert.ToInt32(drPrice[0]["Price_Id"]);

                        DataRow[] drTotalFixAmounts = (BookingRuleResponse.Tables["TotalFixAmounts"].Select("TotalFixAmounts_Id='" + Price_Id + "'"));
                        int TotalFixAmounts_Id = Convert.ToInt32(drTotalFixAmounts[0]["TotalFixAmounts_Id"]);
                        Gross = Math.Round(Convert.ToDouble(drTotalFixAmounts[0]["Gross"]), 2);
                        double GrossCheck = Gross * CurrencyFactor;
                        GrossCheck = Math.Round(GrossCheck, 2);

                        //added on 21-06-2024 #44183
                        Paxdetails = "";
                        DataRow[] drPaxdts = (BookingRuleResponse.Tables["Pax"].Select("Paxes_Id='" + HotelOption_Id + "'"));
                        for (int kj = 0; kj < drPaxdts.Length; kj++)
                        {
                            int Pax_id = Convert.ToInt16(drPaxdts[kj]["IdPax"]);
                            string Name = Convert.ToString(drPaxdts[kj]["Name"]);
                            bool phonenumber = true;
                            if (BookingRuleResponse.Tables.Contains("PhoneNumbers"))
                            {
                                DataRow[] drPaxPhndetails = (BookingRuleResponse.Tables["PhoneNumbers"].Select("Pax_Id='" + Pax_id + "'"));

                                if (drPaxPhndetails.Length > 0)
                                {
                                    if (Convert.ToString(drPaxPhndetails[0]["PhoneNumber"]) != "")
                                    {
                                        if (kj == 0)
                                        {

                                            Paxdetails = Pax_id + "$$" + Name + "$$" + phonenumber;
                                        }
                                        else
                                        {
                                            Paxdetails = Paxdetails + "$$$" + Pax_id + "$$" + Name + "$$" + phonenumber;
                                        }
                                    }
                                }
                            }

                        }
                        //end 
                        #endregion

                        #region BookingCode 
                        if (BookingCode != "")
                        {
                            if (Math.Round(GrossCheck.TruncateDecimalPlaces(0)) > Math.Round(FinalPrice.TruncateDecimalPlaces(0)))
                                BookingCode = "";
                            else
                            {
                                try
                                {
                                    if (BookingRuleResponse.Tables.Contains("CancellationPolicy"))
                                    {
                                        DataRow[] drCancellationPolicy = (BookingRuleResponse.Tables["CancellationPolicy"].Select("CancellationPolicy_Id='" + HotelOption_Id + "'"));
                                        int CancellationPolicy_Id = Convert.ToInt32(drCancellationPolicy[0]["CancellationPolicy_Id"]);
                                        DataRow[] drFirstDayCostCancellation = (BookingRuleResponse.Tables["FirstDayCostCancellation"].Select("CancellationPolicy_Id='" + CancellationPolicy_Id + "'"));
                                        string FirstDayCostCancellation_Text = Convert.ToString(drFirstDayCostCancellation[0]["FirstDayCostCancellation_Text"]);
                                        double CancelCharge = Gross;
                                        DateTime CancelDate = DateTime.Now.Date;
                                        CancelDate = Convert.ToDateTime(FirstDayCostCancellation_Text).Date;
                                        DateTime NowDate = DateTime.Now.Date;
                                        if (CancelDate <= DateTime.Now.Date)
                                            CancelDate = DateTime.Now.Date;
                                        else
                                            CancelDate = CancelDate.AddDays(CutOffDays).Date;
                                        if (CancelDate.Date < DateTime.Now.Date)
                                            CancelDate = DateTime.Now.Date;
                                        bool IsZeroCharge = false;
                                        if (NowDate < CancelDate && CancelDate.Date > DateTime.Now.Date)
                                        {
                                            IsZeroCharge = true;
                                            //0 charge
                                        }
                                        else
                                        {
                                            if (CancelDate < DateTime.Now.Date)
                                                CancelDate = DateTime.Now.Date;
                                        }
                                        if (!isNonRefundable && !IsZeroCharge)
                                            BookingCode = "";
                                    }
                                    else
                                    {
                                        if (!isNonRefundable)
                                            BookingCode = "";
                                    }
                                    return BookingCode;
                                }
                                catch (Exception ex)
                                {
                                    BookingCode = "";
                                    _ = _exception.ErrorNew(ex, _LogAPI.Book);
                                }
                            }
                        }
                        else
                            return "";
                        #endregion

                    }
                    return BookingCode;
                }
            }
            catch (Exception e)
            {
                _ = _exception.ErrorNew(e, _LogAPI.Book);
                return "";
            }
        }

        public string Get_Juniper_BookingRequest(int i, DataSet dsCartDetails, string AgentCode, DataTable dtPassengers, DataRow[] SupplierDetails, string BookingCode, double Gross, string UniqueNo, string Provider, string RatePlanCode, string Currency, bool IsPruvo)
        {
            try
            {
                #region Booking RQ
                string UserID = Convert.ToString(SupplierDetails[0]["UserId"]);
                string Password = Convert.ToString(SupplierDetails[0]["Password"]);
                string CheckIn = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["CheckInDate"]);
                CheckIn = CheckIn.Substring(0, 4) + "-" + CheckIn.Substring(4, 2) + "-" + CheckIn.Substring(6, 2);
                string CheckOut = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["CheckOutDate"]);
                CheckOut = CheckOut.Substring(0, 4) + "-" + CheckOut.Substring(4, 2) + "-" + CheckOut.Substring(6, 2);
                string HotelCode = Convert.ToString(dsCartDetails.Tables[0].DefaultView[i]["XMLHotelId"]);
                double Max = Gross;
                double Min = Gross;
                string NationalityCode = "";// Convert.ToString(Common.GetThirdPartyStaticData_Xml("Nationality", "Nationality", Convert.ToString(SearchPara.Nationality), "XmlId", "Gta"));
                string ExternalReference = "API_" + Convert.ToString(AgentCode + "_" + UniqueNo);
                string CountryName = "";

                if (Provider.ToLower() == "bookohotel")
                {
                    if (RatePlanCode != "")
                        CountryName = Get_BookoHotel_RoomTypeID(RatePlanCode, ref CountryName);
                }// added on 29-May_2025

                RatePlanCode = Get_LOH_RoomTypeID(RatePlanCode, ref NationalityCode);

                StringBuilder objGuestRequest = new StringBuilder();
                int cnt = 0;
                int LPX = 0;
                dtPassengers.DefaultView.RowFilter = "PickUp='" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[0]["RoomTypeId"]) + "' and UserCartID='" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[0]["UserCartID"]) + "' and ServiceType='Hotel'";
                //dtPassengers.DefaultView.RowFilter = "PickUp='" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[0]["RoomTypeId"]) + "' and UserCartID=" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[0]["UserCartID"]);
                dtPassengers.DefaultView.Sort = "IsLead desc";
                if (dtPassengers.DefaultView.Count > 0)
                {
                    DataTable dtPax = dtPassengers.DefaultView.ToTable();
                    for (int k = 0; k < dtPax.Rows.Count; k++)
                    {
                        string FName = Convert.ToString(dtPax.Rows[k]["FirstName"]);
                        string LName = Convert.ToString(dtPax.Rows[k]["LastName"]);
                        int ChildAge = Convert.ToInt32(dtPax.Rows[k]["ChildAge"]);
                        int AdultAge = 30;
                        //=============== New Added on 1-dec-2023
                        string address = "Megan Avenue 2 Jalan Yap Kwan Seng";
                        string city = "Kuala Lumpur";
                        string postalcode = "50450";
                        string phonenumber = "60321663969";
                        //==============

                        DataRow[] drPassengers = dtPax.Select("IsLead='1'");
                        if (drPassengers.Length == 0)
                        {
                            if (Convert.ToString(dtPax.Rows[k]["PaxType"]).Trim().ToLower() == "adult")
                                dtPax.Rows[k]["IsLead"] = 1;
                            dtPax.AcceptChanges();

                        }
                        if (Convert.ToInt16(dtPax.Rows[k]["IsLead"]) == 1)
                        {
                            cnt = cnt + 1;
                            objGuestRequest.Append("<Pax IdPax=\"" + cnt + "\">");
                            objGuestRequest.Append("<Age>" + AdultAge + "</Age>");
                            objGuestRequest.Append("<Name>" + FName + "</Name>");
                            objGuestRequest.Append("<Surname>" + LName + "</Surname>");
                            objGuestRequest.Append("<Nationality>" + NationalityCode + "</Nationality>");
                            if (Provider.ToLower() == "tds" || Provider.ToLower() == "s2bglobal" || Provider.ToLower() == "robios")//added on 29-May-2025
                            {
                                objGuestRequest.Append("<PhoneNumbers>");
                                //objGuestRequest.Append("<PhoneNumber>+60321663969</PhoneNumber>");
                                objGuestRequest.Append("<PhoneNumber>" + phonenumber + "</PhoneNumber>");
                                objGuestRequest.Append("</PhoneNumbers>");
                            }
                            if (Provider.ToLower() == "robios")
                            {
                                objGuestRequest.Append("<Address>" + address + "</Address>");
                                objGuestRequest.Append("<City>" + city + "</City>");
                                objGuestRequest.Append("<PostalCode>" + postalcode + "</PostalCode>");
                            }
                            if (Provider.ToLower() == "bookohotel")//added on 29-May-2025
                            {
                                if (CountryName != "")
                                    objGuestRequest.Append("<Country>" + CountryName + "</Country>");
                            }
                            objGuestRequest.Append("</Pax>");
                            LPX = cnt;
                        }
                        else
                        {
                            if (Convert.ToString(dtPax.Rows[k]["PaxType"]).Trim().ToLower() == "adult")
                            {
                                cnt = cnt + 1;
                                objGuestRequest.Append("<Pax IdPax=\"" + cnt + "\">");
                                objGuestRequest.Append("<Age>" + AdultAge + "</Age>");
                                objGuestRequest.Append("<Name>" + FName + "</Name>");
                                objGuestRequest.Append("<Surname>" + LName + "</Surname>");
                                objGuestRequest.Append("<Nationality>" + NationalityCode + "</Nationality>");
                                if (Provider.ToLower() == "bookohotel")
                                {
                                    if (CountryName != "")
                                        objGuestRequest.Append("<Country>" + CountryName + "</Country>");
                                }//added on 29-May-2025
                                objGuestRequest.Append("</Pax>");
                            }
                            else
                            {
                                cnt = cnt + 1;
                                objGuestRequest.Append("<Pax IdPax=\"" + cnt + "\">");
                                objGuestRequest.Append("<Age>" + ChildAge + "</Age>");
                                objGuestRequest.Append("<Name>" + FName + "</Name>");
                                objGuestRequest.Append("<Surname>" + LName + "</Surname>");
                                objGuestRequest.Append("<Nationality>" + NationalityCode + "</Nationality>");
                                if (Provider.ToLower() == "bookohotel")
                                {
                                    if (CountryName != "")
                                        objGuestRequest.Append("<Country>" + CountryName + "</Country>");
                                }//added on 29-May-2025
                                objGuestRequest.Append("</Pax>");
                            }
                        }
                    }
                }
                StringBuilder BookRQ = new StringBuilder();
                BookRQ.Append("<soapenv:Envelope xmlns:soapenv=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns=\"http://www.juniper.es/webservice/2007/\">");
                BookRQ.Append("<soapenv:Header/>");
                BookRQ.Append("<soapenv:Body>");
                BookRQ.Append("<HotelBooking>");
                BookRQ.Append("<HotelBookingRQ Version=\"1.1\" Context=\"BOOKING\" Language=\"en\">");
                BookRQ.Append("<Login Email=\"" + UserID + "\" Password=\"" + Password + "\"/>");
                BookRQ.Append("<Paxes>");
                BookRQ.Append(objGuestRequest.ToString());
                BookRQ.Append("</Paxes>");
                BookRQ.Append("<Holder>");
                BookRQ.Append("<RelPax IdPax=\"" + LPX + "\"/>");
                BookRQ.Append("</Holder>");
                BookRQ.Append("<ExternalBookingReference>" + ExternalReference + "</ExternalBookingReference>");
                BookRQ.Append("<Elements>");
                BookRQ.Append("<HotelElement>");
                BookRQ.Append("<BookingCode>" + "#BookingCode" + "</BookingCode>");
                BookRQ.Append("<RelPaxesDist>");
                int R = 0;
                for (int x = 0; x < dsCartDetails.Tables[1].DefaultView.Count; x++)
                {
                    BookRQ.Append("<RelPaxDist>");
                    BookRQ.Append("<RelPaxes>");
                    for (int y = 0; y < Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[x]["NoOfAdults"]); y++)
                    {
                        R = R + 1;
                        BookRQ.Append("<RelPax IdPax=\"" + R + "\"/>");
                    }
                    if (Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[x]["NoOfChild"]) > 0)
                    {
                        for (int z = 0; z < Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[x]["NoOfChild"]); z++)
                        {
                            R = R + 1;
                            BookRQ.Append("<RelPax IdPax=\"" + R + "\"/>");
                        }
                    }
                    BookRQ.Append("</RelPaxes>");
                    BookRQ.Append("</RelPaxDist>");
                }
                BookRQ.Append("</RelPaxesDist>");
                BookRQ.Append("<HotelBookingInfo Start=\"" + CheckIn + "\" End=\"" + CheckOut + "\">");
                BookRQ.Append("<Price>");
                BookRQ.Append("<PriceRange Minimum=\"" + 0 + "\" Maximum=\"" + Max + "\" Currency=\"" + Currency + "\"/>");
                BookRQ.Append("</Price>");
                BookRQ.Append("<Status>OK</Status>");
                if (Provider.ToLower() != "robios")
                {
                    //ticket no 39833
                    string Packag = "Package";
                    BookRQ.Append("<PackageContracts>" + Packag + "</PackageContracts>");
                }
                BookRQ.Append("<HotelCode>" + HotelCode + "</HotelCode>");
                BookRQ.Append("</HotelBookingInfo>");

                if (Provider.ToLower() == "robios")
                {
                    #region APISO VC
                    int usercartdetailsid = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[0]["UserCartDetailId"]);
                    string Pan = "";
                    string CVV = "0", Expiry = "0";
                    //GetApisoDetail(usercartdetailsid, "", ref Pan, ref CVV, ref Expiry, IsPruvo);

                    DataTable dtbookingdata = _commonMethod.GetApisoBookingData(usercartdetailsid);

                    if (dtbookingdata != null)
                    {
                        _vcn.GetApisoServiceDatatoCreateVCN("", Convert.ToInt32(dtbookingdata.Rows[0]["OnlineBookingMasterId"]), Convert.ToInt32(dtbookingdata.Rows[0]["OnlineBookingDetailsId"]), ref Pan, ref CVV, ref Expiry, IsPruvo);
                        //PANN = Pan;
                    }


                    #endregion

                    string ExpiryRegex = Convert.ToString(Expiry).Substring(2, 2);
                    string ExpiryRegexFisrt = Convert.ToString(Expiry).Substring(0, 2);
                    Expiry = ExpiryRegex + "/" + ExpiryRegexFisrt;

                    //BookRQ.Append("<CreditCard=\"" + "" + "\" CardCode=\"" + "VI" + "\" CvC=\"" + "737" + "\" CardNumber=\"" + 123 + "\" ExpireDate=\"" + "03/30" + "\"/>");
                    BookRQ.Append("<CreditCard  CardCode=\"" + "MC" + "\" CvC=\"" + CVV + "\" CardNumber=\"" + Pan + "\" ExpireDate=\"" + Expiry + "\">");
                    BookRQ.Append("<Name>" + "WITHINEARTH" + "</Name>");
                    BookRQ.Append("<Surname>" + "WITHINEARTH" + "</Surname>");
                    BookRQ.Append("</CreditCard>");
                }
                BookRQ.Append("</HotelElement>");
                BookRQ.Append("</Elements>");
                BookRQ.Append("<AdvancedOptions>");
                BookRQ.Append("<UseCurrency>" + Currency + "</UseCurrency>");
                BookRQ.Append("</AdvancedOptions>");
                BookRQ.Append("</HotelBookingRQ>");
                BookRQ.Append("</HotelBooking>");
                BookRQ.Append("</soapenv:Body>");
                BookRQ.Append("</soapenv:Envelope>");
                string BookingRQ = BookRQ.ToString();

                return BookingRQ.ToString();
                #endregion
            }
            catch (Exception ex)
            {
                _ = _exception.ErrorNew(ex, _LogAPI.Book);
                return "";
            }
        }
        public string Get_Juniper_BookingRequest_newversion(int i, BookRequest_newversion bookRequest, string AgentCode, DataTable dtPassengers, DataRow[] SupplierDetails, string BookingCode, double Gross, string UniqueNo, string Provider, string RatePlanCode, string Currency, string Paxdetails, bool IsPruvo)
        {
            try
            {
                #region Booking RQ
                string UserID = Convert.ToString(SupplierDetails[0]["UserId"]);
                string Password = Convert.ToString(SupplierDetails[0]["Password"]);

                string CheckIn = Convert.ToString(bookRequest.userCart.UserCartDetails[i].CheckInDate);
                string CheckOut = Convert.ToString(bookRequest.userCart.UserCartDetails[i].CheckOutDate);
               
                DateTime checkInDate = DateTime.Parse(CheckIn);
                DateTime checkOutDate = DateTime.Parse(CheckOut);

                // Convert to "yyyy-MM-dd" format
                CheckIn = checkInDate.ToString("yyyy-MM-dd");
                CheckOut = checkOutDate.ToString("yyyy-MM-dd");

                string[] spitid = RatePlanCode.Split(new String[] { "$$$" }, StringSplitOptions.None);
                string HotelCode = spitid[3].ToString();
                string NationalityCode = spitid[2].ToString();
             
                double Max = Gross;
                double Min = Gross;
                string ExternalReference = "API_" + Convert.ToString(AgentCode + "_" + UniqueNo);
                string CountryName = "";

                if (Provider.ToLower() == "bookohotel")
                {
                    if (RatePlanCode != "")
                        CountryName = Get_BookoHotel_RoomTypeID(RatePlanCode, ref CountryName);
                }

               // RatePlanCode = Get_LOH_RoomTypeID(RatePlanCode, ref NationalityCode); //260526

                StringBuilder objGuestRequest = new StringBuilder();
                int cnt = 0;
                int LPX = 0;
                dtPassengers.DefaultView.RowFilter = "PickUp='" + Convert.ToString(bookRequest.userCart.UserCartDetails[i].RoomTypeId) + "' and UserCartID='" + Convert.ToString(bookRequest.userCart.UserCartDetails[i].UserCartId) + "' and ServiceType='Hotel'";
                
                dtPassengers.DefaultView.Sort = "IsLead desc";
                if (dtPassengers.DefaultView.Count > 0)
                {
                    DataTable dtPax = dtPassengers.DefaultView.ToTable();
                    for (int k = 0; k < dtPax.Rows.Count; k++)
                    {
                        string FName = Convert.ToString(dtPax.Rows[k]["FirstName"]);
                        string LName = Convert.ToString(dtPax.Rows[k]["LastName"]);
                        int ChildAge = Convert.ToInt32(dtPax.Rows[k]["ChildAge"]);
                        int AdultAge = 30;
                        //=============== New Added on 1-dec-2023
                        string address = "";
                        string city = "";
                        string postalcode = "";
                        string phonenumber = "";
                        //==============
                        // added on 21-06-2024 #44183
                        string Name = "";
                        int pax_id = 0;
                        bool isPhonenorequired = false;
                        if (Paxdetails != "")
                        {
                            string[] Paxdetails1 = Paxdetails.Split(new String[] { "$$$" }, StringSplitOptions.None);
                            if (Paxdetails1.Length > 0)
                            {
                                for (int jk = 0; jk < Paxdetails1.Length; jk++)
                                {
                                    if (jk == k)
                                    {
                                        string[] Paxdetailsew = Paxdetails1[k].Split(new String[] { "$$" }, StringSplitOptions.None);
                                        pax_id = Convert.ToInt32(Paxdetailsew[0]);
                                        Name = Convert.ToString(Paxdetailsew[1]);
                                        isPhonenorequired = Convert.ToBoolean(Paxdetailsew[2]);
                                    }
                                }
                            }
                        }
                        //end 
                        DataRow[] drPassengers = dtPax.Select("IsLead='1'");
                        if (drPassengers.Length == 0)
                        {
                            if (Convert.ToString(dtPax.Rows[k]["PaxType"]).Trim().ToLower() == "adult")
                                dtPax.Rows[k]["IsLead"] = 1;
                            dtPax.AcceptChanges();

                        }
                        if (Convert.ToInt16(dtPax.Rows[k]["IsLead"]) == 1)
                        {
                            cnt = cnt + 1;
                            objGuestRequest.Append("<Pax IdPax=\"" + cnt + "\">");
                            objGuestRequest.Append("<Age>" + AdultAge + "</Age>");
                            objGuestRequest.Append("<Name>" + FName + "</Name>");
                            objGuestRequest.Append("<Surname>" + LName + "</Surname>");
                            objGuestRequest.Append("<Nationality>" + NationalityCode + "</Nationality>");
                            // added on 21-06-2024 #44183
                            if (isPhonenorequired == true && Name.ToLower() == "holder name")
                            {
                                objGuestRequest.Append("<PhoneNumbers>");
                                objGuestRequest.Append("<PhoneNumber>" + _supplierConfig.Juniper.PhoneNumber + "</PhoneNumber>");
                                objGuestRequest.Append("</PhoneNumbers>");
                            }
                            //end
                            //if (Provider.ToLower() == "tds" || Provider.ToLower() == "s2bglobal" || Provider.ToLower() == "robios")//added on 29-May-2025
                            //{
                            //    objGuestRequest.Append("<PhoneNumbers>");
                            //    objGuestRequest.Append("<PhoneNumber>" + phonenumber + "</PhoneNumber>");
                            //    objGuestRequest.Append("</PhoneNumbers>");
                            //}
                            if (Provider.ToLower() == "robios")
                            {
                                objGuestRequest.Append("<Address>" + address + "</Address>");
                                objGuestRequest.Append("<City>" + city + "</City>");
                                objGuestRequest.Append("<PostalCode>" + postalcode + "</PostalCode>");
                            }
                            if (Provider.ToLower() == "bookohotel")//added on 29-May-2025
                            {
                                if (CountryName != "")
                                    objGuestRequest.Append("<Country>" + CountryName + "</Country>");
                            }
                            objGuestRequest.Append("</Pax>");
                            LPX = cnt;
                        }
                        else
                        {
                            if (Convert.ToString(dtPax.Rows[k]["PaxType"]).Trim().ToLower() == "adult")
                            {
                                cnt = cnt + 1;
                                objGuestRequest.Append("<Pax IdPax=\"" + cnt + "\">");
                                objGuestRequest.Append("<Age>" + AdultAge + "</Age>");
                                objGuestRequest.Append("<Name>" + FName + "</Name>");
                                objGuestRequest.Append("<Surname>" + LName + "</Surname>");
                                objGuestRequest.Append("<Nationality>" + NationalityCode + "</Nationality>");
                                // added on 21-06-2024 #44183
                                if (isPhonenorequired == true && Name.ToLower() != "holder name")
                                {
                                    objGuestRequest.Append("<PhoneNumbers>");
                                    objGuestRequest.Append("<PhoneNumber>" + _supplierConfig.Juniper.PhoneNumber + "</PhoneNumber>");
                                    objGuestRequest.Append("</PhoneNumbers>");
                                }
                                //end 
                                if (Provider.ToLower() == "bookohotel")
                                {
                                    if (CountryName != "")
                                        objGuestRequest.Append("<Country>" + CountryName + "</Country>");
                                }//added on 29-May-2025
                                objGuestRequest.Append("</Pax>");
                            }
                            else
                            {
                                cnt = cnt + 1;
                                objGuestRequest.Append("<Pax IdPax=\"" + cnt + "\">");
                                objGuestRequest.Append("<Age>" + ChildAge + "</Age>");
                                objGuestRequest.Append("<Name>" + FName + "</Name>");
                                objGuestRequest.Append("<Surname>" + LName + "</Surname>");
                                objGuestRequest.Append("<Nationality>" + NationalityCode + "</Nationality>");
                                if (Provider.ToLower() == "bookohotel")
                                {
                                    if (CountryName != "")
                                        objGuestRequest.Append("<Country>" + CountryName + "</Country>");
                                }//added on 29-May-2025
                                objGuestRequest.Append("</Pax>");
                            }
                        }
                    }
                }
                StringBuilder BookRQ = new StringBuilder();
                BookRQ.Append("<soapenv:Envelope xmlns:soapenv=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns=\"http://www.juniper.es/webservice/2007/\">");
                BookRQ.Append("<soapenv:Header/>");
                BookRQ.Append("<soapenv:Body>");
                BookRQ.Append("<HotelBooking>");
                BookRQ.Append("<HotelBookingRQ Version=\"1.1\" Context=\"BOOKING\" Language=\"en\">");
                BookRQ.Append("<Login Email=\"" + UserID + "\" Password=\"" + Password + "\"/>");
                BookRQ.Append("<Paxes>");
                BookRQ.Append(objGuestRequest.ToString());
                BookRQ.Append("</Paxes>");
                BookRQ.Append("<Holder>");
                BookRQ.Append("<RelPax IdPax=\"" + LPX + "\"/>");
                BookRQ.Append("</Holder>");
                BookRQ.Append("<ExternalBookingReference>" + ExternalReference + "</ExternalBookingReference>");
                BookRQ.Append("<Elements>");
                BookRQ.Append("<HotelElement>");
                BookRQ.Append("<BookingCode>" + "#BookingCode" + "</BookingCode>");
                BookRQ.Append("<RelPaxesDist>");
                int R = 0;
                for (int x = 0; x < bookRequest.userCart.UserCartDetails.Count; x++)
                {
                    BookRQ.Append("<RelPaxDist>");
                    BookRQ.Append("<RelPaxes>");
                    for (int y = 0; y < Convert.ToInt32(bookRequest.userCart.UserCartDetails[x].NoofAdult); y++)
                    {
                        R = R + 1;
                        BookRQ.Append("<RelPax IdPax=\"" + R + "\"/>");
                    }
                    if (Convert.ToInt32(bookRequest.userCart.UserCartDetails[x].NoofChild) > 0)
                    {
                        for (int z = 0; z < Convert.ToInt32(bookRequest.userCart.UserCartDetails[x].NoofChild); z++)
                        {
                            R = R + 1;
                            BookRQ.Append("<RelPax IdPax=\"" + R + "\"/>");
                        }
                    }
                    BookRQ.Append("</RelPaxes>");
                    BookRQ.Append("</RelPaxDist>");
                }
                BookRQ.Append("</RelPaxesDist>");
                BookRQ.Append("<HotelBookingInfo Start=\"" + CheckIn + "\" End=\"" + CheckOut + "\">");
                BookRQ.Append("<Price>");
                BookRQ.Append("<PriceRange Minimum=\"" + 0 + "\" Maximum=\"" + Max + "\" Currency=\"" + Currency + "\"/>");
                BookRQ.Append("</Price>");
                BookRQ.Append("<Status>OK</Status>");
                if (Provider.ToLower() != "robios")
                {
                    //ticket no 39833
                    string Packag = "Package";
                    BookRQ.Append("<PackageContracts>" + Packag + "</PackageContracts>");
                }
                BookRQ.Append("<HotelCode>" + HotelCode + "</HotelCode>");
                BookRQ.Append("</HotelBookingInfo>");

                BookRQ.Append("</HotelElement>");
                BookRQ.Append("</Elements>");
                BookRQ.Append("<AdvancedOptions>");
                BookRQ.Append("<UseCurrency>" + Currency + "</UseCurrency>");
                BookRQ.Append("</AdvancedOptions>");
                BookRQ.Append("</HotelBookingRQ>");
                BookRQ.Append("</HotelBooking>");
                BookRQ.Append("</soapenv:Body>");
                BookRQ.Append("</soapenv:Envelope>");
                string BookingRQ = BookRQ.ToString();

                return BookingRQ.ToString();
                #endregion
            }
            catch (Exception ex)
            {
                _ = _exception.ErrorNew(ex, _LogAPI.Book);
                return "";
            }
        }
        public string Get_BookoHotel_RoomTypeID(string RoomTypeId, ref string Nationality)
        {
            try
            {
                string[] spitid = RoomTypeId.Split(new String[] { "$$$" }, StringSplitOptions.None);
                Nationality = spitid[4].ToString();
                //string Currency = spitid[5].ToString();
                return Nationality;
            }
            catch (Exception ex)
            {
                _ = _exception.ErrorNew(ex, _LogAPI.Book);
                return "";
            }
        }

        public string Get_JuniperNew_BookingCancelRQ(string AgentVoucherNo, DataRow[] SupplierDetails,string Provider = "")
        {
            string CancellationRQ = "";
            try
            {
                #region Booking Cancel RQ
                string UserID = Convert.ToString(SupplierDetails[0]["UserId"]);
                string Password = Convert.ToString(SupplierDetails[0]["Password"]);

                if (Provider.ToLower() == "goldenglobe")
                {
                    try
                    {
                        string[] spitid = AgentVoucherNo.Split(new String[] { "|" }, StringSplitOptions.None);
                        AgentVoucherNo = spitid[0].ToString();
                    }
                    catch (Exception ex)
                    {
                        _ = _exception.ErrorNew(ex, _LogAPI.CancelBooking);
                    }

                }

                StringBuilder CancelRQ = new StringBuilder();
                CancelRQ.Append("<soapenv:Envelope xmlns:soapenv=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns=\"http://www.juniper.es/webservice/2007/\">");
                CancelRQ.Append("<soapenv:Header/>");
                CancelRQ.Append("<soapenv:Body>");
                CancelRQ.Append("<CancelBooking>");
                CancelRQ.Append("<CancelRQ Version=\"1.1\" Context=\"CANCEL\" Language=\"en\">");
                CancelRQ.Append("<Login Email=\"" + UserID + "\" Password=\"" + Password + "\"/>");
                CancelRQ.Append("<CancelRequest ReservationLocator=\"" + AgentVoucherNo + "\"/>");
                CancelRQ.Append("</CancelRQ>");
                CancelRQ.Append("</CancelBooking>");
                CancelRQ.Append("</soapenv:Body>");
                CancelRQ.Append("</soapenv:Envelope>");
                CancellationRQ = CancelRQ.ToString();
                #endregion
            }
            catch (Exception ex)
            {
                _= _exception.ErrorNew(ex,_LogAPI.CancelBooking);
                return "";
            }
            return CancellationRQ;
        }
    }
}
