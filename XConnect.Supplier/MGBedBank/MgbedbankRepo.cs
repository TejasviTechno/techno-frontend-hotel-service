using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XConnect.Models.Hotel;
using XConnect.Models;
using XConnect.Supplier.MGBedBank.Interface;
using static Azure.Core.HttpHeader;
using Microsoft.Extensions.Options;
using XConnect.CommonUtilities.Interface;
using XConnect.ErrorLogHandler.Interface;
using XConnect.Markup.Interface;
using XConnect.CommonUtilities;
using static XConnect.Supplier.Juniper.JuniperRepo;
using Options = XConnect.Models.Options;
using MathNet.Numerics;
using static XConnect.Supplier.Booking.Com.preebookrs;
using System.Net;

namespace XConnect.Supplier.MGBedBank
{
    public class MgbedbankRepo : IMgbedbank
    {
        private readonly ICommonMethod _commonMethod;
        private readonly IException _exception;
        private readonly IMarkup _markup;
        private readonly ISupplierRes _supplierRes;
        private readonly KeySettings _keySettings;
        public MgbedbankRepo(ICommonMethod commonMethod, IMarkup markup, ISupplierRes supplierRes, IException exception, IOptions<KeySettings> keySettings)
        {
            _commonMethod = commonMethod;
            _markup = markup;
            _supplierRes = supplierRes;
            _exception = exception;
            _keySettings = keySettings.Value;
        }

        public void GetMgBedBankRequestOnly_async(DataRow[] commonCode, List<StarRating> listRating, string SupplierName, HotelSearchParameter SearchPara, List<SupplierDetails> MgBedBankDetails, List<SupplierSearchConfig> MgBedBankSearchConfig, DataTable dtPreferredDataHotel, ref ConcurrentBag<HotelX> AllTask, string XMLCity, string XMLCountry, string Xmlnationality, int Hit, CommonSearchList commonSearchList)
        {
            try
            {
                string Ids = string.Empty;
                string xmlbase = "";
                _commonMethod.GetPrefferedHotelCodes(ref Ids, ref xmlbase, ref MgBedBankSearchConfig, SupplierName, dtPreferredDataHotel, SearchPara.IsSearchFor);
                if (MgBedBankSearchConfig != null && commonCode != null)
                {
                    double CurrencyFactorMgBedBank = 1;
                    //if (XMLHotelBookingCommon.MgBedBankCurrency.ToLower() != "usd")
                    //{
                    //    DataTable dtInrCurrency = Common.GetCountryCurrency(XMLHotelBookingCommon.MgBedBankCurrency);
                    //    CurrencyFactorMgBedBank = Convert.ToDouble(dtInrCurrency.Rows[0]["AmtToExch"]);
                    //}
                    if (commonCode.Length > 0)
                    {
                        for (int k = 0; k < MgBedBankSearchConfig.Count; k++)
                        {
                            bool DebugMode = (Convert.ToInt32(MgBedBankSearchConfig[k].DebugMode) == 0 ? false : true);
                            string PaxPassport = Xmlnationality;

                            #region cityWise
                            try
                            {
                                if (Convert.ToInt32(MgBedBankSearchConfig[k].CityID) == 0 || Convert.ToInt32(MgBedBankSearchConfig[k].CityID) == SearchPara.CityID)
                                {
                                    int Noofrecords = Convert.ToInt32(MgBedBankSearchConfig[k].Noofrecords);
                                    string IdsForSearch = "";
                                    if (true)//(Convert.ToInt32(MgBedBankSearchConfig[k].City) == 1)
                                    {
                                        //if (commonSearchList.HotelIDS.Count == 1)// (SearchPara.Filters.IsShowRooms == 1)
                                        if (SearchPara.Filters.IsShowRooms == 1)
                                        {
                                            DataTable[] splittedtables = commonCode.AsEnumerable()
                                                                .Select((row, index) => new { row, index })
                                                                .GroupBy(x => x.index / Noofrecords)  // integer division, the fractional part is truncated
                                                                .Select(g => g.Select(x => x.row).CopyToDataTable())
                                                                .ToArray();
                                            for (int a = 0; a < splittedtables.Length; a++)
                                            {

                                                var paramValues1 = String.Join(",", splittedtables[a].Select().Select(x => String.Join(",", Convert.ToString(x.ItemArray[0]))));
                                                IdsForSearch = Convert.ToString(paramValues1);
                                                HotelX xhotel = new HotelX();
                                                xhotel.Supplier = SupplierName;
                                                xhotel.DebugeMode = DebugMode;
                                                xhotel.SearchTimeOut = Convert.ToInt32(MgBedBankSearchConfig[k].HotelSearchTimeOut);
                                                //if (Hit > 0)
                                                xhotel.ProcessTimeout = Convert.ToInt32(MgBedBankSearchConfig[k].HotelSearchTimeOut2);

                                                xhotel.URL = Convert.ToString(MgBedBankDetails[0].WebSiteURL) + MGBedbackStaticInfo.Searchs;
                                                xhotel.UserId = Convert.ToString(MgBedBankDetails[0].UserID);
                                                xhotel.UserName = Convert.ToString(MgBedBankDetails[0].UserName);
                                                xhotel.PassWord = Convert.ToString(MgBedBankDetails[0].Password);
                                                xhotel.CurrencyCode = MGBedbackStaticInfo.MgBedBankCurrency;
                                                xhotel.CurrencyFactor = CurrencyFactorMgBedBank;
                                                xhotel.XmlCity = XMLCity;
                                                xhotel.XmlCountry = XMLCountry;
                                                xhotel.XmlNationality = Xmlnationality;
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
                                                    objrq.Request = (XMLMgBedBankRequestOnly_async(xhotel.URL, xhotel.UserId, xhotel.UserName, xhotel.PassWord, PaxPassport, xhotel.SearchTimeOut, SearchPara, IdsForSearch, xhotel.CurrencyFactor, DebugMode, XMLCity, xhotel.XmlCountry, xhotel.XmlNationality, Noofrecords, MGBedbackStaticInfo.MgBedBankCurrency));
                                                    objrq.Response = ""; RQRS.Add(objrq);
                                                }
                                                xhotel.RQRS = RQRS;
                                                #endregion
                                                AllTask.Add(xhotel);
                                            }
                                        }
                                        if (SearchPara.Filters.IsShowRooms == 0 && commonSearchList.SearchByHotel == false)
                                        {
                                            HotelX xhotel = new HotelX();
                                            xhotel.Supplier = SupplierName;
                                            xhotel.DebugeMode = DebugMode;
                                            xhotel.SearchTimeOut = Convert.ToInt32(MgBedBankSearchConfig[k].HotelSearchTimeOut);
                                            //if (Hit > 0)
                                            xhotel.ProcessTimeout = Convert.ToInt32(MgBedBankSearchConfig[k].HotelSearchTimeOut2);

                                            xhotel.URL = Convert.ToString(MgBedBankDetails[0].WebSiteURL) + MGBedbackStaticInfo.Searchs;
                                            xhotel.UserId = Convert.ToString(MgBedBankDetails[0].UserID);
                                            xhotel.UserName = Convert.ToString(MgBedBankDetails[0].UserName);
                                            xhotel.PassWord = Convert.ToString(MgBedBankDetails[0].Password);
                                            xhotel.CurrencyCode = MGBedbackStaticInfo.MgBedBankCurrency;
                                            xhotel.CurrencyFactor = CurrencyFactorMgBedBank;
                                            xhotel.XmlCity = XMLCity;
                                            xhotel.XmlCountry = XMLCountry;
                                            xhotel.XmlNationality = Xmlnationality;
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
                                                objrq.Request = (XMLMgBedBankRequestOnly_async(xhotel.URL, xhotel.UserId, xhotel.UserName, xhotel.PassWord, PaxPassport, xhotel.SearchTimeOut, SearchPara, IdsForSearch, xhotel.CurrencyFactor, DebugMode, XMLCity, xhotel.XmlCountry, xhotel.XmlNationality, Noofrecords, MGBedbackStaticInfo.MgBedBankCurrency));
                                                objrq.Response = ""; RQRS.Add(objrq);
                                            }
                                            xhotel.RQRS = RQRS;
                                            #endregion
                                            AllTask.Add(xhotel);
                                        }
                                    }
                                    else
                                    {
                                        if (commonSearchList.SearchByHotel && SearchPara.Filters.IsShowRooms == 1)
                                        {
                                            DataTable[] splittedtables = commonCode.AsEnumerable()
                                                                .Select((row, index) => new { row, index })
                                                                .GroupBy(x => x.index / Noofrecords)  // integer division, the fractional part is truncated
                                                                .Select(g => g.Select(x => x.row).CopyToDataTable())
                                                                .ToArray();
                                            for (int a = 0; a < splittedtables.Length; a++)
                                            {

                                                var paramValues1 = String.Join(",", splittedtables[a].Select().Select(x => String.Join(",", Convert.ToString(x.ItemArray[0]))));
                                                IdsForSearch = Convert.ToString(paramValues1);
                                                HotelX xhotel = new HotelX();
                                                xhotel.Supplier = SupplierName;
                                                xhotel.DebugeMode = DebugMode;
                                                xhotel.SearchTimeOut = Convert.ToInt32(MgBedBankSearchConfig[k].HotelSearchTimeOut);
                                                //if (Hit > 0)
                                                xhotel.ProcessTimeout = Convert.ToInt32(MgBedBankSearchConfig[k].HotelSearchTimeOut2);

                                                xhotel.URL = Convert.ToString(MgBedBankDetails[0].WebSiteURL) + MGBedbackStaticInfo.Searchs;
                                                xhotel.UserId = Convert.ToString(MgBedBankDetails[0].UserID);
                                                xhotel.UserName = Convert.ToString(MgBedBankDetails[0].UserName);
                                                xhotel.PassWord = Convert.ToString(MgBedBankDetails[0].Password);
                                                xhotel.CurrencyCode = MGBedbackStaticInfo.MgBedBankCurrency;
                                                xhotel.CurrencyFactor = CurrencyFactorMgBedBank;
                                                xhotel.XmlCity = XMLCity;
                                                xhotel.XmlCountry = XMLCountry;
                                                xhotel.XmlNationality = Xmlnationality;
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
                                                    objrq.Request = (XMLMgBedBankRequestOnly_async(xhotel.URL, xhotel.UserId, xhotel.UserName, xhotel.PassWord, PaxPassport, xhotel.SearchTimeOut, SearchPara, IdsForSearch, xhotel.CurrencyFactor, DebugMode, XMLCity, xhotel.XmlCountry, xhotel.XmlNationality, Noofrecords, MGBedbackStaticInfo.MgBedBankCurrency));
                                                    objrq.Response = ""; RQRS.Add(objrq);
                                                }
                                                xhotel.RQRS = RQRS;
                                                #endregion
                                                AllTask.Add(xhotel);
                                            }
                                        }
                                        if (commonSearchList.SearchByHotel == false && SearchPara.Filters.IsShowRooms == 0)
                                        {
                                            HotelX xhotel = new HotelX();
                                            xhotel.Supplier = SupplierName;
                                            xhotel.DebugeMode = DebugMode;
                                            xhotel.SearchTimeOut = Convert.ToInt32(MgBedBankSearchConfig[k].HotelSearchTimeOut);
                                            //if (Hit > 0)
                                            xhotel.ProcessTimeout = Convert.ToInt32(MgBedBankSearchConfig[k].HotelSearchTimeOut2);

                                            xhotel.URL = Convert.ToString(MgBedBankDetails[0].WebSiteURL) + MGBedbackStaticInfo.Searchs;
                                            xhotel.UserId = Convert.ToString(MgBedBankDetails[0].UserID);
                                            xhotel.UserName = Convert.ToString(MgBedBankDetails[0].UserName);
                                            xhotel.PassWord = Convert.ToString(MgBedBankDetails[0].Password);
                                            xhotel.CurrencyCode = MGBedbackStaticInfo.MgBedBankCurrency;
                                            xhotel.CurrencyFactor = CurrencyFactorMgBedBank;
                                            xhotel.XmlCity = XMLCity;
                                            xhotel.XmlCountry = XMLCountry;
                                            xhotel.XmlNationality = Xmlnationality;
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
                                                objrq.Request = (XMLMgBedBankRequestOnly_async(xhotel.URL, xhotel.UserId, xhotel.UserName, xhotel.PassWord, PaxPassport, xhotel.SearchTimeOut, SearchPara, IdsForSearch, xhotel.CurrencyFactor, DebugMode, XMLCity, xhotel.XmlCountry, xhotel.XmlNationality, Noofrecords, MGBedbackStaticInfo.MgBedBankCurrency));
                                                objrq.Response = ""; RQRS.Add(objrq);
                                            }
                                            xhotel.RQRS = RQRS;
                                            #endregion
                                            AllTask.Add(xhotel);
                                        }
                                    }


                                }
                            }
                            catch (Exception ex)
                            {
                                _exception.Error(ex);
                                //throw;
                            }
                            #endregion
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
            }
        }

        public void GetMgBedBankRequestOnly_async_new(DataRow[] commonCode, List<StarRating> listRating, HotelSearchParameter SearchPara, List<SupplierDetails> lstMgBedBankDetails, List<SupplierSearchConfig> lstMgBedBankSearchConfig, DataTable dtPreferredDataHotel, ref ConcurrentBag<HotelX> AllTask, int Hit, CommonSearchList commonSearchList, DataSet dtXMLCity, TokenData tokenData, DataTable dtROE)
        {
            try
            {

                if (lstMgBedBankDetails != null && lstMgBedBankDetails.Count > 0)
                {
                    List<SupplierDetails> JuniperMain = new List<SupplierDetails>();
                    List<SupplierDetails> JuniperChild = new List<SupplierDetails>();
                    List<StarRating> lstRating = new List<StarRating>();
                    lstRating = _commonMethod.GetStarRatingStringList_xtg(SearchPara);

                    List<HotelCodeByCity> DtHotelDAta = HtmlExtensions.ToList<HotelCodeByCity>(commonCode.CopyToDataTable());
                    int ratingcount = lstRating.Count;
                    int hotelcount = DtHotelDAta.Count;

                    foreach (SupplierDetails MgBedBankDetails in lstMgBedBankDetails)
                    {
                        int SupplierID = MgBedBankDetails.SupplierID;
                        string Supplier = MgBedBankDetails.SupplierName;

                        if (commonCode != null)
                        {
                            List<SupplierSearchConfig> MgBedBankSearchConfig = lstMgBedBankSearchConfig
                                   .Where(x => x != null && x.SupplierID == SupplierID)
                                   .ToList();

                            if (MgBedBankSearchConfig != null && MgBedBankSearchConfig.Count > 0)
                            {

                                bool IsCheck = true;
                                for (int z = 0; z < SearchPara.Rooms.Count; z++)
                                {
                                    int TotPax = Convert.ToInt32(SearchPara.Rooms[z].NoofAdults);
                                    if (TotPax > 8)
                                        IsCheck = false;
                                }
                                if (IsCheck)
                                {
                                    DataForSearch dataForSearch = _commonMethod.GetXmlDataForSearchTechwise(SearchPara.Nationality, dtXMLCity, Supplier, MgBedBankDetails.TechName);

                                    if (!string.IsNullOrEmpty(dataForSearch.XmlNationality) && !string.IsNullOrEmpty(dataForSearch.xmlCity) && !string.IsNullOrEmpty(dataForSearch.xmlCountry))
                                    {
                                        string Ids = string.Empty;
                                        string xmlbase = "";
                                        _commonMethod.GetPrefferedHotelCodes(ref Ids, ref xmlbase, ref MgBedBankSearchConfig, Supplier, dtPreferredDataHotel, SearchPara.IsSearchFor);

                                        string Currency = MgBedBankDetails.CurrencyShortName;
                                        double CurrencyFactorMgBedBank = 1;
                                        if (Currency.ToLower() != tokenData.SiteCurrency.ToLower())
                                        {
                                            CurrencyFactorMgBedBank = _commonMethod.GetROEByCurrency(Currency, dtROE);
                                        }

                                        List<HotelCodeByCity> filter = new List<HotelCodeByCity>();
                                        filter = _commonMethod.FilterWithOutCity_Code(ratingcount, hotelcount, DtHotelDAta, lstRating, Supplier);

                                        if (filter.Count > 0)
                                        {
                                            for (int k = 0; k < MgBedBankSearchConfig.Count; k++)
                                            {
                                                bool DebugMode = (Convert.ToInt32(MgBedBankSearchConfig[k].DebugMode) == 0 ? false : true);
                                                string PaxPassport = dataForSearch.XmlNationality;

                                                #region cityWise
                                                try
                                                {
                                                    if (Convert.ToInt32(MgBedBankSearchConfig[k].CityID) == 0 || Convert.ToInt32(MgBedBankSearchConfig[k].CityID) == SearchPara.CityID)
                                                    {
                                                        int Noofrecords = Convert.ToInt32(MgBedBankSearchConfig[k].Noofrecords);
                                                        string IdsForSearch = "";
                                                        if (true)//(Convert.ToInt32(MgBedBankSearchConfig[k].City) == 1)
                                                        {
                                                            //if (commonSearchList.HotelIDS.Count == 1)// (SearchPara.Filters.IsShowRooms == 1)
                                                            if (SearchPara.Filters.IsShowRooms == 1)
                                                            {
                                                                double count = Convert.ToDouble(filter.Count);
                                                                int NoOfDiv = Convert.ToInt32(Math.Ceiling(count / Convert.ToDouble(Noofrecords)));
                                                                List<List<XMLID_Search>> lstxml = _commonMethod.GetXMLIDString(NoOfDiv, Noofrecords, count, filter);

                                                                for (int a = 0; a < Noofrecords; a++)
                                                                {
                                                                    IdsForSearch = Convert.ToString(_commonMethod.GetHotelCodeByCOmma(lstxml[a]));
                                                                    HotelX xhotel = new HotelX();
                                                                    xhotel.Supplier = Supplier;
                                                                    xhotel.DebugeMode = DebugMode;
                                                                    xhotel.SearchTimeOut = Convert.ToInt32(MgBedBankSearchConfig[k].HotelSearchTimeOut);
                                                                    //if (Hit > 0)
                                                                    xhotel.ProcessTimeout = Convert.ToInt32(MgBedBankSearchConfig[k].HotelSearchTimeOut2);

                                                                    //xhotel.URL = Convert.ToString(MgBedBankDetails.WebSiteURL) + MGBedbackStaticInfo.Searchs;
                                                                    if (_keySettings.SupplierProxy == "0")
                                                                        xhotel.URL = Convert.ToString(MGBedbackStaticInfo.WebSiteURL) + MGBedbackStaticInfo.Searchs; //Added Static on 14-OCT 2025 Tkt 62064
                                                                    else
                                                                        xhotel.URL = Convert.ToString(MGBedbackStaticInfo.WebSiteURLforAPI) + MGBedbackStaticInfo.Searchs;  //Added Static on 14-OCT 2025 Tkt 62064

                                                                    xhotel.UserId = Convert.ToString(MgBedBankDetails.UserID);
                                                                    xhotel.UserName = Convert.ToString(MgBedBankDetails.UserName);
                                                                    xhotel.PassWord = Convert.ToString(MgBedBankDetails.Password);
                                                                    xhotel.CurrencyCode = Currency;
                                                                    xhotel.CurrencyFactor = CurrencyFactorMgBedBank;
                                                                    xhotel.XmlCity = dataForSearch.xmlCity;
                                                                    xhotel.XmlCountry = dataForSearch.xmlCountry;
                                                                    xhotel.XmlNationality = dataForSearch.XmlNationality;
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
                                                                        objrq.Request = (XMLMgBedBankRequestOnly_async(xhotel.URL, xhotel.UserId, xhotel.UserName, xhotel.PassWord, PaxPassport, xhotel.SearchTimeOut, SearchPara, IdsForSearch, xhotel.CurrencyFactor, DebugMode, dataForSearch.xmlCity, xhotel.XmlCountry, xhotel.XmlNationality, Noofrecords, xhotel.CurrencyCode));
                                                                        objrq.Response = ""; RQRS.Add(objrq);
                                                                    }
                                                                    xhotel.RQRS = RQRS;
                                                                    xhotel.ReturnRooms = MgBedBankDetails.ReturnRooms;
                                                                    xhotel.TechName = MgBedBankDetails.TechName;
                                                                    #endregion
                                                                    AllTask.Add(xhotel);
                                                                }
                                                            }
                                                            if (SearchPara.Filters.IsShowRooms == 0 && commonSearchList.SearchByHotel == false)
                                                            {
                                                                HotelX xhotel = new HotelX();
                                                                xhotel.Supplier = Supplier;
                                                                xhotel.DebugeMode = DebugMode;
                                                                xhotel.SearchTimeOut = Convert.ToInt32(MgBedBankSearchConfig[k].HotelSearchTimeOut);
                                                                //if (Hit > 0)
                                                                xhotel.ProcessTimeout = Convert.ToInt32(MgBedBankSearchConfig[k].HotelSearchTimeOut2);

                                                                //xhotel.URL = Convert.ToString(MgBedBankDetails.WebSiteURL) + MGBedbackStaticInfo.Searchs;

                                                                if (_keySettings.SupplierProxy == "0")
                                                                    xhotel.URL = Convert.ToString(MGBedbackStaticInfo.WebSiteURL) + MGBedbackStaticInfo.Searchs; //Added Static on 14-OCT 2025 Tkt 62064
                                                                else
                                                                    xhotel.URL = Convert.ToString(MGBedbackStaticInfo.WebSiteURLforAPI) + MGBedbackStaticInfo.Searchs; //Added Static on 14-OCT 2025 Tkt 62064

                                                                xhotel.UserId = Convert.ToString(MgBedBankDetails.UserID);
                                                                xhotel.UserName = Convert.ToString(MgBedBankDetails.UserName);
                                                                xhotel.PassWord = Convert.ToString(MgBedBankDetails.Password);
                                                                xhotel.CurrencyCode = Currency;
                                                                xhotel.CurrencyFactor = CurrencyFactorMgBedBank;
                                                                xhotel.XmlCity = dataForSearch.xmlCity;
                                                                xhotel.XmlCountry = dataForSearch.xmlCountry;
                                                                xhotel.XmlNationality = dataForSearch.XmlNationality;
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
                                                                    objrq.Request = (XMLMgBedBankRequestOnly_async(xhotel.URL, xhotel.UserId, xhotel.UserName, xhotel.PassWord, PaxPassport, xhotel.SearchTimeOut, SearchPara, IdsForSearch, xhotel.CurrencyFactor, DebugMode, dataForSearch.xmlCity, xhotel.XmlCountry, xhotel.XmlNationality, Noofrecords, xhotel.CurrencyCode));
                                                                    objrq.Response = ""; RQRS.Add(objrq);
                                                                }
                                                                xhotel.RQRS = RQRS;
                                                                xhotel.ReturnRooms = MgBedBankDetails.ReturnRooms;
                                                                xhotel.TechName = MgBedBankDetails.TechName;
                                                                #endregion
                                                                AllTask.Add(xhotel);
                                                            }
                                                        }
                                                        else
                                                        {
                                                            if (commonSearchList.SearchByHotel && SearchPara.Filters.IsShowRooms == 1)
                                                            {
                                                                double count = Convert.ToDouble(filter.Count);
                                                                int NoOfDiv = Convert.ToInt32(Math.Ceiling(count / Convert.ToDouble(Noofrecords)));
                                                                List<List<XMLID_Search>> lstxml = _commonMethod.GetXMLIDString(NoOfDiv, Noofrecords, count, filter);

                                                                for (int a = 0; a < NoOfDiv; a++)
                                                                {

                                                                    IdsForSearch = Convert.ToString(_commonMethod.GetHotelCodeByCOmma(lstxml[a]));
                                                                    HotelX xhotel = new HotelX();
                                                                    xhotel.Supplier = Supplier;
                                                                    xhotel.DebugeMode = DebugMode;
                                                                    xhotel.SearchTimeOut = Convert.ToInt32(MgBedBankSearchConfig[k].HotelSearchTimeOut);
                                                                    //if (Hit > 0)
                                                                    xhotel.ProcessTimeout = Convert.ToInt32(MgBedBankSearchConfig[k].HotelSearchTimeOut2);

                                                                    xhotel.URL = Convert.ToString(MgBedBankDetails.WebSiteURL) + MGBedbackStaticInfo.Searchs;
                                                                    xhotel.UserId = Convert.ToString(MgBedBankDetails.UserID);
                                                                    xhotel.UserName = Convert.ToString(MgBedBankDetails.UserName);
                                                                    xhotel.PassWord = Convert.ToString(MgBedBankDetails.Password);
                                                                    xhotel.CurrencyCode = Currency;
                                                                    xhotel.CurrencyFactor = CurrencyFactorMgBedBank;
                                                                    xhotel.XmlCity = dataForSearch.xmlCity;
                                                                    xhotel.XmlCountry = dataForSearch.xmlCountry;
                                                                    xhotel.XmlNationality = dataForSearch.XmlNationality;
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
                                                                        objrq.Request = (XMLMgBedBankRequestOnly_async(xhotel.URL, xhotel.UserId, xhotel.UserName, xhotel.PassWord, PaxPassport, xhotel.SearchTimeOut, SearchPara, IdsForSearch, xhotel.CurrencyFactor, DebugMode, dataForSearch.xmlCity, xhotel.XmlCountry, xhotel.XmlNationality, Noofrecords, xhotel.CurrencyCode));
                                                                        objrq.Response = ""; RQRS.Add(objrq);
                                                                    }
                                                                    xhotel.RQRS = RQRS;
                                                                    xhotel.ReturnRooms = MgBedBankDetails.ReturnRooms;
                                                                    xhotel.TechName = MgBedBankDetails.TechName;
                                                                    #endregion
                                                                    AllTask.Add(xhotel);
                                                                }
                                                            }
                                                            if (commonSearchList.SearchByHotel == false && SearchPara.Filters.IsShowRooms == 0)
                                                            {
                                                                HotelX xhotel = new HotelX();
                                                                xhotel.Supplier = Supplier;
                                                                xhotel.DebugeMode = DebugMode;
                                                                xhotel.SearchTimeOut = Convert.ToInt32(MgBedBankSearchConfig[k].HotelSearchTimeOut);
                                                                //if (Hit > 0)
                                                                xhotel.ProcessTimeout = Convert.ToInt32(MgBedBankSearchConfig[k].HotelSearchTimeOut2);

                                                                xhotel.URL = Convert.ToString(MgBedBankDetails.WebSiteURL) + MGBedbackStaticInfo.Searchs;
                                                                xhotel.UserId = Convert.ToString(MgBedBankDetails.UserID);
                                                                xhotel.UserName = Convert.ToString(MgBedBankDetails.UserName);
                                                                xhotel.PassWord = Convert.ToString(MgBedBankDetails.Password);
                                                                xhotel.CurrencyCode = Currency;
                                                                xhotel.CurrencyFactor = CurrencyFactorMgBedBank;
                                                                xhotel.XmlCity = dataForSearch.xmlCity;
                                                                xhotel.XmlCountry = dataForSearch.xmlCountry;
                                                                xhotel.XmlNationality = dataForSearch.XmlNationality;
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
                                                                    objrq.Request = (XMLMgBedBankRequestOnly_async(xhotel.URL, xhotel.UserId, xhotel.UserName, xhotel.PassWord, PaxPassport, xhotel.SearchTimeOut, SearchPara, IdsForSearch, xhotel.CurrencyFactor, DebugMode, dataForSearch.xmlCity, xhotel.XmlCountry, xhotel.XmlNationality, Noofrecords, xhotel.CurrencyCode));
                                                                    objrq.Response = ""; RQRS.Add(objrq);
                                                                }
                                                                xhotel.RQRS = RQRS;
                                                                xhotel.ReturnRooms = MgBedBankDetails.ReturnRooms;
                                                                xhotel.TechName = MgBedBankDetails.TechName;
                                                                #endregion
                                                                AllTask.Add(xhotel);
                                                            }
                                                        }
                                                    }
                                                }
                                                catch (Exception ex)
                                                {
                                                    _exception.Error(ex);
                                                    //throw;
                                                }
                                                #endregion
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
        public string XMLMgBedBankRequestOnly_async(string JCUrl, string JCClientID, string JCUserName, string JCPassword, string PaxPassport, Int32 SearchTimeOut, HotelSearchParameter SearchParam, string JCHotelIds, double CurrencyFactorMgBedBank, bool DebugMode, string CityCode, string CountryCode, string Xmlnationality, int Noofrecords, string MgBedBankCurrency)
        {
            try
            {
                #region PriceOnly RQ
                string JacPriceOnly = Get_MgBedBank_PriceOnly(SearchParam, JCClientID, JCUserName, JCPassword, PaxPassport, CityCode, CountryCode, Xmlnationality, JCHotelIds, Noofrecords, MgBedBankCurrency);
                return JacPriceOnly;

                #endregion
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
                return "";
            }
        }

        public string Get_MgBedBank_PriceOnly(HotelSearchParameter SearchParam, string JCClientID, string MgBedBankUserName, string MgBedBankPassword, string PaxPassport, string MgBedBank_RegionID, string CountryCode, string XmlNationality, string MgBedBankHotelIds, int Noofrecords, string MgBedBankCurrency)
        {
            try
            {
                #region Price Only and Room Only RQ
                string CheckIn = SearchParam.CheckInDate;
                CheckIn = CheckIn.Substring(6, 4) + "-" + CheckIn.Substring(3, 2) + "-" + CheckIn.Substring(0, 2);
                string EN = "EN";
                string CheckOut = SearchParam.CheckOutDate;
                int MaxNoOfHotel = Noofrecords;
                string DetailLevel = "Full";
                bool AvailFlag = true;

                CheckOut = CheckOut.Substring(6, 4) + "-" + CheckOut.Substring(3, 2) + "-" + CheckOut.Substring(0, 2);

                int MinValue = SearchParam.StarStart;
                StringBuilder Jac_SRCH_RQ = new StringBuilder();
                Jac_SRCH_RQ.Append("<SearchHotel>");
                Jac_SRCH_RQ.Append("<Login><AgencyCode>" + JCClientID + "</AgencyCode><Username>" + MgBedBankUserName + "</Username><Password>" + MgBedBankPassword + "</Password></Login>");
                Jac_SRCH_RQ.Append("<SessionID>");
                Jac_SRCH_RQ.Append("</SessionID>");
                Jac_SRCH_RQ.Append("<Nationality>" + XmlNationality + "</Nationality>");                                //Nationality Code=CountryCode in Use MGBedBank
                Jac_SRCH_RQ.Append("<Country>" + CountryCode + "</Country>");
                Jac_SRCH_RQ.Append("<City>" + MgBedBank_RegionID + "</City>");

                string HID = "";

                if (MgBedBankHotelIds.Trim() != string.Empty)
                {
                    string[] HotelIds = Convert.ToString(MgBedBankHotelIds).Split(',');
                    StringBuilder ItemCodeString = new StringBuilder();
                    Jac_SRCH_RQ.Append("<Hotels>");
                    for (int htid = 0; htid < HotelIds.Length; htid++)
                    {
                        Jac_SRCH_RQ.Append("<Code>" + HotelIds[htid] + "</Code>");
                    }
                    Jac_SRCH_RQ.Append("</Hotels>");
                }
                else
                {
                    Jac_SRCH_RQ.Append("<Hotels></Hotels>");
                }
                Jac_SRCH_RQ.Append("<CheckIn>" + CheckIn + "</CheckIn>");
                Jac_SRCH_RQ.Append("<CheckOut>" + CheckOut + "</CheckOut>");

                Jac_SRCH_RQ.Append("<Rooms>");
                for (int ser = 0; ser < SearchParam.Rooms.Count; ser++)
                {
                    string Child1Age = "";
                    string Child2Age = "";
                    int NoOfAdult = 0;
                    string RoomType = "";
                    int NoOfChild = 0;
                    string ExtraBed = "false";
                    NoOfAdult = SearchParam.Rooms[ser].NoofAdults;
                    RoomType = Get_MgBedBank_RoomType(NoOfAdult);

                    NoOfChild = SearchParam.Rooms[ser].NoofChild;

                    if (NoOfChild == 1)
                    {
                        Child1Age = SearchParam.Rooms[ser].Child1Age.ToString();
                    }
                    if (NoOfChild == 2)
                    {
                        Child1Age = SearchParam.Rooms[ser].Child1Age.ToString();
                        Child2Age = SearchParam.Rooms[ser].Child2Age.ToString();
                    }
                    Jac_SRCH_RQ.Append("<Room RoomNo =\"" + (ser + 1) + "\" NoOfAdults =\"" + NoOfAdult + "\" NoOfChild =\"" + NoOfChild + "\" Child1Age =\"" + Child1Age + "\" Child2Age =\"" + Child2Age + "\" ExtraBed =\"" + ExtraBed + "\" />");

                }
                Jac_SRCH_RQ.Append("</Rooms>");
                Jac_SRCH_RQ.Append("<Currency>" + MgBedBankCurrency + "</Currency>");
                Jac_SRCH_RQ.Append("<Language>" + EN + "</Language>");
                Jac_SRCH_RQ.Append("<AvailFlag>" + Convert.ToString(AvailFlag).ToLower() + "</AvailFlag>");
                //Jac_SRCH_RQ.Append("<MaxNoOfHotel>" + MaxNoOfHotel + "</MaxNoOfHotel>");
                Jac_SRCH_RQ.Append("<DetailLevel>" + DetailLevel + "</DetailLevel>");
                Jac_SRCH_RQ.Append("</SearchHotel>");
                return Jac_SRCH_RQ.ToString();
                #endregion
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
            }
            return string.Empty;
        }

        public string Get_MgBedBank_RoomType(int NoOfAdult)
        {
            try
            {
                string RoomType = "";
                switch (NoOfAdult)
                {

                    case 1:
                        RoomType = "Single"; break;

                    case 2:
                        RoomType = "Double"; break;

                    case 3:
                        RoomType = "Triple"; break;

                    case 4:
                        RoomType = "Quad"; break;

                    default: RoomType = ""; break;
                }
                return RoomType;
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
                return "";
            }
        }

        public async Task<List<Options>> Mgbedbank_HotelSearchWithRooms(HotelX Item, CommonSearch commonSearch, string currency, string ResponseData, string xmlcity, HotelSearchParameter SearchParam, string xmlcountry, double currencyfactor, string supplier, string URL, string UserId, string UserName, string PassWord, int SearchTimeOut, string XMLNationality, string XmlHotelCode, string ProccessCutOffTime, Int32 processTimeout, string ResProccessTimerLogActive, string ResProccessTimeValueforSaveLog)
        {
            System.Diagnostics.Stopwatch timer_room_Mgbedbank = new System.Diagnostics.Stopwatch();
            timer_room_Mgbedbank.Start();

            List<Options> options = new List<Options>();

            DataTable DtHotel = _commonMethod.GenerateDataTableNewV2();
            try
            {
                if (ResponseData == string.Empty || ResponseData == "")
                {
                    Item.ResponseStatus = 204;
                    Item.ResponseType = "NoCount";
                    return options;
                }
                DataSet MgBedBankResponse = _commonMethod.GetDataSetFromXMLString(ResponseData);
                if (MgBedBankResponse == null || MgBedBankResponse.Tables["Hotel"] == null)
                {
                    Item.ResponseStatus = 204;
                    Item.ResponseType = "NoCount";
                    return options;
                }

                double FinalMinPrice = 0;

                //for (int h = 0; h <= MgBedBankResponse.Tables["Hotel"].Rows.Count; h++)
                for (int h = 0; h < MgBedBankResponse.Tables["Hotel"].Rows.Count; h++)
                {
                    Item.ResponseType = "Success";
                    //Check Time If High then return NoCount
                    if (ProccessCutOffTime == "1" && timer_room_Mgbedbank.ElapsedMilliseconds > processTimeout)
                    {
                        timer_room_Mgbedbank.Stop();
                        break;
                    }
                    //--end
                    #region "When not Need Room"  
                    // add hotel code from price only and room code blank here
                    if (SearchParam.Filters.IsShowRooms == 0)
                    {
                        #region "Hotel Code Block "

                        int HotelID = Convert.ToInt32(MgBedBankResponse.Tables["Hotel"].Rows[h]["Hotel_Id"]);
                        string ItemCode = Convert.ToString(MgBedBankResponse.Tables["Hotel"].Rows[h]["Code"]);
                        string HotelName = Convert.ToString(MgBedBankResponse.Tables["Hotel"].Rows[h]["Name"]);
                        double Rating1 = Convert.ToDouble(MgBedBankResponse.Tables["Hotel"].Rows[h]["Rating"]);
                        string PromoCode = Convert.ToString(MgBedBankResponse.Tables["RoomDetails"].Rows[h]["PromoCode"]);
                        string PromoName = Convert.ToString(MgBedBankResponse.Tables["RoomDetails"].Rows[h]["PromoName"]);

                        DataRow drXmlHotel = DtHotel.NewRow();
                        string InternalHotelId = Convert.ToString(ItemCode);
                        drXmlHotel["OwnsystemHotelId"] = HotelID + 1;
                        drXmlHotel["XMLHotelCode"] = ItemCode;
                        drXmlHotel["HotelName"] = HotelName.Replace("'", " ").Replace("&", " ");
                        drXmlHotel["SupplierName"] = Item.Supplier;
                        drXmlHotel["OfficeID"] = 0;
                        drXmlHotel["ContractID"] = 0;
                        drXmlHotel["Rating"] = Rating1;
                        drXmlHotel["Reviews"] = 0;
                        drXmlHotel["UniqueNo"] = ItemCode;
                        drXmlHotel["IsShowSupplier"] = false;
                        drXmlHotel["BookingStatus"] = "Available";
                        drXmlHotel["XMLCity"] = xmlcity;
                        drXmlHotel["XMLCountry"] = xmlcountry;
                        drXmlHotel["SupplierCurrency"] = currency;
                        double FinalTotal = 0;
                        DataRow[] drRoomCateg = MgBedBankResponse.Tables["RoomDetails"].Select("Hotel_Id='" + HotelID + "'", "NetPrice ASC");
                        if (drRoomCateg.Length > 0)
                        {
                            FinalTotal = Convert.ToDouble(drRoomCateg[0]["NetPrice"]) * currencyfactor;
                            //for (int g = 0; g < drRoomCateg.Length; g++)
                            //{
                            //    double HotelPrice = Convert.ToDouble(drRoomCateg[g]["NetPrice"]);
                            //    if (g == 0)
                            //        Total = HotelPrice;
                            //    else
                            //    {
                            //        if (HotelPrice < Total)
                            //        {
                            //            Total = HotelPrice;
                            //            FinalTotal = Total * currencyfactor;
                            //        }
                            //    }
                            //}
                            drXmlHotel["FinalSellingPrice"] = FinalTotal;
                            drXmlHotel["SellingPrice"] = FinalTotal;
                            DtHotel.Rows.Add(drXmlHotel);
                        }
                        else
                            continue;

                        if (FinalTotal > 0)
                        {
                            #region "Room code Block"
                            bool willAddOrNot = true;
                            Int64 RaynaID = 0;
                            Int64 GiataId = 0;
                            int Rating = Convert.ToInt32(Rating1);//0;
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
                    #region "When Need Rooms"
                    //add Hotel code blank and rooms code in room block
                    else
                    {
                        string SessionId = Convert.ToString(MgBedBankResponse.Tables["SearchHotel"].Rows[0]["SessionID"]);
                        #region Hotels
                        int HotelID = Convert.ToInt32(MgBedBankResponse.Tables["Hotel"].Rows[h]["Hotel_Id"]);
                        string ItemCode = Convert.ToString(MgBedBankResponse.Tables["Hotel"].Rows[h]["Code"]);
                        string HotelName = Convert.ToString(MgBedBankResponse.Tables["Hotel"].Rows[h]["Name"]);
                        double Rating1 = Convert.ToDouble(MgBedBankResponse.Tables["Hotel"].Rows[h]["Rating"]);
                        //string PromoCode = Convert.ToString(MgBedBankResponse.Tables["RoomDetails"].Rows[h]["PromoCode"]);
                        //string PromoName = Convert.ToString(MgBedBankResponse.Tables["RoomDetails"].Rows[h]["PromoName"]);

                        DataRow drXmlHotel = DtHotel.NewRow();
                        string InternalHotelId = Convert.ToString(ItemCode);
                        drXmlHotel["OwnsystemHotelId"] = HotelID + 1;
                        drXmlHotel["XMLHotelCode"] = ItemCode;
                        drXmlHotel["HotelName"] = HotelName.Replace("'", " ").Replace("&", " ");
                        drXmlHotel["SupplierName"] = Item.Supplier;
                        drXmlHotel["OfficeID"] = 0;
                        drXmlHotel["ContractID"] = 0;
                        drXmlHotel["Rating"] = Rating1;
                        drXmlHotel["Reviews"] = 0;
                        drXmlHotel["UniqueNo"] = ItemCode;
                        drXmlHotel["IsShowSupplier"] = false;
                        drXmlHotel["BookingStatus"] = "Available";
                        drXmlHotel["XMLCity"] = xmlcity;
                        drXmlHotel["XMLCountry"] = xmlcountry;
                        drXmlHotel["SupplierCurrency"] = currency;
                        double FinalTotal = 0;
                        DataRow[] drRoomCateg = MgBedBankResponse.Tables["RoomDetails"].Select("Hotel_Id='" + HotelID + "'", "NetPrice ASC");
                        if (drRoomCateg.Length > 0)
                        {
                            FinalTotal = Convert.ToDouble(drRoomCateg[0]["NetPrice"]) * currencyfactor;
                            //for (int g = 0; g < drRoomCateg.Length; g++)
                            //{
                            //    double HotelPrice = Convert.ToDouble(drRoomCateg[g]["NetPrice"]);
                            //    if (g == 0)
                            //        Total = HotelPrice;
                            //    else
                            //    {
                            //        if (HotelPrice < Total)
                            //        {
                            //            Total = HotelPrice;
                            //            FinalTotal = Total * currencyfactor;
                            //        }
                            //    }
                            //}
                            drXmlHotel["FinalSellingPrice"] = FinalTotal;
                            drXmlHotel["SellingPrice"] = FinalTotal;
                            DtHotel.Rows.Add(drXmlHotel);
                        }
                        else
                            continue;

                        double Pricenew = FinalTotal;

                        #region "GetRooms for this hotel"
                        bool willAddOrNot = true;
                        Int64 RaynaID = 0;
                        Int64 GiataId = 0;
                        int RatingHotel = 0;
                        Int64 OwnsystemHotelId = 0;
                        string Provider = Item.Supplier;
                        double MinPrice = FinalTotal;
                        _markup.GetHotelDataCommon(ref MinPrice, ref willAddOrNot, ref OwnsystemHotelId, ref RaynaID, ref GiataId, ref RatingHotel, ref drXmlHotel, commonSearch.AgentId, commonSearch.TariffID, commonSearch.dsAgentDetails, commonSearch.supplierGiataMaster, SearchParam, commonSearch.dtCountryCityMarkup, commonSearch.B2CMarkup, commonSearch.IsParentId, commonSearch.SubAgentMarkup, commonSearch.SearchByHotel, commonSearch.HotelIDS);
                        if (willAddOrNot)
                        {
                            DataTable dtHotelRoom = _commonMethod.GenerateRoomtypeDataTableNewV2();
                            DataTable dtCancellation = _commonMethod.GenerateCancellationDataTableNewV2();
                            DataTable DtRoomRateBreackUp = _commonMethod.GenerateoomRateBreackUpDataTableNewV2();

                            #region "Room Logic"
                            if (SearchParam.Filters.IsShowRooms == 1)
                            {
                                double AgentPercentage = 0.0;
                                Boolean blnIsShowSupplier = false;
                                double AgentMarkupAmount = 0.0;
                                Boolean isPerce = true;

                                double AgentCompanyPercentage = Convert.ToDouble(SearchParam.AgentCompanyPercentage);
                                bool IsB2C = Convert.ToBoolean(Convert.ToByte(SearchParam.IsB2BOrB2C));

                                _markup.getAgetMarkupSetting(Provider, GiataId.ToString(), commonSearch.dtSuplierMarkup, commonSearch.dtMarkupDetails, ref AgentPercentage, ref AgentMarkupAmount, ref blnIsShowSupplier, ref isPerce, commonSearch.AgentId, RatingHotel, SearchParam.CityID, SearchParam.CountryID.ToString(), SearchParam.CheckInDate);
                                int maxOcupancy = 0;

                                int totalAdult = 0;
                                int totalChild = 0;

                                for (int b = 0; b < SearchParam.Rooms.Count; b++)
                                {
                                    totalAdult = Convert.ToInt32(SearchParam.Rooms[b].NoofAdults) + totalAdult;
                                    totalChild = Convert.ToInt32(SearchParam.Rooms[b].NoofChild) + totalChild;
                                    maxOcupancy = maxOcupancy + SearchParam.Rooms[b].NoofChild + SearchParam.Rooms[b].NoofAdults;
                                }

                                #region Supplier Room Code block


                                string HotelDetail = "", Essential = "";
                                if (MgBedBankResponse.Tables.Contains("Childpolicy"))
                                {
                                    int MinAge = Convert.ToInt32(MgBedBankResponse.Tables["Childpolicy"].Rows[0]["MinAge"]);
                                    int MaxAge = Convert.ToInt32(MgBedBankResponse.Tables["Childpolicy"].Rows[0]["MaxAge"]);
                                    Essential = "Child Policy - If Child age is more then " + MaxAge + " then it will consider as an Adult.";
                                }
                                int Hotel_Id = Convert.ToInt32(MgBedBankResponse.Tables["Hotel"].Rows[h]["Hotel_Id"]);
                                string IsAvail = Convert.ToString(MgBedBankResponse.Tables["RoomDetails"].Rows[h]["AvailFlag"]).ToLower();

                                if (IsAvail.ToLower().Trim() == "true")
                                {
                                    DataRow[] drRoom = MgBedBankResponse.Tables["RoomDetails"].Select("Hotel_Id='" + Hotel_Id + "'");
                                    if (drRoom.Length > 0)
                                    {
                                        for (int j = 0; j < drRoom.Length; j++)
                                        {
                                            //if (j > CacheMaster.RoomLimit_10)
                                            //    break;


                                            if (commonSearch.AgentId == 4440 || commonSearch.AgentId == 4901)
                                            {
                                                if (j > CacheMaster.RoomLimit_20)
                                                    break;
                                            }
                                            else
                                            {
                                                if (j > CacheMaster.RoomLimit_10)
                                                        break;
                                            }

                                            int RoomDetails_Id = Convert.ToInt32(drRoom[j]["RoomDetails_Id"]);
                                            string RoomCode = Convert.ToString(drRoom[j]["Code"]);
                                            string RoomName = Convert.ToString(drRoom[j]["Name"]);
                                            string MealId = Convert.ToString(drRoom[j]["MealPlan"]);
                                            string MealName = Convert.ToString(drRoom[j]["MealPlanName"]);
                                            string CancelType = Convert.ToString(drRoom[j]["CancellationPolicyType"]);
                                            double NetPrice = Convert.ToDouble(drRoom[j]["NetPrice"]);
                                            bool IsPackageRate = Convert.ToBoolean(drRoom[j]["PackageRate"]);
                                            string RoomCodeMain = RoomCode + "$$$" + Guid.NewGuid().ToString().Substring(0, 4);
                                            string RateKeyMain = "";
                                            #region Essential Information
                                            string Message = "";
                                            int Messages_Id = 0;
                                            if (MgBedBankResponse.Tables.Contains("Messages"))
                                            {
                                                DataRow[] drMessage = MgBedBankResponse.Tables["Messages"].Select("RoomDetails_Id='" + RoomDetails_Id + "'");
                                                if (MgBedBankResponse.Tables["Messages"].Columns.Contains("Message"))
                                                {
                                                    for (int me = 0; me < drMessage.Length; me++)
                                                    {
                                                        Message = Convert.ToString(drMessage[me]["Message"]);
                                                    }
                                                }
                                                else if (MgBedBankResponse.Tables["Message"].Columns.Contains("Message_Text"))
                                                {
                                                    for (int es = 0; es < drMessage.Length; es++)
                                                    {
                                                        Messages_Id = Convert.ToInt32(drMessage[es]["Messages_Id"]);
                                                        DataRow[] drmsg = MgBedBankResponse.Tables["Message"].Select("Messages_Id='" + Messages_Id + "'");
                                                        for (int msg = 0; msg < drmsg.Length; msg++)
                                                        {
                                                            Message = Convert.ToString(drmsg[msg]["Message_Text"]);
                                                        }
                                                    }
                                                }
                                            }
                                            string EssentialInfo = "";
                                            if (Message != "")
                                                EssentialInfo = "- " + Message + "</br></br>- " + Essential;
                                            else
                                                EssentialInfo = Essential;
                                            #endregion
                                            int Rooms_Id = Convert.ToInt32(MgBedBankResponse.Tables["Room"].Rows[j]["Room_Id"]);
                                            DataRow[] drRoomTypes = MgBedBankResponse.Tables["Room"].Select("Room_Id='" + Rooms_Id + "'");

                                            if (drRoomTypes.Length > 0)
                                            {
                                                string Type = "";
                                                string offfer = "";
                                                string offferName = "";
                                                for (int room = 0; room < drRoomTypes.Length; room++)
                                                {
                                                    int Room_Id = Convert.ToInt32(drRoomTypes[room]["Room_Id"]);
                                                    string RateKey = Convert.ToString(drRoomTypes[room]["RateKey"]);

                                                    Type = Type + RateKey + "###";


                                                    #region Offers
                                                    if (MgBedBankResponse.Tables.Contains("Promo"))
                                                    {
                                                        DataRow[] drRate = MgBedBankResponse.Tables["RateDetails"].Select("Room_Id='" + Room_Id + "'");
                                                        int RateDetails_Id = 0;
                                                        for (int rate = 0; rate < drRate.Length; rate++)
                                                        {
                                                            RateDetails_Id = Convert.ToInt32(drRate[rate]["RateDetails_Id"]);
                                                        }
                                                        DataRow[] drDiscount = MgBedBankResponse.Tables["DiscountDetails"].Select("RateDetails_Id='" + RateDetails_Id + "'");
                                                        int Discount_Id = 0;
                                                        for (int dis = 0; dis < drDiscount.Length; dis++)
                                                        {
                                                            Discount_Id = Convert.ToInt32(drDiscount[dis]["DiscountDetails_Id"]);
                                                        }
                                                        DataRow[] drRoom1 = MgBedBankResponse.Tables["Promo"].Select("DiscountDetails_Id='" + Discount_Id + "'");
                                                        for (var z = 0; z < drRoom1.Length; z++)
                                                        {
                                                            offfer = Convert.ToString(drRoom1[z]["Code"]);
                                                            offferName = Convert.ToString(drRoom1[z]["Name"]);
                                                        }
                                                    }
                                                    #endregion
                                                }
                                                #region Logic For Room Name
                                                List<Room_W2M> roomlist = new List<Room_W2M>();
                                                for (int dr = 0; dr < drRoomTypes.Length; dr++)
                                                {
                                                    Room_W2M obj = new Room_W2M();
                                                    obj.RoomCount = 1;
                                                    obj.RoomName = RoomName;
                                                    obj.status = true;
                                                    roomlist.Add(obj);
                                                }
                                                for (int p = 0; p < roomlist.Count; p++)
                                                {
                                                    for (int q = (p + 1); q < roomlist.Count; q++)
                                                    {
                                                        if (roomlist[p].RoomName == roomlist[q].RoomName && roomlist[q].status)
                                                        {
                                                            roomlist[p].RoomCount = roomlist[p].RoomCount + roomlist[q].RoomCount;
                                                            roomlist[q].RoomCount = roomlist[p].RoomCount;
                                                            roomlist[q].status = false;
                                                        }
                                                    }
                                                    if (RoomName.IndexOf(roomlist[p].RoomName) < 0 || RoomName == "")
                                                        RoomName = RoomName + roomlist[p].RoomCount + " X " + roomlist[p].RoomName + " ";
                                                }
                                                #endregion
                                                string RoomTypeId = RoomCodeMain + "$$$" + XMLNationality + "$$$" + CancelType + "$$$" + Type + "$$$" + SessionId + "$$$" + MealId + "$$$" + IsAvail + "$$$" + IsPackageRate;
                                                #region RateBreakup
                                                //double FinalPrice = NetPrice / SearchParam.NoofNights;
                                                //DateTime dtCheckInDateForRate = DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null);
                                                //#region Vat
                                                //Double VatOnRateBrackUp = 0;
                                                //double RoomPrice = Common.ProcessHotelRateBreakupPrice_WithMarkup(1, SearchParam.CityID, SearchParam.CountryId, FinalPrice, B2CMarkup, IsParentId, isPerce, AgentPercentage, AgentMarkupAmount, SubAgentMarkup, ref VatOnRateBrackUp);
                                                //#endregion
                                                //for (int night = 0; night < SearchParam.NoofNights; night++)
                                                //{
                                                //    string RateWeekColumn = Convert.ToString(dtCheckInDateForRate.Date.AddDays(night).DayOfWeek).Substring(0, 3);
                                                //    DataRow drMgBedBankRateBreakup = DtMgBedBankRateBreackUp.NewRow();
                                                //    drMgBedBankRateBreakup["RoomNo"] = RoomNo;
                                                //    drMgBedBankRateBreakup["SupplierName"] = Provider;
                                                //    drMgBedBankRateBreakup["OwnsystemHotelId"] = ownsystemhotelId;
                                                //    drMgBedBankRateBreakup["ContractID"] = 0;
                                                //    drMgBedBankRateBreakup["RoomTypeName"] = RoomName;
                                                //    drMgBedBankRateBreakup["RoomTypeId"] = RoomTypeId;
                                                //    drMgBedBankRateBreakup["MealId"] = MealId;
                                                //    drMgBedBankRateBreakup["Meal"] = MealName;
                                                //    drMgBedBankRateBreakup["BookingStatus"] = "Available";
                                                //    drMgBedBankRateBreakup["DateOfWeek"] = RateWeekColumn;
                                                //    drMgBedBankRateBreakup["Date"] = dtCheckInDateForRate.Date.AddDays(night).Date;
                                                //    drMgBedBankRateBreakup["Rate"] = RoomPrice;
                                                //    drMgBedBankRateBreakup["RateType"] = SearchParam.Rooms.Count == 1 ? "A Rate" : ((SearchParam.Rooms.Count) + ". " + RoomName); //"A Rate";
                                                //    drMgBedBankRateBreakup["SupplierCurrency"] = Currency;
                                                //    drMgBedBankRateBreakup["BuyingPrice"] = RoomPrice;
                                                //    DtMgBedBankRateBreackUp.Rows.Add(drMgBedBankRateBreakup);
                                                //    DtMgBedBankRateBreackUp.AcceptChanges();
                                                //}
                                                #endregion

                                                #region Room
                                                DataRow drMgBedBankRoom = dtHotelRoom.NewRow();
                                                drMgBedBankRoom["OwnSystemHotelId"] = OwnsystemHotelId;
                                                drMgBedBankRoom["RoomTypeName"] = RoomName;
                                                drMgBedBankRoom["RoomTypeCode"] = RoomCode;
                                                drMgBedBankRoom["MealId"] = MealId;
                                                drMgBedBankRoom["MealName"] = MealName;
                                                drMgBedBankRoom["ContractID"] = 0;
                                                drMgBedBankRoom["BoardType"] = MealName;
                                                drMgBedBankRoom["Board"] = MealId;
                                                drMgBedBankRoom["RoomTypeId"] = RoomTypeId;
                                                drMgBedBankRoom["AdultsOccupancy"] = totalAdult;
                                                drMgBedBankRoom["ChildOccupancy"] = totalChild;
                                                drMgBedBankRoom["MaxExtraBed"] = "0";
                                                drMgBedBankRoom["RoomNo"] = 1;
                                                drMgBedBankRoom["RoomSequenceNo"] = RoomName;
                                                drMgBedBankRoom["Type"] = Type;                          //Rate Key                                        
                                                drMgBedBankRoom["OfficeId"] = 0;
                                                drMgBedBankRoom["BookingStatus"] = "Available";
                                                drMgBedBankRoom["UniqueNo"] = Guid.NewGuid().ToString();
                                                drMgBedBankRoom["OfferDescription"] = offferName;
                                                drMgBedBankRoom["OfferCode"] = offfer;
                                                drMgBedBankRoom["EssentialInformation"] = EssentialInfo;
                                                double ItemPrice = Convert.ToDouble(NetPrice);
                                                _markup.ProcessHotelRoom_WithMarkup(currencyfactor, currency, false, true, SearchParam.NoofNights * SearchParam.Rooms.Count, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), SearchParam.CheckInDate, SearchParam.CheckOutDate, ref ItemPrice, ref drMgBedBankRoom, isPerce, AgentPercentage, AgentMarkupAmount, commonSearch.SubAgentMarkup, commonSearch.B2CMarkup, commonSearch.IsParentId);
                                                drMgBedBankRoom["SupplierName"] = Provider;
                                                dtHotelRoom.Rows.Add(drMgBedBankRoom);
                                                #endregion

                                                #region Cancellation Policy
                                                if (MgBedBankResponse.Tables.Contains("CancellationPolicies"))
                                                {
                                                    int CutOffDays = -5;
                                                    CutOffDays = _commonMethod.GetCutOffDays(commonSearch.dtSuplierMarkup, Provider);
                                                    bool IsRef = false;
                                                    string uniqid = Guid.NewGuid().ToString();
                                                    double MainAgentCharge = NetPrice;
                                                    double VatOnPolicy = 0;
                                                    #region Vat
                                                    double CancelCharge = _markup.ProcessHotelCancelationPrice_WithMarkup(ref MainAgentCharge, SearchParam.NoofNights, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), NetPrice, isPerce, commonSearch.B2CMarkup, AgentPercentage, AgentMarkupAmount, commonSearch.IsParentId, commonSearch.SubAgentMarkup, ref VatOnPolicy);
                                                    #endregion
                                                    if (CancelType == "Flexi")
                                                    {
                                                        int CancellationPolicies_Id = 0;
                                                        DataRow[] drcan = MgBedBankResponse.Tables["CancellationPolicies"].Select("RoomDetails_Id='" + RoomDetails_Id + "'");
                                                        for (int policy = 0; policy < drcan.Length; policy++)
                                                        {
                                                            CancellationPolicies_Id = Convert.ToInt32(drcan[policy]["CancellationPolicies_Id"]);
                                                            DataRow[] drpolicy = MgBedBankResponse.Tables["policy"].Select("CancellationPolicies_Id='" + CancellationPolicies_Id + "'");
                                                            for (int can = 0; can < drpolicy.Length; can++)
                                                            {
                                                                if (can == 0)
                                                                {
                                                                    DateTime FromDate = Convert.ToDateTime(drpolicy[can]["FromDate"]);
                                                                    DateTime ToDate = Convert.ToDateTime(drpolicy[can]["ToDate"]);

                                                                    DateTime NowDate = DateTime.Now.Date;
                                                                    if (FromDate > NowDate)
                                                                    {
                                                                        if (FromDate.AddDays(CutOffDays).Date > NowDate)
                                                                            FromDate = FromDate.AddDays(CutOffDays).Date;
                                                                        else
                                                                            FromDate = DateTime.Now.Date;

                                                                        if (FromDate > NowDate && IsRef == false)
                                                                        {
                                                                            double zero = 0;
                                                                            dtCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref zero, dtCancellation.NewRow(), currencyfactor, currency, Provider, Convert.ToString(OwnsystemHotelId), "1",
                                                                                                         Convert.ToString(RoomTypeId), RoomName, MealId, DateTime.Now.Date, FromDate.AddDays(-1).AddHours(23).AddMinutes(59).AddSeconds(59), 0, 0, MealId, VatOnPolicy, uniqid, "NO", "", EssentialInfo, "", ""));
                                                                            dtCancellation.AcceptChanges();
                                                                            IsRef = true;
                                                                        }
                                                                        dtCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref MainAgentCharge, dtCancellation.NewRow(), currencyfactor, currency, Provider, Convert.ToString(OwnsystemHotelId), "1",
                                                               Convert.ToString(RoomTypeId), RoomName, MealId, Convert.ToDateTime(FromDate), DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null), CancelCharge, CancelCharge, MealId, VatOnPolicy, uniqid, "NO", "", EssentialInfo, "", ""));
                                                                        dtCancellation.AcceptChanges();
                                                                    }
                                                                    else
                                                                    {
                                                                        dtCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref MainAgentCharge, dtCancellation.NewRow(), currencyfactor, currency, Provider, Convert.ToString(OwnsystemHotelId), "1",
                                                               Convert.ToString(RoomTypeId), RoomName, MealId, Convert.ToDateTime(DateTime.Now.Date), DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null), CancelCharge, CancelCharge, MealId, VatOnPolicy, uniqid, "NO", "", EssentialInfo, "", ""));
                                                                        dtCancellation.AcceptChanges();
                                                                    }
                                                                }
                                                                break;
                                                            }
                                                        }
                                                    }
                                                    else
                                                    {
                                                        dtCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref MainAgentCharge, dtCancellation.NewRow(), currencyfactor, currency, Provider, Convert.ToString(OwnsystemHotelId), "1",
                                                             Convert.ToString(RoomTypeId), RoomName, MealId, Convert.ToDateTime(DateTime.Now.Date), DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null), CancelCharge, CancelCharge, MealId, VatOnPolicy, uniqid, "NO", "", EssentialInfo, "", ""));
                                                        dtCancellation.AcceptChanges();
                                                    }
                                                }
                                                #endregion
                                            }
                                        }
                                    }
                                }
                                #endregion

                                Options ops = (await _supplierRes.GetCommonRoomForSuppliers(GiataId, commonSearch.tokenData, commonSearch.advancedOptions, Pricenew, drXmlHotel, dtHotelRoom, dtCancellation, DtRoomRateBreackUp, SearchParam));
                                if (ops != null)
                                    options.Add(ops);
                            }
                            #endregion
                            #endregion

                        }
                        #endregion
                    }
                    #endregion
                }
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
                    if (SearchParam != null && ResProccessTimerLogActive == "1" && timer_room_Mgbedbank.ElapsedMilliseconds > Convert.ToInt32(ResProccessTimeValueforSaveLog))
                    {
                        timer_room_Mgbedbank.Stop();
                        SearchParam.SupplierTime += ",|Finish:MgBedbank Process Response  " + "|Time :" + timer_room_Mgbedbank.Elapsed.ToString();
                        _= Task.Run(() => _commonMethod.FireAway_SQL_ResPorcessTimeLog("", timer_room_Mgbedbank.Elapsed.ToString(), "MgBedbank", SearchParam.SearchKey, 1, SearchParam.AgentID));
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


        public async Task<PriceCheckCMDto> Get_MgBedBank_PriceCheckRQRS(DataSet dsCartDetails, DataRow[] MgBedBankDetails, string PaxPassPort, int i, int j, DataTable dtCheckPriceReqStructure, string UniqueNo, string AgentUniqueNo, double FinalPrice, bool isForBooking, double CompanyBuyingPrice, string HasKey_IntCode, string IsPrcCheck_Book, string Supplier, string Searchkey = "", int AgentId = 0,TokenData tokenData=null)
        {
            PriceCheckCMDto priceCheckCMDto = new PriceCheckCMDto();

            string responsetype = "Success";
            string time = "";
            string SupplierName = Supplier;
            string Error = "";
            string ErrorResponse = "";
            string xslRequest = "";
            string ResponseData = "";
            try
            {
                #region Price Check RQ and Declaration

                string MgBedBankCurrency = Convert.ToString(MgBedBankDetails[0]["CurrencyShortName"]);
                double CurrencyFactorMgBedBank = 1;
                if (MgBedBankCurrency.ToLower() != tokenData.SiteCurrency.ToLower())
                {
                    //DataTable dtInrCurrency = Common.GetCountryCurrency(XMLHotelBookingCommon.MgBedBankCurrency);
                    //CurrencyFactorMgBedBank = Convert.ToDouble(dtInrCurrency.Rows[0]["AmtToExch"]);

                    CurrencyFactorMgBedBank = _commonMethod.GetROEByCurrency(MgBedBankCurrency, tokenData.dtROE);
                }
                DataSet dsTemp = new DataSet();
                string MgBedBankUrl = Convert.ToString(MgBedBankDetails[0]["WebSiteURL"]) + MGBedbackStaticInfo.RecheckRate;
                string UserId = Convert.ToString(MgBedBankDetails[0]["UserId"]);
                string UserName = Convert.ToString(MgBedBankDetails[0]["UserName"]);
                string Password = Convert.ToString(MgBedBankDetails[0]["Password"]);
                string HotelId = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["XMLHotelId"]);
                string XmlCity = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["XMLCityId"]);
                string XmlCountry = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["XMLCountryId"]);

                int UserCartId = Convert.ToInt32(dsCartDetails.Tables[0].DefaultView[i]["UserCartID"]);
                int UserCartDetailId = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[j]["UserCartDetailId"]);
                string RoomTypeId = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomTypeID"]);
                string CheckIn = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["CheckInDate"]);//"20180327"
                CheckIn = CheckIn.Substring(0, 4) + "-" + CheckIn.Substring(4, 2) + "-" + CheckIn.Substring(6, 2);
                string CheckOut = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["CheckOutDate"]);
                CheckOut = CheckOut.Substring(0, 4) + "-" + CheckOut.Substring(4, 2) + "-" + CheckOut.Substring(6, 2);
                string MealId = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["MealPlanCode"]);

                string cancelType = "";
                string ReqHasKey = "";
                string RoomId = "";
                string SessionId = "";
                string BoardID = "";
                string Nationality = "";
                string IsAvail = "";
                string UniqNo = "";
                bool IsPackageRate = false;

                if (IsPrcCheck_Book.ToLower() == "p")
                {

                    RoomId = Get_MgBedBank_RoomTypeID(RoomTypeId, ref UniqNo, ref Nationality, ref cancelType, ref ReqHasKey, ref SessionId, ref BoardID, ref IsAvail, ref IsPackageRate);
                    string RoomName = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomType"]);

                    string[] spitid = RoomName.Split(new String[] { "[" }, StringSplitOptions.None);
                    string SpitIdRoomName = spitid[0].ToString();

                    bool IsRoomupdate = await _commonMethod.GetRoomNameUpdate(SpitIdRoomName, UserCartId);



                }
                else
                {
                    DataRow[] CartResponse = dsCartDetails.Tables[1].Select("UserCartID='" + UserCartId + "'");
                    string HasKey1 = Convert.ToString(CartResponse[0]["Eachotoken"]);
                    string[] spitid = HasKey1.Split(new String[] { "$$$" }, StringSplitOptions.None);
                    ReqHasKey = spitid[0].ToString();
                }

                if (ReqHasKey != "")
                {
                    xslRequest = Get_MgBedBank_Policy_PriceCheckRQ(dsCartDetails, UserCartId, UserId, UserName, Password, PaxPassPort, HotelId, RoomTypeId, CheckIn, CheckOut, ReqHasKey, XmlCity, XmlCountry, RoomId, cancelType, BoardID, SessionId, Nationality, IsAvail, IsPackageRate, MgBedBankCurrency);
                    _commonMethod.ThirdPartyBookingHistory(Convert.ToString(UniqueNo), Convert.ToString(AgentUniqueNo), xslRequest, "", "Before get price " + Supplier + " request hit " + j + "is for Price Check" + isForBooking, Supplier, "response confirm", "-", "WithinEarth");
                    #endregion

                    if (xslRequest != "")
                    {
                        if (!isForBooking)
                        {
                            dtCheckPriceReqStructure.Rows.Add(_commonMethod.AddCheckPriceReq(dtCheckPriceReqStructure.NewRow(), j + 1, UserCartId, UserCartDetailId, RoomTypeId, Supplier, xslRequest));
                        }
                        #region Price Check 
                        System.Diagnostics.Stopwatch timerroomprocess = new System.Diagnostics.Stopwatch();
                        timerroomprocess.Start();

                        ResponseDto responseDto = await MGGetSupplierParallel(xslRequest, MgBedBankUrl, ErrorResponse, 180000, "MgBedBank");
                        ResponseData = responseDto.Response;
                        ErrorResponse = responseDto.Error;

                        timerroomprocess.Stop();
                        time = timerroomprocess.Elapsed.ToString();

                        if (ErrorResponse != string.Empty)
                        {
                            _commonMethod.ThirdPartyBookingHistory(Convert.ToString(UniqueNo), Convert.ToString(AgentUniqueNo), xslRequest, ErrorResponse, "After get price " + Supplier + " request hit " + j + "is for Price Check" + isForBooking, Supplier, "response confirm", "-", "WithinEarth");
                        }
                        else
                        {
                            _commonMethod.ThirdPartyBookingHistory(Convert.ToString(UniqueNo), Convert.ToString(AgentUniqueNo), xslRequest, ResponseData, "After get price " + Supplier + " request hit " + j + "is for Price Check" + isForBooking, Supplier, "response confirm", "-", "WithinEarth");
                        }
                        if (ResponseData == string.Empty || ResponseData.Contains("Error"))
                        {
                            try
                            {
                                responsetype = "GeneralError";
                                string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, xslRequest, ResponseData, ErrorResponse);
                            }
                            catch (Exception e)
                            {
                                _ = _exception.Error(e, Searchkey);
                            }

                            priceCheckCMDto.Flag = false;
                            return priceCheckCMDto;
                        }
                        DataSet MgBedBank_Response = _commonMethod.GetDataSetFromXMLString(ResponseData);
                        #endregion

                        #region Price Check Logic

                        if (!MgBedBank_Response.Tables.Contains("Hotel"))
                        {
                            try
                            {
                                responsetype = "GeneralError";
                                string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, xslRequest, ResponseData, ErrorResponse);
                            }
                            catch (Exception e)
                            {
                                _ = _exception.Error(e, Searchkey);
                            }
                            priceCheckCMDto.Flag = false;
                            return priceCheckCMDto;
                        }
                        else
                        {
                            int Room_Id = 0;
                            DataRow[] SearchResponse = MgBedBank_Response.Tables["Room"].Select("Room_Id='" + Room_Id + "'");
                            string HasKey = Convert.ToString(SearchResponse[0]["RateKey"]);

                            string IntCode = Convert.ToString(MgBedBank_Response.Tables["RoomDetails"].DefaultView[0]["Code"]);

                            HasKey_IntCode = HasKey + "$$$" + IntCode;
                            HasKey_IntCode = ReqHasKey + "$$$" + IntCode;

                            bool IsSuccess = await _commonMethod.GetHasKeyUpdateStaus(HasKey_IntCode, UserCartId);
                            string PAXPASS = "";
                            string RoomCodeID = Get_MgBedBank_RoomTypeID(RoomTypeId, ref UniqNo, ref PAXPASS, ref cancelType, ref ReqHasKey, ref SessionId, ref BoardID, ref IsAvail, ref IsPackageRate);

                            DataRow[] drRoomCateg = MgBedBank_Response.Tables["RoomDetails"].Select("Code='" + RoomCodeID + "'and MealPlan='" + MealId + "'");

                            if (drRoomCateg.Length > 0)
                            {
                                FinalPrice = Convert.ToDouble(drRoomCateg[0]["NetPrice"]);
                                FinalPrice = FinalPrice * CurrencyFactorMgBedBank;
                                FinalPrice = Convert.ToDouble(Math.Round(FinalPrice, 2));

                                if (Convert.ToDouble(Math.Round(FinalPrice.TruncateDecimalPlaces(0))) > Math.Round(CompanyBuyingPrice.TruncateDecimalPlaces(0)))
                                {
                                    try
                                    {
                                        responsetype = "GeneralError";
                                        string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, xslRequest, ResponseData, ErrorResponse);
                                    }
                                    catch (Exception e)
                                    {
                                        _ = _exception.Error(e, Searchkey);
                                    }

                                    priceCheckCMDto.ItemPrice = FinalPrice;
                                    priceCheckCMDto.Flag = true;
                                    return priceCheckCMDto;
                                }
                            }
                            else
                            {
                                try
                                {
                                    responsetype = "GeneralError";
                                    string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, xslRequest, ResponseData, ErrorResponse);
                                }
                                catch (Exception e)
                                {
                                    _ = _exception.Error(e, Searchkey);
                                }

                                priceCheckCMDto.ItemPrice = FinalPrice;
                                priceCheckCMDto.Flag = false;
                                return priceCheckCMDto;
                            }

                        }
                        #endregion
                    }
                    else
                    {
                        try
                        {
                            responsetype = "GeneralError";
                            string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, xslRequest, ResponseData, ErrorResponse);
                        }
                        catch (Exception e)
                        {
                            _ = _exception.Error(e, Searchkey);
                        }

                        priceCheckCMDto.ItemPrice = FinalPrice;
                        priceCheckCMDto.Flag = false;
                        return priceCheckCMDto;
                    }
                }
            }
            catch (Exception e)
            {
                try
                {
                    responsetype = "GeneralError";
                    string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, xslRequest, ResponseData, ErrorResponse);
                }
                catch (Exception ex)
                {
                    _ = _exception.Error(ex, Searchkey);
                }
                _ = _exception.Error(e);
                FinalPrice = 0;

                priceCheckCMDto.ItemPrice = FinalPrice;
                priceCheckCMDto.Flag = false;
                return priceCheckCMDto;

            }
            try
            {
                responsetype = "Success";
                string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, xslRequest, ResponseData, ErrorResponse);
            }
            catch (Exception e)
            {
                _ = _exception.Error(e, Searchkey);
            }

            priceCheckCMDto.ItemPrice = FinalPrice;
            priceCheckCMDto.Flag = false;
            return priceCheckCMDto;
        }

        public string Get_MgBedBank_RoomTypeID(string RoomTypeId, ref string UniqNo, ref string Nationality, ref string CancelType, ref string RateKey, ref string SessionId, ref string MealId, ref string IsAvail, ref bool IsPackageRate)
        {
            try
            {
                string[] spitid = RoomTypeId.Split(new String[] { "$$$" }, StringSplitOptions.None);
                string MgBedBank_RoomId = spitid[0].ToString();
                UniqNo = spitid[1].ToString();
                Nationality = spitid[2].ToString();
                CancelType = spitid[3].ToString();
                RateKey = spitid[4].ToString();
                SessionId = spitid[5].ToString();
                MealId = spitid[6].ToString();
                IsAvail = spitid[7].ToString();
                IsPackageRate = Convert.ToBoolean(spitid[8]);
                return MgBedBank_RoomId;
            }
            catch (Exception ex)
            {
                _ = _exception.Error(ex);
                return "";
            }
        }

        public string Get_MgBedBank_Policy_PriceCheckRQ(DataSet dsCartDetails, int UserCartID, string UserId, string UserName, string Password, string PaxPassport, string HotelId, string RoomTypeId, string CheckIn, string CheckOut, string Type, string XMLCityId, string XMLCountryId, string RoomId, string CancallationPolicytype, string BoardID, string SessionId, string MainNationality, string IsAvail, bool IsPackageRate,string MgBedBankCurrency)
        {
            try
            {
                #region Price Only and Room Only RQ _ New

                string EN = "EN";
                string AvailFlag = IsAvail;
                string DetailLevel = "Full";
                //bool PackageRate = false;
                //string Rkey = "", HasKey = "", Key = "", RateKey1 = "", RateKeyMain = "";
                //string RateKey = Get_MgBedBank_RateKey(Type, ref Rkey, ref HasKey, ref Key, ref RateKey1);
                StringBuilder Jac_SRCH_RQ = new StringBuilder();
                Jac_SRCH_RQ.Append("<RecheckHotel>");
                Jac_SRCH_RQ.Append("<Login><AgencyCode>" + UserId + "</AgencyCode><Username>" + UserName + "</Username><Password>" + Password + "</Password></Login>");
                Jac_SRCH_RQ.Append("<SessionID>" + SessionId + "</SessionID>");
                Jac_SRCH_RQ.Append("<Nationality>" + MainNationality + "</Nationality>");                                //Nationality Code=CountryCode in Use MGBedBank
                Jac_SRCH_RQ.Append("<Country>" + XMLCountryId + "</Country>");
                Jac_SRCH_RQ.Append("<City>" + XMLCityId + "</City>");
                Jac_SRCH_RQ.Append("<CheckIn>" + CheckIn + "</CheckIn>");
                Jac_SRCH_RQ.Append("<CheckOut>" + CheckOut + "</CheckOut>");
                Jac_SRCH_RQ.Append("<HotelCode>" + HotelId + "</HotelCode>");
                Jac_SRCH_RQ.Append("<RoomDetails Code=\"" + RoomId + "\" MealPlan=\"" + BoardID + "\" CancellationPolicyType=\"" + CancallationPolicytype + "\" PackageRate=\"" + Convert.ToString(IsPackageRate).ToLower() + "\" />");
                Jac_SRCH_RQ.Append("<Rooms>");
                DataRow[] drUserCartCount = dsCartDetails.Tables[1].Select("UserCartId='" + UserCartID + "'");
                int CartCount = Convert.ToInt32(drUserCartCount.Length);

                for (int ser = 0; ser < CartCount; ser++)
                {
                    string Child1Age = "";
                    string Child2Age = "";
                    //string RoomType = "0";
                    string ExtraBed = "false";
                    string NoOfAdult = "";
                    string NoOfChild = "";
                    //string RoomCode1 = Convert.ToString(dsCartDetails.Tables[1].Rows[ser]["RoomNumber"]);
                    NoOfAdult = Convert.ToString(drUserCartCount[ser]["NoOfAdults"]);

                    //RoomType = Common.Get_MgBedBank_RoomType(Convert.ToInt32(NoOfAdult));
                    NoOfChild = Convert.ToString(drUserCartCount[ser]["NoofChild"]);
                    if (NoOfChild == "1")
                    {
                        Child1Age = Convert.ToString(drUserCartCount[ser]["ChildAge1"]).ToString();
                    }
                    if (NoOfChild == "2")
                    {
                        Child1Age = Convert.ToString(drUserCartCount[ser]["ChildAge1"]).ToString();
                        Child2Age = Convert.ToString(drUserCartCount[ser]["ChildAge2"]).ToString();
                    }
                    string[] TypeManage = Type.Split(new String[] { "###" }, StringSplitOptions.None);

                    string TypeManageString = TypeManage[ser].ToString();

                    Jac_SRCH_RQ.Append("<Room RoomNo =\"" + (ser + 1) + "\" RateKey =\"" + TypeManageString + "\" NoOfAdults =\"" + NoOfAdult + "\" NoOfChild =\"" + NoOfChild + "\" Child1Age =\"" + Child1Age + "\" Child2Age =\"" + Child2Age + "\" ExtraBed =\"" + ExtraBed + "\" />");
                }
                Jac_SRCH_RQ.Append("</Rooms>");
                Jac_SRCH_RQ.Append("<Currency>" + MgBedBankCurrency + "</Currency>");
                Jac_SRCH_RQ.Append("<Language>" + EN + "</Language>");
                Jac_SRCH_RQ.Append("<AvailFlag>" + AvailFlag + "</AvailFlag>");
                Jac_SRCH_RQ.Append("<DetailLevel>" + DetailLevel + "</DetailLevel>");
                Jac_SRCH_RQ.Append("</RecheckHotel>");
                return Jac_SRCH_RQ.ToString();
                #endregion
            }
            catch (Exception ex)
            {
                _ = _exception.Error(ex);
            }
            return string.Empty;
        }

        public async Task<ResponseDto> MGGetSupplierParallel(string strxmlRQRS, string strHost, string ErrorResponse, Int32 TimeOut, string Provider = "", string SearchKey = "")
        {
            System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
            timer.Start();

            ResponseDto responseDto = new ResponseDto();
            string response = null;

            string Error = "";
            string reqId = Convert.ToString(DateTime.Now.Ticks);
            try
            {
                string Key = "";
                HttpWebRequest Request;
                Request = (HttpWebRequest)HttpWebRequest.Create(strHost);
                Request.Timeout = TimeOut;
                Request.ReadWriteTimeout = TimeOut;
                if (Provider.ToLower() != "fitruums" && Provider.ToLower() != "youtravel")
                {
                    Request.Proxy = null;
                    Request.Method = "POST";
                    Request.UserAgent = "Mozilla/4.0 (compatible; MSIE 6.0; MS Web Services Client Protocol 1.1.4322.2300)";
                    Request.ContentType = "text/xml;charset=utf-8;";
                    Request.Accept = "text/xml";
                    Request.KeepAlive = false;
                }
                string ProviderNew = Provider.ToLower();
                switch (ProviderNew)
                {
                    case "mgbedbank":
                        Request.ContentType = "application/xml;charset=\"utf-8\"";
                        Request.Accept = "application/xml";
                        Request.Headers.Add("Accept-Encoding", "gzip");
                        break;
                    default:

                        Request.Headers.Add("Accept-Encoding", "gzip");

                        break;
                }
                using (Stream requestStream = Request.GetRequestStream())
                {
                    byte[] postBytes = Encoding.ASCII.GetBytes(strxmlRQRS);
                    //Writes a sequence of bytes to the current stream                     
                    requestStream.Write(postBytes, 0, postBytes.Length);
                    requestStream.Close();//Close stream
                }
                HttpWebResponse response1 = (HttpWebResponse)await Request.GetResponseAsync();
                Stream responseStream = response1.GetResponseStream();

                if (response1.ContentEncoding.ToLower().Contains("gzip"))
                    responseStream = new System.IO.Compression.GZipStream(responseStream, System.IO.Compression.CompressionMode.Decompress);
                else if (response1.ContentEncoding.ToLower().Contains("deflate"))
                    responseStream = new System.IO.Compression.DeflateStream(responseStream, System.IO.Compression.CompressionMode.Decompress);
                StreamReader streamReader = new StreamReader(responseStream);


                response = await streamReader.ReadToEndAsync();
                string strOutput = response.ToString();
                responseStream.Close();
                streamReader.Close();
                response1.Close();
            }
            catch (Exception ee)
            {
                _ = _exception.Error(ee, SearchKey);
                ErrorResponse = ee.ToString();
                Error = ee.Message;

                responseDto.Error = ErrorResponse;
                responseDto.Response = null;

                return responseDto;
            }

            responseDto.Error = Error;
            responseDto.Response = response;
            return responseDto;

        }

        

        public string Get_MgBedBank_BookingRequest(DataSet dsCartDetails, DataTable dtPassengers, DataRow[] MgBedBankDetails, string ExternalReference, bool IsPriceUpdate, int i, int j, string HasKey_IntCode, string PaxPassport, string cancallationpolicytype, string RoomTypeID, string RoomId, string SessionId, string BoardID, string Nationality, string IsAvail, string UniqNo)
        {
            try
            {
                #region Booking RQ
                string MgBedBankCurrency = Convert.ToString(MgBedBankDetails[0]["CurrencyShortName"]);
                string UserID = Convert.ToString(MgBedBankDetails[0]["UserId"]);
                string UserName = Convert.ToString(MgBedBankDetails[0]["UserName"]);
                string Password = Convert.ToString(MgBedBankDetails[0]["Password"]);
                string CheckIn = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["CheckInDate"]);
                CheckIn = CheckIn.Substring(0, 4) + "-" + CheckIn.Substring(4, 2) + "-" + CheckIn.Substring(6, 2);
                string CheckOut = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["CheckOutDate"]);
                CheckOut = CheckOut.Substring(0, 4) + "-" + CheckOut.Substring(4, 2) + "-" + CheckOut.Substring(6, 2);
                string MealId = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["MealPlanCode"]);
                //string CancellationPolicyType = cancallationpolicytype;
                //string PackageRate = "false";
                string OnHold = "false";
                //string AvailFlag = IsAvail;
                string EN = "EN";
                string DetailLevel = "Full";
                string RoomType = "";
                string RoomTypeId = RoomTypeID;
                bool IsPackageRate = false;
                //string Rkey = "", HasKey = "", Key = "", Ratekey1 = "", RateKeyMain = "";
                if (IsPriceUpdate)
                    RoomType = "";
                else
                {
                    RoomType = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomTypeId"]);
                    RoomTypeId = Get_MgBedBank_RoomTypeID(RoomType, ref UniqNo, ref PaxPassport, ref cancallationpolicytype, ref HasKey_IntCode, ref SessionId, ref MealId, ref IsAvail, ref IsPackageRate);
                }
                string InternalCode = "";
                string HotelId = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["XMLHotelId"]);
                int UserCartId = Convert.ToInt32(dsCartDetails.Tables[0].Rows[i]["UserCartID"]);
                //double Price = Convert.ToDouble(dsCartDetails.Tables[1].DefaultView[j]["CompanyBuyingPrice"]);
                //string RoomName = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["purchaseToken"]);
                //string RateKey = Get_MgBedBank_RateKey(HasKey_IntCode, ref Rkey, ref HasKey, ref Key, ref Ratekey1);
                StringBuilder BookRQ = new StringBuilder();
                BookRQ.Append("<BookHotel>");
                BookRQ.Append("<Login><AgencyCode>" + UserID + "</AgencyCode><Username>" + UserName + "</Username><Password>" + Password + "</Password></Login>");
                BookRQ.Append("<SessionID>" + SessionId + "</SessionID>");
                InternalCode = ExternalReference;

                BookRQ.Append("<AgencyBookingID>" + InternalCode + "</AgencyBookingID>");
                BookRQ.Append("<Nationality>" + PaxPassport + "</Nationality>");
                BookRQ.Append("<CheckIn>" + CheckIn + "</CheckIn>");
                BookRQ.Append("<CheckOut>" + CheckOut + "</CheckOut>");
                BookRQ.Append("<HotelCode>" + HotelId + "</HotelCode>");
                BookRQ.Append("<RoomDetails Code =\"" + RoomTypeId + "\" MealPlan =\"" + MealId + "\" CancellationPolicyType =\"" + cancallationpolicytype + "\" PackageRate =\"" + IsPackageRate.ToString().ToLower() + "\" />");
                BookRQ.Append("<Rooms>");
                DataRow[] drCartPaasenger = dsCartDetails.Tables[1].Select("UserCartId='" + UserCartId + "'");

                for (int x = 0; x < drCartPaasenger.Length; x++)
                {
                    int NoOfAdult = 0;
                    int NoOfChild = 0;
                    int Childage1 = 0;
                    int Childage2 = 0;
                    string extrabed = "false";
                    NoOfAdult = Convert.ToInt32(drCartPaasenger[x]["NoOfAdults"]);
                    NoOfChild = Convert.ToInt32(drCartPaasenger[x]["NoOfChild"]);
                    Childage1 = Convert.ToInt32(drCartPaasenger[x]["ChildAge1"]);
                    Childage2 = Convert.ToInt32(drCartPaasenger[x]["ChildAge2"]);
                    string RoomNo = Convert.ToString(drCartPaasenger[x]["RoomNumber"]);
                    string RoomTypeName = "";
                    RoomTypeName = Get_MgBedBank_RoomType(NoOfAdult);
                    #region Room
                    //if (RoomNo == "1")
                    //{
                    //    BookRQ.Append("<Room RoomNo=\"" + (x + 1) + "\" RateKey=\"" + RateKey + "\" NoOfAdults=\"" + NoOfAdult + "\" NoOfChild=\"" + NoOfChild + "\" ExtraBed=\"" + extrabed + "\">");
                    //}
                    //else if (RoomNo == "2")
                    //{
                    //    BookRQ.Append("<Room RoomNo=\"" + (x + 1) + "\" RateKey=\"" + Rkey + "\" NoOfAdults=\"" + NoOfAdult + "\" NoOfChild=\"" + NoOfChild + "\" ExtraBed=\"" + extrabed + "\">");
                    //}
                    //else if (RoomNo == "3")
                    //{
                    //    BookRQ.Append("<Room RoomNo=\"" + (x + 1) + "\" RateKey=\"" + HasKey + "\" NoOfAdults=\"" + NoOfAdult + "\" NoOfChild=\"" + NoOfChild + "\" ExtraBed=\"" + extrabed + "\">");
                    //}
                    //else if (RoomNo == "4")
                    //{
                    //    BookRQ.Append("<Room RoomNo=\"" + (x + 1) + "\" RateKey=\"" + Key + "\" NoOfAdults=\"" + NoOfAdult + "\" NoOfChild=\"" + NoOfChild + "\" ExtraBed=\"" + extrabed + "\">");
                    //}
                    //else if (RoomNo == "5")
                    //{
                    //    BookRQ.Append("<Room RoomNo=\"" + (x + 1) + "\" RateKey=\"" + Ratekey1 + "\" NoOfAdults=\"" + NoOfAdult + "\" NoOfChild=\"" + NoOfChild + "\" ExtraBed=\"" + extrabed + "\">");
                    //}

                    string[] TypeManage = HasKey_IntCode.Split(new String[] { "###" }, StringSplitOptions.None);

                    string TypeManageString = TypeManage[x].ToString();

                    BookRQ.Append("<Room RoomNo=\"" + (x + 1) + "\" RateKey=\"" + TypeManageString + "\" NoOfAdults=\"" + NoOfAdult + "\" NoOfChild=\"" + NoOfChild + "\" ExtraBed=\"" + extrabed + "\">");


                    dtPassengers.DefaultView.RowFilter = "PickUp='" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomTypeId"]) + "' and (RoomNo=" + (x + 1) + ")";
                    #region PaxDetails
                    BookRQ.Append("<PaxDetails>");
                    for (int y = 0; y < dtPassengers.DefaultView.Count; y++)
                    {

                        string Name = Convert.ToString(dtPassengers.DefaultView[y]["FirstName"]);
                        string SurName = Convert.ToString(dtPassengers.DefaultView[y]["LastName"]);
                        string PreFix = Convert.ToString(dtPassengers.DefaultView[y]["Prefix"]);
                        string Age = Convert.ToString(dtPassengers.DefaultView[y]["ChildAge"]);
                        BookRQ.Append("<Pax Salutation=\"" + PreFix + "\" FirstName=\"" + Name + "\" LastName=\"" + SurName + "\" Age=\"" + Age + "\" />");
                    }
                    BookRQ.Append("</PaxDetails>");
                    #endregion
                    BookRQ.Append("</Room>");
                    #endregion
                }
                BookRQ.Append("</Rooms>");
                BookRQ.Append("<OnHold>" + OnHold + "</OnHold>");
                BookRQ.Append("<AvailFlag>" + IsAvail + "</AvailFlag>");
                BookRQ.Append("<Currency>" + MgBedBankCurrency + "</Currency>");
                BookRQ.Append("<Language>" + EN + "</Language>");
                BookRQ.Append("<DetailLevel>" + DetailLevel + "</DetailLevel>");
                BookRQ.Append("</BookHotel>");

                string BookingRQ = BookRQ.ToString();

                return BookingRQ;
                #endregion
            }
            catch (Exception ex)
            {
                _ = _exception.Error(ex);
                return "";
            }
        }
    }
}
