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
using XConnect.Supplier.Hyperguest.Interface;
using Options = XConnect.Models.Options;
using System.Text.Json;

namespace XConnect.Supplier.Hyperguest
{
    public class HyperguestRepositoryOLD
    {
        private readonly ICommonMethod _commonMethod;
        private readonly IException _exception;
        private readonly IMarkup _markup;
        private readonly ISupplierRes _supplierRes;
        private readonly KeySettings _keySettings;

        public HyperguestRepositoryOLD(ICommonMethod commonMethod, IException exception, IMarkup markup, ISupplierRes supplierRes, IOptions<KeySettings> keySettings)
        {
            _commonMethod = commonMethod;
            _exception = exception;
            _markup = markup;
            _supplierRes = supplierRes;
            _keySettings = keySettings.Value;
        }

        public void GethyperguestRequestOnly_Async(DataRow[] commonCode, List<StarRating> listRating, HotelSearchParameter SearchPara, List<SupplierDetails> hyperguestDetails, List<SupplierSearchConfig> hyperguestSearchConfig, DataTable dtPreferredDataHotel, ref ConcurrentBag<HotelX> AllTask, string XMLCity, string XMLCountry, string XMLNationality, int Hit, DataTable dtROE, string Provider, TokenData tokenData)
        {
            try
            {
                string Ids = string.Empty;
                string xmlbase = "";
                _commonMethod.GetPrefferedHotelCodes(ref Ids, ref xmlbase, ref hyperguestSearchConfig, "hyperguest", dtPreferredDataHotel, SearchPara.IsSearchFor);
                if (hyperguestSearchConfig != null)
                {
                    double CurrencyFactorhyperguest = 1;
                    string Currency = Convert.ToString(hyperguestDetails[0].CurrencyShortName);


                    if (Currency.ToLower() != tokenData.SiteCurrency.ToLower())
                    {
                        CurrencyFactorhyperguest = _commonMethod.GetROEByCurrency(Currency, dtROE);// Convert.ToDouble(dtInrCurrency.Rows[0]["AmtToExch"]);
                    }

                    for (int k = 0; k < hyperguestSearchConfig.Count; k++)
                    {
                        bool DebugMode = (Convert.ToInt32(hyperguestSearchConfig[k].DebugMode) == 0 ? false : true);
                        string UserName = Convert.ToString(hyperguestDetails[0].UserName);
                        string Password = Convert.ToString(hyperguestDetails[0].Password);
                        {
                            int IsCitywisesearch = Convert.ToInt32(hyperguestSearchConfig[k].City);

                            if (Convert.ToInt32(hyperguestSearchConfig[k].CityID) == 0 || Convert.ToInt32(hyperguestSearchConfig[k].CityID) == SearchPara.CityID)
                            {
                                if (commonCode.Length > 0)
                                {
                                    int Noofrecords = Convert.ToInt32(hyperguestSearchConfig[k].Noofrecords);
                                    DataTable[] splittedtables = commonCode.AsEnumerable()
                                                .Select((row, index) => new { row, index })
                                                .GroupBy(x => x.index / Noofrecords)  // integer division, the fractional part is truncated
                                                .Select(g => g.Select(x => x.row).CopyToDataTable())
                                                .ToArray();

                                    if (SearchPara.Filters.IsShowRooms == 0)
                                    {
                                        for (int a = 0; a < splittedtables.Length; a++)
                                        {
                                            var paramValues1 = String.Join(",", splittedtables[a].Select().Select(x => String.Join(",", Convert.ToString(x.ItemArray[0]))));
                                            string IdsForSearch = Convert.ToString(paramValues1);
                                            HotelX Xhotel = new HotelX();
                                            Xhotel.Supplier = Provider;
                                            Xhotel.DebugeMode = DebugMode;
                                            Xhotel.SearchTimeOut = Convert.ToInt32(hyperguestSearchConfig[k].HotelSearchTimeOut);
                                            //if (Hit > 0)
                                            Xhotel.ProcessTimeout = Convert.ToInt32(hyperguestSearchConfig[k].HotelSearchTimeOut2);

                                            Xhotel.URL = Convert.ToString(hyperguestDetails[0].WebSiteURL);
                                            Xhotel.UserId = Convert.ToString(hyperguestDetails[0].UserID);
                                            Xhotel.UserName = UserName;
                                            Xhotel.PassWord = Password;
                                            Xhotel.CurrencyCode = Currency;
                                            Xhotel.CurrencyFactor = CurrencyFactorhyperguest;
                                            Xhotel.XmlCity = XMLCity;
                                            Xhotel.XmlCountry = XMLCountry;
                                            Xhotel.XmlNationality = XMLNationality;
                                            string PaxPassport = XMLNationality;
                                            string checkin = Convert.ToString(SearchPara.CheckInDate);
                                            string checkout = Convert.ToString(SearchPara.CheckOutDate);

                                            checkin = checkin.Substring(6, 4) + "-" + checkin.Substring(3, 2) + "-" + checkin.Substring(0, 2);
                                            checkout = checkout.Substring(6, 4) + "-" + checkout.Substring(3, 2) + "-" + checkout.Substring(0, 2);

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
                                                //objrq.Request = XMLhyperguestRequestOnly_Async(Xhotel.URL, SearchPara, checkin, checkout, IdsForSearch, XMLNationality, Currency, 1);
                                                objrq.Request = XMLhyperguestRequestOnly_Async(SearchPara, Xhotel.URL, Xhotel.XmlNationality, IdsForSearch);
                                                objrq.Response = "";
                                                RQRS.Add(objrq);
                                            }
                                            Xhotel.RQRS = RQRS;
                                            Xhotel.ReturnRooms = 5;
                                            Xhotel.TechName = "Hyperguest";
                                            #endregion
                                            AllTask.Add(Xhotel);
                                        }
                                    }
                                    else  // && count == 1)
                                    {
                                        for (int a = 0; a < splittedtables.Length; a++)
                                        {
                                            var paramValues1 = String.Join(",", splittedtables[a].Select().Select(x => String.Join(",", Convert.ToString(x.ItemArray[0]))));
                                            string IdsForSearch = Convert.ToString(paramValues1);
                                            HotelX Xhotel = new HotelX();
                                            Xhotel.Supplier = Provider;
                                            Xhotel.DebugeMode = DebugMode;
                                            Xhotel.SearchTimeOut = Convert.ToInt32(hyperguestSearchConfig[k].HotelSearchTimeOut);
                                            //if (Hit > 0)
                                            Xhotel.ProcessTimeout = Convert.ToInt32(hyperguestSearchConfig[k].HotelSearchTimeOut2);

                                            Xhotel.URL = Convert.ToString(hyperguestDetails[0].WebSiteURL);
                                            Xhotel.UserId = Convert.ToString(hyperguestDetails[0].UserID);
                                            Xhotel.UserName = UserName;
                                            Xhotel.PassWord = Password;
                                            Xhotel.CurrencyCode = Currency;
                                            Xhotel.CurrencyFactor = CurrencyFactorhyperguest;
                                            Xhotel.XmlCity = XMLCity;
                                            Xhotel.XmlCountry = "";
                                            Xhotel.XmlNationality = XMLNationality;
                                            string PaxPassport = XMLNationality;
                                            string checkin = Convert.ToString(SearchPara.CheckInDate);
                                            string checkout = Convert.ToString(SearchPara.CheckOutDate);

                                            checkin = checkin.Substring(6, 4) + "-" + checkin.Substring(3, 2) + "-" + checkin.Substring(0, 2);
                                            checkout = checkout.Substring(6, 4) + "-" + checkout.Substring(3, 2) + "-" + checkout.Substring(0, 2);

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
                                                // objrq.Request = XMLhyperguestRequestOnly_Async(SearchPara, Xhotel.XmlCity, XMLCountry, Xhotel.URL, Xhotel.UserName, Xhotel.PassWord, Xhotel.XmlNationality, Timeout, hyperguestCurrenc, Ids);
                                                objrq.Request = XMLhyperguestRequestOnly_Async(SearchPara, Xhotel.URL, Xhotel.XmlNationality, IdsForSearch);
                                                objrq.Response = "";
                                                RQRS.Add(objrq);
                                            }
                                            Xhotel.RQRS = RQRS;
                                            Xhotel.ReturnRooms = 5;
                                            Xhotel.TechName = "Hyperguest";
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
            catch (Exception ex)
            {
                _exception.Error(ex);
            }
        }

        public string XMLhyperguestRequestOnly_Async(HotelSearchParameter SearchPara, string hyperguest_URL, string Nationality, string hotelId)

        {
            try
            {

                string checkIn = System.Convert.ToString(SearchPara.CheckInDate);
                string checkout = Convert.ToString(SearchPara.CheckOutDate);
                int nights = SearchPara.NoofNights;
                checkIn = checkIn.Substring(6, 4) + "-" + checkIn.Substring(3, 2) + "-" + checkIn.Substring(0, 2);
                checkout = checkout.Substring(6, 4) + "-" + checkout.Substring(3, 2) + "-" + checkout.Substring(0, 2);
                try
                {

                    #region Adults Child Count

                    StringBuilder guests = new StringBuilder();

                    guests.Append("&guests");
                    string guestcount = "";
                    for (int ser = 0; ser < SearchPara.Rooms.Count; ser++)
                    {
                        string guest = "";
                        int NoofAdults = SearchPara.Rooms[ser].NoofAdults;
                        int NoofChild = SearchPara.Rooms[ser].NoofChild;
                        string childage = "";

                        if (NoofChild > 0)
                        {
                            switch (NoofChild)
                            {
                                case 1:
                                    childage = "-" + SearchPara.Rooms[ser].Child1Age;
                                    break;
                                case 2:
                                    childage = "-" + SearchPara.Rooms[ser].Child1Age + "," + SearchPara.Rooms[ser].Child2Age;
                                    break;
                            }
                        }
                        if (ser == 0)
                        {
                            guest = NoofAdults + childage;
                        }
                        else
                        {
                            guest = "." + NoofAdults + childage;
                        }
                        guestcount = guestcount + guest;
                    }
                    #endregion

                    #region Req 
                    StringBuilder SearchRQ = new StringBuilder();

                    SearchRQ.Append("?checkIn=" + checkIn + "");
                    SearchRQ.Append("&nights=" + nights + "");
                    SearchRQ.Append("&guests=" + guestcount + "");
                    SearchRQ.Append("&hotelIds=" + hotelId + "");
                    SearchRQ.Append("&customerNationality=" + Nationality + "");
                    string hyperguestRoomReq = hyperguest_URL + SearchRQ.ToString();
                    return hyperguestRoomReq;

                    #endregion
                }
                catch (Exception ex)
                {
                    _exception.Error(ex);
                    return "";
                }


            }
            catch (Exception e)
            {
                _exception.Error(e);
            }
            return "";
        }

        public async Task<List<Options>> hyperguestHotelSearchWithRooms(HotelX Item, CommonSearch commonSearch, string ResponseData, string CityCode, HotelSearchParameter SearchParam, double CurrencyFactorHyperGuest, string Supplier, DataSet dsAgentDetails, DataTable dtMarkupDetails, string CurrencyCode, DataTable dtROE, string XmlNationality, Int32 processTimeout, string ResProccessTimerLogActive, string ResProccessTimeValueforSaveLog, string StoreSupplierResErrorLog)
        {
            System.Diagnostics.Stopwatch timerroomx = new System.Diagnostics.Stopwatch();
            timerroomx.Start();
            List<Options> options = new List<Options>();
            DataTable DtHotel = _commonMethod.GenerateDataTableNewV2();
            try
            {
                if (ResponseData != string.Empty)
                {
                    int totalAdult = 0;
                    int totalChild = 0;
                    for (int b = 0; b < SearchParam.Rooms.Count; b++)
                    {
                        totalAdult = Convert.ToInt32(SearchParam.Rooms[b].NoofAdults) + totalAdult;
                        totalChild = Convert.ToInt32(SearchParam.Rooms[b].NoofChild) + totalChild;
                    }
                    var result = SearchParam.Rooms.GroupBy(x => new { x.NoofAdults, x.NoofChild, x.Child1Age, x.Child2Age }).Select(n => new { NoofAdults = n.Key.NoofAdults, NoofChild = n.Key.NoofChild, ChildAge = n.ToList(), Count = n.Count() });

                    HyperguestClass.hyperguest.RoomList.Root hyperguestResponse = null;
                    try
                    {
                        //hyperguestResponse = JsonConvert.DeserializeObject<HyperguestClass.hyperguest.RoomList.Root>(ResponseData);
                        hyperguestResponse = JsonSerializer.Deserialize<HyperguestClass.hyperguest.RoomList.Root>(ResponseData);

                    }
                    catch (Exception ex)
                    {
                        await _exception.Error(ex);
                    }
                    if (hyperguestResponse == null)
                    {
                        Item.ResponseType = "NoCount";
                        return options;
                    }
                    if (hyperguestResponse.results.Count < 1)
                    {
                        Item.ResponseType = "NoCount";
                        return options;
                    }
                    for (int h = 0; h < hyperguestResponse.results.Count; h++)
                    {
                        if (hyperguestResponse.results[h].rooms.Count == 0)
                            continue;  //newchange
                        #region "When not Need Room"  
                        if (SearchParam.Filters.IsShowRooms == 0)
                        {
                            #region "Hotel Code Block "
                            {
                                if (hyperguestResponse.results[h].rooms.Count() == 0)
                                {
                                    continue;
                                }
                                //double CurrencyFactor = 0;
                                double CurrencyFactor = 1;
                                string HotelID = Convert.ToString(hyperguestResponse.results[h].hotelId);
                                string currency = hyperguestResponse.results[h].rooms[0].ratePlans[0].inclusivePrice.currency;
                                //if (currency.ToLower() != "usd")
                                {
                                    //DataTable dtInrCurrency = Common.GetCountryCurrency(currency);
                                    CurrencyFactor = _commonMethod.GetROEByCurrency(currency, dtROE);   // newnewchange
                                                                                                        // CurrencyFactor = Convert.ToDouble(dtInrCurrency.Rows[0]["AmtToExch"]);
                                }
                                double NetPrice = Convert.ToDouble(Convert.ToDouble(hyperguestResponse.results[h].rooms[0].ratePlans[0].inclusivePrice.amount * Convert.ToInt32(SearchParam.Rooms.Count)) * CurrencyFactor);

                                #region "Hotel Code Block "
                                DataRow drXmlHotel = DtHotel.NewRow();
                                drXmlHotel["XMLHotelCode"] = HotelID;
                                drXmlHotel["HotelName"] = "";
                                drXmlHotel["SupplierName"] = Supplier;
                                drXmlHotel["OfficeID"] = 0;
                                drXmlHotel["ContractID"] = 0;
                                drXmlHotel["Rating"] = 0;
                                drXmlHotel["Reviews"] = 0;
                                drXmlHotel["UniqueNo"] = 0;
                                drXmlHotel["IsShowSupplier"] = false;
                                drXmlHotel["BookingStatus"] = "Available";
                                drXmlHotel["XMLCity"] = "";
                                drXmlHotel["SupplierCurrency"] = currency;
                                drXmlHotel["FinalSellingPrice"] = NetPrice;
                                drXmlHotel["SellingPrice"] = NetPrice;
                                drXmlHotel["OfferText"] = "";
                                DtHotel.Rows.Add(drXmlHotel);
                                Double FinalTotal = NetPrice;
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
                                #endregion
                            }
                            #endregion
                        }
                        #region"When Room need"
                        else
                        {
                            string xmlhotelcode = hyperguestResponse.results[h].hotelId;
                            bool willAddOrNot = true;
                            Int64 RaynaID = 0;
                            Int64 GiataId = 0;
                            int RatingHotel = 0;
                            Int64 OwnsystemHotelId = 0;
                            double MinPrice = 1;
                            double AgentPercentage = 0;
                            double AgentMarkupAmount = 0;
                            Boolean blnIsShowSupplier = false;
                            Boolean isPerce = true;
                            string currency = hyperguestResponse.results[h].rooms[0].ratePlans[0].inclusivePrice.currency;
                            //if (currency.ToLower() != "usd")
                            {
                                CurrencyFactorHyperGuest = _commonMethod.GetROEByCurrency(currency, dtROE);
                            }

                            Item.ResponseType = "Success";
                            string HotelID = xmlhotelcode;
                            double FinalMinPrice = 0;
                            DataRow drXmlHotel = DtHotel.NewRow();
                            drXmlHotel["XMLHotelCode"] = HotelID;
                            drXmlHotel["HotelName"] = "";
                            drXmlHotel["SupplierName"] = Supplier;
                            drXmlHotel["OfficeID"] = 0;
                            drXmlHotel["ContractID"] = 0;
                            drXmlHotel["Rating"] = 0;
                            drXmlHotel["Reviews"] = 0;
                            drXmlHotel["UniqueNo"] = 0;
                            drXmlHotel["IsShowSupplier"] = false;
                            drXmlHotel["BookingStatus"] = "Available";
                            drXmlHotel["XMLCity"] = "";
                            drXmlHotel["SupplierCurrency"] = currency;
                            drXmlHotel["FinalSellingPrice"] = 1;
                            drXmlHotel["SellingPrice"] = 1;
                            drXmlHotel["OfferText"] = "";
                            DtHotel.Rows.Add(drXmlHotel);

                            #region "Room code Block"
                            int Rating = 0;
                            DataTable dthyperguestHotelRoom = _commonMethod.GenerateRoomtypeDataTableNewV2();
                            DataTable dthyperguestCancellation = _commonMethod.GenerateCancellationDataTableNewV2();
                            DataTable dthyperguestRoomRateBreakup = _commonMethod.GenerateoomRateBreackUpDataTableNewV2();
                            _markup.GetHotelDataCommon(ref MinPrice, ref willAddOrNot, ref OwnsystemHotelId, ref RaynaID, ref GiataId, ref Rating, ref drXmlHotel, commonSearch.AgentId, commonSearch.TariffID, commonSearch.dsAgentDetails, commonSearch.supplierGiataMaster, SearchParam, commonSearch.dtCountryCityMarkup, commonSearch.B2CMarkup, commonSearch.IsParentId, commonSearch.SubAgentMarkup, commonSearch.SearchByHotel, commonSearch.HotelIDS);
                            if (willAddOrNot)
                            {
                                _markup.getAgetMarkupSetting("hyperguest", GiataId.ToString(), commonSearch.dtSuplierMarkup, commonSearch.dtMarkupDetails, ref AgentPercentage, ref AgentMarkupAmount, ref blnIsShowSupplier, ref isPerce, commonSearch.AgentId, RatingHotel, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), Convert.ToString(SearchParam.CheckInDate));
                                string Hotelremark = "";
                                for (int i = 0; i < hyperguestResponse.results[h].remarks.Count; i++)
                                {
                                    string remark1 = Convert.ToString(hyperguestResponse.results[h].remarks[i]);
                                    if (i == 0)
                                        Hotelremark = remark1;
                                    else
                                        Hotelremark = Hotelremark + "<br>" + remark1;
                                }
                                _markup.GetHotelDataCommon(ref MinPrice, ref willAddOrNot, ref OwnsystemHotelId, ref RaynaID, ref GiataId, ref RatingHotel, ref drXmlHotel, commonSearch.AgentId, commonSearch.TariffID, commonSearch.dsAgentDetails, commonSearch.supplierGiataMaster, SearchParam, commonSearch.dtCountryCityMarkup, commonSearch.B2CMarkup, commonSearch.IsParentId, commonSearch.SubAgentMarkup, commonSearch.SearchByHotel, commonSearch.HotelIDS);
                                for (int ro = 0; ro < hyperguestResponse.results[h].rooms.Count; ro++)
                                {
                                    foreach (var item in result)
                                    {
                                        var aa = item.NoofAdults;
                                        var cc = item.NoofChild;
                                        var RCount = item.Count;

                                        int TotalA = Convert.ToInt32(hyperguestResponse.results[h].rooms[ro].pax.adults);
                                        int TotalC = Convert.ToInt32(hyperguestResponse.results[h].rooms[ro].pax.children.Count);
                                        string Roomnumber = "";
                                        for (int kk = 0; kk < SearchParam.Rooms.Count; kk++)
                                        {
                                            totalAdult = Convert.ToInt32(SearchParam.Rooms[kk].NoofAdults);
                                            totalChild = Convert.ToInt32(SearchParam.Rooms[kk].NoofChild);
                                            if (totalAdult == aa && cc == totalChild)
                                            {
                                                int RNo = Convert.ToInt32(SearchParam.Rooms[kk].RoomNo);
                                                if (Roomnumber == "")
                                                {
                                                    Roomnumber = Convert.ToString(RNo);
                                                }
                                                else
                                                {
                                                    Roomnumber = Roomnumber + "$" + RNo;
                                                }
                                            }
                                        }
                                        string[] Rnumber = Roomnumber.Split('$');
                                        for (int r = 0; r < Rnumber.Length; r++)
                                        {
                                            string RoomNo = Convert.ToString(Rnumber[r]);
                                            string RoomName = hyperguestResponse.results[h].rooms[ro].roomName;
                                            string RoomTypeCord = hyperguestResponse.results[h].rooms[ro].roomTypeCode;
                                            int NumberOfAvailableRooms = hyperguestResponse.results[h].rooms[ro].numberOfAvailableRooms;
                                            int RoomId = hyperguestResponse.results[h].rooms[ro].roomId;

                                            totalAdult = 0;
                                            totalChild = 0;
                                            int Rno = Convert.ToInt16(Rnumber[r]) - 1;
                                            try
                                            {
                                                totalAdult = Convert.ToInt32(SearchParam.Rooms[Rno].NoofAdults) + totalAdult;
                                                totalChild = Convert.ToInt32(SearchParam.Rooms[Rno].NoofChild) + totalChild;
                                            }
                                            catch (Exception)
                                            {
                                                totalAdult = Convert.ToInt32(SearchParam.Rooms[r].NoofAdults) + totalAdult;
                                                totalChild = Convert.ToInt32(SearchParam.Rooms[r].NoofChild) + totalChild;
                                            }

                                            for (int roo = 0; roo < hyperguestResponse.results[h].rooms[ro].ratePlans.Count; roo++)
                                            {
                                                string RemarkRoom = Hotelremark;
                                                string remark = "";
                                                string RatePlanCode = hyperguestResponse.results[h].rooms[ro].ratePlans[roo].ratePlanCode;
                                                string Board = hyperguestResponse.results[h].rooms[ro].ratePlans[roo].board;
                                                string Payment = hyperguestResponse.results[h].rooms[ro].ratePlans[roo].payment;
                                                string pricetype = hyperguestResponse.results[h].rooms[ro].ratePlans[roo].priceType;
                                                string Charge = hyperguestResponse.results[h].rooms[ro].ratePlans[roo].charge;
                                                double price = Convert.ToDouble(hyperguestResponse.results[h].rooms[ro].ratePlans[roo].inclusivePrice.amount);
                                                double RSprice = Convert.ToDouble(hyperguestResponse.results[h].rooms[ro].ratePlans[roo].inclusivePrice.amount);
                                                double exclusiveprice = hyperguestResponse.results[h].rooms[ro].ratePlans[roo].exclusivePrice.amount;
                                                currency = hyperguestResponse.results[h].rooms[ro].ratePlans[roo].inclusivePrice.currency;
                                                string remarkdisply = "";
                                                try
                                                {
                                                    for (int f = 0; f < hyperguestResponse.results[h].rooms[ro].ratePlans[roo].fees.mandatoryTaxes.Count; f++)
                                                    {
                                                        string ab = hyperguestResponse.results[h].rooms[ro].ratePlans[roo].fees.mandatoryTaxes[f].relation;
                                                        if (ab.ToLower() == "display")
                                                        {
                                                            string madentorydescription = hyperguestResponse.results[h].rooms[ro].ratePlans[roo].fees.mandatoryTaxes[f].description;
                                                            string Mcurrency = hyperguestResponse.results[h].rooms[ro].ratePlans[roo].fees.mandatoryTaxes[f].currency;
                                                            double MFee = hyperguestResponse.results[h].rooms[ro].ratePlans[roo].fees.mandatoryTaxes[f].amount;
                                                            remark = RemarkRoom + "<br>" + remarkdisply + "<br>" + madentorydescription + ":" + MFee + " " + Mcurrency;
                                                        }
                                                    }
                                                }
                                                catch (Exception e)
                                                {
                                                    await _exception.Error(e);
                                                }
                                                isPerce = true;
                                                //if (currency.ToLower() != "usd")
                                                {
                                                    CurrencyFactorHyperGuest = _commonMethod.GetROEByCurrency(currency, dtROE);
                                                }
                                                int MasterSupplierID = 0;
                                                double SubAgentMarkup = 0;
                                                remark = RemarkRoom + "<br>" + remark;
                                                price = price * CurrencyFactorHyperGuest;
                                                double CancellationPrice = price;// * CurrencyFactorhyperguest;

                                                string boardType = hyperguestResponse.results[h].rooms[ro].ratePlans[roo].board.Replace("'", "");
                                                string uniqid = Guid.NewGuid().ToString();

                                                string RoomTypeId = RoomTypeCord + "###" + RatePlanCode + "###" + xmlhotelcode + "###" + RSprice + "###" + currency + "###" + XmlNationality + "###" + Guid.NewGuid().ToString().Substring(0, 4) + "###" + CurrencyFactorHyperGuest;
                                                double TotalAmount = price;

                                                #region Ratebreakup
                                                for (int n = 0; n < SearchParam.NoofNights; n++)
                                                {
                                                    string CheckInDate = Convert.ToString(SearchParam.CheckInDate);
                                                    string CheckOutDate = Convert.ToString(SearchParam.CheckOutDate);
                                                    DateTime dtCheckInDate = DateTime.ParseExact(CheckInDate, "dd/MM/yyyy", null);
                                                    DateTime dtCheckOutDate = DateTime.ParseExact(CheckOutDate, "dd/MM/yyyy", null);
                                                    TimeSpan t = dtCheckOutDate - dtCheckInDate;
                                                    double ratePerNight = TotalAmount;

                                                    #region Vat
                                                    double VatOnPerNight = 0;
                                                    ratePerNight = _markup.ProcessHotelRateBreakupPrice_WithMarkup(1, SearchParam.CityID, SearchParam.CountryCode, ratePerNight, commonSearch.B2CMarkup, commonSearch.IsParentId, isPerce, Convert.ToDouble(commonSearch.AgentPercent), AgentMarkupAmount, SubAgentMarkup, ref VatOnPerNight);
                                                    #endregion

                                                    ratePerNight = ratePerNight / SearchParam.NoofNights;
                                                    DataRow drRateBreackUp = dthyperguestRoomRateBreakup.NewRow();
                                                    drRateBreackUp["SupplierName"] = Supplier;
                                                    drRateBreackUp["OwnsystemHotelId"] = OwnsystemHotelId;
                                                    drRateBreackUp["ContractID"] = 0;
                                                    drRateBreackUp["RoomNo"] = Convert.ToInt32(RoomNo);
                                                    drRateBreackUp["RoomTypeId"] = RoomTypeId;
                                                    drRateBreackUp["MealId"] = Board;
                                                    drRateBreackUp["Meal"] = Board;
                                                    string rateWeekColumn = Convert.ToString(dtCheckInDate.Date.AddDays(n).DayOfWeek).Substring(0, 3);
                                                    drRateBreackUp["DateOfWeek"] = rateWeekColumn;
                                                    DateTime rateDate = dtCheckInDate.Date.AddDays(n);
                                                    drRateBreackUp["Date"] = rateDate;
                                                    drRateBreackUp["Rate"] = ratePerNight;
                                                    drRateBreackUp["OrderNo"] = 1;
                                                    drRateBreackUp["RoomTypeName"] = RoomName;
                                                    drRateBreackUp["DateOfWeek"] = rateWeekColumn;
                                                    drRateBreackUp["Date"] = rateDate;
                                                    drRateBreackUp["BookingStatus"] = "Available";
                                                    drRateBreackUp["BoardTypeID"] = "";
                                                    drRateBreackUp["VatdiffAmount"] = 0;
                                                    drRateBreackUp["RateType"] = RoomName;
                                                    drRateBreackUp["SupplierCurrency"] = currency;
                                                    drRateBreackUp["BuyingPrice"] = ratePerNight / CurrencyFactorHyperGuest;
                                                    dthyperguestRoomRateBreakup.Rows.Add(drRateBreackUp);

                                                }
                                                #endregion
                                                #region Rooms
                                                DataRow drRoom = dthyperguestHotelRoom.NewRow();
                                                drRoom["OwnsystemHotelId"] = OwnsystemHotelId;
                                                drRoom["RoomTypeCode"] = RoomTypeCord;
                                                drRoom["BoardType"] = Board;
                                                drRoom["MealName"] = Board;
                                                drRoom["MealId"] = Board;
                                                drRoom["Type"] = "";
                                                drRoom["Board"] = "";
                                                drRoom["BookingStatus"] = "Available";
                                                drRoom["UniqueNo"] = Guid.NewGuid().ToString();
                                                drRoom["ContractID"] = 0;
                                                drRoom["RoomNo"] = Convert.ToInt32(RoomNo);
                                                drRoom["BoardTypeID"] = "";
                                                drRoom["AdultsOccupancy"] = totalAdult;
                                                drRoom["ChildOccupancy"] = totalChild;
                                                drRoom["OfferCode"] = "";
                                                drRoom["OfferDescription"] = "";
                                                drRoom["RoomSequenceNo"] = "";

                                                string EssentialInformation = remark;
                                                string Enn = "";

                                                drRoom["EssentialInformation"] = EssentialInformation;
                                                drRoom["Characteristic"] = "";
                                                bool IsCombine = false;
                                                bool IsDynamicPolicy = true;
                                                #region Vat
                                                double itemprice = price;
                                                _markup.ProcessHotelRoom_WithMarkup(CurrencyFactorHyperGuest, currency, IsDynamicPolicy, IsCombine, SearchParam.NoofNights * SearchParam.Rooms.Count, SearchParam.CityID, SearchParam.CountryCode, SearchParam.CheckInDate, SearchParam.CheckOutDate, ref price, ref drRoom, isPerce, AgentPercentage, AgentMarkupAmount, SubAgentMarkup, commonSearch.B2CMarkup, commonSearch.IsParentId);
                                                #endregion
                                                drRoom["SupplierName"] = Supplier;
                                                drRoom["RoomTypeName"] = RoomName;
                                                drRoom["RoomTypeId"] = RoomTypeId;
                                                dthyperguestHotelRoom.Rows.Add(drRoom);
                                                #endregion

                                                //18-01-2022aaa

                                                #region  "Cancellationpolicy"

                                                if (hyperguestResponse.results[h].rooms[ro].ratePlans[roo].cancellationPolicies != null)
                                                {
                                                    bool Istrue = true;
                                                    //for (int c = 0; c < hyperguestResponse.results[h].rooms[ro].ratePlans[roo].cancellationPolicies.Count(); c++)
                                                    for (int c = 0; c < 1; c++)
                                                    {
                                                        double CancelletionPrice = price;// Convert.ToDouble(hyperguestResponse.results[h].rooms[ro].ratePlans[roo].cancellationPolicies[c].amount);
                                                        string PenaltyType = (hyperguestResponse.results[h].rooms[ro].ratePlans[roo].cancellationPolicies[c].penaltyType);
                                                        int CancelletiondaysBefore = Convert.ToInt32(hyperguestResponse.results[h].rooms[ro].ratePlans[roo].cancellationPolicies[c].daysBefore);
                                                        DateTime CancellationDate = DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", CultureInfo.InvariantCulture).Date;
                                                        string roomno = RoomNo;
                                                        double CompanyBuy = price;// CancellationPrice;
                                                        DateTime curDate = _commonMethod.GetTimeZoneDate(SearchParam.CurrentDate);
                                                        double Penalty = Convert.ToDouble(CompanyBuy);
                                                        DateTime MainDate = new DateTime();
                                                        DateTime StartDate = MainDate;
                                                        DateTime CancelDate = CancellationDate.Date.AddDays(-CancelletiondaysBefore);// DateTime.Now.Date;
                                                        string Remark = "";// Convert.ToString(hyperguestResponse.Tables["HotelDTO"].Rows[0]["Remark"]);
                                                        remark = "";
                                                        #region Vat
                                                        double VatOnPolicy = 0;
                                                        double MainAgentCharge = Penalty;
                                                        double CompanyChargeAmount = itemprice;
                                                        //Penalty = Common.ProcessHotelCancelationPrice_WithMarkup(ref MainAgentCharge, SearchParam.NoofNights, SearchParam.CityID, SearchParam.CountryId, Penalty, isPerce, B2CMarkup, AgentPercentage, AgentMarkupAmount, IsParentId, SubAgentMarkup, ref VatOnPolicy);
                                                        #endregion
                                                        int CutOffDays = XMLHotelBookingCommon.CutOffDays;
                                                        CutOffDays = _commonMethod.GetCutOffDays(commonSearch.dtSuplierMarkup, Supplier);
                                                        DateTime NowDate = DateTime.Now.Date;
                                                        if (CancelDate.AddDays(CutOffDays).Date > DateTime.Now.Date)
                                                        {
                                                            //StartDate = StartDate.AddDays(CutOffDays).Date;
                                                            CancelDate = CancelDate.AddDays(CutOffDays).Date;
                                                        }
                                                        else
                                                        {
                                                            CancelDate = DateTime.Now.Date;
                                                            //Penalty = Common.ProcessHotelCancelationPrice_WithMarkup(ref MainAgentCharge, SearchParam.NoofNights, SearchParam.CityID, SearchParam.CountryId, CompanyBuy, isPerce, B2CMarkup, AgentPercentage, AgentMarkupAmount, IsParentId, SubAgentMarkup, ref VatOnPolicy);
                                                        }
                                                        try
                                                        {
                                                            CancelletionPrice = Convert.ToDouble(hyperguestResponse.results[h].rooms[ro].ratePlans[roo].cancellationPolicies[c].amount);
                                                        }
                                                        catch (Exception)
                                                        {

                                                        }

                                                        if (CancelletionPrice > 0)
                                                        {
                                                            CancelDate = DateTime.Now.Date;
                                                        }

                                                        if (NowDate < CancelDate)
                                                        {
                                                            if (Istrue == true)
                                                            {
                                                                double zero = 0;

                                                                dthyperguestCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref zero, dthyperguestCancellation.NewRow(), CurrencyFactorHyperGuest, currency, Supplier, Convert.ToString(OwnsystemHotelId), Convert.ToString(roomno),
                                                                                                                                                  Convert.ToString(RoomTypeId), RoomName, Board, Convert.ToDateTime(curDate), CancelDate.AddDays(-1), 0, 0, "0", VatOnPolicy, uniqid, "NO", "", "", "", ""));
                                                                dthyperguestCancellation.AcceptChanges();
                                                                Istrue = false;
                                                            }

                                                            dthyperguestCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref CompanyChargeAmount, dthyperguestCancellation.NewRow(), CurrencyFactorHyperGuest, currency, Supplier, Convert.ToString(OwnsystemHotelId), Convert.ToString(roomno),
                                                                                           Convert.ToString(RoomTypeId), RoomName, Board, Convert.ToDateTime(CancelDate), DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null), MainAgentCharge, Penalty, "0", VatOnPolicy, uniqid, "NO", "", "", "", ""));
                                                            dthyperguestCancellation.AcceptChanges();
                                                        }
                                                        else
                                                        {
                                                            dthyperguestCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref CompanyChargeAmount, dthyperguestCancellation.NewRow(), CurrencyFactorHyperGuest, currency, Supplier, Convert.ToString(OwnsystemHotelId), Convert.ToString(roomno),
                                                                                           Convert.ToString(RoomTypeId), RoomName, Board, curDate, DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null), MainAgentCharge, Penalty, "0", VatOnPolicy, uniqid, "Yes", "", ""));
                                                            dthyperguestCancellation.AcceptChanges();
                                                        }

                                                        //}
                                                    }
                                                    #endregion
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            Options ops = (await _supplierRes.GetCommonRoomForSuppliers(GiataId, commonSearch.tokenData, commonSearch.advancedOptions, MinPrice, drXmlHotel, dthyperguestHotelRoom, dthyperguestCancellation, dthyperguestRoomRateBreakup, SearchParam));
                            if (ops != null)
                                options.Add(ops);
                            // here
                            #endregion
                        }
                        #endregion
                    }
                }
                return options;
            }
            catch (Exception e)
            {
                await _exception.Error(e);
            }
            finally
            {
                #region ResProcessTimer
                try
                {
                    if (SearchParam != null && ResProccessTimerLogActive == "1" && timerroomx.ElapsedMilliseconds > Convert.ToInt32(ResProccessTimeValueforSaveLog))
                    {
                        timerroomx.Stop();
                        SearchParam.SupplierTime += ",|Finish:hyperguest Process Response  " + "|Time :" + timerroomx.Elapsed.ToString();
                        _= Task.Run(() =>  _commonMethod.FireAway_SQL_ResPorcessTimeLog("", timerroomx.Elapsed.ToString(), "hyperguest", SearchParam.SearchKey, 1, SearchParam.AgentID));
                    }
                }
                catch (Exception x)
                {
                    await _exception.Error(x);
                }
                #endregion

                if (Item.ResponseType == "NoCount" && StoreSupplierResErrorLog == "1")
                    await _commonMethod.XMLRequestResponseTrack_Error(Item.RQRS[0].Request, Item.RQRS[0].Response, Item.Supplier, timerroomx.Elapsed.ToString(), "", SearchParam.SearchKey);
            }
            return options;
        }
    }
}
