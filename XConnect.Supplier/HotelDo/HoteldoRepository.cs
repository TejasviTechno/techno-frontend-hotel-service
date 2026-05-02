using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using static XConnect.Supplier.Iboosy.iBoosyClass;
using System.Xml.Linq;
using XConnect.CommonUtilities.Interface;
using XConnect.CommonUtilities;
using XConnect.ErrorLogHandler.Interface;
using XConnect.Markup.Interface;
using XConnect.Models.Hotel;
using XConnect.Models;
using XConnect.Supplier.HotelDo.Interface;
using Options = XConnect.Models.Options;
using static Azure.Core.HttpHeader;
using System.Net;
using static XConnect.Models.ErrorLog;

namespace XConnect.Supplier.HotelDo
{
    public class HoteldoRepository : IHoteldo
    {
        private readonly ICommonMethod _commonMethod;
        private readonly IException _exception;
        private readonly IMarkup _markup;
        private readonly ISupplierRes _supplierRes;
        private readonly KeySettings _keySettings;
        public HoteldoRepository(ICommonMethod commonMethod, IMarkup markup, ISupplierRes supplierRes, IException exception, IOptions<KeySettings> keySettings)
        {
            _commonMethod = commonMethod;
            _markup = markup;
            _supplierRes = supplierRes;
            _exception = exception;
            _keySettings = keySettings.Value;
        }

        public void GetHotelDoRequestOnly_async(DataRow[] commonCode, List<StarRating> listRating, DataTable dtROE, HotelSearchParameter SearchPara, List<SupplierDetails> HotelDoDetails, List<SupplierSearchConfig> HotelDoSearchConfig, DataTable dtPreferredDataHotel, ref ConcurrentBag<HotelX> AllTask, string XMLCity, string XMLCountry, string XMLNationality, int Hit, TokenData tokenData)
        {


            try
            {
                string Ids = string.Empty;
                string xmlbase = "";
                _commonMethod.GetPrefferedHotelCodes(ref Ids, ref xmlbase, ref HotelDoSearchConfig, "HotelDo", dtPreferredDataHotel, SearchPara.IsSearchFor);
                #region "HotelDo"
                if (HotelDoSearchConfig != null)
                {
                    double CurrencyFactorHotelDo = 1;
                    string HotelDoCurrency = HotelDoDetails[0].CurrencyShortName;
                    if (HotelDoCurrency.ToLower() != Convert.ToString(tokenData.SiteCurrency).ToLower())
                    {
                        CurrencyFactorHotelDo = _commonMethod.GetROEByCurrency(HotelDoCurrency, dtROE);
                    }
                    for (int i = 0; i < HotelDoSearchConfig.Count; i++)
                    {
                        bool DebugMode = Convert.ToInt32(HotelDoSearchConfig[i].DebugMode) == 0 ? false : true;
                        string WebSiteUrl = Convert.ToString(HotelDoDetails[0].WebSiteURL);
                        string UserId = Convert.ToString(HotelDoDetails[0].UserID);
                        string UserName = Convert.ToString(HotelDoDetails[0].UserName);
                        string PassWord = Convert.ToString(HotelDoDetails[0].Password);
                        // List<HotelCodeByCity> DtHotelDAta = FilterFinalList(commonCode, listRating);
                        if (commonCode.Length > 0)
                        {
                            if (Convert.ToInt32(HotelDoSearchConfig[i].CityID) == 0 || Convert.ToInt32(HotelDoSearchConfig[i].CityID) == SearchPara.CityID)
                            {
                                int NoOfRecords = Convert.ToInt32(HotelDoSearchConfig[i].Noofrecords);
                                DataTable[] splittedtables = commonCode.AsEnumerable()
                                                          .Select((row, index) => new { row, index })
                                                          .GroupBy(x => x.index / NoOfRecords)  // integer division, the fractional part is truncated
                                                          .Select(g => g.Select(x => x.row).CopyToDataTable())
                                                          .ToArray();
                                for (int a = 0; a < splittedtables.Length; a++)
                                {

                                    var paramValues1 = string.Join(",", splittedtables[a].Select().Select(x => string.Join(",", Convert.ToString(x.ItemArray[0]))));
                                    string IdsForSearch = Convert.ToString(paramValues1);
                                    HotelX xhotel = new HotelX();
                                    xhotel.Supplier = "HotelDo";
                                    xhotel.SearchTimeOut = Convert.ToInt32(HotelDoSearchConfig[i].HotelSearchTimeOut);
                                    //if (Hit > 0)
                                    //{
                                    xhotel.ProcessTimeout = Convert.ToInt32(HotelDoSearchConfig[i].HotelSearchTimeOut2);
                                    //}
                                    xhotel.DebugeMode = DebugMode;
                                    xhotel.URL = WebSiteUrl;
                                    xhotel.UserId = UserId;
                                    xhotel.UserName = UserName;
                                    xhotel.PassWord = PassWord;
                                    xhotel.CurrencyCode = HotelDoStaticInfo.HotelDoCurrencySupplier;
                                    xhotel.CurrencyFactor = CurrencyFactorHotelDo;
                                    xhotel.XmlCity = XMLCity;
                                    xhotel.XmlCountry = XMLCountry;
                                    xhotel.XmlNationality = XMLNationality;
                                    xhotel.MethoodType = "GET";

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
                                        objrq.Request = XMLHotelDoRequestOnly_async(WebSiteUrl, UserName, PassWord, xhotel.CurrencyCode, xhotel.SearchTimeOut, SearchPara, IdsForSearch, XMLCity, XMLCountry, XMLNationality);
                                        objrq.Response = ""; RQRS.Add(objrq);
                                    }
                                    xhotel.RQRS = RQRS;
                                    xhotel.ReturnRooms = Convert.ToInt16(HotelDoDetails[0].ReturnRooms);
                                    xhotel.TechName = Convert.ToString(HotelDoDetails[0].TechName);
                                    #endregion
                                    AllTask.Add(xhotel);
                                }
                            }
                        }
                    }
                }
                #endregion
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
            }
        }

        public void GetHotelDoRequestOnly_async_new(DataRow[] commonCode, List<StarRating> listRating, DataTable dtROE, HotelSearchParameter SearchPara, List<SupplierDetails> lstHotelDoDetails, List<SupplierSearchConfig> lstHotelDoSearchConfig, DataTable dtPreferredDataHotel, ref ConcurrentBag<HotelX> AllTask, int Hit, TokenData tokenData, DataSet dtXMLCity)
        {
            try
            {
                if (lstHotelDoDetails.Count > 0)
                {
                    List<StarRating> lstRating = new List<StarRating>();
                    lstRating = _commonMethod.GetStarRatingStringList_xtg(SearchPara);

                    List<HotelCodeByCity> DtHotelDAta = HtmlExtensions.ToList<HotelCodeByCity>(commonCode.CopyToDataTable());
                    int ratingcount = lstRating.Count;
                    int hotelcount = DtHotelDAta.Count;

                    foreach (SupplierDetails HotelDoDetails in lstHotelDoDetails)
                    {
                        int SupplierID = HotelDoDetails.SupplierID;
                        string Supplier = HotelDoDetails.SupplierName;

                        if (lstHotelDoSearchConfig.Count > 0)
                        {
                            try
                            {
                                List<SupplierSearchConfig> HotelDoSearchConfig = lstHotelDoSearchConfig
                                  .Where(x => x != null && x.SupplierID == SupplierID)
                                  .ToList();


                                if (HotelDoSearchConfig != null && HotelDoSearchConfig.Count > 0)
                                {
                                    DataForSearch dataForSearch = _commonMethod.GetXmlDataForSearchTechwise(SearchPara.Nationality, dtXMLCity, Supplier, "HotelDo");

                                    if (dataForSearch.XmlNationality != "")
                                    {
                                        string Ids = string.Empty;
                                        string xmlbase = "";
                                        _commonMethod.GetPrefferedHotelCodes(ref Ids, ref xmlbase, ref HotelDoSearchConfig, Supplier, dtPreferredDataHotel, SearchPara.IsSearchFor);

                                        List<HotelCodeByCity> filter = new List<HotelCodeByCity>();
                                        filter = _commonMethod.FilterWithOutCity_Code(ratingcount, hotelcount, DtHotelDAta, lstRating, Supplier);


                                        #region "HotelDo"
                                        if (HotelDoSearchConfig != null)
                                        {
                                            double CurrencyFactorHotelDo = 1;
                                            string HotelDoCurrency = HotelDoDetails.CurrencyShortName;
                                            if (HotelDoCurrency.ToLower() != Convert.ToString(tokenData.SiteCurrency).ToLower())
                                            {
                                                CurrencyFactorHotelDo = _commonMethod.GetROEByCurrency(HotelDoCurrency, dtROE);
                                            }
                                            for (int i = 0; i < HotelDoSearchConfig.Count; i++)
                                            {
                                                bool DebugMode = Convert.ToInt32(HotelDoSearchConfig[i].DebugMode) == 0 ? false : true;


                                                //string WebSiteUrl = Convert.ToString(HotelDoDetails.WebSiteURL);
                                                string UserId = Convert.ToString(HotelDoDetails.UserID);
                                                string UserName = Convert.ToString(HotelDoDetails.UserName);
                                                string PassWord = Convert.ToString(HotelDoDetails.Password);
                                                // List<HotelCodeByCity> DtHotelDAta = FilterFinalList(commonCode, listRating);
                                                if (filter.Count > 0)
                                                {
                                                    if (Convert.ToInt32(HotelDoSearchConfig[i].CityID) == 0 || Convert.ToInt32(HotelDoSearchConfig[i].CityID) == SearchPara.CityID)
                                                    {
                                                        int NoOfRecord = Convert.ToInt32(HotelDoSearchConfig[i].Noofrecords);
                                                        double count = Convert.ToDouble(filter.Count);
                                                        int NoOfDiv = Convert.ToInt32(Math.Ceiling(count / Convert.ToDouble(NoOfRecord)));
                                                        List<List<XMLID_Search>> lstxml = _commonMethod.GetXMLIDString(NoOfDiv, NoOfRecord, count, filter);

                                                        for (int a = 0; a < NoOfDiv; a++)
                                                        {

                                                            //var paramValues1 = string.Join(",", splittedtables[a].Select().Select(x => string.Join(",", Convert.ToString(x.ItemArray[0]))));
                                                            //string IdsForSearch = Convert.ToString(paramValues1);
                                                            string IdsForSearch = Convert.ToString(_commonMethod.GetHotelCodeByCOmma(lstxml[a]));


                                                            HotelX xhotel = new HotelX();
                                                            xhotel.Supplier = Supplier;
                                                            xhotel.SearchTimeOut = Convert.ToInt32(HotelDoSearchConfig[i].HotelSearchTimeOut);
                                                            //if (Hit > 0)
                                                            //{
                                                            xhotel.ProcessTimeout = Convert.ToInt32(HotelDoSearchConfig[i].HotelSearchTimeOut2);
                                                            //}
                                                            xhotel.DebugeMode = DebugMode;
                                                            //xhotel.URL = WebSiteUrl;
                                                            if (_keySettings.SupplierProxy == "0")
                                                                xhotel.URL = Convert.ToString(HotelDoDetails.WebSiteURL);
                                                            else
                                                                xhotel.URL = Convert.ToString(HotelDoDetails.WebSiteURLforAPI);

                                                            xhotel.UserId = UserId;
                                                            xhotel.UserName = UserName;
                                                            xhotel.PassWord = PassWord;
                                                            xhotel.CurrencyCode = HotelDoCurrency; //USD
                                                            xhotel.SupplierCurrency = HotelDoStaticInfo.HotelDoCurrencySupplier;//US
                                                            xhotel.CurrencyFactor = CurrencyFactorHotelDo;
                                                            xhotel.XmlCity = dataForSearch.xmlCity;
                                                            xhotel.XmlCountry = dataForSearch.xmlCountry;
                                                            xhotel.XmlNationality = dataForSearch.XmlNationality;
                                                            xhotel.MethoodType = "GET";

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
                                                                objrq.Request = XMLHotelDoRequestOnly_async(xhotel.URL, UserName, PassWord, xhotel.SupplierCurrency, xhotel.SearchTimeOut, SearchPara, IdsForSearch, dataForSearch.xmlCity, dataForSearch.xmlCountry, dataForSearch.XmlNationality);
                                                                objrq.Response = ""; RQRS.Add(objrq);
                                                            }
                                                            xhotel.RQRS = RQRS;
                                                            xhotel.ReturnRooms = Convert.ToInt16(HotelDoDetails.ReturnRooms);
                                                            xhotel.TechName = Convert.ToString(HotelDoDetails.TechName);
                                                            #endregion
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
        public string XMLHotelDoRequestOnly_async(string Url, string UserName, string PassWord, string Currency, int SearchTimeOut, HotelSearchParameter SearchParam, string HotelIds, string CityCode, string XmlCountry, string XmlNationality)
        {
            string HotelDoPriceOnly = "";
            try
            {
                bool IsChild = false;
                bool IsAdult = false;
                for (int i = 0; i < SearchParam.Rooms.Count; i++)
                {
                    if (SearchParam.Rooms[i].NoofAdults > 9)
                        IsAdult = true;
                    else
                        continue;
                    //if (SearchParam.Rooms[i].Child1Age > 17)
                    //    IsChild = true;
                    //else if (SearchParam.Rooms[i].Child2Age > 17)
                    //    IsChild = true;
                    //else
                    //{
                    //    IsChild = false;
                    //    break;
                    //}
                }
                if (!IsAdult)
                {
                    if (!IsChild)
                    {
                        #region "PriceOnly Req"
                        // HotelDoPriceOnly = Get_HotelDo_PriceOnly(SearchParam, UserName, PassWord, Currency, HotelIds, CityCode, XmlCountry, XmlNationality);
                        HotelDoPriceOnly = Get_HotelDo_PriceOnly_v2(SearchParam, UserName, PassWord, Currency, HotelIds, CityCode, XmlCountry, XmlNationality);
                        #endregion
                    }
                }
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
                return "";
            }
            return HotelDoPriceOnly;
        }
        public string Get_HotelDo_PriceOnly(HotelSearchParameter SearchParam, string AffiliateId, string PassWord, string Currency, string HotelIds, string CityCode, string CountryCode, string Nationality)
        {
            string HotelDoReq = "";
            try
            {
                string CheckIn = Convert.ToString(SearchParam.CheckInDate);
                string CheckOut = Convert.ToString(SearchParam.CheckOutDate);
                CheckIn = CheckIn.Substring(6, 4) + CheckIn.Substring(3, 2) + CheckIn.Substring(0, 2);
                CheckOut = CheckOut.Substring(6, 4) + CheckOut.Substring(3, 2) + CheckOut.Substring(0, 2);

                StringBuilder Hoteldo = new StringBuilder();
                Hoteldo.Append("?a=" + AffiliateId + "");
                Hoteldo.Append("&co=" + CountryCode + "");
                Hoteldo.Append("&c=" + Currency + "");
                Hoteldo.Append("&sd=" + CheckIn + "");
                Hoteldo.Append("&ed=" + CheckOut + "");
                Hoteldo.Append("&h=" + HotelIds + "");
                Hoteldo.Append("&r=" + SearchParam.Rooms.Count + "");
                for (int i = 0; i < SearchParam.Rooms.Count; i++)
                {
                    int adult = SearchParam.Rooms[i].NoofAdults;
                    int child = SearchParam.Rooms[i].NoofChild;
                    Hoteldo.Append("&r" + (i + 1) + "a=" + adult + "");
                    Hoteldo.Append("&r" + (i + 1) + "k=" + child + "");
                    if (child >= 1)
                    {
                        switch (child)
                        {
                            case 1:
                                Hoteldo.Append("&r" + (i + 1) + "k1a=" + SearchParam.Rooms[i].Child1Age + "");
                                break;
                            case 2:
                                Hoteldo.Append("&r" + (i + 1) + "k1a=" + SearchParam.Rooms[i].Child1Age + "");
                                Hoteldo.Append("&r" + (i + 1) + "k2a=" + SearchParam.Rooms[i].Child2Age + "");
                                break;
                            default:
                                break;
                        }
                    }
                }
                Hoteldo.Append("&l=ING");
                Hoteldo.Append("&hash=ha:true;hd:true");
                HotelDoReq = Hoteldo.ToString();

            }
            catch (Exception ex)
            {
                _exception.Error(ex);
                return "";
            }
            return HotelDoReq;
        }
        public string Get_HotelDo_PriceOnly_v2(HotelSearchParameter SearchParam, string AffiliateId, string PassWord, string Currency, string HotelIds, string CityCode, string CountryCode, string Nationality)
        {
            string HotelDoReq = "";
            try
            {
                string CheckIn = Convert.ToString(SearchParam.CheckInDate);
                string CheckOut = Convert.ToString(SearchParam.CheckOutDate);
                CheckIn = CheckIn.Substring(6, 4) + CheckIn.Substring(3, 2) + CheckIn.Substring(0, 2);
                CheckOut = CheckOut.Substring(6, 4) + CheckOut.Substring(3, 2) + CheckOut.Substring(0, 2);

                StringBuilder Hoteldo = new StringBuilder();
                Hoteldo.Append("?a=" + AffiliateId + "");
                Hoteldo.Append("&co=" + Nationality + "");
                Hoteldo.Append("&c=" + Currency + "");
                Hoteldo.Append("&sd=" + CheckIn + "");
                Hoteldo.Append("&ed=" + CheckOut + "");
                Hoteldo.Append("&h=" + HotelIds + "");
                Hoteldo.Append("&r=" + SearchParam.Rooms.Count + "");
                for (int i = 0; i < SearchParam.Rooms.Count; i++)
                {
                    int adult = SearchParam.Rooms[i].NoofAdults;
                    int child = SearchParam.Rooms[i].NoofChild;
                    Hoteldo.Append("&r" + (i + 1) + "a=" + adult + "");
                    Hoteldo.Append("&r" + (i + 1) + "k=" + child + "");
                    if (child >= 1)
                    {
                        switch (child)
                        {
                            case 1:
                                Hoteldo.Append("&r" + (i + 1) + "k1a=" + SearchParam.Rooms[i].Child1Age + "");
                                break;
                            case 2:
                                Hoteldo.Append("&r" + (i + 1) + "k1a=" + SearchParam.Rooms[i].Child1Age + "");
                                Hoteldo.Append("&r" + (i + 1) + "k2a=" + SearchParam.Rooms[i].Child2Age + "");
                                break;
                            default:
                                break;
                        }
                    }
                }
                Hoteldo.Append("&l=ING");
                Hoteldo.Append("&hash=ha:true;hd:true");
                if (SearchParam.Filters.IsShowRooms == 0)
                    Hoteldo.Append("&bestPriceOnly=true"); //newv2
                HotelDoReq = Hoteldo.ToString();

            }
            catch (Exception ex)
            {
                _exception.Error(ex);
                return "";
            }
            return HotelDoReq;
        }

        public async Task<List<Options>> HotelDO_HotelSearchWithRooms(HotelX Item, CommonSearch commonSearch, string SupplierCurrency, string Currency, string ResponseData, string CityCode, string CountryCode, HotelSearchParameter SearchParam, double CurrencyFactorHotelDo, int processTimeout, string ResProccessTimerLogActive, string ResProccessTimeValueforSaveLog, string XmlNationality)
        {
            System.Diagnostics.Stopwatch timerroomx = new System.Diagnostics.Stopwatch();
            timerroomx.Start();
            List<Options> options = new List<Options>(); ;
            try
            {
                if (ResponseData == null || ResponseData == "" || ResponseData == string.Empty)
                {
                    Item.ResponseType = "NoCount";
                    return options;
                }
                //Move from above (3-AUG-2024) due to error root element is missing
                ResponseData = XElement.Parse(ResponseData).ToString(SaveOptions.DisableFormatting);
                ResponseData = ResponseData.Replace("</Id><AgencyPublic>", "</Id><AgencyPublicS>");
                ResponseData = ResponseData.Replace("</GrossAgencyPublic></AgencyPublic>", "</GrossAgencyPublic></AgencyPublicS>");
                DataTable DtHotel = _commonMethod.GenerateDataTableNewV2();
                DataSet HotelDoResponse = _commonMethod.GetDataSetFromXMLString(ResponseData);
                if (HotelDoResponse == null)
                // int totalAdult = 0, totalChild = 0;
                //double VatonfirstnightPrice = 0; //ItemPrice = 0;
                //for (int b = 0; b < SearchParam.Rooms.Count; b++)
                //{
                //    totalAdult = Convert.ToInt32(SearchParam.Rooms[b].NoofAdults) + totalAdult;
                //    totalChild = Convert.ToInt32(SearchParam.Rooms[b].NoofChild) + totalChild;
                //}
                {
                    Item.ResponseType = "NoCount";
                    return options;
                }
                // if (HotelDoResponse.Tables.Contains("QuoteHotels") == true)
                //{
                if (HotelDoResponse.Tables.Contains("Hotel") == true)
                {
                    for (int hh = 0; hh < HotelDoResponse.Tables["Hotel"].Rows.Count; hh++)
                    {
                        Item.ResponseType = "Success";
                        double FinalMinPrice = 0;
                        #region "When not Need Room"  
                        // add hotel code from price only and room code blank here
                        if (SearchParam.Filters.IsShowRooms == 0)
                        {
                            #region "Hotel Code Block "
                            string HotelCode = Convert.ToString(HotelDoResponse.Tables["Hotel"].Rows[hh]["Id"]);
                            if (commonSearch.tokenData.AgentId != 1899)
                            {
                                int HotelId = Convert.ToInt32(HotelDoResponse.Tables["Hotel"].Rows[hh]["Hotel_Id"]);
                                bool IsBindingRate = Convert.ToBoolean(HotelDoResponse.Tables["Hotel"].Rows[hh]["IsBindingRate"]);
                                if (SupplierCurrency.ToLower() != Convert.ToString(HotelDoResponse.Tables["Hotel"].Rows[hh]["Currency"]).ToLower())
                                    continue;
                                DataRow[] drRooms = HotelDoResponse.Tables["Rooms"].Select("Hotel_Id=" + HotelId + "");
                                DataRow[] drRoom = HotelDoResponse.Tables["Room"].Select("Rooms_Id=" + drRooms[0]["Rooms_Id"] + "");
                                //#hoteldochangelist
                                //  int Contract = 0;
                                List<RoomData> lstprice = new List<RoomData>();
                                for (int j = 0; j < drRoom.Length; j++)
                                {
                                    DataRow[] drMealPlans = HotelDoResponse.Tables["MealPlans"].Select("Room_Id=" + drRoom[j]["Room_Id"] + "");
                                    DataRow[] drMealPlan = HotelDoResponse.Tables["MealPlan"].Select("MealPlans_Id=" + drMealPlans[0]["MealPlans_Id"] + "");
                                    for (int k = 0; k < drMealPlan.Length; k++)
                                    {
                                        int MealPlanId = Convert.ToInt32(drMealPlan[k]["MealPlan_Id"]);
                                        DataRow[] drRateDetails = HotelDoResponse.Tables["RateDetails"].Select("MealPlan_Id=" + MealPlanId + "");
                                        DataRow[] drRateDetail = HotelDoResponse.Tables["RateDetail"].Select("RateDetails_Id=" + drRateDetails[0]["RateDetails_Id"] + "");
                                        double Price = 0;
                                        for (int m = 0; m < drRateDetail.Length; m++)
                                        {
                                            if (IsBindingRate)
                                            {
                                                int RateDetail_Id = Convert.ToInt32(drRateDetail[m]["RateDetail_Id"]);
                                                DataRow[] dragencyDetails = HotelDoResponse.Tables["AgencyPublicS"].Select("RateDetail_Id=" + RateDetail_Id + "");
                                                for (int n = 0; n < dragencyDetails.Length; n++)
                                                {
                                                    Price = Math.Round(Convert.ToDouble(dragencyDetails[n]["AgencyPublic"]), 2);
                                                }

                                            }
                                            else
                                            {
                                                Price = Math.Round(Convert.ToDouble(drRateDetail[m]["Total"]), 2);

                                            }


                                            //Contract = Convert.ToInt32(drMealPlan[k]["Contract"]);
                                            RoomData data = new RoomData();
                                            data.FinalSellingPrice = Price;
                                            lstprice.Add(data);
                                        }
                                    }
                                }
                                double HotelPrice = 0;
                                HotelPrice = Convert.ToDouble(lstprice.Min(x => x.FinalSellingPrice));
                                FinalMinPrice = HotelPrice;
                            }
                            else
                            {
                                FinalMinPrice = 1;
                            }
                            DataRow drXmlHotel = DtHotel.NewRow();
                            drXmlHotel["XMLHotelCode"] = HotelCode;
                            drXmlHotel["HotelName"] = "";
                            drXmlHotel["EssentialInformation"] = "";
                            drXmlHotel["Description"] = "";
                            drXmlHotel["IsRecomondedHotel"] = false;
                            drXmlHotel["SupplierName"] = Item.Supplier;
                            drXmlHotel["OfficeID"] = 0;
                            drXmlHotel["SupplierCurrency"] = Currency;
                            drXmlHotel["XMLCountry"] = CountryCode;
                            drXmlHotel["XMLCity"] = CityCode;
                            drXmlHotel["XmlHotelCity"] = CityCode;
                            drXmlHotel["ContractID"] = 0;
                            drXmlHotel["Rating"] = 0;
                            drXmlHotel["Reviews"] = 0;
                            drXmlHotel["UniqueNo"] = HotelCode;
                            drXmlHotel["IsShowSupplier"] = false;
                            drXmlHotel["BookingStatus"] = "Available";
                            drXmlHotel["FinalSellingPrice"] = FinalMinPrice;
                            drXmlHotel["SellingPrice"] = FinalMinPrice;
                            if (FinalMinPrice > 0)
                                DtHotel.Rows.Add(drXmlHotel);


                            #endregion


                            if (FinalMinPrice > 0)
                            {
                                #region "Room code Block"
                                bool willAddOrNot = true;
                                long RaynaID = 0;
                                long GiataId = 0;
                                int Rating = 0;
                                long OwnsystemHotelId = 0;
                                double MinPrice = FinalMinPrice;
                                _markup.GetHotelDataCommon(ref MinPrice, ref willAddOrNot, ref OwnsystemHotelId, ref RaynaID, ref GiataId, ref Rating, ref drXmlHotel, commonSearch.AgentId, commonSearch.TariffID, commonSearch.dsAgentDetails, commonSearch.supplierGiataMaster, SearchParam, commonSearch.dtCountryCityMarkup, commonSearch.B2CMarkup, commonSearch.IsParentId, commonSearch.SubAgentMarkup, commonSearch.SearchByHotel, commonSearch.HotelIDS);
                                if (willAddOrNot)
                                {
                                    #region "Room Logic"
                                    DataTable dt_HotelRoom = _commonMethod.GenerateRoomtypeDataTableNewV2();
                                    DataTable dt_Cancellation = _commonMethod.GenerateCancellationDataTableNewV2();
                                    DataTable dt_RateBreackUp = _commonMethod.GenerateoomRateBreackUpDataTableNewV2();
                                    #endregion
                                    decimal LowestPrice = Convert.ToDecimal(MinPrice).CalculatePrice(commonSearch.tokenData, commonSearch.advancedOptions, Currency);
                                    Options ops = await _supplierRes.GetCommonRoomForSuppliers(GiataId, commonSearch.tokenData, commonSearch.advancedOptions, Convert.ToDouble(LowestPrice), drXmlHotel, dt_HotelRoom, dt_Cancellation, dt_RateBreackUp, SearchParam);
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

                            #region "Hotel Block here"
                            // animesh changes code  21 feb 2019 for price 2 comments
                            string HotelCode = Convert.ToString(HotelDoResponse.Tables["Hotel"].Rows[hh]["Id"]);
                            int HotelId = Convert.ToInt32(HotelDoResponse.Tables["Hotel"].Rows[hh]["Hotel_Id"]);
                            bool IsBindingRate = Convert.ToBoolean(HotelDoResponse.Tables["Hotel"].Rows[hh]["IsBindingRate"]);
                            if (SupplierCurrency.ToLower() != Convert.ToString(HotelDoResponse.Tables["Hotel"].Rows[hh]["Currency"]).ToLower())
                                continue;

                            //#hoteldochange
                            //DataRow[] drRooms = HotelDoResponse.Tables["Rooms"].Select("Hotel_Id=" + HotelId + "");
                            //DataRow[] drRoom = HotelDoResponse.Tables["Room"].Select("Rooms_Id=" + drRooms[0]["Rooms_Id"] + "");
                            //int Contract = 0;
                            //List<RoomData> lstprice = new List<RoomData>();
                            //for (int j = 0; j < drRoom.Length; j++)
                            //{
                            //    DataRow[] drMealPlans = HotelDoResponse.Tables["MealPlans"].Select("Room_Id=" + drRoom[j]["Room_Id"] + "");
                            //    DataRow[] drMealPlan = HotelDoResponse.Tables["MealPlan"].Select("MealPlans_Id=" + drMealPlans[0]["MealPlans_Id"] + "");
                            //    for (int k = 0; k < drMealPlan.Length; k++)
                            //    {
                            //        Contract = Convert.ToInt32(drMealPlan[k]["Contract"]);
                            //        double Price = Convert.ToDouble(drMealPlan[k]["Total"]);
                            //        RoomData data = new RoomData();
                            //        data.FinalSellingPrice = Price;
                            //        lstprice.Add(data);
                            //    }
                            //}
                            //double HotelPrice = 0;
                            //HotelPrice = Convert.ToDouble(lstprice.Min(x => x.FinalSellingPrice));
                            //FinalMinPrice = HotelPrice;
                            //#hoteldochange

                            DataRow drXmlHotel = DtHotel.NewRow();
                            drXmlHotel["XMLHotelCode"] = HotelCode;
                            drXmlHotel["HotelName"] = "";
                            drXmlHotel["EssentialInformation"] = "";
                            drXmlHotel["Description"] = "";
                            drXmlHotel["IsRecomondedHotel"] = false;
                            drXmlHotel["SupplierName"] = "HotelDo";
                            drXmlHotel["OfficeID"] = 0;
                            drXmlHotel["SupplierCurrency"] = Currency;
                            drXmlHotel["XMLCountry"] = CountryCode;
                            drXmlHotel["XMLCity"] = CityCode;
                            drXmlHotel["XmlHotelCity"] = CityCode;
                            drXmlHotel["ContractID"] = 0;
                            drXmlHotel["Rating"] = 0;
                            drXmlHotel["Reviews"] = 0;
                            drXmlHotel["UniqueNo"] = HotelCode;
                            drXmlHotel["IsShowSupplier"] = false;
                            drXmlHotel["BookingStatus"] = "Available";
                            drXmlHotel["FinalSellingPrice"] = FinalMinPrice;
                            drXmlHotel["SellingPrice"] = FinalMinPrice;
                            //// if (TotalAmount > 0)
                            DtHotel.Rows.Add(drXmlHotel);

                            #endregion
                            #region "GetRooms for this hotel"
                            bool willAddOrNot = true;
                            long RaynaID = 0;
                            long GiataId = 0;
                            int RatingHotel = 0;
                            long OwnsystemHotelId = 0;
                            string Provider = Item.Supplier;
                            double MinPrice = 1;
                            _markup.GetHotelDataCommon(ref MinPrice, ref willAddOrNot, ref OwnsystemHotelId, ref RaynaID, ref GiataId, ref RatingHotel, ref drXmlHotel, commonSearch.AgentId, commonSearch.TariffID, commonSearch.dsAgentDetails, commonSearch.supplierGiataMaster, SearchParam, commonSearch.dtCountryCityMarkup, commonSearch.B2CMarkup, commonSearch.IsParentId, commonSearch.SubAgentMarkup, commonSearch.SearchByHotel, commonSearch.HotelIDS);
                            if (willAddOrNot)
                            {
                                DataTable dtHotelRoom = _commonMethod.GenerateRoomtypeDataTableNewV2();
                                DataTable dtHotelDoCancellation = _commonMethod.GenerateCancellationDataTableNewV2();
                                DataTable DtRoomRateBreackUp = _commonMethod.GenerateoomRateBreackUpDataTableNewV2();
                                int CutOffDays = XMLHotelBookingCommon.CutOffDays;
                                //string Currency1 = XMLHotelBookingCommon.HotelDoCurrencySupplier;
                                string Currency1 = SupplierCurrency;
                                CutOffDays = _commonMethod.GetCutOffDays(commonSearch.dtSuplierMarkup, Provider);
                                int totalnight = Convert.ToInt32(SearchParam.NoofNights);
                                #region "Room Logic"
                                if (SearchParam.Filters.IsShowRooms == 1)
                                {
                                    string EssentialInfo1 = "";
                                    if (HotelDoResponse.Tables["Hotel"].Columns.Contains("AdditionalCharges") == true)
                                    {
                                        EssentialInfo1 = Convert.ToString(HotelDoResponse.Tables["Hotel"].Rows[hh]["AdditionalCharges"]);
                                    }
                                    double AgentPercentage = 0.0;
                                    // Boolean blnIsShowSupplier = false;
                                    double AgentMarkupAmount = 0.0;
                                    bool isPerce = true;

                                    double AgentCompanyPercentage = Convert.ToDouble(SearchParam.AgentCompanyPercentage);
                                    bool IsB2C = Convert.ToBoolean(Convert.ToByte(SearchParam.IsB2BOrB2C));
                                    bool blnIsShowSupplier = false;
                                    _markup.getAgetMarkupSetting(Provider, GiataId.ToString(), commonSearch.dtSuplierMarkup, commonSearch.dtMarkupDetails, ref AgentPercentage, ref AgentMarkupAmount, ref blnIsShowSupplier, ref isPerce, commonSearch.AgentId, RatingHotel, SearchParam.CityID, SearchParam.CountryID.ToString(), SearchParam.CheckInDate);
                                    int maxOcupancy = 0;
                                    for (int i = 0; i < SearchParam.Rooms.Count; i++)
                                        maxOcupancy = maxOcupancy + SearchParam.Rooms[i].NoofChild + SearchParam.Rooms[i].NoofAdults;
                                    #region Supplier Room Code block
                                    //DataRow[] drInterface = HotelDoResponse.Tables["InterfaceInfo"].Select("Hotel_Id=" + HotelId + "");
                                    //string InterfaceId = Convert.ToString(drInterface[0]["Id"]);

                                    DataRow[] drRooms = HotelDoResponse.Tables["Rooms"].Select("Hotel_Id=" + HotelId + "");
                                    DataRow[] drRoom = HotelDoResponse.Tables["Room"].Select("Rooms_Id=" + drRooms[0]["Rooms_Id"] + "");
                                    for (int j = 0; j < drRoom.Length; j++)
                                    {
                                        int RoomId = Convert.ToInt32(drRoom[j]["Room_Id"]);
                                        string RoomCode = Convert.ToString(drRoom[j]["Id"]);
                                        string RoomName = Convert.ToString(drRoom[j]["Name"]);
                                        string BeddingType = Convert.ToString(drRoom[j]["Bedding"]);
                                        RoomName = RoomName + "(" + BeddingType + ")";
                                        DataRow[] drMealPlans = HotelDoResponse.Tables["MealPlans"].Select("Room_Id=" + RoomId + "");
                                        DataRow[] drMealPlan = HotelDoResponse.Tables["MealPlan"].Select("MealPlans_Id=" + drMealPlans[0]["MealPlans_Id"] + "");
                                        double RoomPrice = 0;
                                        string MealCode = "";
                                        string MealName = "", Contract = "";
                                        double ItemPrice = 0;
                                        string RoomTypeId = "";
                                        double VatonfirstnightPrice = 0;
                                        for (int k = 0; k < drMealPlan.Length; k++)
                                        {
                                            int MealPlanId = Convert.ToInt32(drMealPlan[k]["MealPlan_Id"]);
                                            //  DataRow[] drAvailable = Response[1].Tables["Available"].Select("MealPlan_Id=" + MealPlanId + "");
                                            //  for (int p = 0; p < drAvailable.Length; p++)
                                            {
                                                //string Status = Convert.ToString(drAvailable[p]["Status"]);
                                                // if (Status.ToLower() == "av")
                                                {
                                                    MealCode = Convert.ToString(drMealPlan[k]["Id"]);
                                                    MealName = Convert.ToString(drMealPlan[k]["Name"]);




                                                    DataRow[] drRateDetails = HotelDoResponse.Tables["RateDetails"].Select("MealPlan_Id=" + MealPlanId + "");
                                                    DataRow[] drRateDetail = HotelDoResponse.Tables["RateDetail"].Select("RateDetails_Id=" + drRateDetails[0]["RateDetails_Id"] + "");
                                                    for (int m = 0; m < drRateDetail.Length; m++)
                                                    {
                                                        double VatOnPolicy = 0;
                                                        int RateDetailId = Convert.ToInt32(drRateDetail[m]["RateDetail_Id"]);
                                                        string RateType = Convert.ToString(drRateDetail[m]["Id"]);
                                                        string RateDetailKey = Convert.ToString(drRateDetail[m]["RateKey"]);
                                                        if (IsBindingRate)
                                                        {
                                                            int RateDetail_Id = Convert.ToInt32(drRateDetail[m]["RateDetail_Id"]);
                                                            DataRow[] dragencyDetails = HotelDoResponse.Tables["AgencyPublicS"].Select("RateDetail_Id=" + RateDetail_Id + "");
                                                            //for (int n = 0; n < dragencyDetails.Length; n++)
                                                            {
                                                                ItemPrice = Math.Round(Convert.ToDouble(dragencyDetails[0]["AgencyPublic"]), 2);
                                                            }

                                                        }
                                                        else
                                                        {
                                                            ItemPrice = Math.Round(Convert.ToDouble(drRateDetail[m]["Total"]), 2);

                                                        }

                                                        RoomPrice = ItemPrice;
                                                        RoomTypeId = Convert.ToString(10 + "$$$" + RateDetailKey + "$$$" + RoomCode + "$$$" + Contract + "$$$" + Currency1 + "$$$" + IsBindingRate + "$$$" + XmlNationality);
                                                        RoomTypeId = RoomTypeId.Replace("'", "");
                                                        DataRow[] drCancellationPolicy = null;
                                                        if (HotelDoResponse.Tables.Contains("CancellationPolicy"))
                                                        {
                                                            if (HotelDoResponse.Tables["CancellationPolicy"].Columns.Contains("RateDetail_Id"))
                                                            {
                                                                drCancellationPolicy = HotelDoResponse.Tables["CancellationPolicy"].Select("RateDetail_Id=" + RateDetailId + "");
                                                                DataRow[] drNoShow = HotelDoResponse.Tables["NoShow"].Select("CancellationPolicy_Id=" + drCancellationPolicy[0]["CancellationPolicy_Id"] + "");
                                                                #region "Cancellation Policy"
                                                                for (int n = 0; n < drCancellationPolicy.Length; n++)
                                                                {
                                                                    string Description = "", DescriptionPolicy = "";
                                                                    //if (Response[1].Tables["CancellationPolicy"].Columns.Contains("Description"))
                                                                    //{
                                                                    //    EssentialInfo += "</br><b>Description:</b>" + Convert.ToString(drCancellationPolicy[n]["Description"]);

                                                                    //}
                                                                    //if (Response[1].Tables["CancellationPolicy"].Columns.Contains("DescriptionPolicy"))
                                                                    //{
                                                                    //    EssentialInfo += "</br><b>DescriptionPolicy:</b>" + Convert.ToString(drCancellationPolicy[n]["DescriptionPolicy"]);

                                                                    //}
                                                                    if (Convert.ToString(drCancellationPolicy[n]["IsNonRefundable"]) == "false")
                                                                    {
                                                                        double cancellationamt = 0;
                                                                        string checkin2 = Convert.ToString(SearchParam.CheckInDate);
                                                                        DateTime dtCheckin1 = DateTime.ParseExact(checkin2, "dd/MM/yyyy", null);
                                                                        cancellationamt = Math.Round(Convert.ToDouble(drCancellationPolicy[n]["Amount"]), 2);

                                                                        double cancellationChargeAmount = _markup.ProcessHotelCancelationPrice_WithMarkup(ref cancellationamt, SearchParam.NoofNights, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), cancellationamt, isPerce, commonSearch.B2CMarkup, AgentPercentage, AgentMarkupAmount, commonSearch.IsParentId, commonSearch.SubAgentMarkup, ref VatOnPolicy);
                                                                        int DaysToApply = Convert.ToInt32(drCancellationPolicy[n]["DaysToApplyCancellation"]);
                                                                        DateTime currentdate = DateTime.Now.Date;
                                                                        DateTime canceldate = dtCheckin1.AddDays(-DaysToApply).Date;
                                                                        if (canceldate < currentdate)
                                                                            canceldate = DateTime.Now.Date;
                                                                        if (canceldate.AddDays(CutOffDays).Date > DateTime.Now.Date)
                                                                        {
                                                                            canceldate = canceldate.AddDays(CutOffDays).Date;
                                                                        }
                                                                        else
                                                                        {
                                                                            canceldate = DateTime.Now.Date;
                                                                        }
                                                                        if (canceldate > currentdate)
                                                                        {
                                                                            if (n == 0)
                                                                            {
                                                                                double Zero = 0;
                                                                                dtHotelDoCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref Zero, dtHotelDoCancellation.NewRow(), CurrencyFactorHotelDo, Currency, Provider, OwnsystemHotelId.ToString(), Convert.ToString(1), RoomTypeId, RoomName, MealCode, currentdate, canceldate.AddDays(-1).AddHours(23).AddMinutes(59).AddSeconds(59), 0, 0, MealCode, VatonfirstnightPrice, "", "No", "", "", "", ""));
                                                                                dtHotelDoCancellation.AcceptChanges();
                                                                            }
                                                                            dtHotelDoCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref cancellationamt, dtHotelDoCancellation.NewRow(), CurrencyFactorHotelDo, Currency, Provider, OwnsystemHotelId.ToString(), Convert.ToString(1), RoomTypeId, RoomName, MealCode, canceldate, dtCheckin1, cancellationChargeAmount, cancellationChargeAmount, MealCode, VatonfirstnightPrice, "", "No", "", "", "", ""));
                                                                            dtHotelDoCancellation.AcceptChanges();
                                                                        }
                                                                        else
                                                                        {
                                                                            dtHotelDoCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref cancellationamt, dtHotelDoCancellation.NewRow(), CurrencyFactorHotelDo, Currency, Provider, OwnsystemHotelId.ToString(), Convert.ToString(1), RoomTypeId, RoomName, MealCode, canceldate, dtCheckin1, cancellationChargeAmount, cancellationChargeAmount, MealCode, VatonfirstnightPrice, "", "No", "", "", "", ""));
                                                                            dtHotelDoCancellation.AcceptChanges();
                                                                        }
                                                                    }
                                                                    else
                                                                    {
                                                                        double cancellationChargeAmount = _markup.ProcessHotelCancelationPrice_WithMarkup(ref ItemPrice, SearchParam.NoofNights, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), ItemPrice, isPerce, commonSearch.B2CMarkup, AgentPercentage, AgentMarkupAmount, commonSearch.IsParentId, commonSearch.SubAgentMarkup, ref VatOnPolicy);
                                                                        dtHotelDoCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref ItemPrice, dtHotelDoCancellation.NewRow(), CurrencyFactorHotelDo, Currency, Provider, OwnsystemHotelId.ToString(), Convert.ToString(1), RoomTypeId, RoomName, MealCode, DateTime.Now.Date, DateTime.Now.Date, cancellationChargeAmount, cancellationChargeAmount, MealCode, VatonfirstnightPrice, "", "Yes", "", "", "", ""));
                                                                        dtHotelDoCancellation.AcceptChanges();
                                                                    }
                                                                }
                                                                #endregion
                                                            }
                                                        }
                                                    }
                                                    //if (Response[1].Tables.Contains("NightsDetail"))
                                                    {
                                                        // DataRow[] drNightsDetail = Response[1].Tables["NightsDetail"].Select("MealPlan_Id=" + MealPlanId + "");
                                                        // DataRow[] drNightlyRate = Response[1].Tables["NightlyRate"].Select("NightsDetail_Id=" + drNightsDetail[0]["NightsDetail_Id"] + "");
                                                        int rooms = drRoom.Length;
                                                        #region "RateBreakUp"
                                                        //for (int l = 0; l < totalnight; l++)
                                                        //{
                                                        //    string newCheckInDate = Convert.ToString(SearchParam.CheckInDate);
                                                        //    string newCheckOutDate = Convert.ToString(SearchParam.CheckOutDate);
                                                        //    DateTime dtCheckInDate = DateTime.ParseExact(newCheckInDate, "dd/MM/yyyy", null);
                                                        //    DateTime dtCheckOutDate = DateTime.ParseExact(newCheckOutDate, "dd/MM/yyyy", null);
                                                        //    TimeSpan t = dtCheckOutDate - dtCheckInDate;
                                                        //    double NightPrice = Math.Round(Convert.ToDouble(RoomPrice), 2);
                                                        //    double ratePerNight = 0;
                                                        //    #region Vat
                                                        //    double VatOnPerNight = 0;
                                                        //    DateTime Day = DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null);
                                                        //    Day = Day.AddDays(l);
                                                        //    string Days = Convert.ToString(Day);
                                                        //    string rateWeekColumn = Convert.ToString(Day.Date.DayOfWeek).Substring(0, 3);
                                                        //    ratePerNight = _markup.ProcessHotelRateBreakupPrice_WithMarkup(1, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), NightPrice / totalnight, commonSearch.B2CMarkup, commonSearch.IsParentId, isPerce, AgentPercentage, AgentMarkupAmount, commonSearch.SubAgentMarkup, ref VatOnPerNight);
                                                        //    #endregion
                                                        //    VatonfirstnightPrice += VatOnPerNight;
                                                        //    //DataRow drHotelDoRoomRateBreakUp = dtHotelDoRateBreakUp.NewRow();
                                                        //    //drHotelDoRoomRateBreakUp["SupplierName"] = "HotelDo";
                                                        //    //drHotelDoRoomRateBreakUp["OwnsystemHotelId"] = ownsystemhotelId;
                                                        //    //drHotelDoRoomRateBreakUp["ContractID"] = 0;
                                                        //    //drHotelDoRoomRateBreakUp["RoomNo"] = 1;
                                                        //    //drHotelDoRoomRateBreakUp["RoomTypeId"] = RoomTypeId;
                                                        //    //drHotelDoRoomRateBreakUp["MealId"] = MealCode;
                                                        //    //drHotelDoRoomRateBreakUp["Meal"] = MealCode;

                                                        //    //drHotelDoRoomRateBreakUp["DateOfWeek"] = rateWeekColumn;

                                                        //    //drHotelDoRoomRateBreakUp["Date"] = Days;
                                                        //    //drHotelDoRoomRateBreakUp["Rate"] = ratePerNight;
                                                        //    //drHotelDoRoomRateBreakUp["OrderNo"] = 1;
                                                        //    //drHotelDoRoomRateBreakUp["RoomTypeName"] = RoomName;
                                                        //    //drHotelDoRoomRateBreakUp["DateOfWeek"] = rateWeekColumn;

                                                        //    //drHotelDoRoomRateBreakUp["BookingStatus"] = "Available";
                                                        //    //drHotelDoRoomRateBreakUp["BoardTypeID"] = "";
                                                        //    //drHotelDoRoomRateBreakUp["VatdiffAmount"] = VatOnPerNight;
                                                        //    //drHotelDoRoomRateBreakUp["RateType"] = j + 1 + "-" + RoomName;
                                                        //    //drHotelDoRoomRateBreakUp["SupplierCurrency"] = Currency;
                                                        //    //drHotelDoRoomRateBreakUp["BuyingPrice"] = ratePerNight / Factor;
                                                        //    //dtHotelDoRateBreakUp.Rows.Add(drHotelDoRoomRateBreakUp);
                                                        //}
                                                        #endregion
                                                    }
                                                }
                                            }
                                            #region "Promotion"
                                            DataRow drHoteldoRoom = dtHotelRoom.NewRow();
                                            if (HotelDoResponse.Tables.Contains("Promotions") == true)
                                            {
                                                DataRow[] drPromotions = HotelDoResponse.Tables["Promotions"].Select("MealPlan_Id=" + MealPlanId + "");
                                                DataRow[] drPromotion = HotelDoResponse.Tables["Promotion"].Select("Promotions_Id=" + drPromotions[0]["Promotions_Id"] + "");
                                                for (int r = 0; r < drPromotion.Length; r++)
                                                {
                                                    string OfferCode = Convert.ToString(drPromotion[r]["Id"]);
                                                    string OfferDesc = Convert.ToString(drPromotion[r]["Description"]);
                                                    drHoteldoRoom["OfferCode"] = OfferCode;
                                                    drHoteldoRoom["OfferDescription"] = OfferDesc;
                                                }
                                            }
                                            else
                                            {
                                                drHoteldoRoom["OfferCode"] = "";
                                                drHoteldoRoom["OfferDescription"] = "";
                                            }
                                            #endregion
                                            #region "Room List"
                                            drHoteldoRoom["OwnsystemHotelId"] = OwnsystemHotelId;
                                            drHoteldoRoom["RoomTypeCode"] = RoomCode;
                                            drHoteldoRoom["BoardType"] = "";
                                            drHoteldoRoom["MealName"] = MealName;
                                            drHoteldoRoom["MealId"] = MealCode;
                                            drHoteldoRoom["Type"] = "";
                                            drHoteldoRoom["Board"] = "";
                                            drHoteldoRoom["BookingStatus"] = "Available";
                                            drHoteldoRoom["UniqueNo"] = "";
                                            drHoteldoRoom["ContractID"] = 0;
                                            drHoteldoRoom["RoomNo"] = 1;
                                            drHoteldoRoom["BoardTypeID"] = "";
                                            drHoteldoRoom["AdultsOccupancy"] = 1;
                                            drHoteldoRoom["ChildOccupancy"] = 0;
                                            drHoteldoRoom["RoomSequenceNo"] = "";
                                            drHoteldoRoom["EssentialInformation"] = EssentialInfo1;
                                            drHoteldoRoom["Characteristic"] = "";
                                            #region Vat
                                            _markup.ProcessHotelRoom_WithMarkup(CurrencyFactorHotelDo, Currency, true, true, SearchParam.NoofNights * SearchParam.Rooms.Count, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), SearchParam.CheckInDate, SearchParam.CheckOutDate, ref RoomPrice, ref drHoteldoRoom, isPerce, AgentPercentage, AgentMarkupAmount, commonSearch.SubAgentMarkup, commonSearch.B2CMarkup, commonSearch.IsParentId);
                                            #endregion
                                            drHoteldoRoom["SupplierCurrency"] = Currency;
                                            drHoteldoRoom["SupplierName"] = Provider;
                                            drHoteldoRoom["RoomTypeName"] = RoomName;
                                            drHoteldoRoom["RoomTypeId"] = RoomTypeId;
                                            dtHotelRoom.Rows.Add(drHoteldoRoom);
                                            #endregion
                                        }

                                    }
                                    #endregion

                                }
                                #endregion
                                Options ops = await _supplierRes.GetCommonRoomForSuppliers(GiataId, commonSearch.tokenData, commonSearch.advancedOptions, FinalMinPrice, drXmlHotel, dtHotelRoom, dtHotelDoCancellation, DtRoomRateBreackUp, SearchParam);
                                if (ops != null)
                                    options.Add(ops);
                            }
                            #endregion
                            #endregion
                        }

                        #endregion

                    }
                }
                //}
                return options;
            }
            catch (Exception ex)
            {
                await _exception.Error(ex);
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
                        SearchParam.SupplierTime += ",|Finish:HotelDO Process Response  " + "|Time :" + timerroomx.Elapsed.ToString();
                        _ = Task.Run(() => _commonMethod.FireAway_SQL_ResPorcessTimeLog("", timerroomx.Elapsed.ToString(), "HotelDO", SearchParam.SearchKey, 1, SearchParam.AgentID));
                    }
                }
                catch (Exception x)
                {
                    await _exception.Error(x);
                }
                #endregion
            }
        }

        public async Task<string> Get_Hoteldo_EInfo(string hotelcode, string Username, string hoteldopassword, string hoteldoURL, string SearchKey)
        {
            string Essen1 = "";
            try
            {
                string EssentialInfo1 = "", Fees = "";

                #region Hoteldetails
                try
                {
                    string HoteldoUrl = hoteldoURL + "/GetHotelInformation"; // "http://ireu1xml.e-tsw.com/AffiliateService/V1.0/AffiliateService.svc/restful/GetHotelInformation";
                    string error = "";
                    string ResponseData = "";
                    //string Request = Common.HoteldoHotelInformationRequest("9012688", "10402789");
                    string Request = HoteldoHotelInformationRequest(hotelcode, Username);
                    //string ResponseData = XMLHotelBookingCommon.HotelDoResponse_New(error, Request, HoteldoUrl , "9012688", "mexpro", 120000, "GET", "");
                    ResponseDto res = await HotelDoResponseAsync(Request, HoteldoUrl, Username, hoteldopassword, 120000, "GET", "");

                    ResponseData = res.Response;
                    error = res.Error;

                    #region RemoveNode

                    #endregion

                    DataSet Hoteldo_Response1 = _commonMethod.GetDataSetFromXMLString(ResponseData);
                    // string EssentialInfo1 = "";


                    if (Hoteldo_Response1.Tables.Contains("Comfort") == true)
                    {
                        if (Hoteldo_Response1.Tables["Comfort"].Columns.Contains("ExtraCharge"))
                        {
                            Hoteldo_Response1.Tables["Comfort"].DefaultView.RowFilter = "ExtraCharge='true'";
                            for (int i = 0; i < Hoteldo_Response1.Tables["Comfort"].DefaultView.Count; i++)
                            {
                                if (i == 0)
                                {
                                    EssentialInfo1 = "<h3>Facilities</h3>";
                                    EssentialInfo1 = EssentialInfo1 + Convert.ToString(Hoteldo_Response1.Tables["Comfort"].DefaultView[i]["Name"]);
                                }
                                else
                                {
                                    EssentialInfo1 = EssentialInfo1 + "</br>" + Convert.ToString(Hoteldo_Response1.Tables["Comfort"].DefaultView[i]["Name"]);
                                }
                            }
                        }
                    }

                    try//Added on 1-JUN-2024 Ticket#45772
                    {
                        if (Hoteldo_Response1.Tables.Contains("Hotel") == true)
                        {
                            EssentialInfo1 += "</br><b>CheckIn:</b>" + Convert.ToString(Hoteldo_Response1.Tables["Hotel"].Rows[0]["CheckIn"]) + "</br> <b>CheckOut:</b>" + Convert.ToString(Hoteldo_Response1.Tables["Hotel"].Rows[0]["CheckOut"]) + "</br> ";
                            if (Hoteldo_Response1.Tables["Hotel"].Columns.Contains("AdditionalCharges") == true)
                            {
                                // if(Convert.ToString(Response[0].Tables["Hotel"].Rows[0]["AdditionalCharges"]) != "" || Convert.ToString(Response[0].Tables["Hotel"].Rows[0]["AdditionalCharges"]) != null)
                                {
                                    EssentialInfo1 += "</br> <b>AdditionalCharges:</b>" + Convert.ToString(Hoteldo_Response1.Tables["Hotel"].Rows[0]["AdditionalCharges"]);

                                }
                            }
                            if (Hoteldo_Response1.Tables["Hotel"].Columns.Contains("Notices") == true)
                            {
                                EssentialInfo1 += "</br> <b>Notices:</b>" + Convert.ToString(Hoteldo_Response1.Tables["Hotel"].Rows[0]["Notices"]);
                            }
                            if (Hoteldo_Response1.Tables.Contains("Fees") == true)
                            {
                                if (Hoteldo_Response1.Tables["Fees"].Rows.Count > 0)
                                {
                                    for (int v = 0; v < Hoteldo_Response1.Tables["Fees"].Rows.Count; v++)
                                    {
                                        int Fees_Id = Convert.ToInt32(Hoteldo_Response1.Tables["Fees"].Rows[v]["Fees_Id"]);
                                        if (Hoteldo_Response1.Tables.Contains("Fee") == true)
                                        {
                                            DataRow[] drFees = Hoteldo_Response1.Tables["Fee"].Select("Fees_Id=" + Fees_Id + "");
                                            if (drFees.Length > 0)
                                            {
                                                for (int j = 0; j < drFees.Length; j++)
                                                {

                                                    Fees = "<b>Currency:</b>" + Convert.ToString(drFees[j]["Currency"]);
                                                    Fees += "</br><b>Amount:</b>" + Convert.ToString(drFees[j]["Amount"]);
                                                    int Fee_Id = Convert.ToInt32(drFees[j]["Fee_Id"]);
                                                    if (Hoteldo_Response1.Tables.Contains("FeeType") == true)
                                                    {

                                                        DataRow[] FeeType = Hoteldo_Response1.Tables["FeeType"].Select("Fee_Id=" + Fee_Id + "");
                                                        if (FeeType.Length > 0)
                                                        {
                                                            for (int h = 0; h < FeeType.Length; h++)
                                                            {
                                                                Fees += "</br><b>Id:</b>" + Convert.ToString(FeeType[h]["Id"]);
                                                                Fees += "</br><b>Description:</b>" + Convert.ToString(FeeType[h]["Description"]);
                                                            }
                                                        }

                                                    }
                                                    if (Hoteldo_Response1.Tables.Contains("FeeApplicationScope") == true)
                                                    {
                                                        DataRow[] FeeApplicationScope = Hoteldo_Response1.Tables["FeeApplicationScope"].Select("Fee_Id=" + Fee_Id + "");

                                                        if (FeeApplicationScope.Length > 0)
                                                        {
                                                            int FeeApplicationScope_Id = Convert.ToInt32(FeeApplicationScope[j]["FeeApplicationScope_Id"]);
                                                            for (int h = 0; h < FeeApplicationScope.Length; h++)
                                                            {
                                                                Fees += "</br><b>Id:</b>" + Convert.ToString(FeeApplicationScope[h]["Id"]);
                                                                Fees += "</br><b>Description:</b>" + Convert.ToString(FeeApplicationScope[h]["Description"]);
                                                            }
                                                            if (Hoteldo_Response1.Tables.Contains("FeeAmountType") == true)
                                                            {
                                                                DataRow[] FeeAmountType = Hoteldo_Response1.Tables["FeeAmountType"].Select("FeeApplicationScope_Id=" + FeeApplicationScope_Id + "");

                                                                if (FeeAmountType.Length > 0)
                                                                {
                                                                    for (int h = 0; h < FeeAmountType.Length; h++)
                                                                    {
                                                                        Fees += "</br><b>Id:</b>" + Convert.ToString(FeeAmountType[h]["Id"]);
                                                                        Fees += "</br><b>Description:</b>" + Convert.ToString(FeeAmountType[h]["Description"]);
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
                            EssentialInfo1 += "</br> <b>Fees:</b>" + Fees;
                        }
                    }
                    catch (Exception x)
                    {
                        _ = _exception.ErrorNew(x, _LogAPI.Recheck, SearchKey);
                    }

                    //else
                    //{
                    //    EssentialInfo1 = " xyz";
                    //}

                    //if (Hoteldo_Response1.Tables.Contains("Hotel") == true)
                    //{
                    //    // Hoteldo_Response1.Tables["Comfort"].DefaultView.RowFilter = "ExtraCharge='true'";
                    //    for (int i = 0; i < Hoteldo_Response1.Tables["Hotel"].DefaultView.Count; i++)
                    //    {
                    //        if (i == 0)
                    //        {
                    //            EssentialInfo1 = "<h3>Facilities</h3>";
                    //            EssentialInfo1 = EssentialInfo1 + Convert.ToString(Hoteldo_Response1.Tables["Hotel"].DefaultView[i]["AdditionalCharges"]);
                    //        }
                    //        else
                    //            EssentialInfo1 = EssentialInfo1 + "</br>" + Convert.ToString(Hoteldo_Response1.Tables["Hotel"].DefaultView[i]["AdditionalCharges"]);
                    //    }
                    //}

                    return EssentialInfo1;
                }
                catch (Exception ex)
                {
                    _ = _exception.ErrorNew(ex, _LogAPI.Recheck, SearchKey);

                }

                #endregion
            }
            catch (Exception ex)
            {
                _ = _exception.ErrorNew(ex, _LogAPI.Recheck, SearchKey);
            }

            return Essen1;
        }

        public string HoteldoHotelInformationRequest(string Hotelcode, string AffiliateId)
        {
            string HotelInfo = "";
            try
            {
                StringBuilder Info = new StringBuilder();
                Info.Append("?a=" + AffiliateId + "");
                Info.Append("&ip=103.19.248.44");
                Info.Append("&l=ING");
                Info.Append("&c=US");
                Info.Append("&h=" + Hotelcode + "");
                HotelInfo = Info.ToString();
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
            }
            return HotelInfo;
        }

        public async Task<ResponseDto> HotelDoResponseAsync(string Request, string Url, string UserName, string Password, int TimeOut, string Method, string Searchkey = "")
        {

            ResponseDto responseDto = new ResponseDto();
            string Response = "";
            string Error = "";

            try
            {
                string ReqUrl = Url + Request;
                string Key = Convert.ToBase64String(Encoding.ASCII.GetBytes(UserName + ":" + Password));
                if (Method.ToLower() == "post")
                    ReqUrl = Url;

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(ReqUrl);
                request.Method = Method;
                request.ContentType = "application/xml";
                request.Accept = "application/xml";
                request.Headers.Add("Accept-Encoding", "gzip");
                request.Timeout = TimeOut;

                if(Method.ToLower() == "post")
                {
                    Stream newStream = null;
                    byte[] data = Encoding.ASCII.GetBytes(Request);
                    request.ContentLength = data.Length;
                    newStream = request.GetRequestStream();
                    newStream.Write(data, 0, data.Length);
                }

                HttpWebResponse Response2 = (HttpWebResponse)await request.GetResponseAsync();
                Stream st = Response2.GetResponseStream();

                if (Response2.ContentEncoding.ToLower().Contains("gzip"))
                    st = new System.IO.Compression.GZipStream(st, System.IO.Compression.CompressionMode.Decompress);
                else if (Response2.ContentEncoding.ToLower().Contains("deflate"))
                    st = new System.IO.Compression.DeflateStream(st, System.IO.Compression.CompressionMode.Decompress);
                StreamReader stIn = new StreamReader(st, Encoding.Default);
                Response = await stIn.ReadToEndAsync();
            }
            catch (Exception ex)
            {
                Error = ex.Message.ToString();
                _ = _exception.Error(ex, Searchkey);
            }

            responseDto.Response = Response;
            responseDto.Error = Error;
            return responseDto;
        }

        public async Task<PriceCheckCMDto> HotelDoPriceCheck_v2(DataTable dtCheckPriceReqStructure, DataSet dsCartDetails, DataRow[] HotelDoDetails, string UniqueNo, string AgentUniqueNo, int i, string IsFor, double CompanyBuyPrice, bool IsError, double itemprice, string Searchkey = "", int AgentId = 0)
        {
            PriceCheckCMDto priceCheckCMDto = new PriceCheckCMDto();
            string responsetype = "Success";
            string time = "";
            string SupplierName = Convert.ToString(dsCartDetails.Tables[1].DefaultView[0]["SupplierName"]);
            string Error = "";
            string PriceCheckRequest = "";
            string ResponseData = "";
            try
            {
                string CheckIn = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["CheckInDate"]);
                string CheckOut = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["CheckOutDate"]);
                string HotelCode = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["XMLHotelId"]);
                string xmlCity = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["XMLCityId"]);
                string xmlCountry = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["XMLCountryId"]);
                string Url = Convert.ToString(HotelDoDetails[0]["WebSiteURL"]);
                string UserName = Convert.ToString(HotelDoDetails[0]["UserName"]);
                string Password = Convert.ToString(HotelDoDetails[0]["PassWord"]);
                for (int k = 0; k < dsCartDetails.Tables[1].DefaultView.Count; k++)
                {
                    //Error = "";
                    PriceCheckRequest = "";
                    ResponseData = "";

                    string[] MBArray1 = Convert.ToString(dsCartDetails.Tables[1].DefaultView[k]["RoomTypeId"]).Split(new String[] { "$$$" }, StringSplitOptions.None);
                    string RateKey = MBArray1[1];
                    string RoomCode = MBArray1[2];
                    bool IsBindingRate = Convert.ToBoolean(MBArray1[5]);
                    //string MarketId = MBArray1[4];
                    string XmlNationality = MBArray1[6];
                    string MealCode = Convert.ToString(dsCartDetails.Tables[1].DefaultView[k]["MealPlanCode"]);
                    PriceCheckRequest = HotelDoPriceCheckRequest_v2(RateKey, k, UserName, Password, RoomCode, dsCartDetails, xmlCity, xmlCountry, /*Contract, MarketId, InterfaceId,*/ MealCode, CheckIn, CheckOut, HotelCode, XmlNationality);

                    _commonMethod.ThirdPartyBookingHistory(Convert.ToString(UniqueNo), Convert.ToString(AgentUniqueNo), PriceCheckRequest, "-", "Before Request hit Price Check", Convert.ToString(dsCartDetails.Tables[1].DefaultView[0]["SupplierName"]), "Request Confirm", "-", "WithInEarth");
                    dtCheckPriceReqStructure.Rows.Add(_commonMethod.AddCheckPriceReq(dtCheckPriceReqStructure.NewRow(), k + 1, Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[k]["UserCartID"]), Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[k]["UserCartDetailId"]), Convert.ToString(dsCartDetails.Tables[1].DefaultView[k]["RoomTypeID"]), Convert.ToString(dsCartDetails.Tables[1].DefaultView[k]["SupplierName"]), PriceCheckRequest));

                    System.Diagnostics.Stopwatch timerroomprocess = new System.Diagnostics.Stopwatch();
                    timerroomprocess.Start();

                    //ResponseData = XMLHotelBookingCommon.HotelDoResponsePricecheck(ref Error, PriceCheckRequest, Url + "/GetHotelRateRules", UserName, Password, 120000, "GET");
                    ResponseDto responseDto = await HotelDoResponseAsync(PriceCheckRequest, Url + "/GetHotelRateRules", UserName, Password, 120000, "GET");
                    ResponseData = responseDto.Response;
                    Error = responseDto.Error;

                    timerroomprocess.Stop();
                    time = timerroomprocess.Elapsed.ToString();

                    _commonMethod.ThirdPartyBookingHistory(Convert.ToString(UniqueNo), Convert.ToString(AgentUniqueNo), PriceCheckRequest, ResponseData + Error, "After Request hit Price Check", Convert.ToString(dsCartDetails.Tables[1].DefaultView[k]["SupplierName"]), "Response Confirm", "-", "WithInEarth");

                    if (ResponseData == "")
                    {

                        try
                        {
                            responsetype = "GeneralError";
                            //AgentId = Convert.ToInt32(Common.getSession("UserId") == "" ? "0" : Common.getSession("UserId"));
                            string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, PriceCheckRequest, ResponseData, Error);
                        }
                        catch (Exception e)
                        {
                            _ = _exception.Error(e, Searchkey);
                        }
                        priceCheckCMDto.Flag = false;
                        return priceCheckCMDto;
                    }
                    if (ResponseData.ToLower().Contains("error"))
                    {
                        try
                        {
                            responsetype = "GeneralError";
                            // int AgentId = Convert.ToInt32(Common.getSession("UserId") == "" ? "0" : Common.getSession("UserId"));
                            string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, PriceCheckRequest, ResponseData, Error);
                        }
                        catch (Exception e)
                        {
                            _ = _exception.Error(e, Searchkey);
                        }
                        priceCheckCMDto.Flag = false;
                        return priceCheckCMDto;
                    }
                    ResponseData = XElement.Parse(ResponseData).ToString(SaveOptions.DisableFormatting);
                    // ResponseData = ResponseData.Replace("</Name><AgencyPublic><AgencyPublic> ", "</Name><AgencyPublicS><AgencyPublic>");
                    ResponseData = ResponseData.Replace("<AgencyPublic><AgencyPublic>", "<AgencyPublic><AgencyPublicS>");
                    ResponseData = ResponseData.Replace("</AgencyPublic><GrossAgencyPublic>", "</AgencyPublicS><GrossAgencyPublic>");

                    //if (ResponseData.Contains("MealPlan") && !ResponseData.Contains("RateDetails"))
                    //{
                    //    string d = ResponseData;
                    //    var doc2 = XDocument.Parse(d);
                    //    var nodeToRemove2 = doc2.Descendants("AgencyPublic");
                    //    nodeToRemove2.Remove();
                    //    ResponseData = doc2.ToString();
                    //}
                    DataSet HotelDoResponse = _commonMethod.GetDataSetFromXMLString(ResponseData);
                    if (HotelDoResponse == null)
                    {
                        try
                        {
                            responsetype = "GeneralError";
                            //int AgentId = Convert.ToInt32(Common.getSession("UserId") == "" ? "0" : Common.getSession("UserId"));
                            string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, PriceCheckRequest, ResponseData, Error);
                        }
                        catch (Exception e)
                        {
                            _ = _exception.Error(e, Searchkey);
                        }
                        priceCheckCMDto.Flag = false;
                        return priceCheckCMDto;
                    }
                    if (HotelDoResponse.Tables.Contains("HotelRateRules") == true)
                    {
                        if (HotelDoResponse.Tables.Contains("HotelRate") == true)
                        {
                            for (int j = 0; j < HotelDoResponse.Tables["HotelRate"].Rows.Count; j++)
                            {
                                int HotelId = Convert.ToInt32(HotelDoResponse.Tables["HotelRate"].Rows[j]["HotelRate_Id"]);
                                DataRow[] drRooms = HotelDoResponse.Tables["Rooms"].Select("HotelRate_Id=" + HotelId + "");
                                DataRow[] drRoom = HotelDoResponse.Tables["Room"].Select("Rooms_Id=" + drRooms[0]["Rooms_Id"] + "");
                                for (int r = 0; r < drRoom.Length; r++)
                                {
                                    int RoomId = Convert.ToInt32(HotelDoResponse.Tables["Room"].Rows[r]["Room_Id"]);
                                    DataRow[] drMealPlans = HotelDoResponse.Tables["MealPlans"].Select("Room_Id=" + RoomId + "");
                                    DataRow[] drMealPlan = HotelDoResponse.Tables["MealPlan"].Select("MealPlans_Id=" + drMealPlans[0]["MealPlans_Id"] + "");
                                    for (int l = 0; l < drMealPlan.Length; l++)
                                    {
                                        int MealPlanId = Convert.ToInt32(drMealPlan[l]["MealPlan_Id"]);
                                        DataRow[] drRateDetails = HotelDoResponse.Tables["RateDetails"].Select("MealPlan_Id=" + MealPlanId + "");
                                        DataRow[] drRateDetail = HotelDoResponse.Tables["RateDetail"].Select("RateDetails_Id=" + drRateDetails[0]["RateDetails_Id"] + "");
                                        for (int m = 0; m < drRateDetail.Length; m++)
                                        {

                                            if (IsBindingRate)
                                            {
                                                int RateDetail_Id = Convert.ToInt32(drRateDetail[m]["RateDetail_Id"]);
                                                DataRow[] dragencyDetails = HotelDoResponse.Tables["AgencyPublic"].Select("RateDetail_Id=" + RateDetail_Id + "");
                                                //for (int n = 0; n < dragencyDetails.Length; n++)
                                                {
                                                    itemprice = Math.Round(Convert.ToDouble(dragencyDetails[0]["AgencyPublicS"]), 2);
                                                }

                                            }
                                            else
                                            {
                                                itemprice += Math.Round(Convert.ToDouble(drRateDetail[m]["Total"]), 2);

                                            }
                                            // itemprice += Convert.ToDouble(drMealPlan[l]["Total"]);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                if (IsFor == "P")
                {
                    if (Convert.ToDouble((itemprice).TruncateDecimalPlaces(2)) > (CompanyBuyPrice).TruncateDecimalPlaces(2))
                    {
                        try
                        {
                            responsetype = "GeneralError";
                            ResponseData = "#Price Changed " + "#Main Price = " + CompanyBuyPrice + " #New Price = " + itemprice + " #Response= " + ResponseData;
                            // int AgentId = Convert.ToInt32(Common.getSession("UserId") == "" ? "0" : Common.getSession("UserId"));
                            string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, PriceCheckRequest, ResponseData, Error);
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
                    if (Convert.ToDouble((itemprice).TruncateDecimalPlaces(0)) > (CompanyBuyPrice).TruncateDecimalPlaces(0))
                    {
                        try
                        {
                            responsetype = "GeneralError";
                            ResponseData = "#Price Changed " + "#Main Price = " + CompanyBuyPrice + " #New Price = " + itemprice + " #Response= " + ResponseData;
                            //int AgentId = Convert.ToInt32(Common.getSession("UserId") == "" ? "0" : Common.getSession("UserId"));
                            string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, PriceCheckRequest, ResponseData, Error);
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
            catch (Exception ex)
            {
                Error = ex.Message.ToString() + "#=" + ex.StackTrace.ToString();
                responsetype = "GeneralError";
                _ = _exception.Error(ex, Searchkey);
                IsError = true;
                priceCheckCMDto.IsError = IsError;

            }

            try
            {
                responsetype = "Success";
                //  int AgentId = Convert.ToInt32(Common.getSession("UserId") == "" ? "0" : Common.getSession("UserId"));
                string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, PriceCheckRequest, ResponseData, Error);
            }
            catch (Exception e)
            {
                _ = _exception.Error(e, Searchkey);
            }

            priceCheckCMDto.Flag = false;
            priceCheckCMDto.ItemPrice = itemprice;
            return priceCheckCMDto;
        }

        public string HotelDoPriceCheckRequest_v2(string RateKey, int k, string AffiliateId, string Password, string RoomCode, DataSet dsCartDetails, string xmlCity, string xmlCountry, /*string Contract, string MarketId, string InterfaceId,*/ string MealCode, string CheckInDate, string CheckOutDate, string HotelCode, string XmlNationality)
        {
            int RoomNo = 0, NoofAdults = 0, Noofchilds = 0, ChildAge1 = 0, ChildAge2 = 0;
            string PriceCheckReq = "";
            try
            {
                StringBuilder PriceCheck = new StringBuilder();
                PriceCheck.Append("?a=" + AffiliateId + "");
                PriceCheck.Append("&co=" + XmlNationality + "");
                PriceCheck.Append("&c=" + HotelDoStaticInfo.HotelDoCurrencySupplier + "");
                PriceCheck.Append("&l=ING");
                PriceCheck.Append("&rk=" + RateKey + "");
                PriceCheck.Append("&sd=" + CheckInDate + "");
                PriceCheck.Append("&ed=" + CheckOutDate + "");
                PriceCheck.Append("&h=" + HotelCode + "");
                // PriceCheck.Append("&ci=" + Contract + "");
                //PriceCheck.Append("&mi=" + MarketId + "");
                //  PriceCheck.Append("&it=" + InterfaceId + "");
                PriceCheck.Append("&ri=" + RoomCode + "");
                PriceCheck.Append("&mp=" + MealCode + "");
                RoomNo = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[k]["RoomNumber"]);
                NoofAdults = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[k]["NoOfAdults"]);
                Noofchilds = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[k]["NoOfChild"]);
                PriceCheck.Append("&r1a=" + NoofAdults + "");
                PriceCheck.Append("&r1k=" + Noofchilds + "");
                if (Noofchilds == 1)
                {
                    ChildAge1 = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[k]["ChildAge1"]);
                    PriceCheck.Append("&r1k1a=" + ChildAge1 + "");
                }
                else if (Noofchilds == 2)
                {
                    ChildAge1 = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[k]["ChildAge1"]);
                    PriceCheck.Append("&r1k1a=" + ChildAge1 + "");
                    ChildAge2 = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[k]["ChildAge2"]);
                    PriceCheck.Append("&r1k2a=" + ChildAge2 + "");
                }
                PriceCheckReq = PriceCheck.ToString();
            }
            catch (Exception ex)
            {
                _ = _exception.ErrorNew(ex, _LogAPI.Prebook);
            }
            return PriceCheckReq;
        }

        public string HotelDoBookingRequest_v2(string PassWord, string AffiliateId, string HotelCode, DataTable dtPassengers, DataSet dsCartDetails, string UniqueNo, bool IsPriceUpdates, bool IsErrors, int i = 0)
        {
            try
            {
                string[] MBArray1 = Convert.ToString(dsCartDetails.Tables[1].DefaultView[0]["RoomTypeId"]).Split(new String[] { "$$$" }, StringSplitOptions.None);
                string RateKey = MBArray1[1];
                string RoomCode = MBArray1[2];
                //string Contract = MBArray1[3];
                //string MarketId = MBArray1[4];
                //string InterfaceId = MBArray1[5];
                string Nationality = MBArray1[6];
                string Currency = MBArray1[4];
                string MealCode = Convert.ToString(dsCartDetails.Tables[1].DefaultView[0]["MealPlanCode"]);

                string Country = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["XMLCountryId"]);
                double Total = Math.Round(Convert.ToDouble(dsCartDetails.Tables[1].DefaultView[0]["CompanyBuyingPrice"]), 2);

                StringBuilder Rooms = new StringBuilder();
                for (int j = 0; j < dsCartDetails.Tables[1].DefaultView.Count; j++)
                {
                    Rooms.Append("<room>");
                    dtPassengers.DefaultView.RowFilter = "PickUp='" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomTypeId"]) + "' and UserCartID='" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["UserCartID"]) + "' and RoomNo='" + Convert.ToInt32(j + 1) + "'";
                    DataTable dtnewpassanger = dtPassengers.DefaultView.ToTable();
                    double RoomTotal = 0;
                    RoomTotal = Math.Round(Convert.ToDouble(Convert.ToDouble(dsCartDetails.Tables[1].DefaultView[0]["Rate"]) / Convert.ToInt32(dsCartDetails.Tables[1].DefaultView.Count)), 2);

                    for (int k = 0; k < dtnewpassanger.DefaultView.Count; k++)
                    {
                        if (j >= 1)
                        {
                            Rooms.Append("<name>" + Convert.ToString(dtnewpassanger.Rows[k]["FirstName"]) + "</name>");
                            Rooms.Append("<lastname>" + Convert.ToString(dtnewpassanger.Rows[k]["LastName"]) + "</lastname>");
                            break;
                        }
                        if (Convert.ToString(dtnewpassanger.Rows[k]["IsLead"]) == "1")
                        {
                            Rooms.Append("<name>" + Convert.ToString(dtnewpassanger.Rows[k]["FirstName"]) + "</name>");
                            Rooms.Append("<lastname>" + Convert.ToString(dtnewpassanger.Rows[k]["LastName"]) + "</lastname>");
                        }

                    }
                    Rooms.Append("<adults>" + Convert.ToString(dsCartDetails.Tables[1].Rows[j]["NoOfAdults"]) + "</adults>");
                    Rooms.Append("<kids>" + Convert.ToString(dsCartDetails.Tables[1].Rows[j]["NoOfChild"]) + "</kids>");
                    if (Convert.ToString(dsCartDetails.Tables[1].Rows[j]["NoOfChild"]) == "1")
                        Rooms.Append("<k1a>" + Convert.ToString(dsCartDetails.Tables[1].Rows[j]["ChildAge1"]) + "</k1a>");
                    else if (Convert.ToString(dsCartDetails.Tables[1].Rows[j]["NoOfChild"]) == "2")
                    {
                        Rooms.Append("<k1a>" + Convert.ToString(dsCartDetails.Tables[1].Rows[j]["ChildAge1"]) + "</k1a>");
                        Rooms.Append("<k2a>" + Convert.ToString(dsCartDetails.Tables[1].Rows[j]["ChildAge2"]) + "</k2a>");
                    }
                    Rooms.Append("<amount>" + RoomTotal + "</amount>");
                    //Rooms.Append("<status>" + Status + "</status>");
                    Rooms.Append("<ratekey>" + RateKey + "</ratekey>");
                    Rooms.Append("</room>");
                }
                StringBuilder HotelDo = new StringBuilder();
                if (IsPriceUpdates == false && IsErrors == false)
                {
                    HotelDo.Append("<Request Type=\"Reservation\" Version=\"2.0\">");
                    HotelDo.Append("<affiliateid>" + AffiliateId + "</affiliateid>");
                    HotelDo.Append("<language>ING</language>");
                    HotelDo.Append("<currency>" + Currency + "</currency>");

                    for (int j = 0; j < dsCartDetails.Tables[1].DefaultView.Count; j++)
                    {
                        dtPassengers.DefaultView.RowFilter = "PickUp='" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomTypeId"]) + "' and UserCartID='" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["UserCartID"]) + "' and RoomNo='" + Convert.ToInt32(j + 1) + "'";
                        DataTable dtnewpassanger = dtPassengers.DefaultView.ToTable();
                        for (int k = 0; k < dtnewpassanger.DefaultView.Count; k++)
                        {
                            if (Convert.ToString(dtnewpassanger.Rows[k]["IsLead"]) == "1")
                            {
                                HotelDo.Append("<firstname>" + Convert.ToString(dtPassengers.Rows[0]["FirstName"]) + "</firstname>");
                                HotelDo.Append("<lastname>" + Convert.ToString(dtPassengers.Rows[0]["LastName"]) + "</lastname>");
                                HotelDo.Append("<emailaddress>" + Convert.ToString("suppliers@withinearth.com") + "</emailaddress>"); //as per Ahmed #10-Dec-2024 Change Email Address
                            }
                        }
                    }
                    HotelDo.Append("<clientcountry>" + Nationality + "</clientcountry>");
                    HotelDo.Append("<country>" + Nationality + "</country>");
                    HotelDo.Append("<total>" + Total + "</total>");
                    HotelDo.Append("<phones>");
                    HotelDo.Append("<phone>");
                    HotelDo.Append("<type>2</type>");
                    HotelDo.Append("<number>9905674838</number>");
                    HotelDo.Append("</phone>");
                    HotelDo.Append("</phones>");
                    HotelDo.Append("<hotels>");
                    HotelDo.Append("<hotel>");
                    HotelDo.Append("<hotelid>" + HotelCode + "</hotelid>");
                    HotelDo.Append("<roomtype>" + RoomCode + "</roomtype>");
                    HotelDo.Append("<mealplan>" + MealCode + "</mealplan>");
                    HotelDo.Append("<datearrival>" + Convert.ToString(dsCartDetails.Tables[0].Rows[i]["CheckInDate"]) + "</datearrival>");
                    HotelDo.Append("<datedeparture>" + Convert.ToString(dsCartDetails.Tables[0].Rows[i]["CheckOutDate"]) + "</datedeparture>");
                    //HotelDo.Append("<marketid>" + MarketId + "</marketid>");
                    //HotelDo.Append("<contractid>" + Contract + "</contractid>");
                    HotelDo.Append("<dutypercent>" + 0 + "</dutypercent>");
                    HotelDo.Append("<rooms>");
                    HotelDo.Append(Rooms.ToString());
                    HotelDo.Append("</rooms>");
                    HotelDo.Append("</hotel>");
                    HotelDo.Append("</hotels>");
                    HotelDo.Append("<payments>");
                    HotelDo.Append("<agencycreditpayment>");
                    HotelDo.Append("<type>CREDUY</type>");
                    HotelDo.Append("<currency>" + Currency + "</currency>");
                    HotelDo.Append("<amount>" + Total + "</amount>");
                    HotelDo.Append("</agencycreditpayment>");
                    HotelDo.Append("</payments>");
                    HotelDo.Append("</Request>");
                    string HotelDoReq = HotelDo.ToString();
                    return HotelDoReq;
                }
                else
                    return "";
            }
            catch (Exception ex)
            {
                _exception.ErrorNew(ex, _LogAPI.Book);
                return "";
            }
        }

        public string HotelDoBookingCancelRequest(string PassWord, string AffiliateId, string BookingId, string ConsecutiveId)
        {
            try
            {
                StringBuilder cancel = new StringBuilder();
                cancel.Append("?a=" + AffiliateId + "");
                cancel.Append("&ip=103.19.248.44");
                cancel.Append("&l=ING");
                cancel.Append("&c=US");
                cancel.Append("&bn=" + BookingId + "");
                cancel.Append("&bc=" + ConsecutiveId + "");
                string CancelReq = cancel.ToString();
                return CancelReq;
            }
            catch (Exception ex)
            {
                _exception.ErrorNew(ex, _LogAPI.CancelBooking);

                return "";
            }
        }
    }
}
