using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using XConnect.CommonUtilities;
using XConnect.CommonUtilities.Interface;
using XConnect.ErrorLogHandler.Interface;
using XConnect.Markup.Interface;
using XConnect.Models;
using XConnect.Models.Hotel;
using XConnect.Supplier.Saltours;
using XConnect.Supplier.Techno.Interface;
using static Azure.Core.HttpHeader;
using static XConnect.Models.ErrorLog;
using Options = XConnect.Models.Options;

namespace XConnect.Supplier.Techno
{
    public class TechnoRepo : ITechno
    {
        private readonly ICommonMethod _commonMethod;
        private readonly IException _exception;
        private readonly IMarkup _markup;
        private readonly ISupplierRes _supplierRes;
        private readonly KeySettings _keySettings; 
        private readonly SupplierConfiguration _supplierConfig;//Added on 27-FEB-26


        public TechnoRepo(ICommonMethod commonMethod, IMarkup markup, ISupplierRes supplierRes, IOptions<KeySettings> keySettings, IException exception, IOptions<SupplierConfiguration> supplierConfig)
        {
            _commonMethod = commonMethod;
            _markup = markup;
            _supplierRes = supplierRes;
            _keySettings = keySettings.Value;
            _exception = exception;
            _supplierConfig = supplierConfig.Value;
        }
        public void GetWE_DMCRequestOnly_Async(bool isHotelId, AdvancedOptions advance, DataRow[] commonCode, DataTable dtPreferredDataHotel, HotelSearchParameter SearchPara, List<SupplierSearchConfig> lstWE_DMCSearchConfig, List<SupplierDetails> lstWE_DMCDetails, ref ConcurrentBag<HotelX> AllTask, string AgentCode, int Hit, List<StarRating> listRating, DataTable dtROE, TokenData tokenData, DataSet dtXMLCity, string Platform, bool SearchByOneID)
        {
            try
            {
                List<StarRating> lstRating = new List<StarRating>();
                lstRating = _commonMethod.GetStarRatingStringList_xtg(SearchPara);

                List<HotelCodeByCity> DtHotelDAta = HtmlExtensions.ToList<HotelCodeByCity>(commonCode.CopyToDataTable());
                int ratingcount = lstRating.Count;
                int hotelcount = DtHotelDAta.Count;

                foreach (SupplierDetails WE_DMCDetails in lstWE_DMCDetails)
                {
                    int SupplierID = WE_DMCDetails.SupplierID;
                    string Supplier = WE_DMCDetails.SupplierName;

                    if (lstWE_DMCSearchConfig.Count > 0)
                    {
                        try
                        {
                            List<SupplierSearchConfig> WE_DMCSearchConfig = lstWE_DMCSearchConfig
                                 .Where(x => x != null && x.SupplierID == SupplierID)
                                 .ToList();

                            if (WE_DMCSearchConfig != null && WE_DMCSearchConfig.Count > 0)
                            {

                                DataForSearch dataForSearch = _commonMethod.GetXmlDataForSearchTechwise(SearchPara.Nationality, dtXMLCity, Supplier, WE_DMCDetails.TechName);
                                dataForSearch.XmlNationality = Convert.ToString(SearchPara.Nationality);
                                if (dataForSearch.XmlNationality != "")
                                {
                                    string[] spitid = Platform.Split(new String[] { "$$" }, StringSplitOptions.None);
                                    string IP_DMC = Convert.ToString(spitid[1]);
                                    bool IsShowRoom = SearchPara.Filters.IsShowRooms == 1 ? true : false;
                                    bool IsShowRoom_OneId = false;

                                    if ((SearchPara.Filters.IsShowRooms == 1 && SearchByOneID))
                                    {
                                        IsShowRoom_OneId = true;
                                    }


                                    string Ids = string.Empty;
                                    string xmlbase = "";
                                    _commonMethod.GetPrefferedHotelCodes(ref Ids, ref xmlbase, ref WE_DMCSearchConfig, Supplier, dtPreferredDataHotel, SearchPara.IsSearchFor);

                                    if (WE_DMCSearchConfig != null)
                                    {
                                        List<HotelCodeByCity> filter = new List<HotelCodeByCity>();
                                        filter = _commonMethod.FilterWithOutCity_Code(ratingcount, hotelcount, DtHotelDAta, lstRating, Supplier);
                                        if (filter.Count > 0)
                                        {
                                            double CurrencyFactorWE_DMC = 1;
                                            string Currency = Convert.ToString(WE_DMCDetails.CurrencyShortName);
                                            if (Currency.ToLower() != tokenData.SiteCurrency.ToLower())
                                            {
                                                CurrencyFactorWE_DMC = _commonMethod.GetROEByCurrency(Currency, dtROE);
                                            }

                                            for (int k = 0; k < WE_DMCSearchConfig.Count; k++)
                                            {
                                                bool DebugMode = (Convert.ToInt32(WE_DMCSearchConfig[k].DebugMode) == 0 ? false : true);

                                                if (WE_DMCSearchConfig[k].City == 0 || IsShowRoom)
                                                {
                                                    if (Convert.ToInt32(WE_DMCSearchConfig[k].CityID) == 0 || Convert.ToInt32(WE_DMCSearchConfig[k].CityID) == SearchPara.CityID)
                                                    {
                                                        if (commonCode.Length > 0)
                                                        {
                                                            int Noofrecords = Convert.ToInt32(WE_DMCSearchConfig[k].Noofrecords);
                                                            double count = Convert.ToDouble(filter.Count);
                                                            int NoOfDiv = Convert.ToInt32(Math.Ceiling(count / Convert.ToDouble(Noofrecords)));
                                                            List<List<XMLID_Search>> lstxml = _commonMethod.GetXMLIDString(NoOfDiv, Noofrecords, count, filter);

                                                            for (int a = 0; a < NoOfDiv; a++)
                                                            {
                                                                string IdsForSearch = Convert.ToString(_commonMethod.GetHotelCodeByCOmma(lstxml[a]));

                                                                HotelX xhotel = new HotelX();
                                                                xhotel.Supplier = Supplier;
                                                                xhotel.DebugeMode = DebugMode;
                                                                xhotel.SearchTimeOut = Convert.ToInt32(WE_DMCSearchConfig[k].HotelSearchTimeOut);
                                                                //if (Hit > 0)
                                                                xhotel.ProcessTimeout = Convert.ToInt32(WE_DMCSearchConfig[k].HotelSearchTimeOut2);

                                                                if (_keySettings.SupplierProxy == "0")
                                                                {
                                                                    if (Supplier.ToLower() == "travelbullz_techno")
                                                                       // xhotel.URL = Convert.ToString(WE_DMCDetails.WebSiteURL) + Convert.ToString(TechnoInfo.WE_DMCHotelSearchWithCancellationURL);
                                                                        xhotel.URL = Convert.ToString(WE_DMCDetails.WebSiteURL) + Convert.ToString(_supplierConfig.Techno.WE_DMCHotelSearchWithCancellationURL);
                                                                    else
                                                                       // xhotel.URL = Convert.ToString(WE_DMCDetails.WebSiteURL) + Convert.ToString(TechnoInfo.WE_DMCHotelSearchURL);
                                                                        xhotel.URL = Convert.ToString(WE_DMCDetails.WebSiteURL) + Convert.ToString(_supplierConfig.Techno.WE_DMCHotelSearchURL);

                                                                }
                                                                else
                                                                {
                                                                    if (Supplier.ToLower() == "travelbullz_techno")
                                                                        //xhotel.URL = Convert.ToString(WE_DMCDetails.WebSiteURLforAPI) + Convert.ToString(TechnoInfo.WE_DMCHotelSearchWithCancellationURL);
                                                                        xhotel.URL = Convert.ToString(WE_DMCDetails.WebSiteURLforAPI) + Convert.ToString(_supplierConfig.Techno.WE_DMCHotelSearchWithCancellationURL);
                                                                    else
                                                                        //xhotel.URL = Convert.ToString(WE_DMCDetails.WebSiteURLforAPI) + Convert.ToString(TechnoInfo.WE_DMCHotelSearchURL);
                                                                        xhotel.URL = Convert.ToString(WE_DMCDetails.WebSiteURLforAPI) + Convert.ToString(_supplierConfig.Techno.WE_DMCHotelSearchURL);

                                                                }
                                                                xhotel.UserId = Convert.ToString(WE_DMCDetails.UserID);
                                                                xhotel.UserName = Convert.ToString(WE_DMCDetails.UserName);
                                                                xhotel.PassWord = Convert.ToString(WE_DMCDetails.Password);
                                                                xhotel.CurrencyCode = Currency;
                                                                xhotel.CurrencyFactor = CurrencyFactorWE_DMC;
                                                                xhotel.XmlCity = dataForSearch.xmlCity;
                                                                xhotel.XmlCountry = dataForSearch.xmlCountry;
                                                                xhotel.XmlNationality = dataForSearch.XmlNationality;
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
                                                                    objrq.Request = WE_DMCRequestOnly_Async(Convert.ToString(WE_DMCDetails.UserID), SearchPara, dataForSearch.xmlCity, dataForSearch.xmlCountry, IdsForSearch, dataForSearch.XmlNationality, 0, IP_DMC, IsShowRoom, IsShowRoom_OneId, Currency, Supplier);
                                                                    objrq.Response = ""; RQRS.Add(objrq);
                                                                }
                                                                xhotel.RQRS = RQRS;
                                                                #endregion
                                                                xhotel.TechName = WE_DMCDetails.TechName;
                                                                xhotel.ReturnRooms = WE_DMCDetails.ReturnRooms;
                                                                AllTask.Add(xhotel);
                                                            }
                                                        }
                                                    }
                                                }
                                                else  // && count == 1)
                                                {
                                                    for (int a = 0; a < listRating.Count; a++)
                                                    {
                                                        Int32 Str = listRating[a].Star;
                                                        //var paramValues1 = String.Join(",", lstxml[a].Select(x => String.Join(",", Convert.ToString(x.XMLID))));
                                                        string IdsForSearch = "";//Convert.ToString(paramValues1);

                                                        HotelX xhotel = new HotelX();
                                                        xhotel.Supplier = Supplier;
                                                        xhotel.DebugeMode = DebugMode;
                                                        xhotel.SearchTimeOut = Convert.ToInt32(WE_DMCSearchConfig[k].HotelSearchTimeOut);
                                                        //if (Hit > 0)
                                                        xhotel.ProcessTimeout = Convert.ToInt32(WE_DMCSearchConfig[k].HotelSearchTimeOut2);


                                                        if (_keySettings.SupplierProxy == "0")
                                                        {
                                                            if (Supplier.ToLower() == "travelbullz_techno")
                                                                // xhotel.URL = Convert.ToString(WE_DMCDetails.WebSiteURL) + Convert.ToString(TechnoInfo.WE_DMCHotelSearchWithCancellationURL);
                                                                xhotel.URL = Convert.ToString(WE_DMCDetails.WebSiteURL) + Convert.ToString(_supplierConfig.Techno.WE_DMCHotelSearchWithCancellationURL);
                                                            else
                                                                // xhotel.URL = Convert.ToString(WE_DMCDetails.WebSiteURL) + Convert.ToString(TechnoInfo.WE_DMCHotelSearchURL);
                                                                xhotel.URL = Convert.ToString(WE_DMCDetails.WebSiteURL) + Convert.ToString(_supplierConfig.Techno.WE_DMCHotelSearchURL);

                                                        }
                                                        else
                                                        {
                                                            if (Supplier.ToLower() == "travelbullz_techno")
                                                                //xhotel.URL = Convert.ToString(WE_DMCDetails.WebSiteURLforAPI) + Convert.ToString(TechnoInfo.WE_DMCHotelSearchWithCancellationURL);
                                                                xhotel.URL = Convert.ToString(WE_DMCDetails.WebSiteURLforAPI) + Convert.ToString(_supplierConfig.Techno.WE_DMCHotelSearchWithCancellationURL);
                                                            else
                                                                //xhotel.URL = Convert.ToString(WE_DMCDetails.WebSiteURLforAPI) + Convert.ToString(TechnoInfo.WE_DMCHotelSearchURL);
                                                                xhotel.URL = Convert.ToString(WE_DMCDetails.WebSiteURLforAPI) + Convert.ToString(_supplierConfig.Techno.WE_DMCHotelSearchURL);

                                                        }
                                                        xhotel.UserId = Convert.ToString(WE_DMCDetails.UserID);
                                                        xhotel.UserName = Convert.ToString(WE_DMCDetails.UserName);
                                                        xhotel.PassWord = Convert.ToString(WE_DMCDetails.Password);
                                                        xhotel.CurrencyCode = Currency;
                                                        xhotel.CurrencyFactor = CurrencyFactorWE_DMC;
                                                        xhotel.XmlCity = dataForSearch.xmlCity;
                                                        xhotel.XmlCountry = Convert.ToString(dataForSearch.xmlCountry);
                                                        xhotel.XmlNationality = dataForSearch.XmlNationality;
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
                                                            objrq.Request = WE_DMCRequestOnly_Async(Convert.ToString(WE_DMCDetails.UserID), SearchPara, dataForSearch.xmlCity, dataForSearch.xmlCountry, IdsForSearch, dataForSearch.XmlNationality, Str, IP_DMC, IsShowRoom, IsShowRoom_OneId, Supplier);
                                                            objrq.Response = ""; RQRS.Add(objrq);
                                                        }
                                                        xhotel.RQRS = RQRS;
                                                        #endregion
                                                        xhotel.TechName = WE_DMCDetails.TechName;
                                                        xhotel.ReturnRooms = WE_DMCDetails.ReturnRooms;
                                                        AllTask.Add(xhotel);
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

        public string WE_DMCRequestOnly_Async(string userID, HotelSearchParameter SearchParam, string cityID, string CountryID, string HotelId, string XmlNationality, int Rating, string CustomerIpAddress, bool IsShowRoom = false, bool IsShowRoom_OneId = false, string Currncy = "", string Provider = "")
        {
            try
            {

                return Withinearth_DMCAvailabilityRequest(userID, SearchParam, cityID, CountryID, HotelId, XmlNationality, Rating, IsShowRoom, CustomerIpAddress, IsShowRoom_OneId, Currncy, Provider);

            }
            catch (Exception ex)
            {
                _exception.Error(ex);
                return "";
            }
        }

        public string Withinearth_DMCAvailabilityRequest(string userID, HotelSearchParameter sp, string CityID, string CountryID, string HotelID, string Nationality, int Rating, bool IsForRoom = true, string CustomerIpAddress = "", bool IsShowRoom_OneId = false, string Currncy = "", string Provider = "")
        {
            try
            {
                string IsShowRooms = "0";
                if (IsShowRoom_OneId)
                {
                    IsShowRooms = "1";
                    IsForRoom = true;
                }
                else
                    IsForRoom = false;
                string CheckInDate = sp.CheckInDate;
                CheckInDate = CheckInDate.Substring(3, 2) + "-" + CheckInDate.Substring(0, 2) + "-" + CheckInDate.Substring(6, 4);
                string CheckOutDate = sp.CheckOutDate;
                CheckOutDate = CheckOutDate.Substring(3, 2) + "-" + CheckOutDate.Substring(0, 2) + "-" + CheckOutDate.Substring(6, 4);
                string Token = userID;
                int RoomNo = sp.Rooms.Count;
                int NoofNights = sp.NoofNights;
                string IsRecommendedOnly = "0";
                //IsShowRooms = "1";
                //if (IsForRoom)
                //IsShowRooms = "1";
                //else
                IsShowRooms = Convert.ToString(sp.Filters.IsShowRooms);

                string IsOnlyAvailable = "1";
                //Rating = 0;


                string Request = "";
                StringBuilder SBreq = new StringBuilder();
                SBreq.Append("{");
                SBreq.Append("\"Token\":\"" + Token + "\",");
                SBreq.Append("\"Request\":{");
                SBreq.Append("\"Rooms\":[");
                for (int i = 0; i < RoomNo; i++)
                {
                    int NoofAdults = sp.Rooms[i].NoofAdults;
                    int NoOfChild = sp.Rooms[i].NoofChild;
                    int ChildAge1 = sp.Rooms[i].Child1Age;
                    int ChildAge2 = sp.Rooms[i].Child2Age;
                    SBreq.Append("{");
                    SBreq.Append("\"RoomNo\":\"" + (i + 1) + "\",");
                    SBreq.Append("\"NoofAdults\":\"" + NoofAdults + "\",");
                    SBreq.Append("\"NoOfChild\":\"" + NoOfChild + "\",");
                    SBreq.Append("\"ChildAge\":[");
                    SBreq.Append("" + ChildAge1 + ", " + ChildAge2 + " ]");
                    SBreq.Append("}");
                    if (RoomNo > 1)
                    {
                        if (RoomNo != (i + 1))
                            SBreq.Append(",");
                    }
                }
                SBreq.Append("],");

                if (Provider.ToLower() == "travelbullz_techno")
                    SBreq.Append("\"CountryID\":\"" + CountryID + "\",");
                else
                    SBreq.Append("\"CityID\":\"" + CityID + "\",");

                SBreq.Append("\"CheckInDate\":\"" + CheckInDate + "\",");
                SBreq.Append("\"CheckOutDate\":\"" + CheckOutDate + "\",");
                SBreq.Append("\"NoofNights\":\"" + NoofNights + "\",");
                SBreq.Append("\"Nationality\":\"" + Nationality + "\",");
                // SBreq.Append("\"HotelID\": \"" + HotelID + "\",");
                SBreq.Append("\"Filters\":{");
                SBreq.Append("\"IsRecommendedOnly\":\"" + IsRecommendedOnly + "\",");
                SBreq.Append("\"IsShowRooms\":\"" + IsShowRooms + "\",");
                SBreq.Append("\"IsOnlyAvailable\":\"" + IsOnlyAvailable + "\",");
                if (IsForRoom == false)
                    SBreq.Append("\"StarRating\":{\"Min\":" + Rating + ",\"Max\":" + Rating + "},");
                else
                    SBreq.Append("\"StarRating\":{\"Min\":" + 1 + ",\"Max\":" + 5 + "},");
                SBreq.Append("\"HotelIDs\":\"" + HotelID + "\"");
                SBreq.Append("}");
                SBreq.Append("},");
                SBreq.Append("\"AdvancedOptions\":{");
                SBreq.Append("\"Currency\":\"" + Currncy + "\",");
                SBreq.Append("\"CustomerIpAddress\":\"" + CustomerIpAddress + "\"");
                SBreq.Append("}");
                SBreq.Append("}");
                Request = SBreq.ToString();
                return Request;
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
                return "";
            }
        }

        public async Task<List<Options>> WE_techno_HotelSearchWithRooms(HotelX Item, CommonSearch commonSearch, string ResponseData, string CityCode, string XmlCountry, HotelSearchParameter SearchParam, string currency, double CurrencyFactorWE_DMC, string Provider, Int32 processTimeout, string ResProccessTimerLogActive, string ResProccessTimeValueforSaveLog)
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
                    if (ResponseData == string.Empty)
                    {
                        Item.ResponseType = "NoCount";
                        return options;
                    }
                    //string ResponseData2 = Newtonsoft.Json.JsonConvert.DeserializeXmlNode(ResponseData, "root").InnerXml;
                    if (ResponseData == string.Empty || ResponseData.Contains("ErrorTxt"))
                    {
                        Item.ResponseType = "NoCount";
                        return options;
                    }
                    TechnoClass.AvailHotelList WE_DMCResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<TechnoClass.AvailHotelList>(ResponseData);
                    //DataSet RoomResponse = new DataSet();
                    //RoomResponse = Common.GetDataSetFromXMLString(ResponseData2);
                    if (WE_DMCResponse.AvailabilityRS == null)
                    {
                        Item.ResponseType = "NoCount";
                        return options;
                    }

                    #endregion

                    #region Hotel List
                    for (int i = 0; i < WE_DMCResponse.AvailabilityRS.HotelResult.Count; i++)
                    {
                        Item.ResponseType = "Success";
                        bool IsAvail = true;
                        if (IsAvail)
                        {
                            string checkin = (SearchParam.CheckInDate).Substring(3, 2) + "-" + (SearchParam.CheckInDate).Substring(0, 2) + "-" + (SearchParam.CheckInDate).Substring(6, 4);
                            string checkout = (SearchParam.CheckOutDate).Substring(3, 2) + "-" + (SearchParam.CheckOutDate).Substring(0, 2) + "-" + (SearchParam.CheckOutDate).Substring(6, 4);
                            #region "When not Need Room"  
                            // add hotel code from price only and room code blank here
                            if (SearchParam.Filters.IsShowRooms == 0)
                            {
                                #region "Hotel Code Block "

                                if (WE_DMCResponse.AvailabilityRS.Count > 0)
                                {
                                    double TotalCharges = Convert.ToDouble(WE_DMCResponse.AvailabilityRS.HotelResult[i].StartPrice) * CurrencyFactorWE_DMC;
                                    DataRow drXmlHotel = DtHotel.NewRow();
                                    drXmlHotel["XMLHotelCode"] = WE_DMCResponse.AvailabilityRS.HotelResult[i].HotelId;
                                    drXmlHotel["XMLCode"] = WE_DMCResponse.AvailabilityRS.SearchKey;
                                    drXmlHotel["HotelName"] = "";
                                    drXmlHotel["SupplierName"] = Provider;
                                    drXmlHotel["OfficeID"] = 0;
                                    drXmlHotel["ContractID"] = 0;
                                    drXmlHotel["Rating"] = 0;
                                    drXmlHotel["XmlCity"] = "";
                                    drXmlHotel["IsShowSupplier"] = false;
                                    drXmlHotel["BookingStatus"] = "Available";
                                    drXmlHotel["FinalSellingPrice"] = Convert.ToDouble(WE_DMCResponse.AvailabilityRS.HotelResult[i].StartPrice) * CurrencyFactorWE_DMC;
                                    drXmlHotel["SellingPrice"] = Convert.ToDouble(WE_DMCResponse.AvailabilityRS.HotelResult[i].StartPrice) * CurrencyFactorWE_DMC;
                                    DtHotel.Rows.Add(drXmlHotel);
                                    #endregion
                                    if (TotalCharges > 0)
                                    {
                                        #region "Room code Block"
                                        bool willAddOrNot = true;
                                        Int64 RaynaID = 0;
                                        Int64 GiataId = 0;
                                        int Rating = 0;
                                        Int64 OwnsystemHotelId = 0;
                                        double MinPrice = Convert.ToDouble(WE_DMCResponse.AvailabilityRS.HotelResult[i].StartPrice) * CurrencyFactorWE_DMC;
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
                            }
                            #endregion
                            #region "When Need Rooms"
                            //add Hotel code blank and rooms code in room block
                            else
                            {


                                #region Hotels
                                #region "Hotel Block here"
                                double Item_Price = Convert.ToDouble(WE_DMCResponse.AvailabilityRS.HotelResult[i].StartPrice) * CurrencyFactorWE_DMC;
                                DataRow drXmlHotel = DtHotel.NewRow();
                                drXmlHotel["XMLHotelCode"] = WE_DMCResponse.AvailabilityRS.HotelResult[i].HotelId;
                                drXmlHotel["XMLCode"] = WE_DMCResponse.AvailabilityRS.SearchKey;
                                drXmlHotel["HotelName"] = "";
                                drXmlHotel["SupplierName"] = Provider;
                                drXmlHotel["OfficeID"] = 0;
                                drXmlHotel["ContractID"] = 0;
                                drXmlHotel["Rating"] = 0;
                                drXmlHotel["XmlCity"] = "";
                                drXmlHotel["IsShowSupplier"] = false;
                                drXmlHotel["BookingStatus"] = "Available";
                                drXmlHotel["FinalSellingPrice"] = Item_Price;
                                drXmlHotel["SellingPrice"] = Item_Price;
                                DtHotel.Rows.Add(drXmlHotel);
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
                                    DataTable DtWithinearth_DMC_HotelRoom = _commonMethod.GenerateRoomtypeDataTableNewV2();
                                    DataTable dtwithinearth_DMC_Cancellation = _commonMethod.GenerateCancellationDataTableNewV2();
                                    DataTable DtRoomRateBreackUp = _commonMethod.GenerateoomRateBreackUpDataTableNewV2();

                                    int CutOffDays = XMLHotelBookingCommon.CutOffDays;
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

                                        string SearchKey = WE_DMCResponse.AvailabilityRS.SearchKey;
                                        //for (int Z = 0; Z < WE_DMCResponse.AvailabilityRS.HotelResult.Count(); Z++)
                                        {
                                            for (int x = 0; x < WE_DMCResponse.AvailabilityRS.HotelResult[i].HotelOption.Count(); x++)
                                            {
                                                string HotelOptionId = WE_DMCResponse.AvailabilityRS.HotelResult[i].HotelOption[x].HotelOptionId;

                                                for (int ii = 0; ii < WE_DMCResponse.AvailabilityRS.HotelResult[i].HotelOption[x].HotelRooms.Count(); ii++)
                                                {
                                                    //if (ii > CacheMaster.RoomLimit_10)
                                                    //    break;


                                                    if (commonSearch.AgentId == 4440 || commonSearch.AgentId == 4901)
                                                    {
                                                        if (ii > CacheMaster.RoomLimit_20)
                                                            break;
                                                    }
                                                    else
                                                    {
                                                        if (ii > CacheMaster.RoomLimit_10)
                                                            break;
                                                    }


                                                    for (int y = 0; y < WE_DMCResponse.AvailabilityRS.HotelResult[i].HotelOption[x].HotelRooms[ii].Count(); y++)
                                                    {
                                                        if (WE_DMCResponse.AvailabilityRS.HotelResult[i].HotelOption[x].HotelRooms[ii][y].BookingStatus != "Available")
                                                        {
                                                            continue;
                                                        }
                                                        bool iscombine = WE_DMCResponse.AvailabilityRS.HotelResult[i].HotelOption[x].IsCombineRoom;
                                                        //if (SearchParam.Rooms.Count() != WE_DMCResponse.AvailabilityRS.HotelResult[i].HotelOption[x].HotelRooms[ii].Count())
                                                        //    iscombine = true;
                                                        double CompanyBuying = Convert.ToDouble(WE_DMCResponse.AvailabilityRS.HotelResult[i].HotelOption[x].HotelRooms[ii][y].Price) * CurrencyFactorWE_DMC;
                                                        string RateDetailsToken = WE_DMCResponse.AvailabilityRS.HotelResult[i].HotelOption[x].HotelRooms[ii][y].RateDetailsToken;
                                                        string RoomToken = WE_DMCResponse.AvailabilityRS.HotelResult[i].HotelOption[x].HotelRooms[ii][y].RoomToken;
                                                        //string RoomTypeId = WE_DMCResponse.AvailabilityRS.HotelResult[i].HotelOption[x].HotelRooms[ii][y].RoomToken + "$$$" + RateDetailsToken + "$$$" + SearchKey + "$$$" + Hotelo;
                                                        string RoomTypeId = SearchKey + "$$$" + HotelOptionId + "$$$" + RoomToken;
                                                        string RoomTypeName = WE_DMCResponse.AvailabilityRS.HotelResult[i].HotelOption[x].HotelRooms[ii][y].RoomTypeName;
                                                        string MealName = WE_DMCResponse.AvailabilityRS.HotelResult[i].HotelOption[x].HotelRooms[ii][y].MealName;
                                                        string CancellationToken = WE_DMCResponse.AvailabilityRS.HotelResult[i].HotelOption[x].HotelRooms[ii][y].CancellationToken;
                                                        //string CancellationTokenSK = CancellationToken + "$$$" + SearchKey;


                                                        #region RateBreakUp

                                                        //for (int n = 0; n < SearchParam.NoofNights; n++)
                                                        //{
                                                        //    DateTime date = (Convert.ToDateTime(checkin).Date).AddDays(n);
                                                        //    string rateWeekColumn = Convert.ToString(date.DayOfWeek).Substring(0, 3);

                                                        //    #region Vat
                                                        //    Double VatOnRateBrackUp = 0;
                                                        //    double PriceRoom = Common.ProcessHotelRateBreakupPrice_WithMarkup(1, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), CompanyBuying / SearchParam.NoofNights, commonSearch.B2CMarkup, commonSearch.IsParentId, commonSearch.isPerce, commonSearch.AgentPercentage, AgentMarkupAmount, SubAgentMarkup, ref VatOnRateBrackUp);
                                                        //    PriceRoom = PriceRoom / SearchParam.NoofNights;
                                                        //    #endregion
                                                        //    //double PriceRoom = Common.getLowestOrBeforeDiscount(CompanyBuying, B2CMarkup, IsParentId, SubAgentMarkup, isPerce, AgentPercentage, AgentMarkupAmount);
                                                        //    DataRow drWithinearth_DMC_RateBreackUp = Dtwithinearth_DMC_RateBreackUp.NewRow();
                                                        //    drWithinearth_DMC_RateBreackUp["SupplierName"] = Provider;
                                                        //    drWithinearth_DMC_RateBreackUp["OwnsystemHotelId"] = OwnsystemHotelId;
                                                        //    drWithinearth_DMC_RateBreackUp["ContractID"] = 0;
                                                        //    drWithinearth_DMC_RateBreackUp["RoomTypeName"] = RoomTypeName;
                                                        //    drWithinearth_DMC_RateBreackUp["RoomTypeId"] = RoomTypeId;
                                                        //    drWithinearth_DMC_RateBreackUp["MealId"] = MealName;
                                                        //    drWithinearth_DMC_RateBreackUp["Meal"] = MealName;
                                                        //    drWithinearth_DMC_RateBreackUp["DateOfWeek"] = rateWeekColumn;
                                                        //    drWithinearth_DMC_RateBreackUp["Date"] = date;
                                                        //    drWithinearth_DMC_RateBreackUp["Rate"] = PriceRoom;
                                                        //    drWithinearth_DMC_RateBreackUp["RateType"] = "A Rate";
                                                        //    drWithinearth_DMC_RateBreackUp["RoomNo"] = WE_DMCResponse.AvailabilityRS.HotelResult[Z].HotelOption[x].HotelRooms[ii][y].RoomNo;
                                                        //    drWithinearth_DMC_RateBreackUp["RoomSequenceNo"] = HotelOptionId;
                                                        //    drWithinearth_DMC_RateBreackUp["BookingStatus"] = WE_DMCResponse.AvailabilityRS.HotelResult[Z].HotelOption[x].HotelRooms[ii][y].BookingStatus;
                                                        //    drWithinearth_DMC_RateBreackUp["SupplierCurrency"] = currency;
                                                        //    drWithinearth_DMC_RateBreackUp["BuyingPrice"] = PriceRoom / CurrencyFactorWE_DMC;
                                                        //    Dtwithinearth_DMC_RateBreackUp.Rows.Add(drWithinearth_DMC_RateBreackUp);
                                                        //}
                                                        #endregion

                                                        #region Rooms

                                                        DataRow drWithinearth_DMCRoom = DtWithinearth_DMC_HotelRoom.NewRow();
                                                        drWithinearth_DMCRoom["OwnsystemHotelId"] = OwnsystemHotelId;
                                                        drWithinearth_DMCRoom["RoomTypeName"] = WE_DMCResponse.AvailabilityRS.HotelResult[i].HotelOption[x].HotelRooms[ii][y].RoomTypeName;
                                                        drWithinearth_DMCRoom["RoomTypeCode"] = x + "-" + i;
                                                        //drWithinearth_DMCRoom["RoomTypeCode"] = RateDetailsToken;
                                                        drWithinearth_DMCRoom["MealId"] = MealName;
                                                        drWithinearth_DMCRoom["MealName"] = MealName;
                                                        drWithinearth_DMCRoom["ContractID"] = 0;
                                                        drWithinearth_DMCRoom["BoardType"] = CancellationToken;
                                                        drWithinearth_DMCRoom["RoomTypeId"] = RoomTypeId;
                                                        drWithinearth_DMCRoom["AdultsOccupancy"] = SearchParam.Rooms[y].NoofAdults;
                                                        drWithinearth_DMCRoom["ChildOccupancy"] = SearchParam.Rooms[y].NoofChild;
                                                        drWithinearth_DMCRoom["MaxExtraBed"] = 0;
                                                        drWithinearth_DMCRoom["RoomNo"] = WE_DMCResponse.AvailabilityRS.HotelResult[i].HotelOption[x].HotelRooms[ii][y].RoomNo;
                                                        drWithinearth_DMCRoom["RoomSequenceNo"] = HotelOptionId;
                                                        drWithinearth_DMCRoom["BookingStatus"] = WE_DMCResponse.AvailabilityRS.HotelResult[i].HotelOption[x].HotelRooms[ii][y].BookingStatus;
                                                        drWithinearth_DMCRoom["UniqueNo"] = OwnsystemHotelId;
                                                        drWithinearth_DMCRoom["OfferDescription"] = "";
                                                        //if (commonSearch.advancedOptions.IsFliggy)
                                                        //drWithinearth_DMCRoom["allocationDetails"] = WE_DMCResponse.AvailabilityRS.HotelResult[i].HotelOption[x].HotelRooms[ii][y].RoomCode;
                                                        //else
                                                        drWithinearth_DMCRoom["allocationDetails"] = "";

                                                        drWithinearth_DMCRoom["OfferCode"] = "";
                                                        drWithinearth_DMCRoom["OfficeId"] = 0;
                                                        drWithinearth_DMCRoom["EssentialInformation"] = WE_DMCResponse.AvailabilityRS.HotelResult[i].HotelOption[x].HotelRooms[ii][y].EssentialInformation;
                                                        drWithinearth_DMCRoom["SupplierName"] = Provider;

                                                        double ItemPrice = CompanyBuying; //Convert.ToDouble(totalPrice);
                                                        _markup.ProcessHotelRoom_WithMarkup(CurrencyFactorWE_DMC, currency, true, iscombine, SearchParam.NoofNights, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), SearchParam.CheckInDate, SearchParam.CheckOutDate, ref ItemPrice, ref drWithinearth_DMCRoom, isPerce, AgentPercentage, AgentMarkupAmount, commonSearch.SubAgentMarkup, commonSearch.B2CMarkup, commonSearch.IsParentId);

                                                        //drWithinearth_DMCRoom["SupplierName"] = Provider;
                                                        DtWithinearth_DMC_HotelRoom.Rows.Add(drWithinearth_DMCRoom);
                                                        #endregion

                                                        #region  "Cancellationpolicy"
                                                        if (WE_DMCResponse.AvailabilityRS.HotelResult[i].HotelOption[x].HotelRooms[ii][y].CancellationPolicy != null)
                                                        {
                                                            for (int c = 0; c < WE_DMCResponse.AvailabilityRS.HotelResult[i].HotelOption[x].HotelRooms[ii][y].CancellationPolicy.Count(); c++)
                                                            {
                                                                double CancellationPrice = Convert.ToDouble(WE_DMCResponse.AvailabilityRS.HotelResult[i].HotelOption[x].HotelRooms[ii][y].CancellationPolicy[c].CancellationPrice) * CurrencyFactorWE_DMC;
                                                                DateTime FromDate = Convert.ToDateTime(WE_DMCResponse.AvailabilityRS.HotelResult[i].HotelOption[x].HotelRooms[ii][y].CancellationPolicy[c].FromDate);
                                                                DateTime ToDate = Convert.ToDateTime(WE_DMCResponse.AvailabilityRS.HotelResult[i].HotelOption[x].HotelRooms[ii][y].CancellationPolicy[c].ToDate);

                                                                #region "Calculation"

                                                                string SPUI = "";
                                                                double AppliedAgentCharges = CancellationPrice;
                                                                #region "Vat"
                                                                double VatOnPolicy = 0;
                                                                double CompanyChargeAmount = CancellationPrice;
                                                                double WE_DMCMainAgentCharge = CancellationPrice;
                                                                AppliedAgentCharges = _markup.ProcessHotelCancelationPrice_WithMarkup(ref WE_DMCMainAgentCharge, SearchParam.NoofNights, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), CancellationPrice, isPerce, commonSearch.B2CMarkup, AgentPercentage, AgentMarkupAmount, commonSearch.IsParentId, commonSearch.SubAgentMarkup, ref VatOnPolicy);

                                                                #endregion

                                                                int WE_DMCCutOffDays = XMLHotelBookingCommon.CutOffDays;
                                                                WE_DMCCutOffDays = _commonMethod.GetCutOffDays(commonSearch.dtSuplierMarkup, Provider);
                                                                DateTime CancellationDate = DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", CultureInfo.InvariantCulture).Date;
                                                                DateTime NowDate = DateTime.Now.Date;
                                                                double CancelCharge = AppliedAgentCharges;
                                                                if (AppliedAgentCharges > 0)
                                                                    CancellationDate = FromDate;
                                                                else
                                                                    CancellationDate = ToDate;

                                                                // CancellationDate = CancellationDate.AddDays(-CancelDAY);

                                                                #endregion

                                                                if (CancellationDate.AddDays(WE_DMCCutOffDays).Date > DateTime.Now.Date)
                                                                    CancellationDate = CancellationDate.AddDays(WE_DMCCutOffDays).Date;
                                                                if (NowDate.Date < CancellationDate.Date)
                                                                {
                                                                    if (AppliedAgentCharges == 0)
                                                                    {
                                                                        double Zero = 0;
                                                                        dtwithinearth_DMC_Cancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref Zero, dtwithinearth_DMC_Cancellation.NewRow(), CurrencyFactorWE_DMC, currency, Provider, Convert.ToString(OwnsystemHotelId), Convert.ToString(y + 1),
                                                                        RoomTypeId, RoomTypeName, MealName, NowDate, CancellationDate.AddDays(-1).AddHours(23).AddMinutes(59).AddSeconds(59), 0, 0, "", VatOnPolicy, "", "NO", CancellationToken, SPUI, "", ""));
                                                                        dtwithinearth_DMC_Cancellation.AcceptChanges();
                                                                    }
                                                                    else
                                                                    {
                                                                        dtwithinearth_DMC_Cancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref CompanyChargeAmount, dtwithinearth_DMC_Cancellation.NewRow(), CurrencyFactorWE_DMC, currency, Provider, Convert.ToString(OwnsystemHotelId), Convert.ToString(y + 1),
                                                                        RoomTypeId, RoomTypeName, MealName, CancellationDate.Date, DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null), AppliedAgentCharges, CancelCharge, "", VatOnPolicy, "", "NO", CancellationToken, SPUI, "", ""));
                                                                        dtwithinearth_DMC_Cancellation.AcceptChanges();
                                                                    }
                                                                }
                                                                else
                                                                {
                                                                    // DataRow drWE_DMCHotelCancellation_Temp = dtwithinearth_techno_Cancellation.NewRow();
                                                                    dtwithinearth_DMC_Cancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref CompanyChargeAmount, dtwithinearth_DMC_Cancellation.NewRow(), CurrencyFactorWE_DMC, currency, Provider, Convert.ToString(OwnsystemHotelId), Convert.ToString(y + 1),
                                                                RoomTypeId, RoomTypeName, MealName, DateTime.Now.Date, DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null), AppliedAgentCharges, CancelCharge, "", VatOnPolicy, "", "YES", CancellationToken, SPUI, "", ""));
                                                                    dtwithinearth_DMC_Cancellation.AcceptChanges();
                                                                }
                                                            }
                                                        }
                                                        #endregion
                                                    }
                                                }
                                            }
                                        }
                                        Options ops = (await _supplierRes.GetCommonRoomForSuppliers(GiataId, commonSearch.tokenData, commonSearch.advancedOptions, Item_Price, drXmlHotel, DtWithinearth_DMC_HotelRoom, dtwithinearth_DMC_Cancellation, DtRoomRateBreackUp, SearchParam));
                                        if (ops != null)
                                            options.Add(ops);
                                    }
                                    #endregion                                    
                                }
                                #endregion
                                #endregion
                            }

                            #endregion

                        }
                        else
                            continue;
                    }
                    #endregion
                }

                return options;
            }
            catch (Exception ex)
            {
                await _exception.Error(ex);
                return options;
            }
            //});
            finally
            {
                #region ResProcessTimer
                try
                {
                    if (SearchParam != null && ResProccessTimerLogActive == "1" && timerroomx.ElapsedMilliseconds > Convert.ToInt32(ResProccessTimeValueforSaveLog))
                    {
                        timerroomx.Stop();
                        SearchParam.SupplierTime += ",|Finish:" + Provider + " Process Response  " + "|Time :" + timerroomx.Elapsed.ToString();
                        _ = Task.Run(() => _commonMethod.FireAway_SQL_ResPorcessTimeLog("", timerroomx.Elapsed.ToString(), Provider, SearchParam.SearchKey, 1, SearchParam.AgentID));
                    }
                }
                catch (Exception x)
                {
                    await _exception.Error(x);
                }
                #endregion
            }
        }

        public async Task<CancellationDto> Get_Withinearth_DMC_CancelationPolicy(DataSet dsHotel, string ownsystemid, string RoomTypeId_Own, DataTable dtAgentDetail, DataTable dtSuplierMarkup, DataTable dtMarkupDetails, DataTable dtSearchConfig, HotelSearchParameter objSearPera, string roomno, string HotelId, string Provider, int AgentId, string GiataId, int hotelRating, string roomtypename, string Mealid, DataTable dtROE, string Search_Key, string IP, TokenData tokenData)
        {
            string Error = "";
            string IsBooking = "Available";
            CancellationDto cancellationDto = new CancellationDto();
            try
            {
                #region Get Crrency Code
                string Currency = "";
                
                #endregion
                //string Currency = ConfigurationManager.AppSettings["Bedlinker_techno_Currency"];
                //DataTable dtInrCurrency = Common.GetCountryCurrency(Currency);
                double CurrencyFactorWE_DMC = 1;
                if (Currency.ToLower() != tokenData.SiteCurrency.ToLower())
                {
                    CurrencyFactorWE_DMC = _commonMethod.GetROEByCurrency(Currency, dtROE);
                }
                //int HotelOptionId = 0;
                //DataRow[] drHotelCode = dsHotel.Tables[0].Select("XMLHotelCode='" + HotelId + "'");
                //string SearchKey = Convert.ToString(drHotelCode[0]["XMLCode"]);
                //string RoomToken = RoomTypeId_Own;
                DataTable dtWithinearth_DMC_Cancellation = _commonMethod.GenerateCancellationDataTableNewV2();
                Boolean isPerce = true;
                double AgentCompanyPercentage = Convert.ToDouble(objSearPera.AgentCompanyPercentage);
                bool IsB2C = Convert.ToBoolean(Convert.ToByte(objSearPera.IsB2BOrB2C));
                double B2CMarkup = (IsB2C == false ? AgentCompanyPercentage : XMLHotelBookingCommon.B2CMarkup);
                double AgentPercentage = 0.0;
                double AgentMarkupAmount = 0.0;
                double SubAgentMarkup = 0;
                Boolean blnIsShowSupplier = false;
                int IsParentId = 1;
                if (dtAgentDetail != null)
                {
                    IsParentId = Convert.ToInt32(dtAgentDetail.Rows[0]["IsParentId"]);
                    SubAgentMarkup = Convert.ToDouble(dtAgentDetail.Rows[0]["B2CMarkupPer"]);
                }
                _markup.getAgetMarkupSetting(Provider, GiataId, dtSuplierMarkup, dtMarkupDetails, ref AgentPercentage, ref AgentMarkupAmount, ref blnIsShowSupplier, ref isPerce, AgentId, hotelRating, objSearPera.CityID, Convert.ToString(objSearPera.CountryID), objSearPera.CheckInDate);
                int CutOffDays = XMLHotelBookingCommon.CutOffDays;
                CutOffDays = _commonMethod.GetCutOffDays(dtSuplierMarkup, Provider);
                DataRow[] drSuplierMarkup = (dtSuplierMarkup.Select("SupplierName='" + Provider + "'"));
                string UserID = Convert.ToString(drSuplierMarkup[0]["UserID"]);
                string password = Convert.ToString(drSuplierMarkup[0]["Password"]);
                Currency = Convert.ToString(drSuplierMarkup[0]["CurrencyShortName"]); //23-06-25
                //string URL = Convert.ToString(drSuplierMarkup[0]["WebSiteURL"]) + TechnoInfo.WE_DMCCancellationPolicyURL;
                string URL = Convert.ToString(drSuplierMarkup[0]["WebSiteURL"]) + _supplierConfig.Techno.WE_DMCCancellationPolicyURL;
                //DataRow[] drRoom = dsHotel.Tables[1].Select("RoomTypeId='" + RoomTypeId_Own + "'");


                //string CancellationToken = Convert.ToString(drRoom[0]["BoardType"]);
                string RoomTypeName = roomtypename;
                string[] RoomTypeIdSplit = RoomTypeId_Own.Split(new String[] { "$$$" }, StringSplitOptions.None);
                string RoomTypeId = RoomTypeId_Own;
                string SearchKey = RoomTypeIdSplit[0];
                string HotelOptionId = RoomTypeIdSplit[1];
                string RoomToken = RoomTypeIdSplit[2];

                string MealId = Mealid;
                string Request = Withinearth_DMCCancellationPolicyRequest(UserID, objSearPera, SearchKey, HotelOptionId, RoomToken, Currency, IP);
                _commonMethod.ThirdPartyGetPolicyHistory(RoomTypeId, Convert.ToString(SearchKey), Request, "-", "After Get Policy", Provider, "Response", Search_Key, "");

                ResponseDto responseDto = await GetSupplierResponseWE_DMC(Request, URL, 30000, Provider, objSearPera, false);
                string Response = responseDto.Response;
                Error = responseDto.Error;

                if (Response == string.Empty)
                {
                    cancellationDto.dsHotelData = dsHotel;
                    cancellationDto.ISBooking = "NotAvailable";
                    return cancellationDto;

                }
                _commonMethod.ThirdPartyGetPolicyHistory(RoomTypeId, Convert.ToString(SearchKey), Request, Response + Error, "After Get Policy", Provider, "Response", Search_Key, "");

                CancellationPilicyRS WE_DMCResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<CancellationPilicyRS>(Response);
                //string ResponseData2 = Newtonsoft.Json.JsonConvert.DeserializeXmlNode(Response, "root").InnerXml;
                //DataSet WE_DMC_Response = Common.GetDataSetFromXMLString(ResponseData2);


                #region  Cancellation policy
                string EssentialInformation = WE_DMCResponse.CancellationRS.EssentialInformation;
                for (int c = 0; c < WE_DMCResponse.CancellationRS.CancellationPolicy.Count(); c++)
                {
                    double CancellationPrice = Convert.ToDouble(WE_DMCResponse.CancellationRS.CancellationPolicy[c].CancellationPrice) * CurrencyFactorWE_DMC;//Convert.ToDouble(WE_DMCResponse.CancellationPilicyRS.HotelResult[0].HotelOption[0].HotelRooms[0][0].CancellationPolicy[c].CancellationPrice);
                    DateTime FromDate = Convert.ToDateTime(WE_DMCResponse.CancellationRS.CancellationPolicy[c].FromDate); //Convert.ToDateTime(WE_DMCResponse.CancellationPilicyRS.HotelResult[0].HotelOption[0].HotelRooms[0][0].CancellationPolicy[c].FromDate);
                    DateTime ToDate = Convert.ToDateTime(WE_DMCResponse.CancellationRS.CancellationPolicy[c].ToDate); //Convert.ToDateTime(WE_DMCResponse.CancellationPilicyRS.HotelResult[0].HotelOption[0].HotelRooms[0][0].CancellationPolicy[c].ToDate);

                    #region "Calculation"

                    string SPUI = EssentialInformation;
                    double AppliedAgentCharges = CancellationPrice;
                    #region Vat
                    double VatOnPolicy = 0;
                    double CompanyChargeAmount = CancellationPrice;
                    double WE_DMCMainAgentCharge = CancellationPrice;
                    AppliedAgentCharges = _markup.ProcessHotelCancelationPrice_WithMarkup(ref WE_DMCMainAgentCharge, objSearPera.NoofNights, objSearPera.CityID, Convert.ToString(objSearPera.CountryID), CancellationPrice, isPerce, B2CMarkup, AgentPercentage, AgentMarkupAmount, IsParentId, SubAgentMarkup, ref VatOnPolicy);

                    #endregion
                    int WE_DMCCutOffDays = XMLHotelBookingCommon.CutOffDays;
                    WE_DMCCutOffDays = _commonMethod.GetCutOffDays(dtSuplierMarkup, Provider);
                    DateTime CancellationDate = DateTime.ParseExact(objSearPera.CheckInDate, "dd/MM/yyyy", CultureInfo.InvariantCulture).Date;
                    DateTime NowDate = DateTime.Now.Date;
                    double CancelCharge = AppliedAgentCharges;
                    if (AppliedAgentCharges > 0)
                        CancellationDate = FromDate;
                    else
                        CancellationDate = ToDate;

                    //CancellationDate = CancellationDate.AddDays(-CancelDAY);

                    #endregion

                    if (CancellationDate.AddDays(WE_DMCCutOffDays).Date > DateTime.Now.Date)
                        CancellationDate = CancellationDate.AddDays(WE_DMCCutOffDays).Date;
                    if (NowDate.Date < CancellationDate.Date)
                    {
                        if (AppliedAgentCharges == 0)
                        {
                            double Zero = 0;
                            dtWithinearth_DMC_Cancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref Zero, dtWithinearth_DMC_Cancellation.NewRow(), CurrencyFactorWE_DMC, Currency, Provider, ownsystemid, Convert.ToString(roomno),
                            RoomTypeId, RoomTypeName, MealId, NowDate, CancellationDate, 0, 0, "", VatOnPolicy, "", "NO", SearchKey, SPUI, "", ""));
                            dtWithinearth_DMC_Cancellation.AcceptChanges();
                        }
                        else
                        {
                            dtWithinearth_DMC_Cancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref CompanyChargeAmount, dtWithinearth_DMC_Cancellation.NewRow(), CurrencyFactorWE_DMC, Currency, Provider, ownsystemid, Convert.ToString(roomno),
                            RoomTypeId, RoomTypeName, MealId, CancellationDate.Date, DateTime.ParseExact(objSearPera.CheckInDate, "dd/MM/yyyy", null), AppliedAgentCharges, CancelCharge, "", VatOnPolicy, "", "NO", SearchKey, SPUI, "", ""));
                            dtWithinearth_DMC_Cancellation.AcceptChanges();
                        }
                    }
                    else
                    {
                        // DataRow drWE_DMCHotelCancellation_Temp = dtWithinearth_DMC_Cancellation.NewRow();
                        dtWithinearth_DMC_Cancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref CompanyChargeAmount, dtWithinearth_DMC_Cancellation.NewRow(), CurrencyFactorWE_DMC, Currency, Provider, ownsystemid, Convert.ToString(roomno),
                        RoomTypeId, RoomTypeName, MealId, DateTime.Now.Date, DateTime.ParseExact(objSearPera.CheckInDate, "dd/MM/yyyy", null), AppliedAgentCharges, CancelCharge, "", VatOnPolicy, "", "YES", SearchKey, SPUI, "", ""));
                        dtWithinearth_DMC_Cancellation.AcceptChanges();
                    }
                }
                #endregion
                dsHotel.Tables[2].Merge(dtWithinearth_DMC_Cancellation);
            }
            catch (Exception ex)
            {
                _ = _exception.ErrorNew(ex, _LogAPI.Recheck, Search_Key);

                cancellationDto.dsHotelData = null;
                cancellationDto.ISBooking = "NotAvailable";
                return cancellationDto;
            }

            cancellationDto.dsHotelData = dsHotel;
            cancellationDto.ISBooking = IsBooking;
            return cancellationDto;

        }

        public string Withinearth_DMCCancellationPolicyRequest(string UserID, HotelSearchParameter sp, string SearchKey, string HotelOptionId, string RoomToken, string Currency, string IP)
        {
            try
            {
                string Token = UserID;

                string Request = "";
                StringBuilder SBreq = new StringBuilder();
                SBreq.Append("{");
                SBreq.Append("\"Token\":\"" + Token + "\",");
                SBreq.Append("\"Request\":{");
                SBreq.Append("\"SearchKey\":\"" + SearchKey + "\",");
                SBreq.Append("\"HotelOptionId\":\"" + HotelOptionId + "\",");
                SBreq.Append("\"RoomToken\":\"" + RoomToken + "\"");
                SBreq.Append("},");
                SBreq.Append("\"AdvancedOptions\":{");
                SBreq.Append("\"Currency\":\"" + Currency + "\",");
                // SBreq.Append("\"CustomerIpAddress\":\"" + Common.GetIPAddress() + "\"");
                SBreq.Append("\"CustomerIpAddress\":\"" + IP + "\"");
                SBreq.Append("}");
                SBreq.Append("}");
                Request = SBreq.ToString();
                return Request;
            }
            catch (Exception ex)
            {
                _ = _exception.Error(ex);
                return "";
            }
        }

        public async Task<ResponseDto> GetSupplierResponseWE_DMC(string strxmlRQRS, string strHost, int TimeOut, string Provider = "", HotelSearchParameter para = null, bool DebugMode = false, string UniqueNo = "", string Search_key = "")
        {
            //System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
            //timer.Start();

            string Error = "";
            string strOutput = "";
            ResponseDto responseDto = new ResponseDto();
            try
            {

                HttpWebRequest Request;
                Request = (HttpWebRequest)HttpWebRequest.Create(strHost);
                Request.Timeout = TimeOut;
                Request.Proxy = null;
                Request.Method = "POST";
                Request.UserAgent = "Mozilla/4.0 (compatible; MSIE 6.0; MS Web Services Client Protocol 1.1.4322.2300)";
                //Request.ContentType = "text/xml;charset=utf-8;";
                //Request.Accept = "text/xml";
                Request.Headers.Add("Accept-Encoding", "gzip,deflate");
                Request.KeepAlive = false;
                Request.ContentType = "application/json";
                Request.Accept = null;
                Stream newStream = null;
                byte[] data = Encoding.ASCII.GetBytes(strxmlRQRS);
                Request.ContentLength = data.Length;
                newStream = Request.GetRequestStream();
                newStream.Write(data, 0, data.Length);
                try
                {
                    HttpWebResponse Response = (HttpWebResponse)await Request.GetResponseAsync();
                    Stream responseStream = Response.GetResponseStream();
                    responseStream = new System.IO.Compression.DeflateStream(responseStream, System.IO.Compression.CompressionMode.Decompress);
                    StreamReader Reader = new StreamReader(responseStream, Encoding.UTF8);

                    strOutput = await Reader.ReadToEndAsync();

                    responseStream.Close();
                    Reader.Close();
                    Response.Close();
                    //timer.Stop();
                    //if (para != null)
                    //{
                    //    para.SupplierTime += ",SupplierName :" + Provider + " : " + "Execution Time :" + timer.Elapsed.ToString();
                    //    if (DebugMode)
                    //        Common.XMLRequestResponseTrack(strxmlRQRS, strOutput, Provider, "Proper");
                    //}
                }
                //catch (WebException webExcp)
                catch (Exception ex)
                {
                    Error = ex.Message;
                    //HttpWebResponse httpResponse = (HttpWebResponse)webExcp.Response;
                    //string result = httpResponse.StatusDescription.ToString();
                    //string result1 = new StreamReader(webExcp.Response.GetResponseStream()).ReadToEnd();
                    //Common.XMLRequestResponseTrackError(strxmlRQRS, result1, Provider, "Proper");
                    //return result1;
                    _ = _exception.Error(ex, Search_key, strxmlRQRS);
                    if (UniqueNo != "")
                    {
                        _commonMethod.ThirdPartyBookingHistory(Convert.ToString(UniqueNo), Convert.ToString(UniqueNo), strxmlRQRS, ex.ToString(), "Error Log [GetSupplierResponse  " + Provider + "]", Provider, "Request Confirm j=" + 0 + "", "-", "WE");
                    }


                    responseDto.Response = "";
                    responseDto.Error = Error;
                    return responseDto;

                }
            }
            catch (Exception ex)
            {
                Error = ex.Message;
                _ = _exception.Error(ex, Search_key, strxmlRQRS);
                if (UniqueNo != "")
                {
                    _commonMethod.ThirdPartyBookingHistory(Convert.ToString(UniqueNo), Convert.ToString(UniqueNo), strxmlRQRS, ex.ToString(), "Error Log2 [GetSupplierResponse " + Provider + "]", Provider, "Request Confirm j=" + 0 + "", "-", "WE");
                }

                responseDto.Response = "";
                responseDto.Error = Error;
                return responseDto;
            }

            responseDto.Response = strOutput;
            responseDto.Error = Error;
            return responseDto;
        }

        public async Task<PriceCheckDidaDto> Get_Withinearth_DMC_PreBookingReqRes(string Provider, bool IsCombine, DataSet dsCartDetails, string ReferenceNo, string ID, string Password, int HotelCode, string RoomToken, string CheckInDate, string CheckOutDate, string URL, double Rate, double FinalPrice, int UserCartId, int UserCartDetailId, DataTable dtCheckPriceReqStructure, int j, string UniqueNo, string AgentUniqueNo, bool isForBooking, int RoomNo, string SearchKey, string HotelOptionId, string Currency, double CurrencyFactorWithinearth_DMC, string Searchkey, int AgentId = 0, string IP = "")
        {
            PriceCheckDidaDto priceCheckDidaDto = new PriceCheckDidaDto();
            string responsetype = "Success";
            string time = "";
            string SupplierName = Provider;
            string Error = "";

            // flag:0= priceCheck Success
            // flag:1= priceCheck Response Error or Empty
            // flag:2= Error in PriceCheck
            // flag:3= Currency Cheange in PriceCheck Response
            // flag:4= Room is OnRequest or N/A at priceCheck
            // flag:5= Price is Changed...!
            int flag = 1;
            string Req = "";
            string ResponseData = "";

            try
            {
                //double Totalprice = 0;
                //double CurrencyFactorWithinearth_DMC = 10;
                //string Currency = "USD";
                //switch (Provider)
                //{
                //    case "bedlinker_techno":
                //        Currency = ConfigurationManager.AppSettings["Bedlinker_techno_Currency"];
                //        break;
                //}
                //DataTable dtInrCurrency = Common.GetCountryCurrency(Currency);
                //CurrencyFactorWithinearth_DMC = Convert.ToDouble(dtInrCurrency.Rows[0]["AmtToExch"]);
                // CheckInDate = CheckInDate.Substring(0, 4) + "-" + CheckInDate.Substring(4, 2) + "-" + CheckInDate.Substring(6, 2);
                // CheckOutDate = CheckOutDate.Substring(0, 4) + "-" + CheckOutDate.Substring(4, 2) + "-" + CheckOutDate.Substring(6, 2);
                CheckInDate = CheckInDate.Substring(4, 2) + "-" + CheckInDate.Substring(6, 2) + "-" + CheckInDate.Substring(0, 4);
                CheckOutDate = CheckOutDate.Substring(4, 2) + "-" + CheckOutDate.Substring(6, 2) + "-" + CheckOutDate.Substring(0, 4);
                double OriginalRate = Math.Round(Rate / CurrencyFactorWithinearth_DMC, 2);
                Req = Withinearth_DMCPreBookRequest(ID, SearchKey, HotelOptionId, RoomNo, RoomToken, dsCartDetails, ref Rate, IsCombine, Currency, IP);

                _commonMethod.ThirdPartyBookingHistory(Convert.ToString(UniqueNo), Convert.ToString(AgentUniqueNo), Req, "-", "Before Get Price " + Provider + " Request hit booking" + j + "isForBooking" + isForBooking, Provider, "Response Confirm", Searchkey, "WithinEarth");

                if (isForBooking == false)// hit if not from booking  
                {
                    dtCheckPriceReqStructure.Rows.Add(_commonMethod.AddCheckPriceReq(dtCheckPriceReqStructure.NewRow(), j + 1, UserCartId, UserCartDetailId, RoomToken, Provider, Req));
                }
                //string Action = URL+"";
                //string ResponseData = XMLHotelBookingCommon.GetSupplierResponse("", Req, Action, 120000, "withinearth_techno");

                System.Diagnostics.Stopwatch timerroomprocess = new System.Diagnostics.Stopwatch();
                timerroomprocess.Start();

                //ResponseData = GetSupplierResponseWE_DMC_report(ref Error, Req, URL, 180000, Provider, null, false, UniqueNo, Searchkey);
                ResponseDto responseDto = await GetSupplierResponseWE_DMC(Req, URL, 180000, Provider, null, false, UniqueNo, Searchkey);
                ResponseData = responseDto.Response;
                Error = responseDto.Error;

                timerroomprocess.Stop();
                time = timerroomprocess.Elapsed.ToString();


                _commonMethod.ThirdPartyBookingHistory(Convert.ToString(UniqueNo), Convert.ToString(AgentUniqueNo), Req, ResponseData + Error, "After Get Price " + Provider + " Request hit booking" + j + "isForBooking" + isForBooking, Provider, "Response Confirm", Searchkey, "WithinEarth");

                PreBookResponse WE_DMCResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<PreBookResponse>(ResponseData);

                if (ResponseData == string.Empty || ResponseData == "")
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

                    priceCheckDidaDto.Flag = 1;
                    return priceCheckDidaDto;
                }
                else
                {
                    string BookingToken = WE_DMCResponse.PreBookRS.HotelOption.BookingToken;

                    string Status = WE_DMCResponse.PreBookRS.HotelOption.Status;
                    switch (Status.ToLower())
                    {
                        case "bookable":
                            break;
                        case "error":
                            try
                            {
                                responsetype = "GeneralError";
                                string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, Req, ResponseData, Error);
                            }
                            catch (Exception e)
                            {
                                _ = _exception.Error(e, Searchkey);
                            }
                            priceCheckDidaDto.Flag = 1;
                            return priceCheckDidaDto;
                            break;
                        case "pricechange":
                            break;
                        case "notallowed":
                            try
                            {
                                responsetype = "GeneralError";
                                string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, Req, ResponseData, Error);
                            }
                            catch (Exception e)
                            {
                                _ = _exception.Error(e, Searchkey);
                            }
                            priceCheckDidaDto.Flag = 4;
                            return priceCheckDidaDto;
                            break;

                    }
                    int IsPriceChange = WE_DMCResponse.PreBookRS.HotelOption.IsPriceChange;
                    string CheckIndate = WE_DMCResponse.PreBookRS.HotelOption.CheckInDate;
                    string CheckOutdate = WE_DMCResponse.PreBookRS.HotelOption.CheckOutDate;
                    double TotalPrice = Convert.ToDouble(WE_DMCResponse.PreBookRS.HotelOption.TotalPrice);
                    string StrUid = "";
                    for (int i = 0; i < WE_DMCResponse.PreBookRS.HotelOption.HotelRooms.Count(); i++)
                    {
                        int UniqueId = WE_DMCResponse.PreBookRS.HotelOption.HotelRooms[i].UniqueId;
                        string Room_No = WE_DMCResponse.PreBookRS.HotelOption.HotelRooms[i].RoomNo;
                        string RoomTypeName = WE_DMCResponse.PreBookRS.HotelOption.HotelRooms[i].RoomTypeName;
                        string MealName = WE_DMCResponse.PreBookRS.HotelOption.HotelRooms[i].MealName;
                        double Price = Convert.ToDouble(WE_DMCResponse.PreBookRS.HotelOption.HotelRooms[i].Price);
                        //FinalPrice = 0;//value1;
                        double OldPrice = Convert.ToDouble(WE_DMCResponse.PreBookRS.HotelOption.HotelRooms[i].PriceChange.OldPrice);
                        double NewPrice = Convert.ToDouble(WE_DMCResponse.PreBookRS.HotelOption.HotelRooms[i].PriceChange.NewPrice);

                        if (i != 0 && i == WE_DMCResponse.PreBookRS.HotelOption.HotelRooms.Count())
                            StrUid = StrUid + UniqueId + "|||" + Room_No;
                        else
                            StrUid = StrUid + UniqueId + "|||" + Room_No + "@@@";
                    }
                    FinalPrice = TotalPrice;
                    ReferenceNo = BookingToken + "$$$" + StrUid;

                    priceCheckDidaDto.FinalPrice = FinalPrice;
                    priceCheckDidaDto.ReferenceNo = ReferenceNo;
                }

                if (Convert.ToDouble((Math.Round(FinalPrice, 0)).TruncateDecimalPlaces(0)) > (Convert.ToDouble(Math.Round(OriginalRate, 0)).TruncateDecimalPlaces(0)))
                {
                    try
                    {
                        ResponseData = "#Price Changed " + "#Main Price = " + OriginalRate + " #New Price = " + FinalPrice + " #Response= " + ResponseData;
                        responsetype = "GeneralError";
                        string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, Req, ResponseData, Error);
                    }
                    catch (Exception e)
                    {
                        _ = _exception.Error(e, Searchkey);
                    }

                    priceCheckDidaDto.Flag = 5;
                    return priceCheckDidaDto;
                }
            }
            catch (Exception ex)
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
                _commonMethod.ThirdPartyBookingHistory(Convert.ToString(UniqueNo), Convert.ToString(AgentUniqueNo), Req, ex.Message.ToString(), "After Get Price " + Provider + " Request hit[Exception]" + j + "isForPriceCheck" + isForBooking, Provider, "Response Confirm", "-", "WithinEarth");
                _ = _exception.Error(ex, Searchkey);

                priceCheckDidaDto.Flag = 2;
                return priceCheckDidaDto;
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

            priceCheckDidaDto.Flag = 0;
            return priceCheckDidaDto;
        }

        public string Withinearth_DMCPreBookRequest(string UserID, string SearchKey, string HotelOptionId, int RoomNo, string RoomToken, DataSet dsCartDetails, ref double Rate, bool IsCombine, string Currency, string IP)
        {
            try
            {
                string Token = UserID;
                Rate = 0;
                string Request = "";
                StringBuilder SBreq = new StringBuilder();
                SBreq.Append("{");
                SBreq.Append("\"Token\":\"" + Token + "\",");
                SBreq.Append("\"Request\":{");
                SBreq.Append("\"SearchKey\":\"" + SearchKey + "\",");
                SBreq.Append("\"HotelOption\":{");
                SBreq.Append("\"HotelOptionId\":\"" + HotelOptionId + "\",");
                SBreq.Append("\"HotelRooms\":[");
                if (IsCombine)
                {
                    RoomNo = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[0]["RoomNumber"]);
                    string[] spitidRoomTypeIdWETH = Convert.ToString(dsCartDetails.Tables[1].DefaultView[0]["RoomTypeId"]).Split(new String[] { "$$$" }, StringSplitOptions.None);
                    SearchKey = spitidRoomTypeIdWETH[0];
                    HotelOptionId = spitidRoomTypeIdWETH[1];
                    RoomToken = spitidRoomTypeIdWETH[2];
                    SBreq.Append("{");
                    SBreq.Append("\"RoomNo\":\"" + RoomNo + "\",");
                    SBreq.Append("\"RoomToken\":\"" + RoomToken + "\"");
                    SBreq.Append("}");
                    Rate += Convert.ToDouble(dsCartDetails.Tables[1].DefaultView[0]["CompanyBuyingPrice"]);
                }
                else
                {
                    for (int i = 0; i < dsCartDetails.Tables[1].DefaultView.Count; i++)
                    {
                        RoomNo = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[i]["RoomNumber"]);
                        string[] spitidRoomTypeIdWETH = Convert.ToString(dsCartDetails.Tables[1].DefaultView[i]["RoomTypeId"]).Split(new String[] { "$$$" }, StringSplitOptions.None);
                        SearchKey = spitidRoomTypeIdWETH[0];
                        HotelOptionId = spitidRoomTypeIdWETH[1];
                        RoomToken = spitidRoomTypeIdWETH[2];
                        SBreq.Append("{");
                        SBreq.Append("\"RoomNo\":\"" + RoomNo + "\",");
                        SBreq.Append("\"RoomToken\":\"" + RoomToken + "\"");
                        SBreq.Append("}");
                        if (dsCartDetails.Tables[1].DefaultView.Count > 1 && i != (dsCartDetails.Tables[1].DefaultView.Count - 1))
                        {
                            SBreq.Append(",");
                        }
                        Rate += Convert.ToDouble(dsCartDetails.Tables[1].DefaultView[i]["CompanyBuyingPrice"]);
                    }
                }
                //SBreq.Append("\"CancellationToken\": \"" + CancellationToken + "\",");
                //SBreq.Append("\"RateDetailsToken\": \"" + RateDetailsToken + "\"");
                //SBreq.Append("}");
                SBreq.Append("]");
                SBreq.Append("}");
                SBreq.Append("},");
                SBreq.Append("\"AdvancedOptions\":{");
                SBreq.Append("\"Currency\":\"" + Currency + "\",");
                //SBreq.Append("\"CustomerIpAddress\":\"" + Common.GetIPAddress() + "\"");
                SBreq.Append("\"CustomerIpAddress\":\"" + IP + "\"");
                SBreq.Append("}");
                SBreq.Append("}");
                Request = SBreq.ToString();
                return Request;

            }
            catch (Exception ex)
            {
                _ = _exception.Error(ex);
                return "";
            }
        }

        public string Withinearth_DMCBookingRequest(DataRow[] Withinearth_DMCDetails, string SearchKey, string BToken, double TotalPrice, string InternalReference, string RoomNo, DataSet dsCartDetails, DataTable dtPassengers, string UniqueNo, string AgentUniqueNo, string WE_DMCCurrency,string IP)
        {
            try
            {
                string[] strBookingTokenMain = BToken.Split(new String[] { "§§§" }, StringSplitOptions.None);
                BToken = strBookingTokenMain[0];
                TotalPrice = Convert.ToDouble(strBookingTokenMain[1]);
                #region Booking RQ
                string Token = Convert.ToString(Withinearth_DMCDetails[0]["UserID"]);
                string PaxType = "";
                string Request = "";
                string[] strBookingToken = BToken.Split(new String[] { "$$$" }, StringSplitOptions.None);
                string BookingToken = strBookingToken[0];
                string[] strUniqueId = strBookingToken[1].Split(new String[] { "$$$" }, StringSplitOptions.None);
                string[] strUniqueIdSplit = strUniqueId[0].Split(new String[] { "@@@" }, StringSplitOptions.None);
                StringBuilder SB_RoomDetails = new StringBuilder();

                for (int j = 0; j < dsCartDetails.Tables[1].DefaultView.Count; j++)
                {
                    string UniqueId = strUniqueIdSplit[j].Split(new String[] { "|||" }, StringSplitOptions.None)[0];
                    RoomNo = strUniqueIdSplit[j].Split(new String[] { "|||" }, StringSplitOptions.None)[1];
                    int noofchild = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[j]["NoOfChild"]);
                    dtPassengers.DefaultView.RowFilter = "PickUp='" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomTypeId"]) + "' and UserCartID='" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["UserCartID"]) + "' and RoomNo='" + Convert.ToInt32(j + 1) + "'";
                    DataTable dtnewpassanger1 = dtPassengers.DefaultView.ToTable();
                    if (j >= 1)
                    {
                        SB_RoomDetails.Append("},{");
                    }
                    for (int R = 0; R < dtnewpassanger1.DefaultView.Count; R++)
                    {
                        if (R >= 1)
                        {
                            SB_RoomDetails.Append("},{");
                        }
                        SB_RoomDetails.Append("\"UniqueId\":\"" + UniqueId + "\",");
                        SB_RoomDetails.Append("\"RoomNo\":\"" + RoomNo + "\",");
                        if (Convert.ToString(dtnewpassanger1.Rows[R]["IsLead"]) == "1")
                            SB_RoomDetails.Append("\"IsLead\":\"" + 1 + "\",");
                        else
                            SB_RoomDetails.Append("\"IsLead\":\"" + 0 + "\",");
                        PaxType = Convert.ToString(dtnewpassanger1.Rows[R]["PaxType"]);
                        if (PaxType.ToLower() == "adult")
                            SB_RoomDetails.Append("\"PaxType\":\"Adult\",");
                        else
                            SB_RoomDetails.Append("\"PaxType\":\"Child\",");
                        SB_RoomDetails.Append("\"Prefix\":\"" + Convert.ToString(dtnewpassanger1.Rows[R]["Prefix"]) + "\",");
                        SB_RoomDetails.Append("\"FirstName\":\"" + Convert.ToString(dtnewpassanger1.Rows[R]["FirstName"]) + "\",");
                        SB_RoomDetails.Append("\"LastName\":\"" + Convert.ToString(dtnewpassanger1.Rows[R]["LastName"]) + "\",");
                        SB_RoomDetails.Append("\"ChildAge\":\"" + Convert.ToString(dtnewpassanger1.Rows[R]["ChildAge"]) + "\"");
                    }
                }

                StringBuilder SBreq = new StringBuilder();
                SBreq.Append("{");
                SBreq.Append("\"Token\":\"" + Token + "\",");
                SBreq.Append("\"Request\":{");
                SBreq.Append("\"SearchKey\":\"" + SearchKey + "\",");
                SBreq.Append("\"BookRQ\":{");
                SBreq.Append("\"BookingToken\":\"" + BookingToken + "\",");
                SBreq.Append("\"TotalPrice\":" + TotalPrice + ",");
                SBreq.Append("\"InternalReference\":\"" + InternalReference + "\",");
                SBreq.Append("\"HotelRooms\":[{");
                SBreq.Append(SB_RoomDetails.ToString());
                SBreq.Append("}]");
                SBreq.Append("}");
                SBreq.Append("},");
                SBreq.Append("\"AdvancedOptions\":{");
                SBreq.Append("\"Currency\":\"" + WE_DMCCurrency + "\",");
                SBreq.Append("\"CustomerIpAddress\":\"" + IP + "\"");
                SBreq.Append("}");
                SBreq.Append("}");
                Request = SBreq.ToString();
                return Request;

                #endregion
            }
            catch (Exception ex)
            {
                _ =_exception.Error(ex);
                _commonMethod.ThirdPartyBookingHistory(Convert.ToString(UniqueNo), Convert.ToString(AgentUniqueNo), ex.ToString(), "-", "ErrorLog from Booking Request", Convert.ToString(dsCartDetails.Tables[1].DefaultView[0]["SupplierName"]), "Request Confirm j=" + 0 + "", "-", "WithinEarth");
                return "";
            }
        }
        public string Withinearth_DMCBookingRequest_newversion(DataRow[] Withinearth_DMCDetails, string SearchKey, string BToken, double TotalPrice, string InternalReference, string RoomNo, BookRequest_newversion bookRequest, DataTable dtPassengers, string UniqueNo, string AgentUniqueNo, string WE_DMCCurrency, string IP)
        {
            try
            {
                string[] strBookingTokenMain = BToken.Split(new String[] { "§§§" }, StringSplitOptions.None);
                BToken = strBookingTokenMain[0];
                TotalPrice = Convert.ToDouble(strBookingTokenMain[1]);
                #region Booking RQ
                string Token = Convert.ToString(Withinearth_DMCDetails[0]["UserID"]);
                string PaxType = "";
                string Request = "";
                string[] strBookingToken = BToken.Split(new String[] { "$$$" }, StringSplitOptions.None);
                string BookingToken = strBookingToken[0];
                string[] strUniqueId = strBookingToken[1].Split(new String[] { "$$$" }, StringSplitOptions.None);
                string[] strUniqueIdSplit = strUniqueId[0].Split(new String[] { "@@@" }, StringSplitOptions.None);
                StringBuilder SB_RoomDetails = new StringBuilder();

                for (int j = 0; j < bookRequest.userCart.UserCartDetails.Count; j++)
                {
                    string UniqueId = strUniqueIdSplit[j].Split(new String[] { "|||" }, StringSplitOptions.None)[0];
                    RoomNo = strUniqueIdSplit[j].Split(new String[] { "|||" }, StringSplitOptions.None)[1];
                    int noofchild = Convert.ToInt32(bookRequest.userCart.UserCartDetails[j].NoofChild);
                    dtPassengers.DefaultView.RowFilter = "PickUp='" + Convert.ToString(bookRequest.userCart.UserCartDetails[j].RoomTypeId) + "' and UserCartID='" + Convert.ToString(bookRequest.userCart.UserCartDetails[j].UserCartId) + "' and RoomNo='" + Convert.ToInt32(j + 1) + "'";
                    DataTable dtnewpassanger1 = dtPassengers.DefaultView.ToTable();
                    if (j >= 1)
                    {
                        SB_RoomDetails.Append("},{");
                    }
                    for (int R = 0; R < dtnewpassanger1.DefaultView.Count; R++)
                    {
                        if (R >= 1)
                        {
                            SB_RoomDetails.Append("},{");
                        }
                        SB_RoomDetails.Append("\"UniqueId\":\"" + UniqueId + "\",");
                        SB_RoomDetails.Append("\"RoomNo\":\"" + RoomNo + "\",");
                        if (Convert.ToString(dtnewpassanger1.Rows[R]["IsLead"]) == "1")
                            SB_RoomDetails.Append("\"IsLead\":\"" + 1 + "\",");
                        else
                            SB_RoomDetails.Append("\"IsLead\":\"" + 0 + "\",");
                        PaxType = Convert.ToString(dtnewpassanger1.Rows[R]["PaxType"]);
                        if (PaxType.ToLower() == "adult")
                            SB_RoomDetails.Append("\"PaxType\":\"Adult\",");
                        else
                            SB_RoomDetails.Append("\"PaxType\":\"Child\",");
                        SB_RoomDetails.Append("\"Prefix\":\"" + Convert.ToString(dtnewpassanger1.Rows[R]["Prefix"]) + "\",");
                        SB_RoomDetails.Append("\"FirstName\":\"" + Convert.ToString(dtnewpassanger1.Rows[R]["FirstName"]) + "\",");
                        SB_RoomDetails.Append("\"LastName\":\"" + Convert.ToString(dtnewpassanger1.Rows[R]["LastName"]) + "\",");
                        SB_RoomDetails.Append("\"ChildAge\":\"" + Convert.ToString(dtnewpassanger1.Rows[R]["ChildAge"]) + "\"");
                    }
                }

                StringBuilder SBreq = new StringBuilder();
                SBreq.Append("{");
                SBreq.Append("\"Token\":\"" + Token + "\",");
                SBreq.Append("\"Request\":{");
                SBreq.Append("\"SearchKey\":\"" + SearchKey + "\",");
                SBreq.Append("\"BookRQ\":{");
                SBreq.Append("\"BookingToken\":\"" + BookingToken + "\",");
                SBreq.Append("\"TotalPrice\":" + TotalPrice + ",");
                SBreq.Append("\"InternalReference\":\"" + InternalReference + "\",");
                SBreq.Append("\"HotelRooms\":[{");
                SBreq.Append(SB_RoomDetails.ToString());
                SBreq.Append("}]");
                SBreq.Append("}");
                SBreq.Append("},");
                SBreq.Append("\"AdvancedOptions\":{");
                SBreq.Append("\"Currency\":\"" + WE_DMCCurrency + "\",");
                SBreq.Append("\"CustomerIpAddress\":\"" + IP + "\"");
                SBreq.Append("}");
                SBreq.Append("}");
                Request = SBreq.ToString();
                return Request;

                #endregion
            }
            catch (Exception ex)
            {
                _ = _exception.Error(ex);
                _commonMethod.ThirdPartyBookingHistory(Convert.ToString(UniqueNo), Convert.ToString(AgentUniqueNo), ex.ToString(), "-", "ErrorLog from Booking Request", Convert.ToString(bookRequest.userCart.UserCartDetails[0].SupplierName), "Request Confirm j=" + 0 + "", "-", "WithinEarth");
                return "";
            }
        }
        public async Task<WE_BookingDetail> GetBookingDetailWE(string AGTRef, string UserID, string BookingId, string URL, string UniqueNo, string AgentUniqueNo, string Currency, string Request, string Search_Key,string IP,string Supplier)
        {
            WE_BookingDetail wE_BookingDetail = new WE_BookingDetail();
            string Error = "";
            try
            {
                URL = URL + TechnoInfo.WE_DMCBookingDetailURL;// ConfigurationManager.AppSettings["WE_DMCBookingDetailURL"];
                string Response = "";
                StringBuilder SBReq = new StringBuilder();
                SBReq.Append("{\"Token\": \"" + UserID + "\",");
                SBReq.Append("\"Request\": {");
                SBReq.Append("\"BookingDetailRQ\": {");
                SBReq.Append("\"BookingId\": \"" + BookingId + "\",");
                SBReq.Append("\"InternalReference\": \"" + AGTRef + "\",");
                SBReq.Append("\"ReferenceNo\": \"\"");
                SBReq.Append("}},\"AdvancedOptions\": {");
                SBReq.Append("\"Currency\": \"" + Currency + "\",");
                SBReq.Append("\"CustomerIpAddress\":\"" + IP + "\"");
                SBReq.Append("}}");
                Request = SBReq.ToString();
                _commonMethod.ThirdPartyBookingHistory(Convert.ToString(UniqueNo), Convert.ToString(AgentUniqueNo), Request, "-", "Before Block Request hit WE_DMCBookingDetail", "travab_dmc", "Request Confirm j=" + 0 + "", AGTRef, "WE");

                ResponseDto responseDto = await GetSupplierResponseWE_DMC(Request, URL, 200000, Supplier, null, false, UniqueNo, Search_Key);
                Response = responseDto.Response;
                Error = responseDto.Error;
                _commonMethod.ThirdPartyBookingHistory(Convert.ToString(UniqueNo), Convert.ToString(AgentUniqueNo), Request, Response + Error, "After Block Request hit WE_DMCBookingDetail", "travab_dmc", "Request Confirm j=" + 0 + "", AGTRef, "WE");

                wE_BookingDetail.Request = Request;
                wE_BookingDetail.Response = Response;
                return wE_BookingDetail;
            }
            catch (Exception ex)
            {
                _ =_exception.ErrorBooking(ex, Search_Key);
                return wE_BookingDetail;
            }

        }


        public string CheckHotelCancellationChargesRequest(DataRow[] Withinearth_DMCDetails, string BookingRef, string Currency,string IP)
        {
            try
            {
                string Token = Convert.ToString(Withinearth_DMCDetails[0]["UserID"]);

                string Request = "";
                StringBuilder SBreq = new StringBuilder();
                SBreq.Append("{");
                SBreq.Append("\"Token\": \"" + Token + "\",");
                SBreq.Append("\"Request\": {");
                SBreq.Append("\"CheckHotelCancellationChargesRQ\": {");
                SBreq.Append("\"BookingId\": 0,");
                SBreq.Append("\"InternalReference\": \"\",");
                SBreq.Append("\"ReferenceNo\": \"" + BookingRef + "\"");
                SBreq.Append("}},");
                SBreq.Append("\"AdvancedOptions\": {");
                SBreq.Append("\"Currency\": \"" + Currency + "\",");
                SBreq.Append("\"CustomerIpAddress\":\"" + IP + "\"");
                SBreq.Append("}}");
                Request = SBreq.ToString();

                return Request;
            }
            catch (Exception ex)
            {
                _ = _exception.ErrorNew(ex, _LogAPI.CancelBooking);
                return "";
            }
        }

        public string Withinearth_DMCCancelBookingRequest(DataRow[] Withinearth_DMCDetails, string BookingRef, string Reason, Int64 BookingId, string BookingDetailId, string CancelCode, string Currency,string IP)
        {
            try
            {
                string Token = Convert.ToString(Withinearth_DMCDetails[0]["UserID"]);

                string Request = "";
                StringBuilder SBreq = new StringBuilder();
                SBreq.Append("{");
                SBreq.Append("\"Token\":\"" + Token + "\",");
                SBreq.Append("\"Request\":{");
                SBreq.Append("\"CancelRQ\":{");
                SBreq.Append("\"BookingId\":\"" + BookingId + "\",");
                SBreq.Append("\"BookingDetailId\":\"" + BookingDetailId + "\",");
                SBreq.Append("\"CancelCode\":\"" + CancelCode + "\",");
                SBreq.Append("\"Reason\":\"" + Reason + "\"");
                SBreq.Append("}");
                SBreq.Append("},");
                SBreq.Append("\"AdvancedOptions\":{");
                SBreq.Append("\"Currency\":\"" + Currency + "\",");
                SBreq.Append("\"CustomerIpAddress\":\"" + IP + "\"");
                SBreq.Append("}");
                SBreq.Append("}");
                Request = SBreq.ToString();
                return Request;
            }
            catch (Exception ex)
            {
                _ = _exception.ErrorNew(ex, _LogAPI.CancelBooking);
                return "";
            }
        }

        public string Withinearth_DMCBookingDetailRequest(DataRow[] Withinearth_DMCDetails, string BookingId, string InternalReference, string Currency = "USD",string IP="")
        {
            try
            {
                string Token = Convert.ToString(Withinearth_DMCDetails[0]["UserID"]);

                string Request = "";
                StringBuilder SBreq = new StringBuilder();
                SBreq.Append("{");
                SBreq.Append("\"Token\":\"" + Token + "\",");
                SBreq.Append("\"Request\":{");
                SBreq.Append("\"BookingDetailRQ\":{");
                SBreq.Append("\"BookingId\":\"" + BookingId + "\",");
                SBreq.Append("\"InternalReference\":\"" + InternalReference + "\",");
                SBreq.Append("\"ReferenceNo\":\"\"");
                SBreq.Append("}");
                SBreq.Append("},");
                SBreq.Append("\"AdvancedOptions\":{");
                SBreq.Append("\"Currency\":\"" + Currency + "\",");
                SBreq.Append("\"CustomerIpAddress\":\"" + IP + "\"");
                SBreq.Append("}");
                SBreq.Append("}");
                Request = SBreq.ToString();
                return Request;

            }
            catch (Exception ex)
            {
                _ = _exception.ErrorNew(ex, _LogAPI.CancelBooking);
                return "";
            }
        }
    }
}
