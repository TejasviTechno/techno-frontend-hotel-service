using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Azure;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Tokens;
using RestSharp;
using XConnect.CommonUtilities;
using XConnect.CommonUtilities.Interface;
using XConnect.ErrorLogHandler.Interface;
using XConnect.Markup.Interface;
using XConnect.Models;
using XConnect.Models.Hotel;
using XConnect.Supplier.PriceTravel.Interface;
using static Azure.Core.HttpHeader;
using static XConnect.Supplier.Booking.Com.preebookrs;
using static XConnect.Supplier.DerbySoft.DerbySoft;
using Options = XConnect.Models.Options;

namespace XConnect.Supplier.PriceTravel
{
    public class PriceTravelRepo : IPriceTravel
    {
        private readonly ICommonMethod _commonMethod;
        private readonly IException _exception;
        private readonly IMarkup _markup;
        private readonly ISupplierRes _supplierRes;
        private readonly KeySettings _keySettings;
        private readonly IMemoryCache _cache;


        public PriceTravelRepo(ICommonMethod commonMethod, IMarkup markup, ISupplierRes supplierRes, IException exception, IOptions<KeySettings> keySettings, IMemoryCache cache)
        {
            _commonMethod = commonMethod;
            _markup = markup;
            _supplierRes = supplierRes;
            _exception = exception;
            _keySettings = keySettings.Value;
            _cache = cache;
        }

        public void GetPriceTravelRequestOnly_async(DataRow[] commonCode, DataTable dtROE, List<StarRating> listRating, DataTable dtPreferredDataHotel, HotelSearchParameter SearchPara, List<SupplierSearchConfig> lstPrice_TravelSearchConfig, List<SupplierDetails> lstPrice_TravelDetails, ref ConcurrentBag<HotelX> AllTask, int hit, string AgentCode, TokenData tokenData, DataSet dtXMLCity)
        {
            try
            {
                List<StarRating> lstRating = new List<StarRating>();
                lstRating = _commonMethod.GetStarRatingStringList_xtg(SearchPara);

                List<HotelCodeByCity> DtHotelDAta = HtmlExtensions.ToList<HotelCodeByCity>(commonCode.CopyToDataTable());
                int ratingcount = lstRating.Count;
                int hotelcount = DtHotelDAta.Count;

                foreach (SupplierDetails Price_TravelDetails in lstPrice_TravelDetails)
                {
                    try
                    {
                        int SupplierID = Price_TravelDetails.SupplierID;
                        string Supplier = Price_TravelDetails.SupplierName;

                        if (lstPrice_TravelSearchConfig.Count > 0)
                        {
                            List<SupplierSearchConfig> Price_TravelSearchConfig = lstPrice_TravelSearchConfig
                                    .Where(x => x != null && x.SupplierID == SupplierID)
                                    .ToList();

                            if (Price_TravelSearchConfig != null && Price_TravelSearchConfig.Count > 0)
                            {
                                DataForSearch dataForSearch = _commonMethod.GetXmlDataForSearchTechwise(SearchPara.Nationality, dtXMLCity, Supplier, Price_TravelDetails.TechName);
                                if (dataForSearch.XmlNationality != "")
                                {

                                    List<HotelCodeByCity> filter = new List<HotelCodeByCity>();
                                    filter = _commonMethod.FilterWithOutCity_Code(ratingcount, hotelcount, DtHotelDAta, lstRating, Supplier);

                                    if (filter.Count > 0)
                                    {
                                        string PriceTravelCurrency = Price_TravelDetails.CurrencyShortName;
                                        double CurrencyFactorWE_DMC = 1;
                                        if (PriceTravelCurrency.ToLower() != tokenData.SiteCurrency.ToLower())
                                        {
                                            CurrencyFactorWE_DMC = _commonMethod.GetROEByCurrency(PriceTravelCurrency, tokenData.dtROE);
                                        }

                                        for (int k = 0; k < Price_TravelSearchConfig.Count; k++)
                                        {
                                            //if (Convert.ToInt32(Price_TravelSearchConfig[k]["Hit"]) != Hit)
                                            //    continue;
                                            bool DebugMode = (Convert.ToInt32(Price_TravelSearchConfig[k].DebugMode) == 0 ? false : true);

                                            if (Convert.ToInt32(Price_TravelSearchConfig[k].CityID) == 0 || Convert.ToInt32(Price_TravelSearchConfig[k].CityID) == SearchPara.CityID)//All City means 0
                                            {

                                                int NoOfRecord = Convert.ToInt32(Price_TravelSearchConfig[k].Noofrecords);
                                                double count = Convert.ToDouble(filter.Count);
                                                int NoOfDiv = Convert.ToInt32(Math.Ceiling(count / Convert.ToDouble(NoOfRecord)));
                                                List<List<XMLID_Search>> lstxml = _commonMethod.GetXMLIDString(NoOfDiv, NoOfRecord, count, filter);


                                                // Price_TravelDetails

                                                // priceTravelToken priceTravelToken = JsonConvert.DeserializeObject<priceTravelToken>(PriceTravelTokenGeneration(strHost, UserName, Password));

                                                priceTravelToken priceTravelToken = Newtonsoft.Json.JsonConvert.DeserializeObject<priceTravelToken>(PriceTravelTokenGeneration(Price_TravelDetails));
                                                if (priceTravelToken != null)
                                                {
                                                    for (int a = 0; a < NoOfDiv; a++)
                                                    {
                                                        string IdsForSearch = Convert.ToString(_commonMethod.GetHotelCodeByCOmma(lstxml[a]));
                                                        HotelX xhotel = new HotelX();
                                                        xhotel.EchoToken = Convert.ToString(priceTravelToken.access_token);
                                                        if (_keySettings.SupplierProxy == "0")
                                                            xhotel.URL = Convert.ToString(Price_TravelDetails.WebSiteURL) + "hotels/rates/?language=en-us";
                                                        else
                                                            xhotel.URL = Convert.ToString(Price_TravelDetails.WebSiteURLforAPI) + "hotels/rates/?language=en-us";
                                                        xhotel.UserName = Convert.ToString(Price_TravelDetails.UserName);
                                                        xhotel.PassWord = Convert.ToString(Price_TravelDetails.Password);
                                                        xhotel.SearchTimeOut = Convert.ToInt32(Price_TravelSearchConfig[k].HotelSearchTimeOut);
                                                        //if (hit > 0)
                                                        xhotel.ProcessTimeout = Convert.ToInt32(Price_TravelSearchConfig[k].HotelSearchTimeOut2);

                                                        xhotel.DebugeMode = DebugMode;
                                                        xhotel.MethoodType = "POST";
                                                        xhotel.Supplier = Supplier;
                                                        xhotel.CurrencyCode = PriceTravelCurrency;
                                                        xhotel.CurrencyFactor = CurrencyFactorWE_DMC;
                                                        xhotel.XmlNationality = dataForSearch.XmlNationality;
                                                        xhotel.XmlCity = "";
                                                        xhotel.XmlCountry = "";
                                                        //xhotel.EchoToken = Convert.ToString(Guid.NewGuid()).Substring(0, 13);

                                                        xhotel.AgentWiseMarkup = Price_TravelDetails.AgentWiseMarkup;
                                                        xhotel.IsShowSupplier = Convert.ToInt16(Price_TravelDetails.IsShowSupplier) == 1 ? true : false;
                                                        xhotel.SupplierID = Price_TravelDetails.SupplierID;
                                                        xhotel.MasterSupplierID = Price_TravelDetails.MasterSupplierId;

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
                                                            //objrq.Request = Common.XMLFastPayRequestOnly_Async(Convert.ToInt32(xhotels.SearchTimeOut), SearchPara, xhotels.URL, xhotels.UserName, xhotels.PassWord, IdsForSearch, XmlNationality, HotelId, xmlCountry);
                                                            //objrq.Request = Common.XMLPriceTravelRequestOnly_async("1", "1", xhotel.URL, xhotel.UserName, xhotel.PassWord, xhotel.SearchTimeOut, SearchPara, IdsForSearch, DebugMode, XmlNationality);
                                                            objrq.Request = XMLPriceTravelRequestOnly_async("1", "1", xhotel.URL, xhotel.UserName, xhotel.PassWord, xhotel.SearchTimeOut, SearchPara, IdsForSearch, DebugMode, xhotel.XmlNationality);

                                                            objrq.Response = "";
                                                            RQRS.Add(objrq);
                                                        }
                                                        xhotel.ReturnRooms = Price_TravelDetails.ReturnRooms;
                                                        xhotel.TechName = Price_TravelDetails.TechName;
                                                        xhotel.RQRS = RQRS;
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
                    catch (Exception ex)
                    {
                        _ = _exception.Error(ex);
                    }
                }
            }
            catch (Exception ex)
            {
                _ = _exception.Error(ex);
            }
        }

        public string PriceTravelTokenGeneration(SupplierDetails Price_TravelDetails)
        {
            string MainRespone = "";
            try
            {
                using var client = new HttpClient();
                string Key = "Cache_PriceTravelToken";
                if (!_cache.TryGetValue(Key, out MainRespone))
                {
                    if (Price_TravelDetails != null)
                    {
                        //string WebSiteURL = Price_TravelDetails[0].WebSiteURL;
                        //WebSiteURL = (WebSiteURL.Substring(0, 34)) + "connect/token";
                        string webSiteUrl = "https://ptapi.pricetravel.com/connect/token";


                        var requestData = new List<KeyValuePair<string, string>>
                                {
                                    new("client_id", Price_TravelDetails.UserName),
                                    new("client_secret", Price_TravelDetails.Password),
                                    new("grant_type", "client_credentials"),
                                    new("scopes", "admin_api")
                                };

                        var requestContent = new FormUrlEncodedContent(requestData);

                        var response = client.PostAsync(webSiteUrl, requestContent).GetAwaiter().GetResult();

                        var json = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                        //var tokenObj = Newtonsoft.Json.JsonSerializer.Deserialize<TokenResponse>(json);

                        if (json != null && !string.IsNullOrEmpty(json.ToString()))
                        {
                            _cache.Set(Key, json, TimeSpan.FromMinutes(58));
                            return json.ToString();
                        }


                    }
                }


            }
            catch (Exception ex)
            {
                _ = _exception.Error(ex);

            }
            return MainRespone;
        }

     

        public string XMLPriceTravelRequestOnly_async(string MinStar, string MaxStar, string WSurl, string Username, string Password, Int32 SearchTimeOut, HotelSearchParameter SearchParam, string HotelIds, bool DebugMode, string HBNationality)
        {
            try
            {
                string checkin = Convert.ToString(SearchParam.CheckInDate);
                string checkout = Convert.ToString(SearchParam.CheckOutDate);
                int NoOfNight = Convert.ToInt32(SearchParam.NoofNights);
                checkin = checkin.Substring(6, 4) + "-" + checkin.Substring(3, 2) + "-" + checkin.Substring(0, 2);
                checkout = checkout.Substring(6, 4) + "-" + checkout.Substring(3, 2) + "-" + checkout.Substring(0, 2);

                string HotelList = PriceTravelSearchRequest(SearchParam, checkin, checkout, MinStar, MaxStar, HotelIds, HBNationality);
                return HotelList;
            }
            catch (IOException e)
            {
                _ = _exception.Error(e);
            }
            return "";
        }
        public string PriceTravelSearchRequest(HotelSearchParameter SearchParam, string checkin, string checkout, string MinStar, string MaxStar, string HotelIds, string HBNationality)
        {
            try
            {
                StringBuilder roomstring = new StringBuilder();

                roomstring.Append("{");
                if (SearchParam.Rooms.Count > 0)
                {

                    roomstring.Append("\n\"checkIn\":\"" + checkin + "\",");
                    roomstring.Append("\n\"checkOut\":\"" + checkout + "\",");
                    roomstring.Append("\n\"roomCriteria\":[");

                    for (int roomNo = 0; roomNo < SearchParam.Rooms.Count; roomNo++)
                    {
                        if (roomNo == 0)
                        {
                            roomstring.Append("\n{");
                        }
                        else if (roomNo > 0)
                        {
                            roomstring.Append("\n{");
                        }

                        if (SearchParam.Rooms[roomNo].NoofAdults > 0)
                        {

                            roomstring.Append("\n\"paxes\":[");

                            for (int AdultNo = 0; AdultNo < SearchParam.Rooms[roomNo].NoofAdults; AdultNo++)
                            {

                                if (AdultNo == 0)
                                {
                                    roomstring.Append("\n{");
                                }
                                else if (AdultNo > 0)
                                {
                                    roomstring.Append("\n{");
                                }
                                int age = 21;

                                roomstring.Append("\n\"age\":" + age);

                                if (SearchParam.Rooms[roomNo].NoofAdults == 1 && SearchParam.Rooms[roomNo].NoofChild == 0)
                                {
                                    roomstring.Append("\n}");
                                }
                                else if (SearchParam.Rooms[roomNo].NoofAdults == (AdultNo + 1) && SearchParam.Rooms[roomNo].NoofChild == 0)
                                {
                                    roomstring.Append("\n}");
                                }
                                else
                                {
                                    roomstring.Append("\n},");
                                }

                            }

                            if (SearchParam.Rooms[roomNo].NoofChild > 0)
                            {


                                for (int ChildNo = 0; ChildNo < SearchParam.Rooms[roomNo].NoofChild; ChildNo++)
                                {
                                    roomstring.Append("\n{");

                                    int age = 0;

                                    /*                               
                                    if(SearchParam.Rooms[roomNo].Child1Age > 0)
                                    {
                                       age = SearchParam.Rooms[roomNo].Child1Age;
                                    }
                                    else if(SearchParam.Rooms[roomNo].Child2Age > 0 )
                                    {
                                        age = SearchParam.Rooms[roomNo].Child2Age;
                                    }
                                   */


                                    if (SearchParam.Rooms[roomNo].NoofChild == 1)
                                    {
                                        age = SearchParam.Rooms[roomNo].Child1Age;
                                    }
                                    else if (SearchParam.Rooms[roomNo].NoofChild == 2)
                                    {
                                        if ((ChildNo + 1) == 1)
                                        {
                                            age = SearchParam.Rooms[roomNo].Child1Age;
                                        }
                                        else if ((ChildNo + 1) == 2)
                                        {
                                            age = SearchParam.Rooms[roomNo].Child2Age;
                                        }

                                    }




                                    roomstring.Append("\n\"age\":" + age);

                                    if (SearchParam.Rooms[roomNo].NoofChild == 1)
                                    {
                                        roomstring.Append("\n}");
                                    }
                                    else if (SearchParam.Rooms[roomNo].NoofChild == (ChildNo + 1))
                                    {
                                        roomstring.Append("\n}");
                                    }
                                    else
                                    {
                                        roomstring.Append("\n},");
                                    }

                                }

                            }

                            roomstring.Append("\n]");
                        }

                        if (SearchParam.Rooms.Count == 1)
                        {
                            roomstring.Append("\n}");
                        }
                        else if (SearchParam.Rooms.Count == (roomNo + 1))
                        {
                            roomstring.Append("\n}");
                        }
                        else
                        {
                            roomstring.Append("\n},");
                        }

                    }

                    roomstring.Append("\n],");//roomCriteria

                    roomstring.Append("\n\"hotelIds\":[\n");

                    roomstring.Append(HotelIds);

                    roomstring.Append("\n],");

                    roomstring.Append("\n\"hotelRateFilter\":{");

                    //roomstring.Append("\n\"nonRefundable\":false");
                    roomstring.Append("\n\"nonRefundable\":true"); // due to ticket#34930

                    roomstring.Append("\n},");

                    roomstring.Append("\n\"market\":\"" + HBNationality + "\"");

                }
                roomstring.Append("\n}");

                return roomstring.ToString();
            }
            catch (Exception ex)
            {
                _ = _exception.Error(ex);
                return "";
            }
        }

        public double CalculateAmountWithTax(double price, double TaxAmt)
        {
            try
            {
                double TaxAmount = (price * TaxAmt) / 100;
                double finalAmt = price + TaxAmount;
                return finalAmt;
            }
            catch (Exception ex)
            {
                _ = _exception.Error(ex);
                return 0;
            }
        }
        public async Task<List<Options>> XMLPriceTravelPriceOnly_async(HotelX Item, CommonSearch commonSearch, double AgentPercentage, int SearchTimeOut, string Supplier, DataTable dtCountryCityMarkup, int AgentId, DataSet dsAgentDetails, int SupplierID, string ResponseData, HotelSearchParameter SearchParam, string xmlcity, string xmlcountry, string xmlnationality, double CurrencyFactor, Boolean IsShowSupplier, string HotelID, DataTable dtROE, DataTable dtSuplierMarkup, string currency, string StoreSupplierResErrorLog)
        {
            System.Diagnostics.Stopwatch timerroomx = new System.Diagnostics.Stopwatch();
            timerroomx.Start();

            List<Options> options = new List<Options>();
            DataTable DtHotel = _commonMethod.GenerateDataTableNewV2();


            Root_PriceTravel response = new Root_PriceTravel();


            string hotelid = "";
            try
            {

                int counthotel = -1;
                try
                {
                    response = Newtonsoft.Json.JsonConvert.DeserializeObject<Root_PriceTravel>(ResponseData);
                    //counthotel = response.hotels.Count;
                }
                catch (Exception ex)
                {
                    Item.ResponseStatus = 404;
                    Item.ResponseType = "NoCount";
                    _ = _exception.Error(ex);
                    return options;
                }
                if (response == null || response.hotels == null)
                {
                    Item.ResponseStatus = 204;
                    Item.ResponseType = "NoCount";
                    return options;
                }
                // double MinPrice = 0;

                for (int i = 0; i < response.hotels.Count; i++)
                {
                    DataTable Dt_PriceTravel_HotelRoom = _commonMethod.GenerateRoomtypeDataTableNewV2();
                    DataTable dt_PriceTravel_Cancellation = _commonMethod.GenerateCancellationDataTableNewV2();
                    DataTable Dt_PriceTravel_RateBreackUp = _commonMethod.GenerateoomRateBreackUpDataTableNewV2();

                    double FinalPrice = 0;
                    Item.ResponseType = "Success";
                    //for (int c = 0; c < SearchParam.Rooms.Count(); c++)
                    {
                        //hotelid = Convert.ToString(FastPayHotelsv3Response.hotelAvails[i].hotelInfo.code);


                        {
                            #region "When not Need Room" 
                            if (SearchParam.Filters.IsShowRooms == 0)
                            {


                                //string p = Convert.ToString(response.hotels[hotel].rooms[a].rates[0].amount);

                                // for (int c = 0; c < SearchParam.Rooms.Count(); c++)
                                {
                                    //int NoOfAdults = SearchParam.Rooms[c].NoofAdults;
                                    bool chk = false;
                                    double Total = 0;

                                    for (int a = 0; a < response.hotels[i].rooms.Count; a++)
                                    {
                                        if (chk == false)
                                        {

                                            // var amount = response.hotels[hotel].rooms[a].rates[0].amount;

                                            //string p = Convert.ToString(response.hotels[hotel].rooms[a].rates[0].publicAmount);
                                            string p = Convert.ToString(response.hotels[i].rooms[a].rates[0].amount);

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
                                                Total = MinPrice;
                                        }
                                    }
                                    FinalPrice = Total;

                                }

                                //if (FinalPrice == 0)
                                //    continue;
                                //DataRow dr = DtHotel.NewRow();

                                FinalPrice = FinalPrice * CurrencyFactor;
                                double TaxPer = 0.0;
                                if (TaxPer != 0)
                                    FinalPrice = CalculateAmountWithTax(FinalPrice, TaxPer);

                                DataRow drXmlHotel = DtHotel.NewRow();
                                string XMLCityCode = xmlcity;
                                drXmlHotel["ContractID"] = 0;
                                drXmlHotel["SupplierId"] = SupplierID;
                                drXmlHotel["HotelName"] = "";//Convert.ToString(drGiataDetails[0]["HotelName"]);
                                drXmlHotel["IsShowSupplier"] = IsShowSupplier;
                                drXmlHotel["UniqueNo"] = 0;
                                drXmlHotel["Rating"] = 0;
                                drXmlHotel["Favorite"] = 0;
                                drXmlHotel["VatdiffAmount"] = 0;
                                drXmlHotel["XMLHotelCode"] = response.hotels[i].id;
                                drXmlHotel["SupplierName"] = Item.Supplier;
                                drXmlHotel["SupplierCurrency"] = currency;
                                //int OwnsystemHotelId = Convert.ToInt32(dtdetails[0]["OwnsystemHotelId"]);
                                //FinalPrice = 1;// Common.setHotelListMarkup(SearchParam, dtCountryCityMarkup, Convert.ToInt32(dsAgentDetails.Tables[0].Rows[0]["IsParentId"]), Convert.ToDouble(dsAgentDetails.Tables[0].Rows[0]["B2CMarkupPer"]), AgentId, commonSearch.TariffID, AgentPercentage, lowprice, GiataId, SupplierID, Rating);
                                drXmlHotel["FinalSellingPrice"] = FinalPrice;
                                drXmlHotel["SellingPrice"] = FinalPrice;
                                //drXmlHotel["SupplierId"] = SupplierID;
                                // double Pricenew = Convert.ToDouble(Convert.ToDouble(((DS.hotelAvails[i].availRoomRates[i1].totalPrice) * SearchParam.Rooms.Count) * fastpayhotelsv3Currency));

                                //drXmlHotel["FinalSellingPrice"] = MainPrice;
                                //drXmlHotel["SellingPrice"] = MainPrice;
                                //drXmlHotel["SupplierId"] = SupplierID;
                                DtHotel.Rows.Add(drXmlHotel);
                                //drXmlHotel["AgentPercentage"] = AgentPercentage;
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
                            #endregion
                            else
                            {



                                DataRow drXmlHotel = DtHotel.NewRow();
                                string XMLCityCode = xmlcity;
                                drXmlHotel["ContractID"] = 0;
                                drXmlHotel["SupplierId"] = SupplierID;
                                drXmlHotel["HotelName"] = "";//Convert.ToString(drGiataDetails[0]["HotelName"]);
                                drXmlHotel["IsShowSupplier"] = IsShowSupplier;
                                drXmlHotel["UniqueNo"] = 0;
                                drXmlHotel["Rating"] = 0;
                                drXmlHotel["Favorite"] = 0;
                                drXmlHotel["VatdiffAmount"] = 0;
                                drXmlHotel["XMLHotelCode"] = response.hotels[i].id;
                                drXmlHotel["SupplierName"] = Item.Supplier;
                                drXmlHotel["SupplierCurrency"] = currency;
                                //int OwnsystemHotelId = Convert.ToInt32(dtdetails[0]["OwnsystemHotelId"]);
                                FinalPrice = 1;// Common.setHotelListMarkup(SearchParam, dtCountryCityMarkup, Convert.ToInt32(dsAgentDetails.Tables[0].Rows[0]["IsParentId"]), Convert.ToDouble(dsAgentDetails.Tables[0].Rows[0]["B2CMarkupPer"]), AgentId, commonSearch.TariffID, AgentPercentage, lowprice, GiataId, SupplierID, Rating);
                                drXmlHotel["FinalSellingPrice"] = FinalPrice;
                                drXmlHotel["SellingPrice"] = FinalPrice;
                                //drXmlHotel["SupplierId"] = SupplierID;
                                // double Pricenew = Convert.ToDouble(Convert.ToDouble(((DS.hotelAvails[i].availRoomRates[i1].totalPrice) * SearchParam.Rooms.Count) * fastpayhotelsv3Currency));

                                //drXmlHotel["FinalSellingPrice"] = MainPrice;
                                //drXmlHotel["SellingPrice"] = MainPrice;
                                //drXmlHotel["SupplierId"] = SupplierID;
                                DtHotel.Rows.Add(drXmlHotel);

                                //drXmlHotel["AgentPercentage"] = AgentPercentage;
                                if (FinalPrice > 0)
                                {
                                    #region "Room code Block"
                                    bool willAddOrNot = true;
                                    Int64 RaynaID = 0;
                                    Int64 GiataId = 0;
                                    int Rating = 0;
                                    Int64 OwnsystemHotelId = 0;
                                    double MinPrice = FinalPrice;




                                    #region "Room Logic"
                                    DataTable dtParsecRoom = _commonMethod.GenerateRoomtypeDataTableNewV2();
                                    DataTable dtParsecRoomRateBreakup = _commonMethod.GenerateoomRateBreackUpDataTableNewV2();
                                    DataTable dtParsecCacelationPolicy = _commonMethod.GenerateCancellationDataTableNewV2();
                                    #endregion
                                    _markup.GetHotelDataCommon(ref MinPrice, ref willAddOrNot, ref OwnsystemHotelId, ref RaynaID, ref GiataId, ref Rating, ref drXmlHotel, commonSearch.AgentId, commonSearch.TariffID, commonSearch.dsAgentDetails, commonSearch.supplierGiataMaster, SearchParam, commonSearch.dtCountryCityMarkup, commonSearch.B2CMarkup, commonSearch.IsParentId, commonSearch.SubAgentMarkup, commonSearch.SearchByHotel, commonSearch.HotelIDS);
                                    if (willAddOrNot)
                                    {
                                        try
                                        {
                                            //Common.getAgetMarkupSetting("FastPayHotelsV3", GiataId.ToString(), commonSearch.dtSuplierMarkup, commonSearch.dtMarkupDetails, ref AgentPercentage, ref AgentMarkupAmount, ref blnIsShowSupplier, ref isPerce, commonSearch.AgentId, RatingHotel, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), Convert.ToString(SearchParam.CheckInDate));
                                            int CutOffDays = -1;// XMLHotelBookingCommon.CutOffDays;
                                            CutOffDays = _commonMethod.GetCutOffDays(dtSuplierMarkup, Supplier);


                                            double SubAgentMarkup = 0;
                                            int IsParentId = 1, agentID = 0;
                                            //if (dtAgentDetail != null)
                                            //{
                                            //    IsParentId = Convert.ToInt32(dtAgentDetail.Rows[0]["IsParentId"]);
                                            //    SubAgentMarkup = Convert.ToDouble(dtAgentDetail.Rows[0]["B2CMarkupPer"]);
                                            //    agentID = Convert.ToInt32(dtAgentDetail.Rows[0]["AgentID"]);
                                            //}
                                            // double AgentPercentage = 0.0;
                                            Boolean blnIsShowSupplier = false;
                                            double AgentMarkupAmount = 0.0;
                                            Boolean isPerce = true;

                                            //_markup.getAgetMarkupSetting(Item.Supplier, GiataId.ToString(), commonSearch.dtSuplierMarkup, commonSearch.dtMarkupDetails, ref AgentPercentage, ref AgentMarkupAmount, ref blnIsShowSupplier, ref isPerce, commonSearch.AgentId, Item.StarRating, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), Convert.ToString(SearchParam.CheckInDate));

                                            _markup.getAgetMarkupSettingNew_new(Item.Supplier, Convert.ToString(RaynaID), commonSearch.dtSuplierMarkup, commonSearch.dsMarkupDetails, ref AgentPercentage, ref AgentMarkupAmount, ref blnIsShowSupplier, ref isPerce, commonSearch.AgentId, Item.StarRating, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), SearchParam.CheckInDate);

                                            double AgentCompanyPercentage = Convert.ToDouble(SearchParam.AgentCompanyPercentage);
                                            bool IsB2C = Convert.ToBoolean(Convert.ToByte(SearchParam.IsB2BOrB2C));

                                            double B2CMarkup = (IsB2C == false ? AgentCompanyPercentage : XMLHotelBookingCommon.B2CMarkup);

                                            double TaxPer = 0;

                                            int NoOfNight = SearchParam.NoofNights;
                                            string searchRQString = "";
                                            try
                                            {


                                                // string JsonResponse = ResponseData;
                                                //string detailResponse = JsonConvert.DeserializeXmlNode("{\"Row\":" + ResponseData + "}", "root").InnerXml;
                                                //DataSet PriceTravel_Response = Common.GetDataSetFromXMLString(detailResponse);


                                                #region prctravel
                                                string error = "";

                                                //for (int s = 0; s < PriceTravel_Response.Tables["Row"].Rows.Count; s++)
                                                {
                                                    // DataRow[] drRow = PriceTravel_Response.Tables["Row"].Select("Row_Id='" + s + "'");

                                                    //if (drRow.Length > 0)
                                                    {
                                                        //main
                                                        //string Row_Id = Convert.ToString(drRow[0]["Row_Id"]);
                                                        string eventId = response.eventId;// Convert.ToString(drRow[0]["eventId"]);
                                                        //if (PriceTravel_Response.Tables["Row"].Columns.Contains("error"))
                                                        //{
                                                        //    error = Convert.ToString(drRow[0]["error"]);
                                                        //}
                                                        //121

                                                        //if (error == "" && Row_Id != null)
                                                        {
                                                            //DataRow[] drHotels = PriceTravel_Response.Tables["Hotels"].Select("Row_Id='" + Row_Id + "'");

                                                            //if (drHotels.Length > 0)
                                                            {
                                                                //string hotels_Id = Convert.ToString(drHotels[0]["hotels_Id"]);//primary
                                                                //Row_Id = Convert.ToString(drHotels[0]["Row_Id"]);
                                                                string Id = Convert.ToString(response.hotels[i].id); //hotel id
                                                                string HotelId = Id;
                                                                string name = "";// Convert.ToString(response.hotels[i].name);//Convert.ToString(drHotels[0]["name"]);// due to issue in hotelname symbol (')


                                                                //DataRow[] drRooms = PriceTravel_Response.Tables["rooms"].Select("hotels_Id='" + hotels_Id + "'");
                                                                //DataTable dtRooms = null;

                                                                //if (drRooms.Length > 0)
                                                                {
                                                                    //room                                   
                                                                    //dtRooms = drRooms.CopyToDataTable();


                                                                    for (int roomNo = 0; roomNo < response.hotels[i].rooms.Count; roomNo++) //1 record  
                                                                    {
                                                                        //string rooms_Id = Convert.ToString(dtRooms.Rows[roomNo]["rooms_Id"]);   //rooms_Id + 1
                                                                        string RoomId = Convert.ToString(response.hotels[i].rooms[roomNo].id);// Convert.ToString(dtRooms.Rows[roomNo]["id"]);
                                                                        string RoomName = Convert.ToString(response.hotels[i].rooms[roomNo].name);//Convert.ToString(dtRooms.Rows[roomNo]["name"]);
                                                                        string RoomCode = null;   //  Convert.toin rooms_Id + 1
                                                                        double publicAmount = 0;
                                                                        int RoomNo = 1;// Convert.ToInt16(rooms_Id) +1;
                                                                        string rateKey = null;
                                                                        string uniqid = Guid.NewGuid().ToString();//

                                                                        string roomTypeId = null;

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

                                                                        //DataRow[] drRates = PriceTravel_Response.Tables["rates"].Select("rooms_Id='" + rooms_Id + "'");

                                                                        string clientCurrency = null;
                                                                        //if (drRates.Length > 0) // 1 record
                                                                        for (int r = 0; r < response.hotels[i].rooms[roomNo].rates.Count; r++)
                                                                        {
                                                                            rateKey = response.hotels[i].rooms[roomNo].rates[r].rateKey;// Convert.ToString(drRates[0]["rateKey"]);

                                                                            clientCurrency = response.hotels[i].rooms[roomNo].rates[r].clientCurrency;// Convert.ToString(drRates[0]["clientCurrency"]);
                                                                            //string rates_Id = Convert.ToString(drRates[0]["rates_Id"]);
                                                                            double amount = response.hotels[i].rooms[roomNo].rates[r].amount; //Convert.ToDouble(drRates[0]["amount"]);
                                                                            publicAmount = amount;//Convert.ToDouble(drRates[0]["publicAmount"]);

                                                                            RoomCode = uniqid + "$$$" + HotelId + "$$$" + name + "$$$" + RoomId + "$$$" + RoomName + "$$$" + clientCurrency + "$$$" + publicAmount + "$$$" + Item.EchoToken + "$$$" + rateKey;

                                                                            roomTypeId = RoomCode;
                                                                            if (clientCurrency.ToLower() != commonSearch.tokenData.SiteCurrency.ToLower())
                                                                            {
                                                                                //DataTable dtInrCurrency = Common.GetCountryCurrency(clientCurrency);
                                                                                //double CurrencyFactor1 = Convert.ToDouble(dtInrCurrency.Rows[0]["AmtToExch"]);

                                                                                double CurrencyFactor1 = _commonMethod.GetROEByCurrency(clientCurrency, dtROE);
                                                                                publicAmount = publicAmount * CurrencyFactor1;
                                                                            }
                                                                            //else
                                                                            //{
                                                                            //    continue;
                                                                            //} //else comment on 110925

                                                                            double CompanyBuying = publicAmount;

                                                                            //part -2
                                                                            //DataRow[] drReservationPolicies = PriceTravel_Response.Tables["reservationPolicies"].Select("rates_id='" + rates_Id + "' and type='MEAL_PLAN'");

                                                                            string MealPlanDescription = "";
                                                                            string ResPolici_Type = "";
                                                                            string ResPolici_Code = "";
                                                                            string ResPolici_Description = "";

                                                                            DataTable dtReservationPolicies = null;
                                                                            if (response.hotels[i].rooms[roomNo].rates[r].reservationPolicies.Count > 0)//(drReservationPolicies.Length > 0)
                                                                            {
                                                                                //dtReservationPolicies = drReservationPolicies.CopyToDataTable();

                                                                                //for (int ResPoliciNo = 0; ResPoliciNo < dtReservationPolicies.Rows.Count; ResPoliciNo++)
                                                                                for (int ResPoliciNo = 0; ResPoliciNo < response.hotels[i].rooms[roomNo].rates[r].reservationPolicies.Count; ResPoliciNo++)
                                                                                {
                                                                                    if (response.hotels[i].rooms[roomNo].rates[r].reservationPolicies[ResPoliciNo].type == "MEAL_PLAN")
                                                                                    {
                                                                                        ResPolici_Type = response.hotels[i].rooms[roomNo].rates[r].reservationPolicies[ResPoliciNo].type;// Convert.ToString(dtReservationPolicies.Rows[ResPoliciNo]["type"]);
                                                                                        ResPolici_Code = response.hotels[i].rooms[roomNo].rates[r].reservationPolicies[ResPoliciNo].code;//Convert.ToString(dtReservationPolicies.Rows[ResPoliciNo]["code"]);
                                                                                        ResPolici_Description = response.hotels[i].rooms[roomNo].rates[r].reservationPolicies[ResPoliciNo].description; //Convert.ToString(dtReservationPolicies.Rows[ResPoliciNo]["description"]);
                                                                                        MealPlanDescription = ResPolici_Description;
                                                                                    }
                                                                                }
                                                                            }
                                                                            //DataRow[] drCancellationPolicies = PriceTravel_Response.Tables["cancellationPolicies"].Select("rates_Id='" + rates_Id + "'");

                                                                            string TaxInfo = "";
                                                                            //if (response.hotels[i].rooms[roomNo].rates[r].taxes != null)//(drTaxes.Length > 0)
                                                                            //{
                                                                            //    TaxInfo = "<b> Taxes Info: </b> <br>";
                                                                            //    double Taxes_Total = response.hotels[i].rooms[roomNo].rates[r].taxes.total;//Convert.ToDouble(drTaxes[0]["total"]);
                                                                            //    //string Taxes_Id = Convert.ToString(drTaxes[0]["taxes_Id"]);
                                                                            //    TaxInfo = TaxInfo + "Taxes_Total=" + Taxes_Total;
                                                                            //    //DataRow[] drDetails = PriceTravel_Response.Tables["details"].Select("taxes_Id='" + Taxes_Id + "'");

                                                                            //    //DataTable dtDetails = null;

                                                                            //    //if (response.hotels[i].rooms[roomNo].rates[r].taxes.details.Count > 0) //(drDetails.Length > 0)
                                                                            //    //{
                                                                            //        //dtDetails = drDetails.CopyToDataTable();

                                                                            //        for (int detailNo = 0; detailNo < response.hotels[i].rooms[roomNo].rates[r].taxes.details.Count; detailNo++)
                                                                            //        {
                                                                            //            string Details_Type = response.hotels[i].rooms[roomNo].rates[r].taxes.details[detailNo].type; //Convert.ToString(dtDetails.Rows[detailNo]["type"]);
                                                                            //            double Details_Amount = response.hotels[i].rooms[roomNo].rates[r].taxes.details[detailNo].amount; // Convert.ToDouble(dtDetails.Rows[detailNo]["amount"]);
                                                                            //            TaxInfo = TaxInfo + " <br> " + Details_Type + " Tax=" + Details_Amount;
                                                                            //        }
                                                                            //    //}
                                                                            //}
                                                                            string feesNotIncluded = "";
                                                                            //if (response.hotels[i].rooms[roomNo].rates[r].feesNotIncluded != null)//(drTaxes.Length > 0)
                                                                            //{

                                                                            //    double feesNotIncluded_Total = response.hotels[i].rooms[roomNo].rates[r].feesNotIncluded.total;//Convert.ToDouble(drTaxes[0]["total"]);
                                                                            //    //string Taxes_Id = Convert.ToString(drTaxes[0]["taxes_Id"]);
                                                                            //    if (feesNotIncluded_Total > 0)
                                                                            //    {
                                                                            //        feesNotIncluded = "<br><br> <b> feesNotIncluded: </b> <br>";
                                                                            //        feesNotIncluded = feesNotIncluded+  "Total=" + feesNotIncluded_Total;
                                                                            //    }
                                                                            //    //DataRow[] drDetails = PriceTravel_Response.Tables["details"].Select("taxes_Id='" + Taxes_Id + "'");

                                                                            //    //DataTable dtDetails = null;

                                                                            //    //if (response.hotels[i].rooms[roomNo].rates[r].taxes.details.Count > 0) //(drDetails.Length > 0)
                                                                            //    //{
                                                                            //    //dtDetails = drDetails.CopyToDataTable();

                                                                            //    for (int detailNo = 0; detailNo < response.hotels[i].rooms[roomNo].rates[r].feesNotIncluded.details.Count; detailNo++)
                                                                            //    {
                                                                            //        string Details_Type = response.hotels[i].rooms[roomNo].rates[r].feesNotIncluded.details[detailNo].type; //Convert.ToString(dtDetails.Rows[detailNo]["type"]);
                                                                            //        double Details_Amount = response.hotels[i].rooms[roomNo].rates[r].feesNotIncluded.details[detailNo].amount; // Convert.ToDouble(dtDetails.Rows[detailNo]["amount"]);
                                                                            //        feesNotIncluded = feesNotIncluded + " <br> " + "type=" + Details_Type + " <br> " + " <br> Amount=" + Details_Amount;
                                                                            //    }
                                                                            //    //}
                                                                            //}

                                                                            //string EssentialInfo = TaxInfo + feesNotIncluded;
                                                                            DateTime OnlyDate = DateTime.Now.Date;
                                                                            if (response.hotels[i].rooms[roomNo].rates[r].cancellationPolicies.Count > 0)//(drCancellationPolicies.Length > 0)
                                                                                OnlyDate = response.hotels[i].rooms[roomNo].rates[r].cancellationPolicies[0].startDate; //Convert.ToDateTime(drCancellationPolicies[0]["startDate"]);

                                                                            double Penalty = 0;
                                                                            #region Cancellation Policy Logic and Row
                                                                            DateTime CancelDate = DateTime.Now.Date;
                                                                            CancelDate = OnlyDate;
                                                                            #region Vat
                                                                            double VatOnPolicy = 0;
                                                                            double MainAgentCharge = publicAmount;
                                                                            double CompanyChargeAmount = publicAmount;
                                                                            double MainAgentCharge_wl = publicAmount;
                                                                            double CancelCharge_wl = publicAmount;

                                                                            Penalty = _markup.ProcessHotelCancelationPrice_WithMarkup(ref MainAgentCharge, SearchParam.NoofNights, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), CompanyBuying, isPerce, B2CMarkup, AgentPercentage, AgentMarkupAmount, IsParentId, SubAgentMarkup, ref VatOnPolicy);
                                                                            #endregion

                                                                            DateTime NowDate = DateTime.Now.Date;
                                                                            if (CancelDate.AddDays(CutOffDays).Date > DateTime.Now.Date)
                                                                                CancelDate = CancelDate.AddDays(CutOffDays).Date;
                                                                            else
                                                                                CancelDate = DateTime.Now.Date;

                                                                            if (NowDate < CancelDate)
                                                                            {
                                                                                double zero = 0;
                                                                                #region new

                                                                                dt_PriceTravel_Cancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref zero, dt_PriceTravel_Cancellation.NewRow(), CurrencyFactor, currency, Item.Supplier, Convert.ToString(OwnsystemHotelId), Convert.ToString(RoomNo), Convert.ToString(roomTypeId), RoomName, MealPlanDescription, Convert.ToDateTime(NowDate), CancelDate.AddDays(-1), 0, 0, "0", VatOnPolicy, uniqid, "NO", "", "", "", ""));
                                                                                //dt_PriceTravel_Cancellation.AcceptChanges();


                                                                                dt_PriceTravel_Cancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref CompanyChargeAmount, dt_PriceTravel_Cancellation.NewRow(), CurrencyFactor, currency, Item.Supplier, Convert.ToString(OwnsystemHotelId), Convert.ToString(RoomNo),
                                                                                Convert.ToString(roomTypeId), RoomName, MealPlanDescription, Convert.ToDateTime(CancelDate), DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null), MainAgentCharge, Penalty, "0", VatOnPolicy, uniqid, "NO", "", "", "", ""));
                                                                                dt_PriceTravel_Cancellation.AcceptChanges();

                                                                                #endregion

                                                                            }
                                                                            else
                                                                            {
                                                                                dt_PriceTravel_Cancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref CompanyChargeAmount, dt_PriceTravel_Cancellation.NewRow(), CurrencyFactor, currency, Item.Supplier, Convert.ToString(OwnsystemHotelId), Convert.ToString(RoomNo),
                                                                Convert.ToString(roomTypeId), RoomName, MealPlanDescription, NowDate, DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null), MainAgentCharge, Penalty, "0", VatOnPolicy, uniqid, "Yes", "", ""));

                                                                                dt_PriceTravel_Cancellation.AcceptChanges();
                                                                            }
                                                                            #endregion

                                                                            //DataRow[] drTaxes = PriceTravel_Response.Tables["taxes"].Select("rates_id='" + rates_Id + "'");


                                                                            #region Ratebreakup
                                                                            //for (int n = 0; n < SearchParam.NoofNights; n++)
                                                                            //{
                                                                            //    string CheckInDate = Convert.ToString(SearchParam.CheckInDate);
                                                                            //    string CheckOutDate = Convert.ToString(SearchParam.CheckOutDate);
                                                                            //    DateTime dtCheckInDate = DateTime.ParseExact(CheckInDate, "dd/MM/yyyy", null);
                                                                            //    DateTime dtCheckOutDate = DateTime.ParseExact(CheckOutDate, "dd/MM/yyyy", null);
                                                                            //    TimeSpan t = dtCheckOutDate - dtCheckInDate;
                                                                            //    double ratePerNight = CompanyBuying;

                                                                            //    #region Vat
                                                                            //    double VatOnPerNight = 0;
                                                                            //    ratePerNight = _markup.ProcessHotelRateBreakupPrice_WithMarkup(1, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), ratePerNight, B2CMarkup, IsParentId, isPerce, AgentPercentage, AgentMarkupAmount, SubAgentMarkup, ref VatOnPerNight);
                                                                            //    #endregion

                                                                            //    ratePerNight = ratePerNight / SearchParam.NoofNights;
                                                                            //    DataRow drRateBreackUp = Dt_PriceTravel_RateBreackUp.NewRow();
                                                                            //    drRateBreackUp["SupplierName"] = Supplier;
                                                                            //    drRateBreackUp["OwnsystemHotelId"] = OwnsystemHotelId;
                                                                            //    drRateBreackUp["ContractID"] = 0;
                                                                            //    drRateBreackUp["RoomNo"] = Convert.ToString(1);
                                                                            //    drRateBreackUp["RoomTypeId"] = roomTypeId;
                                                                            //    if (ResPolici_Type == "MEAL_PLAN")
                                                                            //    {
                                                                            //        drRateBreackUp["MealId"] = ResPolici_Description;
                                                                            //        drRateBreackUp["Meal"] = ResPolici_Description;
                                                                            //    }
                                                                            //    string rateWeekColumn = Convert.ToString(dtCheckInDate.Date.AddDays(n).DayOfWeek).Substring(0, 3);
                                                                            //    drRateBreackUp["DateOfWeek"] = rateWeekColumn;
                                                                            //    DateTime rateDate = dtCheckInDate.Date.AddDays(n);
                                                                            //    drRateBreackUp["Date"] = rateDate;
                                                                            //    drRateBreackUp["Rate"] = ratePerNight;
                                                                            //    drRateBreackUp["OrderNo"] = 1;
                                                                            //    drRateBreackUp["RoomTypeName"] = RoomName;
                                                                            //    drRateBreackUp["DateOfWeek"] = rateWeekColumn;
                                                                            //    drRateBreackUp["Date"] = rateDate;
                                                                            //    drRateBreackUp["BookingStatus"] = "Available";
                                                                            //    drRateBreackUp["BoardTypeID"] = "";
                                                                            //    drRateBreackUp["VatdiffAmount"] = 0;
                                                                            //    drRateBreackUp["RateType"] = "A Rate";
                                                                            //    drRateBreackUp["SupplierCurrency"] = clientCurrency;
                                                                            //    drRateBreackUp["BuyingPrice"] = CompanyBuying;
                                                                            //    Dt_PriceTravel_RateBreackUp.Rows.Add(drRateBreackUp);
                                                                            //}
                                                                            #endregion
                                                                            //DataRow[] drFeesNotIncluded = PriceTravel_Response.Tables["feesNotIncluded"].Select("rates_id='" + rates_Id + "'");

                                                                            //if (drFeesNotIncluded.Length > 0)
                                                                            //{
                                                                            //    double FeesNotIncluded_Total = Convert.ToDouble(drFeesNotIncluded[0]["total"]);
                                                                            //    string FeesNotIncluded_Currency = Convert.ToString(drFeesNotIncluded[0]["currency"]);
                                                                            //}
                                                                            #region Rooms
                                                                            DataRow drRoom = Dt_PriceTravel_HotelRoom.NewRow();
                                                                            drRoom["OwnsystemHotelId"] = OwnsystemHotelId;
                                                                            drRoom["RoomTypeCode"] = RoomCode;
                                                                            //if (ResPolici_Type == "MEAL_PLAN")
                                                                            {
                                                                                drRoom["MealId"] = ResPolici_Description;
                                                                                drRoom["MealName"] = ResPolici_Description;
                                                                                drRoom["BoardType"] = ResPolici_Description;
                                                                            }

                                                                            drRoom["Type"] = "";
                                                                            drRoom["Board"] = "";
                                                                            drRoom["BookingStatus"] = "Available";
                                                                            drRoom["UniqueNo"] = Guid.NewGuid().ToString();
                                                                            drRoom["ContractID"] = 0;
                                                                            drRoom["RoomNo"] = Convert.ToString(1);
                                                                            drRoom["BoardTypeID"] = "";
                                                                            drRoom["AdultsOccupancy"] = TotalAdult;
                                                                            drRoom["ChildOccupancy"] = TotalChild;
                                                                            drRoom["OfferCode"] = "";
                                                                            drRoom["OfferDescription"] = "";
                                                                            drRoom["RoomSequenceNo"] = "";
                                                                            //string EssentialInformation = "";
                                                                            drRoom["EssentialInformation"] = ""; //EssentialInfo;
                                                                            drRoom["Characteristic"] = "";
                                                                            bool IsCombine = true;
                                                                            bool IsDynamicPolicy = true;
                                                                            #region Vat
                                                                            double itemprice = CompanyBuying;
                                                                            _markup.ProcessHotelRoom_WithMarkup(CurrencyFactor, currency, IsDynamicPolicy, IsCombine, SearchParam.NoofNights * SearchParam.Rooms.Count, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), SearchParam.CheckInDate, SearchParam.CheckOutDate, ref itemprice, ref drRoom, isPerce, AgentPercentage, AgentMarkupAmount, SubAgentMarkup, B2CMarkup, IsParentId);
                                                                            #endregion
                                                                            drRoom["SupplierName"] = Supplier;
                                                                            drRoom["RoomTypeName"] = RoomName;
                                                                            drRoom["RoomTypeId"] = RoomCode;
                                                                            Dt_PriceTravel_HotelRoom.Rows.Add(drRoom);
                                                                            #endregion

                                                                        }//rate

                                                                    }//room loop

                                                                } //end room


                                                            }
                                                        }

                                                    }

                                                }

                                                #endregion


                                            }
                                            catch (Exception ex)
                                            {
                                                _ = _exception.Error(ex);
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            _ = _exception.Error(ex);
                                        }

                                        //dsTemp.Tables.Add(dtParsecRoom);
                                        //dsTemp.Tables.Add(dtParsecCacelationPolicy);
                                        //dsTemp.Tables.Add(dtParsecRoomRateBreakup);

                                        //  return dsTemp;

                                        #endregion

                                    }
                                    decimal LowestPrice = Convert.ToDecimal(MinPrice).CalculatePrice(commonSearch.tokenData, commonSearch.advancedOptions, currency);
                                    Options ops = (await _supplierRes.GetCommonRoomForSuppliers(GiataId, commonSearch.tokenData, commonSearch.advancedOptions, Convert.ToDouble(LowestPrice), drXmlHotel, Dt_PriceTravel_HotelRoom, dt_PriceTravel_Cancellation, Dt_PriceTravel_RateBreackUp, SearchParam));
                                    if (ops != null)
                                        options.Add(ops);

                                }


                            }

                        }

                    }

                }


            }
            catch (Exception ex)
            {
                Item.ResponseStatus = 404;
                Item.ResponseType = "NoCount";
                _ = _exception.Error(ex, SearchParam.SearchKey);
                //return Common.GenerateDataTableNewV2();
            }
            finally
            {
                try
                {
                    timerroomx.Stop();
                    Item.CodeProcessingTimeSupplier = timerroomx.ElapsedMilliseconds;
                    if (Item.ResponseType == "NoCount" && StoreSupplierResErrorLog == "1")
                        _ = _commonMethod.XMLRequestResponseTrack_Error(Item.RQRS[0].Request, Item.RQRS[0].Response, Item.Supplier, timerroomx.Elapsed.ToString(), "", SearchParam.SearchKey);
                }
                catch (Exception x)
                {
                    await _exception.Error(x);
                }
            }
            return options;
        }

        public async Task<string> PriceTravel_EssentialInfoOnly(string RoomTypeId, DataRow[] PriceTravelDetails, TokenData tokenData)
        {
            string Remark = "";
            try
            {
                string[] HTL_RoomCode = RoomTypeId.Split(new String[] { "$$$" }, StringSplitOptions.None);
                double CurrencyFactorPriceTravel = 1; 
                string PriceTravelCurrency = Convert.ToString(PriceTravelDetails[0]["CurrencyShortName"]);
                if(PriceTravelCurrency.ToLower() != tokenData.SiteCurrency.ToLower())
                {
                    CurrencyFactorPriceTravel = _commonMethod.GetROEByCurrency(PriceTravelCurrency, tokenData.dtROE);

                }

                string PriceTravelToken = HTL_RoomCode[7];
                string rateKey = HTL_RoomCode[8];
                //string pricetravelToken = "";
                string Request = PriceTravelPolicyRequest(rateKey);
                string Error = "";
                ResponseDto responseDto = await GetPriceTravelSupplierResponse(Error, PriceTravelToken, Convert.ToString(PriceTravelDetails[0]["WebSiteURL"] + "hotels/checkrates"), Convert.ToString(PriceTravelDetails[0]["UserName"]), Convert.ToString(PriceTravelDetails[0]["Password"]), 60000, Request, "", Error);

                string ResponseData = responseDto.Response;
                if (ResponseData == "" && ResponseData.Trim() == "")
                    return "";
                string ConvertXml = Newtonsoft.Json.JsonConvert.DeserializeXmlNode("{\"Row\":" + ResponseData + "}", "root").InnerXml;
                //  DataSet dsHotelBeds = new DataSet();
                //  dsHotelBeds = GetDataSetFromXMLString(ConvertXml);
                DataSet dsPriceTravels = new DataSet();
                dsPriceTravels = _commonMethod.GetDataSetFromXMLString(ConvertXml);
                if (dsPriceTravels != null && dsPriceTravels.Tables.Contains("hotel"))
                {
                    if (dsPriceTravels.Tables["hotel"].Rows[0]["remarks"] != null)
                    {
                        Remark = Convert.ToString(dsPriceTravels.Tables["hotel"].Rows[0]["remarks"]);
                    }
                }
            }
            catch (Exception e)
            {
                _ = _exception.ErrorNew(e,ErrorLog._LogAPI.Recheck);
            }
            return Remark;
        }
        public async Task<ResponseDto> GetPriceTravelSupplierResponse(string Error_str, string priceTravelToken, string strHost, string Username, string Password, Int32 TimeOut, string strxmlRQRS, string methodtype, string Error, string Provider = "", HotelSearchParameter para = null, bool DebugMode = false)
        {
            ResponseDto responseDto = new ResponseDto();
            try
            {

                // priceTravelToken priceTravelToken = JsonConvert.DeserializeObject<priceTravelToken>(PriceTravelTokenGeneration(strHost, Username, Password));

                //strHost = strHost.Replace("https", "http");
                //var client = new RestClient(strHost);
                //client.Timeout = -1;
                //var request = new RestRequest(Method.POST);
                //ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                //request.AddHeader("Authorization", "Bearer " + priceTravelToken);
                //request.AddHeader("Content-Type", "application/json");
                //request.AddHeader("Accept", "application/json");
                //request.AddHeader("Accept-Encoding", "gzip");
                //request.AddParameter("application/json", strxmlRQRS, ParameterType.RequestBody);
                //IRestResponse response = await client.ExecuteAsync(request);
                ////var resp = response.Content;

               

                // Convert https to http if needed
                strHost = strHost.Replace("https", "http");

                // New RestSharp client
                var options = new RestClientOptions(strHost)
                {
                    ThrowOnAnyError = false,
                    MaxTimeout = TimeOut > 0 ? TimeOut * 1000 : -1
                };

                var client = new RestClient(options);

                var request = new RestRequest("", methodtype.ToLower() == "delete" ? Method.Delete : Method.Post);
                request.AddHeader("Authorization", "Bearer " + priceTravelToken);
                request.AddHeader("Content-Type", "application/json");
                request.AddHeader("Accept", "application/json");
                request.AddHeader("Accept-Encoding", "gzip");

                request.AddStringBody(strxmlRQRS, DataFormat.Json);

                // Execute request
                var response = await client.ExecuteAsync(request);

                responseDto.Response = response.Content;
                //responseDto.Error = response.ErrorMessage;
            }
            catch (Exception ex)
            {
                _ = _exception.Error(ex);
                Error_str = ex.Message;
                responseDto.Error = Error_str;
                responseDto.Response = "";
            }

            return responseDto;


        }

        public async Task<PriceCheckCMDto> PriceUpdateHotelPriceTravel(string[] HTL_RoomCode, DataTable dtCheckPriceReqStructure, DataSet dsCartDetails, DataRow[] PriceTravelDetails, string UniqueNo, string AgentUniqueNo, int i, string IsFor, double CompanyBuyPrice, bool IsError, double itemprice, string RequestSPR, string ReponseSPR, TokenData tokenData)
        {
            PriceCheckCMDto priceCheckCMDto = new PriceCheckCMDto();
            try
            {
                string SupplierName = Convert.ToString(dsCartDetails.Tables[1].DefaultView[i]["SupplierName"]);
                string PriceTravelCurrency = Convert.ToString(PriceTravelDetails[0]["CurrencyShortName"]);
                double CurrencyFactorPriceTravel = 1;
                if (PriceTravelCurrency.ToLower() != tokenData.SiteCurrency.ToLower())
                {
                    CurrencyFactorPriceTravel = _commonMethod.GetROEByCurrency(PriceTravelCurrency, tokenData.dtROE);
                }
                string UserCartId = Convert.ToString(dsCartDetails.Tables[1].DefaultView[0]["UserCartID"]);
                string PriceTravelToken = HTL_RoomCode[7];
                string rateKey = HTL_RoomCode[8];
                //string pricetravelToken = "";
                string Request = PriceTravelPolicyRequest(rateKey);
                RequestSPR = Request;
                string Error = "";
                _commonMethod.ThirdPartyBookingHistory(Convert.ToString(UniqueNo), Convert.ToString(AgentUniqueNo), Request, "-", "Before Request hit Price Check" + IsFor, Convert.ToString(dsCartDetails.Tables[1].DefaultView[i]["SupplierName"]), "Request Confirm", "-", "RaynaB2B");
                dtCheckPriceReqStructure.Rows.Add(_commonMethod.AddCheckPriceReq(dtCheckPriceReqStructure.NewRow(), i + 1, Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[i]["UserCartID"]), Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[i]["UserCartDetailId"]), Convert.ToString(dsCartDetails.Tables[1].DefaultView[i]["RoomTypeID"]), Convert.ToString(dsCartDetails.Tables[1].DefaultView[i]["SupplierName"]).ToLower(), Request));
                ResponseDto responseDto  = await GetPriceTravelSupplierResponse(Error, PriceTravelToken, Convert.ToString(PriceTravelDetails[0]["WebSiteURL"] + "hotels/checkrates"), Convert.ToString(PriceTravelDetails[0]["UserName"]), Convert.ToString(PriceTravelDetails[0]["Password"]), 60000, Request, "", Error);

                string ResponseData = responseDto.Response;
                Error = responseDto.Error;

                if (ResponseData == null)
                    ResponseData = Error;
                _commonMethod.ThirdPartyBookingHistory(Convert.ToString(UniqueNo), Convert.ToString(AgentUniqueNo), Request, ResponseData, "After Request hit Price Check" + IsFor, SupplierName, "Response Confirm", "-", "RaynaB2B");
                ReponseSPR = ResponseData;
                // if (ResponseData.ToLower().Contains("error") || ResponseData.Trim() == "")
                //   return true;
                if (ResponseData.Trim() == "" || Error != "")
                {
                    priceCheckCMDto.Flag = true;

                }
                string ConvertXml = Newtonsoft.Json.JsonConvert.DeserializeXmlNode("{\"Row\":" + ResponseData + "}", "root").InnerXml;
                //  DataSet dsHotelBeds = new DataSet();
                //  dsHotelBeds = GetDataSetFromXMLString(ConvertXml);
                DataSet dsPriceTravels = new DataSet();
                dsPriceTravels = _commonMethod.GetDataSetFromXMLString(ConvertXml);
                if (dsPriceTravels != null && dsPriceTravels.Tables.Contains("hotel"))
                {
                    if (dsPriceTravels.Tables["hotel"].Rows[0]["remarks"] != null)
                    {
                        string Remark = Convert.ToString(dsPriceTravels.Tables["hotel"].Rows[0]["remarks"]);
                        string Hotelname = Convert.ToString(dsPriceTravels.Tables["hotel"].Rows[0]["name"]);
                        _commonMethod.GetTypeUpdateStaus_PriceTravel(Convert.ToInt32(UserCartId), Remark);
                    }
                }
                string[] Array = Convert.ToString(dsCartDetails.Tables[1].DefaultView[i]["RoomTypeID"]).Split(new String[] { "$$$" }, StringSplitOptions.None);
                string SearchRoomName = Array[2];
                string RoomName = "";
                //if (dsPriceTravels != null && dsPriceTravels.Tables.Contains("rooms"))
                //{
                //    if (dsPriceTravels.Tables["rooms"].Rows.Count > 0)
                //    {
                //        for (int j = 0; j < dsPriceTravels.Tables["rooms"].Rows.Count; j++)
                //        {
                //            RoomName = Convert.ToString(dsPriceTravels.Tables["rooms"].Rows[j]["name"]);
                //            break;
                //        }
                //    }
                //}
                if (true)//(SearchRoomName.ToLower() == RoomName.ToLower())
                {
                    for (int j = 0; j < dsPriceTravels.Tables["rates"].Rows.Count; j++)
                    {
                        //itemprice = Convert.ToDouble(dsPriceTravels.Tables["rates"].Rows[j]["publicAmount"]) * CurrencyFactorPriceTravel;
                        itemprice = Convert.ToDouble(dsPriceTravels.Tables["rates"].Rows[j]["amount"]) * CurrencyFactorPriceTravel;
                        priceCheckCMDto.ItemPrice = itemprice;

                        string NewRateKey = Convert.ToString(dsPriceTravels.Tables["rates"].Rows[j]["rateKey"]);
                        _commonMethod.GetTypeUpdateRateKey_PriceTravel(Convert.ToInt32(UserCartId), NewRateKey);
                        break;
                    }
                    if (IsFor == "P")
                    {
                        if (Convert.ToDouble((itemprice).TruncateDecimalPlaces(0)) > (CompanyBuyPrice).TruncateDecimalPlaces(0))
                        {
                            priceCheckCMDto.Flag = true;
                            return priceCheckCMDto;
                        }
                    }
                    else
                    {
                        if (Convert.ToDouble((itemprice).TruncateDecimalPlaces(0)) > (CompanyBuyPrice).TruncateDecimalPlaces(0))
                        {
                            priceCheckCMDto.Flag = true;
                            return priceCheckCMDto;

                        }
                    }
                }
                else
                {
                    priceCheckCMDto.Flag = true;
                    return priceCheckCMDto;
                }
            }
            catch (Exception e)
            {
                _ = _exception.ErrorNew(e, ErrorLog._LogAPI.Prebook);
                IsError = true;
            }
            priceCheckCMDto.Flag = false;
            return priceCheckCMDto;
        }

        public string PriceTravelPolicyRequest(string RateKey)
        {
            try
            {
                StringBuilder PriceTravel = new StringBuilder();
                PriceTravel.Append("{");
                PriceTravel.Append("\"rateKey\":\"" + RateKey + "\"");
                PriceTravel.Append("}");
                string request = PriceTravel.ToString();
                return request;
            }
            catch (Exception ex)
            {
                return "";
            }
        }

        public async Task<PriceTravelBookRes> PriceTravelBookingHoldVerified(string[] DsCartDetailsArray, DataTable dtCheckPriceReqStructure, DataTable dtCurrentCartDetails/*DataSet dsCartDetails*/, DataRow[] PriceTravelDetails, string UniqueNo, string AgentUniqueNo, int i, string IsFor, bool IsError, string purchaseToken_pricetravel, TokenData tokenData)
        {
            PriceTravelBookRes priceTravelBookRes = new PriceTravelBookRes();
            try
            {
                string currHotelId = DsCartDetailsArray[1];
                string currHomeName = DsCartDetailsArray[2];
                string currRoomId = DsCartDetailsArray[3];
                string currRoomName = DsCartDetailsArray[4];

                string ClientCurrency = DsCartDetailsArray[5];
                double PublicAmount = Convert.ToDouble(DsCartDetailsArray[6]);

                string PriceTravelToken = DsCartDetailsArray[7];
                string PriceTravelRatekey = purchaseToken_pricetravel;// DsCartDetailsArray[8];

                double CurrencyFactorHotelBeds = 1;
                //  DataTable dtInrCurrency = Common.GetCountryCurrency(Convert.ToString("USD"));
                if (ClientCurrency.ToLower() != tokenData.SiteCurrency.ToLower())
                {
                    CurrencyFactorHotelBeds = _commonMethod.GetROEByCurrency(ClientCurrency, tokenData.dtROE);

                }
               
                string Request = PriceTravelPolicyRequest(PriceTravelRatekey);
                string WebSiteURL = Convert.ToString(PriceTravelDetails[0]["WebSiteURL"]) + "bookings/hold";

                string Error = "";

                _commonMethod.ThirdPartyBookingHistory(Convert.ToString(UniqueNo), Convert.ToString(AgentUniqueNo), Request, "-", "Before Request hit for bookingkey" + IsFor, "pricetravel", "Request Confirm", "-", "RaynaB2B");

                //dtCheckPriceReqStructure.Rows.Add(AddCheckPriceReq(dtCheckPriceReqStructure.NewRow(), i + 1, Convert.ToInt32(dtCurrentCartDetails.DefaultView[i]["UserCartID"]), Convert.ToInt32(dtCurrentCartDetails.DefaultView[i]["UserCartDetailId"]), Convert.ToString(dtCurrentCartDetails.DefaultView[i]["RoomTypeID"]), Convert.ToString(dtCurrentCartDetails.DefaultView[i]["SupplierName"]).ToLower(), Request));

                ResponseDto responseDto = await GetPriceTravelSupplierResponse(Error, PriceTravelToken, WebSiteURL, "", "", 60000, Request, "", Error);
                string ResponseData = responseDto.Response;
                Error = responseDto.Error;
                _commonMethod.ThirdPartyBookingHistory(Convert.ToString(UniqueNo), Convert.ToString(AgentUniqueNo), Request, ResponseData + Error, "After Request hit for bookingkey" + IsFor, "pricetravel", "Response Confirm", "-", "RaynaB2B");


                PriceTravelBookingIds PriceTravelBookingResponseData = Newtonsoft.Json.JsonConvert.DeserializeObject<PriceTravelBookingIds>(ResponseData);



                ////string root = @"D:\PricetravelJson\bookingHold";
                //if (!Directory.Exists(root))
                //{
                //    Directory.CreateDirectory(root);
                //}
                //File.WriteAllText(root + "bookingHoldRequest.txt", Request);
                //File.WriteAllText(root + "bookingHoldResponse.txt", ResponseData);

                priceTravelBookRes.bookingKey = PriceTravelBookingResponseData.bookingKey;
                return priceTravelBookRes;


            }
            catch (Exception e)
            {
                _ =_exception.ErrorBooking(e, "");
                priceTravelBookRes.IsError = true;
            }
            priceTravelBookRes.bookingKey = "";
            return priceTravelBookRes;
        }

        public string PriceTravelBookindgRequestModified(string BookingKey, DataTable dtPassengers, DataSet dsCartDetails, bool IsError, int j, string AgentCode, int AgentId = 0)
        {

            try
            {
                StringBuilder PriceTravelSB = new StringBuilder();
                // if (IsError == false)
                //  {
                //string RoomTypeId = Convert.ToString(dsCartDetails.Tables[1].DefaultView[x]["RoomTypeId"]);
                string[] Array = Convert.ToString(dsCartDetails.Tables[1].DefaultView[0]["RoomTypeID"]).Split(new String[] { "$$$" }, StringSplitOptions.None);
                string BookingCodeHotel = Array[1];
                //#uncomment 
                //string InternalReference = Convert.ToString(Common.getSession("AgentCode")) + "_" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[0]["UserCartDetailId"]);
                string InternalReference = AgentCode + "_" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[0]["UserCartDetailId"]);

                string currUserCartID = Convert.ToString(dsCartDetails.Tables[1].DefaultView[0]["UserCartID"]);

                //string InternalReference = Convert.ToString(Common.getSession("AgentCode")) + "_" + currUserCartID;

                dtPassengers.DefaultView.RowFilter = "PickUp='" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[0]["RoomTypeId"]) + "' and UserCartID='" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[0]["UserCartID"]) + "'";
                //DataTable dtnewpassanger = dtPassengers.DefaultView.ToTable();

                //-- dtPassengers.DefaultView.RowFilter = "PickUp='" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomTypeId"]) + "' and UserCartID='" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["UserCartID"]) + "' and RoomNo='" + Convert.ToInt32(j + 1) + "'";
                //---   DataTable dtnewpassanger = dtPassengers.DefaultView.ToTable();

                //DataRow[] drAdultPassager = dtnewpassanger.Select("PaxType = 'Adult' and UserCartId=" + currUserCartID);
                // DataRow[] drChildPassager = dtnewpassanger.Select("PaxType = 'Child' and UserCartId=" + currUserCartID);

                //SupplierName == pricetravel


                //DataRow[] drCartDetails_new = dsCartDetails.Tables[1].DefaultView[0].Row;
                DataTable dtCartDetails = null;
                if (dsCartDetails.Tables[1].Rows.Count > 0)
                {
                    //dtCartDetails = dsCartDetails.Tables[1].DefaultView[0];// drCartDetails_new.CopyToDataTable();

                    PriceTravelSB.Append("{");
                    PriceTravelSB.Append("\n\"bookingKey\":\"" + BookingKey + "\",");

                    PriceTravelSB.Append("\n\"clientReference\":\"" + InternalReference + "\",");
                    PriceTravelSB.Append("\n\"roomCriteria\":[");

                    for (int RoomNo = 0; RoomNo < dsCartDetails.Tables[1].Rows.Count; RoomNo++)
                    {
                        //int NoOfAdults = Convert.ToInt16(dtCartDetails.Rows[RoomNo]["NoOfAdults"]);
                        //int NoOfChild = Convert.ToInt16(dtCartDetails.Rows[RoomNo]["NoOfChild"]);
                        //int ChildAge1 = Convert.ToInt16(dtCartDetails.Rows[RoomNo]["ChildAge1"]);
                        //int ChildAge2 = Convert.ToInt16(dtCartDetails.Rows[RoomNo]["ChildAge2"]);

                        PriceTravelSB.Append("{");
                        PriceTravelSB.Append("\n\"paxes\":[");

                        //for(int AdultsCount = 0; AdultsCount < NoOfAdults; AdultsCount++)
                        // DataRow[] drAdultPassager = dtnewpassanger.Select("PaxType = 'Adult' and RoomNo=" + (RoomNo + 1));
                        // DataRow[] drChildPassager = dtnewpassanger.Select("PaxType = 'Child' and RoomNo=" + (RoomNo + 1));/ {

                        DataRow[] drAdultPassager = dtPassengers.Select("PaxType = 'Adult' and RoomNo=" + (RoomNo + 1));
                        DataRow[] drChildPassager = dtPassengers.Select("PaxType = 'Child' and RoomNo=" + (RoomNo + 1));

                        DataTable dtAdultPassager = null;
                        if (drAdultPassager.Length > 0)
                        {
                            dtAdultPassager = drAdultPassager.CopyToDataTable();

                            for (int AdultNo = 0; AdultNo < dtAdultPassager.Rows.Count; AdultNo++)
                            {
                                PriceTravelSB.Append("{");

                                // int adultAge = 21 + AdultNo;
                                int adultAge = 30;
                                string adultname = Convert.ToString(dtAdultPassager.Rows[AdultNo]["FirstName"]);
                                string adultsurname = Convert.ToString(dtAdultPassager.Rows[AdultNo]["LastName"]);

                                PriceTravelSB.Append("\n\"age\":" + adultAge + ",");
                                PriceTravelSB.Append("\n\"name\":\"" + adultname + "\",");
                                PriceTravelSB.Append("\n\"surname\":\"" + adultsurname + "\"");

                                if ((AdultNo + 1) == drAdultPassager.Length && drChildPassager.Length == 0)
                                {
                                    PriceTravelSB.Append("}");
                                }
                                else if ((AdultNo + 1) < drAdultPassager.Length && drChildPassager.Length == 0)
                                {
                                    PriceTravelSB.Append("},");
                                }
                                else if ((AdultNo + 1) == drAdultPassager.Length && drChildPassager.Length > 0)
                                {
                                    PriceTravelSB.Append("},");

                                }

                                else if ((AdultNo + 1) < drAdultPassager.Length && drChildPassager.Length > 0)
                                {
                                    PriceTravelSB.Append("},");
                                }
                            }



                            DataTable dtChildPassage = null;

                            if (drChildPassager.Length > 0)
                            {

                                dtChildPassage = drChildPassager.CopyToDataTable();

                                for (int childNo = 0; childNo < dtChildPassage.Rows.Count; childNo++)
                                {

                                    PriceTravelSB.Append("{");

                                    int childAge = Convert.ToInt16(dtChildPassage.Rows[childNo]["ChildAge"]);
                                    string childname = Convert.ToString(dtChildPassage.Rows[childNo]["FirstName"]);
                                    string childsurname = Convert.ToString(dtChildPassage.Rows[childNo]["LastName"]);

                                    PriceTravelSB.Append("\n\"age\":" + childAge);

                                    if ((childNo + 1) == drChildPassager.Length)
                                    {
                                        PriceTravelSB.Append("}");

                                    }
                                    else if ((childNo + 1) < drChildPassager.Length)
                                    {
                                        PriceTravelSB.Append("},");
                                    }

                                }

                            }

                        }

                        //}


                        //dtCartDetails.Rows.Count; RoomNo++
                        //if ((RoomNo + 1) == dsCartDetails.Tables[1].DefaultView.Count)
                        if (RoomNo == 0 && dsCartDetails.Tables[1].DefaultView.Count == 1)
                        {

                            PriceTravelSB.Append("\n]");//paxes
                            PriceTravelSB.Append("}");

                        }
                        else// if ((RoomNo + 1) < dsCartDetails.Tables[1].DefaultView.Count)
                        {
                            if ((RoomNo + 1) == dsCartDetails.Tables[1].DefaultView.Count)
                            {
                                PriceTravelSB.Append("\n]");//paxes
                                PriceTravelSB.Append("}");
                            }
                            else
                            {
                                PriceTravelSB.Append("\n]");
                                PriceTravelSB.Append("},");
                            }
                        }

                    }
                    PriceTravelSB.Append("\n],");//paxes




                    //--

                    //    if (dtnewpassanger.Rows.Count > 0)
                    //{

                    //    PriceTravelSB.Append("{");

                    //    PriceTravelSB.Append("\n\"bookingKey\":\"" + BookingKey + "\",");
                    //    PriceTravelSB.Append("\n\"clientReference\":\"" + InternalReference + "\",");

                    //    PriceTravelSB.Append("\n\"roomCriteria\":[");

                    //    //PriceTravelSB.Append("{");


                    //    DataTable dtAdultPassager = null;
                    //    if (drAdultPassager.Length > 0)
                    //    {
                    //        dtAdultPassager = drAdultPassager.CopyToDataTable();
                    //        PriceTravelSB.Append("{");

                    //        PriceTravelSB.Append("\n\"paxes\":[");

                    //        for (int adultNo = 0; adultNo < dtAdultPassager.Rows.Count; adultNo++)
                    //        {

                    //            PriceTravelSB.Append("{");

                    //            int adultAge = 21 + adultNo;
                    //            string adultname = Convert.ToString(dtAdultPassager.Rows[adultNo]["FirstName"]);
                    //            string adultsurname = Convert.ToString(dtAdultPassager.Rows[adultNo]["LastName"]);

                    //            PriceTravelSB.Append("\n\"age\":" + adultAge + ",");
                    //            PriceTravelSB.Append("\n\"name\":\"" + adultname + "\",");
                    //            PriceTravelSB.Append("\n\"surname\":\"" + adultsurname + "\"");


                    //            if ((adultNo + 1) == drAdultPassager.Length && drChildPassager.Length == 0)
                    //            {
                    //                PriceTravelSB.Append("}");
                    //            }
                    //            else if ((adultNo + 1) < drAdultPassager.Length && drChildPassager.Length == 0)
                    //            {
                    //                PriceTravelSB.Append("},");
                    //            }
                    //            else if ((adultNo + 1) == drAdultPassager.Length && drChildPassager.Length > 0)
                    //            {
                    //                PriceTravelSB.Append("},");

                    //            }

                    //            else if ((adultNo + 1) < drAdultPassager.Length && drChildPassager.Length > 0)
                    //            {
                    //                PriceTravelSB.Append("},");
                    //            }

                    //        }


                    //        DataTable dtChildPassage = null;

                    //        if (drChildPassager.Length > 0)
                    //        {


                    //            dtChildPassage = drChildPassager.CopyToDataTable();

                    //            for (int childNo = 0; childNo < dtChildPassage.Rows.Count; childNo++)
                    //            {

                    //                PriceTravelSB.Append("{");

                    //                int childAge = Convert.ToInt16(dtChildPassage.Rows[childNo]["ChildAge"]);
                    //                string childname = Convert.ToString(dtChildPassage.Rows[childNo]["FirstName"]);
                    //                string childsurname = Convert.ToString(dtChildPassage.Rows[childNo]["LastName"]);

                    //                PriceTravelSB.Append("\n\"age\":" + childAge);

                    //                if ((childNo + 1) == drChildPassager.Length)
                    //                {
                    //                    PriceTravelSB.Append("}");

                    //                }
                    //                else if ((childNo + 1) < drChildPassager.Length)
                    //                {
                    //                    PriceTravelSB.Append("},");
                    //                }

                    //            }

                    //        }





                    //        PriceTravelSB.Append("\n]");//paxes

                    //        PriceTravelSB.Append("}");

                    //    }
                    //    PriceTravelSB.Append("\n],");//paxes

                    //}

                    string clientSurnameName = Convert.ToString(dtPassengers.DefaultView[0]["LastName"]).Trim();
                    string clientName = Convert.ToString(dtPassengers.DefaultView[0]["FirstName"]).Trim();


                    //string clientName = "Within Earth";
                    //string clientSurnameName = "WithinEarth";
                    string clientEmail = "barahim@withinearth.com";
                    string CountryCode = "+60";
                    string clientPhoneNumber = "173495341";

                    PriceTravelSB.Append("\n\"client\":{");
                    PriceTravelSB.Append("\n\"name\":\"" + clientName + "\",");
                    PriceTravelSB.Append("\n\"surname\":\"" + clientSurnameName + "\",");
                    PriceTravelSB.Append("\n\"contactData\":[");
                    PriceTravelSB.Append("\n{");
                    PriceTravelSB.Append("\n\"email\":\"" + clientEmail + "\",");

                    PriceTravelSB.Append("\n\"phone\":{");
                    PriceTravelSB.Append("\n\"countryCode\":\"" + CountryCode + "\",");
                    PriceTravelSB.Append("\n\"number\":\"" + clientPhoneNumber + "\"");
                    PriceTravelSB.Append("\n}");

                    PriceTravelSB.Append("\n}");
                    PriceTravelSB.Append("\n]");   //paxes

                    PriceTravelSB.Append("},");



                    //string paymentType = "VC";
                    //string paymentCardType = "Credit Card";
                    //string holderName = "Int API 4 USD";
                    //long CCnumber = 1774230000014550;
                    //int eMonth = 11;
                    //int eYear = 2023;
                    //int secCode = 197;



                    //PriceTravelSB.Append("\n\"payment\":{");

                    //PriceTravelSB.Append("\n\"type\":\"" + paymentType + "\",");

                    //PriceTravelSB.Append("\n\"paymentCard\":{");

                    //PriceTravelSB.Append("\n\"type\":\"" + paymentCardType + "\",");

                    //PriceTravelSB.Append("\n\"holderName\":\"" + holderName + "\",");

                    //PriceTravelSB.Append("\n\"number\":\"" + CCnumber + "\",");
                    //PriceTravelSB.Append("\n\"ExpirationMonth\":" + eMonth + ",");
                    //PriceTravelSB.Append("\n\"ExpirationYear\":" + eYear + ",");
                    //PriceTravelSB.Append("\n\"cvc\":\"" + secCode + "\"");


                    //PriceTravelSB.Append("\n}");


                    //PriceTravelSB.Append("},");

                    string commentsForHotel = "this is amazing hotel";

                    PriceTravelSB.Append("\n\"commentsForHotel\":\"" + commentsForHotel + "\"");

                    PriceTravelSB.Append("}");//end last


                    return PriceTravelSB.ToString();
                    //}

                }

            }
            catch (Exception ex)
            {
                _=_exception.ErrorNew(ex, ErrorLog._LogAPI.Book);
            }

            return "";
        }
    }
}
