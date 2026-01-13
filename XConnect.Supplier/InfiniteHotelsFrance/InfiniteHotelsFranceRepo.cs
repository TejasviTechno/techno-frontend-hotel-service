using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using XConnect.CommonUtilities;
using XConnect.CommonUtilities.Interface;
using XConnect.ErrorLogHandler.Interface;
using XConnect.Markup.Interface;
using XConnect.Models;
using XConnect.Models.Hotel;
using XConnect.Supplier.InfiniteHotelsFrance.Interface;
using static Azure.Core.HttpHeader;
using static XConnect.Models.Class_Pruvo;
using static XConnect.Models.ErrorLog;
using Options = XConnect.Models.Options;

namespace XConnect.Supplier.InfiniteHotelsFrance
{
    public class InfiniteHotelsFranceRepo : IInfiniteHotelsFrance
    {
        private readonly ICommonMethod _commonMethod;
        private readonly IException _exception;
        private readonly IMarkup _markup;
        private readonly ISupplierRes _supplierRes;
        private readonly KeySettings _keySettings;



        public InfiniteHotelsFranceRepo(ICommonMethod commonMethod, IMarkup markup, ISupplierRes supplierRes, IOptions<KeySettings> keySettings)
        {
            _commonMethod = commonMethod;
            _exception = null;
            _markup = markup;
            _supplierRes = supplierRes;
            _keySettings = keySettings.Value;

        }

        public void GetInfiniteHotelsFrance_async(DataRow[] commonCode, List<StarRating> listRating, HotelSearchParameter SearchPara, List<SupplierDetails> lstInfiniteHotelsFranceDetails, List<SupplierSearchConfig> lstInfiniteHotelsFranceSearchConfig, DataTable dtPreferredDataHotel, ref ConcurrentBag<HotelX> AllTask, int Hit, DataTable dtROE, TokenData tokenData, bool HitFromAll, DataSet dtXMLCity)
        {
            try
            {
                List<StarRating> lstRating = new List<StarRating>();
                lstRating = _commonMethod.GetStarRatingStringList_xtg(SearchPara);

                List<HotelCodeByCity> DtHotelDAta = HtmlExtensions.ToList<HotelCodeByCity>(commonCode.CopyToDataTable());
                int ratingcount = lstRating.Count;
                int hotelcount = DtHotelDAta.Count;

                foreach (SupplierDetails InfiniteHotelsFranceDetails in lstInfiniteHotelsFranceDetails)
                {
                    try
                    {
                        int SupplierID = InfiniteHotelsFranceDetails.SupplierID;
                        string Provider = InfiniteHotelsFranceDetails.SupplierName;


                        if (lstInfiniteHotelsFranceSearchConfig.Count > 0)
                        {
                            List<SupplierSearchConfig> InfiniteHotelsFranceSearchConfig = lstInfiniteHotelsFranceSearchConfig
                                   .Where(x => x != null && x.SupplierID == SupplierID)
                                   .ToList();
                            if (InfiniteHotelsFranceSearchConfig != null && InfiniteHotelsFranceSearchConfig.Count > 0)
                            {
                                DataForSearch dataForSearch = _commonMethod.GetXmlDataForSearchTechwise(SearchPara.Nationality, dtXMLCity, Provider, InfiniteHotelsFranceDetails.TechName);
                                if (dataForSearch.XmlNationality != "")
                                {

                                    string Ids = string.Empty;
                                    string xmlbase = "";
                                    _commonMethod.GetPrefferedHotelCodes(ref Ids, ref xmlbase, ref InfiniteHotelsFranceSearchConfig, Provider, dtPreferredDataHotel, SearchPara.IsSearchFor);

                                    if (InfiniteHotelsFranceSearchConfig != null)
                                    {
                                        List<HotelCodeByCity> filter = new List<HotelCodeByCity>();
                                        filter = _commonMethod.FilterWithOutCity_Code(ratingcount, hotelcount, DtHotelDAta, lstRating, Provider);

                                        if (filter.Count > 0)
                                        {

                                            string InfinitehotelsfranceCurrency = Convert.ToString(InfiniteHotelsFranceDetails.CurrencyShortName);
                                            string Sitecurrency = tokenData.SiteCurrency;
                                            double CurrencyFactorInfinitehotelsfrance = 1;

                                            if (InfinitehotelsfranceCurrency.ToLower() != Sitecurrency.ToLower())
                                            {
                                                CurrencyFactorInfinitehotelsfrance = _commonMethod.GetROEByCurrency(InfinitehotelsfranceCurrency, dtROE);
                                            }

                                            for (int k = 0; k < InfiniteHotelsFranceSearchConfig.Count; k++)
                                            {
                                                bool DebugMode = (Convert.ToInt32(InfiniteHotelsFranceSearchConfig[k].DebugMode) == 0 ? false : true);
                                                string UserName = Convert.ToString(InfiniteHotelsFranceDetails.UserName);
                                                string Password = Convert.ToString(InfiniteHotelsFranceDetails.Password);

                                                int IsCitywisesearch = Convert.ToInt32(InfiniteHotelsFranceSearchConfig[k].City);
                                                if (IsCitywisesearch == 0)
                                                {
                                                    if (Convert.ToInt32(InfiniteHotelsFranceSearchConfig[k].CityID) == 0 || Convert.ToInt32(InfiniteHotelsFranceSearchConfig[k].CityID) == SearchPara.CityID)
                                                    {
                                                        int NoOfRecord = Convert.ToInt32(InfiniteHotelsFranceSearchConfig[k].Noofrecords);
                                                        double count = Convert.ToDouble(filter.Count);
                                                        int NoOfDiv = Convert.ToInt32(Math.Ceiling(count / Convert.ToDouble(NoOfRecord)));
                                                        List<List<XMLID_Search>> lstxml = _commonMethod.GetXMLIDString(NoOfDiv, NoOfRecord, count, filter);

                                                        for (int a = 0; a < NoOfDiv; a++)
                                                        {
                                                            string IdsForSearch = Convert.ToString(_commonMethod.GetHotelCodeByCOmma(lstxml[a]));

                                                            HotelX xhotel = new HotelX();
                                                            xhotel.Supplier = Provider;
                                                            xhotel.DebugeMode = DebugMode;
                                                            xhotel.SearchTimeOut = Convert.ToInt32(InfiniteHotelsFranceSearchConfig[k].HotelSearchTimeOut);

                                                            xhotel.ProcessTimeout = Convert.ToInt32(InfiniteHotelsFranceSearchConfig[k].HotelSearchTimeOut2);
                                                            if (_keySettings.SupplierProxy == "0")
                                                                xhotel.URL = Convert.ToString(InfiniteHotelsFranceDetails.WebSiteURL);
                                                            else
                                                                xhotel.URL = Convert.ToString(InfiniteHotelsFranceDetails.WebSiteURLforAPI);
                                                            xhotel.UserId = Convert.ToString(InfiniteHotelsFranceDetails.UserID);
                                                            xhotel.UserName = UserName;
                                                            xhotel.PassWord = Password;
                                                            xhotel.CurrencyCode = InfinitehotelsfranceCurrency;
                                                            xhotel.CurrencyFactor = CurrencyFactorInfinitehotelsfrance;
                                                            xhotel.XmlCity = dataForSearch.xmlCity;
                                                            xhotel.XmlCountry = "";
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
                                                                objrq.Request = XMLInfinitehotelsfranceRequestOnly_async(xhotel.URL, UserName, Password, xhotel.SearchTimeOut, SearchPara, IdsForSearch, CurrencyFactorInfinitehotelsfrance, DebugMode, dataForSearch.XmlNationality);
                                                                objrq.Response = ""; RQRS.Add(objrq);
                                                            }
                                                            xhotel.RQRS = RQRS;

                                                            #endregion
                                                            xhotel.ReturnRooms = InfiniteHotelsFranceDetails.ReturnRooms;
                                                            xhotel.TechName = InfiniteHotelsFranceDetails.TechName;

                                                            AllTask.Add(xhotel);
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    string IdsForSearch = "";
                                                    HotelX xhotel = new HotelX();
                                                    xhotel.Supplier = Provider;
                                                    xhotel.DebugeMode = DebugMode;
                                                    xhotel.SearchTimeOut = Convert.ToInt32(InfiniteHotelsFranceSearchConfig[k].HotelSearchTimeOut);
                                                    xhotel.ProcessTimeout = Convert.ToInt32(InfiniteHotelsFranceSearchConfig[k].HotelSearchTimeOut2);
                                                    if (_keySettings.SupplierProxy == "0")
                                                        xhotel.URL = Convert.ToString(InfiniteHotelsFranceDetails.WebSiteURL);
                                                    else
                                                        xhotel.URL = Convert.ToString(InfiniteHotelsFranceDetails.WebSiteURLforAPI);
                                                    xhotel.UserId = Convert.ToString(InfiniteHotelsFranceDetails.UserID);
                                                    xhotel.UserName = UserName;
                                                    xhotel.PassWord = Password;
                                                    xhotel.CurrencyCode = InfinitehotelsfranceCurrency;
                                                    xhotel.CurrencyFactor = CurrencyFactorInfinitehotelsfrance;
                                                    xhotel.XmlCity = dataForSearch.xmlCity;
                                                    xhotel.XmlCountry = "";
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
                                                        objrq.Request = XMLInfinitehotelsfranceRequestOnly_async(xhotel.URL, UserName, Password, xhotel.SearchTimeOut, SearchPara, IdsForSearch, CurrencyFactorInfinitehotelsfrance, DebugMode, dataForSearch.xmlCity);
                                                        objrq.Response = ""; RQRS.Add(objrq);
                                                    }
                                                    xhotel.RQRS = RQRS;
                                                    #endregion
                                                    xhotel.ReturnRooms = InfiniteHotelsFranceDetails.ReturnRooms;
                                                    xhotel.TechName = InfiniteHotelsFranceDetails.TechName;
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
            catch (Exception ex)
            {
                _exception.Error(ex)
;
            }
        }

        public string XMLInfinitehotelsfranceRequestOnly_async(string InfiniteHotelsFranceUrl, string InfiniteHotelsFranceUserName, string InfiniteHotelsFrancePassword, Int32 SearchTimeOut, HotelSearchParameter SearchParam, string InfiniteHotelsFranceHotelIds, double CurrencyFactorInfinitehotelsfrance, bool debugMode, string CityCode)
        {
            try
            {
                string customerKey = "";
                #region PriceOnly RQ
                string IHFPriceOnly = Get_Infinitehotelsfrance_PriceOnlyReq(SearchParam, InfiniteHotelsFrancePassword, InfiniteHotelsFranceHotelIds, InfiniteHotelsFranceUserName, customerKey, CityCode);
                return IHFPriceOnly;
                #endregion
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
                return "";
            }
        }

        public string Get_Infinitehotelsfrance_PriceOnlyReq(HotelSearchParameter SearchParam, string Password, string HotelIds, string clientId, string customerKey, string CityCode)
        {
            try
            {
                string checkIn = SearchParam.CheckInDate;
                checkIn = checkIn.Substring(6, 4) + "-" + checkIn.Substring(3, 2) + "-" + checkIn.Substring(0, 2);
                string checkOut = SearchParam.CheckOutDate;
                checkOut = checkOut.Substring(6, 4) + "-" + checkOut.Substring(3, 2) + "-" + checkOut.Substring(0, 2);
                StringBuilder Req = new StringBuilder();
                Req.Append("<soapenv:Envelope xmlns:soapenv=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:v2=\"http://webservice.gekko-holding.com/v2_4\">");
                Req.Append("<soapenv:Header/>");
                Req.Append("<soapenv:Body>");
                Req.Append("<v2:hotelAvailability>");
                Req.Append("<language>EN</language>");
                Req.Append("<identification clientId=\"" + clientId + "\" password=\"" + Password + "\" customerKey=\"" + customerKey + "\"/>");
                Req.Append("<availCriteria>");
                Req.Append("<checkIn>" + checkIn + "</checkIn>");
                Req.Append("<checkOut>" + checkOut + "</checkOut>");
                Req.Append("<destinationCriteria>");
                if (HotelIds != string.Empty)
                {
                    Req.Append("<v2:hotels>");
                    string[] hotelId = HotelIds.Split(',');
                    for (int i = 0; i < hotelId.Length; i++)
                    {
                        Req.Append("<hotel code=\"" + hotelId[i] + "\"/>");

                    }
                    Req.Append("</v2:hotels>");
                }
                else
                {
                    // Req.Append("<v2:city standard=\"locode\" code=\"" + CityCode + "\" range=\"3.0\"/>");
                    Req.Append("<v2:city standard=\"locode\" code=\"" + CityCode + "\"/>");

                }
                Req.Append("</destinationCriteria>");

                Req.Append("<roomCriterias>");
                for (int i = 0; i < SearchParam.Rooms.Count; i++)
                {
                    int noofadult = SearchParam.Rooms[i].NoofAdults;
                    int noofchild = SearchParam.Rooms[i].NoofChild;
                    int childage1 = SearchParam.Rooms[i].Child1Age;
                    int childage2 = SearchParam.Rooms[i].Child2Age;
                    string roomtype = "";
                    switch (noofadult)
                    {
                        case 1:
                            roomtype = "SING";
                            break;

                        case 2:
                            roomtype = "DOUB";
                            break;

                        case 3:
                            roomtype = "TRIP";
                            break;

                        case 4:
                            roomtype = "QUAD";
                            break;

                        default:
                            roomtype = "QUAD";
                            break;
                    }
                    Req.Append("<roomPlan roomType=\"" + roomtype + "\" adultsCount=\"" + noofadult + "\">");
                    if (noofchild == 1)
                    {
                        Req.Append("<children>");
                        Req.Append("<child age=\"" + childage1 + "\"/>");
                        Req.Append("</children>");
                    }
                    if (noofchild == 2)
                    {
                        Req.Append("<children>");
                        Req.Append("<child age=\"" + childage1 + "\"/>");
                        Req.Append("<child age=\"" + childage2 + "\"/>");
                        Req.Append("</children>");
                    }
                    Req.Append("</roomPlan>");
                }
                //Req.Append("</roomPlan>");
                Req.Append("</roomCriterias>");
                Req.Append("<limit></limit>");
                Req.Append("<sortBy order=\"ascending\">price</sortBy>");
                //Req.Append("<hotelNamePart>Alliance</hotelNamePart>");
                // Req.Append("<allowOnRequest>false</allowOnRequest>");
                Req.Append("<ratingCriteria minStars=\"1\" maxStars=\"5\"/>");
                Req.Append("</availCriteria>");
                Req.Append("</v2:hotelAvailability>");
                Req.Append("</soapenv:Body>");
                Req.Append("</soapenv:Envelope>");
                return Req.ToString();
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
                return "";
            }
        }

        public async Task<List<Options>> infinitehotelsfrance_HotelSearchWithRooms(HotelX Item, CommonSearch commonSearch, string WebSiteURL, int SearchTimeOut, string Supplier, bool DebugeMode, string UserName, string PassWord, string MethoodType, string ResponseData, HotelSearchParameter SearchParam, string xmlcity, string xmlcountry, string xmlnationality, string currency, double currencyfactor, int processTimeout, string ResProccessTimerLogActive, string ResProccessTimeValueforSaveLog)
        {
            System.Diagnostics.Stopwatch timerroomx = new System.Diagnostics.Stopwatch();
            timerroomx.Start();

            List<Options> options = new List<Options>();

            DataTable DtHotel = _commonMethod.GenerateDataTableNewV2();
            try
            {
                if (ResponseData.ToLower() == "" || ResponseData.ToLower().Contains("error"))
                {
                    Item.ResponseType = "NoCount";
                    return options;
                }

                int CutOffDays = CacheMaster.CutOffDays;
                CutOffDays = _commonMethod.GetCutOffDays(commonSearch.dtSuplierMarkup, Supplier);

                //string ConvertXml = JsonConvert.DeserializeXmlNode("{\"Row\":" + ResponseData + "}", "root").InnerXml;
                //DataSet TFSResponse = Common.GetDataSetFromXMLString(ConvertXml);

                //TFSResponseRootObject Res = JsonConvert.DeserializeObject<TFSResponseRootObject>(ResponseData);

                //CitywiseRootObject obj = JsonConvert.DeserializeObject<CitywiseRootObject>(ResponseData);

                if (ResponseData != "")
                {
                    ResponseData = ResponseData.Replace("<facilities>", "<facilitie>");
                    ResponseData = ResponseData.Replace("</facilities></facilities>", "</facilities></facilitie>");
                    DataSet IHFResponse = _commonMethod.GetDataSetFromXMLString(ResponseData);
                    if (IHFResponse == null || !IHFResponse.Tables.Contains("hotelResponse"))
                    {
                        Item.ResponseType = "NoCount";
                        return options;
                    }

                    for (int h = 0; h <= IHFResponse.Tables["hotelResponse"].Rows.Count - 1; h++)
                    {
                        Item.ResponseType = "Success";
                        double FinalTotal = 0;
                        #region "When not Need Room"  
                        // add hotel code from price only and room code blank here
                        if (SearchParam.Filters.IsShowRooms == 0)
                        {
                            #region "Hotel Code Block "

                            string HotelCode = Convert.ToString(IHFResponse.Tables["hotelResponse"].Rows[h]["code"]);
                            string HotelName = Convert.ToString(IHFResponse.Tables["hotelResponse"].Rows[h]["name"]);
                            int hotelResponse_Id = Convert.ToInt32(IHFResponse.Tables["hotelResponse"].Rows[h]["hotelResponse_Id"]);
                            DataRow[] droffers = IHFResponse.Tables["offers"].Select("hotelResponse_Id='" + hotelResponse_Id + "'");
                            int offers_Id = Convert.ToInt32(droffers[0]["offers_Id"]);
                            DataRow[] droffer = IHFResponse.Tables["offer"].Select("offers_Id='" + offers_Id + "'");
                            int offer_Id = Convert.ToInt32(droffer[0]["offer_Id"]);
                            DataRow[] drtotalPrice = IHFResponse.Tables["totalPrice"].Select("offer_Id='" + offer_Id + "'");
                            double TotalPrice = Convert.ToDouble(drtotalPrice[0]["value"]);
                            double RoomPrice = TotalPrice * currencyfactor;

                            DataRow drXmlHotel = DtHotel.NewRow();
                            drXmlHotel["XMLHotelCode"] = Convert.ToString(IHFResponse.Tables["hotelResponse"].Rows[h]["code"]);
                            drXmlHotel["HotelName"] = "";
                            drXmlHotel["SupplierName"] = Supplier;
                            drXmlHotel["OfficeID"] = 0;
                            drXmlHotel["ContractID"] = 0;
                            drXmlHotel["Rating"] = 0;
                            drXmlHotel["Reviews"] = 0;
                            drXmlHotel["UniqueNo"] = "";
                            drXmlHotel["IsShowSupplier"] = false;
                            drXmlHotel["BookingStatus"] = "Available";
                            drXmlHotel["XMLCity"] = xmlcity;
                            drXmlHotel["SupplierCurrency"] = currency;
                            drXmlHotel["FinalSellingPrice"] = RoomPrice;
                            drXmlHotel["SellingPrice"] = RoomPrice;
                            DtHotel.Rows.Add(drXmlHotel);
                            FinalTotal = RoomPrice;
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
                        #endregion
                        #region "When Need Rooms"
                        //add Hotel code blank and rooms code in room block
                        else
                        {
                            #region Hotels
                            string HotelCode = Convert.ToString(IHFResponse.Tables["hotelResponse"].Rows[h]["code"]);
                            string HotelName = Convert.ToString(IHFResponse.Tables["hotelResponse"].Rows[h]["name"]);
                            int hotelResponse_Id = Convert.ToInt32(IHFResponse.Tables["hotelResponse"].Rows[h]["hotelResponse_Id"]);
                            DataRow[] droffers = IHFResponse.Tables["offers"].Select("hotelResponse_Id='" + hotelResponse_Id + "'");
                            int offers_Id = Convert.ToInt32(droffers[0]["offers_Id"]);
                            DataRow[] droffer = IHFResponse.Tables["offer"].Select("offers_Id='" + offers_Id + "'");
                            int offer_Id_List = Convert.ToInt32(droffer[0]["offer_Id"]);
                            DataRow[] drtotalPrice_list = IHFResponse.Tables["totalPrice"].Select("offer_Id='" + offer_Id_List + "'");
                            double TotalPrice = Convert.ToDouble(drtotalPrice_list[0]["value"]);
                            double RoomPrice = TotalPrice * currencyfactor;

                            DataRow drXmlHotel = DtHotel.NewRow();
                            drXmlHotel["XMLHotelCode"] = Convert.ToString(IHFResponse.Tables["hotelResponse"].Rows[h]["code"]);
                            drXmlHotel["HotelName"] = "";
                            drXmlHotel["SupplierName"] = Supplier;
                            drXmlHotel["OfficeID"] = 0;
                            drXmlHotel["ContractID"] = 0;
                            drXmlHotel["Rating"] = 0;
                            drXmlHotel["Reviews"] = 0;
                            drXmlHotel["UniqueNo"] = "";
                            drXmlHotel["IsShowSupplier"] = false;
                            drXmlHotel["BookingStatus"] = "Available";
                            drXmlHotel["XMLCity"] = xmlcity;
                            drXmlHotel["SupplierCurrency"] = currency;
                            drXmlHotel["FinalSellingPrice"] = RoomPrice;
                            drXmlHotel["SellingPrice"] = RoomPrice;
                            DtHotel.Rows.Add(drXmlHotel);

                            double Pricenew = RoomPrice;

                            #region "GetRooms for this hotel"
                            bool willAddOrNot = true;
                            Int64 RaynaID = 0;
                            Int64 GiataId = 0;
                            int RatingHotel = 0;
                            Int64 OwnsystemHotelId = 0;
                            string Provider = Supplier;
                            double MinPrice = Pricenew;
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
                                    bool IsError = false;
                                    if (IHFResponse.Tables.Contains("offer"))
                                    {
                                        if (IHFResponse.Tables["offer"].Rows.Count == 0)
                                            return options;
                                        else
                                        {
                                            //for (int i = 0; i < IHFResponse.Tables["offer"].Rows.Count; i++)
                                            for (int i = 0; i < droffer.Length; i++)
                                            {
                                                double CompanyBuying = 0;
                                                string RoomTypeName = "";
                                                int offer_Id = Convert.ToInt32(droffer[i]["offer_Id"]);
                                                string roomCode = Convert.ToString(droffer[i]["offerCode"]);
                                                string onRequest = Convert.ToString(droffer[i]["onRequest"]);
                                                DataRow[] drtotalPrice = IHFResponse.Tables["totalPrice"].Select("offer_Id='" + offer_Id + "'");
                                                double totalPrice = Convert.ToDouble(drtotalPrice[0]["value"]) * currencyfactor;
                                                DataRow[] drroomOffers = IHFResponse.Tables["roomOffers"].Select("offer_Id='" + offer_Id + "'");
                                                int roomOffers_Id = Convert.ToInt32(drroomOffers[0]["roomOffers_Id"]);//Convert.ToInt32(IHFResponse.Tables["roomOffers"].Rows[i]["roomOffers_Id"]);
                                                DataRow[] drroomOffer = IHFResponse.Tables["roomOffer"].Select("roomOffers_Id='" + roomOffers_Id + "'");
                                                string roomType_Text = ""; string boardType = ""; string boardType_Text = ""; int adultsCount = 0; int ChildrenCount = 0; string roomType = "";

                                                List<string> MealTextDist = new List<string>();
                                                List<string> MealList = new List<string>();
                                                for (int j = 0; j < drroomOffer.Length; j++)
                                                {
                                                    int roomOffer_Id = Convert.ToInt32(drroomOffer[j]["roomOffer_Id"]);
                                                    int roomIndex = Convert.ToInt32(drroomOffer[j]["roomIndex"]);
                                                    adultsCount = adultsCount + Convert.ToInt32(drroomOffer[j]["adultsCount"]);
                                                    ChildrenCount = ChildrenCount + Convert.ToInt32(drroomOffer[j]["ChildrenCount"]);
                                                    DataRow[] drroomType = IHFResponse.Tables["roomType"].Select("roomOffer_Id='" + roomOffer_Id + "'");
                                                    DataRow[] drboardType = IHFResponse.Tables["boardType"].Select("roomOffer_Id='" + roomOffer_Id + "'");
                                                    if (j == 0)
                                                    {
                                                        roomType = roomType + Convert.ToString(drroomType[0]["code"]);
                                                        roomType_Text = roomType_Text + "[" + (j + 1) + "]" + Convert.ToString(drroomType[0]["roomType_Text"]);
                                                        boardType = boardType + Convert.ToString(drboardType[0]["Code"]);
                                                        boardType_Text = boardType_Text + Convert.ToString(drboardType[0]["boardType_Text"]);

                                                    }
                                                    else
                                                    {
                                                        roomType = roomType + "|" + Convert.ToString(drroomType[0]["code"]);
                                                        roomType_Text = roomType_Text + "[" + (j + 1) + "]" + Convert.ToString(drroomType[0]["roomType_Text"]);
                                                        boardType = boardType + "|" + Convert.ToString(drboardType[0]["Code"]);
                                                        boardType_Text = boardType_Text + "|" + Convert.ToString(drboardType[0]["boardType_Text"]);


                                                    }
                                                    DataRow[] drroomPrice = IHFResponse.Tables["roomPrice"].Select("roomOffer_Id='" + roomOffer_Id + "'");
                                                    double roomPrice = Convert.ToDouble(drroomPrice[0]["value"]) * currencyfactor;
                                                    //string currency = Convert.ToString(drroomPrice[0]["currency"]);
                                                    if (currency.ToLower() != Convert.ToString(drroomPrice[0]["currency"]).ToLower())
                                                    {
                                                        IsError = true;
                                                        continue;
                                                    }
                                                    CompanyBuying = totalPrice;

                                                    MealList.Add(Convert.ToString(drboardType[0]["Code"]));
                                                    MealTextDist.Add(Convert.ToString(drboardType[0]["boardType_Text"]));

                                                }

                                                var distinctMeals = MealList.Distinct().ToList();
                                                var distinctMealsText = MealTextDist.Distinct().ToList();

                                                if (distinctMeals.Count == 1 && distinctMealsText.Count == 1)
                                                {
                                                    //add by jay

                                                    string RoomTypeId = roomCode + "$$$$" + roomType_Text + "$$$$" + distinctMeals[0];

                                                    //end
                                                    if (IsError == true)
                                                        return options;
                                                    #region RateBreakUp
                                                    //for (int n = 0; n < NoOfNight; n++)
                                                    //{
                                                    //    DateTime date = (Convert.ToDateTime(checkin).Date).AddDays(n);
                                                    //    string rateWeekColumn = Convert.ToString(date.DayOfWeek).Substring(0, 3);

                                                    //    #region Vat
                                                    //    Double VatOnRateBrackUp = 0;
                                                    //    double PriceRoom = Common.ProcessHotelRateBreakupPrice_WithMarkup(1, SearchParam.CityID, SearchParam.CountryId, CompanyBuying / NoOfNight, B2CMarkup, IsParentId, isPerce, AgentPercentage, AgentMarkupAmount, SubAgentMarkup, ref VatOnRateBrackUp);
                                                    //    //PriceRoom = PriceRoom / NoOfNight;
                                                    //    #endregion
                                                    //    //double PriceRoom = Common.getLowestOrBeforeDiscount(CompanyBuying, B2CMarkup, IsParentId, SubAgentMarkup, isPerce, AgentPercentage, AgentMarkupAmount);
                                                    //    DataRow drInfiniteHotelsFrance_RateBreackUp = DtInfiniteHotelsFrance_RateBreackUp.NewRow();
                                                    //    drInfiniteHotelsFrance_RateBreackUp["SupplierName"] = "InfiniteHotelsFrance";
                                                    //    drInfiniteHotelsFrance_RateBreackUp["OwnsystemHotelId"] = ownsystemhotelId;
                                                    //    drInfiniteHotelsFrance_RateBreackUp["ContractID"] = 0;
                                                    //    drInfiniteHotelsFrance_RateBreackUp["RoomTypeName"] = roomType_Text;
                                                    //    drInfiniteHotelsFrance_RateBreackUp["RoomTypeId"] = roomCode;
                                                    //    drInfiniteHotelsFrance_RateBreackUp["MealId"] = boardType;
                                                    //    drInfiniteHotelsFrance_RateBreackUp["Meal"] = boardType;
                                                    //    drInfiniteHotelsFrance_RateBreackUp["DateOfWeek"] = rateWeekColumn;
                                                    //    drInfiniteHotelsFrance_RateBreackUp["Date"] = date;
                                                    //    drInfiniteHotelsFrance_RateBreackUp["Rate"] = PriceRoom;
                                                    //    drInfiniteHotelsFrance_RateBreackUp["RateType"] = "A Rate";
                                                    //    drInfiniteHotelsFrance_RateBreackUp["RoomNo"] = Convert.ToString(1);//rm + 1;
                                                    //    drInfiniteHotelsFrance_RateBreackUp["RoomSequenceNo"] = 0;
                                                    //    drInfiniteHotelsFrance_RateBreackUp["BookingStatus"] = "Available";
                                                    //    drInfiniteHotelsFrance_RateBreackUp["SupplierCurrency"] = Currency;
                                                    //    drInfiniteHotelsFrance_RateBreackUp["BuyingPrice"] = PriceRoom / Factor;
                                                    //    DtInfiniteHotelsFrance_RateBreackUp.Rows.Add(drInfiniteHotelsFrance_RateBreackUp);
                                                    //}
                                                    #endregion

                                                    #region Rooms
                                                    DataRow drInfiniteHotelsFranceRoom = dtHotelRoom.NewRow();
                                                    drInfiniteHotelsFranceRoom["OwnsystemHotelId"] = OwnsystemHotelId;
                                                    drInfiniteHotelsFranceRoom["RoomTypeName"] = roomType_Text;
                                                    drInfiniteHotelsFranceRoom["RoomTypeCode"] = roomCode;
                                                    //drInfiniteHotelsFranceRoom["MealId"] = boardType;
                                                    drInfiniteHotelsFranceRoom["MealId"] = distinctMeals[0];
                                                    //drInfiniteHotelsFranceRoom["MealName"] = boardType_Text;
                                                    drInfiniteHotelsFranceRoom["MealName"] = distinctMealsText[0];
                                                    drInfiniteHotelsFranceRoom["ContractID"] = 0;
                                                    //drInfiniteHotelsFranceRoom["BoardType"] = boardType_Text;
                                                    drInfiniteHotelsFranceRoom["BoardType"] = distinctMealsText[0];
                                                    //drInfiniteHotelsFranceRoom["RoomTypeId"] = roomCode;
                                                    drInfiniteHotelsFranceRoom["RoomTypeId"] = RoomTypeId;
                                                    drInfiniteHotelsFranceRoom["AdultsOccupancy"] = adultsCount;
                                                    drInfiniteHotelsFranceRoom["ChildOccupancy"] = ChildrenCount;
                                                    drInfiniteHotelsFranceRoom["MaxExtraBed"] = 0;
                                                    drInfiniteHotelsFranceRoom["RoomNo"] = Convert.ToString(1);
                                                    drInfiniteHotelsFranceRoom["RoomSequenceNo"] = 0;
                                                    drInfiniteHotelsFranceRoom["BookingStatus"] = "Available";
                                                    drInfiniteHotelsFranceRoom["UniqueNo"] = OwnsystemHotelId;
                                                    drInfiniteHotelsFranceRoom["OfferDescription"] = "";
                                                    drInfiniteHotelsFranceRoom["OfferCode"] = "";
                                                    drInfiniteHotelsFranceRoom["OfficeId"] = 0;
                                                    drInfiniteHotelsFranceRoom["EssentialInformation"] = "";
                                                    drInfiniteHotelsFranceRoom["SupplierName"] = Supplier;

                                                    double ItemPrice = Convert.ToDouble(totalPrice);
                                                    _markup.ProcessHotelRoom_WithMarkup(currencyfactor, currency, true, true, SearchParam.NoofNights, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), SearchParam.CheckInDate, SearchParam.CheckOutDate, ref ItemPrice, ref drInfiniteHotelsFranceRoom, isPerce, AgentPercentage, AgentMarkupAmount, commonSearch.SubAgentMarkup, commonSearch.B2CMarkup, commonSearch.IsParentId);

                                                    drInfiniteHotelsFranceRoom["SupplierName"] = Provider;
                                                    dtHotelRoom.Rows.Add(drInfiniteHotelsFranceRoom);
                                                    #endregion

                                                    #region Old Code

                                                    //DataRow[] drgetPreBookingInfoResponse = IHFResponse.Tables["getPreBookingInfoResponse"].Select();
                                                    //int getPreBookingInfoResponse_Id = Convert.ToInt32(drgetPreBookingInfoResponse[0]["getPreBookingInfoResponse_Id"]);
                                                    //DataRow[] drpreBookingInfo = IHFResponse.Tables["preBookingInfo"].Select("getPreBookingInfoResponse_Id='" + getPreBookingInfoResponse_Id + "'");
                                                    //int preBookingInfo_Id = Convert.ToInt32(drpreBookingInfo[0]["preBookingInfo_Id"]);
                                                    //string voucherObservation = Convert.ToString(drpreBookingInfo[0]["voucherObservation"]);
                                                    //string SPUI = voucherObservation;
                                                    //bool Istrue = true;
                                                    //double FinalPrice = 0;

                                                    //string offerCode = Convert.ToString(drpreBookingInfo[0]["offerCode"]);


                                                    //DataRow[] drcancellationFeesPolicy = IHFResponse.Tables["cancellationFeesPolicy"].Select("preBookingInfo_Id='" + preBookingInfo_Id + "'");
                                                    //DataTable dtcancel = IHFResponse.Tables["cancellationFeesPolicy"].Rows.Cast<DataRow>().OrderBy(row => Convert.ToString(row["fromDate"])).CopyToDataTable();
                                                    //dtcancel.Columns.Add("datefilter", typeof(DateTime));
                                                    //for (int x = 0; x < dtcancel.Rows.Count; x++)
                                                    //{
                                                    //    string dtfrom = Convert.ToString(dtcancel.Rows[x]["fromDate"]);
                                                    //    dtfrom = dtfrom.Substring(3, 2) + "-" + dtfrom.Substring(0, 2) + "-" + dtfrom.Substring(6, 4);
                                                    //    dtcancel.Rows[x]["datefilter"] = Convert.ToDateTime(Convert.ToString(dtfrom));
                                                    //}
                                                    //dtcancel.DefaultView.Sort = "datefilter asc";
                                                    //DataTable DTdrRoomcancellationRule = dtcancel.Clone();
                                                    //for (int cr = 0; cr < dtcancel.DefaultView.Count; cr++)
                                                    //{
                                                    //    if (Convert.ToInt32(dtcancel.DefaultView[cr]["preBookingInfo_Id"]) == preBookingInfo_Id)
                                                    //    {
                                                    //        DTdrRoomcancellationRule.ImportRow(dtcancel.DefaultView[cr].Row);
                                                    //    }
                                                    //}
                                                    //for (int ii = 0; ii < DTdrRoomcancellationRule.Rows.Count; ii++)
                                                    //{
                                                    //    double price = Convert.ToDouble(DTdrRoomcancellationRule.Rows[ii]["price"]) * currencyfactor;
                                                    //    string fromDate = Convert.ToString(DTdrRoomcancellationRule.Rows[ii]["fromDate"]);
                                                    //    string toDate = Convert.ToString(DTdrRoomcancellationRule.Rows[ii]["toDate"]);
                                                    //    currency = Convert.ToString(DTdrRoomcancellationRule.Rows[ii]["currency"]);
                                                    //    string allDates = Convert.ToString(DTdrRoomcancellationRule.Rows[ii]["allDates"]);
                                                    //    DataRow[] drofferPrice = IHFResponse.Tables["offerPrice"].Select("preBookingInfo_Id='" + preBookingInfo_Id + "'");
                                                    //    int offerPrice_Id = Convert.ToInt32(drofferPrice[0]["offerPrice_Id"]);
                                                    //    string changed = Convert.ToString(drofferPrice[0]["changed"]);
                                                    //    //DataRow[] drtotalPrice = IHFResponse.Tables["totalPrice"].Select("offerPrice_Id='" + offerPrice_Id + "'");
                                                    //    string currency1 = Convert.ToString(drtotalPrice[0]["currency"]);
                                                    //    double value1 = Convert.ToDouble(drtotalPrice[0]["value"]) * currencyfactor;
                                                    //    DataRow[] drpricePerRoom = IHFResponse.Tables["pricePerRoom"].Select("offerPrice_Id='" + offerPrice_Id + "'");
                                                    //    int pricePerRoom_Id = Convert.ToInt32(drpricePerRoom[0]["pricePerRoom_Id"]);
                                                    //    DataRow[] drroomPrices = IHFResponse.Tables["roomPrices"].Select("pricePerRoom_Id='" + pricePerRoom_Id + "'");
                                                    //    int roomPrices_Id = Convert.ToInt32(drroomPrices[0]["roomPrices_Id"]);
                                                    //    int roomIndex = Convert.ToInt32(drroomPrices[0]["roomIndex"]);
                                                    //    DataRow[] drroomPrice = IHFResponse.Tables["roomPrice"].Select("roomPrices_Id='" + roomPrices_Id + "'");
                                                    //    string currency2 = Convert.ToString(drroomPrice[0]["currency"]);
                                                    //    double value2 = Convert.ToDouble(drroomPrice[0]["value"]) * currencyfactor;
                                                    //    FinalPrice = value1;
                                                    //    if (Convert.ToDouble((FinalPrice)) > (Convert.ToDouble(CompanyBuying)))
                                                    //    {
                                                    //        Istrue = true;
                                                    //        offerCode = offerCode + "@" + Istrue + "@" + FinalPrice;

                                                    //    }
                                                    //    else
                                                    //    {
                                                    //        Istrue = false;
                                                    //        offerCode = offerCode + "@" + Istrue + "@" + FinalPrice;
                                                    //    }



                                                    //    DateTime curDate = _commonMethod.GetTimeZoneDate(SearchParam.CurrentDate);
                                                    //    //string uniqid = Guid.NewGuid().ToString();
                                                    //    #endregion

                                                    //    #region Cancellation Policy Logic and Row
                                                    //    DateTime DeadLineDate = DateTime.ParseExact(fromDate.Substring(0, 10), "dd/MM/yyyy", CultureInfo.InvariantCulture).Date;


                                                    //    #region Vat
                                                    //    double VatOnPolicy = 0;
                                                    //    double MainAgentCharge = price;
                                                    //    double CompanyChargeAmount = price;
                                                    //    double ChargeAmount = _markup.ProcessHotelCancelationPrice_WithMarkup(ref MainAgentCharge, SearchParam.NoofNights, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), price, isPerce, commonSearch.B2CMarkup, AgentPercentage, AgentMarkupAmount, commonSearch.IsParentId, commonSearch.SubAgentMarkup, ref VatOnPolicy);
                                                    //    #endregion

                                                    //    if (DeadLineDate.AddDays(CutOffDays).Date > DateTime.Now.Date)
                                                    //        DeadLineDate = DeadLineDate.AddDays(CutOffDays).Date;
                                                    //    else
                                                    //        DeadLineDate = DateTime.Now.Date;
                                                    //    if (curDate.Date < DeadLineDate.Date)
                                                    //    {
                                                    //        if (i == 0)
                                                    //        {
                                                    //            double Zero = 0;
                                                    //            dtCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref Zero, dtCancellation.NewRow(), currencyfactor, currency, Provider, Convert.ToString(OwnsystemHotelId), Convert.ToString(1), Convert.ToString(roomCode), roomType_Text, Convert.ToString(boardType), curDate, DeadLineDate.AddDays(-1).AddHours(23).AddMinutes(59).AddSeconds(59), 0, 0, "", VatOnPolicy, "0", "NO", offerCode, SPUI, "", ""));
                                                    //            dtCancellation.AcceptChanges();
                                                    //        }
                                                    //        dtCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref CompanyChargeAmount, dtCancellation.NewRow(), currencyfactor, currency, Provider, Convert.ToString(OwnsystemHotelId), Convert.ToString(1), Convert.ToString(roomCode), roomType_Text, Convert.ToString(boardType), DeadLineDate, DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null), RoomPrice, RoomPrice, "", 0, "0", "NO", offerCode, SPUI, "", ""));
                                                    //        dtCancellation.AcceptChanges();
                                                    //    }
                                                    //    else
                                                    //    {
                                                    //        dtCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref CompanyChargeAmount, dtCancellation.NewRow(), currencyfactor, currency, Provider, Convert.ToString(OwnsystemHotelId), Convert.ToString(1), Convert.ToString(roomCode), roomType_Text, Convert.ToString(boardType), curDate, DeadLineDate, RoomPrice, RoomPrice, "", 0, "0", "Yes", offerCode, SPUI, "", ""));
                                                    //        dtCancellation.AcceptChanges();
                                                    //    }
                                                    //}
                                                    #endregion

                                                    #region New Code

                                                    string offerCode = "";
                                                    bool Istrue = true;
                                                    double FinalPrice = 0;
                                                    int preBookingInfo_Id = 0;
                                                    string voucherObservation = "";
                                                    string SPUI = "";

                                                    if (IHFResponse.Tables.Contains("getPreBookingInfoResponse"))
                                                    {
                                                        DataRow[] drgetPreBookingInfoResponse = IHFResponse.Tables["getPreBookingInfoResponse"].Select();
                                                        int getPreBookingInfoResponse_Id = Convert.ToInt32(drgetPreBookingInfoResponse[0]["getPreBookingInfoResponse_Id"]);
                                                        DataRow[] drpreBookingInfo = IHFResponse.Tables["preBookingInfo"].Select("getPreBookingInfoResponse_Id='" + getPreBookingInfoResponse_Id + "'");
                                                        preBookingInfo_Id = Convert.ToInt32(drpreBookingInfo[0]["preBookingInfo_Id"]);
                                                        voucherObservation = Convert.ToString(drpreBookingInfo[0]["voucherObservation"]);
                                                        SPUI = voucherObservation;
                                                        offerCode = Convert.ToString(drpreBookingInfo[0]["offerCode"]);
                                                    }






                                                    //DataRow[] drcancellationFeesPolicy = IHFResponse.Tables["cancellationFeesPolicy"].Select("preBookingInfo_Id='" + preBookingInfo_Id + "'");
                                                    DataRow[] drcancellationFeesPolicy = IHFResponse.Tables["cancellationFeesPolicy"].Select("offer_Id='" + offer_Id + "'");
                                                    //DataTable dtcancel = IHFResponse.Tables["cancellationFeesPolicy"].Rows.Cast<DataRow>().Select("offer_Id='" + offer_Id + "'").OrderBy(row => Convert.ToString(row["fromDate"])).CopyToDataTable();

                                                    DataTable DTdrRoomcancellationRule = IHFResponse.Tables["cancellationFeesPolicy"]
                                                                        .AsEnumerable()
                                                                        .Where(row => row.Field<int>("offer_Id") == offer_Id)
                                                                        .OrderBy(row => row.Field<string>("fromDate"))  // or DateTime if column is DateTime
                                                                        .CopyToDataTable();

                                                    DTdrRoomcancellationRule.Columns.Add("datefilter", typeof(DateTime));
                                                    for (int x = 0; x < DTdrRoomcancellationRule.Rows.Count; x++)
                                                    {
                                                        string dtfrom = Convert.ToString(DTdrRoomcancellationRule.Rows[x]["fromDate"]);
                                                        dtfrom = dtfrom.Substring(3, 2) + "-" + dtfrom.Substring(0, 2) + "-" + dtfrom.Substring(6, 4);
                                                        DTdrRoomcancellationRule.Rows[x]["datefilter"] = Convert.ToDateTime(Convert.ToString(dtfrom));
                                                    }



                                                    DTdrRoomcancellationRule.DefaultView.Sort = "datefilter asc";
                                                    //DataTable DTdrRoomcancellationRule = dtcancel.Clone();
                                                    //for (int cr = 0; cr < dtcancel.DefaultView.Count; cr++)
                                                    //{
                                                    //    if (Convert.ToInt32(dtcancel.DefaultView[cr]["offer_Id"]) == offer_Id)
                                                    //    {
                                                    //        DTdrRoomcancellationRule.ImportRow(dtcancel.DefaultView[cr].Row);
                                                    //    }
                                                    //}
                                                    for (int ii = 0; ii < DTdrRoomcancellationRule.Rows.Count; ii++)
                                                    {
                                                        double price = Convert.ToDouble(DTdrRoomcancellationRule.Rows[ii]["price"]) * currencyfactor;
                                                        string fromDate = Convert.ToString(DTdrRoomcancellationRule.Rows[ii]["fromDate"]);
                                                        string toDate = Convert.ToString(DTdrRoomcancellationRule.Rows[ii]["toDate"]);
                                                        currency = Convert.ToString(DTdrRoomcancellationRule.Rows[ii]["currency"]);




                                                        DateTime curDate = _commonMethod.GetTimeZoneDate(SearchParam.CurrentDate);
                                                        //string uniqid = Guid.NewGuid().ToString();
                                                        #endregion

                                                        #region Cancellation Policy Logic and Row
                                                        DateTime DeadLineDate = DateTime.ParseExact(fromDate.Substring(0, 10), "dd/MM/yyyy", CultureInfo.InvariantCulture).Date;


                                                        #region Vat
                                                        double VatOnPolicy = 0;
                                                        double MainAgentCharge = price;
                                                        double CompanyChargeAmount = price;
                                                        double ChargeAmount = _markup.ProcessHotelCancelationPrice_WithMarkup(ref MainAgentCharge, SearchParam.NoofNights, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), price, isPerce, commonSearch.B2CMarkup, AgentPercentage, AgentMarkupAmount, commonSearch.IsParentId, commonSearch.SubAgentMarkup, ref VatOnPolicy);
                                                        #endregion

                                                        if (DeadLineDate.AddDays(CutOffDays).Date > DateTime.Now.Date)
                                                            DeadLineDate = DeadLineDate.AddDays(CutOffDays).Date;
                                                        else
                                                            DeadLineDate = DateTime.Now.Date;
                                                        if (curDate.Date < DeadLineDate.Date)
                                                        {
                                                            if (ii == 0)
                                                            {
                                                                double Zero = 0;
                                                                //dtCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref Zero, dtCancellation.NewRow(), currencyfactor, currency, Provider, Convert.ToString(OwnsystemHotelId), Convert.ToString(1), Convert.ToString(RoomTypeId), roomType_Text, Convert.ToString(boardType), curDate, DeadLineDate.AddDays(-1).AddHours(23).AddMinutes(59).AddSeconds(59), 0, 0, "", VatOnPolicy, "0", "NO", offerCode, SPUI, "", ""));
                                                                dtCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref Zero, dtCancellation.NewRow(), currencyfactor, currency, Provider, Convert.ToString(OwnsystemHotelId), Convert.ToString(1), Convert.ToString(RoomTypeId), roomType_Text, Convert.ToString(distinctMeals[0]), curDate, DeadLineDate.AddDays(-1).AddHours(23).AddMinutes(59).AddSeconds(59), 0, 0, "", VatOnPolicy, "0", "NO", offerCode, SPUI, "", ""));
                                                                dtCancellation.AcceptChanges();
                                                            }
                                                            //dtCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref CompanyChargeAmount, dtCancellation.NewRow(), currencyfactor, currency, Provider, Convert.ToString(OwnsystemHotelId), Convert.ToString(1), Convert.ToString(RoomTypeId), roomType_Text, Convert.ToString(boardType), DeadLineDate, DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null), MainAgentCharge, ChargeAmount, "", 0, "0", "NO", offerCode, SPUI, "", ""));
                                                            dtCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref CompanyChargeAmount, dtCancellation.NewRow(), currencyfactor, currency, Provider, Convert.ToString(OwnsystemHotelId), Convert.ToString(1), Convert.ToString(RoomTypeId), roomType_Text, Convert.ToString(distinctMeals[0]), DeadLineDate, DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null), MainAgentCharge, ChargeAmount, "", 0, "0", "NO", offerCode, SPUI, "", ""));
                                                            dtCancellation.AcceptChanges();
                                                        }
                                                        else
                                                        {
                                                            //dtCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref CompanyChargeAmount, dtCancellation.NewRow(), currencyfactor, currency, Provider, Convert.ToString(OwnsystemHotelId), Convert.ToString(1), Convert.ToString(RoomTypeId), roomType_Text, Convert.ToString(boardType), curDate, DeadLineDate, MainAgentCharge, ChargeAmount, "", 0, "0", "Yes", offerCode, SPUI, "", ""));
                                                            dtCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref CompanyChargeAmount, dtCancellation.NewRow(), currencyfactor, currency, Provider, Convert.ToString(OwnsystemHotelId), Convert.ToString(1), Convert.ToString(RoomTypeId), roomType_Text, Convert.ToString(distinctMeals[0]), curDate, DeadLineDate, MainAgentCharge, ChargeAmount, "", 0, "0", "Yes", offerCode, SPUI, "", ""));
                                                            dtCancellation.AcceptChanges();
                                                        }
                                                    }
                                                    #endregion
                                                }
                                                #endregion
                                            }
                                        }
                                    }

                                    #endregion

                                    Options ops = (await _supplierRes.GetCommonRoomForSuppliers(GiataId, commonSearch.tokenData, commonSearch.advancedOptions, Pricenew, drXmlHotel, dtHotelRoom, dtCancellation, DtRoomRateBreackUp, SearchParam));
                                    if (ops != null)
                                        options.Add(ops);
                                }
                                #endregion
                                #endregion

                            }
                            #endregion
                        }

                    }
                }
            }
            catch (Exception e)
            {
                _exception.Error(e);
            }
            finally
            {
                #region ResProcessTimer
                try
                {
                    if (SearchParam != null && ResProccessTimerLogActive == "1" && timerroomx.ElapsedMilliseconds > Convert.ToInt32(ResProccessTimeValueforSaveLog))
                    {
                        timerroomx.Stop();
                        SearchParam.SupplierTime += ",|Finish:InfinitesHotelsFrance Process Response  " + "|Time :" + timerroomx.Elapsed.ToString();
                        Task.Run(() => _commonMethod.FireAway_SQL_ResPorcessTimeLog("", timerroomx.Elapsed.ToString(), Supplier, SearchParam.SearchKey, 1, SearchParam.AgentID));
                    }
                }
                catch (Exception x)
                {
                    _exception.Error(x);
                }
                #endregion
            }
            return options;
        }

        //recheck
        public async Task<CancellationDto> Get_Infinitehotelsfrance_CancellationPolicy(string rating, string ContractID, string Type, DataSet dsHotel, string hotelcode, string ownsystemid, string RoomTypeId_Own, string MealID, string Board_Text, DataTable dtAgentDetail, DataTable dtSuplierMarkup, DataTable dtSearchConfig, HotelSearchParameter objSearPera, string roomno, string provider, int AgentId, DataTable dtMarkupDetails, string clientId, string customerKey, string GiataId = "0", int hotelRating = 0, double RoomPrice = 0, string CompanyBuyingPrice = "0")
        {
            string Error = "";
            string IsBooking = "Available";
            CancellationDto cancellationDto = new CancellationDto();
            try
            {
                DataTable dtInfinitehotelsfranceCancellation = _commonMethod.GenerateCancellationDataTableNewV2();


                #region Cancellation Policy RQ
                //DataRow[] drRoom = dsHotel.Tables[1].Select("RoomTypeId='" + RoomTypeId_Own + "'");
                //string RoomName = Convert.ToString(drRoom[0]["RoomTypeName"]);
                //MealID = Convert.ToString(drRoom[0]["MealId"]);

                string[] HBArray = Convert.ToString(RoomTypeId_Own).Split(new String[] { "$$$$" }, StringSplitOptions.None);
                string RoomCode = HBArray[0];
                string RoomName = HBArray[1];
                MealID = HBArray[2];


                Boolean isPerce = true;
                double AgentCompanyPercentage = Convert.ToDouble(objSearPera.AgentCompanyPercentage);
                bool IsB2C = Convert.ToBoolean(Convert.ToByte(objSearPera.IsB2BOrB2C));
                double B2CMarkup = (IsB2C == false ? AgentCompanyPercentage : XMLHotelBookingCommon.B2CMarkup);
                double AgentPercentage = 0.0;
                double AgentMarkupAmount = 0.0;
                double SubAgentMarkup = 0;
                int IsParentId = 1;
                Boolean blnIsShowSupplier = false;
                //Int32 hotelRating = 0;
                //hotelRating = Convert.ToInt32(rating);
                _markup.getAgetMarkupSetting(provider, GiataId, dtSuplierMarkup, dtMarkupDetails, ref AgentPercentage, ref AgentMarkupAmount, ref blnIsShowSupplier, ref isPerce, AgentId, hotelRating, objSearPera.CityID, Convert.ToString(objSearPera.CountryID), Convert.ToString(objSearPera.CheckInDate));
                if (dtAgentDetail != null)
                {
                    IsParentId = Convert.ToInt32(dtAgentDetail.Rows[0]["IsParentId"]);
                    SubAgentMarkup = Convert.ToDouble(dtAgentDetail.Rows[0]["B2CMarkupPer"]);
                }
                int CutOffDays = XMLHotelBookingCommon.CutOffDays;
                CutOffDays = _commonMethod.GetCutOffDays(dtSuplierMarkup, "infinitehotelsfrance");
                DataRow[] drSuplierMarkup = (dtSuplierMarkup.Select("SupplierName='infinitehotelsfrance'"));
                string UserId = Convert.ToString(drSuplierMarkup[0]["UserName"]);
                string Password = Convert.ToString(drSuplierMarkup[0]["Password"]);

                string Currency = Convert.ToString(drSuplierMarkup[0]["CurrencyShortName"]); //Added on 02-06-25

                DataTable dtInrCurrency = _commonMethod.GetCountryCurrency(Currency);
                double CurrencyFactorInfinitehotelsfrance = Convert.ToDouble(dtInrCurrency.Rows[0]["AmtToExch"]);


                string CheckIn = objSearPera.CheckInDate;
                CheckIn = CheckIn.Substring(6, 4) + "-" + CheckIn.Substring(3, 2) + "-" + CheckIn.Substring(0, 2);
                string CheckOut = objSearPera.CheckOutDate;
                CheckOut = CheckOut.Substring(6, 4) + "-" + CheckOut.Substring(3, 2) + "-" + CheckOut.Substring(0, 2);
                string InfinitehotelsfranceUrl = Convert.ToString(drSuplierMarkup[0]["WebSiteURL"]);
                int SID = Convert.ToInt32(drSuplierMarkup[0]["SupplierID"]);
                DataRow[] drSearchConfig = (dtSearchConfig.Select("SupplierID='" + SID + "'"));
                int SearchTimeOut = Convert.ToInt32(drSearchConfig[0]["BookingConfirmationTimeout"]);
                string PropertyId = ContractID;
                int NoOfNights = objSearPera.NoofNights;
                int RoomNo = Convert.ToInt32(roomno);
                int NoofAdults = objSearPera.Rooms[RoomNo - 1].NoofAdults;
                int NoOfChild = objSearPera.Rooms[RoomNo - 1].NoofChild;
                string RefNo = "";

                // string CancelPolicyRQ = Get_Infinitehotelsfrance_PriceCheckRQ(UserId, Password, "", CheckIn, CheckOut, RoomTypeId_Own, hotelcode);
                string CancelPolicyRQ = Get_Infinitehotelsfrance_PriceCheckRQ(UserId, Password, "", CheckIn, CheckOut, RoomCode, hotelcode);
                #endregion

                if (CancelPolicyRQ != null)
                {
                    #region Cancellation Policy RS
                    //string ResponseData = _commonMethod.GetSupplierResponse(ref Error, "", CancelPolicyRQ, InfinitehotelsfranceUrl, SearchTimeOut, provider);
                    ResponseDto responseDto = await _commonMethod.GetSupplierResponse("", CancelPolicyRQ, InfinitehotelsfranceUrl, SearchTimeOut, provider);
                    string ResponseData = responseDto.Response;
                    Error = responseDto.Error;
                    //ThirdPartyBookingHistory("testjac", "", CancelPolicyRQ, ResponseData, "testJac", "", "", "", "");
                    _commonMethod.ThirdPartyGetPolicyHistory(Convert.ToString(RoomCode), Convert.ToString(ownsystemid), CancelPolicyRQ, Convert.ToString(ResponseData), "After Get Policy", "infinitehotelsfrance", "Response", "-", "");

                    if (ResponseData == string.Empty || ResponseData.Contains("Error"))
                    {
                        cancellationDto.dsHotelData = dsHotel;
                        cancellationDto.ISBooking = "NotAvailable";
                        return cancellationDto;
                    }

                    ResponseData = ResponseData.Replace("<pricePerRoom><roomPrice", "<pricePerRoom><roomPrices");
                    if (objSearPera.Rooms.Count == 1)
                        ResponseData = ResponseData.Replace("</roomPrice>", "</roomPrices>");
                    else
                    {
                        ResponseData = ResponseData.Replace("</roomPrice>", "</roomPrices>");
                        ResponseData = ResponseData.Replace("</roomPrices><roomPrice", "</roomPrices><roomPrices");
                    }


                    DataSet InfinitehotelsfranceResponse = _commonMethod.GetDataSetFromXMLString(ResponseData);
                    #endregion

                    if (!InfinitehotelsfranceResponse.Tables.Contains("preBookingInfo"))
                    {
                        cancellationDto.dsHotelData = dsHotel;
                        cancellationDto.ISBooking = "NotAvailable";
                        return cancellationDto;
                    }
                    else
                    {
                        #region DataRaw

                        DataRow[] drgetPreBookingInfoResponse = InfinitehotelsfranceResponse.Tables["getPreBookingInfoResponse"].Select();
                        int getPreBookingInfoResponse_Id = Convert.ToInt32(drgetPreBookingInfoResponse[0]["getPreBookingInfoResponse_Id"]);
                        DataRow[] drpreBookingInfo = InfinitehotelsfranceResponse.Tables["preBookingInfo"].Select("getPreBookingInfoResponse_Id='" + getPreBookingInfoResponse_Id + "'");
                        int preBookingInfo_Id = Convert.ToInt32(drpreBookingInfo[0]["preBookingInfo_Id"]);
                        string voucherObservation = Convert.ToString(drpreBookingInfo[0]["voucherObservation"]);
                        string SPUI = voucherObservation;
                        bool Istrue = true;
                        double FinalPrice = 0;

                        string offerCode = Convert.ToString(drpreBookingInfo[0]["offerCode"]);


                        DataRow[] drcancellationFeesPolicy = InfinitehotelsfranceResponse.Tables["cancellationFeesPolicy"].Select("preBookingInfo_Id='" + preBookingInfo_Id + "'");
                        DataTable dtcancel = InfinitehotelsfranceResponse.Tables["cancellationFeesPolicy"].Rows.Cast<DataRow>().OrderBy(row => Convert.ToString(row["fromDate"])).CopyToDataTable();
                        dtcancel.Columns.Add("datefilter", typeof(DateTime));
                        for (int x = 0; x < dtcancel.Rows.Count; x++)
                        {
                            string dtfrom = Convert.ToString(dtcancel.Rows[x]["fromDate"]);
                            dtfrom = dtfrom.Substring(3, 2) + "-" + dtfrom.Substring(0, 2) + "-" + dtfrom.Substring(6, 4);
                            dtcancel.Rows[x]["datefilter"] = Convert.ToDateTime(Convert.ToString(dtfrom));
                        }
                        dtcancel.DefaultView.Sort = "datefilter asc";
                        DataTable DTdrRoomcancellationRule = dtcancel.Clone();
                        for (int cr = 0; cr < dtcancel.DefaultView.Count; cr++)
                        {
                            if (Convert.ToInt32(dtcancel.DefaultView[cr]["preBookingInfo_Id"]) == preBookingInfo_Id)
                            {
                                DTdrRoomcancellationRule.ImportRow(dtcancel.DefaultView[cr].Row);
                            }
                        }
                        for (int i = 0; i < DTdrRoomcancellationRule.Rows.Count; i++)
                        {
                            double price = Convert.ToDouble(DTdrRoomcancellationRule.Rows[i]["price"]) * CurrencyFactorInfinitehotelsfrance;
                            string fromDate = Convert.ToString(DTdrRoomcancellationRule.Rows[i]["fromDate"]);
                            string toDate = Convert.ToString(DTdrRoomcancellationRule.Rows[i]["toDate"]);
                            string currency = Convert.ToString(DTdrRoomcancellationRule.Rows[i]["currency"]);
                            string allDates = Convert.ToString(DTdrRoomcancellationRule.Rows[i]["allDates"]);
                            DataRow[] drofferPrice = InfinitehotelsfranceResponse.Tables["offerPrice"].Select("preBookingInfo_Id='" + preBookingInfo_Id + "'");
                            int offerPrice_Id = Convert.ToInt32(drofferPrice[0]["offerPrice_Id"]);
                            string changed = Convert.ToString(drofferPrice[0]["changed"]);
                            DataRow[] drtotalPrice = InfinitehotelsfranceResponse.Tables["totalPrice"].Select("offerPrice_Id='" + offerPrice_Id + "'");
                            string currency1 = Convert.ToString(drtotalPrice[0]["currency"]);
                            double value1 = Convert.ToDouble(drtotalPrice[0]["value"]) * CurrencyFactorInfinitehotelsfrance;
                            DataRow[] drpricePerRoom = InfinitehotelsfranceResponse.Tables["pricePerRoom"].Select("offerPrice_Id='" + offerPrice_Id + "'");
                            int pricePerRoom_Id = Convert.ToInt32(drpricePerRoom[0]["pricePerRoom_Id"]);
                            DataRow[] drroomPrices = InfinitehotelsfranceResponse.Tables["roomPrices"].Select("pricePerRoom_Id='" + pricePerRoom_Id + "'");
                            int roomPrices_Id = Convert.ToInt32(drroomPrices[0]["roomPrices_Id"]);
                            int roomIndex = Convert.ToInt32(drroomPrices[0]["roomIndex"]);
                            DataRow[] drroomPrice = InfinitehotelsfranceResponse.Tables["roomPrice"].Select("roomPrices_Id='" + roomPrices_Id + "'");
                            string currency2 = Convert.ToString(drroomPrice[0]["currency"]);
                            double value2 = Convert.ToDouble(drroomPrice[0]["value"]) * CurrencyFactorInfinitehotelsfrance;
                            FinalPrice = value1;
                            if (Convert.ToDouble((FinalPrice).TruncateDecimalPlaces(0)) > (Convert.ToDouble(CompanyBuyingPrice).TruncateDecimalPlaces(0)))
                            {
                                Istrue = true;
                                offerCode = offerCode + "@" + Istrue + "@" + FinalPrice;

                            }
                            else
                            {
                                Istrue = false;
                                offerCode = offerCode + "@" + Istrue + "@" + FinalPrice;
                            }



                            DateTime curDate = _commonMethod.GetTimeZoneDate(objSearPera.CurrentDate);
                            //string uniqid = Guid.NewGuid().ToString();
                            #endregion

                            #region Cancellation Policy Logic and Row
                            DateTime DeadLineDate = DateTime.ParseExact(fromDate.Substring(0, 10), "dd/MM/yyyy", CultureInfo.InvariantCulture).Date;


                            #region Vat
                            double VatOnPolicy = 0;
                            double MainAgentCharge = price;
                            double CompanyChargeAmount = price;
                            double ChargeAmount = _markup.ProcessHotelCancelationPrice_WithMarkup(ref MainAgentCharge, objSearPera.NoofNights, objSearPera.CityID, Convert.ToString(objSearPera.CountryID), price, isPerce, B2CMarkup, AgentPercentage, AgentMarkupAmount, IsParentId, SubAgentMarkup, ref VatOnPolicy);
                            #endregion

                            if (DeadLineDate.AddDays(CutOffDays).Date > DateTime.Now.Date)
                                DeadLineDate = DeadLineDate.AddDays(CutOffDays).Date;
                            else
                                DeadLineDate = DateTime.Now.Date;
                            if (curDate.Date < DeadLineDate.Date)
                            {
                                if (i == 0)
                                {
                                    double Zero = 0;
                                    dtInfinitehotelsfranceCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref Zero, dtInfinitehotelsfranceCancellation.NewRow(), CurrencyFactorInfinitehotelsfrance, Currency, "InfiniteHotelsFrance", ownsystemid, Convert.ToString(1), Convert.ToString(RoomCode), RoomName, Convert.ToString(MealID), curDate, DeadLineDate.AddDays(-1).AddHours(23).AddMinutes(59).AddSeconds(59), 0, 0, "", VatOnPolicy, "0", "NO", offerCode, SPUI, "", ""));
                                    dtInfinitehotelsfranceCancellation.AcceptChanges();
                                }
                                dtInfinitehotelsfranceCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref CompanyChargeAmount, dtInfinitehotelsfranceCancellation.NewRow(), CurrencyFactorInfinitehotelsfrance, Currency, "InfiniteHotelsFrance", ownsystemid, Convert.ToString(1), Convert.ToString(RoomCode), RoomName, Convert.ToString(MealID), DeadLineDate, DateTime.ParseExact(objSearPera.CheckInDate, "dd/MM/yyyy", null), RoomPrice, RoomPrice, "", 0, "0", "NO", offerCode, SPUI, "", ""));
                                dtInfinitehotelsfranceCancellation.AcceptChanges();
                            }
                            else
                            {
                                dtInfinitehotelsfranceCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref CompanyChargeAmount, dtInfinitehotelsfranceCancellation.NewRow(), CurrencyFactorInfinitehotelsfrance, Currency, "InfiniteHotelsFrance", ownsystemid, Convert.ToString(1), Convert.ToString(RoomCode), RoomName, Convert.ToString(MealID), curDate, DeadLineDate, RoomPrice, RoomPrice, "", 0, "0", "Yes", offerCode, SPUI, "", ""));
                                dtInfinitehotelsfranceCancellation.AcceptChanges();
                            }
                        }
                        #endregion
                    }
                }
                dsHotel.Tables[2].Merge(dtInfinitehotelsfranceCancellation);

                cancellationDto.dsHotelData = dsHotel;
                cancellationDto.ISBooking = IsBooking;
                return cancellationDto;
            }
            catch (Exception ex)
            {
                _ = _exception.Error(ex);
                cancellationDto.dsHotelData = null;
                return cancellationDto;
            }
        }

        public async Task<string> Get_Infinitehotelsfrance_EInfo(string hotelcode, string ownsystemid, string RoomTypeId_Own, DataTable dtSuplierMarkup, DataTable dtSearchConfig, HotelSearchParameter objSearPera, string provider, string SearchKey)
        {
            string Error = "";
            string SPUI = "";
            string ResponseData = "";
            try
            {

                #region Cancellation Policy RQ
                //DataRow[] drRoom = dsHotel.Tables[1].Select("RoomTypeId='" + RoomTypeId_Own + "'");
                //string RoomName = Convert.ToString(drRoom[0]["RoomTypeName"]);
                //MealID = Convert.ToString(drRoom[0]["MealId"]);

                string[] HBArray = Convert.ToString(RoomTypeId_Own).Split(new String[] { "$$$$" }, StringSplitOptions.None);
                string RoomCode = HBArray[0];


                DataRow[] drSuplierMarkup = (dtSuplierMarkup.Select("SupplierName='infinitehotelsfrance'"));
                string UserId = Convert.ToString(drSuplierMarkup[0]["UserName"]);
                string Password = Convert.ToString(drSuplierMarkup[0]["Password"]);
                string CheckIn = objSearPera.CheckInDate;
                CheckIn = CheckIn.Substring(6, 4) + "-" + CheckIn.Substring(3, 2) + "-" + CheckIn.Substring(0, 2);
                string CheckOut = objSearPera.CheckOutDate;
                CheckOut = CheckOut.Substring(6, 4) + "-" + CheckOut.Substring(3, 2) + "-" + CheckOut.Substring(0, 2);
                string InfinitehotelsfranceUrl = Convert.ToString(drSuplierMarkup[0]["WebSiteURL"]);
                int SID = Convert.ToInt32(drSuplierMarkup[0]["SupplierID"]);
                DataRow[] drSearchConfig = (dtSearchConfig.Select("SupplierID='" + SID + "'"));
                int SearchTimeOut = Convert.ToInt32(drSearchConfig[0]["BookingConfirmationTimeout"]);

                string CancelPolicyRQ = Get_Infinitehotelsfrance_PriceCheckRQ(UserId, Password, "", CheckIn, CheckOut, RoomCode, hotelcode);
                #endregion

                if (CancelPolicyRQ != null)
                {
                    #region Cancellation Policy RS
                    ResponseDto responseDto = await _commonMethod.GetSupplierResponse("", CancelPolicyRQ, InfinitehotelsfranceUrl, SearchTimeOut, provider);
                    ResponseData = responseDto.Response;

                    //ThirdPartyBookingHistory("testjac", "", CancelPolicyRQ, ResponseData, "testJac", "", "", "", "");
                    _commonMethod.ThirdPartyGetPolicyHistory(Convert.ToString(RoomCode), Convert.ToString(ownsystemid), CancelPolicyRQ, Convert.ToString(ResponseData), "After Get Policy", "infinitehotelsfrance", "Response", "-", "");

                    if (ResponseData == string.Empty || ResponseData.Contains("Error"))
                        return "";
                    ResponseData = ResponseData.Replace("<pricePerRoom><roomPrice", "<pricePerRoom><roomPrices");
                    if (objSearPera.Rooms.Count == 1)
                        ResponseData = ResponseData.Replace("</roomPrice>", "</roomPrices>");
                    else
                    {
                        ResponseData = ResponseData.Replace("</roomPrice>", "</roomPrices>");
                        ResponseData = ResponseData.Replace("</roomPrices><roomPrice", "</roomPrices><roomPrices");
                    }


                    DataSet InfinitehotelsfranceResponse = _commonMethod.GetDataSetFromXMLString(ResponseData);
                    #endregion

                    if (!InfinitehotelsfranceResponse.Tables.Contains("preBookingInfo"))
                        return "";

                    else
                    {
                        #region DataRaw

                        DataRow[] drgetPreBookingInfoResponse = InfinitehotelsfranceResponse.Tables["getPreBookingInfoResponse"].Select();
                        int getPreBookingInfoResponse_Id = Convert.ToInt32(drgetPreBookingInfoResponse[0]["getPreBookingInfoResponse_Id"]);
                        DataRow[] drpreBookingInfo = InfinitehotelsfranceResponse.Tables["preBookingInfo"].Select("getPreBookingInfoResponse_Id='" + getPreBookingInfoResponse_Id + "'");
                        string voucherObservation = Convert.ToString(drpreBookingInfo[0]["voucherObservation"]);
                        SPUI = voucherObservation;


                        #endregion
                    }
                }


                return SPUI;
            }
            catch (Exception ex)
            {
                _ = _exception.ErrorNew(ex, _LogAPI.Recheck);
                return "";
            }
        }
        public string Get_Infinitehotelsfrance_PriceCheckRQ(string clientId, string Password, string customerKey, string CheckIn, string CheckOut, string offerCode, string hotelcode)
        {
            try
            {
                #region Cancellation Policy and Price Check RQ

                string NationalityCode = "";
                StringBuilder Infinitehotelsfrance_CancelPolicyRQ = new StringBuilder();
                Infinitehotelsfrance_CancelPolicyRQ.Append("<soapenv:Envelope xmlns:soapenv=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:v2=\"http://webservice.gekko-holding.com/v2_4\">");
                Infinitehotelsfrance_CancelPolicyRQ.Append("<soapenv:Header/>");
                Infinitehotelsfrance_CancelPolicyRQ.Append("<soapenv:Body>");
                Infinitehotelsfrance_CancelPolicyRQ.Append("<v2:getPreBookingInfo>");
                Infinitehotelsfrance_CancelPolicyRQ.Append("<language>EN</language>");
                Infinitehotelsfrance_CancelPolicyRQ.Append("<identification clientId=\"" + clientId + "\" password=\"" + Password + "\" customerKey=\"" + customerKey + "\"/>");
                Infinitehotelsfrance_CancelPolicyRQ.Append("<searchCriteria>");
                Infinitehotelsfrance_CancelPolicyRQ.Append("<hotelCode code=\"" + hotelcode + "\"/>");
                Infinitehotelsfrance_CancelPolicyRQ.Append("<startDate>" + CheckIn + "</startDate>");
                Infinitehotelsfrance_CancelPolicyRQ.Append("<endDate>" + CheckOut + "</endDate>");
                Infinitehotelsfrance_CancelPolicyRQ.Append("<offerCode>" + offerCode + "</offerCode>");
                Infinitehotelsfrance_CancelPolicyRQ.Append("</searchCriteria>");
                Infinitehotelsfrance_CancelPolicyRQ.Append("</v2:getPreBookingInfo>");
                Infinitehotelsfrance_CancelPolicyRQ.Append("</soapenv:Body>");
                Infinitehotelsfrance_CancelPolicyRQ.Append("</soapenv:Envelope>");

                string searchRQString = Infinitehotelsfrance_CancelPolicyRQ.ToString();
                return searchRQString;

                #endregion
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
                return "";
            }

        }

        public async Task<PriceCheckDidaDto> Get_Infinitehotelsfrance_PreBookingReqRes(TokenData tokenData, string Currency, string ID, string Password, string HotelCode, string RCode, string CheckInDate, string CheckOutDate, string URL, double Rate, double FinalPrice, int UserCartId, int UserCartDetailId, DataTable dtCheckPriceReqStructure, int j, string UniqueNo, string AgentUniqueNo, bool isForBooking, int NoOfRoom, string UserName, string Newoffercode, string Searchkey, int AgentId = 0)
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
            string SupplierName = "infinitehotelsfrance";
            string Error = "";
            string Req = "";
            string ResponseData = "";
            int flag = 1;

            string[] HBArray = Convert.ToString(RCode).Split(new String[] { "$$$$" }, StringSplitOptions.None);
            string RoomCode = HBArray[0];


            try
            {
                double CurrencyFactorInfiniteHotelsFrance = _commonMethod.GetROEByCurrency(Currency, tokenData.dtROE);

                //DataTable dtInrCurrency = Common.GetCountryCurrency(XMLHotelBookingCommon.InfinitehotelsfranceCurrency);
                //double CurrencyFactorInfiniteHotelsFrance = Convert.ToDouble(dtInrCurrency.Rows[0]["AmtToExch"]);
                CheckInDate = CheckInDate.Substring(0, 4) + "-" + CheckInDate.Substring(4, 2) + "-" + CheckInDate.Substring(6, 2);
                CheckOutDate = CheckOutDate.Substring(0, 4) + "-" + CheckOutDate.Substring(4, 2) + "-" + CheckOutDate.Substring(6, 2);

                Req = Get_Infinitehotelsfrance_PriceCheckRQ(UserName, Password, "", CheckInDate, CheckOutDate, RoomCode, HotelCode);
                _commonMethod.ThirdPartyBookingHistory(Convert.ToString(UniqueNo), Convert.ToString(AgentUniqueNo), Req, "-", "Before Get Price infinitehotelsfrance Request hit booking" + j + "isForBooking" + isForBooking, "infinitehotelsfrance", "Response Confirm", "-", "WithinEarth");

                if (isForBooking == false)// hit if not from booking  
                {
                    dtCheckPriceReqStructure.Rows.Add(_commonMethod.AddCheckPriceReq(dtCheckPriceReqStructure.NewRow(), j + 1, UserCartId, UserCartDetailId, RoomCode, "infinitehotelsfrance", Req));
                }

                string Action = URL;

                System.Diagnostics.Stopwatch timerroomprocess = new System.Diagnostics.Stopwatch();
                timerroomprocess.Start();

                //ResponseData = _commonMethod.GetSupplierResponse_report(ref Error, "", Req, Action, 120000, "infinitehotelsfrance");
                ResponseDto responseDto = await _commonMethod.GetSupplierResponse("", Req, Action, 120000, SupplierName);
                ResponseData = responseDto.Response;
                Error = responseDto.Error;

                timerroomprocess.Stop();
                time = timerroomprocess.Elapsed.ToString();

                _commonMethod.ThirdPartyBookingHistory(Convert.ToString(UniqueNo), Convert.ToString(AgentUniqueNo), Req, ResponseData + Error, "After Get Price infinitehotelsfrance Request hit booking" + j + "isForBooking" + isForBooking, "infinitehotelsfrance", "Response Confirm", "-", "WithinEarth");
                if (ResponseData == string.Empty)
                {
                    //if (ResponseData.Contains("No available room"))
                    //  return 4;
                    //else
                    try
                    {
                        responsetype = "GeneralError";
                        string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, Req, ResponseData, Error);
                    }
                    catch (Exception e)
                    {
                        _ = _exception.Error(e, Searchkey);
                    }
                    priceCheckDidaDto.Flag = flag;
                    return priceCheckDidaDto;
                }
                ResponseData = ResponseData.Replace("<pricePerRoom><roomPrice", "<pricePerRoom><roomPrices");
                if (NoOfRoom == 1)
                    ResponseData = ResponseData.Replace("</roomPrice>", "</roomPrices>");
                else
                {
                    ResponseData = ResponseData.Replace("</roomPrice>", "</roomPrices>");
                    ResponseData = ResponseData.Replace("</roomPrices><roomPrice", "</roomPrices><roomPrices");
                }
                //ResponseData = ResponseData.Replace("<pricePerRoom><roomPrice", "<pricePerRoom><roomPrices");
                //ResponseData = ResponseData.Replace("</roomPrice>", "</roomPrices>");
                //ResponseData = Newtonsoft.Json.JsonConvert.DeserializeXmlNode(ResponseData).InnerXml;
                if (ResponseData.Contains("faultstring"))
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
                    priceCheckDidaDto.Flag = 1;
                    return priceCheckDidaDto;
                }
                DataSet InfinitehotelsfranceResponse = _commonMethod.GetDataSetFromXMLString(ResponseData);
                DataRow[] drRes = InfinitehotelsfranceResponse.Tables["preBookinginfo"].Select();
                double TotalPrice = 0;
                for (int i = 0; i < drRes.Length; i++)
                {
                    DataRow[] drgetPreBookingInfoResponse = InfinitehotelsfranceResponse.Tables["getPreBookingInfoResponse"].Select();
                    int getPreBookingInfoResponse_Id = Convert.ToInt32(drgetPreBookingInfoResponse[0]["getPreBookingInfoResponse_Id"]);
                    int Body_Id = Convert.ToInt32(drgetPreBookingInfoResponse[0]["Body_Id"]);
                    DataRow[] drpreBookingInfo = InfinitehotelsfranceResponse.Tables["preBookingInfo"].Select("getPreBookingInfoResponse_Id='" + getPreBookingInfoResponse_Id + "'");
                    int preBookingInfo_Id1 = Convert.ToInt32(drpreBookingInfo[0]["preBookingInfo_Id"]);
                    string voucherObservation = Convert.ToString(drpreBookingInfo[0]["voucherObservation"]);
                    string offerCode = Convert.ToString(drpreBookingInfo[0]["offerCode"]);
                    Newoffercode = offerCode;

                    priceCheckDidaDto.ReferenceNo = Newoffercode;//02-07-25

                    int GetPreBookingInfoResponse_Id = Convert.ToInt32(drpreBookingInfo[0]["GetPreBookingInfoResponse_Id"]);
                    DataRow[] drcancellationFeesPolicy = InfinitehotelsfranceResponse.Tables["cancellationFeesPolicy"].Select("preBookingInfo_Id='" + preBookingInfo_Id1 + "'");
                    string fromDate = Convert.ToString(drcancellationFeesPolicy[0]["fromDate"]);
                    string toDate = Convert.ToString(drcancellationFeesPolicy[0]["toDate"]);
                    double price = Convert.ToDouble(drcancellationFeesPolicy[0]["price"]) * CurrencyFactorInfiniteHotelsFrance;
                    string currency1 = Convert.ToString(drcancellationFeesPolicy[0]["currency"]);
                    string allDates = Convert.ToString(drcancellationFeesPolicy[0]["allDates"]);
                    int preBookingInfo_Id2 = Convert.ToInt32(drcancellationFeesPolicy[0]["PreBookingInfo_Id"]);
                    DataRow[] drofferPrice = InfinitehotelsfranceResponse.Tables["offerPrice"].Select("PreBookingInfo_Id='" + preBookingInfo_Id2 + "'");
                    int offerPrice_Id1 = Convert.ToInt32(drofferPrice[0]["offerPrice_Id"]);
                    string changed = Convert.ToString(drofferPrice[0]["changed"]);
                    int preBookingInfo_Id = Convert.ToInt32(drofferPrice[0]["preBookingInfo_id"]);
                    DataRow[] drtotalPrice = InfinitehotelsfranceResponse.Tables["totalPrice"].Select("offerPrice_Id='" + offerPrice_Id1 + "'");
                    string currency2 = Convert.ToString(drtotalPrice[0]["currency"]);
                    double value1 = Convert.ToDouble(drtotalPrice[0]["value"]) * CurrencyFactorInfiniteHotelsFrance;
                    int offerPrice_Id2 = Convert.ToInt32(drtotalPrice[0]["offerPrice_Id"]);
                    DataRow[] drpricePerRoom = InfinitehotelsfranceResponse.Tables["pricePerRoom"].Select("offerPrice_Id='" + offerPrice_Id2 + "'");
                    int pricePerRoom_Id1 = Convert.ToInt32(drpricePerRoom[0]["pricePerRoom_Id"]);
                    int offerPrice_Id3 = Convert.ToInt32(drpricePerRoom[0]["offerPrice_Id"]);
                    DataRow[] drroomPrices = InfinitehotelsfranceResponse.Tables["roomPrices"].Select("pricePerRoom_Id='" + pricePerRoom_Id1 + "'");
                    int roomPrices_Id1 = Convert.ToInt32(drroomPrices[0]["roomPrices_Id"]);
                    int roomIndex = Convert.ToInt32(drroomPrices[0]["roomIndex"]);
                    int pricePerRoom_Id2 = Convert.ToInt32(drroomPrices[0]["pricePerRoom_Id"]);
                    DataRow[] drroomPrice = InfinitehotelsfranceResponse.Tables["roomPrice"].Select("roomPrices_Id='" + roomPrices_Id1 + "'");
                    string currency = Convert.ToString(drroomPrice[0]["currency"]);
                    double value2 = Convert.ToDouble(drroomPrice[0]["value"]);
                    int roomPrices_Id2 = Convert.ToInt32(drroomPrice[0]["roomPrices_Id"]);

                    FinalPrice = value1;

                    priceCheckDidaDto.FinalPrice = FinalPrice; //02-07-25
                }
                if (Convert.ToDouble((FinalPrice).TruncateDecimalPlaces(0)) > (Convert.ToDouble(Rate).TruncateDecimalPlaces(0)))
                {
                    try
                    {
                        ResponseData = "#Price Changed " + "#Main Price = " + Rate + " #New Price = " + FinalPrice + " #Response= " + ResponseData;
                        responsetype = "GeneralError";
                        string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, Req, ResponseData, Error);
                    }
                    catch (Exception e)
                    {
                        _ = _exception.Error(e, Searchkey);
                    }

                    priceCheckDidaDto.Flag = 5;
                    return priceCheckDidaDto;

                }
            }
            catch (Exception ex)
            {
                try
                {
                    Error = ex.Message + ex.StackTrace.ToString();
                    responsetype = "GeneralError";
                    string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, Req, ResponseData, Error);
                }
                catch (Exception e)
                {
                    _ = _exception.Error(e, Searchkey);
                }
                _ = _exception.Error(ex);

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
            return priceCheckDidaDto;
        }

        public string Get_Infinitehotelsfrance_BookingRequest(string password, string customerkey, string hotelCode, string code, DataSet dsCartDetails, DataTable dtPassengers, DataRow[] InfinitehotelsfranceDetails, string UniqueNo, int i, int j, string PreBookingToken, string UserName, string Newoffercode, string AgentCode)
        {
            try
            {
                #region Booking RQ
                string clientId = UserName;
                string firstName = "";
                string lastName = "";
                int NoOfRoom = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView.Count);
                //string UserID = Convert.ToString(InfinitehotelsfranceDetails[0]["UserId"]);
                //string Password = Convert.ToString(InfinitehotelsfranceDetails[0]["Password"]);
                string CheckIn = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["CheckInDate"]);
                CheckIn = CheckIn.Substring(0, 4) + "-" + CheckIn.Substring(4, 2) + "-" + CheckIn.Substring(6, 2);
                string CheckOut = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["CheckOutDate"]);
                CheckOut = CheckOut.Substring(0, 4) + "-" + CheckOut.Substring(4, 2) + "-" + CheckOut.Substring(6, 2);
                string PropertyId = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["ContractID"]);
                string MealId = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["MealPlanCode"]);
                string AgentReference = "Book_" + Convert.ToString(AgentCode + "_" + UniqueNo);

                string RoomType = "";
                RoomType = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomTypeId"]);
                string NationalityCode = "";
                StringBuilder SBPassanger = new StringBuilder();
                for (int r = 0; r < NoOfRoom; r++)
                {
                    #region  IsLead Filter Logic
                    int NoofAdults = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[r]["NoOfAdults"]);
                    int NoOfChild = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[r]["NoOfChild"]);
                    string isLead = string.Empty;
                    dtPassengers.DefaultView.RowFilter = "PickUp='" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomTypeID"]) + "'and RoomNo='" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[r]["RoomNumber"]) + "' and IsLead=1";
                    if (dtPassengers.DefaultView.Count == 0)
                        isLead = "true";
                    dtPassengers.DefaultView.RowFilter = "";
                    dtPassengers.DefaultView.RowFilter = "PickUp='" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomTypeID"]) + "' and RoomNo='" + (dsCartDetails.Tables[1].DefaultView[r]["RoomNumber"]) + "' and UserCartId='" + dsCartDetails.Tables[0].Rows[i]["UserCartId"] + "'";
                    dtPassengers.DefaultView.Sort = "IsLead desc";
                    #endregion
                    SBPassanger.Append("<bookedRoom roomIndex=\"" + r + "\">");

                    if (dtPassengers.DefaultView.Count > 0)
                    {
                        string title = "";
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

                            SBPassanger.Append("<v2:pax title =\"" + title + "\"");

                            if (Convert.ToString(dtPassengers.DefaultView[k]["PaxType"]).Trim().ToLower() != "adult")
                            {
                                int ChildAge1 = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[j]["ChildAge1"]);
                                int ChildAge2 = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[j]["ChildAge2"]);
                                if (NoOfChild == 1)
                                    SBPassanger.Append(" isChild=\"true\" childAge=\"" + ChildAge + "\"");
                                if (NoOfChild == 2)
                                    SBPassanger.Append(" isChild=\"true\" childAge=\"" + ChildAge + "\"");
                            }
                            SBPassanger.Append(">");
                            SBPassanger.Append("<firstName>" + firstName + "</firstName>");
                            SBPassanger.Append("<lastName>" + lastName + "</lastName>");
                            SBPassanger.Append("</v2:pax>");
                        }
                        SBPassanger.Append("</bookedRoom>");
                    }
                }

                StringBuilder BookRQ = new StringBuilder();
                BookRQ.Append("<soapenv:Envelope xmlns:soapenv=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:v2=\"http://webservice.gekko-holding.com/v2_4\">");
                BookRQ.Append("<soapenv:Header/>");
                BookRQ.Append("<soapenv:Body>");
                BookRQ.Append("<v2:bookHotel>");
                BookRQ.Append("<language>EN</language>");
                BookRQ.Append("<identification clientId=\"" + clientId + "\" password=\"" + password + "\" customerkey=\"" + customerkey + "\"/>");
                BookRQ.Append("<bookRequest insurance=\"true\">");
                BookRQ.Append("<statisticalFields>");
                //BookRQ.Append("<statisticalField type=\"REM_3\" value=\"val1\"/>");
                //BookRQ.Append("<statisticalField type=\"RM_EXTERNAL_REF\" value=\"yourRef\"/>");
                BookRQ.Append("<statisticalField type=\"RM_EXTERNAL_REF\" value=\"" + AgentReference + "\"/>");
                BookRQ.Append("</statisticalFields>");
                //BookRQ.Append("<creditCard>");
                //BookRQ.Append("<cardholder>John Doe</cardholder>");
                //BookRQ.Append("<cardNumber>XXXXXXXXXXXXXXXXXXX</cardNumber>");
                //BookRQ.Append("<cvc>XXX</cvc>");
                //BookRQ.Append("<expiryDate>09/12</expiryDate>");
                //BookRQ.Append("</creditCard>");
                BookRQ.Append("<hotelBooking hotelCode=\"" + hotelCode + "\">");
                BookRQ.Append("<checkIn>" + CheckIn + "</checkIn>");
                BookRQ.Append("<checkOut>" + CheckOut + "</checkOut>");
                BookRQ.Append("<bookedOffer>");
                BookRQ.Append("<code>" + Newoffercode + "</code>");
                BookRQ.Append("<bookedRooms>");
                BookRQ.Append(SBPassanger.ToString());
                BookRQ.Append("</bookedRooms>");
                BookRQ.Append("</bookedOffer>");
                BookRQ.Append("</hotelBooking>");
                BookRQ.Append("</bookRequest>");
                BookRQ.Append("</v2:bookHotel>");
                BookRQ.Append("</soapenv:Body>");
                BookRQ.Append("</soapenv:Envelope>");

                string BookingRQ = BookRQ.ToString();

                return BookingRQ;
                #endregion
            }
            catch (Exception ex)
            {
                _ = _exception.ErrorNew(ex,_LogAPI.Book);
                return "";
            }
        }

        public string Get_InfiniteHotelsFrance_BookingDetails_RQ(DataRow[] IHFDetails, string clientId, string password, string customerKey, string AgentRefrance)
        {
            try
            {
                #region InfiniteHotelsFrance  Booking Deatils request


                StringBuilder InfiniteHotelsFrance_BookingDetailsRQ = new StringBuilder();

                InfiniteHotelsFrance_BookingDetailsRQ.Append("<soapenv:Envelope xmlns:soapenv=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:v2=\"http://webservice.gekko-holding.com/v2_4\">");
                InfiniteHotelsFrance_BookingDetailsRQ.Append("<soapenv:Header/>");
                InfiniteHotelsFrance_BookingDetailsRQ.Append("<soapenv:Body>");
                InfiniteHotelsFrance_BookingDetailsRQ.Append("<v2:getBookingDetails>");
                InfiniteHotelsFrance_BookingDetailsRQ.Append("<language>EN</language>");
                InfiniteHotelsFrance_BookingDetailsRQ.Append("<identification clientId=\"" + clientId + "\" password=\"" + password + "\" customerKey=\"" + customerKey + "\"/>");
                //InfiniteHotelsFrance_BookingDetailsRQ.Append("<bookId>WZGAYU</bookId>");
                InfiniteHotelsFrance_BookingDetailsRQ.Append("<segmentId>EXTERNAL:" + AgentRefrance + "</segmentId>");
                InfiniteHotelsFrance_BookingDetailsRQ.Append("</v2:getBookingDetails>");
                InfiniteHotelsFrance_BookingDetailsRQ.Append("</soapenv:Body>");
                InfiniteHotelsFrance_BookingDetailsRQ.Append("</soapenv:Envelope>");

                string BookingDeatilsRQString = InfiniteHotelsFrance_BookingDetailsRQ.ToString();
                return BookingDeatilsRQString;
                #endregion
            }
            catch (Exception ex)
            {
                _ = _exception.ErrorNew(ex, _LogAPI.Book);
                return "";
            }

        }

        public string Get_InfiniteHotelsFrance_CancelationRQ(string RefNO, DataRow[] InfiniteHotelsFranceDetails)
        {
            try
            {
                string clientId = Convert.ToString(InfiniteHotelsFranceDetails[0]["UserName"]);
                string password = Convert.ToString(InfiniteHotelsFranceDetails[0]["Password"]);
                string customerKey = "";
                string request = "";
                StringBuilder sbreq = new StringBuilder();
                sbreq.Append("<soapenv:Envelope xmlns:soapenv=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:v2=\"http://webservice.gekko-holding.com/v2_4\">");
                sbreq.Append("<soapenv:Header/>");
                sbreq.Append("<soapenv:Body>");
                sbreq.Append("<v2:cancelBookingSegment>");
                sbreq.Append("<language>EN</language>");
                sbreq.Append("<identification clientId=\"" + clientId + "\" password=\"" + password + "\" customerKey=\"" + customerKey + "\"/>");
                sbreq.Append("<segmentId>" + RefNO + "</segmentId>");
                sbreq.Append("</v2:cancelBookingSegment>");
                sbreq.Append("</soapenv:Body>");
                sbreq.Append("</soapenv:Envelope>");
                request = sbreq.ToString();
                return request;
            }
            catch (Exception ex)
            {
                _ = _exception.ErrorNew(ex, _LogAPI.CancelBooking);
                return "";
            }

        }

    }
}
