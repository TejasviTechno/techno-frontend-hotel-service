using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using XConnect.CommonUtilities.Interface;
using XConnect.CommonUtilities;
using XConnect.ErrorLogHandler.Interface;
using XConnect.Markup.Interface;
using XConnect.Models.Hotel;
using XConnect.Models;
using Options = XConnect.Models.Options;
using XConnect.Supplier.Booking.Com.Interface;
using static Azure.Core.HttpHeader;
using System.Net;
using Newtonsoft.Json;
using Microsoft.IdentityModel.Protocols;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;
using XConnect.DAL;
using static XConnect.Models.ErrorLog;
using System.Data.Common;

namespace XConnect.Supplier.Booking.Com
{
    public class BookingDotComRepo : IBooking
    {
        private readonly ICommonMethod _commonMethod;
        private readonly IVCN _vcn;
        private readonly IException _exception;
        private readonly IMarkup _markup;
        private readonly ISupplierRes _supplierRes;
        private readonly KeySettings _keySettings;

        public BookingDotComRepo(ICommonMethod commonMethod, IException exception, IMarkup markup, ISupplierRes supplierRes, IOptions<KeySettings> keySettings, IVCN vcn)
        {
            _commonMethod = commonMethod;
            _exception = exception;
            _markup = markup;
            _supplierRes = supplierRes;
            _keySettings = keySettings.Value;
            _vcn = vcn;
        }

        public void GetBookingDotComRequestOnly_async(DataRow[] commonCode, List<StarRating> listRating, string SupplierName, HotelSearchParameter SearchPara, List<SupplierDetails> bookingDetails, List<SupplierSearchConfig> bookingSearchConfig, DataTable dtPreferredDataHotel, ref ConcurrentBag<HotelX> AllTask, string XMLCity, string XMLCountry, string Xmlnationality, int Hit, CommonSearchList commonSearchList, DataTable dtROE, TokenData tokenData, double BookingDotComCommison, bool SearchByOneID = true)
        {
            try
            {
                string Ids = string.Empty;
                string xmlbase = "";
                _commonMethod.GetPrefferedHotelCodes(ref Ids, ref xmlbase, ref bookingSearchConfig, "booking.com", dtPreferredDataHotel, SearchPara.IsSearchFor);
                if (bookingSearchConfig != null)
                {

                    string bookingDotComCurrency = Convert.ToString(bookingDetails[0].CurrencyShortName);
                    double CurrencyFactorbooking = 1;

                    if (bookingDotComCurrency.ToLower() != tokenData.SiteCurrency.ToLower())
                    {
                        CurrencyFactorbooking = _commonMethod.GetROEByCurrency(bookingDotComCurrency, dtROE);// Convert.ToDouble(dtInrCurrency.Rows[0]["AmtToExch"]);
                    }


                    for (int k = 0; k < bookingSearchConfig.Count(); k++)
                    {
                        bool DebugMode = (Convert.ToInt32(bookingSearchConfig[k].DebugMode) == 0 ? false : true);

                        {
                            //List<HotelCodeByCity> DtHotelDAta = FilterFinalList(commonCode, listRating);
                            if (commonCode != null)
                            {

                                if (Convert.ToInt32(bookingSearchConfig[k].CityID) == 0 || Convert.ToInt32(bookingSearchConfig[k].CityID) == SearchPara.CityID)
                                {
                                    string Rating = _commonMethod.GetStarRatingString(SearchPara);
                                    int Noofrecords = Convert.ToInt32(bookingSearchConfig[k].Noofrecords);
                                    if (SearchPara.Rooms[0].Child1Age != 1 || SearchPara.Rooms[0].Child2Age != 1)
                                    {
                                        bool IsSame = true;
                                        int childAgeCount = 0; int AdultCount = 0, ChildCount = 0;
                                        for (int i = 0; i < SearchPara.Rooms.Count; i++)
                                        {
                                            int NoOfAdt = SearchPara.Rooms[0].NoofAdults;
                                            int NoOfChd = SearchPara.Rooms[0].NoofChild;
                                            int ChildAge1 = SearchPara.Rooms[0].Child1Age;
                                            int ChildAge2 = SearchPara.Rooms[0].Child2Age;
                                            if (NoOfAdt != SearchPara.Rooms[i].NoofAdults || NoOfChd != SearchPara.Rooms[i].NoofChild || ChildAge1 != SearchPara.Rooms[i].Child1Age || ChildAge2 != SearchPara.Rooms[i].Child2Age)
                                                IsSame = false;

                                            if (SearchPara.Rooms[i].Child1Age == 1)
                                            {
                                                childAgeCount += SearchPara.Rooms[i].Child1Age;
                                            }
                                            if (SearchPara.Rooms[i].Child2Age == 1)
                                            {
                                                childAgeCount += SearchPara.Rooms[i].Child2Age;
                                            }
                                            AdultCount += SearchPara.Rooms[i].NoofAdults;
                                            ChildCount += SearchPara.Rooms[i].NoofChild;

                                        }
                                        if (childAgeCount > 1)
                                            IsSame = false;
                                        if (AdultCount > 9)
                                            IsSame = false;
                                        if (ChildCount > 9)
                                            IsSame = false;

                                        //if (IsSame)
                                        //if (commonCode != null)
                                        {

                                            if (SearchPara.Filters.IsShowRooms == 1 && commonSearchList.SearchByHotel)
                                            {

                                                DataTable[] splittedtables = commonCode.AsEnumerable()
                                                                     .Select((row, index) => new { row, index })
                                                                     //.GroupBy(x => x.index / Noofrecords)  // integer division, the fractional part is truncated
                                                                     .GroupBy(x => x.index / 1)  // no of record set to 1 as they not suppout multiple ID.
                                                                     .Select(g => g.Select(x => x.row).CopyToDataTable())
                                                                     .ToArray();
                                                for (int a = 0; a < splittedtables.Length; a++)
                                                {
                                                    //if (a > 10)
                                                    if (a > 0)
                                                        break;
                                                    var paramValues1 = String.Join(",", splittedtables[a].Select().Select(x => String.Join(",", Convert.ToString(x.ItemArray[0]))));
                                                    string IdsForSearch = Convert.ToString(paramValues1);
                                                    HotelX xhotel = new HotelX();
                                                    xhotel.Supplier = "booking.com";
                                                    xhotel.DebugeMode = DebugMode;
                                                    xhotel.SearchTimeOut = Convert.ToInt32(bookingSearchConfig[k].HotelSearchTimeOut);
                                                    //if (Hit > 0)
                                                    xhotel.ProcessTimeout = Convert.ToInt32(bookingSearchConfig[k].HotelSearchTimeOut2);
                                                    xhotel.Commission = BookingDotComCommison;
                                                    xhotel.URL = Convert.ToString(bookingDetails[0].WebSiteURL);
                                                    xhotel.UserId = Convert.ToString(bookingDetails[0].UserID);
                                                    xhotel.UserName = Convert.ToString(bookingDetails[0].UserName);
                                                    xhotel.PassWord = Convert.ToString(bookingDetails[0].Password);
                                                    xhotel.CurrencyCode = Convert.ToString(bookingDotComCurrency);
                                                    xhotel.CurrencyFactor = CurrencyFactorbooking;
                                                    xhotel.XmlCity = XMLCity;
                                                    xhotel.XmlCountry = XMLCountry;
                                                    xhotel.XmlNationality = Xmlnationality;
                                                    xhotel.AgentWiseMarkup = bookingDetails[0].AgentWiseMarkup;
                                                    xhotel.IsShowSupplier = Convert.ToInt16(bookingDetails[0].IsShowSupplier) == 1 ? true : false;
                                                    xhotel.SupplierID = bookingDetails[0].SupplierID;
                                                    xhotel.MasterSupplierID = bookingDetails[0].MasterSupplierId;
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
                                                        objrq.Request = objrq.Request = Get_BookingDotCom_PriceOnlyRoom(xhotel.URL, SearchPara, xhotel.UserName, xhotel.PassWord, xhotel.XmlCity, IdsForSearch, Convert.ToString(xhotel.CurrencyCode), Convert.ToString(xhotel.hotelRating), xhotel.XmlNationality);

                                                        //if (SearchByOneID)
                                                        //   objrq.Request = objrq.Request = Common.Get_BookingDotCom_PriceOnlyRoom(xhotel.URL, SearchPara, xhotel.UserName, xhotel.PassWord, xhotel.XmlCity, IdsForSearch, Convert.ToString(xhotel.CurrencyCode), Convert.ToString(xhotel.hotelRating), xhotel.XmlNationality);
                                                        //else
                                                        //   objrq.Request = Common.XMLBookingDotComRequestOnly_async(xhotel.URL, xhotel.UserName, xhotel.PassWord, xhotel.SearchTimeOut, SearchPara, IdsForSearch, xhotel.CurrencyCode, DebugMode, XMLCity, Convert.ToString(xhotel.hotelRating), xhotel.XmlNationality);

                                                        objrq.Response = ""; RQRS.Add(objrq);
                                                    }
                                                    xhotel.RQRS = RQRS;
                                                    #endregion
                                                    AllTask.Add(xhotel);
                                                }
                                            }
                                            else
                                            {
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
                                                    xhotel.Supplier = "booking.com";
                                                    xhotel.DebugeMode = DebugMode;
                                                    xhotel.SearchTimeOut = Convert.ToInt32(bookingSearchConfig[k].HotelSearchTimeOut);
                                                    //if (Hit > 0)
                                                    xhotel.ProcessTimeout = Convert.ToInt32(bookingSearchConfig[k].HotelSearchTimeOut2);
                                                    xhotel.Commission = BookingDotComCommison;
                                                    xhotel.URL = Convert.ToString(bookingDetails[0].WebSiteURL);
                                                    xhotel.UserId = Convert.ToString(bookingDetails[0].UserID);
                                                    xhotel.UserName = Convert.ToString(bookingDetails[0].UserName);
                                                    xhotel.PassWord = Convert.ToString(bookingDetails[0].Password);
                                                    xhotel.CurrencyCode = Convert.ToString(bookingDotComCurrency);
                                                    xhotel.CurrencyFactor = CurrencyFactorbooking;
                                                    xhotel.XmlCity = XMLCity;
                                                    xhotel.XmlCountry = XMLCountry;
                                                    xhotel.XmlNationality = Xmlnationality;
                                                    xhotel.AgentWiseMarkup = bookingDetails[0].AgentWiseMarkup;
                                                    xhotel.IsShowSupplier = Convert.ToInt16(bookingDetails[0].IsShowSupplier) == 1 ? true : false;
                                                    xhotel.SupplierID = bookingDetails[0].SupplierID;
                                                    xhotel.MasterSupplierID = bookingDetails[0].MasterSupplierId;
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
                                                        //objrq.Request = Hotel.XMLBookingDotComRequestOnly_async(xhotel.URL, xhotel.UserName, xhotel.PassWord, xhotel.SearchTimeOut, SearchPara, IdsForSearch, xhotel.CurrencyCode, DebugMode, XMLCity, Rating, xhotel.XmlNationality);
                                                        //objrq.Request = objrq.Request = Common.Get_BookingDotCom_PriceOnlyRoom(xhotel.URL, SearchPara, xhotel.UserName, xhotel.PassWord, xhotel.XmlCity, IdsForSearch, Convert.ToString(xhotel.CurrencyCode), Convert.ToString(xhotel.hotelRating), xhotel.XmlNationality);

                                                        if (SearchByOneID)
                                                            objrq.Request = objrq.Request = Get_BookingDotCom_PriceOnlyRoom(xhotel.URL, SearchPara, xhotel.UserName, xhotel.PassWord, xhotel.XmlCity, IdsForSearch, Convert.ToString(xhotel.CurrencyCode), Convert.ToString(xhotel.hotelRating), xhotel.XmlNationality);
                                                        else
                                                            objrq.Request = XMLBookingDotComRequestOnly_async(xhotel.URL, xhotel.UserName, xhotel.PassWord, xhotel.SearchTimeOut, SearchPara, IdsForSearch, xhotel.CurrencyCode, DebugMode, XMLCity, Convert.ToString(xhotel.hotelRating), xhotel.XmlNationality);

                                                        objrq.Response = ""; RQRS.Add(objrq);
                                                    }
                                                    xhotel.RQRS = RQRS;
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
            catch (Exception ex)
            {
                _exception.Error(ex);
            }
        }

        public void GetBookingDotComRequestOnly_async_new(DataRow[] commonCode, List<StarRating> listRating, HotelSearchParameter SearchPara, List<SupplierDetails> lstbookingDetails, List<SupplierSearchConfig> lstbookingSearchConfig, DataTable dtPreferredDataHotel, ref ConcurrentBag<HotelX> AllTask, int Hit, CommonSearchList commonSearchList, DataTable dtROE, TokenData tokenData, double BookingDotComCommison, DataSet dtXMLCity, bool SearchByOneID = true, bool HitFromAll = false)
        {
            try
            {
                if (lstbookingDetails != null && lstbookingDetails.Count > 0)
                {
                    List<SupplierDetails> BookingMain = new List<SupplierDetails>();
                    List<SupplierDetails> BookingChild = new List<SupplierDetails>();
                    List<StarRating> lstRating = new List<StarRating>();
                    lstRating = _commonMethod.GetStarRatingStringList_xtg(SearchPara);

                    List<HotelCodeByCity> DtHotelDAta = HtmlExtensions.ToList<HotelCodeByCity>(commonCode.CopyToDataTable());
                    int ratingcount = lstRating.Count;
                    int hotelcount = DtHotelDAta.Count;


                    foreach (SupplierDetails bookingDetails in lstbookingDetails)
                    {
                        if (bookingDetails != null)
                        {
                            int SupplierID = bookingDetails.SupplierID;
                            string Supplier = bookingDetails.SupplierName;

                            if (commonCode != null)
                            {
                                if ((SearchPara.IsSearchFor.ToLower().IndexOf("booking.com") >= 0 || SearchPara.IsSearchFor.ToLower().IndexOf("multi") >= 0) && HitFromAll)
                                {
                                    DataForSearch dataForSearch = _commonMethod.GetXmlDataForSearchTechwise(SearchPara.Nationality, dtXMLCity, Supplier, "booking.com");
                                    bool HitSupplier = false;

                                    HitSupplier = true;
                                    for (int i = 0; i < SearchPara.Rooms.Count; i++)
                                    {
                                        int NoOfAdt = SearchPara.Rooms[i].NoofAdults;
                                        if (NoOfAdt > 8)
                                            HitSupplier = false;

                                        try // added on 31-07-2024 as per mustafa sir
                                        {
                                            if (SearchPara.CountryID > 0)
                                            {
                                                if (SearchPara.CountryID == 15406)//japan
                                                {
                                                    if (SearchPara.Rooms[i].NoofChild > 0)
                                                    {
                                                        HitSupplier = false;
                                                        break;
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                if (SearchPara.CountryCode.ToLower() == "japan")
                                                {
                                                    if (SearchPara.Rooms[i].NoofChild > 0)
                                                    {
                                                        HitSupplier = false;
                                                        break;
                                                    }
                                                }
                                            }
                                        }
                                        catch (Exception)
                                        {

                                        }
                                    }

                                    List<HotelCodeByCity> filter = new List<HotelCodeByCity>();

                                    filter = _commonMethod.FilterWithOutCity_Code(ratingcount, hotelcount, DtHotelDAta, lstRating, (Supplier == "booking.com" || Supplier == "booking.com_v3") ? "booking.com" : Supplier);


                                    if (HitSupplier && dataForSearch.XmlNationality != "" && filter.Count > 0 && SearchPara.PastDate == false)// added on 8-AUG-2024
                                    {
                                        string StrNationality = dataForSearch.XmlNationality;
                                        if (SearchPara.Nationality.ToLower() == "china" || SearchPara.Nationality.ToLower() == "russia" || SearchPara.Nationality.ToLower() == "iran" || SearchPara.Nationality.ToLower() == "syria")
                                            StrNationality = "US";// as per Mustafa sir ( API group 30-Jan-2024 )

                                        if (lstbookingSearchConfig.Count > 0)
                                        {

                                            List<SupplierSearchConfig> bookingSearchConfig = lstbookingSearchConfig
                                         .Where(x => x != null && x.SupplierID == SupplierID)
                                         .ToList();


                                            if (bookingSearchConfig != null && bookingSearchConfig.Count > 0)
                                            {

                                                string Ids = string.Empty;
                                                string xmlbase = "";
                                                _commonMethod.GetPrefferedHotelCodes(ref Ids, ref xmlbase, ref bookingSearchConfig, Supplier, dtPreferredDataHotel, SearchPara.IsSearchFor);


                                                if (filter.Count > 0)
                                                {


                                                    string bookingDotComCurrency = Convert.ToString(bookingDetails.CurrencyShortName);
                                                    double CurrencyFactorbooking = 1;

                                                    if (bookingDotComCurrency.ToLower() != tokenData.SiteCurrency.ToLower())
                                                    {
                                                        CurrencyFactorbooking = _commonMethod.GetROEByCurrency(bookingDotComCurrency, dtROE);// Convert.ToDouble(dtInrCurrency.Rows[0]["AmtToExch"]);
                                                    }


                                                    for (int k = 0; k < bookingSearchConfig.Count(); k++)
                                                    {
                                                        bool DebugMode = (Convert.ToInt32(bookingSearchConfig[k].DebugMode) == 0 ? false : true);

                                                        {
                                                            //List<HotelCodeByCity> DtHotelDAta = FilterFinalList(commonCode, listRating);
                                                            //if (commonCode != null)
                                                            {

                                                                if (Convert.ToInt32(bookingSearchConfig[k].CityID) == 0 || Convert.ToInt32(bookingSearchConfig[k].CityID) == SearchPara.CityID)
                                                                {
                                                                    string Rating = _commonMethod.GetStarRatingString(SearchPara);
                                                                    int Noofrecords = Convert.ToInt32(bookingSearchConfig[k].Noofrecords);

                                                                    if (SearchPara.Rooms[0].Child1Age != 1 || SearchPara.Rooms[0].Child2Age != 1)
                                                                    {
                                                                        #region Not Used

                                                                        //bool IsSame = true;
                                                                        //int childAgeCount = 0; int AdultCount = 0, ChildCount = 0;
                                                                        //for (int i = 0; i < SearchPara.Rooms.Count; i++)
                                                                        //{
                                                                        //    int NoOfAdt = SearchPara.Rooms[0].NoofAdults;
                                                                        //    int NoOfChd = SearchPara.Rooms[0].NoofChild;
                                                                        //    int ChildAge1 = SearchPara.Rooms[0].Child1Age;
                                                                        //    int ChildAge2 = SearchPara.Rooms[0].Child2Age;
                                                                        //    if (NoOfAdt != SearchPara.Rooms[i].NoofAdults || NoOfChd != SearchPara.Rooms[i].NoofChild || ChildAge1 != SearchPara.Rooms[i].Child1Age || ChildAge2 != SearchPara.Rooms[i].Child2Age)
                                                                        //        IsSame = false;

                                                                        //    if (SearchPara.Rooms[i].Child1Age == 1)
                                                                        //    {
                                                                        //        childAgeCount += SearchPara.Rooms[i].Child1Age;
                                                                        //    }
                                                                        //    if (SearchPara.Rooms[i].Child2Age == 1)
                                                                        //    {
                                                                        //        childAgeCount += SearchPara.Rooms[i].Child2Age;
                                                                        //    }
                                                                        //    AdultCount += SearchPara.Rooms[i].NoofAdults;
                                                                        //    ChildCount += SearchPara.Rooms[i].NoofChild;

                                                                        //}
                                                                        //if (childAgeCount > 1)
                                                                        //    IsSame = false;
                                                                        //if (AdultCount > 9)
                                                                        //    IsSame = false;
                                                                        //if (ChildCount > 9)
                                                                        //    IsSame = false;

                                                                        ////if (IsSame)
                                                                        #endregion
                                                                        //if (commonCode != null)
                                                                        {

                                                                            if (SearchPara.Filters.IsShowRooms == 1 && commonSearchList.SearchByHotel)
                                                                            {

                                                                                int NoOfRecord = Convert.ToInt32(bookingSearchConfig[k].Noofrecords);
                                                                                double count = Convert.ToDouble(filter.Count);
                                                                                int NoOfDiv = Convert.ToInt32(Math.Ceiling(count / Convert.ToDouble(NoOfRecord)));
                                                                                List<List<XMLID_Search>> lstxml = _commonMethod.GetXMLIDString(NoOfDiv, NoOfRecord, count, filter);

                                                                                for (int a = 0; a < NoOfDiv; a++)
                                                                                {
                                                                                    //if (a > 10)
                                                                                    if (a > 0)
                                                                                        break;
                                                                                    string IdsForSearch = Convert.ToString(_commonMethod.GetHotelCodeByCOmma(lstxml[a]));

                                                                                    //var paramValues1 = String.Join(",", splittedtables[a].Select().Select(x => String.Join(",", Convert.ToString(x.ItemArray[0]))));
                                                                                    //string IdsForSearch = Convert.ToString(paramValues1);
                                                                                    HotelX xhotel = new HotelX();
                                                                                    xhotel.Supplier = Supplier;
                                                                                    xhotel.DebugeMode = DebugMode;
                                                                                    xhotel.SearchTimeOut = Convert.ToInt32(bookingSearchConfig[k].HotelSearchTimeOut);
                                                                                    //if (Hit > 0)
                                                                                    xhotel.ProcessTimeout = Convert.ToInt32(bookingSearchConfig[k].HotelSearchTimeOut2);
                                                                                    xhotel.Commission = BookingDotComCommison;
                                                                                    //xhotel.URL = Convert.ToString(bookingDetails.WebSiteURL);

                                                                                    if (_keySettings.SupplierProxy == "0")
                                                                                        xhotel.URL = Convert.ToString(bookingDetails.WebSiteURL);
                                                                                    else
                                                                                        xhotel.URL = Convert.ToString(bookingDetails.WebSiteURLforAPI);

                                                                                    xhotel.UserId = Convert.ToString(bookingDetails.UserID);
                                                                                    xhotel.UserName = Convert.ToString(bookingDetails.UserName);
                                                                                    xhotel.PassWord = Convert.ToString(bookingDetails.Password);
                                                                                    xhotel.CurrencyCode = Convert.ToString(bookingDotComCurrency);
                                                                                    xhotel.CurrencyFactor = CurrencyFactorbooking;
                                                                                    xhotel.XmlCity = dataForSearch.xmlCity;
                                                                                    xhotel.XmlCountry = dataForSearch.xmlCountry;
                                                                                    xhotel.XmlNationality = StrNationality;
                                                                                    xhotel.AgentWiseMarkup = bookingDetails.AgentWiseMarkup;
                                                                                    xhotel.IsShowSupplier = Convert.ToInt16(bookingDetails.IsShowSupplier) == 1 ? true : false;
                                                                                    xhotel.SupplierID = bookingDetails.SupplierID;
                                                                                    xhotel.MasterSupplierID = bookingDetails.MasterSupplierId;
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
                                                                                        objrq.Request = objrq.Request = Get_BookingDotCom_PriceOnlyRoom(xhotel.URL, SearchPara, xhotel.UserName, xhotel.PassWord, xhotel.XmlCity, IdsForSearch, Convert.ToString(xhotel.CurrencyCode), Convert.ToString(xhotel.hotelRating), xhotel.XmlNationality);

                                                                                        //if (SearchByOneID)
                                                                                        //   objrq.Request = objrq.Request = Common.Get_BookingDotCom_PriceOnlyRoom(xhotel.URL, SearchPara, xhotel.UserName, xhotel.PassWord, xhotel.XmlCity, IdsForSearch, Convert.ToString(xhotel.CurrencyCode), Convert.ToString(xhotel.hotelRating), xhotel.XmlNationality);
                                                                                        //else
                                                                                        //   objrq.Request = Common.XMLBookingDotComRequestOnly_async(xhotel.URL, xhotel.UserName, xhotel.PassWord, xhotel.SearchTimeOut, SearchPara, IdsForSearch, xhotel.CurrencyCode, DebugMode, XMLCity, Convert.ToString(xhotel.hotelRating), xhotel.XmlNationality);

                                                                                        objrq.Response = ""; RQRS.Add(objrq);
                                                                                    }
                                                                                    xhotel.RQRS = RQRS;
                                                                                    xhotel.ReturnRooms = bookingDetails.ReturnRooms;
                                                                                    xhotel.TechName = bookingDetails.TechName;
                                                                                    #endregion
                                                                                    AllTask.Add(xhotel);
                                                                                }
                                                                            }
                                                                            else
                                                                            {
                                                                                int NoOfRecord = Convert.ToInt32(bookingSearchConfig[k].Noofrecords);
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
                                                                                    xhotel.SearchTimeOut = Convert.ToInt32(bookingSearchConfig[k].HotelSearchTimeOut);
                                                                                    //if (Hit > 0)
                                                                                    xhotel.ProcessTimeout = Convert.ToInt32(bookingSearchConfig[k].HotelSearchTimeOut2);
                                                                                    xhotel.Commission = BookingDotComCommison;
                                                                                    //xhotel.URL = Convert.ToString(bookingDetails.WebSiteURL);

                                                                                    if (_keySettings.SupplierProxy == "0")
                                                                                        xhotel.URL = Convert.ToString(bookingDetails.WebSiteURL);
                                                                                    else
                                                                                        xhotel.URL = Convert.ToString(bookingDetails.WebSiteURLforAPI);

                                                                                    xhotel.UserId = Convert.ToString(bookingDetails.UserID);
                                                                                    xhotel.UserName = Convert.ToString(bookingDetails.UserName);
                                                                                    xhotel.PassWord = Convert.ToString(bookingDetails.Password);
                                                                                    xhotel.CurrencyCode = Convert.ToString(bookingDotComCurrency);
                                                                                    xhotel.CurrencyFactor = CurrencyFactorbooking;
                                                                                    xhotel.XmlCity = dataForSearch.xmlCity;
                                                                                    xhotel.XmlCountry = dataForSearch.xmlCountry;
                                                                                    xhotel.XmlNationality = StrNationality;
                                                                                    xhotel.AgentWiseMarkup = bookingDetails.AgentWiseMarkup;
                                                                                    xhotel.IsShowSupplier = Convert.ToInt16(bookingDetails.IsShowSupplier) == 1 ? true : false;
                                                                                    xhotel.SupplierID = bookingDetails.SupplierID;
                                                                                    xhotel.MasterSupplierID = bookingDetails.MasterSupplierId;
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
                                                                                        //objrq.Request = Hotel.XMLBookingDotComRequestOnly_async(xhotel.URL, xhotel.UserName, xhotel.PassWord, xhotel.SearchTimeOut, SearchPara, IdsForSearch, xhotel.CurrencyCode, DebugMode, XMLCity, Rating, xhotel.XmlNationality);
                                                                                        //objrq.Request = objrq.Request = Common.Get_BookingDotCom_PriceOnlyRoom(xhotel.URL, SearchPara, xhotel.UserName, xhotel.PassWord, xhotel.XmlCity, IdsForSearch, Convert.ToString(xhotel.CurrencyCode), Convert.ToString(xhotel.hotelRating), xhotel.XmlNationality);

                                                                                        if (SearchByOneID)
                                                                                            objrq.Request = objrq.Request = Get_BookingDotCom_PriceOnlyRoom(xhotel.URL, SearchPara, xhotel.UserName, xhotel.PassWord, xhotel.XmlCity, IdsForSearch, Convert.ToString(xhotel.CurrencyCode), Convert.ToString(xhotel.hotelRating), xhotel.XmlNationality);
                                                                                        else
                                                                                            objrq.Request = XMLBookingDotComRequestOnly_async(xhotel.URL, xhotel.UserName, xhotel.PassWord, xhotel.SearchTimeOut, SearchPara, IdsForSearch, xhotel.CurrencyCode, DebugMode, dataForSearch.xmlCity, Convert.ToString(xhotel.hotelRating), xhotel.XmlNationality);

                                                                                        objrq.Response = ""; RQRS.Add(objrq);
                                                                                    }
                                                                                    xhotel.RQRS = RQRS;
                                                                                    xhotel.ReturnRooms = bookingDetails.ReturnRooms;
                                                                                    xhotel.TechName = bookingDetails.TechName;
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
        public string Get_BookingDotCom_PriceOnlyRoom(string Url, HotelSearchParameter SearchParam, string UserName, string Password, string destination, string HotelIds, string CurrencyCode, string Rating, string Nationality)
        {
            try
            {
                #region Room Only 
                string checkIn = SearchParam.CheckInDate;
                int rooms = Convert.ToInt32(SearchParam.Rooms.Count);
                string adults = "";
                string childage = "";
                checkIn = checkIn.Substring(6, 4) + "-" + checkIn.Substring(3, 2) + "-" + checkIn.Substring(0, 2);
                string checkOut = SearchParam.CheckOutDate;
                checkOut = checkOut.Substring(6, 4) + "-" + checkOut.Substring(3, 2) + "-" + checkOut.Substring(0, 2);
                StringBuilder req = new StringBuilder();
                req.Append(Url + "blockAvailability?");
                req.Append("hotel_ids=" + HotelIds);
                req.Append("&checkin=" + checkIn);//yyyy-mm-dd
                req.Append("&checkout=" + checkOut);
                req.Append("&show_test=" + Convert.ToString(BookingInfo.Bookingshow_test));
                //req.Append("&guest_cc=nl");
                req.Append("&guest_cc=" + Nationality);
                // req.Append("&extras=cancellation_info,policies,room_type_id,mealplans");.
                req.Append("&extras=max_children_free,max_children_free_age,");
                //req.Append("&extras=max_children_free,max_children_free_ageadditional_room_info,cancellation_info,policies,room_type_id,mealplans,deal_flash,deal_lastm,deal_smart,extra_beds,extra_charges,facilities,important_information,payment_terms,");
                //req.Append("&affiliate_id=" + Convert.ToString(ConfigurationManager.AppSettings["BookingDotComAffiliateID"]) + "");
                req.Append("&limit_incremental_prices=1");
                //req.Append("&stars=" + Rating);
                //req.Append("&detail_level=1");
                for (int i = 1; i <= rooms; i++)
                {
                    childage = "";
                    for (int a = 0; a < SearchParam.Rooms[i - 1].NoofAdults; a++)
                    {
                        if (a == 0)
                            adults = "A";
                        else
                            adults = adults + ",A";
                    }
                    switch (Convert.ToInt32(SearchParam.Rooms[i - 1].NoofChild))
                    {
                        case 1:
                            childage = "," + SearchParam.Rooms[i - 1].Child1Age;
                            break;
                        case 2:
                            childage = "," + SearchParam.Rooms[i - 1].Child1Age + "," + SearchParam.Rooms[i - 1].Child2Age;
                            break;
                        default:
                            break;
                    }
                    req.Append("&room" + i + "=" + adults + childage);
                    //req.Append("&user_platform=mobile"); //24-07-2024
                    ////req.Append("&user_platform=mobile-app");//Added on 21-JUN-2024 by mustafa sir

                }
                req.Append("&user_platform=mobile"); //24-07-2024
                                                     //req.Append("&user_platform=mobile-app");//Added on 21-JUN-2024 by mustafa sir
                return req.ToString();
                #endregion
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
                return "";
            }
            return string.Empty;
        }

        public string XMLBookingDotComRequestOnly_async(string Url, string UserName, string Password, Int32 SearchTimeOut, HotelSearchParameter SearchParam, string HotelIds, string CurrencyCode, bool DebugMode, string CityCode, string Rating, string Nationality)
        {
            try
            {
                #region PriceOnly RQ
                string BDCPriceOnly = Get_BookingDotCom_PriceOnly_v1(Url, SearchParam, UserName, Password, CityCode, HotelIds, CurrencyCode, Rating, Nationality);
                return BDCPriceOnly;
                #endregion
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
                return "";
            }
        }

        public string Get_BookingDotCom_PriceOnly_v1(string Url, HotelSearchParameter SearchParam, string UserName, string Password, string destination, string HotelIds, string CurrencyCode, string Rating, string Nationality)
        {
            try
            {
                #region Price Only 
                int rooms = Convert.ToInt32(SearchParam.Rooms.Count);
                string checkIn = SearchParam.CheckInDate;
                checkIn = checkIn.Substring(6, 4) + "-" + checkIn.Substring(3, 2) + "-" + checkIn.Substring(0, 2);
                string checkOut = SearchParam.CheckOutDate;
                checkOut = checkOut.Substring(6, 4) + "-" + checkOut.Substring(3, 2) + "-" + checkOut.Substring(0, 2);
                string adults = "";
                string childage = "";
                StringBuilder req = new StringBuilder();
                req.Append(Url + "hotelAvailability?");
                req.Append("checkin=" + checkIn);//yyyy-mm-dd
                req.Append("&checkout=" + checkOut);
                if (HotelIds == "")
                    req.Append("&city_ids=" + destination);
                else
                    req.Append("&hotel_ids=" + HotelIds);
                //req.Append("language=en");
                for (int i = 1; i <= rooms; i++)
                {
                    childage = "";
                    for (int a = 0; a < SearchParam.Rooms[i - 1].NoofAdults; a++)
                    {
                        if (a == 0)
                            adults = "A";
                        else
                            adults = adults + ",A";
                    }
                    switch (Convert.ToInt32(SearchParam.Rooms[i - 1].NoofChild))
                    {
                        case 1:
                            childage = "," + SearchParam.Rooms[i - 1].Child1Age;
                            break;
                        case 2:
                            childage = "," + SearchParam.Rooms[i - 1].Child1Age + "," + SearchParam.Rooms[i - 1].Child2Age;
                            break;
                        default:
                            break;
                    }
                    req.Append("&room" + i + "=" + adults + childage);
                }
                req.Append("&guest_country=" + Nationality);
                req.Append("&extras=hotel_details,room_details");
                //req.Append("&stars=" + Rating);
                //req.Append("&options=show_test");

                //req.Append("&affiliate_id=" + Convert.ToString(ConfigurationManager.AppSettings["BookingDotComAffiliateID"]) + "");
                return req.ToString();
                #endregion
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
            }
            return string.Empty;
        }

        public async Task<List<Options>> BookingDotCom_HotelSearchWithRooms(HotelX Item, CommonSearch commonSearch, string WebSiteURL, int SearchTimeOut, string Supplier, DataTable dtAgentDetail, DataTable dtSuplierMarkup, DataTable dtMarkupDetails, bool DebugeMode, string UserName, string PassWord, string MethoodType, string ResponseData, HotelSearchParameter SearchParam, string xmlcity, string xmlcountry, string xmlnationality, string currencyCode, double currencyfactor, AdvancedOptions advancedOptions, Int32 processTimeout, string ResProccessTimerLogActive, string ResProccessTimeValueforSaveLog, DataTable dtROE, double Commision = 7)
        {
            System.Diagnostics.Stopwatch timerroomx = new System.Diagnostics.Stopwatch();
            timerroomx.Start();
            List<Options> options = new List<Options>();

            DataTable DtHotel = _commonMethod.GenerateDataTableNewV2();

            //DataTable DtBDC_HotelRoom = Common.GenerateRoomtypeDataTableNewV2();
            DataTable dtInrCurrency = new DataTable();

            if (ResponseData == "")
            {
                Item.ResponseStatus = 204;
                Item.ResponseType = "NoCount";
                return options;
            }

            try
            {

                //string ConvertXml = JsonConvert.DeserializeXmlNode("{\"Row\":" + ResponseData + "}", "root").InnerXml;
                //DataSet BookingDotComResponse = _commonMethod.GetDataSetFromXMLString(ConvertXml);
                //if (BookingDotComResponse == null || !BookingDotComResponse.Tables.Contains("Hotel"))
                //   return null;

                Item.ResponseType = "Success";

                #region "When not Need Room"  
                // add hotel code from price only and room code blank here
                if (SearchParam.Filters.IsShowRooms == 0)
                {

                    currencyfactor = 1;
                    //BookingDotCom.RootObject res = JsonConvert.DeserializeObject<BookingDotCom.RootObject>(ResponseData);
                    //BookingdotcomClass_v1.Root res = JsonSerializer.Deserialize<BookingdotcomClass_v1.Root>(ResponseData);
                    BookingdotcomClass_v1.Root res = Newtonsoft.Json.JsonConvert.DeserializeObject<BookingdotcomClass_v1.Root>(ResponseData); //140525
                    int CutOffDays = CacheMaster.CutOffDays;
                    CutOffDays = _commonMethod.GetCutOffDays(commonSearch.dtSuplierMarkup, Supplier);
                    for (int m = 0; m <= res.result.Count - 1; m++)
                    {


                        string currency = Convert.ToString(res.result[m].hotel_currency_code);
                        if (currency == null)
                            continue;
                        if (currency.ToLower() != (Convert.ToString(currencyCode)).ToLower())
                        {
                            //dtInrCurrency = Common.GetCountryCurrency(currency);
                            //currencyfactor = Convert.ToDouble(dtInrCurrency.Rows[0]["AmtToExch"]);
                            currencyfactor = _commonMethod.GetROEByCurrency(currency, dtROE);
                        }
                        #region "Hotel Code Block "
                        DataRow drXmlHotel = DtHotel.NewRow();
                        string ItemCode = Convert.ToString(res.result[m].hotel_id);
                        //string ItemCode = Convert.ToString(BookingDotComResponse.Tables["result"].Rows[m]["hotel_id"]);
                        //string HotelName = Convert.ToString(BookingDotComResponse.Tables["result"].Rows[m]["hotel_name"]).Replace("'", " ").Replace("&", " ");
                        string HotelName = Convert.ToString(res.result[m].hotel_name.Replace("'", " ").Replace("&", " "));
                        string InternalHotelId = Convert.ToString(ItemCode);
                        drXmlHotel["IsRecomondedHotel"] = false;
                        drXmlHotel["XMLHotelCode"] = ItemCode;
                        drXmlHotel["HotelName"] = HotelName;
                        drXmlHotel["SupplierName"] = Supplier;
                        drXmlHotel["OfficeID"] = 0;
                        drXmlHotel["ContractID"] = 0;
                        drXmlHotel["Rating"] = 0;
                        drXmlHotel["Reviews"] = 0;
                        drXmlHotel["UniqueNo"] = ItemCode;
                        drXmlHotel["IsShowSupplier"] = false;
                        drXmlHotel["BookingStatus"] = "Available";
                        drXmlHotel["XMLCity"] = xmlcity;
                        drXmlHotel["XMLCountry"] = xmlcountry;
                        drXmlHotel["SupplierCurrency"] = currency;
                        double FinalTotal = 0, min = 0;
                        int max = 0, b = 0;
                        for (int hi = 0; hi < SearchParam.Rooms.Count; hi++)
                        {
                            b = (Convert.ToInt32(SearchParam.Rooms[hi].NoofAdults) + Convert.ToInt32(SearchParam.Rooms[hi].NoofChild));
                            if (hi == 0)
                                max = b;
                            else if (max < b)
                                max = b;
                        }
                        int maxoccupancy = 0;

                        for (int r = 0; r < res.result[m].rooms.Count; r++)
                        {
                            //maxoccupancy = res.result[m].rooms[r].adults;
                            //if (res.result[m].rooms[r].adults > maxoccupancy)
                            //    maxoccupancy = res.result[m].rooms[r].adults;
                            maxoccupancy = res.result[m].rooms[r].adults + (res.result[m].rooms[r].children != null ? res.result[m].rooms[r].children.Count : 0);
                            if ((res.result[m].rooms[r].adults + res.result[m].rooms[r].children.Count) > maxoccupancy)
                                maxoccupancy = (res.result[m].rooms[r].adults + res.result[m].rooms[r].children.Count);
                        }
                        if (maxoccupancy >= max)
                        {
                            FinalTotal = 0; min = 0;
                            min = res.result[m].price;
                            //min = min - res.result[m].commission_details.commission_amount;

                            //double extra = 0;
                            //for (int r = 0; r < res.result[m].rooms.Count; r++)
                            //{
                            //    for (int re = 0; re < res.result[m].rooms[r].extra_charge.Count; re++)
                            //    {
                            //        if (res.result[m].rooms[r].extra_charge[re].name.ToLower() != "tourism fee")
                            //        {
                            //            if (res.result[m].rooms[r].extra_charge[re].excluded)
                            //                extra += res.result[m].rooms[r].extra_charge[re].amount;
                            //        }
                            //    }
                            //}

                            //min = min + extra;

                            if (FinalTotal == 0)
                            {
                                FinalTotal = min * currencyfactor;
                            }
                            else if ((min * currencyfactor) < FinalTotal)
                            {
                                FinalTotal = min * currencyfactor;
                            }
                            if (FinalTotal == 0)
                                continue;
                            drXmlHotel["FinalSellingPrice"] = FinalTotal;
                            drXmlHotel["SellingPrice"] = FinalTotal;
                            DtHotel.Rows.Add(drXmlHotel);

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

                        }



                        #endregion
                    }
                }
                #endregion

                #region "When Need Rooms"
                //add Hotel code blank and rooms code in room block
                else
                {

                    //BookingDotComRoom.RootObject dsResponse = JsonConvert.DeserializeObject<BookingDotComRoom.RootObject>(ResponseData);
                    //BookingDotComRoom.RootObject dsResponse = JsonConvert.DeserializeObject<BookingDotComRoom.RootObject>(ResponseData);
                    //BookingDotComRoom.RootObject dsResponse = JsonSerializer.Deserialize<BookingDotComRoom.RootObject>(ResponseData);
                    BookingDotComRoom.RootObject dsResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<BookingDotComRoom.RootObject>(ResponseData);

                    if (dsResponse == null)
                    {
                        Item.ResponseStatus = 204;
                        return options;
                    }
                       

                    if (dsResponse.result == null || dsResponse.result?.Count == 0)
                    {
                        Item.SupplierResponseError = ResponseData;
                        Item.ResponseStatus = 404;
                        return options;
                    }
                        



                    int CutOffDays = CacheMaster.CutOffDays;
                    CutOffDays = _commonMethod.GetCutOffDays(commonSearch.dtSuplierMarkup, Supplier);

                    DataRow drXmlHotel = DtHotel.NewRow();
                    string ItemCode = Convert.ToString(dsResponse.result[0].hotel_id);
                    // string HotelName = Convert.ToString(BookingDotComResponse.Tables["Hotel"].Rows[m]["name"]).Replace("'", " ").Replace("&", " ");
                    string InternalHotelId = Convert.ToString(ItemCode);
                    // string HotelIds =
                    drXmlHotel["IsRecomondedHotel"] = false;
                    drXmlHotel["XMLHotelCode"] = ItemCode;
                    drXmlHotel["HotelName"] = "";
                    drXmlHotel["SupplierName"] = Supplier;
                    drXmlHotel["OfficeID"] = 0;
                    drXmlHotel["ContractID"] = 0;
                    drXmlHotel["Rating"] = 0;
                    drXmlHotel["Reviews"] = 0;
                    drXmlHotel["UniqueNo"] = ItemCode;
                    drXmlHotel["IsShowSupplier"] = false;
                    drXmlHotel["BookingStatus"] = "Available";
                    drXmlHotel["XMLCity"] = xmlcity;
                    drXmlHotel["XMLCountry"] = xmlcountry;
                    drXmlHotel["SupplierCurrency"] = currencyCode;
                    double FinalTotal = 1, min = 0;
                    int max = 0, b = 0;
                    //for (int hi = 0; hi < SearchParam.Rooms.Count; hi++)
                    //{
                    //    b = (Convert.ToInt32(SearchParam.Rooms[hi].NoofAdults) + Convert.ToInt32(SearchParam.Rooms[hi].NoofChild));
                    //    if (hi == 0)
                    //        max = b;
                    //    else if (max < b)
                    //        max = b;
                    //}

                    drXmlHotel["FinalSellingPrice"] = FinalTotal;
                    drXmlHotel["SellingPrice"] = FinalTotal;

                    DtHotel.Rows.Add(drXmlHotel);
                    double Pricenew = FinalTotal;
                    if (FinalTotal > 0)
                    {
                        #region "Room code Block"
                        bool willAddOrNot = true;
                        Int64 RaynaID = 0;
                        Int64 GiataId = 0;
                        int RatingHotel = 0;
                        Int64 OwnsystemHotelId = 0;
                        //  string ownsystemhotelId = null;
                        string Provider = Supplier;
                        double MinPrice = FinalTotal;
                        _markup.GetHotelDataCommon(ref MinPrice, ref willAddOrNot, ref OwnsystemHotelId, ref RaynaID, ref GiataId, ref RatingHotel, ref drXmlHotel, commonSearch.AgentId, commonSearch.TariffID, commonSearch.dsAgentDetails, commonSearch.supplierGiataMaster, SearchParam, commonSearch.dtCountryCityMarkup, commonSearch.B2CMarkup, commonSearch.IsParentId, commonSearch.SubAgentMarkup, commonSearch.SearchByHotel, commonSearch.HotelIDS);
                        if (willAddOrNot)
                        {
                            //#region "Room Logic"
                            //DataTable dt_HotelRoom = Common.GenerateRoomtypeDataTableNewV2();
                            //DataTable dt_Cancellation = Common.GenerateCancellationDataTableNewV2();
                            //DataTable dt_RateBreackUp = Common.GenerateoomRateBreackUpDataTableNewV2();
                            //#endregion
                            #region DataTable
                            DataTable DtBDC_HotelRoom = _commonMethod.GenerateRoomtypeDataTableNewV2();
                            DataTable dtBDC_Cancellation = _commonMethod.GenerateCancellationDataTableNewV2();
                            DataTable DtBDC_RateBreackUp = _commonMethod.GenerateoomRateBreackUpDataTableNewV2();

                            double CurrencyFactor = 1;
                            DataSet dsTemp = new DataSet();
                            #endregion

                            #region RoomOnly RQ


                            double SubAgentMarkup = 0;
                            int IsParentId = 1, agentID = 0;
                            if (dtAgentDetail != null)
                            {
                                IsParentId = Convert.ToInt32(dtAgentDetail.Rows[0]["IsParentId"]);
                                SubAgentMarkup = Convert.ToDouble(dtAgentDetail.Rows[0]["B2CMarkupPer"]);
                                agentID = Convert.ToInt32(dtAgentDetail.Rows[0]["AgentID"]);
                            }
                            string GIITACode = "";
                            double AgentPercentage = 0.0;
                            Boolean blnIsShowSupplier = false;
                            double AgentMarkupAmount = 0.0;
                            Boolean isPerce = true;

                            string Nationality = xmlnationality;

                            double AgentCompanyPercentage = Convert.ToDouble(SearchParam.AgentCompanyPercentage);
                            bool IsB2C = Convert.ToBoolean(Convert.ToByte(SearchParam.IsB2BOrB2C));

                            double B2CMarkup = (IsB2C == false ? AgentCompanyPercentage : XMLHotelBookingCommon.B2CMarkup);
                            if (IsB2C == true)
                                AgentPercentage = 0;

                            int NoOfNight = SearchParam.NoofNights;
                            int hotelRating = 0;
                            //_markup.getAgetMarkupSetting(Supplier, GIITACode, dtSuplierMarkup, dtMarkupDetails, ref AgentPercentage, ref AgentMarkupAmount, ref blnIsShowSupplier, ref isPerce, agentID, hotelRating, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), Convert.ToString(SearchParam.CheckInDate));

                            _markup.getAgetMarkupSettingNew_new(Supplier, Convert.ToString(RaynaID), commonSearch.dtSuplierMarkup, commonSearch.dsMarkupDetails, ref AgentPercentage, ref AgentMarkupAmount, ref blnIsShowSupplier, ref isPerce, commonSearch.AgentId, RatingHotel, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), SearchParam.CheckInDate);

                            int totalAdult = 0;
                            int totalChild = 0;
                            string Essential = "";

                            double VatonfirstnightPrice = 0;
                            for (int e = 0; e < SearchParam.Rooms.Count; e++)
                            {
                                totalAdult = Convert.ToInt32(SearchParam.Rooms[e].NoofAdults) + totalAdult;
                                totalChild = Convert.ToInt32(SearchParam.Rooms[e].NoofChild) + totalChild;
                            }
                            #endregion
                            {


                                string currencycode = "USD";
                                if (dsResponse.result.Count > 0)
                                {
                                    currencycode = Convert.ToString(dsResponse.result[0].block[0].min_price.currency);
                                    {
                                        currencycode = Convert.ToString(dsResponse.result[0].block[0].min_price.currency);
                                        if (currencycode.ToLower() != (Convert.ToString(currencyCode)).ToLower())
                                        {
                                            //dtInrCurrency = Common.GetCountryCurrency(currencycode);
                                            //CurrencyFactor = Convert.ToDouble(dtInrCurrency.Rows[0]["AmtToExch"]);
                                            CurrencyFactor = _commonMethod.GetROEByCurrency(currencycode, dtROE);
                                        }
                                    }
                                }
                                for (int R = 0; R < dsResponse.result.Count; R++)
                                {
                                    bool can_pay_now = dsResponse.result[R].can_pay_now;
                                    string hotel_url = Convert.ToString(dsResponse.result[R].hotel_url);
                                    bool direct_payment = dsResponse.result[R].direct_payment;
                                    bool is_flash_deal = dsResponse.result[R].is_flash_deal;
                                    int hotel_id = Convert.ToInt32(dsResponse.result[R].hotel_id);
                                    string checkin = Convert.ToString(dsResponse.result[R].checkin);
                                    string checkout = Convert.ToString(dsResponse.result[R].checkout);

                                    if (can_pay_now == false)
                                        return null;

                                    int d = 0, maxx = 0;
                                    var result = SearchParam.Rooms.GroupBy(x => new { x.NoofAdults, x.NoofChild, x.Child1Age, x.Child2Age }).Select(n => new { NoofAdults = n.Key.NoofAdults, NoofChild = n.Key.NoofChild, ChildAge = n.ToList(), Count = n.Count() });
                                    //for (int hi = 0; hi < SearchParam.Rooms.Count; hi++)
                                    //{
                                    //    b = (Convert.ToInt32(SearchParam.Rooms[hi].NoofAdults) + Convert.ToInt32(SearchParam.Rooms[hi].NoofChild));
                                    //    if (hi == 0)
                                    //        max = b;
                                    //    else if (max < b)
                                    //        max = b;
                                    //}
                                    //for (int r = 0; r < SearchParam.Rooms.Count; r++)
                                    //    {
                                    //    string e = "";
                                    //    for (int i = 0; i < dsResponse.result[R].block.Count; i++)
                                    //    {
                                    //        e = "";
                                    //        totalAdult = Convert.ToInt32(SearchParam.Rooms[r].NoofAdults);
                                    //        totalChild = Convert.ToInt32(SearchParam.Rooms[r].NoofChild);


                                    //    }

                                    //    }
                                    for (int RO = 0; RO < dsResponse.result[R].block.Count; RO++)
                                    {


                                        string block_id = Convert.ToString(dsResponse.result[R].block[RO].block_id);
                                        string taxes = Convert.ToString(dsResponse.result[R].block[RO].taxes);
                                        bool deposit_required = dsResponse.result[R].block[RO].deposit_required;
                                        //string photos = Convert.ToString(dsResponse.result[R].block[RO].photos);
                                        string pay_later_collection_date = Convert.ToString(dsResponse.result[R].block[RO].pay_later_collection_date);
                                        int max_occupancy = Convert.ToInt32(dsResponse.result[R].block[RO].max_occupancy);
                                        int TotalA = Convert.ToInt32(dsResponse.result[R].block[RO].max_adults);
                                        int max_children_free = Convert.ToInt32(dsResponse.result[R].block[RO].max_children_free);
                                        int max_children_free_age = Convert.ToInt32(dsResponse.result[R].block[RO].max_children_free_age);

                                        bool payleter = true;

                                        int min_rate_nr_available_blocks = Convert.ToInt32(dsResponse.result[R].block[RO].min_rate_nr_available_blocks); //31-01-2024 as mustafa sir k2

                                        if (pay_later_collection_date == "")
                                        {
                                            payleter = false;
                                        }
                                        //bool isflashdeal = dsResponse.result[R].block[RO].is_flash_deal;
                                        int RoomCount = 1;
                                        for (int hi = 0; hi < SearchParam.Rooms.Count; hi++)
                                        {
                                            totalAdult = Convert.ToInt32(SearchParam.Rooms[hi].NoofAdults);
                                            totalChild = Convert.ToInt32(SearchParam.Rooms[hi].NoofChild);

                                            string e = "";
                                            d = (Convert.ToInt32(SearchParam.Rooms[hi].NoofAdults) + Convert.ToInt32(SearchParam.Rooms[hi].NoofChild));
                                            int AdultC = Convert.ToInt32(SearchParam.Rooms[hi].NoofAdults);

                                            if (RoomCount > min_rate_nr_available_blocks)
                                            {
                                                break;
                                            }

                                            if (d <= max_occupancy && AdultC <= TotalA)
                                            {
                                                RoomCount++;

                                                if (hi == 0)
                                                    maxx = d;
                                                else if (maxx < d)
                                                    maxx = d;


                                                {

                                                    string RoomName = Convert.ToString(dsResponse.result[R].block[RO].name);

                                                    int Room_Id = Convert.ToInt32(dsResponse.result[R].block[RO].room_id);
                                                    string RoomDescription = Convert.ToString(dsResponse.result[R].block[RO].room_description);

                                                    #region Price
                                                    DataTable dtExtraCharge = new DataTable();
                                                    dtExtraCharge.Columns.Add("incremental_price");
                                                    dtExtraCharge.Columns.Add("price");
                                                    dtExtraCharge.Columns.Add("charge_price");
                                                    dtExtraCharge.Columns.Add("currency");
                                                    dtExtraCharge.Columns.Add("extra_charges_breakdown");

                                                    double Final_Rate = Convert.ToDouble(dsResponse.result[R].block[RO].min_price.price), ItemPrice = 0;
                                                    //int Commision = 7;
                                                    double CompanyBuying = 0, AgentSelling = 0;
                                                    CompanyBuying = Final_Rate * CurrencyFactor;
                                                    double charge_price = Convert.ToDouble(dsResponse.result[R].block[RO].min_price.charge_price);
                                                    string Currency = Convert.ToString(dsResponse.result[R].block[RO].min_price.currency);
                                                    double ExtraCharge = 0, amount = 0, RoomRateForCompany = 0, RoomRateForAgent = 0, BeforeCommisionRoomRateForAgent = 0;
                                                    for (int P = 0; P < dsResponse.result[R].block[RO].incremental_price.Count; P++)
                                                    {
                                                        double IncrementalPrice = Convert.ToDouble(dsResponse.result[R].block[RO].incremental_price[P].price);

                                                        double pricewithoutFactor = IncrementalPrice;

                                                        IncrementalPrice = IncrementalPrice * CurrencyFactor;

                                                        //RoomRateForAgent = IncrementalPrice;
                                                        double ChargePrice = Convert.ToDouble(dsResponse.result[R].block[RO].incremental_price[P].charge_price);


                                                        RoomRateForAgent = ChargePrice * CurrencyFactor;
                                                        BeforeCommisionRoomRateForAgent = RoomRateForAgent;

                                                        //string Currency = Convert.ToString(dsResponse.result[R].block[RO].incremental_price[P].currency);

                                                        //double Profit = RoomRateForAgent * Convert.ToDouble(Commision) / 100;
                                                        double Profit = Convert.ToDouble(dsResponse.result[R].block[RO].commission_details.commission_amount.amount) * CurrencyFactor;//added on 4-APR-2024 as Mustafasir/ K2


                                                        RoomRateForAgent = RoomRateForAgent - Profit;


                                                        #endregion
                                                        #region Meal
                                                        if (Convert.ToString(dsResponse.result[R].block[RO].breakfast_included) != null)
                                                        {
                                                            bool breakfast_included = Convert.ToBoolean(dsResponse.result[R].block[RO].breakfast_included);

                                                            string Meal = Convert.ToString("RO");
                                                            if (breakfast_included)
                                                                Meal = "BB";


                                                            #endregion
                                                            #region Offer, Refunable - Non Refundable
                                                            bool refundable = Convert.ToBoolean(dsResponse.result[R].block[RO].refundable);
                                                            bool isFlashDeal = Convert.ToBoolean(dsResponse.result[R].block[RO].is_flash_deal);
                                                            string refundable_until = Convert.ToString(dsResponse.result[R].block[RO].refundable_until);
                                                            string OfferDes = "", Offer = "";
                                                            if (isFlashDeal)
                                                                OfferDes = "Offer Applied to Total";
                                                            string RoomTypeId = Room_Id + "$$$" + RoomName + "$$$" + Currency + "$$$" + Meal + "$$$" + currencycode + "$$$" + pricewithoutFactor + "$$$" + block_id + "$$$" + Nationality + "$$$" + payleter + "$$$" + Guid.NewGuid().ToString();
                                                            string EssentialInfo = taxes + "-" + RoomDescription + "-" + e + "<br/>" + Essential;
                                                            RoomTypeId = RoomTypeId.Replace("'", "");

                                                            #endregion

                                                            #region RateBreakUp
                                                            //for (int n = 0; n < SearchParam.NoofNights; n++)
                                                            //{
                                                            //    DateTime Day = DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null);
                                                            //    Day = Day.AddDays(n);
                                                            //    string Days = Convert.ToString(Day);
                                                            //    string rateWeekColumn = Convert.ToString(Day.Date.DayOfWeek).Substring(0, 3);
                                                            //    double FRate = RoomRateForAgent;
                                                            //    RoomRateForCompany = CompanyBuying;
                                                            //    //RoomRateForAgent = AgentSelling;

                                                            //    #region Vat
                                                            //    Double VatOnRateBrackUp = 0;
                                                            //    FRate = _markup.ProcessHotelRateBreakupPrice_WithMarkup(1, SearchParam.CityID, SearchParam.CountryID.ToString(), RoomRateForAgent / NoOfNight, B2CMarkup, IsParentId, isPerce, AgentPercentage, AgentMarkupAmount, SubAgentMarkup, ref VatOnRateBrackUp);
                                                            //    #endregion

                                                            //    VatonfirstnightPrice += VatOnRateBrackUp;
                                                            //    DataRow drRateBreackup = DtBDC_RateBreackUp.NewRow();
                                                            //    drRateBreackup["SupplierName"] = Supplier;
                                                            //    drRateBreackup["OwnsystemHotelId"] = OwnsystemHotelId;
                                                            //    drRateBreackup["ContractID"] = 0;
                                                            //    drRateBreackup["RoomTypeName"] = "[R-" + (hi + 1) + "]" + RoomName;
                                                            //    drRateBreackup["RoomTypeId"] = RoomTypeId;
                                                            //    drRateBreackup["MealId"] = Meal;
                                                            //    drRateBreackup["Meal"] = Meal;
                                                            //    drRateBreackup["DateOfWeek"] = rateWeekColumn;
                                                            //    drRateBreackup["Date"] = Day;
                                                            //    drRateBreackup["Rate"] = FRate;
                                                            //    drRateBreackup["RateType"] = "[R-" + (hi + 1) + "]" + RoomName;
                                                            //    drRateBreackup["RoomNo"] = hi + 1;//r + 1;
                                                            //    drRateBreackup["RoomSequenceNo"] = "";
                                                            //    drRateBreackup["BookingStatus"] = "Available";
                                                            //    drRateBreackup["SupplierCurrency"] = currencycode;
                                                            //    drRateBreackup["BuyingPrice"] = FRate / CurrencyFactor;
                                                            //    drRateBreackup["VatdiffAmount"] = VatOnRateBrackUp;
                                                            //    drRateBreackup["OrderNo"] = 1;
                                                            //    DtBDC_RateBreackUp.Rows.Add(drRateBreackup);
                                                            //}

                                                            //for (int ex = 0; ex < dtExtraCharge.Rows.Count; ex++)
                                                            //{
                                                            //    double ExtraCharge1 = 0, amount1 = 0;
                                                            //    Double VatOnRateBrackUp = 0;
                                                            //    string NameExtraCharge1 = "", type1 = "";
                                                            //    bool excluded1 = true;
                                                            //    ExtraCharge1 = Convert.ToDouble(dtExtraCharge.Rows[ex]["ExtraCharge"]);
                                                            //    NameExtraCharge1 = Convert.ToString(dtExtraCharge.Rows[ex]["Name"]);
                                                            //    amount1 = Convert.ToDouble(dtExtraCharge.Rows[ex]["Amount"]);
                                                            //    type1 = Convert.ToString(dtExtraCharge.Rows[ex]["Type"]);
                                                            //    excluded1 = Convert.ToBoolean(dtExtraCharge.Rows[ex]["Excluded"]);
                                                            //    double a = amount1;

                                                            //    if (excluded1 == true)
                                                            //    {
                                                            //        for (int n = 0; n < SearchParam.NoofNights; n++)
                                                            //        {
                                                            //            amount1 = a;
                                                            //            RoomRateForAgent += amount1;
                                                            //            RoomRateForCompany += amount1;

                                                            //            amount1 = _markup.ProcessHotelRateBreakupPrice_WithMarkup(1, SearchParam.CityID, SearchParam.CountryID.ToString(), RoomRateForAgent, B2CMarkup, IsParentId, isPerce, AgentPercentage, AgentMarkupAmount, SubAgentMarkup, ref VatOnRateBrackUp);
                                                            //            DateTime Day = DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null);
                                                            //            Day = Day.AddDays(n);
                                                            //            string Days = Convert.ToString(Day);
                                                            //            string rateWeekColumn = Convert.ToString(Day.Date.DayOfWeek).Substring(0, 3);

                                                            //            VatonfirstnightPrice += VatOnRateBrackUp;
                                                            //            DataRow drRateBreackup = DtBDC_RateBreackUp.NewRow();
                                                            //            drRateBreackup["SupplierName"] = Supplier;
                                                            //            drRateBreackup["OwnsystemHotelId"] = OwnsystemHotelId;
                                                            //            drRateBreackup["ContractID"] = 0;
                                                            //            drRateBreackup["RoomTypeName"] = "[R-" + (hi + 1) + "]" + RoomName;
                                                            //            drRateBreackup["RoomTypeId"] = RoomTypeId;
                                                            //            drRateBreackup["MealId"] = Meal;
                                                            //            drRateBreackup["Meal"] = Meal;
                                                            //            drRateBreackup["DateOfWeek"] = rateWeekColumn;
                                                            //            drRateBreackup["Date"] = Day;
                                                            //            drRateBreackup["Rate"] = amount1;
                                                            //            drRateBreackup["RateType"] = NameExtraCharge1; //"[R-" + (r + 1) + "]" + roomName;
                                                            //            drRateBreackup["RoomNo"] = hi + 1;//r + 1;
                                                            //            drRateBreackup["RoomSequenceNo"] = "";
                                                            //            drRateBreackup["BookingStatus"] = type1;
                                                            //            drRateBreackup["SupplierCurrency"] = currencycode;
                                                            //            drRateBreackup["BuyingPrice"] = CompanyBuying; //0;
                                                            //            drRateBreackup["OrderNo"] = 1;
                                                            //            DtBDC_RateBreackUp.Rows.Add(drRateBreackup);
                                                            //        }
                                                            //    }
                                                            //}
                                                            #endregion

                                                            #region Room
                                                            IncrementalPrice = IncrementalPrice * CurrencyFactor;
                                                            DataRow drRoom = DtBDC_HotelRoom.NewRow();
                                                            drRoom["OwnSystemHotelId"] = OwnsystemHotelId;
                                                            drRoom["RoomTypeId"] = RoomTypeId;
                                                            drRoom["RoomTypeCode"] = Room_Id;
                                                            drRoom["RoomTypeName"] = RoomName;
                                                            drRoom["SupplierName"] = Supplier;
                                                            drRoom["BookingStatus"] = "Available";
                                                            drRoom["BoardType"] = Meal;
                                                            drRoom["RoomSequenceNo"] = "";
                                                            drRoom["ContractID"] = 0;
                                                            drRoom["EssentialInformation"] = EssentialInfo; //RoomDescription + e;//Convert.ToString(dsResponse.result[h].block[i].room_description);
                                                            drRoom["MealId"] = Meal;
                                                            drRoom["MealName"] = Meal;
                                                            drRoom["MaxExtraBed"] = "";//ExtraBed;
                                                            drRoom["RoomNo"] = hi + 1;//r + 1;
                                                            drRoom["Type"] = "";
                                                            drRoom["UniqueNo"] = "";
                                                            drRoom["Characteristic"] = "";
                                                            drRoom["IsRefundable"] = refundable;
                                                            drRoom["OfferCode"] = Offer;
                                                            drRoom["OfferDescription"] = OfferDes;
                                                            drRoom["AdultsOccupancy"] = totalAdult;
                                                            drRoom["ChildOccupancy"] = totalChild;
                                                            drRoom["OurProfit"] = Profit;// IncrementalPrice;
                                                            drRoom["BoardTypeID"] = Convert.ToString(Meal);
                                                            drRoom["CompanyBuyingPriceExpedia"] = pricewithoutFactor;
                                                            if (RoomRateForAgent > 0)
                                                            {
                                                                ItemPrice = RoomRateForAgent;
                                                                //Common.ProcessHotelRoom_WithMarkupForBookingDotCom(CurrencyFactor, currencycode, false, false, SearchParam.NoofNights, SearchParam.CityID, SearchParam.CountryId, SearchParam.CheckInDate, SearchParam.CheckOutDate, ref RoomRateForCompany, ref drRoom, isPerce, AgentPercentage, AgentMarkupAmount, SubAgentMarkup, B2CMarkup, IsParentId, ItemPrice);
                                                                //Common.ProcessHotelRoom_WithMarkup(CurrencyFactor, currencycode, false, false, SearchParam.NoofNights, SearchParam.CityID, SearchParam.CountryId, SearchParam.CheckInDate, SearchParam.CheckOutDate, ref ItemPrice, ref drRoom, isPerce, AgentPercentage, AgentMarkupAmount, SubAgentMarkup, B2CMarkup, IsParentId);
                                                                _markup.ProcessHotelRoom_WithMarkup_BookingDotComCommision(CurrencyFactor, currencycode, false, false, SearchParam.NoofNights, SearchParam.CityID, SearchParam.CountryID.ToString(), SearchParam.CheckInDate, SearchParam.CheckOutDate, ref ItemPrice, ref drRoom, isPerce, AgentPercentage, AgentMarkupAmount, SubAgentMarkup, B2CMarkup, IsParentId, BeforeCommisionRoomRateForAgent);

                                                            }
                                                            DtBDC_HotelRoom.Rows.Add(drRoom);
                                                            #endregion

                                                            #region Cancellation Policy

                                                            double AgentCancelCharge = 0;
                                                            double pri = 0;
                                                            double VatOnPolicy = 0;
                                                            checkin = Convert.ToString(SearchParam.CheckInDate);
                                                            checkin = checkin.Substring(6, 4) + "-" + checkin.Substring(3, 2) + "-" + checkin.Substring(0, 2);
                                                            DateTime canceldate = Convert.ToDateTime(checkin);


                                                            string CancelDeadlinewithoutCutoff = String.Format("{0:dd/MM/yyyy}", canceldate);
                                                            bool Refundable = Convert.ToBoolean(dsResponse.result[R].block[RO].refundable);

                                                            if (Refundable == false || RoomName.Contains("Non-refundable"))
                                                            {
                                                                AgentCancelCharge = ItemPrice;
                                                                //AgentCancelCharge = Common.ProcessHotelCancelationPrice_WithMarkup(ref pri, SearchParam.NoofNights, SearchParam.CityID, SearchParam.CountryId, AgentCancelCharge, isPerce, B2CMarkup, AgentPercentage, AgentMarkupAmount, IsParentId, SubAgentMarkup, ref VatOnPolicy);

                                                                dtBDC_Cancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref RoomRateForCompany, dtBDC_Cancellation.NewRow(), CurrencyFactor, currencycode, Supplier, Convert.ToString(OwnsystemHotelId), Convert.ToString(hi + 1), RoomTypeId, "[R-" + (hi + 1) + "]" + RoomName, Meal, DateTime.Now.Date, Convert.ToDateTime(checkin), AgentCancelCharge, AgentCancelCharge, Meal, VatonfirstnightPrice, CancelDeadlinewithoutCutoff, "Yes", "", "", "", ""));
                                                                dtBDC_Cancellation.AcceptChanges();

                                                            }
                                                            else
                                                            {
                                                                DateTime FromDate = Convert.ToDateTime(dsResponse.result[R].block[RO].refundable_until);
                                                                canceldate = FromDate;
                                                                pri = 0;
                                                                VatOnPolicy = 0;
                                                                double fee = RoomRateForAgent;
                                                                double companyFee = RoomRateForCompany;
                                                                pri = fee;
                                                                fee = _markup.ProcessHotelCancelationPrice_WithMarkup(ref pri, SearchParam.NoofNights, SearchParam.CityID, SearchParam.CountryID.ToString(), fee, isPerce, B2CMarkup, AgentPercentage, AgentMarkupAmount, IsParentId, SubAgentMarkup, ref VatOnPolicy);


                                                                if (canceldate.AddDays(CutOffDays) > DateTime.Now.Date)
                                                                    canceldate = canceldate.AddDays(CutOffDays);
                                                                else
                                                                    canceldate = DateTime.Now.Date;
                                                                if (canceldate > DateTime.Now.Date)
                                                                {
                                                                    double zero = 0;
                                                                    dtBDC_Cancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref zero, dtBDC_Cancellation.NewRow(), CurrencyFactor, currencycode, Supplier, Convert.ToString(OwnsystemHotelId), Convert.ToString(hi + 1), RoomTypeId, "[R-" + (hi + 1) + "]" + RoomName, Meal, DateTime.Now.Date, canceldate.AddDays(-1), AgentCancelCharge, AgentCancelCharge, Meal, VatonfirstnightPrice, CancelDeadlinewithoutCutoff, "No", "", "", "", ""));
                                                                    dtBDC_Cancellation.AcceptChanges();

                                                                    AgentCancelCharge = ItemPrice;
                                                                    //AgentCancelCharge = Common.ProcessHotelCancelationPrice_WithMarkup(ref pri, SearchParam.NoofNights, SearchParam.CityID, SearchParam.CountryId, AgentCancelCharge, isPerce, B2CMarkup, AgentPercentage, AgentMarkupAmount, IsParentId, SubAgentMarkup, ref VatOnPolicy);

                                                                    dtBDC_Cancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref RoomRateForCompany, dtBDC_Cancellation.NewRow(), CurrencyFactor, currencycode, Supplier, Convert.ToString(OwnsystemHotelId), Convert.ToString(hi + 1), RoomTypeId, "[R-" + (hi + 1) + "]" + RoomName, Meal, canceldate, Convert.ToDateTime(checkin), AgentCancelCharge, AgentCancelCharge, Meal, VatonfirstnightPrice, CancelDeadlinewithoutCutoff, "No", "", "", "", ""));
                                                                    dtBDC_Cancellation.AcceptChanges();
                                                                }
                                                                else
                                                                {
                                                                    AgentCancelCharge = ItemPrice;
                                                                    //AgentCancelCharge = Common.ProcessHotelCancelationPrice_WithMarkup(ref pri, SearchParam.NoofNights, SearchParam.CityID, SearchParam.CountryId, AgentCancelCharge, isPerce, B2CMarkup, AgentPercentage, AgentMarkupAmount, IsParentId, SubAgentMarkup, ref VatOnPolicy);

                                                                    dtBDC_Cancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref RoomRateForCompany, dtBDC_Cancellation.NewRow(), CurrencyFactor, currencycode, Supplier, Convert.ToString(OwnsystemHotelId), Convert.ToString(hi + 1), RoomTypeId, "[R-" + (hi + 1) + "]" + RoomName, Meal, DateTime.Now.Date, Convert.ToDateTime(checkin), AgentCancelCharge, AgentCancelCharge, Meal, VatonfirstnightPrice, CancelDeadlinewithoutCutoff, "Yes", "", "", "", ""));
                                                                    dtBDC_Cancellation.AcceptChanges();
                                                                }
                                                                #endregion
                                                            }
                                                        }
                                                        else
                                                        {
                                                            string Name = dsResponse.result[R].block[RO].name;
                                                        }

                                                    }
                                                }
                                                //}
                                            }
                                            else
                                            {
                                                // return null;
                                                continue;
                                            }
                                        }
                                    }
                                    #region Tables Merge
                                    #endregion
                                }

                            }
                            if (DtBDC_HotelRoom != null && DtBDC_HotelRoom.Rows.Count > 0) //Added on 25-OCT-2024 due to Roomnot add and return with 1$ price
                            {
                                Options ops = (await _supplierRes.GetCommonRoomForSuppliers(GiataId, commonSearch.tokenData, commonSearch.advancedOptions, Convert.ToDouble(MinPrice), drXmlHotel, DtBDC_HotelRoom, dtBDC_Cancellation, DtBDC_RateBreackUp, SearchParam));
                                if (ops != null)
                                    options.Add(ops);
                            }


                        }
                        #endregion

                    }
                }
                #endregion

                return options;
            }
            catch (Exception e)
            {
                Item.ResponseStatus = 404;
                await _exception.Error(e);
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
                        
                        SearchParam.SupplierTime += ",|Finish:Booking.com Process Response  " + "|Time :" + timerroomx.Elapsed.ToString();
                        _ = Task.Run(() => _commonMethod.FireAway_SQL_ResPorcessTimeLog("", timerroomx.Elapsed.ToString(), Supplier, SearchParam.SearchKey, 1, SearchParam.AgentID));

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

        #region V3
        public void GetBookingDotComRequestOnly_async_new_v3(DataRow[] commonCode, List<StarRating> listRating, HotelSearchParameter SearchPara, List<SupplierDetails> lstbookingDetails, List<SupplierSearchConfig> lstbookingSearchConfig, DataTable dtPreferredDataHotel, ref ConcurrentBag<HotelX> AllTask, int Hit, CommonSearchList commonSearchList, DataTable dtROE, TokenData tokenData, double BookingDotComCommison, DataSet dtXMLCity, bool SearchByOneID = true, bool HitFromAll = false)
        {
            try
            {
                if (lstbookingDetails != null && lstbookingDetails.Count > 0)
                {
                    List<SupplierDetails> BookingMain = new List<SupplierDetails>();
                    List<SupplierDetails> BookingChild = new List<SupplierDetails>();
                    List<StarRating> lstRating = new List<StarRating>();
                    lstRating = _commonMethod.GetStarRatingStringList_xtg(SearchPara);

                    List<HotelCodeByCity> DtHotelDAta = HtmlExtensions.ToList<HotelCodeByCity>(commonCode.CopyToDataTable());
                    int ratingcount = lstRating.Count;
                    int hotelcount = DtHotelDAta.Count;


                    foreach (SupplierDetails bookingDetails in lstbookingDetails)
                    {
                        if (bookingDetails != null)
                        {
                            int SupplierID = bookingDetails.SupplierID;
                            string Supplier = bookingDetails.SupplierName;

                            if (commonCode != null)
                            {
                                //if ((SearchPara.IsSearchFor.ToLower().IndexOf("booking.com") >= 0 || SearchPara.IsSearchFor.ToLower().IndexOf("multi") >= 0) && HitFromAll)
                                {
                                    DataForSearch dataForSearch = _commonMethod.GetXmlDataForSearchTechwise(SearchPara.Nationality, dtXMLCity, Supplier, bookingDetails.TechName);
                                    bool HitSupplier = true;
                                    //switch (SearchPara.Nationality.ToLower())
                                    //{
                                    //    case "russia":
                                    //    case "china":
                                    //        HitSupplier = false;
                                    //        break;
                                    //    default:
                                    //        HitSupplier = true;
                                    //        break;
                                    //}
                                    for (int i = 0; i < SearchPara.Rooms.Count; i++)
                                    {
                                        int NoOfAdt = SearchPara.Rooms[i].NoofAdults;
                                        if (NoOfAdt > 8)
                                            HitSupplier = false;
                                    }

                                    List<HotelCodeByCity> filter = new List<HotelCodeByCity>();

                                    filter = _commonMethod.FilterWithOutCity_Code(ratingcount, hotelcount, DtHotelDAta, lstRating, Supplier);


                                    if (HitSupplier && dataForSearch.XmlNationality != "" && filter.Count > 0 && SearchPara.PastDate == false)// added on 8-AUG-2024
                                    {
                                        string StrNationality = dataForSearch.XmlNationality;
                                        if (SearchPara.Nationality.ToLower() == "iran" || SearchPara.Nationality.ToLower() == "syria")
                                            StrNationality = "US";// as per Mustafa sir ( API group 30-Jan-2024 )

                                        if (SearchPara.Nationality.ToLower() == "china" || SearchPara.Nationality.ToLower() == "russia")
                                            StrNationality = "ES";// as per Mustafa sir ( 22-Aug-2025 )

                                        if (lstbookingSearchConfig.Count > 0)
                                        {

                                            List<SupplierSearchConfig> bookingSearchConfig = lstbookingSearchConfig
                                         .Where(x => x != null && x.SupplierID == SupplierID)
                                         .ToList();


                                            if (bookingSearchConfig != null && bookingSearchConfig.Count > 0)
                                            {

                                                string Ids = string.Empty;
                                                string xmlbase = "";
                                                _commonMethod.GetPrefferedHotelCodes(ref Ids, ref xmlbase, ref bookingSearchConfig, Supplier, dtPreferredDataHotel, SearchPara.IsSearchFor);


                                                if (filter.Count > 0)
                                                {


                                                    string bookingDotComCurrency = Convert.ToString(bookingDetails.CurrencyShortName);
                                                    double CurrencyFactorbooking = 1;

                                                    if (bookingDotComCurrency.ToLower() != tokenData.SiteCurrency.ToLower())
                                                    {
                                                        CurrencyFactorbooking = _commonMethod.GetROEByCurrency(bookingDotComCurrency, dtROE);// Convert.ToDouble(dtInrCurrency.Rows[0]["AmtToExch"]);
                                                    }


                                                    for (int k = 0; k < bookingSearchConfig.Count(); k++)
                                                    {
                                                        bool DebugMode = (Convert.ToInt32(bookingSearchConfig[k].DebugMode) == 0 ? false : true);

                                                        {
                                                            //List<HotelCodeByCity> DtHotelDAta = FilterFinalList(commonCode, listRating);
                                                            //if (commonCode != null)
                                                            {

                                                                if (Convert.ToInt32(bookingSearchConfig[k].CityID) == 0 || Convert.ToInt32(bookingSearchConfig[k].CityID) == SearchPara.CityID)
                                                                {
                                                                    string Rating = _commonMethod.GetStarRatingString(SearchPara);
                                                                    int Noofrecords = Convert.ToInt32(bookingSearchConfig[k].Noofrecords);

                                                                    if (SearchPara.Rooms[0].Child1Age != 1 || SearchPara.Rooms[0].Child2Age != 1)
                                                                    {
                                                                        #region Not Used

                                                                        //bool IsSame = true;
                                                                        //int childAgeCount = 0; int AdultCount = 0, ChildCount = 0;
                                                                        //for (int i = 0; i < SearchPara.Rooms.Count; i++)
                                                                        //{
                                                                        //    int NoOfAdt = SearchPara.Rooms[0].NoofAdults;
                                                                        //    int NoOfChd = SearchPara.Rooms[0].NoofChild;
                                                                        //    int ChildAge1 = SearchPara.Rooms[0].Child1Age;
                                                                        //    int ChildAge2 = SearchPara.Rooms[0].Child2Age;
                                                                        //    if (NoOfAdt != SearchPara.Rooms[i].NoofAdults || NoOfChd != SearchPara.Rooms[i].NoofChild || ChildAge1 != SearchPara.Rooms[i].Child1Age || ChildAge2 != SearchPara.Rooms[i].Child2Age)
                                                                        //        IsSame = false;

                                                                        //    if (SearchPara.Rooms[i].Child1Age == 1)
                                                                        //    {
                                                                        //        childAgeCount += SearchPara.Rooms[i].Child1Age;
                                                                        //    }
                                                                        //    if (SearchPara.Rooms[i].Child2Age == 1)
                                                                        //    {
                                                                        //        childAgeCount += SearchPara.Rooms[i].Child2Age;
                                                                        //    }
                                                                        //    AdultCount += SearchPara.Rooms[i].NoofAdults;
                                                                        //    ChildCount += SearchPara.Rooms[i].NoofChild;

                                                                        //}
                                                                        //if (childAgeCount > 1)
                                                                        //    IsSame = false;
                                                                        //if (AdultCount > 9)
                                                                        //    IsSame = false;
                                                                        //if (ChildCount > 9)
                                                                        //    IsSame = false;

                                                                        ////if (IsSame)
                                                                        #endregion
                                                                        //if (commonCode != null)
                                                                        {

                                                                            if (Convert.ToInt32(bookingSearchConfig[k].City) == 0)
                                                                            {

                                                                                int NoOfRecord = Convert.ToInt32(bookingSearchConfig[k].Noofrecords);
                                                                                double count = Convert.ToDouble(filter.Count);
                                                                                int NoOfDiv = Convert.ToInt32(Math.Ceiling(count / Convert.ToDouble(NoOfRecord)));
                                                                                List<List<XMLID_Search>> lstxml = _commonMethod.GetXMLIDString(NoOfDiv, NoOfRecord, count, filter);

                                                                                for (int a = 0; a < NoOfDiv; a++)
                                                                                {
                                                                                    string IdsForSearch = Convert.ToString(_commonMethod.GetHotelCodeByCOmma(lstxml[a]));

                                                                                    HotelX xhotel = new HotelX();
                                                                                    xhotel.Action = IdsForSearch;
                                                                                    xhotel.Supplier = Supplier;
                                                                                    xhotel.DebugeMode = DebugMode;
                                                                                    xhotel.SearchTimeOut = Convert.ToInt32(bookingSearchConfig[k].HotelSearchTimeOut);
                                                                                    // if (Hit > 0)
                                                                                    xhotel.ProcessTimeout = Convert.ToInt32(bookingSearchConfig[k].HotelSearchTimeOut2);

                                                                                    if (_keySettings.SupplierProxy == "0")
                                                                                        xhotel.URL = Convert.ToString(bookingDetails.WebSiteURL) + BookingInfo.BookingAvailability_URL;
                                                                                    else
                                                                                        xhotel.URL = Convert.ToString(bookingDetails.WebSiteURLforAPI) + BookingInfo.BookingAvailability_URL;
                                                                                    xhotel.UserId = Convert.ToString(bookingDetails.UserID);
                                                                                    xhotel.UserName = Convert.ToString(bookingDetails.UserName);
                                                                                    xhotel.PassWord = Convert.ToString(bookingDetails.Password);
                                                                                    xhotel.CurrencyCode = bookingDotComCurrency;
                                                                                    xhotel.CurrencyFactor = CurrencyFactorbooking;
                                                                                    xhotel.XmlCity = dataForSearch.xmlCity;
                                                                                    xhotel.XmlCountry = dataForSearch.xmlCountry;
                                                                                    xhotel.XmlNationality = StrNationality;
                                                                                    xhotel.AgentWiseMarkup = bookingDetails.AgentWiseMarkup;
                                                                                    xhotel.IsShowSupplier = Convert.ToInt16(bookingDetails.IsShowSupplier) == 1 ? true : false;
                                                                                    xhotel.SupplierID = bookingDetails.SupplierID;
                                                                                    xhotel.MasterSupplierID = bookingDetails.MasterSupplierId;
                                                                                    //xhotel.Request = Hotel.XMLBookingDotCom_v3RequestOnly_async(xhotel.URL, xhotel.UserName, xhotel.PassWord, xhotel.SearchTimeOut, SearchPara, IdsForSearch, xhotel.CurrencyCode, DebugMode, XMLCity, Rating, xhotel.XmlNationality);
                                                                                    #region "CoomonCode"
                                                                                    bool IsHitSeperate = false; // _commonMethod.IsSuppliersWithSeperateRoomRquest(xhotel.Supplier);
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
                                                                                        //objrq.Request = Common.Get_ETG_RoomOnly(Convert.ToString(ETG_Details[0].UserName), Convert.ToString(ETG_Details[0].Password), Convert.ToString(ETG_Details[0].UserID), SearchPara, "", DebugMode, Nationality, citycode, Currency);
                                                                                        objrq.Request = XMLBookingDotCom_v3RequestOnly_async(xhotel.URL, xhotel.UserName, xhotel.PassWord, xhotel.SearchTimeOut, SearchPara, IdsForSearch, xhotel.CurrencyCode, DebugMode, dataForSearch.xmlCity, Rating, xhotel.XmlNationality, Supplier);
                                                                                        //public static string Get_ETG_RoomOnly(HotelSearchParameter SearchParam, string CurrencyCode, string ETGHotelIds, string Nationality, string Currency)
                                                                                        objrq.Response = ""; RQRS.Add(objrq);
                                                                                    }
                                                                                    xhotel.RQRS = RQRS;
                                                                                    #endregion
                                                                                    xhotel.ReturnRooms = bookingDetails.ReturnRooms;
                                                                                    xhotel.TechName = bookingDetails.TechName;

                                                                                    AllTask.Add(xhotel);
                                                                                }
                                                                            }
                                                                            else
                                                                            {
                                                                                for (int a = 0; a < 1; a++)
                                                                                {
                                                                                    string IdsForSearch = "";// Convert.ToString(paramValues1);

                                                                                    HotelX xhotel = new HotelX();
                                                                                    xhotel.Supplier = Supplier;
                                                                                    xhotel.DebugeMode = DebugMode;
                                                                                    xhotel.SearchTimeOut = Convert.ToInt32(bookingSearchConfig[k].HotelSearchTimeOut);
                                                                                    //if (Hit > 0)
                                                                                    xhotel.ProcessTimeout = Convert.ToInt32(bookingSearchConfig[k].HotelSearchTimeOut2);


                                                                                    if (_keySettings.SupplierProxy == "0")
                                                                                        xhotel.URL = Convert.ToString(bookingDetails.WebSiteURL) + BookingInfo.BookingAvailability_URL;
                                                                                    else
                                                                                        xhotel.URL = Convert.ToString(bookingDetails.WebSiteURLforAPI) + BookingInfo.BookingAvailability_URL;

                                                                                    xhotel.UserId = Convert.ToString(bookingDetails.UserID);
                                                                                    xhotel.UserName = Convert.ToString(bookingDetails.UserName);
                                                                                    xhotel.PassWord = Convert.ToString(bookingDetails.Password);
                                                                                    xhotel.CurrencyCode = Convert.ToString(bookingDotComCurrency);
                                                                                    xhotel.CurrencyFactor = CurrencyFactorbooking;
                                                                                    xhotel.XmlCity = dataForSearch.xmlCity;
                                                                                    xhotel.XmlCountry = dataForSearch.xmlCountry;
                                                                                    xhotel.XmlNationality = StrNationality;
                                                                                    #region "CoomonCode"
                                                                                    bool IsHitSeperate = false; //_commonMethod.IsSuppliersWithSeperateRoomRquest(xhotel.Supplier);
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
                                                                                        //objrq.Request = Common.Get_ETG_RoomOnly(Convert.ToString(ETG_Details[0].UserName), Convert.ToString(ETG_Details[0].Password), Convert.ToString(ETG_Details[0].UserID), SearchPara, "", DebugMode, Nationality, citycode, Currency);
                                                                                        objrq.Request = XMLBookingDotCom_v3RequestOnly_async(xhotel.URL, xhotel.UserName, xhotel.PassWord, xhotel.SearchTimeOut, SearchPara, IdsForSearch, xhotel.CurrencyCode, DebugMode, dataForSearch.xmlCity, Rating, xhotel.XmlNationality, Supplier);
                                                                                        //public static string Get_ETG_RoomOnly(HotelSearchParameter SearchParam, string CurrencyCode, string ETGHotelIds, string Nationality, string Currency)
                                                                                        objrq.Response = ""; RQRS.Add(objrq);
                                                                                    }
                                                                                    xhotel.RQRS = RQRS;
                                                                                    #endregion
                                                                                    xhotel.ReturnRooms = bookingDetails.ReturnRooms;
                                                                                    xhotel.TechName = bookingDetails.TechName;

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

        public string XMLBookingDotCom_v3RequestOnly_async(string Url, string UserName, string Password, Int32 SearchTimeOut, HotelSearchParameter SearchParam, string HotelIds, string CurrencyCode, bool DebugMode, string CityCode, string Rating, string Nationality, string SupplierName = "")
        {
            try
            {
                #region PriceOnly RQ
                string BDCPriceOnly = Get_BookingDotCom_PriceOnly_v3(Url, SearchParam, UserName, Password, CityCode, HotelIds, CurrencyCode, Rating, Nationality, SupplierName);
                return BDCPriceOnly;
                #endregion
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
                return "";
            }
        }

        public string Get_BookingDotCom_PriceOnly_v3_old(string Url, HotelSearchParameter SearchParam, string UserName, string Password, string destination, string HotelIds, string CurrencyCode, string Rating, string Nationality)
        {
            try
            {

                string checkIn = SearchParam.CheckInDate.Substring(6, 4) + "-" + SearchParam.CheckInDate.Substring(3, 2) + "-" + SearchParam.CheckInDate.Substring(0, 2);
                string checkOut = SearchParam.CheckOutDate.Substring(6, 4) + "-" + SearchParam.CheckOutDate.Substring(3, 2) + "-" + SearchParam.CheckOutDate.Substring(0, 2);


                JsonSerializerSettings Setting = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    DefaultValueHandling = DefaultValueHandling.Ignore
                };

                searchhotelnroom.Root RootObject = new searchhotelnroom.Root();

                searchhotelnroom.Booker Booker = new searchhotelnroom.Booker();
                searchhotelnroom.Guests Guests = new searchhotelnroom.Guests();

                List<int> hoteids = new List<int>();

                if (string.IsNullOrEmpty(HotelIds))
                {
                    RootObject.city = destination;
                }
                else
                {


                    string[] hotelIdsArray = HotelIds.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var id in hotelIdsArray)
                    {
                        string trimmedId = id.Trim();
                        int hotelId;
                        if (int.TryParse(id.Trim(), out hotelId))
                        {
                            hoteids.Add(hotelId);// Add each ID as an integer
                        }

                        // RootObject.accommodations.Add(10004); 
                    }
                }

                RootObject.accommodations = hoteids;
                Booker.country = Nationality.ToLower();

                //Booker.platform = "desktop";
                Booker.platform = "mobile";

                RootObject.booker = Booker;


                RootObject.checkin = checkIn;
                RootObject.checkout = checkOut;

                List<string> extras = new List<string>();
                string extra = "extra_charges";
                extras.Add(extra);
                RootObject.extras = extras;


                int Adult = 0;
                List<int> Child = new List<int>();
                foreach (var room in SearchParam.Rooms)
                {
                    Adult += room.NoofAdults;
                    int NoofChild = room.NoofChild;
                    if (NoofChild > 0)
                    {
                        if (room.NoofChild == 1)
                        {
                            Child.Add(Convert.ToInt32(room.Child1Age));
                        }
                        else if (room.NoofChild == 2)
                        {
                            Child.Add(Convert.ToInt32(room.Child1Age));
                            Child.Add(Convert.ToInt32(room.Child2Age));
                        }
                    }
                }
                Guests.number_of_adults = Adult;
                Guests.children = Child;
                Guests.number_of_rooms = SearchParam.Rooms.Count;
                RootObject.guests = Guests;


                string Request = JsonConvert.SerializeObject(RootObject, Setting);
                return Request;
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
            }
            return string.Empty;
        }

        public string Get_BookingDotCom_PriceOnly_v3(string Url, HotelSearchParameter SearchParam, string UserName, string Password, string destination, string HotelIds, string CurrencyCode, string Rating, string Nationality, string SupplierName = "")
        {
            try
            {

                string checkIn = SearchParam.CheckInDate.Substring(6, 4) + "-" + SearchParam.CheckInDate.Substring(3, 2) + "-" + SearchParam.CheckInDate.Substring(0, 2);
                string checkOut = SearchParam.CheckOutDate.Substring(6, 4) + "-" + SearchParam.CheckOutDate.Substring(3, 2) + "-" + SearchParam.CheckOutDate.Substring(0, 2);


                JsonSerializerSettings Setting = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    DefaultValueHandling = DefaultValueHandling.Ignore
                };

                if(SupplierName.ToLower() != "booking.com_v3") //(SupplierName.ToLower() == "booking.comg_v3" || SupplierName.ToLower() == "booking.com_v3_retail" )
                {

                    searchhotelnroomG.Root RootObject = new searchhotelnroomG.Root();

                    searchhotelnroomG.Booker Booker = new searchhotelnroomG.Booker();
                    searchhotelnroomG.Guests Guests = new searchhotelnroomG.Guests();

                    List<int> hoteids = new List<int>();

                    if (string.IsNullOrEmpty(HotelIds))
                    {
                        RootObject.city = destination;
                    }
                    else
                    {


                        string[] hotelIdsArray = HotelIds.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var id in hotelIdsArray)
                        {
                            string trimmedId = id.Trim();
                            int hotelId;
                            if (int.TryParse(id.Trim(), out hotelId))
                            {
                                hoteids.Add(hotelId);// Add each ID as an integer
                            }

                            // RootObject.accommodations.Add(10004); 
                        }
                    }

                    RootObject.accommodations = hoteids;
                    Booker.country = Nationality.ToLower();
                    //Booker.platform = "desktop";
                    Booker.platform = "mobile";

                    List<string> user_groups = new List<string>();

                    string user_group = "authenticated,genius";


                    string[] user_groupss = user_group.Split(',');

                    for (int ii = 0; ii < (user_groupss.Length); ii++)
                    {
                        user_groups.Add(Convert.ToString(user_groupss[ii]));
                    }

                    Booker.user_groups = user_groups;
                    RootObject.booker = Booker;


                    RootObject.checkin = checkIn;
                    RootObject.checkout = checkOut;

                    List<string> extras = new List<string>();
                    string extra = "extra_charges";
                    extras.Add(extra);
                    RootObject.extras = extras;


                    int Adult = 0;
                    List<int> Child = new List<int>();
                    foreach (var room in SearchParam.Rooms)
                    {
                        Adult += room.NoofAdults;
                        int NoofChild = room.NoofChild;
                        if (NoofChild > 0)
                        {
                            if (room.NoofChild == 1)
                            {
                                Child.Add(Convert.ToInt32(room.Child1Age));
                            }
                            else if (room.NoofChild == 2)
                            {
                                Child.Add(Convert.ToInt32(room.Child1Age));
                                Child.Add(Convert.ToInt32(room.Child2Age));
                            }
                        }
                    }
                    Guests.number_of_adults = Adult;
                    Guests.children = Child;
                    Guests.number_of_rooms = SearchParam.Rooms.Count;
                    RootObject.guests = Guests;


                    string Request = JsonConvert.SerializeObject(RootObject, Setting);
                    return Request;
                }
                else
                {

                    searchhotelnroom.Root RootObject = new searchhotelnroom.Root();

                    searchhotelnroom.Booker Booker = new searchhotelnroom.Booker();
                    searchhotelnroom.Guests Guests = new searchhotelnroom.Guests();

                    List<int> hoteids = new List<int>();

                    if (string.IsNullOrEmpty(HotelIds))
                    {
                        RootObject.city = destination;
                    }
                    else
                    {


                        string[] hotelIdsArray = HotelIds.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var id in hotelIdsArray)
                        {
                            string trimmedId = id.Trim();
                            int hotelId;
                            if (int.TryParse(id.Trim(), out hotelId))
                            {
                                hoteids.Add(hotelId);// Add each ID as an integer
                            }

                            // RootObject.accommodations.Add(10004); 
                        }
                    }

                    RootObject.accommodations = hoteids;
                    Booker.country = Nationality.ToLower();

                    //Booker.platform = "desktop";
                    Booker.platform = "mobile";

                    RootObject.booker = Booker;


                    RootObject.checkin = checkIn;
                    RootObject.checkout = checkOut;

                    List<string> extras = new List<string>();
                    string extra = "extra_charges";
                    extras.Add(extra);
                    RootObject.extras = extras;


                    int Adult = 0;
                    List<int> Child = new List<int>();
                    foreach (var room in SearchParam.Rooms)
                    {
                        Adult += room.NoofAdults;
                        int NoofChild = room.NoofChild;
                        if (NoofChild > 0)
                        {
                            if (room.NoofChild == 1)
                            {
                                Child.Add(Convert.ToInt32(room.Child1Age));
                            }
                            else if (room.NoofChild == 2)
                            {
                                Child.Add(Convert.ToInt32(room.Child1Age));
                                Child.Add(Convert.ToInt32(room.Child2Age));
                            }
                        }
                    }
                    Guests.number_of_adults = Adult;
                    Guests.children = Child;
                    Guests.number_of_rooms = SearchParam.Rooms.Count;
                    RootObject.guests = Guests;


                    string Request = JsonConvert.SerializeObject(RootObject, Setting);
                    return Request;
                }
            }
            catch (Exception ex)
            {
                _ = _exception.Error(ex);
            }
            return string.Empty;
        }


        public async Task<List<Options>> BookingDotCom_v3_HotelsearchWithRooms_bck_080725(HotelX item, CommonSearch commonSearch, string xmlCountry, string CityCode, string ResponseData, HotelSearchParameter SearchParam, double CurrencyFactor, DataSet dsAgentDetails, string Provider, DataTable dtCountryCityMarkup, Int32 AgentId, Int32 TariffID, double AgentPercentage, Boolean IsShowSupplier, int SupplierID, int MasterSupplierID, Int32 processTimeout, string ProccessCutOffTime, string ResProccessTimerLogActive, string ResProccessTimeValueforSaveLog, string StoreSupplierResErrorLog, DataTable dtROE, string xmlhotelcode, string UserName, string Password, string currencycode = "USD")
        {
            List<Options> options = new List<Options>();

            try
            {
                //var hotelIds = SearchParam.Filters.HotelIDs.Split(',').Select(int.Parse).ToList();

                //DataTable DtHotelInformation = new DataTable();
                List<RoomBooking> lstRoomDetails = new List<RoomBooking>();
                Dictionary<long, RoomBooking> roomDict = new Dictionary<long, RoomBooking>();
                if (SearchParam.Filters.IsShowRooms == 1 && false)
                {
                    var xmlhotelcode1 = item.Action.Split(',').ToList();
                    // DtHotelInformation = await _commonMethod.MongoDB_BookingC(xmlhotelcode1);
                    // lstRoomDetails = await _commonMethod.MongoDB_BookingCList(xmlhotelcode1);
                    // roomDict = lstRoomDetails.ToDictionary(r => r.roomid);

                    roomDict = await _commonMethod.MongoDB_BookingCDictionary(xmlhotelcode1);
                }

                #region DataTable
                DataTable DtHotel = _commonMethod.GenerateDataTableNewV2();
                DataTable DtBDC_HotelRoom = _commonMethod.GenerateRoomtypeDataTableNewV2();
                DataTable dtInrCurrency = new DataTable();
                #endregion

                #region PriceOnly RS
                if (ResponseData == string.Empty)
                {
                    item.ResponseType = "NoCount";
                    return options;
                }
                if (ResponseData == null)
                {
                    item.ResponseType = "NoCount";
                    return options;
                }
                #endregion

                CurrencyFactor = 1;
                //BookingDotComRoom.RootObject dsResponse = JsonConvert.DeserializeObject<BookingDotComRoom.RootObject>(ResponseData);

                reshotelsearchnroom.Rootobject res = JsonConvert.DeserializeObject<reshotelsearchnroom.Rootobject>(ResponseData);
                if (res.data == null || res.data.Length == 0)
                {
                    item.ResponseType = "NoCount";
                    return options;
                }
                currencycode = Convert.ToString(res.data[0].currency);
                if (currencycode.ToLower() != Convert.ToString(commonSearch.tokenData.SiteCurrency).ToLower())
                {
                    //dtInrCurrency = _commonMethod.GetCountryCurrency(currencycode);
                    CurrencyFactor = _commonMethod.GetROEByCurrency(Convert.ToString(currencycode), commonSearch.tokenData.dtROE);
                }
                #region Hotel List
                double FinalPrice = 0, min = 0;
                int max = 0, b = 0;
                for (int hi = 0; hi < SearchParam.Rooms.Count; hi++)
                {
                    b = (Convert.ToInt32(SearchParam.Rooms[hi].NoofAdults) + Convert.ToInt32(SearchParam.Rooms[hi].NoofChild));
                    if (hi == 0)
                        max = b;
                    else if (max < b)
                        max = b;
                }
                for (int m = 0; m <= res.data.Length - 1; m++)
                {
                    int maxoccupancy = 0;
                    //bool isValid = true;
                    FinalPrice = 0; min = 0;
                    for (int r = 0; r < res.data[m].products.Length; r++)
                    {
                        maxoccupancy = res.data[m].products[r].maximum_occupancy.total; //res.data[m].products[r].maximum_occupancy.adults + (res.data[m].products[r].maximum_occupancy.children != null ? res.data[m].products[r].maximum_occupancy.children.Count : 0);
                                                                                        //if ((res.data[m].products[r].maximum_occupancy.adults + res.data[m].products[r].maximum_occupancy.children.Count) > maxoccupancy)
                                                                                        //    maxoccupancy = (res.data[m].products[r].maximum_occupancy.adults + res.data[m].products[r].maximum_occupancy.children.Count);


                        if (maxoccupancy >= max)// && isValid
                        {

                            min = res.data[m].products[r].price.total;
                            try
                            {
                                double commision = res.data[m].products[r].commission.amount;
                                //min = min - res.result[m].commission_details.commission_amount;
                                min = min - commision;
                            }
                            catch (Exception)
                            {

                            }

                            if (FinalPrice == 0)
                                FinalPrice = min * CurrencyFactor;
                            else if ((min * CurrencyFactor) < FinalPrice)
                                FinalPrice = min * CurrencyFactor;

                            if (FinalPrice == 0)
                            {
                                item.ResponseType = "NoCount";
                                continue;
                            }
                        }
                    }
                    if (SearchParam.Filters.IsShowRooms == 0)
                    {
                        DataRow drXmlHotel = DtHotel.NewRow();
                        {
                            int RaynaId = 0;
                            //DataRow[] drGiataDetails = dsBulkdata.Tables["GiataDetails"].Select("RaynaId='" + RaynaId + "' and Rating in ('" + strRating + "')");
                            //DataRow[] drGiataDetails = dsBulkdata.Tables["GiataDetails"].Select("RaynaId='" + RaynaId + "'");
                            //if (drGiataDetails.Length > 0)
                            {
                                drXmlHotel["SupplierId"] = SupplierID;
                                drXmlHotel["HotelName"] = "";
                                drXmlHotel["MasterSupplierId"] = Convert.ToInt32(MasterSupplierID);
                                drXmlHotel["GIITACode"] = 0;
                                drXmlHotel["IsShowSupplier"] = IsShowSupplier;
                                drXmlHotel["UniqueNo"] = Convert.ToString(res.data[m].id);
                                drXmlHotel["RaynaId"] = Convert.ToString(res.data[m].id); ;
                                drXmlHotel["ContractID"] = 0;

                                drXmlHotel["OwnsystemHotelId"] = 0;
                                drXmlHotel["XMLHotelCode"] = Convert.ToString(res.data[m].id);
                                drXmlHotel["SupplierName"] = item.Supplier;
                                drXmlHotel["SupplierCurrency"] = currencycode;
                                //dr["XMLCountry"] = Country;
                                drXmlHotel["XMLCity"] = CityCode;
                                drXmlHotel["XmlHotelCity"] = CityCode;
                                drXmlHotel["FinalSellingPrice"] = FinalPrice * SearchParam.Rooms.Count;
                                drXmlHotel["SellingPrice"] = FinalPrice;

                                if (FinalPrice > 0)
                                {
                                    DtHotel.Rows.Add(drXmlHotel);

                                    #region "GetRooms for this hotel"
                                    bool willAddOrNot = true;
                                    Int64 RaynaID = 0;
                                    Int64 GiataId = 0;
                                    int RatingHotel = 0;
                                    Int64 OwnsystemHotelId = 0;
                                    // string Provider = "JacTravel";
                                    double MinPrice = (FinalPrice * CurrencyFactor);
                                    _markup.GetHotelDataCommon(ref MinPrice, ref willAddOrNot, ref OwnsystemHotelId, ref RaynaID, ref GiataId, ref RatingHotel, ref drXmlHotel, commonSearch.AgentId, commonSearch.TariffID, commonSearch.dsAgentDetails, commonSearch.supplierGiataMaster, SearchParam, commonSearch.dtCountryCityMarkup, commonSearch.B2CMarkup, commonSearch.IsParentId, commonSearch.SubAgentMarkup, commonSearch.SearchByHotel, commonSearch.HotelIDS);
                                    if (willAddOrNot)
                                    {
                                        DataTable dtHotelRoom = _commonMethod.GenerateRoomtypeDataTableNewV2();
                                        DataTable dtCancellation = _commonMethod.GenerateCancellationDataTableNewV2();
                                        DataTable DtRoomRateBreackUp = _commonMethod.GenerateoomRateBreackUpDataTableNewV2();
                                        decimal LowestPrice = Convert.ToDecimal(MinPrice).CalculatePrice(commonSearch.tokenData, commonSearch.advancedOptions, currencycode);
                                        MinPrice = Convert.ToDouble(LowestPrice);
                                        Options ops = (await _supplierRes.GetCommonRoomForSuppliers(GiataId, commonSearch.tokenData, commonSearch.advancedOptions, MinPrice, drXmlHotel, dtHotelRoom, dtCancellation, DtRoomRateBreackUp, SearchParam));
                                        if (ops != null)
                                            options.Add(ops);
                                    }
                                    #endregion
                                }
                            }

                        }
                    }
                    else
                    {
                        DataRow drXmlHotel = DtHotel.NewRow();
                        {
                            int RaynaId = 0;
                            //DataRow[] drGiataDetails = dsBulkdata.Tables["GiataDetails"].Select("RaynaId='" + RaynaId + "' and Rating in ('" + strRating + "')");
                            //DataRow[] drGiataDetails = dsBulkdata.Tables["GiataDetails"].Select("RaynaId='" + RaynaId + "'");
                            //if (drGiataDetails.Length > 0)
                            {
                                drXmlHotel["SupplierId"] = SupplierID;
                                drXmlHotel["HotelName"] = "";
                                drXmlHotel["MasterSupplierId"] = Convert.ToInt32(MasterSupplierID);
                                drXmlHotel["GIITACode"] = 0;
                                drXmlHotel["IsShowSupplier"] = IsShowSupplier;
                                drXmlHotel["UniqueNo"] = Convert.ToString(res.data[m].id);
                                drXmlHotel["RaynaId"] = Convert.ToString(res.data[m].id); ;
                                drXmlHotel["ContractID"] = 0;

                                drXmlHotel["OwnsystemHotelId"] = 0;
                                drXmlHotel["XMLHotelCode"] = Convert.ToString(res.data[m].id);
                                drXmlHotel["SupplierName"] = item.Supplier;
                                drXmlHotel["SupplierCurrency"] = currencycode;
                                //dr["XMLCountry"] = Country;
                                drXmlHotel["XMLCity"] = CityCode;
                                drXmlHotel["XmlHotelCity"] = CityCode;
                                FinalPrice = 1;
                                FinalPrice = FinalPrice;// + 1000;
                                drXmlHotel["FinalSellingPrice"] = FinalPrice * SearchParam.Rooms.Count;
                                drXmlHotel["SellingPrice"] = FinalPrice;

                                if (FinalPrice > 0)
                                {
                                    DtHotel.Rows.Add(drXmlHotel);

                                    #region "GetRooms for this hotel"
                                    bool willAddOrNot = true;
                                    Int64 RaynaID = 0;
                                    Int64 GiataId = 0;
                                    int RatingHotel = 0;
                                    Int64 OwnsystemHotelId = 0;
                                    // string Provider = "JacTravel";
                                    double MinPrice = (FinalPrice * CurrencyFactor);
                                    _markup.GetHotelDataCommon(ref MinPrice, ref willAddOrNot, ref OwnsystemHotelId, ref RaynaID, ref GiataId, ref RatingHotel, ref drXmlHotel, commonSearch.AgentId, commonSearch.TariffID, commonSearch.dsAgentDetails, commonSearch.supplierGiataMaster, SearchParam, commonSearch.dtCountryCityMarkup, commonSearch.B2CMarkup, commonSearch.IsParentId, commonSearch.SubAgentMarkup, commonSearch.SearchByHotel, commonSearch.HotelIDS);
                                    if (willAddOrNot)
                                    {
                                        #region DataTable
                                        DtBDC_HotelRoom = _commonMethod.GenerateRoomtypeDataTableNewV2();
                                        DataTable dtBDC_Cancellation = _commonMethod.GenerateCancellationDataTableNewV2();
                                        DataTable DtBDC_RateBreackUp = _commonMethod.GenerateoomRateBreackUpDataTableNewV2();
                                        CurrencyFactor = 1;
                                        DataSet dsTemp = new DataSet();
                                        #endregion

                                        #region RoomOnly RQ

                                        string Error = "";
                                        int CutOffDays = XMLHotelBookingCommon.CutOffDays;
                                        CutOffDays = _commonMethod.GetCutOffDays(commonSearch.dtSuplierMarkup, Provider);
                                        double SubAgentMarkup = 0;
                                        int IsParentId = 1, agentID = 0;
                                        Boolean blnIsShowSupplier = false;
                                        double AgentMarkupAmount = 0.0;
                                        Boolean isPerce = true;

                                        string Nationality = xmlCountry;

                                        double AgentCompanyPercentage = Convert.ToDouble(SearchParam.AgentCompanyPercentage);
                                        AgentPercentage = 0;

                                        int NoOfNight = SearchParam.NoofNights;

                                        _markup.getAgetMarkupSetting(Provider, Convert.ToString(GiataId), commonSearch.dtSuplierMarkup, commonSearch.dtMarkupDetails, ref AgentPercentage, ref AgentMarkupAmount, ref blnIsShowSupplier, ref isPerce, agentID, RatingHotel, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), Convert.ToString(SearchParam.CheckInDate));

                                        int totalAdult = 0;
                                        int totalChild = 0;
                                        string Meal = "Room Only";
                                        double VatonfirstnightPrice = 0;
                                        for (int c = 0; c < SearchParam.Rooms.Count; c++)
                                        {
                                            totalAdult = Convert.ToInt32(SearchParam.Rooms[c].NoofAdults) + totalAdult;
                                            totalChild = Convert.ToInt32(SearchParam.Rooms[c].NoofChild) + totalChild;
                                        }
                                        #endregion
                                        xmlhotelcode = Convert.ToString(res.data[m].id);

                                        string ResponseDataa = "";
                                        {
                                            reshotelsearchnroom.Rootobject dsResponse = JsonConvert.DeserializeObject<reshotelsearchnroom.Rootobject>(ResponseData);

                                            #region Room Details
                                            //string reqId = Convert.ToString(DateTime.Now.Ticks);
                                            //string bookingdotcomUrl = "https://demandapi.booking.com/3.1/accommodations/details";
                                            //string Request = Common.HotelRoomDetailsRequestBookingdotcomv3(Convert.ToInt32(xmlhotelcode));
                                            //ResponseData = BookingDotComv3Response(ref Error, Request, bookingdotcomUrl, UserName, Password, 120000);
                                            //Common.XMLRequestResponseTrack(bookingdotcomUrl + "/" + Request, ResponseData, Provider, "Proper", reqId, SearchParam.SearchKey);


                                            //detailres.Root dsResponsedetail = JsonConvert.DeserializeObject<detailres.Root>(ResponseData);



                                            //string mongoConnectionString = "mongodb://10.32.8.96:27017";
                                            //string mongoDatabaseName = "Booking_com_RoomMapping";
                                            //string mongoCollectionName = "Booking_com_ROOM_Mapping_Xconnect";



                                            //var hotelIds = SearchParam.Filters.HotelIDs.Split(',').Select(int.Parse).ToList();
                                            //var xmlhotelcode1 = xmlhotelcode.Split(',').ToList();

                                            //try
                                            //{
                                            //    // Initialize MongoDB client and get the collection
                                            //    var client = new MongoClient(mongoConnectionString);
                                            //    var database = client.GetDatabase(mongoDatabaseName);
                                            //    var collection = database.GetCollection<BsonDocument>(mongoCollectionName);

                                            //    // Create filter using $in operator
                                            //    var filter = Builders<BsonDocument>.Filter.In("XmlId", xmlhotelcode1);

                                            //    // Fetch the matching records
                                            //    var results = collection.Find(filter).ToList();

                                            //    /// Insert results into a DataTable
                                            //    DtHotelInformation = _commonMethod.ConvertToDataTable(results);

                                            //}
                                            //catch (Exception ex)
                                            //{
                                            //    //DtHotelInformation = new DataTable();
                                            //    await _exception.Error(ex);
                                            //}

                                            //DtHotelInformation = await _commonMethod.MongoDB_BookingC(xmlhotelcode1);
                                            #endregion

                                            var roomNameMapping = new Dictionary<int, string>();
                                            var roomDescriptionMapping = new Dictionary<int, string>();
                                            //foreach (var hotel in dsResponsedetail.data)
                                            //{
                                            //    foreach (var room in hotel.rooms)
                                            //    {
                                            //        if (room.name != null && room.name.engb != "")
                                            //        {
                                            //            roomNameMapping[room.id] = room.name.engb;
                                            //            roomDescriptionMapping[room.id] = room.description.engb;
                                            //        }
                                            //    }
                                            //}
                                            {
                                                if (dsResponse.data.Length > 0)
                                                {
                                                    currencycode = Convert.ToString(dsResponse.data[0].currency);
                                                    {

                                                        if (currencycode.ToLower() != Convert.ToString(commonSearch.tokenData.SiteCurrency).ToLower())
                                                        {
                                                            //dtInrCurrency = _commonMethod.GetCountryCurrency(currencycode);
                                                            CurrencyFactor = _commonMethod.GetROEByCurrency(Convert.ToString(currencycode), commonSearch.tokenData.dtROE);
                                                        }
                                                    }
                                                }

                                                {
                                                    //string RoomName = "";
                                                    string hotel_url = Convert.ToString(dsResponse.data[m].deep_link_url);
                                                    int hotel_id = Convert.ToInt32(dsResponse.data[m].id);
                                                    int t = 0, maxx = 0;
                                                    var result = SearchParam.Rooms.GroupBy(x => new { x.NoofAdults, x.NoofChild, x.Child1Age, x.Child2Age }).Select(n => new { NoofAdults = n.Key.NoofAdults, NoofChild = n.Key.NoofChild, ChildAge = n.ToList(), Count = n.Count() });
                                                    {

                                                        int detailroomid = 0;
                                                        string Roomname = "";
                                                        for (int RO = 0; RO < dsResponse.data[m].products.Length; RO++)
                                                        {
                                                            //Added on 2-JAN-25
                                                            if (_keySettings.IsRoomReturnActive && SearchParam.Rooms.Count == 1 && (RO + 1) > item.ReturnRooms && (DtBDC_HotelRoom.Rows.Count == item.ReturnRooms)) //18-07-25
                                                                break;

                                                            int Room_Id = Convert.ToInt32(dsResponse.data[m].products[RO].room);

                                                            #region Comment code
                                                            //DataRow[] Drroom = DtHotelInformation.Select("roomid = " + Room_Id);
                                                            //DataRow[] Drroom = DtHotelInformation.Select("roomid = '" + Room_Id + "'");

                                                            //if (Drroom.Length > 0)
                                                            //{
                                                            //    Roomname = Drroom[0]["roomname"].ToString();
                                                            //}
                                                            //else
                                                            //{
                                                            //    continue; // or handle as needed
                                                            //}

                                                            //var room = lstRoomDetails.FirstOrDefault(r => r.roomid == Room_Id);

                                                            //if (room != null)
                                                            //{
                                                            //    Roomname = room.roomname;
                                                            //}
                                                            //else
                                                            //{
                                                            //    continue; // or handle as needed
                                                            //}

                                                            //foreach (var kvp in roomNameMapping)
                                                            //{
                                                            //    detailroomid = kvp.Key;
                                                            //    Roomname = kvp.Value;
                                                            //    if (detailroomid == Room_Id)
                                                            //    {
                                                            //        RoomName = Convert.ToString(Roomname);

                                                            //    }

                                                            //}

                                                            #endregion

                                                            if (roomDict.TryGetValue(Room_Id, out var room))
                                                            {
                                                                Roomname = room.roomname;
                                                            }
                                                            else
                                                            {
                                                                continue; // or handle as needed
                                                            }




                                                            string block_id = Convert.ToString(dsResponse.data[m].products[RO].id);

                                                            int max_occupancy = Convert.ToInt32(dsResponse.data[m].products[RO].maximum_occupancy.total);

                                                            int RoomCount = 1;
                                                            int totalrooms = Convert.ToInt32(dsResponse.data[m].products[RO].number_available);
                                                            if (totalrooms < SearchParam.Rooms.Count)
                                                                continue;

                                                            for (int hi = 0; hi < SearchParam.Rooms.Count; hi++)
                                                            {
                                                                totalAdult = Convert.ToInt32(SearchParam.Rooms[hi].NoofAdults);
                                                                totalChild = Convert.ToInt32(SearchParam.Rooms[hi].NoofChild);

                                                                string e = "";
                                                                b = (Convert.ToInt32(SearchParam.Rooms[hi].NoofAdults) + Convert.ToInt32(SearchParam.Rooms[hi].NoofChild));
                                                                int AdultC = Convert.ToInt32(SearchParam.Rooms[hi].NoofAdults);

                                                                if (b <= max_occupancy && AdultC <= max_occupancy)
                                                                {
                                                                    RoomCount++;

                                                                    if (hi == 0)
                                                                        maxx = t;
                                                                    else if (maxx < t)
                                                                        maxx = t;


                                                                    double Final_Rate = Convert.ToDouble(dsResponse.data[m].products[RO].price.total), ItemPrice = 0;
                                                                    double CompanyBuying = 0, AgentSelling = 0;
                                                                    CompanyBuying = Final_Rate * CurrencyFactor;
                                                                    double charge_price = 0; //Convert.ToDouble(dsResponse.data[R].products[RO].price.extra_charges.excluded[0].total_amount);
                                                                    string CurrencY = Convert.ToString(dsResponse.data[m].currency);
                                                                    double ExtraCharge = 0, amount = 0, RoomRateForCompany = 0, RoomRateForAgent = 0, BeforeCommisionRoomRateForAgent = 0;
                                                                    int P = 0;

                                                                    RoomRateForAgent = CompanyBuying;
                                                                    BeforeCommisionRoomRateForAgent = RoomRateForAgent;


                                                                    double Profit = Convert.ToDouble(dsResponse.data[m].products[RO].commission.amount) * CurrencyFactor;
                                                                    RoomRateForAgent = RoomRateForAgent - Profit;



                                                                    string currency = Convert.ToString(dsResponse.data[m].currency);
                                                                    string paytiming = "";
                                                                    if (Convert.ToString(dsResponse.data[m].products[RO].policies) != null)
                                                                    {

                                                                        #region Meal
                                                                        var meals = dsResponse.data[m].products[RO].policies.meal_plan.meals;

                                                                        if (meals != null && meals.Length > 0)
                                                                        {
                                                                            Meal = Convert.ToString(meals[0]);
                                                                        }
                                                                        else
                                                                        {
                                                                            Meal = "Room Only";
                                                                        }
                                                                        bool payments = Convert.ToBoolean(dsResponse.data[m].products[RO].policies.payment.prepayment_required);
                                                                        if (payments)
                                                                        {
                                                                            if (dsResponse.data[m].products[RO].policies.payment.timings.Length > 0)
                                                                            {
                                                                                for (int j = 0; j < dsResponse.data[m].products[RO].policies.payment.timings.Length; j++)
                                                                                {
                                                                                    paytiming += Convert.ToString(dsResponse.data[m].products[RO].policies.payment.timings[j]) + ",";
                                                                                }
                                                                            }
                                                                            paytiming = paytiming.Substring(0, paytiming.Length - 1);
                                                                        }
                                                                        string refundable = Convert.ToString(dsResponse.data[m].products[RO].policies.cancellation.type);
                                                                        Roomname = Roomname;// + "-" + refundable;
                                                                        if (paytiming == "")
                                                                        {
                                                                            paytiming = "pay_online_now";
                                                                        }
                                                                        else
                                                                        {
                                                                            if (refundable == "" || refundable.ToLower().Contains("non_refundable"))
                                                                            {
                                                                                paytiming = "pay_online_now";
                                                                            }
                                                                            else
                                                                            {
                                                                                paytiming = "pay_online_later";
                                                                            }
                                                                        }
                                                                        #endregion
                                                                        string RoomTypeId = Room_Id + "$$$" + Roomname + "$$$" + currency + "$$$" + Meal + "$$$" + currencycode + "$$$" + Final_Rate + "$$$" + block_id + "$$$" + Nationality + "$$$" + Guid.NewGuid().ToString() + "$$$" + paytiming;

                                                                        #region RateBreakUp
                                                                        for (int n = 0; n < SearchParam.NoofNights; n++)
                                                                        {
                                                                            DateTime Day = DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null);
                                                                            Day = Day.AddDays(n);
                                                                            string Days = Convert.ToString(Day);
                                                                            string rateWeekColumn = Convert.ToString(Day.Date.DayOfWeek).Substring(0, 3);
                                                                            double FRate = RoomRateForAgent;
                                                                            RoomRateForCompany = CompanyBuying;
                                                                            //RoomRateForAgent = AgentSelling;

                                                                            #region Vat
                                                                            Double VatOnRateBrackUp = 0;
                                                                            FRate = _markup.ProcessHotelRateBreakupPrice_WithMarkup(1, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), RoomRateForAgent / NoOfNight, commonSearch.B2CMarkup, IsParentId, isPerce, AgentPercentage, AgentMarkupAmount, SubAgentMarkup, ref VatOnRateBrackUp);
                                                                            #endregion

                                                                            VatonfirstnightPrice += VatOnRateBrackUp;
                                                                            DataRow drRateBreackup = DtBDC_RateBreackUp.NewRow();
                                                                            drRateBreackup["SupplierName"] = Provider;
                                                                            drRateBreackup["OwnsystemHotelId"] = OwnsystemHotelId;
                                                                            drRateBreackup["ContractID"] = 0;
                                                                            drRateBreackup["RoomTypeName"] = "[R-" + (hi + 1) + "]" + Roomname;
                                                                            drRateBreackup["RoomTypeId"] = RoomTypeId;
                                                                            drRateBreackup["MealId"] = Meal;
                                                                            drRateBreackup["Meal"] = Meal;
                                                                            drRateBreackup["DateOfWeek"] = rateWeekColumn;
                                                                            drRateBreackup["Date"] = Day;
                                                                            drRateBreackup["Rate"] = FRate;
                                                                            drRateBreackup["RateType"] = "[R-" + (hi + 1) + "]" + Roomname;
                                                                            drRateBreackup["RoomNo"] = hi + 1;//r + 1;
                                                                            drRateBreackup["RoomSequenceNo"] = "";
                                                                            drRateBreackup["BookingStatus"] = "Available";
                                                                            drRateBreackup["SupplierCurrency"] = currencycode;
                                                                            drRateBreackup["BuyingPrice"] = FRate / CurrencyFactor;
                                                                            drRateBreackup["VatdiffAmount"] = VatOnRateBrackUp;
                                                                            drRateBreackup["OrderNo"] = 1;
                                                                            DtBDC_RateBreackUp.Rows.Add(drRateBreackup);
                                                                        }


                                                                        #endregion

                                                                        #region Room
                                                                        // IncrementalPrice = IncrementalPrice * CurrencyFactor;
                                                                        DataRow drRoom = DtBDC_HotelRoom.NewRow();
                                                                        drRoom["OwnSystemHotelId"] = OwnsystemHotelId;
                                                                        drRoom["RoomTypeId"] = RoomTypeId;
                                                                        drRoom["RoomTypeCode"] = Room_Id;
                                                                        drRoom["RoomTypeName"] = Roomname;
                                                                        drRoom["SupplierName"] = Provider;
                                                                        drRoom["BookingStatus"] = "Available";
                                                                        drRoom["BoardType"] = Meal;
                                                                        drRoom["RoomSequenceNo"] = "";
                                                                        drRoom["ContractID"] = 0;
                                                                        //drRoom["EssentialInformation"] = EssentialInfo + "<br/><b>Payment Timing:</b>" + paytiming; //RoomDescription + e;//Convert.ToString(dsResponse.result[h].block[i].room_description);
                                                                        drRoom["EssentialInformation"] = "";
                                                                        drRoom["MealId"] = Meal;
                                                                        drRoom["MealName"] = Meal;
                                                                        drRoom["MaxExtraBed"] = "";//ExtraBed;
                                                                        drRoom["RoomNo"] = hi + 1;//r + 1;
                                                                        drRoom["Type"] = "";
                                                                        drRoom["UniqueNo"] = "";
                                                                        drRoom["Characteristic"] = "";
                                                                        drRoom["IsRefundable"] = refundable;
                                                                        drRoom["OfferCode"] = "";
                                                                        drRoom["OfferDescription"] = "";
                                                                        drRoom["AdultsOccupancy"] = totalAdult;
                                                                        drRoom["ChildOccupancy"] = totalChild;
                                                                        drRoom["OurProfit"] = Profit;// 0;// IncrementalPrice;
                                                                        drRoom["BoardTypeID"] = Convert.ToString(Meal);
                                                                        drRoom["CompanyBuyingPriceExpedia"] = 0;//pricewithoutFactor;
                                                                        if (RoomRateForAgent > 0)
                                                                        {
                                                                            ItemPrice = RoomRateForAgent;
                                                                            //Common.ProcessHotelRoom_WithMarkupForBookingDotCom(CurrencyFactor, currencycode, false, false, SearchParam.NoofNights, SearchParam.CityID, SearchParam.CountryId, SearchParam.CheckInDate, SearchParam.CheckOutDate, ref RoomRateForCompany, ref drRoom, isPerce, AgentPercentage, AgentMarkupAmount, SubAgentMarkup, B2CMarkup, IsParentId, ItemPrice);
                                                                            //Common.ProcessHotelRoom_WithMarkup(CurrencyFactor, currencycode, false, false, SearchParam.NoofNights, SearchParam.CityID, SearchParam.CountryId, SearchParam.CheckInDate, SearchParam.CheckOutDate, ref ItemPrice, ref drRoom, isPerce, AgentPercentage, AgentMarkupAmount, SubAgentMarkup, B2CMarkup, IsParentId);
                                                                            _markup.ProcessHotelRoom_WithMarkup_BookingDotComCommision(CurrencyFactor, currencycode, false, false, SearchParam.NoofNights, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), SearchParam.CheckInDate, SearchParam.CheckOutDate, ref ItemPrice, ref drRoom, isPerce, AgentPercentage, AgentMarkupAmount, SubAgentMarkup, commonSearch.B2CMarkup, IsParentId, BeforeCommisionRoomRateForAgent);

                                                                        }
                                                                        DtBDC_HotelRoom.Rows.Add(drRoom);
                                                                        #endregion

                                                                        #region Cancellation Policy

                                                                        DateTime canceldate = DateTime.Now;
                                                                        double AgentCancelCharge = 0;
                                                                        double pri = 0;
                                                                        double VatOnPolicy = 0;
                                                                        string checkin = Convert.ToString(SearchParam.CheckInDate);
                                                                        checkin = checkin.Substring(6, 4) + "-" + checkin.Substring(3, 2) + "-" + checkin.Substring(0, 2);

                                                                        #region Offer, Refunable - Non Refundable



                                                                        //taxes + "-" + RoomDescription + "-" + e + "<br/>" + Essential;
                                                                        RoomTypeId = RoomTypeId.Replace("'", "");

                                                                        #endregion



                                                                        string Refundable = Convert.ToString(dsResponse.data[m].products[RO].policies.cancellation.type);

                                                                        if (Refundable == "" || Refundable.ToLower().Contains("non_refundable"))
                                                                        {
                                                                            AgentCancelCharge = ItemPrice;


                                                                            dtBDC_Cancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref RoomRateForCompany, dtBDC_Cancellation.NewRow(), CurrencyFactor, currencycode, Provider, Convert.ToString(OwnsystemHotelId), Convert.ToString(hi + 1), RoomTypeId, "[R-" + (hi + 1) + "]" + Roomname, Meal, DateTime.Now.Date, Convert.ToDateTime(checkin), AgentCancelCharge, AgentCancelCharge, Meal, VatonfirstnightPrice, "", "Yes", "", "", "", ""));
                                                                            dtBDC_Cancellation.AcceptChanges();

                                                                        }
                                                                        else
                                                                        {
                                                                            DateTime FromDate = DateTime.Now;
                                                                            if (dsResponse.data[m].products[RO].policies.cancellation.free_cancellation_until != null)
                                                                            {
                                                                                FromDate = Convert.ToDateTime(dsResponse.data[m].products[RO].policies.cancellation.free_cancellation_until);
                                                                            }

                                                                            string CancelDeadlinewithoutCutoff = Convert.ToString(FromDate);
                                                                            canceldate = FromDate;
                                                                            pri = 0;
                                                                            VatOnPolicy = 0;
                                                                            double fee = RoomRateForAgent;
                                                                            double companyFee = RoomRateForCompany;
                                                                            pri = fee;
                                                                            fee = _markup.ProcessHotelCancelationPrice_WithMarkup(ref pri, SearchParam.NoofNights, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), fee, isPerce, commonSearch.B2CMarkup, AgentPercentage, AgentMarkupAmount, IsParentId, SubAgentMarkup, ref VatOnPolicy);



                                                                            if (canceldate.AddDays(CutOffDays) > DateTime.Now.Date)
                                                                                canceldate = canceldate.AddDays(CutOffDays);
                                                                            else
                                                                                canceldate = DateTime.Now.Date;
                                                                            if (canceldate > DateTime.Now.Date)
                                                                            {
                                                                                double zero = 0;
                                                                                dtBDC_Cancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref zero, dtBDC_Cancellation.NewRow(), CurrencyFactor, currencycode, Provider, Convert.ToString(OwnsystemHotelId), Convert.ToString(hi + 1), RoomTypeId, "[R-" + (hi + 1) + "]" + Roomname, Meal, DateTime.Now.Date, canceldate.AddDays(-1), AgentCancelCharge, AgentCancelCharge, Meal, VatonfirstnightPrice, CancelDeadlinewithoutCutoff, "No", "", "", "", ""));
                                                                                dtBDC_Cancellation.AcceptChanges();

                                                                                AgentCancelCharge = ItemPrice;


                                                                                dtBDC_Cancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref RoomRateForCompany, dtBDC_Cancellation.NewRow(), CurrencyFactor, currencycode, Provider, Convert.ToString(OwnsystemHotelId), Convert.ToString(hi + 1), RoomTypeId, "[R-" + (hi + 1) + "]" + Roomname, Meal, canceldate, Convert.ToDateTime(checkin), AgentCancelCharge, AgentCancelCharge, Meal, VatonfirstnightPrice, CancelDeadlinewithoutCutoff, "No", "", "", "", ""));
                                                                                dtBDC_Cancellation.AcceptChanges();
                                                                            }
                                                                            else
                                                                            {
                                                                                AgentCancelCharge = ItemPrice;


                                                                                dtBDC_Cancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref RoomRateForCompany, dtBDC_Cancellation.NewRow(), CurrencyFactor, currencycode, Provider, Convert.ToString(OwnsystemHotelId), Convert.ToString(hi + 1), RoomTypeId, "[R-" + (hi + 1) + "]" + Roomname, Meal, DateTime.Now.Date, Convert.ToDateTime(checkin), AgentCancelCharge, AgentCancelCharge, Meal, VatonfirstnightPrice, CancelDeadlinewithoutCutoff, "Yes", "", "", "", ""));
                                                                                dtBDC_Cancellation.AcceptChanges();
                                                                            }
                                                                            #endregion
                                                                        }
                                                                    }
                                                                    else
                                                                    {
                                                                        string Name = "";//dsResponse.data[R].products[RO].name;
                                                                    }



                                                                }
                                                                else
                                                                {
                                                                    // return null;
                                                                    continue;
                                                                }
                                                            }
                                                        }


                                                    }
                                                }
                                            }


                                        }
                                        decimal LowestPrice = Convert.ToDecimal(MinPrice).CalculatePrice(commonSearch.tokenData, commonSearch.advancedOptions, currencycode);
                                        MinPrice = Convert.ToDouble(LowestPrice);
                                        //Options ops = (await _supplierRes.GetCommonRoomForSuppliers(GiataId, commonSearch.tokenData, commonSearch.advancedOptions, MinPrice, drXmlHotel, DtBDC_HotelRoom, dtBDC_Cancellation, DtBDC_RateBreackUp, SearchParam));
                                        Options ops = (await _supplierRes.GetCommonRoomForSuppliers(GiataId, commonSearch.tokenData, commonSearch.advancedOptions, MinPrice, drXmlHotel, DtBDC_HotelRoom, dtBDC_Cancellation, DtBDC_RateBreackUp, SearchParam, _keySettings.IsRoomReturnActive, item.ReturnRooms, 1));
                                        if (ops != null)
                                            options.Add(ops);
                                    }
                                    #endregion
                                }
                            }

                        }

                    }
                }
                #endregion

            }
            catch (Exception ex)
            {
                await _exception.Error(ex);
            }
            return options;
        }

        public async Task<List<Options>> BookingDotCom_v3_HotelsearchWithRooms(HotelX item, CommonSearch commonSearch, string xmlCountry, string CityCode, string ResponseData, HotelSearchParameter SearchParam, double CurrencyFactor, DataSet dsAgentDetails, string Provider, DataTable dtCountryCityMarkup, Int32 AgentId, Int32 TariffID, double AgentPercentage, Boolean IsShowSupplier, int SupplierID, int MasterSupplierID, Int32 processTimeout, string ProccessCutOffTime, string ResProccessTimerLogActive, string ResProccessTimeValueforSaveLog, string StoreSupplierResErrorLog, DataTable dtROE, string xmlhotelcode, string UserName, string Password, string currencycode = "USD")
        {
            System.Diagnostics.Stopwatch timerroomx = new System.Diagnostics.Stopwatch();
            timerroomx.Start();
            List<Options> options = new List<Options>();

            try
            {
                //var hotelIds = SearchParam.Filters.HotelIDs.Split(',').Select(int.Parse).ToList();

                //DataTable DtHotelInformation = new DataTable();
                List<RoomBooking> lstRoomDetails = new List<RoomBooking>();
                Dictionary<long, RoomBooking> roomDict = new Dictionary<long, RoomBooking>();
                if (SearchParam.Filters.IsShowRooms == 1)
                {
                    var xmlhotelcode1 = item.Action.Split(',').ToList();
                    // DtHotelInformation = await _commonMethod.MongoDB_BookingC(xmlhotelcode1);
                    // lstRoomDetails = await _commonMethod.MongoDB_BookingCList(xmlhotelcode1);
                    // roomDict = lstRoomDetails.ToDictionary(r => r.roomid);

                    roomDict = await _commonMethod.MongoDB_BookingCDictionary(xmlhotelcode1);
                }

                #region DataTable
                DataTable DtHotel = _commonMethod.GenerateDataTableNewV2();
                DataTable DtBDC_HotelRoom = _commonMethod.GenerateRoomtypeDataTableNewV2();
                DataTable dtInrCurrency = new DataTable();
                #endregion

                #region PriceOnly RS
                if (ResponseData == null || ResponseData == string.Empty)
                {
                    item.ResponseStatus = 204;
                    item.ResponseType = "NoCount";
                    return options;
                }
                #endregion


                //BookingDotComRoom.RootObject dsResponse = JsonConvert.DeserializeObject<BookingDotComRoom.RootObject>(ResponseData);

                reshotelsearchnroom.Rootobject dsResponse = JsonConvert.DeserializeObject<reshotelsearchnroom.Rootobject>(ResponseData);
                if (dsResponse.data == null || dsResponse.data.Length == 0)
                {
                    item.SupplierResponseError = ResponseData;
                    item.ResponseStatus = 404;
                    item.ResponseType = "NoCount";
                    return options;
                }

                #region Hotel List
                double FinalPrice = 0, min = 0;
                int max = 0, b = 0;
                for (int hi = 0; hi < SearchParam.Rooms.Count; hi++)
                {
                    b = (Convert.ToInt32(SearchParam.Rooms[hi].NoofAdults) + Convert.ToInt32(SearchParam.Rooms[hi].NoofChild));
                    if (hi == 0)
                        max = b;
                    else if (max < b)
                        max = b;
                }
                for (int m = 0; m <= dsResponse.data.Length - 1; m++)
                {
                    if (m == 0)
                        item.ResponseType = "Success"; //Added on 8-AUG-2025 Ankit

                    CurrencyFactor = 1;

                    currencycode = Convert.ToString(dsResponse.data[m].currency);
                    if (currencycode.ToLower() != Convert.ToString(commonSearch.tokenData.SiteCurrency).ToLower())
                    {
                        //dtInrCurrency = _commonMethod.GetCountryCurrency(currencycode);
                        CurrencyFactor = _commonMethod.GetROEByCurrency(Convert.ToString(currencycode), commonSearch.tokenData.dtROE);
                    }

                    int maxoccupancy = 0;
                    //bool isValid = true;
                    FinalPrice = 0; min = 0;
                    for (int r = 0; r < dsResponse.data[m].products.Length; r++)
                    {
                        maxoccupancy = dsResponse.data[m].products[r].maximum_occupancy.total; //res.data[m].products[r].maximum_occupancy.adults + (res.data[m].products[r].maximum_occupancy.children != null ? res.data[m].products[r].maximum_occupancy.children.Count : 0);
                                                                                               //if ((res.data[m].products[r].maximum_occupancy.adults + res.data[m].products[r].maximum_occupancy.children.Count) > maxoccupancy)
                                                                                               //    maxoccupancy = (res.data[m].products[r].maximum_occupancy.adults + res.data[m].products[r].maximum_occupancy.children.Count);


                        if (maxoccupancy >= max)// && isValid
                        {

                            min = dsResponse.data[m].products[r].price.total;
                            try
                            {
                                double commision = dsResponse.data[m].products[r].commission.amount;
                                //min = min - res.result[m].commission_details.commission_amount;
                                min = min - commision;
                            }
                            catch (Exception)
                            {

                            }

                            if (FinalPrice == 0)
                                FinalPrice = min * CurrencyFactor;
                            else if ((min * CurrencyFactor) < FinalPrice)
                                FinalPrice = min * CurrencyFactor;

                            if (FinalPrice == 0)
                            {
                                item.ResponseType = "NoCount";
                                continue;
                            }
                        }
                    }

                    if (SearchParam.Filters.IsShowRooms == 0)
                    {
                        DataRow drXmlHotel = DtHotel.NewRow();
                        {
                            int RaynaId = 0;
                            //DataRow[] drGiataDetails = dsBulkdata.Tables["GiataDetails"].Select("RaynaId='" + RaynaId + "' and Rating in ('" + strRating + "')");
                            //DataRow[] drGiataDetails = dsBulkdata.Tables["GiataDetails"].Select("RaynaId='" + RaynaId + "'");
                            //if (drGiataDetails.Length > 0)
                            {
                                drXmlHotel["SupplierId"] = SupplierID;
                                drXmlHotel["HotelName"] = "";
                                drXmlHotel["MasterSupplierId"] = Convert.ToInt32(MasterSupplierID);
                                drXmlHotel["GIITACode"] = 0;
                                drXmlHotel["IsShowSupplier"] = IsShowSupplier;
                                drXmlHotel["UniqueNo"] = Convert.ToString(dsResponse.data[m].id);
                                drXmlHotel["RaynaId"] = Convert.ToString(dsResponse.data[m].id); ;
                                drXmlHotel["ContractID"] = 0;

                                drXmlHotel["OwnsystemHotelId"] = 0;
                                drXmlHotel["XMLHotelCode"] = Convert.ToString(dsResponse.data[m].id);
                                drXmlHotel["SupplierName"] = item.Supplier;
                                drXmlHotel["SupplierCurrency"] = currencycode;
                                //dr["XMLCountry"] = Country;
                                drXmlHotel["XMLCity"] = CityCode;
                                drXmlHotel["XmlHotelCity"] = CityCode;
                                drXmlHotel["FinalSellingPrice"] = FinalPrice * SearchParam.Rooms.Count;
                                drXmlHotel["SellingPrice"] = FinalPrice;

                                if (FinalPrice > 0)
                                {

                                    DtHotel.Rows.Add(drXmlHotel);

                                    #region "GetRooms for this hotel"
                                    bool willAddOrNot = true;
                                    Int64 RaynaID = 0;
                                    Int64 GiataId = 0;
                                    int RatingHotel = 0;
                                    Int64 OwnsystemHotelId = 0;
                                    // string Provider = "JacTravel";
                                    double MinPrice = (FinalPrice * CurrencyFactor);
                                    _markup.GetHotelDataCommon(ref MinPrice, ref willAddOrNot, ref OwnsystemHotelId, ref RaynaID, ref GiataId, ref RatingHotel, ref drXmlHotel, commonSearch.AgentId, commonSearch.TariffID, commonSearch.dsAgentDetails, commonSearch.supplierGiataMaster, SearchParam, commonSearch.dtCountryCityMarkup, commonSearch.B2CMarkup, commonSearch.IsParentId, commonSearch.SubAgentMarkup, commonSearch.SearchByHotel, commonSearch.HotelIDS);
                                    if (willAddOrNot)
                                    {
                                        DataTable dtHotelRoom = _commonMethod.GenerateRoomtypeDataTableNewV2();
                                        DataTable dtCancellation = _commonMethod.GenerateCancellationDataTableNewV2();
                                        DataTable DtRoomRateBreackUp = _commonMethod.GenerateoomRateBreackUpDataTableNewV2();
                                        decimal LowestPrice = Convert.ToDecimal(MinPrice).CalculatePrice(commonSearch.tokenData, commonSearch.advancedOptions, currencycode);
                                        MinPrice = Convert.ToDouble(LowestPrice);
                                        Options ops = (await _supplierRes.GetCommonRoomForSuppliers(GiataId, commonSearch.tokenData, commonSearch.advancedOptions, MinPrice, drXmlHotel, dtHotelRoom, dtCancellation, DtRoomRateBreackUp, SearchParam));
                                        if (ops != null)
                                            options.Add(ops);
                                    }
                                    #endregion
                                }
                            }

                        }
                    }
                    else
                    {
                        DataRow drXmlHotel = DtHotel.NewRow();
                        {
                            int RaynaId = 0;
                            //DataRow[] drGiataDetails = dsBulkdata.Tables["GiataDetails"].Select("RaynaId='" + RaynaId + "' and Rating in ('" + strRating + "')");
                            //DataRow[] drGiataDetails = dsBulkdata.Tables["GiataDetails"].Select("RaynaId='" + RaynaId + "'");
                            //if (drGiataDetails.Length > 0)
                            {
                                drXmlHotel["SupplierId"] = SupplierID;
                                drXmlHotel["HotelName"] = "";
                                drXmlHotel["MasterSupplierId"] = Convert.ToInt32(MasterSupplierID);
                                drXmlHotel["GIITACode"] = 0;
                                drXmlHotel["IsShowSupplier"] = IsShowSupplier;
                                drXmlHotel["UniqueNo"] = Convert.ToString(dsResponse.data[m].id);
                                drXmlHotel["RaynaId"] = Convert.ToString(dsResponse.data[m].id); ;
                                drXmlHotel["ContractID"] = 0;

                                drXmlHotel["OwnsystemHotelId"] = 0;
                                drXmlHotel["XMLHotelCode"] = Convert.ToString(dsResponse.data[m].id);
                                drXmlHotel["SupplierName"] = item.Supplier;
                                drXmlHotel["SupplierCurrency"] = currencycode;
                                //dr["XMLCountry"] = Country;
                                drXmlHotel["XMLCity"] = CityCode;
                                drXmlHotel["XmlHotelCity"] = CityCode;
                                FinalPrice = 1;
                                FinalPrice = FinalPrice;// + 1000;
                                drXmlHotel["FinalSellingPrice"] = FinalPrice * SearchParam.Rooms.Count;
                                drXmlHotel["SellingPrice"] = FinalPrice;

                                if (FinalPrice > 0)
                                {

                                    DtHotel.Rows.Add(drXmlHotel);

                                    #region "GetRooms for this hotel"
                                    bool willAddOrNot = true;
                                    Int64 RaynaID = 0;
                                    Int64 GiataId = 0;
                                    int RatingHotel = 0;
                                    Int64 OwnsystemHotelId = 0;
                                    // string Provider = "JacTravel";
                                    double MinPrice = (FinalPrice * CurrencyFactor);
                                    _markup.GetHotelDataCommon(ref MinPrice, ref willAddOrNot, ref OwnsystemHotelId, ref RaynaID, ref GiataId, ref RatingHotel, ref drXmlHotel, commonSearch.AgentId, commonSearch.TariffID, commonSearch.dsAgentDetails, commonSearch.supplierGiataMaster, SearchParam, commonSearch.dtCountryCityMarkup, commonSearch.B2CMarkup, commonSearch.IsParentId, commonSearch.SubAgentMarkup, commonSearch.SearchByHotel, commonSearch.HotelIDS);
                                    if (willAddOrNot)
                                    {
                                        #region DataTable
                                        DtBDC_HotelRoom = _commonMethod.GenerateRoomtypeDataTableNewV2();
                                        DataTable dtBDC_Cancellation = _commonMethod.GenerateCancellationDataTableNewV2();
                                        DataTable DtBDC_RateBreackUp = _commonMethod.GenerateoomRateBreackUpDataTableNewV2();

                                        DataSet dsTemp = new DataSet();
                                        #endregion

                                        #region RoomOnly RQ

                                        string Error = "";
                                        int CutOffDays = XMLHotelBookingCommon.CutOffDays;
                                        CutOffDays = _commonMethod.GetCutOffDays(commonSearch.dtSuplierMarkup, Provider);
                                        double SubAgentMarkup = 0;
                                        int IsParentId = 1, agentID = commonSearch.AgentId;
                                        Boolean blnIsShowSupplier = false;
                                        double AgentMarkupAmount = 0.0;
                                        Boolean isPerce = true;

                                        string Nationality = xmlCountry;

                                        double AgentCompanyPercentage = Convert.ToDouble(SearchParam.AgentCompanyPercentage);
                                        AgentPercentage = 0;

                                        int NoOfNight = SearchParam.NoofNights;

                                        //_markup.getAgetMarkupSetting(Provider, Convert.ToString(GiataId), commonSearch.dtSuplierMarkup, commonSearch.dtMarkupDetails, ref AgentPercentage, ref AgentMarkupAmount, ref blnIsShowSupplier, ref isPerce, agentID, RatingHotel, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), Convert.ToString(SearchParam.CheckInDate));

                                        _markup.getAgetMarkupSettingNew_new(Provider, Convert.ToString(RaynaID), commonSearch.dtSuplierMarkup, commonSearch.dsMarkupDetails, ref AgentPercentage, ref AgentMarkupAmount, ref blnIsShowSupplier, ref isPerce, commonSearch.AgentId, RatingHotel, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), SearchParam.CheckInDate);

                                        int totalAdult = 0;
                                        int totalChild = 0;
                                        string Meal = "Room Only";
                                        double VatonfirstnightPrice = 0;
                                        for (int c = 0; c < SearchParam.Rooms.Count; c++)
                                        {
                                            totalAdult = Convert.ToInt32(SearchParam.Rooms[c].NoofAdults) + totalAdult;
                                            totalChild = Convert.ToInt32(SearchParam.Rooms[c].NoofChild) + totalChild;
                                        }
                                        #endregion
                                        xmlhotelcode = Convert.ToString(dsResponse.data[m].id);

                                        string ResponseDataa = "";
                                        {
                                            //reshotelsearchnroom.Rootobject dsResponse = JsonConvert.DeserializeObject<reshotelsearchnroom.Rootobject>(ResponseData);

                                            #region Room Details
                                            //string reqId = Convert.ToString(DateTime.Now.Ticks);
                                            //string bookingdotcomUrl = "https://demandapi.booking.com/3.1/accommodations/details";
                                            //string Request = Common.HotelRoomDetailsRequestBookingdotcomv3(Convert.ToInt32(xmlhotelcode));
                                            //ResponseData = BookingDotComv3Response(ref Error, Request, bookingdotcomUrl, UserName, Password, 120000);
                                            //Common.XMLRequestResponseTrack(bookingdotcomUrl + "/" + Request, ResponseData, Provider, "Proper", reqId, SearchParam.SearchKey);


                                            //detailres.Root dsResponsedetail = JsonConvert.DeserializeObject<detailres.Root>(ResponseData);



                                            //string mongoConnectionString = "mongodb://10.32.8.96:27017";
                                            //string mongoDatabaseName = "Booking_com_RoomMapping";
                                            //string mongoCollectionName = "Booking_com_ROOM_Mapping_Xconnect";



                                            //var hotelIds = SearchParam.Filters.HotelIDs.Split(',').Select(int.Parse).ToList();
                                            //var xmlhotelcode1 = xmlhotelcode.Split(',').ToList();

                                            //try
                                            //{
                                            //    // Initialize MongoDB client and get the collection
                                            //    var client = new MongoClient(mongoConnectionString);
                                            //    var database = client.GetDatabase(mongoDatabaseName);
                                            //    var collection = database.GetCollection<BsonDocument>(mongoCollectionName);

                                            //    // Create filter using $in operator
                                            //    var filter = Builders<BsonDocument>.Filter.In("XmlId", xmlhotelcode1);

                                            //    // Fetch the matching records
                                            //    var results = collection.Find(filter).ToList();

                                            //    /// Insert results into a DataTable
                                            //    DtHotelInformation = _commonMethod.ConvertToDataTable(results);

                                            //}
                                            //catch (Exception ex)
                                            //{
                                            //    //DtHotelInformation = new DataTable();
                                            //    await _exception.Error(ex);
                                            //}

                                            //DtHotelInformation = await _commonMethod.MongoDB_BookingC(xmlhotelcode1);
                                            #endregion

                                            var roomNameMapping = new Dictionary<int, string>();
                                            var roomDescriptionMapping = new Dictionary<int, string>();
                                            #region Comment Code
                                            //foreach (var hotel in dsResponsedetail.data)
                                            //{
                                            //    foreach (var room in hotel.rooms)
                                            //    {
                                            //        if (room.name != null && room.name.engb != "")
                                            //        {
                                            //            roomNameMapping[room.id] = room.name.engb;
                                            //            roomDescriptionMapping[room.id] = room.description.engb;
                                            //        }
                                            //    }
                                            //}

                                            //if (dsResponse.data.Length > 0)
                                            //{
                                            //    currencycode = Convert.ToString(dsResponse.data[m].currency);
                                            //    {

                                            //        if (currencycode.ToLower() != Convert.ToString(commonSearch.tokenData.SiteCurrency).ToLower())
                                            //        {
                                            //            //dtInrCurrency = _commonMethod.GetCountryCurrency(currencycode);
                                            //            CurrencyFactor = _commonMethod.GetROEByCurrency(Convert.ToString(currencycode), commonSearch.tokenData.dtROE);
                                            //        }
                                            //    }
                                            //}
                                            #endregion
                                            {
                                                {
                                                    //string RoomName = "";
                                                    string hotel_url = Convert.ToString(dsResponse.data[m].deep_link_url);
                                                    int hotel_id = Convert.ToInt32(dsResponse.data[m].id);
                                                    int t = 0, maxx = 0;
                                                    var result = SearchParam.Rooms.GroupBy(x => new { x.NoofAdults, x.NoofChild, x.Child1Age, x.Child2Age }).Select(n => new { NoofAdults = n.Key.NoofAdults, NoofChild = n.Key.NoofChild, ChildAge = n.ToList(), Count = n.Count() });
                                                    {

                                                        int detailroomid = 0;
                                                        string Roomname = "";
                                                        for (int RO = 0; RO < dsResponse.data[m].products.Length; RO++)
                                                        {

                                                            int Room_Id = Convert.ToInt32(dsResponse.data[m].products[RO].room);

                                                            #region Comment code
                                                            //DataRow[] Drroom = DtHotelInformation.Select("roomid = " + Room_Id);
                                                            //DataRow[] Drroom = DtHotelInformation.Select("roomid = '" + Room_Id + "'");

                                                            //if (Drroom.Length > 0)
                                                            //{
                                                            //    Roomname = Drroom[0]["roomname"].ToString();
                                                            //}
                                                            //else
                                                            //{
                                                            //    continue; // or handle as needed
                                                            //}

                                                            //var room = lstRoomDetails.FirstOrDefault(r => r.roomid == Room_Id);

                                                            //if (room != null)
                                                            //{
                                                            //    Roomname = room.roomname;
                                                            //}
                                                            //else
                                                            //{
                                                            //    continue; // or handle as needed
                                                            //}

                                                            //foreach (var kvp in roomNameMapping)
                                                            //{
                                                            //    detailroomid = kvp.Key;
                                                            //    Roomname = kvp.Value;
                                                            //    if (detailroomid == Room_Id)
                                                            //    {
                                                            //        RoomName = Convert.ToString(Roomname);

                                                            //    }

                                                            //}

                                                            #endregion

                                                            if (roomDict.TryGetValue(Room_Id, out var room))
                                                            {
                                                                Roomname = room.roomname;
                                                            }
                                                            else
                                                            {
                                                                continue; // or handle as needed
                                                            }




                                                            string block_id = Convert.ToString(dsResponse.data[m].products[RO].id);

                                                            int max_occupancy = Convert.ToInt32(dsResponse.data[m].products[RO].maximum_occupancy.total);

                                                            int RoomCount = 1;
                                                            int totalrooms = Convert.ToInt32(dsResponse.data[m].products[RO].number_available);
                                                            if (totalrooms < SearchParam.Rooms.Count)
                                                                continue;

                                                            for (int hi = 0; hi < SearchParam.Rooms.Count; hi++)
                                                            {
                                                                totalAdult = Convert.ToInt32(SearchParam.Rooms[hi].NoofAdults);
                                                                totalChild = Convert.ToInt32(SearchParam.Rooms[hi].NoofChild);

                                                                string e = "";
                                                                b = (Convert.ToInt32(SearchParam.Rooms[hi].NoofAdults) + Convert.ToInt32(SearchParam.Rooms[hi].NoofChild));
                                                                int AdultC = Convert.ToInt32(SearchParam.Rooms[hi].NoofAdults);

                                                                if (b <= max_occupancy && AdultC <= max_occupancy)
                                                                {
                                                                    RoomCount++;

                                                                    if (hi == 0)
                                                                        maxx = t;
                                                                    else if (maxx < t)
                                                                        maxx = t;


                                                                    double Final_Rate = Convert.ToDouble(dsResponse.data[m].products[RO].price.total), ItemPrice = 0;
                                                                    //double Final_Rate = Convert.ToDouble(dsResponse.data[m].products[RO].price.book), ItemPrice = 0;
                                                                    string Essa = "";
                                                                    //if (dsResponse.data[m].products[RO].price.extra_charges != null && dsResponse.data[m].products[RO].price.extra_charges.excluded != null)
                                                                    //{
                                                                    //    for (int ex = 0; ex < dsResponse.data[m].products[RO].price.extra_charges.excluded.Count(); ex++)
                                                                    //    {
                                                                    //        if (Convert.ToInt16(dsResponse.data[m].products[RO].price.extra_charges.excluded[ex].charge) == 22) //22 charge means city tax etc.  
                                                                    //        {
                                                                    //            if (Convert.ToDouble(dsResponse.data[m].products[RO].price.extra_charges.excluded[ex].total_amount) > 0)
                                                                    //            {
                                                                    //                Final_Rate = Final_Rate - Convert.ToDouble(dsResponse.data[m].products[RO].price.extra_charges.excluded[ex].total_amount);
                                                                    //                Essa += Math.Round(Convert.ToDouble(dsResponse.data[m].products[RO].price.extra_charges.excluded[ex].total_amount), 2) + " " + currencycode + " Pay at hotel per person per night" + "</br>";
                                                                    //            }
                                                                    //        }
                                                                    //    }
                                                                    //}

                                                                    //if (dsResponse.data[m].products[RO].price.extra_charges != null && dsResponse.data[m].products[RO].price.extra_charges.included != null)
                                                                    //{
                                                                    //    for (int ex = 0; ex < dsResponse.data[m].products[RO].price.extra_charges.included.Count(); ex++)
                                                                    //    {
                                                                    //        if (Convert.ToInt16(dsResponse.data[m].products[RO].price.extra_charges.included[ex].charge) == 22)
                                                                    //        {
                                                                    //            if (Convert.ToDouble(dsResponse.data[m].products[RO].price.extra_charges.included[ex].total_amount) > 0)
                                                                    //            {
                                                                    //                Final_Rate = Final_Rate - Convert.ToDouble(dsResponse.data[m].products[RO].price.extra_charges.included[ex].total_amount);
                                                                    //                Essa += Math.Round(Convert.ToDouble(dsResponse.data[m].products[RO].price.extra_charges.included[ex].total_amount), 2) + " " + currencycode + " Pay at hotel per person per night" + "</br>";

                                                                    //            }
                                                                    //        }
                                                                    //    }
                                                                    //}

                                                                    double CompanyBuying = 0, AgentSelling = 0;
                                                                    CompanyBuying = Final_Rate * CurrencyFactor;
                                                                    double charge_price = 0; //Convert.ToDouble(dsResponse.data[R].products[RO].price.extra_charges.excluded[0].total_amount);
                                                                    string CurrencY = Convert.ToString(dsResponse.data[m].currency);
                                                                    double ExtraCharge = 0, amount = 0, RoomRateForCompany = 0, RoomRateForAgent = 0, BeforeCommisionRoomRateForAgent = 0;
                                                                    int P = 0;

                                                                    RoomRateForAgent = CompanyBuying;
                                                                    BeforeCommisionRoomRateForAgent = RoomRateForAgent;


                                                                    double Profit = Convert.ToDouble(dsResponse.data[m].products[RO].commission.amount) * CurrencyFactor;
                                                                    RoomRateForAgent = RoomRateForAgent - Profit;



                                                                    string currency = Convert.ToString(dsResponse.data[m].currency);
                                                                    string paytiming = "";
                                                                    if (Convert.ToString(dsResponse.data[m].products[RO].policies) != null)
                                                                    {

                                                                        #region Meal
                                                                        var meals = dsResponse.data[m].products[RO].policies.meal_plan.meals;

                                                                        if (meals != null && meals.Length > 0)
                                                                        {
                                                                            Meal = Convert.ToString(meals[0]);
                                                                        }
                                                                        else
                                                                        {
                                                                            Meal = "Room Only";
                                                                        }
                                                                        bool payments = Convert.ToBoolean(dsResponse.data[m].products[RO].policies.payment.prepayment_required);
                                                                        if (payments)
                                                                        {
                                                                            if (dsResponse.data[m].products[RO].policies.payment.timings.Length > 0)
                                                                            {
                                                                                for (int j = 0; j < dsResponse.data[m].products[RO].policies.payment.timings.Length; j++)
                                                                                {
                                                                                    paytiming += Convert.ToString(dsResponse.data[m].products[RO].policies.payment.timings[j]) + ",";
                                                                                }
                                                                            }
                                                                            paytiming = paytiming.Substring(0, paytiming.Length - 1);
                                                                        }
                                                                        string refundable = Convert.ToString(dsResponse.data[m].products[RO].policies.cancellation.type);
                                                                        Roomname = Roomname;// + "-" + refundable;
                                                                        if (paytiming == "")
                                                                        {
                                                                            paytiming = "pay_online_now";
                                                                        }
                                                                        else
                                                                        {
                                                                            if (refundable == "" || refundable.ToLower().Contains("non_refundable"))
                                                                            {
                                                                                paytiming = "pay_online_now";
                                                                            }
                                                                            else
                                                                            {
                                                                                paytiming = "pay_online_later";
                                                                            }
                                                                        }
                                                                        #endregion
                                                                        string RoomTypeId = Room_Id + "$$$" + Roomname + "$$$" + currency + "$$$" + Meal + "$$$" + currencycode + "$$$" + Final_Rate + "$$$" + block_id + "$$$" + Nationality + "$$$" + Guid.NewGuid().ToString() + "$$$" + paytiming;

                                                                        #region RateBreakUp
                                                                        for (int n = 0; n < SearchParam.NoofNights; n++)
                                                                        {
                                                                            DateTime Day = DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null);
                                                                            Day = Day.AddDays(n);
                                                                            string Days = Convert.ToString(Day);
                                                                            string rateWeekColumn = Convert.ToString(Day.Date.DayOfWeek).Substring(0, 3);
                                                                            double FRate = RoomRateForAgent;
                                                                            RoomRateForCompany = CompanyBuying;
                                                                            //RoomRateForAgent = AgentSelling;

                                                                            #region Vat
                                                                            Double VatOnRateBrackUp = 0;
                                                                            FRate = _markup.ProcessHotelRateBreakupPrice_WithMarkup(1, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), RoomRateForAgent / NoOfNight, commonSearch.B2CMarkup, IsParentId, isPerce, AgentPercentage, AgentMarkupAmount, SubAgentMarkup, ref VatOnRateBrackUp);
                                                                            #endregion

                                                                            VatonfirstnightPrice += VatOnRateBrackUp;
                                                                            DataRow drRateBreackup = DtBDC_RateBreackUp.NewRow();
                                                                            drRateBreackup["SupplierName"] = Provider;
                                                                            drRateBreackup["OwnsystemHotelId"] = OwnsystemHotelId;
                                                                            drRateBreackup["ContractID"] = 0;
                                                                            drRateBreackup["RoomTypeName"] = "[R-" + (hi + 1) + "]" + Roomname;
                                                                            drRateBreackup["RoomTypeId"] = RoomTypeId;
                                                                            drRateBreackup["MealId"] = Meal;
                                                                            drRateBreackup["Meal"] = Meal;
                                                                            drRateBreackup["DateOfWeek"] = rateWeekColumn;
                                                                            drRateBreackup["Date"] = Day;
                                                                            drRateBreackup["Rate"] = FRate;
                                                                            drRateBreackup["RateType"] = "[R-" + (hi + 1) + "]" + Roomname;
                                                                            drRateBreackup["RoomNo"] = hi + 1;//r + 1;
                                                                            drRateBreackup["RoomSequenceNo"] = "";
                                                                            drRateBreackup["BookingStatus"] = "Available";
                                                                            drRateBreackup["SupplierCurrency"] = currencycode;
                                                                            drRateBreackup["BuyingPrice"] = FRate / CurrencyFactor;
                                                                            drRateBreackup["VatdiffAmount"] = VatOnRateBrackUp;
                                                                            drRateBreackup["OrderNo"] = 1;
                                                                            DtBDC_RateBreackUp.Rows.Add(drRateBreackup);
                                                                        }

                                                                        #endregion

                                                                        #region Room
                                                                        // IncrementalPrice = IncrementalPrice * CurrencyFactor;
                                                                        DataRow drRoom = DtBDC_HotelRoom.NewRow();
                                                                        drRoom["OwnSystemHotelId"] = OwnsystemHotelId;
                                                                        drRoom["RoomTypeId"] = RoomTypeId;
                                                                        drRoom["RoomTypeCode"] = Room_Id;
                                                                        drRoom["RoomTypeName"] = Roomname;
                                                                        drRoom["SupplierName"] = Provider;
                                                                        drRoom["BookingStatus"] = "Available";
                                                                        drRoom["BoardType"] = Meal;
                                                                        drRoom["RoomSequenceNo"] = "";
                                                                        drRoom["ContractID"] = 0;
                                                                        //drRoom["EssentialInformation"] = EssentialInfo + "<br/><b>Payment Timing:</b>" + paytiming; //RoomDescription + e;//Convert.ToString(dsResponse.result[h].block[i].room_description);
                                                                        drRoom["EssentialInformation"] = Essa;
                                                                        drRoom["MealId"] = Meal;
                                                                        drRoom["MealName"] = Meal;
                                                                        drRoom["MaxExtraBed"] = "";//ExtraBed;
                                                                        drRoom["RoomNo"] = hi + 1;//r + 1;
                                                                        drRoom["Type"] = "";
                                                                        drRoom["UniqueNo"] = "";
                                                                        drRoom["Characteristic"] = "";
                                                                        drRoom["IsRefundable"] = refundable;
                                                                        drRoom["OfferCode"] = "";
                                                                        drRoom["OfferDescription"] = "";
                                                                        drRoom["AdultsOccupancy"] = totalAdult;
                                                                        drRoom["ChildOccupancy"] = totalChild;
                                                                        drRoom["OurProfit"] = Profit;// 0;// IncrementalPrice;
                                                                        drRoom["BoardTypeID"] = Convert.ToString(Meal);
                                                                        drRoom["CompanyBuyingPriceExpedia"] = 0;//pricewithoutFactor;
                                                                        if (RoomRateForAgent > 0)
                                                                        {
                                                                            ItemPrice = RoomRateForAgent;
                                                                            //Common.ProcessHotelRoom_WithMarkupForBookingDotCom(CurrencyFactor, currencycode, false, false, SearchParam.NoofNights, SearchParam.CityID, SearchParam.CountryId, SearchParam.CheckInDate, SearchParam.CheckOutDate, ref RoomRateForCompany, ref drRoom, isPerce, AgentPercentage, AgentMarkupAmount, SubAgentMarkup, B2CMarkup, IsParentId, ItemPrice);
                                                                            //Common.ProcessHotelRoom_WithMarkup(CurrencyFactor, currencycode, false, false, SearchParam.NoofNights, SearchParam.CityID, SearchParam.CountryId, SearchParam.CheckInDate, SearchParam.CheckOutDate, ref ItemPrice, ref drRoom, isPerce, AgentPercentage, AgentMarkupAmount, SubAgentMarkup, B2CMarkup, IsParentId);
                                                                            _markup.ProcessHotelRoom_WithMarkup_BookingDotComCommision(CurrencyFactor, currencycode, false, false, SearchParam.NoofNights, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), SearchParam.CheckInDate, SearchParam.CheckOutDate, ref ItemPrice, ref drRoom, isPerce, AgentPercentage, AgentMarkupAmount, SubAgentMarkup, commonSearch.B2CMarkup, IsParentId, BeforeCommisionRoomRateForAgent);

                                                                        }
                                                                        DtBDC_HotelRoom.Rows.Add(drRoom);
                                                                        #endregion

                                                                        #region Cancellation Policy

                                                                        DateTime canceldate = DateTime.Now;
                                                                        double AgentCancelCharge = 0;
                                                                        double pri = 0;
                                                                        double VatOnPolicy = 0;
                                                                        string checkin = Convert.ToString(SearchParam.CheckInDate);
                                                                        checkin = checkin.Substring(6, 4) + "-" + checkin.Substring(3, 2) + "-" + checkin.Substring(0, 2);

                                                                        #region Offer, Refunable - Non Refundable



                                                                        //taxes + "-" + RoomDescription + "-" + e + "<br/>" + Essential;
                                                                        RoomTypeId = RoomTypeId.Replace("'", "");

                                                                        #endregion



                                                                        string Refundable = Convert.ToString(dsResponse.data[m].products[RO].policies.cancellation.type);

                                                                        if (Refundable == "" || Refundable.ToLower().Contains("non_refundable"))
                                                                        {
                                                                            AgentCancelCharge = ItemPrice;


                                                                            dtBDC_Cancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref RoomRateForCompany, dtBDC_Cancellation.NewRow(), CurrencyFactor, currencycode, Provider, Convert.ToString(OwnsystemHotelId), Convert.ToString(hi + 1), RoomTypeId, "[R-" + (hi + 1) + "]" + Roomname, Meal, DateTime.Now.Date, Convert.ToDateTime(checkin), AgentCancelCharge, AgentCancelCharge, Meal, VatonfirstnightPrice, "", "Yes", "", "", "", ""));
                                                                            dtBDC_Cancellation.AcceptChanges();

                                                                        }
                                                                        else
                                                                        {
                                                                            DateTime FromDate = DateTime.Now;
                                                                            if (dsResponse.data[m].products[RO].policies.cancellation.free_cancellation_until != null)
                                                                            {
                                                                                FromDate = Convert.ToDateTime(dsResponse.data[m].products[RO].policies.cancellation.free_cancellation_until);
                                                                            }

                                                                            string CancelDeadlinewithoutCutoff = Convert.ToString(FromDate);
                                                                            canceldate = FromDate;
                                                                            pri = 0;
                                                                            VatOnPolicy = 0;
                                                                            double fee = RoomRateForAgent;
                                                                            double companyFee = RoomRateForCompany;
                                                                            pri = fee;
                                                                            fee = _markup.ProcessHotelCancelationPrice_WithMarkup(ref pri, SearchParam.NoofNights, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), fee, isPerce, commonSearch.B2CMarkup, AgentPercentage, AgentMarkupAmount, IsParentId, SubAgentMarkup, ref VatOnPolicy);



                                                                            if (canceldate.AddDays(CutOffDays) > DateTime.Now.Date)
                                                                                canceldate = canceldate.AddDays(CutOffDays);
                                                                            else
                                                                                canceldate = DateTime.Now.Date;
                                                                            if (canceldate > DateTime.Now.Date)
                                                                            {
                                                                                double zero = 0;
                                                                                dtBDC_Cancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref zero, dtBDC_Cancellation.NewRow(), CurrencyFactor, currencycode, Provider, Convert.ToString(OwnsystemHotelId), Convert.ToString(hi + 1), RoomTypeId, "[R-" + (hi + 1) + "]" + Roomname, Meal, DateTime.Now.Date, canceldate.AddDays(-1), AgentCancelCharge, AgentCancelCharge, Meal, VatonfirstnightPrice, CancelDeadlinewithoutCutoff, "No", "", "", "", ""));
                                                                                dtBDC_Cancellation.AcceptChanges();

                                                                                AgentCancelCharge = ItemPrice;


                                                                                dtBDC_Cancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref RoomRateForCompany, dtBDC_Cancellation.NewRow(), CurrencyFactor, currencycode, Provider, Convert.ToString(OwnsystemHotelId), Convert.ToString(hi + 1), RoomTypeId, "[R-" + (hi + 1) + "]" + Roomname, Meal, canceldate, Convert.ToDateTime(checkin), AgentCancelCharge, AgentCancelCharge, Meal, VatonfirstnightPrice, CancelDeadlinewithoutCutoff, "No", "", "", "", ""));
                                                                                dtBDC_Cancellation.AcceptChanges();
                                                                            }
                                                                            else
                                                                            {
                                                                                AgentCancelCharge = ItemPrice;


                                                                                dtBDC_Cancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref RoomRateForCompany, dtBDC_Cancellation.NewRow(), CurrencyFactor, currencycode, Provider, Convert.ToString(OwnsystemHotelId), Convert.ToString(hi + 1), RoomTypeId, "[R-" + (hi + 1) + "]" + Roomname, Meal, DateTime.Now.Date, Convert.ToDateTime(checkin), AgentCancelCharge, AgentCancelCharge, Meal, VatonfirstnightPrice, CancelDeadlinewithoutCutoff, "Yes", "", "", "", ""));
                                                                                dtBDC_Cancellation.AcceptChanges();
                                                                            }
                                                                            #endregion
                                                                        }
                                                                    }
                                                                    else
                                                                    {
                                                                        string Name = "";//dsResponse.data[R].products[RO].name;
                                                                    }



                                                                }
                                                                else
                                                                {
                                                                    // return null;
                                                                    continue;
                                                                }
                                                            }
                                                        }


                                                    }
                                                }
                                            }


                                        }
                                        decimal LowestPrice = Convert.ToDecimal(MinPrice).CalculatePrice(commonSearch.tokenData, commonSearch.advancedOptions, currencycode);
                                        MinPrice = Convert.ToDouble(LowestPrice);

                                        if (DtBDC_HotelRoom.Rows.Count == 0)
                                            continue;

                                        Options ops = (await _supplierRes.GetCommonRoomForSuppliers(GiataId, commonSearch.tokenData, commonSearch.advancedOptions, MinPrice, drXmlHotel, DtBDC_HotelRoom, dtBDC_Cancellation, DtBDC_RateBreackUp, SearchParam));
                                        if (ops != null)
                                            options.Add(ops);
                                    }
                                    #endregion
                                }
                            }

                        }

                    }
                }
                #endregion

            }
            catch (Exception ex)
            {
                item.ResponseStatus = 404;
                await _exception.Error(ex);
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

                        SearchParam.SupplierTime += ",|Finish:Travellanda Process Response  " + "|Time :" + timerroomx.Elapsed.ToString();
                        _ = Task.Run(() => _commonMethod.FireAway_SQL_ResPorcessTimeLog("", timerroomx.Elapsed.ToString(), item.Supplier, SearchParam.SearchKey, 1, SearchParam.AgentID));
                    }
                }
                catch (Exception x)
                {
                    await _exception.Error(x);
                }
                #endregion
                //GC.Collect();
            }
            return options;
        }
        #endregion
        public async Task<string> Get_bookingDotcom_EInfo(string hotelcode, string ownsystemid, string roomtypeid, HotelSearchParameter SearchPara, string Password, string Action, string Username, string SearchKey)
        {
            string Essen = "";
            try
            {
                string essentinfo = "";
                #region Essential Info
                StringBuilder sbHotelsKeyCollection = new StringBuilder();
                sbHotelsKeyCollection.Append(Action);
                sbHotelsKeyCollection.Append("hotels?hotel_ids=" + hotelcode);
                sbHotelsKeyCollection.Append("&language=en&extras=hotel_description,hotel_policies&show_test=0");
                //return sbHotelsKeyCollection.ToString() ;
                string Error = "";
                //string Response1 = BookingDotComResponse(ref Error, sbHotelsKeyCollection.ToString(), Action + "hotels", Username, Password, 2000);
                ResponseDto responseDto = await BookingDotComResponse(sbHotelsKeyCollection.ToString(), Action + "hotels", Username, Password, 2000);
                string Response1 = responseDto.Response;
                Error = responseDto.Error;

                BookingDotComRoomEss.Root ResponseEss = Newtonsoft.Json.JsonConvert.DeserializeObject<BookingDotComRoomEss.Root>(Response1);
                if (Response1 != "")
                {
                    for (int h = 0; h < ResponseEss.result.Count; h++)
                    {
                        int HotelId = Convert.ToInt32(ResponseEss.result[h].hotel_id);
                        string RoomData = Convert.ToString(ResponseEss.result[h].room_data);

                        string LicenseNumber = ResponseEss.result[h].hotel_data.license_number;
                        string HotelDescription = ResponseEss.result[h].hotel_data.hotel_description;
                        string HotelImportantInformation = ResponseEss.result[h].hotel_data.hotel_important_information;
                        int ChildMinAge = ResponseEss.result[h].hotel_data.additional_policies.child_min_age;
                        bool children_allowed = ResponseEss.result[h].hotel_data.additional_policies.children_allowed;
                        if (ResponseEss.result[h].hotel_data.additional_policies.cots_and_extra_beds != null)
                        {
                            for (int A = 0; A < ResponseEss.result[h].hotel_data.additional_policies.cots_and_extra_beds.Count; A++)
                            {
                                double price = ResponseEss.result[h].hotel_data.additional_policies.cots_and_extra_beds[A].price;
                                string price_mode = ResponseEss.result[h].hotel_data.additional_policies.cots_and_extra_beds[A].price_mode;
                                string rate_type = ResponseEss.result[h].hotel_data.additional_policies.cots_and_extra_beds[A].rate_type;
                                int to_age = ResponseEss.result[h].hotel_data.additional_policies.cots_and_extra_beds[A].to_age;
                                int from_age = ResponseEss.result[h].hotel_data.additional_policies.cots_and_extra_beds[A].from_age;
                            }
                        }
                        //for (int ES = 0; ES < ResponseEss.result[h].hotel_data.hotel_policies.Count; ES++)
                        //{
                        //    string content = ResponseEss.result[h].hotel_data.hotel_policies[ES].content;
                        //    string type = ResponseEss.result[h].hotel_data.hotel_policies[ES].type;
                        //    string name = ResponseEss.result[h].hotel_data.hotel_policies[ES].name;
                        //}
                        string content = "";
                        string type = "";
                        string name = "";
                        string InFo = "";
                        string InFo1 = "";
                        string Ess = "";

                        for (int ES = 0; ES < ResponseEss.result[h].hotel_data.hotel_policies.Count; ES++)
                        {
                            if (ES == 0)
                            {
                                content = ResponseEss.result[h].hotel_data.hotel_policies[ES].content;
                                type = ResponseEss.result[h].hotel_data.hotel_policies[ES].type;
                                name = ResponseEss.result[h].hotel_data.hotel_policies[ES].name;

                                InFo = content + "<br/>" + type + "<br/>" + name + "<br/>";
                            }
                            else
                            {
                                content = ResponseEss.result[h].hotel_data.hotel_policies[ES].content;
                                type = ResponseEss.result[h].hotel_data.hotel_policies[ES].type;
                                name = ResponseEss.result[h].hotel_data.hotel_policies[ES].name;

                                InFo1 = content + "<br/>" + type + "<br/>" + name + "<br/>";
                                InFo = InFo + "<br/>" + InFo1;
                            }

                            Ess = InFo;//content + "," + type + "," + name +"<br/>"+ content1 + "," + type1 + "," + name1 + "<br/>";


                        }
                        Essen += "<b> HotelImportantInformation: </b> " + HotelImportantInformation + " , <br/> <b> Hotel Policies: </b>  " + Ess;


                    }

                }
                #endregion

            }
            catch (Exception ex)
            {
                await _exception.ErrorNew(ex, _LogAPI.Recheck, SearchKey);
            }

            return Essen;
        }

        public async Task<string> Get_bookingDotcom_V3_EInfo(string hotelcode, string URL, string XmlCity, string ownsystemid, string roomtypeid, HotelSearchParameter SearchPara, string Password, string Action, string Username, string SearchKey, string Provider, string XmlNationality, string CurrencyCode, DataTable dtROE)
        {
            string Essen = "";
            string Essenn = "";
            string EssentialInfo = "";
            string Error = "";
            string Essential = "";
            string EssentialRoomInfo = "";
            string[] RoomTypeId = roomtypeid.Split(new String[] { "$$$" }, StringSplitOptions.None);
            string RoomId = RoomTypeId[0];
            string bookingdotcomUrl = "https://demandapi.booking.com/3.1/accommodations/details";
            try
            {


                for (int hi = 0; hi < SearchPara.Rooms.Count; hi++)
                {
                    #region Room Details
                    string reqId = Convert.ToString(DateTime.Now.Ticks);
                    string InfoRequest = HotelRoomDetailsRequestBookingdotcomv3(Convert.ToInt32(hotelcode));
                    //string ResponseDataa = BookingDotComv3Response(ref Error, InfoRequest, bookingdotcomUrl, Username, Password, 120000);
                    ResponseDto responseDto = await BookingDotComv3Response(InfoRequest, bookingdotcomUrl, Username, Password, 120000);
                    string ResponseDataa = responseDto.Response;
                    Error = responseDto.Error;

                    //_ = _commonMethod.XMLRequestResponseTrack(bookingdotcomUrl + "/" + InfoRequest, ResponseDataa, Provider, "Proper", reqId, SearchKey); //Comment on 29-10-25
                    #endregion
                    detailres.Root dsResponsedetail = JsonConvert.DeserializeObject<detailres.Root>(ResponseDataa);
                    {
                        Essential += "<b>Checkin From Time:</b>" + Convert.ToString(dsResponsedetail.data[0].checkin_checkout_times.checkin_from);
                        Essential += "<br/><b>Checkin To Time:</b>" + Convert.ToString(dsResponsedetail.data[0].checkin_checkout_times.checkin_to);
                        Essential += "<br/><b>CheckOut From Time:</b>" + Convert.ToString(dsResponsedetail.data[0].checkin_checkout_times.checkout_from);
                        Essential += "<br/><b>CheckOut To Time:</b>" + Convert.ToString(dsResponsedetail.data[0].checkin_checkout_times.checkout_to);
                        Essential += "<br/><b>Important Information:</b>" + Convert.ToString(dsResponsedetail.data[0].description.important_information.engb);
                        Essential += "<br/><b>Important Text:</b>" + Convert.ToString(dsResponsedetail.data[0].description.text.engb);
                        Essential += "<br/><br/><b>facility_details:</b> <br/> <b>price</b>:" + Convert.ToString(dsResponsedetail.data[0].facility_details.internet_facility.price) + ", <b>charge_mode:</b>" + Convert.ToString(dsResponsedetail.data[0].facility_details.internet_facility.charge_mode) + ", <b>coverage:</b>" + Convert.ToString(dsResponsedetail.data[0].facility_details.internet_facility.coverage) + ",<b>connection_type:</b>" + Convert.ToString(dsResponsedetail.data[0].facility_details.internet_facility.connection_type);

                        if (dsResponsedetail.data[0].facility_details.parking_facilities != null)
                        {
                            if (dsResponsedetail.data[0].facility_details.parking_facilities.Count > 0)
                            {

                                for (int s = 0; s < dsResponsedetail.data[0].facility_details.parking_facilities.Count; s++)
                                {


                                    Essential += "<br/><br/><b>Parking facility_details:</b> <br><b>price: </b>" + Convert.ToString(dsResponsedetail.data[0].facility_details.parking_facilities[s].price);
                                    Essential += ",<b>charge_mode: </b>" + Convert.ToString(dsResponsedetail.data[0].facility_details.parking_facilities[s].charge_mode);
                                    Essential += ",<b>type: </b>" + Convert.ToString(dsResponsedetail.data[0].facility_details.parking_facilities[s].type);
                                    Essential += ",<b>location: </b>" + Convert.ToString(dsResponsedetail.data[0].facility_details.parking_facilities[s].location);
                                    Essential += ",<b>reservation: </b>" + Convert.ToString(dsResponsedetail.data[0].facility_details.parking_facilities[s].reservation);

                                }


                            }
                        }
                        if (dsResponsedetail.data[0].facility_details.restaurant_facilities != null)
                        {
                            if (dsResponsedetail.data[0].facility_details.restaurant_facilities.Count > 0)
                            {

                                for (int s = 0; s < dsResponsedetail.data[0].facility_details.restaurant_facilities.Count; s++)
                                {


                                    Essential += "<br/><br/><b>Restaurant facility_details:</b> <br/><b>name: </b>" + Convert.ToString(dsResponsedetail.data[0].facility_details.restaurant_facilities[s].name);
                                    Essential += ",<b>status: </b>" + Convert.ToString(dsResponsedetail.data[0].facility_details.restaurant_facilities[s].status);
                                    Essential += ",<b>guests_only: </b>" + Convert.ToString(dsResponsedetail.data[0].facility_details.restaurant_facilities[s].guests_only);
                                    Essential += ",<b>accept_reservations: </b>" + Convert.ToString(dsResponsedetail.data[0].facility_details.restaurant_facilities[s].accept_reservations);


                                }


                            }
                        }
                        if (dsResponsedetail.data[0].facility_details.swimming_pool_facilities != null)
                        {
                            if (dsResponsedetail.data[0].facility_details.swimming_pool_facilities.Count > 0)
                            {

                                for (int s = 0; s < dsResponsedetail.data[0].facility_details.swimming_pool_facilities.Count; s++)
                                {


                                    Essential += "<br/><br/><b>Swimming Pool facility_details:</b> <br/><b>type: </b>" + Convert.ToString(dsResponsedetail.data[0].facility_details.swimming_pool_facilities[s].type);
                                    Essential += ",<b>payment_type: </b>" + Convert.ToString(dsResponsedetail.data[0].facility_details.swimming_pool_facilities[s].payment_type);
                                    Essential += ",<b>allowed_age_type: </b>" + Convert.ToString(dsResponsedetail.data[0].facility_details.swimming_pool_facilities[s].allowed_age_type);



                                }


                            }
                        }
                        if (dsResponsedetail.data[0].policies != null)
                        {
                            if (dsResponsedetail.data[0].policies.cots_and_extra_beds.Count > 0)
                            {

                                for (int s = 0; s < dsResponsedetail.data[0].policies.cots_and_extra_beds.Count; s++)
                                {


                                    Essential += "<br/><br/><b>Extrabed facility_details:</b> <br/><b>age from: </b>" + Convert.ToString(dsResponsedetail.data[0].policies.cots_and_extra_beds[s].age.from);
                                    Essential += ",<b>age to: </b>" + Convert.ToString(dsResponsedetail.data[0].policies.cots_and_extra_beds[s].age.to);
                                    Essential += ",<b>mode: </b>" + Convert.ToString(dsResponsedetail.data[0].policies.cots_and_extra_beds[s].mode);
                                    Essential += ",<b>percentage: </b>" + Convert.ToString(dsResponsedetail.data[0].policies.cots_and_extra_beds[s].percentage);
                                    Essential += ",<b>price: </b>" + Convert.ToString(dsResponsedetail.data[0].policies.cots_and_extra_beds[s].price);
                                    Essential += ",<b>type: </b>" + Convert.ToString(dsResponsedetail.data[0].policies.cots_and_extra_beds[s].type);



                                }


                            }
                            Essential += "<br><br><b>damage: </b>" + Convert.ToString(dsResponsedetail.data[0].policies.damage);
                            Essential += ", <b>maximum_checkin_age: </b>" + Convert.ToString(dsResponsedetail.data[0].policies.maximum_checkin_age);
                            Essential += ", <b>minimum_checkin_age: </b>" + Convert.ToString(dsResponsedetail.data[0].policies.minimum_checkin_age);
                            Essential += ", <b>minimum_guest_age: </b>" + Convert.ToString(dsResponsedetail.data[0].policies.minimum_guest_age);
                            Essential += ", <b>pets: </b> allowed :" + Convert.ToString(dsResponsedetail.data[0].policies.pets.allowed);
                            Essential += ", <b>pets: </b> charge_mode :" + Convert.ToString(dsResponsedetail.data[0].policies.pets.charge_mode);

                        }
                        int detailroomid = 0;

                        //int Room_Id = Convert.ToInt32(dsResponse.data[hi].products[RO].room);
                        int Room_Id = Convert.ToInt32(RoomId);
                        var roomNameMapping = new Dictionary<int, string>();
                        var roomDescriptionMapping = new Dictionary<int, string>();
                        foreach (var hotel in dsResponsedetail.data)
                        {
                            foreach (var room in hotel.rooms)
                            {
                                if (room.name != null && room.name.engb != "")
                                {
                                    roomNameMapping[room.id] = room.name.engb;
                                    roomDescriptionMapping[room.id] = room.description.engb;

                                }

                            }

                        }
                        foreach (var kvpd in roomDescriptionMapping)
                        {
                            detailroomid = kvpd.Key;
                            EssentialRoomInfo = kvpd.Value;
                            if (detailroomid == Room_Id)
                            {
                                Essenn = Convert.ToString(EssentialRoomInfo);

                            }

                        }

                        EssentialInfo = Essential + "<br/>" + Essenn;




                        Essen = EssentialInfo;
                    }
                }


            }
            catch (Exception ex)
            {
                await _exception.ErrorNew(ex, _LogAPI.Recheck, SearchKey);
            }

            return Essen;
        }
        public async Task<ResponseDto> BookingDotComResponse(string Request, string Url, string UserName, string Password, int TimeOut)
        {
            ResponseDto responseDto = new ResponseDto();
            string Response = "";
            string Error = "";
            try
            {
                string Key = Convert.ToBase64String(Encoding.ASCII.GetBytes(UserName + ":" + Password));
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(Request);
                request.Method = "GET";
                request.ContentType = "application/json";
                request.Accept = "application/json";
                request.Headers.Add("Authorization", "Basic " + Key);
                request.Headers.Add("Accept-Encoding", "gzip");
                request.Timeout = TimeOut;
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
                _exception.Error(ex);
            }

            responseDto.Response = Response;
            responseDto.Error = Error;
            return responseDto;
        }

        public async Task<PriceCheckBDCDto> Get_BookingDotCom_PriceCheck(DataSet dsCartDetails, DataRow[] BDCDetails, string UserName, string Password, int i, int j, DataTable dtCheckPriceReqStructure, string UniqueNo, string AgentUniqueNo, double FinalPrice, bool isForBooking, double CompanyBuyingPrice, string IsFor, double commission, TokenData tokenData, int AgentId, string SearchKey = "", string TechName = "")
        {
            PriceCheckBDCDto priceCheckBDCDto = new PriceCheckBDCDto();
            string responsetype = "Success";
            string time = "";
            string xslRequest = "";
            string ResponseData = "";
            string Error = "";
            string SupplierName = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["SupplierName"]).ToLower();
            int srphotelid = 0;
            try
            {
                srphotelid = Convert.ToInt32(dsCartDetails.Tables[0].Rows[0]["ServiceId"]);
            }
            catch (Exception ex)
            {
                _ = _exception.Error(ex, SearchKey);
            }
            try
            {
                #region Price Check RQ and Declaration
                #region Data
                double CurrencyFactor = 1;
                DataSet dsTemp = new DataSet();
                //SearchParameter SearchPara = new SearchParameter();
                string[] array = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomTypeId"]).Split(new String[] { "$$$" }, StringSplitOptions.None);
                string roomId = array[6];
                string Currency = array[4];
                if (Currency.ToLower() != tokenData.SiteCurrency.ToLower())
                {
                    //DataTable dtInrCurrency = Common.GetCountryCurrency(Currency);
                    // CurrencyFactor = Convert.ToDouble(dtInrCurrency.Rows[0]["AmtToExch"]);

                    CurrencyFactor = _commonMethod.GetROEByCurrency(Currency, tokenData.dtROE);
                }
                string Url = Convert.ToString(BDCDetails[0]["WebSiteURL"]);
                string UserId = Convert.ToString(BDCDetails[0]["UserId"]);
                string HotelId = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["XMLHotelId"]);
                int UserCartId = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[j]["UserCartID"]);
                int UserCartDetailId = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[j]["UserCartDetailId"]);
                string RoomTypeId = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomTypeID"]);
                //string SupplierName = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["SupplierName"]).ToLower();
                string CheckIn = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["CheckInDate"]);
                CheckIn = CheckIn.Substring(0, 4) + "-" + CheckIn.Substring(4, 2) + "-" + CheckIn.Substring(6, 2);
                string CheckOut = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["CheckOutDate"]);
                CheckOut = CheckOut.Substring(0, 4) + "-" + CheckOut.Substring(4, 2) + "-" + CheckOut.Substring(6, 2);
                #endregion
                xslRequest = Get_BookingDotCom_PriceCheckRQ(Url, HotelId, CheckIn, CheckOut, UserId, UserCartId, dsCartDetails);
                _commonMethod.ThirdPartyBookingHistory(Convert.ToString(UniqueNo), Convert.ToString(AgentUniqueNo), xslRequest, "-", "Before Request hit Price Check", SupplierName, "response confirm", "-", "WithinEarth");
                #endregion

                if (xslRequest != "")
                {
                    if (!isForBooking)
                    {
                        dtCheckPriceReqStructure.Rows.Add(_commonMethod.AddCheckPriceReq(dtCheckPriceReqStructure.NewRow(), i + 1, UserCartId, UserCartDetailId, RoomTypeId, SupplierName, xslRequest));
                    }
                    #region Price Check RS  

                    System.Diagnostics.Stopwatch timerroomprocess = new System.Diagnostics.Stopwatch();
                    timerroomprocess.Start();

                    //ResponseData = BookingDotComResponsePricecheck(ref Error, xslRequest, Url + "blockAvailability?", UserName, Password, 120000);
                    ResponseDto responseDto = await BookingDotComResponse(xslRequest, Url + "blockAvailability?", UserName, Password, 120000);
                    ResponseData = responseDto.Response;
                    Error = responseDto.Error;

                    timerroomprocess.Stop();
                    time = timerroomprocess.Elapsed.ToString();

                    _commonMethod.ThirdPartyBookingHistory(Convert.ToString(UniqueNo), Convert.ToString(AgentUniqueNo), xslRequest, ResponseData, "After Request hit Price Check", SupplierName, "response confirm", "-", "WithinEarth");
                    if (ResponseData == string.Empty || ResponseData == null)
                    {

                        try
                        {
                            responsetype = "GeneralError";
                            string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, xslRequest, ResponseData, Error, SearchKey, TechName, srphotelid);
                        }
                        catch (Exception e)
                        {
                            _ = _exception.Error(e);
                        }

                        priceCheckBDCDto.Flag = false;
                        return priceCheckBDCDto;
                    }
                    if (ResponseData.Contains("Error"))
                    {
                        try
                        {
                            responsetype = "GeneralError";
                            string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, xslRequest, ResponseData, Error, SearchKey, TechName, srphotelid);
                        }
                        catch (Exception e)
                        {
                            _ = _exception.Error(e);
                        }
                        priceCheckBDCDto.Flag = false;
                        return priceCheckBDCDto;
                    }
                    //BookingDotComRoom.RootObject Response = JsonConvert.DeserializeObject<BookingDotComRoom.RootObject>(ResponseData);
                    BookingDotComPriceCheck.Root Response = JsonConvert.DeserializeObject<BookingDotComPriceCheck.Root>(ResponseData);
                    #endregion

                    #region Price Check Logic
                    if (Response != null)
                    {
                        double Final_Rate = 0;
                        for (int h = 0; h < Response.result.Count; h++)
                        {
                            for (int l = 0; l < Response.result[h].block.Count; l++)
                            {
                                string blockId = Convert.ToString(Response.result[h].block[l].block_id);
                                if (blockId == roomId)
                                {
                                    // Final_Rate = Convert.ToDouble(Response.result[h].block[l].min_price.price);
                                    // FinalPrice = Final_Rate * CurrencyFactor;
                                    //commission = Convert.ToDouble(Response.result[h].block[l].commission_details.commission_amount.amount) * CurrencyFactor;
                                    Currency = Convert.ToString(Response.result[h].block[l].incremental_price[0].currency);
                                    if (Currency.ToLower() != Convert.ToString(tokenData.SiteCurrency).ToLower())
                                    {
                                        //DataTable dtInrCurrency = Common.GetCountryCurrency(Currency);
                                        //CurrencyFactor = Convert.ToDouble(dtInrCurrency.Rows[0]["AmtToExch"]);
                                        CurrencyFactor = _commonMethod.GetROEByCurrency(Currency, tokenData.dtROE);
                                    }
                                    //double incremental_price = Convert.ToDouble(Response.result[h].block[l].incremental_price[0].price); // Need to FIx [0]
                                    double incremental_price = Convert.ToDouble(Response.result[h].block[l].min_price.charge_price); // Need to FIx [0]

                                    FinalPrice = incremental_price * CurrencyFactor;


                                }
                            }
                        }
                        if (IsFor == "P")
                        {
                            CompanyBuyingPrice = Convert.ToDouble(dsCartDetails.Tables[1].DefaultView[j]["CompanyBuyingPrice"]);
                        }
                        else
                        {
                            CompanyBuyingPrice = Convert.ToDouble(dsCartDetails.Tables[1].DefaultView[j]["Rate"]);
                        }
                        FinalPrice = Convert.ToDouble(FinalPrice);

                        priceCheckBDCDto.FinalPrice = FinalPrice;//02-07-25

                        if (Convert.ToDouble(FinalPrice.TruncateDecimalPlaces(0)) > CompanyBuyingPrice.TruncateDecimalPlaces(0))
                        {
                            try
                            {
                                responsetype = "GeneralError";
                                string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, xslRequest, ResponseData, Error, SearchKey, TechName, srphotelid);
                            }
                            catch (Exception e)
                            {
                                _ = _exception.Error(e);
                            }

                            priceCheckBDCDto.Flag = true;
                            return priceCheckBDCDto;
                        }
                    }
                    #endregion
                }
                else
                {
                    try
                    {
                        responsetype = "GeneralError";
                        string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, xslRequest, ResponseData, Error, SearchKey, TechName, srphotelid);
                    }
                    catch (Exception e)
                    {
                        _ = _exception.Error(e);
                    }
                    priceCheckBDCDto.Flag = false;
                    return priceCheckBDCDto;
                }
            }
            catch (Exception e)
            {
                _ = _exception.Error(e);
            }

            try
            {
                responsetype = "Success";
                string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, xslRequest, ResponseData, Error, SearchKey, TechName, srphotelid);
            }
            catch (Exception e)
            {
                _ = _exception.Error(e);
            }
            priceCheckBDCDto.Flag = false;
            return priceCheckBDCDto;
        }

        public string Get_BookingDotCom_PriceCheckRQ(string Url, string HotelId, string CheckIn, string checkout, string UserId, int UserCartId, DataSet dsCartDetails)
        {
            try
            {
                #region Price Check RQ
                string RoomTypeId = Convert.ToString(dsCartDetails.Tables[1].DefaultView[0]["RoomTypeId"]);
                string[] spitid1 = RoomTypeId.Split(new String[] { "$$$" }, StringSplitOptions.None);
                string Nationality = spitid1[7];

                StringBuilder req = new StringBuilder();
                req.Append(Url + "blockAvailability?");
                req.Append("hotel_ids=" + HotelId);
                req.Append("&checkin=" + CheckIn);//yyyy-mm-dd
                req.Append("&checkout=" + checkout);
                req.Append("&extras=max_children_free,max_children_free_age");
                //req.Append("&guest_cc=nl");
                req.Append("&guest_cc=" + Nationality);
                req.Append("&show_test=" + Convert.ToString(BookingInfo.Bookingshow_test));
                // req.Append("&can_pay_now: true");
                DataRow[] drUserCartCount = dsCartDetails.Tables[1].Select("UserCartId='" + UserCartId + "'");
                //151
                // int CartCount = Convert.ToInt32(drUserCartCount.Length);
                for (int i = 0; i < drUserCartCount.Length; i++)
                {
                    string Adults = "";
                    int adults = Convert.ToInt32(drUserCartCount[i]["NoOfAdults"]);
                    string Child1Age = Convert.ToString(drUserCartCount[i]["ChildAge1"]);
                    string Child2Age = Convert.ToString(drUserCartCount[i]["ChildAge2"]);
                    int NoofChild = Convert.ToInt32(drUserCartCount[i]["NoOfChild"]);

                    for (int a = 0; a < adults; a++)
                    {
                        if (a == 0)
                            Adults = "A";
                        else
                            Adults = Adults + ",A";
                    }
                    switch (Convert.ToInt32(NoofChild))
                    {
                        case 1:
                            //Child1Age = "," + SearchPara.Rooms[i-1].Child1Age;
                            Child1Age = "," + drUserCartCount[i]["ChildAge1"];
                            break;
                        case 2:
                            Child1Age = "," + drUserCartCount[i]["ChildAge1"] + "," + drUserCartCount[i]["ChildAge2"];
                            break;
                        default:
                            break;
                    }
                    if (NoofChild > 0)
                        req.Append("&room" + (i + 1) + "=" + Adults + Child1Age);
                    else
                        req.Append("&room" + (i + 1) + "=" + Adults);
                }
                //req.Append("&affiliate_id=" + UserId + "");
                req.Append("&user_platform=mobile");//Added on 24-Jul-2024 by mustafa sir
                                                    //req.Append("&user_platform=mobile-app");//Added on 21-JUN-2024 by mustafa sir
                return req.ToString();
                #endregion
            }
            catch (Exception ex)
            {
                _ = _exception.Error(ex);
            }
            return "";
        }

        public async Task<ResponseDto> BookingDotComv3Response(string Request, string Url, string UserName, string Password, int TimeOut)
        {
            ResponseDto responseDto = new ResponseDto();
            string Error = "";

            string Response = "";
            //ServicePointManager.Expect100Continue = true;
            //ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls
            //       | SecurityProtocolType.Tls11
            //       | SecurityProtocolType.Tls12
            //       | SecurityProtocolType.Ssl3;



            HttpWebRequest httpWebRequest = null;
            HttpWebResponse httpWebResponse = null;
            try
            {
                byte[] bytes;
                bytes = System.Text.Encoding.UTF8.GetBytes(Request);
                int affiliateId = Convert.ToInt32(UserName);

                httpWebRequest = (HttpWebRequest)WebRequest.Create(Url);
                httpWebRequest.Method = "POST";
                httpWebRequest.ContentType = "application/json";
                httpWebRequest.Headers.Add("x-affiliate-id", affiliateId.ToString());
                httpWebRequest.Headers.Add("Authorization", "Bearer " + Password);
                httpWebRequest.Timeout = TimeOut;
                using (Stream requestStream = httpWebRequest.GetRequestStream())
                {

                    requestStream.Write(bytes, 0, bytes.Length);
                    requestStream.Close();//Close stream
                }
                httpWebResponse = (HttpWebResponse)await httpWebRequest.GetResponseAsync();
                using (Stream responseStream = httpWebResponse.GetResponseStream())
                {
                    using (StreamReader reader = new StreamReader(responseStream))
                        Response = await reader.ReadToEndAsync();

                }
            }
            catch (WebException webEx)
            {
                //Error = ex.Message.ToString() + "#=" + ex.StackTrace.ToString();
                if (webEx.Response != null)
                {
                    using (var errorResponse = (HttpWebResponse)webEx.Response)
                    {
                        Stream st = errorResponse.GetResponseStream();

                        if (errorResponse.ContentEncoding.ToLower().Contains("gzip"))
                            st = new System.IO.Compression.GZipStream(st, System.IO.Compression.CompressionMode.Decompress);
                        else if (errorResponse.ContentEncoding.ToLower().Contains("deflate"))
                            st = new System.IO.Compression.DeflateStream(st, System.IO.Compression.CompressionMode.Decompress);

                        using (StreamReader reader = new StreamReader(st))
                        {
                            string errorText = await reader.ReadToEndAsync();
                            Error += ": " + errorText;
                        }
                    }
                }
                _ = _exception.Error(webEx);
            }

            responseDto.Response = Response;
            responseDto.Error = Error;

            return responseDto;
        }

        public string HotelRoomDetailsRequestBookingdotcomv3(int Hotelid)
        {
            try
            {
                JsonSerializerSettings Setting = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    DefaultValueHandling = DefaultValueHandling.Ignore
                };
                var rootObject = new detailrq.Root
                {
                    accommodations = new List<int>(),
                    extras = new List<string>(),
                    languages = new List<string>(),
                };
                // var rootObject = new detailrq.Root();
                rootObject.accommodations.Add(Hotelid);
                rootObject.extras.Add("description");
                rootObject.extras.Add("facilities");
                rootObject.extras.Add("policies");
                rootObject.extras.Add("rooms");
                rootObject.languages.Add("en-gb");
                string Request = JsonConvert.SerializeObject(rootObject, Setting);
                return Request;
            }
            catch (Exception ex)
            {
                _ = _exception.Error(ex);
                return null;
            }
        }

        public async Task<PriceCheckBDCDto> Get_BookingDotCom_PriceCheck_v3(DataSet dsCartDetails, DataRow[] BDCDetails, string UserName, string Password, int i, int j, DataTable dtCheckPriceReqStructure, string UniqueNo, string AgentUniqueNo, double FinalPrice, bool isForBooking, double CompanyBuyingPrice, string IsFor, double commission, TokenData tokenData, int AgentId,  string Searchkey = "", string TechName = "")
        {
            PriceCheckBDCDto priceCheckBDCDto = new PriceCheckBDCDto();
            string responsetype = "Success";
            string time = "";
            string xslRequest = "";
            string ResponseData = "";
            string Error = "";
            string SupplierName = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["SupplierName"]).ToLower();
            int srphotelid = 0;
            try
            {
                srphotelid = Convert.ToInt32(dsCartDetails.Tables[0].Rows[0]["ServiceId"]);
            }
            catch (Exception ex)
            {
                _ = _exception.Error(ex, Searchkey);
            }
            try
            {
                #region Price Check RQ and Declaration
                #region Data
                double CurrencyFactor = 1;
                DataSet dsTemp = new DataSet();
                //SearchParameter SearchPara = new SearchParameter();
                string[] array = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomTypeId"]).Split(new String[] { "$$$" }, StringSplitOptions.None);
                string roomId = array[6];
                string Currency = array[4];
                if (Currency.ToLower() != Convert.ToString(tokenData.SiteCurrency).ToLower())
                {
                    CurrencyFactor = _commonMethod.GetROEByCurrency(Currency, tokenData.dtROE);
                }
                string Url = Convert.ToString(BDCDetails[0]["WebSiteURL"]);
                string UserId = Convert.ToString(BDCDetails[0]["UserId"]);
                int HotelId = Convert.ToInt32(dsCartDetails.Tables[0].Rows[i]["XMLHotelId"]);
                int UserCartId = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[j]["UserCartID"]);
                int UserCartDetailId = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[j]["UserCartDetailId"]);
                string RoomTypeId = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomTypeID"]);
                //string SupplierName = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["SupplierName"]).ToLower();
                string CheckIn = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["CheckInDate"]);
                CheckIn = CheckIn.Substring(0, 4) + "-" + CheckIn.Substring(4, 2) + "-" + CheckIn.Substring(6, 2);
                string CheckOut = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["CheckOutDate"]);
                CheckOut = CheckOut.Substring(0, 4) + "-" + CheckOut.Substring(4, 2) + "-" + CheckOut.Substring(6, 2);
                #endregion
                xslRequest = Get_BookingDotCom_PriceCheckRQ_v3(Url, HotelId, CheckIn, CheckOut, UserId, UserCartId, dsCartDetails, UserCartDetailId, Currency, j);
                _commonMethod.ThirdPartyBookingHistory(Convert.ToString(UniqueNo), Convert.ToString(AgentUniqueNo), xslRequest, "-", "Before Request hit Price Check", SupplierName, "response confirm", "-", "WithinEarth");
                #endregion

                if (xslRequest != "")
                {
                    if (!isForBooking)
                    {
                        dtCheckPriceReqStructure.Rows.Add(_commonMethod.AddCheckPriceReq(dtCheckPriceReqStructure.NewRow(), i + 1, UserCartId, UserCartDetailId, RoomTypeId, SupplierName, xslRequest));
                    }
                    #region Price Check RS

                    System.Diagnostics.Stopwatch timerroomprocess = new System.Diagnostics.Stopwatch();
                    timerroomprocess.Start();

                    ResponseDto responseDto = await BookingDotComv3Response(xslRequest, Url + "/orders/preview", UserName, Password, 120000);
                    ResponseData = responseDto.Response;
                    Error = responseDto.Error;

                    timerroomprocess.Stop();
                    time = timerroomprocess.Elapsed.ToString();

                    _commonMethod.ThirdPartyBookingHistory(Convert.ToString(UniqueNo), Convert.ToString(AgentUniqueNo), xslRequest, ResponseData, "After Request hit Price Check", SupplierName, "response confirm", "-", "WithinEarth");
                    if (ResponseData == string.Empty || ResponseData == null)
                    {

                        try
                        {
                            responsetype = "GeneralError";
                            string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, xslRequest, ResponseData, Error, Searchkey, TechName, srphotelid);
                        }
                        catch (Exception e)
                        {
                            _ = _exception.Error(e);
                        }
                        priceCheckBDCDto.Flag = false;
                        return priceCheckBDCDto;
                    }
                    if (ResponseData.Contains("Error"))
                    {
                        try
                        {
                            responsetype = "GeneralError";
                            string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, xslRequest, ResponseData, Error, Searchkey, TechName, srphotelid);
                        }
                        catch (Exception e)
                        {
                            _ = _exception.Error(e);
                        }
                        priceCheckBDCDto.Flag = false;
                        return priceCheckBDCDto;
                    }
                    //BookingDotComRoom.RootObject Response = JsonConvert.DeserializeObject<BookingDotComRoom.RootObject>(ResponseData);
                    preebookrs.Root Response = JsonConvert.DeserializeObject<preebookrs.Root>(ResponseData);
                    #endregion

                    #region Price Check Logic
                    if (Response != null)
                    {
                        double Final_Rate = 0;
                        double incremental_price = 0;
                        string Paymenttype = "";
                        if (Response.data.accommodation.general_policies != null)
                        {
                            if (Response.data.accommodation.general_policies.payment != null)
                            {
                                if (Response.data.accommodation.general_policies.payment.pay_online_later != null)
                                {
                                    if (Response.data.accommodation.general_policies.payment.pay_online_later.method_required == true)
                                    {
                                        Paymenttype = "pay_online_later";
                                    }
                                    else
                                    {
                                        Paymenttype = "pay_online_now";
                                    }
                                }
                                else
                                {
                                    if (Response.data.accommodation.general_policies.payment.pay_online_now != null)
                                    {
                                        Paymenttype = "pay_online_now"; //27-june-2025
                                    }
                                    else
                                    {
                                        Paymenttype = "pay_online_later";
                                    }
                                }
                            }
                            else
                            {
                                Paymenttype = "pay_online_later";
                            }
                        }
                        for (int h = 0; h < Response.data.accommodation.products.Count; h++)
                        {

                            string blockId = Convert.ToString(Response.data.accommodation.products[h].id);
                            if (blockId == roomId)
                            {
                                // Final_Rate = Convert.ToDouble(Response.result[h].block[l].min_price.price);
                                // FinalPrice = Final_Rate * CurrencyFactor;
                                //commission = Convert.ToDouble(Response.result[h].block[l].commission_details.commission_amount.amount) * CurrencyFactor;
                                incremental_price += Convert.ToDouble(Response.data.accommodation.products[h].price.total.booker_currency);

                            }

                        }
                        FinalPrice = incremental_price * CurrencyFactor;

                        string token = "";
                        token = Convert.ToString(Response.data.order_token);
                        token = token + "@@@" + Paymenttype;
                        if (IsFor == "P")
                        {
                            CompanyBuyingPrice = Convert.ToDouble(dsCartDetails.Tables[1].DefaultView[j]["CompanyBuyingPrice"]);
                        }
                        else
                        {
                            CompanyBuyingPrice = Convert.ToDouble(dsCartDetails.Tables[1].DefaultView[j]["Rate"]);
                        }
                        FinalPrice = Convert.ToDouble(FinalPrice);
                        priceCheckBDCDto.FinalPrice = FinalPrice;

                        if (Convert.ToDouble(FinalPrice.TruncateDecimalPlaces(0)) > CompanyBuyingPrice.TruncateDecimalPlaces(0))
                        {
                            try
                            {
                                responsetype = "GeneralError";
                                string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, xslRequest, ResponseData, Error, Searchkey, TechName, srphotelid);
                            }
                            catch (Exception e)
                            {
                                _ = _exception.Error(e);
                            }
                            priceCheckBDCDto.Flag = true;
                            return priceCheckBDCDto;
                        }

                        #region Para
                        await _commonMethod.UpdateBookingv3Token(
                            Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["UserCartDetailId"]),
                            Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["UserCartId"]),
                            Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["SupplierName"]),
                            Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomTypeId"]), token);
                        #endregion
                    }
                    #endregion
                }
                else
                {
                    try
                    {
                        responsetype = "GeneralError";
                        string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, xslRequest, ResponseData, Error, Searchkey, TechName, srphotelid);
                    }
                    catch (Exception e)
                    {
                        _ = _exception.Error(e);
                    }
                    priceCheckBDCDto.Flag = true;
                    return priceCheckBDCDto;
                }
            }
            catch (Exception e)
            {
                _ = _exception.Error(e);
                priceCheckBDCDto.Flag = true;
                return priceCheckBDCDto;
            }

            try
            {
                responsetype = "Success";
                string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, xslRequest, ResponseData, Error, Searchkey, TechName, srphotelid);
            }
            catch (Exception e)
            {
                _ = _exception.Error(e);
            }
            priceCheckBDCDto.Flag = false;
            return priceCheckBDCDto;
        }

        public string Get_BookingDotCom_PriceCheckRQ_v3(string Url, int HotelId, string CheckIn, string checkout, string UserId, int UserCartId, DataSet dsCartDetails, int UserCartDetailId, string Currency, int j)
        {
            try
            {
                #region Price Check RQ
                string SupplierName = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["SupplierName"]).ToLower();
                string RoomTypeId = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomTypeId"]);
                string[] spitid1 = RoomTypeId.Split(new String[] { "$$$" }, StringSplitOptions.None);
                string Nationality = spitid1[7];
                string Block_ID = spitid1[6];
                //string Platform = "desktop";
                string Platform = "mobile";


                preebookrq.Root RootObject = new preebookrq.Root();
                preebookrq.Booker Booker = new preebookrq.Booker();
                Booker.country = Nationality.ToLower();
                Booker.platform = Platform;
                Booker.travel_purpose = "leisure";
                List<string> extras = new List<string>();
                string extra = "";
                if (true)// (SupplierName.ToLower() == "booking.comg_v3") //Change on 28-10-25
                {
                    extra = "authenticated,genius";
                    string[] user_groupss = extra.Split(',');
                    for (int ii = 0; ii < (user_groupss.Length); ii++)
                    {
                        extras.Add(Convert.ToString(user_groupss[ii]));
                    }
                }
                else
                {
                    extra = "authenticated";
                    extras.Add(extra);
                }
                //extras.Add(extra);
                Booker.user_groups = extras;
                RootObject.booker = Booker;
                RootObject.currency = Currency;
                preebookrq.Accommodation Accommodation = new preebookrq.Accommodation();

                Accommodation.id = HotelId;
                Accommodation.checkin = CheckIn;
                Accommodation.checkout = checkout;


                int Adults = 0;
                int adults = 0;
                List<preebookrq.Product> Roomdetail = new List<preebookrq.Product>();
                preebookrq.Product Roomdetails = new preebookrq.Product();
                Roomdetails.id = Block_ID;
                DataRow[] drUserCartCount = dsCartDetails.Tables[1].Select("UserCartDetailId='" + UserCartDetailId + "'");
                List<int> Child = new List<int>();
                for (int i = 0; i < drUserCartCount.Length; i++)
                {

                    Adults = Convert.ToInt32(drUserCartCount[i]["NoOfAdults"]);
                    string Child1Age = Convert.ToString(drUserCartCount[i]["ChildAge1"]);
                    string Child2Age = Convert.ToString(drUserCartCount[i]["ChildAge2"]);
                    int NoofChild = Convert.ToInt32(drUserCartCount[i]["NoOfChild"]);

                    //for (int a = 0; a < adults; a++)
                    //{

                    //        Adults = Adults + adults;
                    //}
                    if (NoofChild > 0)
                    {
                        if (NoofChild == 1)
                        {
                            Child.Add(Convert.ToInt32(drUserCartCount[i]["ChildAge1"]));
                            //Roomdetails.children.Add(Convert.ToInt32(drUserCartCount[i]["ChildAge1"]));
                        }
                        else if (NoofChild == 2)
                        {
                            Child.Add(Convert.ToInt32(drUserCartCount[i]["ChildAge1"]));
                            Child.Add(Convert.ToInt32(drUserCartCount[i]["ChildAge2"]));
                        }
                    }

                }
                preebookrq.Allocation Allocation = new preebookrq.Allocation();

                Allocation.number_of_adults = Adults;
                Allocation.children = Child;
                Roomdetails.allocation = Allocation;
                Roomdetail.Add(Roomdetails);
                Accommodation.products = Roomdetail;
                RootObject.accommodation = Accommodation;
                JsonSerializerSettings Setting = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    DefaultValueHandling = DefaultValueHandling.Ignore
                };
                string Request = JsonConvert.SerializeObject(RootObject, Setting);
                return Request;
                #endregion
            }
            catch (Exception ex)
            {
                _ = _exception.Error(ex);
                return "";
            }

        }

        public string Get_BookingDotCom_BookingRequest_v3(DataTable dtPassengers, DataSet dsCartDetails, bool IsPriceUpdate, int i, string HotelId, string UserID, string Password, string[] spitid, double Prices, int j, ref string PANN, string UniqueNo, bool IsPruvo)
        {
            try
            {
                #region Data
                string CheckIn = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["CheckInDate"]);
                CheckIn = CheckIn.Substring(0, 4) + "-" + CheckIn.Substring(4, 2) + "-" + CheckIn.Substring(6, 2);
                string CheckOut = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["CheckOutDate"]);
                CheckOut = CheckOut.Substring(0, 4) + "-" + CheckOut.Substring(4, 2) + "-" + CheckOut.Substring(6, 2);
                //string blockid = spitid[0];
                string remark = "";
                if (dsCartDetails.Tables[0].Columns.Contains("SpecialRequest"))
                {
                    remark = Convert.ToString(dsCartDetails.Tables[0].Rows[0]["SpecialRequest"]);
                    if (remark != "")
                        remark = remark.Replace("SpecialRequest:", "").Replace("$$", ",").Replace("/Remark:", ",");
                }
                string[] array = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomTypeId"]).Split(new String[] { "$$$" }, StringSplitOptions.None);
                string Currency = array[4];
                string Rate = array[5];
                string blockid = array[6];


                //string PayLeter = array[9];
                //bool PayLeter = true;//array[8];
                // bool isTourismFee = Convert.ToBoolean(array[7]);
                //string AllTaxes = array[8];

                //string Nationality = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["XmlNationality"]);
                string Nationality = array[7];//Added on 22-AUG-2025 due to swap Nationality as per mustafa sir

                string Purchasetoken = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["PurchaseToken"]);
                string[] array1 = Convert.ToString(Purchasetoken).Split(new String[] { "@@@" }, StringSplitOptions.None);
                string order_token = array1[0];
                string Payment_Type = array1[1];
                string session_id = "";
                //if (IsPriceUpdate)
                //    session_id = "";
                //else
                //    session_id = SessionID;
                int NoofAdults = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[j]["NoOfAdults"]);
                int NoOfChild = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[j]["NoOfChild"]);
                int TotalPax = NoofAdults + NoOfChild;
                string nn = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["NationalityName"]);
                string city = "";

                string adults = "";
                string childage = "";
                HotelSearchParameter newsearchpar = new HotelSearchParameter();//null;
                int rooms = Convert.ToInt32(newsearchpar.Rooms.Count);
                DataSet dsd = _commonMethod.ReadXmlFileToDataSet("Hotel/BookingDotCom_Booker_City.xml"); ;
                if (dsd != null)
                {
                    DataRow[] dr = dsd.Tables[0].Select("XMLNationality='" + nn + "'");
                    if (dr.Length > 0)
                        city = Convert.ToString(dr[0]["Capital"]);
                }
                #endregion

                #region Booking RQ
                #region  IsLead Filter Logic
                string isLead = string.Empty;
                dtPassengers.DefaultView.RowFilter = "PickUp='" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomTypeID"]) + "' and RoomNo='" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomNumber"]) + "' and IsLead=1";
                if (dtPassengers.DefaultView.Count == 0)
                    isLead = "true";
                dtPassengers.DefaultView.RowFilter = "";
                dtPassengers.DefaultView.RowFilter = "PickUp='" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomTypeID"]) + "' and RoomNo='" + (dsCartDetails.Tables[1].DefaultView[j]["RoomNumber"]) + "' and UserCartId='" + dsCartDetails.Tables[0].Rows[i]["UserCartId"] + "'";
                dtPassengers.DefaultView.Sort = "IsLead desc";
                #endregion

                #region APISO VC
                int usercartdetailsid = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[j]["UserCartDetailId"]); //#needuncmtwholeregion
                int UserCartId = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[j]["UserCartID"]);
                //get bookingid and detailsid
                DataTable dtbookingdata = _commonMethod.GetApisoBookingData(usercartdetailsid);

                string Pan = "";
                string CVV = "0", Expiry = "0";

                if (dtbookingdata != null)
                {
                    _vcn.GetApisoServiceDatatoCreateVCN("", Convert.ToInt32(dtbookingdata.Rows[0]["OnlineBookingMasterId"]), Convert.ToInt32(dtbookingdata.Rows[0]["OnlineBookingDetailsId"]), ref Pan, ref CVV, ref Expiry, IsPruvo);
                    PANN = Pan;
                }

                #endregion
                string FName = Convert.ToString(dtPassengers.DefaultView[0]["FirstName"]);//removed p bcs Ticket no 14484
                string LName = Convert.ToString(dtPassengers.DefaultView[0]["LastName"]);//removed p bcs Ticket no 14484
                string ExpiryRegex = Convert.ToString(Expiry).Substring(2, 2); //#needuncmt
                string ExpiryRegexFisrt = Convert.ToString(Expiry).Substring(0, 2); //#needuncmt                                                                        
                string Child1Age = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["ChildAge1"]);
                string Child2Age = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["ChildAge2"]);

                bookingrq.Root bookingRequest = new bookingrq.Root();
                bookingrq.Accommodation Accommodation = new bookingrq.Accommodation();
                Accommodation.label = UniqueNo;
                List<bookingrq.Product> product = new List<bookingrq.Product>();
                bookingrq.Product produc = new bookingrq.Product();
                //produc.bed_configuration = "";
                produc.id = blockid;
                List<bookingrq.Guest> guests = new List<bookingrq.Guest>();
                bookingrq.Guest guest = new bookingrq.Guest();
                guest.email = "ops@withinearth.com";
                guest.name = FName + " " + LName;
                guests.Add(guest);
                produc.guests = guests;
                product.Add(produc);
                Accommodation.products = product;
                bookingrq.Remarks Remarks = new bookingrq.Remarks();
                bookingrq.EstimatedArrivalTime EstimatedArrivalTime = new bookingrq.EstimatedArrivalTime();
                EstimatedArrivalTime.hour = 12;
                Remarks.estimated_arrival_time = EstimatedArrivalTime;
                Remarks.special_requests = remark;
                Accommodation.remarks = Remarks;
                bookingRequest.accommodation = Accommodation;
                bookingrq.Booker Booker = new bookingrq.Booker();
                bookingrq.Address Address = new bookingrq.Address();

                Address.address_line = "attre derrto";
                Address.city = city;
                Address.country = Nationality.ToLower();
                Address.post_code = "123456";
                Booker.address = Address;
                Booker.company = "WITHIN EARTH HOLIDAYS";
                Booker.email = "ops@withinearth.com";
                Booker.language = "en-gb";
                bookingrq.Name Name = new bookingrq.Name();

                Name.first_name = FName;
                Name.last_name = LName;
                Booker.name = Name;
                Booker.telephone = "60321663969";
                bookingRequest.booker = Booker;
                bookingRequest.order_token = order_token;
                bookingrq.Payment Payment = new bookingrq.Payment();
                bookingrq.Card Card = new bookingrq.Card();

                bookingrq.authentication authentication = new bookingrq.authentication();
                //bookingrq.riskified riskified = new bookingrq.riskified();
                //riskified.ip_address = "89.149.206.194";
                //riskified.session_id = "132465568";
                authentication.sca_exemption = "virtual";
                //authentication.riskified = riskified;
                Card.authentication = authentication;

                Card.cardholder = "WITHIN EARTH HOLIDAYS";
                Card.cvc = CVV; //#needuncmt
                                //Card.cvc = "011";
                Card.expiry_date = "20" + ExpiryRegexFisrt + "-" + ExpiryRegex;// + " - 01";//#needuncmt
                                                                               //Card.expiry_date = "2025-12";
                Card.number = "#PAN";//#needuncmt           
                                     // Card.number = "5434641005562893";


                //Card.cardholder = "Withinearth holidays";
                ////Card.cvc = CVV; #needuncmt
                //Card.cvc = "430";
                ////Card.expiry_date = "20" + ExpiryRegexFisrt + " - " + ExpiryRegex + " - 01";#needuncmt
                //Card.expiry_date = "2025-11";
                ////Card.number = "#PAN";#needuncmt           
                //Card.number = "5116590200383610";


                Payment.card = Card;
                //Payment.include_receipt = true;
                Payment.method = "card";
                Payment.timing = Payment_Type;// pay_online_later;  "pay_online_now";//
                bookingRequest.payment = Payment;
                JsonSerializerSettings settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    DefaultValueHandling = DefaultValueHandling.Ignore
                };

                // Serialize the booking request object to JSON
                string requestJson = JsonConvert.SerializeObject(bookingRequest, settings);
                return requestJson;
                #endregion
            }
            catch (Exception exc)
            {
                _ = _exception.Error(exc);
                _ = _exception.ErrorNew(exc, _LogAPI.Book);
                return "";
            }
        }

        public string Get_BookingDotCom_BookingCancelRQ_v3(string AgentVoucherNo, string CancelRes)
        {
            try
            {
                #region Booking Cancel RQ
                string[] spitid = AgentVoucherNo.Split(new String[] { "|" }, StringSplitOptions.None); // uncmnt// #ticket60983 8-JULY_2025
                JsonSerializerSettings Setting = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    DefaultValueHandling = DefaultValueHandling.Ignore
                };
                cancelrq.Root RootObject = new cancelrq.Root();
                RootObject.order = Convert.ToString(spitid[0]); // uncmnt// #ticket60983 8-JULY_2025
                //RootObject.order = Convert.ToString(AgentVoucherNo); // cmnt// #ticket60983 8-JULY_2025
                RootObject.reason = CancelRes;
                string Request = JsonConvert.SerializeObject(RootObject, Setting);
                return Request;
                #endregion
            }
            catch (Exception ex)
            {
                _=_exception.ErrorNew(ex, _LogAPI.CancelBooking);
                return "";
            }
        }

        public async Task<bool> Get_BookingDotCom_RecheckHit_v3(string UserName, string Password, DataTable dtCheckPriceReqStructure, string AgentUniqueNo, double FinalPrice, bool isForBooking, double CompanyBuyingPrice, string IsFor, string Provider, string RoomTypeId, string Currency, string hotelid, string Url, string UserId, string CheckIn, string CheckOut, int Rooms, HotelSearchParameter SearchPara, bool IsPriceChange, string searchKey, TokenData tokenData)
        {
            string responsetype = "Success";
            string time = "";
            string xslRequest = "";
            string ResponseData = "";
            string Error = "";
            string SupplierName = Provider;
            int Adults = 0;
            int Childs = 0;
            bool isSuccess = true;
            try
            {
                #region Price Check RQ and Declaration
                #region Data
                double CurrencyFactor = 1;
                DataSet dsTemp = new DataSet();
                string[] array = Convert.ToString(RoomTypeId).Split(new String[] { "$$$" }, StringSplitOptions.None);
                if (Currency.ToLower() != Convert.ToString(tokenData.SiteCurrency).ToLower())
                {
                    CurrencyFactor = _commonMethod.GetROEByCurrency(Currency, tokenData.dtROE);
                }

                int HotelId = Convert.ToInt32(hotelid);
                string roomId = array[6];
                string checkIn = SearchPara.CheckInDate.Substring(6, 4) + "-" + SearchPara.CheckInDate.Substring(3, 2) + "-" + SearchPara.CheckInDate.Substring(0, 2);
                string checkOut = SearchPara.CheckOutDate.Substring(6, 4) + "-" + SearchPara.CheckOutDate.Substring(3, 2) + "-" + SearchPara.CheckOutDate.Substring(0, 2);
                #endregion
                for (int rm = 0; rm < SearchPara.Rooms.Count; rm++)
                {
                    Adults += SearchPara.Rooms[rm].NoofAdults;
                    Childs += SearchPara.Rooms[rm].NoofChild;
                }
                xslRequest = Get_BookingDotCom_PriceRecheckHit_v3(Url, HotelId, checkIn, checkOut, UserId, Currency, Provider, RoomTypeId, Adults, Childs, SearchPara);
                _commonMethod.ThirdPartyGetPolicyHistory("", Convert.ToString(AgentUniqueNo), xslRequest, "-", "Before Request hit Price Check", SupplierName, "response confirm", "-", "WithinEarth");
                #endregion

                if (xslRequest != "")
                {
                    //if (!isForBooking)
                    //{
                    //    dtCheckPriceReqStructure.Rows.Add(Common.AddCheckPriceReq(dtCheckPriceReqStructure.NewRow(), 0, 0, 0, RoomTypeId, SupplierName, xslRequest));
                    //}
                    #region Price Check RS

                    System.Diagnostics.Stopwatch timerroomprocess = new System.Diagnostics.Stopwatch();
                    timerroomprocess.Start();

                    ResponseDto responseDto = await BookingDotComv3Response(xslRequest, Url + "/orders/preview", UserName, Password, 120000);
                    ResponseData = responseDto.Response;
                    Error = responseDto.Error;
                    timerroomprocess.Stop();
                    time = timerroomprocess.Elapsed.ToString();

                    _commonMethod.ThirdPartyGetPolicyHistory("", Convert.ToString(AgentUniqueNo), xslRequest, ResponseData + Error, "After Request hit Price Check", SupplierName, "response confirm", "-", "WithinEarth");
                    if (ResponseData == string.Empty || ResponseData == null)
                    {

                        try
                        {
                            responsetype = "GeneralError";
                            _= _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", tokenData.AgentId, time, responsetype, xslRequest, ResponseData, Error);
                        }
                        catch (Exception e)
                        {
                            _=_exception.ErrorNew(e, _LogAPI.Recheck, searchKey);
                        }
                        isSuccess = false;
                    }
                    if (ResponseData.Contains("Error"))
                    {
                        try
                        {
                            responsetype = "GeneralError";
                            _ = _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", tokenData.AgentId, time, responsetype, xslRequest, ResponseData, Error);
                        }
                        catch (Exception e)
                        {
                            _ = _exception.ErrorNew(e, _LogAPI.Recheck, searchKey);
                        }
                        isSuccess = false;

                    }
                    preebookrs.Root Response = JsonConvert.DeserializeObject<preebookrs.Root>(ResponseData);
                    #endregion

                    #region Price Check Logic
                    if (Response != null)
                    {

                        double incremental_price = 0;
                        string Paymenttype = "";
                        if (Response.data.accommodation.general_policies != null)
                        {
                            if (Response.data.accommodation.general_policies.payment != null)
                            {
                                if (Response.data.accommodation.general_policies.payment.pay_online_later != null)
                                {
                                    if (Response.data.accommodation.general_policies.payment.pay_online_later.method_required == true)
                                    {
                                        Paymenttype = "pay_online_later";
                                    }
                                    else
                                    {
                                        Paymenttype = "pay_online_now";
                                    }
                                }
                                else
                                {
                                    if (Response.data.accommodation.general_policies.payment.pay_online_now != null)
                                    {
                                        Paymenttype = "pay_online_now"; //27-june-2025
                                    }
                                    else
                                    {
                                        Paymenttype = "pay_online_later";
                                    }
                                }
                            }
                            else
                            {
                                Paymenttype = "pay_online_later";
                            }
                        }
                        for (int h = 0; h < Response.data.accommodation.products.Count; h++)
                        {

                            string blockId = Convert.ToString(Response.data.accommodation.products[h].id);
                            if (blockId == roomId)
                            {

                                incremental_price += Convert.ToDouble(Response.data.accommodation.products[h].price.total.booker_currency);
                            }

                        }
                        FinalPrice = incremental_price * CurrencyFactor;

                        string token = "";
                        token = Convert.ToString(Response.data.order_token);
                        token = token + "@@@" + Paymenttype;

                        FinalPrice = Convert.ToDouble(FinalPrice);
                        if (IsPriceChange == false)
                        {
                            if (Convert.ToDouble(FinalPrice.TruncateDecimalPlaces(0)) > CompanyBuyingPrice.TruncateDecimalPlaces(0))
                            {
                                try
                                {
                                    responsetype = "GeneralError";
                                    _=_commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", tokenData.AgentId, time, responsetype, xslRequest, ResponseData, Error);
                                    //isSuccess = false;

                                }
                                catch (Exception e)
                                {
                                 _ = _exception.ErrorNew(e, _LogAPI.Recheck, searchKey);

                                }
                                //return true;
                                isSuccess = false;
                            }
                        }


                    }
                    #endregion
                }
                else
                {
                    try
                    {
                        responsetype = "GeneralError";
                        _ = _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", tokenData.AgentId, time, responsetype, xslRequest, ResponseData, Error);
                    }
                    catch (Exception e)
                    {
                        _ = _exception.ErrorNew(e, _LogAPI.Recheck, searchKey);
                    }
                    isSuccess = false;

                }
            }
            catch (Exception e)
            {
                _ = _exception.ErrorNew(e, _LogAPI.Recheck, searchKey);
                isSuccess = false;

            }

            try
            {
                if (responsetype == "Success")
                {
                    _ = _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", tokenData.AgentId, time, responsetype, xslRequest, ResponseData, Error);
                    isSuccess = true;
                }
                else
                    isSuccess = false;
            }
            catch (Exception e)
            {
                isSuccess = false;
            }

            return isSuccess;

        }

        public string Get_BookingDotCom_PriceRecheckHit_v3(string Url, int HotelId, string CheckIn, string checkout, string UserId, string Currency, string SupplierName, string RoomTypeId, int Adults, int child, HotelSearchParameter SearchPara)
        {
            try
            {
                #region Price Check RQ
                string[] spitid1 = RoomTypeId.Split(new String[] { "$$$" }, StringSplitOptions.None);
                string Nationality = spitid1[7];
                string Block_ID = spitid1[6];
                string Platform = "mobile";


                preebookrq.Root RootObject = new preebookrq.Root();
                preebookrq.Booker Booker = new preebookrq.Booker();
                Booker.country = Nationality.ToLower();
                Booker.platform = Platform;
                Booker.travel_purpose = "leisure";
                List<string> extras = new List<string>();
                string extra = "";
                if (SupplierName.ToLower() == "booking.comg_v3")
                {
                    extra = "authenticated,genius";
                    string[] user_groupss = extra.Split(',');
                    for (int ii = 0; ii < (user_groupss.Length); ii++)
                    {
                        extras.Add(Convert.ToString(user_groupss[ii]));
                    }
                }
                else
                {
                    extra = "authenticated";
                    extras.Add(extra);
                }
                Booker.user_groups = extras;
                RootObject.booker = Booker;
                RootObject.currency = Currency;
                preebookrq.Accommodation Accommodation = new preebookrq.Accommodation();

                Accommodation.id = HotelId;
                Accommodation.checkin = CheckIn;
                Accommodation.checkout = checkout;


                List<preebookrq.Product> Roomdetail = new List<preebookrq.Product>();
                preebookrq.Product Roomdetails = new preebookrq.Product();
                Roomdetails.id = Block_ID;
                List<int> Child = new List<int>();
                int Adult = 0;

                foreach (var room in SearchPara.Rooms)
                {
                    Adult += room.NoofAdults;
                    int NoofChild = room.NoofChild;
                    if (NoofChild > 0)
                    {
                        if (room.NoofChild == 1)
                        {
                            Child.Add(Convert.ToInt32(room.Child1Age));
                        }
                        else if (room.NoofChild == 2)
                        {
                            Child.Add(Convert.ToInt32(room.Child1Age));
                            Child.Add(Convert.ToInt32(room.Child2Age));
                        }
                    }
                }
                preebookrq.Allocation Allocation = new preebookrq.Allocation();

                Allocation.number_of_adults = Adults;
                Allocation.children = Child;
                Roomdetails.allocation = Allocation;
                Roomdetail.Add(Roomdetails);
                Accommodation.products = Roomdetail;
                RootObject.accommodation = Accommodation;
                JsonSerializerSettings Setting = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    DefaultValueHandling = DefaultValueHandling.Ignore
                };
                string Request = JsonConvert.SerializeObject(RootObject, Setting);
                return Request;
                #endregion
            }
            catch (Exception ex)
            {
                _=_exception.ErrorNew(ex, _LogAPI.Recheck);
                return "";
            }

        }
    }
}
