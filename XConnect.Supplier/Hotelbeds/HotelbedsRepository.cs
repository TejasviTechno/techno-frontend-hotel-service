using System;
using System.Collections.Concurrent;
using System.Data;
using System.Text;
using XConnect.CommonUtilities.Interface;
using XConnect.CommonUtilities;
using XConnect.ErrorLogHandler.Interface;
using Xconnect.Hotelbeds.Interface;
using XConnect.Markup.Interface;
using XConnect.Models.Hotel;
using XConnect.Models;
using Microsoft.Extensions.Options;
using Options = XConnect.Models.Options;
using MongoDB.Bson.IO;
using System.Text.Json;
using System.Linq;
using Azure;
using static Azure.Core.HttpHeader;
using System.Net;
using System.Security.Cryptography;
using static XConnect.Models.ErrorLog;

namespace Xconnect.Hotelbeds

{
    public class HotelbedsRepository : IHotelbeds
    {
        private readonly ICommonMethod _commonMethod;
        private readonly IException _exception;
        private readonly IMarkup _markup;
        private readonly ISupplierRes _supplierRes;
        private readonly KeySettings _keySettings;
        private readonly SupplierConfiguration _supplierConfig;
        public HotelbedsRepository(ICommonMethod commonMethod, IMarkup markup, ISupplierRes supplierRes, IException exception, IOptions<KeySettings> keySettings, IOptions<SupplierConfiguration> supplierConfig)
        {
            _commonMethod = commonMethod;
            _markup = markup;
            _supplierRes = supplierRes;
            _exception = exception;
            _keySettings = keySettings.Value;
            _supplierConfig = supplierConfig.Value;
        }
        public void GetHotelbedsRequestOnly_async(DataRow[] commonCode, List<StarRating> listRating, DataTable dtPreferredDataHotel, HotelSearchParameter SearchPara, List<SupplierSearchConfig> HBV1SearchConfig, List<SupplierDetails> HBV1Details, bool IsHotelWiseSearch, ref ConcurrentBag<HotelX> AllTask, string xmlNationality, int Hit, string supplierName, DataTable dtROE, TokenData tokenData)
        {

            try
            {
                #region "HotelBedsV1"
                DataRow[] drHotelbedsV1 = dtPreferredDataHotel.Select("ServiceName='HotelBedsV1'");
                string Ids = string.Empty;

                //string Currency = (Convert.ToString(_configuration["AppSettings:HotelBedsV1_newCurrency"]));
                //double CurrencyFact = 1;
                ////if (Currency.ToLower() != Convert.ToString(ConfigurationManager.AppSettings["SiteCurrency"]).ToLower())
                //if (Currency.ToLower() != (Convert.ToString(_configuration["AppSettings:HotelBedsV1_newCurrency"])).ToLower())
                //{
                //    CurrencyFact = _commonMethod.GetROEByCurrency(Currency, dtROE);
                //}
                double CurrencyFact = 1;
                string Hotelbedsv1_currency = Convert.ToString(HBV1Details[0].CurrencyShortName);
                string Sitecurrency = tokenData.SiteCurrency;

                //if (Currencydb.ToLower() != "usd") // change systembase currency 
                if (Hotelbedsv1_currency.ToLower() != Sitecurrency.ToLower()) // change systembase currency 
                {
                    CurrencyFact = _commonMethod.GetROEByCurrency(Hotelbedsv1_currency, dtROE);
                }
                if (drHotelbedsV1.Count() > 0)
                {
                    var paramValues1 = String.Join(",", drHotelbedsV1.Select(x => String.Join(",", Convert.ToString(x.ItemArray[0]))));
                    Ids = Convert.ToString(paramValues1);
                }
                if (SearchPara.IsSearchFor.ToLower().IndexOf("preferred") >= 0 && drHotelbedsV1.Count() == 0)
                {
                    HBV1SearchConfig = null;
                }
                //DataTable dtHotelBedsV1 = new DataTable();
                //if (Ids == string.Empty)
                //    dtHotelBedsV1 = Common.GetAgodaHotelCodeByCityID(SearchPara.CityID, false, "HotelBedsV1");
                if (HBV1SearchConfig != null && HBV1Details.Count > 0)
                {
                    for (int k = 0; k < HBV1SearchConfig.Count; k++)
                    {
                        //if (Convert.ToInt32(HBV1SearchConfig[k]["Hit"]) != Hit)
                        //    continue;
                        bool DebugMode = (Convert.ToInt32(HBV1SearchConfig[k].DebugMode) == 0 ? false : true);
                        if (Convert.ToInt32(HBV1SearchConfig[k].CityID) == 0 || Convert.ToInt32(HBV1SearchConfig[k].CityID) == SearchPara.CityID)//All City means 0
                        {


                            if (Convert.ToInt32(HBV1SearchConfig[k].CityID) == 0 || Convert.ToInt32(HBV1SearchConfig[k].CityID) == SearchPara.CityID)//All City means 0
                            {

                                // List<HotelCodeByCity> DtHotelDAta = FilterFinalList(commonCode, listRating);
                                if (commonCode.Length > 0)
                                {
                                    int Noofrecords = Convert.ToInt32(HBV1SearchConfig[k].Noofrecords);
                                    DataTable[] splittedtables = commonCode.AsEnumerable()
                                                         .Select((row, index) => new { row, index })
                                                         .GroupBy(x => x.index / Noofrecords)  // integer division, the fractional part is truncated
                                                         .Select(g => g.Select(x => x.row).CopyToDataTable())
                                                         .ToArray();
                                    for (int a = 0; a < splittedtables.Length; a++)
                                    {

                                        var paramValues1 = String.Join(",", splittedtables[a].Select().Select(x => String.Join(",", Convert.ToString(x.ItemArray[0]))));
                                        string IdsForSearch = Convert.ToString(paramValues1);
                                        if (!string.IsNullOrEmpty(IdsForSearch))
                                        {
                                            HotelX xhotel = new HotelX();
                                            xhotel.URL = Convert.ToString(HBV1Details[0].WebSiteURL);///ConfigurationManager.AppSettings["HBV1_New_URL"];
                                            xhotel.UserName = Convert.ToString(HBV1Details[0].UserName);// ConfigurationManager.AppSettings["HBV1_New_UserName"]; 
                                            xhotel.PassWord = Convert.ToString(HBV1Details[0].Password); //ConfigurationManager.AppSettings["HBV1_New_PassWord"]; 
                                            xhotel.SearchTimeOut = Convert.ToInt32(HBV1SearchConfig[k].HotelSearchTimeOut);
                                            //if (Hit > 0)
                                                xhotel.ProcessTimeout = Convert.ToInt32(HBV1SearchConfig[k].HotelSearchTimeOut2);

                                            xhotel.DebugeMode = DebugMode;
                                            xhotel.MethoodType = "POST";
                                            // xhotel.Supplier = "HotelBedsV1";
                                            xhotel.Supplier = supplierName;
                                            xhotel.CurrencyCode = "USD";
                                            xhotel.CurrencyFactor = CurrencyFact;
                                            xhotel.XmlNationality = xmlNationality;
                                            xhotel.XmlCity = "";
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
                                                objrq.Request = (XMLHotelBedsV1RequestOnly_async(serchRooms, "1", "1", xhotel.URL, xhotel.UserName, xhotel.PassWord, xhotel.SearchTimeOut, SearchPara, IdsForSearch, DebugMode, xmlNationality));
                                                objrq.Response = ""; RQRS.Add(objrq);
                                            }
                                            xhotel.RQRS = RQRS;
                                            xhotel.ReturnRooms = 5; // HBV1Details[0].ReturnRooms;
                                            xhotel.TechName = "Hotelbeds";//HBV1Details[0].TechName;
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
            catch (Exception ex)
            {
                _exception.Error(ex);

            }
        }

        public string XMLHotelBedsV1RequestOnly_async(List<RoomDetails> room, string MinStar, string MaxStar, string WSurl, string Username, string Password, Int32 SearchTimeOut, HotelSearchParameter SearchParam, string HotelIds, bool DebugMode, string HBNationality)
        {
            try
            {
                string checkin = Convert.ToString(SearchParam.CheckInDate);
                string checkout = Convert.ToString(SearchParam.CheckOutDate);
                int NoOfNight = Convert.ToInt32(SearchParam.NoofNights);
                checkin = checkin.Substring(6, 4) + "-" + checkin.Substring(3, 2) + "-" + checkin.Substring(0, 2);
                checkout = checkout.Substring(6, 4) + "-" + checkout.Substring(3, 2) + "-" + checkout.Substring(0, 2);
                string HotelList = HotelsBedV1SearchRequest(room, SearchParam, checkin, checkout, MinStar, MaxStar, HotelIds, HBNationality);
                return HotelList;
            }
            catch (IOException e)
            {
                _exception.Error(e);
            }
            return "";
        }

        public void GetHotelbedsRequestOnly_async_new(DataRow[] commonCode, List<StarRating> listRating, DataTable dtPreferredDataHotel, HotelSearchParameter SearchPara, List<SupplierSearchConfig> lstHotelbedsSearchConfig, List<SupplierDetails> lstHotelbedsDetails, bool IsHotelWiseSearch, ref ConcurrentBag<HotelX> AllTask, int Hit, DataTable dtROE, TokenData tokenData, DataSet dtXMLCity)
        {
            try
            {
                
                List<StarRating> lstRating = new List<StarRating>();
                lstRating = _commonMethod.GetStarRatingStringList_xtg(SearchPara);

                List<HotelCodeByCity> DtHotelDAta = HtmlExtensions.ToList<HotelCodeByCity>(commonCode.CopyToDataTable());
                int ratingcount = lstRating.Count;
                int hotelcount = DtHotelDAta.Count;

                foreach (SupplierDetails HotelbedsChild in lstHotelbedsDetails)
                {
                    try
                    {
                        int SupplierID = HotelbedsChild.SupplierID;
                        string Supplier = HotelbedsChild.SupplierName;

                        if (lstHotelbedsSearchConfig.Count > 0)
                        {
                            List<SupplierSearchConfig> HotelbedsSearchConfig = lstHotelbedsSearchConfig
                                    .Where(x => x != null && x.SupplierID == SupplierID)
                                    .ToList();

                            if (HotelbedsSearchConfig != null && HotelbedsSearchConfig.Count > 0)
                            {
                                if ((SearchPara.IsSearchFor.ToLower().IndexOf(Supplier.ToLower()) >= 0 || SearchPara.IsSearchFor.ToLower().IndexOf("multi") >= 0))
                                {
                                    DataForSearch dataForSearch = _commonMethod.GetXmlDataForSearchTechwise(SearchPara.Nationality, dtXMLCity, Supplier, "hotelbeds");

                                    string HBNationality = "";
                                    if (dataForSearch.XmlNationality.ToLower() == "ru")//@Due to Ticket#35018
                                    {
                                        HBNationality = "ES";//span
                                    }
                                    else
                                    {
                                        HBNationality = dataForSearch.XmlNationality;
                                    }
                                    if (!string.IsNullOrEmpty(HBNationality))
                                    {
                                        DataRow[] drHotelbedsV1 = dtPreferredDataHotel.Select("ServiceName='" + Supplier + "'");
                                        string Ids = string.Empty;

                                        List<HotelCodeByCity> filter = new List<HotelCodeByCity>();
                                        filter = _commonMethod.FilterWithOutCity_Code(ratingcount, hotelcount, DtHotelDAta, lstRating, HotelbedsChild.HotelSupplier);


                                        double CurrencyFact = 1;
                                        string Hotelbedsv1_currency = Convert.ToString(HotelbedsChild.CurrencyShortName);
                                        string Sitecurrency = tokenData.SiteCurrency;

                                        //if (Currencydb.ToLower() != "usd") // change systembase currency 
                                        if (Hotelbedsv1_currency.ToLower() != Sitecurrency.ToLower()) // change systembase currency 
                                        {
                                            CurrencyFact = _commonMethod.GetROEByCurrency(Hotelbedsv1_currency, dtROE);
                                        }
                                        if (drHotelbedsV1.Count() > 0)
                                        {
                                            var paramValues1 = String.Join(",", drHotelbedsV1.Select(x => String.Join(",", Convert.ToString(x.ItemArray[0]))));
                                            Ids = Convert.ToString(paramValues1);
                                        }
                                        if (SearchPara.IsSearchFor.ToLower().IndexOf("preferred") >= 0 && drHotelbedsV1.Count() == 0)
                                        {
                                            HotelbedsSearchConfig = null;
                                        }

                                        for (int k = 0; k < HotelbedsSearchConfig.Count; k++)
                                        {
                                            //if (Convert.ToInt32(HBV1SearchConfig[k]["Hit"]) != Hit)
                                            //    continue;
                                            bool DebugMode = (Convert.ToInt32(HotelbedsSearchConfig[k].DebugMode) == 0 ? false : true);

                                            if (Convert.ToInt32(HotelbedsSearchConfig[k].CityID) == 0 || Convert.ToInt32(HotelbedsSearchConfig[k].CityID) == SearchPara.CityID)//All City means 0
                                            {

                                                // List<HotelCodeByCity> DtHotelDAta = FilterFinalList(commonCode, listRating);
                                                if (filter.Count > 0)
                                                {
                                                    int NoOfRecord = Convert.ToInt32(HotelbedsSearchConfig[k].Noofrecords);
                                                    double count = Convert.ToDouble(filter.Count);
                                                    int NoOfDiv = Convert.ToInt32(Math.Ceiling(count / Convert.ToDouble(NoOfRecord)));
                                                    List<List<XMLID_Search>> lstxml = _commonMethod.GetXMLIDString(NoOfDiv, NoOfRecord, count, filter);
                                                    for (int a = 0; a < NoOfDiv; a++)
                                                    {

                                                        //var paramValues1 = String.Join(",", splittedtables[a].Select().Select(x => String.Join(",", Convert.ToString(x.ItemArray[0]))));
                                                        //string IdsForSearch = Convert.ToString(paramValues1);
                                                        string IdsForSearch = Convert.ToString(_commonMethod.GetHotelCodeByCOmma(lstxml[a]));

                                                        if (!string.IsNullOrEmpty(IdsForSearch))
                                                        {
                                                            HotelX xhotel = new HotelX();
                                                            //xhotel.URL = Convert.ToString(HotelbedsChild[x].WebSiteURL);///ConfigurationManager.AppSettings["HBV1_New_URL"];

                                                            if (_keySettings.SupplierProxy == "0")
                                                                xhotel.URL = Convert.ToString(HotelbedsChild.WebSiteURL);///ConfigurationManager.AppSettings["HBV1_New_URL"];
                                                            else
                                                                xhotel.URL = Convert.ToString(HotelbedsChild.WebSiteURLforAPI);///ConfigurationManager.AppSettings["HBV1_New_URL"];


                                                            xhotel.UserName = Convert.ToString(HotelbedsChild.UserName);// ConfigurationManager.AppSettings["HBV1_New_UserName"]; 
                                                            xhotel.PassWord = Convert.ToString(HotelbedsChild.Password); //ConfigurationManager.AppSettings["HBV1_New_PassWord"]; 
                                                            xhotel.SearchTimeOut = Convert.ToInt32(HotelbedsSearchConfig[k].HotelSearchTimeOut);
                                                            //if (Hit > 0)
                                                                xhotel.ProcessTimeout = Convert.ToInt32(HotelbedsSearchConfig[k].HotelSearchTimeOut2);

                                                            xhotel.DebugeMode = DebugMode;
                                                            xhotel.MethoodType = "POST";
                                                            // xhotel.Supplier = "HotelBedsV1";
                                                            xhotel.Supplier = Supplier;
                                                            xhotel.CurrencyCode = Hotelbedsv1_currency;
                                                            xhotel.CurrencyFactor = CurrencyFact;
                                                            xhotel.XmlNationality = HBNationality;
                                                            xhotel.XmlCity = "";
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
                                                                objrq.Request = (XMLHotelBedsV1RequestOnly_async(serchRooms, "1", "1", xhotel.URL, xhotel.UserName, xhotel.PassWord, xhotel.SearchTimeOut, SearchPara, IdsForSearch, DebugMode, HBNationality));
                                                                objrq.Response = ""; RQRS.Add(objrq);
                                                            }
                                                            xhotel.RQRS = RQRS;
                                                            xhotel.ReturnRooms = HotelbedsChild.ReturnRooms;
                                                            xhotel.TechName = HotelbedsChild.TechName;
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
                    catch (Exception ex)
                    {
                        _exception.Error(ex);
                    }
                }
            }
            catch (Exception ex)
            {
                _exception.Error(ex);

            }
        }
        public string HotelsBedV1SearchRequest(List<RoomDetails> room, HotelSearchParameter SearchParam, string checkin, string checkout, string MinStar, string MaxStar, string HotelIds, string HBNationality)
        {
            try
            {

                if (HBNationality.ToLower() == "xk")
                {
                    HBNationality = "KV";
                }
                else if (HBNationality.ToLower() == "sx")
                {
                    HBNationality = "SF";
                }

                StringBuilder roomstring = new StringBuilder();
                for (int i = 0; i < SearchParam.Rooms.Count; i++)
                {
                    if (i >= 1)
                    {
                        roomstring.Append("},{");
                    }
                    int Adult = SearchParam.Rooms[i].NoofAdults;
                    int Child = SearchParam.Rooms[i].NoofChild;
                    roomstring.Append("\"rooms\":" + (i + 1) + ",");
                    roomstring.Append("\"adults\":" + SearchParam.Rooms[i].NoofAdults + ",");
                    roomstring.Append("\"children\":" + SearchParam.Rooms[i].NoofChild);

                    if (Child >= 1)
                    {
                        switch (Child)
                        {
                            case 1:
                                roomstring.Append(",\"paxes\":[{");
                                roomstring.Append("\"type\":" + "\"CH\"");
                                roomstring.Append(",\"age\":" + Convert.ToString(SearchParam.Rooms[i].Child1Age) + "");
                                roomstring.Append("}]");
                                break;
                            case 2:
                                roomstring.Append(",\"paxes\":[{");
                                roomstring.Append("\"type\":" + "\"CH\"");
                                roomstring.Append(",\"age\":" + Convert.ToString(SearchParam.Rooms[i].Child1Age) + "");
                                roomstring.Append("},{");
                                roomstring.Append("\"type\":" + "\"CH\"");
                                roomstring.Append(",\"age\":" + Convert.ToString(SearchParam.Rooms[i].Child2Age) + "");
                                roomstring.Append("}]");
                                break;
                        }
                    }
                }

                StringBuilder HBv1 = new StringBuilder();
                HBv1.Append("{");
                HBv1.Append("\"stay\":");
                HBv1.Append("{");
                HBv1.Append("\"checkIn\":\"" + checkin + "\",");
                HBv1.Append("\"checkOut\":\"" + checkout + "\"");
                HBv1.Append("},");
                HBv1.Append("\"occupancies\":");
                HBv1.Append("[{");
                HBv1.Append(roomstring.ToString());
                HBv1.Append("}],");
                HBv1.Append("\"hotels\": {");
                //HBv1.Append("\"hotel\": [1349, 1027, 11, 1363]");
                HBv1.Append("\"hotel\":[" + HotelIds + "]");
                HBv1.Append("},");
                //HBv1.Append("\"filter\": {");
                //HBv1.Append("\"minRate\": 10,");
                //HBv1.Append("\"maxRate\": 10000");
                //// HBv1.Append("\"minCategory\":" + MinStar + ",");
                //// HBv1.Append("\"maxCategory\":" + MaxStar + "");
                //HBv1.Append("},");
                HBv1.Append("\"dailyRate\":\"true\",");
                HBv1.Append("\"sourceMarket\": \"" + HBNationality + "\"");
                HBv1.Append("}");
                string request = HBv1.ToString();
                return request;
            }
            catch (Exception ex)
            {
                return "";
            }
        }


        public async Task<List<Options>> HotelbedsV1_HotelSearchWithRoomsv1(bool SearchByOneID, HotelX Item, CommonSearch commonSearch, List<RQRS> rQRS, string WSurl, string Username, string Password, Int32 SearchTimeOut, HotelSearchParameter SearchParam, bool DebugMode, string Currency, double CurrencyFactor, string ProccessCutOffTime, Int32 processTimeout, string ResProccessTimerLogActive, string ResProccessTimeValueforSaveLog, bool withcancellation, string StoreSupplierResErrorLog)
        {
            System.Diagnostics.Stopwatch timer_room_HotelbedsV1 = new System.Diagnostics.Stopwatch();
            timer_room_HotelbedsV1.Start();
            List<Options> options = new List<Options>();
            string ResponseData = rQRS[0].Response;
            DataTable DtHotel = _commonMethod.GenerateDataTableNewV2();
            try
            {
                //string APINAME = Convert.ToString(HttpContext.Current.Request.Url).ToLower();//.Contains("availability")
                if (ResponseData == "")
                {
                    Item.ResponseType = "NoCount";
                    return options;
                }
                Boolean blnIsShowSupplier = false;
                string ConvertXml = "";

                //DataTable policy = new DataTable();
                //if (dsHotelBeds.Tables["cancellationPolicies"] != null)
                //{
                //    if (!dsHotelBeds.Tables["cancellationPolicies"].Columns.Contains("Date"))
                //        dsHotelBeds.Tables["cancellationPolicies"].Columns.Add("Date", typeof(DateTime));

                //    for (int dt = 0; dt < dsHotelBeds.Tables["cancellationPolicies"].Rows.Count; dt++)
                //    {
                //        dsHotelBeds.Tables["cancellationPolicies"].Rows[dt]["Date"] = Convert.ToDateTime(dsHotelBeds.Tables["cancellationPolicies"].Rows[dt]["from"]).Date;
                //    }
                //    dsHotelBeds.Tables["cancellationPolicies"].DefaultView.Sort = "Date Asc";
                //    policy = dsHotelBeds.Tables["cancellationPolicies"].DefaultView.ToTable();
                //}

                int CutOffDays = CacheMaster.CutOffDays;
                // CutOffDays = Common.GetCutOffDays(commonSearch.dtSuplierMarkup, "HotelBedsV1");
                CutOffDays = _commonMethod.GetCutOffDays(commonSearch.dtSuplierMarkup, Item.Supplier);
                //for (int h = 1; h < counthotel; h++)
                {

                    //Check Time If High then return NoCount

                    //--end
                    // double price = ;
                    // string starRating = Convert.ToString(dsHotelBeds.Tables["hotels"].Rows[h]["categoryCode"]);
                    //string Stars = Common.HotelBedsStarRating(starRating);
                    // double finalPrice = price;


                    //New for CombinePrice
                    //
                    double FinalPrice = 1;
                    #region "When not Need Room"  
                    // add hotel code from price only and room code blank here
                    if (SearchParam.Filters.IsShowRooms == 0)
                    {
                        HBV1Class.Rootobject HBResponse = null;
                        try
                        {
                            HBResponse = JsonSerializer.Deserialize<HBV1Class.Rootobject>(ResponseData);

                        }
                        catch (Exception ex)
                        {
                            return options;
                        }
                        if (ConvertXml == "")
                        {
                            Item.ResponseType = "NoCount";
                            return options;
                        }
                        DataSet dsHotelBeds = new DataSet();
                        dsHotelBeds = _commonMethod.GetDataSetFromXMLString(ConvertXml);
                        int counthotel = HBResponse.hotels.hotels.Length;
                        for (int h = 0; h < counthotel; h++)
                        {
                            Item.ResponseType = "Success";
                            #region "Hotel Code Block "
                            if (commonSearch.tokenData.AgentId != 1899)
                            {
                                FinalPrice = 0;
                                for (int c = 0; c < SearchParam.Rooms.Count(); c++)
                                {
                                    int NoOfAdults = SearchParam.Rooms[c].NoofAdults;
                                    bool chk = false;
                                    double Total = 0;

                                    for (int a = 0; a < HBResponse.hotels.hotels[h].rooms.Count(); a++)
                                    {
                                        if (chk == false)
                                        {

                                            var p = HBResponse.hotels.hotels[h].rooms[a].rates
                                                        .Where(r => r.adults == NoOfAdults) // Filter by NoOfAdults
                                                        .Select(r => r.net) // Select only the net price
                                                        .Min();

                                            double MinPrice = 0;
                                            if (p != "")
                                            {
                                                double Price = Convert.ToDouble(p);
                                                chk = true;
                                                if (MinPrice == 0)
                                                    MinPrice = Price;
                                                else
                                                {
                                                    if (Price < MinPrice)
                                                        MinPrice = Price;
                                                }

                                            }

                                            if (MinPrice != 0)
                                                Total = Total + MinPrice;
                                        }
                                    }
                                    FinalPrice = FinalPrice + Total;
                                }
                            }
                            //End
                            DataRow drXmlHotel = DtHotel.NewRow();
                            drXmlHotel["IsRecomondedHotel"] = false;
                            drXmlHotel["XMLHotelCode"] = Convert.ToString(HBResponse.hotels.hotels[h].code);
                            drXmlHotel["HotelFrontImage"] = "";
                            drXmlHotel["HotelName"] = "";
                            drXmlHotel["Description"] = "";
                            drXmlHotel["EssentialInformation"] = "";
                            drXmlHotel["OfficeID"] = 0;
                            drXmlHotel["ContractID"] = 0;
                            drXmlHotel["IsShowSupplier"] = blnIsShowSupplier;
                            drXmlHotel["OwnSystemHotelId"] = 0;
                            drXmlHotel["Address"] = "";
                            drXmlHotel["Favorite"] = 0;
                            drXmlHotel["Rating"] = 0;
                            drXmlHotel["XmlHotelCity"] = Convert.ToString(HBResponse.hotels.hotels[h].destinationCode);
                            drXmlHotel["XMLCountry"] = "";
                            drXmlHotel["XMLCity"] = Convert.ToString(HBResponse.hotels.hotels[h].destinationCode);
                            // drXmlHotel["SupplierName"] = "HotelBedsV1";
                            drXmlHotel["SupplierName"] = Item.Supplier;
                            drXmlHotel["BookingStatus"] = "Available";
                            drXmlHotel["IsOffer"] = "";
                            drXmlHotel["FinalSellingPrice"] = FinalPrice;
                            drXmlHotel["SellingPrice"] = FinalPrice;
                            drXmlHotel["UniqueNo"] = Convert.ToString(HBResponse.hotels.hotels[h].code);
                            drXmlHotel["AvailToken"] = "";
                            drXmlHotel["SupplierCurrency"] = Currency;

                            #endregion


                            if (FinalPrice > 0)
                            {
                                #region "Room code Block"
                                bool willAddOrNot = true;
                                Int64 RaynaID = 0;
                                Int64 GiataId = 0;
                                int Rating = 0;
                                Int64 OwnsystemHotelId = 0;
                                double MinPrice = FinalPrice;
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

                    }
                    #endregion
                    #region "When Need Rooms"


                    if (SearchParam.Filters.IsShowRooms == 1)
                    {
                        //HBV1Class.Rootobject HBResponse = JsonSerializer.Deserialize<HBV1Class.Rootobject>(ResponseData);
                        HBV1Class.Rootobject HBResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<HBV1Class.Rootobject>(ResponseData);

                        for (int h = 0; h < HBResponse.hotels.total; h++)
                        {
                            Item.ResponseType = "Success";
                            if (ProccessCutOffTime == "1" && timer_room_HotelbedsV1.ElapsedMilliseconds > processTimeout)
                            {
                                timer_room_HotelbedsV1.Stop();
                                break;
                            }
                            #region Hotels
                            DataRow drXmlHotel = DtHotel.NewRow();
                            drXmlHotel["IsRecomondedHotel"] = false;
                            drXmlHotel["XMLHotelCode"] = HBResponse.hotels.hotels[h].code;
                            drXmlHotel["OfficeID"] = 0;
                            drXmlHotel["ContractID"] = 0;
                            drXmlHotel["IsShowSupplier"] = blnIsShowSupplier;
                            drXmlHotel["OwnSystemHotelId"] = 0;
                            drXmlHotel["Address"] = "";
                            drXmlHotel["Favorite"] = 0;
                            drXmlHotel["Rating"] = 0;
                            drXmlHotel["XmlHotelCity"] = HBResponse.hotels.hotels[h].destinationCode;
                            drXmlHotel["XMLCountry"] = "";
                            drXmlHotel["XMLCity"] = HBResponse.hotels.hotels[h].destinationCode;
                            drXmlHotel["SupplierName"] = Item.Supplier;
                            drXmlHotel["BookingStatus"] = "Available";
                            drXmlHotel["IsOffer"] = "";
                            drXmlHotel["FinalSellingPrice"] = FinalPrice;
                            drXmlHotel["SellingPrice"] = FinalPrice;
                            drXmlHotel["UniqueNo"] = HBResponse.hotels.hotels[h].code;
                            drXmlHotel["AvailToken"] = "";
                            drXmlHotel["SupplierCurrency"] = Currency;
                            #region "Hotel Block here"
                            #endregion
                            #region "GetRooms for this hotel"
                            bool willAddOrNot = true;
                            Int64 RaynaID = 0;
                            Int64 GiataId = 0;
                            int RatingHotel = 0;
                            Int64 OwnsystemHotelId = 0;
                            string Provider = Item.Supplier;
                            double MinPrice = FinalPrice;
                            _markup.GetHotelDataCommon(ref MinPrice, ref willAddOrNot, ref OwnsystemHotelId, ref RaynaID, ref GiataId, ref RatingHotel, ref drXmlHotel, commonSearch.AgentId, commonSearch.TariffID, commonSearch.dsAgentDetails, commonSearch.supplierGiataMaster, SearchParam, commonSearch.dtCountryCityMarkup, commonSearch.B2CMarkup, commonSearch.IsParentId, commonSearch.SubAgentMarkup, commonSearch.SearchByHotel, commonSearch.HotelIDS);
                            if (willAddOrNot)
                            {
                                DataTable dtHotelRoom = _commonMethod.GenerateRoomtypeDataTableNewV2();
                                DataTable dtCancellation = _commonMethod.GenerateCancellationDataTableNewV2();
                                DataTable DtRoomRateBreackUp = _commonMethod.GenerateoomRateBreackUpDataTableNewV2();

                                #region "Room Logic"
                                if (SearchParam.Filters.IsShowRooms == 1)
                                {
                                    double AgentPercentage = 0.0;
                                    double AgentMarkupAmount = 0.0;
                                    Boolean isPerce = true;

                                    double AgentCompanyPercentage = Convert.ToDouble(SearchParam.AgentCompanyPercentage);
                                    bool IsB2C = Convert.ToBoolean(Convert.ToByte(SearchParam.IsB2BOrB2C));

                                    _markup.getAgetMarkupSetting(Provider, GiataId.ToString(), commonSearch.dtSuplierMarkup, commonSearch.dtMarkupDetails, ref AgentPercentage, ref AgentMarkupAmount, ref blnIsShowSupplier, ref isPerce, commonSearch.AgentId, RatingHotel, SearchParam.CityID, SearchParam.CountryID.ToString(), SearchParam.CheckInDate);

                                    #region Supplier Room Code block
                                    for (int i = 0; i < HBResponse.hotels.hotels[h].rooms.Length; i++)
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
                                        double VatonfirstnightPrice = 0;

                                        for (int j = 0; j < HBResponse.hotels.hotels[h].rooms[i].rates.Length; j++)
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
                                            double itemprice = Convert.ToDouble(HBResponse.hotels.hotels[h].rooms[i].rates[j].net);
                                            string Essen = "", RateCommentID = "";
                                            if (HBResponse.hotels.hotels[h].rooms[i].rates[j].rateCommentsId != null)
                                                RateCommentID = HBResponse.hotels.hotels[h].rooms[i].rates[j].rateCommentsId;
                                            DataRow drHotelBedsV1Room = dtHotelRoom.NewRow();
                                            drHotelBedsV1Room["OwnsystemHotelId"] = OwnsystemHotelId;
                                            //drHotelBedsV1Room["RoomTypeCode"] = HBResponse.hotels.hotels[h].rooms[i].rates[j].boardCode;
                                            //drHotelBedsV1Room["MealName"] = HBResponse.hotels.hotels[h].rooms[i].rates[j].boardCode;
                                            //drHotelBedsV1Room["MealId"] = HBResponse.hotels.hotels[h].rooms[i].rates[j].boardCode;
                                            drHotelBedsV1Room["RoomTypeCode"] = HBResponse.hotels.hotels[h].rooms[i].rates[j].boardName; //added on 20-JUN-2024 as per mustafa sir
                                            drHotelBedsV1Room["MealName"] = HBResponse.hotels.hotels[h].rooms[i].rates[j].boardName;
                                            drHotelBedsV1Room["MealId"] = HBResponse.hotels.hotels[h].rooms[i].rates[j].boardName;
                                            drHotelBedsV1Room["BookingStatus"] = "Available";
                                            drHotelBedsV1Room["UniqueNo"] = HBResponse.hotels.hotels[h].code;
                                            drHotelBedsV1Room["ContractID"] = 0;
                                            int RoomNo = 1;
                                            drHotelBedsV1Room["RoomNo"] = RoomNo;
                                            drHotelBedsV1Room["BoardTypeID"] = "";
                                            drHotelBedsV1Room["AdultsOccupancy"] = HBResponse.hotels.hotels[h].rooms[i].rates[j].adults;
                                            drHotelBedsV1Room["ChildOccupancy"] = HBResponse.hotels.hotels[h].rooms[i].rates[j].children;
                                            drHotelBedsV1Room["RoomSequenceNo"] = "";
                                            if (HBResponse.hotels.hotels[h].rooms[i].rates[j].rateType != "BOOKABLE")
                                            {
                                                drHotelBedsV1Room["EssentialInformation"] = "N/A";
                                            }
                                            else
                                            {
                                                if (HBResponse.hotels.hotels[h].rooms[i].rates[j].rateComments != null)
                                                {
                                                    Essen = HBResponse.hotels.hotels[h].rooms[i].rates[j].rateComments;
                                                    drHotelBedsV1Room["EssentialInformation"] = Essen;
                                                }
                                                else
                                                {
                                                    drHotelBedsV1Room["EssentialInformation"] = " ";
                                                }
                                            }
                                            if (HBResponse.hotels.hotels[h].rooms[i].rates[j].promotions != null)
                                            {
                                                string Promotion = "";
                                                string PromotionRemark = "";
                                                if (HBResponse.hotels.hotels[h].rooms[i].rates[j].promotions.Length > 0)
                                                {
                                                    Promotion = HBResponse.hotels.hotels[h].rooms[i].rates[j].promotions[0].name;
                                                    PromotionRemark = HBResponse.hotels.hotels[h].rooms[i].rates[j].promotions[0].remark;
                                                    drHotelBedsV1Room["EssentialInformation"] = Essen + "<br><br> <b>Promotions : </b>" + Promotion + "<br>" + PromotionRemark + "<br>";
                                                }
                                            }
                                            drHotelBedsV1Room["Characteristic"] = "";
                                            #region Vat
                                            double mainbuy = itemprice;
                                            //_markup.ProcessHotelRoom_WithMarkup(CurrencyFactor, Currency, true, false, SearchParam.NoofNights, SearchParam.CityID, SearchParam.CountryID.ToString(), SearchParam.CheckInDate, SearchParam.CheckOutDate, ref itemprice, ref drHotelBedsV1Room, isPerce, AgentPercentage, AgentMarkupAmount, commonSearch.SubAgentMarkup, commonSearch.B2CMarkup, commonSearch.IsParentId);

                                            _markup.ProcessHotelRoom_WithMarkup(CurrencyFactor, Currency, true, true, SearchParam.NoofNights, SearchParam.CityID, SearchParam.CountryID.ToString(), SearchParam.CheckInDate, SearchParam.CheckOutDate, ref itemprice, ref drHotelBedsV1Room, isPerce, AgentPercentage, AgentMarkupAmount, commonSearch.SubAgentMarkup, commonSearch.B2CMarkup, commonSearch.IsParentId);
                                            #endregion

                                            drHotelBedsV1Room["SupplierName"] = Item.Supplier;
                                            string roomTypeId = Convert.ToString(10 + "$$$" + HBResponse.hotels.hotels[h].rooms[i].rates[j].rateKey + "$$$" + RateCommentID);


                                            if (HBResponse.hotels.hotels[h].rooms[i].rates[j].rateType.ToLower() == "bookable" && HBResponse.hotels.hotels[h].rooms[i].rates[j].cancellationPolicies != null)
                                            {
                                                bool chechk = false;
                                                if (HBResponse.hotels.hotels[h].rooms[i].rates[j].cancellationPolicies.Length > 0)
                                                {
                                                    var newList = HBResponse.hotels.hotels[h].rooms[i].rates[j].cancellationPolicies.OrderBy(x => x.from).ToList();
                                                    double TotalAmount = Convert.ToDouble(HBResponse.hotels.hotels[h].rooms[i].rates[j].net);
                                                    for (int cx = 0; cx < 1; cx++)
                                                    {
                                                        DateTime ToDate = Convert.ToDateTime(newList[cx].from).Date;
                                                        DateTime FromDate = DateTime.Now.Date;
                                                        if (ToDate.AddDays(CutOffDays).Date.AddDays(-1).Date > DateTime.Now.Date)
                                                            ToDate = ToDate.AddDays(CutOffDays).Date;
                                                        else
                                                            ToDate = DateTime.Now.Date;

                                                        double cancellationAmount = TotalAmount;// Convert.ToDouble(newList[cx].amount);
                                                        double VatOnPolicy = 0;
                                                        double cancellationAmountMain = cancellationAmount;
                                                        double CompanyChargeAmount = cancellationAmount;
                                                        double cancellationChargeAmount = _markup.ProcessHotelCancelationPrice_WithMarkup(ref cancellationAmountMain, SearchParam.NoofNights, SearchParam.CityID, SearchParam.CountryID.ToString(), cancellationAmount, isPerce, commonSearch.B2CMarkup, AgentPercentage, AgentMarkupAmount, commonSearch.IsParentId, commonSearch.SubAgentMarkup, ref VatOnPolicy);

                                                        if (ToDate > FromDate && chechk == false)
                                                        {
                                                            double zero = 0;

                                                            dtCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref zero, dtCancellation.NewRow(), CurrencyFactor, Currency, Item.Supplier, Convert.ToString(OwnsystemHotelId), /*(j + 1).ToString()*/(0 + 1).ToString(), roomTypeId, HBResponse.hotels.hotels[h].rooms[i].name, HBResponse.hotels.hotels[h].rooms[i].rates[j].boardName, DateTime.Now.Date, ToDate.AddDays(-1).AddHours(23).AddMinutes(59).AddSeconds(59), 0, 0, itemprice.ToString(), 0, "", "", "", ""));
                                                            chechk = true;
                                                        }
                                                        if (ToDate < FromDate)
                                                            ToDate = DateTime.Now.Date;

                                                        DateTime TO_Date = DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null);
                                                        dtCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref CompanyChargeAmount, dtCancellation.NewRow(), CurrencyFactor, Currency, Item.Supplier, Convert.ToString(OwnsystemHotelId), /*(j + 1).ToString()*/(0 + 1).ToString(), roomTypeId, HBResponse.hotels.hotels[h].rooms[i].name, HBResponse.hotels.hotels[h].rooms[i].rates[j].boardName, ToDate, TO_Date, cancellationAmountMain, cancellationChargeAmount, itemprice.ToString(), VatonfirstnightPrice, "", "", "", "", ""));
                                                    }
                                                    //for (int c = 0; c < HBResponse.hotels.hotels[h].rooms[i].rates[j].cancellationPolicies.Length; c++)
                                                    //{
                                                    //    DateTime ToDate = Convert.ToDateTime(HBResponse.hotels.hotels[h].rooms[i].rates[j].cancellationPolicies[c].from).Date;
                                                    //    DateTime FromDate = DateTime.Now.Date;
                                                    //    if (ToDate.AddDays(CutOffDays).Date.AddDays(-1).Date > DateTime.Now.Date)
                                                    //        ToDate = ToDate.AddDays(CutOffDays).Date;
                                                    //    else
                                                    //        ToDate = DateTime.Now.Date;

                                                    //    double cancellationAmount = Convert.ToDouble(HBResponse.hotels.hotels[h].rooms[i].rates[j].cancellationPolicies[c].amount);
                                                    //    double VatOnPolicy = 0;
                                                    //    double cancellationAmountMain = cancellationAmount;
                                                    //    double CompanyChargeAmount = cancellationAmount;
                                                    //    double cancellationChargeAmount = Common.ProcessHotelCancelationPrice_WithMarkup(ref cancellationAmountMain, SearchParam.NoofNights, SearchParam.CityID, SearchParam.CountryID.ToString(), cancellationAmount, isPerce, commonSearch.B2CMarkup, AgentPercentage, AgentMarkupAmount, commonSearch.IsParentId, commonSearch.SubAgentMarkup, ref VatOnPolicy);

                                                    //    if (ToDate > FromDate && chechk == false)
                                                    //    {
                                                    //        double zero = 0;

                                                    //        dtCancellation.Rows.Add(Common.setCancellationPolicyRow(ref zero, dtCancellation.NewRow(), CurrencyFactor, Currency, Item.Supplier, Convert.ToString(OwnsystemHotelId), /*(j + 1).ToString()*/(0 + 1).ToString(), roomTypeId, HBResponse.hotels.hotels[h].rooms[i].name, HBResponse.hotels.hotels[h].rooms[i].rates[j].boardCode, DateTime.Now.Date, ToDate.AddDays(-1).AddHours(23).AddMinutes(59).AddSeconds(59), 0, 0, itemprice.ToString(), 0, "", "", "", ""));
                                                    //        chechk = true;
                                                    //    }
                                                    //    if (ToDate < FromDate)
                                                    //        ToDate = DateTime.Now.Date;

                                                    //    DateTime TO_Date = DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null);
                                                    //    if (HBResponse.hotels.hotels[h].rooms[i].rates[j].cancellationPolicies.Length > 1)
                                                    //    {
                                                    //        if (HBResponse.hotels.hotels[h].rooms[i].rates[j].cancellationPolicies.Length == 1)
                                                    //        {
                                                    //            TO_Date = DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null);
                                                    //        }
                                                    //        else
                                                    //        {
                                                    //            if ((HBResponse.hotels.hotels[h].rooms[i].rates[j].cancellationPolicies.Length) == (c + 1))//Final loop
                                                    //            {
                                                    //                TO_Date = DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null);
                                                    //            }
                                                    //            else
                                                    //            {
                                                    //                TO_Date = Convert.ToDateTime(HBResponse.hotels.hotels[h].rooms[i].rates[j].cancellationPolicies[c + 1].from);
                                                    //                TO_Date = TO_Date.AddDays(CutOffDays).Date.AddDays(-1);
                                                    //            }
                                                    //        }
                                                    //    }

                                                    //    dtCancellation.Rows.Add(Common.setCancellationPolicyRow(ref CompanyChargeAmount, dtCancellation.NewRow(), CurrencyFactor, Currency, Item.Supplier, Convert.ToString(OwnsystemHotelId), /*(j + 1).ToString()*/(0 + 1).ToString(), roomTypeId, HBResponse.hotels.hotels[h].rooms[i].name, HBResponse.hotels.hotels[h].rooms[i].rates[j].boardCode, ToDate, TO_Date, cancellationAmountMain, cancellationChargeAmount, itemprice.ToString(), VatonfirstnightPrice, "", "", "", "", ""));
                                                    //}

                                                }
                                            }
                                            else
                                            {
                                                if (withcancellation == true)
                                                {
                                                    double cancellationAmount = itemprice;
                                                    double cancellationAmountMain = cancellationAmount;
                                                    double CompanyChargeAmount = cancellationAmount;
                                                    double cancellationChargeAmount = cancellationAmount;
                                                    DateTime ToDate = DateTime.Now.Date;
                                                    dtCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref CompanyChargeAmount, dtCancellation.NewRow(), CurrencyFactor, Currency, Item.Supplier, Convert.ToString(OwnsystemHotelId), /*(j + 1).ToString()*/(0 + 1).ToString(), roomTypeId, HBResponse.hotels.hotels[h].rooms[i].name, HBResponse.hotels.hotels[h].rooms[i].rates[j].boardName, ToDate, DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null), cancellationAmountMain, cancellationChargeAmount, itemprice.ToString(), VatonfirstnightPrice, "", "", "", "", ""));
                                                }
                                            }
                                            drHotelBedsV1Room["RoomTypeName"] = HBResponse.hotels.hotels[h].rooms[i].name;
                                            drHotelBedsV1Room["RoomTypeId"] = roomTypeId;
                                            dtHotelRoom.Rows.Add(drHotelBedsV1Room);
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
                }
            }
            catch (Exception e)
            {
                Item.ResponseType = "NoCount";
                await _exception.Error(e);
            }
            finally
            {
                #region ResProcessTimer
                try
                {
                    if (SearchParam != null && ResProccessTimerLogActive == "1" && timer_room_HotelbedsV1.ElapsedMilliseconds > Convert.ToInt32(ResProccessTimeValueforSaveLog))
                    {
                        timer_room_HotelbedsV1.Stop();
                        SearchParam.SupplierTime += ",|Finish:HotelbedsV1 Process Response  " + "|Time :" + timer_room_HotelbedsV1.Elapsed.ToString();
                        await Task.Run(async () => await _commonMethod.FireAway_SQL_ResPorcessTimeLog("", timer_room_HotelbedsV1.Elapsed.ToString(), "HotelbedsV1", SearchParam.SearchKey, 1, SearchParam.AgentID));
                    }
                }

                catch (Exception x)
                {
                    await _exception.Error(x);
                }
                #endregion

                if (Item.ResponseType == "NoCount" && StoreSupplierResErrorLog == "1")
                    await _commonMethod.XMLRequestResponseTrack_Error(Item.RQRS[0].Request, ResponseData, Item.Supplier, timer_room_HotelbedsV1.Elapsed.ToString(), "", SearchParam.SearchKey);
            }
            return options;
        }

        public async Task<List<Options>> HotelbedsV1_HotelSearchWithRooms(bool SearchByOneID, HotelX Item, CommonSearch commonSearch, List<RQRS> rQRS, string WSurl, string Username, string Password, Int32 SearchTimeOut, HotelSearchParameter SearchParam, bool DebugMode, string Currency, double CurrencyFactor, string ProccessCutOffTime, Int32 processTimeout, string ResProccessTimerLogActive, string ResProccessTimeValueforSaveLog, bool withcancellation, string StoreSupplierResErrorLog)
        {
            System.Diagnostics.Stopwatch timer_room_HotelbedsV1 = new System.Diagnostics.Stopwatch();
            timer_room_HotelbedsV1.Start();
            List<Options> options = new List<Options>();
            string ResponseData = rQRS[0].Response;
            DataTable DtHotel = _commonMethod.GenerateDataTableNewV2();
            try
            {
                //string APINAME = Convert.ToString(HttpContext.Current.Request.Url).ToLower();//.Contains("availability")
                if (ResponseData == "")
                {
                    Item.ResponseType = "NoCount";
                    return options;
                }
                Boolean blnIsShowSupplier = false;

                int CutOffDays = XMLHotelBookingCommon.CutOffDays;
                CutOffDays = _commonMethod.GetCutOffDays(commonSearch.dtSuplierMarkup, Item.Supplier);
                {

                    double FinalPrice = 1;
                    #region "When not Need Room"  
                    // add hotel code from price only and room code blank here
                    if (SearchParam.Filters.IsShowRooms == 0)
                    {
                        HBV1Class.Rootobject HBResponse = null;
                        try
                        {
                            //HBResponse = JsonSerializer.Deserialize<HBV1Class.Rootobject>(ResponseData);
                            HBResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<HBV1Class.Rootobject>(ResponseData);


                        }
                        catch (Exception ex)
                        {
                            Item.ResponseType = "NoCount";
                            return options;
                        }
                        if (HBResponse == null || HBResponse.hotels == null || HBResponse.hotels.hotels == null || HBResponse.hotels.hotels.Count() == 0)
                        {
                            Item.ResponseType = "NoCount";
                            return options;
                        }

                        int counthotel = HBResponse.hotels.hotels.Count();
                        for (int h = 0; h < counthotel; h++)
                        {
                            Item.ResponseType = "Success";
                            #region "Hotel Code Block "
                            if (commonSearch.tokenData.AgentId != 1899)
                            {
                                FinalPrice = 0;
                                for (int c = 0; c < SearchParam.Rooms.Count(); c++)
                                {
                                    int NoOfAdults = SearchParam.Rooms[c].NoofAdults;
                                    bool chk = false;
                                    double Total = 0;

                                    for (int a = 0; a < HBResponse.hotels.hotels[h].rooms.Count(); a++)
                                    {
                                        if (chk == false)
                                        {

                                            var p = HBResponse.hotels.hotels[h].rooms[a].rates
                                                        .Where(r => r.adults == NoOfAdults) // Filter by NoOfAdults
                                                        .Select(r => r.net) // Select only the net price
                                                        .Min();

                                            double MinPrice = 0;
                                            if (p != "")
                                            {
                                                double Price = Convert.ToDouble(p);
                                                chk = true;
                                                if (MinPrice == 0)
                                                    MinPrice = Price;
                                                else
                                                {
                                                    if (Price < MinPrice)
                                                        MinPrice = Price;
                                                }

                                            }

                                            if (MinPrice != 0)
                                                Total = Total + MinPrice;
                                        }
                                    }
                                    FinalPrice = FinalPrice + Total;
                                }
                            }
                            //End
                            DataRow drXmlHotel = DtHotel.NewRow();
                            drXmlHotel["IsRecomondedHotel"] = false;
                            drXmlHotel["XMLHotelCode"] = Convert.ToString(HBResponse.hotels.hotels[h].code);
                            drXmlHotel["HotelFrontImage"] = "";
                            drXmlHotel["HotelName"] = "";
                            drXmlHotel["Description"] = "";
                            drXmlHotel["EssentialInformation"] = "";
                            drXmlHotel["OfficeID"] = 0;
                            drXmlHotel["ContractID"] = 0;
                            drXmlHotel["IsShowSupplier"] = blnIsShowSupplier;
                            drXmlHotel["OwnSystemHotelId"] = 0;
                            drXmlHotel["Address"] = "";
                            drXmlHotel["Favorite"] = 0;
                            drXmlHotel["Rating"] = 0;
                            drXmlHotel["XmlHotelCity"] = Convert.ToString(HBResponse.hotels.hotels[h].destinationCode);
                            drXmlHotel["XMLCountry"] = "";
                            drXmlHotel["XMLCity"] = Convert.ToString(HBResponse.hotels.hotels[h].destinationCode);
                            // drXmlHotel["SupplierName"] = "HotelBedsV1";
                            drXmlHotel["SupplierName"] = Item.Supplier;
                            drXmlHotel["BookingStatus"] = "Available";
                            drXmlHotel["IsOffer"] = "";
                            drXmlHotel["FinalSellingPrice"] = FinalPrice;
                            drXmlHotel["SellingPrice"] = FinalPrice;
                            drXmlHotel["UniqueNo"] = Convert.ToString(HBResponse.hotels.hotels[h].code);
                            drXmlHotel["AvailToken"] = "";
                            drXmlHotel["SupplierCurrency"] = Currency;

                            #endregion


                            if (FinalPrice > 0)
                            {
                                #region "Room code Block"
                                bool willAddOrNot = true;
                                Int64 RaynaID = 0;
                                Int64 GiataId = 0;
                                int Rating = 0;
                                Int64 OwnsystemHotelId = 0;
                                double MinPrice = FinalPrice;
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

                    }
                    #endregion
                    #region "When Need Rooms"


                    if (SearchParam.Filters.IsShowRooms == 1)
                    {

                        HBV1Class.Rootobject HBResponse = null;
                        try
                        {
                            //HBResponse = JsonSerializer.Deserialize<HBV1Class.Rootobject>(ResponseData);
                            HBResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<HBV1Class.Rootobject>(ResponseData);


                        }
                        catch (Exception ex)
                        {
                            Item.ResponseType = "NoCount";
                            return options;
                        }


                        for (int h = 0; h < HBResponse.hotels.total; h++)
                        {
                            Item.ResponseType = "Success";
                            if (ProccessCutOffTime == "1" && timer_room_HotelbedsV1.ElapsedMilliseconds > processTimeout)
                            {
                                timer_room_HotelbedsV1.Stop();
                                break;
                            }
                            #region Hotels
                            DataRow drXmlHotel = DtHotel.NewRow();
                            drXmlHotel["IsRecomondedHotel"] = false;
                            drXmlHotel["XMLHotelCode"] = HBResponse.hotels.hotels[h].code;
                            drXmlHotel["OfficeID"] = 0;
                            drXmlHotel["ContractID"] = 0;
                            drXmlHotel["IsShowSupplier"] = blnIsShowSupplier;
                            drXmlHotel["OwnSystemHotelId"] = 0;
                            drXmlHotel["Address"] = "";
                            drXmlHotel["Favorite"] = 0;
                            drXmlHotel["Rating"] = 0;
                            drXmlHotel["XmlHotelCity"] = HBResponse.hotels.hotels[h].destinationCode;
                            drXmlHotel["XMLCountry"] = "";
                            drXmlHotel["XMLCity"] = HBResponse.hotels.hotels[h].destinationCode;
                            drXmlHotel["SupplierName"] = Item.Supplier;
                            drXmlHotel["BookingStatus"] = "Available";
                            drXmlHotel["IsOffer"] = "";
                            drXmlHotel["FinalSellingPrice"] = FinalPrice;
                            drXmlHotel["SellingPrice"] = FinalPrice;
                            drXmlHotel["UniqueNo"] = HBResponse.hotels.hotels[h].code;
                            drXmlHotel["AvailToken"] = "";
                            drXmlHotel["SupplierCurrency"] = Currency;
                            #region "Hotel Block here"
                            #endregion
                            #region "GetRooms for this hotel"
                            bool willAddOrNot = true;
                            Int64 RaynaID = 0;
                            Int64 GiataId = 0;
                            int RatingHotel = 0;
                            Int64 OwnsystemHotelId = 0;
                            string Provider = Item.Supplier;
                            double MinPrice = FinalPrice;
                            _markup.GetHotelDataCommon(ref MinPrice, ref willAddOrNot, ref OwnsystemHotelId, ref RaynaID, ref GiataId, ref RatingHotel, ref drXmlHotel, commonSearch.AgentId, commonSearch.TariffID, commonSearch.dsAgentDetails, commonSearch.supplierGiataMaster, SearchParam, commonSearch.dtCountryCityMarkup, commonSearch.B2CMarkup, commonSearch.IsParentId, commonSearch.SubAgentMarkup, commonSearch.SearchByHotel, commonSearch.HotelIDS);
                            if (willAddOrNot)
                            {
                                DataTable dtHotelRoom = _commonMethod.GenerateRoomtypeDataTableNewV2();
                                DataTable dtCancellation = _commonMethod.GenerateCancellationDataTableNewV2();
                                DataTable DtRoomRateBreackUp = _commonMethod.GenerateoomRateBreackUpDataTableNewV2();

                                #region "Room Logic"
                                if (SearchParam.Filters.IsShowRooms == 1)
                                {
                                    double AgentPercentage = 0.0;
                                    double AgentMarkupAmount = 0.0;
                                    Boolean isPerce = true;

                                    double AgentCompanyPercentage = Convert.ToDouble(SearchParam.AgentCompanyPercentage);
                                    bool IsB2C = Convert.ToBoolean(Convert.ToByte(SearchParam.IsB2BOrB2C));

                                    _markup.getAgetMarkupSetting(Provider, GiataId.ToString(), commonSearch.dtSuplierMarkup, commonSearch.dtMarkupDetails, ref AgentPercentage, ref AgentMarkupAmount, ref blnIsShowSupplier, ref isPerce, commonSearch.AgentId, RatingHotel, SearchParam.CityID, SearchParam.CountryID.ToString(), SearchParam.CheckInDate);

                                    #region Supplier Room Code block
                                    for (int i = 0; i < HBResponse.hotels.hotels[h].rooms.Length; i++)
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
                                        double VatonfirstnightPrice = 0;

                                        for (int j = 0; j < HBResponse.hotels.hotels[h].rooms[i].rates.Length; j++)
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
                                            double itemprice = Convert.ToDouble(HBResponse.hotels.hotels[h].rooms[i].rates[j].net);
                                            string Essen = "", RateCommentID = "";
                                            if (HBResponse.hotels.hotels[h].rooms[i].rates[j].rateCommentsId != null)
                                                RateCommentID = HBResponse.hotels.hotels[h].rooms[i].rates[j].rateCommentsId;
                                            DataRow drHotelBedsV1Room = dtHotelRoom.NewRow();
                                            drHotelBedsV1Room["OwnsystemHotelId"] = OwnsystemHotelId;
                                            //drHotelBedsV1Room["RoomTypeCode"] = HBResponse.hotels.hotels[h].rooms[i].rates[j].boardCode;
                                            //drHotelBedsV1Room["MealName"] = HBResponse.hotels.hotels[h].rooms[i].rates[j].boardCode;
                                            //drHotelBedsV1Room["MealId"] = HBResponse.hotels.hotels[h].rooms[i].rates[j].boardCode;
                                            drHotelBedsV1Room["RoomTypeCode"] = HBResponse.hotels.hotels[h].rooms[i].rates[j].boardName; //added on 20-JUN-2024 as per mustafa sir
                                            drHotelBedsV1Room["MealName"] = HBResponse.hotels.hotels[h].rooms[i].rates[j].boardName;
                                            drHotelBedsV1Room["MealId"] = HBResponse.hotels.hotels[h].rooms[i].rates[j].boardName;
                                            drHotelBedsV1Room["BookingStatus"] = "Available";
                                            drHotelBedsV1Room["UniqueNo"] = HBResponse.hotels.hotels[h].code;
                                            drHotelBedsV1Room["ContractID"] = 0;
                                            int RoomNo = 1;
                                            drHotelBedsV1Room["RoomNo"] = RoomNo;
                                            drHotelBedsV1Room["BoardTypeID"] = "";
                                            drHotelBedsV1Room["AdultsOccupancy"] = HBResponse.hotels.hotels[h].rooms[i].rates[j].adults;
                                            drHotelBedsV1Room["ChildOccupancy"] = HBResponse.hotels.hotels[h].rooms[i].rates[j].children;
                                            drHotelBedsV1Room["RoomSequenceNo"] = "";
                                            if (HBResponse.hotels.hotels[h].rooms[i].rates[j].rateType != "BOOKABLE")
                                            {
                                                drHotelBedsV1Room["EssentialInformation"] = "N/A";
                                            }
                                            else
                                            {
                                                if (HBResponse.hotels.hotels[h].rooms[i].rates[j].rateComments != null)
                                                {
                                                    Essen = HBResponse.hotels.hotels[h].rooms[i].rates[j].rateComments;
                                                    drHotelBedsV1Room["EssentialInformation"] = Essen;
                                                }
                                                else
                                                {
                                                    drHotelBedsV1Room["EssentialInformation"] = " ";
                                                }
                                            }
                                            if (HBResponse.hotels.hotels[h].rooms[i].rates[j].promotions != null)
                                            {
                                                string Promotion = "";
                                                string PromotionRemark = "";
                                                if (HBResponse.hotels.hotels[h].rooms[i].rates[j].promotions.Length > 0)
                                                {
                                                    Promotion = HBResponse.hotels.hotels[h].rooms[i].rates[j].promotions[0].name;
                                                    PromotionRemark = HBResponse.hotels.hotels[h].rooms[i].rates[j].promotions[0].remark;
                                                    drHotelBedsV1Room["EssentialInformation"] = Essen + "<br><br> <b>Promotions : </b>" + Promotion + "<br>" + PromotionRemark + "<br>";
                                                }
                                            }
                                            drHotelBedsV1Room["Characteristic"] = "";
                                            #region Vat
                                            double mainbuy = itemprice;
                                            _markup.ProcessHotelRoom_WithMarkup(CurrencyFactor, Currency, true, false, SearchParam.NoofNights, SearchParam.CityID, SearchParam.CountryID.ToString(), SearchParam.CheckInDate, SearchParam.CheckOutDate, ref itemprice, ref drHotelBedsV1Room, isPerce, AgentPercentage, AgentMarkupAmount, commonSearch.SubAgentMarkup, commonSearch.B2CMarkup, commonSearch.IsParentId);
                                            #endregion

                                            drHotelBedsV1Room["SupplierName"] = Item.Supplier;
                                            string roomTypeId = Convert.ToString(10 + "$$$" + HBResponse.hotels.hotels[h].rooms[i].rates[j].rateKey + "$$$" + RateCommentID + "$$$" + Guid.NewGuid().ToString().Substring(0, 5));


                                            if (HBResponse.hotels.hotels[h].rooms[i].rates[j].rateType.ToLower() == "bookable" && HBResponse.hotels.hotels[h].rooms[i].rates[j].cancellationPolicies != null)
                                            {
                                                bool chechk = false;
                                                if (HBResponse.hotels.hotels[h].rooms[i].rates[j].cancellationPolicies.Length > 0)
                                                {
                                                    var newList = HBResponse.hotels.hotels[h].rooms[i].rates[j].cancellationPolicies.OrderBy(x => x.from).ToList();
                                                    double TotalAmount = Convert.ToDouble(HBResponse.hotels.hotels[h].rooms[i].rates[j].net);
                                                    for (int cx = 0; cx < 1; cx++)
                                                    {
                                                        DateTime ToDate = Convert.ToDateTime(newList[cx].from).Date;
                                                        DateTime FromDate = DateTime.Now.Date;
                                                        if (ToDate.AddDays(CutOffDays).Date.AddDays(-1).Date > DateTime.Now.Date)
                                                            ToDate = ToDate.AddDays(CutOffDays).Date;
                                                        else
                                                            ToDate = DateTime.Now.Date;

                                                        double cancellationAmount = TotalAmount;// Convert.ToDouble(newList[cx].amount);
                                                        double VatOnPolicy = 0;
                                                        double cancellationAmountMain = cancellationAmount;
                                                        double CompanyChargeAmount = cancellationAmount;
                                                        double cancellationChargeAmount = _markup.ProcessHotelCancelationPrice_WithMarkup(ref cancellationAmountMain, SearchParam.NoofNights, SearchParam.CityID, SearchParam.CountryID.ToString(), cancellationAmount, isPerce, commonSearch.B2CMarkup, AgentPercentage, AgentMarkupAmount, commonSearch.IsParentId, commonSearch.SubAgentMarkup, ref VatOnPolicy);

                                                        if (ToDate > FromDate && chechk == false)
                                                        {
                                                            double zero = 0;

                                                            dtCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref zero, dtCancellation.NewRow(), CurrencyFactor, Currency, Item.Supplier, Convert.ToString(OwnsystemHotelId), /*(j + 1).ToString()*/(0 + 1).ToString(), roomTypeId, HBResponse.hotels.hotels[h].rooms[i].name, HBResponse.hotels.hotels[h].rooms[i].rates[j].boardName, DateTime.Now.Date, ToDate.AddDays(-1).AddHours(23).AddMinutes(59).AddSeconds(59), 0, 0, itemprice.ToString(), 0, "", "", "", ""));
                                                            chechk = true;
                                                        }
                                                        if (ToDate < FromDate)
                                                            ToDate = DateTime.Now.Date;

                                                        DateTime TO_Date = DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null);
                                                        dtCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref CompanyChargeAmount, dtCancellation.NewRow(), CurrencyFactor, Currency, Item.Supplier, Convert.ToString(OwnsystemHotelId), /*(j + 1).ToString()*/(0 + 1).ToString(), roomTypeId, HBResponse.hotels.hotels[h].rooms[i].name, HBResponse.hotels.hotels[h].rooms[i].rates[j].boardName, ToDate, TO_Date, cancellationAmountMain, cancellationChargeAmount, itemprice.ToString(), VatonfirstnightPrice, "", "", "", "", ""));
                                                    }
                                                    //for (int c = 0; c < HBResponse.hotels.hotels[h].rooms[i].rates[j].cancellationPolicies.Length; c++)
                                                    //{
                                                    //    DateTime ToDate = Convert.ToDateTime(HBResponse.hotels.hotels[h].rooms[i].rates[j].cancellationPolicies[c].from).Date;
                                                    //    DateTime FromDate = DateTime.Now.Date;
                                                    //    if (ToDate.AddDays(CutOffDays).Date.AddDays(-1).Date > DateTime.Now.Date)
                                                    //        ToDate = ToDate.AddDays(CutOffDays).Date;
                                                    //    else
                                                    //        ToDate = DateTime.Now.Date;

                                                    //    double cancellationAmount = Convert.ToDouble(HBResponse.hotels.hotels[h].rooms[i].rates[j].cancellationPolicies[c].amount);
                                                    //    double VatOnPolicy = 0;
                                                    //    double cancellationAmountMain = cancellationAmount;
                                                    //    double CompanyChargeAmount = cancellationAmount;
                                                    //    double cancellationChargeAmount = Common.ProcessHotelCancelationPrice_WithMarkup(ref cancellationAmountMain, SearchParam.NoofNights, SearchParam.CityID, SearchParam.CountryID.ToString(), cancellationAmount, isPerce, commonSearch.B2CMarkup, AgentPercentage, AgentMarkupAmount, commonSearch.IsParentId, commonSearch.SubAgentMarkup, ref VatOnPolicy);

                                                    //    if (ToDate > FromDate && chechk == false)
                                                    //    {
                                                    //        double zero = 0;

                                                    //        dtCancellation.Rows.Add(Common.setCancellationPolicyRow(ref zero, dtCancellation.NewRow(), CurrencyFactor, Currency, Item.Supplier, Convert.ToString(OwnsystemHotelId), /*(j + 1).ToString()*/(0 + 1).ToString(), roomTypeId, HBResponse.hotels.hotels[h].rooms[i].name, HBResponse.hotels.hotels[h].rooms[i].rates[j].boardCode, DateTime.Now.Date, ToDate.AddDays(-1).AddHours(23).AddMinutes(59).AddSeconds(59), 0, 0, itemprice.ToString(), 0, "", "", "", ""));
                                                    //        chechk = true;
                                                    //    }
                                                    //    if (ToDate < FromDate)
                                                    //        ToDate = DateTime.Now.Date;

                                                    //    DateTime TO_Date = DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null);
                                                    //    if (HBResponse.hotels.hotels[h].rooms[i].rates[j].cancellationPolicies.Length > 1)
                                                    //    {
                                                    //        if (HBResponse.hotels.hotels[h].rooms[i].rates[j].cancellationPolicies.Length == 1)
                                                    //        {
                                                    //            TO_Date = DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null);
                                                    //        }
                                                    //        else
                                                    //        {
                                                    //            if ((HBResponse.hotels.hotels[h].rooms[i].rates[j].cancellationPolicies.Length) == (c + 1))//Final loop
                                                    //            {
                                                    //                TO_Date = DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null);
                                                    //            }
                                                    //            else
                                                    //            {
                                                    //                TO_Date = Convert.ToDateTime(HBResponse.hotels.hotels[h].rooms[i].rates[j].cancellationPolicies[c + 1].from);
                                                    //                TO_Date = TO_Date.AddDays(CutOffDays).Date.AddDays(-1);
                                                    //            }
                                                    //        }
                                                    //    }

                                                    //    dtCancellation.Rows.Add(Common.setCancellationPolicyRow(ref CompanyChargeAmount, dtCancellation.NewRow(), CurrencyFactor, Currency, Item.Supplier, Convert.ToString(OwnsystemHotelId), /*(j + 1).ToString()*/(0 + 1).ToString(), roomTypeId, HBResponse.hotels.hotels[h].rooms[i].name, HBResponse.hotels.hotels[h].rooms[i].rates[j].boardCode, ToDate, TO_Date, cancellationAmountMain, cancellationChargeAmount, itemprice.ToString(), VatonfirstnightPrice, "", "", "", "", ""));
                                                    //}

                                                }
                                            }
                                            else
                                            {
                                                if (withcancellation == true)
                                                {
                                                    double cancellationAmount = itemprice;
                                                    double cancellationAmountMain = cancellationAmount;
                                                    double CompanyChargeAmount = cancellationAmount;
                                                    double cancellationChargeAmount = cancellationAmount;
                                                    DateTime ToDate = DateTime.Now.Date;
                                                    dtCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref CompanyChargeAmount, dtCancellation.NewRow(), CurrencyFactor, Currency, Item.Supplier, Convert.ToString(OwnsystemHotelId), /*(j + 1).ToString()*/(0 + 1).ToString(), roomTypeId, HBResponse.hotels.hotels[h].rooms[i].name, HBResponse.hotels.hotels[h].rooms[i].rates[j].boardName, ToDate, DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null), cancellationAmountMain, cancellationChargeAmount, itemprice.ToString(), VatonfirstnightPrice, "", "", "", "", ""));
                                                }
                                            }
                                            drHotelBedsV1Room["RoomTypeName"] = HBResponse.hotels.hotels[h].rooms[i].name;
                                            drHotelBedsV1Room["RoomTypeId"] = roomTypeId;
                                            dtHotelRoom.Rows.Add(drHotelBedsV1Room);
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
                    if (SearchParam != null && ResProccessTimerLogActive == "1" && timer_room_HotelbedsV1.ElapsedMilliseconds > Convert.ToInt32(ResProccessTimeValueforSaveLog))
                    {
                        timer_room_HotelbedsV1.Stop();
                        SearchParam.SupplierTime += ",|Finish:HotelbedsV1 Process Response  " + "|Time :" + timer_room_HotelbedsV1.Elapsed.ToString();
                        await Task.Run(async () => await _commonMethod.FireAway_SQL_ResPorcessTimeLog("", timer_room_HotelbedsV1.Elapsed.ToString(), "HotelbedsV1", SearchParam.SearchKey, 1, SearchParam.AgentID));
                    }
                }
                catch (Exception x)
                {
                    await _exception.Error(x);
                }
                #endregion

                if (Item.ResponseType == "NoCount" && StoreSupplierResErrorLog == "1")
                    await _commonMethod.XMLRequestResponseTrack_Error(Item.RQRS[0].Request, ResponseData, Item.Supplier, timer_room_HotelbedsV1.Elapsed.ToString(), "", SearchParam.SearchKey);
            }
            return options;
        }
        public async Task<List<Options>> hotelbedsv1_new_HotelSearchWithRooms(bool SearchByOneID, HotelX Item, CommonSearch commonSearch, List<RQRS> rQRS, string WSurl, string Username, string Password, Int32 SearchTimeOut, HotelSearchParameter SearchParam, bool DebugMode, string currency, double CurrencyFactor, string ProccessCutOffTime, Int32 processTimeout, string ResProccessTimerLogActive, string ResProccessTimeValueforSaveLog, bool withcancellation, string StoreSupplierResErrorLog)
        {
            System.Diagnostics.Stopwatch timer_room_HotelbedsV1 = new System.Diagnostics.Stopwatch();
            timer_room_HotelbedsV1.Start();
            List<Options> options = new List<Options>();
            string ResponseData = rQRS[0].Response;
            DataTable DtHotel = _commonMethod.GenerateDataTableNewV2();
            try
            {
                //string APINAME = Convert.ToString(HttpContext.Current.Request.Url).ToLower();//.Contains("availability")
                if (ResponseData == "")
                {
                    Item.ResponseType = "NoCount";
                    return options;
                }
                Boolean blnIsShowSupplier = false;
                string ConvertXml = "";

                

                int CutOffDays = CacheMaster.CutOffDays;
                // CutOffDays = Common.GetCutOffDays(commonSearch.dtSuplierMarkup, "HotelBedsV1");
                CutOffDays = _commonMethod.GetCutOffDays(commonSearch.dtSuplierMarkup, Item.Supplier);
                //for (int h = 1; h < counthotel; h++)
                {
                    Item.ResponseType = "Success";
                    
                    double FinalPrice = 1;
                    #region "When not Need Room"  
                    // add hotel code from price only and room code blank here
                    HBV1Class.Rootobject HBResponse = null;
                    try
                    {
                        //HBResponse = JsonSerializer.Deserialize<HBV1Class.Rootobject>(ResponseData);
                        HBResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<HBV1Class.Rootobject>(ResponseData);


                    }
                    catch (Exception ex)
                    {
                        Item.ResponseType = "NoCount";
                        return options;
                    }
                    if (HBResponse == null || HBResponse.hotels == null || HBResponse.hotels.hotels == null || HBResponse.hotels.hotels.Count() == 0)
                    {
                        Item.ResponseType = "NoCount";
                        return options;
                    }
                    
                    int counthotel = HBResponse.hotels.hotels.Count();
                    for (int h = 0; h < counthotel; h++)
                    {
                        string Hotel_ID = Convert.ToString(HBResponse.hotels.hotels[h].code);//New added 5-1-24
                        if (SearchParam.Filters.IsShowRooms == 0)
                        {
                            Item.ResponseType = "Success";
                            #region "Hotel Code Block "
                            if (commonSearch.tokenData.AgentId != 1899)
                            {
                                FinalPrice = 0;
                            for (int c = 0; c < SearchParam.Rooms.Count(); c++)
                                {
                                    int NoOfAdults = SearchParam.Rooms[c].NoofAdults;
                                    bool chk = false;
                                    double Total = 0;

                                    //for (int a = 0; a < response.hotels.hotels[hotel].rooms.Count; a++)
                                    {
                                        if (chk == false)
                                        {
                                            string p = HBResponse.hotels.hotels[h].rooms[0].rates[0].net;
                                            double MinPrice = 0;
                                            if (p != "")
                                            {
                                                double Price = Convert.ToDouble(p);
                                                chk = true;
                                                if (MinPrice == 0)
                                                    MinPrice = Price;
                                                else
                                                {
                                                    if (Price < MinPrice)
                                                        MinPrice = Price;
                                                }
                                            }

                                            if (MinPrice != 0)
                                                Total = Total + MinPrice;
                                        }
                                    }
                                    FinalPrice = FinalPrice + Total;
                                }
                            }
                            //End

                            DataRow drXmlHotel = DtHotel.NewRow();
                            drXmlHotel["IsRecomondedHotel"] = false;
                            drXmlHotel["XMLHotelCode"] = Convert.ToString(HBResponse.hotels.hotels[h].code);
                            drXmlHotel["HotelFrontImage"] = "";
                            drXmlHotel["HotelName"] = "";
                            drXmlHotel["Description"] = "";
                            drXmlHotel["EssentialInformation"] = "";
                            drXmlHotel["OfficeID"] = 0;
                            drXmlHotel["ContractID"] = 0;
                            drXmlHotel["IsShowSupplier"] = blnIsShowSupplier;
                            drXmlHotel["OwnSystemHotelId"] = 0;
                            drXmlHotel["Address"] = "";
                            drXmlHotel["Favorite"] = 0;
                            drXmlHotel["Rating"] = 0;
                            drXmlHotel["XmlHotelCity"] = Convert.ToString(HBResponse.hotels.hotels[h].destinationCode);
                            drXmlHotel["XMLCountry"] = "";
                            drXmlHotel["XMLCity"] = Convert.ToString(HBResponse.hotels.hotels[h].destinationCode);
                            drXmlHotel["SupplierName"] = Item.Supplier;
                            drXmlHotel["BookingStatus"] = "Available";
                            drXmlHotel["IsOffer"] = "";
                            drXmlHotel["FinalSellingPrice"] = FinalPrice;
                            drXmlHotel["SellingPrice"] = FinalPrice;
                            drXmlHotel["UniqueNo"] = Convert.ToString(HBResponse.hotels.hotels[h].code);
                            drXmlHotel["AvailToken"] = "";
                            drXmlHotel["SupplierCurrency"] = currency;

                            #endregion


                            if (FinalPrice > 0)
                            {
                                #region "Room code Block"
                                bool willAddOrNot = true;
                                Int64 RaynaID = 0;
                                Int64 GiataId = 0;
                                int Rating = 0;
                                Int64 OwnsystemHotelId = 0;
                                double MinPrice = FinalPrice;
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
                        else
                        #endregion
                        #region "When Need Rooms"
                        {
                            if (ProccessCutOffTime == "1" && timer_room_HotelbedsV1.ElapsedMilliseconds > processTimeout)
                            {
                                timer_room_HotelbedsV1.Stop();
                                break;
                            }

                            #region hotelbedsnew

                            string Essen = "";
                            string RateType = "";
                            int NoOfNight = SearchParam.NoofNights;
                            DataRow drXmlHotel = DtHotel.NewRow();
                            drXmlHotel["IsRecomondedHotel"] = false;
                            drXmlHotel["XMLHotelCode"] = Convert.ToString(HBResponse.hotels.hotels[h].code);
                            drXmlHotel["HotelFrontImage"] = "";
                            drXmlHotel["HotelName"] = "";
                            drXmlHotel["Description"] = "";
                            drXmlHotel["EssentialInformation"] = "";
                            drXmlHotel["OfficeID"] = 0;
                            drXmlHotel["ContractID"] = 0;
                            drXmlHotel["IsShowSupplier"] = blnIsShowSupplier;
                            drXmlHotel["OwnSystemHotelId"] = 0;
                            drXmlHotel["Address"] = "";
                            drXmlHotel["Favorite"] = 0;
                            drXmlHotel["Rating"] = 0;
                            drXmlHotel["XmlHotelCity"] = Convert.ToString(HBResponse.hotels.hotels[h].destinationCode);
                            drXmlHotel["XMLCountry"] = "";
                            drXmlHotel["XMLCity"] = Convert.ToString(HBResponse.hotels.hotels[h].destinationCode);
                            drXmlHotel["SupplierName"] = Item.Supplier;
                            drXmlHotel["BookingStatus"] = "Available";
                            drXmlHotel["IsOffer"] = "";
                            drXmlHotel["FinalSellingPrice"] = FinalPrice;
                            drXmlHotel["SellingPrice"] = FinalPrice;
                            drXmlHotel["UniqueNo"] = Convert.ToString(HBResponse.hotels.hotels[h].code);
                            drXmlHotel["AvailToken"] = "";
                            drXmlHotel["SupplierCurrency"] = currency;
                            #region "GetRooms for this hotel"
                            bool willAddOrNot = true;
                            Int64 RaynaID = 0;
                            Int64 GiataId = 0;
                            int RatingHotel = 0;
                            Int64 OwnsystemHotelId = 0;
                            string Provider = Item.Supplier;
                            double MinPrice = FinalPrice;
                            _markup.GetHotelDataCommon(ref MinPrice, ref willAddOrNot, ref OwnsystemHotelId, ref RaynaID, ref GiataId, ref RatingHotel, ref drXmlHotel, commonSearch.AgentId, commonSearch.TariffID, commonSearch.dsAgentDetails, commonSearch.supplierGiataMaster, SearchParam, commonSearch.dtCountryCityMarkup, commonSearch.B2CMarkup, commonSearch.IsParentId, commonSearch.SubAgentMarkup, commonSearch.SearchByHotel, commonSearch.HotelIDS);
                            if (willAddOrNot)
                            {
                                DataTable DtHotelBedsV1_HotelRoom = _commonMethod.GenerateRoomtypeDataTableNewV2();

                                DataTable dtHotelBedsV1Room = _commonMethod.GenerateRoomtypeDataTableNewV2();
                                DataTable dtHotelBedsV1RoomRateBreakup = _commonMethod.GenerateoomRateBreackUpDataTableNewV2();
                                DataTable dtHotelBedsV1RoomCacelationPolicy = _commonMethod.GenerateCancellationDataTableNewV2();
                                DataTable DtRateBreackUp = _commonMethod.GenerateoomRateBreackUpDataTableNewV2();
                                Boolean isPerce = true;
                                double AgentPercentage = 0.0;
                                double AgentMarkupAmount = 0.0;
                                double SubAgentMarkup = 0;
                                double ratePerNight = 0;
                                int IsParentId = 1;
                                
                                _markup.getAgetMarkupSetting(Provider, GiataId.ToString(), commonSearch.dtSuplierMarkup, commonSearch.dtMarkupDetails, ref AgentPercentage, ref AgentMarkupAmount, ref blnIsShowSupplier, ref isPerce, commonSearch.AgentId, RatingHotel, SearchParam.CityID, SearchParam.CountryID.ToString(), SearchParam.CheckInDate);


                                if (HBResponse.hotels.hotels[h].rooms != null)
                                {
                                    if (HBResponse.hotels.hotels[h].rooms.Length < 1)
                                        return options;
                                    else
                                    {
                                        #region Room List

                                        string Token = Convert.ToString(HBResponse.auditData.token);

                                        // for (int h = 0; h <= dsHotelBeds.Tables["hotels"].Rows.Count - 1; h++)
                                        {
                                            string Hotelcode = Convert.ToString(HBResponse.hotels.hotels[h].code);
                                            string Hotelname = Convert.ToString(HBResponse.hotels.hotels[h].name);
                                        

                                            for (int i = 0; i < HBResponse.hotels.hotels[h].rooms.Length; i++)
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


                                                string code = Convert.ToString(HBResponse.hotels.hotels[h].rooms[i].code);
                                                string roomtyprname = Convert.ToString(HBResponse.hotels.hotels[h].rooms[i].name);
                                                for (int j = 0; j < HBResponse.hotels.hotels[h].rooms[i].rates.Length; j++)
                                                {
                                                    string EssentialInformation = "";
                                                    string rateCommentsId = "";

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


                                                    string Ratekey = Convert.ToString(HBResponse.hotels.hotels[h].rooms[i].rates[j].rateKey);
                                                    string Rateclass = Convert.ToString(HBResponse.hotels.hotels[h].rooms[i].rates[j].rateClass);
                                                    RateType = Convert.ToString(HBResponse.hotels.hotels[h].rooms[i].rates[j].rateType);

                                                    #region  New 100325
                                                    if (HBResponse.hotels.hotels[h].rooms[i].rates[j].rateType != "BOOKABLE")
                                                    {
                                                        EssentialInformation = "N/A";
                                                    }
                                                    else
                                                    {
                                                        if (HBResponse.hotels.hotels[h].rooms[i].rates[j].rateComments != null)
                                                        {
                                                            Essen = HBResponse.hotels.hotels[h].rooms[i].rates[j].rateComments;
                                                            EssentialInformation = Essen;
                                                        }
                                                        else
                                                        {
                                                            EssentialInformation = " ";
                                                        }
                                                    }

                                                    if (HBResponse.hotels.hotels[h].rooms[i].rates[j].promotions != null)
                                                    {
                                                        string Promotion = "";
                                                        string PromotionRemark = "";
                                                        if (HBResponse.hotels.hotels[h].rooms[i].rates[j].promotions.Length > 0)
                                                        {
                                                            Promotion = HBResponse.hotels.hotels[h].rooms[i].rates[j].promotions[0].name;
                                                            PromotionRemark = HBResponse.hotels.hotels[h].rooms[i].rates[j].promotions[0].remark;
                                                            EssentialInformation = Essen + "<br><br> <b>Promotions : </b>" + Promotion + "<br>" + PromotionRemark + "<br>";
                                                        }
                                                    }
                                                    #endregion

                                                    double Net = Convert.ToDouble(HBResponse.hotels.hotels[h].rooms[i].rates[j].net);
                                                    Net = Net * CurrencyFactor;
                                                    if (HBResponse.hotels.hotels[h].rooms[i].rates[j].rateCommentsId != null)
                                                    {
                                                        rateCommentsId = Convert.ToString(HBResponse.hotels.hotels[h].rooms[i].rates[j].rateCommentsId);
                                                    }
                                                    string packaging = Convert.ToString(HBResponse.hotels.hotels[h].rooms[i].rates[j].packaging);
                                                    string boardCode = Convert.ToString(HBResponse.hotels.hotels[h].rooms[i].rates[j].boardCode);

                                                    string boardName = Convert.ToString(HBResponse.hotels.hotels[h].rooms[i].rates[j].boardName);

                                                    int Adult = Convert.ToInt32(HBResponse.hotels.hotels[h].rooms[i].rates[j].adults);
                                                    int children = Convert.ToInt32(HBResponse.hotels.hotels[h].rooms[i].rates[j].children);
                                                    string TotalCount = Convert.ToString(Adult + "," + children);


                                                    string childrenAges = Convert.ToString(HBResponse.hotels.hotels[h].rooms[i].rates[j].childrenAges);
                                                    string ResponseChild1Age = "0";
                                                    string ResponseChild2Age = "0";
                                                    if (childrenAges != null)
                                                    {
                                                        if (childrenAges.Contains(","))
                                                        {
                                                            string[] childrenAgesCount = null;
                                                            childrenAgesCount = childrenAges.Split(new String[] { "," }, StringSplitOptions.None);

                                                            ResponseChild1Age = childrenAgesCount[0];
                                                            ResponseChild2Age = childrenAgesCount[1];
                                                        }
                                                        else
                                                            ResponseChild1Age = childrenAges;
                                                    }

                                                    for (int o = 0; o < SearchParam.Rooms.Count; o++)
                                                    {
                                                        int SearchAdult = Convert.ToInt32(SearchParam.Rooms[o].NoofAdults);
                                                        int SearchChild = Convert.ToInt32(SearchParam.Rooms[o].NoofChild);
                                                        string SearchTotalCount = Convert.ToString(SearchAdult + "," + SearchChild);

                                                        string ChildAge1 = Convert.ToString(SearchParam.Rooms[o].Child1Age);
                                                        string ChildAge2 = Convert.ToString(SearchParam.Rooms[o].Child2Age);

                                                        if (SearchAdult == Adult && SearchChild == children && ChildAge1 == ResponseChild1Age && ChildAge2 == ResponseChild2Age)
                                                        {
                                                            #region Added For Cancellation Policy

                                                            DateTime FinalFromDate = DateTime.Now;

                                                            if (RateType.ToLower() == "bookable" && HBResponse.hotels.hotels[h].rooms[i].rates[j].cancellationPolicies != null && HBResponse.hotels.hotels[h].rooms[i].rates[j].cancellationPolicies.Length > 0)
                                                            {
                                                                for (int l = 0; l < HBResponse.hotels.hotels[h].rooms[i].rates[j].cancellationPolicies.Length; l++)
                                                                {
                                                                    double Amount = Convert.ToDouble(HBResponse.hotels.hotels[h].rooms[i].rates[j].cancellationPolicies[l].amount);
                                                                    DateTime FromDate = Convert.ToDateTime(HBResponse.hotels.hotels[h].rooms[i].rates[j].cancellationPolicies[l].from);

                                                                    if (l == 0)
                                                                    {
                                                                        FinalFromDate = FromDate;
                                                                    }
                                                                    else
                                                                    {
                                                                        if (FinalFromDate > FromDate)
                                                                        {
                                                                            FinalFromDate = FromDate;
                                                                        }
                                                                    }
                                                                }
                                                            }


                                                            #endregion

                                                            string RoomTypeId = Convert.ToString(10 + "$$$" + Ratekey + "$$$" + rateCommentsId + "$$$" + roomtyprname + "$$$" + Guid.NewGuid().ToString());
                                                            #region Rooms
                                                            DataRow drHotelbedsv1Room = DtHotelBedsV1_HotelRoom.NewRow();
                                                            drHotelbedsv1Room["OwnSystemHotelId"] = OwnsystemHotelId;
                                                            drHotelbedsv1Room["RoomTypeName"] = roomtyprname;
                                                            drHotelbedsv1Room["RoomTypeCode"] = ""; //RoomTypeId;
                                                            drHotelbedsv1Room["MealId"] = boardCode;
                                                            drHotelbedsv1Room["MealName"] = boardCode;
                                                            drHotelbedsv1Room["ContractID"] = "0";
                                                            drHotelbedsv1Room["BoardType"] = boardCode;
                                                            drHotelbedsv1Room["RoomTypeId"] = RoomTypeId;
                                                            drHotelbedsv1Room["AdultsOccupancy"] = SearchAdult;
                                                            drHotelbedsv1Room["ChildOccupancy"] = SearchChild;
                                                            drHotelbedsv1Room["RoomNo"] = o + 1;
                                                            drHotelbedsv1Room["Type"] = RateType;
                                                            drHotelbedsv1Room["UniqueNo"] = Hotelcode;
                                                            drHotelbedsv1Room["OfferDescription"] = "";
                                                            drHotelbedsv1Room["OfferCode"] = "";
                                                            drHotelbedsv1Room["EssentialInformation"] = EssentialInformation;
                                                            drHotelbedsv1Room["SupplierCurrency"] = currency;
                                                            drHotelbedsv1Room["FinalSellingPrice"] = Net;
                                                            drHotelbedsv1Room["FinalFromDate"] = FinalFromDate; //Added by Jay
                                                            DtHotelBedsV1_HotelRoom.Rows.Add(drHotelbedsv1Room);
                                                            #endregion

                                                        }
                                                    }
                                                }
                                            }
                                        }
                                        #endregion
                                    }
                                }

                                #region Room Combination Logic

                                DataTable dtFinalRoom = DtHotelBedsV1_HotelRoom.Clone();

                                var Meal = DtHotelBedsV1_HotelRoom.DefaultView.ToTable(true, "MealName");
                                var Room = DtHotelBedsV1_HotelRoom.DefaultView.ToTable(true, "RoomNo");
                               
                                for (int a = 0; a < Meal.Rows.Count; a++)
                                {

                                    DataView view = DtHotelBedsV1_HotelRoom.DefaultView;
                                    view.Sort = "RoomNo ASC";
                                    DataTable sortedDate = view.ToTable();
                                    DataRow[] drMeal = sortedDate.Select("MealName='" + Meal.Rows[a]["MealName"] + "'");
                                    var ResRoom = drMeal.Select(x => x.Field<string>("RoomNo")).Distinct();

                                    if (ResRoom.Count() == Room.Rows.Count)
                                    {

                                        //DataRow[] drRoom1 = sortedDate.Select("MealName='" + Meal.Rows[a]["MealName"] + "'and RoomNo=1" + "and UniqueNo='" + Hotel_ID + "'");//New added 5-1-24
                                        DataRow[] drRoom1 = sortedDate.Select("MealName='" + Meal.Rows[a]["MealName"] + "'and RoomNo=1");//New added 5-1-24
                                        //DataRow[] drOther = sortedDate.Select("MealName='" + Meal.Rows[a]["MealName"] + "'and RoomNo<>1" + "and UniqueNo='" + Hotel_ID + "'");//New added 5-1-24
                                        DataRow[] drOther = sortedDate.Select("MealName='" + Meal.Rows[a]["MealName"] + "'and RoomNo<>1");//New added 5-1-24
                                        //DataRow[] drRoom1 = sortedDate.Select("MealName='" + Meal.Rows[a]["MealName"] + "'and RoomNo=1");
                                        //DataRow[] drOther = sortedDate.Select("MealName='" + Meal.Rows[a]["MealName"] + "'and RoomNo<>1");
                                        for (int O = 0; O < drRoom1.Count(); O++)
                                        {
                                            string RoomName = Convert.ToString(drRoom1[O]["RoomTypeName"]);
                                            double RoomPrice = Convert.ToDouble(drRoom1[O]["FinalSellingPrice"]);
                                            string RoomMeal = Convert.ToString(drRoom1[O]["MealName"]);
                                            string RoomTypeID = Convert.ToString(drRoom1[O]["RoomTypeId"]);
                                            string HotelId = Convert.ToString(drRoom1[O]["UniqueNo"]);
                                            string SupplierCurrency = Convert.ToString(drRoom1[O]["SupplierCurrency"]);
                                            int RMNO = 1;
                                            int ADTOCC = Convert.ToInt32(drRoom1[O]["AdultsOccupancy"]);
                                            int CLDOCC = Convert.ToInt32(drRoom1[O]["ChildOccupancy"]);
                                            //string[] strRoomTypeId = null;
                                            List<string> strRoomTypeId = new List<string>();

                                            strRoomTypeId.Add(RoomTypeID);

                                            #region Added For Policy by Jay

                                            List<DateTime> strRoomPolicy = new List<DateTime>();
                                            DateTime FinalFromDate = Convert.ToDateTime(drRoom1[O]["FinalFromDate"]);
                                            strRoomPolicy.Add(FinalFromDate);
                                            #endregion

                                            string EssentialInfo = Convert.ToString(drRoom1[O]["EssentialInformation"]);


                                            //DataRow drRoom = dtTemp.NewRow();
                                            for (int b = 0; b < drOther.Count(); b++)
                                            {
                                                if (Convert.ToInt32(drOther[b]["RoomNo"]) != RMNO && !strRoomTypeId.Contains(Convert.ToString(drOther[b]["RoomTypeId"])))
                                                {
                                                    strRoomTypeId.Add(Convert.ToString(drOther[b]["RoomTypeId"]));
                                                    RMNO = Convert.ToInt32(drOther[b]["RoomNo"]);
                                                    RoomName = RoomName + "$$$" + drOther[b]["RoomTypeName"];
                                                    RoomPrice += Convert.ToDouble(drOther[b]["FinalSellingPrice"]);
                                                    RoomTypeID = RoomTypeID + "##$$" + drOther[b]["RoomTypeId"];
                                                    ADTOCC += Convert.ToInt32(drOther[b]["AdultsOccupancy"]);
                                                    CLDOCC += Convert.ToInt32(drOther[b]["ChildOccupancy"]);
                                               
                                                    #region Added For Policy by Jay                                                                                                    
                                                    strRoomPolicy.Add(Convert.ToDateTime(drOther[b]["FinalFromDate"]));
                                                    #endregion

                                                    EssentialInfo = EssentialInfo + "<br/>" + Convert.ToString(drOther[b]["EssentialInformation"]);

                                                }
                                            }
                                           
                                            #region "Logic For Room Name "
                                            string[] spitid = RoomName.Split(new String[] { "$$$" }, StringSplitOptions.None);
                                            if (SearchParam.Rooms.Count != spitid.Length)
                                            {
                                                continue;
                                            }

                                            List<Room_EgyptExpress> roomlist = new List<Room_EgyptExpress>();
                                            for (int dr = 0; dr < spitid.Length; dr++)
                                            {
                                                Room_EgyptExpress obj = new Room_EgyptExpress();
                                                obj.RoomCount = 1;
                                                obj.RoomName = Convert.ToString(spitid[dr]);
                                                obj.status = true;
                                                roomlist.Add(obj);
                                            }
                                            string RoomNameMain = "";// Convert.ToString(drHotelRoom[0]["Name"]);

                                            for (int p = 0; p < roomlist.Count; p++)
                                            {
                                                string RoomNme = roomlist[p].RoomName;
                                                RoomNameMain = RoomNameMain + "(" + (p + 1) + ")" + RoomNme + " ";
                                            }
                                          
                                            #endregion

                                            #region Find Cheapest date form Policy
                                            DateTime MinDateTime = strRoomPolicy.Min();
                                            #endregion

                                            RoomTypeID = RoomTypeID.Replace("'", "");
                                            #region RateBreakUp
                                            for (int n = 0; n < NoOfNight; n++)
                                            {
                                                DateTime Day = DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null);
                                                //Day = Day.AddDays(n);

                                                string Days = Convert.ToString(Day);
                                                string rateWeekColumn = Convert.ToString(Day.Date.AddDays(n).DayOfWeek).Substring(0, 3);

                                                #region Vat
                                                Double VatOnRateBrackUp = 0;
                                                double VatOnPerNight = 0;
                                                double PriceRoom = _markup.ProcessHotelRateBreakupPrice_WithMarkup(1, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), RoomPrice / SearchParam.NoofNights, commonSearch.B2CMarkup, IsParentId, isPerce, AgentPercentage, AgentMarkupAmount, SubAgentMarkup, ref VatOnPerNight);
                                                #endregion
                                                DataRow drAdonis_RateBreackUp = DtRateBreackUp.NewRow();
                                                drAdonis_RateBreackUp["SupplierName"] = "HotelBedsV1";
                                                drAdonis_RateBreackUp["OwnsystemHotelId"] = OwnsystemHotelId;
                                                drAdonis_RateBreackUp["ContractID"] = HotelId;
                                                drAdonis_RateBreackUp["RoomTypeName"] = RoomNameMain;
                                                drAdonis_RateBreackUp["RoomTypeId"] = RoomTypeID;
                                                drAdonis_RateBreackUp["MealId"] = RoomMeal;
                                                drAdonis_RateBreackUp["Meal"] = RoomMeal;
                                                drAdonis_RateBreackUp["DateOfWeek"] = rateWeekColumn;
                                                Day = Day.Date.AddDays(n);
                                                drAdonis_RateBreackUp["Date"] = Day;
                                                drAdonis_RateBreackUp["Rate"] = PriceRoom;
                                                drAdonis_RateBreackUp["RateType"] = "A Rate";
                                                drAdonis_RateBreackUp["RoomNo"] = 1;
                                                drAdonis_RateBreackUp["RoomSequenceNo"] = OwnsystemHotelId;
                                                drAdonis_RateBreackUp["BookingStatus"] = "Available";
                                                drAdonis_RateBreackUp["SupplierCurrency"] = SupplierCurrency;
                                                drAdonis_RateBreackUp["BuyingPrice"] = PriceRoom / CurrencyFactor;
                                                DtRateBreackUp.Rows.Add(drAdonis_RateBreackUp);

                                            }
                                            #endregion

                                            #region Rooms
                                            DataRow drAdonisRoom = dtHotelBedsV1Room.NewRow();
                                            drAdonisRoom["OwnSystemHotelId"] = OwnsystemHotelId;
                                            drAdonisRoom["RoomTypeName"] = RoomNameMain;
                                            drAdonisRoom["RoomTypeCode"] = "";
                                            drAdonisRoom["MealId"] = RoomMeal;
                                            drAdonisRoom["MealName"] = RoomMeal;
                                            drAdonisRoom["ContractID"] = "0";
                                            drAdonisRoom["BoardType"] = RoomMeal;
                                            drAdonisRoom["RoomTypeId"] = RoomTypeID;
                                            drAdonisRoom["AdultsOccupancy"] = ADTOCC;
                                            drAdonisRoom["ChildOccupancy"] = CLDOCC;
                                            drAdonisRoom["MaxExtraBed"] = "0";
                                            drAdonisRoom["RoomNo"] = 1;
                                            drAdonisRoom["RoomSequenceNo"] = HotelId;
                                            drAdonisRoom["Type"] = RoomTypeID;
                                            drAdonisRoom["BookingStatus"] = "Available";
                                            drAdonisRoom["UniqueNo"] = HotelId;
                                            drAdonisRoom["OfferDescription"] = "";
                                            drAdonisRoom["OfferCode"] = "";
                                            drAdonisRoom["EssentialInformation"] = EssentialInfo;
                                            double ItemPrice = Convert.ToDouble(RoomPrice);
                                            _markup.ProcessHotelRoom_WithMarkup(CurrencyFactor, currency, true, true, SearchParam.NoofNights, SearchParam.CityID, SearchParam.CountryID.ToString(), SearchParam.CheckInDate, SearchParam.CheckOutDate, ref ItemPrice, ref drAdonisRoom, isPerce, AgentPercentage, AgentMarkupAmount, commonSearch.SubAgentMarkup, commonSearch.B2CMarkup, commonSearch.IsParentId);
                                            drAdonisRoom["SupplierName"] = Item.Supplier;
                                            dtHotelBedsV1Room.Rows.Add(drAdonisRoom);

                                            #endregion


                                            #region Cancellation Policy
                                            double cancellationAmount = RoomPrice;// Convert.ToDouble(dsHotelBeds[q].Tables["cancellationPolicies"].DefaultView[0]["amount"]);
                                            double VatOnPolicy = 0;
                                            #region Vat

                                            //Int32 hotelRating = 0;
                                            //hotelRating = Convert.ToInt32(dsHotel.Tables[0].Rows[0]["Rating"]);

                                            double MainAgentCharge = cancellationAmount;
                                            double CompanyChargeAmount = cancellationAmount;
                                            cancellationAmount = _markup.ProcessHotelCancelationPrice_WithMarkup(ref MainAgentCharge, SearchParam.NoofNights, SearchParam.CityID, SearchParam.CountryID.ToString(), cancellationAmount, isPerce, commonSearch.B2CMarkup, AgentPercentage, AgentMarkupAmount, IsParentId, SubAgentMarkup, ref VatOnPolicy);
                                            #endregion


                                            DateTime ToDate = MinDateTime.Date;
                                            DateTime FromDate = DateTime.Now.Date;
                                            if (ToDate.AddDays(CutOffDays).Date.AddDays(-1).Date > DateTime.Now.Date)
                                                ToDate = ToDate.AddDays(CutOffDays).Date;
                                            else
                                                ToDate = DateTime.Now.Date;

                                            if (ToDate > FromDate)
                                            {
                                                double zero = 0;

                                                dtHotelBedsV1RoomCacelationPolicy.Rows.Add(_commonMethod.setCancellationPolicyRow(ref zero, dtHotelBedsV1RoomCacelationPolicy.NewRow(), 1, "USD", Provider, Convert.ToString(OwnsystemHotelId), Convert.ToString(1), RoomTypeID, "", Convert.ToString(RoomMeal), DateTime.Now.Date, ToDate.AddDays(-1).AddHours(23).AddMinutes(59).AddSeconds(59), 0, 0, RoomMeal, 0, "", "", "", EssentialInfo, "", ""));
                                                dtHotelBedsV1RoomCacelationPolicy.AcceptChanges();

                                            }
                                            if (ToDate < FromDate)
                                                ToDate = DateTime.Now.Date;

                                            dtHotelBedsV1RoomCacelationPolicy.Rows.Add(_commonMethod.setCancellationPolicyRow(ref CompanyChargeAmount, dtHotelBedsV1RoomCacelationPolicy.NewRow(), 1, "USD", Provider, Convert.ToString(OwnsystemHotelId), Convert.ToString(1), RoomTypeID, "", Convert.ToString(RoomMeal), ToDate, DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null), MainAgentCharge, cancellationAmount, RoomMeal, 0, "", "", "", EssentialInfo, "", ""));
                                            dtHotelBedsV1RoomCacelationPolicy.AcceptChanges();

                                            #endregion

                                        }
                                    }
                                    else
                                        continue;
                                }
                                #endregion

                                Options ops = (await _supplierRes.GetCommonRoomForSuppliers(GiataId, commonSearch.tokenData, commonSearch.advancedOptions, MinPrice, drXmlHotel, dtHotelBedsV1Room, dtHotelBedsV1RoomCacelationPolicy, DtRateBreackUp, SearchParam));
                                if (ops != null)
                                    options.Add(ops);

                            }
                            #endregion
                            #endregion
                        }

                    }


                    #endregion
                }
            }
            catch (Exception e)
            {
                Item.ResponseType = "NoCount";
                await _exception.Error(e);
            }
            finally
            {
                #region ResProcessTimer
                try
                {
                    if (SearchParam != null && ResProccessTimerLogActive == "1" && timer_room_HotelbedsV1.ElapsedMilliseconds > Convert.ToInt32(ResProccessTimeValueforSaveLog))
                    {
                        timer_room_HotelbedsV1.Stop();
                        SearchParam.SupplierTime += ",|Finish:HotelbedsV1 Process Response  " + "|Time :" + timer_room_HotelbedsV1.Elapsed.ToString();
                        _= Task.Run(() => _commonMethod.FireAway_SQL_ResPorcessTimeLog("", timer_room_HotelbedsV1.Elapsed.ToString(), "HotelbedsV1", SearchParam.SearchKey, 1, SearchParam.AgentID));
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

        public async Task<ResponseRecheck> HotelBedsV1_cancellation_For_Recheck_OnlyRead_EssInfo(string roomtypeid, DataTable dtSuplierMarkup, string provider, string CompanyBuyPrice, int AgentId,string SearchKey)
        {

            ResponseRecheck res = new ResponseRecheck();

            string Essen = " ";
            string ResponseData = "";
            string Request = "";
            string time = "";
            string Error = "";
            string ISBooking = "NotAvailable";
            try
            {
                DataTable dtHotelBedsV1RoomCacelationPolicy = _commonMethod.GenerateCancellationDataTableNewV2();
                //int CutOffDays = XMLHotelBookingCommon.CutOffDays;
                //CutOffDays = GetCutOffDays(dtSuplierMarkup, provider);

                DataRow[] drSuplierMarkup = (dtSuplierMarkup.Select("SupplierName='" + provider + "'"));
                int SupplierId = Convert.ToInt32(drSuplierMarkup[0]["SupplierID"]);

                string[] roomCode = roomtypeid.Split(new String[] { "$$$" }, StringSplitOptions.None);
                string rateKey = roomCode[1];
                Request = HotelsBedV1PolicyRequest(rateKey);


                System.Diagnostics.Stopwatch timerroomprocess = new System.Diagnostics.Stopwatch();
                timerroomprocess.Start();

                //ResponseData = GetHotelBedsSupplierResponseOlny(Convert.ToString(drSuplierMarkup[0]["WebSiteURL"]) + "/1.0/checkrates", Convert.ToString(drSuplierMarkup[0]["UserName"]), Convert.ToString(drSuplierMarkup[0]["Password"]), 5000, Request, "", ref Error);
                ResponseDto responseDto = await GetHotelBedsSupplierResponseOlny(Convert.ToString(drSuplierMarkup[0]["WebSiteURL"]) + _supplierConfig.Hotelbeds.checkrates, Convert.ToString(drSuplierMarkup[0]["UserName"]), Convert.ToString(drSuplierMarkup[0]["Password"]), 5000, Request, "");
                //Common.ThirdPartyGetPolicyHistory(Convert.ToString(roomtypeid), Convert.ToString(ownsystemid), Request, Convert.ToString(ResponseData), "After Get Policy", provider, "Response", "-", "");

                ResponseData = responseDto.Response;
                Error = responseDto.Error;

                timerroomprocess.Stop();
                time = timerroomprocess.Elapsed.ToString();

                if (ResponseData.ToLower().Contains("error") || ResponseData.Trim() == "" || Error != "")
                {
                    _commonMethod.ThirdPartyGetPolicyHistory(Convert.ToString(roomtypeid), Convert.ToString(AgentId), Request, Convert.ToString(ResponseData), "After Get Policy R1", provider, "Response", "-", "");
                    try
                    {
                        string responsetype = "GeneralError";
                        //int AgentId = Convert.ToInt32(Common.getSession("UserId") == "" ? "0" : Common.getSession("UserId"));
                        string abc = await _commonMethod.SupplierPerformanceReportForRecheckOnly(provider, "PreBook", AgentId, time, responsetype, Request, ResponseData, Error);
                    }
                    catch (Exception e)
                    {
                        _ = _exception.ErrorNew(e, _LogAPI.Recheck, SearchKey);
                    }

                    res.Response = "";
                    res.ISBooking = ISBooking;

                    return res;
                }
                string ConvertXml = Newtonsoft.Json.JsonConvert.DeserializeXmlNode("{\"Row\":" + ResponseData + "}", "root").InnerXml;
                DataSet dsHotelBeds = new DataSet();
                dsHotelBeds = _commonMethod.GetDataSetFromXMLString(ConvertXml);
                //if (dsHotelBeds.Tables["cancellationPolicies"] != null)
                //{
                //    if (!dsHotelBeds.Tables["cancellationPolicies"].Columns.Contains("Date"))
                //        dsHotelBeds.Tables["cancellationPolicies"].Columns.Add("Date", typeof(DateTime));

                //    for (int dt = 0; dt < dsHotelBeds.Tables["cancellationPolicies"].Rows.Count; dt++)
                //    {
                //        dsHotelBeds.Tables["cancellationPolicies"].Rows[dt]["Date"] = Convert.ToDateTime(dsHotelBeds.Tables["cancellationPolicies"].Rows[dt]["from"]).Date;
                //    }
                //}
                for (int i = 0; i < dsHotelBeds.Tables["rates"].Rows.Count; i++)
                {
                    //int rates_id = Convert.ToInt32(dsHotelBeds.Tables["rates"].Rows[i]["rates_Id"]);
                    //string rateKeyRoom = Convert.ToString(dsHotelBeds.Tables["rates"].Rows[i]["rateKey"]);
                    string rateType = Convert.ToString(dsHotelBeds.Tables["rates"].Rows[i]["rateType"]);
                    //int Adult = Convert.ToInt32(dsHotelBeds.Tables["rates"].Rows[i]["adults"]);
                    //int Child = Convert.ToInt32(dsHotelBeds.Tables["rates"].Rows[i]["children"]);
                    //int RoomNo = Convert.ToInt32(roomno); //Common.AdultChildwiseRoomNo(Adult, Child, SearchPara);
                    string RateKey = "";
                    if (rateType.ToLower() == "bookable")
                    {
                        //string Mealcode = Convert.ToString(dsHotelBeds.Tables["rates"].Rows[i]["boardCode"]);
                        //double itemprice = Convert.ToDouble(dsHotelBeds.Tables["rates"].Rows[i]["net"]);

                        if (dsHotelBeds.Tables["rates"].Columns.Contains("rateComments") == true)
                        {
                            Essen = Convert.ToString(dsHotelBeds.Tables["rates"].Rows[i]["rateComments"]);
                        }

                        string[] Array = Convert.ToString(roomtypeid).Split(new String[] { "$$$" }, StringSplitOptions.None);
                        string SearchRoomName = Array[2];
                        RateKey = Array[1];
                        string RoomName = "";
                        bool ISBookable = true;
                        double itemprice = 0;
                        string XYZ = "";
                        //if (ResponseData.Contains("rooms"))
                        //{
                        //    for (int j = 0; j < dsHotelBeds.Tables["rooms"].Rows.Count; j++)
                        //    {
                        //        RoomName = Convert.ToString(dsHotelBeds.Tables["rooms"].Rows[j]["name"]);
                        //        RoomName = RoomName.Replace("'", "");
                        //        break;
                        //    }
                        //}
                        if (RateKey == Convert.ToString(dsHotelBeds.Tables["rates"].Rows[0]["rateKey"]))// (SearchRoomName.ToLower() == RoomName.ToLower())
                        {
                            for (int j = 0; j < dsHotelBeds.Tables["rates"].Rows.Count; j++)
                            {
                                itemprice = Convert.ToDouble(dsHotelBeds.Tables["rates"].Rows[j]["net"]);
                                break;
                            }

                            if ((Convert.ToDouble((itemprice)).TruncateDecimalPlaces(0)) > (Convert.ToDouble(CompanyBuyPrice)).TruncateDecimalPlaces(0))
                            {
                                XYZ = "CompanyBuyingPrices=" + CompanyBuyPrice + " ResPrice=" + itemprice + "";
                                ISBookable = false;
                            }
                        }
                        else
                        {
                            ISBookable = false;
                        }

                        string responsetype = "";
                        if (ISBookable)
                        {
                            responsetype = "Success";
                            ISBooking = "Available";
                        }
                        else
                        {
                            responsetype = "GeneralError";
                        }
                        try
                        {

                            //int AgentId = Convert.ToInt32(Common.getSession("UserId") == "" ? "0" : Common.getSession("UserId"));
                            string abc = await _commonMethod.SupplierPerformanceReportForRecheckOnly(provider, "PreBook", AgentId, time, responsetype, Request, XYZ + ", Response: " + ResponseData, Error);
                        }
                        catch (Exception e)
                        {
                            _ = _exception.ErrorNew(e, _LogAPI.Recheck, SearchKey);
                        }


                        #region comment
                        //if (dsHotelBeds.Tables["cancellationPolicies"] != null)
                        //{
                        //    bool chechk = false;

                        //    dsHotelBeds.Tables["cancellationPolicies"].DefaultView.RowFilter = "rates_Id='" + rates_id + "'";
                        //    dsHotelBeds.Tables["cancellationPolicies"].DefaultView.Sort = "Date Asc";

                        //    if (dsHotelBeds.Tables["cancellationPolicies"].DefaultView.Count > 0)
                        //    {
                        //        double cancellationAmount = itemprice;// Convert.ToDouble(dsHotelBeds[q].Tables["cancellationPolicies"].DefaultView[0]["amount"]);
                        //        double VatOnPolicy = 0;
                        //        #region Vat
                        //        Boolean isPerce = true;
                        //        double AgentCompanyPercentage = Convert.ToDouble(SearchPara.AgentCompanyPercentage);
                        //        bool IsB2C = Convert.ToBoolean(Convert.ToByte(SearchPara.IsB2BOrB2C));
                        //        double B2CMarkup = (IsB2C == false ? AgentCompanyPercentage : XMLHotelBookingCommon.B2CMarkup);
                        //        double AgentPercentage = 0.0;
                        //        double AgentMarkupAmount = 0.0;
                        //        double SubAgentMarkup = 0;
                        //        int IsParentId = 1;
                        //        Boolean blnIsShowSupplier = false;
                        //        //Int32 hotelRating = 0;
                        //        //hotelRating = Convert.ToInt32(dsHotel.Tables[0].Rows[0]["Rating"]);
                        //        getAgetMarkupSetting(provider, GiataId, dtSuplierMarkup, dtMarkupDetails, ref AgentPercentage, ref AgentMarkupAmount, ref blnIsShowSupplier, ref isPerce, AgentId, hotelRating, SearchPara.CityID, SearchPara.CountryID.ToString(), SearchPara.CheckInDate);
                        //        if (dtAgentDetail != null)
                        //        {
                        //            IsParentId = Convert.ToInt32(dtAgentDetail.Rows[0]["IsParentId"]);
                        //            SubAgentMarkup = Convert.ToDouble(dtAgentDetail.Rows[0]["B2CMarkupPer"]);
                        //        }
                        //        double MainAgentCharge = cancellationAmount;
                        //        double CompanyChargeAmount = cancellationAmount;
                        //        cancellationAmount = ProcessHotelCancelationPrice_WithMarkup(ref MainAgentCharge, SearchPara.NoofNights, SearchPara.CityID, SearchPara.CountryID.ToString(), cancellationAmount, isPerce, B2CMarkup, AgentPercentage, AgentMarkupAmount, IsParentId, SubAgentMarkup, ref VatOnPolicy);
                        //        #endregion
                        //        DateTime ToDate = Convert.ToDateTime(dsHotelBeds.Tables["cancellationPolicies"].DefaultView[0]["from"]).Date;
                        //        DateTime FromDate = DateTime.Now.Date;
                        //        if (ToDate.AddDays(CutOffDays).Date.AddDays(-1).Date > DateTime.Now.Date)
                        //            ToDate = ToDate.AddDays(CutOffDays).Date;
                        //        else
                        //            ToDate = DateTime.Now.Date;

                        //        if (ToDate > FromDate && chechk == false)
                        //        {
                        //            double zero = 0;

                        //            dtHotelBedsV1RoomCacelationPolicy.Rows.Add(setCancellationPolicyRow(ref zero, dtHotelBedsV1RoomCacelationPolicy.NewRow(), 1, "USD", provider, Convert.ToString(ownsystemid), Convert.ToString(RoomNo), roomtypeid, "", Convert.ToString(roomtypecode), DateTime.Now.Date, ToDate.AddDays(-1).AddHours(23).AddMinutes(59).AddSeconds(59), 0, 0, "", 0, "", "", "", Essen, "", ""));
                        //            dtHotelBedsV1RoomCacelationPolicy.AcceptChanges();
                        //            chechk = true;
                        //        }
                        //        if (ToDate < FromDate)
                        //            ToDate = DateTime.Now.Date;
                        //        dtHotelBedsV1RoomCacelationPolicy.Rows.Add(setCancellationPolicyRow(ref CompanyChargeAmount, dtHotelBedsV1RoomCacelationPolicy.NewRow(), 1, "USD", provider, Convert.ToString(ownsystemid), Convert.ToString(RoomNo), roomtypeid, "", Convert.ToString(roomtypecode), ToDate, DateTime.ParseExact(SearchPara.CheckInDate, "dd/MM/yyyy", null), MainAgentCharge, cancellationAmount, "", 0, "", "", "", Essen, "", ""));
                        //        dtHotelBedsV1RoomCacelationPolicy.AcceptChanges();
                        //    }
                        //}
                        //dsHotel.Tables[2].Merge(dtHotelBedsV1RoomCacelationPolicy);
                        #endregion
                    }
                    else
                    {
                        //Common.ThirdPartyGetPolicyHistory(Convert.ToString(roomtypeid), Convert.ToString(AgentId), Request, Convert.ToString(ResponseData), "After Get Policy R2", provider, "Response", "-", "");
                        try
                        {
                            string responsetype = "GeneralError";
                            //int AgentId = Convert.ToInt32(Common.getSession("UserId") == "" ? "0" : Common.getSession("UserId"));
                            string abc = await _commonMethod.SupplierPerformanceReportForRecheckOnly(provider, "PreBook", AgentId, time, responsetype, Request, ResponseData, Error);
                        }
                        catch (Exception e)
                        {
                            _ = _exception.ErrorNew(e, _LogAPI.Recheck, SearchKey);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _ = _exception.ErrorNew(ex, _LogAPI.Recheck, SearchKey);

                _commonMethod.ThirdPartyGetPolicyHistory(Convert.ToString(roomtypeid), Convert.ToString(AgentId), Request, Convert.ToString(ex.Message.ToString() + ", Res: " + ResponseData), "After Get Policy R3", provider, "Response", "-", "");
                try
                {
                    string responsetype = "GeneralError";
                    //int AgentId = Convert.ToInt32(Common.getSession("UserId") == "" ? "0" : Common.getSession("UserId"));
                    string abc = await _commonMethod.SupplierPerformanceReportForRecheckOnly(provider, "PreBook", AgentId, time, responsetype, Request, ResponseData, ex.Message.ToString() + ", " + Error);
                }
                catch (Exception e)
                {
                    _ = _exception.ErrorNew(e, _LogAPI.Recheck, SearchKey);
                }

                res.Response = null;
                res.ISBooking = ISBooking;

                return res;
            }

            res.Response = Essen;
            res.ISBooking = ISBooking;

            return res;
        }

        public string HotelsBedV1PolicyRequest(string RateKey)
        {
            try
            {
                StringBuilder HBv1 = new StringBuilder();
                HBv1.Append("{");
                HBv1.Append("\"language\":" + "\"ENG\",");
                HBv1.Append("\"upselling\":" + "\"false\",");
                HBv1.Append("\"rooms\": [{");
                HBv1.Append("\"rateKey\":\"" + RateKey + "\"");
                HBv1.Append("}]");
                HBv1.Append("}");
                string request = HBv1.ToString();
                return request;
            }
            catch (Exception ex)
            {
                return "";
            }
        }

        public async Task<ResponseDto> GetHotelBedsSupplierResponseOlny(string strHost, string Username, string Password, Int32 TimeOut, string strxmlRQRS, string methodtype,  string Provider = "", HotelSearchParameter para = null, bool DebugMode = false, string Searchkey = "")
        {

            ResponseDto responseDto = new ResponseDto();

            System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
            Provider = "hotelbeds";
            timer.Start();
            string strOutput = "";
            string signature = "";
            string Error = "";
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12; //#25-09-2024
            try
            {
                using (var sha = SHA256.Create())
                {
                    long ts = (long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds / 1000;
                    var computedHash = sha.ComputeHash(Encoding.UTF8.GetBytes(Username + Password + ts));
                    signature = BitConverter.ToString(computedHash).Replace("-", "");
                }
                HttpWebRequest Request = (HttpWebRequest)WebRequest.Create(strHost);
                ServicePointManager.DefaultConnectionLimit = 1000;
                // Request.Timeout = TimeOut;
                Request.Headers.Add("X-Signature", signature);
                Request.Headers.Add("Api-Key", Username);
                Request.ContentType = "application/json";
                Request.Accept = "application/json";
                if (methodtype.ToLower() == "delete")
                    Request.Method = "DELETE";
                else
                    Request.Method = "POST";
                Request.Headers.Add("Accept-Encoding", "gzip");
                Request.Timeout = TimeOut;

                Stream newStream = null;
                byte[] data = Encoding.ASCII.GetBytes(strxmlRQRS);
                Request.ContentLength = data.Length;
                newStream = Request.GetRequestStream();
                newStream.Write(data, 0, data.Length);
                string respXml; /* will contain response from api in string form */

                try
                {
                    HttpWebResponse Response = (HttpWebResponse)await Request.GetResponseAsync();
                    Stream st = Response.GetResponseStream();

                    if (Response.ContentEncoding.ToLower().Contains("gzip"))
                        st = new System.IO.Compression.GZipStream(st, System.IO.Compression.CompressionMode.Decompress);
                    else if (Response.ContentEncoding.ToLower().Contains("deflate"))
                        st = new System.IO.Compression.DeflateStream(st, System.IO.Compression.CompressionMode.Decompress);
                    StreamReader stIn = new StreamReader(st, Encoding.Default);
                    respXml = await stIn.ReadToEndAsync();
                    strOutput = respXml;
                    stIn.Close();

                    responseDto.Response = strOutput;
                    responseDto.Error = Error;

                }
                catch (WebException webExcp)
                {
                    HttpWebResponse httpResponse = (HttpWebResponse)webExcp.Response;
                    string result = httpResponse.StatusDescription.ToString();
                    string result1 = new StreamReader(webExcp.Response.GetResponseStream()).ReadToEnd();
                    string StatusCode = "";
                    try
                    {
                        StatusCode = httpResponse.StatusCode.ToString();

                    }
                    catch (Exception)
                    {

                    }
                    _commonMethod.XMLRequestResponseTrackError(strxmlRQRS, "StatusCode =" + StatusCode + " # " + result1, Provider, "Proper, Searchkey=" + Searchkey + "");
                    Error = result + " ," + result1;

                    responseDto.Response = "";
                    responseDto.Error = Error;

                    return responseDto;
                }
            }
            catch (Exception ex)
            {
                Error = ex.Message.ToString();
                _exception.Error(ex, Searchkey);

                responseDto.Response = "";
                responseDto.Error = Error;
            }

           
            return responseDto;
        }

        public async Task<CancellationDto> HotelBedsV1_cancellation(string roomtypecode, DataSet dsHotel, string hotelcode, string ownsystemid, string roomtypeid, DataTable dtAgentDetail, DataTable dtSuplierMarkup, DataTable dtSearchConfig, HotelSearchParameter SearchPara, string roomno, string provider, int AgentId, DataTable dtMarkupDetails, string GiataId = "0", int hotelRating = 0,string SearchKey="")
        {
            CancellationDto cancellationDto = new CancellationDto();    
            try
            {
                DataTable dtHotelBedsV1RoomCacelationPolicy = _commonMethod.GenerateCancellationDataTableNewV2();
                int CutOffDays = XMLHotelBookingCommon.CutOffDays;
                CutOffDays = _commonMethod.GetCutOffDays(dtSuplierMarkup, provider);

                DataRow[] drSuplierMarkup = (dtSuplierMarkup.Select("SupplierName='" + provider + "'"));
                int SupplierId = Convert.ToInt32(drSuplierMarkup[0]["SupplierID"]);
                DataRow[] drSearchConfig = (dtSearchConfig.Select("SupplierID='" + SupplierId + "'"));
                string[] roomCode = roomtypeid.Split(new String[] { "$$$" }, StringSplitOptions.None);
                string rateKey = roomCode[1];
                string Request = HotelsBedV1PolicyRequest(rateKey);
                string Error = "";
                //string ResponseData = GetHotelBedsSupplierResponse(Convert.ToString(drSuplierMarkup[0]["WebSiteURL"]) + "/1.0/checkrates", Convert.ToString(drSuplierMarkup[0]["UserName"]), Convert.ToString(drSuplierMarkup[0]["Password"]), 60000, Request, "", ref Error);
                ResponseDto responseDto = await GetHotelBedsSupplierResponse(Convert.ToString(drSuplierMarkup[0]["WebSiteURL"]) + "/1.0/checkrates", Convert.ToString(drSuplierMarkup[0]["UserName"]), Convert.ToString(drSuplierMarkup[0]["Password"]), 60000, Request, "", provider);
                string ResponseData = responseDto.Response;
                Error = responseDto.Error;


                _commonMethod.ThirdPartyGetPolicyHistory(Convert.ToString(roomtypeid), Convert.ToString(ownsystemid), Request, Convert.ToString(ResponseData), "After Get Policy", provider, "Response", "-", "");

                if (ResponseData.ToLower().Contains("error") || ResponseData.Trim() == "" || Error != "")
                {
                    cancellationDto.dsHotelData = dsHotel;
                    cancellationDto.ISBooking = "NotAvailable";

                    return cancellationDto;

                }
                string ConvertXml = Newtonsoft.Json.JsonConvert.DeserializeXmlNode("{\"Row\":" + ResponseData + "}", "root").InnerXml;
                DataSet dsHotelBeds = new DataSet();
                dsHotelBeds = _commonMethod.GetDataSetFromXMLString(ConvertXml);
                if (dsHotelBeds.Tables["cancellationPolicies"] != null)
                {
                    if (!dsHotelBeds.Tables["cancellationPolicies"].Columns.Contains("Date"))
                        dsHotelBeds.Tables["cancellationPolicies"].Columns.Add("Date", typeof(DateTime));

                    for (int dt = 0; dt < dsHotelBeds.Tables["cancellationPolicies"].Rows.Count; dt++)
                    {
                        dsHotelBeds.Tables["cancellationPolicies"].Rows[dt]["Date"] = Convert.ToDateTime(dsHotelBeds.Tables["cancellationPolicies"].Rows[dt]["from"]).Date;
                    }
                }
                for (int i = 0; i < dsHotelBeds.Tables["rates"].Rows.Count; i++)
                {
                    int rates_id = Convert.ToInt32(dsHotelBeds.Tables["rates"].Rows[i]["rates_Id"]);
                    string rateKeyRoom = Convert.ToString(dsHotelBeds.Tables["rates"].Rows[i]["rateKey"]);
                    string rateType = Convert.ToString(dsHotelBeds.Tables["rates"].Rows[i]["rateType"]);
                    int Adult = Convert.ToInt32(dsHotelBeds.Tables["rates"].Rows[i]["adults"]);
                    int Child = Convert.ToInt32(dsHotelBeds.Tables["rates"].Rows[i]["children"]);
                    int RoomNo = Convert.ToInt32(roomno); //Common.AdultChildwiseRoomNo(Adult, Child, SearchPara);

                    if (rateType.ToLower() == "bookable")
                    {
                        //string Mealcode = Convert.ToString(dsHotelBeds.Tables["rates"].Rows[i]["boardCode"]);
                        string Mealcode = Convert.ToString(dsHotelBeds.Tables["rates"].Rows[i]["boardName"]);//added on 20-JUN-2024
                        double itemprice = Convert.ToDouble(dsHotelBeds.Tables["rates"].Rows[i]["net"]);
                        string Essen = " ";
                        if (dsHotelBeds.Tables["rates"].Columns.Contains("rateComments") == true)
                        {
                            Essen = Convert.ToString(dsHotelBeds.Tables["rates"].Rows[i]["rateComments"]);
                        }

                        if (dsHotelBeds.Tables["cancellationPolicies"] != null)
                        {
                            bool chechk = false;

                            dsHotelBeds.Tables["cancellationPolicies"].DefaultView.RowFilter = "rates_Id='" + rates_id + "'";
                            dsHotelBeds.Tables["cancellationPolicies"].DefaultView.Sort = "Date Asc";

                            if (dsHotelBeds.Tables["cancellationPolicies"].DefaultView.Count > 0)
                            {
                                double cancellationAmount = itemprice;// Convert.ToDouble(dsHotelBeds[q].Tables["cancellationPolicies"].DefaultView[0]["amount"]);
                                double VatOnPolicy = 0;
                                #region Vat
                                Boolean isPerce = true;
                                double AgentCompanyPercentage = Convert.ToDouble(SearchPara.AgentCompanyPercentage);
                                bool IsB2C = Convert.ToBoolean(Convert.ToByte(SearchPara.IsB2BOrB2C));
                                double B2CMarkup = (IsB2C == false ? AgentCompanyPercentage : XMLHotelBookingCommon.B2CMarkup);
                                double AgentPercentage = 0.0;
                                double AgentMarkupAmount = 0.0;
                                double SubAgentMarkup = 0;
                                int IsParentId = 1;
                                Boolean blnIsShowSupplier = false;
                                //Int32 hotelRating = 0;
                                //hotelRating = Convert.ToInt32(dsHotel.Tables[0].Rows[0]["Rating"]);
                                _markup.getAgetMarkupSetting(provider, GiataId, dtSuplierMarkup, dtMarkupDetails, ref AgentPercentage, ref AgentMarkupAmount, ref blnIsShowSupplier, ref isPerce, AgentId, hotelRating, SearchPara.CityID, SearchPara.CountryID.ToString(), SearchPara.CheckInDate);
                                if (dtAgentDetail != null)
                                {
                                    IsParentId = Convert.ToInt32(dtAgentDetail.Rows[0]["IsParentId"]);
                                    SubAgentMarkup = Convert.ToDouble(dtAgentDetail.Rows[0]["B2CMarkupPer"]);
                                }
                                double MainAgentCharge = cancellationAmount;
                                double CompanyChargeAmount = cancellationAmount;
                                cancellationAmount = _markup.ProcessHotelCancelationPrice_WithMarkup(ref MainAgentCharge, SearchPara.NoofNights, SearchPara.CityID, SearchPara.CountryID.ToString(), cancellationAmount, isPerce, B2CMarkup, AgentPercentage, AgentMarkupAmount, IsParentId, SubAgentMarkup, ref VatOnPolicy);
                                #endregion
                                DateTime ToDate = Convert.ToDateTime(dsHotelBeds.Tables["cancellationPolicies"].DefaultView[0]["from"]).Date;
                                DateTime FromDate = DateTime.Now.Date;
                                if (ToDate.AddDays(CutOffDays).Date.AddDays(-1).Date > DateTime.Now.Date)
                                    ToDate = ToDate.AddDays(CutOffDays).Date;
                                else
                                    ToDate = DateTime.Now.Date;

                                if (ToDate > FromDate && chechk == false)
                                {
                                    double zero = 0;

                                    dtHotelBedsV1RoomCacelationPolicy.Rows.Add(_commonMethod.setCancellationPolicyRow(ref zero, dtHotelBedsV1RoomCacelationPolicy.NewRow(), 1, "USD", provider, Convert.ToString(ownsystemid), Convert.ToString(RoomNo), roomtypeid, "", Convert.ToString(roomtypecode), DateTime.Now.Date, ToDate.AddDays(-1).AddHours(23).AddMinutes(59).AddSeconds(59), 0, 0, "", 0, "", "", "", Essen, "", ""));
                                    dtHotelBedsV1RoomCacelationPolicy.AcceptChanges();
                                    chechk = true;
                                }
                                if (ToDate < FromDate)
                                    ToDate = DateTime.Now.Date;
                                dtHotelBedsV1RoomCacelationPolicy.Rows.Add(_commonMethod.setCancellationPolicyRow(ref CompanyChargeAmount, dtHotelBedsV1RoomCacelationPolicy.NewRow(), 1, "USD", provider, Convert.ToString(ownsystemid), Convert.ToString(RoomNo), roomtypeid, "", Convert.ToString(roomtypecode), ToDate, DateTime.ParseExact(SearchPara.CheckInDate, "dd/MM/yyyy", null), MainAgentCharge, cancellationAmount, "", 0, "", "", "", Essen, "", ""));
                                dtHotelBedsV1RoomCacelationPolicy.AcceptChanges();
                            }
                        }
                        dsHotel.Tables[2].Merge(dtHotelBedsV1RoomCacelationPolicy);
                    }
                }
            }
            catch (Exception ex)
            {
                _ = _exception.ErrorNew(ex, _LogAPI.Recheck, SearchKey);

                cancellationDto.dsHotelData = null;
                cancellationDto.ISBooking = "NotAvailable";
                return cancellationDto;
            }
            cancellationDto.dsHotelData = dsHotel;
            cancellationDto.ISBooking = "Available";

            return cancellationDto;
        }

        public async Task<ResponseDto> GetHotelBedsSupplierResponse(string strHost, string Username, string Password, Int32 TimeOut, string strxmlRQRS, string methodtype, string Provider = "", HotelSearchParameter para = null, bool DebugMode = false, string Searchkey = "",bool fromCancel=false)
        {
            ResponseDto responseDto = new ResponseDto();

            System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
            //Provider = "hotelbeds";
            timer.Start();
            string strOutput = "";
            string signature = "";
            string Error = "";
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12; //#25-09-2024
            try
            {
                using (var sha = SHA256.Create())
                {
                    long ts = (long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds / 1000;
                    var computedHash = sha.ComputeHash(Encoding.UTF8.GetBytes(Username + Password + ts));
                    signature = BitConverter.ToString(computedHash).Replace("-", "");
                }
                HttpWebRequest Request = (HttpWebRequest)WebRequest.Create(strHost);
                ServicePointManager.DefaultConnectionLimit = 1000;
                // Request.Timeout = TimeOut;
                Request.Headers.Add("X-Signature", signature);
                Request.Headers.Add("Api-Key", Username);
                Request.ContentType = "application/json";
                Request.Accept = "application/json";
                if (methodtype.ToLower() == "delete")
                    Request.Method = "DELETE";
                else
                    Request.Method = "POST";
                Request.Headers.Add("Accept-Encoding", "gzip");
                Request.Timeout = TimeOut;

                if(!fromCancel)
                {
                    Stream newStream = null;
                    byte[] data = Encoding.ASCII.GetBytes(strxmlRQRS);
                    Request.ContentLength = data.Length;
                    newStream = Request.GetRequestStream();
                    newStream.Write(data, 0, data.Length);
                }
              
                string respXml; /* will contain response from api in string form */

                try
                {
                    HttpWebResponse Response = (HttpWebResponse)await Request.GetResponseAsync();
                    Stream st = Response.GetResponseStream();

                    if (Response.ContentEncoding.ToLower().Contains("gzip"))
                        st = new System.IO.Compression.GZipStream(st, System.IO.Compression.CompressionMode.Decompress);
                    else if (Response.ContentEncoding.ToLower().Contains("deflate"))
                        st = new System.IO.Compression.DeflateStream(st, System.IO.Compression.CompressionMode.Decompress);
                    StreamReader stIn = new StreamReader(st, Encoding.Default);
                    respXml = await stIn.ReadToEndAsync();
                    strOutput = respXml;
                    stIn.Close();
                    timer.Stop();
                    if (para != null)
                    {
                        para.SupplierTime += ",SupplierName :" + Provider + " : " + "Execution Time :" + timer.Elapsed.ToString();
                        if (DebugMode)
                            _ = _commonMethod.XMLRequestResponseTrack(strxmlRQRS, strOutput, Provider, "Proper");
                    }


                    if (Convert.ToString(_keySettings.IsLogTrack) == "1")
                    {
                        TimeSpan timeTaken = timer.Elapsed;
                       // _exception.ErrorLog(Convert.ToString(_keySettings.LogTrackPath), Provider + "->" + strHost + "-> Execution Time : " + timer.Elapsed.ToString());
                        _ = _commonMethod.XMLRequestResponseTrack(strxmlRQRS, strOutput, Provider, "Proper");
                    }
                }
                catch (WebException webExcp)
                {
                    HttpWebResponse httpResponse = (HttpWebResponse)webExcp.Response;
                    string result = httpResponse.StatusDescription.ToString();
                    string result1 = new StreamReader(webExcp.Response.GetResponseStream()).ReadToEnd();
                    _ = _commonMethod.XMLRequestResponseTrackError(strxmlRQRS, result1, Provider, "Proper, Searchkey=" + Searchkey + "");
                    Error = result + " ," + result1;

                    responseDto.Response = "";
                    responseDto.Error = Error;
                    return responseDto;
                }
            }
            catch (Exception ex)
            {
                Error = ex.Message.ToString();
                _ = _exception.Error(ex, Searchkey);
            }

            responseDto.Response = strOutput;
            responseDto.Error = Error;

            return responseDto;
        }

        //prebook
        public async Task<PriceCheckCMDto> PriceUpdateHotelBedsV1(string HTL_RoomCode, DataTable dtCheckPriceReqStructure, DataSet dsCartDetails, DataRow[] HotelBedsV1Details, string UniqueNo, string AgentUniqueNo, int i, string IsFor, double CompanyBuyPrice, bool IsError, double itemprice, string Searchkey = "", int AgentId = 0)
        {
            PriceCheckCMDto priceCheckCMDto = null;
            bool flag = false;

            string responsetype = "Success";
            string time = "";
            // string SupplierName = "HotelBedsV1";
            string SupplierName = Convert.ToString(dsCartDetails.Tables[1].DefaultView[i]["SupplierName"]);
            string Error = "";
            string Request = "";
            string ResponseData = "";
            try
            {

                Request = HotelsBedV1PolicyRequest(HTL_RoomCode);
                _commonMethod.ThirdPartyBookingHistory(Convert.ToString(UniqueNo), Convert.ToString(AgentUniqueNo), Request, "-", "Before Request hit Price Check" + IsFor, Convert.ToString(dsCartDetails.Tables[1].DefaultView[i]["SupplierName"]), "Request Confirm", "-", "API Out");
                dtCheckPriceReqStructure.Rows.Add(_commonMethod.AddCheckPriceReq(dtCheckPriceReqStructure.NewRow(), i + 1, Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[i]["UserCartID"]), Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[i]["UserCartDetailId"]), Convert.ToString(dsCartDetails.Tables[1].DefaultView[i]["RoomTypeID"]), Convert.ToString(dsCartDetails.Tables[1].DefaultView[i]["SupplierName"]).ToLower(), Request));

                System.Diagnostics.Stopwatch timerroomprocess = new System.Diagnostics.Stopwatch();
                timerroomprocess.Start();

                ResponseDto responseDto = await GetHotelBedsSupplierResponse(Convert.ToString(HotelBedsV1Details[0]["WebSiteURL"]) + _supplierConfig.Hotelbeds.checkrates, Convert.ToString(HotelBedsV1Details[0]["UserName"]), Convert.ToString(HotelBedsV1Details[0]["Password"]), 60000, Request, "", SupplierName, null, false, Searchkey);
                ResponseData = responseDto.Response;
                Error = responseDto.Error;

                timerroomprocess.Stop();
                time = timerroomprocess.Elapsed.ToString();


                if (Error == "")
                    _commonMethod.ThirdPartyBookingHistory(Convert.ToString(UniqueNo), Convert.ToString(AgentUniqueNo), Request, ResponseData, "After Request hit Price Check" + IsFor, SupplierName, "Response Confirm", "-", "API Out");
                else
                    _commonMethod.ThirdPartyBookingHistory(Convert.ToString(UniqueNo), Convert.ToString(AgentUniqueNo), Request, Error, "After Request hit Price Check" + IsFor, SupplierName, "Response Confirm", "-", "API Out");


                if (ResponseData.Trim() == "" || Error != "")
                {
                    try
                    {
                        responsetype = "GeneralError";
                        //int AgentId = Convert.ToInt32(Common.getSession("UserId") == "" ? "0" : Common.getSession("UserId"));
                        string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, Request, ResponseData, Error);
                    }
                    catch (Exception e)
                    {
                        _ = _exception.Error(e, Searchkey);
                    }

                    priceCheckCMDto.Flag = true;
                    return priceCheckCMDto;
                }
                string ConvertXml = Newtonsoft.Json.JsonConvert.DeserializeXmlNode("{\"Row\":" + ResponseData + "}", "root").InnerXml;
                DataSet dsHotelBeds = new DataSet();
                dsHotelBeds = _commonMethod.GetDataSetFromXMLString(ConvertXml);
                for (int j = 0; j < dsHotelBeds.Tables["rates"].Rows.Count; j++)
                {
                    itemprice = Convert.ToDouble(dsHotelBeds.Tables["rates"].Rows[j]["net"]);
                    break;
                }

                if (IsFor == "P")
                {
                    if (Convert.ToDouble((itemprice).TruncateDecimalPlaces(0)) > (CompanyBuyPrice).TruncateDecimalPlaces(0))
                    {
                        try
                        {
                            ResponseData = "#Price Changed " + "#Main Price = " + CompanyBuyPrice + " #New Price = " + itemprice + " #Response= " + ResponseData;
                            // string XYZ = "CompanyBuyingPrices=" + CompanyBuyPrice + " ResPrice=" + itemprice + "";
                            responsetype = "GeneralError";
                            string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, Request, ResponseData, Error);
                        }
                        catch (Exception e)
                        {
                            _ = _exception.Error(e, Searchkey);
                        }
                        priceCheckCMDto.Flag = true;
                        priceCheckCMDto.ItemPrice = itemprice;
                        return priceCheckCMDto;
                    }
                }
                else
                {
                    if (Convert.ToDouble((itemprice).TruncateDecimalPlaces(0)) > (CompanyBuyPrice).TruncateDecimalPlaces(0))
                    {
                        try
                        {
                            ResponseData = "#Price Changed " + "#Main Price = " + CompanyBuyPrice + " #New Price = " + itemprice + " #Response= " + ResponseData;
                            //string XYZ = "CompanyBuyingPrices=" + CompanyBuyPrice + " ResPrice=" + itemprice + "";
                            responsetype = "GeneralError";
                            string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, Request, ResponseData, Error);
                        }
                        catch (Exception e)
                        {
                            _ = _exception.Error(e, Searchkey);
                        }
                        priceCheckCMDto.Flag = true;
                        priceCheckCMDto.ItemPrice = itemprice;
                        return priceCheckCMDto;
                    }
                }

                try
                {
                    responsetype = "Success";
                    string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, Request, ResponseData, Error);
                }
                catch (Exception e)
                {
                    _ = _exception.Error(e, Searchkey);
                }
            }
            catch (Exception e)
            {
                try
                {
                    Error = e.Message + e.StackTrace.ToString();
                    responsetype = "GeneralError";
                    string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, Request, ResponseData, Error);
                }
                catch (Exception ex)
                {
                    _ = _exception.Error(e, Searchkey);
                }
                _ = _exception.Error(e, Searchkey);

                priceCheckCMDto.IsError = true;
            }

            priceCheckCMDto.Flag = false;
            priceCheckCMDto.ItemPrice = itemprice;
            return priceCheckCMDto;
        }

        public string HotelBedsdV1BookindgRequest(DataTable dtPassengers, DataSet dsCartDetails, bool IsPRiceUpdate, bool IsError, int j, string AgentCode)
        {
            try
            {
                StringBuilder HBV1 = new StringBuilder();
                if (IsPRiceUpdate == false && IsError == false)
                {
                    string[] Array = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomTypeID"]).Split(new String[] { "$$$" }, StringSplitOptions.None);
                    string BookingCodeHotel = Array[1];
                    string InternalReference = Convert.ToString(AgentCode) + "_" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["UserCartDetailId"]);
                    dtPassengers.DefaultView.RowFilter = "PickUp='" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomTypeId"]) + "' and UserCartID='" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["UserCartID"]) + "' and RoomNo='" + Convert.ToInt32(j + 1) + "'";
                    DataTable dtnewpassanger = dtPassengers.DefaultView.ToTable();
                    HBV1.Append("{");
                    HBV1.Append("\"holder\":");
                    HBV1.Append("{");
                    if (Convert.ToString(dtnewpassanger.Rows[0]["IsLead"]) == "1")
                    {
                        HBV1.Append("\"name\":\"" + Convert.ToString(dtnewpassanger.Rows[0]["FirstName"]) + "\",");
                        HBV1.Append("\"surname\":\"" + Convert.ToString(dtnewpassanger.Rows[0]["LastName"]) + "\"");
                    }
                    else
                    {
                        HBV1.Append("\"name\":\"" + Convert.ToString(dtnewpassanger.Rows[0]["FirstName"]) + "\",");
                        HBV1.Append("\"surname\":\"" + Convert.ToString(dtnewpassanger.Rows[0]["LastName"]) + "\"");
                    }
                    HBV1.Append("},");
                    HBV1.Append("\"rooms\": [{");
                    HBV1.Append("\"rateKey\":\"" + BookingCodeHotel + "\",");
                    HBV1.Append("\"paxes\": [{");
                    for (int b = 0; b < dtnewpassanger.Rows.Count; b++)
                    {
                        if (b >= 1)
                            HBV1.Append("},{");
                        HBV1.Append("\"roomId\":\"1\",");
                        string PaxTypeHB = "";
                        if (Convert.ToString(dtnewpassanger.Rows[b]["PaxType"]) == "Adult")
                            PaxTypeHB = "AD";
                        else
                            PaxTypeHB = "CH";

                        HBV1.Append("\"type\": \"" + PaxTypeHB + "\",");
                        HBV1.Append("\"name\": \"" + Convert.ToString(dtnewpassanger.Rows[b]["FirstName"]) + "\",");
                        HBV1.Append("\"surname\": \"" + Convert.ToString(dtnewpassanger.Rows[b]["LastName"]) + "\"");
                    }
                    HBV1.Append("}]");
                    HBV1.Append("}],");
                    HBV1.Append("\"clientReference\":\"" + InternalReference + "\",");
                    //HBV1.Append("\"tolerance\":\"" + 2 + "\""); //CRM Ticket #12954
                    HBV1.Append("\"tolerance\":\"" + 0 + "\""); //Added on 19-SEP-2024 as per mustafa sir/K2
                    HBV1.Append("}");

                    string HBV1REQ = HBV1.ToString();
                    return HBV1REQ;
                }
                else
                    return "";
            }
            catch (Exception ex)
            {
                _ = _exception.ErrorNew(ex,_LogAPI.Book);
                return "";
            }
        }
        public string HotelBedsdV1BookindgRequest_newversion(DataTable dtPassengers, BookRequest_newversion bookRequest, bool IsPRiceUpdate, bool IsError, int j, string AgentCode)
        {
            try
            {
                StringBuilder HBV1 = new StringBuilder();
                if (IsPRiceUpdate == false && IsError == false)
                {
                    string[] Array = Convert.ToString(bookRequest.userCart.UserCartDetails[j].RoomTypeId).Split(new String[] { "$$$" }, StringSplitOptions.None);
                    string BookingCodeHotel = Array[1];
                    string InternalReference = Convert.ToString(AgentCode) + "_" + Convert.ToString(bookRequest.userCart.UserCartDetails[j].UserCartDetailId);
                    dtPassengers.DefaultView.RowFilter = "PickUp='" + Convert.ToString(bookRequest.userCart.UserCartDetails[j].RoomTypeId) + "' and UserCartID='" + Convert.ToString(bookRequest.userCart.UserCartDetails[j].UserCartId) + "' and RoomNo='" + Convert.ToInt32(j + 1) + "'";
                    DataTable dtnewpassanger = dtPassengers.DefaultView.ToTable();
                    HBV1.Append("{");
                    HBV1.Append("\"holder\":");
                    HBV1.Append("{");
                    if (Convert.ToString(dtnewpassanger.Rows[0]["IsLead"]) == "1")
                    {
                        HBV1.Append("\"name\":\"" + Convert.ToString(dtnewpassanger.Rows[0]["FirstName"]) + "\",");
                        HBV1.Append("\"surname\":\"" + Convert.ToString(dtnewpassanger.Rows[0]["LastName"]) + "\"");
                    }
                    else
                    {
                        HBV1.Append("\"name\":\"" + Convert.ToString(dtnewpassanger.Rows[0]["FirstName"]) + "\",");
                        HBV1.Append("\"surname\":\"" + Convert.ToString(dtnewpassanger.Rows[0]["LastName"]) + "\"");
                    }
                    HBV1.Append("},");
                    HBV1.Append("\"rooms\": [{");
                    HBV1.Append("\"rateKey\":\"" + BookingCodeHotel + "\",");
                    HBV1.Append("\"paxes\": [{");
                    for (int b = 0; b < dtnewpassanger.Rows.Count; b++)
                    {
                        if (b >= 1)
                            HBV1.Append("},{");
                        HBV1.Append("\"roomId\":\"1\",");
                        string PaxTypeHB = "";
                        if (Convert.ToString(dtnewpassanger.Rows[b]["PaxType"]) == "Adult")
                            PaxTypeHB = "AD";
                        else
                            PaxTypeHB = "CH";

                        HBV1.Append("\"type\": \"" + PaxTypeHB + "\",");
                        HBV1.Append("\"name\": \"" + Convert.ToString(dtnewpassanger.Rows[b]["FirstName"]) + "\",");
                        HBV1.Append("\"surname\": \"" + Convert.ToString(dtnewpassanger.Rows[b]["LastName"]) + "\"");
                    }
                    HBV1.Append("}]");
                    HBV1.Append("}],");
                    HBV1.Append("\"clientReference\":\"" + InternalReference + "\",");
                    //HBV1.Append("\"tolerance\":\"" + 2 + "\""); //CRM Ticket #12954
                    HBV1.Append("\"tolerance\":\"" + 0 + "\""); //Added on 19-SEP-2024 as per mustafa sir/K2
                    HBV1.Append("}");

                    string HBV1REQ = HBV1.ToString();
                    return HBV1REQ;
                }
                else
                    return "";
            }
            catch (Exception ex)
            {
                _ = _exception.ErrorNew(ex, _LogAPI.Book);
                return "";
            }
        }
    }
}
