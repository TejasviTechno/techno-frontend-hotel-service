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
using XConnect.Supplier.Rakuten.Interface;
using Microsoft.Extensions.Options;
using XConnect.ErrorLogHandler.Interface;
using XConnect.Markup.Interface;
using static XConnect.Supplier.Rakuten.Rakuten.RakutenClass;
using Options = XConnect.Models.Options;
using Microsoft.IdentityModel.Protocols;
using static Azure.Core.HttpHeader;
using System.Net;
using Microsoft.Extensions.Configuration;
using static XConnect.Supplier.Booking.Com.preebookrs;

namespace XConnect.Supplier.Rakuten
{
    public class RakutenRepo : IRakuten
    {
        private readonly ICommonMethod _commonMethod;
        private readonly IException _exception;
        private readonly IMarkup _markup;
        private readonly ISupplierRes _supplierRes;
        private readonly KeySettings _keySettings;

        public RakutenRepo(ICommonMethod commonMethod, IMarkup markup, ISupplierRes supplierRes, IException exception, IOptions<KeySettings> keySettings)
        {
            _commonMethod = commonMethod;
            _markup = markup;
            _supplierRes = supplierRes;
            _exception = exception;
            _keySettings = keySettings.Value;
        }
        public void RakutenRequestOnly_async(DataRow[] commonCode, List<StarRating> listRating, HotelSearchParameter SearchPara, List<SupplierDetails> lstRakutenDetails, List<SupplierSearchConfig> lstRakutenSearchConfig, DataTable dtPreferredDataHotel, ref ConcurrentBag<HotelX> AllTask, int Hit, DataTable dtROE, TokenData tokenData, bool HitFromAll, DataSet dtXMLCity)
        {
            try
            {
                if (lstRakutenDetails != null && lstRakutenDetails.Count > 0)
                {


                    List<StarRating> lstRating = new List<StarRating>();
                    lstRating = _commonMethod.GetStarRatingStringList_xtg(SearchPara);

                    List<HotelCodeByCity> DtHotelDAta = HtmlExtensions.ToList<HotelCodeByCity>(commonCode.CopyToDataTable());
                    int ratingcount = lstRating.Count;
                    int hotelcount = DtHotelDAta.Count;

                    foreach (SupplierDetails RakutenDetails in lstRakutenDetails)
                    {
                        try
                        {
                            int SupplierID = RakutenDetails.SupplierID;
                            string Supplier = RakutenDetails.SupplierName;
                            //DataRow[] commonCode = null;

                            if (commonCode != null)
                            {
                                if (lstRakutenSearchConfig.Count > 0)
                                {
                                    List<SupplierSearchConfig> RakutenSearchConfig = lstRakutenSearchConfig
                                        .Where(x => x != null && x.SupplierID == SupplierID)
                                        .ToList();

                                    if (RakutenSearchConfig != null && RakutenSearchConfig.Count > 0)
                                    {
                                        if ((SearchPara.IsSearchFor.ToLower().IndexOf(Supplier.ToLower()) >= 0 || SearchPara.IsSearchFor.ToLower().IndexOf("multi") >= 0))
                                        {
                                            DataForSearch dataForSearch = _commonMethod.GetXmlDataForSearchTechwise(SearchPara.Nationality, dtXMLCity, Supplier, RakutenDetails.TechName);

                                            string XmlNationality = "";
                                            XmlNationality = dataForSearch.XmlNationality;

                                            if (!string.IsNullOrEmpty(XmlNationality))
                                            {
                                                DataRow[] drRakuten = dtPreferredDataHotel.Select("ServiceName='" + Supplier + "'");
                                                string Ids = string.Empty;
                                                string xmlbase = "";
                                                _commonMethod.GetPrefferedHotelCodes(ref Ids, ref xmlbase, ref RakutenSearchConfig, Supplier, dtPreferredDataHotel, SearchPara.IsSearchFor);

                                                List<HotelCodeByCity> filter = new List<HotelCodeByCity>();
                                                filter = _commonMethod.FilterWithOutCity_Code(ratingcount, hotelcount, DtHotelDAta, lstRating, Supplier);


                                                if (filter.Count > 0)
                                                {
                                                    string Currency = RakutenDetails.CurrencyShortName;
                                                    double CurrencyFactorRakuten = 1;
                                                    if (Currency.ToLower() != tokenData.SiteCurrency.ToLower())
                                                    {
                                                        CurrencyFactorRakuten = _commonMethod.GetROEByCurrency(Currency, dtROE);
                                                    }


                                                    if (SearchPara.IsSearchFor.ToLower().IndexOf("preferred") >= 0 && drRakuten.Count() == 0)
                                                    {
                                                        RakutenSearchConfig = null;
                                                    }

                                                    for (int k = 0; k < RakutenSearchConfig.Count; k++)
                                                    {
                                                        bool DebugMode = (Convert.ToInt32(RakutenSearchConfig[k].DebugMode) == 0 ? false : true);
                                                        string UserName = Convert.ToString(RakutenDetails.UserName);
                                                        string Password = Convert.ToString(RakutenDetails.Password);
                                                        {
                                                            int IsCitywisesearch = Convert.ToInt32(RakutenSearchConfig[k].City);

                                                            if (Convert.ToInt32(RakutenSearchConfig[k].CityID) == 0 || Convert.ToInt32(RakutenSearchConfig[k].CityID) == SearchPara.CityID)
                                                            {
                                                                if (commonCode.Length > 0)
                                                                {
                                                                    if (filter.Count > 0)
                                                                    {
                                                                        int NoOfRecord = Convert.ToInt32(RakutenSearchConfig[k].Noofrecords);
                                                                        double count = Convert.ToDouble(filter.Count);
                                                                        int NoOfDiv = Convert.ToInt32(Math.Ceiling(count / Convert.ToDouble(NoOfRecord)));
                                                                        List<List<XMLID_Search>> lstxml = _commonMethod.GetXMLIDString(NoOfDiv, NoOfRecord, count, filter);


                                                                        if (SearchPara.Filters.IsShowRooms == 0)
                                                                        {
                                                                            for (int a = 0; a < NoOfDiv; a++)
                                                                            {
                                                                                string IdsForSearch = Convert.ToString(_commonMethod.GetHotelCodeByCOmma(lstxml[a]));
                                                                                HotelX Xhotel = new HotelX();
                                                                                Xhotel.Supplier = Supplier;
                                                                                Xhotel.DebugeMode = DebugMode;
                                                                                Xhotel.SearchTimeOut = Convert.ToInt32(RakutenSearchConfig[k].HotelSearchTimeOut);
                                                                                //if (Hit > 0)
                                                                                Xhotel.ProcessTimeout = Convert.ToInt32(RakutenSearchConfig[k].HotelSearchTimeOut2);
                                                                                if (_keySettings.SupplierProxy == "0")
                                                                                    Xhotel.URL = Convert.ToString(RakutenDetails.WebSiteURL);
                                                                                else
                                                                                    Xhotel.URL = Convert.ToString(RakutenDetails.WebSiteURLforAPI);
                                                                                Xhotel.UserId = Convert.ToString(RakutenDetails.UserID);
                                                                                Xhotel.UserName = UserName;
                                                                                Xhotel.PassWord = Password;
                                                                                Xhotel.CurrencyCode = Currency;
                                                                                Xhotel.CurrencyFactor = CurrencyFactorRakuten;
                                                                                Xhotel.XmlCity = "";
                                                                                Xhotel.XmlCountry = "";
                                                                                Xhotel.XmlNationality = XmlNationality;
                                                                                string PaxPassport = XmlNationality;
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
                                                                                    objrq.Request = RakutenSearchRequest(Xhotel.URL, SearchPara, checkin, checkout, IdsForSearch, XmlNationality, Currency, 1);
                                                                                    objrq.Response = "";
                                                                                    RQRS.Add(objrq);
                                                                                }
                                                                                Xhotel.RQRS = RQRS;
                                                                                Xhotel.ReturnRooms = 5; // Convert.ToInt32(JuniperChild[x].ReturnRooms);
                                                                                Xhotel.TechName = Supplier; // Convert.ToString(JuniperChild[x].TechName);
                                                                                #endregion
                                                                                AllTask.Add(Xhotel);
                                                                            }
                                                                        }
                                                                        else  // && count == 1)
                                                                        {
                                                                            for (int a = 0; a < NoOfDiv; a++)
                                                                            {
                                                                                string IdsForSearch = Convert.ToString(_commonMethod.GetHotelCodeByCOmma(lstxml[a]));

                                                                                HotelX Xhotel = new HotelX();
                                                                                Xhotel.Supplier = Supplier;
                                                                                Xhotel.DebugeMode = DebugMode;
                                                                                Xhotel.SearchTimeOut = Convert.ToInt32(RakutenSearchConfig[k].HotelSearchTimeOut);
                                                                                //if (Hit > 0)
                                                                                Xhotel.ProcessTimeout = Convert.ToInt32(RakutenSearchConfig[k].HotelSearchTimeOut2);
                                                                                if (_keySettings.SupplierProxy == "0")
                                                                                    Xhotel.URL = Convert.ToString(RakutenDetails.WebSiteURL);
                                                                                else
                                                                                    Xhotel.URL = Convert.ToString(RakutenDetails.WebSiteURLforAPI);

                                                                                Xhotel.UserId = Convert.ToString(RakutenDetails.UserID);
                                                                                Xhotel.UserName = UserName;
                                                                                Xhotel.PassWord = Password;
                                                                                Xhotel.CurrencyCode = Currency;
                                                                                Xhotel.CurrencyFactor = CurrencyFactorRakuten;
                                                                                Xhotel.XmlCity = "";
                                                                                Xhotel.XmlCountry = "";
                                                                                Xhotel.XmlNationality = XmlNationality;
                                                                                string PaxPassport = XmlNationality;
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
                                                                                    objrq.Request = RakutenSearchRequest(Xhotel.URL, SearchPara, checkin, checkout, IdsForSearch, XmlNationality, Currency, 2);
                                                                                    objrq.Response = "";
                                                                                    RQRS.Add(objrq);
                                                                                }
                                                                                Xhotel.RQRS = RQRS;
                                                                                Xhotel.TechName = Supplier; // Convert.ToString(JuniperChild[x].TechName);
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

        public string RakutenSearchRequest(string Url, HotelSearchParameter searchParam, string checkin, string checkout, string HotelIds, string RakutenNationality, string CurrencyCode, int reqtype)
        {
            try
            {

                #region Adults Child Count

                int NoOfRooms = searchParam.Rooms.Count();
                int NoofAdults = 0;
                string ChildAge = "";
                for (int ser = 0; ser < searchParam.Rooms.Count; ser++)
                {
                    if (ser == 0)
                        NoofAdults = searchParam.Rooms[ser].NoofAdults;
                    else
                        NoofAdults = NoofAdults + searchParam.Rooms[ser].NoofAdults;

                    switch (searchParam.Rooms[ser].NoofChild)
                    {
                        case 1:
                            if (ChildAge == "")
                            {
                                ChildAge += searchParam.Rooms[ser].Child1Age;
                            }
                            else
                            {
                                ChildAge += "," + searchParam.Rooms[ser].Child1Age;
                            }
                            break;
                        case 2:

                            if (ChildAge == "")
                            {
                                ChildAge += searchParam.Rooms[ser].Child1Age;
                            }
                            else
                            {
                                ChildAge += "," + searchParam.Rooms[ser].Child1Age;
                            }

                            if (ChildAge == "")
                            {
                                ChildAge += searchParam.Rooms[ser].Child2Age;
                            }
                            else
                            {
                                ChildAge += "," + searchParam.Rooms[ser].Child2Age;
                            }
                            break;
                    }
                }
                #endregion

                StringBuilder rakutenhotellist = new StringBuilder();
                if (HotelIds != string.Empty)
                {
                    string HIds = "";
                    string[] Hotelid = HotelIds.Split(',');
                    for (int i = 0; i < Hotelid.Length; i++)
                    {
                        if (i == 0)
                            HIds = Hotelid[i];
                        else
                            HIds = HIds + "," + Hotelid[i];
                    }
                    switch (reqtype)
                    {
                        case 1:
                            rakutenhotellist.Append(Url + RakutenInfo.Rakuten_HotelSearch);//Convert.ToString(ConfigurationManager.AppSettings["Rakuten_HotelSearch"]));
                            rakutenhotellist.Append("?hotel_id_list=" + HIds + "");
                            break;
                        case 2:
                            rakutenhotellist.Append(Url + RakutenInfo.Rakuten_RoomList);//Convert.ToString(ConfigurationManager.AppSettings["Rakuten_RoomList"]));
                            rakutenhotellist.Append("?hotel_id=" + HIds + "");
                            break;
                    }

                    rakutenhotellist.Append("&check_in_date=" + checkin + "");
                    rakutenhotellist.Append("&check_out_date=" + checkout + "");
                    rakutenhotellist.Append("&adult_count=" + NoofAdults + "");
                    rakutenhotellist.Append("&room_count=" + NoOfRooms + "");
                    rakutenhotellist.Append("&currency=" + CurrencyCode + "");
                    rakutenhotellist.Append("&source_market=" + RakutenNationality + "");
                    rakutenhotellist.Append("&locale=en-US");
                    if (ChildAge != "")
                        rakutenhotellist.Append("&children=" + ChildAge + "");
                }
                string rakutenReq = rakutenhotellist.ToString();
                return rakutenReq;
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
            }
            return "";
        }

        public async Task<List<Options>> Rakuten_HotelSearchWithRooms_bck_150725(HotelX Item, CommonSearch commonSearch, string Currency, string ResponseData, string CityCode, HotelSearchParameter SearchParam, double CurrencyFactorTBO, string XMLnationality, string URLandRequest, string UserName, string PassWord, string URL, Int32 SearchTimeOut, string custIP, Int32 processTimeout, string ResProccessTimerLogActive, string ResProccessTimeValueforSaveLog)
        {
            System.Diagnostics.Stopwatch timerroomx = new System.Diagnostics.Stopwatch();
            timerroomx.Start();
            List<Options> options = new List<Options>();
            try
            {
                #region DataTable
                DataTable DtHotel = _commonMethod.GenerateDataTableNewV2();
                #endregion
                if (ResponseData != string.Empty)
                {
                    double TotalCharges = 0;
                    string Provider = Item.Supplier;

                    if (SearchParam.Filters.IsShowRooms == 0)
                    {

                        #region "When No Need Rooms"
                        RakutenHotelListResponse.Root RakutenResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<RakutenHotelListResponse.Root>(ResponseData);
                        if (RakutenResponse == null)
                        {
                            Item.ResponseType = "NoCount";
                            return options;
                        }

                        if (RakutenResponse.hotels.Count < 1)
                        {
                            Item.ResponseType = "NoCount";
                        }

                        for (int u = 0; u < RakutenResponse.hotels.Count; u++)
                        {
                            string ItemCode = Convert.ToString(RakutenResponse.hotels[u].id);
                            string HotelName = Convert.ToString(RakutenResponse.hotels[u].id);

                            Item.ResponseType = "Success";

                            DataRow drXmlHotel = DtHotel.NewRow();
                            //string InternalHotelId = Convert.ToString(ItemCode);
                            drXmlHotel["XMLHotelCode"] = ItemCode;
                            drXmlHotel["HotelName"] = HotelName;
                            drXmlHotel["SupplierName"] = Provider;
                            drXmlHotel["OfficeID"] = 0;
                            drXmlHotel["ContractID"] = 0;
                            drXmlHotel["Rating"] = 0;
                            drXmlHotel["Reviews"] = 0;
                            drXmlHotel["UniqueNo"] = ItemCode;
                            drXmlHotel["IsShowSupplier"] = false;
                            drXmlHotel["BookingStatus"] = "Available";
                            drXmlHotel["XMLCity"] = CityCode;
                            drXmlHotel["SupplierCurrency"] = Currency;
                            drXmlHotel["AvailToken"] = "";

                            double Factor = 1;
                            string currencycode = Convert.ToString(RakutenResponse.hotels[u].rates.packages[0].chargeable_rate_currency);

                            if (currencycode != commonSearch.tokenData.SiteCurrency.ToLower())
                            {
                                Factor = _commonMethod.GetROEByCurrency(Currency, commonSearch.tokenData.dtROE);
                            }

                            double FinalTotal = Convert.ToDouble(RakutenResponse.hotels[u].rates.packages[0].chargeable_rate) * Factor;

                            drXmlHotel["FinalSellingPrice"] = FinalTotal;
                            drXmlHotel["SellingPrice"] = FinalTotal;
                            DtHotel.Rows.Add(drXmlHotel);
                            TotalCharges = FinalTotal;

                            if (TotalCharges > 0)
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
                                    decimal LowestPrice = Convert.ToDecimal(MinPrice).CalculatePrice(commonSearch.tokenData, commonSearch.advancedOptions, Currency);
                                    Options ops = (await _supplierRes.GetCommonRoomForSuppliers(GiataId, commonSearch.tokenData, commonSearch.advancedOptions, Convert.ToDouble(LowestPrice), drXmlHotel, dt_HotelRoom, dt_Cancellation, dt_RateBreackUp, SearchParam));
                                    if (ops != null)
                                        options.Add(ops);
                                }
                                #endregion
                            }

                        }
                        #endregion
                    }
                    else
                    {
                        #region "When Need Rooms"
                        RakutenHotelListResponse.Root RakutenResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<RakutenHotelListResponse.Root>(ResponseData);
                        if (RakutenResponse == null)
                            return options;

                        if (RakutenResponse.hotels.Count < 1)
                            Item.ResponseType = "NoCount";

                        for (int i = 0; i < RakutenResponse.hotels.Count; i++)
                        {
                            string ItemCode = Convert.ToString(RakutenResponse.hotels[i].id);
                            string HotelName = Convert.ToString(RakutenResponse.hotels[i].id);
                            for (int j = 0; j < RakutenResponse.hotels[i].rates.packages.Count; j++)
                            {
                                Item.ResponseType = "Success";

                                double FinalMinPrice = 0;
                                #region Hotels
                                #region "Hotel Block here"
                                DataRow drXmlHotel = DtHotel.NewRow();
                                //string InternalHotelId = Convert.ToString(ItemCode);
                                drXmlHotel["XMLHotelCode"] = ItemCode;
                                drXmlHotel["HotelName"] = HotelName;
                                drXmlHotel["SupplierName"] = Provider;
                                drXmlHotel["OfficeID"] = 0;
                                drXmlHotel["ContractID"] = 0;
                                drXmlHotel["Rating"] = 0;
                                drXmlHotel["Reviews"] = 0;
                                drXmlHotel["UniqueNo"] = ItemCode;
                                drXmlHotel["IsShowSupplier"] = false;
                                drXmlHotel["BookingStatus"] = "Available";
                                drXmlHotel["XMLCity"] = CityCode;
                                drXmlHotel["SupplierCurrency"] = Currency;
                                drXmlHotel["AvailToken"] = "";

                                double Factor = 1;
                                string currencycode = Convert.ToString(RakutenResponse.hotels[i].rates.packages[j].chargeable_rate_currency);

                                if (currencycode != commonSearch.tokenData.SiteCurrency.ToLower())
                                {
                                    Factor = _commonMethod.GetROEByCurrency(Currency, commonSearch.tokenData.dtROE);
                                }
                                double FinalTotal = Convert.ToDouble(RakutenResponse.hotels[i].rates.packages[j].chargeable_rate) * Factor;

                                drXmlHotel["FinalSellingPrice"] = FinalTotal;
                                drXmlHotel["SellingPrice"] = FinalTotal;
                                DtHotel.Rows.Add(drXmlHotel);
                                TotalCharges = FinalTotal;
                                #endregion

                                #region "GetRooms for this hotel"
                                bool willAddOrNot = true;
                                Int64 RaynaID = 0;
                                Int64 GiataId = 0;
                                int RatingHotel = 0;
                                Int64 OwnsystemHotelId = 0;
                                double MinPrice = 1;

                                _markup.GetHotelDataCommon(ref MinPrice, ref willAddOrNot, ref OwnsystemHotelId, ref RaynaID, ref GiataId, ref RatingHotel, ref drXmlHotel, commonSearch.AgentId, commonSearch.TariffID, commonSearch.dsAgentDetails, commonSearch.supplierGiataMaster, SearchParam, commonSearch.dtCountryCityMarkup, commonSearch.B2CMarkup, commonSearch.IsParentId, commonSearch.SubAgentMarkup, commonSearch.SearchByHotel, commonSearch.HotelIDS);
                                if (willAddOrNot)
                                {
                                    DataTable DtRakuten_HotelRoom = _commonMethod.GenerateRoomtypeDataTableNewV2();
                                    DataTable dtRakuten_Cancellation = _commonMethod.GenerateCancellationDataTableNewV2();
                                    DataTable DtRakuten_RateBreackUp = _commonMethod.GenerateoomRateBreackUpDataTableNewV2();
                                    int CutOffDays = CacheMaster.CutOffDays;
                                    CutOffDays = _commonMethod.GetCutOffDays(commonSearch.dtSuplierMarkup, Provider);
                                    #region "Room Logic"

                                    string Type = null;
                                    double SubAgentMarkup = 0;

                                    int IsParentId = 1, agentid = 0;
                                    double AgentPercentage = 0.0;
                                    double AgentMarkupAmount = 0.0;
                                    Boolean isPerce = true;

                                    double AgentCompanyPercentage = Convert.ToDouble(SearchParam.AgentCompanyPercentage);
                                    bool IsB2C = Convert.ToBoolean(Convert.ToByte(SearchParam.IsB2BOrB2C));
                                    bool blnIsShowSupplier = false;
                                    _markup.getAgetMarkupSetting(Provider, GiataId.ToString(), commonSearch.dtSuplierMarkup, commonSearch.dtMarkupDetails, ref AgentPercentage, ref AgentMarkupAmount, ref blnIsShowSupplier, ref isPerce, commonSearch.AgentId, RatingHotel, SearchParam.CityID, SearchParam.CountryID.ToString(), SearchParam.CheckInDate);
                                    double B2CMarkup = (IsB2C == false ? AgentCompanyPercentage : commonSearch.B2CMarkup);
                                    #region Supplier Room Code block

                                    DataRow[] drHotelDetails = DtHotel.Select("OwnsystemHotelId='" + OwnsystemHotelId + "'");
                                    string[] HotelDetails = Convert.ToString(drHotelDetails[0]["AvailToken"]).Split(':');

                                    string Error = "";
                                    int TotalAdult = 0;
                                    int TotalChild = 0;
                                    for (int ad = 0; ad < SearchParam.Rooms.Count; ad++)
                                    {
                                        TotalAdult += SearchParam.Rooms[ad].NoofAdults;
                                        TotalChild += SearchParam.Rooms[ad].NoofChild;
                                    }



                                    string RoomCode = Convert.ToString(RakutenResponse.hotels[i].rates.packages[j].room_details.rate_plan_code);
                                    // string RoomName = Convert.ToString(RakutenResponse.hotels[i].rates.packages[j].room_details.room_type);
                                    string RoomName = Convert.ToString(RakutenResponse.hotels[i].rates.packages[j].room_details.supplier_description);
                                    string bookingKey = Convert.ToString(RakutenResponse.hotels[i].rates.packages[j].booking_key);
                                    int meal = Convert.ToInt32(RakutenResponse.hotels[i].rates.packages[j].room_details.food);
                                    string MealId = Convert.ToString(GetMealName(meal));
                                    string MealName = Convert.ToString(GetMealName(meal));
                                    double NetPrice = Convert.ToDouble(RakutenResponse.hotels[i].rates.packages[j].chargeable_rate);


                                    if ((Convert.ToString(RakutenResponse.hotels[i].rates.packages[j].chargeable_rate_currency)).ToLower() != commonSearch.tokenData.SiteCurrency.ToLower())
                                    {
                                        //DataTable dtInrCurrency = Common.GetCountryCurrency(Convert.ToString(RakutenResponse.hotels[i].rates.packages[j].chargeable_rate_currency).ToLower());
                                        Factor = _commonMethod.GetROEByCurrency(Convert.ToString(RakutenResponse.hotels[i].rates.packages[j].chargeable_rate_currency), commonSearch.tokenData.dtROE); // Convert.ToDouble(dtInrCurrency.Rows[0]["AmtToExch"]);
                                    }
                                    NetPrice = NetPrice * Factor;

                                    string RoomCodeMain = RoomCode + "$$$" + ItemCode + "$$$" + RoomName + "$$$" + meal + "$$$" + bookingKey + "$$$" + XMLnationality + "$$$" + NetPrice;
                                    string RateKeyMain = "";



                                    string EssentialInfo = Convert.ToString(RakutenResponse.hotels[i].rates.packages[j].room_details.supplier_description);

                                    string RoomTypeId = RoomCodeMain;

                                    RoomTypeId = RoomTypeId.Replace("'", "");

                                    #region Room
                                    DataRow drRakutenRoom = DtRakuten_HotelRoom.NewRow();
                                    drRakutenRoom["OwnSystemHotelId"] = OwnsystemHotelId;
                                    drRakutenRoom["RoomTypeName"] = RoomName;
                                    drRakutenRoom["RoomTypeCode"] = RoomCode;
                                    drRakutenRoom["MealId"] = MealId;
                                    drRakutenRoom["MealName"] = MealName;
                                    drRakutenRoom["ContractID"] = 0;
                                    drRakutenRoom["BoardType"] = MealName;
                                    drRakutenRoom["Board"] = MealId;
                                    drRakutenRoom["RoomTypeId"] = RoomTypeId;
                                    drRakutenRoom["AdultsOccupancy"] = TotalAdult;
                                    drRakutenRoom["ChildOccupancy"] = TotalChild;
                                    drRakutenRoom["MaxExtraBed"] = "0";
                                    drRakutenRoom["RoomNo"] = "1";// j + 1;//RoomNo;
                                    drRakutenRoom["RoomSequenceNo"] = RoomName;
                                    drRakutenRoom["Type"] = RoomCodeMain;                          //Rate Key                                        
                                    drRakutenRoom["OfficeId"] = 0;
                                    drRakutenRoom["BookingStatus"] = "Available";
                                    drRakutenRoom["UniqueNo"] = Guid.NewGuid().ToString();
                                    drRakutenRoom["OfferDescription"] = "";
                                    drRakutenRoom["OfferCode"] = "";
                                    drRakutenRoom["EssentialInformation"] = EssentialInfo;
                                    double ItemPrice = Convert.ToDouble(NetPrice);
                                    _markup.ProcessHotelRoom_WithMarkup(Factor, Currency, true, true, SearchParam.NoofNights, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), SearchParam.CheckInDate, SearchParam.CheckOutDate, ref ItemPrice, ref drRakutenRoom, isPerce, AgentPercentage, AgentMarkupAmount, SubAgentMarkup, B2CMarkup, IsParentId);
                                    drRakutenRoom["SupplierName"] = Provider;
                                    DtRakuten_HotelRoom.Rows.Add(drRakutenRoom);
                                    #endregion


                                    #endregion

                                    #region "Cancellation Policy"

                                    string PurchaseToken = NetPrice + "$$$" /*+ booking_policy_id*/;

                                    List<RakutenHotelListResponse.CancellationPolicy2> Paneltyrule = new List<RakutenHotelListResponse.CancellationPolicy2>(RakutenResponse.hotels[i].rates.packages[j].cancellation_policy.cancellation_policies);

                                    DateTime Todate = DateTime.Now.Date;

                                    string checkin2 = Convert.ToString(SearchParam.CheckInDate);
                                    DateTime dtCheckin1 = DateTime.ParseExact(checkin2, "dd/MM/yyyy", null);
                                    int loop = 1;
                                    //  Paneltyrule = Paneltyrule.OrderBy(item => item.date_from).ToList();
                                    Paneltyrule = Paneltyrule.Where(x => x.penalty_percentage > 0).ToList();

                                    DateTime MainDate = new DateTime();
                                    for (int jj = 0; jj < Paneltyrule.Count; jj++)
                                    {
                                        double Percentage = Convert.ToDouble(Paneltyrule[jj].penalty_percentage);

                                        if (Percentage > 0)
                                        {
                                            DateTime OnlyDate = Convert.ToDateTime(Paneltyrule[jj].date_from);
                                            if (jj == 0)
                                                MainDate = OnlyDate;
                                            else if (OnlyDate < MainDate)
                                                MainDate = OnlyDate;
                                        }
                                    }

                                    double Penalty = Convert.ToDouble(NetPrice);
                                    DateTime StartDate = MainDate;
                                    #region Cancellation Policy Logic and Row
                                    DateTime CancelDate = DateTime.Now.Date;
                                    CancelDate = StartDate;

                                    #region Vat
                                    double VatOnPolicy = 0;
                                    double MainAgentCharge = Penalty;
                                    double CompanyChargeAmount = Penalty;
                                    Penalty = _markup.ProcessHotelCancelationPrice_WithMarkup(ref MainAgentCharge, SearchParam.NoofNights, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), Penalty, isPerce, B2CMarkup, AgentPercentage, AgentMarkupAmount, IsParentId, SubAgentMarkup, ref VatOnPolicy);
                                    #endregion

                                    DateTime NowDate = DateTime.Now.Date;
                                    if (CancelDate.AddDays(CutOffDays).Date > DateTime.Now.Date)
                                    {
                                        StartDate = StartDate.AddDays(CutOffDays).Date;
                                        CancelDate = CancelDate.AddDays(CutOffDays).Date;
                                    }
                                    else
                                        CancelDate = DateTime.Now.Date;

                                    if (NowDate < CancelDate)
                                    {
                                        double zero = 0;
                                        dtRakuten_Cancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref zero, dtRakuten_Cancellation.NewRow(), 1, currencycode, Provider, Convert.ToString(OwnsystemHotelId), Convert.ToString(1), RoomTypeId, RoomName, MealId, DateTime.Now.Date, StartDate.AddDays(-1), 0, 0, "0", VatOnPolicy, "", "NO", PurchaseToken, EssentialInfo, "", ""));
                                        dtRakuten_Cancellation.AcceptChanges();
                                        dtRakuten_Cancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref CompanyChargeAmount, dtRakuten_Cancellation.NewRow(), 1, currencycode, Provider, Convert.ToString(OwnsystemHotelId), Convert.ToString(1), RoomTypeId, RoomName, MealId, Convert.ToDateTime(StartDate), DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null), MainAgentCharge, Penalty, "0", VatOnPolicy, "", "NO", PurchaseToken, EssentialInfo, "", ""));
                                        dtRakuten_Cancellation.AcceptChanges();
                                    }
                                    else
                                    {
                                        dtRakuten_Cancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref CompanyChargeAmount, dtRakuten_Cancellation.NewRow(), 1, currencycode, Provider, Convert.ToString(OwnsystemHotelId), Convert.ToString(1), RoomTypeId, RoomName, MealId, DateTime.Now.Date, DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null), MainAgentCharge, Penalty, "0", VatOnPolicy, "", "Yes", PurchaseToken, EssentialInfo, "", ""));
                                        dtRakuten_Cancellation.AcceptChanges();
                                    }
                                    #endregion
                                    #endregion
                                    #endregion
                                    Options ops = (await _supplierRes.GetCommonRoomForSuppliers(GiataId, commonSearch.tokenData, commonSearch.advancedOptions, FinalMinPrice, drXmlHotel, DtRakuten_HotelRoom, dtRakuten_Cancellation, DtRakuten_RateBreackUp, SearchParam));
                                    if (ops != null)
                                        options.Add(ops);
                                }
                                #endregion
                                #endregion
                            }
                        }
                        #endregion

                    }

                }


                return options;
            }
            catch (Exception ex)
            {
                await _exception.Error(ex);
                Item.ResponseType = "NoCount";
                return options;
            }
            finally
            {
                #region ResProcessTimer
                try
                {
                    if (SearchParam != null && ResProccessTimerLogActive == "1" && timerroomx.ElapsedMilliseconds > Convert.ToInt32(ResProccessTimeValueforSaveLog))
                    {
                        timerroomx.Stop();
                        SearchParam.SupplierTime += ",|Finish:Rakuten Process Response  " + "|Time :" + timerroomx.Elapsed.ToString();
                        _ = Task.Run(() => _commonMethod.FireAway_SQL_ResPorcessTimeLog("", timerroomx.Elapsed.ToString(), Item.Supplier, SearchParam.SearchKey, 1, SearchParam.AgentID));
                    }
                }
                catch (Exception x)
                {
                    await _exception.Error(x);
                }
                #endregion
            }
            //});
        }

        public async Task<List<Options>> Rakuten_HotelSearchWithRooms(HotelX Item, CommonSearch commonSearch, string Currency, string ResponseData, string CityCode, HotelSearchParameter SearchParam, double CurrencyFactorTBO, string XMLnationality, string URLandRequest, string UserName, string PassWord, string URL, Int32 SearchTimeOut, string custIP, Int32 processTimeout, string ResProccessTimerLogActive, string ResProccessTimeValueforSaveLog)
        {
            System.Diagnostics.Stopwatch timerroomx = new System.Diagnostics.Stopwatch();
            timerroomx.Start();
            List<Options> options = new List<Options>();
            try
            {
                #region DataTable
                DataTable DtHotel = _commonMethod.GenerateDataTableNewV2();
                #endregion
                if (ResponseData != string.Empty)
                {
                    double TotalCharges = 0;
                    string Provider = Item.Supplier;

                    if (SearchParam.Filters.IsShowRooms == 0)
                    {

                        #region "When No Need Rooms"
                        RakutenHotelListResponse.Root RakutenResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<RakutenHotelListResponse.Root>(ResponseData);
                        if (RakutenResponse == null)
                        {
                            Item.ResponseType = "NoCount";
                            return options;
                        }

                        if (RakutenResponse.hotels.Count < 1)
                        {
                            Item.ResponseType = "NoCount";
                        }

                        for (int u = 0; u < RakutenResponse.hotels.Count; u++)
                        {
                            string ItemCode = Convert.ToString(RakutenResponse.hotels[u].id);
                            string HotelName = Convert.ToString(RakutenResponse.hotels[u].id);

                            Item.ResponseType = "Success";

                            DataRow drXmlHotel = DtHotel.NewRow();
                            //string InternalHotelId = Convert.ToString(ItemCode);
                            drXmlHotel["XMLHotelCode"] = ItemCode;
                            drXmlHotel["HotelName"] = HotelName;
                            drXmlHotel["SupplierName"] = Provider;
                            drXmlHotel["OfficeID"] = 0;
                            drXmlHotel["ContractID"] = 0;
                            drXmlHotel["Rating"] = 0;
                            drXmlHotel["Reviews"] = 0;
                            drXmlHotel["UniqueNo"] = ItemCode;
                            drXmlHotel["IsShowSupplier"] = false;
                            drXmlHotel["BookingStatus"] = "Available";
                            drXmlHotel["XMLCity"] = CityCode;
                            drXmlHotel["SupplierCurrency"] = Currency;
                            drXmlHotel["AvailToken"] = "";

                            double Factor = 1;
                            string currencycode = Convert.ToString(RakutenResponse.hotels[u].rates.packages[0].chargeable_rate_currency);

                            if (currencycode != commonSearch.tokenData.SiteCurrency.ToLower())
                            {
                                Factor = _commonMethod.GetROEByCurrency(Currency, commonSearch.tokenData.dtROE);
                            }

                            double FinalTotal = Convert.ToDouble(RakutenResponse.hotels[u].rates.packages[0].chargeable_rate) * Factor;

                            drXmlHotel["FinalSellingPrice"] = FinalTotal;
                            drXmlHotel["SellingPrice"] = FinalTotal;
                            DtHotel.Rows.Add(drXmlHotel);
                            TotalCharges = FinalTotal;

                            if (TotalCharges > 0)
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
                                    decimal LowestPrice = Convert.ToDecimal(MinPrice).CalculatePrice(commonSearch.tokenData, commonSearch.advancedOptions, Currency);
                                    Options ops = (await _supplierRes.GetCommonRoomForSuppliers(GiataId, commonSearch.tokenData, commonSearch.advancedOptions, Convert.ToDouble(LowestPrice), drXmlHotel, dt_HotelRoom, dt_Cancellation, dt_RateBreackUp, SearchParam));
                                    if (ops != null)
                                        options.Add(ops);
                                }
                                #endregion
                            }

                        }
                        #endregion
                    }
                    else
                    {
                        #region "When Need Rooms"
                        RakutenHotelListResponse.Root RakutenResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<RakutenHotelListResponse.Root>(ResponseData);
                        if (RakutenResponse == null)
                            return options;

                        if (RakutenResponse.hotels.Count < 1)
                            Item.ResponseType = "NoCount";


                        double FinalMinPrice = 1;

                        for (int i = 0; i < RakutenResponse.hotels.Count; i++)
                        {
                            string ItemCode = Convert.ToString(RakutenResponse.hotels[i].id);
                            string HotelName = Convert.ToString(RakutenResponse.hotels[i].id);

                            #region "Hotel Block here"
                            DataRow drXmlHotel = DtHotel.NewRow();
                            //string InternalHotelId = Convert.ToString(ItemCode);
                            drXmlHotel["XMLHotelCode"] = ItemCode;
                            drXmlHotel["HotelName"] = HotelName;
                            drXmlHotel["SupplierName"] = Provider;
                            drXmlHotel["OfficeID"] = 0;
                            drXmlHotel["ContractID"] = 0;
                            drXmlHotel["Rating"] = 0;
                            drXmlHotel["Reviews"] = 0;
                            drXmlHotel["UniqueNo"] = ItemCode;
                            drXmlHotel["IsShowSupplier"] = false;
                            drXmlHotel["BookingStatus"] = "Available";
                            drXmlHotel["XMLCity"] = CityCode;
                            drXmlHotel["SupplierCurrency"] = Currency;
                            drXmlHotel["AvailToken"] = "";

                            double Factor = 1;
                            string currencycode = Convert.ToString(RakutenResponse.hotels[i].rates.packages[0].chargeable_rate_currency);

                            if (currencycode != commonSearch.tokenData.SiteCurrency.ToLower())
                            {
                                Factor = _commonMethod.GetROEByCurrency(Currency, commonSearch.tokenData.dtROE);
                            }
                            double FinalTotal = Convert.ToDouble(RakutenResponse.hotels[i].rates.packages[0].chargeable_rate) * Factor;

                            drXmlHotel["FinalSellingPrice"] = FinalTotal;
                            drXmlHotel["SellingPrice"] = FinalTotal;
                            DtHotel.Rows.Add(drXmlHotel);
                            TotalCharges = FinalTotal;
                            #endregion


                            bool willAddOrNot = true;
                            Int64 RaynaID = 0;
                            Int64 GiataId = 0;
                            int RatingHotel = 0;
                            Int64 OwnsystemHotelId = 0;
                            double MinPrice = 1;

                            _markup.GetHotelDataCommon(ref MinPrice, ref willAddOrNot, ref OwnsystemHotelId, ref RaynaID, ref GiataId, ref RatingHotel, ref drXmlHotel, commonSearch.AgentId, commonSearch.TariffID, commonSearch.dsAgentDetails, commonSearch.supplierGiataMaster, SearchParam, commonSearch.dtCountryCityMarkup, commonSearch.B2CMarkup, commonSearch.IsParentId, commonSearch.SubAgentMarkup, commonSearch.SearchByHotel, commonSearch.HotelIDS);
                            if (willAddOrNot)
                            {
                                DataTable DtRakuten_HotelRoom = _commonMethod.GenerateRoomtypeDataTableNewV2();
                                DataTable dtRakuten_Cancellation = _commonMethod.GenerateCancellationDataTableNewV2();
                                DataTable DtRakuten_RateBreackUp = _commonMethod.GenerateoomRateBreackUpDataTableNewV2();
                                int CutOffDays = CacheMaster.CutOffDays;
                                CutOffDays = _commonMethod.GetCutOffDays(commonSearch.dtSuplierMarkup, Provider);


                                string Type = null;
                                double SubAgentMarkup = 0;

                                int IsParentId = 1, agentid = 0;
                                double AgentPercentage = 0.0;
                                double AgentMarkupAmount = 0.0;
                                Boolean isPerce = true;

                                double AgentCompanyPercentage = Convert.ToDouble(SearchParam.AgentCompanyPercentage);
                                bool IsB2C = Convert.ToBoolean(Convert.ToByte(SearchParam.IsB2BOrB2C));
                                bool blnIsShowSupplier = false;
                                _markup.getAgetMarkupSetting(Provider, GiataId.ToString(), commonSearch.dtSuplierMarkup, commonSearch.dtMarkupDetails, ref AgentPercentage, ref AgentMarkupAmount, ref blnIsShowSupplier, ref isPerce, commonSearch.AgentId, RatingHotel, SearchParam.CityID, SearchParam.CountryID.ToString(), SearchParam.CheckInDate);
                                double B2CMarkup = (IsB2C == false ? AgentCompanyPercentage : commonSearch.B2CMarkup);


                                DataRow[] drHotelDetails = DtHotel.Select("OwnsystemHotelId='" + OwnsystemHotelId + "'");
                                string[] HotelDetails = Convert.ToString(drHotelDetails[0]["AvailToken"]).Split(':');

                                string Error = "";
                                int TotalAdult = 0;
                                int TotalChild = 0;
                                for (int ad = 0; ad < SearchParam.Rooms.Count; ad++)
                                {
                                    TotalAdult += SearchParam.Rooms[ad].NoofAdults;
                                    TotalChild += SearchParam.Rooms[ad].NoofChild;
                                }

                                #region Room Loop

                                for (int j = 0; j < RakutenResponse.hotels[i].rates.packages.Count; j++)
                                {
                                    Item.ResponseType = "Success";


                                    string RoomCode = Convert.ToString(RakutenResponse.hotels[i].rates.packages[j].room_details.rate_plan_code);
                                    // string RoomName = Convert.ToString(RakutenResponse.hotels[i].rates.packages[j].room_details.room_type);
                                    string RoomName = Convert.ToString(RakutenResponse.hotels[i].rates.packages[j].room_details.supplier_description);
                                    string bookingKey = Convert.ToString(RakutenResponse.hotels[i].rates.packages[j].booking_key);
                                    int meal = Convert.ToInt32(RakutenResponse.hotels[i].rates.packages[j].room_details.food);
                                    string MealId = Convert.ToString(GetMealName(meal));
                                    string MealName = Convert.ToString(GetMealName(meal));
                                    double NetPrice = Convert.ToDouble(RakutenResponse.hotels[i].rates.packages[j].chargeable_rate);


                                    if ((Convert.ToString(RakutenResponse.hotels[i].rates.packages[j].chargeable_rate_currency)).ToLower() != commonSearch.tokenData.SiteCurrency.ToLower())
                                    {
                                        //DataTable dtInrCurrency = Common.GetCountryCurrency(Convert.ToString(RakutenResponse.hotels[i].rates.packages[j].chargeable_rate_currency).ToLower());
                                        Factor = _commonMethod.GetROEByCurrency(Convert.ToString(RakutenResponse.hotels[i].rates.packages[j].chargeable_rate_currency), commonSearch.tokenData.dtROE); // Convert.ToDouble(dtInrCurrency.Rows[0]["AmtToExch"]);
                                    }
                                    NetPrice = NetPrice * Factor;

                                    string RoomCodeMain = RoomCode + "$$$" + ItemCode + "$$$" + RoomName + "$$$" + meal + "$$$" + bookingKey + "$$$" + XMLnationality + "$$$" + NetPrice;
                                    string RateKeyMain = "";



                                    string EssentialInfo = Convert.ToString(RakutenResponse.hotels[i].rates.packages[j].room_details.supplier_description);

                                    string RoomTypeId = RoomCodeMain;

                                    RoomTypeId = RoomTypeId.Replace("'", "");

                                    #region Room
                                    DataRow drRakutenRoom = DtRakuten_HotelRoom.NewRow();
                                    drRakutenRoom["OwnSystemHotelId"] = OwnsystemHotelId;
                                    drRakutenRoom["RoomTypeName"] = RoomName;
                                    drRakutenRoom["RoomTypeCode"] = RoomCode;
                                    drRakutenRoom["MealId"] = MealId;
                                    drRakutenRoom["MealName"] = MealName;
                                    drRakutenRoom["ContractID"] = 0;
                                    drRakutenRoom["BoardType"] = MealName;
                                    drRakutenRoom["Board"] = MealId;
                                    drRakutenRoom["RoomTypeId"] = RoomTypeId;
                                    drRakutenRoom["AdultsOccupancy"] = TotalAdult;
                                    drRakutenRoom["ChildOccupancy"] = TotalChild;
                                    drRakutenRoom["MaxExtraBed"] = "0";
                                    drRakutenRoom["RoomNo"] = "1";// j + 1;//RoomNo;
                                    drRakutenRoom["RoomSequenceNo"] = RoomName;
                                    drRakutenRoom["Type"] = RoomCodeMain;                          //Rate Key                                        
                                    drRakutenRoom["OfficeId"] = 0;
                                    drRakutenRoom["BookingStatus"] = "Available";
                                    drRakutenRoom["UniqueNo"] = Guid.NewGuid().ToString();
                                    drRakutenRoom["OfferDescription"] = "";
                                    drRakutenRoom["OfferCode"] = "";
                                    drRakutenRoom["EssentialInformation"] = EssentialInfo;
                                    double ItemPrice = Convert.ToDouble(NetPrice);
                                    _markup.ProcessHotelRoom_WithMarkup(Factor, Currency, true, true, SearchParam.NoofNights, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), SearchParam.CheckInDate, SearchParam.CheckOutDate, ref ItemPrice, ref drRakutenRoom, isPerce, AgentPercentage, AgentMarkupAmount, SubAgentMarkup, B2CMarkup, IsParentId);
                                    drRakutenRoom["SupplierName"] = Provider;
                                    DtRakuten_HotelRoom.Rows.Add(drRakutenRoom);
                                    #endregion




                                    #region "Cancellation Policy"

                                    //string PurchaseToken = NetPrice + "$$$" /*+ booking_policy_id*/;
                                    string PurchaseToken = "";// NetPrice + "$$$" /*+ booking_policy_id*/;//Comment on 19-Nov-2025 as per karnav

                                    List<RakutenHotelListResponse.CancellationPolicy2> Paneltyrule = new List<RakutenHotelListResponse.CancellationPolicy2>(RakutenResponse.hotels[i].rates.packages[j].cancellation_policy.cancellation_policies);

                                    DateTime Todate = DateTime.Now.Date;

                                    string checkin2 = Convert.ToString(SearchParam.CheckInDate);
                                    DateTime dtCheckin1 = DateTime.ParseExact(checkin2, "dd/MM/yyyy", null);
                                    int loop = 1;
                                    //  Paneltyrule = Paneltyrule.OrderBy(item => item.date_from).ToList();
                                    Paneltyrule = Paneltyrule.Where(x => x.penalty_percentage > 0).ToList();

                                    DateTime MainDate = new DateTime();
                                    for (int jj = 0; jj < Paneltyrule.Count; jj++)
                                    {
                                        double Percentage = Convert.ToDouble(Paneltyrule[jj].penalty_percentage);

                                        if (Percentage > 0)
                                        {
                                            DateTime OnlyDate = Convert.ToDateTime(Paneltyrule[jj].date_from);
                                            if (jj == 0)
                                                MainDate = OnlyDate;
                                            else if (OnlyDate < MainDate)
                                                MainDate = OnlyDate;
                                        }
                                    }

                                    double Penalty = Convert.ToDouble(NetPrice);
                                    DateTime StartDate = MainDate;
                                    #region Cancellation Policy Logic and Row
                                    DateTime CancelDate = DateTime.Now.Date;
                                    CancelDate = StartDate;

                                    #region Vat
                                    double VatOnPolicy = 0;
                                    double MainAgentCharge = Penalty;
                                    double CompanyChargeAmount = Penalty;
                                    Penalty = _markup.ProcessHotelCancelationPrice_WithMarkup(ref MainAgentCharge, SearchParam.NoofNights, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), Penalty, isPerce, B2CMarkup, AgentPercentage, AgentMarkupAmount, IsParentId, SubAgentMarkup, ref VatOnPolicy);
                                    #endregion

                                    DateTime NowDate = DateTime.Now.Date;
                                    if (CancelDate.AddDays(CutOffDays).Date > DateTime.Now.Date)
                                    {
                                        StartDate = StartDate.AddDays(CutOffDays).Date;
                                        CancelDate = CancelDate.AddDays(CutOffDays).Date;
                                    }
                                    else
                                        CancelDate = DateTime.Now.Date;

                                    if (NowDate < CancelDate)
                                    {
                                        double zero = 0;
                                        dtRakuten_Cancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref zero, dtRakuten_Cancellation.NewRow(), 1, currencycode, Provider, Convert.ToString(OwnsystemHotelId), Convert.ToString(1), RoomTypeId, RoomName, MealId, DateTime.Now.Date, StartDate.AddDays(-1), 0, 0, "0", VatOnPolicy, "", "NO", PurchaseToken, EssentialInfo, "", ""));
                                        dtRakuten_Cancellation.AcceptChanges();
                                        dtRakuten_Cancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref CompanyChargeAmount, dtRakuten_Cancellation.NewRow(), 1, currencycode, Provider, Convert.ToString(OwnsystemHotelId), Convert.ToString(1), RoomTypeId, RoomName, MealId, Convert.ToDateTime(StartDate), DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null), MainAgentCharge, Penalty, "0", VatOnPolicy, "", "NO", PurchaseToken, EssentialInfo, "", ""));
                                        dtRakuten_Cancellation.AcceptChanges();
                                    }
                                    else
                                    {
                                        dtRakuten_Cancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref CompanyChargeAmount, dtRakuten_Cancellation.NewRow(), 1, currencycode, Provider, Convert.ToString(OwnsystemHotelId), Convert.ToString(1), RoomTypeId, RoomName, MealId, DateTime.Now.Date, DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null), MainAgentCharge, Penalty, "0", VatOnPolicy, "", "Yes", PurchaseToken, EssentialInfo, "", ""));
                                        dtRakuten_Cancellation.AcceptChanges();
                                    }
                                    #endregion
                                    #endregion



                                    #endregion
                                }

                                Options ops = (await _supplierRes.GetCommonRoomForSuppliers(GiataId, commonSearch.tokenData, commonSearch.advancedOptions, FinalMinPrice, drXmlHotel, DtRakuten_HotelRoom, dtRakuten_Cancellation, DtRakuten_RateBreackUp, SearchParam));
                                if (ops != null)
                                    options.Add(ops);
                                #endregion
                            }

                        }


                    }

                }


                return options;
            }
            catch (Exception ex)
            {
                await _exception.Error(ex);
                Item.ResponseType = "NoCount";
                return options;
            }
            finally
            {
                #region ResProcessTimer
                try
                {
                    if (SearchParam != null && ResProccessTimerLogActive == "1" && timerroomx.ElapsedMilliseconds > Convert.ToInt32(ResProccessTimeValueforSaveLog))
                    {
                        timerroomx.Stop();
                        SearchParam.SupplierTime += ",|Finish:Rakuten Process Response  " + "|Time :" + timerroomx.Elapsed.ToString();
                        _ = Task.Run(() => _commonMethod.FireAway_SQL_ResPorcessTimeLog("", timerroomx.Elapsed.ToString(), Item.Supplier, SearchParam.SearchKey, 1, SearchParam.AgentID));
                    }
                }
                catch (Exception x)
                {
                    await _exception.Error(x);
                }
                #endregion
            }
            //});
        }


        public static string GetMealName(int mealId)
        {
            string mealname = "";
            switch (mealId)
            {
                case 1:
                    mealname = "RO";
                    break;

                case 2:
                    mealname = "BF";
                    break;

                case 3:
                    mealname = "Lunch";
                    break;

                case 4:
                    mealname = "Dinner";
                    break;
                case 5:
                    mealname = "HB";
                    break;
                case 6:
                    mealname = "FB";
                    break;
                case 7:
                    mealname = "All inclusive";
                    break;
                default:
                    mealname = "RO";
                    break;
            }
            return mealname;
        }

        public async Task<CancellationDto> Rakuten_Cancellation(double CompanyBuy, string MealCode, DataSet dsHotel, string hotelcode, string ownsystemid, string roomtypeid, DataTable dtAgentDetail, DataTable dtSuplierMarkup, DataTable dtSearchConfig, HotelSearchParameter SearchPara, string roomno, string provider, int AgentId, DataTable dtMarkupDetails, string GiataId = "0", int hotelRating = 0)
        {
            string Error = "";
            string IsBooking = "Available";
            CancellationDto cancellationDto = new CancellationDto();
            try
            {
                DataTable dtRakutenCancellation = _commonMethod.GenerateCancellationDataTableNewV2();
                int CutOffDays = XMLHotelBookingCommon.CutOffDays;
                CutOffDays = _commonMethod.GetCutOffDays(dtSuplierMarkup, provider);
                Boolean isPerce = true;
                double AgentPercentage = 0.0;
                Boolean blnIsShowSupplier = false;
                double AgentMarkupAmount = 0.0;
                _markup.getAgetMarkupSetting(provider, GiataId, dtSuplierMarkup, dtMarkupDetails, ref AgentPercentage, ref AgentMarkupAmount, ref blnIsShowSupplier, ref isPerce, Convert.ToInt32(dtAgentDetail.Rows[0]["AgentID"]), hotelRating, SearchPara.CityID, Convert.ToString(SearchPara.CountryID), Convert.ToString(SearchPara.CheckInDate));
                double AgentCompanyPercentage = Convert.ToDouble(SearchPara.AgentCompanyPercentage);
                bool IsB2C = Convert.ToBoolean(Convert.ToByte(SearchPara.IsB2BOrB2C));

                double B2CMarkup = (IsB2C == false ? AgentCompanyPercentage : XMLHotelBookingCommon.B2CMarkup);
                if (IsB2C == true)
                    AgentPercentage = 0;

                DataRow[] drSuplierMarkup = (dtSuplierMarkup.Select("SupplierName='" + provider + "'"));
                int SupplierId = Convert.ToInt32(drSuplierMarkup[0]["SupplierID"]);

                string Rakuten_URL = Convert.ToString(drSuplierMarkup[0]["WebSiteURL"]); //added on 300525
                string RakutenCurrency = Convert.ToString(drSuplierMarkup[0]["CurrencyShortName"]); //added on 300525

                DataRow[] drSearchConfig = (dtSearchConfig.Select("SupplierID='" + SupplierId + "'"));
                int SearchTimeOut = Convert.ToInt32(drSearchConfig[0]["BookingConfirmationTimeout"]);

                string CheckIn = Convert.ToString(SearchPara.CheckInDate);
                string CheckOut = Convert.ToString(SearchPara.CheckOutDate);
                CheckIn = CheckIn.Substring(6, 4) + "-" + CheckIn.Substring(3, 2) + "-" + CheckIn.Substring(0, 2);
                CheckOut = CheckOut.Substring(6, 4) + "-" + CheckOut.Substring(3, 2) + "-" + CheckOut.Substring(0, 2);
                string[] HBArray = Convert.ToString(roomtypeid).Split(new String[] { "$$$" }, StringSplitOptions.None);
                string RoomCode = HBArray[0];
                string Hotel_Id = HBArray[1];
                string RoomNam = HBArray[2];
                int mealid = Convert.ToInt32(HBArray[3]);
                string bookingKey = HBArray[4];
                string XMLNationality = HBArray[5];


                string RoomRequest = RakutenSearchRequest(Convert.ToString(Rakuten_URL), SearchPara, CheckIn, CheckOut, Hotel_Id, XMLNationality, Convert.ToString(RakutenCurrency), 2);
                //string RoomResponseData = Response_RakutenSupplier(ref Error, RoomRequest, Convert.ToString(drSuplierMarkup[0]["PassWord"]), "GET", "room");
                ResponseDto responseDto = await Response_RakutenSupplier(RoomRequest, Convert.ToString(drSuplierMarkup[0]["PassWord"]), "GET", "room");
                string RoomResponseData = responseDto.Response;
                Error = responseDto.Error;

                if (RoomResponseData == "")
                {
                    cancellationDto.dsHotelData = dsHotel;
                    return cancellationDto;
                }

                RakutenHotelRoomResponse.Root rakutenResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<RakutenHotelRoomResponse.Root>(RoomResponseData);
                if (rakutenResponse == null)
                {
                    cancellationDto.dsHotelData = dsHotel;
                    return cancellationDto;
                }

                //if (rakutenResponse.status == "in-progress")
                //{
                //    RoomResponseData = Response_RakutenSupplier(ref Error, RoomRequest, Convert.ToString(drSuplierMarkup[0]["PassWord"]), "GET", "room");

                //    if (RoomResponseData == "")
                //        return dsHotel;

                //    rakutenResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<RakutenHotelRoomResponse.Root>(RoomResponseData);

                //    if (rakutenResponse == null)
                //        return dsHotel;
                //}

                _commonMethod.ThirdPartyGetPolicyHistory(Convert.ToString(RoomCode), Convert.ToString(ownsystemid), Convert.ToString(RoomRequest), Convert.ToString(RoomResponseData + Error), "Room Request Response before Policy", provider, "Room Response", "-", "WithinEarth");

                RakutenHotelRoomResponse.Search SearchPar = rakutenResponse.search;
                RakutenHotelRoomResponse.Package package = rakutenResponse.hotels[0].rates.packages.Where(item => item.booking_key == bookingKey && item.room_details.food == mealid).FirstOrDefault();

                string Search = Newtonsoft.Json.JsonConvert.SerializeObject(SearchPar);
                string packag = Newtonsoft.Json.JsonConvert.SerializeObject(package);

                StringBuilder rakutenPriceCheck = new StringBuilder();
                rakutenPriceCheck.Append("{");
                rakutenPriceCheck.Append("\"search\"" + ":" + Search);
                rakutenPriceCheck.Append(",");
                rakutenPriceCheck.Append("\"package\"" + ":" + packag);
                rakutenPriceCheck.Append("}");
                string PriceCheckRequest = rakutenPriceCheck.ToString();

                //string ResponseData = Get_Rakuten_Response(ref Error, Convert.ToString(drSuplierMarkup[0]["WebSiteURL"]) + Convert.ToString(RakutenInfo.Rakuten_PriceCheck), Convert.ToString(drSuplierMarkup[0]["PassWord"]), PriceCheckRequest, "Rakuten");
                ResponseDto responseDto1 = await Get_Rakuten_Response(Convert.ToString(drSuplierMarkup[0]["WebSiteURL"]) + Convert.ToString(RakutenInfo.Rakuten_PriceCheck), Convert.ToString(drSuplierMarkup[0]["PassWord"]), PriceCheckRequest, provider);
                string ResponseData = responseDto1.Response;
                Error = responseDto1.Error;
                _commonMethod.ThirdPartyGetPolicyHistory(Convert.ToString(RoomCode), Convert.ToString(ownsystemid), PriceCheckRequest, Convert.ToString(ResponseData + Error), "After Get Policy", provider, "Policy Response", "-", "WithinEarth");




                if (ResponseData == "")
                {
                    cancellationDto.dsHotelData = dsHotel;
                    return cancellationDto;

                }

                RakutenPriceCheckResponse.Root RakutenResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<RakutenPriceCheckResponse.Root>(ResponseData);

                if (RakutenResponse == null)
                {
                    cancellationDto.dsHotelData = dsHotel;
                    return cancellationDto;
                }

                string booking_policy_id = Convert.ToString(RakutenResponse.booking_policy_id);
                string Prices = Convert.ToString(RakutenResponse.package.chargeable_rate);
                string PurchaseToken = Prices + "$$$" + booking_policy_id;
                string NonRefundable = Convert.ToString(RakutenResponse.package.room_details.non_refundable);
                double ItemPrice = Convert.ToDouble(CompanyBuy);
                string EssentialInfo = "";
                string RoomName = "";
                if (RakutenResponse.package.room_details.supplier_description != null)
                    EssentialInfo = Convert.ToString(RakutenResponse.package.room_details.supplier_description);

                double Penalty = Convert.ToDouble(ItemPrice);
                #region Vat
                double VatOnPolicy = 0;
                double MainAgentCharge = Penalty;
                double CompanyChargeAmount = Penalty;
                Penalty = _markup.ProcessHotelCancelationPrice_WithMarkup(ref MainAgentCharge, SearchPara.NoofNights, SearchPara.CityID, Convert.ToString(SearchPara.CountryID), Penalty, isPerce, B2CMarkup, AgentPercentage, AgentMarkupAmount, Convert.ToInt32(dtAgentDetail.Rows[0]["IsParentId"]), Convert.ToDouble(dtAgentDetail.Rows[0]["B2CMarkupPer"]), ref VatOnPolicy);
                #endregion

                if (RakutenResponse.cancellation_policy != null)
                {
                    if (RakutenResponse.cancellation_policy.remarks != null)
                        EssentialInfo += "</br>" + Convert.ToString(RakutenResponse.cancellation_policy.remarks);

                    #region "Cancellation Policy"

                    List<RakutenPriceCheckResponse.CancellationPolicy2> Paneltyrule = new List<RakutenPriceCheckResponse.CancellationPolicy2>(RakutenResponse.cancellation_policy.cancellation_policies);

                    DateTime Todate = DateTime.Now.Date;

                    string checkin2 = Convert.ToString(SearchPara.CheckInDate);
                    DateTime dtCheckin1 = DateTime.ParseExact(checkin2, "dd/MM/yyyy", null);
                    int loop = 1;
                    //  Paneltyrule = Paneltyrule.OrderBy(item => item.date_from).ToList();
                    Paneltyrule = Paneltyrule.Where(x => x.penalty_percentage > 0).ToList();

                    DateTime MainDate = new DateTime();
                    for (int j = 0; j < Paneltyrule.Count; j++)
                    {
                        double Percentage = Convert.ToDouble(Paneltyrule[j].penalty_percentage);

                        if (Percentage > 0)
                        {
                            DateTime OnlyDate = Convert.ToDateTime(Paneltyrule[j].date_from);
                            if (j == 0)
                                MainDate = OnlyDate;
                            else if (OnlyDate < MainDate)
                                MainDate = OnlyDate;
                        }
                    }

                    DateTime StartDate = MainDate;
                    #region Cancellation Policy Logic and Row
                    DateTime CancelDate = DateTime.Now.Date;
                    CancelDate = StartDate;
                    DateTime NowDate = DateTime.Now.Date;
                    if (CancelDate.AddDays(CutOffDays).Date > DateTime.Now.Date)
                    {
                        StartDate = StartDate.AddDays(CutOffDays).Date;
                        CancelDate = CancelDate.AddDays(CutOffDays).Date;
                    }
                    else
                        CancelDate = DateTime.Now.Date;

                    if (NowDate < CancelDate)
                    {
                        double zero = 0;
                        dtRakutenCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref zero, dtRakutenCancellation.NewRow(), 1, Convert.ToString(RakutenCurrency), provider, ownsystemid, Convert.ToString(1), roomtypeid, RoomName, MealCode, DateTime.Now.Date, StartDate.AddDays(-1), 0, 0, "0", VatOnPolicy, "", "NO", PurchaseToken, EssentialInfo, "", ""));
                        dtRakutenCancellation.AcceptChanges();
                        dtRakutenCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref CompanyChargeAmount, dtRakutenCancellation.NewRow(), 1, Convert.ToString(RakutenCurrency), provider, ownsystemid, Convert.ToString(1), roomtypeid, RoomName, MealCode, Convert.ToDateTime(StartDate), DateTime.ParseExact(SearchPara.CheckInDate, "dd/MM/yyyy", null), MainAgentCharge, Penalty, "0", VatOnPolicy, "", "NO", PurchaseToken, EssentialInfo, "", ""));
                        dtRakutenCancellation.AcceptChanges();
                    }
                    else
                    {
                        dtRakutenCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref CompanyChargeAmount, dtRakutenCancellation.NewRow(), 1, Convert.ToString(RakutenCurrency), provider, ownsystemid, Convert.ToString(1), roomtypeid, RoomName, MealCode, DateTime.Now.Date, DateTime.ParseExact(SearchPara.CheckInDate, "dd/MM/yyyy", null), MainAgentCharge, Penalty, "0", VatOnPolicy, "", "Yes", PurchaseToken, EssentialInfo, "", ""));
                        dtRakutenCancellation.AcceptChanges();
                    }
                    #endregion
                    #endregion
                }
                else
                {
                    dtRakutenCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref CompanyChargeAmount, dtRakutenCancellation.NewRow(), 1, Convert.ToString(RakutenCurrency), provider, ownsystemid, Convert.ToString(1), roomtypeid, RoomName, MealCode, DateTime.Now.Date, DateTime.ParseExact(SearchPara.CheckInDate, "dd/MM/yyyy", null), MainAgentCharge, Penalty, "0", VatOnPolicy, "", "Yes", PurchaseToken, EssentialInfo, "", ""));
                    dtRakutenCancellation.AcceptChanges();
                }
                dsHotel.Tables[2].Merge(dtRakutenCancellation);
            }
            catch (Exception ex)
            {
                _ = _exception.Error(ex);

                cancellationDto.dsHotelData = null;
                cancellationDto.ISBooking = "NotAvailable";
                return cancellationDto;
            }

            cancellationDto.dsHotelData = dsHotel;
            cancellationDto.ISBooking = "Available";
            return cancellationDto;
        }

        public async Task<ResponseDto> Response_RakutenSupplier(string request, string Password, string type, string name)
        {
            ResponseDto responseDto = new ResponseDto();
            string Error = "";
            string HotelBedsResponse = string.Empty;
            string response = string.Empty;
            HttpWebRequest Request = (HttpWebRequest)WebRequest.Create(request);
            Request.Headers.Add("x-api-key", Password);
            Request.ContentType = "application/json";
            Request.Accept = "application/json";
            Request.Method = type;

            if (name == "book" || name == "prisecheck")
            {
                Request.ContentLength = 0;
                Request.Method = type;
            }
            Request.Headers.Add("Accept-Encoding", "gzip");

            try
            {
                ServicePointManager.Expect100Continue = true;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls
                | SecurityProtocolType.Tls11
                | SecurityProtocolType.Tls12;

                HttpWebResponse Response = (HttpWebResponse)await Request.GetResponseAsync();
                Stream responseStream = Response.GetResponseStream();
                if (Response.ContentEncoding.ToLower().Contains("gzip"))
                    responseStream = new System.IO.Compression.GZipStream(responseStream, System.IO.Compression.CompressionMode.Decompress);
                else if (Response.ContentEncoding.ToLower().Contains("deflate"))
                    responseStream = new System.IO.Compression.DeflateStream(responseStream, System.IO.Compression.CompressionMode.Decompress);
                StreamReader Reader = new StreamReader(responseStream, Encoding.UTF8);
                HotelBedsResponse = await Reader.ReadToEndAsync();
                responseStream.Close();
                Reader.Close();
                Response.Close();
            }
            catch (WebException we)
            {
                HttpWebResponse httpResponse = (HttpWebResponse)we.Response;
                string result = string.Empty;
                string result1 = string.Empty;
                if (httpResponse != null && httpResponse.StatusDescription != null)
                {
                    result = httpResponse.StatusDescription.ToString();
                    result1 = new StreamReader(we.Response.GetResponseStream()).ReadToEnd();
                }
                Error = result1;
                _ = _commonMethod.XMLRequestResponseTrackError(request, result1, "Rakuten", "Proper");
            }

            responseDto.Response = HotelBedsResponse;
            responseDto.Error = Error;
            return responseDto;
        }

        public async Task<ResponseDto> Get_Rakuten_Response(string RakutenURL, string pass, string request, string Provider)
        {
            System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
            timer.Start();

            string strOutput = "";
            string Error = "";

            ResponseDto responseDto = new ResponseDto();
            try
            {
                HttpWebRequest Request;
                Request = (HttpWebRequest)HttpWebRequest.Create(RakutenURL);
                //Request.Timeout = 12000;
                Request.Method = "POST";
                Request.ContentType = "application/json";
                Request.Headers.Add("x-api-key", pass);
                Request.KeepAlive = false;
                Request.Headers.Add("Accept-Encoding", "gzip");
                Request.Accept = "application/json";
                Stream newStream = null;
                byte[] data = Encoding.ASCII.GetBytes(request);
                Request.ContentLength = data.Length;
                newStream = Request.GetRequestStream();
                newStream.Write(data, 0, data.Length);
                try
                {
                    HttpWebResponse Response = (HttpWebResponse)await Request.GetResponseAsync();
                    Stream responseStream = Response.GetResponseStream();
                    if (Response.ContentEncoding.ToLower().Contains("gzip"))
                        responseStream = new System.IO.Compression.GZipStream(responseStream, System.IO.Compression.CompressionMode.Decompress);
                    else if (Response.ContentEncoding.ToLower().Contains("deflate"))
                        responseStream = new System.IO.Compression.DeflateStream(responseStream, System.IO.Compression.CompressionMode.Decompress);
                    StreamReader Reader = new StreamReader(responseStream);
                    strOutput = await Reader.ReadToEndAsync();
                    responseStream.Close();
                    Reader.Close();
                    Response.Close();
                }
                catch (WebException we)
                {
                    //HttpWebResponse httpResponse = (HttpWebResponse)we.Response;
                    //string result = string.Empty;
                    //string result1 = string.Empty;
                    //if (httpResponse != null && httpResponse.StatusDescription != null)
                    //{
                    //    result = httpResponse.StatusDescription.ToString();
                    //    result1 = new StreamReader(we.Response.GetResponseStream()).ReadToEnd();
                    //}
                    //Error = result1;
                    //_ = _commonMethod.XMLRequestResponseTrackError(request, result1, Provider == "" ? "Rakuten" : Provider, "Proper");

                    //responseDto.Response = "";
                    //responseDto.Error = Error;
                    //return responseDto;
                    HttpWebResponse httpResponse = (HttpWebResponse)we.Response;
                    string result = string.Empty;
                    string result1 = string.Empty;

                    if (httpResponse != null)
                    {
                        Stream errorStream = httpResponse.GetResponseStream();

                        // Decompression handling
                        if (httpResponse.ContentEncoding != null)
                        {
                            string encoding = httpResponse.ContentEncoding.ToLower();
                            if (encoding.Contains("gzip"))
                            {
                                errorStream = new System.IO.Compression.GZipStream(
                                    errorStream, System.IO.Compression.CompressionMode.Decompress);
                            }
                            else if (encoding.Contains("deflate"))
                            {
                                errorStream = new System.IO.Compression.DeflateStream(
                                    errorStream, System.IO.Compression.CompressionMode.Decompress);
                            }
                        }

                        using (var reader = new StreamReader(errorStream))
                        {
                            result1 = reader.ReadToEnd();
                        }

                        result = httpResponse.StatusDescription;
                    }

                    responseDto.Response = "";
                    responseDto.Error = result + " | " + result1;

                    return responseDto;

                }
            }
            catch (Exception ex)
            {
                Error = ex.Message;
                _ = _commonMethod.XMLRequestResponseTrack(request, ex.Message, Provider == "" ? "Rakuten" : Provider, "Proper");

                responseDto.Response = "";
                responseDto.Error = Error;
                return responseDto;
            }

            responseDto.Response = strOutput;
            responseDto.Error = Error;
            return responseDto;
        }

        public async Task<PriceCheckCMDto> PriceUpdateRakuten(DataTable dtCheckPriceReqStructure, DataSet dsCartDetails, DataRow[] RakutenDetails, string UniqueNo, string AgentUniqueNo, int i, int j, string IsFor, double CompanyBuyPrice, int AgentId, bool IsError, double itemprice, TokenData tokenData)
        {
            PriceCheckCMDto priceCheckCMDto = new PriceCheckCMDto();
            string Error = "";
            string RequestSPR = "-", ReponseSPR = "-";
            string time = "00:00";
            string responsetype = "Success";
            try
            {
                string RakutenCurrency = Convert.ToString(RakutenDetails[0]["CurrencyShortName"]);
                double CurrencyFactorRakuten = 1;
                if (RakutenCurrency.ToLower() != tokenData.SiteCurrency.ToLower())
                {
                    CurrencyFactorRakuten = _commonMethod.GetROEByCurrency(RakutenCurrency, tokenData.dtROE);
                }

                int UserCartId = Convert.ToInt32(dsCartDetails.Tables[0].Rows[i]["UserCartID"]);
                int UserCartDetailId = Convert.ToInt32(dsCartDetails.Tables[1].Rows[j]["UserCartDetailId"]);

                DataSet dsTemp = new DataSet();
                string RakutenUrl = Convert.ToString(RakutenDetails[0]["WebSiteURL"]);
                string UserId = Convert.ToString(RakutenDetails[0]["UserID"]);
                string UserName = Convert.ToString(RakutenDetails[0]["UserName"]);
                string Password = Convert.ToString(RakutenDetails[0]["Password"]);
                string RoomId = "";


                string[] HBArray = Convert.ToString(dsCartDetails.Tables[1].DefaultView[i]["RoomTypeId"]).Split(new String[] { "$$$" }, StringSplitOptions.None);
                string RoomCode = HBArray[0];
                string Hotel_Id = HBArray[1];
                string RoomName = HBArray[2];
                int mealid = Convert.ToInt32(HBArray[3]);
                string bookingKey = HBArray[4];
                string XMLNationality = HBArray[5];


                string BookingPolicyId = Convert.ToString(dsCartDetails.Tables[1].DefaultView[0]["purchaseToken"]);

                if (BookingPolicyId != "" && BookingPolicyId != null)
                {

                    string[] BookingPolicy = Convert.ToString(dsCartDetails.Tables[1].DefaultView[i]["purchaseToken"]).Split(new String[] { "$$$" }, StringSplitOptions.None);
                    string Price = BookingPolicy[0];
                    string booking_policy_id = BookingPolicy[1];
                    double FinalPriceMain = 0;
                    double FinalPriceTemp = Convert.ToDouble(Price);
                    string purchaseToken = Convert.ToString(booking_policy_id);
                    FinalPriceMain = FinalPriceTemp * CurrencyFactorRakuten;

                    itemprice = Convert.ToDouble(FinalPriceMain);

                    itemprice = Convert.ToDouble(Math.Round(itemprice, 2));
                    priceCheckCMDto.ItemPrice = itemprice;
                    if (Convert.ToDouble(Math.Round(itemprice.TruncateDecimalPlaces(0))) > Math.Round(CompanyBuyPrice.TruncateDecimalPlaces(0)))
                    {
                        priceCheckCMDto.Flag = true;
                        return priceCheckCMDto;
                    }

                }
                else
                {
                    //  return true;
                    DataTable dttempRoomTable = new DataTable();
                    dttempRoomTable.Columns.Add("RoomNo");
                    dttempRoomTable.Columns.Add("NoOfAdult");
                    dttempRoomTable.Columns.Add("NoOfChild");
                    dttempRoomTable.Columns.Add("ChildAge1");
                    dttempRoomTable.Columns.Add("ChildAge2");

                    for (int r = 0; r < dsCartDetails.Tables[1].DefaultView.Count; r++)
                    {
                        DataRow dr = dttempRoomTable.NewRow();
                        dr["RoomNo"] = r + 1;
                        dr["NoOfAdult"] = dsCartDetails.Tables[1].DefaultView[r]["NoOfAdults"];
                        dr["NoOfChild"] = dsCartDetails.Tables[1].DefaultView[r]["NoOfChild"];
                        dr["ChildAge1"] = 0;
                        dr["ChildAge2"] = 0;
                        int NoofChild = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[r]["NoOfChild"]);
                        if (NoofChild >= 1)
                        {
                            if (NoofChild == 1)
                            {
                                dr["ChildAge1"] = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[r]["ChildAge1"]);
                            }
                            else
                            {
                                dr["ChildAge1"] = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[r]["ChildAge1"]);
                                dr["ChildAge2"] = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[r]["ChildAge2"]);
                            }
                        }
                        dttempRoomTable.Rows.Add(dr);
                    }

                    //string CheckIn = Convert.ToString(dsCartDetails.Tables[1].DefaultView[0]["CheckInDate"]);
                    // string CheckOut = Convert.ToString(dsCartDetails.Tables[1].DefaultView[0]["CheckOutDate"]);

                    string CheckIn = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["CheckInDate"]);
                    CheckIn = CheckIn.Substring(0, 4) + "-" + CheckIn.Substring(4, 2) + "-" + CheckIn.Substring(6, 2);
                    string CheckOut = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["CheckOutDate"]);
                    CheckOut = CheckOut.Substring(0, 4) + "-" + CheckOut.Substring(4, 2) + "-" + CheckOut.Substring(6, 2);


                    // CheckIn = CheckIn.Substring(6, 4) + "-" + CheckIn.Substring(3, 2) + "-" + CheckIn.Substring(0, 2);
                    //CheckOut = CheckOut.Substring(6, 4) + "-" + CheckOut.Substring(3, 2) + "-" + CheckOut.Substring(0, 2);

                    string RoomRequest = RakutenSearchRequestBeforePrebook(Convert.ToString(RakutenUrl), dttempRoomTable, CheckIn, CheckOut, Hotel_Id, XMLNationality, Convert.ToString(RakutenCurrency), 2);
                    ResponseDto responseDto = await Response_RakutenSupplier(RoomRequest, Password, "GET", "room");
                    string RoomResponseData = responseDto.Response;
                    Error = responseDto.Error;

                    if (RoomResponseData == "")
                    {
                        priceCheckCMDto.Flag = true;
                        return priceCheckCMDto;
                    }

                    RakutenHotelRoomResponse.Root rakutenResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<RakutenHotelRoomResponse.Root>(RoomResponseData);
                    if (rakutenResponse == null)
                    {
                        priceCheckCMDto.Flag = true;
                        return priceCheckCMDto;
                    }

                    if (rakutenResponse.status == "in-progress")
                    {
                        ResponseDto responsedt = await Response_RakutenSupplier(RoomRequest, Password, "GET", "room");
                        RoomResponseData = responsedt.Response;
                        Error = responsedt.Error;

                        if (RoomResponseData == "")
                        {
                            priceCheckCMDto.Flag = true;
                            return priceCheckCMDto;
                        }

                        rakutenResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<RakutenHotelRoomResponse.Root>(RoomResponseData);

                        if (rakutenResponse == null)
                        {
                            priceCheckCMDto.Flag = true;
                            return priceCheckCMDto;
                        }
                    }

                    _commonMethod.ThirdPartyBookingHistory(Convert.ToString(UserCartId), Convert.ToString(UserCartDetailId), RoomRequest, RoomResponseData, "Room request hit befor Price check", "rakuten", "response confirm", "-", "WE");

                    //Common.ThirdPartyGetPolicyHistory(Convert.ToString(roomtypeid), Convert.ToString(ownsystemid), Convert.ToString(RoomRequest), Convert.ToString(RoomResponseData), "Room Request Response before Policy", "Rakuten", "Room Response", "-", "WithinEarth");

                    RakutenHotelRoomResponse.Search SearchPar = rakutenResponse.search;
                    RakutenHotelRoomResponse.Package package = rakutenResponse.hotels[0].rates.packages.Where(item => item.booking_key == bookingKey && item.room_details.food == mealid).FirstOrDefault();

                    string Search = Newtonsoft.Json.JsonConvert.SerializeObject(SearchPar);
                    string packag = Newtonsoft.Json.JsonConvert.SerializeObject(package);

                    StringBuilder rakutenPriceCheck = new StringBuilder();
                    rakutenPriceCheck.Append("{");
                    rakutenPriceCheck.Append("\"search\"" + ":" + Search);
                    rakutenPriceCheck.Append(",");
                    rakutenPriceCheck.Append("\"package\"" + ":" + packag);
                    rakutenPriceCheck.Append("}");
                    string PriceCheckRequest = rakutenPriceCheck.ToString();
                    _commonMethod.ThirdPartyBookingHistory(Convert.ToString(UserCartId), Convert.ToString(UserCartDetailId), PriceCheckRequest, "", "Before request hit Price check", "rakuten", "response confirm", "-", "WE");

                    if (PriceCheckRequest == "")
                    {
                        priceCheckCMDto.Flag = true;
                        return priceCheckCMDto;
                    }


                    ResponseDto responseDto1 = await Get_Rakuten_Response(Convert.ToString(RakutenUrl) + Convert.ToString(RakutenInfo.Rakuten_PriceCheck), Convert.ToString(Password), PriceCheckRequest, "Rakuten");

                    string ResponseData = responseDto1.Response;
                    Error = responseDto1.Error;

                    _commonMethod.ThirdPartyBookingHistory(Convert.ToString(UserCartId), Convert.ToString(UserCartDetailId), PriceCheckRequest, ResponseData, "After request hit Price check", "rakuten", "response confirm", "-", "WE");
                    //  Common.ThirdPartyGetPolicyHistory(Convert.ToString(roomtypeid), Convert.ToString(ownsystemid), PriceCheckRequest, Convert.ToString(ResponseData), "After Get Policy", "Rakuten", "Policy Response", "-", "WithinEarth");

                    if (ResponseData == "")
                    {
                        priceCheckCMDto.Flag = true;
                        return priceCheckCMDto;
                    }

                    RakutenPriceCheckResponse.Root RakutenResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<RakutenPriceCheckResponse.Root>(ResponseData);

                    if (RakutenResponse == null)
                    {
                        priceCheckCMDto.Flag = true;
                        return priceCheckCMDto;
                    }

                    string booking_policy_id = Convert.ToString(RakutenResponse.booking_policy_id);
                    string Prices = Convert.ToString(RakutenResponse.package.chargeable_rate);

                    double FinalPriceMain = 0;
                    double FinalPriceTemp = Convert.ToDouble(Prices);

                    FinalPriceMain = FinalPriceTemp * CurrencyFactorRakuten;

                    itemprice = Convert.ToDouble(FinalPriceMain);


                    string PurchaseToken = Prices + "$$$" + booking_policy_id;

                    if (!await _commonMethod.GetEANBookLinkUpdate(PurchaseToken, UserCartId))
                    {
                        priceCheckCMDto.Flag = true;
                        return priceCheckCMDto;
                    }


                    itemprice = Convert.ToDouble(Math.Round(itemprice, 2));
                    priceCheckCMDto.ItemPrice = itemprice;

                    if (Convert.ToDouble(Math.Round(itemprice.TruncateDecimalPlaces(0))) > Math.Round(CompanyBuyPrice.TruncateDecimalPlaces(0)))
                    {
                        responsetype = "GeneralError";
                        ReponseSPR = "#Price Changed " + "#Main Price = " + CompanyBuyPrice + " #New Price = " + itemprice + " #Response= " + ReponseSPR;
                        {
                            priceCheckCMDto.Flag = true;
                            return priceCheckCMDto;
                        }
                    }
                }
                #region SupplierPR Price Check
                try
                {
                    string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook("Rakuten", "PreBook", AgentId, time, responsetype, RequestSPR, ReponseSPR, Error);
                }
                catch (Exception e)
                {
                    _ = _exception.ErrorBooking(e, AgentId.ToString(), "", "");
                }
                #endregion

            }
            catch (Exception e)
            {
                _ = _exception.Error(e);
                IsError = true;
                #region SupplierPerformanceReport Catch
                try
                {
                    Error = e.Message + e.StackTrace.ToString();
                    responsetype = "GeneralError";
                    string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook("Rakuten", "PreBook", AgentId, time, responsetype, RequestSPR, ReponseSPR, Error);
                }
                catch (Exception ex)
                {
                    _ = _exception.ErrorBooking(ex, AgentId.ToString(), "", "");
                }
                #endregion
            }
            priceCheckCMDto.Flag = false;
            return priceCheckCMDto;
        }

        public string RakutenSearchRequestBeforePrebook(string Url, DataTable searchParam, string checkin, string checkout, string HotelIds, string RakutenNationality, string CurrencyCode, int reqtype)
        {
            try
            {

                #region Adults Child Count

                int NoOfRooms = searchParam.Rows.Count;
                int NoofAdults = 0;
                string ChildAge = "";
                for (int ser = 0; ser < searchParam.Rows.Count; ser++)
                {
                    if (ser == 0)
                        NoofAdults = Convert.ToInt32(searchParam.Rows[ser]["NoOfAdult"]);
                    else
                        NoofAdults = NoofAdults + Convert.ToInt32(searchParam.Rows[ser]["NoOfAdult"]);

                    switch (Convert.ToInt32(searchParam.Rows[ser]["NoOfChild"]))
                    {
                        case 1:
                            if (ChildAge == "")
                            {
                                ChildAge += Convert.ToInt32(searchParam.Rows[ser]["ChildAge1"]);
                            }
                            else
                            {
                                ChildAge += "," + Convert.ToInt32(searchParam.Rows[ser]["ChildAge1"]);
                            }
                            break;
                        case 2:

                            if (ChildAge == "")
                            {
                                ChildAge += Convert.ToInt32(searchParam.Rows[ser]["ChildAge1"]);
                            }
                            else
                            {
                                ChildAge += "," + Convert.ToInt32(searchParam.Rows[ser]["ChildAge1"]);
                            }

                            if (ChildAge == "")
                            {
                                ChildAge += Convert.ToInt32(searchParam.Rows[ser]["ChildAge2"]);
                            }
                            else
                            {
                                ChildAge += "," + Convert.ToInt32(searchParam.Rows[ser]["ChildAge2"]);
                            }
                            break;
                    }
                }
                #endregion

                StringBuilder rakutenhotellist = new StringBuilder();
                if (HotelIds != string.Empty)
                {
                    string HIds = "";
                    string[] Hotelid = HotelIds.Split(',');
                    for (int i = 0; i < Hotelid.Length; i++)
                    {
                        if (i == 0)
                            HIds = Hotelid[i];
                        else
                            HIds = HIds + "," + Hotelid[i];
                    }
                    switch (reqtype)
                    {
                        case 1:
                            rakutenhotellist.Append(Url + Convert.ToString(RakutenInfo.Rakuten_HotelSearch));
                            rakutenhotellist.Append("?hotel_id_list=" + HIds + "");
                            break;
                        case 2:
                            rakutenhotellist.Append(Url + Convert.ToString(RakutenInfo.Rakuten_RoomList));
                            rakutenhotellist.Append("?hotel_id=" + HIds + "");
                            break;
                    }

                    rakutenhotellist.Append("&check_in_date=" + checkin + "");
                    rakutenhotellist.Append("&check_out_date=" + checkout + "");
                    rakutenhotellist.Append("&adult_count=" + NoofAdults + "");
                    rakutenhotellist.Append("&room_count=" + NoOfRooms + "");
                    rakutenhotellist.Append("&currency=" + CurrencyCode + "");
                    rakutenhotellist.Append("&source_market=" + RakutenNationality + "");
                    rakutenhotellist.Append("&locale=en-US");
                    if (ChildAge != "")
                        rakutenhotellist.Append("&children=" + ChildAge + "");
                }
                string rakutenReq = rakutenhotellist.ToString();
                return rakutenReq;
            }
            catch (Exception ex)
            {
                _ = _exception.Error(ex);
                return "";
            }
        }

        public string Rakuten_PriBookingRQ(DataSet dsCartDetails, DataTable dtPassengers, DataRow[] DerbySoftDetails, string UniqueNo, bool IsPriceUpdate, int i, int j, string PreBookingToken, string UserName, string Password, string ExternalReference, string xmlNationality)
        {
            try
            {
                #region room PreBooking RQ

                dtPassengers.DefaultView.RowFilter = "PickUp='" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomTypeId"]) + "' and UserCartID='" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["UserCartID"]) + "'";
                DataTable dtnewpassanger = dtPassengers.DefaultView.ToTable();

                DataView view = new DataView(dtnewpassanger);
                DataTable distinctValues = view.ToTable(true, "RoomNo");
                List<RakutenPreBookRequest.RoomLeadGuest> lstleadGuests = new List<RakutenPreBookRequest.RoomLeadGuest>();
                if (distinctValues.Rows.Count > 0)
                {
                    for (int p = 0; p < distinctValues.Rows.Count; p++)
                    {
                        DataRow[] drPassenger = dtnewpassanger.Select("RoomNo='" + distinctValues.Rows[p]["RoomNo"] + "'");


                        for (int r = 0; r < drPassenger.Length; r++)
                        {
                            if (r == 0)
                            {
                                RakutenPreBookRequest.RoomLeadGuest Guest = new RakutenPreBookRequest.RoomLeadGuest();
                                Guest.salutation = Convert.ToString(drPassenger[r]["Prefix"]);
                                Guest.first_name = Convert.ToString(drPassenger[r]["FirstName"]);
                                Guest.last_name = Convert.ToString(drPassenger[r]["LastName"]);
                                Guest.nationality = xmlNationality;
                                lstleadGuests.Add(Guest);
                            }
                        }
                    }
                }

                RakutenPreBookRequest.ContactPerson ContactPerson = new RakutenPreBookRequest.ContactPerson();
                ContactPerson.salutation = "Mr.";
                ContactPerson.first_name = Convert.ToString(RakutenInfo.ContactPerson_firstName);
                ContactPerson.last_name = Convert.ToString(RakutenInfo.ContactPerson_lastName);
                ContactPerson.contact_no = Convert.ToString(RakutenInfo.ContactPerson_phone);

                RakutenPreBookRequest.Root prebookrequest = new RakutenPreBookRequest.Root();
                prebookrequest.booking_policy_id = PreBookingToken;
                prebookrequest.client_reference = ExternalReference;
                prebookrequest.room_lead_guests = lstleadGuests;
                prebookrequest.contact_person = ContactPerson;




                //JsonSerializerSettings settings = new JsonSerializerSettings();
                //settings.NullValueHandling = NullValueHandling.Ignore;
                //settings.DefaultValueHandling = DefaultValueHandling.Ignore;
                string jsonPrebook = Newtonsoft.Json.JsonConvert.SerializeObject(prebookrequest);//, settings);
                return jsonPrebook;

                #endregion
            }
            catch (Exception ex)
            {
                _ =_exception.Error(ex);
            }
            return string.Empty;
        }
    }
}
