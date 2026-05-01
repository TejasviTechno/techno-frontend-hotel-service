using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using XConnect.CommonUtilities.Interface;
using XConnect.CommonUtilities;
using XConnect.ErrorLogHandler.Interface;
using XConnect.Markup.Interface;
using XConnect.Models.Hotel;
using XConnect.Models;
using XConnect.Supplier.PkFare.Interface;
using Options = XConnect.Models.Options;
using Newtonsoft.Json;
using static Azure.Core.HttpHeader;
using System.Net;
using System.Security.Cryptography;
using System.IO.Compression;
using static XConnect.Supplier.Booking.Com.preebookrs;


namespace XConnect.Supplier.PkFare
{
    public class PkFareRepository : IPkFare
    {
        private readonly ICommonMethod _commonMethod;
        private readonly IException _exception;
        private readonly IMarkup _markup;
        private readonly ISupplierRes _supplierRes;
        private readonly KeySettings _keySettings;

        public PkFareRepository(ICommonMethod commonMethod, IException exception, IMarkup markup, ISupplierRes supplierRes, IOptions<KeySettings> keySettings)
        {
            _commonMethod = commonMethod;
            _exception = exception;
            _markup = markup;
            _supplierRes = supplierRes;
            _keySettings = keySettings.Value;
        }


        public void GetPkFareRequestOnly_Async(DataRow[] commonCode, List<StarRating> listRating, DataTable dtPreferredDataHotel, HotelSearchParameter SearchPara, List<SupplierSearchConfig> lstPkFareSearchConfig, List<SupplierDetails> lstPkFareDetails, ref ConcurrentBag<HotelX> AllTask, string AgentCode, DataSet dtXMLCity, int Hit, DataTable dtROE, TokenData tokenData)
        {
            try
            {

                List<StarRating> lstRating = new List<StarRating>();
                lstRating = _commonMethod.GetStarRatingStringList_xtg(SearchPara);

                List<HotelCodeByCity> DtHotelDAta = HtmlExtensions.ToList<HotelCodeByCity>(commonCode.CopyToDataTable());
                int ratingcount = lstRating.Count;
                int hotelcount = DtHotelDAta.Count;

                foreach (SupplierDetails PkFareDetails in lstPkFareDetails)
                {
                    try
                    {

                        int SupplierID = PkFareDetails.SupplierID;
                        string Supplier = PkFareDetails.SupplierName;

                        if (PkFareCheckRoomPax(SearchPara))
                        {
                            if (lstPkFareSearchConfig.Count > 0)
                            {
                                List<SupplierSearchConfig> PkFareSearchConfig = lstPkFareSearchConfig
                                          .Where(x => x != null && x.SupplierID == SupplierID)
                                          .ToList();

                                if (PkFareSearchConfig != null && PkFareSearchConfig.Count > 0)
                                {
                                    string Ids = string.Empty;
                                    string xmlbase = "";
                                    _commonMethod.GetPrefferedHotelCodes(ref Ids, ref xmlbase, ref PkFareSearchConfig, Supplier, dtPreferredDataHotel, SearchPara.IsSearchFor);



                                    if (PkFareSearchConfig != null)
                                    {
                                        List<HotelCodeByCity> filter = new List<HotelCodeByCity>();
                                        filter = _commonMethod.FilterWithOutCity_Code(ratingcount, hotelcount, DtHotelDAta, lstRating, Supplier);

                                        if (filter.Count > 0)
                                        {

                                            if (Ids == string.Empty)
                                            {

                                                double CurrencyFactorPkFare = 1;
                                                string Currency = Convert.ToString(PkFareDetails.CurrencyShortName);


                                                if (Currency.ToLower() != tokenData.SiteCurrency.ToLower())
                                                {
                                                    CurrencyFactorPkFare = _commonMethod.GetROEByCurrency(Currency, dtROE);// Convert.ToDouble(dtInrCurrency.Rows[0]["AmtToExch"]);
                                                }
                                                DataForSearch dataForSearch = _commonMethod.GetXmlDataForSearchTechwise(SearchPara.Nationality, dtXMLCity, Supplier, PkFareDetails.TechName);

                                                //if (dataForSearch.XmlNationality != "")
                                                {
                                                    for (int k = 0; k < PkFareSearchConfig.Count; k++)
                                                    {
                                                        bool DebugMode = (Convert.ToInt32(PkFareSearchConfig[k].DebugMode) == 0 ? false : true);
                                                        string UserName = Convert.ToString(PkFareDetails.UserName);
                                                        string Password = Convert.ToString(PkFareDetails.Password);
                                                        {
                                                            int IsCitywisesearch = Convert.ToInt32(PkFareSearchConfig[k].City);
                                                            if (IsCitywisesearch == 0)//(DtHotelData.DefaultView.Count > 0)
                                                            {
                                                                if (Convert.ToInt32(PkFareSearchConfig[k].CityID) == 0 || Convert.ToInt32(PkFareSearchConfig[k].CityID) == SearchPara.CityID)
                                                                {
                                                                    //List<HotelCodeByCity> DtHotelData = FilterFinalList(commonCode, listRating);
                                                                    //                                    DtHotelData.DefaultView.RowFilter = "Rating in (" + Rating + ") and supplierName='" + sup + "'";
                                                                    if (commonCode.Length > 0)
                                                                    {
                                                                        int Noofrecords = Convert.ToInt32(PkFareSearchConfig[k].Noofrecords);
                                                                        double count = Convert.ToDouble(filter.Count);
                                                                        int NoOfDiv = Convert.ToInt32(Math.Ceiling(count / Convert.ToDouble(Noofrecords)));
                                                                        List<List<XMLID_Search>> lstxml = _commonMethod.GetXMLIDString(NoOfDiv, Noofrecords, count, filter);

                                                                        for (int a = 0; a < NoOfDiv; a++)
                                                                        {
                                                                            string IdsForSearch = Convert.ToString(_commonMethod.GetHotelCodeByCOmma(lstxml[a]));


                                                                            HotelX Xhotel = new HotelX();
                                                                            Xhotel.Supplier = Supplier;
                                                                            Xhotel.DebugeMode = DebugMode;
                                                                            Xhotel.SearchTimeOut = Convert.ToInt32(PkFareSearchConfig[k].HotelSearchTimeOut);
                                                                            //if (Hit > 0)
                                                                            Xhotel.ProcessTimeout = Convert.ToInt32(PkFareSearchConfig[k].HotelSearchTimeOut2);

                                                                            //Xhotel.URL = Convert.ToString(PkFareDetails[0].WebSiteURLforAPI) + Convert.ToString("2.0/hotels");
                                                                            // xhotel.URL = Convert.ToString(AgodaV1Details[0].WebSiteURLforAPI);//Added on 3-MAR-2025
                                                                            //if (ConfigurationManager.AppSettings["SupplierProxy"] == "0")//Added on 9-APR-2025
                                                                            //{
                                                                            //    Xhotel.URL = Convert.ToString(PkFareDetails[0].WebSiteURL) + Convert.ToString("2.0/hotels");
                                                                            //}
                                                                            //else
                                                                            //{
                                                                            if (_keySettings.SupplierProxy == "0")
                                                                                Xhotel.URL = Convert.ToString(PkFareDetails.WebSiteURL) + PkFareInfo.availability;//Added on 3-MAR-2025
                                                                            else
                                                                                Xhotel.URL = Convert.ToString(PkFareDetails.WebSiteURLforAPI) + PkFareInfo.availability;//Added on 3-MAR-2025

                                                                            Xhotel.UserId = Convert.ToString(PkFareDetails.UserID);
                                                                            Xhotel.UserName = Convert.ToString(PkFareDetails.UserName);
                                                                            Xhotel.PassWord = Convert.ToString(PkFareDetails.Password); ;

                                                                            Xhotel.CurrencyCode = Currency;
                                                                            Xhotel.CurrencyFactor = CurrencyFactorPkFare;
                                                                            Xhotel.XmlCity = dataForSearch.xmlCity;
                                                                            Xhotel.XmlCountry = dataForSearch.xmlCountry;
                                                                            Xhotel.XmlNationality = dataForSearch.XmlNationality;
                                                                            Xhotel.AgentWiseMarkup = PkFareDetails.AgentWiseMarkup;
                                                                            Xhotel.IsShowSupplier = Convert.ToInt16(PkFareDetails.IsShowSupplier) == 1 ? true : false;
                                                                            Xhotel.SupplierID = PkFareDetails.SupplierID;
                                                                            Xhotel.MasterSupplierID = PkFareDetails.MasterSupplierId;
                                                                            //Xhotel.Request = Common.XMLPkFareRequestOnly_Async(SearchPara, XmlNationality, Xhotel.SearchTimeOut, IdsForSearch);
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
                                                                                objrq.Request = XMLPkFareRequestOnly_Async(SearchPara, Xhotel.XmlNationality, IdsForSearch);
                                                                                objrq.Response = ""; RQRS.Add(objrq);
                                                                            }
                                                                            Xhotel.RQRS = RQRS;
                                                                            Xhotel.ReturnRooms = PkFareDetails.ReturnRooms;
                                                                            Xhotel.TechName = PkFareDetails.TechName;
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

        public bool PkFareCheckRoomPax(HotelSearchParameter objSearPera)
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
        public string XMLPkFareRequestOnly_Async(HotelSearchParameter SearchParam, string Nationality, string hotelid)
        {
            try
            {
                //JsonSerializerSettings Setting = new JsonSerializerSettings
                //{
                //    NullValueHandling = NullValueHandling.Ignore,
                //    DefaultValueHandling = DefaultValueHandling.Ignore
                //};
                string checkIn = System.Convert.ToString(SearchParam.CheckInDate);
                string checkout = Convert.ToString(SearchParam.CheckOutDate);
                checkIn = checkIn.Substring(6, 4) + "-" + checkIn.Substring(3, 2) + "-" + checkIn.Substring(0, 2);
                checkout = checkout.Substring(6, 4) + "-" + checkout.Substring(3, 2) + "-" + checkout.Substring(0, 2);
                int NoofAdult = 0;
                int NoofChild = 0;

                List<PkfsearchRQ.Occupancy> Roomlists = new List<PkfsearchRQ.Occupancy>();

                List<int> AllChild = new List<int>();
                for (int j = 0; j < SearchParam.Rooms.Count; j++)
                {

                    PkfsearchRQ.Occupancy Roomlist = new PkfsearchRQ.Occupancy();
                    Roomlist.rooms = 1;
                    Roomlist.adults = SearchParam.Rooms[j].NoofAdults;
                    Roomlist.children = SearchParam.Rooms[j].NoofChild;

                    NoofChild = SearchParam.Rooms[j].NoofChild;

                    List<PkfsearchRQ.Paxis> Childs = new List<PkfsearchRQ.Paxis>();
                    // PkfsearchRQ.Paxis Child= new PkfsearchRQ.Paxis();
                    if (NoofChild > 0)
                    {

                        //switch (NoofChild)
                        //{
                        //    case 1:
                        //        Child.type = "CH";
                        //        Child.age=(Convert.ToInt32(SearchParam.Rooms[j].Child1Age));

                        //        break;

                        //    case 2:
                        //        Child.type = "CH";
                        //        Child.age = (Convert.ToInt32(SearchParam.Rooms[j].Child1Age));
                        //        Child.type = "CH";
                        //        Child.age =(Convert.ToInt32(SearchParam.Rooms[j].Child2Age));

                        //        break;


                        //}
                        Childs.Add(new PkfsearchRQ.Paxis
                        {
                            type = "CH",
                            age = SearchParam.Rooms[j].Child1Age
                        });

                        if (NoofChild > 1)
                        {
                            Childs.Add(new PkfsearchRQ.Paxis
                            {
                                type = "CH",
                                age = SearchParam.Rooms[j].Child2Age
                            });
                        }
                        Roomlist.paxes = Childs;

                    }

                    Roomlists.Add(Roomlist);
                }
                PkfsearchRQ.Hotels hotelids = new PkfsearchRQ.Hotels();

                PkfsearchRQ.Root RootObject = new PkfsearchRQ.Root();

                List<Int32> hoteids = new List<Int32>();

                string[] hotelIdsArray = hotelid.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var id in hotelIdsArray)
                {
                    string trimmedId = id.Trim();
                    if (!string.IsNullOrWhiteSpace(trimmedId))
                    {
                        int parsedId;
                        if (int.TryParse(trimmedId, out parsedId))
                        {
                            hoteids.Add(parsedId);
                        }

                    }
                }
                PkfsearchRQ.Stay Stays = new PkfsearchRQ.Stay();
                Stays.checkIn = checkIn;
                Stays.checkOut = checkout;
                Stays.allowOnlyShift = true;

                hotelids.hotel = hoteids;

                RootObject.hotels = hotelids;

                RootObject.aifUse = false;
                RootObject.dailyRate = false;
                RootObject.nationality = Nationality;
                RootObject.occupancies = Roomlists;
                RootObject.stay = Stays;

                string Request = JsonConvert.SerializeObject(RootObject);
                return Request;

            }
            catch (Exception ex)
            {
                _exception.Error(ex);
                return "";
            }
        }

        public async Task<List<Options>> PkFare_HotelSearchWithRooms(DataTable dtROE, CommonSearch commonSearch, HotelX item, string ResponseData, string WSurl, string Username, string Password, Int32 SearchTimeOut, HotelSearchParameter SearchParam, bool DebugMode, string Currency, double CurrencyFactor, DataSet dsAgentDetails, string SupplierName, DataTable dtCountryCityMarkup, Int32 AgentId, Int32 TariffID, double AgentPercentage, Boolean IsShowSupplier, int SupplierID, int MasterSupplierID, Int32 processTimeout, string ResProccessTimerLogActive, string ResProccessTimeValueforSaveLog)
        {
            //return await Task.Run(() =>
            //{
            System.Diagnostics.Stopwatch timerroomx = new System.Diagnostics.Stopwatch();
            timerroomx.Start();

            List<Options> options = new List<Options>();

            DataTable DtHotel = _commonMethod.GenerateDataTableNewV2();


            try
            {
                if (ResponseData == null || ResponseData.Trim() == "")
                {
                    item.ResponseStatus = 204;
                    item.ResponseType = "NoCount";
                    return options;
                }
                //if (ResponseData.ToLower().Contains("error"))
                //{
                //    item.ResponseType = "NoCount";
                //    return DtHotel;
                //}


                //PkfsearchRS.Root response = JsonConvert.DeserializeObject<PkfsearchRS.Root>(ResponseData);
                PkfsearchRS.Root PkFareResponse = JsonConvert.DeserializeObject<PkfsearchRS.Root>(ResponseData);


                if (PkFareResponse.hotels == null)
                {
                    item.ResponseStatus = 404;
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
                if (PkFareResponse != null && PkFareResponse.hotels != null)
                {
                    for (int hotelIndex = 0; hotelIndex < PkFareResponse.hotels.hotel.Count; hotelIndex++)
                    {

                        var hotel = PkFareResponse.hotels.hotel[hotelIndex];
                        var Hotelname = hotel.name;
                        Currency = hotel.currency;
                        if (hotel.room == null || hotel.room.Count == 0)
                        {
                            continue;
                        }
                        item.ResponseType = "Success";

                        double MinPrice = 0;
                        MinPrice = PkFareResponse.hotels.hotel[hotelIndex].minRate;
                        //if (hotel.room != null)
                        //{
                        //    for (int roomIndex = 0; roomIndex < hotel.room.Count; roomIndex++)
                        //    {
                        //        //var room = hotel.room[roomIndex];
                        //        for (int roomIndexrate = 0; roomIndexrate < hotel.room[roomIndex].rate.Count; roomIndexrate++)
                        //        {


                        //            if (roomIndexrate == 0)
                        //            {

                        //                MinPrice = hotel.room[roomIndex].rate[roomIndexrate].net;
                        //            }
                        //            else
                        //            {
                        //                if (MinPrice > hotel.room[roomIndex].rate[roomIndexrate].net)
                        //                {
                        //                    MinPrice = hotel.room[roomIndex].rate[roomIndexrate].net;
                        //                }
                        //            }
                        //        }
                        //    }
                        //}
                        if (Currency.ToLower() != commonSearch.tokenData.SiteCurrency.ToLower())
                        {

                            CurrencyFactor = _commonMethod.GetROEByCurrency(Currency, dtROE);
                        }

                        MinPrice = MinPrice * CurrencyFactor;

                        DataRow drXmlHotel = DtHotel.NewRow();

                        // DataRow[] dtdetails = dsBulkdata.Tables["MappingDetails"].Select("XmlId='" + response.hotels.hotel[hotelIndex].code + "' and SupplierName='" + SupplierName + "' ");
                        //if (dtdetails.Length > 0 && MinPrice > 0)
                        {
                            int RaynaId = PkFareResponse.hotels.hotel[hotelIndex].code;
                            //DataRow[] drGiataDetails = dsBulkdata.Tables["GiataDetails"].Select("RaynaId='" + RaynaId + "' and Rating in ('" + strRating + "')");
                            //DataTable dsBulkdatacopy = dsBulkdata.Tables["GiataDetails"].Copy();
                            //DataRow[] drGiataDetails = dsBulkdatacopy.Select("RaynaId='" + RaynaId + "'");
                            //if (drGiataDetails.Length > 0)
                            {
                                if (SearchParam.Filters.IsShowRooms == 0)
                                {

                                    drXmlHotel["ContractID"] = 0;
                                    drXmlHotel["SupplierId"] = SupplierID;
                                    //drXmlHotel["AgentPercentage"] = AgentPercentage;
                                    drXmlHotel["MasterSupplierId"] = Convert.ToInt32(MasterSupplierID);
                                    drXmlHotel["GIITACode"] = 0;
                                    drXmlHotel["UniqueNo"] = PkFareResponse.hotels.hotel[hotelIndex].code;
                                    drXmlHotel["AvailToken"] = "";//response.sessionid;
                                    drXmlHotel["RaynaId"] = RaynaId;
                                    drXmlHotel["OwnsystemHotelId"] = 0;
                                    drXmlHotel["Favorite"] = 0;
                                    drXmlHotel["VatdiffAmount"] = 0;
                                    drXmlHotel["XMLHotelCode"] = PkFareResponse.hotels.hotel[hotelIndex].code;
                                    drXmlHotel["SupplierName"] = SupplierName;
                                    drXmlHotel["SupplierCurrency"] = Currency;
                                    // dr["XmlHotelCity"] = hotel.city;
                                    //double FinalPrice = Common.setHotelListMarkup(SearchParam, dtCountryCityMarkup, Convert.ToInt32(dsAgentDetails.Tables[0].Rows[0]["IsParentId"]), Convert.ToDouble(dsAgentDetails.Tables[0].Rows[0]["B2CMarkupPer"]), AgentId, TariffID, AgentPercentage, MinPrice, Convert.ToString(drGiataDetails[0]["GiataId"]), SupplierID, Convert.ToString(drGiataDetails[0]["Rating"]));
                                    //double FinalPrice = 1;
                                    drXmlHotel["FinalSellingPrice"] = MinPrice;
                                    drXmlHotel["SellingPrice"] = MinPrice;
                                    double TotalCharges = MinPrice;
                                    //if (dr["HotelName"].ToString() == Hotelname)
                                    //{
                                    //    DtHotel.Rows.Add(dr);
                                    //}
                                    DtHotel.Rows.Add(drXmlHotel);
                                    if (TotalCharges > 0)
                                    {
                                        #region "Room code Block"
                                        bool willAddOrNot = true;
                                        Int64 RaynaID = 0;
                                        Int64 GiataId = 0;
                                        int Rating = 0;
                                        Int64 OwnsystemHotelId = 0;
                                        double MiinPrice = MinPrice;
                                        _markup.GetHotelDataCommon(ref MiinPrice, ref willAddOrNot, ref OwnsystemHotelId, ref RaynaID, ref GiataId, ref Rating, ref drXmlHotel, commonSearch.AgentId, commonSearch.TariffID, commonSearch.dsAgentDetails, commonSearch.supplierGiataMaster, SearchParam, commonSearch.dtCountryCityMarkup, commonSearch.B2CMarkup, commonSearch.IsParentId, commonSearch.SubAgentMarkup, commonSearch.SearchByHotel, commonSearch.HotelIDS);
                                        if (willAddOrNot)
                                        {
                                            #region "Room Logic"
                                            DataTable dt_HotelRoom = _commonMethod.GenerateRoomtypeDataTableNewV2();
                                            DataTable dt_Cancellation = _commonMethod.GenerateCancellationDataTableNewV2();
                                            DataTable dt_RateBreackUp = _commonMethod.GenerateoomRateBreackUpDataTableNewV2();
                                            #endregion
                                            decimal LowestPrice = Convert.ToDecimal(MinPrice).CalculatePrice(commonSearch.tokenData, commonSearch.advancedOptions, item.CurrencyCode);
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
                                    drXmlHotel["MasterSupplierId"] = Convert.ToInt32(MasterSupplierID);
                                    drXmlHotel["GIITACode"] = 0;
                                    drXmlHotel["UniqueNo"] = PkFareResponse.hotels.hotel[hotelIndex].code;
                                    drXmlHotel["AvailToken"] = "";//response.sessionid;
                                    drXmlHotel["RaynaId"] = RaynaId;
                                    drXmlHotel["OwnsystemHotelId"] = 0;
                                    drXmlHotel["Favorite"] = 0;
                                    drXmlHotel["VatdiffAmount"] = 0;
                                    drXmlHotel["XMLHotelCode"] = PkFareResponse.hotels.hotel[hotelIndex].code;
                                    drXmlHotel["SupplierName"] = SupplierName;
                                    drXmlHotel["SupplierCurrency"] = Currency;
                                    // dr["XmlHotelCity"] = hotel.city;
                                    //double FinalPrice = Common.setHotelListMarkup(SearchParam, dtCountryCityMarkup, Convert.ToInt32(dsAgentDetails.Tables[0].Rows[0]["IsParentId"]), Convert.ToDouble(dsAgentDetails.Tables[0].Rows[0]["B2CMarkupPer"]), AgentId, TariffID, AgentPercentage, MinPrice, Convert.ToString(drGiataDetails[0]["GiataId"]), SupplierID, Convert.ToString(drGiataDetails[0]["Rating"]));
                                    //double FinalPrice = 1;
                                    drXmlHotel["FinalSellingPrice"] = MinPrice;
                                    drXmlHotel["SellingPrice"] = MinPrice;
                                    double TotalCharges = MinPrice;
                                    //if (dr["HotelName"].ToString() == Hotelname)
                                    //{
                                    //    DtHotel.Rows.Add(dr);
                                    //}
                                    DtHotel.Rows.Add(drXmlHotel);
                                    if (TotalCharges > 0)
                                    {
                                        #region "Room code Block"
                                        bool willAddOrNot = true;
                                        Int64 RaynaID = 0;
                                        Int64 GiataId = 0;
                                        int Rating = 0;
                                        Int64 OwnsystemHotelId = 0;
                                        double MiinPrice = TotalCharges;
                                        int CutOffDays = XMLHotelBookingCommon.CutOffDays;
                                        CutOffDays = _commonMethod.GetCutOffDays(commonSearch.dtSuplierMarkup, item.Supplier);
                                        _markup.GetHotelDataCommon(ref MiinPrice, ref willAddOrNot, ref OwnsystemHotelId, ref RaynaID, ref GiataId, ref Rating, ref drXmlHotel, commonSearch.AgentId, commonSearch.TariffID, commonSearch.dsAgentDetails, commonSearch.supplierGiataMaster, SearchParam, commonSearch.dtCountryCityMarkup, commonSearch.B2CMarkup, commonSearch.IsParentId, commonSearch.SubAgentMarkup, commonSearch.SearchByHotel, commonSearch.HotelIDS);
                                        if (willAddOrNot)
                                        {
                                            #region "Room Logic"
                                            DataTable dtPkFareRoom = _commonMethod.GenerateRoomtypeDataTableNewV2();
                                            DataTable dtPkFareCancellationPolicy = _commonMethod.GenerateCancellationDataTableNewV2();
                                            DataTable dtPkFareRoomRateBreakup = _commonMethod.GenerateoomRateBreackUpDataTableNewV2();
                                            double AgentMarkupAmount = 0.0;
                                            Boolean isPerce = true;
                                            Boolean blnIsShowSupplier = false;

                                            //PkfsearchRS.Root PkFareResponse = JsonConvert.DeserializeObject<PkfsearchRS.Root>(ResponseData);
                                            _markup.getAgetMarkupSetting(SupplierName, GiataId.ToString(), commonSearch.dtSuplierMarkup, commonSearch.dtMarkupDetails, ref AgentPercentage, ref AgentMarkupAmount, ref blnIsShowSupplier, ref isPerce, commonSearch.AgentId, Rating, SearchParam.CityID, SearchParam.CountryID.ToString(), SearchParam.CheckInDate);


                                            Currency = Convert.ToString(PkFareResponse.hotels.hotel[hotelIndex].currency);
                                            CurrencyFactor = 1;


                                            if (Currency.ToLower() != commonSearch.tokenData.SiteCurrency.ToLower())
                                            {
                                                //DataTable dtInrCurrency = _commonMethod.GetCountryCurrency(Currency);
                                                //CurrencyFactor = Convert.ToDouble(dtInrCurrency.Rows[0]["AmtToExch"]);

                                                CurrencyFactor = _commonMethod.GetROEByCurrency(Currency, dtROE);
                                            }
                                            for (int roomIndex = 0; roomIndex < PkFareResponse.hotels.hotel[hotelIndex].room.Count; roomIndex++)
                                            {
                                                string RoomName = Convert.ToString(PkFareResponse.hotels.hotel[hotelIndex].room[roomIndex].name);
                                                string roomid = Convert.ToString(PkFareResponse.hotels.hotel[hotelIndex].room[roomIndex].code);
                                                for (int roomrateIndex = 0; roomrateIndex < PkFareResponse.hotels.hotel[hotelIndex].room[roomIndex].rate.Count; roomrateIndex++)
                                                {
                                                    // if (room != null)
                                                    {
                                                        string remarks = "";
                                                        string rateKey = Convert.ToString(PkFareResponse.hotels.hotel[hotelIndex].room[roomIndex].rate[roomrateIndex].rateKey);


                                                        string roomtypeid = roomid + "###" + rateKey;

                                                        double Finalprice = Convert.ToDouble(PkFareResponse.hotels.hotel[hotelIndex].room[roomIndex].rate[roomrateIndex].net);


                                                        Finalprice = Finalprice * CurrencyFactor;
                                                        double CompanyBuyingPrice = Finalprice; //new


                                                        string Boardcode = Convert.ToString(PkFareResponse.hotels.hotel[hotelIndex].room[roomIndex].rate[roomrateIndex].boardCode);
                                                        string Meal = "Room Only";
                                                        switch (Boardcode)
                                                        {
                                                            case "RO":
                                                                Meal = "Room Only";
                                                                break;
                                                            case "B1":
                                                                Meal = "Breakfast for one guest";
                                                                break;
                                                            case "B2":
                                                                Meal = "Breakfast for two guests";
                                                                break;
                                                            default:
                                                                Meal = "Room Only";
                                                                break;
                                                        }
                                                        if (PkFareResponse.hotels.hotel[hotelIndex].room[roomIndex].rate[roomrateIndex].taxesAndFees != null)
                                                        {
                                                            for (int d = 0; d < PkFareResponse.hotels.hotel[hotelIndex].room[roomIndex].rate[roomrateIndex].taxesAndFees.Count; d++)
                                                            {
                                                                remarks += "<br/>INCLUDED :" + Convert.ToString(PkFareResponse.hotels.hotel[hotelIndex].room[roomIndex].rate[roomrateIndex].taxesAndFees[d].include);
                                                                remarks += "<br/>" + PkFareResponse.hotels.hotel[hotelIndex].room[roomIndex].rate[roomrateIndex].taxesAndFees[d].description + ": " + PkFareResponse.hotels.hotel[hotelIndex].room[roomIndex].rate[roomrateIndex].taxesAndFees[d].amount + " " + PkFareResponse.hotels.hotel[hotelIndex].room[roomIndex].rate[roomrateIndex].taxesAndFees[d].currency;
                                                            }
                                                        }
                                                        #region Ratebreakup
                                                        //for (int n = 0; n < SearchParam.NoofNights; n++)
                                                        //{
                                                        //    DateTime dtCheckInDate = DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null);
                                                        //    DateTime dtCheckOutDate = DateTime.ParseExact(SearchParam.CheckOutDate, "dd/MM/yyyy", null);
                                                        //    TimeSpan t = dtCheckOutDate - dtCheckInDate;
                                                        //    double ratePerNight = Finalprice;

                                                        //    #region Vat
                                                        //    double VatOnPerNight = 0;
                                                        //    ratePerNight = _markup.ProcessHotelRateBreakupPrice_WithMarkup(1, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), ratePerNight, commonSearch.B2CMarkup, commonSearch.IsParentId, isPerce, AgentPercentage, AgentMarkupAmount, commonSearch.SubAgentMarkup, ref VatOnPerNight);
                                                        //    #endregion

                                                        //    ratePerNight = ratePerNight / SearchParam.NoofNights;
                                                        //    DataRow drRateBreakUp = dtPkFareRoomRateBreakup.NewRow();
                                                        //    drRateBreakUp["SupplierName"] = SupplierName;
                                                        //    drRateBreakUp["OwnsystemHotelId"] = OwnsystemHotelId;
                                                        //    drRateBreakUp["ContractID"] = 0;
                                                        //    drRateBreakUp["RoomNo"] = 1;
                                                        //    drRateBreakUp["RoomTypeId"] = roomtypeid;
                                                        //    drRateBreakUp["Meal"] = Meal;
                                                        //    string rateWeekColumn = Convert.ToString(dtCheckInDate.Date.AddDays(n).DayOfWeek).Substring(0, 3);
                                                        //    drRateBreakUp["DateOfWeek"] = rateWeekColumn;
                                                        //    DateTime rateDate = dtCheckInDate.Date.AddDays(n);
                                                        //    DateTime Day = DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null);
                                                        //    Day = Day.AddDays(n);
                                                        //    string Days = Convert.ToString(Day);
                                                        //    drRateBreakUp["Date"] = rateDate.ToString("dd/MM/yyyy");
                                                        //    drRateBreakUp["MealId"] = Meal;
                                                        //    drRateBreakUp["Rate"] = ratePerNight;
                                                        //    drRateBreakUp["OrderNo"] = 1;
                                                        //    drRateBreakUp["RateType"] = "A Rate";
                                                        //    drRateBreakUp["RoomTypeName"] = RoomName;
                                                        //    drRateBreakUp["DateOfWeek"] = rateWeekColumn;
                                                        //    drRateBreakUp["BookingStatus"] = "Available";
                                                        //    drRateBreakUp["BoardTypeID"] = "";
                                                        //    drRateBreakUp["VatdiffAmount"] = 0;
                                                        //    drRateBreakUp["SupplierCurrency"] = Currency;
                                                        //    drRateBreakUp["BuyingPrice"] = ratePerNight;

                                                        //    dtPkFareRoomRateBreakup.Rows.Add(drRateBreakUp);
                                                        //}
                                                        #endregion

                                                        #region Rooms
                                                        DataRow drRoom = dtPkFareRoom.NewRow();
                                                        drRoom["OwnsystemHotelId"] = OwnsystemHotelId;
                                                        drRoom["MealId"] = Meal;
                                                        drRoom["MealName"] = Meal;
                                                        drRoom["Type"] = "";
                                                        drRoom["Board"] = "";
                                                        drRoom["BookingStatus"] = "Available";
                                                        drRoom["UniqueNo"] = Guid.NewGuid().ToString();
                                                        drRoom["ContractID"] = 0;
                                                        drRoom["RoomNo"] = 1;
                                                        drRoom["BoardTypeID"] = "";
                                                        drRoom["AdultsOccupancy"] = totalAdult;
                                                        drRoom["ChildOccupancy"] = totalChild;
                                                        drRoom["OfferCode"] = "";
                                                        drRoom["OfferDescription"] = "";
                                                        drRoom["RoomSequenceNo"] = "";
                                                        drRoom["RoomTypeCode"] = roomid;
                                                        drRoom["EssentialInformation"] = remarks;
                                                        drRoom["CompanyBuyingPrice"] = Finalprice;

                                                        #region Vat
                                                        _markup.ProcessHotelRoom_WithMarkup(CurrencyFactor, Currency, false, true, SearchParam.NoofNights, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), SearchParam.CheckInDate, SearchParam.CheckOutDate, ref Finalprice, ref drRoom, isPerce, AgentPercentage, AgentMarkupAmount, commonSearch.SubAgentMarkup, commonSearch.B2CMarkup, commonSearch.IsParentId);
                                                        #endregion
                                                        drRoom["SupplierName"] = SupplierName;
                                                        drRoom["RoomTypeName"] = RoomName;
                                                        drRoom["RoomTypeId"] = roomtypeid;
                                                        dtPkFareRoom.Rows.Add(drRoom);
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
                                                        string LastCancellationDeadline = "";
                                                        DateTime lastdate = DateTime.Now.Date;
                                                        if (PkFareResponse.hotels.hotel[hotelIndex].room[roomIndex].rate[roomrateIndex].cancellationPolicy != null)
                                                        {
                                                            var sortedPolicies = PkFareResponse.hotels.hotel[hotelIndex].room[roomIndex].rate[roomrateIndex].cancellationPolicy.Where(p => p.amount != 0).OrderBy(p => p.from).ToList();

                                                            //double CancellationPrice = ItemPrice;///Convert.ToDouble(sortedPolicies[v].CancellationCharge);

                                                            for (int v = 0; v < sortedPolicies.Count(); v++)
                                                            {

                                                                DateTime dateString = DateTime.Now;//Convert.ToString(sortedPolicies[v].from);
                                                                if (sortedPolicies[v].from != null)
                                                                {
                                                                    dateString = Convert.ToDateTime(sortedPolicies[v].from);
                                                                }

                                                                if (v == 0)
                                                                {
                                                                    lastdate = dateString;
                                                                }
                                                                else
                                                                {
                                                                    if (lastdate > dateString)
                                                                    {
                                                                        lastdate = dateString;
                                                                    }
                                                                }
                                                            }




                                                            DateTime CancelDate = lastdate;//DateTime.ParseExact(lastdate, "dd/MM/yyyy", null);

                                                            if (CancelDate.AddDays(CutOffDays).Date > NowDate)
                                                            {
                                                                CancelDate = CancelDate.AddDays(CutOffDays).Date;
                                                            }
                                                            else
                                                            {
                                                                CancelDate = DateTime.Now.Date;
                                                            }
                                                            if (NowDate < CancelDate)
                                                            {
                                                                double charge = 0;
                                                                dtPkFareCancellationPolicy.Rows.Add(_commonMethod.setCancellationPolicyRow(ref charge, dtPkFareCancellationPolicy.NewRow(), CurrencyFactor, Currency, SupplierName, ownsystemid, "1", roomtypeid,
                                                                    RoomName, Meal, NowDate, CancelDate.AddDays(-1), 0, 0, "", VatOnPolicy, "", "", Convert.ToString(lastdate), remarks, "", ""));
                                                                dtPkFareCancellationPolicy.AcceptChanges();

                                                                dtPkFareCancellationPolicy.Rows.Add(_commonMethod.setCancellationPolicyRow(ref CompanyChargeAmount, dtPkFareCancellationPolicy.NewRow(), CurrencyFactor, Currency, SupplierName, ownsystemid, "1", roomtypeid,
                                                                    RoomName, Meal, CancelDate, ToDate, CancelCharge, CancelCharge, "", VatOnPolicy, "", "", Convert.ToString(lastdate), remarks, "", ""));
                                                                dtPkFareCancellationPolicy.AcceptChanges();
                                                            }
                                                            else
                                                            {
                                                                dtPkFareCancellationPolicy.Rows.Add(_commonMethod.setCancellationPolicyRow(ref CompanyChargeAmount, dtPkFareCancellationPolicy.NewRow(), CurrencyFactor, Currency, SupplierName, ownsystemid, "1", roomtypeid,
                                                                    RoomName, Meal, NowDate, ToDate, CancelCharge, CancelCharge, "", VatOnPolicy, "", "", Convert.ToString(lastdate), remarks, "", ""));
                                                                dtPkFareCancellationPolicy.AcceptChanges();
                                                            }



                                                        }
                                                        else
                                                        {

                                                            dtPkFareCancellationPolicy.Rows.Add(_commonMethod.setCancellationPolicyRow(ref CompanyChargeAmount, dtPkFareCancellationPolicy.NewRow(), CurrencyFactor, Currency, SupplierName, ownsystemid, "1", roomtypeid,
                                                                RoomName, Meal, NowDate, ToDate, CancelCharge, CancelCharge, "", VatOnPolicy, "", "", Convert.ToString(lastdate), remarks, "", ""));
                                                            dtPkFareCancellationPolicy.AcceptChanges();
                                                        }
                                                        #endregion

                                                    }
                                                }
                                            }
                                            #endregion
                                            decimal LowestPrice = Convert.ToDecimal(MinPrice).CalculatePrice(commonSearch.tokenData, commonSearch.advancedOptions, item.CurrencyCode);
                                            Options ops = (await _supplierRes.GetCommonRoomForSuppliers(GiataId, commonSearch.tokenData, commonSearch.advancedOptions, Convert.ToDouble(LowestPrice), drXmlHotel, dtPkFareRoom, dtPkFareCancellationPolicy, dtPkFareRoomRateBreakup, SearchParam));
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
                else
                {
                    item.ResponseStatus = 404;
                }

            }
            catch (IOException e)
            {
                item.ResponseStatus = 404;
                item.ResponseType = "NoCount";
                await _exception.Error(e);
            }
            finally
            {
                #region ResProcessTimer
                try
                {
                    timerroomx.Stop();
                    item.CodeProcessingTimeSupplier = timerroomx.ElapsedMilliseconds;
                    if (SearchParam != null && ResProccessTimerLogActive == "1" && timerroomx.ElapsedMilliseconds > Convert.ToInt32(ResProccessTimeValueforSaveLog))
                    {

                        SearchParam.SupplierTime += ",|Finish:Adonis Process Response  " + "|Time :" + timerroomx.Elapsed.ToString();
                        _ = Task.Run(() => _commonMethod.FireAway_SQL_ResPorcessTimeLog("", timerroomx.Elapsed.ToString(), item.Supplier, SearchParam.SearchKey, 1, SearchParam.AgentID));
                    }
                }
                catch (Exception x)
                {
                    await _exception.Error(x);
                }
                #endregion
            }
            //DtHotel.DefaultView.RowFilter = "FinalSellingPrice>0";


            return options;

        }

        public async Task<string> Get_PkFare_EInfo(string hotelcode, HotelSearchParameter SearchPara, string Password, string Action, string Username, string SearchKey, string UserID, string Provider, string URL)
        {
            string EssentialInfo = "";
            string Essential = "";
            string Error = "";
            try
            {

                #region Room Details
                string reqId = Convert.ToString(DateTime.Now.Ticks);

                string Request = URL + "1.0/hotels?languageCode=en_US&hotels=" + hotelcode;

                ResponseDto responseDto = await GetPkFareResponse("", UserID, Username, Password, Request, Error, "GET", "");

                string ResponseData = responseDto.Response;
                Error = responseDto.Error;
                //XMLRequestResponseTrack_HotelBeds(Request, ResponseData, Provider, "Proper", reqId, SearchPara.SearchKey, Convert.ToInt32(SearchPara.GiataId));
                // XMLRequestResponseTrack(Action, ResponseData, Provider, "Proper", reqId, SearchPara.SearchKey);
                try
                {
                    PkfInfoRes.Root PkFareInfoResponse = JsonConvert.DeserializeObject<PkfInfoRes.Root>(ResponseData);
                    Essential += "<b>Checkin From Time:</b>" + Convert.ToString(PkFareInfoResponse.hotels[0].checkin.beginTime);
                    Essential += "<br/><b>CheckOut To Time:</b>" + Convert.ToString(PkFareInfoResponse.hotels[0].checkout.time);
                    Essential += "<br/><b>Instructions:</b>" + Convert.ToString(PkFareInfoResponse.hotels[0].checkin.instructions);
                    Essential += "<br/><b>SpecialInstructions:</b>" + Convert.ToString(PkFareInfoResponse.hotels[0].checkin.specialInstructions);
                    Essential += "<br/><b>Policies:</b>" + Convert.ToString(PkFareInfoResponse.hotels[0].policies.knowBeforeYouGo);
                    Essential += "<br/><b>Mandatory Fees:</b>" + Convert.ToString(PkFareInfoResponse.hotels[0].fees.mandatory);
                    Essential += "<br/><b>Optional Fees:</b>" + Convert.ToString(PkFareInfoResponse.hotels[0].fees.optional);

                }
                catch (Exception e)
                {
                    _ = _exception.ErrorNew(e, ErrorLog._LogAPI.Recheck);
                }
                #endregion
                EssentialInfo = "<br/>" + Essential;
            }
            catch (Exception ex)
            {
                _ = _exception.ErrorNew(ex, ErrorLog._LogAPI.Recheck);
            }
            return EssentialInfo;
        }

        public async Task<ResponseDto> GetPkFareResponse(string strHost, string UserID, string UserName, string Password, string strxmlRQRS, string Error, string Method, string UniqueNo = "", bool from = false)
        {

            ResponseDto responseDto = new ResponseDto();


            System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
            timer.Start();
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls
                   | SecurityProtocolType.Tls11
                   | SecurityProtocolType.Tls12;


            string strOutput = "";
            try
            {
                HttpWebRequest Request;
                if (from)
                    Request = (HttpWebRequest)HttpWebRequest.Create(strHost);
                else
                    Request = (HttpWebRequest)HttpWebRequest.Create(strxmlRQRS);


                Request.Method = Method;
                Request.Accept = "application/json";
                Request.ContentType = "application/json; charset=utf-8";
                Request.Headers.Add("Accept-Encoding", "gzip,deflate");
                Request.Timeout = 120000;

                string signature = "";
                //long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                long timestamp = (long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
                string concatenatedString = Password + UserID + (timestamp / 1000);
                using (var sha = SHA256.Create())
                {
                    var computedHash = sha.ComputeHash(Encoding.UTF8.GetBytes(concatenatedString));
                    signature = BitConverter.ToString(computedHash).Replace("-", "");
                }
                //string authorizationHeader = "User=" + UserName + ",APIKey=" + Password + ",Signature=" + signature + ",Timestamp=" + timestamp + " ";
                // Request.Headers.Add("Authorization", authorizationHeader);
                Request.Headers.Add("apiKey", Password);
                Request.Headers.Add("signature", signature);
                Request.Headers.Add("timestamp", timestamp.ToString());
                Request.KeepAlive = true;

                if(from)
                {
                    Stream newStream = null;
                    byte[] data = Encoding.ASCII.GetBytes(strxmlRQRS);
                    Request.ContentLength = data.Length;
                    newStream = Request.GetRequestStream();
                    newStream.Write(data, 0, data.Length);
                }


                HttpWebResponse Response = (HttpWebResponse)await Request.GetResponseAsync();
                Stream responseStream = Response.GetResponseStream();
                if (Response.ContentEncoding.ToLower().Contains("gzip"))
                    responseStream = new GZipStream(responseStream, CompressionMode.Decompress);
                else if (Response.ContentEncoding.ToLower().Contains("deflate"))
                    responseStream = new DeflateStream(responseStream, CompressionMode.Decompress);
                StreamReader Reader = new StreamReader(responseStream, Encoding.Default);
                strOutput = await Reader.ReadToEndAsync();
                responseStream.Close();
                Reader.Close();
                Response.Close();


            }
            catch (Exception ex)
            {
                Error = ex.Message.ToString();


                _ = _exception.Error(ex);
                if (UniqueNo != "")
                    _commonMethod.ThirdPartyBookingHistory(Convert.ToString(UniqueNo), Convert.ToString(UniqueNo), strxmlRQRS, ex.Message.ToString(), "Error Block", "PkFare", "Response Confirm", "-", "withinearth");

            }

            responseDto.Response = strOutput;
            responseDto.Error = Error;
            return responseDto;
        }
        public async Task<PriceCheckCMDto> GetPkfarePriceCheckREQRES(DataSet dsCartDetails, DataRow[] PkFareDetails, int j, DataTable dtCheckPriceReqStructure, string UniqueNo, string AgentUniqueNo, double MainPrice, int UserCartId, double CompanyBuyingPrice, int AgentId, TokenData tokenData)
        {
            PriceCheckCMDto priceCheckDto = new PriceCheckCMDto();
            string time = "";
            string responsetype = "Success";
            string Error = "";
            string ResponseData = "";
            string SupplierName = "";
            string prebookRequest = "";

            int UserCartDetailId = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[j]["UserCartDetailId"]);
            string HotelId = Convert.ToString(dsCartDetails.Tables[1].DefaultView[0]["XMLHotelId"]);
            string RoomTypeId = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomTypeID"]);
            SupplierName = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["SupplierName"]).ToLower();

            try
            {
                prebookRequest = PkFarePriceCheckRequest(RoomTypeId, j, HotelId, dsCartDetails);

                if (prebookRequest != "")
                {
                    string Url = Convert.ToString(PkFareDetails[0]["WebSiteURL"]) + Convert.ToString("2.0/checkrates");
                    string UserID = Convert.ToString(PkFareDetails[0]["UserID"]);
                    string UserName = Convert.ToString(PkFareDetails[0]["UserName"]);
                    string Password = Convert.ToString(PkFareDetails[0]["Password"]);

                    System.Diagnostics.Stopwatch timerroomprocess = new System.Diagnostics.Stopwatch();
                    timerroomprocess.Start();

                    ResponseDto responseDto = await GetPkFareResponse(Url, UserID, UserName, Password, prebookRequest, Error, "POST", UniqueNo,true);
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
                            //int AgentId = Convert.ToInt32(Common.getSession("UserId") == "" ? "0" : Common.getSession("UserId"));
                            string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, prebookRequest, ResponseData, Error);
                        }
                        catch (Exception e)
                        {
                            _ = _exception.ErrorNew(e, ErrorLog._LogAPI.Prebook);
                        }
                        priceCheckDto.Flag = true;
                        return priceCheckDto;
                    }

                    PkfprbookRS.Root pkfareprebookresponse = JsonConvert.DeserializeObject<PkfprbookRS.Root>(ResponseData);

                    if (pkfareprebookresponse == null || pkfareprebookresponse.hotels.room == null)
                    {
                        try
                        {
                            responsetype = "GeneralError";
                            //int AgentId = Convert.ToInt32(Common.getSession("UserId") == "" ? "0" : Common.getSession("UserId"));
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
                            string purchaseToken = Convert.ToString(dsCartDetails.Tables[1].DefaultView[0]["purchaseToken"]);
                            if (pkfareprebookresponse.hotels.room != null)
                            {
                                for (int r = 0; r < pkfareprebookresponse.hotels.room[0].rate.Count; r++)
                                {
                                    DateTime lastdate = DateTime.Now.Date;
                                    if (pkfareprebookresponse.hotels.room[0].rate[r].cancellationPolicy != null)
                                    {
                                        var sortedPolicies = pkfareprebookresponse.hotels.room[0].rate[r].cancellationPolicy.Where(p => p.amount != 0).OrderBy(p => p.from).ToList();

                                        //double CancellationPrice = ItemPrice;///Convert.ToDouble(sortedPolicies[v].CancellationCharge);

                                        for (int v = 0; v < sortedPolicies.Count(); v++)
                                        {

                                            DateTime dateString = DateTime.Now;//Convert.ToString(sortedPolicies[v].from);
                                            if (sortedPolicies[v].from != null)
                                            {
                                                dateString = Convert.ToDateTime(sortedPolicies[v].from);
                                            }

                                            if (v == 0)
                                            {
                                                lastdate = dateString;
                                            }
                                            else
                                            {
                                                if (lastdate > dateString)
                                                {
                                                    lastdate = dateString;
                                                }
                                            }
                                        }
                                        if (Convert.ToDateTime(purchaseToken).Date != lastdate.Date)
                                        {
                                            try
                                            {
                                                responsetype = "GeneralError";
                                                ResponseData = "#Policy Changed " + "#Old Date = " + purchaseToken + " #New Date = " + lastdate + " #Response= " + ResponseData;
                                                //int AgentId = Convert.ToInt32(Common.getSession("UserId") == "" ? "0" : Common.getSession("UserId"));
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

                                    double Price = Convert.ToDouble(pkfareprebookresponse.hotels.room[0].rate[r].net);

                                    if (Price > 0)
                                    {

                                        string CurrencyCode = Convert.ToString(pkfareprebookresponse.hotels.currency);


                                        double CurrencyFactor = 1;
                                        if (CurrencyCode.ToLower() != tokenData.SiteCurrency.ToLower())
                                        {
                                            //DataTable dtInrCurrency = Common.GetCountryCurrency(CurrencyCode);
                                            //CurrencyFactor = Convert.ToDouble(dtInrCurrency.Rows[0]["AmtToExch"]);
                                            CurrencyFactor = _commonMethod.GetROEByCurrency(CurrencyCode, tokenData.dtROE);
                                        }

                                        MainPrice = Price * CurrencyFactor;

                                        priceCheckDto.ItemPrice = MainPrice;

                                        if (Math.Round(CompanyBuyingPrice, 2) != Math.Round(MainPrice, 2))
                                        {
                                            try
                                            {
                                                responsetype = "GeneralError";
                                                //int AgentId = Convert.ToInt32(Common.getSession("UserId") == "" ? "0" : Common.getSession("UserId"));
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
                                            //int AgentId = Convert.ToInt32(Common.getSession("UserId") == "" ? "0" : Common.getSession("UserId"));
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
                                //int AgentIdgta = Convert.ToInt32(Common.getSession("UserId") == "" ? "0" : Common.getSession("UserId"));
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
                    //int AgentId = Convert.ToInt32(Common.getSession("UserId") == "" ? "0" : Common.getSession("UserId"));
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
                //int AgentId = Convert.ToInt32(Common.getSession("UserId") == "" ? "0" : Common.getSession("UserId"));
                string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, prebookRequest, ResponseData, Error);
            }
            catch (Exception e)
            {
                _ = _exception.ErrorNew(e, ErrorLog._LogAPI.Prebook);
            }

            priceCheckDto.Flag = false;
            return priceCheckDto;

        }
        public string PkFarePriceCheckRequest(string roomtypeid, int j, string HotelId, DataSet dsCartDetails)
        {
            try
            {
                string[] resultdata = roomtypeid.Split(new string[] { "###" }, StringSplitOptions.None);
                string Roomid = resultdata[0];
                string rateKey = resultdata[1];





                //JsonSerializerSettings Setting = new JsonSerializerSettings
                //{
                //    NullValueHandling = NullValueHandling.Ignore,
                //    DefaultValueHandling = DefaultValueHandling.Ignore
                //};
                List<PkfprbookRQ.Room> rooms = new List<PkfprbookRQ.Room>();
                PkfprbookRQ.Room room = new PkfprbookRQ.Room();
                room.rateKey = rateKey;
                rooms.Add(room);

                PkfprbookRQ.Root RootObject = new PkfprbookRQ.Root();


                RootObject.upselling = false;
                RootObject.room = rooms;

                //string Request = JsonConvert.SerializeObject(RootObject, Setting);
                string Request = JsonConvert.SerializeObject(RootObject);
                return Request;

            }
            catch (Exception ex)
            {
                _ = _exception.ErrorNew(ex, ErrorLog._LogAPI.Prebook);
                return ex.ToString();
            }

        }

        public string PkFare_BookingRequest(DataTable dtPassengers, DataSet dsCartDetails, string RoomTypeId, ref string ReferenceNo, int i, int j)
        {
            try
            {
                string firstName = "";
                string lastName = "";
                string BookingREQ = "";
                //JsonSerializerSettings Setting = new JsonSerializerSettings
                //{
                //    NullValueHandling = NullValueHandling.Ignore,
                //    DefaultValueHandling = DefaultValueHandling.Ignore
                //};
                string[] resultdata = RoomTypeId.Split(new string[] { "###" }, StringSplitOptions.None);
                string Roomid = resultdata[0];
                string rateKey = resultdata[1];

                int UserCartDetailId = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[j]["UserCartDetailId"]);

                int NoOfRooms = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView.Count);

                ReferenceNo = ReferenceNo + "|" + UserCartDetailId;

                string remark = "", Holderfirstname = "", HolderLastname = "";
                if (dsCartDetails.Tables[0].Columns.Contains("SpecialRequest"))
                {
                    remark = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["SpecialRequest"]);
                    if (!string.IsNullOrEmpty(remark))
                    {
                        remark = remark.Replace("SpecialRequest:", "").Replace("$$", ",").Replace("/Remark:", ",");
                    }
                }
                List<PkfBookRQ.Paxis> custinfo = new List<PkfBookRQ.Paxis>();
                for (int r = 0; r < NoOfRooms; r++)
                {
                    if (dtPassengers.Rows.Count > 0)
                    {
                        dtPassengers.DefaultView.RowFilter = "PickUp='" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[r]["RoomTypeId"]) + "' and UserCartID=" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[r]["UserCartID"] + " and RoomNo='" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[r]["RoomNumber"]) + "'");

                        dtPassengers.DefaultView.Sort = "IsLead DESC";
                        int Islead = Convert.ToInt32(dtPassengers.DefaultView[0]["islead"]);
                        for (int k = 0; k < dtPassengers.DefaultView.Count; k++)
                        {
                            if (Islead == 1 || k == 0)
                            {
                                Holderfirstname = Convert.ToString(dtPassengers.DefaultView[0]["FirstName"]).Trim();
                                HolderLastname = Convert.ToString(dtPassengers.DefaultView[0]["LastName"]).Trim();
                            }
                            // else
                            {

                                PkfBookRQ.Paxis customer = new PkfBookRQ.Paxis();

                                firstName = Convert.ToString(dtPassengers.DefaultView[k]["FirstName"]).Trim();
                                lastName = Convert.ToString(dtPassengers.DefaultView[k]["LastName"]).Trim();
                                customer.roomId = Convert.ToInt32(dtPassengers.DefaultView[k]["RoomNo"]);
                                customer.name = firstName;
                                customer.surname = lastName;
                                if (Convert.ToString(dtPassengers.DefaultView[k]["PaxType"]).ToLower() == "child")
                                {
                                    customer.type = "CH";
                                }
                                else
                                {
                                    customer.type = "AD";
                                }
                                custinfo.Add(customer);


                            }
                        }
                    }
                }
                PkfBookRQ.Holder holder = new PkfBookRQ.Holder();
                holder.name = Holderfirstname;
                holder.surname = HolderLastname;
                List<PkfBookRQ.Room> Roomsinfo = new List<PkfBookRQ.Room>();
                PkfBookRQ.Room Roominfo = new PkfBookRQ.Room();
                Roominfo.rateKey = rateKey;
                Roominfo.paxes = custinfo;
                Roomsinfo.Add(Roominfo);
                PkfBookRQ.Root RootObject = new PkfBookRQ.Root();

                RootObject.holder = holder;
                RootObject.paymentData = null;
                RootObject.clientReference = ReferenceNo;
                RootObject.remark = string.IsNullOrWhiteSpace(remark) ? null : remark;
                RootObject.language = null;
                RootObject.room = Roomsinfo;
                // string Request = JsonConvert.SerializeObject(RootObject, Setting);
                string Request = JsonConvert.SerializeObject(RootObject);
                return Request;


            }
            catch (Exception ex)
            {
                _ =_exception.Error(ex);
                return "";
            }
        }

    }
}
