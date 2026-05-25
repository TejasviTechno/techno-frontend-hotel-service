using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using XConnect.CommonUtilities;
using XConnect.CommonUtilities.Interface;
using XConnect.ErrorLogHandler.Interface;
using XConnect.Markup.Interface;
using XConnect.Models;
using XConnect.Models.Hotel;
using XConnect.Supplier.Parsec.Interface;
using static Azure.Core.HttpHeader;
using Options = XConnect.Models.Options;

namespace XConnect.Supplier.Parsec
{
    public class ParsecRepo : IParsec
    {
        private readonly ICommonMethod _commonMethod;
        private readonly IException _exception;
        private readonly IMarkup _markup;
        private readonly ISupplierRes _supplierRes;
        private readonly KeySettings _keySettings;
        
        public ParsecRepo(ICommonMethod commonMethod, IMarkup markup, ISupplierRes supplierRes, IException exception, IOptions<KeySettings> keySettings  )
        {
            _commonMethod = commonMethod;
            _markup = markup;
            _supplierRes = supplierRes;
            _exception = exception;
            _keySettings = keySettings.Value;
            
        }
        public void GetParsesRequestOnly_Async(DataRow[] commonCode, List<StarRating> listRating, DataTable dtROE, HotelSearchParameter SearchPara, List<SupplierDetails> lstParsecDetail, List<SupplierSearchConfig> lstParsecConfig, DataTable dtPreferredDataHotel, ref ConcurrentBag<HotelX> AllTask, int Hit, DataSet dtXMLCity, TokenData tokenData)
        {


            if (lstParsecDetail != null && lstParsecDetail.Count > 0)
            {
                List<StarRating> lstRating = new List<StarRating>();
                lstRating = _commonMethod.GetStarRatingStringList_xtg(SearchPara);

                List<HotelCodeByCity> DtHotelDAta = HtmlExtensions.ToList<HotelCodeByCity>(commonCode.CopyToDataTable());
                int ratingcount = lstRating.Count;
                int hotelcount = DtHotelDAta.Count;

                foreach (SupplierDetails ParsecDetail in lstParsecDetail)
                {
                    try
                    {
                        int SupplierID = ParsecDetail.SupplierID;
                        string Supplier = ParsecDetail.SupplierName;


                        if (commonCode != null)
                        {
                            if (lstParsecConfig.Count > 0)
                            {
                                List<SupplierSearchConfig> ParsecConfig = lstParsecConfig
                                    .Where(x => x != null && x.SupplierID == SupplierID)
                                .ToList();

                                if (ParsecConfig != null && ParsecConfig.Count > 0)
                                {
                                    DataForSearch dataForSearch = _commonMethod.GetXmlDataForSearchTechwise(SearchPara.Nationality, dtXMLCity, Supplier, ParsecDetail.TechName);

                                    if (dataForSearch.XmlNationality != "")
                                    {
                                        #region Parsec

                                        string Ids = string.Empty;
                                        string xmlbase = "";
                                        _commonMethod.GetPrefferedHotelCodes(ref Ids, ref xmlbase, ref ParsecConfig, Supplier, dtPreferredDataHotel, SearchPara.IsSearchFor);

                                        List<HotelCodeByCity> filter = new List<HotelCodeByCity>();
                                        filter = _commonMethod.FilterWithOutCity_Code(ratingcount, hotelcount, DtHotelDAta, lstRating, Supplier);


                                        string Currency = ParsecDetail.CurrencyShortName;
                                        double CurrencyFactorParsec = 1;
                                        if (Currency.ToLower() != tokenData.SiteCurrency.ToLower())
                                        {
                                            CurrencyFactorParsec = _commonMethod.GetROEByCurrency(Currency, dtROE);
                                        }

                                        string URL = "";


                                        for (int k = 0; k < ParsecConfig.Count; k++)
                                        {
                                            bool DebugMode = (Convert.ToInt32(ParsecConfig[k].DebugMode) == 0 ? false : true);


                                            if (Convert.ToInt32(ParsecConfig[k].CityID) == 0 || Convert.ToInt32(ParsecConfig[k].CityID) == SearchPara.CityID)//All City means 0
                                            {
                                                if (filter.Count > 0)
                                                {
                                                    int NoOfRecord = Convert.ToInt32(ParsecConfig[k].Noofrecords);
                                                    double count = Convert.ToDouble(filter.Count);
                                                    int NoOfDiv = Convert.ToInt32(Math.Ceiling(count / Convert.ToDouble(NoOfRecord)));
                                                    List<List<XMLID_Search>> lstxml = _commonMethod.GetXMLIDString(NoOfDiv, NoOfRecord, count, filter);

                                                    for (int a = 0; a < NoOfDiv; a++)
                                                    {

                                                        string IdsForSearch = Convert.ToString(_commonMethod.GetHotelCodeByCOmma(lstxml[a]));

                                                        if (!string.IsNullOrEmpty(IdsForSearch))
                                                        {
                                                            //string IdsForSearch = Convert.ToString(paramValues1);
                                                            HotelX xhotel = new HotelX();
                                                            xhotel.Supplier = Supplier;
                                                            xhotel.DebugeMode = DebugMode;
                                                            xhotel.SearchTimeOut = Convert.ToInt32(ParsecConfig[k].HotelSearchTimeOut);
                                                            //if (Hit > 0)
                                                            xhotel.ProcessTimeout = Convert.ToInt32(ParsecConfig[k].HotelSearchTimeOut2);



                                                            if (Supplier.ToLower() == ParsecInfo.IntraTour)
                                                            {
                                                                xhotel.URL = ParsecInfo.IntraTourHotelSearch; //already use http url
                                                            }
                                                            else
                                                            {
                                                                if (_keySettings.SupplierProxy == "0")
                                                                    xhotel.URL = Convert.ToString(ParsecDetail.WebSiteURL) + ParsecInfo.HotelSearch;
                                                                else
                                                                    xhotel.URL = Convert.ToString(ParsecDetail.WebSiteURLforAPI) + ParsecInfo.HotelSearch;

                                                            }

                                                            // xhotel.URL = Convert.ToString(ParsecDetail.WebSiteURL);

                                                            xhotel.UserId = Convert.ToString(ParsecDetail.UserID);
                                                            xhotel.UserName = Convert.ToString(ParsecDetail.UserName);
                                                            xhotel.PassWord = Convert.ToString(ParsecDetail.Password);
                                                            xhotel.CurrencyCode = Currency;
                                                            xhotel.CurrencyFactor = CurrencyFactorParsec;
                                                            xhotel.XmlNationality = dataForSearch.XmlNationality;
                                                            xhotel.XmlCity = dataForSearch.xmlCity;
                                                            xhotel.XmlCountry = dataForSearch.xmlCountry;


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
                                                                objrq.Request = XMLParsecRequestOnly_Async(Convert.ToString(ParsecDetail.UserName), Convert.ToString(ParsecDetail.Password), Convert.ToString(ParsecDetail.UserID), SearchPara, IdsForSearch, DebugMode, xhotel.XmlNationality, xhotel.XmlCity, Currency);
                                                                objrq.Response = ""; RQRS.Add(objrq);
                                                            }
                                                            xhotel.RQRS = RQRS;
                                                            xhotel.ReturnRooms = Convert.ToInt32(ParsecDetail.ReturnRooms);
                                                            xhotel.TechName = Convert.ToString(ParsecDetail.TechName);
                                                            #endregion
                                                            AllTask.Add(xhotel);
                                                        }

                                                    }
                                                }
                                            }
                                            else
                                            {
                                                HotelX xhotel = new HotelX();
                                                xhotel.Supplier = Supplier;
                                                xhotel.DebugeMode = DebugMode;
                                                xhotel.SearchTimeOut = Convert.ToInt32(ParsecConfig[k].HotelSearchTimeOut);
                                                xhotel.ProcessTimeout = Convert.ToInt32(ParsecConfig[k].HotelSearchTimeOut2);

                                                if (Supplier.ToLower() == ParsecInfo.IntraTour)
                                                {
                                                    xhotel.URL = ParsecInfo.IntraTourHotelSearch;
                                                }
                                                else
                                                {
                                                    if (_keySettings.SupplierProxy == "0")
                                                        xhotel.URL = Convert.ToString(ParsecDetail.WebSiteURL) + ParsecInfo.HotelSearch;
                                                    else
                                                        xhotel.URL = Convert.ToString(ParsecDetail.WebSiteURLforAPI) + ParsecInfo.HotelSearch;
                                                }

                                                xhotel.UserId = Convert.ToString(ParsecDetail.UserID);
                                                xhotel.UserName = Convert.ToString(ParsecDetail.UserName);
                                                xhotel.PassWord = Convert.ToString(ParsecDetail.Password);
                                                xhotel.CurrencyCode = Currency;
                                                xhotel.CurrencyFactor = CurrencyFactorParsec;
                                                xhotel.XmlNationality = dataForSearch.XmlNationality;
                                                xhotel.XmlCity = dataForSearch.xmlCity;
                                                xhotel.XmlCountry = dataForSearch.xmlCountry;


                                                #region "CoomonCode"
                                                bool IsHitSeperate = _commonMethod.IsSuppliersWithSeperateRoomRquest(xhotel.Supplier);
                                                int NoOfRoomsSearch = 1;
                                                if (IsHitSeperate)
                                                    NoOfRoomsSearch = SearchPara.Rooms.Count;
                                                List<RQRS> RQRS = new List<RQRS>();
                                                xhotel.IsSeperate = IsHitSeperate;


                                                string Hotelid = "";
                                                if (SearchPara.Filters.IsShowRooms == 1)
                                                {
                                                    int NoOfRecord = Convert.ToInt32(ParsecConfig[k].Noofrecords);
                                                    double count = Convert.ToDouble(filter.Count);
                                                    int NoOfDiv = Convert.ToInt32(Math.Ceiling(count / Convert.ToDouble(NoOfRecord)));
                                                    List<List<XMLID_Search>> lstxml = _commonMethod.GetXMLIDString(NoOfDiv, NoOfRecord, count, filter);

                                                    for (int aa = 0; aa < NoOfDiv; aa++)
                                                    {
                                                        string IdsForSearch = Convert.ToString(_commonMethod.GetHotelCodeByCOmma(lstxml[aa]));
                                                        Hotelid = IdsForSearch;
                                                    }
                                                }

                                                for (int rr = 0; rr < NoOfRoomsSearch; rr++)
                                                {
                                                    RQRS objrq = new RQRS();
                                                    List<RoomDetails> serchRooms = new List<RoomDetails>();
                                                    serchRooms.Add(SearchPara.Rooms[rr]);
                                                    objrq.Request = XMLParsecRequestOnly_Async(Convert.ToString(ParsecDetail.UserName), Convert.ToString(ParsecDetail.Password), Convert.ToString(ParsecDetail.UserID), SearchPara, Hotelid, DebugMode, xhotel.XmlNationality, xhotel.XmlCity, Currency);
                                                    objrq.Response = ""; RQRS.Add(objrq);
                                                }
                                                xhotel.RQRS = RQRS;
                                                #endregion
                                                AllTask.Add(xhotel);

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
                        _exception.Error(ex);
                    }
                }
            }
        }
        public string XMLParsecRequestOnly_Async(string UserName, string Password, string UserID, HotelSearchParameter SearchParam, string hotelIds, bool DebugMode, string Nationality, string citycode, string Currency)
        {
            try
            {
                int isHit = 0;
                return GetParsecHotelListRequest(SearchParam, Nationality, UserName, Password, UserID, hotelIds, isHit, citycode, Currency);
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
                return "";
            }
            finally
            {
                //GC.Collect();
            }
        }
        public string GetParsecHotelListRequest(HotelSearchParameter searchParam, string Nationality, string UserName, string Password, string UserID, string hotelIds, int IsHit, string Citycode, string Currency)
        {
            try
            {
                string checkIn = String.Format("{0:yyyy-MM-dd}", DateTime.ParseExact(searchParam.CheckInDate, "dd/MM/yyyy", null));
                string checkOut = String.Format("{0:yyyy-MM-dd}", DateTime.ParseExact(searchParam.CheckOutDate, "dd/MM/yyyy", null));
                StringBuilder roomstring = new StringBuilder();
                for (int i = 0; i < searchParam.Rooms.Count; i++)
                {
                    int TotalAdult = searchParam.Rooms[i].NoofAdults;
                    int TotalChild = searchParam.Rooms[i].NoofChild;
                    roomstring.Append("<RoomCandidate RPH=\"" + (i + 1) + "\">");
                    roomstring.Append("<Guests>");
                    roomstring.Append("<Guest AgeCode=\"A\" Count=\"" + TotalAdult + "\"/>");
                    if (TotalChild >= 1)
                    {
                        switch (TotalChild)
                        {
                            case 1:
                                roomstring.Append("<Guest AgeCode=\"C\" Count=\"1\" Age=\"" + Convert.ToString(searchParam.Rooms[i].Child1Age) + "\"/>");
                                break;
                            case 2:
                                roomstring.Append("<Guest AgeCode=\"C\" Count=\"1\" Age=\"" + Convert.ToString(searchParam.Rooms[i].Child1Age) + "\"/>" + "<Guest AgeCode=\"C\" Count=\"1\" Age=\"" + Convert.ToString(searchParam.Rooms[i].Child2Age) + "\"/>");
                                break;
                        }
                    }
                    roomstring.Append("</Guests>");
                    roomstring.Append("</RoomCandidate>");
                }
                StringBuilder Perses = new StringBuilder();
                Perses.Append("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
                Perses.Append("<soap-env:Envelope xmlns:soap-env=\"http://schemas.xmlsoap.org/soap/envelope/\">");
                Perses.Append("<soap-env:Header>");
                Perses.Append("<wsse:Security xmlns:wsse=\"http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd\">");
                Perses.Append("<wsse:Username>" + UserName + "</wsse:Username>");
                Perses.Append("<wsse:Password>" + Password + "</wsse:Password>");
                Perses.Append("<Context>" + UserID + "</Context>");
                Perses.Append("</wsse:Security>");
                Perses.Append("</soap-env:Header>");
                Perses.Append("<soap-env:Body>");
                Perses.Append("<OTA_HotelAvailRQ xmlns=\"http://parsec.es/hotelapi/OTA2014Compact\" RateDetails=\"1\" DetailLevel=\"1\">");
                Perses.Append("<HotelSearch>");
                Perses.Append("<Currency Code=\"" + Currency + "\"/>");
                if (hotelIds != "")
                {
                    // hotelIds = "16";//"14,16,17,18";
                    Perses.Append("<HotelRef HotelCode=\"" + hotelIds + "\"/>");
                }
                else
                {
                    Perses.Append("<HotelLocation CityCode=\"" + Citycode + "\"/>");
                }
                Perses.Append("<DateRange Start=\"" + checkIn + "\" End=\"" + checkOut + "\"/>");
                Perses.Append("<GuestCountry Code=\"" + Nationality + "\"/>");
                Perses.Append("<RoomCandidates>");
                Perses.Append(roomstring.ToString());
                Perses.Append("</RoomCandidates>");
                Perses.Append("</HotelSearch>");
                Perses.Append("</OTA_HotelAvailRQ>");
                Perses.Append("</soap-env:Body>");
                Perses.Append("</soap-env:Envelope>");
                string AbreuREQ = Perses.ToString();
                return AbreuREQ;
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
                return "";
            }
        }
        public async Task<List<Options>> Parsec_HotelSearchWithRooms(HotelX Item, CommonSearch commonSearch, double CurrencyFactor, string Currency, string Response, string XMLCITY, string XMLCOUNTRY, string Supplier, HotelSearchParameter SearchParam, Int32 processTimeout, string ResProccessTimerLogActive, string ResProccessTimeValueforSaveLog, DataTable dtROE)
        {
            System.Diagnostics.Stopwatch timerroomx = new System.Diagnostics.Stopwatch();
            timerroomx.Start();
            List<Options> options = new List<Options>();

            int CutOffDays = CacheMaster.CutOffDays;
            CutOffDays = _commonMethod.GetCutOffDays(commonSearch.dtSuplierMarkup, Item.Supplier);
            DataTable DtHotel = _commonMethod.GenerateDataTableNewV2();
            try
            {
                if (Response == null || Response == string.Empty || Response.ToLower().Contains("error"))
                {
                    Item.SupplierResponseError = Response;
                    Item.ResponseStatus = 204;
                    Item.ResponseType = "NoCount";
                    return options;
                }
                DataSet ResponseData = _commonMethod.GetDataSetFromXMLString(Response);
                if (Convert.ToInt32(ResponseData.Tables["hotels"].Rows[0]["hotelCount"]) == 0)
                {
                    Item.SupplierResponseError = Response;
                    Item.ResponseStatus = 404;
                    Item.ResponseType = "NoCount";
                    return options;
                }
                else
                {
                    for (int hh = 0; hh < ResponseData.Tables["hotel"].Rows.Count; hh++)
                    {
                        Item.ResponseType = "Success";
                        double FinalMinPrice = 0;
                        #region "When not Need Room"  
                        // add hotel code from price only and room code blank here
                        if (SearchParam.Filters.IsShowRooms == 0)
                        {
                            #region "Hotel Code Block "
                            // animesh changes code  21 feb 2019 for price 2 comments
                            DataRow[] info = ResponseData.Tables["info"].Select("Hotel_Id='" + Convert.ToInt32(ResponseData.Tables["hotel"].Rows[hh]["Hotel_Id"]) + "'");
                            if (DtHotel.Select("XMLHotelCode='" + Convert.ToString(info[0]["HotelCode"]) + "'").Length > 0) { continue; }

                            DataRow drXmlHotel = DtHotel.NewRow();
                            drXmlHotel["IsRecomondedHotel"] = false;
                            drXmlHotel["XMLHotelCode"] = Convert.ToString(info[0]["HotelCode"]);
                            drXmlHotel["ContractID"] = 0;
                            // drXmlHotel["UniqueNo"] = ItemCode;
                            drXmlHotel["IsShowSupplier"] = false;
                            drXmlHotel["HotelName"] = "";
                            drXmlHotel["Rating"] = 0;
                            string HotelStatus = string.Empty;
                            drXmlHotel["XmlHotelCity"] = XMLCITY;
                            drXmlHotel["XmlCity"] = XMLCITY;
                            drXmlHotel["XmlCountry"] = XMLCOUNTRY;
                            drXmlHotel["SupplierName"] = Supplier;
                            if (commonSearch.tokenData.AgentId != 1899)
                            {
                                DataRow[] BastPrice = ResponseData.Tables["Bestprice"].Select("Hotel_Id=" + ResponseData.Tables["hotel"].Rows[hh]["Hotel_Id"] + "");
                                FinalMinPrice = Convert.ToDouble(BastPrice[0]["Amount"]);
                                string CurrencyH = Convert.ToString(BastPrice[0]["currency"]);
                                if (CurrencyH.ToLower() != Currency.ToLower())
                                    continue;

                            }
                            else
                            {
                                DataRow[] BastPrice = ResponseData.Tables["Bestprice"].Select("Hotel_Id=" + ResponseData.Tables["hotel"].Rows[hh]["Hotel_Id"] + "");
                                FinalMinPrice = 1;
                                string CurrencyH = Convert.ToString(BastPrice[0]["currency"]);
                                if (CurrencyH.ToLower() != Currency.ToLower())
                                    continue;
                            }
                            drXmlHotel["SellingPrice"] = FinalMinPrice;
                            drXmlHotel["FinalSellingPrice"] = FinalMinPrice;
                            drXmlHotel["BookingStatus"] = HotelStatus;
                            //// if (TotalAmount > 0)
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

                            #region "Hotel Block here"
                            // animesh changes code  21 feb 2019 for price 2 comments
                            DataRow[] info = ResponseData.Tables["info"].Select("Hotel_Id='" + Convert.ToInt32(ResponseData.Tables["hotel"].Rows[hh]["Hotel_Id"]) + "'");
                            if (DtHotel.Select("XMLHotelCode='" + Convert.ToString(info[0]["HotelCode"]) + "'").Length > 0) { continue; }

                            string uniq = Convert.ToString(info[0]["HotelCode"]);
                            DataRow drXmlHotel = DtHotel.NewRow();
                            drXmlHotel["IsRecomondedHotel"] = false;
                            drXmlHotel["XMLHotelCode"] = Convert.ToString(info[0]["HotelCode"]);
                            drXmlHotel["ContractID"] = 0;
                            // drXmlHotel["UniqueNo"] = ItemCode;
                            drXmlHotel["IsShowSupplier"] = false;
                            drXmlHotel["HotelName"] = "";
                            drXmlHotel["Rating"] = 0;
                            string HotelStatus = string.Empty;
                            drXmlHotel["XmlHotelCity"] = XMLCITY;
                            drXmlHotel["XmlCity"] = XMLCITY;
                            drXmlHotel["XmlCountry"] = XMLCOUNTRY;
                            drXmlHotel["SupplierName"] = Supplier;

                            drXmlHotel["SellingPrice"] = FinalMinPrice;
                            drXmlHotel["FinalSellingPrice"] = FinalMinPrice;
                            drXmlHotel["BookingStatus"] = HotelStatus;
                            //// if (TotalAmount > 0)
                            DtHotel.Rows.Add(drXmlHotel);

                            #endregion
                            #region "GetRooms for this hotel"
                            bool willAddOrNot = true;
                            Int64 RaynaID = 0;
                            Int64 GiataId = 0;
                            int RatingHotel = 0;
                            Int64 OwnsystemHotelId = 0;
                            string Provider = Supplier;
                            double MinPrice = 1;
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
                                    int totalAdult = 0;
                                    int totalChild = 0;
                                    for (int b = 0; b < SearchParam.Rooms.Count; b++)
                                    {
                                        totalAdult = Convert.ToInt32(SearchParam.Rooms[b].NoofAdults) + totalAdult;
                                        totalChild = Convert.ToInt32(SearchParam.Rooms[b].NoofChild) + totalChild;
                                    }

                                    int Info_Id = 0;
                                    string Essen = "";
                                    if (ResponseData.Tables["info"].Columns.Contains("Info_Id") == true)
                                    {
                                        Info_Id = Convert.ToInt32(info[0]["Info_Id"]);
                                        if (ResponseData.Tables.Contains("Warnings") == true)
                                        {
                                            if (ResponseData.Tables["Warnings"].Columns.Contains("Info_Id") == true)
                                            {
                                                DataRow[] Warnings = ResponseData.Tables["Warnings"].Select("Info_Id=" + Info_Id + "");
                                                if (Supplier.ToLower() == "intratour")
                                                {
                                                    for (int j = 0; j < Warnings.Length; j++)
                                                    {
                                                        Essen = Essen + Convert.ToString(Warnings[j]["Msg"]);
                                                    }
                                                }
                                                else
                                                {
                                                    int Warnings_Id = Convert.ToInt32(Warnings[0]["Warnings_Id"]);
                                                    DataRow[] Msg = ResponseData.Tables["Msg"].Select("Warnings_Id=" + Warnings_Id + "");
                                                    for (int j = 0; j < Msg.Length; j++)
                                                    {
                                                        if (j == 0)
                                                        {
                                                            Essen = Convert.ToString(Msg[j]["Msg_Text"]) + ".";
                                                        }
                                                        else
                                                        {
                                                            Essen = Essen + Convert.ToString(Msg[j]["Msg_Text"]);
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    //double Amount = Convert.ToDouble(BastPrice[0]["Amount"]);
                                    // string Currency = Convert.ToString(BastPrice[0]["currency"]);
                                    //DataTable dtInrCurrency = Common.GetCountryCurrency(Currency);
                                    // double CurrencyFactor = Convert.ToDouble(dtInrCurrency.Rows[0]["AmtToExch"]);
                                    // double TotalAmount = Amount * CurrencyFactor;
                                    DataRow[] Rooms = ResponseData.Tables["Rooms"].Select("Hotel_Id=" + ResponseData.Tables["hotel"].Rows[hh]["Hotel_Id"] + "");
                                    // int Rooms_Id = Convert.ToInt32(Rooms[0]["Rooms_Id"]);
                                    DataRow[] Room = ResponseData.Tables["Room"].Select("Rooms_Id=" + Rooms[0]["Rooms_Id"] + "");
                                    int RoomCode = 0;
                                    for (int k = 0; k < Room.Length; k++)
                                    {
                                        bool IsCombine = false;
                                        int Room_Id = Convert.ToInt32(Room[k]["Room_Id"]);
                                        string RPH = Convert.ToString(Room[k]["RPH"]);
                                        if (RPH.Contains(",") == true)
                                        {
                                            #region
                                            string[] Arr = RPH.Split(',');
                                            for (int a = 0; a < Arr.Length; a++)
                                            {
                                                RPH = Arr[a];
                                                DataRow[] RoomType = ResponseData.Tables["RoomType"].Select("Room_Id=" + Room_Id + "");
                                                RoomCode = Convert.ToInt32(RoomType[0]["Code"]);
                                                string RoomType_Id = "";
                                                if (ResponseData.Tables["RoomType"].Columns.Contains("RoomType_Id") == true)
                                                {
                                                    RoomType_Id = Convert.ToString(RoomType[0]["RoomType_Id"]);
                                                }
                                                if (ResponseData.Tables.Contains("ExtraInfo") == true)
                                                {
                                                    if (ResponseData.Tables["ExtraInfo"].Columns.Contains("Info_Id") == true)
                                                    {
                                                        DataRow[] ExtraInfo = ResponseData.Tables["ExtraInfo"].Select("RoomType_Id=" + RoomType_Id + "");
                                                        if (ExtraInfo.Length > 0)
                                                        {
                                                            int ExtraInfo_Id = Convert.ToInt32(ExtraInfo[0]["ExtraInfo_Id"]);
                                                            if (ResponseData.Tables.Contains("Msg") == true)
                                                            {
                                                                DataRow[] Msg = ResponseData.Tables["Msg"].Select("ExtraInfo_Id=" + ExtraInfo_Id + "");
                                                                for (int b = 0; b < Msg.Length; b++)
                                                                {
                                                                    if (b == 0)
                                                                    {
                                                                        if (Essen == "")
                                                                        {
                                                                            Essen = Convert.ToString(Msg[b]["Msg_Text"]);
                                                                        }
                                                                        else
                                                                        {
                                                                            Essen = Essen + Convert.ToString(Msg[b]["Msg_Text"]);
                                                                        }
                                                                    }
                                                                    else
                                                                    {
                                                                        Essen = Essen + Convert.ToString(Msg[b]["Msg_Text"]);
                                                                    }
                                                                }
                                                            }
                                                            else
                                                            {
                                                                for (int b = 0; b < ExtraInfo.Length; b++)
                                                                {
                                                                    if (b == 0)
                                                                    {
                                                                        if (Essen == "")
                                                                        {
                                                                            Essen = Convert.ToString(ExtraInfo[b]["Msg"]);
                                                                        }
                                                                        else
                                                                        {
                                                                            Essen = Essen + Convert.ToString(ExtraInfo[b]["Msg"]);
                                                                        }
                                                                    }
                                                                    else
                                                                    {
                                                                        Essen = Essen + Convert.ToString(ExtraInfo[b]["Msg"]);
                                                                    }
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                                string RoomName = Convert.ToString(RoomType[0]["Name"]);
                                                DataRow[] RoomRates = ResponseData.Tables["RoomRates"].Select("Room_Id=" + Room_Id + "");
                                                int RoomRates_Id = Convert.ToInt32(RoomRates[0]["RoomRates_Id"]);
                                                DataRow[] RoomRate = ResponseData.Tables["RoomRate"].Select("RoomRates_Id=" + RoomRates_Id + "");

                                                for (int l = 0; l < RoomRate.Length; l++)
                                                {
                                                    string uniqno = Guid.NewGuid().ToString().Substring(0, 4);
                                                    int RoomRate_Id = Convert.ToInt32(RoomRate[l]["RoomRate_Id"]);
                                                    string MealPlan = Convert.ToString(RoomRate[l]["MealPlan"]);
                                                    string RoomTypeId = Convert.ToString(RoomRate[l]["BookingCode"]) + "$$$" + uniqno;
                                                    RoomTypeId = RoomTypeId.Replace("'", "");
                                                    double RoomAmount = 0;
                                                    #region For One RoomOnly
                                                    DataRow[] Total = ResponseData.Tables["Total"].Select("RoomRate_Id=" + RoomRate_Id + "");
                                                    for (int m = 0; m < Total.Length; m++)
                                                    {
                                                        string RoomCurrency = Convert.ToString(Total[m]["Currency"]);

                                                        double Commision = Convert.ToDouble(Total[0]["Commission"]);
                                                        RoomAmount = Convert.ToDouble(Total[0]["Amount"]) * CurrencyFactor;

                                                        #region Ratebreakup

                                                        #endregion

                                                        #region Cancellation Policy
                                                        DataRow[] CancelPanalties = ResponseData.Tables["CancelPenalties"].Select("RoomRate_Id=" + RoomRate_Id + "");
                                                        int CancelPenalties_Id = Convert.ToInt32(CancelPanalties[0]["CancelPenalties_Id"]);
                                                        DataRow[] CancelPenalty = ResponseData.Tables["CancelPenalty"].Select("CancelPenalties_Id=" + CancelPenalties_Id + "");
                                                        bool ISchecked = true;
                                                        DateTime FromDate = DateTime.Now.Date;
                                                        for (int o = 0; o < CancelPenalty.Length; o++)
                                                        {
                                                            int CancelPenalty_Id = Convert.ToInt32(CancelPenalty[o]["CancelPenalty_Id"]);
                                                            DataRow[] Deadline = ResponseData.Tables["Deadline"].Select("CancelPenalty_Id=" + CancelPenalty_Id + "");
                                                            string TimeUnit = Convert.ToString(Deadline[0]["TimeUnit"]);
                                                            int Unit = Convert.ToInt32(Deadline[0]["Units"]);
                                                            if (TimeUnit.ToLower() == "day")
                                                            {
                                                                double VatonPolicy = 0;
                                                                DataRow[] Charge = ResponseData.Tables["Charge"].Select("CancelPenalty_Id=" + CancelPenalty_Id + "");
                                                                string CancellationCurrency = Convert.ToString(Charge[0]["Currency"]);

                                                                double CancellationAmount = Convert.ToDouble(Charge[0]["Amount"]) * CurrencyFactor;
                                                                #region Vat
                                                                CancellationAmount = _markup.ProcessHotelCancelationPrice_WithMarkup(ref CancellationAmount, SearchParam.NoofNights, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), CancellationAmount, isPerce, commonSearch.B2CMarkup, AgentPercentage, AgentMarkupAmount, commonSearch.IsParentId, commonSearch.SubAgentMarkup, ref VatonPolicy);
                                                                #endregion
                                                                DateTime ToDate = DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null);

                                                                if (Unit == 0)
                                                                {
                                                                    //full charge nonrefundable
                                                                    dtCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref CancellationAmount, dtCancellation.NewRow(), CurrencyFactor, CancellationCurrency, Supplier, Convert.ToString(OwnsystemHotelId), (RPH).ToString(), RoomTypeId, RoomName, Convert.ToString(MealPlan), ToDate, DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null), CancellationAmount, CancellationAmount, RoomAmount.ToString(), VatonPolicy, "", "", "", "", ""));
                                                                    break;
                                                                }
                                                                else
                                                                {
                                                                    ToDate = ToDate.AddDays(CutOffDays - Unit);
                                                                    // refundable
                                                                    if (ToDate > FromDate && ISchecked && o == 0)
                                                                    {
                                                                        double zero = 0;
                                                                        ISchecked = false;
                                                                        dtCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref zero, dtCancellation.NewRow(), CurrencyFactor, CancellationCurrency, Supplier, Convert.ToString(OwnsystemHotelId), (RPH).ToString(), RoomTypeId, RoomName, Convert.ToString(MealPlan), DateTime.Now.Date, ToDate.AddDays(-1).AddHours(23).AddMinutes(59).AddSeconds(59), 0, 0, RoomAmount.ToString(), 0, "", "", "", "", ""));
                                                                    }
                                                                    dtCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref CancellationAmount, dtCancellation.NewRow(), CurrencyFactor, CancellationCurrency, Supplier, Convert.ToString(OwnsystemHotelId), (RPH).ToString(), RoomTypeId, RoomName, Convert.ToString(MealPlan), ToDate, DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null), CancellationAmount, CancellationAmount, RoomAmount.ToString(), VatonPolicy, "", "", "", "", ""));
                                                                }
                                                                if (CancelPenalty.Length > 1)
                                                                {
                                                                    FromDate = ToDate;
                                                                }
                                                            }
                                                            else
                                                            {
                                                                //full charge
                                                            }
                                                        }
                                                        #endregion
                                                    }
                                                    #region Rooms
                                                    DataRow drRoom = dtHotelRoom.NewRow();
                                                    drRoom["OwnsystemHotelId"] = OwnsystemHotelId;
                                                    drRoom["RoomTypeCode"] = RoomCode;
                                                    drRoom["BoardType"] = "";
                                                    drRoom["MealName"] = MealPlan;
                                                    drRoom["MealId"] = MealPlan;
                                                    drRoom["Type"] = "";
                                                    drRoom["Board"] = "";
                                                    drRoom["BookingStatus"] = "Available";
                                                    drRoom["UniqueNo"] = uniq;
                                                    drRoom["ContractID"] = 0;
                                                    drRoom["RoomNo"] = Convert.ToInt32(RPH);
                                                    drRoom["BoardTypeID"] = "";
                                                    drRoom["AdultsOccupancy"] = totalAdult;
                                                    drRoom["ChildOccupancy"] = totalChild;
                                                    drRoom["OfferCode"] = "";
                                                    drRoom["OfferDescription"] = "";
                                                    drRoom["RoomSequenceNo"] = "";
                                                    drRoom["EssentialInformation"] = Essen;
                                                    drRoom["Characteristic"] = "";
                                                    #region Vat
                                                    _markup.ProcessHotelRoom_WithMarkup(CurrencyFactor, Currency, false, IsCombine, SearchParam.NoofNights * SearchParam.Rooms.Count, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), SearchParam.CheckInDate, SearchParam.CheckOutDate, ref RoomAmount, ref drRoom, isPerce, AgentPercentage, AgentMarkupAmount, commonSearch.SubAgentMarkup, commonSearch.B2CMarkup, commonSearch.IsParentId);
                                                    #endregion
                                                    drRoom["SupplierName"] = Supplier;
                                                    drRoom["RoomTypeName"] = RoomName;// (k + 1) + "_" + RoomName;
                                                    drRoom["RoomTypeId"] = RoomTypeId;
                                                    dtHotelRoom.Rows.Add(drRoom);
                                                    #endregion
                                                    #endregion
                                                }
                                            }
                                            #endregion
                                        }
                                        else
                                        {
                                            #region
                                            DataRow[] RoomType = ResponseData.Tables["RoomType"].Select("Room_Id=" + Room_Id + "");
                                            RoomCode = Convert.ToInt32(RoomType[0]["Code"]);
                                            string RoomType_Id = "";
                                            if (ResponseData.Tables["RoomType"].Columns.Contains("RoomType_Id") == true)
                                            {
                                                RoomType_Id = Convert.ToString(RoomType[0]["RoomType_Id"]);
                                            }
                                            if (ResponseData.Tables.Contains("ExtraInfo") == true)
                                            {
                                                if (ResponseData.Tables["ExtraInfo"].Columns.Contains("Info_Id") == true)
                                                {
                                                    DataRow[] ExtraInfo = ResponseData.Tables["ExtraInfo"].Select("RoomType_Id=" + RoomType_Id + "");
                                                    if (ExtraInfo.Length > 0)
                                                    {
                                                        int ExtraInfo_Id = Convert.ToInt32(ExtraInfo[0]["ExtraInfo_Id"]);
                                                        if (ResponseData.Tables.Contains("Msg") == true)
                                                        {
                                                            DataRow[] Msg = ResponseData.Tables["Msg"].Select("ExtraInfo_Id=" + ExtraInfo_Id + "");
                                                            for (int b = 0; b < Msg.Length; b++)
                                                            {
                                                                if (b == 0)
                                                                {
                                                                    if (Essen == "")
                                                                    {
                                                                        Essen = Convert.ToString(Msg[b]["Msg_Text"]);
                                                                    }
                                                                    else
                                                                    {
                                                                        Essen = Essen + Convert.ToString(Msg[b]["Msg_Text"]);
                                                                    }
                                                                }
                                                                else
                                                                {
                                                                    Essen = Essen + Convert.ToString(Msg[b]["Msg_Text"]);
                                                                }
                                                            }
                                                        }
                                                        else
                                                        {
                                                            for (int b = 0; b < ExtraInfo.Length; b++)
                                                            {
                                                                if (b == 0)
                                                                {
                                                                    if (Essen == "")
                                                                    {
                                                                        Essen = Convert.ToString(ExtraInfo[b]["Msg"]);
                                                                    }
                                                                    else
                                                                    {
                                                                        Essen = Essen + Convert.ToString(ExtraInfo[b]["Msg"]);
                                                                    }
                                                                }
                                                                else
                                                                {
                                                                    Essen = Essen + Convert.ToString(ExtraInfo[b]["Msg"]);
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                            string RoomName = Convert.ToString(RoomType[0]["Name"]);
                                            DataRow[] RoomRates = ResponseData.Tables["RoomRates"].Select("Room_Id=" + Room_Id + "");
                                            int RoomRates_Id = Convert.ToInt32(RoomRates[0]["RoomRates_Id"]);
                                            DataRow[] RoomRate = ResponseData.Tables["RoomRate"].Select("RoomRates_Id=" + RoomRates_Id + "");

                                            for (int l = 0; l < RoomRate.Length; l++)
                                            {
                                                string uniqno = Guid.NewGuid().ToString().Substring(0, 4);
                                                int RoomRate_Id = Convert.ToInt32(RoomRate[l]["RoomRate_Id"]);
                                                string MealPlan = Convert.ToString(RoomRate[l]["MealPlan"]);
                                                string RoomTypeId = Convert.ToString(RoomRate[l]["BookingCode"]) + "$$$" + uniqno;
                                                RoomTypeId = RoomTypeId.Replace("'", "");
                                                double RoomAmount = 0;
                                                #region For One RoomOnly

                                                DataRow[] Total = ResponseData.Tables["Total"].Select("RoomRate_Id=" + RoomRate_Id + "");
                                                for (int m = 0; m < Total.Length; m++)
                                                {
                                                    string RoomCurrency = Convert.ToString(Total[m]["Currency"]);

                                                    double Commision = Convert.ToDouble(Total[0]["Commission"]);
                                                    RoomAmount = Convert.ToDouble(Total[0]["Amount"]) * CurrencyFactor;

                                                    #region Ratebreakup

                                                    #endregion

                                                    #region Cancellation Policy
                                                    DataRow[] CancelPanalties = ResponseData.Tables["CancelPenalties"].Select("RoomRate_Id=" + RoomRate_Id + "");
                                                    int CancelPenalties_Id = Convert.ToInt32(CancelPanalties[0]["CancelPenalties_Id"]);
                                                    DataRow[] CancelPenalty = ResponseData.Tables["CancelPenalty"].Select("CancelPenalties_Id=" + CancelPenalties_Id + "");
                                                    bool ISchecked = true;
                                                    DateTime FromDate = DateTime.Now.Date;
                                                    for (int o = 0; o < CancelPenalty.Length; o++)
                                                    {
                                                        int CancelPenalty_Id = Convert.ToInt32(CancelPenalty[o]["CancelPenalty_Id"]);
                                                        DataRow[] Deadline = ResponseData.Tables["Deadline"].Select("CancelPenalty_Id=" + CancelPenalty_Id + "");
                                                        string TimeUnit = Convert.ToString(Deadline[0]["TimeUnit"]);
                                                        int Unit = Convert.ToInt32(Deadline[0]["Units"]);
                                                        if (TimeUnit.ToLower() == "day")
                                                        {
                                                            double VatonPolicy = 0;
                                                            DataRow[] Charge = ResponseData.Tables["Charge"].Select("CancelPenalty_Id=" + CancelPenalty_Id + "");
                                                            string CancellationCurrency = Convert.ToString(Charge[0]["Currency"]);

                                                            double CancellationAmount = Convert.ToDouble(Charge[0]["Amount"]) * CurrencyFactor;

                                                            #region Vat
                                                            CancellationAmount = _markup.ProcessHotelCancelationPrice_WithMarkup(ref CancellationAmount, SearchParam.NoofNights, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), CancellationAmount, isPerce, commonSearch.B2CMarkup, AgentPercentage, AgentMarkupAmount, commonSearch.IsParentId, commonSearch.SubAgentMarkup, ref VatonPolicy);
                                                            #endregion

                                                            DateTime ToDate = DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null);
                                                            if (Unit == 0)
                                                            {
                                                                //full charge nonrefundable
                                                                dtCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref CancellationAmount, dtCancellation.NewRow(), CurrencyFactor, CancellationCurrency, Supplier, Convert.ToString(OwnsystemHotelId), (RPH).ToString(), RoomTypeId, RoomName, Convert.ToString(MealPlan), ToDate, DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null), CancellationAmount, CancellationAmount, RoomAmount.ToString(), VatonPolicy, "", "", "", "", ""));
                                                                break;
                                                            }
                                                            else
                                                            {
                                                                ToDate = ToDate.AddDays(CutOffDays - Unit);
                                                                // refundable
                                                                if (ToDate > FromDate && ISchecked && o == 0)
                                                                {
                                                                    double zero = 0;
                                                                    ISchecked = false;
                                                                    dtCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref zero, dtCancellation.NewRow(), CurrencyFactor, CancellationCurrency, Supplier, Convert.ToString(OwnsystemHotelId), (RPH).ToString(), RoomTypeId, RoomName, Convert.ToString(MealPlan), DateTime.Now.Date, ToDate.AddDays(-1).AddHours(23).AddMinutes(59).AddSeconds(59), 0, 0, RoomAmount.ToString(), 0, "", "", "", "", ""));
                                                                }
                                                                dtCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref CancellationAmount, dtCancellation.NewRow(), CurrencyFactor, CancellationCurrency, Supplier, Convert.ToString(OwnsystemHotelId), (RPH).ToString(), RoomTypeId, RoomName, Convert.ToString(MealPlan), ToDate, DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null), CancellationAmount, CancellationAmount, RoomAmount.ToString(), VatonPolicy, "", "", "", "", ""));
                                                            }

                                                            if (CancelPenalty.Length > 1)
                                                            {
                                                                FromDate = ToDate;
                                                            }
                                                            // }
                                                        }
                                                        else
                                                        {
                                                            //full charge
                                                        }

                                                    }

                                                    #endregion

                                                }
                                                #region Rooms
                                                DataRow drRoom = dtHotelRoom.NewRow();
                                                drRoom["OwnsystemHotelId"] = OwnsystemHotelId;
                                                drRoom["RoomTypeCode"] = RoomCode;
                                                drRoom["BoardType"] = "";
                                                drRoom["MealName"] = MealPlan;
                                                drRoom["MealId"] = MealPlan;
                                                drRoom["Type"] = "";
                                                drRoom["Board"] = "";
                                                drRoom["BookingStatus"] = "Available";
                                                drRoom["UniqueNo"] = uniq;
                                                drRoom["ContractID"] = 0;
                                                drRoom["RoomNo"] = Convert.ToInt32(RPH);
                                                drRoom["BoardTypeID"] = "";
                                                drRoom["AdultsOccupancy"] = totalAdult;
                                                drRoom["ChildOccupancy"] = totalChild;
                                                drRoom["OfferCode"] = "";
                                                drRoom["OfferDescription"] = "";
                                                drRoom["RoomSequenceNo"] = "";
                                                drRoom["EssentialInformation"] = Essen;
                                                drRoom["Characteristic"] = "";
                                                #region Vat
                                                _markup.ProcessHotelRoom_WithMarkup(CurrencyFactor, Currency, false, IsCombine, SearchParam.NoofNights * SearchParam.Rooms.Count, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), SearchParam.CheckInDate, SearchParam.CheckOutDate, ref RoomAmount, ref drRoom, isPerce, AgentPercentage, AgentMarkupAmount, commonSearch.SubAgentMarkup, commonSearch.B2CMarkup, commonSearch.IsParentId);
                                                #endregion
                                                drRoom["SupplierName"] = Supplier;
                                                drRoom["RoomTypeName"] = RoomName;//(k + 1) + "_" + RoomName;
                                                drRoom["RoomTypeId"] = RoomTypeId;
                                                dtHotelRoom.Rows.Add(drRoom);
                                                #endregion

                                                #endregion


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
                    return options;
                }
            }
            catch (Exception ex)
            {
                Item.ResponseStatus = 404;
                _exception.Error(ex);
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
                        
                        SearchParam.SupplierTime += ",|Finish:" + Supplier + " Process Response  " + "|Time :" + timerroomx.Elapsed.ToString();
                        Task.Run(async () => await _commonMethod.FireAway_SQL_ResPorcessTimeLog("", timerroomx.Elapsed.ToString(), Supplier, SearchParam.SearchKey, 1, SearchParam.AgentID));
                    }
                }
                catch (Exception x)
                {
                    _exception.Error(x);
                }
                #endregion
            }
        }

        public async Task<PriceCheckCMDto> PriceCheckParsec(int i, DataTable dtCheckPriceReqStructure, DataSet dsCartDetails, DataRow[] AbreuDetails, string UniqueNo, string AgentUniqueNo, string IsFor, bool IsError, string supplier, string AgentCode, string Searchkey = "", int AgentId = 0)
        {
            PriceCheckCMDto priceCheckCMDto = new PriceCheckCMDto();
            string responsetype = "Success";
            string time = "";
            string SupplierName = Convert.ToString(dsCartDetails.Tables[1].DefaultView[i]["SupplierName"]);
            string Error = "";
            string PreBookingRequest = "";
            string ResponseDataAbreu = "";
            try
            {
                string URLContent = ParsecInfo.URLContent;
                if (supplier.ToLower() == "olympia")
                {
                    URLContent = ParsecInfo.olympiaURLContent;
                }
                string InternalReference = "Api_" + Convert.ToString(AgentCode + "_" + UniqueNo);

                PreBookingRequest = ParsecPreBooking(dsCartDetails, Convert.ToString(AbreuDetails[0]["UserName"]), Convert.ToString(AbreuDetails[0]["Password"]), Convert.ToString(AbreuDetails[0]["UserId"]), InternalReference);

                _commonMethod.ThirdPartyBookingHistory(Convert.ToString(UniqueNo), Convert.ToString(AgentUniqueNo), PreBookingRequest, "-", "Before Request hit Price Check", Convert.ToString(dsCartDetails.Tables[1].DefaultView[i]["SupplierName"]), "Request Confirm", " - ", "Withinearth");
                dtCheckPriceReqStructure.Rows.Add(_commonMethod.AddCheckPriceReq(dtCheckPriceReqStructure.NewRow(), i + 1, Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[i]["UserCartID"]), Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[i]["UserCartDetailId"]), Convert.ToString(dsCartDetails.Tables[1].DefaultView[i]["RoomTypeID"]), Convert.ToString(dsCartDetails.Tables[1].DefaultView[i]["SupplierName"]).ToLower(), PreBookingRequest));

                System.Diagnostics.Stopwatch timerroomprocess = new System.Diagnostics.Stopwatch();
                timerroomprocess.Start();

                ResponseDto responseDto = await _commonMethod.AbreuOnlineResponse_Supplier(PreBookingRequest, Convert.ToString(AbreuDetails[0]["WebSiteURL"]) + URLContent, Convert.ToInt32(120000), Searchkey);
                ResponseDataAbreu = responseDto.Response;
                Error = responseDto.Error;

                timerroomprocess.Stop();
                time = timerroomprocess.Elapsed.ToString();

                _commonMethod.ThirdPartyBookingHistory(Convert.ToString(UniqueNo), Convert.ToString(AgentUniqueNo), PreBookingRequest, ResponseDataAbreu + Error, "After Get Price Request hit booking", Convert.ToString(dsCartDetails.Tables[1].DefaultView[i]["SupplierName"]), "Response Confirm", "-", "Withinearth");
                if (ResponseDataAbreu != null)
                {
                    DataSet RoomResponse = _commonMethod.GetDataSetFromXMLString(ResponseDataAbreu);

                    if (RoomResponse.Tables.Contains("OTA_BookingInfoRS") == true)
                    {
                        if (RoomResponse.Tables["OTA_BookingInfoRS"].Columns.Contains("Success") == true)
                        {
                            try
                            {
                                responsetype = "Success";
                                string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, PreBookingRequest, ResponseDataAbreu, Error);
                            }
                            catch (Exception e)
                            {
                                _ = _exception.ErrorNew(e, ErrorLog._LogAPI.Prebook, Searchkey);

                            }
                            priceCheckCMDto.Flag = false;
                            return priceCheckCMDto;
                        }
                        else
                        {
                            try
                            {
                                responsetype = "GeneralError";
                                string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, PreBookingRequest, ResponseDataAbreu, Error);
                            }
                            catch (Exception e)
                            {
                                _ = _exception.ErrorNew(e, ErrorLog._LogAPI.Prebook, Searchkey);

                            }
                            priceCheckCMDto.Flag = true;
                            return priceCheckCMDto;
                        }
                    }
                }
                else
                {
                    try
                    {
                        responsetype = "GeneralError";
                        string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, PreBookingRequest, ResponseDataAbreu, Error);
                    }
                    catch (Exception e)
                    {
                        _ = _exception.ErrorNew(e, ErrorLog._LogAPI.Prebook, Searchkey);

                    }
                    priceCheckCMDto.Flag = false;
                    return priceCheckCMDto;
                }
            }
            catch (Exception ex)
            {
                try
                {
                    Error = ex.Message + ex.StackTrace.ToString();
                    responsetype = "GeneralError";
                    string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, PreBookingRequest, ResponseDataAbreu, Error);
                }
                catch (Exception e)
                {
                    _ = _exception.ErrorNew(e, ErrorLog._LogAPI.Prebook, Searchkey);

                }
                _ = _exception.ErrorNew(ex, ErrorLog._LogAPI.Prebook, Searchkey);

                IsError = true;
                priceCheckCMDto.IsError = IsError;
            }
            try
            {
                responsetype = "Success";
                string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, PreBookingRequest, ResponseDataAbreu, Error);
            }
            catch (Exception e)
            {
                _ = _exception.ErrorNew(e,ErrorLog._LogAPI.Prebook, Searchkey);
            }
            priceCheckCMDto.Flag = false;
            return priceCheckCMDto;
        }

        public string ParsecPreBooking(DataSet dsCartDetails, string UserName, string Password, string UserId, string InternalReference)
        {
            StringBuilder Request = new StringBuilder();
            try
            {
                Request.Append("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
                Request.Append("<soap-env:Envelope xmlns:soap-env=\"http://schemas.xmlsoap.org/soap/envelope/\">");
                Request.Append("<soap-env:Header>");
                Request.Append("<wsse:Security xmlns:wsse=\"http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd\">");
                Request.Append("<wsse:Username>" + UserName + "</wsse:Username>");
                Request.Append("<wsse:Password>" + Password + "</wsse:Password>");
                Request.Append("<Context>" + UserId + "</Context>");
                Request.Append("</wsse:Security>");
                Request.Append("</soap-env:Header>");
                Request.Append("<soap-env:Body>");
                Request.Append("<OTA_HotelResRQ xmlns=\"http://parsec.es/hotelapi/OTA2014Compact\" Transaction=\"PreBooking\">");
                Request.Append("<UniqueID Type=\"ClientReference\" ID=\"" + InternalReference + "\"/>");
                Request.Append("<HotelRes>");
                Request.Append("<Rooms>");
                for (int j = 0; j < dsCartDetails.Tables[1].DefaultView.Count; j++)
                {
                    string[] BookingCode = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomTypeId"]).Split(new String[] { "$$$" }, StringSplitOptions.None);
                    Request.Append("<Room>");
                    Request.Append("<RoomRate BookingCode=\"" + BookingCode[0] + "\"/>");
                    Request.Append("</Room>");
                }
                Request.Append("</Rooms>");
                Request.Append("</HotelRes>");
                Request.Append("</OTA_HotelResRQ>");
                Request.Append("</soap-env:Body>");
                Request.Append("</soap-env:Envelope>");
                string BookingREQ = Request.ToString();
                return BookingREQ;
            }
            catch (Exception ex)
            {
                _ = _exception.ErrorNew(ex,ErrorLog._LogAPI.Prebook);

            }
            return "";
        }

        public string ParsecOlympiaBooking(string AgentCode, DataTable dtPassengers, DataSet dsCartDetails, string UserName, string Password, string UserId, string UniqueNo, int j)
        {
            StringBuilder Request = new StringBuilder();
            try
            {
                string InternalReference = "Api_" + AgentCode + "_" + UniqueNo;
                Request.Append("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
                Request.Append("<soap-env:Envelope xmlns:soap-env=\"http://schemas.xmlsoap.org/soap/envelope/\">");
                Request.Append("<soap-env:Header>");
                Request.Append("<wsse:Security xmlns:wsse=\"http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd\">");
                Request.Append("<wsse:Username>" + UserName + "</wsse:Username>");
                Request.Append("<wsse:Password>" + Password + "</wsse:Password>");
                Request.Append("<Context>" + UserId + "</Context>");
                Request.Append("</wsse:Security>");
                Request.Append("</soap-env:Header>");
                Request.Append("<soap-env:Body>");
                Request.Append("<OTA_HotelResRQ xmlns=\"http://parsec.es/hotelapi/OTA2014Compact\" Transaction=\"Booking\" DetailLevel=\"2\">");
                Request.Append("<UniqueID Type=\"ClientReference\" ID=\"" + InternalReference + "\"/>");
                Request.Append("<HotelRes>");
                Request.Append("<Rooms>");
                //  for (int j = 0; j < dsCartDetails.Tables[1].DefaultView.Count; j++)
                //   {
                dtPassengers.DefaultView.RowFilter = "PickUp='" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomTypeId"]) + "' and UserCartID='" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["UserCartID"]) + "' and RoomNo='" + Convert.ToInt32(j + 1) + "'";
                Request.Append("<Room>");
                string[] Arr = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomTypeID"]).Split(new String[] { "$$$" }, StringSplitOptions.None);
                string BookingCode = Arr[0];
                Request.Append("<RoomRate BookingCode=\"" + BookingCode + "\"/>");
                DataTable dtnewpassanger = dtPassengers.DefaultView.ToTable();
                Request.Append("<Guests>");
                for (int b = 0; b < dtnewpassanger.Rows.Count; b++)
                {
                    if (Convert.ToString(dtnewpassanger.Rows[b]["PaxType"]) == "Adult")
                    {
                        if (Convert.ToInt16(dtPassengers.Rows[b]["IsLead"]) == 1)
                        {
                            Request.Append("<Guest AgeCode=\"A\" LeadGuest=\"1\">");
                        }
                        else
                        {
                            Request.Append("<Guest AgeCode=\"A\">");
                        }
                        Request.Append("<PersonName>");
                        Request.Append("<NamePrefix>" + Convert.ToString(dtnewpassanger.Rows[b]["Prefix"]) + "</NamePrefix>");
                        Request.Append("<GivenName>" + Convert.ToString(dtnewpassanger.Rows[b]["FirstName"]) + "</GivenName>");
                        Request.Append("<Surname>" + Convert.ToString(dtnewpassanger.Rows[b]["LastName"]) + "</Surname>");
                        Request.Append("</PersonName>");
                        Request.Append("</Guest>");
                    }
                    else if (Convert.ToString(dtnewpassanger.Rows[b]["PaxType"]) == "Child")
                    {
                        int childage = Convert.ToInt32(dtnewpassanger.Rows[b]["ChildAge"]);
                        Request.Append("<Guest AgeCode=\"C\" Age=\"" + childage + "\">");
                        Request.Append("<PersonName>");
                        Request.Append("<NamePrefix>" + Convert.ToString(dtnewpassanger.Rows[b]["Prefix"]) + "</NamePrefix>");
                        Request.Append("<GivenName>" + Convert.ToString(dtnewpassanger.Rows[b]["FirstName"]) + "</GivenName>");
                        Request.Append("<Surname>" + Convert.ToString(dtnewpassanger.Rows[b]["LastName"]) + "</Surname>");
                        Request.Append("</PersonName>");
                        Request.Append("</Guest>");
                    }
                }
                Request.Append("</Guests>");
                Request.Append("</Room>");
                //  }
                Request.Append("</Rooms>");
                Request.Append("</HotelRes>");
                Request.Append("</OTA_HotelResRQ>");
                Request.Append("</soap-env:Body>");
                Request.Append("</soap-env:Envelope>");
                string BookingREQ = Request.ToString();
                return BookingREQ;
            }
            catch (Exception ex)
            {
                _ = _exception.ErrorNew(ex, ErrorLog._LogAPI.Book);
            }
            return "";
        }

        public string ParsecOlympiaBooking_new(string AgentCode, DataTable dtPassengers, BookRequest_newversion bookRequest, string UserName, string Password, string UserId, string UniqueNo, int j)
        {
            StringBuilder Request = new StringBuilder();
            try
            {
                string InternalReference = "Api_" + AgentCode + "_" + UniqueNo;
                Request.Append("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
                Request.Append("<soap-env:Envelope xmlns:soap-env=\"http://schemas.xmlsoap.org/soap/envelope/\">");
                Request.Append("<soap-env:Header>");
                Request.Append("<wsse:Security xmlns:wsse=\"http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd\">");
                Request.Append("<wsse:Username>" + UserName + "</wsse:Username>");
                Request.Append("<wsse:Password>" + Password + "</wsse:Password>");
                Request.Append("<Context>" + UserId + "</Context>");
                Request.Append("</wsse:Security>");
                Request.Append("</soap-env:Header>");
                Request.Append("<soap-env:Body>");
                Request.Append("<OTA_HotelResRQ xmlns=\"http://parsec.es/hotelapi/OTA2014Compact\" Transaction=\"Booking\" DetailLevel=\"2\">");
                Request.Append("<UniqueID Type=\"ClientReference\" ID=\"" + InternalReference + "\"/>");
                Request.Append("<HotelRes>");
                Request.Append("<Rooms>");
                //  for (int j = 0; j < dsCartDetails.Tables[1].DefaultView.Count; j++)
                //   {
                dtPassengers.DefaultView.RowFilter = "PickUp='" + Convert.ToString(bookRequest.userCart.UserCartDetails[j].RoomTypeId) + "' and UserCartID='" + Convert.ToString(bookRequest.userCart.UserCartDetails[j].UserCartId) + "' and RoomNo='" + Convert.ToInt32(j + 1) + "'";
                Request.Append("<Room>");
                string[] Arr = Convert.ToString(bookRequest.userCart.UserCartDetails[j].RoomTypeId).Split(new String[] { "$$$" }, StringSplitOptions.None);
                string BookingCode = Arr[0];
                Request.Append("<RoomRate BookingCode=\"" + BookingCode + "\"/>");
                DataTable dtnewpassanger = dtPassengers.DefaultView.ToTable();
                Request.Append("<Guests>");
                for (int b = 0; b < dtnewpassanger.Rows.Count; b++)
                {
                    if (Convert.ToString(dtnewpassanger.Rows[b]["PaxType"]) == "Adult")
                    {
                        if (Convert.ToInt16(dtPassengers.Rows[b]["IsLead"]) == 1)
                        {
                            Request.Append("<Guest AgeCode=\"A\" LeadGuest=\"1\">");
                        }
                        else
                        {
                            Request.Append("<Guest AgeCode=\"A\">");
                        }
                        Request.Append("<PersonName>");
                        Request.Append("<NamePrefix>" + Convert.ToString(dtnewpassanger.Rows[b]["Prefix"]) + "</NamePrefix>");
                        Request.Append("<GivenName>" + Convert.ToString(dtnewpassanger.Rows[b]["FirstName"]) + "</GivenName>");
                        Request.Append("<Surname>" + Convert.ToString(dtnewpassanger.Rows[b]["LastName"]) + "</Surname>");
                        Request.Append("</PersonName>");
                        Request.Append("</Guest>");
                    }
                    else if (Convert.ToString(dtnewpassanger.Rows[b]["PaxType"]) == "Child")
                    {
                        int childage = Convert.ToInt32(dtnewpassanger.Rows[b]["ChildAge"]);
                        Request.Append("<Guest AgeCode=\"C\" Age=\"" + childage + "\">");
                        Request.Append("<PersonName>");
                        Request.Append("<NamePrefix>" + Convert.ToString(dtnewpassanger.Rows[b]["Prefix"]) + "</NamePrefix>");
                        Request.Append("<GivenName>" + Convert.ToString(dtnewpassanger.Rows[b]["FirstName"]) + "</GivenName>");
                        Request.Append("<Surname>" + Convert.ToString(dtnewpassanger.Rows[b]["LastName"]) + "</Surname>");
                        Request.Append("</PersonName>");
                        Request.Append("</Guest>");
                    }
                }
                Request.Append("</Guests>");
                Request.Append("</Room>");
                //  }
                Request.Append("</Rooms>");
                Request.Append("</HotelRes>");
                Request.Append("</OTA_HotelResRQ>");
                Request.Append("</soap-env:Body>");
                Request.Append("</soap-env:Envelope>");
                string BookingREQ = Request.ToString();
                return BookingREQ;
            }
            catch (Exception ex)
            {
                _ = _exception.ErrorNew(ex, ErrorLog._LogAPI.Book);
            }
            return "";
        }
        public string ParsecBooking(string AgentCode, DataTable dtPassengers, DataSet dsCartDetails, string UserName, string Password, string UserId, string UniqueNo)
        {
            StringBuilder Request = new StringBuilder();
            try
            {
                string InternalReference = "Api" + AgentCode + "_" + UniqueNo;
                Request.Append("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
                Request.Append("<soap-env:Envelope xmlns:soap-env=\"http://schemas.xmlsoap.org/soap/envelope/\">");
                Request.Append("<soap-env:Header>");
                Request.Append("<wsse:Security xmlns:wsse=\"http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd\">");
                Request.Append("<wsse:Username>" + UserName + "</wsse:Username>");
                Request.Append("<wsse:Password>" + Password + "</wsse:Password>");
                Request.Append("<Context>" + UserId + "</Context>");
                Request.Append("</wsse:Security>");
                Request.Append("</soap-env:Header>");
                Request.Append("<soap-env:Body>");
                Request.Append("<OTA_HotelResRQ xmlns=\"http://parsec.es/hotelapi/OTA2014Compact\" Transaction=\"Booking\" DetailLevel=\"2\">");
                Request.Append("<UniqueID Type=\"ClientReference\" ID=\"" + InternalReference + "\"/>");
                Request.Append("<HotelRes>");
                Request.Append("<Rooms>");
                for (int j = 0; j < dsCartDetails.Tables[1].DefaultView.Count; j++)
                {
                    dtPassengers.DefaultView.RowFilter = "PickUp='" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomTypeId"]) + "' and UserCartID='" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["UserCartID"]) + "' and RoomNo='" + Convert.ToInt32(j + 1) + "'";
                    Request.Append("<Room>");
                    string[] Arr = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomTypeID"]).Split(new String[] { "$$$" }, StringSplitOptions.None);
                    string BookingCode = Arr[j];
                    Request.Append("<RoomRate BookingCode=\"" + BookingCode + "\"/>");
                    DataTable dtnewpassanger = dtPassengers.DefaultView.ToTable();
                    Request.Append("<Guests>");
                    for (int b = 0; b < dtnewpassanger.Rows.Count; b++)
                    {
                        if (Convert.ToString(dtnewpassanger.Rows[b]["PaxType"]) == "Adult")
                        {
                            if (Convert.ToInt16(dtPassengers.Rows[b]["IsLead"]) == 1 && j == 0)
                            {
                                Request.Append("<Guest AgeCode=\"A\" LeadGuest=\"1\">");
                            }
                            else
                            {
                                Request.Append("<Guest AgeCode=\"A\">");
                            }
                            Request.Append("<PersonName>");
                            Request.Append("<NamePrefix>" + Convert.ToString(dtnewpassanger.Rows[b]["Prefix"]) + "</NamePrefix>");
                            Request.Append("<GivenName>" + Convert.ToString(dtnewpassanger.Rows[b]["FirstName"]) + "</GivenName>");
                            Request.Append("<Surname>" + Convert.ToString(dtnewpassanger.Rows[b]["LastName"]) + "</Surname>");
                            Request.Append("</PersonName>");
                            Request.Append("</Guest>");
                        }
                        else if (Convert.ToString(dtnewpassanger.Rows[b]["PaxType"]) == "Child")
                        {
                            int childage = Convert.ToInt32(dtnewpassanger.Rows[b]["ChildAge"]);
                            Request.Append("<Guest AgeCode=\"c\" Age=\"" + childage + "\">");
                            Request.Append("<PersonName>");
                            Request.Append("<NamePrefix>" + Convert.ToString(dtnewpassanger.Rows[b]["Prefix"]) + "</NamePrefix>");
                            Request.Append("<GivenName>" + Convert.ToString(dtnewpassanger.Rows[b]["FirstName"]) + "</GivenName>");
                            Request.Append("<Surname>" + Convert.ToString(dtnewpassanger.Rows[b]["LastName"]) + "</Surname>");
                            Request.Append("</PersonName>");
                            Request.Append("</Guest>");
                        }
                    }
                    Request.Append("</Guests>");
                    Request.Append("</Room>");
                }
                Request.Append("</Rooms>");
                Request.Append("</HotelRes>");
                Request.Append("</OTA_HotelResRQ>");
                Request.Append("</soap-env:Body>");
                Request.Append("</soap-env:Envelope>");
                string BookingREQ = Request.ToString();
                return BookingREQ;
            }
            catch (Exception ex)
            {
                _exception.ErrorNew(ex, ErrorLog._LogAPI.Book);

            }
            return "";
        }

        public string ParsecBooking_new(string AgentCode, DataTable dtPassengers, BookRequest_newversion bookRequest, string UserName, string Password, string UserId, string UniqueNo)
        {
            StringBuilder Request = new StringBuilder();
            try
            {
                string InternalReference = "Api" + AgentCode + "_" + UniqueNo;
                Request.Append("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
                Request.Append("<soap-env:Envelope xmlns:soap-env=\"http://schemas.xmlsoap.org/soap/envelope/\">");
                Request.Append("<soap-env:Header>");
                Request.Append("<wsse:Security xmlns:wsse=\"http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd\">");
                Request.Append("<wsse:Username>" + UserName + "</wsse:Username>");
                Request.Append("<wsse:Password>" + Password + "</wsse:Password>");
                Request.Append("<Context>" + UserId + "</Context>");
                Request.Append("</wsse:Security>");
                Request.Append("</soap-env:Header>");
                Request.Append("<soap-env:Body>");
                Request.Append("<OTA_HotelResRQ xmlns=\"http://parsec.es/hotelapi/OTA2014Compact\" Transaction=\"Booking\" DetailLevel=\"2\">");
                Request.Append("<UniqueID Type=\"ClientReference\" ID=\"" + InternalReference + "\"/>");
                Request.Append("<HotelRes>");
                Request.Append("<Rooms>");
                for (int j = 0; j < bookRequest.userCart.UserCartDetails.Count; j++)
                {
                    dtPassengers.DefaultView.RowFilter = "PickUp='" + Convert.ToString(bookRequest.userCart.UserCartDetails[j].RoomTypeId) + "' and UserCartID='" + Convert.ToString(bookRequest.userCart.UserCartDetails[j].UserCartId) + "' and RoomNo='" + Convert.ToInt32(j + 1) + "'";
                    Request.Append("<Room>");
                    string[] Arr = Convert.ToString(bookRequest.userCart.UserCartDetails[j].RoomTypeId).Split(new String[] { "$$$" }, StringSplitOptions.None);
                    string BookingCode = Arr[j];
                    Request.Append("<RoomRate BookingCode=\"" + BookingCode + "\"/>");
                    DataTable dtnewpassanger = dtPassengers.DefaultView.ToTable();
                    Request.Append("<Guests>");
                    for (int b = 0; b < dtnewpassanger.Rows.Count; b++)
                    {
                        if (Convert.ToString(dtnewpassanger.Rows[b]["PaxType"]) == "Adult")
                        {
                            if (Convert.ToInt16(dtPassengers.Rows[b]["IsLead"]) == 1 && j == 0)
                            {
                                Request.Append("<Guest AgeCode=\"A\" LeadGuest=\"1\">");
                            }
                            else
                            {
                                Request.Append("<Guest AgeCode=\"A\">");
                            }
                            Request.Append("<PersonName>");
                            Request.Append("<NamePrefix>" + Convert.ToString(dtnewpassanger.Rows[b]["Prefix"]) + "</NamePrefix>");
                            Request.Append("<GivenName>" + Convert.ToString(dtnewpassanger.Rows[b]["FirstName"]) + "</GivenName>");
                            Request.Append("<Surname>" + Convert.ToString(dtnewpassanger.Rows[b]["LastName"]) + "</Surname>");
                            Request.Append("</PersonName>");
                            Request.Append("</Guest>");
                        }
                        else if (Convert.ToString(dtnewpassanger.Rows[b]["PaxType"]) == "Child")
                        {
                            int childage = Convert.ToInt32(dtnewpassanger.Rows[b]["ChildAge"]);
                            Request.Append("<Guest AgeCode=\"c\" Age=\"" + childage + "\">");
                            Request.Append("<PersonName>");
                            Request.Append("<NamePrefix>" + Convert.ToString(dtnewpassanger.Rows[b]["Prefix"]) + "</NamePrefix>");
                            Request.Append("<GivenName>" + Convert.ToString(dtnewpassanger.Rows[b]["FirstName"]) + "</GivenName>");
                            Request.Append("<Surname>" + Convert.ToString(dtnewpassanger.Rows[b]["LastName"]) + "</Surname>");
                            Request.Append("</PersonName>");
                            Request.Append("</Guest>");
                        }
                    }
                    Request.Append("</Guests>");
                    Request.Append("</Room>");
                }
                Request.Append("</Rooms>");
                Request.Append("</HotelRes>");
                Request.Append("</OTA_HotelResRQ>");
                Request.Append("</soap-env:Body>");
                Request.Append("</soap-env:Envelope>");
                string BookingREQ = Request.ToString();
                return BookingREQ;
            }
            catch (Exception ex)
            {
                _exception.ErrorNew(ex, ErrorLog._LogAPI.Book);

            }
            return "";
        }

        public string ParsecCancellationRequest(string Booking_Ref, string UserName, string Password, string UserId)
        {
            try
            {
                StringBuilder ABXML = new StringBuilder();
                ABXML.Append("<soap-env:Envelope xmlns:soap-env=\"http://schemas.xmlsoap.org/soap/envelope/\">");
                ABXML.Append("<soap-env:Header>");
                ABXML.Append("<wsse:Security xmlns:wsse=\"http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd\">");
                ABXML.Append("<wsse:Username>" + UserName + "</wsse:Username>");
                ABXML.Append("<wsse:Password>" + Password + "</wsse:Password>");
                ABXML.Append("<Context>" + UserId + "</Context>");
                ABXML.Append("</wsse:Security>");
                ABXML.Append("</soap-env:Header>");
                ABXML.Append("<soap-env:Body>");
                ABXML.Append("<OTA_CancelRQ xmlns=\"http://parsec.es/hotelapi/OTA2014Compact\" Transaction=\"Cancel\">");
                ABXML.Append("<UniqueID Type=\"Reservation\" ID=\"" + Booking_Ref + "\"/>");
                ABXML.Append("</OTA_CancelRQ>");
                ABXML.Append("</soap-env:Body>");
                ABXML.Append("</soap-env:Envelope>");
                string TLREQ = ABXML.ToString();
                return TLREQ;
            }
            catch (Exception ex)
            {
                _ =_exception.ErrorNew(ex, ErrorLog._LogAPI.CancelBooking);

                return "";
            }
        }

        

    }
}
