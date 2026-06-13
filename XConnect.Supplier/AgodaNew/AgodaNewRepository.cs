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
using Microsoft.Extensions.Logging;
using XConnect.Analytics;
using Microsoft.AspNetCore.Http;
using System.Net.Http;
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
        private readonly ILogger<Exception> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IHttpClientFactory _httpClientFactory;





        public AgodaNewRepository(ICommonMethod commonMethod, IException exception, ISupplierRes supplierRes, IDLConnection dlConnection, IOptions<KeySettings> keySettings, ILogger<Exception> logger, IHttpContextAccessor httpContextAccessor, IHttpClientFactory httpClientFactory) 
        { 
             _commonMethod= commonMethod;
            _exception = exception;
            _supplierRes = supplierRes;
            _dlConnection = dlConnection;
            _keySettings = keySettings.Value;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
            _httpClientFactory = httpClientFactory;

        }

        public async Task<List<Options>> XMLOut_HotelSearchWithRooms(string requestName, HotelX Item, CommonSearch commonSearch, DataSet GTAResponse, string HotelID, HotelSearchParameter SearchParam, DataTable dtAgentDetail, DataTable dtMarkupDetails, AdvancedOptions advancedOptions, DataSet DTChainCode, DataSet DTCountryblock)
        //public async Task<List> XMLOut_HotelSearchWithRooms(string requestName, HotelX Item, CommonSearch commonSearch, DataSet GTAResponse, string HotelID, HotelSearchParameter SearchParam, DataTable dtAgentDetail, DataTable dtSuplierMarkup, DataTable dtMarkupDetails, AdvancedOptions advancedOptions, DataSet DTChainCode)
        {
            System.Diagnostics.Stopwatch timerroomx = new System.Diagnostics.Stopwatch();
            timerroomx.Start();

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
                                //string SupplierID = availabilityResponse.AvailabilityRS.HotelResult[0].HotelOption[0].HotelOptionId.Split('|')[Convert.ToInt16(_cacheHotelOption.supplierId)];
                                //if (SupplierID == "")
                                //{
                                //    SupplierID = "162";
                                //}
                                //MasterSupplierID = Convert.ToInt32(SupplierID);
                                SearchKeyRP = availabilityResponse.AvailabilityRS.SearchKey;

                                try
                                {
                                    SearchParam.SearchKey = SearchKeyRP;

                                }
                                catch (Exception exa)
                                {
                                    _exception.Error(exa);
                                }
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
                                    string SupplierID = availabilityResponse.AvailabilityRS.HotelResult[0].HotelOption[i].HotelOptionId.Split('|')[Convert.ToInt16(_cacheHotelOption.supplierId)];
                                    if (SupplierID == "")
                                    {
                                        SupplierID = "162";
                                    }
                                    MasterSupplierID = Convert.ToInt32(SupplierID);
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
                                            string RoomId = "";
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
                                                        RoomId = availabilityResponse.AvailabilityRS.HotelResult[0].HotelOption[i].HotelRooms[j][kk].RoomId;

                                                        processedRoomTypeID = RoomId; //getMappedRoom_rlm(RoomName.Split('|')[0].Trim(), GTAResponse.Tables[1], Convert.ToInt32(Item.SearchPara.Filters.HotelIDs));

                                                        if (processedRoomTypeID == "")  // needtochange
                                                        {
                                                            isroom = false;
                                                            continue;
                                                        }

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

                                                    //string ChainCode_Agoda = Convert.ToString(DTChainCode.Tables[0].Rows[chaincode]["chaincode"]);// Convert.ToString(chaincode);

                                                    string ChainCode_Agoda = Convert.ToString(DTChainCode.Tables[0].Rows[chaincode]["ChannelCode"]);
                                                    //bool supplierExists = DTChainCode.Tables[1].AsEnumerable().Any(r => Convert.ToString(r["Supplierid"])?.Trim() == SupplierID.Trim());

                                                    if (true)
                                                    {
                                                        bool exists = DTChainCode.Tables[1].AsEnumerable().Any(r => Convert.ToString(r["Supplierid"])?.Trim() == SupplierID.Trim() && Convert.ToString(r["chaincode"])?.Trim() == ChainCode_Agoda);
                                                        if (!exists)

                                                        {

                                                            continue;

                                                        }
                                                    }


                                                    #region countrycityrestrication

                                                    // int CountryId = Convert.ToInt32(GTAResponse.Tables[0].Rows[0]["CountryId"]);
                                                    // int CityId = Convert.ToInt32(GTAResponse.Tables[0].Rows[0]["CityId"]);
                                                    // int chainid = Convert.ToInt32(GTAResponse.Tables[0].Rows[0]["HotelChainId"]);

                                                    // DataRow[] drsChinn = DTCountryblock.Tables[0].Select($"chainid = {chainid} AND channalcode = '{ChainCode_Agoda}'");
                                                    // if (drsChinn.GetLength(0) > 0)
                                                    // {
                                                    //     continue;
                                                    // }
                                                    //// DataRow[] drCountry = DTCountryblock.Tables[1].Select($"CountryId = {CountryId} AND CityId = 0");
                                                    // DataRow[] drCountry = DTCountryblock.Tables[1].Select($"CountryId = {CountryId} AND CityId = '{CityId}' AND channalcode = '{ChainCode_Agoda}'");
                                                    // if (drCountry.GetLength(0) > 0)
                                                    // {
                                                    //     continue;
                                                    // }
                                                    // DataRow[] drCity = DTCountryblock.Tables[1].Select($"CountryId = {CountryId} AND CityId = '{CityId}'");
                                                    // if (drCity.GetLength(0) > 0)
                                                    // {
                                                    //     continue;
                                                    // }

                                                    #region countrycityrestrication

                                                    int CountryId = Convert.ToInt32(GTAResponse.Tables[0].Rows[0]["CountryId"]);

                                                    int CityId = Convert.ToInt32(GTAResponse.Tables[0].Rows[0]["CityId"]);

                                                    int chainid = Convert.ToInt32(GTAResponse.Tables[0].Rows[0]["HotelChainId"]);

                                                    DataRow[] drsChinn = DTCountryblock.Tables[0].Select($"chainid = {chainid} AND channalcode = '{ChainCode_Agoda}'");

                                                    if (drsChinn.GetLength(0) > 0)

                                                    {

                                                        continue;

                                                    }

                                                    // DataRow[] drCountry = DTCountryblock.Tables[1].Select($"CountryId = {CountryId} AND CityId = 0");

                                                    int Cids = 0;

                                                    //DataRow[] drCountry = DTCountryblock.Tables[1].Select($"CountryId = {CountryId} AND CityId = '{CityId}' AND channalcode = '{ChainCode_Agoda}'");

                                                    DataRow[] drCountry = DTCountryblock.Tables[1].Select($"CountryId = {CountryId} AND CityId = '{Cids}' AND channalcode = '{ChainCode_Agoda}'");

                                                    if (drCountry.GetLength(0) > 0)

                                                    {

                                                        continue;

                                                    }

                                                    //DataRow[] drCity = DTCountryblock.Tables[1].Select($"CountryId = {CountryId} AND CityId = '{CityId}'");

                                                    DataRow[] drCity = DTCountryblock.Tables[1].Select($"CountryId = {CountryId} AND CityId = '{CityId}' AND channalcode = '{ChainCode_Agoda}'");

                                                    if (drCity.GetLength(0) > 0)

                                                    {

                                                        continue;

                                                    }

                                                    #endregion

                                                    #endregion

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
                                                    drhotelRoom["RoomTypeId"] = processedRoomTypeID;// "123";// gimmonixDetail.GimmonixRoomId; // Convert.ToString(drRoomCategories[rc]["RoomTypeId"]); 
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
                                                    "Rateaura" + "^^" + ChainCode_Agoda + "^^" + AgentPercentage + "^^" + requestName + "^^" + processedRoomTypeID; 

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
                                                    drHotelCancellation["RoomTypeId"] = processedRoomTypeID;// "123";// gimmonixDetail.GimmonixRoomId;//Convert.ToString(drRoomCategories[rc]["RoomTypeId"]);
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
                                                    drHotelRoomRateBreackUp["RoomTypeId"] = processedRoomTypeID; // "123";// gimmonixDetail.GimmonixRoomId;
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
                                _logger.LogError(ex , "XMLOut_HotelSearchWithRooms");

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
                        else
                        {
                            Item.ResponseType = "NoCount";
                            Item.ResponseStatus = 404;
                            try
                            {
                                Item.SupplierResponseError = Item.RQRS[0].Response;
                            }
                            catch (Exception ex)
                            {
                                _exception.Error(ex);

                            }
                        }
                    }
                    else
                    {
                        Item.ResponseType = "NoCount";
                        Item.ResponseStatus = 404;
                        try
                        {
                            Item.SupplierResponseError = Item.RQRS[0].Response;
                        }
                        catch (Exception ex)
                        {
                            _exception.Error(ex);

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
                    #region ResProcessTimer
                    try
                    {
                        timerroomx.Stop();
                        Item.CodeProcessingTimeSupplier = timerroomx.ElapsedMilliseconds;
                    }
                    catch (Exception ex)
                    {
                        await _exception.Error(ex);
                    }
                    #endregion
                }
            }
            else
            {
                Item.ResponseType = "NoCount";
                Item.ResponseStatus = 204;
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

        public async Task<List<Options>> XMLOut_HotelPreCheck(HotelX Item, CommonSearch commonSearch, DataSet GTAResponse, string HotelID, HotelSearchParameter SearchParam, DataTable dtAgentDetail, DataTable dtMarkupDetails, AdvancedOptions advancedOptions, string ratePlanCode)
        {
            System.Diagnostics.Stopwatch timerroomx = new System.Diagnostics.Stopwatch();
            timerroomx.Start();

            List<Options> options = new List<Options>();
            if (GTAResponse != null && GTAResponse.Tables.Count > 0 && GTAResponse.Tables[0].Rows.Count > 0)
            {
                string giitaCode = "";
                double LowestRate = 0;
                string RoomId = "";
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
                        //giitaCode = Convert.ToString(GTAResponse.Tables[0].Rows[0]["GiataId"]);
                        giitaCode = Convert.ToString(GTAResponse.Tables[0].Rows[0]["Id"]);

                        //recheck

                        CommoXMLOutReCheckResponse.Root_agoda recheckResponse = JsonConvert.DeserializeObject<CommoXMLOutReCheckResponse.Root_agoda>(Item.RQRS[0].Response);
                        var RecheckResponse = JsonConvert.DeserializeObject<CommoXMLOutReCheckResponse.Root_agoda>(Item.RQRS[0].Response);

                        // string RecheckResponse = JsonConvert.DeserializeObject<CommoXMLOutReCheckResponse.Root.ReCheckR>(Item.RQRS[0].Response);


                        //recheck end



                        CommoXMLOutPreCheckResponse.PreCheckResponse preCheckResponse = new CommoXMLOutPreCheckResponse.PreCheckResponse();

                        //AvailabilityResponse availabilityResponse = new AvailabilityResponse();
                        //preCheckResponse = JsonConvert.DeserializeObject<CommoXMLOutPreCheckResponse.PreCheckResponse>(Item.RQRS[0].Response);
                        preCheckResponse = JsonConvert.DeserializeObject<CommoXMLOutPreCheckResponse.PreCheckResponse>(Item.RQRS[0].Request);

                        string requestName = "precheck";
                        string preCheckRequestafterrecheck = await GetSupplierResponseFinalNew_new(Item, requestName);
                        preCheckResponse = JsonConvert.DeserializeObject<CommoXMLOutPreCheckResponse.PreCheckResponse>(preCheckRequestafterrecheck);

                        

                        if (preCheckResponse.PreBookRS != null)
                        {
                            string HotelOptionIdRP = string.Empty;
                            string SearchKeyRP = string.Empty;

                            string Status = preCheckResponse.PreBookRS.HotelOption.Status;
                            if (Status.ToLower() == "bookable")
                            {
                                if (preCheckResponse.PreBookRS.HotelOption != null)
                                {
                                    DataRow drXmlHotel = DtHotel.NewRow();
                                    DataTable DtHotelRoom = _commonMethod.GenerateRoomtypeDataTableNewV2();
                                    DataTable dtCancellation = _commonMethod.GenerateCancellationDataTableNewV2();
                                    DataTable DtHotelRoomRateBreackUp = _commonMethod.GenerateoomRateBreackUpDataTableNewV2();
                                    try
                                    {
                                        getrateplan.Root rateplan = new getrateplan.Root();
                                        rateplan = JsonConvert.DeserializeObject<getrateplan.Root>(ratePlanCode);

                                        ratePlanCode = rateplan.token;
                                        byte[] decodedBytes = Convert.FromBase64String(ratePlanCode);
                                        // Convert the byte array to a readable string (assuming it's UTF8 encoded)
                                        string decoded_ratePlanCode = Encoding.UTF8.GetString(decodedBytes);
                                        string RoomRatePlanCode = decoded_ratePlanCode;
                                        string[] spitid1 = null;
                                        string[] spitid2 = null;
                                        string HotelOptionId = "";
                                        string SearchKey = "";
                                        string RoomToken = "";
                                        string RoomNo = "";
                                        if (!string.IsNullOrEmpty(ratePlanCode))
                                        {
                                            spitid1 = RoomRatePlanCode.Split(new String[] { "^^" }, StringSplitOptions.None);
                                            spitid2 = spitid1[5].Split(new String[] { "$$$" }, StringSplitOptions.None);

                                            HotelOptionId = spitid2[0];
                                            SearchKey = spitid2[1];
                                            RoomToken = spitid2[2];
                                            RoomNo = "1";
                                            RoomId = spitid1[1];
                                        }
                                        string SupplierID = spitid1[6];
                                        int MasterSupplierID = 0;
                                        MasterSupplierID = Convert.ToInt32(spitid1[6]);
                                        SearchKeyRP = spitid2[1];
                                        HotelOptionIdRP = spitid2[0];

                                        //var lowestPriceRoom = GetLowestPriceRoom(availabilityResponse);
                                        //LowestRate = lowestPriceRoom.Price;
                                        LowestRate = preCheckResponse.PreBookRS.HotelOption.TotalPrice;

                                        drXmlHotel["GIITACode"] = giitaCode;
                                        drXmlHotel["OwnsystemHotelId"] = Item.SearchPara.Filters.HotelIDs;
                                        drXmlHotel["RaynaId"] = Item.SearchPara.Filters.HotelIDs;
                                        drXmlHotel["SupplierCurrency"] = preCheckResponse.PreBookRS.Currency;
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

                                        //ROOM
                                        string RoomUniqueId = string.Empty;
                                        if (preCheckResponse.PreBookRS.HotelOption.HotelRooms != null && preCheckResponse.PreBookRS.HotelOption.HotelRooms.Count > 0)
                                        {
                                            int k = 0;
                                            string RoomTokenRP = string.Empty;
                                            //foreach (var rm in preCheckResponse.PreBookRS.HotelOption.HotelRooms)
                                            {
                                                //RoomUniqueId += rm.RoomNo + "_" + rm.UniqueId + ",";
                                                //GimmonixDetail gimmonixDetail = new GimmonixDetail();
                                                //if (GTAResponse.Tables.Count > 1 && GTAResponse.Tables[1] != null && GTAResponse.Tables[1].Rows.Count > 0)
                                                //{
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

                                                RoomTokenRP = RoomToken;
                                                RoomTokenRP = "";
                                                int daysBeforeCheckIn = 365;
                                                double totalprice = 0;
                                                string RoomName = "";
                                                string MealName = "Ro";
                                                double price = 0;
                                                string processedRoomTypeID = "";
                                                bool isroom = true;
                                                int ChainCode_Agoda = Convert.ToInt32(spitid1[9]);
                                                double ChainMarkup = Convert.ToDouble(spitid1[10]);
                                                //TimeSpan totaldays;  // 01062026

                                                //foreach (var rm in availabilityResponse.AvailabilityRS.HotelResult[0].HotelOption[i].HotelRooms[j])
                                                {
                                                    for (int kk = 0; kk < preCheckResponse.PreBookRS.HotelOption.HotelRooms.Count; kk++)
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
                                                        //if (availabilityResponse.AvailabilityRS.HotelResult[0].HotelOption[i].HotelRooms[j][0].RoomTypeName == availabilityResponse.AvailabilityRS.HotelResult[0].HotelOption[i].HotelRooms[j][kk].RoomTypeName && availabilityResponse.AvailabilityRS.HotelResult[0].HotelOption[i].HotelRooms[j][0].MealName == availabilityResponse.AvailabilityRS.HotelResult[0].HotelOption[i].HotelRooms[j][kk].MealName)
                                                        {
                                                            double PRprice = preCheckResponse.PreBookRS.HotelOption.TotalPrice;
                                                            RoomUniqueId += preCheckResponse.PreBookRS.HotelOption.HotelRooms[kk].RoomNo + "_" + preCheckResponse.PreBookRS.HotelOption.HotelRooms[kk].UniqueId + ",";
                                                            double totalprice1 = preCheckResponse.PreBookRS.HotelOption.HotelRooms[kk].Price;
                                                            RoomName = preCheckResponse.PreBookRS.HotelOption.HotelRooms[kk].RoomTypeName;

                                                            processedRoomTypeID = RoomId; // getMappedRoom_rlm(RoomName.Split('|')[0].Trim(), GTAResponse.Tables[1], Convert.ToInt32(Item.SearchPara.Filters.HotelIDs)); //needtochange

                                                            if (processedRoomTypeID == "")
                                                            {
                                                                isroom = false;
                                                                continue;
                                                            }

                                                            MealName = preCheckResponse.PreBookRS.HotelOption.HotelRooms[kk].MappedMealName;
                                                            //totalprice = totalprice + totalprice1;
                                                            daysBeforeCheckIn = Convert.ToInt32(spitid1[7]);

                                                            int UniqueId = preCheckResponse.PreBookRS.HotelOption.HotelRooms[kk].UniqueId;

                                                            //needtochange
                                                            //try
                                                            //{
                                                            //    // DLConnection dBConnection1 = new DLConnection();
                                                            //    SqlParameter[] para1 = new SqlParameter[]{
                                                            //new SqlParameter("@totalprice1",Math.Round(totalprice1,2)),
                                                            //new SqlParameter("@PRprice",Math.Round(PRprice,2)),
                                                            //new SqlParameter("@UniqueId",UniqueId),
                                                            //new SqlParameter("@ChainMarkup",ChainMarkup)
                                                            //};
                                                            //    //DataTable DsPrice = dBConnection1.GetDataTable("AgodaUpdatePrice_channalWise", para1);
                                                            //    DataTable DsPrice = _dlConnection.GetDataTable("AgodaUpdatePrice_channalWise", para1);
                                                            //    price += Convert.ToDouble(DsPrice.Rows[0]["AGprice"]);

                                                            //}
                                                            //catch (System.Exception ex)
                                                            //{
                                                            //    // Common.Error(ex);
                                                            //    _exception.Error(ex);

                                                            //}
                                                            // ===============
                                                        }
                                                    }
                                                    if (isroom == false)
                                                    {
                                                        return options;
                                                    }
                                                    DataRow drhotelRoom = DtHotelRoom.NewRow();

                                                    drhotelRoom["SupplierName"] = "xmlout";
                                                    drhotelRoom["OwnsystemHotelId"] = Item.SearchPara.Filters.HotelIDs;
                                                    drhotelRoom["ContractID"] = 0;
                                                    drhotelRoom["UniqueNo"] = RoomToken;
                                                    drhotelRoom["BookingStatus"] = preCheckResponse.PreBookRS.HotelOption.Status;
                                                    drhotelRoom["RoomNo"] = "1";
                                                    //drhotelRoom["PromotionId"] = 0;
                                                    //drhotelRoom["HSupplierId"] = "";
                                                    //drhotelRoom["HRateTypeId"] = "";
                                                    drhotelRoom["RoomTypeName"] = RoomName;// gimmonixDetail.GimmonixRoomName;
                                                    drhotelRoom["OfferCode"] = "";
                                                    drhotelRoom["MealName"] = MealName;// rm.MealName;
                                                    drhotelRoom["RoomTypeId"] = processedRoomTypeID; // "123";// gimmonixDetail.GimmonixRoomId; drhotelRoom["BoardTypeID"] = RoomToken;
                                                    drhotelRoom["MealId"] = MealName;
                                                    drhotelRoom["OfferDescription"] = "";
                                                    //drhotelRoom["SupplierCurrency"] = preCheckResponse.PreBookRS.Currency;
                                                    drhotelRoom["SupplierCurrency"] = "USD";  //needdtochange
                                                    drhotelRoom["SHRUI"] = "";
                                                    drhotelRoom["CityId"] = GTAResponse.Tables[0].Rows[0]["CityId"];
                                                    drhotelRoom["IsCombineRoom"] = true;
                                                    //drhotelRoom["FinalSellingPrice"] = rm.Price;   //Single Room
                                                    //drhotelRoom["FinalSellingPrice"] = price;// preCheckResponse.PreBookRS.HotelOption.TotalPrice; // Combine Room //needtochange
                                                    drhotelRoom["FinalSellingPrice"] = preCheckResponse.PreBookRS.HotelOption.TotalPrice; // Combine Room
                                                    drhotelRoom["allocationDetails"] = SearchParam.Rooms.Count.ToString();
                                                    //if (ExtraPerson != string.Empty)
                                                    // drhotelRoom["EssentialInformation"] = ExtraPerson;

                                                    string RatePlanCode = ratePlanCode;
                                                    //advancedOptions.searchKeyAgoda + "^^" +
                                                    //drhotelRoom["RoomTypeId"] + "^^" +
                                                    //Item.SearchPara.Filters.HotelIDs + "^^" +
                                                    //drXmlHotel["MealName"] + "^^" +
                                                    ////drhotelRoom["BoardTypeID"] + "^^" +
                                                    //drhotelRoom["SupplierCurrency"] + "^^" +
                                                    ////drhotelRoom["OTHPromoCode"] + "^^" +
                                                    //HotelOptionIdRP + "$$$" + SearchKeyRP + "$$$" + RoomTokenRP + "$$$" + rm.Price + "^^" +
                                                    //MasterSupplierID + "^^" +
                                                    //"Travelbullz";

                                                    drhotelRoom["channel"] = ChainCode_Agoda;
                                                    drhotelRoom["RoomTypeCode"] = RatePlanCode;
                                                    DtHotelRoom.Rows.Add(drhotelRoom);

                                                    DataRow drHotelCancellation = dtCancellation.NewRow();
                                                    drHotelCancellation["SupplierName"] = "xmlout";
                                                    drHotelCancellation["OwnsystemHotelId"] = Item.SearchPara.Filters.HotelIDs;
                                                    drHotelCancellation["RoomNo"] = 1; // Convert.ToString(drRoomCategories[rc]["RoomNo"]);
                                                                                       //drHotelCancellation["RoomTypeId"] = rm.RoomToken;//Convert.ToString(drRoomCategories[rc]["RoomTypeId"]);
                                                    drHotelCancellation["RoomTypeId"] = processedRoomTypeID;// "123";// gimmonixDetail.GimmonixRoomId;//Convert.ToString(drRoomCategories[rc]["RoomTypeId"]);
                                                    drHotelCancellation["BoardTypeID"] = RoomToken;
                                                    drHotelCancellation["RoomTypeCode"] = RatePlanCode;
                                                    //drHotelCancellation["RoomTypeName"] = rm.RoomTypeName;//RoomDesc;
                                                    drHotelCancellation["RoomTypeName"] = RoomName;// "Stander";// gimmonixDetail.GimmonixRoomName;
                                                    drHotelCancellation["MealId"] = MealName;//Convert.ToString(drRoomCategories[rc]["BoardBasis"]);
                                                    if (k == 0)
                                                    {
                                                        k++;
                                                        if (Convert.ToInt32(spitid1[7]) > 0)
                                                        {

                                                            #region recheck

                                                            TimeSpan totaldays = TimeSpan.Zero;  // 01062026


                                                            foreach (var cp in RecheckResponse.ReCheckRS[0].CancellationPolicy)
                                                            {
                                                                //foreach (var cp in rm.CancellationPolicy)

                                                                if (cp.CancellationPrice == 0)
                                                                {
                                                                    drHotelCancellation["FromDate"] = cp.FromDate;
                                                                    drHotelCancellation["ToDate"] = cp.ToDate;

                                                                    DateTime from = DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null);
                                                                    DateTime to = Convert.ToDateTime(cp.ToDate);
                                                                    //TimeSpan totaldays = Convert.ToDateTime(from) - Convert.ToDateTime(to);
                                                                    totaldays = Convert.ToDateTime(from) - Convert.ToDateTime(to);
                                                                    drHotelCancellation["daysBeforeCheckIn"] = Convert.ToString(Convert.ToInt32(totaldays.Days));
                                                                    break;


                                                                }
                                                                else
                                                                {

                                                                    DateTime from = DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null);
                                                                    DateTime to = Convert.ToDateTime(DateTime.Now.Date);
                                                                    //TimeSpan totaldays = Convert.ToDateTime(from) - Convert.ToDateTime(to);
                                                                    totaldays = Convert.ToDateTime(from) - Convert.ToDateTime(to);
                                                                    drHotelCancellation["daysBeforeCheckIn"] = Convert.ToString(Convert.ToInt32(totaldays.Days));

                                                                    drHotelCancellation["CompanyCancellationPrice"] = cp.CancellationPrice;
                                                                    drHotelCancellation["AgentCancellationPrice"] = cp.CancellationPrice;
                                                                }
                                                            }

                                                            #endregion




                                                            //drHotelCancellation["daysBeforeCheckIn"] = Convert.ToString(Convert.ToInt32(spitid1[7]));  //01062026
                                                            drHotelCancellation["daysBeforeCheckIn"] = totaldays.Days;   //01062026
                                                            drHotelCancellation["CompanyCancellationPrice"] = spitid2[3];
                                                            drHotelCancellation["AgentCancellationPrice"] = spitid2[3];
                                                            drHotelCancellation["SupplierCurrency"] = preCheckResponse.PreBookRS.Currency;
                                                            //drHotelCancellation["BuyingPrice"] = rm.Price; // Single

                                                            // ================
                                                            // drHotelCancellation["BuyingPrice"] = price;// preCheckResponse.PreBookRS.HotelOption.TotalPrice; //Combine  //needtochange
                                                            //drHotelCancellation["isNRF"] = Convert.ToBoolean(IsNonRef);


                                                            drHotelCancellation["BuyingPrice"] = preCheckResponse.PreBookRS.HotelOption.TotalPrice; //Combine;
                                                            // ================


                                                            dtCancellation.Rows.Add(drHotelCancellation);
                                                        }
                                                        else
                                                        {
                                                            dtCancellation.Rows.Add(drHotelCancellation);
                                                        }
                                                    }
                                                    DataRow drHotelRoomRateBreackUp = DtHotelRoomRateBreackUp.NewRow();
                                                    drHotelRoomRateBreackUp["RoomNo"] = 1;
                                                    drHotelRoomRateBreackUp["SupplierName"] = "xmlout";
                                                    drHotelRoomRateBreackUp["OwnsystemHotelId"] = Item.SearchPara.Filters.HotelIDs;
                                                    drHotelRoomRateBreackUp["ContractID"] = 0;
                                                    //drHotelRoomRateBreackUp["RoomTypeName"] = rm.RoomTypeName;
                                                    drHotelRoomRateBreackUp["RoomTypeName"] = "Stander";// gimmonixDetail.GimmonixRoomName;
                                                                                                        //drHotelRoomRateBreackUp["RoomTypeId"] = rm.RoomToken;
                                                    drHotelRoomRateBreackUp["RoomTypeId"] = processedRoomTypeID;// "123";// gimmonixDetail.GimmonixRoomId;
                                                    drHotelRoomRateBreackUp["RoomTypeCode"] = RatePlanCode;//RateplancodeSiteminder;
                                                    drHotelRoomRateBreackUp["BoardTypeID"] = RoomToken;
                                                    drHotelRoomRateBreackUp["MealId"] = MealName; // Convert.ToString(drRoomCategories[rc]["BoardBasis"]);
                                                    drHotelRoomRateBreackUp["Meal"] = MealName;// rm.MealName; //Convert.ToString(drRoomCategories[rc]["BoardBasis"]);
                                                    drHotelRoomRateBreackUp["BookingStatus"] = preCheckResponse.PreBookRS.HotelOption.Status;//Convert.ToString(drRoomCategories[rc]["BookingStatus"]);
                                                                                                                                             //drHotelRoomRateBreackUp["DateOfWeek"] = WeekColumn;
                                                                                                                                             //drHotelRoomRateBreackUp["Date"] = datetime;
                                                                                                                                             //drHotelRoomRateBreackUp["Rate"] = rm.Price; // Single
                                                    drHotelRoomRateBreackUp["Rate"] = preCheckResponse.PreBookRS.HotelOption.TotalPrice; // Combine
                                                                                                                                         //drHotelRoomRateBreackUp["RateType"] = Convert.ToString(DTdSurchargerdate[MP]["SurchargeName"]);
                                                                                                                                         //drHotelRoomRateBreackUp["OrderNo"] = cntno;                                        
                                                    drHotelRoomRateBreackUp["SupplierCurrency"] = preCheckResponse.PreBookRS.Currency;
                                                    //drHotelRoomRateBreackUp["CompanyRate"] = Math.Round(Convert.ToDouble(DTdSurchargerdate[MP]["Price"]), 2);
                                                    //drHotelRoomRateBreackUp["NewRateType"] = Convert.ToString(DTdrdate[0]["RateType"]);
                                                    //drHotelRoomRateBreackUp["RateTypeId"] = Convert.ToString(DTdrdate[0]["RateTypeId"]);
                                                    //drHotelRoomRateBreackUp["SupplierId"] = Convert.ToString(DTdrdate[0]["SupplierId"]);
                                                    // drHotelRoomRateBreackUpSurcharge["RatePlanCode"] = RateplancodeSiteminder;                                        
                                                    //drHotelRoomRateBreackUp["BuyingPrice"] = rm.Price; //Single
                                                    drHotelRoomRateBreackUp["BuyingPrice"] = preCheckResponse.PreBookRS.HotelOption.TotalPrice; //Combine

                                                    DtHotelRoomRateBreackUp.Rows.Add(drHotelRoomRateBreackUp);
                                                }
                                            }

                                            try
                                            {
                                                //DLConnection dBConnection = new DLConnection();

                                                SqlParameter[] para = new SqlParameter[]{
                                          new SqlParameter("@Action","INSERT"),
                                          new SqlParameter("@SupplierID",SupplierID),
                                          new SqlParameter("@RatePlanCode",ratePlanCode),
                                          new SqlParameter("@BookingToken",preCheckResponse.PreBookRS.HotelOption.BookingToken),
                                          new SqlParameter("@UniqueId",RoomUniqueId)
                                        };

                                                //int p = dBConnection.ExecuteNonQuery("GetAgodaRoomDetailXMLOut", para);
                                                int p = await _dlConnection.ExecuteNonQuery("GetAgodaRoomDetailXMLOut", para);

                                            }
                                            catch (System.Exception ex)
                                            {
                                                _exception.Error(ex);

                                            }


                                            //Options ops = (HotelXML_Suppliers.GetCommonRoomForSuppliers(Convert.ToInt64(giitaCode), commonSearch.tokenData, commonSearch.advancedOptions, LowestRate, drXmlHotel, DtHotelRoom, dtCancellation, DtHotelRoomRateBreackUp, SearchParam));
                                            Options ops = (await _supplierRes.GetCommonRoomForSuppliers_agoda(Convert.ToInt64(giitaCode), commonSearch.tokenData, commonSearch.advancedOptions, LowestRate, drXmlHotel, DtHotelRoom, dtCancellation, DtHotelRoomRateBreackUp, SearchParam)); //SearchKeyRP
                                            if (ops != null)
                                                options.Add(ops);
                                        }
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
                                }
                                //Options_agoda ops = (HotelXML_Suppliers.GetCommonRoomForSuppliers(Convert.ToInt64(giitaCode), commonSearch.tokenData, commonSearch.advancedOptions, LowestRate, drXmlHotel, DtHotelRoom, dtCancellation, DtHotelRoomRateBreackUp, SearchParam));
                                //if (ops != null)
                                //    options.Add(ops);
                            }
                            else
                            {
                                Item.ResponseType = "NoCount";
                                Item.ResponseStatus = 404;
                                try
                                {
                                    Item.SupplierResponseError = Item.RQRS[0].Response;
                                }
                                catch (Exception ex)
                                {
                                    _exception.Error(ex);

                                }
                            }
                        }
                        else
                        {
                            Item.ResponseType = "NoCount";
                            Item.ResponseStatus = 404;
                            try
                            {
                                Item.SupplierResponseError = Item.RQRS[0].Response;
                            }
                            catch (Exception ex)
                            {
                                _exception.Error(ex);

                            }
                        }
                    }
                    else
                    {
                        Item.ResponseType = "NoCount";
                        Item.ResponseStatus = 404;
                        try
                        {
                            Item.SupplierResponseError = Item.RQRS[0].Response;
                        }
                        catch (Exception ex)
                        {
                            _exception.Error(ex);

                        }
                    }
                    return options;
                }
                catch (Exception ex)
                {
                    _exception.Error(ex);
                    return options;
                }
                finally
                {
                    //GC.Collect();
                    #region ResProcessTimer
                    try
                    {
                        timerroomx.Stop();
                        Item.CodeProcessingTimeSupplier = timerroomx.ElapsedMilliseconds;
                    }
                    catch (Exception ex)
                    {
                        await _exception.Error(ex);
                    }
                    #endregion
                }
            }
            else
            {
                Item.ResponseType = "NoCount";
                Item.ResponseStatus = 204;

            }
            return options;
        }



        #region comment
        //public async Task<List<Options>> XMLOut_HotelPreCheck(HotelX Item, CommonSearch commonSearch, DataSet GTAResponse, string HotelID, HotelSearchParameter SearchParam, DataTable dtAgentDetail, DataTable dtMarkupDetails, AdvancedOptions advancedOptions, string ratePlanCode)
        //{
        //    System.Diagnostics.Stopwatch timerroomx = new System.Diagnostics.Stopwatch();
        //    timerroomx.Start();

        //    List<Options> options = new List<Options>();
        //    if (GTAResponse != null && GTAResponse.Tables.Count > 0 && GTAResponse.Tables[0].Rows.Count > 0)
        //    {
        //        string giitaCode = "";
        //        double LowestRate = 0;
        //        string RoomId = "";
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
        //                                    RoomId = spitid1[1];    
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

        //                                                    processedRoomTypeID = RoomId; // getMappedRoom_rlm(RoomName.Split('|')[0].Trim(), GTAResponse.Tables[1], Convert.ToInt32(Item.SearchPara.Filters.HotelIDs)); //needtochange

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
        //                                                    //    // DLConnection dBConnection1 = new DLConnection();
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
        //                                                    //    // Common.Error(ex);
        //                                                    //    _exception.Error(ex);

        //                                                    //}
        //                                                    // ===============
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
        //                                            //drhotelRoom["SupplierCurrency"] = preCheckResponse.PreBookRS.Currency;
        //                                            drhotelRoom["SupplierCurrency"] = "USD";  //needdtochange
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
        //                                                    // drHotelCancellation["BuyingPrice"] = price;// preCheckResponse.PreBookRS.HotelOption.TotalPrice; //Combine  //needtochange
        //                                                    //drHotelCancellation["isNRF"] = Convert.ToBoolean(IsNonRef);


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
        //                                    Options ops = (await _supplierRes.GetCommonRoomForSuppliers_agoda(Convert.ToInt64(giitaCode), commonSearch.tokenData, commonSearch.advancedOptions, LowestRate, drXmlHotel, DtHotelRoom, dtCancellation, DtHotelRoomRateBreackUp, SearchParam)); //SearchKeyRP
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
        //                    else
        //                    {
        //                        Item.ResponseType = "NoCount";
        //                        Item.ResponseStatus = 404;
        //                        try
        //                        {
        //                            Item.SupplierResponseError = Item.RQRS[0].Response;
        //                        }
        //                        catch (Exception ex)
        //                        {
        //                            _exception.Error(ex);

        //                        }
        //                    }
        //                }
        //                else
        //                {
        //                    Item.ResponseType = "NoCount";
        //                    Item.ResponseStatus = 404;
        //                    try
        //                    {
        //                        Item.SupplierResponseError = Item.RQRS[0].Response;
        //                    }
        //                    catch (Exception ex)
        //                    {
        //                        _exception.Error(ex);

        //                    }
        //                }
        //            }
        //            else
        //            {
        //                Item.ResponseType = "NoCount";
        //                Item.ResponseStatus = 404;
        //                try
        //                {
        //                    Item.SupplierResponseError = Item.RQRS[0].Response;
        //                }
        //                catch (Exception ex)
        //                {
        //                    _exception.Error(ex);

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
        //            #region ResProcessTimer
        //            try
        //            {
        //                timerroomx.Stop();
        //                Item.CodeProcessingTimeSupplier = timerroomx.ElapsedMilliseconds;
        //            }
        //            catch (Exception ex)
        //            {
        //                await _exception.Error(ex);
        //            }
        //            #endregion
        //        }
        //    }
        //    else
        //    {
        //        Item.ResponseType = "NoCount";
        //        Item.ResponseStatus = 204;

        //    }
        //    return options;
        //}
        #endregion
        public AgodaBookingRS XMLOutBookingProcess(AgodaBookingRQ agodaRQ, TokenData tokenData, Stopwatch Timer, string rateplanstring)
        {


            AgodaBookingRS ObjRespond = new AgodaBookingRS();
            try
            {
                DataTable XMLOutBookData = null;
                string[] spitid1 = null;
                string[] spitid2 = null;

                getrateplan.Root rateplan = new getrateplan.Root();
                rateplan = JsonConvert.DeserializeObject<getrateplan.Root>(agodaRQ.metadata);
                string rateplanstring1 = rateplan.token;
                string RoomUniqueId = "";
                string BookingToken = "";


                spitid1 = rateplanstring.Split(new String[] { "^^" }, StringSplitOptions.None);
                spitid2 = spitid1[5].Split(new String[] { "$$$" }, StringSplitOptions.None);

                string RequetType = "";

                try
                {
                    RequetType = Convert.ToString(spitid1[11]);
                }
                catch (Exception)
                {
                    RequetType = "hotel";
                }

                if (RequetType.ToLower() == "precheck")
                {
                    byte[] decodedBytes = Convert.FromBase64String(rateplanstring1);
                    string decoded_ratePlanCode = Encoding.UTF8.GetString(decodedBytes);

                    string RoomRatePlanCode = decoded_ratePlanCode;
                    //string[] spitid1 = RoomRatePlanCode.Split(new String[] { "^^" }, StringSplitOptions.None);
                    //string[] spitid2 = spitid1[5].Split(new String[] { "$$$" }, StringSplitOptions.None);

                    string HotelOptionId = spitid2[0];
                    string SearchKey = spitid2[1];
                    string RoomToken = spitid2[2];
                    string RoomNo = "1";

                    CommoXMLOutPreCheckRequest.PreCheckRequest preCheckRequest = new CommoXMLOutPreCheckRequest.PreCheckRequest();
                    //if (SearchPara.CityID != 0)
                    {
                        //preCheckRequest.Token = WebConfigurationManager.AppSettings["out_agoda_token"];
                        preCheckRequest.Token = _keySettings.out_agoda_token;

                        CommoXMLOutPreCheckRequest.Request request = new CommoXMLOutPreCheckRequest.Request();
                        request.SearchKey = SearchKey;
                        preCheckRequest.Request = request;

                        CommoXMLOutPreCheckRequest.HotelOption hotelOption = new CommoXMLOutPreCheckRequest.HotelOption();
                        hotelOption.HotelOptionId = HotelOptionId;


                        List<CommoXMLOutPreCheckRequest.HotelRoom> rooms = new List<CommoXMLOutPreCheckRequest.HotelRoom>();
                        string[] _RoomToken = RoomToken.Split(new String[] { "###" }, StringSplitOptions.None);

                        for (int i = 0; i < _RoomToken.Length - 1; i++)
                        {
                            CommoXMLOutPreCheckRequest.HotelRoom hotelRoom = new CommoXMLOutPreCheckRequest.HotelRoom();
                            hotelRoom.RoomNo = Convert.ToString(i + 1);// RoomNo;
                            hotelRoom.RoomToken = _RoomToken[i + 1];
                            rooms.Add(hotelRoom);
                        }
                        hotelOption.HotelRooms = rooms;

                        request.HotelOption = hotelOption;

                        CommoXMLOutPreCheckRequest.AdvancedOptions advancedOptions = new CommoXMLOutPreCheckRequest.AdvancedOptions();
                        advancedOptions.Currency = "USD";
                        advancedOptions.CustomerIpAddress = "roommapping_code";
                        advancedOptions.RefreshCache = false;
                        advancedOptions.SearchByOneID = true;
                        preCheckRequest.AdvancedOptions = advancedOptions;

                        preCheckRequest.IsMobile = 1;
                    }

                    HotelX xhotel = new HotelX();
                    xhotel.Supplier = "xmlout";
                    xhotel.DebugeMode = true;
                    xhotel.SearchTimeOut = 30000;// Convert.ToInt32(WebConfigurationManager.AppSettings["TimeOutXMLOut"]);

                    List<RQRS> RQRS = new List<RQRS>();
                    RQRS objrq = new RQRS();
                    objrq.Request = JsonConvert.SerializeObject(preCheckRequest);
                    objrq.Response = "";
                    RQRS.Add(objrq);
                    xhotel.RQRS = RQRS;
                    // string Prebookresponse = Task.Run(() => GetSupplierResponseFinalNew(xhotel, "prebook")).GetAwaiter().GetResult();
                    string Prebookresponse = GetSupplierResponseFinalNew_new(xhotel, "prebook").GetAwaiter().GetResult();
                    CommoXMLOutPreCheckResponse.PreCheckResponse preCheckResponse = new CommoXMLOutPreCheckResponse.PreCheckResponse();

                    //AvailabilityResponse availabilityResponse = new AvailabilityResponse();
                    preCheckResponse = JsonConvert.DeserializeObject<CommoXMLOutPreCheckResponse.PreCheckResponse>(Prebookresponse);

                    if (preCheckResponse.PreBookRS != null)
                    {
                        string HotelOptionIdRP = string.Empty;
                        string SearchKeyRP = string.Empty;

                        string Status = preCheckResponse.PreBookRS.HotelOption.Status;
                        if (Status.ToLower() == "bookable")
                        {
                            if (preCheckResponse.PreBookRS.HotelOption.HotelRooms != null && preCheckResponse.PreBookRS.HotelOption.HotelRooms.Count > 0)
                            {
                                RoomUniqueId = "";
                                //for (int kk = 0; kk < preCheckResponse.PreBookRS.HotelOption.HotelRooms.Count; kk++)
                                //{
                                //    RoomUniqueId += preCheckResponse.PreBookRS.HotelOption.HotelRooms[kk].RoomNo + "_" + preCheckResponse.PreBookRS.HotelOption.HotelRooms[kk].UniqueId + ",";
                                //}
                                BookingToken = preCheckResponse.PreBookRS.HotelOption.BookingToken;

                                try
                                {
                                    double price = 0;
                                    for (int kk = 0; kk < preCheckResponse.PreBookRS.HotelOption.HotelRooms.Count; kk++)
                                    {
                                        double PRprice = preCheckResponse.PreBookRS.HotelOption.TotalPrice;
                                        RoomUniqueId += preCheckResponse.PreBookRS.HotelOption.HotelRooms[kk].RoomNo + "_" + preCheckResponse.PreBookRS.HotelOption.HotelRooms[kk].UniqueId + ",";
                                        double totalprice1 = preCheckResponse.PreBookRS.HotelOption.HotelRooms[kk].Price;
                                        int UniqueId = preCheckResponse.PreBookRS.HotelOption.HotelRooms[kk].UniqueId;
                                        //try
                                        //{
                                        //    double ChainMarkup = Convert.ToDouble(spitid1[10]);

                                        //    //DLConnection dBConnection1 = new DLConnection();
                                        //    SqlParameter[] para1 = new SqlParameter[]{
                                        //                            new SqlParameter("@totalprice1",Math.Round(totalprice1,2)),
                                        //                            new SqlParameter("@PRprice",Math.Round(PRprice,2)),
                                        //                            new SqlParameter("@UniqueId",UniqueId),
                                        //                            new SqlParameter("@ChainMarkup",ChainMarkup)
                                        //                            };
                                        //    //DataTable DsPrice = dBConnection1.GetDataTable("AgodaUpdatePrice_channalWise", para1);
                                        //    DataTable DsPrice = _dlConnection.GetDataTable("AgodaUpdatePrice_channalWise", para1);
                                        //    price += Convert.ToDouble(DsPrice.Rows[0]["AGprice"]);
                                        //}
                                        //catch (System.Exception ex)
                                        //{
                                        //    _exception.Error(ex);
                                        //}
                                    }
                                }
                                catch (System.Exception ex)
                                {
                                    _exception.Error(ex);
                                }
                            }
                        }
                    }
                }
                try
                {
                    if (RoomUniqueId == "" && BookingToken == "")
                    {

                        //DLConnection dBConnection = new DLConnection();

                        SqlParameter[] para = new SqlParameter[]{
                                                  new SqlParameter("@Action","SELECT"),
                                                  new SqlParameter("@SupplierID",spitid1[6]),
                                                  new SqlParameter("@RatePlanCode",rateplanstring1)
                                                };

                        //XMLOutBookData = dBConnection.GetDataTable("GetAgodaRoomDetailXMLOut", para);
                        XMLOutBookData = _dlConnection.GetDataTable("GetAgodaRoomDetailXMLOut", para);

                        if (XMLOutBookData != null && XMLOutBookData.Rows.Count > 0)
                        {
                            RoomUniqueId = Convert.ToString(XMLOutBookData.Rows[0]["UniqueId"]);
                            BookingToken = Convert.ToString(XMLOutBookData.Rows[0]["BookingToken"]);
                        }
                    }

                    if (RoomUniqueId != "" && BookingToken != "")
                    {
                        CommonXMLBookRequest.BookingRequest bookingRequest = new CommonXMLBookRequest.BookingRequest();
                        //bookingRequest.Token = WebConfigurationManager.AppSettings["out_agoda_token"];
                        //bookingRequest.Token = _keySettings.out_agoda_token;
                        bookingRequest.Token = _keySettings.out_agoda_token;
                        bookingRequest.Token = "";

                        //bookingRequest.Token = _keySettings?.out_agoda_token;
                        CommonXMLBookRequest.Request request = new CommonXMLBookRequest.Request();
                        request.SearchKey = spitid2[1];

                        CommonXMLBookRequest.BookRQ bookRQ = new CommonXMLBookRequest.BookRQ();
                        bookRQ.BookingToken = BookingToken;// Convert.ToString(XMLOutBookData.Rows[0]["BookingToken"]);
                        bookRQ.TotalPrice = agodaRQ.totalPrice;
                        bookRQ.InternalReference = agodaRQ.bookingReferenceId;

                        List<CommonXMLBookRequest.HotelRoom> hotelRooms = new List<CommonXMLBookRequest.HotelRoom>();

                        string[] UniqueIds1 = Convert.ToString(RoomUniqueId).Split(new String[] { "," }, StringSplitOptions.None);

                        string[] arraysearchpra = rateplanstring.Split(new String[] { "$$$" }, StringSplitOptions.None);
                        int noofroom = Convert.ToInt32(arraysearchpra[0]);
                        int noofadult = Convert.ToInt32(arraysearchpra[1]);
                        string noofchilds = arraysearchpra[2];
                        //string[] noofchildarray = noofchilds.Split(new String[] { "###" }, StringSplitOptions.None);
                        List<XMLOutRoomDetail> xMLOutRoomDetails = GetXMLOutRoomDetail(noofroom, noofadult, noofchilds);

                        if (xMLOutRoomDetails != null && xMLOutRoomDetails.Count > 0)
                        {
                            int r = 0;
                            foreach (var rm in xMLOutRoomDetails)
                            {
                                string[] UniqueIds2 = UniqueIds1[r].Split(new String[] { "_" }, StringSplitOptions.None);

                                for (int i = 0; i < rm.NoofPassanger; i++)
                                {
                                    CommonXMLBookRequest.HotelRoom hotelRoom = new CommonXMLBookRequest.HotelRoom();
                                    hotelRoom.UniqueId = Convert.ToInt32(UniqueIds2[1]);
                                    hotelRoom.RoomNo = rm.RoomNo;
                                    hotelRoom.IsLead = i == 0 ? 1 : 0;
                                    hotelRoom.PaxType = "Adult";
                                    hotelRoom.ChildAge = 0;
                                    hotelRoom.Prefix = "Mr";
                                    hotelRoom.FirstName = i == 0 ? agodaRQ.guest.firstName : "TBA";
                                    hotelRoom.LastName = i == 0 ? agodaRQ.guest.lastName : "TBA";

                                    hotelRooms.Add(hotelRoom);
                                }
                                if (rm.ChildAge != null && rm.ChildAge.Count > 0)
                                {
                                    for (int j = 0; j < rm.ChildAge.Count; j++)
                                    {
                                        if (rm.ChildAge[j] > 0)
                                        {
                                            CommonXMLBookRequest.HotelRoom childRoom = new CommonXMLBookRequest.HotelRoom();
                                            childRoom.UniqueId = Convert.ToInt32(UniqueIds2[1]);
                                            childRoom.RoomNo = rm.RoomNo;
                                            childRoom.IsLead = 0;
                                            childRoom.PaxType = "Child";
                                            childRoom.ChildAge = rm.ChildAge[j];
                                            childRoom.Prefix = "Mr";
                                            childRoom.FirstName = "Childf" + (j + 1 == 1 ? "one" : "two");
                                            childRoom.LastName = "Childl" + (j + 1 == 1 ? "one" : "two");

                                            hotelRooms.Add(childRoom);
                                        }
                                    }
                                }
                                r++;
                            }
                        }
                        CommonXMLBookRequest.AdvancedOptions advancedOptions = new CommonXMLBookRequest.AdvancedOptions();
                        advancedOptions.Currency = "USD";
                        advancedOptions.CustomerIpAddress = "::1";

                        bookRQ.HotelRooms = hotelRooms;
                        request.BookRQ = bookRQ;
                        bookingRequest.AdvancedOptions = advancedOptions;
                        bookingRequest.Request = request;

                        //Call XMLOut book request
                        HotelX item = new HotelX();
                        RQRS rQRS = new RQRS();
                        rQRS.Request = JsonConvert.SerializeObject(bookingRequest);
                        rQRS.Response = "";
                        item.SearchTimeOut = 120000;
                        List<RQRS> RS = new List<RQRS>();
                        RS.Add(rQRS);

                        item.RQRS = RS;

                        //string response = GetSupplierResponseFinalNew(item, "book").Result;
                        //string response = Task.Run(() => GetSupplierResponseFinalNew(item, "book")).GetAwaiter().GetResult();
                        string response = GetSupplierResponseFinalNew_new(item, "book").GetAwaiter().GetResult();

                        if (!string.IsNullOrEmpty(response))
                        {
                            //string cleanJson = response;

                            //if (response.StartsWith("\""))
                            //{
                            //    cleanJson = JsonConvert.DeserializeObject<string>(response);
                            //}

                            CommonXMLBookResponse commonXMLBookResponse = new CommonXMLBookResponse();
                            commonXMLBookResponse = JsonConvert.DeserializeObject<CommonXMLBookResponse>(response);

                            //var leadPax = bookRQ.HotelRooms.FirstOrDefault(x => x.IsLead == 1);
                            //var preRequest = CreatePreBookRequest( "", "",leadPax.Prefix, leadPax.FirstName,leadPax.LastName,leadPax.UniqueId,leadPax.PaxType,leadPax.IsLead);

                            //string preresponse = Task.Run(() => GetSupplierResponseFinalNew_new(item, "PriceCheck")).GetAwaiter().GetResult();
                            //string bookresponse = Task.Run(() => GetSupplierResponseFinalNew_new(item, "BookService")).GetAwaiter().GetResult();
                            // var bookrequest =

                            //var precheck Url1 = _keySettings.PrecheckEndPointb2b;
                            //var boo Url1 = _keySettings.BookEndPointb2b;

                            //commonXMLBookResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<CommonXMLBookResponse>(response);
                            //response = response.Replace("\\r\\n", "");   // remove line breaks
                            //.Replace("\\\"", "\""); // fix escaped quotes if needed
                            //if (commonXMLBookResponse.Error != null)
                            //{
                            if (commonXMLBookResponse != null && commonXMLBookResponse.Error.Count > 0)

                            {
                                ObjRespond.referenceId = commonXMLBookResponse.BookRS.ReferenceNo;
                                ObjRespond.bookingReferenceId = commonXMLBookResponse.BookRS.InternalReference;
                                ObjRespond.status = "BOOKING_FAILED";
                                ObjRespond.remark = commonXMLBookResponse.Error[0].description;
                            }
                            // }
                            else
                            {
                                //If book have no error then call the BOOKING DETAIL
                                CommonXMLOutBookingDetailRequest.BookingDetailRequest commonXMLOutBookingDetailRequest = new CommonXMLOutBookingDetailRequest.BookingDetailRequest();
                                //commonXMLOutBookingDetailRequest.Token = WebConfigurationManager.AppSettings["out_agoda_token"];
                                commonXMLOutBookingDetailRequest.Token = _keySettings.out_agoda_token;

                                CommonXMLOutBookingDetailRequest.Request request1 = new CommonXMLOutBookingDetailRequest.Request();

                                CommonXMLOutBookingDetailRequest.BookingDetailRQ bookingDetailRQ = new CommonXMLOutBookingDetailRequest.BookingDetailRQ();
                                bookingDetailRQ.BookingId = 0;
                                 bookingDetailRQ.InternalReference = commonXMLBookResponse.BookRS.InternalReference;
                                 bookingDetailRQ.ReferenceNo = commonXMLBookResponse.BookRS.ReferenceNo;

                                CommonXMLOutBookingDetailRequest.AdvancedOptions advancedOptions1 = new CommonXMLOutBookingDetailRequest.AdvancedOptions();
                                advancedOptions1.Currency = "USD";
                                advancedOptions1.CustomerIpAddress = "123";

                                commonXMLOutBookingDetailRequest.AdvancedOptions = advancedOptions1;
                                request1.BookingDetailRQ = bookingDetailRQ;
                                commonXMLOutBookingDetailRequest.Request = request1;

                                HotelX itemBD = new HotelX();
                                RQRS rQRSBD = new RQRS();
                                rQRSBD.Request = JsonConvert.SerializeObject(commonXMLOutBookingDetailRequest);
                                rQRSBD.Response = "";
                                itemBD.SearchTimeOut = 120000;
                                List<RQRS> RSBD = new List<RQRS>();
                                RSBD.Add(rQRSBD);

                                itemBD.RQRS = RSBD;
                                //string responseBookingDetail = GetSupplierResponseFinalNew(itemBD, "BookingDetail").Result;
                                //string responseBookingDetail = Task.Run(() => GetSupplierResponseFinalNew(itemBD, "BookingDetail")).GetAwaiter().GetResult();
                                string responseBookingDetail = Task.Run(() => GetSupplierResponseFinalNew_new(itemBD, "BookingDetail")).GetAwaiter().GetResult();


                                CommonXMLOutBookingDetailResponse.BookingDetailResponse bookingDetailResponse = new CommonXMLOutBookingDetailResponse.BookingDetailResponse();
                                bookingDetailResponse = JsonConvert.DeserializeObject<CommonXMLOutBookingDetailResponse.BookingDetailResponse>(responseBookingDetail);

                                if (bookingDetailResponse != null && bookingDetailResponse.Error.Count > 0)
                                {
                                    ObjRespond.referenceId = bookingDetailResponse.BookingDetailRS.HotelOption.ReferenceNo;
                                    ObjRespond.bookingReferenceId = bookingDetailResponse.BookingDetailRS.HotelOption.InternalReference;
                                    ObjRespond.status = "BOOKING_FAILED";
                                    ObjRespond.remark = commonXMLBookResponse.Error[0].description;
                                }
                                else if (bookingDetailResponse != null && bookingDetailResponse.Error.Count == 0)
                                {
                                    if (bookingDetailResponse.BookingDetailRS.HotelOption.HotelRooms[0].BookingStatus.ToLower() == "confirmed" || bookingDetailResponse.BookingDetailRS.HotelOption.HotelRooms[0].BookingStatus.ToLower() == "vouchered" || bookingDetailResponse.BookingDetailRS.HotelOption.HotelRooms[0].BookingStatus.ToLower() == "completed")
                                    {
                                        ObjRespond.referenceId = commonXMLBookResponse.BookRS.ReferenceNo;
                                        ObjRespond.bookingReferenceId = commonXMLBookResponse.BookRS.InternalReference;
                                        ObjRespond.status = "SUCCESS";
                                        ObjRespond.remark = "booking is confirmed";
                                    }
                                    else
                                    {
                                        ObjRespond.referenceId = bookingDetailResponse.BookingDetailRS.HotelOption.ReferenceNo;
                                        ObjRespond.bookingReferenceId = bookingDetailResponse.BookingDetailRS.HotelOption.InternalReference;
                                        ObjRespond.status = "BOOKING_FAILED";
                                        ObjRespond.remark = commonXMLBookResponse.Error[0].description;
                                    }

                                }
                                return ObjRespond;

                            }
                        }
                    }

                }
                catch (System.Exception ex)
                {
                    //ObjRespond.referenceId = commonXMLBookResponse.BookRS.ReferenceNo;
                    //ObjRespond.bookingReferenceId = commonXMLBookResponse.BookRS.InternalReference;
                    //ObjRespond.status = "BOOKING_FAILED";
                    //ObjRespond.remark = commonXMLBookResponse.Error[0].description;
                    _exception.Error(ex);
                }
                return ObjRespond;

            }
            catch (Exception ex)
            {
                //ObjRespond.referenceId = commonXMLBookResponse.BookRS.ReferenceNo;
                //ObjRespond.bookingReferenceId = commonXMLBookResponse.BookRS.InternalReference;
                //ObjRespond.status = "BOOKING_FAILED";
                //ObjRespond.remark = commonXMLBookResponse.Error[0].description;
                _exception.Error(ex);
                return ObjRespond;
            }

        }

        public async Task<string> GetSupplierResponseFinalNew_new_12062026(HotelX Item, string requestName)
        {
            int HttpStatusCode = 0;
            string responsedata = "";
            string Catchtype = "";
            string reqid = Convert.ToString(DateTime.Now.Ticks);
            //string finalUrl = "http://localhost:44373/api/xconnect/";  //needtochange
            string finalUrl = "";  //needtochange

            var timer = new System.Diagnostics.Stopwatch();
            timer.Start();

            try
            {
                ServicePointManager.Expect100Continue = true;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                using (HttpClientHandler handler = new HttpClientHandler())
                {
                    handler.Proxy = null;

                    // ✅ FIX: handle gzip/deflate automatically
                    handler.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

                    using (HttpClient client = new HttpClient(handler))
                    {
                        //string finalUrl = "";
                        requestName = requestName.ToLower();

                        // 🔹 URL + Timeout selection
                        if (requestName == "availability")
                        {
                            Item.SearchTimeOut = Convert.ToInt32(_keySettings.XMLOutAvailabilityTimeOut);

                            finalUrl = $"{_keySettings.EndPointXMLOut.TrimEnd('/')}/{_keySettings.AvailabilitywithcancellationXMLOut.TrimStart('/')}";
                        }
                        //else if (requestName == "prebook") // ✅ FIXED
                        else if (requestName == "precheck") // ✅ FIXED
                        {
                            Item.SearchTimeOut = Convert.ToInt32(_keySettings.XMLOutPrebookTimeOut);

                            //finalUrl = $"{_keySettings.EndPointforprebook.TrimEnd('/')}/{_keySettings.PrebookXMLOut.TrimStart('/')}";
                            finalUrl = $"{_keySettings.EndPointforprebook.TrimEnd('/')}/{_keySettings.AddHotelToCartXMLOut.TrimStart('/')}";

                        }
                        else if (requestName == "book")
                        {
                            Item.SearchTimeOut = Convert.ToInt32(_keySettings.XMLOutBookTimeOut);

                           // finalUrl = $"{"http://localhost:44373/api/xconnect"}/{_keySettings.BookXMLOut.TrimStart('/')}";
                            finalUrl = $"{_keySettings.EndPointforbook.TrimEnd('/')}/{_keySettings.BookXMLOut.TrimStart('/')}";

                        }
                        else if (requestName == "bookingdetail")
                        {
                            Item.SearchTimeOut = Convert.ToInt32(_keySettings.XMLOutBookingDetailTimeOut);

                            finalUrl = $"{_keySettings.EndPointforbookingdetail.TrimEnd('/')}/{_keySettings.BookingDetailXMLOut.TrimStart('/')}";
                        }
                        else if (requestName == "checkhotelcancellationcharges")
                        {
                            Item.SearchTimeOut = Convert.ToInt32(_keySettings.XMLOutCancellationChargesTimeOut);

                            finalUrl = $"{_keySettings.EndPointforCheckHotelCancellationCharges.TrimEnd('/')}/{_keySettings.CheckHotelCancellationChargesXMLOut.TrimStart('/')}";
                        }
                        else if (requestName == "cancelbooking")
                        {
                            Item.SearchTimeOut = Convert.ToInt32(_keySettings.XMLOutCancelBookingTimeOut);

                            finalUrl = $"{_keySettings.EndPointforCancelBooking.TrimEnd('/')}/{_keySettings.CancelBookingXMLOut.TrimStart('/')}";
                        }

                        // 🔥 Debug log (VERY IMPORTANT)
                        Console.WriteLine("FINAL URL: " + finalUrl);

                        client.Timeout = TimeSpan.FromMilliseconds(Item.SearchTimeOut);

                        string strxmlRQRS = Item.RQRS[0].Request;

                        // ❌ REMOVE token replacement (can break API)
                        // strxmlRQRS = strxmlRQRS.Replace("\"Token\":null", "\"Token\":\"\"");

                        HttpRequestMessage Request = new HttpRequestMessage(HttpMethod.Post, finalUrl);

                        Request.Content = new StringContent(strxmlRQRS, Encoding.UTF8, "application/json");

                        Request.Headers.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _keySettings.agoda_token);
                        //new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJhZ2VudElkIjoxOCwiYWdlbnRDb2RlIjoiUkEtMDAzIiwidXNlck5hbWUiOiJBZ29kYSIsImVtYWlsIjoia2luamFsLmdAdGVjaG5vaGVhdmVuLm5ldCIsInBob25lTm8iOiI3ODk0MTg4ODIwIiwidXNlcklkIjoxOCwic3ViVXNlcklkIjowLCJ1c2VyVHlwZUlkIjoxLCJ1c2VyVHlwZSI6IkFnZW50IiwidXNlckNvZGUiOiJBZ2VudCIsInBheW1lbnRUeXBlIjowLCJwYXltZW50VHlwZVZhbHVlIjoiUG9zdFBhaWQiLCJkaXNwbGF5Q3VycmVuY3lJZCI6NSwiY3JlZGl0bGltaXRDdXJyZW5jeUlkIjo1LCJjcmVkaXRsaW1pdEN1cnJlbmN5Q29kZSI6IlVTRCIsInJhdGVDYXRlZ29yeUlkIjoxLCJ0ZW5hbnRJZCI6IjMwMzcxMDg0LTcyNTgtRkNCNy0xMERGLTNBMUZENUFCQTIyMSIsImlzRnJvbUFwaSI6dHJ1ZSwic2Vzc2lvbklkIjoiYmI3NjhmMWUtNzRmOC00ZmEwLTk4NGItZWY2OGJkZTlhYjc4IiwiYXNzaXN0ZWRBZ2VudElkIjowLCJzZXJ2aWNlVHlwZSI6WyJIb3RlbCJdLCJyZXdhcmRQb2ludHMiOltdLCJwZXJtaXNzaW9uVHlwZSI6WyJGdWxsIEFjY2VzcyBQZXJtaXNzaW9uIiwiQ2FuIFZvdWNoZXIiLCJBbGxvdyBCb29raW5nIiwiRHVlIERhdGUgUmVtaW5kZXIiLCJEdWUgRGF0ZSBWb3VjaGVyIl0sImlzcyI6IlJBLTAwM18zMDM3MTA4NC03MjU4LUZDQjctMTBERi0zQTFGRDVBQkEyMjEiLCJpYXQiOjE3NzgzMzA3MTksImV4cCI6MTgwOTg2NjcxOX0.-S6xuOXONhJ8c2yi38nqwNARoen3LU2fSJFPLpRYI40");

                        //string ipAddress = _httpContextAccessor.HttpContext?
                        //            .Connection?
                        //            .RemoteIpAddress?
                        //            .ToString();

                        var clientIp = _httpContextAccessor.HttpContext.Connection.RemoteIpAddress?.ToString();
                        var existing = _httpContextAccessor.HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
                        var xForwardedFor = string.IsNullOrEmpty(existing)
                            ? clientIp
                            : $"{existing}, {clientIp}";

                        // Custom Header
                        //Request.Headers.Add("X-Custom-Host", "13");
                        Request.Headers.Add("X-Custom-Host", "AgodaReverse");
                        //Request.Headers.Add("X-Time-Zone", "abc");
                        Request.Headers.Add("X-Time-Zone", "Arabian Standard Time");
                        Request.Headers.Add("X-Accept-Language", "abc");
                        Request.Headers.Add("User-Agent", "PostmanRuntime/7.51.1");
                        Request.Headers.Add("X-Forwarded-For", xForwardedFor);

                        //X - Custom - Host : 13

                        Request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                        // ❌ REMOVE manual gzip header
                        // Request.Headers.Add("Accept-Encoding", "gzip");

                        using (var responseMessage = await client.SendAsync(Request))
                        {
                            HttpStatusCode = (int)responseMessage.StatusCode;

                            //Console.WriteLine("STATUS: " + HttpStatusCode);

                            // ✅ FIX: Direct read (auto decompressed)
                            responsedata = await responseMessage.Content.ReadAsStringAsync();

                            // Console.WriteLine("RESPONSE: " + responsedata);

                            await _commonMethod.XMLRequestResponseTrack_agoda(
                                finalUrl + " | " + strxmlRQRS,
                                responsedata,
                                "xmlout",
                                "Proper",
                                reqid
                            );
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Catchtype = ex.Message;
                Console.WriteLine("ERROR: " + ex.Message);
            }
            finally
            {
                timer.Stop();
                Item.RSTimeout = timer.Elapsed.ToString();
            }

            if (!string.IsNullOrEmpty(responsedata))
            {
                Item.ResponseType = "NoCount";
                Item.ResponseStatus = HttpStatusCode;
            }
            else
            {
                string Responsetype = "NoCount";

                if (Catchtype.ToLower().Contains("task was canceled") ||
                    Catchtype.ToLower().Contains("timeout"))
                {
                    Responsetype = "Timeout";
                }

                Item.ResponseType = Responsetype;
                Item.ResponseStatus = HttpStatusCode;
            }

            return responsedata;
        }


        public async Task<string> GetSupplierResponseFinalNew_new(HotelX item, string requestName)
        {
            int httpStatusCode = 0;
            string responseData = string.Empty;
            string catchType = string.Empty;
            string reqId = DateTime.Now.Ticks.ToString();
            var timer = Stopwatch.StartNew();
            try
            {
                requestName = requestName?.ToLowerInvariant() ?? string.Empty;
                string finalUrl = string.Empty;
                switch (requestName)
                {
                    case "availability":
                        item.SearchTimeOut = Convert.ToInt32(_keySettings.XMLOutAvailabilityTimeOut);
                        //finalUrl = $ "{_keySettings.EndPointXMLOut.TrimEnd('/')}/" + $ "{_keySettings.AvailabilitywithcancellationXMLOut.TrimStart('/')}";
                        finalUrl = $"{_keySettings.EndPointXMLOut.TrimEnd('/')}/{_keySettings.AvailabilitywithcancellationXMLOut.TrimStart('/')}";

                        break;
                    case "precheck":
                        item.SearchTimeOut = Convert.ToInt32(_keySettings.XMLOutPrebookTimeOut);
                        //finalUrl = $ "{_keySettings.EndPointforprebook.TrimEnd('/')}/" + $ "{_keySettings.AddHotelToCartXMLOut.TrimStart('/')}";
                        finalUrl = $"{_keySettings.EndPointforprebook.TrimEnd('/')}/{_keySettings.AddHotelToCartXMLOut.TrimStart('/')}";

                        break;
                    case "book":
                        item.SearchTimeOut = Convert.ToInt32(_keySettings.XMLOutBookTimeOut);
                        // finalUrl = $ "{_keySettings.EndPointforbook.TrimEnd('/')}/" + $ "{_keySettings.BookXMLOut.TrimStart('/')}";
                        finalUrl = $"{_keySettings.EndPointforbook.TrimEnd('/')}/{_keySettings.BookXMLOut.TrimStart('/')}";

                        break;
                    case "bookingdetail":
                        item.SearchTimeOut = Convert.ToInt32(_keySettings.XMLOutBookingDetailTimeOut);
                        //finalUrl = $ "{_keySettings.EndPointforbookingdetail.TrimEnd('/')}/" + $ "{_keySettings.BookingDetailXMLOut.TrimStart('/')}";
                        finalUrl = $"{_keySettings.EndPointforbookingdetail.TrimEnd('/')}/{_keySettings.BookingDetailXMLOut.TrimStart('/')}";

                        break;
                    case "checkhotelcancellationcharges":
                        item.SearchTimeOut = Convert.ToInt32(_keySettings.XMLOutCancellationChargesTimeOut);
                        //finalUrl = $ "{_keySettings.EndPointforCheckHotelCancellationCharges.TrimEnd('/')}/" + $ "{_keySettings.CheckHotelCancellationChargesXMLOut.TrimStart('/')}";
                        finalUrl = $"{_keySettings.EndPointforCheckHotelCancellationCharges.TrimEnd('/')}/{_keySettings.CheckHotelCancellationChargesXMLOut.TrimStart('/')}";

                        break;
                    case "cancelbooking":
                        item.SearchTimeOut = Convert.ToInt32(_keySettings.XMLOutCancelBookingTimeOut);
                        //finalUrl = $ "{_keySettings.EndPointforCancelBooking.TrimEnd('/')}/" + $ "{_keySettings.CancelBookingXMLOut.TrimStart('/')}";
                        finalUrl = $"{_keySettings.EndPointforCancelBooking.TrimEnd('/')}/{_keySettings.CancelBookingXMLOut.TrimStart('/')}";

                        break;
                    default:
                        // throw new ArgumentException($ "Unsupported request type: {requestName}");
                        throw new ArgumentException($"Unsupported request type: {requestName}");
                }
                Console.WriteLine("FINAL URL: " + finalUrl);

                var client = _httpClientFactory.CreateClient("AgodaClient");
                client.Timeout = TimeSpan.FromMilliseconds(item.SearchTimeOut);




                string requestBody = item.RQRS[0].Request;
                using
                var request = new HttpRequestMessage(HttpMethod.Post, finalUrl);
                request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _keySettings.agoda_token);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                var clientIp = _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString();
                var existingForwardedFor = _httpContextAccessor.HttpContext?.Request?.Headers["X-Forwarded-For"].FirstOrDefault();
                //var xForwardedFor = string.IsNullOrWhiteSpace(existingForwardedFor) ? clientIp : $ "{existingForwardedFor}, {clientIp}";
                var xForwardedFor = string.IsNullOrEmpty(existingForwardedFor) ? clientIp : $"{existingForwardedFor}, {clientIp}";
                request.Headers.Add("X-Custom-Host", "AgodaReverse");
                request.Headers.Add("X-Time-Zone", "Arabian Standard Time");
                request.Headers.Add("X-Accept-Language", "abc");
                request.Headers.Add("User-Agent", "PostmanRuntime/7.51.1");
                if (!string.IsNullOrWhiteSpace(xForwardedFor))
                {
                    request.Headers.Add("X-Forwarded-For", xForwardedFor);
                }
                using
                var response = await client.SendAsync(request, HttpCompletionOption.ResponseContentRead);
                httpStatusCode = (int)response.StatusCode;
                responseData = await response.Content.ReadAsStringAsync();
                // await _commonMethod.XMLRequestResponseTrack_agoda($ "{finalUrl} | {requestBody}", responseData, "xmlout", "Proper", reqId);
                await _commonMethod.XMLRequestResponseTrack_agoda(
                                finalUrl + " | " + requestBody,
                                responseData,
                                "xmlout",
                                "Proper",
                                reqId
                            );
            }
            catch (TaskCanceledException ex)
            {
                catchType = ex.Message;
                //Console.WriteLine($ "TIMEOUT: {ex.Message}");
                Console.WriteLine("TIMEOUT: " + ex.Message);
            }
            catch (Exception ex)
            {
                catchType = ex.Message;
                // Console.WriteLine($ "ERROR: {ex.Message}");
                Console.WriteLine("ERROR: " + ex.Message);

            }
            finally
            {
                timer.Stop();
                item.RSTimeout = timer.Elapsed.ToString();
            }
            if (!string.IsNullOrWhiteSpace(responseData))
            {
                item.ResponseType = "NoCount";
                item.ResponseStatus = httpStatusCode;
            }
            else
            {
                item.ResponseType = catchType.Contains("timeout", StringComparison.OrdinalIgnoreCase) || catchType.Contains("task was canceled", StringComparison.OrdinalIgnoreCase) ? "Timeout" : "NoCount";
                item.ResponseStatus = httpStatusCode;
            }
            return responseData;
        }


        public List<XMLOutRoomDetail> GetXMLOutRoomDetail(int numberOfRooms, int numberOfPassengers, string numberOfChildAge)
        {
            try
            {
                //int numberOfRooms = 2;
                //int numberOfPassengers = 5;
                //string numberOfChildAge = "2";

                // Calculate base number of passengers per room and remaining passengers
                int passengersPerRoom = numberOfPassengers / numberOfRooms;
                int remainingPassengers = numberOfPassengers % numberOfRooms;

                // Create an array to store the number of passengers in each room
                List<XMLOutRoomDetail> xMLOutRoomDetail = new List<XMLOutRoomDetail>();

                // Assign base number of passengers to each room
                for (int i = 0; i < numberOfRooms; i++)
                {
                    XMLOutRoomDetail rm = new XMLOutRoomDetail();
                    rm.RoomNo = i + 1;
                    rm.NoofPassanger = passengersPerRoom;
                    xMLOutRoomDetail.Add(rm);
                }
                // Distribute remaining passengers across rooms
                for (int i = 0; i < remainingPassengers; i++)
                {
                    xMLOutRoomDetail[i].NoofPassanger += 1;
                }
                string[] noofchildarray = numberOfChildAge.Split(new String[] { "###" }, StringSplitOptions.None);
                //for (int i = 0; i < noofchildarray.Length; i++)
                //{
                //    xMLOutRoomDetail[i].ChildAge += Convert.ToInt32(noofchildarray[i]);
                //}
                if (noofchildarray.Length > 0 && noofchildarray.Length <= numberOfRooms)
                {
                    for (int i = 0; i < noofchildarray.Length; i++)
                    {
                        xMLOutRoomDetail[i].ChildAge = new List<int> { Convert.ToInt32(noofchildarray[i]) };
                    }
                }
                else
                {
                    List<int> childAges = noofchildarray.Select(age => int.Parse(age)).ToList();
                    xMLOutRoomDetail[0].ChildAge = childAges;
                }
                return xMLOutRoomDetail;
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
                return null;
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
