using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XConnect.CommonUtilities;
using XConnect.CommonUtilities.Interface;
using XConnect.ErrorLogHandler.Interface;
using XConnect.Markup.Interface;
using XConnect.Models;
using XConnect.Models.Hotel;
using XConnect.Supplier.DirectContracting.Interface;
using XConnect.Supplier.ETG;
using XConnect.Supplier.Letsfly;
using static Azure.Core.HttpHeader;
using static XConnect.Supplier.Booking.Com.preebookrq;
using static XConnect.Supplier.Booking.Com.preebookrs;
using static XConnect.Supplier.DirectContracting.DirectContractingClass;
using static XConnect.Supplier.TBO.TBOClass.BookingDetailTbores;
using Options = XConnect.Models.Options;



namespace XConnect.Supplier.DirectContracting
{
    public class DirectContractingRepository : IDirectcontracting
    {
        private readonly ICommonMethod _commonMethod;
        private readonly IException _exception;
        private readonly IMarkup _markup;
        private readonly ISupplierRes _supplierRes;
        private readonly KeySettings _keySettings;
        private readonly SupplierConfiguration _supplierConfig;
        public DirectContractingRepository(ICommonMethod commonMethod, IMarkup markup, ISupplierRes supplierRes, IOptions<KeySettings> keySettings, IException exception, IOptions<SupplierConfiguration> supplierConfig)
        {
            _commonMethod = commonMethod;
            _markup = markup;
            _supplierRes = supplierRes;
            _keySettings = keySettings.Value;
            _exception = exception;
            _supplierConfig = supplierConfig.Value;
        }

        public void GetDirectcontractingRequestOnly_Async(DataRow[] commonCode, List<StarRating> listRating, DataTable dtPreferredDataHotel, HotelSearchParameter SearchPara, List<SupplierSearchConfig> lstDirectcontractingSearchConfig, List<SupplierDetails> lstDirectcontractingDetails, ref ConcurrentBag<HotelX> AllTask, string AgentCode, int Hit, TokenData tokenData, DataSet dtXMLCity, string Platform)
        {
            try
            {
                List<StarRating> lstRating = new List<StarRating>();
                lstRating = _commonMethod.GetStarRatingStringList_xtg(SearchPara);

                List<HotelCodeByCity> DtHotelDAta = HtmlExtensions.ToList<HotelCodeByCity>(commonCode.CopyToDataTable());
                int ratingcount = lstRating.Count;
                int hotelcount = DtHotelDAta.Count;



                foreach (SupplierDetails LetDCDetails in lstDirectcontractingDetails)
                {
                    try
                    {
                        int SupplierID = LetDCDetails.SupplierID;
                        string Supplier = LetDCDetails.SupplierName;

                        if (lstDirectcontractingSearchConfig.Count > 0)
                        {
                            List<SupplierSearchConfig> LetsDCSearchConfig = lstDirectcontractingSearchConfig
                                                                            .Where(x => x != null && x.SupplierID == SupplierID)
                                                                            .ToList();

                            if (LetsDCSearchConfig != null && LetsDCSearchConfig.Count > 0)
                            {
                                string Ids = string.Empty;
                                string xmlbase = "";
                                _commonMethod.GetPrefferedHotelCodes(ref Ids, ref xmlbase, ref LetsDCSearchConfig, Supplier, dtPreferredDataHotel, SearchPara.IsSearchFor);

                                List<HotelCodeByCity> filter = new List<HotelCodeByCity>();
                                filter = _commonMethod.FilterWithOutCity_Code(ratingcount, hotelcount, DtHotelDAta, lstRating, Supplier);

                                if (filter.Count > 0)
                                {
                                    DataForSearch dataForSearch = _commonMethod.GetXmlDataForSearchTechwise(SearchPara.Nationality, dtXMLCity, Supplier, LetDCDetails.TechName);

                                    if (dataForSearch.XmlNationality != "")
                                    {
                                        double CurrencyFactor = 1;
                                        string Currency = Convert.ToString(LetDCDetails.CurrencyShortName);


                                        if (Currency.ToLower() != tokenData.SiteCurrency.ToLower())
                                        {
                                            CurrencyFactor = _commonMethod.GetROEByCurrency(Currency, tokenData.dtROE);// Convert.ToDouble(dtInrCurrency.Rows[0]["AmtToExch"]);
                                        }

                                        for (int k = 0; k < LetsDCSearchConfig.Count; k++)
                                        {
                                            bool DebugMode = (Convert.ToInt32(LetsDCSearchConfig[k].DebugMode) == 0 ? false : true);
                                            string UserName = Convert.ToString(LetDCDetails.UserName);
                                            string Password = Convert.ToString(LetDCDetails.Password);
                                            {
                                                {
                                                    int IsCitywisesearch = Convert.ToInt32(LetsDCSearchConfig[k].City);
                                                    if (IsCitywisesearch == 0)//(DtHotelData.DefaultView.Count > 0)
                                                    {
                                                        if (Convert.ToInt32(LetsDCSearchConfig[k].CityID) == 0 || Convert.ToInt32(LetsDCSearchConfig[k].CityID) == SearchPara.CityID)
                                                        {

                                                            {
                                                                int Noofrecords = Convert.ToInt32(LetsDCSearchConfig[k].Noofrecords);
                                                                double count = Convert.ToDouble(filter.Count);
                                                                int NoOfDiv = Convert.ToInt32(Math.Ceiling(count / Convert.ToDouble(Noofrecords)));
                                                                List<List<XMLID_Search>> lstxml = _commonMethod.GetXMLIDString(NoOfDiv, Noofrecords, count, filter);

                                                                for (int a = 0; a < NoOfDiv; a++)
                                                                {
                                                                    string IdsForSearch = Convert.ToString(_commonMethod.GetHotelCodeByCOmma(lstxml[a]));
                                                                    HotelX Xhotel = new HotelX();
                                                                    Xhotel.Supplier = Supplier;
                                                                    Xhotel.DebugeMode = DebugMode;
                                                                    Xhotel.SearchTimeOut = Convert.ToInt32(LetsDCSearchConfig[k].HotelSearchTimeOut);
                                                                    //if (Hit > 0)
                                                                    Xhotel.ProcessTimeout = Convert.ToInt32(LetsDCSearchConfig[k].HotelSearchTimeOut2);

                                                                    if (_keySettings.SupplierProxy == "0")
                                                                        Xhotel.URL = Convert.ToString(LetDCDetails.WebSiteURL + Convert.ToString(_supplierConfig.DirectContract.HotelSearchURL));
                                                                    else
                                                                        Xhotel.URL = Convert.ToString(LetDCDetails.WebSiteURLforAPI + Convert.ToString(_supplierConfig.DirectContract.HotelSearchURL));

                                                                    Xhotel.UserId = Convert.ToString(LetDCDetails.UserID);
                                                                    Xhotel.UserName = UserName;
                                                                    Xhotel.PassWord = Password;

                                                                    Xhotel.CurrencyCode = Currency;
                                                                    Xhotel.CurrencyFactor = CurrencyFactor;
                                                                    Xhotel.XmlCity = dataForSearch.xmlCity;
                                                                    Xhotel.XmlCountry = dataForSearch.xmlCountry;
                                                                    Xhotel.XmlNationality = dataForSearch.XmlNationality;
                                                                    Xhotel.AgentWiseMarkup = LetDCDetails.AgentWiseMarkup;
                                                                    Xhotel.IsShowSupplier = Convert.ToInt16(LetDCDetails.IsShowSupplier) == 1 ? true : false;
                                                                    Xhotel.SupplierID = LetDCDetails.SupplierID;
                                                                    Xhotel.MasterSupplierID = LetDCDetails.MasterSupplierId;
                                                                    //Xhotel.Request = Hotel.XMLLetsFlyRequestOnly_Async(SearchPara, XmlNationality, Xhotel.SearchTimeOut, IdsForSearch);
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
                                                                        objrq.Request = XMLDCRequestOnly_Async(SearchPara, dataForSearch.XmlNationality, Xhotel.SearchTimeOut, IdsForSearch, Xhotel.CurrencyCode, Platform);
                                                                        objrq.Response = ""; RQRS.Add(objrq);
                                                                    }
                                                                    Xhotel.RQRS = RQRS;
                                                                    #endregion
                                                                    Xhotel.ReturnRooms = LetDCDetails.ReturnRooms;
                                                                    Xhotel.TechName = LetDCDetails.TechName;
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
                        _ = _exception.Error(ex);
                    }
                }
            }
            catch (Exception ex)
            {
                _ = _exception.Error(ex);
            }
        }
        public string XMLDCRequestOnly_Async(HotelSearchParameter SearchParam, string Nationality, Int32 TimeOut, string hotelid, string Currency, string Platform)
        {

            try
            {

                string CheckInDate = DateTime.ParseExact(SearchParam.CheckInDate.Trim(), new[] { "dd-MM-yyyy", "dd/MM/yyyy" }, CultureInfo.InvariantCulture, DateTimeStyles.None).ToString("yyyy-MM-dd");

                string CheckOutDate = DateTime.ParseExact(SearchParam.CheckOutDate.Trim(), new[] { "dd-MM-yyyy", "dd/MM/yyyy" }, CultureInfo.InvariantCulture, DateTimeStyles.None).ToString("yyyy-MM-dd");
                int RoomNo = SearchParam.Rooms.Count;
                int NoofNights = SearchParam.NoofNights;
                StringBuilder SBreq = new StringBuilder();
                SBreq.Append("{");
                SBreq.Append("\"countryId\":null,");
                SBreq.Append("\"cityId\":null,");
                SBreq.Append("\"noOfNights\":" + NoofNights + ",");
                SBreq.Append("\"nationality\":\"" + Nationality + "\",");
                SBreq.Append("\"checkInDate\":\"" + CheckInDate + "\",");
                SBreq.Append("\"checkOutDate\":\"" + CheckOutDate + "\",");
                SBreq.Append("\"bookingDate\":\"" + DateTime.Today.ToString("yyyy-MM-dd") + "\",");
                SBreq.Append("\"Rooms\":[");

                for (int i = 0; i < RoomNo; i++)
                {
                    SBreq.Append("{");
                    SBreq.Append("\"roomNo\":" + (i + 1) + ",");
                    SBreq.Append("\"noOfAdults\":" + SearchParam.Rooms[i].NoofAdults + ",");
                    SBreq.Append("\"noOfChild\":" + SearchParam.Rooms[i].NoofChild + ",");

                    // Child Ages Array
                    SBreq.Append("\"childAges\":[");

                    List<int> childAges = new List<int>();

                    if (SearchParam.Rooms[i].Child1Age > 0)
                        childAges.Add(SearchParam.Rooms[i].Child1Age);

                    if (SearchParam.Rooms[i].Child2Age > 0)
                        childAges.Add(SearchParam.Rooms[i].Child2Age);

                    for (int c = 0; c < childAges.Count; c++)
                    {
                        SBreq.Append(childAges[c]);
                        if (c < childAges.Count - 1)
                            SBreq.Append(",");
                    }

                    SBreq.Append("]");
                    SBreq.Append("}");

                    if (i < RoomNo - 1)
                        SBreq.Append(",");
                }

                SBreq.Append("],");
                SBreq.Append("\"Filters\":{");
                SBreq.Append("\"IsOnlyAvailable\":\"" + true + "\",");
                SBreq.Append("\"StarRating\":{\"Min\":" + 1 + ",\"Max\":" + 5 + "},");
                SBreq.Append("\"hotelIds\":[");
                if (!string.IsNullOrEmpty(hotelid))
                {
                    string[] ids = hotelid.Split(',');
                    for (int h = 0; h < ids.Length; h++)
                    {
                        SBreq.Append(ids[h]);
                        if (h < ids.Length - 1)
                            SBreq.Append(",");
                    }
                }
                SBreq.Append("]");
                SBreq.Append("},");
                SBreq.Append("\"AdvancedOptions\":{");
                SBreq.Append("\"Currency\":\"" + Currency + "\",");
                SBreq.Append("\"CustomerIpAddress\":\"" + Platform + "\",");
                SBreq.Append("\"promotionCode\":null");
                SBreq.Append("}");
                SBreq.Append("}");
                return SBreq.ToString();
            }
            catch (Exception ex)
            {
                _ = _exception.Error(ex);
                return "";
            }
        }
        public async Task<List<Options>> Directcontracting_HotelSearchWithRooms(HotelX item, CommonSearch commonSearch, string ResponseData, string WSurl, string Username, string Password, Int32 SearchTimeOut, HotelSearchParameter SearchParam, bool DebugMode, string Currency, double CurrencyFactor, DataSet dsAgentDetails, string Provider, DataTable dtCountryCityMarkup, Int32 AgentId, Int32 TariffID, double AgentPercentage, Boolean IsShowSupplier, int SupplierID, int MasterSupplierID, Int32 processTimeout, string ProccessCutOffTime, string ResProccessTimerLogActive, string ResProccessTimeValueforSaveLog, string StoreSupplierResErrorLog, DataTable dtROE, DataTable dtSuplierMarkup)
        {
            //return await Task.Run(() =>
            //{
            List<Options> options = new List<Options>();
            DataTable DtHotel = _commonMethod.GenerateDataTableNewV2();

            System.Diagnostics.Stopwatch timer_room_DirectCOn = new System.Diagnostics.Stopwatch();
            timer_room_DirectCOn.Start();
            try
            {
                if (ResponseData == null || ResponseData.Trim() == "")
                {
                    return options;
                }
                RootObject_DC.Root DirectContractResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<RootObject_DC.Root>(ResponseData);


                if (DirectContractResponse.result == null)
                {
                    item.ResponseType = "NoCount";
                    return options;
                }
                int totalAdult = 0;
                int totalChild = 0;
                int Rooms = SearchParam.Rooms.Count;

                for (int b = 0; b < Rooms; b++)
                {
                    totalAdult = Convert.ToInt32(SearchParam.Rooms[b].NoofAdults) + totalAdult;
                    totalChild = Convert.ToInt32(SearchParam.Rooms[b].NoofChild) + totalChild;
                }
                Currency = Convert.ToString(DirectContractResponse.result.currency);
                if (DirectContractResponse != null && DirectContractResponse.result.hotelResult != null)
                {

                    for (int hotelIndex = 0; hotelIndex < DirectContractResponse.result.hotelResult.Count; hotelIndex++)
                    {
                        item.ResponseType = "Success";

                        var hotel = DirectContractResponse.result.hotelResult[hotelIndex];
                        //  var Hotelname = hotel.hotelname;

                        if (hotel.hotelOption == null || hotel.hotelOption.Count == 0)
                        {
                            continue;
                        }

                        double MinPrice = 0;

                        if (hotel.hotelOption != null)
                        {
                            for (int roomIndex = 0; roomIndex < hotel.hotelOption.Count; roomIndex++)
                            {
                                var room = hotel.hotelOption[roomIndex];

                                if (roomIndex == 0)
                                {

                                    MinPrice = room.hotelRooms[roomIndex][0].price;

                                }
                                else
                                {
                                    if (MinPrice > room.hotelRooms[roomIndex][0].price)
                                    {
                                        MinPrice = room.hotelRooms[roomIndex][0].price;
                                    }
                                }
                            }
                        }
                        if (Currency.ToLower() != commonSearch.tokenData.SiteCurrency.ToLower())
                        {
                            CurrencyFactor = _commonMethod.GetROEByCurrency(Currency, commonSearch.tokenData.dtROE);
                        }

                        int CutOffDays = _commonMethod.GetCutOffDays(dtSuplierMarkup, Provider);
                        MinPrice = MinPrice * CurrencyFactor;

                        DataRow drXmlHotel = DtHotel.NewRow();

                        //DataRow[] dtdetails = dsBulkdata.Tables["MappingDetails"].Select("XmlId='" + response.hotellist[hotelIndex].hotelid + "' and SupplierName='" + SupplierName + "' ");
                        //if (dtdetails.Length > 0 && MinPrice > 0)
                        {
                            int RaynaId = 0;
                            if (SearchParam.Filters.IsShowRooms == 0)
                            {

                                drXmlHotel["ContractID"] = 0;
                                drXmlHotel["SupplierId"] = SupplierID;
                                //drXmlHotel["AgentPercentage"] = AgentPercentage;
                                drXmlHotel["HotelName"] = ""; //Convert.ToString(drGiataDetails[0]["HotelName"]);
                                drXmlHotel["MasterSupplierId"] = Convert.ToInt32(MasterSupplierID);
                                drXmlHotel["GIITACode"] = 0;
                                drXmlHotel["IsShowSupplier"] = IsShowSupplier;
                                drXmlHotel["UniqueNo"] = hotel.hotelId;
                                drXmlHotel["AvailToken"] = "";
                                drXmlHotel["RaynaId"] = 0;
                                drXmlHotel["OwnsystemHotelId"] = 0;
                                //dr["Rating"] = Convert.ToString(drGiataDetails[0]["Rating"]);
                                drXmlHotel["Rating"] = 0;
                                drXmlHotel["IsRecomondedHotel"] = 0;
                                drXmlHotel["VatdiffAmount"] = 0;
                                drXmlHotel["XMLHotelCode"] = hotel.hotelId;
                                drXmlHotel["SupplierName"] = Provider;
                                drXmlHotel["SupplierCurrency"] = Currency;
                                drXmlHotel["XmlHotelCity"] = "";
                                double FinalPrice = 1;
                                drXmlHotel["FinalSellingPrice"] = FinalPrice;
                                drXmlHotel["SellingPrice"] = FinalPrice;
                                Double FinalTotal = FinalPrice;
                                DtHotel.Rows.Add(drXmlHotel);

                                if (FinalTotal > 0)
                                {
                                    #region "Room code Block"
                                    bool willAddOrNot = true;
                                    Int64 RaynaID = 0;
                                    Int64 GiataId = 0;
                                    int Rating = 0;
                                    Int64 OwnsystemHotelId = 0;
                                    double MiinPrice = FinalTotal;
                                    _markup.GetHotelDataCommon(ref MiinPrice, ref willAddOrNot, ref OwnsystemHotelId, ref RaynaID, ref GiataId, ref Rating, ref drXmlHotel, commonSearch.AgentId, commonSearch.TariffID, commonSearch.dsAgentDetails, commonSearch.supplierGiataMaster, SearchParam, commonSearch.dtCountryCityMarkup, commonSearch.B2CMarkup, commonSearch.IsParentId, commonSearch.SubAgentMarkup, commonSearch.SearchByHotel, commonSearch.HotelIDS);
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
                            else
                            {
                                drXmlHotel["ContractID"] = 0;
                                drXmlHotel["SupplierId"] = SupplierID;
                                //drXmlHotel["AgentPercentage"] = AgentPercentage;
                                drXmlHotel["HotelName"] = ""; //Convert.ToString(drGiataDetails[0]["HotelName"]);
                                drXmlHotel["MasterSupplierId"] = Convert.ToInt32(MasterSupplierID);
                                drXmlHotel["GIITACode"] = 0;
                                drXmlHotel["IsShowSupplier"] = IsShowSupplier;
                                drXmlHotel["UniqueNo"] = hotel.hotelId;
                                drXmlHotel["AvailToken"] = "";
                                drXmlHotel["RaynaId"] = 0;
                                drXmlHotel["OwnsystemHotelId"] = 0;
                                //dr["Rating"] = Convert.ToString(drGiataDetails[0]["Rating"]);
                                drXmlHotel["Rating"] = 0;
                                drXmlHotel["IsRecomondedHotel"] = 0;
                                drXmlHotel["VatdiffAmount"] = 0;
                                drXmlHotel["XMLHotelCode"] = hotel.hotelId;
                                drXmlHotel["SupplierName"] = Provider;
                                drXmlHotel["SupplierCurrency"] = Currency;
                                drXmlHotel["XmlHotelCity"] = "";
                                //double FinalPrice = Common.setHotelListMarkup(SearchParamm, dtCountryCityMarkup, Convert.ToInt32(dsAgentDetails.Tables[0].Rows[0]["IsParentId"]), Convert.ToDouble(dsAgentDetails.Tables[0].Rows[0]["B2CMarkupPer"]), AgentId, TariffID, AgentPercentage, MinPrice, Convert.ToString(drGiataDetails[0]["GiataId"]), SupplierID, Convert.ToString(drGiataDetails[0]["Rating"]));
                                double FinalPrice = 1;
                                drXmlHotel["FinalSellingPrice"] = FinalPrice;
                                drXmlHotel["SellingPrice"] = FinalPrice;
                                Double FinalTotal = FinalPrice;
                                DtHotel.Rows.Add(drXmlHotel);

                                if (FinalTotal > 0)
                                {
                                    #region "Room code Block"
                                    bool willAddOrNot = true;
                                    Int64 RaynaID = 0;
                                    Int64 GiataId = 0;
                                    int Rating = 0;
                                    Boolean blnIsShowSupplier = false;

                                    Int64 OwnsystemHotelId = 0;
                                    double MiinPrice = FinalTotal;
                                    _markup.GetHotelDataCommon(ref MiinPrice, ref willAddOrNot, ref OwnsystemHotelId, ref RaynaID, ref GiataId, ref Rating, ref drXmlHotel, commonSearch.AgentId, commonSearch.TariffID, commonSearch.dsAgentDetails, commonSearch.supplierGiataMaster, SearchParam, commonSearch.dtCountryCityMarkup, commonSearch.B2CMarkup, commonSearch.IsParentId, commonSearch.SubAgentMarkup, commonSearch.SearchByHotel, commonSearch.HotelIDS);
                                    if (willAddOrNot)
                                    {
                                        #region "Room Logic"
                                        DataTable dtDCRoom = _commonMethod.GenerateRoomtypeDataTableNewV2();
                                        DataTable dtDCCancellationPolicy = _commonMethod.GenerateCancellationDataTableNewV2();
                                        DataTable dtDCRoomRateBreakup = _commonMethod.GenerateoomRateBreackUpDataTableNewV2();
                                        double AgentMarkupAmount = 0.0;
                                        Boolean isPerce = true;

                                        _markup.getAgetMarkupSetting(Provider, GiataId.ToString(), commonSearch.dtSuplierMarkup, commonSearch.dtMarkupDetails, ref AgentPercentage, ref AgentMarkupAmount, ref blnIsShowSupplier, ref isPerce, commonSearch.AgentId, Rating, SearchParam.CityID, SearchParam.CountryID.ToString(), SearchParam.CheckInDate);

                                        for (int b = 0; b < Rooms; b++)
                                        {
                                            totalAdult = Convert.ToInt32(SearchParam.Rooms[b].NoofAdults) + totalAdult;
                                            totalChild = Convert.ToInt32(SearchParam.Rooms[b].NoofChild) + totalChild;
                                        }
                                        string remarks = "";
                                        for (int hOption = 0; hOption < hotel.hotelOption.Count; hOption++)
                                        {
                                            //int HotelDetailId = hotel.hotelOption[hOption].hotelDetailId;
                                            int HotelMasterId = hotel.hotelOption[hOption].hotelMasterId;
                                            int supplierMasterId = hotel.hotelOption[hOption].supplierMasterId;
                                            if (hotel.hotelOption[hOption].hotelRooms != null)
                                            {

                                                for (int roomIndex = 0; roomIndex < hotel.hotelOption[hOption].hotelRooms.Count; roomIndex++)
                                                {

                                                    for (int RM = 0; RM < hotel.hotelOption[hOption].hotelRooms[roomIndex].Count(); RM++)
                                                    {
                                                        // string RoomName = Convert.ToString(hotel.hotelOption[hotelIndex].hotelRooms[roomIndex][RM].roomTypeName);

                                                        int RoomNo = hotel.hotelOption[hOption].hotelRooms[roomIndex][RM].roomNo;
                                                        int RoomId = hotel.hotelOption[hOption].hotelRooms[roomIndex][RM].room.roomId;
                                                        int RoomTypeId = hotel.hotelOption[hOption].hotelRooms[roomIndex][RM].room.roomType.id;
                                                        string RoomName = hotel.hotelOption[hOption].hotelRooms[roomIndex][RM].room.roomType.name;
                                                        int BedTypeId = hotel.hotelOption[hOption].hotelRooms[roomIndex][RM].room.bedType.id;
                                                        string BedTypeName = hotel.hotelOption[hOption].hotelRooms[roomIndex][RM].room.bedType.name;
                                                        int RatePlanId = hotel.hotelOption[hOption].hotelRooms[roomIndex][RM].ratePlan.ratePlanId;
                                                        string MealTypeId = Convert.ToString(hotel.hotelOption[hOption].hotelRooms[roomIndex][RM].ratePlan.mealType.id);
                                                        string MealTypeName = hotel.hotelOption[hOption].hotelRooms[roomIndex][RM].ratePlan.mealType.name;
                                                        string MealTypeCode = hotel.hotelOption[hOption].hotelRooms[roomIndex][RM].ratePlan.mealType.code;
                                                        int BoardTypeId = hotel.hotelOption[hOption].hotelRooms[roomIndex][RM].ratePlan.boardType.id;
                                                        string BoardTypeName = hotel.hotelOption[hOption].hotelRooms[roomIndex][RM].ratePlan.boardType.name;
                                                        string BoardTypeCode = hotel.hotelOption[hOption].hotelRooms[roomIndex][RM].ratePlan.boardType.code;
                                                        double Price = hotel.hotelOption[hOption].hotelRooms[roomIndex][RM].price;
                                                        string BookingStatus = hotel.hotelOption[hOption].hotelRooms[roomIndex][RM].bookingStatus;
                                                        Currency = hotel.hotelOption[hOption].currency;
                                                        CurrencyFactor = 1;
                                                        if (Currency.ToLower() != commonSearch.tokenData.SiteCurrency.ToLower())
                                                        {
                                                            CurrencyFactor = _commonMethod.GetROEByCurrency(Currency, commonSearch.tokenData.dtROE);
                                                        }
                                                        double Finaltotal = Price * CurrencyFactor;
                                                        var room = hotel.hotelOption[hOption].hotelRooms[roomIndex][RM];
                                                        List<RootObject_DC.Discount> discounts = null;
                                                        if (room.promotions != null)
                                                        {
                                                            discounts = room.promotions.discounts;
                                                        }
                                                        string DiscountIds = "";
                                                        string DiscountNames = "";
                                                        string DiscountCodes = "";
                                                        string DiscountTypes = "";
                                                        string ValueAddIds = "";
                                                        string ValueAddNames = "";
                                                        string ValueAddCodes = "";

                                                        if (discounts != null && discounts.Count > 0)
                                                        {
                                                            DiscountIds = string.Join(",", discounts.Select(x => x.discountId));
                                                            DiscountNames = string.Join(",", discounts.Select(x => x.discountName));
                                                            DiscountCodes = string.Join(",", discounts.Select(x => x.discountCode));
                                                            DiscountTypes = string.Join(",", discounts.Select(x => x.discountType));
                                                        }
                                                        List<RootObject_DC.ValueAdd> valueAdds = null;

                                                        if (room.promotions != null)
                                                        {
                                                            valueAdds = room.promotions.valueAdds;
                                                        }
                                                        if (valueAdds != null && valueAdds.Count > 0)
                                                        {
                                                            ValueAddIds = string.Join(",", valueAdds.Select(x => x.valueAddId));
                                                            ValueAddNames = string.Join(",", valueAdds.Select(x => x.valueAddName));
                                                            ValueAddCodes = string.Join(",", valueAdds.Select(x => x.valueAddCode));
                                                        }
                                                        bool isRefundable = hotel.hotelOption[hOption].hotelRooms[roomIndex][RM].isRefundable;
                                                        string AllocationTypeId = "";
                                                        List<string> allocationList = new List<string>();
                                                        for (int rb = 0; rb < hotel.hotelOption[hOption].hotelRooms[roomIndex][RM].priceBreakdown.dailyRates.Count(); rb++) //if rate breakup cmnt then we need here 
                                                        {
                                                            AllocationTypeId = Convert.ToString(hotel.hotelOption[hOption].hotelRooms[roomIndex][RM].priceBreakdown.dailyRates[rb].allocationTypeId);
                                                            if (!string.IsNullOrEmpty(AllocationTypeId))
                                                            {
                                                                allocationList.Add(AllocationTypeId);
                                                            }
                                                        }
                                                        string RoomTypeIdNew = HotelMasterId + "##" + RoomId + "##" + RatePlanId + "##" + DiscountIds + "##" + ValueAddIds + "##" + RatePlanId + "##" + string.Join(",", allocationList) + "##" + item.XmlNationality;
                                                        double CompanyCancelAmount = Finaltotal;
                                                        double CompanyBuyingPrice = Finaltotal;
                                                        double CompanyBranchBuyingPrice = 0;
                                                        double CompanyBranchSellingPrice = 0;

                                                        #region Ratebreakup
                                                        //for (int rb = 0; rb < hotel.hotelOption[hOption].hotelRooms[roomIndex][RM].priceBreakdown.dailyRates.Count(); rb++)
                                                        //{
                                                        //    AllocationTypeId = Convert.ToString(hotel.hotelOption[hOption].hotelRooms[roomIndex][RM].priceBreakdown.dailyRates[rb].allocationTypeId);
                                                        //    if (!string.IsNullOrEmpty(AllocationTypeId))
                                                        //    {
                                                        //        allocationList.Add(AllocationTypeId);
                                                        //    }
                                                        //    string CheckInDate = Convert.ToString(SearchParam.CheckInDate);
                                                        //    string CheckOutDate = Convert.ToString(SearchParam.CheckOutDate);
                                                        //    DateTime dtCheckInDate = DateTime.ParseExact(CheckInDate, "dd/MM/yyyy", null);
                                                        //    DateTime dtCheckOutDate = DateTime.ParseExact(CheckOutDate, "dd/MM/yyyy", null);
                                                        //    TimeSpan t = dtCheckOutDate - dtCheckInDate;
                                                        //    double RateBreakUpRate = hotel.hotelOption[hOption].hotelRooms[roomIndex][RM].priceBreakdown.finalRate;
                                                        //    double ratePerNight = RateBreakUpRate;
                                                        //    string DateofWeek = hotel.hotelOption[hOption].hotelRooms[roomIndex][RM].priceBreakdown.dailyRates[rb].dateOfStay;
                                                        //    #region Vat
                                                        //    double VatOnPerNight = 0;
                                                        //    ratePerNight = _markup.ProcessHotelRateBreakupPrice_WithMarkup(1, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), ratePerNight, commonSearch.B2CMarkup, commonSearch.IsParentId, isPerce, AgentPercentage, AgentMarkupAmount, commonSearch.SubAgentMarkup, ref VatOnPerNight);
                                                        //    #endregion

                                                        //    ratePerNight = ratePerNight / SearchParam.NoofNights;
                                                        //    DataRow drRateBreackUp = dtDCRoomRateBreakup.NewRow();
                                                        //    drRateBreackUp["SupplierName"] = Provider;
                                                        //    drRateBreackUp["OwnsystemHotelId"] = OwnsystemHotelId;
                                                        //    drRateBreackUp["ContractID"] = 0;
                                                        //    drRateBreackUp["RoomNo"] = Convert.ToInt32(RoomNo);
                                                        //    drRateBreackUp["RoomTypeId"] = RoomTypeIdNew;
                                                        //    drRateBreackUp["MealId"] = MealTypeId;
                                                        //    drRateBreackUp["Meal"] = MealTypeName;
                                                        //    //string rateWeekColumn = Convert.ToString(dtCheckInDate.Date.AddDays(rb).DayOfWeek).Substring(0, 3);
                                                        //    drRateBreackUp["DateOfWeek"] = DateofWeek;
                                                        //    DateTime rateDate = dtCheckInDate.Date.AddDays(rb);
                                                        //    drRateBreackUp["Date"] = rateDate;
                                                        //    drRateBreackUp["Rate"] = ratePerNight;
                                                        //    drRateBreackUp["OrderNo"] = 1;
                                                        //    drRateBreackUp["RoomTypeName"] = RoomName;
                                                        //    drRateBreackUp["Date"] = rateDate;
                                                        //    drRateBreackUp["BookingStatus"] = "Available";
                                                        //    drRateBreackUp["BoardTypeID"] = "";
                                                        //    drRateBreackUp["VatdiffAmount"] = 0;
                                                        //    drRateBreackUp["RateType"] = RoomName;
                                                        //    drRateBreackUp["SupplierCurrency"] = Currency;
                                                        //    drRateBreackUp["BuyingPrice"] = ratePerNight;
                                                        //    dtDCRoomRateBreakup.Rows.Add(drRateBreackUp);

                                                        //}
                                                        #endregion

                                                        #region Rooms
                                                        DataRow drRoom = dtDCRoom.NewRow();
                                                        drRoom["OwnsystemHotelId"] = OwnsystemHotelId;
                                                        drRoom["MealId"] = MealTypeId;
                                                        drRoom["MealName"] = MealTypeName;
                                                        drRoom["Type"] = "";
                                                        drRoom["Board"] = "";
                                                        drRoom["BookingStatus"] = "Available";
                                                        drRoom["UniqueNo"] = Guid.NewGuid().ToString();
                                                        drRoom["ContractID"] = 0;
                                                        drRoom["RoomNo"] = RoomNo;
                                                        drRoom["BoardTypeID"] = "";
                                                        drRoom["AdultsOccupancy"] = totalAdult;
                                                        drRoom["ChildOccupancy"] = totalChild;
                                                        drRoom["OfferCode"] = "";
                                                        drRoom["OfferDescription"] = "";
                                                        drRoom["RoomSequenceNo"] = "";
                                                        drRoom["RoomTypeCode"] = RoomTypeIdNew;
                                                        drRoom["EssentialInformation"] = remarks;
                                                        drRoom["CompanyBuyingPrice"] = Finaltotal;
                                                        #region Vat
                                                        _markup.ProcessHotelRoom_WithMarkup(CurrencyFactor, Currency, true, false, SearchParam.NoofNights, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), SearchParam.CheckInDate, SearchParam.CheckOutDate, ref Finaltotal, ref drRoom, isPerce, AgentPercentage, AgentMarkupAmount, commonSearch.SubAgentMarkup, commonSearch.B2CMarkup, commonSearch.IsParentId);
                                                        #endregion
                                                        drRoom["SupplierName"] = Provider;
                                                        drRoom["RoomTypeName"] = RoomName;
                                                        drRoom["RoomTypeId"] = RoomTypeIdNew;
                                                        dtDCRoom.Rows.Add(drRoom);
                                                        #endregion
                                                        #region Cancellationpolicy
                                                        DateTime NowDate = DateTime.Now.Date;

                                                        string ownsystemid = Convert.ToString(OwnsystemHotelId);
                                                        double Penalty = Convert.ToDouble(CompanyBuyingPrice);

                                                        #region Vat
                                                        double VatOnPolicy = 0;
                                                        double MainAgentCharge = Penalty;
                                                        double CompanyChargeAmount = Penalty;
                                                        Penalty = _markup.ProcessHotelCancelationPrice_WithMarkup(ref MainAgentCharge, SearchParam.NoofNights, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), Penalty, isPerce, commonSearch.B2CMarkup, AgentPercentage, AgentMarkupAmount, commonSearch.IsParentId, commonSearch.SubAgentMarkup, ref VatOnPolicy);
                                                        #endregion
                                                        double CancelCharge = Penalty;

                                                        DateTime ToDate = DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                                                        string Cancellationdate = DateTime.Now.ToString("dd-MM-yyyy");
                                                        if (hotel.hotelOption[hOption].hotelRooms[roomIndex][RM].cancellationPolicy != null)
                                                        {
                                                            if (isRefundable == false)
                                                            {
                                                                dtDCCancellationPolicy.Rows.Add(_commonMethod.setCancellationPolicyRow(ref CompanyChargeAmount, dtDCCancellationPolicy.NewRow(), CurrencyFactor, Currency, Provider, Convert.ToString(OwnsystemHotelId), Convert.ToString(RoomNo),
                                                                           RoomTypeIdNew, RoomName, MealTypeId, NowDate, ToDate, CompanyCancelAmount, CancelCharge, "", VatOnPolicy, "", "", "No", "", "", ""));
                                                                dtDCCancellationPolicy.AcceptChanges();

                                                            }
                                                            else
                                                            {
                                                                if (hotel.hotelOption[hOption].hotelRooms[roomIndex][RM].cancellationPolicy == null || hotel.hotelOption[hOption].hotelRooms[roomIndex][RM].cancellationPolicy.Count == 0)
                                                                {
                                                                    dtDCCancellationPolicy.Rows.Add(_commonMethod.setCancellationPolicyRow(ref CompanyChargeAmount, dtDCCancellationPolicy.NewRow(), CurrencyFactor, Currency, Provider, Convert.ToString(OwnsystemHotelId), Convert.ToString(RoomNo),
                                                                           RoomTypeIdNew, RoomName, MealTypeId, NowDate, ToDate, CompanyCancelAmount, CancelCharge, "", VatOnPolicy, "", "", "", "Yes", "", ""));
                                                                    dtDCCancellationPolicy.AcceptChanges();
                                                                }
                                                                else
                                                                {
                                                                    var policies = hotel.hotelOption[hOption].hotelRooms[roomIndex][RM].cancellationPolicy;
                                                                    var sortedPolicies = policies.OrderBy(x => x.cancellationPrice).ThenBy(x => DateTime.Parse(x.fromDate)).ToList();
                                                                    for (int cp = 0; cp < sortedPolicies.Count; cp++)
                                                                    {
                                                                        DateTime fromDate = DateTime.Parse(sortedPolicies[cp].fromDate);
                                                                        DateTime toDate = DateTime.Parse(sortedPolicies[cp].toDate);
                                                                        double cancelPrice = sortedPolicies[cp].cancellationPrice;
                                                                        //cancelPrice = _markup.ProcessHotelCancelationPrice_WithMarkup(ref CompanyChargeAmount, SearchParam.NoofNights, SearchParam.CityID, SearchParam.CountryID.ToString(), cancelPrice, isPerce, B2CMarkup, AgentPercentage, AgentMarkupAmount, IsParentId, SubAgentMarkup, ref VatOnPolicy, CBranchId, dtBranch, AgentBranchId, ref CompanyBranchBuyingPrice, ref CompanyBranchSellingPrice);
                                                                        //DateTime CancelDate = toDate;
                                                                        DateTime CancelDate = DateTime.Now.Date;
                                                                        if (cancelPrice == 0)
                                                                        {
                                                                            CancelDate = toDate;
                                                                        }
                                                                        else
                                                                        {
                                                                            CancelDate = fromDate;
                                                                        }
                                                                        CancelDate = CancelDate.AddDays(CutOffDays).Date;
                                                                        if (NowDate > CancelDate)
                                                                        {
                                                                            dtDCCancellationPolicy.Rows.Add(_commonMethod.setCancellationPolicyRow(ref CompanyChargeAmount, dtDCCancellationPolicy.NewRow(), CurrencyFactor, Currency, Provider, Convert.ToString(OwnsystemHotelId), Convert.ToString(RoomNo),
                                                                           RoomTypeIdNew, RoomName, MealTypeId, NowDate, ToDate, CompanyCancelAmount, CancelCharge, "", VatOnPolicy, "", "", "Yes", "", ""));
                                                                            dtDCCancellationPolicy.AcceptChanges();
                                                                        }
                                                                        else
                                                                        {
                                                                            if (cancelPrice == 0)
                                                                            {
                                                                                dtDCCancellationPolicy.Rows.Add(_commonMethod.setCancellationPolicyRow(ref CompanyChargeAmount, dtDCCancellationPolicy.NewRow(), CurrencyFactor, Currency, Provider, Convert.ToString(OwnsystemHotelId), Convert.ToString(RoomNo),
                                                                                RoomTypeIdNew, RoomName, MealTypeId, DateTime.Now.Date, CancelDate.Date.AddDays(-1).AddHours(23).AddMinutes(59).AddSeconds(59), 0, 0, "", VatOnPolicy, "", "", "No", "", ""));

                                                                                dtDCCancellationPolicy.AcceptChanges();
                                                                            }
                                                                            else
                                                                            {
                                                                                dtDCCancellationPolicy.Rows.Add(_commonMethod.setCancellationPolicyRow(ref CompanyChargeAmount, dtDCCancellationPolicy.NewRow(), CurrencyFactor, Currency, Provider, Convert.ToString(OwnsystemHotelId), Convert.ToString(RoomNo),
                                                                                                RoomTypeIdNew, RoomName, MealTypeId, CancelDate.Date.AddDays(-1).AddHours(23).AddMinutes(59).AddSeconds(59), ToDate, CompanyCancelAmount, cancelPrice, "", VatOnPolicy, "", "", "Yes", "", ""));
                                                                                dtDCCancellationPolicy.AcceptChanges();
                                                                            }

                                                                        }
                                                                    }
                                                                }
                                                            }

                                                        }
                                                        else
                                                        {

                                                            dtDCCancellationPolicy.Rows.Add(_commonMethod.setCancellationPolicyRow(ref CompanyChargeAmount, dtDCCancellationPolicy.NewRow(), CurrencyFactor, Currency, Provider, ownsystemid, "1", RoomTypeIdNew,
                                                                RoomName, MealTypeId, NowDate, ToDate, CancelCharge, CancelCharge, "", VatOnPolicy, "", "", Cancellationdate, remarks, "", ""));
                                                            dtDCCancellationPolicy.AcceptChanges();
                                                        }
                                                        #endregion

                                                    }
                                                }
                                            }

                                        }


                                        #endregion
                                        decimal LowestPrice = Convert.ToDecimal(MinPrice).CalculatePrice(commonSearch.tokenData, commonSearch.advancedOptions, Currency);
                                        Options ops = (await _supplierRes.GetCommonRoomForSuppliers(GiataId, commonSearch.tokenData, commonSearch.advancedOptions, Convert.ToDouble(LowestPrice), drXmlHotel, dtDCRoom, dtDCCancellationPolicy, dtDCRoomRateBreakup, SearchParam));
                                        if (ops != null)
                                            options.Add(ops);
                                    }
                                    #endregion
                                }
                            }
                        }

                    }
                }
            }
            catch (Exception e)
            {
                _ = _exception.Error(e);
                return options;
            }
            finally
            {
                #region ResProcessTimer
                try
                {
                    //if (SearchParam != null && ResProccessTimerLogActive == "1" && timer_room_LetsFly.ElapsedMilliseconds > Convert.ToInt32(ResProccessTimeValueforSaveLog))
                    //{
                    //    timer_room_LetsFly.Stop();
                    //    SearchParam.SupplierTime += ",|Finish:Agoda Process Response  " + "|Time :" + timer_room_LetsFly.Elapsed.ToString();
                    //    _ = Task.Run(() => _commonMethod.FireAway_SQL_ResPorcessTimeLog("", timer_room_LetsFly.Elapsed.ToString(), Provider, SearchParam.SearchKey, 1, SearchParam.AgentID));
                    //}
                }
                catch (Exception x)
                {
                    _ = _exception.Error(x);
                }
                #endregion

                if (item.ResponseType == "NoCount" && StoreSupplierResErrorLog == "1")
                    _ = _commonMethod.XMLRequestResponseTrack_Error(item.RQRS[0].Request, ResponseData, item.Supplier, "", "", SearchParam.SearchKey);
            }
            //DtHotel.DefaultView.RowFilter = "FinalSellingPrice>0";
            //DtHotel.DefaultView.RowFilter = "Rating IN(" + strRating + ")";
            return options;
            //return DtHotel;

        }

        public async Task<PriceCheckCMDto> GetDCEPriceCheckREQRES(DataSet dsCartDetails, DataRow[] letsflyDetails, int j, DataTable dtCheckPriceReqStructure, string UniqueNo, string AgentUniqueNo, double MainPrice, int UserCartId, double CompanyBuyingPrice, int AgentId, TokenData tokenData)
        {
            PriceCheckCMDto priceCheckDto = new PriceCheckCMDto();
            string time = "";
            string responsetype = "Success";
            string Error = "";
            string ResponseData = "";
            string prebookRequest = "";

            int UserCartDetailId = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[j]["UserCartDetailId"]);
            string HotelId = Convert.ToString(dsCartDetails.Tables[1].DefaultView[0]["XMLHotelId"]);
            string RoomTypeId = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomTypeID"]);
            string[] roomTypeIds = RoomTypeId.Split(new string[] { "##" }, StringSplitOptions.RemoveEmptyEntries);
            string Nationality = roomTypeIds[5];
            string SupplierName = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["SupplierName"]).ToLower();

            try
            {
                prebookRequest = DCEPriceCheckRequest(RoomTypeId, j, HotelId, dsCartDetails, Nationality);
                _commonMethod.ThirdPartyBookingHistory(Convert.ToString(UserCartDetailId), Convert.ToString(UniqueNo), prebookRequest, "", "Before Hit Price Check Response", SupplierName, "Price Check Request Confirm", "-", "withinearth");

                if (prebookRequest != "")
                {
                    string Url = Convert.ToString(letsflyDetails[0]["WebSiteURL"]) + LetsflyInfo.LetsFly_PrebookURL;
                    
                    System.Diagnostics.Stopwatch timerroomprocess = new System.Diagnostics.Stopwatch();
                    timerroomprocess.Start();

                    ResponseDto responseDto = null;//await GetletsflyResponse(Url, prebookRequest, Error, UniqueNo);
                    ResponseData = responseDto.Response;
                    Error = responseDto.Error;

                    timerroomprocess.Stop();
                    time = timerroomprocess.Elapsed.ToString();

                    _commonMethod.ThirdPartyBookingHistory(Convert.ToString(UserCartDetailId), Convert.ToString(UniqueNo), prebookRequest, ResponseData + Error, "After Hit Price Check Response", SupplierName, "Price Check Response Confirm", "-", "withinearth");

                    if (string.IsNullOrEmpty(ResponseData) || ResponseData.Contains("Error"))
                    {
                        try
                        {
                            responsetype = "GeneralError";
                            string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, prebookRequest, ResponseData, Error);
                        }
                        catch (Exception e)
                        {
                            _ = _exception.ErrorNew(e, ErrorLog._LogAPI.Prebook);
                        }
                        priceCheckDto.Flag = true;
                        return priceCheckDto;
                    }

                    letsflyprebook.letsflyprebookresponse letsflyprebookresponse = Newtonsoft.Json.JsonConvert.DeserializeObject<letsflyprebook.letsflyprebookresponse>(ResponseData);

                    if (letsflyprebookresponse == null || letsflyprebookresponse.status != 0 && letsflyprebookresponse.rooms == null)
                    {
                        try
                        {
                            responsetype = "GeneralError";
                            string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, prebookRequest, ResponseData, Error);
                        }
                        catch (Exception e)
                        {
                            _ = _exception.ErrorNew(e, ErrorLog._LogAPI.Prebook);

                        }
                        priceCheckDto.Flag = true;
                        return priceCheckDto;
                    }
                    else
                    {
                        try
                        {
                            if (letsflyprebookresponse.rooms != null)
                            {
                                for (int r = 0; r < letsflyprebookresponse.rooms.Count; r++)
                                {

                                    double Price = Convert.ToDouble(letsflyprebookresponse.rooms[r].totalprice);

                                    if (Price > 0)
                                    {
                                        string sessionid = Convert.ToString(letsflyprebookresponse.sessionid);
                                        string roomid = Convert.ToString(letsflyprebookresponse.rooms[r].id);
                                        string roomname = Convert.ToString(letsflyprebookresponse.rooms[r].name);
                                        string roomtypeid = roomid + "$$$" + sessionid + "$$$" + Price + "$$$" + roomname;

                                        string CurrencyCode = Convert.ToString(letsflyprebookresponse.rooms[r].currency);
                                        bool IsSuccess = await _commonMethod.GetRoomNameUpdate(roomtypeid, UserCartId);

                                        double CurrencyFactor = 1;
                                        if (Convert.ToString(CurrencyCode).ToLower() != tokenData.SiteCurrency.ToLower())
                                        {

                                            try
                                            {
                                                responsetype = "GeneralError";
                                                string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, prebookRequest, ResponseData, Error);
                                            }
                                            catch (Exception e)
                                            {
                                                _ = _exception.ErrorNew(e, ErrorLog._LogAPI.Prebook);

                                            }
                                            MainPrice = 0;

                                            priceCheckDto.Flag = true;
                                            priceCheckDto.ItemPrice = MainPrice;
                                            return priceCheckDto;
                                        }

                                        MainPrice = Price * CurrencyFactor;
                                        priceCheckDto.ItemPrice = MainPrice;

                                        if (Math.Round(CompanyBuyingPrice, 2) != Math.Round(MainPrice, 2))
                                        {
                                            try
                                            {
                                                responsetype = "GeneralError";
                                                string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, prebookRequest, ResponseData, Error);
                                            }
                                            catch (Exception e)
                                            {
                                                _ = _exception.ErrorNew(e, ErrorLog._LogAPI.Prebook);

                                            }
                                            MainPrice = 0;

                                            priceCheckDto.Flag = true;
                                            priceCheckDto.ItemPrice = MainPrice;
                                            return priceCheckDto;
                                        }
                                    }
                                    else
                                    {
                                        try
                                        {
                                            responsetype = "GeneralError";
                                            string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, prebookRequest, ResponseData, Error);
                                        }
                                        catch (Exception e)
                                        {
                                            _ = _exception.ErrorNew(e, ErrorLog._LogAPI.Prebook);

                                        }
                                        MainPrice = 0;

                                        priceCheckDto.Flag = true;
                                        priceCheckDto.ItemPrice = MainPrice;
                                        return priceCheckDto;
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            try
                            {
                                responsetype = "GeneralError";
                                string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, prebookRequest, ResponseData, Error);
                            }
                            catch (Exception e)
                            {
                                _ = _exception.ErrorNew(e, ErrorLog._LogAPI.Prebook);

                            }
                            _ = _exception.ErrorNew(ex, ErrorLog._LogAPI.Prebook);

                            MainPrice = 0;

                            priceCheckDto.Flag = true;
                            priceCheckDto.ItemPrice = MainPrice;
                            return priceCheckDto;
                        }
                    }

                }

            }
            catch (Exception ex)
            {
                try
                {
                    responsetype = "GeneralError";
                    string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, prebookRequest, ResponseData, Error);
                }
                catch (Exception e)
                {
                    _ = _exception.ErrorNew(e, ErrorLog._LogAPI.Prebook);

                }
                _ = _exception.ErrorNew(ex, ErrorLog._LogAPI.Prebook);

                MainPrice = 0;

                priceCheckDto.Flag = true;
                priceCheckDto.ItemPrice = MainPrice;
                return priceCheckDto;
            }

            try
            {
                responsetype = "Success";
                string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, prebookRequest, ResponseData, Error);
            }
            catch (Exception e)
            {
                _ = _exception.ErrorNew(e, ErrorLog._LogAPI.Prebook);

            }

            priceCheckDto.Flag = false;
            return priceCheckDto;
        }

        public string DCEPriceCheckRequest(string roomtypeid, int j, string HotelIds, DataSet dsCartDetails,string Nationlity)
        {
            try
            {
                string CheckIn = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["CheckInDate"]);//"20180327"
                CheckIn = CheckIn.Substring(0, 4) + "-" + CheckIn.Substring(4, 2) + "-" + CheckIn.Substring(6, 2);
                string CheckOut = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["CheckOutDate"]);
                CheckOut = CheckOut.Substring(0, 4) + "-" + CheckOut.Substring(4, 2) + "-" + CheckOut.Substring(6, 2);
                int NoofNight = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[j]["NoofNight"]);
                int HotelId = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[j]["XMLHotelId"]);
                //int RoomNo = Searchpara.Rooms.Count;
                //int NoofNights = Searchpara.NoofNights;
                //StringBuilder SBreq = new StringBuilder();
                //SBreq.Append("{");
                //SBreq.Append("\"countryId\":null,");
                //SBreq.Append("\"cityId\":null,");
                //SBreq.Append("\"noOfNights\":" + NoofNights + ",");
                //SBreq.Append("\"nationality\":\"" + Nationality + "\",");
                //SBreq.Append("\"checkInDate\":\"" + CheckInDate + "\",");
                //SBreq.Append("\"checkOutDate\":\"" + CheckOutDate + "\",");
                //SBreq.Append("\"bookingDate\":\"" + DateTime.Today.ToString("yyyy-MM-dd") + "\",");
                //SBreq.Append("\"Rooms\":[");

                //for (int i = 0; i < RoomNo; i++)
                //{
                //    SBreq.Append("{");
                //    SBreq.Append("\"roomNo\":" + (i + 1) + ",");
                //    SBreq.Append("\"noOfAdults\":" + Searchpara.Rooms[i].NoofAdults + ",");
                //    SBreq.Append("\"noOfChild\":" + Searchpara.Rooms[i].NoofChild + ",");

                //    // Child Ages Array
                //    SBreq.Append("\"childAges\":[");

                //    List<int> childAges = new List<int>();

                //    if (Searchpara.Rooms[i].Child1Age > 0)
                //        childAges.Add(Searchpara.Rooms[i].Child1Age);

                //    if (Searchpara.Rooms[i].Child2Age > 0)
                //        childAges.Add(Searchpara.Rooms[i].Child2Age);

                //    for (int c = 0; c < childAges.Count; c++)
                //    {
                //        SBreq.Append(childAges[c]);
                //        if (c < childAges.Count - 1)
                //            SBreq.Append(",");
                //    }

                //    SBreq.Append("]");
                //    SBreq.Append("}");

                //    if (i < RoomNo - 1)
                //        SBreq.Append(",");
                //}

                //SBreq.Append("],");
                //SBreq.Append("\"Filters\":{");
                //SBreq.Append("\"IsOnlyAvailable\":\"" + true + "\",");
                //SBreq.Append("\"StarRating\":{\"Min\":" + 1 + ",\"Max\":" + 5 + "},");
                //SBreq.Append("\"hotelIds\":[");
                //if (!string.IsNullOrEmpty(HotelIds))
                //{
                //    string[] ids = HotelIds.Split(',');
                //    for (int h = 0; h < ids.Length; h++)
                //    {
                //        SBreq.Append(ids[h]);
                //        if (h < ids.Length - 1)
                //            SBreq.Append(",");
                //    }
                //}
                //SBreq.Append("]");
                //SBreq.Append("},");
                //SBreq.Append("\"AdvancedOptions\":{");
                //SBreq.Append("\"Currency\":\"" + Currency + "\",");
                //SBreq.Append("\"CustomerIpAddress\":\"" + CustomerIpAddress + "\",");
                //SBreq.Append("\"promotionCode\":null");
                //SBreq.Append("}");
                //SBreq.Append("}");
                //return SBreq.ToString();
                string abc = "";
                return abc;
            }
            catch (Exception ex)
            {
                _ = _exception.ErrorNew(ex, ErrorLog._LogAPI.Prebook);
                return "";
            }

        }

    }
}
