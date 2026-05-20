using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Collections.Concurrent;
using System.Data;
using System.Globalization;
using System.Net;
using System.Text;
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

                                                                    if (_keySettings.SupplierProxy == "0")
                                                                        Xhotel.URL = Convert.ToString(ExelyDetails.WebSiteURL).TrimEnd('/') + "/" + _supplierConfig.Exely.SearchRoomStays;
                                                                    else
                                                                        Xhotel.URL = Convert.ToString(ExelyDetails.WebSiteURLforAPI) + _supplierConfig.Exely.SearchRoomStays;

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
                                                                    bool IsHitSeperate = _commonMethod.IsSuppliersWithSeperateRoomRquest(Xhotel.Supplier);
                                                                    int NoOfRoomsSearch = 1;
                                                                    if (IsHitSeperate)
                                                                        NoOfRoomsSearch = SearchPara.Rooms.Count;
                                                                    List<RQRS> RQRS = new List<RQRS>();
                                                                    Xhotel.IsSeperate = IsHitSeperate;
                                                                    for (int rr = 0; rr < NoOfRoomsSearch; rr++)
                                                                    {
                                                                        RQRS objrq = new RQRS();
                                                                        List<RoomDetails> serchRooms = new List<RoomDetails>();
                                                                        serchRooms.Add(SearchPara.Rooms[rr]);
                                                                        objrq.Request = XMLExelyRequestOnly_Async(SearchPara, Xhotel.SearchTimeOut, ExelyCurrencyFactor, DebugMode, dataForSearch.xmlCity, dataForSearch.XmlNationality, dataForSearch.xmlCountry, Xhotel.UserId, Xhotel.UserName, Xhotel.PassWord, Xhotel.CurrencyCode, IdsForSearch, rr);
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

        //public string Get_Exely_AvailabilityRQ(HotelSearchParameter SearchPara, string HotelCode, string currencyCode, RoomDetails room)
        public string Get_Exely_AvailabilityRQ(HotelSearchParameter SearchPara, string HotelCode, string currencyCode, int roomIndex = -1)
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

                // 🔹 Adults count: use specific room if roomIndex given, else Max across all rooms
                int totalAdults = 1;

                if (SearchPara.Rooms != null && SearchPara.Rooms.Count > 0)
                {
                    if (roomIndex >= 0 && roomIndex < SearchPara.Rooms.Count)
                        totalAdults = Math.Max(SearchPara.Rooms[roomIndex].NoofAdults, 1);
                    else
                        totalAdults = SearchPara.Rooms.Max(r => Math.Max(r.NoofAdults, 1));
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
                // 🔹 Final minimal request
                object exelyRequest;
                if (childAges.Count > 0)
                {
                    exelyRequest = new
                    {
                        propertyIds = propertyIds,
                        adults = totalAdults,
                        arrivalDate = ParseSearchDate(SearchPara.CheckInDate).ToString("yyyy-MM-dd"),
                        departureDate = ParseSearchDate(SearchPara.CheckOutDate).ToString("yyyy-MM-dd"),
                        childAges = childAges,
                        currencyCode = string.IsNullOrWhiteSpace(currencyCode) ? "USD" : currencyCode
                    };
                }
                else
                {
                    exelyRequest = new
                    {
                        propertyIds = propertyIds,
                        adults = totalAdults,
                        arrivalDate = ParseSearchDate(SearchPara.CheckInDate).ToString("yyyy-MM-dd"),
                        departureDate = ParseSearchDate(SearchPara.CheckOutDate).ToString("yyyy-MM-dd"),
                        currencyCode = string.IsNullOrWhiteSpace(currencyCode) ? "USD" : currencyCode
                    };
                }

                return JsonConvert.SerializeObject(exelyRequest);
            }
            catch (Exception ex)
            {
                _ = _exception.Error(ex);
                return "";
            }
        }
        #endregion Request

        #region Response
        public async Task<List<Options>> Exely_HotelSearchWithRooms(HotelX Item, CommonSearch commonSearch, string ResponseData, HotelSearchParameter SearchParam, string XmlCity, string XmlCountry, string XmlNationality, string Currency, double CurrencyFactor, DataTable dtROE, string ProccessCutOffTime, Int32 processTimeout, string ResProccessTimerLogActive, string ResProccessTimeValueforSaveLog, string StoreSupplierResErrorLog)
        {
            System.Diagnostics.Stopwatch timer_room_Exely = new System.Diagnostics.Stopwatch();
            timer_room_Exely.Start();

            List<Options> options = new List<Options>();
            DataTable DtHotel = _commonMethod.GenerateDataTableNewV2();
            Dictionary<string, CancellationRulesResponse?> cancellationRulesCache = new Dictionary<string, CancellationRulesResponse?>(StringComparer.OrdinalIgnoreCase);
            try
            {
                var result = JsonConvert.DeserializeObject<AggregationSearchByPropertiesResponse>(ResponseData);
                if (result == null)
                {
                    Item.ResponseStatus = 204;
                    Item.ResponseType = "NoCount";
                    return options;
                }
                if (result.roomStays == null || result.roomStays.Count == 0)
                {
                    Item.SupplierResponseError = ResponseData;
                    Item.ResponseStatus = 404;
                    Item.ResponseType = "NoCount";
                    return options;
                }
                else
                {
                    if (result.roomStays.Count > 0)
                    {
                        double Pricenew = 0;
                        bool willAddOrNot = true;
                        Int64 RaynaID = 0;
                        Int64 GiataId = 0;
                        Int64 OwnsystemHotelId = 0;
                        int RatingHotel = 0;
                        string Provider = Item.Supplier;
                        double MinPrice = Pricenew;

                        for (int i = 0; i < result.roomStays.Count; i++)
                        {
                            Item.ResponseType = "Success";
                            if (ProccessCutOffTime == "1" && timer_room_Exely.ElapsedMilliseconds > processTimeout)
                            {
                                timer_room_Exely.Stop();
                                break;
                            }

                            string hotelCode = result.roomStays[i].propertyId;
                            // For multi-room search: skip room types that don't have enough allotment.
                            // Only apply when supplier explicitly reports availability (> 0) to avoid false filtering.
                            if (SearchParam.Rooms.Count > 1 && result.roomStays[i].availability > 0 && result.roomStays[i].availability < SearchParam.Rooms.Count)
                                continue;
                            string currency = string.IsNullOrWhiteSpace(result.roomStays[i].currencyCode)
                                ? Item.CurrencyCode
                                : result.roomStays[i].currencyCode;

                            double Factor = 1;
                            if (!string.IsNullOrEmpty(currency))
                            {
                                Factor = _commonMethod.GetROEByCurrency(currency, dtROE);
                            }

                            double FinalTotal = result.roomStays[i].total.priceBeforeTax * Factor;
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

                            for (int j = 0; j < result.roomStays.Count; j++)
                            {
                                double RoomRate = result.roomStays[j].total.priceBeforeTax
                                                + result.roomStays[j].total.taxAmount;

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
                                            List<ShortRoomStay> roomsForHotel = result.roomStays
                                                .Where(rs => string.Equals(rs.propertyId, hotelCode, StringComparison.OrdinalIgnoreCase))
                                                .ToList();
                                            // Fetch English content from Content API (room names, category names, hotel description), cached for 24 hours
                                            string contentCacheKey = $"ExelyPropertyContent_{hotelCode}_en";
                                            Dictionary<string, string>? roomTypeEnNames;
                                            Dictionary<string, string>? roomTypeCategoryNames;
                                            string? hotelDescFromContent;
                                            if (!_cache.TryGetValue(contentCacheKey, out (Dictionary<string, string>? Names, Dictionary<string, string>? CategoryNames, string? Description) contentData))
                                            {
                                                contentData = (null, null, null);
                                                if (!string.IsNullOrWhiteSpace(exelyBaseUrl) && !string.IsNullOrWhiteSpace(Item.PassWord))
                                                {
                                                    string enContentTpl = (_supplierConfig.Exely.PropertyContentByProperty ?? string.Empty).TrimStart('/');
                                                    string enContentUrl = $"{exelyBaseUrl.TrimEnd('/')}/{enContentTpl.Replace("{propertyId}", hotelCode)}?languageCode=en";
                                                    ResponseDto enContentDto = await ExelyGetResponseAsync(enContentUrl, Item.SearchTimeOut, Item.PassWord, SearchParam.SearchKey);
                                                    if (!string.IsNullOrWhiteSpace(enContentDto.Response) && !enContentDto.Response.ToLower().Contains("\"error\""))
                                                    {
                                                        var enContentResult = JsonConvert.DeserializeObject<PropertyInfoType>(enContentDto.Response);
                                                        
                                                        if (enContentResult != null)
                                                        {
                                                            Dictionary<string, string>? names = null;
                                                            Dictionary<string, string>? cats = null;
                                                            if (enContentResult.RoomTypes != null)
                                                            {
                                                                names = enContentResult.RoomTypes
                                                                    .Where(rt => !string.IsNullOrWhiteSpace(rt.Id) && !string.IsNullOrWhiteSpace(rt.Name))
                                                                    .ToDictionary(rt => rt.Id, rt => rt.Name, StringComparer.OrdinalIgnoreCase);
                                                                cats = enContentResult.RoomTypes
                                                                    .Where(rt => !string.IsNullOrWhiteSpace(rt.Id) && !string.IsNullOrWhiteSpace(rt.CategoryName))
                                                                    .ToDictionary(rt => rt.Id, rt => rt.CategoryName, StringComparer.OrdinalIgnoreCase);
                                                            }
                                                            contentData = (names, cats, enContentResult.Description);
                                                        }
                                                    }
                                                }
                                                _cache.Set(contentCacheKey, contentData, TimeSpan.FromHours(24));
                                            }
                                            roomTypeEnNames = contentData.Names;
                                            roomTypeCategoryNames = contentData.CategoryNames;
                                            hotelDescFromContent = contentData.Description;
                                            if (!string.IsNullOrWhiteSpace(hotelDescFromContent))
                                                drXmlHotel["Description"] = hotelDescFromContent;
                                            for (int j = 0; j < roomsForHotel.Count; j++)
                                            {
                                                var roomStay = roomsForHotel[j];
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
                                                FinalPrice = roomStay.total.priceBeforeTax;
                                                double roomLevelTotal = FinalPrice * Factor;
                                                double companyBuyingPrice = roomLevelTotal;
                                                if (roomStay.total.taxAmount > 0)
                                                    BookingConditions = $"+ {roomStay.total.taxAmount.ToString("F2", CultureInfo.InvariantCulture)} {roomCurrency} taxes paid at hotel";
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
                                                _markup.ProcessHotelRoom_WithMarkup(Factor, roomCurrency, false, true, SearchParam.NoofNights, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), SearchParam.CheckInDate, SearchParam.CheckOutDate, ref roomLevelTotal, ref drHotelRoom, isPerce, AgentPercentage, AgentMarkupAmount, commonSearch.SubAgentMarkup, commonSearch.B2CMarkup, commonSearch.IsParentId);
                                                drHotelRoom["SupplierName"] = Item.Supplier;
                                                drHotelRoom["BoardTypeID"] = roomStay.mealPlanCode;
                                                string enRoomName = roomTypeEnNames != null && roomTypeEnNames.TryGetValue(roomStay.roomType?.id ?? "", out string? enN) ? enN : null;
                                                drHotelRoom["RoomTypeName"] = (!string.IsNullOrWhiteSpace(enRoomName) ? enRoomName : (roomStay.fullPlacementsName ?? string.Empty)).Replace("-", "_");
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
                                                    roomStay.cancellationPolicy?.penaltyAmount ?? companyBuyingPrice,
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
                                                        Essen);
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
                                                            Essen);
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
                    else
                    {
                        Item.SupplierResponseError = ResponseData;
                        Item.ResponseStatus = 404;
                        Item.ResponseType = "NoCount";
                        return options;
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

                var childAgesList = new List<int>();
                if (SearchPara.Rooms != null)
                {
                    foreach (var room in SearchPara.Rooms)
                    {
                        if (room.Child1Age > 0) childAgesList.Add(room.Child1Age);
                        if (room.Child2Age > 0) childAgesList.Add(room.Child2Age);
                    }
                }
                int totalAdults = SearchPara.Rooms?.Sum(r => Math.Max(r.NoofAdults, 1)) ?? 1;

                CancellationPolicy? matchedPolicy = null;
                string matchedEssen = "";
                string matchedCurrency = expectedCurrency;

                // STEP 1: POST /api/search/v1/properties/room-stays/search
                var searchCriteria = new SimpleSearchCriteria
                {
                    PropertyIds = new List<string> { hotelcode },
                    Adults = totalAdults,
                    ChildAges = childAgesList.Count > 0 ? childAgesList : null,
                    ArrivalDate = ParseSearchDate(SearchPara.CheckInDate),
                    DepartureDate = ParseSearchDate(SearchPara.CheckOutDate)
                };
                string aggregationRQ = JsonConvert.SerializeObject(searchCriteria, GetExelyIsoJsonSettings());
                string aggregationUrl = $"{baseUrl.TrimEnd('/')}/{(_supplierConfig.Exely.SearchRoomStays ?? string.Empty).TrimStart('/')}";

                _commonMethod.ThirdPartyGetPolicyHistory(Convert.ToString(ownsystemid), Convert.ToString(ownsystemid), aggregationRQ, "-", "Before Exely Aggregation Search", Supplier, "Request", "-", "NomadsB2B");
                ResponseDto aggregationDto = await ExelyResponseNew(aggregationRQ, aggregationUrl, searchTimeout, password, SearchKey);
                _commonMethod.ThirdPartyGetPolicyHistory(Convert.ToString(ownsystemid), Convert.ToString(ownsystemid), aggregationRQ, aggregationDto.Response, "After Exely Aggregation Search", Supplier, "Response", "-", "NomadsB2B");

                if (!string.IsNullOrWhiteSpace(aggregationDto.Response) && !aggregationDto.Response.ToLower().Contains("error"))
                {
                    var aggregationResult = JsonConvert.DeserializeObject<AggregationSearchByPropertiesResponse>(aggregationDto.Response);
                    var matchedShortStay = aggregationResult?.roomStays?.FirstOrDefault(rs =>
                        (string.IsNullOrWhiteSpace(expectedRoomType) || string.Equals(rs.roomType?.id, expectedRoomType, StringComparison.OrdinalIgnoreCase)) &&
                        (string.IsNullOrWhiteSpace(expectedRatePlan) || string.Equals(rs.ratePlan?.id, expectedRatePlan, StringComparison.OrdinalIgnoreCase)));

                    if (matchedShortStay != null)
                    {
                        matchedPolicy = matchedShortStay.cancellationPolicy;
                        matchedCurrency = string.IsNullOrWhiteSpace(matchedShortStay.currencyCode) ? expectedCurrency : matchedShortStay.currencyCode;
                        if (!string.IsNullOrWhiteSpace(matchedShortStay.mealPlanCode))
                            matchedEssen = "Meal Plan: " + matchedShortStay.mealPlanCode;
                        if (matchedShortStay.includedServices?.Count > 0)
                        {
                            string svcs = string.Join(", ", matchedShortStay.includedServices.Select(s => s.id + (string.IsNullOrWhiteSpace(s.mealPlanCode) ? "" : "(" + s.mealPlanCode + ")")));
                            matchedEssen = string.IsNullOrWhiteSpace(matchedEssen) ? svcs : matchedEssen + "</br>" + svcs;
                        }
                        if (!string.IsNullOrWhiteSpace(matchedShortStay.bookingFormLink))
                            matchedEssen = string.IsNullOrWhiteSpace(matchedEssen) ? matchedShortStay.bookingFormLink : matchedEssen + "</br>" + matchedShortStay.bookingFormLink;
                    }
                }

                // STEP 2: GET /api/search/v1/properties/{propertyId}/room-stays (fallback)
                if (matchedPolicy == null)
                {
                    SearchByPropertyResponse? propertyRoomStays = await GetRoomStaysByPropertyAsync(hotelcode, baseUrl, password, searchTimeout, SearchPara, SearchKey);
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
                    }
                }

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
                    #region Request
                    // RECHECK: GET /api/content/v1/properties/{propertyId}
                    string propertyTemplate = (_supplierConfig.Exely.PropertyContentByProperty ?? string.Empty).TrimStart('/');
                    string propertyUrl = $"{baseUrl.TrimEnd('/')}/{propertyTemplate.Replace("{propertyId}", hotelId)}?languageCode=en";
                    requestData = propertyUrl;
                    string recheckCheckIn = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["CheckInDate"]) ?? string.Empty;
                    string recheckCheckOut = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["CheckOutDate"]) ?? string.Empty;
                    string recheckRoomTypeId = string.Empty;
                    int recheckAdults = 1;
                    if (dsCartDetails.Tables.Count > 1 && dsCartDetails.Tables[1].DefaultView.Count > j)
                    {
                        recheckAdults = Math.Max(1, Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[j]["NoOfAdults"]));
                        recheckRoomTypeId = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomTypeID"]) ?? string.Empty;
                    }
                    string[] recheckRtParts = recheckRoomTypeId.Split("$$$");
                    string recheckExpectedRoomType = recheckRtParts.Length > 0 ? recheckRtParts[0] : "";
                    string recheckExpectedRatePlan = recheckRtParts.Length > 1 ? recheckRtParts[1] : "";
                    string recheckArrival = ParseSearchDate(recheckCheckIn).ToString("yyyy-MM-dd");
                    string recheckDeparture = ParseSearchDate(recheckCheckOut).ToString("yyyy-MM-dd");
                    string recheckTemplate = (_supplierConfig.Exely.GetRoomStaysByProperty ?? string.Empty).TrimStart('/');
                    string recheckUrl = $"{baseUrl.TrimEnd('/')}/{recheckTemplate.Replace("{propertyId}", hotelId)}?arrivalDate={recheckArrival}&departureDate={recheckDeparture}&adults={recheckAdults}";
                    requestData = recheckUrl;

                    _commonMethod.ThirdPartyBookingHistory(UniqueNo, AgentUniqueNo, requestData, "-", "Before Exely Recheck", supplierName, "response confirm", "-", "NomadsB2B", Searchkey);
                    #endregion Request

                    #region Response
                    var responseDto = await ExelyGetResponseAsync(recheckUrl, bookingTimeout, password, Searchkey);
                    responseData = responseDto.Response;

                    _commonMethod.ThirdPartyBookingHistory(UniqueNo, AgentUniqueNo, requestData, responseData + "|ERR:" + responseDto.Error, "After Exely Recheck", supplierName, "response confirm", "-", "NomadsB2B", Searchkey);

                    priceCheckCMDto.IsError = false;
                    priceCheckCMDto.Flag = false;
                    priceCheckCMDto.ItemPrice = itempriceY > 0 ? itempriceY : CompanyBuyingPriceA;

                    if (!string.IsNullOrWhiteSpace(responseData) && !responseData.ToLower().Contains("\"error\""))
                    {
                        var recheckResult = JsonConvert.DeserializeObject<SearchByPropertyResponse>(responseData);
                        var matchedRecheckStay = recheckResult?.roomStays?.FirstOrDefault(rs =>
                            (string.IsNullOrWhiteSpace(recheckExpectedRoomType) || string.Equals(rs.roomType?.id, recheckExpectedRoomType, StringComparison.OrdinalIgnoreCase)) &&
                            (string.IsNullOrWhiteSpace(recheckExpectedRatePlan) || string.Equals(rs.ratePlan?.id, recheckExpectedRatePlan, StringComparison.OrdinalIgnoreCase)));

                        if (matchedRecheckStay != null)
                        {
                            double livePrice = matchedRecheckStay.total?.priceBeforeTax ?? 0;
                            double storedPrice = itempriceY > 0 ? itempriceY : CompanyBuyingPriceA;
                            if (livePrice > 0 && Math.Abs(livePrice - storedPrice) > 0.01)
                            {
                                priceCheckCMDto.Flag = true;
                                priceCheckCMDto.ItemPrice = livePrice;
                            }
                        }
                    }
                    #endregion Response
                    #endregion Recheck
                }
                else
                {
                    #region Prebook
                    #region Request
                    // PREBOOK: POST /v1/bookings/verify
                    string verifyCheckIn = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["CheckInDate"]) ?? string.Empty;
                    string verifyCheckOut = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["CheckOutDate"]) ?? string.Empty;
                    int verifyAdults = 1;
                    int verifyChildren = 0;
                    string verifyRoomTypeId = string.Empty;
                    if (dsCartDetails.Tables.Count > 1 && dsCartDetails.Tables[1].DefaultView.Count > j)
                    {
                        verifyAdults = Math.Max(1, Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[j]["NoOfAdults"]));
                        verifyChildren = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[j]["NoOfChild"]);
                        verifyRoomTypeId = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomTypeID"]) ?? string.Empty;
                    }
                    string[] rtParts = verifyRoomTypeId.Split("$$$");
                    string verifyChecksum = rtParts.Length > 6 ? rtParts[6] : "";
                    string verifyNationality = rtParts.Length > 5 && !string.IsNullOrWhiteSpace(rtParts[5])
                        ? rtParts[5]
                        : (Convert.ToString(dsCartDetails.Tables[0].Rows[i]["NationalityName"]) ?? string.Empty);

                    string verifyRQ = Get_Exely_VerifyBookingRQ(verifyAdults, verifyChildren, hotelId, verifyCheckIn, verifyCheckOut, verifyRoomTypeId, verifyNationality, verifyChecksum);
                    string verifyUrlPath = (_supplierConfig.Exely.VerifyBooking ?? string.Empty).TrimStart('/');
                    string verifyUrl = $"{baseUrl.TrimEnd('/')}/{verifyUrlPath}";
                    requestData = verifyRQ;

                    _commonMethod.ThirdPartyBookingHistory(UniqueNo, AgentUniqueNo, requestData, "-", "Before Exely PreBook Verify", supplierName, "response confirm", "-", "NomadsB2B", Searchkey);
                    #endregion Request

                    #region Response
                    var responseDto = await ExelyResponseNew(verifyRQ, verifyUrl, bookingTimeout, password, Searchkey);
                    responseData = responseDto.Response;

                    _commonMethod.ThirdPartyBookingHistory(UniqueNo, AgentUniqueNo, requestData, responseData + "|ERR:" + responseDto.Error, "After Exely PreBook Verify", supplierName, "response confirm", "-", "NomadsB2B", Searchkey);

                    priceCheckCMDto.IsError = false;
                    priceCheckCMDto.Flag = false;
                    priceCheckCMDto.ItemPrice = itempriceY > 0 ? itempriceY : CompanyBuyingPriceA;

                    if (!string.IsNullOrWhiteSpace(responseData) && !responseData.ToLower().Contains("\"error\""))
                    {
                        var verifyResult = JsonConvert.DeserializeObject<VerifyBookingResult>(responseData);
                        if (verifyResult?.alternativeBooking != null)
                        {
                            // Condition change: supplier offered alternative booking with updated terms
                            double newPrice = verifyResult.alternativeBooking.total?.priceBeforeTax ?? 0;
                            if (newPrice > 0 && Math.Abs(newPrice - (itempriceY > 0 ? itempriceY : CompanyBuyingPriceA)) > 0.01)
                            {
                                priceCheckCMDto.Flag = true;
                                priceCheckCMDto.ItemPrice = newPrice;
                            }
                        }
                    }
                    #endregion Response
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

                List<int> childAges = new List<int>();
                for (int c = 0; c < noofchild; c++) childAges.Add(8);

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

                    List<int> childAges = new List<int>();
                    for (int c = 0; c < room.NoOfChild; c++) childAges.Add(8);

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
                List<int> childAges = new List<int>();
                for (int c = 0; c < noOfChild; c++) childAges.Add(8);
                var placements = BuildBookingPlacements(storedPlacementCode,noOfAdults, noOfChild);

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
                                checksum = checksum,
                                services = new List<RoomStayServiceRq>(),
                                extraStay = new ExtraStayRequest(),
                                discounts = new List<BookingDiscountRq>()
                            }
                        },
                        services = new List<BookingServiceRq>(),
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
                        prepayment = new Prepayment { paymentType = "CreditCard", prepaidSum = 0, remark = "" },
                        bookingComments = new List<string>(),
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

                List<int> childAges = new List<int>();
                for (int c = 0; c < noOfChild; c++) childAges.Add(8);
                var placements = BuildBookingPlacements(storedPlacementCode,noOfAdults, noOfChild);

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
                        //prepayment = new Prepayment { paymentType = "CreditCard", prepaidSum = 0, remark = "" },
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

        public async Task<string> Get_Exely_EInfo(string hotelcode, DataTable dtSuplierMarkup, DataTable dtSearchConfig, string provider, string searchKey, string roomTypeId)
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

                string contentTemplate = (_supplierConfig.Exely.PropertyContentByProperty ?? string.Empty).TrimStart('/');
                string contentUrl = $"{baseUrl.TrimEnd('/')}/{contentTemplate.Replace("{propertyId}", hotelcode)}?languageCode=en";

                ResponseDto responseDto = await ExelyGetResponseAsync(contentUrl, searchTimeout, password, searchKey);
                string responseData = responseDto.Response;

                _commonMethod.ThirdPartyGetPolicyHistory(hotelcode, hotelcode, contentUrl, responseData, "After Exely EInfo", provider, "Response", "-", "NomadsB2B");

                if (string.IsNullOrWhiteSpace(responseData) || responseData.ToLower().Contains("\"error\""))
                    return essential;

                var result = JsonConvert.DeserializeObject<PropertyInfoType>(responseData);
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
                if (result.Policy != null)
                {
                    if (!string.IsNullOrWhiteSpace(result.Policy.CheckInTime))
                        parts.Add("CheckIn: " + result.Policy.CheckInTime);
                    if (!string.IsNullOrWhiteSpace(result.Policy.CheckOutTime))
                        parts.Add("CheckOut: " + result.Policy.CheckOutTime);
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
                        .FirstOrDefault(x => x.Id == roomTypeId);

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
                essential = string.Join(" | ", parts).Replace("<br>", " ").Trim();
            }
            catch (Exception ex)
            {
                _ = _exception.Error(ex, searchKey);
            }
            return essential;
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
                ResponseDto responseDto = await ExelyGetResponseAsync(url, searchTimeout, password, searchKey);

                _commonMethod.ThirdPartyGetPolicyHistory(propertyId, propertyId, url, responseDto.Response, "After Exely RoomStaysByProperty", "Exely", "Response", "-", "NomadsB2B");

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

                ResponseDto responseDto = await ExelyGetResponseAsync(cancellationRulesUrl, searchTimeout, password, searchKey);
                string responseData = responseDto.Response;

                _commonMethod.ThirdPartyGetPolicyHistory(propertyId, propertyId, cancellationRulesUrl, responseData, "After Exely cancellation rules", "Exely", "Response", "-", "NomadsB2B");

                if (string.IsNullOrWhiteSpace(responseData) || responseData.ToLower().Contains("\"error\""))
                    return null;

                return JsonConvert.DeserializeObject<CancellationRulesResponse>(responseData);
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
            double penaltyAmount = Math.Max(0, cancellationPolicy.penaltyAmount ?? fallbackPenaltyAmount);
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

        private void AddExelyCancellationRows(DataTable dtHotelCancellation, IEnumerable<ExelyCancellationSegment> segments, string supplier, string ownsystemid, int roomno, string roomtypeid, string optionCode, HotelSearchParameter searchPara, bool isPerce, double b2cMarkup, double agentPercentage, double agentMarkupAmount, int isParentId, double subAgentMarkup, string essen)
        {
            foreach (ExelyCancellationSegment segment in segments.OrderBy(item => item.FromDate))
            {
                double currencyFactor = GetExelyCurrencyFactor(segment.Currency);
                double vatOnPolicy = 0;
                double penaltyConverted = segment.PenaltyAmount * currencyFactor;
                double mainAgentCharge = penaltyConverted;
                double companyChargeAmount = penaltyConverted;
                double markedUpCharge = penaltyConverted > 0
                    ? _markup.ProcessHotelCancelationPrice_WithMarkup(ref mainAgentCharge, searchPara.NoofNights, searchPara.CityID, Convert.ToString(searchPara.CountryID), penaltyConverted, isPerce, b2cMarkup, agentPercentage, agentMarkupAmount, isParentId, subAgentMarkup, ref vatOnPolicy)
                    : 0;

                dtHotelCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(
                    ref companyChargeAmount,
                    dtHotelCancellation.NewRow(),
                    currencyFactor,
                    segment.Currency,
                    supplier,
                    ownsystemid,
                    Convert.ToString(roomno),
                    roomtypeid,
                    "",
                    optionCode,
                    segment.FromDate,
                    segment.ToDate,
                    markedUpCharge,
                    markedUpCharge,
                    "",
                    vatOnPolicy,
                    "",
                    segment.IsWithinCancellationDeadline ? "YES" : "NO",
                    "",
                    essen,
                    "",
                    ""));
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
