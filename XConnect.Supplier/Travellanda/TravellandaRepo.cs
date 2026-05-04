using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.Extensions.Options;
using XConnect.CommonUtilities;
using XConnect.CommonUtilities.Interface;
using XConnect.ErrorLogHandler.Interface;
using XConnect.Markup.Interface;
using XConnect.Models;
using XConnect.Models.Hotel;
using XConnect.Supplier.Travellanda.Interface;
using static Azure.Core.HttpHeader;
using static XConnect.Models.ErrorLog;
using Options = XConnect.Models.Options;

namespace XConnect.Supplier.Travellanda
{
    public class TravellandaRepo : ITravellanda
    {
        private readonly ICommonMethod _commonMethod;
        private readonly IException _exception;
        private readonly IMarkup _markup;
        private readonly ISupplierRes _supplierRes;
        private readonly KeySettings _keySettings;



        public TravellandaRepo(ICommonMethod commonMethod, IMarkup markup, ISupplierRes supplierRes, IException exception, IOptions<KeySettings> keySettings)
        {
            _commonMethod = commonMethod;
            _markup = markup;
            _supplierRes = supplierRes;
            _exception = exception;
            _keySettings = keySettings.Value;
        }

        public void GetTravelandaRequestOnly_Async(DataRow[] commonCode, List<StarRating> listRating, HotelSearchParameter SearchPara, List<SupplierDetails> lstTravelLandaDetails, List<SupplierSearchConfig> lstTravelLandaSearchConfig, DataTable dtPreferredDataHotel, ref ConcurrentBag<HotelX> AllTask, int Hit, DataTable dtROE, TokenData tokenData, DataSet dtXMLCity)
        {
            try
            {
                List<StarRating> lstRating = new List<StarRating>();
                lstRating = _commonMethod.GetStarRatingStringList_xtg(SearchPara);

                List<HotelCodeByCity> DtHotelDAta = HtmlExtensions.ToList<HotelCodeByCity>(commonCode.CopyToDataTable());
                int ratingcount = lstRating.Count;
                int hotelcount = DtHotelDAta.Count;

                foreach (SupplierDetails TravelLandaDetails in lstTravelLandaDetails)
                {
                    try
                    {
                        int SupplierID = TravelLandaDetails.SupplierID;
                        string Supplier = TravelLandaDetails.SupplierName;

                        if (lstTravelLandaSearchConfig.Count > 0)
                        {
                            List<SupplierSearchConfig> TravelLandaSearchConfig = lstTravelLandaSearchConfig
                                .Where(x => x != null && x.SupplierID == SupplierID)
                                .ToList();

                            if (TravelLandaSearchConfig != null && TravelLandaSearchConfig.Count > 0)
                            {
                                DataForSearch dataForSearch = _commonMethod.GetXmlDataForSearchTechwise(SearchPara.Nationality, dtXMLCity, Supplier, TravelLandaDetails.TechName);

                                if (dataForSearch.XmlNationality != "")
                                {
                                    string Ids = string.Empty;
                                    string xmlbase = "";

                                    _commonMethod.GetPrefferedHotelCodes(ref Ids, ref xmlbase, ref TravelLandaSearchConfig, Supplier, dtPreferredDataHotel, SearchPara.IsSearchFor);

                                    List<HotelCodeByCity> filter = new List<HotelCodeByCity>();
                                    filter = _commonMethod.FilterWithOutCity_Code(ratingcount, hotelcount, DtHotelDAta, lstRating, Supplier);

                                    if (filter.Count > 0)
                                    {

                                        string Currency = TravelLandaDetails.CurrencyShortName;
                                        double CurrencyFactorTravelLanda = 1;
                                        if (Currency.ToLower() != tokenData.SiteCurrency.ToLower())
                                        {
                                            CurrencyFactorTravelLanda = _commonMethod.GetROEByCurrency(Currency, dtROE);
                                        }

                                        for (int k = 0; k < TravelLandaSearchConfig.Count; k++)
                                        {
                                            bool DebugMode = (Convert.ToInt32(TravelLandaSearchConfig[k].DebugMode) == 0 ? false : true);
                                            if (true)
                                            {

                                                if (Convert.ToInt32(TravelLandaSearchConfig[0].CityID) == 0 || Convert.ToInt32(TravelLandaSearchConfig[0].CityID) == SearchPara.CityID)//All City means 0
                                                {
                                                    {
                                                        int Noofrecords = Convert.ToInt32(TravelLandaSearchConfig[k].Noofrecords);
                                                        double count = Convert.ToDouble(filter.Count);
                                                        int NoOfDiv = Convert.ToInt32(Math.Ceiling(count / Convert.ToDouble(Noofrecords)));
                                                        List<List<XMLID_Search>> lstxml = _commonMethod.GetXMLIDString(NoOfDiv, Noofrecords, count, filter);

                                                        for (int a = 0; a < NoOfDiv; a++)
                                                        {

                                                            string IdsForSearch = Convert.ToString(_commonMethod.GetHotelCodeByCOmma(lstxml[a]));

                                                            HotelX xhotels = new HotelX();
                                                            xhotels.Supplier = Supplier;
                                                            xhotels.URL = TravellandaStaticInfo.TLHotelSearch;
                                                            xhotels.DebugeMode = DebugMode;
                                                            xhotels.SearchTimeOut = Convert.ToInt32(TravelLandaSearchConfig[k].HotelSearchTimeOut);
                                                            //if (Hit > 0)
                                                            xhotels.ProcessTimeout = Convert.ToInt32(TravelLandaSearchConfig[k].HotelSearchTimeOut2);
                                                            xhotels.CurrencyCode = Currency;
                                                            xhotels.CurrencyFactor = CurrencyFactorTravelLanda;
                                                            xhotels.XmlNationality = dataForSearch.XmlNationality;
                                                            xhotels.XmlCity = dataForSearch.xmlCity;
                                                            xhotels.XmlCountry = "";
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
                                                                objrq.Request = ("xml=" + XMLtravellandaRequestOnly_Async("1", "1", Convert.ToString(TravelLandaDetails.WebSiteURL), Convert.ToString(TravelLandaDetails.UserName), Convert.ToString(TravelLandaDetails.Password), Convert.ToInt32(TravelLandaSearchConfig[k].HotelSearchTimeOut), SearchPara, IdsForSearch, DebugMode, dataForSearch.XmlNationality, dataForSearch.xmlCity));
                                                                objrq.Response = ""; RQRS.Add(objrq);
                                                            }
                                                            xhotels.RQRS = RQRS;
                                                            #endregion
                                                            xhotels.TechName = TravelLandaDetails.TechName;
                                                            xhotels.ReturnRooms = TravelLandaDetails.ReturnRooms;

                                                            AllTask.Add(xhotels);
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

        public string XMLtravellandaRequestOnly_Async(string MinStar, string MaxStar, string APIUrl, string UserName, string Password, int SearchTimeOut, HotelSearchParameter SearchParam, string hotelIds, bool DebugMode, string Nationality, string citycode)
        {

            try
            {
                int isHit = 0;
                return GettravellandaHotelListRequest(SearchParam, citycode, UserName, Password, hotelIds, isHit, Nationality, 0);
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

        public string GettravellandaHotelListRequest(HotelSearchParameter searchParam, string citycode, string UserName, string Password, string hotelCode, int ishit, string TLNationality, int j)
        {
            try
            {

                string checkIn = String.Format("{0:yyyy-MM-dd}", DateTime.ParseExact(searchParam.CheckInDate, "dd/MM/yyyy", null));
                string checkOut = String.Format("{0:yyyy-MM-dd}", DateTime.ParseExact(searchParam.CheckOutDate, "dd/MM/yyyy", null));
                StringBuilder roomstring = new StringBuilder();
                XMLHotelBookingCommon objhotel = new XMLHotelBookingCommon();
                for (int i = 0; i < searchParam.Rooms.Count; i++)
                {
                    roomstring.Append("<Room>");
                    int TotalAdult = searchParam.Rooms[i].NoofAdults;
                    int TotalChild = searchParam.Rooms[i].NoofChild;
                    roomstring.Append("<NumAdults>" + TotalAdult + "</NumAdults>");
                    if (TotalChild >= 1)
                    {
                        roomstring.Append("<Children>");
                        switch (TotalChild)
                        {
                            case 1:
                                roomstring.Append("<ChildAge>" + Convert.ToString(searchParam.Rooms[i].Child1Age) + "</ChildAge>");
                                break;
                            case 2:
                                roomstring.Append("<ChildAge>" + Convert.ToString(searchParam.Rooms[i].Child1Age) + "</ChildAge>" + "<ChildAge>" + Convert.ToString(searchParam.Rooms[i].Child2Age) + "</ChildAge>");
                                break;
                        }
                        roomstring.Append("</Children>");
                    }
                    roomstring.Append("</Room>");
                }
                StringBuilder TLXML = new StringBuilder();
                TLXML.Append("<Request>");
                TLXML.Append("<Head>");
                TLXML.Append("<Username>" + UserName + "</Username>");
                TLXML.Append("<Password>" + Password + "</Password>");
                TLXML.Append("<RequestType>HotelSearch</RequestType>");
                TLXML.Append("</Head>");
                TLXML.Append("<Body>");
                if (hotelCode != string.Empty)
                {
                    if (ishit == 0)
                    {
                        TLXML.Append("<HotelIds>");
                        string[] Hotelid = hotelCode.Split(',');
                        for (int i = 0; i < Hotelid.Length; i++)
                        {
                            TLXML.Append("<HotelId>" + Hotelid[i] + "</HotelId>");
                        }
                        TLXML.Append("</HotelIds>");
                    }
                    else
                    {
                        TLXML.Append("<HotelIds>");
                        TLXML.Append("<HotelId>" + hotelCode + "</HotelId>");
                        TLXML.Append("</HotelIds>");
                    }
                }
                else
                {
                    TLXML.Append("<CityIds>");
                    TLXML.Append("<CityId>" + citycode + "</CityId>");
                    TLXML.Append("</CityIds>");
                }
                TLXML.Append("<CheckInDate>" + checkIn + "</CheckInDate>");
                TLXML.Append("<CheckOutDate>" + checkOut + "</CheckOutDate>");
                TLXML.Append("<Rooms>");
                TLXML.Append(roomstring.ToString());
                TLXML.Append("</Rooms>");
                TLXML.Append("<Nationality>" + TLNationality + "</Nationality>");
                TLXML.Append("<Currency>USD</Currency>");
                TLXML.Append("<AvailableOnly>1</AvailableOnly>");
                TLXML.Append("</Body>");
                TLXML.Append("</Request>");
                string TLREQ = TLXML.ToString();
                return TLREQ;
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
                return "";
            }
        }

        public async Task<List<Options>> Travellanda_HotelSearchWithRooms(HotelX Item, CommonSearch commonSearch, HotelSearchParameter SearchParam, string TLResponse, string XMLCITY, string XMLCOUNTRY, string Currency, double CurrencyFactor, Int32 processTimeout, string ResProccessTimerLogActive, string ResProccessTimeValueforSaveLog)
        {
            System.Diagnostics.Stopwatch timerroomx = new System.Diagnostics.Stopwatch();
            timerroomx.Start();
            List<Options> options = new List<Options>();
            DataTable DtHotel = _commonMethod.GenerateDataTableNewV2();
            try
            {
                if (TLResponse == null || TLResponse == string.Empty || TLResponse.ToLower().Contains("error"))
                {
                    Item.ResponseStatus = 404;
                    Item.ResponseType = "NoCount";
                    return options;
                }
                DataSet TLData = _commonMethod.GetDataSetFromXMLString(TLResponse);
                if (TLData.Tables["Hotel"] == null)
                {
                    Item.ResponseStatus = 404;
                    Item.ResponseType = "NoCount";
                    return options;
                }

                int counthotel = TLData.Tables["Hotel"].Rows.Count;
                if (counthotel > 0)
                {
                    Item.ResponseType = "Success";
                }
                int CutOffDays = XMLHotelBookingCommon.CutOffDays;
                CutOffDays = _commonMethod.GetCutOffDays(commonSearch.dtSuplierMarkup, Item.Supplier);
                for (int h = 0; h < counthotel; h++)
                {
                    // string ItemCode = Convert.ToString(TLData.Tables["Hotel"].Rows[h]["HotelId"]);
                    // string HotelName = Convert.ToString(TLData.Tables["Hotel"].Rows[h]["HotelName"]);
                    // int HotelId = Convert.ToInt32(TLData.Tables["Hotel"].Rows[h]["Hotel_Id"]);
                    //  string starRating = Convert.ToString(TLData.Tables["Hotel"].Rows[h]["StarRating"]);
                    //  string Stars = Common.TravelLandaStarRating(starRating);


                    #region "When not Need Room"  
                    // add hotel code from price only and room code blank here
                    if (SearchParam.Filters.IsShowRooms == 0)
                    {
                        #region "Hotel Code Block "
                        DataRow drXmlHotel = DtHotel.NewRow();
                        drXmlHotel["IsRecomondedHotel"] = false;
                        drXmlHotel["XMLHotelCode"] = TLData.Tables["Hotel"].Rows[h]["HotelId"];
                        drXmlHotel["ContractID"] = 0;
                        drXmlHotel["UniqueNo"] = TLData.Tables["Hotel"].Rows[h]["HotelId"];
                        drXmlHotel["IsShowSupplier"] = false;
                        drXmlHotel["HotelName"] = "";
                        drXmlHotel["Rating"] = 0;
                        string HotelStatus = string.Empty;
                        drXmlHotel["XmlHotelCity"] = XMLCITY;
                        drXmlHotel["XmlCity"] = XMLCITY;
                        drXmlHotel["XmlCountry"] = XMLCOUNTRY;
                        drXmlHotel["SupplierCurrency"] = Currency;

                        drXmlHotel["SupplierName"] = Item.Supplier;
                        double Pricenew = 1;
                        if (commonSearch.tokenData.AgentId != 1899)
                        {
                            DataRow[] droptions = TLData.Tables["Options"].Select("options_Id='" + TLData.Tables["Hotel"].Rows[h]["Hotel_Id"] + "'");
                            DataRow[] option = TLData.Tables["Option"].Select("options_Id='" + droptions[0]["options_Id"] + "'");
                            //string Offer_Text = "";
                            //if (TLData.Tables["Option"].Columns.Contains("DiscountApplied") == true)
                            //{
                            //    Offer_Text = Convert.ToString(option[0]["DiscountApplied"]);
                            //    if (Offer_Text == "")
                            //        drXmlHotel["IsOffer"] = false;
                            //    else
                            //    {
                            //        drXmlHotel["IsOffer"] = "true";
                            //        drXmlHotel["OfferText"] = Offer_Text + " % Discount Applied";
                            //    }
                            //}
                            //else
                            //    drXmlHotel["IsOffer"] = false;

                            Pricenew = Convert.ToDouble(option[0]["TotalPrice"]);
                        }
                        //Pricenew = Math.Round(Convert.ToDouble(Pricenew), 2);
                        drXmlHotel["SellingPrice"] = Pricenew;
                        drXmlHotel["FinalSellingPrice"] = Pricenew;
                        drXmlHotel["BookingStatus"] = HotelStatus;
                        #endregion


                        if (Pricenew > 0)
                        {
                            #region "Room code Block"
                            bool willAddOrNot = true;
                            Int64 RaynaID = 0;
                            Int64 GiataId = 0;
                            int Rating = 0;
                            Int64 OwnsystemHotelId = 0;
                            double MinPrice = Pricenew;
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
                        string Provider = Item.Supplier;
                        #region Hotels
                        DataRow drXmlHotel = DtHotel.NewRow();
                        drXmlHotel["IsRecomondedHotel"] = false;
                        drXmlHotel["XMLHotelCode"] = TLData.Tables["Hotel"].Rows[h]["HotelId"];
                        drXmlHotel["ContractID"] = 0;
                        drXmlHotel["UniqueNo"] = TLData.Tables["Hotel"].Rows[h]["HotelId"];
                        drXmlHotel["IsShowSupplier"] = false;
                        drXmlHotel["HotelName"] = "";
                        drXmlHotel["Rating"] = 0;
                        string HotelStatus = string.Empty;
                        drXmlHotel["XmlHotelCity"] = XMLCITY;
                        drXmlHotel["XmlCity"] = XMLCITY;
                        drXmlHotel["XmlCountry"] = XMLCOUNTRY;
                        drXmlHotel["SupplierCurrency"] = Currency;

                        drXmlHotel["SupplierName"] = Provider;

                        double Pricenew = 0;
                        drXmlHotel["SellingPrice"] = Pricenew;
                        drXmlHotel["FinalSellingPrice"] = Pricenew;
                        drXmlHotel["BookingStatus"] = HotelStatus;
                        #region "Hotel Block here"
                        #endregion
                        #region "GetRooms for this hotel"
                        bool willAddOrNot = true;
                        Int64 RaynaID = 0;
                        Int64 GiataId = 0;
                        int RatingHotel = 0;
                        Int64 OwnsystemHotelId = 0;
                       
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

                                //_markup.getAgetMarkupSetting(Provider, GiataId.ToString(), commonSearch.dtSuplierMarkup, commonSearch.dtMarkupDetails, ref AgentPercentage, ref AgentMarkupAmount, ref blnIsShowSupplier, ref isPerce, commonSearch.AgentId, RatingHotel, SearchParam.CityID, SearchParam.CountryID.ToString(), SearchParam.CheckInDate);

                                _markup.getAgetMarkupSettingNew_new(Provider, Convert.ToString(RaynaID), commonSearch.dtSuplierMarkup, commonSearch.dsMarkupDetails, ref AgentPercentage, ref AgentMarkupAmount, ref blnIsShowSupplier, ref isPerce, commonSearch.AgentId, RatingHotel, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), SearchParam.CheckInDate);
                                int maxOcupancy = 0;
                                for (int i = 0; i < SearchParam.Rooms.Count; i++)
                                    maxOcupancy = maxOcupancy + SearchParam.Rooms[i].NoofChild + SearchParam.Rooms[i].NoofAdults;
                                #region Supplier Room Code block
                                DataRow[] droptions = TLData.Tables["Options"].Select("options_Id='" + TLData.Tables["Hotel"].Rows[h]["Hotel_Id"] + "'");
                                DataRow[] option = TLData.Tables["Option"].Select("options_Id='" + droptions[0]["options_Id"] + "'");
                                for (int oo = 0; oo < option.Length; oo++)
                                {

                                    //if (oo > CacheMaster.RoomLimit_10)
                                    //    break;

                                    if (commonSearch.AgentId == 4440 || commonSearch.AgentId == 4901)
                                    {
                                        if (oo > CacheMaster.RoomLimit_20)
                                            break;
                                    }
                                    else
                                    {
                                        if (oo > CacheMaster.RoomLimit_10)
                                            break;
                                    }

                                    // string OptionId = Convert.ToString(option[oo]["OptionId"]);
                                    // string MealName = Convert.ToString(option[oo]["BoardType"]);
                                    // int option_Id = Convert.ToInt32(option[oo]["Option_Id"]);
                                    // double PriceWithMeal = Convert.ToDouble(option[oo]["TotalPrice"]);
                                    DataRow[] rooms = TLData.Tables["Rooms"].Select("option_Id='" + option[oo]["Option_Id"] + "'");
                                    DataRow[] room = TLData.Tables["Room"].Select("rooms_Id='" + rooms[0]["option_Id"] + "'");
                                    string roomTypeId = "", RoomType_Text = ""; ;
                                    List<Room_HotelBeds> roomlist = new List<Room_HotelBeds>();
                                    int Adult = 0;
                                    int Child = 0;
                                    string uniqno = Guid.NewGuid().ToString().Substring(0, 4);
                                    #region Room CombineLogic
                                    for (int k = 0; k < room.Length; k++)
                                    {
                                        Adult += Convert.ToInt32(room[k]["NumAdults"]);
                                        Child += Convert.ToInt32(room[k]["NumChildren"]);
                                        if (k == 0)
                                            roomTypeId = Convert.ToString(room[k]["RoomId"]);
                                        else
                                            roomTypeId = roomTypeId + "$$$" + Convert.ToString(room[k]["RoomId"]) + "$$$" + uniqno;


                                        Room_HotelBeds obj = new Room_HotelBeds();
                                        obj.RoomCount = Convert.ToInt32(1);
                                        obj.RoomName = Convert.ToString(room[k]["RoomName"]);
                                        obj.status = true;
                                        roomlist.Add(obj);
                                    }
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
                                        if (RoomType_Text.IndexOf(roomlist[p].RoomName) < 0 || RoomType_Text == "")
                                            RoomType_Text = RoomType_Text + roomlist[p].RoomCount + " X " + roomlist[p].RoomName + " ";
                                    }
                                    #endregion
                                    DataRow drHotelRoom = dtHotelRoom.NewRow();
                                    string roomWiseDetails = string.Empty;
                                    drHotelRoom["OwnsystemHotelId"] = OwnsystemHotelId;
                                    drHotelRoom["RoomTypeCode"] = Convert.ToString(option[oo]["BoardType"]);
                                    drHotelRoom["BoardType"] = "";
                                    //for (int j = 0; j < room.Length; j++)
                                    //{
                                    //    double TotalAmount = Convert.ToDouble(room[j]["RoomPrice"]);
                                    //    #region "RateBreakUp"
                                    //    for (int night = 0; night <= Convert.ToInt32(SearchParam.NoofNights) - 1; night++)
                                    //    {
                                    //        ratePerNight = TotalAmount / SearchParam.NoofNights;
                                    //        #region Vat
                                    //        VatOnPerNight = 0;
                                    //        ratePerNight = Common.ProcessHotelRateBreakupPrice_WithMarkup(1, SearchParam.CityID, SearchParam.CountryId, ratePerNight, B2CMarkup, IsParentId, isPerce, AgentPercentage, AgentMarkupAmount, SubAgentMarkup, ref VatOnPerNight);
                                    //        #endregion
                                    //        string newCheckInDate = Convert.ToString(SearchParam.CheckInDate);
                                    //        string newCheckOutDate = Convert.ToString(SearchParam.CheckOutDate);
                                    //        DateTime dtCheckInDate = DateTime.ParseExact(newCheckInDate, "dd/MM/yyyy", null);
                                    //        DateTime dtCheckOutDate = DateTime.ParseExact(newCheckOutDate, "dd/MM/yyyy", null);
                                    //        ratePerNight = Math.Round(ratePerNight, 2);
                                    //        DataRow drRateBreackUp = DttravellandaHotelRoomRateBreackUp.NewRow();
                                    //        drRateBreackUp["SupplierName"] = "travellanda";
                                    //        drRateBreackUp["OwnsystemHotelId"] = ownsystemhotelId;
                                    //        drRateBreackUp["ContractID"] = 0;
                                    //        drRateBreackUp["RoomNo"] = 1;
                                    //        drRateBreackUp["RoomTypeId"] = roomTypeId;
                                    //        drRateBreackUp["MealId"] = OptionId;
                                    //        drRateBreackUp["Meal"] = MealName;
                                    //        string rateWeekColumn = Convert.ToString(dtCheckInDate.Date.AddDays(night).DayOfWeek).Substring(0, 3);
                                    //        drRateBreackUp["DateOfWeek"] = rateWeekColumn;
                                    //        DateTime rateDate = dtCheckInDate.Date.AddDays(night);
                                    //        drRateBreackUp["Date"] = rateDate;
                                    //        drRateBreackUp["Rate"] = ratePerNight;
                                    //        drRateBreackUp["OrderNo"] = 1;
                                    //        drRateBreackUp["RateType"] = (j + 1) + "_" + Convert.ToString(room[j]["RoomName"]);
                                    //        drRateBreackUp["RoomTypeName"] = Convert.ToString(room[j]["RoomName"]);
                                    //        drRateBreackUp["DateOfWeek"] = rateWeekColumn;
                                    //        drRateBreackUp["Date"] = rateDate;
                                    //        drRateBreackUp["BookingStatus"] = "Available";
                                    //        drRateBreackUp["BoardTypeID"] = roomTypeId;
                                    //        DtRoomRateBreackUp.Rows.Add(drRateBreackUp);
                                    //        DtRoomRateBreackUp.AcceptChanges();
                                    //    }
                                    //    #endregion
                                    //}
                                    drHotelRoom["AdultsOccupancy"] = Adult;
                                    drHotelRoom["ChildOccupancy"] = Child;
                                    drHotelRoom["MealName"] = Convert.ToString(option[oo]["BoardType"]);
                                    drHotelRoom["MealId"] = Convert.ToString(option[oo]["OptionId"]);
                                    drHotelRoom["Type"] = "";
                                    drHotelRoom["Board"] = "";
                                    drHotelRoom["BookingStatus"] = "Available";
                                    drHotelRoom["UniqueNo"] = uniqno;
                                    drHotelRoom["ContractID"] = 0;
                                    drHotelRoom["RoomNo"] = 1;
                                    drHotelRoom["RoomTypeId"] = roomTypeId;
                                    drHotelRoom["RoomSequenceNo"] = "";
                                    drHotelRoom["OfferDescription"] = "";
                                    drHotelRoom["OfferCode"] = "";
                                    drHotelRoom["Characteristic"] = "";
                                    drHotelRoom["EssentialInformation"] = "N/A";
                                    double itemprice = Convert.ToDouble(option[oo]["TotalPrice"]);
                                    //itemprice = Math.Round(itemprice, 2);
                                    #region Vat
                                    _markup.ProcessHotelRoom_WithMarkup(CurrencyFactor, Currency, true, true, SearchParam.NoofNights, SearchParam.CityID, SearchParam.CountryID.ToString(), SearchParam.CheckInDate, SearchParam.CheckOutDate, ref itemprice, ref drHotelRoom, isPerce, AgentPercentage, AgentMarkupAmount, commonSearch.SubAgentMarkup, commonSearch.B2CMarkup, commonSearch.IsParentId);
                                    #endregion
                                    drHotelRoom["SupplierName"] = Provider;
                                    drHotelRoom["BoardTypeID"] = roomTypeId;
                                    drHotelRoom["RoomTypeName"] = RoomType_Text;
                                    dtHotelRoom.Rows.Add(drHotelRoom);
                                }

                                #endregion

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
                //DtHotel.DefaultView.RowFilter = "FinalSellingPrice>0";
                // if (DtHotel.DefaultView.Count > 0)
                return options;
                // else
                //  return Common.GenerateDataTableNewV2();
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
                       
                        SearchParam.SupplierTime += ",|Finish:Travellanda Process Response  " + "|Time :" + timerroomx.Elapsed.ToString();
                         _= Task.Run(() => _commonMethod.FireAway_SQL_ResPorcessTimeLog("", timerroomx.Elapsed.ToString(), Item.Supplier, SearchParam.SearchKey, 1, SearchParam.AgentID));
                    }
                }
                catch (Exception x)
                {
                    await _exception.Error(x);
                }
                #endregion
                //GC.Collect();
            }
            //});
        }

        public class Room_HotelBeds
        {
            public int RoomCount { get; set; }
            public string RoomName { get; set; }
            public bool status { get; set; }
        }

        //recheck
        public async Task<CancellationDto> GetCancellationPollicyTravellanda(string Provider, int roomno, DataSet dsHotel, string ownsystemid, string roomtypeid, DataTable dtAgentDetail, DataTable dtSuplierMarkup, DataTable dtMarkupDetails, DataTable dtSearchConfig, HotelSearchParameter SearchPara, string OptionCode, int AgentId, string SearchKey, string GiataId = "0", int hotelRating = 0)
        {
            CancellationDto cancellationDto = new CancellationDto();
            DataTable dtHotelCancellation = _commonMethod.GenerateCancellationDataTableNewV2();
            int CutOffDays = XMLHotelBookingCommon.CutOffDays;
            CutOffDays = _commonMethod.GetCutOffDays(dtSuplierMarkup, Provider);
            try
            {
                //  for (int c = 0; c < dsHotel.Tables[1].DefaultView.Count; c++)
                {
                    DataRow[] drSuplierMarkup = (dtSuplierMarkup.Select("SupplierName='"+ Provider + "'"));
                    string UserName = Convert.ToString(drSuplierMarkup[0]["UserName"]);
                    string Password = Convert.ToString(drSuplierMarkup[0]["Password"]);
                    string APIUrl = Convert.ToString(drSuplierMarkup[0]["WebSiteURL"]);
                    string Currency = Convert.ToString(drSuplierMarkup[0]["CurrencyShortName"]);
                    int SID = Convert.ToInt32(drSuplierMarkup[0]["SupplierID"]);
                    DataRow[] drSearchConfig = (dtSearchConfig.Select("SupplierID='" + SID + "'"));
                    int SearchTimeOut = Convert.ToInt32(drSearchConfig[0]["BookingConfirmationTimeout"]);
                    string TLRequest = GetTravelLandaPolicyRequest(UserName, Password, OptionCode);
                    string sentData = "xml=" + TLRequest;
                    string Error = "";
                    //string TLResponse =  _commonMethod.Response_Supplier(ref Error, sentData, APIUrl, SearchTimeOut, "TravelLanda");
                    ResponseDto responseDto = await  _commonMethod.Response_Supplier(sentData, APIUrl, SearchTimeOut, Provider);
                    string TLResponse = responseDto.Response;
                    Error = responseDto.Error;


                    _commonMethod.ThirdPartyGetPolicyHistory(Convert.ToString(roomtypeid), Convert.ToString(ownsystemid), sentData, Convert.ToString(TLResponse), "After Get Policy", Provider, "Response", "-", "");

                    if (TLResponse == null || TLResponse == string.Empty || TLResponse.ToLower().Contains("error"))
                    {
                        cancellationDto.dsHotelData = dsHotel;
                        cancellationDto.ISBooking = "NotAvailable";
                        return cancellationDto;

                    }
                    DataSet TLData = _commonMethod.GetDataSetFromXMLString(TLResponse);
                    #region "Cancelation Policy OLD"
                    //string Deadline = Convert.ToString(TLData.Tables["Body"].Rows[0]["CancellationDeadline"]);
                    //double TotalPriceRoom = Convert.ToDouble(TLData.Tables["Body"].Rows[0]["TotalPrice"]);
                    //TotalPriceRoom = TotalPriceRoom;
                    //string Essen = "";
                    //if (TLData.Tables.Contains("Policy") == true)
                    //{
                    //    DateTime FromDate = DateTime.Now.Date;
                    //    for (int i = 0; i < TLData.Tables["Policy"].Rows.Count; i++)
                    //    {
                    //        int policyID = Convert.ToInt32(TLData.Tables["Policy"].Rows[i]["policies_Id"]);
                    //        if (TLData.Tables.Contains("Alert") == true)
                    //        {
                    //            DataRow[] AlertTable = TLData.Tables["Alert"].Select("Alerts_Id='" + policyID + "'");
                    //            for (int j = 0; j < AlertTable.Length; j++)
                    //            {
                    //                if (j == 0)
                    //                    Essen = Convert.ToString(AlertTable[j]["Alert_Text"]);
                    //                else
                    //                    Essen = Essen + "<br>" + Convert.ToString(AlertTable[j]["Alert_Text"]);
                    //            }
                    //        }
                    //        else
                    //            Essen = "No information available";

                    //        DateTime ToDate = Convert.ToDateTime(TLData.Tables["Policy"].Rows[i]["From"]);
                    //        if (ToDate.AddDays(CutOffDays).Date > DateTime.Now.Date)
                    //            ToDate = ToDate.AddDays(CutOffDays).Date;
                    //        double policy = Convert.ToDouble(TLData.Tables["Policy"].Rows[i]["Value"]);
                    //        double cancellationChargeAmount = 0;

                    //        string penaltyType = Convert.ToString(TLData.Tables["policy"].Rows[i]["Type"]);
                    //        switch (penaltyType.ToUpper())
                    //        {
                    //            case "NIGHTS":
                    //                cancellationChargeAmount = (TotalPriceRoom / SearchPara.NoofNights) * policy;
                    //                break;
                    //            case "PERCENTAGE":
                    //                cancellationChargeAmount = (Convert.ToDouble(TotalPriceRoom) * (policy > 100 ? 100 : policy)) / 100;
                    //                break;
                    //            case "AMOUNT":
                    //                cancellationChargeAmount = policy;
                    //                break;
                    //        }
                    //        #region Vat
                    //        Boolean isPerce = true;
                    //        double AgentCompanyPercentage = Convert.ToDouble(SearchPara.AgentCompanyPercentage);
                    //        bool IsB2C = Convert.ToBoolean(Convert.ToByte(SearchPara.IsB2BOrB2C));
                    //        double B2CMarkup = (IsB2C == false ? AgentCompanyPercentage : XMLHotelBookingCommon.B2CMarkup);
                    //        double AgentPercentage = 0.0;
                    //        double AgentMarkupAmount = 0.0;
                    //        double SubAgentMarkup = 0;
                    //        int IsParentId = 1;
                    //        Boolean blnIsShowSupplier = false;
                    //        //Int32 hotelRating = 0;
                    //        //hotelRating = Convert.ToInt32(dsHotel.Tables[0].Rows[0]["Rating"]);
                    //        Common.getAgetMarkupSetting("travellanda", GiataId, dtSuplierMarkup, dtMarkupDetails, ref AgentPercentage, ref AgentMarkupAmount, ref blnIsShowSupplier, ref isPerce, AgentId, hotelRating, SearchPara.CityID, SearchPara.CountryId);
                    //        if (dtAgentDetail != null)
                    //        {
                    //            IsParentId = Convert.ToInt32(dtAgentDetail.Rows[0]["IsParentId"]);
                    //            SubAgentMarkup = Convert.ToDouble(dtAgentDetail.Rows[0]["B2CMarkupPer"]);
                    //        }
                    //        double VatOnPolicy = 0;
                    //        cancellationChargeAmount = Common.ProcessHotelCancelationPrice_WithMarkup(SearchPara.NoofNights, SearchPara.CityID, SearchPara.CountryId, cancellationChargeAmount, isPerce, B2CMarkup, AgentPercentage, AgentMarkupAmount, IsParentId, SubAgentMarkup, ref VatOnPolicy);
                    //        #endregion
                    //        if (ToDate > FromDate)
                    //        {
                    //            if (i == 0)
                    //            {
                    //                dtHotelCancellation.Rows.Add(setCancellationPolicyRow(,dtHotelCancellation.NewRow(), 1, "USD", "TravelLanda", Convert.ToString(ownsystemid), roomno.ToString(), roomtypeid, "", Convert.ToString(OptionCode), DateTime.Now.Date, ToDate.AddDays(-1).AddHours(23).AddMinutes(59).AddSeconds(59), 0, 0, "", 0, "", "", "", Essen, "", ""));
                    //                dtHotelCancellation.AcceptChanges();
                    //            }
                    //        }
                    //        dtHotelCancellation.Rows.Add(setCancellationPolicyRow(,dtHotelCancellation.NewRow(), 1, "USD", "TravelLanda", Convert.ToString(ownsystemid), roomno.ToString(), roomtypeid, "", Convert.ToString(OptionCode), ToDate, FromDate, cancellationChargeAmount, cancellationChargeAmount, "", 0, "", "", "", Essen, "", ""));
                    //        dtHotelCancellation.AcceptChanges();
                    //    }
                    //}
                    //else
                    //{
                    //    dtHotelCancellation.Rows.Add(setCancellationPolicyRow(dtHotelCancellation.NewRow(), 1, "USD", "TravelLanda", Convert.ToString(ownsystemid), roomno.ToString(), roomtypeid, "", Convert.ToString(OptionCode), DateTime.Now.Date, DateTime.Now.Date, TotalPriceRoom, TotalPriceRoom, "", 0, "", "", "", Essen, "", ""));
                    //}
                    #endregion
                    #region "Cancelation Policy"
                    string Deadline = Convert.ToString(TLData.Tables["Body"].Rows[0]["CancellationDeadline"]);
                    double TotalPriceRoom = Convert.ToDouble(TLData.Tables["Body"].Rows[0]["TotalPrice"]);
                    //  double vatMAin = 0;
                    double MainAgent = TotalPriceRoom;

                    Boolean isPerce = true;
                    double AgentCompanyPercentage = Convert.ToDouble(SearchPara.AgentCompanyPercentage);
                    bool IsB2C = Convert.ToBoolean(Convert.ToByte(SearchPara.IsB2BOrB2C));
                    double B2CMarkup = (IsB2C == false ? AgentCompanyPercentage : XMLHotelBookingCommon.B2CMarkup);
                    double AgentPercentage = 0.0;
                    double AgentMarkupAmount = 0.0;
                    double SubAgentMarkup = 0;
                    int IsParentId = 1;
                    Boolean blnIsShowSupplier = false;
                    _markup.getAgetMarkupSetting(Provider, GiataId, dtSuplierMarkup, dtMarkupDetails, ref AgentPercentage, ref AgentMarkupAmount, ref blnIsShowSupplier, ref isPerce, AgentId, hotelRating, SearchPara.CityID, SearchPara.CountryID.ToString(), SearchPara.CheckInDate);
                    if (dtAgentDetail != null)
                    {
                        IsParentId = Convert.ToInt32(dtAgentDetail.Rows[0]["IsParentId"]);
                        SubAgentMarkup = Convert.ToDouble(dtAgentDetail.Rows[0]["B2CMarkupPer"]);
                    }
                    // TotalPriceRoom = Common.ProcessHotelCancelationPrice_WithMarkup(ref MainAgent, SearchPara.NoofNights, SearchPara.CityID, SearchPara.CountryId, TotalPriceRoom, isPerce, B2CMarkup, AgentPercentage, AgentMarkupAmount, IsParentId, SubAgentMarkup, ref vatMAin);

                    // TotalPriceRoom = TotalPriceRoom;
                    string Essen = "";


                    if (TLData.Tables.Contains("Policy") == true)
                    {
                        for (int i = 0; i < TLData.Tables["Policy"].Rows.Count; i++)
                        {
                            int policyID = Convert.ToInt32(TLData.Tables["Policy"].Rows[i]["policies_Id"]);
                            if (TLData.Tables.Contains("Alert") == true)
                            {
                                DataRow[] AlertTable = TLData.Tables["Alert"].Select("Alerts_Id='" + policyID + "'");
                                for (int j = 0; j < AlertTable.Length; j++)
                                {
                                    if (j == 0)
                                        Essen = Convert.ToString(AlertTable[j]["Alert_Text"]);
                                    else
                                        Essen = Essen + "<br>" + Convert.ToString(AlertTable[j]["Alert_Text"]);
                                }
                            }
                            else if (TLData.Tables.Contains("Alerts"))
                            {
                                int BodyId = Convert.ToInt32(TLData.Tables["Body"].Rows[0]["Body_Id"]); //change i to 0 due to error. 
                                DataRow[] AlertTable = TLData.Tables["Alerts"].Select("Body_Id='" + BodyId + "'");
                                for (int j = 0; j < AlertTable.Length; j++)
                                {
                                    if (j == 0)
                                        Essen = Convert.ToString(AlertTable[j]["Alert"]);
                                    else
                                        Essen = Essen + "<br>" + Convert.ToString(AlertTable[j]["Alert"]);
                                }
                            }
                            else
                                Essen = "No information available";
                        }

                        DateTime FromDate = DateTime.Now.Date;
                        TLData.Tables["Policy"].DefaultView.Sort = "From asc";


                        DateTime CancelDate = DateTime.Now.Date;
                        CancelDate = Convert.ToDateTime(TLData.Tables["Policy"].DefaultView[0]["From"]);
                        DateTime NowDate = DateTime.Now.Date;

                        if (CancelDate <= DateTime.Now.Date)
                        {
                            CancelDate = DateTime.Now.Date;
                            //if (CancelCharge == 0)
                            //    CancelCharge = Gross;
                        }
                        else
                        {
                            CancelDate = CancelDate.AddDays(CutOffDays).Date;
                        }
                        double policy = 100;
                        double cancellationChargeAmount = 0;

                        string penaltyType = Convert.ToString("PERCENTAGE");
                        switch (penaltyType.ToUpper())
                        {
                            case "NIGHTS":
                                cancellationChargeAmount = (TotalPriceRoom / SearchPara.NoofNights) * policy;
                                break;
                            case "PERCENTAGE":
                                cancellationChargeAmount = (Convert.ToDouble(TotalPriceRoom) * (policy > 100 ? 100 : policy)) / 100;
                                break;
                            case "AMOUNT":
                                cancellationChargeAmount = policy;
                                break;
                        }
                        #region Vat

                        double VatOnPolicy = 0;
                        double MainAgentCharge = cancellationChargeAmount;
                        double CompanyChargeAmount = cancellationChargeAmount;
                        cancellationChargeAmount = _markup.ProcessHotelCancelationPrice_WithMarkup(ref MainAgentCharge, SearchPara.NoofNights, SearchPara.CityID, SearchPara.CountryID.ToString(), cancellationChargeAmount, isPerce, B2CMarkup, AgentPercentage, AgentMarkupAmount, IsParentId, SubAgentMarkup, ref VatOnPolicy);
                        #endregion
                        if (CancelDate.Date < DateTime.Now.Date)
                        {
                            CancelDate = DateTime.Now.Date;
                        }
                        if (CancelDate.Date.AddDays(-1).Date <= DateTime.Now.Date)
                            CancelDate = DateTime.Now.Date;

                        if (NowDate < CancelDate && CancelDate.Date > DateTime.Now.Date)
                        {
                            double zero = 0;

                            dtHotelCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref zero, dtHotelCancellation.NewRow(), 1, Currency, Provider, Convert.ToString(ownsystemid), roomno.ToString(), roomtypeid, "", Convert.ToString(OptionCode), DateTime.Now.Date, CancelDate.AddDays(-1), 0, 0, "", 0, "", "", "", Essen, "", ""));

                            dtHotelCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref CompanyChargeAmount, dtHotelCancellation.NewRow(), 1, Currency, Provider, Convert.ToString(ownsystemid), roomno.ToString(), roomtypeid, "", Convert.ToString(OptionCode), Convert.ToDateTime(CancelDate), DateTime.ParseExact(SearchPara.CheckInDate, "dd/MM/yyyy", null), MainAgentCharge, cancellationChargeAmount, "", 0, "", "", "", Essen, "", ""));
                        }
                        else
                        {
                            if (CancelDate < DateTime.Now.Date)
                                CancelDate = DateTime.Now.Date;
                            dtHotelCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref CompanyChargeAmount, dtHotelCancellation.NewRow(), 1, Currency, Provider, Convert.ToString(ownsystemid), roomno.ToString(), roomtypeid, "", Convert.ToString(OptionCode), Convert.ToDateTime(CancelDate), DateTime.ParseExact(SearchPara.CheckInDate, "dd/MM/yyyy", null), MainAgentCharge, cancellationChargeAmount, "", 0, "", "", "", Essen, "", ""));
                        }


                    }
                    else
                    {
                        dtHotelCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref TotalPriceRoom, dtHotelCancellation.NewRow(), 1, Currency, Provider, Convert.ToString(ownsystemid), roomno.ToString(), roomtypeid, "", Convert.ToString(OptionCode), DateTime.Now.Date, DateTime.ParseExact(SearchPara.CheckInDate, "dd/MM/yyyy", null), MainAgent, TotalPriceRoom, "", 0, "", "", "", Essen, "", ""));
                    }
                    #endregion
                }
            }
            catch (Exception ex)
            {
                _ = _exception.ErrorNew(ex, _LogAPI.Recheck, SearchKey);

                cancellationDto.dsHotelData = null;
                cancellationDto.ISBooking = "NotAvailable";
                return cancellationDto;
            }


            dsHotel.Tables[2].Merge(dtHotelCancellation);

            cancellationDto.dsHotelData = dsHotel;
            cancellationDto.ISBooking = "Available";
            return cancellationDto;
        }

        public string GetTravelLandaPolicyRequest(string UserName, string Password, string OptionCode)
        {
            try
            {
                StringBuilder TLXML = new StringBuilder();
                TLXML.Append("<Request>");
                TLXML.Append("<Head>");
                TLXML.Append("<Username>" + UserName + "</Username>");
                TLXML.Append("<Password>" + Password + "</Password>");
                TLXML.Append("<RequestType>HotelPolicies</RequestType>");
                TLXML.Append("</Head>");
                TLXML.Append("<Body>");
                TLXML.Append("<OptionId>" + OptionCode + "</OptionId>");
                TLXML.Append("</Body>");
                TLXML.Append("</Request>");
                string TLREQ = TLXML.ToString();
                return TLREQ;
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
                return "";
            }
        }

        public async Task<PriceCheckCMDto> PriceCheckTravelLada(string OptionCode, DataTable dtCheckPriceReqStructure, DataSet dsCartDetails, DataRow[] TravelLandaDetails, string UniqueNo, string AgentUniqueNo, int i, string IsFor, double CompanyBuyPrice, bool IsError, double itemprice, string Searchkey = "", int AgentId = 0)
        {
            PriceCheckCMDto priceCheckCMDto = new PriceCheckCMDto();
            string responsetype = "Success";
            string time = "";
            string SupplierName = Convert.ToString(dsCartDetails.Tables[1].DefaultView[0]["SupplierName"]);
            string Error = "";

            string TLRequest = GetTravelLandaPolicyRequest(Convert.ToString(TravelLandaDetails[0]["UserName"]), Convert.ToString(TravelLandaDetails[0]["Password"]), OptionCode);
            _commonMethod.ThirdPartyBookingHistory(Convert.ToString(UniqueNo), Convert.ToString(AgentUniqueNo), TLRequest, "-", "Before Request hit Price Check", Convert.ToString(dsCartDetails.Tables[1].DefaultView[i]["SupplierName"]), "Request Confirm", " - ", "Withinearth");
            dtCheckPriceReqStructure.Rows.Add(_commonMethod.AddCheckPriceReq(dtCheckPriceReqStructure.NewRow(), 1, Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[i]["UserCartID"]), Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[i]["UserCartDetailId"]), Convert.ToString(dsCartDetails.Tables[1].DefaultView[i]["RoomTypeID"]), Convert.ToString(dsCartDetails.Tables[1].DefaultView[i]["SupplierName"]).ToLower(), TLRequest));
            string sentData = "xml=" + TLRequest;

            System.Diagnostics.Stopwatch timerroomprocess = new System.Diagnostics.Stopwatch();
            timerroomprocess.Start();

            ResponseDto responseDto = await _commonMethod.Response_Supplier(sentData, Convert.ToString(TravelLandaDetails[0]["WebSiteURL"]), 120000, Convert.ToString(dsCartDetails.Tables[1].DefaultView[i]["SupplierName"]), null, false, Searchkey);
            string TLResponse = responseDto.Response;
            Error = responseDto.Error;



            timerroomprocess.Stop();
            time = timerroomprocess.Elapsed.ToString();

            _commonMethod.ThirdPartyBookingHistory(Convert.ToString(UniqueNo), Convert.ToString(AgentUniqueNo), TLRequest, TLResponse + Error, "After Request hit Price Check", "TravelLanda", "Response Confirm", " - ", "Withinearth");
            if (TLResponse.ToLower().Contains("error") || TLResponse.Trim() == "")
            {
                try
                {
                    responsetype = "GeneralError";
                    string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, TLRequest, TLResponse, Error);
                }
                catch (Exception e)
                {
                    _ = _exception.Error(e, Searchkey);
                }
                priceCheckCMDto.Flag = false;
                return priceCheckCMDto;
            }
            try
            {
                DataSet TLData = _commonMethod.GetDataSetFromXMLString(TLResponse);
                itemprice = Convert.ToDouble(TLData.Tables["Body"].Rows[0]["TotalPrice"]);
                priceCheckCMDto.ItemPrice = itemprice;

                if (IsFor == "P")
                {
                    if (Convert.ToDouble((itemprice).TruncateDecimalPlaces(0)) > (CompanyBuyPrice).TruncateDecimalPlaces(0))
                    {
                        try
                        {
                            responsetype = "GeneralError";
                            TLResponse = "#Price Changed " + "#Main Price = " + CompanyBuyPrice + " #New Price = " + itemprice + " #Response= " + TLResponse;
                            string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, TLRequest, TLResponse, Error);
                        }
                        catch (Exception e)
                        {
                            _ = _exception.Error(e, Searchkey);
                        }
                        priceCheckCMDto.Flag = true;
                        priceCheckCMDto.ItemPrice = itemprice;
                        return priceCheckCMDto;
                    }
                }
                else
                {
                    if (Convert.ToDouble((itemprice).TruncateDecimalPlaces(0)) != (CompanyBuyPrice).TruncateDecimalPlaces(0))
                    {
                        try
                        {
                            responsetype = "GeneralError";
                            string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, TLRequest, TLResponse, Error);
                        }
                        catch (Exception e)
                        {
                            _ = _exception.Error(e, Searchkey);
                        }
                        priceCheckCMDto.Flag = true;
                        priceCheckCMDto.ItemPrice = itemprice;
                        return priceCheckCMDto;
                    }
                }
            }
            catch (Exception e)
            {
                try
                {
                    Error = e.Message + e.StackTrace.ToString();
                    responsetype = "GeneralError";
                    string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, TLRequest, TLResponse, Error);
                }
                catch (Exception ex)
                {
                    _ = _exception.Error(ex, Searchkey);
                }
                _ = _exception.Error(e, Searchkey);
                IsError = true;
                priceCheckCMDto.IsError = IsError;
            }
            try
            {
                responsetype = "Success";
                string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, TLRequest, TLResponse, Error);
            }
            catch (Exception e)
            {
                _ = _exception.Error(e, Searchkey);
            }
            priceCheckCMDto.Flag = false;
            return priceCheckCMDto;
        }

        public string GetTravelandaBookingquest(string AgentCode, DataTable dtPassengers, DataSet dsCartDetails, string UserName, string Password, string UniqueNo, bool IsPRiceUpdate, bool IsError, int i)
        {
            if (IsPRiceUpdate == false && IsError == false)
            {
                try
                {
                    string OptionCode = Convert.ToString(dsCartDetails.Tables[1].DefaultView[i]["MealPlanCode"]);
                    string InternalReference = "Api_" + AgentCode + "_" + UniqueNo;
                    StringBuilder TLXML = new StringBuilder();
                    TLXML.Append("<Request>");
                    TLXML.Append("<Head>");
                    TLXML.Append("<Username>" + UserName + "</Username>");
                    TLXML.Append("<Password>" + Password + "</Password>");
                    TLXML.Append("<RequestType>HotelBooking</RequestType>");
                    TLXML.Append("</Head>");
                    TLXML.Append("<Body>");
                    TLXML.Append("<OptionId>" + OptionCode + "</OptionId>");
                    TLXML.Append("<YourReference>" + InternalReference + "</YourReference>");
                    TLXML.Append("<Rooms>");
                    for (int j = 0; j < dsCartDetails.Tables[1].DefaultView.Count; j++)
                    {
                        string[] RoomCode = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomTypeID"]).Split(new String[] { "$$$" }, StringSplitOptions.None);
                        TLXML.Append("<Room>");
                        TLXML.Append("<RoomId>" + RoomCode[j] + "</RoomId>");
                        TLXML.Append("<PaxNames>");
                        dtPassengers.DefaultView.RowFilter = "PickUp='" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[i]["RoomTypeId"]) + "' and UserCartID='" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[i]["UserCartID"]) + "' and RoomNo='" + Convert.ToInt32(j + 1) + "'";
                        dtPassengers.DefaultView.Sort = "IsLead desc";
                        DataTable dtnewpassanger = dtPassengers.DefaultView.ToTable();
                        for (int b = 0; b < dtnewpassanger.Rows.Count; b++)
                        {
                            if (Convert.ToString(dtnewpassanger.Rows[b]["PaxType"]).ToLower() == "adult")
                            {
                                string prefix = Convert.ToString(dtnewpassanger.Rows[b]["Prefix"]);
                                if (prefix.ToLower() == "ms.")
                                    prefix = "Miss";
                                TLXML.Append("<AdultName>");
                                TLXML.Append("<Title>" + prefix + "</Title>");
                                TLXML.Append("<FirstName>" + Convert.ToString(dtnewpassanger.Rows[b]["FirstName"]) + "</FirstName>");
                                TLXML.Append("<LastName>" + Convert.ToString(dtnewpassanger.Rows[b]["LastName"]) + "</LastName>");
                                TLXML.Append("</AdultName>");
                            }
                            else if (Convert.ToString(dtnewpassanger.Rows[b]["PaxType"]).ToLower() == "child")
                            {
                                TLXML.Append("<ChildName>");
                                TLXML.Append("<FirstName>" + Convert.ToString(dtnewpassanger.Rows[b]["FirstName"]) + "</FirstName>");
                                TLXML.Append("<LastName>" + Convert.ToString(dtnewpassanger.Rows[b]["LastName"]) + "</LastName>");
                                TLXML.Append("</ChildName>");
                            }
                        }
                        TLXML.Append("</PaxNames>");
                        TLXML.Append("</Room>");
                    }
                    TLXML.Append("</Rooms>");
                    TLXML.Append("</Body>");
                    TLXML.Append("</Request>");

                    string TLREQ = TLXML.ToString();
                    return TLREQ;
                }

                catch (Exception ex)
                {
                    return "";
                }
            }
            return "";
        }

        public string GetTravelLandaCancellationRequest(string Booking_Ref, string UserName, string Password)
        {
            try
            {
                StringBuilder TLXML = new StringBuilder();
                TLXML.Append("<Request>");
                TLXML.Append("<Head>");
                TLXML.Append("<Username>" + UserName + "</Username>");
                TLXML.Append("<Password>" + Password + "</Password>");
                TLXML.Append("<RequestType>HotelBookingCancel</RequestType>");
                TLXML.Append("</Head>");
                TLXML.Append("<Body>");
                TLXML.Append("<BookingReference>" + Booking_Ref + "</BookingReference>");
                TLXML.Append("</Body>");
                TLXML.Append("</Request>");
                string TLREQ = TLXML.ToString();
                return TLREQ;
            }
            catch (Exception ex)
            {
                return "";
            }
        }
    }
}
