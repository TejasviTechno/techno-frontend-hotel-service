using static Azure.Core.HttpHeader;
using static XConnect.Models.CacheMaster;
using static XConnect.Models.CommoXMLOutResponse;
using System.Data;
using System.Text;
using XConnect.Models.Hotel;
using XConnect.Models;
using XConnect.CommonUtilities;
using XConnect.CommonUtilities.Interface;
using XConnect.Supplier.AgodaNew.Interface;
using XConnect.ErrorLogHandler.Interface;
using Newtonsoft.Json;
using Microsoft.Data.SqlClient;
using XConnect.DAL;
using XConnect.DAL.Interface;
using System.Diagnostics;
using Azure;
using System.Net.Http.Headers;
using System.Net;
using System.IO.Compression;
using static XConnect.Supplier.DerbySoft.DerbySoft;
using Microsoft.Extensions.Options;
using Options = XConnect.Models.Options;
using XConnect.Supplier.Techno;
//using static XConnect.Supplier.AgodaNew.AgodaNew;
//using CommoXMLOutPreCheckRequest = XConnect.Supplier.AgodaNew.AgodaNew.CommoXMLOutPreCheckRequest;
//using CommoXMLOutPreCheckResponse = XConnect.Supplier.AgodaNew.AgodaNew.CommoXMLOutPreCheckResponse;

namespace XConnect.Supplier.AgodaNew
{
    public class AgodaNewRepository :IAgodaNew
    {

        private readonly ICommonMethod _commonMethod;
        private readonly IException _exception;
        private readonly ISupplierRes _supplierRes;
        private readonly IDLConnection _dlConnection;
        private readonly KeySettings _keySettings;

        public AgodaNewRepository(ICommonMethod commonMethod, IException exception, ISupplierRes supplierRes, IDLConnection dlConnection, IOptions<KeySettings> keySettings) 
        { 
             _commonMethod= commonMethod;
            _exception = exception;
            _supplierRes = supplierRes;
            _dlConnection = dlConnection;
            _keySettings = keySettings.Value;
        }

        public async Task<List<Options>> XMLOut_HotelSearchWithRooms(string requestName, HotelX Item, CommonSearch commonSearch, DataSet GTAResponse, string HotelID, HotelSearchParameter SearchParam, DataTable dtAgentDetail, DataTable dtMarkupDetails, AdvancedOptions advancedOptions, DataSet DTChainCode, DataSet DTCountryblock)
        //public async Task<List> XMLOut_HotelSearchWithRooms(string requestName, HotelX Item, CommonSearch commonSearch, DataSet GTAResponse, string HotelID, HotelSearchParameter SearchParam, DataTable dtAgentDetail, DataTable dtSuplierMarkup, DataTable dtMarkupDetails, AdvancedOptions advancedOptions, DataSet DTChainCode)
        {
            List<Options> options = new List<Options>();
            if (GTAResponse != null && GTAResponse.Tables.Count > 0 && GTAResponse.Tables[0].Rows.Count > 0)
            {
                string giitaCode = "";
                double LowestRate = 0;
                DataTable DtHotel = _commonMethod.GenerateDataTableNewV2();
                //DataRow drXmlHotel = DtHotel.NewRow();
                //DataTable DtHotelRoom = Common.GenerateRoomtypeDataTableNewV2();
                //DataTable dtCancellation = Common.GenerateCancellationDataTableNewV2();
                //DataTable DtHotelRoomRateBreackUp = Common.GenerateoomRateBreackUpDataTableNewV2();                
                DataTable dtHotelAllocation = _commonMethod.GenerateHotelAllocationTableNewV2();
                try
                {
                    if (Item != null && Item.RQRS != null && Item.RQRS[0].Response != null)
                    {

                        giitaCode = Convert.ToString(GTAResponse.Tables[0].Rows[0]["Id"]);
                        AvailabilityResponse availabilityResponse = new AvailabilityResponse();
                        availabilityResponse = JsonConvert.DeserializeObject<AvailabilityResponse>(Item.RQRS[0].Response);
                        if (availabilityResponse.AvailabilityRS != null)
                        {
                            string HotelOptionIdRP = string.Empty;
                            string SearchKeyRP = string.Empty;
                            DataRow drXmlHotel = DtHotel.NewRow();
                            DataTable DtHotelRoom = _commonMethod.GenerateRoomtypeDataTableNewV2();
                            DataTable dtCancellation = _commonMethod.GenerateCancellationDataTableNewV2();
                            DataTable DtHotelRoomRateBreackUp = _commonMethod.GenerateoomRateBreackUpDataTableNewV2();

                            try
                            {

                                int MasterSupplierID = 0;
                                string SupplierID = availabilityResponse.AvailabilityRS.HotelResult[0].HotelOption[0].HotelOptionId.Split('|')[Convert.ToInt16(_cacheHotelOption.supplierId)];
                                if (SupplierID == "")
                                {
                                    SupplierID = "162";
                                }
                                MasterSupplierID = Convert.ToInt32(SupplierID);
                                SearchKeyRP = availabilityResponse.AvailabilityRS.SearchKey;
                                //HotelOptionIdRP = availabilityResponse.AvailabilityRS.HotelResult[0].HotelOption[i].HotelOptionId;

                                //var lowestPriceRoom = GetLowestPriceRoom(availabilityResponse);
                                //LowestRate = lowestPriceRoom.Price;
                                LowestRate = availabilityResponse.AvailabilityRS.HotelResult[0].HotelOption[0].MinPrice;

                                drXmlHotel["GIITACode"] = giitaCode;
                                drXmlHotel["OwnsystemHotelId"] = Item.SearchPara.Filters.HotelIDs;
                                drXmlHotel["RaynaId"] = Item.SearchPara.Filters.HotelIDs;
                                drXmlHotel["SupplierCurrency"] = availabilityResponse.AvailabilityRS.Currency;
                                drXmlHotel["XmlHotelCity"] = GTAResponse.Tables[0].Rows[0]["CityId"];
                                drXmlHotel["XMLHotelCode"] = Item.SearchPara.Filters.HotelIDs;
                                drXmlHotel["HotelName"] = GTAResponse.Tables[0].Rows[0]["Name"];
                                //drXmlHotel["HotelFrontImage"] = GTAImage1;
                                drXmlHotel["Address"] = GTAResponse.Tables[0].Rows[0]["AddressLine"];
                                //drXmlHotel["ReviewCount"] = ReviewCount;
                                //drXmlHotel["ReviewRating"] = ReviewRating;
                                drXmlHotel["Location"] = GTAResponse.Tables[0].Rows[0]["LandMark"];
                                drXmlHotel["XMLCountry"] = GTAResponse.Tables[0].Rows[0]["CountryId"];
                                drXmlHotel["XMLCity"] = GTAResponse.Tables[0].Rows[0]["CityId"];
                                //drXmlHotel["Rating"] = Rating;
                                //drXmlHotel["GroupName"] = Chain;
                                //drXmlHotel["SortSequence"] = SortSeq;
                                //drXmlHotel["latitude"] = Convert.ToString(Latitude).ToLower();
                                //drXmlHotel["longitute"] = Convert.ToString(Longitude).ToLower();
                                //drXmlHotel["SellingPrice"] = LowestRate;
                                //drXmlHotel["FinalSellingPrice"] = LowestRate;
                                //drXmlHotel["BookingStatus"] = BookingStatus;
                                //drXmlHotel["SupplierName"] = "siteminder";
                                //drXmlHotel["OfficeID"] = 0;
                                //drXmlHotel["ContractID"] = 0;
                                //drXmlHotel["Reviews"] = 0;
                                //drXmlHotel["UniqueNo"] = uniqueno;
                                //drXmlHotel["IsShowSupplier"] = blnIsShowSupplier;
                                //drXmlHotel["VatdiffAmount"] = 0;
                                //drXmlHotel["MasterSupplierId"] = Convert.ToInt32(GTAResponse.Tables[0].Rows[h]["SupplierId"]);
                                //drXmlHotel["SupplierId"] = "14";
                                //drXmlHotel["ReservationEmail"] = Convert.ToString(GTAResponse.Tables[0].Rows[h]["ReservationEmail"]);
                                //drXmlHotel["HotelContactNo"] = ""; // Convert.ToString(GTAResponse.Tables[0].Rows[h]["HotelContactNo"]);

                                DtHotel.Rows.Add(drXmlHotel);
                                for (int i = 0; i < availabilityResponse.AvailabilityRS.HotelResult[0].HotelOption.Count(); i++)
                                {
                                    HotelOptionIdRP = availabilityResponse.AvailabilityRS.HotelResult[0].HotelOption[i].HotelOptionId;

                                    //ROOM
                                    if (true)// (availabilityResponse.AvailabilityRS.HotelResult[0].HotelOption[i].IsCombineRoom == true)
                                    {
                                        string RoomTokenRP = string.Empty;
                                        //foreach (var rm in availabilityResponse.AvailabilityRS.HotelResult[0].HotelOption[i].HotelRooms)

                                        //string keydata = "GetAgodaChainCode_new_" + Convert.ToString(Item.SearchPara.Filters.HotelIDs);
                                        //DataSet DTChainCode = new DataSet();
                                        //if (HttpContext.Current.Cache[keydata] == null || advancedOptions.CustomerIpAddress == "777")
                                        //{
                                        //    SqlParameter[] para = new SqlParameter[]{
                                        //        new SqlParameter("@Hotelid",Convert.ToInt32(Item.SearchPara.Filters.HotelIDs))
                                        //        };
                                        //    DLConnection db = new DLConnection();
                                        //    //db.GetExecuteScalarSP("EnterAPILogs_XMLOUT", para);
                                        //    DTChainCode = db.GetDataSet("GetAgodaChainCode_new", para);
                                        //    if (DTChainCode.Tables[0].Rows.Count > 0)
                                        //    {
                                        //        HttpContext.Current.Cache.Insert(keydata, DTChainCode, null, DateTime.Now.AddMinutes(5), TimeSpan.Zero);
                                        //    }
                                        //}
                                        //else
                                        //{
                                        //    DTChainCode = (DataSet)(HttpContext.Current.Cache[keydata]);
                                        //}


                                        for (int j = 0; j < availabilityResponse.AvailabilityRS.HotelResult[0].HotelOption[i].HotelRooms.Count(); j++)
                                        {
                                            RoomTokenRP = "";
                                            int daysBeforeCheckIn = 365;
                                            double totalprice = 0;
                                            string RoomName = "";
                                            string MealName = "Ro";
                                            string processedRoomTypeID = "";
                                            bool isroom = true;

                                            //foreach (var rm in availabilityResponse.AvailabilityRS.HotelResult[0].HotelOption[i].HotelRooms[j])
                                            {
                                                for (int kk = 0; kk < availabilityResponse.AvailabilityRS.HotelResult[0].HotelOption[i].HotelRooms[j].Count; kk++)
                                                {
                                                    //GimmonixDetail gimmonixDetail = new GimmonixDetail();
                                                    //if (GTAResponse.Tables.Count > 1 && GTAResponse.Tables[1] != null && GTAResponse.Tables[1].Rows.Count > 0)
                                                    //{
                                                    //    //GimmonixRoomId = GTAResponse.Tables[1].AsEnumerable()
                                                    //    //            .Where(row => row.Field<int>("Supplierid") == Convert.ToInt32(SupplierID) && row.Field<string>("SupplierRoomName") == rm.RoomTypeName)
                                                    //    //            .Select(row => row.Field<string>("gimmonixRoomId"))
                                                    //    //            .FirstOrDefault();

                                                    //    gimmonixDetail = GTAResponse.Tables[1].AsEnumerable()
                                                    //                    .Where(row => row.Field<int>("Supplierid") == Convert.ToInt32(SupplierID))
                                                    //                    .Select(row => new GimmonixDetail
                                                    //                    {
                                                    //                        GimmonixRoomId = row.Field<string>("gimmonixRoomId"),
                                                    //                        GimmonixRoomName = row.Field<string>("GimmonixRoomName")
                                                    //                    })
                                                    //                    .FirstOrDefault();
                                                    //    //GimmonixRoomId = GimmonixRoomId + "_" + SupplierID + rm.RoomToken;                                                    
                                                    //}
                                                    //else { continue; }
                                                    if (availabilityResponse.AvailabilityRS.HotelResult[0].HotelOption[i].HotelRooms[j][0].RoomTypeName == availabilityResponse.AvailabilityRS.HotelResult[0].HotelOption[i].HotelRooms[j][kk].RoomTypeName && availabilityResponse.AvailabilityRS.HotelResult[0].HotelOption[i].HotelRooms[j][0].MealName == availabilityResponse.AvailabilityRS.HotelResult[0].HotelOption[i].HotelRooms[j][kk].MealName)
                                                    {
                                                        string RoomTokenRP1 = availabilityResponse.AvailabilityRS.HotelResult[0].HotelOption[i].HotelRooms[j][kk].RoomToken;
                                                        RoomTokenRP = RoomTokenRP + "###" + RoomTokenRP1;
                                                        RoomName = availabilityResponse.AvailabilityRS.HotelResult[0].HotelOption[i].HotelRooms[j][kk].RoomTypeName;

                                                        processedRoomTypeID = "123"; //getMappedRoom_rlm(RoomName.Split('|')[0].Trim(), GTAResponse.Tables[1], Convert.ToInt32(Item.SearchPara.Filters.HotelIDs));

                                                        //if (processedRoomTypeID == "")  // needtochange
                                                        //{
                                                        //    isroom = false;
                                                        //    continue;
                                                        //}

                                                        //MealName = availabilityResponse.AvailabilityRS.HotelResult[0].HotelOption[i].HotelRooms[j][kk].MealName;
                                                        MealName = availabilityResponse.AvailabilityRS.HotelResult[0].HotelOption[i].HotelRooms[j][kk].MappedMealName;

                                                        double totalprice1 = availabilityResponse.AvailabilityRS.HotelResult[0].HotelOption[i].HotelRooms[j][kk].Price;
                                                        totalprice = totalprice + totalprice1;
                                                        TimeSpan totaldays = new TimeSpan();
                                                        for (int cc = 0; cc < availabilityResponse.AvailabilityRS.HotelResult[0].HotelOption[i].HotelRooms[j][kk].CancellationPolicy.Count; cc++)
                                                        {
                                                            double CancellationPrice = availabilityResponse.AvailabilityRS.HotelResult[0].HotelOption[i].HotelRooms[j][kk].CancellationPolicy[cc].CancellationPrice;
                                                            if (CancellationPrice == 0)
                                                            {
                                                                DateTime from = DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null);
                                                                DateTime to = Convert.ToDateTime(availabilityResponse.AvailabilityRS.HotelResult[0].HotelOption[i].HotelRooms[j][kk].CancellationPolicy[cc].ToDate);
                                                                totaldays = Convert.ToDateTime(from) - Convert.ToDateTime(to);
                                                                break;
                                                            }
                                                            else
                                                            {
                                                                DateTime from = DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null);
                                                                DateTime to = Convert.ToDateTime(DateTime.Now.Date);
                                                                totaldays = Convert.ToDateTime(from) - Convert.ToDateTime(to);
                                                            }
                                                        }
                                                        if (kk == 0)
                                                        {
                                                            daysBeforeCheckIn = totaldays.Days;
                                                        }
                                                        else
                                                        {
                                                            if (daysBeforeCheckIn < totaldays.Days)
                                                            {
                                                                daysBeforeCheckIn = totaldays.Days;
                                                            }
                                                        }
                                                    }
                                                    else
                                                    {
                                                        isroom = false;
                                                        break;
                                                    }
                                                }
                                                if (isroom == false)
                                                {
                                                    continue;
                                                }

                                                double _TotalPrice = totalprice;
                                                double AgentPercentage = 0;// Convert.ToInt32(DTChainCode.Rows[chaincode]["markup"]);
                                                bool isPerce = true;
                                                bool isApply = true;

                                                //SqlParameter[] para = new SqlParameter[]{
                                                //new SqlParameter("@Hotelid",Convert.ToInt32(Item.SearchPara.Filters.HotelIDs))
                                                //};
                                                //DLConnection db = new DLConnection();
                                                ////db.GetExecuteScalarSP("EnterAPILogs_XMLOUT", para);
                                                //DataSet DTChainCode = db.GetDataSet("GetAgodaChainCode_new", para);

                                                for (int chaincode = 0; chaincode < DTChainCode.Tables[0].Rows.Count; chaincode++)
                                                {

                                                    string ChainCode_Agoda = Convert.ToString(DTChainCode.Tables[0].Rows[chaincode]["chaincode"]);// Convert.ToString(chaincode);
                                                    string[] getsupplier = HotelOptionIdRP.Split('|');
                                                    string Supplierid = getsupplier[2] == "" ? "12" : getsupplier[2];

                                                    //DataRow[] drsupplier = DTChainCode.Tables[2].Select("SupplierID ='" + Supplierid + "' and chaincode = '" + ChainCode_Agoda + "' and markuptype = 'Hotel Wise markup'");
                                                    //DataRow[] drsupplier = DTChainCode.Tables[2].Select($"SupplierID = '{Supplierid}' AND chaincode = '{ChainCode_Agoda}' AND markuptype = 'Hotel Wise markup'");

                                                    int hid = Convert.ToInt32(Item.SearchPara.Filters.HotelIDs);


                                                    #region comment channelwise markup


//                                                    DataRow[] drsupplier = DTChainCode.Tables[0].Select($"Supplierid = {Supplierid} AND chaincode = {ChainCode_Agoda} AND MarkupType = 'Percentage'"
//);

//                                                    if (drsupplier.Length == 0 || Convert.ToDouble(drsupplier[0]["MarkupValue"]) < 1)
//                                                    {
//                                                        drsupplier = DTChainCode.Tables[0].Select(
//                                                            $"Supplierid = {Supplierid} AND chaincode = {ChainCode_Agoda} AND MarkupType = 'Supplier wise markup'"
//                                                        );
//                                                    }

//                                                    if (drsupplier.Length == 0 || Convert.ToDouble(drsupplier[0]["MarkupValue"]) < 1)
//                                                    {
//                                                        drsupplier = DTChainCode.Tables[0].Select(
//                                                            $"Supplierid = {Supplierid} AND chaincode = {ChainCode_Agoda}"
//                                                        );
//                                                    }

//                                                    if (drsupplier.Length > 0)
//                                                    {
//                                                        // use data
//                                                    }
//                                                    else
//                                                    {
//                                                        continue;
//                                                    }

//                                                    //if (Convert.ToDouble(drsupplier[0]["markup"]) > 0)
//                                                    if (Convert.ToDouble(drsupplier[0]["MarkupValue"]) > 0)
//                                                    {
//                                                        //AgentPercentage = Convert.ToDouble(drsupplier[0]["markup"]);
//                                                        AgentPercentage = Convert.ToDouble(drsupplier[0]["MarkupValue"]);
//                                                        isPerce = true;
//                                                    }
//                                                    double b2cItemPriceMarkup = 0.0;

//                                                    if (isPerce)
//                                                        b2cItemPriceMarkup = Math.Round((Math.Round((_TotalPrice * AgentPercentage), 2) / 100), 2);

//                                                    if (isApply)
//                                                    {
//                                                        totalprice = Math.Round(_TotalPrice + b2cItemPriceMarkup, 6);
//                                                    }

                                                    #endregion


                                                    //isApply = false;

                                                    //RoomTokenRP = rm.RoomToken;

                                                    DataRow drHotelCancellation = dtCancellation.NewRow();
                                                    drHotelCancellation["daysBeforeCheckIn"] = Convert.ToString(Convert.ToInt32(daysBeforeCheckIn));
                                                    drHotelCancellation["CompanyCancellationPrice"] = totalprice;
                                                    drHotelCancellation["AgentCancellationPrice"] = totalprice;

                                                    //if (rm.CancellationPolicy.Count > 1)
                                                    //{
                                                    //    foreach (var cp in rm.CancellationPolicy)
                                                    //    {
                                                    //        if (cp.CancellationPrice == 0)
                                                    //        {
                                                    //            drHotelCancellation["FromDate"] = cp.FromDate;
                                                    //            drHotelCancellation["ToDate"] = cp.ToDate;

                                                    //            DateTime from = DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null);
                                                    //            DateTime to = Convert.ToDateTime(cp.ToDate);
                                                    //            TimeSpan totaldays = Convert.ToDateTime(from) - Convert.ToDateTime(to);
                                                    //            //drHotelCancellation["daysBeforeCheckIn"] = Convert.ToString(Convert.ToInt32(totaldays.Days) + 2);
                                                    //            drHotelCancellation["daysBeforeCheckIn"] = Convert.ToString(Convert.ToInt32(totaldays.Days));
                                                    //        }
                                                    //        else
                                                    //        {
                                                    //            drHotelCancellation["CompanyCancellationPrice"] = cp.CancellationPrice;
                                                    //            drHotelCancellation["AgentCancellationPrice"] = cp.CancellationPrice;
                                                    //        }
                                                    //    }
                                                    //}
                                                    DataRow drhotelRoom = DtHotelRoom.NewRow();
                                                    drhotelRoom["SupplierName"] = "xmlout";
                                                    drhotelRoom["OwnsystemHotelId"] = Item.SearchPara.Filters.HotelIDs;
                                                    drhotelRoom["ContractID"] = 0;
                                                    drhotelRoom["UniqueNo"] = RoomTokenRP;// rm.RoomToken;
                                                    drhotelRoom["BookingStatus"] = "Available";// rm.BookingStatus; //Convert.ToString(drRoomCategories[rc]["BookingStatus"]);
                                                    drhotelRoom["RoomNo"] = "1";// rm.RoomNo;
                                                                                //drhotelRoom["PromotionId"] = 0;
                                                                                //drhotelRoom["HSupplierId"] = "";// Convert.ToString(drRoomCategories[rc]["SupplierId"]);
                                                                                //drhotelRoom["HRateTypeId"] = "";// Convert.ToString(drRoomCategories[rc]["RateTypeId"]);
                                                                                //drhotelRoom["RoomTypeName"] = rm.RoomTypeName;//RoomDesc;
                                                    drhotelRoom["RoomTypeName"] = RoomName; // "Stander"; //gimmonixDetail.GimmonixRoomName;
                                                    drhotelRoom["OfferCode"] = ""; // OfferCode;
                                                    drhotelRoom["MealName"] = MealName;// rm.MealName; //Convert.ToString(drRoomCategories[rc]["BoardBasis"]);
                                                                                       //drhotelRoom["RoomTypeId"] = rm.RoomToken; // Convert.ToString(drRoomCategories[rc]["RoomTypeId"]); 
                                                    drhotelRoom["RoomTypeId"] = "123";// processedRoomTypeID;// "123";// gimmonixDetail.GimmonixRoomId; // Convert.ToString(drRoomCategories[rc]["RoomTypeId"]); 
                                                                                                    //drhotelRoom["RoomTypeCode"] = rm.RoomToken; //RateplancodeSiteminder;
                                                    drhotelRoom["BoardTypeID"] = MealName;//rm.RoomToken;
                                                    drhotelRoom["MealId"] = MealName; // Convert.ToString(drRoomCategories[rc]["BoardBasis"]);
                                                                                      //By manish
                                                    drhotelRoom["OfferDescription"] = "";//Convert.ToString(drRoomCategories[rc]["OfferDescription"]);
                                                    drhotelRoom["SupplierCurrency"] = availabilityResponse.AvailabilityRS.Currency; //CurrencyCode;
                                                    drhotelRoom["SHRUI"] = "";
                                                    drhotelRoom["CityId"] = GTAResponse.Tables[0].Rows[0]["CityId"];
                                                    drhotelRoom["IsCombineRoom"] = true;
                                                    drhotelRoom["FinalSellingPrice"] = totalprice;// rm.Price;
                                                    //drhotelRoom["allocationDetails"] = SearchParam.Rooms.Count.ToString();
                                                    drhotelRoom["allocationDetails"] = 1;// SearchParam.Rooms.Count.ToString(); //needtochange
                                                    //if (ExtraPerson != string.Empty)
                                                    // drhotelRoom["EssentialInformation"] = ExtraPerson;
                                                    int DaysBeforeCheckIn = daysBeforeCheckIn;// Convert.IsDBNull(drHotelCancellation["daysBeforeCheckIn"]) ? 0 : Convert.ToInt32(drHotelCancellation["daysBeforeCheckIn"]);
                                                    string RatePlanCode =
                                                    advancedOptions.searchKeyAgoda + "^^" +
                                                    drhotelRoom["RoomTypeId"] + "^^" +
                                                    Item.SearchPara.Filters.HotelIDs + "^^" +
                                                    drXmlHotel["MealName"] + "^^" +
                                                    //drhotelRoom["BoardTypeID"] + "^^" +
                                                    drhotelRoom["SupplierCurrency"] + "^^" +
                                                    //drhotelRoom["OTHPromoCode"] + "^^" +
                                                    HotelOptionIdRP + "$$$" + SearchKeyRP + "$$$" + RoomTokenRP + "$$$" + totalprice + "^^" +
                                                    //HotelOptionIdRP + "$$$" + SearchKeyRP + "$$$" + RoomTokenRP + "^^" +
                                                    MasterSupplierID + "^^" +
                                                    DaysBeforeCheckIn + "^^" +
                                                    "Rateaura" + "^^" + ChainCode_Agoda + "^^" + AgentPercentage + "^^" + requestName;

                                                    byte[] textBytes = Encoding.UTF8.GetBytes(RatePlanCode);

                                                    // Encode byte array to Base64 string
                                                    drhotelRoom["channel"] = ChainCode_Agoda;

                                                    string base64Encoded = Convert.ToBase64String(textBytes);
                                                    drhotelRoom["RoomTypeCode"] = base64Encoded;// RatePlanCode;
                                                    DtHotelRoom.Rows.Add(drhotelRoom);
                                                    //DataRow drHotelCancellation = dtCancellation.NewRow();
                                                    drHotelCancellation["SupplierName"] = "xmlout";
                                                    drHotelCancellation["OwnsystemHotelId"] = Item.SearchPara.Filters.HotelIDs;
                                                    drHotelCancellation["RoomNo"] = 1; // Convert.ToString(drRoomCategories[rc]["RoomNo"]);
                                                                                       //drHotelCancellation["RoomTypeId"] = rm.RoomToken;//Convert.ToString(drRoomCategories[rc]["RoomTypeId"]);
                                                    drHotelCancellation["RoomTypeId"] = "123";// processedRoomTypeID;// "123";// gimmonixDetail.GimmonixRoomId;//Convert.ToString(drRoomCategories[rc]["RoomTypeId"]);
                                                    drHotelCancellation["BoardTypeID"] = RoomTokenRP;// rm.RoomToken;
                                                    drHotelCancellation["RoomTypeCode"] = base64Encoded;// RatePlanCode;
                                                                                                        //drHotelCancellation["RoomTypeName"] = rm.RoomTypeName;//RoomDesc;
                                                    drHotelCancellation["RoomTypeName"] = RoomName;// "Stander";// gimmonixDetail.GimmonixRoomName;
                                                    drHotelCancellation["MealId"] = MealName;//Convert.ToString(drRoomCategories[rc]["BoardBasis"]);
                                                    dtCancellation.Rows.Add(drHotelCancellation);
                                                    //if (rm.CancellationPolicy.Count > 1)
                                                    //{
                                                    //    //    foreach (var cp in rm.CancellationPolicy)
                                                    //    //    {
                                                    //    //        if (cp.CancellationPrice == 0)
                                                    //    //        {
                                                    //    //            drHotelCancellation["FromDate"] = cp.FromDate;
                                                    //    //            drHotelCancellation["ToDate"] = cp.ToDate;

                                                    //    //            DateTime from = DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null);
                                                    //    //            DateTime to = Convert.ToDateTime(cp.ToDate);
                                                    //    //            TimeSpan totaldays = Convert.ToDateTime(from) - Convert.ToDateTime(to);
                                                    //    //            //drHotelCancellation["daysBeforeCheckIn"] = Convert.ToString(Convert.ToInt32(totaldays.Days) + 2);
                                                    //    //            drHotelCancellation["daysBeforeCheckIn"] = Convert.ToString(Convert.ToInt32(totaldays.Days));
                                                    //    //        }
                                                    //    //        else
                                                    //    //        {
                                                    //    //            drHotelCancellation["CompanyCancellationPrice"] = cp.CancellationPrice;
                                                    //    //            drHotelCancellation["AgentCancellationPrice"] = cp.CancellationPrice;
                                                    //    //        }
                                                    //    //    }

                                                    //    drHotelCancellation["SupplierCurrency"] = availabilityResponse.AvailabilityRS.Currency;
                                                    //    drHotelCancellation["BuyingPrice"] = rm.Price;

                                                    //    //drHotelCancellation["isNRF"] = Convert.ToBoolean(IsNonRef);
                                                    //    dtCancellation.Rows.Add(drHotelCancellation);
                                                    //}
                                                    //else
                                                    //{
                                                    //    dtCancellation.Rows.Add(drHotelCancellation);
                                                    //}
                                                    DataRow drHotelRoomRateBreackUp = DtHotelRoomRateBreackUp.NewRow();
                                                    drHotelRoomRateBreackUp["RoomNo"] = 1;
                                                    drHotelRoomRateBreackUp["SupplierName"] = "xmlout";
                                                    drHotelRoomRateBreackUp["OwnsystemHotelId"] = Item.SearchPara.Filters.HotelIDs;
                                                    drHotelRoomRateBreackUp["ContractID"] = 0;
                                                    //drHotelRoomRateBreackUp["RoomTypeName"] = rm.RoomTypeName;
                                                    drHotelRoomRateBreackUp["RoomTypeName"] = RoomName;// "Stander";// gimmonixDetail.GimmonixRoomName;
                                                                                                       //drHotelRoomRateBreackUp["RoomTypeId"] = rm.RoomToken;
                                                    drHotelRoomRateBreackUp["RoomTypeId"] = "123";// processedRoomTypeID; // "123";// gimmonixDetail.GimmonixRoomId;
                                                    drHotelRoomRateBreackUp["RoomTypeCode"] = base64Encoded;// RatePlanCode;//RateplancodeSiteminder;
                                                    drHotelRoomRateBreackUp["BoardTypeID"] = RoomTokenRP;// rm.RoomToken;
                                                    drHotelRoomRateBreackUp["MealId"] = MealName; // Convert.ToString(drRoomCategories[rc]["BoardBasis"]);
                                                    drHotelRoomRateBreackUp["Meal"] = MealName;// rm.MealName; //Convert.ToString(drRoomCategories[rc]["BoardBasis"]);
                                                    drHotelRoomRateBreackUp["BookingStatus"] = "Available";// rm.BookingStatus;//Convert.ToString(drRoomCategories[rc]["BookingStatus"]);
                                                                                                           //drHotelRoomRateBreackUp["DateOfWeek"] = WeekColumn;
                                                                                                           //drHotelRoomRateBreackUp["Date"] = datetime;
                                                    drHotelRoomRateBreackUp["Rate"] = totalprice;// rm.Price;
                                                                                                 //drHotelRoomRateBreackUp["RateType"] = Convert.ToString(DTdSurchargerdate[MP]["SurchargeName"]);
                                                                                                 //drHotelRoomRateBreackUp["OrderNo"] = cntno;                                        
                                                    drHotelRoomRateBreackUp["SupplierCurrency"] = availabilityResponse.AvailabilityRS.Currency;
                                                    //drHotelRoomRateBreackUp["CompanyRate"] = Math.Round(Convert.ToDouble(DTdSurchargerdate[MP]["Price"]), 2);
                                                    //drHotelRoomRateBreackUp["NewRateType"] = Convert.ToString(DTdrdate[0]["RateType"]);
                                                    //drHotelRoomRateBreackUp["RateTypeId"] = Convert.ToString(DTdrdate[0]["RateTypeId"]);
                                                    //drHotelRoomRateBreackUp["SupplierId"] = Convert.ToString(DTdrdate[0]["SupplierId"]);
                                                    // drHotelRoomRateBreackUpSurcharge["RatePlanCode"] = RateplancodeSiteminder;                                        
                                                    drHotelRoomRateBreackUp["BuyingPrice"] = totalprice;// rm.Price;
                                                    DtHotelRoomRateBreackUp.Rows.Add(drHotelRoomRateBreackUp);
                                                }
                                            }
                                        }
                                        //}
                                    }
                                }

                               // Options ops = (_supplierRes.GetCommonRoomForSuppliers(Convert.ToInt64(giitaCode), commonSearch.tokenData, commonSearch.advancedOptions, LowestRate, drXmlHotel, DtHotelRoom, dtCancellation, DtHotelRoomRateBreackUp, SearchParam));
                                Options ops = await _supplierRes.GetCommonRoomForSuppliers_agoda(Convert.ToInt64(giitaCode), commonSearch.tokenData, commonSearch.advancedOptions, LowestRate, drXmlHotel, DtHotelRoom, dtCancellation, DtHotelRoomRateBreackUp, SearchParam);
                                if (ops != null)
                                    options.Add(ops);
                            }
                            catch (Exception ex)
                            {
                                _exception.Error(ex);

                                return options;
                            }
                            finally
                            {
                                drXmlHotel = null;
                                DtHotelRoom = null;
                                DtHotelRoomRateBreackUp = null;
                                dtCancellation = null;
                            }

                            //Options_agoda ops = (HotelXML_Suppliers.GetCommonRoomForSuppliers(Convert.ToInt64(giitaCode), commonSearch.tokenData, commonSearch.advancedOptions, LowestRate, drXmlHotel, DtHotelRoom, dtCancellation, DtHotelRoomRateBreackUp, SearchParam));
                            //if (ops != null)
                            //    options.Add(ops);
                        }
                    }
                    return options;  //needtochange
                }
                catch (Exception ex)
                {
                    _exception.Error(ex);
                    return options;
                }
                finally
                {
                    //GC.Collect();
                }
            }
            else
            {
                Item.ResponseType = "NoCount";
            }
            return options;
        }

        public string getMappedRoom_rlm(string originalRoomName, DataTable DtHotelInformation, int HotelId)
        {
            try
            {
                DataRow[] dtr = DtHotelInformation.Select("StandardRoomName='" + originalRoomName + "' and HotelId='" + HotelId + "'");
                if (dtr.Length > 0)
                {
                    originalRoomName = dtr[0]["RoomId"].ToString();
                    return originalRoomName;
                }
                else
                {
                    return "";
                }
            }
            catch (Exception ex)
            {
                // Log the exception
                //ExceptionManager.LogException(ex);

                // Return the original room name in case of an exception
                return "";
            }

        }

        //public async Task<List<Options>> XMLOut_HotelPreCheck(HotelX Item, CommonSearch commonSearch, DataSet GTAResponse, string HotelID, HotelSearchParameter SearchParam, DataTable dtAgentDetail, DataTable dtSuplierMarkup, DataTable dtMarkupDetails, AdvancedOptions advancedOptions, string ratePlanCode)
        //public async Task<List<Options>> XMLOut_HotelPreCheck(HotelX Item, CommonSearch commonSearch, DataSet GTAResponse, string HotelID, HotelSearchParameter SearchParam, DataTable dtAgentDetail, DataTable dtMarkupDetails, AdvancedOptions advancedOptions, string ratePlanCode)
        //{
        //    List<Options> options = new List<Options>();
        //    if (GTAResponse != null && GTAResponse.Tables.Count > 0 && GTAResponse.Tables[0].Rows.Count > 0)
        //    {
        //        string giitaCode = "";
        //        double LowestRate = 0;
        //        DataTable DtHotel = _commonMethod.GenerateDataTableNewV2();
        //        //DataRow drXmlHotel = DtHotel.NewRow();
        //        //DataTable DtHotelRoom = Common.GenerateRoomtypeDataTableNewV2();
        //        //DataTable dtCancellation = Common.GenerateCancellationDataTableNewV2();
        //        //DataTable DtHotelRoomRateBreackUp = Common.GenerateoomRateBreackUpDataTableNewV2();
        //        DataTable dtHotelAllocation = _commonMethod.GenerateHotelAllocationTableNewV2();
        //        try
        //        {
        //            if (Item != null && Item.RQRS != null && Item.RQRS[0].Response != null)
        //            {
        //                //giitaCode = Convert.ToString(GTAResponse.Tables[0].Rows[0]["GiataId"]);
        //                giitaCode = Convert.ToString(GTAResponse.Tables[0].Rows[0]["Id"]);

        //                CommoXMLOutPreCheckResponse.PreCheckResponse preCheckResponse = new CommoXMLOutPreCheckResponse.PreCheckResponse();

        //                //AvailabilityResponse availabilityResponse = new AvailabilityResponse();
        //                preCheckResponse = JsonConvert.DeserializeObject<CommoXMLOutPreCheckResponse.PreCheckResponse>(Item.RQRS[0].Response);
        //                if (preCheckResponse.PreBookRS != null)
        //                {
        //                    string HotelOptionIdRP = string.Empty;
        //                    string SearchKeyRP = string.Empty;

        //                    string Status = preCheckResponse.PreBookRS.HotelOption.Status;
        //                    if (Status.ToLower() == "bookable")
        //                    {
        //                        if (preCheckResponse.PreBookRS.HotelOption != null)
        //                        {
        //                            DataRow drXmlHotel = DtHotel.NewRow();
        //                            DataTable DtHotelRoom = _commonMethod.GenerateRoomtypeDataTableNewV2();
        //                            DataTable dtCancellation = _commonMethod.GenerateCancellationDataTableNewV2();
        //                            DataTable DtHotelRoomRateBreackUp = _commonMethod.GenerateoomRateBreackUpDataTableNewV2();
        //                            try
        //                            {
        //                                getrateplan.Root rateplan = new getrateplan.Root();
        //                                rateplan = JsonConvert.DeserializeObject<getrateplan.Root>(ratePlanCode);

        //                                ratePlanCode = rateplan.token;
        //                                byte[] decodedBytes = Convert.FromBase64String(ratePlanCode);
        //                                // Convert the byte array to a readable string (assuming it's UTF8 encoded)
        //                                string decoded_ratePlanCode = Encoding.UTF8.GetString(decodedBytes);
        //                                string RoomRatePlanCode = decoded_ratePlanCode;
        //                                string[] spitid1 = null;
        //                                string[] spitid2 = null;
        //                                string HotelOptionId = "";
        //                                string SearchKey = "";
        //                                string RoomToken = "";
        //                                string RoomNo = "";
        //                                if (!string.IsNullOrEmpty(ratePlanCode))
        //                                {
        //                                    spitid1 = RoomRatePlanCode.Split(new String[] { "^^" }, StringSplitOptions.None);
        //                                    spitid2 = spitid1[5].Split(new String[] { "$$$" }, StringSplitOptions.None);

        //                                    HotelOptionId = spitid2[0];
        //                                    SearchKey = spitid2[1];
        //                                    RoomToken = spitid2[2];
        //                                    RoomNo = "1";
        //                                }
        //                                string SupplierID = spitid1[6];
        //                                int MasterSupplierID = 0;
        //                                MasterSupplierID = Convert.ToInt32(spitid1[6]);
        //                                SearchKeyRP = spitid2[1];
        //                                HotelOptionIdRP = spitid2[0];

        //                                //var lowestPriceRoom = GetLowestPriceRoom(availabilityResponse);
        //                                //LowestRate = lowestPriceRoom.Price;
        //                                LowestRate = preCheckResponse.PreBookRS.HotelOption.TotalPrice;

        //                                drXmlHotel["GIITACode"] = giitaCode;
        //                                drXmlHotel["OwnsystemHotelId"] = Item.SearchPara.Filters.HotelIDs;
        //                                drXmlHotel["RaynaId"] = Item.SearchPara.Filters.HotelIDs;
        //                                drXmlHotel["SupplierCurrency"] = preCheckResponse.PreBookRS.Currency;
        //                                drXmlHotel["XmlHotelCity"] = GTAResponse.Tables[0].Rows[0]["CityId"];
        //                                drXmlHotel["XMLHotelCode"] = Item.SearchPara.Filters.HotelIDs;
        //                                drXmlHotel["HotelName"] = GTAResponse.Tables[0].Rows[0]["Name"];
        //                                //drXmlHotel["HotelFrontImage"] = GTAImage1;
        //                                drXmlHotel["Address"] = GTAResponse.Tables[0].Rows[0]["AddressLine"];
        //                                //drXmlHotel["ReviewCount"] = ReviewCount;
        //                                //drXmlHotel["ReviewRating"] = ReviewRating;
        //                                drXmlHotel["Location"] = GTAResponse.Tables[0].Rows[0]["LandMark"];
        //                                drXmlHotel["XMLCountry"] = GTAResponse.Tables[0].Rows[0]["CountryId"];
        //                                drXmlHotel["XMLCity"] = GTAResponse.Tables[0].Rows[0]["CityId"];
        //                                //drXmlHotel["Rating"] = Rating;
        //                                //drXmlHotel["GroupName"] = Chain;
        //                                //drXmlHotel["SortSequence"] = SortSeq;
        //                                //drXmlHotel["latitude"] = Convert.ToString(Latitude).ToLower();
        //                                //drXmlHotel["longitute"] = Convert.ToString(Longitude).ToLower();
        //                                //drXmlHotel["SellingPrice"] = LowestRate;
        //                                //drXmlHotel["FinalSellingPrice"] = LowestRate;
        //                                //drXmlHotel["BookingStatus"] = BookingStatus;
        //                                //drXmlHotel["SupplierName"] = "siteminder";
        //                                //drXmlHotel["OfficeID"] = 0;
        //                                //drXmlHotel["ContractID"] = 0;
        //                                //drXmlHotel["Reviews"] = 0;
        //                                //drXmlHotel["UniqueNo"] = uniqueno;
        //                                //drXmlHotel["IsShowSupplier"] = blnIsShowSupplier;
        //                                //drXmlHotel["VatdiffAmount"] = 0;
        //                                //drXmlHotel["MasterSupplierId"] = Convert.ToInt32(GTAResponse.Tables[0].Rows[h]["SupplierId"]);
        //                                //drXmlHotel["SupplierId"] = "14";
        //                                //drXmlHotel["ReservationEmail"] = Convert.ToString(GTAResponse.Tables[0].Rows[h]["ReservationEmail"]);
        //                                //drXmlHotel["HotelContactNo"] = ""; // Convert.ToString(GTAResponse.Tables[0].Rows[h]["HotelContactNo"]);

        //                                DtHotel.Rows.Add(drXmlHotel);

        //                                //ROOM
        //                                string RoomUniqueId = string.Empty;
        //                                if (preCheckResponse.PreBookRS.HotelOption.HotelRooms != null && preCheckResponse.PreBookRS.HotelOption.HotelRooms.Count > 0)
        //                                {
        //                                    int k = 0;
        //                                    string RoomTokenRP = string.Empty;
        //                                    //foreach (var rm in preCheckResponse.PreBookRS.HotelOption.HotelRooms)
        //                                    {
        //                                        //RoomUniqueId += rm.RoomNo + "_" + rm.UniqueId + ",";
        //                                        //GimmonixDetail gimmonixDetail = new GimmonixDetail();
        //                                        //if (GTAResponse.Tables.Count > 1 && GTAResponse.Tables[1] != null && GTAResponse.Tables[1].Rows.Count > 0)
        //                                        //{
        //                                        //    gimmonixDetail = GTAResponse.Tables[1].AsEnumerable()
        //                                        //                    .Where(row => row.Field<int>("Supplierid") == Convert.ToInt32(SupplierID))
        //                                        //                    .Select(row => new GimmonixDetail
        //                                        //                    {
        //                                        //                        GimmonixRoomId = row.Field<string>("gimmonixRoomId"),
        //                                        //                        GimmonixRoomName = row.Field<string>("GimmonixRoomName")
        //                                        //                    })
        //                                        //                    .FirstOrDefault();

        //                                        //    //GimmonixRoomId = GimmonixRoomId + "_" + SupplierID + rm.RoomToken;                                                    
        //                                        //}
        //                                        //else { continue; }

        //                                        RoomTokenRP = RoomToken;
        //                                        RoomTokenRP = "";
        //                                        int daysBeforeCheckIn = 365;
        //                                        double totalprice = 0;
        //                                        string RoomName = "";
        //                                        string MealName = "Ro";
        //                                        double price = 0;
        //                                        string processedRoomTypeID = "";
        //                                        bool isroom = true;
        //                                        int ChainCode_Agoda = Convert.ToInt32(spitid1[9]);
        //                                        double ChainMarkup = Convert.ToDouble(spitid1[10]);


        //                                        //foreach (var rm in availabilityResponse.AvailabilityRS.HotelResult[0].HotelOption[i].HotelRooms[j])
        //                                        {
        //                                            for (int kk = 0; kk < preCheckResponse.PreBookRS.HotelOption.HotelRooms.Count; kk++)
        //                                            {
        //                                                //GimmonixDetail gimmonixDetail = new GimmonixDetail();
        //                                                //if (GTAResponse.Tables.Count > 1 && GTAResponse.Tables[1] != null && GTAResponse.Tables[1].Rows.Count > 0)
        //                                                //{
        //                                                //    //GimmonixRoomId = GTAResponse.Tables[1].AsEnumerable()
        //                                                //    //            .Where(row => row.Field<int>("Supplierid") == Convert.ToInt32(SupplierID) && row.Field<string>("SupplierRoomName") == rm.RoomTypeName)
        //                                                //    //            .Select(row => row.Field<string>("gimmonixRoomId"))
        //                                                //    //            .FirstOrDefault();

        //                                                //    gimmonixDetail = GTAResponse.Tables[1].AsEnumerable()
        //                                                //                    .Where(row => row.Field<int>("Supplierid") == Convert.ToInt32(SupplierID))
        //                                                //                    .Select(row => new GimmonixDetail
        //                                                //                    {
        //                                                //                        GimmonixRoomId = row.Field<string>("gimmonixRoomId"),
        //                                                //                        GimmonixRoomName = row.Field<string>("GimmonixRoomName")
        //                                                //                    })
        //                                                //                    .FirstOrDefault();
        //                                                //    //GimmonixRoomId = GimmonixRoomId + "_" + SupplierID + rm.RoomToken;                                                    
        //                                                //}
        //                                                //else { continue; }
        //                                                //if (availabilityResponse.AvailabilityRS.HotelResult[0].HotelOption[i].HotelRooms[j][0].RoomTypeName == availabilityResponse.AvailabilityRS.HotelResult[0].HotelOption[i].HotelRooms[j][kk].RoomTypeName && availabilityResponse.AvailabilityRS.HotelResult[0].HotelOption[i].HotelRooms[j][0].MealName == availabilityResponse.AvailabilityRS.HotelResult[0].HotelOption[i].HotelRooms[j][kk].MealName)
        //                                                {
        //                                                    double PRprice = preCheckResponse.PreBookRS.HotelOption.TotalPrice;
        //                                                    RoomUniqueId += preCheckResponse.PreBookRS.HotelOption.HotelRooms[kk].RoomNo + "_" + preCheckResponse.PreBookRS.HotelOption.HotelRooms[kk].UniqueId + ",";
        //                                                    double totalprice1 = preCheckResponse.PreBookRS.HotelOption.HotelRooms[kk].Price;
        //                                                    RoomName = preCheckResponse.PreBookRS.HotelOption.HotelRooms[kk].RoomTypeName;

        //                                                    processedRoomTypeID = "123"; // getMappedRoom_rlm(RoomName.Split('|')[0].Trim(), GTAResponse.Tables[1], Convert.ToInt32(Item.SearchPara.Filters.HotelIDs)); //needtochange

        //                                                    if (processedRoomTypeID == "")
        //                                                    {
        //                                                        isroom = false;
        //                                                        continue;
        //                                                    }

        //                                                    MealName = preCheckResponse.PreBookRS.HotelOption.HotelRooms[kk].MappedMealName;
        //                                                    //totalprice = totalprice + totalprice1;
        //                                                    daysBeforeCheckIn = Convert.ToInt32(spitid1[7]);

        //                                                    int UniqueId = preCheckResponse.PreBookRS.HotelOption.HotelRooms[kk].UniqueId;

        //                                                    //needtochange
        //                                                    //try
        //                                                    //{
        //                                                    //   // DLConnection dBConnection1 = new DLConnection();
        //                                                    //    SqlParameter[] para1 = new SqlParameter[]{
        //                                                    //new SqlParameter("@totalprice1",Math.Round(totalprice1,2)),
        //                                                    //new SqlParameter("@PRprice",Math.Round(PRprice,2)),
        //                                                    //new SqlParameter("@UniqueId",UniqueId),
        //                                                    //new SqlParameter("@ChainMarkup",ChainMarkup)
        //                                                    //};
        //                                                    //    //DataTable DsPrice = dBConnection1.GetDataTable("AgodaUpdatePrice_channalWise", para1);
        //                                                    //    DataTable DsPrice = _dlConnection.GetDataTable("AgodaUpdatePrice_channalWise", para1);
        //                                                    //    price += Convert.ToDouble(DsPrice.Rows[0]["AGprice"]);

        //                                                    //}
        //                                                    //catch (System.Exception ex)
        //                                                    //{
        //                                                    //   // Common.Error(ex);
        //                                                    //    _exception.Error(ex);

        //                                                    //}
        //                                                    //===============
        //                                                }
        //                                            }
        //                                            if (isroom == false)
        //                                            {
        //                                                return options;
        //                                            }
        //                                            DataRow drhotelRoom = DtHotelRoom.NewRow();

        //                                            drhotelRoom["SupplierName"] = "xmlout";
        //                                            drhotelRoom["OwnsystemHotelId"] = Item.SearchPara.Filters.HotelIDs;
        //                                            drhotelRoom["ContractID"] = 0;
        //                                            drhotelRoom["UniqueNo"] = RoomToken;
        //                                            drhotelRoom["BookingStatus"] = preCheckResponse.PreBookRS.HotelOption.Status;
        //                                            drhotelRoom["RoomNo"] = "1";
        //                                            //drhotelRoom["PromotionId"] = 0;
        //                                            //drhotelRoom["HSupplierId"] = "";
        //                                            //drhotelRoom["HRateTypeId"] = "";
        //                                            drhotelRoom["RoomTypeName"] = RoomName;// gimmonixDetail.GimmonixRoomName;
        //                                            drhotelRoom["OfferCode"] = "";
        //                                            drhotelRoom["MealName"] = MealName;// rm.MealName;
        //                                            drhotelRoom["RoomTypeId"] = processedRoomTypeID; // "123";// gimmonixDetail.GimmonixRoomId; drhotelRoom["BoardTypeID"] = RoomToken;
        //                                            drhotelRoom["MealId"] = MealName;
        //                                            drhotelRoom["OfferDescription"] = "";
        //                                            drhotelRoom["SupplierCurrency"] = preCheckResponse.PreBookRS.Currency;
        //                                            drhotelRoom["SHRUI"] = "";
        //                                            drhotelRoom["CityId"] = GTAResponse.Tables[0].Rows[0]["CityId"];
        //                                            drhotelRoom["IsCombineRoom"] = true;
        //                                            //drhotelRoom["FinalSellingPrice"] = rm.Price;   //Single Room
        //                                            //drhotelRoom["FinalSellingPrice"] = price;// preCheckResponse.PreBookRS.HotelOption.TotalPrice; // Combine Room //needtochange
        //                                            drhotelRoom["FinalSellingPrice"] = preCheckResponse.PreBookRS.HotelOption.TotalPrice; // Combine Room
        //                                            drhotelRoom["allocationDetails"] = SearchParam.Rooms.Count.ToString();
        //                                            //if (ExtraPerson != string.Empty)
        //                                            // drhotelRoom["EssentialInformation"] = ExtraPerson;

        //                                            string RatePlanCode = ratePlanCode;
        //                                            //advancedOptions.searchKeyAgoda + "^^" +
        //                                            //drhotelRoom["RoomTypeId"] + "^^" +
        //                                            //Item.SearchPara.Filters.HotelIDs + "^^" +
        //                                            //drXmlHotel["MealName"] + "^^" +
        //                                            ////drhotelRoom["BoardTypeID"] + "^^" +
        //                                            //drhotelRoom["SupplierCurrency"] + "^^" +
        //                                            ////drhotelRoom["OTHPromoCode"] + "^^" +
        //                                            //HotelOptionIdRP + "$$$" + SearchKeyRP + "$$$" + RoomTokenRP + "$$$" + rm.Price + "^^" +
        //                                            //MasterSupplierID + "^^" +
        //                                            //"Travelbullz";

        //                                            drhotelRoom["channel"] = ChainCode_Agoda;
        //                                            drhotelRoom["RoomTypeCode"] = RatePlanCode;
        //                                            DtHotelRoom.Rows.Add(drhotelRoom);

        //                                            DataRow drHotelCancellation = dtCancellation.NewRow();
        //                                            drHotelCancellation["SupplierName"] = "xmlout";
        //                                            drHotelCancellation["OwnsystemHotelId"] = Item.SearchPara.Filters.HotelIDs;
        //                                            drHotelCancellation["RoomNo"] = 1; // Convert.ToString(drRoomCategories[rc]["RoomNo"]);
        //                                                                               //drHotelCancellation["RoomTypeId"] = rm.RoomToken;//Convert.ToString(drRoomCategories[rc]["RoomTypeId"]);
        //                                            drHotelCancellation["RoomTypeId"] = processedRoomTypeID;// "123";// gimmonixDetail.GimmonixRoomId;//Convert.ToString(drRoomCategories[rc]["RoomTypeId"]);
        //                                            drHotelCancellation["BoardTypeID"] = RoomToken;
        //                                            drHotelCancellation["RoomTypeCode"] = RatePlanCode;
        //                                            //drHotelCancellation["RoomTypeName"] = rm.RoomTypeName;//RoomDesc;
        //                                            drHotelCancellation["RoomTypeName"] = RoomName;// "Stander";// gimmonixDetail.GimmonixRoomName;
        //                                            drHotelCancellation["MealId"] = MealName;//Convert.ToString(drRoomCategories[rc]["BoardBasis"]);
        //                                            if (k == 0)
        //                                            {
        //                                                k++;
        //                                                if (Convert.ToInt32(spitid1[7]) > 0)
        //                                                {
        //                                                    //foreach (var cp in rm.CancellationPolicy)
        //                                                    //{
        //                                                    //    if (cp.CancellationPrice == 0)
        //                                                    //    {
        //                                                    //        drHotelCancellation["FromDate"] = cp.FromDate;
        //                                                    //        drHotelCancellation["ToDate"] = cp.ToDate;

        //                                                    //        DateTime from = DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null);
        //                                                    //        DateTime to = Convert.ToDateTime(cp.ToDate);
        //                                                    //        TimeSpan totaldays = Convert.ToDateTime(from) - Convert.ToDateTime(to);
        //                                                    //        drHotelCancellation["daysBeforeCheckIn"] = Convert.ToString(Convert.ToInt32(totaldays.Days));
        //                                                    //    }
        //                                                    //    else
        //                                                    //    {
        //                                                    //        drHotelCancellation["CompanyCancellationPrice"] = cp.CancellationPrice;
        //                                                    //        drHotelCancellation["AgentCancellationPrice"] = cp.CancellationPrice;
        //                                                    //    }
        //                                                    //}
        //                                                    drHotelCancellation["daysBeforeCheckIn"] = Convert.ToString(Convert.ToInt32(spitid1[7]));
        //                                                    drHotelCancellation["CompanyCancellationPrice"] = spitid2[3];
        //                                                    drHotelCancellation["AgentCancellationPrice"] = spitid2[3];
        //                                                    drHotelCancellation["SupplierCurrency"] = preCheckResponse.PreBookRS.Currency;
        //                                                    //drHotelCancellation["BuyingPrice"] = rm.Price; // Single

        //                                                    // ================
        //                                                   // drHotelCancellation["BuyingPrice"] = price;// preCheckResponse.PreBookRS.HotelOption.TotalPrice; //Combine  //needtochange
        //                                                                                               //drHotelCancellation["isNRF"] = Convert.ToBoolean(IsNonRef);


        //                                                    drHotelCancellation["BuyingPrice"] = preCheckResponse.PreBookRS.HotelOption.TotalPrice; //Combine;
        //                                                    // ================


        //                                                    dtCancellation.Rows.Add(drHotelCancellation);
        //                                                }
        //                                                else
        //                                                {
        //                                                    dtCancellation.Rows.Add(drHotelCancellation);
        //                                                }
        //                                            }
        //                                            DataRow drHotelRoomRateBreackUp = DtHotelRoomRateBreackUp.NewRow();
        //                                            drHotelRoomRateBreackUp["RoomNo"] = 1;
        //                                            drHotelRoomRateBreackUp["SupplierName"] = "xmlout";
        //                                            drHotelRoomRateBreackUp["OwnsystemHotelId"] = Item.SearchPara.Filters.HotelIDs;
        //                                            drHotelRoomRateBreackUp["ContractID"] = 0;
        //                                            //drHotelRoomRateBreackUp["RoomTypeName"] = rm.RoomTypeName;
        //                                            drHotelRoomRateBreackUp["RoomTypeName"] = "Stander";// gimmonixDetail.GimmonixRoomName;
        //                                                                                                //drHotelRoomRateBreackUp["RoomTypeId"] = rm.RoomToken;
        //                                            drHotelRoomRateBreackUp["RoomTypeId"] = processedRoomTypeID;// "123";// gimmonixDetail.GimmonixRoomId;
        //                                            drHotelRoomRateBreackUp["RoomTypeCode"] = RatePlanCode;//RateplancodeSiteminder;
        //                                            drHotelRoomRateBreackUp["BoardTypeID"] = RoomToken;
        //                                            drHotelRoomRateBreackUp["MealId"] = MealName; // Convert.ToString(drRoomCategories[rc]["BoardBasis"]);
        //                                            drHotelRoomRateBreackUp["Meal"] = MealName;// rm.MealName; //Convert.ToString(drRoomCategories[rc]["BoardBasis"]);
        //                                            drHotelRoomRateBreackUp["BookingStatus"] = preCheckResponse.PreBookRS.HotelOption.Status;//Convert.ToString(drRoomCategories[rc]["BookingStatus"]);
        //                                                                                                                                     //drHotelRoomRateBreackUp["DateOfWeek"] = WeekColumn;
        //                                                                                                                                     //drHotelRoomRateBreackUp["Date"] = datetime;
        //                                                                                                                                     //drHotelRoomRateBreackUp["Rate"] = rm.Price; // Single
        //                                            drHotelRoomRateBreackUp["Rate"] = preCheckResponse.PreBookRS.HotelOption.TotalPrice; // Combine
        //                                                                                                                                 //drHotelRoomRateBreackUp["RateType"] = Convert.ToString(DTdSurchargerdate[MP]["SurchargeName"]);
        //                                                                                                                                 //drHotelRoomRateBreackUp["OrderNo"] = cntno;                                        
        //                                            drHotelRoomRateBreackUp["SupplierCurrency"] = preCheckResponse.PreBookRS.Currency;
        //                                            //drHotelRoomRateBreackUp["CompanyRate"] = Math.Round(Convert.ToDouble(DTdSurchargerdate[MP]["Price"]), 2);
        //                                            //drHotelRoomRateBreackUp["NewRateType"] = Convert.ToString(DTdrdate[0]["RateType"]);
        //                                            //drHotelRoomRateBreackUp["RateTypeId"] = Convert.ToString(DTdrdate[0]["RateTypeId"]);
        //                                            //drHotelRoomRateBreackUp["SupplierId"] = Convert.ToString(DTdrdate[0]["SupplierId"]);
        //                                            // drHotelRoomRateBreackUpSurcharge["RatePlanCode"] = RateplancodeSiteminder;                                        
        //                                            //drHotelRoomRateBreackUp["BuyingPrice"] = rm.Price; //Single
        //                                            drHotelRoomRateBreackUp["BuyingPrice"] = preCheckResponse.PreBookRS.HotelOption.TotalPrice; //Combine

        //                                            DtHotelRoomRateBreackUp.Rows.Add(drHotelRoomRateBreackUp);
        //                                        }
        //                                    }

        //                                    try
        //                                    {
        //                                        //DLConnection dBConnection = new DLConnection();

        //                                        SqlParameter[] para = new SqlParameter[]{
        //                                  new SqlParameter("@Action","INSERT"),
        //                                  new SqlParameter("@SupplierID",SupplierID),
        //                                  new SqlParameter("@RatePlanCode",ratePlanCode),
        //                                  new SqlParameter("@BookingToken",preCheckResponse.PreBookRS.HotelOption.BookingToken),
        //                                  new SqlParameter("@UniqueId",RoomUniqueId)
        //                                };

        //                                        //int p = dBConnection.ExecuteNonQuery("GetAgodaRoomDetailXMLOut", para);
        //                                        int p = await _dlConnection.ExecuteNonQuery("GetAgodaRoomDetailXMLOut", para);
                                                
        //                                    }
        //                                    catch (System.Exception ex)
        //                                    {
        //                                        _exception.Error(ex);

        //                                    }


        //                                    //Options ops = (HotelXML_Suppliers.GetCommonRoomForSuppliers(Convert.ToInt64(giitaCode), commonSearch.tokenData, commonSearch.advancedOptions, LowestRate, drXmlHotel, DtHotelRoom, dtCancellation, DtHotelRoomRateBreackUp, SearchParam));
        //                                    Options ops = (await _supplierRes.GetCommonRoomForSuppliers_agoda(Convert.ToInt64(giitaCode), commonSearch.tokenData, commonSearch.advancedOptions, LowestRate, drXmlHotel, DtHotelRoom, dtCancellation, DtHotelRoomRateBreackUp, SearchParam));
        //                                    if (ops != null)
        //                                        options.Add(ops);
        //                                }
        //                            }
        //                            catch (Exception ex)
        //                            {
        //                                _exception.Error(ex);
        //                                return options;
        //                            }
        //                            finally
        //                            {
        //                                drXmlHotel = null;
        //                                DtHotelRoom = null;
        //                                DtHotelRoomRateBreackUp = null;
        //                                dtCancellation = null;
        //                            }
        //                        }
        //                        //Options_agoda ops = (HotelXML_Suppliers.GetCommonRoomForSuppliers(Convert.ToInt64(giitaCode), commonSearch.tokenData, commonSearch.advancedOptions, LowestRate, drXmlHotel, DtHotelRoom, dtCancellation, DtHotelRoomRateBreackUp, SearchParam));
        //                        //if (ops != null)
        //                        //    options.Add(ops);
        //                    }
        //                }
        //            }
        //            return options;
        //        }
        //        catch (Exception ex)
        //        {
        //            _exception.Error(ex);
        //            return options;
        //        }
        //        finally
        //        {
        //            //GC.Collect();
        //        }
        //    }
        //    else
        //    {
        //        //Item.ResponseType = "NoCount";
        //    }
        //    return options;
        //}

       

        public class getrateplan
        {
            public class Root
            {
                public string token { get; set; }
            }
        }

      


    }
}
