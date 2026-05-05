using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Xml;
using Azure;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using XConnect.Analytics;
using XConnect.CommonUtilities;
using XConnect.CommonUtilities.Interface;
using XConnect.ErrorLogHandler.Interface;
using XConnect.Markup.Interface;
using XConnect.Models;
using XConnect.Models.Hotel;
using XConnect.Supplier.ETG.Interface;
using XConnect.Supplier.HotelDo;
using XConnect.Supplier.XTG;
using static Azure.Core.HttpHeader;
using static XConnect.Models.ErrorLog;
using static XConnect.Supplier.Booking.Com.preebookrs;
using static XConnect.Supplier.ETG.ETGSearchResponse;
using static XConnect.Supplier.Webhoteliter.WebHotellier.WebHotelierRoomResponse;
using static XConnect.Supplier.Yalago.Yalago;
using Options = XConnect.Models.Options;

namespace XConnect.Supplier.ETG
{
    public class EtgRepo : IEtg
    {
        private readonly ICommonMethod _commonMethod;
        private readonly IException _exception;
        private readonly IMarkup _markup;
        private readonly ISupplierRes _supplierRes;
        private readonly IMemoryCache _cache;
        private readonly KeySettings _keySettings;
        private readonly SupplierConfiguration _supplierConfig;//Added on 27-FEB-26
        private readonly ILogger<Analytic> _logger;


        public EtgRepo(ICommonMethod commonMethod, IMarkup markup, ISupplierRes supplierRes, IMemoryCache cache, IOptions<KeySettings> keySettings, IException exception, IOptions<SupplierConfiguration> supplierConfig, ILogger<Analytic> logger)
        {
            _commonMethod = commonMethod;
            _markup = markup;
            _supplierRes = supplierRes;
            _cache = cache;
            _keySettings = keySettings.Value;
            _exception = exception;
            _supplierConfig = supplierConfig.Value;
            _logger = logger;
        }
        public void GetOstrovokRequestOnly_Async(DataRow[] commonCode, List<StarRating> listRating, HotelSearchParameter SearchPara, List<SupplierDetails> lstOstrovok_Details, List<SupplierSearchConfig> lstOstrovokSearchConfig, DataTable dtPreferredDataHotel, ref ConcurrentBag<HotelX> AllTask, int Hit, DataTable dtROE, TokenData tokenData, DataSet dtXMLCity)
        {
            try
            {
                if (lstOstrovok_Details.Count > 0)
                {
                    List<StarRating> lstRating = new List<StarRating>();
                    lstRating = _commonMethod.GetStarRatingStringList_xtg(SearchPara);

                    List<HotelCodeByCity> DtHotelDAta = HtmlExtensions.ToList<HotelCodeByCity>(commonCode.CopyToDataTable());
                    int ratingcount = lstRating.Count;
                    int hotelcount = DtHotelDAta.Count;

                    foreach (SupplierDetails Ostrovok_Details in lstOstrovok_Details)
                    {
                        int SupplierID = Ostrovok_Details.SupplierID;
                        string Supplier = Ostrovok_Details.SupplierName;

                        if (lstOstrovokSearchConfig.Count > 0)
                        {
                            try
                            {
                                List<SupplierSearchConfig> OstrovokSearchConfig = lstOstrovokSearchConfig
                                  .Where(x => x != null && x.SupplierID == SupplierID)
                                  .ToList();


                                if (OstrovokSearchConfig != null && OstrovokSearchConfig.Count > 0)
                                {
                                    DataForSearch dataForSearch = _commonMethod.GetXmlDataForSearchTechwise(SearchPara.Nationality, dtXMLCity, Supplier, Ostrovok_Details.TechName);

                                    if (dataForSearch.XmlNationality != "")
                                    {
                                        string Ids = string.Empty;
                                        string xmlbase = "";
                                        _commonMethod.GetPrefferedHotelCodes(ref Ids, ref xmlbase, ref OstrovokSearchConfig, Supplier, dtPreferredDataHotel, SearchPara.IsSearchFor);

                                        List<HotelCodeByCity> filter = new List<HotelCodeByCity>();
                                      //  filter = _commonMethod.FilterWithOutCity_Code(ratingcount, hotelcount, DtHotelDAta, lstRating, Supplier);
                                        filter = _commonMethod.FilterWithOutCity_Code(ratingcount, hotelcount, DtHotelDAta, lstRating, Ostrovok_Details.HotelSupplier);


                                        #region "HotelDo"
                                        if (OstrovokSearchConfig != null)
                                        {
                                            double CurrencyFactorOstrovok = 1;
                                            string OstrovokCurrency = Ostrovok_Details.CurrencyShortName;
                                            if (OstrovokCurrency.ToLower() != Convert.ToString(tokenData.SiteCurrency).ToLower())
                                            {
                                                CurrencyFactorOstrovok = _commonMethod.GetROEByCurrency(OstrovokCurrency, dtROE);
                                            }
                                            for (int k = 0; k < OstrovokSearchConfig.Count; k++)
                                            {
                                                bool DebugMode = Convert.ToInt32(OstrovokSearchConfig[k].DebugMode) == 0 ? false : true;
                                                string UserName = Convert.ToString(Ostrovok_Details.UserName);
                                                string PassWord = Convert.ToString(Ostrovok_Details.Password);
                                                if (filter.Count > 0)
                                                {
                                                    if (Convert.ToInt32(OstrovokSearchConfig[k].CityID) == 0 || Convert.ToInt32(OstrovokSearchConfig[k].CityID) == SearchPara.CityID)
                                                    {
                                                        int NoOfRecord = Convert.ToInt32(OstrovokSearchConfig[k].Noofrecords);
                                                        double count = Convert.ToDouble(filter.Count);
                                                        int NoOfDiv = Convert.ToInt32(Math.Ceiling(count / Convert.ToDouble(NoOfRecord)));
                                                        List<List<XMLID_Search>> lstxml = _commonMethod.GetXMLIDString(NoOfDiv, NoOfRecord, count, filter);

                                                        for (int a = 0; a < NoOfDiv; a++)
                                                        {

                                                            //var paramValues1 = string.Join(",", splittedtables[a].Select().Select(x => string.Join(",", Convert.ToString(x.ItemArray[0]))));
                                                            //string IdsForSearch = Convert.ToString(paramValues1);
                                                            string IdsForSearch = Convert.ToString(_commonMethod.GetHotelCodeByCOmma(lstxml[a]));

                                                            HotelX xhotels = new HotelX();
                                                            xhotels.Supplier = Supplier;
                                                            xhotels.DebugeMode = DebugMode;
                                                            xhotels.SearchTimeOut = Convert.ToInt32(OstrovokSearchConfig[k].HotelSearchTimeOut);
                                                            //if (Hit > 0)
                                                            xhotels.ProcessTimeout = Convert.ToInt32(OstrovokSearchConfig[k].HotelSearchTimeOut2);
                                                            if (_keySettings.SupplierProxy == "0")
                                                                xhotels.URL = Convert.ToString(Ostrovok_Details.WebSiteURL) + _supplierConfig.ETG.search_serp_hotels;
                                                            else
                                                                xhotels.URL = Convert.ToString(Ostrovok_Details.WebSiteURLforAPI) + _supplierConfig.ETG.search_serp_hotels;
                                                            xhotels.Action = "";
                                                            xhotels.UserName = UserName;
                                                            xhotels.PassWord = PassWord;
                                                            xhotels.CurrencyCode = OstrovokCurrency;
                                                            xhotels.CurrencyFactor = CurrencyFactorOstrovok;
                                                            xhotels.XmlNationality = dataForSearch.XmlNationality;
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
                                                                objrq.Request = OSTVRequestOnly_Async(Convert.ToString(Ostrovok_Details.UserName), Convert.ToString(Ostrovok_Details.Password), Convert.ToString(Ostrovok_Details.UserID), SearchPara, IdsForSearch, DebugMode, dataForSearch.XmlNationality, dataForSearch.xmlCity, OstrovokCurrency);
                                                                objrq.Response = ""; RQRS.Add(objrq);
                                                            }

                                                            xhotels.RQRS = RQRS;
                                                            #endregion
                                                            xhotels.ReturnRooms = Convert.ToInt16(Ostrovok_Details.ReturnRooms);
                                                            xhotels.TechName = Convert.ToString(Ostrovok_Details.TechName);
                                                            AllTask.Add(xhotels);
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


        public string OSTVRequestOnly_Async(string UserName, string Password, string UserID, HotelSearchParameter SearchParam, string hotelIds, bool DebugMode, string Nationality, string citycode, string Currency)
        {
            try
            {
                int isHit = 0;
                return OSTVHotelRequest(SearchParam, Nationality, UserName, Password, UserID, hotelIds, isHit, citycode, Currency, false);
            }
            catch (Exception ex)
            {
                _ = _exception.Error(ex);
                return "";
            }
            finally
            {
                //GC.Collect();
            }
        }

        public string OSTVHotelRequest(HotelSearchParameter SearchParam, string Nationality, string UserName, string Password, string UserID, string ETGHotelIds, int IsHit, string Citycode, string Currency, bool IsRooom = false)
        {
            try
            {
                DataTable dtPriceDetail = _commonMethod.GetRoomPriceTableStructure();
                int RoomNo = SearchParam.Rooms.Count;
                int MaxRating = SearchParam.StarStart;
                string language = "en";
                int Adult = 0;
                int Child = 0;
                string checkin2 = (SearchParam.CheckInDate).Substring(6, 4) + "-" + (SearchParam.CheckInDate).Substring(3, 2) + "-" + (SearchParam.CheckInDate).Substring(0, 2);
                string checkout2 = (SearchParam.CheckOutDate).Substring(6, 4) + "-" + (SearchParam.CheckOutDate).Substring(3, 2) + "-" + (SearchParam.CheckOutDate).Substring(0, 2);

                #region Price Only and Room Only RQ
                string Request = "";
                StringBuilder RootObject = new StringBuilder();
                RootObject.Append("{");
                RootObject.Append("\"checkin\": \"" + checkin2 + "\",");
                RootObject.Append("\"checkout\": \"" + checkout2 + "\",");
                RootObject.Append("\"residency\": \"" + Nationality + "\",");
                RootObject.Append("\"language\": \"" + language + "\",");
                RootObject.Append("\"guests\": [");
                for (int i = 0; i < RoomNo; i++)
                {
                    int ChildAge1 = SearchParam.Rooms[i].Child1Age;
                    int ChildAge2 = SearchParam.Rooms[i].Child2Age;
                    Adult = SearchParam.Rooms[i].NoofAdults;
                    Child = SearchParam.Rooms[i].NoofChild;

                    RootObject.Append("{");
                    RootObject.Append("\"adults\": " + Adult + ",");
                    if (Child == 1)
                        RootObject.Append("\"children\": [" + ChildAge1 + "]}");
                    else if (Child == 2)
                        RootObject.Append("\"children\": [" + ChildAge1 + "," + ChildAge2 + "]}");
                    else
                        RootObject.Append("\"children\": [] }");
                    if (RoomNo - 1 != i)
                        RootObject.Append(",");
                }
                RootObject.Append("],");
                {
                    if (ETGHotelIds.Trim() != string.Empty)
                    {
                        RootObject.Append("\"ids\":[\"");
                        ETGHotelIds = ETGHotelIds.Replace(",", "\",\"");
                        RootObject.Append(ETGHotelIds);
                        RootObject.Append("\"],");

                    }
                    else
                        RootObject.Append("\"region_id\": " + Citycode + ",");
                }
                RootObject.Append("\"currency\": \"" + Currency + "\"");
                RootObject.Append("}");
                Request = RootObject.ToString();
                return Request;
                #endregion
            }
            catch (Exception ex)
            {
                _ = _exception.Error(ex);
            }
            return string.Empty;
        }

        public async Task<List<Options>> Ostrovok_HotelSearchWithRoomswithBookHash_XML(HotelX Item, CommonSearch commonSearch, string Currency, string ResponseData, string CityCode, HotelSearchParameter SearchParam, double CurrencyFactorOstrovok, String Provider, string XMLnationality, string URLandRequest, string UserName, string PassWord, string URL, Int32 SearchTimeOut, DataTable dtROE, string ProccessCutOffTime, Int32 processTimeout)
        {
            System.Diagnostics.Stopwatch timer_room_Ostrovok = new System.Diagnostics.Stopwatch();
            timer_room_Ostrovok.Start();
            List<Options> options = new List<Options>();
            //RoomBookHash.Root OSTV_ResponseRoom = null;
            try
            {
                #region DataTable
                DataTable DtHotel = _commonMethod.GenerateDataTableNewV2();
                #endregion
                if (ResponseData != string.Empty)
                {
                    if (ResponseData == string.Empty)
                    {
                        Item.ResponseType = "NoCount";
                        return options;
                    }
                    string ConvertXml = Newtonsoft.Json.JsonConvert.DeserializeXmlNode("{\"Row\":" + ResponseData + "}", "root").InnerXml;

                    DataSet ETGResponse = _commonMethod.GetDataSetFromXMLString(ConvertXml);
                    if (ETGResponse == null || ETGResponse.Tables["hotels"] == null)
                    {
                        Item.ResponseType = "NoCount";
                        return options;
                    }
                    int CBranchId = 0;
                    try
                    {
                        if (commonSearch.dtSuplierMarkup != null)
                        {
                            commonSearch.dtSuplierMarkup.DefaultView.RowFilter = "SupplierName='Ostrovok'";
                            if (commonSearch.dtSuplierMarkup.DefaultView.Count > 0)
                            {
                                CBranchId = Convert.ToInt32(commonSearch.dtSuplierMarkup.DefaultView[0]["CBranchId"]);
                            }
                        }
                    }

                    catch (Exception)
                    {
                    }
                    //string RoomRequest = Common.ETGHotelRequest(SearchParam, XMLnationality, Hotel_Id, xmlcity, Currency);

                    string Essential = "";
                    double FinalMinPrice = 0;
                    double Total = 0;
                    for (int i = 0; i < ETGResponse.Tables["hotels"].Rows.Count; i++) //Hotel Loop
                    {
                        Item.ResponseType = "Success";
                        if (ProccessCutOffTime == "1" && timer_room_Ostrovok.ElapsedMilliseconds > processTimeout)
                        {
                            timer_room_Ostrovok.Stop();
                            break;
                        }
                        Item.ResponseType = "Success";
                        string hotels_id = Convert.ToString(ETGResponse.Tables[2].Rows[i]["hotels_id"]);
                        //string IdforRoom = Convert.ToString(ETGResponse.Tables[2].Rows[i]["hotels_id"]);
                        string ItemCode = Convert.ToString(ETGResponse.Tables["hotels"].Rows[i]["id"]);
                        //string RoomRequest = OstvRoomRequest(SearchParam, XMLnationality, ItemCode, Currency);
                        string Error = "";
                        string URLRoom = "https://api.worldota.net/api/b2b/v3/search/hp/";
                        //string RoomResponseData = OstrovokSupplierRoom(ref Error, RoomRequest, URLRoom, UserName, PassWord, "ostrovok");
                        //if (RoomResponseData == "" || ResponseData == string.Empty || Error != "")
                        //{
                        //    Item.ResponseType = "NoCount";
                        //    return options;
                        //}
                        //OSTV_ResponseRoom = JsonConvert.DeserializeObject<RoomBookHash.Root>(RoomResponseData);
                        //string HotelName = Convert.ToString(ETGResponse.Tables["hotels"].Rows[i]["name"]);
                        DataRow[] drRooms = ETGResponse.Tables["rates"].Select("hotels_Id='" + hotels_id + "'");
                        for (int g = 0; g < drRooms.Length; g++)
                        {
                            DataRow[] drRates = ETGResponse.Tables["rates"].Select("hotels_Id='" + hotels_id + "'");
                            string rateid = Convert.ToString(drRates[0]["rates_Id"]);

                            DataRow[] payment_option = ETGResponse.Tables["payment_options"].Select("rates_Id='" + rateid + "'");
                            int payment_optionId = Convert.ToInt32(payment_option[0]["payment_options_Id"]);

                            DataRow[] payment_type = ETGResponse.Tables["payment_types"].Select("payment_options_Id='" + payment_optionId + "'");

                            double sellingAmount = Convert.ToDouble(payment_type[0]["amount"]);


                            Currency = Convert.ToString(payment_type[0]["currency_code"]);
                            if (Currency != "usd")
                            {
                                try
                                {
                                    CurrencyFactorOstrovok = _commonMethod.GetROEByCurrency(Currency, dtROE);
                                }
                                catch (Exception)
                                {
                                    continue;
                                }
                            }
                            double HotelPrice = sellingAmount * CurrencyFactorOstrovok;

                            if (g == 0)
                                Total = HotelPrice;
                            else
                            {
                                if (HotelPrice < Total)
                                    Total = HotelPrice;
                            }
                        }
                        #region "When not Need Room" 
                        if (SearchParam.Filters.IsShowRooms == 0)
                        {
                            #region "Hotel Code Block "


                            DataRow drXmlHotel = DtHotel.NewRow();
                            //string InternalHotelId = Convert.ToString(ItemCode);
                            drXmlHotel["XMLHotelCode"] = ItemCode;
                            drXmlHotel["SupplierName"] = Provider;
                            drXmlHotel["OfficeID"] = 0;
                            drXmlHotel["ContractID"] = 0;
                            drXmlHotel["Rating"] = 0;
                            drXmlHotel["Reviews"] = 0;
                            drXmlHotel["UniqueNo"] = ItemCode;
                            drXmlHotel["IsShowSupplier"] = false;
                            drXmlHotel["BookingStatus"] = "Available";
                            drXmlHotel["XMLCity"] = CityCode;
                            drXmlHotel["SupplierCurrency"] = Currency;
                            drXmlHotel["FinalSellingPrice"] = Total;
                            drXmlHotel["SellingPrice"] = Total;
                            DtHotel.Rows.Add(drXmlHotel);
                            double TotalCharges = Total;

                            #endregion
                            if (TotalCharges > 0)
                            {
                                #region "Room code Block"
                                bool willAddOrNot = true;
                                Int64 RaynaID = 0;
                                Int64 GiataId = 0;
                                int Rating = 0;
                                Int64 OwnsystemHotelId = 0;
                                double MinPrice = Total;
                                double CompanyBranchBuyingPrice = 0;
                                double CompanyBranchSellingPrice = 0;
                                //int AgentBranchId = commonSearch.tokenData.ABranchId;
                                //HotelXML_Suppliers.GetHotelDataCommon(ref MinPrice, ref willAddOrNot, ref OwnsystemHotelId, ref RaynaID, ref GiataId, ref Rating, ref drXmlHotel, commonSearch.AgentId, commonSearch.TariffID, commonSearch.dsAgentDetails, commonSearch.supplierGiataMaster, SearchParam, commonSearch.dtCountryCityMarkup, commonSearch.B2CMarkup, commonSearch.IsParentId, commonSearch.SubAgentMarkup, commonSearch.SearchByHotel, commonSearch.HotelIDS);
                                _markup.GetHotelDataCommon(ref MinPrice, ref willAddOrNot, ref OwnsystemHotelId, ref RaynaID, ref GiataId, ref Rating, ref drXmlHotel, commonSearch.AgentId, commonSearch.TariffID, commonSearch.dsAgentDetails, commonSearch.supplierGiataMaster, SearchParam, commonSearch.dtCountryCityMarkup, commonSearch.B2CMarkup, commonSearch.IsParentId, commonSearch.SubAgentMarkup, commonSearch.SearchByHotel, commonSearch.HotelIDS);
                                //HotelXML_Suppliers.GetHotelDataCommon(ref MinPrice, ref willAddOrNot, ref OwnsystemHotelId, ref RaynaID, ref GiataId, ref Rating, ref drXmlHotel, commonSearch.AgentId, commonSearch.TariffID, commonSearch.dsAgentDetails, commonSearch.supplierGiataMaster, SearchParam, commonSearch.dtCountryCityMarkup, commonSearch.B2CMarkup, commonSearch.IsParentId, commonSearch.SubAgentMarkup, commonSearch.SearchByHotel, commonSearch.HotelIDS);
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
                        #region "When Need Rooms"
                        else
                        {
                            #region Hotels
                            #region "Hotel Block here"
                            DataRow drXmlHotels = DtHotel.NewRow();
                            drXmlHotels["XMLHotelCode"] = ItemCode;
                            drXmlHotels["HotelName"] = "";
                            drXmlHotels["SupplierName"] = Provider;
                            drXmlHotels["OfficeID"] = 0;
                            drXmlHotels["ContractID"] = 0;
                            drXmlHotels["Rating"] = 0;
                            drXmlHotels["Reviews"] = 0;
                            drXmlHotels["SupplierCurrency"] = Currency;
                            drXmlHotels["UniqueNo"] = ItemCode;
                            drXmlHotels["IsShowSupplier"] = false;
                            drXmlHotels["BookingStatus"] = "Available";
                            drXmlHotels["FinalSellingPrice"] = 1;
                            drXmlHotels["SellingPrice"] = 1;
                            drXmlHotels["IsRecomondedHotel"] = false;
                            DtHotel.Rows.Add(drXmlHotels);
                            #endregion
                            #region "GetRooms for this hotel"
                            bool willAddOrNot = true;
                            int Rating = 0;
                            Int64 RaynaID = 0;
                            Int64 GiataId = 0;
                            int RatingHotel = 0;
                            Int64 OwnsystemHotelId = 0;
                            double MinPrice = 1;
                            double CompanyBranchBuyingPrice = 0;
                            double CompanyBranchSellingPrice = 0;
                            //int AgentBranchId = commonSearch.tokenData.ABranchId;
                            _markup.GetHotelDataCommon(ref MinPrice, ref willAddOrNot, ref OwnsystemHotelId, ref RaynaID, ref GiataId, ref Rating, ref drXmlHotels, commonSearch.AgentId, commonSearch.TariffID, commonSearch.dsAgentDetails, commonSearch.supplierGiataMaster, SearchParam, commonSearch.dtCountryCityMarkup, commonSearch.B2CMarkup, commonSearch.IsParentId, commonSearch.SubAgentMarkup, commonSearch.SearchByHotel, commonSearch.HotelIDS);
                            if (willAddOrNot)
                            {
                                DataTable dtOstvHotelRoom = _commonMethod.GenerateRoomtypeDataTableNewV2();
                                DataTable dtOstvCancellation = _commonMethod.GenerateCancellationDataTableNewV2();
                                DataTable dtOstvHotelRoomRateBreackUp = _commonMethod.GenerateoomRateBreackUpDataTableNewV2();
                                int CutOffDays = XMLHotelBookingCommon.CutOffDays;
                                CutOffDays = _commonMethod.GetCutOffDays(commonSearch.dtSuplierMarkup, Provider);
                                #region "Room Logic"
                                if (SearchParam.Filters.IsShowRooms == 1)
                                {
                                    double AgentPercentage = 0.0;

                                    double AgentMarkupAmount = 0.0;
                                    Boolean isPerce = true;

                                    double AgentCompanyPercentage = Convert.ToDouble(SearchParam.AgentCompanyPercentage);
                                    bool IsB2C = Convert.ToBoolean(Convert.ToByte(SearchParam.IsB2BOrB2C));
                                    bool blnIsShowSupplier = false;
                                    _markup.getAgetMarkupSetting(Provider, GiataId.ToString(), commonSearch.dtSuplierMarkup, commonSearch.dtMarkupDetails, ref AgentPercentage, ref AgentMarkupAmount, ref blnIsShowSupplier, ref isPerce, commonSearch.AgentId, RatingHotel, SearchParam.CityID, SearchParam.CountryID.ToString(), Convert.ToString(SearchParam.CheckInDate));
                                    int NoNight = SearchParam.NoofNights;
                                    int NoOfRoom = SearchParam.Rooms.Count();

                                    string checkin = (SearchParam.CheckInDate).Substring(6, 4) + "-" + (SearchParam.CheckInDate).Substring(3, 2) + "-" + (SearchParam.CheckInDate).Substring(0, 2);
                                    string checkout = (SearchParam.CheckOutDate).Substring(6, 4) + "-" + (SearchParam.CheckOutDate).Substring(3, 2) + "-" + (SearchParam.CheckOutDate).Substring(0, 2);

                                    int TotalAdults = 0;
                                    int TotalChildren = 0;
                                    for (int K = 0; K < SearchParam.Rooms.Count; K++)
                                    {
                                        TotalAdults = Convert.ToInt32(SearchParam.Rooms[K].NoofAdults);
                                        TotalChildren = Convert.ToInt32(SearchParam.Rooms[K].NoofChild);
                                    }
                                    #region Supplier Room Code block
                                    int rates_id = 0;
                                    {
                                        for (int room = 0; room < drRooms.Length; room++) // Room Loop
                                        {
                                            if (ProccessCutOffTime == "1" && timer_room_Ostrovok.ElapsedMilliseconds > processTimeout)
                                            {
                                                timer_room_Ostrovok.Stop();
                                                break;
                                            }
                                            string Etg_MatchHas = Convert.ToString(drRooms[room]["match_hash"]);
                                            string search_hash = Convert.ToString(drRooms[room]["search_hash"]);
                                            //string BookHash = OSTV_ResponseRoom.data.hotels.SelectMany(h => h.rates).FirstOrDefault(r => r.match_hash == Etg_MatchHas);
                                            //                                        var matchedRate = OSTV_ResponseRoom.data.hotels
                                            //.SelectMany(h => h.rates)
                                            //.FirstOrDefault(r => r.match_hash == Etg_MatchHas);
                                            //                                        string BookHash = matchedRate.book_hash;
                                            rates_id = Convert.ToInt32(ETGResponse.Tables["rates"].Rows[room]["rates_Id"]);
                                            DataRow[] payment_option = ETGResponse.Tables["payment_options"].Select("rates_Id='" + rates_id + "'");
                                            int payment_optionId = Convert.ToInt32(payment_option[0]["payment_options_Id"]);
                                            DataRow[] payment_type = ETGResponse.Tables["payment_types"].Select("payment_options_Id='" + payment_optionId + "'");
                                            int payment_typeId = Convert.ToInt32(payment_type[0]["payment_types_Id"]);

                                            string TaxEssential = "";
                                            //if (ETGResponse.Tables.Contains("tax_data"))
                                            //{
                                            //    DataRow[] drTaxData = ETGResponse.Tables["tax_data"].Select("payment_types_Id='" + payment_typeId + "'");
                                            //    for (int s = 0; s < drTaxData.Length; s++)
                                            //    {
                                            //        string taxdataid = Convert.ToString(drTaxData[s]["tax_data_Id"]);
                                            //        DataRow[] drTaxData1 = ETGResponse.Tables["taxes"].Select("tax_data_Id='" + taxdataid + "'");

                                            //        for (int z = 0; z < drTaxData1.Length; z++)
                                            //        {
                                            //            string Include = Convert.ToString(drTaxData1[z]["included_by_supplier"]);
                                            //            string SubTaxEssential = "";
                                            //            if (Include.ToLower() == "false")
                                            //            {
                                            //                if (z == 0)
                                            //                {
                                            //                    SubTaxEssential = "<br><br><b>TAX INFORMATION FOR CUSTOMER WHICH NEEDS TO PAY AT HOTEL</b><br><br>";
                                            //                }
                                            //                SubTaxEssential = SubTaxEssential + Convert.ToString(drTaxData1[z]["name"]) + " : " + Convert.ToString(drTaxData1[z]["amount"]) + " " + Convert.ToString(drTaxData1[z]["currency_code"]) + "<br>";

                                            //            }
                                            //            if (SubTaxEssential != string.Empty)
                                            //                TaxEssential = TaxEssential + "<br>" + SubTaxEssential;
                                            //        }
                                            //        if (TaxEssential != string.Empty)
                                            //            TaxEssential = TaxEssential + "<br><br>";
                                            //    }
                                            //}
                                            DataRow[] cancellation_penalty = ETGResponse.Tables["cancellation_penalties"].Select("payment_types_Id='" + payment_typeId + "'");
                                            int cancellation_penaltyId = Convert.ToInt32(cancellation_penalty[0]["cancellation_penalties_Id"]);
                                            string free_cancellation_before = Convert.ToString(cancellation_penalty[0]["free_cancellation_before"]);
                                            DataRow[] policies = ETGResponse.Tables["policies"].Select("cancellation_penalties_Id='" + payment_optionId + "'");

                                            string MealName = Convert.ToString(ETGResponse.Tables["rates"].Rows[room]["meal"]);
                                            if (MealName.ToLower() == "nomeal" || MealName == "") //added by ddr 11042024
                                            {
                                                MealName = "Room Only";
                                            }

                                            string RoomName = Convert.ToString(ETGResponse.Tables["rates"].Rows[room]["room_name"]);
                                            double sellingAmount = 0;// Convert.ToDouble(drRooms[room]["daily_prices"]);
                                            double commission = 0;//Convert.ToDouble(drRooms[room]["commission"]);

                                            sellingAmount = Convert.ToDouble(payment_type[0]["amount"]);
                                            double TotalPrice = (sellingAmount + commission) * CurrencyFactorOstrovok;
                                            string RoomTypeId = RoomName + "$$$" + sellingAmount + "$$$" + ItemCode + "$$$" + Currency + "$$$" + XMLnationality + "$$$" + Etg_MatchHas + "$$$" + search_hash;
                                            int night = SearchParam.NoofNights;
                                            double NightRate = TotalPrice / night;
                                            #region RateBreakup
                                            //--MarkUp  
                                            double VatOnRateBrackUp = 0;
                                            //double CompanyBranchBuyingPrice = 0;
                                            //double CompanyBranchSellingPrice = 0;
                                            string info1 = "";
                                            //if (ETGResponse.Tables.Contains("amenities_data") == true)
                                            //{
                                            //    if (ETGResponse.Tables["amenities_data"].Columns.Contains("rates_Id") == true && ETGResponse.Tables["amenities_data"].Columns.Contains("amenities_data_Text") == true)
                                            //    {
                                            //        DataRow[] drRooms_info = ETGResponse.Tables["amenities_data"].Select("rates_Id='" + rates_id + "'");
                                            //        var ids = drRooms_info.AsEnumerable().Select(r => r.Field<string>("amenities_data_Text")).ToList();

                                            //        for (int INF = 0; INF < ids.Count; INF++)
                                            //        {
                                            //            if (INF == 0)
                                            //                info1 = "<b>AMENITY: </b></br></br>";
                                            //            info1 = info1 + Convert.ToString("* " + ids[INF] + ".");
                                            //            if (ids.Count - 1 != INF)
                                            //                info1 = info1 + "</br>";
                                            //        }
                                            //    }
                                            //}

                                            double Pricenew_wl = 0.00;
                                            //NightRate = Common.ProcessHotelRateBreakupPrice_WithMarkup_wl(1, objSearPera.CityID, objSearPera.CountryId, NightRate, B2CMarkup, IsParentId, isPerce, AgentPercentage, AgentMarkupAmount, SubAgentMarkup, ref VatOnRateBrackUp, ref Pricenew_wl, DsAgentDetail);
                                            //NightRate = Common.ProcessHotelRateBreakupPrice_WithMarkup(1, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), NightRate, commonSearch.B2CMarkup, commonSearch.IsParentId, isPerce, AgentPercentage, AgentMarkupAmount, commonSearch.SubAgentMarkup, ref VatOnRateBrackUp);
                                            NightRate = _markup.ProcessHotelRateBreakupPrice_WithMarkup(1, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), NightRate, commonSearch.B2CMarkup, commonSearch.IsParentId, isPerce, AgentPercentage, AgentMarkupAmount, commonSearch.SubAgentMarkup, ref VatOnRateBrackUp);

                                            for (int r = 0; r < SearchParam.NoofNights; r++)
                                            {
                                                DateTime FromDate = Convert.ToDateTime(checkin).Date.AddDays(r);
                                                string rateWeekColumn = Convert.ToString(FromDate.Date.DayOfWeek).Substring(0, 3);
                                                DataRow drOstvRateBreackUp = dtOstvHotelRoomRateBreackUp.NewRow();
                                                drOstvRateBreackUp["SupplierName"] = Provider;
                                                drOstvRateBreackUp["OwnsystemHotelId"] = OwnsystemHotelId;
                                                drOstvRateBreackUp["ContractID"] = 0;
                                                drOstvRateBreackUp["RoomNo"] = 1;
                                                drOstvRateBreackUp["RoomTypeId"] = RoomTypeId;
                                                drOstvRateBreackUp["Meal"] = MealName;
                                                drOstvRateBreackUp["DateOfWeek"] = rateWeekColumn;
                                                drOstvRateBreackUp["MealId"] = "";
                                                drOstvRateBreackUp["Rate"] = NightRate;
                                                drOstvRateBreackUp["OrderNo"] = 1;
                                                drOstvRateBreackUp["RateType"] = "A Rate";
                                                drOstvRateBreackUp["RoomTypeName"] = RoomName;
                                                drOstvRateBreackUp["DateOfWeek"] = rateWeekColumn;
                                                drOstvRateBreackUp["Date"] = FromDate;
                                                drOstvRateBreackUp["BookingStatus"] = "Available";
                                                drOstvRateBreackUp["BoardTypeID"] = "";
                                                drOstvRateBreackUp["VatdiffAmount"] = VatOnRateBrackUp;
                                                dtOstvHotelRoomRateBreackUp.Rows.Add(drOstvRateBreackUp);
                                            }
                                            #endregion
                                            #region Room
                                            DataRow drOstvHotelRoom = dtOstvHotelRoom.NewRow();
                                            drOstvHotelRoom["OwnsystemHotelId"] = OwnsystemHotelId;
                                            drOstvHotelRoom["ContractID"] = 0;
                                            drOstvHotelRoom["UniqueNo"] = OwnsystemHotelId;
                                            drOstvHotelRoom["RoomSequenceNo"] = "";
                                            drOstvHotelRoom["MealName"] = MealName;
                                            drOstvHotelRoom["allocationDetails"] = "";
                                            drOstvHotelRoom["minStay"] = "";
                                            drOstvHotelRoom["DateApplyMinStay"] = "";
                                            drOstvHotelRoom["RoomNo"] = 1;
                                            drOstvHotelRoom["EssentialInformation"] = Essential + TaxEssential + Convert.ToString(info1);
                                            drOstvHotelRoom["IsPolicyDynamic"] = false;
                                            drOstvHotelRoom["MealId"] = "";
                                            drOstvHotelRoom["RoomTypeName"] = RoomName;
                                            drOstvHotelRoom["RoomTypeId"] = RoomTypeId;
                                            drOstvHotelRoom["BookingStatus"] = "Available";
                                            drOstvHotelRoom["AdultsOccupancy"] = TotalAdults;
                                            drOstvHotelRoom["ChildOccupancy"] = TotalChildren;
                                            drOstvHotelRoom["MaxExtrabed"] = 0;
                                            drOstvHotelRoom["RoomTypeCode"] = RoomTypeId;
                                            drOstvHotelRoom["BoardTypeID"] = "";
                                            drOstvHotelRoom["CompanyBuyingPrice"] = Math.Round(TotalPrice, 2);
                                            drOstvHotelRoom["OfferDescription"] = "";
                                            drOstvHotelRoom["offerCode"] = "";
                                            drOstvHotelRoom["GrossWitohutDiscount"] = "0";

                                            //Common.ProcessHotelRoom_WithMarkup_wl(CurrencyFactor, EtgCurrency, false, true, objSearPera.NoofNights, objSearPera.CityID, objSearPera.CountryId, objSearPera.CheckInDate, objSearPera.CheckOutDate, ref TotalPrice, ref drOstvHotelRoom, isPerce, AgentPercentage, AgentMarkupAmount, SubAgentMarkup, B2CMarkup, IsParentId, DsAgentDetail);

                                            //Common.ProcessHotelRoom_WithMarkup(CurrencyFactorRatehawk, Currency, false, true, SearchParam.NoofNights, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), SearchParam.CheckInDate, SearchParam.CheckOutDate, ref TotalPrice, ref drOstvHotelRoom, isPerce, AgentPercentage, AgentMarkupAmount, commonSearch.SubAgentMarkup, commonSearch.B2CMarkup, commonSearch.IsParentId);
                                            _markup.ProcessHotelRoom_WithMarkup(CurrencyFactorOstrovok, Currency, false, true, SearchParam.NoofNights, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), SearchParam.CheckInDate, SearchParam.CheckOutDate, ref TotalPrice, ref drOstvHotelRoom, isPerce, AgentPercentage, AgentMarkupAmount, commonSearch.SubAgentMarkup, commonSearch.B2CMarkup, commonSearch.IsParentId);
                                            drOstvHotelRoom["SupplierName"] = Provider;
                                            dtOstvHotelRoom.Rows.Add(drOstvHotelRoom);
                                            #endregion
                                            #region CancelltionPolicy
                                            // string curDate1 = DateTime.Now.Date.ToString("dd/MM/yyyy");
                                            // string currentdate = (Convert.ToString(curDate1)).Substring(3, 2) + "/" + (Convert.ToString(curDate1)).Substring(0, 2) + "/" + (Convert.ToString(curDate1)).Substring(6, 4);
                                            double ChargeAmount = TotalPrice;
                                            DateTime curDate = DateTime.Now.Date;
                                            double Zero = 0;
                                            //string policiedate = "";
                                            DateTime CancelDate = DateTime.Now.Date;
                                            //for (int bbb = 0; bbb < policies.Length; bbb++)
                                            //{
                                            //    ChargeAmount = Convert.ToDouble(policies[bbb]["amount_show"]) * CurrencyFactorOstrovok;
                                            //    string startAt = Convert.ToString(policies[bbb]["start_at"]);
                                            //    if (startAt != "" && ChargeAmount > 0)
                                            //    {
                                            //        string PolicieDate = Convert.ToString(policies[bbb]["start_at"]);
                                            //        policiedate = (Convert.ToString(PolicieDate)).Substring(8, 2) + "/" + (Convert.ToString(PolicieDate)).Substring(5, 2) + "/" + (Convert.ToString(PolicieDate)).Substring(0, 4);
                                            //        CancelDate = DateTime.ParseExact(policiedate, "dd/MM/yyyy", null);
                                            //    }
                                            //}
                                            //if (CancelDate.AddDays(CutOffDays).Date > curDate)
                                            //{
                                            //    CancelDate = CancelDate.AddDays(CutOffDays).Date;
                                            //}
                                            #region Vat
                                            double VatOnPolicy = 0;
                                            double VatOnPolicyChargeAmount = ChargeAmount;
                                            double CompanyChargeAmount = ChargeAmount;

                                            double MainAgentCharge = ChargeAmount;
                                            double CancelCharge = ChargeAmount;

                                            //ChargeAmount = Common.ProcessHotelCancelationPrice_WithMarkup_wl(ref MainAgentCharge_wl, DsAgentDetail, ref CancelCharge_wl, ref VatOnPolicyChargeAmount, objSearPera.NoofNights, objSearPera.CityID, objSearPera.CountryId, ChargeAmount, isPerce, B2CMarkup, AgentPercentage, AgentMarkupAmount, IsParentId, SubAgentMarkup, ref VatOnPolicy);



                                            //ChargeAmount = Common.ProcessHotelCancelationPrice_WithMarkup(ref MainAgentCharge, SearchParam.NoofNights, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), ChargeAmount, isPerce, commonSearch.B2CMarkup, AgentPercentage, AgentMarkupAmount, commonSearch.IsParentId, commonSearch.SubAgentMarkup, ref VatOnPolicy);
                                            //ChargeAmount = _markup.ProcessHotelCancelationPrice_WithMarkup(ref MainAgentCharge, SearchParam.NoofNights, SearchParam.CityID, SearchParam.CountryID.ToString(), ChargeAmount, isPerce, commonSearch.B2CMarkup, AgentPercentage, AgentMarkupAmount, commonSearch.IsParentId, commonSearch.SubAgentMarkup, ref VatOnPolicy);

                                            #endregion
                                            if (free_cancellation_before != null && free_cancellation_before != "" && free_cancellation_before != string.Empty)
                                            {
                                                DateTime dateString11 = DateTime.Parse(free_cancellation_before, CultureInfo.InvariantCulture).Date;
                                                string formattedDate = dateString11.ToString("dd/MM/yyyy");
                                                CancelDate = DateTime.ParseExact(formattedDate, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                                                if (CancelDate.AddDays(CutOffDays).Date > curDate)
                                                {
                                                    CancelDate = CancelDate.AddDays(CutOffDays).Date;
                                                }
                                                else
                                                {
                                                    CancelDate = DateTime.Now.Date;
                                                }
                                                if (curDate < CancelDate)
                                                {

                                                    dtOstvCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref Zero, dtOstvCancellation.NewRow(), CurrencyFactorOstrovok, Currency, "Ostrovok", Convert.ToString(OwnsystemHotelId), Convert.ToString(1),
                                                                Convert.ToString(RoomTypeId), RoomName, Convert.ToString(""), curDate, CancelDate.AddDays(-1), 0, 0, "", VatOnPolicy, "", "NO", "Yes", "", "", ""));
                                                    dtOstvCancellation.AcceptChanges();

                                                    dtOstvCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref CompanyChargeAmount, dtOstvCancellation.NewRow(), CurrencyFactorOstrovok, Currency, "Ostrovok", Convert.ToString(OwnsystemHotelId), Convert.ToString(1),
                                                                Convert.ToString(RoomTypeId), RoomName, Convert.ToString(""), CancelDate, DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null), ChargeAmount, ChargeAmount, "", VatOnPolicy, "", "NO", "Yes", "", "", ""));
                                                    dtOstvCancellation.AcceptChanges();
                                                }
                                                else
                                                {
                                                    dtOstvCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref CompanyChargeAmount, dtOstvCancellation.NewRow(), CurrencyFactorOstrovok, Currency, "Ostrovok", Convert.ToString(OwnsystemHotelId), Convert.ToString(1),
                                                                Convert.ToString(RoomTypeId), RoomName, Convert.ToString(""), curDate, DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null), ChargeAmount, ChargeAmount, "", VatOnPolicy, "", "NO", "Yes", "", "", ""));
                                                    dtOstvCancellation.AcceptChanges();
                                                }
                                            }
                                            else
                                            {
                                                dtOstvCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref CompanyChargeAmount, dtOstvCancellation.NewRow(), CurrencyFactorOstrovok, Currency, "Ostrovok", Convert.ToString(OwnsystemHotelId), Convert.ToString(1),
                                                            Convert.ToString(RoomTypeId), RoomName, Convert.ToString(""), curDate, DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null), ChargeAmount, ChargeAmount, "", VatOnPolicy, "", "NO", "Yes", "", "", ""));
                                                dtOstvCancellation.AcceptChanges();
                                            }
                                            #endregion

                                        }
                                    }
                                    #endregion
                                }
                                #endregion
                                Options ops = (await _supplierRes.GetCommonRoomForSuppliers(GiataId, commonSearch.tokenData, commonSearch.advancedOptions, FinalMinPrice, drXmlHotels, dtOstvHotelRoom, dtOstvCancellation, dtOstvHotelRoomRateBreackUp, SearchParam));
                                if (ops != null)
                                    options.Add(ops);
                            }
                            #endregion
                            #endregion
                        }
                        #endregion


                        #endregion

                    }

                }
                return options;
            }
            catch (Exception ex)
            {
                Item.ResponseType = "NoCount";
                _ = _exception.Error(ex);

                return options;
            }
        }

        public async Task<List<Options>> Ostrovok_HotelSearchWithRoomswithBookHash(HotelX Item, CommonSearch commonSearch, string Currency, string ResponseData, string CityCode, HotelSearchParameter SearchParam, double CurrencyFactorOstrovok, String Provider, string XMLnationality, string URLandRequest, string UserName, string PassWord, string URL, Int32 SearchTimeOut, DataTable dtROE, string ProccessCutOffTime, Int32 processTimeout)
        {
            System.Diagnostics.Stopwatch timer_room_Ostrovok = new System.Diagnostics.Stopwatch();
            timer_room_Ostrovok.Start();
            List<Options> options = new List<Options>();
            //RoomBookHash.Root OSTV_ResponseRoom = null;
            try
            {
                #region DataTable
                DataTable DtHotel = _commonMethod.GenerateDataTableNewV2();
                #endregion
                if (ResponseData != string.Empty)
                {
                    if (ResponseData == string.Empty)
                    {
                        Item.SupplierResponseError = ResponseData;
                        Item.ResponseStatus = 204;
                        Item.ResponseType = "NoCount";
                        return options;
                    }

                    ETGSearchResponse.Root ETGResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<ETGSearchResponse.Root>(ResponseData);

                    if (ETGResponse == null || ETGResponse.data == null || ETGResponse.data.hotels == null || ETGResponse.data.hotels.Count == 0)
                    {
                        Item.SupplierResponseError = ResponseData;
                        Item.ResponseStatus = 404;
                        Item.ResponseType = "NoCount";
                        return options;
                    }

                    string Essential = "";
                    double FinalMinPrice = 0;

                    for (int i = 0; i < ETGResponse.data.hotels.Count; i++) //Hotel Loop
                    {
                        double Total = 0;

                        Item.ResponseType = "Success";
                        if (ProccessCutOffTime == "1" && timer_room_Ostrovok.ElapsedMilliseconds > processTimeout)
                        {
                            timer_room_Ostrovok.Stop();
                            break;
                        }
                        Item.ResponseType = "Success";

                        string ItemCode = Convert.ToString(ETGResponse.data.hotels[i].id);
                        string Error = "";
                        string URLRoom = _supplierConfig.ETG.hotelinfourl;// "https://api.worldota.net/api/b2b/v3/search/hp/";

                        if (ETGResponse.data.hotels[i].rates != null)
                        {

                            #region "When not Need Room" 
                            if (SearchParam.Filters.IsShowRooms == 0)
                            {

                                for (int g = 0; g < ETGResponse.data.hotels[i].rates.Count; g++)
                                {


                                    double sellingAmount = Convert.ToDouble(ETGResponse.data.hotels[i].rates[g].payment_options.payment_types[0].amount);
                                    Currency = Convert.ToString(ETGResponse.data.hotels[i].rates[g].payment_options.payment_types[0].currency_code);
                                    if (Currency != commonSearch.tokenData.SiteCurrency.ToLower())
                                    {
                                        CurrencyFactorOstrovok = _commonMethod.GetROEByCurrency(Currency, dtROE);
                                    }
                                    double HotelPrice = sellingAmount * CurrencyFactorOstrovok;

                                    if (g == 0)
                                        Total = HotelPrice;
                                    else
                                    {
                                        if (HotelPrice < Total)
                                            Total = HotelPrice;
                                    }
                                }

                                #region "Hotel Code Block "


                                DataRow drXmlHotel = DtHotel.NewRow();
                                //string InternalHotelId = Convert.ToString(ItemCode);
                                drXmlHotel["XMLHotelCode"] = ItemCode;
                                drXmlHotel["SupplierName"] = Provider;
                                drXmlHotel["OfficeID"] = 0;
                                drXmlHotel["ContractID"] = 0;
                                drXmlHotel["Rating"] = 0;
                                drXmlHotel["Reviews"] = 0;
                                drXmlHotel["UniqueNo"] = ItemCode;
                                drXmlHotel["IsShowSupplier"] = false;
                                drXmlHotel["BookingStatus"] = "Available";
                                drXmlHotel["XMLCity"] = CityCode;
                                drXmlHotel["SupplierCurrency"] = Currency;
                                drXmlHotel["FinalSellingPrice"] = Total;
                                drXmlHotel["SellingPrice"] = Total;
                                DtHotel.Rows.Add(drXmlHotel);
                                double TotalCharges = Total;

                                #endregion
                                if (TotalCharges > 0)
                                {
                                    #region "Room code Block"
                                    bool willAddOrNot = true;
                                    Int64 RaynaID = 0;
                                    Int64 GiataId = 0;
                                    int Rating = 0;
                                    Int64 OwnsystemHotelId = 0;
                                    double MinPrice = Total;
                                    double CompanyBranchBuyingPrice = 0;
                                    double CompanyBranchSellingPrice = 0;
                                    //int AgentBranchId = commonSearch.tokenData.ABranchId;
                                    //HotelXML_Suppliers.GetHotelDataCommon(ref MinPrice, ref willAddOrNot, ref OwnsystemHotelId, ref RaynaID, ref GiataId, ref Rating, ref drXmlHotel, commonSearch.AgentId, commonSearch.TariffID, commonSearch.dsAgentDetails, commonSearch.supplierGiataMaster, SearchParam, commonSearch.dtCountryCityMarkup, commonSearch.B2CMarkup, commonSearch.IsParentId, commonSearch.SubAgentMarkup, commonSearch.SearchByHotel, commonSearch.HotelIDS);
                                    _markup.GetHotelDataCommon(ref MinPrice, ref willAddOrNot, ref OwnsystemHotelId, ref RaynaID, ref GiataId, ref Rating, ref drXmlHotel, commonSearch.AgentId, commonSearch.TariffID, commonSearch.dsAgentDetails, commonSearch.supplierGiataMaster, SearchParam, commonSearch.dtCountryCityMarkup, commonSearch.B2CMarkup, commonSearch.IsParentId, commonSearch.SubAgentMarkup, commonSearch.SearchByHotel, commonSearch.HotelIDS);
                                    //HotelXML_Suppliers.GetHotelDataCommon(ref MinPrice, ref willAddOrNot, ref OwnsystemHotelId, ref RaynaID, ref GiataId, ref Rating, ref drXmlHotel, commonSearch.AgentId, commonSearch.TariffID, commonSearch.dsAgentDetails, commonSearch.supplierGiataMaster, SearchParam, commonSearch.dtCountryCityMarkup, commonSearch.B2CMarkup, commonSearch.IsParentId, commonSearch.SubAgentMarkup, commonSearch.SearchByHotel, commonSearch.HotelIDS);
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
                            #region "When Need Rooms"
                            else
                            {
                                #region Hotels
                                #region "Hotel Block here"
                                DataRow drXmlHotels = DtHotel.NewRow();
                                drXmlHotels["XMLHotelCode"] = ItemCode;
                                drXmlHotels["HotelName"] = "";
                                drXmlHotels["SupplierName"] = Provider;
                                drXmlHotels["OfficeID"] = 0;
                                drXmlHotels["ContractID"] = 0;
                                drXmlHotels["Rating"] = 0;
                                drXmlHotels["Reviews"] = 0;
                                drXmlHotels["SupplierCurrency"] = Currency;
                                drXmlHotels["UniqueNo"] = ItemCode;
                                drXmlHotels["IsShowSupplier"] = false;
                                drXmlHotels["BookingStatus"] = "Available";
                                drXmlHotels["FinalSellingPrice"] = 1;
                                drXmlHotels["SellingPrice"] = 1;
                                drXmlHotels["IsRecomondedHotel"] = false;
                                DtHotel.Rows.Add(drXmlHotels);
                                #endregion
                                #region "GetRooms for this hotel"
                                bool willAddOrNot = true;
                                int Rating = 0;
                                Int64 RaynaID = 0;
                                Int64 GiataId = 0;
                                int RatingHotel = 0;
                                Int64 OwnsystemHotelId = 0;
                                double MinPrice = 1;
                                double CompanyBranchBuyingPrice = 0;
                                double CompanyBranchSellingPrice = 0;
                                //int AgentBranchId = commonSearch.tokenData.ABranchId;
                                _markup.GetHotelDataCommon(ref MinPrice, ref willAddOrNot, ref OwnsystemHotelId, ref RaynaID, ref GiataId, ref Rating, ref drXmlHotels, commonSearch.AgentId, commonSearch.TariffID, commonSearch.dsAgentDetails, commonSearch.supplierGiataMaster, SearchParam, commonSearch.dtCountryCityMarkup, commonSearch.B2CMarkup, commonSearch.IsParentId, commonSearch.SubAgentMarkup, commonSearch.SearchByHotel, commonSearch.HotelIDS);
                                if (willAddOrNot)
                                {
                                    DataTable dtOstvHotelRoom = _commonMethod.GenerateRoomtypeDataTableNewV2();
                                    DataTable dtOstvCancellation = _commonMethod.GenerateCancellationDataTableNewV2();
                                    DataTable dtOstvHotelRoomRateBreackUp = _commonMethod.GenerateoomRateBreackUpDataTableNewV2();
                                    int CutOffDays = CacheMaster.CutOffDays;
                                    CutOffDays = _commonMethod.GetCutOffDays(commonSearch.dtSuplierMarkup, Provider);
                                    //int CutOffDays = XMLHotelBookingCommon.CutOffDays;//ttt
                                    //CutOffDays = _commonMethod.GetCutOffDays(commonSearch.dtSuplierMarkup, Provider);
                                    #region "Room Logic"
                                    if (SearchParam.Filters.IsShowRooms == 1)
                                    {
                                        double AgentPercentage = 0.0;

                                        double AgentMarkupAmount = 0.0;
                                        Boolean isPerce = true;

                                        double AgentCompanyPercentage = Convert.ToDouble(SearchParam.AgentCompanyPercentage);
                                        bool IsB2C = Convert.ToBoolean(Convert.ToByte(SearchParam.IsB2BOrB2C));
                                        bool blnIsShowSupplier = false;
                                        //_markup.getAgetMarkupSetting(Provider, GiataId.ToString(), commonSearch.dtSuplierMarkup, commonSearch.dtMarkupDetails, ref AgentPercentage, ref AgentMarkupAmount, ref blnIsShowSupplier, ref isPerce, commonSearch.AgentId, RatingHotel, SearchParam.CityID, SearchParam.CountryID.ToString(), Convert.ToString(SearchParam.CheckInDate));
                                        _markup.getAgetMarkupSettingNew_new(Provider, Convert.ToString(RaynaID), commonSearch.dtSuplierMarkup, commonSearch.dsMarkupDetails, ref AgentPercentage, ref AgentMarkupAmount, ref blnIsShowSupplier, ref isPerce, commonSearch.AgentId, RatingHotel, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), SearchParam.CheckInDate);
                                        int NoNight = SearchParam.NoofNights;
                                        int NoOfRoom = SearchParam.Rooms.Count();

                                        string checkin = (SearchParam.CheckInDate).Substring(6, 4) + "-" + (SearchParam.CheckInDate).Substring(3, 2) + "-" + (SearchParam.CheckInDate).Substring(0, 2);
                                        string checkout = (SearchParam.CheckOutDate).Substring(6, 4) + "-" + (SearchParam.CheckOutDate).Substring(3, 2) + "-" + (SearchParam.CheckOutDate).Substring(0, 2);

                                        int TotalAdults = 0;
                                        int TotalChildren = 0;
                                        for (int K = 0; K < SearchParam.Rooms.Count; K++)
                                        {
                                            TotalAdults = Convert.ToInt32(SearchParam.Rooms[K].NoofAdults);
                                            TotalChildren = Convert.ToInt32(SearchParam.Rooms[K].NoofChild);
                                        }
                                        #region Supplier Room Code block
                                        if (ETGResponse.data.hotels[i].rates != null)
                                        {
                                            for (int room = 0; room < ETGResponse.data.hotels[i].rates.Count; room++) // Room Loop
                                            {
                                                if (ProccessCutOffTime == "1" && timer_room_Ostrovok.ElapsedMilliseconds > processTimeout)
                                                {
                                                    timer_room_Ostrovok.Stop();
                                                    break;
                                                }
                                                string Etg_MatchHas = Convert.ToString(ETGResponse.data.hotels[i].rates[room].match_hash);
                                                string search_hash = Convert.ToString(ETGResponse.data.hotels[i].rates[room].search_hash);


                                                string TaxEssential = "";
                                                if (ETGResponse.data.hotels[i].rates[room].payment_options != null)
                                                {
                                                    if (ETGResponse.data.hotels[i].rates[room].payment_options.payment_types.Count > 0)
                                                    {
                                                        for (int p = 0; p < ETGResponse.data.hotels[i].rates[room].payment_options.payment_types.Count; p++)
                                                        {
                                                            if (ETGResponse.data.hotels[i].rates[room].payment_options.payment_types[p].tax_data != null)
                                                            {
                                                                if (ETGResponse.data.hotels[i].rates[room].payment_options.payment_types[p].tax_data.taxes != null)
                                                                {
                                                                    for (int t = 0; t < ETGResponse.data.hotels[i].rates[room].payment_options.payment_types[p].tax_data.taxes.Count; t++)
                                                                    {
                                                                        bool Include = ETGResponse.data.hotels[i].rates[room].payment_options.payment_types[p].tax_data.taxes[t].included_by_supplier;
                                                                        string SubTaxEssential = "";
                                                                        if (Include == false)
                                                                        {
                                                                            if (t == 0)
                                                                            {
                                                                                SubTaxEssential = "<br><br><b>TAX INFORMATION FOR CUSTOMER WHICH NEEDS TO PAY AT HOTEL</b><br><br>";
                                                                            }
                                                                            string Name = ETGResponse.data.hotels[i].rates[room].payment_options.payment_types[p].tax_data.taxes[t].name;
                                                                            string amouth = ETGResponse.data.hotels[i].rates[room].payment_options.payment_types[p].tax_data.taxes[t].amount;
                                                                            string curruncy = ETGResponse.data.hotels[i].rates[room].payment_options.payment_types[p].tax_data.taxes[t].currency_code;
                                                                            SubTaxEssential = SubTaxEssential + Name + " : " + Convert.ToString(amouth) + " " + Convert.ToString(curruncy) + "<br>";
                                                                        }
                                                                        if (SubTaxEssential != string.Empty)
                                                                            TaxEssential = TaxEssential + "<br>" + SubTaxEssential;
                                                                    }
                                                                }
                                                                if (TaxEssential != string.Empty)
                                                                    TaxEssential = TaxEssential + "<br><br>";
                                                            }
                                                        }
                                                    }
                                                }
                                                string info1 = "";
                                                if (ETGResponse.data.hotels[i].rates[room].amenities_data != null)
                                                {
                                                    for (int INF = 0; INF < ETGResponse.data.hotels[i].rates[room].amenities_data.Count; INF++)
                                                    {
                                                        if (INF == 0)
                                                            info1 = "<b>AMENITY: </b></br></br>";
                                                        info1 = info1 + Convert.ToString("* " + ETGResponse.data.hotels[i].rates[room].amenities_data[INF] + ".");
                                                        if (ETGResponse.data.hotels[i].rates[room].amenities_data.Count - 1 != INF)
                                                            info1 = info1 + "</br>";
                                                    }
                                                }
                                                string MealName = Convert.ToString(Convert.ToString(ETGResponse.data.hotels[i].rates[room].meal));
                                                if (MealName.ToLower() == "nomeal" || MealName == "") //added by ddr 11042024
                                                {
                                                    MealName = "Room Only";
                                                }

                                                string RoomName = Convert.ToString(ETGResponse.data.hotels[i].rates[room].room_name);
                                                double sellingAmount = 0;// Convert.ToDouble(drRooms[room]["daily_prices"]);
                                                double commission = 0;//Convert.ToDouble(drRooms[room]["commission"]);

                                                sellingAmount = Convert.ToDouble(ETGResponse.data.hotels[i].rates[room].payment_options.payment_types[0].amount);
                                                double companybuying = sellingAmount * CurrencyFactorOstrovok;
                                                double TotalPrice = (sellingAmount + commission) * CurrencyFactorOstrovok;
                                                string RoomTypeId = RoomName + "$$$" + sellingAmount + "$$$" + ItemCode + "$$$" + Currency + "$$$" + XMLnationality + "$$$" + Etg_MatchHas + "$$$" + search_hash;
                                                int night = SearchParam.NoofNights;
                                                double NightRate = TotalPrice / night;
                                                #region RateBreakup
                                                //--MarkUp  
                                                //double VatOnRateBrackUp = 0;
                                                ////double CompanyBranchBuyingPrice = 0;
                                                ////double CompanyBranchSellingPrice = 0;
                                                //string info1 = "";


                                                //double Pricenew_wl = 0.00;

                                                //NightRate = _markup.ProcessHotelRateBreakupPrice_WithMarkup(1, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), NightRate, commonSearch.B2CMarkup, commonSearch.IsParentId, isPerce, AgentPercentage, AgentMarkupAmount, commonSearch.SubAgentMarkup, ref VatOnRateBrackUp);

                                                //for (int r = 0; r < SearchParam.NoofNights; r++)
                                                //{
                                                //    DateTime FromDate = Convert.ToDateTime(checkin).Date.AddDays(r);
                                                //    string rateWeekColumn = Convert.ToString(FromDate.Date.DayOfWeek).Substring(0, 3);
                                                //    DataRow drOstvRateBreackUp = dtOstvHotelRoomRateBreackUp.NewRow();
                                                //    drOstvRateBreackUp["SupplierName"] = Provider;
                                                //    drOstvRateBreackUp["OwnsystemHotelId"] = OwnsystemHotelId;
                                                //    drOstvRateBreackUp["ContractID"] = 0;
                                                //    drOstvRateBreackUp["RoomNo"] = 1;
                                                //    drOstvRateBreackUp["RoomTypeId"] = RoomTypeId;
                                                //    drOstvRateBreackUp["Meal"] = MealName;
                                                //    drOstvRateBreackUp["DateOfWeek"] = rateWeekColumn;
                                                //    drOstvRateBreackUp["MealId"] = "";
                                                //    drOstvRateBreackUp["Rate"] = NightRate;
                                                //    drOstvRateBreackUp["OrderNo"] = 1;
                                                //    drOstvRateBreackUp["RateType"] = "A Rate";
                                                //    drOstvRateBreackUp["RoomTypeName"] = RoomName;
                                                //    drOstvRateBreackUp["DateOfWeek"] = rateWeekColumn;
                                                //    drOstvRateBreackUp["Date"] = FromDate;
                                                //    drOstvRateBreackUp["BookingStatus"] = "Available";
                                                //    drOstvRateBreackUp["BoardTypeID"] = "";
                                                //    drOstvRateBreackUp["VatdiffAmount"] = VatOnRateBrackUp;
                                                //    dtOstvHotelRoomRateBreackUp.Rows.Add(drOstvRateBreackUp);
                                                //}
                                                #endregion
                                                #region Room
                                                DataRow drOstvHotelRoom = dtOstvHotelRoom.NewRow();
                                                drOstvHotelRoom["OwnsystemHotelId"] = OwnsystemHotelId;
                                                drOstvHotelRoom["ContractID"] = 0;
                                                drOstvHotelRoom["UniqueNo"] = OwnsystemHotelId;
                                                drOstvHotelRoom["RoomSequenceNo"] = "";
                                                drOstvHotelRoom["MealName"] = MealName;
                                                drOstvHotelRoom["allocationDetails"] = "";
                                                drOstvHotelRoom["minStay"] = "";
                                                drOstvHotelRoom["DateApplyMinStay"] = "";
                                                drOstvHotelRoom["RoomNo"] = 1;
                                                drOstvHotelRoom["EssentialInformation"] = Essential + TaxEssential + Convert.ToString(info1);
                                                drOstvHotelRoom["IsPolicyDynamic"] = false;
                                                drOstvHotelRoom["MealId"] = "";
                                                drOstvHotelRoom["RoomTypeName"] = RoomName;
                                                drOstvHotelRoom["RoomTypeId"] = RoomTypeId;
                                                drOstvHotelRoom["BookingStatus"] = "Available";
                                                drOstvHotelRoom["AdultsOccupancy"] = TotalAdults;
                                                drOstvHotelRoom["ChildOccupancy"] = TotalChildren;
                                                drOstvHotelRoom["MaxExtrabed"] = 0;
                                                drOstvHotelRoom["RoomTypeCode"] = RoomTypeId;
                                                drOstvHotelRoom["BoardTypeID"] = "";
                                                drOstvHotelRoom["CompanyBuyingPrice"] = Math.Round(TotalPrice, 2);
                                                drOstvHotelRoom["OfferDescription"] = "";
                                                drOstvHotelRoom["offerCode"] = "";
                                                drOstvHotelRoom["GrossWitohutDiscount"] = "0";


                                                //Common.ProcessHotelRoom_WithMarkup(CurrencyFactorRatehawk, Currency, false, true, SearchParam.NoofNights, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), SearchParam.CheckInDate, SearchParam.CheckOutDate, ref TotalPrice, ref drOstvHotelRoom, isPerce, AgentPercentage, AgentMarkupAmount, commonSearch.SubAgentMarkup, commonSearch.B2CMarkup, commonSearch.IsParentId);
                                                _markup.ProcessHotelRoom_WithMarkup(CurrencyFactorOstrovok, Currency, false, true, SearchParam.NoofNights, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), SearchParam.CheckInDate, SearchParam.CheckOutDate, ref TotalPrice, ref drOstvHotelRoom, isPerce, AgentPercentage, AgentMarkupAmount, commonSearch.SubAgentMarkup, commonSearch.B2CMarkup, commonSearch.IsParentId);
                                                drOstvHotelRoom["SupplierName"] = Provider;
                                                dtOstvHotelRoom.Rows.Add(drOstvHotelRoom);
                                                #endregion
                                                #region CancelltionPolicy
                                                string curDate1 = DateTime.Now.Date.ToString("dd/MM/yyyy");
                                                string currentdate = (Convert.ToString(curDate1)).Substring(3, 2) + "/" + (Convert.ToString(curDate1)).Substring(0, 2) + "/" + (Convert.ToString(curDate1)).Substring(6, 4);
                                                double ChargeAmount = TotalPrice;
                                                DateTime curDate = DateTime.Now.Date;
                                                string policiedate = "";
                                                DateTime CancelDate = DateTime.ParseExact(currentdate, "MM/dd/yyyy", null);

                                                //if (ETGResponse.data.hotels[i].rates[room].payment_options.payment_types[0].cancellation_penalties != null || ETGResponse.data.hotels[i].rates[room].payment_options.payment_types[0].cancellation_penalties.policies != null)
                                                //{
                                                //    for (int bbb = 0; bbb < ETGResponse.data.hotels[i].rates[room].payment_options.payment_types[0].cancellation_penalties.policies.Count; bbb++)
                                                //    {
                                                //        ChargeAmount = Convert.ToDouble(ETGResponse.data.hotels[i].rates[room].payment_options.payment_types[0].cancellation_penalties.policies[bbb].amount_show) * CurrencyFactorOstrovok;
                                                //        string startAt = Convert.ToString(ETGResponse.data.hotels[i].rates[room].payment_options.payment_types[0].cancellation_penalties.policies[bbb].start_at);
                                                //        if (!String.IsNullOrEmpty(startAt) && ChargeAmount > 0)
                                                //        {
                                                //            string PolicieDate = startAt;// Convert.ToString(policies[bbb]["start_at"]);
                                                //            policiedate = (Convert.ToString(PolicieDate)).Substring(8, 2) + "/" + (Convert.ToString(PolicieDate)).Substring(5, 2) + "/" + (Convert.ToString(PolicieDate)).Substring(0, 4);
                                                //            CancelDate = DateTime.ParseExact(policiedate, "dd/MM/yyyy", null);
                                                //        }
                                                //    }
                                                //}
                                                string cancellation = "";
                                                if (ETGResponse.data.hotels[i].rates[room].payment_options.payment_types[0].cancellation_penalties.free_cancellation_before == null)
                                                {
                                                    CancelDate = DateTime.Now.Date;
                                                    cancellation = "nonref";
                                                }
                                                else
                                                {
                                                    DateTime free_cancellation_before = Convert.ToDateTime(ETGResponse.data.hotels[i].rates[room].payment_options.payment_types[0].cancellation_penalties.free_cancellation_before);
                                                    CancelDate = free_cancellation_before;// DateTime.ParseExact(free_cancellation_before, "dd/MM/yyyy", null);
                                                    cancellation = Convert.ToString(free_cancellation_before);
                                                    cancellation = free_cancellation_before.ToString("dd/MM/yyyy"); //added on 27,06
                                                }

                                                if (CancelDate.AddDays(CutOffDays).Date > curDate)
                                                {
                                                    CancelDate = CancelDate.AddDays(CutOffDays).Date;
                                                }
                                                else
                                                {
                                                    CancelDate = DateTime.Now.Date;
                                                }
                                                #region Vat
                                                double VatOnPolicy = 0;
                                                double VatOnPolicyChargeAmount = ChargeAmount;
                                                double CompanyChargeAmount = ChargeAmount;

                                                double MainAgentCharge = ChargeAmount;
                                                double CancelCharge = ChargeAmount;

                                                //ChargeAmount = _markup.ProcessHotelCancelationPrice_WithMarkup(ref MainAgentCharge, SearchParam.NoofNights, SearchParam.CityID, SearchParam.CountryID.ToString(), ChargeAmount, isPerce, commonSearch.B2CMarkup, AgentPercentage, AgentMarkupAmount, commonSearch.IsParentId, commonSearch.SubAgentMarkup, ref VatOnPolicy);

                                                #endregion
                                                //if (curDate < CancelDate && policiedate != "" && policiedate != string.Empty)
                                                if (curDate < CancelDate)
                                                {
                                                    double zero = 0;
                                                    dtOstvCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref zero, dtOstvCancellation.NewRow(), CurrencyFactorOstrovok, Currency, Provider, Convert.ToString(OwnsystemHotelId), Convert.ToString(1),
                                                                Convert.ToString(RoomTypeId), RoomName, Convert.ToString(""), curDate, CancelDate.AddDays(-1), 0, 0, "", VatOnPolicy, "", "NO", cancellation, "", "", ""));
                                                    dtOstvCancellation.AcceptChanges();

                                                    dtOstvCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref companybuying, dtOstvCancellation.NewRow(), CurrencyFactorOstrovok, Currency, Provider, Convert.ToString(OwnsystemHotelId), Convert.ToString(1),
                                                                Convert.ToString(RoomTypeId), RoomName, Convert.ToString(""), CancelDate, DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null), companybuying, ChargeAmount, "", VatOnPolicy, "", "NO", cancellation, "", "", ""));
                                                    dtOstvCancellation.AcceptChanges();
                                                }
                                                else
                                                {
                                                    dtOstvCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref companybuying, dtOstvCancellation.NewRow(), CurrencyFactorOstrovok, Currency, Provider, Convert.ToString(OwnsystemHotelId), Convert.ToString(1),
                                                                Convert.ToString(RoomTypeId), RoomName, Convert.ToString(""), curDate, DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null), companybuying, ChargeAmount, "", VatOnPolicy, "", "NO", cancellation, "", "", ""));
                                                    dtOstvCancellation.AcceptChanges();
                                                }
                                                #endregion

                                            }
                                        }
                                        #endregion
                                    }
                                    #endregion
                                    Options ops = (await _supplierRes.GetCommonRoomForSuppliers(GiataId, commonSearch.tokenData, commonSearch.advancedOptions, FinalMinPrice, drXmlHotels, dtOstvHotelRoom, dtOstvCancellation, dtOstvHotelRoomRateBreackUp, SearchParam));
                                    if (ops != null)
                                        options.Add(ops);
                                }
                                #endregion
                                #endregion
                            }
                            #endregion



                            #endregion
                        }
                    }

                }
                try
                {
                    timer_room_Ostrovok.Stop();
                    Item.CodeProcessingTimeSupplier = timer_room_Ostrovok.ElapsedMilliseconds;
                }
                catch
                {

                }
                

                return options;
            }
            catch (Exception ex)
            {
                Item.ResponseStatus = 404;
                Item.ResponseType = "NoCount";
                _ = _exception.Error(ex);

                return options;
            }
        }

        public async Task<string> RateHawk_EssentialInfo(HotelSearchParameter SearchPara, string Supplier, string xmlhotelcode, string UserName, string PassWord, string Currency, string UserID, string RoomTypeId,string RatehawkURL, string SearchKey)
        {
            string EssentialInfo = "";
            try
            {
                string reqId = Convert.ToString(DateTime.Now.Ticks);
                string error = "";
                string[] spitid = RoomTypeId.Split(new String[] { "$$$" }, StringSplitOptions.None);
                string Nationality = spitid[4].ToString();
                string key = Convert.ToBase64String(Encoding.ASCII.GetBytes(UserName + ":" + PassWord));
                //string strHost = WebConfigurationManager.AppSettings["RatehawkURL"] + "/search/hp/";
                //string strHost = WebConfigurationManager.AppSettings["RatehawkURL"] + "/search/serp/hotels/";
                //string strHost = RatehawkURL + ETGInfo.search_serp_hotels;
                string strHost = RatehawkURL + _supplierConfig.ETG.search_serp_hotels;
                //string strHost = WebConfigurationManager.AppSettings["RatehawkURL"] + "/search/serp/region/";
                string UniqueNo = "";
                bool IsRooom = false;
                int IsHit = 0;
                string Request = "";// GetETG_Multiple_RoomOnly(SearchPara, Nationality, UserName, PassWord, UserID, xmlhotelcode, IsHit, Convert.ToString(SearchPara.CityID), Currency, IsRooom);
                string DataaResponse = "";
                ResponseDto responseDto = await GetEtgHotelDataResponse(RatehawkURL + "/hotel/info/?data={%22id%22:%22" + xmlhotelcode + "%22,%22language%22:%22en%22}", key);
                string DataResponse = responseDto.Response;
                error = responseDto.Error;


                string Essential = "";
                string info1 = "";

                //ExManager.Exception.LogException("GetEtgHotelDataResponse" + DataResponse);
                string TaxEssential = "";
                _ = _commonMethod.XMLRequestResponseTrack(RatehawkURL, DataResponse, Supplier, "Proper", reqId, SearchPara.SearchKey);
                if (DataResponse != string.Empty)
                {
                    //ExManager.Exception.LogException("GetEtgHotelDataResponse 123" + DataResponse);

                    DataSet EtgResponse1 = new DataSet();
                    XmlDocument doc1 = Newtonsoft.Json.JsonConvert.DeserializeXmlNode("{\"Row\":" + DataResponse + "}", "root");
                    String Data2 = doc1.InnerXml.Substring(doc1.InnerXml.IndexOf("<metapolicy_struct"), doc1.InnerXml.IndexOf("</metapolicy_struct>") - doc1.InnerXml.IndexOf("<metapolicy_struct")) + "</metapolicy_struct>";
                    Data2 = "<Root>" + Data2 + "</Root>";
                    XmlReader xmlReader1 = new XmlNodeReader(doc1);
                    EtgResponse1.ReadXml(xmlReader1);

                    DataSet HotelBedsResponse = new DataSet();
                    byte[] buffer = Encoding.UTF8.GetBytes(Data2);
                    using (MemoryStream stream = new MemoryStream(buffer))
                    {
                        XmlReader reader = XmlReader.Create(stream);
                        HotelBedsResponse.ReadXml(reader);
                    }
                    {
                        if (EtgResponse1.Tables.Contains("policy_struct"))
                        {
                            for (int x = 0; x < EtgResponse1.Tables["policy_struct"].Rows.Count; x++)
                            {
                                string Title = ""; string TitleId = "";
                                if (EtgResponse1.Tables["policy_struct"].Columns.Contains("title"))
                                {
                                    Title = Convert.ToString(EtgResponse1.Tables["policy_struct"].Rows[x]["title"]);
                                }

                                if (EtgResponse1.Tables["policy_struct"].Columns.Contains("policy_struct_Id"))
                                {
                                    TitleId = Convert.ToString(EtgResponse1.Tables["policy_struct"].Rows[x]["policy_struct_Id"]);
                                    if (EtgResponse1.Tables.Contains("paragraphs"))
                                    {
                                        DataRow[] drparagraphs = EtgResponse1.Tables["paragraphs"].Select("policy_struct_Id='" + TitleId + "'");
                                        if (drparagraphs.Length > 0)
                                        {
                                            if (x == 0)
                                            {
                                                Essential = "<b>IMPORTANT INFORMATION FOR CUSTOMER</b>";
                                            }
                                            Essential = Essential + "<br><br><b>" + Title + "</b><br><br>";
                                            for (int y = 0; y < drparagraphs.Length; y++)
                                            {
                                                string Info = Convert.ToString(drparagraphs[y]["paragraphs_Text"]);
                                                if (Info.Length < 5)
                                                    continue;
                                                if (Info != string.Empty)
                                                {
                                                    Essential = Essential + "* " + Info + "<br>";
                                                }

                                            }
                                        }
                                    }
                                }
                                else if (EtgResponse1.Tables["policy_struct"].Columns.Contains("paragraphs"))
                                {

                                    if (x == 0)
                                    {
                                        Essential = "<b>IMPORTANT INFORMATION FOR CUSTOMER</b>";
                                    }
                                    Essential = Essential + "<br><br><b>" + Title + "</b><br><br>";
                                    if (EtgResponse1.Tables["policy_struct"].Columns.Contains("paragraphs"))
                                    {
                                        string Info = Convert.ToString(EtgResponse1.Tables["policy_struct"].Rows[x]["paragraphs"]);

                                        if (Info != string.Empty)
                                        {
                                            Essential = Essential + "* " + Info + "<br>";
                                        }
                                    }
                                }

                                //Essential = Essential + "<br><br>";
                            }
                        }
                    }
                    if (HotelBedsResponse.Tables.Count > 0)
                    {
                        //Essential = "";
                        string metapolicyID = "";
                        if (HotelBedsResponse.Tables[0].Columns.Contains("metapolicy_struct_Id") == true)
                        {
                            for (int mpii = 0; mpii < HotelBedsResponse.Tables[0].Rows.Count; mpii++)
                            {
                                metapolicyID = Convert.ToString(HotelBedsResponse.Tables[0].Rows[mpii]["metapolicy_struct_Id"]);
                                if (metapolicyID != "")
                                {
                                    for (int mp = 1; mp < HotelBedsResponse.Tables.Count; mp++)
                                    {

                                        if (HotelBedsResponse.Tables[mp].Columns.Contains("metapolicy_struct_Id"))
                                        {
                                            DataRow[] drTabl1 = HotelBedsResponse.Tables[mp].Select("metapolicy_struct_Id='" + metapolicyID + "'");
                                            if (drTabl1.Length > 0)
                                            {
                                                string MetaName = "";// string metacurrency = ""; double MetaPrice = 0.00; string metafeetype = ""; string metapriceperunit = ""; string agestart = "";
                                                MetaName = Convert.ToString(HotelBedsResponse.Tables[mp].TableName);

                                                for (int lll = 0; lll < drTabl1.Length; lll++)
                                                {
                                                    string info = "";
                                                    info = "<br/><B>" + "Metapolicy" + "</B>";
                                                    info = info + "<br/>**" + MetaName;
                                                    if (HotelBedsResponse.Tables[MetaName].Columns.Contains("fee_type") == true)
                                                    {
                                                        string metafeetype = "";
                                                        metafeetype = Convert.ToString(drTabl1[lll]["fee_type"]);
                                                        info = info + "<br/>* fee_type : " + metafeetype;
                                                    }
                                                    if (HotelBedsResponse.Tables[MetaName].Columns.Contains("pets_type") == true)
                                                    {
                                                        string pets_type = "";
                                                        pets_type = Convert.ToString(drTabl1[lll]["pets_type"]);
                                                        info = info + "<br/>* pets_type : " + pets_type;
                                                    }
                                                    if (HotelBedsResponse.Tables[MetaName].Columns.Contains("visa_support") == true)
                                                    {
                                                        string visa_support = "";
                                                        visa_support = Convert.ToString(drTabl1[lll]["visa_support"]);
                                                        info = info + "<br/>* pets_type : " + visa_support;
                                                    }
                                                    if (HotelBedsResponse.Tables[MetaName].Columns.Contains("availability") == true)
                                                    {
                                                        string availability = "";
                                                        availability = Convert.ToString(drTabl1[lll]["availability"]);
                                                        info = info + "<br/>* availability : " + availability;
                                                    }
                                                    if (HotelBedsResponse.Tables[MetaName].Columns.Contains("day_period") == true)
                                                    {
                                                        string day_period = "";
                                                        day_period = Convert.ToString(drTabl1[lll]["day_period"]);
                                                        info = info + "<br/>* day_period : " + day_period;
                                                    }
                                                    if (HotelBedsResponse.Tables[MetaName].Columns.Contains("time") == true)
                                                    {
                                                        string time = "";
                                                        time = Convert.ToString(drTabl1[lll]["time"]);
                                                        info = info + "<br/>* time : " + time;
                                                    }
                                                    if (HotelBedsResponse.Tables[MetaName].Columns.Contains("amount") == true)
                                                    {
                                                        string amount = "";
                                                        amount = Convert.ToString(drTabl1[lll]["amount"]);
                                                        info = info + "<br/>* amount : " + amount;
                                                    }
                                                    if (HotelBedsResponse.Tables[MetaName].Columns.Contains("internet_type") == true)
                                                    {
                                                        string internet_type = "";
                                                        internet_type = Convert.ToString(drTabl1[lll]["internet_type"]);
                                                        info = info + "<br/>* internet_type : " + internet_type;
                                                    }
                                                    if (HotelBedsResponse.Tables[MetaName].Columns.Contains("work_area") == true)
                                                    {
                                                        string work_area = "";
                                                        work_area = Convert.ToString(drTabl1[lll]["work_area"]);
                                                        info = info + "<br/>* work_area : " + work_area;
                                                    }
                                                    if (HotelBedsResponse.Tables[MetaName].Columns.Contains("age_start") == true)
                                                    {
                                                        string agestart = "";
                                                        agestart = Convert.ToString(drTabl1[lll]["age_start"]);
                                                        info = info + "<br/>* age_start : " + agestart;
                                                    }
                                                    if (HotelBedsResponse.Tables[MetaName].Columns.Contains("deposit_type") == true)
                                                    {
                                                        string deposit_type = "";
                                                        deposit_type = Convert.ToString(drTabl1[lll]["deposit_type"]);
                                                        info = info + "<br/>* deposit_type : " + deposit_type;
                                                    }
                                                    if (HotelBedsResponse.Tables[MetaName].Columns.Contains("age_end") == true)
                                                    {
                                                        string ageend = "";
                                                        ageend = Convert.ToString(drTabl1[lll]["age_end"]);
                                                        info = info + "<br/>* age_end : " + ageend;
                                                    }
                                                    if (HotelBedsResponse.Tables[MetaName].Columns.Contains("inclusion") == true)
                                                    {
                                                        string inclusion = "";
                                                        inclusion = Convert.ToString(drTabl1[lll]["inclusion"]);
                                                        info = info + "<br/>* inclusion : " + inclusion;
                                                    }
                                                    if (HotelBedsResponse.Tables[MetaName].Columns.Contains("meal_type") == true)
                                                    {
                                                        string meal_type = "";
                                                        meal_type = Convert.ToString(drTabl1[lll]["meal_type"]);
                                                        info = info + "<br/>* meal_type : " + meal_type;
                                                    }
                                                    if (HotelBedsResponse.Tables[MetaName].Columns.Contains("extra_bed") == true)
                                                    {
                                                        string extra_bed = "";
                                                        extra_bed = Convert.ToString(drTabl1[lll]["extra_bed"]);
                                                        info = info + "<br/>* extra_bed : " + extra_bed;
                                                    }
                                                    if (HotelBedsResponse.Tables[MetaName].Columns.Contains("price_unit") == true)
                                                    {
                                                        string metapriceperunit = "";
                                                        metapriceperunit = Convert.ToString(drTabl1[lll]["price_unit"]);
                                                        info = info + "<br/>* price_unit:  " + metapriceperunit;
                                                    }
                                                    if (HotelBedsResponse.Tables[MetaName].Columns.Contains("payment_type") == true)
                                                    {
                                                        string payment_type = "";
                                                        payment_type = Convert.ToString(drTabl1[lll]["payment_type"]);
                                                        info = info + "<br/>* payment_type:  " + payment_type;
                                                    }

                                                    if (HotelBedsResponse.Tables[MetaName].Columns.Contains("pricing_method") == true)
                                                    {
                                                        string pricing_method = "";
                                                        pricing_method = Convert.ToString(drTabl1[lll]["pricing_method"]);
                                                        info = info + "<br/>* pricing_method:  " + pricing_method;
                                                    }

                                                    if (HotelBedsResponse.Tables[MetaName].Columns.Contains("price") == true)
                                                    {
                                                        Double MetaPrice = 0.00;
                                                        MetaPrice = Convert.ToDouble(drTabl1[lll]["price"]);
                                                        info = info + "<br/>* price: " + MetaPrice + "(Customer needs to Pay at Property when Check-In) <br/> ";
                                                    }
                                                    if (HotelBedsResponse.Tables[MetaName].Columns.Contains("currency") == true)
                                                    {
                                                        string metacurrency = "";
                                                        metacurrency = Convert.ToString(drTabl1[lll]["currency"]);
                                                        info = info + " " + metacurrency;
                                                    }
                                                    Essential = Essential + " " + info;
                                                    //System.IO.File.WriteAllText(@"D:\ftp\Blueglobz\ess.txt", Convert.ToString(Essential));
                                                }

                                            }
                                        }
                                    }
                                }

                            }



                        }


                    }
                    
                }
                EssentialInfo = Essential + TaxEssential + Convert.ToString(info1);

            }
            catch (Exception ex)
            {
                await _exception.ErrorNew(ex, _LogAPI.Recheck,SearchKey);
                return "";
            }
            return EssentialInfo;
        }

        public async Task<RecheckETG> RateHawk_EssentialInfoNew(HotelSearchParameter SearchPara, string Supplier, string xmlhotelcode, string UserName, string PassWord, string Currency, string UserID, string RoomTypeId, string RatehawkURL, string SearchKey)
        {
            RecheckETG recheckETG = new RecheckETG();
            string EssentialInfo = "";
            try
            {
                string reqId = Convert.ToString(DateTime.Now.Ticks);
                string error = "";
                string[] spitid = RoomTypeId.Split(new String[] { "$$$" }, StringSplitOptions.None);
                string Nationality = spitid[4].ToString();
                string roomTimeMatchHas = spitid[5];
                string key = Convert.ToBase64String(Encoding.ASCII.GetBytes(UserName + ":" + PassWord));
                //string strHost = WebConfigurationManager.AppSettings["RatehawkURL"] + "/search/hp/";
                //string strHost = WebConfigurationManager.AppSettings["RatehawkURL"] + "/search/serp/hotels/";
                //string strHost = RatehawkURL + ETGInfo.search_serp_hotels;
                string strHost = RatehawkURL + _supplierConfig.ETG.search_serp_hotels;
                //string strHost = WebConfigurationManager.AppSettings["RatehawkURL"] + "/search/serp/region/";
                string UniqueNo = "";
                bool IsRooom = false;
                int IsHit = 0;
                string Request = "";// GetETG_Multiple_RoomOnly(SearchPara, Nationality, UserName, PassWord, UserID, xmlhotelcode, IsHit, Convert.ToString(SearchPara.CityID), Currency, IsRooom);
                string DataaResponse = "";
                ResponseDto responseDto = await GetEtgHotelDataResponse(RatehawkURL + "/hotel/info/?data={%22id%22:%22" + xmlhotelcode + "%22,%22language%22:%22en%22}", key);
                string DataResponse = responseDto.Response;
                error = responseDto.Error;


                string Essential = "";
                string info1 = "";

                //ExManager.Exception.LogException("GetEtgHotelDataResponse" + DataResponse);
                string TaxEssential = "";
                _ = _commonMethod.XMLRequestResponseTrack(RatehawkURL, DataResponse, Supplier, "Proper", reqId, SearchPara.SearchKey);
                if (DataResponse != string.Empty)
                {
                    //ExManager.Exception.LogException("GetEtgHotelDataResponse 123" + DataResponse);

                    DataSet EtgResponse1 = new DataSet();
                    XmlDocument doc1 = Newtonsoft.Json.JsonConvert.DeserializeXmlNode("{\"Row\":" + DataResponse + "}", "root");
                    String Data2 = doc1.InnerXml.Substring(doc1.InnerXml.IndexOf("<metapolicy_struct"), doc1.InnerXml.IndexOf("</metapolicy_struct>") - doc1.InnerXml.IndexOf("<metapolicy_struct")) + "</metapolicy_struct>";
                    Data2 = "<Root>" + Data2 + "</Root>";
                    XmlReader xmlReader1 = new XmlNodeReader(doc1);
                    EtgResponse1.ReadXml(xmlReader1);

                    DataSet HotelBedsResponse = new DataSet();
                    byte[] buffer = Encoding.UTF8.GetBytes(Data2);
                    using (MemoryStream stream = new MemoryStream(buffer))
                    {
                        XmlReader reader = XmlReader.Create(stream);
                        HotelBedsResponse.ReadXml(reader);
                    }
                    {
                        if (EtgResponse1.Tables.Contains("policy_struct"))
                        {
                            for (int x = 0; x < EtgResponse1.Tables["policy_struct"].Rows.Count; x++)
                            {
                                string Title = ""; string TitleId = "";
                                if (EtgResponse1.Tables["policy_struct"].Columns.Contains("title"))
                                {
                                    Title = Convert.ToString(EtgResponse1.Tables["policy_struct"].Rows[x]["title"]);
                                }

                                if (EtgResponse1.Tables["policy_struct"].Columns.Contains("policy_struct_Id"))
                                {
                                    TitleId = Convert.ToString(EtgResponse1.Tables["policy_struct"].Rows[x]["policy_struct_Id"]);
                                    if (EtgResponse1.Tables.Contains("paragraphs"))
                                    {
                                        DataRow[] drparagraphs = EtgResponse1.Tables["paragraphs"].Select("policy_struct_Id='" + TitleId + "'");
                                        if (drparagraphs.Length > 0)
                                        {
                                            if (x == 0)
                                            {
                                                Essential = "<b>IMPORTANT INFORMATION FOR CUSTOMER</b>";
                                            }
                                            Essential = Essential + "<br><br><b>" + Title + "</b><br><br>";
                                            for (int y = 0; y < drparagraphs.Length; y++)
                                            {
                                                string Info = Convert.ToString(drparagraphs[y]["paragraphs_Text"]);
                                                if (Info.Length < 5)
                                                    continue;
                                                if (Info != string.Empty)
                                                {
                                                    Essential = Essential + "* " + Info + "<br>";
                                                }

                                            }
                                        }
                                    }
                                }
                                else if (EtgResponse1.Tables["policy_struct"].Columns.Contains("paragraphs"))
                                {

                                    if (x == 0)
                                    {
                                        Essential = "<b>IMPORTANT INFORMATION FOR CUSTOMER</b>";
                                    }
                                    Essential = Essential + "<br><br><b>" + Title + "</b><br><br>";
                                    if (EtgResponse1.Tables["policy_struct"].Columns.Contains("paragraphs"))
                                    {
                                        string Info = Convert.ToString(EtgResponse1.Tables["policy_struct"].Rows[x]["paragraphs"]);

                                        if (Info != string.Empty)
                                        {
                                            Essential = Essential + "* " + Info + "<br>";
                                        }
                                    }
                                }

                                //Essential = Essential + "<br><br>";
                            }
                        }
                    }
                    if (HotelBedsResponse.Tables.Count > 0)
                    {
                        //Essential = "";
                        string metapolicyID = "";
                        if (HotelBedsResponse.Tables[0].Columns.Contains("metapolicy_struct_Id") == true)
                        {
                            for (int mpii = 0; mpii < HotelBedsResponse.Tables[0].Rows.Count; mpii++)
                            {
                                metapolicyID = Convert.ToString(HotelBedsResponse.Tables[0].Rows[mpii]["metapolicy_struct_Id"]);
                                if (metapolicyID != "")
                                {
                                    for (int mp = 1; mp < HotelBedsResponse.Tables.Count; mp++)
                                    {

                                        if (HotelBedsResponse.Tables[mp].Columns.Contains("metapolicy_struct_Id"))
                                        {
                                            DataRow[] drTabl1 = HotelBedsResponse.Tables[mp].Select("metapolicy_struct_Id='" + metapolicyID + "'");
                                            if (drTabl1.Length > 0)
                                            {
                                                string MetaName = "";// string metacurrency = ""; double MetaPrice = 0.00; string metafeetype = ""; string metapriceperunit = ""; string agestart = "";
                                                MetaName = Convert.ToString(HotelBedsResponse.Tables[mp].TableName);

                                                for (int lll = 0; lll < drTabl1.Length; lll++)
                                                {
                                                    string info = "";
                                                    info = "<br/><B>" + "Metapolicy" + "</B>";
                                                    info = info + "<br/>**" + MetaName;
                                                    if (HotelBedsResponse.Tables[MetaName].Columns.Contains("fee_type") == true)
                                                    {
                                                        string metafeetype = "";
                                                        metafeetype = Convert.ToString(drTabl1[lll]["fee_type"]);
                                                        info = info + "<br/>* fee_type : " + metafeetype;
                                                    }
                                                    if (HotelBedsResponse.Tables[MetaName].Columns.Contains("pets_type") == true)
                                                    {
                                                        string pets_type = "";
                                                        pets_type = Convert.ToString(drTabl1[lll]["pets_type"]);
                                                        info = info + "<br/>* pets_type : " + pets_type;
                                                    }
                                                    if (HotelBedsResponse.Tables[MetaName].Columns.Contains("visa_support") == true)
                                                    {
                                                        string visa_support = "";
                                                        visa_support = Convert.ToString(drTabl1[lll]["visa_support"]);
                                                        info = info + "<br/>* pets_type : " + visa_support;
                                                    }
                                                    if (HotelBedsResponse.Tables[MetaName].Columns.Contains("availability") == true)
                                                    {
                                                        string availability = "";
                                                        availability = Convert.ToString(drTabl1[lll]["availability"]);
                                                        info = info + "<br/>* availability : " + availability;
                                                    }
                                                    if (HotelBedsResponse.Tables[MetaName].Columns.Contains("day_period") == true)
                                                    {
                                                        string day_period = "";
                                                        day_period = Convert.ToString(drTabl1[lll]["day_period"]);
                                                        info = info + "<br/>* day_period : " + day_period;
                                                    }
                                                    if (HotelBedsResponse.Tables[MetaName].Columns.Contains("time") == true)
                                                    {
                                                        string time = "";
                                                        time = Convert.ToString(drTabl1[lll]["time"]);
                                                        info = info + "<br/>* time : " + time;
                                                    }
                                                    if (HotelBedsResponse.Tables[MetaName].Columns.Contains("amount") == true)
                                                    {
                                                        string amount = "";
                                                        amount = Convert.ToString(drTabl1[lll]["amount"]);
                                                        info = info + "<br/>* amount : " + amount;
                                                    }
                                                    if (HotelBedsResponse.Tables[MetaName].Columns.Contains("internet_type") == true)
                                                    {
                                                        string internet_type = "";
                                                        internet_type = Convert.ToString(drTabl1[lll]["internet_type"]);
                                                        info = info + "<br/>* internet_type : " + internet_type;
                                                    }
                                                    if (HotelBedsResponse.Tables[MetaName].Columns.Contains("work_area") == true)
                                                    {
                                                        string work_area = "";
                                                        work_area = Convert.ToString(drTabl1[lll]["work_area"]);
                                                        info = info + "<br/>* work_area : " + work_area;
                                                    }
                                                    if (HotelBedsResponse.Tables[MetaName].Columns.Contains("age_start") == true)
                                                    {
                                                        string agestart = "";
                                                        agestart = Convert.ToString(drTabl1[lll]["age_start"]);
                                                        info = info + "<br/>* age_start : " + agestart;
                                                    }
                                                    if (HotelBedsResponse.Tables[MetaName].Columns.Contains("deposit_type") == true)
                                                    {
                                                        string deposit_type = "";
                                                        deposit_type = Convert.ToString(drTabl1[lll]["deposit_type"]);
                                                        info = info + "<br/>* deposit_type : " + deposit_type;
                                                    }
                                                    if (HotelBedsResponse.Tables[MetaName].Columns.Contains("age_end") == true)
                                                    {
                                                        string ageend = "";
                                                        ageend = Convert.ToString(drTabl1[lll]["age_end"]);
                                                        info = info + "<br/>* age_end : " + ageend;
                                                    }
                                                    if (HotelBedsResponse.Tables[MetaName].Columns.Contains("inclusion") == true)
                                                    {
                                                        string inclusion = "";
                                                        inclusion = Convert.ToString(drTabl1[lll]["inclusion"]);
                                                        info = info + "<br/>* inclusion : " + inclusion;
                                                    }
                                                    if (HotelBedsResponse.Tables[MetaName].Columns.Contains("meal_type") == true)
                                                    {
                                                        string meal_type = "";
                                                        meal_type = Convert.ToString(drTabl1[lll]["meal_type"]);
                                                        info = info + "<br/>* meal_type : " + meal_type;
                                                    }
                                                    if (HotelBedsResponse.Tables[MetaName].Columns.Contains("extra_bed") == true)
                                                    {
                                                        string extra_bed = "";
                                                        extra_bed = Convert.ToString(drTabl1[lll]["extra_bed"]);
                                                        info = info + "<br/>* extra_bed : " + extra_bed;
                                                    }
                                                    if (HotelBedsResponse.Tables[MetaName].Columns.Contains("price_unit") == true)
                                                    {
                                                        string metapriceperunit = "";
                                                        metapriceperunit = Convert.ToString(drTabl1[lll]["price_unit"]);
                                                        info = info + "<br/>* price_unit:  " + metapriceperunit;
                                                    }
                                                    if (HotelBedsResponse.Tables[MetaName].Columns.Contains("payment_type") == true)
                                                    {
                                                        string payment_type = "";
                                                        payment_type = Convert.ToString(drTabl1[lll]["payment_type"]);
                                                        info = info + "<br/>* payment_type:  " + payment_type;
                                                    }

                                                    if (HotelBedsResponse.Tables[MetaName].Columns.Contains("pricing_method") == true)
                                                    {
                                                        string pricing_method = "";
                                                        pricing_method = Convert.ToString(drTabl1[lll]["pricing_method"]);
                                                        info = info + "<br/>* pricing_method:  " + pricing_method;
                                                    }

                                                    if (HotelBedsResponse.Tables[MetaName].Columns.Contains("price") == true)
                                                    {
                                                        Double MetaPrice = 0.00;
                                                        MetaPrice = Convert.ToDouble(drTabl1[lll]["price"]);
                                                        info = info + "<br/>* price: " + MetaPrice + "(Customer needs to Pay at Property when Check-In) <br/> ";
                                                    }
                                                    if (HotelBedsResponse.Tables[MetaName].Columns.Contains("currency") == true)
                                                    {
                                                        string metacurrency = "";
                                                        metacurrency = Convert.ToString(drTabl1[lll]["currency"]);
                                                        info = info + " " + metacurrency;
                                                    }
                                                    Essential = Essential + " " + info;
                                                    //System.IO.File.WriteAllText(@"D:\ftp\Blueglobz\ess.txt", Convert.ToString(Essential));
                                                }

                                            }
                                        }
                                    }
                                }

                            }



                        }


                    }

                }
                EssentialInfo = Essential + TaxEssential + Convert.ToString(info1);
                recheckETG.EsentialInfo = EssentialInfo;

                #region For BookHasH

                string RoomRequest = ETGHotelRequest(SearchPara, Nationality, xmlhotelcode, "", Currency);
                string ETGURL = RatehawkURL + _supplierConfig.ETG.search_serp_hp;
                string Error = "";
                string RoomResponseData = ResponseETGSupplier(ref Error, RoomRequest, ETGURL, UserName, PassWord, "ratehawk");


                if (RoomResponseData == "")
                {
                    recheckETG.Book_Hash = "";
                    return recheckETG;
                }


                DataSet ratehawkResponse = null;
                string ConvertXml = "";
                try
                {
                    //ConvertXml = Newtonsoft.Json.JsonConvert.DeserializeXmlNode(objResponse.Response).InnerXml;
                    ConvertXml = Newtonsoft.Json.JsonConvert.DeserializeXmlNode(RoomResponseData, "root").InnerXml;
                    ratehawkResponse = _commonMethod.GetDataSetFromXMLString(ConvertXml);
                }
                catch (Exception ex)
                {
                    _ = _exception.ErrorBooking(ex, "");
                }

                _commonMethod.ThirdPartyGetPolicyHistory(Convert.ToString(""), Convert.ToString(xmlhotelcode), Convert.ToString(RoomRequest), Convert.ToString(RoomResponseData + Error), "Room Request Response before Policy", Supplier, "Room Response", SearchKey, "allproject");

                string book_hash = "";
                string roomName = "";
                string mealName = "";

                double SubAgentMarkup = 0;
                int IsParentId = 1;


                for (int i = 0; i < ratehawkResponse.Tables["rates"].Rows.Count; i++)
                {
                    string match_has = Convert.ToString(ratehawkResponse.Tables["rates"].Rows[i]["match_hash"]);
                    if (match_has != "")
                    {
                        if (roomTimeMatchHas == match_has)
                        {
                            book_hash = Convert.ToString(ratehawkResponse.Tables["rates"].Rows[i]["book_hash"]);

                            recheckETG.Book_Hash = book_hash;
                        }
                    }
                }

                #endregion

            }
            catch (Exception ex)
            {
                await _exception.ErrorNew(ex, _LogAPI.Recheck, SearchKey);
                return recheckETG;
            }
            return recheckETG;
        }

        public async Task<ResponseDto> GetEtgHotelDataResponse(string strHost, string Key)
        {
            ResponseDto responseDto = new ResponseDto();
            System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
            timer.Start();
            string responsedata = "";
            string Error = "";

            try
            {

                var wc = new WebClient();

                string reqUrl = strHost;
                HttpWebRequest Request = (HttpWebRequest)WebRequest.Create(reqUrl);
                Request.Timeout = 40000;
                Request.ContentType = "application/json; charset=utf-8";
                Request.Headers.Add("Authorization", "Basic " + Key);
                Request.Headers.Add("Cookie", "uid=TfTb5l9y0pXCLwUEFjIzAg==; __cfduid=dc92f2e7430bb431322c0d874a0d15e1d1603953156");
                Request.Headers.Add("Accept-Encoding", "gzip, deflate");
                HttpWebResponse response = (HttpWebResponse)Request.GetResponse();
                Stream streamResponse = response.GetResponseStream();
                if (response.ContentEncoding.ToLower().Contains("gzip"))
                    streamResponse = new System.IO.Compression.GZipStream(streamResponse, System.IO.Compression.CompressionMode.Decompress);
                else if (response.ContentEncoding.ToLower().Contains("deflate"))
                    streamResponse = new System.IO.Compression.DeflateStream(streamResponse, System.IO.Compression.CompressionMode.Decompress);
                else
                    streamResponse = response.GetResponseStream();
                StreamReader streamReader = new StreamReader(streamResponse);
                responsedata = streamReader.ReadToEnd();
            }
            catch (Exception ex)
            {
                _ = _exception.Error(ex);
                Error = ex.Message.ToString();
            }
            responseDto.Response = responsedata;
            responseDto.Error = Error;

            return responseDto;
        }

        public async Task<PriceCheckCMDto> Get_Ostrovok_Prebook(DataSet dsCartDetails, DataRow[] OstrovokDetails, double FinalPrice, double CompanyBuyingPrice, TokenData TokenData, string Searchkey, int AgentId, int i = 0, string Supplier = "")
        {
            PriceCheckCMDto priceCheckCMDto = new PriceCheckCMDto() ;
            string responsetype = "Success";
            string time = "";
            string Error = "";
            string SupplierName = Supplier;
            string PrebookRQ = "-";
            string PrebookResponseData = "-";
            string XmlId = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["XMLHotelId"]);
            string GiataId = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["GiataId"]);
            string ServiceId = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["ServiceId"]);
            try
            {
                DataTable dtROE = TokenData.dtROE;
                int rates_id = 0;
                string book_hash = "";
                double sellingAmount = 0;
                bool price_changed = false;
                string UserName = Convert.ToString(OstrovokDetails[0]["UserName"]);
                string Password = Convert.ToString(OstrovokDetails[0]["Password"]);
                string UserCartDetailId = Convert.ToString(dsCartDetails.Tables[1].DefaultView[0]["UserCartDetailId"]);
                string Key = Convert.ToBase64String(Encoding.ASCII.GetBytes(UserName + ":" + Password));
                //string PrebookURL = Convert.ToString(OstrovokDetails[0]["WebSiteURL"]) + "/serp/prebook";
                string PrebookURL = Convert.ToString(OstrovokDetails[0]["WebSiteURL"]) + _supplierConfig.ETG.Prebook;
                int UserCartId = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[0]["UserCartID"]);
                string RoomTypeId = Convert.ToString(dsCartDetails.Tables[1].DefaultView[i]["RoomTypeID"]); // new added
                string free_cancellation_before = "";
                //string ServerIp = GetUserPlatform();
                //string ServerIp = GetUserPlatform();
                //ServerIp = ServerIp.Split(new String[] { "$$" }, StringSplitOptions.None)[1];

                #region Data
                //SearchParameter objSearPara = (SearchParameter)HttpContext.Current.Session["SearchPara"];
                double CurrencyFactor = 1;
                DataSet dsTemp = new DataSet();
                #region New BookHash               
                string[] array = Convert.ToString(dsCartDetails.Tables[1].DefaultView[0]["RoomTypeId"]).Split(new String[] { "$$$" }, StringSplitOptions.None);
                //string Currency = array[3];
                string Etg_UniqueId = array[6];

                #endregion
                PrebookRQ = Convert.ToString(OstrovokPrBookRequest(Etg_UniqueId));
                _commonMethod.ThirdPartyBookingHistory(Convert.ToString(UserCartDetailId), "", PrebookRQ, "-", "Before Hit Prebook Request", SupplierName, "Response Pending", "-", "Withinearth");
                #endregion
                DataRow[] policies = null;
                if (PrebookRQ != "")
                {
                    #region Price Check RS

                    //PrebookResponseData = GetOstrovokRoomResponse(PrebookRQ, PrebookURL, Key, ref Error, UserCartDetailId);
                    PrebookResponseData = await GetRatehawkRoomResponse(PrebookRQ, PrebookURL, Key, UserCartDetailId,60000);

                    _commonMethod.ThirdPartyBookingHistory(Convert.ToString(UserCartDetailId), UserCartDetailId, PrebookRQ, PrebookResponseData + Error, "After Hit Prebook Response", SupplierName, "Prebook Response Confirm", "-", "Withinearth");


                    if (PrebookResponseData == string.Empty || PrebookResponseData == "" || PrebookResponseData == null)
                    {
                        //SuppReason = "NRS";// No Response from supplier side
                        responsetype = "GeneralError";
                        string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", TokenData.AgentId, time, responsetype, PrebookRQ, PrebookResponseData, Error);

                        priceCheckCMDto.Flag = false;
                        return priceCheckCMDto;
                    }
                    DataSet EtgResponse = new DataSet();
                    XmlDocument doc = Newtonsoft.Json.JsonConvert.DeserializeXmlNode("{\"Row\":" + PrebookResponseData + "}", "root");
                    XmlReader xmlReader = new XmlNodeReader(doc);
                    EtgResponse.ReadXml(xmlReader);
                    #endregion

                    #region Price Check Logic
                    if (PrebookResponseData != null && EtgResponse.Tables["Row"].Rows[0]["status"].ToString() == "ok")
                    {
                        if (EtgResponse.Tables.Contains("changes"))
                        {
                            if (EtgResponse.Tables["changes"].Columns.Contains("price_changed") == true)
                            {
                                price_changed = Convert.ToBoolean(EtgResponse.Tables["changes"].Rows[0]["price_changed"]);

                                if (price_changed == true)
                                {
                                    PrebookResponseData = "#Price Changed Tag Received from supplier =  " + price_changed + PrebookResponseData;
                                    //SuppReason = "PCS.";// Price change from supplier side
                                    responsetype = "GeneralError";
                                    string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", TokenData.AgentId, time, responsetype, PrebookRQ, PrebookResponseData, Error);


                                    priceCheckCMDto.Flag = true;
                                    return priceCheckCMDto;
                                }
                            }
                        }
                        int hotels_id = Convert.ToInt32(EtgResponse.Tables["hotels"].Rows[0]["hotels_Id"]);
                        if (EtgResponse.Tables.Contains("rates"))
                        {
                            DataRow[] drRooms = EtgResponse.Tables["rates"].Select("hotels_Id='" + hotels_id + "'");
                            book_hash = Convert.ToString(drRooms[0]["book_hash"]);
                            rates_id = Convert.ToInt32(drRooms[0]["rates_Id"]);
                            DataRow[] payment_option = EtgResponse.Tables["payment_options"].Select("rates_Id='" + rates_id + "'");
                            int payment_optionId = Convert.ToInt32(payment_option[0]["payment_options_Id"]);

                            DataRow[] drpayment_type = EtgResponse.Tables["payment_types"].Select("payment_options_Id='" + payment_optionId + "'");
                            double Amount = Convert.ToDouble(drpayment_type[0]["amount"]);
                            string Currency = Convert.ToString(drpayment_type[0]["currency_code"]);
                            int payment_typeId = Convert.ToInt32(drpayment_type[0]["payment_types_Id"]);
                            DataRow[] cancellation_penalty = EtgResponse.Tables["cancellation_penalties"].Select("payment_types_Id='" + payment_typeId + "'");
                            int cancellation_penaltyId = Convert.ToInt32(cancellation_penalty[0]["cancellation_penalties_Id"]);
                            free_cancellation_before = Convert.ToString(cancellation_penalty[0]["free_cancellation_before"]);
                            policies = EtgResponse.Tables["policies"].Select("cancellation_penalties_Id='" + payment_optionId + "'");

                            if (Currency.ToLower() != TokenData.SiteCurrency.ToLower())
                            {
                                CurrencyFactor = _commonMethod.GetROEByCurrency(Currency, TokenData.dtROE);
                            }
                            sellingAmount = Amount;
                            FinalPrice = (sellingAmount) * CurrencyFactor;

                        }
                        
                        bool IsSuccess = await _commonMethod.GetHasKeyUpdateStaus(book_hash, UserCartId);
                        

                        
                        FinalPrice = Convert.ToDouble(Math.Round(FinalPrice, 2));
                        priceCheckCMDto.ItemPrice = FinalPrice;

                        if (Convert.ToDouble(Math.Round(FinalPrice.TruncateDecimalPlaces(2))) > Math.Round(CompanyBuyingPrice.TruncateDecimalPlaces(2)))
                        {
                            PrebookResponseData = "#Price Changed " + "#Main Price = " + CompanyBuyingPrice + " #New Price = " + FinalPrice + " #Response= " + PrebookResponseData;
                            responsetype = "GeneralError";
                            string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", TokenData.AgentId, time, responsetype, PrebookRQ, PrebookResponseData, Error);
                            //SuppReason = "PCS";// Price change from supplier side

                            priceCheckCMDto.Flag = true;
                            return priceCheckCMDto;
                        }
                        #region Policy Check
                        //DateTime CancellationDeadLine = Convert.ToDateTime("2000/01/01");
                        //if (dtPolicy.Rows.Count > 0)
                        //{
                        //    //DataRow[] dsHotelData = dtPolicy.Select("RoomTypeID='" + RoomTypeId + "'");

                        //    if (dsHotelData.Count() >= 1)
                        //    {
                        //        if (dsHotelData.Count() > 1)
                        //        {
                        //            CancellationDeadLine = Convert.ToDateTime(dsHotelData[1]["FromDate"]).AddDays(-1);
                        //        }

                        //        else
                        //        {
                        //            CancellationDeadLine = Convert.ToDateTime(dsHotelData[0]["FromDate"]);
                        //        }

                        //    }
                        //}
                        //if (CancellationDeadLine != Convert.ToDateTime("2000/01/01"))
                        {
                            //DateTime DeadlineDate = DateTime.Now;
                            string purchaseToken = Convert.ToString(dsCartDetails.Tables[1].DefaultView[0]["purchaseToken"]);

                            if (purchaseToken.ToLower() != "nonref")
                            {
                                string[] formats = {
            "yyyy-MM-ddTHH:mm:ss",  // e.g. 3/12/2026 4:00:00 PM
            "M/d/yyyy h:mm:ss tt",// e.g. 03/12/2026 04:00:00 PM
            "dd/MM/yyyy HH:mm:ss",  // e.g. 12/03/2026 16:00:00
            "dd/MM/yyyy",           // e.g. 12/03/2026
            "M/d/yyyy",              // e.g. 3/12/2026
            "yyyy/MM/dd"              // e.g. 3/12/2026
        };

                                //DateTime free_cancellation_beforeroom = DateTime.ParseExact(purchaseToken, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                                //DateTime free_cancellation_beforeroom = DateTime.ParseExact(purchaseToken, "M/d/yyyy h:mm:ss tt", CultureInfo.InvariantCulture);
                                DateTime free_cancellation_beforeroom = DateTime.ParseExact(purchaseToken, formats, CultureInfo.InvariantCulture);
                                //DateTime free_cancellation_beforeroom = DateTime.ParseExact(purchaseToken, "yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);

                                //CancellationDeadLine = CancellationDeadLine.AddDays(-1).Date;
                                // string policiedate = "";
                                //string curDate1 = DateTime.Now.Date.ToString("dd/MM/yyyy");
                                //string currentdate = (Convert.ToString(curDate1)).Substring(3, 2) + "/" + (Convert.ToString(curDate1)).Substring(0, 2) + "/" + (Convert.ToString(curDate1)).Substring(6, 4);
                                DateTime CancelDate = DateTime.Now.Date;// DateTime.ParseExact(currentdate, "MM/dd/yyyy", null);
                                if (free_cancellation_before != "")
                                {
                                    DateTime dateString11 = DateTime.Parse(free_cancellation_before, CultureInfo.InvariantCulture).Date;
                                    //string formattedDate = dateString11.ToString("dd/MM/yyyy");
                                    string formattedDate = dateString11.ToString("yyyy/MM/dd");

                                    //CancelDate = DateTime.ParseExact(formattedDate, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                                    CancelDate = DateTime.ParseExact(formattedDate, "yyyy/MM/dd", CultureInfo.InvariantCulture);

                                }
                                else
                                {
                                    CancelDate = DateTime.Now.Date;
                                }


                                //for (int bbb = 0; bbb < policies.Length; bbb++)
                                //{
                                //    string startAt = Convert.ToString(policies[bbb]["start_at"]);
                                //    if (startAt != "")
                                //    {
                                //        string PolicieDate = Convert.ToString(policies[bbb]["start_at"]);
                                //        policiedate = (Convert.ToString(PolicieDate)).Substring(8, 2) + "/" + (Convert.ToString(PolicieDate)).Substring(5, 2) + "/" + (Convert.ToString(PolicieDate)).Substring(0, 4);
                                //        CancelDate = DateTime.ParseExact(policiedate, "dd/MM/yyyy", null);
                                //    }
                                //}
                                //string cancelDateStr = CancelDate.ToString("yyyy-MM-dd");
                                //DeadlineDate = DateTime.ParseExact(cancelDateStr, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                                // DeadlineDate = DeadlineDate.AddDays(CutOffDays).Date;
                                //if (CancellationDeadLine.Date > DeadlineDate.Date)
                                if (free_cancellation_beforeroom.Date != CancelDate.Date)
                                {
                                    FinalPrice = 0;
                                    _commonMethod.ThirdPartyBookingHistory(Convert.ToString(UserCartDetailId), Convert.ToString(UserCartDetailId), PrebookRQ, "-", "(After Price Check BookingTime) Cancellation Policy Change", SupplierName, "Request Confirm", " - ", "Withinearth");
                                    PrebookResponseData = "#Policy Changed " + "#Main Policy = " + free_cancellation_beforeroom + " #New Policy = " + CancelDate + " #Response= " + PrebookResponseData;
                                    responsetype = "GeneralError";
                                    string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, PrebookRQ, PrebookResponseData, Error);

                                    priceCheckCMDto.Flag = true;
                                    priceCheckCMDto.ItemPrice = FinalPrice;
                                    return priceCheckCMDto;

                                }

                                //}
                            }
                        }
                        #endregion
                    }
                    else
                    {
                        //SuppReason = "PFS";// Prebook fail from supplier side
                        responsetype = "GeneralError";
                        string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", TokenData.AgentId, time, responsetype, PrebookRQ, PrebookResponseData, Error);
                        priceCheckCMDto.Flag = false;
                        return priceCheckCMDto;

                    }
                    #endregion
                }
                else
                {
                    responsetype = "GeneralError";
                    string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", TokenData.AgentId, time, responsetype, PrebookRQ, PrebookResponseData, Error);
                    priceCheckCMDto.Flag = false;
                    return priceCheckCMDto;
                }
                #region RPT Success
                try
                {
                    responsetype = "Success";
                    string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", TokenData.AgentId, time, responsetype, PrebookRQ, PrebookResponseData, Error);
                }
                catch (Exception e)
                {
                    _ = _exception.Error(e, Searchkey);
                }
                #endregion
            }
            catch (Exception e)
            {
                responsetype = "GeneralError";
                string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", TokenData.AgentId, time, responsetype, PrebookRQ, e.Message.ToString() + "#" + PrebookResponseData, Error);
                _ = _exception.Error(e);
                priceCheckCMDto.Flag = true;
                priceCheckCMDto.ItemPrice = 0;
                return priceCheckCMDto;
            }
            priceCheckCMDto.Flag = false;
            return priceCheckCMDto;
        }
        public async Task<PriceCheckCMDto> Get_Ostrovok_Prebook_new(Shopping_CartDataForHotelXML objdata, DataSet dsCartDetails, DataRow[] OstrovokDetails, double FinalPrice, double CompanyBuyingPrice, TokenData TokenData, string Searchkey, int AgentId, int i = 0, string Supplier = "")
        {
            PriceCheckCMDto priceCheckCMDto = new PriceCheckCMDto();
            string responsetype = "Success";
            string time = "";
            string Error = "";
            string SupplierName = Supplier;
            string PrebookRQ = "-";
            string PrebookResponseData = "-";
            string XmlId = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["XMLHotelId"]);
            string GiataId = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["GiataId"]);
            string ServiceId = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["ServiceId"]);
            try
            {
                DataTable dtROE = TokenData.dtROE;
                int rates_id = 0;
                string book_hash = "";
                double sellingAmount = 0;
                bool price_changed = false;
                string UserName = Convert.ToString(OstrovokDetails[0]["UserName"]);
                string Password = Convert.ToString(OstrovokDetails[0]["Password"]);
                string UserCartDetailId = Convert.ToString(dsCartDetails.Tables[1].DefaultView[0]["UserCartDetailId"]);
                string Key = Convert.ToBase64String(Encoding.ASCII.GetBytes(UserName + ":" + Password));
                //string PrebookURL = Convert.ToString(OstrovokDetails[0]["WebSiteURL"]) + "/serp/prebook";
                string PrebookURL = Convert.ToString(OstrovokDetails[0]["WebSiteURL"]) + _supplierConfig.ETG.Prebook;
                int UserCartId = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[0]["UserCartID"]);
                string RoomTypeId = Convert.ToString(dsCartDetails.Tables[1].DefaultView[i]["RoomTypeID"]); // new added
                string free_cancellation_before = "";
                //string ServerIp = GetUserPlatform();
                //string ServerIp = GetUserPlatform();
                //ServerIp = ServerIp.Split(new String[] { "$$" }, StringSplitOptions.None)[1];

                #region Data
                //SearchParameter objSearPara = (SearchParameter)HttpContext.Current.Session["SearchPara"];
                double CurrencyFactor = 1;
                DataSet dsTemp = new DataSet();
                //adde by trupti
               // #region New BookHas  
                //string[] BookingPolicy = Convert.ToString(dsCartDetails.Tables[1].DefaultView[0]["purchaseToken"]).Split(new String[] { "$$$" }, StringSplitOptions.None);
                //string booking_policy_id = BookingPolicy[0];
                //string Etg_UniqueId = booking_policy_id;

               // #endregion
                #region New BookHash               
                string[] array = Convert.ToString(RoomTypeId).Split(new String[] { "$$$" }, StringSplitOptions.None);
                //string Currency = array[3];
                string Etg_UniqueId = array[6];

                #endregion
                PrebookRQ = Convert.ToString(OstrovokPrBookRequest(Etg_UniqueId));
                _commonMethod.ThirdPartyBookingHistory(Convert.ToString(UserCartDetailId), "", PrebookRQ, "-", "Before Hit Prebook Request", SupplierName, "Response Pending", "-", "Withinearth",Searchkey);
                #endregion
                DataRow[] policies = null;
                if (PrebookRQ != "")
                {
                    #region Price Check RS

                    //PrebookResponseData = GetOstrovokRoomResponse(PrebookRQ, PrebookURL, Key, ref Error, UserCartDetailId);
                    PrebookResponseData = await GetRatehawkRoomResponse(PrebookRQ, PrebookURL, Key, UserCartDetailId,60000);

                    _commonMethod.ThirdPartyBookingHistory(Convert.ToString(UserCartDetailId), UserCartDetailId, PrebookRQ, PrebookResponseData + Error, "After Hit Prebook Response", SupplierName, "Prebook Response Confirm", "-", "Withinearth",Searchkey);


                    if (PrebookResponseData == string.Empty || PrebookResponseData == "" || PrebookResponseData == null)
                    {
                        //SuppReason = "NRS";// No Response from supplier side
                        responsetype = "GeneralError";
                        string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", TokenData.AgentId, time, responsetype, PrebookRQ, PrebookResponseData, Error);

                        priceCheckCMDto.Flag = false;
                        return priceCheckCMDto;
                    }
                    DataSet EtgResponse = new DataSet();
                    XmlDocument doc = Newtonsoft.Json.JsonConvert.DeserializeXmlNode("{\"Row\":" + PrebookResponseData + "}", "root");
                    XmlReader xmlReader = new XmlNodeReader(doc);
                    EtgResponse.ReadXml(xmlReader);
                    #endregion

                    #region Price Check Logic
                    if (PrebookResponseData != null && EtgResponse.Tables["Row"].Rows[0]["status"].ToString() == "ok")
                    {
                        if (EtgResponse.Tables.Contains("changes"))
                        {
                            if (EtgResponse.Tables["changes"].Columns.Contains("price_changed") == true)
                            {
                                price_changed = Convert.ToBoolean(EtgResponse.Tables["changes"].Rows[0]["price_changed"]);

                                if (price_changed == true)
                                {
                                    PrebookResponseData = "#Price Changed Tag Received from supplier =  " + price_changed + PrebookResponseData;
                                    //SuppReason = "PCS.";// Price change from supplier side
                                    responsetype = "GeneralError";
                                    string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", TokenData.AgentId, time, responsetype, PrebookRQ, PrebookResponseData, Error);


                                    priceCheckCMDto.Flag = true;
                                    return priceCheckCMDto;
                                }
                            }
                        }
                        int hotels_id = Convert.ToInt32(EtgResponse.Tables["hotels"].Rows[0]["hotels_Id"]);
                        if (EtgResponse.Tables.Contains("rates"))
                        {
                            DataRow[] drRooms = EtgResponse.Tables["rates"].Select("hotels_Id='" + hotels_id + "'");
                            book_hash = Convert.ToString(drRooms[0]["book_hash"]);
                            rates_id = Convert.ToInt32(drRooms[0]["rates_Id"]);
                            DataRow[] payment_option = EtgResponse.Tables["payment_options"].Select("rates_Id='" + rates_id + "'");
                            int payment_optionId = Convert.ToInt32(payment_option[0]["payment_options_Id"]);

                            DataRow[] drpayment_type = EtgResponse.Tables["payment_types"].Select("payment_options_Id='" + payment_optionId + "'");
                            double Amount = Convert.ToDouble(drpayment_type[0]["amount"]);
                            string Currency = Convert.ToString(drpayment_type[0]["currency_code"]);
                            int payment_typeId = Convert.ToInt32(drpayment_type[0]["payment_types_Id"]);
                            DataRow[] cancellation_penalty = EtgResponse.Tables["cancellation_penalties"].Select("payment_types_Id='" + payment_typeId + "'");
                            int cancellation_penaltyId = Convert.ToInt32(cancellation_penalty[0]["cancellation_penalties_Id"]);
                            free_cancellation_before = Convert.ToString(cancellation_penalty[0]["free_cancellation_before"]);
                            policies = EtgResponse.Tables["policies"].Select("cancellation_penalties_Id='" + payment_optionId + "'");

                            if (Currency.ToLower() != TokenData.SiteCurrency.ToLower())
                            {
                                CurrencyFactor = _commonMethod.GetROEByCurrency(Currency, TokenData.dtROE);
                            }
                            sellingAmount = Amount;
                            FinalPrice = (sellingAmount) * CurrencyFactor;

                        }

                        // bool IsSuccess = await _commonMethod.GetHasKeyUpdateStaus(book_hash, UserCartId);
                        objdata.ShoppingCartDetails[0].EachoToken = book_hash;

                        FinalPrice = Convert.ToDouble(Math.Round(FinalPrice, 2));
                        priceCheckCMDto.ItemPrice = FinalPrice;

                        if (Convert.ToDouble(Math.Round(FinalPrice.TruncateDecimalPlaces(2))) > Math.Round(CompanyBuyingPrice.TruncateDecimalPlaces(2)))
                        {
                            PrebookResponseData = "#Price Changed " + "#Main Price = " + CompanyBuyingPrice + " #New Price = " + FinalPrice + " #Response= " + PrebookResponseData;
                            responsetype = "GeneralError";
                            string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", TokenData.AgentId, time, responsetype, PrebookRQ, PrebookResponseData, Error);
                            //SuppReason = "PCS";// Price change from supplier side

                            priceCheckCMDto.Flag = true;
                            return priceCheckCMDto;
                        }
                        #region Policy Check

                        {
                            //DateTime DeadlineDate = DateTime.Now;
                            string purchaseToken = Convert.ToString(dsCartDetails.Tables[1].DefaultView[0]["purchaseToken"]);

                            if (purchaseToken.ToLower() != "nonref")
                            {
                                string[] formats = {
            "yyyy-MM-ddTHH:mm:ss",  // e.g. 3/12/2026 4:00:00 PM
            "M/d/yyyy h:mm:ss tt",// e.g. 03/12/2026 04:00:00 PM
            "MM/dd/yyyy h:mm:ss tt",
            "dd/MM/yyyy HH:mm:ss",  // e.g. 12/03/2026 16:00:00
            "dd/MM/yyyy",           // e.g. 12/03/2026
            "M/d/yyyy",              // e.g. 3/12/2026
            "yyyy/MM/dd" ,             // e.g. 3/12/2026
            "MM/dd/yyyy HH:mm:ss",
        };

                                //DateTime free_cancellation_beforeroom = DateTime.ParseExact(purchaseToken, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                                //DateTime free_cancellation_beforeroom = DateTime.ParseExact(purchaseToken, "M/d/yyyy h:mm:ss tt", CultureInfo.InvariantCulture);
                                DateTime free_cancellation_beforeroom = DateTime.ParseExact(purchaseToken, formats, CultureInfo.InvariantCulture);

                                DateTime CancelDate = DateTime.Now.Date;// DateTime.ParseExact(currentdate, "MM/dd/yyyy", null);
                                if (free_cancellation_before != "")
                                {
                                    DateTime dateString11 = DateTime.Parse(free_cancellation_before, CultureInfo.InvariantCulture).Date;
                                    //string formattedDate = dateString11.ToString("dd/MM/yyyy");
                                    string formattedDate = dateString11.ToString("yyyy/MM/dd");

                                    //CancelDate = DateTime.ParseExact(formattedDate, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                                    CancelDate = DateTime.ParseExact(formattedDate, "yyyy/MM/dd", CultureInfo.InvariantCulture);

                                }
                                else
                                {
                                    CancelDate = DateTime.Now.Date;
                                }

                                try
                                {
                                    //if (free_cancellation_beforeroom.Date != CancelDate.Date)
                                    if (free_cancellation_beforeroom.Date.ToString("yyyy-MM-dd")
    != CancelDate.ToString("yyyy-MM-dd"))
                                    {
                                        FinalPrice = 0;
                                        PrebookResponseData = "#Policy Changed " + "#Main Policy = " + free_cancellation_beforeroom + " #New Policy = " + CancelDate + " #Response= " + PrebookResponseData;
                                        responsetype = "GeneralError";
                                        _commonMethod.ThirdPartyBookingHistory(Convert.ToString(UserCartDetailId), Convert.ToString(UserCartDetailId), PrebookRQ, PrebookResponseData, "(After Price Check BookingTime) Cancellation Policy Change", SupplierName, "Request Confirm", " - ", "Withinearth");

                                        string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, PrebookRQ, PrebookResponseData, Error);

                                        priceCheckCMDto.Flag = true;
                                        priceCheckCMDto.ItemPrice = FinalPrice;
                                        return priceCheckCMDto;

                                    }

                                }
                                catch (Exception ex)
                                {
                                    
                                   _logger.LogError(ex, "Error policy date format change" + free_cancellation_beforeroom + CancelDate );

                                }



                                //}
                            }
                        }
                        #endregion
                    }
                    else
                    {
                        //SuppReason = "PFS";// Prebook fail from supplier side
                        responsetype = "GeneralError";
                        string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", TokenData.AgentId, time, responsetype, PrebookRQ, PrebookResponseData, Error);
                        priceCheckCMDto.Flag = false;
                        return priceCheckCMDto;

                    }
                    #endregion
                }
                else
                {
                    responsetype = "GeneralError";
                    string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", TokenData.AgentId, time, responsetype, PrebookRQ, PrebookResponseData, Error);
                    priceCheckCMDto.Flag = false;
                    return priceCheckCMDto;
                }
                #region RPT Success
                try
                {
                    responsetype = "Success";
                    string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", TokenData.AgentId, time, responsetype, PrebookRQ, PrebookResponseData, Error);
                }
                catch (Exception e)
                {
                    _ = _exception.Error(e, Searchkey);
                }
                #endregion
            }
            catch (Exception e)
            {
                responsetype = "GeneralError";
                string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", TokenData.AgentId, time, responsetype, PrebookRQ, e.Message.ToString() + "#" + PrebookResponseData, Error);
                _ = _exception.Error(e);
                priceCheckCMDto.Flag = true;
                priceCheckCMDto.ItemPrice = 0;
                return priceCheckCMDto;
            }
            priceCheckCMDto.Flag = false;
            return priceCheckCMDto;
        }
        private DateTime SafeParseDate(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return DateTime.Now;

            string[] formats = {
        "MM/dd/yyyy HH:mm:ss",
        "M/d/yyyy h:mm:ss tt",
        "yyyy-MM-ddTHH:mm:ss",
        "dd/MM/yyyy HH:mm:ss",
        "dd/MM/yyyy",
        "M/d/yyyy",
        "yyyy/MM/dd"
    };

            DateTime result;

            if (DateTime.TryParseExact(input, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out result))
                return result;

            if (DateTime.TryParse(input, out result))
                return result;

            return DateTime.Now; // fallback (no error)
        }
        public string OstrovokPrBookRequest(string Etg_UniqueId)
        {
            string FinalHashKey = "";
            try
            {
                StringBuilder SearchRQ = new StringBuilder();
                SearchRQ.Append("{");
                SearchRQ.Append("\"hash\": \"" + Etg_UniqueId + "\"");
                SearchRQ.Append("}");

                FinalHashKey = SearchRQ.ToString();

                return FinalHashKey;
            }
            catch (Exception ex)
            {
                _ = _exception.Error(ex);
                return "";
            }
        }

        public async Task<string> GetRatehawkRoomResponse(string strxmlRQRS, string strHost, string Key, string UniqueNo = "",int Timeout=60000)
        {

            System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
            timer.Start();
            string strOutput = "";
            try
            {
                HttpWebRequest Request;
                Request = (HttpWebRequest)HttpWebRequest.Create(strHost);
                Request.Method = "POST";
                Request.ContentType = "application/json; charset=utf-8";
                Request.Headers.Add("Authorization", "Basic " + Key);
                Request.Headers.Add("Accept-Encoding", "gzip");
                Request.KeepAlive = true;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls
                               | SecurityProtocolType.Tls11
                               | SecurityProtocolType.Tls12;
                               //| SecurityProtocolType.Ssl3;
                ServicePointManager.Expect100Continue = false;
                Request.Timeout = Timeout;
                Request.UseDefaultCredentials = true;
                Stream newStream = null;
                byte[] data = Encoding.ASCII.GetBytes(strxmlRQRS);
                Request.ContentLength = data.Length;
                newStream = Request.GetRequestStream();
                newStream.Write(data, 0, data.Length);

                HttpWebResponse Response = (HttpWebResponse)await Request.GetResponseAsync();
                Stream responseStream = Response.GetResponseStream();
                responseStream = new System.IO.Compression.GZipStream(responseStream, System.IO.Compression.CompressionMode.Decompress);
                StreamReader Reader = new StreamReader(responseStream, Encoding.Default);
                strOutput = await Reader.ReadToEndAsync();
                responseStream.Close();
                Reader.Close();
                Response.Close();
            }
            catch (Exception ex)
            {
                _ = _exception.Error(ex);
                //Error = ex.Message.ToString();
                //if (UniqueNo != "")
                //    Common.ThirdPartyBookingHistory(Convert.ToString(UniqueNo), Convert.ToString(UniqueNo), strxmlRQRS, ex.Message.ToString(), "Error Block", "Etg", "Price Check Response URL:" + strHost + ", Password:" + password, "-", "VivaBooking");
                _commonMethod.ThirdPartyBookingHistory("", "", strxmlRQRS, strOutput + ex + Key + strHost, " Prebook in Response", "ratehawk", "Prebook Response Confirm", "-", "Withinearth", "");
                _logger.LogError(ex, strOutput + ex+"Errot at prebook");
            }
            return strOutput;
        }

        public async Task<ResponseDto> GetETGRoomResponse(string strxmlRQRS, string strHost, string Key, string UniqueNo = "")
        {
            ResponseDto responseDto = new ResponseDto();
            System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
            timer.Start();
            string strOutput = "";
            string Error = "";
            try
            {
                HttpWebRequest Request;
                Request = (HttpWebRequest)HttpWebRequest.Create(strHost);
                Request.Method = "POST";
                Request.ContentType = "application/json; charset=utf-8";
                Request.Headers.Add("Authorization", "Basic " + Key);
                Request.Headers.Add("Accept-Encoding", "gzip");
                Request.KeepAlive = true;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls
                               | SecurityProtocolType.Tls11
                               | SecurityProtocolType.Tls12;
                //| SecurityProtocolType.Ssl3;
                ServicePointManager.Expect100Continue = false;
                Request.Timeout = 180000;
                Request.UseDefaultCredentials = true;
                Stream newStream = null;
                byte[] data = Encoding.ASCII.GetBytes(strxmlRQRS);
                Request.ContentLength = data.Length;
                newStream = Request.GetRequestStream();
                newStream.Write(data, 0, data.Length);

                HttpWebResponse Response = (HttpWebResponse)await Request.GetResponseAsync();
                Stream responseStream = Response.GetResponseStream();
                responseStream = new System.IO.Compression.GZipStream(responseStream, System.IO.Compression.CompressionMode.Decompress);
                StreamReader Reader = new StreamReader(responseStream, Encoding.Default);
                strOutput = await Reader.ReadToEndAsync();
                responseStream.Close();
                Reader.Close();
                Response.Close();
            }
            catch (Exception ex)
            {
                _ = _exception.Error(ex);
                Error = ex.Message.ToString();
                responseDto.Error = Error;
                //if (UniqueNo != "")
                //    Common.ThirdPartyBookingHistory(Convert.ToString(UniqueNo), Convert.ToString(UniqueNo), strxmlRQRS, ex.Message.ToString(), "Error Block", "Etg", "Price Check Response URL:" + strHost + ", Password:" + password, "-", "VivaBooking");
            }
            responseDto.Response = strOutput;
            return responseDto;
        }

        public string OSTVBookingRequest(DataSet dsCartDetails, DataTable dtPassengers, int i, int j, string Password, string UniqueNo, int NoOfRoom, string RoomTypeId, string HotelCode, string SellingAmount, string type, string Currency)
        {
            try
            {
                int No_Child = 0;
                int childAge1 = 0;
                string child1 = "";
                string child2 = "";
                string FirstName = "";
                string LastName = "";
                string BookingRequest = "";
                string language = "en";
                string partner_comment = "";
                string mail_Id = ETGInfo.ETG_Mail;
                string mobile = ETGInfo.ETG_Mobile;
                string comment = "comment string";
                StringBuilder BookingReq = new StringBuilder();
                BookingReq.Append("{");
                BookingReq.Append("\"user\": {");
                BookingReq.Append("\"email\": \"" + mail_Id + "\",");
                BookingReq.Append("\"comment\": \"" + comment + "\",");
                BookingReq.Append("\"phone\": \"" + mobile + "\"");
                BookingReq.Append("},");
                BookingReq.Append("\"partner\": {");
                BookingReq.Append("\"partner_order_id\": \"" + UniqueNo + "\",");
                BookingReq.Append("\"comment\": \"" + partner_comment + "\",");
                BookingReq.Append("\"amount_sell_b2b2c\": \"" + SellingAmount + "\"");
                BookingReq.Append("},");
                BookingReq.Append("\"language\": \"" + language + "\",");
                BookingReq.Append("\"rooms\": [");
                for (int r = 0; r < NoOfRoom; r++)
                {
                    BookingReq.Append("{");
                    BookingReq.Append("\"guests\": [");
                    dtPassengers.DefaultView.RowFilter = "PickUp='" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomTypeId"]) + "' and UserCartID=" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["UserCartID"] + " and RoomNo='" + Convert.ToInt32(r + 1) + "'");

                    for (int k = 0; k < dtPassengers.DefaultView.Count; k++)
                    {
                        childAge1 = Convert.ToInt32(dtPassengers.DefaultView[k]["ChildAge"]);
                        FirstName = Convert.ToString(dtPassengers.DefaultView[k]["FirstName"]);
                        LastName = Convert.ToString(dtPassengers.DefaultView[k]["LastName"]);
                        if (Convert.ToString(dtPassengers.DefaultView[k]["PaxType"]).Trim().ToLower() == "adult")
                        {
                            BookingReq.Append("{");
                            BookingReq.Append("\"first_name\": \"" + FirstName + "\",");
                            BookingReq.Append("\"last_name\": \"" + LastName + "\"");
                        }
                        if (Convert.ToString(dtPassengers.DefaultView[k]["PaxType"]).Trim().ToLower() != "adult")
                        {
                            BookingReq.Append("{");
                            BookingReq.Append("\"first_name\": \"" + FirstName + "\",");
                            BookingReq.Append("\"last_name\": \"" + LastName + "\",");
                            BookingReq.Append("\"is_child\": true,");
                            BookingReq.Append("\"age\": " + childAge1);
                            No_Child++;
                        }
                        BookingReq.Append("}");
                        if (dtPassengers.DefaultView.Count - 1 != k)
                            BookingReq.Append(",");
                    }
                    BookingReq.Append("]}");
                    if (NoOfRoom - 1 != r)
                        BookingReq.Append(",");
                }
                BookingReq.Append("],");
                BookingReq.Append("\"upsell_data\": [");
                BookingReq.Append("],");
                BookingReq.Append("\"payment_type\": {");
                BookingReq.Append("\"type\": \"" + type + "\",");
                BookingReq.Append("\"amount\": \"" + SellingAmount + "\",");
                BookingReq.Append("\"currency_code\": \"" + Currency + "\"");
                BookingReq.Append("}}");
                BookingRequest = BookingReq.ToString();

                return BookingRequest;
            }
            catch (Exception ex)
            {
                _ = _exception.Error(ex);
                return "";
            }
        }
        public string OSTVBookingRequest_newvesrion(BookRequest_newversion bookRequest, DataTable dtPassengers, int i, int j, string Password, string UniqueNo, int NoOfRoom, string RoomTypeId, string HotelCode, string SellingAmount, string type, string Currency)
        {
            try
            {
                

                int No_Child = 0;
                int childAge1 = 0;
                string child1 = "";
                string child2 = "";
                string FirstName = "";
                string LastName = "";
                string BookingRequest = "";
                string language = "en";
                string partner_comment = "";
                string mail_Id = _supplierConfig.ETG.ETG_Mail;
                string mobile = _supplierConfig.ETG.ETG_Mobile;
                string comment = "comment string";
                StringBuilder BookingReq = new StringBuilder();
                BookingReq.Append("{");
                BookingReq.Append("\"user\": {");
                BookingReq.Append("\"email\": \"" + mail_Id + "\",");
                BookingReq.Append("\"comment\": \"" + comment + "\",");
                BookingReq.Append("\"phone\": \"" + mobile + "\"");
                BookingReq.Append("},");
                BookingReq.Append("\"partner\": {");
               // BookingReq.Append("\"partner_order_id\": \"" + UniqueNo + "\",");
                BookingReq.Append("\"partner_order_id\": \"" + UniqueNo + "\",");
                BookingReq.Append("\"comment\": \"" + partner_comment + "\",");
                BookingReq.Append("\"amount_sell_b2b2c\": \"" + SellingAmount + "\"");
                BookingReq.Append("},");
                BookingReq.Append("\"language\": \"" + language + "\",");
                BookingReq.Append("\"rooms\": [");
                for (int r = 0; r < NoOfRoom; r++)
                {
                    BookingReq.Append("{");
                    BookingReq.Append("\"guests\": [");
                    dtPassengers.DefaultView.RowFilter = "PickUp='" + Convert.ToString(bookRequest.userCart.UserCartDetails[r].RoomTypeId) + "' and UserCartID=" + Convert.ToString(bookRequest.userCart.UserCartDetails[r].UserCartId + " and RoomNo='" + Convert.ToInt32(r + 1) + "'");

                    for (int k = 0; k < dtPassengers.DefaultView.Count; k++)
                    {
                        childAge1 = Convert.ToInt32(dtPassengers.DefaultView[k]["ChildAge"]);
                        FirstName = Convert.ToString(dtPassengers.DefaultView[k]["FirstName"]);
                        LastName = Convert.ToString(dtPassengers.DefaultView[k]["LastName"]);
                        if (Convert.ToString(dtPassengers.DefaultView[k]["PaxType"]).Trim().ToLower() == "adult")
                        {
                            BookingReq.Append("{");
                            BookingReq.Append("\"first_name\": \"" + FirstName + "\",");
                            BookingReq.Append("\"last_name\": \"" + LastName + "\"");
                        }
                        if (Convert.ToString(dtPassengers.DefaultView[k]["PaxType"]).Trim().ToLower() != "adult")
                        {
                            BookingReq.Append("{");
                            BookingReq.Append("\"first_name\": \"" + FirstName + "\",");
                            BookingReq.Append("\"last_name\": \"" + LastName + "\",");
                            BookingReq.Append("\"is_child\": true,");
                            BookingReq.Append("\"age\": " + childAge1);
                            No_Child++;
                        }
                        BookingReq.Append("}");
                        if (dtPassengers.DefaultView.Count - 1 != k)
                            BookingReq.Append(",");
                    }
                    BookingReq.Append("]}");
                    if (NoOfRoom - 1 != r)
                        BookingReq.Append(",");
                }
                BookingReq.Append("],");
                BookingReq.Append("\"upsell_data\": [");
                BookingReq.Append("],");
                BookingReq.Append("\"payment_type\": {");
                BookingReq.Append("\"type\": \"" + type + "\",");
                BookingReq.Append("\"amount\": \"" + SellingAmount + "\",");
                BookingReq.Append("\"currency_code\": \"" + Currency + "\"");
                BookingReq.Append("}}");
                BookingRequest = BookingReq.ToString();

                return BookingRequest;
            }
            catch (Exception ex)
            {
                _ = _exception.Error(ex);
                return "";
            }
        }
        public string OSTVBookingFinishStatusRequest(string UniqueNo)
        {
            try
            {
                string req = "";
                StringBuilder BookingFSReq = new StringBuilder();
                BookingFSReq.Append("{");
                BookingFSReq.Append("\"partner_order_id\": \"" + UniqueNo + "\"");
                BookingFSReq.Append("}");
                req = BookingFSReq.ToString();
                return req;
            }
            catch (Exception ex)
            {
                _ = _exception.Error(ex);
                return "";
            }
        }

        public async Task<ETGPriceCheck> Get_OSTV_PriceCheck(DataSet dsCartDetails, DataRow[] Ostrovok_Details, int i, int j, DataTable dtCheckPriceReqStructure, string UniqueNo, bool isForBooking, string IsFor, string URl, TokenData TokenData, string xslRequest, string ResponseData, string order_id,string UserPlatform)
        {
            ETGPriceCheck eTGPriceCheck = new ETGPriceCheck();
            string UserName = Convert.ToString(Ostrovok_Details[0]["UserName"]);
            string Password = Convert.ToString(Ostrovok_Details[0]["Password"]);
            string SupplierName = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["SupplierName"]).ToLower();
            double CompanyBuyingPrice = Convert.ToDouble(dsCartDetails.Tables[1].DefaultView[0]["CompanyBuyingPrice"]);
            double FinalPrice = 0;
            string Error = "";
            string time = "";

            try
            {
                #region Price Check RQ and Declaration
                #region Data

                #region New BookHas
                string[] BookingPolicy = Convert.ToString(dsCartDetails.Tables[1].DefaultView[i]["purchaseToken"]).Split(new String[] { "$$$" }, StringSplitOptions.None);
                string booking_policy_id = BookingPolicy[0];
                string Etg_UniqueId = booking_policy_id;
                #endregion

                double CurrencyFactor = 1;
                DataSet dsTemp = new DataSet();
                string[] array = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomTypeId"]).Split(new String[] { "$$$" }, StringSplitOptions.None);
                string roomId = array[0];

                DataTable dtROE = TokenData.dtROE;

                string language = "en";
                string UserIP = UserPlatform;
                UserIP = UserIP.Split(new String[] { "$$" }, StringSplitOptions.None)[1];
                //string UserIP = Common.GetIPAddress();
                int UserCartId = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[j]["UserCartID"]);
                //string userCartId = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["UserCartID"]);
                string UserCartDetailId = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["UserCartDetailId"]);
                string Order_Booking_Id = UserCartDetailId;
                string RoomTypeId = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomTypeID"]);
                string FinalBookHash = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["EachoToken"]);
                #endregion
                xslRequest = Convert.ToString(OSTVPriceCheckRequest(Order_Booking_Id, FinalBookHash, language, UserIP));
                eTGPriceCheck.xslRequest = xslRequest;
                _commonMethod.ThirdPartyBookingHistory(Convert.ToString(UserCartDetailId), Convert.ToString(UniqueNo), xslRequest, "-", "After Hit Price Check Request", "ratehawk", "Price Check Response Pending", "-", "WithinEarth");
                #endregion

                if (xslRequest != "")
                {
                    if (!isForBooking)
                    {
                       // dtCheckPriceReqStructure.Rows.Add(AddCheckPriceReq(dtCheckPriceReqStructure.NewRow(), i + 1, UserCartId, Convert.ToInt32(UserCartDetailId), RoomTypeId, SupplierName, xslRequest));
                    }
                    #region Price Check RS
                    string Key = Convert.ToBase64String(Encoding.ASCII.GetBytes(UserName + ":" + Password));
                    System.Diagnostics.Stopwatch timerroomprocess = new System.Diagnostics.Stopwatch();
                    timerroomprocess.Start();
                    //ResponseData = GetOstrovokRoomResponse(xslRequest, URl, Key, ref Error, UniqueNo);
                    ResponseData = await GetRatehawkRoomResponse(xslRequest, URl, Key, UniqueNo);
                    eTGPriceCheck.RatehawkResponse = ResponseData;
                    timerroomprocess.Stop();
                    time = timerroomprocess.Elapsed.ToString();
                    _commonMethod.ThirdPartyBookingHistory(Convert.ToString(UserCartDetailId), Convert.ToString(UniqueNo), xslRequest, ResponseData + Error, "After Hit Price Check Response", "ratehawk", "Price Check Response Confirm", "-", "WithinEarth");

                    if (Error != "")
                    {
                        _commonMethod.ThirdPartyBookingHistory(Convert.ToString(UserCartDetailId), Convert.ToString(UniqueNo), xslRequest, ResponseData + Error, "After Hit Price Check Response", "ratehawk", "Price Check  Response Not Confirm", "-", "WithinEarth");

                    }
                    if (ResponseData == string.Empty || ResponseData == "" || ResponseData == null)
                    {
                        eTGPriceCheck.ispriceupdate = true;
                        return eTGPriceCheck;
                    }


                    DataSet EtgResponse = new DataSet();
                    XmlDocument doc = Newtonsoft.Json.JsonConvert.DeserializeXmlNode("{\"Row\":" + ResponseData + "}", "root");
                    XmlReader xmlReader = new XmlNodeReader(doc);
                    EtgResponse.ReadXml(xmlReader);
                    #endregion

                    #region Price Check Logic
                    if (ResponseData != null && EtgResponse.Tables[0].Rows[0]["status"].ToString() == "ok")
                    {
                        {

                            for (int h = 0; h < EtgResponse.Tables[0].Rows.Count; h++)
                            {
                                DataRow[] Payment_types = EtgResponse.Tables["payment_types"].Select("is_need_credit_card_data='" + false + "'");
                                double sellingAmount = Convert.ToDouble(Payment_types[0]["amount"]);
                                double commission = 0;
                                string Currency = Convert.ToString(Payment_types[0]["currency_code"]);
                                if (Currency.ToLower() != TokenData.SiteCurrency.ToLower())
                                {
                                    CurrencyFactor = _commonMethod.GetROEByCurrency(Currency, TokenData.dtROE);

                                }
                                //end ddr
                                FinalPrice = (sellingAmount + commission) * CurrencyFactor;
                                order_id = Convert.ToString(EtgResponse.Tables[1].Rows[0]["order_id"]);
                                eTGPriceCheck.booking_form_order_id = order_id;
                            }
                            if (IsFor == "P")
                            {
                                CompanyBuyingPrice = Convert.ToDouble(dsCartDetails.Tables[1].DefaultView[j]["CompanyBuyingPrice"]);
                            }
                            else
                            {
                                CompanyBuyingPrice = Convert.ToDouble(dsCartDetails.Tables[1].DefaultView[j]["Rate"]);
                            }
                            FinalPrice = Convert.ToDouble(Math.Round(FinalPrice, 2));


                            if (Convert.ToDouble(Math.Round(FinalPrice.TruncateDecimalPlaces(2))) > Math.Round(CompanyBuyingPrice.TruncateDecimalPlaces(2)))
                            {
                                eTGPriceCheck.ispriceupdate = true;
                                return eTGPriceCheck;
                            }

                        }
                    }
                    #endregion
                }
                else
                {

                    eTGPriceCheck.ispriceupdate = true;
                    return eTGPriceCheck;
                }
            }
            catch (Exception ex)
            {
                
                _ = _exception.Error(ex);
                eTGPriceCheck.ispriceupdate = true;
                return eTGPriceCheck;
            }

            eTGPriceCheck.ispriceupdate = false;
            return eTGPriceCheck;
        }
        public async Task<ETGPriceCheck> Get_OSTV_PriceCheck_newversion(BookRequest_newversion bookRequest, DataRow[] Ostrovok_Details, int i, int j, string UniqueNo, bool isForBooking, string IsFor, string URl, TokenData TokenData, string xslRequest, string ResponseData, string order_id, string UserPlatform)
        {
            ETGPriceCheck eTGPriceCheck = new ETGPriceCheck();
            string UserName = Convert.ToString(Ostrovok_Details[0]["UserName"]);
            string Password = Convert.ToString(Ostrovok_Details[0]["Password"]);
            string SupplierName = Convert.ToString(bookRequest.userCart.UserCartDetails[j].SupplierName).ToLower();
            // double CompanyBuyingPrice = Convert.ToDouble(dsCartDetails.Tables[1].DefaultView[0]["CompanyBuyingPrice"]);
            double CompanyBuyingPrice = Convert.ToDouble(bookRequest.userCart.UserCartDetails[j].ServiceBuyingAmount);
            double FinalPrice = 0;
            string Error = "";
            string time = "";

            try
            {
                #region Price Check RQ and Declaration
                #region Data

                #region New BookHas
                string[] BookingPolicy = Convert.ToString(bookRequest.userCart.UserCartDetails[j].PurchaseToken).Split(new String[] { "$$$" }, StringSplitOptions.None);
                string booking_policy_id = BookingPolicy[0];
                string Etg_UniqueId = booking_policy_id;
                #endregion

                double CurrencyFactor = 1;
                DataSet dsTemp = new DataSet();
                string[] array = Convert.ToString(bookRequest.userCart.UserCartDetails[j].RoomTypeId).Split(new String[] { "$$$" }, StringSplitOptions.None);
                string roomId = array[0];

                DataTable dtROE = TokenData.dtROE;

                string language = "en";
                string UserIP = UserPlatform;
                UserIP = UserIP.Split(new String[] { "$$" }, StringSplitOptions.None)[1];
                //string UserIP = Common.GetIPAddress();
                int UserCartId = Convert.ToInt32(bookRequest.userCart.UserCartDetails[j].UserCartId);
                //string userCartId = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["UserCartID"]);
                string UserCartDetailId = Convert.ToString(bookRequest.userCart.UserCartDetails[j].UserCartDetailId);
                string Order_Booking_Id = UserCartDetailId;
                string RoomTypeId = Convert.ToString(bookRequest.userCart.UserCartDetails[j].RoomTypeId);
                // string FinalBookHash = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["EachoToken"]);
                string FinalBookHash = Convert.ToString(bookRequest.userCart.UserCartDetails[j].EachoToken);
                #endregion
                xslRequest = Convert.ToString(OSTVPriceCheckRequest(Order_Booking_Id, FinalBookHash, language, UserIP));
                eTGPriceCheck.xslRequest = xslRequest;
                _commonMethod.ThirdPartyBookingHistory(Convert.ToString(UserCartDetailId), Convert.ToString(UniqueNo), xslRequest, "-", "After Hit order booking form Request", "ratehawk", "order booking form  Response Pending", "-", "WithinEarth");
                #endregion

                if (xslRequest != "")
                {
                    if (!isForBooking)
                    {
                        // dtCheckPriceReqStructure.Rows.Add(AddCheckPriceReq(dtCheckPriceReqStructure.NewRow(), i + 1, UserCartId, Convert.ToInt32(UserCartDetailId), RoomTypeId, SupplierName, xslRequest));
                    }
                    #region Price Check RS
                    string Key = Convert.ToBase64String(Encoding.ASCII.GetBytes(UserName + ":" + Password));
                    System.Diagnostics.Stopwatch timerroomprocess = new System.Diagnostics.Stopwatch();
                    timerroomprocess.Start();
                    //ResponseData = GetOstrovokRoomResponse(xslRequest, URl, Key, ref Error, UniqueNo);
                    ResponseData = await GetRatehawkRoomResponse(xslRequest, URl, Key, UniqueNo,120000);
                    eTGPriceCheck.RatehawkResponse = ResponseData;
                    timerroomprocess.Stop();
                    time = timerroomprocess.Elapsed.ToString();
                    _commonMethod.ThirdPartyBookingHistory(Convert.ToString(UserCartDetailId), Convert.ToString(UniqueNo), xslRequest, ResponseData + Error, "After Hit order booking form Response", "ratehawk", "order booking formResponse Confirm", "-", "WithinEarth");

                    if (Error != "")
                    {
                        _commonMethod.ThirdPartyBookingHistory(Convert.ToString(UserCartDetailId), Convert.ToString(UniqueNo), xslRequest, ResponseData + Error, "After Hit order booking form Response", "ratehawk", "order booking form Response Not Confirm", "-", "WithinEarth");

                    }
                    if (ResponseData == string.Empty || ResponseData == "" || ResponseData == null)
                    {
                        eTGPriceCheck.ispriceupdate = true;
                        return eTGPriceCheck;
                    }


                    DataSet EtgResponse = new DataSet();
                    XmlDocument doc = Newtonsoft.Json.JsonConvert.DeserializeXmlNode("{\"Row\":" + ResponseData + "}", "root");
                    XmlReader xmlReader = new XmlNodeReader(doc);
                    EtgResponse.ReadXml(xmlReader);
                    #endregion

                    #region Price Check Logic
                    if (ResponseData != null && EtgResponse.Tables[0].Rows[0]["status"].ToString() == "ok")
                    {
                        {

                            for (int h = 0; h < EtgResponse.Tables[0].Rows.Count; h++)
                            {
                                DataRow[] Payment_types = EtgResponse.Tables["payment_types"].Select("is_need_credit_card_data='" + false + "'");
                                double sellingAmount = Convert.ToDouble(Payment_types[0]["amount"]);
                                double commission = 0;
                                string Currency = Convert.ToString(Payment_types[0]["currency_code"]);
                                if (Currency.ToLower() != TokenData.SiteCurrency.ToLower())
                                {
                                    CurrencyFactor = _commonMethod.GetROEByCurrency(Currency, TokenData.dtROE);

                                }
                                //end ddr
                                FinalPrice = (sellingAmount + commission) * CurrencyFactor;
                                order_id = Convert.ToString(EtgResponse.Tables[1].Rows[0]["order_id"]);
                                eTGPriceCheck.booking_form_order_id = order_id;
                            }
                            if (IsFor == "P")
                            {
                                CompanyBuyingPrice = Convert.ToDouble(bookRequest.userCart.UserCartDetails[j].ServiceBuyingAmount);
                            }
                            else
                            {
                                //CompanyBuyingPrice = Convert.ToDouble(dsCartDetails.Tables[1].DefaultView[j]["Rate"]);
                                CompanyBuyingPrice = Convert.ToDouble(bookRequest.userCart.UserCartDetails[j].BaseServiceBuyingAmount);
                            }
                            FinalPrice = Convert.ToDouble(Math.Round(FinalPrice, 2));


                            if (Convert.ToDouble(Math.Round(FinalPrice.TruncateDecimalPlaces(2))) > Math.Round(CompanyBuyingPrice.TruncateDecimalPlaces(2)))
                            {
                                eTGPriceCheck.ispriceupdate = true;
                                return eTGPriceCheck;
                            }

                        }
                    }
                    #endregion
                }
                else
                {

                    eTGPriceCheck.ispriceupdate = true;
                    return eTGPriceCheck;
                }
            }
            catch (Exception ex)
            {

                _ = _exception.Error(ex);
                eTGPriceCheck.ispriceupdate = true;
                return eTGPriceCheck;
            }

            eTGPriceCheck.ispriceupdate = false;
            return eTGPriceCheck;
        }

        public string OSTVPriceCheckRequest(string UserCartDetailId, string Etg_UniqueId, string language, string UserIP)
        {
            string PriceCheckReq = "";
            try
            {
                StringBuilder SearchRQ = new StringBuilder();
                SearchRQ.Append("{");
                SearchRQ.Append("\"partner_order_id\": \"" + UserCartDetailId + "\",");
                SearchRQ.Append("\"book_hash\": \"" + Etg_UniqueId + "\",");
                SearchRQ.Append("\"language\": \"" + language + "\",");
                SearchRQ.Append("\"user_ip\": \"" + UserIP + "\"");
                SearchRQ.Append("}");

                PriceCheckReq = SearchRQ.ToString();

                return PriceCheckReq;
            }
            catch (Exception ex)
            {
                _=_exception.Error(ex);
                return "";
            }
        }

        public string EtgBookingCancelRQ(string Id)//added on 03-Jul-2025
        {
            try
            {
                #region Booking Cancel RQ
                StringBuilder sb = new StringBuilder();
                sb.Append("{");
                sb.Append("\"partner_order_id\": \"" + Id + "\"");
                sb.Append("}");
                return sb.ToString();
                #endregion
            }
            catch (Exception ex)
            {
                _ = _exception.ErrorNew(ex, _LogAPI.CancelBooking);
                return "";
            }
        }

        public async Task<DataSet> Ratehawk_cancellation(string ISBooking, double CompanyBuy, string MealCode, DataSet dsHotel, string hotelcode, string ownsystemid, string roomtypeid, DataTable dtAgentDetail, DataTable dtSuplierMarkup, DataTable dtSearchConfig, HotelSearchParameter SearchPara, string roomno, string provider, int AgentId, DataTable dtMarkupDetails, TokenData TokenData, string SearchKey = "", string GiataId = "0", int hotelRating = 0, string xmlcity = "", string TechName = "")
        {
            //DataTable dtEtgCancellation = GenerateCancellationDataTableNewV2();
            //DataTable dtEtgHotelRoom = Common.GenerateRoomtypeDataTableNewV2();
           // DataTable dtEtgHotelRoomRateBreackUp = Common.GenerateoomRateBreackUpDataTableNewV2();
            DataTable dtEtgHotelRoom = _commonMethod.GenerateRoomtypeDataTableNewV2();
            DataTable dtEtgCancellation = _commonMethod.GenerateCancellationDataTableNewV2();
            DataTable dtEtgHotelRoomRateBreackUp = _commonMethod.GenerateoomRateBreackUpDataTableNewV2();
           string Error = "";
            string responsetype = "Success";
            string time = "";
            try
            {
                ISBooking = "NotAvailable";
                DataTable dtROE = TokenData.dtROE;
                double CurrencyFactorRatehawk = 1;
                DataRow[] drSuplierMarkup = (dtSuplierMarkup.Select("SupplierName='" + provider + "'"));
                int SupplierId = Convert.ToInt32(drSuplierMarkup[0]["SupplierID"]);
                string userName = Convert.ToString(drSuplierMarkup[0]["Username"]);
                string password = Convert.ToString(drSuplierMarkup[0]["Password"]);
                //string URL = drSuplierMarkup[0]["WebSiteURL"] + "/search/hp/";
                string URL =Convert.ToString(drSuplierMarkup[0]["WebSiteURL"]) ;
                DataRow[] drSearchConfig = (dtSearchConfig.Select("SupplierID='" + SupplierId + "'"));
                int SearchTimeOut = Convert.ToInt32(drSearchConfig[0]["BookingConfirmationTimeout"]);

                string[] HBArray = Convert.ToString(roomtypeid).Split(new String[] { "$$$" }, StringSplitOptions.None);
                string RoomCode = HBArray[0]; //RoomName
                string Hotel_Id = HBArray[2]; //Hotel Code
                string XMLNationality = HBArray[4]; //Nationality
                string roomTimeMatchHas = HBArray[5]; //MatchHas

                string essential = "";
                try
                {
                    essential = await RateHawk_EssentialInfo(SearchPara, provider, Hotel_Id, userName, password, "", "", roomtypeid, URL, SearchKey);

                    //essential = ThirdPartyXML.Get_ratehawk_EInfo(Hotel_Id, ownsystemid, roomtypeid, SearchPara, password, URL, userName, SearchPara.SearchKey);
                }
                catch (Exception ex)
                {
                    _exception.Error(ex);
                }

               

                double CompanyBuyPrice = Convert.ToDouble(CompanyBuy);


                int CutOffDays = XMLHotelBookingCommon.CutOffDays;
                CutOffDays = _commonMethod.GetCutOffDays(dtSuplierMarkup, provider);

                //int CutOffDays = XMLHotelBookingCommon.CutOffDays;
                //CutOffDays = GetCutOffDays(dtSuplierMarkup, provider);
                Boolean isPerce = true;
                double AgentPercentage = 0.0;
                Boolean blnIsShowSupplier = false;
                double AgentMarkupAmount = 0.0;
                //Common.getAgetMarkupSetting(provider, GiataId, dtSuplierMarkup, dtMarkupDetails, ref AgentPercentage, ref AgentMarkupAmount, ref blnIsShowSupplier, ref isPerce, Convert.ToInt32(dtAgentDetail.Rows[0]["AgentID"]), hotelRating, SearchPara.CityID, Convert.ToString(SearchPara.CountryID), Convert.ToString(SearchPara.CheckInDate));
               _markup.getAgetMarkupSetting(provider, Convert.ToString(GiataId), dtSuplierMarkup, dtMarkupDetails, ref AgentPercentage, ref AgentMarkupAmount, ref blnIsShowSupplier, ref isPerce, Convert.ToInt32(dtAgentDetail.Rows[0]["AgentID"]), hotelRating, SearchPara.CityID, Convert.ToString(SearchPara.CountryID), SearchPara.CheckInDate);

                double AgentCompanyPercentage = Convert.ToDouble(SearchPara.AgentCompanyPercentage);
                bool IsB2C = Convert.ToBoolean(Convert.ToByte(SearchPara.IsB2BOrB2C));

                double B2CMarkup = (IsB2C == false ? AgentCompanyPercentage : XMLHotelBookingCommon.B2CMarkup);
                //double B2CMarkup = (IsB2C == false ? AgentCompanyPercentage : 10);
                if (IsB2C == true)
                    AgentPercentage = 0;

                string RETGCurrency = Convert.ToString(drSuplierMarkup[0]["CurrencyShortName"]);
                double CurrencyFactorETG = 1;
                //if (RETGCurrency.ToLower() != TokenData.SiteCurrency.ToLower())
                //{
                //    CurrencyFactorETG = _commonMethod.GetROEByCurrency(RETGCurrency, TokenData.dtROE);
                //}

               // string Currency = Convert.ToString(ConfigurationManager.AppSettings["RatehawkCurrency"]);
                string RoomRequest = ETGHotelRequest(SearchPara, XMLNationality, Hotel_Id, xmlcity, RETGCurrency);
                System.Diagnostics.Stopwatch timerroomprocess = new System.Diagnostics.Stopwatch();
                timerroomprocess.Start();

                string ETGURL = URL + _supplierConfig.ETG.search_serp_hp;
                string RoomResponseData = ResponseETGSupplier(ref Error, RoomRequest, ETGURL, userName, password, "ratehawk");

                timerroomprocess.Stop();
                time = timerroomprocess.Elapsed.ToString();

                if (RoomResponseData == "")
                {
                    try
                    {
                        responsetype = "GeneralError";
                        string abcd = await _commonMethod.AnalyticsRecheckData(provider, responsetype, time, SearchKey, TechName);
                    }
                    catch (Exception e)
                    {
                        _ = _exception.ErrorNew(e, _LogAPI.Recheck, SearchKey);
                    }
                    return dsHotel;
                }
                   


               DataSet ratehawkResponse = null;
                string ConvertXml = "";
                try
                {
                    //ConvertXml = Newtonsoft.Json.JsonConvert.DeserializeXmlNode(objResponse.Response).InnerXml;
                    ConvertXml = Newtonsoft.Json.JsonConvert.DeserializeXmlNode(RoomResponseData, "root").InnerXml;
                    ratehawkResponse = _commonMethod.GetDataSetFromXMLString(ConvertXml);
                }
                catch (Exception ex)
                {
                    _ = _exception.ErrorBooking(ex, TokenData.AgentId.ToString());
                }

                if (ratehawkResponse == null)
                {
                    try
                    {
                        responsetype = "GeneralError";
                        string abcd = await _commonMethod.AnalyticsRecheckData(provider, responsetype, time, SearchKey, TechName);
                    }
                    catch (Exception e)
                    {
                        _ = _exception.ErrorNew(e, _LogAPI.Recheck, SearchKey);
                    }
                    return dsHotel;
                }
                    

                _commonMethod.ThirdPartyGetPolicyHistory(Convert.ToString(RoomCode), Convert.ToString(ownsystemid), Convert.ToString(RoomRequest), Convert.ToString(RoomResponseData + Error), "Room Request Response before Policy", provider, "Room Response", SearchKey, "FSTravel Booking");

                string book_hash = "";
                string roomName = "";
                string mealName = "";

                double SubAgentMarkup = 0;
                int IsParentId = 1;
                if (dtAgentDetail != null)
                {
                    IsParentId = Convert.ToInt32(dtAgentDetail.Rows[0]["IsParentId"]);
                    SubAgentMarkup = Convert.ToDouble(dtAgentDetail.Rows[0]["B2CMarkupPer"]);
                }

                for (int i = 0; i < ratehawkResponse.Tables["rates"].Rows.Count; i++)
                {
                    string match_has = Convert.ToString(ratehawkResponse.Tables["rates"].Rows[i]["match_hash"]);
                    if (match_has != "")
                    {
                        if (roomTimeMatchHas == match_has)
                        {
                            ISBooking = "Available";
                            book_hash = Convert.ToString(ratehawkResponse.Tables["rates"].Rows[i]["book_hash"]);
                            roomName = Convert.ToString(ratehawkResponse.Tables["rates"].Rows[i]["room_name"]);
                            mealName = Convert.ToString(ratehawkResponse.Tables["rates"].Rows[i]["meal"]);
                            if (mealName.ToLower() == "nomeal" || mealName == "")
                            {
                                mealName = "Room Only";
                            }

                            // policy code
                            int payment_optionId = Convert.ToInt32(ratehawkResponse.Tables["payment_options"].Rows[i]["payment_options_Id"]);


                            DataRow[] payment_type = ratehawkResponse.Tables["payment_types"].Select("payment_options_Id='" + payment_optionId + "'");
                            int payment_typeId = Convert.ToInt32(payment_type[0]["payment_types_Id"]);

                            DataRow[] cancellation_penalty = ratehawkResponse.Tables["cancellation_penalties"].Select("payment_types_Id='" + payment_typeId + "'");
                            int cancellation_penaltyId = Convert.ToInt32(cancellation_penalty[0]["cancellation_penalties_Id"]);
                            string free_cancellation_before = Convert.ToString(cancellation_penalty[0]["free_cancellation_before"]);
                            RETGCurrency = Convert.ToString(payment_type[0]["currency_code"]);
                            //if (Currency.ToLower() != Convert.ToString(ConfigurationManager.AppSettings["SiteCurrency"]))
                            //{
                            //    CurrencyFactorRatehawk = Common.GetROEByCurrency(Currency, dtROE);
                            //}

                            if (RETGCurrency.ToLower() != TokenData.SiteCurrency.ToLower())
                            {
                                CurrencyFactorETG = _commonMethod.GetROEByCurrency(RETGCurrency, TokenData.dtROE);
                            }

                            DataRow[] policies = ratehawkResponse.Tables["policies"].Select("cancellation_penalties_Id='" + payment_optionId + "'");

                            #region CancelltionPolicy New Added 11-02-2026
                            string curDate1 = DateTime.Now.Date.ToString("dd/MM/yyyy");
                            string currentdate = (Convert.ToString(curDate1)).Substring(3, 2) + "/" + (Convert.ToString(curDate1)).Substring(0, 2) + "/" + (Convert.ToString(curDate1)).Substring(6, 4);
                            double ChargeAmount = 0;
                            DateTime curDate = DateTime.Now.Date;
                            double Zero = 0;

                            DateTime CancelDate = DateTime.ParseExact(currentdate, "MM/dd/yyyy", null);
                            string cancellation = "";

                            if (string.IsNullOrEmpty(free_cancellation_before))
                            {
                                CancelDate = DateTime.Now.Date;
                                cancellation = "nonref";
                                ChargeAmount = Convert.ToDouble(policies[0]["amount_show"]) * CurrencyFactorRatehawk;
                            }
                            else
                            {
                                DateTime free_cancellation_before1 = Convert.ToDateTime(free_cancellation_before);
                                CancelDate = free_cancellation_before1;// DateTime.ParseExact(free_cancellation_before, "dd/MM/yyyy", null);
                                cancellation = Convert.ToString(free_cancellation_before1);
                                DataRow lastRow = policies.LastOrDefault();
                                ChargeAmount = Convert.ToDouble(lastRow["amount_show"]) * CurrencyFactorRatehawk;
                            }

                            if (CancelDate.AddDays(CutOffDays).Date > curDate)
                            {
                                CancelDate = CancelDate.AddDays(CutOffDays).Date;
                            }
                            else
                            {
                                CancelDate = DateTime.Now.Date;
                            }
                            double VatOnPolicy = 0;
                            double VatOnPolicyChargeAmount = ChargeAmount;
                            double CompanyChargeAmount = ChargeAmount;

                            double MainAgentCharge = ChargeAmount;
                            ChargeAmount = _markup.ProcessHotelCancelationPrice_WithMarkup(ref MainAgentCharge, SearchPara.NoofNights, SearchPara.CityID, Convert.ToString(SearchPara.CountryID), ChargeAmount, isPerce, B2CMarkup, AgentPercentage, AgentMarkupAmount, IsParentId, SubAgentMarkup, ref VatOnPolicy);
                            double CancelCharge = ChargeAmount;
                            if (curDate < CancelDate)
                            {
                                dtEtgCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref Zero, dtEtgCancellation.NewRow(), CurrencyFactorRatehawk, RETGCurrency, provider, Convert.ToString(ownsystemid), Convert.ToString(roomno), Convert.ToString(roomtypeid),
                                                                          roomName, Convert.ToString(""), curDate, CancelDate.AddDays(-1).AddHours(23).AddMinutes(59).AddSeconds(59), 0, 0, "", 0, "", "NO", book_hash, essential, "", ""));
                                dtEtgCancellation.AcceptChanges();

                                dtEtgCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref CompanyChargeAmount, dtEtgCancellation.NewRow(), CurrencyFactorRatehawk, RETGCurrency, provider, Convert.ToString(ownsystemid), Convert.ToString(roomno), Convert.ToString(roomtypeid),
                                                                      roomName, Convert.ToString(""), CancelDate, DateTime.ParseExact(SearchPara.CheckInDate, "dd/MM/yyyy", null), ChargeAmount, ChargeAmount, "", 0, "", "NO", book_hash, essential, "", ""));
                                dtEtgCancellation.AcceptChanges();
                            }
                            else
                            {
                                dtEtgCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref CompanyChargeAmount, dtEtgCancellation.NewRow(), CurrencyFactorRatehawk, RETGCurrency, provider, Convert.ToString(ownsystemid), Convert.ToString(roomno), Convert.ToString(roomtypeid),
                                                                           roomName, Convert.ToString(""), curDate, DateTime.ParseExact(SearchPara.CheckInDate, "dd/MM/yyyy", null), ChargeAmount, ChargeAmount, "", 0, "", "YES", book_hash, essential, "", ""));
                                dtEtgCancellation.AcceptChanges();
                            }
                            dsHotel.Tables[2].Merge(dtEtgCancellation);
                            #endregion
                            

                        }
                    }
                }
            }
            catch (Exception exx)
            {
                ISBooking = "NotAvailable";
                _exception.Error(exx);
                return dsHotel;
            }
            try
            {
                responsetype = "Success";
                string abcd = await _commonMethod.AnalyticsRecheckData(provider, responsetype, time, SearchKey, TechName);
            }
            catch (Exception e)
            {
                _ = _exception.ErrorNew(e, _LogAPI.Recheck, SearchKey);
            }
            return dsHotel;
        }
        public  string ETGHotelRequest(HotelSearchParameter SearchParam, string Nationality, string ETGHotelIds, string Citycode, string Currency)
        {
            try
            {
                DataTable dtPriceDetail =  _commonMethod.GetRoomPriceTableStructure();
                int RoomNo = SearchParam.Rooms.Count;
                int MaxRating = SearchParam.StarStart;
                //string residency = "gb";
                string language = "en";
                int Adult = 0;
                int Child = 0;
                //int region_id = 965849721;

                string checkin2 = (SearchParam.CheckInDate).Substring(6, 4) + "-" + (SearchParam.CheckInDate).Substring(3, 2) + "-" + (SearchParam.CheckInDate).Substring(0, 2);
                string checkout2 = (SearchParam.CheckOutDate).Substring(6, 4) + "-" + (SearchParam.CheckOutDate).Substring(3, 2) + "-" + (SearchParam.CheckOutDate).Substring(0, 2);

                #region Price Only and Room Only RQ
                string Request = "";
                StringBuilder RootObject = new StringBuilder();
                RootObject.Append("{");
                RootObject.Append("\"checkin\": \"" + checkin2 + "\",");
                RootObject.Append("\"checkout\": \"" + checkout2 + "\",");
                RootObject.Append("\"residency\": \"" + Nationality + "\",");
                RootObject.Append("\"language\": \"" + language + "\",");
                RootObject.Append("\"guests\": [");
                for (int i = 0; i < RoomNo; i++)
                {
                    int ChildAge1 = SearchParam.Rooms[i].Child1Age;
                    int ChildAge2 = SearchParam.Rooms[i].Child2Age;
                    Adult = SearchParam.Rooms[i].NoofAdults;
                    Child = SearchParam.Rooms[i].NoofChild;

                    RootObject.Append("{");
                    RootObject.Append("\"adults\": " + Adult + ",");
                    if (Child == 1)
                        RootObject.Append("\"children\": [" + ChildAge1 + "]}");
                    else if (Child == 2)
                        RootObject.Append("\"children\": [" + ChildAge1 + "," + ChildAge2 + "]}");
                    else
                        RootObject.Append("\"children\": [] }");
                    if (RoomNo - 1 != i)
                        RootObject.Append(",");
                }
                RootObject.Append("],");
                RootObject.Append("\"id\":\"" + ETGHotelIds + "\",");
                RootObject.Append("\"currency\": \"" + Currency + "\"");
                RootObject.Append("}");
                Request = RootObject.ToString();
                return Request;
                #endregion
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
            }
            return string.Empty;
        }
        public string ResponseETGSupplier(ref string Error, string RoomRequest, string strHost, string UserName, string Password, string Provider)
        {
            string strOutput = "";
            string Key = "";
            HttpWebRequest Request;
            try
            {
                Key = Convert.ToBase64String(Encoding.ASCII.GetBytes(UserName + ":" + Password));
                //Request = (HttpWebRequest)HttpWebRequest.Create(strHost);

                Request = (HttpWebRequest)HttpWebRequest.Create(strHost);
                Request.Method = "POST";
                Request.ContentType = "application/json; charset=utf-8";
                Request.Headers.Add("Authorization", "Basic " + Key);
                Request.Headers.Add("Accept-Encoding", "gzip");
                Request.KeepAlive = true;
                //ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls
                //               | SecurityProtocolType.Tls11
                //               | SecurityProtocolType.Tls12
                //               | SecurityProtocolType.Ssl3;
                //ServicePointManager.Expect100Continue = false;


                Stream newStream = null;
                byte[] data = Encoding.ASCII.GetBytes(RoomRequest);
                Request.ContentLength = data.Length;

                newStream = Request.GetRequestStream();
                newStream.Write(data, 0, data.Length);



                HttpWebResponse Response = (HttpWebResponse)Request.GetResponse();
                Stream responseStream = Response.GetResponseStream();
                responseStream = new System.IO.Compression.GZipStream(responseStream, System.IO.Compression.CompressionMode.Decompress);
                StreamReader Reader = new StreamReader(responseStream, Encoding.Default);

                
                strOutput = Reader.ReadToEnd();
                responseStream.Close();
                Reader.Close();
                Response.Close();
            }
            catch (Exception ex)
            {
                strOutput = "";
                Error = ex.Message;
                _commonMethod.XMLRequestResponseTrack(RoomRequest, ex.Message, Provider == "" ? "ratehawk" : Provider, "Proper");
                //Common.ErrorBooking(ex, "", "", "");
            }
            return strOutput;
        }
    }
}



