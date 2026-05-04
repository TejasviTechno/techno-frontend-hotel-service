using System;
using System.Collections.Concurrent;
using System.Data;
using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using XConnect.CommonUtilities;
using XConnect.CommonUtilities.Interface;
using XConnect.ErrorLogHandler.Interface;
using XConnect.Expedia;
using XConnect.Markup.Interface;
using XConnect.Models;
using XConnect.Models.Hotel;
using XConnect.Supplier.Iboosy;
using XConnect.Supplier.Iboosy.Interface;
using static Azure.Core.HttpHeader;
using static XConnect.Models.ErrorLog;
using static XConnect.Supplier.Iboosy.iBoosyClass;
using Options = XConnect.Models.Options;

//using static System.Runtime.InteropServices.JavaScript.JSType;

namespace XConnect.Supplier
{
    public class iBoosyRepository : IiBoosy
    {
        private readonly ICommonMethod _commonMethod;
        private readonly IException _exception;
        private readonly IMarkup _markup;
        private readonly ISupplierRes _supplierRes;
        private readonly KeySettings _keySettings;
        private readonly SupplierConfiguration _supplierConfig;


        public iBoosyRepository(ICommonMethod commonMethod, IException exception, IMarkup markup, ISupplierRes supplierRes, IOptions<KeySettings> keySettings, IOptions<SupplierConfiguration> supplierConfig)
        {
            _commonMethod = commonMethod;
            _exception = exception;
            _markup = markup;
            _supplierRes = supplierRes;
            _keySettings = keySettings.Value;
             _supplierConfig = supplierConfig.Value;
        }

        public void GetMethaBookRequestOnly_async_old(DataRow[] commonCode, List<StarRating> listRating, DataTable dtROE, HotelSearchParameter SearchPara, List<SupplierDetails> MethaBookDetails, List<SupplierSearchConfig> MethaBookSearchConfig, DataTable dtPreferredDataHotel, ref ConcurrentBag<HotelX> AllTask, string XMLCity, string XmlNationality, int Hit, TokenData tokenData, string Provider)
        {
            try
            {
                string Ids = string.Empty;
                string xmlbase = "";
                _commonMethod.GetPrefferedHotelCodes(ref Ids, ref xmlbase, ref MethaBookSearchConfig, Provider, dtPreferredDataHotel, SearchPara.IsSearchFor);
                #region "MethaBook"
                if (MethaBookSearchConfig != null)
                {

                    string Supplier = Convert.ToString(MethaBookDetails[0].SupplierName);

                    double CurrencyFactorMethaBook = 1;

                    string MethaBookCurrency = Convert.ToString(MethaBookDetails[0].CurrencyShortName);
                    string Sitecurrency = tokenData.SiteCurrency;

                    //if (MethaBookCurrency.ToLower() != Convert.ToString("usd").ToLower())
                    //{
                    //    // DataTable dtInrCurrency = Common.GetCountryCurrency(MethaBookCurrency);
                    //    CurrencyFactorMethaBook = _commonMethod.GetROEByCurrency(MethaBookCurrency, dtROE);
                    //}
                    if (MethaBookCurrency.ToLower() != Sitecurrency.ToLower()) // change systembase currency 
                    {
                        CurrencyFactorMethaBook = _commonMethod.GetROEByCurrency(MethaBookCurrency, dtROE);
                    }
                    for (int i = 0; i < MethaBookSearchConfig.Count; i++)
                    {
                        bool DebugMode = (Convert.ToInt32(MethaBookSearchConfig[i].DebugMode) == 0 ? false : true);
                        string WebsiteUrl = Convert.ToString(MethaBookDetails[0].WebSiteURL);
                        string UserId = Convert.ToString(MethaBookDetails[0].UserID);
                        string UserName = Convert.ToString(MethaBookDetails[0].UserName);
                        string PassWord = Convert.ToString(MethaBookDetails[0].Password);
                        // List<HotelCodeByCity> DtHotelDAta = FilterFinalList(commonCode, listRating);
                        if (commonCode.Length > 0)
                        {
                            if (Convert.ToInt32(MethaBookSearchConfig[i].CityID) == 0 || Convert.ToInt32(MethaBookSearchConfig[i].CityID) == SearchPara.CityID)//All City means 0
                            {
                                //  if (DtHotelDAta.Count > 0)
                                {
                                    int Noofrecords = Convert.ToInt32(MethaBookSearchConfig[i].Noofrecords);
                                    DataTable[] splittedtables = commonCode.AsEnumerable()
                                                          .Select((row, index) => new { row, index })
                                                          .GroupBy(x => x.index / Noofrecords)  // integer division, the fractional part is truncated
                                                          .Select(g => g.Select(x => x.row).CopyToDataTable())
                                                          .ToArray();
                                    for (int a = 0; a < splittedtables.Length; a++)
                                    {

                                        var paramValues1 = String.Join(",", splittedtables[a].Select().Select(x => String.Join(",", Convert.ToString(x.ItemArray[0]))));
                                        string IdsForSearch = Convert.ToString(paramValues1);

                                        HotelX xhotel = new HotelX();
                                        xhotel.Supplier = Supplier;
                                        xhotel.DebugeMode = DebugMode;
                                        xhotel.SearchTimeOut = Convert.ToInt32(MethaBookSearchConfig[i].HotelSearchTimeOut);
                                        //if (Hit > 0)
                                        //{
                                        xhotel.ProcessTimeout = Convert.ToInt32(MethaBookSearchConfig[i].HotelSearchTimeOut2);
                                        //}
                                        xhotel.URL = WebsiteUrl + _supplierConfig.IBoosy.Metha_URL;
                                        xhotel.UserId = UserId;
                                        xhotel.UserName = UserName;
                                        xhotel.PassWord = PassWord;
                                        xhotel.CurrencyCode = MethaBookCurrency;
                                        xhotel.CurrencyFactor = CurrencyFactorMethaBook;
                                        xhotel.XmlCity = XMLCity;
                                        xhotel.XmlCountry = "";
                                        xhotel.MethoodType = XMLHotelBookingCommon.GET;
                                        xhotel.XmlNationality = XmlNationality;
                                        xhotel.AvailableToken = "";
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
                                            objrq.Request = XMLMethaBookRquestOnly_async(WebsiteUrl, UserName, PassWord, xhotel.SearchTimeOut, SearchPara, IdsForSearch, DebugMode, XMLCity, XmlNationality);
                                            objrq.Response = ""; RQRS.Add(objrq);
                                        }
                                        xhotel.RQRS = RQRS;
                                        xhotel.ReturnRooms = 5; // RQRS;
                                        xhotel.TechName = "IBoosy";// RQRS;
                                        //xhotel.RQRS = Convert.ToInt32(IiBoosyInfo.IBoosy_Rate_Plan_Count_10);
                                        #endregion
                                        AllTask.Add(xhotel);
                                    }
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

        public void GetMethaBookRequestOnly_async(CommonSearchList commonSearchList, List<StarRating> listRating, DataTable dtROE, HotelSearchParameter SearchPara, List<SupplierDetails> lstIboosyDetails, List<SupplierSearchConfig> lstIboosySearchConfig, DataTable dtPreferredDataHotel, ref ConcurrentBag<HotelX> AllTask, int Hit, TokenData tokenData, string Provider, DataSet dtXMLCity)
        {
            try
            {
                if (lstIboosyDetails != null && lstIboosyDetails.Count > 0)
                {
                    List<SupplierDetails> IboosyMain = new List<SupplierDetails>();
                    List<SupplierDetails> IboosyChild = new List<SupplierDetails>();

                    foreach (SupplierDetails item in lstIboosyDetails)
                    {
                        if (false)
                            IboosyMain.Add(item);
                        else
                            IboosyChild.Add(item);
                    }

                    for (int x = 0; x < IboosyChild.Count; x++)
                    {
                        int SupplierID = IboosyChild[x].SupplierID;
                        string Supplier = IboosyChild[x].SupplierName;
                        DataRow[] commonCode = null;

                        #region Codes
                        if (Supplier.ToLower() == "methabook")
                            commonCode = commonSearchList.commonHotelCodes.MethaBook_Codes;
                        else if (Supplier.ToLower() == "solole")
                            commonCode = commonSearchList.commonHotelCodes.Solole_Codes;
                        #endregion

                        if (commonCode != null)
                        {
                            if (lstIboosySearchConfig.Count > 0)
                            {
                                List<SupplierSearchConfig> IBoosySearchConfig = lstIboosySearchConfig
                                    .Where(x => x != null && x.SupplierID == SupplierID)
                                    .ToList();

                                if (IBoosySearchConfig != null && IBoosySearchConfig.Count > 0)
                                {
                                    DataForSearch dataForSearch = _commonMethod.GetXmlDataForSearchNew(SearchPara.Nationality, dtXMLCity, Supplier);

                                    if (dataForSearch.XmlNationality != "")
                                    {
                                        string Ids = string.Empty;
                                        string xmlbase = "";
                                        _commonMethod.GetPrefferedHotelCodes(ref Ids, ref xmlbase, ref IBoosySearchConfig, Provider, dtPreferredDataHotel, SearchPara.IsSearchFor);
                                        #region "MethaBook"
                                        if (IBoosySearchConfig != null)
                                        {
                                            double CurrencyFactorMethaBook = 1;

                                            string MethaBookCurrency = Convert.ToString(IboosyChild[x].CurrencyShortName);
                                            if (MethaBookCurrency.ToLower() != tokenData.SiteCurrency.ToLower()) // change systembase currency 
                                            {
                                                CurrencyFactorMethaBook = _commonMethod.GetROEByCurrency(MethaBookCurrency, dtROE);
                                            }
                                            for (int i = 0; i < IBoosySearchConfig.Count; i++)
                                            {
                                                bool DebugMode = (Convert.ToInt32(IBoosySearchConfig[i].DebugMode) == 0 ? false : true);
                                                string WebsiteUrl = Convert.ToString(IboosyChild[x].WebSiteURL);
                                                string UserId = Convert.ToString(IboosyChild[x].UserID);
                                                string UserName = Convert.ToString(IboosyChild[x].UserName);
                                                string PassWord = Convert.ToString(IboosyChild[x].Password);
                                                // List<HotelCodeByCity> DtHotelDAta = FilterFinalList(commonCode, listRating);
                                                if (commonCode.Length > 0)
                                                {
                                                    if (Convert.ToInt32(IBoosySearchConfig[i].CityID) == 0 || Convert.ToInt32(IBoosySearchConfig[i].CityID) == SearchPara.CityID)//All City means 0
                                                    {
                                                        //  if (DtHotelDAta.Count > 0)
                                                        {
                                                            int Noofrecords = Convert.ToInt32(IBoosySearchConfig[i].Noofrecords);
                                                            DataTable[] splittedtables = commonCode.AsEnumerable()
                                                                                  .Select((row, index) => new { row, index })
                                                                                  .GroupBy(x => x.index / Noofrecords)  // integer division, the fractional part is truncated
                                                                                  .Select(g => g.Select(x => x.row).CopyToDataTable())
                                                                                  .ToArray();
                                                            for (int a = 0; a < splittedtables.Length; a++)
                                                            {

                                                                var paramValues1 = String.Join(",", splittedtables[a].Select().Select(x => String.Join(",", Convert.ToString(x.ItemArray[0]))));
                                                                string IdsForSearch = Convert.ToString(paramValues1);

                                                                HotelX xhotel = new HotelX();
                                                                xhotel.Supplier = Supplier;
                                                                xhotel.DebugeMode = DebugMode;
                                                                xhotel.SearchTimeOut = Convert.ToInt32(IBoosySearchConfig[i].HotelSearchTimeOut);
                                                                //if (Hit > 0)
                                                                //{
                                                                xhotel.ProcessTimeout = Convert.ToInt32(IBoosySearchConfig[i].HotelSearchTimeOut2);
                                                                //}
                                                                xhotel.URL = WebsiteUrl + _supplierConfig.IBoosy.Metha_URL;
                                                                xhotel.UserId = UserId;
                                                                xhotel.UserName = UserName;
                                                                xhotel.PassWord = PassWord;
                                                                xhotel.CurrencyCode = MethaBookCurrency;
                                                                xhotel.CurrencyFactor = CurrencyFactorMethaBook;
                                                                xhotel.XmlCity = dataForSearch.xmlCity;
                                                                xhotel.XmlCountry = "";
                                                                xhotel.MethoodType = XMLHotelBookingCommon.GET;
                                                                xhotel.XmlNationality = dataForSearch.XmlNationality;
                                                                xhotel.AvailableToken = "";
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
                                                                    objrq.Request = XMLMethaBookRquestOnly_async(WebsiteUrl, UserName, PassWord, xhotel.SearchTimeOut, SearchPara, IdsForSearch, DebugMode, dataForSearch.xmlCity, dataForSearch.XmlNationality);
                                                                    objrq.Response = ""; RQRS.Add(objrq);
                                                                }
                                                                xhotel.RQRS = RQRS;
                                                                xhotel.ReturnRooms = 5; // RQRS;
                                                                xhotel.TechName = "IBoosy";// RQRS;
                                                                                           //xhotel.RQRS = Convert.ToInt32(IiBoosyInfo.IBoosy_Rate_Plan_Count_10);
                                                                #endregion
                                                                AllTask.Add(xhotel);
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                        #endregion
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

        public void GetMethaBookRequestOnly_async(DataRow[] commonCode, List<StarRating> listRating, DataTable dtROE, HotelSearchParameter SearchPara, List<SupplierDetails> lstIboosyDetails, List<SupplierSearchConfig> lstIboosySearchConfig, DataTable dtPreferredDataHotel, ref ConcurrentBag<HotelX> AllTask, int Hit, TokenData tokenData, string Provider, DataSet dtXMLCity)
        {
            try
            {
                if (lstIboosyDetails != null && lstIboosyDetails.Count > 0)
                {
                    List<SupplierDetails> IboosyMain = new List<SupplierDetails>();
                    List<SupplierDetails> IboosyChild = new List<SupplierDetails>();
                    List<StarRating> lstRating = new List<StarRating>();
                    lstRating = _commonMethod.GetStarRatingStringList_xtg(SearchPara);

                    List<HotelCodeByCity> DtHotelDAta = HtmlExtensions.ToList<HotelCodeByCity>(commonCode.CopyToDataTable());
                    int ratingcount = lstRating.Count;
                    int hotelcount = DtHotelDAta.Count;

                    foreach (SupplierDetails item in lstIboosyDetails)
                    {
                        if (false)
                            IboosyMain.Add(item);
                        else
                            IboosyChild.Add(item);
                    }

                    for (int x = 0; x < IboosyChild.Count; x++)
                    {
                        int SupplierID = IboosyChild[x].SupplierID;
                        string Supplier = IboosyChild[x].SupplierName;


                        // if (commonCode != null)
                        {
                            if (lstIboosySearchConfig.Count > 0)
                            {
                                List<SupplierSearchConfig> IBoosySearchConfig = lstIboosySearchConfig
                                    .Where(x => x != null && x.SupplierID == SupplierID)
                                    .ToList();

                                if (IBoosySearchConfig != null && IBoosySearchConfig.Count > 0)
                                {
                                    //DataForSearch dataForSearch = _commonMethod.GetXmlDataForSearchNew(SearchPara.Nationality, dtXMLCity, Supplier);
                                    DataForSearch dataForSearch = _commonMethod.GetXmlDataForSearchTechwise(SearchPara.Nationality, dtXMLCity, Supplier, "iboosy");

                                    if (dataForSearch.XmlNationality != "")
                                    {
                                        string Ids = string.Empty;
                                        string xmlbase = "";
                                        _commonMethod.GetPrefferedHotelCodes(ref Ids, ref xmlbase, ref IBoosySearchConfig, Provider, dtPreferredDataHotel, SearchPara.IsSearchFor);

                                        List<HotelCodeByCity> filter = new List<HotelCodeByCity>();
                                        filter = _commonMethod.FilterWithOutCity_Code(ratingcount, hotelcount, DtHotelDAta, lstRating, Supplier);

                                        if (IBoosySearchConfig != null)
                                        {
                                            double CurrencyFactorMethaBook = 1;

                                            string MethaBookCurrency = Convert.ToString(IboosyChild[x].CurrencyShortName);
                                            if (MethaBookCurrency.ToLower() != tokenData.SiteCurrency.ToLower()) // change systembase currency 
                                            {
                                                CurrencyFactorMethaBook = _commonMethod.GetROEByCurrency(MethaBookCurrency, dtROE);
                                            }
                                            for (int i = 0; i < IBoosySearchConfig.Count; i++)
                                            {
                                                bool DebugMode = (Convert.ToInt32(IBoosySearchConfig[i].DebugMode) == 0 ? false : true);
                                                //string WebsiteUrl = Convert.ToString(IboosyChild[x].WebSiteURL);

                                                string WebsiteUrl = _keySettings.SupplierProxy == "0" ? Convert.ToString(IboosyChild[x].WebSiteURL) : Convert.ToString(IboosyChild[x].WebSiteURLforAPI);


                                                string UserId = Convert.ToString(IboosyChild[x].UserID);
                                                string UserName = Convert.ToString(IboosyChild[x].UserName);
                                                string PassWord = Convert.ToString(IboosyChild[x].Password);
                                                // List<HotelCodeByCity> DtHotelDAta = FilterFinalList(commonCode, listRating);
                                                if (filter.Count > 0)
                                                {
                                                    if (Convert.ToInt32(IBoosySearchConfig[i].CityID) == 0 || Convert.ToInt32(IBoosySearchConfig[i].CityID) == SearchPara.CityID)//All City means 0
                                                    {
                                                        // if (DtHotelDAta.Count > 0)
                                                        {
                                                            int NoOfRecord = Convert.ToInt32(IBoosySearchConfig[i].Noofrecords);
                                                            double count = Convert.ToDouble(filter.Count);
                                                            int NoOfDiv = Convert.ToInt32(Math.Ceiling(count / Convert.ToDouble(NoOfRecord)));
                                                            List<List<XMLID_Search>> lstxml = _commonMethod.GetXMLIDString(NoOfDiv, NoOfRecord, count, filter);
                                                            for (int a = 0; a < NoOfDiv; a++)
                                                            {
                                                                //var paramValues1 = String.Join(",", splittedtables[a].Select().Select(x => String.Join(",", Convert.ToString(x.ItemArray[0]))));
                                                                //string IdsForSearch = Convert.ToString(paramValues1);

                                                                string IdsForSearch = Convert.ToString(_commonMethod.GetHotelCodeByCOmma(lstxml[a]));

                                                                HotelX xhotel = new HotelX();
                                                                xhotel.Supplier = Supplier;
                                                                xhotel.DebugeMode = DebugMode;
                                                                xhotel.SearchTimeOut = Convert.ToInt32(IBoosySearchConfig[i].HotelSearchTimeOut);
                                                                //if (Hit > 0)
                                                                //{
                                                                xhotel.ProcessTimeout = Convert.ToInt32(IBoosySearchConfig[i].HotelSearchTimeOut2);
                                                                //}
                                                                xhotel.URL = WebsiteUrl + _supplierConfig.IBoosy.Metha_URL;
                                                                xhotel.UserId = UserId;
                                                                xhotel.UserName = UserName;
                                                                xhotel.PassWord = PassWord;
                                                                xhotel.CurrencyCode = MethaBookCurrency;
                                                                xhotel.CurrencyFactor = CurrencyFactorMethaBook;
                                                                xhotel.XmlCity = dataForSearch.xmlCity;
                                                                xhotel.XmlCountry = "";
                                                                xhotel.MethoodType = XMLHotelBookingCommon.GET;
                                                                xhotel.XmlNationality = dataForSearch.XmlNationality;
                                                                xhotel.AvailableToken = "";
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
                                                                    objrq.Request = XMLMethaBookRquestOnly_async(WebsiteUrl, UserName, PassWord, xhotel.SearchTimeOut, SearchPara, IdsForSearch, DebugMode, dataForSearch.xmlCity, dataForSearch.XmlNationality);
                                                                    objrq.Response = ""; RQRS.Add(objrq);
                                                                }
                                                                xhotel.RQRS = RQRS;
                                                                xhotel.ReturnRooms = IboosyChild[x].ReturnRooms; // RQRS;
                                                                xhotel.TechName = IboosyChild[x].TechName;// RQRS;
                                                                                                          //xhotel.RQRS = Convert.ToInt32(IiBoosyInfo.IBoosy_Rate_Plan_Count_10);
                                                                #endregion
                                                                AllTask.Add(xhotel);
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
        public string XMLMethaBookRquestOnly_async(string Url, string UserName, string PassWord, Int32 SearchTimeOut, HotelSearchParameter SearchParam, string HotelIds, bool DebugMode, string CityCode, string XmlNationality)
        {
            string MethaBookPriceOnly = "";
            try
            {
                bool IsAdult = false;
                bool IsForRoom = false;
                string AvailabilityCode = "";
                for (int m = 0; m < SearchParam.Rooms.Count; m++)
                {
                    if (IsAdult)
                        continue;

                    if (SearchParam.Rooms[m].NoofAdults > 9)
                        IsAdult = true;
                }
                if (!IsAdult)
                {
                    #region PriceOnlyREQ
                    MethaBookPriceOnly = Get_MethaBook_PriceOnly(SearchParam, CityCode, HotelIds, XmlNationality, IsForRoom, AvailabilityCode);
                    #endregion
                }
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
                return "";
            }
            return MethaBookPriceOnly;
        }
        public string Get_MethaBook_PriceOnly(HotelSearchParameter SearchParam, string CityCode, string HotelIds, string XmlNationality, bool IsForRoom, string AvailabilityCode)
        {
            string MethaBookReq = "";
            try
            {
                StringBuilder MethaBook = new StringBuilder();
                string CheckIn = Convert.ToString(SearchParam.CheckInDate);
                string CheckOut = Convert.ToString(SearchParam.CheckOutDate);
                CheckIn = CheckIn.Substring(6, 4) + "-" + CheckIn.Substring(3, 2) + "-" + CheckIn.Substring(0, 2);
                CheckOut = CheckOut.Substring(6, 4) + "-" + CheckOut.Substring(3, 2) + "-" + CheckOut.Substring(0, 2);

                MethaBook.Append("?language=en");
                MethaBook.Append("&nationality=" + XmlNationality + "");
                MethaBook.Append("&includeOnRequest=false");
                MethaBook.Append("&checkInDate=" + CheckIn + "");
                MethaBook.Append("&checkOutDate=" + CheckOut + "");
                for (int i = 0; i < SearchParam.Rooms.Count; i++)
                {
                    if (i >= 1)
                        MethaBook.Append("&room" + SearchParam.Rooms[i].RoomNo + "=");
                    else
                        MethaBook.Append("&room" + SearchParam.Rooms[i].RoomNo + "=");
                    for (int j = 0; j < SearchParam.Rooms[i].NoofAdults; j++)
                    {
                        if (j >= 1)
                            MethaBook.Append(",30");
                        else
                            MethaBook.Append("30");
                    }
                    int Child = Convert.ToInt32(SearchParam.Rooms[i].NoofChild);
                    if (Child > 0)
                    {
                        switch (Child)
                        {
                            case 1:
                                MethaBook.Append("," + SearchParam.Rooms[i].Child1Age + "");
                                break;
                            case 2:
                                MethaBook.Append("," + SearchParam.Rooms[i].Child1Age + "");
                                MethaBook.Append("," + SearchParam.Rooms[i].Child2Age + "");
                                break;
                            default:
                                break;
                        }
                    }
                }
                MethaBook.Append("&accommodationCodes=" + HotelIds + "");
                if (IsForRoom == true)
                    MethaBook.Append("&availabilityCode=" + AvailabilityCode + "");

                MethaBook.Append("&showCancellationPolicies=true");
                MethaBookReq = MethaBook.ToString();
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
            }
            return MethaBookReq;
        }
        public async Task<List<Options>> Methabook_HotelSearchWithRooms(HotelX Item, CommonSearch commonSearch, DataTable dtROE,string Currency, string ResponseData, string CityCode, HotelSearchParameter SearchParam, double CurrencyFactorMethaBook, string ProccessCutOffTime, Int32 processTimeout, string ResProccessTimerLogActive, string ResProccessTimeValueforSaveLog, string Suppliername, string BookingStatus, string NoCount, TokenData tokenData, string RS_Success)
        {
            System.Diagnostics.Stopwatch timer_room_Methabook = new System.Diagnostics.Stopwatch();
            timer_room_Methabook.Start();

            List<Options> options = new List<Options>();

            try
            {
                DataTable DtHotel = _commonMethod.GenerateDataTableNewV2();
                if (ResponseData == null || ResponseData == "" || ResponseData == string.Empty)
                {
                    Item.ResponseStatus = 204;
                    Item.ResponseType = NoCount;
                    return options;
                }

                iBoosyClass.MethaBook.RootObject MethaBookResponse = JsonSerializer.Deserialize<iBoosyClass.MethaBook.RootObject>(ResponseData);


                if (MethaBookResponse == null)
                {
                    Item.ResponseStatus = 204;
                    Item.ResponseType = NoCount;
                    return options;
                }
                #region "HotelList"
                if (MethaBookResponse.availabilityRs == null || MethaBookResponse.availabilityRs.accommodations == null)
                {
                    Item.ResponseStatus = 404;
                    Item.ResponseType = NoCount;
                    return options;
                }
                if (MethaBookResponse.availabilityRs.accommodations.Count > 0)
                {
                    for (int hh = 0; hh < MethaBookResponse.availabilityRs.accommodations.Count; hh++)
                    {
                        Item.ResponseType = RS_Success;
                        //Check Time If High then return NoCount
                        if (ProccessCutOffTime == "1" && timer_room_Methabook.ElapsedMilliseconds > processTimeout)
                        {
                            timer_room_Methabook.Stop();
                            break;
                        }
                        //--end
                        double FinalMinPrice = 0;

                        #region "When not Need Room"  
                        // add hotel code from price only and room code blank here
                        if (SearchParam.Filters.IsShowRooms == 0)
                        {
                            #region "Hotel Code Block "

                            iBoosyClass.MethaBook.Accommodation drhotel = MethaBookResponse.availabilityRs.accommodations[hh];
                            string currencycode = "", AvailabilityCode = "";
                            double Price = 0;
                            //animesh changes code  21 feb 2019 for price 2 comments
                            if (commonSearch.tokenData.AgentId != 1899)
                            {
                                List<iBoosyClass.RoomData> lstprice = new List<iBoosyClass.RoomData>();
                                for (int j = 0; j < drhotel.availabilities.Count; j++)
                                {
                                    iBoosyClass.MethaBook.Availability dravailability = drhotel.availabilities[j];
                                    if (j == 0)
                                    {
                                        AvailabilityCode = Convert.ToString(dravailability.code);
                                    }
                                    Price = Convert.ToDouble(dravailability.price.net);
                                    currencycode = Convert.ToString(dravailability.price.currencyCode);
                                    string Sitecurrency = tokenData.SiteCurrency;

                                    if (currencycode.ToLower() != Sitecurrency.ToLower())
                                    {
                                        continue;
                                    }
                                    iBoosyClass.RoomData data = new iBoosyClass.RoomData();
                                    data.FinalSellingPrice = Price;
                                    lstprice.Add(data);
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
                            drXmlHotel["XMLHotelCode"] = MethaBookResponse.availabilityRs.accommodations[hh].code;
                            drXmlHotel["HotelName"] = "";
                            drXmlHotel["EssentialInformation"] = "";
                            drXmlHotel["Description"] = "";
                            drXmlHotel["IsRecomondedHotel"] = false;
                            drXmlHotel["SupplierName"] = Suppliername;
                            drXmlHotel["OfficeID"] = 0;
                            drXmlHotel["SupplierCurrency"] = Currency;
                            drXmlHotel["XMLCountry"] = "";
                            drXmlHotel["XMLCity"] = (CityCode);
                            drXmlHotel["XmlHotelCity"] = (CityCode);
                            drXmlHotel["ContractID"] = 0;
                            drXmlHotel["Rating"] = 0;
                            drXmlHotel["Reviews"] = 0;
                            drXmlHotel["UniqueNo"] = MethaBookResponse.availabilityRs.accommodations[hh].code;
                            drXmlHotel["IsShowSupplier"] = false;
                            drXmlHotel["AvailToken"] = MethaBookResponse.availabilityRs.accommodations[hh].availabilities[0].code;
                            drXmlHotel["BookingStatus"] = BookingStatus;
                            drXmlHotel["FinalSellingPrice"] = FinalMinPrice;
                            drXmlHotel["SellingPrice"] = FinalMinPrice;
                            ////  if (FinalMinPrice > 0)
                            DtHotel.Rows.Add(drXmlHotel);
                            #endregion


                            if (FinalMinPrice > 0)
                            {
                                #region "Room code Block"
                                bool willAddOrNot = true;
                                Int64 RaynaID = 0;
                                Int64 GiataId = 0;
                                int Rating = 0;
                                Int64 OwnsystemHotelId = 0;
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
                                    //Options ops = (await _supplierRes.GetCommonRoomForSuppliers(GiataId, commonSearch.tokenData, commonSearch.advancedOptions, Convert.ToDouble(LowestPrice), drXmlHotel, dt_HotelRoom, dt_Cancellation, dt_RateBreackUp, SearchParam));
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
                            FinalMinPrice = 1;
                            #region Hotels

                            #region "Hotel Block here"
                            DataRow drXmlHotel = DtHotel.NewRow();
                            drXmlHotel["XMLHotelCode"] = MethaBookResponse.availabilityRs.accommodations[hh].code;
                            drXmlHotel["HotelName"] = "";
                            drXmlHotel["EssentialInformation"] = "";
                            drXmlHotel["Description"] = "";
                            drXmlHotel["IsRecomondedHotel"] = false;
                            drXmlHotel["SupplierName"] = Suppliername;
                            drXmlHotel["OfficeID"] = 0;
                            drXmlHotel["SupplierCurrency"] = Currency;
                            drXmlHotel["XMLCountry"] = "";
                            drXmlHotel["XMLCity"] = (CityCode);
                            drXmlHotel["XmlHotelCity"] = (CityCode);
                            drXmlHotel["ContractID"] = 0;
                            drXmlHotel["Rating"] = 0;
                            drXmlHotel["Reviews"] = 0;
                            drXmlHotel["UniqueNo"] = MethaBookResponse.availabilityRs.accommodations[hh].code;
                            drXmlHotel["IsShowSupplier"] = false;
                            drXmlHotel["AvailToken"] = MethaBookResponse.availabilityRs.accommodations[hh].availabilities[0].code;
                            drXmlHotel["BookingStatus"] = BookingStatus;
                            drXmlHotel["FinalSellingPrice"] = FinalMinPrice;
                            drXmlHotel["SellingPrice"] = FinalMinPrice;
                            ////  if (FinalMinPrice > 0)
                            DtHotel.Rows.Add(drXmlHotel);
                            #endregion
                            #region "GetRooms for this hotel"
                            bool willAddOrNot = true;
                            Int64 RaynaID = 0;
                            Int64 GiataId = 0;
                            int RatingHotel = 0;
                            Int64 OwnsystemHotelId = 0;
                            string Provider = Suppliername;
                            double MinPrice = 1;
                            _markup.GetHotelDataCommon(ref MinPrice, ref willAddOrNot, ref OwnsystemHotelId, ref RaynaID, ref GiataId, ref RatingHotel, ref drXmlHotel, commonSearch.AgentId, commonSearch.TariffID, commonSearch.dsAgentDetails, commonSearch.supplierGiataMaster, SearchParam, commonSearch.dtCountryCityMarkup, commonSearch.B2CMarkup, commonSearch.IsParentId, commonSearch.SubAgentMarkup, commonSearch.SearchByHotel, commonSearch.HotelIDS);
                            if (willAddOrNot)
                            {
                                DataTable dtHotelRoom = _commonMethod.GenerateRoomtypeDataTableNewV2();
                                DataTable dtCancellation = _commonMethod.GenerateCancellationDataTableNewV2();
                                DataTable DtRoomRateBreackUp = _commonMethod.GenerateoomRateBreackUpDataTableNewV2();
                                int CutOffDays = CacheMaster.CutOffDays;
                                //CutOffDays = _commonMethod.GetCutOffDays(_commonMethod.dtSuplierMarkup, Provider);
                                CutOffDays = _commonMethod.GetCutOffDays(commonSearch.dtSuplierMarkup, Provider);
                                #region "Room Logic"
                                if (SearchParam.Filters.IsShowRooms == 1)
                                {
                                    double AgentPercentage = 0.0;
                                    // Boolean blnIsShowSupplier = false;
                                    double AgentMarkupAmount = 0.0;
                                    Boolean isPerce = true;

                                    double AgentCompanyPercentage = Convert.ToDouble(SearchParam.AgentCompanyPercentage);
                                    bool IsB2C = Convert.ToBoolean(Convert.ToByte(SearchParam.IsB2BOrB2C));
                                    bool blnIsShowSupplier = false;
                                    //_markup.getAgetMarkupSetting(Provider, GiataId.ToString(), commonSearch.dtSuplierMarkup, commonSearch.dtMarkupDetails, ref AgentPercentage, ref AgentMarkupAmount, ref blnIsShowSupplier, ref isPerce, commonSearch.AgentId, RatingHotel, SearchParam.CityID, SearchParam.CountryID.ToString(), SearchParam.CheckInDate);
                                    _markup.getAgetMarkupSettingNew_new(Provider, Convert.ToString(GiataId), commonSearch.dtSuplierMarkup, commonSearch.dsMarkupDetails, ref AgentPercentage, ref AgentMarkupAmount, ref blnIsShowSupplier, ref isPerce, commonSearch.AgentId, RatingHotel, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), SearchParam.CheckInDate);
                                    int maxOcupancy = 0;
                                    for (int i = 0; i < SearchParam.Rooms.Count; i++)
                                        maxOcupancy = maxOcupancy + SearchParam.Rooms[i].NoofChild + SearchParam.Rooms[i].NoofAdults;
                                    #region Supplier Room Code block
                                    iBoosyClass.MethaBook.Accommodation drhotel = MethaBookResponse.availabilityRs.accommodations[hh];
                                    string Hotelcode = Convert.ToString(drhotel.code);
                                    string HotelName = Convert.ToString(drhotel.name);

                                    for (int j = 0; j < drhotel.availabilities.Count; j++)
                                    {
                                        string AvaillabilityCode = Convert.ToString(drhotel.availabilities[j].code);
                                        string PaymentType = Convert.ToString(drhotel.availabilities[j].paymentType);
                                        string NonRefundable = Convert.ToString(drhotel.availabilities[j].nonRefundable);
                                        string MealCode = Convert.ToString(drhotel.availabilities[j].mealPlan.code);
                                        string MealName = Convert.ToString(drhotel.availabilities[j].mealPlan.name);
                                        double ItemPrice = Convert.ToDouble(drhotel.availabilities[j].price.net);
                                        double BuyingPrice = Convert.ToDouble(drhotel.availabilities[j].price.net);
                                        Currency = Convert.ToString(drhotel.availabilities[j].price.currencyCode);
                                        double NightPrice = Convert.ToDouble(ItemPrice / SearchParam.NoofNights);
                                        string RoomCode = "", RoomName = "";
                                        int rooms = drhotel.availabilities[j].rooms.Count;

                                         CurrencyFactorMethaBook = 1;
                                      

                                        if (Currency.ToLower() != tokenData.SiteCurrency.ToLower())
                                        {
                                            CurrencyFactorMethaBook = _commonMethod.GetROEByCurrency(Currency, dtROE);
                                        }
                                        ItemPrice = ItemPrice * CurrencyFactorMethaBook;
                                        BuyingPrice = BuyingPrice * CurrencyFactorMethaBook;
                                        //#methachange

                                        DataRow drMethaBookRoom = dtHotelRoom.NewRow();
                                        //DataRow drMethaBookRoom = dtMethaBookHotelRoom.NewRow();
                                        string MainRoomName = "";
                                        for (int k = 0; k < drhotel.availabilities[j].rooms.Count; k++)
                                        {
                                            RoomCode += "|" + Convert.ToString(drhotel.availabilities[j].rooms[k].code);
                                            RoomName = Convert.ToString(drhotel.availabilities[j].rooms[k].name) + "(" + Convert.ToString(drhotel.availabilities[j].rooms[k].description) + ")";

                                            if (SearchParam.Rooms.Count > 1) //Added 21-Aug-2025 As per mustafa sir
                                            {
                                                if (k == 0)
                                                    MainRoomName = "1 - " + RoomName;
                                                else
                                                    MainRoomName = MainRoomName + ", 1 - " + RoomName;
                                            }
                                            else
                                            {
                                                MainRoomName = RoomName;
                                            }
                                        }
                                        string RoomTypeId = Convert.ToString(10 + "$$$" + AvaillabilityCode + "$$$" + RoomCode);
                                        RoomTypeId = RoomTypeId.Replace("'", "");

                                        //#methachange

                                        for (int k = 0; k < drhotel.availabilities[j].rooms.Count; k++)
                                        {
                                            RoomCode = Convert.ToString(drhotel.availabilities[j].rooms[k].code);
                                            RoomName = Convert.ToString(drhotel.availabilities[j].rooms[k].description);

                                        }
                                        #region "Promotion"
                                        if (drhotel.availabilities[j].promotions != null)
                                        {
                                            for (int n = 0; n < drhotel.availabilities[j].promotions.Count; n++)
                                            {
                                                if (drhotel.availabilities[j].promotions[n].code != null)
                                                    drMethaBookRoom["OfferCode"] = Convert.ToString(drhotel.availabilities[j].promotions[n].code);
                                                else
                                                    drMethaBookRoom["OfferCode"] = "";
                                                if (drhotel.availabilities[j].promotions[n].name != null)
                                                    drMethaBookRoom["OfferDescription"] = Convert.ToString(drhotel.availabilities[j].promotions[n].name);
                                                else
                                                    drMethaBookRoom["OfferDescription"] = "";
                                            }
                                        }
                                        else
                                        {
                                            drMethaBookRoom["OfferCode"] = "";
                                            drMethaBookRoom["OfferDescription"] = "";
                                        }
                                        #endregion

                                        drMethaBookRoom["OwnsystemHotelId"] = OwnsystemHotelId;
                                        drMethaBookRoom["RoomTypeCode"] = RoomCode;
                                        drMethaBookRoom["BoardType"] = "";
                                        drMethaBookRoom["MealName"] = MealName;
                                        drMethaBookRoom["MealId"] = MealCode;
                                        drMethaBookRoom["Type"] = "";
                                        drMethaBookRoom["Board"] = "";
                                        drMethaBookRoom["BookingStatus"] = BookingStatus;
                                        drMethaBookRoom["UniqueNo"] = "";
                                        drMethaBookRoom["ContractID"] = 0;
                                        drMethaBookRoom["RoomNo"] = 1;
                                        drMethaBookRoom["BoardTypeID"] = "";
                                        drMethaBookRoom["AdultsOccupancy"] = 1;
                                        drMethaBookRoom["ChildOccupancy"] = 1;
                                        drMethaBookRoom["RoomSequenceNo"] = "";
                                        drMethaBookRoom["EssentialInformation"] = "";
                                        drMethaBookRoom["Characteristic"] = "";
                                        #region Vat
                                        _markup.ProcessHotelRoom_WithMarkup(CurrencyFactorMethaBook, Currency, true, true, SearchParam.NoofNights * SearchParam.Rooms.Count, SearchParam.CityID, SearchParam.CountryID.ToString(), SearchParam.CheckInDate, SearchParam.CheckOutDate, ref ItemPrice, ref drMethaBookRoom, isPerce, AgentPercentage, AgentMarkupAmount, commonSearch.SubAgentMarkup, commonSearch.B2CMarkup, commonSearch.IsParentId);
                                        #endregion
                                        drMethaBookRoom["SupplierName"] = Suppliername;
                                        // drMethaBookRoom["RoomTypeName"] = RoomName;
                                        drMethaBookRoom["RoomTypeName"] = MainRoomName; //#methachange
                                        drMethaBookRoom["RoomTypeId"] = RoomTypeId;
                                        dtHotelRoom.Rows.Add(drMethaBookRoom);

                                        #region "Cancellation Policy"
                                        //added 13-12-2025 new
                                        string checkin2 = Convert.ToString(SearchParam.CheckInDate);
                                        DateTime dtCheckin1 = DateTime.ParseExact(checkin2, "dd/MM/yyyy", null);
                                        //end

                                        ItemPrice = BuyingPrice; //14/07/25

                                        if (NonRefundable == null || NonRefundable.ToLower() == "false")
                                        {
                                            if (drhotel.availabilities[j].cancellationPolicy != null)
                                            {
                                                List<MethaBook.PenaltyRule> Paneltyrule = new List<MethaBook.PenaltyRule>(drhotel.availabilities[j].cancellationPolicy.penaltyRules);

                                                //DateTime Todate = DateTime.Now.Date;//comment by anu
                                                DateTime Todate = dtCheckin1; //added by anu new
                                                int loop = 1;
                                                //string checkin2 = Convert.ToString(SearchParam.CheckInDate);
                                                //DateTime dtCheckin1 = DateTime.ParseExact(checkin2, "dd/MM/yyyy", null);

                                                Paneltyrule = Paneltyrule.OrderBy(item => item.dateFrom).ToList();
                                                for (int m = 0; m < Paneltyrule.Count; m++)
                                                {
                                                    double cancellationamt = 0.0;
                                                    string Type = Convert.ToString(Paneltyrule[m].type);
                                                    switch (Type.ToLower())
                                                    {
                                                        case "noshow":
                                                            if (Type == "amount")
                                                                cancellationamt = Convert.ToDouble(Paneltyrule[m].price.net);
                                                            else if (Type == "percentage")
                                                            {
                                                                double Percentage = Convert.ToDouble(Paneltyrule[m].percentage);
                                                                cancellationamt = Convert.ToDouble(ItemPrice * Percentage / 100);
                                                            }
                                                            else if (Type == "nonrefundable")
                                                                cancellationamt = ItemPrice;
                                                            else
                                                            {
                                                                int night1 = Convert.ToInt32(Paneltyrule[m].nights);
                                                                if (night1 > 0) //modify 20-Aug-2025
                                                                {
                                                                    cancellationamt = Convert.ToDouble(ItemPrice / night1);
                                                                }
                                                                else
                                                                {
                                                                    cancellationamt = Convert.ToDouble(ItemPrice);

                                                                }
                                                            }
                                                            break;
                                                        case "amount":
                                                            cancellationamt = Convert.ToDouble(Paneltyrule[m].price.net);
                                                            break;
                                                        case "percentage":
                                                            double Percentage1 = Convert.ToDouble(Paneltyrule[m].percentage);
                                                            cancellationamt = Convert.ToDouble(ItemPrice * Percentage1 / 100);
                                                            break;
                                                        case "night":
                                                            int night = Convert.ToInt32(Paneltyrule[m].nights);
                                                            if (night > 0) //modify 20-Aug-2025
                                                            {
                                                                cancellationamt = Convert.ToDouble(ItemPrice / night);
                                                            }
                                                            else
                                                            {
                                                                cancellationamt = Convert.ToDouble(ItemPrice);
                                                            }
                                                            break;
                                                        case "nonrefundable":
                                                            cancellationamt = ItemPrice;
                                                            break;
                                                        default:
                                                            break;
                                                    }
                                                    double VatOnPolicy = 0;
                                                    double cancellationChargeAmount = _markup.ProcessHotelCancelationPrice_WithMarkup(ref cancellationamt, SearchParam.NoofNights, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), cancellationamt, isPerce, commonSearch.B2CMarkup, AgentPercentage, AgentMarkupAmount, commonSearch.IsParentId, commonSearch.SubAgentMarkup, ref VatOnPolicy);
                                                    DateTime canceldate = Convert.ToDateTime(Paneltyrule[m].dateFrom);
                                                    DateTime currentdate = DateTime.Now.Date;
                                                    if (loop <= Paneltyrule.Count - 1)
                                                    {
                                                        Todate = Convert.ToDateTime(Paneltyrule[loop].dateFrom).AddDays(CutOffDays);
                                                    }
                                                    if (canceldate < currentdate)
                                                        canceldate = DateTime.Now.Date;
                                                    if (canceldate.AddDays(CutOffDays).Date > DateTime.Now.Date)
                                                        canceldate = canceldate.AddDays(CutOffDays).Date;
                                                    if (canceldate > currentdate)
                                                    {
                                                        if (m == 0)
                                                        {
                                                            double Zero = 0;
                                                            dtCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref Zero, dtCancellation.NewRow(), CurrencyFactorMethaBook, Currency, Provider, Convert.ToString(OwnsystemHotelId), Convert.ToString(1), RoomTypeId, RoomName, MealCode, currentdate, canceldate.AddDays(-1).AddHours(23).AddMinutes(59).AddSeconds(59), 0, 0, MealCode, 0, "", "No", "", "", "", ""));
                                                            dtCancellation.AcceptChanges();
                                                        }
                                                        dtCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref cancellationamt, dtCancellation.NewRow(), CurrencyFactorMethaBook, Currency, Provider, Convert.ToString(OwnsystemHotelId), Convert.ToString(1), RoomTypeId, RoomName, MealCode, canceldate, Todate, cancellationamt, cancellationamt, MealCode, 0, "", "No", "", "", "", ""));
                                                        dtCancellation.AcceptChanges();
                                                    }
                                                    else
                                                    {
                                                        dtCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref cancellationamt, dtCancellation.NewRow(), CurrencyFactorMethaBook, Currency, Provider, Convert.ToString(OwnsystemHotelId), Convert.ToString(1), RoomTypeId, RoomName, MealCode, canceldate, dtCheckin1, cancellationamt, cancellationamt, MealCode, 0, "", "No", "", "", "", ""));
                                                        dtCancellation.AcceptChanges();
                                                    }
                                                    loop++;
                                                    Todate = dtCheckin1;
                                                }

                                            }
                                            else
                                            {
                                                dtCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref ItemPrice, dtCancellation.NewRow(), CurrencyFactorMethaBook, Currency, Provider, Convert.ToString(OwnsystemHotelId), Convert.ToString(1), RoomTypeId, RoomName, MealCode, DateTime.Now.Date, /*DateTime.Now.Date*/dtCheckin1, ItemPrice, ItemPrice, MealCode, 0, "", "Yes", "", "", "", ""));//dtCheckin1 change 13-02-2025
                                                dtCancellation.AcceptChanges();
                                            }
                                        }
                                        else
                                        {
                                            dtCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref ItemPrice, dtCancellation.NewRow(), CurrencyFactorMethaBook, Currency, Provider, Convert.ToString(OwnsystemHotelId), Convert.ToString(1), RoomTypeId, RoomName, MealCode, DateTime.Now.Date, /*DateTime.Now.Date*/dtCheckin1, ItemPrice, ItemPrice, MealCode, 0, "", "Yes", "", "", "", ""));//dtCheckin1 change 13-02-2025
                                            dtCancellation.AcceptChanges();
                                            //}
                                            #endregion
                                        }


                                        #endregion

                                    }
                                    #endregion
                                    Options ops = (await _supplierRes.GetCommonRoomForSuppliers(GiataId, commonSearch.tokenData, commonSearch.advancedOptions, FinalMinPrice, drXmlHotel, dtHotelRoom, dtCancellation, DtRoomRateBreackUp, SearchParam));
                                    if (ops != null)
                                        options.Add(ops);
                                }
                                #endregion
                                #endregion
                            }

                            #endregion

                        }
                    }
                    #endregion
                    return options;
                }
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
                    timer_room_Methabook.Stop();
                    Item.CodeProcessingTimeSupplier = timer_room_Methabook.ElapsedMilliseconds;
                    if (SearchParam != null && ResProccessTimerLogActive == "1" && timer_room_Methabook.ElapsedMilliseconds > Convert.ToInt32(ResProccessTimeValueforSaveLog))
                    {
                        
                        SearchParam.SupplierTime += ",|Finish:Methabook Process Response  " + "|Time :" + timer_room_Methabook.Elapsed.ToString();
                        _ = Task.Run(() => _commonMethod.FireAway_SQL_ResPorcessTimeLog("", timer_room_Methabook.Elapsed.ToString(), "Methabook", SearchParam.SearchKey, 1, SearchParam.AgentID));
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

        public async Task<CancellationDto> MethaBook_Cancellation(string MealCode, DataSet dsHotel, string hotelcode, string ownsystemid, string roomtypeid, DataTable dtAgentDetail, DataTable dtSuplierMarkup, DataTable dtSearchConfig, HotelSearchParameter SearchPara, string roomno, string provider, int AgentId, DataTable dtMarkupDetails, string SearchKey, string GiataId = "0", int hotelRating = 0)
        {
            CancellationDto cancellationDto = new CancellationDto();
            string IsBooking = "Available";
            try
            {
                DataTable dtMethaBookCancellation = _commonMethod.GenerateCancellationDataTableNewV2();
                int CutOffDays = XMLHotelBookingCommon.CutOffDays;
                CutOffDays = _commonMethod.GetCutOffDays(dtSuplierMarkup, provider);
                Boolean isPerce = true;
                double AgentPercentage = 0.0;
                Boolean blnIsShowSupplier = false;
                double AgentMarkupAmount = 0.0;
                _markup.getAgetMarkupSetting(provider, GiataId, dtSuplierMarkup, dtMarkupDetails, ref AgentPercentage, ref AgentMarkupAmount, ref blnIsShowSupplier, ref isPerce, Convert.ToInt32(dtAgentDetail.Rows[0]["AgentID"]), hotelRating, SearchPara.CityID, SearchPara.CountryID.ToString(), SearchPara.CheckInDate);
                double AgentCompanyPercentage = Convert.ToDouble(SearchPara.AgentCompanyPercentage);
                bool IsB2C = Convert.ToBoolean(Convert.ToByte(SearchPara.IsB2BOrB2C));

                double B2CMarkup = (IsB2C == false ? AgentCompanyPercentage : XMLHotelBookingCommon.B2CMarkup);
                if (IsB2C == true)
                    AgentPercentage = 0;

                DataRow[] drSuplierMarkup = (dtSuplierMarkup.Select("SupplierName='" + provider + "'"));
                int SupplierId = Convert.ToInt32(drSuplierMarkup[0]["SupplierID"]);
                string MethaBookCurrency = Convert.ToString(drSuplierMarkup[0]["CurrencyShortName"]); //added on 30-05-25 


                DataRow[] drSearchConfig = (dtSearchConfig.Select("SupplierID='" + SupplierId + "'"));
                int SearchTimeOut = Convert.ToInt32(drSearchConfig[0]["BookingConfirmationTimeout"]);

                string CheckIn = Convert.ToString(SearchPara.CheckInDate);
                string CheckOut = Convert.ToString(SearchPara.CheckOutDate);
                CheckIn = CheckIn.Substring(6, 4) + "-" + CheckIn.Substring(3, 2) + "-" + CheckIn.Substring(0, 2);
                CheckOut = CheckOut.Substring(6, 4) + "-" + CheckOut.Substring(3, 2) + "-" + CheckOut.Substring(0, 2);
                string[] MBArray = Convert.ToString(roomtypeid).Split(new String[] { "$$$" }, StringSplitOptions.None);
                string AvailabilityCode = MBArray[1];

                string PriceCheckRequest = MethaBookPriceCheckRequest(AvailabilityCode, CheckIn, CheckOut, hotelcode, MealCode);
                string Error = "";
                //string ResponseData = MethaBookResponseNew(ref Error, PriceCheckRequest, Convert.ToString(drSuplierMarkup[0]["WebSiteURL"]) + "/Accommodation/v1.0/Valuation", Convert.ToString(drSuplierMarkup[0]["UserName"]), Convert.ToString(drSuplierMarkup[0]["PassWord"]), 120000, "GET");
                ResponseDto responseDto = await MethaBookResponseNew(PriceCheckRequest, Convert.ToString(drSuplierMarkup[0]["WebSiteURL"]) + _supplierConfig.IBoosy.PriceCheck, Convert.ToString(drSuplierMarkup[0]["UserName"]), Convert.ToString(drSuplierMarkup[0]["PassWord"]), 120000, "GET");
                string ResponseData = responseDto.Response;
                Error = responseDto.Error;
                _commonMethod.ThirdPartyGetPolicyHistory(Convert.ToString(roomtypeid), Convert.ToString(ownsystemid), PriceCheckRequest, Convert.ToString(ResponseData), "After Get Policy", provider, Error + "Response", SearchKey, "API");


                if (ResponseData == "")
                {
                    IsBooking = "NotAvailable";
                    cancellationDto.dsHotelData = null;
                    cancellationDto.ISBooking = IsBooking;
                    return cancellationDto;
                }

                string ConvertXml = Newtonsoft.Json.JsonConvert.DeserializeXmlNode("{\"Row\":" + ResponseData + "}", "root").InnerXml;
                DataSet HotelDoResponse = _commonMethod.GetDataSetFromXMLString(ConvertXml);
                MethaBook.RootObject MethaBookResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<MethaBook.RootObject>(ResponseData);
                if (MethaBookResponse == null)
                {
                    IsBooking = "NotAvailable";
                    cancellationDto.dsHotelData = null;
                    cancellationDto.ISBooking = IsBooking;
                    return cancellationDto;
                }
                if (MethaBookResponse.valuationRS.availability != null)
                {
                    string NonRefundable = Convert.ToString(MethaBookResponse.valuationRS.availability.nonRefundable);
                    double ItemPrice = Convert.ToDouble(MethaBookResponse.valuationRS.availability.price.net);

                    string EssentialInfo = "", Fees = "";

                    if (MethaBookResponse.valuationRS.availability.remarks != null)
                    {
                        if (MethaBookResponse.valuationRS.availability.remarks.Count > 0)
                        {
                            for (int essen = 0; essen < MethaBookResponse.valuationRS.availability.remarks.Count; essen++)
                            {
                                EssentialInfo += Convert.ToString(MethaBookResponse.valuationRS.availability.remarks[essen]);
                            }
                        }
                    }
                    else
                    {
                        EssentialInfo = "N/A";
                    }
                    if (MethaBookResponse.valuationRS.availability.fees != null)
                    {
                        if (MethaBookResponse.valuationRS.availability.fees.Count > 0)
                        {
                            for (int essen = 0; essen < MethaBookResponse.valuationRS.availability.fees.Count; essen++)
                            {
                                if (MethaBookResponse.valuationRS.availability.fees[essen].currencyCode != null && MethaBookResponse.valuationRS.availability.fees[essen].amount != null)
                                {
                                    if (MethaBookResponse.valuationRS.availability.fees[essen].description != null)
                                    {
                                        Fees = "<br/><b>Description:</b>" + Convert.ToString(MethaBookResponse.valuationRS.availability.fees[essen].description);
                                    }
                                    else
                                    {
                                        Fees = "<br/><b>Description:Pay At Hotel</b>";
                                    }
                                    Fees += "<br/><b>Currency:</b>" + Convert.ToString(MethaBookResponse.valuationRS.availability.fees[essen].currencyCode);
                                    Fees += "<br/><b><b>Amount:</b>" + Convert.ToString(MethaBookResponse.valuationRS.availability.fees[essen].amount);
                                }
                            }
                        }

                    }
                    if (Fees != "")
                    {
                        EssentialInfo = EssentialInfo + "<br/><br/><b>Fees:</b>" + Fees;
                    }
                    else
                    {
                        EssentialInfo = EssentialInfo;
                    }


                    string RoomName = "";
                    #region "Cancellation Policy"
                    
                    if (NonRefundable == null || NonRefundable.ToLower() == "false")
                    {
                        if (MethaBookResponse.valuationRS.availability.cancellationPolicy != null)
                        {
                            List<MethaBook.PenaltyRule> Paneltyrule = new List<MethaBook.PenaltyRule>(MethaBookResponse.valuationRS.availability.cancellationPolicy.penaltyRules);

                            DateTime Todate = DateTime.Now.Date;
                            int loop = 1;
                            string checkin2 = Convert.ToString(SearchPara.CheckInDate);
                            DateTime dtCheckin1 = DateTime.ParseExact(checkin2, "dd/MM/yyyy", null);

                            Paneltyrule = Paneltyrule.OrderBy(item => item.dateFrom).ToList();
                            for (int m = 0; m < Paneltyrule.Count; m++)
                            {
                                double cancellationamt = 0;
                                string Type = Convert.ToString(Paneltyrule[m].type);
                                switch (Type.ToLower())
                                {
                                    case "noshow":
                                        if (Type == "amount")
                                            cancellationamt = Convert.ToDouble(Paneltyrule[m].price.net);
                                        else if (Type == "percentage")
                                        {
                                            double Percentage = Convert.ToDouble(Paneltyrule[m].percentage);
                                            cancellationamt = Convert.ToDouble(ItemPrice * Percentage / 100);
                                        }
                                        else if (Type == "nonrefundable")
                                            cancellationamt = ItemPrice;
                                        else
                                        {
                                            int night1 = Convert.ToInt32(Paneltyrule[m].nights);
                                            if (night1 == 0)
                                                night1 = 1;
                                            cancellationamt = Convert.ToDouble(ItemPrice / night1);
                                        }
                                        break;
                                    case "amount":
                                        cancellationamt = Convert.ToDouble(Paneltyrule[m].price.net);
                                        break;
                                    case "percentage":
                                        double Percentage1 = Convert.ToDouble(Paneltyrule[m].percentage);
                                        cancellationamt = Convert.ToDouble(ItemPrice * Percentage1 / 100);
                                        break;
                                    case "night":
                                        int night = Convert.ToInt32(Paneltyrule[m].nights);
                                        if (night == 0)
                                            night = 1;
                                        cancellationamt = Convert.ToDouble(ItemPrice / night);
                                        break;
                                    case "nonrefundable":
                                        cancellationamt = ItemPrice;
                                        break;
                                    default:
                                        break;
                                }
                                double VatOnPolicy = 0;
                                double cancellationChargeAmount = _markup.ProcessHotelCancelationPrice_WithMarkup(ref cancellationamt, SearchPara.NoofNights, SearchPara.CityID, SearchPara.CountryID.ToString(), cancellationamt, isPerce, XMLHotelBookingCommon.B2CMarkup, AgentPercentage, AgentMarkupAmount, Convert.ToInt32(dtAgentDetail.Rows[0]["IsParentId"]), Convert.ToDouble(dtAgentDetail.Rows[0]["B2CMarkupPer"]), ref VatOnPolicy);
                                DateTime canceldate = Convert.ToDateTime(Paneltyrule[m].dateFrom);
                                DateTime currentdate = DateTime.Now.Date;
                                if (loop <= Paneltyrule.Count - 1)
                                {
                                    Todate = Convert.ToDateTime(Paneltyrule[loop].dateFrom).AddDays(CutOffDays);
                                }
                                if (Paneltyrule.Count == 1)
                                {
                                    Todate = dtCheckin1;
                                }
                                if (canceldate < currentdate)
                                    canceldate = DateTime.Now.Date;
                               
                                canceldate = canceldate.AddDays(CutOffDays).Date;

                                if (canceldate > currentdate)
                                {
                                    if (m == 0)
                                    {
                                        double Zero = 0;
                                        dtMethaBookCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref Zero, dtMethaBookCancellation.NewRow(), 1, MethaBookCurrency, provider, ownsystemid, Convert.ToString(1), roomtypeid, RoomName, MealCode, currentdate, canceldate.AddDays(-1).AddHours(23).AddMinutes(59).AddSeconds(59), 0, 0, MealCode, 0, "", "No", "", EssentialInfo, "", ""));
                                        dtMethaBookCancellation.AcceptChanges();
                                    }
                                    dtMethaBookCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref cancellationamt, dtMethaBookCancellation.NewRow(), 1, MethaBookCurrency, provider, ownsystemid, Convert.ToString(1), roomtypeid, RoomName, MealCode, canceldate, Todate, cancellationChargeAmount, cancellationChargeAmount, MealCode, 0, "", "No", "", EssentialInfo, "", ""));
                                    dtMethaBookCancellation.AcceptChanges();
                                }
                                else
                                {
                                    dtMethaBookCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref cancellationamt, dtMethaBookCancellation.NewRow(), 1, MethaBookCurrency, provider, ownsystemid, Convert.ToString(1), roomtypeid, RoomName, MealCode, canceldate, dtCheckin1, cancellationChargeAmount, cancellationChargeAmount, MealCode, 0, "", "No", "", EssentialInfo, "", ""));
                                    dtMethaBookCancellation.AcceptChanges();
                                }
                                loop++;
                                Todate = dtCheckin1;
                            }

                        }
                        else
                        {
                            dtMethaBookCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref ItemPrice, dtMethaBookCancellation.NewRow(), 1, MethaBookCurrency, provider, ownsystemid, Convert.ToString(1), roomtypeid, RoomName, MealCode, DateTime.Now.Date, /*DateTime.Now.Date*/ DateTime.Now.Date, ItemPrice, ItemPrice, MealCode, 0, "", "Yes", "", EssentialInfo, "", ""));
                            dtMethaBookCancellation.AcceptChanges();
                        }
                    }
                    else
                    {
                        dtMethaBookCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref ItemPrice, dtMethaBookCancellation.NewRow(), 1, MethaBookCurrency, provider, ownsystemid, Convert.ToString(1), roomtypeid, RoomName, MealCode, DateTime.Now.Date, /*DateTime.Now.Date*/ DateTime.Now.Date, ItemPrice, ItemPrice, MealCode, 0, "", "Yes", "", EssentialInfo, "", ""));
                        dtMethaBookCancellation.AcceptChanges();
                    }
                    #endregion
                }
                dsHotel.Tables[2].Merge(dtMethaBookCancellation);
            }
            catch (Exception ex)
            {
                _ = _exception.ErrorNew(ex, _LogAPI.Recheck, SearchKey);

                IsBooking = "NotAvailable";
                cancellationDto.dsHotelData = null;
                cancellationDto.ISBooking = IsBooking;
                return cancellationDto;
            }

            cancellationDto.dsHotelData = dsHotel;
            cancellationDto.ISBooking = IsBooking;
            return cancellationDto;

        }

        public string MethaBookPriceCheckRequest(string AvailabilityCode, string CheckInDate, string CheckOutDate, string HotelCode, string MealCode)
        {
            string PriceCheckReq = "";
            try
            {
                StringBuilder PriceCheck = new StringBuilder();
                PriceCheck.Append("?language=en");
                PriceCheck.Append("&availabilityCode=" + AvailabilityCode + "");
                PriceCheck.Append("&accommodationCode=" + HotelCode + "");
                PriceCheck.Append("&checkInDate=" + CheckInDate + "");
                PriceCheck.Append("&checkOutDate=" + CheckOutDate + "");
                PriceCheck.Append("&mealPlanCode=" + MealCode + "");
                PriceCheckReq = PriceCheck.ToString();
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
            }
            return PriceCheckReq;
        }

        public async Task<ResponseDto> MethaBookResponseNew(string Request, string Url, string UserName, string Password, int TimeOut, string Method, string Searchkey = "", bool frombook = false)
        {
            ResponseDto responseDto = new ResponseDto();
            string Response = "";
            string Error = "";
            try
            {
                string Key = Convert.ToBase64String(Encoding.ASCII.GetBytes(UserName + ":" + Password));
                string ReqUrl = "";
                if (frombook)
                {
                    ReqUrl = Url;
                }
                else
                {
                    ReqUrl = Url + Request;
                }
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(ReqUrl);
                request.Method = Method;
                request.ContentType = "application/json";
                request.Accept = "application/json";
                request.Headers.Add("Authorization", "Basic " + Key);
                request.Headers.Add("Accept-Encoding", "gzip");
                request.Timeout = TimeOut;
                if (Method == "PUT" || Method == "POST")
                {
                    Stream newStream = null;
                    byte[] data = Encoding.ASCII.GetBytes(Request);
                    request.ContentLength = data.Length;
                    newStream = request.GetRequestStream();
                    newStream.Write(data, 0, data.Length);
                }
                HttpWebResponse Response1 = (HttpWebResponse)await request.GetResponseAsync();
                Stream st = Response1.GetResponseStream();

                if (Response1.ContentEncoding.ToLower().Contains("gzip"))
                    st = new System.IO.Compression.GZipStream(st, System.IO.Compression.CompressionMode.Decompress);
                else if (Response1.ContentEncoding.ToLower().Contains("deflate"))
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

        public async Task<PriceCheckCMDto> MethaBookPriceCheck(string AvailabilityCode, DataTable dtCheckPriceReqStructure, DataSet dsCartDetails, DataRow[] MethaBookDetails, string UniqueNo, string AgentUniqueNo, int i, string IsFor, double CompanyBuyPrice, bool IsError, double itemprice, string BookingCode, string MealCode, TokenData tokenData, string Searchkey = "", int AgentId = 0)
        {
            PriceCheckCMDto priceCheckCMDto = new PriceCheckCMDto();

            string responsetype = "Success";
            string time = "";
            string SupplierName = Convert.ToString(dsCartDetails.Tables[1].DefaultView[0]["SupplierName"]);
            string Error = "";
            string PriceCheckRequest = "";
            string ResponseData = "";
             double CurrencyFactorMethabook = 1;
            try
            {
                string CheckIn = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["CheckInDate"]);
                CheckIn = CheckIn.Substring(0, 4) + "-" + CheckIn.Substring(4, 2) + "-" + CheckIn.Substring(6, 2);
                string CheckOut = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["CheckOutDate"]);
                CheckOut = CheckOut.Substring(0, 4) + "-" + CheckOut.Substring(4, 2) + "-" + CheckOut.Substring(6, 2);
                string HotelCode = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["XMLHotelId"]);
                int UserCartId = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[0]["UserCartID"]);
                int UserCartDetailId = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[0]["UserCartDetailId"]);
                string RoomType = Convert.ToString(dsCartDetails.Tables[1].DefaultView[0]["RoomTypeID"]);
                string Url = Convert.ToString(MethaBookDetails[0]["WebSiteURL"]);
                PriceCheckRequest = MethaBookPriceCheckRequest(AvailabilityCode, CheckIn, CheckOut, HotelCode, MealCode);
                _commonMethod.ThirdPartyBookingHistory(Convert.ToString(UniqueNo), Convert.ToString(AgentUniqueNo), PriceCheckRequest, "-", "Before Request hit Price Check", Convert.ToString(dsCartDetails.Tables[1].DefaultView[0]["SupplierName"]), "Request Confirm", "-", "WithInEarth");
                dtCheckPriceReqStructure.Rows.Add(_commonMethod.AddCheckPriceReq(dtCheckPriceReqStructure.NewRow(), i + 1, Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[0]["UserCartID"]), Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[0]["UserCartDetailId"]), Convert.ToString(dsCartDetails.Tables[1].DefaultView[0]["RoomTypeID"]), Convert.ToString(dsCartDetails.Tables[1].DefaultView[0]["SupplierName"]), PriceCheckRequest));

                System.Diagnostics.Stopwatch timerroomprocess = new System.Diagnostics.Stopwatch();
                timerroomprocess.Start();

                ResponseDto responseDto = await MethaBookResponseNew(PriceCheckRequest, Url + _supplierConfig.IBoosy.PriceCheck, Convert.ToString(MethaBookDetails[0]["UserName"]), Convert.ToString(MethaBookDetails[0]["PassWord"]), 120000, "GET", Searchkey);
                ResponseData = responseDto.Response;
                Error = responseDto.Error;

                timerroomprocess.Stop();
                time = timerroomprocess.Elapsed.ToString();

                _commonMethod.ThirdPartyBookingHistory(Convert.ToString(UniqueNo), Convert.ToString(AgentUniqueNo), PriceCheckRequest, ResponseData + Error, "After Request hit Price Check", Convert.ToString(dsCartDetails.Tables[1].DefaultView[0]["SupplierName"]), "Response Confirm", "-", "WithInEarth");
                if (ResponseData == "")
                {
                    try
                    {
                        responsetype = "GeneralError";
                        string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, PriceCheckRequest, ResponseData, Error);
                    }
                    catch (Exception e)
                    {
                        _ = _exception.Error(e, Searchkey);
                    }

                    priceCheckCMDto.Flag = false;
                    return priceCheckCMDto;
                }
                MethaBook.RootObject MethaBookResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<MethaBook.RootObject>(ResponseData);
                if (MethaBookResponse == null)
                {
                    try
                    {
                        responsetype = "GeneralError";
                        string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, PriceCheckRequest, ResponseData, Error);
                    }
                    catch (Exception e)
                    {
                        _ = _exception.Error(e, Searchkey);
                    }
                    priceCheckCMDto.Flag = false;
                    return priceCheckCMDto;
                }
                BookingCode = Convert.ToString(MethaBookResponse.valuationRS.availability.booking.bookingCode);
                double Price = Convert.ToDouble(MethaBookResponse.valuationRS.availability.price.net);
                String MethabookCurrency = Convert.ToString(MethaBookResponse.valuationRS.availability.price.currencyCode);
                if (MethabookCurrency.ToLower() != tokenData.SiteCurrency.ToLower())
                {
                    CurrencyFactorMethabook = _commonMethod.GetROEByCurrency(MethabookCurrency, tokenData.dtROE);
                }
                Price = Price * CurrencyFactorMethabook;


                itemprice = Convert.ToDouble(Price);
                bool IsSuccess = await _commonMethod.GetBookingCodeUpdateStatus(BookingCode, UserCartId);
                if (IsFor == "P")
                {
                    if (Convert.ToDouble((itemprice).TruncateDecimalPlaces(0)) > (CompanyBuyPrice).TruncateDecimalPlaces(0))
                    {
                        try
                        {
                            ResponseData = "#Price Changed " + "#Main Price = " + CompanyBuyPrice + " #New Price = " + itemprice + " #Response= " + ResponseData;
                            responsetype = "GeneralError";
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
                            ResponseData = "#Price Changed " + "#Main Price = " + CompanyBuyPrice + " #New Price = " + itemprice + " #Response= " + ResponseData;
                            responsetype = "GeneralError";
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
                try
                {
                    responsetype = "Success";
                    string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, PriceCheckRequest, ResponseData, Error);
                }
                catch (Exception e)
                {
                    _ = _exception.Error(e, Searchkey);
                }
            }
            catch (Exception ex)
            {
                try
                {
                    Error = ex.Message + ex.StackTrace.ToString();
                    responsetype = "GeneralError";
                    string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, PriceCheckRequest, ResponseData, Error);
                }
                catch (Exception e)
                {
                    _ = _exception.Error(e, Searchkey);
                }
                _ = _exception.Error(ex, Searchkey);
                IsError = true;
                itemprice = 0;

                priceCheckCMDto.Flag = false;
                priceCheckCMDto.ItemPrice = itemprice;
                priceCheckCMDto.IsError = IsError;
                return priceCheckCMDto;
            }

            priceCheckCMDto.Flag = false;
            priceCheckCMDto.ItemPrice = itemprice;
            return priceCheckCMDto;
        }

        public string MethaBookBookingRequest(string BookingCode, List<string> RoomCode, string HotelCode, DataTable dtPassengers, DataSet dsCartDetails, string ClientReference, string UniqueNo, bool IsErrors)
        {
            try
            {
                StringBuilder MethaBook = new StringBuilder();
                if (IsErrors == false)
                {
                    string CheckIn = Convert.ToString(dsCartDetails.Tables[0].Rows[0]["CheckInDate"]);
                    CheckIn = CheckIn.Substring(0, 4) + "-" + CheckIn.Substring(4, 2) + "-" + CheckIn.Substring(6, 2);
                    string CheckOut = Convert.ToString(dsCartDetails.Tables[0].Rows[0]["CheckOutDate"]);
                    CheckOut = CheckOut.Substring(0, 4) + "-" + CheckOut.Substring(4, 2) + "-" + CheckOut.Substring(6, 2);
                    string MealCode = Convert.ToString(dsCartDetails.Tables[1].DefaultView[0]["MealPlanCode"]);
                    double Gross = Convert.ToDouble(dsCartDetails.Tables[1].DefaultView[0]["CompanyBuyingPrice"]);
                    string Currency = Convert.ToString(dsCartDetails.Tables[0].Rows[0]["SupplierCurrency"]);

                    StringBuilder Paxes = new StringBuilder();

                    dtPassengers.DefaultView.RowFilter = "PickUp='" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[0]["RoomTypeId"]) + "' and UserCartID='" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[0]["UserCartID"]) + "'";
                    DataTable dtnewpassanger = dtPassengers.DefaultView.ToTable();

                    for (int i = 0; i < dtnewpassanger.DefaultView.Count; i++)
                    {
                        if (i >= 1)
                        {
                            Paxes.Append("},{");
                        }
                        Paxes.Append("\"PaxId\":" + (i + 1) + ",");
                        if (Convert.ToString(dtnewpassanger.Rows[i]["IsLead"]) == "1")
                        {
                            Paxes.Append("\"Holder\":\"true\",");
                            Paxes.Append("\"Document\":\"" + 111111 + "\",");
                        }
                        else
                        {
                            Paxes.Append("\"Holder\":\"false\",");
                        }
                        Paxes.Append("\"Name\":\"" + Convert.ToString(dtnewpassanger.Rows[i]["FirstName"]) + "\",");
                        Paxes.Append("\"SurName\":\"" + Convert.ToString(dtnewpassanger.Rows[i]["LastName"]) + "\",");
                        if (dtnewpassanger.Rows[i]["PaxType"].ToString().ToLower() == "child")
                            Paxes.Append("\"Age\":" + Convert.ToString(dtnewpassanger.Rows[i]["ChildAge"]) + "");
                        else
                            Paxes.Append("\"Age\":" + 30 + "");
                    }

                    StringBuilder RoomPax = new StringBuilder();
                    int PaxId = 0;
                    for (int j = 0; j < dsCartDetails.Tables[1].DefaultView.Count; j++)
                    {
                        dtPassengers.DefaultView.RowFilter = "PickUp='" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomTypeId"]) + "' and UserCartID='" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["UserCartID"]) + "' and RoomNo='" + Convert.ToInt32(j + 1) + "'";
                        DataTable dtnewpassanger1 = dtPassengers.DefaultView.ToTable();
                        if (j >= 1)
                        {
                            RoomPax.Append("},{");
                        }
                        RoomPax.Append("\"RoomType\":\"" + RoomCode[j] + "\",");
                        RoomPax.Append("\"RoomRefId\":\"" + (j + 1) + "\",");
                        RoomPax.Append("\"Paxes\":[{");
                        for (int k = 0; k < dtnewpassanger1.DefaultView.Count; k++)
                        {
                            PaxId = PaxId + 1;
                            if (k >= 1)
                            {
                                RoomPax.Append("},{");
                            }
                            RoomPax.Append("\"PaxId\":" + PaxId + "");
                        }
                        RoomPax.Append("}]");
                    }
                    MethaBook.Append("{");
                    MethaBook.Append("\"bookingRq\":{");
                    MethaBook.Append("\"Language\":" + "\"en\",");
                    MethaBook.Append("\"ClientReference\":\"" + ClientReference + "\",");
                    MethaBook.Append("\"Paxes\":[{");
                    MethaBook.Append(Paxes.ToString());
                    MethaBook.Append("}],");
                    MethaBook.Append("\"Services\":{");
                    MethaBook.Append("\"AccommodationServices\":[{");
                    MethaBook.Append("\"BookingCode\":\"" + BookingCode + "\",");
                    MethaBook.Append("\"AccommodationCode\":\"" + HotelCode + "\",");
                    MethaBook.Append("\"CheckInDate\":\"" + CheckIn + "\",");
                    MethaBook.Append("\"CheckOutDate\":\"" + CheckOut + "\",");
                    MethaBook.Append("\"MealPlanCode\":\"" + MealCode + "\",");
                    MethaBook.Append("\"Price\":{");
                    MethaBook.Append("\"gross\":\"" + Gross + "\",");
                    MethaBook.Append("\"currencyCode\":\"" + Currency + "\"");
                    MethaBook.Append("},");
                    MethaBook.Append("\"Rooms\":[{");
                    MethaBook.Append(RoomPax.ToString());
                    MethaBook.Append("}]");
                    MethaBook.Append("}]");
                    MethaBook.Append("}");
                    MethaBook.Append("}");
                    MethaBook.Append("}");

                    string MethaBookReq = MethaBook.ToString();
                    return MethaBookReq;
                }
                else
                    return "";
            }
            catch (Exception ex)
            {
                _ = _exception.ErrorNew(ex, _LogAPI.Book);
                return "";
            }
        }
        public string MethaBookBookingRequest_newversion(string BookingCode, List<string> RoomCode, string HotelCode, DataTable dtPassengers, BookRequest_newversion bookRequest, string ClientReference, string UniqueNo, bool IsErrors)
        {
            try
            {
                StringBuilder MethaBook = new StringBuilder();
                //if (IsErrors == false)
                //{
                //    string CheckIn = Convert.ToString(bookRequest.userCart.UserCartDetails[0].CheckInDate);
                //    CheckIn = CheckIn.Substring(0, 4) + "-" + CheckIn.Substring(4, 2) + "-" + CheckIn.Substring(6, 2);
                //    string CheckOut = Convert.ToString(bookRequest.userCart.UserCartDetails[0].CheckOutDate);
                //    CheckOut = CheckOut.Substring(0, 4) + "-" + CheckOut.Substring(4, 2) + "-" + CheckOut.Substring(6, 2);
                //    string MealCode = Convert.ToString(bookRequest.userCart.UserCartDetails[0].MealPlan);
                //    double Gross = Convert.ToDouble(bookRequest.userCart.UserCartDetails[0].BaseServiceBuyingAmount);
                //    //string Currency = Convert.ToString(dsCartDetails.Tables[0].Rows[0]["SupplierCurrency"]);

                //    StringBuilder Paxes = new StringBuilder();

                //    dtPassengers.DefaultView.RowFilter = "PickUp='" + Convert.ToString(bookRequest.userCart.UserCartDetails[0].RoomTypeId) + "' and UserCartID='" + Convert.ToString(bookRequest.userCart.UserCartDetails[0].UserCartId) + "'";
                //    DataTable dtnewpassanger = dtPassengers.DefaultView.ToTable();

                //    for (int i = 0; i < dtnewpassanger.DefaultView.Count; i++)
                //    {
                //        if (i >= 1)
                //        {
                //            Paxes.Append("},{");
                //        }
                //        Paxes.Append("\"PaxId\":" + (i + 1) + ",");
                //        if (Convert.ToString(dtnewpassanger.Rows[i]["IsLead"]) == "1")
                //        {
                //            Paxes.Append("\"Holder\":\"true\",");
                //            Paxes.Append("\"Document\":\"" + 111111 + "\",");
                //        }
                //        else
                //        {
                //            Paxes.Append("\"Holder\":\"false\",");
                //        }
                //        Paxes.Append("\"Name\":\"" + Convert.ToString(dtnewpassanger.Rows[i]["FirstName"]) + "\",");
                //        Paxes.Append("\"SurName\":\"" + Convert.ToString(dtnewpassanger.Rows[i]["LastName"]) + "\",");
                //        if (dtnewpassanger.Rows[i]["PaxType"].ToString().ToLower() == "child")
                //            Paxes.Append("\"Age\":" + Convert.ToString(dtnewpassanger.Rows[i]["ChildAge"]) + "");
                //        else
                //            Paxes.Append("\"Age\":" + 30 + "");
                //    }

                //    StringBuilder RoomPax = new StringBuilder();
                //    int PaxId = 0;
                //    for (int j = 0; j < dsCartDetails.Tables[1].DefaultView.Count; j++)
                //    {
                //        dtPassengers.DefaultView.RowFilter = "PickUp='" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomTypeId"]) + "' and UserCartID='" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["UserCartID"]) + "' and RoomNo='" + Convert.ToInt32(j + 1) + "'";
                //        DataTable dtnewpassanger1 = dtPassengers.DefaultView.ToTable();
                //        if (j >= 1)
                //        {
                //            RoomPax.Append("},{");
                //        }
                //        RoomPax.Append("\"RoomType\":\"" + RoomCode[j] + "\",");
                //        RoomPax.Append("\"RoomRefId\":\"" + (j + 1) + "\",");
                //        RoomPax.Append("\"Paxes\":[{");
                //        for (int k = 0; k < dtnewpassanger1.DefaultView.Count; k++)
                //        {
                //            PaxId = PaxId + 1;
                //            if (k >= 1)
                //            {
                //                RoomPax.Append("},{");
                //            }
                //            RoomPax.Append("\"PaxId\":" + PaxId + "");
                //        }
                //        RoomPax.Append("}]");
                //    }
                //    MethaBook.Append("{");
                //    MethaBook.Append("\"bookingRq\":{");
                //    MethaBook.Append("\"Language\":" + "\"en\",");
                //    MethaBook.Append("\"ClientReference\":\"" + ClientReference + "\",");
                //    MethaBook.Append("\"Paxes\":[{");
                //    MethaBook.Append(Paxes.ToString());
                //    MethaBook.Append("}],");
                //    MethaBook.Append("\"Services\":{");
                //    MethaBook.Append("\"AccommodationServices\":[{");
                //    MethaBook.Append("\"BookingCode\":\"" + BookingCode + "\",");
                //    MethaBook.Append("\"AccommodationCode\":\"" + HotelCode + "\",");
                //    MethaBook.Append("\"CheckInDate\":\"" + CheckIn + "\",");
                //    MethaBook.Append("\"CheckOutDate\":\"" + CheckOut + "\",");
                //    MethaBook.Append("\"MealPlanCode\":\"" + MealCode + "\",");
                //    MethaBook.Append("\"Price\":{");
                //    MethaBook.Append("\"gross\":\"" + Gross + "\",");
                //    MethaBook.Append("\"currencyCode\":\"" + Currency + "\"");
                //    MethaBook.Append("},");
                //    MethaBook.Append("\"Rooms\":[{");
                //    MethaBook.Append(RoomPax.ToString());
                //    MethaBook.Append("}]");
                //    MethaBook.Append("}]");
                //    MethaBook.Append("}");
                //    MethaBook.Append("}");
                //    MethaBook.Append("}");

                    string MethaBookReq = MethaBook.ToString();
                    return MethaBookReq;
                //}
                //else
                //    return "";
            }
            catch (Exception ex)
            {
                _ = _exception.ErrorNew(ex, _LogAPI.Book);
                return "";
            }
        }

        public async Task<MethaBookBookingDetail> MethaBookBookingDetail(string ClientReference, int bookingStatus, string AgtVoucherNo, DataRow[] MethaBookDetails, DataSet dsCartDetails, int i, int TimeOut, string refno)
        {
            MethaBookBookingDetail methaBookBookingDetail = new MethaBookBookingDetail();
            try
            {
                string Url = Convert.ToString(MethaBookDetails[0]["WebSiteURL"]) + _supplierConfig.IBoosy.Bookingurl;
                string UserName = Convert.ToString(MethaBookDetails[0]["UserName"]);
                string Password = Convert.ToString(MethaBookDetails[0]["PassWord"]);
                bookingStatus = 1;
                string BookingStatus = "";
                string Request = "?clientReference=" + ClientReference + "";
                string Error = "";
                ResponseDto responseDto = await MethaBookResponseNew(Request, Url, UserName, Password, TimeOut, "GET");

                string BookingResponse = responseDto.Response;
                Error = responseDto.Error;

                if (BookingResponse == null || BookingResponse == "" || BookingResponse.ToLower().Contains("exceptions") == true)
                {
                    bookingStatus = 1;
                }
                else
                {
                    MethaBook.RootObject MethaBookResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<MethaBook.RootObject>(BookingResponse);
                    for (int b = 0; b < MethaBookResponse.bookingRs.bookings.Count; b++)
                    {
                        MethaBook.Booking drBooking = MethaBookResponse.bookingRs.bookings[b];
                        BookingStatus = Convert.ToString(drBooking.status);
                        AgtVoucherNo = Convert.ToString(drBooking.locator);


                    }
                    if (BookingStatus.ToLower() == "confirmed")
                        bookingStatus = 6;
                    else if (BookingStatus.ToLower() == "onrequest")
                    {
                        bookingStatus = await MethaBookCancellationProcess(AgtVoucherNo, MethaBookDetails, refno, dsCartDetails, bookingStatus);
                    }
                    else
                        bookingStatus = 2;
                }
            }
            catch (Exception ex)
            {
                _ = _exception.ErrorBooking(ex, "00");
            }

            methaBookBookingDetail.AgentVoucherNo = AgtVoucherNo;
            methaBookBookingDetail.bookingStatus = bookingStatus;
            return methaBookBookingDetail;
        }

        public async Task<MethaBookBookingDetail> MethaBookBookingDetail_newversion(string ClientReference, int bookingStatus, string AgtVoucherNo, DataRow[] MethaBookDetails, BookRequest_newversion bookRequest, int i, int TimeOut, string refno)
        {
            MethaBookBookingDetail methaBookBookingDetail = new MethaBookBookingDetail();
            try
            {
                string Url = Convert.ToString(MethaBookDetails[0]["WebSiteURL"]) + _supplierConfig.IBoosy.Bookingurl;
                string UserName = Convert.ToString(MethaBookDetails[0]["UserName"]);
                string Password = Convert.ToString(MethaBookDetails[0]["PassWord"]);
                bookingStatus = 1;
                string BookingStatus = "";
                string Request = "?clientReference=" + ClientReference + "";
                string Error = "";
                ResponseDto responseDto = await MethaBookResponseNew(Request, Url, UserName, Password, TimeOut, "GET");

                string BookingResponse = responseDto.Response;
                Error = responseDto.Error;

                if (BookingResponse == null || BookingResponse == "" || BookingResponse.ToLower().Contains("exceptions") == true)
                {
                    bookingStatus = 1;
                }
                else
                {
                    MethaBook.RootObject MethaBookResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<MethaBook.RootObject>(BookingResponse);
                    for (int b = 0; b < MethaBookResponse.bookingRs.bookings.Count; b++)
                    {
                        MethaBook.Booking drBooking = MethaBookResponse.bookingRs.bookings[b];
                        BookingStatus = Convert.ToString(drBooking.status);
                        AgtVoucherNo = Convert.ToString(drBooking.locator);


                    }
                    if (BookingStatus.ToLower() == "confirmed")
                        bookingStatus = 6;
                    else if (BookingStatus.ToLower() == "onrequest")
                    {
                        bookingStatus = 0;//await MethaBookCancellationProcess(AgtVoucherNo, MethaBookDetails, refno, dsCartDetails, bookingStatus);
                    }
                    else
                        bookingStatus = 2;
                }
            }
            catch (Exception ex)
            {
                _ = _exception.ErrorBooking(ex, "00");
            }

            methaBookBookingDetail.AgentVoucherNo = AgtVoucherNo;
            methaBookBookingDetail.bookingStatus = bookingStatus;
            return methaBookBookingDetail;
        }

        public async Task<int> MethaBookCancellationProcess(string AgentVoucheNo, DataRow[] MethaBookDetails, string rerefenceNo, DataSet dsCartDetails, int bookingStatus)
        {
            string CancelUrl = _supplierConfig.IBoosy.Cancel + AgentVoucheNo;
            _commonMethod.ThirdPartyCancellationHistory(AgentVoucheNo, ""/*Convert.ToString(HttpContext.Current.Session["UserName"])*/, Convert.ToString(MethaBookDetails[0]["WebSiteURL"]) + Convert.ToString(CancelUrl), "-", "Before Request hit booking", Convert.ToString(dsCartDetails.Tables[1].DefaultView[0]["SupplierName"]), "Request Confirm", rerefenceNo, "WithinEarth");
            string Error = "";
            ResponseDto responseDto = await MethaBookResponseNew(CancelUrl, Convert.ToString(MethaBookDetails[0]["WebSiteURL"]), Convert.ToString(MethaBookDetails[0]["UserName"]), Convert.ToString(MethaBookDetails[0]["Password"]), 60000, "PUT", "", false);
            string MBResponse = responseDto.Response;
            Error = responseDto.Error;

            if (MBResponse.ToLower().Contains("exceptions") == true || MBResponse.Trim() == string.Empty)
            {
                _commonMethod.ThirdPartyCancellationHistory(AgentVoucheNo, "" /*Convert.ToString(HttpContext.Current.Session["UserName"])*/, Convert.ToString(MethaBookDetails[0]["WebSiteURL"]) + Convert.ToString(CancelUrl), Convert.ToString(MBResponse), "From Cancellation Page", Convert.ToString(dsCartDetails.Tables[1].DefaultView[0]["SupplierName"]), "Hotel", rerefenceNo, "WithinEarth");
                bookingStatus = 1;
            }
            else
            {
                string CancelStatus = "";
                MethaBook.RootObject MBResponse1 = Newtonsoft.Json.JsonConvert.DeserializeObject<MethaBook.RootObject>(MBResponse);
                for (int c = 0; c < MBResponse1.bookingRs.bookings.Count; c++)
                {
                    MethaBook.Booking drBooking1 = MBResponse1.bookingRs.bookings[c];
                    CancelStatus = Convert.ToString(drBooking1.status);
                    if (CancelStatus.ToLower() == "cancelledbycustomer")
                    {
                        _commonMethod.ThirdPartyCancellationHistory(Convert.ToString(drBooking1.locator), "" /*Convert.ToString(HttpContext.Current.Session["UserName"])*/, Convert.ToString(MethaBookDetails[0]["WebSiteURL"]) + Convert.ToString(CancelUrl), Convert.ToString(MBResponse), "After Request hit booking", Convert.ToString(dsCartDetails.Tables[1].DefaultView[0]["SupplierName"]), "Request Confirm", rerefenceNo, "WithinEarth");
                        bookingStatus = 2;
                    }
                }
            }

            return bookingStatus;
        }
        public async Task<int> MethaBookCancellationProcess_newversion(string AgentVoucheNo, DataRow[] MethaBookDetails, string rerefenceNo, BookRequest_newversion bookRequest, int bookingStatus)
        {
           // string CancelUrl = "/Bookings/v1.0/Cancel/" + AgentVoucheNo;
            string CancelUrl = _supplierConfig.IBoosy.Cancel + AgentVoucheNo;
            _commonMethod.ThirdPartyCancellationHistory(AgentVoucheNo, ""/*Convert.ToString(HttpContext.Current.Session["UserName"])*/, Convert.ToString(MethaBookDetails[0]["WebSiteURL"]) + Convert.ToString(CancelUrl), "-", "Before Request hit booking", Convert.ToString(bookRequest.userCart.UserCartDetails[0].SupplierName), "Request Confirm", rerefenceNo, "WithinEarth");
            string Error = "";
            ResponseDto responseDto = await MethaBookResponseNew(CancelUrl, Convert.ToString(MethaBookDetails[0]["WebSiteURL"]), Convert.ToString(MethaBookDetails[0]["UserName"]), Convert.ToString(MethaBookDetails[0]["Password"]), 60000, "PUT", "", false);
            string MBResponse = responseDto.Response;
            Error = responseDto.Error;

            if (MBResponse.ToLower().Contains("exceptions") == true || MBResponse.Trim() == string.Empty)
            {
                _commonMethod.ThirdPartyCancellationHistory(AgentVoucheNo, "" /*Convert.ToString(HttpContext.Current.Session["UserName"])*/, Convert.ToString(MethaBookDetails[0]["WebSiteURL"]) + Convert.ToString(CancelUrl), Convert.ToString(MBResponse), "From Cancellation Page", Convert.ToString(bookRequest.userCart.UserCartDetails[0].SupplierName), "Hotel", rerefenceNo, "WithinEarth");
                bookingStatus = 1;
            }
            else
            {
                string CancelStatus = "";
                MethaBook.RootObject MBResponse1 = Newtonsoft.Json.JsonConvert.DeserializeObject<MethaBook.RootObject>(MBResponse);
                for (int c = 0; c < MBResponse1.bookingRs.bookings.Count; c++)
                {
                    MethaBook.Booking drBooking1 = MBResponse1.bookingRs.bookings[c];
                    CancelStatus = Convert.ToString(drBooking1.status);
                    if (CancelStatus.ToLower() == "cancelledbycustomer")
                    {
                        _commonMethod.ThirdPartyCancellationHistory(Convert.ToString(drBooking1.locator), "" /*Convert.ToString(HttpContext.Current.Session["UserName"])*/, Convert.ToString(MethaBookDetails[0]["WebSiteURL"]) + Convert.ToString(CancelUrl), Convert.ToString(MBResponse), "After Request hit booking", Convert.ToString(bookRequest.userCart.UserCartDetails[0].SupplierName), "Request Confirm", rerefenceNo, "WithinEarth");
                        bookingStatus = 2;
                    }
                }
            }

            return bookingStatus;
        }
    }
}
