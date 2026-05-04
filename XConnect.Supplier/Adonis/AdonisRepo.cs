using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XConnect.CommonUtilities.Interface;
using XConnect.CommonUtilities;
using XConnect.Models;
using XConnect.Models.Hotel;
using XConnect.Supplier.Adonis.Interface;
using XConnect.Supplier.HotelDo;
using Microsoft.Extensions.Options;
using XConnect.ErrorLogHandler.Interface;
using XConnect.Markup.Interface;
using static Azure.Core.HttpHeader;
using Options = XConnect.Models.Options;
using System.Net;
using System.Xml.Linq;
using static XConnect.Supplier.Booking.Com.preebookrs;
using static XConnect.Models.ErrorLog;

namespace XConnect.Supplier.Adonis
{
    public class AdonisRepo : IAdonis
    {
        private readonly ICommonMethod _commonMethod;
        private readonly IException _exception;
        private readonly IMarkup _markup;
        private readonly ISupplierRes _supplierRes;
        private readonly KeySettings _keySettings;
        public AdonisRepo(ICommonMethod commonMethod, IMarkup markup, ISupplierRes supplierRes, IException exception, IOptions<KeySettings> keySettings)
        {
            _commonMethod = commonMethod;
            _markup = markup;
            _supplierRes = supplierRes;
            _exception = exception;
            _keySettings = keySettings.Value;
        }

        public void GetAdonisRequestOnly_async(DataRow[] commonCode, List<StarRating> listRating, HotelSearchParameter SearchPara, List<SupplierDetails> lstAdonisDetails, List<SupplierSearchConfig> lstAdonisSearchConfig, DataTable dtPreferredDataHotel, ref ConcurrentBag<HotelX> AllTask, int Hit, DataSet dtXMLCity, DataTable dtROE, TokenData tokenData)
        {
            try
            {
                if (lstAdonisDetails.Count > 0)
                {
                    List<StarRating> lstRating = new List<StarRating>();
                    lstRating = _commonMethod.GetStarRatingStringList_xtg(SearchPara);

                    List<HotelCodeByCity> DtHotelDAta = HtmlExtensions.ToList<HotelCodeByCity>(commonCode.CopyToDataTable());
                    int ratingcount = lstRating.Count;
                    int hotelcount = DtHotelDAta.Count;

                    foreach (SupplierDetails AdonisDetails in lstAdonisDetails)
                    {
                        int SupplierID = AdonisDetails.SupplierID;
                        string Supplier = AdonisDetails.SupplierName;

                        if (lstAdonisSearchConfig.Count > 0)
                        {
                            try
                            {
                                List<SupplierSearchConfig> AdonisSearchConfig = lstAdonisSearchConfig
                                  .Where(x => x != null && x.SupplierID == SupplierID)
                                  .ToList();


                                if (AdonisSearchConfig != null && AdonisSearchConfig.Count > 0)
                                {
                                    DataForSearch dataForSearch = _commonMethod.GetXmlDataForSearchTechwise(SearchPara.Nationality, dtXMLCity, Supplier, AdonisDetails.TechName);

                                    if (dataForSearch.XmlNationality != "")
                                    {
                                        string Ids = string.Empty;
                                        string xmlbase = "";
                                        _commonMethod.GetPrefferedHotelCodes(ref Ids, ref xmlbase, ref AdonisSearchConfig, Supplier, dtPreferredDataHotel, SearchPara.IsSearchFor);

                                        List<HotelCodeByCity> filter = new List<HotelCodeByCity>();
                                        filter = _commonMethod.FilterWithOutCity_Code(ratingcount, hotelcount, DtHotelDAta, lstRating, Supplier);


                                        #region "HotelDo"
                                        if (AdonisSearchConfig != null)
                                        {
                                            double CurrencyFactorAdonis = 1;
                                            string AdonisCurrency = AdonisDetails.CurrencyShortName;
                                            if (AdonisCurrency.ToLower() != Convert.ToString(tokenData.SiteCurrency).ToLower())
                                            {
                                                CurrencyFactorAdonis = _commonMethod.GetROEByCurrency(AdonisCurrency, dtROE);
                                            }
                                            for (int k = 0; k < AdonisSearchConfig.Count; k++)
                                            {
                                                bool DebugMode = Convert.ToInt32(AdonisSearchConfig[k].DebugMode) == 0 ? false : true;

                                                if (filter.Count > 0)
                                                {
                                                    if (Convert.ToInt32(AdonisSearchConfig[k].CityID) == 0 || Convert.ToInt32(AdonisSearchConfig[k].CityID) == SearchPara.CityID)
                                                    {
                                                        int NoOfRecord = Convert.ToInt32(AdonisSearchConfig[k].Noofrecords);
                                                        double count = Convert.ToDouble(filter.Count);
                                                        int NoOfDiv = Convert.ToInt32(Math.Ceiling(count / Convert.ToDouble(NoOfRecord)));
                                                        List<List<XMLID_Search>> lstxml = _commonMethod.GetXMLIDString(NoOfDiv, NoOfRecord, count, filter);

                                                        for (int a = 0; a < NoOfDiv; a++)
                                                        {
                                                            string IdsForSearch = Convert.ToString(_commonMethod.GetHotelCodeByCOmma(lstxml[a]));


                                                            HotelX xhotel = new HotelX();
                                                            xhotel.Supplier = Supplier;
                                                            xhotel.DebugeMode = DebugMode;
                                                            xhotel.SearchTimeOut = Convert.ToInt32(AdonisSearchConfig[k].HotelSearchTimeOut);
                                                            //if (Hit > 0)
                                                            xhotel.ProcessTimeout = Convert.ToInt32(AdonisSearchConfig[k].HotelSearchTimeOut2);
                                                            if (_keySettings.SupplierProxy == "0")
                                                                xhotel.URL = Convert.ToString(AdonisDetails.WebSiteURL) + "SearchHotels";
                                                            else
                                                                xhotel.URL = Convert.ToString(AdonisDetails.WebSiteURLforAPI) + "SearchHotels";

                                                            xhotel.UserId = Convert.ToString(AdonisDetails.UserID);
                                                            xhotel.UserName = Convert.ToString(AdonisDetails.UserName);
                                                            xhotel.PassWord = Convert.ToString(AdonisDetails.Password);
                                                            xhotel.CurrencyCode = AdonisCurrency;
                                                            xhotel.CurrencyFactor = CurrencyFactorAdonis;
                                                            xhotel.XmlCity = dataForSearch.xmlCity;
                                                            xhotel.XmlCountry = dataForSearch.XmlNationality;
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
                                                                objrq.Request = "prm_CurrentData=" + Get_Adonis_PriceOnly(SearchPara, xhotel.UserId, xhotel.UserName, xhotel.PassWord, xhotel.XmlCity, IdsForSearch, dataForSearch.XmlNationality);
                                                                objrq.Response = ""; RQRS.Add(objrq);
                                                            }
                                                            xhotel.RQRS = RQRS;
                                                            #endregion
                                                            xhotel.TechName = AdonisDetails.TechName;
                                                            xhotel.ReturnRooms = AdonisDetails.ReturnRooms;


                                                            AllTask.Add(xhotel);
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

        public string Get_Adonis_PriceOnly(HotelSearchParameter SearchParam, string UserId, string UserName, string Password, string RegionID, string HotelIds, string XmlBaseNationality)
        {
            try
            {
                #region Price Only and Room Only RQ
                string CheckIn = SearchParam.CheckInDate;
                CheckIn = CheckIn.Substring(6, 4) + "" + CheckIn.Substring(3, 2) + "" + CheckIn.Substring(0, 2);

                string CheckOut = SearchParam.CheckOutDate;
                CheckOut = CheckOut.Substring(6, 4) + "" + CheckOut.Substring(3, 2) + "" + CheckOut.Substring(0, 2);

                HotelIds = HotelIds.Replace(",", ", ");

                StringBuilder Jac_SRCH_RQ = new StringBuilder();
                Jac_SRCH_RQ.Append("<AdonisHotelSearchCriteriaDTO xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">");
                Jac_SRCH_RQ.Append("<CheckInDate>" + CheckIn + "</CheckInDate>");
                Jac_SRCH_RQ.Append("<CheckOutDate>" + CheckOut + "</CheckOutDate>");
                Jac_SRCH_RQ.Append("<HotelID>" + HotelIds + "</HotelID>");
                //Jac_SRCH_RQ.Append("<HotelID>72036,55517</HotelID>");
                Jac_SRCH_RQ.Append("<NationalityCode>" + XmlBaseNationality + "</NationalityCode>");
                Jac_SRCH_RQ.Append("<PaginationData>");
                Jac_SRCH_RQ.Append("<PageNumber>1</PageNumber>");
                Jac_SRCH_RQ.Append("<ItemsPerPage>50000</ItemsPerPage>");
                Jac_SRCH_RQ.Append("</PaginationData>");
                Jac_SRCH_RQ.Append("<RoomCriteria>");

                for (int x = 0; x < SearchParam.Rooms.Count; x++)
                {
                    Jac_SRCH_RQ.Append("<RoomCriteriaDTO>");
                    Jac_SRCH_RQ.Append("<AdultCount>" + SearchParam.Rooms[x].NoofAdults + "</AdultCount>");
                    Jac_SRCH_RQ.Append("<RoomCount>1</RoomCount>");
                    Jac_SRCH_RQ.Append("<ChildCount>" + SearchParam.Rooms[x].NoofChild + "</ChildCount>");

                    if (SearchParam.Rooms[x].NoofChild > 0)
                    {
                        if (SearchParam.Rooms[x].NoofChild == 1)
                        {
                            Jac_SRCH_RQ.Append("<ChildAges>");
                            Jac_SRCH_RQ.Append("<int>" + SearchParam.Rooms[x].Child1Age + "</int>");
                            Jac_SRCH_RQ.Append("</ChildAges>");
                        }
                        else
                        {
                            Jac_SRCH_RQ.Append("<ChildAges>");
                            Jac_SRCH_RQ.Append("<int>" + SearchParam.Rooms[x].Child1Age + "</int>");
                            Jac_SRCH_RQ.Append("<int>" + SearchParam.Rooms[x].Child2Age + "</int>");
                            Jac_SRCH_RQ.Append("</ChildAges>");
                        }
                    }
                    Jac_SRCH_RQ.Append("</RoomCriteriaDTO>");

                }
                Jac_SRCH_RQ.Append("</RoomCriteria>");
                Jac_SRCH_RQ.Append("<Credentials><ClientID>" + UserId + "</ClientID><Username>" + UserName + "</Username><Password>" + Password + "</Password></Credentials></AdonisHotelSearchCriteriaDTO>");

                return Jac_SRCH_RQ.ToString();
                #endregion
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
            }
            return string.Empty;
        }

        public async Task<List<Options>> Adonis_HotelSearchWithRooms(HotelX Item, CommonSearch commonSearch, string currency, string ResponseData, string CityCode, HotelSearchParameter SearchParam, double CurrencyFactorJacTravel, string Supplier, DataSet dsAgentDetails, Int32 processTimeout, string ResProccessTimerLogActive, string ResProccessTimeValueforSaveLog)
        {
            System.Diagnostics.Stopwatch timerroomx = new System.Diagnostics.Stopwatch();
            timerroomx.Start();

            List<Options> options = new List<Options>();

            DataTable DtHotel = _commonMethod.GenerateDataTableNewV2();
            try
            {
                if (ResponseData != string.Empty)
                {
                    DataSet Adonis_Response = _commonMethod.GetDataSetFromXMLString(ResponseData);
                    if (Adonis_Response == null || !Adonis_Response.Tables.Contains("HotelDTO") ||  Adonis_Response.Tables["HotelDTO"].Rows.Count < 1)
                    {
                        Item.ResponseStatus = 404;
                        Item.ResponseType = "NoCount";
                        return options;
                    }
                    string Token = Convert.ToString(Adonis_Response.Tables["AdonisHotelSearchResultDTO"].Rows[0]["Token"]);
                    DataTable DtAdonis_HotelRoom = _commonMethod.GenerateRoomtypeDataTableNewV2();

                    for (int h = 0; h <= Adonis_Response.Tables["HotelDTO"].Rows.Count - 1; h++)
                    {
                        Item.ResponseType = "Success";
                        string HotelID = Convert.ToString(Adonis_Response.Tables["HotelDTO"].Rows[h]["HotelCode"]);
                        int HotelDTOID = Convert.ToInt32(Adonis_Response.Tables["HotelDTO"].Rows[h]["HotelDTO_Id"]);
                        double FinalTotal = 0;
                        #region "When not Need Room"  
                        // add hotel code from price only and room code blank here
                        if (SearchParam.Filters.IsShowRooms == 0)
                        {
                            #region "Hotel Code Block "
                            DataRow drXmlHotel = DtHotel.NewRow();
                            drXmlHotel["XMLHotelCode"] = HotelID;
                            drXmlHotel["HotelName"] = "";
                            drXmlHotel["SupplierName"] = Item.Supplier;
                            drXmlHotel["OfficeID"] = 0;
                            drXmlHotel["ContractID"] = 0;
                            drXmlHotel["Rating"] = 0;
                            drXmlHotel["Reviews"] = 0;
                            drXmlHotel["UniqueNo"] = 0;
                            drXmlHotel["IsShowSupplier"] = false;
                            drXmlHotel["BookingStatus"] = "Available";
                            drXmlHotel["XMLCity"] = "";

                            double rate = 0, minRate = 0;
                            bool ProcessSkip = false;

                            DataRow[] drAvailableRoom = Adonis_Response.Tables["AvailableRoom"].Select("AvailableRoom_Id=" + HotelDTOID + "");
                            int AvailableRoom_Id = Convert.ToInt32(drAvailableRoom[0]["AvailableRoom_Id"]);

                            DataRow[] drAvailableRoomDTO = Adonis_Response.Tables["AvailableRoomDTO"].Select("AvailableRoom_Id=" + AvailableRoom_Id + "");

                            for (int i = 0; i < drAvailableRoomDTO.Length; i++)
                            {
                                int AvailableRoomDTO_Id = Convert.ToInt32(drAvailableRoomDTO[i]["AvailableRoomDTO_Id"]);

                                DataRow[] drHotelRoom = Adonis_Response.Tables["HotelRoom"].Select("AvailableRoomDTO_Id=" + AvailableRoomDTO_Id + "");
                                int HotelRoom_Id = Convert.ToInt32(drHotelRoom[0]["HotelRoom_Id"]);

                                DataRow[] drHotelRoomDTO = Adonis_Response.Tables["HotelRoomDTO"].Select("HotelRoom_Id=" + HotelRoom_Id + "");
                                int HotelRoomDTO_Id = Convert.ToInt32(drHotelRoomDTO[0]["HotelRoomDTO_Id"]);

                                DataRow[] drPrice = Adonis_Response.Tables["Price"].Select("HotelRoomDTO_Id=" + HotelRoomDTO_Id + "");

                                rate = Convert.ToDouble(Convert.ToString(drPrice[0]["Amount"]).Replace(",", "."));
                                if (i == 0)
                                    minRate = rate;
                                else
                                {
                                    if (rate < minRate)
                                        minRate = rate;
                                }
                                if (Convert.ToString(drPrice[0]["Currency"]) != "USD")
                                    ProcessSkip = true;
                            }
                            if (ProcessSkip)
                                break;
                            drXmlHotel["SupplierCurrency"] = currency;
                            drXmlHotel["FinalSellingPrice"] = minRate;
                            drXmlHotel["SellingPrice"] = minRate;
                            drXmlHotel["OfferText"] = "";
                            DtHotel.Rows.Add(drXmlHotel);
                            FinalTotal = minRate;
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
                                    DataTable dt_HotelRoom = _commonMethod  .GenerateRoomtypeDataTableNewV2();
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
                            #region "Hotel Code Block "
                            DataRow drXmlHotel = DtHotel.NewRow();
                            drXmlHotel["XMLHotelCode"] = HotelID;
                            drXmlHotel["HotelName"] = "";
                            drXmlHotel["SupplierName"] = Item.Supplier;
                            drXmlHotel["OfficeID"] = 0;
                            drXmlHotel["ContractID"] = 0;
                            drXmlHotel["Rating"] = 0;
                            drXmlHotel["Reviews"] = 0;
                            drXmlHotel["UniqueNo"] = 0;
                            drXmlHotel["IsShowSupplier"] = false;
                            drXmlHotel["BookingStatus"] = "Available";
                            drXmlHotel["XMLCity"] = "";

                            double rate = 0, minRate = 0;
                            bool ProcessSkip = false;

                            DataRow[] drAvailableRoom = Adonis_Response.Tables["AvailableRoom"].Select("AvailableRoom_Id=" + HotelDTOID + "");
                            int AvailableRoom_ID = Convert.ToInt32(drAvailableRoom[0]["AvailableRoom_Id"]);

                            DataRow[] drAvailableRoomDTO = Adonis_Response.Tables["AvailableRoomDTO"].Select("AvailableRoom_Id=" + AvailableRoom_ID + "");

                            for (int i = 0; i < drAvailableRoomDTO.Length; i++)
                            {
                                int AvailableRoomDTO_Id = Convert.ToInt32(drAvailableRoomDTO[i]["AvailableRoomDTO_Id"]);

                                DataRow[] drHotelRoom = Adonis_Response.Tables["HotelRoom"].Select("AvailableRoomDTO_Id=" + AvailableRoomDTO_Id + "");
                                int HotelRoom_Id = Convert.ToInt32(drHotelRoom[0]["HotelRoom_Id"]);

                                DataRow[] drHotelRoomDTO = Adonis_Response.Tables["HotelRoomDTO"].Select("HotelRoom_Id=" + HotelRoom_Id + "");
                                int HotelRoomDTO_Id = Convert.ToInt32(drHotelRoomDTO[0]["HotelRoomDTO_Id"]);

                                DataRow[] drPrice = Adonis_Response.Tables["Price"].Select("HotelRoomDTO_Id=" + HotelRoomDTO_Id + "");

                                rate = Convert.ToDouble(Convert.ToString(drPrice[0]["Amount"]).Replace(",", "."));
                                if (i == 0)
                                    minRate = rate;
                                else
                                {
                                    if (rate < minRate)
                                        minRate = rate;
                                }
                                if (Convert.ToString(drPrice[0]["Currency"]) != "USD")
                                    ProcessSkip = true;
                            }
                            if (ProcessSkip)
                                break;
                            drXmlHotel["SupplierCurrency"] = currency;
                            drXmlHotel["FinalSellingPrice"] = minRate;
                            drXmlHotel["SellingPrice"] = minRate;
                            drXmlHotel["OfferText"] = "";
                            DtHotel.Rows.Add(drXmlHotel);
                            FinalTotal = minRate;
                            #endregion
                            double Pricenew = minRate;
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
                                //DataTable dtHotelRoom = Common.GenerateRoomtypeDataTableNewV2();
                                DataTable dtCancellation = _commonMethod.GenerateCancellationDataTableNewV2();
                                DataTable DtRoomRateBreackUp = _commonMethod.GenerateoomRateBreackUpDataTableNewV2();

                                #region "Room Logic"
                                if (SearchParam.Filters.IsShowRooms == 1)
                                {
                                    #region
                                    double AgentPercentage = 0.0;
                                    Boolean blnIsShowSupplier = false;
                                    double AgentMarkupAmount = 0.0;
                                    Boolean isPerce = true;

                                    double AgentCompanyPercentage = Convert.ToDouble(SearchParam.AgentCompanyPercentage);
                                    bool IsB2C = Convert.ToBoolean(Convert.ToByte(SearchParam.IsB2BOrB2C));

                                    //_markup.getAgetMarkupSetting(Provider, GiataId.ToString(), commonSearch.dtSuplierMarkup, commonSearch.dtMarkupDetails, ref AgentPercentage, ref AgentMarkupAmount, ref blnIsShowSupplier, ref isPerce, commonSearch.AgentId, RatingHotel, SearchParam.CityID, SearchParam.CountryID.ToString(), SearchParam.CheckInDate);

                                    _markup.getAgetMarkupSettingNew_new(Provider, Convert.ToString(RaynaID), commonSearch.dtSuplierMarkup, commonSearch.dsMarkupDetails, ref AgentPercentage, ref AgentMarkupAmount, ref blnIsShowSupplier, ref isPerce, commonSearch.AgentId, RatingHotel, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), SearchParam.CheckInDate);
                                    int maxOcupancy = 0;

                                    int totalAdult = 0;
                                    int totalChild = 0;

                                    for (int b = 0; b < SearchParam.Rooms.Count; b++)
                                    {
                                        totalAdult = Convert.ToInt32(SearchParam.Rooms[b].NoofAdults) + totalAdult;
                                        totalChild = Convert.ToInt32(SearchParam.Rooms[b].NoofChild) + totalChild;
                                        maxOcupancy = maxOcupancy + SearchParam.Rooms[b].NoofChild + SearchParam.Rooms[b].NoofAdults;
                                    }
                                    #endregion
                                    #region Supplier Room Code block

                                    #region Room List
                                    if (Adonis_Response.Tables.Contains("AdonisHotelSearchResultDTO"))
                                    {
                                        if (Adonis_Response.Tables["AdonisHotelSearchResultDTO"].Columns.Contains("Token"))
                                        {
                                            //string Token = Convert.ToString(Adonis_Response.Tables["AdonisHotelSearchResultDTO"].Rows[0]["Token"]);
                                            if (Token != null)
                                            {
                                                //for (int h = 0; h <= Adonis_Response.Tables["HotelDTO"].Rows.Count - 1; h++)
                                                {
                                                    string HotelDTO_Id = Convert.ToString(Adonis_Response.Tables["HotelDTO"].Rows[h]["HotelDTO_Id"]);
                                                    string HotelCode = Convert.ToString(Adonis_Response.Tables["HotelDTO"].Rows[h]["HotelCode"]);

                                                    DataRow[] drRoomTypes = Adonis_Response.Tables["AvailableRoom"].Select("HotelDTO_Id='" + HotelDTO_Id + "'");
                                                    for (int i = 0; i < drRoomTypes.Length; i++)
                                                    {
                                                        string AvailableRoom_Id = Convert.ToString(drRoomTypes[i]["AvailableRoom_Id"]);
                                                        DataRow[] drRoomTypes1 = Adonis_Response.Tables["AvailableRoomDTO"].Select("AvailableRoom_Id='" + AvailableRoom_Id + "'");
                                                        for (int j = 0; j < drRoomTypes1.Length; j++)
                                                        {
                                                            string AvailableRoomDTO_Id = Convert.ToString(drRoomTypes1[j]["AvailableRoomDTO_Id"]);
                                                            DataRow[] drHotelOccupancy = Adonis_Response.Tables["HotelOccupancy"].Select("AvailableRoomDTO_Id='" + AvailableRoomDTO_Id + "'");

                                                            for (int m = 0; m < drHotelOccupancy.Length; m++)
                                                            {
                                                                string HotelOccupancy_Id = Convert.ToString(drHotelOccupancy[m]["HotelOccupancy_Id"]);
                                                                DataRow[] drOccupancy = Adonis_Response.Tables["Occupancy"].Select("HotelOccupancy_Id='" + HotelOccupancy_Id + "'");

                                                                string Adult = Convert.ToString(drOccupancy[0]["AdultCount"]);
                                                                string Child = Convert.ToString(drOccupancy[0]["ChildCount"]);
                                                                string TotalCount = Convert.ToString(Adult + "," + Child);

                                                                for (int o = 0; o < SearchParam.Rooms.Count; o++)
                                                                {
                                                                    string SearchAdult = Convert.ToString(SearchParam.Rooms[o].NoofAdults);
                                                                    string SearchChild = Convert.ToString(SearchParam.Rooms[o].NoofChild);
                                                                    string SearchTotalCount = Convert.ToString(SearchAdult + "," + SearchChild);

                                                                    if (TotalCount == SearchTotalCount)
                                                                    {
                                                                        AvailableRoomDTO_Id = Convert.ToString(drRoomTypes1[j]["AvailableRoomDTO_Id"]);
                                                                        DataRow[] drRoomTypes2 = Adonis_Response.Tables["HotelRoom"].Select("AvailableRoomDTO_Id='" + AvailableRoomDTO_Id + "'");

                                                                        for (int k = 0; k < drRoomTypes2.Length; k++)
                                                                        {
                                                                            string HotelRoom_Id = Convert.ToString(drRoomTypes2[k]["HotelRoom_Id"]);
                                                                            DataRow[] drRoomTypes3 = Adonis_Response.Tables["HotelRoomDTO"].Select("HotelRoom_Id='" + HotelRoom_Id + "'");

                                                                            for (int l = 0; l < drRoomTypes3.Length; l++)
                                                                            {
                                                                                string HotelRoomDTO_Id = Convert.ToString(drRoomTypes3[l]["HotelRoomDTO_Id"]);
                                                                                DataRow[] drRoomTypes4 = Adonis_Response.Tables["Price"].Select("HotelRoomDTO_Id='" + HotelRoomDTO_Id + "'");
                                                                                string PriceTemp = Convert.ToString(drRoomTypes4[0]["Amount"]).Replace(",", ".");
                                                                                double Price = Convert.ToDouble(PriceTemp);
                                                                                string Currency1 = Convert.ToString(drRoomTypes4[0]["Currency"]);
                                                                                if (Currency1.ToLower() != commonSearch.tokenData.SiteCurrency.ToLower())
                                                                                {
                                                                                    break;
                                                                                    //DataTable dtInrCurrency1 = Common.GetCountryCurrency(Currency1);
                                                                                    //double CurrencyFactorAdonis = Convert.ToDouble(dtInrCurrency1.Rows[0]["AmtToExch"]);

                                                                                    //Price = Price * CurrencyFactorAdonis;
                                                                                    //Factor = CurrencyFactorAdonis;
                                                                                }
                                                                                //else
                                                                                //Factor = 1;

                                                                                DataRow[] drRoomTypes5 = Adonis_Response.Tables["HotelRoomUniqueNumber"].Select("HotelRoomDTO_Id='" + HotelRoomDTO_Id + "'");
                                                                                string XMLServiceUniqueNumber = Convert.ToString(drRoomTypes5[0]["XmlServicesUniqueNumber"]);
                                                                                string AdonisUniqueNumber = Convert.ToString(drRoomTypes5[0]["AdonisUniqueNumber"]);
                                                                                string XmlServicesType = Convert.ToString(drRoomTypes5[0]["XmlServicesType"]);
                                                                                DataRow[] drRoomTypes6 = Adonis_Response.Tables["Board"].Select("HotelRoomDTO_Id='" + HotelRoomDTO_Id + "'");
                                                                                string Board = Convert.ToString(drRoomTypes6[0]["Name"]);
                                                                                DataRow[] drRoomTypes7 = Adonis_Response.Tables["RoomType"].Select("HotelRoomDTO_Id='" + HotelRoomDTO_Id + "'");
                                                                                string RoomTypeName = Convert.ToString(drRoomTypes7[0]["Name"]);
                                                                                string RoomTypeId = Token + "$$$" + XMLServiceUniqueNumber + "$$$" + AdonisUniqueNumber + "$$$" + XmlServicesType;

                                                                                #region Added by Jay for policy
                                                                                DataRow[] drCancellationPolicy = Adonis_Response.Tables["CancellationPolicy"].Select("HotelRoomDTO_Id='" + HotelRoomDTO_Id  + "'");
                                                                                int CancellationPolicy_Id = Convert.ToInt32(drCancellationPolicy[0]["CancellationPolicy_Id"]);
                                                                                DataRow[] drCancellationPolicyDTO = Adonis_Response.Tables["CancellationPolicyDTO"].Select("CancellationPolicy_Id='" + CancellationPolicy_Id + "'");


                                                                                DateTime MainDate = new DateTime();
                                                                                for (int p = 0; p < drCancellationPolicyDTO.Length; p++)
                                                                                {
                                                                                    string End = Convert.ToString(drCancellationPolicyDTO[p]["DateTimeFrom"]);
                                                                                    DateTime OnlyDate = Convert.ToDateTime(End.Substring(0, 4) + "/" + End.Substring(4, 2) + "/" + End.Substring(6, 2));

                                                                                    if (p == 0)
                                                                                        MainDate = OnlyDate;
                                                                                    else if (OnlyDate < MainDate)
                                                                                        MainDate = OnlyDate;
                                                                                }
                                                                                #endregion

                                                                                #region Rooms
                                                                                DataRow drAdonisRoom = DtAdonis_HotelRoom.NewRow();
                                                                                drAdonisRoom["OwnSystemHotelId"] = OwnsystemHotelId;
                                                                                drAdonisRoom["RoomTypeName"] = RoomTypeName;
                                                                                drAdonisRoom["RoomTypeCode"] = RoomTypeId;
                                                                                drAdonisRoom["MealId"] = Board;
                                                                                drAdonisRoom["MealName"] = Board;
                                                                                drAdonisRoom["ContractID"] = "0";
                                                                                drAdonisRoom["BoardType"] = Board;
                                                                                drAdonisRoom["RoomTypeId"] = RoomTypeId;
                                                                                drAdonisRoom["AdultsOccupancy"] = SearchAdult;
                                                                                drAdonisRoom["ChildOccupancy"] = SearchChild;
                                                                                drAdonisRoom["RoomNo"] = o + 1;
                                                                                drAdonisRoom["Type"] = RoomTypeId;
                                                                                drAdonisRoom["UniqueNo"] = HotelCode;
                                                                                drAdonisRoom["OfferDescription"] = "";
                                                                                drAdonisRoom["OfferCode"] = "";
                                                                                drAdonisRoom["EssentialInformation"] = "";
                                                                                drAdonisRoom["SupplierCurrency"] = Currency1;
                                                                                drAdonisRoom["FinalSellingPrice"] = Price;
                                                                                drAdonisRoom["FinalFromDate"] = MainDate; //Added by Jay on 090725
                                                                                DtAdonis_HotelRoom.Rows.Add(drAdonisRoom);
                                                                                #endregion


                                                                            }
                                                                        }
                                                                    }
                                                                    else
                                                                    {
                                                                        continue;
                                                                    }

                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                            }

                                            #region Adonis CombineRoom Process
                                            #region Room Combination Logic

                                            DataTable dtFinalRoom = DtAdonis_HotelRoom.Clone();
                                            DataTable dtHotelRoom = _commonMethod.GenerateRoomtypeDataTableNewV2();

                                            var Meal = DtAdonis_HotelRoom.DefaultView.ToTable(true, "MealName");
                                            var Room = DtAdonis_HotelRoom.DefaultView.ToTable(true, "RoomNo");
                                            //DataTable dtTemp = new DataTable();
                                            //dtTemp.Columns.Add("CobinationName");
                                            //dtTemp.Columns.Add("MealName");
                                            //dtTemp.Columns.Add("Price");
                                            //dtTemp.Columns.Add("RoomTypeID");
                                            //dtTemp.AcceptChanges();

                                            for (int a = 0; a < Meal.Rows.Count; a++)
                                            {

                                                DataView view = DtAdonis_HotelRoom.DefaultView;
                                                view.Sort = "RoomNo ASC";
                                                DataTable sortedDate = view.ToTable();
                                                DataRow[] drMeal = sortedDate.Select("MealName='" + Meal.Rows[a]["MealName"] + "'");
                                                var ResRoom = drMeal.Select(x => x.Field<string>("RoomNo")).Distinct();

                                                if (ResRoom.Count() == Room.Rows.Count)
                                                {
                                                    DataRow[] drRoom1 = sortedDate.Select("MealName='" + Meal.Rows[a]["MealName"] + "'and RoomNo=1");
                                                    DataRow[] drOther = sortedDate.Select("MealName='" + Meal.Rows[a]["MealName"] + "'and RoomNo<>1");
                                                    for (int O = 0; O < drRoom1.Count(); O++)
                                                    {
                                                        string RoomName = Convert.ToString(drRoom1[O]["RoomTypeName"]);
                                                        double RoomPrice = Convert.ToDouble(drRoom1[O]["FinalSellingPrice"]);
                                                        string RoomMeal = Convert.ToString(drRoom1[O]["MealName"]);
                                                        string RoomTypeID = Convert.ToString(drRoom1[O]["RoomTypeCode"]);
                                                        string HotelId = Convert.ToString(drRoom1[O]["UniqueNo"]);
                                                        string SupplierCurrency = Convert.ToString(drRoom1[O]["SupplierCurrency"]);
                                                        int RMNO = 1;
                                                        int ADTOCC = Convert.ToInt32(drRoom1[O]["AdultsOccupancy"]);
                                                        int CLDOCC = Convert.ToInt32(drRoom1[O]["ChildOccupancy"]);
                                                        //string[] strRoomTypeId = null;
                                                        List<string> strRoomTypeId = new List<string>();

                                                        strRoomTypeId.Add(RoomTypeID);

                                                        #region Added For Policy by Jay

                                                        List<DateTime> strRoomPolicy = new List<DateTime>();
                                                        DateTime FinalFromDate = Convert.ToDateTime(drRoom1[O]["FinalFromDate"]);
                                                        strRoomPolicy.Add(FinalFromDate);
                                                        #endregion


                                                        //DataRow drRoom = dtTemp.NewRow();
                                                        for (int b = 0; b < drOther.Count(); b++)
                                                        {
                                                            //RMNO = Convert.ToInt32(drOther[b]["RoomNo"]);
                                                            //RoomName = RoomName +" && "+ drOther[b]["RoomTypeName"];
                                                            //RoomPrice += Convert.ToDouble(drOther[b]["FinalSellingPrice"]);
                                                            //RoomMeal += drOther[b]["MealName"];
                                                            //for (int c = b+1; c < drOther.Count(); c++)
                                                            if (Convert.ToInt32(drOther[b]["RoomNo"]) != RMNO && !strRoomTypeId.Contains(Convert.ToString(drOther[b]["RoomTypeCode"])))
                                                            {
                                                                strRoomTypeId.Add(Convert.ToString(drOther[b]["RoomTypeCode"]));
                                                                RMNO = Convert.ToInt32(drOther[b]["RoomNo"]);
                                                                RoomName = RoomName + "$$$" + drOther[b]["RoomTypeName"];
                                                                RoomPrice += Convert.ToDouble(drOther[b]["FinalSellingPrice"]);
                                                                RoomTypeID = RoomTypeID + "^^^" + drOther[b]["RoomTypeCode"];
                                                                ADTOCC += Convert.ToInt32(drOther[b]["AdultsOccupancy"]);
                                                                CLDOCC += Convert.ToInt32(drOther[b]["ChildOccupancy"]);
                                                                //RoomMeal += drOther[c]["MealName"];

                                                                //drRoom["CobinationName"] = RoomName;
                                                                //drRoom["MealName"] = RoomMeal;
                                                                //drRoom["Price"] = RoomPrice;
                                                                //drRoom["RoomTypeID"] = RoomTypeID;
                                                                //break;

                                                                #region Added For Policy by Jay                                                                                                    
                                                                strRoomPolicy.Add(Convert.ToDateTime(drOther[b]["FinalFromDate"]));
                                                                #endregion
                                                            }
                                                        }
                                                        //dtTemp.Rows.Add(drRoom);

                                                        #region Find Cheapest date form Policy
                                                        DateTime MinDateTime = strRoomPolicy.Min();
                                                        #endregion

                                                        #region "Logic For Room Name "
                                                        string[] spitid = RoomName.Split(new String[] { "$$$" }, StringSplitOptions.None);

                                                        List<Room_EgyptExpress> roomlist = new List<Room_EgyptExpress>();
                                                        for (int dr = 0; dr < spitid.Length; dr++)
                                                        {
                                                            Room_EgyptExpress obj = new Room_EgyptExpress();
                                                            obj.RoomCount = 1;
                                                            obj.RoomName = Convert.ToString(spitid[dr]);
                                                            obj.status = true;
                                                            roomlist.Add(obj);
                                                        }
                                                        string RoomNameMain = "";// Convert.ToString(drHotelRoom[0]["Name"]);

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
                                                            if (RoomNameMain.IndexOf(roomlist[p].RoomName) < 0 || RoomNameMain == "")
                                                                RoomNameMain = RoomNameMain + roomlist[p].RoomCount + " - " + roomlist[p].RoomName + " ";
                                                        }
                                                        #endregion

                                                        #region RateBreakUp
                                                        //for (int n = 0; n < SearchParam.NoofNights; n++)
                                                        //{
                                                        //    DateTime Day = DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null);
                                                        //    Day = Day.AddDays(n);
                                                        //    string Days = Convert.ToString(Day);
                                                        //    string rateWeekColumn = Convert.ToString(Day.Date.DayOfWeek).Substring(0, 3);

                                                        //    #region Vat
                                                        //    Double VatOnRateBrackUp = 0;
                                                        //    double PriceRoom = Common.ProcessHotelRateBreakupPrice_WithMarkup(1, SearchParam.CityID, SearchParam.CountryId, RoomPrice / NoOfNight, B2CMarkup, IsParentId, isPerce, AgentPercentage, AgentMarkupAmount, SubAgentMarkup, ref VatOnRateBrackUp);
                                                        //    #endregion
                                                        //    DataRow drAdonis_RateBreackUp = DtRateBreackUp.NewRow();
                                                        //    drAdonis_RateBreackUp["SupplierName"] = "Adonis";
                                                        //    drAdonis_RateBreackUp["OwnsystemHotelId"] = ownsystemhotelId;
                                                        //    drAdonis_RateBreackUp["ContractID"] = HotelId;
                                                        //    drAdonis_RateBreackUp["RoomTypeName"] = RoomNameMain;
                                                        //    drAdonis_RateBreackUp["RoomTypeId"] = RoomTypeID;
                                                        //    drAdonis_RateBreackUp["MealId"] = RoomMeal;
                                                        //    drAdonis_RateBreackUp["Meal"] = RoomMeal;
                                                        //    drAdonis_RateBreackUp["DateOfWeek"] = rateWeekColumn;
                                                        //    drAdonis_RateBreackUp["Date"] = Days;
                                                        //    drAdonis_RateBreackUp["Rate"] = PriceRoom;
                                                        //    drAdonis_RateBreackUp["RateType"] = "A Rate";
                                                        //    drAdonis_RateBreackUp["RoomNo"] = 1;
                                                        //    drAdonis_RateBreackUp["RoomSequenceNo"] = ownsystemhotelId;
                                                        //    drAdonis_RateBreackUp["BookingStatus"] = "Available";
                                                        //    drAdonis_RateBreackUp["SupplierCurrency"] = SupplierCurrency;
                                                        //    drAdonis_RateBreackUp["BuyingPrice"] = PriceRoom / Factor;
                                                        //    DtRateBreackUp.Rows.Add(drAdonis_RateBreackUp);

                                                        //}
                                                        #endregion

                                                        #region Rooms
                                                        DataRow drAdonisRoom = dtHotelRoom.NewRow();
                                                        drAdonisRoom["OwnSystemHotelId"] = OwnsystemHotelId;
                                                        drAdonisRoom["RoomTypeName"] = RoomNameMain;
                                                        drAdonisRoom["RoomTypeCode"] = RoomTypeID;
                                                        drAdonisRoom["MealId"] = RoomMeal;
                                                        drAdonisRoom["MealName"] = RoomMeal;
                                                        drAdonisRoom["ContractID"] = "0";
                                                        drAdonisRoom["BoardType"] = RoomMeal;
                                                        drAdonisRoom["RoomTypeId"] = RoomTypeID;
                                                        drAdonisRoom["AdultsOccupancy"] = ADTOCC;
                                                        drAdonisRoom["ChildOccupancy"] = CLDOCC;
                                                        drAdonisRoom["MaxExtraBed"] = "0";
                                                        drAdonisRoom["RoomNo"] = 1;
                                                        drAdonisRoom["RoomSequenceNo"] = HotelId;
                                                        drAdonisRoom["Type"] = RoomTypeID;
                                                        drAdonisRoom["BookingStatus"] = "Available";
                                                        drAdonisRoom["UniqueNo"] = HotelId;
                                                        drAdonisRoom["OfferDescription"] = "";
                                                        drAdonisRoom["OfferCode"] = "";
                                                        drAdonisRoom["EssentialInformation"] = "";
                                                        double ItemPrice = Convert.ToDouble(RoomPrice);
                                                        _markup.ProcessHotelRoom_WithMarkup(CurrencyFactorJacTravel, currency, true, true, SearchParam.NoofNights, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), SearchParam.CheckInDate, SearchParam.CheckOutDate, ref ItemPrice, ref drAdonisRoom, isPerce, AgentPercentage, AgentMarkupAmount, commonSearch.SubAgentMarkup, commonSearch.B2CMarkup, commonSearch.IsParentId);
                                                        drAdonisRoom["SupplierName"] = Provider;
                                                        #endregion
                                                        dtHotelRoom.Rows.Add(drAdonisRoom);


                                                        #region Cancellation Policy
                                                        double cancellationAmount = ItemPrice;// Convert.ToDouble(dsHotelBeds[q].Tables["cancellationPolicies"].DefaultView[0]["amount"]);
                                                        double VatOnPolicy = 0;
                                                        #region Vat

                                                        //Int32 hotelRating = 0;
                                                        //hotelRating = Convert.ToInt32(dsHotel.Tables[0].Rows[0]["Rating"]);

                                                        double MainAgentCharge = cancellationAmount;
                                                        double CompanyChargeAmount = cancellationAmount;
                                                        //cancellationAmount = _markup.ProcessHotelCancelationPrice_WithMarkup(ref MainAgentCharge, SearchParam.NoofNights, SearchParam.CityID, SearchParam.CountryID.ToString(), cancellationAmount, isPerce, commonSearch.B2CMarkup, AgentPercentage, AgentMarkupAmount, commonSearch.IsParentId, commonSearch.SubAgentMarkup, ref VatOnPolicy);
                                                        #endregion

                                                        int CutOffDays = CacheMaster.CutOffDays;
                                                        // CutOffDays = Common.GetCutOffDays(commonSearch.dtSuplierMarkup, "HotelBedsV1");
                                                        CutOffDays = _commonMethod.GetCutOffDays(commonSearch.dtSuplierMarkup, Item.Supplier);

                                                        DateTime ToDate = MinDateTime.Date;
                                                        DateTime FromDate = DateTime.Now.Date;
                                                        if (ToDate.AddDays(CutOffDays).Date.AddDays(-1).Date > DateTime.Now.Date)
                                                            ToDate = ToDate.AddDays(CutOffDays).Date;
                                                        else
                                                            ToDate = DateTime.Now.Date;

                                                        if (ToDate > FromDate)
                                                        {
                                                            double zero = 0;

                                                            dtCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref zero, dtCancellation.NewRow(), 1, commonSearch.tokenData.SiteCurrency, Provider, Convert.ToString(OwnsystemHotelId), Convert.ToString(1), RoomTypeID, "", Convert.ToString(RoomMeal), DateTime.Now.Date, ToDate.AddDays(-1).AddHours(23).AddMinutes(59).AddSeconds(59), 0, 0, RoomMeal, 0, "", "", "", "", "", ""));
                                                            dtCancellation.AcceptChanges();

                                                        }
                                                        if (ToDate < FromDate)
                                                            ToDate = DateTime.Now.Date;

                                                        dtCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref CompanyChargeAmount, dtCancellation.NewRow(), 1, commonSearch.tokenData.SiteCurrency, Provider, Convert.ToString(OwnsystemHotelId), Convert.ToString(1), RoomTypeID, "", Convert.ToString(RoomMeal), ToDate, DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null), MainAgentCharge, cancellationAmount, RoomMeal, 0, "", "", "", "", "", ""));
                                                        dtCancellation.AcceptChanges();

                                                        #endregion
                                                    }
                                                }
                                                else
                                                    continue;
                                            }
                                            #endregion

                                            Options ops1 = (await _supplierRes.GetCommonRoomForSuppliers(GiataId, commonSearch.tokenData, commonSearch.advancedOptions, Pricenew, drXmlHotel, dtHotelRoom, dtCancellation, DtRoomRateBreackUp, SearchParam));
                                            if (ops1 != null)
                                                options.Add(ops1);
                                            #endregion
                                        }
                                    }
                                    #endregion
                                    #endregion
                                }
                                #endregion
                                #endregion

                            }
                            #endregion
                        }
                        #endregion
                    }




                }
                else
                {
                    Item.ResponseStatus = 204;
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
                    timerroomx.Stop();
                    Item.CodeProcessingTimeSupplier = timerroomx.ElapsedMilliseconds;
                    if (SearchParam != null && ResProccessTimerLogActive == "1" && timerroomx.ElapsedMilliseconds > Convert.ToInt32(ResProccessTimeValueforSaveLog))
                    {
                        
                        SearchParam.SupplierTime += ",|Finish:Adonis Process Response  " + "|Time :" + timerroomx.Elapsed.ToString();
                        _= Task.Run(() => _commonMethod.FireAway_SQL_ResPorcessTimeLog("", timerroomx.Elapsed.ToString(), Item.Supplier, SearchParam.SearchKey, 1, SearchParam.AgentID));
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

        public async Task<CancellationDto> Get_Adonis_CancellationPolicy(double CompanyBuy, string Type, DataSet dsHotel, string hotelcode, string ownsystemid, string RoomTypeId_Own, string MealID, string Board_Text, DataTable dtAgentDetail, DataTable dtSuplierMarkup, DataTable dtSearchConfig, HotelSearchParameter objSearPera, string roomno, string provider, int AgentId, DataTable dtMarkupDetails,string SearchKey, TokenData tokenData, string GiataId = "0", int hotelRating = 0, DataTable dtXmlNationality = null)
        {
            CancellationDto cancellationDto = new CancellationDto();
            string IsBooking = "Available";

            try
            {
                DataTable dtLOH_W2M_Cancellation = _commonMethod.GenerateCancellationDataTableNewV2();
                
                #region Cancellation Policy RQ
                Boolean isPerce = true;
                double AgentCompanyPercentage = Convert.ToDouble(objSearPera.AgentCompanyPercentage);
                bool IsB2C = Convert.ToBoolean(Convert.ToByte(objSearPera.IsB2BOrB2C));
                double B2CMarkup = (IsB2C == false ? AgentCompanyPercentage : XMLHotelBookingCommon.B2CMarkup);
                double AgentPercentage = 0.0;
                double AgentMarkupAmount = 0.0;
                double SubAgentMarkup = 0;
                int IsParentId = 1;
                Boolean blnIsShowSupplier = false;
                _markup.getAgetMarkupSetting(provider, GiataId, dtSuplierMarkup, dtMarkupDetails, ref AgentPercentage, ref AgentMarkupAmount, ref blnIsShowSupplier, ref isPerce, AgentId, hotelRating, objSearPera.CityID, Convert.ToString(objSearPera.CountryID), Convert.ToString(objSearPera.CheckInDate));
                if (dtAgentDetail != null)
                {
                    IsParentId = Convert.ToInt32(dtAgentDetail.Rows[0]["IsParentId"]);
                    SubAgentMarkup = Convert.ToDouble(dtAgentDetail.Rows[0]["B2CMarkupPer"]);
                }
                int CutOffDays = XMLHotelBookingCommon.CutOffDays;
                CutOffDays = _commonMethod.GetCutOffDays(dtSuplierMarkup, provider);
                DataRow[] drSuplierMarkup = (dtSuplierMarkup.Select("SupplierName='"+ provider+"'"));

                string Url = Convert.ToString(drSuplierMarkup[0]["WebSiteURL"]);
                string UserId = Convert.ToString(drSuplierMarkup[0]["UserId"]);
                string UserName = Convert.ToString(drSuplierMarkup[0]["UserName"]);
                string Password = Convert.ToString(drSuplierMarkup[0]["Password"]);
                string RoomName = "";

                string AdonisCurrency = Convert.ToString(drSuplierMarkup[0]["CurrencyShortName"]);
                double CurrencyFactorAdonis = 1;
                if (AdonisCurrency.ToLower() != tokenData.SiteCurrency.ToLower())
                {
                    CurrencyFactorAdonis = _commonMethod.GetROEByCurrency(AdonisCurrency, tokenData.dtROE);
                }


                string CancelPolicyRQ = "";
                CancelPolicyRQ = Get_Adonis_Policy_PriceCheckRQ(UserId, UserName, Password, RoomTypeId_Own);

                #endregion

                if (CancelPolicyRQ != null || CancelPolicyRQ != "")
                {
                    #region Cancellation Policy RS
                    string Error = "";
                    //string ResponseData = Get_Adonis_Response(ref Error, "prm_CurrentData=" + CancelPolicyRQ, Url + "BasketHotels", 10000, "Adonis");
                    ResponseDto  responseDto = await Get_Adonis_Response("prm_CurrentData=" + CancelPolicyRQ, Url + "BasketHotels", 10000, provider);
                    string ResponseData = responseDto.Response;
                    Error = responseDto.Error;

                    _commonMethod.ThirdPartyGetPolicyHistory(Convert.ToString(RoomTypeId_Own), Convert.ToString(ownsystemid), CancelPolicyRQ, Convert.ToString(ResponseData), "After Get Policy", provider, "Policy Response", "-", "WithinEarth");

                    if (ResponseData == string.Empty)
                    {
                        cancellationDto.dsHotelData = dsHotel;
                        cancellationDto.ISBooking = "NotAvailable";
                        return cancellationDto;
                    }

                    ResponseData = ResponseData.Replace("<HotelBasketItem>\r\n    <HotelBasketDTO", "<HotelBasketItem1>\r\n    <HotelBasketDTO");
                    ResponseData = ResponseData.Replace("HotelBasketDTO>\r\n  </HotelBasketItem>", "HotelBasketDTO>\r\n  </HotelBasketItem1>");
                    //ResponseData = ResponseData.Replace("<pricePerRoom><roomPrice", "<pricePerRoom><roomPrices");

                    DataSet LOH_W2M_Response = _commonMethod.GetDataSetFromXMLString(ResponseData);
                    #endregion
                    if (LOH_W2M_Response.Tables.Contains("Errors"))
                    {
                        cancellationDto.dsHotelData = dsHotel;
                        cancellationDto.ISBooking = "NotAvailable";
                        return cancellationDto;
                    }
                    else
                    {
                        #region DataRaw
                        string Remark = Convert.ToString(LOH_W2M_Response.Tables["HotelDTO"].Rows[0]["Remark"]);
                        //DataRow[] drDate = LOH_W2M_Response.Tables["HotelDTO"].Rows;
                        var drDate = LOH_W2M_Response.Tables["CancellationPolicyDTO"].DefaultView.ToTable(true, "DateTimeFrom");
                        DateTime MainDate = new DateTime();
                        for (int j = 0; j < drDate.Rows.Count; j++)
                        {
                            string End = Convert.ToString(drDate.Rows[j]["DateTimeFrom"]);
                            DateTime OnlyDate = Convert.ToDateTime(End.Substring(0, 4) + "/" + End.Substring(4, 2) + "/" + End.Substring(6, 2));

                            if (j == 0)
                                MainDate = OnlyDate;
                            else if (OnlyDate < MainDate)
                                MainDate = OnlyDate;
                        }

                        //DataRow[] drPreBookResponse = (LOH_W2M_Response.Tables["CancellationPolicyDTO"].Select("CancellationPolicy_Id='" + 0 + "'"));
                        //string Start = Convert.ToString(drPreBookResponse[0]["DateTimeFrom"]);
                        DateTime StartDate = MainDate;// Convert.ToDateTime(Start.Substring(0, 4) + "/" + Start.Substring(4, 2) + "/" + Start.Substring(6, 2));
                                                      //string End = Convert.ToString(drPreBookResponse[0]["DateTimeTo"]);
                                                      //DateTime EndDate = Convert.ToDateTime(End.Substring(0, 4) + "/" + End.Substring(4, 2) + "/" + End.Substring(6, 2));
                                                      //string Penalty1 = CompanyBuy;// Convert.ToString(drPreBookResponse[0]["Amount"]).Replace(",", ".");
                        double Penalty = Convert.ToDouble(CompanyBuy);

                        Penalty = Penalty * CurrencyFactorAdonis;
                        DateTime curDate = _commonMethod.GetTimeZoneDate(objSearPera.CurrentDate);
                        string uniqid = Guid.NewGuid().ToString();
                        #endregion

                        #region Cancellation Policy Logic and Row
                        DateTime CancelDate = DateTime.Now.Date;
                        CancelDate = StartDate;
                        #region Vat
                        double VatOnPolicy = 0;
                        double MainAgentCharge = Penalty;
                        double CompanyChargeAmount = Penalty;
                        Penalty = _markup.ProcessHotelCancelationPrice_WithMarkup(ref MainAgentCharge, objSearPera.NoofNights, objSearPera.CityID, Convert.ToString(objSearPera.CountryID), Penalty, isPerce, B2CMarkup, AgentPercentage, AgentMarkupAmount, IsParentId, SubAgentMarkup, ref VatOnPolicy);
                        #endregion

                        DateTime NowDate = DateTime.Now.Date;
                        if (CancelDate.AddDays(CutOffDays).Date > DateTime.Now.Date)
                        {
                            //EndDate = EndDate.AddDays(CutOffDays).Date;
                            //if (IsNRF)
                            //EndDate = DateTime.ParseExact(objSearPera.CheckInDate, "dd/MM/yyyy", null);
                            StartDate = StartDate.AddDays(CutOffDays).Date;
                            CancelDate = CancelDate.AddDays(CutOffDays).Date;
                        }
                        else
                        {
                            CancelDate = DateTime.Now.Date;
                            //IsNRF = true;
                            //EndDate = DateTime.ParseExact(objSearPera.CheckInDate, "dd/MM/yyyy", null);
                            Penalty = _markup.ProcessHotelCancelationPrice_WithMarkup(ref MainAgentCharge, objSearPera.NoofNights, objSearPera.CityID, Convert.ToString(objSearPera.CountryID), CompanyBuy, isPerce, B2CMarkup, AgentPercentage, AgentMarkupAmount, IsParentId, SubAgentMarkup, ref VatOnPolicy);
                        }
                        if (NowDate < CancelDate)
                        {

                            dtLOH_W2M_Cancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref CompanyChargeAmount, dtLOH_W2M_Cancellation.NewRow(), CurrencyFactorAdonis, AdonisCurrency, provider, ownsystemid, Convert.ToString(roomno),
                                                          Convert.ToString(RoomTypeId_Own), RoomName, MealID, Convert.ToDateTime(curDate), StartDate.AddDays(-1), 0, 0, "0", VatOnPolicy, uniqid, "NO", "", Remark != string.Empty ? Remark : "No Essential Information Available", "", ""));
                            dtLOH_W2M_Cancellation.AcceptChanges();

                            dtLOH_W2M_Cancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref CompanyChargeAmount, dtLOH_W2M_Cancellation.NewRow(), CurrencyFactorAdonis, AdonisCurrency, provider, ownsystemid, Convert.ToString(roomno),
                                                           Convert.ToString(RoomTypeId_Own), RoomName, MealID, Convert.ToDateTime(StartDate), DateTime.ParseExact(objSearPera.CheckInDate, "dd/MM/yyyy", null), MainAgentCharge, Penalty, "0", VatOnPolicy, uniqid, "NO", "", Remark != string.Empty ? Remark : "No Essential Information Available", "", ""));
                            dtLOH_W2M_Cancellation.AcceptChanges();
                        }
                        else
                        {
                            dtLOH_W2M_Cancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref CompanyChargeAmount, dtLOH_W2M_Cancellation.NewRow(), CurrencyFactorAdonis, AdonisCurrency, provider, ownsystemid, Convert.ToString(roomno),
                                                           Convert.ToString(RoomTypeId_Own), RoomName, MealID, curDate, DateTime.ParseExact(objSearPera.CheckInDate, "dd/MM/yyyy", null), MainAgentCharge, Penalty, "0", VatOnPolicy, uniqid, "Yes", "", Remark != string.Empty ? Remark : "No Essential Information Available"));
                            dtLOH_W2M_Cancellation.AcceptChanges();
                        }
                        //if (IsNRF)
                        //break;
                        #endregion
                    }
                }
                dsHotel.Tables[2].Merge(dtLOH_W2M_Cancellation);

                cancellationDto.dsHotelData = dsHotel;
                cancellationDto.ISBooking = IsBooking;
                return cancellationDto;
            }
            catch (Exception ex)
            {
                _ = _exception.ErrorNew(ex, _LogAPI.Recheck, SearchKey);

                cancellationDto.dsHotelData = dsHotel;
                cancellationDto.ISBooking = "NotAvailable";
                return cancellationDto;
            }
        }

        public string Get_Adonis_Policy_PriceCheckRQ(string UserId, string UserName, string Password, string RoomTypeId)
        {
            try
            {
                #region Cancellation Policy and Price Check RQ

                string[] MultiSplit = RoomTypeId.Split(new String[] { "^^^" }, StringSplitOptions.None);

                string[] spitidtoken = MultiSplit[0].Split(new String[] { "$$$" }, StringSplitOptions.None);
                string Token = spitidtoken[0].ToString();


                StringBuilder CancelPolicyRQ = new StringBuilder();
                CancelPolicyRQ.Append("<AdonisHotelBasketCriteriaDTO xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\">");
                CancelPolicyRQ.Append("<Token>" + Token + "</Token>");
                CancelPolicyRQ.Append("<HotelRoomUniqueNumbers>");
                CancelPolicyRQ.Append("<SelectHotelRoomDTO>");
                CancelPolicyRQ.Append("<HotelRoomUniqueNumbers>");


                for (int i = 0; i < MultiSplit.Length; i++)
                {
                    string[] spitid = null;
                    spitid = MultiSplit[i].Split(new String[] { "$$$" }, StringSplitOptions.None);

                    string XmlServicesUniqueNumber = spitid[1].ToString();
                    string AdonisUniqueNumber = spitid[2].ToString();
                    string XmlServicesType = spitid[3].ToString();

                    CancelPolicyRQ.Append("<HotelRoomUniqueNumberDTO>");
                    CancelPolicyRQ.Append("<XmlServicesUniqueNumber>" + XmlServicesUniqueNumber + "</XmlServicesUniqueNumber>");
                    CancelPolicyRQ.Append("<AdonisUniqueNumber>" + AdonisUniqueNumber + "</AdonisUniqueNumber>");
                    CancelPolicyRQ.Append("<XmlServicesType>" + XmlServicesType + "</XmlServicesType>");
                    CancelPolicyRQ.Append("</HotelRoomUniqueNumberDTO>");
                }

                CancelPolicyRQ.Append("</HotelRoomUniqueNumbers>");
                CancelPolicyRQ.Append("</SelectHotelRoomDTO>");
                CancelPolicyRQ.Append("</HotelRoomUniqueNumbers>");
                CancelPolicyRQ.Append("<Credentials>");
                CancelPolicyRQ.Append("<ClientID>" + UserId + "</ClientID>");
                CancelPolicyRQ.Append("<Username>" + UserName + "</Username>");
                CancelPolicyRQ.Append("<Password>" + Password + "</Password>");
                CancelPolicyRQ.Append("</Credentials>");
                CancelPolicyRQ.Append("</AdonisHotelBasketCriteriaDTO>");

                string searchRQString = CancelPolicyRQ.ToString();

                return searchRQString;
                #endregion
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
                return "";
            }

        }

        public async Task<ResponseDto> Get_Adonis_Response(string request, string TravcoURL, Int32 TimeOut, string Provider = "")
        {
            System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
            timer.Start();

            string Error = "";
            ResponseDto responseDto = new ResponseDto();


            string response = null;
            HttpWebRequest httpWebRequest = null;
            HttpWebResponse httpWebResponse = null;
            //Creates an HttpWebRequest for the specified URL.
            //httpWebRequest = (HttpWebRequest)WebRequest.Create("http://www.v3.staging.illusionswebservices.iwtx.com/illusionsHotelSearch.ashx");
            httpWebRequest = (HttpWebRequest)WebRequest.Create(TravcoURL);

            try
            {
                byte[] bytes;
                bytes = System.Text.Encoding.UTF8.GetBytes(request);
                //Set HttpWebRequest properties
                httpWebRequest.Method = "POST";
                httpWebRequest.Timeout = TimeOut;
                httpWebRequest.Headers.Add("Accept-Encoding", "gzip, deflate");
                httpWebRequest.Headers.Add("X-ClientId", "20d1a875-4809-452e-ae73-04921a9f0db5"); //29/05/24
                byte[] postBytes = Encoding.ASCII.GetBytes(request);
                httpWebRequest.ContentLength = postBytes.Length;
                ServicePointManager.DefaultConnectionLimit = 1000;
                httpWebRequest.ContentType = "application/x-www-form-urlencoded";
                httpWebRequest.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
                using (Stream requestStream = httpWebRequest.GetRequestStream())
                {
                    //Writes a sequence of bytes to the current stream                     
                    requestStream.Write(bytes, 0, bytes.Length);
                    requestStream.Close();//Close stream
                }
                //Sends the HttpWebRequest, and waits for a response.
                httpWebResponse = (HttpWebResponse)await httpWebRequest.GetResponseAsync();
                if (httpWebResponse.StatusCode == HttpStatusCode.OK)
                {
                    //Get response stream into StreamReader 
                    using (Stream responseStream = httpWebResponse.GetResponseStream())
                    {
                        using (StreamReader reader = new StreamReader(responseStream))
                            response = await reader.ReadToEndAsync();

                    }
                    timer.Stop();
                }
            }
            catch (WebException we)
            {   //TODO: Add custom exception handling
                //response = we.Message + we.InnerException;

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


            }
            catch (Exception ex)
            {
                Error = ex.Message.ToString();
                //response = ex.Message + ex.InnerException;
                _ = _commonMethod.XMLRequestResponseTrack(request, ex.Message, Provider == "" ? "Travco" : Provider, "Proper");
                _ = _exception.Error(ex);
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


            responseDto.Response = response;
            responseDto.Error = Error;

            return responseDto;
        }

        public async Task<PriceCheckAdonisDto> Get_Adonis_PriceCheckRQRS(DataSet dsCartDetails, DataRow[] AdonisDetails, int j, DataTable dtCheckPriceReqStructure, string UniqueNo, string AgentUniqueNo, double FinalPrice, bool isForBooking, double CompanyBuyingPrice, string PreBookingToken, string Searchkey = "", int AgentId = 0,TokenData tokenData=null)
        {
            PriceCheckAdonisDto priceCheckAdonisDto = new PriceCheckAdonisDto();

            string responsetype = "Success";
            string time = "";
            string SupplierName = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["SupplierName"]).ToLower();
            string Error = "";
            string xslRequest = "";
            string ResponseData = "";
            try
            {
                #region Price Check RQ and Declaration

                string AdonisCurrency = Convert.ToString(AdonisDetails[0]["CurrencyShortName"]);
                //DataTable dtInrCurrency = Common.GetCountryCurrency(AdonisCurrency);
                //double CurrencyFactorAdonis = Convert.ToDouble(dtInrCurrency.Rows[0]["AmtToExch"]);
                double CurrencyFactorAdonis = _commonMethod.GetROEByCurrency(AdonisCurrency, tokenData.dtROE);
                DataSet dsTemp = new DataSet();
                string AdonisUrl = Convert.ToString(AdonisDetails[0]["WebSiteURL"]);
                string UserId = Convert.ToString(AdonisDetails[0]["UserID"]);
                string UserName = Convert.ToString(AdonisDetails[0]["UserName"]);
                string Password = Convert.ToString(AdonisDetails[0]["Password"]);
                string HotelId = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["ContractID"]);
                int UserCartId = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[j]["UserCartID"]);
                int UserCartDetailId = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[j]["UserCartDetailId"]);
                string RoomTypeId = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomTypeID"]);

                xslRequest = Get_Adonis_Policy_PriceCheckRQ(UserId, UserName, Password, RoomTypeId);

                _commonMethod.ThirdPartyBookingHistory(Convert.ToString(UniqueNo), Convert.ToString(AgentUniqueNo), xslRequest, "", "Before get price " + SupplierName + " request hit " + j + "is for Price Check" + isForBooking, SupplierName, "response confirm", "-", "raynab2b");
                #endregion
                #region Price Check RS

                if (xslRequest != "")
                {
                    if (!isForBooking)
                    {
                        dtCheckPriceReqStructure.Rows.Add(_commonMethod.AddCheckPriceReq(dtCheckPriceReqStructure.NewRow(), j + 1, UserCartId, UserCartDetailId, RoomTypeId, SupplierName, xslRequest));
                    }
                    System.Diagnostics.Stopwatch timerroomprocess = new System.Diagnostics.Stopwatch();
                    timerroomprocess.Start();

                    ResponseDto responseDto = await Get_Adonis_Response("prm_CurrentData=" + xslRequest, AdonisUrl + "BasketHotels", 120000, "Adonis");
                    ResponseData = responseDto.Response;
                    Error = responseDto.Error;

                    timerroomprocess.Stop();
                    time = timerroomprocess.Elapsed.ToString();
                    //File.WriteAllText(@"D:\TestFolder\Adonis2\BasketHotels RQ_RS.txt", "BasketHotels Request : " + xslRequest + "BasketHotels Response : " + ResponseData);

                    _commonMethod.ThirdPartyBookingHistory(Convert.ToString(UniqueNo), Convert.ToString(AgentUniqueNo), xslRequest, ResponseData, "After get price " + SupplierName + " request hit " + j + "is for Price Check" + isForBooking, SupplierName, "response confirm", "-", "raynab2b");

                    if (ResponseData == string.Empty)
                    {
                        try
                        {
                            responsetype = "GeneralError";
                            string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, xslRequest, ResponseData, Error);
                        }
                        catch (Exception e)
                        {
                            _ = _exception.Error(e, Searchkey);
                        }
                        priceCheckAdonisDto.Flag = false;
                        return priceCheckAdonisDto;
                    }
                    ResponseData = XElement.Parse(ResponseData).ToString(SaveOptions.DisableFormatting); // Added on 16-SEP-2024
                                                                                                         //ResponseData = ResponseData.Replace("<HotelBasketItem>\r\n    <HotelBasketDTO", "<HotelBasketItem1>\r\n    <HotelBasketDTO");
                                                                                                         //ResponseData = ResponseData.Replace("HotelBasketDTO>\r\n  </HotelBasketItem>", "HotelBasketDTO>\r\n  </HotelBasketItem1>");

                    ResponseData = ResponseData.Replace("<HotelBasketItem><HotelBasketDTO", "<HotelBasketItem1><HotelBasketDTO");// Added on 18-SEP-2024
                    ResponseData = ResponseData.Replace("HotelBasketDTO></HotelBasketItem>", "HotelBasketDTO></HotelBasketItem1>");

                    DataSet Response = _commonMethod.GetDataSetFromXMLString(ResponseData);

                    #region Price Check Logic
                    if (Response.Tables.Contains("Errors"))
                    {
                        try
                        {
                            responsetype = "GeneralError";
                            string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, xslRequest, ResponseData, Error);
                        }
                        catch (Exception e)
                        {
                            _ = _exception.Error(e, Searchkey);
                        }
                        priceCheckAdonisDto.Flag = false;
                        return priceCheckAdonisDto;
                    }
                    if (Response.Tables["Price"].Rows.Count == 0)
                    {
                        try
                        {
                            responsetype = "GeneralError";
                            string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, xslRequest, ResponseData, Error);
                        }
                        catch (Exception e)
                        {
                            _ = _exception.Error(e, Searchkey);
                        }
                        priceCheckAdonisDto.Flag = false;
                        return priceCheckAdonisDto;
                    }
                    else
                    {
                        if (Response.Tables.Contains("HotelDTO"))
                        {
                            DataRow[] drPurchaseToken = (Response.Tables["HotelDTO"].Select("HotelDTO_Id='" + 0 + "'"));
                            PreBookingToken = Convert.ToString(drPurchaseToken[0]["PurchaseToken"]);
                            priceCheckAdonisDto.PreBookingToken = PreBookingToken;


                            double FinalPriceMain = 0;
                            for (int i = 0; i < Response.Tables["Price"].Rows.Count; i++)
                            {
                                DataRow[] drPreBookResponse = (Response.Tables["Price"].Select("HotelRoomDTO_Id='" + i + "'"));
                                string FinalPrice1 = Convert.ToString(drPreBookResponse[0]["Amount"]).Replace(",", ".");
                                string Currency = Convert.ToString(drPreBookResponse[0]["Currency"]);
                                double FinalPriceTemp = Convert.ToDouble(FinalPrice1);

                                if (Currency.ToLower() != "usd")
                                {
                                    //DataTable dtInrCurrency1 = _commonMethod.GetCountryCurrency(Currency);
                                    //double CurrencyFactorAdonis1 = Convert.ToDouble(dtInrCurrency1.Rows[0]["AmtToExch"]);
                                    double CurrencyFactorAdonis1 = _commonMethod.GetROEByCurrency(Currency, tokenData.dtROE);

                                    FinalPriceTemp = FinalPriceTemp * CurrencyFactorAdonis1;
                                }
                                else
                                    FinalPriceTemp = FinalPriceTemp * CurrencyFactorAdonis;
                                FinalPriceMain += FinalPriceTemp;
                            }
                            FinalPrice = Convert.ToDouble(FinalPriceMain);

                            FinalPrice = Convert.ToDouble(Math.Round(FinalPrice, 2));
                            priceCheckAdonisDto.FinalPrice = FinalPrice;

                            if (Convert.ToDouble(Math.Round(FinalPrice.TruncateDecimalPlaces(0))) > Math.Round(CompanyBuyingPrice.TruncateDecimalPlaces(0)))
                            {
                                try
                                {
                                    responsetype = "GeneralError";
                                    string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, xslRequest, ResponseData, Error);
                                }
                                catch (Exception e)
                                {
                                    _ = _exception.Error(e, Searchkey);
                                }
                                priceCheckAdonisDto.Flag = true;
                                return priceCheckAdonisDto;
                            }
                        }
                    }
                    #endregion
                }
                else
                {
                    try
                    {
                        responsetype = "GeneralError";
                        string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, xslRequest, ResponseData, Error);
                    }
                    catch (Exception e)
                    {
                        _ = _exception.Error(e, Searchkey);
                    }
                    priceCheckAdonisDto.Flag = false;
                    return priceCheckAdonisDto;
                }
                #endregion
            }
            catch (Exception e)
            {
                try
                {
                    Error = e.Message + e.StackTrace.ToString();
                    responsetype = "GeneralError";
                    string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, xslRequest, ResponseData, Error);
                }
                catch (Exception ex)
                {
                    _ = _exception.Error(ex, Searchkey);
                }
                _ = _exception.Error(e);
            }
            
            try
            {
                responsetype = "Success";
                string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, xslRequest, ResponseData, Error);
            }
            catch (Exception e)
            {
                _ = _exception.Error(e, Searchkey);
            }
            priceCheckAdonisDto.Flag = false;
            return priceCheckAdonisDto;
        }

        public string Get_Adonis_BookingRequest(DataSet dsCartDetails, DataTable dtPassengers, DataRow[] JacTravelDetails, string UniqueNo, bool IsPriceUpdate, int i, int j, string PreBookingToken, string AgentCode)
        {
            try
            {
                #region Booking RQ
                string UserID = Convert.ToString(JacTravelDetails[0]["UserId"]);
                string UserName = Convert.ToString(JacTravelDetails[0]["UserName"]);
                string Password = Convert.ToString(JacTravelDetails[0]["Password"]);
                string ExternalReference = "Book_" + Convert.ToString(AgentCode + "_" + UniqueNo);

                if (IsPriceUpdate)
                    PreBookingToken = "";

                string RoomType = "";
                RoomType = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomTypeId"]);

                string[] MultiSplit = RoomType.Split(new String[] { "^^^" }, StringSplitOptions.None);

                string[] spitidtoken = MultiSplit[0].Split(new String[] { "$$$" }, StringSplitOptions.None);
                string Token = spitidtoken[0].ToString();
                StringBuilder BookRQ = new StringBuilder();
                BookRQ.Append("<AdonisHotelConfirmCriteriaDTO xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns:xsi =\"http://www.w3.org/2001/XMLSchema-instance\">");
                BookRQ.Append("<ReferenceNumber>" + ExternalReference + "</ReferenceNumber>");
                BookRQ.Append("<Token>" + Token + "</Token>");
                BookRQ.Append("<PurchaseToken>");
                BookRQ.Append("<PurchaseDTO>");
                BookRQ.Append("<ConfirmPassenger>");

                for (int z = 0; z < dsCartDetails.Tables[1].DefaultView.Count; z++)
                {
                    BookRQ.Append("<ConfirmPassengerDTO>");
                    BookRQ.Append("<PurchaseToken>" + PreBookingToken + "</PurchaseToken>");
                    BookRQ.Append("<Occupancy>");
                    BookRQ.Append("<AdultCount>" + Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[z]["NoOfAdults"]) + "</AdultCount>");
                    BookRQ.Append("<ChildCount>" + Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[z]["NoOfChild"]) + "</ChildCount>");
                    BookRQ.Append("<Passengers>");
                    int ChildAge1 = 0;
                    int ChildAge2 = 0;
                    if (Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[z]["NoOfChild"]) == 1)
                        ChildAge1 = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[z]["ChildAge1"]);
                    else if (Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[z]["NoOfChild"]) == 2)
                    {
                        ChildAge1 = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[z]["ChildAge1"]);
                        ChildAge2 = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[z]["ChildAge2"]);
                    }

                    #region  IsLead Filter Logic
                    dtPassengers.DefaultView.RowFilter = "";
                    dtPassengers.DefaultView.RowFilter = "PickUp='" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[z]["RoomTypeID"]) + "' and RoomNo='" + (dsCartDetails.Tables[1].DefaultView[z]["RoomNumber"]) + "' and UserCartId='" + dsCartDetails.Tables[0].Rows[i]["UserCartId"] + "'";
                    dtPassengers.DefaultView.Sort = "IsLead desc";
                    #endregion
                    int ID = 0;
                    if (dtPassengers.DefaultView.Count > 0)
                    {
                        for (int p = 0; p < dtPassengers.DefaultView.Count; p++)
                        {
                            string FName = Convert.ToString(dtPassengers.DefaultView[p]["FirstName"]);
                            string LName = Convert.ToString(dtPassengers.DefaultView[p]["LastName"]);
                            string Prefix = Convert.ToString(dtPassengers.DefaultView[p]["Prefix"]);
                            string PaxType = Convert.ToString(dtPassengers.DefaultView[p]["PaxType"]);

                            DataRow[] drPassengers = dtPassengers.DefaultView.ToTable().Select("IsLead='1'");
                            //if (drPassengers.Length == 0)
                            //{
                            //    if (Convert.ToString(dtPassengers.DefaultView[p]["PaxType"]).Trim().ToLower() == "adult")
                            //        dtPassengers.DefaultView[p]["IsLead"] = 1;
                            //}
                            if (Convert.ToInt16(dtPassengers.DefaultView[p]["IsLead"]) == 1)
                            {
                                string SalutationID = "";
                                switch (Prefix.ToLower())
                                {
                                    case "mr.":
                                        SalutationID = "66F1C75C-DFF4-4305-99B7-19CDBDC9135A";
                                        break;
                                    case "mrs.":
                                        SalutationID = "67F1C75C-DFF4-4305-99B7-19CDBDC9135A";
                                        break;
                                    case "ms.":
                                        SalutationID = "68F1C75C-DFF4-4305-99B7-19CDBDC9135A";
                                        break;
                                    default:
                                        SalutationID = "";
                                        break;
                                }
                                BookRQ.Append("<PassengerDTO>");
                                BookRQ.Append("<IsLeader>true</IsLeader>");
                                BookRQ.Append("<ID>" + (ID += 1) + "</ID>");
                                BookRQ.Append("<SalutationID>" + SalutationID + "</SalutationID>");
                                BookRQ.Append("<Name>" + FName + "</Name>");
                                BookRQ.Append("<LastName>" + LName + "</LastName>");
                                BookRQ.Append("<Age>30</Age>");
                                BookRQ.Append("<AgeSpecified>true</AgeSpecified>");
                                BookRQ.Append("<PassengerType>" + PaxType + "</PassengerType>");
                                BookRQ.Append("</PassengerDTO>");
                            }
                            else
                            {
                                if (Convert.ToString(dtPassengers.DefaultView[p]["PaxType"]).Trim().ToLower() == "adult")
                                {
                                    string SalutationID = "";
                                    switch (Prefix.ToLower())
                                    {
                                        case "mr.":
                                            SalutationID = "66F1C75C-DFF4-4305-99B7-19CDBDC9135A";
                                            break;
                                        case "mrs.":
                                            SalutationID = "67F1C75C-DFF4-4305-99B7-19CDBDC9135A";
                                            break;
                                        case "ms.":
                                            SalutationID = "68F1C75C-DFF4-4305-99B7-19CDBDC9135A";
                                            break;
                                        default:
                                            SalutationID = "";
                                            break;
                                    }
                                    BookRQ.Append("<PassengerDTO>");
                                    BookRQ.Append("<IsLeader>false</IsLeader>");
                                    BookRQ.Append("<ID>" + (ID += 1) + "</ID>");
                                    BookRQ.Append("<SalutationID>" + SalutationID + "</SalutationID>");
                                    BookRQ.Append("<Name>" + FName + "</Name>");
                                    BookRQ.Append("<LastName>" + LName + "</LastName>");
                                    BookRQ.Append("<Age>30</Age>");
                                    BookRQ.Append("<AgeSpecified>true</AgeSpecified>");
                                    BookRQ.Append("<PassengerType>" + PaxType + "</PassengerType>");
                                    BookRQ.Append("</PassengerDTO>");
                                }
                                else
                                {
                                    int ChildAg2e = Convert.ToInt32(dtPassengers.DefaultView[p]["ChildAge"]);
                                    string SalutationID = "";
                                    switch (Prefix.ToLower())
                                    {
                                        case "mr.":
                                            SalutationID = "66F1C75C-DFF4-4305-99B7-19CDBDC9135A";
                                            break;
                                        case "mrs.":
                                            SalutationID = "67F1C75C-DFF4-4305-99B7-19CDBDC9135A";
                                            break;
                                        case "ms.":
                                            SalutationID = "68F1C75C-DFF4-4305-99B7-19CDBDC9135A";
                                            break;
                                        default:
                                            SalutationID = "";
                                            break;
                                    }
                                    BookRQ.Append("<PassengerDTO>");
                                    BookRQ.Append("<IsLeader>false</IsLeader>");
                                    BookRQ.Append("<ID>" + (ID += 1) + "</ID>");
                                    BookRQ.Append("<SalutationID>" + SalutationID + "</SalutationID>");
                                    BookRQ.Append("<Name>" + FName + "</Name>");
                                    BookRQ.Append("<LastName>" + LName + "</LastName>");
                                    BookRQ.Append("<Age>" + ChildAg2e + "</Age>");
                                    BookRQ.Append("<AgeSpecified>true</AgeSpecified>");
                                    BookRQ.Append("<PassengerType>" + PaxType + "</PassengerType>");
                                    BookRQ.Append("</PassengerDTO>");
                                }
                            }
                        }
                    }
                    BookRQ.Append("</Passengers>");
                    BookRQ.Append("</Occupancy>");

                    //for (int O = 0; O < MultiSplit.Length; O++)
                    {
                        string[] spitid = null;
                        spitid = MultiSplit[z].Split(new String[] { "$$$" }, StringSplitOptions.None);

                        string XmlServicesUniqueNumber = spitid[1].ToString();
                        string AdonisUniqueNumber = spitid[2].ToString();
                        string XmlServicesType = spitid[3].ToString();

                        BookRQ.Append("<HotelRoomUniqueNumber>");
                        BookRQ.Append("<XmlServicesUniqueNumber>" + XmlServicesUniqueNumber + "</XmlServicesUniqueNumber>");
                        BookRQ.Append("<AdonisUniqueNumber>" + AdonisUniqueNumber + "</AdonisUniqueNumber>");
                        BookRQ.Append("<XmlServicesType>" + XmlServicesType + "</XmlServicesType>");
                        BookRQ.Append("</HotelRoomUniqueNumber>");
                    }
                    BookRQ.Append("</ConfirmPassengerDTO>");
                }
                BookRQ.Append("</ConfirmPassenger>");
                BookRQ.Append("</PurchaseDTO>");
                BookRQ.Append("</PurchaseToken>");
                BookRQ.Append("<Credentials>");
                BookRQ.Append("<ClientID>" + UserID + "</ClientID>");
                BookRQ.Append("<Username>" + UserName + "</Username>");
                BookRQ.Append("<Password>" + Password + "</Password>");
                BookRQ.Append("</Credentials>");
                BookRQ.Append("</AdonisHotelConfirmCriteriaDTO>");
                string BookingRQ = BookRQ.ToString();

                return BookingRQ;
                #endregion
            }
            catch (Exception ex)
            {
                _ = _exception.ErrorNew(ex, _LogAPI.Book);
                return "";
            }
        }

        public string Get_Adonis_BookingDetailRQ(string AgentVoucherNo, DataRow[] SupplierDetails)
        {
            string CancellationRQ = "";
            try
            {
                #region Booking Cancel RQ
                string UserID = Convert.ToString(SupplierDetails[0]["UserId"]);
                string UserName = Convert.ToString(SupplierDetails[0]["UserName"]);
                string Password = Convert.ToString(SupplierDetails[0]["Password"]);

                StringBuilder CancelRQ = new StringBuilder();
                CancelRQ.Append("<AdonisHotelDetailCriteriaDTO xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\">");
                CancelRQ.Append("<FileNumber>" + AgentVoucherNo + "</FileNumber>");
                CancelRQ.Append("<Credentials>");
                CancelRQ.Append("<ClientID>" + UserID + "</ClientID>");
                CancelRQ.Append("<Username>" + UserName + "</Username>");
                CancelRQ.Append("<Password>" + Password + "</Password>");
                CancelRQ.Append("</Credentials>");
                CancelRQ.Append("</AdonisHotelDetailCriteriaDTO>");
                CancellationRQ = CancelRQ.ToString();
                #endregion
            }
            catch (Exception ex)
            {
                _ =_exception.ErrorNew(ex, _LogAPI.Book);
                return "";
            }
            return CancellationRQ;
        }

        public string Get_Adonis_BookingCancelRQ(string AgentVoucherNo, DataRow[] SupplierDetails)
        {
            string CancellationRQ = "";
            try
            {
                #region Booking Cancel RQ
                string UserID = Convert.ToString(SupplierDetails[0]["UserId"]);
                string UserName = Convert.ToString(SupplierDetails[0]["UserName"]);
                string Password = Convert.ToString(SupplierDetails[0]["Password"]);

                StringBuilder CancelRQ = new StringBuilder();
                CancelRQ.Append("<AdonisHotelCancelCriteriaDTO xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\">");
                CancelRQ.Append("<FileNumber>" + AgentVoucherNo + "</FileNumber>");
                CancelRQ.Append("<Credentials>");
                CancelRQ.Append("<ClientID>" + UserID + "</ClientID>");
                CancelRQ.Append("<Username>" + UserName + "</Username>");
                CancelRQ.Append("<Password>" + Password + "</Password>");
                CancelRQ.Append("</Credentials>");
                CancelRQ.Append("</AdonisHotelCancelCriteriaDTO>");
                CancellationRQ = CancelRQ.ToString();
                #endregion
            }
            catch (Exception ex)
            {
                _=_exception.ErrorNew(ex, _LogAPI.CancelBooking);
                return "";
            }
            return CancellationRQ;
        }
    }
}