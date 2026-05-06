using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using XConnect.CommonUtilities.Interface;
using XConnect.CommonUtilities;
using XConnect.ErrorLogHandler.Interface;
using XConnect.Markup.Interface;
using XConnect.Models.Hotel;
using XConnect.Models;
using XConnect.Supplier.HotelsPro.Interface;
using Options = XConnect.Models.Options;
using Azure.Core;
using System.Net.Http.Headers;
using System.IO.Compression;
using Microsoft.Extensions.Logging;
using static Azure.Core.HttpHeader;
using System.Net;
using static XConnect.Models.ErrorLog;

namespace XConnect.Supplier.HotelsPro
{
    public class HotelsProRepo : IHotelsPro
    {
        private readonly ICommonMethod _commonMethod;
        private readonly IException _exception;
        private readonly IMarkup _markup;
        private readonly ISupplierRes _supplierRes;
        private readonly KeySettings _keySettings;

        public HotelsProRepo(ICommonMethod commonMethod, IException exception, IMarkup markup, ISupplierRes supplierRes, IOptions<KeySettings> keySettings)
        {
            _commonMethod = commonMethod;
            _exception = exception;
            _markup = markup;
            _supplierRes = supplierRes;
            _keySettings = keySettings.Value;
        }

        public void GetHotesProRequestOnly_async(DataRow[] commonCode, List<StarRating> listRating, DataTable dtPreferredDataHotel, HotelSearchParameter SearchPara, List<SupplierSearchConfig> lstHPSearchConfig, List<SupplierDetails> lstHPDetails, bool IsHotelWiseSearch, ref ConcurrentBag<HotelX> AllTask, string AgentCode, int Hit, TokenData tokenData, DataSet dtXMLCity, DataTable dtROE)
        {
            try
            {
                if (lstHPDetails != null && lstHPDetails.Count > 0)
                {
                    Boolean isTrip = true;
                    for (int n = 0; n < SearchPara.Rooms.Count; n++)
                    {
                        if (SearchPara.Rooms[n].NoofChild <= 4 || SearchPara.Rooms[n].Child1Age <= 17 || SearchPara.Rooms[n].Child2Age <= 17 || SearchPara.Rooms[n].NoofAdults <= 6)
                            isTrip = true;
                        else
                            isTrip = false;
                    }

                    if (isTrip)
                    {
                        List<StarRating> lstRating = new List<StarRating>();
                        lstRating = _commonMethod.GetStarRatingStringList_xtg(SearchPara);

                        List<HotelCodeByCity> DtHotelDAta = HtmlExtensions.ToList<HotelCodeByCity>(commonCode.CopyToDataTable());
                        int ratingcount = lstRating.Count;
                        int hotelcount = DtHotelDAta.Count;

                        foreach (SupplierDetails HPDetails in lstHPDetails)
                        {
                            try
                            {
                                int SupplierID = HPDetails.SupplierID;
                                string Supplier = HPDetails.SupplierName;

                                if (lstHPSearchConfig.Count > 0)
                                {
                                    List<SupplierSearchConfig> HPSearchConfig = lstHPSearchConfig
                                       .Where(x => x != null && x.SupplierID == SupplierID)
                                       .ToList();


                                    DataRow[] drdotw = dtPreferredDataHotel.Select("ServiceName='" + Supplier + "'");
                                    string Ids = string.Empty;
                                    if (drdotw.Count() > 0)
                                    {
                                        var paramValues1 = String.Join(",", drdotw.Select(x => String.Join(",", Convert.ToString(x.ItemArray[0]))));
                                        Ids = Convert.ToString(paramValues1);
                                    }

                                    if (SearchPara.IsSearchFor.ToLower().IndexOf("preferred") >= 0 && drdotw.Count() == 0)
                                        HPSearchConfig = null;

                                    if (HPSearchConfig != null && HPSearchConfig.Count > 0)
                                    {
                                        DataForSearch dataForSearch = _commonMethod.GetXmlDataForSearchTechwise(SearchPara.Nationality, dtXMLCity, HPDetails.TechName, HPDetails.TechName);

                                        if (dataForSearch.XmlNationality != "")
                                        {
                                            List<HotelCodeByCity> filter = new List<HotelCodeByCity>();
                                            filter = _commonMethod.FilterWithOutCity_Code(ratingcount, hotelcount, DtHotelDAta, lstRating, HPDetails.TechName);

                                            if (filter.Count > 0)
                                            {

                                                string Currency = HPDetails.CurrencyShortName;
                                                double CurrencyFactorHP = 1;
                                                if (Currency.ToLower() != tokenData.SiteCurrency.ToLower())
                                                {
                                                    CurrencyFactorHP = _commonMethod.GetROEByCurrency(Currency, dtROE);
                                                }

                                                for (int k = 0; k < HPSearchConfig.Count; k++)
                                                {
                                                    bool DebugMode = (Convert.ToInt32(HPSearchConfig[k].DebugMode) == 0 ? false : true);
                                                    if (Convert.ToInt32(HPSearchConfig[k].CityID) == 0 || Convert.ToInt32(HPSearchConfig[k].CityID) == SearchPara.CityID)//All City means 0
                                                    {
                                                        int NoOfRecord = Convert.ToInt32(HPSearchConfig[k].Noofrecords);
                                                        double count = Convert.ToDouble(filter.Count);
                                                        int NoOfDiv = Convert.ToInt32(Math.Ceiling(count / Convert.ToDouble(NoOfRecord)));
                                                        List<List<XMLID_Search>> lstxml = _commonMethod.GetXMLIDString(NoOfDiv, NoOfRecord, count, filter);
                                                        for (int a = 0; a < NoOfDiv; a++)
                                                        {
                                                            string IdsForSearch = Convert.ToString(_commonMethod.GetHotelCodeByCOmma(lstxml[a]));

                                                            HotelX xhotel = new HotelX();
                                                            xhotel.Supplier = Supplier;
                                                            xhotel.DebugeMode = DebugMode;
                                                            xhotel.SearchTimeOut = Convert.ToInt32(HPSearchConfig[k].HotelSearchTimeOut);
                                                            //if (Hit > 0)
                                                            xhotel.ProcessTimeout = Convert.ToInt32(HPSearchConfig[k].HotelSearchTimeOut2);
                                                            if (_keySettings.SupplierProxy == "0")
                                                                xhotel.URL = Convert.ToString(HPDetails.WebSiteURL);
                                                            else
                                                                xhotel.URL = Convert.ToString(HPDetails.WebSiteURLforAPI);

                                                            xhotel.UserId = Convert.ToString(HPDetails.UserID);
                                                            xhotel.UserName = Convert.ToString(HPDetails.UserName);
                                                            xhotel.PassWord = Convert.ToString(HPDetails.Password);
                                                            xhotel.CurrencyCode = Currency;
                                                            xhotel.CurrencyFactor = CurrencyFactorHP;
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
                                                                objrq.Request = (HotelsProRequestOnly_async(xhotel.URL, xhotel.UserName, xhotel.PassWord, xhotel.SearchTimeOut, SearchPara, IdsForSearch, DebugMode, dataForSearch.xmlCity, dataForSearch.XmlNationality));
                                                                objrq.Response = ""; RQRS.Add(objrq);
                                                            }
                                                            xhotel.RQRS = RQRS;
                                                            xhotel.TechName = HPDetails.TechName;
                                                            xhotel.ReturnRooms = HPDetails.ReturnRooms;
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

        public string HotelsProRequestOnly_async(string WSurl, string Username, string Password, Int32 SearchTimeOut, HotelSearchParameter SearchParam, string HotelIds, bool DebugMode, string XMLCITY, string HPNationality)
        {
            try
            {

                string checkin = Convert.ToString(SearchParam.CheckInDate);
                string checkout = Convert.ToString(SearchParam.CheckOutDate);
                int NoOfNight = Convert.ToInt32(SearchParam.NoofNights);
                checkin = checkin.Substring(6, 4) + "-" + checkin.Substring(3, 2) + "-" + checkin.Substring(0, 2);
                checkout = checkout.Substring(6, 4) + "-" + checkout.Substring(3, 2) + "-" + checkout.Substring(0, 2);
                string HotelList = HotelsProSearchRequest(SearchParam, checkin, checkout, XMLCITY, HPNationality, HotelIds);
                return HotelList;

            }
            catch (IOException e)
            {
                _exception.Error(e);
            }
            return "";
        }



        public string HotelsProSearchRequest(HotelSearchParameter SearchParam, string checkin, string checkout, string XMLCity, string Nationality, string HotelIds)
        {
            try
            {
                StringBuilder HotelsPro = new StringBuilder();
                HotelsPro.Append("search/");
                for (int i = 0; i < SearchParam.Rooms.Count; i++)
                {
                    int Adult = SearchParam.Rooms[i].NoofAdults;
                    int Child = SearchParam.Rooms[i].NoofChild;
                    if (i >= 1)
                        HotelsPro.Append("&pax=" + SearchParam.Rooms[i].NoofAdults);
                    else
                        HotelsPro.Append("?pax=" + SearchParam.Rooms[i].NoofAdults);

                    if (Child >= 1)
                    {
                        switch (Child)
                        {
                            case 1:
                                HotelsPro.Append("," + Convert.ToString(SearchParam.Rooms[i].Child1Age) + "");
                                break;
                            case 2:
                                HotelsPro.Append("," + Convert.ToString(SearchParam.Rooms[i].Child1Age) + "," + Convert.ToString(SearchParam.Rooms[i].Child2Age) + "");
                                break;
                        }
                    }
                }
                HotelsPro.Append("&checkin=" + checkin + "");
                HotelsPro.Append("&checkout=" + checkout + "");
                if (HotelIds == "")
                    HotelsPro.Append("&destination_code=" + XMLCity + "");
                else
                    HotelsPro.Append("&hotel_code=" + HotelIds + "");
                HotelsPro.Append("&client_nationality=" + Nationality + "");
                HotelsPro.Append("&currency=USD");
                string HotelsProREQ = HotelsPro.ToString();
                return HotelsProREQ;
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
                return "";
            }
        }

        public async Task<List<Options>> HotelsPro_HotelSearchWithRooms(HotelX Item, CommonSearch commonSearch, string WebSiteURL, int SearchTimeOut, string Supplier, bool DebugeMode, string UserName, string PassWord, string MethoodType, string ResponseData, HotelSearchParameter SearchParam, string xmlcity, string xmlcountry, string xmlnationality, string currency, double currencyfactor, Int32 processTimeout, string ResProccessTimerLogActive, string ResProccessTimeValueforSaveLog)
        {
            System.Diagnostics.Stopwatch timerroomx = new System.Diagnostics.Stopwatch();
            timerroomx.Start();
            List<Options> options = new List<Options>();

            DataTable DtHotel = _commonMethod.GenerateDataTableNewV2();
            try
            {
                if (ResponseData.ToLower().Contains("eanwserror") || ResponseData.Trim() == "")
                {
                    Item.SupplierResponseError = ResponseData;
                    Item.ResponseStatus = 404;
                    Item.ResponseType = "NoCount";
                    return options;
                }
                dynamic data = JObject.Parse(ResponseData);
                ValueType count = (data.count).Value;
                for (int hh = 0; hh < Convert.ToInt32(count); hh++)
                {
                    Item.ResponseType = "Success";
                    double FinalMinPrice = 0;
                    #region "When not Need Room"  
                    // add hotel code from price only and room code blank here
                    if (SearchParam.Filters.IsShowRooms == 0)
                    {
                        #region "Hotel Code Block "
                        if (commonSearch.tokenData.AgentId != 1899)
                        {
                            string price = (data.results[hh].products[0].price).Value;
                            FinalMinPrice = Convert.ToDouble(price);

                        }
                        else
                        {
                            FinalMinPrice = 1;
                        }
                        DataRow drXmlHotel = DtHotel.NewRow();
                        drXmlHotel["IsRecomondedHotel"] = false;
                        drXmlHotel["XMLHotelCode"] = (data.results[hh].hotel_code).Value;
                        drXmlHotel["HotelFrontImage"] = "";
                        drXmlHotel["HotelName"] = "";
                        drXmlHotel["Description"] = "";
                        drXmlHotel["EssentialInformation"] = "";
                        drXmlHotel["OfficeID"] = 0;
                        drXmlHotel["ContractID"] = 0;
                        drXmlHotel["IsShowSupplier"] = false;
                        drXmlHotel["OwnSystemHotelId"] = 0;
                        drXmlHotel["Address"] = "";
                        drXmlHotel["Favorite"] = 0;
                        drXmlHotel["XmlHotelCity"] = xmlcity;
                        drXmlHotel["XMLCountry"] = xmlcountry;
                        drXmlHotel["XMLCity"] = xmlcity;
                        drXmlHotel["SupplierName"] = Item.Supplier;
                        drXmlHotel["BookingStatus"] = "Available";
                        drXmlHotel["SupplierCurrency"] = currency;

                        //drHotel["IsOffer"] = offer;
                        //if (offer.ToString() == "true")
                        //    drHotel["OfferText"] = "";
                        drXmlHotel["FinalSellingPrice"] = FinalMinPrice;
                        drXmlHotel["SellingPrice"] = FinalMinPrice;
                        drXmlHotel["UniqueNo"] = (data.results[hh].hotel_code).Value;
                        drXmlHotel["AvailToken"] = (data.code).Value;
                        // if (finalPrice > 0)
                        DtHotel.Rows.Add(drXmlHotel);

                        #endregion


                        if (FinalMinPrice > 0)
                        {
                            #region "Room code Block"
                            bool willAddOrNot = true;
                            Int64 RaynaID = 0;
                            Int64 GiataId = 0;
                            int Rating = 0;
                            Int64 OwnsystemHotelId = 0;
                            double MinPrice = FinalMinPrice;
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

                        #region "Hotel Block here"
                        // string price = (data.results[i].products[0].price).Value;
                        //FinalMinPrice = Convert.ToDouble(price);
                        DataRow drXmlHotel = DtHotel.NewRow();
                        drXmlHotel["IsRecomondedHotel"] = false;
                        drXmlHotel["XMLHotelCode"] = (data.results[hh].hotel_code).Value;
                        drXmlHotel["HotelFrontImage"] = "";
                        drXmlHotel["HotelName"] = "";
                        drXmlHotel["Description"] = "";
                        drXmlHotel["EssentialInformation"] = "";
                        drXmlHotel["OfficeID"] = 0;
                        drXmlHotel["ContractID"] = 0;
                        drXmlHotel["IsShowSupplier"] = false;
                        drXmlHotel["OwnSystemHotelId"] = 0;
                        drXmlHotel["Address"] = "";
                        drXmlHotel["Favorite"] = 0;
                        drXmlHotel["XmlHotelCity"] = xmlcity;
                        drXmlHotel["XMLCountry"] = xmlcountry;
                        drXmlHotel["XMLCity"] = xmlcity;
                        drXmlHotel["SupplierName"] = Item.Supplier;
                        drXmlHotel["BookingStatus"] = "Available";
                        drXmlHotel["SupplierCurrency"] = currency;


                        //drHotel["IsOffer"] = offer;
                        //if (offer.ToString() == "true")
                        //    drHotel["OfferText"] = "";
                        drXmlHotel["FinalSellingPrice"] = 1;
                        drXmlHotel["SellingPrice"] = 1;
                        drXmlHotel["UniqueNo"] = (data.results[hh].hotel_code).Value;
                        drXmlHotel["AvailToken"] = (data.code).Value;
                        // if (finalPrice > 0)
                        DtHotel.Rows.Add(drXmlHotel);
                        #endregion
                        #region "GetRooms for this hotel"
                        bool willAddOrNot = true;
                        Int64 RaynaID = 0;
                        Int64 GiataId = 0;
                        int RatingHotel = 0;
                        Int64 OwnsystemHotelId = 0;
                        string Provider = Item.Supplier;
                        double MinPrice = 1;
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
                                // Boolean blnIsShowSupplier = false;
                                double AgentMarkupAmount = 0.0;
                                Boolean isPerce = true;

                                double AgentCompanyPercentage = Convert.ToDouble(SearchParam.AgentCompanyPercentage);
                                bool IsB2C = Convert.ToBoolean(Convert.ToByte(SearchParam.IsB2BOrB2C));
                                bool blnIsShowSupplier = false;
                                //_markup.getAgetMarkupSetting(Provider, GiataId.ToString(), commonSearch.dtSuplierMarkup, commonSearch.dtMarkupDetails, ref AgentPercentage, ref AgentMarkupAmount, ref blnIsShowSupplier, ref isPerce, commonSearch.AgentId, RatingHotel, SearchParam.CityID, SearchParam.CountryID.ToString(), SearchParam.CheckInDate);

                                _markup.getAgetMarkupSettingNew_new(Provider, Convert.ToString(RaynaID), commonSearch.dtSuplierMarkup, commonSearch.dsMarkupDetails, ref AgentPercentage, ref AgentMarkupAmount, ref blnIsShowSupplier, ref isPerce, commonSearch.AgentId, RatingHotel, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), SearchParam.CheckInDate);
                                int maxOcupancy = 0;
                                for (int i = 0; i < SearchParam.Rooms.Count; i++)
                                    maxOcupancy = maxOcupancy + SearchParam.Rooms[i].NoofChild + SearchParam.Rooms[i].NoofAdults;
                                #region Supplier Room Code block
                                string tokenHP = (data.code).Value;
                                var ctsNew = new CancellationTokenSource();
                                var tokenNew = ctsNew.Token;
                                ctsNew.CancelAfter(3000);
                                string Request = HotelsProRoomDetailsRequest((data.results[hh].hotel_code).Value, tokenHP);
                                //Task<string> response = HotelRepository.GetSupplierResponseFinal(Item, "", "", "", ctsNew, "", Request, WebSiteURL, SearchTimeOut, Supplier, SearchParam, DebugeMode, UserName, PassWord, "Post", "", (Supplier.ToLower() == Item.Supplier.ToLower()));
                                ResponseDto responseDto = await ProResponse(Request, WebSiteURL, UserName, PassWord, SearchTimeOut, "",true);
                                string response = responseDto.Response;
                                // if (response.Wait(TimeSpan.FromSeconds(2)))
                                {
                                    string Response = (response != null ? response : "");
                                    if (Response == "")
                                    {
                                        Item.SupplierResponseError = ResponseData;
                                        Item.ResponseStatus = 404;
                                        return options;
                                    }
                                        
                                    int totalAdult = 0;
                                    int totalChild = 0;
                                    for (int b = 0; b < SearchParam.Rooms.Count; b++)
                                    {
                                        totalAdult = Convert.ToInt32(SearchParam.Rooms[b].NoofAdults) + totalAdult;
                                        totalChild = Convert.ToInt32(SearchParam.Rooms[b].NoofChild) + totalChild;
                                    }
                                    data = JObject.Parse(Response);
                                    ValueType categoryCount = (data.count).Value;
                                    string hotelCode = "", destinationCode = "", price = "", Mealcode = "", roomCode = "", roomCategory = "", RUMcod = "", MealName = "";
                                    double itemprice = 0, Night_Price = 0, VatonfirstnightPrice = 0;
                                    double CompanyBuyingPrice = 0;//080525
                                    int roomCount = (data.results.Count);
                                    for (int a = 0; a < roomCount; a++)
                                    {
                                        hotelCode = (data.results[a].hotel_code).Value;
                                        roomCode = (data.results[a].code).Value;
                                        destinationCode = (data.results[a].destination_code).Value;
                                        bool offer = (data.results[a].offer).Value;
                                        price = (data.results[a].price).Value;
                                        itemprice = Convert.ToDouble(price);
                                        CompanyBuyingPrice = itemprice; //080525

                                        if (FinalMinPrice < itemprice)
                                        {
                                            FinalMinPrice = Convert.ToDouble(price);
                                        }
                                        Mealcode = (data.results[a].meal_type).Value;
                                        MealName = HotelsProMeal(Mealcode);
                                        Night_Price = itemprice / SearchParam.NoofNights;
                                        string uniqno = Guid.NewGuid().ToString().Substring(0, 4);
                                        DataRow drHotelsProRoom = dtHotelRoom.NewRow();
                                        drHotelsProRoom["OwnsystemHotelId"] = OwnsystemHotelId;
                                        drHotelsProRoom["RoomTypeCode"] = Mealcode;
                                        drHotelsProRoom["BoardType"] = "";
                                        drHotelsProRoom["MealName"] = MealName;
                                        drHotelsProRoom["MealId"] = Mealcode;
                                        drHotelsProRoom["Type"] = "";
                                        drHotelsProRoom["Board"] = "";
                                        drHotelsProRoom["BookingStatus"] = "Available";
                                        drHotelsProRoom["UniqueNo"] = OwnsystemHotelId;
                                        drHotelsProRoom["ContractID"] = 0;
                                        drHotelsProRoom["RoomNo"] = 1;
                                        drHotelsProRoom["BoardTypeID"] = "";
                                        drHotelsProRoom["AdultsOccupancy"] = totalAdult;
                                        drHotelsProRoom["ChildOccupancy"] = totalChild;
                                        drHotelsProRoom["OfferCode"] = "";
                                        drHotelsProRoom["OfferDescription"] = "";
                                        drHotelsProRoom["RoomSequenceNo"] = "";
                                        drHotelsProRoom["EssentialInformation"] = "N/A";
                                        drHotelsProRoom["Characteristic"] = "";
                                        #region Vat
                                        _markup.ProcessHotelRoom_WithMarkup(currencyfactor, currency, true, true, SearchParam.NoofNights * SearchParam.Rooms.Count, SearchParam.CityID, SearchParam.CountryID.ToString(), SearchParam.CheckInDate, SearchParam.CheckOutDate, ref itemprice, ref drHotelsProRoom, isPerce, AgentPercentage, AgentMarkupAmount, commonSearch.SubAgentMarkup, commonSearch.B2CMarkup, commonSearch.IsParentId);
                                        #endregion
                                        drHotelsProRoom["SupplierName"] = Item.Supplier;
                                        List<Room_HotelsPro> roomlist = new List<Room_HotelsPro>();
                                        int roomS = (data.results[a].rooms.Count);
                                        string RoomType_Text = "";
                                        for (int b = 0; b < roomS; b++)
                                        {
                                            Room_HotelsPro obj = new Room_HotelsPro();
                                            obj.RoomCount = b + 1;
                                            obj.RoomName = (data.results[a].rooms[b].room_description).Value;
                                            obj.status = true;
                                            roomlist.Add(obj);
                                            if (b == 0)
                                                RUMcod = (data.results[a].rooms[b].room_type).Value;
                                            else
                                                RUMcod = RUMcod + " " + (data.results[a].rooms[b].room_type).Value;
                                        }
                                        for (int i = 0; i < roomlist.Count; i++)
                                        {
                                            for (int j = (i + 1); j < roomlist.Count; j++)
                                            {
                                                if (roomlist[i].RoomName == roomlist[j].RoomName && roomlist[j].status)
                                                {
                                                    roomlist[j].RoomCount = roomlist[i].RoomCount;
                                                    roomlist[j].status = false;
                                                }
                                            }
                                            if (RoomType_Text.IndexOf(roomlist[i].RoomName) < 0 || RoomType_Text == "")
                                                RoomType_Text = RoomType_Text + roomlist[i].RoomCount + " X " + roomlist[i].RoomName + " ";
                                        }
                                        drHotelsProRoom["RoomTypeName"] = RoomType_Text + "/" + RUMcod;
                                        string roomTypeId = (10 + a) + "$$$" + RUMcod + "$$$" + tokenHP + "$$$" + roomCode;
                                        drHotelsProRoom["RoomTypeId"] = roomTypeId;
                                        dtHotelRoom.Rows.Add(drHotelsProRoom);



                                        #region Policy - Added on 080525
                                        bool chechk = false;
                                        int Poliocy = (data.results[a].policies.Count);
                                        string Checking = (data.results[a].checkin).Value;

                                        bool nonRefPolicy = true;
                                        if (Poliocy == 1)
                                            nonRefPolicy = false;

                                        if (nonRefPolicy)
                                        {
                                            double cancellationChargeAmount = CompanyBuyingPrice;
                                            #region Vat
                                           
                                        
                                            double VatOnPolicy = 0;
                                            double MainAgentCharge = cancellationChargeAmount;
                                            double CompanyChargeAmount = cancellationChargeAmount;
                                            cancellationChargeAmount = _markup.ProcessHotelCancelationPrice_WithMarkup(ref MainAgentCharge, SearchParam.Rooms.Count * SearchParam.NoofNights, SearchParam.CityID, SearchParam.CountryID.ToString(), cancellationChargeAmount, isPerce, commonSearch.B2CMarkup, AgentPercentage, AgentMarkupAmount, commonSearch.IsParentId, commonSearch.SubAgentMarkup, ref VatOnPolicy);
                                            #endregion
                                            dtCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref CompanyChargeAmount, dtCancellation.NewRow(), 1, Item.CurrencyCode, Provider, Convert.ToString(OwnsystemHotelId), (1).ToString(), roomTypeId, "", Convert.ToString(Mealcode), DateTime.Now.Date, Convert.ToDateTime(Checking), MainAgentCharge, cancellationChargeAmount, "", 0, "", "", "", "", "", ""));
                                            dtCancellation.AcceptChanges();
                                        }
                                        else
                                        {
                                            for (int i = Convert.ToInt32(Poliocy) - 1; i >= 0; i--)
                                            {
                                                string Ratio = (data.results[a].policies[i].ratio).Value;
                                                double cancellationChargeAmount = CompanyBuyingPrice * Convert.ToDouble(Ratio);
                                                #region Vat
                                               
                                               
                                                double VatOnPolicy = 0;
                                                double MainAgentCharge = cancellationChargeAmount;
                                                double CompanyChargeAmount = cancellationChargeAmount;

                                                cancellationChargeAmount = _markup.ProcessHotelCancelationPrice_WithMarkup(ref MainAgentCharge, SearchParam.Rooms.Count * SearchParam.NoofNights, SearchParam.CityID, SearchParam.CountryID.ToString(), cancellationChargeAmount, isPerce, commonSearch.B2CMarkup, AgentPercentage, AgentMarkupAmount, commonSearch.IsParentId, commonSearch.SubAgentMarkup, ref VatOnPolicy);
                                                #endregion

                                                DateTime FromDate = DateTime.Now.Date;
                                                ValueType RemainingDays = (data.results[a].policies[i].days_remaining).Value;
                                                DateTime checkIn = Convert.ToDateTime(Checking);
                                                DateTime FinalDate = checkIn.AddDays(-Convert.ToInt32(RemainingDays)).Date;
                                                DateTime ToDate = FinalDate.AddDays(CutOffDays);
                                                //if (ownsystemid == "355963" || ownsystemid == "344643")
                                                // ToDate = DateTime.Now.Date;
                                                if (FromDate < ToDate && chechk == false)
                                                {
                                                    double zero = 0;

                                                    dtCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref zero, dtCancellation.NewRow(), Item.CurrencyFactor, Item.CurrencyCode, Provider, Convert.ToString(OwnsystemHotelId), (1).ToString(), roomTypeId, "", Convert.ToString(Mealcode), DateTime.Now.Date, ToDate.AddDays(-1).AddHours(23).AddMinutes(59).AddSeconds(59), 0, 0, "", 0, "", "", "", "", "", ""));
                                                    dtCancellation.AcceptChanges();
                                                    chechk = true;
                                                }
                                                if (FromDate > ToDate)
                                                    ToDate = DateTime.Now;
                                                dtCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref CompanyChargeAmount, dtCancellation.NewRow(), Item.CurrencyFactor, Item.CurrencyCode, Provider, Convert.ToString(OwnsystemHotelId), (1).ToString(), roomTypeId, "", Convert.ToString(Mealcode), ToDate, checkIn, MainAgentCharge, cancellationChargeAmount, "", 0, "", "", "", "", "", ""));
                                                dtCancellation.AcceptChanges();
                                            }
                                        }
                                        #endregion
                                    }

                                }



                                #endregion

                            }
                            #endregion
                            Options ops = (await _supplierRes.GetCommonRoomForSuppliers(GiataId, commonSearch.tokenData, commonSearch.advancedOptions, FinalMinPrice, drXmlHotel, dtHotelRoom, dtCancellation, DtRoomRateBreackUp, SearchParam));
                            if (ops != null)
                                options.Add(ops);
                        }
                        #endregion
                        #endregion
                    }

                    #endregion

                }
            }
            catch (Exception e)
            {
                Item.ResponseStatus = 404;
                await _exception.Error(e);

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
                        
                        SearchParam.SupplierTime += ",|Finish:" + Supplier + " Process Response  " + "|Time :" + timerroomx.Elapsed.ToString();
                        _= Task.Run(() => _commonMethod.FireAway_SQL_ResPorcessTimeLog("", timerroomx.Elapsed.ToString(), Supplier, SearchParam.SearchKey, 1, SearchParam.AgentID));
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

        public class Room_HotelsPro
        {
            public int RoomCount { get; set; }
            public string RUMcod { get; set; }
            public string RoomName { get; set; }
            public bool status { get; set; }
        }

        public string HotelsProRoomDetailsRequest(string hotelCode, string Code)
        {
            try
            {
                StringBuilder HotelsPro = new StringBuilder();
                HotelsPro.Append("hotel-availability/");
                HotelsPro.Append("?hotel_code=" + hotelCode + "");
                HotelsPro.Append("&search_code=" + Code + "");
                HotelsPro.Append("&currency=USD");
                string HotelsProREQ = HotelsPro.ToString();
                return HotelsProREQ;
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
                return "";
            }
        }

        public async Task<string> ProResponseHttp(string Xmlrequest, string Url, string username, string password, int Timeout, string Searchkey = "")
        {
            string responseString = "";
            try
            {
                using (var httpClient = new HttpClient())
                {
                    // Set timeout
                    HttpRequestMessage Request = new HttpRequestMessage();


                    string Key = Convert.ToBase64String(Encoding.ASCII.GetBytes(username + ":" + password));
                    string requestUri = Url + Xmlrequest;

                    Request = new HttpRequestMessage(System.Net.Http.HttpMethod.Get, requestUri);
                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    httpClient.DefaultRequestHeaders.Add("Authorization", "Basic " + Key); // Replace with your actual Key
                    httpClient.DefaultRequestHeaders.Add("Accept-Encoding", "gzip");
                    httpClient.DefaultRequestHeaders.Add("ContentType", "application/x-www-form-urlencoded");


                    HttpResponseMessage responseMessage = await httpClient.SendAsync(Request);
                    Stream streamResponse = await responseMessage.Content.ReadAsStreamAsync();
                    if (responseMessage.Content.Headers.ContentEncoding.Contains("gzip"))
                    {
                        streamResponse = new GZipStream(streamResponse, CompressionMode.Decompress);

                    }
                    else if (responseMessage.Content.Headers.ContentEncoding.Contains("deflate"))
                    {
                        streamResponse = new DeflateStream(streamResponse, CompressionMode.Decompress);
                    }
                    else
                    {
                        responseString = await responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);
                    }

                    StreamReader streamReader = new StreamReader(streamResponse);
                    responseString = await streamReader.ReadToEndAsync();
                }
            }
            catch (Exception ex)
            {
                await _exception.Error(ex, Searchkey);
                //ErrorResponse = ex.Message;
            }

            return responseString;
        }

        public string HotelsProMeal(string mealCode)
        {
            string mealName = "";
            if (mealCode == "RO")
                mealName = "Room Only";
            if (mealCode == "BC")
                mealName = "Breakfast Continental";
            if (mealCode == "BE")
                mealName = "Breakfast English";
            if (mealCode == "HB")
                mealName = "Half Board";
            if (mealCode == "FB")
                mealName = "Full Board";
            if (mealCode == "AI")
                mealName = "All Inclusive";
            if (mealCode == "UL")
                mealName = "Ultra";
            return mealName;
        }

        //Recheck
        public async Task<CancellationDto> HotelsPro_cancellation(string roomtypecode, DataSet dsHotel, string hotelcode, string ownsystemid, string roomtypeid, DataTable dtAgentDetail, DataTable dtSuplierMarkup, DataTable dtSearchConfig, HotelSearchParameter SearchPara, string roomno, string provider, int AgentId, DataTable dtMarkupDetails, string GiataId = "0", int hotelRating = 0, string SearchKey = "")
        {
            CancellationDto cancellationDto = new CancellationDto();
            try
            {
                DataTable dtHotelsProRoomCacelationPolicy = _commonMethod.GenerateCancellationDataTableNewV2();
                int CutOffDays = XMLHotelBookingCommon.CutOffDays;
                CutOffDays = _commonMethod.GetCutOffDays(dtSuplierMarkup, provider);

                DataRow[] drSuplierMarkup = (dtSuplierMarkup.Select("SupplierName='" + provider + "'"));
                int SupplierId = Convert.ToInt32(drSuplierMarkup[0]["SupplierID"]);
                DataRow[] drSearchConfig = (dtSearchConfig.Select("SupplierID='" + SupplierId + "'"));
                int SearchTimeOut = Convert.ToInt32(drSearchConfig[0]["BookingConfirmationTimeout"]);
                string[] Rcode = roomtypeid.Split(new String[] { "$$$" }, StringSplitOptions.None);
                string code = "provision/" + Rcode[3];
                string ErrorResponse = "";
                //string HotelsProResponse = ProResponse(code, Convert.ToString(drSuplierMarkup[0]["WebSiteURL"]), Convert.ToString(drSuplierMarkup[0]["UserName"]), Convert.ToString(drSuplierMarkup[0]["Password"]), Convert.ToInt32(drSearchConfig[0]["HotelSearchTimeOut"]), ref ErrorResponse);
                ResponseDto responseDto = await ProResponse(code, Convert.ToString(drSuplierMarkup[0]["WebSiteURL"]), Convert.ToString(drSuplierMarkup[0]["UserName"]), Convert.ToString(drSuplierMarkup[0]["Password"]), 6000);//
                string HotelsProResponse = responseDto.Response;
                ErrorResponse = responseDto.Error;


                _commonMethod.ThirdPartyGetPolicyHistory(Convert.ToString(roomtypeid), Convert.ToString(ownsystemid), code, Convert.ToString(HotelsProResponse), "After Get Policy", provider, "Response", "-", "");
                if (HotelsProResponse.ToLower().Contains("eanwserror") || HotelsProResponse.Trim() == "")
                {
                    _commonMethod.ThirdPartyGetPolicyHistory(Convert.ToString(roomtypeid), Convert.ToString(ownsystemid), code, Convert.ToString(ErrorResponse), "After Get Policy", provider, "Response", "-", "");

                    cancellationDto.dsHotelData = dsHotel;
                    return cancellationDto;
                }
                else
                {
                    _commonMethod.ThirdPartyGetPolicyHistory(Convert.ToString(roomtypeid), Convert.ToString(ownsystemid), code, Convert.ToString(HotelsProResponse), "After Get Policy", provider, "Response", "-", "");
                }

                dynamic data = JObject.Parse(HotelsProResponse);
                int Poliocy = (data.policies.Count);
                string Checking = (data.checkin).Value;
                string price = (data.price).Value;
                double roomprice = Convert.ToDouble(price);
                string additionalInfo = "";
                if (HotelsProResponse.Contains("additional_info") == true)
                    additionalInfo = (data.additional_info).Value;
                else
                    additionalInfo = "N/A";
                bool chechk = false;
                bool nonRefPolicy = true;
                if (Poliocy == 1)
                    nonRefPolicy = false;
                if (nonRefPolicy)
                {
                    double cancellationChargeAmount = roomprice;
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
                    double VatOnPolicy = 0;
                    double MainAgentCharge = cancellationChargeAmount;
                    double CompanyChargeAmount = cancellationChargeAmount;
                    cancellationChargeAmount = _markup.ProcessHotelCancelationPrice_WithMarkup(ref MainAgentCharge, SearchPara.Rooms.Count * SearchPara.NoofNights, SearchPara.CityID, SearchPara.CountryID.ToString(), cancellationChargeAmount, isPerce, B2CMarkup, AgentPercentage, AgentMarkupAmount, IsParentId, SubAgentMarkup, ref VatOnPolicy);
                    #endregion
                    dtHotelsProRoomCacelationPolicy.Rows.Add(_commonMethod.setCancellationPolicyRow(ref CompanyChargeAmount, dtHotelsProRoomCacelationPolicy.NewRow(), 1, "USD", provider, Convert.ToString(ownsystemid), (1).ToString(), roomtypeid, "", Convert.ToString(roomtypecode), DateTime.Now.Date, Convert.ToDateTime(Checking), MainAgentCharge, cancellationChargeAmount, "", 0, "", "", "", additionalInfo, "", ""));
                    dtHotelsProRoomCacelationPolicy.AcceptChanges();
                }
                else
                {
                    for (int i = Convert.ToInt32(Poliocy) - 1; i >= 0; i--)
                    {
                        string Ratio = (data.policies[i].ratio).Value;
                        double cancellationChargeAmount = roomprice * Convert.ToDouble(Ratio);
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
                        double VatOnPolicy = 0;
                        double MainAgentCharge = cancellationChargeAmount;
                        double CompanyChargeAmount = cancellationChargeAmount;

                        cancellationChargeAmount = _markup.ProcessHotelCancelationPrice_WithMarkup(ref MainAgentCharge, SearchPara.Rooms.Count * SearchPara.NoofNights, SearchPara.CityID, SearchPara.CountryID.ToString(), cancellationChargeAmount, isPerce, B2CMarkup, AgentPercentage, AgentMarkupAmount, IsParentId, SubAgentMarkup, ref VatOnPolicy);
                        #endregion

                        DateTime FromDate = DateTime.Now.Date;
                        ValueType RemainingDays = (data.policies[i].days_remaining).Value;
                        DateTime checkIn = Convert.ToDateTime(Checking);
                        DateTime FinalDate = checkIn.AddDays(-Convert.ToInt32(RemainingDays)).Date;
                        DateTime ToDate = FinalDate.AddDays(CutOffDays);
                        //if (ownsystemid == "355963" || ownsystemid == "344643")
                        // ToDate = DateTime.Now.Date;
                        if (FromDate < ToDate && chechk == false)
                        {
                            double zero = 0;

                            dtHotelsProRoomCacelationPolicy.Rows.Add(_commonMethod.setCancellationPolicyRow(ref zero, dtHotelsProRoomCacelationPolicy.NewRow(), 1, "USD", provider, Convert.ToString(ownsystemid), (1).ToString(), roomtypeid, "", Convert.ToString(roomtypecode), DateTime.Now.Date, ToDate.AddDays(-1).AddHours(23).AddMinutes(59).AddSeconds(59), 0, 0, "", 0, "", "", "", additionalInfo, "", ""));
                            dtHotelsProRoomCacelationPolicy.AcceptChanges();
                            chechk = true;
                        }
                        if (FromDate > ToDate)
                            ToDate = DateTime.Now;
                        dtHotelsProRoomCacelationPolicy.Rows.Add(_commonMethod.setCancellationPolicyRow(ref CompanyChargeAmount, dtHotelsProRoomCacelationPolicy.NewRow(), 1, "USD", provider, Convert.ToString(ownsystemid), (1).ToString(), roomtypeid, "", Convert.ToString(roomtypecode), ToDate, checkIn, MainAgentCharge, cancellationChargeAmount, "", 0, "", "", "", additionalInfo, "", ""));
                        dtHotelsProRoomCacelationPolicy.AcceptChanges();
                    }
                }
                //for (int i = Convert.ToInt32(Poliocy) - 1; i >= 0; i--)
                //{
                //    string Ratio = (data.policies[i].ratio).Value;
                //    double cancellationChargeAmount = roomprice * Convert.ToDouble(Ratio);
                //    #region Vat
                //    Boolean isPerce = true;
                //    double AgentCompanyPercentage = Convert.ToDouble(SearchPara.AgentCompanyPercentage);
                //    bool IsB2C = Convert.ToBoolean(Convert.ToByte(SearchPara.IsB2BOrB2C));
                //    double B2CMarkup = (IsB2C == false ? AgentCompanyPercentage : XMLHotelBookingCommon.B2CMarkup);
                //    double AgentPercentage = 0.0;
                //    double AgentMarkupAmount = 0.0;
                //    double SubAgentMarkup = 0;
                //    int IsParentId = 1;
                //    Boolean blnIsShowSupplier = false;
                //    //Int32 hotelRating = 0;
                //    //hotelRating = Convert.ToInt32(dsHotel.Tables[0].Rows[0]["Rating"]);
                //    Common.getAgetMarkupSetting("hotelspro", GiataId, dtSuplierMarkup, dtMarkupDetails, ref AgentPercentage, ref AgentMarkupAmount, ref blnIsShowSupplier, ref isPerce, AgentId, hotelRating, SearchPara.CityID, SearchPara.CountryId);
                //    if (dtAgentDetail != null)
                //    {
                //        IsParentId = Convert.ToInt32(dtAgentDetail.Rows[0]["IsParentId"]);
                //        SubAgentMarkup = Convert.ToDouble(dtAgentDetail.Rows[0]["B2CMarkupPer"]);
                //    }
                //    double VatOnPolicy = 0;
                //    cancellationChargeAmount = Common.ProcessHotelCancelationPrice_WithMarkup(SearchPara.Rooms.Count * SearchPara.NoofNights, SearchPara.CityID, SearchPara.CountryId, cancellationChargeAmount, isPerce, B2CMarkup, AgentPercentage, AgentMarkupAmount, IsParentId, SubAgentMarkup, ref VatOnPolicy);
                //    #endregion

                //    DateTime FromDate = DateTime.Now.Date;
                //    ValueType RemainingDays = (data.policies[i].days_remaining).Value;
                //    DateTime checkIn = Convert.ToDateTime(Checking);
                //    DateTime FinalDate = checkIn.AddDays(-Convert.ToInt32(RemainingDays)).Date;
                //    DateTime ToDate = FinalDate.AddDays(CutOffDays);
                //    //if (ownsystemid == "355963" || ownsystemid == "344643")
                //    //    ToDate = DateTime.Now.Date;
                //    if (FromDate < ToDate && chechk == false)
                //    {
                //        dtHotelsProRoomCacelationPolicy.Rows.Add(setCancellationPolicyRow(dtHotelsProRoomCacelationPolicy.NewRow(), 1, "USD", provider, Convert.ToString(ownsystemid), (1).ToString(), roomtypeid, "", Convert.ToString(roomtypecode), DateTime.Now.Date, ToDate.AddDays(-1).AddHours(23).AddMinutes(59).AddSeconds(59), 0, 0, "", 0, "", "", "", additionalInfo, "", ""));
                //        dtHotelsProRoomCacelationPolicy.AcceptChanges();
                //        chechk = true;
                //    }
                //    if (FromDate > ToDate)
                //        ToDate = DateTime.Now;
                //    dtHotelsProRoomCacelationPolicy.Rows.Add(setCancellationPolicyRow(dtHotelsProRoomCacelationPolicy.NewRow(), 1, "USD", provider, Convert.ToString(ownsystemid), (1).ToString(), roomtypeid, "", Convert.ToString(roomtypecode), ToDate, FromDate, cancellationChargeAmount, cancellationChargeAmount, "", 0, "", "", "", additionalInfo, "", ""));
                //    dtHotelsProRoomCacelationPolicy.AcceptChanges();
                //}
                dsHotel.Tables[2].Merge(dtHotelsProRoomCacelationPolicy);
            }
            catch (Exception ex)
            {
                _ = _exception.ErrorNew(ex, _LogAPI.Recheck, SearchKey);
                cancellationDto.dsHotelData = null;
                cancellationDto.ISBooking = "NotActive";
                return cancellationDto;
            }

            cancellationDto.dsHotelData = dsHotel;
            cancellationDto.ISBooking = "Active";
            return cancellationDto;
        }
        public async Task<string> HotelsPro_EInfo(string ownsystemid, string roomtypeid, DataTable dtSuplierMarkup, DataTable dtSearchConfig, string provider, string SearchKey)
        {
            string EssentialInfo = "";
            try
            {

                string HotelsProResponse = "";
                DataRow[] drSuplierMarkup = (dtSuplierMarkup.Select("SupplierName='" + provider + "'"));
                int SupplierId = Convert.ToInt32(drSuplierMarkup[0]["SupplierID"]);
                DataRow[] drSearchConfig = (dtSearchConfig.Select("SupplierID='" + SupplierId + "'"));
                string[] Rcode = roomtypeid.Split(new String[] { "$$$" }, StringSplitOptions.None);
                string code = "provision/" + Rcode[3];
                string ErrorResponse = "";
                ResponseDto responseDto = await ProResponse(code, Convert.ToString(drSuplierMarkup[0]["WebSiteURL"]), Convert.ToString(drSuplierMarkup[0]["UserName"]), Convert.ToString(drSuplierMarkup[0]["Password"]), Convert.ToInt32(drSearchConfig[0]["HotelSearchTimeOut"]), SearchKey, false,true,"" );
                HotelsProResponse = responseDto.Response;

                _commonMethod.ThirdPartyGetPolicyHistory(Convert.ToString(roomtypeid), Convert.ToString(ownsystemid), code, Convert.ToString(HotelsProResponse), "After Get Policy", provider, "Response", "-", "");
                if (HotelsProResponse.ToLower().Contains("eanwserror") || HotelsProResponse.Trim() == "")
                {
                    _commonMethod.ThirdPartyGetPolicyHistory(Convert.ToString(roomtypeid), Convert.ToString(ownsystemid), code, Convert.ToString(ErrorResponse), "After Get Policy", provider, "Response", "-", "");
                    //return dsHotel;
                }
                else
                {
                    _commonMethod.ThirdPartyGetPolicyHistory(Convert.ToString(roomtypeid), Convert.ToString(ownsystemid), code, Convert.ToString(HotelsProResponse), "After Get Policy", provider, "Response", "-", "");
                }

                dynamic data = JObject.Parse(HotelsProResponse);
                //int Poliocy = (data.policies.Count);
                //string Checking = (data.checkin).Value;
                //string price = (data.price).Value;
                //double roomprice = Convert.ToDouble(price);
                string additionalInfo = "";
                if (HotelsProResponse.Contains("additional_info") == true)
                    additionalInfo = (data.additional_info).Value;
                else
                    additionalInfo = "N/A";
                EssentialInfo = additionalInfo;
            }

            catch (Exception ex)
            {
                _ = _exception.ErrorNew(ex, _LogAPI.Recheck,SearchKey);
                return "";
            }
            return EssentialInfo;
        }

        public  async Task<ResponseDto> ProResponse(string Xmlrequest, string Url, string username, string password, int Timeout, string Searchkey = "",bool ForRoom=false, bool FromBooking = false,string RequestBody="")
        {
            string responseString = "";
            string ErrorResponse = "";
            ResponseDto responseDto = new ResponseDto();
            try
            {
                string Key = Convert.ToBase64String(Encoding.ASCII.GetBytes(username + ":" + password));
                string reqUrl = Url + Xmlrequest;
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(reqUrl);

                var postData = "";

                if (!FromBooking)
                {
                    postData = Xmlrequest;
                }
                else
                {
                    postData = RequestBody;
                }
                var data = Encoding.ASCII.GetBytes(postData);
                
                if(ForRoom)
                    request.Method = "GET";
                else
                    request.Method = "POST";

                request.Timeout = Timeout;
                request.ContentType = "application/x-www-form-urlencoded";
                request.Headers.Add("Authorization", "Basic " + Key);
                

                if (!ForRoom)
                {
                    request.ContentLength = data.Length;
                    using (var stream = request.GetRequestStream())
                    {
                        stream.Write(data, 0, data.Length);
                    }
                }
               
                var response = (HttpWebResponse)await request.GetResponseAsync();
                responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();
            }
            catch (Exception ex)
            {
                _ = _exception.Error(ex, Searchkey);
                ErrorResponse = ex.Message.ToString();
            }

            responseDto.Response = responseString;
            responseDto.Error = ErrorResponse;

            return responseDto;
        }

        //prebook

        public async Task<PriceCheckhotelsproDto> GetPriceUpdateRQRS(string supplierName, string HTL_RoomCode, DataTable dtCheckPriceReqStructure, DataSet dsCartDetails, DataRow[] HotelsProDetails, string UniqueNo, string AgentUniqueNo, int i, string IsFor, double CompanyBuyPrice, bool IsError, double itemprice, string roomCode, string Searchkey = "", int AgentId = 0)
        {
            PriceCheckhotelsproDto priceCheckhotelsproDto = new PriceCheckhotelsproDto();

            string responsetype = "Success";
            string time = "";
            string SupplierName = supplierName;
            string RoomDetailsRequest = "";
            string ResponseDataHP = "";
            string ErrorResponse = "";
            try
            {
                RoomDetailsRequest = "provision/" + HTL_RoomCode;
                _commonMethod.ThirdPartyBookingHistory(Convert.ToString(UniqueNo), Convert.ToString(AgentUniqueNo), RoomDetailsRequest, "-", "Before Request hit Price Check", Convert.ToString(dsCartDetails.Tables[1].DefaultView[0]["SupplierName"]), "Request Confirm", "-", "WithInearth");
                dtCheckPriceReqStructure.Rows.Add(_commonMethod.AddCheckPriceReq(dtCheckPriceReqStructure.NewRow(), 0 + 1, Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[0]["UserCartID"]), Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[0]["UserCartDetailId"]), Convert.ToString(dsCartDetails.Tables[1].DefaultView[0]["RoomTypeID"]), Convert.ToString(dsCartDetails.Tables[1].DefaultView[0]["SupplierName"]).ToLower(), RoomDetailsRequest));


                System.Diagnostics.Stopwatch timerroomprocess = new System.Diagnostics.Stopwatch();
                timerroomprocess.Start();

                ResponseDto responseDto = await ProResponse(RoomDetailsRequest, Convert.ToString(HotelsProDetails[0]["WebSiteURL"]), Convert.ToString(HotelsProDetails[0]["UserName"]), Convert.ToString(HotelsProDetails[0]["Password"]), 120000, Searchkey);
                ResponseDataHP = responseDto.Response;
                ErrorResponse = responseDto.Error;

                timerroomprocess.Stop();
                time = timerroomprocess.Elapsed.ToString();

                _commonMethod.ThirdPartyBookingHistory(Convert.ToString(UniqueNo), Convert.ToString(AgentUniqueNo), RoomDetailsRequest, ResponseDataHP + ErrorResponse, "After Request hit Price Check", supplierName, "Response Confirm", "-", "WithInearth");
                if (ResponseDataHP.ToLower().Contains("eanwserror") || ResponseDataHP.Trim() == "")
                {
                    try
                    {
                        responsetype = "GeneralError";
                        string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, RoomDetailsRequest, ResponseDataHP, ErrorResponse);
                    }
                    catch (Exception e)
                    {
                        _ = _exception.Error(e, Searchkey);
                    }
                    //  Common.ThirdPartyBookingHistory(Convert.ToString(UniqueNo), Convert.ToString(AgentUniqueNo), RoomDetailsRequest, ErrorResponse, "After Request hit Price Check", supplierName, "Response Confirm", "-", "WithInearth");
                   
                    priceCheckhotelsproDto.Flag = false;
                    return priceCheckhotelsproDto;
                }
                else
                {
                    _commonMethod.ThirdPartyBookingHistory(Convert.ToString(UniqueNo), Convert.ToString(AgentUniqueNo), RoomDetailsRequest, ResponseDataHP, "After Request hit Price Check", supplierName, "Response Confirm", "-", "WithInearth");
                }

                dynamic data = JObject.Parse(ResponseDataHP);
                string Hprice = "";
                roomCode = (data.code).Value;
                priceCheckhotelsproDto.roomCodeHP = roomCode;//23/12/25

                Hprice = (data.price).Value;
                itemprice = Convert.ToDouble(Hprice);
                priceCheckhotelsproDto.FinalPrice = itemprice;

                if (IsFor == "P")
                {
                    if (Convert.ToDouble((itemprice).TruncateDecimalPlaces(0)) > (CompanyBuyPrice).TruncateDecimalPlaces(0))
                    {
                        try
                        {
                            ResponseDataHP = "#Price Changed " + "#Main Price = " + CompanyBuyPrice + " #New Price = " + itemprice + " #Response= " + ResponseDataHP;
                            responsetype = "GeneralError";
                            string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, RoomDetailsRequest, ResponseDataHP, ErrorResponse);
                        }
                        catch (Exception e)
                        {
                            _ = _exception.Error(e, Searchkey);
                        }
                        priceCheckhotelsproDto.Flag = true;
                        priceCheckhotelsproDto.FinalPrice = itemprice;
                        priceCheckhotelsproDto.roomCodeHP = roomCode;
                        return priceCheckhotelsproDto;
                    }
                }
                else
                {
                    if (Convert.ToDouble((itemprice).TruncateDecimalPlaces(0)) > (CompanyBuyPrice).TruncateDecimalPlaces(0))
                    {
                        try
                        {
                            ResponseDataHP = "#Price Changed " + "#Main Price = " + CompanyBuyPrice + " #New Price = " + itemprice + " #Response= " + ResponseDataHP;
                            responsetype = "GeneralError";
                            string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, RoomDetailsRequest, ResponseDataHP, ErrorResponse);
                        }
                        catch (Exception e)
                        {
                            _ = _exception.Error(e, Searchkey);
                        }
                        priceCheckhotelsproDto.Flag = true;
                        priceCheckhotelsproDto.FinalPrice = itemprice;
                        priceCheckhotelsproDto.roomCodeHP = roomCode;
                        return priceCheckhotelsproDto;
                    }

                }
            }
            catch (Exception e)
            {
                try
                {
                    responsetype = "GeneralError";
                    ErrorResponse = e.Message + e.StackTrace.ToString();
                    string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, RoomDetailsRequest, ResponseDataHP, ErrorResponse);
                }
                catch (Exception ex)
                {
                    _ = _exception.Error(ex, Searchkey);
                }
                _ = _exception.Error(e, Searchkey);
                IsError = true;
                priceCheckhotelsproDto.IsErrors = IsError;
            }
            try
            {
                responsetype = "Success";
                string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, RoomDetailsRequest, ResponseDataHP, ErrorResponse);
            }
            catch (Exception e)
            {
                _ = _exception.Error(e, Searchkey);
            }
            priceCheckhotelsproDto.Flag = false;
            return priceCheckhotelsproDto;
        }

        public string HotelsProBookindgRequestNEW(DataTable dtPassengers, DataSet dsCartDetails, string UniqueNo, bool IsPRiceUpdate, bool IsError, string AgentCode, string roomCodeHP, string SearchKey)
        {
            try
            {
                StringBuilder HotelsPro = new StringBuilder();
                if (IsPRiceUpdate == false && IsError == false)
                {
                    HotelsPro.Append("book/");
                    string InternalReference = "Api_" + AgentCode + "_" + UniqueNo;
                    for (int j = 0; j < dsCartDetails.Tables[1].DefaultView.Count; j++)
                    {
                        dtPassengers.DefaultView.RowFilter = "PickUp='" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomTypeId"]) + "' and UserCartID='" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["UserCartID"]) + "' and RoomNo='" + Convert.ToInt32(j + 1) + "'";
                        DataTable dtnewpassanger = dtPassengers.DefaultView.ToTable();
                        string[] HotelCode = Convert.ToString(dsCartDetails.Tables[1].DefaultView[0]["RoomTypeID"]).Split(new String[] { "$$$" }, StringSplitOptions.None);
                        string BookingCodeHotel = HotelCode[3];
                        if (j == 0)
                        {
                            HotelsPro.Append(roomCodeHP + "$$$");
                            //HotelsPro.Append(BookingCodeHotel + "$$$");
                            HotelsPro.Append("agency_ref_id=" + InternalReference);
                        }
                        for (int b = 0; b < dtnewpassanger.Rows.Count; b++)
                        {
                            int A = j + 1;
                            HotelsPro.Append("&name=" + A);

                            if (Convert.ToString(dtnewpassanger.Rows[b]["PaxType"]) == "Adult")
                            {
                                HotelsPro.Append("," + Convert.ToString(dtnewpassanger.Rows[b]["FirstName"]));
                                HotelsPro.Append("," + Convert.ToString(dtnewpassanger.Rows[b]["LastName"]));
                                HotelsPro.Append("," + Convert.ToString(dtnewpassanger.Rows[b]["PaxType"]).ToLower());
                            }
                            else if (Convert.ToString(dtnewpassanger.Rows[b]["PaxType"]) == "Child")
                            {
                                HotelsPro.Append("," + Convert.ToString(dtnewpassanger.Rows[b]["FirstName"]));
                                HotelsPro.Append("," + Convert.ToString(dtnewpassanger.Rows[b]["LastName"]));
                                HotelsPro.Append("," + Convert.ToString(dtnewpassanger.Rows[b]["PaxType"]).ToLower());
                                int childage = Convert.ToInt32(dtnewpassanger.Rows[b]["ChildAge"]);
                                HotelsPro.Append("," + childage);
                            }
                        }
                    }
                    string HotelsProREQ = HotelsPro.ToString();
                    return HotelsProREQ;
                }
                else
                    return "";

            }
            catch (Exception ex)
            {
                _ = _exception.ErrorNew(ex, _LogAPI.Book, SearchKey);
                return "";
            }
        }
    }
}
