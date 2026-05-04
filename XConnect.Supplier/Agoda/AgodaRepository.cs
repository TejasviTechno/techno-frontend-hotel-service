using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using XConnect.Agoda;
using XConnect.CommonUtilities;
using XConnect.CommonUtilities.Interface;
using XConnect.ErrorLogHandler.Interface;
using XConnect.Expedia;
using XConnect.Markup.Interface;
using XConnect.Models;
using XConnect.Models.Hotel;
using XConnect.Supplier.Agoda;
using XConnect.Supplier.Agoda.Interface;
using static Azure.Core.HttpHeader;
using Options = XConnect.Models.Options;
using System.IO.Compression;
using static XConnect.Models.ErrorLog;


namespace Xconnect.Expedia
{
    public class AgodaRepository : IAgoda
    {
        private readonly ICommonMethod _commonMethod;
        private readonly IException _exception;
        private readonly IMarkup _markup;
        private readonly ISupplierRes _supplierRes;
        private readonly KeySettings _keySettings;

        public AgodaRepository(ICommonMethod commonMethod, IMarkup markup, ISupplierRes supplierRes, IOptions<KeySettings> keySettings, IException exception)
        {
            _commonMethod = commonMethod;
            _markup = markup;
            _supplierRes = supplierRes;
            _keySettings = keySettings.Value;
            _exception = exception;
        }

        public void GetAgoda_NewRequestOnly_async_New(bool SearchByOneID, CommonSearchList commonSearchList, List<StarRating> listRating, bool IsHotelWiseSearch, bool HitFromAll, HotelSearchParameter SearchPara, List<SupplierDetails> lstAgodaV1Details, List<SupplierSearchConfig> lstAgodaV1SearchConfig, DataTable dtPreferredDataHotel, ref ConcurrentBag<HotelX> AllTask, int Hit, DataTable dtROE, TokenData tokenData, DataSet dtXMLCity)
        {
            try
            {

                if (lstAgodaV1Details != null && lstAgodaV1Details.Count > 0)
                {
                    List<SupplierDetails> AgodaMain = new List<SupplierDetails>();
                    List<SupplierDetails> AgodaChild = new List<SupplierDetails>();

                    foreach (SupplierDetails item in lstAgodaV1Details)
                    {
                        if (false)
                            AgodaMain.Add(item);
                        else
                            AgodaChild.Add(item);
                    }

                    for (int x = 0; x < AgodaChild.Count; x++)
                    {
                        int SupplierID = AgodaChild[x].SupplierID;
                        string Supplier = AgodaChild[x].SupplierName;
                        DataRow[] commonCode = null;

                        #region Codes
                        if (Supplier.ToLower() == "agoda_chain")
                            commonCode = commonSearchList.commonHotelCodes.Agoda_Chian_Codes;
                        else if (Supplier.ToLower() == "agoda_v1")
                            commonCode = commonSearchList.commonHotelCodes.agoda_v1_Codes;
                        #endregion

                        if (commonCode != null)
                        {
                            if (lstAgodaV1SearchConfig.Count > 0)
                            {
                                List<SupplierSearchConfig> AgodaSearchConfig = lstAgodaV1SearchConfig
                                    .Where(x => x != null && x.SupplierID == SupplierID)
                                    .ToList();

                                bool HitSupplier = false;
                                DataForSearch dataForSearch = _commonMethod.GetXmlDataForSearchNew(SearchPara.Nationality, dtXMLCity, Supplier.ToLower());
                                switch (SearchPara.Nationality.ToLower())
                                {
                                    case "cuba":
                                    case "syria":
                                    case "sudan":
                                    case "iran":
                                    case "north korea":

                                        HitSupplier = false;
                                        break;
                                    default:
                                        HitSupplier = true;

                                        break;
                                }

                                if (HitSupplier && dataForSearch.XmlNationality != "" && commonCode != null)
                                {
                                    string AgodaNationality = "";
                                    if (dataForSearch.XmlNationality.ToLower() == "ru")//@Due to Ticket#35018
                                    {
                                        AgodaNationality = "ES";//span
                                    }
                                    else
                                    {
                                        AgodaNationality = dataForSearch.XmlNationality;
                                    }

                                    GetAgoda_NewRequestOnly_async_v1(SearchByOneID, commonCode, listRating, Supplier, IsHotelWiseSearch, HitFromAll, SearchPara, AgodaChild[x], AgodaSearchConfig, dtPreferredDataHotel, ref AllTask, dataForSearch.xmlCity, AgodaNationality, Hit, dtROE, tokenData);

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

        //130325
        public void GetAgoda_NewRequestOnly_async_New_v1(bool SearchByOneID, DataRow[] commonCode, List<StarRating> listRating, bool IsHotelWiseSearch, bool HitFromAll, HotelSearchParameter SearchPara, List<SupplierDetails> lstAgodaV1Details, List<SupplierSearchConfig> lstAgodaV1SearchConfig, DataTable dtPreferredDataHotel, ref ConcurrentBag<HotelX> AllTask, int Hit, DataTable dtROE, TokenData tokenData, DataSet dtXMLCity)
        {
            try
            {

                if (lstAgodaV1Details != null && lstAgodaV1Details.Count > 0)
                {
                    //List<SupplierDetails> AgodaMain = new List<SupplierDetails>();
                    //List<SupplierDetails> AgodaChild = new List<SupplierDetails>();
                    List<StarRating> lstRating = new List<StarRating>();
                    lstRating = _commonMethod.GetStarRatingStringList_xtg(SearchPara);

                    List<HotelCodeByCity> DtHotelDAta = HtmlExtensions.ToList<HotelCodeByCity>(commonCode.CopyToDataTable());
                    int ratingcount = lstRating.Count;
                    int hotelcount = DtHotelDAta.Count;

                    foreach (SupplierDetails AgodaChild in lstAgodaV1Details)
                    {
                        int SupplierID = AgodaChild.SupplierID;
                        string Supplier = AgodaChild.SupplierName;

                        if (lstAgodaV1SearchConfig.Count > 0)
                        {
                            List<SupplierSearchConfig> AgodaSearchConfig = lstAgodaV1SearchConfig
                                .Where(x => x != null && x.SupplierID == SupplierID)
                                .ToList();

                            if (AgodaSearchConfig.Count > 0)
                            {

                                List<HotelCodeByCity> filter = new List<HotelCodeByCity>();
                                filter = _commonMethod.FilterWithOutCity_Code(ratingcount, hotelcount, DtHotelDAta, lstRating, Supplier);

                                if (filter.Count > 0)
                                {
                                    bool HitSupplier = false;
                                    DataForSearch dataForSearch = _commonMethod.GetXmlDataForSearchTechwise(SearchPara.Nationality, dtXMLCity, Supplier.ToLower(), AgodaChild.TechName);
                                    switch (SearchPara.Nationality.ToLower())
                                    {
                                        case "cuba":
                                        case "syria":
                                        case "sudan":
                                        case "iran":
                                        case "north korea":

                                            HitSupplier = false;
                                            break;
                                        default:
                                            HitSupplier = true;

                                            break;
                                    }

                                    if (HitSupplier && dataForSearch.XmlNationality != "" && commonCode != null)
                                    {
                                        string AgodaNationality = "";
                                        if (dataForSearch.XmlNationality.ToLower() == "ru")//@Due to Ticket#35018
                                        {
                                            AgodaNationality = "ES";//span
                                        }
                                        else
                                        {
                                            AgodaNationality = dataForSearch.XmlNationality;
                                        }

                                        GetAgoda_NewRequestOnly_async_v1_new(SearchByOneID, filter, listRating, Supplier, IsHotelWiseSearch, HitFromAll, SearchPara, AgodaChild, AgodaSearchConfig, dtPreferredDataHotel, ref AllTask, dataForSearch.xmlCity, AgodaNationality, Hit, dtROE, tokenData);

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

        //130325
        public void GetAgoda_NewRequestOnly_async_v1_new(bool SearchByOneID, List<HotelCodeByCity> commonCode, List<StarRating> listRating, string Supplier, bool IsHotelWiseSearch, bool HitFromAll, HotelSearchParameter SearchPara, SupplierDetails AgodaV1Details, List<SupplierSearchConfig> AgodaV1SearchConfig, DataTable dtPreferredDataHotel, ref ConcurrentBag<HotelX> AllTask, string Xmlcity, string XmlNationality, int Hit, DataTable dtROE, TokenData tokenData)
        {
            try
            {
                #region Agoda
                if ((SearchPara.IsSearchFor.ToLower().IndexOf(Supplier.ToLower()) >= 0 || SearchPara.IsSearchFor.ToLower().IndexOf("multi") >= 0) && HitFromAll)
                {
                    string Ids = string.Empty;
                    string xmlbase = "";
                    _commonMethod.GetPrefferedHotelCodes(ref Ids, ref xmlbase, ref AgodaV1SearchConfig, Supplier, dtPreferredDataHotel, SearchPara.IsSearchFor);
                    if (AgodaV1SearchConfig != null)
                    {
                        //string Currency = AgodaInfo.AgodaNewCurrency;
                        string Currency = Convert.ToString(AgodaV1Details.CurrencyShortName);

                        double CurrencyFactorAgoda = 1;
                        if (Currency.ToLower() != tokenData.SiteCurrency.ToLower())
                        {
                            //DataTable dtInrCurrency = Common.GetCountryCurrency(Currency);
                            CurrencyFactorAgoda = _commonMethod.GetROEByCurrency(Currency, dtROE);// Convert.ToDouble(dtInrCurrency.Rows[0]["AmtToExch"]);
                        }

                        {
                            for (int k = 0; k < AgodaV1SearchConfig.Count; k++)
                            {
                                bool DebugMode = (Convert.ToInt32(AgodaV1SearchConfig[k].DebugMode) == 0 ? false : true);
                                {
                                    //List<HotelCodeByCity> DtHotelDAta = FilterFinalList(commonCode, listRating);
                                    if (commonCode.Count > 0)
                                    {
                                        if (Convert.ToInt32(AgodaV1SearchConfig[k].CityID) == 0 || Convert.ToInt32(AgodaV1SearchConfig[k].CityID) == SearchPara.CityID)//All City means 0
                                        {
                                            {
                                                int NoOfRecord = Convert.ToInt32(AgodaV1SearchConfig[k].Noofrecords);
                                                double count = Convert.ToDouble(commonCode.Count);
                                                int NoOfDiv = Convert.ToInt32(Math.Ceiling(count / Convert.ToDouble(NoOfRecord)));
                                                List<List<XMLID_Search>> lstxml = _commonMethod.GetXMLIDString(NoOfDiv, NoOfRecord, count, commonCode);
                                                for (int a = 0; a < NoOfDiv; a++)
                                                //for (int a = 0; a < 30; a++)
                                                {
                                                    //var paramValues1 = String.Join(",", splittedtables[a].Select().Select(x => String.Join(",", Convert.ToString(x.ItemArray[0]))));
                                                    //string IdsForSearch = Convert.ToString(paramValues1);

                                                    string IdsForSearch = Convert.ToString(_commonMethod.GetHotelCodeByCOmma(lstxml[a]));


                                                    HotelX xhotel = new HotelX();
                                                    xhotel.Supplier = Supplier;
                                                    xhotel.DebugeMode = DebugMode;
                                                    xhotel.SearchTimeOut = Convert.ToInt32(AgodaV1SearchConfig[k].HotelSearchTimeOut);
                                                    xhotel.ProcessTimeout = Convert.ToInt32(AgodaV1SearchConfig[k].HotelSearchTimeOut2);
                                                    //xhotel.URL = Convert.ToString(AgodaInfo.AgodaNewHotelsearchUrl);
                                                    if (_keySettings.SupplierProxy == "0")
                                                        xhotel.URL = Convert.ToString(AgodaInfo.AgodaNewHotelsearchUrl);
                                                    else
                                                        xhotel.URL = Convert.ToString(AgodaV1Details.WebSiteURLforAPI);


                                                    xhotel.UserId = Convert.ToString(AgodaV1Details.UserID);
                                                    xhotel.UserName = Convert.ToString(AgodaV1Details.UserName);
                                                    xhotel.PassWord = Convert.ToString(AgodaV1Details.Password);
                                                    xhotel.CurrencyCode = Currency;
                                                    xhotel.CurrencyFactor = CurrencyFactorAgoda;
                                                    xhotel.XmlNationality = XmlNationality;
                                                    xhotel.XmlCity = Xmlcity;
                                                    xhotel.XmlCountry = "";
                                                    xhotel.AgentWiseMarkup = AgodaV1Details.AgentWiseMarkup;
                                                    xhotel.IsShowSupplier = Convert.ToInt16(AgodaV1Details.IsShowSupplier) == 1 ? true : false;
                                                    xhotel.SupplierID = AgodaV1Details.SupplierID;
                                                    //xhotel.MasterSupplierID = AgodaV1Details[0].MasterSupplierId;
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
                                                        objrq.Request = XML_Agoda_New_RequestOnly_async(SearchByOneID, "1", xhotel.URL, xhotel.UserId, xhotel.PassWord, xhotel.SearchTimeOut, SearchPara, IdsForSearch, xhotel.Supplier, CurrencyFactorAgoda, DebugMode, Xmlcity, XmlNationality, xhotel.CurrencyCode);
                                                        objrq.Response = ""; RQRS.Add(objrq);
                                                    }
                                                    xhotel.RQRS = RQRS;
                                                    xhotel.ReturnRooms = AgodaV1Details.ReturnRooms; //140225
                                                    xhotel.TechName = AgodaV1Details.TechName; ;//140225
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
                #endregion
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
            }
        }
        public void GetAgoda_NewRequestOnly_async_v1(bool SearchByOneID, DataRow[] commonCode, List<StarRating> listRating, string Supplier, bool IsHotelWiseSearch, bool HitFromAll, HotelSearchParameter SearchPara, SupplierDetails AgodaV1Details, List<SupplierSearchConfig> AgodaV1SearchConfig, DataTable dtPreferredDataHotel, ref ConcurrentBag<HotelX> AllTask, string Xmlcity, string XmlNationality, int Hit, DataTable dtROE, TokenData tokenData)
        {
            try
            {
                #region Agoda
                if ((SearchPara.IsSearchFor.ToLower().IndexOf(Supplier.ToLower()) >= 0 || SearchPara.IsSearchFor.ToLower().IndexOf("multi") >= 0) && HitFromAll)
                {
                    string Ids = string.Empty;
                    string xmlbase = "";
                    _commonMethod.GetPrefferedHotelCodes(ref Ids, ref xmlbase, ref AgodaV1SearchConfig, Supplier, dtPreferredDataHotel, SearchPara.IsSearchFor);
                    if (AgodaV1SearchConfig != null)
                    {
                        //string Currency = AgodaInfo.AgodaNewCurrency;
                        string Currency = Convert.ToString(AgodaV1Details.CurrencyShortName);

                        double CurrencyFactorAgoda = 1;
                        if (Currency.ToLower() != tokenData.SiteCurrency.ToLower())
                        {
                            //DataTable dtInrCurrency = Common.GetCountryCurrency(Currency);
                            CurrencyFactorAgoda = _commonMethod.GetROEByCurrency(Currency, dtROE);// Convert.ToDouble(dtInrCurrency.Rows[0]["AmtToExch"]);
                        }

                        {
                            for (int k = 0; k < AgodaV1SearchConfig.Count; k++)
                            {
                                bool DebugMode = (Convert.ToInt32(AgodaV1SearchConfig[k].DebugMode) == 0 ? false : true);
                                {
                                    //List<HotelCodeByCity> DtHotelDAta = FilterFinalList(commonCode, listRating);
                                    if (commonCode.Length > 0)
                                    {
                                        if (Convert.ToInt32(AgodaV1SearchConfig[k].CityID) == 0 || Convert.ToInt32(AgodaV1SearchConfig[k].CityID) == SearchPara.CityID)//All City means 0
                                        {
                                            {
                                                int Noofrecords = Convert.ToInt32(AgodaV1SearchConfig[k].Noofrecords);
                                                DataTable[] splittedtables = commonCode.AsEnumerable()
                                                                           .Select((row, index) => new { row, index })
                                                                           .GroupBy(x => x.index / Noofrecords)  // integer division, the fractional part is truncated
                                                                           .Select(g => g.Select(x => x.row).CopyToDataTable())
                                                                           .ToArray();
                                                for (int a = 0; a < splittedtables.Length; a++)
                                                //for (int a = 0; a < 30; a++)
                                                {
                                                    var paramValues1 = String.Join(",", splittedtables[a].Select().Select(x => String.Join(",", Convert.ToString(x.ItemArray[0]))));
                                                    string IdsForSearch = Convert.ToString(paramValues1);
                                                    HotelX xhotel = new HotelX();
                                                    xhotel.Supplier = Supplier;
                                                    xhotel.DebugeMode = DebugMode;
                                                    xhotel.SearchTimeOut = Convert.ToInt32(AgodaV1SearchConfig[k].HotelSearchTimeOut);
                                                    xhotel.ProcessTimeout = Convert.ToInt32(AgodaV1SearchConfig[k].HotelSearchTimeOut2);
                                                    xhotel.URL = Convert.ToString(AgodaInfo.AgodaNewHotelsearchUrl);
                                                    xhotel.UserId = Convert.ToString(AgodaV1Details.UserID);
                                                    xhotel.UserName = Convert.ToString(AgodaV1Details.UserName);
                                                    xhotel.PassWord = Convert.ToString(AgodaV1Details.Password);
                                                    xhotel.CurrencyCode = Currency;
                                                    xhotel.CurrencyFactor = CurrencyFactorAgoda;
                                                    xhotel.XmlNationality = XmlNationality;
                                                    xhotel.XmlCity = Xmlcity;
                                                    xhotel.XmlCountry = "";
                                                    xhotel.AgentWiseMarkup = AgodaV1Details.AgentWiseMarkup;
                                                    xhotel.IsShowSupplier = Convert.ToInt16(AgodaV1Details.IsShowSupplier) == 1 ? true : false;
                                                    xhotel.SupplierID = AgodaV1Details.SupplierID;
                                                    //xhotel.MasterSupplierID = AgodaV1Details[0].MasterSupplierId;
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
                                                        objrq.Request = XML_Agoda_New_RequestOnly_async(SearchByOneID, "1", xhotel.URL, xhotel.UserId, xhotel.PassWord, xhotel.SearchTimeOut, SearchPara, IdsForSearch, xhotel.Supplier, CurrencyFactorAgoda, DebugMode, Xmlcity, XmlNationality, xhotel.CurrencyCode);
                                                        objrq.Response = ""; RQRS.Add(objrq);
                                                    }
                                                    xhotel.RQRS = RQRS;
                                                    xhotel.ReturnRooms = 5; // AgodaV1Details[0].ReturnRooms; //140225
                                                    xhotel.TechName = "Agoda"; // AgodaV1Details[0].TechName; ;//140225
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
                #endregion
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
            }
        }
        public void GetAgoda_NewRequestOnly_async(bool SearchByOneID, DataRow[] commonCode, List<StarRating> listRating, string Supplier, bool IsHotelWiseSearch, bool HitFromAll, HotelSearchParameter SearchPara, List<SupplierDetails> AgodaV1Details, List<SupplierSearchConfig> AgodaV1SearchConfig, DataTable dtPreferredDataHotel, ref ConcurrentBag<HotelX> AllTask, string Xmlcity, string XmlNationality, int Hit, DataTable dtROE, TokenData tokenData)
        {
            try
            {
                #region Agoda
                if ((SearchPara.IsSearchFor.ToLower().IndexOf(Supplier.ToLower()) >= 0 || SearchPara.IsSearchFor.ToLower().IndexOf("multi") >= 0) && HitFromAll)
                {
                    string Ids = string.Empty;
                    string xmlbase = "";
                    _commonMethod.GetPrefferedHotelCodes(ref Ids, ref xmlbase, ref AgodaV1SearchConfig, Supplier, dtPreferredDataHotel, SearchPara.IsSearchFor);
                    if (AgodaV1SearchConfig != null)
                    {
                        //string Currency = AgodaInfo.AgodaNewCurrency;
                        string Currency = Convert.ToString(AgodaV1Details[0].CurrencyShortName);

                        double CurrencyFactorAgoda = 1;
                        if (Currency.ToLower() != tokenData.SiteCurrency.ToLower())
                        {
                            //DataTable dtInrCurrency = Common.GetCountryCurrency(Currency);
                            CurrencyFactorAgoda = _commonMethod.GetROEByCurrency(Currency, dtROE);// Convert.ToDouble(dtInrCurrency.Rows[0]["AmtToExch"]);
                        }

                        {
                            for (int k = 0; k < AgodaV1SearchConfig.Count; k++)
                            {
                                bool DebugMode = (Convert.ToInt32(AgodaV1SearchConfig[k].DebugMode) == 0 ? false : true);
                                {
                                    //List<HotelCodeByCity> DtHotelDAta = FilterFinalList(commonCode, listRating);
                                    if (commonCode.Length > 0)
                                    {
                                        if (Convert.ToInt32(AgodaV1SearchConfig[k].CityID) == 0 || Convert.ToInt32(AgodaV1SearchConfig[k].CityID) == SearchPara.CityID)//All City means 0
                                        {
                                            {
                                                int Noofrecords = Convert.ToInt32(AgodaV1SearchConfig[k].Noofrecords);
                                                DataTable[] splittedtables = commonCode.AsEnumerable()
                                                                           .Select((row, index) => new { row, index })
                                                                           .GroupBy(x => x.index / Noofrecords)  // integer division, the fractional part is truncated
                                                                           .Select(g => g.Select(x => x.row).CopyToDataTable())
                                                                           .ToArray();
                                                for (int a = 0; a < splittedtables.Length; a++)
                                                //for (int a = 0; a < 30; a++)
                                                {
                                                    var paramValues1 = String.Join(",", splittedtables[a].Select().Select(x => String.Join(",", Convert.ToString(x.ItemArray[0]))));
                                                    string IdsForSearch = Convert.ToString(paramValues1);
                                                    HotelX xhotel = new HotelX();
                                                    xhotel.Supplier = Supplier;
                                                    xhotel.DebugeMode = DebugMode;
                                                    xhotel.SearchTimeOut = Convert.ToInt32(AgodaV1SearchConfig[k].HotelSearchTimeOut);
                                                    xhotel.ProcessTimeout = Convert.ToInt32(AgodaV1SearchConfig[k].HotelSearchTimeOut2);
                                                    xhotel.URL = Convert.ToString(AgodaInfo.AgodaNewHotelsearchUrl);
                                                    xhotel.UserId = Convert.ToString(AgodaV1Details[0].UserID);
                                                    xhotel.UserName = Convert.ToString(AgodaV1Details[0].UserName);
                                                    xhotel.PassWord = Convert.ToString(AgodaV1Details[0].Password);
                                                    xhotel.CurrencyCode = Currency;
                                                    xhotel.CurrencyFactor = CurrencyFactorAgoda;
                                                    xhotel.XmlNationality = XmlNationality;
                                                    xhotel.XmlCity = Xmlcity;
                                                    xhotel.XmlCountry = "";
                                                    xhotel.AgentWiseMarkup = AgodaV1Details[0].AgentWiseMarkup;
                                                    xhotel.IsShowSupplier = Convert.ToInt16(AgodaV1Details[0].IsShowSupplier) == 1 ? true : false;
                                                    xhotel.SupplierID = AgodaV1Details[0].SupplierID;
                                                    //xhotel.MasterSupplierID = AgodaV1Details[0].MasterSupplierId;
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
                                                        objrq.Request = XML_Agoda_New_RequestOnly_async(SearchByOneID, "1", xhotel.URL, xhotel.UserId, xhotel.PassWord, xhotel.SearchTimeOut, SearchPara, IdsForSearch, xhotel.Supplier, CurrencyFactorAgoda, DebugMode, Xmlcity, XmlNationality, xhotel.CurrencyCode);
                                                        objrq.Response = ""; RQRS.Add(objrq);
                                                    }
                                                    xhotel.RQRS = RQRS;
                                                    xhotel.ReturnRooms = 5; // AgodaV1Details[0].ReturnRooms; //140225
                                                    xhotel.TechName = "Agoda"; // AgodaV1Details[0].TechName; ;//140225
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
                #endregion
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
            }
        }
        public string XML_Agoda_New_RequestOnly_async(bool SearchByOneID, string star, string Url, string ClientID, string Password, Int32 SearchTimeOut, HotelSearchParameter SearchParam, string HotelId, string Provider, double CurrencyFactorAgoda, bool DebugMode, string CityCode, string NationalityCode, string Currency)
        {
            try
            {
                #region PriceOnly RQ
                string AgodaSiteID = ClientID;
                string AgodaApiKey = Password;
                string AgodaWebUrl = Url;
                string AgodaAuthorizationKey = AgodaSiteID + ":" + AgodaApiKey;
                //AgodaWebUrl = "https://affiliateapi7643.agoda.com/api/v4/property/availability";//AgodaWebUrl + "/api/v4/property/availability";
                AgodaWebUrl = AgodaInfo.AgodaNewHotelavailabilityUrl;//AgodaWebUrl + "/api/v4/property/availability";
                string AgodaLaguage = AgodaInfo.AgodaLaguage;
                //string AgodaCurrency = AgodaInfo.AgodaNewCurrency;
                string AgodaCurrency = Currency;
                string CheckInDate = SearchParam.CheckInDate;
                CheckInDate = CheckInDate.Substring(6, 4) + "-" + CheckInDate.Substring(3, 2) + "-" + CheckInDate.Substring(0, 2);
                string CheckInOut = SearchParam.CheckOutDate;
                CheckInOut = CheckInOut.Substring(6, 4) + "-" + CheckInOut.Substring(3, 2) + "-" + CheckInOut.Substring(0, 2);
                int Rooms = SearchParam.Rooms.Count;
                int Adult = 0;
                int child = 0;

                foreach (var item in SearchParam.Rooms)
                {
                    Adult = Adult + item.NoofAdults;
                    child = child + item.NoofChild;
                }
                string ChildAge = "";
                string adults = "";
                string childage = "";

                for (int i = 1; i <= Rooms; i++)
                {
                    switch (Convert.ToInt32(SearchParam.Rooms[i - 1].NoofChild))
                    {
                        case 1:
                            childage = Convert.ToString(SearchParam.Rooms[i - 1].Child1Age);
                            break;
                        case 2:
                            childage = SearchParam.Rooms[i - 1].Child1Age + "," + SearchParam.Rooms[i - 1].Child2Age;
                            break;
                        default:
                            break;
                    }
                    if (Convert.ToInt32(SearchParam.Rooms[i - 1].NoofChild) > 0)// Added on 25-JULY-2024
                    {
                        ChildAge = childage + "," + ChildAge;
                    }
                }
                //ChildAge = ChildAge.Remove(ChildAge.Length - 1);
                if (child != 0)// Added on 25-JULY-2024
                    ChildAge = ChildAge.Substring(0, ChildAge.Length - 1);

                string AgodaPriceOnly = GetAgoda_NewSearchRequest(SearchByOneID, CheckInDate, CheckInOut, Rooms, Adult, child, AgodaSiteID, AgodaApiKey, AgodaLaguage, AgodaCurrency, 1000, HotelId, NationalityCode, ChildAge);
                return AgodaPriceOnly;
                #endregion
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
                return "";
            }
        }
        public string GetAgoda_NewSearchRequest(bool SearchByOneID, string CheckInDate, string CheckInOut, int Rooms, int Adult, int child, string AgodaSiteID, string AgodaApiKey, string AgodaLaguage, string AgodaCurrency, int SearchTimeout, string HotelIds, string Nationality, string ChildAge)
        {
            try
            {
                if (HotelIds.Contains(","))
                {
                    SearchByOneID = false;
                }
                StringBuilder AGNEW = new StringBuilder();
                AGNEW.Append("{");
                AGNEW.Append("\"waitTime\":" + SearchTimeout + ",");
                AGNEW.Append("\"criteria\":");
                AGNEW.Append("{");
                //AGNEW.Append("\"propertyIds\":" + HotelIds +  "\" ");
                AGNEW.Append("\"propertyIds\":");
                AGNEW.Append("[");
                //if (HotelIds.Split(',').Length > 1)
                AGNEW.Append(" " + HotelIds + " ");
                AGNEW.Append("],");
                AGNEW.Append("\"checkIn\":\"" + CheckInDate + "\",");
                AGNEW.Append("\"checkOut\":\"" + CheckInOut + "\",");
                AGNEW.Append("\"rooms\":" + Rooms + ",");
                AGNEW.Append("\"adults\":" + Adult + ",");
                AGNEW.Append("\"children\":" + child + ",");
                if (child > 0)
                {
                    AGNEW.Append("\"childrenAges\":");
                    AGNEW.Append("[");
                    AGNEW.Append(ChildAge);
                    AGNEW.Append("],");
                }
                AGNEW.Append("\"language\":\"" + AgodaLaguage + "\",");
                AGNEW.Append("\"currency\":\"" + AgodaCurrency + "\",");
                AGNEW.Append("\"userCountry\":\"" + Nationality + "\" ");
                AGNEW.Append("},");
                AGNEW.Append("\"features\":");
                AGNEW.Append("{");
                if (SearchByOneID)
                    AGNEW.Append("\"ratesPerProperty\":100,"); // Added on 20-JUN-2024 as per mustafa sir
                else
                    //AGNEW.Append("\"ratesPerProperty\":25,");
                    //AGNEW.Append("\"ratesPerProperty\":10,");// Added on 03-OCT-2024 as per mustafa sir
                    AGNEW.Append("\"ratesPerProperty\":20,");// Added on 09-SEP-2024 as per mustafa sir//karnav
                AGNEW.Append("\"extra\":");
                AGNEW.Append("[");
                AGNEW.Append("\"content\",");
                AGNEW.Append("\"surchargeDetail\",");
                AGNEW.Append("\"cancellationDetail\",");
                AGNEW.Append("\"benefitDetail\",");
                AGNEW.Append("\"dailyRate\",");
                AGNEW.Append("\"taxDetail\",");
                AGNEW.Append("\"rateDetail\",");
                AGNEW.Append("\"promotionDetail\"");
                AGNEW.Append("]");
                AGNEW.Append("}");
                AGNEW.Append("}");
                string AgodaNewString = AGNEW.ToString();
                return AgodaNewString;
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
                return "";
            }
        }
        public async Task<List<Options>> XMLAgoda_V1_HotelSearchWithRooms(HotelX Item, CommonSearch commonSearch, string Response, string CityCode, HotelSearchParameter SearchParam, double CurrencyFactorAgoda, string Agoda_NationalityCode, string ProccessCutOffTime, Int32 processTimeout, string ResProccessTimerLogActive, string ResProccessTimeValueforSaveLog, string StoreSupplierResErrorLog, string Provider)
        {
            System.Diagnostics.Stopwatch timer_room_Agoda = new System.Diagnostics.Stopwatch();
            timer_room_Agoda.Start();
            List<Options> options = new List<Options>();
            try
            {
                #region DataTable
                DataTable DtHotel = _commonMethod.GenerateDataTableNewV2();
                #endregion
                if (Response != string.Empty)
                {
                    #region PriceOnly RS
                    Agoda_NewRoomSearch.Root Agoda_NewResponse = null;
                    try
                    {
                        Agoda_NewResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<Agoda_NewRoomSearch.Root>(Response);
                        //Agoda_NewResponse = JsonSerializer.Deserialize<Agoda_NewRoomSearch.Root>(Response);

                    }
                    catch (Exception ex)
                    {
                        Item.ResponseStatus = 404;
                        await _exception.Error(ex);
                        Item.ResponseType = "NoCount";
                        return options;
                    }
                    if (Agoda_NewResponse == null)
                    {
                        Item.ResponseStatus = 204;
                        Item.ResponseType = "NoCount";
                        return options;
                    }
                    //if (DS.results.Count < 1)
                    if (Agoda_NewResponse.properties == null || Agoda_NewResponse.properties.Count < 1)
                    {
                        Item.ResponseStatus = 404;
                        Item.ResponseType = "NoCount";
                        return options;
                    }
                    int MaxAdult = 0;
                    int MaxChild = 0;
                    foreach (RoomDetails item in SearchParam.Rooms)
                    {
                        MaxAdult += item.NoofAdults;
                        MaxChild += item.NoofChild;
                    }
                    int Adult = 0;
                    foreach (var item in SearchParam.Rooms)
                    {
                        Adult = Adult + item.NoofAdults + item.NoofChild;
                        //child = child + item.NoofChild;
                    }
                    #endregion
                    for (int h = 0; h < Agoda_NewResponse.properties.Count; h++)
                    {
                        Item.ResponseType = "Success";
                        //Check Time If High then return NoCount
                        if (ProccessCutOffTime == "1" && timer_room_Agoda.ElapsedMilliseconds > processTimeout)
                        {
                            timer_room_Agoda.Stop();
                            break;
                        }
                        //--end
                        string ItemCode = Convert.ToString(Agoda_NewResponse.properties[h].propertyId);
                        #region "When not Need Room"  
                        // add hotel code from price only and room code blank here
                        if (SearchParam.Filters.IsShowRooms == 0)
                        {
                            #region "Hotel Code Block "
                            double TotalCharges = 0;
                            {
                                DataRow drXmlHotel = DtHotel.NewRow();
                                string InternalHotelId = Convert.ToString(ItemCode);
                                drXmlHotel["XMLHotelCode"] = ItemCode;
                                drXmlHotel["HotelName"] = "";
                                drXmlHotel["SupplierName"] = Provider;
                                drXmlHotel["OfficeID"] = 0;
                                drXmlHotel["ContractID"] = 0;
                                drXmlHotel["Rating"] = 0;
                                drXmlHotel["Reviews"] = 0;
                                drXmlHotel["UniqueNo"] = ItemCode;
                                drXmlHotel["IsShowSupplier"] = false;
                                drXmlHotel["BookingStatus"] = "Available";
                                drXmlHotel["XMLCity"] = CityCode;
                                //drXmlHotel["SupplierCurrency"] = AgodaInfo.AgodaNewCurrency;
                                drXmlHotel["SupplierCurrency"] = Item.CurrencyCode;
                                string drLowest = Convert.ToString(Agoda_NewResponse.properties[h].propertyId);
                                //if (XMLHotelBookingCommon.AgodaNewCurrency.ToLower() != "usd")
                                //{
                                //    DataTable dtInrCurrency = Common.GetCountryCurrency(currency);
                                //    CurrencyFactorAgoda = Convert.ToDouble(dtInrCurrency.Rows[0]["AmtToExch"]);
                                //}
                                TotalCharges = Convert.ToDouble(Convert.ToDouble(Agoda_NewResponse.properties[h].rooms[0].totalPayment.inclusive) * CurrencyFactorAgoda);
                                drXmlHotel["FinalSellingPrice"] = TotalCharges;
                                drXmlHotel["SellingPrice"] = TotalCharges;


                                DtHotel.Rows.Add(drXmlHotel);
                                #endregion
                                if (TotalCharges > 0)
                                {
                                    #region "Room code Block"
                                    bool willAddOrNot = true;
                                    Int64 RaynaID = 0;
                                    Int64 GiataId = 0;
                                    int Rating = 0;
                                    Int64 OwnsystemHotelId = 0;
                                    double MinPrice = TotalCharges;
                                    _markup.GetHotelDataCommon(ref MinPrice, ref willAddOrNot, ref OwnsystemHotelId, ref RaynaID, ref GiataId, ref Rating, ref drXmlHotel, commonSearch.AgentId, commonSearch.TariffID, commonSearch.dsAgentDetails, commonSearch.supplierGiataMaster, SearchParam, commonSearch.dtCountryCityMarkup, commonSearch.B2CMarkup, commonSearch.IsParentId, commonSearch.SubAgentMarkup, commonSearch.SearchByHotel, commonSearch.HotelIDS);
                                    if (willAddOrNot)
                                    {
                                        #region "Room Logic"
                                        DataTable dt_HotelRoom = _commonMethod.GenerateRoomtypeDataTableNewV2();
                                        DataTable dt_Cancellation = _commonMethod.GenerateCancellationDataTableNewV2();
                                        DataTable dt_RateBreackUp = _commonMethod.GenerateoomRateBreackUpDataTableNewV2();
                                        #endregion
                                        decimal LowestPrice = Convert.ToDecimal(MinPrice).CalculatePrice(commonSearch.tokenData, commonSearch.advancedOptions, AgodaInfo.AgodaNewCurrency);
                                        Options ops = (await _supplierRes.GetCommonRoomForSuppliers(GiataId, commonSearch.tokenData, commonSearch.advancedOptions, Convert.ToDouble(LowestPrice), drXmlHotel, dt_HotelRoom, dt_Cancellation, dt_RateBreackUp, SearchParam));
                                        if (ops != null)
                                            options.Add(ops);
                                    }
                                    #endregion
                                }
                            }
                        }
                        #endregion
                        #region "When Need Rooms"
                        //add Hotel code blank and rooms code in room block
                        else
                        {
                            double FinalMinPrice = 0;
                            #region Hotels
                            #region "Hotel Block here"
                            double TotalCharges = 1;
                            DataRow drXmlHotel = DtHotel.NewRow();
                            string InternalHotelId = Convert.ToString(ItemCode);
                            drXmlHotel["XMLHotelCode"] = ItemCode;
                            drXmlHotel["HotelName"] = "";
                            drXmlHotel["SupplierName"] = Provider;
                            drXmlHotel["OfficeID"] = 0;
                            drXmlHotel["ContractID"] = 0;
                            drXmlHotel["Rating"] = 0;
                            drXmlHotel["Reviews"] = 0;
                            drXmlHotel["UniqueNo"] = ItemCode;
                            drXmlHotel["IsShowSupplier"] = false;
                            drXmlHotel["BookingStatus"] = "Available";
                            drXmlHotel["XMLCity"] = CityCode;
                            //drXmlHotel["SupplierCurrency"] = AgodaInfo.AgodaNewCurrency;
                            drXmlHotel["SupplierCurrency"] = Item.CurrencyCode;

                            drXmlHotel["FinalSellingPrice"] = TotalCharges;
                            drXmlHotel["SellingPrice"] = TotalCharges;

                            DtHotel.Rows.Add(drXmlHotel);
                            FinalMinPrice = TotalCharges;
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
                                DataTable dtHotelRoom = _commonMethod.GenerateRoomtypeDataTableNewV2();
                                DataTable dtCancellation = _commonMethod.GenerateCancellationDataTableNewV2();
                                DataTable DtRoomRateBreackUp = _commonMethod.GenerateoomRateBreackUpDataTableNewV2();
                                int CutOffDays = XMLHotelBookingCommon.CutOffDays;
                                CutOffDays = _commonMethod.GetCutOffDays(commonSearch.dtSuplierMarkup, Provider);
                                #region "Room Logic"
                                //DataTable XMLTBORoomOnly = Common.GenerateRoomtypeDataTableNewV2();

                                if (SearchParam.Filters.IsShowRooms == 1)
                                {
                                    #region Data
                                    double AgentPercentage = 0.0;
                                    double AgentMarkupAmount = 0.0;
                                    Boolean isPerce = true;
                                    bool blnIsShowSupplier = false;
                                    //_markup.getAgetMarkupSetting(Provider, GiataId.ToString(), commonSearch.dtSuplierMarkup, commonSearch.dtMarkupDetails, ref AgentPercentage, ref AgentMarkupAmount, ref blnIsShowSupplier, ref isPerce, commonSearch.AgentId, RatingHotel, SearchParam.CityID, SearchParam.CountryID.ToString(), SearchParam.CheckInDate);

                                    _markup.getAgetMarkupSettingNew_new(Provider, Convert.ToString(GiataId), commonSearch.dtSuplierMarkup, commonSearch.dsMarkupDetails, ref AgentPercentage, ref AgentMarkupAmount, ref blnIsShowSupplier, ref isPerce, commonSearch.AgentId, RatingHotel, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), SearchParam.CheckInDate);
                                    #endregion
                                    string RoomTypeId = "";
                                    double Price = 0;
                                    string TaxBreakdown = "";
                                    string TaxBreakdownID = "";
                                    int SurchargeId = 0;
                                    string surcharge_Name = "";
                                    double surcharge_inclusive = 0;
                                    string surcharge_method = "";
                                    double fees = 0;
                                    double tax = 0;
                                    double exclusive = 0;
                                    bool mandatory = false;
                                    string SearchId = Convert.ToString(Agoda_NewResponse.searchId);
                                    //for (int h = 0; h < Agoda_NewResponse.properties.Count; h++)
                                    {
                                        int propertyId = Agoda_NewResponse.properties[h].propertyId;
                                        string propertyName = Agoda_NewResponse.properties[h].propertyName;
                                        string translatedPropertyName = Agoda_NewResponse.properties[h].translatedPropertyName;

                                        for (int ro = 0; ro < Agoda_NewResponse.properties[h].rooms.Count; ro++)
                                        {

                                            if (_keySettings.IsRoomReturnActive && ((ro + 1) > Item.ReturnRooms))
                                            {
                                                break;
                                            }



                                            string Essentialinfo = "";

                                            // string RoomName = hyperguestResponse.results[h].rooms[ro].roomName;
                                            int RoomId = Agoda_NewResponse.properties[h].rooms[ro].roomId;
                                            int parentRoomId = Agoda_NewResponse.properties[h].rooms[ro].parentRoomId;
                                            string BlockId = Agoda_NewResponse.properties[h].rooms[ro].blockId;
                                            string RoomName = Agoda_NewResponse.properties[h].rooms[ro].roomName;
                                            if (RoomName == "" || RoomName == null)//Added on 3-AUG-2024
                                            {
                                                continue;
                                            }
                                            RoomName = RoomName.Replace("'", "");//added on 8-MAY-2024
                                            int RatePlanId = Agoda_NewResponse.properties[h].rooms[ro].ratePlanId;
                                            int PromotionDetail = 0;
                                            string Description = "";
                                            if (Agoda_NewResponse.properties[h].rooms[ro].promotionDetail != null)
                                            {
                                                PromotionDetail = Agoda_NewResponse.properties[h].rooms[ro].promotionDetail.promotionId;
                                                Description = Agoda_NewResponse.properties[h].rooms[ro].promotionDetail.description;
                                            }


                                            string PaymentModel = Agoda_NewResponse.properties[h].rooms[ro].paymentModel;
                                            double TotalInclusiv = Convert.ToDouble(Agoda_NewResponse.properties[h].rooms[ro].totalPayment.inclusive);
                                            TotalInclusiv = TotalInclusiv;// * objSearPera.NoofNights;// * objSearPera.Rooms.Count;
                                            double TotalPrice = Agoda_NewResponse.properties[h].rooms[ro].totalPayment.inclusive;
                                            double RatePrice = Agoda_NewResponse.properties[h].rooms[ro].rate.inclusive;
                                            TotalPrice = TotalPrice;// * objSearPera.Rooms.Count;
                                            int NormalBedding = 0;
                                            int ExtraBeds = 0;
                                            double ChargeTotal = 0;
                                            NormalBedding = Agoda_NewResponse.properties[h].rooms[ro].normalBedding;
                                            ExtraBeds = Agoda_NewResponse.properties[h].rooms[ro].extraBeds;

                                            for (int tx = 0; tx < Agoda_NewResponse.properties[h].rooms[ro].taxBreakdown.Count; tx++)
                                            {
                                                TaxBreakdownID = Agoda_NewResponse.properties[h].rooms[ro].taxBreakdown[tx].id;
                                                TaxBreakdown = Agoda_NewResponse.properties[h].rooms[ro].taxBreakdown[tx].taxDescription;
                                            }
                                            //bool freeBreakfast = Agoda_NewResponse.properties[h].rooms[ro].freeBreakfast;

                                            #region Meal
                                            if (Convert.ToString(Agoda_NewResponse.properties[h].rooms[ro].freeBreakfast) != null)
                                            {
                                                bool freeBreakfast = Convert.ToBoolean(Agoda_NewResponse.properties[h].rooms[ro].freeBreakfast);

                                                string Meal = Convert.ToString("RO");
                                                if (freeBreakfast)
                                                    Meal = "Breakfast";

                                                #endregion

                                                //--for benefits
                                                if (Adult > 2)
                                                {
                                                    if (ExtraBeds == 0)
                                                        RoomName = RoomName + " [" + "No ExtraBed]";
                                                    else if (ExtraBeds > 0)
                                                        RoomName = RoomName + " [" + " With ExtraBed]";
                                                }

                                                int RemainingRooms = Agoda_NewResponse.properties[h].rooms[ro].remainingRooms;

                                                #region Benefit
                                                string Benefit = "";
                                                string BoardName = "";

                                                //140525
                                                bool addpax = true;
                                                //if (MaxAdult == NormalBedding)
                                                if (Adult == NormalBedding) // totak audlt + child = adult //added on 150525
                                                {
                                                    addpax = false;
                                                }

                                                List<string> StrMealList = new List<string>();
                                                string TotalAmenities = "";

                                                for (int B = 0; B < Agoda_NewResponse.properties[h].rooms[ro].benefits.Count; B++)
                                                {
                                                    int Benefit_Id = Agoda_NewResponse.properties[h].rooms[ro].benefits[B].id;

                                                    string BenefitName = Agoda_NewResponse.properties[h].rooms[ro].benefits[B].benefitName + ",";
                                                    string translatedBenefitName = Agoda_NewResponse.properties[h].rooms[ro].benefits[B].translatedBenefitName;
                                                    string Mealname = Get_AgodaNew_BenifitName(Benefit_Id, NormalBedding, addpax, Convert.ToString(Agoda_NewResponse.properties[h].rooms[ro].benefits[B].benefitName)); //140525
                                                    //Benefit = Benefit +", "+ NormalBedding + "X " + BenefitName.Substring(0, BenefitName.Length - 1);
                                                    //Benefit = Benefit + ", " + BenefitName.Substring(0, BenefitName.Length - 1);
                                                    if (Mealname == "")
                                                    {
                                                        Benefit = Benefit + ", " + BenefitName;
                                                    }
                                                    else
                                                    {
                                                        Benefit = Benefit + ", " + Mealname;
                                                    }
                                                    if (Mealname != "")
                                                    {
                                                        if (BoardName == "")
                                                        {
                                                            BoardName = Mealname;//Mealname.Substring(0, Mealname.Length - 1);
                                                        }
                                                        else
                                                        {
                                                            BoardName = BoardName + ", " + Mealname;//.Substring(0, Mealname.Length - 1);

                                                        }

                                                        StrMealList.Add(Mealname);//140525

                                                    }
                                                }

                                                //140525

                                                StrMealList.Sort();

                                                for (int i = 0; i < StrMealList.Count; i++)
                                                {
                                                    if (TotalAmenities == string.Empty)
                                                        TotalAmenities = StrMealList[i];
                                                    else
                                                        TotalAmenities = TotalAmenities + " , " + StrMealList[i];
                                                }

                                                if (TotalAmenities == "")
                                                {
                                                    TotalAmenities = "RO";
                                                }

                                                //end 140525

                                                if (BoardName == "")
                                                {
                                                    BoardName = "RO";
                                                }
                                                // Benefit = NormalBedding + " X " + Benefit;
                                                #endregion

                                                #region Rate
                                                string Currency = Agoda_NewResponse.properties[h].rooms[ro].rate.currency;
                                                Price = Agoda_NewResponse.properties[h].rooms[ro].rate.inclusive;
                                                double Rate_exclusive = Agoda_NewResponse.properties[h].rooms[ro].rate.exclusive;
                                                double Tax = Agoda_NewResponse.properties[h].rooms[ro].rate.tax;
                                                double Fees = Agoda_NewResponse.properties[h].rooms[ro].rate.fees;
                                                string Method = Agoda_NewResponse.properties[h].rooms[ro].rate.method;


                                                Essentialinfo += "<p>Room Details:</p><p>" + RoomName + "</p>" +
                                                                 (Benefit == "" ? "" : "<p>Include in Price:" + Benefit + "<p>") +
                                                                 (TaxBreakdown == "" ? "" : "<p>" + TaxBreakdown + "</p>") +
                                                                 (RemainingRooms <= 2 ? "<strong>Our last room!</strong> Book now to avoid disappointment!" : "<strong>" + RemainingRooms + "</strong>" + " Rooms Remaining");

                                                #endregion

                                                double RatePerStay = 0;
                                                #region Surcharge
                                                DataTable dtSurcharge = new DataTable();
                                                dtSurcharge.Columns.Add("SurchargeName");
                                                dtSurcharge.Columns.Add("Type");
                                                dtSurcharge.Columns.Add("Price");
                                                dtSurcharge.Columns.Add("Method");
                                                double TotalSurcharge = 0;

                                                if (Agoda_NewResponse.properties[h].rooms[ro].surcharges.Count() > 0)
                                                {
                                                    for (int sur = 0; sur < Agoda_NewResponse.properties[h].rooms[ro].surcharges.Count; sur++)
                                                    {
                                                        SurchargeId = Agoda_NewResponse.properties[h].rooms[ro].surcharges[sur].id;
                                                        surcharge_method = Agoda_NewResponse.properties[h].rooms[ro].surcharges[sur].method;
                                                        string surcharge_Charge = Agoda_NewResponse.properties[h].rooms[ro].surcharges[sur].charge;
                                                        string surcharge_Margin = Agoda_NewResponse.properties[h].rooms[ro].surcharges[sur].margin;
                                                        surcharge_Name = Agoda_NewResponse.properties[h].rooms[ro].surcharges[sur].name;

                                                        string surcharge_currency = Agoda_NewResponse.properties[h].rooms[ro].surcharges[sur].rate.currency;
                                                        exclusive = Agoda_NewResponse.properties[h].rooms[ro].surcharges[sur].rate.exclusive;
                                                        surcharge_inclusive = Agoda_NewResponse.properties[h].rooms[ro].surcharges[sur].rate.inclusive;
                                                        tax = Agoda_NewResponse.properties[h].rooms[ro].surcharges[sur].rate.tax;
                                                        fees = Agoda_NewResponse.properties[h].rooms[ro].surcharges[sur].rate.fees;
                                                        string method = Agoda_NewResponse.properties[h].rooms[ro].surcharges[sur].rate.method;

                                                        //if (Currency.ToLower() != "usd")
                                                        //{
                                                        //    DataTable dtInrCurrency = Common.GetCountryCurrency(Currency);
                                                        //    Factor = Convert.ToDouble(dtInrCurrency.Rows[0]["AmtToExch"]);
                                                        //}

                                                        DataRow drroomsurcharge = dtSurcharge.NewRow();
                                                        drroomsurcharge["SurchargeName"] = surcharge_Name;
                                                        drroomsurcharge["Type"] = Convert.ToString(surcharge_Charge);
                                                        drroomsurcharge["Price"] = surcharge_inclusive;
                                                        drroomsurcharge["Method"] = surcharge_method;
                                                        dtSurcharge.Rows.Add(drroomsurcharge);

                                                        switch (surcharge_Charge.ToLower())
                                                        {
                                                            case "mandatory":
                                                                TotalSurcharge = TotalSurcharge + surcharge_inclusive;
                                                                mandatory = true;
                                                                break;
                                                            //case "included":
                                                            //    TotalInclusiv = TotalInclusiv;
                                                            //    break;
                                                            //case "optional":
                                                            //    TotalInclusiv = TotalInclusiv;
                                                            //    break;
                                                            //case "excluded":
                                                            //    TotalInclusiv = TotalInclusiv + surcharge_inclusive;
                                                            //    break;

                                                            default:
                                                                break;
                                                        }
                                                    }
                                                    //}
                                                }
                                                RoomTypeId = BlockId + "$" + SurchargeId + "$" + surcharge_Name + "$" + surcharge_inclusive + "$" + surcharge_method + "$" + fees + "$" + tax + "$" +
                                                    exclusive + "$" + Agoda_NationalityCode + "$" + RoomName + "$" + RatePlanId + "$" + SearchId + "$" + propertyId + "$" + RatePrice + "$" + Rate_exclusive +
                                                    "$" + Tax + "$" + Fees + "$" + Currency + "$" + PaymentModel + "$" + parentRoomId + "$" + RoomId + "$" + mandatory + "$" + Price;

                                                TotalInclusiv = (TotalInclusiv);// + TotalSurcharge;
                                                double VATTotalSurcharge = 0;
                                                TotalSurcharge = _markup.ProcessHotelRateBreakupPrice_WithMarkup(SearchParam.NoofNights, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), TotalSurcharge, commonSearch.B2CMarkup, commonSearch.IsParentId, isPerce, AgentPercentage, AgentMarkupAmount, commonSearch.SubAgentMarkup, ref VATTotalSurcharge);

                                                // double TotalPrice = Price * Factor;

                                                #endregion
                                                //string IncludedMeal = BoardName;// Benefit;//Meal; //comment on 150525
                                                string IncludedMeal = TotalAmenities;// Benefit;//Meal; //added on 150525
                                                BlockId = BlockId.Replace("'", "");
                                                #region Room 
                                                Double TotalVat = 0;
                                                if (IncludedMeal == "")
                                                    IncludedMeal = "RO";
                                                DataRow drAgodaHotelRoom = dtHotelRoom.NewRow();

                                                drAgodaHotelRoom["OwnsystemHotelId"] = OwnsystemHotelId;
                                                drAgodaHotelRoom["ContractID"] = 0;
                                                drAgodaHotelRoom["UniqueNo"] = OwnsystemHotelId;
                                                drAgodaHotelRoom["RoomSequenceNo"] = Guid.NewGuid();
                                                drAgodaHotelRoom["MealName"] = IncludedMeal;
                                                drAgodaHotelRoom["allocationDetails"] = RemainingRooms;
                                                drAgodaHotelRoom["RoomTypeCode"] = parentRoomId;
                                                drAgodaHotelRoom["minStay"] = 0;
                                                drAgodaHotelRoom["DateApplyMinStay"] = "";
                                                drAgodaHotelRoom["MealId"] = IncludedMeal;
                                                drAgodaHotelRoom["RoomNo"] = Convert.ToString("1");
                                                drAgodaHotelRoom["RoomTypeName"] = RoomName;
                                                drAgodaHotelRoom["RoomTypeId"] = RoomTypeId;
                                                drAgodaHotelRoom["BookingStatus"] = "Available";
                                                drAgodaHotelRoom["AdultsOccupancy"] = MaxAdult;
                                                drAgodaHotelRoom["ChildOccupancy"] = MaxChild;
                                                drAgodaHotelRoom["MaxExtrabed"] = ExtraBeds;
                                                drAgodaHotelRoom["BoardTypeID"] = "";
                                                drAgodaHotelRoom["CompanyBuyingPrice"] = TotalInclusiv;
                                                drAgodaHotelRoom["OfferDescription"] = Description;
                                                drAgodaHotelRoom["offerCode"] = "";
                                                //dtAgoda_NewRoom.Rows.Add(drAgodaHotelRoom);

                                                ChargeTotal = TotalInclusiv;

                                                //_markup.ProcessHotelRoom_WithMarkup(CurrencyFactorAgoda, AgodaInfo.AgodaNewCurrency, false, true, SearchParam.NoofNights, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), SearchParam.CheckInDate, SearchParam.CheckOutDate, ref TotalInclusiv, ref drAgodaHotelRoom, isPerce, AgentPercentage, AgentMarkupAmount, commonSearch.SubAgentMarkup, commonSearch.B2CMarkup, commonSearch.IsParentId);
                                                _markup.ProcessHotelRoom_WithMarkup(CurrencyFactorAgoda, Item.CurrencyCode, false, true, SearchParam.NoofNights, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), SearchParam.CheckInDate, SearchParam.CheckOutDate, ref TotalInclusiv, ref drAgodaHotelRoom, isPerce, AgentPercentage, AgentMarkupAmount, commonSearch.SubAgentMarkup, commonSearch.B2CMarkup, commonSearch.IsParentId);
                                                TotalVat = Convert.ToDouble(drAgodaHotelRoom["VatdiffAmount"]);
                                                drAgodaHotelRoom["SupplierName"] = Provider;
                                                #endregion
                                                #region "Rate Breakup"
                                                //double FirstNightCharge = TotalPrice;
                                                DateTime dtCheckInDateForRate = DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null);
                                                double ratePerNight = 0;
                                                //for (int night = 0; night < objSearPera.NoofNights; night++)
                                                //{
                                                //    ratePerNight = TotalPrice;
                                                //    #region Vat
                                                //    double VatOnPerNight = 0;
                                                //    ratePerNight = Common.ProcessHotelRateBreakupPrice_WithMarkup(1, objSearPera.CityID, objSearPera.CountryId, ratePerNight, B2CMarkup, IsParentId, isPerce, AgentPercentage, AgentMarkupAmount, SubAgentMarkup, ref VatOnPerNight);
                                                //    #endregion

                                                //    string rateWeekColumn = Convert.ToString(dtCheckInDateForRate.Date.AddDays(night).DayOfWeek).Substring(0, 3);
                                                //    DataRow drAgodaRoomRateBreackUp = dtAgoda_NewRoomRateBreakup.NewRow();
                                                //    drAgodaRoomRateBreackUp["RoomNo"] = Convert.ToString("1");
                                                //    drAgodaRoomRateBreackUp["SupplierName"] = "agoda_v1";
                                                //    drAgodaRoomRateBreackUp["OwnsystemHotelId"] = ownsystemid;
                                                //    drAgodaRoomRateBreackUp["ContractID"] = 0;
                                                //    drAgodaRoomRateBreackUp["RoomTypeName"] = RoomName;
                                                //    drAgodaRoomRateBreackUp["RoomTypeId"] = RoomTypeId;
                                                //    drAgodaRoomRateBreackUp["MealId"] = IncludedMeal;
                                                //    drAgodaRoomRateBreackUp["Meal"] = IncludedMeal;
                                                //    drAgodaRoomRateBreackUp["BookingStatus"] = "Available";
                                                //    drAgodaRoomRateBreackUp["DateOfWeek"] = rateWeekColumn;
                                                //    drAgodaRoomRateBreackUp["Date"] = dtCheckInDateForRate.Date.AddDays(night).Date;
                                                //    drAgodaRoomRateBreackUp["Rate"] = /*TotalInclusiv*/ratePerNight / objSearPera.NoofNights;
                                                //    drAgodaRoomRateBreackUp["RateType"] = "A Rate";
                                                //    drAgodaRoomRateBreackUp["OrderNo"] = 1;
                                                //    drAgodaRoomRateBreackUp["RoomSequenceNo"] = Guid.NewGuid();
                                                //    drAgodaRoomRateBreackUp["BoardTypeID"] = "";
                                                //    drAgodaRoomRateBreackUp["Surcharge"] = 0;
                                                //    drAgodaRoomRateBreackUp["CompanyCharge"] = 0;
                                                //    drAgodaRoomRateBreackUp["VatdiffAmount"] = TotalVat / objSearPera.NoofNights;
                                                //    dtAgoda_NewRoomRateBreakup.Rows.Add(drAgodaRoomRateBreackUp);
                                                //    dtAgoda_NewRoomRateBreakup.AcceptChanges();
                                                //}
                                                if (true)
                                                {

                                                    for (int night = 0; night < SearchParam.NoofNights; night++)
                                                    {
                                                        for (int sur = 0; sur < dtSurcharge.Rows.Count; sur++)
                                                        {
                                                            TotalSurcharge = Convert.ToDouble(dtSurcharge.Rows[sur]["Price"]);
                                                            string surtype = Convert.ToString(dtSurcharge.Rows[sur]["Type"]);
                                                            string SurchargeName = Convert.ToString(dtSurcharge.Rows[sur]["SurchargeName"]);
                                                            VATTotalSurcharge = 0;
                                                            if (surtype.ToLower() != "excluded")
                                                            {
                                                                SurchargeName = "(Included in Total) " + SurchargeName;
                                                                TotalSurcharge = _markup.ProcessHotelRateBreakupPrice_WithMarkup(SearchParam.NoofNights, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), TotalSurcharge, commonSearch.B2CMarkup, commonSearch.IsParentId, isPerce, AgentPercentage, AgentMarkupAmount, commonSearch.SubAgentMarkup, ref VATTotalSurcharge);
                                                            }
                                                            else
                                                                SurchargeName = SurchargeName + "<b>(Not Included in Total)</b> ";// + TotalSurcharge;
                                                            if (sur == 0 && night == 0)
                                                                Essentialinfo += "<p><strong> Surcharges Collected by Property:</strong> </p>";
                                                            if (night == 0)
                                                                Essentialinfo += "<p>" + SurchargeName + " Amount: " + TotalSurcharge + " " + Currency;
                                                            string rateWeekColumn = Convert.ToString(dtCheckInDateForRate.Date.AddDays(night).DayOfWeek).Substring(0, 3);

                                                        }
                                                    }
                                                }
                                                drAgodaHotelRoom["EssentialInformation"] = Essentialinfo;
                                                dtHotelRoom.Rows.Add(drAgodaHotelRoom);
                                                drAgodaHotelRoom.AcceptChanges();
                                                #endregion

                                                //18-01-2022aaa

                                                #region  "Cancellationpolicy"

                                                if (Agoda_NewResponse.properties[h].rooms[ro].cancellationPolicy != null)
                                                {
                                                    int cutoff = 1;
                                                    cutoff = (-_commonMethod.GetCutOffDays(commonSearch.dtSuplierMarkup, Provider));
                                                    bool IsRefundable = true;
                                                    //double CancelletionPrice = price;
                                                    string Code = Agoda_NewResponse.properties[h].rooms[ro].cancellationPolicy.code;
                                                    string CancellationText = Agoda_NewResponse.properties[h].rooms[ro].cancellationPolicy.cancellationText;
                                                    string TranslatedCancellationText = Agoda_NewResponse.properties[h].rooms[ro].cancellationPolicy.translatedCancellationText;

                                                    DataTable dtpolicyparameter = new DataTable();
                                                    dtpolicyparameter.Columns.Add("FromDate");
                                                    dtpolicyparameter.Columns.Add("ToDate");
                                                    dtpolicyparameter.Columns.Add("ChargeAmount");
                                                    dtpolicyparameter.Columns.Add("IsZeroCharge");
                                                    dtpolicyparameter.Columns.Add("DeadLine");
                                                    dtpolicyparameter.Columns.Add("VatdiffAmount");

                                                    string FromDate = "";
                                                    string ToDate = "";
                                                    double ChargeAmount = 0;
                                                    bool IsZeroCharge = true;
                                                    string DeadLine = "";
                                                    double VatdiffAmount = 0;

                                                    DateTime dtCheckInDate = DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null);

                                                    for (int cc = 0; cc < Agoda_NewResponse.properties[h].rooms[ro].cancellationPolicy.parameter.Count; cc++)
                                                    {
                                                        int DaysBefore = Agoda_NewResponse.properties[h].rooms[ro].cancellationPolicy.parameter[cc].days;

                                                        //if (DaysBefore != 0)
                                                        //{
                                                        //    DaysBefore = DaysBefore + cutoff;
                                                        //}
                                                        if (cc == 0)
                                                        {
                                                            if (DaysBefore != 0)
                                                            {
                                                                DaysBefore = DaysBefore + cutoff;
                                                            }
                                                            //if (DaysBefore == 0 || DaysBefore >= 365 || dtCheckInDate.AddDays(-(DaysBefore + 1)).Date <= DateTime.Now.Date)
                                                            if (DaysBefore == 0 || DaysBefore >= 365 || dtCheckInDate.AddDays(-(DaysBefore)).Date <= DateTime.Now.Date)//remove +1 on 13-feb-2024 due to ticket #40169
                                                            {
                                                                DaysBefore = 365;
                                                                FromDate = Convert.ToString(DateTime.Now.Date);
                                                                ToDate = Convert.ToString(dtCheckInDate);
                                                                IsZeroCharge = false;
                                                                //continue; //comment on 13-feb-2024 due to ticket #40169
                                                                break;// added on 15-Feb-2024
                                                            }
                                                            else
                                                            {
                                                                DeadLine = Convert.ToString(dtCheckInDate.AddDays(-(DaysBefore + 1)).AddHours(23).AddMinutes(59).AddSeconds(59));
                                                                FromDate = Convert.ToString(dtCheckInDate.AddDays(-DaysBefore));
                                                                IsZeroCharge = true;
                                                                int NextDays = Convert.ToInt32(DaysBefore);
                                                                if (dtCheckInDate.AddDays(-(NextDays + 1)) > dtCheckInDate.AddDays(-DaysBefore))
                                                                    ToDate = Convert.ToString(dtCheckInDate.AddDays(-(NextDays + 1)).AddHours(23).AddMinutes(59).AddSeconds(59));
                                                                else
                                                                    ToDate = Convert.ToString(dtCheckInDate);
                                                            }
                                                        }
                                                        else
                                                        {
                                                            IsZeroCharge = false;
                                                            //if ((cc + 1) != Agoda_NewResponse.properties[h].rooms[ro].cancellationPolicy.parameter.Count)
                                                            //{
                                                            //    DaysBefore = DaysBefore + cutoff;
                                                            //}
                                                            if ((cc + 1) != Agoda_NewResponse.properties[h].rooms[ro].cancellationPolicy.parameter.Count) //added 28-06-2024 k2 ankit start
                                                            {
                                                                DaysBefore = DaysBefore + cutoff;
                                                                FromDate = Convert.ToString(dtCheckInDate.AddDays(-(DaysBefore == 0 ? 0 : DaysBefore)));
                                                            }
                                                            else
                                                            {
                                                                FromDate = Convert.ToString(dtCheckInDate.AddDays(-(DaysBefore + cutoff == 0 ? 0 : DaysBefore + cutoff))); //end
                                                            }
                                                            //FromDate = Convert.ToString(dtCheckInDate.AddDays(-(DaysBefore == 0 ? 0 : DaysBefore)));
                                                            if (DaysBefore > 0)
                                                            {
                                                                int NextDays = Convert.ToInt32(DaysBefore);
                                                                if (dtCheckInDate.AddDays(-(NextDays + 1)) > dtCheckInDate.AddDays(-DaysBefore))
                                                                    ToDate = Convert.ToString(dtCheckInDate.AddDays(-(NextDays + 1)).AddHours(23).AddMinutes(59).AddSeconds(59));
                                                                else
                                                                    ToDate = Convert.ToString(dtCheckInDate);
                                                            }
                                                            else
                                                                ToDate = Convert.ToString(dtCheckInDate);
                                                        }
                                                        string Charge = Agoda_NewResponse.properties[h].rooms[ro].cancellationPolicy.parameter[cc].charge;
                                                        int PolicyParameter_text = Agoda_NewResponse.properties[h].rooms[ro].cancellationPolicy.parameter[cc].value;
                                                        double CancellationAmount = ChargeTotal;
                                                        if (Charge == "N")//night charge
                                                            CancellationAmount = TotalPrice * PolicyParameter_text;
                                                        else //percent
                                                            CancellationAmount = (ChargeTotal * PolicyParameter_text) / 100;
                                                        #region Vat
                                                        double TotalVatOnPolicy = 0, MainCharge = 0;
                                                        ChargeAmount = _markup.ProcessHotelCancelationPrice_WithMarkup(ref MainCharge, SearchParam.NoofNights, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), CancellationAmount, isPerce, commonSearch.B2CMarkup, AgentPercentage, AgentMarkupAmount, commonSearch.IsParentId, commonSearch.SubAgentMarkup, ref TotalVatOnPolicy);
                                                        VatdiffAmount = TotalVatOnPolicy;
                                                        #endregion
                                                        DataRow drPolicyParameter = dtpolicyparameter.NewRow();
                                                        drPolicyParameter["FromDate"] = FromDate;
                                                        drPolicyParameter["ToDate"] = ToDate;
                                                        drPolicyParameter["ChargeAmount"] = ChargeAmount;
                                                        drPolicyParameter["IsZeroCharge"] = IsZeroCharge;
                                                        drPolicyParameter["DeadLine"] = DeadLine;
                                                        drPolicyParameter["VatdiffAmount"] = TotalVatOnPolicy;
                                                        dtpolicyparameter.Rows.Add(drPolicyParameter);
                                                    }


                                                    //DataRow [] abcd = dtpolicyparameter.Select(" _id=" + dt +"");
                                                    //dtpolicyparameter = drPolicyParameter.Select();
                                                    if (dtpolicyparameter.Rows.Count == 0)
                                                    {
                                                        double TotalVatOnPolicy = 0;
                                                        double TotalVatOnPolic1y = ChargeTotal;
                                                        double Zero = ChargeTotal;

                                                        ChargeAmount = _markup.ProcessHotelCancelationPrice_WithMarkup(ref TotalVatOnPolic1y, SearchParam.Rooms.Count * SearchParam.NoofNights, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), ChargeTotal, isPerce, commonSearch.B2CMarkup, AgentPercentage, AgentMarkupAmount, commonSearch.IsParentId, commonSearch.SubAgentMarkup, ref TotalVatOnPolicy);
                                                        dtCheckInDate = DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null);

                                                        //dtCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref Zero, dtCancellation.NewRow(), CurrencyFactorAgoda, AgodaInfo.AgodaNewCurrency, Provider, Convert.ToString(OwnsystemHotelId), "1", RoomTypeId, IncludedMeal, IncludedMeal, DateTime.Now.Date, (dtCheckInDate), ChargeAmount, ChargeAmount, "", TotalVatOnPolicy, "", "", "", "", "", ""));
                                                        dtCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref Zero, dtCancellation.NewRow(), CurrencyFactorAgoda, Item.CurrencyCode, Provider, Convert.ToString(OwnsystemHotelId), "1", RoomTypeId, IncludedMeal, IncludedMeal, DateTime.Now.Date, (dtCheckInDate), ChargeAmount, ChargeAmount, "", TotalVatOnPolicy, "", "", "", "", "", ""));
                                                        dtCancellation.AcceptChanges();
                                                    }
                                                    else
                                                    {
                                                        for (int can = 0; can < dtpolicyparameter.Rows.Count; can++)
                                                        {
                                                            //if (Agoda_NewResponse.properties[h].rooms[ro].cancellationPolicy.parameter.Count == 3)
                                                            {
                                                                bool isref = Agoda_NewResponse.properties[h].rooms[ro].freeCancellation;
                                                                int Days = Agoda_NewResponse.properties[h].rooms[ro].cancellationPolicy.parameter[can].days;
                                                                string Charge = Agoda_NewResponse.properties[h].rooms[ro].cancellationPolicy.parameter[can].charge;
                                                                int PolicyParameter_text = Agoda_NewResponse.properties[h].rooms[ro].cancellationPolicy.parameter[can].value;
                                                                if (Days >= 365 || isref == false) //non ref
                                                                {
                                                                    IsRefundable = false;
                                                                    break;
                                                                }
                                                                #region

                                                                #endregion
                                                                else
                                                                {
                                                                    bool IsZero = Convert.ToBoolean(dtpolicyparameter.Rows[can]["IsZeroCharge"]);
                                                                    FromDate = Convert.ToString(dtpolicyparameter.Rows[can]["FromDate"]);
                                                                    ToDate = Convert.ToString(dtpolicyparameter.Rows[can]["ToDate"]);
                                                                    ChargeAmount = Convert.ToDouble(dtpolicyparameter.Rows[can]["ChargeAmount"]);
                                                                    DeadLine = Convert.ToString(dtpolicyparameter.Rows[can]["DeadLine"]);
                                                                    //double VatdiffAmount = Convert.ToDouble(drPolicyParameter[can]["VatdiffAmount"]);
                                                                    double Zero = 0, VatonPolicy = 0;
                                                                    if (IsZero)
                                                                    {
                                                                        if (ChargeAmount == 0)
                                                                            dtCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref Zero, dtCancellation.NewRow(), CurrencyFactorAgoda, AgodaInfo.AgodaNewCurrency, Provider, Convert.ToString(OwnsystemHotelId), "1", RoomTypeId, IncludedMeal, IncludedMeal, DateTime.Now.Date, Convert.ToDateTime(FromDate).Date.AddHours(23).AddMinutes(59).AddSeconds(59), 0, 0, "", VatonPolicy, "", "", "", "", "", ""));
                                                                        else
                                                                        {
                                                                            dtCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref Zero, dtCancellation.NewRow(), CurrencyFactorAgoda, AgodaInfo.AgodaNewCurrency, Provider, Convert.ToString(OwnsystemHotelId), "1", RoomTypeId, IncludedMeal, IncludedMeal, DateTime.Now.Date, Convert.ToDateTime(DeadLine), 0, 0, "", VatonPolicy, "", "", "", "", "", ""));

                                                                            dtCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref ChargeAmount, dtCancellation.NewRow(), CurrencyFactorAgoda, AgodaInfo.AgodaNewCurrency, Provider, Convert.ToString(OwnsystemHotelId), "1", RoomTypeId, IncludedMeal, IncludedMeal, Convert.ToDateTime(FromDate), Convert.ToDateTime(ToDate), ChargeAmount, ChargeAmount, "", VatonPolicy, "", "", "", "", "", ""));
                                                                        }
                                                                    }
                                                                    else
                                                                        dtCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref ChargeAmount, dtCancellation.NewRow(), CurrencyFactorAgoda, AgodaInfo.AgodaNewCurrency, Provider, Convert.ToString(OwnsystemHotelId), "1", RoomTypeId, IncludedMeal, IncludedMeal, Convert.ToDateTime(FromDate), Convert.ToDateTime(ToDate), ChargeAmount, ChargeAmount, "", VatonPolicy, "", "", "", "", "", ""));
                                                                    dtCancellation.AcceptChanges();
                                                                    IsRefundable = true;
                                                                }
                                                            }
                                                            if (!IsRefundable)
                                                            {
                                                                double TotalVatOnPolicy = 0;
                                                                double TotalVatOnPolic1y = ChargeTotal;
                                                                double Zero = ChargeTotal;

                                                                ChargeAmount = _markup.ProcessHotelCancelationPrice_WithMarkup(ref TotalVatOnPolic1y, SearchParam.Rooms.Count * SearchParam.NoofNights, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), ChargeTotal, isPerce, commonSearch.B2CMarkup, AgentPercentage, AgentMarkupAmount, commonSearch.IsParentId, commonSearch.SubAgentMarkup, ref TotalVatOnPolicy);
                                                                dtCheckInDate = DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null);

                                                                //dtCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref Zero, dtCancellation.NewRow(), CurrencyFactorAgoda, AgodaInfo.AgodaNewCurrency, Provider, Convert.ToString(OwnsystemHotelId), "1", RoomTypeId, IncludedMeal, IncludedMeal, DateTime.Now.Date, (dtCheckInDate), ChargeAmount, ChargeAmount, "", TotalVatOnPolicy, "", "", "", "", "", ""));
                                                                dtCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref Zero, dtCancellation.NewRow(), CurrencyFactorAgoda, Item.CurrencyCode, Provider, Convert.ToString(OwnsystemHotelId), "1", RoomTypeId, IncludedMeal, IncludedMeal, DateTime.Now.Date, (dtCheckInDate), ChargeAmount, ChargeAmount, "", TotalVatOnPolicy, "", "", "", "", "", ""));
                                                                dtCancellation.AcceptChanges();
                                                            }

                                                            #endregion

                                                        }
                                                    }
                                                    //}
                                                }
                                            }
                                        }
                                    }

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
                else
                {
                    Item.ResponseStatus = 204;
                }


                return options;
            }
            catch (Exception ex)
            {
                Item.ResponseStatus = 404;
                await _exception.Error(ex);
                Item.ResponseType = "NoCount";
                return options;
            }
            //});
            finally
            {
                #region ResProcessTimer
                try
                {
                    timer_room_Agoda.Stop();
                    Item.CodeProcessingTimeSupplier = timer_room_Agoda.ElapsedMilliseconds;
                    if (SearchParam != null && ResProccessTimerLogActive == "1" && timer_room_Agoda.ElapsedMilliseconds > Convert.ToInt32(ResProccessTimeValueforSaveLog))
                    {
                        
                        SearchParam.SupplierTime += ",|Finish:Agoda Process Response  " + "|Time :" + timer_room_Agoda.Elapsed.ToString();
                        _ = Task.Run(() => _commonMethod.FireAway_SQL_ResPorcessTimeLog("", timer_room_Agoda.Elapsed.ToString(), Item.Supplier, SearchParam.SearchKey, 1, SearchParam.AgentID));


                    }
                }
                catch (Exception x)
                {
                    await _exception.Error(x);

                }
                #endregion

                if (Item.ResponseType == "NoCount" && StoreSupplierResErrorLog == "1")
                    await _commonMethod.XMLRequestResponseTrack_Error(Item.RQRS[0].Request, Response, Item.Supplier, timer_room_Agoda.Elapsed.ToString(), "", SearchParam.SearchKey);
            }
        }

        public static string Get_AgodaNew_BenifitName(int Benefit_Id, int NormalBedding, bool addpax = true, string benefitName="") 
        {
            bool Flag = true;
            string Name = "";
            switch (Benefit_Id)
            {
                case 1:
                    Name = "Breakfast";
                    //Flag = false;              
                    break;
                case 2:
                    Name = "Lunch included";
                    break;
                case 3:
                    Name = "Dinner included";
                    break;
                case 20:
                    Name = "Breakfast for 1 person";
                    Flag = false;
                    break;
                case 21:
                    Name = "Iftar";
                    break;
                case 22:
                    Name = "Suhur";
                    break;
                case 26:
                    Name = "Breakfast for 2 people";
                    Flag = false;
                    break;
                case 27:
                    Name = "Buffet dinner";
                    break;
                case 31:
                    Name = "Cruise dinner";
                    break;
                case 40:
                    Name = "Japanese dinner";
                    // Flag = false;
                    break;
                case 41:
                    Name = "Kaiseki dinner";
                    break;
                case 52:
                    Name = "Special meal arrangements";
                    break;
                case 53:
                    Name = "Theme park tickets";
                    //Flag = false;              
                    break;
                case 54:
                    Name = "Western dinner";
                    break;
                case 55:
                    Name = "All Inclusive";
                    break;
                case 57:
                    Name = "2 Complimentary buffet credits";
                    Flag = false;
                    break;
                case 62:
                    Name = "Ferry transfer [both ways]";
                    break;
                case 63:
                    Name = "Ferry transfer [one way]";
                    break;
                case 64:
                    Name = "Food and beverage credit";
                    //Flag = false;              
                    break;
                case 67:
                    Name = "Free meal upgrade (dinner and lunch)";
                    break;
                case 76:
                    Name = "Water park tickets";
                    break;
                case 78:
                    Name = "Complimentary breakfast for 3 guests";
                    Flag = false;
                    break;
                case 84:
                    Name = "Complimentary snacks";
                    break;
                case 86:
                    Name = "In-room dinner";
                    break;
                case 87:
                    Name = "Wagyu beef dinner";
                    //Flag = false;              
                    break;
                case 89:
                    Name = "Local breakfast";
                    break;
                case 90:
                    Name = "Crab dinner";
                    break;
                case 102:
                    Name = "Sunset Cruise";
                    //Flag = false;
                    break;
                case 103:
                    Name = "Dolphin Cruis";
                    break;
                case 104:
                    Name = "Local Island Excursion";
                    break;
                case 105:
                    Name = "Sunset Fishing Excursion";
                    //Flag = false;              
                    break;
                case 106:
                    Name = "Snorkeling Excursion";
                    break;
                case 107:
                    Name = "One Half Day Excursion per Person per Stay";
                    Flag = false;
                    break;
                case 108:
                    Name = "One Night Fishing per Person per Stay";
                    Flag = false;
                    break;
                case 109:
                    Name = "2 Adventure Cove Waterpark Tickets";
                    Flag = false;
                    break;
                case 110:
                    Name = "2 S.E.A. Aquarium Tickets";
                    Flag = false;
                    break;
                case 111:
                    Name = "2 The Maritime Experiential Museum and Typhoon Theater Tickets";
                    Flag = false;
                    break;
                case 112:
                    Name = "2 Universal Studios Singapore Tickets";
                    Flag = false;
                    break;
                case 113:
                    Name = "Basic Kaiseki Dinner";
                    break;
                case 114:
                    Name = "Casual Kaiseki Dinner";
                    //  Flag = false;
                    break;
                case 118:
                    Name = "Premium Kaiseki Dinner";
                    break;
                case 121:
                    Name = "Vegetarian Breakfast";
                    break;
                case 122:
                    Name = "Vegetarian Dinner";
                    //Flag = false;              
                    break;
                case 123:
                    Name = "Dinner in Private roo";
                    break;
                case 139:
                    Name = "2 meals included";
                    Flag = false;

                    break;
                case 190:
                    Name = "Free flow beverages [non-alcoholic] for 1";
                    Flag = false;
                    break;
                case 191:
                    Name = "Free flow beverages [non-alcoholic] for 2";
                    Flag = false;

                    break;
                case 194:
                    Name = "Complimentary tickets";
                    break;
                case 198:
                    Name = "Complimentary Lunch";
                    //Flag = false;
                    break;
                case 199:
                    Name = "Complimentary Dinner";
                    break;
                case 202:
                    Name = "Airport transfer (one way)";
                    Flag = false;
                    break;
                case 203:
                    Name = "Airport transfer (two ways)";
                    Flag = false;
                    break;
                case 206:
                    Name = "Complimentary Breakfast";
                    break;
                case 216:
                    Name = "VIP Only - Free Breakfast";
                    // Flag = false;
                    break;
                case 219:
                    Name = "3 meals daily";
                    //Flag = false;
                    break;
                case 239:
                    Name = "Lunch included";
                    Flag = false;
                    break;
                case 240:
                    Name = "Dinner included";
                    Flag = false;
                    break;
                case 257:
                    Name = "Breakfast";
                    Flag = false;
                    break;
                case 258:
                    Name = "Lunch included";
                    Flag = false;
                    break;
                case 259:
                    Name = "Dinner included";
                    Flag = false;
                    break;
                case 278:
                    //Name = "Breakfast";
                    Name = benefitName; //Added on 27-10-25
                    Flag = false;
                    break;
                case 298:
                    Name = "Speedboat transfer [both ways]";
                    Flag = false;

                    break;
                case 299:
                    Name = "Speedboat transfer [one way]";
                    Flag = false;

                    break;
                case 300:
                    Name = "Seaplane transfer [both ways]";
                    Flag = false;
                    break;
                case 301:
                    Name = "Seaplane transfer [one way]";
                    Flag = false;
                    break;
                case 302:
                    Name = "Domestic Flight & Speedboat transfer [both ways]";
                    Flag = false;

                    break;
                case 303:
                    Name = "Domestic Flight & Speedboat transfer [one way]";
                    Flag = false;
                    break;
                case 308:
                    Name = "VIP only - Free dinner";
                    //Flag = false;
                    break;
                case 309:
                    Name = "Shrimp dinner";
                    break;
                case 310:
                    Name = "Japanese pufferfish dinner";
                    break;
                case 311:
                    Name = "Basic dinner";
                    break;
                case 312:
                    Name = "Special dinner";
                    // Flag = false;
                    break;
                case 313:
                    Name = "Local food dinne";
                    //Flag = false;
                    break;
                case 314:
                    Name = "Hot pot dinner";
                    break;
                case 315:
                    Name = "Buffet breakfast";
                    break;
                case 316:
                    Name = "Breakfast set menu";
                    break;
                case 317:
                    Name = "Breakfast selections";
                    break;
                case 8:
                    Name = "Airport transfer [one way]";
                    Flag = false;
                    break;
                case 9:
                    Name = "Airport transfer [both ways]";
                    Flag = false;
                    break;
                case 12:
                    Name = "Free room upgrade";
                    Flag = false;
                    break;
                default:
                    break;
            }
            if (addpax && Flag && Name != "")
            {
                Name = NormalBedding + " X " + Name;
            }
            return Name;
        }

        public string Get_Agoda_EInfo(string HotelId, string Username, string agodapassword, string platform, string URL, string SupplierName, string SearchKey)
        {
            //string Response = "";
            string Essentialinfo = "";

            try
            {
                string AgodaUrl = URL;
                string HotelInfo = "";
                try
                {
                    string r = AgodaInfo.AgodaFeed + "?apikey=" + agodapassword + "" + "&mhotel_id=" + HotelId + "&feed_id=19";
                    HotelInfo = ""; // XMLHotelBookingCommon.getResponseAgoda(r);
                }
                catch (Exception ex)
                {
                    _ = _exception.ErrorNew(ex, _LogAPI.Recheck, SearchKey);
                }

                //  string HotelInfo = XMLHotelBookingCommon.getResponseAgoda(URL);
                string Remark = "";
                if (HotelInfo != "" && HotelInfo != null && HotelInfo != string.Empty)
                {
                    DataSet dsAgoda = _commonMethod.GetDataSetFromXMLString(HotelInfo);

                    if (dsAgoda != null)
                    {
                        if (dsAgoda.Tables.Contains("hotel") == true)
                        {
                            if (dsAgoda.Tables["hotel"].Columns.Contains("remark") == true)
                                Remark = Convert.ToString(dsAgoda.Tables["hotel"].Rows[0]["remark"]);
                            if (Remark != string.Empty && Remark != "")
                                Essentialinfo += "<p><strong> Hotel Remark: </strong></p>" + Remark;
                        }
                        string desc = "";
                        if (dsAgoda.Tables.Contains("hotel_description") == true)
                        {
                            if (dsAgoda.Tables["hotel_description"].Columns.Contains("overview") == true)
                                desc = Convert.ToString(dsAgoda.Tables["hotel_description"].Rows[0]["overview"]);
                            if (desc != string.Empty && desc != "")
                                Essentialinfo += "<p><strong> Hotel Description: </strong></p>" + desc;
                        }
                        if (dsAgoda.Tables.Contains("child_and_extra_bed_policy") == true)
                        {
                            string infant = "", childFrom = "", childTo = "";/////, childFree = "";
                            if (dsAgoda.Tables["child_and_extra_bed_policy"].Columns.Contains("infant_age") == true)
                                infant = Convert.ToString(dsAgoda.Tables["child_and_extra_bed_policy"].Rows[0]["infant_age"]);
                            if (dsAgoda.Tables["child_and_extra_bed_policy"].Columns.Contains("children_age_from") == true)
                                childFrom = Convert.ToString(dsAgoda.Tables["child_and_extra_bed_policy"].Rows[0]["children_age_from"]);
                            if (dsAgoda.Tables["child_and_extra_bed_policy"].Columns.Contains("children_age_to") == true)
                                childTo = Convert.ToString(dsAgoda.Tables["child_and_extra_bed_policy"].Rows[0]["children_age_to"]);
                            Essentialinfo += "<p><strong> Child and Extra Bed Policy:</strong> </p> <p> Infant : " + infant + " to " + Convert.ToDouble(Convert.ToDouble(childFrom) - 1) + " Stay for free if using existing Bedding <br/> Children From " + childFrom + " To " + childTo + " Must use an Extra Bed<br/> Children above " + Convert.ToDouble(Convert.ToDouble(childTo) + 1) + " Must use an Extra Bed which will incur an additional charge</p>";
                        }
                    }
                }
            }
            catch (Exception Ex)
            {
                _ = _exception.ErrorNew(Ex, _LogAPI.Recheck, SearchKey);
            }
            return Essentialinfo;
        }
        public async Task<string> Get_Agoda_v1_EInfo(string HotelId, string Username, string agodapassword, string platform, string URL, string SupplierName, string RoomTypeId, string SearchKey)
        {
            //string Response = "";
            string essentialinfo = "";

            try
            {
                string AgodaUrl = URL;
                string HotelInfo = "";
                string Request = "";
                string JsonResponse = "";
                try
                {
                    Request = AgodaInfo.AgodaFeed + "?apikey=" + agodapassword + "" + "&mhotel_id=" + HotelId + "&feed_id=19";
                    ResponseDto responseDto = await getResponseAgoda_json(Request);
                    JsonResponse = responseDto.Response;
                }
                catch (Exception ex)
                {
                    _ = _exception.ErrorNew(ex, _LogAPI.Recheck, SearchKey);
                    return "";
                }

                //  string HotelInfo = XMLHotelBookingCommon.getResponseAgoda(URL);
                if (JsonResponse != string.Empty)
                {
                    //DataSet dsAgoda = Common.GetDataSetFromXMLString(HotelInfo);

                    Agoda_NewESS.Root AgodaResponse = new Agoda_NewESS.Root();
                    AgodaResponse = JsonConvert.DeserializeObject<Agoda_NewESS.Root>(JsonResponse);

                    try
                    {

                        try//Added on 7-JUN-2024
                        {
                            string RoomTypeIdTemp = Convert.ToString(RoomTypeId);

                            int RoomTypeIdagoda = Convert.ToInt32(RoomTypeIdTemp.Split('$')[20]);

                            for (int ai = 0; ai < AgodaResponse.hotelFullFeed.roomtypes.roomtype.Count(); ai++)
                            {
                                if (AgodaResponse.hotelFullFeed.roomtypes.roomtype[ai].hotelRoomtypeId == RoomTypeIdagoda)
                                {
                                    if (AgodaResponse.hotelFullFeed.roomtypes.roomtype[ai].views != "")
                                    {
                                        essentialinfo = "<br><strong> Room  Info : " + AgodaResponse.hotelFullFeed.roomtypes.roomtype[ai].views + " </strong> <br>";
                                    }
                                }
                            }
                        }
                        catch (Exception y)
                        {
                            _ = _exception.ErrorNew(y, _LogAPI.Recheck, SearchKey);
                        }

                        string HotelFullFeed = Convert.ToString(AgodaResponse.hotelFullFeed.hotels);
                        string Descriptions = Convert.ToString(AgodaResponse.hotelFullFeed.descriptions);

                        for (int EE = 0; EE < AgodaResponse.hotelFullFeed.hotels.hotel.Count; EE++)
                        {
                            string Remark = AgodaResponse.hotelFullFeed.hotels.hotel[EE].remark;
                            string ChildAndExtrabedPolicy = Convert.ToString(AgodaResponse.hotelFullFeed.hotels.hotel[EE].childAndExtrabedPolicy);
                            if (Convert.ToString(AgodaResponse.hotelFullFeed.hotels.hotel[EE].childAndExtrabedPolicy.childrenStayFree).ToLower() == "true")
                            {
                                essentialinfo += "<p><strong> Child and Extra Bed Policy:</strong> </p> <br>Child Sharing Existing Bed with Parents is Free of Charge, Additional Charges will be applied if Guest require Extrabed or Meal for the The Child to be paid by the Guest at the Property <br>";
                            }
                            else
                            {
                                string infant = AgodaResponse.hotelFullFeed.hotels.hotel[EE].childAndExtrabedPolicy.infantAge;
                                string childFrom = AgodaResponse.hotelFullFeed.hotels.hotel[EE].childAndExtrabedPolicy.childrenAgeFrom;
                                string childTo = AgodaResponse.hotelFullFeed.hotels.hotel[EE].childAndExtrabedPolicy.childrenAgeTo;
                                essentialinfo += "<p><strong> Child and Extra Bed Policy:</strong> </p> <p> Infant : " + infant + " to " + Convert.ToDouble(Convert.ToDouble(childFrom) - 1) + " Stay for free if using existing Bedding <br/> Children From " + childFrom + " To " + childTo + " Must use an Extra Bed<br/> Children above " + Convert.ToDouble(Convert.ToDouble(childTo) + 1) + " Must use an Extra Bed which will incur an additional charge</p>";
                            }
                            if (Remark != string.Empty && Remark != "")
                                essentialinfo += "<p><strong> Hotel Remark: </strong></p>" + Remark;
                        }
                        for (int DD = 0; DD < AgodaResponse.hotelFullFeed.descriptions.description.Count; DD++)
                        {

                            string desc = AgodaResponse.hotelFullFeed.descriptions.description[DD].overview;

                            if (desc != string.Empty && desc != "")
                                essentialinfo += "<p><strong> Hotel Description: </strong></p>" + desc;
                        }
                    }
                    catch (Exception ex)
                    {
                        _ = _exception.ErrorNew(ex, _LogAPI.Recheck, SearchKey);
                        //return null;
                    }
                }
            }
            catch (Exception Ex)
            {
                _ = _exception.ErrorNew(Ex, _LogAPI.Recheck, SearchKey);
            }
            return essentialinfo;
        }

        public async Task<ResponseDto> getResponseAgoda_json(string url)
        {
            ResponseDto responseDto = new ResponseDto();
            string xmlString = "";
            string Error = "";
            try
            {

                var httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
                httpWebRequest.ContentType = "application/json";
                httpWebRequest.ContentLength = 0;
                httpWebRequest.Method = "GET";
                httpWebRequest.Timeout = 5000;//Added on 23-MAY-2025
                HttpWebResponse Response = (HttpWebResponse)await httpWebRequest.GetResponseAsync();
                Stream responseStream = Response.GetResponseStream();
                responseStream = new GZipStream(responseStream, CompressionMode.Decompress);
                //Read the output
                StreamReader Reader = new StreamReader(responseStream, Encoding.UTF8);
                xmlString = await Reader.ReadToEndAsync();
                responseStream.Close();
                Reader.Close();
                Response.Close();

                responseDto.Response = xmlString;
                responseDto.Error = Error;
                return responseDto;
            }
            catch (System.Exception ex)
            {
                responseDto.Response = "";
                responseDto.Error = ex.Message.ToString();
                _exception.Error(ex);
            }
            return responseDto;
        }

        public void PrepareAgoda_NewBeforeBooking_Recheck(DataRow[] AgodaV1Details, ref string checkindate, ref string checkoutdate, ref int Adults,
     ref int Childs, ref int Rooms, ref string AgodaSearchUrl, ref string AgodaSiteID, ref string AgodaApiKey, ref string ChildAge, HotelSearchParameter SearchPara)
        {
            try
            {
                AgodaSiteID = Convert.ToString(AgodaV1Details[0]["UserID"]);
                AgodaApiKey = Convert.ToString(AgodaV1Details[0]["Password"]);
                string AgodaWebUrl = Convert.ToString(AgodaV1Details[0]["WebSiteURL"]);

                AgodaSearchUrl = AgodaInfo.AgodaNewHotelsearchUrl;// 
                checkindate = SearchPara.CheckInDate;
                //checkindate = checkindate.Substring(0, 4) + "-" + checkindate.Substring(4, 2) + "-" + checkindate.Substring(6, 2);
                checkindate = checkindate.Substring(6, 4) + "-" + checkindate.Substring(3, 2) + "-" + checkindate.Substring(0, 2);
                checkoutdate = SearchPara.CheckOutDate;
                //checkoutdate = checkoutdate.Substring(0, 4) + "-" + checkoutdate.Substring(4, 2) + "-" + checkoutdate.Substring(6, 2);
                checkoutdate = checkoutdate.Substring(6, 4) + "-" + checkoutdate.Substring(3, 2) + "-" + checkoutdate.Substring(0, 2);


                for (int rm = 0; rm < SearchPara.Rooms.Count; rm++)
                {
                    Adults += SearchPara.Rooms[rm].NoofAdults;
                    Childs += SearchPara.Rooms[rm].NoofChild;
                }

                string childage = "";
                //for (int PP = 0; PP < drUserCartCount.Length; PP++)
                {
                    string adults = "";
                    string ChildAgeString = "";

                    for (int ser = 0; ser < SearchPara.Rooms.Count; ser++)
                    {
                        switch (Convert.ToInt32(SearchPara.Rooms[ser].NoofChild))
                        {
                            case 1:
                                ChildAgeString = (Convert.ToString(SearchPara.Rooms[ser].Child1Age));
                                ChildAge = ChildAgeString + "," + ChildAge;
                                break;
                            case 2:
                                ChildAgeString = Convert.ToString(SearchPara.Rooms[ser].Child1Age) + "," + Convert.ToString(SearchPara.Rooms[ser].Child2Age);
                                ChildAge = ChildAgeString + "," + ChildAge;
                                break;
                        }
                    }
                    if (ChildAge != "")
                    {
                        ChildAge = ChildAge.Substring(0, ChildAge.Length - 1);
                    }
                }
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
            }

        }

        public string Agoda_NewPriceCheckRQ(string checkin, string checkout, int rooms, int adult, int child, string siteid, string apiKey, string roomtypeID, ref double FinalPrice, int i, string childrenAges)
        {
            try
            {
                string RequestString = "";
                string AgodaLaguage = AgodaInfo.AgodaLaguage;

                string RoomTypeIdTemp = roomtypeID;
                FinalPrice = Convert.ToDouble(RoomTypeIdTemp.Split('$')[13]);
                bool mandatory = Convert.ToBoolean(RoomTypeIdTemp.Split('$')[21]);
                string searchId = RoomTypeIdTemp.Split('$')[11];
                int propertyId = Convert.ToInt32(RoomTypeIdTemp.Split('$')[12]);
                string Currency = RoomTypeIdTemp.Split('$')[17];
                string paymentModel = RoomTypeIdTemp.Split('$')[18];
                string blockId = RoomTypeIdTemp.Split('$')[0];
                double inclusive = Convert.ToDouble(RoomTypeIdTemp.Split('$')[13]);
                int id = Convert.ToInt32(RoomTypeIdTemp.Split('$')[1]);
                double SurchargeInclusive = Convert.ToDouble(RoomTypeIdTemp.Split('$')[3]);
                string userCountry = RoomTypeIdTemp.Split('$')[8];

                //int childage1 = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[0]["ChildAge1"]); // Comment on 7-SEP-2024 due to no use
                //int childage2 = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[0]["ChildAge2"]); // Comment on 7-SEP-2024 due to no use
                //var NoOfChild = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[0]["NoOfChild"]);

                List<int> t = new List<int>();

                if (childrenAges != "")
                {
                    string NoOfChild11 = childrenAges;
                    string[] NoOfChild1 = NoOfChild11.Split(',');

                    for (int ii = 0; ii < (NoOfChild1.Length); ii++)
                    {
                        t.Add(Convert.ToInt16(NoOfChild1[ii]));
                    }
                }

                precheck_new.Rate ObjsurchargeRate = new precheck_new.Rate();
                ObjsurchargeRate.inclusive = SurchargeInclusive;

                List<precheck_new.Surcharge> ObjSurcharges = new List<precheck_new.Surcharge>();
                precheck_new.Surcharge surcharge = new precheck_new.Surcharge();
                surcharge.id = id;
                surcharge.rate = ObjsurchargeRate;

                precheck_new.Rate ObjRate = new precheck_new.Rate();
                ObjRate.inclusive = inclusive;

                List<precheck_new.Room> ObjRoom = new List<precheck_new.Room>();
                precheck_new.Room room = new precheck_new.Room();
                room.currency = Currency;
                room.paymentModel = paymentModel;
                room.blockId = blockId;
                room.count = rooms;
                room.adults = adult;
                room.children = child;

                if (child > 0)
                {
                    room.childrenAges = t;
                }
                room.rate = ObjRate;
                room.surcharges = ObjSurcharges;

                ObjRoom.Add(room);

                precheck_new.Property Objproperty = new precheck_new.Property();
                Objproperty.propertyId = propertyId;
                Objproperty.rooms = ObjRoom;

                precheck_new.PrecheckDetails ObjPrecheckDetails = new precheck_new.PrecheckDetails();
                ObjPrecheckDetails.searchId = Convert.ToInt64(searchId);
                //ObjPrecheckDetails.tag = apiKey;
                ObjPrecheckDetails.tag = Convert.ToString(Guid.NewGuid()).Substring(0, 13); //31-05-2024 Remove API Key K2
                ObjPrecheckDetails.allowDuplication = false;
                ObjPrecheckDetails.checkIn = checkin;
                ObjPrecheckDetails.checkOut = checkout;
                ObjPrecheckDetails.property = Objproperty;
                ObjPrecheckDetails.language = AgodaLaguage;
                ObjPrecheckDetails.userCountry = userCountry;

                precheck_new.Root main = new precheck_new.Root();
                main.precheckDetails = ObjPrecheckDetails;

                JsonSerializerSettings settings = new JsonSerializerSettings();
                settings.NullValueHandling = NullValueHandling.Ignore;
                //settings.DefaultValueHandling = DefaultValueHandling.Ignore;
                RequestString = Newtonsoft.Json.JsonConvert.SerializeObject(main, settings);
                return RequestString;
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
                return string.Empty;
            }
        }

        public async Task<ResponseDto> GetSupplierResponseAgoda_new(string strUniqueId, string strxmlRQRS, string strHost, Int32 TimeOut, bool IsSecure, string Provider = "", HotelSearchParameter para = null, bool DebugMode = false)
        {
            ResponseDto responseDto = new ResponseDto();
            if (strHost == "AgodaNewPreCheckUrl")
                strHost = AgodaInfo.AgodaNewPreCheckUrl;


            System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
            timer.Start();
            string strOutput = "";
            string Error = "";

            try
            {

                HttpWebRequest Request;
                Request = (HttpWebRequest)HttpWebRequest.Create(strHost);
                Request.Timeout = TimeOut;
                Request.Accept = "application/json";
                Request.Method = "POST";
                Request.ContentType = "application/json";
                Request.UserAgent = "Mozilla/4.0 (compatible; MSIE 6.0; MS Web Services Client Protocol 1.1.4322.2300";
                Request.Headers.Add("Accept-Encoding", "gzip,deflat");
                Request.Headers.Add("Authorization", strUniqueId);

                Request.KeepAlive = false;  // ✅ Disable keep-alive to prevent closed connection reuse //Added on 27-10-25
                Request.ProtocolVersion = HttpVersion.Version11; // ✅ Force HTTP/1.1, not HTTP/2 //Added on 27-10-25

                if (IsSecure)
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                Stream newStream = null;
                byte[] data = Encoding.ASCII.GetBytes(strxmlRQRS);
                Request.ContentLength = data.Length;
                newStream = Request.GetRequestStream();
                newStream.Write(data, 0, data.Length);

                try
                {
                    HttpWebResponse Response = (HttpWebResponse)await Request.GetResponseAsync();
                    Stream responseStream = Response.GetResponseStream();
                    if (!IsSecure)
                        responseStream = new GZipStream(responseStream, CompressionMode.Decompress);

                    StreamReader Reader = new StreamReader(responseStream, Encoding.UTF8);
                    strOutput = await Reader.ReadToEndAsync();
                    responseStream.Close();
                    Reader.Close();
                    Response.Close();
                    timer.Stop();

                    responseDto.Response = strOutput;
                    responseDto.Error = Error;
                }
                catch (WebException webExcp)
                {
                    //return "";
                    try
                    {
                        HttpWebResponse httpResponse = (HttpWebResponse)webExcp.Response;
                        if (httpResponse != null && httpResponse.StatusDescription != null)
                        {
                            string result = Convert.ToString(httpResponse.StatusDescription);
                            // Get the response stream
                            Stream responseStream = webExcp.Response.GetResponseStream();

                            // Check if the response uses GZip or Deflate encoding
                            if (httpResponse.ContentEncoding.ToLower().Contains("gzip"))
                            {
                                responseStream = new GZipStream(responseStream, CompressionMode.Decompress);
                            }
                            else if (httpResponse.ContentEncoding.ToLower().Contains("deflate"))
                            {
                                responseStream = new DeflateStream(responseStream, CompressionMode.Decompress);
                            }

                            // Read the decompressed response
                            string result1 = new StreamReader(responseStream).ReadToEnd();
                            Error = result1;

                            responseDto.Response = "";
                            responseDto.Error = Error;

                            return responseDto;
                        }
                    }
                    catch (Exception ex)
                    {
                        Error = ex.Message.ToString();
                        _ = _exception.Error(ex);

                        responseDto.Response = "";
                        responseDto.Error = Error;
                    }

                }
            }
            catch (Exception ex)
            {
                Error = ex.Message.ToString();
                _ = _exception.Error(ex);

                responseDto.Response = "";
                responseDto.Error = Error;
            }



            return responseDto;
        }


        public async Task<ResponseRecheck> CheckAgoda_NewBeforeBooking_Recheck(DataRow[] drSuplierMarkup, HotelSearchParameter SearchParam, string RoomTypeId, string agodapassword,string agodaURL,int AgentId)
        {
            ResponseRecheck responseRecheck = new ResponseRecheck();

            string responsetype = "Success";
            string time = "";
            string Error = "";
            string AgodaSiteID = agodapassword;
            double FinalPrice = 0;
            int i = 0;
            string ChildAge = "";
            string checkindate = "";
            string checkoutdate = "";
            string AgodaApiKey = "";
            int Adults = 0;
            int Childs = 0;
            int Rooms = SearchParam.Rooms.Count;

            int p = 1;
            PrepareAgoda_NewBeforeBooking_Recheck(drSuplierMarkup,ref checkindate, ref checkoutdate, ref Adults, ref Childs, ref Rooms, ref agodaURL, ref AgodaSiteID, ref AgodaApiKey, ref ChildAge, SearchParam);

            string AgodaRequestRoom = Agoda_NewPriceCheckRQ(checkindate, checkoutdate, Rooms, Adults, Childs, "", "", RoomTypeId, ref FinalPrice, i, ChildAge);
            ResponseDto responseDto =await  GetSupplierResponseAgoda_new(AgodaSiteID + ":" + AgodaApiKey, AgodaRequestRoom, "AgodaNewPreCheckUrl", 180000, false, "agoda_chain");

            string AgodaResponseRoom = responseDto.Response;
            Error = responseDto.Error;

            if (AgodaResponseRoom.ToLower() == "" || AgodaResponseRoom.ToLower().Contains("errormessages"))
            {
                responsetype = "Timeout";
            }
            else
            {
                Agoda_newPreCheckResponse.Root DS = new Agoda_newPreCheckResponse.Root();
                try
                {
                    DS = JsonConvert.DeserializeObject<Agoda_newPreCheckResponse.Root>(AgodaResponseRoom);
                }
                catch (Exception ex)
                {
                    await _exception.Error(ex);
                }
                int Status = DS.status;

                if (DS.status == 200)/*Status = 200*/
                {
                    if (Status != 200)
                    {
                        p = 1;

                        FinalPrice = 0;
                    }
                    else
                    {
                        p = 0;
                    }
                }
                else
                {
                    p = 1;
                    FinalPrice = 0;
                }
            }
            if (FinalPrice == 0)
            {
                responsetype = "GeneralError";
            }
            string ISBooking = "NotAvailable";
            if (responsetype == "Success")
            {
                ISBooking = "Available";
            }

            #region SupplierPR Price Check
            try
            {
                string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook("agoda_chain", "PreBook", AgentId, time, responsetype, AgodaRequestRoom, AgodaResponseRoom, Error);
            }
            catch (Exception ex)
            {
                await _exception.Error(ex);
            }
            #endregion

            responseRecheck.Response = "";
            responseRecheck.ISBooking = ISBooking;

            return responseRecheck;
        }

        //Prebook 30-06-25

        #region Old Version Code - Not Used
        public async Task<PriceCheckQTDto> DoAgodaPriceCheck(string supplier, bool IsFromBook, DataTable dtCheckPriceReqStructure, DataRow[] AgodaDetails, DataSet dsCartDetails, DataSet dsAgentDetails, int i, string UniqueNo, string AgentUniqueNo, DataRow[] AgodaSearchConfig, DataTable dtCountryCityMarkup, bool isPerce, double AgentMarkupAmount, bool IsB2C, double B2CMarkup, double SubAgentMarkup, int IsParentId, double FinalPrice, string Searchkey = "", int AgentId = 0)
        {
            PriceCheckQTDto priceCheckCMDto = new PriceCheckQTDto();
            string responsetype = "Success";
            string time = "";
            // string SupplierName = "agoda";
            string SupplierName = supplier;
            string Error = "";
            string AgodaRequestRoom = "";
            string AgodaResponseRoom = "";

            int p = 0;
            try
            {
                string AgodaLaguage = AgodaInfo.AgodaLaguage;
                //string AgodaCurrency = XMLHotelBookingCommon.AgodaNewCurrency;
                string AgodaCurrency = Convert.ToString(AgodaDetails[0]["CurrencyShortName"]); //added on 30-06-25


                string checkindate = "", checkoutdate = "", AgodaSiteID = "", AgodaApiKey = "", XmlHotelId = "", AgodaSearchUrl = "";
                int Rooms = 0, Adults = 0, Childs = 0;
                string ChildAge = "";
                PrepareAgodaBeforeBooking(AgodaDetails, dsCartDetails, i, ref checkindate, ref checkoutdate, ref Adults, ref Childs, ref Rooms,ref XmlHotelId, ref AgodaSearchUrl, ref AgodaSiteID, ref AgodaApiKey);
                //PrepareAgoda_NewBeforeBooking_Recheck(AgodaDetails, ref checkindate, ref checkoutdate, ref Adults, ref Childs, ref Rooms, ref AgodaSearchUrl, ref AgodaSiteID, ref AgodaApiKey, ref ChildAge, SearchParam);


                string RoomTypeIdTemp1 = Convert.ToString(dsCartDetails.Tables[1].DefaultView[0]["RoomTypeId"]);

                AgodaRequestRoom = AgodaPriceCheckRQ(checkindate, checkoutdate, Rooms, Adults, Childs, AgodaSiteID, AgodaApiKey, AgodaLaguage, AgodaCurrency, 1000, XmlHotelId, RoomTypeIdTemp1, ref FinalPrice);
                //AgodaRequestRoom = Agoda_NewPriceCheckRQ(checkindate, checkoutdate, Rooms, Adults, Childs, "", "", RoomTypeIdTemp1, ref FinalPrice, i, ChildAge);


                if (!IsFromBook)
                    dtCheckPriceReqStructure.Rows.Add(_commonMethod.AddCheckPriceReq(dtCheckPriceReqStructure.NewRow(), 1, Convert.ToInt32(dsCartDetails.Tables[0].Rows[i]["UserCartID"]), Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[0]["UserCartDetailId"]), Convert.ToString(dsCartDetails.Tables[1].DefaultView[0]["RoomTypeID"]), Convert.ToString(dsCartDetails.Tables[1].DefaultView[0]["SupplierName"]).ToLower(), AgodaRequestRoom));

                _commonMethod.ThirdPartyBookingHistory(Convert.ToString(UniqueNo), Convert.ToString(AgentUniqueNo), AgodaRequestRoom, "", "Before Request hit PriceCheck", Convert.ToString(dsCartDetails.Tables[1].DefaultView[0]["SupplierName"]), "Response Confirm", "-", "WithinEarth");


                System.Diagnostics.Stopwatch timerroomprocess = new System.Diagnostics.Stopwatch();
                timerroomprocess.Start();

                //AgodaResponseRoom = GetSupplierResponseAgoda_report(ref Error, AgodaSiteID + ":" + AgodaApiKey, AgodaRequestRoom, XMLHotelBookingCommon.AgodaPreCheckUrl, 180000, false, supplier, null, false, Searchkey);
                ResponseDto responseDto = await GetSupplierResponseAgoda_new(AgodaSiteID + ":" + AgodaApiKey, AgodaRequestRoom, "AgodaNewPreCheckUrl", 180000, false, supplier);
                AgodaResponseRoom = responseDto.Response;
                Error = responseDto.Error;

                timerroomprocess.Stop();
                time = timerroomprocess.Elapsed.ToString();

                _commonMethod.ThirdPartyBookingHistory(Convert.ToString(UniqueNo), Convert.ToString(AgentUniqueNo), AgodaRequestRoom, AgodaResponseRoom + Error, "After Request hit PriceCheck", Convert.ToString(dsCartDetails.Tables[1].DefaultView[0]["SupplierName"]), "Response Confirm", "-", "WithinEarth");
                if (AgodaResponseRoom.ToLower() == "" || AgodaResponseRoom.ToLower().Contains("errormessages"))
                {

                }
                else
                {
                    DataSet DsAgoda = _commonMethod.GetDataSetFromXMLString(AgodaResponseRoom);
                    #region Old Logic
                    //string RoomTypeIdTemp = Convert.ToString(dsCartDetails.Tables[1].DefaultView[0]["RoomTypeId"]);
                    //string AgodaNationality = RoomTypeIdTemp.Split('$')[1];
                    //string RoomTypeId = RoomTypeIdTemp.Split('$')[0];
                    //DataRow[] drRoom = DsAgoda.Tables["Room"].Select("blockid='" + RoomTypeId + "'");
                    //if (drRoom.Length > 0)
                    //{
                    //    string Room_Id = Convert.ToString(drRoom[0]["Room_Id"]);
                    //    DataRow[] drRateInfo = DsAgoda.Tables["RateInfo"].Select("Room_Id='" + Room_Id + "'");
                    //    if (drRateInfo.Length > 0)
                    //    {
                    //        string RateInfo_Id = Convert.ToString(drRateInfo[0]["RateInfo_Id"]);
                    //        DataRow[] drRate = DsAgoda.Tables["Rate"].Select("RateInfo_Id='" + RateInfo_Id + "'");
                    //        double inclusive = 0;
                    //        if (drRate.Length > 0)
                    //            inclusive = Convert.ToDouble(drRate[0]["inclusive"]);
                    //        StringBuilder strSurcharge = new StringBuilder();
                    //        if (DsAgoda.Tables.Contains("Surcharges"))
                    //        {
                    //            DataRow[] drSurcharges = DsAgoda.Tables["Surcharges"].Select("RateInfo_Id='" + RateInfo_Id + "'");
                    //            if (drSurcharges.Length > 0)
                    //            {
                    //                DataRow[] drSurcharge = DsAgoda.Tables["Surcharge"].Select("Surcharges_Id='" + drSurcharges[0]["Surcharges_Id"] + "'");
                    //                if (drSurcharge.Length > 0)
                    //                {
                    //                    for (int sur = 0; sur < drSurcharge.Length; sur++)
                    //                    {
                    //                        string surcharge_charge = "";
                    //                        surcharge_charge = Convert.ToString(drSurcharge[sur]["charge"]);
                    //                        string Surcharge_Id = Convert.ToString(drSurcharge[sur]["Surcharge_Id"]);

                    //                        DataRow[] drSurchargeRate = DsAgoda.Tables["Rate"].Select("Surcharge_Id='" + Surcharge_Id + "'");
                    //                        if (drSurchargeRate.Length > 0)
                    //                        {
                    //                            double surcharge_inclusive = 0;
                    //                            surcharge_inclusive = Convert.ToDouble(drSurchargeRate[0]["inclusive"]);
                    //                            switch (surcharge_charge.ToLower())
                    //                            {
                    //                                case "mandatory":
                    //                                    inclusive = inclusive + surcharge_inclusive;
                    //                                    break;
                    //                                default:
                    //                                    break;
                    //                            }
                    //                        }
                    //                    }

                    //                }
                    //            }
                    //        }
                    //        if (Convert.ToDouble(Math.Round(inclusive, 2)) > Convert.ToDouble(dsCartDetails.Tables[1].DefaultView[0]["CompanyBuyingPrice"]))
                    //        {
                    //            #region "Price update logic with markup"
                    //            double UpdatedPrice = Convert.ToDouble(inclusive);
                    //            #region "Markup Logic Supplierwise"
                    //            double AgentPercentage = 0.0;
                    //            Boolean blnIsShowSupplier = false;
                    //            int SupplierID = 0;
                    //            if (dsAgentDetails.Tables[1] != null)
                    //            {
                    //                DataRow[] drSupRow = null;
                    //                foreach (DataRow row in dsAgentDetails.Tables[1].Rows)
                    //                {
                    //                    if (Convert.ToString(row["SupplierName"]).ToLower().Replace(" ", "") == Convert.ToString("agoda").ToLower())
                    //                    {
                    //                        drSupRow = new DataRow[1];
                    //                        drSupRow[0] = row;
                    //                        break;
                    //                    }
                    //                }
                    //                if (drSupRow != null)
                    //                {
                    //                    AgentPercentage = Convert.ToDouble(drSupRow[0]["AgentWiseMarkup"]);
                    //                    blnIsShowSupplier = (Convert.ToInt16(drSupRow[0]["IsShowSupplier"]) == 1 ? true : false);
                    //                    SupplierID = Convert.ToInt32(drSupRow[0]["SupplierID"]);
                    //                }
                    //            }

                    //            #endregion
                    //            //if (IsB2C == true)
                    //            //	AgentPercentage = 0;

                    //            #region "HOtelWise markup "
                    //            //Take rating from session
                    //            Int32 hotelRating = 0;
                    //            DataSet dsTempHotel = (DataSet)HttpContext.Current.Session["HotelData"];
                    //            if (dsTempHotel != null && dsTempHotel.Tables.Count > 0 && dsTempHotel.Tables[0].Rows.Count > 0)
                    //            {
                    //                dsTempHotel.Tables[0].DefaultView.RowFilter = "OwnsystemHotelId=" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[0]["ServiceId"]);
                    //                if (dsTempHotel.Tables[0].DefaultView.Count > 0)
                    //                    hotelRating = Convert.ToInt32(dsTempHotel.Tables[0].DefaultView[0]["Rating"]);
                    //            }
                    //            //  Common.getAgetMarkupSetting("Agoda", Convert.ToString(dsCartDetails.Tables[1].DefaultView[0]["ServiceId"]), dsAgentDetails.Tables[1], dtCountryCityMarkup, ref AgentPercentage, ref AgentMarkupAmount, ref blnIsShowSupplier, ref isPerce, getAgentID(), hotelRating, search.CityID, objSearPera.CountryId);
                    //            //if (dtCountryCityMarkup != null && dtCountryCityMarkup.Rows.Count > 0)
                    //            //{
                    //            //	DataRow[] drHotelMarkup = dtCountryCityMarkup.Select("RaynaID='" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[0]["ServiceId"]) + "'and SupplierID='" + SupplierID + "' ");
                    //            //	if (drHotelMarkup.GetLength(0) > 0)
                    //            //	{
                    //            //		if (Convert.ToBoolean(drHotelMarkup[0]["IsPercentage"]) && Convert.ToDouble(drHotelMarkup[0]["MarkupAmount"]) > 0)
                    //            //		{
                    //            //			AgentPercentage = Convert.ToDouble(drHotelMarkup[0]["MarkupAmount"]);
                    //            //			isPerce = true;
                    //            //		}
                    //            //		else if (!Convert.ToBoolean(drHotelMarkup[0]["IsPercentage"]) && Convert.ToDouble(drHotelMarkup[0]["MarkupAmount"]) > 0)
                    //            //		{
                    //            //			isPerce = false;
                    //            //			AgentMarkupAmount = Convert.ToDouble(drHotelMarkup[0]["MarkupAmount"]);
                    //            //		}
                    //            //	}
                    //            //}
                    //            #endregion


                    //            #region "Price update logic with markup"
                    //            int BookNight = Convert.ToInt32(dsCartDetails.Tables[0].Rows[i]["NoOfNight"]);

                    //            Common.ProcessPricePoupTable(false, 0, 0, "USD", dsCartDetails.Tables[1].DefaultView.Count * BookNight, 0, "", ref dtPriceDetail, dsAgentDetails, IsB2C, isPerce, dtCountryCityMarkup, dsCartDetails, AgentMarkupAmount, UpdatedPrice, B2CMarkup, SubAgentMarkup, IsParentId, AgentPercentage, 0);
                    //            #endregion
                    //            #endregion
                    //            #region "Logic for update cart data "
                    //            double CurerntPrice = Convert.ToDouble(inclusive);
                    //            Common.ProcessPriceTable(dsCartDetails.Tables[1].DefaultView.Count * BookNight, 0, "", ref dtPriceUpdateTable, ref serviceTot, CurerntPrice, isPerce, AgentMarkupAmount, AgentPercentage, SubAgentMarkup, IsParentId, B2CMarkup, dsCartDetails, 0, "agoda");
                    //            #endregion
                    //        }
                    //    }
                    //}
                    #endregion
                    if (DsAgoda.Tables.Contains("PrecheckResponse") == true)
                    {
                        string status = Convert.ToString(DsAgoda.Tables["PrecheckResponse"].Rows[0]["status"]);
                        if (status != "200")
                        {
                            p = 1;
                            FinalPrice = 0;
                        }
                        else p = 0;
                    }
                    else
                    {
                        p = 1;
                        FinalPrice = 0;
                    }
                }
            }
            catch (Exception ex)
            {
                p = 1; 
                FinalPrice = 0;
                _ = _exception.Error(ex, Searchkey);
                #region SupplierPerformanceReport Catch
                try
                {
                    Error = ex.Message + ex.StackTrace.ToString();
                    responsetype = "GeneralError";
                    string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, AgodaRequestRoom, AgodaResponseRoom, Error);
                }
                catch (Exception e)
                {
                    _ = _exception.ErrorBooking(e, AgentId.ToString(), "", "");
                }
                #endregion
            }
            if (FinalPrice == 0)
            {
                try
                {
                    responsetype = "GeneralError";
                    string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, AgodaRequestRoom, AgodaResponseRoom, Error);
                }
                catch (Exception e)
                {
                    _ = _exception.Error(e, Searchkey);
                }
            }
            else
            {
                try
                {
                    responsetype = "Success";
                    string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, AgodaRequestRoom, AgodaResponseRoom, Error);
                }
                catch (Exception e)
                {
                    _ = _exception.Error(e, Searchkey);
                }
            }
            //return p;

            priceCheckCMDto.FinalPrice = FinalPrice;
            priceCheckCMDto.Flag = p;
            return priceCheckCMDto;
        }

        public void PrepareAgodaBeforeBooking(DataRow[] AgodaDetails, DataSet dsCartDetails, int i, ref string checkindate, ref string checkoutdate, ref int Adults,
        ref int Childs, ref int Rooms, ref string XmlHotelId, ref string AgodaSearchUrl, ref string AgodaSiteID, ref string AgodaApiKey)
        {
            try
            {
                AgodaSiteID = Convert.ToString(AgodaDetails[0]["UserID"]);
                AgodaApiKey = Convert.ToString(AgodaDetails[0]["Password"]);
                string AgodaWebUrl = Convert.ToString(AgodaDetails[0]["WebSiteURL"]);

                AgodaSearchUrl = AgodaWebUrl + "/XmlService/search_lrv3";
                checkindate = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["CheckInDate"]);
                checkindate = checkindate.Substring(0, 4) + "-" + checkindate.Substring(4, 2) + "-" + checkindate.Substring(6, 2);
                checkoutdate = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["CheckOutDate"]);
                checkoutdate = checkoutdate.Substring(0, 4) + "-" + checkoutdate.Substring(4, 2) + "-" + checkoutdate.Substring(6, 2);
                XmlHotelId = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["xmlHotelId"]);

                for (int rm = 0; rm < dsCartDetails.Tables[1].DefaultView.Count; rm++)
                {
                    Adults += Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[rm]["NoOfAdults"]);
                    Childs += Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[rm]["NoOfChild"]);
                }

                Rooms = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView.Count);

            }
            catch (Exception ex)
            {
                _ = _exception.Error(ex);
            }

        }

        public string AgodaPriceCheckRQ(string checkin, string checkout, int rooms, int adult, int child, string siteid, string apiKey, string language, string currency, int timeout, string hotelID, string roomtypeID, ref double FinalPrice)
        {
            try
            {
                string RoomTypeIdTemp = roomtypeID;
                FinalPrice = Convert.ToDouble(RoomTypeIdTemp.Split('$')[11]);
                StringBuilder sb = new StringBuilder();
                sb.Append("<PrecheckRequest siteid=\"" + siteid + "\" apikey=\"" + apiKey + "\" xmlns=\"http://xml.agoda.com\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\">");
                sb.Append("<PrecheckDetails searchid=\"" + RoomTypeIdTemp.Split('$')[10] + "\" tag=\"" + apiKey + "\" CheckIn=\"" + checkin + "\" CheckOut=\"" + checkout + "\">"); // tag=\"00000000 - 0000 - 0000 - 0000 - 000000000000\" AllowDuplication=\"true\"
                sb.Append("<Hotel id=\"" + hotelID + "\">");
                sb.Append("<Rooms>");
                sb.Append("<Room id=\"" + RoomTypeIdTemp.Split('$')[2] + "\" name=\"" + RoomTypeIdTemp.Split('$')[3] + "\" lineitemid=\"" + RoomTypeIdTemp.Split('$')[4] + "\" rateplan=\"" + RoomTypeIdTemp.Split('$')[5] + "\" ratetype=\"" + RoomTypeIdTemp.Split('$')[6] + "\" currency=\"" + RoomTypeIdTemp.Split('$')[7] + "\" model=\"" + RoomTypeIdTemp.Split('$')[8] + "\" ratecategoryid=\"" + RoomTypeIdTemp.Split('$')[9] + "\" blockid=\"" + RoomTypeIdTemp.Split('$')[0] + "\" count=\"" + rooms + "\" adults=\"" + adult + "\" children=\"" + child + "\">");
                sb.Append("<Rate exclusive=\"" + RoomTypeIdTemp.Split('$')[12] + "\" tax=\"" + RoomTypeIdTemp.Split('$')[13] + "\" fees=\"" + RoomTypeIdTemp.Split('$')[14] + "\" inclusive=\"" + RoomTypeIdTemp.Split('$')[11] + "\"/>");
                sb.Append("</Room></Rooms></Hotel></PrecheckDetails></PrecheckRequest>");
                return sb.ToString();
            }
            catch (Exception ex)
            {
                _ = _exception.Error(ex);
                return string.Empty;
            }
        }

        #endregion
        public async Task<PriceCheckDto> DoAgoda_NewPriceCheck(string supplier,bool IsFromBook, DataTable dtCheckPriceReqStructure, DataTable dtPriceUpdateTable, DataTable dtPriceDetail ,double serviceTot, DataRow[] AgodaV1Details, DataSet dsCartDetails, DataSet dsAgentDetails, int i, string UniqueNo, string AgentUniqueNo, DataRow[] AgodaSearchConfig, DataTable dtCountryCityMarkup, bool isPerce, double AgentMarkupAmount, bool IsB2C, double B2CMarkup, double SubAgentMarkup, int IsParentId, double FinalPrice, int AgentId)
        {
            PriceCheckDto priceCheckDto = new PriceCheckDto();

            int p = 0;
            string responsetype = "Success";
            string time = "";
            string Error = "";
            string AgodaRequestRoom = "";
            string AgodaResponseRoom = "";
            //int AgentIdgta = Convert.ToInt32(Common.getSession("UserId") == "" ? "0" : Common.getSession("UserId"));
            try
            {
                string AgodaLaguage = AgodaInfo.AgodaLaguage;
                string AgodaCurrency = Convert.ToString(AgodaV1Details[0]["CurrencyShortName"]);
                string checkindate = "", checkoutdate = "", AgodaSiteID = "", AgodaApiKey = "", XmlHotelId = "", AgodaSearchUrl = "", ChildAge = "";
                int Rooms = 0, Adults = 0, Childs = 0;

                PrepareAgoda_NewBeforeBooking(AgodaV1Details, dsCartDetails, i, ref checkindate, ref checkoutdate, ref Adults, ref Childs, ref Rooms,
                    ref XmlHotelId, ref AgodaSearchUrl, ref AgodaSiteID, ref AgodaApiKey, ref ChildAge);
                string RoomTypeIdTemp1 = Convert.ToString(dsCartDetails.Tables[1].DefaultView[0]["RoomTypeId"]);
                //AgodaRequestRoom = Agoda_NewPriceCheckRQ(dsCartDetails, checkindate, checkoutdate, Rooms, Adults, Childs, AgodaSiteID, AgodaApiKey, AgodaLaguage, AgodaCurrency, 1000, XmlHotelId, RoomTypeIdTemp1, ref FinalPrice, i, ChildAge);
                AgodaRequestRoom = Agoda_NewPriceCheckRQ(checkindate, checkoutdate, Rooms, Adults, Childs, AgodaSiteID, AgodaApiKey, RoomTypeIdTemp1, ref FinalPrice, i, ChildAge);//Remove unuse para on 7-SEP-2024
                if (!IsFromBook)
                    dtCheckPriceReqStructure.Rows.Add(_commonMethod.AddCheckPriceReq(dtCheckPriceReqStructure.NewRow(), 1, Convert.ToInt32(dsCartDetails.Tables[0].Rows[i]["UserCartID"]), Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[0]["UserCartDetailId"]), Convert.ToString(dsCartDetails.Tables[1].DefaultView[0]["RoomTypeID"]), Convert.ToString(dsCartDetails.Tables[1].DefaultView[0]["SupplierName"]).ToLower(), AgodaRequestRoom));

                _commonMethod.ThirdPartyBookingHistory(Convert.ToString(UniqueNo), Convert.ToString(AgentUniqueNo), AgodaRequestRoom, "", "Before Request hit PriceCheck", Convert.ToString(dsCartDetails.Tables[1].DefaultView[0]["SupplierName"]), "Response Confirm", "-", "WithinEarth");


                System.Diagnostics.Stopwatch timerroomprocess = new System.Diagnostics.Stopwatch();
                timerroomprocess.Start();

                ResponseDto responseDto = await GetSupplierResponseAgoda_new(AgodaSiteID + ":" + AgodaApiKey, AgodaRequestRoom, AgodaInfo.AgodaNewPreCheckUrl, 180000, false, supplier);
                AgodaResponseRoom = responseDto.Response;
                Error = responseDto.Error;

                timerroomprocess.Stop();
                time = timerroomprocess.Elapsed.ToString();

                _commonMethod.ThirdPartyBookingHistory(Convert.ToString(UniqueNo), Convert.ToString(AgentUniqueNo), AgodaRequestRoom, AgodaResponseRoom, "After Request hit PriceCheck", Convert.ToString(dsCartDetails.Tables[1].DefaultView[0]["SupplierName"]), "Response Confirm", "-", "WithinEarth");
                if (AgodaResponseRoom.ToLower() == "" || AgodaResponseRoom.ToLower().Contains("errormessages"))
                {
                    responsetype = "Timeout";
                }
                else
                {
                    Agoda_newPreCheckResponse.Root DS = new Agoda_newPreCheckResponse.Root();
                    try
                    {
                        DS = JsonConvert.DeserializeObject<Agoda_newPreCheckResponse.Root>(AgodaResponseRoom);
                    }
                    catch (Exception ex)
                    {
                        _ = _exception.Error(ex);
                    }
                    int Status = DS.status;

                    if (DS.status == 200)/*Status = 200*/
                    {
                        if (Status != 200)
                        {
                            p = 1;

                            FinalPrice = 0;
                            
                        }
                        else
                        {
                            p = 0;
                        }
                    }
                    else
                    {
                        p = 1;
                        FinalPrice = 0;
                    }
                }
                if (FinalPrice == 0)
                {
                    responsetype = "GeneralError";
                }
                #region SupplierPR Price Check
                try
                {
                    string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(Convert.ToString(dsCartDetails.Tables[1].DefaultView[0]["SupplierName"]), "PreBook", AgentId, time, responsetype, AgodaRequestRoom, AgodaResponseRoom, Error);
                }
                catch (Exception ex)
                {
                    _ = _exception.Error(ex);
                }
                #endregion
            }
            catch (Exception ex)
            {
                p = 1; FinalPrice = 0;
                #region SupplierPerformanceReport Catch
                try
                {
                    Error = ex.Message + ex.StackTrace.ToString();
                    responsetype = "GeneralError";
                    string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(Convert.ToString(dsCartDetails.Tables[1].DefaultView[0]["SupplierName"]), "PreBook", AgentId, time, responsetype, AgodaRequestRoom, AgodaResponseRoom, Error);
                }
                catch (Exception e)
                {
                    _ = _exception.Error(e);
                }
                #endregion
                _ = _exception.Error(ex);
            }

            priceCheckDto.FinalPrice = FinalPrice;
            priceCheckDto.Flag = p;
            return priceCheckDto;
        }

        public void PrepareAgoda_NewBeforeBooking(DataRow[] AgodaV1Details, DataSet dsCartDetails, int i, ref string checkindate, ref string checkoutdate, ref int Adults,ref int Childs, ref int Rooms, ref string XmlHotelId, ref string AgodaSearchUrl, ref string AgodaSiteID, ref string AgodaApiKey, ref string ChildAge)
        {
            try
            {
                AgodaSiteID = Convert.ToString(AgodaV1Details[0]["UserID"]);
                AgodaApiKey = Convert.ToString(AgodaV1Details[0]["Password"]);
                string AgodaWebUrl = Convert.ToString(AgodaV1Details[0]["WebSiteURL"]);

                //AgodaSearchUrl = AgodaWebUrl + "/XmlService/search_lrv3";
                AgodaSearchUrl = AgodaInfo.AgodaNewHotelsearchUrl;// "https://affiliateapi7643.agoda.com/api/v4/property/availability";//AgodaWebUrl + "/api/v4/property/availability";
                checkindate = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["CheckInDate"]);
                checkindate = checkindate.Substring(0, 4) + "-" + checkindate.Substring(4, 2) + "-" + checkindate.Substring(6, 2);
                checkoutdate = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["CheckOutDate"]);
                checkoutdate = checkoutdate.Substring(0, 4) + "-" + checkoutdate.Substring(4, 2) + "-" + checkoutdate.Substring(6, 2);
                XmlHotelId = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["xmlHotelId"]);

                for (int rm = 0; rm < dsCartDetails.Tables[1].DefaultView.Count; rm++)
                {
                    Adults += Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[rm]["NoOfAdults"]);
                    Childs += Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[rm]["NoOfChild"]);
                }
                int UserCartId = Convert.ToInt32(dsCartDetails.Tables[0].Rows[i]["UserCartID"]);
                DataRow[] drUserCartCount = dsCartDetails.Tables[1].Select("UserCartId='" + UserCartId + "'");
                Rooms = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView.Count);
                string childage = "";
                //for (int PP = 0; PP < drUserCartCount.Length; PP++)
                {
                    string adults = "";
                    string ChildAgeString = "";

                    for (int ser = 0; ser < drUserCartCount.Length; ser++)
                    {
                        switch (Convert.ToInt32(drUserCartCount[ser]["NoOfChild"]))
                        {
                            case 1:
                                ChildAgeString = (Convert.ToString(drUserCartCount[ser]["ChildAge1"]));
                                ChildAge = ChildAgeString + "," + ChildAge;
                                break;
                            case 2:
                                ChildAgeString = Convert.ToString(drUserCartCount[ser]["ChildAge1"]) + "," + Convert.ToString(drUserCartCount[ser]["ChildAge2"]);
                                ChildAge = ChildAgeString + "," + ChildAge;
                                break;
                        }
                    }
                    if (ChildAge != "")
                    {
                        ChildAge = ChildAge.Substring(0, ChildAge.Length - 1);
                    }
                }
            }
            catch (Exception ex)
            {
                _ = _exception.Error(ex);
            }

        }

        public string GetAgoda_NewSearchRequest_BeforeBooking(string CheckInDate, string CheckInOut, int Rooms, int Adult, int child, string AgodaSiteID, string AgodaApiKey, string AgodaLaguage, string AgodaCurrency, int SearchTimeout, string HotelIds, string Nationality, string ChildAge)
        {
            try
            {
                StringBuilder AGNEW = new StringBuilder();
                AGNEW.Append("{");
                AGNEW.Append("\"waitTime\":" + SearchTimeout + ",");
                AGNEW.Append("\"criteria\":");
                AGNEW.Append("{");
                //AGNEW.Append("\"propertyIds\":" + HotelIds +  "\" ");
                AGNEW.Append("\"propertyIds\":");
                AGNEW.Append("[");
                //if (HotelIds.Split(',').Length > 1)
                AGNEW.Append(" " + HotelIds + " ");
                AGNEW.Append("],");
                AGNEW.Append("\"checkIn\":\"" + CheckInDate + "\",");
                AGNEW.Append("\"checkOut\":\"" + CheckInOut + "\",");
                AGNEW.Append("\"rooms\":" + Rooms + ",");
                AGNEW.Append("\"adults\":" + Adult + ",");
                AGNEW.Append("\"children\":" + child + ",");
                if (child > 0)
                {
                    AGNEW.Append("\"childrenAges\":");
                    AGNEW.Append("[");
                    AGNEW.Append(ChildAge);
                    AGNEW.Append("],");
                }
                AGNEW.Append("\"language\":\"" + AgodaLaguage + "\",");
                AGNEW.Append("\"currency\":\"" + AgodaCurrency + "\",");
                AGNEW.Append("\"userCountry\":\"" + Nationality + "\" ");
                AGNEW.Append("},");
                AGNEW.Append("\"features\":");
                AGNEW.Append("{");
                //AGNEW.Append("\"ratesPerProperty\":\"" + "\" ");
                AGNEW.Append("\"ratesPerProperty\":25,");
                AGNEW.Append("\"extra\":");
                AGNEW.Append("[");
                AGNEW.Append("\"content\",");
                AGNEW.Append("\"surchargeDetail\",");
                AGNEW.Append("\"cancellationDetail\",");
                AGNEW.Append("\"benefitDetail\",");
                AGNEW.Append("\"dailyRate\",");
                AGNEW.Append("\"taxDetail\",");
                AGNEW.Append("\"rateDetail\",");
                AGNEW.Append("\"promotionDetail\"");
                AGNEW.Append("]");
                AGNEW.Append("}");
                AGNEW.Append("}");
                string AgodaNewString = AGNEW.ToString();
                return AgodaNewString;
            }
            catch (Exception ex)
            {
                _ = _exception.Error(ex);
                return "";
            }
        }

        public string GetAgoda_NewBookingRequest(ref bool isTrue, string GetRoomResponse, DataSet dsCartDetails, DataTable dtPassengers, string AgodaSiteID, string AgodaApiKey, string RoomTypeId, string XmlHotelId, string checkindate, string checkoutdate, string AgodaCurrency, int Adults, int Childs, int Rooms, string Nationality, int i, int j, string RefNo, ref string ChildAge)
        {
            string RequestString = "";
            try
            {
                int UserCartId = Convert.ToInt32(dsCartDetails.Tables[0].Rows[i]["UserCartID"]);
                DataRow[] drUserCartCount = dsCartDetails.Tables[1].Select("UserCartId='" + UserCartId + "'");
                Rooms = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView.Count);
                bool mandatory = false;//Convert.ToBoolean(RoomTypeIdTemp.Split('$')[21]);
                int Id = 0;
                double surcharge_inclusive = 0;

                int childage1 = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[0]["ChildAge1"]);
                int childage2 = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[0]["ChildAge2"]);
                var NoOfChild = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[0]["NoOfChild"]);

                List<int> t = new List<int>();

                if (ChildAge != "")
                {
                    string NoOfChild11 = ChildAge;
                    string[] NoOfChild1 = NoOfChild11.Split(',');

                    for (int ii = 0; ii < (NoOfChild1.Length); ii++)
                    {
                        t.Add(Convert.ToInt16(NoOfChild1[ii]));
                    }
                }


                int Year = Convert.ToInt32(DateTime.Now.Year) + 2;
                string AgodaLaguage = AgodaInfo.AgodaLaguage;
                string ExpriMonth = AgodaInfo.creditCardExpirationMonth_Agoda;
                string ExpriYear = Convert.ToString(Year);//XMLHotelBookingCommon.creditCardExpirationYear_Agoda;
                string roomtypeid = Convert.ToString(dsCartDetails.Tables[1].DefaultView[0]["RoomTypeId"]);
                List<Agoda_newBookingAvalibility.Room> RoomFilter = null;
                List<Agoda_newBookingRQ.Surcharge> ObjSurcharge = new List<Agoda_newBookingRQ.Surcharge>();


                if (GetRoomResponse != "")
                {
                    Agoda_newBookingAvalibility.Root AgodaResponse = JsonConvert.DeserializeObject<Agoda_newBookingAvalibility.Root>(GetRoomResponse);
                    RoomFilter = AgodaResponse.properties[0].rooms.Where(item => item.blockId == RoomTypeId).ToList();

                    //string searchId = Convert.ToString(AgodaResponse.searchId);
                    //string PaymentModel = RoomFilter[0].paymentModel;// AgodaResponse.properties[0].rooms[0].paymentModel;
                    //double Inclusive = RoomFilter[0].rate.inclusive;// AgodaResponse.properties[0].rooms[0].rate.inclusive;


                    //Agoda_newBookingRQ.Rate ObjRate = new Agoda_newBookingRQ.Rate();
                    //ObjRate.inclusive = surcharge_inclusive;

                    //List<Agoda_newBookingRQ.Surcharge> ObjSurcharge = new List<Agoda_newBookingRQ.Surcharge>();

                    //if (AgodaResponse.properties[0].rooms[0].surcharges.Count() > 0)
                    //{
                    //    for (int sur = 0; sur < AgodaResponse.properties[0].rooms[0].surcharges.Count; sur++)
                    //    {
                    //        Agoda_newBookingRQ.Surcharge objsurcharge = new Agoda_newBookingRQ.Surcharge();

                    //        Id = AgodaResponse.properties[0].rooms[0].surcharges[0].id;
                    //        surcharge_inclusive = AgodaResponse.properties[0].rooms[0].surcharges[0].rate.inclusive;
                    //        string Change = AgodaResponse.properties[0].rooms[0].surcharges[0].charge;
                    //        if (Change.ToLower() == "mandatory")
                    //        {
                    //            mandatory = true;
                    //        }
                    //        ObjRate.inclusive = surcharge_inclusive;
                    //        objsurcharge.id = Id;
                    //        objsurcharge.rate = ObjRate;
                    //        ObjSurcharge.Add(objsurcharge);
                    //    }
                    //}

                    //Agoda_newBookingRQ.Rate ObjRate = new Agoda_newBookingRQ.Rate();
                    //ObjRate.inclusive = surcharge_inclusive;


                    if (RoomFilter[0].surcharges.Count() > 0)
                    {
                        for (int sur = 0; sur < RoomFilter[0].surcharges.Count; sur++)
                        {
                            Agoda_newBookingRQ.Surcharge objsurcharge = new Agoda_newBookingRQ.Surcharge();
                            Agoda_newBookingRQ.Rate ObjRate = new Agoda_newBookingRQ.Rate();

                            Id = RoomFilter[0].surcharges[sur].id;
                            surcharge_inclusive = RoomFilter[0].surcharges[sur].rate.inclusive;
                            string Change = RoomFilter[0].surcharges[sur].charge;
                            if (Change.ToLower() == "mandatory")
                            {
                                mandatory = true;
                            }
                            ObjRate.inclusive = surcharge_inclusive;
                            objsurcharge.id = Id;
                            objsurcharge.rate = ObjRate;
                            ObjSurcharge.Add(objsurcharge);
                        }
                    }
                }
                string BlockId = RoomTypeId;// AgodaResponse.properties[0].rooms[0].blockId;

                //double Price = RoomFilter[0].totalPayment.inclusive;
                //double Price = AgodaResponse.properties[0].rooms[0].totalPayment.inclusive;

                string[] spitid1 = roomtypeid.Split(new String[] { "$" }, StringSplitOptions.None);
                string AgodaNationality = spitid1[8];
                double TotalPrice = Convert.ToDouble(spitid1[13]);
                string searchId = Convert.ToString(spitid1[11]);
                string PaymentModel = Convert.ToString(spitid1[18]);
                double Inclusive = Convert.ToDouble(spitid1[22]);

                Agoda_newBookingRQ.CreditCardInfo ObjCreditCardInfo = new Agoda_newBookingRQ.CreditCardInfo();
                ObjCreditCardInfo.cardType = AgodaInfo.AgodaCardtype;

                if (false)//(Price > TotalPrice)
                {
                    isTrue = false;
                    //strAgodaRequest.Append("<Cvc>00</Cvc>");
                    ObjCreditCardInfo.cvc = "00";//Convert.ToInt32(XMLHotelBookingCommon.creditCardIdentifier_Agoda);
                }
                else
                {
                    isTrue = true;
                    //strAgodaRequest.Append("<Cvc>" + XMLHotelBookingCommon.creditCardIdentifier_Agoda + "</Cvc>");
                    ObjCreditCardInfo.cvc = Convert.ToString(AgodaInfo.creditCardIdentifier_Agoda);
                }
                ObjCreditCardInfo.number = Convert.ToString(AgodaInfo.creditCardNumber_Agoda);
                //ObjCreditCardInfo.expiryDate = Convert.ToInt32(ExpriMonth + "" + ExpriYear);
                //ObjCreditCardInfo.expiryDate = Convert.ToString(ExpriYear + ExpriMonth + "12");
                ObjCreditCardInfo.expiryDate = Convert.ToString(ExpriMonth + ExpriYear);
                //ObjCreditCardInfo.cvc = Convert.ToInt32(XMLHotelBookingCommon.creditCardIdentifier_Agoda);
                ObjCreditCardInfo.holderName = AgodaInfo.cardHolderName;
                ObjCreditCardInfo.countryOfIssue = AgodaInfo.CountryOfIssue;
                ObjCreditCardInfo.issuingBank = AgodaInfo.IssuingBank;

                Agoda_newBookingRQ.PaymentDetails ObjPaymentDetail = new Agoda_newBookingRQ.PaymentDetails();
                ObjPaymentDetail.creditCardInfo = ObjCreditCardInfo;

                Agoda_newBookingRQ.Phone objphone = new Agoda_newBookingRQ.Phone();
                objphone.countryCode = AgodaInfo.CountryCode;
                objphone.areaCode = AgodaInfo.AreaCode;
                objphone.number = AgodaInfo.PhoneNumber;

                Agoda_newBookingRQ.CustomerDetail objCustomerDetail = new Agoda_newBookingRQ.CustomerDetail();
                objCustomerDetail.language = AgodaLaguage;
                objCustomerDetail.email = AgodaInfo.email_Agoda;
                objCustomerDetail.title = "Mr.";
                objCustomerDetail.firstName = AgodaInfo.firstName_Agoda;
                objCustomerDetail.lastName = AgodaInfo.lastName_Agoda;
                objCustomerDetail.phone = objphone;
                objCustomerDetail.newsletter = false;

                List<Agoda_newBookingRQ.GuestDetail> ObjGuestDetail = new List<Agoda_newBookingRQ.GuestDetail>();

                //string isLead = string.Empty;
                //dtPassengers.DefaultView.RowFilter = "PickUp='" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomTypeID"]) + "' and RoomNo='" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomNumber"]) + "' and IsLead=1";
                //if (dtPassengers.DefaultView.Count == 0)
                //    isLead = "true";
                //dtPassengers.DefaultView.RowFilter = "";
                //dtPassengers.DefaultView.RowFilter = "PickUp='" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomTypeID"]) + "' and RoomNo='" + (dsCartDetails.Tables[1].DefaultView[j]["RoomNumber"]) + "' and UserCartId='" + dsCartDetails.Tables[0].Rows[i]["UserCartId"] + "'";
                //dtPassengers.DefaultView.Sort = "IsLead desc";

                //string isLead = string.Empty;
                //dtPassengers.DefaultView.RowFilter = "PickUp='" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomTypeID"]) + "' and RoomNo='" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomNumber"]) + "' and IsLead=1";
                //if (dtPassengers.DefaultView.Count == 0)
                //    isLead = "true";
                //dtPassengers.DefaultView.RowFilter = "";
                //dtPassengers.DefaultView.RowFilter = "PickUp='" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomTypeID"]) + "' and RoomNo='" + (dsCartDetails.Tables[1].DefaultView[j]["RoomNumber"]) + "' and UserCartId='" + dsCartDetails.Tables[0].Rows[i]["UserCartId"] + "'";
                //dtPassengers.DefaultView.Sort = "IsLead desc";

                string isLead = string.Empty;
                dtPassengers.DefaultView.RowFilter = "PickUp='" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomTypeID"]) + "' and IsLead=1";
                if (dtPassengers.DefaultView.Count == 0)
                    isLead = "true";
                dtPassengers.DefaultView.RowFilter = "";
                dtPassengers.DefaultView.RowFilter = "PickUp='" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomTypeID"]) + "' and UserCartId='" + dsCartDetails.Tables[0].Rows[i]["UserCartId"] + "'";
                dtPassengers.DefaultView.Sort = "IsLead desc";

                if (dtPassengers.DefaultView.Count > 0)
                {
                    for (int k = 0; k < dtPassengers.DefaultView.Count; k++)
                    {
                        DataRow[] drPassengers = dtPassengers.DefaultView.ToTable().Select("IsLead='1'");
                        if (drPassengers.Length == 0)
                        {
                            if (Convert.ToString(dtPassengers.DefaultView[k]["PaxType"]).Trim().ToLower() == "adult")
                                dtPassengers.DefaultView[k]["IsLead"] = 1;
                        }
                        if (Convert.ToInt16(dtPassengers.DefaultView[k]["IsLead"]) == 1)
                        {
                            Agoda_newBookingRQ.GuestDetail objguestdetal = new Agoda_newBookingRQ.GuestDetail();
                            objguestdetal.firstName = Convert.ToString(dtPassengers.DefaultView[k]["FirstName"]).Trim();
                            objguestdetal.lastName = Convert.ToString(dtPassengers.DefaultView[k]["LastName"]).Trim();
                            objguestdetal.countryOfResidence = Nationality;
                            objguestdetal.title = Convert.ToString(dtPassengers.DefaultView[k]["Prefix"]);/*.Trim().Replace("", ".")*/
                            objguestdetal.gender = Convert.ToString(dtPassengers.DefaultView[k]["Gender"]).Trim();
                            objguestdetal.age = 30;
                            objguestdetal.primary = true;
                            ObjGuestDetail.Add(objguestdetal);
                        }
                        else
                        {
                            if (Convert.ToString(dtPassengers.DefaultView[k]["PaxType"]).Trim().ToLower() == "adult")
                            {
                                Agoda_newBookingRQ.GuestDetail objguestdetal = new Agoda_newBookingRQ.GuestDetail();
                                objguestdetal.firstName = Convert.ToString(dtPassengers.DefaultView[k]["FirstName"]).Trim();
                                objguestdetal.lastName = Convert.ToString(dtPassengers.DefaultView[k]["LastName"]).Trim();
                                objguestdetal.countryOfResidence = Nationality;
                                objguestdetal.title = Convert.ToString(dtPassengers.DefaultView[k]["Prefix"]);/*.Trim().Replace("", ".")*/
                                objguestdetal.gender = Convert.ToString(dtPassengers.DefaultView[k]["Gender"]).Trim();
                                objguestdetal.age = 30;
                                objguestdetal.primary = false;
                                ObjGuestDetail.Add(objguestdetal);
                            }
                            else
                            {
                                int A_ChildAges = Convert.ToInt32(dtPassengers.DefaultView[k]["ChildAge"]);
                                Agoda_newBookingRQ.GuestDetail objguestdetal = new Agoda_newBookingRQ.GuestDetail();
                                objguestdetal.firstName = Convert.ToString(dtPassengers.DefaultView[k]["FirstName"]).Trim();
                                objguestdetal.lastName = Convert.ToString(dtPassengers.DefaultView[k]["LastName"]).Trim();
                                objguestdetal.countryOfResidence = Nationality;
                                objguestdetal.title = Convert.ToString(dtPassengers.DefaultView[k]["Prefix"]);/*.Trim().Replace("", ".")*/
                                objguestdetal.gender = Convert.ToString(dtPassengers.DefaultView[k]["Gender"]).Trim();
                                objguestdetal.age = A_ChildAges;
                                objguestdetal.primary = false;
                                ObjGuestDetail.Add(objguestdetal);
                            }
                        }
                    }
                }
                Agoda_newBookingRQ.Rate objrate = new Agoda_newBookingRQ.Rate();
                objrate.inclusive = Inclusive;

                List<Agoda_newBookingRQ.Room> objRoom = new List<Agoda_newBookingRQ.Room>();
                Agoda_newBookingRQ.Room objroom = new Agoda_newBookingRQ.Room();
                // if (mandatory)
                {
                    objroom.surcharges = ObjSurcharge;
                }

                objroom.blockId = BlockId;
                objroom.currency = AgodaCurrency;
                objroom.paymentModel = PaymentModel;
                objroom.count = Rooms;
                objroom.adults = Adults;
                objroom.children = Childs;
                if (Childs > 0)
                {
                    objroom.childrenAges = t;
                }
                //objroom.surcharges = ObjSurcharge;
                objroom.guestDetails = ObjGuestDetail;
                objroom.rate = objrate;
                objRoom.Add(objroom);

                Agoda_newBookingRQ.Property objPropert = new Agoda_newBookingRQ.Property();
                objPropert.propertyId = Convert.ToInt32(XmlHotelId);
                objPropert.rooms = objRoom;

                Agoda_newBookingRQ.BookingDetails objBookingDetail = new Agoda_newBookingRQ.BookingDetails();
                objBookingDetail.userCountry = AgodaNationality;
                objBookingDetail.searchId = (Convert.ToInt64(searchId));
                objBookingDetail.tag = RefNo;
                objBookingDetail.allowDuplication = true;
                objBookingDetail.checkIn = checkindate;
                objBookingDetail.checkOut = checkoutdate;
                objBookingDetail.language = AgodaLaguage;
                objBookingDetail.property = objPropert;

                Agoda_newBookingRQ.Root main = new Agoda_newBookingRQ.Root();
                main.waitTime = 120;
                main.bookingDetails = objBookingDetail;
                main.customerDetail = objCustomerDetail;
                main.paymentDetails = ObjPaymentDetail;

                JsonSerializerSettings settings = new JsonSerializerSettings();
                settings.NullValueHandling = NullValueHandling.Ignore;
                //settings.DefaultValueHandling = DefaultValueHandling.Ignore;
                RequestString = JsonConvert.SerializeObject(main, settings);
                return RequestString;
            }
            catch (Exception ex)
            {
                _ = _exception.ErrorBooking(ex, "");
                isTrue = false;
                return "";
            }
        }

        public string GetAgoda_newBookingCancelRQ(string AgentVoucherNo)
        {
            string RequestString = "";
            try
            {
                Agoda_newCancelRQ.Root main = new Agoda_newCancelRQ.Root();
                main.bookingId = Convert.ToInt32(AgentVoucherNo);

                RequestString = JsonConvert.SerializeObject(main);
                return RequestString;
            }
            catch (Exception e)
            {
                _ =_exception.ErrorNew(e, _LogAPI.CancelBooking);
            }
            return RequestString;
        }

        public string ConfirmCancellation(string AgentVoucherNo, int Reference, string Currency, double inclusive)
        {
            string RequestString = "";
            try
            {
                Agoda_newConfirmCancelRQ.RefundRate ObjRefundRate = new Agoda_newConfirmCancelRQ.RefundRate();
                ObjRefundRate.currency = Currency;
                ObjRefundRate.inclusive = inclusive;

                Agoda_newConfirmCancelRQ.Root Main = new Agoda_newConfirmCancelRQ.Root();
                Main.bookingId = Convert.ToInt32(AgentVoucherNo);
                Main.reference = Convert.ToInt32(Reference);
                Main.cancelReason = 0;
                Main.refundRate = ObjRefundRate;

                RequestString = JsonConvert.SerializeObject(Main);
                return RequestString;
            }
            catch (Exception e)
            {
                _ = _exception.ErrorNew(e, _LogAPI.CancelBooking);
            }
            return RequestString;
        }
    }
}
