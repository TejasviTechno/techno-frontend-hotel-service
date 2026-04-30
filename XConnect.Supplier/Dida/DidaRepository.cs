using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using MongoDB.Bson.IO;
using XConnect.CommonUtilities;
using XConnect.CommonUtilities.Interface;
using XConnect.ErrorLogHandler.Interface;
using XConnect.Expedia;
using XConnect.Markup.Interface;
using XConnect.Models;
using XConnect.Models.Hotel;
using XConnect.Supplier.Dida;
using XConnect.Supplier.Dida.Interface;
using static Azure.Core.HttpHeader;
using static XConnect.Models.ErrorLog;
using static XConnect.Supplier.Booking.Com.preebookrs;
using Options = XConnect.Models.Options;

namespace Xconnect.Expedia
{
    public class DidaRepository : IDida
    {
        private readonly ICommonMethod _commonMethod;
        private readonly IException _exception;
        private readonly IMarkup _markup;
        private readonly ISupplierRes _supplierRes;
        private readonly IMemoryCache _cache;
        private readonly KeySettings _keySettings;
        private readonly SupplierConfiguration _supplierConfig;//Added on 26-FEB-26



        public DidaRepository(ICommonMethod commonMethod, IMarkup markup, ISupplierRes supplierRes, IMemoryCache cache, IOptions<KeySettings> keySettings, IException exception, IOptions<SupplierConfiguration> supplierConfig)
        {
            _commonMethod = commonMethod;
            _markup = markup;
            _supplierRes = supplierRes;
            _cache = cache;
            _keySettings = keySettings.Value;
            _exception = exception;
            _supplierConfig = supplierConfig.Value;
        }
        public void GetDidaRequestOnly_Async(DataRow[] commonCode, List<StarRating> listRating, string SupplierName, HotelSearchParameter SearchPara, List<SupplierDetails> DidaDetails, List<SupplierSearchConfig> DidaSearchConfig, DataTable dtPreferredDataHotel, ref ConcurrentBag<HotelX> AllTask, string XMLCity, string XMLCountry, string Xmlnationality, int Hit, string AgentCode, DataTable dtROE, TokenData tokenData)
        {
            try
            {
                double CurrencyFactorDida = 1;
                string DidaCurrency = Convert.ToString(DidaDetails[0].CurrencyShortName);
                //DataRow[] drDida = dtPreferredDataHotel.Select("ServiceName='Dida'");
                DataRow[] drDida = dtPreferredDataHotel.Select("ServiceName='" + SupplierName + "'");
                string Ids = string.Empty;
                string xmlbase = "";
                _commonMethod.GetPrefferedHotelCodes(ref Ids, ref xmlbase, ref DidaSearchConfig, SupplierName, dtPreferredDataHotel, SearchPara.IsSearchFor);
                if (drDida.Count() > 0)
                {
                    var paramValues1 = String.Join(",", drDida.Select(x => String.Join(",", Convert.ToString(x.ItemArray[0]))));
                    Ids = Convert.ToString(paramValues1);
                }
                if (SearchPara.IsSearchFor.ToLower().IndexOf("preferred") >= 0 && drDida.Count() == 0)
                {
                    DidaSearchConfig = null;
                }
                #region "Dida"
                if (DidaSearchConfig != null)
                {
                    for (int k = 0; k < DidaSearchConfig.Count; k++)
                    {
                        bool DebugMode = (Convert.ToInt32(DidaSearchConfig[k].DebugMode) == 0 ? false : true);
                        string Rating = _commonMethod.GetStarRatingString(SearchPara);

                        if (Convert.ToInt32(DidaSearchConfig[k].CityID) == 0 || Convert.ToInt32(DidaSearchConfig[k].CityID) == SearchPara.CityID)//All City means 0
                        {
                            int NoOfRecords = Convert.ToInt32(DidaSearchConfig[k].Noofrecords);
                            DataTable[] splittedtables = commonCode.AsEnumerable()
                                                      .Select((row, index) => new { row, index })
                                                      .GroupBy(x => x.index / NoOfRecords)  // integer division, the fractional part is truncated
                                                      .Select(g => g.Select(x => x.row).CopyToDataTable())
                                                      .ToArray();
                            for (int a = 0; a < splittedtables.Length; a++)
                            {
                                var paramValues1 = String.Join(",", splittedtables[a].Select().Select(x => String.Join(",", Convert.ToString(x.ItemArray[0]))));
                                string IdsForSearch = Convert.ToString(paramValues1);
                                HotelX xhotels = new HotelX();
                                xhotels.Supplier = SupplierName;
                                xhotels.DebugeMode = DebugMode;
                                xhotels.SearchTimeOut = Convert.ToInt32(DidaSearchConfig[k].HotelSearchTimeOut);
                                //if (Hit > 0)
                                xhotels.ProcessTimeout = Convert.ToInt32(DidaSearchConfig[k].HotelSearchTimeOut2);

                                xhotels.URL = Convert.ToString(DidaDetails[0].WebSiteURL) + _supplierConfig.Dida.DidaHotel_Action;
                                xhotels.Action = "";
                                xhotels.DebugeMode = DebugMode;
                                //xhotels.CurrencyCode = Convert.ToString(_configuration["AppSettings:DidaCurrency"]);
                                xhotels.CurrencyCode = Convert.ToString(DidaDetails[0].CurrencyShortName);
                                xhotels.CurrencyFactor = CurrencyFactorDida;
                                if (Convert.ToString(DidaCurrency).ToLower() != tokenData.SiteCurrency.ToLower())//need to update dynamic

                                {
                                    CurrencyFactorDida = _commonMethod.GetROEByCurrency(Convert.ToString(DidaCurrency), dtROE);
                                }
                                xhotels.XmlNationality = Xmlnationality;
                                xhotels.XmlCity = XMLCity;
                                xhotels.XmlCountry = XMLCountry;
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
                                    objrq.Request = XMLDidaRequestOnly_Async(Convert.ToInt32(DidaSearchConfig[k].HotelSearchTimeOut), SearchPara, Convert.ToString(DidaDetails[0].UserName), Convert.ToString(DidaDetails[0].Password), IdsForSearch, XMLCity, XMLCountry, Xmlnationality, xhotels.CurrencyCode);
                                    objrq.Response = ""; RQRS.Add(objrq);
                                }
                                xhotels.RQRS = RQRS;
                                xhotels.ReturnRooms = 5; // DidaDetails[0].ReturnRooms; //140225
                                xhotels.TechName = "Dida"; // DidaDetails[0].TechName; ;//140225
                                #endregion
                                AllTask.Add(xhotels);
                            }
                        }
                    }
                }
                #endregion
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
            }
        }

        public void GetDidaRequestOnly_Async_new(DataRow[] commonCode, List<StarRating> listRating, HotelSearchParameter SearchPara, List<SupplierDetails> lstDidaDetails, List<SupplierSearchConfig> lstDidaSearchConfig, DataTable dtPreferredDataHotel, ref ConcurrentBag<HotelX> AllTask, int Hit, string AgentCode, DataTable dtROE, TokenData tokenData, DataSet dtXMLCity)
        {
            try
            {
                List<StarRating> lstRating = new List<StarRating>();
                lstRating = _commonMethod.GetStarRatingStringList_xtg(SearchPara);

                List<HotelCodeByCity> DtHotelDAta = HtmlExtensions.ToList<HotelCodeByCity>(commonCode.CopyToDataTable());
                int ratingcount = lstRating.Count;
                int hotelcount = DtHotelDAta.Count;

                foreach (SupplierDetails DidaDetails in lstDidaDetails)
                {
                    try
                    {
                        int SupplierID = DidaDetails.SupplierID;
                        string Supplier = DidaDetails.SupplierName;

                        if (lstDidaSearchConfig.Count > 0)
                        {
                            List<SupplierSearchConfig> DidaSearchConfig = lstDidaSearchConfig
                                  .Where(x => x != null && x.SupplierID == SupplierID)
                                  .ToList();

                            if (DidaSearchConfig != null && DidaSearchConfig.Count > 0)
                            {

                                List<HotelCodeByCity> filter = new List<HotelCodeByCity>();
                                filter = _commonMethod.FilterWithOutCity_Code(ratingcount, hotelcount, DtHotelDAta, lstRating, Supplier);

                                if (filter.Count > 0)
                                {
                                    DateTime dt = DateTime.ParseExact(SearchPara.CheckInDate, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                                    double days = (dt - Convert.ToDateTime(DateTime.Now)).Days;

                                    if (SearchPara.NoofNights < 28 && days < 364)
                                    {
                                        DataForSearch dataForSearch = _commonMethod.GetXmlDataForSearchTechwise(SearchPara.Nationality, dtXMLCity, Supplier, DidaDetails.TechName);
                                        if (dataForSearch.XmlNationality != "")
                                        {
                                            if (DidaCheckRoomPax(SearchPara) == true)
                                            {
                                                double CurrencyFactorDida = 1;
                                                string DidaCurrency = Convert.ToString(DidaDetails.CurrencyShortName);

                                                if (Convert.ToString(DidaCurrency).ToLower() != tokenData.SiteCurrency.ToLower())//need to update dynamic
                                                {
                                                    CurrencyFactorDida = _commonMethod.GetROEByCurrency(Convert.ToString(DidaCurrency), dtROE);
                                                }
                                                //DataRow[] drDida = dtPreferredDataHotel.Select("ServiceName='Dida'");
                                                DataRow[] drDida = dtPreferredDataHotel.Select("ServiceName='" + Supplier + "'");
                                                string Ids = string.Empty;
                                                string xmlbase = "";
                                                _commonMethod.GetPrefferedHotelCodes(ref Ids, ref xmlbase, ref DidaSearchConfig, Supplier, dtPreferredDataHotel, SearchPara.IsSearchFor);
                                                if (drDida.Count() > 0)
                                                {
                                                    var paramValues1 = String.Join(",", drDida.Select(x => String.Join(",", Convert.ToString(x.ItemArray[0]))));
                                                    Ids = Convert.ToString(paramValues1);
                                                }
                                                if (SearchPara.IsSearchFor.ToLower().IndexOf("preferred") >= 0 && drDida.Count() == 0)
                                                {
                                                    DidaSearchConfig = null;
                                                }

                                                #region "Dida"
                                                if (DidaSearchConfig != null)
                                                {
                                                    for (int k = 0; k < DidaSearchConfig.Count; k++)
                                                    {
                                                        bool DebugMode = (Convert.ToInt32(DidaSearchConfig[k].DebugMode) == 0 ? false : true);
                                                        string Rating = _commonMethod.GetStarRatingString(SearchPara);

                                                        if (Convert.ToInt32(DidaSearchConfig[k].CityID) == 0 || Convert.ToInt32(DidaSearchConfig[k].CityID) == SearchPara.CityID)//All City means 0
                                                        {
                                                            int NoOfRecord = Convert.ToInt32(DidaSearchConfig[k].Noofrecords);
                                                            double count = Convert.ToDouble(filter.Count);
                                                            int NoOfDiv = Convert.ToInt32(Math.Ceiling(count / Convert.ToDouble(NoOfRecord)));
                                                            List<List<XMLID_Search>> lstxml = _commonMethod.GetXMLIDString(NoOfDiv, NoOfRecord, count, filter);


                                                            for (int a = 0; a < NoOfDiv; a++)
                                                            {
                                                                string IdsForSearch = Convert.ToString(_commonMethod.GetHotelCodeByCOmma(lstxml[a]));
                                                                if (!string.IsNullOrEmpty(IdsForSearch))
                                                                {
                                                                    HotelX xhotels = new HotelX();
                                                                    xhotels.Supplier = Supplier;
                                                                    xhotels.DebugeMode = DebugMode;
                                                                    xhotels.SearchTimeOut = Convert.ToInt32(DidaSearchConfig[k].HotelSearchTimeOut);
                                                                    //if (Hit > 0)
                                                                    xhotels.ProcessTimeout = Convert.ToInt32(DidaSearchConfig[k].HotelSearchTimeOut2);

                                                                    //xhotels.URL = Convert.ToString(DidaDetails.WebSiteURL) + _supplierConfig.DidaInfo.DidaHotel_Action;

                                                                    if (_keySettings.SupplierProxy == "0")
                                                                        xhotels.URL = Convert.ToString(DidaDetails.WebSiteURL) + _supplierConfig.Dida.DidaHotel_Action;
                                                                    else
                                                                        xhotels.URL = Convert.ToString(DidaDetails.WebSiteURLforAPI) + _supplierConfig.Dida.DidaHotel_Action;


                                                                    xhotels.Action = "";
                                                                    xhotels.DebugeMode = DebugMode;
                                                                    //xhotels.CurrencyCode = Convert.ToString(_configuration["AppSettings:DidaCurrency"]);
                                                                    xhotels.CurrencyCode = Convert.ToString(DidaDetails.CurrencyShortName);
                                                                    xhotels.CurrencyFactor = CurrencyFactorDida;
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
                                                                        objrq.Request = XMLDidaRequestOnly_Async(Convert.ToInt32(DidaSearchConfig[k].HotelSearchTimeOut), SearchPara, Convert.ToString(DidaDetails.UserName), Convert.ToString(DidaDetails.Password), IdsForSearch, dataForSearch.xmlCity, dataForSearch.xmlCountry, dataForSearch.XmlNationality, xhotels.CurrencyCode);
                                                                        objrq.Response = ""; RQRS.Add(objrq);
                                                                    }
                                                                    xhotels.RQRS = RQRS;
                                                                    xhotels.ReturnRooms = Convert.ToInt32(DidaDetails.ReturnRooms); //140225
                                                                    xhotels.TechName = DidaDetails.TechName; ;//140225
                                                                    #endregion
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
        public bool DidaCheckRoomPax(HotelSearchParameter objSearPera)
        {
            bool IsSame = true;
            int NoOfAdt = objSearPera.Rooms[0].NoofAdults;
            int NoOfChd = objSearPera.Rooms[0].NoofChild;
            int Age1 = objSearPera.Rooms[0].Child1Age;
            int Age2 = objSearPera.Rooms[0].Child2Age;
            for (int i = 0; i < objSearPera.Rooms.Count; i++)
            {
                if (NoOfAdt != objSearPera.Rooms[i].NoofAdults || NoOfChd != objSearPera.Rooms[i].NoofChild || Age1 != objSearPera.Rooms[i].Child1Age || Age2 != objSearPera.Rooms[i].Child2Age)
                    IsSame = false;
            }
            return IsSame;
        }
        public string XMLDidaRequestOnly_Async(Int32 SearchTimeOut, HotelSearchParameter SearchParam, string DidaUser_Name, string DidaPassword, string HotelId, string CityCode, string CountryCode, string Nationality, string Currency)
        {
            try
            {
                #region Data
                DataSet dsTemp = new DataSet();
                #endregion

                return DidaPriceonlyRequest(SearchParam, CityCode, CountryCode, DidaUser_Name, DidaPassword, HotelId, Nationality, Currency);

            }
            catch (Exception ex)
            {
                _exception.Error(ex);
                return "";
            }
        }
        public string DidaPriceonlyRequest(HotelSearchParameter SearchParam, string DidaCityCode, string CountryCode, string UserName, string Password, string HotelId, string Nationality, string Currency)
        {
            string Req = "";
            try
            {
                string Ids = HotelId;
                string checkin = (SearchParam.CheckInDate).Substring(6, 4) + "-" + (SearchParam.CheckInDate).Substring(3, 2) + "-" + (SearchParam.CheckInDate).Substring(0, 2);
                string checkout = (SearchParam.CheckOutDate).Substring(6, 4) + "-" + (SearchParam.CheckOutDate).Substring(3, 2) + "-" + (SearchParam.CheckOutDate).Substring(0, 2);
                StringBuilder SearchRQ = new StringBuilder();
                SearchRQ.Append("{\"Header\": {");
                SearchRQ.Append("\"ClientID\": \"" + UserName + "\",");
                SearchRQ.Append("\"LicenseKey\": \"" + Password + "\"},");
                SearchRQ.Append("\"HotelIDList\": [" + Ids + "],");
                SearchRQ.Append("\"CheckInDate\": \"" + checkin + "\",");
                SearchRQ.Append("\"CheckOutDate\": \"" + checkout + "\",");
                SearchRQ.Append("\"IsRealTime\": {");
                SearchRQ.Append("\"Value\": true,");
                SearchRQ.Append("\"RoomCount\": " + SearchParam.Rooms.Count + "},");
                SearchRQ.Append("\"RealTimeOccupancy\": {");
                SearchRQ.Append("\"ChildCount\": " + SearchParam.Rooms[0].NoofChild + ",");
                SearchRQ.Append("\"AdultCount\": " + SearchParam.Rooms[0].NoofAdults + ",");
                if (SearchParam.Rooms[0].NoofChild == 0)
                    SearchRQ.Append("\"ChildAgeDetails\": []},");
                if (SearchParam.Rooms[0].NoofChild == 1)
                    SearchRQ.Append("\"ChildAgeDetails\": [" + SearchParam.Rooms[0].Child1Age + "]},");
                if (SearchParam.Rooms[0].NoofChild == 2)
                    SearchRQ.Append("\"ChildAgeDetails\": [" + SearchParam.Rooms[0].Child1Age + "," + SearchParam.Rooms[0].Child2Age + "]},");
                SearchRQ.Append("\"Nationality\": \"" + Nationality + "\",");
                //SearchRQ.Append("\"Currency\": \"" + Convert.ToString(_configuration["AppSettings:DidaCurrency"]) + "\"}");
                SearchRQ.Append("\"Currency\": \"" + Convert.ToString(Currency) + "\"}");

                Req = SearchRQ.ToString();

                return Req;
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
                return "";
            }
        }

        public async Task<List<Options>> XMLDidaPriceOnly_Async_xml(HotelX Item, CommonSearch commonSearch, string ResponseData, HotelSearchParameter SearchParam, string SupplierName, string XMLCITY, string XMLCountry, string Currency, double CurrencyFactor, string XmlNationality, Int32 processTimeout, string ResProccessTimerLogActive, string ResProccessTimeValueforSaveLog, DataTable dtROE)
        {
            System.Diagnostics.Stopwatch timerroomx = new System.Diagnostics.Stopwatch();
            timerroomx.Start();

            List<Options> options = new List<Options>();
            DataTable DtHotel = _commonMethod.GenerateDataTableNewV2();
            try
            {
                #region Response
                if (ResponseData == string.Empty || ResponseData.Contains("ErrorTxt"))
                {
                    Item.ResponseType = "NoCount";
                    return options;
                }
                //ResponseData = Newtonsoft.Json.JsonConvert.DeserializeXmlNode(ResponseData).InnerXml;
                //ResponseData = Newtonsoft.Json.JsonConvert.DeserializeXmlNode(ResponseData, "root").InnerXml; //#04-NOV-2024 K2
                // ResponseData = Newtonsoft.Json.JsonConvert.DeserializeXmlNode(ResponseData, "root").InnerXml; //#04-NOV-2024 K2


                string Provider = SupplierName;
                DataSet dsDidaResponse = _commonMethod.GetDataSetFromXMLString(ResponseData);
                if (dsDidaResponse == null || dsDidaResponse.Tables["RatePlanList"] == null)
                {
                    Item.ResponseType = "NoCount";
                    return options;
                }
                #endregion

                for (int ii = 0; ii < dsDidaResponse.Tables["HotelList"].Rows.Count; ii++)
                {
                    Item.ResponseType = "Success";

                    #region "When not Need Room"  
                    // add hotel code from price only and room code blank here
                    if (SearchParam.Filters.IsShowRooms == 0)
                    {
                        #region "Hotel Code Block "
                        double TotalCharges = 0;
                        DataRow drXmlHotel = DtHotel.NewRow();
                        string HotelStar = "";
                        HotelStar = _commonMethod.Star_Rating(HotelStar);
                        string InternalHotelId = Convert.ToString(0);
                        drXmlHotel["XMLHotelCode"] = Convert.ToString(dsDidaResponse.Tables["HotelList"].Rows[ii]["HotelId"]);
                        drXmlHotel["HotelName"] = "";
                        drXmlHotel["SupplierName"] = SupplierName;
                        drXmlHotel["OfficeID"] = 0;
                        drXmlHotel["ContractID"] = 0;
                        drXmlHotel["Rating"] = Convert.ToInt32(0);
                        drXmlHotel["XmlCity"] = XMLCITY;
                        drXmlHotel["IsShowSupplier"] = false;
                        drXmlHotel["BookingStatus"] = "Available";
                        TotalCharges = ((Convert.ToDouble(dsDidaResponse.Tables["RatePlanList"].Rows[ii]["TotalPrice"]) * CurrencyFactor) * SearchParam.Rooms.Count) * SearchParam.NoofNights;
                        drXmlHotel["FinalSellingPrice"] = TotalCharges;
                        drXmlHotel["SellingPrice"] = TotalCharges;
                        drXmlHotel["SupplierCurrency"] = Currency;
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
                            double MinPrice = TotalCharges;
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
                        #region "Hotel Code Block "
                        double TotalCharges = 0;
                        double FinalMinPrice = 0;

                        DataRow drXmlHotel = DtHotel.NewRow();
                        string HotelStar = "";
                        HotelStar = _commonMethod.Star_Rating(HotelStar);
                        string InternalHotelId = Convert.ToString(0);
                        drXmlHotel["XMLHotelCode"] = Convert.ToString(dsDidaResponse.Tables["HotelList"].Rows[ii]["HotelId"]);
                        drXmlHotel["HotelName"] = "";
                        drXmlHotel["SupplierName"] = SupplierName;
                        drXmlHotel["OfficeID"] = 0;
                        drXmlHotel["ContractID"] = 0;
                        drXmlHotel["Rating"] = Convert.ToInt32(0);
                        drXmlHotel["XmlCity"] = XMLCITY;
                        drXmlHotel["IsShowSupplier"] = false;
                        drXmlHotel["BookingStatus"] = "Available";
                        TotalCharges = ((Convert.ToDouble(dsDidaResponse.Tables["RatePlanList"].Rows[ii]["TotalPrice"]) * CurrencyFactor) * SearchParam.Rooms.Count) * SearchParam.NoofNights;
                        drXmlHotel["FinalSellingPrice"] = TotalCharges;
                        drXmlHotel["SellingPrice"] = TotalCharges;
                        drXmlHotel["SupplierCurrency"] = Currency;
                        DtHotel.Rows.Add(drXmlHotel);
                        FinalMinPrice = TotalCharges;
                        #endregion

                        #region "GetRooms for this hotel"
                        bool willAddOrNot = true;
                        Int64 RaynaID = 0;
                        Int64 GiataId = 0;
                        int RatingHotel = 0;
                        Int64 ownsystemhotelId = 0;
                        double MinPrice = 1;

                        _markup.GetHotelDataCommon(ref MinPrice, ref willAddOrNot, ref ownsystemhotelId, ref RaynaID, ref GiataId, ref RatingHotel, ref drXmlHotel, commonSearch.AgentId, commonSearch.TariffID, commonSearch.dsAgentDetails, commonSearch.supplierGiataMaster, SearchParam, commonSearch.dtCountryCityMarkup, commonSearch.B2CMarkup, commonSearch.IsParentId, commonSearch.SubAgentMarkup, commonSearch.SearchByHotel, commonSearch.HotelIDS);
                        if (willAddOrNot)
                        {
                            //DataTable dtInrCurrency = Common.GetCountryCurrency(Currency);
                            double CurrencyFactorDida = _commonMethod.GetROEByCurrency(Currency, dtROE);// Convert.ToDouble(dtInrCurrency.Rows[0]["AmtToExch"]);

                            double SubAgentMarkup = 0;
                            int IsParentId = 1; int agentid = 0;
                            if (commonSearch.dtAgentDetail != null)
                            {
                                IsParentId = Convert.ToInt32(commonSearch.dtAgentDetail.Rows[0]["IsParentId"]);
                                SubAgentMarkup = Convert.ToDouble(commonSearch.dtAgentDetail.Rows[0]["B2CMarkupPer"]);
                                agentid = Convert.ToInt32(commonSearch.dtAgentDetail.Rows[0]["AgentID"]);

                            }
                            double AgentPercentage = 0.0;
                            double AgentMarkupAmount = 0.0;
                            Boolean isPerce = true;
                            Boolean blnIsShowSupplier = false;
                            int hotelRating = 0;
                            _markup.getAgetMarkupSetting(SupplierName, Convert.ToString(GiataId), commonSearch.dtSuplierMarkup, commonSearch.dtMarkupDetails, ref AgentPercentage, ref AgentMarkupAmount, ref blnIsShowSupplier, ref isPerce, agentid, hotelRating, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), SearchParam.CheckInDate);

                            double AgentCompanyPercentage = Convert.ToDouble(SearchParam.AgentCompanyPercentage);
                            bool IsB2C = Convert.ToBoolean(Convert.ToByte(SearchParam.IsB2BOrB2C));
                            double B2CMarkup = (IsB2C == false ? AgentCompanyPercentage : XMLHotelBookingCommon.B2CMarkup);
                            if (IsB2C == true)
                                AgentPercentage = 0;

                            DataRow[] DidaDetails = null;
                            DataRow[] DidaSearchConfig = null;
                            if (commonSearch.dtSuplierMarkup != null & commonSearch.dtSuplierMarkup != null && commonSearch?.dtSuplierMarkup?.Rows.Count > 0)
                            {
                                DidaDetails = commonSearch.dtSuplierMarkup.Select("SupplierName='Dida'");
                            }
                            if (commonSearch?.dtSearchConfig != null & commonSearch?.dtSearchConfig != null && commonSearch?.dtSearchConfig?.Rows.Count > 0)
                            {
                                if (SearchParam != null)
                                {
                                    if (DidaDetails?.GetLength(0) > 0)
                                        DidaSearchConfig = commonSearch.dtSearchConfig.Select("SupplierID=" + Convert.ToString(DidaDetails[0]["SupplierID"]) + " and (CityID=" + SearchParam.CityID + " or CityID=0) and (Stars=0 OR (Stars>=" + SearchParam.StarStart + " and Stars<=" + SearchParam.StarEnd + "))");
                                }
                            }

                            string checkin = (SearchParam.CheckInDate).Substring(6, 4) + "-" + (SearchParam.CheckInDate).Substring(3, 2) + "-" + (SearchParam.CheckInDate).Substring(0, 2);
                            string checkout = (SearchParam.CheckOutDate).Substring(6, 4) + "-" + (SearchParam.CheckOutDate).Substring(3, 2) + "-" + (SearchParam.CheckOutDate).Substring(0, 2);

                            DataTable dtDidaHotelRoom = _commonMethod.GenerateRoomtypeDataTableNewV2();
                            DataTable dtDidaCancellation = _commonMethod.GenerateCancellationDataTableNewV2();
                            DataTable dtDidaHotelRoomRateBreackUp = _commonMethod.GenerateoomRateBreackUpDataTableNewV2();
                            int Rooms = SearchParam.Rooms.Count;
                            int CutOffDays = XMLHotelBookingCommon.CutOffDays;
                            CutOffDays = _commonMethod.GetCutOffDays(commonSearch.dtSuplierMarkup, Provider);
                            DataSet DidaResponse = dsDidaResponse;
                            DataRow[] drDidaResponse = dsDidaResponse.Tables["RatePlanList"].Select("HotelList_Id=" + Convert.ToString(dsDidaResponse.Tables["HotelList"].Rows[ii]["HotelList_Id"]));
                            if (SearchParam.Filters.IsShowRooms == 1)
                            {
                                for (int i = 0; i < drDidaResponse.Length; i++)
                                {
                                    string GUID = Guid.NewGuid().ToString();
                                    int BreakfastType = Convert.ToInt32(drDidaResponse[i]["BreakfastType"]);
                                    int RatePlanList_Id = Convert.ToInt32(drDidaResponse[i]["RatePlanList_Id"]);
                                    int StandardOccupancy = Convert.ToInt32(drDidaResponse[i]["StandardOccupancy"]);
                                    int MaxOccupancy = Convert.ToInt32(drDidaResponse[i]["MaxOccupancy"]);
                                    string MealName = BreakfastType == 2 ? "With Breakfast" : "No Breakfast";
                                    string RoomTypeId = Convert.ToString(drDidaResponse[i]["RatePlanId"]) + "$$$" + XmlNationality;
                                    int BedTypeId = Convert.ToInt32(drDidaResponse[i]["BedType"]);
                                    string RoomName = Convert.ToString(drDidaResponse[i]["RatePlanName"]);
                                    int InventoryCount = Convert.ToInt32(drDidaResponse[i]["InventoryCount"]);
                                    if (InventoryCount < SearchParam.Rooms.Count || MaxOccupancy < (SearchParam.Rooms[0].NoofAdults + SearchParam.Rooms[0].NoofChild))
                                        continue;

                                    DataRow[] drRatePlanCancellationPolicyList_date = DidaResponse.Tables["RatePlanCancellationPolicyList"].Select("RatePlanList_Id='" + RatePlanList_Id + "'");
                                    string CDate = Convert.ToString(drRatePlanCancellationPolicyList_date[0]["FromDate"]);
                                    RoomTypeId = RoomTypeId + "$$$" + CDate;

                                    string BedType = "";// !!!!!! Note: Read XML File For Get BedType !!!!!!
                                    BedType = DidaRedXMLFileData("BedType", "ID", Convert.ToString(BedTypeId), "Name", Item.TechName);
                                    if (BedType != "")
                                    {
                                        RoomName = RoomName + " [BedType: " + BedType + "]";
                                    }
                                    #region/added for excluded info 26-Dec-2024
                                    string EXINFO = "";
                                    try
                                    {
                                        DataRow[] Excludedprice = DidaResponse.Tables["ExcludedFeeList"].Select("RatePlanList_Id='" + RatePlanList_Id + "'");
                                        for (int R = 0; R < Excludedprice.Length; R++)
                                        {
                                            string Excurrency = Convert.ToString(Excludedprice[R]["Currency"]);
                                            double ExAmpount = Convert.ToDouble(Excludedprice[R]["Amount"]);
                                            string ExFeeTypeName = Convert.ToString(Excludedprice[R]["FeeTypeName"]);
                                            string EXinfo = ("<span style=\"font-size: large;\">Info:" + (R + 1) + "</br> Amount: " + ExAmpount) + "</span>,</br> <span style=\"font-size: large;\"> FeeTypeName: " + ExFeeTypeName + "</span>" + "</span>,</br> <span style=\"font-size: large;\"> Currency: " + Excurrency + "</span></br>";
                                            EXINFO += EXinfo;
                                        }
                                    }
                                    catch (Exception)
                                    {

                                    }

                                    #endregion
                                    string RoomTypeName = Rooms + " X " + RoomName;
                                    double TotalPrice = (Convert.ToDouble(drDidaResponse[i]["TotalPrice"]) * CurrencyFactorDida) * Rooms;
                                    double NightRate = TotalPrice / SearchParam.NoofNights;
                                    //double ChargeAmount = (Convert.ToDouble(DidaResponse.Tables["RatePlanCancellationPolicyList"].Rows[i]["Amount"]) * CurrencyFactorDida) * Rooms;
                                    //DateTime DeadLineDate = Convert.ToDateTime(DidaResponse.Tables["RatePlanCancellationPolicyList"].Rows[i]["FromDate"]);
                                    double PriceWithoutSupplement = 0;
                                    double Supplement = 0;
                                    if (DidaResponse.Tables["RatePlanList"].Columns.Contains("PriceWithoutSupplement"))
                                    {
                                        double WithoutSupplement = (Convert.ToString(drDidaResponse[i]["PriceWithoutSupplement"])) == "" ? 0 : (Convert.ToDouble(drDidaResponse[i]["PriceWithoutSupplement"]));
                                        double SupplementOnly = (Convert.ToString(drDidaResponse[i]["Supplement"])) == "" ? 0 : (Convert.ToDouble(drDidaResponse[i]["Supplement"]));
                                        PriceWithoutSupplement = (WithoutSupplement * Rooms) * CurrencyFactorDida;
                                        Supplement = (SupplementOnly * Rooms) * CurrencyFactorDida;
                                    }
                                    #region RateBreakup
                                    //--MarkUp  
                                    double VatOnRateBrackUp = 0;
                                    NightRate = _markup.ProcessHotelRateBreakupPrice_WithMarkup(1, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), NightRate, B2CMarkup, IsParentId, isPerce, AgentPercentage, AgentMarkupAmount, SubAgentMarkup, ref VatOnRateBrackUp);
                                    for (int r = 0; r < SearchParam.NoofNights; r++)
                                    {
                                        DateTime FromDate = Convert.ToDateTime(checkin).Date.AddDays(r);
                                        DataRow drDidaRateBreackUp = dtDidaHotelRoomRateBreackUp.NewRow();
                                        drDidaRateBreackUp["SupplierName"] = SupplierName;
                                        drDidaRateBreackUp["OwnsystemHotelId"] = ownsystemhotelId;
                                        drDidaRateBreackUp["ContractID"] = 0;
                                        drDidaRateBreackUp["RoomNo"] = 1;
                                        drDidaRateBreackUp["RoomTypeId"] = RoomTypeId;
                                        drDidaRateBreackUp["Meal"] = MealName;
                                        string rateWeekColumn = Convert.ToString(FromDate.Date.DayOfWeek).Substring(0, 3);
                                        drDidaRateBreackUp["DateOfWeek"] = rateWeekColumn;
                                        drDidaRateBreackUp["MealId"] = BreakfastType;
                                        drDidaRateBreackUp["Rate"] = NightRate;
                                        drDidaRateBreackUp["OrderNo"] = 1;
                                        drDidaRateBreackUp["RateType"] = "A Rate";
                                        drDidaRateBreackUp["RoomTypeName"] = RoomTypeName;
                                        drDidaRateBreackUp["DateOfWeek"] = rateWeekColumn;
                                        drDidaRateBreackUp["Date"] = FromDate;
                                        drDidaRateBreackUp["BookingStatus"] = "Available";
                                        drDidaRateBreackUp["BoardTypeID"] = "";
                                        drDidaRateBreackUp["VatdiffAmount"] = VatOnRateBrackUp;
                                        dtDidaHotelRoomRateBreackUp.Rows.Add(drDidaRateBreackUp);
                                    }
                                    #endregion

                                    #region Room
                                    DataRow drDidaHotelRoom = dtDidaHotelRoom.NewRow();
                                    drDidaHotelRoom["OwnsystemHotelId"] = ownsystemhotelId;
                                    drDidaHotelRoom["ContractID"] = 0;
                                    drDidaHotelRoom["UniqueNo"] = ownsystemhotelId;
                                    drDidaHotelRoom["RoomSequenceNo"] = "";
                                    drDidaHotelRoom["MealName"] = MealName;
                                    drDidaHotelRoom["allocationDetails"] = "";
                                    drDidaHotelRoom["minStay"] = "";
                                    drDidaHotelRoom["DateApplyMinStay"] = "";
                                    drDidaHotelRoom["RoomNo"] = 1;
                                    drDidaHotelRoom["EssentialInformation"] = EXINFO; // (BedType == "" ? "" : "<span style=\"font-size: large;\"> BedType: " + BedType) + "</span>,</br> <span style=\"font-size: large;\"> InventoryCount: " + InventoryCount + "</span>";
                                    drDidaHotelRoom["MealId"] = BreakfastType;
                                    drDidaHotelRoom["RoomTypeName"] = RoomTypeName;
                                    drDidaHotelRoom["RoomTypeId"] = RoomTypeId;
                                    drDidaHotelRoom["BookingStatus"] = "Available";
                                    drDidaHotelRoom["AdultsOccupancy"] = SearchParam.Rooms[0].NoofAdults;
                                    drDidaHotelRoom["ChildOccupancy"] = SearchParam.Rooms[0].NoofChild;
                                    drDidaHotelRoom["MaxExtrabed"] = 0;
                                    drDidaHotelRoom["RoomTypeCode"] = RoomTypeId;
                                    drDidaHotelRoom["BoardTypeID"] = "";
                                    drDidaHotelRoom["CompanyBuyingPrice"] = TotalPrice;
                                    drDidaHotelRoom["OfferDescription"] = "";
                                    drDidaHotelRoom["offerCode"] = "";
                                    if (PriceWithoutSupplement > TotalPrice)
                                    {
                                        VatOnRateBrackUp = 0;
                                        PriceWithoutSupplement = _markup.ProcessHotelRateBreakupPrice_WithMarkup(1, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), PriceWithoutSupplement, B2CMarkup, IsParentId, isPerce, AgentPercentage, AgentMarkupAmount, SubAgentMarkup, ref VatOnRateBrackUp);
                                        drDidaHotelRoom["GrossWitohutDiscount"] = PriceWithoutSupplement;
                                    }
                                    _markup.ProcessHotelRoom_WithMarkup(CurrencyFactorDida, Currency, false, true, SearchParam.NoofNights, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), SearchParam.CheckInDate, SearchParam.CheckOutDate, ref TotalPrice, ref drDidaHotelRoom, isPerce, AgentPercentage, AgentMarkupAmount, SubAgentMarkup, B2CMarkup, IsParentId);

                                    drDidaHotelRoom["SupplierName"] = SupplierName;
                                    dtDidaHotelRoom.Rows.Add(drDidaHotelRoom);
                                    #endregion

                                    #region CancelltionPolicy
                                    DateTime curDate = _commonMethod.GetTimeZoneDate(SearchParam.CurrentDate);
                                    #region Vat
                                    double VatOnPolicy = 0;
                                    double ChargeAmount = 0;
                                    DateTime DeadLineDate = DateTime.Now;
                                    DataRow[] drRatePlanCancellationPolicyList = DidaResponse.Tables["RatePlanCancellationPolicyList"].Select("RatePlanList_Id='" + RatePlanList_Id + "'");
                                    for (int c = 0; c < drRatePlanCancellationPolicyList.Length; c++)
                                    {
                                        ChargeAmount = (Convert.ToDouble(drRatePlanCancellationPolicyList[c]["Amount"]) * CurrencyFactorDida) * Rooms;
                                        DeadLineDate = Convert.ToDateTime(drRatePlanCancellationPolicyList[c]["FromDate"]);
                                        double cancellationAmountMain = ChargeAmount;
                                        ChargeAmount = _markup.ProcessHotelCancelationPrice_WithMarkup(ref cancellationAmountMain, SearchParam.NoofNights, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), ChargeAmount, isPerce, B2CMarkup, AgentPercentage, AgentMarkupAmount, IsParentId, SubAgentMarkup, ref VatOnPolicy);
                                        #endregion
                                        if (DeadLineDate.AddDays(CutOffDays).Date > DateTime.Now.Date)
                                            DeadLineDate = DeadLineDate.AddDays(CutOffDays).Date;
                                        else
                                            DeadLineDate = DateTime.Now.Date;
                                        if (curDate.Date < DeadLineDate.Date)
                                        {
                                            if (c == 0)
                                            {
                                                double Zero = 0;
                                                dtDidaCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref Zero, dtDidaCancellation.NewRow(), CurrencyFactorDida, Currency, SupplierName, Convert.ToString(ownsystemhotelId), Convert.ToString(1), Convert.ToString(RoomTypeId), RoomTypeName, Convert.ToString(BreakfastType), curDate, DeadLineDate.AddDays(-1).AddHours(23).AddMinutes(59).AddSeconds(59), 0, 0, "", 0, "", "NO"));
                                                dtDidaCancellation.AcceptChanges();
                                            }
                                            dtDidaCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref cancellationAmountMain, dtDidaCancellation.NewRow(), CurrencyFactorDida, Currency, SupplierName, Convert.ToString(ownsystemhotelId), Convert.ToString(1), Convert.ToString(RoomTypeId), RoomTypeName, Convert.ToString(BreakfastType), DeadLineDate, DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null), ChargeAmount, ChargeAmount, "", 0, "", "Yes"));
                                            dtDidaCancellation.AcceptChanges();
                                        }
                                        else
                                        {
                                            dtDidaCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref cancellationAmountMain, dtDidaCancellation.NewRow(), CurrencyFactorDida, Currency, SupplierName, Convert.ToString(ownsystemhotelId), Convert.ToString(1), Convert.ToString(RoomTypeId), RoomTypeName, Convert.ToString(BreakfastType), curDate, DeadLineDate, TotalPrice, TotalPrice, "", 0, "", "Yes"));
                                            dtDidaCancellation.AcceptChanges();
                                        }

                                    }
                                    #endregion
                                }
                                dtDidaHotelRoom.AcceptChanges();
                            }
                            Options ops = (await _supplierRes.GetCommonRoomForSuppliers(GiataId, commonSearch.tokenData, commonSearch.advancedOptions, FinalMinPrice, drXmlHotel, dtDidaHotelRoom, dtDidaCancellation, dtDidaHotelRoomRateBreackUp, SearchParam));
                            if (ops != null)
                                options.Add(ops);
                        }
                        #endregion
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
            finally
            {
                #region ResProcessTimer
                try
                {
                    if (SearchParam != null && ResProccessTimerLogActive == "1" && timerroomx.ElapsedMilliseconds > Convert.ToInt32(ResProccessTimeValueforSaveLog))
                    {
                        timerroomx.Stop();
                        SearchParam.SupplierTime += ",|Finish:Dida Process Response  " + "|Time :" + timerroomx.Elapsed.ToString();
                        await Task.Run(async () => await _commonMethod.FireAway_SQL_ResPorcessTimeLog("", timerroomx.Elapsed.ToString(), SupplierName, SearchParam.SearchKey, 1, SearchParam.AgentID));
                    }
                }
                catch (Exception ex)
                {
                    await _exception.Error(ex);
                }
                #endregion
            }
        }

        public async Task<List<Options>> XMLDidaPriceOnly_Async(HotelX Item, CommonSearch commonSearch, string ResponseData, HotelSearchParameter SearchParam, string SupplierName, string XMLCITY, string XMLCountry, string Currency, double CurrencyFactor, string XmlNationality, Int32 processTimeout, string ResProccessTimerLogActive, string ResProccessTimeValueforSaveLog, DataTable dtROE)
        {
            System.Diagnostics.Stopwatch timerroomx = new System.Diagnostics.Stopwatch();
            timerroomx.Start();

            List<Options> options = new List<Options>();
            DataTable DtHotel = _commonMethod.GenerateDataTableNewV2();
            try
            {
                #region Response    
                if (ResponseData == string.Empty || ResponseData.Contains("ErrorTxt"))
                {
                    Item.ResponseStatus = 204;
                    Item.ResponseType = "NoCount";
                    return options;
                }

                DidaClass.Root Dida_Response = null;
                try
                {
                    //Dida_Response = JsonSerializer.Deserialize<DidaClass.Root>(ResponseData);
                    Dida_Response = Newtonsoft.Json.JsonConvert.DeserializeObject<DidaClass.Root>(ResponseData); //140525

                }
                catch (Exception ex)
                {
                    Item.ResponseStatus = 404;
                    _ = _exception.Error(ex);
                    Item.ResponseType = "NoCount";
                    return options;
                }
                try
                {
                    if (Dida_Response != null && Dida_Response.Error != null)
                    {
                        //Item.ResponseStatus = Convert.ToInt32(Dida_Response.Error.Code);
                        Item.ResponseType = "NoCount";
                        Item.ActualReason = Dida_Response.Error.Message;
                        return options;
                    }
                }
                catch
                {

                }
                
                if (Dida_Response == null)
                {
                    Item.ResponseStatus = 204;
                    Item.ResponseType = "NoCount";
                    return options;
                }
                //if (DS.results.Count < 1)
                if (Dida_Response.Success == null)
                {
                    Item.ResponseStatus = 204;
                    Item.ResponseType = "NoCount";
                    return options;
                }
                string Provider = SupplierName;


                #endregion

                for (int ii = 0; ii < Dida_Response.Success.PriceDetails.HotelList.Count; ii++)
                {



                    Item.ResponseType = "Success";

                    #region "When not Need Room"  
                    // add hotel code from price only and room code blank here
                    if (SearchParam.Filters.IsShowRooms == 0)
                    {
                        double cheapestRate = Dida_Response.Success.PriceDetails.HotelList[ii]
                                            .RatePlanList
                                            .OrderBy(rate => rate.TotalPrice)
                                            .Select(rate => rate.TotalPrice)
                                            .FirstOrDefault();

                        #region "Hotel Code Block "
                        double TotalCharges = 0;
                        DataRow drXmlHotel = DtHotel.NewRow();
                        string HotelStar = "";
                        HotelStar = _commonMethod.Star_Rating(HotelStar);
                        string InternalHotelId = Convert.ToString(0);
                        drXmlHotel["XMLHotelCode"] = Dida_Response.Success.PriceDetails.HotelList[ii].HotelID;
                        drXmlHotel["HotelName"] = Dida_Response.Success.PriceDetails.HotelList[ii].HotelName;
                        drXmlHotel["SupplierName"] = SupplierName;
                        drXmlHotel["OfficeID"] = 0;
                        drXmlHotel["ContractID"] = 0;
                        drXmlHotel["Rating"] = Convert.ToInt32(0);
                        drXmlHotel["XmlCity"] = XMLCITY;
                        drXmlHotel["IsShowSupplier"] = false;
                        drXmlHotel["BookingStatus"] = "Available";
                        TotalCharges = ((cheapestRate * CurrencyFactor) * SearchParam.Rooms.Count);
                        drXmlHotel["FinalSellingPrice"] = TotalCharges;
                        drXmlHotel["SellingPrice"] = TotalCharges;
                        drXmlHotel["SupplierCurrency"] = Currency;
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
                            double MinPrice = TotalCharges;
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
                        #region "Hotel Code Block "
                        double TotalCharges = 0;
                        double FinalMinPrice = 0;

                        DataRow drXmlHotel = DtHotel.NewRow();
                        string HotelStar = "";
                        HotelStar = _commonMethod.Star_Rating(HotelStar);
                        string InternalHotelId = Convert.ToString(0);
                        drXmlHotel["XMLHotelCode"] = Dida_Response.Success.PriceDetails.HotelList[ii].HotelID;
                        drXmlHotel["HotelName"] = Dida_Response.Success.PriceDetails.HotelList[ii].HotelName;
                        drXmlHotel["SupplierName"] = SupplierName;
                        drXmlHotel["OfficeID"] = 0;
                        drXmlHotel["ContractID"] = 0;
                        drXmlHotel["Rating"] = Convert.ToInt32(0);
                        drXmlHotel["XmlCity"] = XMLCITY;
                        drXmlHotel["IsShowSupplier"] = false;
                        drXmlHotel["BookingStatus"] = "Available";
                        TotalCharges = ((Dida_Response.Success.PriceDetails.HotelList[ii].RatePlanList[0].TotalPrice * CurrencyFactor) * SearchParam.Rooms.Count);
                        drXmlHotel["FinalSellingPrice"] = TotalCharges;
                        drXmlHotel["SellingPrice"] = TotalCharges;
                        drXmlHotel["SupplierCurrency"] = Currency;
                        DtHotel.Rows.Add(drXmlHotel);
                        FinalMinPrice = TotalCharges;
                        #endregion

                        #region "GetRooms for this hotel"
                        bool willAddOrNot = true;
                        Int64 RaynaID = 0;
                        Int64 GiataId = 0;
                        int RatingHotel = 0;
                        Int64 ownsystemhotelId = 0;
                        double MinPrice = 1;

                        _markup.GetHotelDataCommon(ref MinPrice, ref willAddOrNot, ref ownsystemhotelId, ref RaynaID, ref GiataId, ref RatingHotel, ref drXmlHotel, commonSearch.AgentId, commonSearch.TariffID, commonSearch.dsAgentDetails, commonSearch.supplierGiataMaster, SearchParam, commonSearch.dtCountryCityMarkup, commonSearch.B2CMarkup, commonSearch.IsParentId, commonSearch.SubAgentMarkup, commonSearch.SearchByHotel, commonSearch.HotelIDS);
                        if (willAddOrNot)
                        {
                            //DataTable dtInrCurrency = Common.GetCountryCurrency(Currency);
                            double CurrencyFactorDida = _commonMethod.GetROEByCurrency(Currency, dtROE);// Convert.ToDouble(dtInrCurrency.Rows[0]["AmtToExch"]);

                            double SubAgentMarkup = 0;
                            int IsParentId = 1; int agentid = 0;
                            if (commonSearch.dtAgentDetail != null)
                            {
                                IsParentId = Convert.ToInt32(commonSearch.dtAgentDetail.Rows[0]["IsParentId"]);
                                SubAgentMarkup = Convert.ToDouble(commonSearch.dtAgentDetail.Rows[0]["B2CMarkupPer"]);
                                agentid = Convert.ToInt32(commonSearch.dtAgentDetail.Rows[0]["AgentID"]);

                            }
                            double AgentPercentage = 0.0;
                            double AgentMarkupAmount = 0.0;
                            Boolean isPerce = true;
                            Boolean blnIsShowSupplier = false;
                            int hotelRating = 0;
                            _markup.getAgetMarkupSetting(SupplierName, Convert.ToString(GiataId), commonSearch.dtSuplierMarkup, commonSearch.dtMarkupDetails, ref AgentPercentage, ref AgentMarkupAmount, ref blnIsShowSupplier, ref isPerce, agentid, hotelRating, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), SearchParam.CheckInDate);

                            double AgentCompanyPercentage = Convert.ToDouble(SearchParam.AgentCompanyPercentage);
                            bool IsB2C = Convert.ToBoolean(Convert.ToByte(SearchParam.IsB2BOrB2C));
                            double B2CMarkup = (IsB2C == false ? AgentCompanyPercentage : XMLHotelBookingCommon.B2CMarkup);
                            if (IsB2C == true)
                                AgentPercentage = 0;

                            DataRow[] DidaDetails = null;
                            DataRow[] DidaSearchConfig = null;
                            if (commonSearch.dtSuplierMarkup != null & commonSearch.dtSuplierMarkup != null && commonSearch.dtSuplierMarkup.Rows.Count > 0)
                            {
                                DidaDetails = commonSearch.dtSuplierMarkup.Select("SupplierName='" + SupplierName + "'");
                            }
                            if (commonSearch.dtSearchConfig != null & commonSearch.dtSearchConfig != null && commonSearch.dtSearchConfig.Rows.Count > 0)
                            {
                                if (SearchParam != null)
                                {
                                    if (DidaDetails.GetLength(0) > 0)
                                        DidaSearchConfig = commonSearch.dtSearchConfig.Select("SupplierID=" + Convert.ToString(DidaDetails[0]["SupplierID"]) + " and (CityID=" + SearchParam.CityID + " or CityID=0) and (Stars=0 OR (Stars>=" + SearchParam.StarStart + " and Stars<=" + SearchParam.StarEnd + "))");
                                }
                            }

                            string checkin = (SearchParam.CheckInDate).Substring(6, 4) + "-" + (SearchParam.CheckInDate).Substring(3, 2) + "-" + (SearchParam.CheckInDate).Substring(0, 2);
                            string checkout = (SearchParam.CheckOutDate).Substring(6, 4) + "-" + (SearchParam.CheckOutDate).Substring(3, 2) + "-" + (SearchParam.CheckOutDate).Substring(0, 2);

                            DataTable dtDidaHotelRoom = _commonMethod.GenerateRoomtypeDataTableNewV2();
                            DataTable dtDidaCancellation = _commonMethod.GenerateCancellationDataTableNewV2();
                            DataTable dtDidaHotelRoomRateBreackUp = _commonMethod.GenerateoomRateBreackUpDataTableNewV2();
                            int Rooms = SearchParam.Rooms.Count;
                            int CutOffDays = XMLHotelBookingCommon.CutOffDays;
                            CutOffDays = _commonMethod.GetCutOffDays(commonSearch.dtSuplierMarkup, Provider);
                            string DidaResponse = ResponseData;
                            //string drDidaResponse = Dida_Response.Success.PriceDetails.HotelList[ii].RatePlanList[ii].Select("HotelList_Id=" + Convert.ToString(Dida_Response.Success.PriceDetails.HotelList[ii].HotelID)); ;
                            if (SearchParam.Filters.IsShowRooms == 1)
                            {
                                for (int i = 0; i < Dida_Response.Success.PriceDetails.HotelList[ii].RatePlanList.Count; i++)
                                {
                                    string GUID = Guid.NewGuid().ToString();
                                    int BreakfastType = Convert.ToInt32(Dida_Response.Success.PriceDetails.HotelList[ii].RatePlanList[i].BreakfastType);
                                    //int RatePlanList_Id = Convert.ToInt32(drDidaResponse[i]["RatePlanList_Id"]);
                                    //int RatePlanList_Id = Convert.ToInt32(Dida_Response.Success.PriceDetails.HotelList[ii].RatePlanList[i].RatePlanID);
                                    int StandardOccupancy = Convert.ToInt32(Dida_Response.Success.PriceDetails.HotelList[ii].RatePlanList[i].StandardOccupancy);
                                    int MaxOccupancy = Convert.ToInt32(Dida_Response.Success.PriceDetails.HotelList[ii].RatePlanList[i].MaxOccupancy);
                                    string MealName = BreakfastType == 2 ? "With Breakfast" : "No Breakfast";
                                    string RoomTypeId = Convert.ToString(Dida_Response.Success.PriceDetails.HotelList[ii].RatePlanList[i].RatePlanID) + "$$$" + XmlNationality;
                                    int BedTypeId = Convert.ToInt32(Dida_Response.Success.PriceDetails.HotelList[ii].RatePlanList[i].BedType);
                                    string RoomName = Convert.ToString(Dida_Response.Success.PriceDetails.HotelList[ii].RatePlanList[i].RatePlanName);
                                    int InventoryCount = Convert.ToInt32(Dida_Response.Success.PriceDetails.HotelList[ii].RatePlanList[i].InventoryCount);
                                    if (InventoryCount < SearchParam.Rooms.Count || MaxOccupancy < (SearchParam.Rooms[0].NoofAdults + SearchParam.Rooms[0].NoofChild))
                                        continue;

                                    string CDate = Convert.ToString(Dida_Response.Success.PriceDetails.HotelList[ii].RatePlanList[i].RatePlanCancellationPolicyList[0].FromDate);

                                    RoomTypeId = RoomTypeId + "$$$" + CDate;

                                    string BedType = "";// !!!!!! Note: Read XML File For Get BedType !!!!!!
                                    BedType = DidaRedXMLFileData("BedType", "ID", Convert.ToString(BedTypeId), "Name", Item.TechName);
                                    if (BedType != "")
                                    {
                                        RoomName = RoomName + " [BedType: " + BedType + "]";
                                    }
                                    #region/added for excluded info 26-Dec-2024
                                    string EXINFO = "";
                                    try
                                    {
                                        if (Dida_Response.Success.PriceDetails.HotelList[ii].RatePlanList[i].ExcludedFeeList != null)
                                        {
                                            for (int R = 0; R < Dida_Response.Success.PriceDetails.HotelList[ii].RatePlanList[i].ExcludedFeeList.Count; R++)
                                            {
                                                string Excurrency = Convert.ToString(Dida_Response.Success.PriceDetails.HotelList[ii].RatePlanList[i].ExcludedFeeList[R].Currency);
                                                double ExAmpount = Convert.ToDouble(Dida_Response.Success.PriceDetails.HotelList[ii].RatePlanList[i].ExcludedFeeList[R].Amount);
                                                string ExFeeTypeName = Convert.ToString(Dida_Response.Success.PriceDetails.HotelList[ii].RatePlanList[i].ExcludedFeeList[R].FeeTypeName);
                                                string EXinfo = ("<span style=\"font-size: large;\">Info:" + (R + 1) + "</br> Amount: " + ExAmpount) + "</span>,</br> <span style=\"font-size: large;\"> FeeTypeName: " + ExFeeTypeName + "</span>" + "</span>,</br> <span style=\"font-size: large;\"> Currency: " + Excurrency + "</span></br>";
                                                EXINFO += EXinfo;
                                            }
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        await _exception.Error(ex);
                                    }

                                    #endregion
                                    string RoomTypeName = Rooms + " X " + RoomName;
                                    double TotalPrice = (Convert.ToDouble(Dida_Response.Success.PriceDetails.HotelList[ii].RatePlanList[i].TotalPrice) * CurrencyFactorDida) * Rooms;
                                    double NightRate = TotalPrice / SearchParam.NoofNights;
                                    //double ChargeAmount = (Convert.ToDouble(DidaResponse.Tables["RatePlanCancellationPolicyList"].Rows[i]["Amount"]) * CurrencyFactorDida) * Rooms;
                                    //DateTime DeadLineDate = Convert.ToDateTime(DidaResponse.Tables["RatePlanCancellationPolicyList"].Rows[i]["FromDate"]);
                                    double PriceWithoutSupplement = 0;
                                    double Supplement = 0;
                                    if (Dida_Response.Success.PriceDetails.HotelList[ii].RatePlanList[i].PriceWithoutSupplement != null)
                                    {
                                        double WithoutSupplement = (Convert.ToString(Dida_Response.Success.PriceDetails.HotelList[ii].RatePlanList[i].PriceWithoutSupplement)) == "" ? 0 : (Convert.ToDouble(Dida_Response.Success.PriceDetails.HotelList[ii].RatePlanList[i].PriceWithoutSupplement));
                                        double SupplementOnly = (Convert.ToString(Dida_Response.Success.PriceDetails.HotelList[ii].RatePlanList[i].Supplement)) == "" ? 0 : (Convert.ToDouble(Dida_Response.Success.PriceDetails.HotelList[ii].RatePlanList[i].Supplement));
                                        PriceWithoutSupplement = (WithoutSupplement * Rooms) * CurrencyFactorDida;
                                        Supplement = (SupplementOnly * Rooms) * CurrencyFactorDida;
                                    }

                                    #region RateBreakup
                                    //--MarkUp  
                                    double VatOnRateBrackUp = 0;
                                    //NightRate = _markup.ProcessHotelRateBreakupPrice_WithMarkup(1, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), NightRate, B2CMarkup, IsParentId, isPerce, AgentPercentage, AgentMarkupAmount, SubAgentMarkup, ref VatOnRateBrackUp);
                                    //for (int r = 0; r < SearchParam.NoofNights; r++)
                                    //{
                                    //    DateTime FromDate = Convert.ToDateTime(checkin).Date.AddDays(r);
                                    //    DataRow drDidaRateBreackUp = dtDidaHotelRoomRateBreackUp.NewRow();
                                    //    drDidaRateBreackUp["SupplierName"] = SupplierName;
                                    //    drDidaRateBreackUp["OwnsystemHotelId"] = ownsystemhotelId;
                                    //    drDidaRateBreackUp["ContractID"] = 0;
                                    //    drDidaRateBreackUp["RoomNo"] = 1;
                                    //    drDidaRateBreackUp["RoomTypeId"] = RoomTypeId;
                                    //    drDidaRateBreackUp["Meal"] = MealName;
                                    //    string rateWeekColumn = Convert.ToString(FromDate.Date.DayOfWeek).Substring(0, 3);
                                    //    drDidaRateBreackUp["DateOfWeek"] = rateWeekColumn;
                                    //    drDidaRateBreackUp["MealId"] = BreakfastType;
                                    //    drDidaRateBreackUp["Rate"] = NightRate;
                                    //    drDidaRateBreackUp["OrderNo"] = 1;
                                    //    drDidaRateBreackUp["RateType"] = "A Rate";
                                    //    drDidaRateBreackUp["RoomTypeName"] = RoomTypeName;
                                    //    drDidaRateBreackUp["DateOfWeek"] = rateWeekColumn;
                                    //    drDidaRateBreackUp["Date"] = FromDate;
                                    //    drDidaRateBreackUp["BookingStatus"] = "Available";
                                    //    drDidaRateBreackUp["BoardTypeID"] = "";
                                    //    drDidaRateBreackUp["VatdiffAmount"] = VatOnRateBrackUp;
                                    //    dtDidaHotelRoomRateBreackUp.Rows.Add(drDidaRateBreackUp);
                                    //}
                                    #endregion

                                    #region Room
                                    DataRow drDidaHotelRoom = dtDidaHotelRoom.NewRow();
                                    drDidaHotelRoom["OwnsystemHotelId"] = ownsystemhotelId;
                                    drDidaHotelRoom["ContractID"] = 0;
                                    drDidaHotelRoom["UniqueNo"] = ownsystemhotelId;
                                    drDidaHotelRoom["RoomSequenceNo"] = "";
                                    drDidaHotelRoom["MealName"] = MealName;
                                    drDidaHotelRoom["allocationDetails"] = "";
                                    drDidaHotelRoom["minStay"] = "";
                                    drDidaHotelRoom["DateApplyMinStay"] = "";
                                    drDidaHotelRoom["RoomNo"] = 1;
                                    drDidaHotelRoom["EssentialInformation"] = EXINFO; // (BedType == "" ? "" : "<span style=\"font-size: large;\"> BedType: " + BedType) + "</span>,</br> <span style=\"font-size: large;\"> InventoryCount: " + InventoryCount + "</span>";
                                    drDidaHotelRoom["MealId"] = BreakfastType;
                                    drDidaHotelRoom["RoomTypeName"] = RoomTypeName;
                                    drDidaHotelRoom["RoomTypeId"] = RoomTypeId;
                                    drDidaHotelRoom["BookingStatus"] = "Available";
                                    drDidaHotelRoom["AdultsOccupancy"] = SearchParam.Rooms[0].NoofAdults;
                                    drDidaHotelRoom["ChildOccupancy"] = SearchParam.Rooms[0].NoofChild;
                                    drDidaHotelRoom["MaxExtrabed"] = 0;
                                    drDidaHotelRoom["RoomTypeCode"] = RoomTypeId;
                                    drDidaHotelRoom["BoardTypeID"] = "";
                                    drDidaHotelRoom["CompanyBuyingPrice"] = TotalPrice;
                                    drDidaHotelRoom["OfferDescription"] = "";
                                    drDidaHotelRoom["offerCode"] = "";
                                    if (PriceWithoutSupplement > TotalPrice)
                                    {
                                        VatOnRateBrackUp = 0;
                                        PriceWithoutSupplement = _markup.ProcessHotelRateBreakupPrice_WithMarkup(1, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), PriceWithoutSupplement, B2CMarkup, IsParentId, isPerce, AgentPercentage, AgentMarkupAmount, SubAgentMarkup, ref VatOnRateBrackUp);
                                        drDidaHotelRoom["GrossWitohutDiscount"] = PriceWithoutSupplement;
                                    }
                                    _markup.ProcessHotelRoom_WithMarkup(CurrencyFactorDida, Currency, false, true, SearchParam.NoofNights, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), SearchParam.CheckInDate, SearchParam.CheckOutDate, ref TotalPrice, ref drDidaHotelRoom, isPerce, AgentPercentage, AgentMarkupAmount, SubAgentMarkup, B2CMarkup, IsParentId);

                                    drDidaHotelRoom["SupplierName"] = SupplierName;
                                    dtDidaHotelRoom.Rows.Add(drDidaHotelRoom);
                                    #endregion

                                    #region CancelltionPolicy
                                    DateTime curDate = _commonMethod.GetTimeZoneDate(SearchParam.CurrentDate);
                                    #region Vat
                                    double VatOnPolicy = 0;
                                    double ChargeAmount = 0;
                                    DateTime DeadLineDate = DateTime.Now;

                                    if (Dida_Response.Success.PriceDetails.HotelList[ii].RatePlanList[i].RatePlanCancellationPolicyList != null)
                                    {
                                        for (int c = 0; c < Dida_Response.Success.PriceDetails.HotelList[ii].RatePlanList[i].RatePlanCancellationPolicyList.Count; c++)
                                        {
                                            ChargeAmount = (Convert.ToDouble(Dida_Response.Success.PriceDetails.HotelList[ii].RatePlanList[i].RatePlanCancellationPolicyList[c].Amount) * CurrencyFactorDida) * Rooms;
                                            DeadLineDate = Convert.ToDateTime(Dida_Response.Success.PriceDetails.HotelList[ii].RatePlanList[i].RatePlanCancellationPolicyList[c].FromDate);
                                            double cancellationAmountMain = ChargeAmount;
                                            ChargeAmount = _markup.ProcessHotelCancelationPrice_WithMarkup(ref cancellationAmountMain, SearchParam.NoofNights, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), ChargeAmount, isPerce, B2CMarkup, AgentPercentage, AgentMarkupAmount, IsParentId, SubAgentMarkup, ref VatOnPolicy);
                                            #endregion
                                            if (DeadLineDate.AddDays(CutOffDays).Date > DateTime.Now.Date)
                                                DeadLineDate = DeadLineDate.AddDays(CutOffDays).Date;
                                            else
                                                DeadLineDate = DateTime.Now.Date;
                                            if (curDate.Date < DeadLineDate.Date)
                                            {
                                                if (c == 0)
                                                {
                                                    double Zero = 0;
                                                    dtDidaCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref Zero, dtDidaCancellation.NewRow(), CurrencyFactorDida, Currency, SupplierName, Convert.ToString(ownsystemhotelId), Convert.ToString(1), Convert.ToString(RoomTypeId), RoomTypeName, Convert.ToString(BreakfastType), curDate, DeadLineDate.AddDays(-1).AddHours(23).AddMinutes(59).AddSeconds(59), 0, 0, "", 0, "", "NO"));
                                                    dtDidaCancellation.AcceptChanges();
                                                }
                                                dtDidaCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref cancellationAmountMain, dtDidaCancellation.NewRow(), CurrencyFactorDida, Currency, SupplierName, Convert.ToString(ownsystemhotelId), Convert.ToString(1), Convert.ToString(RoomTypeId), RoomTypeName, Convert.ToString(BreakfastType), DeadLineDate, DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null), ChargeAmount, ChargeAmount, "", 0, "", "Yes"));
                                                dtDidaCancellation.AcceptChanges();
                                            }
                                            else
                                            {
                                                dtDidaCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref cancellationAmountMain, dtDidaCancellation.NewRow(), CurrencyFactorDida, Currency, SupplierName, Convert.ToString(ownsystemhotelId), Convert.ToString(1), Convert.ToString(RoomTypeId), RoomTypeName, Convert.ToString(BreakfastType), curDate, DeadLineDate, TotalPrice, TotalPrice, "", 0, "", "Yes"));
                                                dtDidaCancellation.AcceptChanges();
                                            }
                                        }
                                    }

                                    #endregion
                                }
                                dtDidaHotelRoom.AcceptChanges();
                            }
                            Options ops = (await _supplierRes.GetCommonRoomForSuppliers(GiataId, commonSearch.tokenData, commonSearch.advancedOptions, FinalMinPrice, drXmlHotel, dtDidaHotelRoom, dtDidaCancellation, dtDidaHotelRoomRateBreackUp, SearchParam));
                            if (ops != null)
                                options.Add(ops);
                        }
                        #endregion
                    }
                    #endregion
                }
                return options;
            }
            catch (Exception ex)
            {
                Item.ResponseStatus = 404;
                _= _exception.Error(ex);
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
                        
                        SearchParam.SupplierTime += ",|Finish:Dida Process Response  " + "|Time :" + timerroomx.Elapsed.ToString();
                        _ = Task.Run(() => _commonMethod.FireAway_SQL_ResPorcessTimeLog("", timerroomx.Elapsed.ToString(), SupplierName, SearchParam.SearchKey, 1, SearchParam.AgentID));
                    }
                }
                catch (Exception ex)
                {
                    await _exception.Error(ex);
                }
                #endregion
            }
        }


        public string DidaRedXMLFileData(string FileName, string FieldToSearch, string FieldSearchFor, string ReturnField, string Supplier)
        {
            try
            {
                string ReturnText = "";
                DataSet dsMeal = new DataSet();
                if (!_cache.TryGetValue(Supplier + "_" + FileName, out dsMeal))
                {
                    dsMeal = _commonMethod.ReadXmlFile("Hotel/" + Supplier + "_" + FileName + ".xml");
                    //HttpRuntime.Cache.Insert(Supplier + "_" + FileName, dsMeal, null, DateTime.Now.AddHours(24), TimeSpan.Zero);

                    _cache.Set(Supplier + "_" + FileName, dsMeal, new MemoryCacheEntryOptions().SetAbsoluteExpiration(DateTime.Now.AddHours(24)));
                }
                //else
                //{
                //    dsMeal = (DataSet)(_cache.Get(Supplier + "_" + FileName));
                //}

                DataRow[] drdsMeal = null;
                if (dsMeal.Tables[2] != null)
                {
                    foreach (DataRow row in dsMeal.Tables[2].Rows)
                    {
                        if (Convert.ToString(row[FieldToSearch]).ToLower() == FieldSearchFor.ToLower())
                        {
                            drdsMeal = new DataRow[1];
                            drdsMeal[0] = row;
                            break;
                        }
                    }
                }
                if (drdsMeal != null)
                {
                    ReturnText = Convert.ToString(drdsMeal[0][ReturnField]);
                }

                return ReturnText;
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
                return string.Empty;
            }
        }

        #region Recheck - 270525
        public  async Task<int> CheckDidaPrebookOnRecheck(string hotelcode, string ownsystemid, DataTable dtCheckPriceReqStructure, string roomtypeid, HotelSearchParameter SearchPara, string DidaPassword, string DidaURL, string Username, string SearchKey, string Provider,  string checkindate,  string checkoutdate,  int Adults,
 int Childs,  int Rooms,  string AgodaSearchUrl,  string AgodaApiKey,  string ChildAge, bool isForBooking, double Rate, string AgentUniqueNo, string ISBooking, DataTable dtROE, bool IsPriceChange, double FinalPrice = 0, string SupplierCurrency="USD")////new added on 03-May-2025

        {

            try
            {

                for (int j = 0; j < 1; j++)
                {

                    #region Dida PreBook hit on Recheck


                    {
                        string responsetype = "Success";
                        string time = "";
                        string SupplierName = "Dida";
                        string Error = "";
                        string Req = "";
                        string ResponseData = "";
                        string RoomTypeIdTemp = roomtypeid;
                        int flag = 1;
                        string[] spitidRoomTypeId = roomtypeid.Split(new String[] { "$$$" }, StringSplitOptions.None);
                        string RoomTypeId = spitidRoomTypeId[0];
                        string Nationality = spitidRoomTypeId[1];
                        string CDate = spitidRoomTypeId[2];
                        try
                        {
                            for (int rm = 0; rm < SearchPara.Rooms.Count; rm++)
                            {
                                Adults += SearchPara.Rooms[rm].NoofAdults;
                                Childs += SearchPara.Rooms[rm].NoofChild;
                            }
                            //DataTable dtInrCurrency = Common.GetCountryCurrency(Convert.ToString(ConfigurationManager.AppSettings["DidaCurrency"]));
                            double CurrencyFactorDida = _commonMethod.GetROEByCurrency(Convert.ToString(SupplierCurrency), dtROE);
                            checkindate = checkindate.Substring(6, 4) + "-" + checkindate.Substring(3, 2) + "-" + checkindate.Substring(0, 2);
                            checkoutdate = checkoutdate.Substring(6, 4) + "-" + checkoutdate.Substring(3, 2) + "-" + checkoutdate.Substring(0, 2);
                            StringBuilder PreBookRQ = new StringBuilder();

                            //PreBookRQ.Append("{\"PreBook\": true,");
                            PreBookRQ.Append("{\"PreBook\": false,");//added on 10-APR-2025
                            PreBookRQ.Append("\"CheckOutDate\": \"" + checkoutdate + "\",");
                            PreBookRQ.Append("\"CheckInDate\": \"" + checkindate + "\",");
                            PreBookRQ.Append("\"NumOfRooms\": " + Rooms + ",");
                            PreBookRQ.Append("\"HotelID\": " + hotelcode + ",");
                            PreBookRQ.Append("\"Header\": {");
                            PreBookRQ.Append("\"LicenseKey\": \"" + DidaPassword + "\",");
                            PreBookRQ.Append("\"ClientID\": \"" + Username + "\"},");
                            PreBookRQ.Append("\"OccupancyDetails\": [");
                            for (int i = 0; i < Rooms; i++)
                            {
                                PreBookRQ.Append("{\"ChildCount\": " + Childs + ",");
                                PreBookRQ.Append("\"AdultCount\": " + Adults + ",");
                                if (Childs == 1)
                                    PreBookRQ.Append("\"ChildAgeDetails\": [" + SearchPara.Rooms[i].Child1Age + "],");
                                if (Childs == 2)
                                    PreBookRQ.Append("\"ChildAgeDetails\": [" + SearchPara.Rooms[i].Child1Age + "," + SearchPara.Rooms[i].Child2Age + " ],");
                                PreBookRQ.Append("\"RoomNum\": " + (i + 1) + "}");
                                if ((i + 1) != Rooms)
                                    PreBookRQ.Append(",");
                            }
                            PreBookRQ.Append("],");
                            PreBookRQ.Append("\"Currency\": \"" + Convert.ToString(SupplierCurrency) + "\",");
                            PreBookRQ.Append("\"Nationality\": \"" + Nationality + "\",");
                            PreBookRQ.Append("\"RatePlanID\": \"" + RoomTypeId + "\"}");


                            Req = PreBookRQ.ToString();
                            if (isForBooking == false)// hit if not from booking
                            {
                                int UserCartId = 0, UserCartDetailId = 0;
                                dtCheckPriceReqStructure.Rows.Add(_commonMethod.AddCheckPriceReq(dtCheckPriceReqStructure.NewRow(), j + 1, UserCartId, UserCartDetailId, roomtypeid, "Dida", Req));
                            }

                            string Action = DidaURL + Convert.ToString(_supplierConfig.Dida.Dida_preBooking_Action);

                            System.Diagnostics.Stopwatch timerroomprocess = new System.Diagnostics.Stopwatch();
                            timerroomprocess.Start();

                           // ResponseData = GetDidaResponse( Req, Action, "", SearchKey, 5000);

                            ResponseDto responseDto = await GetDidaResponse(Req, Action, "", SearchKey, 5000);
                            ResponseData = responseDto.Response;
                            Error = responseDto.Error;

                            timerroomprocess.Stop();
                            time = timerroomprocess.Elapsed.ToString();

                            _commonMethod.ThirdPartyBookingHistory(roomtypeid, Convert.ToString(AgentUniqueNo), Req, ResponseData + Error, "After Get Price Dida Request hit booking" + j + "isForBooking" + isForBooking, "Dida", "Response Confirm", "-", "WithinEarth");
                            if (ResponseData == string.Empty || !ResponseData.Contains("Success"))
                            {
                                try
                                {
                                    responsetype = "GeneralError";
                                    string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", SearchPara.AgentID, time, responsetype, Req, ResponseData, Error);
                                }
                                catch (Exception e)
                                {
                                    _ = _exception.ErrorNew(e, _LogAPI.Recheck,SearchKey);
                                }
                                if (ResponseData.Contains("No available room"))
                                {
                                    ISBooking = "NotAvailable";
                                    return 4;

                                }

                                else
                                    ISBooking = "Available";
                                return flag;


                            }
                            //ResponseData = Newtonsoft.Json.JsonConvert.DeserializeXmlNode(ResponseData).InnerXml;
                            ResponseData = Newtonsoft.Json.JsonConvert.DeserializeXmlNode(ResponseData, "root").InnerXml; //#04-NOV-2024 K2
                            DataSet DidaResponse = _commonMethod.GetDataSetFromXMLString(ResponseData);//###### In New Version API Remove XML dataset. nedd to use json class
                            DataRow[] drRes = DidaResponse.Tables["PriceDetails"].Select();
                            double TotalPrice = 0;
                            for (int i = 0; i < drRes.Length; i++)
                            {
                                //string ReferenceNo = Convert.ToString(drRes[i]["ReferenceNo"]);
                                int PriceDetails_Id = Convert.ToInt32(drRes[i]["PriceDetails_Id"]);
                                for (int a = 0; a < DidaResponse.Tables["HotelList"].Rows.Count; a++)
                                {
                                    string HotelId = Convert.ToString(DidaResponse.Tables["HotelList"].Rows[a]["HotelId"]);
                                    int HotelList_Id = Convert.ToInt32(DidaResponse.Tables["HotelList"].Rows[a]["HotelList_Id"]);
                                    TotalPrice = Convert.ToDouble(DidaResponse.Tables["HotelList"].Rows[a]["TotalPrice"]);
                                    for (int b = 0; b < DidaResponse.Tables["RatePlanList"].Rows.Count; b++)
                                    {
                                        int RatePlanList_Id = Convert.ToInt32(DidaResponse.Tables["RatePlanList"].Rows[b]["RatePlanList_Id"]);
                                        int RoomStatus = Convert.ToInt32(DidaResponse.Tables["RatePlanList"].Rows[b]["RoomStatus"]);
                                        if (RoomStatus != 1)
                                        {
                                            ISBooking = "NotAvailable";
                                            return 4;

                                        }
                                        int StandardOccupancy = Convert.ToInt32(DidaResponse.Tables["RatePlanList"].Rows[b]["StandardOccupancy"]);
                                        int InventoryCount = Convert.ToInt32(DidaResponse.Tables["RatePlanList"].Rows[b]["InventoryCount"]);
                                        int MaxOccupancy = Convert.ToInt32(DidaResponse.Tables["RatePlanList"].Rows[b]["MaxOccupancy"]);
                                        //string RoomTypeId = Convert.ToString(DidaResponse.Tables["RatePlanList"].Rows[b]["RoomTypeId"]);
                                        string Currency = Convert.ToString(DidaResponse.Tables["RatePlanList"].Rows[b]["Currency"]);
                                        if (Currency != Convert.ToString(SupplierCurrency))
                                        {
                                            ISBooking = "NotAvailable";
                                            return 3;

                                        }
                                        string RatePlanId = Convert.ToString(DidaResponse.Tables["RatePlanList"].Rows[b]["RatePlanId"]);
                                        DataRow[] drRoomOcc = DidaResponse.Tables["RoomOccupancy"].Select("RatePlanList_Id='" + RatePlanList_Id + "'");
                                        for (int c = 0; c < drRoomOcc.Length; c++)
                                        {
                                            int RoomNum = Convert.ToInt32(drRoomOcc[c]["RoomNum"]);
                                            int AdultCount = Convert.ToInt32(drRoomOcc[c]["AdultCount"]);
                                            int ChildCount = Convert.ToInt32(drRoomOcc[c]["ChildCount"]);
                                        }
                                    }
                                    DataRow[] drCancellationPolicyList = DidaResponse.Tables["CancellationPolicyList"].Select("HotelList_Id='" + HotelList_Id + "'");
                                    for (int d = 0; d < drCancellationPolicyList.Length; d++)
                                    {
                                        double Amount = Convert.ToDouble(drCancellationPolicyList[d]["Amount"]);
                                        DateTime FromDate = Convert.ToDateTime(drCancellationPolicyList[d]["FromDate"]).Date;
                                        if (FromDate.Date != Convert.ToDateTime(CDate).Date)//If Policy is change at Prebook step then we Remove from cart
                                        {
                                            try
                                            {
                                                responsetype = "GeneralError";
                                                string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", SearchPara.AgentID, time, responsetype, Req, ResponseData, Error);
                                            }
                                            catch (Exception e)
                                            {
                                                _ = _exception.ErrorNew(e, _LogAPI.Recheck,SearchKey);
                                            }
                                            ISBooking = "NotAvailable";
                                            return 4;


                                        }
                                        break;
                                    }
                                }
                            }


                            FinalPrice = TotalPrice * CurrencyFactorDida;
                            if (IsPriceChange == false)//false then enter
                            {
                                //FinalPrice = (TotalPrice * CurrencyFactorDida) + 5; // to test
                                if (Math.Round(Rate, 2) != Math.Round(FinalPrice, 2))//discuss with ahmed
                                {

                                    ISBooking = "NotAvailable";
                                    return 2; // will Not send updated price


                                }
                            }

                            //return 1;
                        }
                        catch (Exception ex)
                        {
                            try
                            {
                                Error = ex.Message + ex.StackTrace.ToString();
                                responsetype = "GeneralError";
                                string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", SearchPara.AgentID, time, responsetype, Req, ResponseData, Error);
                            }
                            catch (Exception e)
                            {
                                _ = _exception.ErrorNew(e, _LogAPI.Recheck, SearchKey);

                            }
                            _ = _exception.ErrorNew(ex, _LogAPI.Recheck, SearchKey);

                            ISBooking = "NotAvailable";
                            return 2;


                        }

                        try
                        {
                            responsetype = "Success";
                            string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", SearchPara.AgentID, time, responsetype, Req, ResponseData, Error);
                        }
                        catch (Exception e)
                        {
                            _ = _exception.ErrorNew(e, _LogAPI.Recheck, SearchKey);

                        }
                        ISBooking = "Available";
                        return 0;

                    }
                    #endregion
                }

            }
            catch (Exception ex)
            {
                _ = _exception.ErrorNew(ex, _LogAPI.Recheck, SearchKey);

            }

            return 0;
        }


        public async Task<ResponseDto> GetDidaResponse(string strxmlRQRS, string strHost, string UniqueNo = "", string Searchkey = "", int timeout = 200000)
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
                Request.Accept = "application/json";
                Request.Headers.Add("Accept-Encoding", "gzip");
                Request.Timeout = timeout;
                // Request.ContentType = "text/xml;charset=utf-8";
                Request.UserAgent = "Mozilla/4.0 (compatible; MSIE 6.0; MS Web Services Client Protocol 1.1.4322.2300";
                Stream newStream = null;
                byte[] data = Encoding.ASCII.GetBytes(strxmlRQRS);
                Request.ContentLength = data.Length;
                newStream = Request.GetRequestStream();
                newStream.Write(data, 0, data.Length);

                HttpWebResponse Response = (HttpWebResponse)await Request.GetResponseAsync();
                Stream responseStream = Response.GetResponseStream();
                responseStream = new GZipStream(responseStream, CompressionMode.Decompress);
                StreamReader Reader = new StreamReader(responseStream, Encoding.Default);
                strOutput = await Reader.ReadToEndAsync();
                responseStream.Close();
                Reader.Close();
                Response.Close();
            }
            catch (Exception ex)
            {
                Error = ex.Message.ToString();
                _exception.Error(ex, Searchkey);
                if (UniqueNo != "")
                    _commonMethod.ThirdPartyBookingHistory(Convert.ToString(UniqueNo), Convert.ToString(UniqueNo), strxmlRQRS, ex.Message.ToString(), "Error Block", "Dida", "Response Confirm", "-", "We");
            }
            responseDto.Response = strOutput;
            responseDto.Error = Error;


            return responseDto;
        }

        #endregion

        public async Task<PriceCheckDidaDto> DidaPreBookingReq(string ReferenceNo, string ID, string Password, string HotelCode, string RCode, int NoOfAdults, int NoOfChild, string CheckInDate, string CheckOutDate, string URL, double Rate, double FinalPrice, int UserCartId, int UserCartDetailId, DataTable dtCheckPriceReqStructure, int j, string UniqueNo, string AgentUniqueNo, bool isForBooking, int ChildAge1, int ChildAge2, int NoOfRoom, string Nationality, string CDate, string Searchkey = "", int AgentId = 0, string DidaCurrency="USD", double CurrencyFactorDida=1)
        {
            PriceCheckDidaDto priceCheckDidaDto = new PriceCheckDidaDto();
            // flag:0= priceCheck Success
            // flag:1= priceCheck Response Error or Empty
            // flag:2= Error in PriceCheck
            // flag:3= Currency Cheange in PriceCheck Response
            // flag:4= Room is OnRequest or N/A at priceCheck
            // flag:5= Price is Changed...!

            string responsetype = "Success";
            string time = "";
            string SupplierName = "Dida";
            string Error = "";
            string Req = "";
            string ResponseData = "";

            int flag = 1;
            try
            {
                
               // DataTable dtInrCurrency = _commonMethod.GetCountryCurrency(DidaCurrency);
               // double CurrencyFactorDida = Convert.ToDouble(dtInrCurrency.Rows[0]["AmtToExch"]);
                CheckInDate = CheckInDate.Substring(0, 4) + "-" + CheckInDate.Substring(4, 2) + "-" + CheckInDate.Substring(6, 2);
                CheckOutDate = CheckOutDate.Substring(0, 4) + "-" + CheckOutDate.Substring(4, 2) + "-" + CheckOutDate.Substring(6, 2);
                StringBuilder PreBookRQ = new StringBuilder();

                PreBookRQ.Append("{\"PreBook\": true,");
                PreBookRQ.Append("\"CheckOutDate\": \"" + CheckOutDate + "\",");
                PreBookRQ.Append("\"CheckInDate\": \"" + CheckInDate + "\",");
                PreBookRQ.Append("\"NumOfRooms\": " + NoOfRoom + ",");
                PreBookRQ.Append("\"HotelID\": " + HotelCode + ",");
                PreBookRQ.Append("\"Header\": {");
                PreBookRQ.Append("\"LicenseKey\": \"" + Password + "\",");
                PreBookRQ.Append("\"ClientID\": \"" + ID + "\"},");
                PreBookRQ.Append("\"OccupancyDetails\": [");
                for (int i = 0; i < NoOfRoom; i++)
                {
                    PreBookRQ.Append("{\"ChildCount\": " + NoOfChild + ",");
                    PreBookRQ.Append("\"AdultCount\": " + NoOfAdults + ",");
                    if (NoOfChild == 1)
                        PreBookRQ.Append("\"ChildAgeDetails\": [" + ChildAge1 + "],");
                    if (NoOfChild == 2)
                        PreBookRQ.Append("\"ChildAgeDetails\": [" + ChildAge1 + "," + ChildAge2 + " ],");
                    PreBookRQ.Append("\"RoomNum\": " + (i + 1) + "}");
                    if ((i + 1) != NoOfRoom)
                        PreBookRQ.Append(",");
                }
                PreBookRQ.Append("],");
                PreBookRQ.Append("\"Currency\": \"" + DidaCurrency + "\",");
                PreBookRQ.Append("\"Nationality\": \"" + Nationality + "\",");
                PreBookRQ.Append("\"RatePlanID\": \"" + RCode + "\"}");


                Req = PreBookRQ.ToString();
                if (isForBooking == false)// hit if not from booking
                {
                    dtCheckPriceReqStructure.Rows.Add(_commonMethod.AddCheckPriceReq(dtCheckPriceReqStructure.NewRow(), j + 1, UserCartId, UserCartDetailId, RCode, "Dida", Req));
                }

                string Action = URL + Convert.ToString(_supplierConfig.Dida.Dida_preBooking_Action);

                System.Diagnostics.Stopwatch timerroomprocess = new System.Diagnostics.Stopwatch();
                timerroomprocess.Start();

                ResponseDto responseDto = await GetDidaResponse(Req, Action, "", Searchkey);
                ResponseData = responseDto.Response;
                Error = responseDto.Error;

                timerroomprocess.Stop();
                time = timerroomprocess.Elapsed.ToString();

                _commonMethod.ThirdPartyBookingHistory(Convert.ToString(UniqueNo), Convert.ToString(AgentUniqueNo), Req, ResponseData + Error, "After Get Price Dida Request hit booking" + j + "isForBooking" + isForBooking, "Dida", "Response Confirm", "-", "WithinEarth");
                if (ResponseData == string.Empty || !ResponseData.Contains("Success"))
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
                    if (ResponseData.Contains("No available room"))
                    {
                        priceCheckDidaDto.Flag = 4;
                        return priceCheckDidaDto;
                    }
                    else
                        return priceCheckDidaDto;
                }
                //ResponseData = Newtonsoft.Json.JsonConvert.DeserializeXmlNode(ResponseData).InnerXml;
                ResponseData = Newtonsoft.Json.JsonConvert.DeserializeXmlNode(ResponseData, "root").InnerXml; //#04-NOV-2024 K2
                DataSet DidaResponse = _commonMethod.GetDataSetFromXMLString(ResponseData);
                DataRow[] drRes = DidaResponse.Tables["PriceDetails"].Select();
                double TotalPrice = 0;
                for (int i = 0; i < drRes.Length; i++)
                {
                    ReferenceNo = Convert.ToString(drRes[i]["ReferenceNo"]);
                    priceCheckDidaDto.ReferenceNo = ReferenceNo;

                    int PriceDetails_Id = Convert.ToInt32(drRes[i]["PriceDetails_Id"]);
                    for (int a = 0; a < DidaResponse.Tables["HotelList"].Rows.Count; a++)
                    {
                        string HotelId = Convert.ToString(DidaResponse.Tables["HotelList"].Rows[a]["HotelId"]);
                        int HotelList_Id = Convert.ToInt32(DidaResponse.Tables["HotelList"].Rows[a]["HotelList_Id"]);
                        TotalPrice = Convert.ToDouble(DidaResponse.Tables["HotelList"].Rows[a]["TotalPrice"]);
                        for (int b = 0; b < DidaResponse.Tables["RatePlanList"].Rows.Count; b++)
                        {
                            int RatePlanList_Id = Convert.ToInt32(DidaResponse.Tables["RatePlanList"].Rows[b]["RatePlanList_Id"]);
                            int RoomStatus = Convert.ToInt32(DidaResponse.Tables["RatePlanList"].Rows[b]["RoomStatus"]);
                            if (RoomStatus != 1)
                            {
                                priceCheckDidaDto.Flag = 4;
                                return priceCheckDidaDto;
                            }
                            int StandardOccupancy = Convert.ToInt32(DidaResponse.Tables["RatePlanList"].Rows[b]["StandardOccupancy"]);
                            int InventoryCount = Convert.ToInt32(DidaResponse.Tables["RatePlanList"].Rows[b]["InventoryCount"]);
                            int MaxOccupancy = Convert.ToInt32(DidaResponse.Tables["RatePlanList"].Rows[b]["MaxOccupancy"]);
                            //string RoomTypeId = Convert.ToString(DidaResponse.Tables["RatePlanList"].Rows[b]["RoomTypeId"]);
                            string Currency = Convert.ToString(DidaResponse.Tables["RatePlanList"].Rows[b]["Currency"]);
                            if (Currency.ToLower() != Convert.ToString(DidaCurrency).ToLower())
                            {
                                priceCheckDidaDto.Flag = 3;
                                return priceCheckDidaDto;
                            }
                            string RatePlanId = Convert.ToString(DidaResponse.Tables["RatePlanList"].Rows[b]["RatePlanId"]);
                            DataRow[] drRoomOcc = DidaResponse.Tables["RoomOccupancy"].Select("RatePlanList_Id='" + RatePlanList_Id + "'");
                            for (int c = 0; c < drRoomOcc.Length; c++)
                            {
                                int RoomNum = Convert.ToInt32(drRoomOcc[c]["RoomNum"]);
                                int AdultCount = Convert.ToInt32(drRoomOcc[c]["AdultCount"]);
                                int ChildCount = Convert.ToInt32(drRoomOcc[c]["ChildCount"]);
                            }
                        }
                        DataRow[] drCancellationPolicyList = DidaResponse.Tables["CancellationPolicyList"].Select("HotelList_Id='" + HotelList_Id + "'");
                        for (int d = 0; d < drCancellationPolicyList.Length; d++)
                        {
                            double Amount = Convert.ToDouble(drCancellationPolicyList[d]["Amount"]);
                            DateTime FromDate = Convert.ToDateTime(drCancellationPolicyList[d]["FromDate"]).Date;
                            if (FromDate.Date != Convert.ToDateTime(CDate).Date)//If Policy is change at Prebook step then we Remove from cart
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

                                priceCheckDidaDto.Flag = 4;
                                return priceCheckDidaDto;
                            }
                            break;
                        }
                    }
                }
                FinalPrice = TotalPrice * CurrencyFactorDida;
                if (Math.Round(Rate, 2) != Math.Round(FinalPrice, 2))
                {
                    //try // comment on 16-SEP-2024
                    //{
                    //    responsetype = "GeneralError";
                    //    string abc = Common.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, Req, ResponseData, Error);
                    //}
                    //catch (Exception e)
                    //{
                    //    Common.Error(e, Searchkey);
                    //}

                    priceCheckDidaDto.Flag = 5;
                    priceCheckDidaDto.FinalPrice = FinalPrice;
                    return priceCheckDidaDto;
                }
            }
            catch (Exception ex)
            {
                try
                {
                    Error = ex.Message + ex.StackTrace.ToString();
                    responsetype = "GeneralError";
                    string abc =  await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, Req, ResponseData, Error);
                }
                catch (Exception e)
                {
                    _ = _exception.Error(e, Searchkey);
                }
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
            priceCheckDidaDto.FinalPrice = FinalPrice;
            return priceCheckDidaDto;
        }

        public string DidaBookingRequest(DataSet dsCartDetails, DataTable dtPassengers, int i, int j, string ClientID, string UserName, string Password, int UniqueNo, string PreBookRef, int NoOfRoom, string AgentCode)
        {
            try
            {
                //j=Room & i = Hotel
                int NoOfChild = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[j]["NoOfChild"]);
                int NoOfAdults = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[j]["NoOfAdults"]);
                string checkindate = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["CheckInDate"]);
                string checkin = (checkindate).Substring(0, 4) + "-" + (checkindate).Substring(4, 2) + "-" + (checkindate).Substring(6, 2);// + "T03:00:00+03:00";
                string checkoutdate = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["CheckOutDate"]);
                string checkout = (checkoutdate).Substring(0, 4) + "-" + (checkoutdate).Substring(4, 2) + "-" + (checkoutdate).Substring(6, 2);// + "T03:00:00+03:00";

                string AgentReference = "WE_" + Convert.ToString(AgentCode + "_" + UniqueNo);
                string firstName = ""; string lastName = ""; string title = "";
                string BookingRQ = "";

                int Rooms = NoOfRoom;//Convert.ToInt32(dtPassengers.DefaultView.Count);
                StringBuilder SBPassanger = new StringBuilder();
                StringBuilder SBContect = new StringBuilder();
                for (int r = 0; r < Rooms; r++)//no of room loop
                {
                    dtPassengers.DefaultView.RowFilter = "PickUp='" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomTypeId"]) + "' and (RoomNo=" + (r + 1) + ")";
                    dtPassengers.DefaultView.Sort = "IsLead DESC";
                    string SpecialRequest = Convert.ToString(dtPassengers.DefaultView[0]["Message"]).Trim();
                    SBPassanger.Append("{\"RoomNum\": " + (r + 1) + ",");
                    SBPassanger.Append("\"GuestInfo\": [");
                    for (int k = 0; k < dtPassengers.DefaultView.Count; k++)
                    {
                        DataRow[] drPassengers = dtPassengers.DefaultView.ToTable().Select("IsLead='1'");
                        if (drPassengers.Length == 0)
                        {
                            if (Convert.ToString(dtPassengers.DefaultView[k]["PaxType"]).Trim().ToLower() == "adult")
                                dtPassengers.DefaultView[k]["IsLead"] = 1;
                        }
                        firstName = Convert.ToString(dtPassengers.DefaultView[k]["FirstName"]).Trim();
                        lastName = Convert.ToString(dtPassengers.DefaultView[k]["LastName"]).Trim();
                        title = Convert.ToString(dtPassengers.DefaultView[k]["Prefix"]).Trim().Replace(".", "");
                        int ChildAge = Convert.ToInt32(dtPassengers.DefaultView[k]["ChildAge"]);
                        if (Convert.ToInt16(dtPassengers.DefaultView[k]["IsLead"]) == 1)
                        {
                            SBContect.Append("\"Contact\": {");
                            SBContect.Append("\"Name\": {");
                            SBContect.Append("\"Last\": \"" + lastName + "\",");
                            SBContect.Append("\"First\": \"" + firstName + "\"");
                            SBContect.Append("},");
                            SBContect.Append("\"Email\": \"" + _supplierConfig.Dida.email + "\",");
                            SBContect.Append("\"Phone\": \"" + _supplierConfig.Dida.WorkPhone + "\"");
                            SBContect.Append("},");
                        }
                        SBPassanger.Append("{");
                        if (Convert.ToString(dtPassengers.DefaultView[k]["PaxType"]).Trim().ToLower() == "adult")
                            SBPassanger.Append("\"IsAdult\": true,");
                        else
                            SBPassanger.Append("\"IsAdult\": false,");
                        SBPassanger.Append("\"Name\": {");
                        SBPassanger.Append("\"Last\": \"" + lastName + "\",");
                        SBPassanger.Append("\"First\": \"" + firstName + "\"");
                        SBPassanger.Append("}");
                        if (Convert.ToString(dtPassengers.DefaultView[k]["PaxType"]).Trim().ToLower() != "adult")
                        {
                            int ChildAge1 = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[j]["ChildAge1"]);
                            int ChildAge2 = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[j]["ChildAge2"]);
                            if (NoOfChild == 1)
                                SBPassanger.Append(",\"Age\":" + ChildAge);
                            if (NoOfChild == 2)
                                SBPassanger.Append(",\"Age\":" + ChildAge);
                        }
                        SBPassanger.Append("}");

                        if ((k + 1) != dtPassengers.DefaultView.Count)
                            SBPassanger.Append(",");
                    }
                    SBPassanger.Append("]}");
                    if ((r + 1) != Rooms)
                        SBPassanger.Append(",");
                }
                StringBuilder BookRQ = new StringBuilder();
                BookRQ.Append("{\"CheckOutDate\": \"" + checkout + "\",");
                BookRQ.Append("\"CheckInDate\": \"" + checkin + "\",");
                BookRQ.Append("\"NumOfRooms\": " + Rooms + ",");
                BookRQ.Append("\"Header\": {");
                BookRQ.Append("\"LicenseKey\": \"" + Password + "\",");
                BookRQ.Append("\"ClientID\": \"" + UserName + "\"},");
                BookRQ.Append("\"GuestList\": [");
                BookRQ.Append(SBPassanger.ToString());

                BookRQ.Append("],");
                BookRQ.Append(SBContect.ToString());

                BookRQ.Append("\"ClientReference\": \"" + AgentReference + "\",");
                BookRQ.Append("\"ReferenceNo\": \"" + PreBookRef + "\"");
                BookRQ.Append("}");

                BookingRQ = BookRQ.ToString();
                return BookingRQ;
            }
            catch (Exception ex)
            {
                _= _exception.Error(ex);
                return "";
            }
        }
        public string DidaBookingRequest_newversion(BookRequest_newversion bookRequest ,DataTable dtPassengers, int i, int j, string ClientID, string UserName, string Password, int UniqueNo, string PreBookRef, int NoOfRoom, string AgentCode)
        {
            try
            {
                //j=Room & i = Hotel
                int NoOfChild = Convert.ToInt32(bookRequest.userCart.UserCartDetails[j].NoofChild);
                int NoOfAdults = Convert.ToInt32(bookRequest.userCart.UserCartDetails[j].NoofAdult);
                string checkindate = Convert.ToString(bookRequest.userCart.UserCartDetails[j].CheckInDate);
                //string checkin = (checkindate).Substring(0, 4) + "-" + (checkindate).Substring(4, 2) + "-" + (checkindate).Substring(6, 2);// + "T03:00:00+03:00";
                string checkoutdate = Convert.ToString(bookRequest.userCart.UserCartDetails[j].CheckOutDate);
                //string checkout = (checkoutdate).Substring(0, 4) + "-" + (checkoutdate).Substring(4, 2) + "-" + (checkoutdate).Substring(6, 2);// + "T03:00:00+03:00";

                // Parse the strings to DateTime
                DateTime checkInDate = DateTime.Parse(checkindate);
                DateTime checkOutDate = DateTime.Parse(checkoutdate);

                // Convert to "yyyy-MM-dd" format
                string checkin = checkInDate.ToString("yyyy-MM-dd");
                string checkout = checkOutDate.ToString("yyyy-MM-dd");


                string AgentReference = "WE_" + Convert.ToString(AgentCode + "_" + UniqueNo);
                string firstName = ""; string lastName = ""; string title = "";
                string BookingRQ = "";

                int Rooms = NoOfRoom;//Convert.ToInt32(dtPassengers.DefaultView.Count);
                StringBuilder SBPassanger = new StringBuilder();
                StringBuilder SBContect = new StringBuilder();
                for (int r = 0; r < Rooms; r++)//no of room loop
                {
                    dtPassengers.DefaultView.RowFilter = "PickUp='" + Convert.ToString(bookRequest.userCart.UserCartDetails[j].RoomTypeId) + "' and (RoomNo=" + (r + 1) + ")";
                    dtPassengers.DefaultView.Sort = "IsLead DESC";
                    string SpecialRequest = Convert.ToString(dtPassengers.DefaultView[0]["Message"]).Trim();
                    SBPassanger.Append("{\"RoomNum\": " + (r + 1) + ",");
                    SBPassanger.Append("\"GuestInfo\": [");
                    for (int k = 0; k < dtPassengers.DefaultView.Count; k++)
                    {
                        DataRow[] drPassengers = dtPassengers.DefaultView.ToTable().Select("IsLead='1'");
                        if (drPassengers.Length == 0)
                        {
                            if (Convert.ToString(dtPassengers.DefaultView[k]["PaxType"]).Trim().ToLower() == "adult")
                                dtPassengers.DefaultView[k]["IsLead"] = 1;
                        }
                        firstName = Convert.ToString(dtPassengers.DefaultView[k]["FirstName"]).Trim();
                        lastName = Convert.ToString(dtPassengers.DefaultView[k]["LastName"]).Trim();
                        title = Convert.ToString(dtPassengers.DefaultView[k]["Prefix"]).Trim().Replace(".", "");
                        int ChildAge = Convert.ToInt32(dtPassengers.DefaultView[k]["ChildAge"]);
                        //int ChildAge1 =Convert.ToInt32( ChildAge.Split(',')[0]);
                        //int ChildAge2 =Convert.ToInt32( ChildAge.Split(',')[1]);
                        if (Convert.ToInt16(dtPassengers.DefaultView[k]["IsLead"]) == 1)
                        {
                            SBContect.Append("\"Contact\": {");
                            SBContect.Append("\"Name\": {");
                            SBContect.Append("\"Last\": \"" + lastName + "\",");
                            SBContect.Append("\"First\": \"" + firstName + "\"");
                            SBContect.Append("},");
                            SBContect.Append("\"Email\": \"" + _supplierConfig.Dida.email + "\",");
                            SBContect.Append("\"Phone\": \"" + _supplierConfig.Dida.WorkPhone + "\"");
                            SBContect.Append("},");
                        }
                        SBPassanger.Append("{");
                        if (Convert.ToString(dtPassengers.DefaultView[k]["PaxType"]).Trim().ToLower() == "adult")
                            SBPassanger.Append("\"IsAdult\": true,");
                        else
                            SBPassanger.Append("\"IsAdult\": false,");
                        SBPassanger.Append("\"Name\": {");
                        SBPassanger.Append("\"Last\": \"" + lastName + "\",");
                        SBPassanger.Append("\"First\": \"" + firstName + "\"");
                        SBPassanger.Append("}");
                        if (Convert.ToString(dtPassengers.DefaultView[k]["PaxType"]).Trim().ToLower() != "adult")
                        {
                           // int ChildAge1 = Convert.ToInt32(bookRequest.userCart.UserCartDetails[j].ChildAge1);
                            //int ChildAge2 = Convert.ToInt32(bookRequest.userCart.UserCartDetails[j].ChildAge2);
                            if (NoOfChild == 1)
                                SBPassanger.Append(",\"Age\":" + ChildAge);
                            if (NoOfChild == 2)
                                SBPassanger.Append(",\"Age\":" + ChildAge);
                        }
                        SBPassanger.Append("}");

                        if ((k + 1) != dtPassengers.DefaultView.Count)
                            SBPassanger.Append(",");
                    }
                    SBPassanger.Append("]}");
                    if ((r + 1) != Rooms)
                        SBPassanger.Append(",");
                }
                StringBuilder BookRQ = new StringBuilder();
                BookRQ.Append("{\"CheckOutDate\": \"" + checkout + "\",");
                BookRQ.Append("\"CheckInDate\": \"" + checkin + "\",");
                BookRQ.Append("\"NumOfRooms\": " + Rooms + ",");
                BookRQ.Append("\"Header\": {");
                BookRQ.Append("\"LicenseKey\": \"" + Password + "\",");
                BookRQ.Append("\"ClientID\": \"" + UserName + "\"},");
                BookRQ.Append("\"GuestList\": [");
                BookRQ.Append(SBPassanger.ToString());

                BookRQ.Append("],");
                BookRQ.Append(SBContect.ToString());

                BookRQ.Append("\"ClientReference\": \"" + AgentReference + "\",");
                BookRQ.Append("\"ReferenceNo\": \"" + PreBookRef + "\"");
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
        public string DidaPreCancelltionReq(string BookingId, string Id, string password, string url)
        {
            try
            {
                StringBuilder sbReq = new StringBuilder();

                sbReq.Append("{\"Header\": {");
                sbReq.Append("\"LicenseKey\": \"" + password + "\",");
                sbReq.Append("\"ClientID\": \"" + Id + "\"");
                sbReq.Append("},");
                sbReq.Append("\"BookingID\": \"" + BookingId + "\"");
                sbReq.Append("}");
                return sbReq.ToString();
            }
            catch (Exception ex)
            {
                _ = _exception.ErrorNew(ex, _LogAPI.CancelBooking);
                return "";
            }
        }

        public string DidaCancelConfirmReq(string BookingId, string ConfirmId, string Id, string password)
        {
            try
            {
                string Req = "";
                StringBuilder sbReq = new StringBuilder();

                sbReq.Append("{\"Header\": {");
                sbReq.Append("\"LicenseKey\": \"" + password + "\",");
                sbReq.Append("\"ClientID\": \"" + Id + "\"");
                sbReq.Append("},");
                sbReq.Append("\"BookingID\": \"" + BookingId + "\",");
                sbReq.Append("\"ConfirmID\": \"" + ConfirmId + "\"");
                sbReq.Append("}");
                Req = sbReq.ToString();

                return Req;
            }
            catch (Exception ex)
            {
                _ = _exception.ErrorNew(ex, _LogAPI.CancelBooking);
                return "";
            }
        }
    }
}
