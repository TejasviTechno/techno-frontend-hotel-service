using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using XConnect.CommonUtilities;
using XConnect.CommonUtilities.Interface;
using XConnect.ErrorLogHandler.Interface;
using XConnect.Markup.Interface;
using XConnect.Models;
using XConnect.Models.Hotel;
using XConnect.Supplier.Fitruums;
using XConnect.Supplier.Fitruums.Interface;
using ZstdSharp.Unsafe;
using static Azure.Core.HttpHeader;
using static XConnect.Models.ErrorLog;
using Options = XConnect.Models.Options;

namespace Xconnect.Expedia
{

    public class FitruumsRepository: IFitruums
    {
        private readonly ICommonMethod _commonMethod;
        private readonly IException _exception;
        private readonly IMarkup _markup;
        private readonly ISupplierRes _supplierRes;
        private readonly KeySettings _keySettings;


        public FitruumsRepository(ICommonMethod commonMethod, IMarkup markup, ISupplierRes supplierRes, IOptions<KeySettings> keySettings, IException exception)
        {
            _commonMethod = commonMethod;
            _markup = markup;
            _supplierRes = supplierRes;
            _keySettings = keySettings.Value;
            _exception = exception;
        }

        public  void GetfitruumsRequestOnly_Async_new(DataRow[] commonCode, List<StarRating> listRating, HotelSearchParameter SearchPara, List<SupplierDetails> fitruumsDetails, List<SupplierSearchConfig> fitruumsSearchConfig, DataTable dtPreferredDataHotel, ref ConcurrentBag<HotelX> AllTask, string citycode, string Nationality, int Hit, string supplierName,TokenData tokenData)
        {
            try
            {
                #region "fitruums"
                if (fitruumsSearchConfig != null)
                {
                    DataRow[] drfitruums = dtPreferredDataHotel.Select("ServiceName='fitruums'");
                    string Ids = string.Empty;
                    if (drfitruums.Count() > 0)
                    {
                        var paramValues1 = String.Join(",", drfitruums.Select(x => String.Join(",", Convert.ToString(x.ItemArray[0]))));
                        Ids = Convert.ToString(paramValues1);
                    }
                    if (SearchPara.IsSearchFor.ToLower().IndexOf("preferred") >= 0 && drfitruums.Count() == 0)
                    {
                        fitruumsSearchConfig = null;
                    }
                    double FitruumsCurrencyFactor = 1;
                    String Sitecurrency = tokenData.SiteCurrency;

                    string FitruumsCurrency = Convert.ToString(fitruumsDetails[0].CurrencyShortName);

                    if (FitruumsCurrency.ToLower() != Sitecurrency.ToLower())
                    {
                        FitruumsCurrencyFactor = _commonMethod.GetROEByCurrency(FitruumsCurrency, tokenData.dtROE); 
                    }


                    if (fitruumsSearchConfig != null)
                    {
                        
                        for (int k = 0; k < fitruumsSearchConfig.Count; k++)
                        {
                            bool DebugMode = (Convert.ToInt32(fitruumsSearchConfig[k].DebugMode) == 0 ? false : true);
                            if (Convert.ToInt32(fitruumsSearchConfig[0].CityID) == 0 || Convert.ToInt32(fitruumsSearchConfig[0].CityID) == SearchPara.CityID)//All City means 0
                            {
                                // List<HotelCodeByCity> DtHotelDAta = FilterFinalList(commonCode, listRating);
                                if (commonCode.Length > 0)
                                {
                                    if (Convert.ToInt32(fitruumsSearchConfig[k].CityID) == 0 || Convert.ToInt32(fitruumsSearchConfig[k].CityID) == SearchPara.CityID)//All City means 0
                                    {
                                        {

                                            if (SearchPara.Rooms[0].Child1Age != 1 || SearchPara.Rooms[0].Child2Age != 1)
                                            {
                                                bool IsSame = true;
                                                int childAgeCount = 0; int AdultCount = 0, ChildCount = 0;
                                                for (int i = 0; i < SearchPara.Rooms.Count; i++)
                                                {
                                                    //    int NoOfAdt = SearchPara.Rooms[0].NoofAdults;
                                                    //    int NoOfChd = SearchPara.Rooms[0].NoofChild;
                                                    //    int ChildAge1 = SearchPara.Rooms[0].Child1Age;
                                                    //    int ChildAge2 = SearchPara.Rooms[0].Child2Age;
                                                    //    if (NoOfAdt != SearchPara.Rooms[i].NoofAdults || NoOfChd != SearchPara.Rooms[i].NoofChild || ChildAge1 != SearchPara.Rooms[i].Child1Age || ChildAge2 != SearchPara.Rooms[i].Child2Age)
                                                    //        IsSame = false;

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

                                                if (IsSame)
                                                {

                                                    int Noofrecords = Convert.ToInt32(fitruumsSearchConfig[k].Noofrecords);
                                                    DataTable[] splittedtables = commonCode.AsEnumerable()
                                                         .Select((row, index) => new { row, index })
                                                         .GroupBy(x => x.index / Noofrecords)  // integer division, the fractional part is truncated
                                                         .Select(g => g.Select(x => x.row).CopyToDataTable())
                                                         .ToArray();
                                                    for (int a = 0; a < splittedtables.Length; a++)
                                                   // for (int a = 0; a < 1; a++)
                                                    {

                                                        var paramValues1 = String.Join(",", splittedtables[a].Select().Select(x => String.Join(",", Convert.ToString(x.ItemArray[0]))));
                                                        string IdsForSearch = Convert.ToString(paramValues1);
                                                        HotelX xhotel = new HotelX();
                                                        // xhotel.Supplier = "fitruums";
                                                        xhotel.Supplier = supplierName;
                                                        xhotel.URL = Convert.ToString(fitruumsDetails[0].WebSiteURL) + FitruumsInfo.FitruumsHotel_Action;
                                                        xhotel.DebugeMode = DebugMode;
                                                        xhotel.SearchTimeOut = Convert.ToInt32(fitruumsSearchConfig[k].HotelSearchTimeOut);
                                                        // if (Hit > 0)
                                                        xhotel.ProcessTimeout = Convert.ToInt32(fitruumsSearchConfig[k].HotelSearchTimeOut2);
                                                        xhotel.CurrencyCode = FitruumsCurrency;
                                                        xhotel.CurrencyFactor = FitruumsCurrencyFactor;
                                                        xhotel.XmlNationality = Nationality;
                                                        xhotel.XmlCity = citycode;
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
                                                            objrq.Request = XMLfitruumsRequestOnly_Async("1", "1", Convert.ToString(fitruumsDetails[0].WebSiteURL), Convert.ToString(fitruumsDetails[0].UserID), Convert.ToString(fitruumsDetails[0].UserName), Convert.ToString(fitruumsDetails[0].Password), Convert.ToInt32(fitruumsSearchConfig[k].HotelSearchTimeOut), SearchPara, IdsForSearch, DebugMode, Nationality, citycode, FitruumsCurrency);
                                                            objrq.Response = ""; RQRS.Add(objrq);
                                                        }
                                                        xhotel.RQRS = RQRS;
                                                        xhotel.ReturnRooms = 5;// fitruumsDetails[0].ReturnRooms; //140225
                                                        xhotel.TechName = "Fitruums";// fitruumsDetails[0].TechName; ;//140225
                                                        #endregion
                                                        //xhotels.Request = Hotel.XMLfitruumsRequestOnly_Async("1", "1", Convert.ToString(fitruumsDetails[0].WebSiteURL), Convert.ToString(fitruumsDetails[0].UserID), Convert.ToString(fitruumsDetails[0].UserName), Convert.ToString(fitruumsDetails[0].Password), Convert.ToInt32(fitruumsSearchConfig[k].HotelSearchTimeOut), SearchPara, IdsForSearch, DebugMode, Nationality, citycode);
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
                #endregion
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
            }
        }

        //130325
        public void GetfitruumsRequestOnly_Async_new_v1(DataRow[] commonCode, List<StarRating> listRating, HotelSearchParameter SearchPara, List<SupplierDetails> lstfitruumsDetails, List<SupplierSearchConfig> lstfitruumsSearchConfig, DataTable dtPreferredDataHotel, ref ConcurrentBag<HotelX> AllTask, int Hit, TokenData tokenData, DataSet dtXMLCity, bool HitFromAll = true)
        {
            try
            {
                List<SupplierDetails> FitruumsMain = new List<SupplierDetails>();
                List<SupplierDetails> FitruumsChild = new List<SupplierDetails>();
                List<StarRating> lstRating = new List<StarRating>();
                lstRating = _commonMethod.GetStarRatingStringList_xtg(SearchPara);

                List<HotelCodeByCity> DtHotelDAta = HtmlExtensions.ToList<HotelCodeByCity>(commonCode.CopyToDataTable());
                int ratingcount = lstRating.Count;
                int hotelcount = DtHotelDAta.Count;

                foreach (SupplierDetails item in lstfitruumsDetails)
                {
                    if (false)
                        FitruumsMain.Add(item);
                    else
                        FitruumsChild.Add(item);
                }


                for (int x = 0; x < FitruumsChild.Count; x++)
                {
                    try
                    {
                        int SupplierID = FitruumsChild[x].SupplierID;
                        string Supplier = FitruumsChild[x].SupplierName;

                        if (lstfitruumsSearchConfig.Count > 0)
                        {
                            List<SupplierSearchConfig> fitruumsSearchConfig = lstfitruumsSearchConfig
                                    .Where(x => x != null && x.SupplierID == SupplierID)
                                    .ToList();

                            if (fitruumsSearchConfig != null && fitruumsSearchConfig.Count > 0)
                            {
                                if ((SearchPara.IsSearchFor.ToLower().IndexOf("fitruums") >= 0 || SearchPara.IsSearchFor.ToLower().IndexOf("multi") >= 0) && HitFromAll)
                                {
                                    DataForSearch dataForSearch = _commonMethod.GetXmlDataForSearchTechwise(SearchPara.Nationality, dtXMLCity, Supplier, "fitruums");

                                    if (dataForSearch.XmlNationality != "" && SearchPara.PastDate == false)// #Added on 130424
                                    {
                                        DataRow[] drfitruums = dtPreferredDataHotel.Select("ServiceName='fitruums'");
                                        string Ids = string.Empty;
                                        if (drfitruums.Count() > 0)
                                        {
                                            var paramValues1 = String.Join(",", drfitruums.Select(x => String.Join(",", Convert.ToString(x.ItemArray[0]))));
                                            Ids = Convert.ToString(paramValues1);
                                        }
                                        if (SearchPara.IsSearchFor.ToLower().IndexOf("preferred") >= 0 && drfitruums.Count() == 0)
                                        {
                                            fitruumsSearchConfig = null;
                                        }

                                        List<HotelCodeByCity> filter = new List<HotelCodeByCity>();
                                        filter = _commonMethod.FilterWithOutCity_Code(ratingcount, hotelcount, DtHotelDAta, lstRating, Supplier);


                                        double FitruumsCurrencyFactor = 1;
                                        String Sitecurrency = tokenData.SiteCurrency;

                                        string FitruumsCurrency = Convert.ToString(FitruumsChild[x].CurrencyShortName);

                                        if (FitruumsCurrency.ToLower() != Sitecurrency.ToLower())
                                        {
                                            FitruumsCurrencyFactor = _commonMethod.GetROEByCurrency(FitruumsCurrency, tokenData.dtROE);
                                        }

                                        for (int k = 0; k < fitruumsSearchConfig.Count; k++)
                                        {
                                            bool DebugMode = (Convert.ToInt32(fitruumsSearchConfig[k].DebugMode) == 0 ? false : true);
                                            if (Convert.ToInt32(fitruumsSearchConfig[0].CityID) == 0 || Convert.ToInt32(fitruumsSearchConfig[0].CityID) == SearchPara.CityID)//All City means 0
                                            {
                                                // List<HotelCodeByCity> DtHotelDAta = FilterFinalList(commonCode, listRating);
                                                if (filter.Count > 0)
                                                {
                                                    if (Convert.ToInt32(fitruumsSearchConfig[k].CityID) == 0 || Convert.ToInt32(fitruumsSearchConfig[k].CityID) == SearchPara.CityID)//All City means 0
                                                    {
                                                        {

                                                            if (SearchPara.Rooms[0].Child1Age != 1 || SearchPara.Rooms[0].Child2Age != 1)
                                                            {
                                                                bool IsSame = true;
                                                                int childAgeCount = 0; int AdultCount = 0, ChildCount = 0;
                                                                for (int i = 0; i < SearchPara.Rooms.Count; i++)
                                                                {
                                                                    //    int NoOfAdt = SearchPara.Rooms[0].NoofAdults;
                                                                    //    int NoOfChd = SearchPara.Rooms[0].NoofChild;
                                                                    //    int ChildAge1 = SearchPara.Rooms[0].Child1Age;
                                                                    //    int ChildAge2 = SearchPara.Rooms[0].Child2Age;
                                                                    //    if (NoOfAdt != SearchPara.Rooms[i].NoofAdults || NoOfChd != SearchPara.Rooms[i].NoofChild || ChildAge1 != SearchPara.Rooms[i].Child1Age || ChildAge2 != SearchPara.Rooms[i].Child2Age)
                                                                    //        IsSame = false;

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

                                                                if (IsSame)
                                                                {

                                                                    int NoOfRecord = Convert.ToInt32(fitruumsSearchConfig[k].Noofrecords);
                                                                    double count = Convert.ToDouble(filter.Count);
                                                                    int NoOfDiv = Convert.ToInt32(Math.Ceiling(count / Convert.ToDouble(NoOfRecord)));
                                                                    List<List<XMLID_Search>> lstxml = _commonMethod.GetXMLIDString(NoOfDiv, NoOfRecord, count, filter);
                                                                    for (int a = 0; a < NoOfDiv; a++)
                                                                    // for (int a = 0; a < 1; a++)
                                                                    {

                                                                        //var paramValues1 = String.Join(",", splittedtables[a].Select().Select(x => String.Join(",", Convert.ToString(x.ItemArray[0]))));
                                                                        //string IdsForSearch = Convert.ToString(paramValues1);
                                                                        string IdsForSearch = Convert.ToString(_commonMethod.GetHotelCodeByCOmma(lstxml[a]));

                                                                        HotelX xhotel = new HotelX();
                                                                        // xhotel.Supplier = "fitruums";
                                                                        xhotel.Supplier = Supplier;
                                                                        //xhotel.URL = Convert.ToString(FitruumsChild[x].WebSiteURL) + FitruumsInfo.FitruumsHotel_Action;
                                                                        if (_keySettings.SupplierProxy == "0")
                                                                            xhotel.URL = Convert.ToString(FitruumsChild[x].WebSiteURL) + FitruumsInfo.FitruumsHotel_Action;
                                                                        else
                                                                            xhotel.URL = Convert.ToString(FitruumsChild[x].WebSiteURLforAPI) + FitruumsInfo.FitruumsHotel_Action;

                                                                        xhotel.DebugeMode = DebugMode;
                                                                        xhotel.SearchTimeOut = Convert.ToInt32(fitruumsSearchConfig[k].HotelSearchTimeOut);
                                                                        // if (Hit > 0)
                                                                        xhotel.ProcessTimeout = Convert.ToInt32(fitruumsSearchConfig[k].HotelSearchTimeOut2);
                                                                        xhotel.CurrencyCode = FitruumsCurrency;
                                                                        xhotel.CurrencyFactor = FitruumsCurrencyFactor;
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
                                                                            objrq.Request = XMLfitruumsRequestOnly_Async("1", "1", Convert.ToString(FitruumsChild[x].WebSiteURL), Convert.ToString(FitruumsChild[x].UserID), Convert.ToString(FitruumsChild[x].UserName), Convert.ToString(FitruumsChild[x].Password), Convert.ToInt32(fitruumsSearchConfig[k].HotelSearchTimeOut), SearchPara, IdsForSearch, DebugMode, dataForSearch.XmlNationality, dataForSearch.xmlCity, FitruumsCurrency);
                                                                            objrq.Response = ""; RQRS.Add(objrq);
                                                                        }
                                                                        xhotel.RQRS = RQRS;
                                                                        xhotel.ReturnRooms = FitruumsChild[0].ReturnRooms; //140225
                                                                        xhotel.TechName =  FitruumsChild[0].TechName; ;//140225
                                                                        #endregion
                                                                        //xhotels.Request = Hotel.XMLfitruumsRequestOnly_Async("1", "1", Convert.ToString(fitruumsDetails[0].WebSiteURL), Convert.ToString(fitruumsDetails[0].UserID), Convert.ToString(fitruumsDetails[0].UserName), Convert.ToString(fitruumsDetails[0].Password), Convert.ToInt32(fitruumsSearchConfig[k].HotelSearchTimeOut), SearchPara, IdsForSearch, DebugMode, Nationality, citycode);
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
                    catch (Exception)
                    {

                        throw;
                    }
                }


            }
            catch (Exception ex)
            {
                _exception.Error(ex);
            }
        }
        public  string XMLfitruumsRequestOnly_Async(string MinStar, string MaxStar, string APIUrl, string UserId, string UserName, string Password, int SearchTimeOut, HotelSearchParameter SearchParam, string hotelIds, bool DebugMode, string Nationality, string citycode, String FitruumsCurrency)
        {

            try
            {
                int isHit = 0;
                return GetfitruumsHotelAndRoomListRequest(SearchParam, citycode, APIUrl, UserId, UserName, Password, hotelIds, isHit, Nationality, 0, 1, FitruumsCurrency);
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
                return "";
            }
            finally
            {
                //GC.Collect();
            }
        }
        public  string GetfitruumsHotelAndRoomListRequest(HotelSearchParameter searchParam, string citycode, string Url, string UserId, string UserName, string Password, string hotelCode, int ishit, string TLNationality, int j, int reqtype,String FitruumsCurrency)
        {
            try
            {

                string checkIn = String.Format("{0:yyyy-MM-dd}", DateTime.ParseExact(searchParam.CheckInDate, "dd/MM/yyyy", null));
                string checkOut = String.Format("{0:yyyy-MM-dd}", DateTime.ParseExact(searchParam.CheckOutDate, "dd/MM/yyyy", null));

                #region Adults Child Count
                int StarStart = searchParam.StarStart == 0 ? 1 : searchParam.StarStart;
                int StarEnd = searchParam.StarEnd == 0 ? 1 : searchParam.StarEnd;
                int NoOfRooms = searchParam.Rooms.Count();
                int NoofAdults = 0;
                int NoOfChild = 0;
                int NoOfInfant = 0;
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
                            switch (searchParam.Rooms[ser].Child1Age < 2)
                            {
                                case true:
                                    NoOfInfant += 1;
                                    break;
                                case false:
                                    NoOfChild += 1;
                                    if (ChildAge == "")
                                    {
                                        ChildAge += searchParam.Rooms[ser].Child1Age;
                                    }
                                    else
                                    {
                                        ChildAge += "," + searchParam.Rooms[ser].Child1Age;
                                    }

                                    break;
                                default:
                                    break;
                            }
                            break;
                        case 2:
                            switch (searchParam.Rooms[ser].Child1Age < 2)
                            {
                                case true:
                                    NoOfInfant += 1;
                                    break;
                                case false:
                                    NoOfChild += 1;
                                    if (ChildAge == "")
                                    {
                                        ChildAge += searchParam.Rooms[ser].Child1Age;
                                    }
                                    else
                                    {
                                        ChildAge += "," + searchParam.Rooms[ser].Child1Age;
                                    }
                                    break;
                                default:
                                    break;
                            }
                            switch (searchParam.Rooms[ser].Child2Age < 2)
                            {
                                case true:
                                    NoOfInfant += 1;
                                    break;
                                case false:
                                    NoOfChild += 1;
                                    if (ChildAge == "")
                                    {
                                        ChildAge += searchParam.Rooms[ser].Child2Age;
                                    }
                                    else
                                    {
                                        ChildAge += "," + searchParam.Rooms[ser].Child2Age;
                                    }
                                    break;
                                default:
                                    break;
                            }
                            break;
                        default:
                            break;
                    }
                }


                #endregion
                StringBuilder fitruumshotellist = new StringBuilder();
                if (hotelCode != string.Empty)
                {
                    string HIds = "";
                    string[] Hotelid = hotelCode.Split(',');
                    for (int i = 0; i < Hotelid.Length; i++)
                    {
                        if (i == 0)
                            HIds = Hotelid[i];
                        else
                            HIds = HIds + "," + Hotelid[i];
                    }
                    //switch (reqtype)
                    //{
                    //    case 1: fitruumshotellist.Append(Url + XMLHotelBookingCommon.FitruumsRoomList); break;
                    //    case 2: fitruumshotellist.Append(Url + XMLHotelBookingCommon.FitruumsRoomList); break;
                    //}
                   
                    switch (reqtype)
                    {
                        case 1: fitruumshotellist.Append(Url + FitruumsInfo.FitruumsHotelList); break;//Added on 21-JUN-2024 as per mustafa sir
                        case 2: fitruumshotellist.Append(Url + FitruumsInfo.FitruumsHotelList); break;//Added on 21-JUN-2024
                    }
                    fitruumshotellist.Append("userName=" + UserName + "");
                    fitruumshotellist.Append("&password=" + Password + "");
                    fitruumshotellist.Append("&language=en");
                    fitruumshotellist.Append("&currencies=" + FitruumsCurrency + "");
                    fitruumshotellist.Append("&checkInDate=" + checkIn + "");
                    fitruumshotellist.Append("&checkOutDate=" + checkOut + "");
                    fitruumshotellist.Append("&numberOfRooms=" + NoOfRooms + "");
                    fitruumshotellist.Append("&destination=");
                    fitruumshotellist.Append("&destinationID=");
                    fitruumshotellist.Append("&hotelIDs=" + HIds + "");
                    fitruumshotellist.Append("&resortIDs=");
                    fitruumshotellist.Append("&accommodationTypes=");
                    fitruumshotellist.Append("&numberOfAdults=" + NoofAdults + "");
                    fitruumshotellist.Append("&numberOfChildren=" + NoOfChild + "");
                    fitruumshotellist.Append("&childrenAges=" + ChildAge + "");
                    fitruumshotellist.Append("&infant=" + NoOfInfant + "");
                    fitruumshotellist.Append("&sortBy=");
                    fitruumshotellist.Append("&sortOrder=");
                    fitruumshotellist.Append("&exactDestinationMatch=");
                    fitruumshotellist.Append("&blockSuperdeal=");
                    fitruumshotellist.Append("&showTransfer=");
                    fitruumshotellist.Append("&mealIds=");
                    fitruumshotellist.Append("&showCoordinates=");
                    fitruumshotellist.Append("&showReviews=");
                    fitruumshotellist.Append("&showRoomTypeName=1");//Added on 21-JUN-2024
                    fitruumshotellist.Append("&referencePointLatitude=");
                    fitruumshotellist.Append("&referencePointLongitude=");
                    fitruumshotellist.Append("&maxDistanceFromReferencePoint=");
                    fitruumshotellist.Append("&minStarRating=" + StarStart + "");
                    fitruumshotellist.Append("&maxStarRating=" + StarEnd + "");
                    fitruumshotellist.Append("&featureIds=");
                    fitruumshotellist.Append("&minPrice=");
                    fitruumshotellist.Append("&maxPrice=");
                    fitruumshotellist.Append("&themeIds=");
                    fitruumshotellist.Append("&excludeSharedRooms=");
                    fitruumshotellist.Append("&excludeSharedFacilities=");
                    fitruumshotellist.Append("&prioritizedHotelIds=");
                    fitruumshotellist.Append("&totalRoomsInBatch=");
                    fitruumshotellist.Append("&paymentMethodId=");
                    fitruumshotellist.Append("&CustomerCountry=" + TLNationality + "");
                    fitruumshotellist.Append("&b2c=0");

                }
                string fitruumsReq = fitruumshotellist.ToString();
                return fitruumsReq;

            }
            catch (Exception ex)
            {
                _exception.Error(ex);
                return "";
            }
        }


        public async Task< List<Options>> Fitruums_HotelSearchWithRooms(HotelX Item, CommonSearch commonSearch, string NationalityCode, HotelSearchParameter SearchParam, string TLResponse, string XMLCITY, string XMLCOUNTRY, string FitruumsCurrency, double FitruumsCurrencyFactor, DataTable dtROE,string ProccessCutOffTime, Int32 processTimeout, string ResProccessTimerLogActive, string ResProccessTimeValueforSaveLog, string StoreSupplierResErrorLog)
        {
            System.Diagnostics.Stopwatch timer_room_Fitruums = new System.Diagnostics.Stopwatch();
            timer_room_Fitruums.Start();
            List<Options> options = new List<Options>();

            DataTable DtHotel = _commonMethod.GenerateDataTableNewV2();
            try
            {
                if (TLResponse == null || TLResponse == string.Empty || TLResponse.ToLower().Contains("error"))
                {
                    Item.ResponseStatus = 404;
                    Item.ResponseType = "NoCount";
                    return options;
                }
                TLResponse = TLResponse.Replace(" xsi:nil=\"true\" ", "");
                DataSet fitruumsResponse = _commonMethod.GetDataSetFromXMLString(TLResponse);
                if (fitruumsResponse.Tables.Contains("hotel") == true)
                {
                    for (int h = 0; h <= fitruumsResponse.Tables["hotel"].Rows.Count - 1; h++)
                    {
                        Item.ResponseType = "Success";
                        //Check Time If High then return NoCount
                        if (ProccessCutOffTime == "1" && timer_room_Fitruums.ElapsedMilliseconds > processTimeout)
                        {
                            //timer_room_Fitruums.Stop();
                            break;
                        }
                        //--end
                        // DtHotel.Rows.Add(drXmlHotel);

                        double LowestRate = 1;

                        #region "When not Need Room"  
                        // add hotel code from price only and room code blank here
                        if (SearchParam.Filters.IsShowRooms == 0)
                        {
                            #region "Hotel Code Block "
                            
                            DataTable HotelRooms = GenerateRooms();
                            //animesh changes code  21 feb 2019 for price 2 comments
                            if (commonSearch.tokenData.AgentId != 1899)
                            {
                                #region Calculation
                                LowestRate = 0;
                                if (fitruumsResponse.Tables.Contains("roomtypes") == true)
                                {
                                    if (fitruumsResponse.Tables["roomtypes"].Columns.Contains("hotel_id") == true)
                                    {
                                        DataRow[] RoomTypes = fitruumsResponse.Tables["roomtypes"].Select("hotel_id='" + Convert.ToInt32(fitruumsResponse.Tables["Hotel"].Rows[h]["hotel_id"]) + "'");
                                        if (RoomTypes.Length > 0)
                                        {
                                            for (int RTsid = 0; RTsid < RoomTypes.GetLength(0); RTsid++)
                                            {
                                                int roomtypes_Id = 0;
                                                if (fitruumsResponse.Tables["roomtypes"].Columns.Contains("roomtypes_Id") == true)
                                                {
                                                    roomtypes_Id = Convert.ToInt32(RoomTypes[RTsid]["roomtypes_Id"]);
                                                }
                                                if (fitruumsResponse.Tables.Contains("roomtype") == true)
                                                {
                                                    if (fitruumsResponse.Tables["roomtype"].Columns.Contains("roomtypes_Id") == true)
                                                    {
                                                        DataRow[] RoomType = fitruumsResponse.Tables["roomtype"].Select("roomtypes_Id='" + roomtypes_Id + "'");
                                                        if (RoomType.Length > 0)
                                                        {
                                                            for (int RTid = 0; RTid < RoomType.GetLength(0); RTid++)
                                                            {
                                                                int roomtype_Id = 0;// RoomTypeId = 0; string RoomeTypeName = "";
                                                                if (fitruumsResponse.Tables["roomtype"].Columns.Contains("roomtype_id") == true)
                                                                {
                                                                    roomtype_Id = Convert.ToInt32(RoomType[RTid]["roomtype_id"]);
                                                                }
                                                                //if (fitruumsResponse.Tables["roomtype"].Columns.Contains("roomtype.Id") == true)
                                                                //{
                                                                //    RoomTypeId = Convert.ToInt32(RoomType[RTid]["roomtype.Id"]);
                                                                //}
                                                                string RoomeTypeName = "";
                                                                if (fitruumsResponse.Tables["roomtype"].Columns.Contains("roomtype.Name") == true)
                                                                {
                                                                    RoomeTypeName = Convert.ToString(RoomType[RTid]["roomtype.Name"]);
                                                                }
                                                                if (fitruumsResponse.Tables.Contains("rooms") == true)
                                                                {
                                                                    if (fitruumsResponse.Tables["rooms"].Columns.Contains("roomtype_Id") == true)
                                                                    {
                                                                        DataRow[] rooms = fitruumsResponse.Tables["rooms"].Select("roomtype_Id='" + roomtype_Id + "'");
                                                                        if (rooms.Length > 0)
                                                                        {
                                                                            for (int Rsid = 0; Rsid < rooms.GetLength(0); Rsid++)
                                                                            {

                                                                                int rooms_Id = 0;
                                                                                if (fitruumsResponse.Tables["rooms"].Columns.Contains("rooms_Id") == true)
                                                                                {
                                                                                    rooms_Id = Convert.ToInt32(rooms[Rsid]["rooms_Id"]);
                                                                                }

                                                                                if (fitruumsResponse.Tables.Contains("room") == true)
                                                                                {
                                                                                    if (fitruumsResponse.Tables["room"].Columns.Contains("rooms_Id") == true)
                                                                                    {
                                                                                        DataRow[] room = fitruumsResponse.Tables["room"].Select("rooms_Id='" + rooms_Id + "'");
                                                                                        if (room.Length > 0)
                                                                                        {
                                                                                            for (int Rid = 0; Rid < room.GetLength(0); Rid++)
                                                                                            {
                                                                                                int room_Id = 0; int RoomIdnew = 0;
                                                                                                if (fitruumsResponse.Tables["room"].Columns.Contains("room_Id") == true)
                                                                                                {
                                                                                                    room_Id = Convert.ToInt32(room[Rid]["room_Id"]);
                                                                                                }
                                                                                                if (fitruumsResponse.Tables["room"].Columns.Contains("Id") == true)
                                                                                                {
                                                                                                    RoomIdnew = Convert.ToInt32(room[Rid]["Id"]);
                                                                                                }
                                                                                                if (fitruumsResponse.Tables.Contains("meals") == true)
                                                                                                {
                                                                                                    if (fitruumsResponse.Tables["meals"].Columns.Contains("room_Id") == true)
                                                                                                    {
                                                                                                        DataRow[] meals = fitruumsResponse.Tables["meals"].Select("room_Id='" + room_Id + "'");
                                                                                                        if (meals.Length > 0)
                                                                                                        {

                                                                                                            for (int Msid = 0; Msid < meals.GetLength(0); Msid++)
                                                                                                            {

                                                                                                                int meals_Id = 0;
                                                                                                                if (fitruumsResponse.Tables["meals"].Columns.Contains("meals_Id") == true)
                                                                                                                {
                                                                                                                    meals_Id = Convert.ToInt32(meals[Msid]["meals_Id"]);
                                                                                                                }
                                                                                                                if (fitruumsResponse.Tables.Contains("meal") == true)
                                                                                                                {
                                                                                                                    if (fitruumsResponse.Tables["meal"].Columns.Contains("meals_Id") == true)
                                                                                                                    {
                                                                                                                        DataRow[] meal = fitruumsResponse.Tables["meal"].Select("meals_Id='" + meals_Id + "'");
                                                                                                                        if (meal.Length > 0)
                                                                                                                        {
                                                                                                                            for (int Mid = 0; Mid < meal.GetLength(0); Mid++)
                                                                                                                            {
                                                                                                                                DataRow drHotelImage = HotelRooms.NewRow();
                                                                                                                                int meal_Id = 0;// string MealName = "", MealId = "";
                                                                                                                                if (fitruumsResponse.Tables["meal"].Columns.Contains("meal_Id") == true)
                                                                                                                                {
                                                                                                                                    meal_Id = Convert.ToInt32(meal[Mid]["meal_Id"]);
                                                                                                                                }
                                                                                                                                //if (fitruumsResponse.Tables["meal"].Columns.Contains("Id") == true)
                                                                                                                                //{
                                                                                                                                //    MealId = Convert.ToString(meal[Mid]["Id"]);
                                                                                                                                //}
                                                                                                                                //if (fitruumsResponse.Tables["meal"].Columns.Contains("name") == true)
                                                                                                                                //{
                                                                                                                                //    MealName = Convert.ToString(meal[Mid]["name"]);
                                                                                                                                //    if (MealName == "No meals")
                                                                                                                                //    {
                                                                                                                                //        MealName = "Room Only";
                                                                                                                                //    }
                                                                                                                                //}
                                                                                                                                if (fitruumsResponse.Tables.Contains("Prices") == true)
                                                                                                                                {
                                                                                                                                    if (fitruumsResponse.Tables["Prices"].Columns.Contains("meal_Id") == true)
                                                                                                                                    {
                                                                                                                                        DataRow[] Prices = fitruumsResponse.Tables["Prices"].Select("meal_Id='" + meal_Id + "'");
                                                                                                                                        if (Prices.Length > 0)
                                                                                                                                        {
                                                                                                                                            for (int Psid = 0; Psid < Prices.GetLength(0); Psid++)
                                                                                                                                            {
                                                                                                                                                int prices_Id = 0;
                                                                                                                                                if (fitruumsResponse.Tables["Prices"].Columns.Contains("prices_Id") == true)
                                                                                                                                                {
                                                                                                                                                    prices_Id = Convert.ToInt32(Prices[Psid]["prices_Id"]);
                                                                                                                                                }
                                                                                                                                                if (fitruumsResponse.Tables.Contains("price") == true)
                                                                                                                                                {
                                                                                                                                                    if (fitruumsResponse.Tables["price"].Columns.Contains("prices_Id") == true)
                                                                                                                                                    {
                                                                                                                                                        DataRow[] price = fitruumsResponse.Tables["price"].Select("prices_Id='" + prices_Id + "'");
                                                                                                                                                        if (price.Length > 0)
                                                                                                                                                        {
                                                                                                                                                            for (int Pid = 0; Pid < price.GetLength(0); Pid++)
                                                                                                                                                            {
                                                                                                                                                                Double  price_Text = 0;
                                                                                                                                                                
                                                                                                                                                                if (fitruumsResponse.Tables["price"].Columns.Contains("price_Text") == true)
                                                                                                                                                                {
                                                                                                                                                                    price_Text = Convert.ToDouble(price[Pid]["price_Text"]);
                                                                                                                                                                                                                                                                                                                                      
                                                                                                                                                                }

                                                                                                                                                                price_Text = price_Text * FitruumsCurrencyFactor; //added
                                                                                                                                                                drHotelImage["Price"] = price_Text;
                                                                                                                                                                HotelRooms.Rows.Add(drHotelImage);
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
                                LowestRate = Convert.ToDouble(HotelRooms.Rows[0]["Price"]);
                                #endregion
                            }
                            // HotelRooms.DefaultView.Sort = "Price asc";
                            //  HotelRooms = HotelRooms.DefaultView.ToTable(true);

                            // if (Pricenew > 0)

                            DataRow drXmlHotel = DtHotel.NewRow();
                            drXmlHotel["IsRecomondedHotel"] = false;
                            drXmlHotel["ContractID"] = 0;
                            drXmlHotel["XMLHotelCode"] = Convert.ToInt32(fitruumsResponse.Tables["Hotel"].Rows[h]["hotel.id"]);
                            drXmlHotel["UniqueNo"] = Convert.ToInt32(fitruumsResponse.Tables["Hotel"].Rows[h]["hotel.id"]);
                            drXmlHotel["IsShowSupplier"] = false;
                            drXmlHotel["HotelName"] = "";// Convert.ToString(fitruumsResponse.Tables["Hotel"].Rows[h]["name"]);
                            drXmlHotel["Rating"] = 1;
                            drXmlHotel["XmlHotelCity"] = XMLCITY;
                            drXmlHotel["XmlCity"] = XMLCITY;
                            drXmlHotel["XmlCountry"] = XMLCOUNTRY;
                            drXmlHotel["SupplierCurrency"] = FitruumsCurrency;
                            //drXmlHotel["SupplierName"] = "fitruums";
                            drXmlHotel["SupplierName"] = Item.Supplier;
                            drXmlHotel["IsOffer"] = false;

                            drXmlHotel["SellingPrice"] = LowestRate;
                            drXmlHotel["FinalSellingPrice"] = LowestRate;
                            drXmlHotel["BookingStatus"] = "Available";
                            // if (LowestRate > 0)
                            DtHotel.Rows.Add(drXmlHotel);
                            #endregion


                            //  if (LowestRate > 0)
                            {
                                #region "Room code Block"
                                bool willAddOrNot = true;
                                Int64 RaynaID = 0;
                                Int64 GiataId = 0;
                                int Rating = 0;
                                Int64 OwnsystemHotelId = 0;
                                double MinPrice = LowestRate;
                                _markup.GetHotelDataCommon(ref MinPrice, ref willAddOrNot, ref OwnsystemHotelId, ref RaynaID, ref GiataId, ref Rating, ref drXmlHotel, commonSearch.AgentId, commonSearch.TariffID, commonSearch.dsAgentDetails, commonSearch.supplierGiataMaster, SearchParam, commonSearch.dtCountryCityMarkup, commonSearch.B2CMarkup, commonSearch.IsParentId, commonSearch.SubAgentMarkup, commonSearch.SearchByHotel, commonSearch.HotelIDS);
                                if (willAddOrNot)
                                {
                                    #region "Room Logic"
                                    DataTable dt_HotelRoom = _commonMethod.GenerateRoomtypeDataTableNewV2();
                                    DataTable dt_Cancellation = _commonMethod.GenerateCancellationDataTableNewV2();
                                    DataTable dt_RateBreackUp = _commonMethod.GenerateoomRateBreackUpDataTableNewV2();
                                    #endregion
                                    decimal LowestPrice = Convert.ToDecimal(MinPrice).CalculatePrice(commonSearch.tokenData, commonSearch.advancedOptions, FitruumsCurrency);
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
                            DataRow drXmlHotel = DtHotel.NewRow();
                            drXmlHotel["IsRecomondedHotel"] = false;
                            drXmlHotel["ContractID"] = 0;
                            drXmlHotel["XMLHotelCode"] = Convert.ToInt32(fitruumsResponse.Tables["Hotel"].Rows[h]["hotel.id"]);
                            drXmlHotel["UniqueNo"] = Convert.ToInt32(fitruumsResponse.Tables["Hotel"].Rows[h]["hotel.id"]);
                            drXmlHotel["IsShowSupplier"] = false;
                            drXmlHotel["HotelName"] = "";// Convert.ToString(fitruumsResponse.Tables["Hotel"].Rows[h]["name"]);
                            drXmlHotel["Rating"] = 1;
                            drXmlHotel["XmlHotelCity"] = XMLCITY;
                            drXmlHotel["XmlCity"] = XMLCITY;
                            drXmlHotel["XmlCountry"] = XMLCOUNTRY;
                            drXmlHotel["SupplierCurrency"] = FitruumsCurrency;
                            //drXmlHotel["SupplierName"] = "fitruums";
                            drXmlHotel["SupplierName"] = Item.Supplier;
                            drXmlHotel["IsOffer"] = false;
                            //// double Pricenew = Convert.ToDouble(HotelRooms.Rows[0]["Price"]);
                            //// Pricenew = Math.Round(Convert.ToDouble(Pricenew), 2);
                            drXmlHotel["SellingPrice"] = 1;
                            drXmlHotel["FinalSellingPrice"] = 1;
                            drXmlHotel["BookingStatus"] = "Available";
                            #endregion
                            #region "GetRooms for this hotel"
                            bool willAddOrNot = true;
                            Int64 RaynaID = 0;
                            Int64 GiataId = 0;
                            int RatingHotel = 0;
                            Int64 OwnsystemHotelId = 0;
                            //string Provider = "fitruums";
                            string Provider = Item.Supplier;
                            double MinPrice = 1;
                            _markup.GetHotelDataCommon(ref MinPrice, ref willAddOrNot, ref OwnsystemHotelId, ref RaynaID, ref GiataId, ref RatingHotel, ref drXmlHotel, commonSearch.AgentId, commonSearch.TariffID, commonSearch.dsAgentDetails, commonSearch.supplierGiataMaster, SearchParam, commonSearch.dtCountryCityMarkup, commonSearch.B2CMarkup, commonSearch.IsParentId, commonSearch.SubAgentMarkup, commonSearch.SearchByHotel, commonSearch.HotelIDS);
                            if (willAddOrNot)
                            {
                                DataTable dtHotelRoom = _commonMethod.GenerateRoomtypeDataTableNewV2();
                                DataTable dtCancellation = _commonMethod.GenerateCancellationDataTableNewV2();
                                DataTable DtRoomRateBreackUp = _commonMethod.GenerateoomRateBreackUpDataTableNewV2();
                                int CutOffDays = CacheMaster.CutOffDays;
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
                                    DataTable HotelRooms = GenerateRooms();
                                    //string HotelName = "";
                                    //if (fitruumsResponse.Tables["hotel"].Columns.Contains("hotel.id") == true)
                                    //{
                                    //    HotelId = Convert.ToInt32(fitruumsResponse.Tables["Hotel"].Rows[h]["hotel.id"]);
                                    //}
                                    //if (fitruumsResponse.Tables["hotel"].Columns.Contains("Name") == true)
                                    //{
                                    //    HotelName = Convert.ToString(fitruumsResponse.Tables["Hotel"].Rows[h]["Name"]);
                                    //}
                                    //if (fitruumsResponse.Tables["hotel"].Columns.Contains("hotel_id") == true)
                                    //{
                                    //    hotel_id = Convert.ToInt32();
                                    //}
                                    if (fitruumsResponse.Tables.Contains("roomtypes") == true)
                                    {
                                        if (fitruumsResponse.Tables["roomtypes"].Columns.Contains("hotel_id") == true)
                                        {
                                            DataRow[] RoomTypes = fitruumsResponse.Tables["roomtypes"].Select("hotel_id='" + fitruumsResponse.Tables["Hotel"].Rows[h]["hotel_id"] + "'");
                                            if (RoomTypes.Length > 0)
                                            {
                                                for (int RTsid = 0; RTsid < RoomTypes.GetLength(0); RTsid++)
                                                {
                                                    //int roomtypes_Id = 0;
                                                    //if (fitruumsResponse.Tables["roomtypes"].Columns.Contains("roomtypes_Id") == true)
                                                    //{
                                                    //    roomtypes_Id = Convert.ToInt32(RoomTypes[RTsid]["roomtypes_Id"]);
                                                    //}
                                                    if (fitruumsResponse.Tables.Contains("roomtype") == true)
                                                    {
                                                        // if (fitruumsResponse.Tables["roomtype"].Columns.Contains("roomtypes_Id") == true)
                                                        {
                                                            DataRow[] RoomType = fitruumsResponse.Tables["roomtype"].Select("roomtypes_Id='" + RoomTypes[RTsid]["roomtypes_Id"] + "'");
                                                            if (RoomType.Length > 0)
                                                            {
                                                                for (int RTid = 0; RTid < RoomType.GetLength(0); RTid++)
                                                                {
                                                                    string RoomeTypeName = "";
                                                                    if (fitruumsResponse.Tables["roomtype"].Columns.Contains("roomtype.Name") == true)
                                                                    {
                                                                        RoomeTypeName = Convert.ToString(RoomType[RTid]["roomtype.Name"]);
                                                                    }
                                                                    // int roomtype_Id = 0, RoomTypeId = 0; string RoomeTypeName = "";
                                                                    //if (fitruumsResponse.Tables["roomtype"].Columns.Contains("roomtype_id") == true)
                                                                    //{
                                                                    //    roomtype_Id = Convert.ToInt32(RoomType[RTid]["roomtype_id"]);
                                                                    //}
                                                                    //if (fitruumsResponse.Tables["roomtype"].Columns.Contains("roomtype.Id") == true)
                                                                    //{
                                                                    //    RoomTypeId = Convert.ToInt32(RoomType[RTid]["roomtype.Id"]);
                                                                    //}
                                                                    //if (fitruumsResponse.Tables["roomtype"].Columns.Contains("room.type") == true)
                                                                    //{
                                                                    //    RoomeTypeName = Convert.ToString(RoomType[RTid]["room.type"]);
                                                                    //}
                                                                    if (fitruumsResponse.Tables.Contains("rooms") == true)
                                                                    {
                                                                        // if (fitruumsResponse.Tables["rooms"].Columns.Contains("roomtype_Id") == true)
                                                                        {
                                                                            DataRow[] rooms = fitruumsResponse.Tables["rooms"].Select("roomtype_Id='" + RoomType[RTid]["roomtype_id"] + "'");
                                                                            if (rooms.Length > 0)
                                                                            {
                                                                                for (int Rsid = 0; Rsid < rooms.GetLength(0); Rsid++)
                                                                                {

                                                                                    //int rooms_Id = 0;
                                                                                    //if (fitruumsResponse.Tables["rooms"].Columns.Contains("rooms_Id") == true)
                                                                                    //{
                                                                                    //    rooms_Id = Convert.ToInt32(rooms[Rsid]["rooms_Id"]);
                                                                                    //}
                                                                                    // if (fitruumsResponse.Tables.Contains("room") == true)
                                                                                    {
                                                                                        // if (fitruumsResponse.Tables["room"].Columns.Contains("rooms_Id") == true)
                                                                                        {
                                                                                            DataRow[] room = fitruumsResponse.Tables["room"].Select("rooms_Id='" + rooms[Rsid]["rooms_Id"] + "'");
                                                                                            if (room.Length > 0)
                                                                                            {
                                                                                                for (int Rid = 0; Rid < room.GetLength(0); Rid++)
                                                                                                {
                                                                                                    //int room_Id = 0; int RoomIdnew = 0;
                                                                                                    //if (fitruumsResponse.Tables["room"].Columns.Contains("room_Id") == true)
                                                                                                    //{
                                                                                                    //    room_Id = Convert.ToInt32(room[Rid]["room_Id"]);
                                                                                                    //}
                                                                                                    //if (fitruumsResponse.Tables["room"].Columns.Contains("Id") == true)
                                                                                                    //{
                                                                                                    //    RoomIdnew = Convert.ToInt32(room[Rid]["Id"]);
                                                                                                    //}
                                                                                                    if (fitruumsResponse.Tables.Contains("meals") == true)
                                                                                                    {
                                                                                                        //if (fitruumsResponse.Tables["meals"].Columns.Contains("room_Id") == true)
                                                                                                        {
                                                                                                            DataRow[] meals = fitruumsResponse.Tables["meals"].Select("room_Id='" + room[Rid]["room_Id"] + "'");
                                                                                                            if (meals.Length > 0)
                                                                                                            {

                                                                                                                for (int Msid = 0; Msid < meals.GetLength(0); Msid++)
                                                                                                                {
                                                                                                                    //int meals_Id = 0;
                                                                                                                    //if (fitruumsResponse.Tables["meals"].Columns.Contains("meals_Id") == true)
                                                                                                                    //{
                                                                                                                    //    meals_Id = Convert.ToInt32(meals[Msid]["meals_Id"]);
                                                                                                                    //}
                                                                                                                    if (fitruumsResponse.Tables.Contains("meal") == true)
                                                                                                                    {
                                                                                                                        //  if (fitruumsResponse.Tables["meal"].Columns.Contains("meals_Id") == true)
                                                                                                                        {
                                                                                                                            DataRow[] meal = fitruumsResponse.Tables["meal"].Select("meals_Id='" + meals[Msid]["meals_Id"] + "'");
                                                                                                                            if (meal.Length > 0)
                                                                                                                            {
                                                                                                                                for (int Mid = 0; Mid < meal.GetLength(0); Mid++)
                                                                                                                                {
                                                                                                                                    // DataRow drHotelImage = HotelRooms.NewRow();
                                                                                                                                    // int meal_Id = 0;
                                                                                                                                    string MealName = "RO";//, MealId = "";
                                                                                                                                    //if (fitruumsResponse.Tables["meal"].Columns.Contains("meal_Id") == true)
                                                                                                                                    //{
                                                                                                                                    //    meal_Id = Convert.ToInt32(meal[Mid]["meal_Id"]);
                                                                                                                                    //}
                                                                                                                                    //if (fitruumsResponse.Tables["meal"].Columns.Contains("Id") == true)
                                                                                                                                    //{
                                                                                                                                    //    MealId = Convert.ToString(meal[Mid]["Id"]);
                                                                                                                                    //}
                                                                                                                                    if (fitruumsResponse.Tables["meal"].Columns.Contains("Id") == true)
                                                                                                                                    {
                                                                                                                                        string MealId = Convert.ToString(meal[Mid]["Id"]);
                                                                                                                                        switch (MealId)
                                                                                                                                        {
                                                                                                                                            case "1":
                                                                                                                                                MealName = "Room Only";
                                                                                                                                                break;
                                                                                                                                            case "3":
                                                                                                                                                MealName = "Breakfast";
                                                                                                                                                break;
                                                                                                                                            case "4":
                                                                                                                                                MealName = "Half board";
                                                                                                                                                break;
                                                                                                                                            case "5":
                                                                                                                                                MealName = "Full board";
                                                                                                                                                break;
                                                                                                                                            case "6":
                                                                                                                                                MealName = "All inclusive";
                                                                                                                                                break;
                                                                                                                                            default:
                                                                                                                                                MealName = "Room Only";
                                                                                                                                                break;
                                                                                                                                        }
                                                                                                                                    }
                                                                                                                                    if (fitruumsResponse.Tables["meal"].Columns.Contains("name") == true)
                                                                                                                                    {
                                                                                                                                        MealName = Convert.ToString(meal[Mid]["name"]);
                                                                                                                                        if (MealName == "No meals")
                                                                                                                                        {
                                                                                                                                            MealName = "Room Only";
                                                                                                                                        }
                                                                                                                                    }
                                                                                                                                    if (MealName == "")
                                                                                                                                    {
                                                                                                                                        MealName = "Room Only";
                                                                                                                                    }
                                                                                                                                    //if (fitruumsResponse.Tables["meal"].Columns.Contains("name") == true)
                                                                                                                                    //{
                                                                                                                                    //    MealName = Convert.ToString(meal[Mid]["name"]);
                                                                                                                                    //    if (MealName == "No meals")
                                                                                                                                    //    {
                                                                                                                                    //        MealName = "Room Only";
                                                                                                                                    //    }
                                                                                                                                    //}
                                                                                                                                    if (fitruumsResponse.Tables.Contains("Prices") == true)
                                                                                                                                    {
                                                                                                                                        // if (fitruumsResponse.Tables["Prices"].Columns.Contains("meal_Id") == true)
                                                                                                                                        {
                                                                                                                                            DataRow[] Prices = fitruumsResponse.Tables["Prices"].Select("meal_Id='" + meal[Mid]["meal_Id"] + "'");
                                                                                                                                            if (Prices.Length > 0)
                                                                                                                                            {
                                                                                                                                                for (int Psid = 0; Psid < Prices.GetLength(0); Psid++)
                                                                                                                                                {
                                                                                                                                                    //int prices_Id = 0;
                                                                                                                                                    //if (fitruumsResponse.Tables["Prices"].Columns.Contains("prices_Id") == true)
                                                                                                                                                    //{
                                                                                                                                                    //    prices_Id = Convert.ToInt32(Prices[Psid]["prices_Id"]);
                                                                                                                                                    //}
                                                                                                                                                    if (fitruumsResponse.Tables.Contains("price") == true)
                                                                                                                                                    {
                                                                                                                                                        // if (fitruumsResponse.Tables["price"].Columns.Contains("prices_Id") == true)
                                                                                                                                                        {
                                                                                                                                                            DataRow[] price = fitruumsResponse.Tables["price"].Select("prices_Id='" + Prices[Psid]["prices_Id"] + "'");
                                                                                                                                                            if (price.Length > 0)
                                                                                                                                                            {
                                                                                                                                                                for (int Pid = 0; Pid < price.GetLength(0); Pid++)
                                                                                                                                                                {
                                                                                                                                                                    double price_Text = 0.00;
                                                                                                                                                                    
                                                                                                                                                                    if (fitruumsResponse.Tables["price"].Columns.Contains("price_Text") == true)
                                                                                                                                                                    {
                                                                                                                                                                        price_Text = Convert.ToDouble(price[Pid]["price_Text"]);
                                                                                                                                                                    }
                                                                                                                                                                   
                                                                                                                                                                    price_Text = price_Text * FitruumsCurrencyFactor; //added
                                                                                                                                                                    int noofnight = Convert.ToInt32(SearchParam.NoofNights);
                                                                                                                                                                    string WeekColumn = "";
                                                                                                                                                                    string uniqid = Guid.NewGuid().ToString();

                                                                                                                                                                    string MainRoomtypeId = room[Rid]["Id"] + "$" + NationalityCode + "$" + meal[Mid]["Id"];
                                                                                                                                                                    #region RateBreackUp

                                                                                                                                                                    for (int nigid = 0; nigid < noofnight; nigid++)
                                                                                                                                                                    {

                                                                                                                                                                        string dtcc = SearchParam.CheckInDate.Substring(3, 2) + "/" + SearchParam.CheckInDate.Substring(0, 2) + "/" + SearchParam.CheckInDate.Substring(6, 4);
                                                                                                                                                                        DateTime dt = Convert.ToDateTime(dtcc);
                                                                                                                                                                        string datetime = Convert.ToString(dt.Date.AddDays(nigid));
                                                                                                                                                                        WeekColumn = Convert.ToString(dt.Date.AddDays(nigid).DayOfWeek).Substring(0, 3);

                                                                                                                                                                        double DateGross = 0;

                                                                                                                                                                        DateGross = Convert.ToDouble(price[Pid]["price_Text"]) / noofnight;
                                                                                                                                                                        if (noofnight == 0)
                                                                                                                                                                        {
                                                                                                                                                                            price_Text = DateGross;
                                                                                                                                                                        }
                                                                                                                                                                        //if (XMLHotelBookingCommon.Darina_Currency == "7") // if currency is in USD
                                                                                                                                                                        //    DateGross = DateGross * XMLHotelBookingCommon.Darina_CurrencyFactor;
                                                                                                                                                                        #region Vat
                                                                                                                                                                        double VatOnRateBrackup = 0;
                                                                                                                                                                        DateGross = _markup.ProcessHotelRateBreakupPrice_WithMarkup(1, SearchParam.CityID, SearchParam.CountryID.ToString(), DateGross, commonSearch.B2CMarkup, commonSearch.IsParentId, isPerce, AgentPercentage, AgentMarkupAmount, commonSearch.SubAgentMarkup, ref VatOnRateBrackup);
                                                                                                                                                                        #endregion

                                                                                                                                                                        //DataRow drfitruumsRoomRateBreackUp = d.NewRow();
                                                                                                                                                                        //drfitruumsRoomRateBreackUp["RoomNo"] = Convert.ToString("1");
                                                                                                                                                                        //drfitruumsRoomRateBreackUp["SupplierName"] = "fitruums";
                                                                                                                                                                        //drfitruumsRoomRateBreackUp["OwnsystemHotelId"] = OwnsystemHotelId;
                                                                                                                                                                        //drfitruumsRoomRateBreackUp["ContractID"] = 0;
                                                                                                                                                                        //drfitruumsRoomRateBreackUp["RoomTypeName"] = RoomeTypeName;
                                                                                                                                                                        //drfitruumsRoomRateBreackUp["RoomTypeId"] = MainRoomtypeId;
                                                                                                                                                                        //drfitruumsRoomRateBreackUp["MealId"] = MealId;
                                                                                                                                                                        //drfitruumsRoomRateBreackUp["Meal"] = MealName;
                                                                                                                                                                        //drfitruumsRoomRateBreackUp["BookingStatus"] = "Available";
                                                                                                                                                                        //drfitruumsRoomRateBreackUp["DateOfWeek"] = WeekColumn;
                                                                                                                                                                        //drfitruumsRoomRateBreackUp["Date"] = datetime;
                                                                                                                                                                        //drfitruumsRoomRateBreackUp["Rate"] = DateGross;
                                                                                                                                                                        //drfitruumsRoomRateBreackUp["VatdiffAmount"] = VatOnRateBrackup;
                                                                                                                                                                        //drfitruumsRoomRateBreackUp["RateType"] = "A Rate";
                                                                                                                                                                        //drfitruumsRoomRateBreackUp["OrderNo"] = 1;
                                                                                                                                                                        //drfitruumsRoomRateBreackUp["RoomSequenceNo"] = uniqid;
                                                                                                                                                                        //drfitruumsRoomRateBreackUp["BoardTypeID"] = "";
                                                                                                                                                                        //drfitruumsRoomRateBreackUp["SupplierCurrency"] = Currency;
                                                                                                                                                                        //drfitruumsRoomRateBreackUp["BuyingPrice"] = DateGross / CurrencyFactor;
                                                                                                                                                                        //dtHotelRoom.Rows.Add(drfitruumsRoomRateBreackUp);
                                                                                                                                                                        //DtfitruumsRoomRateBreackUp.AcceptChanges();
                                                                                                                                                                    }
                                                                                                                                                                    #endregion

                                                                                                                                                                    #region Cancelation

                                                                                                                                                                    int room_Id = 0; int RoomIdnew = 0;
                                                                                                                                                                    if (fitruumsResponse.Tables["room"].Columns.Contains("room_Id") == true)
                                                                                                                                                                    {
                                                                                                                                                                        room_Id = Convert.ToInt32(room[Rid]["room_Id"]);
                                                                                                                                                                    }
                                                                                                                                                                    if (fitruumsResponse.Tables.Contains("cancellation_policies") == true)
                                                                                                                                                                    {
                                                                                                                                                                        if (fitruumsResponse.Tables["cancellation_policies"].Columns.Contains("room_Id") == true)
                                                                                                                                                                        {
                                                                                                                                                                            DataRow[] drCancellation = fitruumsResponse.Tables["cancellation_policies"].Select("room_Id='" + room_Id + "'");
                                                                                                                                                                            if (drCancellation.Length > 0)
                                                                                                                                                                            {

                                                                                                                                                                                for (int CCid = 0; CCid < drCancellation.GetLength(0); CCid++)
                                                                                                                                                                                {
                                                                                                                                                                                    int cancellation_policies_Id = 0;
                                                                                                                                                                                    if (fitruumsResponse.Tables["cancellation_policies"].Columns.Contains("cancellation_policies_Id") == true)
                                                                                                                                                                                    {
                                                                                                                                                                                        cancellation_policies_Id = Convert.ToInt32(drCancellation[CCid]["cancellation_policies_Id"]);
                                                                                                                                                                                    }
                                                                                                                                                                                    if (fitruumsResponse.Tables.Contains("cancellation_policy") == true)
                                                                                                                                                                                    {
                                                                                                                                                                                        if (fitruumsResponse.Tables["cancellation_policy"].Columns.Contains("cancellation_policies_Id") == true)
                                                                                                                                                                                        {
                                                                                                                                                                                            DataRow[] drCancellationdata = fitruumsResponse.Tables["cancellation_policy"].Select("cancellation_policies_Id='" + cancellation_policies_Id + "'");
                                                                                                                                                                                            if (drCancellationdata.Length > 0)
                                                                                                                                                                                            {
                                                                                                                                                                                                string deadline = ""; int percentage = 0;
                                                                                                                                                                                                for (int CCCid = 0; CCCid < drCancellationdata.GetLength(0); CCCid++)
                                                                                                                                                                                                {
                                                                                                                                                                                                    if (fitruumsResponse.Tables["cancellation_policy"].Columns.Contains("deadline") == true)
                                                                                                                                                                                                    {
                                                                                                                                                                                                        deadline = Convert.ToString(drCancellationdata[CCCid]["deadline"]);
                                                                                                                                                                                                    }
                                                                                                                                                                                                    if (fitruumsResponse.Tables["cancellation_policy"].Columns.Contains("percentage") == true)
                                                                                                                                                                                                    {
                                                                                                                                                                                                        percentage = Convert.ToInt32(drCancellationdata[CCCid]["percentage"]);
                                                                                                                                                                                                    }


                                                                                                                                                                                                    string dtfrom = Convert.ToString(SearchParam.CheckInDate);
                                                                                                                                                                                                    dtfrom = dtfrom.Substring(3, 2) + "/" + dtfrom.Substring(0, 2) + "/" + dtfrom.Substring(6, 4);
                                                                                                                                                                                                    DateTime FromDate = Convert.ToDateTime((Convert.ToString(dtfrom).Replace("/", "-")));
                                                                                                                                                                                                    string dtTO = Convert.ToString(SearchParam.CheckOutDate);
                                                                                                                                                                                                    dtTO = dtTO.Substring(3, 2) + "/" + dtTO.Substring(0, 2) + "/" + dtTO.Substring(6, 4);
                                                                                                                                                                                                    DateTime ToDate = Convert.ToDateTime((Convert.ToString(dtTO).Replace("/", "-")));

                                                                                                                                                                                                    double ChargeAmount = Convert.ToDouble(price_Text);
                                                                                                                                                                                                    //string AmoutnType = Convert.ToString(0.00).Replace(",", "");

                                                                                                                                                                                                    //#region Vat
                                                                                                                                                                                                    //double VatOnPolicy = 0;
                                                                                                                                                                                                    //ChargeAmount = Common.ProcessHotelCancelationPrice_WithMarkup(objSearPera.NoofNights, objSearPera.CityID, objSearPera.CountryId, ChargeAmount, isPerce, B2CMarkup, AgentPercentage, AgentMarkupAmount, IsParentId, SubAgentMarkup, ref VatOnPolicy);
                                                                                                                                                                                                    //#endregion

                                                                                                                                                                                                    //#region Vat //29-Oct-2024
                                                                                                                                                                                                    //double VatOnPolicy = 0;
                                                                                                                                                                                                    //double VatOnPolicyChargeAmount = ChargeAmount;
                                                                                                                                                                                                    //double CompanyChargeAmount = ChargeAmount;
                                                                                                                                                                                                    //ChargeAmount = Common.ProcessHotelCancelationPrice_WithMarkup(ref VatOnPolicyChargeAmount, SearchParam.NoofNights, SearchParam.CityID, SearchParam.CountryID.ToString(), ChargeAmount, isPerce, commonSearch.B2CMarkup, AgentPercentage, AgentMarkupAmount, commonSearch.IsParentId, commonSearch.SubAgentMarkup, ref VatOnPolicy);
                                                                                                                                                                                                    //#endregion

                                                                                                                                                                                                    if (deadline != "")
                                                                                                                                                                                                    {
                                                                                                                                                                                                        //CutOffDays = -2;
                                                                                                                                                                                                        int deadlineval = Convert.ToInt32(deadline);
                                                                                                                                                                                                        FromDate = FromDate.AddHours(-deadlineval);
                                                                                                                                                                                                        FromDate = FromDate.AddDays(CutOffDays);
                                                                                                                                                                                                        ToDate = ToDate.AddDays(CutOffDays);
                                                                                                                                                                                                        //new add
                                                                                                                                                                                                        ChargeAmount = (ChargeAmount / 100) * percentage;
                                                                                                                                                                                                        ChargeAmount = Math.Round(ChargeAmount, 2);
                                                                                                                                                                                                    }
                                                                                                                                                                                                    else
                                                                                                                                                                                                    {
                                                                                                                                                                                                        FromDate = DateTime.Now.Date;
                                                                                                                                                                                                        //new add
                                                                                                                                                                                                        ChargeAmount = (ChargeAmount / 100) * percentage;
                                                                                                                                                                                                        ChargeAmount = Math.Round(ChargeAmount, 2);
                                                                                                                                                                                                    }


                                                                                                                                                                                                    #region Vat //29-Oct-2024
                                                                                                                                                                                                    double VatOnPolicy = 0;
                                                                                                                                                                                                    double VatOnPolicyChargeAmount = ChargeAmount;
                                                                                                                                                                                                    double CompanyChargeAmount = ChargeAmount;
                                                                                                                                                                                                    ChargeAmount = _markup.ProcessHotelCancelationPrice_WithMarkup(ref VatOnPolicyChargeAmount, SearchParam.NoofNights, SearchParam.CityID, SearchParam.CountryID.ToString(), ChargeAmount, isPerce, commonSearch.B2CMarkup, AgentPercentage, AgentMarkupAmount, commonSearch.IsParentId, commonSearch.SubAgentMarkup, ref VatOnPolicy);
                                                                                                                                                                                                    #endregion

                                                                                                                                                                                                    if (ChargeAmount == 0)
                                                                                                                                                                                                        FromDate = DateTime.Now.Date;

                                                                                                                                                                                                    DateTime curDate = DateTime.Now.Date;
                                                                                                                                                                                                    if (curDate < FromDate)
                                                                                                                                                                                                    {
                                                                                                                                                                                                        if (FromDate < DateTime.Now.Date)
                                                                                                                                                                                                        {
                                                                                                                                                                                                            FromDate = DateTime.Now.Date;
                                                                                                                                                                                                        }
                                                                                                                                                                                                        if (FromDate == ToDate)
                                                                                                                                                                                                            FromDate = FromDate.AddDays(-1);
                                                                                                                                                                                                        if (CCCid == 0)
                                                                                                                                                                                                        {
                                                                                                                                                                                                            double zero = 0;
                                                                                                                                                                                                            dtCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref zero, dtCancellation.NewRow(), FitruumsCurrencyFactor, FitruumsCurrency, Item.Supplier, Convert.ToString(OwnsystemHotelId), Convert.ToString(1),
                                                                                                                                                                                                                                       Convert.ToString(MainRoomtypeId), RoomeTypeName, Convert.ToString(meal[Mid]["Id"]), DateTime.Now.Date, Convert.ToDateTime(FromDate.AddDays(-1)), 0, 0, "0", VatOnPolicy, "", "NO", "true", "", "", ""));
                                                                                                                                                                                                            dtCancellation.AcceptChanges();
                                                                                                                                                                                                        }

                                                                                                                                                                                                        dtCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref CompanyChargeAmount, dtCancellation.NewRow(), FitruumsCurrencyFactor, FitruumsCurrency, Item.Supplier, Convert.ToString(OwnsystemHotelId), Convert.ToString(1),
                                                                                                                                                                                                             Convert.ToString(MainRoomtypeId), RoomeTypeName, Convert.ToString(meal[Mid]["Id"]), FromDate.Date, DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null), VatOnPolicyChargeAmount, ChargeAmount, "", VatOnPolicy, "", "NO", "true", "", "", ""));
                                                                                                                                                                                                        dtCancellation.AcceptChanges();
                                                                                                                                                                                                    }
                                                                                                                                                                                                    else
                                                                                                                                                                                                    {
                                                                                                                                                                                                        //#region Vat //29-Oct-2024
                                                                                                                                                                                                        //double VatOnPolicy = 0;
                                                                                                                                                                                                        //double VatOnPolicyChargeAmount = ChargeAmount;
                                                                                                                                                                                                        //double CompanyChargeAmount = ChargeAmount;
                                                                                                                                                                                                        //ChargeAmount = Common.ProcessHotelCancelationPrice_WithMarkup(ref VatOnPolicyChargeAmount, SearchParam.NoofNights, SearchParam.CityID, SearchParam.CountryID.ToString(), ChargeAmount, isPerce, commonSearch.B2CMarkup, AgentPercentage, AgentMarkupAmount, commonSearch.IsParentId, commonSearch.SubAgentMarkup, ref VatOnPolicy);
                                                                                                                                                                                                        //#endregion

                                                                                                                                                                                                        dtCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref CompanyChargeAmount, dtCancellation.NewRow(), FitruumsCurrencyFactor, FitruumsCurrency, Item.Supplier, Convert.ToString(OwnsystemHotelId), Convert.ToString(1),
                                                                                                                                                                                                         Convert.ToString(MainRoomtypeId), RoomeTypeName, Convert.ToString(meal[Mid]["Id"]), DateTime.Now.Date, DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null), VatOnPolicyChargeAmount, ChargeAmount, "", VatOnPolicy, "", "YES", "false", "", "", ""));
                                                                                                                                                                                                        dtCancellation.AcceptChanges();
                                                                                                                                                                                                    }
                                                                                                                                                                                                    //if (curDate < FromDate)
                                                                                                                                                                                                    //{
                                                                                                                                                                                                    //    if (FromDate < DateTime.Now.Date)
                                                                                                                                                                                                    //    {
                                                                                                                                                                                                    //        FromDate = DateTime.Now.Date;
                                                                                                                                                                                                    //    }

                                                                                                                                                                                                    //    if (FromDate == ToDate)
                                                                                                                                                                                                    //        FromDate = FromDate.AddDays(-1);
                                                                                                                                                                                                    //    if (CCCid == 0)
                                                                                                                                                                                                    //    {
                                                                                                                                                                                                    //        dtCancellation.Rows.Add(Common.setCancellationPolicyRow(dtCancellation.NewRow(), Factor, Currency, "fitruums", ownsystemhotelId, Convert.ToString(1),
                                                                                                                                                                                                    //                                   Convert.ToString(MainRoomtypeId), RoomeTypeName, MealId, DateTime.Now.Date, Convert.ToDateTime(FromDate.AddDays(-1)), 0, 0, "0", VatOnPolicy, "", "NO", "", "", "", ""));
                                                                                                                                                                                                    //        dtCancellation.AcceptChanges();
                                                                                                                                                                                                    //        dtCancellation.Rows.Add(Common.setCancellationPolicyRow(dtCancellation.NewRow(), Factor, Currency, "fitruums", ownsystemhotelId, Convert.ToString(1),
                                                                                                                                                                                                    //         Convert.ToString(MainRoomtypeId), RoomeTypeName, MealId, FromDate.Date, DateTime.ParseExact(objSearPera.CheckInDate, "dd/MM/yyyy", null), ChargeAmount, ChargeAmount, "", VatOnPolicy, "", "NO", "", "", "", ""));
                                                                                                                                                                                                    //        dtCancellation.AcceptChanges();
                                                                                                                                                                                                    //}
                                                                                                                                                                                                    //else
                                                                                                                                                                                                    //{
                                                                                                                                                                                                    //        dtCancellation.Rows.Add(Common.setCancellationPolicyRow(dtCancellation.NewRow(), Factor, Currency, "fitruums", ownsystemhotelId, Convert.ToString(1),
                                                                                                                                                                                                    //     Convert.ToString(MainRoomtypeId), RoomeTypeName, MealId, DateTime.Now.Date, DateTime.ParseExact(objSearPera.CheckInDate, "dd/MM/yyyy", null), ChargeAmount, ChargeAmount, "", VatOnPolicy, "", "YES", "", "", "", ""));
                                                                                                                                                                                                    //        dtCancellation.AcceptChanges();
                                                                                                                                                                                                    //}
                                                                                                                                                                                                }
                                                                                                                                                                                            }
                                                                                                                                                                                        }
                                                                                                                                                                                    }
                                                                                                                                                                                }
                                                                                                                                                                            }
                                                                                                                                                                        }
                                                                                                                                                                    }
                                                                                                                                                                    #endregion

                                                                                                                                                                    #region Room
                                                                                                                                                                    Double RatePerStay = price_Text;
                                                                                                                                                                    DataRow drfitruumsRoom = dtHotelRoom.NewRow();
                                                                                                                                                                    drfitruumsRoom["OwnsystemHotelId"] = OwnsystemHotelId;
                                                                                                                                                                    drfitruumsRoom["ContractID"] = 0;
                                                                                                                                                                    drfitruumsRoom["UniqueNo"] = OwnsystemHotelId;
                                                                                                                                                                    drfitruumsRoom["RoomSequenceNo"] = uniqid;
                                                                                                                                                                    drfitruumsRoom["MealName"] = MealName;
                                                                                                                                                                    drfitruumsRoom["allocationDetails"] = "";
                                                                                                                                                                    drfitruumsRoom["minStay"] = 0;
                                                                                                                                                                    drfitruumsRoom["DateApplyMinStay"] = "";
                                                                                                                                                                    drfitruumsRoom["MealId"] = meal[Mid]["Id"];
                                                                                                                                                                    drfitruumsRoom["RoomNo"] = Convert.ToString("1");
                                                                                                                                                                    drfitruumsRoom["EssentialInformation"] = "";
                                                                                                                                                                    drfitruumsRoom["RoomTypeName"] = RoomeTypeName;// RoomType[RTid]["room.type"];
                                                                                                                                                                    drfitruumsRoom["RoomTypeId"] = MainRoomtypeId;
                                                                                                                                                                    drfitruumsRoom["BookingStatus"] = "Available";
                                                                                                                                                                    drfitruumsRoom["AdultsOccupancy"] = SearchParam.Rooms[0].NoofAdults;
                                                                                                                                                                    drfitruumsRoom["ChildOccupancy"] = SearchParam.Rooms[0].NoofChild;
                                                                                                                                                                    drfitruumsRoom["MaxExtrabed"] = 0;
                                                                                                                                                                    drfitruumsRoom["BoardTypeID"] = "";
                                                                                                                                                                    drfitruumsRoom["CompanyBuyingPrice"] = RatePerStay;
                                                                                                                                                                    drfitruumsRoom["OfferDescription"] = "";
                                                                                                                                                                    drfitruumsRoom["offerCode"] = "";

                                                                                                                                                                    double AgentBuyingPrice = RatePerStay;

                                                                                                                                                                    #region Vat
                                                                                                                                                                    Double VatOnTotal = 0;
                                                                                                                                                                    _markup.ProcessHotelRoom_WithMarkup(FitruumsCurrencyFactor, FitruumsCurrency, false, true, SearchParam.NoofNights, SearchParam.CityID, SearchParam.CountryID.ToString(), SearchParam.CheckInDate, SearchParam.CheckOutDate, ref RatePerStay, ref drfitruumsRoom, isPerce, AgentPercentage, AgentMarkupAmount, commonSearch.SubAgentMarkup, commonSearch.B2CMarkup, commonSearch.IsParentId);
                                                                                                                                                                    VatOnTotal = Convert.ToDouble(drfitruumsRoom["VatdiffAmount"]);
                                                                                                                                                                    #endregion

                                                                                                                                                                    // drfitruumsRoom["SupplierName"] = "fitruums";
                                                                                                                                                                    drfitruumsRoom["SupplierName"] = Item.Supplier;
                                                                                                                                                                    drfitruumsRoom["IsCombineRoom"] = true;
                                                                                                                                                                    dtHotelRoom.Rows.Add(drfitruumsRoom);
                                                                                                                                                                    dtHotelRoom.AcceptChanges();
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
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    #endregion

                                }
                                #endregion
                                Options ops = (await _supplierRes.GetCommonRoomForSuppliers(GiataId, commonSearch.tokenData, commonSearch.advancedOptions, LowestRate, drXmlHotel, dtHotelRoom, dtCancellation, DtRoomRateBreackUp, SearchParam));
                                if (ops != null)
                                    options.Add(ops);
                            }
                            #endregion
                            #endregion
                        }

                        #endregion
                    }
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
                    timer_room_Fitruums.Stop();
                    Item.CodeProcessingTimeSupplier = timer_room_Fitruums.ElapsedMilliseconds;
                    if (SearchParam != null && ResProccessTimerLogActive == "1" && timer_room_Fitruums.ElapsedMilliseconds > Convert.ToInt32(ResProccessTimeValueforSaveLog))
                    {
                        
                        SearchParam.SupplierTime += ",|Finish:Fitrumms Process Response  " + "|Time :" + timer_room_Fitruums.Elapsed.ToString();
                        
                        _= Task.Run(() => _commonMethod.FireAway_SQL_ResPorcessTimeLog("", timer_room_Fitruums.Elapsed.ToString(), Item.Supplier, SearchParam.SearchKey, 1, SearchParam.AgentID));
                    }
                }
                catch (Exception x)
                {
                    await _exception.Error(x);
                }
                #endregion
                //GC.Collect();
                if (Item.ResponseType == "NoCount" && StoreSupplierResErrorLog == "1")
                    _ = Task.Run(() => _commonMethod.XMLRequestResponseTrack_Error(Item.RQRS[0].Request, Item.RQRS[0].Response, Item.Supplier, timer_room_Fitruums.Elapsed.ToString(), "", SearchParam.SearchKey));
            }

            return options;
        }
        public  DataTable GenerateRooms()
        {
            try
            {
                DataTable Rooms = new DataTable();
                Rooms.Columns.Add("HotelCode", typeof(int));
                Rooms.Columns.Add("RoomType", typeof(string));
                Rooms.Columns.Add("HotelName", typeof(string));
                Rooms.Columns.Add("RoomtypeId", typeof(int));
                Rooms.Columns.Add("MealName", typeof(string));
                Rooms.Columns.Add("Price", typeof(string));
                return Rooms;
            }
            catch (System.Exception ex)
            {
                _exception.Error(ex);
            }
            return null;
        }

        //Recheck
        public async Task<CancellationDto> GetFitruumsCancelationpolicy(int AgentId, DataSet dsHotel, string ownsystemid, string RoomTypeId_Own, DataTable dtAgentDetail, DataTable dtSuplierMarkup, DataTable dtMarkupDetails, DataTable dtSearchConfig, HotelSearchParameter objSearPera, string roomno, string provider, string SearchKey, int hotelRating = 0, string XmlNationality = "", string RoomTypeName = "", string MealId = "", string Companybuy = "")
        {
            CancellationDto cancellationDto = new CancellationDto();

            string ISBooking = "Available";


            DataTable dtHotelRoomxmlCancellation = new DataTable();
            // string roomtypeid = "", Mealname = "", citycode = "", board = "", shortname = "", characteristic = "", type = "", boardtype = "", roomtypecode1 = "";

            int CutOffDays = XMLHotelBookingCommon.CutOffDays;
            //CutOffDays = Common.GetCutOffDays(dtSuplierMarkup, "fitruums");
            CutOffDays = _commonMethod.GetCutOffDays(dtSuplierMarkup, provider);

            string XmlCommanRQRS = _commonMethod.GenerateGTARequest(objSearPera);
            DataSet dsOriginalRequest = _commonMethod.GetDataSetFromXMLString(XmlCommanRQRS);
            string roomtypecode = "", roomid = "";
            double rateprice = 00;
            double finalprice = 00;
            double VatOnfinalprice = 0;
            DataTable dtfitruumsCancellation = _commonMethod.GenerateCancellationDataTableNewV2();

            // if (dsHotel.Tables[1] != null && dsHotel.Tables[1].Rows.Count > 0)
            {
                //if (RoomTypeId_Own != "")
                //{
                //    dsHotel.Tables[1].DefaultView.RowFilter = "OwnsystemHotelId=" + ownsystemid + "and RoomTypeId='" + RoomTypeId_Own + "' and RoomNo='" + roomno + "'";
                //    dsHotel.Tables[2].DefaultView.RowFilter = "OwnsystemHotelId=" + ownsystemid + "and RoomTypeId='" + RoomTypeId_Own + "'and RoomNo='" + roomno + "'";

                //}
                //else
                //{
                //    dsHotel.Tables[1].DefaultView.RowFilter = "OwnsystemHotelId='" + ownsystemid + "'";
                //    dsHotel.Tables[2].DefaultView.RowFilter = "OwnsystemHotelId='" + ownsystemid + "'";
                //}
                //if (dsHotel.Tables[1].DefaultView.Count > 0 && dsHotel.Tables[2].DefaultView.Count == 0)
                {
                    List<DataRow> rowsToDelete = new List<DataRow>();
                    //foreach (DataRow item in dsHotel.Tables[2].DefaultView.ToTable().Rows)
                    //{
                    //    if (item.ItemArray[1].ToString() == ownsystemid && item.ItemArray[3].ToString() == roomtypeid)
                    //        rowsToDelete.Add(item);
                    //}
                    //foreach (DataRow row in rowsToDelete)
                    //{
                    //    dsHotel.Tables[2].Rows.Remove(row);
                    //    dsHotel.Tables[2].AcceptChanges();
                    //}
                    Int32 GIITACode = 0;
                    //for (int c = 0; c < dsHotel.Tables[1].DefaultView.Count; c++)
                    {
                        //HttpContext.Current.Session["chkprice"] = 0.00;
                        //characteristic = Convert.ToString(dsHotel.Tables[1].DefaultView[c]["Characteristic"]);
                        //roomtypecode = Convert.ToString(dsHotel.Tables[1].DefaultView[c]["RoomTypeCode"]);
                        //boardtype = Convert.ToString(dsHotel.Tables[1].DefaultView[c]["BoardType"]);
                        //type = Convert.ToString(dsHotel.Tables[1].DefaultView[c]["Type"]);
                        //board = Convert.ToString(dsHotel.Tables[1].DefaultView[c]["Board"]);
                        //shortname = Convert.ToString(dsHotel.Tables[1].DefaultView[c]["MealId"]);
                        //roomid = Convert.ToString(dsHotel.Tables[1].DefaultView[c]["RoomTypeId"]);
                        rateprice = Convert.ToDouble(Companybuy);// Convert.ToDouble(dsHotel.Tables[1].DefaultView[c]["CompanyBuyingPrice"]);
                                                                 //finalprice = Convert.ToDouble(dsHotel.Tables[1].DefaultView[c]["FinalSellingPrice"]);
                                                                 //VatOnfinalprice = Convert.ToDouble(dsHotel.Tables[1].DefaultView[c]["VatdiffAmount"]);
                                                                 //roomno = Convert.ToString(dsHotel.Tables[1].DefaultView[c]["RoomNo"]);
                                                                 //roomtypeid = Convert.ToString(dsHotel.Tables[1].DefaultView[c]["RoomTypeId"]);
                                                                 //GIITACode = Convert.ToInt32(dsHotel.Tables[1].DefaultView[c]["GIITACode"]);
                                                                 //string RoomTypeName = Convert.ToString(dsHotel.Tables[1].DefaultView[c]["RoomTypeName"]);

                        //Mealname = Convert.ToString(dsHotel.Tables[1].DefaultView[c]["MealId"]);

                        try
                        {
                            DataRow[] fitruumsDetails = null;
                            DataRow[] fitruumsSearchConfig = null;
                            if (dtSuplierMarkup != null & dtSuplierMarkup != null && dtSuplierMarkup.Rows.Count > 0)
                            {
                                // fitruumsDetails = dtSuplierMarkup.Select("SupplierName='fitruums'");
                                fitruumsDetails = dtSuplierMarkup.Select("SupplierName='" + provider + "'");
                            }

                            string Currency = Convert.ToString(fitruumsDetails[0]["CurrencyShortName"]); //added on 30-05-25
                            if (dtSearchConfig != null & dtSearchConfig != null && dtSearchConfig.Rows.Count > 0)
                            {
                                if (objSearPera != null)
                                {
                                    if (fitruumsDetails.GetLength(0) > 0)
                                        fitruumsSearchConfig = dtSearchConfig.Select("SupplierID=" + Convert.ToString(fitruumsDetails[0]["SupplierID"]) + " and (CityID=" + objSearPera.CityID + " or CityID=0) and (Stars=0 OR (Stars>=" + objSearPera.StarStart + " and Stars<=" + objSearPera.StarEnd + "))");//10 for DOTW
                                }
                            }
                            string reqString = GetFitruumsCanelationpolicyRequestRequest(RoomTypeId_Own.Split('$')[0], objSearPera, fitruumsDetails, MealId, rateprice, XmlNationality, Currency);
                            string Error = "";
                            //string XMLResponse = XMLHotelBookingCommon.GetSupplierResponseFitruums(ref Error, "", reqString, Convert.ToString(fitruumsDetails[0]["WebSiteURL"]), Convert.ToInt32(fitruumsSearchConfig[0]["HotelSearchTimeOut"]), "fitruums");
                            //string XMLResponse = GetSupplierResponseFitruums(ref Error, "", reqString, Convert.ToString(fitruumsDetails[0]["WebSiteURL"]), Convert.ToInt32(fitruumsSearchConfig[0]["HotelSearchTimeOut"]), provider);
                            ResponseDto responseDto = await GetSupplierResponseFitruums("", reqString, Convert.ToString(fitruumsDetails[0]["WebSiteURL"]), Convert.ToInt32(fitruumsSearchConfig[0]["HotelSearchTimeOut"]), provider);
                            // Common.ThirdPartyGetPolicyHistory(Convert.ToString(RoomTypeId_Own), Convert.ToString(MealId), reqString, XMLResponse, "After Get Policy", "fitruums", "Response", "-", "");

                            string XMLResponse = responseDto.Response;
                            Error = responseDto.Error;


                            _commonMethod.ThirdPartyGetPolicyHistory(Convert.ToString(RoomTypeId_Own), Convert.ToString(MealId), reqString, XMLResponse, "After Get Policy", provider, "Response", "-", "");

                            string checkInDate = string.Empty;

                            if (dsOriginalRequest != null)
                                checkInDate = Convert.ToString(dsOriginalRequest.Tables["StayDateRange"].Rows[0]["Start"]);


                            XMLHotelBookingCommon objhotel = new XMLHotelBookingCommon();
                            double SubAgentMarkup = 0;
                            int IsParentId = 1;
                            if (dtAgentDetail != null)
                            {
                                IsParentId = Convert.ToInt32(dtAgentDetail.Rows[0]["IsParentId"]);
                                SubAgentMarkup = Convert.ToDouble(dtAgentDetail.Rows[0]["B2CMarkupPer"]);
                            }
                            double AgentPercentage = 0.0;
                            double AgentMarkupAmount = 0.0;
                            Boolean isPerce = true;
                            Boolean blnIsShowSupplier = false;
                            //Common.getAgetMarkupSetting("fitruums", ownsystemid.ToString(), dtSuplierMarkup, dtMarkupDetails, ref AgentPercentage, ref AgentMarkupAmount, ref blnIsShowSupplier, ref isPerce, AgentId, hotelRating, objSearPera.CityID, objSearPera.CountryID.ToString(), objSearPera.CheckInDate);
                            _markup.getAgetMarkupSetting(provider, ownsystemid.ToString(), dtSuplierMarkup, dtMarkupDetails, ref AgentPercentage, ref AgentMarkupAmount, ref blnIsShowSupplier, ref isPerce, AgentId, hotelRating, objSearPera.CityID, objSearPera.CountryID.ToString(), objSearPera.CheckInDate);

                            double AgentCompanyPercentage = Convert.ToDouble(XmlCommanRQRS.Substring(XmlCommanRQRS.IndexOf("<AgentCompanyPercentage>"), XmlCommanRQRS.IndexOf("</AgentCompanyPercentage>") - XmlCommanRQRS.IndexOf("<AgentCompanyPercentage>")).Replace("<AgentCompanyPercentage>", ""));
                            bool IsB2C = Convert.ToBoolean(Convert.ToByte(objSearPera.IsB2BOrB2C));
                            double B2CMarkup = (IsB2C == false ? AgentCompanyPercentage : XMLHotelBookingCommon.B2CMarkup);
                            #region "Cancelation Policy"

                            DataSet dsDetails = _commonMethod.GetDataSetFromXMLString(XMLResponse);

                            string aa = ""; int Factor = 1;

                            string deadline = ""; int percentage = 0; string PreBookCode = ""; string CPrice = "";
                            if (dsDetails.Tables.Contains("preBookResult") == true)
                            {
                                if (dsDetails.Tables["preBookResult"].Columns.Contains("PreBookCode") == true)
                                {
                                    PreBookCode = Convert.ToString(dsDetails.Tables["preBookResult"].Rows[0]["PreBookCode"]);
                                }
                                if (dsDetails.Tables.Contains("Price") == true)
                                {
                                    if (dsDetails.Tables["Price"].Columns.Contains("Price_Text") == true)
                                    {
                                        CPrice = Convert.ToString(dsDetails.Tables["Price"].Rows[0]["Price_Text"]);
                                    }
                                }


                                string Note = "";
                                if (dsDetails.Tables.Contains("Note") == true)
                                {
                                    for (int CCC1id = 0; CCC1id < dsDetails.Tables["Note"].Rows.Count; CCC1id++)
                                    {
                                        if (dsDetails.Tables["Note"].Columns.Contains("text") == true)
                                        {
                                            if (CCC1id == 0)
                                                Note = Convert.ToString(dsDetails.Tables["Note"].Rows[CCC1id]["text"]);
                                            else
                                                Note = Note + Convert.ToString(dsDetails.Tables["Note"].Rows[CCC1id]["text"]);
                                        }
                                        if (dsDetails.Tables["Note"].Columns.Contains("start_date") == true)
                                        {
                                            Note = Note + "  Dates:- " + Convert.ToString(dsDetails.Tables["Note"].Rows[CCC1id]["start_date"]);
                                        }
                                        if (dsDetails.Tables["Note"].Columns.Contains("end_date") == true)
                                        {
                                            Note = Note + " Between " + Convert.ToString(dsDetails.Tables["Note"].Rows[CCC1id]["end_date"]);
                                            Note = Note + "<br>";
                                        }
                                    }
                                }
                                if (dsDetails.Tables.Contains("CancellationPolicy") == true)
                                {
                                    for (int CCCid = 0; CCCid < dsDetails.Tables["CancellationPolicy"].Rows.Count; CCCid++)
                                    {
                                        if (dsDetails.Tables["CancellationPolicy"].Columns.Contains("deadline") == true)
                                        {
                                            deadline = Convert.ToString(dsDetails.Tables["CancellationPolicy"].Rows[CCCid]["deadline"]);
                                        }
                                        if (dsDetails.Tables["CancellationPolicy"].Columns.Contains("percentage") == true)
                                        {
                                            percentage = Convert.ToInt32(dsDetails.Tables["CancellationPolicy"].Rows[CCCid]["percentage"]);
                                        }


                                        string dtfrom = Convert.ToString(objSearPera.CheckInDate);
                                        dtfrom = dtfrom.Substring(3, 2) + "/" + dtfrom.Substring(0, 2) + "/" + dtfrom.Substring(6, 4);
                                        DateTime FromDate = Convert.ToDateTime((Convert.ToString(dtfrom).Replace("/", "-")));
                                        string dtTO = Convert.ToString(objSearPera.CheckOutDate);
                                        dtTO = dtTO.Substring(3, 2) + "/" + dtTO.Substring(0, 2) + "/" + dtTO.Substring(6, 4);
                                        DateTime ToDate = Convert.ToDateTime((Convert.ToString(dtTO).Replace("/", "-")));

                                        double ChargeAmount = Convert.ToDouble(rateprice);
                                        string AmoutnType = Convert.ToString(0.00).Replace(",", "");

                                        #region Vat
                                        double VatOnPolicy = 0;
                                        double VatOnPolicyChargeAmount = ChargeAmount;
                                        double CompanyChargeAmount = ChargeAmount;
                                        ChargeAmount = _markup.ProcessHotelCancelationPrice_WithMarkup(ref VatOnPolicyChargeAmount, objSearPera.NoofNights, objSearPera.CityID, objSearPera.CountryID.ToString(), ChargeAmount, isPerce, B2CMarkup, AgentPercentage, AgentMarkupAmount, IsParentId, SubAgentMarkup, ref VatOnPolicy);
                                        #endregion

                                        if (deadline != "")
                                        {

                                            int deadlineval = Convert.ToInt32(deadline);
                                            FromDate = FromDate.AddHours(-deadlineval);
                                            FromDate = FromDate.AddDays(CutOffDays);
                                            ToDate = ToDate.AddDays(CutOffDays);
                                            //new add
                                            ChargeAmount = (ChargeAmount / 100) * percentage;
                                            ChargeAmount = Math.Round(ChargeAmount, 2);

                                        }
                                        else
                                        {
                                            FromDate = DateTime.Now.Date;
                                            //new add
                                            ChargeAmount = (ChargeAmount / 100) * percentage;
                                            ChargeAmount = Math.Round(ChargeAmount, 2);
                                        }

                                        if (ChargeAmount == 0)
                                            FromDate = DateTime.Now.Date;

                                        DateTime curDate = DateTime.Now.Date;

                                        if (curDate < FromDate)
                                        {
                                            if (FromDate < DateTime.Now.Date)
                                            {
                                                FromDate = DateTime.Now.Date;
                                            }

                                            if (FromDate == ToDate)
                                                FromDate = FromDate.AddDays(-1);
                                            if (CCCid == 0)
                                            {
                                                double zero = 0;

                                                //dtfitruumsCancellation.Rows.Add(Common.setCancellationPolicyRow(ref zero, dtfitruumsCancellation.NewRow(), Factor, XMLHotelBookingCommon.FitruumsCurrency, "fitruums", ownsystemid, Convert.ToString(1),
                                                //                           Convert.ToString(RoomTypeId_Own), RoomTypeName, MealId, DateTime.Now.Date, Convert.ToDateTime(FromDate.AddDays(-1)), 0, 0, "0", VatOnPolicy, "", "NO", PreBookCode + "$" + CPrice, Note, "", ""));
                                                dtfitruumsCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref zero, dtfitruumsCancellation.NewRow(), Factor, Currency, provider, ownsystemid, Convert.ToString(1),
                                                                          Convert.ToString(RoomTypeId_Own), RoomTypeName, MealId, DateTime.Now.Date, Convert.ToDateTime(FromDate.AddDays(-1)), 0, 0, "0", VatOnPolicy, "", "NO", PreBookCode + "$" + CPrice, Note, "", ""));
                                                dtfitruumsCancellation.AcceptChanges();
                                            }

                                            //dtfitruumsCancellation.Rows.Add(Common.setCancellationPolicyRow(ref CompanyChargeAmount, dtfitruumsCancellation.NewRow(), Factor, XMLHotelBookingCommon.FitruumsCurrency, "fitruums", ownsystemid, Convert.ToString(1),
                                            //     Convert.ToString(RoomTypeId_Own), RoomTypeName, MealId, FromDate.Date, DateTime.ParseExact(objSearPera.CheckInDate, "dd/MM/yyyy", null), VatOnPolicyChargeAmount, ChargeAmount, "", VatOnPolicy, "", "NO", PreBookCode + "$" + CPrice, Note, "", ""));
                                            dtfitruumsCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref CompanyChargeAmount, dtfitruumsCancellation.NewRow(), Factor, Currency, provider, ownsystemid, Convert.ToString(1),
                                                 Convert.ToString(RoomTypeId_Own), RoomTypeName, MealId, FromDate.Date, DateTime.ParseExact(objSearPera.CheckInDate, "dd/MM/yyyy", null), VatOnPolicyChargeAmount, ChargeAmount, "", VatOnPolicy, "", "NO", PreBookCode + "$" + CPrice, Note, "", ""));
                                            dtfitruumsCancellation.AcceptChanges();
                                        }
                                        else
                                        {
                                            //dtfitruumsCancellation.Rows.Add(Common.setCancellationPolicyRow(ref CompanyChargeAmount, dtfitruumsCancellation.NewRow(), Factor, XMLHotelBookingCommon.FitruumsCurrency, "fitruums", ownsystemid, Convert.ToString(1),
                                            // Convert.ToString(RoomTypeId_Own), RoomTypeName, MealId, DateTime.Now.Date, DateTime.ParseExact(objSearPera.CheckInDate, "dd/MM/yyyy", null), VatOnPolicyChargeAmount, ChargeAmount, "", VatOnPolicy, "", "YES", PreBookCode + "$" + CPrice, Note, "", ""));
                                            dtfitruumsCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref CompanyChargeAmount, dtfitruumsCancellation.NewRow(), Factor, Currency, provider, ownsystemid, Convert.ToString(1),
                                             Convert.ToString(RoomTypeId_Own), RoomTypeName, MealId, DateTime.Now.Date, DateTime.ParseExact(objSearPera.CheckInDate, "dd/MM/yyyy", null), VatOnPolicyChargeAmount, ChargeAmount, "", VatOnPolicy, "", "YES", PreBookCode + "$" + CPrice, Note, "", ""));
                                            dtfitruumsCancellation.AcceptChanges();
                                        }
                                    }
                                }
                            }

                            #endregion
                        }

                        catch (Exception ex)
                        {
                            _ = _exception.ErrorNew(ex, _LogAPI.Recheck, SearchKey);

                            ISBooking = "NotAvailable";
                        }

                        // return dsHotel;
                    }
                    dsHotel.Tables[2].Merge(dtfitruumsCancellation);

                    cancellationDto.ISBooking = ISBooking;
                    cancellationDto.dsHotelData = dsHotel;
                    return cancellationDto;
                }

            }
            return cancellationDto;
        }
        public async  Task<string> GetFitruums_EInfo(string RoomTypeId_Own, DataTable dtSuplierMarkup, DataTable dtSearchConfig, HotelSearchParameter objSearPera, string provider, string SearchKey, string Currency,string XmlNationality = "", string MealId = "", string Companybuy = "")
        {
            string EssentialInfo = "";
            double rateprice = 00;
            string XMLResponse = "";
            {
                {
                    {
                        rateprice = Convert.ToDouble(Companybuy);

                        try
                        {
                            DataRow[] fitruumsDetails = null;
                            DataRow[] fitruumsSearchConfig = null;
                            if (dtSuplierMarkup != null & dtSuplierMarkup != null && dtSuplierMarkup.Rows.Count > 0)
                            {

                                fitruumsDetails = dtSuplierMarkup.Select("SupplierName='" + provider + "'");
                            }
                            if (dtSearchConfig != null & dtSearchConfig != null && dtSearchConfig.Rows.Count > 0)
                            {
                                if (objSearPera != null)
                                {
                                    if (fitruumsDetails.GetLength(0) > 0)
                                        fitruumsSearchConfig = dtSearchConfig.Select("SupplierID=" + Convert.ToString(fitruumsDetails[0]["SupplierID"]) + " and (CityID=" + objSearPera.CityID + " or CityID=0) and (Stars=0 OR (Stars>=" + objSearPera.StarStart + " and Stars<=" + objSearPera.StarEnd + "))");//10 for DOTW
                                }
                            }
                            string reqString = GetFitruumsCanelationpolicyRequestRequest(RoomTypeId_Own.Split('$')[0], objSearPera, fitruumsDetails, MealId, rateprice, XmlNationality, Currency);
                            //public string GetFitruumsCanelationpolicyRequestRequest(string Roomtypecode, HotelSearchParameter searchParam, DataRow[] details, string MealId, double CompanyBuyingPrice, string XmlNationality,string Currency)
                            string Error = "";

                            ResponseDto responseDto = await GetSupplierResponseFitruums( "", reqString, Convert.ToString(fitruumsDetails[0]["WebSiteURL"]), Convert.ToInt32(fitruumsSearchConfig[0]["HotelSearchTimeOut"]), provider, objSearPera, false, SearchKey);
                            //public async Task<ResponseDto> GetSupplierResponseFitruums(string strUniqueId, string strxmlRQRS, string strHost, Int32 TimeOut, string Provider = "", HotelSearchParameter para = null, bool DebugMode = false, string SearchKey = "")
                            XMLResponse = responseDto.Response;
                            _commonMethod.ThirdPartyGetPolicyHistory(Convert.ToString(RoomTypeId_Own), Convert.ToString(MealId), reqString, XMLResponse, "After Get Policy", provider, "Response", "-", "");


                            #region "Cancelation Policy"

                            DataSet dsDetails = _commonMethod.GetDataSetFromXMLString(XMLResponse);




                            if (dsDetails.Tables.Contains("preBookResult") == true)
                            {




                                string Note = "";
                                if (dsDetails.Tables.Contains("Note") == true)
                                {
                                    for (int CCC1id = 0; CCC1id < dsDetails.Tables["Note"].Rows.Count; CCC1id++)
                                    {
                                        if (dsDetails.Tables["Note"].Columns.Contains("text") == true)
                                        {
                                            if (CCC1id == 0)
                                                Note = Convert.ToString(dsDetails.Tables["Note"].Rows[CCC1id]["text"]);
                                            else
                                                Note = Note + Convert.ToString(dsDetails.Tables["Note"].Rows[CCC1id]["text"]);
                                        }
                                        if (dsDetails.Tables["Note"].Columns.Contains("start_date") == true)
                                        {
                                            Note = Note + "  Dates:- " + Convert.ToString(dsDetails.Tables["Note"].Rows[CCC1id]["start_date"]);
                                        }
                                        if (dsDetails.Tables["Note"].Columns.Contains("end_date") == true)
                                        {
                                            Note = Note + " Between " + Convert.ToString(dsDetails.Tables["Note"].Rows[CCC1id]["end_date"]);
                                            Note = Note + "<br>";
                                        }
                                    }
                                }
                                EssentialInfo = Note;


                            }

                            #endregion
                        }

                        catch (Exception ex)
                        {
                            _=_exception.ErrorNew(ex, _LogAPI.Recheck, SearchKey);
                            return "";
                        }


                    }

                }

            }
            return EssentialInfo;

        }
        public string GetFitruumsCanelationpolicyRequestRequest(string Roomtypecode, HotelSearchParameter searchParam, DataRow[] details, string MealId, double CompanyBuyingPrice, string XmlNationality,string Currency)
        {
            try
            {

                string checkIn = String.Format("{0:yyyy-MM-dd}", DateTime.ParseExact(searchParam.CheckInDate, "dd/MM/yyyy", null));
                string checkOut = String.Format("{0:yyyy-MM-dd}", DateTime.ParseExact(searchParam.CheckOutDate, "dd/MM/yyyy", null));

                #region Adults Child Count
                int StarStart = searchParam.StarStart;
                int StarEnd = searchParam.StarEnd;
                int NoOfRooms = searchParam.Rooms.Count();
                int NoofAdults = 0;
                int NoOfChild = 0;
                int NoOfInfant = 0;
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
                            switch (searchParam.Rooms[ser].Child1Age < 2)
                            {
                                case true:
                                    NoOfInfant += 1;
                                    break;
                                case false:
                                    NoOfChild += 1;
                                    if (ChildAge == "")
                                    {
                                        ChildAge += searchParam.Rooms[ser].Child1Age;
                                    }
                                    else
                                    {
                                        ChildAge += "," + searchParam.Rooms[ser].Child1Age;
                                    }

                                    break;
                                default:
                                    break;
                            }
                            break;
                        case 2:
                            switch (searchParam.Rooms[ser].Child1Age < 2)
                            {
                                case true:
                                    NoOfInfant += 1;
                                    break;
                                case false:
                                    NoOfChild += 1;
                                    if (ChildAge == "")
                                    {
                                        ChildAge += searchParam.Rooms[ser].Child1Age;
                                    }
                                    else
                                    {
                                        ChildAge += "," + searchParam.Rooms[ser].Child1Age;
                                    }
                                    break;
                                default:
                                    break;
                            }
                            switch (searchParam.Rooms[ser].Child2Age < 2)
                            {
                                case true:
                                    NoOfInfant += 1;
                                    break;
                                case false:
                                    NoOfChild += 1;
                                    if (ChildAge == "")
                                    {
                                        ChildAge += searchParam.Rooms[ser].Child2Age;
                                    }
                                    else
                                    {
                                        ChildAge += "," + searchParam.Rooms[ser].Child2Age;
                                    }
                                    break;
                                default:
                                    break;
                            }
                            break;
                        default:
                            break;
                    }
                }


                #endregion
                StringBuilder fitruumsPreBooking = new StringBuilder();
                fitruumsPreBooking.Append(FitruumsInfo.FitruumsPreBooking);
                fitruumsPreBooking.Append("userName=" + details[0]["UserName"] + "");
                fitruumsPreBooking.Append("&password=" + details[0]["Password"] + "");
                //fitruumsPreBooking.Append("&currency=" + XMLHotelBookingCommon.FitruumsCurrency + "");
                fitruumsPreBooking.Append("&currency=" + Currency + "");//added on 30-05-25
                fitruumsPreBooking.Append("&language=en");
                fitruumsPreBooking.Append("&checkInDate=" + checkIn + "");
                fitruumsPreBooking.Append("&checkOutDate=" + checkOut + "");
                fitruumsPreBooking.Append("&roomId=" + Roomtypecode + "");
                fitruumsPreBooking.Append("&rooms=" + NoOfRooms + "");
                fitruumsPreBooking.Append("&adults=" + NoofAdults + "");
                fitruumsPreBooking.Append("&children=" + NoOfChild + "");
                fitruumsPreBooking.Append("&childrenAges=" + ChildAge + "");
                fitruumsPreBooking.Append("&infant=" + NoOfInfant + "");
                fitruumsPreBooking.Append("&mealId=" + MealId + "");
                fitruumsPreBooking.Append("&CustomerCountry=" + XmlNationality + "");
                fitruumsPreBooking.Append("&b2c=0");
                fitruumsPreBooking.Append("&searchPrice=" + CompanyBuyingPrice + "");

                string fitruumsReq = fitruumsPreBooking.ToString();
                return fitruumsReq;

            }
            catch (Exception ex)
            {
                _exception.Error(ex);
                return "";
            }
        }

        public async Task<ResponseDto> GetSupplierResponseFitruums(string strUniqueId, string strxmlRQRS, string strHost, Int32 TimeOut, string Provider = "", HotelSearchParameter para = null, bool DebugMode = false, string SearchKey = "")
        {
            ResponseDto responseDto = new ResponseDto();
            string Error = "";

            System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
            timer.Start();

            string strOutput = "";
            try
            {
                HttpWebRequest Request;
                Request = (HttpWebRequest)WebRequest.Create(strxmlRQRS);
                Request.ContentType = "application/x-www-form-urlencoded";
                Request.Accept = "application/json";

                try
                {
                    HttpWebResponse Response = (HttpWebResponse)await Request.GetResponseAsync();
                    Stream responseStream = Response.GetResponseStream();
                    if (Response.ContentEncoding.ToLower().Contains("gzip"))
                        responseStream = new System.IO.Compression.GZipStream(responseStream, System.IO.Compression.CompressionMode.Decompress);
                    else if (Response.ContentEncoding.ToLower().Contains("deflate"))
                        responseStream = new System.IO.Compression.DeflateStream(responseStream, System.IO.Compression.CompressionMode.Decompress);

                    StreamReader Reader = new StreamReader(responseStream, Encoding.UTF8);
                    strOutput = await Reader.ReadToEndAsync();
                    responseStream.Close();
                    Reader.Close();
                    Response.Close();
                    timer.Stop();
                    if (para != null)
                    {
                        para.SupplierTime += ",SupplierName :" + Provider + " : " + "Execution Time :" + timer.Elapsed.ToString();
                        if (DebugMode)
                           _= _commonMethod.XMLRequestResponseTrack(strxmlRQRS, strOutput, Provider, "Proper");
                    }
                    if (_keySettings.IsLogTrack == "1")
                    {
                        TimeSpan timeTaken = timer.Elapsed;

                        //_exception.ErrorLog(Convert.ToString(_keySettings.LogTrackPath), Provider + "->" + strHost + "-> Execution Time : " + timer.Elapsed.ToString());
                        _ = _commonMethod.XMLRequestResponseTrack(strxmlRQRS, strOutput, Provider, "Proper");
                    }
                }
                catch (WebException webExcp)
                {
                    try
                    {
                        timer.Stop();
                        if (para != null)
                        {
                            para.SupplierTime += ",SupplierName :" + Provider + " : " + "Execution Time :" + timer.Elapsed.ToString();
                            if (DebugMode)
                                _ = _commonMethod.XMLRequestResponseTrack(strxmlRQRS, strOutput, Provider, SearchKey);
                        }
                        HttpWebResponse httpResponse = (HttpWebResponse)webExcp.Response;
                        if (httpResponse.StatusDescription != null)
                        {

                            string result1 = new StreamReader(webExcp.Response.GetResponseStream()).ReadToEnd();


                            _ =  _commonMethod.XMLRequestResponseTrackError(strxmlRQRS, result1, Provider, SearchKey);

                            Error = result1;

                            responseDto.Response = "";
                            responseDto.Error = Error;

                            return responseDto;
                        }
                    }
                    catch (Exception ex)
                    {
                        Error = ex.Message.ToString();
                        _ = _exception.Error(ex, SearchKey);

                        responseDto.Response = "";
                        responseDto.Error = Error;
                        return responseDto;
                    }
                }
            }
            catch (Exception ex)
            {
                Error = ex.Message.ToString();
                _ = _exception.Error(ex, SearchKey);

                responseDto.Response = "";
                responseDto.Error = Error;
                return responseDto;
            }

            responseDto.Response = strOutput;
            responseDto.Error = Error;
            return responseDto;
        }

        public  string GetfitruumsPreBookingRequest(DataTable UsercartData, string Url, string UserName, string Password,string FitruumsCurrency)
        {
            try
            {

                string CheckIn = Convert.ToString(UsercartData.Rows[0]["CheckInDate"]);
                CheckIn = CheckIn.Substring(0, 4) + "-" + CheckIn.Substring(4, 2) + "-" + CheckIn.Substring(6, 2);
                string CheckOut = Convert.ToString(UsercartData.Rows[0]["CheckOutDate"]);
                CheckOut = CheckOut.Substring(0, 4) + "-" + CheckOut.Substring(4, 2) + "-" + CheckOut.Substring(6, 2);
                #region Adults Child Count

                int NoOfRooms = UsercartData.Rows.Count;
                int NoofAdults = 0;
                int NoOfChild = 0;
                int NoOfInfant = 0;
                string ChildAge = "";
                string TLNationality = Convert.ToString(UsercartData.Rows[0]["XmlNationality"]);
                string RoomTypeId = Convert.ToString(UsercartData.Rows[0]["RoomTypeId"]).Split('$')[0];
                string MealId = Convert.ToString(UsercartData.Rows[0]["MealPlanCode"]);
                double CompanyBuyingPrice = Math.Round(Convert.ToDouble(UsercartData.Rows[0]["CompanyBuyingPrice"]));
                for (int ser = 0; ser < UsercartData.Rows.Count; ser++)
                {
                    if (ser == 0)
                        NoofAdults = Convert.ToInt32(UsercartData.Rows[ser]["NoOfAdults"]);
                    else
                        NoofAdults = NoofAdults + Convert.ToInt32(UsercartData.Rows[ser]["NoOfAdults"]);

                    switch (Convert.ToInt32(UsercartData.Rows[ser]["NoOfChild"]))
                    {
                        case 1:
                            switch (Convert.ToInt32(UsercartData.Rows[ser]["ChildAge1"]) < 2)
                            {
                                case true:
                                    NoOfInfant += 1;
                                    break;
                                case false:
                                    NoOfChild += 1;
                                    if (ChildAge == "")
                                    {
                                        ChildAge += Convert.ToInt32(UsercartData.Rows[ser]["ChildAge1"]);
                                    }
                                    else
                                    {
                                        ChildAge += "," + Convert.ToInt32(UsercartData.Rows[ser]["ChildAge1"]);
                                    }

                                    break;
                                default:
                                    break;
                            }
                            break;
                        case 2:
                            switch (Convert.ToInt32(UsercartData.Rows[ser]["ChildAge1"]) < 2)
                            {
                                case true:
                                    NoOfInfant += 1;
                                    break;
                                case false:
                                    NoOfChild += 1;
                                    if (ChildAge == "")
                                    {
                                        ChildAge += Convert.ToInt32(UsercartData.Rows[ser]["ChildAge1"]);
                                    }
                                    else
                                    {
                                        ChildAge += "," + Convert.ToInt32(UsercartData.Rows[ser]["ChildAge1"]);
                                    }
                                    break;
                                default:
                                    break;
                            }
                            switch (Convert.ToInt32(UsercartData.Rows[ser]["ChildAge2"]) < 2)
                            {
                                case true:
                                    NoOfInfant += 1;
                                    break;
                                case false:
                                    NoOfChild += 1;
                                    if (ChildAge == "")
                                    {
                                        ChildAge += Convert.ToInt32(UsercartData.Rows[ser]["ChildAge2"]);
                                    }
                                    else
                                    {
                                        ChildAge += "," + Convert.ToInt32(UsercartData.Rows[ser]["ChildAge2"]);
                                    }
                                    break;
                                default:
                                    break;
                            }
                            break;
                        default:
                            break;
                    }
                }


                #endregion
                StringBuilder fitruumsPreBooking = new StringBuilder();

                fitruumsPreBooking.Append(FitruumsInfo.FitruumsPreBooking);
                fitruumsPreBooking.Append("userName=" + UserName + "");
                fitruumsPreBooking.Append("&password=" + Password + "");
                fitruumsPreBooking.Append("&currency=" + FitruumsCurrency + "");
                fitruumsPreBooking.Append("&language=en");
                fitruumsPreBooking.Append("&checkInDate=" + CheckIn + "");
                fitruumsPreBooking.Append("&checkOutDate=" + CheckOut + "");
                fitruumsPreBooking.Append("&roomId=" + RoomTypeId + "");
                fitruumsPreBooking.Append("&rooms=" + NoOfRooms + "");
                fitruumsPreBooking.Append("&adults=" + NoofAdults + "");
                fitruumsPreBooking.Append("&children=" + NoOfChild + "");
                fitruumsPreBooking.Append("&childrenAges=" + ChildAge + "");
                fitruumsPreBooking.Append("&infant=" + NoOfInfant + "");
                fitruumsPreBooking.Append("&mealId=" + MealId + "");
                fitruumsPreBooking.Append("&CustomerCountry=" + TLNationality + "");
                fitruumsPreBooking.Append("&b2c=0");
                fitruumsPreBooking.Append("&searchPrice=" + CompanyBuyingPrice + "");

                string fitruumsfitruumsPreBookingReq = fitruumsPreBooking.ToString();
                return fitruumsfitruumsPreBookingReq;

            }
            catch (Exception ex)
            {
                 _ = _exception.Error(ex);
                return "";
            }
        }


        public string Get_fitruums_BookingRequest(DataSet dsCartDetails, DataTable dtPassengers, DataRow[] JacTravelDetails, string UniqueNo, bool IsPriceUpdate, int i, int j, string PreBookingToken, string UserName, string Password, string AgentCode)
        {
            try
            {
                string CheckIn = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["CheckInDate"]);
                CheckIn = CheckIn.Substring(0, 4) + "-" + CheckIn.Substring(4, 2) + "-" + CheckIn.Substring(6, 2);
                string CheckOut = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["CheckOutDate"]);
                CheckOut = CheckOut.Substring(0, 4) + "-" + CheckOut.Substring(4, 2) + "-" + CheckOut.Substring(6, 2);

                DataTable UsercartData = dsCartDetails.Tables[1].DefaultView.ToTable();

                #region Adults Child Count

                int NoOfRooms = UsercartData.Rows.Count;
                int NoofAdults = 0;
                int NoOfChild = 0;
                int NoOfInfant = 0;
                string ChildAge = "";
                string TLNationality = Convert.ToString(UsercartData.Rows[0]["XmlNationality"]);
                string RoomTypeId = Convert.ToString(UsercartData.Rows[0]["RoomTypeId"]).Split('$')[0];
                string MealId = Convert.ToString(UsercartData.Rows[0]["MealPlanCode"]);
                string ExternalReference = "Book" + Convert.ToString(AgentCode + UniqueNo);
                double CompanyBuyingPrice = Math.Round(Convert.ToDouble(UsercartData.Rows[0]["CompanyBuyingPrice"]));
                string preBookCode = "";
                string[] Data = Convert.ToString(dsCartDetails.Tables[1].DefaultView[0]["purchaseToken"]).Split('$');
                if (Data.Length > 0)
                {
                    preBookCode = Convert.ToString(Data[0]);
                }

                StringBuilder AdultDetails = new StringBuilder();
                StringBuilder ChildDetails = new StringBuilder();
                int ChildCount = 0;
                int AdultCount = 0;

                for (int ser = 0; ser < UsercartData.Rows.Count; ser++)
                {
                    if (ser == 0)
                        NoofAdults = Convert.ToInt32(UsercartData.Rows[ser]["NoOfAdults"]);
                    else
                        NoofAdults = NoofAdults + Convert.ToInt32(UsercartData.Rows[ser]["NoOfAdults"]);

                    switch (Convert.ToInt32(UsercartData.Rows[ser]["NoOfChild"]))
                    {
                        case 1:
                            switch (Convert.ToInt32(UsercartData.Rows[ser]["ChildAge1"]) < 2)
                            {
                                case true:
                                    NoOfInfant += 1;
                                    break;
                                case false:
                                    NoOfChild += 1;
                                    if (ChildAge == "")
                                    {
                                        ChildAge += Convert.ToInt32(UsercartData.Rows[ser]["ChildAge1"]);
                                    }
                                    else
                                    {
                                        ChildAge += "," + Convert.ToInt32(UsercartData.Rows[ser]["ChildAge1"]);
                                    }

                                    break;
                                default:
                                    break;
                            }
                            break;
                        case 2:
                            switch (Convert.ToInt32(UsercartData.Rows[ser]["ChildAge1"]) < 2)
                            {
                                case true:
                                    NoOfInfant += 1;
                                    break;
                                case false:
                                    NoOfChild += 1;
                                    if (ChildAge == "")
                                    {
                                        ChildAge += Convert.ToInt32(UsercartData.Rows[ser]["ChildAge1"]);
                                    }
                                    else
                                    {
                                        ChildAge += "," + Convert.ToInt32(UsercartData.Rows[ser]["ChildAge1"]);
                                    }
                                    break;
                                default:
                                    break;
                            }
                            switch (Convert.ToInt32(UsercartData.Rows[ser]["ChildAge2"]) < 2)
                            {
                                case true:
                                    NoOfInfant += 1;
                                    break;
                                case false:
                                    NoOfChild += 1;
                                    if (ChildAge == "")
                                    {
                                        ChildAge += Convert.ToInt32(UsercartData.Rows[ser]["ChildAge2"]);
                                    }
                                    else
                                    {
                                        ChildAge += "," + Convert.ToInt32(UsercartData.Rows[ser]["ChildAge2"]);
                                    }
                                    break;
                                default:
                                    break;
                            }
                            break;
                        default:
                            break;
                    }

                    #region  IsLead Filter Logic
                    string isLead = string.Empty;
                    dtPassengers.DefaultView.RowFilter = "PickUp='" + Convert.ToString(UsercartData.Rows[ser]["RoomTypeID"]) + "' and RoomNo='" + Convert.ToString(UsercartData.Rows[ser]["RoomNumber"]) + "' and IsLead=1";
                    if (dtPassengers.DefaultView.Count == 0)
                        isLead = "true";
                    dtPassengers.DefaultView.RowFilter = "";
                    dtPassengers.DefaultView.RowFilter = "PickUp='" + Convert.ToString(UsercartData.Rows[ser]["RoomTypeID"]) + "' and RoomNo='" + (UsercartData.Rows[ser]["RoomNumber"]) + "' and UserCartId='" + dsCartDetails.Tables[0].Rows[i]["UserCartId"] + "'";
                    dtPassengers.DefaultView.Sort = "IsLead desc";
                    #endregion


                    if (dtPassengers.DefaultView.Count > 0)
                    {
                        for (int p = 0; p < dtPassengers.DefaultView.Count; p++)
                        {
                            string FName = Convert.ToString(dtPassengers.DefaultView[p]["FirstName"]);
                            string LName = Convert.ToString(dtPassengers.DefaultView[p]["LastName"]);
                            int ChildAgex = Convert.ToInt32(dtPassengers.DefaultView[p]["ChildAge"]);
                            string Prefix = Convert.ToString(dtPassengers.DefaultView[p]["Prefix"]).Replace(".", "");
                            string PaxType = Convert.ToString(dtPassengers.DefaultView[p]["PaxType"]);

                            if (PaxType.ToLower() == "adult")
                            {
                                AdultCount = AdultCount + 1;
                                AdultDetails.Append("&adultGuest" + AdultCount + "FirstName=" + FName + "");
                                AdultDetails.Append("&adultGuest" + AdultCount + "LastName=" + LName + "");
                            }
                            if (PaxType.ToLower() == "child")
                            {
                                if (ChildAgex > 1)
                                {
                                    ChildCount = ChildCount + 1;
                                    ChildDetails.Append("&childrenGuest" + ChildCount + "FirstName=" + FName + "");
                                    ChildDetails.Append("&childrenGuest" + ChildCount + "LastName=" + LName + "");
                                    ChildDetails.Append("&childrenGuestAge" + ChildCount + "=" + ChildAgex + "");
                                }
                            }
                        }
                    }
                }
                int Remainedadult = 0; int RemainedChild = 0;
                Remainedadult = 9 - AdultCount;
                RemainedChild = 9 - ChildCount;
                for (int adtid = 1; adtid <= Remainedadult; adtid++)
                {
                    AdultCount = AdultCount + 1;
                    AdultDetails.Append("&adultGuest" + AdultCount + "FirstName=");
                    AdultDetails.Append("&adultGuest" + AdultCount + "LastName=");
                }
                for (int Chdid = 1; Chdid <= RemainedChild; Chdid++)
                {
                    ChildCount = ChildCount + 1;
                    ChildDetails.Append("&childrenGuest" + ChildCount + "FirstName=");
                    ChildDetails.Append("&childrenGuest" + ChildCount + "LastName=");
                    ChildDetails.Append("&childrenGuestAge" + ChildCount + "=");
                }

                #endregion

                string FitruumsCurrency = Convert.ToString(JacTravelDetails[0]["CurrencyShortName"]);

                StringBuilder fitruumshotellist = new StringBuilder();

                fitruumshotellist.Append(FitruumsInfo.FitruumsBooking);
                fitruumshotellist.Append("userName=" + UserName + "");
                fitruumshotellist.Append("&password=" + Password + "");
                fitruumshotellist.Append("&currency=" + FitruumsCurrency + "");
                fitruumshotellist.Append("&language=en");
                fitruumshotellist.Append("&email=noreply@fitruums.com");
                fitruumshotellist.Append("&checkInDate=" + CheckIn + "");
                fitruumshotellist.Append("&checkOutDate=" + CheckOut + "");
                fitruumshotellist.Append("&roomId=" + RoomTypeId + "");
                fitruumshotellist.Append("&rooms=" + NoOfRooms + "");
                fitruumshotellist.Append("&adults=" + NoofAdults + "");
                fitruumshotellist.Append("&children=" + NoOfChild + "");
                fitruumshotellist.Append("&childrenAges=" + ChildAge + "");
                fitruumshotellist.Append("&infant=" + NoOfInfant + "");
                fitruumshotellist.Append("&yourRef=" + ExternalReference + "");
                fitruumshotellist.Append("&specialrequest=");
                fitruumshotellist.Append("&mealId=" + MealId + "");
                fitruumshotellist.Append(AdultDetails);
                fitruumshotellist.Append(ChildDetails);
                fitruumshotellist.Append("&paymentMethodId=1");
                fitruumshotellist.Append("&creditCardType=");
                fitruumshotellist.Append("&creditCardNumber=");
                fitruumshotellist.Append("&creditCardHolder=");
                fitruumshotellist.Append("&creditCardCVV2=");
                fitruumshotellist.Append("&creditCardExpYear=");
                fitruumshotellist.Append("&creditCardExpMonth=");
                fitruumshotellist.Append("&customerEmail=");
                fitruumshotellist.Append("&invoiceRef=");
                fitruumshotellist.Append("&commissionAmountInHotelCurrency=");
                fitruumshotellist.Append("&CustomerCountry=" + TLNationality + "");
                fitruumshotellist.Append("&b2c=0");
                fitruumshotellist.Append("&preBookCode=" + preBookCode + "");

                string fitruumsReq = fitruumshotellist.ToString();
                return fitruumsReq;

            }
            catch (Exception ex)
            {
                _ = _exception.Error(ex);
                return "";
            }
        }

        public async Task<FitruumsBookRes> Get_fitruums_BookingDetailsProcess(int bookingStatus,string AgentVoucherNo, string UserName, string Password, string UniqueNo, string AgentUniqueNo, string Url, DataSet dsCartDetails, int j, string AgentCode)
        {
            FitruumsBookRes fitruumsBookRes = new FitruumsBookRes();
            try
            {
                string ExternalReference = "Book" + Convert.ToString(AgentCode + UniqueNo);
                if (ExternalReference != "")
                {
                    string BookingDetailsRequest = Get_fitruums_BookingDetails(ExternalReference, UserName, Password);
                    _commonMethod.ThirdPartyBookingHistory(Convert.ToString(UniqueNo), Convert.ToString(AgentUniqueNo), BookingDetailsRequest, "-", "Booking done and get Booking Details", Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["SupplierName"]), "Request Confirm", "-", "Within Earth");

                    string Error = "";
                    ResponseDto responseDto = await GetSupplierResponseFitruums("", BookingDetailsRequest, Url, 180000, "fitruums");

                    string BookingDetailsResponse = responseDto.Response;
                    Error = responseDto.Error;

                    _commonMethod.ThirdPartyBookingHistory(Convert.ToString(UniqueNo), Convert.ToString(AgentUniqueNo), BookingDetailsRequest, BookingDetailsResponse, "Booking done and get Booking Details", Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["SupplierName"]), "Response Confirm", "-", "Within Earth");

                    BookingDetailsResponse = BookingDetailsResponse.Replace(" xsi:nil=\"true\" ", "");
                    if (BookingDetailsResponse != "" || BookingDetailsResponse != string.Empty)
                    {
                        DataSet fitruumsBookingDetails = _commonMethod.GetDataSetFromXMLString(BookingDetailsResponse);

                        if (fitruumsBookingDetails.Tables["booking"] != null)
                        {
                            if (fitruumsBookingDetails.Tables["booking"].Columns.Contains("bookingStatus"))
                            {
                                if (Convert.ToString(fitruumsBookingDetails.Tables["booking"].Rows[0]["bookingStatus"]) != string.Empty)
                                {
                                    string status = Convert.ToString(fitruumsBookingDetails.Tables["booking"].Rows[0]["bookingStatus"]);
                                    if (status != string.Empty)
                                    {
                                        switch (status.ToLower())
                                        {
                                            case "active": bookingStatus = 6; break;
                                            case "notactive": bookingStatus = 2; break;
                                        }
                                    }
                                }
                            }
                            if (fitruumsBookingDetails.Tables["booking"].Columns.Contains("bookingnumber"))
                            {
                                if (Convert.ToString(fitruumsBookingDetails.Tables["booking"].Rows[0]["bookingnumber"]) != string.Empty)
                                {
                                    AgentVoucherNo = Convert.ToString(fitruumsBookingDetails.Tables["booking"].Rows[0]["bookingnumber"]);
                                }
                            }
                        }
                        else
                        {
                            AgentVoucherNo = "";
                            bookingStatus = 2;
                        }
                    }
                    else
                    {
                        AgentVoucherNo = "";
                        bookingStatus = 2;
                    }
                }
            }
            catch (Exception ex)
            {
                AgentVoucherNo = "";
                bookingStatus = 1;
                _ = _exception.ErrorNew(ex, _LogAPI.Book);
            }

            fitruumsBookRes.bookingStatus = bookingStatus;
            fitruumsBookRes.AgentVoucherNo = AgentVoucherNo;
            return fitruumsBookRes;
        }

        public string Get_fitruums_BookingDetails(string bookingID, string UserName, string Password)
        {
            try
            {
                StringBuilder fitruumsBookingDetails = new StringBuilder();
                fitruumsBookingDetails.Append(FitruumsInfo.FitruumsGetBookingDetails);
                fitruumsBookingDetails.Append("userName=" + UserName + "");
                fitruumsBookingDetails.Append("&password=" + Password + "");
                fitruumsBookingDetails.Append("&language=en");
                fitruumsBookingDetails.Append("&bookingID=");
                fitruumsBookingDetails.Append("&reference=" + bookingID + "");
                fitruumsBookingDetails.Append("&createdDateFrom=");
                fitruumsBookingDetails.Append("&createdDateTo=");
                fitruumsBookingDetails.Append("&arrivalDateFrom=");
                fitruumsBookingDetails.Append("&arrivalDateTo=");
                string fitruumsReq = fitruumsBookingDetails.ToString();
                return fitruumsReq;
            }
            catch (Exception ex)
            {
                _ = _exception.Error(ex);
                return "";
            }
        }

        public string Get_fitruums_BookingCancelRQ(string AgentVoucherNo, DataRow[] SupplierDetails)
        {
            string CancellationRQ = "";
            try
            {
                #region Booking Cancel RQ
                string UserName = Convert.ToString(SupplierDetails[0]["UserName"]);
                string Password = Convert.ToString(SupplierDetails[0]["Password"]);

                StringBuilder CancelRQ = new StringBuilder();
                CancelRQ.Append(FitruumsInfo.FitruumsCancelBooking);
                CancelRQ.Append("userName=" + UserName + "");
                CancelRQ.Append("&password=" + Password + "");
                CancelRQ.Append("&bookingID=" + AgentVoucherNo + "");
                CancelRQ.Append("&language=en");
                CancellationRQ = CancelRQ.ToString();
                return CancellationRQ;

                #endregion
            }
            catch (Exception ex)
            {
                _ =_exception.ErrorNew(ex, _LogAPI.CancelBooking);
                return "";
            }
            return CancellationRQ;
        }
    }

}
