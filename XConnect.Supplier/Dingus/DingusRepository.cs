using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XConnect.CommonUtilities.Interface;
using XConnect.DAL.Interface;
using XConnect.ErrorLogHandler.Interface;
using XConnect.Markup.Interface;
using XConnect.Models;
using XConnect.Models.Hotel;
using XConnect.Supplier.Interface;
using System.Collections.Concurrent;
using System.Data;
using static Azure.Core.HttpHeader;
using XConnect.CommonUtilities;
using static XConnect.Models.Class_Pruvo;
using static XConnect.Supplier.Booking.Com.BookingDotComRoomEss;
using XConnect.Supplier.Dingus;
using XConnect.Supplier.Dingus.Interface;
using Microsoft.Extensions.Options;
using Options = XConnect.Models.Options;
using System.Net;
using System.Xml.Serialization;
using static XConnect.Supplier.Dingus.DingusClass;
using Xconnect.Hotelbeds.Interface;
using XConnect.Supplier.Booking.Com;
using Azure;




namespace XConnect.Supplier.Dingus
{
    public class DingusRepository : IDingus
    {
        private readonly ICommonMethod _commonMethod;
        private readonly IException _exception;
        private readonly IMarkup _markup;
        private readonly ISupplierRes _supplierRes;
        private readonly KeySettings _keySettings;
        private readonly IDLConnection _dLConnection;
        private readonly SupplierConfiguration _supplierConfig;//Added on 26-FEB-26
        public DingusRepository(ICommonMethod commonMethod, IMarkup markup, ISupplierRes supplierRes, IOptions<KeySettings> keySettings, IException exception)
        {
            _commonMethod = commonMethod;

            _markup = markup;
            _supplierRes = supplierRes;
            _keySettings = keySettings.Value;
            _exception = exception;
        }

        public void DingusRequestOnly_async(DataRow[] commonCode, List<StarRating> listRating, DataTable dtROE, HotelSearchParameter SearchPara, List<SupplierDetails> DingusDetails, List<SupplierSearchConfig> DingusSearchConfig, DataTable dtPreferredDataHotel, ref ConcurrentBag<HotelX> AllTask, string XMLCity, string XMLCountry, string XMLNationality, int Hit, string supplierName, TokenData tokenData)
        {
            try
            {
                string Ids = string.Empty;
                string xmlbase = "";
                _commonMethod.GetPrefferedHotelCodes(ref Ids, ref xmlbase, ref DingusSearchConfig, supplierName, dtPreferredDataHotel, SearchPara.IsSearchFor);

                foreach (SupplierDetails DingusChild in DingusDetails)
                {
                    try
                    {
                        int SupplierID = DingusChild.SupplierID;
                        string Supplier = DingusChild.SupplierName;
                        string UserName = Convert.ToString(DingusChild.UserName);
                        string Password = Convert.ToString(DingusChild.Password);

                        if (DingusSearchConfig != null)
                        {
                            string language1 = "en";

                            double CurrencyFactor = 1;
                            string currency = Convert.ToString(DingusChild.CurrencyShortName);

                            if (currency.ToLower() != tokenData.SiteCurrency.ToLower())
                            {
                                CurrencyFactor = _commonMethod.GetROEByCurrency(currency, dtROE);
                            }
                            for (int k = 0; k < DingusSearchConfig.Count; k++)
                            {
                                bool DebugMode = (Convert.ToInt32(DingusSearchConfig[k].DebugMode) == 0 ? false : true);

                                {
                                    {
                                        int IsCitywisesearch = Convert.ToInt32(DingusSearchConfig[k].City);
                                        {
                                            if (Convert.ToInt32(DingusSearchConfig[k].CityID) == 0 || Convert.ToInt32(DingusSearchConfig[k].CityID) == SearchPara.CityID)
                                            {
                                                if (commonCode != null && commonCode.Length > 0)
                                                {
                                                    int Noofrecords = Convert.ToInt32(DingusSearchConfig[k].Noofrecords);
                                                    DataTable[] splittedtables = commonCode.AsEnumerable()
                                                                .Select((row, index) => new { row, index })
                                                                .GroupBy(x => x.index / Noofrecords)  // integer division, the fractional part is truncated
                                                                .Select(g => g.Select(x => x.row).CopyToDataTable())
                                                                .ToArray();

                                                    for (int a = 0; a < splittedtables.Length; a++)
                                                    {
                                                        var paramValues1 = String.Join(",", splittedtables[a].Select().Select(x => String.Join(",", Convert.ToString(x.ItemArray[0]))));
                                                        string IdsForSearch = Convert.ToString(paramValues1);
                                                        HotelX Xhotel = new HotelX();
                                                        //Xhotel.Supplier = supplierName;
                                                        Xhotel.Supplier = Supplier;
                                                        Xhotel.DebugeMode = DebugMode;
                                                        Xhotel.SearchTimeOut = Convert.ToInt32(DingusSearchConfig[k].HotelSearchTimeOut);
                                                        if (Hit > 0)
                                                            Xhotel.SearchTimeOut = Convert.ToInt32(DingusSearchConfig[k].HotelSearchTimeOut);

                                                        Xhotel.URL = Convert.ToString(DingusChild.WebSiteURL);
                                                        Xhotel.UserId = Convert.ToString(DingusChild.UserID);
                                                        Xhotel.UserName = UserName;
                                                        Xhotel.PassWord = Password;
                                                        Xhotel.CurrencyCode = currency;
                                                        Xhotel.CurrencyFactor = CurrencyFactor;
                                                        Xhotel.XmlCity = XMLCity;
                                                        Xhotel.XmlCountry = "";
                                                        Xhotel.XmlNationality = XMLNationality;

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
                                                            objrq.Request = DingusSearchRequest(SearchPara, IdsForSearch, Xhotel.UserName, Xhotel.PassWord, Xhotel.URL, language1, supplierName);
                                                            objrq.Response = "";
                                                            RQRS.Add(objrq);
                                                        }
                                                        Xhotel.RQRS = RQRS;
                                                        #endregion
                                                        Xhotel.ReturnRooms = DingusDetails[0].ReturnRooms;
                                                        Xhotel.TechName = DingusDetails[0].TechName;
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
                    catch (Exception ex)
                    {
                        _exception.Error(ex);
                    }

                }

            }
            catch (Exception ex)
            {

            }
        }
        public void DingusRequestOnly_async_new(DataRow[] commonCode, List<StarRating> listRating, DataTable dtROE, HotelSearchParameter SearchPara, List<SupplierDetails> lstDingusDetails, List<SupplierSearchConfig> lstDingusSearchConfig, DataTable dtPreferredDataHotel, ref ConcurrentBag<HotelX> AllTask, int Hit, TokenData tokenData, DataSet dtXMLCity)
        {
            try
            {
                List<StarRating> lstRating = new List<StarRating>();
                lstRating = _commonMethod.GetStarRatingStringList_xtg(SearchPara);

                List<HotelCodeByCity> DtHotelDAta = HtmlExtensions.ToList<HotelCodeByCity>(commonCode.CopyToDataTable());
                int ratingcount = lstRating.Count;
                int hotelcount = DtHotelDAta.Count;

                foreach (SupplierDetails DingusChild in lstDingusDetails)
                {
                    try
                    {
                        int SupplierID = DingusChild.SupplierID;
                        string Supplier = DingusChild.SupplierName;

                        List<SupplierSearchConfig> DingusSearchConfig = lstDingusSearchConfig
                                          .Where(x => x != null && x.SupplierID == SupplierID)
                                          .ToList();
                        if (DingusSearchConfig != null && DingusSearchConfig.Count > 0)
                        {
                            string language1 = "en";
                            if ((SearchPara.IsSearchFor.ToLower().IndexOf(Supplier.ToLower()) >= 0 || SearchPara.IsSearchFor.ToLower().IndexOf("multi") >= 0))
                            {
                                DataForSearch dataForSearch = _commonMethod.GetXmlDataForSearchTechwise(SearchPara.Nationality, dtXMLCity, Supplier, DingusChild.TechName);

                                if (dataForSearch.XmlNationality != "" && SearchPara.PastDate == false)
                                {
                                    string Ids = string.Empty;
                                    string xmlbase = "";
                                    _commonMethod.GetPrefferedHotelCodes(ref Ids, ref xmlbase, ref DingusSearchConfig, Supplier, dtPreferredDataHotel, SearchPara.IsSearchFor);

                                    List<HotelCodeByCity> filter = new List<HotelCodeByCity>();
                                    filter = _commonMethod.FilterWithOutCity_Code(ratingcount, hotelcount, DtHotelDAta, lstRating, DingusChild.HotelSupplier);


                                    string Dingus_currency = Convert.ToString(DingusChild.CurrencyShortName);
                                    string Sitecurrency = tokenData.SiteCurrency;

                                    double CurrencyFactorDingus = 1;

                                    if (Dingus_currency.ToLower() != Sitecurrency.ToLower()) // change systembase currency 
                                    {
                                        CurrencyFactorDingus = _commonMethod.GetROEByCurrency(Dingus_currency, dtROE);
                                    }

                                    if (DingusSearchConfig != null)
                                    {
                                        for (int k = 0; k < DingusSearchConfig.Count; k++)
                                        {
                                            bool DebugMode = (Convert.ToInt32(DingusSearchConfig[k].DebugMode) == 0 ? false : true);
                                            string UserName = Convert.ToString(DingusChild.UserName);
                                            string Password = Convert.ToString(DingusChild.Password);

                                            int IsCitywisesearch = Convert.ToInt32(DingusSearchConfig[k].City);

                                            if (Convert.ToInt32(DingusSearchConfig[k].CityID) == 0 || Convert.ToInt32(DingusSearchConfig[k].CityID) == SearchPara.CityID)
                                            {
                                                if (filter.Count > 0)
                                                {
                                                    int NoOfRecord = Convert.ToInt32(DingusSearchConfig[k].Noofrecords);
                                                    double count = Convert.ToDouble(filter.Count);
                                                    int NoOfDiv = Convert.ToInt32(Math.Ceiling(count / Convert.ToDouble(NoOfRecord)));
                                                    List<List<XMLID_Search>> lstxml = _commonMethod.GetXMLIDString(NoOfDiv, NoOfRecord, count, filter);

                                                    for (int a = 0; a < NoOfDiv; a++)
                                                    {
                                                        string Uniqueid = Guid.NewGuid().ToString().Substring(0, 8);

                                                        string IdsForSearch = Convert.ToString(_commonMethod.GetHotelCodeByCOmma(lstxml[a]));

                                                        HotelX Xhotel = new HotelX();
                                                        Xhotel.Supplier = Supplier;
                                                        Xhotel.DebugeMode = DebugMode;
                                                        Xhotel.SearchTimeOut = Convert.ToInt32(DingusSearchConfig[k].HotelSearchTimeOut);

                                                        Xhotel.ProcessTimeout = Convert.ToInt32(DingusSearchConfig[k].HotelSearchTimeOut2);
                                                        Xhotel.URL = Convert.ToString(DingusChild.WebSiteURL);
                                                        Xhotel.UserId = Convert.ToString(DingusChild.UserID);
                                                        Xhotel.UserName = UserName;
                                                        Xhotel.PassWord = Password;
                                                        Xhotel.CurrencyCode = Dingus_currency;
                                                        Xhotel.CurrencyFactor = CurrencyFactorDingus;
                                                        Xhotel.XmlCity = dataForSearch.xmlCity;
                                                        Xhotel.XmlCountry = "";
                                                        Xhotel.XmlNationality = dataForSearch.XmlNationality;
                                                        Xhotel.Action = Uniqueid;

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
                                                            objrq.Request = DingusSearchRequest(SearchPara, IdsForSearch, Xhotel.UserName, Xhotel.PassWord, Xhotel.URL, language1, Supplier);
                                                            objrq.Response = "";
                                                            RQRS.Add(objrq);
                                                        }
                                                        Xhotel.RQRS = RQRS;
                                                        Xhotel.ReturnRooms = DingusChild.ReturnRooms; //190225
                                                        Xhotel.TechName = DingusChild.TechName; //190225
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

                    catch (Exception ex)
                    {
                        _exception.Error(ex);
                    }

                }

            }
            catch (Exception ex)
            {

            }
        }

        public string DingusSearchRequest(HotelSearchParameter searchParam, string HotelIds, string Username, string Password, string DingusURL, string language1, string Provider)
        {
            try
            {
                string language = "en";
                //if (searchParam.Language != 0)
                //{
                //    language = "es"; //es=Spanish
                //}
                //else
                //{
                //    language = "en"; //en= English
                //}

                string HotelRequest = "";
                string checkIn = String.Format("{0:yyyy-MM-dd}", DateTime.ParseExact(searchParam.CheckInDate, "dd/MM/yyyy", null));
                string checkOut = String.Format("{0:yyyy-MM-dd}", DateTime.ParseExact(searchParam.CheckOutDate, "dd/MM/yyyy", null));

                StringBuilder HotelAvailRQ = new StringBuilder();
                if (HotelIds != string.Empty)
                {
                    HotelAvailRQ.Append("<SOAP-ENV:Envelope xmlns:SOAP-ENV=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:SOAPENC=\"http://schemas.xmlsoap.org/soap/encoding/\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchemainstance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">");
                    HotelAvailRQ.Append("<SOAP-ENV:Body>");
                    //HotelAvailRQ.Append("<m:OTA_HotelAvailRQ xmlns:m=\"http://www.opentravel.org/OTA/2003/05\" Version=\"1.0\" PrimaryLangID=\"en\" AvailRatesOnly=\"true\" GetOnlyBAR=\"false\" >");
                    HotelAvailRQ.Append("<m:OTA_HotelAvailRQ xmlns:m=\"http://www.opentravel.org/OTA/2003/05\" Version=\"1.0\" PrimaryLangID=\"" + language + "\" AvailRatesOnly=\"true\" GetOnlyBAR=\"false\" >");
                    HotelAvailRQ.Append("<m:POS>");
                    HotelAvailRQ.Append("<m:Source>");
                    HotelAvailRQ.Append("<m:RequestorID MessagePassword=\"" + Password + "\" ID=\"" + Username + "\" />");
                    HotelAvailRQ.Append("</m:Source>");
                    HotelAvailRQ.Append("</m:POS>");
                    HotelAvailRQ.Append("<m:AvailRequestSegments>");
                    HotelAvailRQ.Append("<m:AvailRequestSegment>");
                    HotelAvailRQ.Append("<m:StayDateRange Start=\"" + checkIn + "\" End=\"" + checkOut + "\"/>");
                    HotelAvailRQ.Append("<m:RoomStayCandidates>");

                    for (int i = 0; i < searchParam.Rooms.Count; i++)
                    {
                        // int Quantity = searchParam.Rooms[i].NoofAdults + searchParam.Rooms[i].NoofChild;

                        HotelAvailRQ.Append("<m:RoomStayCandidate RPH=\"" + (i + 1) + "\" Quantity=\"" + 1 + "\" >");
                        HotelAvailRQ.Append("<m:GuestCounts>");
                        //int NoofAdults = searchParam.Rooms[i].NoofAdults;int NoofChild = searchParam.Rooms[i].NoofChild;
                        int NoofAdults = Convert.ToInt32(searchParam.Rooms[i].NoofAdults);
                        int NoofChild = Convert.ToInt32(searchParam.Rooms[i].NoofChild);

                        switch (NoofChild)
                        {
                            case 1:
                                if (searchParam.Rooms[i].Child1Age > 12)
                                    NoofAdults = NoofAdults + 1;
                                break;
                            case 2:
                                if (searchParam.Rooms[i].Child1Age > 12)
                                    NoofAdults = NoofAdults + 1;
                                if (searchParam.Rooms[i].Child2Age > 12)
                                    NoofAdults = NoofAdults + 1;
                                break;
                                //case 3:
                                //    if (searchParam.Rooms[i].Child1Age > 12)
                                //        NoofAdults = NoofAdults + 1;
                                //    if (searchParam.Rooms[i].Child2Age > 12)
                                //        NoofAdults = NoofAdults + 1;
                                //    if (searchParam.Rooms[i].Child3Age > 12)
                                //        NoofAdults = NoofAdults + 1;
                                //    break;
                                //case 4:
                                //    if (searchParam.Rooms[i].Child1Age > 12)
                                //        NoofAdults = NoofAdults + 1;
                                //    if (searchParam.Rooms[i].Child2Age > 12)
                                //        NoofAdults = NoofAdults + 1;
                                //    if (searchParam.Rooms[i].Child3Age > 12)
                                //        NoofAdults = NoofAdults + 1;
                                //    if (searchParam.Rooms[i].Child4Age > 12)
                                //        NoofAdults = NoofAdults + 1;
                                //    break;
                        }


                        if (NoofAdults > 0)
                        {
                            HotelAvailRQ.Append("<m:GuestCount Count=\"" + NoofAdults + "\" AgeQualifyingCode=\"10\" Age=\"30\" />");
                        }
                        if (searchParam.Rooms[i].NoofChild > 0)
                        {
                            if (searchParam.Rooms[i].NoofChild == 1)
                            {
                                if (searchParam.Rooms[i].Child1Age < 2)
                                {
                                    HotelAvailRQ.Append("<m:GuestCount Count=\"" + 1 + "\" AgeQualifyingCode=\"7\" Age=\"" + searchParam.Rooms[i].Child1Age + "\" />");
                                }
                                else if (searchParam.Rooms[i].Child1Age > 12)
                                {
                                    // HotelAvailRQ.Append("<m:GuestCount Count=\"" + 1 + "\" AgeQualifyingCode=\"7\" Age=\"" + searchParam.Rooms[i].Child1Age + "\" />");
                                }
                                else
                                {
                                    HotelAvailRQ.Append("<m:GuestCount Count=\"" + 1 + "\" AgeQualifyingCode=\"8\" Age=\"" + searchParam.Rooms[i].Child1Age + "\" />");
                                }

                            }
                            if (searchParam.Rooms[i].NoofChild == 2)
                            {
                                #region child1age
                                if (searchParam.Rooms[i].Child1Age < 2)
                                {
                                    HotelAvailRQ.Append("<m:GuestCount Count=\"" + 1 + "\" AgeQualifyingCode=\"7\" Age=\"" + searchParam.Rooms[i].Child1Age + "\" />");
                                }
                                else if (searchParam.Rooms[i].Child1Age > 12)
                                {
                                    // HotelAvailRQ.Append("<m:GuestCount Count=\"" + 1 + "\" AgeQualifyingCode=\"7\" Age=\"" + searchParam.Rooms[i].Child1Age + "\" />");
                                }
                                else
                                {
                                    HotelAvailRQ.Append("<m:GuestCount Count=\"" + 1 + "\" AgeQualifyingCode=\"8\" Age=\"" + searchParam.Rooms[i].Child1Age + "\" />");
                                }
                                #endregion
                                #region child2age
                                if (searchParam.Rooms[i].Child2Age < 2)
                                {
                                    HotelAvailRQ.Append("<m:GuestCount Count=\"" + 1 + "\" AgeQualifyingCode=\"7\" Age=\"" + searchParam.Rooms[i].Child2Age + "\" />");
                                }
                                else if (searchParam.Rooms[i].Child2Age > 12)
                                {
                                    // HotelAvailRQ.Append("<m:GuestCount Count=\"" + 1 + "\" AgeQualifyingCode=\"7\" Age=\"" + searchParam.Rooms[i].Child1Age + "\" />");
                                }
                                else
                                {
                                    HotelAvailRQ.Append("<m:GuestCount Count=\"" + 1 + "\" AgeQualifyingCode=\"8\" Age=\"" + searchParam.Rooms[i].Child2Age + "\" />");
                                }
                                #endregion

                            }
                            //if (searchParam.Rooms[i].NoofChild == 3)
                            //{
                            //    #region child1age
                            //    if (searchParam.Rooms[i].Child1Age < 2)
                            //    {
                            //        HotelAvailRQ.Append("<m:GuestCount Count=\"" + 1 + "\" AgeQualifyingCode=\"7\" Age=\"" + searchParam.Rooms[i].Child1Age + "\" />");
                            //    }
                            //    else if (searchParam.Rooms[i].Child1Age > 12)
                            //    {
                            //        // HotelAvailRQ.Append("<m:GuestCount Count=\"" + 1 + "\" AgeQualifyingCode=\"7\" Age=\"" + searchParam.Rooms[i].Child1Age + "\" />");
                            //    }
                            //    else
                            //    {
                            //        HotelAvailRQ.Append("<m:GuestCount Count=\"" + 1 + "\" AgeQualifyingCode=\"8\" Age=\"" + searchParam.Rooms[i].Child1Age + "\" />");
                            //    }
                            //    #endregion child1age
                            //    #region Child2Age
                            //    if (searchParam.Rooms[i].Child2Age < 2)
                            //    {
                            //        HotelAvailRQ.Append("<m:GuestCount Count=\"" + 1 + "\" AgeQualifyingCode=\"7\" Age=\"" + searchParam.Rooms[i].Child2Age + "\" />");
                            //    }
                            //    else if (searchParam.Rooms[i].Child2Age > 12)
                            //    {
                            //        // HotelAvailRQ.Append("<m:GuestCount Count=\"" + 1 + "\" AgeQualifyingCode=\"7\" Age=\"" + searchParam.Rooms[i].Child1Age + "\" />");
                            //    }
                            //    else
                            //    {
                            //        HotelAvailRQ.Append("<m:GuestCount Count=\"" + 1 + "\" AgeQualifyingCode=\"8\" Age=\"" + searchParam.Rooms[i].Child2Age + "\" />");
                            //    }
                            //    #endregion
                            //    #region Child3Age
                            //    if (searchParam.Rooms[i].Child3Age < 2)
                            //    {
                            //        HotelAvailRQ.Append("<m:GuestCount Count=\"" + 1 + "\" AgeQualifyingCode=\"7\" Age=\"" + searchParam.Rooms[i].Child3Age + "\" />");
                            //    }
                            //    else if (searchParam.Rooms[i].Child3Age > 12)
                            //    {
                            //        // HotelAvailRQ.Append("<m:GuestCount Count=\"" + 1 + "\" AgeQualifyingCode=\"7\" Age=\"" + searchParam.Rooms[i].Child1Age + "\" />");
                            //    }
                            //    else
                            //    {
                            //        HotelAvailRQ.Append("<m:GuestCount Count=\"" + 1 + "\" AgeQualifyingCode=\"8\" Age=\"" + searchParam.Rooms[i].Child3Age + "\" />");
                            //    }
                            //    #endregion

                            //}
                            //if (searchParam.Rooms[i].NoofChild == 4)
                            //{
                            //    #region child1age
                            //    if (searchParam.Rooms[i].Child1Age < 2)
                            //    {
                            //        HotelAvailRQ.Append("<m:GuestCount Count=\"" + 1 + "\" AgeQualifyingCode=\"7\" Age=\"" + searchParam.Rooms[i].Child1Age + "\" />");
                            //    }
                            //    else if (searchParam.Rooms[i].Child1Age > 12)
                            //    {
                            //        // HotelAvailRQ.Append("<m:GuestCount Count=\"" + 1 + "\" AgeQualifyingCode=\"7\" Age=\"" + searchParam.Rooms[i].Child1Age + "\" />");
                            //    }
                            //    else
                            //    {
                            //        HotelAvailRQ.Append("<m:GuestCount Count=\"" + 1 + "\" AgeQualifyingCode=\"8\" Age=\"" + searchParam.Rooms[i].Child1Age + "\" />");
                            //    }
                            //    #endregion child1age
                            //    #region Child2Age
                            //    if (searchParam.Rooms[i].Child2Age < 2)
                            //    {
                            //        HotelAvailRQ.Append("<m:GuestCount Count=\"" + 1 + "\" AgeQualifyingCode=\"7\" Age=\"" + searchParam.Rooms[i].Child2Age + "\" />");
                            //    }
                            //    else if (searchParam.Rooms[i].Child2Age > 12)
                            //    {
                            //        // HotelAvailRQ.Append("<m:GuestCount Count=\"" + 1 + "\" AgeQualifyingCode=\"7\" Age=\"" + searchParam.Rooms[i].Child1Age + "\" />");
                            //    }
                            //    else
                            //    {
                            //        HotelAvailRQ.Append("<m:GuestCount Count=\"" + 1 + "\" AgeQualifyingCode=\"8\" Age=\"" + searchParam.Rooms[i].Child2Age + "\" />");
                            //    }
                            //    #endregion
                            //    #region Child3Age
                            //    if (searchParam.Rooms[i].Child3Age < 2)
                            //    {
                            //        HotelAvailRQ.Append("<m:GuestCount Count=\"" + 1 + "\" AgeQualifyingCode=\"7\" Age=\"" + searchParam.Rooms[i].Child3Age + "\" />");
                            //    }
                            //    else if (searchParam.Rooms[i].Child3Age > 12)
                            //    {
                            //        // HotelAvailRQ.Append("<m:GuestCount Count=\"" + 1 + "\" AgeQualifyingCode=\"7\" Age=\"" + searchParam.Rooms[i].Child1Age + "\" />");
                            //    }
                            //    else
                            //    {
                            //        HotelAvailRQ.Append("<m:GuestCount Count=\"" + 1 + "\" AgeQualifyingCode=\"8\" Age=\"" + searchParam.Rooms[i].Child3Age + "\" />");
                            //    }
                            //    #endregion
                            //    #region child4age
                            //    if (searchParam.Rooms[i].Child4Age < 2)
                            //    {
                            //        HotelAvailRQ.Append("<m:GuestCount Count=\"" + 1 + "\" AgeQualifyingCode=\"7\" Age=\"" + searchParam.Rooms[i].Child4Age + "\" />");
                            //    }
                            //    else if (searchParam.Rooms[i].Child4Age > 12)
                            //    {
                            //        // HotelAvailRQ.Append("<m:GuestCount Count=\"" + 1 + "\" AgeQualifyingCode=\"7\" Age=\"" + searchParam.Rooms[i].Child1Age + "\" />");
                            //    }
                            //    else
                            //    {
                            //        HotelAvailRQ.Append("<m:GuestCount Count=\"" + 1 + "\" AgeQualifyingCode=\"8\" Age=\"" + searchParam.Rooms[i].Child4Age + "\" />");
                            //    }
                            //    #endregion
                            //}
                        }

                        HotelAvailRQ.Append("</m:GuestCounts>");
                        HotelAvailRQ.Append("</m:RoomStayCandidate>");
                    }
                    HotelAvailRQ.Append("</m:RoomStayCandidates>");
                    HotelAvailRQ.Append("<m:HotelSearchCriteria>");
                    HotelAvailRQ.Append("<m:Criterion>");

                    string[] Hotelid = HotelIds.Split(',');

                    for (int j = 0; j < Hotelid.Length; j++)
                    {
                        HotelAvailRQ.Append("<m:HotelRef HotelCode=\"" + Hotelid[j] + "\" HotelCityCode=\"" + "" + "\" ChainCode=\"\" VenderCode=\"\" />");
                    }

                    HotelAvailRQ.Append("</m:Criterion>");
                    HotelAvailRQ.Append("</m:HotelSearchCriteria>");
                    HotelAvailRQ.Append("</m:AvailRequestSegment>");
                    HotelAvailRQ.Append("</m:AvailRequestSegments>");

                    HotelAvailRQ.Append("</m:OTA_HotelAvailRQ>");
                    HotelAvailRQ.Append("</SOAP-ENV:Body>");
                    HotelAvailRQ.Append("</SOAP-ENV:Envelope>");

                }
                HotelRequest = HotelAvailRQ.ToString();
                return HotelRequest;
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
                return "";
            }
        }
        public async Task<List<Options>> XMLDingusPriceOnly_Async(HotelX Item, CommonSearch commonSearch, string ResponseData, HotelSearchParameter SearchParam, string XMLCITY, string XMLCountry, string Currency, double CurrencyFactor, string XmlNationality, DataTable dtAgentDetail, string Provider, DataSet dsAgentDetails, DataTable dtVatHotelList, int SupplierID, string GIATACode, DataTable dtROE)
        {
            List<Options> options = new List<Options>();
            DataTable DtHotel = _commonMethod.GenerateDataTableNewV2();
            //DataTable dtHotelBedsV1RoomCacelationPolicy = Common.GenerateCancellationDataTableNewV2();
            //DataTable dtHotelBedsV1Room = Common.GenerateRoomtypeDataTableNewV2();
            //DataTable dtHotelBedsV1RoomRateBreakup = Common.GenerateoomRateBreackUpDataTableNewV2();
            double TaxPer = 0;

            try
            {
                #region Response
                if (ResponseData == string.Empty || ResponseData.Contains("ErrorTxt"))
                {
                    Item.ResponseType = "NoCount";
                    return options;
                }

                //ResponseData = Newtonsoft.Json.JsonConvert.DeserializeXmlNode(ResponseData).InnerXml;
                //string Provider = "Sandos_dingus";
                DataSet Dingus_Response = _commonMethod.GetDataSetFromXMLString(ResponseData);
                if (Dingus_Response == null || Dingus_Response.Tables["BasicPropertyInfo"] == null)
                {
                    Item.ResponseType = "NoCount";
                    return options;
                }

                #endregion

                DataView view = new DataView(Dingus_Response.Tables["BasicPropertyInfo"]);
                DataTable distinctValues = view.ToTable(true, "HotelCode");

                for (int h = 0; h < distinctValues.Rows.Count; h++)
                {
                    double FinalPrice = 0; ;
                    string ItemCode = Convert.ToString(distinctValues.Rows[h]["HotelCode"]);

                    FinalPrice = GetChepestParice(Dingus_Response, h, distinctValues, dtROE, commonSearch);

                    if (FinalPrice == 0)
                        continue;

                    Item.ResponseType = "Success";

                    #region "When not Need Room"  
                    // add hotel code from price only and room code blank here
                    if (SearchParam.Filters.IsShowRooms == 0)
                    {
                        #region "Hotel Code Block "
                        double TotalCharges = 0;
                        DataRow drXmlHotel = DtHotel.NewRow();
                        string HotelStar = "";
                        HotelStar = _commonMethod.Star_Rating(HotelStar);
                        string InternalHotelId = Convert.ToString(0);
                        drXmlHotel["XMLHotelCode"] = Convert.ToString(Dingus_Response.Tables["BasicPropertyInfo"].Rows[h]["HotelCode"]);
                        drXmlHotel["HotelName"] = "";
                        drXmlHotel["SupplierName"] = Provider;
                        drXmlHotel["OfficeID"] = 0;
                        drXmlHotel["ContractID"] = 0;
                        drXmlHotel["Rating"] = Convert.ToInt32(0);
                        drXmlHotel["XmlCity"] = XMLCITY;
                        drXmlHotel["IsShowSupplier"] = false;
                        drXmlHotel["BookingStatus"] = "Available";
                        //TotalCharges = ((Convert.ToDouble(Dingus_Response.Tables["Base"].Rows[h]["AmountAfterTax"]) * CurrencyFactor) * SearchParam.Rooms.Count) * SearchParam.NoofNights;
                        TotalCharges = FinalPrice;
                        drXmlHotel["FinalSellingPrice"] = TotalCharges;
                        drXmlHotel["SellingPrice"] = TotalCharges;
                        drXmlHotel["SupplierCurrency"] = Currency;
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
                        #region "Hotel Code Block "
                        double TotalCharges = 0;
                        double FinalMinPrice = 0;

                        DataRow drXmlHotel = DtHotel.NewRow();
                        string HotelStar = "";
                        HotelStar = _commonMethod.Star_Rating(HotelStar);
                        string InternalHotelId = Convert.ToString(0);
                        drXmlHotel["XMLHotelCode"] = Convert.ToString(Dingus_Response.Tables["BasicPropertyInfo"].Rows[h]["HotelCode"]);
                        drXmlHotel["HotelName"] = "";
                        drXmlHotel["SupplierName"] = Provider;
                        drXmlHotel["OfficeID"] = 0;
                        drXmlHotel["ContractID"] = 0;
                        drXmlHotel["Rating"] = Convert.ToInt32(0);
                        drXmlHotel["XmlCity"] = XMLCITY;
                        drXmlHotel["IsShowSupplier"] = false;
                        drXmlHotel["BookingStatus"] = "Available";
                        TotalCharges = ((Convert.ToDouble(Dingus_Response.Tables["Base"].Rows[h]["AmountAfterTax"]) * CurrencyFactor) * SearchParam.Rooms.Count) * SearchParam.NoofNights;
                        // TotalCharges = Convert.ToDouble(Dingus_Response.Tables["Total"].Rows[h]["AmountAfterTax"])  ;

                        drXmlHotel["FinalSellingPrice"] = TotalCharges;
                        drXmlHotel["SellingPrice"] = TotalCharges;
                        drXmlHotel["SupplierCurrency"] = Currency;
                        DtHotel.Rows.Add(drXmlHotel);
                        FinalMinPrice = TotalCharges;
                        #endregion

                        #region "GetRooms for this hotel"
                        bool willAddOrNot = true;
                        Int64 RaynaID = 0;
                        Int64 GiataId = 0;
                        int RatingHotel = 0;
                        Int64 ownsystemhotelId = 0;
                        double MinPrice = 1;

                        _markup.GetHotelDataCommon(ref MinPrice, ref willAddOrNot, ref ownsystemhotelId, ref RaynaID, ref GiataId, ref RatingHotel, ref drXmlHotel, commonSearch.AgentId, commonSearch.TariffID, commonSearch.dsAgentDetails, commonSearch.supplierGiataMaster, SearchParam, commonSearch.dtCountryCityMarkup, commonSearch.B2CMarkup, commonSearch.IsParentId, commonSearch.SubAgentMarkup, commonSearch.SearchByHotel, commonSearch.HotelIDS);
                        if (willAddOrNot)
                        {

                            DataTable dtHotelBedsV1RoomCacelationPolicy = _commonMethod.GenerateCancellationDataTableNewV2(); // added 
                            DataTable dtHotelBedsV1Room = _commonMethod.GenerateRoomtypeDataTableNewV2();
                            DataTable dtHotelBedsV1RoomRateBreakup = _commonMethod.GenerateoomRateBreackUpDataTableNewV2();



                            double SubAgentMarkup = 0;
                            int IsParentId = 1; int agentid = 0;
                            if (commonSearch.dtAgentDetail != null)
                            {
                                IsParentId = Convert.ToInt32(commonSearch.dtAgentDetail.Rows[0]["IsParentId"]);
                                SubAgentMarkup = Convert.ToDouble(commonSearch.dtAgentDetail.Rows[0]["B2CMarkupPer"]);
                                agentid = Convert.ToInt32(commonSearch.dtAgentDetail.Rows[0]["AgentID"]);

                            }
                            double AgentPercentage = 0.0;
                            double AgentMarkupAmount = 0.0;
                            Boolean isPerce = true;
                            Boolean blnIsShowSupplier = false;
                            int hotelRating = 0;
                            _markup.getAgetMarkupSetting(Provider, Convert.ToString(GiataId), commonSearch.dtSuplierMarkup, commonSearch.dtMarkupDetails, ref AgentPercentage, ref AgentMarkupAmount, ref blnIsShowSupplier, ref isPerce, agentid, hotelRating, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), SearchParam.CheckInDate);

                            double AgentCompanyPercentage = Convert.ToDouble(SearchParam.AgentCompanyPercentage);
                            bool IsB2C = Convert.ToBoolean(Convert.ToByte(SearchParam.IsB2BOrB2C));
                            double B2CMarkup = (IsB2C == false ? AgentCompanyPercentage : XMLHotelBookingCommon.B2CMarkup);
                            if (IsB2C == true)
                                AgentPercentage = 0;

                            DataRow[] DingusDetails = null;
                            DataRow[] DingusSearchConfig = null;
                            if (commonSearch.dtSuplierMarkup != null & commonSearch.dtSuplierMarkup != null && commonSearch.dtSuplierMarkup.Rows.Count > 0)
                            {
                                DingusDetails = commonSearch.dtSuplierMarkup.Select("SupplierName='" + Provider + "'");
                            }
                            if (commonSearch.dtSearchConfig != null & commonSearch.dtSearchConfig != null && commonSearch.dtSearchConfig.Rows.Count > 0)
                            {
                                if (SearchParam != null)
                                {
                                    if (DingusDetails.GetLength(0) > 0)
                                        DingusSearchConfig = commonSearch.dtSearchConfig.Select("SupplierID=" + Convert.ToString(DingusDetails[0]["SupplierID"]) + " and (CityID=" + SearchParam.CityID + " or CityID=0) and (Stars=0 OR (Stars>=" + SearchParam.StarStart + " and Stars<=" + SearchParam.StarEnd + "))");
                                }
                            }

                            string checkin = (SearchParam.CheckInDate).Substring(6, 4) + "-" + (SearchParam.CheckInDate).Substring(3, 2) + "-" + (SearchParam.CheckInDate).Substring(0, 2);
                            string checkout = (SearchParam.CheckOutDate).Substring(6, 4) + "-" + (SearchParam.CheckOutDate).Substring(3, 2) + "-" + (SearchParam.CheckOutDate).Substring(0, 2);

                            //DataTable dtDidaHotelRoom = Common.GenerateRoomtypeDataTableNewV2();
                            //DataTable dtDidaCancellation = Common.GenerateCancellationDataTableNewV2();
                            //DataTable dtDidaHotelRoomRateBreackUp = Common.GenerateoomRateBreackUpDataTableNewV2();


                            int Rooms = SearchParam.Rooms.Count;
                            int CutOffDays = XMLHotelBookingCommon.CutOffDays;
                            CutOffDays = _commonMethod.GetCutOffDays(commonSearch.dtSuplierMarkup, Provider);
                            DataSet DidaResponse = Dingus_Response;


                            if (SearchParam.Filters.IsShowRooms == 1)
                            {
                                //for (int j = 0; j < distinctValues.Rows.Count; j++)
                                {
                                    string HotelCode = Convert.ToString(distinctValues.Rows[h]["HotelCode"]);

                                    if (Dingus_Response.Tables.Contains("BasicPropertyInfo"))
                                    {
                                        DataRow[] drBasicPropertyInfo = Dingus_Response.Tables["BasicPropertyInfo"].Select("HotelCode='" + HotelCode + "'");

                                        for (int x = 0; x < drBasicPropertyInfo.Length; x++)
                                        {
                                            //int RoomNo = x + 1;

                                            int RoomStay_Id = Convert.ToInt32(drBasicPropertyInfo[x]["RoomStay_Id"]);
                                            string ChainCode = Convert.ToString(drBasicPropertyInfo[x]["ChainCode"]);
                                            DataRow[] drRoomRates = Dingus_Response.Tables["RoomRates"].Select("RoomStay_Id='" + RoomStay_Id + "'");
                                            string RoomRates_Id = Convert.ToString(drRoomRates[0]["RoomRates_Id"]);
                                            DataRow[] drRoomRate = Dingus_Response.Tables["RoomRate"].Select("RoomRates_Id='" + RoomRates_Id + "'");

                                            //int RoomStay_Id = Convert.ToInt32(drBasicPropertyInfo[x]["RoomStay_Id"]);
                                            DataRow[] drRoomStay = Dingus_Response.Tables["RoomStay"].Select("RoomStay_Id='" + RoomStay_Id + "'");
                                            int RoomStayCandidateRPH = Convert.ToInt32(drRoomStay[0]["RoomStayCandidateRPH"]);
                                            int RoomNo = RoomStayCandidateRPH;


                                            for (int k = 0; k < drRoomRate.Length; k++)
                                            {
                                                bool AvailabilityStatus = Convert.ToBoolean(drRoomRate[k]["AvailabilityStatus"]);
                                                //   string RateTypeCode = Convert.ToString(drRoomRate[k]["RateTypeCode"]);
                                                int NumberOfUnits = Convert.ToInt32(drRoomRate[k]["NumberOfUnits"]);

                                                if (AvailabilityStatus && NumberOfUnits > 0)
                                                {
                                                    string RoomRate_Id = Convert.ToString(drRoomRate[k]["RoomRate_Id"]);
                                                    string RoomTypeCode = Convert.ToString(drRoomRate[k]["RoomTypeCode"]);
                                                    string RatePlanID = Convert.ToString(drRoomRate[k]["RatePlanID"]);
                                                    string RatePlanCode = Convert.ToString(drRoomRate[k]["RatePlanCode"]);
                                                    string MealPlanCodes = Convert.ToString(drRoomRate[k]["MealPlanCodes"]);
                                                    string MealPlanDescription = Convert.ToString(drRoomRate[k]["MealPlanDescription"]);
                                                    string PromotionCode = Convert.ToString(drRoomRate[k]["PromotionCode"]);
                                                    string PromotionName = Convert.ToString(drRoomRate[k]["PromotionName"]);


                                                    DataRow[] drRoomTypes = Dingus_Response.Tables["RoomTypes"].Select("RoomStay_Id='" + RoomStay_Id + "'");
                                                    string RoomTypes_Id = Convert.ToString(drRoomTypes[0]["RoomTypes_Id"]);

                                                    DataRow[] drRoomType = Dingus_Response.Tables["RoomType"].Select("RoomTypes_Id='" + RoomTypes_Id + "' and RoomTypeCode='" + RoomTypeCode + "'");
                                                    string RoomType_Id = Convert.ToString(drRoomType[0]["RoomType_Id"]);

                                                    DataRow[] drRoomDescription = Dingus_Response.Tables["RoomDescription"].Select("RoomType_Id='" + RoomType_Id + "'");
                                                    string RoomName = Convert.ToString(drRoomDescription[0]["Name"]);


                                                    DataRow[] drTotal = Dingus_Response.Tables["Total"].Select("RoomRate_Id='" + RoomRate_Id + "'");
                                                    double itemprice = Convert.ToDouble(drTotal[0]["AmountAfterTax"]);
                                                    TotalCharges = itemprice; // added  9/4/2024
                                                    string Dingus_Currency = Convert.ToString(drTotal[0]["CurrencyCode"]);
                                                    //added on 19-02-2025 #40209
                                                    double CurrencyFactor1 = 1;
                                                    //  if (Dingus_Currency.ToLower() == "ars" || Dingus_Currency.ToLower() == "usd" || Dingus_Currency.ToLower() == "eur" || Dingus_Currency.ToLower() == "brl")
                                                    // {
                                                    if (Dingus_Currency.ToLower() != commonSearch.tokenData.SiteCurrency.ToLower())
                                                    {
                                                        if (Dingus_Currency.ToLower() == "ars")
                                                        {
                                                            Dingus_Currency = "AR$";
                                                        }
                                                        CurrencyFactor1 = _commonMethod.GetROEByCurrency(Dingus_Currency, dtROE);

                                                        itemprice = itemprice * CurrencyFactor1;
                                                    }
                                                    else
                                                        itemprice = itemprice * CurrencyFactor1;
                                                    //  }
                                                    //else
                                                    //{
                                                    //    continue;
                                                    //}
                                                    //end
                                                    //int Adult = Convert.ToInt32(SearchParam.Rooms[x].NoofAdults);
                                                    //int Child = Convert.ToInt32(SearchParam.Rooms[x].NoofChild);
                                                    int Adult = Convert.ToInt32(SearchParam.Rooms[RoomNo - 1].NoofAdults);
                                                    int Child = Convert.ToInt32(SearchParam.Rooms[RoomNo - 1].NoofChild);

                                                    string roomTypeId = Convert.ToString(10 + "$$$" + RoomTypeCode + "$$$" + MealPlanCodes + "$$$" + RatePlanID + "$$$" + RatePlanCode + "$$$" + HotelCode + "$$$" + PromotionCode + "$$$" + ChainCode);
                                                    string uniqid = Guid.NewGuid().ToString();
                                                    #region RateBrekUp
                                                    //double Night_Price = 0.0;

                                                    //for (int m = 0; m < SearchParam.NoofNights; m++)
                                                    //{
                                                    //    Night_Price = itemprice / SearchParam.NoofNights;
                                                    //    string newCheckInDate = Convert.ToString(SearchParam.CheckInDate);
                                                    //    string newCheckOutDate = Convert.ToString(SearchParam.CheckOutDate);
                                                    //    DateTime dtCheckInDate = DateTime.ParseExact(newCheckInDate, "dd/MM/yyyy", null);
                                                    //    DateTime dtCheckOutDate = DateTime.ParseExact(newCheckOutDate, "dd/MM/yyyy", null);
                                                    //    TimeSpan t = dtCheckOutDate - dtCheckInDate;
                                                    //    #region Vat
                                                    //    Double VatOnRateBrackUp = 0;
                                                    //    double Pricenew_wl = 0;

                                                    //    double VatOnPerNight = 0;
                                                    //    double VatonfirstnightPrice = 0;
                                                    //    double ratePerNight = 0;

                                                    //    if (TaxPer != 0)
                                                    //        Night_Price = CalculateAmountWithTax(Night_Price, TaxPer);

                                                    //    ratePerNight = _markup.ProcessHotelRateBreakupPrice_WithMarkup(1, SearchParam.CityID, SearchParam.CountryID.ToString(), Night_Price, B2CMarkup, IsParentId, isPerce, AgentPercentage, AgentMarkupAmount, SubAgentMarkup, ref VatOnPerNight);
                                                    //    #endregion




                                                    //    VatonfirstnightPrice += VatOnPerNight;
                                                    //    DataRow drHotelBedsV1RoomRateBreackUp = dtHotelBedsV1RoomRateBreakup.NewRow();
                                                    //    drHotelBedsV1RoomRateBreackUp["SupplierName"] = Provider;
                                                    //    drHotelBedsV1RoomRateBreackUp["OwnsystemHotelId"] = ownsystemhotelId;
                                                    //    drHotelBedsV1RoomRateBreackUp["ContractID"] = 0;
                                                    //    drHotelBedsV1RoomRateBreackUp["RoomNo"] = RoomNo;
                                                    //    drHotelBedsV1RoomRateBreackUp["RoomTypeId"] = roomTypeId;
                                                    //    drHotelBedsV1RoomRateBreackUp["MealId"] = MealPlanDescription;
                                                    //    drHotelBedsV1RoomRateBreackUp["Meal"] = MealPlanDescription;
                                                    //    string rateWeekColumn = Convert.ToString(dtCheckInDate.Date.AddDays(m).DayOfWeek).Substring(0, 3);
                                                    //    drHotelBedsV1RoomRateBreackUp["DateOfWeek"] = rateWeekColumn;
                                                    //    DateTime rateDate = dtCheckInDate.Date.AddDays(m);
                                                    //    drHotelBedsV1RoomRateBreackUp["Date"] = rateDate;
                                                    //    drHotelBedsV1RoomRateBreackUp["Rate"] = ratePerNight;
                                                    //    drHotelBedsV1RoomRateBreackUp["Rate"] = Pricenew_wl;
                                                    //    drHotelBedsV1RoomRateBreackUp["OrderNo"] = 1;
                                                    //    drHotelBedsV1RoomRateBreackUp["RoomTypeName"] = RoomName;
                                                    //    drHotelBedsV1RoomRateBreackUp["RoomSequenceNo"] = uniqid;
                                                    //    drHotelBedsV1RoomRateBreackUp["DateOfWeek"] = rateWeekColumn;
                                                    //    drHotelBedsV1RoomRateBreackUp["Date"] = rateDate;
                                                    //    drHotelBedsV1RoomRateBreackUp["BookingStatus"] = "Available";
                                                    //    drHotelBedsV1RoomRateBreackUp["BoardTypeID"] = "";
                                                    //    drHotelBedsV1RoomRateBreackUp["VatdiffAmount"] = VatOnPerNight;
                                                    //    drHotelBedsV1RoomRateBreackUp["RateType"] = "A Rate";
                                                    //    drHotelBedsV1RoomRateBreackUp["SupplierCurrency"] = Currency;
                                                    //    drHotelBedsV1RoomRateBreackUp["BuyingPrice"] = ratePerNight / CurrencyFactor;
                                                    //    //drHotelBedsV1RoomRateBreackUp["BuyingPrice_wl"] = ratePerNight / CurrencyFactor;
                                                    //    dtHotelBedsV1RoomRateBreakup.Rows.Add(drHotelBedsV1RoomRateBreackUp);
                                                    //}

                                                    #endregion

                                                    #region Room

                                                    DataRow drHotelBedsV1Room = dtHotelBedsV1Room.NewRow();
                                                    drHotelBedsV1Room["OwnsystemHotelId"] = ownsystemhotelId;
                                                    drHotelBedsV1Room["RoomTypeCode"] = "";
                                                    drHotelBedsV1Room["BoardType"] = MealPlanDescription;

                                                    #region Vat
                                                    double mainbuy = itemprice;


                                                    _markup.ProcessHotelRoom_WithMarkup(CurrencyFactor, Currency, false, false, SearchParam.NoofNights, SearchParam.CityID, SearchParam.CountryID.ToString(), SearchParam.CheckInDate, SearchParam.CheckOutDate, ref itemprice, ref drHotelBedsV1Room, isPerce, AgentPercentage, AgentMarkupAmount, SubAgentMarkup, B2CMarkup, IsParentId);

                                                    #endregion

                                                    drHotelBedsV1Room["MealName"] = MealPlanDescription;
                                                    drHotelBedsV1Room["MealId"] = MealPlanDescription;
                                                    drHotelBedsV1Room["Type"] = "";
                                                    drHotelBedsV1Room["Board"] = "";
                                                    drHotelBedsV1Room["BookingStatus"] = "Available";
                                                    drHotelBedsV1Room["UniqueNo"] = Guid.NewGuid().ToString();
                                                    drHotelBedsV1Room["ContractID"] = 0;
                                                    drHotelBedsV1Room["RoomNo"] = RoomNo;
                                                    drHotelBedsV1Room["RoomSequenceNo"] = uniqid;
                                                    drHotelBedsV1Room["BoardTypeID"] = "";
                                                    drHotelBedsV1Room["AdultsOccupancy"] = Adult;
                                                    drHotelBedsV1Room["ChildOccupancy"] = Child;
                                                    drHotelBedsV1Room["MaxExtraBed"] = "";
                                                    drHotelBedsV1Room["RoomSequenceNo"] = "";
                                                    drHotelBedsV1Room["EssentialInformation"] = Convert.ToString(Dingus_Response.Tables["CancelPenalty"].Rows[0]["PenaltyDescription"]);
                                                    drHotelBedsV1Room["Characteristic"] = "";
                                                    drHotelBedsV1Room["SupplierName"] = Provider;
                                                    drHotelBedsV1Room["OfferCode"] = PromotionCode;
                                                    drHotelBedsV1Room["OfferDescription"] = PromotionName;
                                                    drHotelBedsV1Room["RoomTypeName"] = RoomName;
                                                    drHotelBedsV1Room["RoomTypeId"] = roomTypeId;
                                                    //  drHotelBedsV1Room["FinalSellingPrice"] = mainbuy; //03042026
                                                    //drHotelBedsV1Room["CompanyBuyingPrice"] = TotalCharges;
                                                    dtHotelBedsV1Room.Rows.Add(drHotelBedsV1Room);

                                                    #endregion

                                                    #region Policy
                                                    string EssentialInfo = "";
                                                    DateTime MainDate = new DateTime();
                                                    if (Dingus_Response.Tables.Contains("CancelPolicies"))
                                                    {
                                                        DataRow[] drCancelPolicies = Dingus_Response.Tables["CancelPolicies"].Select("RoomStay_Id='" + RoomStay_Id + "'");
                                                        int CancelPolicies_Id = Convert.ToInt32(drCancelPolicies[0]["CancelPolicies_Id"]);

                                                        DataRow[] drCancelPenalty = Dingus_Response.Tables["CancelPenalty"].Select("CancelPolicies_Id='" + CancelPolicies_Id + "' and RatePlanID='" + RatePlanID + "'");

                                                        if (drCancelPenalty.Length > 0)
                                                        {
                                                            for (int p = 0; p < drCancelPenalty.Length; p++)
                                                            {
                                                                EssentialInfo = EssentialInfo + Convert.ToString(drCancelPenalty[p]["PenaltyDescription"]) + "</br>";
                                                                DateTime OnlyDate = Convert.ToDateTime(drCancelPenalty[0]["Start"]);

                                                                if (p == 0)
                                                                    MainDate = OnlyDate;
                                                                else if (OnlyDate < MainDate)
                                                                    MainDate = OnlyDate;
                                                            }
                                                        }
                                                        else
                                                        {
                                                            MainDate = DateTime.Now.Date;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        MainDate = DateTime.Now.Date;
                                                    }


                                                    DateTime StartDate = MainDate;

                                                    double Penalty = 0;
                                                    //double cancellationPrice = 0;
                                                    //double VatonPolicy = 0;
                                                    #region Cancellation Policy Logic and Row
                                                    DateTime CancelDate = DateTime.Now.Date;
                                                    CancelDate = StartDate;
                                                    #region Vat
                                                    double VatOnPolicy = 0;
                                                    double MainAgentCharge = mainbuy;
                                                    double CompanyChargeAmount = mainbuy;
                                                    double MainAgentCharge_wl = mainbuy;
                                                    double CancelCharge_wl = mainbuy;

                                                    Penalty = _markup.ProcessHotelCancelationPrice_WithMarkup(ref MainAgentCharge, SearchParam.NoofNights, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), mainbuy, isPerce, B2CMarkup, AgentPercentage, AgentMarkupAmount, IsParentId, SubAgentMarkup, ref VatOnPolicy);
                                                    #endregion

                                                    DateTime NowDate = DateTime.Now.Date;
                                                    if (CancelDate.AddDays(CutOffDays).Date > DateTime.Now.Date)
                                                    {
                                                        StartDate = StartDate.AddDays(CutOffDays).Date;
                                                        CancelDate = CancelDate.AddDays(CutOffDays).Date;
                                                    }
                                                    else
                                                    {
                                                        CancelDate = DateTime.Now.Date;
                                                    }
                                                    if (NowDate < CancelDate)
                                                    {
                                                        double zero = 0;

                                                        dtHotelBedsV1RoomCacelationPolicy.Rows.Add(_commonMethod.setCancellationPolicyRow(ref CompanyChargeAmount, dtHotelBedsV1RoomCacelationPolicy.NewRow(), 1, "USD", Provider, Convert.ToString(ownsystemhotelId), Convert.ToString(RoomNo), roomTypeId, RoomName, MealPlanDescription, DateTime.Now.Date, StartDate.AddDays(-1), 0, 0, "0", VatOnPolicy, "", "NO", "", EssentialInfo, "", ""));

                                                        dtHotelBedsV1RoomCacelationPolicy.AcceptChanges();

                                                        dtHotelBedsV1RoomCacelationPolicy.Rows.Add(_commonMethod.setCancellationPolicyRow(ref CompanyChargeAmount, dtHotelBedsV1RoomCacelationPolicy.NewRow(), 1, Convert.ToString(Dingus_Currency), Provider, Convert.ToString(ownsystemhotelId), Convert.ToString(RoomNo), Convert.ToString(roomTypeId), RoomName, Convert.ToString(MealPlanDescription), Convert.ToDateTime(StartDate), DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null), Penalty, Penalty, "0", VatOnPolicy, "", "NO", "", EssentialInfo, "", ""));

                                                        dtHotelBedsV1RoomCacelationPolicy.AcceptChanges();


                                                    }
                                                    else
                                                    {

                                                        dtHotelBedsV1RoomCacelationPolicy.Rows.Add(_commonMethod.setCancellationPolicyRow(ref CompanyChargeAmount, dtHotelBedsV1RoomCacelationPolicy.NewRow(), 1, Convert.ToString(Dingus_Currency), Provider, Convert.ToString(ownsystemhotelId), Convert.ToString(1), Convert.ToString(roomTypeId), RoomName, Convert.ToString(MealPlanDescription), Convert.ToDateTime(StartDate), DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null), Penalty, Penalty, "", 0, "", "Yes"));

                                                        dtHotelBedsV1RoomCacelationPolicy.AcceptChanges();
                                                    }

                                                    #endregion



                                                    #endregion

                                                }
                                            }
                                        }
                                    }

                                }
                                dtHotelBedsV1Room.AcceptChanges();
                            }
                            Options ops = (await _supplierRes.GetCommonRoomForSuppliers(GiataId, commonSearch.tokenData, commonSearch.advancedOptions, FinalMinPrice, drXmlHotel, dtHotelBedsV1Room, dtHotelBedsV1RoomCacelationPolicy, dtHotelBedsV1RoomRateBreakup, SearchParam));
                            if (ops != null)
                                options.Add(ops);
                        }
                        #endregion
                    }
                    #endregion
                }
                return options;
            }
            catch (Exception ex)
            {
                Item.ResponseType = "NoCount";
                _exception.Error(ex);
                return options;
            }
        }
        public async Task<List<Options>> XMLDingusPriceOnly_Async_Multiroom(HotelX Item, CommonSearch commonSearch, string ResponseData, HotelSearchParameter SearchParam, string XMLCITY, string XMLCountry, string Currency, double CurrencyFactor, string XmlNationality, DataTable dtAgentDetail, string Provider, DataSet dsAgentDetails, DataTable dtVatHotelList, int SupplierID, string GIATACode, DataTable dtROE)
        {
            List<Options> options = new List<Options>();
            DataTable DtHotel = _commonMethod.GenerateDataTableNewV2();
            string Dingus_Currency = Currency;
            double TaxPer = 0;
            try
            {
                #region Response
                if (ResponseData == string.Empty || ResponseData.Contains("ErrorTxt"))
                {
                    Item.ResponseType = "NoCount";
                    return options;
                }

                XmlSerializer serializer = new XmlSerializer(typeof(DingusClass.Envelope));

                DingusClass.Envelope envelope;

                using (StringReader reader = new StringReader(ResponseData))
                {
                    envelope = (DingusClass.Envelope)serializer.Deserialize(reader);
                }
                if (envelope == null && envelope.Body == null && envelope.Body.OTA_HotelAvailRS == null && envelope.Body.OTA_HotelAvailRS.RoomStays == null)
                {
                    Item.ResponseType = "NoCount";
                    return options;
                }
                var hotelGroups = envelope.Body.OTA_HotelAvailRS.RoomStays
                                  .GroupBy(x => x.BasicPropertyInfo.HotelCode).ToList();

                #endregion

                for (int h = 0; h < hotelGroups.Count; h++)
                {
                    var hotelGroup = hotelGroups[h];
                    string HotelCode = hotelGroups[h].Key;

                    double FinalPrice = 0;

                    int NoOfRooms = SearchParam.Rooms.Count;

                    Double DingusFactor = 1;
                    if (SearchParam.Filters.IsShowRooms == 0)
                    {
                        List<DingusClass.Roomlist> hotelRoom = new List<DingusClass.Roomlist>();

                        for (int r = 0; r < hotelGroup.Count(); r++)
                        {
                            var roomStay = hotelGroup.ElementAt(r);

                            for (int ro = 0; ro < roomStay.RoomRates.Length; ro++)
                            {
                                string MealPlanDescription = Convert.ToString(roomStay.RoomRates[ro].MealPlanDescription);
                                double Price = Convert.ToDouble(roomStay.RoomRates[ro].Total.AmountAfterTax);
                                Dingus_Currency = Convert.ToString(roomStay.RoomRates[ro].Total.CurrencyCode);
                                int NumberOfUnits = Convert.ToInt32(roomStay.RoomRates[ro].NumberOfUnits);
                                if (NumberOfUnits >= NoOfRooms)
                                {
                                    if (Dingus_Currency.ToLower() != commonSearch.tokenData.SiteCurrency)
                                    {
                                        DingusFactor = _commonMethod.GetROEByCurrency(Dingus_Currency, dtROE);
                                        Price = Price * DingusFactor;
                                    }
                                    DingusClass.Roomlist hotelRoomlist = new DingusClass.Roomlist();
                                    hotelRoomlist.Meal = MealPlanDescription;
                                    hotelRoomlist.Price = Price;
                                    hotelRoomlist.RPH = roomStay.RoomStayCandidateRPH;
                                    hotelRoom.Add(hotelRoomlist);
                                }
                            }
                        }

                        var mealGroups = hotelRoom.Select(x => x.Meal).Distinct().ToList(); ;
                        var uniqueRooms = hotelRoom.Select(x => x.RPH).Distinct().ToList();

                        if (uniqueRooms.Count == NoOfRooms)
                        {
                            for (int a = 0; a < mealGroups.Count; a++)
                            {
                                List<DingusClass.Roomlist>[] roomtypearr1 = new List<DingusClass.Roomlist>[NoOfRooms];

                                for (int rm = 1; rm <= NoOfRooms; rm++)
                                {
                                    if (hotelRoom[rm-1] == null)
                                        continue;

                                    List<DingusClass.Roomlist> xlist = hotelRoom.Where(A => A.Meal == mealGroups[a] && A.RPH == rm).ToList();

                                    List<DingusClass.Roomlist> Suppliermeal = new List<DingusClass.Roomlist>();
                                    if (xlist != null)
                                        Suppliermeal = xlist;

                                    foreach (DingusClass.Roomlist item in Suppliermeal)
                                    {
                                        if (roomtypearr1[rm - 1] == null)
                                        {
                                            roomtypearr1[rm - 1] = new List<DingusClass.Roomlist>();
                                        }
                                        roomtypearr1[rm - 1].Add(item.CastList<DingusClass.Roomlist>());
                                    }

                                }

                                List<List<DingusClass.Roomlist>> dataV1 = MergeRooms_list(roomtypearr1, NoOfRooms);

                                for (int room = 0; room < dataV1.Count; room++)
                                {
                                    List<DingusClass.Roomlist> roomss = dataV1[room];
                                    double TotalPrice = 0;
                                    for (int rr = 0; rr < roomss.Count; rr++)
                                    {
                                        TotalPrice += roomss[rr].Price;
                                    }

                                    if (room == 0)
                                    {
                                        FinalPrice = TotalPrice;
                                    }
                                    else if (FinalPrice > TotalPrice)
                                    {
                                        FinalPrice = TotalPrice;
                                    }

                                }

                            }
                        }

                        if (FinalPrice == 0)
                            continue; // need to chaange

                        Item.ResponseType = "Success";

                        #region "When not Need Room"  
                        // add hotel code from price only and room code blank here

                        #region "Hotel Code Block "
                        double TotalCharges = 0;
                        DataRow drXmlHotel = DtHotel.NewRow();
                        string HotelStar = "";
                        HotelStar = _commonMethod.Star_Rating(HotelStar);
                        string InternalHotelId = Convert.ToString(0);
                        drXmlHotel["XMLHotelCode"] = HotelCode;
                        drXmlHotel["HotelName"] = "";
                        drXmlHotel["SupplierName"] = Provider;
                        drXmlHotel["OfficeID"] = 0;
                        drXmlHotel["ContractID"] = 0;
                        drXmlHotel["Rating"] = Convert.ToInt32(0);
                        drXmlHotel["XmlCity"] = XMLCITY;
                        drXmlHotel["IsShowSupplier"] = false;
                        drXmlHotel["BookingStatus"] = "Available";
                        TotalCharges = FinalPrice;
                        drXmlHotel["FinalSellingPrice"] = TotalCharges;
                        drXmlHotel["SellingPrice"] = TotalCharges;
                        drXmlHotel["SupplierCurrency"] = Dingus_Currency;
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
                                decimal LowestPrice = Convert.ToDecimal(MinPrice).CalculatePrice(commonSearch.tokenData, commonSearch.advancedOptions, Dingus_Currency);
                                Options ops = (await _supplierRes.GetCommonRoomForSuppliers(GiataId, commonSearch.tokenData, commonSearch.advancedOptions, Convert.ToDouble(LowestPrice), drXmlHotel, dt_HotelRoom, dt_Cancellation, dt_RateBreackUp, SearchParam));

                                if (ops != null)
                                    options.Add(ops);
                            }
                            #endregion
                        }

                        #endregion
                    }
                    #region "When Need Rooms"
                    //add Hotel code blank and rooms code in room block
                    else
                    {
                        #region "Hotel Code Block "
                        double TotalCharges = 0;
                        double FinalMinPrice = 0;

                        DataRow drXmlHotel = DtHotel.NewRow();
                        string HotelStar = "";
                        HotelStar = _commonMethod.Star_Rating(HotelStar);
                        string InternalHotelId = Convert.ToString(0);
                        drXmlHotel["XMLHotelCode"] = HotelCode;
                        drXmlHotel["HotelName"] = "";
                        drXmlHotel["SupplierName"] = Provider;
                        drXmlHotel["OfficeID"] = 0;
                        drXmlHotel["ContractID"] = 0;
                        drXmlHotel["Rating"] = Convert.ToInt32(0);
                        drXmlHotel["XmlCity"] = XMLCITY;
                        drXmlHotel["IsShowSupplier"] = false;
                        drXmlHotel["BookingStatus"] = "Available";
                        TotalCharges = 1;// ((Convert.ToDouble(Dingus_Response.Tables["Base"].Rows[h]["AmountAfterTax"]) * CurrencyFactor) * SearchParam.Rooms.Count) * SearchParam.NoofNights;
                        drXmlHotel["FinalSellingPrice"] = TotalCharges;
                        drXmlHotel["SellingPrice"] = TotalCharges;
                        drXmlHotel["SupplierCurrency"] = Currency;
                        DtHotel.Rows.Add(drXmlHotel);
                        FinalMinPrice = TotalCharges;
                        #endregion

                        #region "GetRooms for this hotel"
                        bool willAddOrNot = true;
                        Int64 RaynaID = 0;
                        Int64 GiataId = 0;
                        int RatingHotel = 0;
                        Int64 ownsystemhotelId = 0;
                        double MinPrice = 1;

                        _markup.GetHotelDataCommon(ref MinPrice, ref willAddOrNot, ref ownsystemhotelId, ref RaynaID, ref GiataId, ref RatingHotel, ref drXmlHotel, commonSearch.AgentId, commonSearch.TariffID, commonSearch.dsAgentDetails, commonSearch.supplierGiataMaster, SearchParam, commonSearch.dtCountryCityMarkup, commonSearch.B2CMarkup, commonSearch.IsParentId, commonSearch.SubAgentMarkup, commonSearch.SearchByHotel, commonSearch.HotelIDS);
                        if (willAddOrNot)
                        {
                            DataTable dtHotelBedsV1RoomCacelationPolicy = _commonMethod.GenerateCancellationDataTableNewV2(); // added 
                            DataTable dtHotelBedsV1Room = _commonMethod.GenerateRoomtypeDataTableNewV2();
                            DataTable dtHotelBedsV1RoomRateBreakup = _commonMethod.GenerateoomRateBreackUpDataTableNewV2();

                            double SubAgentMarkup = 0;
                            int IsParentId = 1; int agentid = 0;
                            if (commonSearch.dtAgentDetail != null)
                            {
                                IsParentId = Convert.ToInt32(commonSearch.dtAgentDetail.Rows[0]["IsParentId"]);
                                SubAgentMarkup = Convert.ToDouble(commonSearch.dtAgentDetail.Rows[0]["B2CMarkupPer"]);
                                agentid = Convert.ToInt32(commonSearch.dtAgentDetail.Rows[0]["AgentID"]);

                            }
                            double AgentPercentage = 0.0;
                            double AgentMarkupAmount = 0.0;
                            Boolean isPerce = true;
                            Boolean blnIsShowSupplier = false;
                            int hotelRating = 0;
                            _markup.getAgetMarkupSetting(Provider, Convert.ToString(GiataId), commonSearch.dtSuplierMarkup, commonSearch.dtMarkupDetails, ref AgentPercentage, ref AgentMarkupAmount, ref blnIsShowSupplier, ref isPerce, agentid, hotelRating, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), SearchParam.CheckInDate);

                            double AgentCompanyPercentage = Convert.ToDouble(SearchParam.AgentCompanyPercentage);
                            bool IsB2C = Convert.ToBoolean(Convert.ToByte(SearchParam.IsB2BOrB2C));
                            double B2CMarkup = (IsB2C == false ? AgentCompanyPercentage : XMLHotelBookingCommon.B2CMarkup);
                            if (IsB2C == true)
                                AgentPercentage = 0;

                            //DataRow[] DingusDetails = null;
                            //DataRow[] DingusSearchConfig = null;
                            //if (commonSearch.dtSuplierMarkup != null & commonSearch.dtSuplierMarkup != null && commonSearch.dtSuplierMarkup.Rows.Count > 0)
                            //{
                            //    DingusDetails = commonSearch.dtSuplierMarkup.Select("SupplierName='" + Provider + "'");
                            //}
                            //if (commonSearch.dtSearchConfig != null & commonSearch.dtSearchConfig != null && commonSearch.dtSearchConfig.Rows.Count > 0)
                            //{
                            //    if (SearchParam != null)
                            //    {
                            //        if (DingusDetails.GetLength(0) > 0)
                            //            DingusSearchConfig = commonSearch.dtSearchConfig.Select("SupplierID=" + Convert.ToString(DingusDetails[0]["SupplierID"]) + " and (CityID=" + SearchParam.CityID + " or CityID=0) and (Stars=0 OR (Stars>=" + SearchParam.StarStart + " and Stars<=" + SearchParam.StarEnd + "))");
                            //    }
                            //}

                            //string checkin = (SearchParam.CheckInDate).Substring(6, 4) + "-" + (SearchParam.CheckInDate).Substring(3, 2) + "-" + (SearchParam.CheckInDate).Substring(0, 2);
                            //string checkout = (SearchParam.CheckOutDate).Substring(6, 4) + "-" + (SearchParam.CheckOutDate).Substring(3, 2) + "-" + (SearchParam.CheckOutDate).Substring(0, 2);


                            int CutOffDays = CacheMaster.CutOffDays;
                            CutOffDays = _commonMethod.GetCutOffDays(commonSearch.dtSuplierMarkup, Provider);
                            int NoofAdults = 0;
                            int NoofChild = 0;
                            for (int o = 0; o < SearchParam.Rooms.Count; o++)
                            {
                                NoofAdults += SearchParam.Rooms[o].NoofAdults;
                                NoofChild += SearchParam.Rooms[o].NoofChild;
                            }

                            if (SearchParam.Filters.IsShowRooms == 1)
                            {
                                //if (SearchParam.Rooms.Count == envelope.Body.OTA_HotelAvailRS.RoomStays.Length)
                                {
                                    List<DingusClass.HotelRoom> hotelRooms = new List<DingusClass.HotelRoom>();

                                    //  for (int i = 1; i <= SearchParam.Rooms.Count; i++)
                                    {
                                        for (int r = 0; r < hotelGroup.Count(); r++)
                                        {
                                            var roomStay = hotelGroup.ElementAt(r);

                                            // if (roomStay.RoomStayCandidateRPH == i)
                                            {
                                                int RoomStayCandidateRPH = roomStay.RoomStayCandidateRPH;
                                                // string HotelCode = envelope.Body.OTA_HotelAvailRS.RoomStays[r].BasicPropertyInfo.HotelCode;
                                                string ChainCode = roomStay.BasicPropertyInfo.ChainCode;
                                                string RoomName = "";

                                                for (int ro = 0; ro < roomStay.RoomRates.Length; ro++)
                                                {
                                                    string MealPlanDescription = Convert.ToString(roomStay.RoomRates[ro].MealPlanDescription);
                                                    string MealPlanCodes = Convert.ToString(roomStay.RoomRates[ro].MealPlanCodes);
                                                    string RoomTypeCode = Convert.ToString(roomStay.RoomRates[ro].RoomTypeCode);
                                                    int NumberOfUnits = Convert.ToInt32(roomStay.RoomRates[ro].NumberOfUnits);
                                                    int RatePlanID = Convert.ToInt32(roomStay.RoomRates[ro].RatePlanID);
                                                    string RatePlanCode = Convert.ToString(roomStay.RoomRates[ro].RatePlanCode);
                                                    string PromotionCode = Convert.ToString(roomStay.RoomRates[ro].PromotionCode);
                                                    string PromotionName = Convert.ToString(roomStay.RoomRates[ro].PromotionName);

                                                    string RateTypeCode = Convert.ToString(roomStay.RoomRates[ro].RateTypeCode == null ? "" : roomStay.RoomRates[ro].RateTypeCode);
                                                    double FinalSellingPrice = Convert.ToDouble(roomStay.RoomRates[ro].Total.AmountAfterTax);
                                                    Dingus_Currency = Convert.ToString(roomStay.RoomRates[ro].Total.CurrencyCode);

                                                    if (NumberOfUnits >= NoOfRooms)
                                                    {
                                                        if (Dingus_Currency.ToLower() != commonSearch.tokenData.SiteCurrency)
                                                        {
                                                            DingusFactor = _commonMethod.GetROEByCurrency(Dingus_Currency, dtROE); // added on 14-05-2025
                                                            
                                                        }
                                                        FinalSellingPrice = FinalSellingPrice * DingusFactor;
                                                        if (roomStay.RoomTypes != null)
                                                        {
                                                            List<OTA_HotelAvailRSRoomStayRoomType> filteredRoomTypeName = roomStay.RoomTypes
                                                                                    .Where(x => x.RoomTypeCode == RoomTypeCode)
                                                                                    .ToList();

                                                            if (filteredRoomTypeName.Count > 0)
                                                            {
                                                                for (int t = 0; t < filteredRoomTypeName.Count; t++)
                                                                {
                                                                    RoomName = filteredRoomTypeName[t].RoomDescription.Name;
                                                                }
                                                            }
                                                            if (RateTypeCode.ToLower() == "package")
                                                            {

                                                                if ("spanish" == "spanish" && false)
                                                                {
                                                                    //es=Spanish
                                                                    RoomName = RoomName + " (Para paquete)";
                                                                }
                                                                else
                                                                {
                                                                    // en= English
                                                                    RoomName = RoomName + " (For Package)";
                                                                }

                                                            }
                                                            //end
                                                        }

                                                        #region Policy
                                                        DateTime MainDate = new DateTime();
                                                        string EssentialInfo = "";

                                                        if (roomStay.CancelPolicies != null)
                                                        {

                                                            List<OTA_HotelAvailRSRoomStayCancelPenalty> filteredPolicies = roomStay.CancelPolicies
                                                                                    .Where(x => x.RatePlanID == RatePlanID)
                                                                                    .ToList();

                                                            if (filteredPolicies.Count > 0)
                                                            {
                                                                for (int p = 0; p < filteredPolicies.Count; p++)
                                                                {
                                                                    EssentialInfo = EssentialInfo + Convert.ToString(filteredPolicies[p].PenaltyDescription) + "</br>";
                                                                    // added on 17-06-2025 # 66080
                                                                    if (EssentialInfo.Contains("Noches"))
                                                                    {
                                                                        EssentialInfo = "PenaltyDescription : " + EssentialInfo;
                                                                    }
                                                                    DateTime OnlyDate = Convert.ToDateTime(filteredPolicies[p].Start);

                                                                    if (p == 0)
                                                                        MainDate = OnlyDate;
                                                                    else if (OnlyDate < MainDate)
                                                                        MainDate = OnlyDate;
                                                                }
                                                            }
                                                            else
                                                            {
                                                                MainDate = DateTime.Now.Date;
                                                            }
                                                        }
                                                        else
                                                        {
                                                            MainDate = DateTime.Now.Date;
                                                        }

                                                        #endregion

                                                        string uniqid = Guid.NewGuid().ToString();
                                                        string roomTypeId = Convert.ToString(10 + "$$$" + RoomTypeCode + "$$$" + MealPlanCodes + "$$$" + RatePlanID + "$$$" + RatePlanCode + "$$$" + HotelCode + "$$$" + PromotionCode + "$$$" + ChainCode + "$$$" + uniqid + "$$$" + RoomStayCandidateRPH);

                                                        DingusClass.HotelRoom hotelRoomList = new DingusClass.HotelRoom();
                                                        hotelRoomList.RoomTypeName = RoomName;
                                                        hotelRoomList.RoomTypeCode = roomTypeId; //RoomTypeId;
                                                        hotelRoomList.MealId = MealPlanDescription;
                                                        hotelRoomList.MealName = MealPlanDescription;
                                                        hotelRoomList.BoardType = MealPlanDescription;
                                                        hotelRoomList.RoomTypeId = roomTypeId;
                                                        hotelRoomList.RoomNo = RoomStayCandidateRPH;
                                                        hotelRoomList.EssentialInformation = EssentialInfo;
                                                        hotelRoomList.SupplierCurrency = Dingus_Currency;
                                                        hotelRoomList.FinalSellingPrice = FinalSellingPrice;
                                                        hotelRoomList.FinalPolicyDate = MainDate;
                                                        hotelRoomList.PromotionName = PromotionName;
                                                        hotelRoomList.NumberOfUnits = NumberOfUnits;
                                                        hotelRooms.Add(hotelRoomList);

                                                    }

                                                }

                                            }

                                        }
                                    }

                                    if (hotelRooms.Count > 0)
                                    {
                                        var uniqueMealNames = hotelRooms
                                .Select(h => h.MealName)
                                .Distinct()
                                .ToList();

                                        var uniqueRoomNos = hotelRooms
                                             .Select(h => h.RoomNo)
                                             .Distinct()
                                             .ToList();


                                        if (uniqueRoomNos.Count == SearchParam.Rooms.Count)
                                        {
                                            for (int a = 0; a < uniqueMealNames.Count; a++)
                                            {
                                                List<DingusClass.HotelRoom>[] roomtypearr1 = new List<DingusClass.HotelRoom>[SearchParam.Rooms.Count];

                                                for (int rm = 1; rm <= SearchParam.Rooms.Count; rm++) //search para loops
                                                {
                                                    if (hotelRooms[rm-1] == null)//Added on 10-APR-2024
                                                        continue;

                                                    List<DingusClass.HotelRoom> xlist = hotelRooms.Where(A => A.MealName == uniqueMealNames[a] && A.RoomNo == rm).ToList();

                                                    List<DingusClass.HotelRoom> Suppliermeal = new List<DingusClass.HotelRoom>();
                                                    if (xlist != null)
                                                        Suppliermeal = xlist;

                                                    foreach (DingusClass.HotelRoom item in Suppliermeal)
                                                    {
                                                        if (roomtypearr1[rm - 1] == null)
                                                        {
                                                            roomtypearr1[rm - 1] = new List<DingusClass.HotelRoom>();
                                                        }
                                                        roomtypearr1[rm - 1].Add(item.CastList<DingusClass.HotelRoom>());
                                                    }

                                                }

                                                List<List<DingusClass.HotelRoom>> dataV1 = MergeRooms(roomtypearr1, SearchParam.Rooms.Count);

                                                for (int room = 0; room < dataV1.Count; room++)
                                                {
                                                    List<DingusClass.HotelRoom> roomss = dataV1[room];

                                                    string RoomName = "";
                                                    string RoomTypeId = "";
                                                    double TotalPrice = 0;
                                                    string EssentialInfo = "";

                                                    string PromotionName = "";
                                                    string PromotionCode = "";

                                                    for (int rr = 0; rr < roomss.Count; rr++)
                                                    {
                                                        if (rr == 0)
                                                        {
                                                            RoomName = "[" + (rr + 1) + "]" + roomss[rr].RoomTypeName;
                                                            RoomTypeId = roomss[rr].RoomTypeId;
                                                            TotalPrice = roomss[rr].FinalSellingPrice;
                                                            EssentialInfo = roomss[rr].EssentialInformation;
                                                            if (roomss[rr].PromotionName != "")
                                                                PromotionName = "Room 1. " + PromotionName;

                                                            if (roomss[rr].PromotionCode != "")
                                                                PromotionCode = "Room 1. " + PromotionCode;
                                                        }
                                                        else
                                                        {
                                                            RoomName += " X [" + (rr + 1) + "]" + roomss[rr].RoomTypeName;
                                                            RoomTypeId += "##$$" + roomss[rr].RoomTypeId;
                                                            TotalPrice += roomss[rr].FinalSellingPrice;
                                                            EssentialInfo += "</br>" + roomss[rr].EssentialInformation;
                                                            if (roomss[rr].PromotionCode != "")
                                                                PromotionCode += " Room " + (rr + 1) + PromotionCode;

                                                        }
                                                    }

                                                    var earliestDate = dataV1[room].Min(x => x.FinalPolicyDate);
                                                    string MealPlanDescription = uniqueMealNames[a];

                                                    string uniqid = Guid.NewGuid().ToString();
                                                    #region RateBrekUp
                                                    //double Night_Price = 0.0;

                                                    //for (int m = 0; m < SearchParam.NoofNights; m++)
                                                    //{
                                                    //    Night_Price = TotalPrice / SearchParam.NoofNights;
                                                    //    string newCheckInDate = Convert.ToString(SearchParam.CheckInDate);
                                                    //    string newCheckOutDate = Convert.ToString(SearchParam.CheckOutDate);
                                                    //    DateTime dtCheckInDate = DateTime.ParseExact(newCheckInDate, "dd/MM/yyyy", null);
                                                    //    DateTime dtCheckOutDate = DateTime.ParseExact(newCheckOutDate, "dd/MM/yyyy", null);
                                                    //    TimeSpan t = dtCheckOutDate - dtCheckInDate;
                                                    //    #region Vat
                                                    //    Double VatOnRateBrackUp = 0;
                                                    //    double Pricenew_wl = 0;
                                                    //    //   ratePerNight = Common.ProcessHotelRateBreakupPrice_WithMarkup(1, SearchParam.CityID, SearchParam.CountryId, Night_Price, B2CMarkup, IsParentId, isPerce, AgentPercentage, AgentMarkupAmount, SubAgentMarkup, ref VatOnPerNight);
                                                    //    if (TaxPer != 0)
                                                    //        Night_Price = Common.CalculateAmountWithTax(Night_Price, TaxPer);

                                                    //    ratePerNight = Common.ProcessHotelRateBreakupPrice_WithMarkup_wl(1, SearchParam.CityID, SearchParam.CountryId, Night_Price, B2CMarkup, IsParentId, isPerce, AgentPercentage, AgentMarkupAmount, SubAgentMarkup, ref VatOnPerNight, ref Pricenew_wl, DsAgentDetail);
                                                    //    #endregion

                                                    //    VatonfirstnightPrice += VatOnPerNight;
                                                    //    DataRow drHotelBedsV1RoomRateBreackUp = dtHotelBedsV1RoomRateBreakup.NewRow();
                                                    //    drHotelBedsV1RoomRateBreackUp["SupplierName"] = provider;
                                                    //    drHotelBedsV1RoomRateBreackUp["OwnsystemHotelId"] = ownsystemhotelId;
                                                    //    drHotelBedsV1RoomRateBreackUp["ContractID"] = 0;
                                                    //    drHotelBedsV1RoomRateBreackUp["RoomNo"] = 1;
                                                    //    drHotelBedsV1RoomRateBreackUp["RoomTypeId"] = RoomTypeId;
                                                    //    drHotelBedsV1RoomRateBreackUp["MealId"] = MealPlanDescription;
                                                    //    drHotelBedsV1RoomRateBreackUp["Meal"] = MealPlanDescription;
                                                    //    string rateWeekColumn = Convert.ToString(dtCheckInDate.Date.AddDays(m).DayOfWeek).Substring(0, 3);
                                                    //    drHotelBedsV1RoomRateBreackUp["DateOfWeek"] = rateWeekColumn;
                                                    //    DateTime rateDate = dtCheckInDate.Date.AddDays(m);
                                                    //    drHotelBedsV1RoomRateBreackUp["Date"] = rateDate;
                                                    //    drHotelBedsV1RoomRateBreackUp["Rate"] = ratePerNight;
                                                    //    drHotelBedsV1RoomRateBreackUp["Rate"] = Pricenew_wl;
                                                    //    drHotelBedsV1RoomRateBreackUp["OrderNo"] = 1;
                                                    //    drHotelBedsV1RoomRateBreackUp["RoomTypeName"] = RoomName;
                                                    //    drHotelBedsV1RoomRateBreackUp["RoomSequenceNo"] = uniqid;
                                                    //    drHotelBedsV1RoomRateBreackUp["DateOfWeek"] = rateWeekColumn;
                                                    //    drHotelBedsV1RoomRateBreackUp["Date"] = rateDate;
                                                    //    drHotelBedsV1RoomRateBreackUp["BookingStatus"] = "Available";
                                                    //    drHotelBedsV1RoomRateBreackUp["BoardTypeID"] = "";
                                                    //    drHotelBedsV1RoomRateBreackUp["VatdiffAmount"] = VatOnPerNight;
                                                    //    //drHotelBedsV1RoomRateBreackUp["RateType"] = RoomName;
                                                    //    drHotelBedsV1RoomRateBreackUp["RateType"] = "A Rate";
                                                    //    ;
                                                    //    drHotelBedsV1RoomRateBreackUp["SupplierCurrency"] = Currency;
                                                    //    drHotelBedsV1RoomRateBreackUp["BuyingPrice"] = ratePerNight / Factor;
                                                    //    drHotelBedsV1RoomRateBreackUp["BuyingPrice_wl"] = ratePerNight / Factor;
                                                    //    dtHotelBedsV1RoomRateBreakup.Rows.Add(drHotelBedsV1RoomRateBreackUp);
                                                    //}

                                                    #endregion

                                                    double mainbuy = TotalPrice;
                                                    double itemprice = TotalPrice;
                                                    #region Room
                                                    DataRow drHotelBedsV1Room = dtHotelBedsV1Room.NewRow();
                                                    drHotelBedsV1Room["OwnsystemHotelId"] = ownsystemhotelId;
                                                    drHotelBedsV1Room["RoomTypeCode"] = "";
                                                    drHotelBedsV1Room["BoardType"] = MealPlanDescription;

                                                    #region Vat

                                                    _markup.ProcessHotelRoom_WithMarkup(DingusFactor, Dingus_Currency, false, true, SearchParam.NoofNights, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), SearchParam.CheckInDate, SearchParam.CheckOutDate, ref itemprice, ref drHotelBedsV1Room, isPerce, AgentPercentage, AgentMarkupAmount, SubAgentMarkup, B2CMarkup, IsParentId);
                                                    #endregion

                                                    drHotelBedsV1Room["MealName"] = MealPlanDescription;
                                                    drHotelBedsV1Room["MealId"] = MealPlanDescription;
                                                    drHotelBedsV1Room["Type"] = "";
                                                    drHotelBedsV1Room["Board"] = "";
                                                    drHotelBedsV1Room["BookingStatus"] = "Available";
                                                    drHotelBedsV1Room["UniqueNo"] = Guid.NewGuid().ToString();
                                                    drHotelBedsV1Room["ContractID"] = 0;

                                                    drHotelBedsV1Room["RoomNo"] = 1;
                                                    drHotelBedsV1Room["RoomSequenceNo"] = uniqid;
                                                    drHotelBedsV1Room["BoardTypeID"] = "";
                                                    drHotelBedsV1Room["AdultsOccupancy"] = NoofAdults;
                                                    drHotelBedsV1Room["ChildOccupancy"] = NoofChild;
                                                    drHotelBedsV1Room["MaxExtraBed"] = "";
                                                    drHotelBedsV1Room["RoomSequenceNo"] = "";
                                                    drHotelBedsV1Room["EssentialInformation"] = EssentialInfo;
                                                    drHotelBedsV1Room["Characteristic"] = "";
                                                    drHotelBedsV1Room["SupplierName"] = Provider;

                                                    drHotelBedsV1Room["OfferCode"] = PromotionCode;
                                                    drHotelBedsV1Room["OfferDescription"] = PromotionName;
                                                    drHotelBedsV1Room["RoomTypeName"] = RoomName;
                                                    drHotelBedsV1Room["RoomTypeId"] = RoomTypeId;
                                                    // drHotelBedsV1Room["JKGUIDID"] = JKGUIDID; //added by anu
                                                    //drHotelBedsV1Room["MarkupappliedDts"] = MarkupappliedDts; // added on 06-08-2024 #49746

                                                    dtHotelBedsV1Room.Rows.Add(drHotelBedsV1Room);
                                                    #endregion

                                                    #region Policy

                                                    DateTime StartDate = earliestDate;
                                                    // double Penalty = Convert.ToDouble(itemprice);
                                                    // double Penalty = Convert.ToDouble(ratePerNight);
                                                    double Penalty = 0;
                                                    #region Cancellation Policy Logic and Row
                                                    DateTime CancelDate = DateTime.Now.Date;
                                                    CancelDate = StartDate;
                                                    #region Vat
                                                    double VatOnPolicy = 0;
                                                    double MainAgentCharge = mainbuy;
                                                    double CompanyChargeAmount = mainbuy;
                                                    double MainAgentCharge_wl = mainbuy;
                                                    double CancelCharge_wl = mainbuy;

                                                    Penalty = _markup.ProcessHotelCancelationPrice_WithMarkup(ref MainAgentCharge, SearchParam.NoofNights, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), mainbuy, isPerce, B2CMarkup, AgentPercentage, AgentMarkupAmount, Convert.ToInt32(dtAgentDetail.Rows[0]["IsParentId"]), Convert.ToDouble(dtAgentDetail.Rows[0]["B2CMarkupPer"]), ref VatOnPolicy);
                                                    #endregion

                                                    DateTime NowDate = DateTime.Now.Date;
                                                    if (CancelDate.AddDays(CutOffDays).Date > DateTime.Now.Date)
                                                    {
                                                        StartDate = StartDate.AddDays(CutOffDays).Date;
                                                        CancelDate = CancelDate.AddDays(CutOffDays).Date;
                                                    }
                                                    else
                                                    {
                                                        CancelDate = DateTime.Now.Date;
                                                    }
                                                    if (NowDate < CancelDate)
                                                    {
                                                        double zero = 0;
                                                        dtHotelBedsV1RoomCacelationPolicy.Rows.Add(_commonMethod.setCancellationPolicyRow(ref zero, dtHotelBedsV1RoomCacelationPolicy.NewRow(), 1, Currency, Provider, Convert.ToString(ownsystemhotelId), Convert.ToString(1), RoomTypeId, RoomName, MealPlanDescription, DateTime.Now.Date, StartDate.AddDays(-1), 0, 0, "0", VatOnPolicy, "", "NO", "", EssentialInfo, "", ""));
                                                        dtHotelBedsV1RoomCacelationPolicy.AcceptChanges();

                                                        dtHotelBedsV1RoomCacelationPolicy.Rows.Add(_commonMethod.setCancellationPolicyRow(ref CompanyChargeAmount, dtHotelBedsV1RoomCacelationPolicy.NewRow(), 1, Currency, Provider, Convert.ToString(ownsystemhotelId), Convert.ToString(1), RoomTypeId, RoomName, MealPlanDescription, Convert.ToDateTime(StartDate), DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null), Penalty, MainAgentCharge, "0", VatOnPolicy, "", "NO", "", EssentialInfo, "", ""));
                                                        dtHotelBedsV1RoomCacelationPolicy.AcceptChanges();
                                                    }
                                                    else
                                                    {
                                                        dtHotelBedsV1RoomCacelationPolicy.Rows.Add(_commonMethod.setCancellationPolicyRow(ref CompanyChargeAmount, dtHotelBedsV1RoomCacelationPolicy.NewRow(), 1, Currency, Provider, Convert.ToString(ownsystemhotelId), Convert.ToString(1), RoomTypeId, RoomName, MealPlanDescription, DateTime.Now.Date, DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null), Penalty, MainAgentCharge, "0", VatOnPolicy, "", "Yes", "", EssentialInfo, "", ""));
                                                        dtHotelBedsV1RoomCacelationPolicy.AcceptChanges();
                                                    }

                                                    #endregion

                                                    #endregion

                                                }
                                            }
                                        }

                                    }
                                    else
                                        continue;
                                }

                                dtHotelBedsV1Room.AcceptChanges();
                            }
                            Options ops = (await _supplierRes.GetCommonRoomForSuppliers(GiataId, commonSearch.tokenData, commonSearch.advancedOptions, FinalMinPrice, drXmlHotel, dtHotelBedsV1Room, dtHotelBedsV1RoomCacelationPolicy, dtHotelBedsV1RoomRateBreakup, SearchParam));
                            if (ops != null)
                                options.Add(ops);
                        }
                        #endregion
                    }

                    #endregion
                }
                return options;
            }
            catch (Exception ex)
            {
                Item.ResponseType = "NoCount";
                _exception.Error(ex);
                return options;
            }
        }

        public static List<List<DingusClass.HotelRoom>> MergeRooms(List<DingusClass.HotelRoom>[] array, int noOfRoom)
        {
            var results = new List<List<DingusClass.HotelRoom>> {
                new List<DingusClass.HotelRoom>()
                };

            if (array.Length != noOfRoom)
                return results;

            foreach (var roomList in array)
            {
                if (roomList == null)
                    return new List<List<DingusClass.HotelRoom>>();

                var newResults = new List<List<DingusClass.HotelRoom>>();

                foreach (var existing in results)
                {
                    foreach (var room in roomList)
                    {
                        var combination = new List<DingusClass.HotelRoom>(existing);
                        combination.Add(room);
                        newResults.Add(combination);
                    }
                }

                results = newResults;
            }

            return results;
        }
        public static List<List<DingusClass.Roomlist>> MergeRooms_list(List<DingusClass.Roomlist>[] array, int noOfRoom)
        {
            var results = new List<List<DingusClass.Roomlist>> {
                new List<DingusClass.Roomlist>()
                };

            if (array.Length != noOfRoom)
                return results;

            foreach (var roomList in array)
            {
                if (roomList == null)
                    return new List<List<DingusClass.Roomlist>>();

                var newResults = new List<List<DingusClass.Roomlist>>();

                foreach (var existing in results)
                {
                    foreach (var room in roomList)
                    {
                        var combination = new List<DingusClass.Roomlist>(existing);
                        combination.Add(room);
                        newResults.Add(combination);
                    }
                }

                results = newResults;
            }

            return results;
        }
        public double GetChepestParice_2026(DingusClass.Envelope envelope, int i, DataTable dtROE, CommonSearch commonSearch)
        {
            double NetPrice = 0;
            double Price = 0;
            double NewPrice = 0;
            double LastPrice = 0;
            //added 28-06-2024
            double LastPrice1 = 0;
            double LastPrice2 = 0;
            //end 
            bool isValid = true;
            bool IsDefault = true;
            bool Ischeck = true;

            //added 28-06-2024
            bool Ischecked = true;
            bool Ischecked1 = true;
            //end 
            int distinctHotelCount = envelope.Body.OTA_HotelAvailRS.RoomStays.Select(x => x.BasicPropertyInfo.HotelCode).Distinct().Count();





            if (distinctHotelCount == 1)
            {




            }
            else if (distinctHotelCount == 2) { }
            //added 
            else if (distinctHotelCount == 3) { }

            //added 28-06-2024
            else if (distinctHotelCount == 4) { }
            else { }
            //end

            return NetPrice;
        }

        public double GetChepestParice(DataSet Dingus_Response, int i, DataTable distinctValues, DataTable dtROE, CommonSearch commonSearch)
        {
            double NetPrice = 0;
            double Price = 0;
            double NewPrice = 0;
            double LastPrice = 0;
            //added 28-06-2024
            double LastPrice1 = 0;
            double LastPrice2 = 0;
            //end 
            bool isValid = true;
            bool IsDefault = true;
            bool Ischeck = true;

            //added 28-06-2024
            bool Ischecked = true;
            bool Ischecked1 = true;
            //end 


            DataRow[] drBasicPropertyInfo = Dingus_Response.Tables["BasicPropertyInfo"].Select("HotelCode='" + distinctValues.Rows[i]["HotelCode"] + "'");

            if (drBasicPropertyInfo.Length == 1)
            {
                int RoomStay_Id = Convert.ToInt32(drBasicPropertyInfo[0]["RoomStay_Id"]);
                DataRow[] drRoomRates = Dingus_Response.Tables["RoomRates"].Select("RoomStay_Id='" + RoomStay_Id + "'");
                string RoomRates_Id = Convert.ToString(drRoomRates[0]["RoomRates_Id"]);
                DataRow[] drRoomRate = Dingus_Response.Tables["RoomRate"].Select("RoomRates_Id='" + RoomRates_Id + "'");


                for (int j = 0; j < drRoomRate.Length; j++)
                {
                    string RoomRate_Id = Convert.ToString(drRoomRate[j]["RoomRate_Id"]);
                    DataRow[] drTotal = Dingus_Response.Tables["Total"].Select("RoomRate_Id='" + RoomRate_Id + "'");
                    double room1Price = Convert.ToDouble(drTotal[0]["AmountAfterTax"]);
                    string Dingus_Currency1 = Convert.ToString(drTotal[0]["CurrencyCode"]);

                    //if (Dingus_Currency1.ToLower() != "usd")
                    //{
                    //    DataTable dtInrCurrency = Common.GetCountryCurrency(Dingus_Currency1);
                    //    double CurrencyFactor1 = Convert.ToDouble(dtInrCurrency.Rows[0]["AmtToExch"]);
                    //    room1Price = room1Price * CurrencyFactor1;
                    //}
                    //added on 19-02-2024 #40209
                    // if (Dingus_Currency1.ToLower() == "ars" || Dingus_Currency1.ToLower() == "usd" || Dingus_Currency1.ToLower() == "eur" || Dingus_Currency1.ToLower() == "brl")
                    //{
                    if (Dingus_Currency1.ToLower() != commonSearch.tokenData.SiteCurrency.ToLower())
                    {
                        double CurrencyFactor1 = _commonMethod.GetROEByCurrency(Dingus_Currency1, dtROE);
                        room1Price = room1Price * CurrencyFactor1;
                    }
                    // }
                    // else
                    // {
                    //     continue;
                    //}
                    //end

                    if (j == 0)
                    {
                        NetPrice = room1Price;
                    }
                    else if (NetPrice > room1Price)
                    {
                        NetPrice = room1Price;
                    }
                }
            }
            else if (drBasicPropertyInfo.Length == 2)
            {

                int RoomStay_Id = Convert.ToInt32(drBasicPropertyInfo[0]["RoomStay_Id"]);
                DataRow[] drRoomRates = Dingus_Response.Tables["RoomRates"].Select("RoomStay_Id='" + RoomStay_Id + "'");
                string RoomRates_Id = Convert.ToString(drRoomRates[0]["RoomRates_Id"]);
                DataRow[] drRoomRate = Dingus_Response.Tables["RoomRate"].Select("RoomRates_Id='" + RoomRates_Id + "'");

                int RoomStay_Id1 = Convert.ToInt32(drBasicPropertyInfo[1]["RoomStay_Id"]);
                DataRow[] drRoomRates1 = Dingus_Response.Tables["RoomRates"].Select("RoomStay_Id='" + RoomStay_Id1 + "'");
                string RoomRates_Id1 = Convert.ToString(drRoomRates1[0]["RoomRates_Id"]);
                DataRow[] drRoomRate1 = Dingus_Response.Tables["RoomRate"].Select("RoomRates_Id='" + RoomRates_Id1 + "'");

                for (int j = 0; j < drRoomRate.Length; j++)
                {
                    for (int k = 0; k < drRoomRate1.Length; k++)
                    {
                        string MealPlanCodes = Convert.ToString(drRoomRate[j]["MealPlanCodes"]);
                        string MealPlanCodes1 = Convert.ToString(drRoomRate1[k]["MealPlanCodes"]);
                        string RoomRate_Id = Convert.ToString(drRoomRate[j]["RoomRate_Id"]);
                        string RoomRate_Id1 = Convert.ToString(drRoomRate1[k]["RoomRate_Id"]);
                        if (MealPlanCodes == MealPlanCodes1)
                        {
                            DataRow[] drTotal = Dingus_Response.Tables["Total"].Select("RoomRate_Id='" + RoomRate_Id + "'");
                            string Dingus_Currency1 = Convert.ToString(drTotal[0]["CurrencyCode"]);
                            double room1Price = Convert.ToDouble(drTotal[0]["AmountAfterTax"]);
                            if (Dingus_Currency1.ToLower() != commonSearch.tokenData.SiteCurrency.ToLower())
                            {
                                double CurrencyFactor1 = _commonMethod.GetROEByCurrency(Dingus_Currency1, dtROE);
                                room1Price = room1Price * CurrencyFactor1;
                            }

                            DataRow[] drTotal1 = Dingus_Response.Tables["Total"].Select("RoomRate_Id='" + RoomRate_Id1 + "'");
                            string Dingus_Currency2 = Convert.ToString(drTotal1[0]["CurrencyCode"]);
                            double room2Price = Convert.ToDouble(drTotal1[0]["AmountAfterTax"]);
                            if (Dingus_Currency2.ToLower() != commonSearch.tokenData.SiteCurrency.ToLower())
                            {
                                double CurrencyFactor1 = _commonMethod.GetROEByCurrency(Dingus_Currency1, dtROE);
                                room2Price = room2Price * CurrencyFactor1;
                            }

                            Price = room1Price + room2Price;

                            if (k == 0 || IsDefault == true)
                            {
                                if (Price == 0)
                                {
                                    IsDefault = true;
                                }
                                else
                                {
                                    NewPrice = Price;
                                    IsDefault = false;
                                }

                            }
                            else if (NewPrice > Price)
                            {
                                NewPrice = Price;
                            }
                        }
                    }
                    if (j == 0 || isValid == true)
                    {
                        if (NewPrice == 0)
                        {
                            isValid = true;
                        }
                        else
                        {
                            NetPrice = NewPrice;
                            isValid = false;
                        }
                    }
                    else if (NetPrice > NewPrice)
                    {
                        NetPrice = NewPrice;
                    }

                }

            }
            //added 
            else if (drBasicPropertyInfo.Length == 3)
            {

                int RoomStay_Id = Convert.ToInt32(drBasicPropertyInfo[0]["RoomStay_Id"]);
                DataRow[] drRoomRates = Dingus_Response.Tables["RoomRates"].Select("RoomStay_Id='" + RoomStay_Id + "'");
                string RoomRates_Id = Convert.ToString(drRoomRates[0]["RoomRates_Id"]);
                DataRow[] drRoomRate = Dingus_Response.Tables["RoomRate"].Select("RoomRates_Id='" + RoomRates_Id + "'");

                int RoomStay_Id1 = Convert.ToInt32(drBasicPropertyInfo[1]["RoomStay_Id"]);
                DataRow[] drRoomRates1 = Dingus_Response.Tables["RoomRates"].Select("RoomStay_Id='" + RoomStay_Id1 + "'");
                string RoomRates_Id1 = Convert.ToString(drRoomRates1[0]["RoomRates_Id"]);
                DataRow[] drRoomRate1 = Dingus_Response.Tables["RoomRate"].Select("RoomRates_Id='" + RoomRates_Id1 + "'");

                int RoomStay_Id2 = Convert.ToInt32(drBasicPropertyInfo[2]["RoomStay_Id"]);
                DataRow[] drRoomRates2 = Dingus_Response.Tables["RoomRates"].Select("RoomStay_Id='" + RoomStay_Id2 + "'");
                string RoomRates_Id2 = Convert.ToString(drRoomRates2[0]["RoomRates_Id"]);
                DataRow[] drRoomRate2 = Dingus_Response.Tables["RoomRate"].Select("RoomRates_Id='" + RoomRates_Id2 + "'");

                for (int j = 0; j < drRoomRate.Length; j++)
                {
                    for (int k = 0; k < drRoomRate1.Length; k++)
                    {
                        for (int l = 0; l < drRoomRate2.Length; l++)
                        {

                            string MealPlanCodes = Convert.ToString(drRoomRate[j]["MealPlanCodes"]);
                            string MealPlanCodes1 = Convert.ToString(drRoomRate1[k]["MealPlanCodes"]);
                            string MealPlanCodes2 = Convert.ToString(drRoomRate2[l]["MealPlanCodes"]);
                            string RoomRate_Id = Convert.ToString(drRoomRate[j]["RoomRate_Id"]);
                            string RoomRate_Id1 = Convert.ToString(drRoomRate1[k]["RoomRate_Id"]);
                            string RoomRate_Id2 = Convert.ToString(drRoomRate2[l]["RoomRate_Id"]);
                            if (MealPlanCodes == MealPlanCodes1 && MealPlanCodes1 == MealPlanCodes2 && MealPlanCodes == MealPlanCodes2)
                            {
                                DataRow[] drTotal = Dingus_Response.Tables["Total"].Select("RoomRate_Id='" + RoomRate_Id + "'");
                                string Dingus_Currency1 = Convert.ToString(drTotal[0]["CurrencyCode"]);
                                double room1Price = Convert.ToDouble(drTotal[0]["AmountAfterTax"]);
                                if (Dingus_Currency1.ToLower() != commonSearch.tokenData.SiteCurrency.ToLower())
                                {
                                    double CurrencyFactor1 = _commonMethod.GetROEByCurrency(Dingus_Currency1, dtROE);
                                    room1Price = room1Price * CurrencyFactor1;
                                }

                                DataRow[] drTotal1 = Dingus_Response.Tables["Total"].Select("RoomRate_Id='" + RoomRate_Id1 + "'");
                                string Dingus_Currency2 = Convert.ToString(drTotal1[0]["CurrencyCode"]);
                                double room2Price = Convert.ToDouble(drTotal1[0]["AmountAfterTax"]);
                                if (Dingus_Currency2.ToLower() != commonSearch.tokenData.SiteCurrency.ToLower())
                                {
                                    double CurrencyFactor1 = _commonMethod.GetROEByCurrency(Dingus_Currency1, dtROE);
                                    room2Price = room2Price * CurrencyFactor1;
                                }

                                DataRow[] drTotal2 = Dingus_Response.Tables["Total"].Select("RoomRate_Id='" + RoomRate_Id2 + "'");
                                string Dingus_Currency3 = Convert.ToString(drTotal2[0]["CurrencyCode"]);
                                double room3Price = Convert.ToDouble(drTotal2[0]["AmountAfterTax"]);
                                if (Dingus_Currency3.ToLower() != commonSearch.tokenData.SiteCurrency.ToLower())
                                {
                                    double CurrencyFactor1 = _commonMethod.GetROEByCurrency(Dingus_Currency1, dtROE);
                                    room3Price = room3Price * CurrencyFactor1;
                                }

                                Price = room1Price + room2Price + room3Price;

                                if (l == 0 || Ischeck == true)
                                {
                                    if (Price == 0)
                                    {
                                        Ischeck = true;
                                    }
                                    else
                                    {
                                        NewPrice = Price;
                                        Ischeck = false;
                                    }

                                }
                                else if (NewPrice > Price)
                                {
                                    NewPrice = Price;
                                }
                            }

                        }
                        if (k == 0 || isValid == true)
                        {
                            if (NewPrice == 0)
                            {
                                isValid = true;
                            }
                            else
                            {
                                LastPrice = NewPrice;
                                isValid = false;
                            }

                        }
                        else if (LastPrice > NewPrice)
                        {
                            LastPrice = NewPrice;
                        }
                    }
                    if (j == 0 || IsDefault == true)
                    {
                        if (LastPrice == 0)
                        {
                            IsDefault = true;
                        }
                        else
                        {
                            NetPrice = LastPrice;
                            IsDefault = false;
                        }

                    }
                    else if (NetPrice > LastPrice)
                    {
                        NetPrice = LastPrice;
                    }
                }
            }

            //added 28-06-2024
            else if (drBasicPropertyInfo.Length == 4)
            {

                int RoomStay_Id = Convert.ToInt32(drBasicPropertyInfo[0]["RoomStay_Id"]);
                DataRow[] drRoomRates = Dingus_Response.Tables["RoomRates"].Select("RoomStay_Id='" + RoomStay_Id + "'");
                string RoomRates_Id = Convert.ToString(drRoomRates[0]["RoomRates_Id"]);
                DataRow[] drRoomRate = Dingus_Response.Tables["RoomRate"].Select("RoomRates_Id='" + RoomRates_Id + "'");

                int RoomStay_Id1 = Convert.ToInt32(drBasicPropertyInfo[1]["RoomStay_Id"]);
                DataRow[] drRoomRates1 = Dingus_Response.Tables["RoomRates"].Select("RoomStay_Id='" + RoomStay_Id1 + "'");
                string RoomRates_Id1 = Convert.ToString(drRoomRates1[0]["RoomRates_Id"]);
                DataRow[] drRoomRate1 = Dingus_Response.Tables["RoomRate"].Select("RoomRates_Id='" + RoomRates_Id1 + "'");

                int RoomStay_Id2 = Convert.ToInt32(drBasicPropertyInfo[2]["RoomStay_Id"]);
                DataRow[] drRoomRates2 = Dingus_Response.Tables["RoomRates"].Select("RoomStay_Id='" + RoomStay_Id2 + "'");
                string RoomRates_Id2 = Convert.ToString(drRoomRates2[0]["RoomRates_Id"]);
                DataRow[] drRoomRate2 = Dingus_Response.Tables["RoomRate"].Select("RoomRates_Id='" + RoomRates_Id2 + "'");

                int RoomStay_Id3 = Convert.ToInt32(drBasicPropertyInfo[3]["RoomStay_Id"]);
                DataRow[] drRoomRates3 = Dingus_Response.Tables["RoomRates"].Select("RoomStay_Id='" + RoomStay_Id3 + "'");
                string RoomRates_Id3 = Convert.ToString(drRoomRates3[0]["RoomRates_Id"]);
                DataRow[] drRoomRate3 = Dingus_Response.Tables["RoomRate"].Select("RoomRates_Id='" + RoomRates_Id3 + "'");

                for (int j = 0; j < drRoomRate.Length; j++)
                {
                    for (int k = 0; k < drRoomRate1.Length; k++)
                    {
                        for (int l = 0; l < drRoomRate2.Length; l++)
                        {
                            for (int m = 0; m < drRoomRate3.Length; m++)
                            {
                                string MealPlanCodes = Convert.ToString(drRoomRate[j]["MealPlanCodes"]);
                                string MealPlanCodes1 = Convert.ToString(drRoomRate1[k]["MealPlanCodes"]);
                                string MealPlanCodes2 = Convert.ToString(drRoomRate2[l]["MealPlanCodes"]);
                                string MealPlanCodes3 = Convert.ToString(drRoomRate3[m]["MealPlanCodes"]);

                                string RoomRate_Id = Convert.ToString(drRoomRate[j]["RoomRate_Id"]);
                                string RoomRate_Id1 = Convert.ToString(drRoomRate1[k]["RoomRate_Id"]);
                                string RoomRate_Id2 = Convert.ToString(drRoomRate2[l]["RoomRate_Id"]);
                                string RoomRate_Id3 = Convert.ToString(drRoomRate3[m]["RoomRate_Id"]);

                                if (MealPlanCodes == MealPlanCodes1 && MealPlanCodes == MealPlanCodes2 && MealPlanCodes == MealPlanCodes3 && MealPlanCodes1 == MealPlanCodes2 && MealPlanCodes1 == MealPlanCodes3 && MealPlanCodes2 == MealPlanCodes3)
                                {
                                    DataRow[] drTotal = Dingus_Response.Tables["Total"].Select("RoomRate_Id='" + RoomRate_Id + "'");
                                    string Dingus_Currency1 = Convert.ToString(drTotal[0]["CurrencyCode"]);
                                    double room1Price = Convert.ToDouble(drTotal[0]["AmountAfterTax"]);
                                    if (Dingus_Currency1.ToLower() != commonSearch.tokenData.SiteCurrency.ToLower())
                                    {
                                        double CurrencyFactor1 = _commonMethod.GetROEByCurrency(Dingus_Currency1, dtROE);
                                        room1Price = room1Price * CurrencyFactor1;
                                    }

                                    DataRow[] drTotal1 = Dingus_Response.Tables["Total"].Select("RoomRate_Id='" + RoomRate_Id1 + "'");
                                    string Dingus_Currency2 = Convert.ToString(drTotal1[0]["CurrencyCode"]);
                                    double room2Price = Convert.ToDouble(drTotal1[0]["AmountAfterTax"]);
                                    if (Dingus_Currency2.ToLower() != commonSearch.tokenData.SiteCurrency.ToLower())
                                    {
                                        double CurrencyFactor1 = _commonMethod.GetROEByCurrency(Dingus_Currency1, dtROE);
                                        room2Price = room2Price * CurrencyFactor1;
                                    }

                                    DataRow[] drTotal2 = Dingus_Response.Tables["Total"].Select("RoomRate_Id='" + RoomRate_Id2 + "'");
                                    string Dingus_Currency3 = Convert.ToString(drTotal2[0]["CurrencyCode"]);
                                    double room3Price = Convert.ToDouble(drTotal2[0]["AmountAfterTax"]);
                                    if (Dingus_Currency3.ToLower() != commonSearch.tokenData.SiteCurrency.ToLower())
                                    {
                                        double CurrencyFactor1 = _commonMethod.GetROEByCurrency(Dingus_Currency1, dtROE);
                                        room3Price = room3Price * CurrencyFactor1;
                                    }

                                    DataRow[] drTotal3 = Dingus_Response.Tables["Total"].Select("RoomRate_Id='" + RoomRate_Id3 + "'");
                                    string Dingus_Currency4 = Convert.ToString(drTotal3[0]["CurrencyCode"]);
                                    double room4Price = Convert.ToDouble(drTotal3[0]["AmountAfterTax"]);
                                    if (Dingus_Currency4.ToLower() != commonSearch.tokenData.SiteCurrency.ToLower())
                                    {
                                        double CurrencyFactor1 = _commonMethod.GetROEByCurrency(Dingus_Currency1, dtROE);
                                        room4Price = room4Price * CurrencyFactor1;
                                    }

                                    Price = room1Price + room2Price + room3Price + room4Price;

                                    if (m == 0 || Ischecked == true)
                                    {
                                        if (Price == 0)
                                        {
                                            Ischecked = true;
                                        }
                                        else
                                        {
                                            NewPrice = Price;
                                            Ischecked = false;
                                        }

                                    }
                                    else if (NewPrice > Price)
                                    {
                                        NewPrice = Price;
                                    }
                                }
                            }
                            if (l == 0 || Ischeck == true)
                            {
                                if (NewPrice == 0)
                                {
                                    Ischeck = true;
                                }
                                else
                                {
                                    LastPrice = NewPrice;
                                    Ischeck = false;
                                }

                            }
                            else if (LastPrice > NewPrice)
                            {
                                LastPrice = NewPrice;
                            }
                        }
                        if (k == 0 || isValid == true)
                        {
                            if (LastPrice == 0)
                            {
                                isValid = true;
                            }
                            else
                            {
                                LastPrice1 = LastPrice;
                                isValid = false;
                            }

                        }
                        else if (LastPrice1 > LastPrice)
                        {
                            LastPrice1 = LastPrice;
                        }
                    }
                    if (j == 0 || IsDefault == true)
                    {
                        if (LastPrice1 == 0)
                        {
                            IsDefault = true;
                        }
                        else
                        {
                            NetPrice = LastPrice1;
                            IsDefault = false;
                        }

                    }
                    else if (NetPrice > LastPrice1)
                    {
                        NetPrice = LastPrice1;
                    }
                }
            }
            else
            {

                int RoomStay_Id = Convert.ToInt32(drBasicPropertyInfo[0]["RoomStay_Id"]);
                DataRow[] drRoomRates = Dingus_Response.Tables["RoomRates"].Select("RoomStay_Id='" + RoomStay_Id + "'");
                string RoomRates_Id = Convert.ToString(drRoomRates[0]["RoomRates_Id"]);
                DataRow[] drRoomRate = Dingus_Response.Tables["RoomRate"].Select("RoomRates_Id='" + RoomRates_Id + "'");

                int RoomStay_Id1 = Convert.ToInt32(drBasicPropertyInfo[1]["RoomStay_Id"]);
                DataRow[] drRoomRates1 = Dingus_Response.Tables["RoomRates"].Select("RoomStay_Id='" + RoomStay_Id1 + "'");
                string RoomRates_Id1 = Convert.ToString(drRoomRates1[0]["RoomRates_Id"]);
                DataRow[] drRoomRate1 = Dingus_Response.Tables["RoomRate"].Select("RoomRates_Id='" + RoomRates_Id1 + "'");

                int RoomStay_Id2 = Convert.ToInt32(drBasicPropertyInfo[2]["RoomStay_Id"]);
                DataRow[] drRoomRates2 = Dingus_Response.Tables["RoomRates"].Select("RoomStay_Id='" + RoomStay_Id2 + "'");
                string RoomRates_Id2 = Convert.ToString(drRoomRates2[0]["RoomRates_Id"]);
                DataRow[] drRoomRate2 = Dingus_Response.Tables["RoomRate"].Select("RoomRates_Id='" + RoomRates_Id2 + "'");

                int RoomStay_Id3 = Convert.ToInt32(drBasicPropertyInfo[3]["RoomStay_Id"]);
                DataRow[] drRoomRates3 = Dingus_Response.Tables["RoomRates"].Select("RoomStay_Id='" + RoomStay_Id3 + "'");
                string RoomRates_Id3 = Convert.ToString(drRoomRates3[0]["RoomRates_Id"]);
                DataRow[] drRoomRate3 = Dingus_Response.Tables["RoomRate"].Select("RoomRates_Id='" + RoomRates_Id3 + "'");

                int RoomStay_Id4 = Convert.ToInt32(drBasicPropertyInfo[4]["RoomStay_Id"]);
                DataRow[] drRoomRates4 = Dingus_Response.Tables["RoomRates"].Select("RoomStay_Id='" + RoomStay_Id4 + "'");
                string RoomRates_Id4 = Convert.ToString(drRoomRates4[0]["RoomRates_Id"]);
                DataRow[] drRoomRate4 = Dingus_Response.Tables["RoomRate"].Select("RoomRates_Id='" + RoomRates_Id4 + "'");

                for (int j = 0; j < drRoomRate.Length; j++)
                {
                    for (int k = 0; k < drRoomRate1.Length; k++)
                    {
                        for (int l = 0; l < drRoomRate2.Length; l++)
                        {
                            for (int m = 0; m < drRoomRate3.Length; m++)
                            {
                                for (int n = 0; n < drRoomRate4.Length; n++)
                                {
                                    string MealPlanCodes = Convert.ToString(drRoomRate[j]["MealPlanCodes"]);
                                    string MealPlanCodes1 = Convert.ToString(drRoomRate1[k]["MealPlanCodes"]);
                                    string MealPlanCodes2 = Convert.ToString(drRoomRate2[l]["MealPlanCodes"]);
                                    string MealPlanCodes3 = Convert.ToString(drRoomRate3[m]["MealPlanCodes"]);
                                    string MealPlanCodes4 = Convert.ToString(drRoomRate4[n]["MealPlanCodes"]);

                                    string RoomRate_Id = Convert.ToString(drRoomRate[j]["RoomRate_Id"]);
                                    string RoomRate_Id1 = Convert.ToString(drRoomRate1[k]["RoomRate_Id"]);
                                    string RoomRate_Id2 = Convert.ToString(drRoomRate2[l]["RoomRate_Id"]);
                                    string RoomRate_Id3 = Convert.ToString(drRoomRate3[m]["RoomRate_Id"]);
                                    string RoomRate_Id4 = Convert.ToString(drRoomRate4[n]["RoomRate_Id"]);

                                    if (MealPlanCodes == MealPlanCodes1 && MealPlanCodes == MealPlanCodes2 && MealPlanCodes == MealPlanCodes3 && MealPlanCodes == MealPlanCodes4 && MealPlanCodes1 == MealPlanCodes2 && MealPlanCodes1 == MealPlanCodes3 && MealPlanCodes1 == MealPlanCodes4 && MealPlanCodes2 == MealPlanCodes3 && MealPlanCodes2 == MealPlanCodes4 && MealPlanCodes3 == MealPlanCodes4)
                                    {
                                        DataRow[] drTotal = Dingus_Response.Tables["Total"].Select("RoomRate_Id='" + RoomRate_Id + "'");
                                        string Dingus_Currency1 = Convert.ToString(drTotal[0]["CurrencyCode"]);
                                        double room1Price = Convert.ToDouble(drTotal[0]["AmountAfterTax"]);
                                        if (Dingus_Currency1.ToLower() != commonSearch.tokenData.SiteCurrency.ToLower())
                                        {
                                            double CurrencyFactor1 = _commonMethod.GetROEByCurrency(Dingus_Currency1, dtROE);
                                            room1Price = room1Price * CurrencyFactor1;
                                        }

                                        DataRow[] drTotal1 = Dingus_Response.Tables["Total"].Select("RoomRate_Id='" + RoomRate_Id1 + "'");
                                        string Dingus_Currency2 = Convert.ToString(drTotal1[0]["CurrencyCode"]);
                                        double room2Price = Convert.ToDouble(drTotal1[0]["AmountAfterTax"]);
                                        if (Dingus_Currency2.ToLower() != commonSearch.tokenData.SiteCurrency.ToLower())
                                        {
                                            double CurrencyFactor1 = _commonMethod.GetROEByCurrency(Dingus_Currency1, dtROE);
                                            room2Price = room2Price * CurrencyFactor1;
                                        }

                                        DataRow[] drTotal2 = Dingus_Response.Tables["Total"].Select("RoomRate_Id='" + RoomRate_Id2 + "'");
                                        string Dingus_Currency3 = Convert.ToString(drTotal2[0]["CurrencyCode"]);
                                        double room3Price = Convert.ToDouble(drTotal2[0]["AmountAfterTax"]);
                                        if (Dingus_Currency3.ToLower() != commonSearch.tokenData.SiteCurrency.ToLower())
                                        {
                                            double CurrencyFactor1 = _commonMethod.GetROEByCurrency(Dingus_Currency1, dtROE);
                                            room3Price = room3Price * CurrencyFactor1;
                                        }

                                        DataRow[] drTotal3 = Dingus_Response.Tables["Total"].Select("RoomRate_Id='" + RoomRate_Id3 + "'");
                                        string Dingus_Currency4 = Convert.ToString(drTotal3[0]["CurrencyCode"]);
                                        double room4Price = Convert.ToDouble(drTotal3[0]["AmountAfterTax"]);
                                        if (Dingus_Currency4.ToLower() != commonSearch.tokenData.SiteCurrency.ToLower())
                                        {
                                            double CurrencyFactor1 = _commonMethod.GetROEByCurrency(Dingus_Currency1, dtROE);
                                            room4Price = room4Price * CurrencyFactor1;
                                        }
                                        DataRow[] drTotal4 = Dingus_Response.Tables["Total"].Select("RoomRate_Id='" + RoomRate_Id4 + "'");
                                        string Dingus_Currency5 = Convert.ToString(drTotal4[0]["CurrencyCode"]);
                                        double room5Price = Convert.ToDouble(drTotal4[0]["AmountAfterTax"]);
                                        if (Dingus_Currency5.ToLower() != commonSearch.tokenData.SiteCurrency.ToLower())
                                        {
                                            double CurrencyFactor1 = _commonMethod.GetROEByCurrency(Dingus_Currency1, dtROE);
                                            room5Price = room5Price * CurrencyFactor1;
                                        }

                                        Price = room1Price + room2Price + room3Price + room4Price + room5Price;

                                        if (n == 0 || Ischecked == true)
                                        {
                                            if (Price == 0)
                                            {
                                                Ischecked = true;
                                            }
                                            else
                                            {
                                                NewPrice = Price;
                                                Ischecked = false;
                                            }

                                        }
                                        else if (NewPrice > Price)
                                        {
                                            NewPrice = Price;
                                        }
                                    }
                                }
                                if (m == 0 || Ischeck == true)
                                {
                                    if (NewPrice == 0)
                                    {
                                        Ischeck = true;
                                    }
                                    else
                                    {
                                        LastPrice = NewPrice;
                                        Ischeck = false;
                                    }

                                }
                                else if (LastPrice > NewPrice)
                                {
                                    LastPrice = NewPrice;
                                }
                            }
                            if (l == 0 || isValid == true)
                            {
                                if (LastPrice == 0)
                                {
                                    isValid = true;
                                }
                                else
                                {
                                    LastPrice1 = LastPrice;
                                    isValid = false;
                                }

                            }
                            else if (LastPrice1 > LastPrice)
                            {
                                LastPrice1 = LastPrice;
                            }
                        }
                        if (k == 0 || Ischecked1 == true)
                        {
                            if (LastPrice1 == 0)
                            {
                                Ischecked1 = true;
                            }
                            else
                            {
                                LastPrice2 = LastPrice1;
                                Ischecked1 = false;
                            }

                        }
                        else if (LastPrice2 > LastPrice1)
                        {
                            LastPrice2 = LastPrice1;
                        }
                    }
                    if (j == 0 || IsDefault == true)
                    {
                        if (LastPrice2 == 0)
                        {
                            IsDefault = true;
                        }
                        else
                        {
                            NetPrice = LastPrice2;
                            IsDefault = false;
                        }

                    }
                    else if (NetPrice > LastPrice2)
                    {
                        NetPrice = LastPrice2;
                    }
                }
            }
            //end

            return NetPrice;
        }
        public static double CalculateAmountWithTax(double price, double TaxAmt)
        {
            try
            {
                double TaxAmount = (price * TaxAmt) / 100;
                double finalAmt = price + TaxAmount;
                return finalAmt;
            }
            catch (Exception ex)
            {
                // Common.Error(ex);
                return 0;
            }
        }
        public async Task<PriceCheckCMDto> PriceUpdateDingus(string supplierName, DataTable dtCheckPriceReqStructure, DataSet dsCartDetails, DataRow[] DingusDetails, string UniqueNo, string AgentUniqueNo, int i, int j, string IsFor, double CompanyBuyPrice, bool IsError, double itemprice, TokenData tokenData, string Searchkey, int AgentId = 0,string TechName="")
        {
            PriceCheckCMDto priceCheckDingus = new PriceCheckCMDto();
            string RequestSPR = "";
            string ResponseSPR = "";
            string Error = "";
            string time = "";
            string responsetype = "Success";
            string ResponseData = "-";
            //int AgentId = Convert.ToInt32(Common.getSession("UserId") == "" ? "0" : Common.getSession("UserId"));
            string supplier = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["SupplierName"]);
            string GiataId = "";
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
                string language = "en";
                //if (SearchPara.Language != 0) 
                //{
                //    language = "es"; 
                //}
                //else
                //{
                //    language = "en"; 
                //}


                GiataId = Convert.ToString(dsCartDetails.Tables[0].Rows[0]["GiataId"]); //added 


                string[] HBArray = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomTypeId"]).Split(new String[] { "##$$" }, StringSplitOptions.None);

                DataSet dsTemp = new DataSet();
                string DingusUrl = Convert.ToString(DingusDetails[0]["WebSiteURL"]);
                string UserId = Convert.ToString(DingusDetails[0]["UserID"]);
                string UserName = Convert.ToString(DingusDetails[0]["UserName"]);
                string Password = Convert.ToString(DingusDetails[0]["Password"]);
                string RoomId = "";

                string HotelCode = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["XMLHotelId"]);

                string CheckIn = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["CheckInDate"]);
                CheckIn = CheckIn.Substring(0, 4) + "-" + CheckIn.Substring(4, 2) + "-" + CheckIn.Substring(6, 2);
                string CheckOut = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["CheckOutDate"]);
                CheckOut = CheckOut.Substring(0, 4) + "-" + CheckOut.Substring(4, 2) + "-" + CheckOut.Substring(6, 2);

                #region PriceCheckRequest

                String Request = DingusPriceCheckRequest(CheckIn, CheckOut, HotelCode, UserName, Password, DingusUrl, supplierName, language, dsCartDetails);

                #endregion

                _commonMethod.ThirdPartyBookingHistory(Convert.ToString(UniqueNo), Convert.ToString(AgentUniqueNo), Request, "-", "Before Request hit Price Check" + IsFor, Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["SupplierName"]), "Request Confirm", "-", "WithInEarth");

                dtCheckPriceReqStructure.Rows.Add(_commonMethod.AddCheckPriceReq(dtCheckPriceReqStructure.NewRow(), j + 1, Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[j]["UserCartID"]), Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[j]["UserCartDetailId"]), Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomTypeID"]), Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["SupplierName"]).ToLower(), Request));

                ResponseDto responseDto = await Get_Dingus_Response(DingusUrl, Request, supplierName, Error);


                RequestSPR = Request;
                ResponseData = responseDto.Response;
                Error = responseDto.Error;

                _commonMethod.ThirdPartyBookingHistory(Convert.ToString(UniqueNo), Convert.ToString(AgentUniqueNo), Request, ResponseData + Error, "After Request hit Price Check" + IsFor, Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["SupplierName"]), "Response Confirm", "-", "WithInEarth");

                if (ResponseData.ToLower().Contains("errors") || ResponseData.Trim() == "")
                {
                    #region SupplierPerformanceReportForPrebook
                    try
                    {
                        responsetype = "GeneralError";
                        string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(supplier, "PreBook", AgentId, time, responsetype, RequestSPR, ResponseSPR, Error, Searchkey, TechName, srphotelid);
                    }
                    catch (Exception e)
                    {
                        _ = _exception.Error(e, Searchkey);
                    }
                    #endregion
                    priceCheckDingus.Flag = true;
                    return priceCheckDingus;
                }

                DataSet Dingus_Response = _commonMethod.GetDataSetFromXMLString(ResponseData);

                if (Dingus_Response == null)
                {
                    #region SupplierPerformanceReportForPrebook
                    try
                    {
                        responsetype = "GeneralError";
                        string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(supplier, "PreBook", AgentId, time, responsetype, RequestSPR, ResponseSPR, Error, Searchkey, TechName, srphotelid);
                    }
                    catch (Exception e)
                    {
                        _ = _exception.Error(e, Searchkey);
                    }
                    #endregion
                    priceCheckDingus.Flag = true;
                    return priceCheckDingus;
                }
                double FinalPriceMain = 0;
                for (int r = 0; r < Dingus_Response.Tables["RoomStay"].Rows.Count; r++)
                {
                    string[] roomtypeid = HBArray[r].Split(new String[] { "$$$" }, StringSplitOptions.None);

                    string RoomTypeCode = roomtypeid[1];
                    string MealPlanCodes = roomtypeid[2];
                    string RatePlanID = roomtypeid[3];
                    string RatePlanCode = roomtypeid[4];
                    if (Dingus_Response.Tables["RoomRate"] != null)
                    {
                        DataRow[] drRoomRate = Dingus_Response.Tables["RoomRate"].Select("RoomTypeCode='" + RoomTypeCode + "' and MealPlanCodes='" + MealPlanCodes + "' and RatePlanID='" + RatePlanID + "' and RatePlanCode='" + RatePlanCode + "'");

                        if (drRoomRate.Length > 0)
                        {
                            string RoomRate_Id = Convert.ToString(drRoomRate[r]["RoomRate_Id"]);

                            DataRow[] drTotal = Dingus_Response.Tables["Total"].Select("RoomRate_Id='" + RoomRate_Id + "'");

                            if (drTotal.Length > 0)
                            {
                                string DingusCurrency = Convert.ToString(drTotal[0]["CurrencyCode"]);

                                double CurrencyFactorDingus = 1;
                                if (DingusCurrency.ToLower() != tokenData.SiteCurrency.ToLower())
                                {
                                    CurrencyFactorDingus = _commonMethod.GetROEByCurrency(DingusCurrency, tokenData.dtROE);
                                }

                                double FinalPriceTemp = Convert.ToDouble(drTotal[0]["AmountAfterTax"]) * CurrencyFactorDingus;

                                FinalPriceMain = FinalPriceTemp + FinalPriceMain;


                                itemprice = Convert.ToDouble(Math.Round(FinalPriceMain, 2));
                                priceCheckDingus.ItemPrice = itemprice;
                                if (Convert.ToDouble(Math.Round(itemprice.TruncateDecimalPlaces(0))) > Math.Round(CompanyBuyPrice.TruncateDecimalPlaces(0)))
                                {
                                    #region SupplierPerformanceReportForPrebook
                                    try
                                    {
                                        responsetype = "GeneralError";
                                        string ResponseData_Ex = "CompanyBuyingPrices = " + CompanyBuyPrice + " MainPrice = " + itemprice + " #Response: " + ResponseSPR;
                                        string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(supplier, "PreBook", AgentId, time, responsetype, RequestSPR, ResponseData_Ex, Error, Searchkey, TechName, srphotelid);
                                    }
                                    catch (Exception e)
                                    {
                                        _ = _exception.Error(e, Searchkey);
                                    }
                                    #endregion
                                    priceCheckDingus.Flag = true;
                                    return priceCheckDingus;
                                }
                            }
                            else
                            {
                                #region SupplierPerformanceReportForPrebook
                                try
                                {
                                    responsetype = "GeneralError";
                                    string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(supplier, "PreBook", AgentId, time, responsetype, RequestSPR, ResponseSPR, Error, Searchkey, TechName, srphotelid);
                                }
                                catch (Exception e)
                                {
                                    _ = _exception.Error(e, Searchkey);
                                }
                                #endregion
                                priceCheckDingus.Flag = true;
                                return priceCheckDingus;
                            }
                        }
                        else
                        {
                            #region SupplierPerformanceReportForPrebook
                            try
                            {
                                responsetype = "GeneralError";
                                string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(supplier, "PreBook", AgentId, time, responsetype, RequestSPR, ResponseSPR, Error, Searchkey, TechName, srphotelid);
                            }
                            catch (Exception e)
                            {
                                _ = _exception.Error(e, Searchkey);
                            }
                            #endregion
                            priceCheckDingus.Flag = true;
                            return priceCheckDingus;
                        }

                    }
                    else
                    {
                        #region SupplierPerformanceReportForPrebook
                        try
                        {
                            responsetype = "GeneralError";
                            string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(supplier, "PreBook", AgentId, time, responsetype, RequestSPR, ResponseSPR, Error, Searchkey, TechName, srphotelid);
                        }
                        catch (Exception e)
                        {
                            _ = _exception.Error(e, Searchkey);
                        }
                        #endregion
                        priceCheckDingus.Flag = true;
                        return priceCheckDingus;
                    }
                }

                #region SupplierPerformanceReportForPrebook
                try
                {
                    responsetype = "Success";
                    string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(supplier, "PreBook", AgentId, time, responsetype, RequestSPR, ResponseSPR, Error, Searchkey, TechName, srphotelid);
                }
                catch (Exception e)
                {
                    _ = _exception.Error(e, Searchkey);
                }
                #endregion

            }
            catch (Exception e)
            {
                _ = _exception.Error(e, Searchkey);
                IsError = true;
            }
            priceCheckDingus.Flag = false;
            return priceCheckDingus;
        }


        public string DingusPriceCheckRequest(string checkIn, string checkOut, string HotelId, string Username, string Password, string DingusUrl, string SupplierName, string language, DataSet dsCartDetails)
        {
            //PriceCheckCMDto priceCheckDingusR = new PriceCheckCMDto();
            string HotelRequest = "";
            string Error = "";
            try
            {

                int MinChildAge = 0;
                int MaxChildAge = 0;

                StringBuilder HotelAvailRQ = new StringBuilder();
                if (HotelId != string.Empty)
                {
                    HotelAvailRQ.Append("<SOAP-ENV:Envelope xmlns:SOAP-ENV=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:SOAPENC=\"http://schemas.xmlsoap.org/soap/encoding/\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchemainstance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">");
                    HotelAvailRQ.Append("<SOAP-ENV:Body>");
                    HotelAvailRQ.Append("<m:OTA_HotelAvailRQ xmlns:m=\"http://www.opentravel.org/OTA/2003/05\" Version=\"1.0\" PrimaryLangID=\"" + language + "\" AvailRatesOnly=\"true\" GetOnlyBAR=\"false\" >");
                    HotelAvailRQ.Append("<m:POS>");
                    HotelAvailRQ.Append("<m:Source>");
                    HotelAvailRQ.Append("<m:RequestorID MessagePassword=\"" + Password + "\" ID=\"" + Username + "\" />");
                    HotelAvailRQ.Append("</m:Source>");
                    HotelAvailRQ.Append("</m:POS>");
                    HotelAvailRQ.Append("<m:AvailRequestSegments>");
                    HotelAvailRQ.Append("<m:AvailRequestSegment>");
                    HotelAvailRQ.Append("<m:StayDateRange Start=\"" + checkIn + "\" End=\"" + checkOut + "\"/>");
                    HotelAvailRQ.Append("<m:RoomStayCandidates>");

                    for (int j = 0; j < dsCartDetails.Tables[1].DefaultView.Count; j++)
                    {
                        HotelAvailRQ.Append("<m:RoomStayCandidate RPH=\"" + (j + 1) + "\" Quantity=\"" + 1 + "\" >");
                        HotelAvailRQ.Append("<m:GuestCounts>");
                        int NoofChild = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[j]["NoofChild"]);
                        int NoofAdults = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[j]["NoOfAdults"]);
                        int Child1Age = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[j]["ChildAge1"]);
                        int Child2Age = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[j]["ChildAge2"]);
                        //int Child3Age = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[j]["ChildAge3"]);
                        //int Child4Age = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[j]["ChildAge4"]);


                        switch (NoofChild)
                        {
                            case 1:
                                if (Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[j]["ChildAge1"]) > 12)
                                    NoofAdults = NoofAdults + 1;
                                break;
                            case 2:
                                if (Child1Age > 12)
                                    NoofAdults = NoofAdults + 1;
                                if (Child2Age > 12)
                                    NoofAdults = NoofAdults + 1;
                                break;
                                //case 3:
                                //    if (Child1Age > 12)
                                //        NoofAdults = NoofAdults + 1;
                                //    if (Child2Age > 12)
                                //        NoofAdults = NoofAdults + 1;
                                //    if (Child3Age > 12)
                                //        NoofAdults = NoofAdults + 1;
                                //    break;
                                //case 4:
                                //    if (Child1Age > 12)
                                //        NoofAdults = NoofAdults + 1;
                                //    if (Child2Age > 12)
                                //        NoofAdults = NoofAdults + 1;
                                //    if (Child3Age > 12)
                                //        NoofAdults = NoofAdults + 1;
                                //    if (Child4Age > 12)
                                //        NoofAdults = NoofAdults + 1;
                                //    break;
                        }


                        if (NoofAdults > 0)
                        {
                            HotelAvailRQ.Append("<m:GuestCount Count=\"" + NoofAdults + "\" AgeQualifyingCode=\"10\" Age=\"30\" />");
                        }
                        if (NoofChild > 0)
                        {
                            if (NoofChild == 1)
                            {
                                if (Child1Age < 2)
                                {
                                    HotelAvailRQ.Append("<m:GuestCount Count=\"" + 1 + "\" AgeQualifyingCode=\"7\" Age=\"" + Child1Age + "\" />");
                                }
                                else if (Child1Age > 12)
                                {
                                    //HotelAvailRQ.Append("<m:GuestCount Count=\"" + 1 + "\" AgeQualifyingCode=\"7\" Age=\"" + DR[i].Child1Age + "\" />");
                                }
                                else
                                {
                                    HotelAvailRQ.Append("<m:GuestCount Count=\"" + 1 + "\" AgeQualifyingCode=\"8\" Age=\"" + Child1Age + "\" />");
                                }

                            }
                            if (NoofChild == 2)
                            {
                                #region Child1Age
                                if (Child1Age < 2)
                                {
                                    HotelAvailRQ.Append("<m:GuestCount Count=\"" + 1 + "\" AgeQualifyingCode=\"7\" Age=\"" + Child1Age + "\" />");
                                }
                                else if (Child1Age > 12)
                                {
                                    //HotelAvailRQ.Append("<m:GuestCount Count=\"" + 1 + "\" AgeQualifyingCode=\"7\" Age=\"" + DR[i].Child1Age + "\" />");
                                }
                                else
                                {
                                    HotelAvailRQ.Append("<m:GuestCount Count=\"" + 1 + "\" AgeQualifyingCode=\"8\" Age=\"" + Child1Age + "\" />");
                                }
                                #endregion
                                #region Child2Age
                                if (Child2Age < 2)
                                {
                                    HotelAvailRQ.Append("<m:GuestCount Count=\"" + 1 + "\" AgeQualifyingCode=\"7\" Age=\"" + Child2Age + "\" />");
                                }
                                else if (Child2Age > 12)
                                {
                                    //HotelAvailRQ.Append("<m:GuestCount Count=\"" + 1 + "\" AgeQualifyingCode=\"7\" Age=\"" + DR[i].Child1Age + "\" />");
                                }
                                else
                                {
                                    HotelAvailRQ.Append("<m:GuestCount Count=\"" + 1 + "\" AgeQualifyingCode=\"8\" Age=\"" + Child2Age + "\" />");
                                }
                                #endregion
                            }

                            //if (NoofChild == 3)
                            //{
                            //    #region Child1Age
                            //    if (Child1Age < 2)
                            //    {
                            //        HotelAvailRQ.Append("<m:GuestCount Count=\"" + 1 + "\" AgeQualifyingCode=\"7\" Age=\"" + Child1Age + "\" />");
                            //    }
                            //    else if (Child1Age > 12)
                            //    {
                            //        //HotelAvailRQ.Append("<m:GuestCount Count=\"" + 1 + "\" AgeQualifyingCode=\"7\" Age=\"" + DR[i].Child1Age + "\" />");
                            //    }
                            //    else
                            //    {
                            //        HotelAvailRQ.Append("<m:GuestCount Count=\"" + 1 + "\" AgeQualifyingCode=\"8\" Age=\"" + Child1Age + "\" />");
                            //    }
                            //    #endregion
                            //    #region Child2Age
                            //    if (Child2Age < 2)
                            //    {
                            //        HotelAvailRQ.Append("<m:GuestCount Count=\"" + 1 + "\" AgeQualifyingCode=\"7\" Age=\"" + Child2Age + "\" />");
                            //    }
                            //    else if (Child2Age > 12)
                            //    {
                            //        //HotelAvailRQ.Append("<m:GuestCount Count=\"" + 1 + "\" AgeQualifyingCode=\"10\" Age=\"" + DR[i].Child2Age + "\" />");
                            //        //DR[i].NoofAdults = +1;
                            //    }
                            //    else
                            //    {
                            //        HotelAvailRQ.Append("<m:GuestCount Count=\"" + 1 + "\" AgeQualifyingCode=\"8\" Age=\"" + Child2Age + "\" />");
                            //    }
                            //    #endregion
                            //    #region Child3Age
                            //    if (Child3Age <1)
                            //    {
                            //        HotelAvailRQ.Append("<m:GuestCount Count=\"" + 1 + "\" AgeQualifyingCode=\"8\" Age=\"" + Child3Age + "\" />");
                            //    }
                            //    else if (Child3Age > 12)
                            //    {
                            //        //HotelAvailRQ.Append("<m:GuestCount Count=\"" + 1 + "\" AgeQualifyingCode=\"10\" Age=\"" + DR[i].Child3Age + "\" />");
                            //        //DR[i].NoofAdults = +1;
                            //    }
                            //    else
                            //    {
                            //        HotelAvailRQ.Append("<m:GuestCount Count=\"" + 1 + "\" AgeQualifyingCode=\"7\" Age=\"" + Child3Age + "\" />");
                            //    }
                            //    #endregion

                            //}
                            //if (NoofChild == 4)
                            //{
                            //    #region Child1Age
                            //    if (Child1Age < 2)
                            //    {
                            //        HotelAvailRQ.Append("<m:GuestCount Count=\"" + 1 + "\" AgeQualifyingCode=\"7\" Age=\"" + Child1Age + "\" />");
                            //    }
                            //    else if (Child1Age > 12)
                            //    {
                            //        //HotelAvailRQ.Append("<m:GuestCount Count=\"" + 1 + "\" AgeQualifyingCode=\"7\" Age=\"" + DR[i].Child1Age + "\" />");
                            //    }
                            //    else
                            //    {
                            //        HotelAvailRQ.Append("<m:GuestCount Count=\"" + 1 + "\" AgeQualifyingCode=\"8\" Age=\"" + Child1Age + "\" />");
                            //    }
                            //    #endregion
                            //    #region Child2Age
                            //    if (Child2Age < 2)
                            //    {
                            //        HotelAvailRQ.Append("<m:GuestCount Count=\"" + 1 + "\" AgeQualifyingCode=\"7\" Age=\"" + Child2Age + "\" />");
                            //    }
                            //    else if (Child2Age > 12)
                            //    {
                            //        //HotelAvailRQ.Append("<m:GuestCount Count=\"" + 1 + "\" AgeQualifyingCode=\"7\" Age=\"" + DR[i].Child1Age + "\" />");
                            //    }
                            //    else
                            //    {
                            //        HotelAvailRQ.Append("<m:GuestCount Count=\"" + 1 + "\" AgeQualifyingCode=\"8\" Age=\"" + Child2Age + "\" />");
                            //    }
                            //    #endregion
                            //    #region Child3Age
                            //    if (Child3Age > 1)
                            //    {
                            //        HotelAvailRQ.Append("<m:GuestCount Count=\"" + 1 + "\" AgeQualifyingCode=\"8\" Age=\"" + Child3Age + "\" />");
                            //    }
                            //    else if (Child3Age > 12)
                            //    {
                            //        //HotelAvailRQ.Append("<m:GuestCount Count=\"" + 1 + "\" AgeQualifyingCode=\"7\" Age=\"" + DR[i].Child1Age + "\" />");
                            //    }
                            //    else
                            //    {
                            //        HotelAvailRQ.Append("<m:GuestCount Count=\"" + 1 + "\" AgeQualifyingCode=\"7\" Age=\"" + Child3Age + "\" />");
                            //    }
                            //    #endregion
                            //    #region Child4Age
                            //    if (Child4Age > 1)
                            //    {
                            //        HotelAvailRQ.Append("<m:GuestCount Count=\"" + 1 + "\" AgeQualifyingCode=\"8\" Age=\"" + Child4Age + "\" />");
                            //    }
                            //    else if (Child4Age > 12)
                            //    {
                            //        //HotelAvailRQ.Append("<m:GuestCount Count=\"" + 1 + "\" AgeQualifyingCode=\"7\" Age=\"" + DR[i].Child1Age + "\" />");
                            //    }
                            //    else
                            //    {
                            //        HotelAvailRQ.Append("<m:GuestCount Count=\"" + 1 + "\" AgeQualifyingCode=\"7\" Age=\"" +Child4Age + "\" />");
                            //    }
                            //    #endregion
                            //}
                        }

                        HotelAvailRQ.Append("</m:GuestCounts>");
                        HotelAvailRQ.Append("</m:RoomStayCandidate>");
                    }
                    HotelAvailRQ.Append("</m:RoomStayCandidates>");
                    HotelAvailRQ.Append("<m:HotelSearchCriteria>");
                    HotelAvailRQ.Append("<m:Criterion>");


                    HotelAvailRQ.Append("<m:HotelRef HotelCode=\"" + HotelId + "\" HotelCityCode=\"" + "" + "\" ChainCode=\"\" VenderCode=\"\" />");

                    HotelAvailRQ.Append("</m:Criterion>");
                    HotelAvailRQ.Append("</m:HotelSearchCriteria>");
                    HotelAvailRQ.Append("</m:AvailRequestSegment>");
                    HotelAvailRQ.Append("</m:AvailRequestSegments>");

                    HotelAvailRQ.Append("</m:OTA_HotelAvailRQ>");
                    HotelAvailRQ.Append("</SOAP-ENV:Body>");
                    HotelAvailRQ.Append("</SOAP-ENV:Envelope>");

                }
                HotelRequest = HotelAvailRQ.ToString();
                return HotelRequest;
            }
            catch (Exception ex)
            {
                Error = ex.Message.ToString();
                return "";
            }
        }
        public static string DingusHotelInfoRequest(string HotelId, string Username, string Password, string language)
        {
            try
            {
                StringBuilder HotelDescriptiveInfo = new StringBuilder();
                if (HotelId != string.Empty)
                {
                    HotelDescriptiveInfo.Append("<SOAP-ENV:Envelope xmlns:SOAP-ENV=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:SOAPENC=\"http://schemas.xmlsoap.org/soap/encoding/\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchemainstance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">");
                    HotelDescriptiveInfo.Append("<SOAP-ENV:Body>");
                    HotelDescriptiveInfo.Append("<m:OTA_HotelDescriptiveInfoRQ xmlns:m=\"http://www.opentravel.org/OTA/2003/05\" Version=\"1.0\" PrimaryLangID=\"" + language + "\" AvailRatesOnly=\"true\" GetOnlyBAR=\"false\" >");
                    HotelDescriptiveInfo.Append("<m:POS>");
                    HotelDescriptiveInfo.Append("<m:Source>");
                    HotelDescriptiveInfo.Append("<m:RequestorID MessagePassword=\"" + Password + "\" ID=\"" + Username + "\" />");
                    HotelDescriptiveInfo.Append("</m:Source>");
                    HotelDescriptiveInfo.Append("</m:POS>");
                    HotelDescriptiveInfo.Append("<m:HotelDescriptiveInfos>");
                    // for (int j = 0; j < HotelIds.Length; j++)
                    // {
                    HotelDescriptiveInfo.Append("<m:HotelDescriptiveInfo HotelCode=\"" + HotelId + "\" HotelCityCode=\"" + "" + "\"/>");
                    // }
                    HotelDescriptiveInfo.Append("</m:HotelDescriptiveInfos>");
                    HotelDescriptiveInfo.Append("</m:OTA_HotelDescriptiveInfoRQ>");
                    HotelDescriptiveInfo.Append("</SOAP-ENV:Body>");
                    HotelDescriptiveInfo.Append("</SOAP-ENV:Envelope>");
                }
                string HotelRequest = HotelDescriptiveInfo.ToString();
                return HotelRequest;
            }
            catch (Exception ex)
            {
                string Error = ex.Message.ToString();
                return "";
            }
        }

        public async Task<ResponseDto> Get_Dingus_Response(string DingusURL, string request, string Provider, string Error)
        {
            ResponseDto responseDto = new ResponseDto();
            System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
            timer.Start();

            string strOutput = "";
            try
            {
                HttpWebRequest Request;
                Request = (HttpWebRequest)HttpWebRequest.Create(DingusURL);
                //Request.Timeout = 12000;
                Request.Method = "POST";
                Request.ContentType = "text/xml;charset=\"utf-8\"";
                Request.KeepAlive = false;
                Request.Headers.Add("Accept-Encoding", "gzip, deflate");
                Request.Accept = "text/xml";
                Stream newStream = null;
                byte[] data = Encoding.ASCII.GetBytes(request);
                Request.ContentLength = data.Length;
                newStream = Request.GetRequestStream();
                newStream.Write(data, 0, data.Length);


                try
                {
                    HttpWebResponse Response = (HttpWebResponse)Request.GetResponse();
                    Stream responseStream = Response.GetResponseStream();
                    if (Response.ContentEncoding.ToLower().Contains("gzip"))
                        responseStream = new System.IO.Compression.GZipStream(responseStream, System.IO.Compression.CompressionMode.Decompress);
                    else if (Response.ContentEncoding.ToLower().Contains("deflate"))
                        responseStream = new System.IO.Compression.DeflateStream(responseStream, System.IO.Compression.CompressionMode.Decompress);
                    StreamReader Reader = new StreamReader(responseStream);
                    strOutput = Reader.ReadToEnd();
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

                }

            }
            catch (Exception ex)
            {
                Error = ex.Message.ToString();
            }
            responseDto.Response = strOutput;
            responseDto.Error = Error;
            return responseDto;

        }
        public string Get_Dingus_BookingRequestNew(BookRequest_newversion bookRequest, DataTable dtPassengers, DataRow[] DingusDetails, string UniqueNo, int i, int p, string ReferenceNo, string SpecialRequestToPass, string CheckIn, string CheckOut)
        {
            try
            {
                string AgentVoucherNo = "";
                #region Booking RQ
                string language = "en";
                //if (SearchPara.Language != 0)
                //{
                //    language = "es"; //es=Spanish
                //}
                //else
                //{
                //    language = "en"; //en= English
                //}
                //string PropertyId =Convert.ToString(bookRequest.userCart.UserCartDetails[jj].ContractID);
                StringBuilder BookRQ = new StringBuilder();

                string Prefix = "", Name = "", SurName = "";
                bool PrimaryIndicator = false;


                string UserName = Convert.ToString(DingusDetails[0]["UserName"]);
                string Password = Convert.ToString(DingusDetails[0]["Password"]);

                //ReferenceNo = ReferenceNo.Remove(0, 4);

                BookRQ.Append("<SOAP-ENV:Envelope xmlns:SOAP-ENV=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:SOAPENC=\"http://schemas.xmlsoap.org/soap/encoding/\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchemainstance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">");
                BookRQ.Append("<SOAP-ENV:Body>");
                BookRQ.Append("<m:OTA_HotelResRQ xmlns:m=\"http://www.opentravel.org/OTA/2003/05\" Version=\"2.0\" ResStatus=\"Commit\" PrimaryLangID=\"" + language + "\" >");
                BookRQ.Append("<m:POS>");
                BookRQ.Append("<m:Source>");
                BookRQ.Append("<m:RequestorID ID=\"" + UserName + "\" MessagePassword=\"" + Password + "\"/>");
                BookRQ.Append("</m:Source>");
                BookRQ.Append("</m:POS>");
                BookRQ.Append("<m:HotelReservations>");
                // BookRQ.Append("<m:HotelReservation ReaStatus=\"Book\"  CreatorID=\"\" CreateDateTime=\"" + DateTime.Now.ToString("yyyy-MM-ddTHH:mm:sszzz") + "" + "\">");
                //BookRQ.Append("<m:HotelReservation  CreateDateTime=\"" + String.Format("{0:s}", DateTime.Now)  + "\">");
                BookRQ.Append("<m:HotelReservation  CreateDateTime=\"" + DateTime.Now.ToString("yyyy-MM-dd") + "\">");
                BookRQ.Append("<m:RoomStays>");
                for (int x = 0; x < bookRequest.userCart.UserCartDetails.Count; x++)
                {
                    // dtPassengers.DefaultView.RowFilter = "PickUp='" + Convert.ToString(bookRequest.userCart.UserCartDetails[x].RoomTypeId) + "' and RoomNo='" + Convert.ToString(bookRequest.userCart.UserCartDetails[x].RoomNo + "'");

                    string rph = Convert.ToString(bookRequest.userCart.UserCartDetails[x].RoomNo);

                    BookRQ.Append("<m:RoomStay RPH=\"" + rph + "\">");
                    string Meal = Convert.ToString(bookRequest.userCart.UserCartDetails[x].MealPlan);

                    string HotelCode = Convert.ToString(bookRequest.userCart.HotelId);
                    string HotelName = Convert.ToString(bookRequest.userCart.ServiceName);

                    string[] Roomidsplit = Convert.ToString(bookRequest.userCart.UserCartDetails[x].RoomTypeId).Split(new String[] { "##$$" }, StringSplitOptions.None);

                    string[] Roomids = Roomidsplit[x].Split(new String[] { "$$$" }, StringSplitOptions.None);

                    string RoomTypeCode = Roomids[1].ToString();
                    string MealPlanCodes = Roomids[2].ToString();
                    string RatePlanID = Roomids[3].ToString();
                    string RatePlanCode = Roomids[4].ToString();
                    string HotelCodeNew = Roomids[5].ToString();
                    string PromotionCode = Roomids[6].ToString();
                    string ChainCode = Roomids[7].ToString();

                    string UserCartDetailId = Convert.ToString(bookRequest.userCart.UserCartDetails[x].UserCartDetailId);
                    int NoofChild = Convert.ToInt32(bookRequest.userCart.UserCartDetails[x].NoofChild);
                    int NoofAdults = Convert.ToInt32(bookRequest.userCart.UserCartDetails[x].NoofAdult);

                    switch (NoofChild)
                    {
                        case 1:
                            if (Convert.ToInt32(bookRequest.userCart.UserCartDetails[x].ChildAge1) > 12)
                                NoofAdults = NoofAdults + 1;
                            break;
                        case 2:
                            if (Convert.ToInt32(bookRequest.userCart.UserCartDetails[x].ChildAge1) > 12)
                                NoofAdults = NoofAdults + 1;
                            if (Convert.ToInt32(bookRequest.userCart.UserCartDetails[x].ChildAge2) > 12)
                                NoofAdults = NoofAdults + 1;
                            break;
                            //case 3:
                            //    if (Convert.ToInt32(bookRequest.userCart.UserCartDetails[jj].ChildAge1) > 12)
                            //        NoofAdults = NoofAdults + 1;
                            //    if (Convert.ToInt32(bookRequest.userCart.UserCartDetails[jj].ChildAge2) > 12)
                            //        NoofAdults = NoofAdults + 1;
                            //    if (Convert.ToInt32(bookRequest.userCart.UserCartDetails[jj].ChildAge3) > 12)
                            //        NoofAdults = NoofAdults + 1;
                            //    break;
                            //case 4:
                            //    if (Convert.ToInt32(bookRequest.userCart.UserCartDetails[jj].ChildAge1) > 12)
                            //        NoofAdults = NoofAdults + 1;
                            //    if (Convert.ToInt32(bookRequest.userCart.UserCartDetails[jj].ChildAge2) > 12)
                            //        NoofAdults = NoofAdults + 1;
                            //    if (Convert.ToInt32(bookRequest.userCart.UserCartDetails[jj].ChildAge3) > 12)
                            //        NoofAdults = NoofAdults + 1;
                            //    if (Convert.ToInt32(bookRequest.userCart.UserCartDetails[jj].ChildAge4) > 12)
                            //        NoofAdults = NoofAdults + 1;
                            //    break;
                    }

                    #region GuestCount
                    BookRQ.Append("<m:GuestCounts>");
                    BookRQ.Append("<m:GuestCount AgeQualifyingCode=\"" + 10 + "\"" + " " + "Count=\"" + NoofAdults + "\" Age=\"30\" />");
                    switch (Convert.ToInt32(bookRequest.userCart.UserCartDetails[x].NoofChild))
                    {
                        case 1:
                            #region ChildAge1
                            if (Convert.ToInt32(bookRequest.userCart.UserCartDetails[x].ChildAge1) < 2)
                            {
                                BookRQ.Append("<m:GuestCount AgeQualifyingCode=\"" + 7 + "\"" + " " + "Count=\"" + 1 + "\" Age=\"" + Convert.ToInt32(bookRequest.userCart.UserCartDetails[x].ChildAge1) + "\" />");
                            }
                            else if (Convert.ToInt32(bookRequest.userCart.UserCartDetails[x].ChildAge1) > 12)
                            {
                                //BookRQ.Append("<m:GuestCount AgeQualifyingCode=\"" + 7 + "\"" + " " + "Count=\"" + 1 + "\" Age=\"" + Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[jj]["ChildAge1"]) + "\" />");
                            }
                            else
                            {
                                BookRQ.Append("<m:GuestCount AgeQualifyingCode=\"" + 8 + "\"" + " " + "Count=\"" + 1 + "\" Age=\"" + Convert.ToInt32(bookRequest.userCart.UserCartDetails[x].ChildAge1) + "\" />");
                            }
                            #endregion
                            break;
                        case 2:
                            #region ChildAge1
                            if (Convert.ToInt32(bookRequest.userCart.UserCartDetails[x].ChildAge1) < 2)
                            {
                                BookRQ.Append("<m:GuestCount AgeQualifyingCode=\"" + 7 + "\"" + " " + "Count=\"" + 1 + "\" Age=\"" + Convert.ToInt32(bookRequest.userCart.UserCartDetails[x].ChildAge1) + "\" />");
                            }
                            else if (Convert.ToInt32(bookRequest.userCart.UserCartDetails[x].ChildAge1) > 12)
                            {
                                //BookRQ.Append("<m:GuestCount AgeQualifyingCode=\"" + 7 + "\"" + " " + "Count=\"" + 1 + "\" Age=\"" + Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[jj]["ChildAge1"]) + "\" />");
                            }
                            else
                            {
                                BookRQ.Append("<m:GuestCount AgeQualifyingCode=\"" + 8 + "\"" + " " + "Count=\"" + 1 + "\" Age=\"" + Convert.ToInt32(bookRequest.userCart.UserCartDetails[x].ChildAge1) + "\" />");
                            }
                            #endregion
                            #region ChildAge2
                            if (Convert.ToInt32(bookRequest.userCart.UserCartDetails[x].ChildAge2) < 2)
                            {
                                BookRQ.Append("<m:GuestCount AgeQualifyingCode=\"" + 7 + "\"" + " " + "Count=\"" + 1 + "\" Age=\"" + Convert.ToInt32(bookRequest.userCart.UserCartDetails[x].ChildAge2) + "\" />");
                            }
                            else if (Convert.ToInt32(bookRequest.userCart.UserCartDetails[x].ChildAge2) > 12)
                            {
                                //BookRQ.Append("<m:GuestCount AgeQualifyingCode=\"" + 7 + "\"" + " " + "Count=\"" + 1 + "\" Age=\"" + Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[jj]["ChildAge1"]) + "\" />");
                            }
                            else
                            {
                                BookRQ.Append("<m:GuestCount AgeQualifyingCode=\"" + 8 + "\"" + " " + "Count=\"" + 1 + "\" Age=\"" + Convert.ToInt32(bookRequest.userCart.UserCartDetails[x].ChildAge2) + "\" />");
                            }
                            #endregion
                            break;
                            #region "comment"
                            //case 3:
                            //    #region ChildAge1
                            //    if (Convert.ToInt32(bookRequest.userCart.UserCartDetails[jj].ChildAge1) < 2)
                            //    {
                            //        BookRQ.Append("<m:GuestCount AgeQualifyingCode=\"" + 7 + "\"" + " " + "Count=\"" + 1 + "\" Age=\"" + Convert.ToInt32(bookRequest.userCart.UserCartDetails[jj].ChildAge1) + "\" />");
                            //    }
                            //    else if (Convert.ToInt32(bookRequest.userCart.UserCartDetails[jj].ChildAge1) > 12)
                            //    {
                            //        //BookRQ.Append("<m:GuestCount AgeQualifyingCode=\"" + 7 + "\"" + " " + "Count=\"" + 1 + "\" Age=\"" + Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[jj]["ChildAge1"]) + "\" />");
                            //    }
                            //    else
                            //    {
                            //        BookRQ.Append("<m:GuestCount AgeQualifyingCode=\"" + 8 + "\"" + " " + "Count=\"" + 1 + "\" Age=\"" + Convert.ToInt32(bookRequest.userCart.UserCartDetails[jj].ChildAge1) + "\" />");
                            //    }
                            //    #endregion
                            //    #region ChildAge2
                            //    if (Convert.ToInt32(bookRequest.userCart.UserCartDetails[jj].ChildAge2) < 2)
                            //    {
                            //        BookRQ.Append("<m:GuestCount AgeQualifyingCode=\"" + 7 + "\"" + " " + "Count=\"" + 1 + "\" Age=\"" + Convert.ToInt32(bookRequest.userCart.UserCartDetails[jj].ChildAge2) + "\" />");
                            //    }
                            //    else if (Convert.ToInt32(bookRequest.userCart.UserCartDetails[jj].ChildAge2) > 12)
                            //    {
                            //        //BookRQ.Append("<m:GuestCount AgeQualifyingCode=\"" + 7 + "\"" + " " + "Count=\"" + 1 + "\" Age=\"" + Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[jj]["ChildAge1"]) + "\" />");
                            //    }
                            //    else
                            //    {
                            //        BookRQ.Append("<m:GuestCount AgeQualifyingCode=\"" + 8 + "\"" + " " + "Count=\"" + 1 + "\" Age=\"" + Convert.ToInt32(bookRequest.userCart.UserCartDetails[jj].ChildAge2) + "\" />");
                            //    }
                            //    #endregion
                            //    #region ChildAge3
                            //    if (Convert.ToInt32(bookRequest.userCart.UserCartDetails[jj].ChildAge3) < 2)
                            //    {
                            //        BookRQ.Append("<m:GuestCount AgeQualifyingCode=\"" + 8 + "\"" + " " + "Count=\"" + 1 + "\" Age=\"" + Convert.ToInt32(bookRequest.userCart.UserCartDetails[jj].ChildAge3) + "\" />");
                            //    }
                            //    else if (Convert.ToInt32(bookRequest.userCart.UserCartDetails[jj].ChildAge3) > 12)
                            //    {
                            //        //BookRQ.Append("<m:GuestCount AgeQualifyingCode=\"" + 7 + "\"" + " " + "Count=\"" + 1 + "\" Age=\"" + Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[jj]["ChildAge1"]) + "\" />");
                            //    }
                            //    else
                            //    {
                            //        BookRQ.Append("<m:GuestCount AgeQualifyingCode=\"" + 7 + "\"" + " " + "Count=\"" + 1 + "\" Age=\"" + Convert.ToInt32(bookRequest.userCart.UserCartDetails[jj].ChildAge3) + "\" />");
                            //    }
                            //    #endregion
                            //    break;
                            //case 4:
                            //    #region ChildAge1
                            //    if (Convert.ToInt32(bookRequest.userCart.UserCartDetails[jj].ChildAge1) < 2)
                            //    {
                            //        BookRQ.Append("<m:GuestCount AgeQualifyingCode=\"" + 7 + "\"" + " " + "Count=\"" + 1 + "\" Age=\"" + Convert.ToInt32(bookRequest.userCart.UserCartDetails[jj].ChildAge1) + "\" />");
                            //    }
                            //    else if (Convert.ToInt32(bookRequest.userCart.UserCartDetails[jj].ChildAge1) > 12)
                            //    {
                            //        //BookRQ.Append("<m:GuestCount AgeQualifyingCode=\"" + 7 + "\"" + " " + "Count=\"" + 1 + "\" Age=\"" + Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[jj]["ChildAge1"]) + "\" />");
                            //    }
                            //    else
                            //    {
                            //        BookRQ.Append("<m:GuestCount AgeQualifyingCode=\"" + 8 + "\"" + " " + "Count=\"" + 1 + "\" Age=\"" + Convert.ToInt32(bookRequest.userCart.UserCartDetails[jj].ChildAge1) + "\" />");
                            //    }
                            //    #endregion
                            //    #region ChildAge2
                            //    if (Convert.ToInt32(bookRequest.userCart.UserCartDetails[jj].ChildAge2) < 2)
                            //    {
                            //        BookRQ.Append("<m:GuestCount AgeQualifyingCode=\"" + 7 + "\"" + " " + "Count=\"" + 1 + "\" Age=\"" + Convert.ToInt32(bookRequest.userCart.UserCartDetails[jj].ChildAge2) + "\" />");
                            //    }
                            //    else if (Convert.ToInt32(bookRequest.userCart.UserCartDetails[jj].ChildAge2) > 12)
                            //    {
                            //        //BookRQ.Append("<m:GuestCount AgeQualifyingCode=\"" + 7 + "\"" + " " + "Count=\"" + 1 + "\" Age=\"" + Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[jj]["ChildAge1"]) + "\" />");
                            //    }
                            //    else
                            //    {
                            //        BookRQ.Append("<m:GuestCount AgeQualifyingCode=\"" + 8 + "\"" + " " + "Count=\"" + 1 + "\" Age=\"" + Convert.ToInt32(bookRequest.userCart.UserCartDetails[jj].ChildAge2) + "\" />");
                            //    }
                            //    #endregion
                            //    #region ChildAge3
                            //    if (Convert.ToInt32(bookRequest.userCart.UserCartDetails[jj].ChildAge3) < 2)
                            //    {
                            //        BookRQ.Append("<m:GuestCount AgeQualifyingCode=\"" + 8 + "\"" + " " + "Count=\"" + 1 + "\" Age=\"" + Convert.ToInt32(bookRequest.userCart.UserCartDetails[jj].ChildAge3) + "\" />");
                            //    }
                            //    else if (Convert.ToInt32(bookRequest.userCart.UserCartDetails[jj].ChildAge3) > 12)
                            //    {
                            //        //BookRQ.Append("<m:GuestCount AgeQualifyingCode=\"" + 7 + "\"" + " " + "Count=\"" + 1 + "\" Age=\"" + Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[jj]["ChildAge1"]) + "\" />");
                            //    }
                            //    else
                            //    {
                            //        BookRQ.Append("<m:GuestCount AgeQualifyingCode=\"" + 7 + "\"" + " " + "Count=\"" + 1 + "\" Age=\"" + Convert.ToInt32(bookRequest.userCart.UserCartDetails[jj].ChildAge3) + "\" />");
                            //    }
                            //    #endregion
                            //    #region ChildAge4
                            //    if (Convert.ToInt32(bookRequest.userCart.UserCartDetails[jj].ChildAge4) < 2)
                            //    {
                            //        BookRQ.Append("<m:GuestCount AgeQualifyingCode=\"" + 8 + "\"" + " " + "Count=\"" + 1 + "\" Age=\"" + Convert.ToInt32(bookRequest.userCart.UserCartDetails[jj].ChildAge4) + "\" />");
                            //    }
                            //    else if (Convert.ToInt32(bookRequest.userCart.UserCartDetails[jj].ChildAge4) > 12)
                            //    {
                            //        //BookRQ.Append("<m:GuestCount AgeQualifyingCode=\"" + 7 + "\"" + " " + "Count=\"" + 1 + "\" Age=\"" + Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[jj]["ChildAge1"]) + "\" />");
                            //    }
                            //    else
                            //    {
                            //        BookRQ.Append("<m:GuestCount AgeQualifyingCode=\"" + 7 + "\"" + " " + "Count=\"" + 1 + "\" Age=\"" + Convert.ToInt32(bookRequest.userCart.UserCartDetails[jj].ChildAge4) + "\" />");
                            //    }
                            //    #endregion
                            //    break;
                            #endregion
                    }
                    #endregion
                    BookRQ.Append("</m:GuestCounts>");
                    BookRQ.Append("<m:TimeSpan Start=\"" + CheckIn + "\" End=\"" + CheckOut + "\"/>");
                    BookRQ.Append("<m:RoomRates>");
                    BookRQ.Append("<m:RoomRate RoomTypeCode=\"" + RoomTypeCode + "\" MealPlanCodes=\"" + MealPlanCodes + "\" RatePlanCode=\"" + RatePlanCode + "\" PromotionCode=\"" + PromotionCode + "\" SellChannel=\"" + " " + "\"/>");
                    BookRQ.Append("</m:RoomRates>");
                    BookRQ.Append("<m:BasicPropertyInfo HotelCode=\"" + HotelCodeNew + "\" ChainCode=\"" + ChainCode + "\" VenderCode=\"" + "" + "\"/>");

                    //try
                    //{
                    //    if (!string.IsNullOrEmpty(SpecialRequestToPass))
                    //        BookRQ.Append("<m:Remarks>" + SpecialRequestToPass + "</m:Remarks>");
                    //}
                    //catch (Exception ex)
                    //{
                    //    _ = _exception.Error(ex);
                    //}
                    BookRQ.Append("</m:RoomStay>");

                    ReferenceNo = UserCartDetailId + "|" + (p + 1); // added on  07-08-2024 # 50740 
                    AgentVoucherNo = ReferenceNo;// UserCartDetailId;

                }
                BookRQ.Append("</m:RoomStays>");

                BookRQ.Append("<m:ResGuests>");

                for (int d = 0; d < dtPassengers.DefaultView.Count; d++)
                {

                    Prefix = Convert.ToString(dtPassengers.DefaultView[d]["Prefix"]).Replace(".", "");
                    //Prefix = CheckPrefix(Convert.ToString(dtPassengers.DefaultView[d]["Prefix"])).Replace(".", ""); // new change
                    Name = Convert.ToString(dtPassengers.DefaultView[d]["FirstName"]).Replace(".", "");
                    SurName = Convert.ToString(dtPassengers.DefaultView[d]["LastName"]).Replace(".", "");

                    #region Comment Code
                    ////PrimaryIndicator = Convert.ToBoolean(dtPassengers.DefaultView[d]["IsLead"]);
                    //PrimaryIndicator = d == 0 ? true : false;
                    //// int AgeQualifyingCode = Convert.ToString(dtPassengers.DefaultView[d]["PaxType"]) == "Adult" ? 10 : 8;
                    //int AgeQualifyingCode;
                    //if (Convert.ToString(dtPassengers.DefaultView[d]["PaxType"]) == "Adult")
                    //    AgeQualifyingCode = 10;
                    //else
                    //    if (Convert.ToInt32(dtPassengers.DefaultView[d]["ChildAge"]) < 2)
                    //    AgeQualifyingCode = 7;
                    //else
                    //    AgeQualifyingCode = 8;
                    #endregion

                    BookRQ.Append("<m:ResGuest>");
                    BookRQ.Append("<m:Profiles>");
                    BookRQ.Append("<m:ProfileInfo>");
                    BookRQ.Append("<m:Profile>");
                    BookRQ.Append("<m:Customer>");

                    #region Comment Code
                    //if(PrimaryIndicator)
                    //{
                    //    string Email = Convert.ToString(dtPassengers.DefaultView[d]["Email"]);
                    //    BookRQ.Append("<m:Email>" + Email + "</m:Email>");
                    //}    
                    #endregion
                    BookRQ.Append("<m:PersonName>");
                    BookRQ.Append("<m:NamePrefix>" + Prefix + "</m:NamePrefix>");
                    BookRQ.Append("<m:GivenName>" + Name + "</m:GivenName>");
                    BookRQ.Append("<m:Surname>" + SurName + "</m:Surname>");
                    BookRQ.Append("</m:PersonName>");
                    BookRQ.Append("</m:Customer>");
                    BookRQ.Append("</m:Profile>");
                    BookRQ.Append("</m:ProfileInfo>");
                    BookRQ.Append("</m:Profiles>");
                    BookRQ.Append("</m:ResGuest>");
                }
                BookRQ.Append("</m:ResGuests>");
                BookRQ.Append("<m:ResGlobalInfo>");
                BookRQ.Append("<m:HotelReservationIDs>");
                BookRQ.Append("<m:HotelReservationID ResID_Value=\"" + AgentVoucherNo + "\"/>");
                BookRQ.Append("</m:HotelReservationIDs>");
                BookRQ.Append("</m:ResGlobalInfo>");
                BookRQ.Append("</m:HotelReservation>");
                BookRQ.Append("</m:HotelReservations>");
                BookRQ.Append("</m:OTA_HotelResRQ>");
                BookRQ.Append("</SOAP-ENV:Body>");
                BookRQ.Append("</SOAP-ENV:Envelope>");


                //return BookingRQ;
                #endregion

                string BookingRQ = BookRQ.ToString();
                return BookingRQ;
            }
            catch (Exception ex)
            {
                _ = _exception.Error(ex);
                return "";
            }

        }


        public async Task<ResponseDto> Get_Dingus_Response_timeout(string DingusURL, string request, string Provider, string referenceNumber, string CheckIn, string CheckOut, string Error, string HostName = "")
        {
            ResponseDto responseDto = new ResponseDto();

            System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
            timer.Start();
            string strOutput = "";

            try
            {
                HttpWebRequest Request;
                Request = (HttpWebRequest)HttpWebRequest.Create(DingusURL);
                //Request.Timeout = 12000;
                Request.Method = "POST";
                Request.ContentType = "text/xml;charset=\"utf-8\"";
                Request.KeepAlive = false;
                Request.Headers.Add("Accept-Encoding", "gzip, deflate");
                Request.Accept = "text/xml";
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
                    HttpWebResponse httpResponse = (HttpWebResponse)we.Response;
                    string result = string.Empty;
                    string result1 = string.Empty;
                    if (httpResponse != null && httpResponse.StatusDescription != null)
                    {
                        result = httpResponse.StatusDescription.ToString();
                        result1 = new StreamReader(we.Response.GetResponseStream()).ReadToEnd();
                    }
                    Error = result1;
                    _commonMethod.XMLRequestResponseTrack_HotelBeds(request, result1, Provider == "" ? "Dingus" : Provider, "Proper");
                    try
                    {
                        #region Code For Timeout
                        if (we.Message.ToLower().Contains("timeout") || (we.Message.ToLower().Contains("timed out")))
                        {
                            _commonMethod.Gettimeoutemail(referenceNumber, Provider, CheckIn, CheckOut, CheckIn, Error, HostName);
                        }
                        #endregion
                    }
                    catch (WebException ex1)
                    {

                        _ = _exception.Error(ex1);
                    }
                    //Common.XMLRequestResponseTrackError(request, result1, Provider == "" ? "Dingus" : Provider, "Proper");
                }
            }
            catch (Exception ex)
            {
                Error = ex.Message.ToString();
                _commonMethod.XMLRequestResponseTrack(request, ex.Message, Provider == "" ? "Dingus" : Provider, "Proper");
                try
                {
                    #region Code For Timeout
                    if (ex.Message.ToLower().Contains("timeout") || (ex.Message.ToLower().Contains("timed out")))
                    {
                        _commonMethod.Gettimeoutemail(referenceNumber, Provider, CheckIn, CheckOut, CheckIn, Error, HostName);
                    }
                    #endregion
                }
                catch (WebException ex1)
                {

                    _ = _exception.Error(ex1);
                }
            }
            responseDto.Response = strOutput;
            responseDto.Error = Error;
            return responseDto;

        }

        public string Get_Dingus_BookingCancelRQ(string AgentVoucher, string Username, string Password, string HotelCode, string ChainCode, string language)
        {
            try
            {
                #region Dingus  Cancel Booking request                 
                StringBuilder Dingus_CancelBook_RQ = new StringBuilder();
                Dingus_CancelBook_RQ.Append("<SOAP-ENV:Envelope xmlns:SOAP-ENV=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:SOAPENC=\"http://schemas.xmlsoap.org/soap/encoding/\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchemainstance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">");
                Dingus_CancelBook_RQ.Append("<SOAP-ENV:Body>");
                Dingus_CancelBook_RQ.Append("<m:OTA_CancelRQ xmlns:m=\"http://www.opentravel.org/OTA/2003/05\" Version=\"2.0\" Commit=\"true\" PrimaryLangID=\"" + language + "\" >");
                Dingus_CancelBook_RQ.Append("<m:POS>");
                Dingus_CancelBook_RQ.Append("<m:Source>");
                Dingus_CancelBook_RQ.Append("<m:RequestorID ID=\"" + Username + "\" MessagePassword=\"" + Password + "\" />");
                Dingus_CancelBook_RQ.Append("</m:Source>");
                Dingus_CancelBook_RQ.Append("</m:POS>");
                Dingus_CancelBook_RQ.Append("<m:BasicPropertyInfo HotelCode=\"" + HotelCode + "\" ChainCode=\"" + ChainCode + "\" VendorCode=\"\" />");
                Dingus_CancelBook_RQ.Append("<m:UniqueID ID=\"" + AgentVoucher + "\"/>");
                Dingus_CancelBook_RQ.Append("</m:OTA_CancelRQ>");
                Dingus_CancelBook_RQ.Append("</SOAP-ENV:Body>");
                Dingus_CancelBook_RQ.Append("</SOAP-ENV:Envelope>");
                string BookingDeatilsRQString = Dingus_CancelBook_RQ.ToString();
                return BookingDeatilsRQString;
                #endregion
            }
            catch (Exception ex)
            {
                _ = _exception.Error(ex);
                return "";
            }

        }
        public async Task<ResponseDto> Get_Dingus_Response_v1(string DingusURL, string request, string Provider, string referenceNumber, string CheckIn, string CheckOut, string Error, string HostName = "")
        {
            ResponseDto responseDto = new ResponseDto();
            System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
            timer.Start();

            string strOutput = "";
            try
            {
                HttpWebRequest Request;
                Request = (HttpWebRequest)HttpWebRequest.Create(DingusURL);
                //Request.Timeout = 12000;
                Request.Method = "POST";
                Request.ContentType = "text/xml;charset=\"utf-8\"";
                Request.KeepAlive = false;
                Request.Headers.Add("Accept-Encoding", "gzip, deflate");
                Request.Accept = "text/xml";
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
                    HttpWebResponse httpResponse = (HttpWebResponse)we.Response;
                    string result = string.Empty;
                    string result1 = string.Empty;
                    if (httpResponse != null && httpResponse.StatusDescription != null)
                    {
                        result = httpResponse.StatusDescription.ToString();
                        result1 = new StreamReader(we.Response.GetResponseStream()).ReadToEnd();
                    }
                    Error = result1;
                    _commonMethod.XMLRequestResponseTrack_HotelBeds(request, result1, Provider == "" ? "Dingus" : Provider, "Proper");
                    //Common.XMLRequestResponseTrackError(request, result1, Provider == "" ? "Dingus" : Provider, "Proper");

                    #region Code For Mail forbidden
                    try
                    {

                        if (we.Message.ToLower().Contains("forbidden") || result1.ToLower().Contains("forbidden"))
                        {
                            _commonMethod.Gettimeoutemail_forbidden(referenceNumber, Provider, CheckIn, CheckOut, CheckIn, Error, HostName);
                        }
                    }
                    catch (WebException ex1)
                    {
                        _ = _exception.Error(ex1);
                    }
                    #endregion
                }
            }
            catch (Exception ex)
            {
                Error = ex.Message.ToString();
                _commonMethod.XMLRequestResponseTrack(request, ex.Message, Provider == "" ? "Dingus" : Provider, "Proper");
            }

            responseDto.Response = strOutput;
            responseDto.Error = Error;
            return responseDto;

        }




    }
}
