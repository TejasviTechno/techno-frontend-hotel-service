using System.Collections.Concurrent;
using System.Data;
using System.Net;
using System.Text;
using Microsoft.Extensions.Options;
using XConnect.CommonUtilities;
using XConnect.CommonUtilities.Interface;
using XConnect.ErrorLogHandler.Interface;
using XConnect.Markup.Interface;
using XConnect.Models;
using XConnect.Models.Hotel;
using XConnect.Supplier.Yalago.Interface;
using ZstdSharp.Unsafe;
using static Azure.Core.HttpHeader;
using static XConnect.Models.ErrorLog;
using Options = XConnect.Models.Options;



namespace XConnect.Supplier.Yalago
{
    public class YalagoRepo : IYalago
    {
        private readonly ICommonMethod _commonMethod;
        private readonly IException _exception;
        private readonly IMarkup _markup;
        private readonly ISupplierRes _supplierRes;
        private readonly KeySettings _keySettings;

        public YalagoRepo(ICommonMethod commonMethod, IException exception, IMarkup markup, ISupplierRes supplierRes, IOptions<KeySettings> keySettings)
        {
            _commonMethod = commonMethod;
            _exception = exception;
            _markup = markup;
            _supplierRes = supplierRes;
            _keySettings = keySettings.Value;
        }

        public void GetYalagoRequestOnly_Async(DataRow[] commonCode, List<StarRating> listRating, HotelSearchParameter SearchPara, List<SupplierDetails> lstYalagoDetails, List<SupplierSearchConfig> lstYalagoSearchConfig, DataTable dtPreferredDataHotel, ref ConcurrentBag<HotelX> AllTask, int Hit, TokenData tokenData, DataSet dtXMLCity, DataTable dtROE)
        {
            try
            {
                if (lstYalagoDetails != null && lstYalagoDetails.Count > 0)
                {
                    List<StarRating> lstRating = new List<StarRating>();
                    lstRating = _commonMethod.GetStarRatingStringList_xtg(SearchPara);

                    List<HotelCodeByCity> DtHotelDAta = HtmlExtensions.ToList<HotelCodeByCity>(commonCode.CopyToDataTable());
                    int ratingcount = lstRating.Count;
                    int hotelcount = DtHotelDAta.Count;

                    foreach (SupplierDetails YalagoDetails in lstYalagoDetails)
                    {
                        try
                        {
                            int SupplierID = YalagoDetails.SupplierID;
                            string Supplier = YalagoDetails.SupplierName;

                            if (commonCode != null)
                            {
                                if (lstYalagoSearchConfig.Count > 0)
                                {
                                    List<SupplierSearchConfig> YalagoSearchConfig = lstYalagoSearchConfig
                                    .Where(x => x != null && x.SupplierID == SupplierID)
                                    .ToList();

                                    if (YalagoSearchConfig != null && YalagoSearchConfig.Count > 0)
                                    {
                                        DataForSearch dataForSearch = _commonMethod.GetXmlDataForSearchTechwise(SearchPara.Nationality, dtXMLCity, Supplier, YalagoDetails.TechName);

                                        if (dataForSearch.XmlNationality != "")
                                        {
                                            string Ids = string.Empty;
                                            string XmlBase = "";
                                            _commonMethod.GetPrefferedHotelCodes(ref Ids, ref XmlBase, ref YalagoSearchConfig, Supplier, dtPreferredDataHotel, SearchPara.IsSearchFor);

                                            List<HotelCodeByCity> filter = new List<HotelCodeByCity>();
                                            filter = _commonMethod.FilterWithOutCity_Code(ratingcount, hotelcount, DtHotelDAta, lstRating, Supplier);

                                            if (filter.Count > 0)
                                            {
                                                string Currency = YalagoDetails.CurrencyShortName;
                                                double YalagoCurrencyFactor = 1;
                                                if (Currency.ToLower() != tokenData.SiteCurrency.ToLower())
                                                {
                                                    YalagoCurrencyFactor = _commonMethod.GetROEByCurrency(Currency, dtROE);
                                                }

                                                for (int i = 0; i < YalagoSearchConfig.Count; i++)
                                                {
                                                    bool DebugMode = (Convert.ToInt32(YalagoSearchConfig[i].DebugMode) == 0 ? false : true);
                                                    //string WebSiteUrl = Convert.ToString(YalagoDetails.WebSiteURL);
                                                    string UserId = Convert.ToString(YalagoDetails.UserID);
                                                    string UserName = Convert.ToString(YalagoDetails.UserName);
                                                    string Password = Convert.ToString(YalagoDetails.Password);
                                                    // List<HotelCodeByCity> DtHotelData = FilterFinalList(CommonCode, listRating);


                                                    if (Convert.ToInt32(YalagoSearchConfig[i].CityID) == 0 || Convert.ToInt32(YalagoSearchConfig[i].CityID) == SearchPara.CityID)
                                                    {


                                                        int NoOfRecords = Convert.ToInt32(YalagoSearchConfig[i].Noofrecords);
                                                        double count = Convert.ToDouble(filter.Count);
                                                        int NoOfDiv = Convert.ToInt32(Math.Ceiling(count / Convert.ToDouble(NoOfRecords)));
                                                        List<List<XMLID_Search>> lstxml = _commonMethod.GetXMLIDString(NoOfDiv, NoOfRecords, count, filter);

                                                        for (int a = 0; a < NoOfDiv; a++)
                                                        {
                                                            string IdsForSearch = Convert.ToString(_commonMethod.GetHotelCodeByCOmma(lstxml[a]));

                                                            HotelX XHotel = new HotelX();
                                                            XHotel.Supplier = Supplier;
                                                            XHotel.DebugeMode = DebugMode;
                                                            XHotel.SearchTimeOut = Convert.ToInt32(YalagoSearchConfig[0].HotelSearchTimeOut);
                                                            //if (Hit > 0)
                                                            //{
                                                            XHotel.ProcessTimeout = Convert.ToInt32(YalagoSearchConfig[0].HotelSearchTimeOut2);
                                                            //}
                                                            if (_keySettings.SupplierProxy == "0")
                                                                XHotel.URL = Convert.ToString(YalagoDetails.WebSiteURL) + YalagoInfo.availability;
                                                            else
                                                                XHotel.URL = Convert.ToString(YalagoDetails.WebSiteURLforAPI) + YalagoInfo.availability;
                                                            XHotel.UserId = UserId;
                                                            XHotel.UserName = UserName;
                                                            XHotel.PassWord = Password;
                                                            XHotel.CurrencyCode = Currency; // XMLHotelBookingCommon.YalagoCurrency;
                                                            XHotel.CurrencyFactor = YalagoCurrencyFactor;
                                                            XHotel.XmlCity = dataForSearch.xmlCity;
                                                            XHotel.XmlCountry = dataForSearch.xmlCountry;
                                                            XHotel.XmlNationality = dataForSearch.XmlNationality;
                                                            XHotel.AvailableToken = "";
                                                            #region "CoomonCode"
                                                            bool IsHitSeperate = _commonMethod.IsSuppliersWithSeperateRoomRquest(XHotel.Supplier);
                                                            int NoOfRoomsSearch = 1;
                                                            if (IsHitSeperate)
                                                                NoOfRoomsSearch = SearchPara.Rooms.Count;
                                                            List<RQRS> RQRS = new List<RQRS>();
                                                            XHotel.IsSeperate = IsHitSeperate;
                                                            for (int rr = 0; rr < NoOfRoomsSearch; rr++)
                                                            {
                                                                RQRS objrq = new RQRS();
                                                                List<RoomDetails> serchRooms = new List<RoomDetails>();
                                                                serchRooms.Add(SearchPara.Rooms[rr]);
                                                                objrq.Request = (XMLYalagoRequest_Async(SearchPara, XHotel.SearchTimeOut, YalagoCurrencyFactor, DebugMode, dataForSearch.xmlCity, dataForSearch.XmlNationality, dataForSearch.xmlCountry, XHotel.UserId, XHotel.UserName, XHotel.PassWord, XHotel.CurrencyCode, IdsForSearch));
                                                                objrq.Response = ""; RQRS.Add(objrq);
                                                            }
                                                            XHotel.RQRS = RQRS;
                                                            XHotel.TechName = YalagoDetails.TechName;
                                                            XHotel.ReturnRooms = YalagoDetails.ReturnRooms;
                                                            #endregion
                                                            AllTask.Add(XHotel);
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


        public string XMLYalagoRequest_Async(HotelSearchParameter SearchPara, Int32 SearchTimeOut, double CurrencyFactorYalago, bool DebugMode, string XmlCity, string XmlNationality, string XmlCountry, string UserId, string UserName, string Password, string CurrencyCode, string HotelCode)
        {
            try
            {
                #region "PriceOnly Request"

                string YalagoPriceOnly = Get_Yalago_AvailabilityReq(SearchPara, XmlCity, XmlNationality, XmlCountry, CurrencyFactorYalago, UserId, UserName, Password, CurrencyCode, HotelCode);
                return YalagoPriceOnly;

                #endregion
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
                return "";
            }
        }

        public string Get_Yalago_AvailabilityReq(HotelSearchParameter SearchPara, string XmlCity, string XmlNationality, string XmlCountry, double CurrencyFactorYalago, string UserId, string UserName, string Password, string CurrencyCode, string HotelCode)
        {
            try
            {
                #region "Price Only"
                string CheckIn = SearchPara.CheckInDate;
                CheckIn = CheckIn.Substring(6, 4) + "-" + CheckIn.Substring(3, 2) + "-" + CheckIn.Substring(0, 2);
                string CheckOut = SearchPara.CheckOutDate;
                CheckOut = CheckOut.Substring(6, 4) + "-" + CheckOut.Substring(3, 2) + "-" + CheckOut.Substring(0, 2);

                Yalago.SearchRequest SR = new Yalago.SearchRequest();
                SR.CheckInDate = CheckIn;
                SR.CheckOutDate = CheckOut;

                List<object> Est = new List<object>();
                string[] Code = HotelCode.Split(',');
                for (int k = 0; k < Code.Length; k++)
                {
                    object Hid = new object();
                    Hid = Code[k];
                    Est.Add(Hid);
                }
                SR.EstablishmentIds = Est;
                SR.SourceMarket = XmlNationality;

                List<Yalago.Room> RoomMs = new List<Yalago.Room>();
                for (int i = 0; i < SearchPara.Rooms.Count; i++)
                {
                    Yalago.Room RM = new Yalago.Room();
                    RM.Adults = SearchPara.Rooms[i].NoofAdults;
                    int TotalChild = SearchPara.Rooms[i].NoofChild;
                    if (TotalChild >= 1)
                    {
                        List<object> ChildAges = new List<object>();
                        for (int j = 0; j < TotalChild; j++)
                        {
                            object Childage = new object();
                            if (j == 0)
                            {
                                Childage = SearchPara.Rooms[i].Child1Age;
                            }
                            if (j == 1)
                            {
                                Childage = SearchPara.Rooms[i].Child2Age;
                            }

                            ChildAges.Add(Childage);
                        }
                        RM.ChildAges = ChildAges;
                    }
                    RoomMs.Add(RM);
                }

                SR.Rooms = RoomMs;
                SR.Culture = "en-gb";
                //SR.GetPackagePrice = true;
                //SR.GetPackagePrice = false; //due to ticket no: 13870
                SR.GetPackagePrice = true;// on 26-5-2023

                SR.GetTaxBreakdown = false;

                //var RootObjectString = new JavaScriptSerializer().Serialize(SR);
                var RootObjectString = Newtonsoft.Json.JsonConvert.SerializeObject(SR);
                return RootObjectString;
                #endregion

            }
            catch (Exception ex)
            {
                _exception.Error(ex);
                return "";
            }
        }

        public async Task<List<Options>> Yalago_HotelSearchWithRooms(HotelX Item, CommonSearch commonSearch, string ResponseData, HotelSearchParameter SearchParam, string XmlCity, string XmlCountry, string XmlNationality, string Currency, double CurrencyFactor, DataTable dtROE, string ProccessCutOffTime, Int32 processTimeout, string ResProccessTimerLogActive, string ResProccessTimeValueforSaveLog, string StoreSupplierResErrorLog)
        {
            System.Diagnostics.Stopwatch timer_room_Yalago = new System.Diagnostics.Stopwatch();
            timer_room_Yalago.Start();

            List<Options> options = new List<Options>();
            DataTable DtHotel = _commonMethod.GenerateDataTableNewV2();
            try
            {

                DataTable DtYalago = _commonMethod.GenerateDataTableNewV2();
                var result = Newtonsoft.Json.JsonConvert.DeserializeObject<Yalago.HotellistResponse>(ResponseData);
                double FinalMinPrice = 0;
                if (result == null)
                {
                    Item.ResponseStatus = 204;
                    Item.ResponseType = "NoCount";
                    return options;
                }
                if (result.Messages.Count > 0)
                {
                    Item.ResponseStatus = 404;
                    Item.ResponseType = "NoCount";
                    return options;
                }
                else
                {
                    if (result.Establishments.Count > 0)
                    {
                        //string SiteCurrency = Convert.ToString(ConfigurationManager.AppSettings["SiteCurrency"]);
                        for (int i = 0; i < result.Establishments.Count; i++)
                        {
                            Item.ResponseType = "Success";
                            //Check Time If High then return NoCount
                            if (ProccessCutOffTime == "1" && timer_room_Yalago.ElapsedMilliseconds > processTimeout)
                            {
                                timer_room_Yalago.Stop();
                                break;
                            }
                            //--end
                            string hotelCode = Convert.ToString(result.Establishments[i].EstablishmentId);
                            //for (int j = 0; j < result.Establishments[i].Rooms.Count; j++)
                            {
                                #region "When not Need Room"  
                                // add hotel code from price only and room code blank here
                                if (SearchParam.Filters.IsShowRooms == 0)
                                {
                                    #region "Hotel Code Block "

                                    DataRow drXmlHotel = DtHotel.NewRow();
                                    drXmlHotel["IsRecomondedHotel"] = false;
                                    drXmlHotel["XMLHotelCode"] = hotelCode;
                                    drXmlHotel["HotelFrontImage"] = "";
                                    drXmlHotel["HotelName"] = "";
                                    drXmlHotel["Description"] = "";
                                    drXmlHotel["EssentialInformation"] = "";
                                    drXmlHotel["OfficeID"] = 0;
                                    drXmlHotel["ContractID"] = 0;
                                    drXmlHotel["IsShowSupplier"] = false;
                                    drXmlHotel["HotelName"] = "";
                                    drXmlHotel["Rating"] = 0;
                                    string HotelStatus = string.Empty;
                                    drXmlHotel["XmlHotelCity"] = XmlCity;
                                    drXmlHotel["XMLCountry"] = XmlCountry;
                                    drXmlHotel["XMLCity"] = XmlCity;
                                    drXmlHotel["SupplierName"] = Item.Supplier;
                                    double FinalTotal = 0;
                                    for (int j = 0; j < result.Establishments[i].Rooms.Count; j++)
                                    {
                                        for (int k = 0; k < result.Establishments[i].Rooms[j].Boards.Count; k++)
                                        {
                                            double RoomRate = result.Establishments[i].Rooms[j].Boards[k].NetCost.Amount;
                                            if (FinalTotal < RoomRate)
                                            {
                                                FinalTotal = RoomRate;
                                            }
                                        }
                                    }

                                    drXmlHotel["SellingPrice"] = FinalTotal;
                                    drXmlHotel["FinalSellingPrice"] = FinalTotal;
                                    drXmlHotel["BookingStatus"] = HotelStatus;
                                    drXmlHotel["BookingStatus"] = "Available";
                                    drXmlHotel["SupplierCurrency"] = Currency;
                                    DtHotel.Rows.Add(drXmlHotel);

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
                                            decimal LowestPrice = Convert.ToDecimal(MinPrice).CalculatePrice(commonSearch.tokenData, commonSearch.advancedOptions, Currency);
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
                                    DataRow drXmlHotel = DtHotel.NewRow();
                                    drXmlHotel["IsRecomondedHotel"] = false;
                                    drXmlHotel["XMLHotelCode"] = hotelCode;
                                    drXmlHotel["HotelFrontImage"] = "";
                                    drXmlHotel["HotelName"] = "";
                                    drXmlHotel["Description"] = "";
                                    drXmlHotel["EssentialInformation"] = "";
                                    drXmlHotel["OfficeID"] = 0;
                                    drXmlHotel["ContractID"] = 0;
                                    drXmlHotel["IsShowSupplier"] = false;
                                    drXmlHotel["HotelName"] = "";
                                    drXmlHotel["Rating"] = 0;
                                    string HotelStatus = string.Empty;
                                    drXmlHotel["XmlHotelCity"] = XmlCity;
                                    drXmlHotel["XMLCountry"] = XmlCountry;
                                    drXmlHotel["XMLCity"] = XmlCity;
                                    drXmlHotel["SupplierName"] = Item.Supplier;
                                    double FinalTotal = 0;
                                    for (int j = 0; j < result.Establishments[i].Rooms.Count; j++)
                                    {
                                        for (int k = 0; k < result.Establishments[i].Rooms[j].Boards.Count; k++)
                                        {
                                            double RoomRate = result.Establishments[i].Rooms[j].Boards[k].NetCost.Amount;
                                            if (FinalTotal < RoomRate)
                                            {
                                                FinalTotal = RoomRate;
                                            }
                                        }
                                    }
                                    drXmlHotel["SellingPrice"] = FinalTotal;
                                    drXmlHotel["FinalSellingPrice"] = FinalTotal;
                                    drXmlHotel["BookingStatus"] = HotelStatus;
                                    drXmlHotel["BookingStatus"] = "Available";
                                    drXmlHotel["SupplierCurrency"] = Currency;
                                    DtHotel.Rows.Add(drXmlHotel);

                                    double Pricenew = 0;

                                    #region "GetRooms for this hotel"
                                    bool willAddOrNot = true;
                                    Int64 RaynaID = 0;
                                    Int64 GiataId = 0;
                                    int RatingHotel = 0;
                                    Int64 OwnsystemHotelId = 0;
                                    string Provider = Item.Supplier;
                                    double MinPrice = Pricenew;
                                    _markup.GetHotelDataCommon(ref MinPrice, ref willAddOrNot, ref OwnsystemHotelId, ref RaynaID, ref GiataId, ref RatingHotel, ref drXmlHotel, commonSearch.AgentId, commonSearch.TariffID, commonSearch.dsAgentDetails, commonSearch.supplierGiataMaster, SearchParam, commonSearch.dtCountryCityMarkup, commonSearch.B2CMarkup, commonSearch.IsParentId, commonSearch.SubAgentMarkup, commonSearch.SearchByHotel, commonSearch.HotelIDS);
                                    if (willAddOrNot)
                                    {
                                        DataTable dtHotelRoom = _commonMethod.GenerateRoomtypeDataTableNewV2();
                                        DataTable DtYalagoCancellation = _commonMethod.GenerateCancellationDataTableNewV2();
                                        DataTable DtRoomRateBreackUp = _commonMethod.GenerateoomRateBreackUpDataTableNewV2();

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

                                            int totalAdult = 0;
                                            int totalChild = 0;

                                            for (int b = 0; b < SearchParam.Rooms.Count; b++)
                                            {
                                                totalAdult = Convert.ToInt32(SearchParam.Rooms[b].NoofAdults) + totalAdult;
                                                totalChild = Convert.ToInt32(SearchParam.Rooms[b].NoofChild) + totalChild;
                                                maxOcupancy = maxOcupancy + SearchParam.Rooms[b].NoofChild + SearchParam.Rooms[b].NoofAdults;
                                            }

                                            #region Supplier Room Code block
                                            string currncy = "";
                                            double Factor = 1;
                                            string Meal = "";
                                            double FinalPrice = 0;
                                            for (int j = 0; j < result.Establishments[i].Rooms.Count; j++)
                                            {
                                                for (int k = 0; k < result.Establishments[i].Rooms[j].Boards.Count; k++)
                                                {
                                                    //if (k > CacheMaster.RoomLimit_10)
                                                    //    break;


                                                    if (commonSearch.AgentId == 4440 || commonSearch.AgentId == 4901)
                                                    {
                                                        if (k > CacheMaster.RoomLimit_20)
                                                            break;
                                                    }
                                                    else
                                                    {
                                                        if (k > CacheMaster.RoomLimit_10)
                                                            break;
                                                    }

                                                    string uniqno = Guid.NewGuid().ToString().Substring(0, 4);
                                                    string BookingConditions = Convert.ToString(result.Establishments[i].Rooms[j].Boards[k].BookingConditions);
                                                    string SpecialOfferText = Convert.ToString(result.Establishments[i].Rooms[j].Boards[k].SpecialOfferText);
                                                    if (result.Establishments[i].Rooms[j].Boards[k].IsPayAtHotel == false)
                                                    {
                                                        if (result.Establishments[i].Rooms[j].Boards[k].IsOnRequest == false)
                                                        {
                                                            int Count = result.Establishments[i].Rooms[j].Boards[k].RequestedRoomIndex;
                                                            Count = Count + 1;
                                                            if (k == 0)
                                                            {
                                                                Meal = result.Establishments[i].Rooms[j].Boards[k].Description;

                                                                {
                                                                    //if (result.Establishments[i].Rooms[j].Boards[k].NetCost == null)
                                                                    //  break;
                                                                    //FinalPrice = Convert.ToDouble(result.Establishments[i].Rooms[j].Boards[k].NetCost.Amount);
                                                                    FinalPrice = Convert.ToDouble(result.Establishments[i].Rooms[j].Boards[k].NetCost.Amount);
                                                                    currncy = Convert.ToString(result.Establishments[i].Rooms[j].Boards[k].NetCost.Currency);
                                                                    //if (currncy.ToLower() != SiteCurrency.ToLower())
                                                                    //{
                                                                    //break;
                                                                    Factor = _commonMethod.GetROEByCurrency(currncy, dtROE);
                                                                    //}
                                                                    //DataTable dtInrCurrency = Common.GetCountryCurrency(currncy);
                                                                    //Factor = Convert.ToDouble(dtInrCurrency.Rows[0]["AmtToExch"]);
                                                                }
                                                                //else //use net cost only Due to ticket#35124
                                                                //{
                                                                //    if (result.Establishments[i].Rooms[j].Boards[k].GrossCost == null)
                                                                //        break;
                                                                //    FinalPrice = Convert.ToDouble(result.Establishments[i].Rooms[j].Boards[k].GrossCost.Amount);
                                                                //    currncy = Convert.ToString(result.Establishments[i].Rooms[j].Boards[k].GrossCost.Currency);
                                                                //    if (currncy.ToLower() != SiteCurrency.ToLower())
                                                                //    {
                                                                //        break;
                                                                //        //Factor = Common.GetROEByCurrency(currncy, dtROE);
                                                                //    }

                                                                //}
                                                            }
                                                            else
                                                            {
                                                                //if (result.Establishments[i].Rooms[j].Boards[k].IsBindingPrice == false)
                                                                {
                                                                    //if (result.Establishments[i].Rooms[j].Boards[k].NetCost == null)
                                                                    //break;
                                                                    FinalPrice = Convert.ToDouble(result.Establishments[i].Rooms[j].Boards[k].NetCost.Amount);
                                                                    currncy = Convert.ToString(result.Establishments[i].Rooms[j].Boards[k].NetCost.Currency);
                                                                    //if (currncy.ToLower() != SiteCurrency.ToLower())
                                                                    //{
                                                                    //break;
                                                                    Factor = _commonMethod.GetROEByCurrency(currncy, dtROE);
                                                                    //}
                                                                }
                                                                //else
                                                                //{
                                                                //    if (result.Establishments[i].Rooms[j].Boards[k].GrossCost == null)
                                                                //        break;
                                                                //    FinalPrice = Convert.ToDouble(result.Establishments[i].Rooms[j].Boards[k].GrossCost.Amount);
                                                                //    currncy = Convert.ToString(result.Establishments[i].Rooms[j].Boards[k].GrossCost.Currency);
                                                                //    if (currncy.ToLower() != SiteCurrency.ToLower())
                                                                //    {
                                                                //        break;
                                                                //        //Factor = Common.GetROEByCurrency(currncy, dtROE);
                                                                //    }

                                                                //}
                                                            }
                                                            double itemprice = 0;
                                                            itemprice = FinalPrice * Factor;
                                                            if (Pricenew < itemprice)
                                                            {
                                                                Pricenew = itemprice;
                                                            }

                                                            int CutOffDays = XMLHotelBookingCommon.CutOffDays;
                                                            CutOffDays = _commonMethod.GetCutOffDays(commonSearch.dtSuplierMarkup, Item.Supplier);
                                                            bool IsBindingPrice = result.Establishments[i].Rooms[j].Boards[k].IsBindingPrice;//6th no in RoomtypeID

                                                            string RoomTypeId = result.Establishments[i].Rooms[j].Code + "$$$" + result.Establishments[i].Rooms[j].Boards[k].Code + "$$$" + result.Establishments[i].Rooms[j].Boards[k].NetCost.Amount + "$$$" + result.Establishments[i].Rooms[j].Boards[k].NetCost.Currency + "$$$" + uniqno + "$$$" + XmlNationality + "$$$" + IsBindingPrice;
                                                            //string RoomTypeId = result.Establishments[i].Rooms[j].Code + "$$$" + result.Establishments[i].Rooms[j].Boards[k].Code + "$$$" + result.Establishments[i].Rooms[j].Boards[k].NetCost.Amount + "$$$" + result.Establishments[i].Rooms[j].Boards[k].NetCost.Currency + "$$$" + uniqno + "$$$" + XmlNationality;

                                                            double companyBuyingPrice = itemprice; //070525

                                                            #region "Room"

                                                            DataRow drHotelRoom = dtHotelRoom.NewRow();
                                                            string roomWiseDetails = string.Empty;
                                                            drHotelRoom["OwnsystemHotelId"] = OwnsystemHotelId;
                                                            drHotelRoom["RoomTypeCode"] = result.Establishments[i].Rooms[j].Code;
                                                            drHotelRoom["BoardType"] = result.Establishments[i].Rooms[j].Boards[k].Description;
                                                            drHotelRoom["AdultsOccupancy"] = totalAdult;
                                                            drHotelRoom["ChildOccupancy"] = totalChild;
                                                            drHotelRoom["MealName"] = result.Establishments[i].Rooms[j].Boards[k].Description;
                                                            drHotelRoom["MealId"] = result.Establishments[i].Rooms[j].Boards[k].Description;
                                                            drHotelRoom["Type"] = "";
                                                            drHotelRoom["Board"] = result.Establishments[i].Rooms[j].Boards[k].Description;
                                                            drHotelRoom["BookingStatus"] = "Available";
                                                            drHotelRoom["UniqueNo"] = 0;
                                                            drHotelRoom["ContractID"] = 0;
                                                            drHotelRoom["RoomNo"] = 1;//RoomNo;// Count;
                                                            drHotelRoom["RoomTypeId"] = RoomTypeId;//result.Establishments[i].Rooms[j].Code + "$$$" + result.Establishments[i].Rooms[j].Boards[k].Code + "$$$" + result.Establishments[i].Rooms[j].Boards[k].NetCost.Amount + "$$$" + result.Establishments[i].Rooms[j].Boards[k].NetCost.Currency + "$$$" + uniqno + "$$$" + XmlNationality;
                                                            drHotelRoom["RoomSequenceNo"] = "";
                                                            if (SpecialOfferText != "")
                                                            {
                                                                drHotelRoom["OfferDescription"] = SpecialOfferText;
                                                                drHotelRoom["OfferCode"] = "offer";

                                                            }
                                                            else
                                                            {
                                                                drHotelRoom["OfferDescription"] = "";
                                                                drHotelRoom["OfferCode"] = "";

                                                            }
                                                            drHotelRoom["Characteristic"] = "";
                                                            if (BookingConditions != "")
                                                            {
                                                                drHotelRoom["EssentialInformation"] = "</br>" + BookingConditions;//"N/A";
                                                            }
                                                            #region Vat
                                                            _markup.ProcessHotelRoom_WithMarkup(Factor, Currency, false, false, SearchParam.NoofNights, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), SearchParam.CheckInDate, SearchParam.CheckOutDate, ref itemprice, ref drHotelRoom, isPerce, AgentPercentage, AgentMarkupAmount, commonSearch.SubAgentMarkup, commonSearch.B2CMarkup, commonSearch.IsParentId);
                                                            #endregion
                                                            drHotelRoom["SupplierName"] = Item.Supplier;
                                                            drHotelRoom["BoardTypeID"] = result.Establishments[i].Rooms[j].Boards[k].Code;
                                                            drHotelRoom["RoomTypeName"] = result.Establishments[i].Rooms[j].Description.Replace("-", "_");
                                                            dtHotelRoom.Rows.Add(drHotelRoom);

                                                            #endregion

                                                            #region Cancellation Policy
                                                            if (true) // as per B2b
                                                            {
                                                                string Essen = "";
                                                                if (result.Establishments[i].Rooms[j].Boards[k].NonRefundable == true)
                                                                {
                                                                    for (int L = 0; L < result.Establishments[i].Rooms[j].Boards[k].CancellationPolicy.CancellationCharges.Count; L++)
                                                                    {
                                                                        DateTime Fromdate = DateTime.Now.Date;
                                                                        DateTime Todate = Convert.ToDateTime(result.Establishments[i].Rooms[j].Boards[k].CancellationPolicy.CancellationCharges[L].ExpiryDate);
                                                                        string CurrencyCancellation = result.Establishments[i].Rooms[j].Boards[k].CancellationPolicy.CancellationCharges[L].Charge.Currency;

                                                                        //DataTable dtInrCurrency = Common.GetCountryCurrency(CurrencyCancellation);
                                                                        //double YalagoCurrencyFactor = Convert.ToDouble(dtInrCurrency.Rows[0]["AmtToExch"]);

                                                                        double YalagoCurrencyFactor = _commonMethod.GetROEByCurrency(CurrencyCancellation, dtROE);

                                                                        double cancellationChargeAmount = companyBuyingPrice;// Convert.ToDouble(result.Establishments[i].Rooms[j].Boards[k].CancellationPolicy.CancellationCharges[L].Charge.Amount) * YalagoCurrencyFactor;
                                                                        #region Vat
                                                                        double VatOnPolicy = 0;
                                                                        double MainAgentCharge = cancellationChargeAmount;
                                                                        double CompanyChargeAmount = cancellationChargeAmount;
                                                                        //cancellationChargeAmount = Companybuy;//Common.ProcessHotelCancelationPrice_WithMarkup(ref MainAgentCharge, SearchPara.NoofNights, SearchPara.CityID, SearchPara.CountryId, cancellationChargeAmount, isPerce, B2CMarkup, AgentPercentage, AgentMarkupAmount, IsParentId, SubAgentMarkup, ref VatOnPolicy);
                                                                        cancellationChargeAmount = _markup.ProcessHotelCancelationPrice_WithMarkup(ref MainAgentCharge, SearchParam.NoofNights, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), cancellationChargeAmount, isPerce, commonSearch.B2CMarkup, AgentPercentage, AgentMarkupAmount, commonSearch.IsParentId, commonSearch.SubAgentMarkup, ref VatOnPolicy);
                                                                        #endregion
                                                                        //dtHotelCancellation.Rows.Add(setCancellationPolicyRow(ref CompanyChargeAmount, dtHotelCancellation.NewRow(), 1, "USD", "Yalago", Convert.ToString(ownsystemid), roomno.ToString(), roomtypeid, "", Convert.ToString(OptionCode),  DateTime.ParseExact(SearchPara.CheckInDate, "dd/MM/yyyy", null),Convert.ToDateTime(Todate), MainAgentCharge, cancellationChargeAmount, "", 0, "", "", "", Essen, "", ""));

                                                                        DtYalagoCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref CompanyChargeAmount, DtYalagoCancellation.NewRow(), YalagoCurrencyFactor, CurrencyCancellation, "Yalago", Convert.ToString(OwnsystemHotelId), Convert.ToString(1),
                                                                        RoomTypeId, "", Convert.ToString(result.Establishments[i].Rooms[j].Boards[k].Description), DateTime.Now.Date, DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null), MainAgentCharge, cancellationChargeAmount, "", VatOnPolicy, "", "YES", "", Essen, "", ""));
                                                                        DtYalagoCancellation.AcceptChanges();
                                                                    }
                                                                }
                                                                else
                                                                {

                                                                    double cancellationChargeAmount = companyBuyingPrice;// Convert.ToDouble(result.Establishments[i].Rooms[j].Boards[k].CancellationPolicy.CancellationCharges[0].Charge.Amount);
                                                                    DateTime NowDate = DateTime.Now.Date;
                                                                    DateTime Todate = Convert.ToDateTime(result.Establishments[i].Rooms[j].Boards[k].CancellationPolicy.CancellationCharges[0].ExpiryDate);
                                                                    string CurrencyCancellation = result.Establishments[i].Rooms[j].Boards[k].CancellationPolicy.CancellationCharges[0].Charge.Currency;

                                                                    //DataTable dtInrCurrency = Common.GetCountryCurrency(CurrencyCancellation);
                                                                    //double YalagoCurrencyFactor = Convert.ToDouble(dtInrCurrency.Rows[0]["AmtToExch"]);

                                                                    double YalagoCurrencyFactor = _commonMethod.GetROEByCurrency(CurrencyCancellation, dtROE);

                                                                    #region Vat
                                                                    double VatOnPolicy = 0;
                                                                    double MainAgentCharge = cancellationChargeAmount;
                                                                    double CompanyChargeAmount = cancellationChargeAmount;
                                                                    double cancellationCharge = _markup.ProcessHotelCancelationPrice_WithMarkup(ref MainAgentCharge, SearchParam.NoofNights, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), cancellationChargeAmount, isPerce, commonSearch.B2CMarkup, AgentPercentage, AgentMarkupAmount, commonSearch.IsParentId, commonSearch.SubAgentMarkup, ref VatOnPolicy);
                                                                    //cancellationChargeAmount = Common.ProcessHotelCancelationPrice_WithMarkup(ref MainAgentCharge, SearchPara.NoofNights, SearchPara.CityID, SearchPara.CountryId, cancellationChargeAmount, isPerce, B2CMarkup, AgentPercentage, AgentMarkupAmount, IsParentId, SubAgentMarkup, ref VatOnPolicy);
                                                                    #endregion

                                                                    DateTime DeadlineDate = DateTime.Now;
                                                                    DateTime StartDate = DateTime.Now.Date;
                                                                    for (int p = 0; p < result.Establishments[i].Rooms[j].Boards[k].CancellationPolicy.CancellationCharges.Count; p++)
                                                                    {
                                                                        if (result.Establishments[i].Rooms[j].Boards[k].CancellationPolicy.CancellationCharges.Count > 1)
                                                                        {
                                                                            if (result.Establishments[i].Rooms[j].Boards[k].CancellationPolicy.CancellationCharges[p].Charge.Amount == 0)
                                                                            {
                                                                                DeadlineDate = Convert.ToDateTime(result.Establishments[i].Rooms[j].Boards[k].CancellationPolicy.CancellationCharges[p].ExpiryDate);
                                                                                DeadlineDate = DeadlineDate.AddDays(CutOffDays);
                                                                            }
                                                                        }
                                                                        else
                                                                        {
                                                                            DeadlineDate = Convert.ToDateTime(result.Establishments[i].Rooms[j].Boards[k].CancellationPolicy.CancellationCharges[p].ExpiryDate);
                                                                            DeadlineDate = DeadlineDate.AddDays(CutOffDays);
                                                                        }
                                                                    }

                                                                    // Add cutoff days in DeadlineDate
                                                                    //DeadlineDate.AddDays(-1).AddHours(-23).AddMinutes(-59).AddSeconds(-59)
                                                                    if (NowDate.Date < DeadlineDate.Date)
                                                                    {
                                                                        double Zero = 0;
                                                                        DtYalagoCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref Zero, DtYalagoCancellation.NewRow(), YalagoCurrencyFactor, CurrencyCancellation, Item.Supplier, Convert.ToString(OwnsystemHotelId), Convert.ToString(1),
                                                                    RoomTypeId, "", Convert.ToString(result.Establishments[i].Rooms[j].Boards[k].Description), DateTime.Now.Date, DeadlineDate.AddDays(-1).AddHours(00).AddMinutes(00).AddSeconds(00), 0, 0, "", VatOnPolicy, "", "NO", "", Essen, "", ""));
                                                                        DtYalagoCancellation.AcceptChanges();



                                                                        DtYalagoCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref CompanyChargeAmount, DtYalagoCancellation.NewRow(), YalagoCurrencyFactor, CurrencyCancellation, Item.Supplier, Convert.ToString(OwnsystemHotelId), Convert.ToString(1),
                                                                  RoomTypeId, "", Convert.ToString(result.Establishments[i].Rooms[j].Boards[k].Description), DeadlineDate, DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null), cancellationCharge, cancellationCharge, "", VatOnPolicy, "", "YES", "", Essen, "", ""));
                                                                        DtYalagoCancellation.AcceptChanges();

                                                                    }
                                                                    else
                                                                    {
                                                                        DtYalagoCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref CompanyChargeAmount, DtYalagoCancellation.NewRow(), YalagoCurrencyFactor, CurrencyCancellation, Item.Supplier, Convert.ToString(OwnsystemHotelId), Convert.ToString(1),
                                                                       RoomTypeId, "", Convert.ToString(result.Establishments[i].Rooms[j].Boards[k].Description), DateTime.Now.Date, DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null), cancellationCharge, cancellationCharge, "", VatOnPolicy, "", "YES", "", Essen, "", ""));
                                                                        DtYalagoCancellation.AcceptChanges();
                                                                    }
                                                                }


                                                            }
                                                            #endregion

                                                            //Count++;
                                                        }
                                                        else
                                                        {
                                                            continue;
                                                        }

                                                    }
                                                    else
                                                    {
                                                        continue;
                                                    }


                                                }
                                            }

                                            #endregion

                                            Options ops = (await _supplierRes.GetCommonRoomForSuppliers(GiataId, commonSearch.tokenData, commonSearch.advancedOptions, Pricenew, drXmlHotel, dtHotelRoom, DtYalagoCancellation, DtRoomRateBreackUp, SearchParam));
                                            if (ops != null)
                                                options.Add(ops);
                                        }
                                        #endregion
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
                    timer_room_Yalago.Stop();
                    Item.CodeProcessingTimeSupplier = timer_room_Yalago.ElapsedMilliseconds;
                    if (SearchParam != null && ResProccessTimerLogActive == "1" && timer_room_Yalago.ElapsedMilliseconds > Convert.ToInt32(ResProccessTimeValueforSaveLog))
                    {
                        
                        SearchParam.SupplierTime += ",|Finish:Yalago Process Response  " + "|Time :" + timer_room_Yalago.Elapsed.ToString();
                        _= Task.Run(() => _commonMethod.FireAway_SQL_ResPorcessTimeLog("", timer_room_Yalago.Elapsed.ToString(), Item.Supplier, SearchParam.SearchKey, 1, SearchParam.AgentID));
                    }
                }
                catch (Exception x)
                {
                    await _exception.Error(x);
                }
                #endregion

                if (Item.ResponseType == "NoCount" && StoreSupplierResErrorLog == "1")
                    _ = Task.Run(() => _commonMethod.XMLRequestResponseTrack_Error(Item.RQRS[0].Request, Item.RQRS[0].Response, Item.Supplier, timer_room_Yalago.Elapsed.ToString(), "", SearchParam.SearchKey));
            }

            return options;
        }

        public async Task<CancellationDto> Get_Yalago_CancellationPolicy(int roomno, string hotelcode, DataSet dsHotel, string ownsystemid, string roomtypeid, DataTable dtAgentDetail, DataTable dtSuplierMarkup, DataTable dtMarkupDetails, DataTable dtSearchConfig, HotelSearchParameter SearchPara, string OptionCode, int AgentId, double roomprice, double Companybuy,string SearchKey,string Supplier, string GiataId = "0", int hotelRating = 0)
        {
            string IsBooking = "Available";
            CancellationDto cancellationDto = new CancellationDto();

            DataTable dtHotelCancellation = _commonMethod.GenerateCancellationDataTableNewV2();
            int CutOffDays = XMLHotelBookingCommon.CutOffDays;
            CutOffDays = _commonMethod.GetCutOffDays(dtSuplierMarkup, Supplier);
            Boolean isPerce = true;
            double AgentCompanyPercentage = Convert.ToDouble(SearchPara.AgentCompanyPercentage);
            bool IsB2C = Convert.ToBoolean(Convert.ToByte(SearchPara.IsB2BOrB2C));
            double B2CMarkup = (IsB2C == false ? AgentCompanyPercentage : XMLHotelBookingCommon.B2CMarkup);
            double AgentPercentage = 0.0;
            double AgentMarkupAmount = 0.0;
            double SubAgentMarkup = 0;
            int IsParentId = 1;
            Boolean blnIsShowSupplier = false;
            string CheckinDate = SearchPara.CheckInDate;
            _markup.getAgetMarkupSetting(Supplier, GiataId, dtSuplierMarkup, dtMarkupDetails, ref AgentPercentage, ref AgentMarkupAmount, ref blnIsShowSupplier, ref isPerce, AgentId, hotelRating, SearchPara.CityID, Convert.ToString(SearchPara.CountryID), Convert.ToString(SearchPara.CheckInDate));
            try
            {
                //for (int c = 0; c < dsHotel.Tables[1].DefaultView.Count; c++)
                {
                    DataRow[] drSuplierMarkup = (dtSuplierMarkup.Select("SupplierName='"+ Supplier  + "'"));
                    string Password = Convert.ToString(drSuplierMarkup[0]["Password"]);
                    string YalagoURL = Convert.ToString(drSuplierMarkup[0]["WebSiteURL"]) + "hotels/details/get";
                    int SID = Convert.ToInt32(drSuplierMarkup[0]["SupplierID"]);
                    DataRow[] drSearchConfig = (dtSearchConfig.Select("SupplierID='" + SID + "'"));
                    int SearchTimeOut = Convert.ToInt32(drSearchConfig[0]["BookingConfirmationTimeout"]);
                    string CheckIn = SearchPara.CheckInDate;
                    CheckIn = CheckIn.Substring(6, 4) + "-" + CheckIn.Substring(3, 2) + "-" + CheckIn.Substring(0, 2);
                    string CheckOut = SearchPara.CheckOutDate;
                    CheckOut = CheckOut.Substring(6, 4) + "-" + CheckOut.Substring(3, 2) + "-" + CheckOut.Substring(0, 2);
                    int newroomno = roomno - 1;
                    string CancellationPolicyRQ = Get_Yalago_CancellationPolicyRQ(SearchPara, hotelcode, CheckIn, CheckOut, roomtypeid, newroomno);
                    //Common.ThirdPartyGetPolicyHistory(Convert.ToString(ownsystemid), Convert.ToString(ownsystemid), CancellationPolicyRQ, "", "Before Get Policy", "yalago", "Request", "-", "withinearthyalago");

                    string Error = "";
                    //string YalagoResponse = YalagoResponseNew(ref Error, CancellationPolicyRQ, YalagoURL, SearchTimeOut, Password);
                    ResponseDto responseDto = await YalagoResponseNew(CancellationPolicyRQ, YalagoURL, SearchTimeOut, Password);
                    string YalagoResponse = responseDto.Response;
                    Error = responseDto.Error;


                    _commonMethod.ThirdPartyGetPolicyHistory(Convert.ToString(ownsystemid), Convert.ToString(ownsystemid), CancellationPolicyRQ, Convert.ToString(YalagoResponse), "After Get Policy", Supplier, "Response", "-", "withinearth");
                    if (YalagoResponse == null || YalagoResponse == string.Empty || YalagoResponse.ToLower().Contains("error"))
                    {
                        cancellationDto.dsHotelData = dsHotel;
                        cancellationDto.ISBooking = "NotAvailable";
                        return cancellationDto;

                    }

                    var result = Newtonsoft.Json.JsonConvert.DeserializeObject<Policy.CancellationResponse>(YalagoResponse);
                    string Essential = "", Essen = "";
                    for (int eses = 0; eses < result.InfoItems.Count; eses++)
                    {
                        Essential = result.InfoItems[eses].Title;
                        Essential += ":- " + result.InfoItems[eses].Description;
                        Essen += "</br></br>" + Essential;
                    }
                    for (int i = 0; i < result.Establishment.Rooms.Count; i++)
                    {
                        for (int j = 0; j < result.Establishment.Rooms[i].Boards.Count; j++)
                        {
                            if (Essen == "")
                                Essen = result.Establishment.Rooms[i].Boards[j].BookingConditions;
                            else
                                Essen = Essen + " " + result.Establishment.Rooms[i].Boards[j].BookingConditions;
                            try
                            {
                                string localChargeessen = "";
                                for (int k = 0; k < result.Establishment.Rooms[i].Boards[j].LocalCharges.Count; k++)
                                {
                                    string Charge = result.Establishment.Rooms[i].Boards[j].LocalCharges[k].Amount.ToString();
                                    var result1 = Newtonsoft.Json.JsonConvert.DeserializeObject<localChargeAmount>(Charge);
                                    string Title = Convert.ToString(result.Establishment.Rooms[i].Boards[j].LocalCharges[k].Title);
                                    string Description = Convert.ToString(result.Establishment.Rooms[i].Boards[j].LocalCharges[k].Description);
                                    bool isprebooking = result.Establishment.Rooms[i].Boards[j].LocalCharges[k].IsPerBooking;
                                    string Pay = "";
                                    if (isprebooking == false)
                                    {
                                        Pay = "This Charge need to pay at hotel.";
                                    }

                                    string Essent = "</br></br><b>" + Title + " :" + "</br>" + Description + " " + Pay + " Total Net Amount = " + result1.Amount + " " + result1.Currency + " (Pay at hotel)" + "</b>";

                                    localChargeessen = localChargeessen + " " + Essent;
                                }
                                Essen = Essen + "" + localChargeessen;
                            }
                            catch (Exception ex)
                            {
                               _ =  _exception.Error(ex);
                                IsBooking = "NotAvailable";
                            }
                            if (Essen != "")
                            {
                                dsHotel.Tables[2].DefaultView.RowFilter = "OwnsystemHotelId=" + ownsystemid + "and RoomTypeId='" + roomtypeid + "' and RoomNo='" + roomno + "'";
                                for (int l = 0; l < dsHotel.Tables[2].DefaultView.Count; l++)
                                {
                                    dsHotel.Tables[2].DefaultView[l]["SPUI"] = Essen;
                                }
                            }


                            if (true)
                            {
                                #region Policy Bind
                                int Count = result.Establishment.Rooms[i].Boards[j].RequestedRoomIndex;
                                Count = Count + 1;
                                if (result.Establishment.Rooms[i].Boards[j].NonRefundable == true)
                                {
                                    for (int k = 0; k < result.Establishment.Rooms[i].Boards[j].CancellationPolicy.CancellationCharges.Count; k++)
                                    {
                                        DateTime Fromdate = DateTime.Now.Date;
                                        DateTime Todate = Convert.ToDateTime(result.Establishment.Rooms[i].Boards[j].CancellationPolicy.CancellationCharges[k].ExpiryDate);
                                        string Currency = result.Establishment.Rooms[i].Boards[j].CancellationPolicy.CancellationCharges[k].Charge.Currency;
                                        DataTable dtInrCurrency = _commonMethod.GetCountryCurrency(Currency);
                                        double YalagoCurrencyFactor = Convert.ToDouble(dtInrCurrency.Rows[0]["AmtToExch"]);
                                        double cancellationChargeAmount = Companybuy;// Convert.ToDouble(result.Establishment.Rooms[i].Boards[j].CancellationPolicy.CancellationCharges[k].Charge.Amount) * YalagoCurrencyFactor;
                                        #region Vat
                                        double VatOnPolicy = 0;
                                        double MainAgentCharge = cancellationChargeAmount;
                                        double CompanyChargeAmount = cancellationChargeAmount;
                                        //cancellationChargeAmount = Companybuy;//Common.ProcessHotelCancelationPrice_WithMarkup(ref MainAgentCharge, SearchPara.NoofNights, SearchPara.CityID, SearchPara.CountryId, cancellationChargeAmount, isPerce, B2CMarkup, AgentPercentage, AgentMarkupAmount, IsParentId, SubAgentMarkup, ref VatOnPolicy);
                                        cancellationChargeAmount = _markup.ProcessHotelCancelationPrice_WithMarkup(ref MainAgentCharge, SearchPara.NoofNights, SearchPara.CityID, Convert.ToString(SearchPara.CountryID), cancellationChargeAmount, isPerce, B2CMarkup, AgentPercentage, AgentMarkupAmount, IsParentId, SubAgentMarkup, ref VatOnPolicy);
                                        #endregion
                                        //dtHotelCancellation.Rows.Add(setCancellationPolicyRow(ref CompanyChargeAmount, dtHotelCancellation.NewRow(), 1, "USD", "Yalago", Convert.ToString(ownsystemid), roomno.ToString(), roomtypeid, "", Convert.ToString(OptionCode),  DateTime.ParseExact(SearchPara.CheckInDate, "dd/MM/yyyy", null),Convert.ToDateTime(Todate), MainAgentCharge, cancellationChargeAmount, "", 0, "", "", "", Essen, "", ""));

                                        dtHotelCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref CompanyChargeAmount, dtHotelCancellation.NewRow(), YalagoCurrencyFactor, Currency, Supplier, ownsystemid, Convert.ToString(roomno),
                                        roomtypeid, "", Convert.ToString(OptionCode), DateTime.Now.Date, DateTime.ParseExact(SearchPara.CheckInDate, "dd/MM/yyyy", null), MainAgentCharge, cancellationChargeAmount, "", VatOnPolicy, "", "YES", "", Essen, "", ""));
                                        dtHotelCancellation.AcceptChanges();
                                    }
                                }
                                else
                                {
                                    {
                                        double cancellationChargeAmount = Companybuy;// Convert.ToDouble(result.Establishment.Rooms[i].Boards[j].CancellationPolicy.CancellationCharges[0].Charge.Amount);
                                        DateTime NowDate = DateTime.Now.Date;
                                        DateTime Todate = Convert.ToDateTime(result.Establishment.Rooms[i].Boards[j].CancellationPolicy.CancellationCharges[0].ExpiryDate);
                                        string Currency = result.Establishment.Rooms[i].Boards[j].CancellationPolicy.CancellationCharges[0].Charge.Currency;
                                        DataTable dtInrCurrency = _commonMethod.GetCountryCurrency(Currency);
                                        double YalagoCurrencyFactor = Convert.ToDouble(dtInrCurrency.Rows[0]["AmtToExch"]);
                                        #region Vat
                                        double VatOnPolicy = 0;
                                        double MainAgentCharge = Companybuy;
                                        double CompanyChargeAmount = Companybuy;
                                        cancellationChargeAmount = _markup.ProcessHotelCancelationPrice_WithMarkup(ref MainAgentCharge, SearchPara.NoofNights, SearchPara.CityID, Convert.ToString(SearchPara.CountryID), cancellationChargeAmount, isPerce, B2CMarkup, AgentPercentage, AgentMarkupAmount, IsParentId, SubAgentMarkup, ref VatOnPolicy);
                                        #endregion

                                        DateTime DeadlineDate = DateTime.Now;
                                        DateTime StartDate = DateTime.Now.Date;
                                        for (int p = 0; p < result.Establishment.Rooms[i].Boards[j].CancellationPolicy.CancellationCharges.Count; p++)
                                        {
                                            if (result.Establishment.Rooms[i].Boards[j].CancellationPolicy.CancellationCharges.Count > 1)
                                            {
                                                if (result.Establishment.Rooms[i].Boards[j].CancellationPolicy.CancellationCharges[p].Charge.Amount == 0)
                                                {
                                                    DeadlineDate = Convert.ToDateTime(result.Establishment.Rooms[i].Boards[j].CancellationPolicy.CancellationCharges[p].ExpiryDate);
                                                    DeadlineDate = DeadlineDate.AddDays(CutOffDays);
                                                }
                                            }
                                            else
                                            {
                                                if (result.Establishment.Rooms[i].Boards[j].CancellationPolicy.CancellationCharges[p].Charge.Amount == 0)
                                                {
                                                    DeadlineDate = Convert.ToDateTime(result.Establishment.Rooms[i].Boards[j].CancellationPolicy.CancellationCharges[p].ExpiryDate);
                                                    DeadlineDate = DeadlineDate.AddDays(CutOffDays);
                                                }
                                            }


                                        }

                                        // Add cutoff days in DeadlineDate
                                        //DeadlineDate.AddDays(-1).AddHours(-23).AddMinutes(-59).AddSeconds(-59)
                                        if (NowDate.Date < DeadlineDate.Date)
                                        {
                                            double Zero = 0;
                                            dtHotelCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref Zero, dtHotelCancellation.NewRow(), YalagoCurrencyFactor, Currency, Supplier, ownsystemid, Convert.ToString(roomno),
                                        roomtypeid, "", Convert.ToString(OptionCode), DateTime.Now.Date, DeadlineDate.AddDays(-1).AddHours(00).AddMinutes(00).AddSeconds(00), 0, 0, "", VatOnPolicy, "", "NO", "", Essen, "", ""));
                                            dtHotelCancellation.AcceptChanges();

                                            dtHotelCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref CompanyChargeAmount, dtHotelCancellation.NewRow(), YalagoCurrencyFactor, Currency, Supplier, ownsystemid, Convert.ToString(roomno),
                                           roomtypeid, "", Convert.ToString(OptionCode), DeadlineDate, DateTime.ParseExact(SearchPara.CheckInDate, "dd/MM/yyyy", null), cancellationChargeAmount, cancellationChargeAmount, "", VatOnPolicy, "", "YES", "", Essen, "", ""));
                                            dtHotelCancellation.AcceptChanges();
                                        }
                                        else
                                        {
                                            dtHotelCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref CompanyChargeAmount, dtHotelCancellation.NewRow(), YalagoCurrencyFactor, Currency, Supplier, ownsystemid, Convert.ToString(roomno),
                                           roomtypeid, "", Convert.ToString(OptionCode), DateTime.Now.Date, DateTime.ParseExact(SearchPara.CheckInDate, "dd/MM/yyyy", null), cancellationChargeAmount, cancellationChargeAmount, "", VatOnPolicy, "", "YES", "", Essen, "", ""));
                                            dtHotelCancellation.AcceptChanges();
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
                _ = _exception.ErrorNew(ex, _LogAPI.Recheck, SearchKey);
                IsBooking = "NotAvailable";
            }
            dsHotel.Tables[2].Merge(dtHotelCancellation);

            cancellationDto.dsHotelData = dsHotel;
            cancellationDto.ISBooking = IsBooking;
            return cancellationDto;
        }

        public string Get_Yalago_CancellationPolicyRQ(HotelSearchParameter SearchPara, string hotelcode, string CheckIn, string CheckOut, string RoomTypeId, int roomNo)
        {
            try
            {

                #region "CancellationPolicy RQ"     

                //Yalago.Extra Extra = new Yalago.Extra();
                //Extra.ExtraId = "";
                //Extra.OptionId = "";
                List<Yalago.Room1> RoomMs = new List<Yalago.Room1>();
                //  for (int i = 0; i < SearchPara.Rooms.Count; i++)
                //{
                Yalago.Room1 RM = new Yalago.Room1();
                RM.Adults = SearchPara.Rooms[roomNo].NoofAdults;
                int TotalChild = SearchPara.Rooms[roomNo].NoofChild;
                if (TotalChild >= 1)
                {
                    List<object> ChildAges = new List<object>();
                    for (int j = 0; j < TotalChild; j++)
                    {
                        object Childage = new object();
                        if (j == 0)
                        {
                            Childage = SearchPara.Rooms[roomNo].Child1Age;
                        }
                        if (j == 1)
                        {
                            Childage = SearchPara.Rooms[roomNo].Child2Age;
                        }

                        ChildAges.Add(Childage);
                    }
                    RM.ChildAges = ChildAges;
                }
                string[] Arr = RoomTypeId.Split(new String[] { "$$$" }, StringSplitOptions.None);
                string RoomCode = Arr[0];
                string BoardCode = Arr[1];
                string XmlNationality = Arr[5];
                RM.RoomCode = RoomCode;
                RM.BoardCode = BoardCode;
                RoomMs.Add(RM);
                //}


                Yalago.RootObject RootObject1 = new Yalago.RootObject();
                RootObject1.CheckInDate = CheckIn;
                RootObject1.CheckOutDate = CheckOut;
                RootObject1.EstablishmentId = Convert.ToInt32(hotelcode);
                RootObject1.Rooms = RoomMs;
                RootObject1.Culture = "en-gb";
                RootObject1.GetPackagePrice = true;
                RootObject1.GetLocalCharges = true;
                RootObject1.SourceMarket = XmlNationality;
                RootObject1.GetTaxBreakdown = false;
                var RootObjectString = Newtonsoft.Json.JsonConvert.SerializeObject(RootObject1);
                return RootObjectString;

                #endregion
            }

            catch (Exception ex)
            {
                _exception.Error(ex);
                return "";
            }
        }

        public async Task<ResponseDto> YalagoResponseNew(string xmlRQRS, string strHost, int SearchTimeOut, string Password, string SearchKey = "")
        {
            string responseString = "";

            ResponseDto responseDto = new ResponseDto();
            string Error = "";
            try
            {
                string reqUrl = strHost;
                var request = (HttpWebRequest)WebRequest.Create(reqUrl);
                var postData = xmlRQRS;
                var data = Encoding.ASCII.GetBytes(postData);
                request.Proxy = null;
                request.UserAgent = "Mozilla/4.0 (compatible; MSIE 6.0; MS Web Services Client Protocol 1.1.4322.2300)";
                request.Method = "POST";
                request.Timeout = SearchTimeOut;
                request.ContentType = "application/json;charset=utf-8";
                request.Headers.Add("X-Api-Key", Password);
                request.KeepAlive = true;
                ServicePointManager.Expect100Continue = true;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls
                | SecurityProtocolType.Tls11
                | SecurityProtocolType.Tls12
                 | (SecurityProtocolType)12288; //#25-09-2024
                
                request.Accept = "application/json";
                request.Headers.Add("Accept-Encoding", "application/gzip");
                request.ContentLength = data.Length;
                using (var stream = request.GetRequestStream())
                {
                    stream.Write(data, 0, data.Length);
                }
                var response = (HttpWebResponse)await request.GetResponseAsync();
                Stream st = response.GetResponseStream();
                if (response.ContentEncoding.ToLower().Contains("gzip"))
                    st = new System.IO.Compression.GZipStream(st, System.IO.Compression.CompressionMode.Decompress);
                else if (response.ContentEncoding.ToLower().Contains("deflate"))
                    st = new System.IO.Compression.DeflateStream(st, System.IO.Compression.CompressionMode.Decompress);
                StreamReader stIn = new StreamReader(st, Encoding.Default);
                string respXml = Convert.ToString(await stIn.ReadToEndAsync());
                responseString = respXml;
                stIn.Close();
            }
            catch (Exception ex)
            {
                Error = ex.Message.ToString();
                _ = _exception.Error(ex, SearchKey);
            }

            responseDto.Response = responseString;
            responseDto.Error = Error;

            return responseDto;
        }

        public async Task<PriceCheckCMDto> Get_Yalago_PriceCheckRQRS(DataSet dsCartDetails, DataRow[] YalagoDetails, int i, int j, DataTable dtCheckPriceReqStructure, string UniqueNo, string AgentUniqueNo, bool isForBooking, double CompanyBuyingPriceA, string xmlhotelid, string IsFor, bool IssErrorY, double itempriceY, string Searchkey, int AgentId = 0,TokenData tokenData=null)
        {
            PriceCheckCMDto priceCheckCMDto = new PriceCheckCMDto();    

            string responsetype = "Success";
            string time = "";
            string SupplierName = Convert.ToString(dsCartDetails.Tables[1].DefaultView[0]["SupplierName"]);
            string Error = "";
            string Request = "";
            string YalagoResponse = "";
            try
            {
                #region "PriceCheck RQ"
                //DataTable dtInrCurrency = Common.GetCountryCurrency(XMLHotelBookingCommon.StandardTourCurrency);
                //double CurrencyFactorStandardTour = Convert.ToDouble(dtInrCurrency.Rows[0]["AmtToExch"]);
                string YalagoURL = Convert.ToString(YalagoDetails[0]["WebSiteURL"]) + "hotels/details/get";
                //int SearchTimeOut = Convert.ToInt32(YalagoDetails[0]["BookingConfirmationTimeout"]);
                string Password = Convert.ToString(YalagoDetails[0]["Password"]);
                string HotelId = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["XMLHotelId"]);
                string XmlCity = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["XMLCityId"]);
                string XmlCountry = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["XMLCountryId"]);
                int UserCartId = Convert.ToInt32(dsCartDetails.Tables[0].Rows[i]["UserCartID"]);
                int UserCartDetailId = Convert.ToInt32(dsCartDetails.Tables[1].Rows[j]["UserCartDetailID"]);
                string RoomTypeId = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomTypeID"]);
                string CheckIn = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["CheckInDate"]);//"20180327"
                CheckIn = CheckIn.Substring(4, 2) + "/" + CheckIn.Substring(6, 2) + "/" + CheckIn.Substring(0, 4);
                string CheckOut = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["CheckOutDate"]);
                CheckOut = CheckOut.Substring(4, 2) + "/" + CheckOut.Substring(6, 2) + "/" + CheckOut.Substring(0, 4);
                string[] aa = CheckIn.Split('/');
                string ckin = aa[2] + "-" + aa[0] + "-" + aa[1];
                string[] aa1 = CheckOut.Split('/');
                string ckout = aa1[2] + "-" + aa1[0] + "-" + aa1[1];
                string MealId = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["MealPlanCode"]);
                string XmlNationality = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["XmlNationality"]); //Convert.ToString(dsCartDetails.Tables[0].Rows[j]["NationalityName"]);
                int noOfNights = Convert.ToInt32(dsCartDetails.Tables[0].Rows[i]["NoOfNight"]);
                int noofadult = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[j]["NoOfAdults"]);
                int noofchild = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[j]["NoOfChild"]);
                //string Request = Get_StandardTour_PriceCheckRQ(dsCartDetails, XmlCity, XmlCountry, XmlNationality, CurrencyFactorStandardTour, HotelId, CheckIn, CheckOut, RoomTypeId, NoOfNoghts);
                Request = Get_Yalago_PriceCheckRQ(noofadult, noofchild, HotelId, ckin, ckout, RoomTypeId, XmlNationality);
                _commonMethod.ThirdPartyBookingHistory(Convert.ToString(UniqueNo), Convert.ToString(AgentUniqueNo), Request, "-", "Before get price " + SupplierName + " request hit " + j + " is for Price Check" + isForBooking, SupplierName, "response confirm", "-", "WithinEarth");
                #endregion

                #region "PriceCheck Response"
                if (Request != "")
                {
                    System.Diagnostics.Stopwatch timerroomprocess = new System.Diagnostics.Stopwatch();
                    timerroomprocess.Start();

                    //YalagoResponse = YalagoResponse(ref Error, Request, YalagoURL, 120000, Password);
                    ResponseDto responseDto = await YalagoResponseNew(Request, YalagoURL, 120000, Password);
                    YalagoResponse = responseDto.Response;
                    Error = responseDto.Error;

                    timerroomprocess.Stop();
                    time = timerroomprocess.Elapsed.ToString();

                    _commonMethod.ThirdPartyBookingHistory(Convert.ToString(UniqueNo), Convert.ToString(AgentUniqueNo), Request, YalagoResponse, "After get price " + SupplierName + " request hit " + j + "is for Price Check" + isForBooking, SupplierName, "response confirm", "-", "WithinEarth");
                    if (YalagoResponse == null || YalagoResponse == string.Empty || YalagoResponse.ToLower().Contains("error"))
                    {
                        try
                        {
                            responsetype = "GeneralError";
                            string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, Request, YalagoResponse, Error);
                        }
                        catch (Exception e)
                        {
                            _ = _exception.Error(e, Searchkey);
                        }
                        priceCheckCMDto.Flag = false;
                        return priceCheckCMDto;
                    }
                    var result = Newtonsoft.Json.JsonConvert.DeserializeObject<PriceCheck2.RootObjecT>(YalagoResponse);
                    for (int a = 0; a < result.Establishment.Rooms.Count; a++)
                    {
                        for (int b = 0; b < result.Establishment.Rooms[a].Boards.Count; b++)
                        {
                            int Count = result.Establishment.Rooms[a].Boards[b].RequestedRoomIndex;
                            Count = Count + 1;
                            //if (result.Establishment.Rooms[a].Boards[b].NonRefundable == true)
                            //{
                            string Currency = result.Establishment.Rooms[a].Boards[b].NetCost.Currency;
                            //DataTable dtInrCurrency = _commonMethod.GetCountryCurrency(Currency);
                            //double YalagoCurrencyFactor = Convert.ToDouble(dtInrCurrency.Rows[0]["AmtToExch"]);

                            double YalagoCurrencyFactor = _commonMethod.GetROEByCurrency(Currency, tokenData.dtROE);

                            if (result.Establishment.Rooms[a].Boards[b].IsBindingPrice == false)
                            {
                                itempriceY = Convert.ToDouble(result.Establishment.Rooms[a].Boards[b].NetCost.Amount) * YalagoCurrencyFactor;
                            }
                            else
                            {
                                itempriceY = Convert.ToDouble(result.Establishment.Rooms[a].Boards[b].GrossCost.Amount) * YalagoCurrencyFactor;

                            }
                            //itempriceY = Convert.ToDouble(result.Establishment.Rooms[a].Boards[b].NetCost.Amount) * YalagoCurrencyFactor;
                            //double PriceChekChargeAmount = Convert.ToDouble(result.Establishment.Rooms[a].Boards[b].NetCost.Amount) * YalagoCurrencyFactor;
                            if (IsFor == "P")
                            {
                                if (Convert.ToDouble((itempriceY).TruncateDecimalPlaces(0)) > (CompanyBuyingPriceA).TruncateDecimalPlaces(0))
                                {
                                    try
                                    {
                                        YalagoResponse = "#Price Changed " + "#Main Price = " + CompanyBuyingPriceA + " #New Price = " + itempriceY + " #Response= " + YalagoResponse;
                                        responsetype = "GeneralError";
                                        string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, Request, YalagoResponse, Error);
                                    }
                                    catch (Exception e)
                                    {
                                        _ = _exception.Error(e, Searchkey);
                                    }

                                    priceCheckCMDto.ItemPrice = itempriceY;
                                    priceCheckCMDto.Flag = true;
                                    return priceCheckCMDto;
                                }
                            }
                            else
                            {
                                if (Convert.ToDouble((itempriceY).TruncateDecimalPlaces(0)) != (CompanyBuyingPriceA).TruncateDecimalPlaces(0))
                                {
                                    try
                                    {
                                        YalagoResponse = "#Price Changed " + "#Main Price = " + CompanyBuyingPriceA + " #New Price = " + itempriceY + " #Response= " + YalagoResponse;
                                        responsetype = "GeneralError";
                                        string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, Request, YalagoResponse, Error);
                                    }
                                    catch (Exception e)
                                    {
                                        _ = _exception.Error(e, Searchkey);
                                    }
                                    priceCheckCMDto.ItemPrice = itempriceY;
                                    priceCheckCMDto.Flag = true;
                                    return priceCheckCMDto;
                                }
                            }
                            //}
                        }
                    }
                }
                #endregion
            }
            catch (Exception ex)
            {
                try
                {
                    Error = ex.Message + ex.StackTrace.ToString();
                    responsetype = "GeneralError";
                    string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, Request, YalagoResponse, Error);
                   
                }
                catch (Exception e)
                {
                    _ = _exception.Error(e, Searchkey);
                }
                _ = _exception.Error(ex);

                IssErrorY = true;
            }
            try
            {
                responsetype = "Success";
                string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, Request, YalagoResponse, Error);
            }
            catch (Exception e)
            {
                _ = _exception.Error(e, Searchkey);
            }
            //return false;

            priceCheckCMDto.ItemPrice = itempriceY;
            priceCheckCMDto.Flag = false;
            priceCheckCMDto.IsError = IssErrorY;
            return priceCheckCMDto;
        }


        public string Get_Yalago_PriceCheckRQ(int noofadult, int noofchild, string hotelcode, string CheckIn, string CheckOut, string RoomTypeId, string XmlNationality)
        {
            try
            {

                #region " PriceCheckRQ "     


                List<Yalago.Room1> RoomMs = new List<Yalago.Room1>();

                Yalago.Room1 RM = new Yalago.Room1();
                RM.Adults = noofadult;
                int TotalChild = noofchild;
                if (TotalChild >= 1)
                {
                    List<object> ChildAges = new List<object>();
                    for (int j = 0; j < TotalChild; j++)
                    {
                        object Childage = new object();
                        if (j == 0)
                        {
                            Childage = noofchild;
                        }
                        if (j == 1)
                        {
                            Childage = noofchild;
                        }

                        ChildAges.Add(Childage);
                    }
                    RM.ChildAges = ChildAges;
                }
                string[] Arr = RoomTypeId.Split(new String[] { "$$$" }, StringSplitOptions.None);
                string RoomCode = Arr[0];
                string BoardCode = Arr[1];
                RM.RoomCode = RoomCode;
                RM.BoardCode = BoardCode;
                RoomMs.Add(RM);



                Yalago.RootObject RootObject1 = new Yalago.RootObject();
                RootObject1.CheckInDate = CheckIn;
                RootObject1.CheckOutDate = CheckOut;
                RootObject1.EstablishmentId = Convert.ToInt32(hotelcode);
                RootObject1.Rooms = RoomMs;
                RootObject1.Culture = "en-gb";
                //RootObject1.GetPackagePrice = true;
                //RootObject1.GetPackagePrice = false; //due to ticket no: 13870
                RootObject1.GetPackagePrice = true; // on 26-5-2023

                RootObject1.SourceMarket = XmlNationality;
                RootObject1.GetTaxBreakdown = false;
                var RootObjectString = Newtonsoft.Json.JsonConvert.SerializeObject(RootObject1);
                return RootObjectString;

                #endregion
            }

            catch (Exception ex)
            {
                _ = _exception.Error(ex);
                return "";
            }
        }

        public string Get_Yalago_BookingRequest(DataSet dsCartDetails, DataTable dtPassengers, string UniqueNo, int i, int j, string hotelcode, string CheckIn, string CheckOut, string Currency, string Nationality, string agtCode)
        {
            string BookingRQ = "";
            try
            {
                string InternalReference = "Book_" + agtCode + "_" + UniqueNo;
                Yalago.rootObject rootObject = new Yalago.rootObject();
                rootObject.AffiliateRef = InternalReference;
                rootObject.CheckInDate = CheckIn.Substring(0, 4) + "-" + CheckIn.Substring(4, 2) + "-" + CheckIn.Substring(6, 2);
                rootObject.CheckOutDate = CheckOut.Substring(0, 4) + "-" + CheckOut.Substring(4, 2) + "-" + CheckOut.Substring(6, 2);
                Yalago.ContactDetails ContactDetails = new Yalago.ContactDetails();
                string isLead = string.Empty;
                dtPassengers.DefaultView.RowFilter = "PickUp='" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[0]["RoomTypeID"]) + "' and RoomNo='" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[0]["RoomNumber"]) + "' and IsLead=1";
                if (dtPassengers.DefaultView.Count >= 1)
                {
                    ContactDetails.FirstName = Convert.ToString(dtPassengers.Rows[0]["FirstName"]);
                    ContactDetails.LastName = Convert.ToString(dtPassengers.Rows[0]["LastName"]);
                    ContactDetails.Title = Convert.ToString(dtPassengers.Rows[0]["Prefix"]);
                }
                rootObject.ContactDetails = ContactDetails;
                rootObject.Culture = "en-GB";
                rootObject.EstablishmentId = Convert.ToInt32(hotelcode);
                List<Yalago.room> rooms = new List<Yalago.room>();
                //for (int j = 0; j < dsCartDetails.Tables[1].Rows.Count; j++)
                //{
                #region Process
                Yalago.room room = new Yalago.room();
                room.AffiliateRoomRef = InternalReference;
                string RoomType = Convert.ToString(dsCartDetails.Tables[1].DefaultView[0]["RoomTypeID"]);
                string[] RoomCode = RoomType.Split(new String[] { "$$$" }, StringSplitOptions.None);// Convert.ToString(dsCartDetails.Tables[1].Rows[j]["RoomTypeId"]).Split(new String[] { "$$$" }, StringSplitOptions.None);
                room.RoomCode = RoomCode[0];
                //string[] BoardCode = Convert.ToString(dsCartDetails.Tables[1].Rows[j]["RoomTypeId"]).Split(new String[] { "$$$" }, StringSplitOptions.None);
                room.BoardCode = RoomCode[1];
                //string[] price = Convert.ToString(dsCartDetails.Tables[1].Rows[j]["RoomTypeId"]).Split(new String[] { "$$$" }, StringSplitOptions.None);
                //string[] YalagoCurrency = Convert.ToString(dsCartDetails.Tables[1].Rows[j]["RoomTypeId"]).Split(new String[] { "$$$" }, StringSplitOptions.None);

                Yalago.ExpectedNetCost ExpectedNetCost = new Yalago.ExpectedNetCost();
                ExpectedNetCost.Amount = Convert.ToDouble(RoomCode[2]);
                ExpectedNetCost.Currency = RoomCode[3];

                room.ExpectedNetCost = ExpectedNetCost;

                List<Yalago.Guest> Guest = new List<Yalago.Guest>();
                string IsLead = string.Empty;

                dtPassengers.DefaultView.RowFilter = "";
                dtPassengers.DefaultView.RowFilter = "PickUp='" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomTypeId"]) + "' and RoomNo='" + (dsCartDetails.Tables[1].DefaultView[j]["RoomNumber"]) + "' and UserCartId='" + dsCartDetails.Tables[1].Rows[i]["UserCartId"] + "'and RoomNo='" + Convert.ToInt32(j + 1) + "'";
                if (dtPassengers.DefaultView.Count > 0)
                {
                    for (int k = 0; k < dtPassengers.DefaultView.Count; k++)
                    {
                        Yalago.Guest GST = new Yalago.Guest();
                        int age = 0;
                        if (Convert.ToString(dtPassengers.DefaultView[k]["PaxType"]).ToLower() == "adult")
                            age = 27;
                        else
                            age = Convert.ToInt32(dtPassengers.DefaultView[k]["ChildAge"]);
                        GST.Age = age;
                        GST.FirstName = Convert.ToString(dtPassengers.DefaultView[k]["FirstName"]);
                        GST.LastName = Convert.ToString(dtPassengers.DefaultView[k]["LastName"]);
                        GST.Title = Convert.ToString(dtPassengers.DefaultView[k]["Prefix"]);
                        Guest.Add(GST);
                    }
                }

                #endregion
                rooms.Add(room);
                room.Guests = Guest;

                //}
                rootObject.Rooms = rooms;
                //rootObject.GetPackagePrice = true;
                bool GetPackagePrice = Convert.ToBoolean(RoomCode[6]);
                //rootObject.GetPackagePrice = GetPackagePrice; //due to ticket no: 13870
                rootObject.GetPackagePrice = true; // on 26-5-2023
                rootObject.SourceMarket = Nationality;
                rootObject.GetTaxBreakdown = false;
                var RootObjectString = Newtonsoft.Json.JsonConvert.SerializeObject(rootObject);
                BookingRQ = RootObjectString.ToString();
            }
            catch (Exception ex)
            {
                _=_exception.Error(ex);
            }
            return BookingRQ;
        }

        public string Get_Yalago_GetCancellationChargesRQ(string AgentRef)
        {
            try
            {
                StringBuilder Yalago_GetCancellationCharges = new StringBuilder();
                {
                    Yalago_GetCancellationCharges.Append("{");
                    Yalago_GetCancellationCharges.Append("\"BookingRef\":\"" + AgentRef + "\"");
                    Yalago_GetCancellationCharges.Append("}");
                    string GetCancellationCharges = Yalago_GetCancellationCharges.ToString();
                    return GetCancellationCharges;
                }
            }
            catch (Exception ex)
            {
                _ =_exception.ErrorNew(ex, _LogAPI.CancelBooking);
                return "";
            }
        }

        public string Get_Yalago_CancelBooking(double Amount, string currency, string AgentRef)
        {
            try
            {

                StringBuilder Get_Yalago_CancelBooking = new StringBuilder();
                {
                    Get_Yalago_CancelBooking.Append("{");
                    Get_Yalago_CancelBooking.Append("\"BookingRef\":\"" + AgentRef + "\",");
                    Get_Yalago_CancelBooking.Append("\"ExpectedCharge\":{");
                    Get_Yalago_CancelBooking.Append("\"Charge\":{");
                    Get_Yalago_CancelBooking.Append("\"Amount\":" + Amount + ",");
                    Get_Yalago_CancelBooking.Append("\"Currency\":\"" + currency + "\"");
                    Get_Yalago_CancelBooking.Append("}");
                    Get_Yalago_CancelBooking.Append("}");
                    Get_Yalago_CancelBooking.Append("}");

                    string YalagoCancelBooking = Get_Yalago_CancelBooking.ToString();
                    return YalagoCancelBooking;
                }

            }
            catch (Exception ex)
            {
                _ = _exception.ErrorNew(ex, _LogAPI.CancelBooking);

                return "";
            }

        }
    }
}
