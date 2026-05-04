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
using XConnect.Supplier.Rezlive.Interface;
using Microsoft.Extensions.Options;
using XConnect.ErrorLogHandler.Interface;
using XConnect.Markup.Interface;
using static Azure.Core.HttpHeader;
using XConnect.Supplier.Webbeds;
using Options = XConnect.Models.Options;
using System.Globalization;
using static XConnect.Supplier.Booking.Com.detailres;
using Microsoft.AspNetCore.Http;
using System.Net.Security;
using System.Net;
using static XConnect.Supplier.Booking.Com.preebookrs;
using ZstdSharp.Unsafe;

namespace XConnect.Supplier.Rezlive
{
    public class RezliveRepo : IRezlive
    {
        private readonly ICommonMethod _commonMethod;
        private readonly IException _exception;
        private readonly IMarkup _markup;
        private readonly ISupplierRes _supplierRes;
        private readonly KeySettings _keySettings;



        public RezliveRepo(ICommonMethod commonMethod, IMarkup markup, ISupplierRes supplierRes, IOptions<KeySettings> keySettings, IException exception)
        {
            _commonMethod = commonMethod;
            _exception = exception;
            _markup = markup;
            _supplierRes = supplierRes;
            _keySettings = keySettings.Value;

        }
        public void GetRezliveRequestOnly_async(DataRow[] commonCode, List<StarRating> listRating, bool HitFromAll, HotelSearchParameter SearchPara, List<SupplierDetails> lstRezliveDetails, List<SupplierSearchConfig> lstRezliveSearchConfig, DataTable dtPreferredDataHotel, ref ConcurrentBag<HotelX> AllTask, int Hit, TokenData tokenData, DataSet dtXMLCity)
        {

            try
            {

                List<StarRating> lstRating = new List<StarRating>();
                lstRating = _commonMethod.GetStarRatingStringList_xtg(SearchPara);

                List<HotelCodeByCity> DtHotelDAta = HtmlExtensions.ToList<HotelCodeByCity>(commonCode.CopyToDataTable());
                int ratingcount = lstRating.Count;
                int hotelcount = DtHotelDAta.Count;

                foreach (SupplierDetails RezliveDetails in lstRezliveDetails)
                {
                    try
                    {
                        int SupplierID = RezliveDetails.SupplierID;
                        string Supplier = RezliveDetails.SupplierName;

                        if (lstRezliveSearchConfig.Count > 0)
                        {
                            List<SupplierSearchConfig> RezliveSearchConfig = lstRezliveSearchConfig
                                   .Where(x => x != null && x.SupplierID == SupplierID)
                                   .ToList();

                            if (RezliveSearchConfig != null && RezliveSearchConfig.Count > 0)
                            {
                                DataForSearch dataForSearch = _commonMethod.GetXmlDataForSearchTechwise(SearchPara.Nationality, dtXMLCity, Supplier, RezliveDetails.TechName);
                                if (dataForSearch.XmlNationality != "")
                                {
                                    string Ids = string.Empty;
                                    string xmlbase = "";
                                    _commonMethod.GetPrefferedHotelCodes(ref Ids, ref xmlbase, ref RezliveSearchConfig, Supplier, dtPreferredDataHotel, SearchPara.IsSearchFor);

                                    List<HotelCodeByCity> filter = new List<HotelCodeByCity>();



                                    string RezliveCurrency = Convert.ToString(RezliveDetails.CurrencyShortName);
                                    string Sitecurrency = tokenData.SiteCurrency;

                                    double CurrencyFactorRezlive = 1;

                                    if (RezliveCurrency.ToLower() != Sitecurrency.ToLower()) // change systembase currency 
                                    {
                                        CurrencyFactorRezlive = _commonMethod.GetROEByCurrency(RezliveCurrency, tokenData.dtROE);
                                    }

                                    for (int k = 0; k < RezliveSearchConfig.Count; k++)
                                    {
                                        bool DebugMode = (Convert.ToInt32(RezliveSearchConfig[k].DebugMode) == 0 ? false : true);

                                        if (Convert.ToInt32(RezliveSearchConfig[k].City) == 0)
                                        {
                                            int IsCitywisesearch = Convert.ToInt32(RezliveSearchConfig[k].City);
                                            if (IsCitywisesearch == 0)
                                            {
                                                if (Convert.ToInt32(RezliveSearchConfig[k].CityID) == 0 || Convert.ToInt32(RezliveSearchConfig[k].CityID) == SearchPara.CityID)//All City means 0
                                                {
                                                    //if (filter.Count > 0)
                                                    List<XMLBaseID_search> lst = GetXMLBAsedIdStringNew(DtHotelDAta, Supplier);
                                                    for (int gt = 0; gt < lst.Count; gt++)
                                                    {
                                                        if (lst[gt].XMLBaseID != "")
                                                        {

                                                            List<HotelCodeByCity> filterwithCity = FilterWithCityCode_New(DtHotelDAta, lst[gt].XMLBaseID.ToLower(), Supplier);

                                                            int NoOfRecords = Convert.ToInt32(RezliveSearchConfig[k].Noofrecords);
                                                            double count = Convert.ToDouble(filterwithCity.Count);
                                                            int NoOfDiv = Convert.ToInt32(Math.Ceiling(count / Convert.ToDouble(NoOfRecords)));
                                                            List<List<XMLID_Search>> lstxml = _commonMethod.GetXMLIDString(NoOfDiv, NoOfRecords, count, filterwithCity);

                                                            try
                                                            {
                                                                xmlbase = lst[gt].XMLBaseID.Split('#')[1];

                                                            }
                                                            catch (Exception)
                                                            {
                                                                continue;
                                                            }


                                                            for (int a = 0; a < NoOfDiv; a++)
                                                            {

                                                                string IdsForSearch = Convert.ToString(_commonMethod.GetHotelCodeByCOmma(lstxml[a]));


                                                                HotelX xhotel = new HotelX();
                                                                xhotel.Supplier = Supplier;
                                                                xhotel.SearchTimeOut = Convert.ToInt32(RezliveSearchConfig[k].HotelSearchTimeOut);
                                                                //if (Hit > 0)
                                                                xhotel.ProcessTimeout = Convert.ToInt32(RezliveSearchConfig[k].HotelSearchTimeOut2);
                                                                if (_keySettings.SupplierProxy == "0")
                                                                    xhotel.URL = Convert.ToString(RezliveDetails.WebSiteURL) + RezliveStaticInfo.hotelsearchbyid;
                                                                else
                                                                    xhotel.URL = Convert.ToString(RezliveDetails.WebSiteURLforAPI) + RezliveStaticInfo.hotelsearchbyid;
                                                                xhotel.Action = "hotelSearch";
                                                                xhotel.CurrencyCode = RezliveCurrency;
                                                                xhotel.CurrencyFactor = CurrencyFactorRezlive;
                                                                xhotel.XmlNationality = dataForSearch.XmlNationality;
                                                                xhotel.XmlCity = xmlbase;
                                                                //xhotel.XmlCity = lst[gt].XMLBaseID.Split('#')[1];
                                                                //xhotel.XmlCity = lst[gt].XMLBaseID.Split('#')[0];
                                                                xhotel.XmlCountry = dataForSearch.xmlCountry;
                                                                xhotel.DebugeMode = DebugMode;
                                                                xhotel.AgentWiseMarkup = RezliveDetails.AgentWiseMarkup;
                                                                xhotel.IsShowSupplier = Convert.ToInt16(RezliveDetails.IsShowSupplier) == 1 ? true : false;
                                                                xhotel.SupplierID = RezliveDetails.SupplierID;
                                                                xhotel.MasterSupplierID = RezliveDetails.MasterSupplierId;
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
                                                                    //objrq.Request = "";
                                                                    objrq.Request = "XML=" + XMLRezliveRequestOnly_async(Convert.ToString(RezliveDetails.WebSiteURL), Convert.ToString(RezliveDetails.UserID), Convert.ToString(RezliveDetails.UserName), Convert.ToString(RezliveDetails.Password), SearchPara, RezliveCurrency, CurrencyFactorRezlive, IdsForSearch, DebugMode, xhotel.XmlCity, xhotel.XmlCountry, xhotel.XmlNationality, 0);
                                                                    objrq.Response = "";
                                                                    RQRS.Add(objrq);
                                                                }
                                                                xhotel.RQRS = RQRS;
                                                                #endregion
                                                                // xhotel.Request = "XML=" + Hotel.XMLRezliveRequestOnly_async(Convert.ToString(RezliveDetails[0].WebSiteURL), Convert.ToString(RezliveDetails[0].UserID), Convert.ToString(RezliveDetails[0].UserName), Convert.ToString(RezliveDetails[0].Password), SearchPara, RezliveCurrency, CurrencyFactorRezlive, IdsForSearch, DebugMode, xhotel.XmlCity, CountryCode, NationalityCode, 0);
                                                                xhotel.TechName = RezliveDetails.TechName;
                                                                xhotel.ReturnRooms = RezliveDetails.ReturnRooms;

                                                                AllTask.Add(xhotel);
                                                            }

                                                        }
                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            string IdsForSearch = "";
                                            Int32 Str = listRating[0].Star;
                                            HotelX xhotel = new HotelX();
                                            xhotel.Supplier = Supplier;
                                            xhotel.SearchTimeOut = Convert.ToInt32(RezliveSearchConfig[k].HotelSearchTimeOut);
                                            //if (Hit > 0)
                                            xhotel.ProcessTimeout = Convert.ToInt32(RezliveSearchConfig[k].HotelSearchTimeOut2);
                                            if (_keySettings.SupplierProxy == "0")
                                                xhotel.URL = Convert.ToString(RezliveDetails.WebSiteURL) + RezliveStaticInfo.hotelsearchbydestination;
                                            else
                                                xhotel.URL = Convert.ToString(RezliveDetails.WebSiteURLforAPI) + RezliveStaticInfo.hotelsearchbydestination;
                                            xhotel.Action = "hotelSearch";
                                            xhotel.CurrencyCode = RezliveCurrency;
                                            xhotel.CurrencyFactor = CurrencyFactorRezlive;
                                            xhotel.XmlNationality = dataForSearch.XmlNationality;
                                            xhotel.XmlCity = dataForSearch.xmlCity;
                                            xhotel.XmlCountry = dataForSearch.xmlCountry;
                                            xhotel.DebugeMode = DebugMode;
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
                                                objrq.Request = "XML=" + XMLRezliveRequestOnly_async(Convert.ToString(RezliveDetails.WebSiteURL), Convert.ToString(RezliveDetails.UserID), Convert.ToString(RezliveDetails.UserName), Convert.ToString(RezliveDetails.Password), SearchPara, RezliveCurrency, CurrencyFactorRezlive, IdsForSearch, DebugMode, xhotel.XmlCity, xhotel.XmlCountry, xhotel.XmlNationality, 0);
                                                //objrq.Request = "";
                                                objrq.Response = "";
                                                RQRS.Add(objrq);
                                            }
                                            xhotel.RQRS = RQRS;
                                            #endregion
                                            xhotel.TechName = RezliveDetails.TechName;
                                            xhotel.ReturnRooms = RezliveDetails.ReturnRooms;
                                            AllTask.Add(xhotel);

                                        }
                                    }

                                }
                            }

                        }
                    }
                    catch (Exception ex)
                    {
                        _ = _exception.Error(ex);
                    }
                }
            }
            catch (Exception ex)
            {
                _ = _exception.Error(ex);
            }
        }

        public List<HotelCodeByCity> FilterWithCityCode_New(List<HotelCodeByCity> DtHotelDAta, string XMLBaseID, string Supplier)
        {
            List<HotelCodeByCity> filterwithCity = new List<HotelCodeByCity>();
            try
            {
                for (int c = 0; c < DtHotelDAta.Count; c++)
                {
                    if (DtHotelDAta[c].XmlbaseId.ToLower() == XMLBaseID.ToLower() && DtHotelDAta[c].supplierName.ToLower() == Supplier.ToLower())
                        filterwithCity.Add(DtHotelDAta[c]);
                }
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
            }
            return filterwithCity;
        }

        public List<XMLBaseID_search> GetXMLBAsedIdStringNew(List<HotelCodeByCity> DtHotelDAta, string Supplier)
        {
            List<XMLBaseID_search> lst = new List<XMLBaseID_search>();

            try
            {
                for (int x = 0; x < DtHotelDAta.Count; x++)
                {


                    List<XMLBaseID_search> temp = new List<XMLBaseID_search>();
                    foreach (XMLBaseID_search item in lst)
                    {
                        if (item.XMLBaseID.ToLower() == Convert.ToString(DtHotelDAta[x].XmlbaseId).ToLower() && Convert.ToString(DtHotelDAta[x].supplierName).ToLower() == Supplier.ToLower())
                            temp.Add(item);
                    }

                    if (temp.Count == 0)
                    {
                        lst.Add(new XMLBaseID_search { XMLBaseID = Convert.ToString(DtHotelDAta[x].XmlbaseId) });
                    }


                }

            }
            catch (Exception ex)
            {
                _exception.Error(ex);
            }
            return lst;
        }


        public class XMLBaseID_search
        {
            public string XMLBaseID { get; set; }
        }

        public class XMLID_search
        {
            public string XMLID { get; set; }
        }


        public string XMLRezliveRequestOnly_async(string Url, string AgentCode, string UserName, string Password, HotelSearchParameter SearchParam, string MikiCurrency, double CurrencyFactorRezlive, string HotelId, bool DebugMode, string CityCode, string CountryCode, string NationalityCode, int Rating)
        {
            try
            {
                string jsonString = "";
                if (HotelId != string.Empty)
                {
                    string RezlivePriceOnlyByProductId = GetRezlivePriceOnlyByProductId(SearchParam, AgentCode, UserName, Password, MikiCurrency, CityCode, CountryCode, NationalityCode, HotelId);
                    jsonString = RezlivePriceOnlyByProductId;
                }
                else
                {
                    string RezlivePriceOnlyByHotelId = GetRezlivePriceOnlyByHotelId(SearchParam, AgentCode, UserName, Password, MikiCurrency, CityCode, CountryCode, NationalityCode, Rating);
                    jsonString = RezlivePriceOnlyByHotelId;
                }
                return jsonString;

            }
            catch (Exception ex)
            {
                _ = _exception.Error(ex);
                return "";
            }
        }

        public string GetRezlivePriceOnlyByProductId(HotelSearchParameter SearchParam, String AgentCode, string UserName, string Password, string Currency, string CityCode, string CountryCode, string NationalityCode, string HotelId)
        {
            try
            {
                #region Price Only and Room Only RQ
                string CheckIn = SearchParam.CheckInDate;
                CheckIn = CheckIn.Substring(0, 2) + "/" + CheckIn.Substring(3, 2) + "/" + CheckIn.Substring(6, 4);

                string CheckOut = SearchParam.CheckOutDate;
                CheckOut = CheckOut.Substring(0, 2) + "/" + CheckOut.Substring(3, 2) + "/" + CheckOut.Substring(6, 4);

                StringBuilder Rezlive_RQ = new StringBuilder();
                Rezlive_RQ.Append("<HotelFindRequest>");
                Rezlive_RQ.Append("<Authentication><AgentCode>" + AgentCode + "</AgentCode><UserName>" + UserName + "</UserName><Password>" + Password + "</Password></Authentication>");
                Rezlive_RQ.Append("<Booking>");
                Rezlive_RQ.Append("<ArrivalDate>" + CheckIn + "</ArrivalDate>");
                Rezlive_RQ.Append("<DepartureDate>" + CheckOut + "</DepartureDate>");
                Rezlive_RQ.Append("<CountryCode>" + CountryCode + "</CountryCode>");
                Rezlive_RQ.Append("<City>" + CityCode + "</City>");
                Rezlive_RQ.Append("<HotelIDs>");
                if (HotelId.Trim() != string.Empty)
                {
                    string[] HotelIds = Convert.ToString(HotelId).Split(',');
                    for (int htid = 0; htid < HotelIds.Length; htid++)
                    {
                        Rezlive_RQ.Append("<Int>" + HotelIds[htid] + "</Int>");
                    }
                }
                Rezlive_RQ.Append("</HotelIDs>");
                Rezlive_RQ.Append("<GuestNationality>" + NationalityCode + "</GuestNationality>");
                int R = 0;
                Rezlive_RQ.Append("<Rooms>");
                for (int i = 0; i < SearchParam.Rooms.Count; i++)
                {
                    Rezlive_RQ.Append("<Room>");
                    R = i + 1;
                    Rezlive_RQ.Append("<Type>Room-" + R + "</Type>");
                    int NoofAdults = SearchParam.Rooms[i].NoofAdults;
                    int NoOfChild = SearchParam.Rooms[i].NoofChild;
                    Rezlive_RQ.Append("<NoOfAdults>" + NoofAdults + "</NoOfAdults>");
                    Rezlive_RQ.Append("<NoOfChilds>" + NoOfChild + "</NoOfChilds>");
                    if (NoOfChild != 0)
                    {
                        Rezlive_RQ.Append("<ChildrenAges>");
                        if (NoOfChild == 1)
                            Rezlive_RQ.Append("<ChildAge>" + SearchParam.Rooms[i].Child1Age + "</ChildAge>");
                        if (NoOfChild == 2)
                        {
                            Rezlive_RQ.Append("<ChildAge>" + SearchParam.Rooms[i].Child1Age + "</ChildAge>");
                            Rezlive_RQ.Append("<ChildAge>" + SearchParam.Rooms[i].Child2Age + "</ChildAge>");
                        }

                        Rezlive_RQ.Append("</ChildrenAges>");
                    }
                    Rezlive_RQ.Append("</Room>");
                }
                Rezlive_RQ.Append("</Rooms>");
                Rezlive_RQ.Append("</Booking>");
                Rezlive_RQ.Append("</HotelFindRequest>");
                return Rezlive_RQ.ToString();
                #endregion

            }
            catch (Exception ex)
            {
                _ = _exception.Error(ex);
            }
            return string.Empty;
        }

        public string GetRezlivePriceOnlyByHotelId(HotelSearchParameter SearchParam, String AgentCode, string UserName, string Password, string MikiCurrency, string CityCode, string CountryCode, string NationalityCode, int Rating)
        {
            try
            {
                #region Price Only and Room Only RQ
                string CheckIn = SearchParam.CheckInDate;
                CheckIn = CheckIn.Substring(0, 2) + "/" + CheckIn.Substring(3, 2) + "/" + CheckIn.Substring(6, 4);

                string CheckOut = SearchParam.CheckOutDate;
                CheckOut = CheckOut.Substring(0, 2) + "/" + CheckOut.Substring(3, 2) + "/" + CheckOut.Substring(6, 4);

                string totalrating = SearchParam.SelectedStart;

                StringBuilder Jac_SRCH_RQ = new StringBuilder();
                Jac_SRCH_RQ.Append("<HotelFindRequest>");
                Jac_SRCH_RQ.Append("<Authentication><AgentCode>" + AgentCode + "</AgentCode><UserName>" + UserName + "</UserName><Password>" + Password + "</Password></Authentication>");
                Jac_SRCH_RQ.Append("<Booking>");
                Jac_SRCH_RQ.Append("<ArrivalDate>" + CheckIn + "</ArrivalDate>");
                Jac_SRCH_RQ.Append("<DepartureDate>" + CheckOut + "</DepartureDate>");
                Jac_SRCH_RQ.Append("<CountryCode>" + CountryCode + "</CountryCode>");
                Jac_SRCH_RQ.Append("<City>" + CityCode + "</City>");
                Jac_SRCH_RQ.Append("<GuestNationality>" + NationalityCode + "</GuestNationality>");
                //Jac_SRCH_RQ.Append("<HotelRatings>");

                ////Jac_SRCH_RQ.Append("<HotelRating>" + Rating + "</HotelRating>");
                //Jac_SRCH_RQ.Append("<HotelRating>1</HotelRating>");
                //Jac_SRCH_RQ.Append("<HotelRating>2</HotelRating>");
                //Jac_SRCH_RQ.Append("<HotelRating>3</HotelRating>");
                //Jac_SRCH_RQ.Append("<HotelRating>4</HotelRating>");
                //Jac_SRCH_RQ.Append("<HotelRating>5</HotelRating>");
                //Jac_SRCH_RQ.Append("</HotelRatings>");
                Jac_SRCH_RQ.Append("<HotelRatings>");
                if (totalrating.Trim() != string.Empty)
                {
                    string[] totalratings = Convert.ToString(totalrating).Split(',');
                    for (int htid = 0; htid < totalratings.Length; htid++)
                    {
                        Jac_SRCH_RQ.Append("<HotelRating>" + totalratings[htid] + "</HotelRating>");
                    }
                }
                Jac_SRCH_RQ.Append("</HotelRatings>");
                int R = 0;
                Jac_SRCH_RQ.Append("<Rooms>");
                for (int i = 0; i < SearchParam.Rooms.Count; i++)
                {
                    Jac_SRCH_RQ.Append("<Room>");
                    R = i + 1;
                    Jac_SRCH_RQ.Append("<Type>Room-" + R + "</Type>");
                    int NoofAdults = SearchParam.Rooms[i].NoofAdults;
                    int NoOfChild = SearchParam.Rooms[i].NoofChild;
                    Jac_SRCH_RQ.Append("<NoOfAdults>" + NoofAdults + "</NoOfAdults>");
                    Jac_SRCH_RQ.Append("<NoOfChilds>" + NoOfChild + "</NoOfChilds>");
                    if (NoOfChild != 0)
                    {
                        Jac_SRCH_RQ.Append("<ChildrenAges>");
                        if (NoOfChild == 1)
                            Jac_SRCH_RQ.Append("<ChildAge>" + SearchParam.Rooms[i].Child1Age + "</ChildAge>");
                        if (NoOfChild == 2)
                        {
                            Jac_SRCH_RQ.Append("<ChildAge>" + SearchParam.Rooms[i].Child1Age + "</ChildAge>");
                            Jac_SRCH_RQ.Append("<ChildAge>" + SearchParam.Rooms[i].Child2Age + "</ChildAge>");
                        }

                        Jac_SRCH_RQ.Append("</ChildrenAges>");
                    }
                    Jac_SRCH_RQ.Append("</Room>");
                }
                Jac_SRCH_RQ.Append("</Rooms>");
                Jac_SRCH_RQ.Append("</Booking>");
                Jac_SRCH_RQ.Append("</HotelFindRequest>");
                return Jac_SRCH_RQ.ToString();
                #endregion
            }
            catch (Exception ex)
            {
                _ = _exception.Error(ex);
            }
            return string.Empty;
        }

        public async Task<List<Options>> Rezlive_HotelSearchWithRooms_090925(HotelX item, string giitaCode, double AgentPercentage, CommonSearch commonSearch, string currency, string ResponseData, string CityCode, HotelSearchParameter SearchParam, string ContryCode, string XMLNationality, double CurrencyFactorRezlive, string Provider, DataSet dsAgentDetails, DataTable dtCountryCityMarkup, Int32 AgentId, Int32 TariffID, int SupplierID, Int32 processTimeout, string ResProccessTimerLogActive, string ResProccessTimeValueforSaveLog)
        {
            List<Options> options = new List<Options>();
            try
            {
                #region DataTable
                DataTable DtHotel = _commonMethod.GenerateDataTableNewV2();
                #endregion
                if (ResponseData != string.Empty)
                {
                    #region PriceOnly RS
                    if (ResponseData == string.Empty)
                    {
                        item.ResponseType = "NoCount";
                        return options;
                    }
                    DataSet RezliveResponse = _commonMethod.GetDataSetFromXMLString(ResponseData);
                    if (RezliveResponse == null || RezliveResponse.Tables["Hotel"] == null)
                    {
                        item.ResponseType = "NoCount";
                        return options;
                    }
                    #endregion


                    #region Hotel List
                    if (RezliveResponse.Tables["Hotel"].Rows.Count < 1)
                    {
                        item.ResponseType = "NoCount";
                        return options;
                    }
                    for (int h = 0; h <= RezliveResponse.Tables["Hotel"].Rows.Count - 1; h++)
                    {
                        double TotalPrice = 0;
                        DataRow[] Roomdetails_ids = RezliveResponse.Tables["RoomDetails"].Select("hotel_id=" + Convert.ToInt16(RezliveResponse.Tables["Hotel"].Rows[h]["hotel_id"]));
                        DataRow[] pricedata = RezliveResponse.Tables["RoomDetail"].Select("RoomDetails_id=" + Convert.ToInt32(Roomdetails_ids[0]["RoomDetails_id"]));
                        {
                            string[] PriceList = Convert.ToString(pricedata[0]["totalRate"]).Split(new String[] { "|" }, StringSplitOptions.None);
                            for (int pr = 0; pr < PriceList.Length; pr++)
                            {
                                if (pr == 0)
                                    TotalPrice = Convert.ToDouble(PriceList[pr]);
                                else
                                    TotalPrice = Convert.ToDouble(TotalPrice + Convert.ToDouble(PriceList[pr]));
                            }

                            if (TotalPrice == 0)
                            {
                                item.ResponseType = "NoCount";
                                continue;
                            }

                            //DataRow drXmlHotel = DtHotel.NewRow();
                            AgentPercentage = 0.0;
                            double AgentMarkupAmount = 0.0;
                            Int64 GiataCode = Convert.ToInt64(giitaCode);
                            if (SearchParam.Filters.IsShowRooms == 0)
                            {
                                Int64 RaynaID = Convert.ToInt64(RezliveResponse.Tables["Hotel"].Rows[h]["Id"]);

                                DataRow drXmlHotel = DtHotel.NewRow();
                                drXmlHotel["ContractID"] = 0;
                                drXmlHotel["SupplierId"] = SupplierID;
                                drXmlHotel["HotelName"] = Convert.ToString(RezliveResponse.Tables["Hotel"].Rows[h]["Name"]);
                                drXmlHotel["GIITACode"] = GiataCode;
                                drXmlHotel["UniqueNo"] = Convert.ToString(RezliveResponse.Tables["Hotel"].Rows[h]["Id"]);
                                drXmlHotel["RaynaId"] = RaynaID;
                                drXmlHotel["OwnsystemHotelId"] = RaynaID;
                                drXmlHotel["Rating"] = 0;
                                Int64 OwnsystemHotelId = 0;
                                drXmlHotel["IsRecomondedHotel"] = true;
                                drXmlHotel["VatdiffAmount"] = 0;
                                drXmlHotel["XMLHotelCode"] = Convert.ToString(RezliveResponse.Tables["Hotel"].Rows[h]["Id"]);
                                drXmlHotel["SupplierName"] = Provider;
                                drXmlHotel["SupplierCurrency"] = currency;
                                double Pricenew = TotalPrice * CurrencyFactorRezlive;
                                drXmlHotel["FinalSellingPrice"] = Pricenew;
                                drXmlHotel["SellingPrice"] = Pricenew;

                                DtHotel.Rows.Add(drXmlHotel);
                                double FinalMinPrice = Pricenew;
                                if (FinalMinPrice > 0)
                                {
                                    #region "Room code Block"
                                    bool willAddOrNot = true;
                                    Int64 GiataId = 0;
                                    int RatingMain = 0;
                                    //Int64 OwnsystemHotelId = 0;
                                    double price = FinalMinPrice;
                                    _markup.GetHotelDataCommon(ref price, ref willAddOrNot, ref OwnsystemHotelId, ref RaynaID, ref GiataId, ref RatingMain, ref drXmlHotel, commonSearch.AgentId, commonSearch.TariffID, commonSearch.dsAgentDetails, commonSearch.supplierGiataMaster, SearchParam, commonSearch.dtCountryCityMarkup, commonSearch.B2CMarkup, commonSearch.IsParentId, commonSearch.SubAgentMarkup, commonSearch.SearchByHotel, commonSearch.HotelIDS);
                                    if (willAddOrNot)
                                    {
                                        #region "Room Logic"
                                        DataTable dt_HotelRoom = _commonMethod.GenerateRoomtypeDataTableNewV2();
                                        DataTable dt_Cancellation = _commonMethod.GenerateCancellationDataTableNewV2();
                                        DataTable dt_RateBreackUp = _commonMethod.GenerateoomRateBreackUpDataTableNewV2();
                                        #endregion
                                        decimal LowestPrice = Convert.ToDecimal(price).CalculatePrice(commonSearch.tokenData, commonSearch.advancedOptions, currency);
                                        Options ops1 = (await _supplierRes.GetCommonRoomForSuppliers(Convert.ToInt64(giitaCode), commonSearch.tokenData, commonSearch.advancedOptions, Convert.ToDouble(LowestPrice), drXmlHotel, dt_HotelRoom, dt_Cancellation, dt_RateBreackUp, SearchParam));
                                        if (ops1 != null)
                                            options.Add(ops1);
                                    }
                                    #endregion
                                }


                            }
                            else
                            {

                                {
                                    Int64 RaynaID = Convert.ToInt64(RezliveResponse.Tables["Hotel"].Rows[h]["Id"]);
                                    {
                                        DataRow drXmlHotel = DtHotel.NewRow();
                                        drXmlHotel["ContractID"] = 0;
                                        drXmlHotel["SupplierId"] = SupplierID;
                                        drXmlHotel["HotelName"] = Convert.ToString(RezliveResponse.Tables["Hotel"].Rows[h]["Name"]);
                                        drXmlHotel["GIITACode"] = GiataCode;
                                        drXmlHotel["UniqueNo"] = Convert.ToString(RezliveResponse.Tables["Hotel"].Rows[h]["Id"]);
                                        drXmlHotel["RaynaId"] = RaynaID;
                                        drXmlHotel["OwnsystemHotelId"] = RaynaID;
                                        drXmlHotel["Rating"] = 0;
                                        Int64 OwnsystemHotelId = 0;
                                        drXmlHotel["IsRecomondedHotel"] = true;
                                        drXmlHotel["VatdiffAmount"] = 0;
                                        drXmlHotel["XMLHotelCode"] = Convert.ToString(RezliveResponse.Tables["Hotel"].Rows[h]["Id"]);
                                        drXmlHotel["SupplierName"] = Provider;
                                        drXmlHotel["SupplierCurrency"] = currency;
                                        double Pricenew = TotalPrice * CurrencyFactorRezlive;
                                        drXmlHotel["FinalSellingPrice"] = Pricenew;
                                        drXmlHotel["SellingPrice"] = Pricenew;

                                        DtHotel.Rows.Add(drXmlHotel);
                                        double FinalMinPrice = Pricenew;
                                        //if (FinalMinPrice > 0)
                                        {
                                            #region "Room code Block"
                                            bool willAddOrNot = true;
                                            //Int64 GiataId = 0;
                                            int RatingMain = 0;
                                            //Int64 OwnsystemHotelId = 0;
                                            //double price = 0;
                                            //price = FinalMinPrice;
                                            _markup.GetHotelDataCommon(ref Pricenew, ref willAddOrNot, ref OwnsystemHotelId, ref RaynaID, ref GiataCode, ref RatingMain, ref drXmlHotel, commonSearch.AgentId, commonSearch.TariffID, commonSearch.dsAgentDetails, commonSearch.supplierGiataMaster, SearchParam, commonSearch.dtCountryCityMarkup, commonSearch.B2CMarkup, commonSearch.IsParentId, commonSearch.SubAgentMarkup, commonSearch.SearchByHotel, commonSearch.HotelIDS);
                                            if (willAddOrNot)
                                            {
                                                #region "Room Logic"
                                                DataTable DtRezlive_HotelRoom = _commonMethod.GenerateRoomtypeDataTableNewV2();
                                                DataTable dt_Cancellation = _commonMethod.GenerateCancellationDataTableNewV2();
                                                DataTable DtRezlive_RateBreackUp = _commonMethod.GenerateoomRateBreackUpDataTableNewV2();
                                                DataSet Rezlive_Response = _commonMethod.GetDataSetFromXMLString(ResponseData);

                                                if (Rezlive_Response.Tables["HotelFindResponse"] != null)
                                                {
                                                    for (int j = 0; j < Rezlive_Response.Tables["HotelFindResponse"].Rows.Count; j++)
                                                    {
                                                        string SearchSessionId = "";

                                                        if (Rezlive_Response.Tables.Contains("HotelFindResponse"))
                                                        {
                                                            if (Rezlive_Response.Tables["HotelFindResponse"].Columns.Contains("SearchSessionId"))
                                                                SearchSessionId = Convert.ToString(Rezlive_Response.Tables["HotelFindResponse"].Rows[j]["SearchSessionId"]);
                                                            else
                                                                continue;
                                                        }
                                                        else
                                                            continue;
                                                        double totalPrice = 0;
                                                        if (Rezlive_Response.Tables.Contains("Hotel"))
                                                        {
                                                            //for (int i = 0; i < Rezlive_Response.Tables["Hotel"].Rows.Count; i++)
                                                            {
                                                                //string PropertyID = Convert.ToString(Rezlive_Response.Tables["Hotel"].Rows[i]["Id"]);

                                                                for (int k = 0; k < Rezlive_Response.Tables["RoomDetail"].Rows.Count; k++)
                                                                {
                                                                    #region DataRaw and Variable Declaration

                                                                    int TotalAdult = 0;
                                                                    int TotalChild = 0;
                                                                    int noofroom = Convert.ToInt32(Rezlive_Response.Tables["RoomDetail"].Rows[k]["TotalRooms"]);

                                                                    string BoardID = Convert.ToString(Rezlive_Response.Tables["RoomDetail"].Rows[k]["BoardBasis"]);
                                                                    string[] BoardtypeID = Convert.ToString(BoardID).Split(new String[] { "|" }, StringSplitOptions.None);
                                                                    BoardID = BoardtypeID[0];
                                                                    string RoomPrice = Convert.ToString(Rezlive_Response.Tables["RoomDetail"].Rows[k]["TotalRate"]);
                                                                    string[] RoomPriceList = Convert.ToString(RoomPrice).Split(new String[] { "|" }, StringSplitOptions.None);
                                                                    double RoomTotalPrice = 0;
                                                                    for (int pr = 0; pr < RoomPriceList.Length; pr++)
                                                                    {
                                                                        if (pr == 0)
                                                                            RoomTotalPrice = Convert.ToDouble(RoomPriceList[pr]);
                                                                        else
                                                                            RoomTotalPrice = Convert.ToDouble(RoomTotalPrice + Convert.ToDouble(RoomPriceList[pr]));
                                                                    }
                                                                    string BookingKey = Convert.ToString(Rezlive_Response.Tables["RoomDetail"].Rows[k]["BookingKey"]);
                                                                    string TotAdult = Convert.ToString(Rezlive_Response.Tables["RoomDetail"].Rows[k]["Adults"]);
                                                                    string[] NoOfAdults = Convert.ToString(TotAdult).Split(new String[] { "|" }, StringSplitOptions.None);
                                                                    for (int TH = 0; TH < NoOfAdults.Length; TH++)
                                                                    {
                                                                        if (TH == 0)
                                                                            TotalAdult = Convert.ToInt16(NoOfAdults[TH]);
                                                                        else
                                                                            TotalAdult = Convert.ToInt16(TotalAdult + Convert.ToInt16(NoOfAdults[TH]));
                                                                    }
                                                                    string TotChild = Convert.ToString(Rezlive_Response.Tables["RoomDetail"].Rows[k]["Children"]);
                                                                    string[] NoOfChilds = Convert.ToString(TotChild).Split(new String[] { "|" }, StringSplitOptions.None);
                                                                    for (int TC = 0; TC < NoOfChilds.Length; TC++)
                                                                    {
                                                                        if (TC == 0)
                                                                            TotalChild = Convert.ToInt16(NoOfChilds[TC]);
                                                                        else
                                                                            TotalChild = Convert.ToInt16(TotalChild + Convert.ToInt16(NoOfChilds[TC]));
                                                                    }
                                                                    int Child = Convert.ToInt16(TotalChild);
                                                                    string ChildrenAges = Convert.ToString(Rezlive_Response.Tables["RoomDetail"].Rows[k]["ChildrenAges"]);
                                                                    string TypeNameRoom = Convert.ToString(Rezlive_Response.Tables["RoomDetail"].Rows[k]["Type"]);
                                                                    //string[] Type = Convert.ToString(TypeNameRoom).Split(new String[] { "|" }, StringSplitOptions.None);
                                                                    //TypeNameRoom = Type[0];
                                                                    //if (Child > 0)
                                                                    //{
                                                                    //    string[] Type1 = Convert.ToString(TypeNameRoom).Split(new String[] { "-" }, StringSplitOptions.None);
                                                                    //    TypeNameRoom = Type1[0];
                                                                    //}
                                                                    string RoomDescription = Convert.ToString(Rezlive_Response.Tables["RoomDetail"].Rows[k]["RoomDescription"]);
                                                                    string[] Description = Convert.ToString(RoomDescription).Split(new String[] { "|" }, StringSplitOptions.None);
                                                                    RoomDescription = Description[0];
                                                                    totalPrice = Convert.ToDouble(RoomTotalPrice);
                                                                    totalPrice = totalPrice * CurrencyFactorRezlive;
                                                                    string RoomName = "";
                                                                    int NoOfNight = SearchParam.NoofNights;
                                                                    if (RoomDescription == "")
                                                                    {
                                                                        RoomName = TypeNameRoom;
                                                                    }
                                                                    else
                                                                    {
                                                                        RoomName = TypeNameRoom + "-" + RoomDescription;
                                                                    }
                                                                    string RoomTypeId = SearchSessionId + "$$$" + BookingKey + "$$$" + CityCode + "$$$" + ContryCode + "$$$" + XMLNationality + "$$$" + Convert.ToString(Rezlive_Response.Tables["RoomDetail"].Rows[k]["Adults"]) + "$$$" + Convert.ToString(Rezlive_Response.Tables["RoomDetail"].Rows[k]["Children"]) + "$$$" + ChildrenAges + "$$$" + Convert.ToString(Rezlive_Response.Tables["RoomDetail"].Rows[k]["Type"]).Replace("'", "#") + "$$$" + noofroom + "$$$" + Convert.ToString(Rezlive_Response.Tables["RoomDetail"].Rows[k]["TotalRate"]) + "$$$" + RoomDescription;

                                                                    RoomTypeId = RoomTypeId.Replace("'", "");
                                                                    #endregion

                                                                    #region"Ratebreakup"
                                                                    DateTime dtCheckInDateForRate = DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null);
                                                                    #region Vat
                                                                    Double VatOnRateBrackUp = 0;
                                                                    double AgentCompanyPercentage = Convert.ToDouble(SearchParam.AgentCompanyPercentage);
                                                                    bool IsB2C = Convert.ToBoolean(Convert.ToByte(SearchParam.IsB2BOrB2C));
                                                                    Boolean isPerce = true;
                                                                    //double B2CMarkup = (IsB2C == false ? AgentCompanyPercentage : XMLHotelBookingCommon.B2CMarkup);
                                                                    // if (IsB2C == true)
                                                                    //AgentPercentage = 0;
                                                                    double RoomPrice1 = _markup.ProcessHotelRateBreakupPrice_WithMarkup(1, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), totalPrice / NoOfNight, commonSearch.B2CMarkup, commonSearch.IsParentId, isPerce, AgentPercentage, AgentMarkupAmount, commonSearch.SubAgentMarkup, ref VatOnRateBrackUp);
                                                                    #endregion
                                                                    for (int night = 0; night < SearchParam.NoofNights; night++)
                                                                    {
                                                                        string rateWeekColumn = Convert.ToString(dtCheckInDateForRate.Date.AddDays(night).DayOfWeek).Substring(0, 3);
                                                                        DataRow drRezlive_RateBreackUp = DtRezlive_RateBreackUp.NewRow();
                                                                        drRezlive_RateBreackUp["RoomNo"] = Convert.ToString("1");
                                                                        drRezlive_RateBreackUp["SupplierName"] = Provider;
                                                                        drRezlive_RateBreackUp["OwnsystemHotelId"] = OwnsystemHotelId;
                                                                        drRezlive_RateBreackUp["ContractID"] = 0;
                                                                        drRezlive_RateBreackUp["RoomTypeName"] = RoomName;
                                                                        drRezlive_RateBreackUp["RoomTypeId"] = RoomTypeId;
                                                                        drRezlive_RateBreackUp["MealId"] = BoardID;
                                                                        drRezlive_RateBreackUp["Meal"] = BoardID;
                                                                        drRezlive_RateBreackUp["BookingStatus"] = "Available";
                                                                        drRezlive_RateBreackUp["DateOfWeek"] = rateWeekColumn;
                                                                        drRezlive_RateBreackUp["Date"] = dtCheckInDateForRate.Date.AddDays(night).Date;
                                                                        drRezlive_RateBreackUp["Rate"] = RoomPrice1;
                                                                        drRezlive_RateBreackUp["RateType"] = "A Rate"; //SearchParam.Rooms.Count == 1 ? "A Rate" : ((j + 1) + ". " + TypeNameRoom); 
                                                                        drRezlive_RateBreackUp["SupplierCurrency"] = currency;
                                                                        drRezlive_RateBreackUp["BuyingPrice"] = RoomPrice1;
                                                                        DtRezlive_RateBreackUp.Rows.Add(drRezlive_RateBreackUp);
                                                                        DtRezlive_RateBreackUp.AcceptChanges();
                                                                    }
                                                                    #endregion

                                                                    #region Rooms
                                                                    DataRow drRezliveRoom = DtRezlive_HotelRoom.NewRow();
                                                                    drRezliveRoom["OwnSystemHotelId"] = OwnsystemHotelId;
                                                                    drRezliveRoom["RoomTypeName"] = RoomName;
                                                                    drRezliveRoom["RoomTypeCode"] = BookingKey;
                                                                    drRezliveRoom["MealId"] = BoardID;
                                                                    drRezliveRoom["MealName"] = BoardID;
                                                                    drRezliveRoom["ContractID"] = 0;
                                                                    drRezliveRoom["BoardType"] = BoardID;
                                                                    drRezliveRoom["RoomTypeId"] = RoomTypeId;
                                                                    drRezliveRoom["AdultsOccupancy"] = TotalAdult;
                                                                    drRezliveRoom["ChildOccupancy"] = TotalChild;
                                                                    drRezliveRoom["MaxExtraBed"] = "0";
                                                                    drRezliveRoom["RoomNo"] = "1";
                                                                    drRezliveRoom["RoomSequenceNo"] = 0;
                                                                    drRezliveRoom["Type"] = "";
                                                                    drRezliveRoom["OfficeId"] = "";
                                                                    drRezliveRoom["BookingStatus"] = "Available";
                                                                    drRezliveRoom["UniqueNo"] = "";
                                                                    drRezliveRoom["OfferDescription"] = "";
                                                                    drRezliveRoom["OfferCode"] = "";
                                                                    drRezliveRoom["EssentialInformation"] = "";

                                                                    double ItemPrice = Convert.ToDouble(totalPrice);

                                                                    _markup.ProcessHotelRoom_WithMarkup(CurrencyFactorRezlive, currency, true, true, SearchParam.NoofNights, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), SearchParam.CheckInDate, SearchParam.CheckOutDate, ref ItemPrice, ref drRezliveRoom, isPerce, AgentPercentage, AgentMarkupAmount, commonSearch.SubAgentMarkup, commonSearch.B2CMarkup, commonSearch.IsParentId);

                                                                    drRezliveRoom["SupplierName"] = Provider;
                                                                    DtRezlive_HotelRoom.Rows.Add(drRezliveRoom);
                                                                    #endregion
                                                                }
                                                            }
                                                        }
                                                        #endregion
                                                        decimal LowestPrice = Convert.ToDecimal(Pricenew).CalculatePrice(commonSearch.tokenData, commonSearch.advancedOptions, currency);
                                                        //(this decimal price, TokenData tokenData, AdvancedOptions advancedOptions, string SupplierCurrency, int DecimalDigitRound = 2)
                                                        Options ops1 = (await _supplierRes.GetCommonRoomForSuppliers(GiataCode, commonSearch.tokenData, commonSearch.advancedOptions, Convert.ToDouble(LowestPrice), drXmlHotel, DtRezlive_HotelRoom, dt_Cancellation, DtRezlive_RateBreackUp, SearchParam));
                                                        if (ops1 != null)
                                                            options.Add(ops1);
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
                    #endregion
                }
                else
                {
                    item.ResponseType = "NoCount";
                }
                //DtHotel.DefaultView.RowFilter = "FinalSellingPrice>0";
                //DtHotel.DefaultView.RowFilter = "Rating IN(" + strRating + ")";
                //return DtHotel.DefaultView.ToTable();
                return options;
            }
            catch (Exception ex)
            {
                _ = _exception.Error(ex);
                return options;
            }
            return options;
        }

        public async Task<List<Options>> Rezlive_HotelSearchWithRooms(HotelX item, string giitaCode, double AgentPercentage, CommonSearch commonSearch, string currency, string ResponseData, string CityCode, HotelSearchParameter SearchParam, string ContryCode, string XMLNationality, double CurrencyFactorRezlive, string Provider, DataSet dsAgentDetails, DataTable dtCountryCityMarkup, Int32 AgentId, Int32 TariffID, int SupplierID, Int32 processTimeout, string ResProccessTimerLogActive, string ResProccessTimeValueforSaveLog, string ProccessCutOffTime)
        {
            System.Diagnostics.Stopwatch timer_room_Rezlive = new System.Diagnostics.Stopwatch();
            timer_room_Rezlive.Start();
            List<Options> options = new List<Options>();
            int CBranchId = 0;
            string SearchSessionId = "";
            try
            {
                #region DataTable
                DataTable DtHotel = _commonMethod.GenerateDataTableNewV2();
                #endregion
                if (ResponseData != string.Empty)
                {
                    #region PriceOnly RS
                    if (ResponseData == string.Empty)
                    {
                        item.ResponseStatus = 204;
                        item.ResponseType = "NoCount";
                        return options;
                    }
                    DataSet Rezlive_Response = _commonMethod.GetDataSetFromXMLString(ResponseData);
                    if (Rezlive_Response == null || Rezlive_Response.Tables["Hotel"] == null)
                    {
                        item.ResponseStatus = 404;
                        item.ResponseType = "NoCount";
                        return options;
                    }
                    else
                    {
                        if (Rezlive_Response.Tables["HotelFindResponse"] != null && Rezlive_Response.Tables["HotelFindResponse"].Rows.Count > 0)
                        {
                            if (Rezlive_Response.Tables["HotelFindResponse"].Columns.Contains("SearchSessionId"))
                                SearchSessionId = Convert.ToString(Rezlive_Response.Tables["HotelFindResponse"].Rows[0]["SearchSessionId"]);

                            if (Convert.ToString(Rezlive_Response.Tables["HotelFindResponse"].Rows[0]["Currency"]).ToLower() != commonSearch.tokenData.SiteCurrency.ToLower())
                                CurrencyFactorRezlive = _commonMethod.GetROEByCurrency(Convert.ToString(Rezlive_Response.Tables["HotelFindResponse"].Rows[0]["Currency"]), commonSearch.tokenData.dtROE);
                        }
                    }
                    #endregion

                    #region Hotel List
                    if (Rezlive_Response.Tables["Hotel"].Rows.Count < 1)
                    {
                        item.ResponseStatus = 404;
                        item.ResponseType = "NoCount";
                        return options;
                    }
                    for (int h = 0; h <= Rezlive_Response.Tables["Hotel"].Rows.Count - 1; h++)
                    {
                        item.ResponseType = "Success";
                        if (ProccessCutOffTime == "1" && timer_room_Rezlive.ElapsedMilliseconds > processTimeout)
                        {
                            timer_room_Rezlive.Stop();
                            break;
                        }
                        double TotalPrice = 0;
                        DataRow[] Roomdetails_ids = Rezlive_Response.Tables["RoomDetails"].Select("hotel_id=" + Convert.ToInt16(Rezlive_Response.Tables["Hotel"].Rows[h]["hotel_id"]));
                        DataRow[] pricedata = Rezlive_Response.Tables["RoomDetail"].Select("RoomDetails_id=" + Convert.ToInt32(Roomdetails_ids[0]["RoomDetails_id"]));
                        if (pricedata.Length > 0)
                        {
                            string[] PriceList = Convert.ToString(pricedata[0]["totalRate"]).Split(new String[] { "|" }, StringSplitOptions.None);
                            for (int pr = 0; pr < PriceList.Length; pr++)
                            {
                                if (pr == 0)
                                    TotalPrice = Convert.ToDouble(PriceList[pr]);
                                else
                                    TotalPrice = Convert.ToDouble(TotalPrice + Convert.ToDouble(PriceList[pr]));
                            }

                            if (TotalPrice == 0)
                            {
                                item.ResponseType = "NoCount";
                                continue;
                            }

                            //DataRow drXmlHotel = DtHotel.NewRow();
                            AgentPercentage = 0.0;
                            double AgentMarkupAmount = 0.0;
                            Int64 GiataCode = Convert.ToInt64(giitaCode);
                            if (SearchParam.Filters.IsShowRooms == 0)
                            {


                                Int64 RaynaID = Convert.ToInt64(Rezlive_Response.Tables["Hotel"].Rows[h]["Id"]);

                                DataRow drXmlHotel = DtHotel.NewRow();
                                drXmlHotel["ContractID"] = 0;
                                drXmlHotel["SupplierId"] = SupplierID;
                                drXmlHotel["HotelName"] = Convert.ToString(Rezlive_Response.Tables["Hotel"].Rows[h]["Name"]);
                                drXmlHotel["UniqueNo"] = Convert.ToString(Rezlive_Response.Tables["Hotel"].Rows[h]["Id"]);
                                drXmlHotel["Rating"] = 0;
                                Int64 OwnsystemHotelId = 0;
                                drXmlHotel["XMLHotelCode"] = Convert.ToString(Rezlive_Response.Tables["Hotel"].Rows[h]["Id"]);
                                drXmlHotel["SupplierName"] = Provider;
                                drXmlHotel["SupplierCurrency"] = currency;
                                double Pricenew = TotalPrice * CurrencyFactorRezlive;
                                drXmlHotel["FinalSellingPrice"] = Pricenew;
                                drXmlHotel["SellingPrice"] = Pricenew;

                                DtHotel.Rows.Add(drXmlHotel);
                                double FinalMinPrice = Pricenew;
                                if (FinalMinPrice > 0)
                                {
                                    #region "Room code Block"
                                    bool willAddOrNot = true;
                                    Int64 GiataId = 0;
                                    int RatingMain = 0;
                                    //Int64 OwnsystemHotelId = 0;
                                    double price = FinalMinPrice;
                                    double CompanyBranchBuyingPrice = 0;
                                    double CompanyBranchSellingPrice = 0;
                                    int Rating = 0;
                                    double MinPrice = FinalMinPrice;
                                    _markup.GetHotelDataCommon(ref price, ref willAddOrNot, ref OwnsystemHotelId, ref RaynaID, ref GiataId, ref RatingMain, ref drXmlHotel, commonSearch.AgentId, commonSearch.TariffID, commonSearch.dsAgentDetails, commonSearch.supplierGiataMaster, SearchParam, commonSearch.dtCountryCityMarkup, commonSearch.B2CMarkup, commonSearch.IsParentId, commonSearch.SubAgentMarkup, commonSearch.SearchByHotel, commonSearch.HotelIDS);
                                    if (willAddOrNot)
                                    {
                                        #region "Room Logic"
                                        DataTable dt_HotelRoom = _commonMethod.GenerateRoomtypeDataTableNewV2();
                                        DataTable dt_Cancellation = _commonMethod.GenerateCancellationDataTableNewV2();
                                        DataTable dt_RateBreackUp = _commonMethod.GenerateoomRateBreackUpDataTableNewV2();
                                        #endregion
                                        decimal LowestPrice = Convert.ToDecimal(price).CalculatePrice(commonSearch.tokenData, commonSearch.advancedOptions, currency);
                                        Options ops1 = (await _supplierRes.GetCommonRoomForSuppliers(Convert.ToInt64(GiataId), commonSearch.tokenData, commonSearch.advancedOptions, Convert.ToDouble(LowestPrice), drXmlHotel, dt_HotelRoom, dt_Cancellation, dt_RateBreackUp, SearchParam));
                                        if (ops1 != null)
                                            options.Add(ops1);
                                    }
                                    #endregion
                                }


                            }
                            else
                            {
                                Int64 RaynaID = Convert.ToInt64(Rezlive_Response.Tables["Hotel"].Rows[h]["Id"]);

                                DataRow drXmlHotel = DtHotel.NewRow();
                                drXmlHotel["ContractID"] = 0;
                                drXmlHotel["SupplierId"] = SupplierID;
                                drXmlHotel["HotelName"] = Convert.ToString(Rezlive_Response.Tables["Hotel"].Rows[h]["Name"]);
                                drXmlHotel["GIITACode"] = GiataCode;
                                drXmlHotel["UniqueNo"] = Convert.ToString(Rezlive_Response.Tables["Hotel"].Rows[h]["Id"]);
                                drXmlHotel["RaynaId"] = RaynaID;
                                drXmlHotel["OwnsystemHotelId"] = RaynaID;
                                drXmlHotel["Rating"] = 0;
                                Int64 OwnsystemHotelId = 0;
                                drXmlHotel["IsRecomondedHotel"] = true;
                                drXmlHotel["VatdiffAmount"] = 0;
                                drXmlHotel["XMLHotelCode"] = Convert.ToString(Rezlive_Response.Tables["Hotel"].Rows[h]["Id"]);
                                drXmlHotel["SupplierName"] = Provider;
                                drXmlHotel["SupplierCurrency"] = currency;
                                double Pricenew = TotalPrice * CurrencyFactorRezlive;
                                drXmlHotel["FinalSellingPrice"] = Pricenew;
                                drXmlHotel["SellingPrice"] = Pricenew;

                                DtHotel.Rows.Add(drXmlHotel);
                                double FinalMinPrice = Pricenew;
                                //if (FinalMinPrice > 0)
                                {
                                    #region "Room code Block"
                                    bool willAddOrNot = true;
                                    //Int64 GiataId = 0;
                                    int RatingMain = 0;
                                    //Int64 OwnsystemHotelId = 0;
                                    //double price = 0;
                                    //price = FinalMinPrice;
                                    Int64 GiataId = 0;
                                    int RatingHotel = 0;
                                    double MinPrice = Pricenew;
                                    double CompanyBranchBuyingPrice = 0;
                                    double CompanyBranchSellingPrice = 0;
                                    _markup.GetHotelDataCommon(ref Pricenew, ref willAddOrNot, ref OwnsystemHotelId, ref RaynaID, ref GiataCode, ref RatingMain, ref drXmlHotel, commonSearch.AgentId, commonSearch.TariffID, commonSearch.dsAgentDetails, commonSearch.supplierGiataMaster, SearchParam, commonSearch.dtCountryCityMarkup, commonSearch.B2CMarkup, commonSearch.IsParentId, commonSearch.SubAgentMarkup, commonSearch.SearchByHotel, commonSearch.HotelIDS);

                                    if (willAddOrNot)
                                    {
                                        #region "Room Logic"
                                        DataTable DtRezlive_HotelRoom = _commonMethod.GenerateRoomtypeDataTableNewV2();
                                        DataTable DtRezlive_Cancellation = _commonMethod.GenerateCancellationDataTableNewV2();
                                        DataTable DtRezlive_RateBreackUp = _commonMethod.GenerateoomRateBreackUpDataTableNewV2();

                                        Boolean isPerce = true;
                                        bool blnIsShowSupplier = false;
                                        //_markup.getAgetMarkupSetting(Provider, GiataId.ToString(), commonSearch.dtSuplierMarkup, commonSearch.dtMarkupDetails, ref AgentPercentage, ref AgentMarkupAmount, ref blnIsShowSupplier, ref isPerce, commonSearch.AgentId, RatingHotel, SearchParam.CityID, SearchParam.CountryID.ToString(), SearchParam.CheckInDate);
                                        _markup.getAgetMarkupSettingNew_new(Provider, Convert.ToString(RaynaID), commonSearch.dtSuplierMarkup, commonSearch.dsMarkupDetails, ref AgentPercentage, ref AgentMarkupAmount, ref blnIsShowSupplier, ref isPerce, commonSearch.AgentId, RatingHotel, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), SearchParam.CheckInDate);
                                        int CutOffDays = CacheMaster.CutOffDays;
                                        CutOffDays = _commonMethod.GetCutOffDays(commonSearch.dtSuplierMarkup, item.Supplier);

                                        //if (Rezlive_Response.Tables["HotelFindResponse"] != null)
                                        {
                                            //for (int j = 0; j < Rezlive_Response.Tables["HotelFindResponse"].Rows.Count; j++)
                                            {


                                                //if (Rezlive_Response.Tables.Contains("HotelFindResponse"))
                                                //{
                                                //    if (Rezlive_Response.Tables["HotelFindResponse"].Columns.Contains("SearchSessionId"))
                                                //        SearchSessionId = Convert.ToString(Rezlive_Response.Tables["HotelFindResponse"].Rows[j]["SearchSessionId"]);
                                                //    else
                                                //        continue;
                                                //}
                                                //else
                                                //    continue;
                                                double totalPrice = 0;
                                                if (pricedata.Length > 0)
                                                {
                                                    //for (int i = 0; i < Rezlive_Response.Tables["Hotel"].Rows.Count; i++)
                                                    {
                                                        //string PropertyID = Convert.ToString(Rezlive_Response.Tables["Hotel"].Rows[i]["Id"]);

                                                        //for (int k = 0; k < Rezlive_Response.Tables["RoomDetail"].Rows.Count; k++)
                                                        for (int k = 0; k < pricedata.Length; k++)
                                                        {
                                                            if (ProccessCutOffTime == "1" && timer_room_Rezlive.ElapsedMilliseconds > processTimeout)
                                                            {
                                                                timer_room_Rezlive.Stop();
                                                                break;
                                                            }
                                                            #region DataRaw and Variable Declaration

                                                            int RoomDetail_Id = Convert.ToInt32(pricedata[k]["RoomDetail_Id"]);

                                                            int TotalAdult = 0;
                                                            int TotalChild = 0;
                                                            int noofroom = Convert.ToInt32(pricedata[k]["TotalRooms"]);

                                                            string BoardID = Convert.ToString(pricedata[k]["BoardBasis"]);
                                                            string[] BoardtypeID = Convert.ToString(BoardID).Split(new String[] { "|" }, StringSplitOptions.None);
                                                            //BoardID = BoardtypeID[0];
                                                            //add on 04-08-2025
                                                            if (BoardtypeID.All(x => x.ToLower() == "breakfast"))
                                                            {
                                                                BoardID = "Breakfast";
                                                            }
                                                            else
                                                            {
                                                                BoardID = "Room Only";
                                                            }
                                                            string RoomPrice = Convert.ToString(pricedata[k]["TotalRate"]);
                                                            string[] RoomPriceList = Convert.ToString(RoomPrice).Split(new String[] { "|" }, StringSplitOptions.None);
                                                            double RoomTotalPrice = 0;
                                                            for (int pr = 0; pr < RoomPriceList.Length; pr++)
                                                            {
                                                                if (pr == 0)
                                                                    RoomTotalPrice = Convert.ToDouble(RoomPriceList[pr]);
                                                                else
                                                                    RoomTotalPrice = Convert.ToDouble(RoomTotalPrice + Convert.ToDouble(RoomPriceList[pr]));
                                                            }
                                                            string BookingKey = Convert.ToString(pricedata[k]["BookingKey"]);
                                                            string TotAdult = Convert.ToString(pricedata[k]["Adults"]);
                                                            string[] NoOfAdults = Convert.ToString(TotAdult).Split(new String[] { "|" }, StringSplitOptions.None);
                                                            for (int TH = 0; TH < NoOfAdults.Length; TH++)
                                                            {
                                                                if (TH == 0)
                                                                    TotalAdult = Convert.ToInt16(NoOfAdults[TH]);
                                                                else
                                                                    TotalAdult = Convert.ToInt16(TotalAdult + Convert.ToInt16(NoOfAdults[TH]));
                                                            }
                                                            string TotChild = Convert.ToString(pricedata[k]["Children"]);
                                                            string[] NoOfChilds = Convert.ToString(TotChild).Split(new String[] { "|" }, StringSplitOptions.None);
                                                            for (int TC = 0; TC < NoOfChilds.Length; TC++)
                                                            {
                                                                if (TC == 0)
                                                                    TotalChild = Convert.ToInt16(NoOfChilds[TC]);
                                                                else
                                                                    TotalChild = Convert.ToInt16(TotalChild + Convert.ToInt16(NoOfChilds[TC]));
                                                            }
                                                            int Child = Convert.ToInt16(TotalChild);
                                                            string ChildrenAges = Convert.ToString(pricedata[k]["ChildrenAges"]);
                                                            string TypeNameRoom = Convert.ToString(pricedata[k]["Type"]);
                                                            //string[] Type = Convert.ToString(TypeNameRoom).Split(new String[] { "|" }, StringSplitOptions.None);
                                                            //TypeNameRoom = Type[0];
                                                            //if (Child > 0)
                                                            //{
                                                            //    string[] Type1 = Convert.ToString(TypeNameRoom).Split(new String[] { "-" }, StringSplitOptions.None);
                                                            //    TypeNameRoom = Type1[0];
                                                            //}
                                                            string RoomDescription = Convert.ToString(pricedata[k]["RoomDescription"]);
                                                            string[] Description = Convert.ToString(RoomDescription).Split(new String[] { "|" }, StringSplitOptions.None);
                                                            RoomDescription = Description[0];
                                                            totalPrice = Convert.ToDouble(RoomTotalPrice);
                                                            totalPrice = totalPrice * CurrencyFactorRezlive;
                                                            string RoomName = "";
                                                            int NoOfNight = SearchParam.NoofNights;
                                                            if (RoomDescription == "")
                                                            {
                                                                RoomName = TypeNameRoom;
                                                            }
                                                            else
                                                            {
                                                                RoomName = TypeNameRoom + "-" + RoomDescription;
                                                            }
                                                            string RoomTypeId = SearchSessionId + "$$$" + BookingKey + "$$$" + CityCode + "$$$" + ContryCode + "$$$" + XMLNationality + "$$$" + Convert.ToString(pricedata[k]["Adults"]) + "$$$" + Convert.ToString(pricedata[k]["Children"]) + "$$$" + ChildrenAges + "$$$" + Convert.ToString(pricedata[k]["Type"]).Replace("'", "#") + "$$$" + noofroom + "$$$" + Convert.ToString(Rezlive_Response.Tables["RoomDetail"].Rows[k]["TotalRate"]) + "$$$" + RoomDescription;

                                                            RoomTypeId = RoomTypeId.Replace("'", "");
                                                            #endregion

                                                            #region"Ratebreakup"
                                                            DateTime dtCheckInDateForRate = DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null);
                                                            #region Vat
                                                            Double VatOnRateBrackUp = 0;
                                                            double AgentCompanyPercentage = Convert.ToDouble(SearchParam.AgentCompanyPercentage);
                                                            bool IsB2C = Convert.ToBoolean(Convert.ToByte(SearchParam.IsB2BOrB2C));
                                                            //Boolean isPerce = true;
                                                            //double B2CMarkup = (IsB2C == false ? AgentCompanyPercentage : XMLHotelBookingCommon.B2CMarkup);
                                                            // if (IsB2C == true)
                                                            //AgentPercentage = 0;
                                                            //double RoomPrice1 = Common.ProcessHotelRateBreakupPrice_WithMarkup(1, SearchParam.CityID, SearchParam.CountryID, totalPrice / NoOfNight, commonSearch.B2CMarkup, commonSearch.IsParentId, isPerce, AgentPercentage, AgentMarkupAmount, commonSearch.SubAgentMarkup, ref VatOnRateBrackUp);
                                                            double VATTotalSurcharge = 0;
                                                            //double CompanyBranchBuyingPrice = 0;
                                                            //double CompanyBranchSellingPrice = 0;
                                                            //int AgentBranchId = commonSearch.tokenData.ABranchId;
                                                            double RoomPrice1 = _markup.ProcessHotelRateBreakupPrice_WithMarkup(1, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), totalPrice / NoOfNight, commonSearch.B2CMarkup, commonSearch.IsParentId, isPerce, AgentPercentage, AgentMarkupAmount, commonSearch.SubAgentMarkup, ref VatOnRateBrackUp);

                                                            #endregion
                                                            for (int night = 0; night < SearchParam.NoofNights; night++)
                                                            {
                                                                string rateWeekColumn = Convert.ToString(dtCheckInDateForRate.Date.AddDays(night).DayOfWeek).Substring(0, 3);
                                                                DataRow drRezlive_RateBreackUp = DtRezlive_RateBreackUp.NewRow();
                                                                drRezlive_RateBreackUp["RoomNo"] = Convert.ToString("1");
                                                                drRezlive_RateBreackUp["SupplierName"] = Provider;
                                                                drRezlive_RateBreackUp["OwnsystemHotelId"] = OwnsystemHotelId;
                                                                drRezlive_RateBreackUp["ContractID"] = 0;
                                                                drRezlive_RateBreackUp["RoomTypeName"] = RoomName;
                                                                drRezlive_RateBreackUp["RoomTypeId"] = RoomTypeId;
                                                                drRezlive_RateBreackUp["MealId"] = BoardID;
                                                                drRezlive_RateBreackUp["Meal"] = BoardID;
                                                                drRezlive_RateBreackUp["BookingStatus"] = "Available";
                                                                drRezlive_RateBreackUp["DateOfWeek"] = rateWeekColumn;
                                                                drRezlive_RateBreackUp["Date"] = dtCheckInDateForRate.Date.AddDays(night).Date;
                                                                drRezlive_RateBreackUp["Rate"] = RoomPrice1;
                                                                drRezlive_RateBreackUp["RateType"] = "A Rate"; //SearchParam.Rooms.Count == 1 ? "A Rate" : ((j + 1) + ". " + TypeNameRoom); 
                                                                drRezlive_RateBreackUp["SupplierCurrency"] = currency;
                                                                drRezlive_RateBreackUp["BuyingPrice"] = RoomPrice1;
                                                                DtRezlive_RateBreackUp.Rows.Add(drRezlive_RateBreackUp);
                                                                DtRezlive_RateBreackUp.AcceptChanges();
                                                            }
                                                            #endregion

                                                            #region Rooms
                                                            DataRow drRezliveRoom = DtRezlive_HotelRoom.NewRow();
                                                            drRezliveRoom["OwnSystemHotelId"] = OwnsystemHotelId;
                                                            drRezliveRoom["RoomTypeName"] = RoomName;
                                                            drRezliveRoom["RoomTypeCode"] = BookingKey;
                                                            drRezliveRoom["MealId"] = BoardID;
                                                            drRezliveRoom["MealName"] = BoardID;
                                                            drRezliveRoom["ContractID"] = 0;
                                                            drRezliveRoom["BoardType"] = BoardID;
                                                            drRezliveRoom["RoomTypeId"] = RoomTypeId;
                                                            drRezliveRoom["AdultsOccupancy"] = TotalAdult;
                                                            drRezliveRoom["ChildOccupancy"] = TotalChild;
                                                            drRezliveRoom["MaxExtraBed"] = "0";
                                                            drRezliveRoom["RoomNo"] = "1";
                                                            drRezliveRoom["RoomSequenceNo"] = 0;
                                                            drRezliveRoom["Type"] = "";
                                                            drRezliveRoom["OfficeId"] = "";
                                                            drRezliveRoom["BookingStatus"] = "Available";
                                                            drRezliveRoom["UniqueNo"] = "";
                                                            drRezliveRoom["OfferDescription"] = "";
                                                            drRezliveRoom["OfferCode"] = "";
                                                            drRezliveRoom["EssentialInformation"] = "";

                                                            double ItemPrice = Convert.ToDouble(totalPrice);

                                                            _markup.ProcessHotelRoom_WithMarkup(CurrencyFactorRezlive, currency, true, true, SearchParam.NoofNights, SearchParam.CityID, SearchParam.CountryCode, SearchParam.CheckInDate, SearchParam.CheckOutDate, ref ItemPrice, ref drRezliveRoom, isPerce, AgentPercentage, AgentMarkupAmount, commonSearch.SubAgentMarkup, commonSearch.B2CMarkup, commonSearch.IsParentId);

                                                            drRezliveRoom["SupplierName"] = Provider;
                                                            DtRezlive_HotelRoom.Rows.Add(drRezliveRoom);
                                                            #endregion

                                                            #region Cancellation Policy Bind
                                                            DateTime curDate = _commonMethod.GetTimeZoneDate(SearchParam.CurrentDate);
                                                            if (Rezlive_Response.Tables["CancellationPolicy"] != null)
                                                            {
                                                                DataRow[] CancellationPolicydr = Rezlive_Response.Tables["CancellationPolicy"].Select("RoomDetail_Id=" + RoomDetail_Id);
                                                                if (CancellationPolicydr.Length > 0)
                                                                {
                                                                    if (Convert.ToString(CancellationPolicydr[0]["Refundable"]).ToLower() == "yes")
                                                                    {
                                                                        DateTime Fromdate = DateTime.Now.Date;
                                                                        //DateTime Todate = Convert.ToDateTime(Rezlive_Response.Tables["CancellationPolicy"].Rows[k]["TillDate"]);

                                                                        string dateString = Convert.ToString(CancellationPolicydr[0]["TillDate"]);
                                                                        DateTime parsedDate = DateTime.ParseExact(dateString, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                                                                        string formattedDate = parsedDate.ToString("MM/dd/yyyy");
                                                                        DateTime Todate = DateTime.ParseExact(formattedDate, "MM/dd/yyyy", CultureInfo.InvariantCulture);

                                                                        double cancellationChargeAmount = totalPrice;
                                                                        #region Vat
                                                                        double VatOnPolicy = 0;
                                                                        double MainAgentCharge = cancellationChargeAmount;
                                                                        double CompanyChargeAmount = cancellationChargeAmount;
                                                                        cancellationChargeAmount = _markup.ProcessHotelCancelationPrice_WithMarkup(ref MainAgentCharge, SearchParam.NoofNights, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), cancellationChargeAmount, isPerce, commonSearch.B2CMarkup, AgentPercentage, AgentMarkupAmount, commonSearch.IsParentId, commonSearch.SubAgentMarkup, ref VatOnPolicy);
                                                                        #endregion

                                                                        if (Todate.AddDays(CutOffDays).Date > DateTime.Now.Date)
                                                                            Todate = Todate.AddDays(CutOffDays).Date;
                                                                        else
                                                                            Todate = DateTime.Now.Date;

                                                                        if (curDate.Date < Todate.Date)
                                                                        {
                                                                            double zero = 0;
                                                                            DtRezlive_Cancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref zero, DtRezlive_Cancellation.NewRow(), CurrencyFactorRezlive, currency, Provider, Convert.ToString(OwnsystemHotelId), Convert.ToString(1),
                                                                            RoomTypeId, "", BoardID, Fromdate, Todate, 0, 0, "", VatOnPolicy, "", "No", "No", Convert.ToString(pricedata[k]["TermsAndConditions"]), "", ""));
                                                                            DtRezlive_Cancellation.AcceptChanges();

                                                                            DtRezlive_Cancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref CompanyChargeAmount, DtRezlive_Cancellation.NewRow(), CurrencyFactorRezlive, currency, Provider, Convert.ToString(OwnsystemHotelId), Convert.ToString(1),
                                                                            RoomTypeId, "", BoardID, Todate.AddDays(1), DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null), MainAgentCharge, cancellationChargeAmount, "", VatOnPolicy, "", "YES", "No", Convert.ToString(pricedata[k]["TermsAndConditions"]), "", ""));
                                                                            DtRezlive_Cancellation.AcceptChanges();
                                                                        }
                                                                        else
                                                                        {
                                                                            DtRezlive_Cancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref CompanyChargeAmount, DtRezlive_Cancellation.NewRow(), CurrencyFactorRezlive, currency, Provider, Convert.ToString(OwnsystemHotelId), Convert.ToString(1),
                                                                            RoomTypeId, "", BoardID, curDate, DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null), MainAgentCharge, cancellationChargeAmount, "", VatOnPolicy, "", "YES", "No", Convert.ToString(pricedata[k]["TermsAndConditions"]), "", ""));
                                                                            DtRezlive_Cancellation.AcceptChanges();
                                                                        }
                                                                            
                                                                    }
                                                                    else
                                                                    {
                                                                        DateTime Fromdate = DateTime.Now.Date;
                                                                        double cancellationChargeAmount = totalPrice;
                                                                        #region Vat
                                                                        double VatOnPolicy = 0;
                                                                        double MainAgentCharge = cancellationChargeAmount;
                                                                        double CompanyChargeAmount = cancellationChargeAmount;
                                                                        cancellationChargeAmount = _markup.ProcessHotelCancelationPrice_WithMarkup(ref MainAgentCharge, SearchParam.NoofNights, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), cancellationChargeAmount, isPerce, commonSearch.B2CMarkup, AgentPercentage, AgentMarkupAmount, commonSearch.IsParentId, commonSearch.SubAgentMarkup, ref VatOnPolicy);
                                                                        #endregion

                                                                        DtRezlive_Cancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref CompanyChargeAmount, DtRezlive_Cancellation.NewRow(), CurrencyFactorRezlive, currency, Provider, Convert.ToString(OwnsystemHotelId), Convert.ToString(1),
                                                                        RoomTypeId, "", BoardID, Fromdate, DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null), MainAgentCharge, cancellationChargeAmount, "", VatOnPolicy, "", "YES", "No", Convert.ToString(pricedata[k]["TermsAndConditions"]), "", ""));
                                                                        DtRezlive_Cancellation.AcceptChanges();
                                                                    }
                                                                }
                                                            }
                                                            else
                                                            {
                                                                DateTime Fromdate = DateTime.Now.Date;
                                                                double cancellationChargeAmount = totalPrice;
                                                                #region Vat
                                                                double VatOnPolicy = 0;
                                                                double MainAgentCharge = cancellationChargeAmount;
                                                                double CompanyChargeAmount = cancellationChargeAmount;
                                                                cancellationChargeAmount = _markup.ProcessHotelCancelationPrice_WithMarkup(ref MainAgentCharge, SearchParam.NoofNights, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), cancellationChargeAmount, isPerce, commonSearch.B2CMarkup, AgentPercentage, AgentMarkupAmount, commonSearch.IsParentId, commonSearch.SubAgentMarkup, ref VatOnPolicy);
                                                                #endregion

                                                                DtRezlive_Cancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref CompanyChargeAmount, DtRezlive_Cancellation.NewRow(), CurrencyFactorRezlive, currency, Provider, Convert.ToString(OwnsystemHotelId), Convert.ToString(1),
                                                                RoomTypeId, "", BoardID, Fromdate, DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null), MainAgentCharge, cancellationChargeAmount, "", VatOnPolicy, "", "YES", "No", Convert.ToString(pricedata[k]["TermsAndConditions"]), "", ""));
                                                                DtRezlive_Cancellation.AcceptChanges();
                                                            }
                                                            #endregion
                                                        }
                                                    }
                                                }
                                                #endregion
                                                decimal LowestPrice = Convert.ToDecimal(Pricenew).CalculatePrice(commonSearch.tokenData, commonSearch.advancedOptions, currency);
                                                //(this decimal price, TokenData tokenData, AdvancedOptions advancedOptions, string SupplierCurrency, int DecimalDigitRound = 2)
                                                Options ops1 = (await _supplierRes.GetCommonRoomForSuppliers(GiataCode, commonSearch.tokenData, commonSearch.advancedOptions, Convert.ToDouble(LowestPrice), drXmlHotel, DtRezlive_HotelRoom, DtRezlive_Cancellation, DtRezlive_RateBreackUp, SearchParam));
                                                if (ops1 != null)
                                                    options.Add(ops1);

                                            }
                                        }
                                    }
                                    #endregion
                                }


                            }
                        }
                    }
                    #endregion
                }
                else
                {
                    item.ResponseStatus = 204;
                    item.ResponseType = "NoCount";
                }
                //DtHotel.DefaultView.RowFilter = "FinalSellingPrice>0";
                //DtHotel.DefaultView.RowFilter = "Rating IN(" + strRating + ")";
                //return DtHotel.DefaultView.ToTable();
                return options;
            }
            catch (Exception ex)
            {
                item.ResponseStatus = 404;
                _ = _exception.Error(ex);
                return options;
            }
            finally
            {
                #region ResProcessTimer
                try
                {
                    timer_room_Rezlive.Stop();
                    item.CodeProcessingTimeSupplier = timer_room_Rezlive.ElapsedMilliseconds;
                    if (SearchParam != null && ResProccessTimerLogActive == "1" && timer_room_Rezlive.ElapsedMilliseconds > Convert.ToInt32(ResProccessTimeValueforSaveLog))
                    {

                        SearchParam.SupplierTime += ",|Finish:Adonis Process Response  " + "|Time :" + timer_room_Rezlive.Elapsed.ToString();
                        _ = Task.Run(() => _commonMethod.FireAway_SQL_ResPorcessTimeLog("", timer_room_Rezlive.Elapsed.ToString(), item.Supplier, SearchParam.SearchKey, 1, SearchParam.AgentID));
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

        public async Task<DataSet> Get_Rezlive_CancellationPolicy(string rating, string incommingofficecode, string Type, DataSet dsHotel, string hotelcode, string ownsystemid, string roomtypecode, string RoomTypeId_Own, double RoomTotalPrice, string MealID, string Board_Text, DataTable dtAgentDetail, DataTable dtSuplierMarkup, DataTable dtSearchConfig, HotelSearchParameter objSearPera, string PaxPassport, string roomno, string Provider, int AgentId, DataTable dtMarkupDetails,TokenData tokenData, string GiataId = "0", int hotelRating = 0)
        {
            try
            {
                DataTable dtLOH_W2M_Cancellation = _commonMethod.GenerateCancellationDataTableNewV2();
              
                #region Cancellation Policy RQ
                Boolean isPerce = true;
                double AgentCompanyPercentage = Convert.ToDouble(objSearPera.AgentCompanyPercentage);
                bool IsB2C = Convert.ToBoolean(Convert.ToByte(objSearPera.IsB2BOrB2C));
                double B2CMarkup = (IsB2C == false ? AgentCompanyPercentage : XMLHotelBookingCommon.B2CMarkup);
                double AgentPercentage = 0.0;
                double AgentMarkupAmount = 0.0;
                double SubAgentMarkup = 0;
                int IsParentId = 1;
                Boolean blnIsShowSupplier = false;
                _markup.getAgetMarkupSetting(Provider, GiataId, dtSuplierMarkup, dtMarkupDetails, ref AgentPercentage, ref AgentMarkupAmount, ref blnIsShowSupplier, ref isPerce, AgentId, hotelRating, objSearPera.CityID,Convert.ToString(objSearPera.CountryID), Convert.ToString(objSearPera.CheckInDate));
                if (dtAgentDetail != null)
                {
                    IsParentId = Convert.ToInt32(dtAgentDetail.Rows[0]["IsParentId"]);
                    SubAgentMarkup = Convert.ToDouble(dtAgentDetail.Rows[0]["B2CMarkupPer"]);
                }
                int CutOffDays = XMLHotelBookingCommon.CutOffDays;
                CutOffDays = _commonMethod.GetCutOffDays(dtSuplierMarkup, "Rezlive");
                DataRow[] drSuplierMarkup = (dtSuplierMarkup.Select("SupplierName='" + Provider + "'"));
                string UserId = Convert.ToString(drSuplierMarkup[0]["UserId"]);
                string UserName = Convert.ToString(drSuplierMarkup[0]["UserName"]);
                string Password = Convert.ToString(drSuplierMarkup[0]["Password"]);
                string RezliveCurrency = Convert.ToString(drSuplierMarkup[0]["CurrencyShortName"]);
                double CurrencyFactorRezlive = 1;
                if (RezliveCurrency.ToLower() != tokenData.SiteCurrency.ToLower())
                {
                    CurrencyFactorRezlive = _commonMethod.GetROEByCurrency(RezliveCurrency, tokenData.dtROE);
                }
                string CheckIn = objSearPera.CheckInDate;
                CheckIn = CheckIn.Substring(0, 2) + "/" + CheckIn.Substring(3, 2) + "/" + CheckIn.Substring(6, 4);
                string CheckOut = objSearPera.CheckOutDate;
                CheckOut = CheckOut.Substring(0, 2) + "/" + CheckOut.Substring(3, 2) + "/" + CheckOut.Substring(6, 4);
                string RezliveUrl = Convert.ToString(drSuplierMarkup[0]["WebSiteURL"]) + RezliveStaticInfo.prebook;
                int SID = Convert.ToInt32(drSuplierMarkup[0]["SupplierID"]);
                DataRow[] drSearchConfig = (dtSearchConfig.Select("SupplierID='" + SID + "'"));
                int SearchTimeOut = Convert.ToInt32(drSearchConfig[0]["BookingConfirmationTimeout"]);
                string PropertyId = incommingofficecode;
                int NoOfNights = objSearPera.NoofNights;
                string RoomName = "";
                int RoomNo = Convert.ToInt32(roomno);
                double MainAgentCha = RoomTotalPrice;
                double CompanyChargeAmount1 = RoomTotalPrice;
                double ChargeAmount;
                string[] SessionId = RoomTypeId_Own.Split(new String[] { "$$$" }, StringSplitOptions.None);
                string SessionIdMain = Convert.ToString(SessionId[0]);


                string CancelPolicyRQ = Get_Rezlive_PriceCheckRQ(UserId, UserName, Password, hotelcode, RoomTypeId_Own, CheckIn, CheckOut, roomtypecode, RezliveCurrency);

                #endregion

                if (CancelPolicyRQ != null)
                {
                    #region Cancellation Policy RS
                    string sentData = CancelPolicyRQ;
                    string Error = "";
                    ResponseDto responseDto = await Rezlive_Response_Supplier_new(sentData, RezliveUrl, SearchTimeOut, Provider, "");
                    string ResponseData = responseDto.Response;
                    Error = responseDto.Error;
                    _commonMethod.ThirdPartyGetPolicyHistory(Convert.ToString(RoomTypeId_Own), Convert.ToString(ownsystemid), CancelPolicyRQ, Convert.ToString(ResponseData), "After Get Policy", "Rezlive", "Response", "-", "");

                    if (ResponseData == string.Empty || ResponseData == "" || ResponseData == null)
                        return dsHotel;

                    DataSet LOH_Rezlive_Response = _commonMethod.GetDataSetFromXMLString(ResponseData);

                    #endregion
                    {
                        string terms = "";
                        if (LOH_Rezlive_Response.Tables.Contains("RoomDetails"))
                        {
                            if (LOH_Rezlive_Response.Tables.Contains("RoomDetail"))
                            {
                                if (LOH_Rezlive_Response.Tables["RoomDetail"].Columns.Contains("TermsAndConditions"))
                                {
                                    terms = "<b>Terms And Conditions:</b><br>" + Convert.ToString(LOH_Rezlive_Response.Tables["RoomDetail"].Rows[0]["TermsAndConditions"]);
                                }
                            }
                        }
                        string Info = Convert.ToString(LOH_Rezlive_Response.Tables["CancellationInformations"].Rows[0]["Info"]);
                        string Description = Info + "<br>" + terms;
                        //string Description = Info;
                        {
                            DateTime StartDate = Convert.ToDateTime(LOH_Rezlive_Response.Tables["CancellationInformation"].Rows[0]["StartDate"]);
                            DateTime EndDate = Convert.ToDateTime(LOH_Rezlive_Response.Tables["CancellationInformation"].Rows[0]["EndDate"]);
                            string ChargeType = Convert.ToString(LOH_Rezlive_Response.Tables["CancellationInformation"].Rows[0]["ChargeType"]);
                            ChargeAmount = Convert.ToDouble(LOH_Rezlive_Response.Tables["CancellationInformation"].Rows[0]["ChargeAmount"]);
                            string Currency = Convert.ToString(LOH_Rezlive_Response.Tables["CancellationInformation"].Rows[0]["Currency"]);

                            Double Penalty = 0;
                            Double Penalty1 = 0;
                            ChargeAmount = RoomTotalPrice * CurrencyFactorRezlive;
                            Penalty = ChargeAmount;
                            if (LOH_Rezlive_Response.Tables["CancellationInformation"].Rows.Count == 2)
                            {
                                ChargeAmount = Convert.ToDouble(LOH_Rezlive_Response.Tables["CancellationInformation"].Rows[0]["ChargeAmount"]);
                                //ChargeAmount = ChargeAmount * CurrencyFactorRezlive;
                                //Penalty1 = ChargeAmount;
                                if (ChargeType == "Amount")
                                {
                                    ChargeAmount = ChargeAmount * CurrencyFactorRezlive;
                                    Penalty1 = ChargeAmount;
                                }
                                else
                                {
                                    ChargeAmount = ChargeAmount * CurrencyFactorRezlive;
                                    ChargeAmount = Math.Round(ChargeAmount, 2);
                                    Penalty1 = ChargeAmount;
                                }
                            }
                            DateTime Canceldate = DateTime.Now;
                            {
                                EndDate = Convert.ToDateTime(LOH_Rezlive_Response.Tables["CancellationInformation"].Rows[0]["EndDate"]);
                                Canceldate = EndDate;
                            }

                            Canceldate = Canceldate.AddDays(CutOffDays).Date;

                            #region Vat
                            double VatOnPolicy = 0;
                            double MainAgentCharge = ChargeAmount;
                            double CompanyChargeAmount = ChargeAmount;
                            Penalty = _markup.ProcessHotelCancelationPrice_WithMarkup(ref MainAgentCharge, objSearPera.NoofNights, objSearPera.CityID,Convert.ToString(objSearPera.CountryID), Penalty, isPerce, B2CMarkup, AgentPercentage, AgentMarkupAmount, IsParentId, SubAgentMarkup, ref VatOnPolicy);

                            #endregion
                            if (LOH_Rezlive_Response.Tables["CancellationInformation"].Rows.Count == 2)
                            {
                                Penalty1 = _markup.ProcessHotelCancelationPrice_WithMarkup(ref MainAgentCharge, objSearPera.NoofNights, objSearPera.CityID, Convert.ToString(objSearPera.CountryID), Penalty1, isPerce, B2CMarkup, AgentPercentage, AgentMarkupAmount, IsParentId, SubAgentMarkup, ref VatOnPolicy);
                                DateTime NowDate = DateTime.Now.Date;
                                {
                                    if (NowDate.Date < EndDate.Date && Penalty1 == 0)
                                    {
                                        double zero = 0;

                                        dtLOH_W2M_Cancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref zero, dtLOH_W2M_Cancellation.NewRow(), CurrencyFactorRezlive, Currency, Provider, ownsystemid, Convert.ToString(roomno),
                                                                   Convert.ToString(RoomTypeId_Own), RoomName, MealID, DateTime.Now.Date, Convert.ToDateTime(Canceldate), Penalty1, Penalty1, "0", VatOnPolicy, "", "NO", SessionIdMain, Description, "", ""));
                                        dtLOH_W2M_Cancellation.AcceptChanges();


                                        StartDate = Convert.ToDateTime(LOH_Rezlive_Response.Tables["CancellationInformation"].Rows[1]["StartDate"]);



                                        dtLOH_W2M_Cancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref CompanyChargeAmount, dtLOH_W2M_Cancellation.NewRow(), CurrencyFactorRezlive, Currency, Provider, ownsystemid, Convert.ToString(roomno),
                                                                 Convert.ToString(RoomTypeId_Own), RoomName, MealID, Convert.ToDateTime(Canceldate.AddDays(1)), DateTime.ParseExact(objSearPera.CheckInDate, "dd/MM/yyyy", null), Penalty, Penalty, "0", VatOnPolicy, "", "Yes", SessionIdMain, Description, "", ""));
                                        dtLOH_W2M_Cancellation.AcceptChanges();
                                    }
                                    else
                                    {
                                        dtLOH_W2M_Cancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref CompanyChargeAmount, dtLOH_W2M_Cancellation.NewRow(), CurrencyFactorRezlive, Currency, Provider, ownsystemid, Convert.ToString(roomno),
                                                               Convert.ToString(RoomTypeId_Own), RoomName, MealID, DateTime.Now.Date, DateTime.ParseExact(objSearPera.CheckInDate, "dd/MM/yyyy", null), MainAgentCharge, Penalty, "0", 0, "", "Yes", SessionIdMain, Description));
                                        dtLOH_W2M_Cancellation.AcceptChanges();

                                    }
                                }
                            }
                            else
                            {
                                ChargeAmount = 0;
                                if (dtLOH_W2M_Cancellation.Rows.Count != 0)
                                    ChargeAmount = Convert.ToDouble(LOH_Rezlive_Response.Tables["CancellationInformation"].Rows[0]["ChargeAmount"]);
                                if (ChargeAmount < RoomTotalPrice)
                                {
                                    dtLOH_W2M_Cancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref CompanyChargeAmount, dtLOH_W2M_Cancellation.NewRow(), CurrencyFactorRezlive, Currency, Provider, ownsystemid, Convert.ToString(roomno),
                                                               Convert.ToString(RoomTypeId_Own), RoomName, MealID, DateTime.Now.Date, DateTime.ParseExact(objSearPera.CheckInDate, "dd/MM/yyyy", null), MainAgentCharge, Penalty, "0", 0, "", "Yes", SessionIdMain, Description));
                                    dtLOH_W2M_Cancellation.AcceptChanges();
                                }
                            }
                        }

                    }
                }

                dsHotel.Tables[2].Merge(dtLOH_W2M_Cancellation);

                return dsHotel;
            }
            catch (Exception ex)
            {
                _=_exception.ErrorNew(ex, ErrorLog._LogAPI.Recheck);
                return dsHotel;
            }
        }
        public string Get_Rezlive_PriceCheckRQ(string UserId, string UserName, string Password, string PropertyId, string RoomTypeId, string CheckIn, string CheckOut, string BookingKey, string RezliveCurrency)
        {
            try
            {
                #region Price Only and Room Only RQ
                // string RoomType1 = Common.Get_MGHoliday_RoomTypeID(RoomTypeId, ref PaxPassport);
                string[] DATA = RoomTypeId.Split(new String[] { "$$$" }, StringSplitOptions.None);
                string searchsessionid = DATA[0];
                string NationalityCode = DATA[4];
                string NoOfAdult = DATA[5];
                string Children = DATA[6];
                string ChildrenAges = DATA[7];
                string RoomType = DATA[8].Replace("#", "'");
                string Noofroom = DATA[9];
                string roomrate = DATA[10];
                string CityCode = DATA[2];
                string CountryCode = DATA[3];

                StringBuilder Rezlive_PC_RQ = new StringBuilder();
                Rezlive_PC_RQ.Append("<PreBookingRequest>");
                Rezlive_PC_RQ.Append("<Authentication><AgentCode>" + UserId + "</AgentCode><UserName>" + UserName + "</UserName><Password>" + Password + "</Password></Authentication>");
                Rezlive_PC_RQ.Append("<PreBooking>");
                Rezlive_PC_RQ.Append("<SearchSessionId>" + searchsessionid + "</SearchSessionId>");
                Rezlive_PC_RQ.Append("<ArrivalDate>" + CheckIn + "</ArrivalDate>");
                Rezlive_PC_RQ.Append("<DepartureDate>" + CheckOut + "</DepartureDate>");
                Rezlive_PC_RQ.Append("<GuestNationality>" + NationalityCode + "</GuestNationality>");
                Rezlive_PC_RQ.Append("<CountryCode>" + CountryCode + "</CountryCode>");
                Rezlive_PC_RQ.Append("<City>" + CityCode + "</City>");
                Rezlive_PC_RQ.Append("<HotelId>" + PropertyId + "</HotelId>");
                Rezlive_PC_RQ.Append("<Currency>" + RezliveCurrency + "</Currency>");
                Rezlive_PC_RQ.Append("<RoomDetails>");
                Rezlive_PC_RQ.Append("<RoomDetail>");
                Rezlive_PC_RQ.Append("<Type>" + RoomType + "</Type>");
                Rezlive_PC_RQ.Append("<BookingKey>" + BookingKey + "</BookingKey>");
                Rezlive_PC_RQ.Append("<Adults>" + NoOfAdult + "</Adults>");
                Rezlive_PC_RQ.Append("<Children>" + Children + "</Children>");
                Rezlive_PC_RQ.Append("<ChildrenAges>" + ChildrenAges + "</ChildrenAges>");
                Rezlive_PC_RQ.Append("<TotalRooms>" + Noofroom + "</TotalRooms>");
                Rezlive_PC_RQ.Append("<TotalRate>" + roomrate + "</TotalRate>");
                Rezlive_PC_RQ.Append("</RoomDetail>");
                Rezlive_PC_RQ.Append("</RoomDetails>");
                Rezlive_PC_RQ.Append("</PreBooking>");
                Rezlive_PC_RQ.Append("</PreBookingRequest>");
                return Rezlive_PC_RQ.ToString();
                #endregion
            }
            catch (Exception ex)
            {
                _ = _exception.ErrorNew(ex, ErrorLog._LogAPI.Recheck);
            }
            return string.Empty;

        }

        public async Task<ResponseDto> Rezlive_Response_Supplier_new(string Request_Text, string APIUrl, int SearchTimeOut, string SupplieName, string SearchKey = "", HotelSearchParameter para = null, bool DebugMode = false)
        {
            ResponseDto responseDto = new ResponseDto();
            string responseFromServer = "";
            string Error = "";
            try
            {
                //HttpWebRequest Request;
                WebRequest request = WebRequest.Create(APIUrl);
                request.Timeout = SearchTimeOut;
                request.Method = "POST";
                // Request.ContentType = "application/xml";
                request.ContentType = "application/x-www-form-urlencoded";
                //request.Headers.Add("Accept-Encoding", "gzip");
                ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(delegate { return true; });
                //string postData = "XML=" + HttpContext.Current.Server.UrlEncode(Request_Text);
                string postData = "XML=" + WebUtility.UrlEncode(Request_Text);
                byte[] byteArray = Encoding.UTF8.GetBytes(postData);
                request.ContentLength = byteArray.Length;
                Stream dataStream = request.GetRequestStream();
                dataStream.Write(byteArray, 0, byteArray.Length);
                //try
                //{
                WebResponse response = request.GetResponse();
                dataStream = response.GetResponseStream();
                //if (response.ContentEncoding.ToLower().Contains("gzip"))
                // dataStream = new System.IO.Compression.GZipStream(dataStream, System.IO.Compression.CompressionMode.Decompress);
                //else if (response.ContentEncoding.ToLower().Contains("deflate"))
                // dataStream = new System.IO.Compression.DeflateStream(dataStream, System.IO.Compression.CompressionMode.Decompress);
                StreamReader reader = new StreamReader(dataStream);
                responseFromServer = reader.ReadToEnd();
                dataStream.Close();
                reader.Close();
                response.Close();
                //}
                //catch (WebException webExcp)
                //{
                //    try
                //    {
                //        HttpWebResponse httpResponse = (HttpWebResponse)webExcp.Response;
                //        if (httpResponse.StatusDescription != null)
                //        {
                //            string result1 = new StreamReader(webExcp.Response.GetResponseStream()).ReadToEnd();
                //            return result1;
                //        }
                //    }
                //    catch (System.Exception ex)
                //    {
                //        return "";
                //    }
                //}
            }
            catch (Exception ex)
            {
                Error = ex.Message.ToString();
                _= _exception.ErrorBooking(ex, "", SupplieName, SearchKey);
            }

            responseDto.Response = responseFromServer;
            responseDto.Error = Error;
            return responseDto;
        }

        public async Task<PriceCheckRezliveDto> Get_Rezlive_PriceCheckRQRS(DataSet dsCartDetails, DataRow[] RezliveDetails, string PaxPassPort, int i, int j, DataTable dtCheckPriceReqStructure, string UniqueNo, string AgentUniqueNo, double FinalPrice, bool isForBooking, double CompanyBuyingPrice, string HasKey_IntCode, string IsPrcCheck_Book, string Supplier, int AgentId, TokenData tokenData)
        {
            PriceCheckRezliveDto priceCheckRezliveDto = new PriceCheckRezliveDto();
            string responsetype = "Success";
            string time = "";
            string SupplierName = Supplier;
            string Error = "";
            string xslRequest = "";
            string ResponseData = "";
            try
            {
                #region Price Check RQ and Declaration
                //DataTable dtInrCurrency = Common.GetCountryCurrency(XMLHotelBookingCommon.MGHolidayCurrency);
                //double CurrencyFactorMGHoliday = Convert.ToDouble(dtInrCurrency.Rows[0]["AmtToExch"]);
               
                DataSet dsTemp = new DataSet();
                string RezliveCurrency = Convert.ToString(RezliveDetails[0]["CurrencyShortName"]);
                double CurrencyFactorRezlive = 1;
                if (RezliveCurrency.ToLower() != tokenData.SiteCurrency.ToLower())
                {
                    CurrencyFactorRezlive = _commonMethod.GetROEByCurrency(RezliveCurrency, tokenData.dtROE);
                }

                string RezliveUrl = Convert.ToString(RezliveDetails[0]["WebSiteURL"]) + RezliveStaticInfo.prebook;
                string UserId = Convert.ToString(RezliveDetails[0]["UserId"]);
                string UserName = Convert.ToString(RezliveDetails[0]["UserName"]);
                string Password = Convert.ToString(RezliveDetails[0]["Password"]);
                string HotelId = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["XMLHotelId"]);
                string XmlCity = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["XMLCityId"]);
                string XmlCountry = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["XMLCountryId"]);
                int UserCartId = Convert.ToInt32(dsCartDetails.Tables[0].Rows[i]["UserCartID"]);
                int UserCartDetailId = Convert.ToInt32(dsCartDetails.Tables[1].Rows[j]["UserCartDetailId"]);
                string RoomTypeId = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomTypeID"]);
                //string SupplierName = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["SupplierName"]).ToLower();
                string CheckIn = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["CheckInDate"]);//"20180327"
                CheckIn = CheckIn.Substring(6, 2) + "/" + CheckIn.Substring(4, 2) + "/" + CheckIn.Substring(0, 4);
                string CheckOut = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["CheckOutDate"]);
                CheckOut = CheckOut.Substring(6, 2) + "/" + CheckOut.Substring(4, 2) + "/" + CheckOut.Substring(0, 4);
                string MealId = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["MealPlanCode"]);
                string RoomName = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomType"]);
                string[] spitid = RoomName.Split(new String[] { "-" }, StringSplitOptions.None);
                string SpitIdRoomName = spitid[0].ToString();
                string[] DATA = RoomTypeId.Split(new String[] { "$$$" }, StringSplitOptions.None);
                string BookingKey = DATA[1].ToString();
                //double TotalRate = Convert.ToDouble(dsCartDetails.Tables[1].DefaultView[j]["CompanyBuyingPrice"]);
                //int RoomNumber = Convert.ToInt16(dsCartDetails.Tables[1].DefaultView[j]["RoomNumber"]);
                //for
                //DataRow[] data = dsCartDetails.Tables[1].Select("UserCartId='" + UserCartId + "'");
                //int RoomNumber = Convert.ToInt32(data[j]["RoomNumber"]);
                if (BookingKey != "")
                {
                    //string xslRequest = Get_Rezlive_PriceCheckRQ(dsCartDetails, UserCartId, UserId, UserName, Password, PaxPassPort, HotelId, RoomTypeId, CheckIn, CheckOut, BookingKey, XmlCity, XmlCountry);
                    xslRequest = Get_Rezlive_PriceCheckRQ(UserId, UserName, Password, HotelId, RoomTypeId, CheckIn, CheckOut, BookingKey, RezliveCurrency);
                    _commonMethod.ThirdPartyBookingHistory(Convert.ToString(UniqueNo), Convert.ToString(AgentUniqueNo), xslRequest, "-", "Before get price " + Supplier + " request hit " + j + "is for Price Check" + isForBooking, Supplier, "response confirm", "-", "WithinEarth");
                    #endregion

                    if (xslRequest != "")
                    {
                        if (!isForBooking)
                        {
                            dtCheckPriceReqStructure.Rows.Add(_commonMethod.AddCheckPriceReq(dtCheckPriceReqStructure.NewRow(), j + 1, UserCartId, UserCartDetailId, RoomTypeId, Supplier, xslRequest));
                        }
                        #region Price Check RS

                        System.Diagnostics.Stopwatch timerroomprocess = new System.Diagnostics.Stopwatch();
                        timerroomprocess.Start();

                        ResponseDto responseDto = await Rezlive_Response_Supplier_new(xslRequest, RezliveUrl, 120000, Supplier, "");
                        ResponseData = responseDto.Response;
                        Error = responseDto.Error;

                        timerroomprocess.Stop();
                        time = timerroomprocess.Elapsed.ToString();

                        _commonMethod.ThirdPartyBookingHistory(Convert.ToString(UniqueNo), Convert.ToString(AgentUniqueNo), xslRequest, ResponseData + Error, "After get price " + Supplier + " request hit " + j + "is for Price Check" + isForBooking, Supplier, "response confirm", "-", "WithinEarth");

                        if (ResponseData == string.Empty)
                        {
                            try
                            {
                                responsetype = "GeneralError";
                                //int AgentId = Convert.ToInt32(Common.getSession("UserId") == "" ? "0" : Common.getSession("UserId"));
                                _ = _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, xslRequest, ResponseData, Error);
                            }
                            catch (Exception e)
                            {
                                _=_exception.ErrorNew(e, ErrorLog._LogAPI.Prebook);
                            }
                            priceCheckRezliveDto.Flag = false;
                            return priceCheckRezliveDto;
                        }
                        if (ResponseData.Contains("Error"))
                        {
                            try
                            {
                                responsetype = "GeneralError";
                                //int AgentId = Convert.ToInt32(Common.getSession("UserId") == "" ? "0" : Common.getSession("UserId"));
                                _ = _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, xslRequest, ResponseData, Error);
                            }
                            catch (Exception e)
                            {
                                _ = _exception.ErrorNew(e, ErrorLog._LogAPI.Prebook);
                                
                            }

                            priceCheckRezliveDto.Flag = false;
                            return priceCheckRezliveDto;
                        }
                        DataSet Rezlive_Response = _commonMethod.GetDataSetFromXMLString(ResponseData);
                        string pricecheck_BookingKey = "";
                        #endregion
                        if (Rezlive_Response.Tables.Contains("RoomDetail"))
                        {
                            pricecheck_BookingKey = Convert.ToString(Rezlive_Response.Tables["RoomDetail"].Rows[0]["BookingKey"]);
                        }

                        #region Price Check Logic

                        if (!Rezlive_Response.Tables.Contains("preBookingDetails"))
                        {
                            try
                            {
                                responsetype = "GeneralError";
                                //int AgentId = Convert.ToInt32(Common.getSession("UserId") == "" ? "0" : Common.getSession("UserId"));
                                _ = _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, xslRequest, ResponseData, Error);
                            }
                            catch (Exception e)
                            {
                                _ = _exception.ErrorNew(e, ErrorLog._LogAPI.Prebook);

                            }

                            priceCheckRezliveDto.Flag = false;
                            return priceCheckRezliveDto;
                        }
                        else
                        {
                            bool IsSuccess = await _commonMethod.GetbookingkeyUpdateStaus(pricecheck_BookingKey, UserCartId);
                            double AfterPrice = Convert.ToDouble(Rezlive_Response.Tables["preBookingDetails"].Rows[0]["BookingAfterPrice"]);
                            double BeforePrice = Convert.ToDouble(Rezlive_Response.Tables["preBookingDetails"].Rows[0]["BookingBeforePrice"]);
                            string Status = Convert.ToString(Rezlive_Response.Tables["preBookingDetails"].Rows[0]["Status"]);
                            if (Status != "True")
                            {
                                try
                                {
                                    responsetype = "GeneralError";
                                    //int AgentId = Convert.ToInt32(Common.getSession("UserId") == "" ? "0" : Common.getSession("UserId"));
                                    _ = _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, xslRequest, ResponseData, Error);
                                }
                                catch (Exception e)
                                {
                                    _ = _exception.ErrorNew(e, ErrorLog._LogAPI.Prebook);

                                }

                                priceCheckRezliveDto.Flag = true;
                                return priceCheckRezliveDto;
                            }
                            FinalPrice = AfterPrice * CurrencyFactorRezlive;
                            FinalPrice = Convert.ToDouble(Math.Round(FinalPrice, 2));

                            priceCheckRezliveDto.FinalPrice = FinalPrice;

                            if (Convert.ToDouble(Math.Round(FinalPrice.TruncateDecimalPlaces(0))) > Math.Round(CompanyBuyingPrice.TruncateDecimalPlaces(0)))
                            {
                                try
                                {
                                    ResponseData = "#Price Changed " + "#Main Price = " + CompanyBuyingPrice + " #New Price = " + FinalPrice + " #Response= " + ResponseData;
                                    responsetype = "GeneralError";
                                    //int AgentId = Convert.ToInt32(Common.getSession("UserId") == "" ? "0" : Common.getSession("UserId"));
                                    _ = _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, xslRequest, ResponseData, Error);
                                }
                                catch (Exception e)
                                {
                                    _ = _exception.ErrorNew(e, ErrorLog._LogAPI.Prebook);

                                }

                                priceCheckRezliveDto.Flag = true;
                                return priceCheckRezliveDto;
                            }
                            else
                            {
                                try
                                {
                                    responsetype = "Success";
                                    //int AgentId = Convert.ToInt32(Common.getSession("UserId") == "" ? "0" : Common.getSession("UserId"));
                                    _ = _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, xslRequest, ResponseData, Error);
                                }
                                catch (Exception e)
                                {
                                    _ = _exception.ErrorNew(e, ErrorLog._LogAPI.Prebook);

                                }
                                priceCheckRezliveDto.Flag = false;
                                return priceCheckRezliveDto;
                            }
                        }
                        #endregion
                    }
                    else
                    {
                        priceCheckRezliveDto.Flag = false;
                        return priceCheckRezliveDto;
                    }
                }
            }
            catch (Exception ex)
            {
                #region SupplierPerformanceReport Catch
                try
                {
                    //int AgentId = Convert.ToInt32(Common.getSession("UserId") == "" ? "0" : Common.getSession("UserId"));
                    Error = ex.Message.ToString() + "#=" + ex.StackTrace.ToString();
                    responsetype = "GeneralError";
                    _ = _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, xslRequest, ResponseData, Error);

                }
                catch (Exception e)
                {
                    _ = _exception.ErrorNew(e, ErrorLog._LogAPI.Prebook);

                }
                #endregion
                _ = _exception.ErrorNew(ex, ErrorLog._LogAPI.Prebook);

            }

            try
            {
                responsetype = "Success";
                //int AgentId = Convert.ToInt32(Common.getSession("UserId") == "" ? "0" : Common.getSession("UserId"));
                _ = _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype);
            }
            catch (Exception e)
            {
                _ = _exception.ErrorNew(e, ErrorLog._LogAPI.Prebook);

            }
            priceCheckRezliveDto.Flag = false;
            return priceCheckRezliveDto;
        }

        public string Get_Rezlive_BookingRequest(DataSet dsCartDetails, DataTable dtPassengers, DataRow[] RezliveDetails, string ExternalReference, bool IsPriceUpdate, int i, int j, string BookingKey)
        {
            try
            {
                #region Booking RQ
                string UserID = Convert.ToString(RezliveDetails[0]["UserId"]);
                string UserName = Convert.ToString(RezliveDetails[0]["UserName"]);
                string Password = Convert.ToString(RezliveDetails[0]["Password"]);
                string RezliveCurrency = Convert.ToString(RezliveDetails[0]["CurrencyShortName"]);
                string CheckIn = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["CheckInDate"]);
                CheckIn = CheckIn.Substring(6, 2) + "/" + CheckIn.Substring(4, 2) + "/" + CheckIn.Substring(0, 4);
                string CheckOut = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["CheckOutDate"]);
                CheckOut = CheckOut.Substring(6, 2) + "/" + CheckOut.Substring(4, 2) + "/" + CheckOut.Substring(0, 4);
                string RoomTypeId = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomTypeID"]);
                string[] DATA = RoomTypeId.Split(new String[] { "$$$" }, StringSplitOptions.None);
                string searchsessionid = DATA[0];
                string xmlcityid = DATA[2];
                string xmlcountryid = DATA[3];
                string NationalityCode = DATA[4];
                string noOfAdult = DATA[5];
                string Children = DATA[6];
                string ChildrenAges = DATA[7];
                string RoomType = DATA[8].Replace("#", "'");
                string Noofroom = DATA[9];
                int NoOfroom = Convert.ToInt16(Noofroom);
                string roomrate = DATA[10];
                string HotelId = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["XMLHotelId"]);
                int UserCartId = Convert.ToInt32(dsCartDetails.Tables[0].Rows[i]["UserCartID"]);
                double Price = Convert.ToDouble(dsCartDetails.Tables[1].DefaultView[j]["CompanyBuyingPrice"]);
                string RoomName = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["purchaseToken"]);
                string HotelName = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["ServiceName"]);

                StringBuilder bookrq = new StringBuilder();
                bookrq.Append("<BookingRequest>");
                bookrq.Append("<Authentication><AgentCode>" + UserID + "</AgentCode><UserName>" + UserName + "</UserName><Password>" + Password + "</Password></Authentication>");
                bookrq.Append("<Booking>");
                bookrq.Append("<SearchSessionId>" + searchsessionid + "</SearchSessionId>");
                bookrq.Append("<AgentRefNo>" + ExternalReference + "</AgentRefNo>");
                bookrq.Append("<ArrivalDate>" + CheckIn + "</ArrivalDate>");
                bookrq.Append("<DepartureDate>" + CheckOut + "</DepartureDate>");
                bookrq.Append("<GuestNationality>" + NationalityCode + "</GuestNationality>");
                bookrq.Append("<CountryCode>" + xmlcountryid + "</CountryCode>");
                bookrq.Append("<City>" + xmlcityid + "</City>");
                bookrq.Append("<HotelId>" + HotelId + "</HotelId>");
                bookrq.Append("<Name>" + Convert.ToString(HotelName).Replace("&", "and") + "</Name>");
                bookrq.Append("<Currency>" + RezliveCurrency + "</Currency>");
                bookrq.Append("<RoomDetails>");
                bookrq.Append("<RoomDetail>");
                bookrq.Append("<Type>" + RoomType + "</Type>");
                bookrq.Append("<BookingKey>" + BookingKey + "</BookingKey>");
                bookrq.Append("<Adults>" + noOfAdult + "</Adults>");
                bookrq.Append("<Children>" + Children + "</Children>");
                bookrq.Append("<ChildrenAges>" + ChildrenAges + "</ChildrenAges>");
                bookrq.Append("<TotalRooms>" + Noofroom + "</TotalRooms>");
                bookrq.Append("<TotalRate>" + roomrate + "</TotalRate>");

                for (int r = 0; r < NoOfroom; r++)//no of room loop
                {
                    int R = 0;
                    bookrq.Append("<Guests>");
                    dtPassengers.DefaultView.RowFilter = "PickUp='" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[0]["RoomTypeId"]) + "' and UserCartID='" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[0]["UserCartID"]) + "' and (RoomNo=" + (r + 1) + ")";
                    dtPassengers.DefaultView.Sort = "IsLead desc";
                    if (dtPassengers.DefaultView.Count > 0)
                    {
                        DataTable dtPax = dtPassengers.DefaultView.ToTable();
                        for (int k = 0; k < dtPax.Rows.Count; k++)
                        {
                            DataRow[] drPassengers = dtPax.Select("IsLead='1'");
                            if (drPassengers.Length == 0)
                            {
                                if (Convert.ToString(dtPax.Rows[k]["PaxType"]).Trim().ToLower() == "adult")
                                    dtPax.Rows[k]["IsLead"] = 1;
                                dtPax.AcceptChanges();

                            }
                            if (Convert.ToInt16(dtPax.Rows[k]["IsLead"]) == 1)
                            {
                                bookrq.Append("<Guest>");
                                bookrq.Append("<Salutation>" + Convert.ToString(dtPax.Rows[k]["Prefix"]).Replace(".", "") + "</Salutation>");
                                bookrq.Append("<FirstName>" + Convert.ToString(dtPax.Rows[k]["FirstName"]) + "</FirstName>");
                                bookrq.Append("<LastName>" + Convert.ToString(dtPax.Rows[k]["LastName"]) + "</LastName>");
                                bookrq.Append("</Guest>");
                            }
                            else
                            {
                                if (Convert.ToString(dtPax.Rows[k]["PaxType"]).Trim().ToLower() == "adult")
                                {
                                    bookrq.Append("<Guest>");
                                    bookrq.Append("<Salutation>" + Convert.ToString(dtPax.Rows[k]["Prefix"]).Replace(".", "") + "</Salutation>");
                                    bookrq.Append("<FirstName>" + Convert.ToString(dtPax.Rows[k]["FirstName"]) + "</FirstName>");
                                    bookrq.Append("<LastName>" + Convert.ToString(dtPax.Rows[k]["LastName"]) + "</LastName>");
                                    bookrq.Append("</Guest>");
                                }
                                else
                                {
                                    R = R + 1;
                                    bookrq.Append("<Guest>");
                                    bookrq.Append("<Salutation>" + Convert.ToString(dtPax.Rows[k]["PaxType"]) + "</Salutation>");
                                    bookrq.Append("<FirstName>" + Convert.ToString(dtPax.Rows[k]["FirstName"]) + "</FirstName>");
                                    bookrq.Append("<LastName>" + Convert.ToString(dtPax.Rows[k]["LastName"]) + "</LastName>");
                                    bookrq.Append("<IsChild>" + R + "</IsChild>");
                                    bookrq.Append("<Age>" + Convert.ToInt32(dtPax.Rows[k]["ChildAge"]) + "</Age>");
                                    bookrq.Append("</Guest>");
                                }
                            }
                        }
                    }
                    bookrq.Append("</Guests>");
                }
                bookrq.Append("</RoomDetail>");
                bookrq.Append("</RoomDetails>");
                bookrq.Append("</Booking>");
                bookrq.Append("</BookingRequest>");

                string BookingRQ = bookrq.ToString();
                return BookingRQ;
                #endregion
            }
            catch (Exception ex)
            {
                _ = _exception.ErrorNew(ex, ErrorLog._LogAPI.Book);
                return "";
            }
        }

        public string Get_Rezlive_BookingCancelRQ(string AgentVoucherNo, DataRow[] SupplierDetails)
        {
            string CancellationRQ = "";
            try
            {
                #region Booking Cancel RQ
                string UserID = Convert.ToString(SupplierDetails[0]["UserId"]);
                string UserName = Convert.ToString(SupplierDetails[0]["UserName"]);
                string Password = Convert.ToString(SupplierDetails[0]["Password"]);
                string BookingId = "";
                string BookingCode = "";

                string[] spitid = AgentVoucherNo.Split(new String[] { "|" }, StringSplitOptions.None);
                BookingId = spitid[0].ToString();
                BookingCode = spitid[1].ToString();

                StringBuilder CancelRQ = new StringBuilder();
                CancelRQ.Append("<CancellationRequest>");
                CancelRQ.Append("<Authentication><AgentCode>" + UserID + "</AgentCode><UserName>" + UserName + "</UserName><Password>" + Password + "</Password></Authentication>");
                CancelRQ.Append("<Cancellation>");
                CancelRQ.Append("<BookingId>" + BookingId + "</BookingId>");
                CancelRQ.Append("<BookingCode>" + BookingCode + "</BookingCode>");
                CancelRQ.Append("</Cancellation>");
                CancelRQ.Append("</CancellationRequest>");
                CancellationRQ = CancelRQ.ToString();
                #endregion
            }
            catch (Exception ex)
            {
                _ = _exception.ErrorNew(ex, ErrorLog._LogAPI.CancelBooking);
                return "";
            }
            return CancellationRQ;
        }
    }
}
