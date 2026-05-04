using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using XConnect.CommonUtilities;
using XConnect.CommonUtilities.Interface;
using XConnect.DAL.Interface;
using XConnect.ErrorLogHandler.Interface;
using XConnect.Markup.Interface;
using XConnect.Models;
using XConnect.Models.Hotel;
using XConnect.Supplier.Saltours.Interface;
using ZstdSharp.Unsafe;
using static Azure.Core.HttpHeader;
using Options = XConnect.Models.Options;


namespace XConnect.Supplier.Saltours
{
    public class SaltoursRepo : ISaltours
    {
        private readonly ICommonMethod _commonMethod;
        private readonly IException _exception;
        private readonly IMarkup _markup;
        private readonly ISupplierRes _supplierRes;
        private readonly KeySettings _keySettings;


        public SaltoursRepo(ICommonMethod commonMethod, IMarkup markup, ISupplierRes supplierRes, IOptions<KeySettings> keySettings, IException exception)
        {
            _commonMethod = commonMethod;
            _markup = markup;
            _supplierRes = supplierRes;
            _keySettings = keySettings.Value;
            _exception = exception;
        }
        public void GetSaltoursRequestOnly_Async(DataRow[] commonCode, List<StarRating> listRating, DataTable dtPreferredDataHotel, HotelSearchParameter SearchPara, List<SupplierSearchConfig> lstSaltoursSearchConfig, List<SupplierDetails> lstSaltoursDetails, ref ConcurrentBag<HotelX> AllTask, string AgentCode, int Hit, DataTable dtROE, TokenData tokenData, DataSet dtXMLCity)
        {
            try
            {
                List<StarRating> lstRating = new List<StarRating>();
                lstRating = _commonMethod.GetStarRatingStringList_xtg(SearchPara);

                List<HotelCodeByCity> DtHotelDAta = HtmlExtensions.ToList<HotelCodeByCity>(commonCode.CopyToDataTable());
                int ratingcount = lstRating.Count;
                int hotelcount = DtHotelDAta.Count;

                foreach (SupplierDetails SaltoursDetails in lstSaltoursDetails)
                {
                    int SupplierID = SaltoursDetails.SupplierID;
                    string Supplier = SaltoursDetails.SupplierName;

                    if (lstSaltoursSearchConfig.Count > 0)
                    {
                        try
                        {
                            List<SupplierSearchConfig> SaltoursSearchConfig = lstSaltoursSearchConfig
                                 .Where(x => x != null && x.SupplierID == SupplierID)
                                 .ToList();

                            if (SaltoursSearchConfig != null && SaltoursSearchConfig.Count > 0)
                            {

                                DataForSearch dataForSearch = _commonMethod.GetXmlDataForSearchTechwise(SearchPara.Nationality, dtXMLCity, Supplier, SaltoursDetails.TechName);

                                if (dataForSearch.XmlNationality != "")
                                {
                                    string Ids = string.Empty;
                                    string xmlbase = "";
                                    _commonMethod.GetPrefferedHotelCodes(ref Ids, ref xmlbase, ref SaltoursSearchConfig, Supplier, dtPreferredDataHotel, SearchPara.IsSearchFor);

                                    if (SaltoursSearchConfig != null)
                                    {
                                        List<HotelCodeByCity> filter = new List<HotelCodeByCity>();
                                        filter = _commonMethod.FilterWithOutCity_Code(ratingcount, hotelcount, DtHotelDAta, lstRating, Supplier);
                                        if (filter.Count > 0)
                                        {
                                            double CurrencyFact = 1;
                                            string Currency = Convert.ToString(SaltoursDetails.CurrencyShortName);
                                            if (Currency.ToLower() != tokenData.SiteCurrency.ToLower())
                                            {
                                                CurrencyFact = _commonMethod.GetROEByCurrency(Currency, dtROE);
                                            }

                                            for (int k = 0; k < SaltoursSearchConfig.Count; k++)
                                            {
                                                bool DebugMode = (Convert.ToInt32(SaltoursSearchConfig[k].DebugMode) == 0 ? false : true);

                                                {
                                                    int IsCitywisesearch = Convert.ToInt32(SaltoursSearchConfig[k].City);

                                                    if (Convert.ToInt32(SaltoursSearchConfig[k].CityID) == 0 || Convert.ToInt32(SaltoursSearchConfig[k].CityID) == SearchPara.CityID)
                                                    {
                                                        if (commonCode.Length > 0)
                                                        {
                                                            int Noofrecords = Convert.ToInt32(SaltoursSearchConfig[k].Noofrecords);
                                                            double count = Convert.ToDouble(filter.Count);
                                                            int NoOfDiv = Convert.ToInt32(Math.Ceiling(count / Convert.ToDouble(Noofrecords)));
                                                            List<List<XMLID_Search>> lstxml = _commonMethod.GetXMLIDString(NoOfDiv, Noofrecords, count, filter);


                                                            if (SearchPara.Filters.IsShowRooms == 0)
                                                            {
                                                                for (int a = 0; a < NoOfDiv; a++)
                                                                {
                                                                    string IdsForSearch = Convert.ToString(_commonMethod.GetHotelCodeByCOmma(lstxml[a]));

                                                                    HotelX xhotels = new HotelX();
                                                                    xhotels.Supplier = Supplier;
                                                                    if (_keySettings.SupplierProxy == "0")
                                                                        xhotels.URL = Convert.ToString(SaltoursDetails.WebSiteURL) + Convert.ToString(SalyoursInfo.SaltoursSearchURL);
                                                                    else
                                                                        xhotels.URL = Convert.ToString(SaltoursDetails.WebSiteURLforAPI) + Convert.ToString(SalyoursInfo.SaltoursSearchURL);
                                                                    xhotels.DebugeMode = DebugMode;
                                                                    xhotels.SearchTimeOut = Convert.ToInt32(SaltoursSearchConfig[k].HotelSearchTimeOut);
                                                                    //if (Hit > 0)
                                                                    xhotels.ProcessTimeout = Convert.ToInt32(SaltoursSearchConfig[k].HotelSearchTimeOut2);

                                                                    xhotels.CurrencyCode = Currency;
                                                                    xhotels.CurrencyFactor = CurrencyFact;
                                                                    xhotels.XmlNationality = dataForSearch.XmlNationality;//WebConfigurationManager.AppSettings["PaximumNationality"];
                                                                    xhotels.XmlCity = dataForSearch.xmlCity;
                                                                    xhotels.XmlCountry = dataForSearch.xmlCountry;
                                                                    xhotels.AgentWiseMarkup = SaltoursDetails.AgentWiseMarkup;
                                                                    xhotels.IsShowSupplier = Convert.ToInt16(SaltoursDetails.IsShowSupplier) == 1 ? true : false;
                                                                    xhotels.SupplierID = SaltoursDetails.SupplierID;
                                                                    xhotels.MasterSupplierID = SaltoursDetails.MasterSupplierId;
                                                                    #region "CoomonCode"
                                                                    bool IsHitSeperate = _commonMethod.IsSuppliersWithSeperateRoomRquest(xhotels.Supplier);
                                                                    int NoOfRoomsSearch = 1;
                                                                    if (IsHitSeperate)
                                                                        NoOfRoomsSearch = SearchPara.Rooms.Count;
                                                                    List<RQRS> RQRS = new List<RQRS>();
                                                                    xhotels.IsSeperate = IsHitSeperate;
                                                                    for (int rr = 0; rr < NoOfRoomsSearch; rr++)
                                                                    {
                                                                        RQRS objrq = new RQRS();
                                                                        List<RoomDetails> serchRooms = new List<RoomDetails>();
                                                                        serchRooms.Add(SearchPara.Rooms[rr]);
                                                                        objrq.Request = XMLSaltoursRequestOnly_Async(SearchPara, xhotels.XmlCity, xhotels.XmlCountry, Convert.ToString(SaltoursDetails.UserName), Convert.ToString(SaltoursDetails.Password), xhotels.XmlNationality, Convert.ToInt32(xhotels.SearchTimeOut), Currency, IdsForSearch);
                                                                        objrq.Response = ""; RQRS.Add(objrq);
                                                                    }
                                                                    xhotels.RQRS = RQRS;
                                                                    #endregion
                                                                    xhotels.ReturnRooms = SaltoursDetails.ReturnRooms;
                                                                    xhotels.TechName = SaltoursDetails.TechName;
                                                                    AllTask.Add(xhotels);
                                                                }
                                                            }
                                                            else  // && count == 1)
                                                            {
                                                                for (int a = 0; a < NoOfDiv; a++)
                                                                {
                                                                    string IdsForSearch = Convert.ToString(_commonMethod.GetHotelCodeByCOmma(lstxml[a]));

                                                                    HotelX xhotels = new HotelX();
                                                                    xhotels.Supplier = Supplier;
                                                                    if (_keySettings.SupplierProxy == "0")
                                                                        xhotels.URL = Convert.ToString(SaltoursDetails.WebSiteURL) + Convert.ToString(SalyoursInfo.SaltoursSearchURL);
                                                                    else
                                                                        xhotels.URL = Convert.ToString(SaltoursDetails.WebSiteURLforAPI) + Convert.ToString(SalyoursInfo.SaltoursSearchURL);

                                                                    xhotels.DebugeMode = DebugMode;
                                                                    xhotels.SearchTimeOut = Convert.ToInt32(SaltoursSearchConfig[k].HotelSearchTimeOut);
                                                                    //if (Hit > 0)
                                                                    xhotels.ProcessTimeout = Convert.ToInt32(SaltoursSearchConfig[k].HotelSearchTimeOut2);
                                                                    xhotels.CurrencyCode = Currency;
                                                                    xhotels.CurrencyFactor = CurrencyFact;
                                                                    xhotels.XmlNationality = dataForSearch.XmlNationality;
                                                                    xhotels.XmlCity = dataForSearch.xmlCity;
                                                                    xhotels.XmlCountry = dataForSearch.xmlCountry;
                                                                    xhotels.AgentWiseMarkup = SaltoursDetails.AgentWiseMarkup;
                                                                    xhotels.IsShowSupplier = Convert.ToInt16(SaltoursDetails.IsShowSupplier) == 1 ? true : false;
                                                                    xhotels.SupplierID = SaltoursDetails.SupplierID;
                                                                    xhotels.MasterSupplierID = SaltoursDetails.MasterSupplierId;
                                                                    #region "CoomonCode"
                                                                    bool IsHitSeperate = _commonMethod.IsSuppliersWithSeperateRoomRquest(xhotels.Supplier);
                                                                    int NoOfRoomsSearch = 1;
                                                                    if (IsHitSeperate)
                                                                        NoOfRoomsSearch = SearchPara.Rooms.Count;
                                                                    List<RQRS> RQRS = new List<RQRS>();
                                                                    xhotels.IsSeperate = IsHitSeperate;
                                                                    for (int rr = 0; rr < NoOfRoomsSearch; rr++)
                                                                    {
                                                                        RQRS objrq = new RQRS();
                                                                        List<RoomDetails> serchRooms = new List<RoomDetails>();
                                                                        serchRooms.Add(SearchPara.Rooms[rr]);
                                                                        objrq.Request = XMLSaltoursRequestOnly_Async(SearchPara, xhotels.XmlCity, xhotels.XmlCountry, Convert.ToString(SaltoursDetails.UserName), Convert.ToString(SaltoursDetails.Password), xhotels.XmlNationality, Convert.ToInt32(xhotels.SearchTimeOut), Currency, IdsForSearch);
                                                                        objrq.Response = ""; RQRS.Add(objrq);
                                                                    }
                                                                    xhotels.RQRS = RQRS;
                                                                    #endregion
                                                                    xhotels.ReturnRooms = SaltoursDetails.ReturnRooms;
                                                                    xhotels.TechName = SaltoursDetails.TechName;
                                                                    AllTask.Add(xhotels);
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
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
            }
        }

        public string XMLSaltoursRequestOnly_Async(HotelSearchParameter SearchPara, string CityCode, string CountryCode, string saltours_User_Name, string saltours_Password, string saltoursNationality, Int32 TimeOut, string saltoursCurrency, string hotelId)
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
                    #region Req
                    StringBuilder SRQ = new StringBuilder();
                    SRQ.Append("<Envelope xmlns='http://schemas.xmlsoap.org/soap/envelope/' xmlns:xsd='http://www.w3.org/2001/XMLSchema' xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance'>");
                    SRQ.Append("<Header>");
                    SRQ.Append("<Security xmlns= 'http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd'>");
                    SRQ.Append("<UsernameToken>");
                    //SRQ.Append("<Username>" + saltours_User_Name + "</Username>");
                    SRQ.Append("<Username>" + saltours_User_Name + "</Username>");
                    SRQ.Append("<Password>" + saltours_Password + "</Password>");
                    SRQ.Append("</UsernameToken>");
                    SRQ.Append("</Security>");
                    SRQ.Append("</Header>");
                    SRQ.Append("<Body>");
                    SRQ.Append("<OTA_HotelAvailRQ BestOnly=\"\"  RateDetailsInd=\"\"  ResponseFormat=\"0\"  Currency=\"" + saltoursCurrency + "\">");
                    SRQ.Append("<POS xmlns=\'http://www.opentravel.org/OTA/2003/05'>");
                    SRQ.Append("<Source ISOCountry=\"" + saltoursNationality + "\" />");
                    SRQ.Append("</POS>");
                    SRQ.Append("<AvailRequestSegments xmlns='http://www.opentravel.org/OTA/2003/05'>");
                    SRQ.Append("<AvailRequestSegment xmlns=\"'http://www.opentravel.org/OTA/2003/05'\" ResponseType=\"PropertyRateList\">");
                    SRQ.Append("<StayDateRange xmlns=\"'http://www.opentravel.org/OTA/2003/05'\" End=\"" + checkout + "\" Start= \"" + checkIn + "\" />");
                    SRQ.Append("<RoomStayCandidates>");

                    for (int i = 0; i < SearchPara.Rooms.Count; i++)
                    {
                        SRQ.Append("<RoomStayCandidate Quantity=\"1\">");
                        SRQ.Append("<GuestCounts>");
                        int ACount = SearchPara.Rooms[i].NoofAdults;
                        int NoofChild = SearchPara.Rooms[i].NoofChild;
                        SRQ.Append("<GuestCount Count=\"" + ACount + "\"  AgeQualifyingCode =\"10\"/>");

                        //if (NoofChild > 0)
                        //{
                        //    switch (NoofChild)
                        //    {
                        //        case 1:
                        //            int ChildAge1 = SearchPara.Rooms[ser].Child1Age;
                        //            SRQ.Append("<GuestCount Count=\"1\"  AgeQualifyingCode =\"8\" Age=\"" + ChildAge1 + "\"/>");
                        //            break;
                        //        case 2:
                        //            int ChildAge = SearchPara.Rooms[ser].Child1Age;
                        //            int ChildAge2 = SearchPara.Rooms[ser].Child2Age;
                        //            SRQ.Append("<GuestCount Count=\"1\"  AgeQualifyingCode =\"8\" Age=\"" + ChildAge + "\"/>");
                        //            SRQ.Append("<GuestCount Count=\"1\"  AgeQualifyingCode =\"8\" Age=\"" + ChildAge2 + "\"/>");
                        //            break;
                        //    }
                        //}

                        if (SearchPara.Rooms[i].NoofChild > 0)
                        {
                            if (SearchPara.Rooms[i].NoofChild == 1)
                            {
                                if (SearchPara.Rooms[i].Child1Age < 2)
                                {
                                    SRQ.Append("<GuestCount Count=\"" + 1 + "\" AgeQualifyingCode=\"7\" Age=\"" + SearchPara.Rooms[i].Child1Age + "\" />");
                                }
                                else
                                {
                                    SRQ.Append("<GuestCount Count=\"" + 1 + "\" AgeQualifyingCode=\"8\" Age=\"" + SearchPara.Rooms[i].Child1Age + "\" />");
                                }

                            }
                            if (SearchPara.Rooms[i].NoofChild == 2)
                            {
                                if (SearchPara.Rooms[i].Child1Age > 2 && SearchPara.Rooms[i].Child2Age > 2)
                                {
                                    SRQ.Append("<GuestCount Count=\"" + 1 + "\" AgeQualifyingCode=\"8\" Age=\"" + SearchPara.Rooms[i].Child1Age + "\" />");
                                    SRQ.Append("<GuestCount Count=\"" + 1 + "\" AgeQualifyingCode=\"8\" Age=\"" + SearchPara.Rooms[i].Child2Age + "\" />");
                                }
                                else if (SearchPara.Rooms[i].Child1Age > 2 && SearchPara.Rooms[i].Child2Age < 2)
                                {
                                    SRQ.Append("<GuestCount Count=\"" + 1 + "\" AgeQualifyingCode=\"8\" Age=\"" + SearchPara.Rooms[i].Child1Age + "\" />");
                                    SRQ.Append("<GuestCount Count=\"" + 1 + "\" AgeQualifyingCode=\"7\" Age=\"" + SearchPara.Rooms[i].Child2Age + "\" />");
                                }
                                else if (SearchPara.Rooms[i].Child1Age < 2 && SearchPara.Rooms[i].Child2Age > 2)
                                {
                                    SRQ.Append("<GuestCount Count=\"" + 1 + "\" AgeQualifyingCode=\"7\" Age=\"" + SearchPara.Rooms[i].Child1Age + "\" />");
                                    SRQ.Append("<GuestCount Count=\"" + 1 + "\" AgeQualifyingCode=\"8\" Age=\"" + SearchPara.Rooms[i].Child2Age + "\" />");
                                }
                                else
                                {
                                    SRQ.Append("<GuestCount Count=\"" + 1 + "\" AgeQualifyingCode=\"7\" Age=\"" + SearchPara.Rooms[i].Child1Age + "\" />");
                                    SRQ.Append("<GuestCount Count=\"" + 2 + "\" AgeQualifyingCode=\"7\" Age=\"" + SearchPara.Rooms[i].Child2Age + "\" />");
                                }
                            }
                        }


                        SRQ.Append("</GuestCounts>");
                        SRQ.Append("</RoomStayCandidate>");

                    }
                    SRQ.Append("</RoomStayCandidates>");
                    SRQ.Append("<HotelSearchCriteria>");
                    SRQ.Append("<Criterion>");

                    if (hotelId.Trim() != string.Empty)
                    {
                        string[] HotelIds = Convert.ToString(hotelId).Split(',');
                        StringBuilder ItemCodeString = new StringBuilder();
                        for (int htid = 0; htid < HotelIds.Length; htid++)
                        {
                            SRQ.Append("<HotelRef HotelCode=\"" + HotelIds[htid] + "\" />");
                        }
                    }
                    else if (CityCode.Trim() != string.Empty)
                    {
                        SRQ.Append("<HotelRef HotelCityCode=\"" + CityCode + "\" />");
                    }
                    SRQ.Append("</Criterion>");
                    SRQ.Append("</HotelSearchCriteria>");
                    SRQ.Append("</AvailRequestSegment>");
                    SRQ.Append("</AvailRequestSegments>");
                    SRQ.Append("</OTA_HotelAvailRQ>");
                    SRQ.Append("</Body>");
                    SRQ.Append("</Envelope>");
                    string SalToursString = SRQ.ToString();
                    return SalToursString;
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

        public async Task<List<Options>> Saltours_HotelSearchWithRooms(HotelX item, CommonSearch commonSearch, string ResponseData, HotelSearchParameter SearchParam, DataTable dtAgentDetail, DataTable dtSuplierMarkup, DataTable dtMarkupDetails, string XMLCITY, string XMLCountry, string Currency, string Provider, DataSet dsAgentDetails, DataTable dtCountryCityMarkup, int AgentId, Int32 TariffID, double AgentPercentage, Boolean IsShowSupplier, int SupplierID, int MasterSupplierID, string ProccessCutOffTime, Int32 processTimeout, string ResProccessTimerLogActive, string ResProccessTimeValueforSaveLog, DataTable dtROE, string XmlNationality = "", Double CurrencyFactor = 1)

        {
            System.Diagnostics.Stopwatch timer_room_Saltours = new System.Diagnostics.Stopwatch();
            timer_room_Saltours.Start();
            List<Options> options = new List<Options>();
            try
            {
                #region DataTable
                DataTable DtHotel = _commonMethod.GenerateDataTableNewV2();

                #endregion

                DataSet SaltoursResponse = _commonMethod.GetDataSetFromXMLString(ResponseData);

                if (ResponseData != string.Empty)
                {
                    #region PriceOnly RS
                    if (ResponseData == string.Empty)

                        if (SaltoursResponse == null || SaltoursResponse.Tables["RoomStay"] == null)
                        {
                            item.ResponseType = "NoCount";
                            return options;
                        }
                    #endregion

                    //#region Hotel List
                    if (SaltoursResponse.Tables.Contains("RoomStay"))
                    {


                        if (SaltoursResponse.Tables["RoomStay"].Rows.Count < 1)
                        {
                            item.ResponseType = "NoCount";
                            return options;
                        }
                    }
                    else
                    {
                        item.ResponseType = "NoCount";
                        return options;
                    }
                    for (int h = 0; h <= SaltoursResponse.Tables["RoomStay"].Rows.Count - 1; h++)
                    {

                        string HotelCode = "";
                        double price = 0;

                        string Roomstay_id = Convert.ToString(SaltoursResponse.Tables["RoomStay"].Rows[h]["RoomStay_Id"]);
                        DataRow[] drRoomStay = SaltoursResponse.Tables["BasicPropertyInfo"].Select("RoomStay_Id=" + Roomstay_id + "");

                        if (drRoomStay.Length > 0)
                        {
                            for (int a = 0; a < drRoomStay.Length; a++)
                            {
                                HotelCode = Convert.ToString(drRoomStay[a]["HotelCode"]);

                                int Rating = 0;

                                #region When Not Need Rooms
                                if (SearchParam.Filters.IsShowRooms == 0)
                                {
                                    DataRow drXmlHotel = DtHotel.NewRow();

                                    {
                                        #region "Hotel Code Block "

                                        drXmlHotel["ContractID"] = 0;
                                        drXmlHotel["SupplierId"] = SupplierID;

                                        drXmlHotel["HotelName"] = ""; //Convert.ToString(drGiataDetails[0]["HotelName"]);
                                        drXmlHotel["Address"] = "";
                                        drXmlHotel["MasterSupplierId"] = Convert.ToInt32(MasterSupplierID);

                                        drXmlHotel["GIITACode"] = "";
                                        drXmlHotel["IsShowSupplier"] = IsShowSupplier;
                                        drXmlHotel["UniqueNo"] = Guid.NewGuid().ToString().Substring(0, 4);
                                        drXmlHotel["RaynaId"] = "";

                                        drXmlHotel["OwnsystemHotelId"] = "";
                                        drXmlHotel["Rating"] = Rating;

                                        drXmlHotel["IsRecomondedHotel"] = false;

                                        drXmlHotel["XMLHotelCode"] = HotelCode;//Convert.ToString(SaltoursResponse.Tables["BasicPropertyInfo"].Rows[h]["HotelCode"]);
                                        drXmlHotel["SupplierName"] = Provider;

                                        DataRow[] drRoomRates = SaltoursResponse.Tables["RoomRates"].Select("RoomStay_Id=" + Roomstay_id + "");
                                        string RoomRates_id = Convert.ToString(drRoomRates[0]["RoomRates_Id"]);
                                        DataRow[] drRoomrate = SaltoursResponse.Tables["RoomRate"].Select("RoomRates_Id=" + RoomRates_id + "");

                                        //for (int r = 0; r < drRoomrate.Length; r++)
                                        {


                                            /*DataRow[] drRoomrate_Id = SaltoursResponse.Tables["Total"].Select("RoomRate_Id=" + RoomRates_id + "")*/
                                            //;
                                            string RoomRate_id = Convert.ToString(drRoomrate[0]["RoomRate_Id"]);

                                            DataRow[] drRate_Id = SaltoursResponse.Tables["Rates"].Select("RoomRate_Id=" + RoomRate_id + "");
                                            int Rates_Id = Convert.ToInt32(drRate_Id[0]["Rates_Id"]);

                                            DataRow[] drRate = SaltoursResponse.Tables["Rate"].Select("Rates_Id=" + Rates_Id + "");
                                            int Rate_Id = Convert.ToInt32(drRate[0]["Rate_Id"]);

                                            DataRow[] drBase = SaltoursResponse.Tables["Base"].Select("Rate_Id=" + Rate_Id + "");

                                            price = Convert.ToDouble(drBase[0]["AmountAfterTax"]);
                                            Currency = Convert.ToString(drBase[0]["CurrencyCode"]);
                                        }
                                        drXmlHotel["SupplierCurrency"] = Currency;
                                        if (Currency.ToLower() != commonSearch.tokenData.SiteCurrency.ToLower())
                                        {
                                            CurrencyFactor = _commonMethod.GetROEByCurrency(Currency, dtROE);
                                        }
                                        double Pricenew = price * CurrencyFactor * SearchParam.NoofNights * SearchParam.Rooms.Count();

                                        drXmlHotel["FinalSellingPrice"] = Pricenew;
                                        drXmlHotel["SellingPrice"] = Pricenew;

                                        DtHotel.Rows.Add(drXmlHotel);

                                        double TotalCharges = Pricenew;
                                        #endregion
                                        if (TotalCharges > 0)
                                        {
                                            #region "Room code Block"
                                            bool willAddOrNot = true;
                                            Int64 RaynaID = 0;
                                            Int64 GiataId = 0;
                                            //int Rating = 0;
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
                                                decimal LowestPrice = Convert.ToDecimal(MinPrice).CalculatePrice(commonSearch.tokenData, commonSearch.advancedOptions, item.CurrencyCode);
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
                                    #region "Hotel Code Block "
                                    DataRow drXmlHotel = DtHotel.NewRow();
                                    drXmlHotel["ContractID"] = 0;
                                    drXmlHotel["SupplierId"] = SupplierID;

                                    drXmlHotel["HotelName"] = ""; //Convert.ToString(drGiataDetails[0]["HotelName"]);
                                    drXmlHotel["Address"] = "";
                                    drXmlHotel["MasterSupplierId"] = Convert.ToInt32(MasterSupplierID);

                                    drXmlHotel["GIITACode"] = "";
                                    drXmlHotel["IsShowSupplier"] = IsShowSupplier;
                                    drXmlHotel["UniqueNo"] = Guid.NewGuid().ToString().Substring(0, 4);
                                    drXmlHotel["RaynaId"] = "";

                                    drXmlHotel["OwnsystemHotelId"] = "";
                                    drXmlHotel["Rating"] = Rating;

                                    drXmlHotel["IsRecomondedHotel"] = false;

                                    drXmlHotel["XMLHotelCode"] = HotelCode;//Convert.ToString(SaltoursResponse.Tables["BasicPropertyInfo"].Rows[h]["HotelCode"]);
                                    drXmlHotel["SupplierName"] = Provider;
                                    drXmlHotel["SupplierCurrency"] = Currency;
                                    if (Currency.ToLower() != commonSearch.tokenData.SiteCurrency.ToLower())
                                    {
                                        CurrencyFactor = _commonMethod.GetROEByCurrency(Currency, dtROE);
                                    }
                                    double Pricenew = 1;

                                    drXmlHotel["FinalSellingPrice"] = Pricenew;
                                    drXmlHotel["SellingPrice"] = Pricenew;

                                    DtHotel.Rows.Add(drXmlHotel);

                                    double TotalCharges = Pricenew;
                                    #endregion
                                    if (TotalCharges > 0)
                                    {
                                        #region "Room code Block"
                                        bool willAddOrNot = true;
                                        Int64 RaynaID = 0;
                                        Int64 GiataId = 0;
                                        //int Rating = 0;
                                        Int64 OwnsystemHotelId = 0;
                                        double MinPrice = TotalCharges;
                                        _markup.GetHotelDataCommon(ref MinPrice, ref willAddOrNot, ref OwnsystemHotelId, ref RaynaID, ref GiataId, ref Rating, ref drXmlHotel, commonSearch.AgentId, commonSearch.TariffID, commonSearch.dsAgentDetails, commonSearch.supplierGiataMaster, SearchParam, commonSearch.dtCountryCityMarkup, commonSearch.B2CMarkup, commonSearch.IsParentId, commonSearch.SubAgentMarkup, commonSearch.SearchByHotel, commonSearch.HotelIDS);
                                        if (willAddOrNot)
                                        {
                                            DataTable DtSaltoursHotelRoom = _commonMethod.GenerateRoomtypeDataTableNewV2();
                                            DataTable DtSaltoursRoomRateBreackUp = _commonMethod.GenerateoomRateBreackUpDataTableNewV2();
                                            DataTable dtSaltoursCancellation = _commonMethod.GenerateCancellationDataTableNewV2();
                                            #region "Room Logic"
                                            if (SearchParam.Filters.IsShowRooms == 1)
                                            {
                                                int CutOffDays = XMLHotelBookingCommon.CutOffDays;
                                                CutOffDays = _commonMethod.GetCutOffDays(dtSuplierMarkup, "saltours");

                                                double SubAgentMarkup = 0;
                                                int IsParentId = 1, agentID = 0;
                                                if (dtAgentDetail != null)
                                                {
                                                    IsParentId = Convert.ToInt32(dtAgentDetail.Rows[0]["IsParentId"]);
                                                    SubAgentMarkup = Convert.ToDouble(dtAgentDetail.Rows[0]["B2CMarkupPer"]);
                                                    agentID = Convert.ToInt32(dtAgentDetail.Rows[0]["AgentID"]);
                                                }
                                                //double AgentPercentage = 0.0;
                                                Boolean blnIsShowSupplier = false;
                                                string GIATACode = "";
                                                double AgentMarkupAmount = 0.0;
                                                int hotelRating = 0;
                                                Boolean isPerce = true;
                                                //_markup.getAgetMarkupSetting(Provider, GIATACode, dtSuplierMarkup, dtMarkupDetails, ref AgentPercentage, ref AgentMarkupAmount, ref blnIsShowSupplier, ref isPerce, agentID, hotelRating, SearchParam.CityID,Convert.ToString(SearchParam.CountryID), Convert.ToString(SearchParam.CheckInDate));

                                                _markup.getAgetMarkupSettingNew_new(Provider, Convert.ToString(RaynaID), commonSearch.dtSuplierMarkup, commonSearch.dsMarkupDetails, ref AgentPercentage, ref AgentMarkupAmount, ref blnIsShowSupplier, ref isPerce, commonSearch.AgentId, hotelRating, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), SearchParam.CheckInDate);
                                                double AgentCompanyPercentage = Convert.ToDouble(SearchParam.AgentCompanyPercentage);
                                                bool IsB2C = Convert.ToBoolean(Convert.ToByte(SearchParam.IsB2BOrB2C));
                                                double B2CMarkup = (IsB2C == false ? AgentCompanyPercentage : XMLHotelBookingCommon.B2CMarkup);
                                                if (IsB2C == true)
                                                {
                                                    AgentPercentage = 0;
                                                }

                                                string checkin = SearchParam.CheckInDate; //Convert.ToString(xmlDocRequest.SelectSingleNode("//Hotel/StayDateRange/@Start").InnerText);
                                                string checkout = SearchParam.CheckOutDate; //Convert.ToString(xmlDocRequest.SelectSingleNode("//Hotel/StayDateRange/@End").InnerText);
                                                int roomnos = Convert.ToInt32(SearchParam.Rooms.Count);
                                                int totnight = Convert.ToInt32(SearchParam.NoofNights);
                                                checkin = "" + checkin.Substring(6, 4) + "/" + checkin.Substring(3, 2) + "/" + checkin.Substring(0, 2) + "";
                                                checkout = "" + checkout.Substring(6, 4) + "/" + checkout.Substring(3, 2) + "/" + checkout.Substring(0, 2) + "";
                                                string Room_Text = "";
                                                double AmountAfterTax = 0;

                                                string RoomTypeCode = "";
                                                string NumberOfUnits = "";
                                                string TotalCurrency = "";
                                                string Text_Enss = "";
                                                string RatesPlanCode = "";
                                                int CancelPolicies_Id = 0;
                                                string Nationality = XmlNationality;
                                                string SpeDesText = "";

                                                int Adult = 0;
                                                int Child = 0;
                                                DataRow[] drRoomRate = SaltoursResponse.Tables["RoomRates"].Select("Roomstay_id=" + Roomstay_id + "");
                                                for (int RR = 0; RR < drRoomRate.Length; RR++)
                                                {
                                                    string RoomRates = Convert.ToString(drRoomRate[RR]["RoomRates_Id"]);
                                                    int RoomStay_Id = Convert.ToInt32(drRoomRate[RR]["RoomStay_Id"]);

                                                    DataRow[] drRoomRates = SaltoursResponse.Tables["RoomRate"].Select("RoomRates_Id=" + RoomRates + "");
                                                    for (int Room = 0; Room < drRoomRates.Length; Room++)
                                                    {
                                                        RoomTypeCode = Convert.ToString(drRoomRates[Room]["RoomTypeCode"]);
                                                        RatesPlanCode = Convert.ToString(drRoomRates[Room]["RatePlanCode"]);

                                                        int RoomRate_Id = Convert.ToInt32(drRoomRates[Room]["RoomRate_Id"]);

                                                        DataRow[] drGuestCounts = SaltoursResponse.Tables["GuestCounts"].Select("RoomRate_Id=" + RoomRate_Id + "");
                                                        int GuestCounts_Id = Convert.ToInt32(drGuestCounts[0]["GuestCounts_Id"]);

                                                        int RoomNo = 0;
                                                        DataRow[] drRoomRateDescription = SaltoursResponse.Tables["RoomRateDescription"].Select("RoomRate_Id=" + RoomRate_Id);//+ " and Name = '" + RoomIndex + "'");
                                                        for (int i = 0; i < drRoomRateDescription.Length; i++)
                                                        {
                                                            // RoomNo = Convert.ToInt32(drRoomRateDescription[i]["Text"]);

                                                            if (SaltoursResponse.Tables.Contains("RoomRateDescription"))
                                                            {
                                                                if (SaltoursResponse.Tables["RoomRateDescription"].Columns.Contains("Name"))
                                                                {
                                                                    string name1 = Convert.ToString(drRoomRateDescription[i]["Name"]);
                                                                    if (name1.ToLower() == "roomindex")
                                                                    {
                                                                        RoomNo = Convert.ToInt32(drRoomRateDescription[i]["Text"]);
                                                                    }

                                                                    if (name1.ToLower() == "specialnotes")
                                                                    {
                                                                        DataRow[] drRoomRateDescriptions = SaltoursResponse.Tables["RoomRateDescription"].Select("RoomRate_Id=" + RoomRate_Id);// + " and SpecialNotes= " + SpecialNotes + "");
                                                                        SpeDesText = Convert.ToString(drRoomRateDescriptions[i]["Text"]);
                                                                    }
                                                                }
                                                            }
                                                        }

                                                        DataRow[] drRoomType = SaltoursResponse.Tables["RoomType"].Select("RoomTypeCode='" + RoomTypeCode + "'");
                                                        int RoomType_Id = Convert.ToInt32(drRoomType[0]["RoomType_Id"]);
                                                        NumberOfUnits = Convert.ToString(drRoomType[0]["NumberOfUnits"]);


                                                        DataRow[] drRoomName = SaltoursResponse.Tables["RoomDescription"].Select("RoomType_Id='" + RoomType_Id + "'");
                                                        Room_Text = Convert.ToString(drRoomName[0]["Text"]);


                                                        DataRow[] drRate_Id = SaltoursResponse.Tables["Rates"].Select("RoomRate_Id=" + RoomRate_Id + "");
                                                        int Rates_Id = Convert.ToInt32(drRate_Id[0]["Rates_Id"]);

                                                        DataRow[] drRate = SaltoursResponse.Tables["Rate"].Select("Rates_Id=" + Rates_Id + "");
                                                        int Rate_Id = Convert.ToInt32(drRate[0]["Rate_Id"]);

                                                        //DataRow[] drBase = SaltoursResponse.Tables["Base"].Select("Rate_Id=" + drRate + "");
                                                        DataRow[] drBase = SaltoursResponse.Tables["Base"].Select("Rate_Id=" + Rate_Id + "");
                                                        AmountAfterTax = Convert.ToDouble(drBase[0]["AmountAfterTax"]);
                                                        string CurrencyCode = Convert.ToString(drBase[0]["CurrencyCode"]);

                                                        DataRow[] drRatesPlanCode = SaltoursResponse.Tables["RatePlan"].Select("RatePlanCode='" + RatesPlanCode + "'");
                                                        int RatePlan_Id = Convert.ToInt32(drRatesPlanCode[0]["RatePlan_Id"]);

                                                        DataRow[] drRatePlanlnclusion = SaltoursResponse.Tables["RatePlanInclusions"].Select("RatePlan_Id=" + RatePlan_Id + "");
                                                        int TaxInclusive_Id = Convert.ToInt32(drRatePlanlnclusion[0]["RatePlanInclusions_Id"]);

                                                        string Code = "";
                                                        string currencycode = "";
                                                        double FeeAmount = 0;
                                                        string ChargeUnit = "";
                                                        string ChargeFrequency = "";
                                                        string FeesEss = "";
                                                        string EssentialInformation = "";
                                                        if (SaltoursResponse.Tables.Contains("Fees"))
                                                        {
                                                            DataRow[] drFees = SaltoursResponse.Tables["Fees"].Select("Rate_Id=" + Rate_Id + "");
                                                            if (drFees.Length > 0)
                                                            {
                                                                int feesId = Convert.ToInt32(drFees[0]["Fees_Id"]);

                                                                DataRow[] drFee = SaltoursResponse.Tables["Fee"].Select("Fees_Id=" + feesId + "");

                                                                for (int fee = 0; fee < drFee.Length; fee++)
                                                                {
                                                                    bool MandatoryIndicator = Convert.ToBoolean(drFee[fee]["MandatoryIndicator"]);
                                                                    if (MandatoryIndicator == true)
                                                                    {
                                                                        Code = Convert.ToString(drFee[fee]["Code"]);
                                                                        currencycode = Convert.ToString(drFee[fee]["CurrencyCode"]);
                                                                        FeeAmount = Convert.ToDouble(drFee[fee]["Amount"]);
                                                                        ChargeUnit = Convert.ToString(drFee[fee]["ChargeUnit"]);
                                                                        ChargeFrequency = Convert.ToString(drFee[fee]["ChargeFrequency"]);

                                                                        FeesEss = Code + " " + FeeAmount + " " + currencycode + " " + "pay at hotel" + " " + ChargeUnit + " " + ChargeFrequency;
                                                                    }
                                                                    EssentialInformation = EssentialInformation + "<br>" + FeesEss;
                                                                }
                                                            }
                                                        }

                                                        DataRow[] drRatePlanlnclusionDesciption = SaltoursResponse.Tables["RatePlanInclusionDesciption"].Select("RatePlanInclusions_Id=" + TaxInclusive_Id + "");
                                                        Text_Enss = Convert.ToString(drRatePlanlnclusionDesciption[0]["Text"]);

                                                        #region Meals

                                                        string MealName = "Room Only";
                                                        int MealsId = 0;
                                                        string MealDescription = "";
                                                        int MealID = 0;
                                                        string Description = "";
                                                        if (SaltoursResponse.Tables.Contains("TPA_Extensions"))
                                                        {
                                                            DataRow[] drTPA_Extensions = SaltoursResponse.Tables["TPA_Extensions"].Select("Rate_Id=" + Rate_Id + "");
                                                            if (drTPA_Extensions.Length > 0)
                                                            {
                                                                string TPA_ExtensionsId = Convert.ToString(drTPA_Extensions[0]["TPA_Extensions_Id"]);

                                                                DataRow[] drMeals = SaltoursResponse.Tables["Meals"].Select("TPA_Extensions_Id=" + TPA_ExtensionsId + "");
                                                                MealsId = Convert.ToInt32(drMeals[0]["Meals_Id"]);

                                                                DataRow[] drMeal = SaltoursResponse.Tables["Meal"].Select("Meals_Id=" + MealsId + "");
                                                                MealName = Convert.ToString(drMeal[0]["Name"]);

                                                                if (SaltoursResponse.Tables.Contains("Meal"))
                                                                {
                                                                    if (SaltoursResponse.Tables["Meal"].Columns.Contains("Meal_Id"))
                                                                    {
                                                                        MealID = Convert.ToInt32(drMeal[0]["Meal_Id"]);

                                                                        if (SaltoursResponse.Tables.Contains("MealDescription"))
                                                                        {
                                                                            DataRow[] drMealDescription = SaltoursResponse.Tables["MealDescription"].Select("Meal_Id=" + MealID + "");

                                                                            for (int MD = 0; MD < drMealDescription.Length; MD++)
                                                                            {
                                                                                MealDescription = Convert.ToString(drMealDescription[MD]["Text"]);
                                                                            }
                                                                        }
                                                                    }
                                                                }
                                                                Description = MealDescription;
                                                            }

                                                        }

                                                        Text_Enss = Text_Enss + "<br>" + EssentialInformation + "<b><br>" + Description + "</br>" + SpeDesText;

                                                        #endregion

                                                        DataRow[] drBasicPropertyInfo = SaltoursResponse.Tables["BasicPropertyInfo"].Select("RoomStay_Id=" + RoomStay_Id + "");
                                                        string HotelCode1 = Convert.ToString(drBasicPropertyInfo[0]["HotelCode"]);
                                                        string HotelName = Convert.ToString(drBasicPropertyInfo[0]["HotelName"]);

                                                        if (CurrencyCode.ToLower() != commonSearch.tokenData.SiteCurrency.ToLower())
                                                        {

                                                            double CurrencyFactor1 = _commonMethod.GetROEByCurrency(Currency, dtROE);
                                                            AmountAfterTax = AmountAfterTax * CurrencyFactor1;
                                                        }

                                                        string RoomTypeId = RatesPlanCode + "$$$" + RatePlan_Id + "$$$" + RoomTypeCode + "$$$" + NumberOfUnits + "$$$" + CurrencyCode + "$$$" + HotelCode + "$$$" + HotelName + "$$$" + AmountAfterTax + "$$$" + Nationality;

                                                        Adult = Convert.ToInt32(SearchParam.Rooms[RoomNo - 1].NoofAdults);
                                                        Child = Convert.ToInt32(SearchParam.Rooms[RoomNo - 1].NoofChild);

                                                        double Night_Price = 0.0;

                                                        double VatOnPerNight = 0;

                                                        #region RateBrekUp

                                                        //for (int m = 0; m < SearchParam.NoofNights; m++)
                                                        //{
                                                        //    DateTime dtCheckInDateForRate = DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null);
                                                        //    string rateWeekColumn = Convert.ToString(dtCheckInDateForRate.Date.AddDays(m).DayOfWeek).Substring(0, 3);

                                                        //    Night_Price = AmountAfterTax / SearchParam.NoofNights;

                                                        //    #region Vat
                                                        //    Night_Price = Common.ProcessHotelRateBreakupPrice_WithMarkup(1, SearchParam.CityID, SearchParam.CountryId, Night_Price, B2CMarkup, IsParentId, isPerce, AgentPercentage, AgentMarkupAmount, SubAgentMarkup, ref VatOnPerNight);
                                                        //    #endregion
                                                        //    //VatonfirstnightPrice += VatOnPerNight;
                                                        //    DataRow drRateBreackUp = DtSaltoursRoomRateBreackUp.NewRow();
                                                        //    drRateBreackUp["SupplierName"] = Provider;
                                                        //    drRateBreackUp["OwnsystemHotelId"] = OwnsystemHotelId;
                                                        //    drRateBreackUp["ContractID"] = 0;
                                                        //    drRateBreackUp["RoomNo"] = RoomNo;
                                                        //    drRateBreackUp["RoomTypeId"] = RoomTypeId;
                                                        //    drRateBreackUp["MealId"] = MealName;
                                                        //    drRateBreackUp["Meal"] = MealName;
                                                        //    //string rateWeekColumn = Convert.ToString(dtCheckInDate.Date.AddDays(m).DayOfWeek).Substring(0, 3);
                                                        //    drRateBreackUp["DateOfWeek"] = rateWeekColumn;
                                                        //    //DateTime rateDate = dtCheckInDate.Date.AddDays(m);
                                                        //    drRateBreackUp["Date"] = dtCheckInDateForRate.Date.AddDays(m).Date;// rateDate;
                                                        //    drRateBreackUp["Rate"] = Night_Price;
                                                        //    drRateBreackUp["OrderNo"] = 0;
                                                        //    drRateBreackUp["RoomTypeName"] = Room_Text;
                                                        //    drRateBreackUp["DateOfWeek"] = rateWeekColumn;
                                                        //    drRateBreackUp["BookingStatus"] = "Available";
                                                        //    drRateBreackUp["BoardTypeID"] = MealName;
                                                        //    drRateBreackUp["VatdiffAmount"] = VatOnPerNight;
                                                        //    drRateBreackUp["RateType"] = Room_Text;
                                                        //    drRateBreackUp["SupplierCurrency"] = TotalCurrency;
                                                        //    drRateBreackUp["BuyingPrice"] = Night_Price / CurrencyFactor;
                                                        //    DtSaltoursRoomRateBreackUp.Rows.Add(drRateBreackUp);
                                                        //}
                                                        #endregion


                                                        #region Room
                                                        //for (int a = 0; a < objSearPera.Rooms.Count; a++)
                                                        //{
                                                        DataRow drRoom = DtSaltoursHotelRoom.NewRow();
                                                        drRoom["OwnsystemHotelId"] = OwnsystemHotelId;
                                                        drRoom["RoomTypeCode"] = RatesPlanCode;
                                                        drRoom["BoardType"] = MealName;

                                                        #region Vat
                                                        double itemprice = AmountAfterTax;
                                                        double Factor = 1;// change  here.
                                                                          //double ItemPrice = Convert.ToDouble(AmountAfterTax);

                                                        _markup.ProcessHotelRoom_WithMarkup(Factor, Currency, true, false, SearchParam.NoofNights, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), SearchParam.CheckInDate, SearchParam.CheckOutDate, ref itemprice, ref drRoom, isPerce, AgentPercentage, AgentMarkupAmount, SubAgentMarkup, B2CMarkup, IsParentId);
                                                        #endregion

                                                        drRoom["MealName"] = MealName;
                                                        drRoom["MealId"] = MealName;
                                                        drRoom["Type"] = "";
                                                        drRoom["Board"] = MealName;
                                                        drRoom["BookingStatus"] = "Available";
                                                        drRoom["UniqueNo"] = Guid.NewGuid().ToString();
                                                        drRoom["ContractID"] = 0;

                                                        drRoom["RoomNo"] = RoomNo;
                                                        drRoom["BoardTypeID"] = MealName;
                                                        drRoom["AdultsOccupancy"] = Adult;
                                                        drRoom["ChildOccupancy"] = Child;
                                                        drRoom["RoomSequenceNo"] = "";
                                                        drRoom["EssentialInformation"] = Text_Enss;
                                                        drRoom["Characteristic"] = "";
                                                        drRoom["SupplierName"] = Provider;

                                                        drRoom["OfferCode"] = "";
                                                        drRoom["OfferDescription"] = "";
                                                        drRoom["RoomTypeName"] = Room_Text;
                                                        drRoom["RoomTypeId"] = RoomTypeId;

                                                        DtSaltoursHotelRoom.Rows.Add(drRoom);

                                                        #endregion
                                                        #region Cancellation Policy

                                                        try
                                                        {
                                                            string Text = "";
                                                            DataRow[] drCancelPolicies = SaltoursResponse.Tables["CancelPolicies"].Select("Rate_Id=" + Rate_Id + "");
                                                            CancelPolicies_Id = Convert.ToInt32(drCancelPolicies[0]["CancelPolicies_Id"]);

                                                            DataRow[] drCancelPenalty = SaltoursResponse.Tables["CancelPenalty"].Select("CancelPolicies_Id=" + CancelPolicies_Id + "");
                                                            int CancelPenalty_Id = Convert.ToInt32(drCancelPenalty[0]["CancelPenalty_Id"]);

                                                            for (int CP = 0; CP < drCancelPenalty.Length; CP++)
                                                            {
                                                                DataRow[] drDeadline = SaltoursResponse.Tables["Deadline"].Select("CancelPenalty_Id=" + CancelPenalty_Id + "");
                                                                string OffsetDropTime = Convert.ToString(drDeadline[CP]["OffsetDropTime"]);
                                                                int OffsetUnitMultiplier = Convert.ToInt32(drDeadline[CP]["OffsetUnitMultiplier"]);
                                                                string OffsetTimeUnit = Convert.ToString(drDeadline[CP]["OffsetTimeUnit"]);

                                                                string Offset = ""; // ticket no 42909
                                                                if (CP == 0)
                                                                {
                                                                    Offset = Convert.ToString(drDeadline[0]["OffsetUnitMultiplier"]);
                                                                }


                                                                DataRow[] drAmountPercent = SaltoursResponse.Tables["AmountPercent"].Select("CancelPenalty_Id=" + CancelPenalty_Id + "");
                                                                double Amount = Convert.ToDouble(drAmountPercent[0]["Amount"]);

                                                                DataRow[] drPenaltyDescription = SaltoursResponse.Tables["PenaltyDescription"].Select("CancelPenalty_Id=" + CancelPenalty_Id + "");

                                                                Text = Convert.ToString(drPenaltyDescription[0]["Text"]);

                                                                string uniqid = Guid.NewGuid().ToString();
                                                                bool Istrue = true;

                                                                #region Vat
                                                                double VatOnPolicy = 0;
                                                                double MainAgentCharge = AmountAfterTax;
                                                                double CompanyChargeAmount = AmountAfterTax;
                                                                #endregion

                                                                #region Vat
                                                                double CancelCharge = _markup.ProcessHotelCancelationPrice_WithMarkup(ref MainAgentCharge, SearchParam.NoofNights, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), AmountAfterTax, isPerce, B2CMarkup, AgentPercentage, AgentMarkupAmount, IsParentId, SubAgentMarkup, ref VatOnPolicy);
                                                                #endregion

                                                                DateTime CancellationDate = DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", CultureInfo.InvariantCulture).Date;

                                                                string ownsystemid = Convert.ToString(OwnsystemHotelId);
                                                                //string roomno = RoomNo;
                                                                double CompanyBuy = AmountAfterTax;// CancellationPrice;
                                                                DateTime curDate = _commonMethod.GetTimeZoneDate(SearchParam.CurrentDate);
                                                                double Penalty = Convert.ToDouble(CompanyBuy);
                                                                DateTime MainDate = new DateTime();
                                                                DateTime StartDate = MainDate;

                                                                DateTime CancelDate = CancellationDate.Date.AddDays(-OffsetUnitMultiplier);

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

                                                                if (NowDate < CancelDate)
                                                                {
                                                                    if (Istrue == true)
                                                                    {
                                                                        double zero = 0;
                                                                        dtSaltoursCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref zero, dtSaltoursCancellation.NewRow(), CurrencyFactor, Currency, Provider, ownsystemid, Convert.ToString(RoomNo),
                                                                                                      Convert.ToString(RoomTypeId), Room_Text, MealName, Convert.ToDateTime(curDate), CancelDate.AddDays(-1), 0, 0, "0", VatOnPolicy, uniqid, "NO", Offset, "" != string.Empty ? "" : "", "", Convert.ToString(GiataId)));
                                                                        dtSaltoursCancellation.AcceptChanges();
                                                                        Istrue = false;
                                                                    }

                                                                    dtSaltoursCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref CompanyChargeAmount, dtSaltoursCancellation.NewRow(), CurrencyFactor, Currency, Provider, ownsystemid, Convert.ToString(RoomNo),
                                                                                                   Convert.ToString(RoomTypeId), Room_Text, MealName, Convert.ToDateTime(CancelDate), DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null), MainAgentCharge, itemprice, "0", VatOnPolicy, uniqid, "NO", Offset, "" != string.Empty ? "" : "", "", Convert.ToString(GiataId)));
                                                                    dtSaltoursCancellation.AcceptChanges();
                                                                }
                                                                else
                                                                {
                                                                    dtSaltoursCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref CompanyChargeAmount, dtSaltoursCancellation.NewRow(), CurrencyFactor, Currency, Provider, ownsystemid, Convert.ToString(RoomNo),
                                                                                                   Convert.ToString(RoomTypeId), Room_Text, MealName, curDate, DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null), MainAgentCharge, itemprice, "0", VatOnPolicy, uniqid, "Yes", Offset, "" != string.Empty ? "" : "", "", Convert.ToString(GiataId)));
                                                                    //(ref double CompanyChargeAmount, DataRow drHotelCancellation, double CurrencyFactor, string Currency, string SupplierName, string ownsystemhotelId, string RoomNo, string RoomTypeId,
                                                                    //string RoomTypeName, string MealId, DateTime FromDate, DateTime ToDate, double CompanyCancellationPrice, double AgentCancellationPrice, string BoardTypeID, double VatOnPolicy, string RoomSequenceNo = "", string WithInCancellationDeadLine = "", string PurchaseToken = "", string SPUI = "", string CartID = "", string GIITACode = "")
                                                                    dtSaltoursCancellation.AcceptChanges();
                                                                }

                                                                break;

                                                            }
                                                        }
                                                        catch (Exception ex)
                                                        {
                                                             await _exception.Error(ex, "true");
                                                            return options;
                                                        }

                                                    }
                                                }
                                            }
                                            #endregion
                                            decimal LowestPrice = Convert.ToDecimal(MinPrice).CalculatePrice(commonSearch.tokenData, commonSearch.advancedOptions, item.CurrencyCode);
                                            Options ops = (await _supplierRes.GetCommonRoomForSuppliers(GiataId, commonSearch.tokenData, commonSearch.advancedOptions, Convert.ToDouble(LowestPrice), drXmlHotel, DtSaltoursHotelRoom, dtSaltoursCancellation, DtSaltoursRoomRateBreackUp, SearchParam));
                                            if (ops != null)
                                                options.Add(ops);
                                        }
                                        #endregion
                                    }
                                }
                                #endregion







                            }

                            #endregion
                        }

                        //return options;

                    }
                }
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
                //return options;
            }
            //});
            finally
            {
                #region ResProcessTimer
                try
                {
                    if (SearchParam != null && ResProccessTimerLogActive == "1" && timer_room_Saltours.ElapsedMilliseconds > Convert.ToInt32(ResProccessTimeValueforSaveLog))
                    {
                        timer_room_Saltours.Stop();
                        SearchParam.SupplierTime += ",|Finish:Saltours Process Response  " + "|Time :" + timer_room_Saltours.Elapsed.ToString();
                        _= Task.Run(() => _commonMethod.FireAway_SQL_ResPorcessTimeLog("", timer_room_Saltours.Elapsed.ToString(), item.Supplier, SearchParam.SearchKey, 1, SearchParam.AgentID));
                    }
                }
                catch (Exception x)
                {
                    _exception.Error(x);
                }
                #endregion
            }
            return options;
        }

        public async Task<PriceCheckDto> SaltoursPriceCheck(DataSet dsCartDetails, DataRow[] saltoursDetails, int j, double FinalPrice,  double CurrencyFactorsaltours, string UniqueNo, string AgentUniqueNo, double ResPrice, string SupplierName, int i, string Nationality, double saltoursCurrency,int AgentId,TokenData tokenData)
        {
            PriceCheckDto priceCheckDto = new PriceCheckDto();

            string CancellationPolicyRQ = "";
            string responsetype = "Success";
            string ResponseData = "";
            string time = "";
            //string request = "";
            string Error = "";
            try
            {
                string username = Convert.ToString(saltoursDetails[0]["UserName"]);
                string password = Convert.ToString(saltoursDetails[0]["Password"]);
                string Url1 = Convert.ToString(saltoursDetails[0]["WebSiteURL"]);
                string roomtypeid = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomTypeId"]);
                string[] spitid1 = roomtypeid.Split(new String[] { "$$$" }, StringSplitOptions.None);
                string CheckIn = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["CheckInDate"]);
                CheckIn = CheckIn.Substring(0, 4) + "-" + CheckIn.Substring(4, 2) + "-" + CheckIn.Substring(6, 2);
                string CheckOut = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["CheckOutDate"]);
                CheckOut = CheckOut.Substring(0, 4) + "-" + CheckOut.Substring(4, 2) + "-" + CheckOut.Substring(6, 2);
                double CurrencyFactorsaltour = 1;
                string URL = Url1 + Convert.ToString(SalyoursInfo.SaltoursSearchURL);

                string request = SaltoursPriceCheckRequest(roomtypeid, CheckIn, CheckOut, dsCartDetails, saltoursCurrency, username, password, Nationality, j, i);

                _commonMethod.ThirdPartyBookingHistory(Convert.ToString(UniqueNo), Convert.ToString(AgentUniqueNo), request, "-", "Before Block Request hit Price check", Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["SupplierName"]), "Request Confirm j=" + j + "", "-", "WithinEarth");
                #region Price Check RS

                if (request != "")
                {

                    System.Diagnostics.Stopwatch timerroomprocess = new System.Diagnostics.Stopwatch();
                    timerroomprocess.Start();

                    ResponseDto responseDto = await Get_Saltours_Response(request, URL, 120000, "saltours");
                    ResponseData = responseDto.Response;
                    Error = responseDto.Error;


                    timerroomprocess.Stop();
                    time = timerroomprocess.Elapsed.ToString();

                    if (ResponseData == "" || ResponseData == null)
                    {
                        try
                        {
                            responsetype = "GeneralError";
                            string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, request, ResponseData, "purchaseToken");
                        }
                        catch (Exception e)
                        {
                            _ = _exception.Error(e);
                        }
                        priceCheckDto.Flag = 2;
                        return priceCheckDto;
                    }
                    _commonMethod.ThirdPartyBookingHistory(Convert.ToString(UniqueNo), Convert.ToString(AgentUniqueNo), request, ResponseData, "After Block Request hit Price check", Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["SupplierName"]), "Request Confirm j=" + j + "", "-", "WithinEarth"); 

                    DataSet Response = _commonMethod.GetDataSetFromXMLString(ResponseData);

                    #region Price Check Logic
                    //if (Response.Tables.Contains("Errors"))
                    //{
                    //    try
                    //    {
                    //        responsetype = "GeneralError";
                    //        int AgentId = Convert.ToInt32(Common.getSession("UserId") == "" ? "0" : Common.getSession("UserId"));
                    //        string abc = Common.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, request, ResponseData, Error);
                    //    }
                    //    catch (Exception e)
                    //    {
                    //        ExceptionManager.LogException(e);
                    //    }
                    //    //return false;
                    //}
                    //if (Response.Tables["Price"].Rows.Count == 0)
                    //{
                    //    try
                    //    {
                    //        responsetype = "GeneralError";
                    //        int AgentId = Convert.ToInt32(Common.getSession("UserId") == "" ? "0" : Common.getSession("UserId"));
                    //        string abc = Common.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, request, ResponseData, Error);
                    //    }
                    //    catch (Exception e)
                    //    {
                    //        ExceptionManager.LogException(e);
                    //    }
                    //    //return false;
                    //}
                    //else
                    //{
                    //    if (Response.Tables.Contains("HotelDTO"))
                    //    {

                    //        DataRow[] drPurchaseToken = (Response.Tables["HotelDTO"].Select("HotelDTO_Id='" + 0 + "'"));
                    //        //PreBookingToken = Convert.ToString(drPurchaseToken[0]["PurchaseToken"]);
                    //        double FinalPriceMain = 0;
                    //        for (int p = 0; p < Response.Tables["Price"].Rows.Count; p++)
                    //        {
                    //            DataRow[] drPreBookResponse = (Response.Tables["Price"].Select("HotelRoomDTO_Id='" + p + "'"));
                    //            string FinalPrice1 = Convert.ToString(drPreBookResponse[0]["Amount"]).Replace(",", ".");
                    //            string Currency = Convert.ToString(drPreBookResponse[0]["Currency"]);
                    //            double FinalPriceTemp = Convert.ToDouble(FinalPrice1);

                    //            if (Currency.ToLower() != "usd")
                    //            {
                    //                DataTable dtInrCurrency1 = Common.GetCountryCurrency(Currency);
                    //                double CurrencyFactorAdonis1 = Convert.ToDouble(dtInrCurrency1.Rows[0]["AmtToExch"]);
                    //                FinalPriceTemp = FinalPriceTemp * CurrencyFactorAdonis1;

                    //            }
                    //            else
                    //                FinalPriceTemp = FinalPriceTemp * CurrencyFactorsaltours;
                    //            FinalPriceMain += FinalPriceTemp;
                    //        }

                    //        FinalPrice = Convert.ToDouble(FinalPriceMain);


                    //        FinalPrice = Convert.ToDouble(Math.Round(FinalPrice, 2));
                    //        if (Convert.ToDouble(Math.Round(FinalPrice.TruncateDecimalPlaces(0))) > Math.Round(ResPrice.TruncateDecimalPlaces(0)))
                    //        {
                    //            try
                    //            {
                    //                responsetype = "GeneralError";
                    //                int AgentId = Convert.ToInt32(Common.getSession("UserId") == "" ? "0" : Common.getSession("UserId"));
                    //                string abc = Common.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, request, ResponseData, Error);
                    //            }
                    //            catch (Exception e)
                    //            {
                    //                ExceptionManager.LogException(e);
                    //            }
                    //            //return true;
                    //        }
                    //    }
                    //}
                    #endregion

                    #region Price Check Logic Saltours
                    if (Response.Tables.Contains("Errors"))
                    {
                        try
                        {
                            responsetype = "GeneralError";
                            string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, request, ResponseData, Error);
                        }
                        catch (Exception e)
                        {
                            _ = _exception.Error(e);
                        }
                        priceCheckDto.Flag = 2;
                        return priceCheckDto;
                    }
                    if (Response.Tables["RoomStay"].Rows.Count == 0)
                    {
                        try
                        {
                            responsetype = "GeneralError";
                            string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, request, ResponseData, Error);
                        }
                        catch (Exception e)
                        {
                            _ = _exception.Error(e);
                        }
                        priceCheckDto.Flag = 2;
                        return priceCheckDto;
                    }
                    else
                    {
                        if (Response.Tables["RoomStay"] != null)
                        {
                            for (int h = 0; h <= Response.Tables["RoomStay"].Rows.Count; h++)
                            {
                                string Roomstay_id = Convert.ToString(Response.Tables["RoomStay"].Rows[0]["RoomStay_Id"]);
                                DataRow[] drRateplans = Response.Tables["RoomRates"].Select("RoomStay_Id=" + Roomstay_id + "");

                                string RoomRates = Convert.ToString(drRateplans[0]["RoomRates_Id"]);
                                DataRow[] drRatePlan = Response.Tables["RoomRate"].Select("RoomRate_Id=" + RoomRates + "");
                                for (int P = 0; P < drRatePlan.Length; P++)
                                {
                                    string RoomTypeCode = Convert.ToString(drRatePlan[P]["RoomTypeCode"]);
                                    string RatePlanCode = Convert.ToString(drRatePlan[P]["RatePlanCode"]);
                                    int roomrate_id = Convert.ToInt32(drRatePlan[P]["RoomRate_Id"]);

                                    DataRow[] drRoomType = Response.Tables["RoomType"].Select("RoomTypeCode='" + RoomTypeCode + "'");
                                    int RoomType_Id = Convert.ToInt32(drRoomType[0]["RoomType_Id"]);

                                    DataRow[] drRate_Id = Response.Tables["Rates"].Select("RoomRate_Id=" + roomrate_id + "");
                                    int Rates_Id = Convert.ToInt32(drRate_Id[0]["Rates_Id"]);

                                    DataRow[] drRate = Response.Tables["Rate"].Select("Rates_Id=" + Rates_Id + "");
                                    int Rate_Id = Convert.ToInt32(drRate[0]["Rate_Id"]);

                                    DataRow[] drBase = Response.Tables["Base"].Select("Rate_Id=" + Rate_Id + "");
                                    double AmountAfterTax = Convert.ToDouble(drBase[0]["AmountAfterTax"]);

                                    string CurrencyCode = Convert.ToString(drBase[0]["CurrencyCode"]);
                                    if (CurrencyCode.ToLower() != tokenData.SiteCurrency.ToLower())
                                    {
                                        DataTable dtInrCurrency1 = _commonMethod.GetCountryCurrency(CurrencyCode);
                                        CurrencyFactorsaltour = Convert.ToDouble(dtInrCurrency1.Rows[0]["AmtToExch"]);
                                    }

                                    DataRow[] drBasicPropertyInfo = Response.Tables["BasicPropertyInfo"].Select("RoomStay_Id=" + Roomstay_id + "");
                                    for (int BP = 0; BP < drBasicPropertyInfo.Length; BP++)
                                    {
                                        string HotelCode = Convert.ToString(drBasicPropertyInfo[BP]["HotelCode"]);
                                    }

                                    if (drBase.Length > 0)
                                    {
                                        FinalPrice = Convert.ToDouble(drBase[0]["AmountAfterTax"]);
                                        FinalPrice = FinalPrice * CurrencyFactorsaltour;
                                        FinalPrice = Convert.ToDouble(Math.Round(FinalPrice, 2));
                                        //FinalPrice = Convert.ToDouble(Math.Round(FinalPrice, 2));
                                        if (Convert.ToDouble(Math.Round(FinalPrice, 2)) > ResPrice)
                                        {
                                            priceCheckDto.Flag = 1;
                                            priceCheckDto.FinalPrice = FinalPrice;
                                            return priceCheckDto;

                                        }
                                    }

                                }
                            }
                            #endregion
                        }
                        else
                        {
                            priceCheckDto.Flag = 2;
                            return priceCheckDto;
                        }
                    }
                }
                else
                {
                    try
                    {
                        responsetype = "GeneralError";
                        string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, request, ResponseData, Error);
                    }
                    catch (Exception e)
                    {
                        _ = _exception.Error(e);
                    }
                    priceCheckDto.Flag = 2;
                    return priceCheckDto;
                }
            }
            #endregion



            catch (Exception e)
            {
                _ = _exception.Error(e);
                priceCheckDto.Flag = 2;
                return priceCheckDto;
            }

            priceCheckDto.Flag = 0;
            return priceCheckDto;
        }

        public string SaltoursPriceCheckRequest(string roomtypeid, string checkIn, string checkout, DataSet dsCartDetails, double saltoursCurrency, string username, string password, string Nationality, int j, int i)
        {
            try
            {
                string[] spitid1 = roomtypeid.Split(new String[] { "$$$" }, StringSplitOptions.None);
                string Hotelcode = spitid1[5];
                int UserCartId = Convert.ToInt32(dsCartDetails.Tables[0].Rows[i]["UserCartID"]);
                //string Hotelcode = spitid1[5];
                //string Nationality = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["NationalityName"]);
                try
                {
                    #region Req
                    StringBuilder SRQ = new StringBuilder();
                    SRQ.Append("<Envelope xmlns='http://schemas.xmlsoap.org/soap/envelope/' xmlns:xsd='http://www.w3.org/2001/XMLSchema' xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance'>");
                    SRQ.Append("<Header>");
                    SRQ.Append("<Security xmlns= 'http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd'>");
                    SRQ.Append("<UsernameToken>");
                    //SRQ.Append("<Username>" + saltours_User_Name + "</Username>");
                    SRQ.Append("<Username>" + username + "</Username>");
                    SRQ.Append("<Password>" + password + "</Password>");
                    SRQ.Append("</UsernameToken>");
                    SRQ.Append("</Security>");
                    SRQ.Append("</Header>");
                    SRQ.Append("<Body>");
                    SRQ.Append("<OTA_HotelAvailRQ BestOnly=\"\"  RateDetailsInd=\"\"  ResponseFormat=\"0\"  Currency=\"" + saltoursCurrency + "\">");
                    SRQ.Append("<POS xmlns=\'http://www.opentravel.org/OTA/2003/05'>");
                    SRQ.Append("<Source ISOCountry=\"" + Nationality + "\" />");
                    SRQ.Append("</POS>");
                    SRQ.Append("<AvailRequestSegments xmlns='http://www.opentravel.org/OTA/2003/05'>");
                    SRQ.Append("<AvailRequestSegment xmlns=\"'http://www.opentravel.org/OTA/2003/05'\" ResponseType=\"PropertyRateList\">");
                    SRQ.Append("<StayDateRange xmlns=\"'http://www.opentravel.org/OTA/2003/05'\" End=\"" + checkout + "\" Start= \"" + checkIn + "\" />");
                    SRQ.Append("<RoomStayCandidates>");

                    DataRow[] drUserCartCount = dsCartDetails.Tables[1].Select("UserCartId='" + UserCartId + "'");
                    //for (int ser = 0; ser < drUserCartCount.Length; ser++)
                    int ser = j;
                    {
                        SRQ.Append("<RoomStayCandidate Quantity=\"1\">");
                        SRQ.Append("<GuestCounts>");

                        int adults = Convert.ToInt32(drUserCartCount[ser]["NoOfAdults"]);
                        string ChildAge = Convert.ToString(drUserCartCount[ser]["ChildAge1"]);
                        string ChildAge2 = Convert.ToString(drUserCartCount[ser]["ChildAge2"]);
                        int NoofChild = Convert.ToInt32(drUserCartCount[ser]["NoOfChild"]);

                        SRQ.Append("<GuestCount Count=\"" + adults + "\"  AgeQualifyingCode =\"10\"/>");
                        //if (NoofChild > 0)
                        //{
                        //    switch (Convert.ToInt32(drUserCartCount[ser]["NoOfChild"]))
                        //    {
                        //        case 1:
                        //            ChildAge = Convert.ToString(drUserCartCount[ser]["ChildAge1"]);
                        //            SRQ.Append("<GuestCount Count=\"1\"  AgeQualifyingCode =\"8\" Age=\"" + ChildAge + "\"/>");
                        //            break;
                        //        case 2:
                        //            ChildAge = Convert.ToString(drUserCartCount[ser]["ChildAge1"]);
                        //            ChildAge2 = Convert.ToString(drUserCartCount[ser]["ChildAge2"]);
                        //            SRQ.Append("<GuestCount Count=\"1\"  AgeQualifyingCode =\"8\" Age=\"" + ChildAge + "\"/>");
                        //            SRQ.Append("<GuestCount Count=\"1\"  AgeQualifyingCode =\"8\" Age=\"" + ChildAge2 + "\"/>");
                        //            break;
                        //    }
                        //}
                        switch (Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[ser]["NoOfChild"]))
                        {
                            case 1:
                                if (Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[ser]["ChildAge1"]) < 2)
                                    SRQ.Append("<Occupancy AgeQualifyingCode=\"" + 7 + "\"" + " " + "MinOccupancy=\"" + 1 + "\" Age=\"" + Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[ser]["ChildAge1"]) + "\" />");
                                else
                                    SRQ.Append("<Occupancy AgeQualifyingCode=\"" + 8 + "\"" + " " + "MinOccupancy=\"" + 1 + "\" Age=\"" + Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[ser]["ChildAge1"]) + "\" />");
                                break;
                            case 2:
                                if (Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[ser]["ChildAge1"]) > 1 && Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[ser]["ChildAge2"]) > 1)
                                {
                                    SRQ.Append("<Occupancy AgeQualifyingCode=\"" + 8 + "\"" + " " + "MinOccupancy=\"" + 1 + "\" Age=\"" + Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[ser]["ChildAge1"]) + "\" />");
                                    SRQ.Append("<Occupancy AgeQualifyingCode=\"" + 8 + "\"" + " " + "MinOccupancy=\"" + 1 + "\" Age=\"" + Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[ser]["ChildAge2"]) + "\" />");
                                }
                                else if (Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[ser]["ChildAge1"]) > 1 && Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[ser]["ChildAge2"]) < 2)
                                {
                                    SRQ.Append("<Occupancy AgeQualifyingCode=\"" + 8 + "\"" + " " + "MinOccupancy=\"" + 1 + "\" Age=\"" + Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[ser]["ChildAge1"]) + "\" />");
                                    SRQ.Append("<Occupancy AgeQualifyingCode=\"" + 7 + "\"" + " " + "MinOccupancy=\"" + 1 + "\" Age=\"" + Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[ser]["ChildAge2"]) + "\" />");
                                }
                                else if (Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[ser]["ChildAge1"]) < 2 && Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[ser]["ChildAge2"]) > 1)
                                {
                                    SRQ.Append("<Occupancy AgeQualifyingCode=\"" + 7 + "\"" + " " + "MinOccupancy=\"" + 1 + "\" Age=\"" + Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[ser]["ChildAge1"]) + "\" />");
                                    SRQ.Append("<Occupancy AgeQualifyingCode=\"" + 8 + "\"" + " " + "MinOccupancy=\"" + 1 + "\" Age=\"" + Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[ser]["ChildAge2"]) + "\" />");
                                }
                                else
                                {
                                    SRQ.Append("<Occupancy AgeQualifyingCode=\"" + 7 + "\"" + " " + "MinOccupancy=\"" + 2 + "\" Age=\"" + Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[ser]["ChildAge1"]) + "\" />");
                                }
                                break;
                        }
                        SRQ.Append("</GuestCounts>");
                        SRQ.Append("</RoomStayCandidate>");
                    }
                    SRQ.Append("</RoomStayCandidates>");
                    SRQ.Append("<HotelSearchCriteria>");
                    SRQ.Append("<Criterion>");

                    if (Hotelcode.Trim() != string.Empty)
                    {
                        string[] HotelIds = Convert.ToString(Hotelcode).Split(',');
                        StringBuilder ItemCodeString = new StringBuilder();
                        for (int htid = 0; htid < HotelIds.Length; htid++)
                        {
                            SRQ.Append("<HotelRef HotelCode=\"" + HotelIds[htid] + "\" />");
                        }
                    }
                    //else if (CityCode.Trim() != string.Empty)
                    //{
                    //    SRQ.Append("<HotelRef HotelCityCode=\"" + CityCode + "\" />");
                    //}
                    SRQ.Append("</Criterion>");
                    SRQ.Append("</HotelSearchCriteria>");
                    SRQ.Append("</AvailRequestSegment>");
                    SRQ.Append("</AvailRequestSegments>");
                    SRQ.Append("</OTA_HotelAvailRQ>");
                    SRQ.Append("</Body>");
                    SRQ.Append("</Envelope>");
                    string SalToursString = SRQ.ToString();
                    return SalToursString;
                    #endregion
                }
                catch (Exception ex)
                {
                     _ = _exception.Error(ex);
                }
            }
            catch (Exception e)
            {
                _ = _exception.Error(e);
                return null;
            }
            return null;
        }

        public async Task<ResponseDto> Get_Saltours_Response(string Request, string URL, Int32 TimeOut, string Provider = "")
        {
            ResponseDto responseDto = new ResponseDto();
            string Error = "";
            string strOutput = "";
            System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
            timer.Start();
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(URL);
                request.Method = "POST";
                request.Accept = "application/xml";

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
                    strOutput = await Reader.ReadToEndAsync();
                    responseStream.Close();
                    Reader.Close();
                    Response.Close();
                    timer.Stop();
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
                _= _exception.Error(ex);
                Error = ex.Message.ToString();
                responseDto.Response = ex.Message.ToString();
                responseDto.Error = Error;
                return responseDto;
            }

            responseDto.Response = strOutput;
            responseDto.Error = Error;
            return responseDto;
        }

        public string SaltoursBookingDetailRequest(string Password, string UserName, DataSet dsCartDetails, DataRow[] saltoursDetails, int i, int j, double resPrice, DataTable dtPassengers, ref string AgtRef, string CheckIn, string CheckOut, int UniqueNo, string AgentCode="")
        {
            #region Booking Request 
            try
            {
                string roomtypeid = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomTypeId"]);
                string[] spitid1 = roomtypeid.Split(new String[] { "$$$" }, StringSplitOptions.None);

                string RoomTypeCode = spitid1[2];
                string NumberOfUnits = spitid1[3];
                string HotelName = spitid1[6];
                string RatesPlanCode = spitid1[0];
                string nationality = spitid1[8];
                string CurrencyCode = spitid1[4];
                double AmountAfterTax = Convert.ToDouble(spitid1[7]);
                int adults = 0;
                string ChildAge = "";
                string ChildAge2 = "";
                int NoofChild = 0;
                string HotelCode = Convert.ToString(dsCartDetails.Tables[0].DefaultView[i]["XMLHotelId"]);
                string BirthDay1 = Convert.ToString(DateTime.Now.Date.AddYears(-30));
                DateTime Dt1 = (Convert.ToDateTime(BirthDay1));
                string ExternalReference = "Book_" + Convert.ToString(AgentCode + "_" + UniqueNo);
                int UserCartId = Convert.ToInt32(dsCartDetails.Tables[0].Rows[i]["UserCartId"]);
                //string Email = WebConfigurationManager.AppSettings["email"];
                string RoomNumber = "1";//Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomNumber"]);
                StringBuilder bookingreq = new StringBuilder();

                bookingreq.Append("<?xml version=\"1.0\"?>");
                bookingreq.Append("<Envelope xmlns='http://schemas.xmlsoap.org/soap/envelope/' xmlns:xsd='http://www.w3.org/2001/XMLSchema' xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance'>");
                bookingreq.Append("<Header>");
                bookingreq.Append("<Security xmlns='http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd'>");
                bookingreq.Append("<UsernameToken>");
                bookingreq.Append("<Username>" + UserName + "</Username>");
                bookingreq.Append("<Password>" + Password + "</Password>");
                bookingreq.Append("</UsernameToken>");
                bookingreq.Append("</Security>");
                bookingreq.Append("</Header>");
                bookingreq.Append("<Body>");
                bookingreq.Append("<OTA_HotelResRQ xmlns='http://schemas.xmlsoap.org/soap/envelope/' ResStatus=\"Initiate\"  TransactionStatusCode=\"Start\">");
                bookingreq.Append("<POS xmlns='http://www.opentravel.org/OTA/2003/05'>");
                bookingreq.Append("<Source ISOCountry=\"" + nationality + "\" />");
                bookingreq.Append("</POS>");
                bookingreq.Append("<HotelReservations xmlns='http://www.opentravel.org/OTA/2003/05'>");
                bookingreq.Append("<HotelReservation xmlns='http://www.opentravel.org/OTA/2003/05'>");
                bookingreq.Append("<UniqueID Type=\"14\"  Instance=\"PNR\" ID=\"" + ExternalReference + "\" />");
                bookingreq.Append("<RoomStays>");
                bookingreq.Append("<RoomStay>");
                bookingreq.Append("<RoomTypes>");
                bookingreq.Append("<RoomType RoomTypeCode=\"" + RoomTypeCode + "\" NumberOfUnits=\"" + NumberOfUnits + "\">");

                DataRow[] drUserCartCount = dsCartDetails.Tables[1].Select("UserCartId='" + UserCartId + "'");
                int ser = j;
                //for (int ser = 0; ser < drUserCartCount.Length; ser++)
                {
                    adults = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[ser]["NoOfAdults"]);//Convert.ToInt32(drUserCartCount[ser]["NoOfAdults"]);
                    ChildAge = Convert.ToString(dsCartDetails.Tables[1].DefaultView[ser]["ChildAge1"]);
                    ChildAge2 = Convert.ToString(dsCartDetails.Tables[1].DefaultView[ser]["ChildAge2"]);
                    NoofChild = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[ser]["NoOfChild"]);

                    bookingreq.Append("<Occupancy MinOccupancy=\"" + adults + "\"  AgeQualifyingCode =\"10\"/>");

                    switch (Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[ser]["NoOfChild"]))
                    {
                        case 1:
                            if (Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[ser]["ChildAge1"]) < 2)
                                bookingreq.Append("<Occupancy MinOccupancy=\"" + 1 + "\"" + " " + "AgeQualifyingCode=\"" + 7 + "\" />");
                            else
                                bookingreq.Append("<Occupancy MinOccupancy=\"" + 1 + "\"" + " " + "AgeQualifyingCode=\"" + 8 + "\" MinAge=\"" + Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[ser]["ChildAge1"]) + "\" />");
                            break;
                        case 2:
                            if (Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[ser]["ChildAge1"]) > 2 && Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[ser]["ChildAge2"]) > 2)
                            {
                                bookingreq.Append("<Occupancy MinOccupancy=\"" + 1 + "\"" + " " + "AgeQualifyingCode=\"" + 8 + "\" MinAge=\"" + Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[ser]["ChildAge1"]) + "\" />");
                                bookingreq.Append("<Occupancy MinOccupancy=\"" + 1 + "\"" + " " + "AgeQualifyingCode=\"" + 8 + "\" MinAge=\"" + Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[ser]["ChildAge2"]) + "\" />");
                            }
                            else if (Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[ser]["ChildAge1"]) > 2 && Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[ser]["ChildAge2"]) < 2)
                            {
                                bookingreq.Append("<Occupancy MinOccupancy=\"" + 1 + "\"" + " " + "AgeQualifyingCode=\"" + 8 + "\" MinAge=\"" + Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[ser]["ChildAge1"]) + "\" />");
                                bookingreq.Append("<Occupancy MinOccupancy=\"" + 1 + "\"" + " " + "AgeQualifyingCode=\"" + 7 + "\" />");
                            }
                            else if (Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[ser]["ChildAge1"]) < 2 && Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[ser]["ChildAge2"]) > 2)
                            {
                                bookingreq.Append("<Occupancy MinOccupancy=\"" + 1 + "\"" + " " + "AgeQualifyingCode=\"" + 7 + "\" />");
                                bookingreq.Append("<Occupancy MinOccupancy=\"" + 1 + "\"" + " " + "AgeQualifyingCode=\"" + 8 + "\" MinAge=\"" + Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[ser]["ChildAge2"]) + "\" />");
                            }
                            else
                            {
                                bookingreq.Append("<Occupancy MinOccupancy=\"" + 2 + "\"" + " " + "AgeQualifyingCode=\"" + 7 + "\" />");
                            }
                            break;
                    }
                }

                bookingreq.Append("<TPA_Extension>");
                //bookingreq.Append("<ResGuestRPH RPH=\"1\"/>");
                bookingreq.Append("<ResGuestRPH RPH=\"" + RoomNumber + "\" />");
                bookingreq.Append("</TPA_Extension>");
                bookingreq.Append(" </RoomType>");
                bookingreq.Append("</RoomTypes>");
                bookingreq.Append("<RoomRates>");
                bookingreq.Append("<RoomRate RoomTypeCode=\"" + RoomTypeCode + "\" RatePlanCode=\"" + RatesPlanCode + "\" >");
                bookingreq.Append("<Rates>");
                bookingreq.Append("<Rate>");
                bookingreq.Append("<Base CurrencyCode=\"" + CurrencyCode + "\" AmountAfterTax=\"" + AmountAfterTax + "\"/>");
                bookingreq.Append("</Rate>");
                bookingreq.Append("</Rates>");
                bookingreq.Append("</RoomRate>");
                bookingreq.Append("</RoomRates>");
                bookingreq.Append("<GuestCounts xmlns='http://www.opentravel.org/OTA/2003/05'>");
                DataRow[] drUserCartCount2 = dsCartDetails.Tables[1].Select("UserCartId='" + UserCartId + "'");
                int Guest = j;
                //for (int Guest = 0; Guest < drUserCartCount2.Length; Guest++)
                {

                    adults = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[Guest]["NoOfAdults"]);
                    ChildAge = Convert.ToString(dsCartDetails.Tables[1].DefaultView[Guest]["ChildAge1"]);
                    ChildAge2 = Convert.ToString(dsCartDetails.Tables[1].DefaultView[Guest]["ChildAge2"]);
                    NoofChild = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[Guest]["NoOfChild"]);

                    bookingreq.Append("<GuestCount AgeQualifyingCode=\"10\" Count=\"" + adults + "\"/>");


                    switch (Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[Guest]["NoOfChild"]))
                    {
                        case 1:
                            if (Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[Guest]["ChildAge1"]) < 2)
                                bookingreq.Append("<GuestCount AgeQualifyingCode=\"" + 7 + "\"" + " " + "Count=\"" + 1 + "\" />");
                            else
                                bookingreq.Append("<GuestCount AgeQualifyingCode=\"" + 8 + "\"" + " " + "Count=\"" + 1 + "\" Age=\"" + Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[Guest]["ChildAge1"]) + "\" />");
                            break;
                        case 2:
                            if (Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[Guest]["ChildAge1"]) > 2 && Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[Guest]["ChildAge2"]) > 2)
                            {
                                bookingreq.Append("<GuestCount AgeQualifyingCode=\"" + 8 + "\"" + " " + "Count=\"" + 1 + "\" Age=\"" + Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[Guest]["ChildAge1"]) + "\" />");
                                bookingreq.Append("<GuestCount AgeQualifyingCode=\"" + 8 + "\"" + " " + "Count=\"" + 1 + "\" Age=\"" + Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[Guest]["ChildAge2"]) + "\" />");
                            }
                            else if (Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[Guest]["ChildAge1"]) > 2 && Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[Guest]["ChildAge2"]) < 2)
                            {
                                bookingreq.Append("<GuestCount AgeQualifyingCode=\"" + 8 + "\"" + " " + "Count=\"" + 1 + "\" Age=\"" + Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[Guest]["ChildAge1"]) + "\" />");
                                bookingreq.Append("<GuestCount AgeQualifyingCode=\"" + 7 + "\"" + " " + "Count=\"" + 1 + "\" />");
                            }
                            else if (Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[Guest]["ChildAge1"]) < 2 && Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[Guest]["ChildAge2"]) > 2)
                            {
                                bookingreq.Append("<GuestCount AgeQualifyingCode=\"" + 7 + "\"" + " " + "Count=\"" + 1 + "\" />");
                                bookingreq.Append("<GuestCount AgeQualifyingCode=\"" + 8 + "\"" + " " + "Count=\"" + 1 + "\" Age=\"" + Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[Guest]["ChildAge2"]) + "\" />");
                            }
                            else
                            {
                                bookingreq.Append("<GuestCount AgeQualifyingCode=\"" + 7 + "\"" + " " + "Count=\"" + 2 + "\" />");

                            }
                            break;
                    }

                    //}
                    bookingreq.Append("</GuestCounts>");
                    bookingreq.Append("<TimeSpan End=\"" + CheckOut + "\" Start=\"" + CheckIn + "\"/>");
                    bookingreq.Append("<BasicPropertyInfo HotelCode=\"" + HotelCode + "\"/>");
                    bookingreq.Append("<Comments xmlns='http://www.opentravel.org/OTA/2003/05'>");
                    bookingreq.Append("<Comment>");
                    bookingreq.Append("<Text></Text>");
                    bookingreq.Append("</Comment>");
                    bookingreq.Append("</Comments>");
                    bookingreq.Append("</RoomStay>");
                    bookingreq.Append("</RoomStays>");

                    bookingreq.Append("<ResGuests>");

                    string isLead = string.Empty;
                    dtPassengers.DefaultView.RowFilter = "PickUp='" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomTypeID"]) + "' and RoomNo='" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomNumber"]) + "' and IsLead=1";
                    if (dtPassengers.DefaultView.Count == 0)
                        isLead = "true";
                    dtPassengers.DefaultView.RowFilter = "";
                    dtPassengers.DefaultView.RowFilter = "PickUp='" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomTypeID"]) + "' and RoomNo='" + (dsCartDetails.Tables[1].DefaultView[j]["RoomNumber"]) + "' and UserCartId='" + dsCartDetails.Tables[0].Rows[i]["UserCartId"] + "'";
                    dtPassengers.DefaultView.Sort = "IsLead desc";

                    bool islead = true;

                    if (dtPassengers.DefaultView.Count > 0)
                    {
                        for (int p = 0; p < dtPassengers.DefaultView.Count; p++)

                        {
                            string Name = Convert.ToString(dtPassengers.DefaultView[p]["FirstName"]);
                            string SurName = Convert.ToString(dtPassengers.DefaultView[p]["LastName"]);
                            string PreFix = Convert.ToString(dtPassengers.DefaultView[p]["Prefix"]);
                            int Childage = Convert.ToInt32(dtPassengers.DefaultView[p]["ChildAge"]);
                            //RoomNumber = Convert.ToString(dsCartDetails.Tables[1].DefaultView[0]["RoomNumber"]);

                            //DataRow[] drPassengers = dtPassengers.DefaultView.ToTable().Select("IsLead='1'");
                            //if (drPassengers.Length == 0)
                            //{
                            //    if (Convert.ToString(dtPassengers.DefaultView[p]["PaxType"]).Trim().ToLower() == "adult")
                            //        dtPassengers.DefaultView[p]["IsLead"] = 1;
                            //}

                            if (Convert.ToString(dtPassengers.DefaultView[p]["PaxType"]).Trim().ToLower() == "adult")
                            {

                                int NoOfAdult = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[0]["NoOfAdults"]);//Convert.ToInt32(drUserCartCount[0]["NoOfAdults"]);
                                if (islead)// (Convert.ToInt16(dtPassengers.DefaultView[p]["IsLead"]) == 1)
                                {
                                    bookingreq.Append("<ResGuest AgeQualifyingCode=\"10\" ResGuestRPH=\"" + RoomNumber + "\" PrimaryIndicator=\"true\">");
                                    islead = false;
                                }
                                else
                                {
                                    bookingreq.Append("<ResGuest AgeQualifyingCode=\"10\" ResGuestRPH=\"" + RoomNumber + "\" PrimaryIndicator=\"false\">");
                                }


                                //bookingreq.Append("<ResGuest AgeQualifyingCode=\"10\" ResGuestRPH=\"" + RoomNumber + "\" PrimaryIndicator=\"true\">");
                                bookingreq.Append("<Profiles>");
                                bookingreq.Append("<ProfileInfo>");
                                bookingreq.Append("<Profile ProfileType=\"1\">");
                                bookingreq.Append("<Customer>");
                                bookingreq.Append("<PersonName>");
                                //for (int y = 0; y < NoOfAdult; y++)
                                //{

                                //string Name = Convert.ToString(dtPassengers.DefaultView[p]["FirstName"]);
                                //string SurName = Convert.ToString(dtPassengers.DefaultView[p]["LastName"]);
                                //string PreFix = Convert.ToString(dtPassengers.DefaultView[p]["Prefix"]);


                                bookingreq.Append("<NamePrefix>" + PreFix + "</NamePrefix>");
                                bookingreq.Append("<GivenName>" + Name + "</GivenName>");
                                bookingreq.Append("<Surname>" + SurName + "</Surname>");
                                bookingreq.Append("</PersonName>");
                                bookingreq.Append("<Email>" + "Test@gmail.com" + "</Email>");
                                bookingreq.Append("</Customer>");
                                bookingreq.Append("</Profile>");
                                bookingreq.Append("</ProfileInfo>");
                                bookingreq.Append("</Profiles>");
                                bookingreq.Append("</ResGuest>");
                            }
                            #region old
                            //if (Convert.ToInt16(dtPassengers.DefaultView[p]["IsLead"]) != 1 && (Convert.ToString(dtPassengers.DefaultView[p]["PaxType"]).Trim().ToLower() == "adult"))
                            //{
                            //    int NoOfAdult = Convert.ToInt32(drUserCartCount[0]["NoOfAdults"]);
                            //    bookingreq.Append("<ResGuest AgeQualifyingCode=\"10\" ResGuestRPH=\"" + RoomNumber1 + "\" PrimaryIndicator=\"false\">");
                            //    bookingreq.Append("<Profiles>");
                            //    bookingreq.Append("<ProfileInfo>");
                            //    bookingreq.Append("<Profile ProfileType=\"1\">");
                            //    bookingreq.Append("<Customer>");
                            //    bookingreq.Append("<PersonName>");
                            //    bookingreq.Append("<NamePrefix>" + PreFix + "</NamePrefix>");
                            //    bookingreq.Append("<GivenName>" + Name + "</GivenName>");
                            //    bookingreq.Append("<Surname>" + SurName + "</Surname>");
                            //    bookingreq.Append("</PersonName>");
                            //    bookingreq.Append("<Email>" + "Test@gmail.com" + "</Email>");
                            //    bookingreq.Append("</Customer>");
                            //    bookingreq.Append("</Profile>");
                            //    bookingreq.Append("</ProfileInfo>");
                            //    bookingreq.Append("</Profiles>");
                            //    bookingreq.Append("</ResGuest>");
                            //}
                            #endregion
                            if (Convert.ToString(dtPassengers.DefaultView[p]["PaxType"]).Trim().ToLower() == "child")
                            {
                                if (Childage > 2)
                                {
                                    bookingreq.Append("<ResGuest AgeQualifyingCode=\"8\" Age=\"" + Childage + "\" ResGuestRPH=\"" + RoomNumber + "\" PrimaryIndicator=\"false\">");
                                }
                                else
                                {
                                    bookingreq.Append("<ResGuest AgeQualifyingCode=\"7\" ResGuestRPH=\"" + RoomNumber + "\" PrimaryIndicator=\"false\">");
                                }
                                //bookingreq.Append("<ResGuest AgeQualifyingCode=\"8\" ResGuestRPH=\"" + RoomNumber1 + "\" PrimaryIndicator=\"false\">");
                                bookingreq.Append("<Profiles>");
                                bookingreq.Append("<ProfileInfo>");
                                bookingreq.Append("<Profile ProfileType=\"1\">");
                                bookingreq.Append("<Customer>");
                                bookingreq.Append("<PersonName>");

                                //Name = Convert.ToString(dtPassengers.DefaultView[p]["FirstName"]);
                                //SurName = Convert.ToString(dtPassengers.DefaultView[p]["LastName"]);
                                //PreFix = Convert.ToString(dtPassengers.DefaultView[p]["Prefix"]);

                                bookingreq.Append("<NamePrefix>" + PreFix + "</NamePrefix>");
                                bookingreq.Append("<GivenName>" + Name + "</GivenName>");
                                bookingreq.Append("<Surname>" + SurName + "</Surname>");
                                bookingreq.Append("</PersonName>");
                                bookingreq.Append("<Email>" + "Test@gmail.com" + "</Email>");
                                bookingreq.Append("</Customer>");
                                bookingreq.Append("</Profile>");
                                bookingreq.Append("</ProfileInfo>");
                                bookingreq.Append("</Profiles>");
                                bookingreq.Append("</ResGuest>");
                            }
                            #region
                            //if (Childage < 3 && Convert.ToString(dtPassengers.DefaultView[p]["PaxType"]).Trim().ToLower() != "adult")
                            //{
                            //    bookingreq.Append("<ResGuest AgeQualifyingCode=\"7\" ResGuestRPH=\"" + RoomNumber1 + "\" PrimaryIndicator=\"false\">");
                            //    bookingreq.Append("<Profiles>");
                            //    bookingreq.Append("<ProfileInfo>");
                            //    bookingreq.Append("<Profile ProfileType=\"1\">");
                            //    bookingreq.Append("<Customer>");
                            //    bookingreq.Append("<PersonName>");

                            //    Name = Convert.ToString(dtPassengers.DefaultView[p]["FirstName"]);
                            //    SurName = Convert.ToString(dtPassengers.DefaultView[p]["LastName"]);
                            //    PreFix = Convert.ToString(dtPassengers.DefaultView[p]["Prefix"]);

                            //    bookingreq.Append("<NamePrefix>" + PreFix + "</NamePrefix>");
                            //    bookingreq.Append("<GivenName>" + Name + "</GivenName>");
                            //    bookingreq.Append("<Surname>" + SurName + "</Surname>");
                            //    bookingreq.Append("</PersonName>");
                            //    bookingreq.Append("<Email>" + "Test@gmail.com" + "</Email>");
                            //    bookingreq.Append("</Customer>");
                            //    bookingreq.Append("</Profile>");
                            //    bookingreq.Append("</ProfileInfo>");
                            //    bookingreq.Append("</Profiles>");
                            //    bookingreq.Append("</ResGuest>");
                            //}
                            #endregion
                        }
                    }
                    bookingreq.Append("</ResGuests>");
                    bookingreq.Append("</HotelReservation>");
                    bookingreq.Append("</HotelReservations>");
                    bookingreq.Append("</OTA_HotelResRQ>");
                    bookingreq.Append("</Body>");
                    bookingreq.Append("</Envelope>");
                }
                string bookingrequest = bookingreq.ToString();
                return bookingrequest;

            }
            catch (Exception ex)
            {
                _ =_exception.Error(ex);
            }
            return null;
            #endregion
        }
        public string SaltoursBookingRequest(string Password, string UserName, DataSet dsCartDetails, DataRow[] saltoursDetails, int i, int j, double resPrice, DataTable dtPassengers, ref string AgtRef, string CheckIn, string CheckOut, int UniqueNo, string BookingId,string AgentCode="")
        {
            #region Booking Request 
            try
            {
                string ExternalReference = "Book_" + Convert.ToString(AgentCode + "_" + UniqueNo);
                int UserCartId = Convert.ToInt32(dsCartDetails.Tables[0].Rows[i]["UserCartId"]);
                //string Email = WebConfigurationManager.AppSettings["email"];
                StringBuilder BookingReq = new StringBuilder();
                BookingReq.Append("<?xml version=\"1.0\"?>");
                BookingReq.Append("<Envelope xmlns='http://schemas.xmlsoap.org/soap/envelope/' xmlns:xsd='http://www.w3.org/2001/XMLSchema' xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance'>");
                BookingReq.Append("<Header>");
                BookingReq.Append("<Security xmlns='http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd'>");
                BookingReq.Append("<UsernameToken>");
                BookingReq.Append("<Username>" + UserName + "</Username>");
                BookingReq.Append("<Password>" + Password + "</Password>");
                BookingReq.Append("</UsernameToken>");
                BookingReq.Append("</Security>");
                BookingReq.Append("</Header>");
                BookingReq.Append("<Body>");
                BookingReq.Append("<OTA_HotelResRQ TransactionStatusCode=\"End\" Version=\"1.002\" TimeStamp=\"" + DateTime.Now.ToString("yyyy-MM-ddThh:mm:sszzz") + "\" TransactionIdentifier=\"" + UniqueNo + "\" ResStatus=\"Commit\"  xmlns='http://schemas.xmlsoap.org/soap/envelope/'>");
                BookingReq.Append("<HotelReservations xmlns='http://www.opentravel.org/OTA/2003/05'>");
                BookingReq.Append("<HotelReservation xmlns='http://www.opentravel.org/OTA/2003/05'>");
                BookingReq.Append("<UniqueID ID=\"" + ExternalReference + "\" Instance=\"PNR\" Type=\"14\" />");
                BookingReq.Append("<ResGlobalInfo xmlns='http://www.opentravel.org/OTA/2003/05'>");
                BookingReq.Append("<HotelReservationIDs xmlns='http://www.opentravel.org/OTA/2003/05'>");
                BookingReq.Append("<HotelReservationID ResID_Type=\"16\" ResID_Value=\"" + BookingId + "\"/>");
                BookingReq.Append("</HotelReservationIDs>");
                BookingReq.Append("</ResGlobalInfo>");
                BookingReq.Append("</HotelReservation>");
                BookingReq.Append("</HotelReservations>");
                BookingReq.Append("</OTA_HotelResRQ>");
                BookingReq.Append("</Body>");
                BookingReq.Append("</Envelope>");

                string bookingrequest = BookingReq.ToString();
                return bookingrequest;

            }
            catch (Exception ex)
            {
                _ =_exception.Error(ex);
            }
            return null;
            #endregion
        }

        public async Task<ResponseDto> GetsaltoursResponse(string strxmlRQRS, string strHost, Int32 TimeOut, string Provider = "")
        {
            ResponseDto responseDto = new ResponseDto();
            string Error = "";
            System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
            timer.Start();
            string response = null;


            string reqId = Convert.ToString(DateTime.Now.Ticks);
            try
            {
                HttpWebRequest Request;
                Request = (HttpWebRequest)HttpWebRequest.Create(strHost);
                Request.Timeout = TimeOut;
                Request.ReadWriteTimeout = TimeOut;

                string ProviderNew = Provider.ToLower();
                switch (ProviderNew)
                {
                    case "saltours":
                        Request.Accept = "application/xml";
                        Request.Method = "POST";
                        Request.KeepAlive = true;
                        break;
                    default:

                        Request.Headers.Add("Accept-Encoding", "gzip");

                        break;
                }
                using (Stream requestStream = Request.GetRequestStream())
                {
                    byte[] postBytes = Encoding.ASCII.GetBytes(strxmlRQRS);
                    //Writes a sequence of bytes to the current stream                     
                    requestStream.Write(postBytes, 0, postBytes.Length);
                    requestStream.Close();//Close stream
                }

                HttpWebResponse response1 = (HttpWebResponse)await Request.GetResponseAsync();
                Stream responseStream = response1.GetResponseStream();

                if (response1.ContentEncoding.ToLower().Contains("gzip"))
                    responseStream = new System.IO.Compression.GZipStream(responseStream, System.IO.Compression.CompressionMode.Decompress);
                else if (response1.ContentEncoding.ToLower().Contains("deflate"))
                    responseStream = new System.IO.Compression.DeflateStream(responseStream, System.IO.Compression.CompressionMode.Decompress);
                StreamReader streamReader = new StreamReader(responseStream);


                response =await streamReader.ReadToEndAsync();
                string strOutput = response.ToString();
                responseStream.Close();
                streamReader.Close();
                response1.Close();
            }
            catch (Exception ee)
            {
                _ = _exception.Error(ee);

                Error = ee.Message.ToString();
                responseDto.Error = Error;
                return responseDto;
            }
            responseDto.Response = response;
            return responseDto;

        }

        public string Get_Saltours_BookingCancelRQ(string AgentVoucherNo, DataRow[] SaltoursDetails, string ISOCountry, string Uniqu_Id, string Type, string Provider = "")
        {
            string CancellationRQ = "";
            try
            {
                #region Booking Cancel RQ
                string UserID = Convert.ToString(SaltoursDetails[0]["UserId"]);
                string UserName = Convert.ToString(SaltoursDetails[0]["UserName"]);
                string Password = Convert.ToString(SaltoursDetails[0]["Password"]);
                string uniqid = Guid.NewGuid().ToString().Substring(0, 13);

                StringBuilder CancelRQ = new StringBuilder();

                CancelRQ.Append("<?xml version=\"1.0\"?>");
                CancelRQ.Append("<Envelope xmlns='http://schemas.xmlsoap.org/soap/envelope/' xmlns:xsd='http://www.w3.org/2001/XMLSchema' xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance'>");
                CancelRQ.Append("<Header>");
                CancelRQ.Append("<Security xmlns='http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd'>");
                CancelRQ.Append("<UsernameToken>");
                CancelRQ.Append("<Username>" + UserName + "</Username>");
                CancelRQ.Append("<Password>" + Password + "</Password>");
                CancelRQ.Append("</UsernameToken>");
                CancelRQ.Append("</Security>");
                CancelRQ.Append("</Header>");
                CancelRQ.Append("<Body>");
                CancelRQ.Append("<OTA_CancelRQ TransactionStatusCode=\"End\" CancelType=\"Commit\" TimeStamp=\"" + DateTime.Now.ToString("yyyy-MM-ddThh:mm:sszzz") + "\" Version=\"9\" >");
                CancelRQ.Append("<POS xmlns='http://www.opentravel.org/OTA/2003/05'>");
                CancelRQ.Append("<Source ISOCountry=\"" + ISOCountry + "\" />");
                CancelRQ.Append("</POS>");
                CancelRQ.Append("<UniqueID xmlns='http://www.opentravel.org/OTA/2003/05' Type=\"" + Type + "\" ID=\"" + Uniqu_Id + "\"/>");
                CancelRQ.Append("</OTA_CancelRQ>");
                CancelRQ.Append("</Body>");
                CancelRQ.Append("</Envelope>");

                CancellationRQ = CancelRQ.ToString();
                #endregion
            }
            catch (Exception ex)
            {
                _ =_exception.ErrorNew(ex, ErrorLog._LogAPI.CancelBooking);
                return "";
            }
            return CancellationRQ;
        }
    }
}
