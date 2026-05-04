using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using MongoDB.Bson.Serialization.IdGenerators;
using XConnect.CommonUtilities;
using XConnect.CommonUtilities.Interface;
using XConnect.ErrorLogHandler.Interface;
using XConnect.Markup.Interface;
using XConnect.Models;
using XConnect.Models.Hotel;
using XConnect.Supplier.GRNC.Interface;
using static Azure.Core.HttpHeader;
using static XConnect.Models.ErrorLog;
using Options = XConnect.Models.Options;

namespace XConnect.Supplier.GRNC
{
    public class GrncRepo : IGrnc
    {
        private readonly ICommonMethod _commonMethod;
        private readonly IException _exception;
        private readonly IMarkup _markup;
        private readonly ISupplierRes _supplierRes;
        private readonly IMemoryCache _cache;
        private readonly KeySettings _keySettings;
        private readonly SupplierConfiguration _supplierConfig;


        public GrncRepo(ICommonMethod commonMethod, IMarkup markup, ISupplierRes supplierRes, IMemoryCache cache, IOptions<KeySettings> keySettings, IException exception, IOptions<SupplierConfiguration> supplierConfig)
        {
            _commonMethod = commonMethod;
            _markup = markup;
            _supplierRes = supplierRes;
            _cache = cache;
            _keySettings = keySettings.Value;
            _exception = exception;
            _supplierConfig = supplierConfig.Value;
        }

        public void GRNCRequestOnly_async(DataRow[] commonCode, List<StarRating> listRating, HotelSearchParameter SearchPara, List<SupplierDetails> lstGRNCDetails, List<SupplierSearchConfig> lstGRNCSearchConfig, DataTable dtPreferredDataHotel, ref ConcurrentBag<HotelX> AllTask, int Hit, DataTable dtROE, TokenData tokenData, DataSet dtXMLCity)
        {
            try
            {
                List<StarRating> lstRating = new List<StarRating>();
                lstRating = _commonMethod.GetStarRatingStringList_xtg(SearchPara);

                List<HotelCodeByCity> DtHotelDAta = HtmlExtensions.ToList<HotelCodeByCity>(commonCode.CopyToDataTable());
                int ratingcount = lstRating.Count;
                int hotelcount = DtHotelDAta.Count;

                foreach (SupplierDetails GRNCDetails in lstGRNCDetails)
                {
                    try
                    {
                        int SupplierID = GRNCDetails.SupplierID;
                        string Supplier = GRNCDetails.SupplierName;

                        if (lstGRNCSearchConfig.Count > 0)
                        {
                            List<SupplierSearchConfig> GRNCSearchConfig = lstGRNCSearchConfig
                                  .Where(x => x != null && x.SupplierID == SupplierID)
                                  .ToList();

                            if (GRNCSearchConfig != null && GRNCSearchConfig.Count > 0)
                            {
                                List<HotelCodeByCity> filter = new List<HotelCodeByCity>();
                                filter = _commonMethod.FilterWithOutCity_Code(ratingcount, hotelcount, DtHotelDAta, lstRating, Supplier);

                                if (filter.Count > 0)
                                {

                                    DataForSearch dataForSearch = _commonMethod.GetXmlDataForSearchTechwise(SearchPara.Nationality, dtXMLCity, Supplier, GRNCDetails.TechName);

                                    if (dataForSearch.XmlNationality != "" && dataForSearch.XmlNationality != "IN")
                                    {
                                        string Ids = string.Empty;
                                        string xmlbase = "";
                                        _commonMethod.GetPrefferedHotelCodes(ref Ids, ref xmlbase, ref GRNCSearchConfig, Supplier, dtPreferredDataHotel, SearchPara.IsSearchFor);
                                        if (GRNCSearchConfig != null)
                                        {
                                            double CurrencyFactorGRNChotelsfrance = 1;
                                            string GRNCCurrency = Convert.ToString(GRNCDetails.CurrencyShortName);

                                            if (Convert.ToString(GRNCCurrency).ToLower() != tokenData.SiteCurrency.ToLower())//need to update dynamic
                                            {
                                                CurrencyFactorGRNChotelsfrance = _commonMethod.GetROEByCurrency(Convert.ToString(GRNCCurrency), dtROE);
                                            }

                                            for (int k = 0; k < GRNCSearchConfig.Count; k++)
                                            {
                                                bool DebugMode = (Convert.ToInt32(GRNCSearchConfig[k].DebugMode) == 0 ? false : true);
                                                string UserName = Convert.ToString(GRNCDetails.UserName);
                                                string Password = Convert.ToString(GRNCDetails.Password);
                                                {

                                                    int IsCitywisesearch = Convert.ToInt32(GRNCSearchConfig[k].City);
                                                    if (IsCitywisesearch == 0)//(DtHotelData.DefaultView.Count > 0)
                                                    {
                                                        if (Convert.ToInt32(GRNCSearchConfig[k].CityID) == 0 || Convert.ToInt32(GRNCSearchConfig[k].CityID) == SearchPara.CityID)
                                                        {
                                                            if (commonCode.Length > 0)
                                                            {
                                                                int Noofrecords = Convert.ToInt32(GRNCSearchConfig[k].Noofrecords);
                                                                double count = Convert.ToDouble(filter.Count);
                                                                int NoOfDiv = Convert.ToInt32(Math.Ceiling(count / Convert.ToDouble(Noofrecords)));
                                                                List<List<XMLID_Search>> lstxml = _commonMethod.GetXMLIDString(NoOfDiv, Noofrecords, count, filter);
                                                                for (int a = 0; a < NoOfDiv; a++)
                                                                {
                                                                     string IdsForSearch = Convert.ToString(_commonMethod.GetHotelCodeByCOmma(lstxml[a]));
                                                                    HotelX Xhotel = new HotelX();
                                                                    Xhotel.Supplier = Supplier;
                                                                    Xhotel.DebugeMode = DebugMode;
                                                                    Xhotel.SearchTimeOut = Convert.ToInt32(GRNCSearchConfig[k].HotelSearchTimeOut);
                                                                    //if (Hit > 0)
                                                                    Xhotel.ProcessTimeout = Convert.ToInt32(GRNCSearchConfig[k].HotelSearchTimeOut2);

                                                                    
                                                                    if (_keySettings.SupplierProxy == "0")//Added on 9-APR-2025
                                                                    {
                                                                        //Xhotel.URL = Convert.ToString(GRNCDetails.WebSiteURL) + GrncStaticInfo.GRNC_HotelSearch;//Added on 13-MAR-2025;
                                                                        Xhotel.URL = Convert.ToString(GRNCDetails.WebSiteURL) + _supplierConfig.GRNC.HotelSearch;
                                                                    }
                                                                    else
                                                                    {
                                                                        Xhotel.URL = Convert.ToString(GRNCDetails.WebSiteURLforAPI) + _supplierConfig.GRNC.HotelSearch; 
                                                                    }
                                                                    Xhotel.UserId = Convert.ToString(GRNCDetails.UserID);
                                                                    Xhotel.UserName = UserName;
                                                                    Xhotel.PassWord = Password;
                                                                    Xhotel.CurrencyCode = GRNCCurrency;
                                                                    Xhotel.CurrencyFactor = CurrencyFactorGRNChotelsfrance;
                                                                    Xhotel.XmlCity = dataForSearch.xmlCity;
                                                                    // Xhotel.HotelID = HotelId;
                                                                    Xhotel.XmlCountry = "";
                                                                    Xhotel.XmlNationality = dataForSearch.XmlNationality;
                                                                    string PaxPassport = dataForSearch.XmlNationality;

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
                                                                        objrq.Request = XMLGRNCRequestOnly_async(Xhotel.URL, UserName, Xhotel.PassWord, Xhotel.SearchTimeOut, SearchPara, IdsForSearch, DebugMode, dataForSearch.XmlNationality, dataForSearch.xmlCity, dataForSearch.xmlCountry, SearchPara.UserName, Xhotel.CurrencyCode, Hit, Xhotel.hotelRating);
                                                                        objrq.Response = ""; RQRS.Add(objrq);
                                                                    }
                                                                    Xhotel.RQRS = RQRS;
                                                                    #endregion
                                                                    Xhotel.TechName = GRNCDetails.TechName;
                                                                    Xhotel.ReturnRooms = GRNCDetails.ReturnRooms;
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
                        _exception.Error(ex);
                    }
                }
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
            }
        }
        public string XMLGRNCRequestOnly_async(string WSurl, string Username, string Password, Int32 SearchTimeOut, HotelSearchParameter SearchParam, string HotelIds, bool DebugMode, string GBNationality, string GBCityId, string CountryId, string UserId, string CurrencyCode, int IsHit, int hotelRating)
        {
            try
            {
                string checkin = Convert.ToString(SearchParam.CheckInDate);
                string checkout = Convert.ToString(SearchParam.CheckOutDate);
                int NoOfNight = Convert.ToInt32(SearchParam.NoofNights);
                checkin = checkin.Substring(6, 4) + "-" + checkin.Substring(3, 2) + "-" + checkin.Substring(0, 2);
                checkout = checkout.Substring(6, 4) + "-" + checkout.Substring(3, 2) + "-" + checkout.Substring(0, 2);
                string HotelList = GRNCSearchRequest(SearchParam, checkin, checkout, WSurl, HotelIds, GBNationality, GBCityId, CountryId, Username, Password, UserId, CurrencyCode, IsHit, hotelRating, SearchTimeOut);
                return HotelList;
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
            }
            return "";
        }
        public string GRNCSearchRequest(HotelSearchParameter SearchParam, string checkin, string checkout, string WSurl, string HotelIds, string GRNNationality, string GRNCityId, string GRNCountryId, string Username, string Password, string UserId, string CurrencyCode, int IsHit, int hotelRating, int Timeout)
        {
            try
            {
                string XMLCountryName = SearchParam.CountryCode;
                string XMLCityName = SearchParam.CityName;
                int XMLCITY = SearchParam.CityID;
                string bed_type;
                int RoomNo = SearchParam.Rooms.Count;
                int id;
                var rates = "concise";
                var child = "3\"" + "," + "\"11";
                StringBuilder roomstring = new StringBuilder();
                {
                    roomstring.Append("{");
                    string[] StrHid = HotelIds.Split(',');
                    string hid = "";
                    for (int i = 0; i < StrHid.Count(); i++)
                    {
                        if (i == 0)
                        {
                            hid = "\"" + StrHid[i] + "\"";
                        }
                        else
                        {
                            hid = hid + ",\"" + StrHid[i] + "\"";
                        }
                    }
                    roomstring.Append("\"hotel_codes\": [" + hid + "],");
                    roomstring.Append("\"checkin\":\"" + checkin + "\",");
                    roomstring.Append("\"checkout\":\"" + checkout + "\",");
                    roomstring.Append("\"client_nationality\":\"" + GRNNationality + "\",");
                    roomstring.Append("\"cutoff_time\": " + Timeout + "  ,");
                    roomstring.Append("\"currency\":\"" + CurrencyCode + "\",");
                    roomstring.Append("\"rates\":\"" + rates + "\",");
                    roomstring.Append("\"hotel_category\": [3,7] ,");
                    roomstring.Append("\"rooms\": [");
                    for (int i = 0; i < RoomNo; i++)
                    {
                        int NoofAdult = SearchParam.Rooms[i].NoofAdults;
                        if (SearchParam.Rooms[i].NoofChild > 0)
                        {
                            if (SearchParam.Rooms[i].Child1Age > 11)
                            {
                                NoofAdult = NoofAdult + 1;
                            }
                            if (SearchParam.Rooms[i].Child2Age > 11)
                            {
                                NoofAdult = NoofAdult + 1;
                            }
                        }
                        roomstring.Append("{");
                        //if (SearchParam.Rooms[i].NoofChild > 0 && SearchParam.Rooms[i].Child1Age < 1&& SearchParam.Rooms[i].Child1Age >1&& SearchParam.Rooms[i].Child2Age < 12&& SearchParam.Rooms[i].Child2Age >1)
                        //{
                        //    roomstring.Append("\"adults\": " + "\"" + NoofAdult + "\"" + ",");
                        //}
                        if (SearchParam.Rooms[i].NoofChild > 0 && (SearchParam.Rooms[i].Child1Age > 1 && SearchParam.Rooms[i].Child1Age <= 11) /*&& (SearchParam.Rooms[i].Child2Age < 2 && SearchParam.Rooms[i].Child2Age < 12)*/)
                        {
                            roomstring.Append("\"adults\": " + "\"" + NoofAdult + "\"" + ",");
                        }
                        else if (SearchParam.Rooms[i].NoofChild > 0 && (SearchParam.Rooms[i].Child2Age > 1 && SearchParam.Rooms[i].Child2Age <= 11) /*&& (SearchParam.Rooms[i].Child1Age < 2 && SearchParam.Rooms[i].Child1Age < 12)*/)
                        {
                            roomstring.Append("\"adults\": " + "\"" + NoofAdult + "\"" + ",");
                        }
                        else
                        {
                            roomstring.Append("\"adults\": " + "\"" + NoofAdult + "\"");
                            //roomstring.Append("\"adults\": " + "\"" + SearchParam.Rooms[i].NoofAdults + "\"");
                        }
                        if (SearchParam.Rooms[i].NoofChild > 0)
                        {
                            int child1;
                            int child2;
                            //if (SearchParam.Rooms[i].NoofChild == 1)
                            if (SearchParam.Rooms[i].Child1Age > 1 && SearchParam.Rooms[i].Child1Age < 12 && SearchParam.Rooms[i].NoofChild == 1)
                            {
                                roomstring.Append("\"children_ages\": [" + "\"" + SearchParam.Rooms[i].Child1Age + "\"]");
                            }
                            else if (SearchParam.Rooms[i].NoofChild == 2 && (SearchParam.Rooms[i].Child1Age > 1 && SearchParam.Rooms[i].Child1Age < 12) && (SearchParam.Rooms[i].Child2Age < 2 || SearchParam.Rooms[i].Child2Age > 11))
                            {
                                roomstring.Append("\"children_ages\": [" + "\"" + SearchParam.Rooms[i].Child1Age + "\"]");
                            }
                            else if (SearchParam.Rooms[i].NoofChild == 2 && (SearchParam.Rooms[i].Child2Age > 1 && SearchParam.Rooms[i].Child2Age < 12) && (SearchParam.Rooms[i].Child1Age < 2 || SearchParam.Rooms[i].Child1Age > 11))
                            {
                                roomstring.Append("\"children_ages\": [" + "\"" + SearchParam.Rooms[i].Child2Age + "\"]");
                            }
                            else if (SearchParam.Rooms[i].NoofChild == 2 && SearchParam.Rooms[i].Child2Age > 1 && SearchParam.Rooms[i].Child2Age < 12 && SearchParam.Rooms[i].Child1Age > 1 && SearchParam.Rooms[i].Child1Age < 12)
                            {
                                roomstring.Append("\"children_ages\": [" + "\"" + SearchParam.Rooms[i].Child1Age + "\"" + "," + "\"" + SearchParam.Rooms[i].Child2Age + "\"]");
                            }
                        }
                        roomstring.Append("}");
                        if (RoomNo > 1)
                        {
                            if (RoomNo != (i + 1))
                                roomstring.Append(",");
                        }
                    }
                    roomstring.Append("],");
                    roomstring.Append("\"version\": \"2.0\"");

                    roomstring.Append("}");
                    string request = roomstring.ToString();
                    return request;
                }
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
                return "";
            }
        }

        public async Task<List<Options>> GRNC_HotelSearchWithRooms(HotelX Item, CommonSearch commonSearch, string Currency, string ResponseData, string CityCode, HotelSearchParameter SearchParam, double CurrencyFactorTBO, string XMLnationality, string URLandRequest, string UserName, string PassWord, string URL, Int32 SearchTimeOut, Int32 processTimeout, string ResProccessTimerLogActive, string ResProccessTimeValueforSaveLog)
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
                    string Provider = "grnc";
                    string ConvertXml = Newtonsoft.Json.JsonConvert.DeserializeXmlNode("{\"Row\":" + ResponseData + "}", "root").InnerXml;
                    DataSet GRNCResponsedata = _commonMethod.GetDataSetFromXMLString(ConvertXml);


                    //  GRNC.GRNCRS GRNCResponse = JsonConvert.DeserializeObject<GRNC.GRNCRS>(ResponseData);
                    //  DataSet GRNCResponse = Common.GetDataSetFromXMLString(ResponseData);
                    if (GRNCResponsedata == null)
                    {
                        Item.ResponseStatus = 204;
                        Item.ResponseType = "NoCount";
                        return options;
                    }
                    #endregion
                    string SessionId = Convert.ToString(GRNCResponsedata.Tables["Row"].Rows[0]["search_id"]);
                    if (SessionId != "" || SessionId != null || SessionId != string.Empty)
                    {
                        Item.ResponseStatus = 204;
                        Item.ResponseType = "NoCount";
                        //Common.XMLRequestResponseTrack_SupplierHotel(URLandRequest, ResponseData, "TBO", "HotelSearch", SessionId);
                    }
                    double TotalCharges = 0;
                    if (!GRNCResponsedata.Tables.Contains("hotels"))
                    {
                        Item.ResponseStatus = 404;
                        Item.ResponseType = "NoCount";
                        return options;
                    }
                    for (int h = 0; h <= GRNCResponsedata.Tables["hotels"].Rows.Count - 1; h++)
                    {
                        Item.ResponseType = "Success";
                        string ItemCode = Convert.ToString(GRNCResponsedata.Tables["hotels"].Rows[h]["hotel_code"]);
                        string HotelName = Convert.ToString(GRNCResponsedata.Tables["hotels"].Rows[h]["name"]);

                        #region "When not Need Room"  
                        // add hotel code from price only and room code blank here
                        if (SearchParam.Filters.IsShowRooms == 0)
                        {
                            #region "Hotel Code Block "

                            {
                                DataRow drXmlHotel = DtHotel.NewRow();
                                //string InternalHotelId = Convert.ToString(ItemCode);
                                drXmlHotel["XMLHotelCode"] = ItemCode;
                                drXmlHotel["HotelName"] = HotelName;
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
                                drXmlHotel["AvailToken"] = SessionId;

                                //   DataRow[] drhotel = GRNCResponsedata.Tables["hotels"].Select("HotelCode='" + ItemCode + "'");
                                double Factor = 1;
                                if ((GRNCResponsedata.Tables["min_rate"].Rows[h]["currency"]).ToString() != commonSearch.tokenData.SiteCurrency.ToLower())
                                {
                                    DataTable dtInrCurrency = _commonMethod.GetCountryCurrency(GRNCResponsedata.Tables["min_rate"].Rows[h]["currency"].ToString());
                                    Factor = Convert.ToDouble(dtInrCurrency.Rows[0]["AmtToExch"]);
                                }
                                double FinalTotal = Convert.ToDouble(GRNCResponsedata.Tables["min_rate"].Rows[h]["price"]) * Factor;


                                drXmlHotel["FinalSellingPrice"] = FinalTotal;
                                drXmlHotel["SellingPrice"] = FinalTotal;
                                DtHotel.Rows.Add(drXmlHotel);
                                TotalCharges = FinalTotal;

                                #endregion
                                if (TotalCharges > 0)
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
                        }
                        #endregion
                        #region "When Need Rooms"
                        //add Hotel code blank and rooms code in room block
                        else
                        {
                            double FinalMinPrice = 0;
                            #region Hotels
                            #region "Hotel Block here"
                            DataRow drXmlHotel = DtHotel.NewRow();
                            //string InternalHotelId = Convert.ToString(ItemCode);
                            drXmlHotel["XMLHotelCode"] = ItemCode;
                            drXmlHotel["HotelName"] = HotelName;
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
                            drXmlHotel["AvailToken"] = SessionId;

                            //   DataRow[] drhotel = GRNCResponsedata.Tables["hotels"].Select("HotelCode='" + ItemCode + "'");
                            double Factor = 1;
                           
                            if ((GRNCResponsedata.Tables["min_rate"].Rows[h]["currency"]).ToString().ToLower() != commonSearch.tokenData.SiteCurrency.ToLower())
                            {
                                //DataTable dtInrCurrency = _commonMethod.GetCountryCurrency(GRNCResponsedata.Tables["min_rate"].Rows[h]["currency"].ToString());
                                //Factor = Convert.ToDouble(dtInrCurrency.Rows[0]["AmtToExch"]);

                                Factor = _commonMethod.GetROEByCurrency(Convert.ToString(GRNCResponsedata.Tables["min_rate"].Rows[h]["currency"].ToString()), commonSearch.tokenData.dtROE);


                            }
                            double FinalTotal = Convert.ToDouble(GRNCResponsedata.Tables["min_rate"].Rows[h]["price"]) * Factor;


                            drXmlHotel["FinalSellingPrice"] = FinalTotal;
                            drXmlHotel["SellingPrice"] = FinalTotal;
                            DtHotel.Rows.Add(drXmlHotel);
                            TotalCharges = FinalTotal;
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
                                DataTable dtHotelRoom = _commonMethod.GenerateRoomtypeDataTableNewV2();
                                DataTable dtCancellation = _commonMethod.GenerateCancellationDataTableNewV2();
                                DataTable DtRoomRateBreackUp = _commonMethod.GenerateoomRateBreackUpDataTableNewV2();
                                int CutOffDays = XMLHotelBookingCommon.CutOffDays;
                                CutOffDays = _commonMethod.GetCutOffDays(commonSearch.dtSuplierMarkup, Provider);
                                #region "Room Logic"

                                //DataTable DtGRNCHotelRoom = Common.GenerateRoomtypeDataTableNewV2();

                                //DataTable dtGRNCCancellation = Common.GenerateCancellationDataTableNewV2();
                                //DataTable DtGRNCHotelRoomRateBreackUp = Common.GenerateoomRateBreackUpDataTableNewV2();
                                if (SearchParam.Filters.IsShowRooms == 1)
                                {

                                    string Type = null;
                                    double SubAgentMarkup = 0;
                                    string room_type = "";
                                    string group_code = "";
                                    string room_code = "";
                                    string rate_key = "";
                                    string rate_type = "";
                                    string boarding_details = "";
                                    string group_code1 = "";
                                    string rate_key1 = "";
                                    string room_code1 = "";
                                    int price1;
                                    double price;
                                    string boarding_details1 = "";
                                    string rate_type1 = "";
                                    int no_of_room;
                                    string room_type1 = "";
                                    string covid_19_safety_protocol = "";
                                    string EssentialInfo = "";
                                    string other_inclusions = "";
                                    //string RoomN = "";
                                    //string room_type2 = "";
                                    int IsParentId = 1, agentid = 0;

                                    //      string citycode = GRNC_CityCode;
                                    double AgentPercentage = 0.0;
                                    // Boolean blnIsShowSupplier = false;
                                    double AgentMarkupAmount = 0.0;
                                    Boolean isPerce = true;

                                    double AgentCompanyPercentage = Convert.ToDouble(SearchParam.AgentCompanyPercentage);
                                    bool IsB2C = Convert.ToBoolean(Convert.ToByte(SearchParam.IsB2BOrB2C));
                                    bool blnIsShowSupplier = false;
                                    //_markup.getAgetMarkupSetting(Provider, GiataId.ToString(), commonSearch.dtSuplierMarkup, commonSearch.dtMarkupDetails, ref AgentPercentage, ref AgentMarkupAmount, ref blnIsShowSupplier, ref isPerce, commonSearch.AgentId, RatingHotel, SearchParam.CityID, SearchParam.CountryID.ToString(), SearchParam.CheckInDate);

                                    _markup.getAgetMarkupSettingNew_new(Provider, Convert.ToString(RaynaID), commonSearch.dtSuplierMarkup, commonSearch.dsMarkupDetails, ref AgentPercentage, ref AgentMarkupAmount, ref blnIsShowSupplier, ref isPerce, commonSearch.AgentId, RatingHotel, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), SearchParam.CheckInDate);

                                    double B2CMarkup = (IsB2C == false ? AgentCompanyPercentage : XMLHotelBookingCommon.B2CMarkup);
                                    #region Supplier Room Code block
                                    //int NoOfNight = SearchParam.NoofNights;
                                    DataRow[] drHotelDetails = DtHotel.Select("OwnsystemHotelId='" + OwnsystemHotelId + "'");
                                    string[] HotelDetails = Convert.ToString(drHotelDetails[0]["AvailToken"]).Split(':');
                                    SessionId = HotelDetails[0];
                                    //string ResultIndex = HotelDetails[1];
                                    //  string HotelCode = HotelDetails[2];
                                    // XMLnationality = HotelDetails[3];
                                    //   tickNow = HotelDetails[4];
                                    // GRNCResponsedata = null;
                                    if (SessionId != "")
                                    {
                                        string GRNCRoomReq = Convert.ToString(URL + "/" + SessionId + "?hcode=" + ItemCode + "&bundled=true");
                                        string Response1 = GRNCResponseroom(GRNCRoomReq, URL, UserName, PassWord, 2000, "");
                                        //string Response1 = GRNCResponseroom(GRNCRoomReq, URL, UserName, PassWord, 2000, "af3675c6f7819722b572fcd4bcd6598d");
                                        RootObjectgrnc GRNCResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<RootObjectgrnc>(Response1);
                                        //Common.XMLRequestResponseTrack_SupplierRooms(URLandRequest, Response, "TBO", "RoomSearch", SessionId);

                                        int no_of_infants = 0;
                                        string hcode = GRNCResponse.hotel.hotel_code;
                                        int noofadult = GRNCResponse.no_of_adults;
                                        int noofchild = GRNCResponse.no_of_children;
                                        string desc = GRNCResponse.hotel.description;
                                        string CityId = GRNCResponse.hotel.city_code;
                                        #region NoOfRooms
                                        // string EssentialInfo = "";
                                        if (GRNCResponse.hotel != null)
                                        {

                                            for (int al = 0; al < GRNCResponse.hotel.rates.Count; al++)

                                            {

                                                group_code = "";
                                                room_code = "";
                                                rate_key = "";
                                                boarding_details = "";
                                                rate_type = "";
                                                price = 0;
                                                room_type = "";

                                                if (Convert.ToString(GRNCResponse.hotel.rates[al].other_inclusions) != "")
                                                {
                                                    for (int z = 0; z < GRNCResponse.hotel.rates[al].other_inclusions.Count; z++)
                                                    {
                                                        if (z == 0)
                                                        {
                                                            other_inclusions = "<b>other_inclusions:<b>" + Convert.ToString(GRNCResponse.hotel.rates[al].other_inclusions[z]) + ",";

                                                        }
                                                        else
                                                        {
                                                            other_inclusions = other_inclusions + Convert.ToString(GRNCResponse.hotel.rates[al].other_inclusions[z]) + ",";

                                                        }

                                                    }
                                                }
                                                if (GRNCResponse.hotel.safe2stay != null)
                                                {
                                                    covid_19_safety_protocol = "<b>covid_19_safety_protocol:<b>" + GRNCResponse.hotel.safe2stay.covid_19_safety_protocol;
                                                }
                                                int rooms = GRNCResponse.hotel.rates[al].no_of_rooms;
                                                bool supports_cancellation = Convert.ToBoolean(GRNCResponse.hotel.rates[al].supports_cancellation);
                                                bool supports_amendment = Convert.ToBoolean(GRNCResponse.hotel.rates[al].supports_amendment);
                                                string cancellation_policy_code = Convert.ToString(GRNCResponse.hotel.rates[al].cancellation_policy);
                                                string cancellation_policy = Convert.ToString(GRNCResponse.hotel.rates[al].cancellation_policy_code);
                                                //EssentialInfo = Convert.ToString(GRNCResponse.hotel.rates[al].rate_comments.pax_comments);
                                                EssentialInfo = covid_19_safety_protocol + Convert.ToString(GRNCResponse.hotel.rates[al].rate_comments.pax_comments);


                                                group_code = Convert.ToString(GRNCResponse.hotel.rates[al].group_code);

                                                room_code = Convert.ToString(GRNCResponse.hotel.rates[al].room_code);

                                                price = Convert.ToInt32(GRNCResponse.hotel.rates[al].price);
                                                // price = price + price1;

                                                int noofrom = Convert.ToInt32(GRNCResponse.hotel.rates[al].no_of_rooms);
                                                string essan = Convert.ToString(GRNCResponse.hotel.rates[al].rate_comments.pax_comments);


                                                rate_key = Convert.ToString(GRNCResponse.hotel.rates[al].rate_key);

                                                rate_type = Convert.ToString(GRNCResponse.hotel.rates[al].rate_type);

                                                boarding_details = Convert.ToString(GRNCResponse.hotel.rates[al].boarding_details[0]);


                                                string Roomcode = room_code + "%%%%" + rate_key;

                                                double firstNightCharge = Convert.ToDouble(GRNCResponse.hotel.rates[al].price) / SearchParam.NoofNights;
                                                DateTime serviceDate = Convert.ToDateTime(GRNCResponse.checkin);
                                                string RoomN = "";
                                                string room_reference = "";
                                                string description = "";

                                                int b = 0;
                                                for (int l = 0; l < SearchParam.Rooms.Count(); l++)
                                                {

                                                    bool flag = false;
                                                    for (int roo = 0; roo < GRNCResponse.hotel.rates[al].rooms.Count; roo++)
                                                    {
                                                        int NoOfChild1 = 0;
                                                        int NoOfInfant1 = 0;
                                                        int Child1Age = 0;
                                                        int Child2Age = 0;
                                                        Child1Age = Convert.ToInt32(SearchParam.Rooms[l].Child1Age);
                                                        Child2Age = Convert.ToInt32(SearchParam.Rooms[l].Child2Age);
                                                        int NoOfChild = Convert.ToInt32(SearchParam.Rooms[l].NoofChild);
                                                        // int NoOfAdults = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[p]["NoOfAdults"]);
                                                        // int childage = Convert.ToInt32(dtPassengers.DefaultView[p]["ChildAge"]);
                                                        int ChildAge1 = 0;
                                                        int ChildAge2 = 0;
                                                        int NoOfInfant = 0;
                                                        int ChildAges = 0;
                                                        if (NoOfChild == 1)
                                                        {
                                                            ChildAge1 = Convert.ToInt32(SearchParam.Rooms[l].Child1Age);
                                                            if (ChildAge1 < 2)
                                                            {
                                                                // NoOfChild = NoOfChild - 1;
                                                                NoOfInfant1++;// = NoOfInfant + 1;
                                                            }
                                                            else
                                                            {
                                                                NoOfChild1++;// += NoOfChild - 1;
                                                                             //ChildAges = ChildAges + 1;
                                                            }
                                                        }
                                                        else if (NoOfChild == 2)
                                                        {
                                                            ChildAge1 = Convert.ToInt32(SearchParam.Rooms[l].Child1Age);
                                                            if (ChildAge1 < 2)
                                                            {
                                                                //NoOfChild = NoOfChild - 1;
                                                                //NoOfInfant = NoOfInfant + 1;
                                                                NoOfInfant1++;
                                                            }
                                                            else
                                                            {
                                                                NoOfChild1++;
                                                            }
                                                            ChildAge2 = Convert.ToInt32(SearchParam.Rooms[l].Child2Age);
                                                            if (ChildAge2 < 2)
                                                            {
                                                                NoOfInfant1++;
                                                            }
                                                            else
                                                            {
                                                                NoOfChild1++;
                                                            }
                                                        }

                                                        if (Convert.ToInt32(GRNCResponse.hotel.rates[al].rooms[roo].no_of_adults) == Convert.ToInt32(SearchParam.Rooms[l].NoofAdults) && Convert.ToInt32(GRNCResponse.hotel.rates[al].rooms[roo].no_of_children) == Convert.ToInt32(NoOfChild1))
                                                        {
                                                            flag = true;



                                                            // for (int l = 0; l < SearchParam.Rooms.Count(); l++)
                                                            {
                                                                // for (int roo = 0; roo < GRNCResponse.hotel.rates[al].rooms.Count; roo++)
                                                                {
                                                                    no_of_room = Convert.ToInt32(GRNCResponse.hotel.rates[al].rooms[roo].no_of_rooms);
                                                                    int no_of_adult = Convert.ToInt32(GRNCResponse.hotel.rates[al].rooms[roo].no_of_adults);
                                                                    int no_of_children = Convert.ToInt32(GRNCResponse.hotel.rates[al].rooms[roo].no_of_children);
                                                                    description = Convert.ToString(GRNCResponse.hotel.rates[al].rooms[roo].description);
                                                                    //if (SearchParam.Rooms[l].NoofAdults == no_of_adult && SearchParam.Rooms[l].NoofChild == no_of_children)
                                                                    {
                                                                        // if (room_reference.Contains(GRNCResponse.hotel.rates[al].rooms[roo].room_reference) == false)
                                                                        {


                                                                            if (no_of_children != 0)
                                                                            {
                                                                                for (int i = 0; i < Convert.ToString(GRNCResponse.hotel.rates[al].rooms[roo].children_ages).Length; i++)
                                                                                {
                                                                                    List<Roomgrnc> lst1 = new List<Roomgrnc>();
                                                                                    if (GRNCResponse.hotel.rates[al].rooms[roo].children_ages != null)
                                                                                    {
                                                                                        //lst1 = (List<Roomgrnc>)GRNCResponse.hotel.rates[al].rooms[roo].children_ages;
                                                                                        //var jsonSerialisers = new JavaScriptSerializer();
                                                                                        // var jsons = jsonSerialiser.Serialize(lst);

                                                                                        //string BeforeBlockResponse1 = Newtonsoft.Json.JsonConvert.DeserializeXmlNode(json, "Roomgrnc").InnerXml;
                                                                                        //DataTable dtq1 = HtmlExtensions.ConvertToDataTable(lst1);
                                                                                        // if (Convert.ToInt32(NoOfInfant) == 1)
                                                                                        {
                                                                                            no_of_infants = NoOfInfant;
                                                                                        }
                                                                                        //  else
                                                                                        {
                                                                                            no_of_children = NoOfChild;
                                                                                        }
                                                                                    }

                                                                                }
                                                                            }
                                                                            room_type1 = Convert.ToString(GRNCResponse.hotel.rates[al].rooms[roo].room_type);
                                                                            //string RoomName1 = Convert.ToString(GRNCResponse.hotels[h]["RoomTypeId"]) + "(" + Convert.ToString(GRNCResponse.hotels[h].rates[al].boarding_details[0]) + ")";
                                                                            room_type = room_type + "$$$" + room_type1;
                                                                            string[] room_type2 = Convert.ToString(room_type).Split(new String[] { "$$$" }, StringSplitOptions.None);
                                                                            for (int i = 1; i < room_type2.Length; i++)
                                                                            {
                                                                                int c = l + 1;

                                                                                RoomN = RoomN + "[" + c + "]" + room_type2[i];
                                                                                room_type = RoomN;
                                                                            }
                                                                            // int no_of_rooms = (GRNCResponse.hotels[h].rates[al].rooms[0].no_of_rooms);
                                                                            // bool non_Refundable = Convert.ToBoolean(dtq.Rows[roo]["non_refundable"]);
                                                                            string room_reference1 = Convert.ToString(GRNCResponse.hotel.rates[al].rooms[roo].room_reference);
                                                                            room_reference = room_reference + "###" + room_reference1;
                                                                            if (flag)
                                                                            {
                                                                                //dtq.Rows[roo].Delete();
                                                                                break;
                                                                            }
                                                                            break;
                                                                        }
                                                                    }
                                                                }
                                                            }
                                                        }

                                                    }

                                                }
                                                //  string GRNC_NationalityCode = "";
                                                string GRNCCurrency = Item.CurrencyCode;
                                                double CurrencyFactorGRNC = 1;
                                                string CheckInDate = Convert.ToString(SearchParam.CheckInDate);
                                                string CheckOutDate = Convert.ToString(SearchParam.CheckOutDate);
                                                //int NoOfNight = Convert.ToInt32(SearchParam.NoofNights);
                                                DateTime curDate = _commonMethod.GetTimeZoneDate(SearchParam.CurrentDate);
                                                int NoOfRoom = Convert.ToInt32(SearchParam.Rooms.Count);
                                                if (room_type == "")
                                                {
                                                    continue;
                                                }
                                                string RoomTypeId = room_type + "$$$$" + cancellation_policy + "$$$$" + rate_key + "$$$$" + SessionId + "$$$$" + group_code + "$$$$" + rate_type + "$$$$" + hcode + "$$$$" + room_code + "$$$$" + CityId + "$$$$" + no_of_infants + "$$$$" + XMLnationality + "$$$$" + rooms + "$$$$" + NoOfRoom + "$$$$" + room_reference + "$$$$" + "<b>Room Type:<b>" + description + "</br>" + other_inclusions + "</br>" + EssentialInfo;

                                                RoomTypeId = RoomTypeId.Replace("'", "");

                                                #region "Rate Break Up"
                                                //for (int date = 0; date < SearchParam.NoofNights; date++)
                                                //{
                                                //    #region "Rate Break Up"

                                                //    DateTime dtCheckInDateForRate = DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null);
                                                //    string priceType = Convert.ToString(GRNCResponse.hotel.rates[al].price_details);

                                                //    #region Vat
                                                //    Double VatOnRateBrackUp = 0;
                                                //    Double PriceRoom = _markup.ProcessHotelRateBreakupPrice_WithMarkup(1, SearchParam.CityID, SearchParam.CountryID.ToString(), firstNightCharge, B2CMarkup, IsParentId, isPerce, AgentPercentage, AgentMarkupAmount, SubAgentMarkup, ref VatOnRateBrackUp);
                                                //    #endregion
                                                //    //if (date == 0)
                                                //    //    firstNightCharge = PriceRoom;
                                                //    string rateWeekColumn = Convert.ToString(serviceDate.Date.AddDays(date).DayOfWeek).Substring(0, 3);
                                                //    DataRow drHotelGRNCRateBreackUp = DtRoomRateBreackUp.NewRow();
                                                //    drHotelGRNCRateBreackUp["SupplierName"] = "grnc";
                                                //    drHotelGRNCRateBreackUp["OwnsystemHotelId"] = OwnsystemHotelId;
                                                //    drHotelGRNCRateBreackUp["ContractID"] = 0;
                                                //    drHotelGRNCRateBreackUp["RoomTypeName"] = room_type;
                                                //    drHotelGRNCRateBreackUp["RoomTypeId"] = RoomTypeId;
                                                //    drHotelGRNCRateBreackUp["MealId"] = boarding_details;
                                                //    drHotelGRNCRateBreackUp["Meal"] = boarding_details;
                                                //    drHotelGRNCRateBreackUp["DateOfWeek"] = rateWeekColumn;
                                                //    drHotelGRNCRateBreackUp["Date"] = dtCheckInDateForRate.Date.AddDays(date).Date;
                                                //    drHotelGRNCRateBreackUp["Rate"] = Convert.ToString((PriceRoom));
                                                //    drHotelGRNCRateBreackUp["VatdiffAmount"] = Convert.ToString((VatOnRateBrackUp));
                                                //    drHotelGRNCRateBreackUp["RateType"] = room_type;
                                                //    drHotelGRNCRateBreackUp["RoomNo"] = "1";
                                                //    drHotelGRNCRateBreackUp["RoomSequenceNo"] = room_code;
                                                //    drHotelGRNCRateBreackUp["BookingStatus"] = "Available";
                                                //    drHotelGRNCRateBreackUp["SupplierCurrency"] = GRNCCurrency;
                                                //    drHotelGRNCRateBreackUp["BuyingPrice"] = PriceRoom / CurrencyFactorGRNC;
                                                //    DtRoomRateBreackUp.Rows.Add(drHotelGRNCRateBreackUp);
                                                //}
                                                #endregion

                                             
                                                DataRow drXmlHotelRoom = dtHotelRoom.NewRow();
                                                drXmlHotelRoom["OwnSystemHotelId"] = OwnsystemHotelId;
                                                drXmlHotelRoom["RoomTypeName"] = room_type;
                                                drXmlHotelRoom["RoomTypeCode"] = room_code;
                                                drXmlHotelRoom["MealId"] = boarding_details;
                                                drXmlHotelRoom["MealName"] = boarding_details;
                                                drXmlHotelRoom["ContractID"] = 0;
                                                drXmlHotelRoom["BoardTypeID"] = boarding_details;
                                                drXmlHotelRoom["RoomTypeId"] = RoomTypeId;
                                                drXmlHotelRoom["AdultsOccupancy"] = noofadult/*(GRNCResponse.hotels[h].rates[al].rooms[roo].no_of_adults)*/;
                                                drXmlHotelRoom["ChildOccupancy"] = noofchild/*(GRNCResponse.hotels[h].rates[al].rooms[roo].no_of_children)*/;
                                                drXmlHotelRoom["MaxExtraBed"] = 0;
                                                drXmlHotelRoom["RoomNo"] = "1";
                                                drXmlHotelRoom["RoomSequenceNo"] = room_code;
                                                drXmlHotelRoom["Type"] = "";
                                                drXmlHotelRoom["BookingStatus"] = "Available";
                                                drXmlHotelRoom["IsCombineRoom"] = true;
                                                drXmlHotelRoom["UniqueNo"] = Guid.NewGuid().ToString();
                                                drXmlHotelRoom["OfferDescription"] = "";
                                                drXmlHotelRoom["OfferCode"] = "";
                                                //if (essential_information != null)
                                                //{
                                                //    drXmlHotelRoom["EssentialInformation"] = essential_information;
                                                //}
                                                //else
                                                //{
                                                drXmlHotelRoom["EssentialInformation"] = "";
                                                //}


                                                double ItemPrice = Convert.ToDouble(price);
                                                //if (cancellation_policy_code != "")
                                                //{
                                                //    Common.ProcessHotelRoom_WithMarkup(CurrencyFactorGRNC, GRNCCurrency, false, true, SearchParam.NoofNights, SearchParam.CityID, SearchParam.CountryId, SearchParam.CheckInDate, SearchParam.CheckOutDate, ref ItemPrice, ref drXmlHotelRoom, isPerce, AgentPercentage, AgentMarkupAmount, SubAgentMarkup, B2CMarkup, IsParentId);
                                                //}
                                                //else
                                                {
                                                    _markup.ProcessHotelRoom_WithMarkup(CurrencyFactorGRNC, GRNCCurrency, true, true, SearchParam.NoofNights, SearchParam.CityID, SearchParam.CountryID.ToString(), SearchParam.CheckInDate, SearchParam.CheckOutDate, ref ItemPrice, ref drXmlHotelRoom, isPerce, AgentPercentage, AgentMarkupAmount, SubAgentMarkup, B2CMarkup, IsParentId);
                                                }
                                                drXmlHotelRoom["SupplierName"] = Provider;
                                                dtHotelRoom.Rows.Add(drXmlHotelRoom);
                                                DtRoomRateBreackUp.DefaultView.RowFilter = "OwnsystemHotelId='" + OwnsystemHotelId + "'";
                                                DtRoomRateBreackUp.DefaultView.Sort = "Rate ASC";



                                                #endregion

                                            }
                                        }
                                    }
                                    else
                                    {
                                        return options;
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
                else
                {
                    Item.ResponseStatus = 204;
                }

                return options;
            }
            catch (Exception ex)
            {
                Item.ResponseStatus = 404;
                Item.ResponseType = "NoCount";
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
                        
                        SearchParam.SupplierTime += ",|Finish:GRNC Process Response  " + "|Time :" + timerroomx.Elapsed.ToString();
                        _ = Task.Run(() => _commonMethod.FireAway_SQL_ResPorcessTimeLog("", timerroomx.Elapsed.ToString(), "GRNC", SearchParam.SearchKey, 1, SearchParam.AgentID));
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

        public string GRNCResponseroom(string Xmlrequest, string Url, string username, string password, int Timeout, string UserId)
        {
            string responseString = "";
            string strOutput = "";
            try
            {
                string Key = Convert.ToBase64String(Encoding.ASCII.GetBytes(username + ":" + password));
                //  string reqUrl = Url + Xmlrequest;
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(Xmlrequest);

                request.Method = "GET";
                //Request.Headers.Add("Api-Key", "8d09f4f93c7d96122f41db96810f51db");//test
                //request.Headers.Add("Api-Key", "af3675c6f7819722b572fcd4bcd6598d");//Live
                request.Headers.Add("Api-Key", password);//Live
                request.ContentType = "application/json";
                request.Accept = "application/json";
                //request.Headers.Add("Accept-Encoding", "application/gzip");
                //using (var stream = request.GetRequestStream())
                //{
                //   // stream.Write(data, 0, data.Length);
                //}
                //HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                //responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();

                try
                {
                    Task<string> Response;

                    HttpWebResponse response1 = (HttpWebResponse)request.GetResponse();
                    Stream responseStream = response1.GetResponseStream();
                    //StreamReader Reader = new StreamReader(responseStream, Encoding.Default);
                    // responseStream = new GZipStream(responseStream, CompressionMode.Decompress);

                    //    if (response1.ContentEncoding.ToLower().Contains("gzip"))
                    //      responseStream = new System.IO.Compression.GZipStream(responseStream, System.IO.Compression.CompressionMode.Decompress);
                    //  else if (response1.ContentEncoding.ToLower().Contains("deflate"))
                    //      responseStream = new System.IO.Compression.DeflateStream(responseStream, System.IO.Compression.CompressionMode.Decompress);
                    StreamReader streamReader = new StreamReader(responseStream);


                    Response = streamReader.ReadToEndAsync();
                    //responsedata = await Response.ConfigureAwait(false);

                    responseString = Response.Result.ToString();
                    responseStream.Close();
                    streamReader.Close();
                    response1.Close();
                }
                catch (WebException webExcp)
                {

                    HttpWebResponse httpResponse = (HttpWebResponse)webExcp.Response;
                    string result = httpResponse.StatusDescription.ToString();
                    string result1 = new StreamReader(webExcp.Response.GetResponseStream()).ReadToEnd();
                    //Error = result1;
                    return "";
                }

            }
            catch (Exception ex)
            {
                _exception.Error(ex);

            }
            return responseString;
        }

        public async Task<CancellationDto> GRNCCancelltionPolicy(DataSet dsHotel, string ownsystemid, string RoomTypeId_Own, DataTable dtAgentDetail, DataTable dtSuplierMarkup, DataTable dtMarkupDetails, DataTable dtSearchConfig, HotelSearchParameter objSearPera, string roomno, string HotelId, string Provider, string roomtypecode, string type, string GiataId, int AgentId, int hotelRating/*, string CurrencyCode, string amount_type*/, string checkin, string checkout, double roomprice, string MealId, string SearchKey, string CompanyBuying = "")
        {
            CancellationDto cancellationDto = new  CancellationDto();
            string IsBooking = "Available";

            string responsetype = "Success";
            string time = "";
            string SupplierName = "grnc";
            string Error = "";
            string xslRequest = "";
            string ResponseDataLog = "";
            string ResponseData = "";
            try
            {
               
                string SessionID = "";
                //dsHotel = null;
                DataRow[] drSuplierMarkup = (dtSuplierMarkup.Select("SupplierName='" + Provider + "'"));
                //  DataRow[] drHotelCode = dsHotel.Tables[1].Select("XMLHotelCode='" + HotelId + "'");
                //   DataRow[] drRoom = dsHotel.Tables[3].Select("RoomTypeId='" + RoomTypeId_Own + "'");
                string RoomTypeId1 = RoomTypeId_Own;// Convert.ToString(drRoom[0]["RoomTypeId"]);
                string[] RoomTypeIdList = Convert.ToString(RoomTypeId1).Split(new String[] { "$$$$" }, StringSplitOptions.None);
                string group_code = RoomTypeIdList[4];
                //ststring group_code = RoomTypeIdList[4];
                string cancellation_policycode1 = RoomTypeIdList[1];
                string rate_key = RoomTypeIdList[2];
                string room_type = RoomTypeIdList[0];
                string sid = RoomTypeIdList[3];
                string hcode = RoomTypeIdList[6];
                string rate_type = RoomTypeIdList[5];
                string noofinfants = RoomTypeIdList[9];
                string city_code = RoomTypeIdList[8];
                string room_code = RoomTypeIdList[7];
                string Nationality = RoomTypeIdList[10];
                string noofrm = RoomTypeIdList[11];
                string Roomreference = RoomTypeIdList[13];
                string essen = RoomTypeIdList[14];
                //string essen = "";

                string Password = Convert.ToString(drSuplierMarkup[0]["Password"]);
                string Username = Convert.ToString(drSuplierMarkup[0]["Username"]);

                //  SessionID = Convert.ToString(drRoom[0]["Type"]);

                // string cancellation_policy_code = RoomTypeIdList[1];
                DataTable dtgrncRoomCacelationPolicy = _commonMethod.GenerateCancellationDataTableNewV2();
                //string Urls = Convert.ToString(drSuplierMarkup[0]["WebSiteURL"]) + "/api/v3/hotels/availability/" + sid + "/rates/?action=" + rate_type;
                string Urls = Convert.ToString(drSuplierMarkup[0]["WebSiteURL"]) + _supplierConfig.GRNC.HotelSearch + sid + "/rates/?action=" + rate_type;

                string RecheckRQ = GRNCrecheckRequest(HotelId, type, rate_type, rate_key, group_code, cancellation_policycode1);
                xslRequest = RecheckRQ;
                //System.IO.File.WriteAllText(@"F:\Pratik\log\RecheckRequest.txt", RecheckRQ);
                _commonMethod.ThirdPartyGetPolicyHistory(Convert.ToString(RoomTypeId_Own), Convert.ToString(ownsystemid), RecheckRQ, Convert.ToString(ResponseData), "After Get Policy", Provider, "Response", "-", "");
                //ThirdPartyBookingHistory("", "", RecheckRQ, "-", "Before get price " + Provider + " request hit " + 0 + "is for Price Check" + "p", Provider, "request confirm", "-", "WithinEarth");

                System.Diagnostics.Stopwatch timerroomprocess = new System.Diagnostics.Stopwatch();
                timerroomprocess.Start();

                //ResponseData = GetSupplierResponseGRNCForReport(ref Error, sid, RecheckRQ, Urls, "");
                //ResponseDto responseDto = await GetSupplierResponseGRNCForReport(sid, RecheckRQ, Urls, "");
                ResponseDto responseDto = await GetSupplierResponseGRNCForReport(sid, RecheckRQ, Urls, Password);
                ResponseData = responseDto.Response;
                Error = responseDto.Error; 


                ResponseDataLog = ResponseData;

                timerroomprocess.Stop();
                time = timerroomprocess.Elapsed.ToString();
                _commonMethod.ThirdPartyGetPolicyHistory(Convert.ToString(RoomTypeId_Own), Convert.ToString(ownsystemid), RecheckRQ, Convert.ToString(ResponseData + Error), "After Get Policy", Provider, "Response", "-", "");

                //System.IO.File.WriteAllText(@"F:\Pratik\log\recheckResponse.txt", ResponseData);
                //ThirdPartyBookingHistory("", "", RecheckRQ, ResponseData, "After get price " + Provider + " request hit " + 0 + "is for Price Check" + "p", Provider, "response confirm", "-", "WithinEarth");
                //if (ResponseData1 == null || ResponseData1 == "" || ResponseData1 == string.Empty)
                //    return false;
                string node = "";
                try
                {
                    node = Newtonsoft.Json.JsonConvert.DeserializeXmlNode(ResponseData, "Root").InnerXml;

                    if (node != "")
                    {
                        if (node.Contains("<rate_comments>") == true)
                        {
                            String Data2 = node.Substring(node.IndexOf("<rate_comments"), node.IndexOf("</rate_comments>") - node.IndexOf("<rate_comments")) + "</rate_comments>";

                            node = Data2;
                        }
                    }

                }
                catch (Exception ex)
                {
                    
                    _ = _exception.Error(ex);
                    Error = Error + "#= " + ex.Message + "#= " + ex.StackTrace.ToString();
                }
                string Rate_coment = "";
                try
                {
                    if (node != "")
                    {


                        DataSet dsSTRS = _commonMethod.GetDataSetFromXMLString(node);


                        for (int i = 0; i < dsSTRS.Tables["rate_comments"].Columns.Count; i++)
                        {

                            string ColoumnName = dsSTRS.Tables["rate_comments"].Columns[i].ColumnName;
                            Rate_coment = Rate_coment + "<b>" + ColoumnName + ":</b>" + dsSTRS.Tables["rate_comments"].Rows[0][ColoumnName] + "</br>";
                        }
                    }
                }
                catch (Exception ex)
                {
                    _ = _exception.Error(ex);
                    Error = Error + "#= " + ex.Message + "#= " + ex.StackTrace.ToString();
                }
                recheck.Example rck = Newtonsoft.Json.JsonConvert.DeserializeObject<recheck.Example>(ResponseData);

                bool notmatch = false;
                for (int k = 0; k < rck.hotel.rate.rooms.Count(); k++)
                {
                    int count = 0;
                    string[] RoomreferenceNew = Convert.ToString(Roomreference).Split(new String[] { "###" }, StringSplitOptions.None);
                    string roomref = rck.hotel.rate.rooms[k].room_reference;
                    for (int p = 0; p < RoomreferenceNew.Length; p++)
                    {

                        if (roomref == RoomreferenceNew[p])
                        {
                            count = 1 + count;
                        }
                    }
                    if (count == 0)
                    {
                        notmatch = true;
                        break;

                    }
                }

                string Currency = Convert.ToString(drSuplierMarkup[0]["CurrencyShortName"]);

                DataTable dtInrCurrency = _commonMethod.GetCountryCurrency(Currency);
                double CurrencyFactorQTech = Convert.ToDouble(dtInrCurrency.Rows[0]["AmtToExch"]);

                if (ResponseData.Contains("error") != true)
                {
                    string firstNightCharge = "";
                    string group_Code = rck.hotel.rate.group_code;
                    bool non_refundable = Convert.ToBoolean(rck.hotel.rate.non_refundable);
                    string cpolocycode = Convert.ToString(rck.hotel.rate.cancellation_policy_code);

                    string cancellation_policy = Convert.ToString(rck.hotel.rate.cancellation_policy);
                    string cancellation_policy_Code = Convert.ToString(rck.hotel.rate.cancellation_policy_code);
                    string hotel_charge = "";
                    bool mandetory = false;
                    if (ResponseData.Contains("mandatory"))
                    {
                        try
                        {
                            mandetory = Convert.ToBoolean(rck.hotel.rate.price_details.hotel_charges[0].mandatory);
                            hotel_charge = Convert.ToString(rck.hotel.rate.price_details.hotel_charges);
                        }
                        catch (Exception ex)
                        {
                            mandetory = false;
                            _exception.Error(ex);
                            Error = Error + "#= " + ex.Message + "#= " + ex.StackTrace.ToString();
                        }

                    }
                    hotel_charge = Convert.ToString(rck.hotel.rate.price_details.hotel_charges);
                    bool include = true;
                    string amount_type = "";
                    string currency = "";
                    //if (hotel_charge != "")
                    string info = " You Have To Pay This Amount On Hotel Desk.This Is Hotel Extra Charges.";
                    string amounttypes = "";
                    string name = "";

                    if (rck.hotel.rate.price_details.hotel_charges != null)
                    {
                        for (int i = 0; i < rck.hotel.rate.price_details.hotel_charges.Count; i++)
                        {
                            string amount = "";
                            include = Convert.ToBoolean(rck.hotel.rate.price_details.hotel_charges[i].included);
                            amount_type = Convert.ToString(rck.hotel.rate.price_details.hotel_charges[i].amount_type);
                            currency = Convert.ToString(rck.hotel.rate.price_details.hotel_charges[i].currency);
                            name = Convert.ToString(rck.hotel.rate.price_details.hotel_charges[i].name);

                            if (include == false && mandetory == true)
                            {
                                amount = name + ":" + Convert.ToString(rck.hotel.rate.price_details.hotel_charges[i].amount) + " " + currency + info;
                                amounttypes = amounttypes + "</br>" + amount;
                            }

                        }

                    }

                    string spl_checkin_instructions = Convert.ToString(Rate_coment);
                    //string checkin_instructions = Convert.ToString(rck.hotel.rate.rate_comments.checkin_instructions);
                    // string comments = Convert.ToString(rck.hotel.rate.rate_comments.comments);

                    string boarding_details = Convert.ToString(rck.hotel.rate.boarding_details[0]);
                    string Prices = Convert.ToString(rck.hotel.rate.price);
                    string roomcode = Convert.ToString(rck.hotel.rate.room_code);
                    string ratekey = Convert.ToString(rck.hotel.rate.rate_key);
                    //string EssentialInfo = spl_checkin_instructions + "</br>" + checkin_instructions + "</br>" + comments;
                    string EssentialInfo = spl_checkin_instructions;


                    if (Convert.ToDouble(CompanyBuying).TruncateDecimalPlaces(0) < Convert.ToDouble(Prices).TruncateDecimalPlaces(0))
                    {
                        ResponseDataLog = "#Price Changed " + "#Main Price = " + CompanyBuying + " #New Price = " + Prices + " #Response= " + ResponseDataLog;
                        responsetype = "GeneralError";
                        Prices = "0";
                    }
                    if (notmatch == true)
                    {
                        Prices = "0";
                    }
                    string code = Prices + "@@@" + roomcode + "@@@" + ratekey;


                    if (Prices == "0")
                    {
                        responsetype = "GeneralError";
                    }



                    #region Cancellation Policy RQ & RES
                    Boolean isPerce = true;
                    double AgentCompanyPercentage = Convert.ToDouble(objSearPera.AgentCompanyPercentage);
                    bool IsB2C = Convert.ToBoolean(Convert.ToByte(objSearPera.IsB2BOrB2C));
                    double B2CMarkup = (IsB2C == false ? AgentCompanyPercentage : XMLHotelBookingCommon.B2CMarkup);
                    double AgentPercentage = 0.0;
                    double AgentMarkupAmount = 0.0;
                    double SubAgentMarkup = 0;
                    int IsParentId = 1;
                    if (dtAgentDetail != null)
                    {
                        IsParentId = Convert.ToInt32(dtAgentDetail.Rows[0]["IsParentId"]);
                        SubAgentMarkup = Convert.ToDouble(dtAgentDetail.Rows[0]["B2CMarkupPer"]);
                    }
                    int CutOffDays = XMLHotelBookingCommon.CutOffDays;
                    CutOffDays = _commonMethod.GetCutOffDays(dtSuplierMarkup, "grnc");
                    string username = Convert.ToString(drSuplierMarkup[0]["UserName"]);
                    string password = Convert.ToString(drSuplierMarkup[0]["Password"]);
                    string UserId = Convert.ToString(drSuplierMarkup[0]["UserID"]);
                    string WBEKey = Convert.ToBase64String(Encoding.ASCII.GetBytes(username + ":" + password));
                    string CheckIn = objSearPera.CheckInDate;
                    string CheckOut = objSearPera.CheckOutDate;
                    //string Url = Convert.ToString(drSuplierMarkup[0]["WebSiteURL"]) + "/api/v3/hotels/availability/" + sid + "/rates/cancellation_policies";
                    string Url = Convert.ToString(drSuplierMarkup[0]["WebSiteURL"]) + _supplierConfig.GRNC.HotelSearch + sid + _supplierConfig.GRNC.Policies;

                    

                    #endregion


                    //DateTime from = DateTime.Now.Date;
                    DateTime cancel_by_date = DateTime.Now.Date;

                    string Currancy = "";
                    if (cancellation_policy_Code != null)
                    {
                        string CancellationPolicyRQ = GRNCCancelltionPolicyRequests(HotelId, type, roomtypecode, rate_key/*, CurrencyCode, amount_type*/, checkin, checkout, cancellation_policy_Code, rate_key);
                        //ResponseData = GetSupplierResponseGRNC(sid, CancellationPolicyRQ, Url, WBEKey);
                        ResponseData = GetSupplierResponseGRNC(sid, CancellationPolicyRQ, Url, password);

                        GRNCsearch.RoomCancellationPolicyv1 grncResponseCancelltionPolicy =  Newtonsoft.Json.JsonConvert.DeserializeObject<GRNCsearch.RoomCancellationPolicyv1>(ResponseData);
                        if (grncResponseCancelltionPolicy == null)/*|| grncResponseCancelltionPolicy.under_cancellation == false)*/
                        {
                            cancellationDto.dsHotelData = dsHotel;
                            return cancellationDto;
                        }
                        amount_type = Convert.ToString(grncResponseCancelltionPolicy.amount_type);
                        cancel_by_date = Convert.ToDateTime(grncResponseCancelltionPolicy.cancel_by_date/*.FromDate*/);
                        Currancy = Convert.ToString(grncResponseCancelltionPolicy.details[0].currency);
                    }

                    if (ResponseData == null || ResponseData == "" || ResponseData == string.Empty)
                    {
                        responsetype = "GeneralError";
                        cancellationDto.dsHotelData = dsHotel;
                        return cancellationDto;
                    }
                    else
                    {

                        double cancellationChargeAmount = Convert.ToDouble(roomprice);
                        //double cancellationChargeAmount = Convert.ToDouble(Prices);

                        DateTime currentdate = DateTime.Now.Date;

                        #region Vat   
                        double VatOnPolicy = 0;
                        //cancellationChargeAmount = ProcessHotelCancelationPrice_WithMarkup(ref roomprice, objSearPera.NoofNights, objSearPera.CityID, objSearPera.CountryId, roomprice, isPerce, B2CMarkup, AgentPercentage, AgentMarkupAmount, IsParentId, SubAgentMarkup, ref VatOnPolicy);
                        #endregion
                        if (non_refundable == true)
                        {
                            dtgrncRoomCacelationPolicy.Rows.Add(_commonMethod.setCancellationPolicyRow(ref cancellationChargeAmount, dtgrncRoomCacelationPolicy.NewRow(), 1, Currency, Provider, Convert.ToString(ownsystemid), Convert.ToString(1), RoomTypeId_Own, room_type, boarding_details, DateTime.Now.Date, DateTime.ParseExact(objSearPera.CheckInDate, "dd/MM/yyyy", null), cancellationChargeAmount, cancellationChargeAmount, boarding_details, 0, "", "", code, amounttypes + "</br>" + essen + "</br>" + EssentialInfo, "", ""));
                            dtgrncRoomCacelationPolicy.AcceptChanges();
                        }

                        //if (from < currentdate)
                        //{
                        //    from = DateTime.Now.Date;
                        //}


                        if (cancellation_policy != "")
                        {
                            cancel_by_date = Convert.ToDateTime(rck.hotel.rate.cancellation_policy.cancel_by_date);
                            if (cancel_by_date.AddDays(CutOffDays).Date > DateTime.Now.Date)
                            {
                                cancel_by_date = cancel_by_date.AddDays(CutOffDays).Date;
                            }
                            else
                            {
                                cancel_by_date = DateTime.Now.Date;
                            }
                            bool under_canllation = Convert.ToBoolean(rck.hotel.rate.cancellation_policy.under_cancellation);
                            string detail = Convert.ToString(rck.hotel.rate.cancellation_policy.details);
                            double persentage = Convert.ToDouble(rck.hotel.rate.cancellation_policy.details[0].percent);
                            DateTime frm = Convert.ToDateTime(rck.hotel.rate.cancellation_policy.details[0].from);
                            double flat_fee = Convert.ToDouble(rck.hotel.rate.cancellation_policy.details[0].flat_fee);
                            if (cancel_by_date > currentdate)
                            {

                                if (dtgrncRoomCacelationPolicy.Rows.Count == 0)
                                {
                                    double Zero = 0;
                                    dtgrncRoomCacelationPolicy.Rows.Add(_commonMethod.setCancellationPolicyRow(ref Zero, dtgrncRoomCacelationPolicy.NewRow(), 1, Currency, Provider, Convert.ToString(ownsystemid), Convert.ToString(1), RoomTypeId_Own, room_type, boarding_details, DateTime.Now.Date, cancel_by_date.AddDays(-1), 0, 0, boarding_details, 0, "", "", code, amounttypes + "</br>" + essen + "</br>" + EssentialInfo, "", ""));
                                    dtgrncRoomCacelationPolicy.AcceptChanges();
                                }
                                dtgrncRoomCacelationPolicy.Rows.Add(_commonMethod.setCancellationPolicyRow(ref cancellationChargeAmount, dtgrncRoomCacelationPolicy.NewRow(), 1, Currency, Provider, Convert.ToString(ownsystemid), Convert.ToString(1), RoomTypeId_Own, room_type, boarding_details, cancel_by_date, DateTime.ParseExact(objSearPera.CheckInDate, "dd/MM/yyyy", null), cancellationChargeAmount, cancellationChargeAmount, boarding_details, 0, "", "", code, amounttypes + "</br>" + essen + "</br>" + EssentialInfo, "", ""));
                                dtgrncRoomCacelationPolicy.AcceptChanges();
                            }
                            else
                            {
                                dtgrncRoomCacelationPolicy.Rows.Add(_commonMethod.setCancellationPolicyRow(ref cancellationChargeAmount, dtgrncRoomCacelationPolicy.NewRow(), 1, Currency, Provider, Convert.ToString(ownsystemid), Convert.ToString(1), RoomTypeId_Own, room_type, boarding_details, DateTime.Now.Date, DateTime.ParseExact(objSearPera.CheckInDate, "dd/MM/yyyy", null), cancellationChargeAmount, cancellationChargeAmount, boarding_details, 0, "", "", code, amounttypes + "</br>" + essen + "</br>" + EssentialInfo, "", ""));
                                dtgrncRoomCacelationPolicy.AcceptChanges();
                            }
                        }
                    }
                }
                else
                {
                    responsetype = "GeneralError";

                    double cancellationChargeAmount = Convert.ToDouble(roomprice);
                    string boarding_details = "ro";
                    dtgrncRoomCacelationPolicy.Rows.Add(_commonMethod.setCancellationPolicyRow(ref cancellationChargeAmount, dtgrncRoomCacelationPolicy.NewRow(), 1, Currency, Provider, Convert.ToString(ownsystemid), Convert.ToString(1), RoomTypeId_Own, room_type, MealId, DateTime.Now.Date, DateTime.ParseExact(objSearPera.CheckInDate, "dd/MM/yyyy", null), cancellationChargeAmount, cancellationChargeAmount, MealId, 0, "", "", "0", "This room is no longer available", "", ""));
                    dtgrncRoomCacelationPolicy.AcceptChanges();
                }
                #region SupplierPR Price Check
                try
                {
                    string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, xslRequest, ResponseDataLog, Error);
                }
                catch (Exception e)
                {
                    _ = _exception.ErrorBooking(e, AgentId.ToString(), "", "");
                }
                #endregion
                dsHotel.Tables[2].Merge(dtgrncRoomCacelationPolicy);
            }
            catch (Exception ex)
            {
                _ = _exception.ErrorNew(ex, _LogAPI.Recheck, SearchKey);

                #region SupplierPerformanceReport Catch
                try
                {
                    Error = Error + "#= " + ex.Message + "#= " + ex.StackTrace.ToString();
                    responsetype = "GeneralError";
                    string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, xslRequest, ResponseDataLog, Error);
                }
                catch (Exception e)
                {
                    _exception.ErrorBooking(e, AgentId.ToString(), "", "");
                }
                #endregion

            }
            cancellationDto.dsHotelData = dsHotel;
            return cancellationDto;
        }

        public string GRNCrecheckRequest(string xmlhotelcode, string type, string rate_type, string rate_key/*, string CurrencyCode, string amount_type*/, string group_code, string cancellation_policy_code)
        {
            try
            {

                string Request = "";
                StringBuilder SBreq = new StringBuilder();
                SBreq.Append("{");

                SBreq.Append("\"rate_key\": \"" + rate_key + "\",");
                SBreq.Append("\"group_code\": \"" + group_code + "\"");

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
        public string GRNCCancelltionPolicyRequests(string xmlhotelcode, string SID, string TID, string rate/*, string CurrencyCode, string amount_type*/, string checkin, string checkout, string cancellation_policy_code, string rate_key)
        {
            try
            {

                string Request = "";
                StringBuilder SBreq = new StringBuilder();
                SBreq.Append("{");

                SBreq.Append("\"rate_key\": \"" + rate + "\",");
                SBreq.Append("\"cp_code\": \"" + cancellation_policy_code + "\"");

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
        public async Task<ResponseDto> GetSupplierResponseGRNCForReport(string sid, string strxmlRQRS, string url, string Key)
        {
            System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
            timer.Start();
            string strOutput = "";
            string responsedata = "";
            string Error = "";

            ResponseDto responseDto = new ResponseDto();
            try
            {
                HttpWebRequest Request;
                Request = (HttpWebRequest)HttpWebRequest.Create(url);

                Request.Method = "POST";
                //Request.UserAgent = "Mozilla/4.0 (compatible; MSIE 6.0; MS Web Services Client Protocol 1.1.4322.2300)";
                //Request.Headers.Add("Api-Key", "8d09f4f93c7d96122f41db96810f51db");//test
                //Request.Headers.Add("Api-Key", "af3675c6f7819722b572fcd4bcd6598d");//live
                Request.Headers.Add("Api-Key", Key);//live

                Request.ContentType = "application/json";
                Request.Accept = "application/json";
                //Request.Headers.Add("Accept", "application/json");
                //Request.Headers.Add("Accept-Encoding", "application/gzip");
                Stream newStream = null;
                byte[] data = Encoding.ASCII.GetBytes(strxmlRQRS);
                Request.ContentLength = data.Length;
                newStream = Request.GetRequestStream();
                newStream.Write(data, 0, data.Length);
                #region close
                //    HttpWebRequest Request;
                //    Request = (HttpWebRequest)HttpWebRequest.Create(strHost);
                //    //Request.Method = "POST";
                //    //Request.Accept = "text/xml";
                //    //Request.ContentType = "text/xml;charset=utf-8";
                //    //Request.UserAgent = "Mozilla/4.0 (compatible; MSIE 6.0; MS Web Services Client Protocol 1.1.4322.2300";

                //    //Request.ContentType = "application/json";
                //    //Request.Headers.Add("WBE-Api-Key", UserId);
                //    //Request.Headers.Add("Authorization", "Basic " + Key);
                //    Request.Method = "POST";
                //    //Request.UserAgent = "Mozilla/4.0 (compatible; MSIE 6.0; MS Web Services Client Protocol 1.1.4322.2300)";
                //    Request.Headers.Add("Api-Key", "8d09f4f93c7d96122f41db96810f51db");
                //    Request.ContentType = "application/json";
                //    Request.Accept = "application/json";
                //    //Request.Headers.Add("Accept", "application/json");
                //    Request.Headers.Add("Accept-Encoding", "application/gzip");


                //    Stream newStream = null;

                //    byte[] data = Encoding.ASCII.GetBytes(strxmlRQRS);
                //    Request.ContentLength = data.Length;

                //    newStream = Request.GetRequestStream();
                //    newStream.Write(data, 0, data.Length);
                #endregion
                try
                {
                    Task<string> Response;
                    HttpWebResponse response = (HttpWebResponse)await Request.GetResponseAsync();
                    Stream responseStream = response.GetResponseStream();
                    //// if (response.ContentEncoding.ToLower().Contains("gzip"))
                    //    responseStream = new System.IO.Compression.GZipStream(responseStream, System.IO.Compression.CompressionMode.Decompress);
                    // else if (response.ContentEncoding.ToLower().Contains("deflate"))
                    //     responseStream = new System.IO.Compression.DeflateStream(responseStream, System.IO.Compression.CompressionMode.Decompress);
                    StreamReader streamReader = new StreamReader(responseStream);
                    Response = streamReader.ReadToEndAsync();
                    //responsedata = await Response.ConfigureAwait(false);
                    strOutput = Response.Result.ToString();
                    responseStream.Close();
                    streamReader.Close();
                    response.Close();

                    timer.Stop();
                    if (_keySettings.IsLogTrack == "1")
                    {
                        TimeSpan timeTaken = timer.Elapsed;
                        //_exception.ErrorLog(Convert.ToString(_keySettings.LogTrackPath), sid + "-> Execution Time : " + timer.Elapsed.ToString());
                    }
                }
                catch (WebException webExcp)
                {
                    HttpWebResponse httpResponse = (HttpWebResponse)webExcp.Response;
                    string result = httpResponse.StatusDescription.ToString();
                    string result1 = new StreamReader(webExcp.Response.GetResponseStream()).ReadToEnd();
                    Error = result1;

                    responseDto.Response = "";
                    responseDto.Error = Error;

                    return responseDto;
                }
            }
            catch (Exception ex)
            {
                Error = ex.Message.ToString();
                 _= _exception.Error(ex);

                responseDto.Response = "";
                responseDto.Error = Error;
                return responseDto;
            }

            responseDto.Response = strOutput;
            responseDto.Error = Error;
            return responseDto;
        }

        public string GetSupplierResponseGRNC(string sid, string strxmlRQRS, string url, string Key)
        {
            System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
            timer.Start();
            string strOutput = "";
            string responsedata = "";
            try
            {
                HttpWebRequest Request;
                Request = (HttpWebRequest)HttpWebRequest.Create(url);

                Request.Method = "POST";
                //Request.UserAgent = "Mozilla/4.0 (compatible; MSIE 6.0; MS Web Services Client Protocol 1.1.4322.2300)";
                //Request.Headers.Add("Api-Key", "8d09f4f93c7d96122f41db96810f51db");//test
                //Request.Headers.Add("Api-Key", "af3675c6f7819722b572fcd4bcd6598d");//live
                Request.Headers.Add("Api-Key", Key);//live

                Request.ContentType = "application/json";
                Request.Accept = "application/json";
                //Request.Headers.Add("Accept", "application/json");
                //Request.Headers.Add("Accept-Encoding", "application/gzip");
                Stream newStream = null;
                byte[] data = Encoding.ASCII.GetBytes(strxmlRQRS);
                Request.ContentLength = data.Length;
                newStream = Request.GetRequestStream();
                newStream.Write(data, 0, data.Length);
                #region close
                //    HttpWebRequest Request;
                //    Request = (HttpWebRequest)HttpWebRequest.Create(strHost);
                //    //Request.Method = "POST";
                //    //Request.Accept = "text/xml";
                //    //Request.ContentType = "text/xml;charset=utf-8";
                //    //Request.UserAgent = "Mozilla/4.0 (compatible; MSIE 6.0; MS Web Services Client Protocol 1.1.4322.2300";

                //    //Request.ContentType = "application/json";
                //    //Request.Headers.Add("WBE-Api-Key", UserId);
                //    //Request.Headers.Add("Authorization", "Basic " + Key);
                //    Request.Method = "POST";
                //    //Request.UserAgent = "Mozilla/4.0 (compatible; MSIE 6.0; MS Web Services Client Protocol 1.1.4322.2300)";
                //    Request.Headers.Add("Api-Key", "8d09f4f93c7d96122f41db96810f51db");
                //    Request.ContentType = "application/json";
                //    Request.Accept = "application/json";
                //    //Request.Headers.Add("Accept", "application/json");
                //    Request.Headers.Add("Accept-Encoding", "application/gzip");


                //    Stream newStream = null;

                //    byte[] data = Encoding.ASCII.GetBytes(strxmlRQRS);
                //    Request.ContentLength = data.Length;

                //    newStream = Request.GetRequestStream();
                //    newStream.Write(data, 0, data.Length);
                #endregion
                try
                {
                    Task<string> Response;
                    HttpWebResponse response = (HttpWebResponse)Request.GetResponse();
                    Stream responseStream = response.GetResponseStream();
                    //// if (response.ContentEncoding.ToLower().Contains("gzip"))
                    //    responseStream = new System.IO.Compression.GZipStream(responseStream, System.IO.Compression.CompressionMode.Decompress);
                    // else if (response.ContentEncoding.ToLower().Contains("deflate"))
                    //     responseStream = new System.IO.Compression.DeflateStream(responseStream, System.IO.Compression.CompressionMode.Decompress);
                    StreamReader streamReader = new StreamReader(responseStream);
                    Response = streamReader.ReadToEndAsync();
                    //responsedata = await Response.ConfigureAwait(false);
                    strOutput = Response.Result.ToString();
                    responseStream.Close();
                    streamReader.Close();
                    response.Close();

                    timer.Stop();
                    if (Convert.ToBoolean(_keySettings.IsLogTrack))
                    {
                        TimeSpan timeTaken = timer.Elapsed;
                        _exception.ErrorLog(Convert.ToString(_keySettings.LogTrackPath), sid + "-> Execution Time : " + timer.Elapsed.ToString());
                    }
                }
                catch (WebException webExcp)
                {
                    HttpWebResponse httpResponse = (HttpWebResponse)webExcp.Response;
                    string result = httpResponse.StatusDescription.ToString();
                    string result1 = new StreamReader(webExcp.Response.GetResponseStream()).ReadToEnd();
                    return "";
                }
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
            }

            return strOutput;
        }

        public string GRNCBookingResponse(ref string Error, string sid, string Xmlrequest, string Url, string username, string password, int Timeout, string UserId)
        {
            string responseString = "";
            System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
            timer.Start();
            string strOutput = "";
            string responsedata = "";
            try
            {
                string Key = Convert.ToBase64String(Encoding.ASCII.GetBytes(username + ":" + password));
                string reqUrl = Url;
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(reqUrl);
                var postData = Xmlrequest;
                request.Method = "POST";
                //Request.UserAgent = "Mozilla/4.0 (compatible; MSIE 6.0; MS Web Services Client Protocol 1.1.4322.2300)";
                request.Headers.Add("Api-Key", "af3675c6f7819722b572fcd4bcd6598d");
                request.ContentType = "application/json";
                request.Accept = "application/json";
                //Request.Headers.Add("Accept", "application/json");
                // request.Headers.Add("Accept-Encoding", "application/gzip");
                Stream newStream = null;
                byte[] data = Encoding.ASCII.GetBytes(postData);
                request.ContentLength = data.Length;
                newStream = request.GetRequestStream();
                newStream.Write(data, 0, data.Length);


                //byte[] data = Encoding.ASCII.GetBytes(postData);
                //request.ContentLength = data.Length;

                //newStream = request.GetRequestStream();
                //newStream.Write(data, 0, data.Length);
                try
                {
                    Task<string> Response;

                    HttpWebResponse response1 = (HttpWebResponse)request.GetResponse();
                    Stream responseStream = response1.GetResponseStream();
                    //StreamReader Reader = new StreamReader(responseStream, Encoding.Default);
                    // responseStream = new GZipStream(responseStream, CompressionMode.Decompress);

                    //  if (response1.ContentEncoding.ToLower().Contains("gzip"))
                    //      responseStream = new System.IO.Compression.GZipStream(responseStream, System.IO.Compression.CompressionMode.Decompress);
                    //  else if (response1.ContentEncoding.ToLower().Contains("deflate"))
                    //      responseStream = new System.IO.Compression.DeflateStream(responseStream, System.IO.Compression.CompressionMode.Decompress);
                    StreamReader streamReader = new StreamReader(responseStream);


                    Response = streamReader.ReadToEndAsync();
                    //responsedata = await Response.ConfigureAwait(false);

                    strOutput = Response.Result.ToString();
                    responseStream.Close();
                    streamReader.Close();
                    response1.Close();

                    timer.Stop();
                    if (Convert.ToString(_keySettings.IsLogTrack) == "1")
                    {
                        TimeSpan timeTaken = timer.Elapsed;
                        //ErrorLog(Convert.ToString(System.Web.Configuration.WebConfigurationManager.AppSettings["LogTrackPath"]), sid + "-> Execution Time : " + timer.Elapsed.ToString());

                    }
                }
                catch (WebException webExcp)
                {

                    HttpWebResponse httpResponse = (HttpWebResponse)webExcp.Response;
                    string result = httpResponse.StatusDescription.ToString();
                    string result1 = new StreamReader(webExcp.Response.GetResponseStream()).ReadToEnd();
                    Error = result1;
                    _ =_exception.ErrorBooking(webExcp, "0", "", "");
                    return "";
                }

                //var response = (HttpWebResponse)request.GetResponse();
                //responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();
            }
            catch (Exception ex)
            {
                Error = ex.Message.ToString();
                _ = _exception.ErrorBooking(ex, "0", "", "");

            }
            return strOutput;
        }

        public string GRNCBookingRequest1(DataSet dsCartDetails, DataTable dtPassengers, int i, int j, int UniqueNo, string PreBookRef, int NoOfRoom, string HotelId, string TID, string ContryId, string CITY_CODE, string AgentCode)
        {
            try
            {
                string UserCartId = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["UserCartId"]);
                string checkindate = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["CheckInDate"]);
                string checkin = (checkindate).Substring(0, 4) + "-" + (checkindate).Substring(4, 2) + "-" + (checkindate).Substring(6, 2);// + "T03:00:00+03:00";
                string checkoutdate = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["CheckOutDate"]);
                string checkout = (checkoutdate).Substring(0, 4) + "-" + (checkoutdate).Substring(4, 2) + "-" + (checkoutdate).Substring(6, 2);// + "T03:00:00+03:00";
                string AgentReference = "WE_" + Convert.ToString(AgentCode + "_" + UniqueNo);
                //string AgentReference = "WE_" + Convert.ToString(Common.getSession("AgentCode") + "_" + UniqueNo);
                string firstName = ""; string lastName = ""; string title = "";
                string BookingRQ = "";
                DataRow[] drRoom = dsCartDetails.Tables[1].Select("RoomTypeId='" + TID + "'");
                string RoomTypeId1 = Convert.ToString(drRoom[0]["RoomTypeId"]);
                string[] RoomTypeIdList = Convert.ToString(RoomTypeId1).Split(new String[] { "$$$$" }, StringSplitOptions.None);
                string group_code = RoomTypeIdList[4];
                string rate_key = RoomTypeIdList[2];
                string cancellation_policy_code1 = RoomTypeIdList[1];
                string room_type = RoomTypeIdList[0];
                string sid = RoomTypeIdList[3];
                string hcode = RoomTypeIdList[6];
                string rate_type = RoomTypeIdList[5];
                string noofinfants = RoomTypeIdList[9];
                string city_code = RoomTypeIdList[8];
                string room_code = RoomTypeIdList[7];
                string Nationality = RoomTypeIdList[10];
                string noofrm = RoomTypeIdList[11];
                string Roomreference = RoomTypeIdList[13];
                string code = Convert.ToString(dsCartDetails.Tables[1].Rows[j]["purchaseToken"]);
                string[] roomtype = Convert.ToString(code).Split(new String[] { "@@@" }, StringSplitOptions.None);
                string price = roomtype[0];
                string roomcode = roomtype[1];
                string ratekey = roomtype[2];

                string[] Roomreference2 = Convert.ToString(Roomreference).Split(new String[] { "###" }, StringSplitOptions.None);
                int room = Convert.ToInt32(noofrm);
                StringBuilder SBPassanger = new StringBuilder();
                StringBuilder SBContect = new StringBuilder();
                DataRow[] drHotelBookingDetail = dsCartDetails.Tables[1].Select("UserCartId='" + UserCartId + "'");
                int RoomNumber = Convert.ToInt32(drHotelBookingDetail[j]["RoomNumber"]);
                DataRow[] drPassengers = dtPassengers.DefaultView.ToTable().Select("IsLead='1'");
                if (drPassengers.Length == 0)
                {
                    if (Convert.ToString(dtPassengers.DefaultView[0]["PaxType"]).Trim().ToLower() == "adult")
                        dtPassengers.DefaultView[0]["IsLead"] = 1;
                }
                firstName = Convert.ToString(dtPassengers.DefaultView[0]["FirstName"]).Trim();
                lastName = Convert.ToString(dtPassengers.DefaultView[0]["LastName"]).Trim();
                title = Convert.ToString(dtPassengers.DefaultView[0]["Prefix"]).Trim().Replace(".", "");
                int ChildAge = Convert.ToInt32(dtPassengers.DefaultView[0]["ChildAge"]);
                if (Convert.ToInt16(dtPassengers.DefaultView[0]["IsLead"]) == 1)
                {
                    SBContect.Append("\"holder\": {");
                    SBContect.Append("\"title\": \"" + title + ".\",");
                    SBContect.Append("\"name\": \"" + firstName + "\",");
                    SBContect.Append("\"surname\": \"" + lastName + "\",");
                    SBContect.Append("\"email\": \"" + GrncStaticInfo.email + "\",");
                    SBContect.Append("\"phone_number\": \"" + GrncStaticInfo.WorkPhone_Xpedia + "\",");
                    SBContect.Append("\"client_nationality\": \"" + Nationality + "\"");
                    SBContect.Append("}");
                }
                SBPassanger.Append("\"booking_items\":[");
                for (int p = 0; p < room; p++)
                {
                    int NoOfChild = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[p]["NoOfChild"]);
                    int NoOfAdults = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[p]["NoOfAdults"]);
                    SBPassanger.Append("{");
                    if (p == 0)
                    {
                        SBPassanger.Append("\"room_code\": \"" + roomcode + "\",");
                        SBPassanger.Append("\"rate_key\": \"" + ratekey + "\",");
                        SBPassanger.Append("\"rooms\":[");
                        SBPassanger.Append("{");
                    }
                    int Child1Age = Convert.ToInt32(drHotelBookingDetail[p]["ChildAge1"]);
                    int Child2Age = Convert.ToInt32(drHotelBookingDetail[p]["ChildAge2"]);
                    DataRow[] dsAdults = dtPassengers.Select("RoomNo=" + RoomNumber + " and UserCartId = '" + UserCartId + "'");
                    // int childage = Convert.ToInt32(dtPassengers.DefaultView[p]["ChildAge"]);
                    int ChildAge1 = 0;
                    int ChildAge2 = 0;
                    int NoOfInfant = 0;
                    int ChildAges = 0;
                    if (NoOfChild == 1)
                    {
                        ChildAge1 = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[p]["ChildAge1"]);
                        if (ChildAge1 < 2)
                        {
                            NoOfChild = NoOfChild - 1;
                            NoOfInfant = NoOfInfant + 1;
                        }
                        else
                        {
                            NoOfChild += NoOfChild - 1;
                            ChildAges = ChildAges + 1;
                        }
                    }
                    else if (NoOfChild == 2)
                    {
                        ChildAge1 = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[p]["ChildAge1"]);
                        if (ChildAge1 < 2)
                        {
                            NoOfChild = NoOfChild - 1;
                            NoOfInfant = NoOfInfant + 1;
                        }
                        else
                        {
                            NoOfChild += NoOfChild - 1;
                            ChildAges = ChildAges + 1;
                        }
                        ChildAge2 = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[p]["ChildAge2"]);
                        if (ChildAge2 < 2)
                        {
                            NoOfChild = NoOfChild - 1;
                            NoOfInfant = NoOfInfant + 1;
                        }
                        else
                        {
                            NoOfChild += NoOfChild - 1;
                            ChildAges = ChildAges + 1;
                        }
                    }

                    if (NoOfInfant != 0)
                    {
                        SBPassanger.Append("\"no_of_infants\":  " + NoOfInfant + ",");
                    }

                    if (ChildAges != 0)
                    {
                        SBPassanger.Append("\"no_of_extra_beds\":  " + ChildAges + ",");
                    }

                    string romreference = Roomreference2[p + 1];

                    SBPassanger.Append("\"room_reference\": \"" + romreference + "\",");
                    int a = 0;

                    for (int r = 0; r < RoomNumber; r++)//no of room loop
                    {
                        dtPassengers.DefaultView.RowFilter = "PickUp='" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomTypeId"]) + "' and (RoomNo=" + (p + 1) + ")";
                        dtPassengers.DefaultView.Sort = "IsLead DESC";
                        string SpecialRequest = Convert.ToString(dtPassengers.DefaultView[0]["Message"]).Trim();
                        //for (int t = 0; t < dtPassengers.DefaultView.Count; t++)
                        //{
                        //int childage = Convert.ToInt32(dtPassengers.DefaultView[t]["ChildAge"]);
                        SBPassanger.Append("\"paxes\": [");
                        for (int k = 0; k < dtPassengers.DefaultView.Count; k++)
                        // for (int k = 0; k < RoomNumber.Count; k++)
                        {
                            string FirstName = Convert.ToString(dtPassengers.DefaultView[k]["FirstName"]).Trim();
                            string LastName = Convert.ToString(dtPassengers.DefaultView[k]["LastName"]).Trim();
                            string Title = Convert.ToString(dtPassengers.DefaultView[k]["Prefix"]).Trim().Replace(".", "");
                            int ChAge = Convert.ToInt32(dtPassengers.DefaultView[k]["ChildAge"]);
                            a = k + 1;
                            if (ChAge != 1)
                            {
                                SBPassanger.Append("{");
                                SBPassanger.Append("\"title\": \"" + Title + ".\",");
                                SBPassanger.Append("\"name\": \"" + FirstName + "\",");
                                SBPassanger.Append("\"surname\": \"" + LastName + "\",");
                                if (Convert.ToString(dtPassengers.DefaultView[k]["PaxType"]).Trim().ToLower() == "adult" || ChAge > 11)
                                    SBPassanger.Append("\"type\": \"" + "AD" + "\"");
                                else if (ChAge < 12 && ChAge > 1)
                                {
                                    SBPassanger.Append("\"type\": \"" + "CH" + "\",");
                                    SBPassanger.Append("\"age\": \"" + ChAge + "\"");
                                }
                                if (a == dtPassengers.DefaultView.Count)
                                {
                                    SBPassanger.Append("}");
                                }
                                else
                                {
                                    SBPassanger.Append("},");
                                }
                            }
                            else//Infant
                            {
                                if (a == dtPassengers.DefaultView.Count)
                                {
                                    SBPassanger = SBPassanger.Remove(SBPassanger.Length - 1, 1);
                                }
                            }
                        }
                        int c = p + 1;
                        if (c == room)
                        {
                            SBPassanger.Append("]");
                            SBPassanger.Append("}");
                        }
                        else
                        {
                            SBPassanger.Append("]");
                            SBPassanger.Append("},");
                        }
                    }
                }
                SBPassanger.Append("]");
                SBPassanger.Append("}");
                StringBuilder BookRQ = new StringBuilder();
                BookRQ.Append("{");
                BookRQ.Append("\"search_id\": \"" + sid + "\",");
                BookRQ.Append("\"hotel_code\": \"" + HotelId + "\",");
                BookRQ.Append("\"city_code\": \"" + city_code + "\",");
                BookRQ.Append("\"group_code\": \"" + group_code + "\",");
                BookRQ.Append("\"checkin\": \"" + checkin + "\",");
                BookRQ.Append("\"checkout\": \"" + checkout + "\",");
                BookRQ.Append("\"payment_type\": \"" + "AT_WEB" + "\",");
                BookRQ.Append("\"booking_name\": \"" + AgentReference + "\",");
                BookRQ.Append(SBPassanger.ToString());
                BookRQ.Append("],");
                BookRQ.Append(SBContect.ToString());
                BookRQ.Append("}");
                BookingRQ = BookRQ.ToString();

                return BookingRQ;
            }
            catch (Exception ex)
            {
                _ = _exception.Error(ex);
                return "";
            }
        }

        public string GRNCBookingfetch(DataSet dsCartDetails, DataTable dtPassengers, int i, int j, int UniqueNo, string PreBookRef, int NoOfRoom, string HotelId, string TID, string ContryId, string CITY_CODE)
        {
            var jsonString = "";
            try
            {


            }
            catch (Exception ex)
            {
                _ = _exception.Error(ex);
            }
            return jsonString;



        }

        public string GRNCBookingrefetchResponse(string sid, string Xmlrequest, string Url, string username, string password, int Timeout, string UserId)
        {
            string responseString = "";
            System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
            timer.Start();
            string strOutput = "";
            string responsedata = "";
            try
            {
                string Key = Convert.ToBase64String(Encoding.ASCII.GetBytes(username + ":" + password));
                string reqUrl = Url;
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(reqUrl);
                var postData = Xmlrequest;
                request.Method = "POST";
                //Request.UserAgent = "Mozilla/4.0 (compatible; MSIE 6.0; MS Web Services Client Protocol 1.1.4322.2300)";
                request.Headers.Add("Api-Key", "af3675c6f7819722b572fcd4bcd6598d");
                request.ContentType = "application/json";
                request.Accept = "application/json";
                //Request.Headers.Add("Accept", "application/json");
                //request.Headers.Add("Accept-Encoding", "application/gzip");
                Stream newStream = null;
                byte[] data = Encoding.ASCII.GetBytes(postData);
                request.ContentLength = data.Length;
                newStream = request.GetRequestStream();
                newStream.Write(data, 0, data.Length);


                //byte[] data = Encoding.ASCII.GetBytes(postData);
                //request.ContentLength = data.Length;

                //newStream = request.GetRequestStream();
                //newStream.Write(data, 0, data.Length);
                try
                {
                    Task<string> Response;

                    HttpWebResponse response1 = (HttpWebResponse)request.GetResponse();
                    Stream responseStream = response1.GetResponseStream();
                    //StreamReader Reader = new StreamReader(responseStream, Encoding.Default);
                    // responseStream = new GZipStream(responseStream, CompressionMode.Decompress);

                    //if (response1.ContentEncoding.ToLower().Contains("gzip"))
                    //    responseStream = new System.IO.Compression.GZipStream(responseStream, System.IO.Compression.CompressionMode.Decompress);
                    //else if (response1.ContentEncoding.ToLower().Contains("deflate"))
                    //    responseStream = new System.IO.Compression.DeflateStream(responseStream, System.IO.Compression.CompressionMode.Decompress);
                    StreamReader streamReader = new StreamReader(responseStream);


                    Response = streamReader.ReadToEndAsync();
                    //responsedata = await Response.ConfigureAwait(false);

                    strOutput = Response.Result.ToString();
                    responseStream.Close();
                    streamReader.Close();
                    response1.Close();

                    timer.Stop();
                    if (Convert.ToString(_keySettings.IsLogTrack) == "1")
                    {
                        TimeSpan timeTaken = timer.Elapsed;
                        //ErrorLog(Convert.ToString(System.Web.Configuration.WebConfigurationManager.AppSettings["LogTrackPath"]), sid + "-> Execution Time : " + timer.Elapsed.ToString());

                    }
                }
                catch (WebException webExcp)
                {
                    HttpWebResponse httpResponse = (HttpWebResponse)webExcp.Response;
                    string result = httpResponse.StatusDescription.ToString();
                    string result1 = new StreamReader(webExcp.Response.GetResponseStream()).ReadToEnd();
                    return "";
                }

                //var response = (HttpWebResponse)request.GetResponse();
                //responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();
            }
            catch (Exception ex)
            {
                _ =_exception.Error(ex);

            }
            return strOutput;
        }

        public string GRNCCancellationResponse(string sid, string strxmlRQRS, string url, string Key)
        {
            System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
            timer.Start();
            string strOutput = "";
            string responsedata = "";
            try
            {
                HttpWebRequest Request;
                Request = (HttpWebRequest)HttpWebRequest.Create(url);
                Request.Method = "DELETE";
                //Request.UserAgent = "Mozilla/4.0 (compatible; MSIE 6.0; MS Web Services Client Protocol 1.1.4322.2300)";
                Request.Headers.Add("Api-Key", "af3675c6f7819722b572fcd4bcd6598d");
                Request.ContentType = "application/json";
                Request.Accept = "application/json";
                //Request.Headers.Add("Accept", "application/json");
                //Request.Headers.Add("Accept-Encoding", "application/gzip");
                Stream newStream = null;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls
           | SecurityProtocolType.Tls11
           | SecurityProtocolType.Tls12
           | SecurityProtocolType.Ssl3;
                byte[] data = Encoding.ASCII.GetBytes(strxmlRQRS);
                Request.ContentLength = data.Length;
                newStream = Request.GetRequestStream();
                newStream.Write(data, 0, data.Length);

                try
                {
                    Task<string> Response;

                    HttpWebResponse response = (HttpWebResponse)Request.GetResponse();
                    Stream responseStream = response.GetResponseStream();
                    // if (response.ContentEncoding.ToLower().Contains("gzip"))
                    //     responseStream = new System.IO.Compression.GZipStream(responseStream, System.IO.Compression.CompressionMode.Decompress);
                    //  else if (response.ContentEncoding.ToLower().Contains("deflate"))
                    //      responseStream = new System.IO.Compression.DeflateStream(responseStream, System.IO.Compression.CompressionMode.Decompress);
                    StreamReader streamReader = new StreamReader(responseStream);
                    Response = streamReader.ReadToEndAsync();
                    strOutput = Response.Result.ToString();
                    responseStream.Close();
                    streamReader.Close();
                    response.Close();
                    timer.Stop();
                    if (Convert.ToString(_keySettings.IsLogTrack) == "1")
                    {
                        TimeSpan timeTaken = timer.Elapsed;
                        //ErrorLog(Convert.ToString(System.Web.Configuration.WebConfigurationManager.AppSettings["LogTrackPath"]), sid + "-> Execution Time : " + timer.Elapsed.ToString());
                    }
                }
                catch (WebException webExcp)
                {
                    HttpWebResponse httpResponse = (HttpWebResponse)webExcp.Response;
                    string result = httpResponse.StatusDescription.ToString();
                    string result1 = new StreamReader(webExcp.Response.GetResponseStream()).ReadToEnd();
                    return "";
                }
            }
            catch (Exception ex)
            {
                _= _exception.ErrorNew(ex, _LogAPI.CancelBooking);
            }

            return strOutput;
        }
    }
}
