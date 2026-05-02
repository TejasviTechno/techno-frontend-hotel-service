using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using XConnect.CommonUtilities;
using XConnect.CommonUtilities.Interface;
using XConnect.ErrorLogHandler.Interface;
using XConnect.Markup.Interface;
using XConnect.Models;
using XConnect.Models.Hotel;
using XConnect.Supplier.UkTravco.Interface;
using static Azure.Core.HttpHeader;
using Options = XConnect.Models.Options;

namespace XConnect.Supplier.UkTravco
{
    public class UkTravcoRepo : IUkTravco
    {
        private readonly ICommonMethod _commonMethod;
        private readonly IException _exception;
        private readonly IMarkup _markup;
        private readonly ISupplierRes _supplierRes;
        private readonly KeySettings _keySettings;

        public UkTravcoRepo(ICommonMethod commonMethod, IMarkup markup, ISupplierRes supplierRes, IOptions<KeySettings> keySettings, IException exception)
        {
            _commonMethod = commonMethod;
            _markup = markup;
            _supplierRes = supplierRes;
            _keySettings = keySettings.Value;
            _exception = exception;
        }
        public void GetUkTravcoRequestOnly_Async(DataRow[] commonCode, List<StarRating> listRating, DataTable dtPreferredDataHotel, HotelSearchParameter SearchPara, List<SupplierSearchConfig> lstUk_travcoSearchConfig, List<SupplierDetails> lstUk_travcoDetails, ref ConcurrentBag<HotelX> AllTask, string AgentCode, int Hit, TokenData tokenData, DataSet dtXMLCity)

        {
            List<StarRating> lstRating = new List<StarRating>();
            lstRating = _commonMethod.GetStarRatingStringList_xtg(SearchPara);

            List<HotelCodeByCity> DtHotelDAta = HtmlExtensions.ToList<HotelCodeByCity>(commonCode.CopyToDataTable());
            int ratingcount = lstRating.Count;
            int hotelcount = DtHotelDAta.Count;

            foreach (SupplierDetails uk_travcoDetails in lstUk_travcoDetails)
            {
                try
                {
                    int SupplierID = uk_travcoDetails.SupplierID;
                    string Supplier = uk_travcoDetails.SupplierName;

                    if (DtHotelDAta.Count > 0)
                    {
                        if (lstUk_travcoSearchConfig.Count > 0)
                        {
                            List<SupplierSearchConfig> uk_travcoSearchConfig = lstUk_travcoSearchConfig
                           .Where(x => x != null && x.SupplierID == SupplierID)
                           .ToList();

                            if (uk_travcoSearchConfig != null)
                            {

                                DataForSearch dataForSearch = _commonMethod.GetXmlDataForSearchTechwise(SearchPara.Nationality, dtXMLCity, Supplier, uk_travcoDetails.TechName);

                                //if (dataForSearch.xmlCity != "" && dataForSearch.xmlCountry != "")
                                {
                                    int Pax = 0;
                                    bool isChild = false;
                                    for (int i = 0; i < SearchPara.Rooms.Count; i++)
                                    {
                                        Pax = Pax + SearchPara.Rooms[i].NoofAdults + SearchPara.Rooms[i].NoofChild;
                                        //if (SearchPara.Rooms[i].NoofChild > 0) //Comment On 061125
                                        //    isChild = true;
                                    }
                                    //if (Pax > 9 || isChild) //Comment On 061125 As per Testing team its working before
                                    if (Pax > 9) 
                                    {
                                        uk_travcoSearchConfig = null;
                                    }
                                    DataRow[] druk_travco = dtPreferredDataHotel.Select("ServiceName='" + Supplier + "'");
                                    string Ids = string.Empty;
                                    if (druk_travco.Count() > 0)
                                    {
                                        var paramValues1 = String.Join(",", druk_travco.Select(x => String.Join(",", Convert.ToString(x.ItemArray[0]))));
                                        Ids = Convert.ToString(paramValues1);
                                    }
                                    if (SearchPara.IsSearchFor.ToLower().IndexOf("preferred") >= 0 && druk_travco.Count() == 0)
                                    {
                                        uk_travcoSearchConfig = null;
                                    }

                                    if (uk_travcoSearchConfig != null && uk_travcoSearchConfig.Count > 0)
                                    {
                                        List<HotelCodeByCity> filter = new List<HotelCodeByCity>();
                                        filter = _commonMethod.FilterWithOutCity_Code(ratingcount, hotelcount, DtHotelDAta, lstRating, Supplier);

                                        if (filter.Count > 0)
                                        {
                                            string Currency = uk_travcoDetails.CurrencyShortName;
                                            double CurrencyFactoruk_travco = 1;
                                            if (Currency.ToLower() != tokenData.SiteCurrency.ToLower())
                                            {
                                                CurrencyFactoruk_travco = _commonMethod.GetROEByCurrency(Currency, tokenData.dtROE);
                                            }


                                            for (int k = 0; k < uk_travcoSearchConfig.Count; k++)
                                            {
                                                //if (Convert.ToInt32(uk_travcoSearchConfig[k]["Hit"]) != Hit)
                                                //    continue;
                                                if (Convert.ToInt32(uk_travcoSearchConfig[k].CityID) == 0 || Convert.ToInt32(uk_travcoSearchConfig[k].CityID) == SearchPara.CityID)//All City means 0
                                                {
                                                    // List<HotelCodeByCity> DtHotelDAta = FilterFinalList(commonCode, listRating);
                                                    if (commonCode.Length > 0)
                                                    {
                                                        bool DebugMode = (Convert.ToInt32(uk_travcoSearchConfig[k].DebugMode) == 0 ? false : true);
                                                        int Noofrecords = Convert.ToInt32(uk_travcoSearchConfig[k].Noofrecords);
                                                        double count = Convert.ToDouble(filter.Count);
                                                        int NoOfDiv = Convert.ToInt32(Math.Ceiling(count / Convert.ToDouble(Noofrecords)));
                                                        List<List<XMLID_Search>> lstxml = _commonMethod.GetXMLIDString(NoOfDiv, Noofrecords, count, filter);

                                                        for (int a = 0; a < NoOfDiv; a++)
                                                        {
                                                            string IdsForSearch = Convert.ToString(_commonMethod.GetHotelCodeByCOmma(lstxml[a]));
                                                            HotelX xhotels = new HotelX();
                                                            xhotels.Supplier = Supplier;
                                                            xhotels.SearchTimeOut = Convert.ToInt32(uk_travcoSearchConfig[k].HotelSearchTimeOut);
                                                            //if (Hit > 0)
                                                            xhotels.ProcessTimeout = Convert.ToInt32(uk_travcoSearchConfig[k].HotelSearchTimeOut2);

                                                            //xhotels.URL = Convert.ToString(uk_travcoDetails[0].WebSiteURL) + "HotelAvailabilityV7Service?wsdl";
                                                            //xhotels.URL = Convert.ToString(uk_travcoDetails[0].WebSiteURL) + "HotelAvailabilityV7Service";
                                                            if (_keySettings.SupplierProxy == "0")//Added on 9-APR-2025
                                                            {
                                                                xhotels.URL = Convert.ToString(uk_travcoDetails.WebSiteURL) + "HotelAvailabilityV7Service";
                                                            }
                                                            else
                                                            {
                                                                xhotels.URL = Convert.ToString(uk_travcoDetails.WebSiteURLforAPI) + "HotelAvailabilityV7Service";//Added on 3-MAR-2025
                                                            }
                                                            xhotels.Action = "urn:checkAvailabilityByMultipleHotelCodes";
                                                            xhotels.DebugeMode = DebugMode;
                                                            xhotels.CurrencyCode = Currency;
                                                            xhotels.CurrencyFactor = CurrencyFactoruk_travco;
                                                            xhotels.XmlNationality = "";
                                                            xhotels.XmlCity = dataForSearch.xmlCity;
                                                            xhotels.XmlCountry = dataForSearch.xmlCountry;

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
                                                                objrq.Request = (XMLuk_travcoRequestOnly_Async(Convert.ToInt32(uk_travcoSearchConfig[k].HotelSearchTimeOut), SearchPara, Convert.ToString(uk_travcoDetails.WebSiteURL), Convert.ToString(uk_travcoDetails.UserID), Convert.ToString(uk_travcoDetails.UserName), Convert.ToString(uk_travcoDetails.Password), IdsForSearch, dataForSearch.xmlCity, dataForSearch.xmlCountry));
                                                                objrq.Response = ""; RQRS.Add(objrq);
                                                            }
                                                            xhotels.RQRS = RQRS;
                                                            xhotels.ReturnRooms = uk_travcoDetails.ReturnRooms;
                                                            xhotels.TechName = uk_travcoDetails.TechName;
                                                            #endregion


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


        public string XMLuk_travcoRequestOnly_Async(Int32 SearchTimeOut, HotelSearchParameter SearchParam, string uk_travco_URL, string uk_travco_User_ID, string uk_travco_User_Name, string uk_travco_Password, string HotelId, string CityCode, string CountryCode)
        {
            try
            {
                #region Data

                DataSet dsTemp = new DataSet();
                ///////////////
                if (SearchParam.Rooms.Count > 1)
                {
                    for (int i = 0; i < SearchParam.Rooms.Count; i++)
                    {
                        if (SearchParam.Rooms[i].Child1Age == 12)
                            return "";
                        if (SearchParam.Rooms[i].NoofChild > 0)
                        {
                            if (SearchParam.Rooms[i].NoofAdults != 2)
                                return "";
                        }
                    }
                }
                for (int i = 0; i < SearchParam.Rooms.Count; i++)
                {
                    if (SearchParam.Rooms[i].Child2Age > 0)
                        return "";
                }
                //////////////

                int Adult = 0;
                int Child = 0;
                #endregion
                string Action = "urn:checkAvailabilityByCity";
                return Getuk_travcoSearchRequest(SearchParam, CityCode, CountryCode, uk_travco_URL, uk_travco_User_ID, uk_travco_User_Name, uk_travco_Password, HotelId, ref Adult, ref Child, ref Action, false);

            }
            catch (Exception ex)
            {
                _exception.Error(ex);
                return "";
            }
        }

        public string Getuk_travcoSearchRequest(HotelSearchParameter SearchParam, string uk_travco_CityCode, string CountryCode, string uk_travco_URL, string uk_travco_User_ID, string uk_travco_User_Name, string uk_travco_Password, string HotelId, ref int NoOfA, ref int ChildCount, ref string Action, bool isSearch = false)
        {
            StringBuilder SearchRQ = new StringBuilder();
            try
            {
                string Ids = HotelId;
                string checkin = (SearchParam.CheckInDate).Substring(6, 4) + "-" + (SearchParam.CheckInDate).Substring(3, 2) + "-" + (SearchParam.CheckInDate).Substring(0, 2);
                string checkout = (SearchParam.CheckOutDate).Substring(6, 4) + "-" + (SearchParam.CheckOutDate).Substring(3, 2) + "-" + (SearchParam.CheckOutDate).Substring(0, 2);
                string AgentID = uk_travco_User_Name;
                string Password = uk_travco_Password;
                int NoOfRoom = SearchParam.Rooms.Count();
                //int Adult = SearchParam.Rooms[0].NoofAdults;
                //int Child = SearchParam.Rooms[0].NoofChild;
                StringBuilder RoomType = new StringBuilder();

                //RoomType.Append("<ns2:RoomData ");
                string[] rtad = new string[NoOfRoom];
                string[] rtch = new string[NoOfRoom];
                int ad = 1;
                int ch = 1;
                int chk = 0;
                string RType = "";
                //////////
                //DataTable dtChild = new DataTable("Child");
                //DataColumn dcChild;
                //dcChild = new DataColumn("Child", typeof(int));
                //dtChild.Columns.Add(dcChild);
                ////////////
                DataTable dtRoomDisp = new DataTable("tblRoomDisp");
                DataTable dtOriginalRoom = new DataTable("tblOriginalRooms");//Original No of rooms without merge
                dtRoomDisp.Columns.Add("RoomNo", typeof(int));
                dtRoomDisp.Columns.Add("NoofAdult", typeof(int));
                //dtRoomDisp.Columns.Add("NoofChild", typeof(int));
                dtRoomDisp.Columns.Add("RoomUnit", typeof(int));

                var result = SearchParam.Rooms.GroupBy(x => new { x.RoomName, x.NoofAdults }).Select(n => new { RoomName = n.Key.RoomName, NoofAdults = n.Key.NoofAdults, Count = n.Count() });

                int sroomNo = 1; int TotalAdult = 0; int TotalChild = 0;
                foreach (var item in result)//For each Item
                {
                    DataRow dtrow = dtRoomDisp.NewRow();
                    dtrow["RoomNo"] = sroomNo;
                    dtrow["NoofAdult"] = item.NoofAdults;
                    //dtrow["NoofChild"] = item.NoofChild;
                    dtrow["RoomUnit"] = item.Count;
                    if (TotalAdult > 0)
                    {
                        dtrow["TotalAdult"] = TotalAdult;
                        // dtrow["TotalChild"] = TotalChild;
                        dtrow["MaxSelect"] = TotalAdult % 2 != 0 ? 1 : TotalAdult / 2;
                        dtrow["UnitSelected"] = 0;
                    }
                    dtRoomDisp.Rows.Add(dtrow);
                    sroomNo = sroomNo + item.Count;
                }

                RoomType.Append("<ns2:RoomData ");
                //<req:RoomData SingleRoom="?" DoubleRoom="?" TripleRoom="?" QuadRoom="?" ChildRoom="?"/>
                for (int i = 0; i < dtRoomDisp.Rows.Count; i++)
                {
                    int RNo = Convert.ToInt32(dtRoomDisp.Rows[i]["RoomNo"]);
                    NoOfA = Convert.ToInt32(dtRoomDisp.Rows[i]["NoofAdult"]);
                    int RUnit = Convert.ToInt32(dtRoomDisp.Rows[i]["RoomUnit"]);
                    for (int a = 0; a < NoOfRoom; a++) //For 12 year old child as Adult
                    {
                        if (SearchParam.Rooms[i].NoofChild > 0)
                        {
                            if (SearchParam.Rooms[i].NoofChild == 1)
                            {
                                if (SearchParam.Rooms[i].Child1Age > 11)
                                    NoOfA = NoOfA + 1;
                            }
                            if (SearchParam.Rooms[i].NoofChild == 2)
                            {
                                if (SearchParam.Rooms[i].Child1Age > 11)
                                    NoOfA = NoOfA + 1;
                                if (SearchParam.Rooms[i].Child2Age > 11)
                                    NoOfA = NoOfA + 1;
                            }
                        }
                    }
                    switch (NoOfA)
                    {
                        case 1:
                            RoomType.Append(" SingleRoom=\"" + RUnit + "\"");
                            break;
                        case 2:
                            RoomType.Append(" DoubleRoom=\"" + RUnit + "\"");
                            break;
                        case 3:
                            RoomType.Append(" TripleRoom=\"" + RUnit + "\"");
                            break;
                        case 4:
                            RoomType.Append(" QuadRoom=\"" + RUnit + "\"");
                            break;
                    }
                    //RoomType.Append("SingleRoom=\"?\"");
                    //RoomType.Append("DoubleRoom=\"?\"");
                    //RoomType.Append("TripleRoom=\"?\"");
                    //RoomType.Append("QuadRoom=\"?\"");
                }

                for (int i = 0; i < NoOfRoom; i++)//For 1 year old child as infant
                {
                    if (SearchParam.Rooms[i].NoofChild > 0)
                    {
                        if (SearchParam.Rooms[i].NoofChild == 1)
                        {
                            if (SearchParam.Rooms[i].Child1Age > 1)
                                ChildCount = ChildCount + 1;
                            if (SearchParam.Rooms[i].Child1Age > 11)//For 12year child remove
                                if (ChildCount > 0)
                                    ChildCount = ChildCount - 1;
                        }
                        if (SearchParam.Rooms[i].NoofChild == 2)
                        {
                            if (SearchParam.Rooms[i].Child1Age > 1)
                                ChildCount = ChildCount + 1;
                            if (SearchParam.Rooms[i].Child2Age > 1)
                                ChildCount = ChildCount + 1;
                            if (SearchParam.Rooms[i].Child1Age > 11)//For 12year child remove
                                if (ChildCount > 0)
                                    ChildCount = ChildCount - 1;
                            if (SearchParam.Rooms[i].Child2Age > 11)//For 12year child remove
                                if (ChildCount > 0)
                                    ChildCount = ChildCount - 1;
                        }
                    }
                }
                RoomType.Append(" ChildRoom=\"" + ChildCount + "\"");
                //RoomType.Append(" ChildRoom=\"1\"");
                RoomType.Append("/>");
                SearchRQ.Append("<?xml version='1.0' encoding='UTF-8'?>");
                SearchRQ.Append("<soapenv:Envelope xmlns:soapenv=\"http://schemas.xmlsoap.org/soap/envelope/\">");
                SearchRQ.Append("<soapenv:Body>");
                //if (Ids == string.Empty)
                //    SearchRQ.Append("<ns4:checkAvailabilityByCity xmlns:ns4=\"http://www.travco.co.uk/\"");
                //else
                //    SearchRQ.Append("<ns4:checkAvailabilityByMultipleHotelCodes xmlns:ns4=\"http://www.travco.co.uk/\"");
                //SearchRQ.Append(" xmlns:ns3=\"http://www.travco.co.uk/trlink/xsd/hotelavailabilityv7/response\"");
                //SearchRQ.Append(" xmlns:ns2=\"http://www.travco.co.uk/trlink/xsd/hotelavailabilityv7/request\">");
                if (Ids == string.Empty)
                    SearchRQ.Append("<ns2:checkAvailabilityByCity xmlns:ns2=\"http://www.travco.co.uk/trlink/xsd/hotelavailabilityv7/request\">");
                else
                    SearchRQ.Append("<ns2:checkAvailabilityByMultipleHotelCodes xmlns:ns2=\"http://www.travco.co.uk/trlink/xsd/hotelavailabilityv7/request\">");
                //SearchRQ.Append(" xmlns:ns3=\"http://www.travco.co.uk/trlink/xsd/hotelavailabilityv7/response\"");
                //SearchRQ.Append(" xmlns:ns2=\"http://www.travco.co.uk/trlink/xsd/hotelavailabilityv7/request\">");
                SearchRQ.Append("<ns2:RequestBase Lang=\"en-GB\" AgentPassword=\"" + uk_travco_Password + "\" AgentCode=\"" + uk_travco_User_Name + "\" />");
                if (Ids == string.Empty)
                {
                    SearchRQ.Append("<ns2:CountryCode>" + CountryCode + "</ns2:CountryCode>");
                    SearchRQ.Append("<ns2:CityCode>" + uk_travco_CityCode + "</ns2:CityCode>");
                }
                else
                {
                    SearchRQ.Append("<ns2:MultipleHotelCodes>");
                    string[] hid = Ids.Split(',');
                    for (int i = 0; i < hid.Length; i++)
                    {
                        SearchRQ.Append("<HotelCode>" + hid[i] + "</HotelCode>");
                    }
                    SearchRQ.Append("</ns2:MultipleHotelCodes>");
                    Action = "urn:checkAvailabilityByMultipleHotelCodes";
                }
                SearchRQ.Append("<ns2:CheckInDate>" + checkin + "</ns2:CheckInDate>");
                SearchRQ.Append("<ns2:CheckOutDate>" + checkout + "</ns2:CheckOutDate>");
                SearchRQ.Append(RoomType.ToString());
                //SearchRQ.Append("<ns2:OptionalData RequestLetter=\"\" FirstTime=\"no\">");
                //SearchRQ.Append("<ns2:RequestPart />");
                //SearchRQ.Append("</ns2:OptionalData>");
                SearchRQ.Append("<ns2:AdditionalData NeedEnglishText=\"no\" ");
                SearchRQ.Append("NeedCityArea=\"no\" ");
                SearchRQ.Append("NeedLocation=\"no\" ");
                SearchRQ.Append("NeedHotelProperties=\"no\" ");
                SearchRQ.Append("NeedGeoCodes=\"no\" ");
                SearchRQ.Append("NeedArrivalPointOther=\"no\" ");
                SearchRQ.Append("NeedArrivalPointMain=\"no\" ");
                SearchRQ.Append("NeedHotelCity=\"no\" ");
                SearchRQ.Append("NeedHotelDescription=\"no\" ");
                SearchRQ.Append("NeedAmenity=\"no\" ");
                SearchRQ.Append("NeedPicture=\"no\" ");
                SearchRQ.Append("NeedEmail=\"no\" ");
                SearchRQ.Append("NeedMapPicture=\"no\" ");
                SearchRQ.Append("NeedBedPicture=\"no\" ");
                SearchRQ.Append("NeedFaxNo=\"no\" ");
                SearchRQ.Append("NeedTelephoneNo=\"no\" ");
                SearchRQ.Append("NeedHotelAddress=\"no\" ");
                SearchRQ.Append("NeedFreeNightDetail=\"yes\" ");
                if (isSearch)
                {
                    SearchRQ.Append("NeedHotelMessages=\"no\" ");
                    SearchRQ.Append("NeedReductionAmount=\"yes\" ");
                    SearchRQ.Append("NeedAvailableHotelsOnly=\"yes\" ");
                    SearchRQ.Append("NeedTotalNoOfHotels=\"yes\" ");
                    SearchRQ.Append("NeedCancellationDetails=\"no\" ");
                }
                else
                {
                    SearchRQ.Append("NeedHotelMessages=\"yes\" ");
                    SearchRQ.Append("NeedReductionAmount=\"yes\" ");
                    SearchRQ.Append("NeedAvailableHotelsOnly=\"yes\" ");
                    SearchRQ.Append("NeedTotalNoOfHotels=\"yes\" ");
                    SearchRQ.Append("NeedCancellationDetails=\"yes\" ");
                }

                SearchRQ.Append("/>");
                if (Ids == string.Empty)
                    SearchRQ.Append("</ns2:checkAvailabilityByCity>");
                else
                    SearchRQ.Append("</ns2:checkAvailabilityByMultipleHotelCodes>");
                SearchRQ.Append("</soapenv:Body>");
                SearchRQ.Append("</soapenv:Envelope>");

                return SearchRQ.ToString();
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
            }
            return "";
        }

        public async Task<List<Options>> UkTravco_HotelSearchWithRooms(HotelX Item, CommonSearch commonSearch, string ResponseData, HotelSearchParameter SearchParam, string XMLCITY, string XMLCountry, string Currency, double CurrencyFactoruk_travco, Int32 processTimeout, string ResProccessTimerLogActive, string ResProccessTimeValueforSaveLog, string XmlNationality = "")
        {
            System.Diagnostics.Stopwatch timerroomx = new System.Diagnostics.Stopwatch();
            timerroomx.Start();
            List<Options> options = new List<Options>(); ;
            try
            {
                string PaxCountryName = XmlNationality;
                if (XmlNationality == null || XmlNationality == "")
                {
                    PaxCountryName = SearchParam.Nationality;
                }
                //DataSet DSNationalityData = new DataSet();
                //DSNationalityData = (DataSet)(System.Web.HttpRuntime.Cache["uk_travco_Nationality"]);
                ///////////////
                // DataTable dtInrCurrency = Common.GetCountryCurrency("USD");
                // double CurrencyFactoruk_travco = 1;// Convert.ToDouble(dtInrCurrency.Rows[0]["AmtToExch"]);
                DataTable DtHotel = _commonMethod.GenerateDataTableNewV2();

                #region Response
                if (ResponseData == string.Empty || ResponseData.Contains("ErrorTxt"))
                {
                    Item.ResponseStatus = 404;
                    Item.ResponseType = "NoCount";
                    return options;
                }

                DataSet uk_travcoResponse = _commonMethod.GetDataSetFromXMLString(ResponseData);
                if (uk_travcoResponse == null || uk_travcoResponse.Tables["Hotel"] == null)
                {
                    Item.ResponseStatus = 404;
                    Item.ResponseType = "NoCount";
                    return options;
                }
                DataTable dtlowprice = uk_travcoResponse.Tables["Hotel"];
                DataTable dtRoomData = uk_travcoResponse.Tables["RoomData"];
                DataColumn Id = new DataColumn();
                Id.DataType = System.Type.GetType("System.Int32");
                Id.AutoIncrement = true;
                Id.AutoIncrementSeed = 0;
                Id.AutoIncrementStep = 1;
                dtRoomData.Columns.Add(Id);
                int index = -1;
                int NoOfA = 0;
                int ChildCount = 0;
                DateTime dt = DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                DateTime DateFrom = Convert.ToDateTime((dt).ToString("MM/dd/yyyy"));
                int RT_Count = SearchParam.Rooms.Count();
                DataTable dtRoomDisp = new DataTable("tblRoomDisp");
                DataTable dtOriginalRoom = new DataTable("tblOriginalRooms");//Original No of rooms without merge
                dtRoomDisp.Columns.Add("RoomNo", typeof(int));
                dtRoomDisp.Columns.Add("NoofAdult", typeof(int));
                //dtRoomDisp.Columns.Add("NoofChild", typeof(int));
                dtRoomDisp.Columns.Add("RoomUnit", typeof(int));

                var result = SearchParam.Rooms.GroupBy(x => new { x.RoomName, x.NoofAdults }).Select(n => new { RoomName = n.Key.RoomName, NoofAdults = n.Key.NoofAdults, Count = n.Count() });

                int sroomNo = 1; int TotalAdult = 0; //int TotalChild = 0;
                int NoOfRoom = SearchParam.Rooms.Count();
                foreach (var item in result)//For each Item
                {
                    DataRow dtrow = dtRoomDisp.NewRow();
                    dtrow["RoomNo"] = sroomNo;
                    dtrow["NoofAdult"] = item.NoofAdults;
                    //dtrow["NoofChild"] = item.NoofChild;
                    dtrow["RoomUnit"] = item.Count;
                    if (TotalAdult > 0)
                    {
                        dtrow["TotalAdult"] = TotalAdult;
                        // dtrow["TotalChild"] = TotalChild;
                        dtrow["MaxSelect"] = TotalAdult % 2 != 0 ? 1 : TotalAdult / 2;
                        dtrow["UnitSelected"] = 0;
                    }
                    dtRoomDisp.Rows.Add(dtrow);
                    sroomNo = sroomNo + item.Count;
                }
                for (int i = 0; i < dtRoomDisp.Rows.Count; i++)
                {
                    int RNo = Convert.ToInt32(dtRoomDisp.Rows[i]["RoomNo"]);
                    NoOfA = Convert.ToInt32(dtRoomDisp.Rows[i]["NoofAdult"]);
                    int RUnit = Convert.ToInt32(dtRoomDisp.Rows[i]["RoomUnit"]);
                    for (int a = 0; a < NoOfRoom; a++) //For 12 year old child as Adult
                    {
                        if (SearchParam.Rooms[i].NoofChild > 0)
                        {
                            if (SearchParam.Rooms[i].NoofChild == 1)
                            {
                                if (SearchParam.Rooms[i].Child1Age > 11)
                                    NoOfA = NoOfA + 1;
                            }
                            if (SearchParam.Rooms[i].NoofChild == 2)
                            {
                                if (SearchParam.Rooms[i].Child1Age > 11)
                                    NoOfA = NoOfA + 1;
                                if (SearchParam.Rooms[i].Child2Age > 11)
                                    NoOfA = NoOfA + 1;
                            }
                        }
                    }

                }

                for (int i = 0; i < NoOfRoom; i++)//For 1 year old child as infant
                {
                    if (SearchParam.Rooms[i].NoofChild > 0)
                    {
                        if (SearchParam.Rooms[i].NoofChild == 1)
                        {
                            if (SearchParam.Rooms[i].Child1Age > 1)
                                ChildCount = ChildCount + 1;
                            if (SearchParam.Rooms[i].Child1Age > 11)//For 12year child remove
                                if (ChildCount > 0)
                                    ChildCount = ChildCount - 1;
                        }
                        if (SearchParam.Rooms[i].NoofChild == 2)
                        {
                            if (SearchParam.Rooms[i].Child1Age > 1)
                                ChildCount = ChildCount + 1;
                            if (SearchParam.Rooms[i].Child2Age > 1)
                                ChildCount = ChildCount + 1;
                            if (SearchParam.Rooms[i].Child1Age > 11)//For 12year child remove
                                if (ChildCount > 0)
                                    ChildCount = ChildCount - 1;
                            if (SearchParam.Rooms[i].Child2Age > 11)//For 12year child remove
                                if (ChildCount > 0)
                                    ChildCount = ChildCount - 1;
                        }
                    }
                }

                foreach (DataRow row in dtRoomData.Rows)
                {
                    row.SetField(Id, ++index);
                }
                #endregion

                for (int h = 0; h <= dtlowprice.Rows.Count - 1; h++)
                {
                    Item.ResponseType = "Success";
                    //bool NationalityFillter = false;
                    if ((dtlowprice.Rows[h]["Status"].ToString()).ToLower() != "available")
                        continue;
                    // string ItemCode = Convert.ToString(dtlowprice.Rows[h]["HotelCode"]);
                    string Hotel_id = Convert.ToString(dtlowprice.Rows[h]["Hotel_Id"]);
                    DataTable dtSubHotelData = uk_travcoResponse.Tables["SubHotelData"];
                    DataRow[] drSubHotelData = (dtSubHotelData.Select("Hotel_Id='" + Hotel_id + "'"));
                    if (drSubHotelData.Length == 0)
                        continue;
                    string NoOfExtraBeds = "0";
                    double NightPrice = 0;
                    bool chk = false;
                    double[] arr = new double[drSubHotelData.Length];
                    double FinalRate = 1;
                    #region Nationality Filter
                    bool NationalityFillter = false;
                    DataRow[] dr_Hotel = uk_travcoResponse.Tables["Hotel"].Select();
                    string Heading = "";
                    string Hotel_Code = "";
                    string Hotelcode = "";
                    string Headertext = "";
                    string countryname = "";

                    // string HotelCode = Convert.ToString(drSubHotelData[i]["HotelCode"]);
                    //for (int h = 0; h < drHotel.Length; h++)
                    // {
                    //     Hotel_Code = Convert.ToString(dr_Hotel[h]["HotelCode"]);
                    //    string body_id = Convert.ToString(uk_travcoResponse.Tables["Body"].Rows[h]["Body_Id"]);

                    //    DataRow[] dravaility = uk_travcoResponse.Tables["HotelAvailabilityV7Response"].Select("Body_Id='" + body_id + "'");
                    //   if (dravaility.Length > 0)
                    //   {
                    //     int HotelsAvailabilityV7Response_id = Convert.ToInt32(dravaility[h]["HotelAvailabilityV7Response_Id"]);
                    //   DataRow[] drresponseid = uk_travcoResponse.Tables["Response"].Select("HotelAvailabilityV7Response_Id='" + HotelsAvailabilityV7Response_id + "'");
                    //  if (drresponseid.Length > 0)
                    // {
                    //  for (int d = 0; d < drresponseid.Length; d++)
                    // {
                    //int Response_Id = Convert.ToInt32(drresponseid[d]["Response_Id"]);
                    //string Strfrom = (Convert.ToString(drdaterange[d]["from"])).Substring(6, 4) + "-" + (Convert.ToString(drdaterange[d]["from"])).Substring(3, 2) + "-" + (Convert.ToString(drdaterange[d]["from"])).Substring(0, 2);
                    //string Strto = (Convert.ToString(drdaterange[d]["to"])).Substring(6, 4) + "-" + (Convert.ToString(drdaterange[d]["to"])).Substring(3, 2) + "-" + (Convert.ToString(drdaterange[d]["to"])).Substring(0, 2);
                    //DateTime from = Convert.ToDateTime(Strfrom);
                    //DateTime to = Convert.ToDateTime(Strto);
                    //checkin = (objSearPera.CheckInDate).Substring(6, 4) + "-" + (objSearPera.CheckInDate).Substring(3, 2) + "-" + (objSearPera.CheckInDate).Substring(0, 2);
                    //checkout = (objSearPera.CheckOutDate).Substring(6, 4) + "-" + (objSearPera.CheckOutDate).Substring(3, 2) + "-" + (objSearPera.CheckOutDate).Substring(0, 2);
                    //DateTime CheckInDate = Convert.ToDateTime(checkin);
                    //DateTime CheckOutDate = Convert.ToDateTime(checkout);

                    // if (from <= CheckInDate && to >= CheckOutDate)
                    // {
                    // DataRow[] drhotelid = uk_travcoResponse.Tables["HotelDatas"].Select("Response_Id='" + Response_Id + "'");
                    // if (drhotelid.Length > 0)
                    // {
                    //   int HotelDatas_Id = Convert.ToInt32(drhotelid[d]["HotelDatas_Id"]);

                    //   DataRow[] drhId = uk_travcoResponse.Tables["HotelData"].Select("HotelDatas_Id='" + HotelDatas_Id + "'");

                    // if (drhId.Length > 0)
                    // {

                    //string Hoteldata_id = Convert.ToString(drhId[d]["HotelData_Id"]);
                    //string Countryname = Convert.ToString(drhId[d]["CountryName"]);
                    //string cityname = Convert.ToString(drhId[d]["CityName"]);
                    //string cityCode = Convert.ToString(drhId[d]["CityCode"]);
                    // string countryCode = Convert.ToString(drhId[d]["CountryCode"]);

                    //DataRow[] drhotel = uk_travcoResponse.Tables["Hotels"].Select("HotelData_Id='" + Hoteldata_id + "'");
                    //      if (drhotel.Length > 0)
                    //{
                    //string Hotels_id = Convert.ToString(drhotel[d]["Hotels_Id"]);

                    //DataRow[] drHotellist = uk_travcoResponse.Tables["Hotel"].Select("Hotels_Id='" + Hotels_id + "'");
                    //if (drHotellist.Length > 0)
                    //{
                    //string HotelName = Convert.ToString(drHotellist[d]["HotelName"]);
                    //string Hotel_id = Convert.ToString(drHotellist[d]["Hotel_Id"]);
                    //Hotelcode = Convert.ToString(drHotellist[d]["HotelCode"]);
                    DataRow[] drsubhoteldata = uk_travcoResponse.Tables["SubHotelData"].Select("Hotel_Id='" + Hotel_id + "'");
                    for (int dr = 0; dr < drsubhoteldata.Length; dr++)
                    {
                        string SubHotelData_id = Convert.ToString(drsubhoteldata[dr]["SubHotelData_Id"]);
                        if (uk_travcoResponse.Tables.Contains("ImportantInformation") && uk_travcoResponse.Tables["ImportantInformation"].Rows.Count > 0)
                        {
                            DataRow[] drinformid = uk_travcoResponse.Tables["ImportantInformation"].Select("SubHotelData_Id='" + SubHotelData_id + "'");
                            for (int drt = 0; drt < drinformid.Length; drt++)
                            {
                                string ImportantInformation_id = Convert.ToString(drinformid[drt]["ImportantInformation_Id"]);

                                DataRow[] drrestric = uk_travcoResponse.Tables["NationalityRestriction"].Select("ImportantInformation_Id='" + ImportantInformation_id + "'");
                                for (int drtt = 0; drtt < drrestric.Length; drtt++)
                                {
                                    string NationalityRestriction_id = Convert.ToString(drrestric[drtt]["NationalityRestriction_Id"]);

                                    DataRow[] drheader = uk_travcoResponse.Tables["Header"].Select("NationalityRestriction_Id='" + NationalityRestriction_id + "'");

                                    for (int tt = 0; tt < drrestric.Length; tt++)
                                    {
                                        Heading = Convert.ToString(drheader[tt]["Heading"]);
                                        Headertext = Convert.ToString(drheader[tt]["HeaderText"]);
                                        //string ISOCountryCode = Convert.ToString(drheader[dr]["ISOCountryCode"]);
                                    }
                                    DataRow[] drnationalityname = uk_travcoResponse.Tables["RestrictCountries"].Select("NationalityRestriction_Id='" + NationalityRestriction_id + "'");
                                    if (drnationalityname.Length > 0)
                                    {
                                        for (int t1 = 0; t1 < drnationalityname.Length; t1++)
                                        {
                                            string RestrictCountries_id = Convert.ToString(drnationalityname[t1]["RestrictCountries_Id"]);

                                            DataRow[] drnationalitiname = uk_travcoResponse.Tables["RestrictCountry"].Select("RestrictCountries_Id='" + RestrictCountries_id + "'");
                                            if (drnationalitiname.Length > 0)
                                            {
                                                for (int t2 = 0; t2 < drnationalitiname.Length; t2++)
                                                {
                                                    countryname = Convert.ToString(drnationalitiname[t2]["CountryName"]);

                                                    if (PaxCountryName.ToLower() == countryname.ToLower())
                                                    {
                                                        if (Heading.ToLower() == "inclusions")
                                                        {
                                                            NationalityFillter = true;
                                                            return options;
                                                        }
                                                        else if (Heading.ToLower() == "exclusions")
                                                        {
                                                            NationalityFillter = false;
                                                            return options;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        if (Heading.ToLower() == "exclusions")
                                                        {
                                                            NationalityFillter = true;
                                                            // return dshotel;
                                                        }
                                                        else if (Heading.ToLower() == "inclusions")
                                                        {
                                                            NationalityFillter = true;
                                                            return options;
                                                        }
                                                    }
                                                }

                                            }
                                            else
                                            {
                                                NationalityFillter = true;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        NationalityFillter = true;

                                    }

                                }
                            }
                            //        }


                        }

                        //}
                        // }
                        //}
                    }

                    // }
                    // }


                    // }
                    //}
                    //}

                    //if (PaxCountryName.ToLower() == countryname.ToLower())
                    //{
                    //    if (Heading.ToLower() == "inclusions")
                    //    {
                    //        NationalityFillter = true;

                    //    }
                    //    else if (Heading.ToLower() == "exclusions")
                    //    {
                    //        NationalityFillter = false;
                    //        return dshotel;

                    //    }
                    //}
                    //else
                    //{
                    //    if (Heading.ToLower() == "exclusions")
                    //    {
                    //        NationalityFillter = true;
                    //       // return dshotel;

                    //    }
                    //    else if(Heading.ToLower() == "inclusions")
                    //    {
                    //        NationalityFillter = false;
                    //        return dshotel;

                    //    }

                    //}



                    #endregion
                    //DataRow[] drhotel = dtlowprice.Select("HotelCode='" + dtlowprice.Rows[h]["HotelCode"] + "'");
                    #region "When not Need Room"  
                    // add hotel code from price only and room code blank here
                    if (SearchParam.Filters.IsShowRooms == 0)
                    {
                        #region Calculation

                        for (int i = 0; i < drSubHotelData.Length; i++)
                        {

                            int SubHotelData_Id = Convert.ToInt32(drSubHotelData[i]["SubHotelData_Id"]);
                            DataRow[] drRoomData = (dtRoomData.Select("RoomDatas_Id='" + SubHotelData_Id + "'"));
                            if (drRoomData.Length < NoOfRoom)
                                chk = true;
                            if (commonSearch.tokenData.AgentId != 1899)
                            {


                                double lo = 0;
                                double[] ar = new double[drRoomData.Length];
                                //for (int a = 0; a < drRoomData.Length; a++)
                                for (int a = 0; a < 1; a++)
                                {
                                    double TotalRate = 0;
                                    for (int x = 0; x < SearchParam.Rooms.Count(); x++)
                                    {
                                        int RoAdlt = SearchParam.Rooms[x].NoofAdults;
                                        double RoomTotalRate1 = 0;
                                        for (int xx = 0; xx < drRoomData.Length; xx++)
                                        {
                                            int RoomPax1 = Convert.ToInt32(drRoomData[xx]["RoomPax"]);
                                            if (RoAdlt == RoomPax1)
                                            {
                                                double AdultPrice1 = Convert.ToDouble(drRoomData[xx]["AdultPrice"]);
                                                double ChildPrice1 = 0;
                                                if (uk_travcoResponse.Tables["RoomData"].Columns.Contains("ChildPrice"))
                                                {
                                                    ChildPrice1 = Convert.ToDouble(drRoomData[a]["ChildPrice"]);
                                                }
                                                RoomTotalRate1 = (AdultPrice1 * RoAdlt) + (ChildPrice1 * SearchParam.Rooms[x].NoofAdults);
                                                xx = drRoomData.Length;
                                            }
                                            TotalRate = TotalRate + RoomTotalRate1;
                                        }
                                    }
                                    FinalRate = TotalRate;
                                    int RoomPax = Convert.ToInt32(drRoomData[a]["RoomPax"]);
                                    int RoomData_Id = Convert.ToInt32(drRoomData[a]["Column1"]);
                                    double TotalAdultPrice = Convert.ToDouble(drRoomData[a]["TotalAdultPrice"]);
                                    double AdultPrice = Convert.ToDouble(drRoomData[a]["AdultPrice"]);
                                    double ChildPrice = 0;
                                    if (uk_travcoResponse.Tables["RoomData"].Columns.Contains("ChildPrice"))
                                    {
                                        ChildPrice = Convert.ToDouble(drRoomData[a]["ChildPrice"]);
                                    }
                                    double TotalChildPrice = 0;

                                    if (uk_travcoResponse.Tables["RoomData"].Columns.Contains("TotalChildPrice"))
                                    {
                                        TotalChildPrice = Convert.ToString(drRoomData[a]["TotalChildPrice"]) == "" ? 0 : Convert.ToDouble(drRoomData[a]["TotalChildPrice"]);
                                    }
                                    double RoomTotalRate = (AdultPrice * NoOfA) + (ChildPrice * ChildCount);
                                    //FreeNightOffer
                                    int freeNight = 0;
                                    if (uk_travcoResponse.Tables.Contains("FreeNightsDetails"))
                                    {
                                        DataRow[] drFreeNightsDetails = uk_travcoResponse.Tables["FreeNightsDetails"].Select("RoomData_Id='" + RoomData_Id + "'");
                                        if (drFreeNightsDetails.Length > 0)
                                        {
                                            freeNight = Convert.ToInt32(drFreeNightsDetails[0]["NoOfAdultFreeNights"]);
                                        }
                                    }
                                    //--------
                                    NightPrice = RoomTotalRate;// / (SearchParam.NoofNights - freeNight);



                                    // put logic for lowest NightPrice
                                    ar[a] = NightPrice;
                                }
                                lo = ar[0];
                                for (int c = 0; c < ar.Length; c++)
                                {
                                    if (lo > ar[c])
                                        lo = ar[c];
                                }
                                arr[i] = lo;
                            }
                            //Pax filter
                            int RoomPaxx = Convert.ToInt32(drRoomData[0]["RoomPax"]);
                            if (uk_travcoResponse.Tables["RoomData"].Columns.Contains("NoOfExtraBeds"))
                            {
                                NoOfExtraBeds = Convert.ToString(drRoomData[0]["NoOfExtraBeds"]);
                                if (NoOfExtraBeds == "")
                                    NoOfExtraBeds = "0";
                            }
                            for (int r = 0; r < SearchParam.Rooms.Count(); r++)
                            {
                                if (RoomPaxx == 2)
                                {
                                    if (Convert.ToInt32(NoOfExtraBeds) < ChildCount)
                                        chk = true;
                                    //if (Child > 0)
                                    //    if (Adult != 2)
                                    //        chk = true;
                                }
                            }
                        }

                        if (chk == true)
                            continue;
                        if (commonSearch.tokenData.AgentId != 1899)
                            FinalRate = FinalRate * CurrencyFactoruk_travco;
                        #endregion

                        #region "Hotel Code Block "
                        DataRow drXmlHotel = DtHotel.NewRow();
                        // string HotelStar = Convert.ToString(drhotel[0]["HotelStar"]);
                        // HotelStar = Common.Star_Rating(HotelStar);
                        // string InternalHotelId = Convert.ToString(ItemCode);
                        drXmlHotel["XMLHotelCode"] = dtlowprice.Rows[h]["HotelCode"];
                        drXmlHotel["HotelName"] = "";
                        drXmlHotel["SupplierName"] = Item.Supplier;
                        drXmlHotel["OfficeID"] = 0;
                        drXmlHotel["ContractID"] = 0;
                        drXmlHotel["Rating"] = 0;
                        drXmlHotel["XmlCity"] = XMLCITY;
                        drXmlHotel["IsShowSupplier"] = false;
                        drXmlHotel["BookingStatus"] = "Available";
                        drXmlHotel["FinalSellingPrice"] = FinalRate;
                        drXmlHotel["SellingPrice"] = FinalRate;
                        drXmlHotel["SupplierCurrency"] = Currency;
                        if (FinalRate > 0)
                        {
                            #region "Room code Block"
                            bool willAddOrNot = true;
                            Int64 RaynaID = 0;
                            Int64 GiataId = 0;
                            int Rating = 0;
                            Int64 OwnsystemHotelId = 0;
                            double MinPrice = FinalRate;
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
                        #endregion




                    }
                    #endregion
                    #region "When Need Rooms"

                    //add Hotel code blank and rooms code in room block
                    else
                    {
                        #region Hotels

                        #region "Hotel Block here"
                        DataRow drXmlHotel = DtHotel.NewRow();
                        // string HotelStar = Convert.ToString(drhotel[0]["HotelStar"]);
                        // HotelStar = Common.Star_Rating(HotelStar);
                        // string InternalHotelId = Convert.ToString(ItemCode);
                        drXmlHotel["XMLHotelCode"] = dtlowprice.Rows[h]["HotelCode"];
                        drXmlHotel["HotelName"] = "";
                        drXmlHotel["SupplierName"] = Item.Supplier;
                        drXmlHotel["OfficeID"] = 0;
                        drXmlHotel["ContractID"] = 0;
                        drXmlHotel["Rating"] = 0;
                        drXmlHotel["XmlCity"] = XMLCITY;
                        drXmlHotel["IsShowSupplier"] = false;
                        drXmlHotel["BookingStatus"] = "Available";
                        drXmlHotel["FinalSellingPrice"] = FinalRate * CurrencyFactoruk_travco;
                        drXmlHotel["SellingPrice"] = FinalRate * CurrencyFactoruk_travco;
                        drXmlHotel["SupplierCurrency"] = Currency;
                        #endregion
                        #region "GetRooms for this hotel"
                        bool willAddOrNot = true;
                        Int64 RaynaID = 0;
                        Int64 GiataId = 0;
                        int RatingHotel = 0;
                        Int64 OwnsystemHotelId = 0;
                        string Provider = Item.Supplier;
                        double MinPrice = FinalRate;
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

                                _markup.getAgetMarkupSetting(Provider, GiataId.ToString(), commonSearch.dtSuplierMarkup, commonSearch.dtMarkupDetails, ref AgentPercentage, ref AgentMarkupAmount, ref blnIsShowSupplier, ref isPerce, commonSearch.AgentId, RatingHotel, SearchParam.CityID, SearchParam.CountryID.ToString(), SearchParam.CheckInDate);
                                int maxOcupancy = 0;
                                for (int i = 0; i < SearchParam.Rooms.Count; i++)
                                    maxOcupancy = maxOcupancy + SearchParam.Rooms[i].NoofChild + SearchParam.Rooms[i].NoofAdults;
                                #region Supplier Room Code block

                                for (int su = 0; su < drSubHotelData.Length; su++)
                                {
                                    // int SubHotelData_Id = Convert.ToInt32(drSubHotelData[su]["SubHotelData_Id"]);
                                    DataRow[] drdtRooms = uk_travcoResponse.Tables["RoomData"].Select("RoomDatas_Id='" + drSubHotelData[su]["SubHotelData_Id"] + "'");
                                    // string HotelCode = Convert.ToString(drSubHotelData[su]["HotelCode"]);

                                    //string CurrencyCode = Convert.ToString(drSubHotelData[su]["CurrencyCode"]);
                                    //string Satus = Convert.ToString(drSubHotelData[su]["Status"]);
                                    if (Convert.ToString(drSubHotelData[su]["Status"]).ToLower() != "available")
                                        continue;
                                    if (Convert.ToString(drSubHotelData[su]["CurrencyCode"]).ToLower() != "usd")
                                    {
                                        continue;

                                        //if (CurrencyCode.ToLower() == "pds")
                                        //    CurrencyCode = "GBP";
                                        //DataTable dtInrCurrency = Common.GetCountryCurrency(CurrencyCode);
                                        //CurrencyFactoruk_travco = Convert.ToDouble(dtInrCurrency.Rows[0]["AmtToExch"]);

                                    }

                                    string EssentialInformation = "Note: Child Age:12 or more considered as an adult ";
                                    for (int RT_Type = 0; RT_Type < RT_Count; RT_Type++)
                                    {
                                        #region AdultChildLogic
                                        int RoomNo = SearchParam.Rooms[RT_Type].RoomNo;
                                        int NoOfChild = SearchParam.Rooms[RT_Type].NoofChild;
                                        int ChildAge1 = SearchParam.Rooms[RT_Type].Child1Age;
                                        int ChildAge2 = SearchParam.Rooms[RT_Type].Child2Age;
                                        int NoOfAdult = SearchParam.Rooms[RT_Type].NoofAdults;

                                        int ChildNew = ChildCheck(NoOfAdult, NoOfChild, ChildAge1, ChildAge2);
                                        int AdultNew = AdultCheck(NoOfAdult, NoOfChild, ChildAge1, ChildAge2);
                                        #endregion

                                        for (int DtRooms = 0; DtRooms <= drdtRooms.GetLength(0) - 1; DtRooms++)  // This block for Room wise 
                                        {
                                            #region RateBreakUp
                                            //int RoomData_Id = Convert.ToInt32(drdtRooms[DtRooms]["Column1"]);
                                            // int RoomDatas_Id = Convert.ToInt32(drdtRooms[DtRooms]["RoomDatas_Id"]);

                                            DataTable dtRatePlanDetails = uk_travcoResponse.Tables["RatePlanDetails"];
                                            DataRow[] drRatePlanDetails = (dtRatePlanDetails.Select("SubHotelData_Id='" + drdtRooms[DtRooms]["RoomDatas_Id"] + "'"));
                                            string RatePlanDescription = Convert.ToString(drRatePlanDetails[0]["RatePlanDescription"]);

                                            string RoomPax = Convert.ToString(drdtRooms[DtRooms]["RoomPax"]);
                                            string PriceCode = Convert.ToString(drdtRooms[DtRooms]["PriceCode"]);

                                            //Filter Pax
                                            if (AdultNew != Convert.ToInt32(drdtRooms[DtRooms]["RoomPax"]))
                                                continue;
                                            NoOfExtraBeds = "0";
                                            if (uk_travcoResponse.Tables["RoomData"].Columns.Contains("NoOfExtraBeds"))
                                            {
                                                NoOfExtraBeds = Convert.ToString(drdtRooms[DtRooms]["NoOfExtraBeds"]);
                                                if (NoOfExtraBeds == "")
                                                    NoOfExtraBeds = "0";
                                            }
                                            //child filter
                                            if (Convert.ToInt32(drdtRooms[DtRooms]["RoomPax"]) == 2)
                                                if (Convert.ToInt32(NoOfExtraBeds) < ChildNew)
                                                    continue;

                                            double TotalRate = Convert.ToDouble(drdtRooms[DtRooms]["TotalAdultPrice"]);
                                            if (uk_travcoResponse.Tables["RoomData"].Columns.Contains("TotalChildPrice"))
                                            {
                                                string CRate = Convert.ToString(drdtRooms[DtRooms]["TotalChildPrice"]);
                                                if (CRate == "")
                                                    CRate = "0";
                                                TotalRate = TotalRate + Convert.ToDouble(CRate);
                                            }
                                            //For Free NightOffer
                                            int NoOfAdultFreeNights = 0;
                                            int NoOfChildFreeNights = 0;
                                            string Offer = "";
                                            string offerText = "";
                                            if (uk_travcoResponse.Tables.Contains("FreeNightsDetails"))
                                            {
                                                string[] offerTxtArr = Convert.ToString(drdtRooms[DtRooms]["AdultPriceDetails"]).Split(';');
                                                if (offerTxtArr.Length > 1)
                                                {
                                                    Offer = "Special Offer";
                                                    offerText = offerTxtArr[1] + "</br>";
                                                    DataRow[] drFreeNightsDetails = (uk_travcoResponse.Tables["FreeNightsDetails"].Select("RoomData_Id='" + drdtRooms[DtRooms]["Column1"] + "'"));
                                                    if (drFreeNightsDetails.Length > 0)
                                                    {
                                                        NoOfAdultFreeNights = Convert.ToInt32(drFreeNightsDetails[0]["NoOfAdultFreeNights"]);
                                                        if (uk_travcoResponse.Tables["FreeNightsDetails"].Columns.Contains("NoOfChildFreeNights"))
                                                        {
                                                            NoOfChildFreeNights = Convert.ToInt32(drFreeNightsDetails[0]["NoOfAdultFreeNights"]);
                                                        }
                                                    }
                                                }

                                            }
                                            //For ReducedPrice
                                            double AdultReductionAmount = 0;
                                            double AdultReductionPer = 0;
                                            double ChildReductionAmount = 0;
                                            double ChildReductionPer = 0;
                                            double ReducedPrice = 0;
                                            double ReducedPer = 0;
                                            if (uk_travcoResponse.Tables.Contains("ReducedPriceDetails"))
                                            {
                                                if (uk_travcoResponse.Tables["ReducedPriceDetails"].Columns.Contains("AdultReductionAmount"))
                                                {
                                                    DataRow[] drReducedPriceDetails = (uk_travcoResponse.Tables["ReducedPriceDetails"].Select("RoomData_Id = '" + drdtRooms[DtRooms]["Column1"] + "'"));
                                                    if (drReducedPriceDetails.Length > 0)
                                                    {
                                                        AdultReductionAmount = (Convert.ToDouble(drReducedPriceDetails[0]["AdultReductionAmount"])) * AdultNew;
                                                        AdultReductionPer = (Convert.ToDouble(drReducedPriceDetails[0]["AdultReductionPercentage"]));
                                                        if (uk_travcoResponse.Tables["ReducedPriceDetails"].Columns.Contains("ChildReductionAmount"))
                                                        {
                                                            ChildReductionAmount = (Convert.ToString(drReducedPriceDetails[0]["ChildReductionAmount"]) == "" ? 0 : (Convert.ToDouble(drReducedPriceDetails[0]["ChildReductionAmount"]))) * ChildNew;
                                                            ChildReductionPer = (Convert.ToString(drReducedPriceDetails[0]["ChildReductionPercentage"]) == "" ? 0 : (Convert.ToDouble(drReducedPriceDetails[0]["ChildReductionPercentage"])));
                                                        }
                                                    }
                                                    ReducedPer = AdultReductionPer;
                                                    ReducedPrice = AdultReductionAmount + ChildReductionAmount;
                                                }
                                            }
                                            //--
                                            double AdultPrice = Convert.ToDouble(drdtRooms[DtRooms]["AdultPrice"]);
                                            double ChildPrice = Convert.ToDouble(drdtRooms[DtRooms]["ChildPrice"]);
                                            double RoomTotalRate = (AdultPrice * AdultNew) + (ChildPrice * ChildNew);
                                            double Rate = RoomTotalRate * CurrencyFactoruk_travco;// AdultPrice + ChildPrice;
                                            double RatePerNight = 0;

                                            #region EBD Offer
                                            double EBDAmount = 0;
                                            string EBDPercent = "";

                                            if (ReducedPrice > 0)
                                            {
                                                offerText = offerText + "ReducedPrice: " + ReducedPer + "% </br>";
                                                Offer = "Special Offer";
                                            }
                                            if (uk_travcoResponse.Tables.Contains("EarlyBirdDetails"))
                                            {
                                                if (uk_travcoResponse.Tables["EarlyBirdDetails"].Columns.Contains("Percentage"))
                                                {
                                                    DataRow[] drEBD = (uk_travcoResponse.Tables["EarlyBirdDetails"].Select("RoomData_Id = '" + drdtRooms[DtRooms]["Column1"] + "'"));
                                                    if (drEBD.Length > 0)
                                                    {
                                                        EBDAmount = Convert.ToDouble(drEBD[0]["ReductionAmount"]);
                                                        EBDPercent = Convert.ToString(drEBD[0]["Percentage"]);
                                                        if (offerText != "")
                                                            offerText = offerText + "+ Include Early Bird Discount:" + EBDPercent + "%";
                                                        else
                                                            offerText = offerText + "Include Early Bird Discount:" + EBDPercent + "%";
                                                        Offer = "Special Offer";
                                                    }
                                                }
                                            }

                                            #endregion

                                            if (ReducedPrice > 0)
                                            {
                                                ReducedPrice = (ReducedPrice + RoomTotalRate) * CurrencyFactoruk_travco;
                                            }

                                            if (NoOfAdultFreeNights > 0)
                                                RatePerNight = (Rate / (SearchParam.NoofNights - NoOfAdultFreeNights));
                                            else
                                                RatePerNight = (Rate / SearchParam.NoofNights);

                                            string RoomName = Convert.ToString(drdtRooms[DtRooms]["RoomName"]);
                                            string RoomCode = Convert.ToString(drdtRooms[DtRooms]["RoomCode"]);
                                            if (RoomName.Split('-').Length > 1)
                                                RatePlanDescription = "[" + RoomName.Split('-')[0].Replace("Room", "").Trim() + "]" + RatePlanDescription;
                                            //for (int i = 0; i < SearchParam.NoofNights; i++)
                                            //{
                                            //    double NightRate = RatePerNight;
                                            //    if (NoOfAdultFreeNights > 0 && i == (SearchParam.NoofNights - NoOfAdultFreeNights))
                                            //        NightRate = 0;

                                            //    DateTime FromDate = DateFrom.AddDays(i);
                                            //    DataRow druk_travcoRateBreackUp = DtRoomRateBreackUp.NewRow();
                                            //    druk_travcoRateBreackUp["SupplierName"] = "uk_travco";
                                            //    druk_travcoRateBreackUp["OwnsystemHotelId"] = OwnsystemHotelId;
                                            //    druk_travcoRateBreackUp["ContractID"] = 0;
                                            //    druk_travcoRateBreackUp["RoomNo"] = RoomNo;
                                            //    druk_travcoRateBreackUp["RoomTypeId"] = RoomCode + "$" + HotelCode + "$" + PriceCode + "$" + RoomNo;//Opt + "$" + SearchParam.CountryId;
                                            //    druk_travcoRateBreackUp["Meal"] = "";
                                            //    string rateWeekColumn = Convert.ToString(FromDate.Date.DayOfWeek).Substring(0, 3);
                                            //    druk_travcoRateBreackUp["DateOfWeek"] = rateWeekColumn;

                                            //    //--MarkUp  
                                            //    double VatOnRateBrackUp = 0;
                                            //    NightRate = Common.ProcessHotelRateBreakupPrice_WithMarkup(1, SearchParam.CityID, SearchParam.CountryID.ToString(), NightRate, commonSearch.B2CMarkup, commonSearch.IsParentId, isPerce, AgentPercentage, AgentMarkupAmount, commonSearch.SubAgentMarkup, ref VatOnRateBrackUp);

                                            //    druk_travcoRateBreackUp["MealId"] = "";
                                            //    druk_travcoRateBreackUp["Rate"] = NightRate;
                                            //    druk_travcoRateBreackUp["OrderNo"] = 1;
                                            //    druk_travcoRateBreackUp["RateType"] = "A Rate";
                                            //    druk_travcoRateBreackUp["RoomTypeName"] = RatePlanDescription;
                                            //    druk_travcoRateBreackUp["DateOfWeek"] = rateWeekColumn;
                                            //    druk_travcoRateBreackUp["Date"] = FromDate;
                                            //    druk_travcoRateBreackUp["BookingStatus"] = "Available";
                                            //    druk_travcoRateBreackUp["BoardTypeID"] = "";
                                            //    druk_travcoRateBreackUp["VatdiffAmount"] = VatOnRateBrackUp;
                                            //    druk_travcoRateBreackUp["SupplierCurrency"] = Currency;
                                            //    druk_travcoRateBreackUp["BuyingPrice"] = NightRate / CurrencyFactoruk_travco;
                                            //    DtRoomRateBreackUp.Rows.Add(druk_travcoRateBreackUp);

                                            //}
                                            #endregion

                                            #region Rooms
                                            if (true)
                                            {
                                                // string OfferCode = "";
                                                double ItemPrice = Rate;
                                                DataRow druk_travcoHotelRoom = dtHotelRoom.NewRow();
                                                druk_travcoHotelRoom["OwnsystemHotelId"] = OwnsystemHotelId;
                                                druk_travcoHotelRoom["ContractID"] = 0;
                                                druk_travcoHotelRoom["UniqueNo"] = OwnsystemHotelId;
                                                druk_travcoHotelRoom["RoomSequenceNo"] = "";
                                                string MealName = RoomName;
                                                if (RoomName.Split('-').Length > 1)
                                                    MealName = RoomName.Split('-')[1];
                                                druk_travcoHotelRoom["MealName"] = MealName.Trim();
                                                druk_travcoHotelRoom["allocationDetails"] = "";
                                                druk_travcoHotelRoom["minStay"] = "";
                                                druk_travcoHotelRoom["DateApplyMinStay"] = "";
                                                druk_travcoHotelRoom["RoomNo"] = RoomNo;
                                                druk_travcoHotelRoom["EssentialInformation"] = EssentialInformation;
                                                druk_travcoHotelRoom["MealId"] = "";
                                                druk_travcoHotelRoom["RoomTypeName"] = RatePlanDescription;
                                                druk_travcoHotelRoom["RoomTypeId"] = RoomCode + "$" + Convert.ToString(drSubHotelData[su]["HotelCode"]) + "$" + PriceCode + "$" + RoomNo;
                                                druk_travcoHotelRoom["BookingStatus"] = "Available";
                                                druk_travcoHotelRoom["AdultsOccupancy"] = RoomPax;
                                                druk_travcoHotelRoom["ChildOccupancy"] = 0;
                                                druk_travcoHotelRoom["MaxExtrabed"] = NoOfExtraBeds;
                                                druk_travcoHotelRoom["RoomTypeCode"] = RoomCode;
                                                druk_travcoHotelRoom["BoardTypeID"] = "";
                                                druk_travcoHotelRoom["CompanyBuyingPrice"] = ItemPrice;
                                                druk_travcoHotelRoom["OfferDescription"] = offerText;
                                                druk_travcoHotelRoom["offerCode"] = Offer;
                                                if (ReducedPrice > 0)
                                                {
                                                    double VatOnRateBrackUp = 0;
                                                    ReducedPrice = _markup.ProcessHotelRateBreakupPrice_WithMarkup(1, SearchParam.CityID, SearchParam.CountryID.ToString(), ReducedPrice, commonSearch.B2CMarkup, commonSearch.IsParentId, isPerce, AgentPercentage, AgentMarkupAmount, commonSearch.SubAgentMarkup, ref VatOnRateBrackUp);
                                                    druk_travcoHotelRoom["GrossWitohutDiscount"] = ReducedPrice;
                                                }
                                                _markup.ProcessHotelRoom_WithMarkup(CurrencyFactoruk_travco, Convert.ToString(drSubHotelData[su]["CurrencyCode"]), false, false, SearchParam.NoofNights, SearchParam.CityID, SearchParam.CountryID.ToString(), SearchParam.CheckInDate, SearchParam.CheckOutDate, ref ItemPrice, ref druk_travcoHotelRoom, isPerce, AgentPercentage, AgentMarkupAmount, commonSearch.SubAgentMarkup, commonSearch.B2CMarkup, commonSearch.IsParentId);

                                                druk_travcoHotelRoom["SupplierName"] = Item.Supplier;
                                                dtHotelRoom.Rows.Add(druk_travcoHotelRoom);
                                            }
                                            #endregion

                                            #region CancellationPolicy
                                            double CancelCharge = 0;
                                            var datetime = DateTime.Now;
                                            DateTime NowDate = System.DateTime.Now;
                                            DateTime CancellationDate = System.DateTime.Now;
                                            //try
                                            //{
                                            //    DataTable dtDetail = uk_travcoResponse.Tables["Detail"];
                                            //    DataRow[] drDetail = (dtDetail.Select("FullCancellationDetails_Id='" + RoomDatas_Id + "'"));
                                            //    int FullCancellationDetails_Id = Convert.ToInt32(drDetail[0]["FullCancellationDetails_Id"]);
                                            //    int Detail_Id = Convert.ToInt32(drDetail[0]["Detail_Id"]);
                                            //    string FullCancellationPolicy = Convert.ToString(drDetail[0]["FullCancellationPolicy"]);
                                            //    string[] arr = FullCancellationPolicy.Split('-');
                                            //    string NightCharge = arr[1].Substring(1, 1);
                                            //    string[] arr2 = arr[1].Split('x');
                                            //    string Percent = arr2[1].Replace("%", "").Replace(" ", "");
                                            //    if (NoOfAdultFreeNights > 0)
                                            //        CancelCharge = RatePerNight * (Convert.ToInt32(NightCharge) - NoOfAdultFreeNights);
                                            //    else
                                            //        CancelCharge = RatePerNight * Convert.ToInt32(NightCharge);
                                            //    if (NoNight < Convert.ToInt32(NightCharge))
                                            //    {
                                            //        CancelCharge = Rate;
                                            //    }
                                            //    CancelCharge = ((CancelCharge / 100) * Convert.ToDouble(Percent));
                                            //    DataTable dtMessage = uk_travcoResponse.Tables["Message"];
                                            //    DataRow[] drMessage = (dtMessage.Select("Detail_Id='" + Detail_Id + "'"));
                                            //    //string LastDateByWhichToCancel = Convert.ToString(drMessage[0]["LastDateByWhichToCancel"]);
                                            //    //CancellationDate = DateTime.ParseExact(LastDateByWhichToCancel, "dd/MM/yy", null);
                                            //    DateTime LastDate = Convert.ToDateTime(drMessage[0]["LastDateByWhichToCancel"]);
                                            //    CancellationDate = LastDate;
                                            //}
                                            //catch (Exception ex)
                                            //{
                                            //    //ExceptionManager.LogException(ex);
                                            //    CancelCharge = Rate;
                                            //    CancellationDate = System.DateTime.Now;
                                            //}
                                            try
                                            {
                                                /////
                                                DataRow[] drCanDetail = (uk_travcoResponse.Tables["CancellationDetails"].Select("SubHotelData_Id='" + drdtRooms[DtRooms]["RoomDatas_Id"] + "'"));
                                                if (drCanDetail.Count() > 0)
                                                {
                                                    //int CancellationDetails_Id = Convert.ToInt32(drCanDetail[0]["CancellationDetails_Id"]);
                                                    DataRow[] drFullCanDetail = (uk_travcoResponse.Tables["FullCancellationDetails"].Select("CancellationDetails_Id='" + drCanDetail[0]["CancellationDetails_Id"] + "'"));
                                                    //int FullCancellationDetails_Id = Convert.ToInt32(drFullCanDetail[0]["FullCancellationDetails_Id"]);
                                                    /////
                                                    DataTable dtDetail = uk_travcoResponse.Tables["Detail"];
                                                    DataRow[] drDetail = (dtDetail.Select("FullCancellationDetails_Id='" + drFullCanDetail[0]["FullCancellationDetails_Id"] + "'"));
                                                    //int FullCancellationDetails_Id = Convert.ToInt32(drDetail[0]["FullCancellationDetails_Id"]);
                                                    int Detail_Id = Convert.ToInt32(drDetail[0]["Detail_Id"]);
                                                    string FullCancellationPolicy = Convert.ToString(drDetail[0]["FullCancellationPolicy"]);
                                                    string[] arr1 = FullCancellationPolicy.Split('-');
                                                    string NightCharge = arr1[1].Substring(1, 1);
                                                    string[] arr2 = arr1[1].Split('x');
                                                    string Percent = arr2[1].Replace("%", "").Replace(" ", "");
                                                    if (NoOfAdultFreeNights > 0)
                                                        CancelCharge = RatePerNight * (Convert.ToInt32(NightCharge) - NoOfAdultFreeNights);
                                                    else
                                                        CancelCharge = RatePerNight * Convert.ToInt32(NightCharge);
                                                    if (SearchParam.NoofNights < Convert.ToInt32(NightCharge))
                                                    {
                                                        CancelCharge = Rate;
                                                    }
                                                    //CancelCharge = ((CancelCharge / 100) * Convert.ToDouble(Percent));
                                                    CancelCharge = Rate; // ApplyFull charge
                                                    DataTable dtMessage = uk_travcoResponse.Tables["Message"];
                                                    DataRow[] drMessage = (dtMessage.Select("Detail_Id='" + drDetail[0]["Detail_Id"] + "'"));
                                                    //string LastDateByWhichToCancel = Convert.ToString(drMessage[0]["LastDateByWhichToCancel"]);
                                                    //CancellationDate = DateTime.ParseExact(LastDateByWhichToCancel, "dd/MM/yy", null);
                                                    DateTime LastDate = Convert.ToDateTime(drMessage[0]["LastDateByWhichToCancel"]);
                                                    CancellationDate = LastDate;
                                                }
                                                else
                                                {
                                                    CancelCharge = Rate;
                                                    CancellationDate = System.DateTime.Now;
                                                }
                                            }
                                            catch (Exception ex)
                                            {
                                                // ExceptionManager.LogException(ex);
                                                CancelCharge = Rate;
                                                CancellationDate = System.DateTime.Now;
                                            }
                                            //CancellationDate = System.DateTime.Now;


                                            #region Vat
                                            double VatOnPolicy = 0;
                                            CancelCharge = CancelCharge * CurrencyFactoruk_travco;
                                            double VatOnPolicyCancelCharge = CancelCharge;
                                            double CompanyChargeAmount = CancelCharge;
                                            CancelCharge = _markup.ProcessHotelCancelationPrice_WithMarkup(ref VatOnPolicyCancelCharge, SearchParam.NoofNights, SearchParam.CityID, SearchParam.CountryID.ToString(), CancelCharge, isPerce, commonSearch.B2CMarkup, AgentPercentage, AgentMarkupAmount, commonSearch.IsParentId, commonSearch.SubAgentMarkup, ref VatOnPolicy);
                                            #endregion

                                            if (CancellationDate.AddDays(CutOffDays).Date > DateTime.Now.Date)
                                                CancellationDate = CancellationDate.AddDays(CutOffDays).Date;
                                            else
                                                CancellationDate = DateTime.Now.Date;
                                            if (NowDate.Date < CancellationDate.Date)
                                            {
                                                DataRow druk_travcoCancellation = dtCancellation.NewRow();
                                                druk_travcoCancellation["SupplierName"] = Item.Supplier;
                                                druk_travcoCancellation["OwnsystemHotelId"] = OwnsystemHotelId;
                                                druk_travcoCancellation["RoomNo"] = RoomNo;
                                                druk_travcoCancellation["RoomTypeId"] = RoomCode + "$" + Convert.ToString(drSubHotelData[su]["HotelCode"]) + "$" + PriceCode + "$" + RoomNo;
                                                druk_travcoCancellation["MealId"] = "";
                                                druk_travcoCancellation["FromDate"] = DateTime.Now.Date;
                                                druk_travcoCancellation["ToDate"] = CancellationDate.AddDays(-1).AddHours(23).AddMinutes(59).AddSeconds(59);
                                                druk_travcoCancellation["CompanyCancellationPrice"] = 0;
                                                druk_travcoCancellation["AgentCancellationPrice"] = 0;
                                                druk_travcoCancellation["RoomTypeName"] = RatePlanDescription;
                                                druk_travcoCancellation["RoomSequenceNo"] = "";
                                                druk_travcoCancellation["WithInCancellationDeadLine"] = "NO";
                                                druk_travcoCancellation["SupplierCurrency"] = Currency;
                                                druk_travcoCancellation["BuyingPrice"] = 0;
                                                dtCancellation.Rows.Add(druk_travcoCancellation);
                                                dtCancellation.AcceptChanges();

                                                DataRow druk_travcoCancellation_Temp = dtCancellation.NewRow();
                                                druk_travcoCancellation_Temp["SupplierName"] = Item.Supplier;
                                                druk_travcoCancellation_Temp["OwnsystemHotelId"] = OwnsystemHotelId;
                                                druk_travcoCancellation_Temp["RoomNo"] = RoomNo;
                                                druk_travcoCancellation_Temp["RoomTypeId"] = RoomCode + "$" + Convert.ToString(drSubHotelData[su]["HotelCode"]) + "$" + PriceCode + "$" + RoomNo;
                                                druk_travcoCancellation_Temp["MealId"] = "";
                                                druk_travcoCancellation_Temp["FromDate"] = CancellationDate.Date;
                                                druk_travcoCancellation_Temp["ToDate"] = DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null);
                                                druk_travcoCancellation_Temp["CompanyCancellationPrice"] = VatOnPolicyCancelCharge;
                                                druk_travcoCancellation_Temp["AgentCancellationPrice"] = CancelCharge;
                                                druk_travcoCancellation_Temp["RoomTypeName"] = RatePlanDescription;
                                                druk_travcoCancellation_Temp["RoomSequenceNo"] = "";
                                                druk_travcoCancellation_Temp["WithInCancellationDeadLine"] = "NO";
                                                druk_travcoCancellation_Temp["SupplierCurrency"] = Currency;
                                                druk_travcoCancellation_Temp["BuyingPrice"] = CompanyChargeAmount;
                                                dtCancellation.Rows.Add(druk_travcoCancellation_Temp);
                                                dtCancellation.AcceptChanges();
                                            }
                                            else
                                            {

                                                DataRow druk_travcoCancellation_Temp = dtCancellation.NewRow();
                                                druk_travcoCancellation_Temp["SupplierName"] = Item.Supplier;
                                                druk_travcoCancellation_Temp["OwnsystemHotelId"] = OwnsystemHotelId;
                                                druk_travcoCancellation_Temp["RoomNo"] = RoomNo;
                                                druk_travcoCancellation_Temp["RoomTypeId"] = RoomCode + "$" + Convert.ToString(drSubHotelData[su]["HotelCode"]) + "$" + PriceCode + "$" + RoomNo;
                                                druk_travcoCancellation_Temp["MealId"] = "";
                                                druk_travcoCancellation_Temp["FromDate"] = DateTime.Now.Date;
                                                druk_travcoCancellation_Temp["ToDate"] = DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null);
                                                druk_travcoCancellation_Temp["CompanyCancellationPrice"] = VatOnPolicyCancelCharge;
                                                druk_travcoCancellation_Temp["AgentCancellationPrice"] = CancelCharge;
                                                druk_travcoCancellation_Temp["RoomTypeName"] = RatePlanDescription;
                                                druk_travcoCancellation_Temp["RoomSequenceNo"] = "";
                                                druk_travcoCancellation_Temp["WithInCancellationDeadLine"] = "YES";
                                                druk_travcoCancellation_Temp["SupplierCurrency"] = Currency;
                                                druk_travcoCancellation_Temp["BuyingPrice"] = CompanyChargeAmount;
                                                dtCancellation.Rows.Add(druk_travcoCancellation_Temp);
                                                dtCancellation.AcceptChanges();
                                            }
                                            #endregion
                                        }
                                    }
                                }


                                #endregion

                            }
                            #endregion
                            Options ops = (await _supplierRes.GetCommonRoomForSuppliers(GiataId, commonSearch.tokenData, commonSearch.advancedOptions, MinPrice, drXmlHotel, dtHotelRoom, dtCancellation, DtRoomRateBreackUp, SearchParam));
                            if (ops != null)
                                options.Add(ops);
                        }
                        #endregion
                        #endregion
                    }

                    #endregion

                }
                // DtHotel.DefaultView.RowFilter = "FinalSellingPrice>0";
                // if (DtHotel.DefaultView.Count > 0)
                return options;//.DefaultView.ToTable();
                               // else
                               // return Common.GenerateDataTableNewV2();

            }
            catch (Exception ex)
            {
                Item.ResponseStatus = 404;
                await _exception.Error(ex);
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
                        
                        SearchParam.SupplierTime += ",|Finish:Uk_Travco Process Response  " + "|Time :" + timerroomx.Elapsed.ToString();
                        _= Task.Run(() => _commonMethod.FireAway_SQL_ResPorcessTimeLog("", timerroomx.Elapsed.ToString(), "Uk_Travco", SearchParam.SearchKey, 1, SearchParam.AgentID));
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


        #region "uk_travco"
        public static int AdultCheck(int NoOfAdult, int NoOfChild, int ChildAge1, int ChildAge2)
        {
            if (NoOfChild == 1)
            {
                if (ChildAge1 > 11)
                {
                    NoOfAdult = NoOfAdult + 1;
                    NoOfChild = NoOfChild - 1;
                }
            }
            if (NoOfChild == 2)
            {
                if (ChildAge1 > 11)
                {
                    NoOfAdult = NoOfAdult + 1;
                    NoOfChild = NoOfChild - 1;
                }
                if (ChildAge2 > 11)
                {
                    NoOfAdult = NoOfAdult + 1;
                    NoOfChild = NoOfChild - 1;
                }
            }
            return NoOfAdult;
        }
        public static int ChildCheck(int NoOfAdult, int NoOfChild, int ChildAge1, int ChildAge2)
        {
            if (NoOfChild == 1)
            {
                if (ChildAge1 > 11)
                {
                    NoOfAdult = NoOfAdult + 1;
                    NoOfChild = NoOfChild - 1;
                }
            }
            if (NoOfChild == 2)
            {
                if (ChildAge1 > 11)
                {
                    NoOfAdult = NoOfAdult + 1;
                    NoOfChild = NoOfChild - 1;
                }
                if (ChildAge2 > 11)
                {
                    NoOfAdult = NoOfAdult + 1;
                    NoOfChild = NoOfChild - 1;
                }
            }
            return NoOfChild;
        }
        public string uk_travcoRoomType(HotelSearchParameter SearchParam, int NoOfRoom)
        {
            string RType = "";
            switch (Convert.ToInt32(SearchParam.Rooms[NoOfRoom - 1].NoofAdults))//<-----[0] for 1st room only
            {
                case 1:
                    RType = "SingleRoom";
                    break;
                case 2:
                    RType = "DoubleRoom";
                    break;
                case 3:
                    RType = "TripleRoom";
                    break;
                default:
                    RType = "QuadRoom";
                    break;
            }
            return RType;
        }
        public string Star_Rating(string HotelStar)
        {
            switch (HotelStar)
            {
                case "1 Star": { HotelStar = "1"; } break;
                case "1 Star +": { HotelStar = "2"; } break;
                case "2 Star": { HotelStar = "2"; } break;
                case "2 Star +": { HotelStar = "3"; } break;
                case "3 Star": { HotelStar = "3"; } break;
                case "3 Star +": { HotelStar = "4"; } break;
                case "4 Star": { HotelStar = "4"; } break;
                case "4 Star +": { HotelStar = "5"; } break;
                case "5 Star": { HotelStar = "5"; } break;
                case "5 Star +": { HotelStar = "5"; } break;
            }
            return HotelStar;
        }
        public int RoomTypeFilter(int RoomType, int Pax)
        {
            switch (RoomType)
            {
                case 1:
                    Pax = 1;
                    break;
                case 2:
                    Pax = 2;
                    break;
                case 3:
                    Pax = 3;
                    break;
                default:
                    Pax = 4;
                    break;
            }
            return Pax;
        }

        #endregion

        public async Task<PriceCheckDto> AllocationEnqiry(string AgentCode, string AgentPassword, string HotelCode, string RCode, int NoOfAdults, int NoOfChild, string CheckInDate, string CheckOutDate, string uk_travco_URL, double Rate,double FinalPrice, int UserCartId, int UserCartDetailId,DataTable dtCheckPriceReqStructure, int j, string UniqueNo, string AgentUniqueNo, bool isForBooking, int ChildAge1, int ChildAge2, string Searchkey = "", int AgentId = 0,string Supplier="")
        {
            PriceCheckDto priceCheckDto = new PriceCheckDto();

            string responsetype = "Success";
            string time = "";
            string SupplierName = Supplier;
            string Error = "";
            string Req = "";
            string ResponseData = "";

            int flag = 1;
            try
            {
                int Adult = 0;
                int Child = 0;

                if (NoOfChild == 1)
                {
                    if (ChildAge1 > 1)//remove child age1
                        Child = 1;
                    if (ChildAge1 > 11)//For age12 child to Adult and remove child of 12age
                    {
                        Adult = Adult + 1;
                        if (Child > 0)
                            Child = Child - 1;
                    }

                }
                if (NoOfChild == 2)
                {
                    if (ChildAge1 > 1)
                        Child = 1;
                    if (ChildAge2 > 1)
                        Child = Child + 1;
                    if (ChildAge1 > 11)
                    {
                        Adult = Adult + 1;
                        if (Child > 0)
                            Child = Child - 1;
                    }
                    if (ChildAge2 > 11)
                    {
                        Adult = Adult + 1;
                        if (Child > 0)
                            Child = Child - 1;
                    }
                }
                NoOfAdults = NoOfAdults + Adult;
                DataTable dtInrCurrency = _commonMethod.GetCountryCurrency("USD");
                double CurrencyFactoruk_travco = Convert.ToDouble(dtInrCurrency.Rows[0]["AmtToExch"]);
                CheckInDate = CheckInDate.Substring(0, 4) + "-" + CheckInDate.Substring(4, 2) + "-" + CheckInDate.Substring(6, 2);
                CheckOutDate = CheckOutDate.Substring(0, 4) + "-" + CheckOutDate.Substring(4, 2) + "-" + CheckOutDate.Substring(6, 2);
                DateTime CIn = DateTime.ParseExact(CheckInDate, "yyyy-MM-dd", null);
                DateTime COut = DateTime.ParseExact(CheckOutDate, "yyyy-MM-dd", null);
                int NoOfNight = Convert.ToInt32((COut - CIn).TotalDays);
                StringBuilder AllocationEnquiryRQ = new StringBuilder();
                AllocationEnquiryRQ.Append("<soapenv:Envelope xmlns:soapenv=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:req=\"http://www.travco.co.uk/trlink/xsd/allocationenquiryv7/request\">");
                AllocationEnquiryRQ.Append("<soapenv:Header/>");
                AllocationEnquiryRQ.Append("<soapenv:Body>");
                AllocationEnquiryRQ.Append("<req:getAllocationEnquiry>");
                AllocationEnquiryRQ.Append("<req:RequestBase AgentCode=\"" + AgentCode + "\" AgentPassword=\"" + AgentPassword + "\" Lang=\"en-GB\"/>");
                AllocationEnquiryRQ.Append("<req:EnquiryNo>1</req:EnquiryNo>");
                AllocationEnquiryRQ.Append("<req:HotelCode>" + HotelCode + "</req:HotelCode>");
                AllocationEnquiryRQ.Append("<req:NoOfAdults>" + NoOfAdults + "</req:NoOfAdults>");
                if (Child > 0)
                    AllocationEnquiryRQ.Append("<req:NoOfChildren>" + Child + "</req:NoOfChildren>");
                AllocationEnquiryRQ.Append("<req:CheckInDate>" + CheckInDate + "</req:CheckInDate>");
                AllocationEnquiryRQ.Append("<req:Duration>" + NoOfNight + "</req:Duration>");
                AllocationEnquiryRQ.Append("<req:NoOfPaxInRoom>" + NoOfAdults + "</req:NoOfPaxInRoom>");
                AllocationEnquiryRQ.Append("<req:AdditionalData NeedReductionAmount=\"yes\" NeedHotelMessages=\"no\" NeedFreeNightDetails=\"yes\"/>");
                AllocationEnquiryRQ.Append("</req:getAllocationEnquiry>");
                AllocationEnquiryRQ.Append("</soapenv:Body>");
                AllocationEnquiryRQ.Append("</soapenv:Envelope>");
                Req = AllocationEnquiryRQ.ToString();
                _commonMethod.ThirdPartyBookingHistory(Convert.ToString(UniqueNo), Convert.ToString(AgentUniqueNo), Req, "-", "Before Get Price "+ SupplierName + " Request hit booking" + j + "isForBooking" + isForBooking, SupplierName, "Response Confirm", "-", "API Out");
                if (isForBooking == false)// hit if not from booking
                {
                    dtCheckPriceReqStructure.Rows.Add(_commonMethod.AddCheckPriceReq(dtCheckPriceReqStructure.NewRow(), j + 1, UserCartId, UserCartDetailId, RCode, SupplierName, Req));
                }

                string Action = "urn:getAllocationEnquiry";

                System.Diagnostics.Stopwatch timerroomprocess = new System.Diagnostics.Stopwatch();
                timerroomprocess.Start();

                //ResponseData = XMLHotelBookingCommon.Getuk_travco_Response_report(ref Error, uk_travco_URL + "AllocationEnquiryV7Service?wsdl", Action, Req, 12000, "ukTravco", null, Searchkey);
                ResponseDto responseDto = await Getuk_travco_Response_report(uk_travco_URL + "AllocationEnquiryV7Service", Action, Req, 12000, SupplierName, null, Searchkey);
                ResponseData = responseDto.Response;
                Error = responseDto.Error;

                timerroomprocess.Stop();
                time = timerroomprocess.Elapsed.ToString();

                _commonMethod.ThirdPartyBookingHistory(Convert.ToString(UniqueNo), Convert.ToString(AgentUniqueNo), Req, ResponseData + Error, "After Get Price "+ SupplierName + " Request hit booking" + j + "isForBooking" + isForBooking, SupplierName, "Response Confirm", "-", "API Out");
                if (ResponseData == string.Empty || ResponseData.Contains("ErrorTxt"))
                {
                    try
                    {
                        responsetype = "GeneralError";
                        string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, Req, ResponseData, Error);
                    }
                    catch (Exception e)
                    {
                        _ = _exception.Error(e, Searchkey);
                    }

                    priceCheckDto.Flag = flag;
                    priceCheckDto.FinalPrice = FinalPrice;
                    return priceCheckDto;

                }
                DataSet dsHotelDoResponse = _commonMethod.GetDataSetFromXMLString(ResponseData);

                #region Nationality Filter
                //bool NationalityFillter = false;
                string AllocationEnquiryV7Response_id = "";
                string Response_id = "";
                string Allocations_id = "";
                string Allocation_id = "";
                string ImportantInformation_id = "";
                string NationalityRestriction_id = "";
                string Heading = "";
                string Headertext = "";
                string RestrictCountries_id = "";
                string PaxCountryName = "";
                string countryname = "";
                string PaxCountrycode = "";


                if (dsHotelDoResponse != null)
                {
                    string Body_id = Convert.ToString(dsHotelDoResponse.Tables["Body"].Rows[0]["Body_Id"]);

                    if (dsHotelDoResponse.Tables.Contains("AllocationEnquiryV7Response"))
                    {

                        DataRow[] drHotel = dsHotelDoResponse.Tables["AllocationEnquiryV7Response"].Select("Body_Id='" + Body_id + "'");
                        if (drHotel.GetLength(0) > 0)
                        {
                            for (int k = 0; k < drHotel.Length; k++)
                            {
                                AllocationEnquiryV7Response_id = Convert.ToString(drHotel[k]["AllocationEnquiryV7Response_Id"]);

                            }

                        }
                        else
                        {
                            //return 0;
                        }
                        if (dsHotelDoResponse.Tables.Contains("Response"))
                        {

                            DataRow[] drHotelRes = dsHotelDoResponse.Tables["Response"].Select("AllocationEnquiryV7Response_Id='" + AllocationEnquiryV7Response_id + "'");
                            if (drHotelRes.GetLength(0) > 0)
                            {
                                for (int lm = 0; lm < drHotelRes.Length; lm++)
                                {
                                    Response_id = Convert.ToString(drHotelRes[lm]["Response_Id"]);
                                }

                            }
                            else
                            {
                                //return 0;
                            }
                            if (dsHotelDoResponse.Tables.Contains("Allocations"))
                            {
                                DataRow[] drAllocations = dsHotelDoResponse.Tables["Allocations"].Select("Response_Id='" + Response_id + "'");
                                if (drAllocations.GetLength(0) > 0)
                                {
                                    for (int mn = 0; mn < drAllocations.Length; mn++)
                                    {
                                        Allocations_id = Convert.ToString(drAllocations[mn]["Allocations_Id"]);
                                    }

                                }
                                else
                                {
                                    //return 0;
                                }
                                if (dsHotelDoResponse.Tables.Contains("Allocation"))
                                {
                                    DataRow[] drAllocation = dsHotelDoResponse.Tables["Allocation"].Select("Allocations_Id='" + Allocations_id + "'");
                                    if (drAllocation.GetLength(0) > 0)
                                    {
                                        for (int l = 0; l < drAllocation.Length; l++)
                                        {
                                            Allocation_id = Convert.ToString(drAllocation[l]["Allocation_Id"]);
                                        }

                                    }
                                    else
                                    {
                                        //return 0;
                                    }
                                    if (dsHotelDoResponse.Tables.Contains("ImportantInformation") && dsHotelDoResponse.Tables["ImportantInformation"].Rows.Count > 0)
                                    {
                                        DataRow[] drInformation = dsHotelDoResponse.Tables["ImportantInformation"].Select("Allocation_Id='" + Allocation_id + "'");
                                        if (drInformation.GetLength(0) > 0)
                                        {
                                            for (int tt = 0; tt < drInformation.Length; tt++)
                                            {
                                                ImportantInformation_id = Convert.ToString(drInformation[tt]["ImportantInformation_Id"]);



                                                if (dsHotelDoResponse.Tables.Contains("NationalityRestriction") && dsHotelDoResponse.Tables["NationalityRestriction"].Rows.Count > 0)
                                                {
                                                    DataRow[] drNationality = dsHotelDoResponse.Tables["NationalityRestriction"].Select("ImportantInformation_Id='" + ImportantInformation_id + "'");
                                                    if (drNationality.GetLength(0) > 0)
                                                    {
                                                        for (int m = 0; m < drNationality.Length; m++)
                                                        {
                                                            NationalityRestriction_id = Convert.ToString(drNationality[m]["NationalityRestriction_Id"]);

                                                            if (dsHotelDoResponse.Tables.Contains("Header") && dsHotelDoResponse.Tables["Header"].Rows.Count > 0)
                                                            {
                                                                DataRow[] drHeader = dsHotelDoResponse.Tables["Header"].Select("NationalityRestriction_Id='" + NationalityRestriction_id + "'");
                                                                if (drHeader.GetLength(0) > 0)
                                                                {
                                                                    for (int n = 0; n < drHeader.Length; n++)
                                                                    {
                                                                        Heading = Convert.ToString(drHeader[n]["Heading"]);
                                                                        Headertext = Convert.ToString(drHeader[n]["HeaderText"]);
                                                                        NationalityRestriction_id = Convert.ToString(drHeader[n]["NationalityRestriction_Id"]);

                                                                        if (dsHotelDoResponse.Tables.Contains("RestrictCountries") && dsHotelDoResponse.Tables["RestrictCountries"].Rows.Count > 0)
                                                                        {
                                                                            DataRow[] drRestrictCountries = dsHotelDoResponse.Tables["RestrictCountries"].Select("NationalityRestriction_Id='" + NationalityRestriction_id + "'");
                                                                            if (drRestrictCountries.GetLength(0) > 0)
                                                                            {
                                                                                for (int p = 0; p < drRestrictCountries.Length; p++)
                                                                                {
                                                                                    RestrictCountries_id = Convert.ToString(drRestrictCountries[p]["RestrictCountries_Id"]);

                                                                                    if (dsHotelDoResponse.Tables.Contains("RestrictCountry") && dsHotelDoResponse.Tables["RestrictCountry"].Rows.Count > 0)
                                                                                    {
                                                                                        DataRow[] drRestrictCountry = dsHotelDoResponse.Tables["RestrictCountry"].Select("RestrictCountries_Id='" + RestrictCountries_id + "'");
                                                                                        if (drRestrictCountry.GetLength(0) > 0)
                                                                                        {
                                                                                            for (int px = 0; px < drRestrictCountry.Length; px++)
                                                                                            {
                                                                                                PaxCountryName = Convert.ToString(drRestrictCountry[px]["CountryName"]);
                                                                                                PaxCountrycode = Convert.ToString(drRestrictCountry[px]["ISOCountryCode"]);

                                                                                                if (PaxCountryName.ToLower() == countryname.ToLower())
                                                                                                {
                                                                                                    if (Heading.ToLower() == "inclusions")
                                                                                                    {
                                                                                                        priceCheckDto.Flag = flag;
                                                                                                        priceCheckDto.FinalPrice = FinalPrice;
                                                                                                        return priceCheckDto;
                                                                                                    }
                                                                                                    if (Heading.ToLower() == "exclusions")
                                                                                                    {
                                                                                                        priceCheckDto.Flag = flag;
                                                                                                        priceCheckDto.FinalPrice = FinalPrice;
                                                                                                        return priceCheckDto;
                                                                                                    }
                                                                                                }
                                                                                                else
                                                                                                {
                                                                                                    //return 0;
                                                                                                }
                                                                                            }

                                                                                        }

                                                                                    }

                                                                                }

                                                                            }
                                                                            else
                                                                            {
                                                                                //return 0;
                                                                            }
                                                                        }
                                                                    }
                                                                }
                                                                else
                                                                {
                                                                    //return 0;
                                                                }

                                                            }
                                                        }

                                                    }
                                                    else
                                                    {
                                                        //return 0;
                                                    }
                                                }
                                                else
                                                {
                                                    //return 0;
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


                DataRow[] drRes = dsHotelDoResponse.Tables["Allocation"].Select();
                double APrice = 0;
                double CPrice = 0;
                double RoomTotalRate = 0;
                for (int i = 0; i < drRes.Length; i++)
                {
                    string RoomCode = Convert.ToString(drRes[i]["RoomCode"]);
                    string HCode = Convert.ToString(drRes[i]["HotelCode"]);
                    if (RCode.Split('$')[0] != RoomCode || HotelCode != HCode)
                        continue;
                    string Status = Convert.ToString(drRes[i]["Status"]);
                    if (Status.ToLower() != "avl")
                    {
                        priceCheckDto.Flag = flag;
                        priceCheckDto.FinalPrice = FinalPrice;
                        return priceCheckDto;
                    }
                    string CurrencyCode = Convert.ToString(drRes[i]["CurrencyCode"]);
                    if (CurrencyCode.ToLower() != "usd")
                    {

                        if (CurrencyCode.ToLower() == "pds")
                            CurrencyCode = "GBP";
                        dtInrCurrency = _commonMethod.GetCountryCurrency(CurrencyCode);
                        CurrencyFactoruk_travco = Convert.ToDouble(dtInrCurrency.Rows[0]["AmtToExch"]);

                    }
                    APrice = Convert.ToDouble(drRes[i]["TotalAdultPrice"]);
                    if (dsHotelDoResponse.Tables["Allocation"].Columns.Contains("TotalChildPrice"))
                        CPrice = Convert.ToDouble(drRes[i]["TotalChildPrice"]);
                    double AdultPrice = Convert.ToDouble(drRes[i]["AdultPrice"]);
                    double ChildPrice = Convert.ToDouble(drRes[i]["ChildPrice"]);
                    RoomTotalRate = (AdultPrice * NoOfAdults) + (ChildPrice * Child);
                }

                FinalPrice = RoomTotalRate * CurrencyFactoruk_travco;
                if ((FinalPrice).TruncateDecimalPlaces(0) > Rate.TruncateDecimalPlaces(0))
                {
                    try
                    {
                        responsetype = "GeneralError";
                        ResponseData = "#Price Changed " + "#Main Price = " + Rate + " #New Price = " + FinalPrice + " #Response= " + ResponseData;
                        string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, Req, ResponseData, Error);
                    }
                    catch (Exception e)
                    {
                        _ = _exception.Error(e, Searchkey);
                    }
                    priceCheckDto.Flag = flag;
                    priceCheckDto.FinalPrice = FinalPrice;
                    return priceCheckDto;
                }
            }
            catch (Exception ex)
            {
                try
                {
                    responsetype = "GeneralError";
                    Error = ex.Message + ex.StackTrace.ToString() + "#= " + Error;
                    string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, Req, ResponseData, Error);
                }
                catch (Exception e)
                {
                    _ = _exception.Error(e, Searchkey);
                }
                _= _exception.Error(ex, Searchkey);

                priceCheckDto.Flag = 2;
                priceCheckDto.FinalPrice = FinalPrice;
                return priceCheckDto;
            }
            try
            {
                responsetype = "Success";
                string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, Req, ResponseData, Error);
            }
            catch (Exception e)
            {
                _ = _exception.Error(e, Searchkey);
            }
            priceCheckDto.Flag = 0;
            priceCheckDto.FinalPrice = FinalPrice;
            return priceCheckDto;
        }

        public async Task<ResponseDto> Getuk_travco_Response_report(string _url, string _action, string Request, Int32 Timeout, string Provider = "ukTravco", HotelSearchParameter para = null, string Searchkey = "")
        {
            ResponseDto responseDto = new ResponseDto();
            string Error = "";
            try
            {


                System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
                timer.Start();


                XmlDocument soapEnvelopeXml = CreateSoapEnvelopeUKT(Request);
                HttpWebRequest webRequest = CreateWebRequest_uk_travco(_url, _action, Timeout);
                InsertSoapEnvelopeIntoWebRequestUKT(soapEnvelopeXml, webRequest);
                // begin async call to web request.
                IAsyncResult asyncResult = webRequest.BeginGetResponse(null, null);
                // suspend this thread until call is complete. You might want to
                // do something usefull here like update your UI.
                asyncResult.AsyncWaitHandle.WaitOne();
                // get the response from the completed web request.
                string soapResult;
                using (WebResponse webResponse = webRequest.GetResponse())
                using (StreamReader rd = new StreamReader(webResponse.GetResponseStream()))
                {
                    soapResult = await rd.ReadToEndAsync();
                }
                timer.Stop();
                if (para != null)
                    para.SupplierTime += ",SupplierName :" + Provider + " : " + "Execution Time :" + timer.Elapsed.ToString();
                Console.Write(soapResult);

                responseDto.Response = soapResult;
                responseDto.Error = "";

                return responseDto;
            }
            catch (Exception ex)
            {
                Error = ex.Message.ToString();
                _ = _exception.Error(ex, Searchkey);

                responseDto.Response = "";
                responseDto.Error = "";

                return responseDto;
            }
        }

        private static XmlDocument CreateSoapEnvelopeUKT(string xml)
        {
            XmlDocument soapEnvelop = new XmlDocument();
            soapEnvelop.LoadXml(xml);
            return soapEnvelop;
        }
        private static HttpWebRequest CreateWebRequest_uk_travco(string url, string action, Int32 Timeout)
        {
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(url);
            webRequest.Headers.Add("SOAPAction", action);
            webRequest.ContentType = "text/xml;charset=\"utf-8\"";
            webRequest.Accept = "text/xml";
            webRequest.Method = "POST";
            webRequest.Timeout = Timeout;
            return webRequest;
        }
        
        private static void InsertSoapEnvelopeIntoWebRequestUKT(XmlDocument soapEnvelopeXml, HttpWebRequest webRequest)
        {
            using (Stream stream = webRequest.GetRequestStream())
            {
                soapEnvelopeXml.Save(stream);
            }
        }

        //book
        public string Getuk_travcoBookingRequest_combineBook(DataSet dsCartDetails, DataTable dtPassengers, int i, int j, string ClientID, string UserName, string Password, string UniqueNo,string SearchKey)
        {
            //j=Room & i = Hotel
            string BookingRQ = "";
            try
            {


                string checkindate = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["CheckInDate"]);
                string checkin = (checkindate).Substring(0, 4) + "-" + (checkindate).Substring(4, 2) + "-" + (checkindate).Substring(6, 2);// + "T03:00:00+03:00";
                string checkoutdate = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["CheckOutDate"]);
                string checkout = (checkoutdate).Substring(0, 4) + "-" + (checkoutdate).Substring(4, 2) + "-" + (checkoutdate).Substring(6, 2);// + "T03:00:00+03:00";

                string chkin = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["CheckInDate"]);
                chkin = chkin.Substring(6, 2) + "/" + chkin.Substring(4, 2) + "/" + chkin.Substring(0, 4);
                string chkout = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["CheckOutDate"]);

                chkout = chkout.Substring(6, 2) + "/" + chkout.Substring(4, 2) + "/" + chkout.Substring(0, 4);
                chkin = String.Format("{0:yyyy/MM/dd}", DateTime.ParseExact(chkin, "dd/MM/yyyy", null));
                chkout = String.Format("{0:yyyy/MM/dd}", DateTime.ParseExact(chkout, "dd/MM/yyyy", null));

                DateTime dtCheck = Convert.ToDateTime(chkin);
                DateTime dtCheckOut = Convert.ToDateTime(chkout);

                int night = Convert.ToInt32((dtCheckOut - dtCheck).TotalDays);


                string AgentReference = "Api_" + UniqueNo;
                string firstName = ""; string lastName = ""; string title = ""; string Fn = ""; string Ln = ""; string Ti = ""; string PassangerName = "";

                StringBuilder BookRQ = new StringBuilder();
                BookRQ.Append("<soapenv:Envelope xmlns:soapenv=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:req=\"http://www.travco.co.uk/trlink/xsd/hotelbookingv7/request\">");

                BookRQ.Append("<soapenv:Body>");
                BookRQ.Append("<req:doHotelBooking>");
                BookRQ.Append("<req:RequestBase AgentCode=\"" + UserName + "\" AgentPassword=\"" + Password + "\" Lang=\"en-GB\"/>");
                //BookRQ.Append("<req:ClerkCode>?</req:ClerkCode>");
                BookRQ.Append("<req:Bookings>");
                ////////
                for (int x = 0; x < dsCartDetails.Tables[1].DefaultView.Count; x++)
                {
                    int NoOfChild = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[x]["NoOfChild"]);
                    int NoOfAdults = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[x]["NoOfAdults"]);
                    string HCode = Convert.ToString(dsCartDetails.Tables[1].DefaultView[x]["RoomTypeId"]).Split('$')[1];
                    string RCode = Convert.ToString(dsCartDetails.Tables[1].DefaultView[x]["RoomTypeId"]).Split('$')[0];
                    string PriceCode = Convert.ToString(dsCartDetails.Tables[1].DefaultView[x]["RoomTypeId"]).Split('$')[2];
                    dtPassengers.DefaultView.RowFilter = "PickUp='" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[x]["RoomTypeId"]) + "' and (RoomNo=" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[x]["RoomNumber"]) + ")";
                    dtPassengers.DefaultView.Sort = "IsLead DESC";
                    string SpecialRequest = Convert.ToString(dtPassengers.DefaultView[0]["Message"]).Trim();
                    //string AgentReference= Convert.ToString(dtPassengers.DefaultView[0]["AgentReferenceNo"]).Trim();
                    int Children = 0;
                    int Infant = 0;
                    int Adult = NoOfAdults;
                    int ChildAG1 = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[x]["ChildAge1"]);
                    int ChildAG2 = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[x]["ChildAge2"]);
                    if (ChildAG1 != 0 && ChildAG2 == 0)
                    {
                        if (ChildAG1 < 2)
                            Infant = Infant + 1;
                        else
                            Children = Children + 1;
                        if (ChildAG1 > 11)
                        {
                            if (Children > 0)
                                Children = Children - 1;
                            Adult = Adult + 1;
                        }
                    }
                    if (ChildAG2 != 0)
                    {
                        if (ChildAG2 < 2)
                            Infant = Infant + 1;
                        else
                            Children = Children + 1;
                        if (ChildAG1 > 11)
                        {
                            if (Children > 0)
                                Children = Children - 1;
                            Adult = Adult + 1;
                        }
                        if (ChildAG2 > 11)
                        {
                            if (Children > 0)
                                Children = Children - 1;
                            Adult = Adult + 1;
                        }
                    }
                    for (int k = 0; k < dtPassengers.DefaultView.Count; k++)
                    {
                        DataRow[] drPassengers = dtPassengers.DefaultView.ToTable().Select("IsLead='1'");
                        if (drPassengers.Length == 0)
                        {
                            if (Convert.ToString(dtPassengers.DefaultView[k]["PaxType"]).Trim().ToLower() == "adult")
                                dtPassengers.DefaultView[k]["IsLead"] = 1;
                        }
                        if (Convert.ToInt16(dtPassengers.DefaultView[k]["IsLead"]) == 1)
                        {
                            firstName = Convert.ToString(dtPassengers.DefaultView[k]["FirstName"]).Trim();
                            lastName = Convert.ToString(dtPassengers.DefaultView[k]["LastName"]).Trim();
                            title = Convert.ToString(dtPassengers.DefaultView[k]["Prefix"]).Trim().Replace(".", "");
                            Fn = firstName.ToUpper();
                            Ln = lastName.ToUpper();
                            Ti = title.ToUpper();
                            PassangerName = Ln + "/" + Fn + "/" + Ti;
                        }
                    }
                    string Child = "NoOfChildren=\"" + Convert.ToString(Children) + "\" ";
                    if (Children == 0)
                        Child = "";
                    string Infants = "Infants=\"" + Infant + "\" ";
                    if (Infant == 0)
                        Infants = "";

                    BookRQ.Append("<req:Booking>");
                    BookRQ.Append("<req:HotelData NoOfAdults=\"" + Adult + "\" ");
                    BookRQ.Append(Child);
                    BookRQ.Append(Infants);
                    BookRQ.Append(" Duration=\"" + night + "\" CheckInDate=\"" + checkin + "\" HotelCode=\"" + HCode + "\" RoomCode=\"" + RCode + "\" PriceCode=\"" + PriceCode + "\">");
                    BookRQ.Append("<PassengerName>" + PassangerName + "</PassengerName>");
                    if (AgentReference != "")
                        BookRQ.Append("<AgentReference>" + AgentReference + "</AgentReference>");
                    if (Infant > 0)
                    {
                        if (SpecialRequest != "")
                            BookRQ.Append("<AgentText>Cot Required, " + SpecialRequest + "</AgentText>");
                        else
                            BookRQ.Append("<AgentText>Cot Required</AgentText>");
                    }
                    else
                    {
                        if (SpecialRequest != "")
                            BookRQ.Append("<AgentText>" + SpecialRequest + "</AgentText>");
                    }
                    //BookRQ.Append("<AgentComment>test</AgentComment>");
                    BookRQ.Append("</req:HotelData>");
                    BookRQ.Append("</req:Booking>");
                }
                ////////////
                //BookRQ.Append("<req:Booking>");
                //BookRQ.Append("<req:HotelData NoOfAdults=\"" + Adult + "\" ");
                //BookRQ.Append(Child);
                //BookRQ.Append(Infants);
                //BookRQ.Append(" Duration=\"" + night + "\" CheckInDate=\"" + checkin + "\" HotelCode=\"" + HCode + "\" RoomCode=\"" + RCode + "\" PriceCode=\"" + PriceCode + "\">");
                //BookRQ.Append("<PassengerName>" + PassangerName + "</PassengerName>");
                //if (AgentReference != "")
                //    BookRQ.Append("<AgentReference>" + AgentReference + "</AgentReference>");
                //if (Infant > 0)
                //{
                //    if (SpecialRequest != "")
                //        BookRQ.Append("<AgentText>Cot Required, " + SpecialRequest + "</AgentText>");
                //    else
                //        BookRQ.Append("<AgentText>Cot Required</AgentText>");
                //}
                //else
                //{
                //    if (SpecialRequest != "")
                //        BookRQ.Append("<AgentText>" + SpecialRequest + "</AgentText>");
                //}
                ////BookRQ.Append("<AgentComment>test</AgentComment>");
                //BookRQ.Append("</req:HotelData>");
                //BookRQ.Append("</req:Booking>");
                BookRQ.Append("</req:Bookings>");
                BookRQ.Append("<req:AdditionalData NeedHotelMessages=\"yes\"/>");
                BookRQ.Append("</req:doHotelBooking>");
                BookRQ.Append("</soapenv:Body>");
                BookRQ.Append("</soapenv:Envelope>");
                BookingRQ = BookRQ.ToString();
                //}
                return BookingRQ;
            }
            catch (Exception ex)
            {
                _ = _exception.ErrorNew(ex,ErrorLog._LogAPI.Book,SearchKey);
                return "";
            }
        }

        public string BookingDetailsRQ(string Ref, DataRow[] uk_travcoDetails)
        {
            string AgentID = Convert.ToString(uk_travcoDetails[0]["UserId"]);
            string Password = Convert.ToString(uk_travcoDetails[0]["Password"]);
            StringBuilder BookDetailsRQ = new StringBuilder();
            BookDetailsRQ.Append("<soapenv:Envelope xmlns:soapenv=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:req=\"http://www.travco.co.uk/trlink/xsd/bookenquiryv7/request\">");
            BookDetailsRQ.Append("<soapenv:Header/>");
            BookDetailsRQ.Append("<soapenv:Body>");
            BookDetailsRQ.Append("<req:getBookEnquiry>");
            BookDetailsRQ.Append("<req:RequestBase AgentCode=\"" + AgentID + "\" AgentPassword=\"" + Password + "\" Lang=\"en-GB\"/>");
            BookDetailsRQ.Append("<req:BookEnquiry RefNo=\"" + Ref + "\">");
            BookDetailsRQ.Append("</req:BookEnquiry>");
            BookDetailsRQ.Append("</req:getBookEnquiry>");
            BookDetailsRQ.Append("</soapenv:Body>");
            BookDetailsRQ.Append("</soapenv:Envelope>");

            return BookDetailsRQ.ToString();
        }

        public string Getuk_travcoCancelationRQ(string Ref, DataRow[] uk_travcoDetails, int NoOfAdults, int NoOfChildren, int Duration, string CheckInDate, string HotelCode, string RoomCode, string PriceCode, string PassengerName, string AgentReference, int Infants)
        {
            try
            {

                string AgentID = Convert.ToString(uk_travcoDetails[0]["UserId"]);
                string Password = Convert.ToString(uk_travcoDetails[0]["Password"]);

                StringBuilder CancelationRQ = new StringBuilder();
                CancelationRQ.Append("<soapenv:Envelope xmlns:soapenv=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:req=\"http://www.travco.co.uk/trlink/xsd/hotelbookingv7/request\">");
                CancelationRQ.Append("<soapenv:Header/>");
                CancelationRQ.Append("<soapenv:Body>");
                CancelationRQ.Append("<req:cancelHotelBooking>");
                CancelationRQ.Append("<req:RequestBase AgentCode=\"" + AgentID + "\" AgentPassword=\"" + Password + "\" Lang=\"en-GB\"/>");
                CancelationRQ.Append("<req:BookingReference>" + Ref + "</req:BookingReference>");
                CancelationRQ.Append("<req:Booking>");
                CancelationRQ.Append("<req:HotelData NoOfAdults=\"" + NoOfAdults + "\" ");
                if (NoOfChildren != 0)
                    CancelationRQ.Append("NoOfChildren=\"" + NoOfChildren + "\" ");
                if (Infants != 0)
                    CancelationRQ.Append("Infants=\"" + Infants + "\" ");
                CancelationRQ.Append("Duration=\"" + Duration + "\" CheckInDate=\"" + CheckInDate + "\" HotelCode=\"" + HotelCode + "\" RoomCode=\"" + RoomCode + "\" PriceCode=\"" + PriceCode + "\" >");
                CancelationRQ.Append("<PassengerName>" + PassengerName + "</PassengerName>");
                CancelationRQ.Append("<AgentReference>" + AgentReference + "</AgentReference>");
                //CancelationRQ.Append("<AgentComment>Plan changed</AgentComment>");
                CancelationRQ.Append("</req:HotelData>");
                CancelationRQ.Append("</req:Booking>");
                CancelationRQ.Append("<req:AdditionalData NeedHotelMessages=\"yes\"/>");
                CancelationRQ.Append("</req:cancelHotelBooking>");
                CancelationRQ.Append("</soapenv:Body>");
                CancelationRQ.Append("</soapenv:Envelope>");

                return CancelationRQ.ToString();
            }
            catch (Exception ex)
            {
                _=_exception.ErrorNew(ex, ErrorLog._LogAPI.CancelBooking);
            }
            return string.Empty;
        }
    }
}
