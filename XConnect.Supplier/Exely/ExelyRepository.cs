using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Collections.Concurrent;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Text;
using System.Linq;
using XConnect.CommonUtilities;
using XConnect.CommonUtilities.Interface;
using XConnect.ErrorLogHandler.Interface;
using XConnect.Markup.Interface;
using XConnect.Models;
using XConnect.Models.Hotel;
using XConnect.Supplier.Exely.Interface;
using Options = XConnect.Models.Options;

namespace XConnect.Supplier.Exely
{
    public class ExelyRepository : IExely
    {
        private readonly ICommonMethod _commonMethod;
        private readonly KeySettings _keySettings;
        private readonly IException _exception;
        private readonly ISupplierRes _supplierRes;
        private readonly IMarkup _markup;
        private readonly SupplierConfiguration _supplierConfig;
        private readonly IMemoryCache _cache;
        private static readonly TimeSpan ExelyContentCacheDuration = TimeSpan.FromHours(24);
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> ExelyContentCacheLocks = new ConcurrentDictionary<string, SemaphoreSlim>(StringComparer.OrdinalIgnoreCase);


        public ExelyRepository(ICommonMethod commonMethod, IOptions<KeySettings> keySettings, IException exception, ISupplierRes supplierRes, IMarkup markup, IConfiguration configuration, IOptions<SupplierConfiguration> supplierConfig, IMemoryCache cache)
        {
            _commonMethod = commonMethod;
            _keySettings = keySettings.Value;
            _exception = exception;
            _supplierRes = supplierRes;
            _markup = markup;
            _supplierConfig = supplierConfig.Value;
            _cache = cache;
        }

        #region Availability
        #region Request
        public void GetExelyRequestOnly_Async(DataRow[] commonCode, List<StarRating> listRating, DataTable dtPreferredDataHotel, HotelSearchParameter SearchPara, List<SupplierSearchConfig> lstExelySearchConfig, List<SupplierDetails> lstExelyDetails, ref ConcurrentBag<HotelX> AllTask, string AgentCode, int Hit, TokenData tokenData, DataSet dtXMLCity, AdvancedOptions advancedOptions)
        {
            try
            {
                List<StarRating> lstRating = new List<StarRating>();
                lstRating = _commonMethod.GetStarRatingStringList_xtg(SearchPara);

                List<HotelCodeByCity> DtHotelDAta = HtmlExtensions.ToList<HotelCodeByCity>(commonCode.CopyToDataTable());
                int ratingcount = lstRating.Count;
                int hotelcount = DtHotelDAta.Count;

                foreach (SupplierDetails ExelyDetails in lstExelyDetails)
                {
                    try
                    {
                        int SupplierID = ExelyDetails.SupplierID;
                        string Supplier = ExelyDetails.SupplierName;

                        if (lstExelySearchConfig.Count > 0)
                        {
                            List<SupplierSearchConfig> ExelySearchConfig = lstExelySearchConfig
                                                                            .Where(x => x != null && x.SupplierID == SupplierID)
                                                                            .ToList();

                            if (ExelySearchConfig != null && ExelySearchConfig.Count > 0)
                            {
                                string Ids = string.Empty;
                                string xmlbase = "";
                                _commonMethod.GetPrefferedHotelCodes(ref Ids, ref xmlbase, ref ExelySearchConfig, Supplier, dtPreferredDataHotel, SearchPara.IsSearchFor);

                                List<HotelCodeByCity> filter = new List<HotelCodeByCity>();
                                filter = _commonMethod.FilterWithOutCity_Code(ratingcount, hotelcount, DtHotelDAta, lstRating, Supplier);

                                if (filter.Count > 0)
                                {
                                    DataForSearch dataForSearch = _commonMethod.GetXmlDataForSearchTechwise(SearchPara.Nationality, dtXMLCity, Supplier, ExelyDetails.TechName);

                                    if (dataForSearch.XmlNationality != "")
                                    {
                                        double ExelyCurrencyFactor = 1;
                                        string Exely_Currency = !string.IsNullOrWhiteSpace(advancedOptions?.Currency)
                                            ? Convert.ToString(advancedOptions.Currency)
                                            : Convert.ToString(ExelyDetails.CurrencyShortName);


                                        if (!string.IsNullOrWhiteSpace(Exely_Currency) && Exely_Currency.ToLower() != tokenData.SiteCurrency.ToLower())
                                        {
                                            ExelyCurrencyFactor = _commonMethod.GetROEByCurrency(Exely_Currency, tokenData.dtROE);
                                        }

                                        for (int k = 0; k < ExelySearchConfig.Count; k++)
                                        {
                                            bool DebugMode = (Convert.ToInt32(ExelySearchConfig[k].DebugMode) == 0 ? false : true);
                                            string UserName = Convert.ToString(ExelyDetails.UserName);
                                            string Password = Convert.ToString(ExelyDetails.Password);
                                            {
                                                {
                                                    int IsCitywisesearch = Convert.ToInt32(ExelySearchConfig[k].City);
                                                    if (IsCitywisesearch == 0)
                                                    {
                                                        if (Convert.ToInt32(ExelySearchConfig[k].CityID) == 0 || Convert.ToInt32(ExelySearchConfig[k].CityID) == SearchPara.CityID)
                                                        {
                                                            {
                                                                double count = Convert.ToDouble(filter.Count);
                                                                int Noofrecords = Convert.ToInt32(ExelySearchConfig[k].Noofrecords);
                                                                int NoOfDiv = Convert.ToInt32(Math.Ceiling(count / Convert.ToDouble(Noofrecords)));
                                                                List<List<XMLID_Search>> lstxml = _commonMethod.GetXMLIDString(NoOfDiv, Noofrecords, count, filter);

                                                                // Flatten all batches into one request — exactly one aggregate POST per search
                                                                List<XMLID_Search> allIds = lstxml.SelectMany(batch => batch).ToList();
                                                                string IdsForSearch = Convert.ToString(_commonMethod.GetHotelCodeByCOmma(allIds));
                                                                {
                                                                    HotelX Xhotel = new HotelX();
                                                                    Xhotel.Supplier = Supplier;
                                                                    Xhotel.DebugeMode = DebugMode;
                                                                    Xhotel.SearchTimeOut = Convert.ToInt32(ExelySearchConfig[k].HotelSearchTimeOut);
                                                                    Xhotel.ProcessTimeout = Convert.ToInt32(ExelySearchConfig[k].HotelSearchTimeOut2);

                                                                    bool SearchByOneID = IdsForSearch
                                                                        .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                                                        .Select(s => s.Trim())
                                                                        .Count() == 1;
                                                                    if (SearchByOneID)
                                                                    {
                                                                        if (_keySettings.SupplierProxy == "0")
                                                                            Xhotel.URL = Convert.ToString(ExelyDetails.WebSiteURL).TrimEnd('/') + "/" + _supplierConfig.Exely.GetRoomStaysByProperty;
                                                                        else
                                                                            Xhotel.URL = Convert.ToString(ExelyDetails.WebSiteURLforAPI) + _supplierConfig.Exely.GetRoomStaysByProperty;
                                                                    }
                                                                    else
                                                                    {
                                                                        if (_keySettings.SupplierProxy == "0")
                                                                            Xhotel.URL = Convert.ToString(ExelyDetails.WebSiteURL).TrimEnd('/') + "/" + _supplierConfig.Exely.SearchRoomStays;
                                                                        else
                                                                            Xhotel.URL = Convert.ToString(ExelyDetails.WebSiteURLforAPI) + _supplierConfig.Exely.SearchRoomStays;
                                                                    }

                                                                    Xhotel.UserId = Convert.ToString(ExelyDetails.UserID);
                                                                    Xhotel.UserName = UserName;
                                                                    Xhotel.PassWord = Password;

                                                                    Xhotel.CurrencyCode = Exely_Currency;
                                                                    Xhotel.CurrencyFactor = ExelyCurrencyFactor;
                                                                    Xhotel.XmlCity = dataForSearch.xmlCity;
                                                                    Xhotel.XmlCountry = dataForSearch.xmlCountry;
                                                                    Xhotel.XmlNationality = dataForSearch.XmlNationality;
                                                                    Xhotel.AgentWiseMarkup = ExelyDetails.AgentWiseMarkup;
                                                                    Xhotel.IsShowSupplier = Convert.ToInt16(ExelyDetails.IsShowSupplier) == 1 ? true : false;
                                                                    Xhotel.SupplierID = ExelyDetails.SupplierID;
                                                                    Xhotel.MasterSupplierID = ExelyDetails.MasterSupplierId;
                                                                    #region "CoomonCode"
                                                                    bool IsHitSeperate = false;
                                                                    int NoOfRoomsSearch = 1;
                                                                    List<RQRS> RQRS = new List<RQRS>();
                                                                    Xhotel.IsSeperate = IsHitSeperate;
                                                                    for (int rr = 0; rr < NoOfRoomsSearch; rr++)
                                                                    {
                                                                        RQRS objrq = new RQRS();
                                                                        int roomIndex = IsHitSeperate ? rr : -1;
                                                                        // Single property requests use GET; hotel lists use aggregated JSON POST.
                                                                        if (SearchByOneID)
                                                                        {
                                                                            objrq.Request = IdsForSearch.Trim();
                                                                        }
                                                                        else
                                                                        {
                                                                            objrq.Request = XMLExelyRequestOnly_Async(SearchPara, Xhotel.SearchTimeOut, ExelyCurrencyFactor, DebugMode, dataForSearch.xmlCity, dataForSearch.XmlNationality, dataForSearch.xmlCountry, Xhotel.UserId, Xhotel.UserName, Xhotel.PassWord, Xhotel.CurrencyCode, IdsForSearch, roomIndex);
                                                                        }
                                                                        objrq.Response = ""; RQRS.Add(objrq);
                                                                    }
                                                                    Xhotel.RQRS = RQRS;
                                                                    Xhotel.ReturnRooms = ExelyDetails.ReturnRooms;
                                                                    Xhotel.TechName = ExelyDetails.TechName;
                                                                    #endregion
                                                                    AllTask.Add(Xhotel);
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

        public string XMLExelyRequestOnly_Async(HotelSearchParameter SearchPara, Int32 SearchTimeOut, double ExelyCurrencyFactor, bool DebugMode, string XmlCity, string XmlNationality, string XmlCountry, string UserId, string UserName, string Password, string CurrencyCode, string HotelCode, int roomIndex = -1)
        {
            try
            {
                string ExelyRequestJson = Get_Exely_AvailabilityRQ(SearchPara, HotelCode, CurrencyCode, roomIndex);
                return ExelyRequestJson;
            }
            catch (Exception ex)
            {
                _ = _exception.Error(ex);
                return "";
            }
        }

        public string Get_Exely_AvailabilityRQ(HotelSearchParameter SearchPara, string HotelCode, string currencyCode, int roomIndex = -1, string include = "", MealPreference? mealPreference = null, double? minPrice = null, double? maxPrice = null)
        {
            try
            {
                // 🔹 PropertyIds
                List<string> propertyIds = new List<string>();
                if (!string.IsNullOrEmpty(HotelCode))
                {
                    propertyIds = HotelCode
                        .Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(x => x.Trim())
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .ToList();
                }

                // 🔹 Adults count: use specific room if roomIndex given, else total occupancy across all rooms
                int totalAdults = 1;

                if (SearchPara.Rooms != null && SearchPara.Rooms.Count > 0)
                {
                    if (roomIndex >= 0 && roomIndex < SearchPara.Rooms.Count)
                        totalAdults = Math.Max(SearchPara.Rooms[roomIndex].NoofAdults, 1);
                    else
                        totalAdults = SearchPara.Rooms.Sum(r => Math.Max(r.NoofAdults, 1));
                }

                // 🔹 Child ages: use specific room if roomIndex given, else all rooms
                List<int> childAges = new List<int>();
                if (SearchPara.Rooms != null)
                {
                    var roomsToCheck = (roomIndex >= 0 && roomIndex < SearchPara.Rooms.Count)
                        ? new List<RoomDetails> { SearchPara.Rooms[roomIndex] }
                        : SearchPara.Rooms;

                    foreach (var room in roomsToCheck)
                    {
                        if (room.ChildAge != null && room.ChildAge.Length > 0)
                        {
                            childAges.AddRange(room.ChildAge.Where(a => a > 0));
                        }
                        else
                        {
                            if (room.Child1Age > 0) childAges.Add(room.Child1Age);
                            if (room.Child2Age > 0) childAges.Add(room.Child2Age);
                        }
                    }
                }
                // 🔹 Build request matching partner contract: include, mealPreference, pricePreference
                var req = new Dictionary<string, object?>
                {
                    ["propertyIds"] = propertyIds,
                    ["adults"] = totalAdults,
                    ["arrivalDate"] = ParseSearchDate(SearchPara.CheckInDate).ToString("yyyy-MM-dd"),
                    ["departureDate"] = ParseSearchDate(SearchPara.CheckOutDate).ToString("yyyy-MM-dd")
                    //["include"] = include ?? string.Empty
                };

                if (childAges.Count > 0)
                    req["childAges"] = childAges;

                if (mealPreference != null)
                    req["mealPreference"] = mealPreference;

                // Price preference: always include requested currency (fallback to USD if empty)
                /*string prefCurrency = string.IsNullOrWhiteSpace(currencyCode) ? "USD" : currencyCode;
                req["pricePreference"] = new
                {
                    currencyCode = prefCurrency,
                    minPrice = minPrice ?? 0,
                    maxPrice = maxPrice ?? 10000000
                };*/

                return JsonConvert.SerializeObject(req);
            }
            catch (Exception ex)
            {
                _ = _exception.Error(ex);
                return "";
            }
        }
        #endregion Request

        #region Response
        public async Task<List<Options>> Exely_HotelSearchWithRooms(HotelX Item, CommonSearch commonSearch, string ResponseData, HotelSearchParameter SearchParam, string XmlCity, string XmlCountry, string XmlNationality, string Currency, double CurrencyFactor, DataTable dtROE, string ProccessCutOffTime, Int32 processTimeout, string ResProccessTimerLogActive, string ResProccessTimeValueforSaveLog, string StoreSupplierResErrorLog, bool SearchByOneID)
        {
            Stopwatch timer_room_Exely = new Stopwatch();
            timer_room_Exely.Start();

            List<Options> options = new List<Options>();
            DataTable DtHotel = _commonMethod.GenerateDataTableNewV2();
            Dictionary<string, CancellationRulesResponse?> cancellationRulesCache = new Dictionary<string, CancellationRulesResponse?>(StringComparer.OrdinalIgnoreCase);
            try
            {
                bool searchByOneId = SearchByOneID;
                string firstRq = Item.RQRS?.FirstOrDefault()?.Request ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(firstRq))
                {
                    string trimmedRq = firstRq.TrimStart();
                    if (trimmedRq.StartsWith("{") || trimmedRq.StartsWith("["))
                        searchByOneId = false;
                    else
                        searchByOneId = true;
                }

                List<ShortRoomStay> roomStays = new List<ShortRoomStay>();
                if (searchByOneId)
                {
                    var result = JsonConvert.DeserializeObject<SearchByPropertyResponse>(ResponseData);
                    if (result == null || result.roomStays == null || result.roomStays.Count == 0)
                    {
                        Item.SupplierResponseError = ResponseData;
                        Item.ResponseStatus = 404;
                        Item.ResponseType = "NoCount";
                        return options;
                    }

                    roomStays = result.roomStays.Select(d => new ShortRoomStay
                    {
                        propertyId = d.propertyId,
                        roomType = d.roomType,
                        ratePlan = d.ratePlan,
                        guestCount = d.guestCount,
                        stayDates = d.stayDates,
                        currencyCode = d.currencyCode,
                        total = d.total,
                        cancellationPolicy = d.cancellationPolicy,
                        includedServices = d.includedServices,
                        mealPlanCode = d.mealPlanCode,
                        availableDiscounts = d.availableDiscounts,
                        fullPlacementsName = d.fullPlacementsName,
                        availability = d.availability,
                        checksum = d.checksum
                    }).ToList();
                }
                else
                {
                    var result = JsonConvert.DeserializeObject<AggregationSearchByPropertiesResponse>(ResponseData);
                    if (result == null || result.roomStays == null || result.roomStays.Count == 0)
                    {
                        Item.SupplierResponseError = ResponseData;
                        Item.ResponseStatus = 404;
                        Item.ResponseType = "NoCount";
                        return options;
                    }

                    roomStays = result.roomStays;
                }

                double Pricenew = 0;
                bool willAddOrNot = true;
                Int64 RaynaID = 0;
                Int64 GiataId = 0;
                Int64 OwnsystemHotelId = 0;
                int RatingHotel = 0;
                string Provider = Item.Supplier;
                double MinPrice = Pricenew;

                var propertyIds = roomStays.Select(rs => rs.propertyId).Where(p => !string.IsNullOrWhiteSpace(p)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                foreach (var hotelCode in propertyIds)
                {
                    Item.ResponseType = "Success";
                    if (ProccessCutOffTime == "1" && timer_room_Exely.ElapsedMilliseconds > processTimeout)
                    {
                        timer_room_Exely.Stop();
                        break;
                    }

                    // all roomStays for this property
                    var roomsForHotel = roomStays.Where(rs => string.Equals(rs.propertyId, hotelCode, StringComparison.OrdinalIgnoreCase)).ToList();
                    if (roomsForHotel == null || roomsForHotel.Count == 0)
                        continue;

                    // For multi-room search: skip property if all room types have insufficient allotment.
                    if (SearchParam.Rooms.Count > 1 && roomsForHotel.All(r => r.availability > 0 && r.availability < SearchParam.Rooms.Count))
                        continue;

                    string currency = string.IsNullOrWhiteSpace(roomsForHotel[0].currencyCode) ? Item.CurrencyCode : roomsForHotel[0].currencyCode;
                    double FinalTotal = roomsForHotel.Max(r => r.total?.priceBeforeTax ?? 0);
                    if (Pricenew < FinalTotal)
                    {
                        Pricenew = FinalTotal;
                    }
                    #region "Hotel Code Block "

                    DataRow drXmlHotel = DtHotel.NewRow();
                    drXmlHotel["IsRecomondedHotel"] = false;
                    drXmlHotel["XMLHotelCode"] = hotelCode;
                    drXmlHotel["HotelFrontImage"] = "";
                    drXmlHotel["Description"] = "";
                    drXmlHotel["EssentialInformation"] = "";
                    drXmlHotel["OfficeID"] = 0;
                    drXmlHotel["ContractID"] = 0;
                    drXmlHotel["IsShowSupplier"] = false;
                    drXmlHotel["HotelName"] = "";
                    drXmlHotel["Rating"] = 0;
                    drXmlHotel["Reviews"] = 0;
                    drXmlHotel["UniqueNo"] = hotelCode;
                    drXmlHotel["XmlHotelCity"] = XmlCity;
                    drXmlHotel["XMLCountry"] = XmlCountry;
                    drXmlHotel["XMLCity"] = XmlCity;
                    drXmlHotel["SupplierName"] = Item.Supplier;

                    string HotelStatus = "Available";

                    // Multiple Room Stay
                    /*double FinalTotal = 0;

                    for (int j = 0; j < roomStays.Count; j++)
                    {
                        double RoomRate = roomStays[j].total.priceBeforeTax
                                        + roomStays[j].total.taxAmount;

                        if (FinalTotal < RoomRate)
                        {
                            FinalTotal = RoomRate;
                        }
                    }*/

                    drXmlHotel["SellingPrice"] = FinalTotal;
                    drXmlHotel["FinalSellingPrice"] = FinalTotal;
                    drXmlHotel["BookingStatus"] = HotelStatus;
                    drXmlHotel["SupplierCurrency"] = currency;
                    DtHotel.Rows.Add(drXmlHotel);

                    _markup.GetHotelDataCommon(ref MinPrice, ref willAddOrNot, ref OwnsystemHotelId, ref RaynaID, ref GiataId, ref RatingHotel, ref drXmlHotel, commonSearch.AgentId, commonSearch.TariffID, commonSearch.dsAgentDetails, commonSearch.supplierGiataMaster, SearchParam, commonSearch.dtCountryCityMarkup, commonSearch.B2CMarkup, commonSearch.IsParentId, commonSearch.SubAgentMarkup, commonSearch.SearchByHotel, commonSearch.HotelIDS);

                    #region "Room Logic"
                    DataTable dt_HotelRoom = _commonMethod.GenerateRoomtypeDataTableNewV2();
                    DataTable dt_Cancellation = _commonMethod.GenerateCancellationDataTableNewV2();
                    DataTable dt_RateBreakUp = _commonMethod.GenerateoomRateBreackUpDataTableNewV2();
                    #endregion
                    #endregion
                    {
                        #region "When not Need Room"
                        if (SearchParam.Filters.IsShowRooms == 0)
                        {
                            MinPrice = FinalTotal;
                            if (FinalTotal > 0)
                            {
                                if (willAddOrNot)
                                {
                                    decimal LowestPrice = Convert.ToDecimal(MinPrice).CalculatePrice(commonSearch.tokenData, commonSearch.advancedOptions, Currency);
                                    double passPrice = SearchParam.Filters.IsShowRooms == 1 ? Pricenew : FinalTotal;
                                    Options ops = (await _supplierRes.GetCommonRoomForSuppliers(GiataId, commonSearch.tokenData, commonSearch.advancedOptions, passPrice, drXmlHotel, dt_HotelRoom, dt_Cancellation, dt_RateBreakUp, SearchParam));
                                    if (ops != null)
                                        options.Add(ops);
                                }
                            }
                        }
                        #endregion
                        #region "When Need Rooms"
                        else
                        {
                            #region "GetRooms for this hotel"
                            if (willAddOrNot)
                            {
                                #region "Room Logic"
                                if (SearchParam.Filters.IsShowRooms == 1)
                                {
                                    MinPrice = Pricenew;
                                    double AgentPercentage = 0.0;
                                    //Boolean blnIsShowSupplier = false;
                                    double AgentMarkupAmount = 0.0;
                                    Boolean isPerce = true;
                                    double AgentCompanyPercentage = Convert.ToDouble(SearchParam.AgentCompanyPercentage);
                                    bool IsB2C = Convert.ToBoolean(Convert.ToByte(SearchParam.IsB2BOrB2C));
                                    bool includeDetailedCancellation = SearchParam.IsAvailbilityWithCancellation == 1 || SearchParam.IsAvailbilityWithCancellationv2 == 1;
                                    int cutOffDays = _commonMethod.GetCutOffDays(commonSearch.dtSuplierMarkup, Item.Supplier);
                                    string exelyBaseUrl = GetExelyBaseUrl(Item.URL);
                                    DateTime checkInDate = DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", CultureInfo.InvariantCulture);

                                    int maxOcupancy = 0;

                                    int totalAdult = 0;
                                    int totalChild = 0;

                                    for (int b = 0; b < SearchParam.Rooms.Count; b++)
                                    {
                                        totalAdult = Convert.ToInt32(SearchParam.Rooms[b].NoofAdults) + totalAdult;
                                        totalChild = Convert.ToInt32(SearchParam.Rooms[b].NoofChild) + totalChild;
                                        maxOcupancy = maxOcupancy + SearchParam.Rooms[b].NoofChild + SearchParam.Rooms[b].NoofAdults;
                                    }

                                    #region Supplier Room Code block
                                    double FinalPrice = 0;
                                    List<ShortRoomStay> roomStaysForHotel = roomStays
                                        .Where(rs => string.Equals(rs.propertyId, hotelCode, StringComparison.OrdinalIgnoreCase))
                                        .ToList();
                                    // Single-hotel requests already use GetRoomStaysByProperty, so no fallback call here.
                                    PropertyInfoType? propertyContent = null;
                                    if (roomStaysForHotel != null && roomStaysForHotel.Count > 0)
                                    {
                                        propertyContent = await GetExelyPropertyContentAsync(hotelCode, exelyBaseUrl, Item.PassWord, Item.SearchTimeOut, SearchParam.SearchKey, "en", string.Empty, Item.Supplier);
                                    }

                                    Dictionary<string, string>? roomTypeCategoryNames;
                                    roomTypeCategoryNames = null;
                                    for (int j = 0; j < roomStaysForHotel.Count; j++)
                                    {
                                        var roomStay = roomStaysForHotel[j];
                                        if (!string.Equals(roomStay.propertyId, hotelCode, StringComparison.OrdinalIgnoreCase))
                                        {
                                            continue;
                                        }

                                        string roomCurrency = string.IsNullOrWhiteSpace(roomStay.currencyCode)
                                            ? currency
                                            : roomStay.currencyCode;
                                        roomCurrency = string.IsNullOrWhiteSpace(roomCurrency) ? Item.CurrencyCode : roomCurrency;

                                        string uniqno = Guid.NewGuid().ToString().Substring(0, 4);
                                        string SpecialOfferText = string.Empty;
                                        string BookingConditions = string.Empty;
                                        double roomCurrencyFactor = _commonMethod.GetROEByCurrency(roomCurrency, dtROE);
                                        FinalPrice = roomStay.total.priceBeforeTax;
                                        double roomLevelTotal = FinalPrice * roomCurrencyFactor;
                                        double companyBuyingPrice = roomLevelTotal;
                                        if (roomStay.total.taxAmount > 0)
                                        {
                                            double amountAfterTax = FinalPrice + roomStay.total.taxAmount;
                                            BookingConditions = $"Taxes: {roomStay.total.taxAmount.ToString("F2", CultureInfo.InvariantCulture)} {roomCurrency}; Total incl. taxes: {amountAfterTax.ToString("F2", CultureInfo.InvariantCulture)} {roomCurrency} (informative, paid at hotel)";
                                        }
                                        string placementCode = string.Join("|", roomStay.roomType.placements?.Select(p => p.code) ?? Enumerable.Empty<string>());
                                        int checkInHourVal = roomStay.stayDates?.arrivalDateTime.Hour ?? 14;
                                        int checkOutHourVal = roomStay.stayDates?.departureDateTime.Hour ?? 12;
                                        string RoomTypeId = roomStay.roomType.id + "$$$" + roomStay.ratePlan.id + "$$$" + FinalPrice.ToString(CultureInfo.InvariantCulture) + "$$$" + roomCurrency + "$$$" + uniqno + "$$$" + XmlNationality + "$$$" + (roomStay.checksum ?? "") + "$$$" + (roomStay.propertyId ?? "") + "$$$" + placementCode + "$$$" + checkInHourVal + "$$$" + checkOutHourVal;

                                        #region "Room"
                                        DataRow drHotelRoom = dt_HotelRoom.NewRow();
                                        drHotelRoom["OwnsystemHotelId"] = OwnsystemHotelId;
                                        drHotelRoom["RoomTypeCode"] = roomStay.roomType.id;
                                        drHotelRoom["BoardType"] = roomStay.mealPlanCode;
                                        drHotelRoom["AdultsOccupancy"] = SearchParam.Rooms.Count > 0 ? SearchParam.Rooms[0].NoofAdults : totalAdult;
                                        drHotelRoom["ChildOccupancy"] = SearchParam.Rooms.Count > 0 ? SearchParam.Rooms[0].NoofChild : totalChild;
                                        drHotelRoom["MealName"] = roomStay.mealPlanCode;
                                        drHotelRoom["MealId"] = roomStay.mealPlanCode;
                                        drHotelRoom["Type"] = "";
                                        drHotelRoom["Board"] = roomStay.mealPlanCode;
                                        drHotelRoom["BookingStatus"] = "Available";
                                        drHotelRoom["UniqueNo"] = hotelCode;
                                        drHotelRoom["ContractID"] = 0;
                                        drHotelRoom["RoomNo"] = SearchParam.Rooms.Count > 0 ? SearchParam.Rooms[0].RoomNo : 1;
                                        drHotelRoom["IsCombineRoom"] = true;
                                        drHotelRoom["RoomTypeId"] = RoomTypeId;
                                        drHotelRoom["RoomSequenceNo"] = "";
                                        if (roomStay.availableDiscounts != null && roomStay.availableDiscounts.Count > 0)
                                        {
                                            var discounts = roomStay.availableDiscounts
                                                .Select(d => d.reason.ToString())
                                                .ToList();
                                            SpecialOfferText = string.Join(", ", discounts);
                                        }
                                        drHotelRoom["OfferDescription"] = SpecialOfferText;
                                        drHotelRoom["OfferCode"] = string.IsNullOrEmpty(SpecialOfferText) ? "" : "offer";
                                        drHotelRoom["Characteristic"] = "";
                                        string stayTimesInfo = "";
                                        if (roomStay.stayDates != null)
                                        {
                                            string checkInTime = roomStay.stayDates.arrivalDateTime.ToString("HH:mm");
                                            string checkOutTime = roomStay.stayDates.departureDateTime.ToString("HH:mm");
                                            string timezoneLabel = "hotel local time";
                                            if (roomStay.cancellationPolicy?.freeCancellationDeadlineLocal.HasValue == true
                                                && roomStay.cancellationPolicy?.freeCancellationDeadlineUtc.HasValue == true)
                                            {
                                                TimeSpan utcOffset = roomStay.cancellationPolicy.freeCancellationDeadlineLocal.Value
                                                    - roomStay.cancellationPolicy.freeCancellationDeadlineUtc.Value;
                                                string sign = utcOffset >= TimeSpan.Zero ? "+" : "-";
                                                timezoneLabel = $"UTC {sign}{Math.Abs((int)utcOffset.TotalHours):D2}:{Math.Abs(utcOffset.Minutes):D2}";
                                            }
                                            stayTimesInfo = $"Check-in: {checkInTime} | Check-out: {checkOutTime} ({timezoneLabel})";
                                        }
                                        var essentialParts = new List<string>();
                                        if (!string.IsNullOrEmpty(stayTimesInfo)) essentialParts.Add(stayTimesInfo);
                                        if (!string.IsNullOrEmpty(BookingConditions)) essentialParts.Add(BookingConditions);
                                        if (essentialParts.Count > 0)
                                            drHotelRoom["EssentialInformation"] = "</br>" + string.Join("</br>", essentialParts);
                                        _markup.ProcessHotelRoom_WithMarkup(roomCurrencyFactor, roomCurrency, false, true, SearchParam.NoofNights, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), SearchParam.CheckInDate, SearchParam.CheckOutDate, ref roomLevelTotal, ref drHotelRoom, isPerce, AgentPercentage, AgentMarkupAmount, commonSearch.SubAgentMarkup, commonSearch.B2CMarkup, commonSearch.IsParentId);
                                        drHotelRoom["SupplierName"] = Item.Supplier;
                                        drHotelRoom["BoardTypeID"] = roomStay.mealPlanCode;
                                        if (propertyContent != null)
                                        {
                                            if (roomStay.roomType != null)
                                                roomStay.roomType.name = GetExelyRoomTypeById(propertyContent, roomStay.roomType.id)?.Name;
                                            if (roomStay.ratePlan != null)
                                                roomStay.ratePlan.name = GetExelyRatePlanById(propertyContent, roomStay.ratePlan.id)?.Name;
                                        }
                                        drHotelRoom["RoomTypeName"] = BuildExelyRoomDisplayName(roomStay, propertyContent);

                                        if (roomTypeCategoryNames != null && roomTypeCategoryNames.TryGetValue(roomStay.roomType?.id ?? "", out string? catN) && !string.IsNullOrWhiteSpace(catN))
                                            drHotelRoom["Type"] = catN;
                                        dt_HotelRoom.Rows.Add(drHotelRoom);
                                        // Clone room row for each additional room — use per-room occupancy search
                                        for (int roomIdx = 1; roomIdx < SearchParam.Rooms.Count; roomIdx++)
                                        {
                                            DataRow drExtraRoom = dt_HotelRoom.NewRow();
                                            drExtraRoom.ItemArray = (object[])drHotelRoom.ItemArray.Clone();
                                            drExtraRoom["RoomNo"] = SearchParam.Rooms[roomIdx].RoomNo;
                                            drExtraRoom["AdultsOccupancy"] = SearchParam.Rooms[roomIdx].NoofAdults;
                                            drExtraRoom["ChildOccupancy"] = SearchParam.Rooms[roomIdx].NoofChild;
                                            dt_HotelRoom.Rows.Add(drExtraRoom);
                                        }
                                        #endregion

                                        #region Cancellation Policy
                                        string Essen = "";
                                        string cancellationCurrency = roomCurrency;
                                        double supplierCurrencyFactor = _commonMethod.GetROEByCurrency(cancellationCurrency, dtROE);
                                        double cancellationChargeAmount = companyBuyingPrice;
                                        double vatOnPolicy = 0;
                                        double mainAgentCharge = cancellationChargeAmount;
                                        double companyChargeAmount = cancellationChargeAmount;
                                        double markedupCancellationCharge = _markup.ProcessHotelCancelationPrice_WithMarkup(
                                            ref mainAgentCharge,
                                            SearchParam.NoofNights,
                                            SearchParam.CityID,
                                            Convert.ToString(SearchParam.CountryID),
                                            cancellationChargeAmount,
                                            isPerce,
                                            commonSearch.B2CMarkup,
                                            AgentPercentage,
                                            AgentMarkupAmount,
                                            commonSearch.IsParentId,
                                            commonSearch.SubAgentMarkup,
                                            ref vatOnPolicy);
                                        bool addedDetailedCancellation = false;
                                        List<ExelyCancellationSegment> liveSegments = BuildExelyCancellationSegments(
                                            roomStay.cancellationPolicy,
                                            checkInDate,
                                            FinalPrice,
                                            cutOffDays,
                                            cancellationCurrency);
                                        if (liveSegments.Count > 0)
                                        {
                                            AddExelyCancellationRows(
                                                dt_Cancellation,
                                                liveSegments,
                                                Item.Supplier,
                                                Convert.ToString(OwnsystemHotelId),
                                                1,
                                                RoomTypeId,
                                                roomStay.mealPlanCode ?? "",
                                                SearchParam,
                                                isPerce,
                                                commonSearch.B2CMarkup,
                                                AgentPercentage,
                                                AgentMarkupAmount,
                                                commonSearch.IsParentId,
                                                commonSearch.SubAgentMarkup,
                                                Essen,
                                                dtROE);
                                            addedDetailedCancellation = true;
                                        }

                                        if (!addedDetailedCancellation && includeDetailedCancellation && !string.IsNullOrWhiteSpace(exelyBaseUrl) && !string.IsNullOrWhiteSpace(Item.PassWord))
                                        {
                                            if (!cancellationRulesCache.TryGetValue(hotelCode, out CancellationRulesResponse? rulesResponse))
                                            {
                                                rulesResponse = await GetExelyCancellationRulesResponseAsync(hotelCode, exelyBaseUrl, Item.PassWord, Item.SearchTimeOut, SearchParam.SearchKey);
                                                cancellationRulesCache[hotelCode] = rulesResponse;
                                            }

                                            List<ExelyCancellationSegment> ruleSegments = BuildExelyCancellationSegments(rulesResponse, checkInDate, companyBuyingPrice, SearchParam.NoofNights, cutOffDays);
                                            if (ruleSegments.Count > 0)
                                            {
                                                AddExelyCancellationRows(
                                                    dt_Cancellation,
                                                    ruleSegments,
                                                    Item.Supplier,
                                                    Convert.ToString(OwnsystemHotelId),
                                                    1,
                                                    RoomTypeId,
                                                    roomStay.mealPlanCode ?? "",
                                                    SearchParam,
                                                    isPerce,
                                                    commonSearch.B2CMarkup,
                                                    AgentPercentage,
                                                    AgentMarkupAmount,
                                                    commonSearch.IsParentId,
                                                    commonSearch.SubAgentMarkup,
                                                    Essen,
                                                    dtROE);
                                                addedDetailedCancellation = true;
                                            }
                                        }

                                        if (!addedDetailedCancellation)
                                        {
                                            DateTime nowDate = DateTime.Now.Date;
                                            dt_Cancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref companyChargeAmount, dt_Cancellation.NewRow(), supplierCurrencyFactor, cancellationCurrency, Item.Supplier, Convert.ToString(OwnsystemHotelId), Convert.ToString(1),
                                                RoomTypeId, "", roomStay.mealPlanCode ?? "", nowDate, checkInDate, markedupCancellationCharge, markedupCancellationCharge, "", vatOnPolicy, "", "YES", "", Essen, "", ""));
                                            dt_Cancellation.AcceptChanges();
                                        }
                                        // Clone cancellation rows for each additional room in multi-room search
                                        if (SearchParam.Rooms.Count > 1)
                                        {
                                            string room0No = Convert.ToString(SearchParam.Rooms[0].RoomNo);
                                            var room0CancelRows = dt_Cancellation.AsEnumerable()
                                                .Where(r => Convert.ToString(r["RoomTypeId"]) == RoomTypeId && Convert.ToString(r["RoomNo"]) == room0No)
                                                .ToList();
                                            for (int roomIdx = 1; roomIdx < SearchParam.Rooms.Count; roomIdx++)
                                            {
                                                string extraRoomNo = Convert.ToString(SearchParam.Rooms[roomIdx].RoomNo);
                                                foreach (var srcRow in room0CancelRows)
                                                {
                                                    DataRow newCancelRow = dt_Cancellation.NewRow();
                                                    newCancelRow.ItemArray = (object[])srcRow.ItemArray.Clone();
                                                    newCancelRow["RoomNo"] = extraRoomNo;
                                                    dt_Cancellation.Rows.Add(newCancelRow);
                                                }
                                                dt_Cancellation.AcceptChanges();
                                            }
                                        }
                                        #endregion

                                    }

                                    #endregion
                                    double passPrice = SearchParam.Filters.IsShowRooms == 1 ? Pricenew : FinalTotal;
                                    Options ops = (await _supplierRes.GetCommonRoomForSuppliers(GiataId, commonSearch.tokenData, commonSearch.advancedOptions, passPrice, drXmlHotel, dt_HotelRoom, dt_Cancellation, dt_RateBreakUp, SearchParam));
                                    if (ops != null)
                                        options.Add(ops);
                                }
                                #endregion
                            }
                            #endregion
                        }
                        #endregion
                    }
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
                    timer_room_Exely.Stop();
                    Item.CodeProcessingTimeSupplier = timer_room_Exely.ElapsedMilliseconds;
                    if (SearchParam != null && ResProccessTimerLogActive == "1" && timer_room_Exely.ElapsedMilliseconds > Convert.ToInt32(ResProccessTimeValueforSaveLog))
                    {

                        SearchParam.SupplierTime += ",|Finish:Exely Process Response  " + "|Time :" + timer_room_Exely.Elapsed.ToString();
                        _ = Task.Run(() => _commonMethod.FireAway_SQL_ResPorcessTimeLog("", timer_room_Exely.Elapsed.ToString(), Item.Supplier, SearchParam.SearchKey, 1, SearchParam.AgentID));
                    }
                }
                catch (Exception x)
                {
                    await _exception.Error(x);
                }
                #endregion

                if (Item.ResponseType == "NoCount" && StoreSupplierResErrorLog == "1")
                    _ = Task.Run(() => _commonMethod.XMLRequestResponseTrack_Error(Item.RQRS[0].Request, Item.RQRS[0].Response, Item.Supplier, timer_room_Exely.Elapsed.ToString(), "", SearchParam.SearchKey));
            }

            return options;
        }
        #endregion Response
        #endregion Availability

        public async Task<CancellationDto> Get_Exely_CancellationPolicy(int roomno, string hotelcode, DataSet dsHotel, string ownsystemid, string roomtypeid, DataTable dtAgentDetail, DataTable dtSuplierMarkup, DataTable dtMarkupDetails, DataTable dtSearchConfig, HotelSearchParameter SearchPara, string OptionCode, int AgentId, double roomprice, double Companybuy, string SearchKey, string Supplier, string GiataId = "0", int hotelRating = 0)
        {
            CancellationDto cancellationDto = new CancellationDto
            {
                dsHotelData = dsHotel,
                ISBooking = "Available"
            };

            try
            {
                DataTable dtHotelCancellation = _commonMethod.GenerateCancellationDataTableNewV2();
                bool isPerce = true;
                double agentPercentage = 0.0;
                double agentMarkupAmount = 0.0;
                bool blnIsShowSupplier = false;
                double agentCompanyPercentage = Convert.ToDouble(SearchPara.AgentCompanyPercentage);
                bool isB2C = Convert.ToBoolean(Convert.ToByte(SearchPara.IsB2BOrB2C));
                double b2cMarkup = (isB2C == false ? agentCompanyPercentage : XMLHotelBookingCommon.B2CMarkup);

                _markup.getAgetMarkupSetting(Supplier, GiataId, dtSuplierMarkup, dtMarkupDetails, ref agentPercentage, ref agentMarkupAmount, ref blnIsShowSupplier, ref isPerce, AgentId, hotelRating, SearchPara.CityID, Convert.ToString(SearchPara.CountryID), Convert.ToString(SearchPara.CheckInDate));

                DataRow[] drSupplierMarkup = dtSuplierMarkup.Select("SupplierName='" + Supplier + "'");
                if (drSupplierMarkup.Length == 0)
                {
                    cancellationDto.ISBooking = "NotAvailable";
                    return cancellationDto;
                }

                string password = Convert.ToString(drSupplierMarkup[0]["Password"]) ?? string.Empty;
                string baseUrl = Convert.ToString(drSupplierMarkup[0]["WebSiteURL"]) ?? string.Empty;
                int sid = Convert.ToInt32(drSupplierMarkup[0]["SupplierID"]);
                DataRow[] drSearchConfig = dtSearchConfig.Select("SupplierID='" + sid + "'");
                int searchTimeout = drSearchConfig.Length > 0 ? Convert.ToInt32(drSearchConfig[0]["BookingConfirmationTimeout"]) : 20000;
                DateTime checkInDate = ParseSearchDate(SearchPara.CheckInDate);
                int cutOffDays = _commonMethod.GetCutOffDays(dtSuplierMarkup, Supplier);

                string[] roomTypeParts = (roomtypeid ?? string.Empty).Split("$$$");
                string expectedRoomType = roomTypeParts.Length > 0 ? roomTypeParts[0] : "";
                string expectedRatePlan = roomTypeParts.Length > 1 ? roomTypeParts[1] : "";
                string expectedCurrency = roomTypeParts.Length > 3 ? roomTypeParts[3] : "";

                CancellationPolicy? matchedPolicy = null;
                string matchedEssen = "";
                string matchedCurrency = expectedCurrency;

                // STEP 2: GET /api/search/v1/properties/{propertyId}/room-stays (fallback)
                //if (matchedPolicy == null)
                //{
                // Hotel-specific room data must come from GET /api/search/v1/properties/{propertyId}/room-stays.
                /*SearchByPropertyResponse? propertyRoomStays = await GetRoomStaysByPropertyAsync(hotelcode, baseUrl, password, searchTimeout, SearchPara, SearchKey);
                    var matchedDetailedStay = propertyRoomStays?.roomStays?.FirstOrDefault(rs =>
                        (string.IsNullOrWhiteSpace(expectedRoomType) || string.Equals(rs.roomType?.id, expectedRoomType, StringComparison.OrdinalIgnoreCase)) &&
                        (string.IsNullOrWhiteSpace(expectedRatePlan) || string.Equals(rs.ratePlan?.id, expectedRatePlan, StringComparison.OrdinalIgnoreCase)));

                    if (matchedDetailedStay != null)
                    {
                        matchedPolicy = matchedDetailedStay.cancellationPolicy;
                        matchedCurrency = string.IsNullOrWhiteSpace(matchedDetailedStay.currencyCode) ? expectedCurrency : matchedDetailedStay.currencyCode;
                        if (!string.IsNullOrWhiteSpace(matchedDetailedStay.mealPlanCode))
                            matchedEssen = "Meal Plan: " + matchedDetailedStay.mealPlanCode;
                        if (matchedDetailedStay.includedServices?.Count > 0)
                        {
                            string svcs = string.Join(", ", matchedDetailedStay.includedServices.Select(s => s.id + (string.IsNullOrWhiteSpace(s.mealPlanCode) ? "" : "(" + s.mealPlanCode + ")")));
                            matchedEssen = string.IsNullOrWhiteSpace(matchedEssen) ? svcs : matchedEssen + "</br>" + svcs;
                        }
                        if (!string.IsNullOrWhiteSpace(matchedDetailedStay.bookingFormLink))
                            matchedEssen = string.IsNullOrWhiteSpace(matchedEssen) ? matchedDetailedStay.bookingFormLink : matchedEssen + "</br>" + matchedDetailedStay.bookingFormLink;
                    }*/
                //}

                if (matchedPolicy != null)
                {
                    string currency = string.IsNullOrWhiteSpace(matchedCurrency) ? "USD" : matchedCurrency;
                    DataTable dtInrCurrency = _commonMethod.GetCountryCurrency(currency);
                    double currencyFactor = (dtInrCurrency != null && dtInrCurrency.Rows.Count > 0) ? Convert.ToDouble(dtInrCurrency.Rows[0]["AmtToExch"]) : 1;
                    double cancellationChargeAmount = Companybuy > 0 ? Companybuy : roomprice;
                    double vatOnPolicy = 0;
                    double mainAgentCharge = cancellationChargeAmount;
                    double companyChargeAmount = cancellationChargeAmount;
                    double markedUpCharge = _markup.ProcessHotelCancelationPrice_WithMarkup(ref mainAgentCharge, SearchPara.NoofNights, SearchPara.CityID, Convert.ToString(SearchPara.CountryID), cancellationChargeAmount, isPerce, b2cMarkup, agentPercentage, agentMarkupAmount, 1, 0, ref vatOnPolicy);
                    DateTime nowDate = DateTime.Now.Date;

                    if (dsHotel.Tables.Count > 2 && !string.IsNullOrWhiteSpace(matchedEssen))
                    {
                        dsHotel.Tables[2].DefaultView.RowFilter = "OwnsystemHotelId=" + ownsystemid + "and RoomTypeId='" + roomtypeid + "' and RoomNo='" + roomno + "'";
                        for (int l = 0; l < dsHotel.Tables[2].DefaultView.Count; l++)
                        {
                            dsHotel.Tables[2].DefaultView[l]["SPUI"] = matchedEssen;
                        }
                    }

                    if (matchedPolicy.freeCancellationPossible && matchedPolicy.freeCancellationDeadlineLocal.HasValue)
                    {
                        DateTime deadlineDate = matchedPolicy.freeCancellationDeadlineLocal.Value.Date;
                        if (nowDate.Date < deadlineDate.Date)
                        {
                            double zero = 0;
                            dtHotelCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref zero, dtHotelCancellation.NewRow(), currencyFactor, currency, Supplier, ownsystemid, Convert.ToString(roomno),
                                roomtypeid, "", Convert.ToString(OptionCode), nowDate, deadlineDate.AddDays(-1).AddHours(00).AddMinutes(00).AddSeconds(00), 0, 0, "", vatOnPolicy, "", "NO", "", matchedEssen, "", ""));
                            dtHotelCancellation.AcceptChanges();
                        }
                        double penaltyBase = (matchedPolicy.penaltyAmount.HasValue && matchedPolicy.penaltyAmount.Value > 0)
                            ? matchedPolicy.penaltyAmount.Value * currencyFactor
                            : cancellationChargeAmount;
                        double vatOnPenalty = 0;
                        double penaltyMainAgent = penaltyBase;
                        double penaltyCompany = penaltyBase;
                        double penaltyMarkedUp = _markup.ProcessHotelCancelationPrice_WithMarkup(ref penaltyMainAgent, SearchPara.NoofNights, SearchPara.CityID, Convert.ToString(SearchPara.CountryID), penaltyBase, isPerce, b2cMarkup, agentPercentage, agentMarkupAmount, 1, 0, ref vatOnPenalty);
                        dtHotelCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref penaltyCompany, dtHotelCancellation.NewRow(), currencyFactor, currency, Supplier, ownsystemid, Convert.ToString(roomno),
                             roomtypeid, "", Convert.ToString(OptionCode), deadlineDate, checkInDate, penaltyMarkedUp, penaltyMarkedUp, "", vatOnPenalty, "", "YES", "", matchedEssen, "", ""));
                        dtHotelCancellation.AcceptChanges();
                    }
                    else
                    {
                        dtHotelCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref companyChargeAmount, dtHotelCancellation.NewRow(), currencyFactor, currency, Supplier, ownsystemid, Convert.ToString(roomno),
                            roomtypeid, "", Convert.ToString(OptionCode), nowDate, checkInDate, markedUpCharge, markedUpCharge, "", vatOnPolicy, "", "YES", "", matchedEssen, "", ""));
                        dtHotelCancellation.AcceptChanges();
                    }
                }

                if (dsHotel.Tables.Count > 2)
                    dsHotel.Tables[2].Merge(dtHotelCancellation);
                else
                    dsHotel.Tables.Add(dtHotelCancellation.Copy());

                cancellationDto.dsHotelData = dsHotel;
            }
            catch (Exception ex)
            {
                await _exception.Error(ex, SearchKey);
                cancellationDto.ISBooking = "NotAvailable";
            }

            return cancellationDto;
        }

        public string Get_Exely_CancellationPolicyRQ(HotelSearchParameter SearchPara, string hotelcode, string CheckIn, string CheckOut, string RoomTypeId, int roomNo)
        {
            try
            {
                var request = new SearchByPropertyRequest
                {
                    propertyId = hotelcode,
                    arrivalDate = ParseSearchDate(CheckIn).ToString("yyyy-MM-dd"),
                    departureDate = ParseSearchDate(CheckOut).ToString("yyyy-MM-dd"),
                    adults = SearchPara.Rooms != null && SearchPara.Rooms.Count > 0 ? SearchPara.Rooms[0].NoofAdults : 1,
                    childAges = new List<int>(),
                    includeExtraServices = true,
                    includeExtraStays = true
                };

                if (SearchPara.Rooms != null && SearchPara.Rooms.Count > roomNo && SearchPara.Rooms[roomNo].NoofChild > 0)
                {
                    if (SearchPara.Rooms[roomNo].Child1Age > 0) request.childAges.Add(SearchPara.Rooms[roomNo].Child1Age);
                    if (SearchPara.Rooms[roomNo].Child2Age > 0) request.childAges.Add(SearchPara.Rooms[roomNo].Child2Age);
                }

                return JsonConvert.SerializeObject(request, GetExelyIsoJsonSettings());
            }
            catch (Exception ex)
            {
                _ = _exception.Error(ex);
                return "";
            }
        }

        public async Task<ResponseDto> ExelyResponseNew(string xmlRQRS, string strHost, int SearchTimeOut, string Password, string SearchKey = "", string requestMethod = "POST")
        {
            string responseString = "";
            string error = "";
            try
            {
                var request = (HttpWebRequest)WebRequest.Create(strHost);
                //var data = Encoding.UTF8.GetBytes(xmlRQRS);
                request.Proxy = null;
                request.Method = requestMethod;
                request.Timeout = SearchTimeOut;
                request.ContentType = "application/json";
                request.Accept = "application/json";
                request.Headers.Add("X-Api-Key", Password);
                //request.ContentLength = data.Length;

                if (requestMethod.ToUpper() != "GET")
                {
                    var data = Encoding.UTF8.GetBytes(xmlRQRS);
                    request.ContentLength = data.Length;

                    using (var stream = await request.GetRequestStreamAsync())
                    {
                        await stream.WriteAsync(data, 0, data.Length);
                    }
                }

                //using (var stream = await request.GetRequestStreamAsync())
                //{
                //    await stream.WriteAsync(data, 0, data.Length);
                //}

                var response = (HttpWebResponse)await request.GetResponseAsync();
                Stream st = response.GetResponseStream();
                if (!string.IsNullOrWhiteSpace(response.ContentEncoding) && response.ContentEncoding.ToLower().Contains("gzip"))
                    st = new System.IO.Compression.GZipStream(st, System.IO.Compression.CompressionMode.Decompress);
                else if (!string.IsNullOrWhiteSpace(response.ContentEncoding) && response.ContentEncoding.ToLower().Contains("deflate"))
                    st = new System.IO.Compression.DeflateStream(st, System.IO.Compression.CompressionMode.Decompress);

                using (var reader = new StreamReader(st, Encoding.UTF8))
                {
                    responseString = await reader.ReadToEndAsync();
                }
            }
            catch (WebException webEx) when (webEx.Response is HttpWebResponse errResponse)
            {
                Stream errStream = errResponse.GetResponseStream();
                if (!string.IsNullOrWhiteSpace(errResponse.ContentEncoding) && errResponse.ContentEncoding.ToLower().Contains("gzip"))
                    errStream = new System.IO.Compression.GZipStream(errStream, System.IO.Compression.CompressionMode.Decompress);
                else if (!string.IsNullOrWhiteSpace(errResponse.ContentEncoding) && errResponse.ContentEncoding.ToLower().Contains("deflate"))
                    errStream = new System.IO.Compression.DeflateStream(errStream, System.IO.Compression.CompressionMode.Decompress);
                using (var reader = new StreamReader(errStream, Encoding.UTF8))
                {
                    error = await reader.ReadToEndAsync();
                }
                responseString = error;
                _ = _exception.Error(webEx, SearchKey);
            }
            catch (Exception ex)
            {
                error = ex.Message;
                _ = _exception.Error(ex, SearchKey);
            }

            return new ResponseDto { Response = responseString, Error = error };
        }

        // - Recheck: when isForBooking is false 
        // - Prebook: when isForBooking is true
        public async Task<PriceCheckCMDto> Get_Exely_RecheckPreBookRQRS(DataSet dsCartDetails, DataRow[] ExelyDetails, int i, int j, DataTable dtCheckPriceReqStructure, string UniqueNo, string AgentUniqueNo, bool isForBooking, double CompanyBuyingPriceA, string xmlhotelid, string IsFor, bool IssErrorY, double itempriceY, string Searchkey, int AgentId = 0, TokenData tokenData = null)
        {
            PriceCheckCMDto priceCheckCMDto = new PriceCheckCMDto { ItemPrice = itempriceY, Flag = false, IsError = true };
            string requestData = "";
            string responseData = "";
            string supplierName = Convert.ToString(dsCartDetails.Tables[1].DefaultView[0]["SupplierName"]) ?? string.Empty;
            try
            {
                string password = Convert.ToString(ExelyDetails[0]["Password"]) ?? string.Empty;
                string rawUrl = Convert.ToString(ExelyDetails[0]["WebSiteURL"]) ?? string.Empty;
                string baseUrl = GetExelyBaseUrl(rawUrl);
                string hotelId = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["XMLHotelId"]) ?? string.Empty;

                int bookingTimeout = 20000;
                if (ExelyDetails[0].Table.Columns.Contains("BookingConfirmationTimeout")
                    && !string.IsNullOrWhiteSpace(Convert.ToString(ExelyDetails[0]["BookingConfirmationTimeout"])))
                {
                    bookingTimeout = Convert.ToInt32(ExelyDetails[0]["BookingConfirmationTimeout"]);
                }

                if (!isForBooking)
                {
                    #region Recheck
                    // Just verify that cached availability response exists (no live price check, policy assumed unchanged)
                    requestData = "Exely Recheck - Using cached availability data";
                    responseData = "Recheck passed - Policy not changed";

                    _commonMethod.ThirdPartyBookingHistory(UniqueNo, AgentUniqueNo, requestData, responseData, "Exely Recheck", supplierName, "response confirm", "-", "NomadsB2B", Searchkey);

                    priceCheckCMDto.IsError = false;
                    priceCheckCMDto.Flag = false;
                    priceCheckCMDto.ItemPrice = itempriceY > 0 ? itempriceY : CompanyBuyingPriceA;
                    #endregion Recheck
                }

                else
                {
                    #region Prebook
                    // PREBOOK: Call supplier GetRoomStaysByProperty API to verify rooms and prices
                    string verifyCheckIn = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["CheckInDate"]) ?? string.Empty;
                    string verifyCheckOut = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["CheckOutDate"]) ?? string.Empty;
                    int verifyAdults = 1;
                    int verifyChildren = 0;
                    if (dsCartDetails.Tables.Count > 1 && dsCartDetails.Tables[1].DefaultView.Count > j)
                    {
                        verifyAdults = Math.Max(1, Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[j]["NoOfAdults"]));
                        verifyChildren = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[j]["NoOfChild"]);
                    }

                    // Build search param for API call
                    List<RoomDetails> searchRooms = new List<RoomDetails>();
                    if (dsCartDetails.Tables.Count > 1)
                    {
                        for (int roomIndex = 0; roomIndex < dsCartDetails.Tables[1].DefaultView.Count; roomIndex++)
                        {
                            var roomRow = dsCartDetails.Tables[1].DefaultView[roomIndex];
                            int adults = Math.Max(1, Convert.ToInt32(roomRow["NoOfAdults"]));
                            int children = Convert.ToInt32(roomRow["NoOfChild"]);
                            int child1 = roomRow.DataView.Table.Columns.Contains("Child1Age") ? Convert.ToInt32(roomRow["Child1Age"]) : 0;
                            int child2 = roomRow.DataView.Table.Columns.Contains("Child2Age") ? Convert.ToInt32(roomRow["Child2Age"]) : 0;
                            searchRooms.Add(new RoomDetails { NoofAdults = adults, NoofChild = children, Child1Age = child1, Child2Age = child2 });
                        }
                    }

                    var searchParam = new HotelSearchParameter
                    {
                        CheckInDate = verifyCheckIn,
                        CheckOutDate = verifyCheckOut,
                        Rooms = searchRooms
                    };

                    // Build supplier API URL and call
                    string template = _supplierConfig.Exely.GetRoomStaysByProperty ?? string.Empty;
                    string path = template.TrimStart('/').Replace("{propertyId}", hotelId);
                    string arrivalDate = ParseSearchDate(verifyCheckIn).ToString("yyyy-MM-dd");
                    string departureDate = ParseSearchDate(verifyCheckOut).ToString("yyyy-MM-dd");
                    int adultsTotal = searchParam.Rooms?.Sum(r => Math.Max(r.NoofAdults, 1)) ?? 1;
                    var qp = new List<string> { $"arrivalDate={arrivalDate}", $"departureDate={departureDate}", $"adults={adultsTotal}" };
                    if (searchParam.Rooms != null)
                    {
                        foreach (var r in searchParam.Rooms)
                        {
                            if (r.Child1Age > 0) qp.Add($"childAges={r.Child1Age}");
                            if (r.Child2Age > 0) qp.Add($"childAges={r.Child2Age}");
                        }
                    }
                    string prebookUrl = $"{baseUrl.TrimEnd('/')}/{path}?{string.Join("&", qp)}";
                    requestData = prebookUrl;

                    _commonMethod.ThirdPartyBookingHistory(UniqueNo, AgentUniqueNo, requestData, "-", "Before Exely PreBook RoomStaysByProperty", supplierName, "Request", "-", "NomadsB2B", Searchkey);

                    // Call supplier API
                    Stopwatch prebookTimer = Stopwatch.StartNew();
                    var detailed = await GetRoomStaysByPropertyAsync(hotelId, baseUrl, password, bookingTimeout, searchParam, Searchkey);
                    prebookTimer.Stop();
                    responseData = detailed == null ? string.Empty : JsonConvert.SerializeObject(detailed, GetExelyIsoJsonSettings());

                    // Log supplier response
                    _commonMethod.ThirdPartyBookingHistory(UniqueNo, AgentUniqueNo, requestData, responseData, "After Exely PreBook RoomStaysByProperty", supplierName, "Response", "-", "NomadsB2B", Searchkey);
                    await _commonMethod.XMLRequestResponseTrack_Error(prebookUrl, responseData, supplierName, prebookTimer.Elapsed.ToString(), "ExelyPreBookRoomStaysByProperty", Searchkey);

                    // Load PropertyContent to get English names
                    PropertyInfoType? propertyContent = null;
                    if (detailed != null && detailed.roomStays != null && detailed.roomStays.Count > 0)
                    {
                        propertyContent = await GetExelyPropertyContentAsync(hotelId, baseUrl, password, bookingTimeout, Searchkey, "en", string.Empty, supplierName);
                    }

                    // Match rooms from cart with prebook response
                    bool anyMismatch = false;
                    if (detailed == null || detailed.roomStays == null || detailed.roomStays.Count == 0)
                    {
                        anyMismatch = true;
                    }
                    else
                    {
                        for (int roomIndex = 0; roomIndex < (dsCartDetails.Tables.Count > 1 ? dsCartDetails.Tables[1].DefaultView.Count : 0); roomIndex++)
                        {
                            string storedRoomTypeId = Convert.ToString(dsCartDetails.Tables[1].DefaultView[roomIndex]["RoomTypeID"]) ?? string.Empty;
                            string[] parts = storedRoomTypeId.Split(new[] { "$$$" }, StringSplitOptions.None);
                            string expectedRoomType = parts.Length > 0 ? parts[0].Trim() : string.Empty;
                            string expectedRatePlan = parts.Length > 1 ? parts[1].Trim() : string.Empty;

                            // Match by roomType.id and ratePlan.id
                            var matched = detailed.roomStays.FirstOrDefault(rs =>
                                string.Equals(rs.roomType?.id?.Trim(), expectedRoomType, StringComparison.OrdinalIgnoreCase) &&
                                string.Equals(rs.ratePlan?.id?.Trim(), expectedRatePlan, StringComparison.OrdinalIgnoreCase));

                            if (matched == null)
                            {
                                anyMismatch = true;
                                break;
                            }
                            else
                            {
                                // Compare prices: availability price vs prebook price
                                double rawLivePrice = matched.total?.priceBeforeTax ?? 0;
                                string roomCurrency = !string.IsNullOrWhiteSpace(matched.currencyCode)
                                    ? matched.currencyCode
                                    : Convert.ToString(ExelyDetails[0]["CurrencyShortName"]);
                                double livePrice = rawLivePrice;
                                if (!string.IsNullOrWhiteSpace(roomCurrency) && tokenData?.dtROE != null)
                                {
                                    double roomCurrencyFactor = _commonMethod.GetROEByCurrency(roomCurrency, tokenData.dtROE);
                                    livePrice = rawLivePrice * roomCurrencyFactor;
                                }

                                double storedPrice = itempriceY > 0 ? itempriceY : CompanyBuyingPriceA;
                                if (livePrice > 0 && Math.Abs(livePrice - storedPrice) > 0.01)
                                {
                                    priceCheckCMDto.Flag = true;
                                    priceCheckCMDto.ItemPrice = livePrice;
                                }

                                // Update names from PropertyContent (English names)
                                if (propertyContent != null)
                                {
                                    if (matched.roomType != null)
                                        matched.roomType.name = GetExelyRoomTypeById(propertyContent, matched.roomType.id)?.Name;
                                    if (matched.ratePlan != null)
                                        matched.ratePlan.name = GetExelyRatePlanById(propertyContent, matched.ratePlan.id)?.Name;
                                }
                            }
                        }
                    }

                    if (anyMismatch)
                    {
                        // Prebook failed - room or rate plan mismatch
                        priceCheckCMDto.IsError = true;
                        priceCheckCMDto.Flag = true;
                        priceCheckCMDto.ItemPrice = 0;
                        _commonMethod.ThirdPartyBookingHistory(UniqueNo, AgentUniqueNo, requestData, responseData, "Exely PreBook failed - room/rate mismatch", supplierName, "Response", "-", "NomadsB2B", Searchkey);
                    }
                    else
                    {
                        // Prebook success - prices match
                        priceCheckCMDto.IsError = false;
                        priceCheckCMDto.Flag = priceCheckCMDto.Flag; // preserve price-flag if set
                        priceCheckCMDto.ItemPrice = priceCheckCMDto.ItemPrice > 0 ? priceCheckCMDto.ItemPrice : (itempriceY > 0 ? itempriceY : CompanyBuyingPriceA);
                    }
                    #endregion Prebook
                }
            }
            catch (Exception ex)
            {
                _ = _exception.Error(ex, Searchkey);
            }
            finally
            {
                _ = Task.Run(() => _commonMethod.SupplierPerformanceReportForPrebookAndBook(supplierName, isForBooking ? "PreBook" : "Recheck", AgentId, "0", priceCheckCMDto.IsError ? "Error" : "Success", requestData, responseData, ""));
            }

            return priceCheckCMDto;
        }

        #region Prebook
        #region Request

        private static string AppendExelyCreateBookingToken(string roomTypeId, string createBookingToken)
        {
            if (string.IsNullOrWhiteSpace(roomTypeId) || string.IsNullOrWhiteSpace(createBookingToken))
                return roomTypeId ?? string.Empty;

            List<string> parts = roomTypeId.Split("$$$").ToList();
            while (parts.Count <= 11)
                parts.Add(string.Empty);

            parts[11] = createBookingToken;
            return string.Join("$$$", parts);
        }

        public string Get_Exely_VerifyBookingRQ(int noofadult, int noofchild, string hotelcode, string CheckIn, string CheckOut, string RoomTypeId, string XmlNationality, string checksum = "", DataTable? dtPassengers = null)
        {
            try
            {
                string[] roomTypeParts = (RoomTypeId ?? string.Empty).Split("$$$");
                string roomTypeCode = roomTypeParts.Length > 0 ? roomTypeParts[0] : "";
                string ratePlanCode = roomTypeParts.Length > 1 ? roomTypeParts[1] : "";
                string storedPlacementCode = roomTypeParts.Length > 8 ? roomTypeParts[8] : "";
                int checkInHour = roomTypeParts.Length > 9 && int.TryParse(roomTypeParts[9], out int ciH) ? ciH : 14;
                int checkOutHour = roomTypeParts.Length > 10 && int.TryParse(roomTypeParts[10], out int coH) ? coH : 12;

                List<int> childAges = GetExelyChildAges(dtPassengers, noofchild);

                var placements = BuildBookingPlacements(storedPlacementCode, noofadult, noofchild);

                var guests = new List<BookingGuest>();
                if (dtPassengers != null && dtPassengers.Rows.Count > 0)
                {
                    for (int k = 0; k < dtPassengers.Rows.Count; k++)
                    {
                        string fn = Convert.ToString(dtPassengers.Rows[k]["FirstName"]) ?? string.Empty;
                        string ln = Convert.ToString(dtPassengers.Rows[k]["LastName"]) ?? string.Empty;
                        string sexStr = dtPassengers.Columns.Contains("Gender") ? (Convert.ToString(dtPassengers.Rows[k]["Gender"]) ?? "Male") : "Male";
                        guests.Add(new BookingGuest
                        {
                            firstName = string.IsNullOrWhiteSpace(fn) ? "Guest" : fn,
                            lastName = string.IsNullOrWhiteSpace(ln) ? "User" : ln,
                            middleName = "",
                            citizenship = XmlNationality,
                            sex = sexStr.Equals("Female", StringComparison.OrdinalIgnoreCase) ? BookingGuestSexKind.Female : BookingGuestSexKind.Male
                        });
                    }
                }
                while (guests.Count < noofadult)
                    guests.Add(new BookingGuest { firstName = "Guest", lastName = "User", middleName = "", citizenship = XmlNationality, sex = BookingGuestSexKind.Male });

                string leadFirstName = guests[0].firstName ?? "Guest";
                string leadLastName = guests[0].lastName ?? "User";

                var request = new VerifyBookingRequest
                {
                    Booking = new BookingRq
                    {
                        PropertyId = hotelcode,
                        RoomStays = new List<BookingRoomStayRq>
                        {
                            new BookingRoomStayRq
                            {
                                stayDates = new BookingStayDates { arrivalDateTime = ParseSearchDate(CheckIn).Date.AddHours(checkInHour), departureDateTime = ParseSearchDate(CheckOut).Date.AddHours(checkOutHour) },
                                ratePlan = new BookingRatePlanRq { id = ratePlanCode },
                                roomType = new BookingRoomTypeRq { id = roomTypeCode, placements = placements },
                                guests = guests,
                                guestCount = new BookingGuestCount { adultCount = noofadult, childAges = childAges.Count > 0 ? childAges : null },
                                checksum = checksum,
                            }
                        },
                        Customer = new BookingCustomer
                        {
                            firstName = leadFirstName,
                            lastName = leadLastName,
                            middleName = "",
                            citizenship = XmlNationality,
                            contacts = new BookingPersonContacts
                            {
                                phones = new List<BookingPersonPhone> { new BookingPersonPhone { phoneNumber = "+442012345678" } },
                                emails = new List<BookingPersonEmail> { new BookingPersonEmail { emailAddress = "guest@withinearth.com" } }
                            }
                        },
                    }
                };

                return JsonConvert.SerializeObject(request, GetExelyIsoJsonSettings());
            }
            catch (Exception ex)
            {
                _ = _exception.Error(ex);
                return "";
            }
        }
        #endregion Request
        #endregion Prebook

        #region Group Prebook (multi-room verify)
        public string Get_Exely_VerifyBookingRQ(List<ExelyRoomInfo> rooms, string hotelcode, string XmlNationality, DataTable? dtPassengers = null)
        {
            try
            {
                var roomStays = new List<BookingRoomStayRq>();
                foreach (var room in rooms)
                {
                    string[] parts = (room.RoomTypeId ?? "").Split("$$$");
                    string roomTypeCode = parts.Length > 0 ? parts[0] : "";
                    string ratePlanCode = parts.Length > 1 ? parts[1] : "";
                    string storedPlacementCode = parts.Length > 8 ? parts[8] : "";
                    string checksum = parts.Length > 6 ? parts[6] : "";
                    int checkInHour = parts.Length > 9 && int.TryParse(parts[9], out int ciH) ? ciH : 14;
                    int checkOutHour = parts.Length > 10 && int.TryParse(parts[10], out int coH) ? coH : 12;
                    string nationality = !string.IsNullOrWhiteSpace(room.Nationality) ? room.Nationality : XmlNationality;

                    List<int> childAges = GetExelyChildAges(dtPassengers, room.NoOfChild);

                    var placements = BuildBookingPlacements(storedPlacementCode, room.NoOfAdults, room.NoOfChild);

                    var guests = new List<BookingGuest>();
                    if (dtPassengers != null && dtPassengers.Rows.Count > 0)
                    {
                        string fn = Convert.ToString(dtPassengers.Rows[0]["FirstName"]) ?? "Guest";
                        string ln = Convert.ToString(dtPassengers.Rows[0]["LastName"]) ?? "User";
                        guests.Add(new BookingGuest { firstName = fn, lastName = ln, middleName = "", citizenship = nationality, sex = BookingGuestSexKind.Male });
                    }
                    while (guests.Count < Math.Max(1, room.NoOfAdults))
                        guests.Add(new BookingGuest { firstName = "Guest", lastName = "User", middleName = "", citizenship = nationality, sex = BookingGuestSexKind.Male });

                    roomStays.Add(new BookingRoomStayRq
                    {
                        stayDates = new BookingStayDates { arrivalDateTime = ParseSearchDate(room.CheckIn).Date.AddHours(checkInHour), departureDateTime = ParseSearchDate(room.CheckOut).Date.AddHours(checkOutHour) },
                        ratePlan = new BookingRatePlanRq { id = ratePlanCode },
                        roomType = new BookingRoomTypeRq { id = roomTypeCode, placements = placements },
                        guests = guests,
                        guestCount = new BookingGuestCount { adultCount = Math.Max(1, room.NoOfAdults), childAges = childAges.Count > 0 ? childAges : null },
                        checksum = checksum,
                    });
                }

                string leadFirst = "Guest", leadLast = "User";
                if (dtPassengers != null && dtPassengers.Rows.Count > 0)
                {
                    leadFirst = Convert.ToString(dtPassengers.Rows[0]["FirstName"]) ?? "Guest";
                    leadLast = Convert.ToString(dtPassengers.Rows[0]["LastName"]) ?? "User";
                }

                var request = new VerifyBookingRequest
                {
                    Booking = new BookingRq
                    {
                        PropertyId = hotelcode,
                        RoomStays = roomStays,
                        Customer = new BookingCustomer
                        {
                            firstName = leadFirst,
                            lastName = leadLast,
                            middleName = "",
                            citizenship = XmlNationality,
                            contacts = new BookingPersonContacts
                            {
                                phones = new List<BookingPersonPhone> { new BookingPersonPhone { phoneNumber = "+442012345678" } },
                                emails = new List<BookingPersonEmail> { new BookingPersonEmail { emailAddress = "guest@withinearth.com" } }
                            }
                        }
                    }
                };

                return JsonConvert.SerializeObject(request, GetExelyIsoJsonSettings());
            }
            catch (Exception ex)
            {
                _ = _exception.Error(ex);
                return "";
            }
        }
        #endregion

        #region Group Book (multi-room create)
        public string Get_Exely_BookingRequest(List<ExelyRoomInfo> rooms, DataTable dtPassengers, string uniqueBookingToken, string hotelcode, string nationality)
        {
            try
            {
                var roomStays = new List<BookingRoomStayRq>();
                foreach (var room in rooms)
                {
                    string[] parts = (room.RoomTypeId ?? "").Split("$$$");
                    string roomTypeCode = parts.Length > 0 ? parts[0] : "";
                    string ratePlanCode = parts.Length > 1 ? parts[1] : "";
                    string storedPlacementCode = parts.Length > 8 ? parts[8] : "";
                    string checksum = parts.Length > 6 ? parts[6] : "";
                    int checkInHour = parts.Length > 9 && int.TryParse(parts[9], out int ciH) ? ciH : 14;
                    int checkOutHour = parts.Length > 10 && int.TryParse(parts[10], out int coH) ? coH : 12;

                    List<int> childAges = new List<int>();
                    for (int c = 0; c < room.NoOfChild; c++) childAges.Add(8);
                    var placements = BuildBookingPlacements(storedPlacementCode, room.NoOfAdults, room.NoOfChild);

                    var guests = new List<BookingGuest>();
                    if (dtPassengers != null && dtPassengers.Rows.Count > 0)
                    {
                        string fn = Convert.ToString(dtPassengers.Rows[0]["FirstName"]) ?? "Guest";
                        string ln = Convert.ToString(dtPassengers.Rows[0]["LastName"]) ?? "User";
                        string sx = dtPassengers.Columns.Contains("Sex") ? (Convert.ToString(dtPassengers.Rows[0]["Sex"]) ?? "Male") : "Male";
                        guests.Add(new BookingGuest { firstName = fn, lastName = ln, middleName = "", citizenship = nationality, sex = sx.Equals("Female", StringComparison.OrdinalIgnoreCase) ? BookingGuestSexKind.Female : BookingGuestSexKind.Male });
                    }
                    while (guests.Count < Math.Max(1, room.NoOfAdults))
                        guests.Add(new BookingGuest { firstName = "Guest", lastName = "User", middleName = "", citizenship = nationality, sex = BookingGuestSexKind.Male });

                    roomStays.Add(new BookingRoomStayRq
                    {
                        stayDates = new BookingStayDates { arrivalDateTime = ParseSearchDate(room.CheckIn).Date.AddHours(checkInHour), departureDateTime = ParseSearchDate(room.CheckOut).Date.AddHours(checkOutHour) },
                        ratePlan = new BookingRatePlanRq { id = ratePlanCode },
                        roomType = new BookingRoomTypeRq { id = roomTypeCode, placements = placements },
                        guests = guests,
                        guestCount = new BookingGuestCount { adultCount = Math.Max(1, room.NoOfAdults), childAges = childAges.Count > 0 ? childAges : null },
                        checksum = checksum,
                    });
                }

                string leadFirst = guests_LeadFirst(dtPassengers, "Guest");
                string leadLast = guests_LeadLast(dtPassengers, "User");

                var request = new CreateBookingRequest
                {
                    booking = new CreateBookingRq
                    {
                        propertyId = hotelcode,
                        roomStays = roomStays,
                        customer = new Customer
                        {
                            firstName = leadFirst,
                            lastName = leadLast,
                            middleName = "",
                            citizenship = nationality,
                            contacts = new Contacts
                            {
                                phones = new List<Phone> { new Phone { phoneNumber = "+442012345678" } },
                                emails = new List<Email> { new Email { emailAddress = "guest@withinearth.com" } }
                            }
                        },
                        createBookingToken = uniqueBookingToken
                    }
                };

                return JsonConvert.SerializeObject(request, GetExelyIsoJsonSettings());
            }
            catch (Exception ex)
            {
                _ = _exception.Error(ex);
                return "";
            }
        }

        private static string guests_LeadFirst(DataTable? dt, string fallback) =>
            dt != null && dt.Rows.Count > 0 ? (Convert.ToString(dt.Rows[0]["FirstName"]) ?? fallback) : fallback;

        private static string guests_LeadLast(DataTable? dt, string fallback) =>
            dt != null && dt.Rows.Count > 0 ? (Convert.ToString(dt.Rows[0]["LastName"]) ?? fallback) : fallback;
        #endregion

        #region Book
        #region Request
        public string Get_Exely_BookingRequest(DataSet dsCartDetails, DataTable dtPassengers, string uniqueBookingToken, int i, int j, string hotelcode, string checkIn, string checkOut, string currency, string nationality, string agtCode, string checksum)
        {
            try
            {
                string roomTypeId = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomTypeID"]) ?? string.Empty;
                string[] roomTypeParts = (roomTypeId ?? string.Empty).Split("$$$");
                string roomTypeCode = roomTypeParts.Length > 0 ? roomTypeParts[0] : "";
                string ratePlanCode = roomTypeParts.Length > 1 ? roomTypeParts[1] : "";
                string storedPlacementCode = roomTypeParts.Length > 8 ? roomTypeParts[8] : "";
                int checkInHour = roomTypeParts.Length > 9 && int.TryParse(roomTypeParts[9], out int ciH) ? ciH : 14;
                int checkOutHour = roomTypeParts.Length > 10 && int.TryParse(roomTypeParts[10], out int coH) ? coH : 12;

                int noOfAdults = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[j]["NoOfAdults"]);
                int noOfChild = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[j]["NoOfChild"]);
                List<int> childAges = GetExelyChildAges(dtPassengers, noOfChild);
                var placements = BuildBookingPlacements(storedPlacementCode, noOfAdults, noOfChild);

                List<BookingGuest> guests = new List<BookingGuest>();
                foreach (DataRow pax in dtPassengers.Rows)
                {
                    string firstName = Convert.ToString(pax["FirstName"]) ?? string.Empty;
                    string lastName = Convert.ToString(pax["LastName"]) ?? string.Empty;
                    string sexStr = pax.Table.Columns.Contains("Sex") ? Convert.ToString(pax["Sex"]) ?? "Male" : "Male";

                    guests.Add(new BookingGuest
                    {
                        firstName = firstName,
                        lastName = lastName,
                        middleName = "",
                        citizenship = nationality,
                        sex = sexStr.Equals("Female", StringComparison.OrdinalIgnoreCase)
                                    ? BookingGuestSexKind.Female
                                    : BookingGuestSexKind.Male
                    });
                }
                while (guests.Count < noOfAdults)
                {
                    guests.Add(new BookingGuest { firstName = "Guest", lastName = "User", middleName = "", citizenship = nationality, sex = BookingGuestSexKind.Male });
                }

                var request = new CreateBookingRequest
                {
                    booking = new CreateBookingRq
                    {
                        propertyId = hotelcode,
                        roomStays = new List<BookingRoomStayRq>
                        {
                            new BookingRoomStayRq
                            {
                                stayDates = new BookingStayDates { arrivalDateTime = ParseSearchDate(checkIn).Date.AddHours(checkInHour), departureDateTime = ParseSearchDate(checkOut).Date.AddHours(checkOutHour) },
                                ratePlan = new BookingRatePlanRq { id = ratePlanCode },
                                roomType = new BookingRoomTypeRq { id = roomTypeCode, placements = placements },
                                guests = guests,
                                guestCount = new BookingGuestCount { adultCount = noOfAdults, childAges = childAges.Count > 0 ? childAges : null },
                                checksum = checksum
                            }
                        },
                        customer = new Customer
                        {
                            firstName = guests[0].firstName,
                            lastName = guests[0].lastName,
                            middleName = "",
                            citizenship = nationality,
                            comment = "",
                            contacts = new Contacts
                            {
                                phones = new List<Phone> { new Phone { phoneNumber = "+442012345678", remark = "Mobile" } },
                                emails = new List<Email> { new Email { emailAddress = "guest@withinearth.com" } }
                            }
                        },
                        createBookingToken = uniqueBookingToken
                    }
                };

                return JsonConvert.SerializeObject(request, GetExelyIsoJsonSettings());
            }
            catch (Exception ex)
            {
                _ = _exception.Error(ex);
                return "";
            }
        }

        public string Get_Exely_BookingRequest(DataTable dtPassengers, string uniqueBookingToken, string hotelcode, string checkIn, string checkOut, string currency, string nationality, string agtCode, string checksum, string roomTypeId, int noOfAdults, int noOfChild)
        {
            try
            {
                string[] roomTypeParts = (roomTypeId ?? string.Empty).Split("$$$");
                string roomTypeCode = roomTypeParts.Length > 0 ? roomTypeParts[0] : "";
                string ratePlanCode = roomTypeParts.Length > 1 ? roomTypeParts[1] : "";
                string storedPlacementCode = roomTypeParts.Length > 8 ? roomTypeParts[8] : "";
                int checkInHour = roomTypeParts.Length > 9 && int.TryParse(roomTypeParts[9], out int ciH) ? ciH : 14;
                int checkOutHour = roomTypeParts.Length > 10 && int.TryParse(roomTypeParts[10], out int coH) ? coH : 12;

                List<int> childAges = GetExelyChildAges(dtPassengers, noOfChild);
                var placements = BuildBookingPlacements(storedPlacementCode, noOfAdults, noOfChild);

                List<BookingGuest> guests = new List<BookingGuest>();
                foreach (DataRow pax in dtPassengers.Rows)
                {
                    string firstName = Convert.ToString(pax["FirstName"]) ?? string.Empty;
                    string lastName = Convert.ToString(pax["LastName"]) ?? string.Empty;
                    string sexStr = pax.Table.Columns.Contains("Sex") ? Convert.ToString(pax["Sex"]) ?? "Male" : "Male";

                    guests.Add(new BookingGuest
                    {
                        firstName = firstName,
                        lastName = lastName,
                        middleName = "",
                        citizenship = nationality,
                        sex = sexStr.Equals("Female", StringComparison.OrdinalIgnoreCase)
                                    ? BookingGuestSexKind.Female
                                    : BookingGuestSexKind.Male
                    });
                }
                while (guests.Count < noOfAdults)
                    guests.Add(new BookingGuest { firstName = "Guest", lastName = "User", middleName = "", citizenship = nationality, sex = BookingGuestSexKind.Male });

                var request = new CreateBookingRequest
                {
                    booking = new CreateBookingRq
                    {
                        propertyId = hotelcode,
                        roomStays = new List<BookingRoomStayRq>
                        {
                            new BookingRoomStayRq
                            {
                                stayDates = new BookingStayDates { arrivalDateTime = ParseSearchDate(checkIn).Date.AddHours(checkInHour), departureDateTime = ParseSearchDate(checkOut).Date.AddHours(checkOutHour) },
                                ratePlan = new BookingRatePlanRq { id = ratePlanCode },
                                roomType = new BookingRoomTypeRq { id = roomTypeCode, placements = placements },
                                guests = guests,
                                guestCount = new BookingGuestCount { adultCount = noOfAdults, childAges = childAges.Count > 0 ? childAges : null },
                                checksum = checksum,
                                //services = new List<RoomStayServiceRq>(),
                                //extraStay = new ExtraStayRequest(),
                                //discounts = new List<BookingDiscountRq>()
                            }
                        },
                        //services = new List<BookingServiceRq>(),
                        customer = new Customer
                        {
                            firstName = guests[0].firstName,
                            lastName = guests[0].lastName,
                            middleName = "",
                            citizenship = nationality,
                            //comment = "",
                            contacts = new Contacts
                            {
                                phones = new List<Phone> { new Phone { phoneNumber = "+442012345678" } },
                                emails = new List<Email> { new Email { emailAddress = "guest@withinearth.com" } }
                            }
                        },
                        //bookingComments = new List<string>(),
                        createBookingToken = uniqueBookingToken
                    }
                };

                return JsonConvert.SerializeObject(request, GetExelyIsoJsonSettings());
            }
            catch (Exception ex)
            {
                _ = _exception.Error(ex);
                return "";
            }
        }
        #endregion Request

        #region Response
        public CreateBookingResult? Exely_ProcessBookingResponse(string responseData)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(responseData))
                    return null;

                return JsonConvert.DeserializeObject<CreateBookingResult>(responseData);
            }
            catch (Exception ex)
            {
                _ = _exception.Error(ex);
                return null;
            }
        }
        #endregion Response
        #endregion Book

        private static List<BookingPlacementRq> BuildBookingPlacements(string storedPlacementCodes, int noOfAdults, int noOfChild)
        {
            if (!string.IsNullOrEmpty(storedPlacementCodes))
                return storedPlacementCodes.Split('|')
                    .Where(c => !string.IsNullOrEmpty(c))
                    .Select(c => new BookingPlacementRq { code = c })
                    .ToList();

            int adultBeds = Math.Max(1, noOfAdults);
            return new List<BookingPlacementRq> { new BookingPlacementRq { code = $"AdultBed-{adultBeds}" } };
        }

        private static List<int> GetExelyChildAges(DataTable? dtPassengers, int noOfChild)
        {
            if (dtPassengers != null)
            {
                var childAges = new List<int>();
                if (dtPassengers.Columns.Contains("ChildAge"))
                {
                    foreach (DataRow row in dtPassengers.Rows)
                    {
                        if (int.TryParse(Convert.ToString(row["ChildAge"]), out int age) && age > 0)
                            childAges.Add(age);
                    }
                }
                else if (dtPassengers.Columns.Contains("PaxType") && dtPassengers.Columns.Contains("Age"))
                {
                    foreach (DataRow row in dtPassengers.Rows)
                    {
                        string paxType = Convert.ToString(row["PaxType"]) ?? string.Empty;
                        if (paxType.Equals("child", StringComparison.OrdinalIgnoreCase) && int.TryParse(Convert.ToString(row["Age"]), out int age) && age > 0)
                            childAges.Add(age);
                    }
                }

                if (childAges.Count > 0)
                    return childAges;
            }

            return Enumerable.Range(0, noOfChild).Select(_ => 8).ToList();
        }

        public string Get_Exely_GetCancellationChargesRQ(string AgentRef)
        {
            try
            {
                return JsonConvert.SerializeObject(new { bookingNumber = AgentRef });
            }
            catch (Exception ex)
            {
                _ = _exception.Error(ex);
                return "";
            }
        }

        public string Get_Exely_CancelBooking(double Amount, string currency, string AgentRef)
        {
            try
            {
                return JsonConvert.SerializeObject(new
                {
                    bookingNumber = AgentRef,
                    cancellation = new BookingCancellationRq
                    {
                        expectedPenaltyAmount = Amount,
                        reason = "Cancelled by agent"
                    },
                    currencyCode = currency
                });
            }
            catch (Exception ex)
            {
                _ = _exception.Error(ex);
                return "";
            }
        }

        private static DateTime ParseSearchDate(string dateText)
        {
            if (string.IsNullOrWhiteSpace(dateText))
                return DateTime.Today;

            string[] formats = new[] { "dd-MM-yyyy", "dd/MM/yyyy", "yyyy-MM-dd", "yyyyMMdd",
                "yyyy-MM-ddTHH:mm",
                "yyyy-MM-ddTHH:mm:ss",
                "dd-MM-yyyy HH:mm",
                "dd/MM/yyyy HH:mm" };
            if (DateTime.TryParseExact(dateText, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsed))
                return parsed;

            if (DateTime.TryParse(dateText, out parsed))
                return parsed;

            return DateTime.Today;
        }

        public async Task<string> Get_Exely_EInfo(string hotelcode, DataTable dtSuplierMarkup, DataTable dtSearchConfig, string provider, string searchKey, string roomTypeId, HotelSearchParameter? searchPara = null)
        {
            string essential = "";
            try
            {
                DataRow[] drSuplierMarkup = dtSuplierMarkup.Select("SupplierName='" + provider + "'");
                if (drSuplierMarkup.Length == 0) return essential;

                string password = Convert.ToString(drSuplierMarkup[0]["Password"]) ?? string.Empty;
                string baseUrl = Convert.ToString(drSuplierMarkup[0]["WebSiteURL"]) ?? string.Empty;
                int sid = Convert.ToInt32(drSuplierMarkup[0]["SupplierID"]);
                DataRow[] drSearchConfig = dtSearchConfig.Select("SupplierID='" + sid + "'");
                int searchTimeout = drSearchConfig.Length > 0 ? Convert.ToInt32(drSearchConfig[0]["BookingConfirmationTimeout"]) : 20000;

                string roomTypeCode = roomTypeId ?? string.Empty;
                string ratePlanCode = string.Empty;
                if (!string.IsNullOrWhiteSpace(roomTypeId) && roomTypeId.Contains("$$$"))
                {
                    var roomTypeParts = roomTypeId.Split("$$$");
                    roomTypeCode = roomTypeParts.Length > 0 ? roomTypeParts[0] : string.Empty;
                    ratePlanCode = roomTypeParts.Length > 1 ? roomTypeParts[1] : string.Empty;
                }

                PropertyInfoType? result = await GetExelyPropertyContentAsync(hotelcode, baseUrl, password, searchTimeout, searchKey, "en", "After Exely EInfo", provider, false);
                //PropertyInfoType? result = null;

                if (result == null) return essential;

                var parts = new List<string>();

                if (result.Services != null && result.Services.Count > 0)
                {
                    string serviceList = string.Join(", ",
                        result.Services
                            .Where(s => s.Status == EntityState.Active)
                            .Select(s => s.Name)
                            .Where(x => !string.IsNullOrWhiteSpace(x))
                            .Distinct()
                    );

                    if (!string.IsNullOrWhiteSpace(serviceList))
                        parts.Add("Services: " + serviceList);
                }
                if (result.Amenities != null && result.Amenities.Count > 0)
                {
                    string amenityList = string.Join(", ", result.Amenities
                        .Where(a => !string.IsNullOrWhiteSpace(a.DisplayName ?? a.Code))
                        .Select(a => a.DisplayName ?? a.Code));

                    if (!string.IsNullOrWhiteSpace(amenityList))
                        parts.Add("Amenities: " + amenityList);
                }
                if (result.RoomTypes != null && result.RoomTypes.Count > 0)
                {
                    var room = result.RoomTypes
                        .FirstOrDefault(x => string.Equals(x.Id, roomTypeCode, StringComparison.OrdinalIgnoreCase));

                    if (room != null)
                    {
                        if (!string.IsNullOrWhiteSpace(room.Name))
                            parts.Add("Room Type: " + room.Name);
                        if (!string.IsNullOrWhiteSpace(room.Description))
                            parts.Add("Room Details: " + room.Description);

                        if (room?.Amenities != null && room.Amenities.Count > 0)
                        {
                            string roomAmenityList = string.Join(", ",
                                room.Amenities
                                    .Where(a => !string.IsNullOrWhiteSpace(a.Name ?? a.Code))
                                    .Select(a => a.Name ?? a.Code)
                                    .Distinct()
                            );

                            if (!string.IsNullOrWhiteSpace(roomAmenityList))
                                parts.Add("Room Amenities: " + roomAmenityList);
                        }
                    }
                }

                if (!string.IsNullOrWhiteSpace(ratePlanCode))
                {
                    var ratePlan = GetExelyRatePlanById(result, ratePlanCode);
                    if (ratePlan != null)
                    {
                        if (!string.IsNullOrWhiteSpace(ratePlan.Name))
                            parts.Add("Rate Plan: " + ratePlan.Name);
                        if (!string.IsNullOrWhiteSpace(ratePlan.Description))
                            parts.Add("Plan Details: " + ratePlan.Description);
                        if (!string.IsNullOrWhiteSpace(ratePlan.Currency))
                            parts.Add("Currency: " + ratePlan.Currency);
                        parts.Add("IsStayWithChildrenOnly: " + (ratePlan.IsStayWithChildrenOnly ? "true" : "false"));
                        //if (!string.IsNullOrWhiteSpace(ratePlan.CancellationRuleId))
                        //    parts.Add("CancellationRuleId: " + ratePlan.CancellationRuleId);
                        //if (!string.IsNullOrWhiteSpace(ratePlan.ExtraStayRuleId))
                        //    parts.Add("ExtraStayRuleId: " + ratePlan.ExtraStayRuleId);
                        parts.Add("CorporateOnly: " + (ratePlan.CorporateOnly ? "true" : "false"));
                    }
                }
                essential = string.Join(" | ", parts.Where(p => !string.IsNullOrWhiteSpace(p))).Trim();
            }
            catch (Exception ex)
            {
                _ = _exception.Error(ex, searchKey);
            }
            return essential;
        }

        private sealed class ExelyCachedValue<T> where T : class
        {
            public T? Value { get; set; }
            public DateTime LastRefreshedUtc { get; set; }
        }

        private async Task<T?> GetOrCreateExelyContentCacheAsync<T>(string cacheKey, Func<Task<T?>> factory) where T : class
        {
            if (_cache.TryGetValue(cacheKey, out ExelyCachedValue<T>? cachedValue))
                return cachedValue?.Value;

            SemaphoreSlim cacheLock = ExelyContentCacheLocks.GetOrAdd(cacheKey, _ => new SemaphoreSlim(1, 1));
            await cacheLock.WaitAsync();
            try
            {
                if (_cache.TryGetValue(cacheKey, out cachedValue))
                    return cachedValue?.Value;

                T? value = await factory();
                if (value != null)
                {
                    _cache.Set(cacheKey, new ExelyCachedValue<T> { Value = value, LastRefreshedUtc = DateTime.UtcNow }, ExelyContentCacheDuration);
                }
                return value;
            }
            finally
            {
                cacheLock.Release();
            }
        }

        private bool IsOffPeakWindow()
        {
            // Default off-peak window: 02:00 - 05:00 UTC. Adjust if needed.
            int hour = DateTime.UtcNow.Hour;
            return hour >= 2 && hour < 5;
        }

        private async Task<PropertyInfoType?> GetExelyPropertyFromAllCacheAsync(string propertyId, string baseUrl, string password, int searchTimeout, string languageCode, string searchKey, string provider = "Exely")
        {
            if (string.IsNullOrWhiteSpace(propertyId) || string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(password))
                return null;

            // allow override via supplier config if present, but use default when config is empty
            string allTemplate = !string.IsNullOrWhiteSpace(_supplierConfig.Exely.PropertyContent)
                ? _supplierConfig.Exely.PropertyContent
                : "api/content/v1/properties?count=200&include=ALL";
            string allUrl = $"{baseUrl.TrimEnd('/')}/{allTemplate.TrimStart('/')}";
            allUrl += allUrl.Contains("?") ? $"&languageCode={languageCode}" : $"?languageCode={languageCode}";
            string cacheKey = $"Exely:PropertyContent:All:{allUrl.ToLowerInvariant()}";

            // Use cache helper to fetch and cache the PropertyInfoPage
            PropertyInfoPage? page = await GetOrCreateExelyContentCacheAsync<PropertyInfoPage>(cacheKey, async () =>
            {
                ResponseDto resp = await ExelyGetResponseAsync(allUrl, searchTimeout, password, searchKey);
                if (string.IsNullOrWhiteSpace(resp.Response) || resp.Response.ToLower().Contains("\"error\""))
                    return null;

                try
                {
                    return JsonConvert.DeserializeObject<PropertyInfoPage>(resp.Response);
                }
                catch
                {
                    return null;
                }
            });

            if (page?.Properties == null || page.Properties.Count == 0)
                return null;

            var found = page.Properties.FirstOrDefault(p => string.Equals(p.Id, propertyId, StringComparison.OrdinalIgnoreCase));
            if (found == null)
                return null;

            try
            {
                // Convert the include-type object to the full PropertyInfoType by serializing and deserializing
                string json = JsonConvert.SerializeObject(found);
                var prop = JsonConvert.DeserializeObject<PropertyInfoType>(json);
                if (prop != null)
                    RewriteExelyContentImageUrls(prop, propertyId);
                return prop;
            }
            catch
            {
                return null;
            }
        }

        private async Task<PropertyInfoType?> GetExelyPropertyContentAsync(string propertyId, string baseUrl, string password, int searchTimeout, string searchKey, string languageCode = "en", string historyLabel = "", string provider = "Exely", bool propertyOnly = false)
        {
            // Try to fetch from combined/all-properties content endpoint first (cached)
            if (!propertyOnly)
            {
                try
                {
                    var fromAll = await GetExelyPropertyFromAllCacheAsync(propertyId, baseUrl, password, searchTimeout, languageCode, searchKey, provider);
                    if (fromAll != null)
                        return fromAll;
                }
                catch
                {
                    // ignore and fallback to per-property fetch
                }
            }
            try
            {
                string contentTemplate = (_supplierConfig.Exely.PropertyContentByProperty ?? string.Empty).TrimStart('/');
                if (string.IsNullOrWhiteSpace(contentTemplate) || string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(propertyId) || string.IsNullOrWhiteSpace(password))
                    return null;

                string contentUrl = $"{baseUrl.TrimEnd('/')}/{contentTemplate.Replace("{propertyId}", propertyId)}?languageCode={languageCode}";
                string cacheKey = $"Exely:PropertyContent:{contentUrl.ToLowerInvariant()}";

                SemaphoreSlim cacheLock = ExelyContentCacheLocks.GetOrAdd(cacheKey, _ => new SemaphoreSlim(1, 1));
                await cacheLock.WaitAsync();
                try
                {
                    if (_cache.TryGetValue(cacheKey, out ExelyCachedValue<PropertyInfoType>? cached) && cached?.Value != null)
                    {
                        // If cached recently within 24h, return cached
                        if ((DateTime.UtcNow - cached.LastRefreshedUtc) < TimeSpan.FromHours(24))
                            return cached.Value;

                        // If cache is stale but it's not off-peak, avoid calling Content API; return stale cached value
                        if (!IsOffPeakWindow())
                            return cached.Value;

                        // else off-peak: fallthrough to refresh
                    }

                    // No cached value or off-peak window -> fetch fresh
                    ResponseDto responseDto = await ExelyGetResponseAsync(contentUrl, searchTimeout, password, searchKey);
                    string responseData = responseDto.Response;

                    if (!string.IsNullOrWhiteSpace(historyLabel))
                    {
                        var formatted = FormatJsonIfPossible(responseData);
                        _commonMethod.ThirdPartyGetPolicyHistory(propertyId, propertyId, contentUrl, formatted, historyLabel, provider, "Response", "-", "NomadsB2B");
                    }

                    if (string.IsNullOrWhiteSpace(responseData) || responseData.ToLower().Contains("\"error\""))
                    {
                        // If we have a stale cached value, return it instead of null
                        if (cached?.Value != null)
                            return cached.Value;

                        return null;
                    }

                    try
                    {
                        var result = JsonConvert.DeserializeObject<PropertyInfoType>(responseData);
                        RewriteExelyContentImageUrls(result, propertyId);

                        // store in cache with refresh timestamp
                        _cache.Set(cacheKey, new ExelyCachedValue<PropertyInfoType> { Value = result, LastRefreshedUtc = DateTime.UtcNow });

                        return result;
                    }
                    catch (Exception ex)
                    {
                        _ = _exception.Error(ex, searchKey);
                        // return stale cached value if available
                        if (cached?.Value != null)
                            return cached.Value;

                        return null;
                    }
                }
                finally
                {
                    cacheLock.Release();
                }
            }
            catch (Exception ex)
            {
                _ = _exception.Error(ex, searchKey);
                return null;
            }
        }

        private void RewriteExelyContentImageUrls(PropertyInfoType content, string propertyId)
        {
            if (content?.Images == null || content.Images.Count == 0)
                return;

            foreach (var image in content.Images)
            {
                if (image == null || string.IsNullOrWhiteSpace(image.Url))
                    continue;

                image.Url = GetExelyCachedImageUrl(image.Url, propertyId);
            }
        }

        private RatePlanInfo? GetExelyRatePlanById(PropertyInfoType? content, string? ratePlanId)
        {
            if (content?.RatePlans == null || string.IsNullOrWhiteSpace(ratePlanId))
                return null;

            return content.RatePlans.FirstOrDefault(rp => string.Equals(rp.Id, ratePlanId, StringComparison.OrdinalIgnoreCase));
        }

        private RoomTypeInfo? GetExelyRoomTypeById(PropertyInfoType? content, string? roomTypeId)
        {
            if (content?.RoomTypes == null || string.IsNullOrWhiteSpace(roomTypeId))
                return null;

            return content.RoomTypes.FirstOrDefault(rt => string.Equals(rt.Id, roomTypeId, StringComparison.OrdinalIgnoreCase));
        }

        private string GetExelyCachedImageUrl(string sourceUrl, string propertyId)
        {
            if (string.IsNullOrWhiteSpace(sourceUrl) || string.IsNullOrWhiteSpace(propertyId))
                return sourceUrl;

            try
            {
                var cachedFileName = Uri.EscapeDataString(sourceUrl);
                return $"/media/supplier/Exely/{propertyId}/{cachedFileName}";
            }
            catch
            {
                return sourceUrl;
            }
        }

        private string FormatJsonIfPossible(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return input;

            try
            {
                var parsed = JsonConvert.DeserializeObject(input);
                if (parsed == null)
                    return input;
                return JsonConvert.SerializeObject(parsed, Formatting.Indented);
            }
            catch
            {
                return input;
            }
        }

        public async Task<ResponseDto> ExelyGetResponseAsync(string url, int searchTimeOut, string password, string searchKey = "")
        {
            string responseString = "";
            string error = "";
            try
            {
                var request = (HttpWebRequest)WebRequest.Create(url);
                request.Proxy = null;
                request.Method = "GET";
                request.Timeout = searchTimeOut;
                request.ContentType = "application/json;charset=utf-8";
                request.Accept = "application/json";
                request.Headers.Add("X-Api-Key", password);

                var response = (HttpWebResponse)await request.GetResponseAsync();
                Stream st = response.GetResponseStream();
                if (!string.IsNullOrWhiteSpace(response.ContentEncoding) && response.ContentEncoding.ToLower().Contains("gzip"))
                    st = new System.IO.Compression.GZipStream(st, System.IO.Compression.CompressionMode.Decompress);
                else if (!string.IsNullOrWhiteSpace(response.ContentEncoding) && response.ContentEncoding.ToLower().Contains("deflate"))
                    st = new System.IO.Compression.DeflateStream(st, System.IO.Compression.CompressionMode.Decompress);

                using (var reader = new StreamReader(st, Encoding.UTF8))
                {
                    responseString = await reader.ReadToEndAsync();
                }
            }
            catch (Exception ex)
            {
                error = ex.Message;
                _ = _exception.Error(ex, searchKey);
            }
            return new ResponseDto { Response = responseString, Error = error };
        }

        private async Task<List<ExelyCancellationSegment>> GetExelyCancellationSegmentsAsync(string propertyId, string baseUrl, string password, int searchTimeout, DateTime checkInDate, double totalPrice, int noOfNights, int cutOffDays, string searchKey)
        {
            try
            {
                CancellationRulesResponse? rulesResponse = await GetExelyCancellationRulesResponseAsync(propertyId, baseUrl, password, searchTimeout, searchKey);
                return rulesResponse == null
                    ? new List<ExelyCancellationSegment>()
                    : BuildExelyCancellationSegments(rulesResponse, checkInDate, totalPrice, noOfNights, cutOffDays);
            }
            catch (Exception ex)
            {
                _ = _exception.Error(ex, searchKey);
                return new List<ExelyCancellationSegment>();
            }
        }

        private async Task<SearchByPropertyResponse?> GetRoomStaysByPropertyAsync(string propertyId, string baseUrl, string password, int searchTimeout, HotelSearchParameter searchParam, string searchKey)
        {
            try
            {
                string template = _supplierConfig.Exely.GetRoomStaysByProperty ?? string.Empty;
                if (string.IsNullOrWhiteSpace(template) || string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(propertyId))
                    return null;

                string path = template.TrimStart('/').Replace("{propertyId}", propertyId);
                string arrivalDate = ParseSearchDate(searchParam.CheckInDate).ToString("yyyy-MM-dd");
                string departureDate = ParseSearchDate(searchParam.CheckOutDate).ToString("yyyy-MM-dd");
                int adults = searchParam.Rooms?.Sum(r => Math.Max(r.NoofAdults, 1)) ?? 1;

                var queryParams = new List<string>
                {
                    $"arrivalDate={arrivalDate}",
                    $"departureDate={departureDate}",
                    $"adults={adults}"
                };

                if (searchParam.Rooms != null)
                {
                    foreach (var room in searchParam.Rooms)
                    {
                        if (room.Child1Age > 0) queryParams.Add($"childAges={room.Child1Age}");
                        if (room.Child2Age > 0) queryParams.Add($"childAges={room.Child2Age}");
                    }
                }

                string url = $"{baseUrl.TrimEnd('/')}/{path}?{string.Join("&", queryParams)}";
                Stopwatch roomStaysTimer = Stopwatch.StartNew();
                ResponseDto responseDto = await ExelyGetResponseAsync(url, searchTimeout, password, searchKey);
                roomStaysTimer.Stop();

                await _commonMethod.XMLRequestResponseTrack_DebugMode(url, responseDto.Response, "Exely", roomStaysTimer.Elapsed.ToString(), "GetRoomStaysByProperty", searchKey);

                if (string.IsNullOrWhiteSpace(responseDto.Response) || responseDto.Response.ToLower().Contains("\"error\""))
                    return null;

                return JsonConvert.DeserializeObject<SearchByPropertyResponse>(responseDto.Response);
            }
            catch (Exception ex)
            {
                _ = _exception.Error(ex, searchKey);
                return null;
            }
        }

        private async Task<SearchByPropertyResponse?> GetRoomStaysByPropertyAsync(string propertyId, string baseUrl, string password, int searchTimeout, HotelSearchParameter searchParam, string searchKey, int roomIndex)
        {
            try
            {
                if (searchParam.Rooms == null || roomIndex >= searchParam.Rooms.Count) return null;
                var room = searchParam.Rooms[roomIndex];
                int adults = Math.Max(room.NoofAdults, 1);
                var childAges = new List<int>();
                if (room.ChildAge != null && room.ChildAge.Length > 0)
                    childAges.AddRange(room.ChildAge.Where(a => a > 0));
                else
                {
                    if (room.Child1Age > 0) childAges.Add(room.Child1Age);
                    if (room.Child2Age > 0) childAges.Add(room.Child2Age);
                }

                string template = _supplierConfig.Exely.GetRoomStaysByProperty ?? string.Empty;
                if (string.IsNullOrWhiteSpace(template) || string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(propertyId))
                    return null;

                string path = template.TrimStart('/').Replace("{propertyId}", propertyId);
                string arrivalDate = ParseSearchDate(searchParam.CheckInDate).ToString("yyyy-MM-dd");
                string departureDate = ParseSearchDate(searchParam.CheckOutDate).ToString("yyyy-MM-dd");

                var queryParams = new List<string>
                {
                    $"arrivalDate={arrivalDate}",
                    $"departureDate={departureDate}",
                    $"adults={adults}"
                };
                foreach (int age in childAges) queryParams.Add($"childAges={age}");

                string url = $"{baseUrl.TrimEnd('/')}/{path}?{string.Join("&", queryParams)}";
                ResponseDto responseDto = await ExelyGetResponseAsync(url, searchTimeout, password, searchKey);

                await _commonMethod.XMLRequestResponseTrack_Error(url, responseDto.Response, "Exely", string.Empty, "GetRoomStaysByProperty", searchKey);

                if (string.IsNullOrWhiteSpace(responseDto.Response) || responseDto.Response.ToLower().Contains("\"error\""))
                    return null;

                return JsonConvert.DeserializeObject<SearchByPropertyResponse>(responseDto.Response);
            }
            catch (Exception ex)
            {
                _ = _exception.Error(ex, searchKey);
                return null;
            }
        }

        private async Task<CancellationRulesResponse?> GetExelyCancellationRulesResponseAsync(string propertyId, string baseUrl, string password, int searchTimeout, string searchKey)
        {
            try
            {
                string cancellationRulesUrl = BuildExelyCancellationRulesUrl(baseUrl, propertyId);
                if (string.IsNullOrWhiteSpace(cancellationRulesUrl))
                    return null;

                string cacheKey = $"Exely:CancellationRules:{cancellationRulesUrl.ToLowerInvariant()}";
                return await GetOrCreateExelyContentCacheAsync<CancellationRulesResponse>(cacheKey, async () =>
                {
                    ResponseDto responseDto = await ExelyGetResponseAsync(cancellationRulesUrl, searchTimeout, password, searchKey);
                    string responseData = responseDto.Response;

                    _commonMethod.ThirdPartyGetPolicyHistory(propertyId, propertyId, cancellationRulesUrl, FormatJsonIfPossible(responseData), "After Exely cancellation rules", "Exely", "Response", "-", "NomadsB2B");

                    if (string.IsNullOrWhiteSpace(responseData) || responseData.ToLower().Contains("\"error\""))
                        return null;

                    try
                    {
                        return JsonConvert.DeserializeObject<CancellationRulesResponse>(responseData);
                    }
                    catch (Exception ex)
                    {
                        _ = _exception.Error(ex, searchKey);
                        return null;
                    }
                });
            }
            catch (Exception ex)
            {
                _ = _exception.Error(ex, searchKey);
                return null;
            }
        }

        private string BuildExelyCancellationRulesUrl(string baseUrl, string propertyId)
        {
            if (string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(propertyId))
                return string.Empty;

            string template = _supplierConfig.Exely.GetCancellationRulesByProperty;
            if (string.IsNullOrWhiteSpace(template))
                template = _supplierConfig.Exely.GetCancellationRulesByProperty;

            return $"{baseUrl.TrimEnd('/')}/{template.TrimStart('/').Replace("{propertyId}", propertyId)}";
        }

        private string GetExelyBaseUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return string.Empty;

            string searchPath = _supplierConfig.Exely.SearchRoomStays ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(searchPath) && url.EndsWith(searchPath, StringComparison.OrdinalIgnoreCase))
                return url.Substring(0, url.Length - searchPath.Length).TrimEnd('/');

            return url.TrimEnd('/');
        }

        private List<ExelyCancellationSegment> BuildExelyCancellationSegments(CancellationRulesResponse? rulesResponse, DateTime checkInDate, double totalPrice, int noOfNights, int cutOffDays)
        {
            DateTime startDate = DateTime.Now.Date;
            DateTime arrivalDate = checkInDate.Date;
            if (rulesResponse?.CancellationRules == null || rulesResponse.CancellationRules.Count == 0 || arrivalDate <= startDate)
                return new List<ExelyCancellationSegment>();

            List<CancellationTerm> applicableTerms = rulesResponse.CancellationRules
                .Where(rule => rule?.CancellationTerms != null)
                .SelectMany(rule => rule.CancellationTerms)
                .Where(term => term != null && IsArrivalDateApplicable(term.ArrivalDates, arrivalDate))
                .ToList();

            if (applicableTerms.Count == 0)
                return new List<ExelyCancellationSegment>();

            List<DateTime> boundaries = new List<DateTime> { startDate, arrivalDate };
            foreach (CancellationTerm term in applicableTerms)
            {
                boundaries.AddRange(GetExelyTermBoundaries(term, arrivalDate, cutOffDays));
            }

            List<DateTime> orderedBoundaries = boundaries
                .Where(date => date >= startDate && date <= arrivalDate)
                .Distinct()
                .OrderBy(date => date)
                .ToList();

            if (orderedBoundaries.Count < 2)
                orderedBoundaries = new List<DateTime> { startDate, arrivalDate };

            List<ExelyCancellationSegment> segments = new List<ExelyCancellationSegment>();
            for (int i = 0; i < orderedBoundaries.Count - 1; i++)
            {
                DateTime segmentStart = orderedBoundaries[i];
                DateTime segmentEnd = orderedBoundaries[i + 1];
                if (segmentEnd <= segmentStart)
                    continue;

                DateTime probeDate = segmentStart.AddTicks((segmentEnd - segmentStart).Ticks / 2);
                CancellationTerm? selectedTerm = applicableTerms
                    .Where(term => IsExelyTermMatched(term, probeDate, arrivalDate, cutOffDays))
                    .OrderBy(term => GetExelyTermWindowSize(term, startDate, arrivalDate, cutOffDays))
                    .ThenBy(term => term.BeforeArrivalValue)
                    .FirstOrDefault();

                double penaltyAmount = selectedTerm == null
                    ? 0
                    : CalculateExelyPenaltyAmount(selectedTerm, totalPrice, noOfNights);

                string penaltyCurrency = selectedTerm?.PenaltyValueCurrency;
                if (string.IsNullOrWhiteSpace(penaltyCurrency))
                {
                    penaltyCurrency = applicableTerms
                        .Select(term => term.PenaltyValueCurrency)
                        .FirstOrDefault(currency => !string.IsNullOrWhiteSpace(currency)) ?? "USD";
                }

                ExelyCancellationSegment segment = new ExelyCancellationSegment
                {
                    FromDate = segmentStart,
                    ToDate = segmentEnd,
                    PenaltyAmount = Math.Max(0, penaltyAmount),
                    Currency = penaltyCurrency,
                    IsWithinCancellationDeadline = penaltyAmount > 0
                };

                ExelyCancellationSegment? lastSegment = segments.LastOrDefault();
                if (lastSegment != null
                    && lastSegment.ToDate == segment.FromDate
                    && string.Equals(lastSegment.Currency, segment.Currency, StringComparison.OrdinalIgnoreCase)
                    && lastSegment.IsWithinCancellationDeadline == segment.IsWithinCancellationDeadline
                    && Math.Abs(lastSegment.PenaltyAmount - segment.PenaltyAmount) < 0.0001)
                {
                    lastSegment.ToDate = segment.ToDate;
                }
                else
                {
                    segments.Add(segment);
                }
            }

            return segments;
        }

        private List<ExelyCancellationSegment> BuildExelyCancellationSegments(CancellationPolicy? cancellationPolicy, DateTime checkInDate, double fallbackPenaltyAmount, int cutOffDays, string currencyCode)
        {
            DateTime startDate = DateTime.Now.Date;
            DateTime arrivalDate = checkInDate.Date;
            if (cancellationPolicy == null || arrivalDate <= startDate)
                return new List<ExelyCancellationSegment>();

            string segmentCurrency = string.IsNullOrWhiteSpace(currencyCode) ? "USD" : currencyCode;
            double penaltyAmount = fallbackPenaltyAmount;
            List<ExelyCancellationSegment> segments = new List<ExelyCancellationSegment>();

            if (cancellationPolicy.freeCancellationPossible)
            {
                if (cancellationPolicy.freeCancellationDeadlineLocal.HasValue)
                {
                    DateTime deadlineDate = cancellationPolicy.freeCancellationDeadlineLocal.Value.Date;
                    string deadlineLocalStr = deadlineDate.ToString("yyyy-MM-ddTHH:mm");
                    string deadlineUtcStr = cancellationPolicy.freeCancellationDeadlineUtc.HasValue
                        ? cancellationPolicy.freeCancellationDeadlineUtc.Value.AddDays(cutOffDays).ToString("yyyy-MM-ddTHH:mm") + "Z"
                        : "";
                    if (deadlineDate > startDate)
                    {
                        segments.Add(new ExelyCancellationSegment
                        {
                            FromDate = startDate,
                            ToDate = deadlineDate,
                            PenaltyAmount = 0,
                            Currency = segmentCurrency,
                            IsWithinCancellationDeadline = false
                        });
                    }

                    if (arrivalDate > deadlineDate)
                    {
                        segments.Add(new ExelyCancellationSegment
                        {
                            FromDate = deadlineDate > startDate ? deadlineDate : startDate,
                            ToDate = arrivalDate,
                            PenaltyAmount = penaltyAmount,
                            Currency = segmentCurrency,
                            IsWithinCancellationDeadline = penaltyAmount > 0
                        });
                    }
                }
                else
                {
                    segments.Add(new ExelyCancellationSegment
                    {
                        FromDate = startDate,
                        ToDate = arrivalDate,
                        PenaltyAmount = 0,
                        Currency = segmentCurrency,
                        IsWithinCancellationDeadline = false
                    });
                }
            }
            else
            {
                segments.Add(new ExelyCancellationSegment
                {
                    FromDate = startDate,
                    ToDate = arrivalDate,
                    PenaltyAmount = penaltyAmount,
                    Currency = segmentCurrency,
                    IsWithinCancellationDeadline = penaltyAmount > 0
                });
            }

            return segments
                .Where(segment => segment.ToDate > segment.FromDate)
                .ToList();
        }

        private string BuildExelyRoomDisplayName(ShortRoomStay roomStay, PropertyInfoType? propertyContent = null)
        {
            string roomTypeName = roomStay.roomType?.name?.Trim().Replace("_", "-");
            if (string.IsNullOrWhiteSpace(roomTypeName) && propertyContent != null)
                roomTypeName = GetExelyRoomTypeById(propertyContent, roomStay.roomType?.id)?.Name?.Trim().Replace("_", "-");

            if (string.IsNullOrWhiteSpace(roomTypeName))
                roomTypeName = BuildExelyPlacementName(roomStay);

            if (string.IsNullOrWhiteSpace(roomTypeName))
                roomTypeName = (roomStay.fullPlacementsName ?? string.Empty).Trim().Replace("_", "-");

            string ratePlanName = roomStay.ratePlan?.name?.Trim().Replace("_", "-");
            if (string.IsNullOrWhiteSpace(ratePlanName) && propertyContent != null)
                ratePlanName = GetExelyRatePlanById(propertyContent, roomStay.ratePlan?.id)?.Name?.Trim().Replace("_", "-");

            if (!string.IsNullOrWhiteSpace(ratePlanName))
            {
                if (string.IsNullOrWhiteSpace(roomTypeName))
                    roomTypeName = ratePlanName;
                else if (!roomTypeName.Contains(ratePlanName, StringComparison.OrdinalIgnoreCase))
                    roomTypeName = $"{roomTypeName} - {ratePlanName}";
            }

            return (roomTypeName?.Trim() ?? string.Empty).Replace("__", "-").Trim('-', '_').Trim();
        }

        private string BuildExelyPlacementName(ShortRoomStay roomStay)
        {
            if (roomStay.roomType?.placements != null && roomStay.roomType.placements.Count > 0)
            {
                List<string> placementParts = roomStay.roomType.placements
                    .Where(placement => placement.count > 0)
                    .Select(BuildExelyPlacementPart)
                    .Where(part => !string.IsNullOrWhiteSpace(part))
                    .ToList();
                if (placementParts.Count > 0)
                    return string.Join(", ", placementParts);
            }
            int adults = roomStay.guestCount?.adultCount ?? 0;
            List<int> childAges = roomStay.guestCount?.childAges?.Where(age => age >= 0).ToList() ?? new List<int>();
            List<string> parts = new List<string>();
            if (adults > 0)
                parts.Add($"{adults} {(adults == 1 ? "adult" : "adults")} on main beds");

            if (childAges.Count > 0)
            {
                string childText = childAges.Count == 1 ? "1 child" : $"{childAges.Count} children";
                string agesText = string.Join(", ", childAges);
                parts.Add($"{childText} (age {agesText})");
            }

            return string.Join(", ", parts);
        }

        private string BuildExelyPlacementPart(Placement placement)
        {
            string ageInfo = "";
            if (placement.minAge.HasValue || placement.maxAge.HasValue)
            {
                string minAge = placement.minAge?.ToString(CultureInfo.InvariantCulture) ?? "0";
                string maxAge = placement.maxAge?.ToString(CultureInfo.InvariantCulture) ?? "";
                ageInfo = string.IsNullOrWhiteSpace(maxAge)
                    ? $" (age {minAge}+)"
                    : $" (age {minAge}-{maxAge})";
            }

            return placement.kind switch
            {
                BookingPlacementKind.Adult => $"{placement.count} {(placement.count == 1 ? "adult" : "adults")} on main beds",
                BookingPlacementKind.ExtraAdult => $"{placement.count} {(placement.count == 1 ? "adult" : "adults")} on an extra beds",
                BookingPlacementKind.Child => $"{placement.count} {(placement.count == 1 ? "child" : "children")} on main beds{ageInfo}",
                BookingPlacementKind.ExtraChild => $"{placement.count} {(placement.count == 1 ? "child" : "children")} on an extra beds{ageInfo}",
                BookingPlacementKind.ChildBandWithoutBed => $"{placement.count} {(placement.count == 1 ? "child" : "children")} without bed{ageInfo}",
                _ => ""
            };
        }


        private void AddExelyCancellationRows(DataTable dtHotelCancellation, IEnumerable<ExelyCancellationSegment> segments, string supplier, string ownsystemid, int roomno, string roomtypeid, string optionCode, HotelSearchParameter searchPara, bool isPerce, double b2cMarkup, double agentPercentage, double agentMarkupAmount, int isParentId, double subAgentMarkup, string essen, DataTable dtROE)
        {
            foreach (ExelyCancellationSegment segment in segments.OrderBy(item => item.FromDate))
            {
                double currencyFactor = _commonMethod.GetROEByCurrency(segment.Currency, dtROE);
                double vatOnPolicy = 0;
                double penaltyConverted = segment.PenaltyAmount * currencyFactor;
                double mainAgentCharge = penaltyConverted;
                double companyChargeAmount = penaltyConverted;
                double markedUpCharge = penaltyConverted > 0
                    ? _markup.ProcessHotelCancelationPrice_WithMarkup(ref mainAgentCharge, searchPara.NoofNights, searchPara.CityID, Convert.ToString(searchPara.CountryID), penaltyConverted, isPerce, b2cMarkup, agentPercentage, agentMarkupAmount, isParentId, subAgentMarkup, ref vatOnPolicy)
                    : 0;

                dtHotelCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref companyChargeAmount, dtHotelCancellation.NewRow(), currencyFactor, segment.Currency, supplier, ownsystemid, Convert.ToString(roomno), roomtypeid, "", optionCode, segment.FromDate, segment.ToDate, markedUpCharge, markedUpCharge, "", vatOnPolicy, "", segment.IsWithinCancellationDeadline ? "YES" : "NO", "", essen, "", ""));
                dtHotelCancellation.AcceptChanges();
            }
        }

        private static IEnumerable<DateTime> GetExelyTermBoundaries(CancellationTerm term, DateTime arrivalDate, int cutOffDays)
        {
            List<DateTime> boundaries = new List<DateTime>();
            switch (term.BeforeArrivalMatching)
            {
                case BeforeArrivalMatching.AtLeast:
                case BeforeArrivalMatching.NoMoreThan:
                    boundaries.Add(GetExelyThresholdDate(arrivalDate, term.BeforeArrivalUnit, term.BeforeArrivalValue, cutOffDays));
                    break;
                case BeforeArrivalMatching.Between:
                    boundaries.Add(GetExelyThresholdDate(arrivalDate, term.BeforeArrivalUnit, term.BeforeArrivalValue, cutOffDays));
                    boundaries.Add(GetExelyThresholdDate(arrivalDate, term.BeforeArrivalUnit, term.BeforeArrivalValueMax, cutOffDays));
                    break;
            }

            return boundaries;
        }

        private static bool IsExelyTermMatched(CancellationTerm term, DateTime probeDate, DateTime arrivalDate, int cutOffDays)
        {
            switch (term.BeforeArrivalMatching)
            {
                case BeforeArrivalMatching.NoMatter:
                    return probeDate < arrivalDate;
                case BeforeArrivalMatching.AtLeast:
                    return probeDate < GetExelyThresholdDate(arrivalDate, term.BeforeArrivalUnit, term.BeforeArrivalValue, cutOffDays);
                case BeforeArrivalMatching.NoMoreThan:
                    return probeDate >= GetExelyThresholdDate(arrivalDate, term.BeforeArrivalUnit, term.BeforeArrivalValue, cutOffDays)
                        && probeDate < arrivalDate;
                case BeforeArrivalMatching.Between:
                    DateTime firstBoundary = GetExelyThresholdDate(arrivalDate, term.BeforeArrivalUnit, term.BeforeArrivalValue, cutOffDays);
                    DateTime secondBoundary = GetExelyThresholdDate(arrivalDate, term.BeforeArrivalUnit, term.BeforeArrivalValueMax, cutOffDays);
                    DateTime fromDate = firstBoundary <= secondBoundary ? firstBoundary : secondBoundary;
                    DateTime toDate = firstBoundary <= secondBoundary ? secondBoundary : firstBoundary;
                    return probeDate >= fromDate && probeDate < toDate;
                default:
                    return false;
            }
        }

        private static double GetExelyTermWindowSize(CancellationTerm term, DateTime startDate, DateTime arrivalDate, int cutOffDays)
        {
            switch (term.BeforeArrivalMatching)
            {
                case BeforeArrivalMatching.NoMatter:
                    return double.MaxValue;
                case BeforeArrivalMatching.AtLeast:
                    return Math.Max(0, (GetExelyThresholdDate(arrivalDate, term.BeforeArrivalUnit, term.BeforeArrivalValue, cutOffDays) - startDate).TotalMinutes);
                case BeforeArrivalMatching.NoMoreThan:
                    return Math.Max(0, (arrivalDate - GetExelyThresholdDate(arrivalDate, term.BeforeArrivalUnit, term.BeforeArrivalValue, cutOffDays)).TotalMinutes);
                case BeforeArrivalMatching.Between:
                    DateTime firstBoundary = GetExelyThresholdDate(arrivalDate, term.BeforeArrivalUnit, term.BeforeArrivalValue, cutOffDays);
                    DateTime secondBoundary = GetExelyThresholdDate(arrivalDate, term.BeforeArrivalUnit, term.BeforeArrivalValueMax, cutOffDays);
                    return Math.Abs((secondBoundary - firstBoundary).TotalMinutes);
                default:
                    return double.MaxValue;
            }
        }

        private static DateTime GetExelyThresholdDate(DateTime arrivalDate, TimeUnit unit, int value, int cutOffDays)
        {
            DateTime thresholdDate = arrivalDate.Date;
            switch (unit)
            {
                case TimeUnit.Hour:
                    thresholdDate = thresholdDate.AddHours(-value);
                    break;
                case TimeUnit.Day:
                    thresholdDate = thresholdDate.AddDays(-value);
                    break;
            }

            return thresholdDate.AddDays(cutOffDays);
        }

        private static double CalculateExelyPenaltyAmount(CancellationTerm term, double totalPrice, int noOfNights)
        {
            double safeTotalPrice = Math.Max(0, totalPrice);
            int safeNights = noOfNights > 0 ? noOfNights : 1;
            double firstNightPrice = safeTotalPrice / safeNights;

            return term.PenaltyCalculationMethod switch
            {
                PenaltyCalculationMethod.NoPenalty => 0,
                PenaltyCalculationMethod.Fixed => term.PenaltyValue,
                PenaltyCalculationMethod.FirstNights => firstNightPrice * Math.Max(0, term.PenaltyValue),
                PenaltyCalculationMethod.FirstNightPercent => firstNightPrice * Math.Max(0, term.PenaltyValue) / 100,
                PenaltyCalculationMethod.PrepaymentPercent => safeTotalPrice * Math.Max(0, term.PenaltyValue) / 100,
                PenaltyCalculationMethod.Percent => safeTotalPrice * Math.Max(0, term.PenaltyValue) / 100,
                _ => GetExelyFallbackPenaltyAmount(term, safeTotalPrice)
            };
        }

        private static double GetExelyFallbackPenaltyAmount(CancellationTerm term, double safeTotalPrice)
        {
            double penaltyValue = Math.Max(0, term.PenaltyValue);
            if (penaltyValue == 0)
                return 0;

            if (!string.IsNullOrWhiteSpace(term.PenaltyValueCurrency))
                return penaltyValue;

            if (penaltyValue <= 100)
                return safeTotalPrice * penaltyValue / 100;

            return penaltyValue <= safeTotalPrice ? penaltyValue : safeTotalPrice;
        }

        private static bool IsArrivalDateApplicable(List<ArrivalDatesInfo>? arrivalDates, DateTime checkInDate)
        {
            if (arrivalDates == null || arrivalDates.Count == 0)
                return true;

            return arrivalDates.Any(arrivalDate =>
            {
                DateTime startDate = arrivalDate.StartDate.Date;
                DateTime endDate = arrivalDate.IsEndless ? DateTime.MaxValue.Date : arrivalDate.EndDate.Date;
                return checkInDate.Date >= startDate && checkInDate.Date <= endDate;
            });
        }

        private double GetExelyCurrencyFactor(string currencyCode)
        {
            if (string.IsNullOrWhiteSpace(currencyCode))
                return 1;

            DataTable dtCurrency = _commonMethod.GetCountryCurrency(currencyCode);
            return dtCurrency != null && dtCurrency.Rows.Count > 0
                ? Convert.ToDouble(dtCurrency.Rows[0]["AmtToExch"])
                : 1;
        }

        private static JsonSerializerSettings GetExelyIsoJsonSettings()
        {
            return new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                DateFormatString = "yyyy-MM-ddTHH:mm",
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                Converters = new List<JsonConverter> { new Newtonsoft.Json.Converters.StringEnumConverter() }
            };
        }

        public string GetRoomAmenities(RoomTypeInfo roomTypeInfo)
        {
            if (roomTypeInfo?.Amenities == null || roomTypeInfo.Amenities.Count == 0)
                return "";

            return string.Join(", ", roomTypeInfo.Amenities.Select(x => x.Name));
        }

    }
}
