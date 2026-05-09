using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using XConnect.CommonUtilities;
using XConnect.CommonUtilities.Interface;
using XConnect.ErrorLogHandler.Interface;
using XConnect.Markup.Interface;
using XConnect.Models;
using XConnect.Models.Hotel;
using XConnect.Supplier.Letsfly.Interface;
using static Azure.Core.HttpHeader;
using Options = XConnect.Models.Options;

namespace XConnect.Supplier.Letsfly
{
    public class LetsflyRepo : ILetsfly
    {
        private readonly ICommonMethod _commonMethod;
        private readonly IException _exception;
        private readonly IMarkup _markup;
        private readonly ISupplierRes _supplierRes;
        private readonly KeySettings _keySettings;
        private readonly SupplierConfiguration _supplierConfig;//Added on 27-FEB-26



        public LetsflyRepo(ICommonMethod commonMethod, IMarkup markup, ISupplierRes supplierRes, IOptions<KeySettings> keySettings, IException exception, IOptions<SupplierConfiguration> supplierConfig)
        {
            _commonMethod = commonMethod;

            _markup = markup;
            _supplierRes = supplierRes;
            _keySettings = keySettings.Value;
            _exception = exception;
            _supplierConfig = supplierConfig.Value;
        }

        public void GetLetsflyRequestOnly_Async(DataRow[] commonCode, List<StarRating> listRating, DataTable dtPreferredDataHotel, HotelSearchParameter SearchPara, List<SupplierSearchConfig> lstLetsFlySearchConfig, List<SupplierDetails> lstLetsFlyDetails, ref ConcurrentBag<HotelX> AllTask, string AgentCode, int Hit, TokenData tokenData, DataSet dtXMLCity)
        {
            try
            {
                List<StarRating> lstRating = new List<StarRating>();
                lstRating = _commonMethod.GetStarRatingStringList_xtg(SearchPara);

                List<HotelCodeByCity> DtHotelDAta = HtmlExtensions.ToList<HotelCodeByCity>(commonCode.CopyToDataTable());
                int ratingcount = lstRating.Count;
                int hotelcount = DtHotelDAta.Count;

                foreach (SupplierDetails LetsFlyDetails in lstLetsFlyDetails)
                {
                    try
                    {
                        int SupplierID = LetsFlyDetails.SupplierID;
                        string Supplier = LetsFlyDetails.SupplierName;

                        if (lstLetsFlySearchConfig.Count > 0)
                        {
                            List<SupplierSearchConfig> LetsFlySearchConfig = lstLetsFlySearchConfig
                                                                            .Where(x => x != null && x.SupplierID == SupplierID)
                                                                            .ToList();

                            if (LetsFlySearchConfig != null && LetsFlySearchConfig.Count > 0)
                            {
                                string Ids = string.Empty;
                                string xmlbase = "";
                                _commonMethod.GetPrefferedHotelCodes(ref Ids, ref xmlbase, ref LetsFlySearchConfig, Supplier, dtPreferredDataHotel, SearchPara.IsSearchFor);

                                List<HotelCodeByCity> filter = new List<HotelCodeByCity>();
                                filter = _commonMethod.FilterWithOutCity_Code(ratingcount, hotelcount, DtHotelDAta, lstRating, Supplier);

                                if (filter.Count > 0)
                                {
                                    DataForSearch dataForSearch = _commonMethod.GetXmlDataForSearchTechwise(SearchPara.Nationality, dtXMLCity, Supplier, LetsFlyDetails.TechName);

                                    if (dataForSearch.XmlNationality != "")
                                    {
                                        double CurrencyFactor = 1;
                                        string Currency = Convert.ToString(LetsFlyDetails.CurrencyShortName);


                                        if (Currency.ToLower() != tokenData.SiteCurrency.ToLower())
                                        {
                                            CurrencyFactor = _commonMethod.GetROEByCurrency(Currency, tokenData.dtROE);// Convert.ToDouble(dtInrCurrency.Rows[0]["AmtToExch"]);
                                        }

                                        for (int k = 0; k < LetsFlySearchConfig.Count; k++)
                                        {
                                            bool DebugMode = (Convert.ToInt32(LetsFlySearchConfig[k].DebugMode) == 0 ? false : true);
                                            string UserName = Convert.ToString(LetsFlyDetails.UserName);
                                            string Password = Convert.ToString(LetsFlyDetails.Password);
                                            {
                                                {
                                                    int IsCitywisesearch = Convert.ToInt32(LetsFlySearchConfig[k].City);
                                                    if (IsCitywisesearch == 0)//(DtHotelData.DefaultView.Count > 0)
                                                    {
                                                        if (Convert.ToInt32(LetsFlySearchConfig[k].CityID) == 0 || Convert.ToInt32(LetsFlySearchConfig[k].CityID) == SearchPara.CityID)
                                                        {

                                                            {
                                                                int Noofrecords = Convert.ToInt32(LetsFlySearchConfig[k].Noofrecords);
                                                                double count = Convert.ToDouble(filter.Count);
                                                                int NoOfDiv = Convert.ToInt32(Math.Ceiling(count / Convert.ToDouble(Noofrecords)));
                                                                List<List<XMLID_Search>> lstxml = _commonMethod.GetXMLIDString(NoOfDiv, Noofrecords, count, filter);

                                                                for (int a = 0; a < NoOfDiv; a++)
                                                                {
                                                                    string IdsForSearch = Convert.ToString(_commonMethod.GetHotelCodeByCOmma(lstxml[a]));
                                                                    HotelX Xhotel = new HotelX();
                                                                    Xhotel.Supplier = Supplier;
                                                                    Xhotel.DebugeMode = DebugMode;
                                                                    Xhotel.SearchTimeOut = Convert.ToInt32(LetsFlySearchConfig[k].HotelSearchTimeOut);
                                                                    //if (Hit > 0)
                                                                    Xhotel.ProcessTimeout = Convert.ToInt32(LetsFlySearchConfig[k].HotelSearchTimeOut2);

                                                                    if (_keySettings.SupplierProxy == "0")
                                                                        Xhotel.URL = Convert.ToString(LetsFlyDetails.WebSiteURL) + _supplierConfig.LetsFly.LetsFly_HotelSearch;
                                                                    else
                                                                        Xhotel.URL = Convert.ToString(LetsFlyDetails.WebSiteURLforAPI) + _supplierConfig.LetsFly.LetsFly_HotelSearch;

                                                                    Xhotel.UserId = Convert.ToString(LetsFlyDetails.UserID);
                                                                    Xhotel.UserName = UserName;
                                                                    Xhotel.PassWord = Password;

                                                                    Xhotel.CurrencyCode = Currency;
                                                                    Xhotel.CurrencyFactor = CurrencyFactor;
                                                                    Xhotel.XmlCity = dataForSearch.xmlCity;
                                                                    Xhotel.XmlCountry = dataForSearch.xmlCountry;
                                                                    Xhotel.XmlNationality = dataForSearch.XmlNationality;
                                                                    Xhotel.AgentWiseMarkup = LetsFlyDetails.AgentWiseMarkup;
                                                                    Xhotel.IsShowSupplier = Convert.ToInt16(LetsFlyDetails.IsShowSupplier) == 1 ? true : false;
                                                                    Xhotel.SupplierID = LetsFlyDetails.SupplierID;
                                                                    Xhotel.MasterSupplierID = LetsFlyDetails.MasterSupplierId;
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
                                                                        objrq.Request = XMLLetsFlyRequestOnly_Async(SearchPara, dataForSearch.XmlNationality, Xhotel.SearchTimeOut, IdsForSearch);
                                                                        objrq.Response = ""; RQRS.Add(objrq);
                                                                    }
                                                                    Xhotel.RQRS = RQRS;
                                                                    #endregion
                                                                    Xhotel.ReturnRooms = LetsFlyDetails.ReturnRooms;
                                                                    Xhotel.TechName = LetsFlyDetails.TechName;
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

        public string XMLLetsFlyRequestOnly_Async(HotelSearchParameter SearchParam, string Nationality, Int32 TimeOut, string hotelid)
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
                int NoofChild1 = 0;
                //string ChildAge = "";
                List<letsflyHotelsearchRQ.Roomlist> Roomlists = new List<letsflyHotelsearchRQ.Roomlist>();

                List<int> AllChild = new List<int>();
                for (int j = 0; j < SearchParam.Rooms.Count; j++)
                {

                    letsflyHotelsearchRQ.Roomlist Roomlist = new letsflyHotelsearchRQ.Roomlist();
                    Roomlist.roomnum = (j + 1);
                    Roomlist.adults = SearchParam.Rooms[j].NoofAdults;
                    Roomlist.children = SearchParam.Rooms[j].NoofChild;
                    NoofAdult += SearchParam.Rooms[j].NoofAdults;
                    NoofChild += SearchParam.Rooms[j].NoofChild;
                    NoofChild1 = SearchParam.Rooms[j].NoofChild;
                    List<int> Child = new List<int>();

                    if (NoofChild1 > 0)
                    {
                        //if (SearchParam.Rooms[j].NoofChild == 1)
                        //{
                        //    Child.Add(Convert.ToInt32(SearchParam.Rooms[j].Child1Age));
                        //    AllChild.Add(Convert.ToInt32(SearchParam.Rooms[j].Child1Age));
                        //}
                        //else if (SearchParam.Rooms[j].NoofChild == 2)
                        //{
                        //    Child.Add(Convert.ToInt32(SearchParam.Rooms[j].Child1Age));
                        //    Child.Add(Convert.ToInt32(SearchParam.Rooms[j].Child2Age));
                        //    AllChild.Add(Convert.ToInt32(SearchParam.Rooms[j].Child1Age));
                        //    AllChild.Add(Convert.ToInt32(SearchParam.Rooms[j].Child2Age));
                        //}
                        switch (NoofChild1)
                        {
                            case 1:
                                Child.Add(Convert.ToInt32(SearchParam.Rooms[j].Child1Age));
                                AllChild.Add(Convert.ToInt32(SearchParam.Rooms[j].Child1Age));
                                break;

                            case 2:
                                Child.Add(Convert.ToInt32(SearchParam.Rooms[j].Child1Age));
                                Child.Add(Convert.ToInt32(SearchParam.Rooms[j].Child2Age));
                                AllChild.Add(Convert.ToInt32(SearchParam.Rooms[j].Child1Age));
                                AllChild.Add(Convert.ToInt32(SearchParam.Rooms[j].Child2Age));
                                break;


                        }
                        Roomlist.childrenages = Child;
                        //AllChild = Child;
                    }

                    Roomlists.Add(Roomlist);
                }

                letsflyHotelsearchRQ.Root RootObject = new letsflyHotelsearchRQ.Root();

                List<string> hoteids = new List<string>();

                string[] hotelIdsArray = hotelid.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var id in hotelIdsArray)
                {
                    string trimmedId = id.Trim();
                    if (!string.IsNullOrWhiteSpace(trimmedId))
                    {
                        hoteids.Add(trimmedId);
                    }
                }


                RootObject.hotelid = hoteids;
                RootObject.checkin = checkIn;
                RootObject.checkout = checkout;
                RootObject.roomcount = SearchParam.Rooms.Count;
                RootObject.adults = NoofAdult;
                RootObject.children = NoofChild;
                //if (NoofChild > 0)
                {
                    //RootObject.childrenages = Convert.ToString(Child);
                    RootObject.childrenages = string.Join(",", AllChild);
                }
                RootObject.is_precache = "0";
                RootObject.nationality = Nationality;
                RootObject.timeout = (TimeOut / 1000);
                RootObject.currency = "USD";
                RootObject.roomlist = Roomlists;
                // string Request = JsonConvert.SerializeObject(RootObject, Setting);
                string Request = Newtonsoft.Json.JsonConvert.SerializeObject(RootObject);
                return Request;

            }
            catch (Exception ex)
            {
                _ = _exception.Error(ex);
                return "";
            }
        }

        public async Task<List<Options>> Letsfly_HotelSearchWithRooms(HotelX item, CommonSearch commonSearch, string ResponseData, string WSurl, string Username, string Password, Int32 SearchTimeOut, HotelSearchParameter SearchParam, bool DebugMode, string Currency, double CurrencyFactor, DataSet dsAgentDetails, string Provider, DataTable dtCountryCityMarkup, Int32 AgentId, Int32 TariffID, double AgentPercentage, Boolean IsShowSupplier, int SupplierID, int MasterSupplierID, Int32 processTimeout, string ProccessCutOffTime, string ResProccessTimerLogActive, string ResProccessTimeValueforSaveLog, string StoreSupplierResErrorLog, DataTable dtROE, DataTable dtSuplierMarkup)
        {
            //return await Task.Run(() =>
            //{
            List<Options> options = new List<Options>();
            DataTable DtHotel = _commonMethod.GenerateDataTableNewV2();

            System.Diagnostics.Stopwatch timer_room_LetsFly = new System.Diagnostics.Stopwatch();
            timer_room_LetsFly.Start();
            try
            {
                if (ResponseData == null || ResponseData.Trim() == "")
                {
                    item.ResponseStatus = 204;
                    return options;
                }
                letsflysearchRS.Root letsflyResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<letsflysearchRS.Root>(ResponseData);


                if (letsflyResponse.hotellist == null)
                {
                    item.SupplierResponseError = ResponseData;
                    item.ResponseStatus = 204;
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
                if (letsflyResponse != null && letsflyResponse.hotellist != null)
                {
                    for (int hotelIndex = 0; hotelIndex < letsflyResponse.hotellist.Count; hotelIndex++)
                    {
                        item.ResponseType = "Success";

                        var hotel = letsflyResponse.hotellist[hotelIndex];
                        var Hotelname = hotel.hotelname;
                        string Hotelid = hotel.hotelid;

                        if (hotel.rooms == null || hotel.rooms.Count == 0)
                        {
                            continue;
                        }

                        double MinPrice = 0;

                        if (hotel.rooms != null)
                        {
                            for (int roomIndex = 0; roomIndex < hotel.rooms.Count; roomIndex++)
                            {
                                var room = hotel.rooms[roomIndex];

                                if (roomIndex == 0)
                                {
                                    Currency = Convert.ToString(hotel.rooms[roomIndex].currency);
                                    MinPrice = room.totalprice;
                                }
                                else
                                {
                                    if (MinPrice > room.totalprice)
                                    {
                                        MinPrice = room.totalprice;
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
                                drXmlHotel["UniqueNo"] = letsflyResponse.hotellist[hotelIndex].hotelid;
                                drXmlHotel["AvailToken"] = letsflyResponse.sessionid;
                                drXmlHotel["RaynaId"] = 0;
                                drXmlHotel["OwnsystemHotelId"] = 0;
                                //dr["Rating"] = Convert.ToString(drGiataDetails[0]["Rating"]);
                                drXmlHotel["Rating"] = 0;
                                drXmlHotel["IsRecomondedHotel"] = 0;
                                drXmlHotel["VatdiffAmount"] = 0;
                                drXmlHotel["XMLHotelCode"] = hotel.hotelid;
                                drXmlHotel["SupplierName"] = Provider;
                                drXmlHotel["SupplierCurrency"] = Currency;
                                drXmlHotel["XmlHotelCity"] = hotel.city;
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
                                drXmlHotel["UniqueNo"] = letsflyResponse.hotellist[hotelIndex].hotelid;
                                drXmlHotel["AvailToken"] = letsflyResponse.sessionid;
                                drXmlHotel["RaynaId"] = 0;
                                drXmlHotel["OwnsystemHotelId"] = 0;
                                //dr["Rating"] = Convert.ToString(drGiataDetails[0]["Rating"]);
                                drXmlHotel["Rating"] = 0;
                                drXmlHotel["IsRecomondedHotel"] = 0;
                                drXmlHotel["VatdiffAmount"] = 0;
                                drXmlHotel["XMLHotelCode"] = hotel.hotelid;
                                drXmlHotel["SupplierName"] = Provider;
                                drXmlHotel["SupplierCurrency"] = Currency;
                                drXmlHotel["XmlHotelCity"] = hotel.city;
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
                                        DataTable dtletsflyRoom = _commonMethod.GenerateRoomtypeDataTableNewV2();
                                        DataTable dtletsflyCancellationPolicy = _commonMethod.GenerateCancellationDataTableNewV2();
                                        DataTable dtletsflyRoomRateBreakup = _commonMethod.GenerateoomRateBreackUpDataTableNewV2();
                                        double AgentMarkupAmount = 0.0;
                                        Boolean isPerce = true;

                                        //_markup.getAgetMarkupSetting(Provider, GiataId.ToString(), commonSearch.dtSuplierMarkup, commonSearch.dtMarkupDetails, ref AgentPercentage, ref AgentMarkupAmount, ref blnIsShowSupplier, ref isPerce, commonSearch.AgentId, Rating, SearchParam.CityID, SearchParam.CountryID.ToString(), SearchParam.CheckInDate);

                                        _markup.getAgetMarkupSettingNew_new(Provider, Convert.ToString(RaynaID), commonSearch.dtSuplierMarkup, commonSearch.dsMarkupDetails, ref AgentPercentage, ref AgentMarkupAmount, ref blnIsShowSupplier, ref isPerce, commonSearch.AgentId, Rating, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), SearchParam.CheckInDate);

                                        for (int b = 0; b < Rooms; b++)
                                        {
                                            totalAdult = Convert.ToInt32(SearchParam.Rooms[b].NoofAdults) + totalAdult;
                                            totalChild = Convert.ToInt32(SearchParam.Rooms[b].NoofChild) + totalChild;
                                        }
                                        string remarks = "";
                                        if (letsflyResponse.hotellist != null)
                                        {
                                            string sessionid = Convert.ToString(letsflyResponse.sessionid);

                                            for (int roomIndex = 0; roomIndex < letsflyResponse.hotellist[hotelIndex].rooms.Count; roomIndex++)
                                            {

                                                // if (room != null)
                                                {
                                                    string RoomName = Convert.ToString(letsflyResponse.hotellist[hotelIndex].rooms[roomIndex].name);
                                                    remarks = Convert.ToString(letsflyResponse.hotellist[hotelIndex].rooms[roomIndex].remarks);
                                                    string roomid = Convert.ToString(letsflyResponse.hotellist[hotelIndex].rooms[roomIndex].id);

                                                    string roomtypeid = roomid + "###" + sessionid+"###"+item.XmlNationality;

                                                    double Finalprice = Convert.ToDouble(letsflyResponse.hotellist[hotelIndex].rooms[roomIndex].totalprice);
                                                    Currency = Convert.ToString(letsflyResponse.hotellist[hotelIndex].rooms[roomIndex].currency);

                                                    if (Currency.ToLower() != commonSearch.tokenData.SiteCurrency.ToLower())
                                                    {
                                                        CurrencyFactor = _commonMethod.GetROEByCurrency(Currency, commonSearch.tokenData.dtROE);
                                                    }

                                                    Finalprice = Finalprice * CurrencyFactor;
                                                    double CompanyBuyingPrice = Finalprice; //new

                                                    string Meal = "Room Only";
                                                    bool Meals = Convert.ToBoolean(letsflyResponse.hotellist[hotelIndex].rooms[roomIndex].meals.breakfastInclude);
                                                    if (Meals)
                                                    {
                                                        Meal = "Breakfast";
                                                    }
                                                    //Dictionary<string, bool> mealsDist = letsflyResponse.hotellist[0].rooms[roomIndex].meals;

                                                    //if (mealsDist.Count > 0)
                                                    //{
                                                    //    int i = 0;
                                                    //    foreach (var meal in mealsDist)
                                                    //    {
                                                    //        if (meal.Value)
                                                    //        {
                                                    //            i++;
                                                    //            if (i == 1)
                                                    //            {
                                                    //                Meal = meal.Key;
                                                    //            }
                                                    //            else
                                                    //            {
                                                    //                Meal = ", " + meal.Key;
                                                    //            }

                                                    //        }
                                                    //    }
                                                    //}
                                                    if (letsflyResponse.hotellist[hotelIndex].rooms[roomIndex].extra_fees != null)
                                                    {
                                                        for (int d = 0; d < letsflyResponse.hotellist[hotelIndex].rooms[roomIndex].extra_fees.Count; d++)
                                                        {
                                                            remarks += "<br/>INCLUDED :" + Convert.ToString(letsflyResponse.hotellist[hotelIndex].rooms[roomIndex].extra_fees[d].is_included);
                                                            remarks += "<br/>" + letsflyResponse.hotellist[hotelIndex].rooms[roomIndex].extra_fees[d].type + ": " + letsflyResponse.hotellist[hotelIndex].rooms[roomIndex].extra_fees[d].amount + " " + letsflyResponse.hotellist[hotelIndex].rooms[roomIndex].extra_fees[d].currency;
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
                                                    //    DataRow drRateBreakUp = dtletsflyRoomRateBreakup.NewRow();
                                                    //    drRateBreakUp["SupplierName"] = Provider;
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

                                                    //    dtletsflyRoomRateBreakup.Rows.Add(drRateBreakUp);
                                                    //}
                                                    #endregion

                                                    #region Rooms
                                                    DataRow drRoom = dtletsflyRoom.NewRow();
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
                                                    drRoom["SupplierName"] = Provider;
                                                    drRoom["RoomTypeName"] = RoomName;
                                                    drRoom["RoomTypeId"] = roomtypeid;
                                                    dtletsflyRoom.Rows.Add(drRoom);
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
                                                    if (letsflyResponse.hotellist[hotelIndex].rooms[roomIndex].cancellation != null)
                                                    {
                                                        string cancellationType = letsflyResponse.hotellist[hotelIndex].rooms[roomIndex].cancellation.cancellationType;
                                                        Cancellationdate = letsflyResponse.hotellist[hotelIndex].rooms[roomIndex].cancellation.cancellationWindowDate;

                                                        if (cancellationType == "1" && !string.IsNullOrEmpty(Cancellationdate))
                                                        {
                                                            //DateTime CancelDate = DateTime.ParseExact(Cancellationdate.Trim(), "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None);
                                                            //Cancellationdate = Cancellationdate.Substring(0, 2) + "/" + Cancellationdate.Substring(3, 2) + "/" + Cancellationdate.Substring(6, 4);
                                                            Cancellationdate = Cancellationdate.Substring(8, 2) + "/" + Cancellationdate.Substring(5, 2) + "/" + Cancellationdate.Substring(0, 4);

                                                            DateTime CancelDate = DateTime.ParseExact(Cancellationdate, "dd/MM/yyyy", CultureInfo.InvariantCulture).Date;


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
                                                                dtletsflyCancellationPolicy.Rows.Add(_commonMethod.setCancellationPolicyRow(ref charge, dtletsflyCancellationPolicy.NewRow(), CurrencyFactor, Currency, Provider, ownsystemid, "1", roomtypeid,
                                                                    RoomName, Meal, NowDate, CancelDate.AddDays(-1), 0, 0, "", VatOnPolicy, "", "", Cancellationdate, remarks, "", ""));
                                                                dtletsflyCancellationPolicy.AcceptChanges();

                                                                dtletsflyCancellationPolicy.Rows.Add(_commonMethod.setCancellationPolicyRow(ref CompanyChargeAmount, dtletsflyCancellationPolicy.NewRow(), CurrencyFactor, Currency, Provider, ownsystemid, "1", roomtypeid,
                                                                    RoomName, Meal, CancelDate, ToDate, CancelCharge, CancelCharge, "", VatOnPolicy, "", "", Cancellationdate, remarks, "", ""));
                                                                dtletsflyCancellationPolicy.AcceptChanges();
                                                            }
                                                            else
                                                            {
                                                                dtletsflyCancellationPolicy.Rows.Add(_commonMethod.setCancellationPolicyRow(ref CompanyChargeAmount, dtletsflyCancellationPolicy.NewRow(), CurrencyFactor, Currency, Provider, ownsystemid, "1", roomtypeid,
                                                                    RoomName, Meal, NowDate, ToDate, CancelCharge, CancelCharge, "", VatOnPolicy, "", "", Cancellationdate, remarks, "", ""));
                                                                dtletsflyCancellationPolicy.AcceptChanges();
                                                            }
                                                        }
                                                        else
                                                        {

                                                            dtletsflyCancellationPolicy.Rows.Add(_commonMethod.setCancellationPolicyRow(ref CompanyChargeAmount, dtletsflyCancellationPolicy.NewRow(), CurrencyFactor, Currency, Provider, ownsystemid, "1", roomtypeid,
                                                                RoomName, Meal, NowDate, ToDate, CancelCharge, CancelCharge, "", VatOnPolicy, "", "", Cancellationdate, remarks, "", ""));
                                                            dtletsflyCancellationPolicy.AcceptChanges();
                                                        }

                                                    }
                                                    else
                                                    {

                                                        dtletsflyCancellationPolicy.Rows.Add(_commonMethod.setCancellationPolicyRow(ref CompanyChargeAmount, dtletsflyCancellationPolicy.NewRow(), CurrencyFactor, Currency, Provider, ownsystemid, "1", roomtypeid,
                                                            RoomName, Meal, NowDate, ToDate, CancelCharge, CancelCharge, "", VatOnPolicy, "", "", Cancellationdate, remarks, "", ""));
                                                        dtletsflyCancellationPolicy.AcceptChanges();
                                                    }
                                                    #endregion

                                                }
                                            }
                                        }
                                        #endregion
                                        decimal LowestPrice = Convert.ToDecimal(MinPrice).CalculatePrice(commonSearch.tokenData, commonSearch.advancedOptions, Currency);
                                        Options ops = (await _supplierRes.GetCommonRoomForSuppliers(GiataId, commonSearch.tokenData, commonSearch.advancedOptions, Convert.ToDouble(LowestPrice), drXmlHotel, dtletsflyRoom, dtletsflyCancellationPolicy, dtletsflyRoomRateBreakup, SearchParam));
                                        if (ops != null)
                                            options.Add(ops);
                                    }
                                    #endregion
                                }
                            }
                        }

                    }
                }
                else
                {
                    item.ResponseStatus = 204;
                }
            }
            catch (Exception e)
            { 
                item.ResponseStatus = 404;
                _ = _exception.Error(e);
                return options;
            }
            finally
            {
                #region ResProcessTimer
                try
                {
                    timer_room_LetsFly.Stop();
                    item.CodeProcessingTimeSupplier = timer_room_LetsFly.ElapsedMilliseconds;
                    if (SearchParam != null && ResProccessTimerLogActive == "1" && timer_room_LetsFly.ElapsedMilliseconds > Convert.ToInt32(ResProccessTimeValueforSaveLog))
                    {
                        
                        SearchParam.SupplierTime += ",|Finish:Agoda Process Response  " + "|Time :" + timer_room_LetsFly.Elapsed.ToString();
                        _ = Task.Run(() => _commonMethod.FireAway_SQL_ResPorcessTimeLog("", timer_room_LetsFly.Elapsed.ToString(), Provider, SearchParam.SearchKey, 1, SearchParam.AgentID));
                    }
                }
                catch (Exception x)
                {
                    _ = _exception.Error(x);
                }
                #endregion

                if (item.ResponseType == "NoCount" && StoreSupplierResErrorLog == "1")
                    _ = _commonMethod.XMLRequestResponseTrack_Error(item.RQRS[0].Request, ResponseData, item.Supplier, timer_room_LetsFly.Elapsed.ToString(), "", SearchParam.SearchKey);
            }
            //DtHotel.DefaultView.RowFilter = "FinalSellingPrice>0";
            //DtHotel.DefaultView.RowFilter = "Rating IN(" + strRating + ")";
            return options;
            //return DtHotel;

        }

        public async Task<PriceCheckCMDto> GetLetsflyPriceCheckREQRES(DataSet dsCartDetails, DataRow[] letsflyDetails, int j, DataTable dtCheckPriceReqStructure, string UniqueNo, string AgentUniqueNo, double MainPrice, int UserCartId, double CompanyBuyingPrice, int AgentId,TokenData tokenData)
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
            string SupplierName = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["SupplierName"]).ToLower();
            //260326
            string[] RoomTypeId1 = RoomTypeId.Split(new string[] { "###" }, StringSplitOptions.None);
            string Nationality = RoomTypeId1[2];
            //
            try
            {
                prebookRequest = LetsFlyPriceCheckRequest(RoomTypeId, j, HotelId, dsCartDetails);
                _commonMethod.ThirdPartyBookingHistory(Convert.ToString(UserCartDetailId), Convert.ToString(UniqueNo), prebookRequest, "", "Before Hit Price Check Response", SupplierName, "Price Check Request Confirm", "-", "withinearth");

                if (prebookRequest != "")
                {
                    //string Url = Convert.ToString(letsflyDetails[0]["WebSiteURL"]) + LetsflyInfo.LetsFly_PrebookURL;
                    string Url = Convert.ToString(letsflyDetails[0]["WebSiteURL"]) + _supplierConfig.LetsFly.LetsFly_PrebookURL;
                    string UserID = Convert.ToString(letsflyDetails[0]["UserID"]);
                    string UserName = Convert.ToString(letsflyDetails[0]["UserName"]);
                    string Password = Convert.ToString(letsflyDetails[0]["Password"]);

                    System.Diagnostics.Stopwatch timerroomprocess = new System.Diagnostics.Stopwatch();
                    timerroomprocess.Start();

                    ResponseDto responseDto = await GetletsflyResponse(Url, UserID, UserName, Password, prebookRequest, Error, UniqueNo);
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
                            _ =_exception.ErrorNew(e, ErrorLog._LogAPI.Prebook);
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
                                        string hotelid = Convert.ToString(letsflyprebookresponse.hotelid);
                                        string roomtypeid = roomid + "$$$" + sessionid + "$$$" + Price + "$$$" + roomname + "$$$" + hotelid+"$$$"+ Nationality; //260326
                                        //string roomtypeid = roomid + "$$$" + sessionid + "$$$" + Price + "$$$" + roomname;

                                        string CurrencyCode = Convert.ToString(letsflyprebookresponse.rooms[r].currency);

                                        //objdata.ShoppingCartDetails[0].PurchaseToken = roomtypeid;
                                       // bool IsSuccess = await _commonMethod.GetRoomNameUpdate(roomtypeid, UserCartId);

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
        public async Task<PriceCheckCMDto> GetLetsflyPriceCheckREQRES_newversion(Shopping_CartDataForHotelXML objdata, DataSet dsCartDetails, DataRow[] letsflyDetails, int j, DataTable dtCheckPriceReqStructure, string UniqueNo, string AgentUniqueNo, double MainPrice, int UserCartId, double CompanyBuyingPrice, int AgentId, TokenData tokenData, string Searchkey,string TechName="")
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
            string SupplierName = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["SupplierName"]).ToLower();
            //260326
            string[] RoomTypeId1 = RoomTypeId.Split(new string[] { "###" }, StringSplitOptions.None);
            string Nationality = RoomTypeId1[2];
            //
            try
            {
                prebookRequest = LetsFlyPriceCheckRequest(RoomTypeId, j, HotelId, dsCartDetails);
                _commonMethod.ThirdPartyBookingHistory(Convert.ToString(UserCartDetailId), Convert.ToString(UniqueNo), prebookRequest, "", "Before Hit Price Check Response", SupplierName, "Price Check Request Confirm", "-", "withinearth",Searchkey);

                if (prebookRequest != "")
                {
                   // string Url = Convert.ToString(letsflyDetails[0]["WebSiteURL"]) + LetsflyInfo.LetsFly_PrebookURL;
                    string Url = Convert.ToString(letsflyDetails[0]["WebSiteURL"]) + _supplierConfig.LetsFly.LetsFly_PrebookURL;
                    string UserID = Convert.ToString(letsflyDetails[0]["UserID"]);
                    string UserName = Convert.ToString(letsflyDetails[0]["UserName"]);
                    string Password = Convert.ToString(letsflyDetails[0]["Password"]);

                    System.Diagnostics.Stopwatch timerroomprocess = new System.Diagnostics.Stopwatch();
                    timerroomprocess.Start();

                    ResponseDto responseDto = await GetletsflyResponse(Url, UserID, UserName, Password, prebookRequest, Error, UniqueNo);
                    ResponseData = responseDto.Response;
                    Error = responseDto.Error;

                    timerroomprocess.Stop();
                    time = timerroomprocess.Elapsed.ToString();

                    _commonMethod.ThirdPartyBookingHistory(Convert.ToString(UserCartDetailId), Convert.ToString(UniqueNo), prebookRequest, ResponseData + Error, "After Hit Price Check Response", SupplierName, "Price Check Response Confirm", "-", "withinearth",Searchkey);

                    if (string.IsNullOrEmpty(ResponseData) || ResponseData.Contains("Error"))
                    {
                        try
                        {
                            responsetype = "GeneralError";
                            string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, prebookRequest, ResponseData, Error,Searchkey,TechName);
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
                            string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, prebookRequest, ResponseData, Error, Searchkey, TechName);
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
                                        string hotelid = Convert.ToString(letsflyprebookresponse.hotelid);
                                        string roomtypeid = roomid + "$$$" + sessionid + "$$$" + Price + "$$$" + roomname + "$$$" + hotelid + "$$$" + Nationality; //260326
                                        //string roomtypeid = roomid + "$$$" + sessionid + "$$$" + Price + "$$$" + roomname;

                                        string CurrencyCode = Convert.ToString(letsflyprebookresponse.rooms[r].currency);

                                        objdata.ShoppingCartDetails[r].PurchaseToken = roomtypeid;
                                        // bool IsSuccess = await _commonMethod.GetRoomNameUpdate(roomtypeid, UserCartId);

                                        double CurrencyFactor = 1;
                                        if (Convert.ToString(CurrencyCode).ToLower() != tokenData.SiteCurrency.ToLower())
                                        {

                                            try
                                            {
                                                responsetype = "GeneralError";
                                                string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, prebookRequest, ResponseData, Error, Searchkey, TechName);
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
                                                string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, prebookRequest, ResponseData, Error, Searchkey, TechName);
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
                                            string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, prebookRequest, ResponseData, Error, Searchkey, TechName);
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
                                string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, prebookRequest, ResponseData, Error, Searchkey, TechName);
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
                    string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, prebookRequest, ResponseData, Error, Searchkey, TechName);
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
                string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, prebookRequest, ResponseData, Error, Searchkey, TechName);
            }
            catch (Exception e)
            {
                _ = _exception.ErrorNew(e, ErrorLog._LogAPI.Prebook);

            }

            priceCheckDto.Flag = false;
            return priceCheckDto;
        }
        public string LetsFlyPriceCheckRequest(string roomtypeid, int j, string HotelId, DataSet dsCartDetails)
        {
            try
            {
                string[] resultdata = roomtypeid.Split(new string[] { "###" }, StringSplitOptions.None);
                string Roomid = resultdata[0];
                string sessionid = resultdata[1];


                string CheckIn = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["CheckInDate"]);//"20180327"
                CheckIn = CheckIn.Substring(0, 4) + "-" + CheckIn.Substring(4, 2) + "-" + CheckIn.Substring(6, 2);
                string CheckOut = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["CheckOutDate"]);
                CheckOut = CheckOut.Substring(0, 4) + "-" + CheckOut.Substring(4, 2) + "-" + CheckOut.Substring(6, 2);
                int NoOfRooms = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView.Count);
                int NoOfAdults = 0;
                int NoOfChild = 0;
                int NoOfChild1 = 0;
                string ChildAge = "";


                //JsonSerializerSettings Setting = new JsonSerializerSettings
                //{
                //    NullValueHandling = NullValueHandling.Ignore,
                //    DefaultValueHandling = DefaultValueHandling.Ignore
                //};

                List<letsflyPreeBookRQ.Roomlist> Roomlists = new List<letsflyPreeBookRQ.Roomlist>();

                List<int> AllChild = new List<int>();
                for (j = 0; j < dsCartDetails.Tables[1].DefaultView.Count; j++)
                {

                    letsflyPreeBookRQ.Roomlist Roomlist = new letsflyPreeBookRQ.Roomlist();
                    Roomlist.roomnum = (j + 1);
                    Roomlist.adults = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[j]["NoOfAdults"]);
                    Roomlist.children = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[j]["NoOfChild"]);
                    NoOfAdults += Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[j]["NoOfAdults"]);
                    NoOfChild += Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[j]["NoOfChild"]);
                    NoOfChild1 = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[j]["NoOfChild"]);
                    List<int> Child = new List<int>();
                    if (NoOfChild1 > 0)
                    {
                        //if (Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[j]["NoOfChild"]) == 1)
                        //{
                        //    Child.Add(Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[j]["ChildAge1"]));
                        //    //Roomdetails.children.Add(Convert.ToInt32(drUserCartCount[i]["ChildAge1"]));
                        //}
                        //else if (Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[j]["NoOfChild"]) == 2)
                        //{
                        //    Child.Add(Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[j]["ChildAge1"]));
                        //    Child.Add(Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[j]["ChildAge2"]));
                        //}
                        switch (NoOfChild1)
                        {
                            case 1:
                                Child.Add(Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[j]["ChildAge1"]));
                                AllChild.Add(Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[j]["ChildAge1"]));
                                break;

                            case 2:
                                Child.Add(Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[j]["ChildAge1"]));
                                Child.Add(Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[j]["ChildAge2"]));
                                AllChild.Add(Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[j]["ChildAge1"]));
                                AllChild.Add(Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[j]["ChildAge2"]));
                                break;


                        }
                    }
                    //NoOfChild1 = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[j]["NoOfChild"]);
                    //string childAgeData = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["ChildAge"]);

                    //List<int> Child = new List<int>();

                    //if (NoOfChild1 > 0 && !string.IsNullOrEmpty(childAgeData))
                    //{
                    //    var childAges = childAgeData
                    //        .Split(',')
                    //        .Where(x => int.TryParse(x.Trim(), out _))
                    //        .Select(x => Convert.ToInt32(x.Trim()))
                    //        .ToList();

                    //    Child.AddRange(childAges);
                    //    AllChild.AddRange(childAges);
                    //}
                    Roomlist.childrenages = Child;
                    Roomlists.Add(Roomlist);
                }

                letsflyPreeBookRQ.Root RootObject = new letsflyPreeBookRQ.Root();


                RootObject.hotelid = HotelId;
                RootObject.checkin = CheckIn;
                RootObject.checkout = CheckOut;
                RootObject.roomcount = NoOfRooms;
                RootObject.adults = NoOfAdults;
                RootObject.children = NoOfChild;
                // RootObject.childrenages = Convert.ToString(Child);
                RootObject.childrenages = string.Join(",", AllChild); ;
                RootObject.roomtypeid = Roomid;
                RootObject.sessionid = sessionid;
                RootObject.roomlist = Roomlists;
                //string Request = JsonConvert.SerializeObject(RootObject, Setting);
                string Request = Newtonsoft.Json.JsonConvert.SerializeObject(RootObject);
                return Request;

            }
            catch (Exception ex)
            {
                _ = _exception.ErrorNew(ex, ErrorLog._LogAPI.Prebook);
                return ex.ToString();
            }

        }

        public async Task<ResponseDto> GetletsflyResponse(string strHost, string UserID, string UserName, string Password, string strxmlRQRS, string Error, string UniqueNo = "")
        {
            ResponseDto responseDto = new ResponseDto();
            System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
            timer.Start();
            string strOutput = "";
            try
            {
                HttpWebRequest Request;
                Request = (HttpWebRequest)HttpWebRequest.Create(strHost);
                Request.Method = "POST";
                Request.Accept = "application/json";
                Request.ContentType = "application/json; charset=utf-8";
                Request.Headers.Add("Accept-Encoding", "gzip,deflate");
                Request.Timeout = 120000;

                string signature = "";
                long timestamp = (long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds / 1000;
                string concatenatedString = Password + UserID + timestamp;
                using (var sha = SHA256.Create())
                {
                    var computedHash = sha.ComputeHash(Encoding.UTF8.GetBytes(concatenatedString));
                    signature = BitConverter.ToString(computedHash).Replace("-", "");
                }
                string authorizationHeader = "User=" + UserName + ",APIKey=" + Password + ",Signature=" + signature + ",Timestamp=" + timestamp + " ";
                Request.Headers.Add("Authorization", authorizationHeader);
                Request.KeepAlive = true;

                Stream newStream = null;
                byte[] data = Encoding.ASCII.GetBytes(strxmlRQRS);
                Request.ContentLength = data.Length;
                newStream = Request.GetRequestStream();
                newStream.Write(data, 0, data.Length);
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
                _ = _exception.ErrorNew(ex,ErrorLog._LogAPI.Prebook);
                if (UniqueNo != "")
                    _commonMethod.ThirdPartyBookingHistory(Convert.ToString(UniqueNo), Convert.ToString(UniqueNo), strxmlRQRS, ex.Message.ToString(), "Error Block", "letsfly", "Response Confirm", "-", "withinearth");

                responseDto.Error = Error;

            }
            responseDto.Response = strOutput;
            return responseDto;
        }

        public string Letsfly_BookingRequest(DataTable dtPassengers, DataSet dsCartDetails, string RoomTypeId, string sessionid, string Rate, ref string ReferenceNo, int i, int j, string roomname)
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
                string letsflyHotelId = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["XMLHotelId"]);
                int UserCartDetailId = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[j]["UserCartDetailId"]);
                string checkindate = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["CheckInDate"]);
                string checkin = checkindate.Substring(0, 4) + "-" + checkindate.Substring(4, 2) + "-" + checkindate.Substring(6, 2);

                string checkoutdate = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["CheckOutDate"]);
                string checkout = checkoutdate.Substring(0, 4) + "-" + checkoutdate.Substring(4, 2) + "-" + checkoutdate.Substring(6, 2);

                int NoOfRooms = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView.Count);
                string XmlNationality = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["XmlNationality"]);

                int NoOfAdults = 0;
                int NoOfChild = 0;
                string ChildAge = "";
                ReferenceNo = ReferenceNo + "|" + UserCartDetailId;
                List<int> Child = new List<int>();
                for (int k = 0; k < NoOfRooms; k++)
                {
                    NoOfAdults += Convert.ToInt32(dsCartDetails.Tables[1].Rows[k]["NoOfAdults"]);
                    NoOfChild += Convert.ToInt32(dsCartDetails.Tables[1].Rows[k]["NoOfChild"]);

                    if (NoOfChild > 0)
                    {
                        if (Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[j]["NoOfChild"]) == 1)
                        {
                            Child.Add(Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[j]["ChildAge1"]));

                        }
                        else if (Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[j]["NoOfChild"]) == 2)
                        {
                            Child.Add(Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[j]["ChildAge1"]));
                            Child.Add(Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[j]["ChildAge2"]));
                        }
                    }
                }


                string remark = "";
                if (dsCartDetails.Tables[0].Columns.Contains("SpecialRequest"))
                {
                    remark = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["SpecialRequest"]);
                    if (!string.IsNullOrEmpty(remark))
                    {
                        remark = remark.Replace("SpecialRequest:", "").Replace("$$", ",").Replace("/Remark:", ",");
                    }
                }
                letsflybookingRQ.Root RootObject = new letsflybookingRQ.Root();

                RootObject.hotelid = letsflyHotelId;
                RootObject.checkin = checkin;
                RootObject.checkout = checkout;
                RootObject.roomcount = NoOfRooms;
                RootObject.adults = NoOfAdults;
                RootObject.children = NoOfChild;
                RootObject.childrenages = string.Join(",", Child);
                RootObject.roomtypeid = RoomTypeId;
                RootObject.sessionid = sessionid;
                RootObject.orderno = ReferenceNo;
                RootObject.roomname = roomname;
                RootObject.totalprice = Convert.ToDouble(Rate);
                RootObject.contactname = Convert.ToString(_supplierConfig.LetsFly.LetsFly_contactname);
                RootObject.contactemail = Convert.ToString(_supplierConfig.LetsFly.LetsFly_contactemail);
                RootObject.contactphone = Convert.ToString(_supplierConfig.LetsFly.LetsFly_contactphone);
                RootObject.special_request = Convert.ToString(remark);
                List<letsflybookingRQ.Customerinfo> custinfo = new List<letsflybookingRQ.Customerinfo>();
                for (int r = 0; r < NoOfRooms; r++)
                {
                    if (dtPassengers.Rows.Count > 0)
                    {
                        dtPassengers.DefaultView.RowFilter = "PickUp='" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomTypeId"]) + "' and UserCartID=" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["UserCartID"] + " and RoomNo='" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomNumber"]) + "'");

                        dtPassengers.DefaultView.Sort = "IsLead DESC";
                        int Islead = Convert.ToInt32(dtPassengers.DefaultView[0]["islead"]);

                        if (dtPassengers.DefaultView.Count > 0)
                        {
                            for (int k = 0; k < dtPassengers.DefaultView.Count; k++)
                            {
                                letsflybookingRQ.Customerinfo customer = new letsflybookingRQ.Customerinfo();

                                firstName = Convert.ToString(dtPassengers.DefaultView[k]["FirstName"]).Trim();
                                lastName = Convert.ToString(dtPassengers.DefaultView[k]["LastName"]).Trim();
                                customer.roomnum = Convert.ToInt32(dtPassengers.DefaultView[k]["RoomNo"]);
                                customer.firstname = firstName;
                                customer.lastname = lastName;
                                customer.nationality = XmlNationality;
                                custinfo.Add(customer);


                            }
                        }
                    }
                }
                RootObject.customerinfo = custinfo;
                // string Request = JsonConvert.SerializeObject(RootObject, Setting);
                string Request = Newtonsoft.Json.JsonConvert.SerializeObject(RootObject);
                return Request;


            }
            catch (Exception ex)
            {
                _ =_exception.Error(ex);
                return "";
            }
        }
        public string Letsfly_BookingRequest_newversion(DataTable dtPassengers, BookRequest_newversion bookRequest, string RoomTypeId, string sessionid, string Rate, ref string ReferenceNo, int i, int j, string roomname,string hotelid,string nationality)
        {
            try
            {
                string firstName = "";
                string lastName = "";
                string BookingREQ = "";
               
                int UserCartDetailId = Convert.ToInt32(bookRequest.userCart.UserCartDetails[0].UserCartDetailId);

                string CheckIn = Convert.ToString(bookRequest.userCart.UserCartDetails[0].CheckInDate);
                //CheckIn = CheckIn.Substring(0, 4) + "-" + CheckIn.Substring(4, 2) + "-" + CheckIn.Substring(6, 2);
                string CheckOut = Convert.ToString(bookRequest.userCart.UserCartDetails[0].CheckOutDate);
                //CheckOut = CheckOut.Substring(0, 4) + "-" + CheckOut.Substring(4, 2) + "-" + CheckOut.Substring(6, 2);

                // Parse the strings to DateTime
                DateTime checkInDate = DateTime.Parse(CheckIn);
                DateTime checkOutDate = DateTime.Parse(CheckOut);

                // Convert to "yyyy-MM-dd" format
                string checkin = checkInDate.ToString("yyyy-MM-dd");
                string checkout = checkOutDate.ToString("yyyy-MM-dd");

                int NoOfRooms = Convert.ToInt32(bookRequest.userCart.UserCartDetails.Count);
                //string XmlNationality = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["XmlNationality"]);

                int NoOfAdults = 0;
                int NoOfChild = 0;
                string ChildAge = "";
                ReferenceNo = ReferenceNo + "|" + UserCartDetailId;
                List<int> Child = new List<int>();
                for (int k = 0; k < NoOfRooms; k++)
                {
                    NoOfAdults += Convert.ToInt32(bookRequest.userCart.UserCartDetails[k].NoofAdult);
                    NoOfChild += Convert.ToInt32(bookRequest.userCart.UserCartDetails[k].NoofChild);

                    if (NoOfChild > 0)
                    {
                        if (Convert.ToInt32(bookRequest.userCart.UserCartDetails[k].NoofChild) == 1)
                        {
                            Child.Add(Convert.ToInt32(bookRequest.userCart.UserCartDetails[k].ChildAge));

                        }
                        else if (Convert.ToInt32(bookRequest.userCart.UserCartDetails[k].NoofChild) == 2)
                        {
                            Child.Add(Convert.ToInt32(bookRequest.userCart.UserCartDetails[k].ChildAge));
                            Child.Add(Convert.ToInt32(bookRequest.userCart.UserCartDetails[k].ChildAge));
                        }
                    }
                }

                


                string remark = "";
                //if (dsCartDetails.Tables[0].Columns.Contains("SpecialRequest"))
                //{
                //    remark = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["SpecialRequest"]);
                //    if (!string.IsNullOrEmpty(remark))
                //    {
                //        remark = remark.Replace("SpecialRequest:", "").Replace("$$", ",").Replace("/Remark:", ",");
                //    }
                //}
                letsflybookingRQ.Root RootObject = new letsflybookingRQ.Root();

                RootObject.hotelid = hotelid;
                RootObject.checkin = checkin;
                RootObject.checkout = checkout;
                RootObject.roomcount = NoOfRooms;
                RootObject.adults = NoOfAdults;
                RootObject.children = NoOfChild;
                RootObject.childrenages = string.Join(",", Child);
                RootObject.roomtypeid = RoomTypeId;
                RootObject.sessionid = sessionid;
                RootObject.orderno = ReferenceNo;
                RootObject.roomname = roomname;
                RootObject.totalprice = Convert.ToDouble(Rate);
                //RootObject.contactname = Convert.ToString(LetsflyInfo.LetsFly_contactname);
                RootObject.contactname = Convert.ToString(_supplierConfig.LetsFly.LetsFly_contactname);
                RootObject.contactemail = Convert.ToString(_supplierConfig.LetsFly.LetsFly_contactemail);
                RootObject.contactphone = Convert.ToString(_supplierConfig.LetsFly.LetsFly_contactphone);
                RootObject.special_request = Convert.ToString(remark);
                List<letsflybookingRQ.Customerinfo> custinfo = new List<letsflybookingRQ.Customerinfo>();
                for (int r = 0; r < NoOfRooms; r++)
                {
                    if (dtPassengers.Rows.Count > 0)
                    {
                        dtPassengers.DefaultView.RowFilter = "PickUp='" + Convert.ToString(bookRequest.userCart.UserCartDetails[r].RoomTypeId) + "' and UserCartID=" + Convert.ToString(bookRequest.userCart.UserCartDetails[r].UserCartId + " and RoomNo='" + Convert.ToString(bookRequest.userCart.UserCartDetails[r].RoomNo) + "'");

                        dtPassengers.DefaultView.Sort = "IsLead DESC";
                        int Islead = Convert.ToInt32(dtPassengers.DefaultView[0]["islead"]);

                        if (dtPassengers.DefaultView.Count > 0)
                        {
                            for (int k = 0; k < dtPassengers.DefaultView.Count; k++)
                            {
                                letsflybookingRQ.Customerinfo customer = new letsflybookingRQ.Customerinfo();

                                firstName = Convert.ToString(dtPassengers.DefaultView[k]["FirstName"]).Trim();
                                lastName = Convert.ToString(dtPassengers.DefaultView[k]["LastName"]).Trim();
                                customer.roomnum = Convert.ToInt32(dtPassengers.DefaultView[k]["RoomNo"]);
                                customer.firstname = firstName;
                                customer.lastname = lastName;
                                customer.nationality = nationality;
                                custinfo.Add(customer);


                            }
                        }
                    }
                }
                RootObject.customerinfo = custinfo;
                // string Request = JsonConvert.SerializeObject(RootObject, Setting);
                string Request = Newtonsoft.Json.JsonConvert.SerializeObject(RootObject);
                return Request;


            }
            catch (Exception ex)
            {
                _ = _exception.Error(ex);
                return "";
            }
        }
        public string LetsflyBookingCancelReq(string AgentVoucherNo)
        {
            try
            {

                string[] spitid1 = AgentVoucherNo.Split(new String[] { "$$$" }, StringSplitOptions.None);
                string orderno = spitid1[0];
                string lforderno = spitid1[1];
                //JsonSerializerSettings Setting = new JsonSerializerSettings
                //{
                //    NullValueHandling = NullValueHandling.Ignore,
                //    DefaultValueHandling = DefaultValueHandling.Ignore
                //};
                letsflybookingcancelRQ.Root RootObject = new letsflybookingcancelRQ.Root();
                RootObject.orderno = orderno;
                RootObject.lforderno = lforderno;
                // string Request = JsonConvert.SerializeObject(RootObject, Setting);
                string Request = Newtonsoft.Json.JsonConvert.SerializeObject(RootObject);
                return Request;

            }
            catch (Exception ex)
            {
                _ =_exception.ErrorNew(ex, ErrorLog._LogAPI.CancelBooking);
                return "";
            }
        }
    }
}
