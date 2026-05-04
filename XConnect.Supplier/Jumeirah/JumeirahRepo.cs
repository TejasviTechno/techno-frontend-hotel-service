using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using XConnect.CommonUtilities;
using XConnect.CommonUtilities.Interface;
using XConnect.ErrorLogHandler.Interface;
using XConnect.Markup.Interface;
using XConnect.Models;
using XConnect.Models.Hotel;
using XConnect.Supplier.Jumeirah.Interface;
using static Azure.Core.HttpHeader;
using static XConnect.Supplier.Booking.Com.preebookrs;
using static XConnect.Supplier.TBO.TBOClass.BookingDetailTbores;
using Options = XConnect.Models.Options;

namespace XConnect.Supplier.Jumeirah
{
    public class JumeirahRepo : IJumeirah
    {
        private readonly ICommonMethod _commonMethod;
        private readonly IException _exception;
        private readonly IMarkup _markup;
        private readonly ISupplierRes _supplierRes;
        private readonly KeySettings _keySettings;
        public JumeirahRepo(ICommonMethod commonMethod, IMarkup markup, ISupplierRes supplierRes, IException exception, IOptions<KeySettings> keySettings)
        {
            _commonMethod = commonMethod;
            _markup = markup;
            _supplierRes = supplierRes;
            _exception = exception;
            _keySettings = keySettings.Value;
        }
        public void GetJumeirahRequestOnly_async(DataRow[] commonCode, List<StarRating> listRating, HotelSearchParameter SearchPara, List<SupplierDetails> lstJumeirahDetails, List<SupplierSearchConfig> lstJumeirahSearchConfig, DataTable dtPreferredDataHotel, ref ConcurrentBag<HotelX> AllTask, int Hit, DataTable dtROE, TokenData tokenData)
        {
            try
            {
                List<StarRating> lstRating = new List<StarRating>();
                lstRating = _commonMethod.GetStarRatingStringList_xtg(SearchPara);

                List<HotelCodeByCity> DtHotelDAta = HtmlExtensions.ToList<HotelCodeByCity>(commonCode.CopyToDataTable());
                int ratingcount = lstRating.Count;
                int hotelcount = DtHotelDAta.Count;

                foreach (SupplierDetails JumeirahDetails in lstJumeirahDetails)
                {
                    int SupplierID = JumeirahDetails.SupplierID;
                    string Supplier = JumeirahDetails.SupplierName;

                    if (lstJumeirahSearchConfig.Count > 0)
                    {
                        try
                        {
                            List<SupplierSearchConfig> JumeirahSearchConfig = lstJumeirahSearchConfig
                                 .Where(x => x != null && x.SupplierID == SupplierID)
                                 .ToList();

                            if (JumeirahSearchConfig != null && JumeirahSearchConfig.Count > 0)
                            {
                                string Ids = string.Empty;
                                string xmlbase = "";
                                _commonMethod.GetPrefferedHotelCodes(ref Ids, ref xmlbase, ref JumeirahSearchConfig, Supplier, dtPreferredDataHotel, SearchPara.IsSearchFor);

                                if (JumeirahSearchConfig != null)
                                {
                                    List<HotelCodeByCity> filter = new List<HotelCodeByCity>();
                                    filter = _commonMethod.FilterWithOutCity_Code(ratingcount, hotelcount, DtHotelDAta, lstRating, Supplier);
                                    if (filter.Count > 0)
                                    {
                                        double CurrencyFactorJumeirah = 1;
                                        string Currency = Convert.ToString(JumeirahDetails.CurrencyShortName);
                                        if (Currency.ToLower() != tokenData.SiteCurrency.ToLower())
                                        {
                                            CurrencyFactorJumeirah = _commonMethod.GetROEByCurrency(Currency, dtROE);
                                        }

                                        for (int k = 0; k < JumeirahSearchConfig.Count; k++)
                                        {
                                            bool DebugMode = (Convert.ToInt32(JumeirahSearchConfig[k].DebugMode) == 0 ? false : true);
                                            string UserName = Convert.ToString(JumeirahDetails.UserName);
                                            string Password = Convert.ToString(JumeirahDetails.Password);
                                            {
                                                int IsCitywisesearch = Convert.ToInt32(JumeirahSearchConfig[k].City);

                                                if (Convert.ToInt32(JumeirahSearchConfig[k].CityID) == 0 || Convert.ToInt32(JumeirahSearchConfig[k].CityID) == SearchPara.CityID)
                                                {
                                                    if (commonCode.Length > 0)
                                                    {
                                                        int Noofrecords = Convert.ToInt32(JumeirahSearchConfig[k].Noofrecords);
                                                        double count = Convert.ToDouble(filter.Count);
                                                        int NoOfDiv = Convert.ToInt32(Math.Ceiling(count / Convert.ToDouble(Noofrecords)));
                                                        List<List<XMLID_Search>> lstxml = _commonMethod.GetXMLIDString(NoOfDiv, Noofrecords, count, filter);


                                                        if (SearchPara.Filters.IsShowRooms == 0)
                                                        {
                                                            for (int a = 0; a < NoOfDiv; a++)
                                                            {
                                                                string IdsForSearch = Convert.ToString(_commonMethod.GetHotelCodeByCOmma(lstxml[a]));

                                                                HotelX Xhotel = new HotelX();
                                                                Xhotel.xmlhotelcode = IdsForSearch;
                                                                Xhotel.Supplier = Supplier;
                                                                Xhotel.DebugeMode = DebugMode;
                                                                Xhotel.SearchTimeOut = Convert.ToInt32(JumeirahSearchConfig[k].HotelSearchTimeOut);
                                                                //if (Hit > 0)
                                                                Xhotel.ProcessTimeout = Convert.ToInt32(JumeirahSearchConfig[k].HotelSearchTimeOut2);
                                                                if (_keySettings.SupplierProxy == "0")
                                                                    Xhotel.URL = Convert.ToString(JumeirahDetails.WebSiteURL);
                                                                else
                                                                    Xhotel.URL = Convert.ToString(JumeirahDetails.WebSiteURLforAPI);
                                                                Xhotel.UserId = Convert.ToString(JumeirahDetails.UserID);
                                                                Xhotel.UserName = UserName;
                                                                Xhotel.PassWord = Password;
                                                                Xhotel.CurrencyCode = Currency;
                                                                Xhotel.CurrencyFactor = CurrencyFactorJumeirah;
                                                                //Xhotel.XmlCity = XMLCity;
                                                                Xhotel.XmlCountry = "";
                                                                //Xhotel.XmlNationality = XMLNationality;
                                                                //string PaxPassport = XMLNationality;
                                                                int NoOfAdult = Convert.ToInt32(SearchPara.Rooms[0].NoofAdults);
                                                                int NoOfChild = Convert.ToInt32(SearchPara.Rooms[0].NoofChild);

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
                                                                    objrq.Request = JumeirahSearchRequest_SigleHotel(checkin, checkout, IdsForSearch, NoOfAdult, NoOfChild);
                                                                    objrq.Response = "";
                                                                    RQRS.Add(objrq);
                                                                }
                                                                Xhotel.RQRS = RQRS;
                                                                #endregion
                                                                Xhotel.ReturnRooms = Convert.ToInt16(JumeirahDetails.ReturnRooms);
                                                                Xhotel.TechName = Convert.ToString(JumeirahDetails.TechName);
                                                                AllTask.Add(Xhotel);
                                                            }
                                                        }
                                                        else  // && count == 1)
                                                        {
                                                            for (int a = 0; a < NoOfDiv; a++)
                                                            {
                                                                string IdsForSearch = Convert.ToString(_commonMethod.GetHotelCodeByCOmma(lstxml[a]));

                                                                HotelX Xhotel = new HotelX();
                                                                Xhotel.xmlhotelcode = IdsForSearch;
                                                                Xhotel.Supplier = Supplier;
                                                                Xhotel.DebugeMode = DebugMode;
                                                                Xhotel.SearchTimeOut = Convert.ToInt32(JumeirahSearchConfig[k].HotelSearchTimeOut);
                                                                //if (Hit > 0)
                                                                Xhotel.ProcessTimeout = Convert.ToInt32(JumeirahSearchConfig[k].HotelSearchTimeOut2);
                                                                if (_keySettings.SupplierProxy == "0")
                                                                    Xhotel.URL = Convert.ToString(JumeirahDetails.WebSiteURL);
                                                                else
                                                                    Xhotel.URL = Convert.ToString(JumeirahDetails.WebSiteURLforAPI);
                                                                Xhotel.UserId = Convert.ToString(JumeirahDetails.UserID);
                                                                Xhotel.UserName = UserName;
                                                                Xhotel.PassWord = Password;
                                                                Xhotel.CurrencyCode = Currency;
                                                                Xhotel.CurrencyFactor = CurrencyFactorJumeirah;
                                                                //Xhotel.XmlCity = XMLCity;
                                                                Xhotel.XmlCountry = "";
                                                                //Xhotel.XmlNationality = XMLNationality;
                                                                //string PaxPassport = XMLNationality;
                                                                int NoOfAdult = Convert.ToInt32(SearchPara.Rooms[0].NoofAdults);
                                                                int NoOfChild = Convert.ToInt32(SearchPara.Rooms[0].NoofChild);

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
                                                                    objrq.Request = JumeirahSearchRequest_SigleHotel(checkin, checkout, IdsForSearch, NoOfAdult, NoOfChild);
                                                                    objrq.Response = "";
                                                                    RQRS.Add(objrq);
                                                                }
                                                                Xhotel.RQRS = RQRS;
                                                                #endregion
                                                                Xhotel.ReturnRooms = Convert.ToInt16(JumeirahDetails.ReturnRooms);
                                                                Xhotel.TechName = Convert.ToString(JumeirahDetails.TechName);
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
                        catch (Exception ex)
                        {
                            _exception.Error(ex);

                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
            }
        }
        public string JumeirahSearchRequest_SigleHotel(string checkin, string checkout, string HotelIds, int NoOfAdult, int NoOfChild)
        {
            try
            {
                StringBuilder REQ = new StringBuilder();
                REQ.Append("hotels/book/v1/" + HotelIds + "/availability?");

                REQ.Append("adults_count=" + NoOfAdult + "");
                REQ.Append("&date_check_in=" + checkin + "");
                REQ.Append("&date_check_out=" + checkout + "");

                if (NoOfChild > 0)
                    REQ.Append("&children_count=" + NoOfChild + "");
                REQ.Append("&promo_code=" + Convert.ToString(JumeirahInfo.promo_codeJumeirah) + "");
                REQ.Append("&group_by=rooms");
                string request = REQ.ToString();
                return request;
            }
            catch (Exception ex)
            {
                return "";
            }
        }

        public async Task<List<Options>> JumeirahPriceOnly_Async(HotelX Item, CommonSearch commonSearch, double AgentPercentage, int SearchTimeOut, string Supplier, DataTable dtCountryCityMarkup, int AgentId, DataSet dsAgentDetails, int SupplierID, string ResponseData, HotelSearchParameter SearchParam, string xmlcity, string xmlcountry, string xmlnationality, string JumeirahCurrency, double CurrencyFactorJumeirah, bool IsShowSupplier, string HotelID, DataTable dtROE, DataTable dtSuplierMarkup, int processTimeout, string ResProccessTimerLogActive, string ResProccessTimeValueforSaveLog, string PassWord)
        {
            System.Diagnostics.Stopwatch timerroomx = new System.Diagnostics.Stopwatch();
            timerroomx.Start();
            List<Options> options = new List<Options>();
            #region DataTable
            DataTable DtHotel = _commonMethod.GenerateDataTableNewV2();
            #endregion
            try
            {
                #region PriceOnly RS 
                if (ResponseData == string.Empty || ResponseData == null || ResponseData.Contains("Error") || ResponseData == "")
                {
                    Item.ResponseStatus = 404;
                    Item.ResponseType = "NoCount";
                    return options;
                }

                Jumeirah.Root JumeirahResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<Jumeirah.Root>(ResponseData);

                if (JumeirahResponse == null || JumeirahResponse.availablity_price == null)
                {
                    Item.ResponseStatus = 404;
                    Item.ResponseType = "NoCount";
                    return options;
                }

                //if (JumeirahResponse == null || JumeirahResponse.Tables["HotelResult"] == null)
                //    return DtHotel;

                //string HotelID = Convert.ToString(distinctValues.Rows[i]["hotel_id"]);
                double MainPrice = 0;
                bool flag = true;
                //string HotelID = JumeirahResponse.availablity_price[0].room_type_code; //Convert.ToInt32(JumeirahResponse.Tables["Hotel"].Rows[h]["Hotel_Id"]);
                for (int h = 0; h < 1; h++)
                {

                    //MainPrice = MainPrice * CurrencyFactorJumeirah;
                    #region "When not Need Room" 
                    if (SearchParam.Filters.IsShowRooms == 0)
                    {
                        for (int i = 0; i < JumeirahResponse.availablity_price.Count; i++)
                        {
                            Item.ResponseType = "Success";
                            if (JumeirahResponse.availablity_price[i].available_plans != null)
                            {
                                if (JumeirahResponse.availablity_price[i].available_plans.Count > 0)
                                {
                                    for (int j = 0; j < JumeirahResponse.availablity_price[i].available_plans.Count; j++)
                                    {
                                        if (JumeirahResponse.availablity_price[i].available_plans[j].no_of_units_available > Convert.ToInt32(SearchParam.Rooms.Count))
                                        {
                                            double Price = Convert.ToDouble(JumeirahResponse.availablity_price[i].available_plans[j].total_amount_after_tax);
                                            string Currency = Convert.ToString(JumeirahResponse.availablity_price[i].available_plans[j].currency_code);

                                            //string HotelId = JumeirahResponse.availablity_price[i].room_type_code;
                                            CurrencyFactorJumeirah = 1;
                                            if (JumeirahCurrency.ToLower() != commonSearch.tokenData.SiteCurrency.ToLower())// || OmnibeesCurrency.ToLower() == "brl")
                                            {
                                                //CurrencyFactorOmnibees = CurrencyFactorOmnibees;
                                                try
                                                {
                                                    CurrencyFactorJumeirah = _commonMethod.GetROEByCurrency(JumeirahCurrency, dtROE);
                                                    //DataTable dtInrCurrency = Common.GetCountryCurrency(OmnibeesCurrency);
                                                    //CurrencyFactorOmnibees = Convert.ToDouble(dtInrCurrency.Rows[0]["AmtToExch"]);
                                                }
                                                catch (Exception)
                                                {
                                                    continue;
                                                }
                                            }

                                            //DataTable dtInrCurrency = Common.GetCountryCurrency(Convert.ToString(Currency));
                                            //CurrencyFactorJumeirah = Convert.ToDouble(dtInrCurrency.Rows[0]["AmtToExch"]);

                                            Price = Price * CurrencyFactorJumeirah;

                                            //MainPrice = i == 0 ? Price : Price < MainPrice ? Price : MainPrice;
                                            if (flag)
                                            {
                                                MainPrice = Price;
                                                flag = false;
                                                JumeirahCurrency = Currency;
                                            }
                                            else
                                            {
                                                if (Price < MainPrice)
                                                {
                                                    MainPrice = Price;
                                                    JumeirahCurrency = Currency;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        if (MainPrice < 0 || MainPrice == 0)
                            return options;
                        DataRow drXmlHotel = DtHotel.NewRow();
                        string XMLCityCode = xmlcity;
                        drXmlHotel["ContractID"] = 0;
                        drXmlHotel["SupplierId"] = SupplierID;
                        drXmlHotel["HotelName"] = "";//Convert.ToString(drGiataDetails[0]["HotelName"]);
                        drXmlHotel["IsShowSupplier"] = IsShowSupplier;
                        drXmlHotel["UniqueNo"] = 0;
                        drXmlHotel["Rating"] = 0;
                        drXmlHotel["Favorite"] = 0;
                        drXmlHotel["VatdiffAmount"] = 0;
                        drXmlHotel["XMLHotelCode"] = HotelID;
                        drXmlHotel["SupplierName"] = Supplier;
                        drXmlHotel["SupplierCurrency"] = JumeirahCurrency;
                        //int OwnsystemHotelId = Convert.ToInt32(dtdetails[0]["OwnsystemHotelId"]);
                        //MainPrice = Common.setHotelListMarkup(SearchParam, dtCountryCityMarkup, Convert.ToInt32(dsAgentDetails.Tables[0].Rows[0]["IsParentId"]), Convert.ToDouble(dsAgentDetails.Tables[0].Rows[0]["B2CMarkupPer"]), AgentId, commonSearch.TariffID, AgentPercentage, lowprice, GiataId, SupplierID, Rating);
                        drXmlHotel["FinalSellingPrice"] = MainPrice;
                        drXmlHotel["SellingPrice"] = MainPrice;
                        drXmlHotel["SupplierId"] = SupplierID;
                        //drXmlHotel["AgentPercentage"] = AgentPercentage;
                        DtHotel.Rows.Add(drXmlHotel);
                        if (MainPrice > 0)
                        {
                            #region "Room code Block"
                            bool willAddOrNot = true;
                            Int64 RaynaID = 0;
                            Int64 GiataId = 0;
                            int Rating = 0;
                            Int64 OwnsystemHotelId = 0;
                            double MinPrice = MainPrice;
                            _markup.GetHotelDataCommon(ref MinPrice, ref willAddOrNot, ref OwnsystemHotelId, ref RaynaID, ref GiataId, ref Rating, ref drXmlHotel, commonSearch.AgentId, commonSearch.TariffID, commonSearch.dsAgentDetails, commonSearch.supplierGiataMaster, SearchParam, commonSearch.dtCountryCityMarkup, commonSearch.B2CMarkup, commonSearch.IsParentId, commonSearch.SubAgentMarkup, commonSearch.SearchByHotel, commonSearch.HotelIDS);
                            if (willAddOrNot)
                            {
                                #region "Room Logic"
                                DataTable dt_HotelRoom = _commonMethod.GenerateRoomtypeDataTableNewV2();
                                DataTable dt_Cancellation = _commonMethod.GenerateCancellationDataTableNewV2();
                                DataTable dt_RateBreackUp = _commonMethod.GenerateoomRateBreackUpDataTableNewV2();
                                #endregion
                                decimal LowestPrice = Convert.ToDecimal(MinPrice).CalculatePrice(commonSearch.tokenData, commonSearch.advancedOptions, JumeirahCurrency);
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
                        //  Int32 ItemCode = JumeirahResponse.ItemCode;// JumeirahResponse[h].OwnsystemHotelId;
                        string XMLCityCode = Convert.ToString(SearchParam.CityID); //xmlcity;
                        string XMLCountry = Convert.ToString(SearchParam.CountryID);
                        MainPrice = 1;
                        drXmlHotel["ContractID"] = 0;
                        drXmlHotel["SupplierId"] = SupplierID;
                        drXmlHotel["HotelName"] = "";//Convert.ToString(drGiataDetails[0]["HotelName"]);
                        drXmlHotel["IsShowSupplier"] = IsShowSupplier;
                        drXmlHotel["UniqueNo"] = 0;
                        drXmlHotel["Rating"] = 0;
                        drXmlHotel["Favorite"] = 0;
                        drXmlHotel["VatdiffAmount"] = 0;
                        // drXmlHotel["XmlHotelCity"] = Convert.ToString(ItemCode);
                        drXmlHotel["XMLHotelCode"] = HotelID;
                        drXmlHotel["SupplierName"] = Supplier;
                        drXmlHotel["SupplierCurrency"] = JumeirahCurrency;
                        //int OwnsystemHotelId = Convert.ToInt32(dtdetails[0]["OwnsystemHotelId"]);
                        //MainPrice = Common.setHotelListMarkup(SearchParam, dtCountryCityMarkup, Convert.ToInt32(dsAgentDetails.Tables[0].Rows[0]["IsParentId"]), Convert.ToDouble(dsAgentDetails.Tables[0].Rows[0]["B2CMarkupPer"]), AgentId, commonSearch.TariffID, AgentPercentage, lowprice, GiataId, SupplierID, Rating);
                        drXmlHotel["FinalSellingPrice"] = MainPrice;
                        drXmlHotel["SellingPrice"] = MainPrice;
                        drXmlHotel["SupplierId"] = SupplierID;
                        //drXmlHotel["AgentPercentage"] = AgentPercentage;
                        DtHotel.Rows.Add(drXmlHotel);
                        if (MainPrice > 0)
                        {
                            #region "Room code Block"
                            bool willAddOrNot = true;
                            Int64 RaynaID = 0;
                            Int64 GiataId = 0;
                            int Rating = 0;
                            int RatingHotel = 0;
                            Int64 OwnsystemHotelId = 0;
                            double MinPrice = MainPrice;
                            _markup.GetHotelDataCommon(ref MinPrice, ref willAddOrNot, ref OwnsystemHotelId, ref RaynaID, ref GiataId, ref Rating, ref drXmlHotel, commonSearch.AgentId, commonSearch.TariffID, commonSearch.dsAgentDetails, commonSearch.supplierGiataMaster, SearchParam, commonSearch.dtCountryCityMarkup, commonSearch.B2CMarkup, commonSearch.IsParentId, commonSearch.SubAgentMarkup, commonSearch.SearchByHotel, commonSearch.HotelIDS);
                            if (willAddOrNot)
                            {
                                #region DataTable
                                DataTable DtJumeirahRoom = _commonMethod.GenerateRoomtypeDataTableNewV2();
                                DataTable dtJumeirahCancellation = _commonMethod.GenerateCancellationDataTableNewV2();
                                DataTable DtJumeirahRateBreackUp = _commonMethod.GenerateoomRateBreackUpDataTableNewV2();
                                #endregion

                                #region Pax Occupancy
                                //int AdultsOcc = 0;
                                //int ChildOcc = 0;
                                //int TotalAdult = 0;
                                //int TotalChild = 0;

                                //for (int a = 0; a < SearchParam.Rooms.Count; a++)
                                //{
                                //    AdultsOcc = SearchParam.Rooms[a].NoofAdults;
                                //    ChildOcc = SearchParam.Rooms[a].NoofChild;
                                //    TotalAdult = TotalAdult + AdultsOcc;
                                //    TotalChild = TotalChild + ChildOcc;
                                //}
                                #endregion

                                double SubAgentMarkup = 0;
                                int CutOffDays = -1;// Convert.ToInt32(XMLHotelBookingCommon.CutOffDays);
                                CutOffDays = _commonMethod.GetCutOffDays(dtSuplierMarkup, Supplier);
                                int IsParentId = 1, agentId = commonSearch.AgentId;
                                //if (dtAgentDetail != null)
                                //{
                                //    IsParentId = Convert.ToInt32(dtAgentDetail.Rows[0]["IsParentId"]);
                                //    SubAgentMarkup = Convert.ToDouble(dtAgentDetail.Rows[0]["B2CMarkupPer"]);
                                //    agentId = Convert.ToInt32(dtAgentDetail.Rows[0]["AgentID"]);
                                //}
                                AgentPercentage = 0.0;
                                Boolean blnIsShowSupplier = false;
                                double AgentMarkupAmount = 0.0;
                                Boolean isPerce = true;
                                int RoomNo = 1;

                                double AgentCompanyPercentage = Convert.ToDouble(SearchParam.AgentCompanyPercentage);
                                bool IsB2C = Convert.ToBoolean(Convert.ToByte(SearchParam.IsB2BOrB2C));

                                double B2CMarkup = (IsB2C == false ? AgentCompanyPercentage : XMLHotelBookingCommon.B2CMarkup);
                                if (IsB2C == true)
                                    AgentPercentage = 0;

                                //_markup.getAgetMarkupSetting(Supplier, GiataId.ToString(), commonSearch.dtSuplierMarkup, commonSearch.dtMarkupDetails, ref AgentPercentage, ref AgentMarkupAmount, ref blnIsShowSupplier, ref isPerce, commonSearch.AgentId, RatingHotel, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), Convert.ToString(SearchParam.CheckInDate));

                                _markup.getAgetMarkupSettingNew_new(Supplier, Convert.ToString(RaynaID), commonSearch.dtSuplierMarkup, commonSearch.dsMarkupDetails, ref AgentPercentage, ref AgentMarkupAmount, ref blnIsShowSupplier, ref isPerce, commonSearch.AgentId, RatingHotel, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), SearchParam.CheckInDate);

                                //if (myReports == null)
                                //    return options;

                                //for (int XX = 0; XX < myReports.Count; XX++)
                                //{
                                //    //Jumeirah.Root JumeirahResponse = JsonConvert.DeserializeObject<Jumeirah.Root>(Response);
                                //    Common.Jumeirah.Root JumeirahResponse = JsonConvert.DeserializeObject<Common.Jumeirah.Root>(myReports[XX].Response);

                                //    if (JumeirahResponse == null || JumeirahResponse.availablity_price == null)
                                //        return options;

                                //    //double MainPrice = 0;

                                string url = "https://services.jumeirah.com";
                                string request = "/hotels/info/v1/" + HotelID + "/rooms";
                                //string ResponseRoomName = GetJumeirahSupplierResponse(url, request, 200000, "Jumeirah", PassWord); //Change 11-feb-2025 Dynemic set room name for Jumeirah
                                string ResponseRoomName = await GetJumeirahSupplierResponseAsync(url, request, 200000, "Jumeirah", PassWord); //Change 11-feb-2025 Dynemic set room name for Jumeirah

                                for (int i = 0; i < JumeirahResponse.availablity_price.Count; i++)
                                {
                                    string room_type_code = "";
                                    if (JumeirahResponse.availablity_price[i].room_type_code != null)
                                        room_type_code = Convert.ToString(JumeirahResponse.availablity_price[i].room_type_code);
                                    else
                                        continue;

                                    //string RoomName = Common.GetJumeirahRoomName(room_type_code, Convert.ToString(HotelID));
                                    string RoomName = "";

                                    Jumeirah.Jumeirahroomname.Rootroomname JumeirahRoomNameResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<Jumeirah.Jumeirahroomname.Rootroomname>(ResponseRoomName);
                                    for (int nn = 0; nn < JumeirahRoomNameResponse.rooms.Count; nn++) 
                                    {
                                        string type_code = "";
                                        string type_name = "";
                                        string room_view_name = "";
                                        if (JumeirahRoomNameResponse.rooms[nn].type_code != null)
                                            type_code = Convert.ToString(JumeirahRoomNameResponse.rooms[nn].type_code);
                                        if (room_type_code == type_code)
                                        {
                                            type_name = Convert.ToString(JumeirahRoomNameResponse.rooms[nn].type_name);
                                            room_view_name = Convert.ToString(JumeirahRoomNameResponse.rooms[nn].room_view_name);
                                            RoomName = type_name + "," + room_view_name;
                                        }

                                    }

                                    if (string.IsNullOrEmpty(RoomName))
                                        continue;

                                    if (JumeirahResponse.availablity_price[i].available_plans != null)
                                    {
                                        Item.ResponseType = "Success";
                                        if (JumeirahResponse.availablity_price[i].available_plans.Count > 0)
                                        {
                                            for (int j = 0; j < JumeirahResponse.availablity_price[i].available_plans.Count; j++)
                                            {
                                                string RoomType = JumeirahResponse.availablity_price[i].room_type_code;
                                                if (JumeirahResponse.availablity_price[i].available_plans[j].no_of_units_available > Convert.ToInt32(SearchParam.Rooms.Count))
                                                {
                                                    MainPrice = Convert.ToDouble(JumeirahResponse.availablity_price[i].available_plans[j].total_amount_after_tax);

                                                    string CurrencyCode = Convert.ToString(JumeirahResponse.availablity_price[i].available_plans[j].currency_code);

                                                    CurrencyFactorJumeirah = 1;
                                                    //if (Convert.ToString(CurrencyCode).ToLower() != "usd")
                                                    //{
                                                    //    DataTable dtInrCurrency = Common.GetCountryCurrency(Convert.ToString(CurrencyCode));
                                                    //    CurrencyFactorJumeirah = Convert.ToDouble(dtInrCurrency.Rows[0]["AmtToExch"]);
                                                    //}

                                                    if (CurrencyCode.ToLower() != commonSearch.tokenData.SiteCurrency.ToLower())
                                                    {
                                                        CurrencyFactorJumeirah = _commonMethod.GetROEByCurrency(CurrencyCode, dtROE);
                                                    }
                                                    if (CurrencyFactorJumeirah == 0)
                                                        break;

                                                    MainPrice = MainPrice * CurrencyFactorJumeirah;

                                                    //string RoomName = Convert.ToString(JumeirahResponse.availablity_price[i].available_plans[j].rate_plan_name);
                                                    string RatePlanCode = Convert.ToString(JumeirahResponse.availablity_price[i].available_plans[j].rate_plan_code);
                                                    RoomType = RoomType + "$$$" + RatePlanCode + "$$$" + HotelID + "$$$" + Guid.NewGuid().ToString();

                                                    string MealPlanCode = Convert.ToString(JumeirahResponse.availablity_price[i].available_plans[j].meal_plan_code);
                                                    string Meal = !string.IsNullOrEmpty(GetJumeirahMeal(MealPlanCode)) ? GetJumeirahMeal(MealPlanCode) : "Room Only";

                                                    #region RateBreakup
                                                    //double FinalPrice = MainPrice / SearchParam.NoofNights;
                                                    //DateTime dtCheckInDateForRate = DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null);
                                                    //#region Vat
                                                    //Double VatOnRateBrackUp = 0;
                                                    //double RoomPrice = _markup.ProcessHotelRateBreakupPrice_WithMarkup(1, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), FinalPrice, B2CMarkup, IsParentId, isPerce, AgentPercentage, AgentMarkupAmount, SubAgentMarkup, ref VatOnRateBrackUp);
                                                    //#endregion
                                                    //for (int night = 0; night < SearchParam.NoofNights; night++)
                                                    //{
                                                    //    string RateWeekColumn = Convert.ToString(dtCheckInDateForRate.Date.AddDays(night).DayOfWeek).Substring(0, 3);
                                                    //    DataRow drJumeirahRateBreakup = DtJumeirahRateBreackUp.NewRow();
                                                    //    drJumeirahRateBreakup["RoomNo"] = Convert.ToString(RoomNo);//RoomNo;
                                                    //    drJumeirahRateBreakup["SupplierName"] = Supplier;
                                                    //    drJumeirahRateBreakup["OwnsystemHotelId"] = OwnsystemHotelId;
                                                    //    drJumeirahRateBreakup["ContractID"] = 0;
                                                    //    drJumeirahRateBreakup["RoomTypeName"] = RoomName;
                                                    //    drJumeirahRateBreakup["RoomTypeId"] = RoomType;
                                                    //    drJumeirahRateBreakup["MealId"] = Meal;
                                                    //    drJumeirahRateBreakup["Meal"] = Meal;
                                                    //    drJumeirahRateBreakup["BookingStatus"] = "Available";
                                                    //    drJumeirahRateBreakup["DateOfWeek"] = RateWeekColumn;
                                                    //    drJumeirahRateBreakup["Date"] = dtCheckInDateForRate.Date.AddDays(night).Date;
                                                    //    drJumeirahRateBreakup["Rate"] = RoomPrice;
                                                    //    drJumeirahRateBreakup["RateType"] = SearchParam.Rooms.Count == 1 ? "A Rate" : ((SearchParam.Rooms.Count) + ". " + RoomName); //"A Rate";
                                                    //    drJumeirahRateBreakup["SupplierCurrency"] = CurrencyCode;
                                                    //    drJumeirahRateBreakup["BuyingPrice"] = RoomPrice;
                                                    //    DtJumeirahRateBreackUp.Rows.Add(drJumeirahRateBreakup);
                                                    //    DtJumeirahRateBreackUp.AcceptChanges();
                                                    //}
                                                    #endregion

                                                    #region Room
                                                    DataRow drJumeirahRoom = DtJumeirahRoom.NewRow();
                                                    drJumeirahRoom["OwnSystemHotelId"] = OwnsystemHotelId;
                                                    drJumeirahRoom["RoomTypeName"] = RoomName;
                                                    drJumeirahRoom["RoomTypeCode"] = RoomType;
                                                    drJumeirahRoom["MealId"] = Meal;
                                                    drJumeirahRoom["MealName"] = Meal;
                                                    drJumeirahRoom["ContractID"] = 0;
                                                    drJumeirahRoom["BoardType"] = Meal;
                                                    drJumeirahRoom["Board"] = Meal;
                                                    drJumeirahRoom["RoomTypeId"] = RoomType;
                                                    drJumeirahRoom["AdultsOccupancy"] = Convert.ToString(SearchParam.Rooms[RoomNo - 1].NoofAdults); //TotalAdult;
                                                    drJumeirahRoom["ChildOccupancy"] = Convert.ToString(SearchParam.Rooms[RoomNo - 1].NoofAdults); //TotalChild;
                                                    drJumeirahRoom["MaxExtraBed"] = "0";
                                                    drJumeirahRoom["RoomNo"] = Convert.ToString(RoomNo); //RoomNo;
                                                    drJumeirahRoom["RoomSequenceNo"] = RoomName;
                                                    drJumeirahRoom["Type"] = RoomType;                          //Rate Key                                        
                                                    drJumeirahRoom["OfficeId"] = 0;
                                                    drJumeirahRoom["BookingStatus"] = "Available";
                                                    drJumeirahRoom["UniqueNo"] = Guid.NewGuid().ToString();
                                                    drJumeirahRoom["OfferDescription"] = "";
                                                    drJumeirahRoom["OfferCode"] = "";
                                                    drJumeirahRoom["EssentialInformation"] = "";
                                                    double ItemPrice = Convert.ToDouble(MainPrice);
                                                    _markup.ProcessHotelRoom_WithMarkup(CurrencyFactorJumeirah, JumeirahCurrency, false, false, SearchParam.NoofNights, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), SearchParam.CheckInDate, SearchParam.CheckOutDate, ref ItemPrice, ref drJumeirahRoom, isPerce, AgentPercentage, AgentMarkupAmount, SubAgentMarkup, B2CMarkup, IsParentId);
                                                    drJumeirahRoom["SupplierName"] = Supplier;
                                                    DtJumeirahRoom.Rows.Add(drJumeirahRoom);
                                                    #endregion

                                                    #region Cancel Policy
                                                    bool CancellationAllowed = Convert.ToBoolean(JumeirahResponse.availablity_price[i].available_plans[j].cancellation_policy.cancellation_allowed);
                                                    string PolicyCode = Convert.ToString(JumeirahResponse.availablity_price[i].available_plans[j].cancellation_policy.policy_code);

                                                    if (CancellationAllowed)
                                                    {
                                                        DateTime CancelDate = DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null);
                                                        if (PolicyCode.Substring(0, 1) != "N")
                                                        {
                                                            int numericValue;
                                                            bool isNumber = int.TryParse(PolicyCode.Split('D')[0], out numericValue);

                                                            if (isNumber)
                                                            {
                                                                string Days = PolicyCode.Split('D')[0];
                                                                if (CancelDate.AddDays(-Convert.ToInt32(Days)) <= DateTime.Now.Date)
                                                                    CancelDate = DateTime.Now.Date;
                                                                else
                                                                    CancelDate = CancelDate.AddDays(-Convert.ToInt32(Days)).Date;

                                                                if (CancelDate.AddDays(Convert.ToInt32(CutOffDays)) <= DateTime.Now.Date)
                                                                    CancelDate = DateTime.Now.Date;
                                                                else
                                                                    CancelDate = CancelDate.AddDays(Convert.ToInt32(CutOffDays)).Date;

                                                                if (CancelDate.AddDays(-1) <= DateTime.Now.Date)
                                                                    CancelDate = DateTime.Now.Date;

                                                                #region Vat
                                                                double VatOnPolicy = 0;
                                                                double MainAgentCharge = MainPrice;
                                                                double CompanyChargeAmount = MainPrice;
                                                                double CancelCharge = _markup.ProcessHotelCancelationPrice_WithMarkup(ref MainAgentCharge, SearchParam.NoofNights, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), MainPrice, isPerce, B2CMarkup, AgentPercentage, AgentMarkupAmount, IsParentId, SubAgentMarkup, ref VatOnPolicy);
                                                                #endregion
                                                                if (CancelDate.Date < DateTime.Now.Date)
                                                                    CancelDate = DateTime.Now.Date;

                                                                if (CancelDate.Date > DateTime.Now.Date)
                                                                {
                                                                    double zero = 0;

                                                                    dtJumeirahCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref zero, dtJumeirahCancellation.NewRow(), CurrencyFactorJumeirah, CurrencyCode, Supplier, Convert.ToString(OwnsystemHotelId), Convert.ToString(RoomNo),
                                                                Convert.ToString(RoomType), RoomName, Meal, DateTime.Now.Date, CancelDate.AddDays(-1), 0, 0, "", VatOnPolicy, "", "NO", "", "", "", ""));
                                                                    dtJumeirahCancellation.AcceptChanges();

                                                                    dtJumeirahCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref CompanyChargeAmount, dtJumeirahCancellation.NewRow(), CurrencyFactorJumeirah, CurrencyCode, Supplier, Convert.ToString(OwnsystemHotelId), Convert.ToString(RoomNo),
                                                                Convert.ToString(RoomType), RoomName, Meal, Convert.ToDateTime(CancelDate), DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null), MainAgentCharge, CancelCharge, "", VatOnPolicy, "", "NO", "", "", "", ""));
                                                                    dtJumeirahCancellation.AcceptChanges();
                                                                }
                                                                else
                                                                {
                                                                    #region Vat
                                                                    VatOnPolicy = 0;
                                                                    MainAgentCharge = MainPrice;
                                                                    CompanyChargeAmount = MainPrice;
                                                                    CancelCharge = _markup.ProcessHotelCancelationPrice_WithMarkup(ref MainAgentCharge, SearchParam.NoofNights, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), MainPrice, isPerce, B2CMarkup, AgentPercentage, AgentMarkupAmount, IsParentId, SubAgentMarkup, ref VatOnPolicy);
                                                                    #endregion

                                                                    dtJumeirahCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref CompanyChargeAmount, dtJumeirahCancellation.NewRow(), CurrencyFactorJumeirah, CurrencyCode, Supplier, Convert.ToString(OwnsystemHotelId), Convert.ToString(RoomNo),
                                                                                                                   Convert.ToString(RoomType), RoomName, Meal, Convert.ToDateTime(DateTime.Now.Date),
                                                                                                                   DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null), CancelCharge, CancelCharge, "", 0, Guid.NewGuid().ToString(), "Yes", "", ""));
                                                                    dtJumeirahCancellation.AcceptChanges();
                                                                }
                                                            }
                                                            else
                                                            {
                                                                #region Vat
                                                                double VatOnPolicy = 0;
                                                                double MainAgentCharge = MainPrice;
                                                                double CompanyChargeAmount = MainPrice;
                                                                double CancelCharge = _markup.ProcessHotelCancelationPrice_WithMarkup(ref MainAgentCharge, SearchParam.NoofNights, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), MainPrice, isPerce, B2CMarkup, AgentPercentage, AgentMarkupAmount, IsParentId, SubAgentMarkup, ref VatOnPolicy);
                                                                #endregion

                                                                dtJumeirahCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref CompanyChargeAmount, dtJumeirahCancellation.NewRow(), CurrencyFactorJumeirah, CurrencyCode, Supplier, Convert.ToString(OwnsystemHotelId), Convert.ToString(RoomNo),
                                                                                                               Convert.ToString(RoomType), RoomName, Meal, Convert.ToDateTime(DateTime.Now.Date),
                                                                                                               DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null), CancelCharge, CancelCharge, "", 0, Guid.NewGuid().ToString(), "Yes", "", ""));
                                                                dtJumeirahCancellation.AcceptChanges();
                                                            }
                                                        }
                                                        else
                                                        {
                                                            #region Vat
                                                            double VatOnPolicy = 0;
                                                            double MainAgentCharge = MainPrice;
                                                            double CompanyChargeAmount = MainPrice;
                                                            double CancelCharge = _markup.ProcessHotelCancelationPrice_WithMarkup(ref MainAgentCharge, SearchParam.NoofNights, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), MainPrice, isPerce, B2CMarkup, AgentPercentage, AgentMarkupAmount, IsParentId, SubAgentMarkup, ref VatOnPolicy);
                                                            #endregion

                                                            dtJumeirahCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref CompanyChargeAmount, dtJumeirahCancellation.NewRow(), CurrencyFactorJumeirah, CurrencyCode, Supplier, Convert.ToString(OwnsystemHotelId), Convert.ToString(RoomNo),
                                                                                                           Convert.ToString(RoomType), RoomName, Meal, Convert.ToDateTime(DateTime.Now.Date),
                                                                                                           DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null), CancelCharge, CancelCharge, "", 0, Guid.NewGuid().ToString(), "Yes", "", ""));
                                                            dtJumeirahCancellation.AcceptChanges();
                                                        }
                                                    }
                                                    else
                                                    {
                                                        #region Vat
                                                        double VatOnPolicy = 0;
                                                        double MainAgentCharge = MainPrice;
                                                        double CompanyChargeAmount = MainPrice;
                                                        double CancelCharge = _markup.ProcessHotelCancelationPrice_WithMarkup(ref MainAgentCharge, SearchParam.NoofNights, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), MainPrice, isPerce, B2CMarkup, AgentPercentage, AgentMarkupAmount, IsParentId, SubAgentMarkup, ref VatOnPolicy);
                                                        #endregion

                                                        dtJumeirahCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref CompanyChargeAmount, dtJumeirahCancellation.NewRow(), CurrencyFactorJumeirah, CurrencyCode, Supplier, Convert.ToString(OwnsystemHotelId), Convert.ToString(RoomNo),
                                                                                                       Convert.ToString(RoomType), RoomName, Meal, Convert.ToDateTime(DateTime.Now.Date),
                                                                                                       DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null), CancelCharge, CancelCharge, "", 0, Guid.NewGuid().ToString(), "Yes", "", ""));
                                                        dtJumeirahCancellation.AcceptChanges();
                                                    }
                                                    #endregion
                                                }
                                            }
                                        }

                                    }
                                }
                                Options ops = (await _supplierRes.GetCommonRoomForSuppliers(GiataId, commonSearch.tokenData, commonSearch.advancedOptions, MinPrice, drXmlHotel, DtJumeirahRoom, dtJumeirahCancellation, DtJumeirahRateBreackUp, SearchParam));
                                if (ops != null)
                                    options.Add(ops);
                            }
                        }
                        #endregion
                    }
                }
                #endregion
                #endregion
            }
            catch (Exception ex)
            {
                Item.ResponseStatus = 404;
                await _exception.Error(ex);
                return null;
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
                        _ = Task.Run(() => _commonMethod.FireAway_SQL_ResPorcessTimeLog("", timerroomx.Elapsed.ToString(), Item.Supplier, SearchParam.SearchKey, 1, SearchParam.AgentID));
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

        public string GetJumeirahMeal(string MealCode)
        {
            string MealToBeSent = "Room Only";
            try
            {
                switch (MealCode)
                {
                    case "0":
                        MealToBeSent = "Room Only";
                        break;
                    case "3":
                        MealToBeSent = "Bed & Breakfast";
                        break;
                    case "12":
                        MealToBeSent = "Half Board";
                        break;
                    default:
                        MealToBeSent = "Room Only";
                        break;
                }
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
            }
            return MealToBeSent;
        }

        public string GetJumeirahSupplierResponse(string strHost, string strxmlRQRS, int TimeOut, string supplier, string Password)
        {
            System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
            timer.Start();


            string strOutput = "";
            try
            {

                HttpWebRequest Request;
                string reqUrl = strHost + strxmlRQRS;
                Request = (HttpWebRequest)WebRequest.Create(reqUrl);
                Request.Timeout = TimeOut;
                Request.Headers.Add("ocp-apim-subscription-key", Password); //DD
                                                                            //Request.Headers.Add("ocp-apim-subscription-key", "379b2d622a45478fbdc90616924caf05"); //WE
                //ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls | SecurityProtocolType.Ssl3;
                Request.Method = "GET";
                Request.UseDefaultCredentials = true;
                Request.PreAuthenticate = true;
                Request.Credentials = CredentialCache.DefaultCredentials;

                try
                {
                    HttpWebResponse Response = (HttpWebResponse)Request.GetResponse();

                    if (Response.ContentEncoding == "gzip")
                    {
                        /* You can put your code to implement decompression */
                        Stream st = Response.GetResponseStream();
                        if (Response.ContentEncoding.ToLower().Contains("gzip"))
                            st = new System.IO.Compression.GZipStream(st, System.IO.Compression.CompressionMode.Decompress);
                        else if (Response.ContentEncoding.ToLower().Contains("deflate"))
                            st = new System.IO.Compression.DeflateStream(st, System.IO.Compression.CompressionMode.Decompress);
                        StreamReader stIn = new StreamReader(st, Encoding.Default);
                        strOutput = stIn.ReadToEnd();
                        stIn.Close();
                        st.Close();
                    }
                    else
                    {
                        StreamReader stIn = new StreamReader(Response.GetResponseStream());
                        strOutput = stIn.ReadToEnd();
                        stIn.Close();

                    }

                    Response.Close();
                    timer.Stop();
                }
                catch (WebException webExcp)
                {
                    HttpWebResponse httpResponse = (HttpWebResponse)webExcp.Response;
                    string result = httpResponse.StatusDescription.ToString();
                    string result1 = new StreamReader(webExcp.Response.GetResponseStream()).ReadToEnd();
                    return result1;
                }
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
            }
            return strOutput;
        }

        public async Task<string> GetJumeirahSupplierResponseAsync(string strHost, string strxmlRQRS, int TimeOut, string supplier, string Password)
        {
            var timer = new System.Diagnostics.Stopwatch();
            timer.Start();

            string strOutput = string.Empty;
            try
            {
                string reqUrl = strHost + strxmlRQRS;

                using (var handler = new HttpClientHandler
                {
                    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                    UseDefaultCredentials = true,
                    PreAuthenticate = true,
                    Credentials = CredentialCache.DefaultCredentials
                })
                using (var client = new HttpClient(handler))
                {
                    client.Timeout = TimeSpan.FromMilliseconds(TimeOut);

                    var request = new HttpRequestMessage(HttpMethod.Get, reqUrl);
                    request.Headers.Add("ocp-apim-subscription-key", Password);

                    HttpResponseMessage response = await client.SendAsync(request);

                    response.EnsureSuccessStatusCode(); // throws if not 2xx

                    strOutput = await response.Content.ReadAsStringAsync();
                }
            }
            catch (HttpRequestException httpEx)
            {
                // You can log or return a specific message
                await _exception.Error(httpEx);
                if (httpEx.InnerException != null)
                    return httpEx.InnerException.Message;
                return httpEx.Message;
            }
            catch (TaskCanceledException ex)
            {
                await _exception.Error(ex);
                return "Request timed out";
            }
            catch (Exception ex)
            {
                await _exception.Error(ex);
                return ex.Message;
            }
            finally
            {
                timer.Stop();
            }

            return strOutput;
        }

        public async Task<PriceCheckCMDto> GetJumeirahPriceCheck(DataSet dsCartDetails, DataRow[] JumeirahDetails, int j, DataTable dtCheckPriceReqStructure, string UniqueNo, string AgentUniqueNo, double MainPrice, string HotelId, int UserCartId, int UserCartDetailId, string RoomTypeId, string SupplierName, double CompanyBuyingPrice, int NoOfAdults, int NoOfChild, string CheckIn, string CheckOut, TokenData tokenData, int AgentId = 0)
        {
            PriceCheckCMDto priceCheckCMDto = new PriceCheckCMDto();

            string time = "";
            string responsetype = "Success";
            string Error = "";
            var xslRequest = "";
            string ResponseData = "";
            try
            {
                string RoomTypeCode = "";
                string RatePlanCode = "";
                try
                {
                    if (RoomTypeId.Contains("$$$"))
                    {
                        string[] Array = RoomTypeId.Split(new String[] { "$$$" }, StringSplitOptions.None);
                        RoomTypeCode = Convert.ToString(Array[0]);
                        RatePlanCode = Convert.ToString(Array[1]);
                    }
                }
                catch (Exception ex1)
                {
                    _ = _exception.Error(ex1);
                }
                StringBuilder REQ = new StringBuilder();
                //REQ.Append("hotels/book/v1/" + HotelId + "/availability?");
                REQ.Append("hotels/book/v1/" + HotelId + "/price_verification?");
                REQ.Append("adults_count=" + NoOfAdults + "");
                REQ.Append("&date_check_in=" + CheckIn + "");
                REQ.Append("&date_check_out=" + CheckOut + "");

                if (NoOfChild > 0)
                    REQ.Append("&children_count=" + NoOfChild + "");
                REQ.Append("&promo_code=" + Convert.ToString(JumeirahInfo.promo_codeJumeirah) + "");
                REQ.Append("&group_by=rooms");

                REQ.Append("&room_type_code=" + RoomTypeCode + "");
                REQ.Append("&rate_plan_code=" + RatePlanCode + "");
                xslRequest = REQ.ToString();
                xslRequest = REQ.ToString();


                if (xslRequest != "")
                {
                    string Url = Convert.ToString(JumeirahDetails[0]["WebSiteURL"]);
                    System.Diagnostics.Stopwatch timerroomprocess = new System.Diagnostics.Stopwatch();
                    timerroomprocess.Start();
                    ResponseDto responseDto = await GetSupplierResponseJumeirah(Convert.ToString(JumeirahDetails[0]["Password"]), xslRequest, Url, "GET", 60000, SupplierName);
                    ResponseData = responseDto.Response;
                    Error = responseDto.Error;
                    timerroomprocess.Stop();
                    time = timerroomprocess.Elapsed.ToString();

                    _commonMethod.ThirdPartyBookingHistory(Convert.ToString(UniqueNo), Convert.ToString(AgentUniqueNo), xslRequest, ResponseData + Error, "After Get Price Jumeirah Request hit booking" + j + "isForBooking" + false, "Jumeirah", "Response Confirm", "-", "RaynaB2B");

                    if (Error.ToLower().Contains("task was canceled") || Error.ToLower().Contains("no response was received during the time-out period for a request") || Error.ToLower().Contains("the request was aborted: the request was canceled."))
                    {
                        try
                        {
                            string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, xslRequest, ResponseData, Error);
                        }
                        catch (Exception e)
                        {
                            _ = _exception.Error(e);
                        }

                        priceCheckCMDto.Flag = true;
                        return priceCheckCMDto;
                    }

                    if (string.IsNullOrEmpty(ResponseData) || ResponseData.Contains("Error"))
                    {
                        responsetype = "GeneralError";
                        priceCheckCMDto.Flag = false;
                        return priceCheckCMDto;
                    }

                    //Jumeirah.Root JumeirahResponse = JsonConvert.DeserializeObject<Jumeirah.Root>(ResponseData);
                    Jumeirah.PriceCheckRoot JumeirahResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<Jumeirah.PriceCheckRoot>(ResponseData);

                    //if (JumeirahResponse == null || JumeirahResponse.availablity_price == null)
                    //{
                    //    responsetype = "GeneralError";
                    //    return false;
                    //}

                    if (JumeirahResponse == null || JumeirahResponse.price_check.total_amount_after_tax == null)
                    {
                        #region SupplierPR Price Check
                        try
                        {
                            responsetype = "GeneralError";
                            string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, xslRequest, ResponseData, Error);
                        }
                        catch (Exception ex)
                        {
                            _ = _exception.Error(ex);
                            //ExceptionManager.LogException(ex);
                        }
                        #endregion
                        priceCheckCMDto.Flag = false;
                        return priceCheckCMDto;
                    }

                    if (Convert.ToString(ResponseData.Trim()) != string.Empty && JumeirahResponse.success)
                    {
                        try
                        {
                            //double MainPrice = 0;
                            double Price = Convert.ToDouble(JumeirahResponse.price_check.total_amount_after_tax);
                            string CurrencyCode = Convert.ToString(JumeirahResponse.price_check.currency_code);
                            double CurrencyFactorJumeirah = 1;
                            if (Convert.ToString(CurrencyCode).ToLower() != tokenData.SiteCurrency.ToLower())
                            {
                                CurrencyFactorJumeirah = _commonMethod.GetROEByCurrency(CurrencyCode, tokenData.dtROE);
                            }
                            MainPrice = Price * CurrencyFactorJumeirah;

                            priceCheckCMDto.ItemPrice = MainPrice;

                            if ((MainPrice).TruncateDecimalPlaces(0) > (CompanyBuyingPrice).TruncateDecimalPlaces(0))
                            {
                                #region SupplierPR Price Check
                                try
                                {
                                    ResponseData = "#Price Changed " + "#Main Price = " + CompanyBuyingPrice + " #New Price = " + MainPrice + " #Response= " + ResponseData;
                                    responsetype = "GeneralError";
                                    string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, xslRequest, ResponseData, Error);
                                }
                                catch (Exception ex)
                                {
                                    _ = _exception.Error(ex);
                                    //ExceptionManager.LogException(ex);
                                }
                                #endregion
                                priceCheckCMDto.Flag = true;
                                return priceCheckCMDto;
                            }
                            else
                            {
                                priceCheckCMDto.Flag = false;
                                return priceCheckCMDto;
                            }
                        }
                        catch (Exception ex)
                        {
                            try
                            {
                                MainPrice = 0;
                                Error = ex.Message.ToString() + "#=" + ex.StackTrace.ToString();
                                responsetype = "GeneralError";
                                string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook("Jumeirah", "PreBook", AgentId, time, responsetype, xslRequest, ResponseData, Error);
                            }
                            catch (Exception e)
                            {
                                _ = _exception.Error(ex);
                                //ExceptionManager.LogException(e);
                            }
                            _ = _exception.Error(ex);
                            //ExceptionManager.LogException(ex);
                            priceCheckCMDto.Flag = true;
                            return priceCheckCMDto;
                        }
                    }
                    else
                    {
                        try
                        {
                            responsetype = "GeneralError";
                            string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook("Jumeirah", "PreBook", AgentId, time, responsetype, xslRequest, ResponseData, Error);
                        }
                        catch (Exception e)
                        {
                            _ = _exception.Error(e);
                            //ExceptionManager.LogException(e);
                        }
                        MainPrice = 0;
                        priceCheckCMDto.ItemPrice = MainPrice;
                        priceCheckCMDto.Flag = true;
                        return priceCheckCMDto;
                    }
                }
            }
            catch (Exception ex)
            {
                #region SupplierPerformanceReport Catch
                try
                {
                    Error = ex.Message + ex.StackTrace.ToString();
                    responsetype = "GeneralError";
                    string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook("Jumeirah", "PreBook", AgentId, time, responsetype, xslRequest, ResponseData, Error);
                }
                catch (Exception e)
                {
                    _ = _exception.ErrorBooking(e, AgentId.ToString(), "", "");
                }
                #endregion
                _ = _exception.Error(ex);
            }
            priceCheckCMDto.Flag = false;
            return priceCheckCMDto;
        }

        public async Task<ResponseDto> GetSupplierResponseJumeirah(string Password, string strxmlRQRS, string strHost, string Action, int TimeOut, string Provider = "")
        {
            System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
            timer.Start();
            ResponseDto responseDto = new ResponseDto();
            string Error = "";
            string strOutput = "";
            try
            {

                HttpWebRequest Request;
                string reqUrl = strHost + strxmlRQRS;
                Request = (HttpWebRequest)WebRequest.Create(reqUrl);
                Request.Timeout = TimeOut;
                Request.Headers.Add("ocp-apim-subscription-key", Password); //DD
                                                                            //Request.Headers.Add("ocp-apim-subscription-key", "379b2d622a45478fbdc90616924caf05"); //WE
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls | SecurityProtocolType.Ssl3;
                Request.Method = Action;
                Request.UseDefaultCredentials = true;
                Request.PreAuthenticate = true;
                Request.Credentials = CredentialCache.DefaultCredentials;
                //Stream newStream = null;

                //byte[] data = Encoding.ASCII.GetBytes(strxmlRQRS);
                //Request.ContentLength = data.Length;

                //newStream = Request.GetRequestStream();
                //newStream.Write(data, 0, data.Length);

                try
                {
                    HttpWebResponse Response = (HttpWebResponse)await Request.GetResponseAsync();
                    Stream responseStream = Response.GetResponseStream();
                    //responseStream = new GZipStream(responseStream, CompressionMode.Decompress);
                    StreamReader Reader = new StreamReader(responseStream, Encoding.UTF8);

                    strOutput = await Reader.ReadToEndAsync();

                    responseStream.Close();
                    Reader.Close();
                    Response.Close();
                    timer.Stop();
                    //if (para != null)
                    //{
                    //    para.SupplierTime += ",SupplierName :" + Provider + " : " + "Execution Time :" + timer.Elapsed.ToString();
                    //    if (DebugMode)
                    //        Common.XMLRequestResponseTrack(strxmlRQRS, strOutput, Provider, "Proper");
                    //}
                }
                catch (WebException webExcp)
                {
                    HttpWebResponse httpResponse = (HttpWebResponse)webExcp.Response;
                    string result = httpResponse.StatusDescription.ToString();
                    string result1 = new StreamReader(webExcp.Response.GetResponseStream()).ReadToEnd();
                    Error = result1;
                    _ = _commonMethod.XMLRequestResponseTrackError(strxmlRQRS, result1, Provider, "Proper");

                    responseDto.Error = Error;
                    return responseDto;
                }
            }
            catch (Exception ex)
            {
                Error = ex.Message;
                _ = _exception.Error(ex);

                responseDto.Error = Error;
                return responseDto;
            }
            responseDto.Response = strOutput;
            return responseDto;
        }
    }
}
