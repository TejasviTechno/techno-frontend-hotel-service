using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using XConnect.CommonUtilities;
using XConnect.CommonUtilities.Interface;
using XConnect.ErrorLogHandler.Interface;
using XConnect.Markup.Interface;
using XConnect.Models;
using XConnect.Models.Hotel;
using XConnect.Supplier.HotelsTrack.Interface;
using XConnect.Supplier.Rezlive;
using static Azure.Core.HttpHeader;
using static XConnect.Supplier.Booking.Com.preebookrs;
using Options = XConnect.Models.Options;

namespace XConnect.Supplier.HotelsTrack
{
    public class HotelsTrackRepo : IHotelsTrack
    {
        private readonly ICommonMethod _commonMethod;
        private readonly IException _exception;
        private readonly IMarkup _markup;
        private readonly ISupplierRes _supplierRes;
        private readonly KeySettings _keySettings;



        public HotelsTrackRepo(ICommonMethod commonMethod, IMarkup markup, ISupplierRes supplierRes, IOptions<KeySettings> keySettings, IException exception)
        {
            _commonMethod = commonMethod;

            _markup = markup;
            _supplierRes = supplierRes;
            _keySettings = keySettings.Value;
            _exception = exception;
        }
        public void GetHotelsTrackRequestOnly_async(DataRow[] commonCode, List<StarRating> listRating, bool HitFromAll, HotelSearchParameter SearchPara, List<SupplierDetails> lstHotelsTrackDetails, List<SupplierSearchConfig> lstHotelsTrackSearchConfig, DataTable dtPreferredDataHotel, ref ConcurrentBag<HotelX> AllTask, int Hit, TokenData tokenData, DataSet dtXMLCity)
        {
            try
            {

                List<StarRating> lstRating = new List<StarRating>();
                lstRating = _commonMethod.GetStarRatingStringList_xtg(SearchPara);

                List<HotelCodeByCity> DtHotelDAta = HtmlExtensions.ToList<HotelCodeByCity>(commonCode.CopyToDataTable());
                int ratingcount = lstRating.Count;
                int hotelcount = DtHotelDAta.Count;

                foreach (SupplierDetails HotelsTrackDetails in lstHotelsTrackDetails)
                {
                    try
                    {
                        int SupplierID = HotelsTrackDetails.SupplierID;
                        string Supplier = HotelsTrackDetails.SupplierName;

                        if (lstHotelsTrackSearchConfig.Count > 0)
                        {
                            List<SupplierSearchConfig> HotelsTrackSearchConfig = lstHotelsTrackSearchConfig
                                   .Where(x => x != null && x.SupplierID == SupplierID)
                                   .ToList();

                            if (HotelsTrackSearchConfig != null && HotelsTrackSearchConfig.Count > 0)
                            {
                                DataForSearch dataForSearch = _commonMethod.GetXmlDataForSearchTechwise(SearchPara.Nationality, dtXMLCity, Supplier, HotelsTrackDetails.TechName);
                                if (dataForSearch.XmlNationality != "")
                                {
                                    string Ids = string.Empty;
                                    string xmlbase = "";
                                    _commonMethod.GetPrefferedHotelCodes(ref Ids, ref xmlbase, ref HotelsTrackSearchConfig, Supplier, dtPreferredDataHotel, SearchPara.IsSearchFor);

                                    List<HotelCodeByCity> filter = new List<HotelCodeByCity>();
                                    filter = _commonMethod.FilterWithOutCity_Code(ratingcount, hotelcount, DtHotelDAta, lstRating, Supplier);


                                    string HotelsTrackCurrency = Convert.ToString(HotelsTrackDetails.CurrencyShortName);
                                    string Sitecurrency = tokenData.SiteCurrency;

                                    double CurrencyFactorHotelsTrack = 1;

                                    if (HotelsTrackCurrency.ToLower() != Sitecurrency.ToLower()) // change systembase currency 
                                    {
                                        CurrencyFactorHotelsTrack = _commonMethod.GetROEByCurrency(HotelsTrackCurrency, tokenData.dtROE);
                                    }

                                    for (int k = 0; k < HotelsTrackSearchConfig.Count; k++)
                                    {
                                        bool DebugMode = (Convert.ToInt32(HotelsTrackSearchConfig[k].DebugMode) == 0 ? false : true);

                                        string version = Convert.ToString(HotelsTrackDetails.version);

                                        if (Convert.ToInt32(HotelsTrackSearchConfig[k].CityID) == 0 || Convert.ToInt32(HotelsTrackSearchConfig[k].CityID) == SearchPara.CityID)//All City means 0
                                        {
                                            if (filter.Count > 0)
                                            {
                                                int NoOfRecords = Convert.ToInt32(HotelsTrackSearchConfig[k].Noofrecords);
                                                double count = Convert.ToDouble(filter.Count);
                                                int NoOfDiv = Convert.ToInt32(Math.Ceiling(count / Convert.ToDouble(NoOfRecords)));
                                                List<List<XMLID_Search>> lstxml = _commonMethod.GetXMLIDString(NoOfDiv, NoOfRecords, count, filter);

                                                for (int a = 0; a < NoOfDiv; a++)
                                                {
                                                    string IdsForSearch = Convert.ToString(_commonMethod.GetHotelCodeByCOmma(lstxml[a]));


                                                    HotelX xhotel = new HotelX();
                                                    xhotel.UserId = Convert.ToString(HotelsTrackDetails.UserID);
                                                    xhotel.UserName = Convert.ToString(HotelsTrackDetails.UserName);
                                                    xhotel.PassWord = Convert.ToString(HotelsTrackDetails.Password);

                                                    xhotel.Supplier = Supplier;
                                                    xhotel.SearchTimeOut = Convert.ToInt32(HotelsTrackSearchConfig[k].HotelSearchTimeOut);
                                                    //if (Hit > 0)
                                                    xhotel.ProcessTimeout = Convert.ToInt32(HotelsTrackSearchConfig[k].HotelSearchTimeOut2);
                                                    if (_keySettings.SupplierProxy == "0")
                                                        xhotel.URL = Convert.ToString(HotelsTrackDetails.WebSiteURL) + HSStaticInfo.SearchHotelsTrack;
                                                    else
                                                        xhotel.URL = Convert.ToString(HotelsTrackDetails.WebSiteURLforAPI) + HSStaticInfo.SearchHotelsTrack;
                                                    xhotel.CurrencyCode = HotelsTrackCurrency;
                                                    xhotel.CurrencyFactor = CurrencyFactorHotelsTrack;
                                                    xhotel.XmlNationality = dataForSearch.XmlNationality;
                                                    xhotel.XmlCity = dataForSearch.xmlCity;
                                                    //xhotel.XmlCity = lst[gt].XMLBaseID.Split('#')[1];
                                                    //xhotel.XmlCity = lst[gt].XMLBaseID.Split('#')[0];
                                                    xhotel.XmlCountry = dataForSearch.xmlCountry;
                                                    xhotel.DebugeMode = DebugMode;
                                                    xhotel.AgentWiseMarkup = HotelsTrackDetails.AgentWiseMarkup;
                                                    xhotel.IsShowSupplier = Convert.ToInt16(HotelsTrackDetails.IsShowSupplier) == 1 ? true : false;
                                                    xhotel.SupplierID = HotelsTrackDetails.SupplierID;
                                                    xhotel.MasterSupplierID = HotelsTrackDetails.MasterSupplierId;
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
                                                        //objrq.Request = "";
                                                        objrq.Request = "requestXML=" + HotelsTrackRequestOnly_Async(xhotel.URL, xhotel.UserId, xhotel.UserName, xhotel.PassWord, xhotel.XmlCity, xhotel.XmlCountry, xhotel.XmlNationality, xhotel.CurrencyCode, xhotel.CurrencyFactor, xhotel.DebugeMode, IdsForSearch, SearchPara, version);
                                                        objrq.Response = "";
                                                        RQRS.Add(objrq);
                                                    }
                                                    xhotel.RQRS = RQRS;
                                                    #endregion
                                                    // xhotel.Request = "XML=" + Hotel.XMLRezliveRequestOnly_async(Convert.ToString(RezliveDetails[0].WebSiteURL), Convert.ToString(RezliveDetails[0].UserID), Convert.ToString(RezliveDetails[0].UserName), Convert.ToString(RezliveDetails[0].Password), SearchPara, RezliveCurrency, CurrencyFactorRezlive, IdsForSearch, DebugMode, xhotel.XmlCity, CountryCode, NationalityCode, 0);
                                                    xhotel.TechName = HotelsTrackDetails.TechName;
                                                    xhotel.ReturnRooms = HotelsTrackDetails.ReturnRooms;

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
                        _ = _exception.Error(ex);
                    }
                }
            }
            catch (Exception ex)
            {
                _ = _exception.Error(ex);
            }
        }

        public string HotelsTrackRequestOnly_Async(string Url, string UserId, string UserName, string Password, string xmlCity, string XmlCountry, string XmlNationality, string Currency, double CurrencyFactor, bool Debug, string HotelId, HotelSearchParameter SearchPara, string version = "")
        {
            try
            {
                string HotelsTrackPriceOnly = HotelsTrackPriceOnlyRequest(SearchPara, UserId, UserName, Password, Currency, CurrencyFactor, Debug, xmlCity, XmlCountry, XmlNationality, HotelId, version);
                return HotelsTrackPriceOnly;
            }
            catch (Exception ex)
            {
                _ = _exception.Error(ex);
                return "";
            }
        }

        public string HotelsTrackPriceOnlyRequest(HotelSearchParameter SearchPara, string UserId, string UserName, string Password, string Currency, double CurrencyFactor, bool DebugMode, string City, string Country, string Nationality, string HotelIds, string version = "")
        {
            try
            {
                #region Price Only
                string CheckIn = SearchPara.CheckInDate;
                CheckIn = CheckIn.Substring(6, 4) + "-" + CheckIn.Substring(3, 2) + "-" + CheckIn.Substring(0, 2);
                string CheckOut = SearchPara.CheckOutDate;
                CheckOut = CheckOut.Substring(6, 4) + "-" + CheckOut.Substring(3, 2) + "-" + CheckOut.Substring(0, 2);
                StringBuilder HotelsTrackRQ = new StringBuilder();
                HotelsTrackRQ.Append("<Service_SearchHotel>");
                HotelsTrackRQ.Append("<AgentLogin><AgentId>" + UserId + "</AgentId><LoginName>" + UserName + "</LoginName><Password>" + Password + "</Password></AgentLogin>");
                HotelsTrackRQ.Append("<SearchHotel_Request>");
                HotelsTrackRQ.Append("<PaxPassport>" + Nationality + "</PaxPassport>");
                // HotelsTrackRQ.Append("<RPCurrency>" + XMLHotelBookingCommon.HotelsTrackCurrency + "</RPCurrency>");
                HotelsTrackRQ.Append("<DestCountry>" + Country + "</DestCountry>");
                HotelsTrackRQ.Append("<DestCity>" + City + "</DestCity>");
                string HID = "";
                if (HotelIds.Trim() != string.Empty)
                {
                    string[] HotelId = Convert.ToString(HotelIds).Split(',');
                    StringBuilder ItemCodeString = new StringBuilder();
                    for (int htid = 0; htid < HotelId.Length; htid++)
                    {
                        if (htid == 0)
                            HID = HotelId[htid];
                        else
                            HID = HID + "," + HotelId[htid];
                    }
                }
                //HotelsTrackRQ.Append("<HotelId>" + HID + "</HotelId>");
                HotelsTrackRQ.Append("<HotelId InternalCode=\"" + version + "\">" + HID + "</HotelId>");
                HotelsTrackRQ.Append("<ServiceCode></ServiceCode>");

                HotelsTrackRQ.Append("<Period CheckIn=\"" + CheckIn + "\" CheckOut=\"" + CheckOut + "\"/>");

                for (int ser = 0; ser < SearchPara.Rooms.Count; ser++)
                {
                    int NoOfAdult = 0;
                    NoOfAdult = SearchPara.Rooms[ser].NoofAdults;
                    int NoOfChild = SearchPara.Rooms[ser].NoofChild;
                    NoOfAdult = NoOfAdult + NoOfChild;
                    string RQBedchild = "N";

                    string RoomType = "";
                    RoomType = Get_HotelsTrack_RoomType(NoOfAdult);

                    //if (NoOfChild != 0)
                    //{
                    //    if (NoOfAdult == 2)
                    //    {
                    //        int childage1 = Convert.ToInt32(SearchPara.Rooms[ser].Child1Age);
                    //        int childage2 = Convert.ToInt32(SearchPara.Rooms[ser].Child2Age);
                    //        if (childage1 > 2 || childage2 > 2)
                    //        {
                    //            RoomType = "Triple";
                    //            RQBedchild = "Y";
                    //        }
                    //        else
                    //        {
                    //            RoomType = "Double";
                    //        }
                    //    }
                    //}
                    HotelsTrackRQ.Append("<RoomInfo>");
                    HotelsTrackRQ.Append("<AdultNum RoomType=\"" + RoomType + "\" RQBedChild=\"" + RQBedchild + "\">" + NoOfAdult + "</AdultNum>");
                    //NoOfChild = SearchPara.Rooms[ser].NoofChild;
                    //if (NoOfChild != 0)
                    //{
                    //    HotelsTrackRQ.Append("<ChildAges>");

                    //    if (NoOfChild == 1)
                    //        HotelsTrackRQ.Append("<ChildAge>" + SearchPara.Rooms[ser].Child1Age + "</ChildAge>");
                    //    if (NoOfChild == 2)
                    //    {
                    //        HotelsTrackRQ.Append("<ChildAge>" + SearchPara.Rooms[ser].Child1Age + "</ChildAge>");
                    //        HotelsTrackRQ.Append("<ChildAge>" + SearchPara.Rooms[ser].Child2Age + "</ChildAge>");
                    //    }

                    //    HotelsTrackRQ.Append("</ChildAges>");
                    //}
                    HotelsTrackRQ.Append("</RoomInfo>");
                }
                HotelsTrackRQ.Append("<flagAvail>Y</flagAvail>");
                HotelsTrackRQ.Append("</SearchHotel_Request>");
                HotelsTrackRQ.Append("</Service_SearchHotel>");
                return HotelsTrackRQ.ToString();
                #endregion
            }
            catch (Exception ex)
            {
                _ = _exception.Error(ex);
                return "";
            }
        }

        public string Get_HotelsTrack_RoomType(int NoOfAdult)
        {
            try
            {
                string RoomType = "";
                switch (NoOfAdult)
                {

                    case 1:
                        RoomType = "Single"; break;

                    case 2:
                        RoomType = "Double"; break;

                    case 3:
                        RoomType = "Triple"; break;

                    case 4:
                        RoomType = "Quad"; break;

                    default: RoomType = ""; break;
                }
                return RoomType;
            }
            catch (Exception ex)
            {
                _ = _exception.Error(ex);
                return "";
            }
        }

        public async Task<List<Options>> HotelsTrack_HotelSearchWithRooms(HotelX item, string giitaCode, double AgentPercentage, CommonSearch commonSearch, string currency, string ResponseData, string CityCode, HotelSearchParameter SearchParam, string ContryCode, string XMLNationality, double CurrencyFactorRezlive, string Provider, DataSet dsAgentDetails, DataTable dtCountryCityMarkup, Int32 AgentId, Int32 TariffID, int SupplierID, Int32 processTimeout, string ResProccessTimerLogActive, string ResProccessTimeValueforSaveLog)
        {
            List<Options> options = new List<Options>();
            try
            {
                #region DataTable
                DataTable DtHotel = _commonMethod.GenerateDataTableNewV2();
                #endregion
                if (ResponseData != string.Empty)
                {
                    #region PriceOnly RS
                    if (ResponseData == string.Empty)
                    {
                        item.ResponseType = "NoCount";
                        return options;
                    }
                    DataSet HotelsTrackResponse = _commonMethod.GetDataSetFromXMLString(ResponseData);
                    if (HotelsTrackResponse == null || HotelsTrackResponse.Tables["Hotel"] == null)
                    {
                        item.ResponseType = "NoCount";
                        return options;
                    }
                    #endregion


                    #region Hotel List
                    string InternalCode = string.Empty;
                    string newsupplier = string.Empty;
                    string haskey = Convert.ToString(HotelsTrackResponse.Tables["SearchHotel_Response"].Rows[0]["hasKey"]);
                    if (HotelsTrackResponse.Tables["hotel"].Rows.Count < 1)
                    {
                        item.ResponseType = "NoCount";
                        return options;
                    }
                    for (int i = 0; i < HotelsTrackResponse.Tables["hotel"].Rows.Count; i++)
                    {
                        item.ResponseType = "Success";

                        string Suppliername = string.Empty;
                        double FinalMinPrice = 0;
                        bool IsAvail = Convert.ToBoolean(HotelsTrackResponse.Tables["Hotel"].Rows[i]["avail"]);
                        if (IsAvail)
                        {

                            string PropertyID = Convert.ToString(HotelsTrackResponse.Tables["Hotel"].Rows[i]["Hotel_Id"]);
                            InternalCode = Convert.ToString(HotelsTrackResponse.Tables["Hotel"].Rows[i]["InternalCode"]);
                            string HotelCode = Convert.ToString(HotelsTrackResponse.Tables["Hotel"].Rows[i]["HotelId"]);
                            string CancelPolicyId = Convert.ToString(HotelsTrackResponse.Tables["Hotel"].Rows[i]["CancelPolicyId"]);

                            string Currency = Convert.ToString(HotelsTrackResponse.Tables["Hotel"].Rows[i]["Currency"]);
                            double CurrencyFactor = 1;
                            if (Currency.ToLower() != commonSearch.tokenData.SiteCurrency.ToLower())
                            {
                                //DataTable dtInrCurrency = Common.GetCountryCurrency(Currency);
                                //CurrencyFactor = Convert.ToDouble(dtInrCurrency.Rows[0]["AmtToExch"]);

                                CurrencyFactor = _commonMethod.GetROEByCurrency(Currency, commonSearch.tokenData.dtROE);
                            }
                            if (SearchParam.Filters.IsShowRooms == 0)
                            {






                                DataRow[] drRoomCateg = HotelsTrackResponse.Tables["RoomCateg"].Select("Hotel_Id='" + PropertyID + "'");
                                List<RoomData> lstprice = new List<RoomData>();

                                for (int k = 0; k < drRoomCateg.Length; k++)
                                {
                                    //Contract = Convert.ToInt32(drMealPlan[k]["Contract"]);
                                    double Price = Math.Round(Convert.ToDouble(drRoomCateg[k]["NetPrice"]), 2);
                                    RoomData data = new RoomData();
                                    data.FinalSellingPrice = Price;
                                    lstprice.Add(data);
                                }



                                //double HotelPrice = 0;
                                double HotelPrice = Convert.ToDouble(lstprice.Min(x => x.FinalSellingPrice));
                                FinalMinPrice = HotelPrice * CurrencyFactor;

                                if (FinalMinPrice == 0)
                                {
                                    item.ResponseType = "NoCount";
                                    continue;
                                }



                                DataRow drXmlHotel = DtHotel.NewRow();
                                drXmlHotel["IsRecomondedHotel"] = true;
                                drXmlHotel["ContractID"] = 0;
                                drXmlHotel["XMLHotelCode"] = Convert.ToString(HotelsTrackResponse.Tables["Hotel"].Rows[i]["HotelId"]);
                                drXmlHotel["UniqueNo"] = Convert.ToString(HotelsTrackResponse.Tables["Hotel"].Rows[i]["HotelId"]);
                                drXmlHotel["HotelName"] = "";
                                drXmlHotel["SupplierName"] = Provider;
                                drXmlHotel["SupplierCurrency"] = currency;
                                drXmlHotel["SupplierId"] = SupplierID;
                                drXmlHotel["GIITACode"] = giitaCode;
                                drXmlHotel["Rating"] = 0;
                                Int64 OwnsystemHotelId = 0;
                                drXmlHotel["VatdiffAmount"] = 0;

                                double Pricenew = FinalMinPrice;
                                drXmlHotel["FinalSellingPrice"] = Pricenew;
                                drXmlHotel["SellingPrice"] = Pricenew;

                                DtHotel.Rows.Add(drXmlHotel);
                                double TotalPrice = Pricenew;
                                if (TotalPrice > 0)
                                {
                                    #region "Room code Block"
                                    bool willAddOrNot = true;
                                    Int64 GiataId = 0;
                                    Int64 RaynaID = 0;
                                    int RatingMain = 0;
                                    //Int64 OwnsystemHotelId = 0;
                                    double price = TotalPrice;
                                    _markup.GetHotelDataCommon(ref price, ref willAddOrNot, ref OwnsystemHotelId, ref RaynaID, ref GiataId, ref RatingMain, ref drXmlHotel, commonSearch.AgentId, commonSearch.TariffID, commonSearch.dsAgentDetails, commonSearch.supplierGiataMaster, SearchParam, commonSearch.dtCountryCityMarkup, commonSearch.B2CMarkup, commonSearch.IsParentId, commonSearch.SubAgentMarkup, commonSearch.SearchByHotel, commonSearch.HotelIDS);
                                    if (willAddOrNot)
                                    {
                                        #region "Room Logic"
                                        DataTable dt_HotelRoom = _commonMethod.GenerateRoomtypeDataTableNewV2();
                                        DataTable dt_Cancellation = _commonMethod.GenerateCancellationDataTableNewV2();
                                        DataTable dt_RateBreackUp = _commonMethod.GenerateoomRateBreackUpDataTableNewV2();
                                        #endregion
                                        decimal LowestPrice = Convert.ToDecimal(price).CalculatePrice(commonSearch.tokenData, commonSearch.advancedOptions, currency);
                                        Options ops1 = (await _supplierRes.GetCommonRoomForSuppliers(Convert.ToInt64(GiataId), commonSearch.tokenData, commonSearch.advancedOptions, Convert.ToDouble(LowestPrice), drXmlHotel, dt_HotelRoom, dt_Cancellation, dt_RateBreackUp, SearchParam));
                                        if (ops1 != null)
                                            options.Add(ops1);
                                    }
                                    #endregion
                                }


                            }
                            else
                            {

                                DataRow drXmlHotel = DtHotel.NewRow();
                                drXmlHotel["ContractID"] = 0;
                                drXmlHotel["SupplierId"] = SupplierID;
                                drXmlHotel["HotelName"] = Convert.ToString(HotelsTrackResponse.Tables["Hotel"].Rows[i]["Hotel_Id"]);
                                drXmlHotel["GIITACode"] = giitaCode;
                                drXmlHotel["UniqueNo"] = Convert.ToString(HotelsTrackResponse.Tables["Hotel"].Rows[i]["HotelId"]);
                                drXmlHotel["Rating"] = 0;
                                drXmlHotel["IsRecomondedHotel"] = true;
                                drXmlHotel["VatdiffAmount"] = 0;
                                drXmlHotel["XMLHotelCode"] = Convert.ToString(HotelsTrackResponse.Tables["Hotel"].Rows[i]["HotelId"]);
                                drXmlHotel["SupplierName"] = Provider;
                                drXmlHotel["SupplierCurrency"] = currency;
                                double Pricenew = 1; // TotalPrice * CurrencyFactorRezlive;
                                drXmlHotel["FinalSellingPrice"] = Pricenew;
                                drXmlHotel["SellingPrice"] = Pricenew;

                                DtHotel.Rows.Add(drXmlHotel);
                                FinalMinPrice = Pricenew;
                                //if (FinalMinPrice > 0)
                                {
                                    #region "Room code Block"
                                    bool willAddOrNot = true;
                                    Int64 RaynaID = 0;
                                    Int64 GiataId = 0;
                                    int RatingHotel = 0;
                                    Int64 OwnsystemHotelId = 0;
                                    //string Provider = "fitruums";
                                    double MinPrice = 1;

                                    _markup.GetHotelDataCommon(ref MinPrice, ref willAddOrNot, ref OwnsystemHotelId, ref RaynaID, ref GiataId, ref RatingHotel, ref drXmlHotel, commonSearch.AgentId, commonSearch.TariffID, commonSearch.dsAgentDetails, commonSearch.supplierGiataMaster, SearchParam, commonSearch.dtCountryCityMarkup, commonSearch.B2CMarkup, commonSearch.IsParentId, commonSearch.SubAgentMarkup, commonSearch.SearchByHotel, commonSearch.HotelIDS);
                                    if (willAddOrNot)
                                    {
                                        #region "Room Logic"
                                        DataTable dtHotelsTrackRoom = _commonMethod.GenerateRoomtypeDataTableNewV2();
                                        DataTable dtHotelsTrackCancellationPolicy = _commonMethod.GenerateCancellationDataTableNewV2();
                                        DataTable dtHotelsTrackRateBreakup = _commonMethod.GenerateoomRateBreackUpDataTableNewV2();


                                        DataRow[] drsupplier = commonSearch.dsAgentDetails.Tables[1].Select("Version='" + InternalCode + "'");
                                        if (drsupplier.Length > 0)
                                        {
                                            Suppliername = Convert.ToString(drsupplier[0]["SupplierName"]);
                                        }
                                        else
                                        {
                                            continue;
                                        }

                                        int CutOffDays = XMLHotelBookingCommon.CutOffDays;
                                        CutOffDays = _commonMethod.GetCutOffDays(commonSearch.dtSuplierMarkup, Provider);

                                        AgentPercentage = 0.0;
                                        // Boolean blnIsShowSupplier = false;
                                        double AgentMarkupAmount = 0.0;
                                        Boolean isPerce = true;

                                        double AgentCompanyPercentage = Convert.ToDouble(SearchParam.AgentCompanyPercentage);
                                        bool IsB2C = Convert.ToBoolean(Convert.ToByte(SearchParam.IsB2BOrB2C));
                                        bool blnIsShowSupplier = false;

                                        _markup.getAgetMarkupSetting(Provider, GiataId.ToString(), commonSearch.dtSuplierMarkup, commonSearch.dtMarkupDetails, ref AgentPercentage, ref AgentMarkupAmount, ref blnIsShowSupplier, ref isPerce, commonSearch.AgentId, RatingHotel, SearchParam.CityID, SearchParam.CountryID.ToString(), SearchParam.CheckInDate);



                                        string Message = "";
                                        if (HotelsTrackResponse.Tables["Hotel"].Columns.Contains("Message"))
                                        {
                                            if (HotelsTrackResponse.Tables["Hotel"].Rows[i]["Message"] != null)
                                                Message = Convert.ToString(HotelsTrackResponse.Tables["Hotel"].Rows[i]["Message"]);
                                        }
                                        string InternalCode_PolicyId = "";
                                        if (CancelPolicyId != "")
                                        {
                                            InternalCode_PolicyId = InternalCode + "$$$" + CancelPolicyId;
                                        }

                                        DataRow[] drRoomCateg = HotelsTrackResponse.Tables["RoomCateg"].Select("Hotel_Id='" + PropertyID + "'");
                                        if (drRoomCateg.Length > 0)
                                        {
                                            for (int room = 0; room < drRoomCateg.Length; room++)
                                            {
                                                #region DataRaw and Variable Declaration
                                                string RoomCategId = Convert.ToString(drRoomCateg[room]["RoomCateg_Id"]);
                                                string RoomCodeMain = Convert.ToString(drRoomCateg[room]["Code"]);
                                                double Price1 = Convert.ToDouble(drRoomCateg[room]["Price"]);
                                                string TypeName = Convert.ToString(drRoomCateg[room]["Name"]);
                                                string RoomCode = RoomCodeMain + Guid.NewGuid().ToString().Substring(0, 4) + "$$$" + XMLNationality + "$$$" + Currency + "$$$" + Price1 + "$$$" + TypeName + "$$$" + haskey + "$$$" + item.XmlCity + "$$$" + item.XmlCountry;

                                                RoomCode = RoomCode.Replace("'", "");



                                                string BoardID = Convert.ToString(drRoomCateg[room]["BFType"]);

                                                double NetPrice = Convert.ToDouble(drRoomCateg[room]["NetPrice"]);
                                                double GrossPrice = Convert.ToDouble(drRoomCateg[room]["GrossPrice"]);
                                                double Price = Convert.ToDouble(drRoomCateg[room]["Price"]);

                                                Price = Price * CurrencyFactor;

                                                #endregion

                                                #region Offers
                                                string offfer = "";
                                                string offfer1 = "";
                                                DataRow[] drRoom1 = HotelsTrackResponse.Tables["Rate"].Select("RoomType_Id='" + RoomCategId + "'");

                                                for (var k = 0; k < drRoom1.Length; k++)
                                                {
                                                    int roomid = Convert.ToInt16(drRoom1[k]["Rate_Id"]);
                                                    DataRow[] promotion = HotelsTrackResponse.Tables["Promotion"].Select("RoomRateInfo_Id='" + roomid + "'");
                                                    for (var z = 0; z < promotion.Length; z++)
                                                    {
                                                        var ab = Convert.ToBoolean(promotion[z]["Value"]);
                                                        if (ab == true)
                                                        {
                                                            offfer1 = Convert.ToString(promotion[z]["Name"]);
                                                            offfer = offfer + "<br/>" + offfer1;
                                                        }
                                                    }
                                                }
                                                #endregion

                                                DataRow[] drRoomTypes = HotelsTrackResponse.Tables["RoomType"].Select("RoomCateg_Id='" + RoomCategId + "'");

                                                string RoomName = "";
                                                for (int j = 0; j < drRoomTypes.Length; j++)
                                                {
                                                    string RoomType_Id = Convert.ToString(drRoomTypes[j]["RoomType_Id"]);
                                                    string TypeNameRoom = Convert.ToString(drRoomTypes[j]["TypeName"]);
                                                    double TotalPrice = Convert.ToDouble(drRoomTypes[j]["TotalPrice"]);
                                                    double avrNightPrice = Convert.ToDouble(drRoomTypes[j]["avrNightPrice"]);
                                                    avrNightPrice = avrNightPrice * CurrencyFactor;

                                                    #region "Logic For Room Name "
                                                    List<Room_W2M> roomlist = new List<Room_W2M>();
                                                    for (int dr = 0; dr < drRoomTypes.Length; dr++)
                                                    {
                                                        Room_W2M obj = new Room_W2M();
                                                        obj.RoomCount = 1;
                                                        obj.RoomName = Convert.ToString(drRoomTypes[dr]["TypeName"]);
                                                        obj.status = true;
                                                        roomlist.Add(obj);
                                                    }
                                                    RoomName = "";// Convert.ToString(drHotelRoom[0]["Name"]);
                                                    for (int p = 0; p < roomlist.Count; p++)
                                                    {
                                                        for (int q = (p + 1); q < roomlist.Count; q++)
                                                        {
                                                            if (roomlist[p].RoomName == roomlist[q].RoomName && roomlist[q].status)
                                                            {
                                                                roomlist[p].RoomCount = roomlist[p].RoomCount + roomlist[q].RoomCount;
                                                                roomlist[q].RoomCount = roomlist[p].RoomCount;
                                                                roomlist[q].status = false;
                                                            }
                                                        }
                                                        if (RoomName.IndexOf(roomlist[p].RoomName) < 0 || RoomName == "")
                                                            RoomName = RoomName + roomlist[p].RoomCount + " X " + roomlist[p].RoomName + " ";
                                                    }
                                                    #endregion



                                                }
                                                #region"Ratebreakup"
                                                //DateTime dtCheckInDateForRate = DateTime.ParseExact(SearchPara.CheckInDate, "dd/MM/yyyy", null);
                                                //#region Vat
                                                //Double VatOnRateBrackUp = 0;
                                                //double RoomPrice1 = Common.ProcessHotelRateBreakupPrice_WithMarkup(1, SearchPara.CityID, SearchPara.CountryId, Price / NoOfNights, B2CMarkup, IsParentId, IsPerce, AgentPercentage, AgentMarkupAmount, SubAgentMarkup, ref VatOnRateBrackUp);
                                                //#endregion
                                                //for (int night = 0; night < SearchPara.NoofNights; night++)
                                                //{
                                                //    string rateWeekColumn = Convert.ToString(dtCheckInDateForRate.Date.AddDays(night).DayOfWeek).Substring(0, 3);
                                                //    DataRow drRateBreackUp = dtHotelsTrackRateBreakup.NewRow();
                                                //    drRateBreackUp["RoomNo"] = Convert.ToString("1");
                                                //    drRateBreackUp["SupplierName"] = Suppliername;
                                                //    drRateBreackUp["OwnsystemHotelId"] = ownsystemhotelId;
                                                //    drRateBreackUp["ContractID"] = 0;
                                                //    drRateBreackUp["RoomTypeName"] = TypeName + "[" + RoomName + "]"; //TypeNameRoom;
                                                //    drRateBreackUp["RoomTypeId"] = RoomCode;
                                                //    drRateBreackUp["MealId"] = BoardID;
                                                //    drRateBreackUp["Meal"] = BoardID;
                                                //    drRateBreackUp["BookingStatus"] = "Available";
                                                //    drRateBreackUp["DateOfWeek"] = rateWeekColumn;
                                                //    drRateBreackUp["Date"] = dtCheckInDateForRate.Date.AddDays(night).Date;
                                                //    drRateBreackUp["Rate"] = RoomPrice1;
                                                //    drRateBreackUp["RateType"] = TypeName + "[" + RoomName + "]"; //SearchPara.Rooms.Count == 1 ? "A Rate" : ((j + 1) + ". " + TypeNameRoom); //"A Rate";
                                                //    drRateBreackUp["SupplierCurrency"] = Currency;
                                                //    drRateBreackUp["BuyingPrice"] = Price1 / NoOfNights;
                                                //    dtHotelsTrackRateBreakup.Rows.Add(drRateBreackUp);
                                                //    dtHotelsTrackRateBreakup.AcceptChanges();
                                                //}
                                                #endregion
                                                #region Rooms
                                                int TotAdult = 0;
                                                int TotChild = 0;
                                                for (int k = 0; k < SearchParam.Rooms.Count; k++)
                                                {
                                                    int Adult = SearchParam.Rooms[k].NoofAdults;
                                                    if (k == 0)
                                                        TotAdult = TotAdult + Adult;
                                                    else
                                                        TotAdult = TotAdult + Adult;
                                                    int Child = SearchParam.Rooms[k].NoofChild;
                                                    if (Child > 0)
                                                    {
                                                        if (k == 0)
                                                            TotChild = TotChild + Child;
                                                        else
                                                            TotChild = TotChild + Child;
                                                    }
                                                }
                                                DataRow drRoom = dtHotelsTrackRoom.NewRow();
                                                drRoom["OwnSystemHotelId"] = OwnsystemHotelId;
                                                drRoom["RoomTypeName"] = TypeName + "[" + RoomName + "]";
                                                drRoom["RoomTypeCode"] = RoomCode;
                                                drRoom["MealId"] = BoardID;
                                                drRoom["MealName"] = BoardID;
                                                drRoom["ContractID"] = 0;
                                                drRoom["BoardType"] = TypeName;
                                                drRoom["RoomTypeId"] = RoomCode;
                                                drRoom["AdultsOccupancy"] = TotAdult;
                                                drRoom["ChildOccupancy"] = TotChild;
                                                drRoom["MaxExtraBed"] = "0";
                                                drRoom["RoomNo"] = "1";
                                                drRoom["RoomSequenceNo"] = TypeName;
                                                drRoom["Type"] = haskey;
                                                drRoom["OfficeId"] = CancelPolicyId == "" ? InternalCode : InternalCode_PolicyId;
                                                drRoom["BookingStatus"] = "Available";
                                                drRoom["UniqueNo"] = "";
                                                drRoom["OfferDescription"] = offfer;
                                                drRoom["OfferCode"] = "";
                                                drRoom["EssentialInformation"] = Message;
                                                double ItemPrice = Convert.ToDouble(Price);
                                                _markup.ProcessHotelRoom_WithMarkup(CurrencyFactor, Currency, true, true, SearchParam.NoofNights, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), SearchParam.CheckInDate, SearchParam.CheckOutDate, ref ItemPrice, ref drRoom, isPerce, AgentPercentage, AgentMarkupAmount, commonSearch.SubAgentMarkup, commonSearch.B2CMarkup, commonSearch.IsParentId);
                                                drRoom["SupplierName"] = Suppliername;
                                                dtHotelsTrackRoom.Rows.Add(drRoom);
                                                #endregion

                                                #region Policy Added New on 01/09/25
                                                DataRow[] drPolicies = HotelsTrackResponse.Tables["Policies"].Select("RoomCateg_Id='" + RoomCategId + "'");
                                                int Policies_Id = Convert.ToInt32(drPolicies[0]["Policies_Id"]);

                                                DataRow[] drPolicy = HotelsTrackResponse.Tables["Policy"].Select("Policies_Id='" + Policies_Id + "'");

                                                List<int> ExCancelDays = new List<int>();
                                                string Description = "";
                                                for (int k = 0; k < drPolicy.Length; k++)
                                                {

                                                    int ExCancelDay = Convert.ToInt32(drPolicy[k]["ExCancelDays"]);
                                                    #region Not Used
                                                    //string ChargeType = Convert.ToString(drPolicy[k]["ChargeType"]);
                                                    //string CurrencyP = Convert.ToString(ChargeType.ToLower() == "amount" ? Convert.ToString(drPolicy[k]["Currency"]) : "");

                                                    ////string Description = Convert.ToString(dtTemp.DefaultView[k]["Description"]) != string.Empty ? Convert.ToString(dtTemp.DefaultView[k]["Description"]) : "";


                                                    //try
                                                    //{
                                                    //    Description = Convert.ToString(drPolicy[k]["Description"]) != string.Empty ? Convert.ToString(drPolicy[k]["Description"]) : "";

                                                    //}
                                                    //catch (Exception)
                                                    //{

                                                    //}

                                                    //double ChargeRate = 0;
                                                    //ChargeRate = Convert.ToDouble(ChargeType.ToLower() == "full charge" ? 0 : drPolicy[k]["ChargeRate"]);

                                                    //string FromDate = Convert.ToString(drPolicy[k]["FromDate"]);
                                                    //string ToDate = Convert.ToString(drPolicy[k]["ToDate"]);
                                                    #endregion
                                                    ExCancelDays.Add(ExCancelDay);
                                                }


                                                int maxExCancelDays = ExCancelDays.Max();
                                                //DateTime ToDateTime = Convert.ToDateTime(ToDate).Date;
                                                // string CheckIN = objSearPera.CheckInDate;
                                                // CheckIN = CheckIN.Substring(3, 2) + "/" + CheckIN.Substring(0, 2) + "/" + CheckIN.Substring(6, 4);
                                                DateTime CheckInDate = DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null);//Convert.ToDateTime(CheckIN);
                                                DateTime currentdate = DateTime.Now.Date;
                                                DateTime FinalCancelDate = DateTime.Now.Date;
                                                FinalCancelDate = CheckInDate.AddDays(-maxExCancelDays).Date;
                                                if (FinalCancelDate < currentdate)
                                                    FinalCancelDate = DateTime.Now.Date;


                                                if (FinalCancelDate.Date > DateTime.Now.Date)
                                                {
                                                    FinalCancelDate = FinalCancelDate.AddDays(CutOffDays).Date;
                                                }
                                                else
                                                {
                                                    FinalCancelDate = DateTime.Now.Date;
                                                }


                                                #region Vat
                                                double VatOnPolicy = 0;
                                                double MainAgentCharge = 0;
                                                double Penalty = Price;
                                                Penalty = _markup.ProcessHotelCancelationPrice_WithMarkup(ref MainAgentCharge, SearchParam.NoofNights, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), Penalty, isPerce, commonSearch.B2CMarkup, AgentPercentage, AgentMarkupAmount, commonSearch.IsParentId, commonSearch.SubAgentMarkup, ref VatOnPolicy);
                                                MainAgentCharge = Penalty;
                                                double CompanyChargeAmount = Penalty;
                                                //Common.ProcessHotelCancelationPrice_WithMarkup(ref MainAgentCharge, objSearPera.NoofNights, objSearPera.CityID, objSearPera.CountryId, Penalty, isPerce, B2CMarkup, AgentPercentage, AgentMarkupAmount, IsParentId, SubAgentMarkup, ref VatOnPolicy);

                                                #endregion

                                                DateTime NowDate = DateTime.Now.Date;

                                                if (NowDate < FinalCancelDate.Date)
                                                {
                                                    if (dtHotelsTrackCancellationPolicy.Rows.Count == 0)
                                                    {
                                                        double zero = 0;

                                                        dtHotelsTrackCancellationPolicy.Rows.Add(_commonMethod.setCancellationPolicyRow(ref zero, dtHotelsTrackCancellationPolicy.NewRow(), CurrencyFactor, item.CurrencyCode, item.Supplier, Convert.ToString(OwnsystemHotelId), Convert.ToString(1),
                                                                                   Convert.ToString(RoomCode), RoomName, BoardID, DateTime.Now.Date, Convert.ToDateTime(FinalCancelDate.AddDays(-1)), 0, 0, "0", VatOnPolicy, "", "NO", "", Description, "", ""));
                                                        dtHotelsTrackCancellationPolicy.AcceptChanges();
                                                    }

                                                    //double CancelPrice = 0;
                                                    //CancelPrice = Convert.ToDouble(dtHotelsTrackPolicy.Rows[0]["CompanyCancellationPrice"]);

                                                    //if (CancelPrice < RoomTotalPrice)
                                                    {
                                                        dtHotelsTrackCancellationPolicy.Rows.Add(_commonMethod.setCancellationPolicyRow(ref CompanyChargeAmount, dtHotelsTrackCancellationPolicy.NewRow(), CurrencyFactor, item.CurrencyCode, item.Supplier, Convert.ToString(OwnsystemHotelId), Convert.ToString(1),
                                                                                   Convert.ToString(RoomCode), RoomName, BoardID, Convert.ToDateTime(FinalCancelDate), DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null), CompanyChargeAmount, MainAgentCharge, "0", VatOnPolicy, "", "NO", "", Description, "", ""));
                                                        dtHotelsTrackCancellationPolicy.AcceptChanges();
                                                    }
                                                }
                                                else
                                                {
                                                    //double CancelPrice = 0;
                                                    //if (dtHotelsTrackPolicy.Rows.Count != 0)
                                                    //    CancelPrice = Convert.ToDouble(dtHotelsTrackPolicy.Rows[0]["CompanyCancellationPrice"]);

                                                    //if (CancelPrice < RoomTotalPrice)
                                                    {
                                                        dtHotelsTrackCancellationPolicy.Rows.Add(_commonMethod.setCancellationPolicyRow(ref CompanyChargeAmount, dtHotelsTrackCancellationPolicy.NewRow(), CurrencyFactor, item.CurrencyCode, item.Supplier, Convert.ToString(OwnsystemHotelId), Convert.ToString(1),
                                                                                   Convert.ToString(RoomCode), RoomName, BoardID, DateTime.Now.Date, DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null), CompanyChargeAmount, MainAgentCharge, "0", 0, "", "Yes", "", Description));
                                                        dtHotelsTrackCancellationPolicy.AcceptChanges();
                                                    }
                                                }
                                                #endregion

                                            }

                                            Options ops = (await _supplierRes.GetCommonRoomForSuppliers(GiataId, commonSearch.tokenData, commonSearch.advancedOptions, Pricenew, drXmlHotel, dtHotelsTrackRoom, dtHotelsTrackCancellationPolicy, dtHotelsTrackRateBreakup, SearchParam));
                                            if (ops != null)
                                                options.Add(ops);
                                        }


                                        
                                    }
                                    #endregion
                                    #endregion
                                }

                            }
                        }
                    }
                    #endregion
                }

            }
            catch (Exception ex)
            {
                _ = _exception.Error(ex);
                return options;
            }
            return options;
        }

        public class RoomData
        {
            public double FinalSellingPrice { set; get; }
            public int RoomNo { set; get; }
            public string MealID { set; get; }
        }

        public class Room_W2M
        {
            public int RoomCount { get; set; }
            public string RoomName { get; set; }
            public bool status { get; set; }
        }

        public async Task<DataSet> GetHotelsTrackCancellationPolicy(double RoomPrice, string incommingofficecode, string Type, DataSet dsHotel, string hotelcode, string ownsystemid, string RoomTypeId_Own, double RoomTotalPrice, string MealID, string Board_Text, DataTable dtAgentDetail, DataTable dtSuplierMarkup, DataTable dtSearchConfig, HotelSearchParameter objSearPera, string roomno, string Provider, int AgentId, DataTable dtMarkupDetails,TokenData tokenData, string GiataId = "0", int hotelRating = 0, string SupplierProvider = "")
        {
            try
            {
                #region Cancellation Policy RQ
                DataTable dtHotelsTrackPolicy = _commonMethod.GenerateCancellationDataTableNewV2();
               
                Boolean isPerce = true;
                double AgentCompanyPercentage = Convert.ToDouble(objSearPera.AgentCompanyPercentage);
                bool IsB2C = Convert.ToBoolean(Convert.ToByte(objSearPera.IsB2BOrB2C));
                double B2CMarkup = (IsB2C == false ? AgentCompanyPercentage : XMLHotelBookingCommon.B2CMarkup);
                double AgentPercentage = 0.0;
                double AgentMarkupAmount = 0.0;
                double SubAgentMarkup = 0;
                int IsParentId = 1;
                Boolean blnIsShowSupplier = false;
                string CheckinDate = objSearPera.CheckInDate;
                _markup.getAgetMarkupSetting(SupplierProvider, GiataId, dtSuplierMarkup, dtMarkupDetails, ref AgentPercentage, ref AgentMarkupAmount, ref blnIsShowSupplier, ref isPerce, AgentId, hotelRating, objSearPera.CityID,Convert.ToString(objSearPera.CountryID), CheckinDate);
                if (dtAgentDetail != null)
                {
                    IsParentId = Convert.ToInt32(dtAgentDetail.Rows[0]["IsParentId"]);
                    SubAgentMarkup = Convert.ToDouble(dtAgentDetail.Rows[0]["B2CMarkupPer"]);
                }
                int CutOffDays = XMLHotelBookingCommon.CutOffDays;
                CutOffDays = _commonMethod.GetCutOffDays(dtSuplierMarkup, SupplierProvider);
                DataRow[] drSuplierMarkup = (dtSuplierMarkup.Select("SupplierName='" + SupplierProvider + "'"));
                string UserId = Convert.ToString(drSuplierMarkup[0]["UserId"]);
                string UserName = Convert.ToString(drSuplierMarkup[0]["UserName"]);
                string Password = Convert.ToString(drSuplierMarkup[0]["Password"]);
                string HotelsTrackCurrency = Convert.ToString(drSuplierMarkup[0]["CurrencyShortName"]);

                double CurrencyFactorHotelsTrack = 1;
                if (HotelsTrackCurrency.ToLower() != tokenData.SiteCurrency.ToLower())
                {
                    //DataTable dtInrCurrency = Common.GetCountryCurrency(XMLHotelBookingCommon.HotelsTrackCurrency);
                    //CurrencyFactorHotelsTrack = Convert.ToDouble(dtInrCurrency.Rows[0]["AmtToExch"]);
                    CurrencyFactorHotelsTrack = _commonMethod.GetROEByCurrency(HotelsTrackCurrency, tokenData.dtROE);
                }

                string CheckIn = objSearPera.CheckInDate;
                CheckIn = CheckIn.Substring(6, 4) + "-" + CheckIn.Substring(3, 2) + "-" + CheckIn.Substring(0, 2);
                string CheckOut = objSearPera.CheckOutDate;
                CheckOut = CheckOut.Substring(6, 4) + "-" + CheckOut.Substring(3, 2) + "-" + CheckOut.Substring(0, 2);
                string HotelsTrackUrl = Convert.ToString(drSuplierMarkup[0]["WebSiteURL"]) + HSStaticInfo.ViewPolicyHotelsTrack;
                int SID = Convert.ToInt32(drSuplierMarkup[0]["SupplierID"]);
                DataRow[] drSearchConfig = (dtSearchConfig.Select("SupplierID='" + SID + "'"));
                int SearchTimeOut = Convert.ToInt32(drSearchConfig[0]["BookingConfirmationTimeout"]);
                string PropertyId = incommingofficecode;
                int NoOfNights = objSearPera.NoofNights;
                string RoomName = "", PaxPassport = "";
                int RoomNo = Convert.ToInt32(roomno);
                double MainAgentCha = RoomTotalPrice;
                double CompanyChargeAmount1 = RoomTotalPrice;
                string CancelPolicyRQ = Get_HotelsTrack_Policy_RQ(objSearPera, UserId, UserName, Password, PaxPassport, hotelcode, PropertyId, CheckIn, CheckOut, NoOfNights, RoomTypeId_Own, MealID, ref RoomName, Type, RoomNo);
                #endregion
                #region Cancellation Policy RS
                if (CancelPolicyRQ != null)
                {
                    string sentData = "requestXML=" + CancelPolicyRQ;

                    _commonMethod.ThirdPartyGetPolicyHistory(Convert.ToString(RoomTypeId_Own), Convert.ToString(ownsystemid), CancelPolicyRQ, "-", "Before Get Policy", SupplierProvider, "Request", "-", "");

                    ResponseDto responseDto = await Response_SupplierHotelsTrack(sentData, HotelsTrackUrl, SearchTimeOut, "hotelstrack");
                    string ResponseData = responseDto.Response;
                    string Error = responseDto.Error;

                    if (ResponseData == string.Empty || ResponseData == "" || ResponseData == null)
                        return dsHotel;
                    _commonMethod.ThirdPartyGetPolicyHistory(Convert.ToString(RoomTypeId_Own), Convert.ToString(ownsystemid), CancelPolicyRQ, Convert.ToString(ResponseData) + Error, "After Get Policy", SupplierProvider, "Response", "-", "");

                    DataSet HotelsTrackResponse = _commonMethod.GetDataSetFromXMLString(ResponseData);

                    if (!HotelsTrackResponse.Tables.Contains("Policy"))
                        return dsHotel;
                    else
                    {
                        #region DataRaw
                        List<int> Catg = new List<int>();
                        if (HotelsTrackResponse.Tables.Contains("RoomCatgCode") == true)
                        {
                            int RoomCatgCodeAllRow = Convert.ToInt32(HotelsTrackResponse.Tables["RoomCatgCode"].Rows.Count);

                            for (int i = 0; i < RoomCatgCodeAllRow; i++)
                            {
                                int PolicyId1 = 0;
                                if (HotelsTrackResponse.Tables["RoomCatgCode"].Columns.Contains("Policy_Id") == true)
                                    PolicyId1 = Convert.ToInt32(HotelsTrackResponse.Tables["RoomCatgCode"].Rows[i]["Policy_Id"]);
                                Catg.Add(PolicyId1);

                            }
                        }

                        List<int> Policy = new List<int>();
                        if (HotelsTrackResponse.Tables.Contains("Policy") == true)
                        {
                            int PolicyAllRow = Convert.ToInt32(HotelsTrackResponse.Tables["Policy"].Rows.Count);

                            for (int i = 0; i < PolicyAllRow; i++)
                            {
                                int PolicyId2 = 0;
                                if (HotelsTrackResponse.Tables["Policy"].Columns.Contains("Policy_Id") == true)
                                    PolicyId2 = Convert.ToInt32(HotelsTrackResponse.Tables["Policy"].Rows[i]["Policy_Id"]);
                                Policy.Add(PolicyId2);

                            }
                        }

                        int[] FinalRes = (Catg.Except(Policy).Union(Policy.Except(Catg))).ToArray();
                        int ResPolicyID = 0;
                        if (FinalRes.Count() > 0)
                            ResPolicyID = FinalRes[0];


                        DataTable dtTemp = HotelsTrackResponse.Tables["Policy"].Clone();

                        bool IsNonRef = false;
                        if (HotelsTrackResponse.Tables.Contains("RoomCatgCode") == true)
                        {
                            int RoomCatgCoderow = Convert.ToInt32(HotelsTrackResponse.Tables["RoomCatgCode"].Rows.Count);
                            for (int k = 0; k < RoomCatgCoderow; k++)
                            {
                                int policyid = 0;
                                string PolicyName = "";
                                string RoomCatgCode_Text = "";
                                string BFType = "";

                                if (HotelsTrackResponse.Tables["RoomCatgCode"].Columns.Contains("Name") == true)
                                    PolicyName = Convert.ToString(HotelsTrackResponse.Tables["RoomCatgCode"].Rows[k]["Name"]);
                                if (HotelsTrackResponse.Tables["RoomCatgCode"].Columns.Contains("RoomCatgCode_Text") == true)
                                    RoomCatgCode_Text = Convert.ToString(HotelsTrackResponse.Tables["RoomCatgCode"].Rows[k]["RoomCatgCode_Text"]);
                                if (HotelsTrackResponse.Tables["RoomCatgCode"].Columns.Contains("Policy_Id") == true)
                                    policyid = Convert.ToInt32(HotelsTrackResponse.Tables["RoomCatgCode"].Rows[k]["Policy_Id"]);
                                if (HotelsTrackResponse.Tables["RoomCatgCode"].Columns.Contains("BFType") == true)
                                    BFType = Convert.ToString(HotelsTrackResponse.Tables["RoomCatgCode"].Rows[k]["BFType"]);

                                string Passport = "";
                                string currency = "";
                                string Price = "";
                                string RoomId = Get_HotelsTrack_RoomTypeID(RoomTypeId_Own, ref Passport, ref currency, ref Price);

                                string RoomCode = RoomId.Remove(RoomId.Length - 4);

                                //if (RoomCode == RoomCatgCode_Text && MealID == BFType && Board_Text == PolicyName)
                                if (RoomCode == RoomCatgCode_Text && MealID == BFType) //Board_Text not getting in apiout
                                {
                                    if (HotelsTrackResponse.Tables.Contains("Policy") == true)
                                    {
                                        if (HotelsTrackResponse.Tables["Policy"].Columns.Contains("Policy_Id") == true)
                                        {
                                            DataRow[] drPolicyrow = (HotelsTrackResponse.Tables["Policy"].Select("Policy_Id='" + policyid + "'"));
                                            for (int m = 0; m < drPolicyrow.Length; m++)
                                                dtTemp.ImportRow(drPolicyrow[m]);
                                        }
                                    }
                                }
                                else if (BFType == "")
                                {
                                    IsNonRef = true;
                                }
                            }

                            if (dtTemp.DefaultView.Count == 0)
                            {
                                if (HotelsTrackResponse.Tables.Contains("Policy") == true)
                                {
                                    if (HotelsTrackResponse.Tables["Policy"].Columns.Contains("Policy_Id") == true)
                                    {
                                        int policyid = ResPolicyID;
                                        DataRow[] drPolicyrow = (HotelsTrackResponse.Tables["Policy"].Select("Policy_Id='" + policyid + "'"));
                                        for (int m = 0; m < drPolicyrow.Length; m++)
                                            dtTemp.ImportRow(drPolicyrow[m]);
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (HotelsTrackResponse.Tables.Contains("Policy") == true)
                            {
                                if (HotelsTrackResponse.Tables["Policy"].Columns.Contains("Policies_Id") == true)
                                {
                                    int policyid = ResPolicyID;
                                    DataRow[] drPolicyrow = (HotelsTrackResponse.Tables["Policy"].Select("Policies_Id='" + policyid + "'"));
                                    for (int m = 0; m < drPolicyrow.Length; m++)
                                        dtTemp.ImportRow(drPolicyrow[m]);
                                }
                            }
                        }
                        if (IsNonRef != true)
                        {
                            string Description = "";
                            string ToDate = "";
                            Double Penalty = RoomTotalPrice;
                            dtTemp.DefaultView.Sort = "ExCancelDays desc";
                            // Ensure there are no DBNull values
                            //
                            List<int> ExCancelDays = new List<int>();
                            for (int k = 0; k < dtTemp.DefaultView.Count; k++)
                            {

                                int ExCancelDay = Convert.ToInt32(dtTemp.DefaultView[k]["ExCancelDays"]);
                                string ChargeType = Convert.ToString(dtTemp.DefaultView[k]["ChargeType"]);
                                string Currency = Convert.ToString(ChargeType.ToLower() == "amount" ? Convert.ToString(dtTemp.DefaultView[k]["Currency"]) : "");

                                //string Description = Convert.ToString(dtTemp.DefaultView[k]["Description"]) != string.Empty ? Convert.ToString(dtTemp.DefaultView[k]["Description"]) : "";


                                try
                                {
                                    Description = Convert.ToString(dtTemp.DefaultView[k]["Description"]) != string.Empty ? Convert.ToString(dtTemp.DefaultView[k]["Description"]) : "";

                                }
                                catch (Exception)
                                {

                                }

                                double ChargeRate = 0;
                                ChargeRate = Convert.ToDouble(ChargeType.ToLower() == "full charge" ? 0 : dtTemp.DefaultView[k]["ChargeRate"]);

                                string FromDate = Convert.ToString(dtTemp.DefaultView[k]["FromDate"]);
                                ToDate = Convert.ToString(dtTemp.DefaultView[k]["ToDate"]);
                                string Policies_Id = Convert.ToString(dtTemp.DefaultView[k]["Policies_Id"]);
                                ExCancelDays.Add(ExCancelDay);
                            }
                            int maxExCancelDays = ExCancelDays.Max();
                            DateTime ToDateTime = Convert.ToDateTime(ToDate).Date;
                            // string CheckIN = objSearPera.CheckInDate;
                            // CheckIN = CheckIN.Substring(3, 2) + "/" + CheckIN.Substring(0, 2) + "/" + CheckIN.Substring(6, 4);
                            DateTime CheckInDate = DateTime.ParseExact(objSearPera.CheckInDate, "dd/MM/yyyy", null);//Convert.ToDateTime(CheckIN);
                            DateTime currentdate = DateTime.Now.Date;
                            DateTime FinalCancelDate = DateTime.Now.Date;
                            FinalCancelDate = CheckInDate.AddDays(-maxExCancelDays).Date;
                            if (FinalCancelDate < currentdate)
                                FinalCancelDate = DateTime.Now.Date;


                            if (FinalCancelDate.Date > DateTime.Now.Date)
                            {
                                FinalCancelDate = FinalCancelDate.AddDays(CutOffDays).Date;
                            }
                            else
                            {
                                FinalCancelDate = DateTime.Now.Date;
                            }


                            #region Vat
                            double VatOnPolicy = 0;
                            double MainAgentCharge = 0;
                            // double Penalty = RoomPrice;
                            Penalty = _markup.ProcessHotelCancelationPrice_WithMarkup(ref MainAgentCharge, objSearPera.NoofNights, objSearPera.CityID, Convert.ToString(objSearPera.CountryID), Penalty, isPerce, B2CMarkup, AgentPercentage, AgentMarkupAmount, IsParentId, SubAgentMarkup, ref VatOnPolicy);
                            MainAgentCharge = Penalty;
                            double CompanyChargeAmount = Penalty;
                            //Common.ProcessHotelCancelationPrice_WithMarkup(ref MainAgentCharge, objSearPera.NoofNights, objSearPera.CityID, objSearPera.CountryId, Penalty, isPerce, B2CMarkup, AgentPercentage, AgentMarkupAmount, IsParentId, SubAgentMarkup, ref VatOnPolicy);

                            #endregion

                            DateTime NowDate = DateTime.Now.Date;

                            if (NowDate < FinalCancelDate.Date)
                            {
                                if (dtHotelsTrackPolicy.Rows.Count == 0)
                                {
                                    double zero = 0;

                                    dtHotelsTrackPolicy.Rows.Add(_commonMethod.setCancellationPolicyRow(ref zero, dtHotelsTrackPolicy.NewRow(), CurrencyFactorHotelsTrack, HotelsTrackCurrency, SupplierProvider, ownsystemid, Convert.ToString(roomno),
                                                               Convert.ToString(RoomTypeId_Own), RoomName, MealID, DateTime.Now.Date, Convert.ToDateTime(FinalCancelDate.AddDays(-1)), 0, 0, "0", VatOnPolicy, "", "NO", Type, Description, "", ""));
                                    dtHotelsTrackPolicy.AcceptChanges();
                                }

                                //double CancelPrice = 0;
                                //CancelPrice = Convert.ToDouble(dtHotelsTrackPolicy.Rows[0]["CompanyCancellationPrice"]);

                                //if (CancelPrice < RoomTotalPrice)
                                {
                                    dtHotelsTrackPolicy.Rows.Add(_commonMethod.setCancellationPolicyRow(ref CompanyChargeAmount, dtHotelsTrackPolicy.NewRow(), CurrencyFactorHotelsTrack, HotelsTrackCurrency, SupplierProvider, ownsystemid, Convert.ToString(roomno),
                                                               Convert.ToString(RoomTypeId_Own), RoomName, MealID, Convert.ToDateTime(FinalCancelDate), DateTime.ParseExact(objSearPera.CheckInDate, "dd/MM/yyyy", null), CompanyChargeAmount, MainAgentCharge, "0", VatOnPolicy, "", "NO", Type, Description, "", ""));
                                    dtHotelsTrackPolicy.AcceptChanges();
                                }
                            }
                            else
                            {
                                //double CancelPrice = 0;
                                //if (dtHotelsTrackPolicy.Rows.Count != 0)
                                //    CancelPrice = Convert.ToDouble(dtHotelsTrackPolicy.Rows[0]["CompanyCancellationPrice"]);

                                //if (CancelPrice < RoomTotalPrice)
                                {
                                    dtHotelsTrackPolicy.Rows.Add(_commonMethod.setCancellationPolicyRow(ref CompanyChargeAmount, dtHotelsTrackPolicy.NewRow(), CurrencyFactorHotelsTrack, HotelsTrackCurrency, SupplierProvider, ownsystemid, Convert.ToString(roomno),
                                                               Convert.ToString(RoomTypeId_Own), RoomName, MealID, DateTime.Now.Date, DateTime.ParseExact(objSearPera.CheckInDate, "dd/MM/yyyy", null), CompanyChargeAmount, MainAgentCharge, "0", 0, "", "Yes", Type, Description));
                                    dtHotelsTrackPolicy.AcceptChanges();
                                }
                            }
                            //}
                        }
                        else
                        {
                            double mainvat = 0;

                            RoomTotalPrice = _markup.ProcessHotelCancelationPrice_WithMarkup(ref MainAgentCha, objSearPera.NoofNights, objSearPera.CityID, Convert.ToString(objSearPera.CountryID), RoomTotalPrice, isPerce, B2CMarkup, AgentPercentage, AgentMarkupAmount, IsParentId, SubAgentMarkup, ref mainvat);

                            dtHotelsTrackPolicy.Rows.Add(_commonMethod.setCancellationPolicyRow(ref CompanyChargeAmount1, dtHotelsTrackPolicy.NewRow(), CurrencyFactorHotelsTrack, HotelsTrackCurrency, SupplierProvider, ownsystemid, Convert.ToString(roomno),
                                                                   Convert.ToString(RoomTypeId_Own), RoomName, MealID, DateTime.Now.Date, DateTime.ParseExact(objSearPera.CheckInDate, "dd/MM/yyyy", null), MainAgentCha, RoomTotalPrice, "0", 0, "", "Yes", Type, ""));
                            dtHotelsTrackPolicy.AcceptChanges();
                        }
                        #endregion

                    }
                }
                dsHotel.Tables[2].Merge(dtHotelsTrackPolicy);
                return dsHotel;
                #endregion
            }
            catch (Exception ex)
            {
                _=_exception.ErrorNew(ex, ErrorLog._LogAPI.Recheck);
                return dsHotel;
            }
        }
        public string Get_HotelsTrack_Policy_RQ(HotelSearchParameter SearchPara, string UserId, string UserName, string Password, string PaxPassport, string hotelcode, string InternalCode, string CheckIn, string CheckOut, int NoOfNights, string RoomTypeId, string MealID, ref string RoomName, string Type, int RoomNo)
        {
            try
            {
                #region Cancellation Policy
                string[] spitid = InternalCode.Split(new String[] { "$$$" }, StringSplitOptions.None);
                string CancelPolicy = "";
                string IntCode = "";
                string currency = "";

                IntCode = spitid[0].ToString();
                if (spitid.Length >= 2)
                    CancelPolicy = spitid[1].ToString();
                string Price = "";
                RoomTypeId = Get_HotelsTrack_RoomTypeID(RoomTypeId, ref PaxPassport, ref currency, ref Price);
                StringBuilder HotelsTrack_CancelPolicyRQ = new StringBuilder();
                HotelsTrack_CancelPolicyRQ.Append("<Service_ViewCancelPolicy>");
                HotelsTrack_CancelPolicyRQ.Append("<AgentLogin><AgentId>" + UserId + "</AgentId><LoginName>" + UserName + "</LoginName><Password>" + Password + "</Password></AgentLogin>");
                HotelsTrack_CancelPolicyRQ.Append("<ViewCancelPolicy_Request>");
                HotelsTrack_CancelPolicyRQ.Append("<PaxPassport>" + PaxPassport + "</PaxPassport>");
                // HotelsTrack_CancelPolicyRQ.Append("<RPCurrency>" + currency + "</RPCurrency>");
                HotelsTrack_CancelPolicyRQ.Append("<HotelId>" + hotelcode + "</HotelId>");
                HotelsTrack_CancelPolicyRQ.Append("<InternalCode>" + IntCode + "</InternalCode>");
                HotelsTrack_CancelPolicyRQ.Append("<dtCheckIn>" + CheckIn + "</dtCheckIn>");
                HotelsTrack_CancelPolicyRQ.Append("<dtCheckOut>" + CheckOut + "</dtCheckOut>");
                for (int l = 0; l < SearchPara.Rooms.Count; l++)
                {
                    int NoOfAdult = 0;
                    NoOfAdult = SearchPara.Rooms[l].NoofAdults;

                    string RoomType = "";
                    int NoOfChild = SearchPara.Rooms[l].NoofChild;
                    NoOfAdult = NoOfAdult + NoOfChild;
                    RoomType = Get_HotelsTrack_RoomType(NoOfAdult);
                    string ChildBed = "N";
                    HotelsTrack_CancelPolicyRQ.Append("<RoomInfo SeqNo=\"" + (l + 1) + "\" RoomType =\"" + RoomType + "\" RQBedChild=\"" + ChildBed + "\">");

                    HotelsTrack_CancelPolicyRQ.Append("<AdultNum>" + NoOfAdult + "</AdultNum>");

                    HotelsTrack_CancelPolicyRQ.Append("</RoomInfo>");
                }

                if (CancelPolicy != "")
                    HotelsTrack_CancelPolicyRQ.Append("<CancelPolicyID>" + CancelPolicy + "</CancelPolicyID>");
                else
                    HotelsTrack_CancelPolicyRQ.Append("<CancelPolicyID/>");

                HotelsTrack_CancelPolicyRQ.Append("<flagAvail>Y</flagAvail>");
                HotelsTrack_CancelPolicyRQ.Append("</ViewCancelPolicy_Request>");
                HotelsTrack_CancelPolicyRQ.Append("</Service_ViewCancelPolicy>");
                string searchRQString = HotelsTrack_CancelPolicyRQ.ToString();
                return searchRQString;
                #endregion
            }
            catch (Exception ex)
            {
                _ =_exception.ErrorNew(ex, ErrorLog._LogAPI.Recheck);
                return "";
            }
        }

        public async Task<PriceCheckHTDto> Get_HotelsTrack_PriceCheck_RQRS(DataSet dsCartDetails, DataRow[] HotelsTrackDetails, string PaxPassPort, int i, int j, DataTable dtCheckPriceReqStructure, string UniqueNo, string AgentUniqueNo, double FinalPrice, bool isForBooking, double CompanyBuyingPrice, string HasKey_IntCode, string IsPrcCheck_Book, string Supplier, int AgentId, TokenData tokenData)
        {
            PriceCheckHTDto priceCheckDto = new PriceCheckHTDto();
            string responsetype = "Success";
            string time = "";
            //string SupplierName = "hotelstrack";
            string xslRequest = "";
            string ResponseData = "";
            string Error = "";
            try
            {
                #region Price Check RQ and Declaration
                double CurrencyFactorHotelsTrack = 1;
                //if (XMLHotelBookingCommon.HotelsTrackCurrency.ToLower() != "usd")
                //{
                //    DataTable dtInrCurrency = Common.GetCountryCurrency(XMLHotelBookingCommon.HotelsTrackCurrency);
                //    CurrencyFactorHotelsTrack = Convert.ToDouble(dtInrCurrency.Rows[0]["AmtToExch"]);
                //}
                DataSet dstemp = new DataSet();
                string WebSiteUrl = Convert.ToString(HotelsTrackDetails[0]["WebSiteURL"]);
                string UserId = Convert.ToString(HotelsTrackDetails[0]["UserId"]);
                string UserName = Convert.ToString(HotelsTrackDetails[0]["UserName"]);
                string Password = Convert.ToString(HotelsTrackDetails[0]["PassWord"]);
                string Hotelcode = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["XMLHotelId"]);
                string XmlCity = "";//Convert.ToString(dsCartDetails.Tables[0].Rows[i]["XMLCityId"]);
                string XmlCountry = "";//  Convert.ToString(dsCartDetails.Tables[0].Rows[i]["XMLCountryId"]);
                int UserCartId = Convert.ToInt32(dsCartDetails.Tables[0].Rows[i]["UserCartID"]);
                int UserCartDetailId = Convert.ToInt32(dsCartDetails.Tables[1].Rows[j]["UserCartDetailId"]);
                string RoomTypeId = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomTypeID"]);
                string CheckIn = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["CheckInDate"]);
                CheckIn = CheckIn.Substring(0, 4) + "-" + CheckIn.Substring(4, 2) + "-" + CheckIn.Substring(6, 2);
                string CheckOut = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["CheckOutDate"]);
                CheckOut = CheckOut.Substring(0, 4) + "-" + CheckOut.Substring(4, 2) + "-" + CheckOut.Substring(6, 2);
                string MealId = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["MealPlanCode"]);

                string ReqHasKey = "", InternalCode = "";

                InternalCode = Convert.ToString(HotelsTrackDetails[0]["Version"]);

                string haskey = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["purchaseToken"]);


                if (IsPrcCheck_Book.ToLower() == "p")
                {
                    DataRow[] CartResponse = dsCartDetails.Tables[1].Select("UserCartID='" + UserCartId + "'");

                    string HasKey1 = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomTypeID"]);
                    if (HasKey1.Contains("$$$"))
                    {
                        string[] spitid1 = HasKey1.Split(new String[] { "$$$" }, StringSplitOptions.None);
                        ReqHasKey = spitid1[5].ToString();
                        XmlCity = spitid1[6].ToString();
                        XmlCountry = spitid1[7].ToString();
                    }
                    //else
                    //{
                    //    ReqHasKey = Convert.ToString(CartResponse[0]["purchaseToken"]);
                    //}
                    string RoomName = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomType"]);

                    string[] spitid = RoomName.Split(new String[] { "[" }, StringSplitOptions.None);
                    string SpitIdRoomName = spitid[0].ToString();

                    bool IsRoomupdate = await _commonMethod.GetRoomNameUpdate(SpitIdRoomName, UserCartId);
                }
                //else
                //{
                //    DataRow[] CartResponse = dsCartDetails.Tables[1].Select("UserCartID='" + UserCartId + "'");
                //    string HasKey1 = Convert.ToString(CartResponse[0]["Eachotoken"]);
                //    string[] spitid = HasKey1.Split(new String[] { "$$$" }, StringSplitOptions.None);
                //    ReqHasKey = spitid[0].ToString();
                //}
                if (ReqHasKey != "")
                {
                    xslRequest = Get_HotelsTrack_PriceCheckRQ(dsCartDetails, UserCartId, UserId, UserName, Password, PaxPassPort, Hotelcode, RoomTypeId, CheckIn, CheckOut, ReqHasKey, XmlCity, XmlCountry, InternalCode);

                    _commonMethod.ThirdPartyBookingHistory(Convert.ToString(UniqueNo), Convert.ToString(AgentUniqueNo), xslRequest, "", "Before get price " + Supplier + " request hit " + j + "is for Price Check" + isForBooking, Supplier, "response confirm", "-", "WithinEarth");
                    if (xslRequest != "")
                    {
                        if (!isForBooking)
                        {
                            dtCheckPriceReqStructure.Rows.Add(_commonMethod.AddCheckPriceReq(dtCheckPriceReqStructure.NewRow(), j + 1, UserCartId, UserCartDetailId, RoomTypeId, Supplier, xslRequest));
                        }
                        #region Price Check RS
                        System.Diagnostics.Stopwatch timerroomprocess = new System.Diagnostics.Stopwatch();
                        timerroomprocess.Start();

                        ResponseDto responseDto = await Response_SupplierHotelsTrack("requestXML=" + xslRequest, WebSiteUrl + HSStaticInfo.SearchHotelsTrack, 120000, "hotelstrack");
                        ResponseData = responseDto.Response;
                        Error = responseDto.Error;
                        timerroomprocess.Stop();
                        time = timerroomprocess.Elapsed.ToString();

                        _commonMethod.ThirdPartyBookingHistory(Convert.ToString(UniqueNo), Convert.ToString(AgentUniqueNo), xslRequest, ResponseData + Error, "After get price " + Supplier + " request hit " + j + "is for Price Check" + isForBooking, Supplier, "response confirm", "-", "WithinEarth");

                        if (ResponseData == string.Empty)
                        {
                            try
                            {
                                responsetype = "GeneralError";
                                //int AgentId = Convert.ToInt32(Common.getSession("UserId") == "" ? "0" : Common.getSession("UserId"));
                                _= _commonMethod.SupplierPerformanceReportForPrebookAndBook(Supplier, "PreBook", AgentId, time, responsetype, xslRequest, ResponseData, Error);
                            }
                            catch (Exception e)
                            {
                                _ = _exception.ErrorNew(e, ErrorLog._LogAPI.Prebook);

                            }

                            priceCheckDto.Flag = false;
                            return priceCheckDto;
                        }
                        if (ResponseData.Contains("Error"))
                        {
                            try
                            {
                                responsetype = "GeneralError";
                                //int AgentId = Convert.ToInt32(Common.getSession("UserId") == "" ? "0" : Common.getSession("UserId"));
                                _ = _commonMethod.SupplierPerformanceReportForPrebookAndBook(Supplier, "PreBook", AgentId, time, responsetype, xslRequest, ResponseData, Error);
                            }
                            catch (Exception e)
                            {
                                _ = _exception.ErrorNew(e, ErrorLog._LogAPI.Prebook);

                            }
                            priceCheckDto.Flag = false;
                            return priceCheckDto;
                        }
                        DataSet HotelsTrack_Response = _commonMethod.GetDataSetFromXMLString(ResponseData);
                        #endregion

                        #region Price Check Logic

                        if (!HotelsTrack_Response.Tables.Contains("Hotel"))
                        {
                            try
                            {
                                responsetype = "GeneralError";
                                //int AgentId = Convert.ToInt32(Common.getSession("UserId") == "" ? "0" : Common.getSession("UserId"));
                                _ = _commonMethod.SupplierPerformanceReportForPrebookAndBook(Supplier, "PreBook", AgentId, time, responsetype, xslRequest, ResponseData, Error);
                            }
                            catch (Exception e)
                            {
                                _ = _exception.ErrorNew(e, ErrorLog._LogAPI.Prebook);

                            }
                            priceCheckDto.Flag = false;
                            return priceCheckDto;
                        }
                        else
                        {
                            int SearchHotel_Response_ID = 0;
                            DataRow[] SearchResponse = HotelsTrack_Response.Tables["SearchHotel_Response"].Select("SearchHotel_Response_Id='" + SearchHotel_Response_ID + "'");
                            string HasKey = Convert.ToString(SearchResponse[0]["hasKey"]);

                            string IntCode = Convert.ToString(HotelsTrack_Response.Tables["Hotel"].DefaultView[0]["InternalCode"]);

                            //  HasKey_IntCode = ReqHasKey + "$$$" + IntCode;
                            HasKey_IntCode = HasKey + "$$$" + IntCode;

                            string Currency = Convert.ToString(HotelsTrack_Response.Tables["Hotel"].DefaultView[0]["Currency"]);
                            if (Currency.ToLower() != tokenData.SiteCurrency.ToLower())
                            {
                                //DataTable dtInrCurrency = _commonMethod.GetCountryCurrency(Currency);
                                //CurrencyFactorHotelsTrack = Convert.ToDouble(dtInrCurrency.Rows[0]["AmtToExch"]);
                                CurrencyFactorHotelsTrack = _commonMethod.GetROEByCurrency(Currency, tokenData.dtROE);
                            }

                            bool IsSuccess = await _commonMethod.GetHasKeyUpdateStaus(HasKey_IntCode, UserCartId);
                            string PAXPASS = "";
                            string currency = "";
                            string Price = "";
                            string RoomCodeID = Get_HotelsTrack_RoomTypeID(RoomTypeId, ref PAXPASS, ref currency, ref Price);

                            string RoomType = RoomCodeID.Remove(RoomCodeID.Length - 4);

                            DataRow[] drRoomCateg = HotelsTrack_Response.Tables["RoomCateg"].Select("Code='" + RoomType + "'and BFType='" + MealId + "'");

                            if (drRoomCateg.Length > 0)
                            {
                                FinalPrice = Convert.ToDouble(drRoomCateg[0]["Price"]);
                                FinalPrice = FinalPrice * CurrencyFactorHotelsTrack;
                                FinalPrice = Convert.ToDouble(Math.Round(FinalPrice, 2));
                                priceCheckDto.FinalPrice = FinalPrice;
                                if (Convert.ToDouble(Math.Round(FinalPrice.TruncateDecimalPlaces(0))) > Math.Round(CompanyBuyingPrice.TruncateDecimalPlaces(0)))
                                {
                                    try
                                    {
                                        ResponseData = "#Price Changed " + "#Main Price = " + CompanyBuyingPrice + " #New Price = " + FinalPrice + " #Response= " + ResponseData;
                                        responsetype = "GeneralError";
                                        //int AgentId = Convert.ToInt32(Common.getSession("UserId") == "" ? "0" : Common.getSession("UserId"));
                                        _ = _commonMethod.SupplierPerformanceReportForPrebookAndBook(Supplier, "PreBook", AgentId, time, responsetype, xslRequest, ResponseData, Error);
                                    }
                                    catch (Exception e)
                                    {
                                        _ = _exception.ErrorNew(e, ErrorLog._LogAPI.Prebook);

                                    }
                                    priceCheckDto.Flag = true;
                                    return priceCheckDto;
                                }
                            }
                            else
                            {
                                try
                                {
                                    responsetype = "Success";
                                    //int AgentId = Convert.ToInt32(Common.getSession("UserId") == "" ? "0" : Common.getSession("UserId"));
                                    _ = _commonMethod.SupplierPerformanceReportForPrebookAndBook(Supplier, "PreBook", AgentId, time, responsetype, xslRequest, ResponseData, Error);
                                }
                                catch (Exception e)
                                {
                                    _ = _exception.ErrorNew(e, ErrorLog._LogAPI.Prebook);

                                }
                                priceCheckDto.Flag = false;
                                return priceCheckDto;
                            }
                        }
                        #endregion
                    }
                    else
                    {
                        try
                        {
                            responsetype = "GeneralError";
                            //int AgentId = Convert.ToInt32(Common.getSession("UserId") == "" ? "0" : Common.getSession("UserId"));
                            _ = _commonMethod.SupplierPerformanceReportForPrebookAndBook(Supplier, "PreBook", AgentId, time, responsetype, xslRequest, ResponseData, Error);
                        }
                        catch (Exception e)
                        {
                            _ = _exception.ErrorNew(e, ErrorLog._LogAPI.Prebook);

                        }
                        priceCheckDto.Flag = false;
                        return priceCheckDto;
                    }
                }
                #endregion
            }
            catch (Exception ex)
            {
                #region SupplierPerformanceReport Catch
                try
                {
                    Error = ex.Message.ToString() + "#=" + ex.StackTrace.ToString();
                    responsetype = "GeneralError";
                    //int AgentId = Convert.ToInt32(Common.getSession("UserId") == "" ? "0" : Common.getSession("UserId"));
                    _ = _commonMethod.SupplierPerformanceReportForPrebookAndBook(Supplier, "PreBook", AgentId, time, responsetype, xslRequest, ResponseData, Error);
                }
                catch (Exception e)
                {
                    _ = _exception.ErrorNew(e, ErrorLog._LogAPI.Prebook);

                }
                #endregion
                _ = _exception.ErrorNew(ex, ErrorLog._LogAPI.Prebook);

            }
            try
            {
                responsetype = "Success";
                //int AgentId = Convert.ToInt32(Common.getSession("UserId") == "" ? "0" : Common.getSession("UserId"));
                _ = _commonMethod.SupplierPerformanceReportForPrebookAndBook(Supplier, "PreBook", AgentId, time, responsetype, xslRequest, ResponseData, Error);
            }
            catch (Exception e)
            {
                _=_exception.ErrorNew(e, ErrorLog._LogAPI.Prebook);
            }

            priceCheckDto.Flag = false;
            return priceCheckDto;
        }

        public string Get_HotelsTrack_PriceCheckRQ(DataSet dsCartDetails, int UserCartID, string UserId, string UserName, string Password, string PaxPassport, string PropertyId, string RoomTypeId, string CheckIn, string CheckOut, string Type, string XMLCityId, string XMLCountryId, string InternalCode)
        {
            try
            {
                #region Price Check RQ
                string currency = "";
                string Price = "";
                string RoomType1 = Get_HotelsTrack_RoomTypeID(RoomTypeId, ref PaxPassport, ref currency, ref Price);

                RoomType1 = RoomType1.Remove(RoomType1.Length - 4);
                StringBuilder HotelsTrackPriceCheckRQ = new StringBuilder();
                HotelsTrackPriceCheckRQ.Append("<Service_SearchHotel>");
                HotelsTrackPriceCheckRQ.Append("<AgentLogin><AgentId>" + UserId + "</AgentId><LoginName>" + UserName + "</LoginName><Password>" + Password + "</Password></AgentLogin>");
                HotelsTrackPriceCheckRQ.Append("<SearchHotel_Request>");
                HotelsTrackPriceCheckRQ.Append("<hasKey>" + Type + "</hasKey>");
                HotelsTrackPriceCheckRQ.Append("<PaxPassport>" + PaxPassport + "</PaxPassport>");
                //  HotelsTrackPriceCheckRQ.Append("<RPCurrency>" + currency + "</RPCurrency>");
                HotelsTrackPriceCheckRQ.Append("<DestCountry>" + XMLCountryId + "</DestCountry>");
                HotelsTrackPriceCheckRQ.Append("<DestCity>" + XMLCityId + "</DestCity>");
                HotelsTrackPriceCheckRQ.Append("<HotelId InternalCode=\"" + InternalCode + "\">" + PropertyId + "</HotelId>");
                HotelsTrackPriceCheckRQ.Append("<ServiceCode>" + RoomType1 + "</ServiceCode>");
                HotelsTrackPriceCheckRQ.Append("<Period CheckIn=\"" + CheckIn + "\" CheckOut=\"" + CheckOut + "\"/>");

                DataRow[] drUserCartCount = dsCartDetails.Tables[1].Select("UserCartId='" + UserCartID + "'");
                int CartCount = Convert.ToInt32(drUserCartCount.Length);

                for (int ser = 0; ser < CartCount; ser++)
                {
                    string RoomType = "";
                    int NoOfAdult = 0;
                    NoOfAdult = Convert.ToInt32(drUserCartCount[ser]["NoOfAdults"]);
                    int NoOfChild = Convert.ToInt32(drUserCartCount[ser]["NoofChild"]);
                    NoOfAdult = NoOfAdult + NoOfChild;
                    RoomType = Get_HotelsTrack_RoomType(NoOfAdult);

                    string RQBedchild = "N";

                    HotelsTrackPriceCheckRQ.Append("<RoomInfo>");
                    HotelsTrackPriceCheckRQ.Append("<AdultNum RoomType=\"" + RoomType + "\" RQBedChild=\"" + RQBedchild + "\">" + NoOfAdult + "</AdultNum>");

                    HotelsTrackPriceCheckRQ.Append("</RoomInfo>");
                }
                HotelsTrackPriceCheckRQ.Append("<flagAvail>Y</flagAvail>");
                HotelsTrackPriceCheckRQ.Append("</SearchHotel_Request>");
                HotelsTrackPriceCheckRQ.Append("</Service_SearchHotel>");
                return HotelsTrackPriceCheckRQ.ToString();
                #endregion
            }
            catch (Exception ex)
            {
                _ =_exception.ErrorNew(ex,ErrorLog._LogAPI.Prebook);
                return "";
            }
        }

        public string Get_HotelsTrack_Booking_RQ(DataSet dsCartDetails, DataTable dtPassenger, DataRow[] HotelsTrackDetails, string ExternalRef, bool IsPriceUpdate, int i, int j, string HasKey_IntCode, string Nationality)
        {
            try
            {
                #region BookingRQ
                string UserID = Convert.ToString(HotelsTrackDetails[0]["UserId"]);
                string UserName = Convert.ToString(HotelsTrackDetails[0]["UserName"]);
                string Password = Convert.ToString(HotelsTrackDetails[0]["Password"]);
                string CheckIn = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["CheckInDate"]);
                CheckIn = CheckIn.Substring(0, 4) + "-" + CheckIn.Substring(4, 2) + "-" + CheckIn.Substring(6, 2);
                string CheckOut = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["CheckOutDate"]);
                CheckOut = CheckOut.Substring(0, 4) + "-" + CheckOut.Substring(4, 2) + "-" + CheckOut.Substring(6, 2);
                string MealId = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["MealPlanCode"]);
                string Hash_key = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["purchaseToken"]);


                string RoomType = "";
                string RoomTypeId = "";
                string currency = "";
                string Price = "";
                if (IsPriceUpdate)
                    RoomType = "";
                else
                {
                    RoomType = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomTypeId"]);
                    RoomTypeId = Get_HotelsTrack_RoomTypeID(RoomType, ref Nationality, ref currency, ref Price);
                    RoomTypeId = Convert.ToString(RoomTypeId.Remove(RoomTypeId.Length - 4));
                }
                string[] spitid = HasKey_IntCode.Split(new String[] { "$$$" }, StringSplitOptions.None);
                string InternalCode = "";
                InternalCode = Convert.ToString(HotelsTrackDetails[0]["Version"]);
                string HasKey = spitid[0].ToString();

                //if (spitid.Length > 1)
                //{
                //    InternalCode = spitid[1].ToString();
                //}

                string HotelId = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["XMLHotelId"]);
                int UserCartId = Convert.ToInt32(dsCartDetails.Tables[0].Rows[i]["UserCartID"]);
                // double Price = Convert.ToDouble(dsCartDetails.Tables[1].DefaultView[j]["CompanyBuyingPrice"]);

                string RoomTypeID = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomTypeId"]);
                string[] spitid1 = RoomTypeID.Split(new String[] { "$$$" }, StringSplitOptions.None);
                string RoomName = spitid1[4].ToString();

                DataRow[] CartResponse = dsCartDetails.Tables[1].Select("UserCartID='" + UserCartId + "'");
                string HasKey1 = Convert.ToString(CartResponse[0]["Eachotoken"]);
                string[] spitid12 = HasKey1.Split(new String[] { "$$$" }, StringSplitOptions.None);
                string ReqHaskey = spitid12[0].ToString();

                StringBuilder BookRQ = new StringBuilder();
                BookRQ.Append("<Service_BookHotel>");
                BookRQ.Append("<AgentLogin><AgentId>" + UserID + "</AgentId><LoginName>" + UserName + "</LoginName><Password>" + Password + "</Password></AgentLogin>");
                BookRQ.Append("<BookHotel>");
                BookRQ.Append("<RefRsvnNo/>");
                BookRQ.Append("<hasKey>" + ReqHaskey + "</hasKey>");
                BookRQ.Append("<OSRefNo>" + ExternalRef + "</OSRefNo>");
                BookRQ.Append("<PaxPassport>" + Nationality + "</PaxPassport>");
                // BookRQ.Append("<RPCurrency>" + currency + "</RPCurrency>");
                BookRQ.Append("<HotelList Seq=\"1\" InternalCode=\"" + InternalCode + "\" flagAvail=\"Y\">");
                BookRQ.Append("<HotelId>" + HotelId + "</HotelId>");
                BookRQ.Append("<RoomCatg CatgId=\"" + RoomTypeId + "\" CatgName=\"" + RoomName + "\" checkIn=\"" + CheckIn + "\" checkOut=\"" + CheckOut + "\" BFType=\"" + MealId + "\">");

                DataRow[] drCartPaasenger = dsCartDetails.Tables[1].Select("UserCartId='" + UserCartId + "'");

                for (int x = 0; x < drCartPaasenger.Length; x++)
                {
                    int NoOfAdult = 0;
                    int NoOfChild = 0;
                    int Childage1 = 0;
                    int Childage2 = 0;
                    NoOfAdult = Convert.ToInt32(drCartPaasenger[x]["NoOfAdults"]);
                    NoOfChild = Convert.ToInt32(drCartPaasenger[x]["NoOfChild"]);
                    Childage1 = Convert.ToInt32(drCartPaasenger[x]["NoOfChild"]);
                    Childage2 = Convert.ToInt32(drCartPaasenger[x]["NoOfChild"]);
                    NoOfAdult = NoOfAdult + NoOfChild;
                    string RoomTypeName = "";
                    RoomTypeName = Get_HotelsTrack_RoomType(NoOfAdult);
                    string RQBedchild = "N";

                    BookRQ.Append("<RoomType Seq=\"" + (x + 1) + "\" TypeName=\"" + RoomTypeName + "\" RQBedChild=\"" + RQBedchild + "\" Price=\"" + Math.Round(Convert.ToDouble(Price), 2) + "\">");
                    BookRQ.Append("<AdultNum>" + NoOfAdult + "</AdultNum>");

                    dtPassenger.DefaultView.RowFilter = "";
                    dtPassenger.DefaultView.RowFilter = "PickUp='" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomTypeID"]) + "' and RoomNo='" + (dsCartDetails.Tables[1].DefaultView[x]["RoomNumber"]) + "' and UserCartId='" + dsCartDetails.Tables[0].Rows[i]["UserCartId"] + "'";
                    BookRQ.Append("<PaxInformation>");
                    for (int y = 0; y < NoOfAdult; y++)
                    {
                        string Name = Convert.ToString(dtPassenger.DefaultView[y]["FirstName"]);
                        string SurName = Convert.ToString(dtPassenger.DefaultView[y]["LastName"]);
                        string PreFix = Convert.ToString(dtPassenger.DefaultView[y]["Prefix"]);

                        string FinalName = Convert.ToString(SurName + " " + Name + ", " + PreFix);

                        BookRQ.Append("<PaxInfo Id=\"" + (y + 1) + "\">" + FinalName + "</PaxInfo>");
                    }
                    BookRQ.Append("</PaxInformation>");
                    BookRQ.Append("</RoomType>");
                }
                BookRQ.Append("</RoomCatg>");
                BookRQ.Append("</HotelList>");
                BookRQ.Append("</BookHotel>");
                BookRQ.Append("</Service_BookHotel>");

                string BookingRQ = BookRQ.ToString();
                return BookingRQ;
                #endregion
            }
            catch (Exception ex)
            {
                _ = _exception.Error(ex);

                return "";
            }
        }

        public string Get_HotelsTrack_RoomTypeID(string RoomTypeId, ref string Nationality, ref string currency, ref string Price)
        {
            try
            {
                string[] spitid = RoomTypeId.Split(new String[] { "$$$" }, StringSplitOptions.None);
                string HotelsTrack_RoomId = spitid[0].ToString();
                Nationality = spitid[1].ToString();
                currency = spitid[2].ToString();
                Price = spitid[3].ToString();
                return HotelsTrack_RoomId;
            }
            catch (Exception ex)
            {
                _ = _exception.Error(ex);
                return "";
            }
        }

        public async Task<ResponseDto> Response_SupplierHotelsTrack(string Request_Text, string APIUrl, int SearchTimeOut, string SupplieName, bool DebugMode = false)
        {
            ResponseDto responseDto = new ResponseDto();
            string Error = "";
            try
            {
                System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
                timer.Start();


                WebRequest reqPOST = System.Net.WebRequest.Create(@"" + APIUrl + "");
                reqPOST.Method = "POST"; // Setting up Data transmitting method  into POST
                if (SupplieName.ToLower() == "travellanda" || SupplieName.ToLower() == "mgholiday" || SupplieName.ToLower() == "mgbedbank" || SupplieName.ToLower() == "hotelstrack" || SupplieName.ToLower() == "kliknbook_hotelstrack")
                    reqPOST.Headers.Add("Accept-Encoding", "gzip");
                ServicePointManager.DefaultConnectionLimit = 1000;
                reqPOST.Timeout = SearchTimeOut; // Setting up connection time out
                reqPOST.ContentType = "application/x-www-form-urlencoded"; // show content type
                byte[] sentData = Encoding.UTF8.GetBytes(Request_Text); // Parameters
                reqPOST.ContentLength = sentData.Length;
                Stream sendStream = reqPOST.GetRequestStream();
                sendStream.Write(sentData, 0, sentData.Length);
                sendStream.Close();
                WebResponse result =await reqPOST.GetResponseAsync();
                String responseString;
                using (Stream stream = result.GetResponseStream())
                {
                    if (SupplieName.ToLower() == "travellanda" || SupplieName.ToLower() == "mgholiday" || SupplieName.ToLower() == "whitesandsuae" || SupplieName.ToLower() == "mgbedbank" || SupplieName.ToLower() == "hotelstrack" || SupplieName.ToLower() == "kliknbook_hotelstrack")
                    {
                        HttpWebResponse Response = (HttpWebResponse)reqPOST.GetResponse();
                        Stream responseStream = Response.GetResponseStream();
                        if (SupplieName.ToLower() != "whitesandsuae")
                            responseStream = new GZipStream(responseStream, CompressionMode.Decompress);

                        StreamReader Reader = new StreamReader(responseStream, Encoding.UTF8);
                        responseString =await Reader.ReadToEndAsync();
                        responseStream.Close();
                        Reader.Close();
                        Response.Close();
                        timer.Stop();
                        //if (para != null)
                        //{
                        //    para.SupplierTime += ",SupplierName :" + SupplieName + " : " + "Execution Time :" + timer.Elapsed.ToString();
                        //    if (DebugMode)
                        //        Common.XMLRequestResponseTrack(Request_Text, responseString, SupplieName, "Proper");
                        //}

                    }
                    else
                    {
                        StreamReader reader = new StreamReader(stream, Encoding.UTF8);
                        responseString = reader.ReadToEnd();
                        JObject jsonObject = JObject.Parse(responseString);
                    }
                }
                responseDto.Response = responseString;
                return responseDto;
            }
            catch (Exception ex)
            {
                Error = ex.Message.ToString();
                responseDto.Error = Error;
                _ =_exception.Error(ex);
            }
            return responseDto;
        }
    }
}
