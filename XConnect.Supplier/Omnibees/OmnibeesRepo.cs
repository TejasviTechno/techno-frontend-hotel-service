using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using XConnect.CommonUtilities;
using XConnect.CommonUtilities.Interface;
using XConnect.ErrorLogHandler.Interface;
using XConnect.Markup.Interface;
using XConnect.Models;
using static Azure.Core.HttpHeader;
using XConnect.Models.Hotel;
using System.Text.Json;
using Options = XConnect.Models.Options;
using XConnect.Supplier.Omnibees.Interface;
using XConnect.Supplier.MGBedBank;
using Microsoft.Extensions.Configuration;
using System.Net;


namespace XConnect.Supplier.Omnibees
{
    public class OmnibeesRepo : IOmnibees
    {
        private readonly ICommonMethod _commonMethod;
        private readonly IException _exception;
        private readonly IMarkup _markup;
        private readonly ISupplierRes _supplierRes;
        private readonly KeySettings _keySettings;
        private readonly IVCN _vcn;
        private readonly SupplierConfiguration _supplierConfig;
        public OmnibeesRepo(ICommonMethod commonMethod, IMarkup markup, ISupplierRes supplierRes, IException exception, IOptions<KeySettings> keySettings, IVCN vcn, IOptions<SupplierConfiguration> supplierConfig)
        {
            _commonMethod = commonMethod;
            _markup = markup;
            _supplierRes = supplierRes;
            _exception = exception;
            _keySettings = keySettings.Value;
            _vcn = vcn;
            _supplierConfig = supplierConfig.Value;
        }


        public void GetOmnibeesRequestOnly_async(DataRow[] commonCode, List<StarRating> listRating, HotelSearchParameter SearchPara, List<SupplierDetails> OmnibeesDetails, List<SupplierSearchConfig> OmnibeesSearchConfig, DataTable dtPreferredDataHotel, ref ConcurrentBag<HotelX> AllTask, string XMLCity, string Xmlnationality, int Hit, DataSet dtXMLCity, DataTable dtROE, TokenData tokenData)
        {
            try
            {
                string Ids = string.Empty;
                string xmlbase = "";
                _commonMethod.GetPrefferedHotelCodes(ref Ids, ref xmlbase, ref OmnibeesSearchConfig, "Omnibees", dtPreferredDataHotel, SearchPara.IsSearchFor);
                if (OmnibeesSearchConfig != null && OmnibeesDetails.Count > 0)
                {
                    double CurrencyFactorOmnibees = 1;
                    string CurrencyCode = Convert.ToString(OmnibeesDetails[0].CurrencyShortName);
                    if (CurrencyCode.ToLower() != Convert.ToString(tokenData.SiteCurrency).ToLower())
                    {
                        //DataTable dtInrCurrency = Common.GetCountryCurrency(CurrencyCode);
                        CurrencyFactorOmnibees = Convert.ToDouble(_commonMethod.GetROEByCurrency(CurrencyCode, dtROE));

                        //CurrencyFactorOmnibees = Convert.ToDouble(dtInrCurrency.Rows[0]["AmtToExch"]);
                    }
                    else
                        CurrencyFactorOmnibees = 1;
                    for (int k = 0; k < OmnibeesSearchConfig.Count; k++)
                    {
                        bool DebugMode = (Convert.ToInt32(OmnibeesSearchConfig[k].DebugMode) == 0 ? false : true);
                        if (Xmlnationality != "")
                        {
                            // List<HotelCodeByCity> DtHotelDAta = FilterFinalList(commonCode, listRating);
                            if (commonCode.Length > 0)

                            {
                                List<HotelCodeByCity> DtHotelDAta = HtmlExtensions.ToList<HotelCodeByCity>(commonCode.CopyToDataTable());

                                if (Convert.ToInt32(OmnibeesSearchConfig[k].CityID) == 0 || Convert.ToInt32(OmnibeesSearchConfig[k].CityID) == SearchPara.CityID)//All City means 0
                                {
                                    int Noofrecords = Convert.ToInt32(OmnibeesSearchConfig[k].Noofrecords);
                                    double count = Convert.ToDouble(DtHotelDAta.Count);
                                    int NoOfDiv = Convert.ToInt32(Math.Ceiling(count / Convert.ToDouble(Noofrecords)));
                                    List<List<XConnect.Models.XMLID_Search>> lstxml = _commonMethod.GetXMLIDString(NoOfDiv, Noofrecords, count, DtHotelDAta);
                                    for (int a = 0; a < NoOfDiv; a++)
                                    {
                                        string IdsForSearch = Convert.ToString(_commonMethod.GetHotelCodeByCOmma(lstxml[a]));
                                        HotelX xhotel = new HotelX();
                                        xhotel.Supplier = "Omnibees";
                                        xhotel.DebugeMode = DebugMode;
                                        xhotel.SearchTimeOut = Convert.ToInt32(OmnibeesSearchConfig[k].HotelSearchTimeOut);
                                        //if (Hit > 0)
                                        xhotel.ProcessTimeout = Convert.ToInt32(OmnibeesSearchConfig[k].HotelSearchTimeOut2);

                                        //xhotel.URL = Convert.ToString(OmnibeesDetails[0].WebSiteURL) + OmnibeesStaticInfo.OmnibessHotelAvai;

                                        if (_keySettings.SupplierProxy == "0")
                                            xhotel.URL = Convert.ToString(OmnibeesDetails[0].WebSiteURL) + _supplierConfig.Omnibees.OmnibessHotelAvai;
                                        else
                                            xhotel.URL = Convert.ToString(OmnibeesDetails[0].WebSiteURLforAPI) + _supplierConfig.Omnibees.OmnibessHotelAvai;

                                        xhotel.UserId = Convert.ToString(OmnibeesDetails[0].UserID);
                                        xhotel.UserName = Convert.ToString(OmnibeesDetails[0].UserName);
                                        xhotel.PassWord = Convert.ToString(OmnibeesDetails[0].Password);
                                        xhotel.Version = _supplierConfig.Omnibees.Version;
                                        xhotel.CurrencyCode = CurrencyCode;
                                        xhotel.CurrencyFactor = CurrencyFactorOmnibees;
                                        xhotel.XmlCity = XMLCity;
                                        xhotel.XmlCountry = Xmlnationality;
                                        xhotel.XmlNationality = Xmlnationality;
                                        xhotel.AgentWiseMarkup = OmnibeesDetails[0].AgentWiseMarkup;
                                        xhotel.IsShowSupplier = Convert.ToInt16(OmnibeesDetails[0].IsShowSupplier) == 1 ? true : false;
                                        xhotel.SupplierID = OmnibeesDetails[0].SupplierID;
                                        xhotel.MasterSupplierID = OmnibeesDetails[0].MasterSupplierId;

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
                                            objrq.Request = XMLOmnibeesRequestOnly_async(xhotel.URL, xhotel.UserId, xhotel.UserName, xhotel.PassWord, xhotel.SearchTimeOut, SearchPara, IdsForSearch, DebugMode, XMLCity, Xmlnationality, xhotel.Version);
                                            objrq.Response = ""; RQRS.Add(objrq);
                                        }
                                        xhotel.RQRS = RQRS;
                                        xhotel.ReturnRooms = Convert.ToInt16(OmnibeesDetails[0].ReturnRooms);
                                        xhotel.TechName = Convert.ToString(OmnibeesDetails[0].TechName);
                                        #endregion


                                        AllTask.Add(xhotel);
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
        public void GetOmnibeesRequestOnly_async_new(DataRow[] commonCode, List<StarRating> listRating, HotelSearchParameter SearchPara, List<SupplierDetails> lstOmnibeesDetails, List<SupplierSearchConfig> lstOmnibeesSearchConfig, DataTable dtPreferredDataHotel, ref ConcurrentBag<HotelX> AllTask, string XMLCity, string Xmlnationality, int Hit, DataSet dtXMLCity, DataTable dtROE, TokenData tokenData)
        {
            try
            {

                List<StarRating> lstRating = new List<StarRating>();
                lstRating = _commonMethod.GetStarRatingStringList_xtg(SearchPara);

                List<HotelCodeByCity> DtHotelDAta = HtmlExtensions.ToList<HotelCodeByCity>(commonCode.CopyToDataTable());
                int ratingcount = lstRating.Count;
                int hotelcount = DtHotelDAta.Count;
                foreach (SupplierDetails OmnibeesChild in lstOmnibeesDetails)
                {
                    try
                    {
                        int SupplierID = OmnibeesChild.SupplierID;
                        string Supplier = OmnibeesChild.SupplierName;

                            if (lstOmnibeesSearchConfig.Count > 0)
                            {
                                List<SupplierSearchConfig> OmnibeesSearchConfig = lstOmnibeesSearchConfig
                                       .Where(x => x != null && x.SupplierID == SupplierID)
                                       .ToList();
                                if (OmnibeesSearchConfig != null && OmnibeesSearchConfig.Count > 0)
                                {
                                    if ((SearchPara.IsSearchFor.ToLower().IndexOf(Supplier.ToLower()) >= 0 || SearchPara.IsSearchFor.ToLower().IndexOf("multi") >= 0))
                                    {
                                        DataForSearch dataForSearch = _commonMethod.GetXmlDataForSearchTechwise(SearchPara.Nationality, dtXMLCity, Supplier, OmnibeesChild.TechName);

                                        if (dataForSearch.XmlNationality != "" && SearchPara.PastDate == false)
                                        {
                                            string Ids = string.Empty;
                                            string xmlbase = "";
                                            _commonMethod.GetPrefferedHotelCodes(ref Ids, ref xmlbase, ref OmnibeesSearchConfig, Supplier, dtPreferredDataHotel, SearchPara.IsSearchFor);

                                            List<HotelCodeByCity> filter = new List<HotelCodeByCity>();
                                            filter = _commonMethod.FilterWithOutCity_Code(ratingcount, hotelcount, DtHotelDAta, lstRating, OmnibeesChild.HotelSupplier);


                                            string CurrencyCode = Convert.ToString(OmnibeesChild.CurrencyShortName);
                                            string Sitecurrency = tokenData.SiteCurrency;

                                            double CurrencyFactorOmnibees = 1;

                                            if (CurrencyCode.ToLower() != Sitecurrency.ToLower()) // change systembase currency 
                                            {
                                                CurrencyFactorOmnibees = _commonMethod.GetROEByCurrency(CurrencyCode, dtROE);
                                            }

                                            if (OmnibeesSearchConfig != null)
                                            {

                                                for (int k = 0; k < OmnibeesSearchConfig.Count; k++)
                                                {
                                                    bool DebugMode = (Convert.ToInt32(OmnibeesSearchConfig[k].DebugMode) == 0 ? false : true);
                                                    string UserName = Convert.ToString(OmnibeesChild.UserName);
                                                    string Password = Convert.ToString(OmnibeesChild.Password);


                                                    {
                                                        int IsCitywisesearch = Convert.ToInt32(OmnibeesSearchConfig[k].City);

                                                        if (Convert.ToInt32(OmnibeesSearchConfig[k].CityID) == 0 || Convert.ToInt32(OmnibeesSearchConfig[k].CityID) == SearchPara.CityID)
                                                        {
                                                            if (filter.Count > 0)
                                                            {
                                                                int NoOfRecord = Convert.ToInt32(OmnibeesSearchConfig[k].Noofrecords);
                                                                double count = Convert.ToDouble(filter.Count);
                                                                int NoOfDiv = Convert.ToInt32(Math.Ceiling(count / Convert.ToDouble(NoOfRecord)));
                                                                List<List<XMLID_Search>> lstxml = _commonMethod.GetXMLIDString(NoOfDiv, NoOfRecord, count, filter);

                                                                for (int a = 0; a < NoOfDiv; a++)
                                                                {
                                                                    string Uniqueid = Guid.NewGuid().ToString().Substring(0, 8);
                                                                    //var paramValues1 = String.Join(",", splittedtables[a].Select().Select(x => String.Join(",", Convert.ToString(x.ItemArray[0]))));
                                                                    //string IdsForSearch = Convert.ToString(paramValues1);
                                                                    string IdsForSearch = Convert.ToString(_commonMethod.GetHotelCodeByCOmma(lstxml[a]));

                                                                    HotelX Xhotel = new HotelX();
                                                                    Xhotel.Supplier = Supplier;
                                                                    Xhotel.DebugeMode = DebugMode;
                                                                    Xhotel.SearchTimeOut = Convert.ToInt32(OmnibeesSearchConfig[k].HotelSearchTimeOut);
                                                                    //if (Hit > 0)
                                                                    Xhotel.ProcessTimeout = Convert.ToInt32(OmnibeesSearchConfig[k].HotelSearchTimeOut2);

                                                                    if (_keySettings.SupplierProxy == "0")
                                                                        Xhotel.URL = Convert.ToString(OmnibeesChild.WebSiteURL) + _supplierConfig.Omnibees.OmnibessHotelAvai;
                                                                    else
                                                                        Xhotel.URL = Convert.ToString(OmnibeesChild.WebSiteURLforAPI) + _supplierConfig.Omnibees.OmnibessHotelAvai;

                                                                    Xhotel.UserId = Convert.ToString(OmnibeesChild.UserID);
                                                                    Xhotel.UserName = UserName;
                                                                    Xhotel.PassWord = Password;
                                                                    Xhotel.CurrencyCode = CurrencyCode;
                                                                    Xhotel.Version = _supplierConfig.Omnibees.Version;
                                                                    Xhotel.CurrencyFactor = CurrencyFactorOmnibees;
                                                                    Xhotel.XmlCity = dataForSearch.xmlCity;
                                                                    Xhotel.XmlCountry = "";
                                                                    Xhotel.XmlNationality = dataForSearch.XmlNationality;
                                                                    Xhotel.Action = Uniqueid;


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
                                                                        objrq.Request = XMLOmnibeesRequestOnly_async(Xhotel.URL, Xhotel.UserId, Xhotel.UserName, Xhotel.PassWord, Xhotel.SearchTimeOut, SearchPara, IdsForSearch, DebugMode, XMLCity, Xmlnationality, Xhotel.Version);
                                                                        objrq.Response = "";
                                                                        RQRS.Add(objrq);
                                                                    }
                                                                    Xhotel.RQRS = RQRS;
                                                                    Xhotel.ReturnRooms = OmnibeesChild.ReturnRooms; //190225
                                                                    Xhotel.TechName = OmnibeesChild.TechName; //190225
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


        public string XMLOmnibeesRequestOnly_async(string JCUrl, string UserId, string JCUserName, string JCPassword, Int32 SearchTimeOut, HotelSearchParameter SearchParam, string JCHotelIds, bool DebugMode, string CityCode, string XmlBaseNationality, string Version)
        {
            try
            {
                #region PriceOnly RQ
                string JacPriceOnly = Get_Omnibees_PriceOnly(SearchParam, UserId, JCUserName, JCPassword, CityCode, JCHotelIds, XmlBaseNationality, Version);
                return JacPriceOnly;
                #endregion
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
                return "";
            }
        }

        public string Get_Omnibees_PriceOnly(HotelSearchParameter SearchParam, string UserId, string UserName, string Password, string RegionID, string HotelIds, string XmlBaseNationality, string Version)
        {
            string jsonString = "";
            try
            {
                string CheckIn = SearchParam.CheckInDate;
                CheckIn = CheckIn.Substring(6, 4) + "-" + CheckIn.Substring(3, 2) + "-" + CheckIn.Substring(0, 2);

                string CheckOut = SearchParam.CheckOutDate;
                CheckOut = CheckOut.Substring(6, 4) + "-" + CheckOut.Substring(3, 2) + "-" + CheckOut.Substring(0, 2);

                Ominibees.HotelSearchRQ Request = new Ominibees.HotelSearchRQ();
                Ominibees.HotelSearchCriteria HotelSearchCriteria = new Ominibees.HotelSearchCriteria();
                Ominibees.Criterion Criterion = new Ominibees.Criterion();
                Ominibees.Address Address = new Ominibees.Address();

                List<Ominibees.HotelRef> HotelRef = new List<Ominibees.HotelRef>();

                List<Ominibees.RoomStayCandidate> RoomStayCandidate = new List<Ominibees.RoomStayCandidate>();
                Ominibees.RoomStayCandidatesType RoomStayCandidatesType = new Ominibees.RoomStayCandidatesType();

                Ominibees.StayDateRange StayDateRange = new Ominibees.StayDateRange();
                Ominibees.MultimediaObjects MultimediaObjects = new Ominibees.MultimediaObjects();
                Ominibees.TPAExtensions TPAExtensions = new Ominibees.TPAExtensions();

                Request.AvailRatesOnly = true;
                Request.BestOnly = false;
                Request.EchoToken = "technopull_" + Guid.NewGuid().ToString();

                //Request.HotelSearchCriteria.Criterion.Address.CountryCode = 40;
                //Request.HotelSearchCriteria.AvailableOnlyIndicator = null;

                if (HotelIds != null)
                {
                    string[] hotelid = Convert.ToString(HotelIds).Split(',');
                    for (int i = 0; i < hotelid.Length; i++)
                    {
                        Ominibees.HotelRef HotelId = new Ominibees.HotelRef();
                        HotelId.HotelCode = Convert.ToInt32(hotelid[i]);
                        HotelRef.Add(HotelId);
                    }
                }
                else
                {
                    //Address.CityCode = 4689388;
                    //Address.CountryCode = 40;
                    //Criterion.Address = Address;
                }
                Criterion.HotelRefs = HotelRef;
                //Request.HotelSearchCriteria.Criterion.Address.CountryCode = 40;
                //Request.HotelSearchCriteria.Criterion.RoomStayCandidatesType.RoomStayCandidates[0].GuestCountsType.GuestCounts[0].AgeQualifyCode = 10;
                //Request.HotelSearchCriteria.Criterion.RoomStayCandidatesType.RoomStayCandidates[0].GuestCountsType.GuestCounts[0].Count = 2;
                for (int x = 0; x < SearchParam.Rooms.Count; x++)
                {
                    Ominibees.GuestCount GuestCount_obj = new Ominibees.GuestCount();
                    Ominibees.RoomStayCandidate RoomStayCandidate_obj = new Ominibees.RoomStayCandidate();
                    Ominibees.GuestCountsType GuestCountsType = new Ominibees.GuestCountsType();
                    List<Ominibees.GuestCount> GuestCount = new List<Ominibees.GuestCount>();
                    GuestCount_obj.AgeQualifyCode = 10;
                    GuestCount_obj.Count = SearchParam.Rooms[x].NoofAdults;
                    GuestCount.Add(GuestCount_obj);
                    if (SearchParam.Rooms[x].NoofChild > 0)
                    {
                        if (SearchParam.Rooms[x].NoofChild == 1)
                        {
                            Ominibees.GuestCount Child1GuestCount_obj = new Ominibees.GuestCount();
                            Child1GuestCount_obj.Age = SearchParam.Rooms[x].Child1Age;
                            Child1GuestCount_obj.AgeQualifyCode = 8;
                            Child1GuestCount_obj.Count = 1;
                            GuestCount.Add(Child1GuestCount_obj);
                        }
                        else
                        {
                            Ominibees.GuestCount Child1GuestCount_obj = new Ominibees.GuestCount();
                            Child1GuestCount_obj.Age = SearchParam.Rooms[x].Child1Age;
                            Child1GuestCount_obj.AgeQualifyCode = 8;
                            Child1GuestCount_obj.Count = 1;
                            GuestCount.Add(Child1GuestCount_obj);
                            Ominibees.GuestCount Child2GuestCount_obj = new Ominibees.GuestCount();
                            Child2GuestCount_obj.Age = SearchParam.Rooms[x].Child2Age;
                            Child2GuestCount_obj.AgeQualifyCode = 8;
                            Child2GuestCount_obj.Count = 1;
                            GuestCount.Add(Child2GuestCount_obj);
                        }
                    }
                    GuestCountsType.GuestCounts = GuestCount;
                    //RoomStayCandidate.GuestCountsType = GuestCount;
                    //Request.HotelSearchCriteria.Criterion.RoomStayCandidatesType.RoomStayCandidates[0].Quantity = 1;
                    RoomStayCandidate_obj.Quantity = 1;
                    RoomStayCandidate_obj.RPH = x;
                    RoomStayCandidate_obj.GuestCountsType = GuestCountsType;
                    RoomStayCandidate.Add(RoomStayCandidate_obj);
                }
                //Request.HotelSearchCriteria.Criterion.StayDateRange.End = Convert.ToDateTime("2021-03-24T00:00:00");
                //Request.HotelSearchCriteria.Criterion.StayDateRange.Start = Convert.ToDateTime("2021-03-23T00:00:00");
                //Criterion.RatePlanCandidatesType = RoomStayCandidatesType;
                RoomStayCandidatesType.RoomStayCandidates = RoomStayCandidate;
                Criterion.RoomStayCandidatesType = RoomStayCandidatesType;

                //StayDateRange.End = Convert.ToDateTime("2021-03-24T00:00:00");
                //StayDateRange.Start = Convert.ToDateTime("2021-03-23T00:00:00");

                StayDateRange.End = Convert.ToDateTime(CheckOut);
                StayDateRange.Start = Convert.ToDateTime(CheckIn);

                Criterion.StayDateRange = StayDateRange;

                //Request.HotelSearchCriteria.Criterion.TPA_Extensions.IsForMobile = false;
                //Request.HotelSearchCriteria.Criterion.TPA_Extensions.MultimediaObjects.SendData = false;

                TPAExtensions.IsForMobile = false;
                MultimediaObjects.SendData = false;

                TPAExtensions.MultimediaObjects = MultimediaObjects;
                Criterion.TPA_Extensions = TPAExtensions;
                HotelSearchCriteria.Criterion = Criterion;
                Request.HotelSearchCriteria = HotelSearchCriteria;
                Request.OnRequestInd = false;
                Request.PrimaryLangID = 1;
                //Request.RequestedCurrency = Convert.ToInt32(ConfigurationManager.AppSettings["RequestedCurrency"]);
                Request.Target = Convert.ToInt32(_supplierConfig.Omnibees.Target);
                //Request.RequestedCurrency = 34;
                string timeStamp = GetTimestamp(DateTime.Now);

                Request.TimeStamp = Convert.ToDateTime(DateTime.UtcNow); //Convert.ToDateTime("2021-03-19T06:00:37.2687519+00:00");
                Request.Version = Convert.ToDouble(Version);

                jsonString = JsonSerializer.Serialize(Request);
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
                return "";
            }
            return jsonString;
        }

        public static String GetTimestamp(DateTime value)
        {
            return value.ToString("yyyyMMddhhmmss");
        }

        public async Task<List<Options>> XMLOmnibeesPriceOnly_async(HotelX Item, CommonSearch commonSearch, double AgentPercentage, int SearchTimeOut, string Supplier, DataTable dtCountryCityMarkup, int AgentId, DataSet dsAgentDetails, int SupplierID, string ResponseData, HotelSearchParameter SearchParam, string xmlcity, string xmlcountry, string xmlnationality, string OmnibeesCurrency, double CurrencyFactorOmnibees, Boolean IsShowSupplier, DataTable DtROE, Int32 processTimeout, string ResProccessTimerLogActive, string ResProccessTimeValueforSaveLog)
        {
            System.Diagnostics.Stopwatch timerroomx = new System.Diagnostics.Stopwatch();
            timerroomx.Start();

            string reqid = Convert.ToString(DateTime.Now.Ticks);
            List<Options> options = new List<Options>();
            System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
            timer.Start();
            //if (OmnibeesCurrency.ToLower() == "eur")

            //{
            //    CurrencyFactorOmnibees = 4.3328017077;
            //}

            DataTable DtHotel = _commonMethod.GenerateDataTableNewV2();
            // DataTable DtHotel = Common.GenerateDataTableNewV2();
            try
            {
                if (ResponseData == null || ResponseData == "" || ResponseData == string.Empty)
                {
                    Item.ResponseStatus = 204;
                    Item.ResponseType = "NoCount";
                    return options;//Common.GenerateDataTableNewV2();
                }

                HotelSearchResponse.HotelSearchRS OmnibeesResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<HotelSearchResponse.HotelSearchRS>(ResponseData);

                if (OmnibeesResponse == null || OmnibeesResponse.HotelStaysType == null || OmnibeesResponse.HotelStaysType.HotelStays == null)
                {
                    Item.ResponseStatus = 404;
                    Item.ResponseType = "NoCount";
                    return options;
                }

                for (int h = 0; h <= OmnibeesResponse.HotelStaysType.HotelStays.Count - 1; h++)
                {
                    Item.ResponseType = "Success";
                    #region "No Of Hotels "
                    int XmlID = OmnibeesResponse.HotelStaysType.HotelStays[h].BasicPropertyInfo.HotelRef.HotelCode;

                    double lowprice = 0;

                    for (int ro = 0; ro < OmnibeesResponse.RoomStaysType.RoomStays[h].RoomRates.Count; ro++)
                    {


                        int RoomId = OmnibeesResponse.RoomStaysType.RoomStays[h].RoomRates[ro].RoomID;
                        int RatePlanId = OmnibeesResponse.RoomStaysType.RoomStays[h].RoomRates[ro].RatePlanID;

                        //List<Common.HotelSearchResponse.RoomType> RoomFilter = OmnibeesResponse.RoomStaysType.RoomStays[0].RoomTypes.Where(item => item.RoomID == RoomId).ToList();
                        List<HotelSearchResponse.RatePlan> RatePlanFilter = OmnibeesResponse.RoomStaysType.RoomStays[h].RatePlans.Where(item => item.RatePlanID == RatePlanId).ToList();

                        bool isshow = false;
                        for (int v = 0; v < RatePlanFilter[0].PaymentPolicies.AcceptedPayments.Count; v++)
                        {
                            //if (RatePlanFilter[0].PaymentPolicies.AcceptedPayments[v].GuaranteeTypeCode == 5)
                            if (RatePlanFilter[0].PaymentPolicies.AcceptedPayments[v].GuaranteeTypeCode == 3 || RatePlanFilter[0].PaymentPolicies.AcceptedPayments[v].GuaranteeTypeCode == 4)

                            {
                                isshow = true;
                                break;
                            }
                        }
                        if (isshow == false)
                        {
                            continue;
                        }

                        int CurrencyCode = OmnibeesResponse.RoomStaysType.RoomStays[h].RoomRates[ro].Total.CurrencyCode;
                        OmnibeesCurrency = ReturnOmnibeesCurrency(CurrencyCode);

                        if (OmnibeesCurrency.ToLower() != commonSearch.tokenData.SiteCurrency.ToLower())// || OmnibeesCurrency.ToLower() == "brl")
                        {
                            //CurrencyFactorOmnibees = CurrencyFactorOmnibees;
                            try
                            {
                                CurrencyFactorOmnibees = Convert.ToDouble(_commonMethod.GetROEByCurrency(OmnibeesCurrency, DtROE));
                                //DataTable dtInrCurrency = Common.GetCountryCurrency(OmnibeesCurrency);
                                //CurrencyFactorOmnibees = Convert.ToDouble(dtInrCurrency.Rows[0]["AmtToExch"]);
                            }
                            catch (Exception)
                            {
                                continue;
                            }
                        }
                        //else
                        //{
                        //    continue;
                        //}
                        double Price = OmnibeesResponse.RoomStaysType.RoomStays[h].RoomRates[ro].Total.AmountAfterTax;
                        Price = Price * CurrencyFactorOmnibees;
                        if (lowprice == 0)
                            lowprice = Price;
                        else
                        {
                            if (Price < lowprice)
                                lowprice = Price;
                        }
                    }
                    #region "When not Need Room" 
                    if (SearchParam.Filters.IsShowRooms == 0)
                    {
                        DataRow drXmlHotel = DtHotel.NewRow();

                        drXmlHotel["ContractID"] = 0;
                        drXmlHotel["SupplierId"] = SupplierID;
                        // drXmlHotel["AgentPercentage"] = AgentPercentage;
                        drXmlHotel["HotelName"] = "";
                        drXmlHotel["IsShowSupplier"] = IsShowSupplier;
                        drXmlHotel["UniqueNo"] = OmnibeesResponse.HotelStaysType.HotelStays[h].BasicPropertyInfo.HotelRef.HotelCode;
                        drXmlHotel["Rating"] = 0;
                        //int OwnsystemHotelId = 0;
                        drXmlHotel["Favorite"] = 0;
                        drXmlHotel["VatdiffAmount"] = 0;
                        drXmlHotel["XMLHotelCode"] = OmnibeesResponse.HotelStaysType.HotelStays[h].BasicPropertyInfo.HotelRef.HotelCode;
                        drXmlHotel["SupplierName"] = "Omnibees";
                        drXmlHotel["SupplierCurrency"] = OmnibeesCurrency;
                        //double FinalMinPrice = OmnibeesResponse.HotelStaysType.HotelStays[h].Price.AmountBeforeTax * CurrencyFactorOmnibees;
                        if (lowprice == 0)
                            continue;
                        Int64 GiataId = 0;
                        int Rating = 0;
                        //lowprice = Common.setHotelListMarkup(SearchParam, dtCountryCityMarkup, Convert.ToInt32(dsAgentDetails.Tables[0].Rows[0]["IsParentId"]), Convert.ToDouble(dsAgentDetails.Tables[0].Rows[0]["B2CMarkupPer"]), AgentId, commonSearch.TariffID, AgentPercentage, lowprice, GiataId, SupplierID, Rating);
                        drXmlHotel["FinalSellingPrice"] = lowprice;
                        drXmlHotel["SellingPrice"] = lowprice;
                        DtHotel.Rows.Add(drXmlHotel);

                        if (lowprice > 0)
                        {
                            #region "Room code Block"
                            bool willAddOrNot = true;
                            Int64 RaynaID = 0;
                            Int64 OwnsystemHotelId = 0;
                            double MinPrice = lowprice;
                            _markup.GetHotelDataCommon(ref MinPrice, ref willAddOrNot, ref OwnsystemHotelId, ref RaynaID, ref GiataId, ref Rating, ref drXmlHotel, commonSearch.AgentId, commonSearch.TariffID, commonSearch.dsAgentDetails, commonSearch.supplierGiataMaster, SearchParam, commonSearch.dtCountryCityMarkup, commonSearch.B2CMarkup, commonSearch.IsParentId, commonSearch.SubAgentMarkup, commonSearch.SearchByHotel, commonSearch.HotelIDS);
                            if (willAddOrNot)
                            {

                                #region "Room Logic"
                                DataTable dt_HotelRoom = _commonMethod.GenerateRoomtypeDataTableNewV2();
                                DataTable dt_Cancellation = _commonMethod.GenerateCancellationDataTableNewV2();
                                DataTable dt_RateBreackUp = _commonMethod.GenerateoomRateBreackUpDataTableNewV2();
                                #endregion
                                decimal LowestPrice = Convert.ToDecimal(MinPrice).CalculatePrice(commonSearch.tokenData, commonSearch.advancedOptions, OmnibeesCurrency);
                                Options ops = (await _supplierRes.GetCommonRoomForSuppliers(GiataId, commonSearch.tokenData, commonSearch.advancedOptions, Convert.ToDouble(LowestPrice), drXmlHotel, dt_HotelRoom, dt_Cancellation, dt_RateBreackUp, SearchParam));
                                if (ops != null)
                                    options.Add(ops);
                            }
                            #endregion
                        }
                    }
                    #endregion
                    #region "When Need Rooms"
                    else
                    {
                        DataRow drXmlHotel = DtHotel.NewRow();

                        drXmlHotel["ContractID"] = 0;
                        drXmlHotel["SupplierId"] = SupplierID;
                        // drXmlHotel["AgentPercentage"] = AgentPercentage;
                        drXmlHotel["HotelName"] = "";
                        drXmlHotel["IsShowSupplier"] = IsShowSupplier;
                        drXmlHotel["UniqueNo"] = OmnibeesResponse.HotelStaysType.HotelStays[h].BasicPropertyInfo.HotelRef.HotelCode;
                        drXmlHotel["Rating"] = 0;
                        //int OwnsystemHotelId = 0;
                        drXmlHotel["Favorite"] = 0;
                        drXmlHotel["VatdiffAmount"] = 0;
                        drXmlHotel["XMLHotelCode"] = OmnibeesResponse.HotelStaysType.HotelStays[h].BasicPropertyInfo.HotelRef.HotelCode;
                        drXmlHotel["SupplierName"] = "Omnibees";
                        drXmlHotel["SupplierCurrency"] = OmnibeesCurrency;
                        //double FinalMinPrice = OmnibeesResponse.HotelStaysType.HotelStays[h].Price.AmountBeforeTax * CurrencyFactorOmnibees;
                        //double lowprice = 0;
                        if (lowprice == 0)
                            continue;
                        Int64 GiataId = 0;
                        int Rating = 0;
                        //lowprice = Common.setHotelListMarkup(SearchParam, dtCountryCityMarkup, Convert.ToInt32(dsAgentDetails.Tables[0].Rows[0]["IsParentId"]), Convert.ToDouble(dsAgentDetails.Tables[0].Rows[0]["B2CMarkupPer"]), AgentId, commonSearch.TariffID, AgentPercentage, lowprice, GiataId, SupplierID, Rating);
                        drXmlHotel["FinalSellingPrice"] = lowprice;
                        drXmlHotel["SellingPrice"] = lowprice;
                        DtHotel.Rows.Add(drXmlHotel);

                        if (lowprice > 0)
                        {
                            #region "Room code Block"
                            bool willAddOrNot = true;
                            Int64 RaynaID = 0;
                            Int64 OwnsystemHotelId = 0;
                            int RatingHotel = 0;
                            double MinPrice = lowprice;
                            int CutOffDays = -3;
                            double SubAgentMarkup = 0;
                            Boolean blnIsShowSupplier = false;
                            _markup.GetHotelDataCommon(ref MinPrice, ref willAddOrNot, ref OwnsystemHotelId, ref RaynaID, ref GiataId, ref Rating, ref drXmlHotel, commonSearch.AgentId, commonSearch.TariffID, commonSearch.dsAgentDetails, commonSearch.supplierGiataMaster, SearchParam, commonSearch.dtCountryCityMarkup, commonSearch.B2CMarkup, commonSearch.IsParentId, commonSearch.SubAgentMarkup, commonSearch.SearchByHotel, commonSearch.HotelIDS);
                            if (willAddOrNot)
                            {
                                DataTable DtOmnibeesHotelRoom = _commonMethod.GenerateRoomtypeDataTableNewV2();
                                DataTable dtOmnibeesCancellation = _commonMethod.GenerateCancellationDataTableNewV2();
                                DataTable DtOmnibeesHotelRoomRateBreackUp = _commonMethod.GenerateoomRateBreackUpDataTableNewV2();
                                //DataTable dtHotelBedsV1Room = Common.GenerateRoomtypeDataTableNewV2();
                                //DataTable dtHotelBedsV1RoomCacelationPolicy = Common.GenerateCancellationDataTableNewV2();
                                //DataTable DtRoomRateBreackUp = Common.GenerateoomRateBreackUpDataTableNewV2();
                                DataSet dsTemp = new DataSet();
                                {
                                    #region "No Of Hotels "
                                    //string ItemCode = Convert.ToString(drhotel.productCode);
                                    double AgentMarkupAmount = 0.0;
                                    Boolean isPerce = true;
                                    _markup.getAgetMarkupSetting("Omnibees", GiataId.ToString(), commonSearch.dtSuplierMarkup, commonSearch.dtMarkupDetails, ref AgentPercentage, ref AgentMarkupAmount, ref blnIsShowSupplier, ref isPerce, commonSearch.AgentId, RatingHotel, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), Convert.ToString(SearchParam.CheckInDate));
                                    //string InternalHotelId = Convert.ToString(h);
                                    #region NoOfRooms
                                    string EssentialInfo = "";
                                    int NumberOfUnits = 0;// Added on 23-AUG-2024
                                    for (int ro = 0; ro < OmnibeesResponse.RoomStaysType.RoomStays[h].RoomRates.Count; ro++)
                                    {
                                        NumberOfUnits = OmnibeesResponse.RoomStaysType.RoomStays[h].RoomRates[ro].RatesType.Rates[0].NumberOfUnits;
                                        if (NumberOfUnits >= SearchParam.Rooms.Count)// Added on 23-AUG-2024
                                        {
                                            int RoomId = OmnibeesResponse.RoomStaysType.RoomStays[h].RoomRates[ro].RoomID;
                                            int RatePlanId = OmnibeesResponse.RoomStaysType.RoomStays[h].RoomRates[ro].RatePlanID;
                                            EssentialInfo = "RoomId : " + RoomId + ", RatePlanId :" + RatePlanId;
                                            List<HotelSearchResponse.RoomType> RoomFilter = OmnibeesResponse.RoomStaysType.RoomStays[h].RoomTypes.Where(item => item.RoomID == RoomId).ToList();
                                            List<HotelSearchResponse.RatePlan> RatePlanFilter = OmnibeesResponse.RoomStaysType.RoomStays[h].RatePlans.Where(item => item.RatePlanID == RatePlanId).ToList();

                                            bool isshow = false;
                                            for (int v = 0; v < RatePlanFilter[0].PaymentPolicies.AcceptedPayments.Count; v++)
                                            {
                                                //if (RatePlanFilter[0].PaymentPolicies.AcceptedPayments[v].GuaranteeTypeCode == 5)
                                                if (RatePlanFilter[0].PaymentPolicies.AcceptedPayments[v].GuaranteeTypeCode == 3 || RatePlanFilter[0].PaymentPolicies.AcceptedPayments[v].GuaranteeTypeCode == 4)
                                                {
                                                    isshow = true;
                                                    break;
                                                }
                                            }
                                            if (isshow == false)
                                            {
                                                continue;
                                            }
                                            string RoomDeatils = "";
                                            if (RoomFilter[0].RoomDescription.Description != null)
                                            {
                                                RoomDeatils = RoomFilter[0].RoomDescription.Description;
                                            }
                                            string RateInformation = "";

                                            if (RatePlanFilter[0].AdditionalDetailsType != null)
                                            {
                                                if (RatePlanFilter[0].AdditionalDetailsType.AdditionalDetails != null)
                                                {
                                                    for (int i = 0; i < RatePlanFilter[0].AdditionalDetailsType.AdditionalDetails.Count; i++)
                                                    {
                                                        string RatDetailsname = RatePlanFilter[0].AdditionalDetailsType.AdditionalDetails[i].DetailDescription.Name;
                                                        string RatDescription = RatePlanFilter[0].AdditionalDetailsType.AdditionalDetails[i].DetailDescription.Description;
                                                        RateInformation = RateInformation + "<br>" + RatDetailsname + ":" + RatDescription;
                                                    }
                                                }
                                            }
                                            EssentialInfo = EssentialInfo + "<br>" + RoomDeatils + "<br>" + RateInformation;
                                            string RoomName = RoomFilter[0].RoomName;
                                            string RatePlan = RatePlanFilter[0].RatePlanName;
                                            int RoomStayCandidateRPH = OmnibeesResponse.RoomStaysType.RoomStays[h].RoomRates[ro].RoomStayCandidateRPH;
                                            int RoomNo = OmnibeesResponse.RoomStaysType.RoomStays[h].RoomRates[ro].RoomStayCandidateRPH + 1;
                                            double AmountAfterTax = OmnibeesResponse.RoomStaysType.RoomStays[h].RoomRates[ro].Total.AmountAfterTax;
                                            double AmountBeforeTax = OmnibeesResponse.RoomStaysType.RoomStays[h].RoomRates[ro].Total.AmountBeforeTax;
                                            int CurrencyCode = OmnibeesResponse.RoomStaysType.RoomStays[h].RoomRates[ro].Total.CurrencyCode;
                                            double Price = OmnibeesResponse.RoomStaysType.RoomStays[h].RoomRates[ro].Total.AmountAfterTax;
                                            bool AmountIncludingMarkup = OmnibeesResponse.RoomStaysType.RoomStays[h].RoomRates[ro].Total.AmountIncludingMarkup;
                                            bool AmountIsPackage = OmnibeesResponse.RoomStaysType.RoomStays[h].RoomRates[ro].Total.AmountIsPackage;
                                            CurrencyFactorOmnibees = 1;
                                            //if (CurrencyCode != 109)
                                            //{
                                            //    continue;
                                            //}
                                            OmnibeesCurrency = ReturnOmnibeesCurrency(CurrencyCode);
                                            if (OmnibeesCurrency == "")
                                                continue;
                                            if (OmnibeesCurrency.ToLower() != commonSearch.tokenData.SiteCurrency.ToLower())
                                            {
                                                try
                                                {
                                                    CurrencyFactorOmnibees = Convert.ToDouble(_commonMethod.GetROEByCurrency(OmnibeesCurrency, DtROE));
                                                    //DataTable dtInrCurrency = Common.GetCountryCurrency(OmnibeesCurrency);
                                                    //CurrencyFactorOmnibees = Convert.ToDouble(dtInrCurrency.Rows[0]["AmtToExch"]);
                                                }
                                                catch (Exception)
                                                {
                                                    continue;
                                                }
                                            }
                                            //if (OmnibeesCurrency.ToLower() == "usd" || OmnibeesCurrency.ToLower() == "brl")
                                            //{
                                            //    CurrencyFactorOmnibees = 1;
                                            //    try
                                            //    {
                                            //        DataTable dtInrCurrency = Common.GetCountryCurrency(OmnibeesCurrency);
                                            //        CurrencyFactorOmnibees = Convert.ToDouble(dtInrCurrency.Rows[0]["AmtToExch"]);
                                            //    }
                                            //    catch (Exception)
                                            //    {
                                            //        continue;
                                            //    }
                                            //}
                                            //else
                                            //{
                                            //    continue;
                                            //}
                                            //if (OmnibeesCurrency.ToLower() == "usd")
                                            //{
                                            //    CurrencyFactorOmnibees = 1;
                                            //}
                                            //else
                                            //{
                                            //    continue;
                                            //}
                                            if (AmountIsPackage)
                                            {
                                                EssentialInfo = EssentialInfo + "<br>" + ".";
                                            }
                                            if (RatePlanFilter[0].Commission != null)
                                            {
                                                EssentialInfo = EssentialInfo + "<br>" + "*";
                                            }

                                            Price = Price * CurrencyFactorOmnibees;

                                            string roomAvailability = OmnibeesResponse.RoomStaysType.RoomStays[h].RoomRates[ro].Availability[0].AvailabilityStatus;
                                            string mealName = "Room Only";
                                            if (RatePlanFilter[0].MealsIncluded != null)
                                            {
                                                mealName = RatePlanFilter[0].MealsIncluded.Name;
                                            }
                                            if (roomAvailability == "AvailableForSale")
                                                roomAvailability = "Available";
                                            else
                                                continue;

                                            string rateIdentifier = RoomId + "$$$" + RatePlanId + "$$$" + AmountAfterTax + "$$$" + AmountBeforeTax + "$$$" + CurrencyCode + "$$$" + CurrencyFactorOmnibees + "$$$" + AmountIncludingMarkup + "$$$" + AmountIsPackage + "$$$" + RoomStayCandidateRPH + "$$$" + Guid.NewGuid().ToString().Substring(0, 4);
                                            string uniqid = Guid.NewGuid().ToString();
                                            double OriginalPrice = Price;
                                            #region "Rate Break Up"
                                            for (int date = 0; date < SearchParam.NoofNights; date++)
                                            {
                                                DateTime dtCheckInDateForRate = DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null);
                                                string DailyStatus = roomAvailability;
                                                //int priceType = roomOptions.dates[date].dailyPrice.priceType;
                                                double PriceRoom = Price / SearchParam.NoofNights;
                                                #region Vat
                                                Double VatOnRateBrackUp = 0;
                                                double Pricenew_wl = 0;


                                                //PriceRoom = Common.ProcessHotelRateBreakupPrice_WithMarkup(1, SearchParam.CityID, SearchParam.CountryId, PriceRoom, B2CMarkup, IsParentId, isPerce, AgentPercentage, AgentMarkupAmount, SubAgentMarkup, ref VatOnRateBrackUp);
                                                PriceRoom = _markup.ProcessHotelRateBreakupPrice_WithMarkup(1, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), PriceRoom, commonSearch.B2CMarkup, commonSearch.IsParentId, isPerce, AgentPercentage, AgentMarkupAmount, SubAgentMarkup, ref VatOnRateBrackUp);
                                                #endregion

                                                //string rateWeekColumn = Convert.ToString(serviceDate.DayOfWeek).Substring(0, 3);
                                                string rateWeekColumn = Convert.ToString(dtCheckInDateForRate.Date.AddDays(date).DayOfWeek).Substring(0, 3);
                                                DataRow drHotelOmnibeesRateBreackUp = DtOmnibeesHotelRoomRateBreackUp.NewRow();
                                                drHotelOmnibeesRateBreackUp["SupplierName"] = "Omnibees";
                                                drHotelOmnibeesRateBreackUp["OwnsystemHotelId"] = OwnsystemHotelId;
                                                drHotelOmnibeesRateBreackUp["ContractID"] = "0";
                                                drHotelOmnibeesRateBreackUp["RoomTypeName"] = RoomName;
                                                drHotelOmnibeesRateBreackUp["RoomTypeId"] = rateIdentifier;
                                                drHotelOmnibeesRateBreackUp["MealId"] = mealName;
                                                drHotelOmnibeesRateBreackUp["Meal"] = mealName;
                                                drHotelOmnibeesRateBreackUp["DateOfWeek"] = rateWeekColumn;
                                                drHotelOmnibeesRateBreackUp["Date"] = dtCheckInDateForRate.Date.AddDays(date).Date;
                                                drHotelOmnibeesRateBreackUp["Rate"] = Convert.ToString((PriceRoom));
                                                //drHotelOmnibeesRateBreackUp["Rate"] = Pricenew_wl;
                                                drHotelOmnibeesRateBreackUp["VatdiffAmount"] = Convert.ToString((VatOnRateBrackUp));
                                                drHotelOmnibeesRateBreackUp["RateType"] = "A Rate";
                                                drHotelOmnibeesRateBreackUp["RoomNo"] = Convert.ToString(RoomNo);
                                                drHotelOmnibeesRateBreackUp["RoomSequenceNo"] = uniqid;
                                                drHotelOmnibeesRateBreackUp["BookingStatus"] = DailyStatus;
                                                drHotelOmnibeesRateBreackUp["SupplierCurrency"] = CurrencyFactorOmnibees;
                                                drHotelOmnibeesRateBreackUp["BuyingPrice"] = PriceRoom / CurrencyFactorOmnibees;
                                                //drHotelOmnibeesRateBreackUp["BuyingPrice_wl"] = PriceRoom / CurrencyFactorOmnibees;
                                                DtOmnibeesHotelRoomRateBreackUp.Rows.Add(drHotelOmnibeesRateBreackUp);
                                            }
                                            #endregion "Rate Break Up"
                                            DataRow drXmlHotelRoom = DtOmnibeesHotelRoom.NewRow();
                                            drXmlHotelRoom["OwnSystemHotelId"] = OwnsystemHotelId;
                                            drXmlHotelRoom["RoomTypeName"] = RoomName;
                                            drXmlHotelRoom["RoomTypeCode"] = rateIdentifier;
                                            drXmlHotelRoom["MealId"] = mealName;
                                            drXmlHotelRoom["MealName"] = mealName;
                                            drXmlHotelRoom["ContractID"] = 0;
                                            drXmlHotelRoom["BoardType"] = mealName;
                                            drXmlHotelRoom["RoomTypeId"] = rateIdentifier;
                                            drXmlHotelRoom["AdultsOccupancy"] = Convert.ToString(SearchParam.Rooms[RoomNo - 1].NoofAdults);
                                            drXmlHotelRoom["ChildOccupancy"] = Convert.ToString(SearchParam.Rooms[RoomNo - 1].NoofChild);
                                            drXmlHotelRoom["MaxExtraBed"] = "";
                                            drXmlHotelRoom["RoomNo"] = Convert.ToString(RoomNo);
                                            drXmlHotelRoom["RoomSequenceNo"] = uniqid;
                                            drXmlHotelRoom["Type"] = "";
                                            drXmlHotelRoom["BookingStatus"] = roomAvailability;
                                            drXmlHotelRoom["UniqueNo"] = Guid.NewGuid().ToString();
                                            string OfferDesc = "";
                                            drXmlHotelRoom["OfferDescription"] = OfferDesc;
                                            drXmlHotelRoom["OfferCode"] = OfferDesc;
                                            drXmlHotelRoom["EssentialInformation"] = EssentialInfo;
                                            double ItemPrice = Convert.ToDouble(Price);
                                            //Common.ProcessHotelRoom_WithMarkup(CurrencyFactorOmnibees, MikiCurrency, false, false, SearchParam.NoofNights, SearchParam.CityID, SearchParam.CountryId, SearchParam.CheckInDate, SearchParam.CheckOutDate, ref ItemPrice, ref drXmlHotelRoom, isPerce, AgentPercentage, AgentMarkupAmount, SubAgentMarkup, B2CMarkup, IsParentId);
                                            _markup.ProcessHotelRoom_WithMarkup(CurrencyFactorOmnibees, OmnibeesCurrency, false, false, SearchParam.NoofNights, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), SearchParam.CheckInDate, SearchParam.CheckOutDate, ref ItemPrice, ref drXmlHotelRoom, isPerce, AgentPercentage, AgentMarkupAmount, SubAgentMarkup, commonSearch.B2CMarkup, commonSearch.IsParentId);

                                            drXmlHotelRoom["SupplierName"] = "Omnibees";
                                            DtOmnibeesHotelRoom.Rows.Add(drXmlHotelRoom);
                                            DtOmnibeesHotelRoomRateBreackUp.DefaultView.RowFilter = "OwnsystemHotelId='" + OwnsystemHotelId + "'";
                                            //DtOmnibeesHotelRoomRateBreackUp.DefaultView.Sort = "Rate ASC";
                                            #region "Cancelation Policy"
                                            bool NonRefundable = RatePlanFilter[0].CancelPenalties[0].NonRefundable;
                                            DateTime curDate = DateTime.Now;
                                            double ItemPrice_NF = Price;
                                            double VatOnPolicy_NF = 0;
                                            double MainAgentCharge_wl = Price;
                                            double CancelCharge_wl = Price;



                                            if (NonRefundable == true)
                                            {
                                                //double zero = 0;
                                                //dtOmnibeesCancellation.Rows.Add(Common.setCancellationPolicyRow_wl(0, 0, ref zero, dtOmnibeesCancellation.NewRow(), CurrencyFactorOmnibees, MikiCurrency, "Omnibees", ownsystemhotelId, Convert.ToString(RoomNo),
                                                //                       Convert.ToString(rateIdentifier), RoomName, mealName, DateTime.Now.Date, DateTime.Now.Date.AddDays(30), 0, 0, "", 0, uniqid, "NO"));
                                                //dtOmnibeesCancellation.AcceptChanges();

                                                //dtOmnibeesCancellation.Rows.Add(Common.setCancellationPolicyRow_wl(MainAgentCharge_wl, CancelCharge_wl, ref Price, dtOmnibeesCancellation.NewRow(), CurrencyFactorOmnibees, MikiCurrency, "Omnibees", ownsystemhotelId, Convert.ToString(RoomNo),
                                                //                              Convert.ToString(rateIdentifier), RoomName, mealName, DateTime.Now.Date.AddDays(30),
                                                //                              DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null), ItemPrice, ItemPrice, "", 0, uniqid, "Yes"));
                                                //dtOmnibeesCancellation.AcceptChanges();

                                                dtOmnibeesCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref Price, dtOmnibeesCancellation.NewRow(), CurrencyFactorOmnibees, OmnibeesCurrency, "Omnibees", Convert.ToString(OwnsystemHotelId), Convert.ToString(RoomNo),
                                                                                        Convert.ToString(rateIdentifier), RoomName, mealName, curDate,
                                                                                        DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null), Price, ItemPrice, "", 0, uniqid, "Yes"));
                                                dtOmnibeesCancellation.AcceptChanges();
                                            }
                                            else
                                            {
                                                bool ISRef = true;
                                                int OffsetUnitMultiplier = 0;
                                                DateTime canceldate = DateTime.Now;
                                                DateTime checkindate = DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null);
                                                if (RatePlanFilter[0].CancelPenalties[0].DeadLine != null)
                                                {
                                                    OffsetUnitMultiplier = RatePlanFilter[0].CancelPenalties[0].DeadLine.OffsetUnitMultiplier;
                                                }
                                                else
                                                {
                                                    ISRef = false;
                                                }


                                                canceldate = checkindate.AddDays(-OffsetUnitMultiplier);
                                                canceldate = canceldate.AddDays(CutOffDays);

                                                //if (OffsetUnitMultiplier == 0)
                                                //{
                                                //    ISRef = false;
                                                //}
                                                //else
                                                //{
                                                //    canceldate = checkindate.AddDays(-OffsetUnitMultiplier);
                                                //    canceldate = canceldate.AddDays(CutOffDays);
                                                //}
                                                if (ISRef == false)
                                                {
                                                    //double zero = 0;
                                                    //dtOmnibeesCancellation.Rows.Add(Common.setCancellationPolicyRow(ref zero, dtOmnibeesCancellation.NewRow(), CurrencyFactorOmnibees, MikiCurrency, "Omnibees", ownsystemhotelId, Convert.ToString(RoomNo),
                                                    //                       Convert.ToString(rateIdentifier), RoomName, mealName, DateTime.Now.Date, DateTime.Now.Date.AddDays(30), 0, 0, "", 0, uniqid, "NO"));
                                                    //dtOmnibeesCancellation.AcceptChanges();
                                                    //dtOmnibeesCancellation.Rows.Add(Common.setCancellationPolicyRow(ref Price, dtOmnibeesCancellation.NewRow(), CurrencyFactorOmnibees, MikiCurrency, "Omnibees", ownsystemhotelId, Convert.ToString(RoomNo),
                                                    //                              Convert.ToString(rateIdentifier), RoomName, mealName, DateTime.Now.Date.AddDays(30),
                                                    //                              DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null), ItemPrice, ItemPrice, "", 0, uniqid, "Yes"));
                                                    //dtOmnibeesCancellation.AcceptChanges();

                                                    dtOmnibeesCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref Price, dtOmnibeesCancellation.NewRow(), CurrencyFactorOmnibees, OmnibeesCurrency, "Omnibees", Convert.ToString(OwnsystemHotelId), Convert.ToString(RoomNo),
                                                                                  Convert.ToString(rateIdentifier), RoomName, mealName, curDate,
                                                                                  DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null), Price, ItemPrice, "", 0, uniqid, "Yes"));
                                                    dtOmnibeesCancellation.AcceptChanges();
                                                }
                                                else
                                                {
                                                    if (canceldate.AddDays(-1) > curDate && ISRef == true)
                                                    {
                                                        double zero = 0;
                                                        dtOmnibeesCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref zero, dtOmnibeesCancellation.NewRow(), CurrencyFactorOmnibees, OmnibeesCurrency, "Omnibees", Convert.ToString(OwnsystemHotelId), Convert.ToString(RoomNo),
                                                                               Convert.ToString(rateIdentifier), RoomName, mealName, DateTime.Now.Date, canceldate.AddDays(-1).AddHours(23).AddMinutes(59).AddSeconds(59), 0, 0, "", 0, uniqid, "NO"));
                                                        dtOmnibeesCancellation.AcceptChanges();

                                                        dtOmnibeesCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref Price, dtOmnibeesCancellation.NewRow(), CurrencyFactorOmnibees, OmnibeesCurrency, "Omnibees", Convert.ToString(OwnsystemHotelId), Convert.ToString(RoomNo),
                                                                               Convert.ToString(rateIdentifier), RoomName, mealName, canceldate,
                                                                               DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null), Price, ItemPrice, "", 0, uniqid, "Yes"));
                                                        dtOmnibeesCancellation.AcceptChanges();

                                                    }
                                                    else
                                                    {
                                                        //double zero = 0;
                                                        //dtOmnibeesCancellation.Rows.Add(Common.setCancellationPolicyRow(ref zero, dtOmnibeesCancellation.NewRow(), CurrencyFactorOmnibees, MikiCurrency, "Omnibees", ownsystemhotelId, Convert.ToString(RoomNo),
                                                        //                       Convert.ToString(rateIdentifier), RoomName, mealName, DateTime.Now.Date, DateTime.Now.Date.AddDays(30), 0, 0, "", 0, uniqid, "NO"));
                                                        //dtOmnibeesCancellation.AcceptChanges();
                                                        //dtOmnibeesCancellation.Rows.Add(Common.setCancellationPolicyRow(ref Price, dtOmnibeesCancellation.NewRow(), CurrencyFactorOmnibees, MikiCurrency, "Omnibees", ownsystemhotelId, Convert.ToString(RoomNo),
                                                        //                              Convert.ToString(rateIdentifier), RoomName, mealName, DateTime.Now.Date.AddDays(30),
                                                        //                              DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null), ItemPrice, ItemPrice, "", 0, uniqid, "Yes"));
                                                        //dtOmnibeesCancellation.AcceptChanges();
                                                        dtOmnibeesCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref Price, dtOmnibeesCancellation.NewRow(), CurrencyFactorOmnibees, OmnibeesCurrency, "Omnibees", Convert.ToString(OwnsystemHotelId), Convert.ToString(RoomNo),
                                                                                 Convert.ToString(rateIdentifier), RoomName, mealName, curDate,
                                                                                 DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null), Price, ItemPrice, "", 0, uniqid, "Yes"));
                                                        dtOmnibeesCancellation.AcceptChanges();
                                                    }
                                                }
                                            }
                                            #endregion
                                        }
                                        else
                                        {
                                            continue;
                                        }
                                    }
                                    #endregion
                                    #endregion
                                }


                                Options ops = (await _supplierRes.GetCommonRoomForSuppliers(GiataId, commonSearch.tokenData, commonSearch.advancedOptions, MinPrice, drXmlHotel, DtOmnibeesHotelRoom, dtOmnibeesCancellation, DtOmnibeesHotelRoomRateBreackUp, SearchParam));
                                if (ops != null)
                                    options.Add(ops);
                            }
                        }
                        #endregion
                    }
                    #endregion

                    #endregion
                }
            }
            catch (Exception ex)
            {
                Item.ResponseStatus = 404;
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
                        
                        SearchParam.SupplierTime += ",|Finish:Omnibees Process Response  " + "|Time :" + timerroomx.Elapsed.ToString();
                        _ = Task.Run(() => _commonMethod.FireAway_SQL_ResPorcessTimeLog("", timerroomx.Elapsed.ToString(), "Omnibees", SearchParam.SearchKey, 1, AgentId));
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

        public string ReturnOmnibeesCurrency(int CurrencyCode)
        {
            string CurrencyName = "";
            switch (CurrencyCode)
            {
                case 109:
                    CurrencyName = "USD";
                    break;
                case 102:
                    CurrencyName = "THB";
                    break;
                case 64:
                    CurrencyName = "MYR";
                    break;
                case 5:
                    CurrencyName = "AUD";
                    break;
                case 34:
                    CurrencyName = "EUR";
                    break;
                case 108:
                    CurrencyName = "GBP";
                    break;
                case 46:
                    CurrencyName = "INR";
                    break;
                case 121:
                    CurrencyName = "AED";
                    break;
                case 184:
                    CurrencyName = "OMR";
                    break;
                case 192:
                    CurrencyName = "SAR";
                    break;
                case 91:
                    CurrencyName = "SGD";
                    break;
                case 94:
                    CurrencyName = "ZAR";
                    break;
                case 98:
                    CurrencyName = "CHF";
                    break;
                case 104:
                    CurrencyName = "TRY";
                    break;
                case 22:
                    CurrencyName = "CNY";
                    break;
                case 47:
                    CurrencyName = "IDR";
                    break;
                case 31:
                    CurrencyName = "EGP";
                    break;
                case 43:
                    CurrencyName = "HKD";
                    break;
                case 126:
                    CurrencyName = "BHD";
                    break;
                case 161:
                    CurrencyName = "KWD";
                    break;
                case 189:
                    CurrencyName = "QAR";
                    break;
                case 6:
                    CurrencyName = "AZN";
                    break;
                case 82:
                    CurrencyName = "PHP";
                    break;
                case 156:
                    CurrencyName = "JOD";
                    break;
                case 72:
                    CurrencyName = "NZD";
                    break;
                case 117:
                    CurrencyName = "MAD";
                    break;
                case 16:
                    CurrencyName = "BRL";
                    break;
                case 66:
                    CurrencyName = "MXN";
                    break;
                case 21:
                    CurrencyName = "CLP";
                    break;
                case 23:
                    CurrencyName = "COP";
                    break;
                case 29:
                    CurrencyName = "DOP";
                    break;
                default:
                    CurrencyName = "";
                    break;
            }
            return CurrencyName;
        }

        public string Get_Omnibees_BookingRequest(DataSet dsCartDetails, DataTable dtPassengers, DataRow[] OmnibeesDetails, string UniqueNo, bool IsPriceUpdate, int i, int jj, string PreBookingToken, string UserID, string Password, string ExternalReference, string Bookingtokan, string referenceNumber, string AgentUniqueNo, ref string errorinrq, bool IsPruvo)
        {
            string Error = "";
            try
            {
                string OmnibeesUserName = Convert.ToString(OmnibeesDetails[0]["UserName"]);
                string OmnibeesPassword = Convert.ToString(OmnibeesDetails[0]["Password"]);
                string version = Convert.ToString(OmnibeesDetails[0]["Version"]);
                string xmlhotelid = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["XMLHotelId"]);
                string CheckIn = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["CheckInDate"]);//"20180327"
                                                                                                  //CheckIn = CheckIn.Substring(6, 2) + "-" + CheckIn.Substring(4, 2) + "-" + CheckIn.Substring(0, 4);
                CheckIn = CheckIn.Substring(0, 4) + "-" + CheckIn.Substring(4, 2) + "-" + CheckIn.Substring(6, 2);
                string CheckOut = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["CheckOutDate"]);
                //CheckOut = CheckOut.Substring(6, 2) + "-" + CheckOut.Substring(4, 2) + "-" + CheckOut.Substring(0, 4);
                CheckOut = CheckOut.Substring(0, 4) + "-" + CheckOut.Substring(4, 2) + "-" + CheckOut.Substring(6, 2);
                int NoOfNights = Convert.ToInt32(dsCartDetails.Tables[0].Rows[i]["NoOfNight"]);
                int NoOfRoom = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView.Count);
                int adultcount = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[jj]["NoOfAdults"]);
                int childage1 = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[jj]["ChildAge1"]);
                int childage2 = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[jj]["ChildAge2"]);
                var NoOfChild = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[jj]["NoOfChild"]);
                string SupplierName = Convert.ToString(dsCartDetails.Tables[1].DefaultView[jj]["SupplierName"]);

                #region Room RQ RS
                string jsonString = "";
                try
                {
                    //string CheckIn = SearchParam.CheckInDate;
                    //CheckIn = CheckIn.Substring(6, 4) + "-" + CheckIn.Substring(3, 2) + "-" + CheckIn.Substring(0, 2);
                    //string CheckOut = SearchParam.CheckOutDate;
                    //CheckOut = CheckOut.Substring(6, 4) + "-" + CheckOut.Substring(3, 2) + "-" + CheckOut.Substring(0, 2);
                    Ominibees.HotelSearchRQ Request = new Ominibees.HotelSearchRQ();
                    Ominibees.HotelSearchCriteria HotelSearchCriteria = new Ominibees.HotelSearchCriteria();
                    Ominibees.Criterion Criterion = new Ominibees.Criterion();
                    Ominibees.Address Address = new Ominibees.Address();
                    List<Ominibees.HotelRef> HotelRef = new List<Ominibees.HotelRef>();
                    List<Ominibees.RoomStayCandidate> RoomStayCandidate = new List<Ominibees.RoomStayCandidate>();
                    Ominibees.RoomStayCandidatesType RoomStayCandidatesType = new Ominibees.RoomStayCandidatesType();
                    Ominibees.StayDateRange StayDateRange = new Ominibees.StayDateRange();
                    Ominibees.MultimediaObjects MultimediaObjects = new Ominibees.MultimediaObjects();
                    Ominibees.TPAExtensions TPAExtensions = new Ominibees.TPAExtensions();
                    Request.AvailRatesOnly = true;
                    Request.BestOnly = false;
                    Request.EchoToken = "technoheavenpull_" + Guid.NewGuid().ToString();

                    //Request.HotelSearchCriteria.Criterion.Address.CountryCode = 40;
                    //Request.HotelSearchCriteria.AvailableOnlyIndicator = null;

                    if (xmlhotelid != null)
                    {
                        Ominibees.HotelRef HotelId = new Ominibees.HotelRef();
                        HotelId.HotelCode = Convert.ToInt32(xmlhotelid);
                        HotelRef.Add(HotelId);
                    }
                    else
                    {
                        errorinrq = "HotelId i not available";
                        return "";
                    }
                    Criterion.HotelRefs = HotelRef;
                    //Request.HotelSearchCriteria.Criterion.Address.CountryCode = 40;
                    //Request.HotelSearchCriteria.Criterion.RoomStayCandidatesType.RoomStayCandidates[0].GuestCountsType.GuestCounts[0].AgeQualifyCode = 10;
                    //Request.HotelSearchCriteria.Criterion.RoomStayCandidatesType.RoomStayCandidates[0].GuestCountsType.GuestCounts[0].Count = 2;
                    for (int x = 0; x < NoOfRoom; x++)
                    {
                        //int NoOfRoom = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView.Count);
                        adultcount = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[x]["NoOfAdults"]);
                        childage1 = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[x]["ChildAge1"]);
                        childage2 = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[x]["ChildAge2"]);
                        NoOfChild = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[x]["NoOfChild"]);

                        Ominibees.GuestCount GuestCount_obj = new Ominibees.GuestCount();
                        Ominibees.RoomStayCandidate RoomStayCandidate_obj = new Ominibees.RoomStayCandidate();
                        Ominibees.GuestCountsType GuestCountsType = new Ominibees.GuestCountsType();
                        List<Ominibees.GuestCount> GuestCount = new List<Ominibees.GuestCount>();
                        GuestCount_obj.AgeQualifyCode = 10;
                        GuestCount_obj.Count = adultcount;
                        GuestCount.Add(GuestCount_obj);
                        if (NoOfChild > 0)
                        {
                            if (NoOfChild == 1)
                            {
                                Ominibees.GuestCount Child1GuestCount_obj = new Ominibees.GuestCount();
                                Child1GuestCount_obj.Age = childage1;
                                Child1GuestCount_obj.AgeQualifyCode = 8;
                                Child1GuestCount_obj.Count = 1;
                                GuestCount.Add(Child1GuestCount_obj);
                            }
                            else
                            {
                                Ominibees.GuestCount Child1GuestCount_obj = new Ominibees.GuestCount();
                                Child1GuestCount_obj.Age = childage1;
                                Child1GuestCount_obj.AgeQualifyCode = 8;
                                Child1GuestCount_obj.Count = 1;
                                GuestCount.Add(Child1GuestCount_obj);

                                Ominibees.GuestCount Child2GuestCount_obj = new Ominibees.GuestCount();
                                Child2GuestCount_obj.Age = childage2;
                                Child2GuestCount_obj.AgeQualifyCode = 8;
                                Child2GuestCount_obj.Count = 1;
                                GuestCount.Add(Child2GuestCount_obj);
                            }
                        }
                        GuestCountsType.GuestCounts = GuestCount;
                        //RoomStayCandidate.GuestCountsType = GuestCount;
                        //Request.HotelSearchCriteria.Criterion.RoomStayCandidatesType.RoomStayCandidates[0].Quantity = 1;
                        RoomStayCandidate_obj.Quantity = 1;
                        RoomStayCandidate_obj.RPH = x;
                        RoomStayCandidate_obj.GuestCountsType = GuestCountsType;
                        RoomStayCandidate.Add(RoomStayCandidate_obj);
                    }
                    //Request.HotelSearchCriteria.Criterion.StayDateRange.End = Convert.ToDateTime("2021-03-24T00:00:00");
                    //Request.HotelSearchCriteria.Criterion.StayDateRange.Start = Convert.ToDateTime("2021-03-23T00:00:00");
                    //Criterion.RatePlanCandidatesType = RoomStayCandidatesType;
                    RoomStayCandidatesType.RoomStayCandidates = RoomStayCandidate;
                    Criterion.RoomStayCandidatesType = RoomStayCandidatesType;

                    //StayDateRange.End = Convert.ToDateTime("2021-03-24T00:00:00");
                    //StayDateRange.Start = Convert.ToDateTime("2021-03-23T00:00:00");

                    StayDateRange.End = Convert.ToDateTime(CheckOut);
                    StayDateRange.Start = Convert.ToDateTime(CheckIn);

                    Criterion.StayDateRange = StayDateRange;

                    //Request.HotelSearchCriteria.Criterion.TPA_Extensions.IsForMobile = false;
                    //Request.HotelSearchCriteria.Criterion.TPA_Extensions.MultimediaObjects.SendData = false;

                    TPAExtensions.IsForMobile = false;
                    MultimediaObjects.SendData = false;

                    TPAExtensions.MultimediaObjects = MultimediaObjects;

                    Criterion.TPA_Extensions = TPAExtensions;

                    HotelSearchCriteria.Criterion = Criterion;

                    Request.HotelSearchCriteria = HotelSearchCriteria;

                    Request.OnRequestInd = false;
                    Request.PrimaryLangID = 1;
                    Request.Target = Convert.ToInt32(_supplierConfig.Omnibees.Target);
                    //Request.RequestedCurrency = Convert.ToInt32(ConfigurationManager.AppSettings["RequestedCurrency"]);
                    //Request.RequestedCurrency = 34;
                    string timeStamp = GetTimestamp(DateTime.UtcNow);
                    Request.TimeStamp = Convert.ToDateTime(DateTime.UtcNow); //Convert.ToDateTime("2021-03-19T06:00:37.2687519+00:00");
                    Request.Version = Convert.ToDouble(version);
                    jsonString = Newtonsoft.Json.JsonConvert.SerializeObject(Request);
                }
                catch (Exception ex)
                {
                    _ = _exception.ErrorBooking(ex, "");
                    errorinrq = "Issue in room request";
                    return "";
                }
                #endregion

                string OmnibeesUrl = Convert.ToString(OmnibeesDetails[0]["WebSiteURL"] + _supplierConfig.Omnibees.OmnibessHotelAvai);
                string GetRoomResponse = GetOmnibeesResponse(ref Error, jsonString, OmnibeesUrl, 180000, OmnibeesUserName, OmnibeesPassword, SupplierName);


                _commonMethod.ThirdPartyBookingHistory(Convert.ToString(UniqueNo), Convert.ToString(AgentUniqueNo), jsonString, GetRoomResponse + Error, "Before Request hit Room request", Convert.ToString(dsCartDetails.Tables[1].DefaultView[jj]["SupplierName"]), "Request Confirm", "-", "TechnoHub");

                List<OmnibeesBookRQ.RatePlan> RatePlanJsonJ = null;
                List<OmnibeesBookRQ.RoomType> RoomTypeJson = null;
                List<OmnibeesBookRQ.RoomRate> RoomRateJson = null;
                List<HotelSearchResponse.RoomType> RoomFilter = null;
                List<HotelSearchResponse.RatePlan> RatePlanFilter = null;
                List<HotelSearchResponse.RoomRate> RoomRateFilter = null;
                double AmountAfterTax = 0;
                double AmountBeforeTax = 0;
                int CurrencyCode = 0;
                bool AmountIncludingMarkup = true;
                bool AmountIsPackage = false;
                int RoomStayCandidateRPH = 0;
                string GuaranteeTypeCode = "";

                for (int j = 0; j < dsCartDetails.Tables[1].DefaultView.Count; j++)
                {

                    string RoomTypeId = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomTypeID"]);
                    string[] spitidtoken = RoomTypeId.Split(new String[] { "$$$" }, StringSplitOptions.None);

                    int RoomId = Convert.ToInt32(spitidtoken[0]);
                    int RatePlanId = Convert.ToInt32(spitidtoken[1]);
                    double RoomPrice = Convert.ToDouble(spitidtoken[2]);
                    AmountAfterTax = RoomPrice + AmountAfterTax;
                    double RoomPrice1 = Convert.ToDouble(spitidtoken[3]);
                    AmountBeforeTax = RoomPrice1 + AmountBeforeTax;
                    CurrencyCode = Convert.ToInt32(spitidtoken[4]);
                    double CurrencyFactorOmnibees = Convert.ToDouble(spitidtoken[5]);

                    AmountIncludingMarkup = Convert.ToBoolean(spitidtoken[6]);
                    AmountIsPackage = Convert.ToBoolean(spitidtoken[7]);
                    RoomStayCandidateRPH = Convert.ToInt32(spitidtoken[8]);

                    RoomPrice = RoomPrice * CurrencyFactorOmnibees;

                    HotelSearchResponse.HotelSearchRS OmnibeesResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<HotelSearchResponse.HotelSearchRS>(GetRoomResponse);

                    RoomFilter = OmnibeesResponse.RoomStaysType.RoomStays[0].RoomTypes.Where(item => item.RoomID == RoomId).ToList();
                    RatePlanFilter = OmnibeesResponse.RoomStaysType.RoomStays[0].RatePlans.Where(item => item.RatePlanID == RatePlanId).ToList();
                    RoomRateFilter = OmnibeesResponse.RoomStaysType.RoomStays[0].RoomRates.Where(item => item.RoomID == RoomId && item.RatePlanID == RatePlanId && item.Total.AmountIncludingMarkup == AmountIncludingMarkup && item.Total.AmountIsPackage == AmountIsPackage && item.RoomStayCandidateRPH == RoomStayCandidateRPH).ToList();


                    //bool isshow = false;

                    //for (int v = 0; v < RatePlanFilter[0].PaymentPolicies.AcceptedPayments.Count; v++)
                    //{
                    //    if (RatePlanFilter[0].PaymentPolicies.AcceptedPayments[v].GuaranteeTypeCode == 3 || RatePlanFilter[0].PaymentPolicies.AcceptedPayments[v].GuaranteeTypeCode == 4)
                    //    {
                    //        int GuaranteeTypeCode1 = RatePlanFilter[0].PaymentPolicies.AcceptedPayments[v].GuaranteeTypeCode;
                    //        GuaranteeTypeCode = GuaranteeTypeCode + "," + GuaranteeTypeCode1;
                    //        isshow = true;
                    //    }
                    //}
                    //if (isshow == false)
                    //{
                    //    ThirdPartyBookingHistory(Convert.ToString(UniqueNo), Convert.ToString(AgentUniqueNo), jsonString, GetRoomResponse, "PayMent Not Supported", Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["SupplierName"]), "Request Confirm", "-", "TechnoHub");
                    //    return "";
                    //}

                    string RatePlanJ = Newtonsoft.Json.JsonConvert.SerializeObject(RatePlanFilter);
                    RatePlanJsonJ = Newtonsoft.Json.JsonConvert.DeserializeObject<List<OmnibeesBookRQ.RatePlan>>(RatePlanJ);

                    string RoomJ = Newtonsoft.Json.JsonConvert.SerializeObject(RoomFilter);
                    RoomTypeJson = Newtonsoft.Json.JsonConvert.DeserializeObject<List<OmnibeesBookRQ.RoomType>>(RoomJ);

                    string RoomRateF = Newtonsoft.Json.JsonConvert.SerializeObject(RoomRateFilter);
                    RoomRateJson = Newtonsoft.Json.JsonConvert.DeserializeObject<List<OmnibeesBookRQ.RoomRate>>(RoomRateF);

                    string RoomType = Newtonsoft.Json.JsonConvert.SerializeObject(RoomFilter);

                    string RoomName = RoomFilter[0].RoomName;
                    string RatePlan = RatePlanFilter[0].RatePlanName;
                    int RoomNo = RoomRateFilter[0].RoomStayCandidateRPH + 1;
                    //double AmountAfterTax = RoomRateFilter[0].Total.AmountAfterTax;
                    double Price = RoomRateFilter[0].Total.AmountAfterTax;

                    //double CurrencyFactorOmnibees = 0;
                    CurrencyCode = RoomRateFilter[0].Total.CurrencyCode;
                    string OmnibeesCurrency = ReturnOmnibeesCurrency(CurrencyCode);

                    if (OmnibeesCurrency != "")
                    {
                        DataTable dtInrCurrency = _commonMethod.GetCountryCurrency(OmnibeesCurrency);
                        CurrencyFactorOmnibees = Convert.ToDouble(dtInrCurrency.Rows[0]["AmtToExch"]);
                    }
                    else
                    {
                        errorinrq = "Issue in Currency " + OmnibeesCurrency;
                        return "";
                    }

                    Price = Price * CurrencyFactorOmnibees;
                    string roomAvailability = RoomRateFilter[0].Availability[0].AvailabilityStatus;
                    string mealName = "Room Only";

                    if (RatePlanFilter[0].MealsIncluded != null)
                    {
                        mealName = RatePlanFilter[0].MealsIncluded.Name;
                    }
                    if (roomAvailability == "AvailableForSale")
                        roomAvailability = "Available";
                    else
                    {
                        errorinrq = "Room is not available";
                        return "";
                    }
                    if (RoomPrice.TruncateDecimalPlaces(0) < Price.TruncateDecimalPlaces(0))
                    {
                        errorinrq = "Price has been changed";
                        return "";
                    }
                }
                #region HotelReservationsType
                List<OmnibeesBookRQ.HotelReservation> ListhotelReservation = new List<OmnibeesBookRQ.HotelReservation>();
                for (int r = 0; r < 1; r++)
                {
                    #region TPA_Extensions
                    OmnibeesBookRQ.TPAExtensions TPAExtensions = new OmnibeesBookRQ.TPAExtensions();
                    TPAExtensions.SupplierCode = referenceNumber;
                    #endregion
                    RoomRateJson[0].TPA_Extensions = TPAExtensions;
                    #region ResGlobalInfo
                    OmnibeesBookRQ.CommentsType commentsType = new OmnibeesBookRQ.CommentsType();
                    List<OmnibeesBookRQ.Comment> lstComment = new List<OmnibeesBookRQ.Comment>();
                    for (int p = 0; p < 1; p++)
                    {
                        OmnibeesBookRQ.Comment comment = new OmnibeesBookRQ.Comment();
                        comment.Description = "Our internal reservation number " + referenceNumber;
                        comment.Language = null;
                        comment.Name = null;
                        lstComment.Add(comment);
                    }
                    commentsType.Comments = lstComment;

                    List<OmnibeesBookRQ.GuaranteesAccepted> lstGuaranteesAccepted = new List<OmnibeesBookRQ.GuaranteesAccepted>();
                    OmnibeesBookRQ.GuaranteesAccepted guaranteesAccepted = new OmnibeesBookRQ.GuaranteesAccepted();
                    guaranteesAccepted.GuaranteeID = null;

                    //if (GuaranteeTypeCode.Contains("3"))
                    //    guaranteesAccepted.GuaranteeTypeCode = 3;
                    //else
                    //    guaranteesAccepted.GuaranteeTypeCode = 4;

                    //guaranteesAccepted.PaymentCard = null;


                    #region APISO VC
                    int usercartdetailsid = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[jj]["UserCartDetailId"]);
                    string Pan = "";
                    string CVV = "0", Expiry = "0";
                    DataTable dtbookingdata = _commonMethod.GetApisoBookingData(usercartdetailsid);
                    _vcn.GetApisoServiceDatatoCreateVCN("", Convert.ToInt32(dtbookingdata.Rows[0]["OnlineBookingMasterId"]), Convert.ToInt32(dtbookingdata.Rows[0]["OnlineBookingDetailsId"]), ref Pan, ref CVV, ref Expiry, IsPruvo);
                    #endregion


                    string ExpiryRegex = Convert.ToString(Expiry).Substring(2, 2);
                    string ExpiryRegexFisrt = Convert.ToString(Expiry).Substring(0, 2);

                    //string datestring = "01-" + ExpiryRegex + "-20" + ExpiryRegexFisrt;
                    string datestring = ExpiryRegex + "-01-20" + ExpiryRegexFisrt;
                    guaranteesAccepted.GuaranteeTypeCode = 5;


                    OmnibeesBookRQ.PaymentCard PaymentCard1 = new OmnibeesBookRQ.PaymentCard();
                    PaymentCard1.CardCode = 2;
                    PaymentCard1.CardHolderName = "With Earth";
                    PaymentCard1.CardNumber = Pan;
                    PaymentCard1.EffectiveDate = Convert.ToDateTime(CheckIn);
                    PaymentCard1.ExpireDate = Convert.ToDateTime(datestring);
                    PaymentCard1.SeriesCode = CVV;

                    //PaymentCard1.CardCode = 2;
                    //PaymentCard1.CardHolderName = "Test Card";
                    //PaymentCard1.CardNumber = "4111111111111111";
                    //PaymentCard1.EffectiveDate = Convert.ToDateTime("2021-12-14T00:00:00"); ;
                    //PaymentCard1.ExpireDate = Convert.ToDateTime("2023-07-27T00:00:00");
                    //PaymentCard1.SeriesCode = "123";

                    guaranteesAccepted.PaymentCard = PaymentCard1;
                    guaranteesAccepted.RPH = 0;
                    lstGuaranteesAccepted.Add(guaranteesAccepted);

                    OmnibeesBookRQ.GuaranteesAcceptedType guaranteesAcceptedType = new OmnibeesBookRQ.GuaranteesAcceptedType();
                    guaranteesAcceptedType.GuaranteesAccepted = lstGuaranteesAccepted;

                    OmnibeesBookRQ.Guarantee gaurantee = new OmnibeesBookRQ.Guarantee();
                    gaurantee.AmountPercent = null;
                    gaurantee.DeadLine = null;
                    gaurantee.Duration = null;
                    gaurantee.End = null;
                    gaurantee.GuaranteeCode = null;
                    gaurantee.GuaranteeDescription = null;
                    gaurantee.GuaranteesAcceptedType = guaranteesAcceptedType;
                    gaurantee.Start = null;

                    OmnibeesBookRQ.Total Total1 = new OmnibeesBookRQ.Total();

                    Total1.AmountAfterTax = AmountAfterTax;
                    Total1.AmountBeforeTax = AmountBeforeTax;
                    Total1.AmountIncludingMarkup = AmountIncludingMarkup;
                    Total1.AmountIsPackage = AmountIsPackage;
                    Total1.ChargeType = 12;
                    Total1.CurrencyCode = CurrencyCode;

                    OmnibeesBookRQ.POS POS = new OmnibeesBookRQ.POS();
                    List<OmnibeesBookRQ.Source> listSource = new List<OmnibeesBookRQ.Source>();
                    OmnibeesBookRQ.Source Source = new OmnibeesBookRQ.Source();
                    Source.ISOCountry = "IND";
                    listSource.Add(Source);
                    POS.Sources = listSource;

                    OmnibeesBookRQ.ResGlobalInfo resGlobalInfo = new OmnibeesBookRQ.ResGlobalInfo();
                    resGlobalInfo.CommentsType = commentsType;
                    resGlobalInfo.Guarantee = gaurantee;
                    resGlobalInfo.Total = Total1;
                    #endregion

                    #region ResGuestsType
                    OmnibeesBookRQ.ResGuestsType resGuestsType = new OmnibeesBookRQ.ResGuestsType();
                    List<OmnibeesBookRQ.ResGuest> lstResGuest = new List<OmnibeesBookRQ.ResGuest>();
                    var RoomNumber = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[jj]["RoomNumber"]);
                    //dtPassengers.DefaultView.RowFilter = "PickUp='" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[i]["RoomTypeId"]) + "' and UserCartID=" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[i]["UserCartID"]);
                    dtPassengers.DefaultView.RowFilter = "UserCartID=" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[jj]["UserCartID"]);
                    for (int k = 0; k < dtPassengers.DefaultView.Count; k++)
                    {
                        List<OmnibeesBookRQ.ProfileInfo> lstProfileInfos = new List<OmnibeesBookRQ.ProfileInfo>();
                        for (int l = 0; l < 1; l++)
                        {
                            string firstName = Convert.ToString(dtPassengers.DefaultView[k]["FirstName"]);
                            //string SurName = Convert.ToString(dtPassengers.DefaultView[p]["LastName"]);
                            string lastName = Convert.ToString(dtPassengers.DefaultView[k]["LastName"]);
                            OmnibeesBookRQ.PersonName personName = new OmnibeesBookRQ.PersonName();
                            personName.GivenName = firstName;// "OmnibeesAdult00";
                                                             //personName.MiddleName = "Omn";
                                                             //personName.NamePrefix = 0;
                            personName.Surname = lastName;//"Mebee";                        
                            OmnibeesBookRQ.Customer customer = new OmnibeesBookRQ.Customer();
                            customer.PersonName = personName;
                            OmnibeesBookRQ.Profile profile = new OmnibeesBookRQ.Profile();
                            profile.Customer = customer;
                            OmnibeesBookRQ.ProfileInfo profileInfo = new OmnibeesBookRQ.ProfileInfo();
                            profileInfo.Profile = profile;
                            profileInfo.UniqueID = null;
                            lstProfileInfos.Add(profileInfo);
                        }
                        OmnibeesBookRQ.Profiles profiles = new OmnibeesBookRQ.Profiles();
                        profiles.ProfileInfos = lstProfileInfos;
                        OmnibeesBookRQ.ResGuest resGuest = new OmnibeesBookRQ.ResGuest();
                        if (Convert.ToString(dtPassengers.DefaultView[k]["PaxType"]).Trim().ToLower() == "adult")
                        {
                            resGuest.Age = 0;
                            resGuest.AgeQualifyingCode = 10;
                        }
                        else
                        {
                            resGuest.Age = Convert.ToInt32(dtPassengers.DefaultView[k]["ChildAge"]);
                            resGuest.AgeQualifyingCode = 8;
                        }
                        if (Convert.ToInt16(dtPassengers.DefaultView[k]["IsLead"]) == 1)
                            resGuest.PrimaryIndicator = true;
                        else
                            resGuest.PrimaryIndicator = false;

                        resGuest.Profiles = profiles;
                        resGuest.ResGuestRPH = k + 1;
                        resGuest.SpecialRequests = null;
                        lstResGuest.Add(resGuest);
                    }
                    resGuestsType.ResGuests = lstResGuest;
                    #endregion

                    #region ResStatus
                    OmnibeesBookRQ.ResStatus resStatus = new OmnibeesBookRQ.ResStatus();
                    resStatus.PMS_ResStatusType = null;
                    resStatus.TransactionActionType = 1;
                    #endregion

                    #region RoomStaysType

                    int RPHCount = 0;
                    List<OmnibeesBookRQ.RoomStay> lstRoomStays = new List<OmnibeesBookRQ.RoomStay>();
                    OmnibeesBookRQ.RoomStaysType roomStaysType = new OmnibeesBookRQ.RoomStaysType();
                    for (int j = 0; j < dsCartDetails.Tables[1].DefaultView.Count; j++)
                    {

                        string RoomTypeId = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomTypeID"]);
                        string[] spitidtoken = RoomTypeId.Split(new String[] { "$$$" }, StringSplitOptions.None);

                        int RoomId = Convert.ToInt32(spitidtoken[0]);
                        int RatePlanId = Convert.ToInt32(spitidtoken[1]);

                        double AmountAfterTaxForSingelRoom = Convert.ToDouble(spitidtoken[2]);
                        double AmountBeforeTaxForSingelRoom = Convert.ToDouble(spitidtoken[3]);

                        AmountIncludingMarkup = Convert.ToBoolean(spitidtoken[6]);
                        AmountIsPackage = Convert.ToBoolean(spitidtoken[7]);
                        RoomStayCandidateRPH = Convert.ToInt32(spitidtoken[8]);

                        HotelSearchResponse.HotelSearchRS OmnibeesResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<HotelSearchResponse.HotelSearchRS>(GetRoomResponse);

                        RoomFilter = OmnibeesResponse.RoomStaysType.RoomStays[0].RoomTypes.Where(item => item.RoomID == RoomId).ToList();
                        RatePlanFilter = OmnibeesResponse.RoomStaysType.RoomStays[0].RatePlans.Where(item => item.RatePlanID == RatePlanId).ToList();
                        RoomRateFilter = OmnibeesResponse.RoomStaysType.RoomStays[0].RoomRates.Where(item => item.RoomID == RoomId && item.RatePlanID == RatePlanId && item.Total.AmountIncludingMarkup == AmountIncludingMarkup && item.Total.AmountIsPackage == AmountIsPackage && item.RoomStayCandidateRPH == RoomStayCandidateRPH).ToList();

                        string RatePlanJ = Newtonsoft.Json.JsonConvert.SerializeObject(RatePlanFilter);
                        RatePlanJsonJ = Newtonsoft.Json.JsonConvert.DeserializeObject<List<OmnibeesBookRQ.RatePlan>>(RatePlanJ);

                        string RoomJ = Newtonsoft.Json.JsonConvert.SerializeObject(RoomFilter);
                        RoomTypeJson = Newtonsoft.Json.JsonConvert.DeserializeObject<List<OmnibeesBookRQ.RoomType>>(RoomJ);

                        string RoomRateF = Newtonsoft.Json.JsonConvert.SerializeObject(RoomRateFilter);
                        RoomRateJson = Newtonsoft.Json.JsonConvert.DeserializeObject<List<OmnibeesBookRQ.RoomRate>>(RoomRateF);

                        string RoomType = Newtonsoft.Json.JsonConvert.SerializeObject(RoomFilter);
                        OmnibeesBookRQ.RoomStay roomStay = new OmnibeesBookRQ.RoomStay();
                        roomStay.AvailabilityStatus = null;
                        #region BasicPropertyInf
                        OmnibeesBookRQ.HotelRef hotelRef = new OmnibeesBookRQ.HotelRef();
                        hotelRef.BrandName = null;
                        hotelRef.ChainCode = null;
                        hotelRef.ChainName = null;
                        hotelRef.HotelCode = Convert.ToInt32(xmlhotelid);
                        hotelRef.HotelName = null;
                        OmnibeesBookRQ.BasicPropertyInfo basicPropertyInfo = new OmnibeesBookRQ.BasicPropertyInfo();
                        basicPropertyInfo.Address = null;
                        basicPropertyInfo.Award = null;
                        basicPropertyInfo.ContactNumbersType = null;
                        basicPropertyInfo.CurrencyCode = null;
                        basicPropertyInfo.HotelAmenities = null;
                        basicPropertyInfo.HotelRef = hotelRef;
                        basicPropertyInfo.ImageURL = null;
                        basicPropertyInfo.Policy = null;
                        basicPropertyInfo.Position = null;
                        basicPropertyInfo.VendorMessagesType = null;
                        #endregion
                        roomStay.BasicPropertyInfo = basicPropertyInfo;
                        #region CommentsType
                        List<OmnibeesBookRQ.Comment> lstcomment = new List<OmnibeesBookRQ.Comment>();
                        OmnibeesBookRQ.Comment comment = new OmnibeesBookRQ.Comment();
                        comment.Description = "Our internal reservation number " + referenceNumber;
                        comment.Language = null;
                        comment.Language = null;
                        lstcomment.Add(comment);
                        OmnibeesBookRQ.CommentsType commentsTypes = new OmnibeesBookRQ.CommentsType();
                        commentsTypes.Comments = lstcomment;
                        #endregion
                        roomStay.CommentsType = commentsTypes;
                        roomStay.Guarantees = null;
                        #region GuestCountsType

                        adultcount = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[j]["NoOfAdults"]);
                        childage1 = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[j]["ChildAge1"]);
                        childage2 = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[j]["ChildAge2"]);
                        NoOfChild = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[j]["NoOfChild"]);

                        OmnibeesBookRQ.GuestCountsType guestCountsType = new OmnibeesBookRQ.GuestCountsType();
                        List<OmnibeesBookRQ.GuestCount> lstGuestCount = new List<OmnibeesBookRQ.GuestCount>();
                        OmnibeesBookRQ.GuestCount guestCount = new OmnibeesBookRQ.GuestCount();
                        guestCount.Age = null;
                        guestCount.AgeQualifyCode = 10;
                        guestCount.Count = adultcount;
                        List<int> lstResGuestRPH = new List<int>();
                        for (int A = 0; A < adultcount; A++)
                        {
                            RPHCount = RPHCount + 1;
                            lstResGuestRPH.Add(RPHCount);
                        }
                        //guestCount.ResGuestRPH = lstResGuestRPH;
                        //lstGuestCount.Add(guestCount);
                        guestCount.ResGuestRPH = lstResGuestRPH;
                        lstGuestCount.Add(guestCount);

                        if (NoOfChild > 0)
                        {
                            if (NoOfChild == 1)
                            {
                                RPHCount = RPHCount + 1;
                                OmnibeesBookRQ.GuestCount Child1GuestCount_obj = new OmnibeesBookRQ.GuestCount();
                                Child1GuestCount_obj.Age = childage1;
                                Child1GuestCount_obj.AgeQualifyCode = 8;
                                Child1GuestCount_obj.Count = 1;

                                List<int> lstResGuestRPHChild1 = new List<int>();
                                lstResGuestRPHChild1.Add(RPHCount);
                                Child1GuestCount_obj.ResGuestRPH = lstResGuestRPHChild1;
                                lstGuestCount.Add(Child1GuestCount_obj);
                            }
                            else
                            {
                                RPHCount = RPHCount + 1;
                                OmnibeesBookRQ.GuestCount Child1GuestCount_obj = new OmnibeesBookRQ.GuestCount();
                                Child1GuestCount_obj.Age = childage1;
                                Child1GuestCount_obj.AgeQualifyCode = 8;
                                Child1GuestCount_obj.Count = 1;

                                List<int> lstResGuestRPHChild1 = new List<int>();
                                lstResGuestRPHChild1.Add(RPHCount);
                                Child1GuestCount_obj.ResGuestRPH = lstResGuestRPHChild1;

                                //guestCount.ResGuestRPH = lstResGuestRPHChild1;
                                lstGuestCount.Add(Child1GuestCount_obj);

                                RPHCount = RPHCount + 1;
                                OmnibeesBookRQ.GuestCount Child2GuestCount_obj = new OmnibeesBookRQ.GuestCount();
                                Child2GuestCount_obj.Age = childage2;
                                Child2GuestCount_obj.AgeQualifyCode = 8;
                                Child2GuestCount_obj.Count = 1;

                                List<int> lstResGuestRPHChild2 = new List<int>();
                                lstResGuestRPHChild2.Add(RPHCount);
                                //Child1GuestCount_obj.ResGuestRPH = lstResGuestRPHChild2;
                                Child2GuestCount_obj.ResGuestRPH = lstResGuestRPHChild2;//Fixed on 2-APR-2025 K2

                                //guestCount.ResGuestRPH = lstResGuestRPHChild2;
                                lstGuestCount.Add(Child2GuestCount_obj);
                            }
                        }

                        guestCountsType.GuestCounts = lstGuestCount;
                        #endregion
                        roomStay.GuestCountsType = guestCountsType;
                        roomStay.IndexNumber = null;
                        roomStay.RPH = 0;
                        //roomStay.RatePlans = RatePlanFilter;

                        roomStay.RatePlans = RatePlanJsonJ;
                        roomStay.RoomRates = RoomRateJson;
                        roomStay.RoomTypes = RoomTypeJson;

                        roomStay.TPA_Extensions = null;
                        OmnibeesBookRQ.Total Total = new OmnibeesBookRQ.Total();
                        Total.AmountAfterTax = AmountAfterTaxForSingelRoom;
                        Total.AmountBeforeTax = AmountBeforeTaxForSingelRoom;
                        //Total.AmountIncludingMarkup = false;
                        Total.AmountIncludingMarkup = AmountIncludingMarkup;
                        Total.AmountIsPackage = AmountIsPackage;
                        Total.ChargeType = 12;
                        Total.CurrencyCode = CurrencyCode;
                        roomStay.Total = Total;
                        roomStay.RoomStayLanguage = 1;
                        lstRoomStays.Add(roomStay);

                        roomStaysType.RoomStays = lstRoomStays;
                        roomStaysType.MoreIndicator = false;
                        // roomStaysType.RoomStays = ;
                        #endregion
                    }
                    OmnibeesBookRQ.HotelReservation hotelReservation = new OmnibeesBookRQ.HotelReservation();
                    hotelReservation.TPA_Extensions = TPAExtensions;
                    hotelReservation.RoomStaysType = roomStaysType;
                    hotelReservation.CreateDateTime = Convert.ToDateTime(DateTime.Now);
                    hotelReservation.LastModifyDateTime = Convert.ToDateTime(DateTime.Now);
                    hotelReservation.POS = POS;
                    hotelReservation.ResGlobalInfo = resGlobalInfo;
                    hotelReservation.ResGuestsType = resGuestsType;
                    hotelReservation.ResStatus = resStatus;
                    ListhotelReservation.Add(hotelReservation);

                    OmnibeesBookRQ.HotelReservationsType hotelReservationsType = new OmnibeesBookRQ.HotelReservationsType();
                    hotelReservationsType.HotelReservations = ListhotelReservation;
                    #endregion
                    OmnibeesBookRQ.Request BookingRQ = new OmnibeesBookRQ.Request();
                    BookingRQ.EchoToken = "technoheavenpull_" + Guid.NewGuid().ToString();
                    BookingRQ.HotelReservationsType = hotelReservationsType;
                    //BookingRQ.PrimaryLangID = 1;
                    BookingRQ.ResStatus = null;
                    BookingRQ.RetransmissionIndicator = null;
                    BookingRQ.TimeStamp = Convert.ToDateTime(DateTime.UtcNow);
                    BookingRQ.Target = 0;
                    BookingRQ.Version = Convert.ToDouble(version);

                    string BookingRequest = Newtonsoft.Json.JsonConvert.SerializeObject(BookingRQ);
                    return BookingRequest;
                }
            }
            catch (Exception ex)
            {
                errorinrq = ex.Message;
                _ = _exception.ErrorBooking(ex, "");
            }
            return string.Empty;
        }
        public string Get_Omnibees_BookingRequest_newversion(BookRequest_newversion bookRequest, DataTable dtPassengers, DataRow[] OmnibeesDetails, string UniqueNo, bool IsPriceUpdate, int i, int jj, string PreBookingToken, string UserID, string Password, string ExternalReference, string Bookingtokan, string referenceNumber, string AgentUniqueNo, ref string errorinrq, bool IsPruvo=false)
        {
            string Error = "";
            try
            {
                string OmnibeesUserName = Convert.ToString(OmnibeesDetails[0]["UserName"]);
                string OmnibeesPassword = Convert.ToString(OmnibeesDetails[0]["Password"]);
                string version = Convert.ToString(OmnibeesDetails[0]["Version"]);
                string xmlhotelid = "";//Convert.ToString(dsCartDetails.Tables[0].Rows[i]["XMLHotelId"]);//need
                string CheckIn = Convert.ToString(bookRequest.userCart.UserCartDetails[jj].CheckInDate);//"20180327"
                string CheckOut = Convert.ToString(bookRequest.userCart.UserCartDetails[jj].CheckOutDate);
                //CheckOut = CheckOut.Substring(6, 2) + "-" + CheckOut.Substring(4, 2) + "-" + CheckOut.Substring(0, 4);
                CheckOut = CheckOut.Substring(0, 4) + "-" + CheckOut.Substring(4, 2) + "-" + CheckOut.Substring(6, 2);
                int NoOfNights = Convert.ToInt32(0);//need
                int NoOfRoom = Convert.ToInt32(bookRequest.userCart.UserCartDetails.Count);
                int adultcount = 0;//Convert.ToInt32(bookRequest.userCart.UserCartDetails[jj].NoofAdult);
                int childage1 = 0;//Convert.ToInt32(bookRequest.userCart.UserCartDetails[jj].ChildAge);
                int childage2 = 0; //Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[jj]["ChildAge2"]);
                var NoOfChild = Convert.ToInt32(bookRequest.userCart.UserCartDetails[jj].NoofChild);
                string SupplierName = Convert.ToString(bookRequest.userCart.UserCartDetails[jj].SupplierName);

                #region Room RQ RS
                string jsonString = "";
                try
                {
                    //string CheckIn = SearchParam.CheckInDate;
                    //CheckIn = CheckIn.Substring(6, 4) + "-" + CheckIn.Substring(3, 2) + "-" + CheckIn.Substring(0, 2);
                    //string CheckOut = SearchParam.CheckOutDate;
                    //CheckOut = CheckOut.Substring(6, 4) + "-" + CheckOut.Substring(3, 2) + "-" + CheckOut.Substring(0, 2);
                    Ominibees.HotelSearchRQ Request = new Ominibees.HotelSearchRQ();
                    Ominibees.HotelSearchCriteria HotelSearchCriteria = new Ominibees.HotelSearchCriteria();
                    Ominibees.Criterion Criterion = new Ominibees.Criterion();
                    Ominibees.Address Address = new Ominibees.Address();
                    List<Ominibees.HotelRef> HotelRef = new List<Ominibees.HotelRef>();
                    List<Ominibees.RoomStayCandidate> RoomStayCandidate = new List<Ominibees.RoomStayCandidate>();
                    Ominibees.RoomStayCandidatesType RoomStayCandidatesType = new Ominibees.RoomStayCandidatesType();
                    Ominibees.StayDateRange StayDateRange = new Ominibees.StayDateRange();
                    Ominibees.MultimediaObjects MultimediaObjects = new Ominibees.MultimediaObjects();
                    Ominibees.TPAExtensions TPAExtensions = new Ominibees.TPAExtensions();
                    Request.AvailRatesOnly = true;
                    Request.BestOnly = false;
                    Request.EchoToken = "technoheavenpull_" + Guid.NewGuid().ToString();

                    //Request.HotelSearchCriteria.Criterion.Address.CountryCode = 40;
                    //Request.HotelSearchCriteria.AvailableOnlyIndicator = null;

                    if (xmlhotelid != null)
                    {
                        Ominibees.HotelRef HotelId = new Ominibees.HotelRef();
                        HotelId.HotelCode = Convert.ToInt32(xmlhotelid);
                        HotelRef.Add(HotelId);
                    }
                    else
                    {
                        errorinrq = "HotelId i not available";
                        return "";
                    }
                    Criterion.HotelRefs = HotelRef;
                    //Request.HotelSearchCriteria.Criterion.Address.CountryCode = 40;
                    //Request.HotelSearchCriteria.Criterion.RoomStayCandidatesType.RoomStayCandidates[0].GuestCountsType.GuestCounts[0].AgeQualifyCode = 10;
                    //Request.HotelSearchCriteria.Criterion.RoomStayCandidatesType.RoomStayCandidates[0].GuestCountsType.GuestCounts[0].Count = 2;
                    for (int x = 0; x < NoOfRoom; x++)
                    {
                        //int NoOfRoom = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView.Count);
                        adultcount = Convert.ToInt32(bookRequest.userCart.UserCartDetails[x].NoofAdult);
                        childage1 = Convert.ToInt32(bookRequest.userCart.UserCartDetails[jj].ChildAge);
                        //childage2 = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[x]["ChildAge2"]);
                        NoOfChild = Convert.ToInt32(bookRequest.userCart.UserCartDetails[jj].NoofChild);

                        Ominibees.GuestCount GuestCount_obj = new Ominibees.GuestCount();
                        Ominibees.RoomStayCandidate RoomStayCandidate_obj = new Ominibees.RoomStayCandidate();
                        Ominibees.GuestCountsType GuestCountsType = new Ominibees.GuestCountsType();
                        List<Ominibees.GuestCount> GuestCount = new List<Ominibees.GuestCount>();
                        GuestCount_obj.AgeQualifyCode = 10;
                        GuestCount_obj.Count = adultcount;
                        GuestCount.Add(GuestCount_obj);
                        if (NoOfChild > 0)
                        {
                            if (NoOfChild == 1)
                            {
                                Ominibees.GuestCount Child1GuestCount_obj = new Ominibees.GuestCount();
                                Child1GuestCount_obj.Age = childage1;
                                Child1GuestCount_obj.AgeQualifyCode = 8;
                                Child1GuestCount_obj.Count = 1;
                                GuestCount.Add(Child1GuestCount_obj);
                            }
                            else
                            {
                                Ominibees.GuestCount Child1GuestCount_obj = new Ominibees.GuestCount();
                                Child1GuestCount_obj.Age = childage1;
                                Child1GuestCount_obj.AgeQualifyCode = 8;
                                Child1GuestCount_obj.Count = 1;
                                GuestCount.Add(Child1GuestCount_obj);

                                Ominibees.GuestCount Child2GuestCount_obj = new Ominibees.GuestCount();
                                Child2GuestCount_obj.Age = childage2;
                                Child2GuestCount_obj.AgeQualifyCode = 8;
                                Child2GuestCount_obj.Count = 1;
                                GuestCount.Add(Child2GuestCount_obj);
                            }
                        }
                        GuestCountsType.GuestCounts = GuestCount;
                        //RoomStayCandidate.GuestCountsType = GuestCount;
                        //Request.HotelSearchCriteria.Criterion.RoomStayCandidatesType.RoomStayCandidates[0].Quantity = 1;
                        RoomStayCandidate_obj.Quantity = 1;
                        RoomStayCandidate_obj.RPH = x;
                        RoomStayCandidate_obj.GuestCountsType = GuestCountsType;
                        RoomStayCandidate.Add(RoomStayCandidate_obj);
                    }
                    //Request.HotelSearchCriteria.Criterion.StayDateRange.End = Convert.ToDateTime("2021-03-24T00:00:00");
                    //Request.HotelSearchCriteria.Criterion.StayDateRange.Start = Convert.ToDateTime("2021-03-23T00:00:00");
                    //Criterion.RatePlanCandidatesType = RoomStayCandidatesType;
                    RoomStayCandidatesType.RoomStayCandidates = RoomStayCandidate;
                    Criterion.RoomStayCandidatesType = RoomStayCandidatesType;

                    //StayDateRange.End = Convert.ToDateTime("2021-03-24T00:00:00");
                    //StayDateRange.Start = Convert.ToDateTime("2021-03-23T00:00:00");

                    StayDateRange.End = Convert.ToDateTime(CheckOut);
                    StayDateRange.Start = Convert.ToDateTime(CheckIn);

                    Criterion.StayDateRange = StayDateRange;

                    //Request.HotelSearchCriteria.Criterion.TPA_Extensions.IsForMobile = false;
                    //Request.HotelSearchCriteria.Criterion.TPA_Extensions.MultimediaObjects.SendData = false;

                    TPAExtensions.IsForMobile = false;
                    MultimediaObjects.SendData = false;

                    TPAExtensions.MultimediaObjects = MultimediaObjects;

                    Criterion.TPA_Extensions = TPAExtensions;

                    HotelSearchCriteria.Criterion = Criterion;

                    Request.HotelSearchCriteria = HotelSearchCriteria;

                    Request.OnRequestInd = false;
                    Request.PrimaryLangID = 1;
                    Request.Target = Convert.ToInt32(_supplierConfig.Omnibees.Target);
                    //Request.RequestedCurrency = Convert.ToInt32(ConfigurationManager.AppSettings["RequestedCurrency"]);
                    //Request.RequestedCurrency = 34;
                    string timeStamp = GetTimestamp(DateTime.UtcNow);
                    Request.TimeStamp = Convert.ToDateTime(DateTime.UtcNow); //Convert.ToDateTime("2021-03-19T06:00:37.2687519+00:00");
                    Request.Version = Convert.ToDouble(version);
                    jsonString = Newtonsoft.Json.JsonConvert.SerializeObject(Request);
                }
                catch (Exception ex)
                {
                    _ = _exception.ErrorBooking(ex, "");
                    errorinrq = "Issue in room request";
                    return "";
                }
                #endregion

                string OmnibeesUrl = Convert.ToString(OmnibeesDetails[0]["WebSiteURL"] + _supplierConfig.Omnibees.OmnibessHotelAvai);
                string GetRoomResponse = GetOmnibeesResponse(ref Error, jsonString, OmnibeesUrl, 180000, OmnibeesUserName, OmnibeesPassword, SupplierName);


                _commonMethod.ThirdPartyBookingHistory(Convert.ToString(UniqueNo), Convert.ToString(AgentUniqueNo), jsonString, GetRoomResponse + Error, "Before Request hit Room request", Convert.ToString(bookRequest.userCart.UserCartDetails[jj].SupplierName), "Request Confirm", "-", "TechnoHub");

                List<OmnibeesBookRQ.RatePlan> RatePlanJsonJ = null;
                List<OmnibeesBookRQ.RoomType> RoomTypeJson = null;
                List<OmnibeesBookRQ.RoomRate> RoomRateJson = null;
                List<HotelSearchResponse.RoomType> RoomFilter = null;
                List<HotelSearchResponse.RatePlan> RatePlanFilter = null;
                List<HotelSearchResponse.RoomRate> RoomRateFilter = null;
                double AmountAfterTax = 0;
                double AmountBeforeTax = 0;
                int CurrencyCode = 0;
                bool AmountIncludingMarkup = true;
                bool AmountIsPackage = false;
                int RoomStayCandidateRPH = 0;
                string GuaranteeTypeCode = "";

                for (int j = 0; j < bookRequest.userCart.UserCartDetails.Count; j++)
                {

                    string RoomTypeId = Convert.ToString(bookRequest.userCart.UserCartDetails[j].RoomTypeId);
                    string[] spitidtoken = RoomTypeId.Split(new String[] { "$$$" }, StringSplitOptions.None);

                    int RoomId = Convert.ToInt32(spitidtoken[0]);
                    int RatePlanId = Convert.ToInt32(spitidtoken[1]);
                    double RoomPrice = Convert.ToDouble(spitidtoken[2]);
                    AmountAfterTax = RoomPrice + AmountAfterTax;
                    double RoomPrice1 = Convert.ToDouble(spitidtoken[3]);
                    AmountBeforeTax = RoomPrice1 + AmountBeforeTax;
                    CurrencyCode = Convert.ToInt32(spitidtoken[4]);
                    double CurrencyFactorOmnibees = Convert.ToDouble(spitidtoken[5]);

                    AmountIncludingMarkup = Convert.ToBoolean(spitidtoken[6]);
                    AmountIsPackage = Convert.ToBoolean(spitidtoken[7]);
                    RoomStayCandidateRPH = Convert.ToInt32(spitidtoken[8]);

                    RoomPrice = RoomPrice * CurrencyFactorOmnibees;

                    HotelSearchResponse.HotelSearchRS OmnibeesResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<HotelSearchResponse.HotelSearchRS>(GetRoomResponse);

                    RoomFilter = OmnibeesResponse.RoomStaysType.RoomStays[0].RoomTypes.Where(item => item.RoomID == RoomId).ToList();
                    RatePlanFilter = OmnibeesResponse.RoomStaysType.RoomStays[0].RatePlans.Where(item => item.RatePlanID == RatePlanId).ToList();
                    RoomRateFilter = OmnibeesResponse.RoomStaysType.RoomStays[0].RoomRates.Where(item => item.RoomID == RoomId && item.RatePlanID == RatePlanId && item.Total.AmountIncludingMarkup == AmountIncludingMarkup && item.Total.AmountIsPackage == AmountIsPackage && item.RoomStayCandidateRPH == RoomStayCandidateRPH).ToList();


                    //bool isshow = false;

                    //for (int v = 0; v < RatePlanFilter[0].PaymentPolicies.AcceptedPayments.Count; v++)
                    //{
                    //    if (RatePlanFilter[0].PaymentPolicies.AcceptedPayments[v].GuaranteeTypeCode == 3 || RatePlanFilter[0].PaymentPolicies.AcceptedPayments[v].GuaranteeTypeCode == 4)
                    //    {
                    //        int GuaranteeTypeCode1 = RatePlanFilter[0].PaymentPolicies.AcceptedPayments[v].GuaranteeTypeCode;
                    //        GuaranteeTypeCode = GuaranteeTypeCode + "," + GuaranteeTypeCode1;
                    //        isshow = true;
                    //    }
                    //}
                    //if (isshow == false)
                    //{
                    //    ThirdPartyBookingHistory(Convert.ToString(UniqueNo), Convert.ToString(AgentUniqueNo), jsonString, GetRoomResponse, "PayMent Not Supported", Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["SupplierName"]), "Request Confirm", "-", "TechnoHub");
                    //    return "";
                    //}

                    string RatePlanJ = Newtonsoft.Json.JsonConvert.SerializeObject(RatePlanFilter);
                    RatePlanJsonJ = Newtonsoft.Json.JsonConvert.DeserializeObject<List<OmnibeesBookRQ.RatePlan>>(RatePlanJ);

                    string RoomJ = Newtonsoft.Json.JsonConvert.SerializeObject(RoomFilter);
                    RoomTypeJson = Newtonsoft.Json.JsonConvert.DeserializeObject<List<OmnibeesBookRQ.RoomType>>(RoomJ);

                    string RoomRateF = Newtonsoft.Json.JsonConvert.SerializeObject(RoomRateFilter);
                    RoomRateJson = Newtonsoft.Json.JsonConvert.DeserializeObject<List<OmnibeesBookRQ.RoomRate>>(RoomRateF);

                    string RoomType = Newtonsoft.Json.JsonConvert.SerializeObject(RoomFilter);

                    string RoomName = RoomFilter[0].RoomName;
                    string RatePlan = RatePlanFilter[0].RatePlanName;
                    int RoomNo = RoomRateFilter[0].RoomStayCandidateRPH + 1;
                    //double AmountAfterTax = RoomRateFilter[0].Total.AmountAfterTax;
                    double Price = RoomRateFilter[0].Total.AmountAfterTax;

                    //double CurrencyFactorOmnibees = 0;
                    CurrencyCode = RoomRateFilter[0].Total.CurrencyCode;
                    string OmnibeesCurrency = ReturnOmnibeesCurrency(CurrencyCode);

                    if (OmnibeesCurrency != "")
                    {
                        DataTable dtInrCurrency = _commonMethod.GetCountryCurrency(OmnibeesCurrency);
                        CurrencyFactorOmnibees = Convert.ToDouble(dtInrCurrency.Rows[0]["AmtToExch"]);
                    }
                    else
                    {
                        errorinrq = "Issue in Currency " + OmnibeesCurrency;
                        return "";
                    }

                    Price = Price * CurrencyFactorOmnibees;
                    string roomAvailability = RoomRateFilter[0].Availability[0].AvailabilityStatus;
                    string mealName = "Room Only";

                    if (RatePlanFilter[0].MealsIncluded != null)
                    {
                        mealName = RatePlanFilter[0].MealsIncluded.Name;
                    }
                    if (roomAvailability == "AvailableForSale")
                        roomAvailability = "Available";
                    else
                    {
                        errorinrq = "Room is not available";
                        return "";
                    }
                    if (RoomPrice.TruncateDecimalPlaces(0) < Price.TruncateDecimalPlaces(0))
                    {
                        errorinrq = "Price has been changed";
                        return "";
                    }
                }
                #region HotelReservationsType
                List<OmnibeesBookRQ.HotelReservation> ListhotelReservation = new List<OmnibeesBookRQ.HotelReservation>();
                for (int r = 0; r < 1; r++)
                {
                    #region TPA_Extensions
                    OmnibeesBookRQ.TPAExtensions TPAExtensions = new OmnibeesBookRQ.TPAExtensions();
                    TPAExtensions.SupplierCode = referenceNumber;
                    #endregion
                    RoomRateJson[0].TPA_Extensions = TPAExtensions;
                    #region ResGlobalInfo
                    OmnibeesBookRQ.CommentsType commentsType = new OmnibeesBookRQ.CommentsType();
                    List<OmnibeesBookRQ.Comment> lstComment = new List<OmnibeesBookRQ.Comment>();
                    for (int p = 0; p < 1; p++)
                    {
                        OmnibeesBookRQ.Comment comment = new OmnibeesBookRQ.Comment();
                        comment.Description = "Our internal reservation number " + referenceNumber;
                        comment.Language = null;
                        comment.Name = null;
                        lstComment.Add(comment);
                    }
                    commentsType.Comments = lstComment;

                    List<OmnibeesBookRQ.GuaranteesAccepted> lstGuaranteesAccepted = new List<OmnibeesBookRQ.GuaranteesAccepted>();
                    OmnibeesBookRQ.GuaranteesAccepted guaranteesAccepted = new OmnibeesBookRQ.GuaranteesAccepted();
                    guaranteesAccepted.GuaranteeID = null;

                    //if (GuaranteeTypeCode.Contains("3"))
                    //    guaranteesAccepted.GuaranteeTypeCode = 3;
                    //else
                    //    guaranteesAccepted.GuaranteeTypeCode = 4;

                    //guaranteesAccepted.PaymentCard = null;


                    #region APISO VC
                    int usercartdetailsid = Convert.ToInt32(bookRequest.userCart.UserCartDetails[jj].UserCartDetailId);
                    string Pan = "";
                    string CVV = "0", Expiry = "0";
                    DataTable dtbookingdata = _commonMethod.GetApisoBookingData(usercartdetailsid);
                    _vcn.GetApisoServiceDatatoCreateVCN("", Convert.ToInt32(dtbookingdata.Rows[0]["OnlineBookingMasterId"]), Convert.ToInt32(dtbookingdata.Rows[0]["OnlineBookingDetailsId"]), ref Pan, ref CVV, ref Expiry, IsPruvo);
                    #endregion


                    string ExpiryRegex = Convert.ToString(Expiry).Substring(2, 2);
                    string ExpiryRegexFisrt = Convert.ToString(Expiry).Substring(0, 2);

                    //string datestring = "01-" + ExpiryRegex + "-20" + ExpiryRegexFisrt;
                    string datestring = ExpiryRegex + "-01-20" + ExpiryRegexFisrt;
                    guaranteesAccepted.GuaranteeTypeCode = 5;


                    OmnibeesBookRQ.PaymentCard PaymentCard1 = new OmnibeesBookRQ.PaymentCard();
                    PaymentCard1.CardCode = 2;
                    PaymentCard1.CardHolderName = "With Earth";
                    PaymentCard1.CardNumber = Pan;
                    PaymentCard1.EffectiveDate = Convert.ToDateTime(CheckIn);
                    PaymentCard1.ExpireDate = Convert.ToDateTime(datestring);
                    PaymentCard1.SeriesCode = CVV;

                    //PaymentCard1.CardCode = 2;
                    //PaymentCard1.CardHolderName = "Test Card";
                    //PaymentCard1.CardNumber = "4111111111111111";
                    //PaymentCard1.EffectiveDate = Convert.ToDateTime("2021-12-14T00:00:00"); ;
                    //PaymentCard1.ExpireDate = Convert.ToDateTime("2023-07-27T00:00:00");
                    //PaymentCard1.SeriesCode = "123";

                    guaranteesAccepted.PaymentCard = PaymentCard1;
                    guaranteesAccepted.RPH = 0;
                    lstGuaranteesAccepted.Add(guaranteesAccepted);

                    OmnibeesBookRQ.GuaranteesAcceptedType guaranteesAcceptedType = new OmnibeesBookRQ.GuaranteesAcceptedType();
                    guaranteesAcceptedType.GuaranteesAccepted = lstGuaranteesAccepted;

                    OmnibeesBookRQ.Guarantee gaurantee = new OmnibeesBookRQ.Guarantee();
                    gaurantee.AmountPercent = null;
                    gaurantee.DeadLine = null;
                    gaurantee.Duration = null;
                    gaurantee.End = null;
                    gaurantee.GuaranteeCode = null;
                    gaurantee.GuaranteeDescription = null;
                    gaurantee.GuaranteesAcceptedType = guaranteesAcceptedType;
                    gaurantee.Start = null;

                    OmnibeesBookRQ.Total Total1 = new OmnibeesBookRQ.Total();

                    Total1.AmountAfterTax = AmountAfterTax;
                    Total1.AmountBeforeTax = AmountBeforeTax;
                    Total1.AmountIncludingMarkup = AmountIncludingMarkup;
                    Total1.AmountIsPackage = AmountIsPackage;
                    Total1.ChargeType = 12;
                    Total1.CurrencyCode = CurrencyCode;

                    OmnibeesBookRQ.POS POS = new OmnibeesBookRQ.POS();
                    List<OmnibeesBookRQ.Source> listSource = new List<OmnibeesBookRQ.Source>();
                    OmnibeesBookRQ.Source Source = new OmnibeesBookRQ.Source();
                    Source.ISOCountry = "IND";
                    listSource.Add(Source);
                    POS.Sources = listSource;

                    OmnibeesBookRQ.ResGlobalInfo resGlobalInfo = new OmnibeesBookRQ.ResGlobalInfo();
                    resGlobalInfo.CommentsType = commentsType;
                    resGlobalInfo.Guarantee = gaurantee;
                    resGlobalInfo.Total = Total1;
                    #endregion

                    #region ResGuestsType
                    OmnibeesBookRQ.ResGuestsType resGuestsType = new OmnibeesBookRQ.ResGuestsType();
                    List<OmnibeesBookRQ.ResGuest> lstResGuest = new List<OmnibeesBookRQ.ResGuest>();
                    //var RoomNumber = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[jj]["RoomNumber"]);
                    var RoomNumber = Convert.ToInt32(0);//need
                    //dtPassengers.DefaultView.RowFilter = "PickUp='" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[i]["RoomTypeId"]) + "' and UserCartID=" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[i]["UserCartID"]);
                    dtPassengers.DefaultView.RowFilter = "UserCartID=" + Convert.ToString(bookRequest.userCart.UserCartDetails[jj].UserCartId);
                    for (int k = 0; k < dtPassengers.DefaultView.Count; k++)
                    {
                        List<OmnibeesBookRQ.ProfileInfo> lstProfileInfos = new List<OmnibeesBookRQ.ProfileInfo>();
                        for (int l = 0; l < 1; l++)
                        {
                            string firstName = Convert.ToString(dtPassengers.DefaultView[k]["FirstName"]);
                            //string SurName = Convert.ToString(dtPassengers.DefaultView[p]["LastName"]);
                            string lastName = Convert.ToString(dtPassengers.DefaultView[k]["LastName"]);
                            OmnibeesBookRQ.PersonName personName = new OmnibeesBookRQ.PersonName();
                            personName.GivenName = firstName;// "OmnibeesAdult00";
                                                             //personName.MiddleName = "Omn";
                                                             //personName.NamePrefix = 0;
                            personName.Surname = lastName;//"Mebee";                        
                            OmnibeesBookRQ.Customer customer = new OmnibeesBookRQ.Customer();
                            customer.PersonName = personName;
                            OmnibeesBookRQ.Profile profile = new OmnibeesBookRQ.Profile();
                            profile.Customer = customer;
                            OmnibeesBookRQ.ProfileInfo profileInfo = new OmnibeesBookRQ.ProfileInfo();
                            profileInfo.Profile = profile;
                            profileInfo.UniqueID = null;
                            lstProfileInfos.Add(profileInfo);
                        }
                        OmnibeesBookRQ.Profiles profiles = new OmnibeesBookRQ.Profiles();
                        profiles.ProfileInfos = lstProfileInfos;
                        OmnibeesBookRQ.ResGuest resGuest = new OmnibeesBookRQ.ResGuest();
                        if (Convert.ToString(dtPassengers.DefaultView[k]["PaxType"]).Trim().ToLower() == "adult")
                        {
                            resGuest.Age = 0;
                            resGuest.AgeQualifyingCode = 10;
                        }
                        else
                        {
                            resGuest.Age = Convert.ToInt32(dtPassengers.DefaultView[k]["ChildAge"]);
                            resGuest.AgeQualifyingCode = 8;
                        }
                        if (Convert.ToInt16(dtPassengers.DefaultView[k]["IsLead"]) == 1)
                            resGuest.PrimaryIndicator = true;
                        else
                            resGuest.PrimaryIndicator = false;

                        resGuest.Profiles = profiles;
                        resGuest.ResGuestRPH = k + 1;
                        resGuest.SpecialRequests = null;
                        lstResGuest.Add(resGuest);
                    }
                    resGuestsType.ResGuests = lstResGuest;
                    #endregion

                    #region ResStatus
                    OmnibeesBookRQ.ResStatus resStatus = new OmnibeesBookRQ.ResStatus();
                    resStatus.PMS_ResStatusType = null;
                    resStatus.TransactionActionType = 1;
                    #endregion

                    #region RoomStaysType

                    int RPHCount = 0;
                    List<OmnibeesBookRQ.RoomStay> lstRoomStays = new List<OmnibeesBookRQ.RoomStay>();
                    OmnibeesBookRQ.RoomStaysType roomStaysType = new OmnibeesBookRQ.RoomStaysType();
                    for (int j = 0; j < bookRequest.userCart.UserCartDetails.Count; j++)
                    {

                        string RoomTypeId = Convert.ToString(bookRequest.userCart.UserCartDetails[jj].RoomTypeId);
                        string[] spitidtoken = RoomTypeId.Split(new String[] { "$$$" }, StringSplitOptions.None);

                        int RoomId = Convert.ToInt32(spitidtoken[0]);
                        int RatePlanId = Convert.ToInt32(spitidtoken[1]);

                        double AmountAfterTaxForSingelRoom = Convert.ToDouble(spitidtoken[2]);
                        double AmountBeforeTaxForSingelRoom = Convert.ToDouble(spitidtoken[3]);

                        AmountIncludingMarkup = Convert.ToBoolean(spitidtoken[6]);
                        AmountIsPackage = Convert.ToBoolean(spitidtoken[7]);
                        RoomStayCandidateRPH = Convert.ToInt32(spitidtoken[8]);

                        HotelSearchResponse.HotelSearchRS OmnibeesResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<HotelSearchResponse.HotelSearchRS>(GetRoomResponse);

                        RoomFilter = OmnibeesResponse.RoomStaysType.RoomStays[0].RoomTypes.Where(item => item.RoomID == RoomId).ToList();
                        RatePlanFilter = OmnibeesResponse.RoomStaysType.RoomStays[0].RatePlans.Where(item => item.RatePlanID == RatePlanId).ToList();
                        RoomRateFilter = OmnibeesResponse.RoomStaysType.RoomStays[0].RoomRates.Where(item => item.RoomID == RoomId && item.RatePlanID == RatePlanId && item.Total.AmountIncludingMarkup == AmountIncludingMarkup && item.Total.AmountIsPackage == AmountIsPackage && item.RoomStayCandidateRPH == RoomStayCandidateRPH).ToList();

                        string RatePlanJ = Newtonsoft.Json.JsonConvert.SerializeObject(RatePlanFilter);
                        RatePlanJsonJ = Newtonsoft.Json.JsonConvert.DeserializeObject<List<OmnibeesBookRQ.RatePlan>>(RatePlanJ);

                        string RoomJ = Newtonsoft.Json.JsonConvert.SerializeObject(RoomFilter);
                        RoomTypeJson = Newtonsoft.Json.JsonConvert.DeserializeObject<List<OmnibeesBookRQ.RoomType>>(RoomJ);

                        string RoomRateF = Newtonsoft.Json.JsonConvert.SerializeObject(RoomRateFilter);
                        RoomRateJson = Newtonsoft.Json.JsonConvert.DeserializeObject<List<OmnibeesBookRQ.RoomRate>>(RoomRateF);

                        string RoomType = Newtonsoft.Json.JsonConvert.SerializeObject(RoomFilter);
                        OmnibeesBookRQ.RoomStay roomStay = new OmnibeesBookRQ.RoomStay();
                        roomStay.AvailabilityStatus = null;
                        #region BasicPropertyInf
                        OmnibeesBookRQ.HotelRef hotelRef = new OmnibeesBookRQ.HotelRef();
                        hotelRef.BrandName = null;
                        hotelRef.ChainCode = null;
                        hotelRef.ChainName = null;
                        hotelRef.HotelCode = Convert.ToInt32(xmlhotelid);
                        hotelRef.HotelName = null;
                        OmnibeesBookRQ.BasicPropertyInfo basicPropertyInfo = new OmnibeesBookRQ.BasicPropertyInfo();
                        basicPropertyInfo.Address = null;
                        basicPropertyInfo.Award = null;
                        basicPropertyInfo.ContactNumbersType = null;
                        basicPropertyInfo.CurrencyCode = null;
                        basicPropertyInfo.HotelAmenities = null;
                        basicPropertyInfo.HotelRef = hotelRef;
                        basicPropertyInfo.ImageURL = null;
                        basicPropertyInfo.Policy = null;
                        basicPropertyInfo.Position = null;
                        basicPropertyInfo.VendorMessagesType = null;
                        #endregion
                        roomStay.BasicPropertyInfo = basicPropertyInfo;
                        #region CommentsType
                        List<OmnibeesBookRQ.Comment> lstcomment = new List<OmnibeesBookRQ.Comment>();
                        OmnibeesBookRQ.Comment comment = new OmnibeesBookRQ.Comment();
                        comment.Description = "Our internal reservation number " + referenceNumber;
                        comment.Language = null;
                        comment.Language = null;
                        lstcomment.Add(comment);
                        OmnibeesBookRQ.CommentsType commentsTypes = new OmnibeesBookRQ.CommentsType();
                        commentsTypes.Comments = lstcomment;
                        #endregion
                        roomStay.CommentsType = commentsTypes;
                        roomStay.Guarantees = null;
                        #region GuestCountsType

                        adultcount = Convert.ToInt32(bookRequest.userCart.UserCartDetails[j].NoofAdult);
                        childage1 = Convert.ToInt32(bookRequest.userCart.UserCartDetails[j].ChildAge);
                        //childage2 = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[j]["ChildAge2"]);
                        NoOfChild = Convert.ToInt32(bookRequest.userCart.UserCartDetails[j].NoofChild);

                        OmnibeesBookRQ.GuestCountsType guestCountsType = new OmnibeesBookRQ.GuestCountsType();
                        List<OmnibeesBookRQ.GuestCount> lstGuestCount = new List<OmnibeesBookRQ.GuestCount>();
                        OmnibeesBookRQ.GuestCount guestCount = new OmnibeesBookRQ.GuestCount();
                        guestCount.Age = null;
                        guestCount.AgeQualifyCode = 10;
                        guestCount.Count = adultcount;
                        List<int> lstResGuestRPH = new List<int>();
                        for (int A = 0; A < adultcount; A++)
                        {
                            RPHCount = RPHCount + 1;
                            lstResGuestRPH.Add(RPHCount);
                        }
                        //guestCount.ResGuestRPH = lstResGuestRPH;
                        //lstGuestCount.Add(guestCount);
                        guestCount.ResGuestRPH = lstResGuestRPH;
                        lstGuestCount.Add(guestCount);

                        if (NoOfChild > 0)
                        {
                            if (NoOfChild == 1)
                            {
                                RPHCount = RPHCount + 1;
                                OmnibeesBookRQ.GuestCount Child1GuestCount_obj = new OmnibeesBookRQ.GuestCount();
                                Child1GuestCount_obj.Age = childage1;
                                Child1GuestCount_obj.AgeQualifyCode = 8;
                                Child1GuestCount_obj.Count = 1;

                                List<int> lstResGuestRPHChild1 = new List<int>();
                                lstResGuestRPHChild1.Add(RPHCount);
                                Child1GuestCount_obj.ResGuestRPH = lstResGuestRPHChild1;
                                lstGuestCount.Add(Child1GuestCount_obj);
                            }
                            else
                            {
                                RPHCount = RPHCount + 1;
                                OmnibeesBookRQ.GuestCount Child1GuestCount_obj = new OmnibeesBookRQ.GuestCount();
                                Child1GuestCount_obj.Age = childage1;
                                Child1GuestCount_obj.AgeQualifyCode = 8;
                                Child1GuestCount_obj.Count = 1;

                                List<int> lstResGuestRPHChild1 = new List<int>();
                                lstResGuestRPHChild1.Add(RPHCount);
                                Child1GuestCount_obj.ResGuestRPH = lstResGuestRPHChild1;

                                //guestCount.ResGuestRPH = lstResGuestRPHChild1;
                                lstGuestCount.Add(Child1GuestCount_obj);

                                RPHCount = RPHCount + 1;
                                OmnibeesBookRQ.GuestCount Child2GuestCount_obj = new OmnibeesBookRQ.GuestCount();
                                Child2GuestCount_obj.Age = childage2;
                                Child2GuestCount_obj.AgeQualifyCode = 8;
                                Child2GuestCount_obj.Count = 1;

                                List<int> lstResGuestRPHChild2 = new List<int>();
                                lstResGuestRPHChild2.Add(RPHCount);
                                //Child1GuestCount_obj.ResGuestRPH = lstResGuestRPHChild2;
                                Child2GuestCount_obj.ResGuestRPH = lstResGuestRPHChild2;//Fixed on 2-APR-2025 K2

                                //guestCount.ResGuestRPH = lstResGuestRPHChild2;
                                lstGuestCount.Add(Child2GuestCount_obj);
                            }
                        }

                        guestCountsType.GuestCounts = lstGuestCount;
                        #endregion
                        roomStay.GuestCountsType = guestCountsType;
                        roomStay.IndexNumber = null;
                        roomStay.RPH = 0;
                        //roomStay.RatePlans = RatePlanFilter;

                        roomStay.RatePlans = RatePlanJsonJ;
                        roomStay.RoomRates = RoomRateJson;
                        roomStay.RoomTypes = RoomTypeJson;

                        roomStay.TPA_Extensions = null;
                        OmnibeesBookRQ.Total Total = new OmnibeesBookRQ.Total();
                        Total.AmountAfterTax = AmountAfterTaxForSingelRoom;
                        Total.AmountBeforeTax = AmountBeforeTaxForSingelRoom;
                        //Total.AmountIncludingMarkup = false;
                        Total.AmountIncludingMarkup = AmountIncludingMarkup;
                        Total.AmountIsPackage = AmountIsPackage;
                        Total.ChargeType = 12;
                        Total.CurrencyCode = CurrencyCode;
                        roomStay.Total = Total;
                        roomStay.RoomStayLanguage = 1;
                        lstRoomStays.Add(roomStay);

                        roomStaysType.RoomStays = lstRoomStays;
                        roomStaysType.MoreIndicator = false;
                        // roomStaysType.RoomStays = ;
                        #endregion
                    }
                    OmnibeesBookRQ.HotelReservation hotelReservation = new OmnibeesBookRQ.HotelReservation();
                    hotelReservation.TPA_Extensions = TPAExtensions;
                    hotelReservation.RoomStaysType = roomStaysType;
                    hotelReservation.CreateDateTime = Convert.ToDateTime(DateTime.Now);
                    hotelReservation.LastModifyDateTime = Convert.ToDateTime(DateTime.Now);
                    hotelReservation.POS = POS;
                    hotelReservation.ResGlobalInfo = resGlobalInfo;
                    hotelReservation.ResGuestsType = resGuestsType;
                    hotelReservation.ResStatus = resStatus;
                    ListhotelReservation.Add(hotelReservation);

                    OmnibeesBookRQ.HotelReservationsType hotelReservationsType = new OmnibeesBookRQ.HotelReservationsType();
                    hotelReservationsType.HotelReservations = ListhotelReservation;
                    #endregion
                    OmnibeesBookRQ.Request BookingRQ = new OmnibeesBookRQ.Request();
                    BookingRQ.EchoToken = "technoheavenpull_" + Guid.NewGuid().ToString();
                    BookingRQ.HotelReservationsType = hotelReservationsType;
                    //BookingRQ.PrimaryLangID = 1;
                    BookingRQ.ResStatus = null;
                    BookingRQ.RetransmissionIndicator = null;
                    BookingRQ.TimeStamp = Convert.ToDateTime(DateTime.UtcNow);
                    BookingRQ.Target = 0;
                    BookingRQ.Version = Convert.ToDouble(version);

                    string BookingRequest = Newtonsoft.Json.JsonConvert.SerializeObject(BookingRQ);
                    return BookingRequest;
                }
            }
            catch (Exception ex)
            {
                errorinrq = ex.Message;
                _ = _exception.ErrorBooking(ex, "");
            }
            return string.Empty;
        }

        public string GetOmnibeesResponse(ref string Error, string request, string TravcoURL, Int32 TimeOut, string UserName, string Password, string Provider = "", HotelSearchParameter para = null, bool DebugMode = false)
        {
            System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
            timer.Start();
            string response = null;
            HttpWebRequest httpWebRequest = null;
            HttpWebResponse httpWebResponse = null;
            httpWebRequest = (HttpWebRequest)WebRequest.Create(TravcoURL);

            try
            {
                byte[] bytes;
                bytes = System.Text.Encoding.UTF8.GetBytes(request);

                httpWebRequest.Method = "POST";
                httpWebRequest.Timeout = TimeOut;
                httpWebRequest.ContentLength = bytes.Length;
                ServicePointManager.DefaultConnectionLimit = 1000;
                httpWebRequest.ContentType = "application/x-www-form-urlencoded";
                if (Provider.ToLower() == "derbysoft")
                {
                    httpWebRequest.Headers.Add("Authorization", _supplierConfig.Omnibees.DerbySoftAuthorization);
                    httpWebRequest.Accept = "application/json";
                    httpWebRequest.Method = "POST";
                    httpWebRequest.ContentType = "application/json";
                    httpWebRequest.Headers.Add("Accept-Encoding", "gzip");
                    httpWebRequest.KeepAlive = true;
                    ServicePointManager.Expect100Continue = true;
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                }
                if (Provider.ToLower() == "omnibees")
                {
                    httpWebRequest.Method = "POST";
                    httpWebRequest.ContentType = "application/json";
                    httpWebRequest.Accept = "application/json";
                    httpWebRequest.Headers.Add("UserName", UserName);
                    httpWebRequest.Headers.Add("Password", Password);
                    ServicePointManager.Expect100Continue = true;
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                }
                using (Stream requestStream = httpWebRequest.GetRequestStream())
                {
                    requestStream.Write(bytes, 0, bytes.Length);
                    requestStream.Close();//Close stream
                }
                httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                if (httpWebResponse.StatusCode == HttpStatusCode.OK)
                {
                    using (Stream responseStream = httpWebResponse.GetResponseStream())
                    {
                        Stream st = httpWebResponse.GetResponseStream();
                        if (httpWebResponse.ContentEncoding.ToLower().Contains("gzip"))
                            st = new System.IO.Compression.GZipStream(st, System.IO.Compression.CompressionMode.Decompress);
                        else if (httpWebResponse.ContentEncoding.ToLower().Contains("deflate"))
                            st = new System.IO.Compression.DeflateStream(st, System.IO.Compression.CompressionMode.Decompress);
                        StreamReader stIn = new StreamReader(st, Encoding.Default);
                        response = stIn.ReadToEnd();
                    }
                    timer.Stop();
                    if (para != null)
                    {
                        para.SupplierTime += ",SupplierName :" + Provider + " : " + "Execution Time :" + timer.Elapsed.ToString();
                        if (DebugMode)
                            _ = _commonMethod.XMLRequestResponseTrack(request, response, Provider, "Proper");
                    }
                    if (Convert.ToString(_keySettings.IsLogTrack) == "1")
                    {
                        TimeSpan timeTaken = timer.Elapsed;
                        //XMLHotelBookingCommon.ErrorLog(Convert.ToString(System.Web.Configuration.WebConfigurationManager.AppSettings["LogTrackPath"]), "Trvaco->" + Provider + "-> Execution Time : " + timer.Elapsed.ToString());
                        _ = _commonMethod.XMLRequestResponseTrack(request, response, Provider == "" ? "Travco" : Provider, "Proper");
                    }
                }
            }
            catch (WebException we)
            {
                HttpWebResponse httpResponse = (HttpWebResponse)we.Response;
                string result = string.Empty;
                string result1 = string.Empty;
                if (httpResponse != null && httpResponse.StatusDescription != null)
                {
                    result = httpResponse.StatusDescription.ToString();
                    result1 = new StreamReader(we.Response.GetResponseStream()).ReadToEnd();
                }
                Error = result1;
                _ = _commonMethod.XMLRequestResponseTrackError(request, result1, Provider == "" ? "Travco" : Provider, "Proper");
                return result1;
            }
            catch (Exception ex)
            {
                Error = ex.Message;
                //response = ex.Message + ex.InnerException;
                _ = _commonMethod.XMLRequestResponseTrack(request, ex.Message, Provider == "" ? "Travco" : Provider, "Proper");
                //ExceptionManager.LogException(ex);
                return ex.Message;
            }
            finally
            {
                if (httpWebResponse != null)
                {
                    httpWebResponse.Close();
                    httpWebResponse = null;
                    httpWebRequest = null;
                }
            }
            return response;
        }


        public string Get_Omnibees_BookingCancelRQ(string AgentVoucherNo, DataRow[] SupplierDetails, string RoomTypeId, string CheckIn, string CheckOut)
        {
            string jsonString = "";
            try
            {

                //CheckIn = CheckIn.Substring(4, 2) + "/" + CheckIn.Substring(6, 2) + "/" + CheckIn.Substring(0, 4);
                //CheckOut = CheckOut.Substring(4, 2) + "/" + CheckOut.Substring(6, 2) + "/" + CheckOut.Substring(0, 4);

                //CheckIn = CheckIn.Substring(0, 4) + "-" + CheckIn.Substring(4, 2) + "-" + CheckIn.Substring(6, 2);
                //CheckOut = CheckOut.Substring(0, 4) + "-" + CheckOut.Substring(4, 2) + "-" + CheckOut.Substring(6, 2);

                string version = "2.7";
                string[] AgentVoucherNoVal = AgentVoucherNo.Split('@');
                if (AgentVoucherNoVal.Length > 0)
                {
                    string UniqueID = Convert.ToString(AgentVoucherNoVal[0]);
                    string HotelCode = Convert.ToString(AgentVoucherNoVal[1]);
                    string TransactionIdentifier = Convert.ToString(AgentVoucherNoVal[2]);

                    OmnibeesCancelRQ.CancelRQ cancelRequest = new OmnibeesCancelRQ.CancelRQ();

                    cancelRequest.CancelType = 98;
                    cancelRequest.EchoToken = "technoheavenpull_" + Guid.NewGuid().ToString();
                    cancelRequest.PrimaryLangID = 1;

                    List<OmnibeesCancelRQ.Segment> segment = new List<OmnibeesCancelRQ.Segment>();

                    OmnibeesCancelRQ.Segment lstsegment = new OmnibeesCancelRQ.Segment();
                    lstsegment.ItinSegNbr = UniqueID;
                    lstsegment.Status = 0;
                    segment.Add(lstsegment);

                    cancelRequest.Segment = segment;

                    cancelRequest.Target = Convert.ToInt32(_supplierConfig.Omnibees.Target);

                    cancelRequest.TimeStamp = Convert.ToDateTime(DateTime.Now); //Convert.ToDateTime("2021-03-19T06:00:37.2687519+00:00");
                    cancelRequest.Version = Convert.ToDouble(version);

                    cancelRequest.TransactionIdentifier = TransactionIdentifier;

                    List<OmnibeesCancelRQ.UniqueID> ReferanceNumber = new List<OmnibeesCancelRQ.UniqueID>();

                    OmnibeesCancelRQ.UniqueID lstReferanceNumber = new OmnibeesCancelRQ.UniqueID();

                    string[] UniqueIDArry = UniqueID.Split('/');
                    lstReferanceNumber.ID = UniqueIDArry[0];
                    lstReferanceNumber.Type = 14;

                    ReferanceNumber.Add(lstReferanceNumber);
                    cancelRequest.UniqueID = ReferanceNumber;

                    OmnibeesCancelRQ.Verification Verification = new OmnibeesCancelRQ.Verification();

                    OmnibeesCancelRQ.HotelRef HotelRef = new OmnibeesCancelRQ.HotelRef();
                    HotelRef.HotelCode = Convert.ToInt32(HotelCode);
                    Verification.HotelRef = HotelRef;

                    OmnibeesCancelRQ.ReservationTimeSpan ReservationTimeSpan = new OmnibeesCancelRQ.ReservationTimeSpan();

                    ReservationTimeSpan.End = Convert.ToDateTime(CheckOut);
                    ReservationTimeSpan.Start = Convert.ToDateTime(CheckIn);
                    Verification.ReservationTimeSpan = ReservationTimeSpan;

                    cancelRequest.Verification = Verification;
                    jsonString = Newtonsoft.Json.JsonConvert.SerializeObject(cancelRequest);
                }
                return jsonString;
            }
            catch (Exception ex)
            {
                _ = _exception.ErrorNew(ex, ErrorLog._LogAPI.CancelBooking);
                return "";
            }

        }
    }
}
