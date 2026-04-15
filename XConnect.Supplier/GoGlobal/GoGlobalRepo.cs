using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Extensions.Options;
using MongoDB.Bson.Serialization.IdGenerators;
using XConnect.CommonUtilities;
using XConnect.CommonUtilities.Interface;
using XConnect.ErrorLogHandler.Interface;
using XConnect.Markup.Interface;
using XConnect.Models;
using XConnect.Models.Hotel;
using XConnect.Supplier.GoGlobal.Interface;
using static Azure.Core.HttpHeader;
using static XConnect.Models.ErrorLog;
using Options = XConnect.Models.Options;

namespace XConnect.Supplier.GoGlobal
{
    public class GoGlobalRepo : IGoGlobal
    {
        private readonly ICommonMethod _commonMethod;
        private readonly IException _exception;
        private readonly IMarkup _markup;
        private readonly ISupplierRes _supplierRes;
        private readonly KeySettings _keySettings;
        private readonly SupplierConfiguration _supplierConfig;//Added on 15-april-26
        public GoGlobalRepo(ICommonMethod commonMethod, IMarkup markup, ISupplierRes supplierRes, IException exception, IOptions<KeySettings> keySettings, IOptions<SupplierConfiguration> supplierConfig)
        {
            _commonMethod = commonMethod;
            _markup = markup;
            _supplierRes = supplierRes;
            _exception = exception;
            _keySettings = keySettings.Value;
            _supplierConfig = supplierConfig.Value;
        }

        public void GetGoGlobalRequestOnly_Async(DataRow[] commonCode, List<StarRating> listRating, HotelSearchParameter SearchPara, List<SupplierDetails> GGDetails, List<SupplierSearchConfig> GGSearchConfig, DataTable dtPreferredDataHotel, ref ConcurrentBag<HotelX> AllTask, string citycode, int Hit, string Nationality, string Supplier, DataTable dtROE, bool SearchByOneID, TokenData tokenData)
        {
            try
            {
                if (GGSearchConfig != null)
                {
                    for (int k = 0; k < GGSearchConfig.Count; k++)
                    {
                        bool DebugMode = (Convert.ToInt32(GGSearchConfig[k].DebugMode) == 0 ? false : true);
                        string Currency = tokenData.SiteCurrency;
                        double CurrencyFact = 1;
                        //if (Currency.ToLower() != Convert.ToString(ConfigurationManager.AppSettings["SiteCurrency"]).ToLower())
                        //{
                        //    CurrencyFact = Common.GetROEByCurrency(Currency, dtROE);
                        //}
                        if (Convert.ToInt32(GGSearchConfig[0].City) == 0 || SearchByOneID)
                        {
                            if (Convert.ToInt32(GGSearchConfig[0].CityID) == 0 || Convert.ToInt32(GGSearchConfig[0].CityID) == SearchPara.CityID)//All City means 0
                            {
                                //   List<HotelCodeByCity> DtHotelDAta = FilterFinalList(commonCode, listRating);                                

                                int Noofrecords = Convert.ToInt32(GGSearchConfig[k].Noofrecords);
                                DataTable[] splittedtables = commonCode.AsEnumerable()
                                                          .Select((row, index) => new { row, index })
                                                          .GroupBy(x => x.index / Noofrecords)  // integer division, the fractional part is truncated
                                                          .Select(g => g.Select(x => x.row).CopyToDataTable())
                                                          .ToArray();
                                for (int a = 0; a < splittedtables.Length; a++)
                                {
                                    var paramValues1 = String.Join(",", splittedtables[a].Select().Select(x => String.Join(",", Convert.ToString(x.ItemArray[0]))));
                                    string IdsForSearch = Convert.ToString(paramValues1);
                                    HotelX xhotels = new HotelX();
                                    xhotels.Supplier = Supplier;
                                    if (_keySettings.SupplierProxy == "0")
                                        xhotels.URL = Convert.ToString(GGDetails[0].WebSiteURL);
                                    else
                                        xhotels.URL = Convert.ToString(GGDetails[0].WebSiteURLforAPI);
                                    xhotels.DebugeMode = DebugMode;
                                    xhotels.SearchTimeOut = Convert.ToInt32(GGSearchConfig[k].HotelSearchTimeOut);
                                    //if (Hit > 0)
                                    xhotels.ProcessTimeout = Convert.ToInt32(GGSearchConfig[k].HotelSearchTimeOut2);
                                    xhotels.CurrencyCode = Currency;
                                    xhotels.CurrencyFactor = CurrencyFact;
                                    xhotels.XmlNationality = Nationality;
                                    xhotels.XmlCity = citycode;
                                    xhotels.XmlCountry = "";
                                    #region "CoomonCode"
                                    bool IsHitSeperate = _commonMethod.IsSuppliersWithSeperateRoomRquest(xhotels.Supplier);
                                    int NoOfRoomsSearch = 1;
                                    if (IsHitSeperate)
                                        NoOfRoomsSearch = SearchPara.Rooms.Count;
                                    List<RQRS> RQRS = new List<RQRS>();
                                    xhotels.IsSeperate = IsHitSeperate;
                                    for (int rr = 0; rr < NoOfRoomsSearch; rr++)
                                    {
                                        RQRS objrq = new RQRS();
                                        List<RoomDetails> serchRooms = new List<RoomDetails>();
                                        serchRooms.Add(SearchPara.Rooms[rr]);
                                        objrq.Request = (GGRequestOnly_Async(Convert.ToString(GGDetails[0].UserName), Convert.ToString(GGDetails[0].Password), Convert.ToString(GGDetails[0].UserID), SearchPara, IdsForSearch, false, Nationality, citycode, Currency));
                                        objrq.Response = ""; RQRS.Add(objrq);
                                    }
                                    xhotels.RQRS = RQRS;
                                    xhotels.ReturnRooms = Convert.ToInt32(GGDetails[0].ReturnRooms);
                                    xhotels.TechName = Convert.ToString(GGDetails[0].TechName);
                                    //xhotels.Action = Convert.ToString(GoGlobalStaticInfo.AvailabilitySearch);
                                    xhotels.Action = Convert.ToString(_supplierConfig.GoGlobal.AvailabilitySearch);
                                    #endregion
                                    AllTask.Add(xhotels);
                                    //break; remove on 17-DEC-2024
                                }
                            }
                        }
                        else
                        {
                            HotelX xhotels = new HotelX();
                            xhotels.Supplier = Supplier;
                            if (_keySettings.SupplierProxy == "0")
                                xhotels.URL = Convert.ToString(GGDetails[0].WebSiteURL);
                            else
                                xhotels.URL = Convert.ToString(GGDetails[0].WebSiteURLforAPI);
                            xhotels.DebugeMode = DebugMode;
                            xhotels.SearchTimeOut = Convert.ToInt32(GGSearchConfig[k].HotelSearchTimeOut);
                            //if (Hit > 0)
                            xhotels.ProcessTimeout = Convert.ToInt32(GGSearchConfig[k].HotelSearchTimeOut2);
                            xhotels.CurrencyCode = Currency;
                            xhotels.CurrencyFactor = CurrencyFact;
                            xhotels.XmlNationality = Nationality;
                            xhotels.XmlCity = citycode;
                            xhotels.XmlCountry = "";
                            #region "CoomonCode"
                            bool IsHitSeperate = _commonMethod.IsSuppliersWithSeperateRoomRquest(xhotels.Supplier);
                            int NoOfRoomsSearch = 1;
                            if (IsHitSeperate)
                                NoOfRoomsSearch = SearchPara.Rooms.Count;
                            List<RQRS> RQRS = new List<RQRS>();
                            xhotels.IsSeperate = IsHitSeperate;
                            for (int rr = 0; rr < NoOfRoomsSearch; rr++)
                            {
                                RQRS objrq = new RQRS();
                                List<RoomDetails> serchRooms = new List<RoomDetails>();
                                serchRooms.Add(SearchPara.Rooms[rr]);
                                objrq.Request = (GGRequestOnly_Async(Convert.ToString(GGDetails[0].UserName), Convert.ToString(GGDetails[0].Password), Convert.ToString(GGDetails[0].UserID), SearchPara, "", false, Nationality, citycode, Currency));
                                objrq.Response = ""; RQRS.Add(objrq);
                            }
                            xhotels.RQRS = RQRS;
                            xhotels.ReturnRooms = Convert.ToInt32(GGDetails[0].ReturnRooms);
                            xhotels.TechName = Convert.ToString(GGDetails[0].TechName);
                            //xhotels.Action = Convert.ToString(GoGlobalStaticInfo.AvailabilitySearch);
                            xhotels.Action = Convert.ToString(_supplierConfig.GoGlobal.AvailabilitySearch);

                            #endregion
                            AllTask.Add(xhotels);
                        }

                    }
                }
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
            }
        }

        public void GetGoGlobalRequestOnly_Async_new(DataRow[] commonCode, List<StarRating> listRating, HotelSearchParameter SearchPara, List<SupplierDetails> lstGGDetails, List<SupplierSearchConfig> lstGGSearchConfig, DataTable dtPreferredDataHotel, ref ConcurrentBag<HotelX> AllTask, int Hit, DataTable dtROE, bool SearchByOneID, TokenData tokenData, DataSet dtXMLCity)
        {
            try
            {
                List<StarRating> lstRating = new List<StarRating>();
                lstRating = _commonMethod.GetStarRatingStringList_xtg(SearchPara);

                List<HotelCodeByCity> DtHotelDAta = HtmlExtensions.ToList<HotelCodeByCity>(commonCode.CopyToDataTable());
                int ratingcount = lstRating.Count;
                int hotelcount = DtHotelDAta.Count;

                foreach (SupplierDetails GGDetails in lstGGDetails)
                {
                    try
                    {
                        int SupplierID = GGDetails.SupplierID;
                        string Supplier = GGDetails.SupplierName;

                        if (DtHotelDAta.Count > 0)
                        {
                            if (lstGGSearchConfig.Count > 0)
                            {
                                List<SupplierSearchConfig> GGSearchConfig = lstGGSearchConfig
                                   .Where(x => x != null && x.SupplierID == SupplierID)
                                   .ToList();

                                if (GGSearchConfig != null && GGSearchConfig.Count > 0)
                                {
                                    if ((SearchPara.Filters.IsShowRooms == 1 && SearchByOneID) || SearchPara.Filters.IsShowRooms == 0)
                                    {
                                        DataForSearch dataForSearch = _commonMethod.GetXmlDataForSearchTechwise(SearchPara.Nationality, dtXMLCity, Supplier, "goglobal");

                                        if (dataForSearch.XmlNationality != "")
                                        {
                                            List<HotelCodeByCity> filter = new List<HotelCodeByCity>();
                                            filter = _commonMethod.FilterWithOutCity_Code(ratingcount, hotelcount, DtHotelDAta, lstRating, Supplier);

                                            if (filter.Count > 0)
                                            {

                                                for (int k = 0; k < GGSearchConfig.Count; k++)
                                                {
                                                    bool DebugMode = (Convert.ToInt32(GGSearchConfig[k].DebugMode) == 0 ? false : true);
                                                    string Currency = tokenData.SiteCurrency;
                                                    double CurrencyFact = 1;
                                                    //if (Currency.ToLower() != Convert.ToString(ConfigurationManager.AppSettings["SiteCurrency"]).ToLower())
                                                    //{
                                                    //    CurrencyFact = Common.GetROEByCurrency(Currency, dtROE);
                                                    //}
                                                    if (Convert.ToInt32(GGSearchConfig[0].City) == 0 || SearchByOneID)
                                                    {
                                                        if (Convert.ToInt32(GGSearchConfig[0].CityID) == 0 || Convert.ToInt32(GGSearchConfig[0].CityID) == SearchPara.CityID)//All City means 0
                                                        {
                                                            //   List<HotelCodeByCity> DtHotelDAta = FilterFinalList(commonCode, listRating);                                

                                                            int Noofrecords = Convert.ToInt32(GGSearchConfig[k].Noofrecords);
                                                            DataTable[] splittedtables = commonCode.AsEnumerable()
                                                                                      .Select((row, index) => new { row, index })
                                                                                      .GroupBy(x => x.index / Noofrecords)  // integer division, the fractional part is truncated
                                                                                      .Select(g => g.Select(x => x.row).CopyToDataTable())
                                                                                      .ToArray();
                                                            for (int a = 0; a < splittedtables.Length; a++)
                                                            {
                                                                var paramValues1 = String.Join(",", splittedtables[a].Select().Select(x => String.Join(",", Convert.ToString(x.ItemArray[0]))));
                                                                string IdsForSearch = Convert.ToString(paramValues1);
                                                                HotelX xhotels = new HotelX();
                                                                xhotels.Supplier = Supplier;
                                                                //xhotels.URL = Convert.ToString(GGDetails.WebSiteURL);
                                                                if (_keySettings.SupplierProxy == "0")
                                                                    xhotels.URL = Convert.ToString(GGDetails.WebSiteURL);
                                                                else
                                                                    xhotels.URL = Convert.ToString(GGDetails.WebSiteURLforAPI);

                                                                xhotels.DebugeMode = DebugMode;
                                                                xhotels.SearchTimeOut = Convert.ToInt32(GGSearchConfig[k].HotelSearchTimeOut);
                                                                //if (Hit > 0)
                                                                xhotels.ProcessTimeout = Convert.ToInt32(GGSearchConfig[k].HotelSearchTimeOut2);
                                                                xhotels.CurrencyCode = Currency;
                                                                xhotels.CurrencyFactor = CurrencyFact;
                                                                xhotels.XmlNationality = dataForSearch.xmlCountry;
                                                                xhotels.XmlCity = dataForSearch.xmlCity;
                                                                xhotels.XmlCountry = "";
                                                                #region "CoomonCode"
                                                                bool IsHitSeperate = _commonMethod.IsSuppliersWithSeperateRoomRquest(xhotels.Supplier);
                                                                int NoOfRoomsSearch = 1;
                                                                if (IsHitSeperate)
                                                                    NoOfRoomsSearch = SearchPara.Rooms.Count;
                                                                List<RQRS> RQRS = new List<RQRS>();
                                                                xhotels.IsSeperate = IsHitSeperate;
                                                                for (int rr = 0; rr < NoOfRoomsSearch; rr++)
                                                                {
                                                                    RQRS objrq = new RQRS();
                                                                    List<RoomDetails> serchRooms = new List<RoomDetails>();
                                                                    serchRooms.Add(SearchPara.Rooms[rr]);
                                                                    objrq.Request = (GGRequestOnly_Async(Convert.ToString(GGDetails.UserName), Convert.ToString(GGDetails.Password), Convert.ToString(GGDetails.UserID), SearchPara, IdsForSearch, false, dataForSearch.XmlNationality, dataForSearch.xmlCity, Currency));
                                                                    objrq.Response = ""; RQRS.Add(objrq);
                                                                }
                                                                xhotels.RQRS = RQRS;
                                                                xhotels.ReturnRooms = Convert.ToInt32(GGDetails.ReturnRooms);
                                                                xhotels.TechName = Convert.ToString(GGDetails.TechName);
                                                                //xhotels.Action = Convert.ToString(GoGlobalStaticInfo.AvailabilitySearch);
                                                                xhotels.Action = Convert.ToString(_supplierConfig.GoGlobal.AvailabilitySearch);
                                                                #endregion
                                                                AllTask.Add(xhotels);
                                                                //break; remove on 17-DEC-2024
                                                            }
                                                        }
                                                    }
                                                    else
                                                    {
                                                        HotelX xhotels = new HotelX();
                                                        xhotels.Supplier = Supplier;
                                                        //xhotels.URL = Convert.ToString(GGDetails.WebSiteURL);
                                                        if (_keySettings.SupplierProxy == "0")
                                                            xhotels.URL = Convert.ToString(GGDetails.WebSiteURL);
                                                        else
                                                            xhotels.URL = Convert.ToString(GGDetails.WebSiteURLforAPI);

                                                        xhotels.DebugeMode = DebugMode;
                                                        xhotels.SearchTimeOut = Convert.ToInt32(GGSearchConfig[k].HotelSearchTimeOut);
                                                        //if (Hit > 0)
                                                        xhotels.ProcessTimeout = Convert.ToInt32(GGSearchConfig[k].HotelSearchTimeOut2);
                                                        xhotels.CurrencyCode = Currency;
                                                        xhotels.CurrencyFactor = CurrencyFact;
                                                        xhotels.XmlNationality = dataForSearch.XmlNationality;
                                                        xhotels.XmlCity = dataForSearch.xmlCity;
                                                        xhotels.XmlCountry = "";
                                                        #region "CoomonCode"
                                                        bool IsHitSeperate = _commonMethod.IsSuppliersWithSeperateRoomRquest(xhotels.Supplier);
                                                        int NoOfRoomsSearch = 1;
                                                        if (IsHitSeperate)
                                                            NoOfRoomsSearch = SearchPara.Rooms.Count;
                                                        List<RQRS> RQRS = new List<RQRS>();
                                                        xhotels.IsSeperate = IsHitSeperate;
                                                        for (int rr = 0; rr < NoOfRoomsSearch; rr++)
                                                        {
                                                            RQRS objrq = new RQRS();
                                                            List<RoomDetails> serchRooms = new List<RoomDetails>();
                                                            serchRooms.Add(SearchPara.Rooms[rr]);
                                                            objrq.Request = (GGRequestOnly_Async(Convert.ToString(GGDetails.UserName), Convert.ToString(GGDetails.Password), Convert.ToString(GGDetails.UserID), SearchPara, "", false, dataForSearch.XmlNationality, dataForSearch.xmlCity, Currency));
                                                            objrq.Response = ""; RQRS.Add(objrq);
                                                        }
                                                        xhotels.RQRS = RQRS;
                                                        xhotels.ReturnRooms = Convert.ToInt32(GGDetails.ReturnRooms);
                                                        xhotels.TechName = Convert.ToString(GGDetails.TechName);
                                                        xhotels.Action = Convert.ToString(_supplierConfig.GoGlobal.AvailabilitySearch);

                                                        #endregion
                                                        AllTask.Add(xhotels);
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
                _exception.Error(ex);
            }
        }


        public string GGRequestOnly_Async(string UserName, string Password, string UserID, HotelSearchParameter SearchParam, string hotelIds, bool DebugMode, string Nationality, string citycode, string Currency)
        {
            try
            {
                int isHit = 0;
                return GGHotelRequest(SearchParam, Nationality, UserName, Password, UserID, hotelIds, isHit, citycode, Currency);
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

        public string GGHotelRequest(HotelSearchParameter searchParam, string Nationality, string UserName, string Password, string UserID, string hotelIds, int IsHit, string Citycode, string Currency, bool IsRooom = false)
        {
            try
            {
                string checkIn = String.Format("{0:yyyy-MM-dd}", DateTime.ParseExact(searchParam.CheckInDate, "dd/MM/yyyy", null));
                string checkOut = String.Format("{0:yyyy-MM-dd}", DateTime.ParseExact(searchParam.CheckOutDate, "dd/MM/yyyy", null));
                StringBuilder roomstring = new StringBuilder();
                int RoomCount = 1;
                for (int i = 0; i < searchParam.Rooms.Count; i++)
                {
                    int TotalAdult = searchParam.Rooms[i].NoofAdults;
                    int TotalChild = searchParam.Rooms[i].NoofChild;
                    roomstring.Append("<Room Adults=\"" + TotalAdult + "\" ChildCount=\"" + TotalChild + "\" RoomCount=\"" + RoomCount + "\" >");
                    if (TotalChild >= 1)
                    {
                        switch (TotalChild)
                        {
                            case 1:
                                roomstring.Append("<ChildAge>" + Convert.ToString(searchParam.Rooms[i].Child1Age) + "</ChildAge>");
                                break;
                            case 2:
                                roomstring.Append("<ChildAge>" + Convert.ToString(searchParam.Rooms[i].Child1Age) + "</ChildAge>" + "<ChildAge>" + Convert.ToString(searchParam.Rooms[i].Child2Age) + "</ChildAge>");
                                break;
                        }
                    }
                    roomstring.Append("</Room>");
                }
                StringBuilder GG = new StringBuilder();
                GG.Append("<soap12:Envelope xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance' xmlns:xsd='http://www.w3.org/2001/XMLSchema' xmlns:soap12='http://www.w3.org/2003/05/soap-envelope'>");
                GG.Append("<soap12:Body>");
                GG.Append("<MakeRequest xmlns='http://www.goglobal.travel/'>");
                GG.Append("<requestType>11</requestType>");
                GG.Append("<xmlRequest>");
                GG.Append("<![CDATA[");
                GG.Append("<Root>");
                GG.Append("<Header>");
                GG.Append("<Agency>" + UserID + "</Agency>");
                GG.Append("<User>" + UserName + "</User>");
                GG.Append("<Password>" + Password + "</Password>");
                GG.Append("<Operation>HOTEL_SEARCH_REQUEST</Operation>");
                GG.Append("<OperationType>Request</OperationType>");
                GG.Append("</Header>");
                GG.Append("<Main Version=\"2.3\" ResponseFormat=\"JSON\" Currency=\"" + Currency + "\">");
                GG.Append("<SortOrder>1</SortOrder>");
                GG.Append("<MaximumWaitTime>2</MaximumWaitTime>");
                GG.Append("<FilterRoomBasises>");
                GG.Append("<FilterRoomBasis></FilterRoomBasis>");
                GG.Append("</FilterRoomBasises>");
                GG.Append("<Nationality>" + Nationality + "</Nationality>");
                if (hotelIds != "")
                {
                    GG.Append("<Hotels>");
                    string[] Hotelid = hotelIds.Split(',');
                    for (int i = 0; i < Hotelid.Length; i++)
                    {
                        GG.Append("<HotelId>" + Hotelid[i] + "</HotelId>");
                    }
                    GG.Append("</Hotels>");
                }
                else
                {
                    GG.Append("<CityCode>" + Citycode + "</CityCode>");
                }
                GG.Append("<ArrivalDate>" + checkIn + "</ArrivalDate>");
                GG.Append("<Nights>" + searchParam.NoofNights + "</Nights>");


                //if (!IsRooom)
                //{
                //    string[] star = searchParam.SelectedStart.Split(',');
                //    for (int i = 0; i < star.Length; i++)
                //    {
                //        GG.Append("<Stars>" + star[i] + "</Stars>");
                //    }
                //}

                GG.Append("<Rooms>");
                GG.Append(roomstring.ToString());
                GG.Append("</Rooms>");
                GG.Append("</Main>");
                GG.Append("</Root>");
                GG.Append("]]>");
                GG.Append("</xmlRequest>");
                GG.Append("</MakeRequest>");
                GG.Append("</soap12:Body>");
                GG.Append("</soap12:Envelope>");
                string REQ = GG.ToString();
                return REQ;
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
                return "";
            }
        }


        public async Task<List<Options>> GoGlobal_HotelSearchWithRooms(HotelX Item, CommonSearch commonSearch, string WebSiteURL, int SearchTimeOut, string Supplier, bool DebugeMode, string UserName, string PassWord, string MethoodType, string ResponseData, HotelSearchParameter SearchParam, string xmlcity, string xmlcountry, string xmlnationality, string currency, double currencyfactor, Int32 processTimeout, string ResProccessTimerLogActive, string ResProccessTimeValueforSaveLog)
        {
            System.Diagnostics.Stopwatch timerroomx = new System.Diagnostics.Stopwatch();
            timerroomx.Start();
            List<Options> options = new List<Options>();

            DataTable DtHotel = _commonMethod.GenerateDataTableNewV2();
            try
            {
                string JsonResponse = "";
                try
                {
                    if (ResponseData == "")
                    {
                        Item.ResponseType = "NoCount";
                        return options;
                    }
                    var rootElement = XElement.Parse(ResponseData);
                    JsonResponse = rootElement.Value;
                    if (JsonResponse.Contains("<Error"))
                    {
                        Item.ResponseType = "NoCount";
                        return options;
                    }
                }
                catch (Exception ex)
                {
                    Item.ResponseType = "NoCount";
                    return options;
                }
                GoGlobal.GoGlobalHotel GoResponse = new GoGlobal.GoGlobalHotel();
                //GoResponse = JsonConvert.DeserializeObject<GoGlobal.GoGlobalHotel>(JsonResponse);
                //GoResponse = JsonSerializer.Deserialize<GoGlobal.GoGlobalHotel>(JsonResponse);
                GoResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<GoGlobal.GoGlobalHotel>(JsonResponse);
                if (GoResponse == null || GoResponse.Hotels == null)
                {
                    Item.ResponseType = "NoCount";
                    return options;
                }
                double FinalMinPrice = 0;
                for (int i = 0; i < GoResponse.Hotels.Count; i++)
                {
                    Item.ResponseType = "Success";

                    #region "When not Need Room"  
                    // add hotel code from price only and room code blank here
                    if (SearchParam.Filters.IsShowRooms == 0)
                    {
                        #region "Hotel Code Block "

                        DataRow drXmlHotel = DtHotel.NewRow();
                        drXmlHotel["IsRecomondedHotel"] = false;
                        drXmlHotel["XMLHotelCode"] = GoResponse.Hotels[i].HotelCode;
                        drXmlHotel["HotelFrontImage"] = "";
                        drXmlHotel["HotelName"] = "";
                        drXmlHotel["Description"] = "";
                        drXmlHotel["EssentialInformation"] = "";
                        drXmlHotel["OfficeID"] = 0;
                        drXmlHotel["ContractID"] = 0;
                        drXmlHotel["IsShowSupplier"] = false;
                        drXmlHotel["HotelName"] = GoResponse.Hotels[i].HotelName;
                        drXmlHotel["Rating"] = 0;
                        string HotelStatus = string.Empty;
                        drXmlHotel["XmlHotelCity"] = GoResponse.Hotels[i].CityId;
                        drXmlHotel["XmlCity"] = GoResponse.Hotels[i].CityId;
                        drXmlHotel["XmlCountry"] = GoResponse.Hotels[i].CountryId;
                        drXmlHotel["SupplierName"] = Supplier;
                        double FinalTotal = 0;
                        FinalTotal = Convert.ToDouble(GoResponse.Hotels[i].Offers[0].TotalPrice);
                        //for (int j = 0; j < GoResponse.Hotels[i].Offers.Count; j++)
                        //{
                        //    if (j == 0)
                        //    {
                        //        FinalTotal = Convert.ToDouble(GoResponse.Hotels[i].Offers[j].TotalPrice);
                        //    }
                        //    else
                        //    {
                        //        if (Convert.ToDouble(GoResponse.Hotels[i].Offers[j].TotalPrice) < FinalTotal)
                        //        {
                        //            FinalTotal = Convert.ToDouble(GoResponse.Hotels[i].Offers[j].TotalPrice);
                        //        }
                        //    }
                        //}
                        FinalTotal = FinalTotal * currencyfactor;
                        drXmlHotel["SellingPrice"] = FinalTotal;
                        drXmlHotel["FinalSellingPrice"] = FinalTotal;
                        drXmlHotel["BookingStatus"] = HotelStatus;
                        drXmlHotel["SupplierName"] = Supplier;
                        drXmlHotel["BookingStatus"] = "Available";
                        drXmlHotel["SupplierCurrency"] = currency;
                        DtHotel.Rows.Add(drXmlHotel);

                        #endregion
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
                    #region "When Need Rooms"
                    //add Hotel code blank and rooms code in room block
                    else
                    {
                        #region Hotels
                        DataRow drXmlHotel = DtHotel.NewRow();
                        drXmlHotel["IsRecomondedHotel"] = false;
                        drXmlHotel["XMLHotelCode"] = GoResponse.Hotels[i].HotelCode;
                        drXmlHotel["HotelName"] = "";
                        drXmlHotel["ContractID"] = 0;
                        drXmlHotel["IsShowSupplier"] = false;
                        drXmlHotel["HotelName"] = GoResponse.Hotels[i].HotelName;
                        drXmlHotel["Rating"] = 0;
                        string HotelStatus = string.Empty;
                        drXmlHotel["XmlHotelCity"] = GoResponse.Hotels[i].CityId;
                        drXmlHotel["XmlCity"] = GoResponse.Hotels[i].CityId;
                        drXmlHotel["XmlCountry"] = drXmlHotel["XmlCity"] = GoResponse.Hotels[i].CountryId;
                        drXmlHotel["SupplierName"] = Supplier;
                        double FinalTotal = 0;
                        FinalTotal = Convert.ToDouble(GoResponse.Hotels[i].Offers[0].TotalPrice);
                        //for (int j = 0; j < GoResponse.Hotels[i].Offers.Count; j++)
                        //{
                        //    if (j == 0)
                        //    {
                        //        FinalTotal = Convert.ToDouble(GoResponse.Hotels[i].Offers[j].TotalPrice);
                        //    }
                        //    else
                        //    {
                        //        if (Convert.ToDouble(GoResponse.Hotels[i].Offers[j].TotalPrice) < FinalTotal)
                        //        {
                        //            FinalTotal = Convert.ToDouble(GoResponse.Hotels[i].Offers[j].TotalPrice);
                        //        }
                        //    }
                        //}
                        FinalTotal = FinalTotal * currencyfactor;
                        drXmlHotel["SellingPrice"] = FinalTotal;
                        drXmlHotel["FinalSellingPrice"] = FinalTotal;
                        drXmlHotel["BookingStatus"] = HotelStatus;
                        //drXmlHotel["SupplierName"] = "GoGlobal";
                        drXmlHotel["SupplierCurrency"] = currency;
                        DtHotel.Rows.Add(drXmlHotel);

                        double Pricenew = FinalTotal;

                        #region "GetRooms for this hotel"
                        bool willAddOrNot = true;
                        Int64 RaynaID = 0;
                        Int64 GiataId = 0;
                        int RatingHotel = 0;
                        Int64 OwnsystemHotelId = 0;
                        string Provider = Supplier;
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


                                int CutOffDays = CacheMaster.CutOffDays;
                                CutOffDays = _commonMethod.GetCutOffDays(commonSearch.dtSuplierMarkup, Supplier); //030425 added


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
                                string Currency = "";
                                //double CurrencyFactor = 0;
                                string uniqno = Guid.NewGuid().ToString().Substring(0, 4);
                                for (int j = 0; j < GoResponse.Hotels[i].Offers.Count; j++)
                                {
                                    //if (j > CacheMaster.RoomLimit_10)
                                    //break;


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

                                    string RoomTypeId = GoResponse.Hotels[i].Offers[j].HotelSearchCode;
                                    string MealPlan = GoResponse.Hotels[i].Offers[j].RoomBasis;
                                    double RoomAmount = GoResponse.Hotels[i].Offers[j].TotalPrice;
                                    //if (j == 0)
                                    //{
                                    Currency = GoResponse.Hotels[i].Offers[j].Currency;
                                    if (Currency.ToLower() != currency.ToLower())
                                        continue;
                                    //    DataTable dtInrCurrency = Common.GetCountryCurrency(Currency);
                                    //    CurrencyFactor = Convert.ToDouble(dtInrCurrency.Rows[0]["AmtToExch"]);
                                    //}
                                    //else
                                    //{
                                    //    if (Currency != GoResponse.Hotels[i].Offers[j].Currency)
                                    //    {
                                    //        DataTable dtInrCurrency = Common.GetCountryCurrency(Currency);
                                    //        CurrencyFactor = Convert.ToDouble(dtInrCurrency.Rows[0]["AmtToExch"]);
                                    //    }
                                    //}
                                    double TotalAmount = RoomAmount * currencyfactor;

                                    #region Combine Rooms Logic
                                    string RoomType_Text = "";
                                    for (int k = 0; k < GoResponse.Hotels[i].Offers[j].Rooms.Count; k++)
                                    {
                                        string RoomName = GoResponse.Hotels[i].Offers[j].Rooms[k];
                                        if (RoomType_Text == "")
                                            RoomType_Text = RoomName;
                                        else
                                            RoomType_Text = RoomType_Text + " X " + RoomName;
                                    }
                                    #endregion

                                    #region Rooms
                                    DataRow drRoom = dtHotelRoom.NewRow();
                                    drRoom["OwnsystemHotelId"] = OwnsystemHotelId;
                                    drRoom["RoomTypeCode"] = RoomTypeId;
                                    drRoom["BoardType"] = "";
                                    drRoom["MealName"] = MealPlan;
                                    drRoom["MealId"] = MealPlan;
                                    drRoom["Type"] = "";
                                    drRoom["Board"] = "";
                                    drRoom["BookingStatus"] = "Available";
                                    drRoom["UniqueNo"] = uniqno;
                                    drRoom["ContractID"] = 0;
                                    drRoom["RoomNo"] = Convert.ToInt32(1);
                                    drRoom["BoardTypeID"] = "";
                                    drRoom["AdultsOccupancy"] = totalAdult;
                                    drRoom["ChildOccupancy"] = totalChild;
                                    drRoom["OfferCode"] = "";
                                    drRoom["OfferDescription"] = "";
                                    drRoom["RoomSequenceNo"] = "";
                                    string EssentialInformation = "";
                                    drRoom["EssentialInformation"] = EssentialInformation;
                                    drRoom["Characteristic"] = "";
                                    bool IsCombine = true;
                                    bool IsDynamicPolicy = true;
                                    #region Vat
                                    _markup.ProcessHotelRoom_WithMarkup(currencyfactor, Currency, IsDynamicPolicy, IsCombine, SearchParam.NoofNights * SearchParam.Rooms.Count, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), SearchParam.CheckInDate, SearchParam.CheckOutDate, ref RoomAmount, ref drRoom, isPerce, AgentPercentage, AgentMarkupAmount, commonSearch.SubAgentMarkup, commonSearch.B2CMarkup, commonSearch.IsParentId);
                                    #endregion
                                    drRoom["SupplierName"] = Provider;
                                    drRoom["RoomTypeName"] = RoomType_Text;
                                    drRoom["RoomTypeId"] = RoomTypeId;
                                    dtHotelRoom.Rows.Add(drRoom);
                                    #endregion

                                    #region Ratebreakup
                                    //for (int n = 0; n < SearchParam.NoofNights; n++)
                                    //{
                                    //    string CheckInDate = Convert.ToString(SearchParam.CheckInDate);
                                    //    string CheckOutDate = Convert.ToString(SearchParam.CheckOutDate);
                                    //    DateTime dtCheckInDate = DateTime.ParseExact(CheckInDate, "dd/MM/yyyy", null);
                                    //    DateTime dtCheckOutDate = DateTime.ParseExact(CheckOutDate, "dd/MM/yyyy", null);
                                    //    TimeSpan t = dtCheckOutDate - dtCheckInDate;
                                    //    double ratePerNight = TotalAmount;//RoomAmount * CurrencyFactor;

                                    //    #region Vat
                                    //    double VatOnPerNight = 0;
                                    //    ratePerNight = Common.ProcessHotelRateBreakupPrice_WithMarkup(1, SearchParam.CityID, SearchParam.CountryId, ratePerNight, B2CMarkup, IsParentId, isPerce, AgentPercentage, AgentMarkupAmount, SubAgentMarkup, ref VatOnPerNight);
                                    //    #endregion

                                    //    ratePerNight = ratePerNight / SearchParam.NoofNights;
                                    //    DataRow drRateBreackUp = dtParsecRoomRateBreakup.NewRow();
                                    //    drRateBreackUp["SupplierName"] = supplier;
                                    //    drRateBreackUp["OwnsystemHotelId"] = ownsystemhotelId;
                                    //    drRateBreackUp["ContractID"] = 0;
                                    //    drRateBreackUp["RoomNo"] = Convert.ToInt32(1);
                                    //    drRateBreackUp["RoomTypeId"] = RoomTypeId;
                                    //    drRateBreackUp["MealId"] = MealPlan;
                                    //    drRateBreackUp["Meal"] = MealPlan;
                                    //    string rateWeekColumn = Convert.ToString(dtCheckInDate.Date.AddDays(n).DayOfWeek).Substring(0, 3);
                                    //    drRateBreackUp["DateOfWeek"] = rateWeekColumn;
                                    //    DateTime rateDate = dtCheckInDate.Date.AddDays(n);
                                    //    drRateBreackUp["Date"] = rateDate;
                                    //    drRateBreackUp["Rate"] = ratePerNight;
                                    //    drRateBreackUp["OrderNo"] = 1;
                                    //    drRateBreackUp["RoomTypeName"] = RoomType_Text;
                                    //    drRateBreackUp["DateOfWeek"] = rateWeekColumn;
                                    //    drRateBreackUp["Date"] = rateDate;
                                    //    drRateBreackUp["BookingStatus"] = "Available";
                                    //    drRateBreackUp["BoardTypeID"] = "";
                                    //    drRateBreackUp["VatdiffAmount"] = 0;
                                    //    drRateBreackUp["RateType"] = RoomType_Text;
                                    //    drRateBreackUp["SupplierCurrency"] = Currency;
                                    //    drRateBreackUp["BuyingPrice"] = ratePerNight / Factor;
                                    //    dtParsecRoomRateBreakup.Rows.Add(drRateBreackUp);
                                    //}
                                    #endregion

                                    #region Cancellation Policy
                                    if (true) //030425
                                    {
                                        bool IsNonRefundable = GoResponse.Hotels[i].Offers[j].NonRef;
                                        DateTime FromDate = DateTime.Now.Date;
                                        DateTime ToDate = DateTime.ParseExact(GoResponse.Hotels[i].Offers[j].CxlDeadLine, "dd/MMM/yyyy", null);
                                        ToDate = ToDate.AddDays(CutOffDays);
                                        double CancellationAmount = TotalAmount;
                                        double VatonPolicy = 0;
                                        #region Vat
                                        CancellationAmount = _markup.ProcessHotelCancelationPrice_WithMarkup(ref CancellationAmount, SearchParam.NoofNights, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), CancellationAmount, isPerce, commonSearch.B2CMarkup, AgentPercentage, AgentMarkupAmount, commonSearch.IsParentId, commonSearch.SubAgentMarkup, ref VatonPolicy);
                                        #endregion
                                        if (IsNonRefundable)
                                        {
                                            //full charge nonrefundable
                                            dtCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref CancellationAmount, dtCancellation.NewRow(), currencyfactor, Currency, Supplier, Convert.ToString(OwnsystemHotelId), (1).ToString(), RoomTypeId, RoomType_Text, Convert.ToString(MealPlan), DateTime.Now.Date, DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null), CancellationAmount, CancellationAmount, RoomAmount.ToString(), VatonPolicy, "", "", "", "", ""));
                                        }
                                        else
                                        {
                                            // refundable
                                            if (ToDate > FromDate)
                                            {
                                                double Zero = 0;
                                                dtCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref Zero, dtCancellation.NewRow(), currencyfactor, Currency, Supplier, Convert.ToString(OwnsystemHotelId), (1).ToString(), RoomTypeId, RoomType_Text, Convert.ToString(MealPlan), DateTime.Now.Date, ToDate.AddDays(-1).AddHours(23).AddMinutes(59).AddSeconds(59), 0, 0, RoomAmount.ToString(), 0, "", "", "", "", ""));
                                            }
                                            dtCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref CancellationAmount, dtCancellation.NewRow(), currencyfactor, Currency, Supplier, Convert.ToString(OwnsystemHotelId), (1).ToString(), RoomTypeId, RoomType_Text, Convert.ToString(MealPlan), ToDate, DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null), CancellationAmount, CancellationAmount, RoomAmount.ToString(), VatonPolicy, "", "", "", "", ""));
                                        }
                                    }
                                    #endregion



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
                await _exception.Error(e);
            }
            finally
            {
                #region ResProcessTimer
                try
                {
                    if (SearchParam != null && ResProccessTimerLogActive == "1" && timerroomx.ElapsedMilliseconds > Convert.ToInt32(ResProccessTimeValueforSaveLog))
                    {
                        timerroomx.Stop();
                        SearchParam.SupplierTime += ",|Finish:" + Supplier + " Process Response  " + "|Time :" + timerroomx.Elapsed.ToString();
                        _= Task.Run(() =>  _commonMethod.FireAway_SQL_ResPorcessTimeLog("", timerroomx.Elapsed.ToString(), Supplier, SearchParam.SearchKey, 1, SearchParam.AgentID));
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

        public async Task<CancellationDto> GoGlobal_cancellation(string Mealid, string roomtypecode, DataSet dsHotel, string hotelcode, string ownsystemid, string roomtypeid, DataTable dtAgentDetail, DataTable dtSuplierMarkup, DataTable dtSearchConfig, HotelSearchParameter SearchPara, string roomno, string provider, int AgentId, DataTable dtMarkupDetails, double roomprice, string GiataId = "0", int hotelRating = 0, string SearchKey = "")
        {
            string Error = "";

            CancellationDto cancellationDto = new CancellationDto();
            string IsBooking = "Available";

            try
            {
                DataTable dtGoGlobalRoomCacelationPolicy = _commonMethod.GenerateCancellationDataTableNewV2();
                int CutOffDays = XMLHotelBookingCommon.CutOffDays;
                CutOffDays = _commonMethod.GetCutOffDays(dtSuplierMarkup, provider);

                DataRow[] GGDetails = (dtSuplierMarkup.Select("SupplierName='" + provider + "'"));
                int SupplierId = Convert.ToInt32(GGDetails[0]["SupplierID"]);
                DataRow[] drSearchConfig = (dtSearchConfig.Select("SupplierID='" + SupplierId + "'"));
                int SearchTimeOut = Convert.ToInt32(drSearchConfig[0]["BookingConfirmationTimeout"]);
                string CheckIn = SearchPara.CheckInDate;
                CheckIn = CheckIn.Substring(6, 4) + "-" + CheckIn.Substring(3, 2) + "-" + CheckIn.Substring(0, 2);


                string PreBookingRequest = PriceBreakDownForCheckPrice(Convert.ToString(GGDetails[0]["UserName"]), Convert.ToString(GGDetails[0]["Password"]), Convert.ToString(GGDetails[0]["UserId"]), roomtypeid);
                _commonMethod.ThirdPartyGetPolicyHistory(Convert.ToString(roomtypeid), Convert.ToString(ownsystemid), PreBookingRequest, "-", "Before Get PriceBreakDown", "GoGlobal", "Response", "-", "");
                //string ResponseData = _commonMethod.AbreuOnlineResponse_Supplier(ref Error, PreBookingRequest, Convert.ToString(GGDetails[0]["WebSiteURL"]), Convert.ToInt32(10000), SearchKey, "goglobal", Convert.ToString(GGDetails[0]["UserId"]), "policy");
                ResponseDto responseDto = await _commonMethod.AbreuOnlineResponse_Supplier(PreBookingRequest, Convert.ToString(GGDetails[0]["WebSiteURL"]), Convert.ToInt32(10000), SearchKey, "goglobal", Convert.ToString(GGDetails[0]["UserId"]), "policy");
                string ResponseData = responseDto.Response;
                Error = responseDto.Error;

                _commonMethod.ThirdPartyGetPolicyHistory(Convert.ToString(roomtypeid), Convert.ToString(ownsystemid), PreBookingRequest, Convert.ToString(ResponseData + Error), "After Get PriceBreakDown", "GoGlobal", "Response", "-", "");

                string decodedXml = "";
                string Currency = "";
                double itemprice = 0;
                if (ResponseData != null)
                {
                    decodedXml = ResponseData.Replace("&amp;", "&").Replace("&lt;", "<").Replace("&gt;", ">").Replace("&quot;", "\"").Replace("&apos;", " ").Replace("<?xml version=\"1.0\" encoding=\"UTF-8\"?>", "");
                    ResponseData = decodedXml;
                    DataSet GGData = new DataSet();
                    GGData = _commonMethod.GetDataSetFromXMLString(ResponseData);
                    if (GGData.Tables.Contains("PriceBreakdown"))
                    {
                        double CurrencyFactor = 0;
                        for (int j = 0; j < GGData.Tables["PriceBreakdown"].Rows.Count; j++)
                        {
                            if (j == 0)
                            {
                                Currency = Convert.ToString(GGData.Tables["PriceBreakdown"].Rows[j]["Currency"]);
                                DataTable dtInrCurrency = _commonMethod.GetCountryCurrency(Currency);
                                CurrencyFactor = Convert.ToDouble(dtInrCurrency.Rows[j]["AmtToExch"]);
                            }
                            double Price = Convert.ToDouble(GGData.Tables["PriceBreakdown"].Rows[j]["Price"]) * CurrencyFactor;
                            itemprice = itemprice + Price;
                        }
                    }
                }


                if (itemprice == 0)
                {
                    itemprice = roomprice;
                }

                if (itemprice <= roomprice)
                {
                    itemprice = roomprice;
                }
                string CheckIn1 = String.Format("{0:yyyy-MM-dd}", DateTime.ParseExact(SearchPara.CheckInDate, "dd/MM/yyyy", null));
                string HotelSearchCode = roomtypeid;

                string ErrorResponse = "";
                string Request = BookingValueationPolicy(Convert.ToString(GGDetails[0]["UserName"]), Convert.ToString(GGDetails[0]["Password"]), Convert.ToString(GGDetails[0]["UserId"]), CheckIn1, HotelSearchCode);
                _commonMethod.ThirdPartyGetPolicyHistory(Convert.ToString(roomtypeid), Convert.ToString(ownsystemid), Request, Convert.ToString(ErrorResponse), "After Get Policy", "GoGlobal", "Response", "-", "");
                //string Response = _commonMethod.AbreuOnlineResponse_Supplier(ref ErrorResponse, Request, Convert.ToString(GGDetails[0]["WebSiteURL"]), Convert.ToInt32(10000), SearchKey, "goglobal", Convert.ToString(GGDetails[0]["UserId"]), "checkprice");
                ResponseDto responseDto1 = await _commonMethod.AbreuOnlineResponse_Supplier(Request, Convert.ToString(GGDetails[0]["WebSiteURL"]), Convert.ToInt32(10000), SearchKey, "goglobal", Convert.ToString(GGDetails[0]["UserId"]), "checkprice");
                string Response = responseDto1.Response;
                ErrorResponse = responseDto1.Error;



                _commonMethod.ThirdPartyGetPolicyHistory(Convert.ToString(roomtypeid), Convert.ToString(ownsystemid), Request, Convert.ToString(Response + ErrorResponse), "After Get Policy", "GoGlobal", "Response", "-", "");
                decodedXml = Response.Replace("&amp;", "&").Replace("&lt;", "<").Replace("&gt;", ">").Replace("&quot;", "\"").Replace("&apos;", " ").Replace("<?xml version=\"1.0\" encoding=\"UTF-8\"?>", "");

                DataSet GGVData = new DataSet();
                GGVData = _commonMethod.GetDataSetFromXMLString(decodedXml);
                bool chechk = false;
                string EssentialInformation = "";
                string Enn = "";
                try
                {
                    string additionalInfo = "";
                    for (int j = 0; j < GGVData.Tables["main"].Rows.Count; j++)
                    {
                        additionalInfo = Convert.ToString(GGVData.Tables["main"].Rows[j]["Remarks"]);
                    }
                    string Essen1 = additionalInfo;
                    string noHTML = Regex.Replace(Essen1, @"<[^>]+>|&nbsp;", "").Trim();
                    string noHTMLNormalised = Regex.Replace(noHTML, @"\s{2,}", " ");
                    string[] arr = noHTMLNormalised.Split('.');

                    for (int k = 0; k < arr.Length; k++)
                    {
                        Enn = arr[k];
                        if (Enn.ToLower().Contains("cxl"))
                        {
                            continue;
                        }
                        EssentialInformation = EssentialInformation + " " + Enn;
                    }
                }
                catch (Exception ex)
                {
                    IsBooking = "NotAvailable";
                    _ = _exception.ErrorNew(ex, _LogAPI.Recheck, SearchKey);
                }



                for (int j = 0; j < GGVData.Tables["main"].Rows.Count; j++)
                {
                    DateTime FromDate = DateTime.Now.Date;
                    string cxldeadline = Convert.ToString(GGVData.Tables["main"].Rows[j]["cancellationDeadline"]).Replace("-", "/");
                    DateTime ToDate = DateTime.ParseExact(cxldeadline, "yyyy/MM/dd", null);// "dd/MMM/yyyy", null);

                    double cancellationChargeAmount = 0;
                    double MainAgentCharge = 0;
                    double CompanyChargeAmount = 0;

                    if (itemprice == roomprice)
                    {
                        cancellationChargeAmount = roomprice;
                        MainAgentCharge = cancellationChargeAmount;
                        CompanyChargeAmount = cancellationChargeAmount;
                    }
                    else
                    {
                        cancellationChargeAmount = itemprice;
                        #region Vat
                        Boolean isPerce = true;
                        double AgentCompanyPercentage = Convert.ToDouble(SearchPara.AgentCompanyPercentage);
                        bool IsB2C = Convert.ToBoolean(Convert.ToByte(SearchPara.IsB2BOrB2C));
                        double B2CMarkup = (IsB2C == false ? AgentCompanyPercentage : XMLHotelBookingCommon.B2CMarkup);
                        double AgentPercentage = 0.0;
                        double AgentMarkupAmount = 0.0;
                        double SubAgentMarkup = 0;
                        int IsParentId = 1;
                        Boolean blnIsShowSupplier = false;
                        //Int32 hotelRating = 0;
                        //hotelRating = Convert.ToInt32(dsHotel.Tables[0].Rows[0]["Rating"]);
                        _markup.getAgetMarkupSetting(provider, GiataId, dtSuplierMarkup, dtMarkupDetails, ref AgentPercentage, ref AgentMarkupAmount, ref blnIsShowSupplier, ref isPerce, AgentId, hotelRating, SearchPara.CityID, Convert.ToString(SearchPara.CountryID), SearchPara.CheckInDate);
                        if (dtAgentDetail != null)
                        {
                            IsParentId = Convert.ToInt32(dtAgentDetail.Rows[0]["IsParentId"]);
                            SubAgentMarkup = Convert.ToDouble(dtAgentDetail.Rows[0]["B2CMarkupPer"]);
                        }
                        double VatOnPolicy = 0;
                        MainAgentCharge = cancellationChargeAmount;
                        CompanyChargeAmount = cancellationChargeAmount;
                        cancellationChargeAmount = _markup.ProcessHotelCancelationPrice_WithMarkup(ref MainAgentCharge, SearchPara.Rooms.Count * SearchPara.NoofNights, SearchPara.CityID, Convert.ToString(SearchPara.CountryID), cancellationChargeAmount, isPerce, B2CMarkup, AgentPercentage, AgentMarkupAmount, IsParentId, SubAgentMarkup, ref VatOnPolicy);
                        #endregion
                    }



                    ToDate = ToDate.AddDays(CutOffDays);
                    if (FromDate < ToDate && chechk == false)
                    {
                        double zero = 0;
                        dtGoGlobalRoomCacelationPolicy.Rows.Add(_commonMethod.setCancellationPolicyRow(ref zero, dtGoGlobalRoomCacelationPolicy.NewRow(), 1, Currency, provider, Convert.ToString(ownsystemid), (1).ToString(), roomtypeid, "", Mealid /*Convert.ToString(roomtypecode)*/, DateTime.Now.Date, ToDate.AddDays(-1).AddHours(23).AddMinutes(59).AddSeconds(59), 0, 0, "", 0, "", "", "", EssentialInformation, "", ""));
                        dtGoGlobalRoomCacelationPolicy.AcceptChanges();
                        chechk = true;
                    }
                    if (FromDate > ToDate)
                        ToDate = DateTime.Now.Date;

                    DateTime CheckInDate = Convert.ToDateTime(CheckIn);
                    dtGoGlobalRoomCacelationPolicy.Rows.Add(_commonMethod.setCancellationPolicyRow(ref CompanyChargeAmount, dtGoGlobalRoomCacelationPolicy.NewRow(), 1, Currency, provider, Convert.ToString(ownsystemid), (1).ToString(), roomtypeid, "", Mealid/* Convert.ToString(roomtypecode)*/, ToDate, CheckInDate, MainAgentCharge, cancellationChargeAmount, "", 0, "", "", "", EssentialInformation, "", ""));
                    dtGoGlobalRoomCacelationPolicy.AcceptChanges();

                    dsHotel.Tables[2].Merge(dtGoGlobalRoomCacelationPolicy);
                }
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

        public string PriceBreakDownForCheckPrice(string UserName, string Password, string UserId, string InternalReference)
        {
            StringBuilder Request = new StringBuilder();
            try
            {
                Request.Append("<soap12:Envelope xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance' xmlns:xsd='http://www.w3.org/2001/XMLSchema' xmlns:soap12='http://www.w3.org/2003/05/soap-envelope'>");
                Request.Append("<soap12:Body>");
                Request.Append("<MakeRequest xmlns='http://www.goglobal.travel/'>");
                Request.Append("<requestType>14</requestType>");
                Request.Append("<xmlRequest>");
                Request.Append("<![CDATA[");
                Request.Append("<Root>");
                Request.Append("<Header>");
                Request.Append("<Agency>" + UserId + "</Agency>");
                Request.Append("<User>" + UserName + "</User>");
                Request.Append("<Password>" + Password + "</Password>");
                Request.Append("<Operation>PRICE_BREAKDOWN_REQUEST</Operation>");
                Request.Append("<OperationType>Request</OperationType>");
                Request.Append("</Header>");
                Request.Append("<Main>");
                Request.Append("<HotelSearchCode>" + InternalReference + "</HotelSearchCode>");
                Request.Append("</Main>");
                Request.Append("</Root>");
                Request.Append("]]>");
                Request.Append("</xmlRequest>");
                Request.Append("</MakeRequest>");
                Request.Append("</soap12:Body>");
                Request.Append("</soap12:Envelope>");
                string REQ = Request.ToString();
                return REQ;
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
                return null;
            }
            return null;
        }

        public string BookingValueationPolicy(string UserName, string Password, string UserId, string CheckIn, string HotelSearchCode)
        {
            StringBuilder Request = new StringBuilder();
            string BookingREQ = "";
            try
            {
                
                Request.Append("<soap12:Envelope xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance' xmlns:xsd='http://www.w3.org/2001/XMLSchema' xmlns:soap12='http://www.w3.org/2003/05/soap-envelope'>");
                Request.Append("<soap12:Body>");
                Request.Append("<MakeRequest xmlns='http://www.goglobal.travel/'>");
                Request.Append("<requestType>9</requestType>");
                Request.Append("<xmlRequest>");
                Request.Append("<![CDATA[");
                Request.Append("<Root>");
                Request.Append("<Header>");
                Request.Append("<Agency>" + UserId + "</Agency>");
                Request.Append("<User>" + UserName + "</User>");
                Request.Append("<Password>" + Password + "</Password>");
                Request.Append("<Operation>BOOKING_VALUATION_REQUEST</Operation>");
                Request.Append("<OperationType>Request</OperationType>");
                Request.Append("</Header>");
                Request.Append("<Main>");
                Request.Append("<HotelSearchCode>" + HotelSearchCode + "</HotelSearchCode>");
                Request.Append("<ArrivalDate>" + CheckIn + "</ArrivalDate>");
                Request.Append("</Main>");
                Request.Append("</Root>");
                Request.Append("]]>");
                Request.Append("</xmlRequest>");
                Request.Append("</MakeRequest>");
                Request.Append("</soap12:Body>");
                Request.Append("</soap12:Envelope>");
                BookingREQ = Request.ToString();
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
                return BookingREQ;
            }
            return BookingREQ;
        }

        public async Task<PriceCheckCMDto> PriceCheckGoGlobal(int i, DataTable dtCheckPriceReqStructure, DataSet dsCartDetails, DataRow[] GGDetails, string UniqueNo, string AgentUniqueNo, string IsFor, bool IsError, string supplier, double CompanyBuyPrice, double itemprice, bool PolicyChange, int AgentId = 0, string SearchKey = "")
        {
            PriceCheckCMDto priceCheckCMDto = new PriceCheckCMDto();
            bool PriceUpdate = false;
            string responsetype = "Success";
            string time = "";
            string SupplierName = supplier;
            string Error = "";
            string Request = "";
            string Response = "";
            try
            {
                string SupplierCurrency = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["SupplierCurrency"]);

                string CheckIn = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["CheckInDate"]);
                CheckIn = CheckIn.Substring(0, 4) + "-" + CheckIn.Substring(4, 2) + "-" + CheckIn.Substring(6, 2);
                string HotelSearchCode = Convert.ToString(dsCartDetails.Tables[1].DefaultView[0]["RoomTypeID"]);

                Request = BookingValueationPolicy(Convert.ToString(GGDetails[0]["UserName"]), Convert.ToString(GGDetails[0]["Password"]), Convert.ToString(GGDetails[0]["UserId"]), CheckIn, HotelSearchCode);

                _commonMethod.ThirdPartyBookingHistory(Convert.ToString(UniqueNo), Convert.ToString(AgentUniqueNo), Request, "-", "Before Request BookingValueation (After Price Check BookingTime)", "GoGlobal", "Request Confirm", " - ", "RaynaB2B");
                //string Response = XMLHotelBookingCommon.AbreuOnlineResponse_Supplier(Request, Convert.ToString(GGDetails[0]["WebSiteURL"]), Convert.ToInt32(120000));

                System.Diagnostics.Stopwatch timerroomprocess = new System.Diagnostics.Stopwatch();
                timerroomprocess.Start();

                //Response = _commonMethod.AbreuOnlineResponse_Supplier_report(Request, Convert.ToString(GGDetails[0]["WebSiteURL"]), Convert.ToInt32(120000),"", "goglobal", Convert.ToString(GGDetails[0]["UserId"]), "checkprice");
                ResponseDto responseDto1 = await _commonMethod.AbreuOnlineResponse_Supplier(Request, Convert.ToString(GGDetails[0]["WebSiteURL"]), Convert.ToInt32(120000), SearchKey, "goglobal", Convert.ToString(GGDetails[0]["UserId"]), "checkprice");
                Response = responseDto1.Response;
                Error = responseDto1.Error;

                timerroomprocess.Stop();
                time = timerroomprocess.Elapsed.ToString();

                _commonMethod.ThirdPartyBookingHistory(Convert.ToString(UniqueNo), Convert.ToString(AgentUniqueNo), Request, Response + Error, "After Request BookingValueation (After Price Check BookingTime)", "GoGlobal", "Response Confirm", "-", "RaynaB2B");


                if (Response != null)
                {
                    string decodedXml = Response.Replace("&amp;", "&").Replace("&lt;", "<").Replace("&gt;", ">").Replace("&quot;", "\"").Replace("&apos;", " ").Replace("<?xml version=\"1.0\" encoding=\"UTF-8\"?>", "");
                    string ResponseData = decodedXml;
                    DataSet GGVData = new DataSet();
                    GGVData = _commonMethod.GetDataSetFromXMLString(ResponseData);
                    double FinalRoomRate = 0;
                    if (GGVData.Tables.Contains("Main"))
                    {
                        if (GGVData.Tables["Main"].Columns.Contains("rates"))
                        {
                            if (GGVData.Tables["Main"].Rows.Count > 0)
                            {
                                string Rates = Convert.ToString(GGVData.Tables["Main"].Rows[0]["rates"]);
                                if (Rates != "")
                                {
                                    Rates = Rates.Replace(" ", ",");
                                    FinalRoomRate = Convert.ToDouble(Rates.Split(',')[0]);
                                    if (Rates.Split(',')[1] != SupplierCurrency)
                                    {
                                        PriceUpdate = true;
                                        itemprice = 0;
                                    }
                                    else
                                    {
                                        PriceUpdate = true;
                                        itemprice = FinalRoomRate;
                                    }
                                }
                                else
                                {
                                    PriceUpdate = false;
                                    itemprice = CompanyBuyPrice;
                                }
                            }
                            if (IsFor == "P")
                            {
                                if (Convert.ToDouble((itemprice).TruncateDecimalPlaces(0)) > (CompanyBuyPrice).TruncateDecimalPlaces(0))
                                {
                                    Response = "#Price Changed " + "#Main Price = " + CompanyBuyPrice + " #New Price = " + itemprice + " #Response= " + Response;
                                    PriceUpdate = true;
                                    itemprice = FinalRoomRate;
                                }
                            }
                            else
                            {
                                if (Convert.ToDouble((itemprice).TruncateDecimalPlaces(0)) != (CompanyBuyPrice).TruncateDecimalPlaces(0))
                                {
                                    Response = "#Price Changed " + "#Main Price = " + CompanyBuyPrice + " #New Price = " + itemprice + " #Response= " + Response;
                                    PriceUpdate = true;
                                    itemprice = 0;
                                }
                            }
                        }
                        else
                        {
                            PriceUpdate = true;
                            itemprice = 0;
                        }
                    }
                    else
                    {
                        PriceUpdate = true;
                        itemprice = 0;
                    }
                }
                else
                {
                    try
                    {
                        responsetype = "GeneralError";
                        string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, Request, Response, Error);
                    }
                    catch (Exception e)
                    {
                        _ = _exception.Error(e, SearchKey);
                    }
                    IsError = true;
                    //return true;

                    priceCheckCMDto.ItemPrice = itemprice;
                    priceCheckCMDto.IsError = IsError;
                    priceCheckCMDto.Flag = true;
                    return priceCheckCMDto;
                }
            }
            catch (Exception ex)
            {
                try
                {
                    Error = ex.Message + ex.StackTrace.ToString();
                    responsetype = "GeneralError";
                    string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, Request, Response, Error);
                }
                catch (Exception e)
                {
                    _ = _exception.Error(e, SearchKey);
                }
                _ = _exception.Error(ex, SearchKey);
                IsError = true;
            }
            if (PriceUpdate)
            {

                try
                {
                    responsetype = "GeneralError";
                    string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, Request, Response, Error);
                }
                catch (Exception e)
                {
                    _ = _exception.Error(e, SearchKey);
                }
            }
            else
            {
                try
                {
                    responsetype = "Success";
                    string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, Request, Response, Error);
                }
                catch (Exception e)
                {
                    _ = _exception.Error(e, SearchKey);
                }
            }
            priceCheckCMDto.ItemPrice = itemprice;
            priceCheckCMDto.IsError = IsError;
            priceCheckCMDto.Flag = PriceUpdate;
            return priceCheckCMDto;
        }

        public string GoGlobalBooking(int i, DataTable dtPassengers, DataSet dsCartDetails, string UserName, string Password, string UserId, string UniqueNo, string AgentCode, string SpecialReqToPass)
        {
            StringBuilder Request = new StringBuilder();
            string BookingREQ = "";
            try
            {
                string InternalReference = "Book_" + AgentCode + "_" + UniqueNo;
                Request.Append("<soap12:Envelope xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance' xmlns:xsd='http://www.w3.org/2001/XMLSchema' xmlns:soap12='http://www.w3.org/2003/05/soap-envelope'>");
                Request.Append("<soap12:Body>");
                Request.Append("<MakeRequest xmlns='http://www.goglobal.travel/'>");
                Request.Append("<requestType>2</requestType>");
                Request.Append("<xmlRequest>");
                Request.Append("<![CDATA[");
                Request.Append("<Root>");
                Request.Append("<Header>");
                Request.Append("<Agency>" + UserId + "</Agency>");
                Request.Append("<User>" + UserName + "</User>");
                Request.Append("<Password>" + Password + "</Password>");
                Request.Append("<Operation>BOOKING_INSERT_REQUEST</Operation>");
                Request.Append("<OperationType>Request</OperationType>");
                Request.Append("</Header>");
                //Request.Append("<Main Version=\"2.0\">");
                Request.Append("<Main Version=\"2.3\">");

                Request.Append("<AgentReference>" + InternalReference + "</AgentReference>");

                for (int j = 0; j < dsCartDetails.Tables[1].DefaultView.Count; j++)
                {
                    int RoomNo = Convert.ToInt32(j + 1);
                    dtPassengers.DefaultView.RowFilter = "PickUp='" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomTypeId"]) + "' and UserCartID='" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["UserCartID"]) + "' and RoomNo='" + RoomNo + "'";
                    if (j == 0)
                    {
                        string CheckIn = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["CheckInDate"]);
                        CheckIn = CheckIn.Substring(0, 4) + "-" + CheckIn.Substring(4, 2) + "-" + CheckIn.Substring(6, 2);
                        string CheckOut = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["CheckOutDate"]);
                        CheckOut = CheckOut.Substring(0, 4) + "-" + CheckOut.Substring(4, 2) + "-" + CheckOut.Substring(6, 2);
                        DateTime StartDate = DateTime.ParseExact(CheckIn, "yyyy-MM-dd", null);
                        DateTime EndDate = DateTime.ParseExact(CheckOut, "yyyy-MM-dd", null);
                        double NumberOfNights = (EndDate - StartDate).TotalDays;
                        string HotelSearchCode = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomTypeID"]);
                        Request.Append("<HotelSearchCode>" + HotelSearchCode + "</HotelSearchCode>");
                        Request.Append("<ArrivalDate>" + CheckIn + "</ArrivalDate>");
                        Request.Append("<Nights>" + NumberOfNights + "</Nights>");
                        Request.Append("<NoAlternativeHotel>1</NoAlternativeHotel>");
                        Request.Append("<Leader LeaderPersonID=\"" + 1 + "\"/>");
                        Request.Append("<Rooms>");
                    }
                    DataTable dtnewpassanger = dtPassengers.DefaultView.ToTable();
                    DataRow[] TTLAdult = dtnewpassanger.Select("PaxType='Adult'");
                    Request.Append("<RoomType Adults=\"" + Convert.ToInt32(TTLAdult.Length) + "\">");
                    Request.Append("<Room RoomID=\"" + RoomNo + "\">");
                    for (int b = 0; b < dtnewpassanger.Rows.Count; b++)
                    {
                        if (Convert.ToString(dtnewpassanger.Rows[b]["PaxType"]) == "Adult")
                        {
                            Request.Append("<PersonName PersonID=\"" + (b + 1) + "\" Title=\"" + Convert.ToString(dtnewpassanger.Rows[b]["Prefix"]) + "\" FirstName=\"" + Convert.ToString(dtnewpassanger.Rows[b]["FirstName"]) + "\" LastName=\"" + Convert.ToString(dtnewpassanger.Rows[b]["LastName"]) + "\"/>");
                        }
                        else if (Convert.ToString(dtnewpassanger.Rows[b]["PaxType"]) == "Child")
                        {
                            int childage = Convert.ToInt32(dtnewpassanger.Rows[b]["ChildAge"]);
                            Request.Append("<ExtraBed PersonID=\"" + (b + 1) + "\" FirstName=\"" + Convert.ToString(dtnewpassanger.Rows[b]["FirstName"]) + "\" LastName=\"" + Convert.ToString(dtnewpassanger.Rows[b]["LastName"]) + "\" ChildAge=\"" + childage + "\"/>");
                        }
                    }
                    Request.Append("</Room>");
                    Request.Append("</RoomType>");
                }
                Request.Append("</Rooms>");
                string SpecialReq = "";
                try
                {
                    if (!string.IsNullOrEmpty(SpecialReqToPass))
                        Request.Append("<Remark>" + SpecialReqToPass + "</Remark>");
                }
                catch (Exception ex)
                {
                    _ = _exception.ErrorNew(ex, _LogAPI.Book);
                }
                Request.Append("</Main>");
                Request.Append("</Root>");
                Request.Append("]]>");
                Request.Append("</xmlRequest>");
                Request.Append("</MakeRequest>");
                Request.Append("</soap12:Body>");
                Request.Append("</soap12:Envelope>");
                BookingREQ = Request.ToString();
            }
            catch (Exception ex)
            {
                _ = _exception.ErrorNew(ex, _LogAPI.Book);
                return BookingREQ;
            }
            return BookingREQ;
        }

        public string GoGlobalBooking_newversion(int i, DataTable dtPassengers, BookRequest_newversion bookRequest, string UserName, string Password, string UserId, string UniqueNo, string AgentCode, string SpecialReqToPass)
        {
            StringBuilder Request = new StringBuilder();
            string BookingREQ = "";
            try
            {
                string InternalReference = "Book_" + AgentCode + "_" + UniqueNo;
                Request.Append("<soap12:Envelope xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance' xmlns:xsd='http://www.w3.org/2001/XMLSchema' xmlns:soap12='http://www.w3.org/2003/05/soap-envelope'>");
                Request.Append("<soap12:Body>");
                Request.Append("<MakeRequest xmlns='http://www.goglobal.travel/'>");
                Request.Append("<requestType>2</requestType>");
                Request.Append("<xmlRequest>");
                Request.Append("<![CDATA[");
                Request.Append("<Root>");
                Request.Append("<Header>");
                Request.Append("<Agency>" + UserId + "</Agency>");
                Request.Append("<User>" + UserName + "</User>");
                Request.Append("<Password>" + Password + "</Password>");
                Request.Append("<Operation>BOOKING_INSERT_REQUEST</Operation>");
                Request.Append("<OperationType>Request</OperationType>");
                Request.Append("</Header>");
                //Request.Append("<Main Version=\"2.0\">");
                Request.Append("<Main Version=\"2.3\">");

                Request.Append("<AgentReference>" + InternalReference + "</AgentReference>");

                for (int j = 0; j < bookRequest.userCart.UserCartDetails.Count; j++)
                {
                    int RoomNo = Convert.ToInt32(j + 1);
                    dtPassengers.DefaultView.RowFilter = "PickUp='" + Convert.ToString(bookRequest.userCart.UserCartDetails[j].RoomTypeId) + "' and UserCartID='" + Convert.ToString(bookRequest.userCart.UserCartDetails[j].UserCartId) + "' and RoomNo='" + RoomNo + "'";
                    if (j == 0)
                    {
                        string CheckIn = Convert.ToString(bookRequest.userCart.UserCartDetails[j].CheckInDate);
                        CheckIn = CheckIn.Substring(0, 4) + "-" + CheckIn.Substring(4, 2) + "-" + CheckIn.Substring(6, 2);
                        string CheckOut = Convert.ToString(bookRequest.userCart.UserCartDetails[j].CheckOutDate);
                        CheckOut = CheckOut.Substring(0, 4) + "-" + CheckOut.Substring(4, 2) + "-" + CheckOut.Substring(6, 2);
                        DateTime StartDate = DateTime.ParseExact(CheckIn, "yyyy-MM-dd", null);
                        DateTime EndDate = DateTime.ParseExact(CheckOut, "yyyy-MM-dd", null);
                        double NumberOfNights = (EndDate - StartDate).TotalDays;
                        string HotelSearchCode = Convert.ToString(bookRequest.userCart.UserCartDetails[j].RoomTypeId);
                        Request.Append("<HotelSearchCode>" + HotelSearchCode + "</HotelSearchCode>");
                        Request.Append("<ArrivalDate>" + CheckIn + "</ArrivalDate>");
                        Request.Append("<Nights>" + NumberOfNights + "</Nights>");
                        Request.Append("<NoAlternativeHotel>1</NoAlternativeHotel>");
                        Request.Append("<Leader LeaderPersonID=\"" + 1 + "\"/>");
                        Request.Append("<Rooms>");
                    }
                    DataTable dtnewpassanger = dtPassengers.DefaultView.ToTable();
                    DataRow[] TTLAdult = dtnewpassanger.Select("PaxType='Adult'");
                    Request.Append("<RoomType Adults=\"" + Convert.ToInt32(TTLAdult.Length) + "\">");
                    Request.Append("<Room RoomID=\"" + RoomNo + "\">");
                    for (int b = 0; b < dtnewpassanger.Rows.Count; b++)
                    {
                        if (Convert.ToString(dtnewpassanger.Rows[b]["PaxType"]) == "Adult")
                        {
                            Request.Append("<PersonName PersonID=\"" + (b + 1) + "\" Title=\"" + Convert.ToString(dtnewpassanger.Rows[b]["Prefix"]) + "\" FirstName=\"" + Convert.ToString(dtnewpassanger.Rows[b]["FirstName"]) + "\" LastName=\"" + Convert.ToString(dtnewpassanger.Rows[b]["LastName"]) + "\"/>");
                        }
                        else if (Convert.ToString(dtnewpassanger.Rows[b]["PaxType"]) == "Child")
                        {
                            int childage = Convert.ToInt32(dtnewpassanger.Rows[b]["ChildAge"]);
                            Request.Append("<ExtraBed PersonID=\"" + (b + 1) + "\" FirstName=\"" + Convert.ToString(dtnewpassanger.Rows[b]["FirstName"]) + "\" LastName=\"" + Convert.ToString(dtnewpassanger.Rows[b]["LastName"]) + "\" ChildAge=\"" + childage + "\"/>");
                        }
                    }
                    Request.Append("</Room>");
                    Request.Append("</RoomType>");
                }
                Request.Append("</Rooms>");
                string SpecialReq = "";
                try
                {
                    if (!string.IsNullOrEmpty(SpecialReqToPass))
                        Request.Append("<Remark>" + SpecialReqToPass + "</Remark>");
                }
                catch (Exception ex)
                {
                    _ = _exception.ErrorNew(ex, _LogAPI.Book);
                }
                Request.Append("</Main>");
                Request.Append("</Root>");
                Request.Append("]]>");
                Request.Append("</xmlRequest>");
                Request.Append("</MakeRequest>");
                Request.Append("</soap12:Body>");
                Request.Append("</soap12:Envelope>");
                BookingREQ = Request.ToString();
            }
            catch (Exception ex)
            {
                _ = _exception.ErrorNew(ex, _LogAPI.Book);
                return BookingREQ;
            }
            return BookingREQ;
        }
        public string VoucherBooking(string UserName, string Password, string UserId, string BookingCode)
        {
            StringBuilder Request = new StringBuilder();
            string BookingREQ = "";
            try
            {
                Request.Append("<soap12:Envelope xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance' xmlns:xsd='http://www.w3.org/2001/XMLSchema' xmlns:soap12='http://www.w3.org/2003/05/soap-envelope'>");
                Request.Append("<soap12:Body>");
                Request.Append("<MakeRequest xmlns='http://www.goglobal.travel/'>");
                Request.Append("<requestType>8</requestType>");
                Request.Append("<xmlRequest>");
                Request.Append("<![CDATA[");
                Request.Append("<Root>");
                Request.Append("<Header>");
                Request.Append("<Agency>" + UserId + "</Agency>");
                Request.Append("<User>" + UserName + "</User>");
                Request.Append("<Password>" + Password + "</Password>");
                Request.Append("<Operation>VOUCHER_DETAILS_REQUEST</Operation>");
                Request.Append("<OperationType>Request</OperationType>");
                Request.Append("</Header>");
                Request.Append("<Main Version=\"2.0\">");
                Request.Append("<GoBookingCode>" + BookingCode + "</GoBookingCode>");
                Request.Append("<GetEmergencyPhone>true</GetEmergencyPhone>");
                Request.Append("</Main>");
                Request.Append("</Root>");
                Request.Append("]]>");
                Request.Append("</xmlRequest>");
                Request.Append("</MakeRequest>");
                Request.Append("</soap12:Body>");
                Request.Append("</soap12:Envelope>");
                BookingREQ = Request.ToString();
            }
            catch (Exception ex)
            {
                _ = _exception.Error(ex);
                return BookingREQ;
            }
            return BookingREQ;
        }

        public string GOGlobalBookingStatusRequest(string Booking_Ref, string UserName, string Password, string UserId)
        {
            string TLREQ = "";
            try
            {
                StringBuilder GG = new StringBuilder();
                // Booking Status
                GG.Append("<soap12:Envelope xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance' xmlns:xsd='http://www.w3.org/2001/XMLSchema' xmlns:soap12='http://www.w3.org/2003/05/soap-envelope'>");
                GG.Append("<soap12:Body>");
                GG.Append("<MakeRequest xmlns='http://www.goglobal.travel/'>");
                GG.Append("<requestType>5</requestType>");
                GG.Append("<xmlRequest>");
                GG.Append("<![CDATA[");
                GG.Append("<Root>");
                GG.Append("<Header>");
                GG.Append("<Agency>" + UserId + "</Agency>");
                GG.Append("<User>" + UserName + "</User>");
                GG.Append("<Password>" + Password + "</Password>");
                GG.Append("<Operation>BOOKING_STATUS_REQUEST</Operation>");
                GG.Append("<OperationType>Request</OperationType>");
                GG.Append("</Header>");
                GG.Append("<Main>");
                GG.Append("<GoBookingCode>" + Booking_Ref + "</GoBookingCode>");
                GG.Append("</Main>");
                GG.Append("</Root>");
                GG.Append("]]>");
                GG.Append("</xmlRequest>");
                GG.Append("</MakeRequest>");
                GG.Append("</soap12:Body>");
                GG.Append("</soap12:Envelope>");
                TLREQ = GG.ToString();
                return TLREQ;
            }
            catch (Exception ex)
            {
                _ = _exception.Error(ex);
                return TLREQ;
            }
        }

        public int GoGlobalBookingStatus(string BStatus)
        {
            int bookingStatus = 6;
            try
            {
                switch (BStatus.ToLower())
                {
                    case "rq":
                        bookingStatus = 1;
                        break;
                    case "c":
                        bookingStatus = 6;
                        break;
                    case "rc":
                    case "rx":
                    case "rj":
                    case "x":
                        bookingStatus = 2;
                        break;
                }
            }
            catch (Exception ex)
            {
                _ = _exception.Error(ex);
            }
            return bookingStatus;
        }

        public string GOGlobalCancellationRequest(string Booking_Ref, string UserName, string Password, string UserId)
        {
            string TLREQ = "";
            try
            {
                StringBuilder GG = new StringBuilder();
                GG.Append("<soap12:Envelope xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance' xmlns:xsd='http://www.w3.org/2001/XMLSchema' xmlns:soap12='http://www.w3.org/2003/05/soap-envelope'>");
                GG.Append("<soap12:Body>");
                GG.Append("<MakeRequest xmlns='http://www.goglobal.travel/'>");
                GG.Append("<requestType>3</requestType>");
                GG.Append("<xmlRequest>");
                GG.Append("<![CDATA[");
                GG.Append("<Root>");
                GG.Append("<Header>");
                GG.Append("<Agency>" + UserId + "</Agency>");
                GG.Append("<User>" + UserName + "</User>");
                GG.Append("<Password>" + Password + "</Password>");
                GG.Append("<Operation>BOOKING_CANCEL_REQUEST</Operation>");
                GG.Append("<OperationType>Request</OperationType>");
                GG.Append("</Header>");
                GG.Append("<Main>");
                GG.Append("<GoBookingCode>" + Booking_Ref + "</GoBookingCode>");
                GG.Append("</Main>");
                GG.Append("</Root>");
                GG.Append("]]>");
                GG.Append("</xmlRequest>");
                GG.Append("</MakeRequest>");
                GG.Append("</soap12:Body>");
                GG.Append("</soap12:Envelope>");
                TLREQ = GG.ToString();
                return TLREQ;
            }
            catch (Exception ex)
            {
                _ =_exception.ErrorNew(ex, _LogAPI.CancelBooking);
                return TLREQ;
            }
        }
    }
}
