using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using XConnect.CommonUtilities.Interface;
using XConnect.CommonUtilities;
using XConnect.ErrorLogHandler.Interface;
using XConnect.Markup.Interface;
using XConnect.Models.Hotel;
using XConnect.Models;
using XConnect.Supplier.Qtech.Interface;
using Microsoft.Extensions.Options;
using Options = XConnect.Models.Options;
using static Azure.Core.HttpHeader;
using System.Globalization;
using System.Net;
using System.IO.Compression;
using static XConnect.Models.ErrorLog;

namespace XConnect.Supplier.Qtech
{
    public class QtechRepository : IQtech
    {
        private readonly ICommonMethod _commonMethod;
        private readonly IException _exception;
        private readonly IMarkup _markup;
        private readonly ISupplierRes _supplierRes;
        private readonly KeySettings _keySettings;


        public QtechRepository(ICommonMethod commonMethod, IMarkup markup, ISupplierRes supplierRes, IOptions<KeySettings> keySettings, IException exception)
        {
            _commonMethod = commonMethod;
            _markup = markup;
            _supplierRes = supplierRes;
            _keySettings = keySettings.Value;
            _exception = exception;
        }

        public void GetQTechRequestOnly_Async_bck(DataTable dtROE, DataRow[] commonCode, DataTable dtPreferredDataHotel, HotelSearchParameter SearchPara, List<SupplierSearchConfig> QTechSearchConfig, List<SupplierDetails> QTechDetails, ref ConcurrentBag<HotelX> AllTask, string AgentCode, string CityCode, string CountryCode, List<StarRating> listRating, int Hit, string Provider, string XmlNationality)
        {
            try
            {

                string QTechCurrency = "";
                string Action = "";

                switch (Provider.ToLower())
                {
                    case "mercan":
                        QTechCurrency = QtechInfo.MercanCurrency;
                        Action = QtechInfo.Mercan_hotelListAction;
                        break;
                    case "worldavenues":
                        QTechCurrency = QtechInfo.WorldAvenuesCurrency;
                        Action = QtechInfo.Mercan_hotelListAction;
                        break;
                    case "asianoverland":
                        QTechCurrency = QtechInfo.AsianOverLandCurrency;
                        Action = QtechInfo.Mercan_hotelListAction;
                        break;
                    case "nirvana":
                        QTechCurrency = QtechInfo.NirvanaCurrency;
                        Action = QtechInfo.Mercan_hotelListAction;
                        break;
                }

                double CurrencyFactorQTech = 1;
                if (QTechCurrency.ToLower() != QtechInfo.SiteCurrency.ToLower())  //Note Currency
                {
                    CurrencyFactorQTech = _commonMethod.GetROEByCurrency(QTechCurrency, dtROE);
                }
                DataRow[] drQTech = dtPreferredDataHotel.Select("ServiceName='" + Provider + "'");
                string Ids = string.Empty;
                var paramValues1 = "";
                if (drQTech.Count() > 0)
                {
                    paramValues1 = String.Join(",", drQTech.Select(x => String.Join(",", Convert.ToString(x.ItemArray[0]))));
                    Ids = Convert.ToString(paramValues1);
                }
                if (SearchPara.IsSearchFor.ToLower().IndexOf("preferred") >= 0 && drQTech.Count() == 0)
                {
                    QTechSearchConfig = null;
                }
                #region "QTech"                

                if (QTechSearchConfig != null)
                {

                    for (int k = 0; k < QTechSearchConfig.Count; k++)
                    {
                        if (true)
                        {
                            if (Convert.ToInt32(QTechSearchConfig[k].City) == 1)
                            {
                                string Rating = _commonMethod.GetStarRatingString(SearchPara);
                                bool DebugMode = (Convert.ToInt32(QTechSearchConfig[k].DebugMode) == 0 ? false : true);

                                #region StarRating Logic
                                string Str = "";
                                if (SearchPara.Filters.IsShowRooms == 1)
                                {
                                    #region comment
                                    //for (int i = 0; i < listRating.Count; i++)
                                    //{
                                    // Str = Convert.ToString(listRating[i].Star) + ".0";
                                    #endregion
                                    #endregion
                                    {
                                        int Noofrecords = Convert.ToInt32(QTechSearchConfig[k].Noofrecords);
                                        DataTable[] splittedtables = commonCode.AsEnumerable()
                                                            .Select((row, index) => new { row, index })
                                                            .GroupBy(x => x.index / Noofrecords)  // integer division, the fractional part is truncated
                                                            .Select(g => g.Select(x => x.row).CopyToDataTable())
                                                            .ToArray();

                                        paramValues1 = String.Join(",", splittedtables[0].Select().Select(x => String.Join(",", Convert.ToString(x.ItemArray[0]))));
                                        string IdsForSearch = Convert.ToString(paramValues1);

                                        HotelX xhotel = new HotelX();
                                        xhotel.Supplier = Provider;
                                        xhotel.SearchTimeOut = (Convert.ToInt32(QTechSearchConfig[k].HotelSearchTimeOut));
                                        //if (Hit > 0)
                                        //{
                                        xhotel.ProcessTimeout = (Convert.ToInt32(QTechSearchConfig[k].HotelSearchTimeOut2));
                                        //}
                                        if (Provider.ToLower() == "nirvana")
                                        {
                                            xhotel.URL = QtechInfo.Nirvana_url + Action;
                                        }
                                        else
                                        {
                                            xhotel.URL = Convert.ToString(QTechDetails[0].WebSiteURL) + Action;
                                        }

                                        //if (Provider.ToLower() == "asianoverland") //due to ticket 42115
                                        //{
                                        //    //xhotel.URL = Convert.ToString("http://www.aosconnect.com:8080/ws/index.php?action=") + Action;
                                        //    xhotel.URL = Convert.ToString("http://api.aosconnect.com:8086/ws/index.php?action=") + Action; //#27-09-2024(b2b) added on 27-DEC-2024(API)
                                        //}
                                        //else
                                        //{
                                        //    xhotel.URL = Convert.ToString(QTechDetails[0].WebSiteURL) + Action;
                                        //}

                                        xhotel.Action = Action;
                                        xhotel.DebugeMode = DebugMode;
                                        xhotel.CurrencyCode = QTechCurrency;
                                        xhotel.CurrencyFactor = CurrencyFactorQTech;
                                        xhotel.XmlNationality = "";
                                        xhotel.XmlCity = CityCode;
                                        xhotel.XmlCountry = CountryCode;
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
                                            objrq.Request = XMLQTechRequestOnly_Async(Convert.ToInt32(xhotel.SearchTimeOut), SearchPara, xhotel.URL, Convert.ToString(QTechDetails[0].UserName), Convert.ToString(QTechDetails[0].Password), IdsForSearch, CityCode, CountryCode, XmlNationality, xhotel.SearchTimeOut, QTechCurrency, Str, "");
                                            objrq.Response = ""; RQRS.Add(objrq);
                                        }
                                        xhotel.RQRS = RQRS;
                                        #endregion
                                        AllTask.Add(xhotel);
                                    }
                                    //}
                                }
                                else
                                {
                                    int Noofrecords = Convert.ToInt32(QTechSearchConfig[k].Noofrecords);
                                    string IdsForSearch;
                                    DataTable[] splittedtables = commonCode.AsEnumerable()
                                                        .Select((row, index) => new { row, index })
                                                        .GroupBy(x => x.index / Noofrecords)
                                                        .Select(g => g.Select(x => x.row).CopyToDataTable())
                                                        .ToArray();
                                    for (int a = 0; a < splittedtables.Length; a++)
                                    {
                                        paramValues1 = String.Join(",", splittedtables[a].Select().Select(x => String.Join(",", Convert.ToString(x.ItemArray[0]))));
                                        //IdsForSearch = Convert.ToString(paramValues1);
                                        IdsForSearch = Convert.ToString(paramValues1);
                                        HotelX xhotel = new HotelX();
                                        xhotel.Supplier = Provider;
                                        xhotel.SearchTimeOut = (Convert.ToInt32(QTechSearchConfig[k].HotelSearchTimeOut));
                                        //if (Hit > 0)
                                        //{
                                        xhotel.ProcessTimeout = (Convert.ToInt32(QTechSearchConfig[k].HotelSearchTimeOut2));
                                        // }
                                        if (Provider.ToLower() == "nirvana")
                                        {
                                            xhotel.URL = QtechInfo.Nirvana_url + Action; //ConfigurationManager.AppSettings["Nirvana_url"] + Action;
                                        }
                                        else
                                        {
                                            xhotel.URL = Convert.ToString(QTechDetails[0].WebSiteURL) + Action;
                                        }
                                        //if (Provider.ToLower() == "asianoverland")  //due to ticket 42115
                                        //{
                                        //    //xhotel.URL = Convert.ToString("http://www.aosconnect.com:8080/ws/index.php?action=") + Action;
                                        //    xhotel.URL = Convert.ToString("http://api.aosconnect.com:8086/ws/index.php?action=") + Action; //#27-09-2024(b2b) added on 27-DEC-2024(API)
                                        //}
                                        //else
                                        //{
                                        //    xhotel.URL = Convert.ToString(QTechDetails[0].WebSiteURL) + Action;
                                        //}

                                        xhotel.Action = Action;
                                        xhotel.DebugeMode = DebugMode;
                                        xhotel.CurrencyCode = QTechCurrency;
                                        xhotel.CurrencyFactor = CurrencyFactorQTech;
                                        xhotel.XmlNationality = "";
                                        xhotel.XmlCity = CityCode;
                                        xhotel.XmlCountry = CountryCode;
                                        #region "CoomonCode"
                                        bool IsHitSeperate = false; //IsSuppliersWithSeperateRoomRquest(xhotel.Supplier);
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
                                            objrq.Request = XMLQTechRequestOnly_Async(Convert.ToInt32(xhotel.SearchTimeOut), SearchPara, xhotel.URL, Convert.ToString(QTechDetails[0].UserName), Convert.ToString(QTechDetails[0].Password), IdsForSearch, CityCode, CountryCode, XmlNationality, xhotel.SearchTimeOut, QTechCurrency, Str, "");
                                            objrq.Response = ""; RQRS.Add(objrq);
                                        }
                                        xhotel.RQRS = RQRS;
                                        #endregion
                                        AllTask.Add(xhotel);
                                    }
                                }

                            }
                            else
                            {
                                string Rating = _commonMethod.GetStarRatingString(SearchPara);
                                bool DebugMode = (Convert.ToInt32(QTechSearchConfig[k].DebugMode) == 0 ? false : true);

                                #region StarRating Logic
                                string Str = "";
                                if (SearchPara.Filters.IsShowRooms == 1)
                                {
                                    #region comment
                                    //for (int i = 0; i < listRating.Count; i++)
                                    //{
                                    // Str = Convert.ToString(listRating[i].Star) + ".0";
                                    #endregion
                                    #endregion
                                    {
                                        int Noofrecords = Convert.ToInt32(QTechSearchConfig[k].Noofrecords);
                                        DataTable[] splittedtables = commonCode.AsEnumerable()
                                                            .Select((row, index) => new { row, index })
                                                            .GroupBy(x => x.index / Noofrecords)  // integer division, the fractional part is truncated
                                                            .Select(g => g.Select(x => x.row).CopyToDataTable())
                                                            .ToArray();

                                        paramValues1 = String.Join(",", splittedtables[0].Select().Select(x => String.Join(",", Convert.ToString(x.ItemArray[0]))));
                                        string IdsForSearch = Convert.ToString(paramValues1);

                                        HotelX xhotel = new HotelX();
                                        xhotel.Supplier = Provider;
                                        xhotel.SearchTimeOut = (Convert.ToInt32(QTechSearchConfig[k].HotelSearchTimeOut));
                                        //if (Hit > 0)
                                        //{
                                        xhotel.ProcessTimeout = (Convert.ToInt32(QTechSearchConfig[k].HotelSearchTimeOut2));
                                        //}
                                        if (Provider.ToLower() == "nirvana")
                                        {
                                            xhotel.URL = QtechInfo.Nirvana_url + Action;// ConfigurationManager.AppSettings["Nirvana_url"] + Action;
                                        }
                                        else
                                        {
                                            xhotel.URL = Convert.ToString(QTechDetails[0].WebSiteURL) + Action;
                                        }
                                        //xhotel.URL = Convert.ToString(QTechDetails[0].WebSiteURL) + Action;

                                        //if (Provider.ToLower() == "asianoverland")  //due to ticket 42115
                                        //{
                                        //    //xhotel.URL = Convert.ToString("http://www.aosconnect.com:8080/ws/index.php?action=") + Action;
                                        //    xhotel.URL = Convert.ToString("http://api.aosconnect.com:8086/ws/index.php?action=") + Action; //#27-09-2024(b2b) added on 27-DEC-2024(API)
                                        //}
                                        //else
                                        //{
                                        //    xhotel.URL = Convert.ToString(QTechDetails[0].WebSiteURL) + Action;
                                        //}

                                        xhotel.Action = Action;
                                        xhotel.DebugeMode = DebugMode;
                                        xhotel.CurrencyCode = QTechCurrency;
                                        xhotel.CurrencyFactor = CurrencyFactorQTech;
                                        xhotel.XmlNationality = "";
                                        xhotel.XmlCity = CityCode;
                                        xhotel.XmlCountry = CountryCode;
                                        #region "CoomonCode"
                                        bool IsHitSeperate = false;// IsSuppliersWithSeperateRoomRquest(xhotel.Supplier);
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
                                            objrq.Request = XMLQTechRequestOnly_Async(Convert.ToInt32(xhotel.SearchTimeOut), SearchPara, xhotel.URL, Convert.ToString(QTechDetails[0].UserName), Convert.ToString(QTechDetails[0].Password), IdsForSearch, CityCode, CountryCode, XmlNationality, xhotel.SearchTimeOut, QTechCurrency, Str, "");
                                            objrq.Response = ""; RQRS.Add(objrq);
                                        }
                                        xhotel.RQRS = RQRS;
                                        #endregion
                                        AllTask.Add(xhotel);
                                    }
                                    //}
                                }
                                else
                                {
                                    int Noofrecords = Convert.ToInt32(QTechSearchConfig[k].Noofrecords);
                                    string IdsForSearch;
                                    DataTable[] splittedtables = commonCode.AsEnumerable()
                                                        .Select((row, index) => new { row, index })
                                                        .GroupBy(x => x.index / Noofrecords)
                                                        .Select(g => g.Select(x => x.row).CopyToDataTable())
                                                        .ToArray();
                                    for (int a = 0; a < splittedtables.Length; a++)
                                    {
                                        paramValues1 = String.Join(",", splittedtables[a].Select().Select(x => String.Join(",", Convert.ToString(x.ItemArray[0]))));
                                        //IdsForSearch = Convert.ToString(paramValues1);
                                        IdsForSearch = Convert.ToString(paramValues1);
                                        HotelX xhotel = new HotelX();
                                        xhotel.Supplier = Provider;
                                        xhotel.SearchTimeOut = (Convert.ToInt32(QTechSearchConfig[k].HotelSearchTimeOut));
                                        //if (Hit > 0)
                                        //{
                                        xhotel.ProcessTimeout = (Convert.ToInt32(QTechSearchConfig[k].HotelSearchTimeOut2));
                                        //}
                                        if (Provider.ToLower() == "nirvana")
                                        {
                                            xhotel.URL = QtechInfo.Nirvana_url + Action;// ConfigurationManager.AppSettings["Nirvana_url"] + Action;
                                        }
                                        else
                                        {
                                            xhotel.URL = Convert.ToString(QTechDetails[0].WebSiteURL) + Action;
                                        }
                                        // xhotel.URL = Convert.ToString(QTechDetails[0].WebSiteURL) + Action;

                                        //if (Provider.ToLower() == "asianoverland")  //due to ticket 42115
                                        //{
                                        //    //xhotel.URL = Convert.ToString("http://www.aosconnect.com:8080/ws/index.php?action=") + Action;
                                        //    xhotel.URL = Convert.ToString("http://api.aosconnect.com:8086/ws/index.php?action=") + Action; //#27-09-2024(b2b) added on 27-DEC-2024(API)
                                        //}
                                        //else
                                        //{
                                        //    xhotel.URL = Convert.ToString(QTechDetails[0].WebSiteURL) + Action;
                                        //}
                                        xhotel.Action = Action;
                                        xhotel.DebugeMode = DebugMode;
                                        xhotel.CurrencyCode = QTechCurrency;
                                        xhotel.CurrencyFactor = CurrencyFactorQTech;
                                        xhotel.XmlNationality = "";
                                        xhotel.XmlCity = CityCode;
                                        xhotel.XmlCountry = CountryCode;
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
                                            objrq.Request = XMLQTechRequestOnly_Async(Convert.ToInt32(xhotel.SearchTimeOut), SearchPara, xhotel.URL, Convert.ToString(QTechDetails[0].UserName), Convert.ToString(QTechDetails[0].Password), IdsForSearch, CityCode, CountryCode, XmlNationality, xhotel.SearchTimeOut, QTechCurrency, Str, "");
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
                #endregion
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
            }
        }

        public void GetQTechRequestOnly_Async_old(CommonSearchList commonSearchList, List<StarRating> listRating, DataTable dtROE, bool IsHotelWiseSearch, bool HitFromAll, HotelSearchParameter SearchPara, List<SupplierDetails> lstQTechDetails, List<SupplierSearchConfig> lstQTechSearchConfig, DataTable dtPreferredDataHotel, ref ConcurrentBag<HotelX> AllTask, int Hit, TokenData tokenData, DataSet dtXMLCity)
        {
            try
            {
                if (lstQTechDetails != null && lstQTechDetails.Count > 0)
                {
                    string QTechCurrency = "";
                    string Action = "";

                    List<SupplierDetails> QtechMain = new List<SupplierDetails>();
                    List<SupplierDetails> QtechChild = new List<SupplierDetails>();

                    foreach (SupplierDetails item in lstQTechDetails)
                    {
                        if (false)
                            QtechMain.Add(item);
                        else
                            QtechChild.Add(item);
                    }

                    for (int x = 0; x < QtechChild.Count; x++)
                    {
                        int SupplierID = QtechChild[x].SupplierID;
                        string Supplier = QtechChild[x].SupplierName;
                        DataRow[] commonCode = null;

                        #region Codes
                        if (Supplier.ToLower() == "asianoverland")
                            commonCode = commonSearchList.commonHotelCodes.AsianOverland_Codes;
                        else if (Supplier.ToLower() == "mercan")
                            commonCode = commonSearchList.commonHotelCodes.Mercan_Codes;
                        else if (Supplier.ToLower() == "nirvana")
                            commonCode = commonSearchList.commonHotelCodes.Nirvana_Codes;
                        else if (Supplier.ToLower() == "worldavenues")
                            commonCode = commonSearchList.commonHotelCodes.WorldAvenues_Codes;
                        #endregion

                        #region not use
                        //switch (Provider.ToLower())
                        //switch (Supplier.ToLower())
                        //{
                        //    case "mercan":
                        //        QTechCurrency = QtechInfo.MercanCurrency;
                        //        Action = QtechInfo.Mercan_hotelListAction;
                        //        break;
                        //    case "worldavenues":
                        //        QTechCurrency = QtechInfo.WorldAvenuesCurrency;
                        //        Action = QtechInfo.Mercan_hotelListAction;
                        //        break;
                        //    case "asianoverland":
                        //        QTechCurrency = QtechInfo.AsianOverLandCurrency;
                        //        Action = QtechInfo.Mercan_hotelListAction;
                        //        break;
                        //    case "nirvana":
                        //        QTechCurrency = QtechInfo.NirvanaCurrency;
                        //        Action = QtechInfo.Mercan_hotelListAction;
                        //    break;
                        //}

                        #endregion                      

                        if (commonCode != null)
                        {
                            if (lstQTechSearchConfig.Count > 0)
                            {
                                List<SupplierSearchConfig> QtechSearchConfig = lstQTechSearchConfig
                                    .Where(x => x != null && x.SupplierID == SupplierID)
                                    .ToList();

                                if (QtechSearchConfig != null && QtechSearchConfig.Count > 0)
                                {
                                    DataForSearch dataForSearch = _commonMethod.GetXmlDataForSearchNew(SearchPara.Nationality, dtXMLCity, Supplier);

                                    if (dataForSearch.XmlNationality != "")
                                    {
                                        #region Qtech
                                        if ((SearchPara.IsSearchFor.ToLower().IndexOf(Supplier.ToLower()) >= 0 || SearchPara.IsSearchFor.ToLower().IndexOf("multi") >= 0) && HitFromAll)
                                        {
                                            string Ids = string.Empty;
                                            string xmlbase = "";
                                            _commonMethod.GetPrefferedHotelCodes(ref Ids, ref xmlbase, ref QtechSearchConfig, Supplier, dtPreferredDataHotel, SearchPara.IsSearchFor);

                                            //if (QtechSearchConfig != null && commonCode != null)
                                            {
                                                string Currency = QtechChild[x].CurrencyShortName;
                                                for (int k = 0; k < QtechSearchConfig.Count; k++)
                                                {
                                                    bool DebugMode = (Convert.ToInt32(QtechSearchConfig[k].DebugMode) == 0 ? false : true);
                                                    double CurrencyFactorQtech = 1;
                                                    if (Currency.ToLower() != tokenData.SiteCurrency.ToLower())
                                                    {
                                                        CurrencyFactorQtech = _commonMethod.GetROEByCurrency(Currency, dtROE);
                                                    }
                                                    if (commonCode.Length > 0)
                                                    {
                                                        if (Convert.ToInt32(QtechSearchConfig[k].CityID) == 0 || Convert.ToInt32(QtechSearchConfig[k].CityID) == SearchPara.CityID)//All City means 0
                                                        {
                                                            int Noofrecords = Convert.ToInt32(QtechSearchConfig[k].Noofrecords);
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
                                                                xhotel.SearchTimeOut = Convert.ToInt32(QtechSearchConfig[k].HotelSearchTimeOut);
                                                                //if (Hit > 0)
                                                                xhotel.ProcessTimeout = Convert.ToInt32(QtechSearchConfig[k].HotelSearchTimeOut2);

                                                                xhotel.URL = Convert.ToString(QtechChild[x].WebSiteURL) + QtechInfo.HotelAvail;
                                                                xhotel.UserId = Convert.ToString(QtechChild[x].UserID);
                                                                xhotel.UserName = Convert.ToString(QtechChild[x].UserName);
                                                                xhotel.PassWord = Convert.ToString(QtechChild[x].Password);
                                                                xhotel.CurrencyCode = Currency;
                                                                xhotel.CurrencyFactor = CurrencyFactorQtech;
                                                                xhotel.XmlNationality = dataForSearch.XmlNationality;
                                                                xhotel.XmlCity = dataForSearch.xmlCity;
                                                                xhotel.XmlCountry = "";


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
                                                                    //objrq.Request = XMLQTechRequestOnly_Async(Convert.ToInt32(xhotel.SearchTimeOut), SearchPara, xhotel.URL, Convert.ToString(QTechDetails[0].UserName), xhotel.PassWord, IdsForSearch, CityCode, CountryCode, dataForSearch.XmlNationality, xhotel.SearchTimeOut, QTechCurrency, Str, "");
                                                                    //objrq.Request = XMLQTechRequestOnly_Async(Convert.ToInt32(xhotel.SearchTimeOut), SearchPara, xhotel.URL, xhotel.UserName, xhotel.PassWord, IdsForSearch, CityCode, CountryCode, dataForSearch.XmlNationality, xhotel.SearchTimeOut, QTechCurrency, Str, "");
                                                                    objrq.Request = XMLQTechRequestOnly_Async(Convert.ToInt32(xhotel.SearchTimeOut), SearchPara, xhotel.URL, xhotel.UserName, xhotel.PassWord, IdsForSearch, dataForSearch.xmlCity, dataForSearch.xmlCountry, dataForSearch.XmlNationality, xhotel.SearchTimeOut, QTechCurrency, "1", "");
                                                                    objrq.Response = ""; RQRS.Add(objrq);
                                                                }
                                                                xhotel.RQRS = RQRS;
                                                                xhotel.ReturnRooms = 5; // Convert.ToInt32(JuniperChild[x].ReturnRooms);
                                                                xhotel.TechName = "Qtech"; // Convert.ToString(JuniperChild[x].TechName);
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

                        #region "QTech"
                        //double CurrencyFactorQTech = 1;
                        //if (QTechCurrency.ToLower() != QtechInfo.SiteCurrency.ToLower())  //Note Currency
                        //{
                        //    CurrencyFactorQTech = _commonMethod.GetROEByCurrency(QTechCurrency, dtROE);
                        //}
                        //DataRow[] drQTech = dtPreferredDataHotel.Select("ServiceName='" + Provider + "'");
                        //string Ids = string.Empty;
                        //var paramValues1 = "";
                        //if (drQTech.Count() > 0)
                        //{
                        //    paramValues1 = String.Join(",", drQTech.Select(x => String.Join(",", Convert.ToString(x.ItemArray[0]))));
                        //    Ids = Convert.ToString(paramValues1);
                        //}
                        //if (SearchPara.IsSearchFor.ToLower().IndexOf("preferred") >= 0 && drQTech.Count() == 0)
                        //{
                        //    QTechSearchConfig = null;
                        //}

                        //if (QTechSearchConfig != null)
                        //{
                        //    for (int k = 0; k < QTechSearchConfig.Count; k++)
                        //    {
                        //        if (true)
                        //        {
                        //            if (Convert.ToInt32(QTechSearchConfig[k].City) == 1)
                        //            {
                        //                string Rating = _commonMethod.GetStarRatingString(SearchPara);
                        //                bool DebugMode = (Convert.ToInt32(QTechSearchConfig[k].DebugMode) == 0 ? false : true);

                        //                #region StarRating Logic
                        //                string Str = "";
                        //                if (SearchPara.Filters.IsShowRooms == 1)
                        //                {
                        //                    #endregion
                        //                    {
                        //                        int Noofrecords = Convert.ToInt32(QTechSearchConfig[k].Noofrecords);
                        //                        DataTable[] splittedtables = commonCode.AsEnumerable()
                        //                                            .Select((row, index) => new { row, index })
                        //                                            .GroupBy(x => x.index / Noofrecords)  // integer division, the fractional part is truncated
                        //                                            .Select(g => g.Select(x => x.row).CopyToDataTable())
                        //                                            .ToArray();

                        //                        paramValues1 = String.Join(",", splittedtables[0].Select().Select(x => String.Join(",", Convert.ToString(x.ItemArray[0]))));
                        //                        string IdsForSearch = Convert.ToString(paramValues1);

                        //                        HotelX xhotel = new HotelX();
                        //                        xhotel.Supplier = Provider;
                        //                        xhotel.SearchTimeOut = (Convert.ToInt32(QTechSearchConfig[k].HotelSearchTimeOut));
                        //                        //if (Hit > 0)
                        //                        //{
                        //                        xhotel.ProcessTimeout = (Convert.ToInt32(QTechSearchConfig[k].HotelSearchTimeOut2));
                        //                        //}
                        //                        if (Provider.ToLower() == "nirvana")
                        //                        {
                        //                            xhotel.URL = QtechInfo.Nirvana_url + Action;
                        //                        }
                        //                        else
                        //                        {
                        //                            xhotel.URL = Convert.ToString(QTechDetails[0].WebSiteURL) + Action;
                        //                        }

                        //                        //if (Provider.ToLower() == "asianoverland") //due to ticket 42115
                        //                        //{
                        //                        //    //xhotel.URL = Convert.ToString("http://www.aosconnect.com:8080/ws/index.php?action=") + Action;
                        //                        //    xhotel.URL = Convert.ToString("http://api.aosconnect.com:8086/ws/index.php?action=") + Action; //#27-09-2024(b2b) added on 27-DEC-2024(API)
                        //                        //}
                        //                        //else
                        //                        //{
                        //                        //    xhotel.URL = Convert.ToString(QTechDetails[0].WebSiteURL) + Action;
                        //                        //}

                        //                        xhotel.Action = Action;
                        //                        xhotel.DebugeMode = DebugMode;
                        //                        xhotel.CurrencyCode = QTechCurrency;
                        //                        xhotel.CurrencyFactor = CurrencyFactorQTech;
                        //                        xhotel.XmlNationality = "";
                        //                        xhotel.XmlCity = CityCode;
                        //                        xhotel.XmlCountry = CountryCode;
                        //                        #region "CoomonCode"
                        //                        bool IsHitSeperate = IsSuppliersWithSeperateRoomRquest(xhotel.Supplier);
                        //                        int NoOfRoomsSearch = 1;
                        //                        if (IsHitSeperate)
                        //                            NoOfRoomsSearch = SearchPara.Rooms.Count;
                        //                        List<RQRS> RQRS = new List<RQRS>();
                        //                        xhotel.IsSeperate = IsHitSeperate;
                        //                        for (int rr = 0; rr < NoOfRoomsSearch; rr++)
                        //                        {
                        //                            RQRS objrq = new RQRS();
                        //                            List<RoomDetails> serchRooms = new List<RoomDetails>();
                        //                            serchRooms.Add(SearchPara.Rooms[rr]);
                        //                            objrq.Request = XMLQTechRequestOnly_Async(Convert.ToInt32(xhotel.SearchTimeOut), SearchPara, xhotel.URL, Convert.ToString(QTechDetails[0].UserName), Convert.ToString(QTechDetails[0].Password), IdsForSearch, CityCode, CountryCode, XmlNationality, xhotel.SearchTimeOut, QTechCurrency, Str, "");
                        //                            objrq.Response = ""; RQRS.Add(objrq);
                        //                        }
                        //                        xhotel.RQRS = RQRS;
                        //                        #endregion
                        //                        AllTask.Add(xhotel);
                        //                    }
                        //                    //}
                        //                }
                        //                else
                        //                {
                        //                    int Noofrecords = Convert.ToInt32(QTechSearchConfig[k].Noofrecords);
                        //                    string IdsForSearch;
                        //                    DataTable[] splittedtables = commonCode.AsEnumerable()
                        //                                        .Select((row, index) => new { row, index })
                        //                                        .GroupBy(x => x.index / Noofrecords)
                        //                                        .Select(g => g.Select(x => x.row).CopyToDataTable())
                        //                                        .ToArray();
                        //                    for (int a = 0; a < splittedtables.Length; a++)
                        //                    {
                        //                        paramValues1 = String.Join(",", splittedtables[a].Select().Select(x => String.Join(",", Convert.ToString(x.ItemArray[0]))));
                        //                        //IdsForSearch = Convert.ToString(paramValues1);
                        //                        IdsForSearch = Convert.ToString(paramValues1);
                        //                        HotelX xhotel = new HotelX();
                        //                        xhotel.Supplier = Provider;
                        //                        xhotel.SearchTimeOut = (Convert.ToInt32(QTechSearchConfig[k].HotelSearchTimeOut));
                        //                        //if (Hit > 0)
                        //                        //{
                        //                        xhotel.ProcessTimeout = (Convert.ToInt32(QTechSearchConfig[k].HotelSearchTimeOut2));
                        //                        // }
                        //                        if (Provider.ToLower() == "nirvana")
                        //                        {
                        //                            xhotel.URL = QtechInfo.Nirvana_url + Action; //ConfigurationManager.AppSettings["Nirvana_url"] + Action;
                        //                        }
                        //                        else
                        //                        {
                        //                            xhotel.URL = Convert.ToString(QTechDetails[0].WebSiteURL) + Action;
                        //                        }
                        //                        //if (Provider.ToLower() == "asianoverland")  //due to ticket 42115
                        //                        //{
                        //                        //    //xhotel.URL = Convert.ToString("http://www.aosconnect.com:8080/ws/index.php?action=") + Action;
                        //                        //    xhotel.URL = Convert.ToString("http://api.aosconnect.com:8086/ws/index.php?action=") + Action; //#27-09-2024(b2b) added on 27-DEC-2024(API)
                        //                        //}
                        //                        //else
                        //                        //{
                        //                        //    xhotel.URL = Convert.ToString(QTechDetails[0].WebSiteURL) + Action;
                        //                        //}

                        //                        xhotel.Action = Action;
                        //                        xhotel.DebugeMode = DebugMode;
                        //                        xhotel.CurrencyCode = QTechCurrency;
                        //                        xhotel.CurrencyFactor = CurrencyFactorQTech;
                        //                        xhotel.XmlNationality = "";
                        //                        xhotel.XmlCity = CityCode;
                        //                        xhotel.XmlCountry = CountryCode;
                        //                        #region "CoomonCode"
                        //                        bool IsHitSeperate = false; //IsSuppliersWithSeperateRoomRquest(xhotel.Supplier);
                        //                        int NoOfRoomsSearch = 1;
                        //                        if (IsHitSeperate)
                        //                            NoOfRoomsSearch = SearchPara.Rooms.Count;
                        //                        List<RQRS> RQRS = new List<RQRS>();
                        //                        xhotel.IsSeperate = IsHitSeperate;
                        //                        for (int rr = 0; rr < NoOfRoomsSearch; rr++)
                        //                        {
                        //                            RQRS objrq = new RQRS();
                        //                            List<RoomDetails> serchRooms = new List<RoomDetails>();
                        //                            serchRooms.Add(SearchPara.Rooms[rr]);
                        //                            objrq.Request = XMLQTechRequestOnly_Async(Convert.ToInt32(xhotel.SearchTimeOut), SearchPara, xhotel.URL, Convert.ToString(QTechDetails[0].UserName), Convert.ToString(QTechDetails[0].Password), IdsForSearch, CityCode, CountryCode, XmlNationality, xhotel.SearchTimeOut, QTechCurrency, Str, "");
                        //                            objrq.Response = ""; RQRS.Add(objrq);
                        //                        }
                        //                        xhotel.RQRS = RQRS;
                        //                        #endregion
                        //                        AllTask.Add(xhotel);
                        //                    }
                        //                }

                        //            }
                        //            else
                        //            {
                        //                string Rating = _commonMethod.GetStarRatingString(SearchPara);
                        //                bool DebugMode = (Convert.ToInt32(QTechSearchConfig[k].DebugMode) == 0 ? false : true);

                        //                #region StarRating Logic
                        //                string Str = "";
                        //                if (SearchPara.Filters.IsShowRooms == 1)
                        //                {
                        //                    #endregion
                        //                    {
                        //                        int Noofrecords = Convert.ToInt32(QTechSearchConfig[k].Noofrecords);
                        //                        DataTable[] splittedtables = commonCode.AsEnumerable()
                        //                                            .Select((row, index) => new { row, index })
                        //                                            .GroupBy(x => x.index / Noofrecords)  // integer division, the fractional part is truncated
                        //                                            .Select(g => g.Select(x => x.row).CopyToDataTable())
                        //                                            .ToArray();

                        //                        paramValues1 = String.Join(",", splittedtables[0].Select().Select(x => String.Join(",", Convert.ToString(x.ItemArray[0]))));
                        //                        string IdsForSearch = Convert.ToString(paramValues1);

                        //                        HotelX xhotel = new HotelX();
                        //                        xhotel.Supplier = Provider;
                        //                        xhotel.SearchTimeOut = (Convert.ToInt32(QTechSearchConfig[k].HotelSearchTimeOut));
                        //                        //if (Hit > 0)
                        //                        //{
                        //                        xhotel.ProcessTimeout = (Convert.ToInt32(QTechSearchConfig[k].HotelSearchTimeOut2));
                        //                        //}
                        //                        if (Provider.ToLower() == "nirvana")
                        //                        {
                        //                            xhotel.URL = QtechInfo.Nirvana_url + Action;// ConfigurationManager.AppSettings["Nirvana_url"] + Action;
                        //                        }
                        //                        else
                        //                        {
                        //                            xhotel.URL = Convert.ToString(QTechDetails[0].WebSiteURL) + Action;
                        //                        }
                        //                        //xhotel.URL = Convert.ToString(QTechDetails[0].WebSiteURL) + Action;

                        //                        //if (Provider.ToLower() == "asianoverland")  //due to ticket 42115
                        //                        //{
                        //                        //    //xhotel.URL = Convert.ToString("http://www.aosconnect.com:8080/ws/index.php?action=") + Action;
                        //                        //    xhotel.URL = Convert.ToString("http://api.aosconnect.com:8086/ws/index.php?action=") + Action; //#27-09-2024(b2b) added on 27-DEC-2024(API)
                        //                        //}
                        //                        //else
                        //                        //{
                        //                        //    xhotel.URL = Convert.ToString(QTechDetails[0].WebSiteURL) + Action;
                        //                        //}

                        //                        xhotel.Action = Action;
                        //                        xhotel.DebugeMode = DebugMode;
                        //                        xhotel.CurrencyCode = QTechCurrency;
                        //                        xhotel.CurrencyFactor = CurrencyFactorQTech;
                        //                        xhotel.XmlNationality = "";
                        //                        xhotel.XmlCity = CityCode;
                        //                        xhotel.XmlCountry = CountryCode;
                        //                        #region "CoomonCode"
                        //                        bool IsHitSeperate = false;// IsSuppliersWithSeperateRoomRquest(xhotel.Supplier);
                        //                        int NoOfRoomsSearch = 1;
                        //                        if (IsHitSeperate)
                        //                            NoOfRoomsSearch = SearchPara.Rooms.Count;
                        //                        List<RQRS> RQRS = new List<RQRS>();
                        //                        xhotel.IsSeperate = IsHitSeperate;
                        //                        for (int rr = 0; rr < NoOfRoomsSearch; rr++)
                        //                        {
                        //                            RQRS objrq = new RQRS();
                        //                            List<RoomDetails> serchRooms = new List<RoomDetails>();
                        //                            serchRooms.Add(SearchPara.Rooms[rr]);
                        //                            objrq.Request = XMLQTechRequestOnly_Async(Convert.ToInt32(xhotel.SearchTimeOut), SearchPara, xhotel.URL, Convert.ToString(QTechDetails[0].UserName), Convert.ToString(QTechDetails[0].Password), IdsForSearch, CityCode, CountryCode, XmlNationality, xhotel.SearchTimeOut, QTechCurrency, Str, "");
                        //                            objrq.Response = ""; RQRS.Add(objrq);
                        //                        }
                        //                        xhotel.RQRS = RQRS;
                        //                        #endregion
                        //                        AllTask.Add(xhotel);
                        //                    }
                        //                    //}
                        //                }
                        //                else
                        //                {
                        //                    int Noofrecords = Convert.ToInt32(QTechSearchConfig[k].Noofrecords);
                        //                    string IdsForSearch;
                        //                    DataTable[] splittedtables = commonCode.AsEnumerable()
                        //                                        .Select((row, index) => new { row, index })
                        //                                        .GroupBy(x => x.index / Noofrecords)
                        //                                        .Select(g => g.Select(x => x.row).CopyToDataTable())
                        //                                        .ToArray();
                        //                    for (int a = 0; a < splittedtables.Length; a++)
                        //                    {
                        //                        paramValues1 = String.Join(",", splittedtables[a].Select().Select(x => String.Join(",", Convert.ToString(x.ItemArray[0]))));
                        //                        //IdsForSearch = Convert.ToString(paramValues1);
                        //                        IdsForSearch = Convert.ToString(paramValues1);
                        //                        HotelX xhotel = new HotelX();
                        //                        xhotel.Supplier = Provider;
                        //                        xhotel.SearchTimeOut = (Convert.ToInt32(QTechSearchConfig[k].HotelSearchTimeOut));
                        //                        //if (Hit > 0)
                        //                        //{
                        //                        xhotel.ProcessTimeout = (Convert.ToInt32(QTechSearchConfig[k].HotelSearchTimeOut2));
                        //                        //}
                        //                        if (Provider.ToLower() == "nirvana")
                        //                        {
                        //                            xhotel.URL = QtechInfo.Nirvana_url + Action;// ConfigurationManager.AppSettings["Nirvana_url"] + Action;
                        //                        }
                        //                        else
                        //                        {
                        //                            xhotel.URL = Convert.ToString(QTechDetails[0].WebSiteURL) + Action;
                        //                        }
                        //                        // xhotel.URL = Convert.ToString(QTechDetails[0].WebSiteURL) + Action;

                        //                        //if (Provider.ToLower() == "asianoverland")  //due to ticket 42115
                        //                        //{
                        //                        //    //xhotel.URL = Convert.ToString("http://www.aosconnect.com:8080/ws/index.php?action=") + Action;
                        //                        //    xhotel.URL = Convert.ToString("http://api.aosconnect.com:8086/ws/index.php?action=") + Action; //#27-09-2024(b2b) added on 27-DEC-2024(API)
                        //                        //}
                        //                        //else
                        //                        //{
                        //                        //    xhotel.URL = Convert.ToString(QTechDetails[0].WebSiteURL) + Action;
                        //                        //}
                        //                        xhotel.Action = Action;
                        //                        xhotel.DebugeMode = DebugMode;
                        //                        xhotel.CurrencyCode = QTechCurrency;
                        //                        xhotel.CurrencyFactor = CurrencyFactorQTech;
                        //                        xhotel.XmlNationality = "";
                        //                        xhotel.XmlCity = CityCode;
                        //                        xhotel.XmlCountry = CountryCode;
                        //                        #region "CoomonCode"
                        //                        bool IsHitSeperate = IsSuppliersWithSeperateRoomRquest(xhotel.Supplier);
                        //                        int NoOfRoomsSearch = 1;
                        //                        if (IsHitSeperate)
                        //                            NoOfRoomsSearch = SearchPara.Rooms.Count;
                        //                        List<RQRS> RQRS = new List<RQRS>();
                        //                        xhotel.IsSeperate = IsHitSeperate;
                        //                        for (int rr = 0; rr < NoOfRoomsSearch; rr++)
                        //                        {
                        //                            RQRS objrq = new RQRS();
                        //                            List<RoomDetails> serchRooms = new List<RoomDetails>();
                        //                            serchRooms.Add(SearchPara.Rooms[rr]);
                        //                            objrq.Request = XMLQTechRequestOnly_Async(Convert.ToInt32(xhotel.SearchTimeOut), SearchPara, xhotel.URL, Convert.ToString(QTechDetails[0].UserName), Convert.ToString(QTechDetails[0].Password), IdsForSearch, CityCode, CountryCode, XmlNationality, xhotel.SearchTimeOut, QTechCurrency, Str, "");
                        //                            objrq.Response = ""; RQRS.Add(objrq);
                        //                        }
                        //                        xhotel.RQRS = RQRS;
                        //                        #endregion
                        //                        AllTask.Add(xhotel);
                        //                    }
                        //                }
                        //            }

                        //        }
                        //    }
                        //}
                        #endregion
                    }
                }

            }
            catch (Exception ex)
            {
                _exception.Error(ex);
            }
        }

        public void GetQTechRequestOnly_Async(CommonSearchList commonSearchList, List<StarRating> listRating, DataTable dtROE, bool IsHotelWiseSearch, bool HitFromAll, HotelSearchParameter SearchPara, List<SupplierDetails> lstQTechDetails, List<SupplierSearchConfig> lstQTechSearchConfig, DataTable dtPreferredDataHotel, ref ConcurrentBag<HotelX> AllTask, int Hit, TokenData tokenData, DataSet dtXMLCity)
        {
            try
            {
                if (lstQTechDetails != null && lstQTechDetails.Count > 0)
                {


                    List<SupplierDetails> QtechMain = new List<SupplierDetails>();
                    List<SupplierDetails> QtechChild = new List<SupplierDetails>();

                    foreach (SupplierDetails item in lstQTechDetails)
                    {
                        if (false)
                            QtechMain.Add(item);
                        else
                            QtechChild.Add(item);
                    }

                    for (int x = 0; x < QtechChild.Count; x++)
                    {
                        int SupplierID = QtechChild[x].SupplierID;
                        string Supplier = QtechChild[x].SupplierName;
                        DataRow[] commonCode = null;

                        #region Codes
                        if (Supplier.ToLower() == "asianoverland")
                            commonCode = commonSearchList.commonHotelCodes.AsianOverland_Codes;
                        else if (Supplier.ToLower() == "mercan")
                            commonCode = commonSearchList.commonHotelCodes.Mercan_Codes;
                        else if (Supplier.ToLower() == "nirvana")
                            commonCode = commonSearchList.commonHotelCodes.Nirvana_Codes;
                        else if (Supplier.ToLower() == "worldavenues")
                            commonCode = commonSearchList.commonHotelCodes.WorldAvenues_Codes;
                        #endregion

                        #region not use
                        //switch (Provider.ToLower())
                        //switch (Supplier.ToLower())
                        //{
                        //    case "mercan":
                        //        QTechCurrency = QtechInfo.MercanCurrency;
                        //        Action = QtechInfo.Mercan_hotelListAction;
                        //        break;
                        //    case "worldavenues":
                        //        QTechCurrency = QtechInfo.WorldAvenuesCurrency;
                        //        Action = QtechInfo.Mercan_hotelListAction;
                        //        break;
                        //    case "asianoverland":
                        //        QTechCurrency = QtechInfo.AsianOverLandCurrency;
                        //        Action = QtechInfo.Mercan_hotelListAction;
                        //        break;
                        //    case "nirvana":
                        //        QTechCurrency = QtechInfo.NirvanaCurrency;
                        //        Action = QtechInfo.Mercan_hotelListAction;
                        //    break;
                        //}

                        #endregion

                        if (commonCode != null)
                        {
                            if (lstQTechSearchConfig.Count > 0)
                            {
                                List<SupplierSearchConfig> QtechSearchConfig = lstQTechSearchConfig
                                    .Where(x => x != null && x.SupplierID == SupplierID)
                                    .ToList();

                                if (QtechSearchConfig != null && QtechSearchConfig.Count > 0)
                                {
                                    DataForSearch dataForSearch = _commonMethod.GetXmlDataForSearchNew(SearchPara.Nationality, dtXMLCity, Supplier);

                                    #region new
                                    int totalPax = 0;
                                    bool isValidate = true;
                                    for (int i = 0; i < SearchPara.Rooms.Count; i++)
                                    {
                                        totalPax = totalPax + (SearchPara.Rooms[i].NoofAdults + SearchPara.Rooms[i].NoofChild);
                                    }
                                    #endregion

                                    if (Supplier.ToLower() == "asianoverland")
                                        isValidate = false;


                                    if ((totalPax < 11 || isValidate) && dataForSearch.XmlNationality != "" && dataForSearch.xmlCity != "" && dataForSearch.xmlCountry != "")
                                    {
                                        #region Qtech
                                        if ((SearchPara.IsSearchFor.ToLower().IndexOf(Supplier.ToLower()) >= 0 || SearchPara.IsSearchFor.ToLower().IndexOf("multi") >= 0) && HitFromAll)
                                        {
                                            string Ids = string.Empty;
                                            string xmlbase = "";
                                            _commonMethod.GetPrefferedHotelCodes(ref Ids, ref xmlbase, ref QtechSearchConfig, Supplier, dtPreferredDataHotel, SearchPara.IsSearchFor);

                                            //if (QtechSearchConfig != null && commonCode != null)
                                            {
                                                string Currency = QtechChild[x].CurrencyShortName;
                                                for (int k = 0; k < QtechSearchConfig.Count; k++)
                                                {
                                                    bool DebugMode = (Convert.ToInt32(QtechSearchConfig[k].DebugMode) == 0 ? false : true);
                                                    double CurrencyFactorQtech = 1;
                                                    if (Currency.ToLower() != tokenData.SiteCurrency.ToLower())
                                                    {
                                                        CurrencyFactorQtech = _commonMethod.GetROEByCurrency(Currency, dtROE);
                                                    }
                                                    if (commonCode.Length > 0)
                                                    {
                                                        if (Convert.ToInt32(QtechSearchConfig[k].CityID) == 1 || Convert.ToInt32(QtechSearchConfig[k].CityID) == SearchPara.CityID)//All City means 0
                                                        {
                                                            if (SearchPara.Filters.IsShowRooms == 1)
                                                            {
                                                                int Noofrecords = Convert.ToInt32(QtechSearchConfig[k].Noofrecords);
                                                                DataTable[] splittedtables = commonCode.AsEnumerable()
                                                                         .Select((row, index) => new { row, index })
                                                                         .GroupBy(x => x.index / Noofrecords)  // integer division, the fractional part is truncated
                                                                         .Select(g => g.Select(x => x.row).CopyToDataTable())
                                                                         .ToArray();
                                                                //for (int a = 0; a < splittedtables.Length; a++)
                                                                {

                                                                    var paramValues1 = String.Join(",", splittedtables[0].Select().Select(x => String.Join(",", Convert.ToString(x.ItemArray[0]))));
                                                                    string IdsForSearch = Convert.ToString(paramValues1);
                                                                    HotelX xhotel = new HotelX();
                                                                    xhotel.Supplier = Supplier;
                                                                    xhotel.DebugeMode = DebugMode;
                                                                    xhotel.SearchTimeOut = Convert.ToInt32(QtechSearchConfig[k].HotelSearchTimeOut);
                                                                    //if (Hit > 0)
                                                                    xhotel.ProcessTimeout = Convert.ToInt32(QtechSearchConfig[k].HotelSearchTimeOut2);

                                                                    xhotel.URL = Convert.ToString(QtechChild[x].WebSiteURL) + QtechInfo.HotelAvail;
                                                                    xhotel.UserId = Convert.ToString(QtechChild[x].UserID);
                                                                    xhotel.UserName = Convert.ToString(QtechChild[x].UserName);
                                                                    xhotel.PassWord = Convert.ToString(QtechChild[x].Password);
                                                                    xhotel.CurrencyCode = Currency;
                                                                    xhotel.CurrencyFactor = CurrencyFactorQtech;
                                                                    xhotel.XmlNationality = dataForSearch.XmlNationality;
                                                                    xhotel.XmlCity = dataForSearch.xmlCity;
                                                                    xhotel.XmlCountry = dataForSearch.xmlCountry;


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
                                                                        //objrq.Request = XMLQTechRequestOnly_Async(Convert.ToInt32(xhotel.SearchTimeOut), SearchPara, xhotel.URL, Convert.ToString(QTechDetails[0].UserName), xhotel.PassWord, IdsForSearch, CityCode, CountryCode, dataForSearch.XmlNationality, xhotel.SearchTimeOut, QTechCurrency, Str, "");
                                                                        //objrq.Request = XMLQTechRequestOnly_Async(Convert.ToInt32(xhotel.SearchTimeOut), SearchPara, xhotel.URL, xhotel.UserName, xhotel.PassWord, IdsForSearch, CityCode, CountryCode, dataForSearch.XmlNationality, xhotel.SearchTimeOut, QTechCurrency, Str, "");
                                                                        objrq.Request = XMLQTechRequestOnly_Async(Convert.ToInt32(xhotel.SearchTimeOut), SearchPara, xhotel.URL, xhotel.UserName, xhotel.PassWord, IdsForSearch, dataForSearch.xmlCity, dataForSearch.xmlCountry, dataForSearch.XmlNationality, xhotel.SearchTimeOut, Currency, "1", "");
                                                                        objrq.Response = ""; RQRS.Add(objrq);
                                                                    }
                                                                    xhotel.RQRS = RQRS;
                                                                    xhotel.ReturnRooms = 5; // Convert.ToInt32(QtechChild[x].ReturnRooms);
                                                                    xhotel.TechName = "Qtech"; // Convert.ToString(QtechChild[x].TechName);
                                                                    #endregion
                                                                    AllTask.Add(xhotel);
                                                                }
                                                            }
                                                            else
                                                            {
                                                                int Noofrecords = Convert.ToInt32(QtechSearchConfig[k].Noofrecords);
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
                                                                    xhotel.SearchTimeOut = Convert.ToInt32(QtechSearchConfig[k].HotelSearchTimeOut);
                                                                    //if (Hit > 0)
                                                                    xhotel.ProcessTimeout = Convert.ToInt32(QtechSearchConfig[k].HotelSearchTimeOut2);

                                                                    xhotel.URL = Convert.ToString(QtechChild[x].WebSiteURL) + QtechInfo.HotelAvail;
                                                                    xhotel.UserId = Convert.ToString(QtechChild[x].UserID);
                                                                    xhotel.UserName = Convert.ToString(QtechChild[x].UserName);
                                                                    xhotel.PassWord = Convert.ToString(QtechChild[x].Password);
                                                                    xhotel.CurrencyCode = Currency;
                                                                    xhotel.CurrencyFactor = CurrencyFactorQtech;
                                                                    xhotel.XmlNationality = dataForSearch.XmlNationality;
                                                                    xhotel.XmlCity = dataForSearch.xmlCity;
                                                                    xhotel.XmlCountry = dataForSearch.xmlCountry;


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
                                                                        //objrq.Request = XMLQTechRequestOnly_Async(Convert.ToInt32(xhotel.SearchTimeOut), SearchPara, xhotel.URL, Convert.ToString(QTechDetails[0].UserName), xhotel.PassWord, IdsForSearch, CityCode, CountryCode, dataForSearch.XmlNationality, xhotel.SearchTimeOut, QTechCurrency, Str, "");
                                                                        //objrq.Request = XMLQTechRequestOnly_Async(Convert.ToInt32(xhotel.SearchTimeOut), SearchPara, xhotel.URL, xhotel.UserName, xhotel.PassWord, IdsForSearch, CityCode, CountryCode, dataForSearch.XmlNationality, xhotel.SearchTimeOut, QTechCurrency, Str, "");
                                                                        objrq.Request = XMLQTechRequestOnly_Async(Convert.ToInt32(xhotel.SearchTimeOut), SearchPara, xhotel.URL, xhotel.UserName, xhotel.PassWord, IdsForSearch, dataForSearch.xmlCity, dataForSearch.xmlCountry, dataForSearch.XmlNationality, xhotel.SearchTimeOut, Currency, "1", "");
                                                                        objrq.Response = ""; RQRS.Add(objrq);
                                                                    }
                                                                    xhotel.RQRS = RQRS;
                                                                    xhotel.ReturnRooms = 5; // Convert.ToInt32(QtechChild[x].ReturnRooms);
                                                                    xhotel.TechName = "Qtech"; // Convert.ToString(QtechChild[x].TechName);
                                                                    #endregion
                                                                    AllTask.Add(xhotel);
                                                                }
                                                            }
                                                        }
                                                        else
                                                        {
                                                            if (SearchPara.Filters.IsShowRooms == 1)
                                                            {
                                                                int Noofrecords = Convert.ToInt32(QtechSearchConfig[k].Noofrecords);
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
                                                                    xhotel.SearchTimeOut = Convert.ToInt32(QtechSearchConfig[k].HotelSearchTimeOut);
                                                                    //if (Hit > 0)
                                                                    xhotel.ProcessTimeout = Convert.ToInt32(QtechSearchConfig[k].HotelSearchTimeOut2);

                                                                    xhotel.URL = Convert.ToString(QtechChild[x].WebSiteURL) + QtechInfo.HotelAvail;
                                                                    xhotel.UserId = Convert.ToString(QtechChild[x].UserID);
                                                                    xhotel.UserName = Convert.ToString(QtechChild[x].UserName);
                                                                    xhotel.PassWord = Convert.ToString(QtechChild[x].Password);
                                                                    xhotel.CurrencyCode = Currency;
                                                                    xhotel.CurrencyFactor = CurrencyFactorQtech;
                                                                    xhotel.XmlNationality = dataForSearch.XmlNationality;
                                                                    xhotel.XmlCity = dataForSearch.xmlCity;
                                                                    xhotel.XmlCountry = dataForSearch.xmlCountry;


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
                                                                        //objrq.Request = XMLQTechRequestOnly_Async(Convert.ToInt32(xhotel.SearchTimeOut), SearchPara, xhotel.URL, Convert.ToString(QTechDetails[0].UserName), xhotel.PassWord, IdsForSearch, CityCode, CountryCode, dataForSearch.XmlNationality, xhotel.SearchTimeOut, QTechCurrency, Str, "");
                                                                        //objrq.Request = XMLQTechRequestOnly_Async(Convert.ToInt32(xhotel.SearchTimeOut), SearchPara, xhotel.URL, xhotel.UserName, xhotel.PassWord, IdsForSearch, CityCode, CountryCode, dataForSearch.XmlNationality, xhotel.SearchTimeOut, QTechCurrency, Str, "");
                                                                        objrq.Request = XMLQTechRequestOnly_Async(Convert.ToInt32(xhotel.SearchTimeOut), SearchPara, xhotel.URL, xhotel.UserName, xhotel.PassWord, IdsForSearch, dataForSearch.xmlCity, dataForSearch.xmlCountry, dataForSearch.XmlNationality, xhotel.SearchTimeOut, Currency, "1", "");
                                                                        objrq.Response = ""; RQRS.Add(objrq);
                                                                    }
                                                                    xhotel.RQRS = RQRS;
                                                                    xhotel.ReturnRooms = 5; // Convert.ToInt32(QtechChild[x].ReturnRooms);
                                                                    xhotel.TechName = "Qtech"; // Convert.ToString(QtechChild[x].TechName);
                                                                    #endregion
                                                                    AllTask.Add(xhotel);
                                                                }
                                                            }
                                                            else
                                                            {
                                                                int Noofrecords = Convert.ToInt32(QtechSearchConfig[k].Noofrecords);
                                                                DataTable[] splittedtables = commonCode.AsEnumerable()
                                                                         .Select((row, index) => new { row, index })
                                                                         .GroupBy(x => x.index / Noofrecords)  // integer division, the fractional part is truncated
                                                                         .Select(g => g.Select(x => x.row).CopyToDataTable())
                                                                         .ToArray();
                                                                //for (int a = 0; a < splittedtables.Length; a++)
                                                                {

                                                                    var paramValues1 = String.Join(",", splittedtables[0].Select().Select(x => String.Join(",", Convert.ToString(x.ItemArray[0]))));
                                                                    string IdsForSearch = Convert.ToString(paramValues1);
                                                                    HotelX xhotel = new HotelX();
                                                                    xhotel.Supplier = Supplier;
                                                                    xhotel.DebugeMode = DebugMode;
                                                                    xhotel.SearchTimeOut = Convert.ToInt32(QtechSearchConfig[k].HotelSearchTimeOut);
                                                                    //if (Hit > 0)
                                                                    xhotel.ProcessTimeout = Convert.ToInt32(QtechSearchConfig[k].HotelSearchTimeOut2);

                                                                    xhotel.URL = Convert.ToString(QtechChild[x].WebSiteURL) + QtechInfo.HotelAvail;
                                                                    xhotel.UserId = Convert.ToString(QtechChild[x].UserID);
                                                                    xhotel.UserName = Convert.ToString(QtechChild[x].UserName);
                                                                    xhotel.PassWord = Convert.ToString(QtechChild[x].Password);
                                                                    xhotel.CurrencyCode = Currency;
                                                                    xhotel.CurrencyFactor = CurrencyFactorQtech;
                                                                    xhotel.XmlNationality = dataForSearch.XmlNationality;
                                                                    xhotel.XmlCity = dataForSearch.xmlCity;
                                                                    xhotel.XmlCountry = dataForSearch.xmlCountry;


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
                                                                        //objrq.Request = XMLQTechRequestOnly_Async(Convert.ToInt32(xhotel.SearchTimeOut), SearchPara, xhotel.URL, Convert.ToString(QTechDetails[0].UserName), xhotel.PassWord, IdsForSearch, CityCode, CountryCode, dataForSearch.XmlNationality, xhotel.SearchTimeOut, QTechCurrency, Str, "");
                                                                        //objrq.Request = XMLQTechRequestOnly_Async(Convert.ToInt32(xhotel.SearchTimeOut), SearchPara, xhotel.URL, xhotel.UserName, xhotel.PassWord, IdsForSearch, CityCode, CountryCode, dataForSearch.XmlNationality, xhotel.SearchTimeOut, QTechCurrency, Str, "");
                                                                        objrq.Request = XMLQTechRequestOnly_Async(Convert.ToInt32(xhotel.SearchTimeOut), SearchPara, xhotel.URL, xhotel.UserName, xhotel.PassWord, IdsForSearch, dataForSearch.xmlCity, dataForSearch.xmlCountry, dataForSearch.XmlNationality, xhotel.SearchTimeOut, Currency, "1", "");
                                                                        objrq.Response = ""; RQRS.Add(objrq);
                                                                    }
                                                                    xhotel.RQRS = RQRS;
                                                                    xhotel.ReturnRooms = 5; // Convert.ToInt32(QtechChild[x].ReturnRooms);
                                                                    xhotel.TechName = "Qtech"; // Convert.ToString(QtechChild[x].TechName);
                                                                    #endregion
                                                                    AllTask.Add(xhotel);
                                                                }
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

        public void GetQTechRequestOnly_Async(DataRow[] commonCode, List<StarRating> listRating, DataTable dtROE, bool IsHotelWiseSearch, bool HitFromAll, HotelSearchParameter SearchPara, List<SupplierDetails> lstQTechDetails, List<SupplierSearchConfig> lstQTechSearchConfig, DataTable dtPreferredDataHotel, ref ConcurrentBag<HotelX> AllTask, int Hit, TokenData tokenData, DataSet dtXMLCity)
        {
            try
            {
                if (lstQTechDetails != null && lstQTechDetails.Count > 0)
                {

                    List<StarRating> lstRating = new List<StarRating>();
                    lstRating = _commonMethod.GetStarRatingStringList_xtg(SearchPara);

                    List<HotelCodeByCity> DtHotelDAta = HtmlExtensions.ToList<HotelCodeByCity>(commonCode.CopyToDataTable());
                    int ratingcount = lstRating.Count;
                    int hotelcount = DtHotelDAta.Count;



                    foreach (SupplierDetails QtechChild in lstQTechDetails)
                    {
                        int SupplierID = QtechChild.SupplierID;
                        string Supplier = QtechChild.SupplierName;



                        #region not use
                        //switch (Provider.ToLower())
                        //switch (Supplier.ToLower())
                        //{
                        //    case "mercan":
                        //        QTechCurrency = QtechInfo.MercanCurrency;
                        //        Action = QtechInfo.Mercan_hotelListAction;
                        //        break;
                        //    case "worldavenues":
                        //        QTechCurrency = QtechInfo.WorldAvenuesCurrency;
                        //        Action = QtechInfo.Mercan_hotelListAction;
                        //        break;
                        //    case "asianoverland":
                        //        QTechCurrency = QtechInfo.AsianOverLandCurrency;
                        //        Action = QtechInfo.Mercan_hotelListAction;
                        //        break;
                        //    case "nirvana":
                        //        QTechCurrency = QtechInfo.NirvanaCurrency;
                        //        Action = QtechInfo.Mercan_hotelListAction;
                        //    break;
                        //}

                        #endregion

                        if (commonCode != null)
                        {
                            if (lstQTechSearchConfig.Count > 0)
                            {
                                List<SupplierSearchConfig> QtechSearchConfig = lstQTechSearchConfig
                                    .Where(x => x != null && x.SupplierID == SupplierID)
                                    .ToList();

                                if (QtechSearchConfig != null && QtechSearchConfig.Count > 0)
                                {
                                    //DataForSearch dataForSearch = _commonMethod.GetXmlDataForSearchNew(SearchPara.Nationality, dtXMLCity, Supplier);
                                    DataForSearch dataForSearch = _commonMethod.GetXmlDataForSearchTechwise(SearchPara.Nationality, dtXMLCity, Supplier, "qtech");


                                    #region new
                                    int totalPax = 0;
                                    bool isValidate = true;
                                    for (int i = 0; i < SearchPara.Rooms.Count; i++)
                                    {
                                        totalPax = totalPax + (SearchPara.Rooms[i].NoofAdults + SearchPara.Rooms[i].NoofChild);
                                    }
                                    #endregion

                                    if (Supplier.ToLower() == "asianoverland")
                                        isValidate = false;


                                    if ((totalPax < 11 || isValidate) && dataForSearch.XmlNationality != "" /*&& dataForSearch.xmlCity != ""*/ && dataForSearch.xmlCountry != "") //dataForSearch.xmlCity for search using countryid
                                    {
                                        #region Qtech
                                        //if ((SearchPara.IsSearchFor.ToLower().IndexOf(Supplier.ToLower()) >= 0 || SearchPara.IsSearchFor.ToLower().IndexOf("multi") >= 0) && HitFromAll)
                                        {
                                            string Ids = string.Empty;
                                            string xmlbase = "";
                                            _commonMethod.GetPrefferedHotelCodes(ref Ids, ref xmlbase, ref QtechSearchConfig, Supplier, dtPreferredDataHotel, SearchPara.IsSearchFor);

                                            List<HotelCodeByCity> filter = new List<HotelCodeByCity>();
                                            filter = _commonMethod.FilterWithOutCity_Code(ratingcount, hotelcount, DtHotelDAta, lstRating, Supplier);


                                            //if (QtechSearchConfig != null && commonCode != null)
                                            {
                                                string Currency = QtechChild.CurrencyShortName;
                                                double CurrencyFactorQtech = 1;

                                                if (Currency.ToLower() != tokenData.SiteCurrency.ToLower())
                                                {
                                                    CurrencyFactorQtech = _commonMethod.GetROEByCurrency(Currency, dtROE);
                                                }

                                                for (int k = 0; k < QtechSearchConfig.Count; k++)
                                                {
                                                    bool DebugMode = (Convert.ToInt32(QtechSearchConfig[k].DebugMode) == 0 ? false : true);

                                                    if (filter.Count > 0)
                                                    {
                                                        if (Convert.ToInt32(QtechSearchConfig[k].CityID) == 1 || Convert.ToInt32(QtechSearchConfig[k].CityID) == SearchPara.CityID)//All City means 0
                                                        {
                                                            if (SearchPara.Filters.IsShowRooms == 1)
                                                            {
                                                                int NoOfRecord = Convert.ToInt32(QtechSearchConfig[k].Noofrecords);
                                                                double count = Convert.ToDouble(filter.Count);
                                                                int NoOfDiv = Convert.ToInt32(Math.Ceiling(count / Convert.ToDouble(NoOfRecord)));
                                                                List<List<XMLID_Search>> lstxml = _commonMethod.GetXMLIDString(NoOfDiv, NoOfRecord, count, filter);

                                                                //for (int a = 0; a < splittedtables.Length; a++)
                                                                {

                                                                    //var paramValues1 = String.Join(",", splittedtables[0].Select().Select(x => String.Join(",", Convert.ToString(x.ItemArray[0]))));
                                                                    //string IdsForSearch = Convert.ToString(paramValues1);

                                                                    string IdsForSearch = Convert.ToString(_commonMethod.GetHotelCodeByCOmma(lstxml[0]));


                                                                    HotelX xhotel = new HotelX();
                                                                    xhotel.Supplier = Supplier;
                                                                    xhotel.DebugeMode = DebugMode;
                                                                    xhotel.SearchTimeOut = Convert.ToInt32(QtechSearchConfig[k].HotelSearchTimeOut);
                                                                    //if (Hit > 0)
                                                                    xhotel.ProcessTimeout = Convert.ToInt32(QtechSearchConfig[k].HotelSearchTimeOut2);

                                                                    //xhotel.URL = Convert.ToString(QtechChild.WebSiteURL) + QtechInfo.HotelAvail;
                                                                    if (_keySettings.SupplierProxy == "0")
                                                                    {
                                                                        if (Supplier.ToLower() == "nirvana")
                                                                            xhotel.URL = Convert.ToString(QtechInfo.Nirvana_url) + QtechInfo.HotelAvail;
                                                                        else
                                                                            xhotel.URL = Convert.ToString(QtechChild.WebSiteURL) + QtechInfo.HotelAvail;

                                                                    }
                                                                    else
                                                                    {
                                                                        if (Supplier.ToLower() == "nirvana")
                                                                            xhotel.URL = Convert.ToString(QtechInfo.Nirvana_url) + QtechInfo.HotelAvail;
                                                                        else
                                                                            xhotel.URL = Convert.ToString(QtechChild.WebSiteURLforAPI) + QtechInfo.HotelAvail;
                                                                    }

                                                                    xhotel.UserId = Convert.ToString(QtechChild.UserID);
                                                                    xhotel.UserName = Convert.ToString(QtechChild.UserName);
                                                                    xhotel.PassWord = Convert.ToString(QtechChild.Password);
                                                                    xhotel.CurrencyCode = Currency;
                                                                    xhotel.CurrencyFactor = CurrencyFactorQtech;
                                                                    xhotel.XmlNationality = dataForSearch.XmlNationality;
                                                                    xhotel.XmlCity = dataForSearch.xmlCity;
                                                                    xhotel.XmlCountry = dataForSearch.xmlCountry;


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
                                                                        //objrq.Request = XMLQTechRequestOnly_Async(Convert.ToInt32(xhotel.SearchTimeOut), SearchPara, xhotel.URL, Convert.ToString(QTechDetails[0].UserName), xhotel.PassWord, IdsForSearch, CityCode, CountryCode, dataForSearch.XmlNationality, xhotel.SearchTimeOut, QTechCurrency, Str, "");
                                                                        //objrq.Request = XMLQTechRequestOnly_Async(Convert.ToInt32(xhotel.SearchTimeOut), SearchPara, xhotel.URL, xhotel.UserName, xhotel.PassWord, IdsForSearch, CityCode, CountryCode, dataForSearch.XmlNationality, xhotel.SearchTimeOut, QTechCurrency, Str, "");
                                                                        objrq.Request = XMLQTechRequestOnly_Async(Convert.ToInt32(xhotel.SearchTimeOut), SearchPara, xhotel.URL, xhotel.UserName, xhotel.PassWord, IdsForSearch, dataForSearch.xmlCity, dataForSearch.xmlCountry, dataForSearch.XmlNationality, xhotel.SearchTimeOut, Currency, "1", "");
                                                                        objrq.Response = ""; RQRS.Add(objrq);
                                                                    }
                                                                    xhotel.RQRS = RQRS;
                                                                    xhotel.ReturnRooms = Convert.ToInt32(QtechChild.ReturnRooms);
                                                                    xhotel.TechName = Convert.ToString(QtechChild.TechName);
                                                                    #endregion
                                                                    AllTask.Add(xhotel);
                                                                }
                                                            }
                                                            else
                                                            {
                                                                //int Noofrecords = Convert.ToInt32(QtechSearchConfig[k].Noofrecords);
                                                                //DataTable[] splittedtables = commonCode.AsEnumerable()
                                                                //         .Select((row, index) => new { row, index })
                                                                //         .GroupBy(x => x.index / Noofrecords)  // integer division, the fractional part is truncated
                                                                //         .Select(g => g.Select(x => x.row).CopyToDataTable())
                                                                //         .ToArray();

                                                                int NoOfRecord = Convert.ToInt32(QtechSearchConfig[k].Noofrecords);
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
                                                                    xhotel.SearchTimeOut = Convert.ToInt32(QtechSearchConfig[k].HotelSearchTimeOut);
                                                                    //if (Hit > 0)
                                                                    xhotel.ProcessTimeout = Convert.ToInt32(QtechSearchConfig[k].HotelSearchTimeOut2);

                                                                    //xhotel.URL = Convert.ToString(QtechChild.WebSiteURL) + QtechInfo.HotelAvail;

                                                                    if (_keySettings.SupplierProxy == "0")
                                                                    {
                                                                        if (Supplier.ToLower() == "nirvana")
                                                                            xhotel.URL = Convert.ToString(QtechInfo.Nirvana_url) + QtechInfo.HotelAvail;
                                                                        else
                                                                            xhotel.URL = Convert.ToString(QtechChild.WebSiteURL) + QtechInfo.HotelAvail;

                                                                    }
                                                                    else
                                                                    {
                                                                        if (Supplier.ToLower() == "nirvana")
                                                                            xhotel.URL = Convert.ToString(QtechInfo.Nirvana_url) + QtechInfo.HotelAvail;
                                                                        else
                                                                            xhotel.URL = Convert.ToString(QtechChild.WebSiteURLforAPI) + QtechInfo.HotelAvail;
                                                                    }

                                                                    xhotel.UserId = Convert.ToString(QtechChild.UserID);
                                                                    xhotel.UserName = Convert.ToString(QtechChild.UserName);
                                                                    xhotel.PassWord = Convert.ToString(QtechChild.Password);
                                                                    xhotel.CurrencyCode = Currency;
                                                                    xhotel.CurrencyFactor = CurrencyFactorQtech;
                                                                    xhotel.XmlNationality = dataForSearch.XmlNationality;
                                                                    xhotel.XmlCity = dataForSearch.xmlCity;
                                                                    xhotel.XmlCountry = dataForSearch.xmlCountry;


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
                                                                        //objrq.Request = XMLQTechRequestOnly_Async(Convert.ToInt32(xhotel.SearchTimeOut), SearchPara, xhotel.URL, Convert.ToString(QTechDetails[0].UserName), xhotel.PassWord, IdsForSearch, CityCode, CountryCode, dataForSearch.XmlNationality, xhotel.SearchTimeOut, QTechCurrency, Str, "");
                                                                        //objrq.Request = XMLQTechRequestOnly_Async(Convert.ToInt32(xhotel.SearchTimeOut), SearchPara, xhotel.URL, xhotel.UserName, xhotel.PassWord, IdsForSearch, CityCode, CountryCode, dataForSearch.XmlNationality, xhotel.SearchTimeOut, QTechCurrency, Str, "");
                                                                        objrq.Request = XMLQTechRequestOnly_Async(Convert.ToInt32(xhotel.SearchTimeOut), SearchPara, xhotel.URL, xhotel.UserName, xhotel.PassWord, IdsForSearch, dataForSearch.xmlCity, dataForSearch.xmlCountry, dataForSearch.XmlNationality, xhotel.SearchTimeOut, Currency, "1", "");
                                                                        objrq.Response = ""; RQRS.Add(objrq);
                                                                    }
                                                                    xhotel.RQRS = RQRS;
                                                                    xhotel.ReturnRooms = Convert.ToInt32(QtechChild.ReturnRooms);
                                                                    xhotel.TechName = Convert.ToString(QtechChild.TechName);
                                                                    #endregion
                                                                    AllTask.Add(xhotel);
                                                                }
                                                            }
                                                        }
                                                        else
                                                        {
                                                            if (SearchPara.Filters.IsShowRooms == 1)
                                                            {
                                                                int NoOfRecord = Convert.ToInt32(QtechSearchConfig[k].Noofrecords);
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
                                                                    xhotel.SearchTimeOut = Convert.ToInt32(QtechSearchConfig[k].HotelSearchTimeOut);
                                                                    //if (Hit > 0)
                                                                    xhotel.ProcessTimeout = Convert.ToInt32(QtechSearchConfig[k].HotelSearchTimeOut2);

                                                                    //xhotel.URL = Convert.ToString(QtechChild.WebSiteURL) + QtechInfo.HotelAvail;

                                                                    if (_keySettings.SupplierProxy == "0")
                                                                    {
                                                                        if (Supplier.ToLower() == "nirvana")
                                                                            xhotel.URL = Convert.ToString(QtechInfo.Nirvana_url) + QtechInfo.HotelAvail;
                                                                        else
                                                                            xhotel.URL = Convert.ToString(QtechChild.WebSiteURL) + QtechInfo.HotelAvail;

                                                                    }
                                                                    else
                                                                    {
                                                                        if (Supplier.ToLower() == "nirvana")
                                                                            xhotel.URL = Convert.ToString(QtechInfo.Nirvana_url) + QtechInfo.HotelAvail;
                                                                        else
                                                                            xhotel.URL = Convert.ToString(QtechChild.WebSiteURLforAPI) + QtechInfo.HotelAvail;
                                                                    }

                                                                    xhotel.UserId = Convert.ToString(QtechChild.UserID);
                                                                    xhotel.UserName = Convert.ToString(QtechChild.UserName);
                                                                    xhotel.PassWord = Convert.ToString(QtechChild.Password);
                                                                    xhotel.CurrencyCode = Currency;
                                                                    xhotel.CurrencyFactor = CurrencyFactorQtech;
                                                                    xhotel.XmlNationality = dataForSearch.XmlNationality;
                                                                    xhotel.XmlCity = dataForSearch.xmlCity;
                                                                    xhotel.XmlCountry = dataForSearch.xmlCountry;


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
                                                                        //objrq.Request = XMLQTechRequestOnly_Async(Convert.ToInt32(xhotel.SearchTimeOut), SearchPara, xhotel.URL, Convert.ToString(QTechDetails[0].UserName), xhotel.PassWord, IdsForSearch, CityCode, CountryCode, dataForSearch.XmlNationality, xhotel.SearchTimeOut, QTechCurrency, Str, "");
                                                                        //objrq.Request = XMLQTechRequestOnly_Async(Convert.ToInt32(xhotel.SearchTimeOut), SearchPara, xhotel.URL, xhotel.UserName, xhotel.PassWord, IdsForSearch, CityCode, CountryCode, dataForSearch.XmlNationality, xhotel.SearchTimeOut, QTechCurrency, Str, "");
                                                                        objrq.Request = XMLQTechRequestOnly_Async(Convert.ToInt32(xhotel.SearchTimeOut), SearchPara, xhotel.URL, xhotel.UserName, xhotel.PassWord, IdsForSearch, dataForSearch.xmlCity, dataForSearch.xmlCountry, dataForSearch.XmlNationality, xhotel.SearchTimeOut, Currency, "1", "");
                                                                        objrq.Response = ""; RQRS.Add(objrq);
                                                                    }
                                                                    xhotel.RQRS = RQRS;
                                                                    xhotel.ReturnRooms = Convert.ToInt32(QtechChild.ReturnRooms);
                                                                    xhotel.TechName = Convert.ToString(QtechChild.TechName);
                                                                    #endregion
                                                                    AllTask.Add(xhotel);
                                                                }
                                                            }
                                                            else
                                                            {
                                                                int NoOfRecord = Convert.ToInt32(QtechSearchConfig[k].Noofrecords);
                                                                double count = Convert.ToDouble(filter.Count);
                                                                int NoOfDiv = Convert.ToInt32(Math.Ceiling(count / Convert.ToDouble(NoOfRecord)));
                                                                List<List<XMLID_Search>> lstxml = _commonMethod.GetXMLIDString(NoOfDiv, NoOfRecord, count, filter);

                                                                //for (int a = 0; a < splittedtables.Length; a++)
                                                                {

                                                                    //var paramValues1 = String.Join(",", splittedtables[0].Select().Select(x => String.Join(",", Convert.ToString(x.ItemArray[0]))));
                                                                    //string IdsForSearch = Convert.ToString(paramValues1);

                                                                    string IdsForSearch = Convert.ToString(_commonMethod.GetHotelCodeByCOmma(lstxml[0]));


                                                                    HotelX xhotel = new HotelX();
                                                                    xhotel.Supplier = Supplier;
                                                                    xhotel.DebugeMode = DebugMode;
                                                                    xhotel.SearchTimeOut = Convert.ToInt32(QtechSearchConfig[k].HotelSearchTimeOut);
                                                                    //if (Hit > 0)
                                                                    xhotel.ProcessTimeout = Convert.ToInt32(QtechSearchConfig[k].HotelSearchTimeOut2);

                                                                    //xhotel.URL = Convert.ToString(QtechChild.WebSiteURL) + QtechInfo.HotelAvail;

                                                                    if (_keySettings.SupplierProxy == "0")
                                                                    {
                                                                        if (Supplier.ToLower() == "nirvana")
                                                                            xhotel.URL = Convert.ToString(QtechInfo.Nirvana_url) + QtechInfo.HotelAvail;
                                                                        else
                                                                            xhotel.URL = Convert.ToString(QtechChild.WebSiteURL) + QtechInfo.HotelAvail;

                                                                    }
                                                                    else
                                                                    {
                                                                        if (Supplier.ToLower() == "nirvana")
                                                                            xhotel.URL = Convert.ToString(QtechInfo.Nirvana_url) + QtechInfo.HotelAvail;
                                                                        else
                                                                            xhotel.URL = Convert.ToString(QtechChild.WebSiteURLforAPI) + QtechInfo.HotelAvail;
                                                                    }

                                                                    xhotel.UserId = Convert.ToString(QtechChild.UserID);
                                                                    xhotel.UserName = Convert.ToString(QtechChild.UserName);
                                                                    xhotel.PassWord = Convert.ToString(QtechChild.Password);
                                                                    xhotel.CurrencyCode = Currency;
                                                                    xhotel.CurrencyFactor = CurrencyFactorQtech;
                                                                    xhotel.XmlNationality = dataForSearch.XmlNationality;
                                                                    xhotel.XmlCity = dataForSearch.xmlCity;
                                                                    xhotel.XmlCountry = dataForSearch.xmlCountry;


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
                                                                        //objrq.Request = XMLQTechRequestOnly_Async(Convert.ToInt32(xhotel.SearchTimeOut), SearchPara, xhotel.URL, Convert.ToString(QTechDetails[0].UserName), xhotel.PassWord, IdsForSearch, CityCode, CountryCode, dataForSearch.XmlNationality, xhotel.SearchTimeOut, QTechCurrency, Str, "");
                                                                        //objrq.Request = XMLQTechRequestOnly_Async(Convert.ToInt32(xhotel.SearchTimeOut), SearchPara, xhotel.URL, xhotel.UserName, xhotel.PassWord, IdsForSearch, CityCode, CountryCode, dataForSearch.XmlNationality, xhotel.SearchTimeOut, QTechCurrency, Str, "");
                                                                        objrq.Request = XMLQTechRequestOnly_Async(Convert.ToInt32(xhotel.SearchTimeOut), SearchPara, xhotel.URL, xhotel.UserName, xhotel.PassWord, IdsForSearch, dataForSearch.xmlCity, dataForSearch.xmlCountry, dataForSearch.XmlNationality, xhotel.SearchTimeOut, Currency, "1", "");
                                                                        objrq.Response = ""; RQRS.Add(objrq);
                                                                    }
                                                                    xhotel.RQRS = RQRS;
                                                                    xhotel.ReturnRooms = Convert.ToInt32(QtechChild.ReturnRooms);
                                                                    xhotel.TechName = Convert.ToString(QtechChild.TechName);
                                                                    #endregion
                                                                    AllTask.Add(xhotel);
                                                                }
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
        public string XMLQTechRequestOnly_Async(Int32 SearchTimeOut, HotelSearchParameter SearchParam, string QTech_URL, string QTech_User_Name, string QTech_Password, string HotelId, string CityCode, string CountryCode, string XmlNationality, Int32 TimeOut, string QTechCurrency, string Str, string type)
        {
            try
            {
                return QTechHotelListRequest(SearchParam, CityCode, CountryCode, QTech_URL, QTech_User_Name, QTech_Password, HotelId, XmlNationality, TimeOut, QTechCurrency, Str, type);
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
                return "";
            }
        }
        public string QTechHotelListRequest(HotelSearchParameter SearchPara, string QTech_CityCode, string CountryCode, string QTech_URL, string QTech_User_Name, string QTech_Password, string HotelId, string Nationality, Int32 TimeOut, string QTechCurrency, string Str, string type)
        {
            string Request = "";
            try
            {
                string Ids = "";
                //string Ids = HotelId;
                //if (SearchPara.Filters.IsShowRooms == 1)// add here by krunal 
                //{
                //     Ids = HotelId;
                //}
                #region AddRooms Logic
                TimeOut = TimeOut / 1000;
                int NoOfRooms = SearchPara.Rooms.Count();
                string CheckInDate = SearchPara.CheckInDate;
                string CheckOutDate = SearchPara.CheckOutDate;
                StringBuilder roomDetails = new StringBuilder();
                roomDetails.Append("[");
                for (int i = 0; i < NoOfRooms; i++)
                {
                    if (i > 0)
                        roomDetails.Append(",");
                    int NoOfAdult = SearchPara.Rooms[i].NoofAdults;
                    int NoOfChild = SearchPara.Rooms[i].NoofChild;
                    roomDetails.Append("{\"numberOfAdults\":" + NoOfAdult + "");
                    if (NoOfChild == 0)
                        roomDetails.Append("}");
                    if (NoOfChild > 0)
                    {
                        switch (NoOfChild)
                        {
                            case 1:
                                int Chid1Age = SearchPara.Rooms[i].Child1Age;
                                roomDetails.Append("," + "\"" + "numberOfChild" + "\":" + "" + NoOfChild + "" + "," + "\"" + "ChildAge" + "\":" + "\"" + Chid1Age + "\"}");
                                break;
                            case 2:
                                int ChidAge1 = SearchPara.Rooms[i].Child1Age;
                                int ChidAge2 = SearchPara.Rooms[i].Child2Age;
                                roomDetails.Append("," + "\"" + "numberOfChild" + "\":" + "" + NoOfChild + "" + "," + "\"" + "ChildAge" + "\":" + "\"" + ChidAge1 + "," + ChidAge2 + "\"}");
                                break;
                        }
                    }
                }
                roomDetails.Append("]");
                #endregion

                #region StarRating Logic
                //string[] Star = new string[(SearchPara.SelectedStart.Split(',')).Count()];// SelectedStart = "1,2,3,4,5"
                //string Str = "";
                //for (int i = 0; i < Star.Length; i++)
                //{
                //    Str = Str + SearchPara.SelectedStart.Split(',')[i] + ".0,";
                //}
                //Str = Str.Substring(0, Str.Length - 1);
                //string Str = "1.0,2.0,3.0,4.0,5.0";
                #endregion

                #region Req 
                StringBuilder SearchRQ = new StringBuilder();
                SearchRQ.Append(QTech_URL);
                SearchRQ.Append("&username=" + QTech_User_Name + "");
                SearchRQ.Append("&password=" + QTech_Password + "");
                SearchRQ.Append("&checkin_date=" + CheckInDate + "");
                SearchRQ.Append("&checkout_date=" + CheckOutDate + "");
                SearchRQ.Append("&sel_country=" + CountryCode + "");
                SearchRQ.Append("&sel_city=" + QTech_CityCode);

                if (type == "1")
                    SearchRQ.Append("&chk_ratings=" + Str + "");
                else
                    SearchRQ.Append("&chk_ratings=1.0,2.0,3.0,4.0,5.0");

                SearchRQ.Append("&sel_nationality=" + Nationality + "");
                SearchRQ.Append("&country_of_residence=" + Nationality + "");
                SearchRQ.Append("&sel_currency=" + QTechCurrency + "");
                SearchRQ.Append("&availableonly=1");
                SearchRQ.Append("&number_of_rooms=" + NoOfRooms + "");
                SearchRQ.Append("&roomDetails=" + roomDetails + "");

                if (type == "2")
                    SearchRQ.Append("&sel_hotel=" + "" + "");
                else
                    SearchRQ.Append("&sel_hotel=" + Ids + "");
                //SearchRQ.Append("&gzip=yes");
                SearchRQ.Append("&gzip=no");  //Note
                SearchRQ.Append("&timeout=" + TimeOut + "");
                SearchRQ.Append("&static_data=0");
                if (SearchPara.Filters.IsShowRooms == 1)
                {
                    SearchRQ.Append("&limit_hotel_room_type=100");
                }
                else
                {
                    SearchRQ.Append("&limit_hotel_room_type=1");
                }
                if (SearchPara.Filters.IsShowRooms == 0)//(type == "2") // change here by krunal 
                {
                    SearchRQ.Append("&hotel_ids=" + HotelId + "");
                }
                else
                {
                    SearchRQ.Append("&hotel_ids=" + HotelId + "");
                }

                Request = SearchRQ.ToString();

                #endregion
                Request = SearchRQ.ToString();


                return Request;
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
                return "";
            }
        }


        public async Task<List<Options>> QTech_HotelSearchWithRooms(HotelX Item, CommonSearch commonSearch, string ResponseData, HotelSearchParameter SearchParam, string XMLCITY, string XMLCountry, string Currency, double CurrencyFactor, string Provider, Int32 processTimeout, string ResProccessTimerLogActive, string ResProccessTimeValueforSaveLog, string XmlNationality = "", string SaveNoCountSupplog = "")
        {
            List<Options> options = new List<Options>();
            DataTable DtHotel = _commonMethod.GenerateDataTableNewV2();

            try
            {
                if (ResponseData != "")
                {
                    QtechListrs.Root Qtechres = null;

                    //Qtechres = JsonSerializer.Deserialize<QtechListrs.Root>(ResponseData);
                    Qtechres = Newtonsoft.Json.JsonConvert.DeserializeObject<QtechListrs.Root>(ResponseData);


                    if (ResponseData == string.Empty || ResponseData.Contains("ErrorTxt"))
                    {
                        Item.ResponseType = "NoCount";
                        return options;
                    }

                    if (Qtechres == null)
                    {
                        Item.ResponseType = "NoCount";
                        return options;
                    }

                    //DataSet RoomResponse = new DataSet();
                    //RoomResponse = _commonMethod.GetDataSetFromXMLString(ResponseData);
                    //string Message = Convert.ToString(RoomResponse.Tables["root"].Rows[0]["Message"]);  
                    string Message = Convert.ToString(Qtechres.Message[0]);
                    if (Message.ToLower() == "fail")
                    {
                        Item.ResponseType = "NoCount";
                        return options;
                    }
                    //int TotalCount = Convert.ToInt32(RoomResponse.Tables["root"].Rows[0]["TotalCount"]);
                    int TotalCount = Convert.ToInt32(Qtechres.TotalCount);
                    if (TotalCount == 0)
                    {
                        Item.ResponseType = "NoCount";
                        return options;
                    }

                    #region comment 
                    //GiataMasterClass giatamaster = new GiataMasterClass();
                    //switch (Provider.ToLower())
                    //{
                    //    case "mercan":
                    //        giatamaster = commonSearch.supplierGiataMaster.MercanGiataInfo;
                    //        break;
                    //    case "worldavenues":
                    //        giatamaster = commonSearch.supplierGiataMaster.WorldAvenuesGiataInfo;
                    //        break;
                    //    case "asianoverland":
                    //        giatamaster = commonSearch.supplierGiataMaster.AsianOverlandGiataInfo;
                    //        break;
                    //}
                    //DataRow[] drHotel = RoomResponse.Tables["HotelList"].Select();
                    #endregion

                    //for (int hh = 0; hh < RoomResponse.Tables["HotelList"].Rows.Count; hh++)
                    for (int hh = 0; hh < Qtechres.HotelList.Count; hh++)
                    {
                        Item.ResponseType = "Success";
                        //double FinalMinPrice = 0;
                        double FinalMinPrice = 1;
                        #region "When not Need Room"  
                        // add hotel code from price only and room code blank here
                        if (SearchParam.Filters.IsShowRooms == 0)
                        {
                            #region "Hotel Code Block "
                            #region comment
                            // DataRow[] drHotelList = RoomResponse.Tables["HotelList"].Select("HotelList_Id='" + hh + "'");
                            // string LocalHotelId = Convert.ToString(drHotelList[0]["LocalHotelId"]);
                            // string PropertyRating = Convert.ToString(drHotelList[0]["PropertyRating"]).Substring(0, 1);
                            #endregion
                            if (commonSearch.tokenData.AgentId != 1899)
                            {
                                FinalMinPrice = (Convert.ToDouble(Qtechres.HotelList[hh].TotalCharges)) * CurrencyFactor;
                            }
                            else
                            {
                                FinalMinPrice = 1;
                            }
                            DataRow drXmlHotel = DtHotel.NewRow();
                            drXmlHotel["XMLHotelCode"] = Convert.ToString(Qtechres.HotelList[hh].LocalHotelId);
                            //drXmlHotel["XMLHotelCode"] = Convert.ToString(RoomResponse.Tables["HotelList"].Rows[hh]["LocalHotelId"]);
                            //drXmlHotel["XMLCode"] = Convert.ToString(RoomResponse.Tables["root"].Rows[0]["SearchUniqueId"]) + "$" + Convert.ToString(RoomResponse.Tables["HotelList"].Rows[hh]["HotelId"]);
                            drXmlHotel["XMLCode"] = Convert.ToString(Qtechres.SearchUniqueId[0]) + "$" + Convert.ToString(Qtechres.HotelList[hh].HotelId);
                            drXmlHotel["HotelName"] = "";
                            drXmlHotel["SupplierName"] = Provider;
                            drXmlHotel["OfficeID"] = 0;
                            drXmlHotel["ContractID"] = 0;
                            drXmlHotel["Rating"] = 0;
                            drXmlHotel["XmlCity"] = XMLCITY;
                            drXmlHotel["IsShowSupplier"] = false;
                            drXmlHotel["BookingStatus"] = "Available";
                            drXmlHotel["FinalSellingPrice"] = FinalMinPrice;
                            drXmlHotel["SellingPrice"] = FinalMinPrice;
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
                            #region Hotels

                            #region "Hotel Block here"
                            #region comment
                            //DataRow[] drHotelList = RoomResponse.Tables["HotelList"].Select("HotelList_Id='" + hh + "'");
                            // string LocalHotelId = Convert.ToString(drHotelList[0]["LocalHotelId"]);
                            // string PropertyRating = Convert.ToString(drHotelList[0]["PropertyRating"]).Substring(0, 1);
                            // double TotalCharges = (Convert.ToDouble(drHotelList[0]["TotalCharges"])) * CurrencyFactor;
                            #endregion

                            DataRow drXmlHotel = DtHotel.NewRow();
                            //drXmlHotel["XMLHotelCode"] = Convert.ToString(RoomResponse.Tables["HotelList"].Rows[hh]["LocalHotelId"]);
                            drXmlHotel["XMLHotelCode"] = Convert.ToString(Qtechres.HotelList[hh].LocalHotelId);
                            drXmlHotel["XMLCode"] = Convert.ToString(Qtechres.SearchUniqueId[0]) + "$" + Convert.ToString(Qtechres.HotelList[hh].HotelId);
                            //drXmlHotel["XMLCode"] = Convert.ToString(RoomResponse.Tables["root"].Rows[0]["SearchUniqueId"]) + "$" + Convert.ToString(RoomResponse.Tables["HotelList"].Rows[hh]["HotelId"]);
                            drXmlHotel["HotelName"] = "";
                            drXmlHotel["SupplierName"] = Provider;
                            drXmlHotel["OfficeID"] = 0;
                            drXmlHotel["ContractID"] = 0;
                            drXmlHotel["Rating"] = 0;
                            drXmlHotel["XmlCity"] = XMLCITY;
                            drXmlHotel["IsShowSupplier"] = false;
                            drXmlHotel["BookingStatus"] = "Available";
                            drXmlHotel["FinalSellingPrice"] = FinalMinPrice;
                            drXmlHotel["SellingPrice"] = FinalMinPrice;
                            DtHotel.Rows.Add(drXmlHotel);

                            #endregion
                            #region "GetRooms for this hotel"
                            bool willAddOrNot = true;
                            Int64 RaynaID = 0;
                            Int64 GiataId = 0;
                            int RatingHotel = 0;
                            Int64 OwnsystemHotelId = 0;
                            // string Provider = "dotw";
                            double MinPrice = 1;

                            _markup.GetHotelDataCommon(ref MinPrice, ref willAddOrNot, ref OwnsystemHotelId, ref RaynaID, ref GiataId, ref RatingHotel, ref drXmlHotel, commonSearch.AgentId, commonSearch.TariffID, commonSearch.dsAgentDetails, commonSearch.supplierGiataMaster, SearchParam, commonSearch.dtCountryCityMarkup, commonSearch.B2CMarkup, commonSearch.IsParentId, commonSearch.SubAgentMarkup, commonSearch.SearchByHotel, commonSearch.HotelIDS);
                            if (willAddOrNot)
                            {
                                DataTable dtHotelRoom = _commonMethod.GenerateRoomtypeDataTableNewV2();
                                DataTable dtMercanCancellation = _commonMethod.GenerateCancellationDataTableNewV2();
                                //DataTable dtCancellation = _commonMethod.GenerateCancellationDataTableNewV2();
                                DataTable DtRoomRateBreackUp = _commonMethod.GenerateoomRateBreackUpDataTableNewV2();
                                int CutOffDays = -1; //XMLHotelBookingCommon.CutOffDays;
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
                                    _markup.getAgetMarkupSetting(Provider, GiataId.ToString(), commonSearch.dtSuplierMarkup, commonSearch.dtMarkupDetails, ref AgentPercentage, ref AgentMarkupAmount, ref blnIsShowSupplier, ref isPerce, commonSearch.AgentId, RatingHotel, SearchParam.CityID, SearchParam.CountryID.ToString(), SearchParam.CheckInDate);
                                    int maxOcupancy = 0;
                                    for (int i = 0; i < SearchParam.Rooms.Count; i++)
                                        maxOcupancy = maxOcupancy + SearchParam.Rooms[i].NoofChild + SearchParam.Rooms[i].NoofAdults;
                                    #region Supplier Room Code block
                                    #region DataRows
                                    //DataRow[] drroot = RoomResponse.Tables["root"].Select();
                                    //DataRow[] drroot = Qtechres;
                                    //int root_Id = Convert.ToInt32(drroot[0]["root_Id"]);
                                    string HotelId = Convert.ToString(Qtechres.HotelList[hh].HotelId);
                                    //string HotelId = Convert.ToString(RoomResponse.Tables["HotelList"].Rows[hh]["HotelId"]);




                                    #endregion
                                    int NoOfRoom = SearchParam.Rooms.Count();
                                    string checkin = (SearchParam.CheckInDate).Substring(6, 4) + "-" + (SearchParam.CheckInDate).Substring(3, 2) + "-" + (SearchParam.CheckInDate).Substring(0, 2);
                                    string checkout = (SearchParam.CheckOutDate).Substring(6, 4) + "-" + (SearchParam.CheckOutDate).Substring(3, 2) + "-" + (SearchParam.CheckOutDate).Substring(0, 2);
                                    string CurrencyCode = "";
                                    //CurrencyCode = Common.GetQTechCurrency(Provider);
                                    //CurrencyCode = GetQTechCurrency(Provider);
                                    CurrencyCode = Currency;

                                    // DataRow[] drSectionSelection = RoomResponse.Tables["HotelProperty"].Select("HotelList_Id='" + RoomResponse.Tables["HotelList"].Rows[hh]["HotelList_Id"] + "'");
                                    if (Qtechres.HotelList[hh].HotelProperty != null)
                                    {
                                        #region Rooms Loop
                                        for (int i = 0; i < Qtechres.HotelList[hh].HotelProperty.Count; i++)
                                        {
                                            #region RateBreakUp
                                            //double DisplayRoomRate = Convert.ToDouble(drSectionSelection[i]["DisplayRoomRate"]) * CurrencyFactor;
                                            double DisplayRoomRate = Convert.ToDouble(Qtechres.HotelList[hh].HotelProperty[i].DisplayRoomRate) * CurrencyFactor;
                                            //string Type = Convert.ToString(drSectionSelection[i]["Type"]);
                                            string Type = Convert.ToString(Qtechres.HotelList[hh].HotelProperty[i].Type);
                                            //string SectionUniqueId = Convert.ToString(drSectionSelection[i]["SectionUniqueId"]);
                                            string SectionUniqueId = Convert.ToString(Qtechres.HotelList[hh].HotelProperty[i].SectionUniqueId);
                                            //int HotelProperty_Id = Convert.ToInt32(drSectionSelection[i]["HotelProperty_Id"]);                                        

                                            string RoomType = "";
                                            string MealBasis = "";
                                            string RoomTypeName = "";
                                            string RoomType_Text = "";
                                            //string RoomTypeId = Convert.ToString(RoomResponse.Tables["HotelList"].Rows[hh]["HotelId"]) + "$" + Convert.ToString(RoomResponse.Tables["root"].Rows[0]["SearchUniqueId"]) + "$" + SectionUniqueId;// + "$" + HotelId + "$" + Convert.ToString(RoomResponse.Tables["root"].Rows[0]["SearchUniqueId"]) + "$" + Convert.ToString(RoomResponse.Tables["HotelList"].Rows[hh]["HotelId"]);
                                            string RoomTypeId = Convert.ToString(Qtechres.HotelList[hh].HotelId) + "$" + Convert.ToString(Qtechres.SearchUniqueId) + "$" + SectionUniqueId;// + "$" + HotelId + "$" + Convert.ToString(RoomResponse.Tables["root"].Rows[0]["SearchUniqueId"]) + "$" + Convert.ToString(RoomResponse.Tables["HotelList"].Rows[hh]["HotelId"]);
                                            int TotalAdult = 0;
                                            int TotalChild = 0;
                                            //DataRow[] drRoomRates = RoomResponse.Tables["RoomRates"].Select("HotelProperty_Id='" + HotelProperty_Id + "'");
                                            int Room = 0;
                                            string RoomClassId = "";

                                            if (Qtechres.HotelList[hh].HotelProperty[i].RoomRates != null)
                                            {
                                                //for (int jx = 0; jx < drRoomRates.Length; jx++)
                                                for (int jx = 0; jx < Qtechres.HotelList[hh].HotelProperty[i].RoomRates.Count; jx++)
                                                {
                                                    //Room = Room + Convert.ToInt32(drRoomRates[jx]["NumberOfRooms"]);
                                                    Room = Room + Convert.ToInt32(Qtechres.HotelList[hh].HotelProperty[i].RoomRates[jx].NumberOfRooms);
                                                }

                                                if (Room < NoOfRoom)
                                                    continue;
                                                //string RoomClassId = "";
                                                // if (drRoomRates.Length < NoOfRoom)
                                                //    continue;
                                                bool MealDiff = false;

                                                //for (int x = 0; x < drRoomRates.Length; x++)
                                                for (int x = 0; x < Qtechres.HotelList[hh].HotelProperty[i].RoomRates.Count; x++)
                                                {
                                                    string MB = "";
                                                    //MB = Convert.ToString(drRoomRates[x]["MealBasis"]);
                                                    MB = Convert.ToString(Qtechres.HotelList[hh].HotelProperty[i].RoomRates[x].MealBasis);
                                                    //if (MB != Convert.ToString(drRoomRates[0]["MealBasis"]))
                                                    if (MB != Convert.ToString(Qtechres.HotelList[hh].HotelProperty[i].RoomRates[0].MealBasis))
                                                        MealDiff = true;
                                                }
                                                if (MealDiff == true)
                                                    continue;

                                                //for (int j = 0; j < drRoomRates.Length; j++)
                                                for (int j = 0; j < Qtechres.HotelList[hh].HotelProperty[i].RoomRates.Count; j++)
                                                {



                                                    //int Available = Convert.ToInt32(drRoomRates[j]["Available"]);
                                                    int Available = Convert.ToInt32(Qtechres.HotelList[hh].HotelProperty[i].RoomRates[j].Available);
                                                    //int NumberOfRooms = Convert.ToInt32(drRoomRates[j]["NumberOfRooms"]);
                                                    int NumberOfRooms = Convert.ToInt32(Qtechres.HotelList[hh].HotelProperty[i].RoomRates[j].NumberOfRooms);

                                                    //for (int NoOfUnit = 0; NoOfUnit < NumberOfRooms; NoOfUnit++)
                                                    //{

                                                    int NumberOfAdults = (Convert.ToInt32(Qtechres.HotelList[hh].HotelProperty[i].RoomRates[j].NumberOfAdults));
                                                    //int NumberOfAdults = (Convert.ToInt32(drRoomRates[j]["NumberOfAdults"]));
                                                    int NumberOfChild = (Convert.ToInt32(Qtechres.HotelList[hh].HotelProperty[i].RoomRates[j].NumberOfChild));

                                                    //int NumberOfChild = (Convert.ToInt32(drRoomRates[j]["NumberOfChild"]));
                                                    double RoomRate = (Convert.ToDouble(Qtechres.HotelList[hh].HotelProperty[i].RoomRates[j].RoomRate)) * CurrencyFactor;
                                                    //double RoomRate = (Convert.ToDouble(drRoomRates[j]["RoomRate"])) * CurrencyFactor;
                                                    RoomType = Convert.ToString(Qtechres.HotelList[hh].HotelProperty[i].RoomRates[j].RoomType);
                                                    //RoomType = Convert.ToString(drRoomRates[j]["RoomType"]);
                                                    string Roomcategory = Convert.ToString(Qtechres.HotelList[hh].HotelProperty[i].RoomRates[j].RoomCategory);
                                                    //string Roomcategory = Convert.ToString(drRoomRates[j]["Roomcategory"]);
                                                    if (Provider.ToLower() == "asianoverland")
                                                    {
                                                        MealBasis = Convert.ToString(Qtechres.HotelList[hh].HotelProperty[i].RoomRates[j].MealBasis);
                                                        //MealBasis = Convert.ToString(drRoomRates[j]["MealBasis"]);
                                                    }
                                                    //MealBasis = Convert.ToString(drRoomRates[j]["MealBasis"]);
                                                    else
                                                    {
                                                        MealBasis = Convert.ToString(Qtechres.HotelList[hh].HotelProperty[i].RoomRates[j].MealCode);// due to Tickt#34939
                                                        //MealBasis = Convert.ToString(drRoomRates[j]["MealCode"]);// due to Tickt#34939
                                                    }
                                                    string ClassUniqueId = Convert.ToString(Qtechres.HotelList[hh].HotelProperty[i].RoomRates[j].ClassUniqueId);
                                                    //string ClassUniqueId = Convert.ToString(drRoomRates[j]["ClassUniqueId"]);
                                                    string RateTypeName = NumberOfAdults + " + " + RoomType;
                                                    for (int R = 0; R < NumberOfRooms; R++)
                                                    {
                                                        if (R == 0)
                                                            ClassUniqueId = ClassUniqueId + "|" + NumberOfAdults + "|" + NumberOfChild;

                                                        if (RoomClassId == "")
                                                            RoomClassId = RoomClassId + ClassUniqueId;
                                                        else
                                                            RoomClassId = RoomClassId + "$" + ClassUniqueId;
                                                    }
                                                    string RateType = "";
                                                    if (RoomType.Contains(":"))
                                                        RateType = RoomType;
                                                    //RateType = Roomcategory + RoomType.Split(':')[1];
                                                    else
                                                        RateType = Roomcategory + " " + NumberOfAdults + " Pax";

                                                    RateType = NumberOfRooms + " X " + RateType;
                                                    #region Room CombineLogic
                                                    List<Hotel.Room_Qtech> roomlist = new List<Hotel.Room_Qtech>();
                                                    //for (int l = 0; l < drRoomRates.Length; l++)
                                                    for (int l = 0; l < Qtechres.HotelList[hh].HotelProperty[i].RoomRates.Count; l++)
                                                    {
                                                        Hotel.Room_Qtech obj = new Hotel.Room_Qtech();
                                                        //obj.RoomCount = Convert.ToInt32(drRoomRates[l]["NumberOfRooms"]);
                                                        obj.RoomCount = Convert.ToInt32(Qtechres.HotelList[hh].HotelProperty[i].RoomRates[l].NumberOfRooms);
                                                        obj.RoomName = Convert.ToString(Qtechres.HotelList[hh].HotelProperty[i].RoomRates[l].RoomCategory);
                                                        //obj.RoomName = Convert.ToString(drRoomRates[l]["Roomcategory"]);
                                                        obj.status = true;
                                                        roomlist.Add(obj);
                                                    }

                                                    //List<Hotel.Hotel.Room_HotelBeds> roomlist = new List<Hotel.Hotel.Room_HotelBeds>();
                                                    //for (int l = 0; l < drRoomRates.Length; l++)
                                                    //{
                                                    //    Hotel.Hotel.Room_HotelBeds obj = new Hotel.Hotel.Room_HotelBeds();
                                                    //    obj.RoomCount = Convert.ToInt32(drRoomRates[l]["NumberOfRooms"]);
                                                    //    obj.RoomName = Convert.ToString(drRoomRates[l]["Roomcategory"]);
                                                    //    obj.status = true;
                                                    //    roomlist.Add(obj);
                                                    //}

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
                                                        if (RoomType_Text.IndexOf(roomlist[p].RoomName) < 0 || RoomType_Text == "")
                                                            RoomType_Text = RoomType_Text + roomlist[p].RoomCount + " X " + roomlist[p].RoomName + " ";
                                                    }
                                                    #endregion

                                                    TotalAdult = TotalAdult + NumberOfAdults;
                                                    TotalChild = TotalChild + NumberOfChild;
                                                    #region Comment
                                                    //double NightRate = 0;
                                                    //double VatOnRateBrackUp = 0;
                                                    //NightRate = Common.ProcessHotelRateBreakupPrice_WithMarkup(1, SearchParam.CityID, SearchParam.CountryId, RoomRate, B2CMarkup, IsParentId, isPerce, AgentPercentage, AgentMarkupAmount, SubAgentMarkup, ref VatOnRateBrackUp);
                                                    //NightRate = NightRate / SearchParam.NoofNights;

                                                    //for (int n = 0; n < SearchParam.NoofNights; n++)
                                                    //{
                                                    //    DateTime date = (Convert.ToDateTime(checkin).Date).AddDays(n);
                                                    //    DataRow drMercanRateBreackUp = DtQTechHotelRoomRateBreackUp.NewRow();
                                                    //    drMercanRateBreackUp["SupplierName"] = Provider;
                                                    //    drMercanRateBreackUp["OwnsystemHotelId"] = ownsystemhotelId;
                                                    //    drMercanRateBreackUp["ContractID"] = 0;
                                                    //    drMercanRateBreackUp["RoomNo"] = Convert.ToString("1");
                                                    //    drMercanRateBreackUp["RoomTypeId"] = RoomTypeId;
                                                    //    drMercanRateBreackUp["Meal"] = MealBasis;
                                                    //    string rateWeekColumn = Convert.ToString(date.DayOfWeek).Substring(0, 3);
                                                    //    drMercanRateBreackUp["DateOfWeek"] = rateWeekColumn;
                                                    //    drMercanRateBreackUp["MealId"] = MealBasis;
                                                    //    drMercanRateBreackUp["Rate"] = NightRate;
                                                    //    drMercanRateBreackUp["OrderNo"] = 1;
                                                    //    drMercanRateBreackUp["RateType"] = ((j + 1) + ". " + RateType); //"A Rate"
                                                    //    drMercanRateBreackUp["RoomTypeName"] = RoomType_Text;
                                                    //    drMercanRateBreackUp["DateOfWeek"] = rateWeekColumn;
                                                    //    drMercanRateBreackUp["Date"] = date;
                                                    //    drMercanRateBreackUp["BookingStatus"] = "Available";
                                                    //    drMercanRateBreackUp["BoardTypeID"] = "";
                                                    //    drMercanRateBreackUp["VatdiffAmount"] = VatOnRateBrackUp;
                                                    //    drMercanRateBreackUp["SupplierCurrency"] = Currency;
                                                    //    drMercanRateBreackUp["BuyingPrice"] = NightRate / CurrencyFactor;
                                                    //    DtQTechHotelRoomRateBreackUp.Rows.Add(drMercanRateBreackUp);
                                                    //}
                                                    //}
                                                    #endregion
                                                }
                                            }

                                            #endregion
                                            #region ROOM
                                            RoomTypeId = RoomTypeId + "###" + RoomClassId;
                                            double ItemPrice = DisplayRoomRate;
                                            DataRow drMercanHotelRoom = dtHotelRoom.NewRow();
                                            drMercanHotelRoom["OwnsystemHotelId"] = OwnsystemHotelId;
                                            drMercanHotelRoom["ContractID"] = 0;
                                            drMercanHotelRoom["UniqueNo"] = OwnsystemHotelId;
                                            drMercanHotelRoom["RoomSequenceNo"] = "";
                                            drMercanHotelRoom["MealName"] = MealBasis;
                                            drMercanHotelRoom["allocationDetails"] = "";
                                            drMercanHotelRoom["minStay"] = "";
                                            drMercanHotelRoom["DateApplyMinStay"] = "";
                                            drMercanHotelRoom["RoomNo"] = Convert.ToString("1");
                                            drMercanHotelRoom["EssentialInformation"] = "";
                                            drMercanHotelRoom["MealId"] = MealBasis;
                                            drMercanHotelRoom["RoomTypeName"] = RoomType_Text;
                                            drMercanHotelRoom["RoomTypeId"] = RoomTypeId;
                                            drMercanHotelRoom["BookingStatus"] = "Available";
                                            drMercanHotelRoom["AdultsOccupancy"] = TotalAdult;
                                            drMercanHotelRoom["ChildOccupancy"] = TotalChild;
                                            drMercanHotelRoom["MaxExtrabed"] = "";
                                            drMercanHotelRoom["RoomTypeCode"] = RoomTypeId;
                                            drMercanHotelRoom["BoardTypeID"] = "";
                                            drMercanHotelRoom["CompanyBuyingPrice"] = ItemPrice;
                                            drMercanHotelRoom["OfferDescription"] = "";
                                            drMercanHotelRoom["offerCode"] = "";
                                            drMercanHotelRoom["Type"] = RoomClassId;
                                            _markup.ProcessHotelRoom_WithMarkup(CurrencyFactor, CurrencyCode, true, true, SearchParam.NoofNights, SearchParam.CityID, SearchParam.CountryID.ToString(), SearchParam.CheckInDate, SearchParam.CheckOutDate, ref ItemPrice, ref drMercanHotelRoom, isPerce, AgentPercentage, AgentMarkupAmount, commonSearch.SubAgentMarkup, commonSearch.B2CMarkup, commonSearch.IsParentId);
                                            drMercanHotelRoom["SupplierName"] = Provider;
                                            dtHotelRoom.Rows.Add(drMercanHotelRoom);
                                            #endregion

                                            #region CancelltionPolicy

                                            #region Vat
                                            double CurrencyFactorQTech = CurrencyFactor; //Convert.ToDouble(dtInrCurrency.Rows[0]["AmtToExch"]);  //Note
                                            double VatOnPolicy = 0;
                                            double ChargeAmount = DisplayRoomRate;
                                            double MainAgentCharge = DisplayRoomRate; //AppliedAgentCharges;
                                            double CompanyChargeAmount = DisplayRoomRate; //AppliedAgentCharges;

                                            double cancellationAmountMain = DisplayRoomRate;
                                            ChargeAmount = _markup.ProcessHotelCancelationPrice_WithMarkup(ref cancellationAmountMain, SearchParam.NoofNights, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), ChargeAmount, isPerce, commonSearch.B2CMarkup, AgentPercentage, AgentMarkupAmount, commonSearch.IsParentId, commonSearch.SubAgentMarkup, ref VatOnPolicy);
                                            //ChargeAmount = _markup.ProcessHotelCancelationPrice_WithMarkup(ref MainAgentCharge, objSearPera.Rooms.Count * objSearPera.NoofNights, objSearPera.CityID, objSearPera.CountryID.ToString(), AppliedAgentCharges, isPerce, B2CMarkup, AgentPercentage, AgentMarkupAmount, IsParentId, SubAgentMarkup, ref VatOnPolicy);
                                            #endregion

                                            //DateTime CancellationDate = DateTime.ParseExact(CheckIn, "dd/MM/yyyy", CultureInfo.InvariantCulture).Date;
                                            //DateTime CancellationDate = DateTime.ParseExact(CheckIn, "dd/MM/yyyy", CultureInfo.InvariantCulture).Date;

                                            DateTime NowDate = DateTime.Now.Date;
                                            double CancelCharge = ChargeAmount;
                                            //CancellationDate = CancellationDate.AddHours(-CancellationHours);
                                            //CancellationDate = CancellationDate.AddDays(-1);

                                            //for (int p = 0; p < Qtechres.HotelList[hh].HotelProperty[i].RoomRates.Count; p++)
                                            {
                                                //ChargeAmount = (Convert.ToDouble(drRatePlanCancellationPolicyList[c]["Amount"]) * CurrencyFactorDida) * Rooms;
                                                //DateTime CancellationDate = Convert.ToDateTime(Qtechres.HotelList[hh].HotelProperty[i].Policies.CancellationPolicy[i].Start);
                                                //CancellationDate = CancellationDate.AddDays(-1);

                                                DateTime CancellationDate = DateTime.Now.Date;

                                                bool IsRef = Qtechres.HotelList[hh].HotelProperty[i].Refundable;
                                                if (IsRef)
                                                {
                                                    if (Qtechres.HotelList[hh].HotelProperty[i].Policies.CancellationPolicy != null)
                                                    {
                                                        CancellationDate = Convert.ToDateTime(Qtechres.HotelList[hh].HotelProperty[i].Policies.CancellationPolicy[0].Start);
                                                    }
                                                    //CancellationDate = Convert.ToDateTime(Qtechres.HotelList[hh].HotelProperty[i].Policies.CancellationPolicy[0].Start);
                                                    //if (CancellationDate.AddDays(CutOffDays).Date > DateTime.Now.Date)
                                                    CancellationDate = CancellationDate.AddDays(CutOffDays).Date;

                                                    if (NowDate.Date < CancellationDate.Date)
                                                    {
                                                        double charge = 0;
                                                        dtMercanCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref charge, dtMercanCancellation.NewRow(), CurrencyFactorQTech, Currency, Provider, Convert.ToString(OwnsystemHotelId), Convert.ToString(1),
                                                        Convert.ToString(RoomTypeId), RoomTypeName, Convert.ToString(MealBasis), DateTime.Now.Date, CancellationDate.AddDays(-1).AddHours(23).AddMinutes(59).AddSeconds(59), 0, 0, "", VatOnPolicy, "", "NO"));
                                                        dtMercanCancellation.AcceptChanges();

                                                        dtMercanCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref CompanyChargeAmount, dtMercanCancellation.NewRow(), CurrencyFactorQTech, Currency, Provider, Convert.ToString(OwnsystemHotelId), Convert.ToString(1),
                                                        Convert.ToString(RoomTypeId), RoomTypeName, Convert.ToString(MealBasis), CancellationDate.Date, DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null), CancelCharge, CancelCharge, "", VatOnPolicy, "", "YES"));
                                                        dtMercanCancellation.AcceptChanges();
                                                    }
                                                    else
                                                    {
                                                        DataRow drMercanHotelCancellation_Temp = dtMercanCancellation.NewRow();
                                                        dtMercanCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref CompanyChargeAmount, dtMercanCancellation.NewRow(), CurrencyFactorQTech, Currency, Provider, Convert.ToString(OwnsystemHotelId), Convert.ToString(1),
                                                        Convert.ToString(RoomTypeId), RoomTypeName, Convert.ToString(MealBasis), DateTime.Now.Date, DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null), CancelCharge, CancelCharge, "", VatOnPolicy, "", "YES"));
                                                        dtMercanCancellation.AcceptChanges();
                                                    }
                                                }
                                                else
                                                {
                                                    DataRow drMercanHotelCancellation_Temp = dtMercanCancellation.NewRow();
                                                    dtMercanCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref CompanyChargeAmount, dtMercanCancellation.NewRow(), CurrencyFactorQTech, Currency, Provider, Convert.ToString(OwnsystemHotelId), Convert.ToString(1),
                                                    Convert.ToString(RoomTypeId), RoomTypeName, Convert.ToString(MealBasis), DateTime.Now.Date, DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null), CancelCharge, CancelCharge, "", VatOnPolicy, "", "YES"));
                                                    dtMercanCancellation.AcceptChanges();
                                                }
                                            }
                                            #endregion
                                        } //
                                        #endregion
                                    }

                                    #endregion
                                }
                                #endregion
                                Options ops = (await _supplierRes.GetCommonRoomForSuppliers(GiataId, commonSearch.tokenData, commonSearch.advancedOptions, FinalMinPrice, drXmlHotel, dtHotelRoom, dtMercanCancellation, DtRoomRateBreackUp, SearchParam));
                                //Options ops = (await _supplierRes.GetCommonRoomForSuppliers(GiataId, commonSearch.tokenData, commonSearch.advancedOptions, FinalMinPrice, drXmlHotel, dtHotelRoom, dtCancellation, DtRoomRateBreackUp, SearchParam));
                                //Options ops = (await _supplierRes.GetCommonRoomForSuppliers(GiataId, commonSearch.tokenData, commonSearch.advancedOptions, MinPrice, drXmlHotel, dtHotelRoom, dtCancellation, DtRoomRateBreackUp, SearchParam));
                                if (ops != null)
                                    options.Add(ops);
                            }
                            #endregion
                            #endregion
                        }
                        #endregion
                    }
                }

                #region
                //DtHotel.DefaultView.RowFilter = "FinalSellingPrice>0";
                //dsTemp.Tables.Add(DtHotel.DefaultView.ToTable());
                //ExManager.Exception.LogException("error4");////////////////////
                #endregion
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
                    if (Item.ResponseType == "NoCount" && SaveNoCountSupplog == "1")
                    {
                        //Task.Run(() => FireAway_SQL_noEncoded(Item.RQRS[0].Request, Item.RQRS[0].Response == "" ? Item.ResponseError : Item.RQRS[0].Response, Provider, SearchParam.SearchKey, 1, SearchParam.AgentID));
                        //await Task.Run(async () => await _commonMethod.FireAway_SQL_noEncoded(Item.RQRS[0].Request, Item.RQRS[0].Response == "" ? Item.ResponseError : Item.RQRS[0].Response, Provider, SearchParam.SearchKey, 1, SearchParam.AgentID));
                    }
                }
                catch (Exception x)
                {
                    await _exception.Error(x);
                }
                #endregion
            }


        }
        public string GetQTechCurrency(string Provider)
        {
            string Currency = "";
            switch (Provider.ToLower())
            {
                case "mercan":
                    Currency = QtechInfo.MercanCurrency;
                    break;
                case "worldavenues":
                    Currency = QtechInfo.WorldAvenuesCurrency;
                    break;
                case "asianoverland":
                    Currency = QtechInfo.AsianOverLandCurrency;
                    break;
                case "nirvana":
                    Currency = QtechInfo.NirvanaCurrency;
                    break;
            }
            return Currency;
        }

        //recheck  - 300525
        public async Task<CancellationDto> GetQTechCancelationPolicy(DataSet dsHotel, string ownsystemid, string RoomTypeId_Own, DataTable dtAgentDetail, DataTable dtSuplierMarkup, DataTable dtMarkupDetails, DataTable dtSearchConfig, HotelSearchParameter objSearPera, string roomno, string HotelId, string Provider, int AgentId, string GiataId, int hotelRating, string RoomTypename, string mealId, string type, string SearchKey)
        {
            CancellationDto cancellationDto = new CancellationDto();
            try
            {

                string Cid = "";
                string Error = "";

                string[] MBArray = Convert.ToString(RoomTypeId_Own).Split(new String[] { "###" }, StringSplitOptions.None);

                string RoomTypeId = MBArray[0];

                string HotelCode = RoomTypeId.Split('$')[0];
                string unique_id = RoomTypeId.Split('$')[1];
                string section_unique_id = RoomTypeId.Split('$')[2];
                #region
                //DataRow[] drHotelCode = dsHotel.Tables[0].Select("XMLHotelCode='" + HotelId + "'");
                //string HotelCode = Convert.ToString(drHotelCode[0]["XMLCode"]).Split('$')[1];
                //string unique_id = Convert.ToString(drHotelCode[0]["XMLCode"]).Split('$')[0];
                //DataRow[] drRoom = dsHotel.Tables[1].Select("RoomTypeId='" + RoomTypeId_Own + "'");
                #endregion
                string RoomTypeName = RoomTypename;// Convert.ToString(drRoom[0]["RoomTypeName"]);
                string MealId = mealId;// Convert.ToString(drRoom[0]["MealId"]);
                Cid = type;// Convert.ToString(drRoom[0]["Type"]);
                DataTable dtMercanCancellation = _commonMethod.GenerateCancellationDataTableNewV2();

                #region
                // DataRow[] drRateBreakup = dsHotel.Tables[3].Select("RoomTypeId='" + RoomTypeId_Own + "'");

                //DataTable dtRB = drRateBreakup.CopyToDataTable();
                //DataView view = new DataView(dtRB);
                //DataTable distinctValues = view.ToTable(true, "RateType");
                // string FullRoomName = "";

                //for (int i = 0; i < distinctValues.Rows.Count; i++)
                //{
                //    if (i == 0)
                //        FullRoomName = FullRoomName + Convert.ToString(distinctValues.Rows[i]["RateType"]);
                //    else
                //        FullRoomName = FullRoomName + "</br>" + Convert.ToString(distinctValues.Rows[i]["RateType"]);
                //}
                #endregion

                string FullRoomName = "A Rate"; // add by krunal 

                FullRoomName = "<b>RoomType<b></br>" + FullRoomName;

                #region Cancellation Policy RQ & RES
                Boolean isPerce = true;
                double AgentCompanyPercentage = Convert.ToDouble(objSearPera.AgentCompanyPercentage);
                bool IsB2C = Convert.ToBoolean(Convert.ToByte(objSearPera.IsB2BOrB2C));
                double B2CMarkup = (IsB2C == false ? AgentCompanyPercentage : XMLHotelBookingCommon.B2CMarkup);
                double AgentPercentage = 0.0;
                double AgentMarkupAmount = 0.0;
                double SubAgentMarkup = 0;
                Boolean blnIsShowSupplier = false;
                int IsParentId = 1;
                if (dtAgentDetail != null)
                {
                    IsParentId = Convert.ToInt32(dtAgentDetail.Rows[0]["IsParentId"]);
                    SubAgentMarkup = Convert.ToDouble(dtAgentDetail.Rows[0]["B2CMarkupPer"]);
                }
                _markup.getAgetMarkupSetting(Provider, GiataId, dtSuplierMarkup, dtMarkupDetails, ref AgentPercentage, ref AgentMarkupAmount, ref blnIsShowSupplier, ref isPerce, AgentId, hotelRating, objSearPera.CityID, objSearPera.CountryID.ToString(), objSearPera.CheckInDate);
                int CutOffDays = XMLHotelBookingCommon.CutOffDays;
                CutOffDays = _commonMethod.GetCutOffDays(dtSuplierMarkup, Provider);
                DataRow[] drSuplierMarkup = (dtSuplierMarkup.Select("SupplierName='" + Provider + "'"));
                string username = Convert.ToString(drSuplierMarkup[0]["UserName"]);
                string password = Convert.ToString(drSuplierMarkup[0]["Password"]);

                string Currency = Convert.ToString(drSuplierMarkup[0]["CurrencyShortName"]);//Added on 30-05-25
                //string Currency = Common.GetQTechCurrency(Provider);//30-05-25
                DataTable dtInrCurrency = _commonMethod.GetCountryCurrency(Currency);
                double CurrencyFactorQTech = Convert.ToDouble(dtInrCurrency.Rows[0]["AmtToExch"]);

                string CheckIn = objSearPera.CheckInDate;
                string CheckOut = objSearPera.CheckOutDate;
                string Url = Convert.ToString(drSuplierMarkup[0]["WebSiteURL"]);
                int SID = Convert.ToInt32(drSuplierMarkup[0]["SupplierID"]);
                DataRow[] drSearchConfig = (dtSearchConfig.Select("SupplierID='" + SID + "'"));
                int SearchTimeOut = Convert.ToInt32(drSearchConfig[0]["HotelSearchTimeOut"]);
                string CancellationPolicyRQ = "";

                StringBuilder Sb_CanPolicyRQ = new StringBuilder();
                Sb_CanPolicyRQ.Append("" + Url + QtechInfo.Mercan_CancelltionPolicyAction);
                Sb_CanPolicyRQ.Append("&username=" + username + "");
                Sb_CanPolicyRQ.Append("&password=" + password + "");
                Sb_CanPolicyRQ.Append("&hotel_id=" + HotelCode + "");
                Sb_CanPolicyRQ.Append("&unique_id=" + unique_id + "");
                Sb_CanPolicyRQ.Append("&section_unique_id=" + section_unique_id + "");
                Sb_CanPolicyRQ.Append("&gzip=yes");
                CancellationPolicyRQ = Sb_CanPolicyRQ.ToString();

                //Common.ThirdPartyGetPolicyHistory(Convert.ToString(RoomTypeId_Own), Convert.ToString(unique_id), CancellationPolicyRQ, "-", "Before Get Policy", Provider, "Response", "-", "");

                //string ResponseData = GetQTechResponse(ref Error, CancellationPolicyRQ, 10000);
                ResponseDto responseDto = await GetQTechResponse(CancellationPolicyRQ, 10000);
                string ResponseData = responseDto.Response;
                Error = responseDto.Error;

                _commonMethod.ThirdPartyGetPolicyHistory(Convert.ToString(RoomTypeId_Own), Convert.ToString(unique_id), CancellationPolicyRQ, ResponseData + Error, "After Get Policy", Provider, "Response", objSearPera.SearchKey, "");

                ResponseData = Newtonsoft.Json.JsonConvert.DeserializeXmlNode(ResponseData, "root").InnerXml;
                #endregion
                DataSet dsResponse = new DataSet();
                dsResponse = _commonMethod.GetDataSetFromXMLString(ResponseData);

                DataRow[] drRoot = dsResponse.Tables["root"].Select();
                string Message = Convert.ToString(drRoot[0]["Message"]);
                double TotalBookingAmount = Convert.ToDouble(drRoot[0]["TotalBookingAmount"]);
                string ContractComment = Convert.ToString(drRoot[0]["ContractComment"]);
                double CancellationHours = Convert.ToInt32(drRoot[0]["CancellationHours"]);
                double CancallationDay = CancellationHours / 24;
                string CancallationDays = Convert.ToString(CancallationDay);
                double CancelDAY = 0;
                if (CancallationDays.Contains("."))
                {
                    CancallationDays = CancallationDays.Split('.')[0];
                    CancelDAY = Convert.ToDouble(CancallationDays) + 1;
                }
                else
                {
                    CancelDAY = Convert.ToDouble(CancallationDays);
                }
                double AppliedAgentCharges = Convert.ToDouble(drRoot[0]["AppliedAgentCharges"]) * CurrencyFactorQTech;

                DataRow[] drBookingAllowedInfo = dsResponse.Tables["BookingAllowedInfo"].Select();
                string BookingAllowed = Convert.ToString(drBookingAllowedInfo[0]["BookingAllowed"]);
                string MessageInfo = Convert.ToString(drBookingAllowedInfo[0]["MessageInfo"]);
                if (MessageInfo.Contains("credit limit"))
                {
                    MessageInfo = "Booking Not Allowed";
                }
                string StatusMessage = Convert.ToString(drBookingAllowedInfo[0]["Message"]);
                string SoldOut = Convert.ToString(drBookingAllowedInfo[0]["SoldOut"]);
                StringBuilder SbSPUI = new StringBuilder();
                SbSPUI.Append("<span style=\"font-size:larger; font-weight:bold; color:steelblue\">");
                SbSPUI.Append("BookingAllowed: ");
                if (BookingAllowed.ToLower() == "yes")
                    SbSPUI.Append("<b style=\"color:#8BCA1E\">");
                else
                    SbSPUI.Append("<b style=\"color:Red\">");
                SbSPUI.Append("" + BookingAllowed + "</b>");
                SbSPUI.Append("</br>");

                SbSPUI.Append("SoldOut: ");
                if (SoldOut.ToLower() == "no")
                    SbSPUI.Append("<b style=\"color:#8BCA1E\">");
                else
                    SbSPUI.Append("<b style=\"color:Red\">");
                SbSPUI.Append("" + SoldOut + "</b>");
                SbSPUI.Append("</br>");

                SbSPUI.Append("Message Info: ");
                if (BookingAllowed.ToLower() == "yes")
                    SbSPUI.Append("<b style=\"color:#2BB8E1\">");
                else
                    SbSPUI.Append("<b style=\"color:Red\">");
                SbSPUI.Append("" + MessageInfo + "</b>");
                if (ContractComment == "")
                    SbSPUI.Append("</span>");
                else
                {
                    SbSPUI.Append("</br></br>");
                    SbSPUI.Append("Note: ");
                    SbSPUI.Append("</span>");
                    SbSPUI.Append(ContractComment);
                }


                //if (FullRoomName.Contains("A Rate"))
                //{
                //    FullRoomName = "<b>RoomType<b></br>" + RoomTypeName;
                //}
                string SPUI = SbSPUI.ToString();// + "</br></br>" + FullRoomName;

                #region Caculation
                #region Vat
                double VatOnPolicy = 0;
                double MainAgentCharge = AppliedAgentCharges;
                double CompanyChargeAmount = AppliedAgentCharges;
                //AppliedAgentCharges = _markup.ProcessHotelCancelationPrice_WithMarkup(ref MainAgentCharge, objSearPera.Rooms.Count * objSearPera.NoofNights, objSearPera.CityID, objSearPera.CountryID.ToString(), AppliedAgentCharges, isPerce, B2CMarkup, AgentPercentage, AgentMarkupAmount, IsParentId, SubAgentMarkup, ref VatOnPolicy);
                AppliedAgentCharges = _markup.ProcessHotelCancelationPrice_WithMarkup(ref MainAgentCharge, objSearPera.NoofNights, objSearPera.CityID, objSearPera.CountryID.ToString(), AppliedAgentCharges, isPerce, B2CMarkup, AgentPercentage, AgentMarkupAmount, IsParentId, SubAgentMarkup, ref VatOnPolicy);
                #endregion

                DateTime CancellationDate = DateTime.ParseExact(CheckIn, "dd/MM/yyyy", CultureInfo.InvariantCulture).Date;
                DateTime NowDate = DateTime.Now.Date;
                double CancelCharge = AppliedAgentCharges;
                //CancellationDate = CancellationDate.AddHours(-CancellationHours);
                CancellationDate = CancellationDate.AddDays(-CancelDAY);

                #endregion

                if (CancellationDate.AddDays(CutOffDays).Date > DateTime.Now.Date)
                    CancellationDate = CancellationDate.AddDays(CutOffDays).Date;
                if (NowDate.Date < CancellationDate.Date)
                {
                    double charge = 0;
                    dtMercanCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref charge, dtMercanCancellation.NewRow(), CurrencyFactorQTech, Currency, Provider, ownsystemid, Convert.ToString(1),
                    RoomTypeId_Own, RoomTypeName, MealId, DateTime.Now.Date, CancellationDate.AddDays(-1).AddHours(23).AddMinutes(59).AddSeconds(59), 0, 0, "", VatOnPolicy, "", "NO", Cid, SPUI, "", ""));
                    dtMercanCancellation.AcceptChanges();
                    dtMercanCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref CompanyChargeAmount, dtMercanCancellation.NewRow(), CurrencyFactorQTech, Currency, Provider, ownsystemid, Convert.ToString(1),
                    RoomTypeId_Own, RoomTypeName, MealId, CancellationDate.Date, DateTime.ParseExact(objSearPera.CheckInDate, "dd/MM/yyyy", null), CancelCharge, CancelCharge, "", VatOnPolicy, "", "NO", Cid, SPUI, "", ""));
                    dtMercanCancellation.AcceptChanges();
                }
                else
                {
                    DataRow drMercanHotelCancellation_Temp = dtMercanCancellation.NewRow();
                    dtMercanCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref CompanyChargeAmount, dtMercanCancellation.NewRow(), CurrencyFactorQTech, Currency, Provider, ownsystemid, Convert.ToString(1),
                    RoomTypeId_Own, RoomTypeName, MealId, DateTime.Now.Date, DateTime.ParseExact(objSearPera.CheckInDate, "dd/MM/yyyy", null), CancelCharge, CancelCharge, "", VatOnPolicy, "", "YES", Cid, SPUI, "", ""));
                    dtMercanCancellation.AcceptChanges();
                }

                dsHotel.Tables[2].Merge(dtMercanCancellation);
            }
            catch (Exception ex)
            {
                _ = _exception.ErrorNew(ex, _LogAPI.Recheck, SearchKey);

                cancellationDto.dsHotelData = null;
                cancellationDto.ISBooking = "NotAvailable";

                return cancellationDto;
            }
            cancellationDto.dsHotelData = dsHotel;
            cancellationDto.ISBooking = "Available";

            return cancellationDto;
        }

        public async Task<ResponseDto> GetQTechResponse(string Request, Int32 TimeOut = 120000, string Searchkey = "")
        {
            ResponseDto responseDto = new ResponseDto();
            string Error = "";
            string output = "";
            string s = "";
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(Request);
                request.ContentType = "application/json";
                request.Accept = "application/json";

                request.Timeout = TimeOut;
                HttpWebResponse Response = (HttpWebResponse)await request.GetResponseAsync();
                Stream responseStream = Response.GetResponseStream();
                responseStream = new GZipStream(responseStream, CompressionMode.Decompress);
                //Read the output
                StreamReader Reader = new StreamReader(responseStream, Encoding.UTF8);
                output = await Reader.ReadToEndAsync();
                responseStream.Close();
                Reader.Close();
                Response.Close();
            }
            catch (Exception ex)
            {
                Error = ex.Message;
                _ = _exception.Error(ex, Searchkey);

            }
            responseDto.Response = output;
            responseDto.Error = Error;

            return responseDto;
        }

        public async Task<PriceCheckQTDto> QTechPriceCheck(DataSet dsCartDetails, DataRow[] MercanDetails, int j, double FinalPrice, double CurrencyFactorQTech, string UniqueNo, string AgentUniqueNo, double ResPrice, int AgentId, string Searchkey = "")
        {
            PriceCheckQTDto priceCheckQTDto = new PriceCheckQTDto();



            string CancellationPolicyRQ = "";
            string RequestSPR = "-", ReponseSPR = "-";
            string time = "00:00";
            string responsetype = "Success";
            string Error = "";
            try
            {
                string[] MBArray = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomTypeId"]).Split(new String[] { "###" }, StringSplitOptions.None);
                string RoomTypeId = MBArray[0];
                string username = Convert.ToString(MercanDetails[0]["UserName"]);
                string password = Convert.ToString(MercanDetails[0]["Password"]);
                string Url = Convert.ToString(MercanDetails[0]["WebSiteURL"]);
                string HotelCode = RoomTypeId.Split('$')[0];
                string unique_id = RoomTypeId.Split('$')[1];
                string section_unique_id = RoomTypeId.Split('$')[2];

                StringBuilder Sb_CanPolicyRQ = new StringBuilder();
                Sb_CanPolicyRQ.Append("" + Url + QtechInfo.Mercan_CancelltionPolicyAction);
                Sb_CanPolicyRQ.Append("&username=" + username + "");
                Sb_CanPolicyRQ.Append("&password=" + password + "");
                Sb_CanPolicyRQ.Append("&hotel_id=" + HotelCode + "");
                Sb_CanPolicyRQ.Append("&unique_id=" + unique_id + "");
                Sb_CanPolicyRQ.Append("&section_unique_id=" + section_unique_id + "");
                Sb_CanPolicyRQ.Append("&gzip=yes");
                CancellationPolicyRQ = Sb_CanPolicyRQ.ToString();
                _commonMethod.ThirdPartyBookingHistory(Convert.ToString(UniqueNo), Convert.ToString(AgentUniqueNo), CancellationPolicyRQ, "-", "Before Block Request hit Price check", Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["SupplierName"]), "Request Confirm j=" + j + "", "-", "WithinEarth");
                ResponseDto responseDto = await GetQTechResponse(CancellationPolicyRQ, 120000, Searchkey);
                string ResponseData = responseDto.Response;
                Error = responseDto.Error;


                _commonMethod.ThirdPartyBookingHistory(Convert.ToString(UniqueNo), Convert.ToString(AgentUniqueNo), CancellationPolicyRQ, ResponseData + Error, "After Block Request hit Price check", Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["SupplierName"]), "Request Confirm j=" + j + "", "-", "WithinEarth");
                ResponseData = Newtonsoft.Json.JsonConvert.DeserializeXmlNode(ResponseData, "root").InnerXml;
                RequestSPR = CancellationPolicyRQ;
                ReponseSPR = ResponseData;

                DataSet dsResponse = new DataSet();
                dsResponse = _commonMethod.GetDataSetFromXMLString(ResponseData);
                if (dsResponse != null)
                {
                    if (dsResponse.Tables["root"] == null || dsResponse.Tables["root"].Columns.Contains("TotalBookingAmount") == false)
                    {
                        responsetype = "GeneralError";

                        priceCheckQTDto.Flag = 2;
                        priceCheckQTDto.FinalPrice = FinalPrice;
                        priceCheckQTDto.ResPrice = ResPrice;
                        return priceCheckQTDto;
                    }
                }
                DataRow[] drRoot = dsResponse.Tables["root"].Select();
                string Message = Convert.ToString(drRoot[0]["Message"]);
                double TotalBookingAmount = Convert.ToDouble(drRoot[0]["TotalBookingAmount"]);
                ResPrice = TotalBookingAmount;
                TotalBookingAmount = TotalBookingAmount * CurrencyFactorQTech;
                string ContractComment = Convert.ToString(drRoot[0]["ContractComment"]);
                int CancellationHours = Convert.ToInt32(drRoot[0]["CancellationHours"]);
                double AppliedAgentCharges = Convert.ToDouble(drRoot[0]["AppliedAgentCharges"]);

                DataRow[] drBookingAllowedInfo = dsResponse.Tables["BookingAllowedInfo"].Select();
                string BookingAllowed = Convert.ToString(drBookingAllowedInfo[0]["BookingAllowed"]);
                string MessageInfo = Convert.ToString(drBookingAllowedInfo[0]["MessageInfo"]);
                string StatusMessage = Convert.ToString(drBookingAllowedInfo[0]["Message"]);
                string SoldOut = Convert.ToString(drBookingAllowedInfo[0]["SoldOut"]);
                if (BookingAllowed.ToLower() == "no")
                {
                    responsetype = "GeneralError";

                    priceCheckQTDto.Flag = 2;
                    priceCheckQTDto.FinalPrice = FinalPrice;
                    priceCheckQTDto.ResPrice = ResPrice;
                    return priceCheckQTDto;
                }
                if (Math.Round(TotalBookingAmount, 2) > Math.Round(FinalPrice, 2))
                {
                    responsetype = "GeneralError";
                    ReponseSPR = "#Price Changed " + "#Main Price = " + FinalPrice + " #New Price = " + TotalBookingAmount + " #Response= " + ReponseSPR;
                    FinalPrice = TotalBookingAmount;

                    priceCheckQTDto.Flag = 1;
                    priceCheckQTDto.FinalPrice = FinalPrice;
                    priceCheckQTDto.ResPrice = ResPrice;
                    return priceCheckQTDto;
                }
                #region SupplierPR Price Check
                try
                {
                    string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["SupplierName"]), "Price Check", AgentId, time, responsetype, RequestSPR, ReponseSPR, Error);
                }
                catch (Exception e)
                {
                    _ = _exception.ErrorBooking(e, AgentId.ToString(), "", "");
                }
                #endregion
            }
            catch (Exception ex)
            {
                _ = _exception.Error(ex, Searchkey);

                #region SupplierPerformanceReport Catch
                try
                {
                    Error = ex.Message + ex.StackTrace.ToString();
                    responsetype = "GeneralError";
                    string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["SupplierName"]), "Price Check", AgentId, time, responsetype, RequestSPR, ReponseSPR, Error);
                }
                catch (Exception e)
                {
                    _ = _exception.ErrorBooking(e, AgentId.ToString(), "", "");
                }
                #endregion

                priceCheckQTDto.Flag = 2;
                priceCheckQTDto.FinalPrice = FinalPrice;
                priceCheckQTDto.ResPrice = ResPrice;
                return priceCheckQTDto;
            }
            priceCheckQTDto.Flag = 0;
            priceCheckQTDto.FinalPrice = FinalPrice;
            priceCheckQTDto.ResPrice = ResPrice;
            return priceCheckQTDto;
        }

        public string QTechBookingRequest(ref string Error, string agentcode, DataSet dsCartDetails, DataRow[] MercanDetails, int i, int j, double ResPrice, DataTable dtPassengers, ref string AGTRef, string SearchKey = "")
        {
            string CancellationPolicyRQ = "";
            string BookingDetail = string.Empty;

            try
            {
                string[] MBArray = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomTypeId"]).Split(new String[] { "###" }, StringSplitOptions.None);
                string RoomTypeId = MBArray[0];

                string username = Convert.ToString(MercanDetails[0]["UserName"]);
                string password = Convert.ToString(MercanDetails[0]["Password"]);
                string Url = Convert.ToString(MercanDetails[0]["WebSiteURL"]);
                string HotelCode = RoomTypeId.Split('$')[0];
                string unique_id = RoomTypeId.Split('$')[1];
                string section_unique_id = RoomTypeId.Split('$')[2];
                //string[] RoomClass_Id = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["purchaseToken"]).Split('$');
                string[] RoomClass_Id = MBArray[1].Split('$');
                string UserCartId = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["UserCartId"]);
                DataRow[] drHotelBookingDetail = dsCartDetails.Tables[1].Select("UserCartId='" + UserCartId + "'");
                AGTRef = "Api_" + agentcode + "_" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["UserCartDetailId"]);
                #region String Roomdetails
                StringBuilder SB_RoomDetails = new StringBuilder();
                for (int R = 0; R < drHotelBookingDetail.Length; R++)
                {
                    int noofAdult = Convert.ToInt32(drHotelBookingDetail[R]["NoOfAdults"]);
                    int noofChild = Convert.ToInt32(drHotelBookingDetail[R]["NoOfChild"]);
                    int RoomNumber = Convert.ToInt32(drHotelBookingDetail[R]["RoomNumber"]);
                    string roomClassId = ""; string split = ""; int splitAdult = 0; int splitChild = 0;
                    //for (int r = 0; r < RoomClass_Id.Length; r++) --29/05/24 #45716
                    {
                        split = RoomClass_Id[R].Split('|')[0]; //29/05/24 #45716
                        splitAdult = Convert.ToInt32(RoomClass_Id[R].Split('|')[1]); //29/05/24 #45716
                        splitChild = Convert.ToInt32(RoomClass_Id[R].Split('|')[2]); //29/05/24 #45716
                        if (splitAdult == noofAdult && splitChild == noofChild)
                        {
                            roomClassId = split;
                        }
                    }
                    #region Old
                    BookingDetail += "{\"" + "numberOfChilds" + "\":" + "\"" + noofChild + "\"" + "," + "\"" + "roomClassId" + "\":" + "\"" + RoomClass_Id + "\"" + "," + "\"" + "passangers" + "\":[";
                    DataRow[] dsAdult = dtPassengers.Select("PaxType=" + "'Adult'" + " and RoomNo=" + RoomNumber + " and UserCartId = '" + UserCartId + "'");
                    DataRow[] dsChild = dtPassengers.Select("PaxType=" + "'Child'" + " and RoomNo=" + RoomNumber + " and UserCartId = '" + UserCartId + "'");
                    for (int c = 0; c <= dsAdult.GetLength(0) - 1; c++)
                    {
                        BookingDetail += "{" + "\"" + "salutation" + "\":" + "\"" + Convert.ToString(dsAdult[c]["Prefix"]).Replace(".", "") + "\"" + "," + "\"" + "first_name" + "\":" + "\"" + Convert.ToString(dsAdult[c]["FirstName"]) + "\"" + "," + "\"" + "last_name" + "\":" + "\"" + Convert.ToString(dsAdult[c]["LastName"]) + "\"},";
                    }
                    for (int d = 0; d <= dsChild.GetLength(0) - 1; d++)
                    {
                        BookingDetail += "{" + "\"" + "salutation" + "\":" + "\"" + Convert.ToString(dsChild[d]["PaxType"]) + "\"" + "," + "\"" + "first_name" + "\":" + "\"" + Convert.ToString(dsChild[d]["FirstName"]) + "\"" + "," + "\"" + "last_name" + "\":" + "\"" + Convert.ToString(dsChild[d]["LastName"]) + "\"" + "," + "\"" + "age" + "\":" + "\"" + Convert.ToString(dsChild[d]["ChildAge"]) + "\"},";
                    }
                    BookingDetail = BookingDetail.Remove(BookingDetail.Length - 1, 1);
                    BookingDetail += "]},";

                    BookingDetail = BookingDetail.Remove(BookingDetail.Length - 1, 1);
                    BookingDetail += "]";
                    #endregion
                    DataRow[] dsAdults = dtPassengers.Select("PaxType=" + "'Adult'" + " and RoomNo=" + RoomNumber + " and UserCartId = '" + UserCartId + "'");
                    DataRow[] dsChilds = dtPassengers.Select("PaxType=" + "'Child'" + " and RoomNo=" + RoomNumber + " and UserCartId = '" + UserCartId + "'");

                    SB_RoomDetails.Append("{\"numberOfChilds\":\"" + noofChild + "\",\"roomClassId\":\"" + roomClassId + "\",");
                    SB_RoomDetails.Append("\"passangers\":[");
                    for (int adult = 0; adult < noofAdult; adult++)
                        SB_RoomDetails.Append("{\"salutation\":\"" + Convert.ToString(dsAdult[adult]["Prefix"]).Replace(".", "") + "\",\"first_name\":\"" + Convert.ToString(dsAdult[adult]["FirstName"]) + "\",\"last_name\":\"" + Convert.ToString(dsAdult[adult]["LastName"]) + "\"},");
                    //SB_RoomDetails.Remove(SB_RoomDetails.Length - 1, 1);
                    for (int child = 0; child < noofChild; child++)
                    {
                        SB_RoomDetails.Append("{\"salutation\":\"" + Convert.ToString(dsChild[child]["PaxType"]) + "\",\"first_name\":\"" + Convert.ToString(dsChild[child]["FirstName"]) + "\",\"last_name\":\"" + Convert.ToString(dsChild[child]["LastName"]) + "\",\"age\":\"" + Convert.ToString(dsChild[child]["ChildAge"]) + "\"},");
                        //SB_RoomDetails.Remove(SB_RoomDetails.Length - 1, 1);
                    }
                    SB_RoomDetails.Remove(SB_RoomDetails.Length - 1, 1);
                    SB_RoomDetails.Append("]},");
                }
                SB_RoomDetails.Remove(SB_RoomDetails.Length - 1, 1);
                #endregion

                StringBuilder Sb_CanPolicyRQ = new StringBuilder();
                Sb_CanPolicyRQ.Append("" + Url + QtechInfo.Mercan_BookingAction);
                Sb_CanPolicyRQ.Append("&username=" + username + "");
                Sb_CanPolicyRQ.Append("&password=" + password + "");
                Sb_CanPolicyRQ.Append("&unique_id=" + unique_id + "");
                Sb_CanPolicyRQ.Append("&hotel_id=" + HotelCode + "");
                Sb_CanPolicyRQ.Append("&section_unique_id=" + section_unique_id + "");
                Sb_CanPolicyRQ.Append("&expected_price=" + ResPrice + "");
                Sb_CanPolicyRQ.Append("&agent_ref_no=" + AGTRef + "");
                Sb_CanPolicyRQ.Append("&roomDetails=[" + SB_RoomDetails + "]");
                Sb_CanPolicyRQ.Append("&gzip=yes");
                CancellationPolicyRQ = Sb_CanPolicyRQ.ToString();
            }
            catch (Exception ex)
            {
                _ = _exception.ErrorBooking(ex, "", "QTechBookingRequest", SearchKey);
                _ = _exception.ErrorNew(ex, _LogAPI.Book, SearchKey);
                Error = ex.Message;
                return "";
            }
            return CancellationPolicyRQ;
        }

        public async Task<QtechBookRes> GetBookingDetail(string AGTRef, DataRow[] QTechDetails, string ReqBookingDetail)
        {
            QtechBookRes qtechBookRes = new QtechBookRes();
            try
            {
                string Res = "", Error = "";
                string url = Convert.ToString(QTechDetails[0]["WebSiteURL"]);
                string username = Convert.ToString(QTechDetails[0]["UserName"]);
                string password = Convert.ToString(QTechDetails[0]["Password"]);
                StringBuilder SBReq = new StringBuilder();
                SBReq.Append("" + url + "booking_detail&username=" + username + "&password=" + password + "&agent_ref_no=" + AGTRef + "&gzip=yes");

                ResponseDto responsedto = await GetQTechResponse(SBReq.ToString());
                string Responsebook = responsedto.Response;
                Error = responsedto.Error;

                Res = Responsebook;
                ReqBookingDetail = SBReq.ToString();

                qtechBookRes.ReqBookingDetail = ReqBookingDetail;
                qtechBookRes.Res = Res;


            }
            catch (Exception ex)
            {
                _ = _exception.ErrorNew(ex, _LogAPI.Book);

                qtechBookRes.ReqBookingDetail = "";
                qtechBookRes.Res = ex.Message;
            }

            return qtechBookRes;
        }


        public async Task<QtechCancelCharge> QTechGet_cancellation_charges(string username, string password, string bookingid, string ChargesRequest, string url)
        {
            QtechCancelCharge qtechCancelCharge = new QtechCancelCharge();
            string Request = "";
            string Error = "";
            try
            {
                string msg = ""; string Message = ""; string Status = ""; string AllowCancel = "";
                StringBuilder SbRequest = new StringBuilder();
                SbRequest.Append("" + url + QtechInfo.get_cancellation_charges);
                SbRequest.Append("&username=" + username + "&password=" + password + "&booking_id=" + bookingid + "&gzip=yes");
                Request = SbRequest.ToString();

                ResponseDto responseDto = await GetQTechResponse(Request);
                string Res = responseDto.Response;
                Error = responseDto.Error;
                ChargesRequest = Request + "$" + Res;

                qtechCancelCharge.ChargesRequest = ChargesRequest;


                string Response = Newtonsoft.Json.JsonConvert.DeserializeXmlNode(Res, "root").InnerXml;
                DataSet dsResponse = _commonMethod.GetDataSetFromXMLString(Response);
                if (dsResponse.Tables.Contains("root"))
                {
                    DataRow[] drHotel = dsResponse.Tables["root"].Select();
                    Message = Convert.ToString(drHotel[0]["Message"]);
                    if (Message.ToLower() == "success")
                        Status = Convert.ToString(drHotel[0]["Message"]);
                    if (Status.ToLower() == "success")
                        AllowCancel = Convert.ToString(drHotel[0]["Message"]);

                    qtechCancelCharge.AllowCancel = AllowCancel;

                }
                else
                {
                    return qtechCancelCharge;
                }

                return qtechCancelCharge;


            }
            catch (Exception ex)
            {
                _ = _exception.ErrorNew(ex, _LogAPI.CancelBooking);

                return qtechCancelCharge;
            }


        }

        public string QTechCancellationRQ(string username, string password, string BookingRef, string url)
        {
            string CancellationRQ = "";
            try
            {
                StringBuilder SBCancellationRQ = new StringBuilder();
                SBCancellationRQ.Append("" + url + "cancel_the_booking");
                SBCancellationRQ.Append("&username=" + username + "");
                SBCancellationRQ.Append("&password=" + password + "");
                SBCancellationRQ.Append("&booking_id=" + BookingRef + "");
                SBCancellationRQ.Append("&gzip=yes");
                CancellationRQ = SBCancellationRQ.ToString();
            }
            catch (Exception ex)
            {
                _ = _exception.ErrorNew(ex, _LogAPI.CancelBooking);

                return "";
            }
            return CancellationRQ;
        }
    }
}
