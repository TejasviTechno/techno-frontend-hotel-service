using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
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
using XConnect.Supplier.HotelDo;
using XConnect.Supplier.Hoteltrader.Interface;
using static Azure.Core.HttpHeader;
using static XConnect.Supplier.Booking.Com.preebookrs;
using Options = XConnect.Models.Options;

namespace XConnect.Supplier.Hoteltrader
{
    public class HoteltraderRepo : IHoteltrader
    {
        private readonly ICommonMethod _commonMethod;
        private readonly IException _exception;
        private readonly IMarkup _markup;
        private readonly ISupplierRes _supplierRes;
        private readonly KeySettings _keySettings;
        public HoteltraderRepo(ICommonMethod commonMethod, IMarkup markup, ISupplierRes supplierRes, IException exception, IOptions<KeySettings> keySettings)
        {
            _commonMethod = commonMethod;
            _markup = markup;
            _supplierRes = supplierRes;
            _exception = exception;
            _keySettings = keySettings.Value;
        }
        public void GetHotelTraderRequestOnly_Async(DataRow[] commonCode, List<StarRating> listRating, HotelSearchParameter SearchPara, List<SupplierDetails> lstHotelTraderDetails, List<SupplierSearchConfig> lstHotelTraderSearchConfig, DataTable dtPreferredDataHotel, ref ConcurrentBag<HotelX> AllTask, int Hit, DataSet dtXMLCity, TokenData tokenData, DataTable dtROE)
        {
            try
            {
                if (lstHotelTraderDetails.Count > 0)
                {
                    List<StarRating> lstRating = new List<StarRating>();
                    lstRating = _commonMethod.GetStarRatingStringList_xtg(SearchPara);

                    List<HotelCodeByCity> DtHotelDAta = HtmlExtensions.ToList<HotelCodeByCity>(commonCode.CopyToDataTable());
                    int ratingcount = lstRating.Count;
                    int hotelcount = DtHotelDAta.Count;

                    foreach (SupplierDetails HotelTraderDetails in lstHotelTraderDetails)
                    {
                        int SupplierID = HotelTraderDetails.SupplierID;
                        string Supplier = HotelTraderDetails.SupplierName;

                        if (lstHotelTraderSearchConfig.Count > 0)
                        {
                            try
                            {
                                List<SupplierSearchConfig> HotelTraderSearchConfig = lstHotelTraderSearchConfig
                                  .Where(x => x != null && x.SupplierID == SupplierID)
                                  .ToList();


                                if (HotelTraderSearchConfig != null && HotelTraderSearchConfig.Count > 0)
                                {
                                    DataForSearch dataForSearch = _commonMethod.GetXmlDataForSearchTechwise(SearchPara.Nationality, dtXMLCity, Supplier, HotelTraderDetails.TechName);

                                    if (dataForSearch.XmlNationality != "")
                                    {
                                        string Ids = string.Empty;
                                        string xmlbase = "";
                                        _commonMethod.GetPrefferedHotelCodes(ref Ids, ref xmlbase, ref HotelTraderSearchConfig, Supplier, dtPreferredDataHotel, SearchPara.IsSearchFor);

                                        List<HotelCodeByCity> filter = new List<HotelCodeByCity>();
                                        filter = _commonMethod.FilterWithOutCity_Code(ratingcount, hotelcount, DtHotelDAta, lstRating, Supplier);


                                        #region "HotelDo"
                                        if (HotelTraderSearchConfig != null)
                                        {
                                            double CurrencyFactorHotelTrader = 1;
                                            string HotelTraderCurrency = HotelTraderDetails.CurrencyShortName;
                                            if (HotelTraderCurrency.ToLower() != Convert.ToString(tokenData.SiteCurrency).ToLower())
                                            {
                                                CurrencyFactorHotelTrader = _commonMethod.GetROEByCurrency(HotelTraderCurrency, dtROE);
                                            }
                                            for (int i = 0; i < HotelTraderSearchConfig.Count; i++)
                                            {
                                                bool DebugMode = Convert.ToInt32(HotelTraderSearchConfig[i].DebugMode) == 0 ? false : true;


                                                //string WebSiteUrl = Convert.ToString(HotelDoDetails.WebSiteURL);
                                                string UserId = Convert.ToString(HotelTraderDetails.UserID);
                                                string UserName = Convert.ToString(HotelTraderDetails.UserName);
                                                string PassWord = Convert.ToString(HotelTraderDetails.Password);
                                                // List<HotelCodeByCity> DtHotelDAta = FilterFinalList(commonCode, listRating);
                                                if (filter.Count > 0)
                                                {
                                                    if (Convert.ToInt32(HotelTraderSearchConfig[i].CityID) == 0 || Convert.ToInt32(HotelTraderSearchConfig[i].CityID) == SearchPara.CityID)
                                                    {
                                                        int NoOfRecord = Convert.ToInt32(HotelTraderSearchConfig[i].Noofrecords);
                                                        double count = Convert.ToDouble(filter.Count);
                                                        int NoOfDiv = Convert.ToInt32(Math.Ceiling(count / Convert.ToDouble(NoOfRecord)));
                                                        List<List<XMLID_Search>> lstxml = _commonMethod.GetXMLIDString(NoOfDiv, NoOfRecord, count, filter);

                                                        for (int a = 0; a < NoOfDiv; a++)
                                                        {

                                                            //var paramValues1 = string.Join(",", splittedtables[a].Select().Select(x => string.Join(",", Convert.ToString(x.ItemArray[0]))));
                                                            //string IdsForSearch = Convert.ToString(paramValues1);
                                                            string IdsForSearch = Convert.ToString(_commonMethod.GetHotelCodeByCOmma(lstxml[a]));


                                                            HotelX xhotel = new HotelX();
                                                            xhotel.Supplier = Supplier;
                                                            xhotel.SearchTimeOut = Convert.ToInt32(HotelTraderSearchConfig[i].HotelSearchTimeOut);
                                                            //if (Hit > 0)
                                                            //{
                                                            xhotel.ProcessTimeout = Convert.ToInt32(HotelTraderSearchConfig[i].HotelSearchTimeOut2);
                                                            //}
                                                            xhotel.DebugeMode = DebugMode;
                                                            //xhotel.URL = WebSiteUrl;
                                                            if (_keySettings.SupplierProxy == "0")
                                                                xhotel.URL = Convert.ToString(HotelTraderDetails.WebSiteURL);
                                                            else
                                                                xhotel.URL = Convert.ToString(HotelTraderDetails.WebSiteURLforAPI);

                                                            xhotel.UserId = UserId;
                                                            xhotel.UserName = UserName;
                                                            xhotel.PassWord = PassWord;
                                                            xhotel.CurrencyCode = HotelTraderCurrency; //USD
                                                            xhotel.SupplierCurrency = HotelDoStaticInfo.HotelDoCurrencySupplier;//US
                                                            xhotel.CurrencyFactor = CurrencyFactorHotelTrader;
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
                                                                objrq.Request = HotelTraderHotelListRequest(SearchPara, xhotel.URL, xhotel.UserName, xhotel.PassWord, IdsForSearch, dataForSearch.XmlNationality, xhotel.SearchTimeOut);
                                                                objrq.Response = ""; RQRS.Add(objrq);
                                                            }
                                                            xhotel.RQRS = RQRS;
                                                            xhotel.ReturnRooms = Convert.ToInt16(HotelTraderDetails.ReturnRooms);
                                                            xhotel.TechName = Convert.ToString(HotelTraderDetails.TechName);
                                                            #endregion
                                                            AllTask.Add(xhotel);
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                        #endregion
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
            catch (Exception ex)
            {
                _exception.Error(ex);
            }
        }

        public string HotelTraderHotelListRequest(HotelSearchParameter SearchPara, string hotelTrader_Url, string hotelTrader_UserName, string hotelTrader_Password, string idsForSearch, string xmlNationality, int searchTimeOut)
        {
            try
            {
                string CheckInDate = String.Format("{0:yyyy-MM-dd}", DateTime.ParseExact(SearchPara.CheckInDate, "dd/MM/yyyy", null));
                string CheckInOut = String.Format("{0:yyyy-MM-dd}", DateTime.ParseExact(SearchPara.CheckOutDate, "dd/MM/yyyy", null));

                List<string> hotels = new List<string>();

                string[] StrHid = idsForSearch.Split(',');
                for (int i = 0; i < StrHid.Count(); i++)
                {
                    hotels.Add(StrHid[i]);
                }

                int noOfRooms = SearchPara.Rooms.Count;

                List<HotelTraderSearchRequet.Occupancy> occupancylist = new List<HotelTraderSearchRequet.Occupancy>();
                for (int i = 0; i < noOfRooms; i++)
                {
                    HotelTraderSearchRequet.Occupancy occupancies = new HotelTraderSearchRequet.Occupancy();
                    occupancies.checkInDate = CheckInDate;
                    occupancies.checkOutDate = CheckInOut;
                    occupancies.numberOfAdults = SearchPara.Rooms[i].NoofAdults;
                    occupancies.numberOfChildren = SearchPara.Rooms[i].NoofChild;

                    if (SearchPara.Rooms[i].NoofChild == 0)
                    {
                        occupancies.childrenAges = "";
                    }
                    else
                    {
                        if (SearchPara.Rooms[i].NoofChild == 1)
                        {
                            occupancies.childrenAges = Convert.ToString(SearchPara.Rooms[i].Child1Age);
                        }
                        else
                        {
                            occupancies.childrenAges = "" + SearchPara.Rooms[i].Child1Age + "," + SearchPara.Rooms[i].Child2Age + "";
                        }
                    }
                    occupancylist.Add(occupancies);
                }
                HotelTraderSearchRequet.FilterBy FilterBy = new HotelTraderSearchRequet.FilterBy();
                FilterBy.includeNREF = "true";
                FilterBy.rateType = "ALL_RATES_PER_ROOM";

                HotelTraderSearchRequet.SearchCriteriaByIds main = new HotelTraderSearchRequet.SearchCriteriaByIds();

                main.propertyIds = hotels.Select(int.Parse).ToList();
                main.occupancies = occupancylist;
                main.filterBy = FilterBy;

                HotelTraderSearchRequet.Root hotelTraderSearchRequet = new HotelTraderSearchRequet.Root();
                hotelTraderSearchRequet.SearchCriteriaByIds = main;

                string RequestString = Newtonsoft.Json.JsonConvert.SerializeObject(hotelTraderSearchRequet);
                return RequestString;
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
                return "";
            }
        }

        public async Task<List<Options>> HotelTrader_HotelSearchWithRooms(HotelX Item, CommonSearch commonSearch, string currency, string ResponseData1, string CityCode, HotelSearchParameter SearchParam, double CurrencyFactorHotelTader, string Supplier, DataSet dsAgentDetails, Int32 processTimeout, string ResProccessTimerLogActive, string ResProccessTimeValueforSaveLog, string StoreSupplierResErrorLog, DataTable DtROE)
        {
            System.Diagnostics.Stopwatch timerroomx = new System.Diagnostics.Stopwatch();
            timerroomx.Start();
            List<Options> options = new List<Options>();

            DataTable DtHotel = _commonMethod.GenerateDataTableNewV2();
            try
            {
                if (ResponseData1 != string.Empty)
                {




                    DataTable DtAdonis_HotelRoom = _commonMethod.GenerateRoomtypeDataTableNewV2();


                    #region "When not Need Room"  
                    // add hotel code from price only and room code blank here
                    if (SearchParam.Filters.IsShowRooms == 0)
                    {
                        HotelTraderResponse.Root Adonis_Response = Newtonsoft.Json.JsonConvert.DeserializeObject<HotelTraderResponse.Root>(ResponseData1);

                        if (Adonis_Response == null)
                        {
                            Item.ResponseType = "NoCount";
                            return options;
                        }


                        if (Adonis_Response.data.getPropertiesByIds.properties == null)
                        {
                            Item.ResponseType = "NoCount";
                            return options;
                        }
                        if (Adonis_Response.data.getPropertiesByIds.properties.Count < 1)
                        {
                            Item.ResponseType = "NoCount";
                            return options;
                        }


                        for (int i = 0; i < Adonis_Response.data.getPropertiesByIds.properties.Count; i++)
                        {
                            Item.ResponseType = "Success";

                            //double NetPrice1 = GetHotelTraderChepestPrice(ResponseData1, i);
                            //if (NetPrice1 == 0)
                            //{
                            //    Item.ResponseType = "NoCount";
                            //    continue;
                            //}

                            string HotelID = Convert.ToString(Adonis_Response.data.getPropertiesByIds.properties[i].propertyId);

                            if (Adonis_Response.data.getPropertiesByIds.properties[i].rooms.Count > 0)
                            {
                                currency = Adonis_Response.data.getPropertiesByIds.properties[i].rooms[0].rateInfo.currency;
                                if(currency.ToLower() != commonSearch.tokenData.SiteCurrency.ToLower())
                                {
                                    CurrencyFactorHotelTader = _commonMethod.GetROEByCurrency(currency, DtROE);
                                }
                                #region "Hotel Code Block "
                                DataRow drXmlHotel = DtHotel.NewRow();
                                drXmlHotel["XMLHotelCode"] = HotelID;
                                drXmlHotel["HotelName"] = "";
                                drXmlHotel["SupplierName"] = Supplier;
                                drXmlHotel["OfficeID"] = 0;
                                drXmlHotel["ContractID"] = 0;
                                drXmlHotel["Rating"] = 0;
                                drXmlHotel["Reviews"] = 0;
                                drXmlHotel["UniqueNo"] = 0;
                                drXmlHotel["IsShowSupplier"] = false;
                                drXmlHotel["BookingStatus"] = "Available";
                                drXmlHotel["XMLCity"] = "";
                                drXmlHotel["SupplierCurrency"] = currency;

                                double GrossPrice = Convert.ToDouble(Adonis_Response.data.getPropertiesByIds.properties[i].rooms[0].rateInfo.grossPrice) * CurrencyFactorHotelTader;
                                double NetPrice1 = GrossPrice * SearchParam.Rooms.Count();// GetHotelTraderChepestPrice(ResponseData, i);                                

                                drXmlHotel["FinalSellingPrice"] = NetPrice1;
                                drXmlHotel["SellingPrice"] = NetPrice1;
                                drXmlHotel["OfferText"] = "";
                                DtHotel.Rows.Add(drXmlHotel);
                                Double FinalTotal = NetPrice1;
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


                    }
                    #endregion
                    #region "When Need Rooms"
                    //add Hotel code blank and rooms code in room block
                    else
                    {
                        string ResponseData = Newtonsoft.Json.JsonConvert.DeserializeXmlNode(ResponseData1, "root").InnerXml;

                        if (ResponseData == string.Empty)
                        {
                            Item.ResponseType = "NoCount";
                            return options;
                        }

                        DataSet RoomResponse = _commonMethod.GetDataSetFromXMLString(ResponseData);

                        if (RoomResponse == null)
                        {
                            Item.ResponseType = "NoCount";
                            return options;
                        }


                        if (!RoomResponse.Tables.Contains("properties"))
                        {
                            Item.ResponseType = "NoCount";
                            return options;
                        }


                        string data = "0";

                        for (int p = 0; p < RoomResponse.Tables["properties"].Rows.Count; p++)
                        {
                            Item.ResponseType = "Success";

                            string propertiesid = Convert.ToString(RoomResponse.Tables["properties"].Rows[p]["propertyid"]);
                            string properties_id = Convert.ToString(RoomResponse.Tables["properties"].Rows[p]["properties_id"]);

                            double FinalMinPrice = 0;

                            DataRow drXmlHotel = DtHotel.NewRow();
                            drXmlHotel["XMLHotelCode"] = propertiesid;
                            drXmlHotel["HotelName"] = "";
                            drXmlHotel["SupplierName"] = Supplier;
                            drXmlHotel["OfficeID"] = 0;
                            drXmlHotel["ContractID"] = 0;
                            drXmlHotel["Rating"] = 0;
                            drXmlHotel["Reviews"] = 0;
                            drXmlHotel["UniqueNo"] = 0;
                            drXmlHotel["IsShowSupplier"] = false;
                            drXmlHotel["BookingStatus"] = "Available";
                            drXmlHotel["XMLCity"] = "";
                            drXmlHotel["SupplierCurrency"] = currency;
                            drXmlHotel["FinalSellingPrice"] = 1;
                            drXmlHotel["SellingPrice"] = 1;
                            drXmlHotel["OfferText"] = "";
                            DtHotel.Rows.Add(drXmlHotel);



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
                                DataTable DtHotelTraderHotelRoom = _commonMethod.GenerateRoomtypeDataTableNewV2();
                                DataTable dtHotelTraderCancellation = _commonMethod.GenerateCancellationDataTableNewV2();
                                DataTable DtHotelTraderHotelRoomRateBreackUp = _commonMethod.GenerateoomRateBreackUpDataTableNewV2();

                                int CutOffDays = XMLHotelBookingCommon.CutOffDays;
                                CutOffDays = _commonMethod.GetCutOffDays(commonSearch.dtSuplierMarkup, Supplier);

                                #region
                                double AgentPercentage = 0.0;
                                Boolean blnIsShowSupplier = false;
                                double AgentMarkupAmount = 0.0;
                                Boolean isPerce = true;

                                double AgentCompanyPercentage = Convert.ToDouble(SearchParam.AgentCompanyPercentage);
                                bool IsB2C = Convert.ToBoolean(Convert.ToByte(SearchParam.IsB2BOrB2C));

                                //_markup.getAgetMarkupSetting(Supplier, GiataId.ToString(), commonSearch.dtSuplierMarkup, commonSearch.dtMarkupDetails, ref AgentPercentage, ref AgentMarkupAmount, ref blnIsShowSupplier, ref isPerce, commonSearch.AgentId, RatingHotel, SearchParam.CityID, SearchParam.CountryID.ToString(), SearchParam.CheckInDate);

                                _markup.getAgetMarkupSettingNew_new(Supplier, Convert.ToString(RaynaID), commonSearch.dtSuplierMarkup, commonSearch.dsMarkupDetails, ref AgentPercentage, ref AgentMarkupAmount, ref blnIsShowSupplier, ref isPerce, commonSearch.AgentId, RatingHotel, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), SearchParam.CheckInDate);
                                int maxOcupancy = 0;

                                int totalAdult = 0;
                                int totalChild = 0;

                                for (int b = 0; b < SearchParam.Rooms.Count; b++)
                                {
                                    totalAdult = Convert.ToInt32(SearchParam.Rooms[b].NoofAdults) + totalAdult;
                                    totalChild = Convert.ToInt32(SearchParam.Rooms[b].NoofChild) + totalChild;
                                    maxOcupancy = maxOcupancy + SearchParam.Rooms[b].NoofChild + SearchParam.Rooms[b].NoofAdults;
                                }
                                #endregion

                                int NoOfRoom = SearchParam.Rooms.Count();
                                string checkin = (SearchParam.CheckInDate).Substring(6, 4) + "-" + (SearchParam.CheckInDate).Substring(3, 2) + "-" + (SearchParam.CheckInDate).Substring(0, 2);
                                string checkout = (SearchParam.CheckOutDate).Substring(6, 4) + "-" + (SearchParam.CheckOutDate).Substring(3, 2) + "-" + (SearchParam.CheckOutDate).Substring(0, 2);
                                string CurrencyCode = currency;

                                DataRow[] droccupency = RoomResponse.Tables["occupancies"].Select("properties_id=" + properties_id + "");

                                for (int i = 0; i < droccupency.Count(); i++)
                                {
                                    int Adult = Convert.ToInt32(droccupency[i]["numberOfAdults"]);
                                    int Child = Convert.ToInt32(droccupency[i]["numberOfChildren"]);
                                    string Age = Convert.ToString(droccupency[i]["childrenAges"]);

                                    int occupancyRefId = Convert.ToInt32(Convert.ToString(droccupency[i]["occupancyRefid"]));

                                    DataRow[] drroom = RoomResponse.Tables["rooms"].Select("occupancyRefid=" + occupancyRefId + " and  properties_id=" + properties_id + "");


                                    if (drroom.Count() > 0)
                                    {
                                        for (int j = 0; j < drroom.Count(); j++)
                                        {
                                            int rooms_Id = Convert.ToInt32(drroom[j]["rooms_Id"]);
                                            string htidentifier = Convert.ToString(drroom[j]["htIdentifier"]);
                                            string roomname = Convert.ToString(drroom[j]["roomName"]);
                                            string roomtyprname = Convert.ToString(drroom[j]["roomName"]);
                                            bool refundable = (Convert.ToBoolean(drroom[j]["refundable"]));


                                            #region Meals
                                            string MealName = "";
                                            string MealDescription = "";

                                            DataRow[] drmeal = null;
                                            if (RoomResponse.Tables.Contains("mealplanoptions"))
                                            {
                                                drmeal = RoomResponse.Tables["mealplanOptions"].Select("rooms_Id=" + rooms_Id);
                                            }
                                            if (drmeal != null && drmeal.Count() > 0)
                                            {
                                                if (RoomResponse.Tables["mealplanOptions"].Columns.Contains("mealplanDescription") == true && !string.IsNullOrEmpty(Convert.ToString(drmeal[0]["mealplanDescription"])))
                                                {
                                                    MealDescription = "<br> MealDescription:" + Convert.ToString(drmeal[0]["mealplanDescription"]);// added [17-10-2022] #ticket 23492
                                                }

                                                if (!string.IsNullOrEmpty(Convert.ToString(drmeal[0]["breakfastIncluded"])) && Convert.ToBoolean(drmeal[0]["breakfastIncluded"]) == true)
                                                {
                                                    MealName = "BreakFast";//BB//breakfastinclude
                                                }
                                                if (!string.IsNullOrEmpty(Convert.ToString(drmeal[0]["lunchIncluded"])) && Convert.ToBoolean(drmeal[0]["lunchIncluded"]) == true)
                                                {
                                                    MealName = MealName + " + " + "LunchIncluded";//HB///lunchIncluded
                                                }
                                                if (!string.IsNullOrEmpty(Convert.ToString(drmeal[0]["dinnerIncluded"])) && Convert.ToBoolean(drmeal[0]["dinnerIncluded"]) == true)
                                                {
                                                    MealName = MealName + " + " + "DinnerIncluded"; //HB//dinnerIncluded
                                                }
                                                if (!string.IsNullOrEmpty(Convert.ToString(drmeal[0]["allInclusive"])) && Convert.ToBoolean(drmeal[0]["allInclusive"]) == true)
                                                {
                                                    MealName = "AllInclusive";//AI//allInclusive
                                                }
                                                if (Convert.ToBoolean(drmeal[0]["allInclusive"]) == false && Convert.ToBoolean(drmeal[0]["dinnerIncluded"]) == false && Convert.ToBoolean(drmeal[0]["lunchIncluded"]) == false && Convert.ToBoolean(drmeal[0]["breakfastIncluded"]) == false)
                                                {
                                                    MealName = "Room Only";
                                                }
                                            }
                                            else
                                            {
                                                MealName = "Room Only";
                                                MealDescription = drmeal.Count() > 0 ? Convert.ToString(drmeal[0]["mealplanDescription"]) : "";
                                            }

                                            #endregion

                                            #region rateInfo

                                            DataRow[] drrateinfo = RoomResponse.Tables["rateInfo"].Select("rooms_Id=" + rooms_Id);
                                            double grossPrice = 0;
                                            string roomTypeId = "";
                                            string Message = "";

                                            for (int r = 0; r < drrateinfo.Length; r++)
                                            {

                                                double netPrice = Convert.ToDouble(drrateinfo[r]["netPrice"]);
                                                double tax = Convert.ToDouble(drrateinfo[r]["tax"]);
                                                grossPrice = Convert.ToDouble(drrateinfo[r]["grossPrice"]);
                                                // Convert.ToDouble(drrateinfo[r]["payAtProperty"]);
                                                double payAtProperty = 0;
                                                if (RoomResponse.Tables["rateInfo"].Columns.Contains("rateInfo_Id"))
                                                {
                                                    double ratinfo_Id = Convert.ToDouble(drrateinfo[r]["rateInfo_Id"]);
                                                    if (RoomResponse.Tables.Contains("taxinfo"))//Added on 5-sep-2023
                                                    {
                                                        DataRow[] drtaxinfo = RoomResponse.Tables["taxinfo"].Select("rateInfo_Id=" + ratinfo_Id);

                                                        double taxinfo_Id = Convert.ToDouble(drtaxinfo[0]["taxinfo_Id"]);
                                                        DataRow[] drpayAtProperty = null;

                                                        if (!string.IsNullOrEmpty(Convert.ToString(drrateinfo[r]["payAtProperty"])) && Convert.ToDouble(drrateinfo[r]["payAtProperty"]) != 0)
                                                        {
                                                            drpayAtProperty = RoomResponse.Tables["payAtProperty"].Select("taxinfo_Id=" + taxinfo_Id);
                                                            payAtProperty = Convert.ToDouble(drrateinfo[r]["payAtProperty"]);
                                                            Message = "Guest should pay " + " " + payAtProperty + " " + Convert.ToString(drrateinfo[r]["currency"]) + " at the hotel during checkIn." + "<br/>";
                                                            string payAtProperty1 = "";
                                                            for (int k = 0; k < drpayAtProperty.Count(); k++)
                                                            {
                                                                payAtProperty1 = Convert.ToString(Convert.ToDateTime(drpayAtProperty[k]["date"]) + " " + Convert.ToString(drpayAtProperty[k]["name"]) + " " + Convert.ToString(drpayAtProperty[k]["description"]) + " " + Convert.ToDouble(drpayAtProperty[k]["value"]) + " ");
                                                                //Message = " Guest should pay at the hotel during checkIn(not included in grossPrice)" + "   " + Convert.ToString(drpayAtProperty[k]["description"]) + "  " + "<br/>" + Convert.ToString(drpayAtProperty[k]["name"]) + "  " + "<br/>";
                                                            }
                                                        }
                                                    }
                                                }

                                                //drpayAtProperty = RoomResponse.Tables["payAtProperty"].Select("taxinfo_Id=" + taxinfo_Id);

                                                currency = Convert.ToString(drrateinfo[r]["currency"]);
                                                roomTypeId = Convert.ToString(rooms_Id + "$" + htidentifier + "$" + grossPrice + "$" + netPrice + "$" + tax + "$" + payAtProperty + "$" + currency);

                                                CurrencyFactorHotelTader = 1;
                                                if (currency.ToLower() != commonSearch.tokenData.SiteCurrency.ToLower())
                                                {
                                                    //currency = Adonis_Response.data.getPropertiesByIds.properties[i].rooms[0].rateInfo.currency;
                                                    CurrencyFactorHotelTader = _commonMethod.GetROEByCurrency(currency, DtROE);
                                                    //DataTable dtInrCurrency = Common.GetCountryCurrency(currency);
                                                    //CurrencyFactorHotelTader = Convert.ToDouble(dtInrCurrency.Rows[0]["AmtToExch"]);
                                                }
                                                grossPrice = grossPrice * CurrencyFactorHotelTader;
                                            }
                                            double Night_Price = 0.0;
                                            double ratePerNight = 0;
                                            double VatOnPerNight = 0;
                                            #region RateBrekUp
                                            //for (int m = 0; m < SearchParam.NoofNights; m++)
                                            //{
                                            //    Night_Price = grossPrice / SearchParam.NoofNights;
                                            //    string newCheckInDate = Convert.ToString(SearchParam.CheckInDate);
                                            //    string newCheckOutDate = Convert.ToString(SearchParam.CheckOutDate);
                                            //    DateTime dtCheckInDate = DateTime.ParseExact(newCheckInDate, "dd/mm/yyyy", null);
                                            //    DateTime dtCheckOutDate = DateTime.ParseExact(newCheckOutDate, "dd/mm/yyyy", null);
                                            //    TimeSpan t = dtCheckOutDate - dtCheckInDate;

                                            //    #region Vat
                                            //    ratePerNight = _markup.ProcessHotelRateBreakupPrice_WithMarkup(1, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), Night_Price, commonSearch.B2CMarkup, commonSearch.IsParentId, isPerce, AgentPercentage, AgentMarkupAmount, commonSearch.SubAgentMarkup, ref VatOnPerNight);
                                            //    #endregion
                                            //    //VatonfirstnightPrice += VatOnPerNight;
                                            //    DataRow drHotelTraderRoomRateBreackUp = DtHotelTraderHotelRoomRateBreackUp.NewRow();
                                            //    drHotelTraderRoomRateBreackUp["SupplierName"] = Supplier;
                                            //    drHotelTraderRoomRateBreackUp["OwnsystemHotelId"] = OwnsystemHotelId;
                                            //    drHotelTraderRoomRateBreackUp["ContractID"] = 0;
                                            //    drHotelTraderRoomRateBreackUp["RoomNo"] = (i + 1);
                                            //    drHotelTraderRoomRateBreackUp["RoomTypeId"] = roomTypeId;
                                            //    drHotelTraderRoomRateBreackUp["MealId"] = MealName;
                                            //    drHotelTraderRoomRateBreackUp["Meal"] = MealName;
                                            //    string rateWeekColumn = Convert.ToString(dtCheckInDate.Date.AddDays(m).DayOfWeek).Substring(0, 3);
                                            //    drHotelTraderRoomRateBreackUp["DateOfWeek"] = rateWeekColumn;
                                            //    DateTime rateDate = dtCheckInDate.Date.AddDays(m);
                                            //    drHotelTraderRoomRateBreackUp["Date"] = rateDate;
                                            //    drHotelTraderRoomRateBreackUp["Rate"] = ratePerNight;
                                            //    drHotelTraderRoomRateBreackUp["OrderNo"] = 0;
                                            //    drHotelTraderRoomRateBreackUp["RoomTypeName"] = roomtyprname;
                                            //    drHotelTraderRoomRateBreackUp["DateOfWeek"] = rateWeekColumn;
                                            //    drHotelTraderRoomRateBreackUp["BookingStatus"] = "Available";
                                            //    drHotelTraderRoomRateBreackUp["BoardTypeID"] = MealName;
                                            //    drHotelTraderRoomRateBreackUp["VatdiffAmount"] = VatOnPerNight;
                                            //    drHotelTraderRoomRateBreackUp["RateType"] = roomtyprname;
                                            //    drHotelTraderRoomRateBreackUp["SupplierCurrency"] = currency;
                                            //    drHotelTraderRoomRateBreackUp["BuyingPrice"] = ratePerNight / CurrencyFactorHotelTader;
                                            //    DtHotelTraderHotelRoomRateBreackUp.Rows.Add(drHotelTraderRoomRateBreackUp);
                                            //}
                                            #endregion

                                            #endregion

                                            #region Policy
                                            DataRow[] drcancellation = null;

                                            if (RoomResponse.Tables.Contains("cancellationPolicies"))
                                            {
                                                if (RoomResponse.Tables["cancellationPolicies"].Rows.Count > 0)
                                                {
                                                    if (RoomResponse.Tables["cancellationPolicies"].Columns.Contains("startWindowTime"))
                                                    {
                                                        drcancellation = RoomResponse.Tables["cancellationPolicies"].Select("rooms_Id=" + rooms_Id);

                                                    }
                                                }
                                            }
                                            if (drcancellation != null && (!string.IsNullOrEmpty(Convert.ToString(drcancellation[0]["cancellationCharge"])) && drcancellation.Count() > 0))
                                            {
                                                bool check = false;
                                                double AppliedAgentCharges = grossPrice;// * CurrencyFactorHotelTader;//Convert.ToDouble(drcancellation[0]["cancellationCharge"]) * CurrencyFactorHotelTader;

                                                #region Caculation
                                                double VatOnPolicy = 0;
                                                double VatonfirstnightPrice = 0;
                                                double MainAgentCharge = AppliedAgentCharges;
                                                double CompanyChargeAmount = AppliedAgentCharges;
                                                AppliedAgentCharges = _markup.ProcessHotelCancelationPrice_WithMarkup(ref MainAgentCharge, SearchParam.NoofNights, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), AppliedAgentCharges, isPerce, commonSearch.B2CMarkup, AgentPercentage, AgentMarkupAmount, commonSearch.IsParentId, commonSearch.SubAgentMarkup, ref VatOnPolicy);
                                                #endregion

                                                //DateTime ToDate = Convert.ToDateTime(drcancellation[0]["startWindowTime"]).Date;
                                                DateTime ToDate = DateTime.Now.Date;
                                                for (int v = 0; v < drcancellation.Length; v++) //Added on 25-Aug-2025
                                                {
                                                    DateTime ToDate1 = Convert.ToDateTime(drcancellation[v]["startWindowTime"]).Date;
                                                    if (v == 0)
                                                    {
                                                        ToDate = ToDate1;
                                                    }
                                                    else
                                                    {
                                                        if (ToDate > ToDate1)
                                                        {
                                                            ToDate = ToDate1;
                                                        }
                                                    }
                                                }
                                                DateTime FromDate = DateTime.Now.Date;

                                                FromDate = DateTime.Now.Date;
                                                if (ToDate.AddDays(CutOffDays).Date.AddDays(-1).Date > DateTime.Now.Date)
                                                {
                                                    ToDate = ToDate.AddDays(CutOffDays).Date;
                                                }
                                                else
                                                {
                                                    ToDate = DateTime.Now.Date;
                                                }
                                                if (ToDate > FromDate && check == false)
                                                {
                                                    double zero = 0;
                                                    dtHotelTraderCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref zero, dtHotelTraderCancellation.NewRow(), CurrencyFactorHotelTader, currency, Supplier, Convert.ToString(OwnsystemHotelId), (i + 1).ToString(), roomTypeId, roomtyprname, Convert.ToString(MealName), DateTime.Now.Date, ToDate.AddDays(-1).AddHours(23).AddMinutes(59).AddSeconds(59), 0, 0, "", 0, "", "", "", ""));

                                                    dtHotelTraderCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref CompanyChargeAmount, dtHotelTraderCancellation.NewRow(), CurrencyFactorHotelTader, currency, Supplier, Convert.ToString(OwnsystemHotelId), (i + 1).ToString(), roomTypeId,
                                                    roomtyprname, Convert.ToString(MealName), ToDate, DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null), MainAgentCharge, AppliedAgentCharges, "", VatonfirstnightPrice, "", "", "", "", ""));
                                                    check = true;
                                                }
                                                else //(ToDate < FromDate)
                                                {
                                                    ToDate = DateTime.Now.Date;
                                                    dtHotelTraderCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref CompanyChargeAmount, dtHotelTraderCancellation.NewRow(), CurrencyFactorHotelTader, currency, Supplier, Convert.ToString(OwnsystemHotelId), (i + 1).ToString(), roomTypeId,
                                                    roomtyprname, Convert.ToString(MealName), ToDate, DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null), MainAgentCharge, AppliedAgentCharges, "", VatonfirstnightPrice, "", "", "", "", ""));
                                                }
                                            }
                                            else
                                            {
                                                double AppliedAgentCharges = grossPrice;// * CurrencyFactorHotelTader;

                                                #region Caculation
                                                double VatOnPolicy = 0;
                                                double VatonfirstnightPrice = 0;
                                                double MainAgentCharge = AppliedAgentCharges;
                                                double CompanyChargeAmount = AppliedAgentCharges;
                                                AppliedAgentCharges = _markup.ProcessHotelCancelationPrice_WithMarkup(ref MainAgentCharge, SearchParam.NoofNights, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), AppliedAgentCharges, isPerce, commonSearch.B2CMarkup, AgentPercentage, AgentMarkupAmount, commonSearch.IsParentId, commonSearch.SubAgentMarkup, ref VatOnPolicy);
                                                #endregion

                                                //DateTime ToDate = DateTime.ParseExact(objSearPera.CheckInDate, "dd/MM/yyyy", null);
                                                DateTime FromDate = DateTime.Now.Date;
                                                dtHotelTraderCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref CompanyChargeAmount, dtHotelTraderCancellation.NewRow(), CurrencyFactorHotelTader, currency, Supplier, Convert.ToString(OwnsystemHotelId), (i + 1).ToString(), roomTypeId,
                                                    roomtyprname, Convert.ToString(MealName), FromDate, DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null), MainAgentCharge, AppliedAgentCharges, "", VatonfirstnightPrice, "", "", "", "", ""));
                                            }
                                            #endregion

                                            #region Room

                                            DataRow drHotelTraderRoom = DtHotelTraderHotelRoom.NewRow();
                                            drHotelTraderRoom["OwnsystemHotelId"] = OwnsystemHotelId;
                                            drHotelTraderRoom["RoomTypeCode"] = rooms_Id;
                                            drHotelTraderRoom["BoardType"] = "";

                                            #region Vat
                                            double mainbuy = grossPrice;
                                            double Factor = CurrencyFactorHotelTader;// change  here.

                                            _markup.ProcessHotelRoom_WithMarkup(Factor, currency, true, false, SearchParam.NoofNights, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), SearchParam.CheckInDate, SearchParam.CheckOutDate, ref grossPrice, ref drHotelTraderRoom, isPerce, AgentPercentage, AgentMarkupAmount, commonSearch.SubAgentMarkup, commonSearch.B2CMarkup, commonSearch.IsParentId);
                                            #endregion

                                            drHotelTraderRoom["MealName"] = MealName;
                                            drHotelTraderRoom["MealId"] = MealName;
                                            drHotelTraderRoom["Type"] = "";
                                            drHotelTraderRoom["Board"] = MealName;
                                            drHotelTraderRoom["BookingStatus"] = "Available";
                                            drHotelTraderRoom["UniqueNo"] = Guid.NewGuid().ToString();
                                            drHotelTraderRoom["ContractID"] = 0;

                                            drHotelTraderRoom["RoomNo"] = (i + 1);
                                            drHotelTraderRoom["BoardTypeID"] = MealName;
                                            drHotelTraderRoom["AdultsOccupancy"] = Adult;
                                            drHotelTraderRoom["ChildOccupancy"] = Child;
                                            drHotelTraderRoom["RoomSequenceNo"] = "";
                                            drHotelTraderRoom["EssentialInformation"] = Message + MealDescription;
                                            drHotelTraderRoom["Characteristic"] = "";
                                            drHotelTraderRoom["SupplierName"] = Supplier;

                                            drHotelTraderRoom["OfferCode"] = "";
                                            drHotelTraderRoom["OfferDescription"] = "";
                                            drHotelTraderRoom["RoomTypeName"] = roomtyprname;
                                            drHotelTraderRoom["RoomTypeId"] = roomTypeId;

                                            DtHotelTraderHotelRoom.Rows.Add(drHotelTraderRoom);
                                            #endregion



                                        }
                                    }
                                }

                                Options ops = (await _supplierRes.GetCommonRoomForSuppliers(GiataId, commonSearch.tokenData, commonSearch.advancedOptions, FinalMinPrice, drXmlHotel, DtHotelTraderHotelRoom, dtHotelTraderCancellation, DtHotelTraderHotelRoomRateBreackUp, SearchParam));
                                if (ops != null)
                                    options.Add(ops);

                            }



                            #endregion
                        }





                    }
                    #endregion





                }
                return options;
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
                    if (SearchParam != null && ResProccessTimerLogActive == "1" && timerroomx.ElapsedMilliseconds > Convert.ToInt32(ResProccessTimeValueforSaveLog))
                    {
                        timerroomx.Stop();
                        SearchParam.SupplierTime += ",|Finish:HotelTrader Process Response  " + "|Time :" + timerroomx.Elapsed.ToString();
                        _ = Task.Run(() => _commonMethod.FireAway_SQL_ResPorcessTimeLog("", timerroomx.Elapsed.ToString(), "HotelTrader", SearchParam.SearchKey, 1, SearchParam.AgentID));
                    }
                }
                catch (Exception x)
                {
                    await _exception.Error(x);
                }
                #endregion

                if (Item.ResponseType == "NoCount" && StoreSupplierResErrorLog == "1")
                    _ = Task.Run(() =>  _commonMethod.XMLRequestResponseTrack_Error(Item.RQRS[0].Request, Item.RQRS[0].Response, Item.Supplier, timerroomx.Elapsed.ToString(), "", SearchParam.SearchKey));
            }

            return options;
        }

        //Prebook 22-07-25
        public async Task<PriceCheckDto> HotelTraderPriceCheck(DataSet dsCartDetails, DataRow[] HotelTraderDetails, int j, double CompanyBuyingPrice,double FinalPrice, double CurrencyFactorHotelTrader, string UniqueNo, string AgentUniqueNo, double ResPrice, string SupplierName, int AgentId, TokenData tokenData)
        {
            PriceCheckDto priceCheckDto = new PriceCheckDto();
            string CancellationPolicyRQ = "";
            string responsetype = "Success";
            string ResponseData = "-";
            string time = "";
            string Error = "", request = "-";
            try
            {

                string username = Convert.ToString(HotelTraderDetails[0]["UserName"]);
                string password = Convert.ToString(HotelTraderDetails[0]["Password"]);
                string Url = "https://reservation.hoteltrader.com/graphql";
                string roomtypeid = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomTypeId"]);
                request = HotelTraderPriceCheckRequest(roomtypeid, dsCartDetails, j);

                var fulQuery = new HotelTraderGraphqlClient.GraphQLQuery()
                {
                    query = HotelTraderGraphqlClient.queryQuote,
                    variables = Newtonsoft.Json.Linq.JObject.Parse(request),
                };
                request = Newtonsoft.Json.JsonConvert.SerializeObject(fulQuery);

                _commonMethod.ThirdPartyBookingHistory(Convert.ToString(UniqueNo), Convert.ToString(AgentUniqueNo), request, "-", "Before Block Request hit Price check", Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["SupplierName"]), "Request Confirm j=" + j + "", "-", "WithinEarth");

                System.Diagnostics.Stopwatch timerroomprocess = new System.Diagnostics.Stopwatch();
                timerroomprocess.Start();

                ResponseDto responseDto = await GetHotelTraderResponse(request, Url, password);
                ResponseData = responseDto.Response;
                Error = responseDto.Error;
                _commonMethod.ThirdPartyBookingHistory(Convert.ToString(UniqueNo), Convert.ToString(AgentUniqueNo), ResponseData + Error, "-", "After Block Response hit Price check", Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["SupplierName"]), "Request Confirm j=" + j + "", "-", "WithinEarth");

                timerroomprocess.Stop();
                time = timerroomprocess.Elapsed.ToString();

                if (ResponseData == "" || ResponseData == null)
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
                HotelTraderQuoteResponse.Root ds = null;
                try
                {
                    ds = Newtonsoft.Json.JsonConvert.DeserializeObject<HotelTraderQuoteResponse.Root>(ResponseData);
                }
                catch (Exception ex)
                {
                    _ = _exception.Error(ex);
                }

                string[] spitid1 = roomtypeid.Split(new String[] { "$" }, StringSplitOptions.None);
                string currency = spitid1[6];

                double CurrencyFactor = 1;
                if (currency.ToLower() != tokenData.SiteCurrency.ToLower())
                {
                    CurrencyFactor = _commonMethod.GetROEByCurrency(currency, tokenData.dtROE);
                }

                string Message = ds.data.quote.rooms[0].message; //need to add here case for message
                FinalPrice = ds.data.quote.rooms[0].rates.grossPrice * CurrencyFactor;
                priceCheckDto.FinalPrice = FinalPrice;

                if (ds.data.quote.rooms[0].message.ToLower() != "confirm price success. please proceed to book stage.")
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
                if (Math.Round(FinalPrice, 2) > Math.Round(CompanyBuyingPrice, 2))
                {
                    try
                    {
                        ResponseData = "#Price Changed " + "#Main Price = " + CompanyBuyingPrice + " #New Price = " + FinalPrice + " #Response= " + ResponseData;
                        responsetype = "GeneralError";
                        string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, CancellationPolicyRQ, ResponseData, Error);
                    }
                    catch (Exception e)
                    {
                        _ = _exception.Error(e);
                    }
                    priceCheckDto.Flag = 1;
                    return priceCheckDto;
                }
            }
            catch (Exception ex)
            {
                try
                {
                    Error = ex.Message + ex.StackTrace.ToString();
                    responsetype = "GeneralError";
                    string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, request, ResponseData, Error);
                }
                catch (Exception e)
                {
                    _ = _exception.Error(e);
                }
                _ = _exception.Error(ex);
                priceCheckDto.Flag = 2;
                return priceCheckDto;
            }
            try
            {
                responsetype = "Success";
                string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, request, ResponseData, Error);
            }
            catch (Exception ex)
            {
                _ = _exception.Error(ex);
            }
            priceCheckDto.Flag = 0;
            return priceCheckDto;
        }

        public string HotelTraderPriceCheckRequest(string roomtypeid, DataSet dsCartDetails, int j = 0)
        {
            string RequestString = "";
            try
            {
                //string roomTypeId = Convert.ToString(rooms_Id + "$" + htidentifier + "$" + grossPrice + "$" + netPrice + "$" + tax + "$" + payAtProperty);

                int night = Convert.ToInt32(dsCartDetails.Tables[0].Rows[0]["NoOfNight"]);
                //string roomtypeid = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomTypeId"]);
                int NoOfRoom = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView.Count);

                string[] spitid1 = null;
                double dailyprice = 0; List<double> dailyt = new List<double>(); List<double> taxdaily = new List<double>(); double dailytax = 0;
                //int night = objSearch.NoofNights;
                if (roomtypeid.Contains("$"))
                {
                    spitid1 = roomtypeid.Split(new String[] { "$" }, StringSplitOptions.None);
                    if (spitid1.Length >= 3)
                        dailyprice = Convert.ToDouble(spitid1[3]);
                    if (spitid1.Length >= 4)
                        dailytax = Convert.ToDouble(spitid1[4]);
                    for (int d = 0; d < night; d++)
                    {
                        dailyt.Add(Convert.ToDouble(dailyprice / night));
                    }
                    for (int d = 0; d < night; d++)
                    {
                        taxdaily.Add(Convert.ToDouble(dailytax / night));
                    }
                }

                List<HotelTraderRequest_Quote.Quote> QuoteList = new List<HotelTraderRequest_Quote.Quote>();

                HotelTraderRequest_Quote.Occupancy occupancies = new HotelTraderRequest_Quote.Occupancy();

                occupancies.numberOfAdults = Convert.ToInt32(dsCartDetails.Tables[1].Rows[j]["NoOfAdults"]);
                occupancies.numberOfChildren = Convert.ToInt32(dsCartDetails.Tables[1].Rows[j]["NoOfChild"]);
                occupancies.childrenAges = "";

                if (occupancies.numberOfChildren == 0)
                {
                    occupancies.childrenAges = "";
                }
                else
                {
                    if (occupancies.numberOfChildren == 1)
                    {
                        occupancies.childrenAges = Convert.ToString(dsCartDetails.Tables[1].Rows[j]["ChildAge1"]);
                    }
                    else
                    {
                        occupancies.childrenAges = "" + Convert.ToString(dsCartDetails.Tables[1].Rows[j]["ChildAge1"]) + "," + Convert.ToString(dsCartDetails.Tables[1].Rows[j]["ChildAge2"]) + "";
                    }
                }

                HotelTraderRequest_Quote.Rates rates = new HotelTraderRequest_Quote.Rates();
                rates.dailyPrice = dailyt;
                rates.dailyTax = taxdaily;
                rates.grossPrice = Convert.ToDouble(spitid1[2]);
                rates.netPrice = dailyprice;
                rates.tax = dailytax;
                rates.payAtProperty = Convert.ToDouble(spitid1[5]);

                HotelTraderRequest_Quote.Quote quote = new HotelTraderRequest_Quote.Quote();
                quote.htIdentifier = spitid1[1].ToString();
                quote.rates = rates;
                quote.occupancy = occupancies; //occupancylist;
                QuoteList.Add(quote);

                HotelTraderRequest_Quote.Root main = new HotelTraderRequest_Quote.Root();
                main.Quote = QuoteList;// quote;

                RequestString = Newtonsoft.Json.JsonConvert.SerializeObject(main);
                return RequestString;
            }
            catch (Exception ex)
            {
                _ = _exception.Error(ex);

            }
            return RequestString;
        }

        public async Task<ResponseDto> GetHotelTraderResponse(string Request, string url, string Password, Int32 TimeOut = 120000)
        {
            ResponseDto responseDto = new ResponseDto();
            string Error = "";
            string strOutput = "";
            System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
            timer.Start();
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "POST";
                request.Headers.Add("Authorization", "Basic " + Password + "");
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                request.ContentType = "application/json";
                request.Accept = "application/json";
                request.KeepAlive = true;

                ServicePointManager.DefaultConnectionLimit = 2000;

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
                    if (ex.Response != null)
                    {
                        WebResponse errorResponse = ex.Response;
                        using (Stream responseStream = errorResponse.GetResponseStream())
                        {
                            StreamReader reader = new StreamReader(responseStream, Encoding.GetEncoding("utf-8"));
                            String errorText = reader.ReadToEnd();
                        }
                        Error = ex.Message;
                    }
                }
            }
            catch (Exception ex)
            {
                _ = _exception.Error(ex);
                Error = ex.Message.ToString();

                responseDto.Error = Error;
                responseDto.Response = "";
                return responseDto;
            }

            responseDto.Error = Error;
            responseDto.Response = strOutput;
            return responseDto;
        }

        public string HotelTraderBookingRequest(DataSet dsCartDetails, DataRow[] hotelTraderDetails, int i, int j, double resPrice, DataTable dtPassengers, string AgentCode, ref string AgtRef)
        {
            string RequestString = "";
            try
            {
                string[] spitid1 = null;
                double dailyprice = 0;
                List<double> dailyt = new List<double>();
                List<double> taxdaily = new List<double>();
                double dailytax = 0;
                int night = Convert.ToInt32(dsCartDetails.Tables[0].Rows[i]["NoOfNight"]);
                string roomtypeid = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomTypeId"]);
                int NoOfRoom = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView.Count);

                if (roomtypeid.Contains("$"))
                {
                    spitid1 = roomtypeid.Split(new String[] { "$" }, StringSplitOptions.None);
                    if (spitid1.Length >= 3)
                        dailyprice = Convert.ToDouble(spitid1[3]);
                    if (spitid1.Length >= 4)
                        dailytax = Convert.ToDouble(spitid1[4]);
                    for (int d = 0; d < night; d++)
                    {
                        dailyt.Add(Convert.ToDouble(dailyprice / night));
                    }
                    for (int d = 0; d < night; d++)
                    {
                        taxdaily.Add(Convert.ToDouble(dailytax / night));
                    }
                }

                #region paymentinfo
                //HotelTrader.HotelTraderHTBookingReq.PaymentInformation paymentinfo = new HotelTrader.HotelTraderHTBookingReq.PaymentInformation();
                //paymentinfo.address = "";
                //paymentinfo.address2 = "";
                //paymentinfo.cardNumber = "";
                //paymentinfo.cardType = "";
                //paymentinfo.city = "";
                //paymentinfo.countryCode = "";
                //paymentinfo.email = "";
                //paymentinfo.expiryMonth = "";
                //paymentinfo.expiryYear = "";
                //paymentinfo.firstName = "";
                //paymentinfo.lastName = "";
                //paymentinfo.currencyCode = "";
                //paymentinfo.paymentAmount = 0;
                //paymentinfo.securityCode = 0;
                //paymentinfo.state = "";
                //paymentinfo.zip = 0;
                #endregion

                string refernceNo = "Book_" + Convert.ToString(AgentCode) + Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["UserCartDetailId"]);//USerCartId to UserCartDetailI


                List<HotelTraderBookingRequest.HotelTraderHTBookingReq.Room> roomlist = new List<HotelTraderBookingRequest.HotelTraderHTBookingReq.Room>();

                HotelTraderBookingRequest.HotelTraderHTBookingReq.Rates rates = new HotelTraderBookingRequest.HotelTraderHTBookingReq.Rates();
                rates.dailyPrice = dailyt;
                rates.dailyTax = taxdaily;
                rates.netPrice = dailyprice;
                rates.tax = dailytax;
                rates.payAtProperty = Convert.ToDouble(spitid1[5]);


                #region  IsLead Filter Logic
                string isLead = string.Empty;
                dtPassengers.DefaultView.RowFilter = "PickUp='" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomTypeID"]) + "' and RoomNo='" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomNumber"]) + "' and IsLead=1";
                if (dtPassengers.DefaultView.Count == 0)
                    isLead = "true";
                dtPassengers.DefaultView.RowFilter = "";
                dtPassengers.DefaultView.RowFilter = "PickUp='" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomTypeID"]) + "' and RoomNo='" + (dsCartDetails.Tables[1].DefaultView[j]["RoomNumber"]) + "' and UserCartId='" + dsCartDetails.Tables[0].Rows[i]["UserCartId"] + "'";
                dtPassengers.DefaultView.Sort = "IsLead desc";
                #endregion


                List<HotelTraderBookingRequest.HotelTraderHTBookingReq.Guest> guestlist = new List<HotelTraderBookingRequest.HotelTraderHTBookingReq.Guest>();
                for (i = 0; i < dtPassengers.DefaultView.Count; i++)
                {
                    HotelTraderBookingRequest.HotelTraderHTBookingReq.Guest guest = new HotelTraderBookingRequest.HotelTraderHTBookingReq.Guest();
                    guest.firstName = Convert.ToString(dtPassengers.DefaultView[i]["FirstName"]);
                    guest.lastName = Convert.ToString(dtPassengers.DefaultView[i]["LastName"]);
                    guest.email = Convert.ToString(dtPassengers.DefaultView[i]["Email"]);

                    guest.adult = true;// Convert.ToBoolean(dtPassengers.Rows[i]["Adult"]);// need to check
                    if (Convert.ToString(dtPassengers.DefaultView[i]["PaxType"]).ToLower() == "adult")
                    {
                        guest.adult = true;
                    }
                    else
                    {
                        guest.adult = false;
                    }
                    guest.age = Convert.ToInt32(dtPassengers.DefaultView[i]["ChildAge"]);
                    guest.phone = Convert.ToString(dtPassengers.DefaultView[i]["Mobile"]);
                    guest.primary = true;//Convert.ToBoolean(dtPassengers.DefaultView[i]["primary"]);
                    if (Convert.ToString(dtPassengers.DefaultView[i]["IsLead"]) == "1")
                    {
                        guest.adult = true;
                    }
                    else
                    {
                        guest.adult = false;
                    }
                    guestlist.Add(guest);
                }

                HotelTraderBookingRequest.HotelTraderHTBookingReq.Occupancy occupancies = new HotelTraderBookingRequest.HotelTraderHTBookingReq.Occupancy();
                occupancies.numberOfAdults = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[j]["NoOfAdults"]);
                occupancies.numberOfChildren = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[j]["NoOfChild"]);
                occupancies.childrenAges = "";

                if (Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[j]["NoOfChild"]) == 0)
                {
                    occupancies.childrenAges = "";
                }
                else
                {
                    if (Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[j]["NoOfChild"]) == 1)
                    {
                        occupancies.childrenAges = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["ChildAge1"]);
                    }
                    else
                    {
                        occupancies.childrenAges = "" + Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[j]["ChildAge1"]) + "," + Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[j]["ChildAge2"]) + "";
                    }

                }
                //string roomTypeId = Convert.ToString(rooms_Id + "$" + htidentifier + "$" + grossPrice + "$" + netPrice + "$" + tax + "$" + payAtProperty);

                HotelTraderBookingRequest.HotelTraderHTBookingReq.Room rooms = new HotelTraderBookingRequest.HotelTraderHTBookingReq.Room();
                rooms.htIdentifier = Convert.ToString(spitid1[1]); ;
                rooms.clientRoomConfirmationCode = refernceNo + "-" + 1;
                rooms.roomSpecialRequests = null;
                rooms.rates = rates;
                rooms.occupancy = occupancies;
                rooms.guests = guestlist;
                roomlist.Add(rooms);

                HotelTraderBookingRequest.HotelTraderHTBookingReq.Book Book = new HotelTraderBookingRequest.HotelTraderHTBookingReq.Book();
                Book.clientConfirmationCode = refernceNo;
                Book.otaConfirmationCode = "test-123";
                Book.otaClientName = "WithinEarth";
                Book.membershipId = null;
                Book.specialRequests = null;
                Book.paymentInformation = null;
                Book.rooms = roomlist;

                HotelTraderBookingRequest.HotelTraderHTBookingReq.Root main = new HotelTraderBookingRequest.HotelTraderHTBookingReq.Root();
                main.Book = Book;
                RequestString = Newtonsoft.Json.JsonConvert.SerializeObject(main);
                return RequestString;
            }
            catch (Exception ex)
            {
                _=_exception.Error(ex);
            }
            return RequestString;
        }

        public string GetHotelTrader_BookingCancelRQ(string bookingid)
        {
            string RequestString = "";
            try
            {
                HotelTraderCancelRequest.BookingCancellation_Request.Cancel bookingcancel = new HotelTraderCancelRequest.BookingCancellation_Request.Cancel();
                bookingcancel.htConfirmationCode = bookingid;

                HotelTraderCancelRequest.BookingCancellation_Request.Root main = new HotelTraderCancelRequest.BookingCancellation_Request.Root();
                main.Cancel = bookingcancel;

                RequestString = Newtonsoft.Json.JsonConvert.SerializeObject(main);
                return RequestString;
            }
            catch (Exception ex)
            {
                _ =_exception.ErrorNew(ex, ErrorLog._LogAPI.CancelBooking);
            }
            return RequestString;
        }
    }
}
