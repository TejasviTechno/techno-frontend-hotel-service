using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using XConnect.CommonUtilities;
using XConnect.CommonUtilities.Interface;
using XConnect.ErrorLogHandler.Interface;
using XConnect.Markup.Interface;
using XConnect.Models;
using XConnect.Models.Hotel;
using XConnect.Supplier.Restel.Interface;
using static Azure.Core.HttpHeader;
using static XConnect.Models.ErrorLog;
using Options = XConnect.Models.Options;

namespace XConnect.Supplier.Restel
{
    public class RestelRepo : IRestel
    {
        private readonly ICommonMethod _commonMethod;
        private readonly IException _exception;
        private readonly IMarkup _markup;
        private readonly ISupplierRes _supplierRes;
        private readonly KeySettings _keySettings;
        private readonly SupplierConfiguration _supplierConfig;//Added on 27-FEB-26



        public RestelRepo(ICommonMethod commonMethod, IMarkup markup, ISupplierRes supplierRes, IException exception, IOptions<KeySettings> keySettings, IOptions<SupplierConfiguration> supplierConfig)
        {
            _commonMethod = commonMethod;
            _markup = markup;
            _supplierRes = supplierRes;
            _exception = exception;
            _keySettings = keySettings.Value;
            _supplierConfig = supplierConfig.Value;
        }
        public void GetRestelRequestOnly_Async(DataRow[] commonCode, List<StarRating> listRating, HotelSearchParameter SearchPara, List<SupplierDetails> lstRestelDetails, List<SupplierSearchConfig> lstRestelSearchConfig, DataTable dtPreferredDataHotel, ref ConcurrentBag<HotelX> AllTask, int Hit, DataTable dtROE, TokenData tokenData, DataSet dtXMLCity)
        {
            try
            {
                List<StarRating> lstRating = new List<StarRating>();
                lstRating = _commonMethod.GetStarRatingStringList_xtg(SearchPara);

                List<HotelCodeByCity> DtHotelDAta = HtmlExtensions.ToList<HotelCodeByCity>(commonCode.CopyToDataTable());
                int ratingcount = lstRating.Count;
                int hotelcount = DtHotelDAta.Count;

                foreach (SupplierDetails RestelDetails in lstRestelDetails)
                {
                    try
                    {
                        int SupplierID = RestelDetails.SupplierID;
                        string Supplier = RestelDetails.SupplierName;

                        if (lstRestelSearchConfig.Count > 0)
                        {
                            List<SupplierSearchConfig> RestelSearchConfig = lstRestelSearchConfig
                                .Where(x => x != null && x.SupplierID == SupplierID)
                                .ToList();

                            if (RestelSearchConfig != null && RestelSearchConfig.Count > 0)
                            {
                                DataForSearch dataForSearch = _commonMethod.GetXmlDataForSearchTechwise(SearchPara.Nationality, dtXMLCity, Supplier, RestelDetails.TechName);

                                if (dataForSearch.XmlNationality != "")
                                {
                                    string Ids = string.Empty;
                                    string xmlbase = "";

                                    _commonMethod.GetPrefferedHotelCodes(ref Ids, ref xmlbase, ref RestelSearchConfig, Supplier, dtPreferredDataHotel, SearchPara.IsSearchFor);

                                    List<HotelCodeByCity> filter = new List<HotelCodeByCity>();
                                    filter = _commonMethod.FilterWithOutCity_Code(ratingcount, hotelcount, DtHotelDAta, lstRating, Supplier);


                                    string Currency = RestelDetails.CurrencyShortName;
                                    double CurrencyFactorRestel = 1;
                                    if (Currency.ToLower() != tokenData.SiteCurrency.ToLower())
                                    {
                                        CurrencyFactorRestel = _commonMethod.GetROEByCurrency(Currency, dtROE);
                                    }

                                    for (int k = 0; k < RestelSearchConfig.Count; k++)
                                    {
                                        bool DebugMode = (Convert.ToInt32(RestelSearchConfig[k].DebugMode) == 0 ? false : true);
                                        string PaxPassport = dataForSearch.XmlNationality;
                                        if (true)
                                        {

                                            if (Convert.ToInt32(RestelSearchConfig[k].CityID) == 0 || Convert.ToInt32(RestelSearchConfig[k].CityID) == SearchPara.CityID)//All City means 0
                                            {
                                                {
                                                    int Noofrecords = Convert.ToInt32(RestelSearchConfig[k].Noofrecords);
                                                    double count = Convert.ToDouble(filter.Count);
                                                    int NoOfDiv = Convert.ToInt32(Math.Ceiling(count / Convert.ToDouble(Noofrecords)));
                                                    List<List<XMLID_Search>> lstxml = _commonMethod.GetXMLIDString(NoOfDiv, Noofrecords, count, filter);
                                                    for (int a = 0; a < NoOfDiv; a++)
                                                    {

                                                        string IdsForSearch = Convert.ToString(_commonMethod.GetHotelCodeByCOmma(lstxml[a]));

                                                        HotelX xhotel = new HotelX();
                                                        xhotel.Supplier = Supplier;
                                                        xhotel.DebugeMode = DebugMode;
                                                        xhotel.SearchTimeOut = Convert.ToInt32(RestelSearchConfig[k].HotelSearchTimeOut);
                                                        //if (Hit > 0)
                                                        xhotel.ProcessTimeout = Convert.ToInt32(RestelSearchConfig[k].HotelSearchTimeOut2);
                                                        if (_keySettings.SupplierProxy == "0")
                                                            xhotel.URL = Convert.ToString(RestelDetails.WebSiteURL);
                                                        else
                                                            xhotel.URL = Convert.ToString(RestelDetails.WebSiteURLforAPI);

                                                        xhotel.UserId = Convert.ToString(RestelDetails.UserID);
                                                        xhotel.UserName = Convert.ToString(RestelDetails.UserName);
                                                        xhotel.PassWord = Convert.ToString(RestelDetails.Password);
                                                        xhotel.CurrencyCode = Currency;
                                                        xhotel.CurrencyFactor = CurrencyFactorRestel;
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
                                                            objrq.Request = "xml=" + XMLRestelRequestOnly_Async(Convert.ToString(RestelDetails.UserID), Convert.ToString(RestelDetails.UserName), Convert.ToString(RestelDetails.Password), Convert.ToInt32(RestelSearchConfig[k].HotelSearchTimeOut), SearchPara, "", DebugMode, dataForSearch.xmlCity, IdsForSearch, dataForSearch.XmlNationality);
                                                            objrq.Response = ""; RQRS.Add(objrq);
                                                        }
                                                        xhotel.RQRS = RQRS;
                                                        #endregion
                                                        xhotel.ReturnRooms = RestelDetails.ReturnRooms;
                                                        xhotel.TechName = RestelDetails.TechName;
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

        public string XMLRestelRequestOnly_Async(string UserID, string UserName, string Password, int SearchTimeOut, HotelSearchParameter SearchParam, string hotelIds, bool DebugMode, string citycode, string IdsForSearch, string Nationality)
        {
            try
            {
                int isHit = 0;
                return GetRestelHotelListRequest(SearchParam, citycode, UserID, UserName, Password, hotelIds, isHit, IdsForSearch, Nationality);
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

        public string GetRestelHotelListRequest(HotelSearchParameter searchParam, string citycode, string GateId, string UserName, string Password, string hotelIds, int IsHit, string IdsForSearch, string Nationality)
        {
            try
            {
                string checkIn = String.Format("{0:MM/dd/yyyy}", DateTime.ParseExact(searchParam.CheckInDate, "dd/MM/yyyy", null));
                string checkOut = String.Format("{0:MM/dd/yyyy}", DateTime.ParseExact(searchParam.CheckOutDate, "dd/MM/yyyy", null));

                StringBuilder roomstring = new StringBuilder();

                //IdsForSearch = "889954,415813,270988,138925,416317,365541,370310,323382,271620,100450,889952,100485,908568,477200,367467,453789,306586,150209,120026,120198,416241,889959,959633,906918,272777,068085,889900,342824,306674,067139,962985,272771,387976,889221,423630,444539,959632,436798,416409,120199,356711,907568,270393,305960,444534,904648,109420,270975,404896,805555";
                //IdsForSearch = "139721,745388,962243,601022,644551,636739";
                //IdsForSearch = "636739,994214,962243,601022,758213,752553,745388";
                //IdsForSearch = "745388";
                //IdsForSearch = IdsForSearch.Replace(',', '#');
                string newhotelid = "";
                if (IdsForSearch != "")
                {
                    string[] newids = IdsForSearch.Split(',');
                    for (int i = 0; i < newids.Length; i++)
                    {
                        newhotelid += newids[i] + "#";
                        if (i == 200)
                        {
                            break;
                        }
                    }
                }
                StringBuilder RSXML = new StringBuilder();
                RSXML.Append("<peticion><tipo>110</tipo><nombre></nombre><agencia></agencia><parametros>");
                //GWXML.Append("<hotel></hotel>");
                RSXML.Append("<hotel>" + newhotelid + "</hotel>");
                RSXML.Append("<pais></pais>");
                RSXML.Append("<provincia></provincia><poblacion></poblacion>");
                //if (Convert.ToString(searchParam.Nationality) == "India")
                //{ RSXML.Append("<pais_cliente>IN</pais_cliente>"); }
                //else if (Convert.ToString(searchParam.Nationality) == "China")
                //{ RSXML.Append("<pais_cliente>CN</pais_cliente>"); }
                //else if (Convert.ToString(searchParam.Nationality) == "Spain")
                //{ RSXML.Append("<pais_cliente>ES</pais_cliente>"); }
                //else
                //{
                RSXML.Append("<pais_cliente>" + Nationality + "</pais_cliente>");
                //}
                RSXML.Append("<categoria>0</categoria>");
                RSXML.Append("<radio>9</radio>");
                RSXML.Append("<fechaentrada>" + checkIn + "</fechaentrada>");
                RSXML.Append("<fechasalida>" + checkOut + "</fechasalida>");
                RSXML.Append("<marca></marca>");
                RSXML.Append("<afiliacion>RS</afiliacion>");
                RSXML.Append("<usuario>" + GateId + "</usuario>");

                for (int i = 0; i < searchParam.Rooms.Count; i++)
                {
                    int TotalAdult = searchParam.Rooms[i].NoofAdults;
                    int TotalChild = searchParam.Rooms[i].NoofChild;
                    string childage = "";
                    if (i == 0)
                    {
                        RSXML.Append("<numhab" + (i + 1) + ">" + searchParam.Rooms.Count + "</numhab" + (i + 1) + ">");
                        for (int k = 0; k < searchParam.Rooms.Count; k++)
                        {
                            if (k == searchParam.Rooms.Count - 1)
                                childage += searchParam.Rooms[i].Child1Age;
                            else
                                childage += searchParam.Rooms[i].Child1Age + ",";
                        }
                    }
                    else
                    {
                        RSXML.Append("<numhab" + (i + 1) + ">0</numhab" + (i + 1) + ">");
                    }
                    RSXML.Append("<paxes" + (i + 1) + ">" + TotalAdult + "-" + TotalChild + "</paxes" + (i + 1) + ">");

                    //for (int ch = 0; ch < TotalChild; ch++)
                    //{
                    //    if (ch == 0)
                    //        RSXML.Append("<edades" + (i + 1) + ">" + searchParam.Rooms[i].Child1Age + "</edades" + (i + 1) + ">");
                    //    if (ch == 1)
                    //        RSXML.Append("<edades" + (i + 1) + ">" + searchParam.Rooms[i].Child2Age + "</edades" + (i + 1) + ">");
                    //}
                    if (i == 0)
                    {
                        RSXML.Append("<edades" + (i + 1) + ">" + childage + "</edades" + (i + 1) + ">");
                    }
                    else
                    {
                        RSXML.Append("<edades" + (i + 1) + ">" + searchParam.Rooms[i].Child2Age + "</edades" + (i + 1) + ">");
                    }
                }
                RSXML.Append("<idioma>2</idioma>");
                RSXML.Append("<duplicidad>1</duplicidad>");
                RSXML.Append("<comprimido>2</comprimido>");
                RSXML.Append("<informacion_hotel>0</informacion_hotel>");
                RSXML.Append("<tarifas_reembolsables>1</tarifas_reembolsables>");
                if (searchParam.Filters.IsShowRooms == 1)
                {
                    RSXML.Append("<gastos>1</gastos>"); //Added by jay for get policy at room level
                }
                RSXML.Append("</parametros>");
                RSXML.Append("</peticion>");

                string RestelREQ = RSXML.ToString();
                return RestelREQ;
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
                return "";
            }
        }

        public async Task<List<Options>> Restel_HotelSearchWithRooms(string searchkey, HotelX Item, CommonSearch commonSearch, string currency, string ResponseData, string CityCode, HotelSearchParameter SearchParam, string ContryCode, double CurrencyFactorRestel, string Provider, string XmlNationality, int processTimeout, string ResProccessTimerLogActive, string ResProccessTimeValueforSaveLog)
        {
            System.Diagnostics.Stopwatch timerroomx = new System.Diagnostics.Stopwatch();
            timerroomx.Start();

            List<Options> options = new List<Options>();
            try
            {
                #region DataTable
                DataTable DtHotel = _commonMethod.GenerateDataTableNewV2();
                #endregion
                if (!string.IsNullOrEmpty(ResponseData))
                {
                    #region PriceOnly RS
                    ResponseData = ResponseData.Replace("<?xml version=\"1.0\" encoding=\"iso-8859-1\" ?>", "");
                    ResponseData = ResponseData.Replace("<!DOCTYPE respuesta SYSTEM \"http://xml.hotelresb2b.com/xml/dtd/res_disponibilidad_110.dtd\">", "");
                    ResponseData = ResponseData.Replace("<![CDATA[", "");
                    ResponseData = ResponseData.Replace("]]>", "");
                    ResponseData = ResponseData.Replace("\n", "");
                    ResponseData = ResponseData.Replace("\t", "");
                    ResponseData = ResponseData.Replace("&", "and");
                    DataSet Restel_Response = _commonMethod.GetDataSetFromXMLString(ResponseData);
                    if (Restel_Response == null || Restel_Response.Tables["hot"] == null)
                    {
                        Item.SupplierResponseError = ResponseData;
                        Item.ResponseStatus = 204;
                        Item.ResponseType = "NoCount";
                        return options;
                    }
                    #endregion

                    #region Hotel List
                    for (int h = 0; h < Restel_Response.Tables["hot"].Rows.Count; h++)
                    {
                        Item.ResponseType = "Success";
                        string ItemCode = Convert.ToString(Restel_Response.Tables["hot"].Rows[h]["cod"]);
                        string HotelName = Convert.ToString(Restel_Response.Tables["hot"].Rows[h]["nom"]);
                        int HotelId = Convert.ToInt32(Restel_Response.Tables["hot"].Rows[h]["hot_Id"]);
                        
                        double FinalMinPrice = 0;
                        #region "When not Need Room"  
                        // add hotel code from price only and room code blank here
                        if (SearchParam.Filters.IsShowRooms == 0)
                        {
                            #region "Hotel Code Block "
                            double TotalCharges = 0;
                            DataRow[] res = Restel_Response.Tables["res"].Select("hot_Id='" + HotelId + "'");
                            int res_Id = Convert.ToInt32(res[0]["res_Id"]);

                            DataRow[] drpax = Restel_Response.Tables["pax"].Select("res_Id = '" + res_Id + "'");

                            int pax_Id = Convert.ToInt32(drpax[0]["pax_Id"]);

                            DataRow[] drhab = Restel_Response.Tables["hab"].Select("pax_Id='" + pax_Id + "'");

                            if (drhab.Length > 0)
                            {
                                double regprice = 0;
                                double regpricenew = 0;
                                int regnr = 0;
                                string regesr = "";
                                DataRow[] drreg = null;
                                for (int hb = 0; hb < drhab.Length; hb++)
                                {
                                    drreg = Restel_Response.Tables["reg"].Select("hab_Id='" + Convert.ToInt32(drhab[hb]["hab_Id"]) + "'");
                                    
                                    regpricenew = Convert.ToDouble(drreg[0]["prr"]);
                                    if (hb != 0)
                                    {
                                        if (regpricenew < regprice)
                                        {
                                            regprice = regpricenew;
                                        }
                                    }
                                    else
                                    {
                                        regprice = regpricenew;
                                    }

                                    regnr = Convert.ToInt32(drreg[0]["nr"]);
                                    regesr = Convert.ToString(drreg[0]["esr"]);
                                    if (regesr != "OK")
                                        return options;


                                }
                                if (drreg.Length > 0)
                                {

                                    DataRow drXmlHotel = DtHotel.NewRow();
                                    drXmlHotel["XMLHotelCode"] = ItemCode;
                                    drXmlHotel["XMLCode"] = "";
                                    drXmlHotel["HotelName"] = "";
                                    drXmlHotel["SupplierName"] = Provider;
                                    drXmlHotel["OfficeID"] = 0;
                                    drXmlHotel["ContractID"] = 0;
                                    drXmlHotel["Rating"] = 0;
                                    drXmlHotel["XmlCity"] = CityCode;
                                    drXmlHotel["IsShowSupplier"] = false;
                                    drXmlHotel["BookingStatus"] = "Available";
                                    drXmlHotel["FinalSellingPrice"] = regprice;
                                    drXmlHotel["SellingPrice"] = regprice;
                                    DtHotel.Rows.Add(drXmlHotel);
                                    TotalCharges = regprice;
                                    #endregion
                                    if (TotalCharges > 0)
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
                                            decimal LowestPrice = Convert.ToDecimal(MinPrice).CalculatePrice(commonSearch.tokenData, commonSearch.advancedOptions, currency);
                                            Options ops = (await _supplierRes.GetCommonRoomForSuppliers(GiataId, commonSearch.tokenData, commonSearch.advancedOptions, Convert.ToDouble(LowestPrice), drXmlHotel, dt_HotelRoom, dt_Cancellation, dt_RateBreackUp, SearchParam));
                                            if (ops != null)
                                                options.Add(ops);
                                        }
                                        #endregion
                                    }
                                }
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
                            drXmlHotel["XMLHotelCode"] = ItemCode;
                            drXmlHotel["XMLCode"] = "";
                            drXmlHotel["HotelName"] = "";
                            drXmlHotel["SupplierName"] = Provider;
                            drXmlHotel["OfficeID"] = 0;
                            drXmlHotel["ContractID"] = 0;
                            drXmlHotel["Rating"] = 0;
                            drXmlHotel["XmlCity"] = CityCode;
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
                                if (SearchParam.Filters.IsShowRooms == 1)
                                {
                                    double AgentPercentage = 0.0;
                                    
                                    double AgentMarkupAmount = 0.0;
                                    Boolean isPerce = true;

                                    double AgentCompanyPercentage = Convert.ToDouble(SearchParam.AgentCompanyPercentage);
                                    bool IsB2C = Convert.ToBoolean(Convert.ToByte(SearchParam.IsB2BOrB2C));
                                    bool blnIsShowSupplier = false;
                                    //_markup.getAgetMarkupSetting(Provider, GiataId.ToString(), commonSearch.dtSuplierMarkup, commonSearch.dtMarkupDetails, ref AgentPercentage, ref AgentMarkupAmount, ref blnIsShowSupplier, ref isPerce, commonSearch.AgentId, RatingHotel, SearchParam.CityID, SearchParam.CountryID.ToString(), SearchParam.CheckInDate);

                                    _markup.getAgetMarkupSettingNew_new(Provider, Convert.ToString(RaynaID), commonSearch.dtSuplierMarkup, commonSearch.dsMarkupDetails, ref AgentPercentage, ref AgentMarkupAmount, ref blnIsShowSupplier, ref isPerce, commonSearch.AgentId, RatingHotel, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), SearchParam.CheckInDate);
                                    int maxOcupancy = 0;
                                    for (int i = 0; i < SearchParam.Rooms.Count; i++)
                                        maxOcupancy = maxOcupancy + SearchParam.Rooms[i].NoofChild + SearchParam.Rooms[i].NoofAdults;
                                    #region Supplier Room Code block
                                   
                                    if (Restel_Response.Tables["hotls"] != null)
                                    {
                                        {
                                            DataRow[] drpriceres = Restel_Response.Tables["res"].Select("hot_id='" + Convert.ToInt32(Restel_Response.Tables["hot"].Rows[h]["hot_Id"]) + "'");
                                           

                                            DataRow[] drpax = Restel_Response.Tables["pax"].Select("res_id='" + Convert.ToInt32(drpriceres[0]["res_Id"]) + "'");
                                           

                                            DataRow[] drhab = Restel_Response.Tables["hab"].Select("pax_Id = '" + Convert.ToInt32(drpax[0]["pax_Id"]) + "'");

                                            if (drhab.Length > 0)
                                            {

                                                for (int room = 0; room < drhab.Length; room++)
                                                {
                                                    //if (room > CacheMaster.RoomLimit_10)
                                                    //    break;


                                                    if (commonSearch.AgentId == 4440 || commonSearch.AgentId == 4901)
                                                    {
                                                        if (room > CacheMaster.RoomLimit_20)
                                                            break;
                                                    }
                                                    else
                                                    {
                                                        if (room > CacheMaster.RoomLimit_10)
                                                            break;
                                                    }

                                                    #region DataRaw and Variable Declaration
                                                    string Type = "id";


                                                    string RoomCode = "";
                                                    //RoomCode = guid + "|";
                                                    RoomCode = Convert.ToString(drhab[room]["cod"]);
                                                    if (RoomCode == "0")
                                                    {
                                                        Type = "token";
                                                    }
                                                 
                                                    string RoomName = Convert.ToString(drhab[room]["desc"]);//New

                                                    DataRow[] drreg = Restel_Response.Tables["reg"].Select("hab_Id = '" + Convert.ToInt32(drhab[room]["hab_Id"]) + "'");

                                                    if (drreg.Length > 0)
                                                    {
                                                        for (int ml = 0; ml < drreg.Length; ml++)
                                                        {
                                                            int reg_Id = Convert.ToInt32(drreg[ml]["reg_Id"]); //New

                                                            string BoardID = "";
                                                            if (Restel_Response.Tables["reg"].Columns.Contains("lin") == true)
                                                            {
                                                                BoardID = Convert.ToString(drreg[ml]["lin"]);
                                                            }
                                                            else
                                                            {
                                                                DataRow[] drlin = Restel_Response.Tables["lin"].Select("reg_Id = '" + Convert.ToInt32(drreg[ml]["reg_Id"]) + "'");

                                                                for (int lin = 0; lin < drlin.Length; lin++)
                                                                {
                                                                    BoardID += Convert.ToString(drlin[lin]["lin_Text"]) + "$$$";
                                                                }

                                                            }
                                                            RoomCode = Guid.NewGuid().ToString().Substring(0, 4) + "|" + BoardID + "@@@" + ItemCode;

                                                            string bkstatus = Convert.ToString(drreg[ml]["esr"]); //#add new change on 3-Mar-2024

                                                            string uniqid = Guid.NewGuid().ToString();
                                                            double CompanyBuying = Convert.ToDouble(Convert.ToString(drreg[ml]["prr"])) * CurrencyFactorRestel;
                                                            int NoOfAdult = Convert.ToInt32(Convert.ToString(Convert.ToString(drpax[0]["cod"])).Split('-')[0]);
                                                            int NoOfChild = Convert.ToInt32(Convert.ToString(Convert.ToString(drpax[0]["cod"])).Split('-')[1]);/*Convert.ToInt32(drhab[room]["Children"]);*/
                                                            int NoOfInfants = 0;/*Convert.ToInt32(drhab[room]["Infants"]);*/
                                                            string Offer_Name = "";
                                                            string Offer_Desc = "";
                                                            string Errata = "";
                                                            string Offer = "";


                                                            int SeqNo = 1;/*Convert.ToInt32(drhab[room]["Seq"]);*/
                                                            #endregion

                                                            //#region Offers
                                                            //if (Restel_Response.Tables["RoomType"].Columns.Contains("SpecialOfferApplied"))
                                                            //{
                                                            //    Offer = Convert.ToString(drhab[room]["SpecialOfferApplied"]);
                                                            //    Offer_Name = Offer == string.Empty ? "" : "Offer";
                                                            //    Offer_Desc = Offer == string.Empty ? "" : Offer;
                                                            //}
                                                            //#endregion

                                                            #region Logic For Essential Info

                                                            //if (Restel_Response.Tables["Errata"] != null)
                                                            //{
                                                            //    int RoomTypeId = Convert.ToInt32(drhab[0]["RoomType_Id"]);
                                                            //    DataRow[] drErrata = Restel_Response.Tables["Errata"].Select("RoomType_Id='" + RoomTypeId + "'");
                                                            //    int Errata_Id = Convert.ToInt32(drErrata[0]["Errata_Id"]);
                                                            //    DataRow[] drErratum = Restel_Response.Tables["Erratum"].Select("Errata_Id='" + Errata_Id + "'");
                                                            //    for (int z = 0; z < drErratum.Length; z++)
                                                            //    {
                                                            //        string desc = Convert.ToString(drErratum[z]["Description"]);
                                                            //        if (z == 0)
                                                            //            Errata = Errata + desc;
                                                            //        else
                                                            //            Errata = Errata + "<br>" + desc;
                                                            //    }

                                                            //}
                                                            if (Restel_Response.Tables["hot"].Columns.Contains("city_tax")) //Added on 5-AUG-2024
                                                            {
                                                                Errata = Convert.ToString(Restel_Response.Tables["hot"].Rows[h]["city_tax"]);
                                                            }
                                                            if (Errata == string.Empty)
                                                                Errata = "";
                                                            #endregion

                                                            #region RateBreakUp
                                                            int NoOfNight = SearchParam.NoofNights;
                                                            //for (int n = 0; n < NoOfNight; n++)
                                                            //{
                                                            //    //DateTime Day = Convert.ToDateTime(CheckIn);
                                                            //    //Day = Day.AddDays(n);
                                                            //    //string Days = Convert.ToString(Day);
                                                            //    ////Days = Days.Substring(6, 4) + "/" + Days.Substring(0, 2) + "/" + Days.Substring(3, 2);
                                                            //    ////string rateWeekColumn = Convert.ToString(Convert.ToDateTime(Days).Date.DayOfWeek).Substring(0, 3);
                                                            //    //Days = Days.Substring(3, 2) + "/" + Days.Substring(0, 2) + "/" + Days.Substring(6, 4);

                                                            //    //string rateWeekColumn = Convert.ToString(Convert.ToDateTime(Days).Date.DayOfWeek).Substring(0, 2);
                                                            //    DateTime Day = DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null);
                                                            //    Day = Day.AddDays(n);
                                                            //    string Days = Convert.ToString(Day);
                                                            //    string rateWeekColumn = Convert.ToString(Day.Date.DayOfWeek).Substring(0, 3);

                                                            //    #region Vat
                                                            //    Double VatOnRateBrackUp = 0;
                                                            //    double PriceRoom = _commonMethod.ProcessHotelRateBreakupPrice_WithMarkup(1, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), CompanyBuying / NoOfNight, commonSearch.B2CMarkup, commonSearch.IsParentId, isPerce, AgentPercentage, AgentMarkupAmount, commonSearch.SubAgentMarkup, ref VatOnRateBrackUp);
                                                            //    #endregion

                                                            //    //double PriceRoom = Common.getLowestOrBeforeDiscount(CompanyBuying, B2CMarkup, IsParentId, SubAgentMarkup, isPerce, AgentPercentage, AgentMarkupAmount);


                                                            //    //DataRow drJacTravel_RateBreackUp = DtRestel_RateBreackUp.NewRow();
                                                            //    //drJacTravel_RateBreackUp["SupplierName"] = "Restel";
                                                            //    //drJacTravel_RateBreackUp["OwnsystemHotelId"] = ownsystemhotelId;
                                                            //    //drJacTravel_RateBreackUp["ContractID"] = 0;
                                                            //    //drJacTravel_RateBreackUp["RoomTypeName"] = RoomName;
                                                            //    //drJacTravel_RateBreackUp["RoomTypeId"] = RoomCode;
                                                            //    //drJacTravel_RateBreackUp["MealId"] = BoardID;
                                                            //    //drJacTravel_RateBreackUp["Meal"] = Board_Text;
                                                            //    //drJacTravel_RateBreackUp["DateOfWeek"] = rateWeekColumn;
                                                            //    //drJacTravel_RateBreackUp["Date"] = Days;
                                                            //    //drJacTravel_RateBreackUp["Rate"] = PriceRoom;
                                                            //    //drJacTravel_RateBreackUp["RateType"] = "A Rate";
                                                            //    //drJacTravel_RateBreackUp["RoomNo"] = SeqNo;//rm + 1;
                                                            //    //drJacTravel_RateBreackUp["RoomSequenceNo"] = uniqid;
                                                            //    /////check booking status OK Or Not
                                                            //    //drJacTravel_RateBreackUp["BookingStatus"] = bkstatus == "OK" ? "Available" : "";
                                                            //    //drJacTravel_RateBreackUp["SupplierCurrency"] = Currency;
                                                            //    //drJacTravel_RateBreackUp["BuyingPrice"] = PriceRoom / Factor;
                                                            //    //DtRestel_RateBreackUp.Rows.Add(drJacTravel_RateBreackUp);
                                                            //}
                                                            #endregion

                                                            #region Rooms
                                                            DataRow drJacTravelRoom = dtHotelRoom.NewRow();
                                                            drJacTravelRoom["OwnSystemHotelId"] = OwnsystemHotelId;
                                                            drJacTravelRoom["RoomTypeName"] = Convert.ToString(drhab[room]["desc"]);
                                                            drJacTravelRoom["RoomTypeCode"] = RoomCode;
                                                            drJacTravelRoom["MealId"] = BoardID;
                                                            drJacTravelRoom["MealName"] = Convert.ToString(drreg[ml]["cod"]);
                                                            drJacTravelRoom["ContractID"] = 0;//Convert.ToInt32(Restel_Response.Tables["hot"].Rows[i]["cod"]);
                                                            drJacTravelRoom["BoardType"] = Convert.ToString(drreg[ml]["cod"]);
                                                            drJacTravelRoom["RoomTypeId"] = RoomCode;
                                                            drJacTravelRoom["AdultsOccupancy"] = NoOfAdult;
                                                            drJacTravelRoom["ChildOccupancy"] = NoOfChild + NoOfInfants;
                                                            drJacTravelRoom["MaxExtraBed"] = "0";
                                                            drJacTravelRoom["RoomNo"] = SeqNo;
                                                            drJacTravelRoom["RoomSequenceNo"] = uniqid;
                                                            drJacTravelRoom["Type"] = Type;
                                                            drJacTravelRoom["BookingStatus"] = bkstatus == "OK" ? "Available" : "";
                                                            drJacTravelRoom["UniqueNo"] = uniqid;
                                                            drJacTravelRoom["OfferDescription"] = Offer_Desc;
                                                            drJacTravelRoom["OfferCode"] = Offer_Name;
                                                            drJacTravelRoom["EssentialInformation"] = Errata;

                                                            double ItemPrice = Convert.ToDouble(CompanyBuying);

                                                            _markup.ProcessHotelRoom_WithMarkup(CurrencyFactorRestel, currency, true, true, SearchParam.NoofNights, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), SearchParam.CheckInDate, SearchParam.CheckOutDate, ref ItemPrice, ref drJacTravelRoom, isPerce, AgentPercentage, AgentMarkupAmount, commonSearch.SubAgentMarkup, commonSearch.B2CMarkup, commonSearch.IsParentId);

                                                            drJacTravelRoom["SupplierName"] = Provider;
                                                            dtHotelRoom.Rows.Add(drJacTravelRoom);
                                                            #endregion



                                                            #region CancellationPolicy New Added 16-07-25
                                                            DataRow[] drGastos = Restel_Response.Tables["gastos"].Select("reg_Id = '" + reg_Id + "'");
                                                            if (drGastos.Length > 0)
                                                            {
                                                                int gastos_Id = Convert.ToInt32(drGastos[0]["gastos_Id"]); //New

                                                                DataRow[] drCancellation = Restel_Response.Tables["gasto"].Select("gastos_Id = '" + gastos_Id + "'");
                                                                if (drCancellation.Length > 0)
                                                                {
                                                                    int daysbefore = 0;
                                                                    int daysbefore1 = 0;

                                                                    for (int c = 0; c < drCancellation.Length; c++)
                                                                    {
                                                                        if (c == 0)
                                                                        {
                                                                            daysbefore = Convert.ToInt32(drCancellation[c]["dias_antelacion"]);  //14
                                                                        }
                                                                        else
                                                                        {
                                                                            daysbefore1 = Convert.ToInt32(drCancellation[c]["dias_antelacion"]);
                                                                            if (daysbefore1 > daysbefore)
                                                                            {
                                                                                if (daysbefore1 == 0)
                                                                                {
                                                                                    daysbefore1 = daysbefore;
                                                                                }
                                                                                else
                                                                                {
                                                                                    daysbefore = daysbefore1;
                                                                                }
                                                                            }
                                                                        }

                                                                    }

                                                                    int hours = Convert.ToInt32(drCancellation[0]["horas_antelacion"]);
                                                                    int nights = Convert.ToInt32(drCancellation[0]["noches_gasto"]);
                                                                    int percent = Convert.ToInt32(drCancellation[0]["estCom_gasto"]);   //100%
                                                                                                                                        //string desc = Convert.ToString(drCancellation[0]["concepto"]);
                                                                                                                                        //string TvDate = String.Format("{0:yyyy/MM/dd}", DateTime.ParseExact(Convert.ToString(drCancellation[0]["fecha"]), "dd/MM/yyyy", null));
                                                                    string TvDate = String.Format("{0:yyyy/MM/dd}", DateTime.ParseExact(Convert.ToString(SearchParam.CheckInDate), "dd/MM/yyyy", null));
                                                                    string TDDate = String.Format("{0:yyyy/MM/dd}", Convert.ToString(DateTime.Now.Date));
                                                                    TimeSpan foctotalnights = DateTime.Parse(TvDate) - DateTime.Parse(TDDate);
                                                                    int finalday = foctotalnights.Days;  //262
                                                                    DateTime StartDate = DateTime.Now.Date;
                                                                    DateTime EndDate = DateTime.Now.Date;

                                                                    DateTime curDate = _commonMethod.GetTimeZoneDate(SearchParam.CurrentDate);
                                                                    //string uniqid = Guid.NewGuid().ToString();

                                                                    string CheckIn = SearchParam.CheckInDate;
                                                                    CheckIn = CheckIn.Substring(6, 4) + "-" + CheckIn.Substring(3, 2) + "-" + CheckIn.Substring(0, 2);


                                                                    #region Cancellation Policy Logic and Row
                                                                    if (finalday > daysbefore)
                                                                    {
                                                                        double Penalty = 0;
                                                                        for (int lp = 0; lp < 2; lp++)
                                                                        {
                                                                            if (percent > 0)
                                                                            {
                                                                                if (lp == 0)
                                                                                {
                                                                                    Penalty = 0;
                                                                                    EndDate = Convert.ToDateTime(TvDate).AddDays(-(daysbefore + 1));
                                                                                }
                                                                                else
                                                                                {
                                                                                  
                                                                                    Penalty = (ItemPrice * 100) / 100;
                                                                                    StartDate = Convert.ToDateTime(TvDate).AddDays(-daysbefore);
                                                                                    EndDate = Convert.ToDateTime(TvDate);
                                                                                }
                                                                            }
                                                                            else
                                                                            {
                                                                                if (lp == 0)
                                                                                {
                                                                                    Penalty = 0;
                                                                                    EndDate = Convert.ToDateTime(TvDate).AddDays(-(daysbefore + 1));
                                                                                }
                                                                                else
                                                                                {
                                                                                    Penalty = (ItemPrice * 100) / 100;
                                                                                    
                                                                                    StartDate = Convert.ToDateTime(TvDate).AddDays(-(daysbefore));
                                                                                    EndDate = Convert.ToDateTime(TvDate);
                                                                                }

                                                                            }
                                                                            DateTime CancelDate = DateTime.Now.Date;
                                                                            if (Penalty == 0)
                                                                                CancelDate = EndDate;
                                                                            else
                                                                                CancelDate = StartDate;


                                                                            #region Vat
                                                                            double VatOnPolicy = 0;
                                                                            double penaltynew = Penalty;
                                                                            //penaltynew = _markup.ProcessHotelCancelationPrice_WithMarkup(ref VatOnPolicy, SearchParam.NoofNights, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), Penalty, isPerce, commonSearch.B2CMarkup, AgentPercentage, AgentMarkupAmount, commonSearch.IsParentId, commonSearch.SubAgentMarkup, ref VatOnPolicy);
                                                                            #endregion

                                                                            DateTime NowDate = DateTime.Now.Date;
                                                                            if (CancelDate.AddDays(CutOffDays).Date > DateTime.Now.Date)
                                                                            {
                                                                                EndDate = EndDate.AddDays(CutOffDays).Date;
                                                                                StartDate = StartDate.AddDays(CutOffDays).Date;
                                                                                CancelDate = CancelDate.AddDays(CutOffDays).Date;
                                                                            }
                                                                            else
                                                                            {
                                                                                EndDate = DateTime.Now.Date;
                                                                            }
                                                                            if (NowDate < EndDate && lp == 0)
                                                                            {
                                                                                DateTime CheckInDate = Convert.ToDateTime(CheckIn);
                                                                                
                                                                                if (EndDate > CheckInDate)
                                                                                {
                                                                                    EndDate = CheckInDate;
                                                                                }
                                                                                double charge = 0;
                                                                                 dtCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref charge, dtCancellation.NewRow(), CurrencyFactorRestel, currency, Provider, Convert.ToString(OwnsystemHotelId), Convert.ToString(SeqNo),
                                                                                                               Convert.ToString(RoomCode), RoomName, BoardID, Convert.ToDateTime(curDate), EndDate, Penalty, Penalty, "0", VatOnPolicy, uniqid, "NO", "", Type, "", ""));
                                                                                dtCancellation.AcceptChanges();
                                                                            }
                                                                            else
                                                                            {
                                                                                DateTime CheckInDate = Convert.ToDateTime(CheckIn);
                                                                              
                                                                                if (EndDate > CheckInDate)
                                                                                {
                                                                                    EndDate = CheckInDate;
                                                                                }

                                                                                Penalty = (ItemPrice * 100) / 100;

                                                                                double chargetype = Penalty;
                                                                                 dtCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref chargetype, dtCancellation.NewRow(), CurrencyFactorRestel, currency, Provider, Convert.ToString(OwnsystemHotelId), Convert.ToString(SeqNo),
                                                                                                               Convert.ToString(RoomCode), RoomName, BoardID, StartDate, CheckInDate, Penalty, Penalty, "0", VatOnPolicy, uniqid, "Yes", "", Type));
                                                                                dtCancellation.AcceptChanges();
                                                                                break;

                                                                            }
                                                                        }
                                                                    }
                                                                    else
                                                                    {
                                                                        double Penalty = (ItemPrice * 100) / 100;
                                                                       
                                                                        DateTime CancelDate = DateTime.Now.Date;
                                                                        if (Penalty == 0)
                                                                        {
                                                                            EndDate = Convert.ToDateTime(TvDate).AddDays(-(daysbefore + 1));
                                                                            CancelDate = EndDate;
                                                                        }
                                                                        else
                                                                            CancelDate = Convert.ToDateTime(TvDate).AddDays(-daysbefore);

                                                                        #region Vat
                                                                        double VatOnPolicy = 0;
                                                                        double penaltynew = Penalty;
                                                                        //penaltynew = _markup.ProcessHotelCancelationPrice_WithMarkup(ref VatOnPolicy, SearchParam.NoofNights, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), Penalty, isPerce, commonSearch.B2CMarkup, AgentPercentage, AgentMarkupAmount, commonSearch.IsParentId, commonSearch.SubAgentMarkup, ref VatOnPolicy);
                                                                        #endregion

                                                                        DateTime NowDate = DateTime.Now.Date;
                                                                        if (CancelDate.AddDays(CutOffDays).Date > DateTime.Now.Date)
                                                                        {
                                                                            EndDate = EndDate.AddDays(CutOffDays).Date;
                                                                            StartDate = StartDate.AddDays(CutOffDays).Date;
                                                                            CancelDate = CancelDate.AddDays(CutOffDays).Date;
                                                                        }
                                                                        else
                                                                        {
                                                                            EndDate = DateTime.Now.Date;
                                                                        }
                                                                        //if (NowDate < EndDate)
                                                                        if (false)
                                                                        {
                                                                            DateTime CheckInDate = Convert.ToDateTime(CheckIn);
                                                                            //DateTime CheckInDate = Convert.ToDateTime(CheckIn);
                                                                            if (EndDate > CheckInDate)
                                                                            {
                                                                                EndDate = CheckInDate;
                                                                            }
                                                                            double charge = 0;
                                                                            dtCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref charge, dtCancellation.NewRow(), CurrencyFactorRestel, currency, Provider, Convert.ToString(OwnsystemHotelId), Convert.ToString(SeqNo),
                                                                                                           Convert.ToString(RoomCode), RoomName, BoardID, Convert.ToDateTime(StartDate), EndDate, Penalty, Penalty, "0", VatOnPolicy, uniqid, "NO", "", Type, "", ""));
                                                                            dtCancellation.AcceptChanges();
                                                                        }
                                                                        else
                                                                        {
                                                                            DateTime CheckInDate = Convert.ToDateTime(CheckIn);
                                                                           
                                                                            if (EndDate > CheckInDate)
                                                                            {
                                                                                EndDate = CheckInDate;
                                                                                EndDate = EndDate.AddDays(CutOffDays).Date; // added new 
                                                                            }
                                                                            double chargetype = Penalty;
                                                                            dtCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref chargetype, dtCancellation.NewRow(), CurrencyFactorRestel, currency, Provider, Convert.ToString(OwnsystemHotelId), Convert.ToString(SeqNo), Convert.ToString(RoomCode), RoomName, BoardID, curDate, CheckInDate, Penalty, Penalty, "0", VatOnPolicy, uniqid, "Yes", "", Type));
                                                                            dtCancellation.AcceptChanges();
                                                                        }
                                                                    }
                                                                    //}
                                                                    #endregion
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
                    #endregion
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
                await _exception.Error(ex, searchkey, ResponseData);
                return options;
            }
            //});
            finally
            {
                #region ResProcessTimer
                try
                {
                    timerroomx.Stop();
                    Item.CodeProcessingTimeSupplier = timerroomx.ElapsedMilliseconds;
                    if (SearchParam != null && ResProccessTimerLogActive == "1" && timerroomx.ElapsedMilliseconds > Convert.ToInt32(ResProccessTimeValueforSaveLog))
                    {
                        
                        SearchParam.SupplierTime += ",|Finish:Restel Process Response  " + "|Time :" + timerroomx.Elapsed.ToString();
                         _= Task.Run(() => _commonMethod.FireAway_SQL_ResPorcessTimeLog("", timerroomx.Elapsed.ToString(), Provider, SearchParam.SearchKey, 1, SearchParam.AgentID));
                    }
                }
                catch (Exception x)
                {
                    await _exception.Error(x);
                }
                #endregion
            }
        }

        public async Task<CancellationDto> Get_Restel_CancellationPolicy(string rating, string ContractID, string Type, DataSet dsHotel, string hotelcode, string ownsystemid, string RoomTypeId_Own, string MealID, string Board_Text, DataTable dtAgentDetail, DataTable dtSuplierMarkup, DataSet dsCountryCityMarkup, DataTable dtSearchConfig, HotelSearchParameter objSearPera, string UserId, string Password, string roomno, string Provider, int AgentId, DataTable dtMarkupDetails,string SearchKey, string GiataId = "0", int hotelRating = 0, double FinalSellPrice = 0, string TechName = "")
        {
            CancellationDto cancellationDto = new CancellationDto();
            string IsBooking = "Available";
            string responsetype = "Success";
            string time = "";
            try
            {
                DataTable dtLOH_W2M_Cancellation = _commonMethod.GenerateCancellationDataTableNewV2();
                
                double CurrencyFactorRestel = 1;//USD
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
                   //_markup.getAgetMarkupSetting(Provider, GiataId, dtSuplierMarkup, dtMarkupDetails, ref AgentPercentage, ref AgentMarkupAmount, ref blnIsShowSupplier, ref isPerce, AgentId, hotelRating, objSearPera.CityID, Convert.ToString(objSearPera.CountryID), objSearPera.CheckInDate);
                _markup.getAgetMarkupSettingNew_new(Provider, Convert.ToString(GiataId), dtSuplierMarkup, dsCountryCityMarkup, ref AgentPercentage, ref AgentMarkupAmount, ref blnIsShowSupplier, ref isPerce, AgentId, hotelRating, objSearPera.CityID, Convert.ToString(objSearPera.CountryID), objSearPera.CheckInDate);

                if (dtAgentDetail != null)
                {
                    IsParentId = Convert.ToInt32(dtAgentDetail.Rows[0]["IsParentId"]);
                    SubAgentMarkup = Convert.ToDouble(dtAgentDetail.Rows[0]["B2CMarkupPer"]);
                }
                int CutOffDays = XMLHotelBookingCommon.CutOffDays;
                CutOffDays = _commonMethod.GetCutOffDays(dtSuplierMarkup, Provider);
                DataRow[] drSuplierMarkup = (dtSuplierMarkup.Select("SupplierName='"+ Provider + "'"));
               
                string CheckIn = objSearPera.CheckInDate;
                CheckIn = CheckIn.Substring(6, 4) + "-" + CheckIn.Substring(3, 2) + "-" + CheckIn.Substring(0, 2);
                string RestelUrl = Convert.ToString(drSuplierMarkup[0]["WebSiteURL"]);
                int SID = Convert.ToInt32(drSuplierMarkup[0]["SupplierID"]);

                string RestelCurrency = Convert.ToString(drSuplierMarkup[0]["CurrencyShortName"]);


                DataRow[] drSearchConfig = (dtSearchConfig.Select("SupplierID='" + SID + "'"));
                int SearchTimeOut = Convert.ToInt32(drSearchConfig[0]["BookingConfirmationTimeout"]);
                string PropertyId = hotelcode;//ContractID;
                int NoOfNights = objSearPera.NoofNights;
                string RoomName = "";
                int RoomNo = Convert.ToInt32(roomno);

                string Observation = "";
                string EssentialRQ = Get_Restel_EssenInfo_PriceCheckRQ(PropertyId, CheckIn, NoOfNights, RoomTypeId_Own, MealID, ref RoomName, Type, RoomNo);
                if (EssentialRQ != null)
                {
                    try
                    {
                        string Error = "";
                        //string EssenResponseData = GetRestelResponse(ref Error, "xml=" + EssentialRQ, RestelUrl, SearchTimeOut, "Restel");
                        ResponseDto responseDto = await GetRestelResponse("xml=" + EssentialRQ, RestelUrl, SearchTimeOut, Provider);
                        string EssenResponseData = responseDto.Response;
                        Error  = responseDto.Error;
                        
                        if (EssenResponseData == string.Empty || EssenResponseData.Contains("Error"))
                        {
                            cancellationDto.dsHotelData = dsHotel;
                            cancellationDto.ISBooking = "NotAvailable";
                            return cancellationDto;
                        }
                            

                        EssenResponseData = EssenResponseData.Replace("<?xml version=\"1.0\" encoding=\"ISO-8859-1\"?>", "");
                        EssenResponseData = EssenResponseData.Replace("<!DOCTYPE respuesta SYSTEM \"http://xml.hotelresb2b.com/xml/dtd/res_hotobs.dtd\">", "");
                        EssenResponseData = EssenResponseData.Replace("<![CDATA[", "");
                        EssenResponseData = EssenResponseData.Replace("]]>", "");//observacion

                        DataSet Restel_ResponseNew = _commonMethod.GetDataSetFromXMLString(EssenResponseData);

                        if (Restel_ResponseNew.Tables.Contains("observacion"))
                        {
                            for (int s = 0; s < Restel_ResponseNew.Tables["observacion"].Rows.Count; s++)
                            {
                                Observation += Convert.ToString(Restel_ResponseNew.Tables["observacion"].Rows[s]["obs_texto"]) + "<br>";
                            }
                            Type = Observation;
                        }
                        else
                        {
                            Type = "No Essential Information Available.";
                        }
                    }
                    catch (Exception ex)
                    {
                        Type = "No Essential Information Available.";
                    }
                }

                string CancelPolicyRQ = Get_Restel_Policy_PriceCheckRQ(PropertyId, CheckIn, NoOfNights, RoomTypeId_Own, MealID, ref RoomName, Type, RoomNo);
                _commonMethod.ThirdPartyGetPolicyHistory(Convert.ToString(RoomTypeId_Own), Convert.ToString(ownsystemid), CancelPolicyRQ, "", "After Get Policy", Provider, "Request", SearchKey, "");
                 #endregion

                if (CancelPolicyRQ != null)
                {
                    #region Cancellation Policy RS
                    string Error = "";
                    System.Diagnostics.Stopwatch timerroomprocess = new System.Diagnostics.Stopwatch();
                    timerroomprocess.Start();

                    //string ResponseData = GetRestelResponse(ref Error, "xml=" + CancelPolicyRQ, RestelUrl, SearchTimeOut, "Restel");
                    ResponseDto responseDto = await GetRestelResponse("xml=" + CancelPolicyRQ, RestelUrl, SearchTimeOut, Provider);
                    string ResponseData = responseDto.Response;
                    Error = responseDto.Error;

                    timerroomprocess.Stop();
                    time = timerroomprocess.Elapsed.ToString();

                    _commonMethod.ThirdPartyGetPolicyHistory(Convert.ToString(RoomTypeId_Own), Convert.ToString(ownsystemid), CancelPolicyRQ, Convert.ToString(ResponseData), "After Get Policy", Provider, "Response", SearchKey, "");

                    if (ResponseData == string.Empty || ResponseData.Contains("Error"))
                    {
                        try
                        {
                            responsetype = "GeneralError";
                            string abcd = await _commonMethod.AnalyticsRecheckData(Provider, responsetype, time, SearchKey, TechName);
                        }
                        catch (Exception e)
                        {
                            _ = _exception.ErrorNew(e, _LogAPI.Recheck, SearchKey);
                        }

                        cancellationDto.dsHotelData = dsHotel;
                        cancellationDto.ISBooking = "NotAvailable";
                        return cancellationDto;
                    }

                    ResponseData = ResponseData.Replace("<?xml version=\"1.0\" encoding=\"ISO-8859-1\"?>", "");
                    ResponseData = ResponseData.Replace("<!DOCTYPE respuesta SYSTEM \"http://xml.hotelresb2b.com/xml/dtd/res_algGastosCancAll.dtd\">", "");
                    ResponseData = ResponseData.Replace("<![CDATA[", "");
                    ResponseData = ResponseData.Replace("]]>", "");

                    DataSet Restel_Response = _commonMethod.GetDataSetFromXMLString(ResponseData);
                    #endregion

                    if (Convert.ToInt32(Restel_Response.Tables["politicaCanc"].Rows.Count) == 0)
                    {
                        try
                        {
                            responsetype = "GeneralError";
                            string abcd = await _commonMethod.AnalyticsRecheckData(Provider, responsetype, time, SearchKey, TechName);
                        }
                        catch (Exception e)
                        {
                            _ = _exception.ErrorNew(e, _LogAPI.Recheck, SearchKey);
                        }

                        cancellationDto.dsHotelData = dsHotel;
                        cancellationDto.ISBooking = "NotAvailable";
                        return cancellationDto;

                    }
                    else
                    {
                        #region DataRaw

                        int parametros_Id = Convert.ToInt32(Restel_Response.Tables["parametros"].Rows[0]["parametros_Id"]);

                        DataRow[] drCancellation = (Restel_Response.Tables["politicaCanc"].Select("parametros_Id='" + parametros_Id + "'"));
                        
                        int daysbefore = Convert.ToInt32(drCancellation[0]["dias_antelacion"]);  //14
                        int hours = Convert.ToInt32(drCancellation[0]["horas_antelacion"]);
                        int nights = Convert.ToInt32(drCancellation[0]["noches_gasto"]);
                        int percent = Convert.ToInt32(drCancellation[0]["estCom_gasto"]);   //100%
                        string desc = Convert.ToString(drCancellation[0]["concepto"]);
                        //string TvDate = String.Format("{0:yyyy/MM/dd}", DateTime.ParseExact(Convert.ToString(drCancellation[0]["fecha"]), "dd/MM/yyyy", null));
                        string TvDate = String.Format("{0:yyyy/MM/dd}", DateTime.ParseExact(Convert.ToString(objSearPera.CheckInDate), "dd/MM/yyyy", null));
                        string TDDate = String.Format("{0:yyyy/MM/dd}", Convert.ToString(DateTime.Now.Date));
                        TimeSpan foctotalnights = DateTime.Parse(TvDate) - DateTime.Parse(TDDate);
                        int finalday = foctotalnights.Days;  //262
                        DateTime StartDate = DateTime.Now.Date;
                        DateTime EndDate = DateTime.Now.Date;

                        DateTime curDate = _commonMethod.GetTimeZoneDate(objSearPera.CurrentDate);
                        string uniqid = Guid.NewGuid().ToString();
                        #endregion

                        #region Cancellation Policy Logic and Row
                        if (finalday > daysbefore)
                        {
                            double Penalty = 0;
                            for (int lp = 0; lp < 2; lp++)
                            {
                                if (percent > 0)
                                {
                                    if (lp == 0)
                                    {
                                        Penalty = 0;
                                       
                                        EndDate = Convert.ToDateTime(TvDate).AddDays(-(daysbefore + 1));
                                    }
                                    else
                                    {
                                        Penalty = (FinalSellPrice * 100) / 100;
                                        Penalty = Penalty * CurrencyFactorRestel;
                                        StartDate = Convert.ToDateTime(TvDate).AddDays(-daysbefore);
                                        EndDate = Convert.ToDateTime(TvDate);
                                    }
                                }
                                else
                                {
                                    if (lp == 0)
                                    {
                                        Penalty = 0;
                                        EndDate = Convert.ToDateTime(TvDate).AddDays(-(daysbefore + 1));
                                    }
                                    else
                                    {
                                        Penalty = (FinalSellPrice * 100) / 100;
                                        Penalty = Penalty * CurrencyFactorRestel;
                                        StartDate = Convert.ToDateTime(TvDate).AddDays(-(daysbefore));
                                        EndDate = Convert.ToDateTime(TvDate);
                                    }

                                }
                                DateTime CancelDate = DateTime.Now.Date;
                                if (Penalty == 0)
                                    CancelDate = EndDate;
                                else
                                    CancelDate = StartDate;

                                #region Vat
                                double VatOnPolicy = 0;
                                double penaltynew = 0;
                                penaltynew = _markup.ProcessHotelCancelationPrice_WithMarkup(ref VatOnPolicy, objSearPera.NoofNights, objSearPera.CityID, Convert.ToString(objSearPera.CountryID), Penalty, isPerce, B2CMarkup, AgentPercentage, AgentMarkupAmount, IsParentId, SubAgentMarkup, ref VatOnPolicy);
                                #endregion

                                DateTime NowDate = DateTime.Now.Date;
                                if (CancelDate.AddDays(CutOffDays).Date > DateTime.Now.Date)
                                {
                                    EndDate = EndDate.AddDays(CutOffDays).Date;
                                    StartDate = StartDate.AddDays(CutOffDays).Date;
                                    CancelDate = CancelDate.AddDays(CutOffDays).Date;
                                }
                                else
                                {
                                    EndDate = DateTime.Now.Date;
                                }
                                if (NowDate < EndDate && lp == 0)
                                {
                                    DateTime CheckInDate = Convert.ToDateTime(CheckIn);
                                    
                                    if (EndDate > CheckInDate)
                                    {
                                        EndDate = CheckInDate;
                                    }
                                    double charge = 0;
                                    
                                    dtLOH_W2M_Cancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref charge, dtLOH_W2M_Cancellation.NewRow(), CurrencyFactorRestel, RestelCurrency, Provider, ownsystemid, Convert.ToString(roomno),
                                                                   Convert.ToString(RoomTypeId_Own), RoomName, MealID, Convert.ToDateTime(curDate), EndDate, Penalty, Penalty, "0", VatOnPolicy, uniqid, "NO", "", Type, "", ""));
                                    dtLOH_W2M_Cancellation.AcceptChanges();
                                }
                                else
                                {
                                    DateTime CheckInDate = Convert.ToDateTime(CheckIn);
                                    
                                    if (EndDate > CheckInDate)
                                    {
                                        EndDate = CheckInDate;
                                    }

                                    Penalty = (FinalSellPrice * 100) / 100;
                                    Penalty = Penalty * CurrencyFactorRestel;

                                    double chargetype = Penalty;
                                   
                                    dtLOH_W2M_Cancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref chargetype, dtLOH_W2M_Cancellation.NewRow(), CurrencyFactorRestel, RestelCurrency, Provider, ownsystemid, Convert.ToString(roomno),
                                                                   Convert.ToString(RoomTypeId_Own), RoomName, MealID, StartDate, CheckInDate, Penalty, Penalty, "0", VatOnPolicy, uniqid, "Yes", "", Type));
                                    dtLOH_W2M_Cancellation.AcceptChanges();
                                }
                            }
                        }
                        else
                        {
                            double Penalty = (FinalSellPrice * 100) / 100;
                            Penalty = Penalty * CurrencyFactorRestel;

                            DateTime CancelDate = DateTime.Now.Date;
                            if (Penalty == 0)
                            {
                                EndDate = Convert.ToDateTime(TvDate).AddDays(-(daysbefore + 1));
                                CancelDate = EndDate;
                            }
                            else
                                CancelDate = Convert.ToDateTime(TvDate).AddDays(-daysbefore);

                            #region Vat
                            double VatOnPolicy = 0;
                            double penaltynew = 0;
                            penaltynew = _markup.ProcessHotelCancelationPrice_WithMarkup(ref VatOnPolicy, objSearPera.NoofNights, objSearPera.CityID, Convert.ToString(objSearPera.CountryID), Penalty, isPerce, B2CMarkup, AgentPercentage, AgentMarkupAmount, IsParentId, SubAgentMarkup, ref VatOnPolicy);
                            #endregion

                            DateTime NowDate = DateTime.Now.Date;
                            if (CancelDate.AddDays(CutOffDays).Date > DateTime.Now.Date)
                            {
                                EndDate = EndDate.AddDays(CutOffDays).Date;
                                StartDate = StartDate.AddDays(CutOffDays).Date;
                                CancelDate = CancelDate.AddDays(CutOffDays).Date;
                            }
                            else
                            {
                                EndDate = DateTime.Now.Date;
                            }
                            if(false)// (NowDate < EndDate)
                            {
                                DateTime CheckInDate = Convert.ToDateTime(CheckIn);
                                //DateTime CheckInDate = Convert.ToDateTime(CheckIn);
                                if (EndDate > CheckInDate)
                                {
                                    EndDate = CheckInDate;
                                }
                                double charge = 0;
                                dtLOH_W2M_Cancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref charge, dtLOH_W2M_Cancellation.NewRow(), CurrencyFactorRestel, RestelCurrency, Provider, ownsystemid, Convert.ToString(roomno),
                                                               Convert.ToString(RoomTypeId_Own), RoomName, MealID, Convert.ToDateTime(StartDate), EndDate, Penalty, Penalty, "0", VatOnPolicy, uniqid, "NO", "", Type, "", ""));
                                dtLOH_W2M_Cancellation.AcceptChanges();
                            }
                            else
                            {
                                DateTime CheckInDate = Convert.ToDateTime(CheckIn);
                                
                                if (EndDate > CheckInDate)
                                {
                                    EndDate = CheckInDate;
                                    EndDate = EndDate.AddDays(CutOffDays).Date; // added new 
                                }
                                double chargetype = Penalty;
                                dtLOH_W2M_Cancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref chargetype, dtLOH_W2M_Cancellation.NewRow(), CurrencyFactorRestel, RestelCurrency, Provider, ownsystemid, Convert.ToString(roomno),
                                                               Convert.ToString(RoomTypeId_Own), RoomName, MealID, curDate, CheckInDate, Penalty, Penalty, "0", VatOnPolicy, uniqid, "Yes", "", Type));
                                dtLOH_W2M_Cancellation.AcceptChanges();
                            }


                        }
                        //}
                        #endregion
                    }
                }

                dsHotel.Tables[2].Merge(dtLOH_W2M_Cancellation);
                try
                {
                    responsetype = "Success";
                    string abcd = await _commonMethod.AnalyticsRecheckData(Provider, responsetype, time, SearchKey, TechName);
                }
                catch (Exception e)
                {
                    _ = _exception.ErrorNew(e, _LogAPI.Recheck, SearchKey);
                }

                cancellationDto.ISBooking = IsBooking;
                cancellationDto.dsHotelData = dsHotel;
                return cancellationDto;


            }
            catch (Exception ex)
            {
                _ = _exception.ErrorNew(ex, _LogAPI.Recheck, SearchKey);

                cancellationDto.ISBooking = "NotAvailable";
                cancellationDto.dsHotelData = dsHotel;
                return cancellationDto;
            }
        }

        public string Get_Restel_EssenInfo_PriceCheckRQ(string PropertyId, string CheckIn, int NoOfNights, string RoomTypeId, string MealID, ref string RoomName, string Type, int RoomNo)
        {
            try
            {
                string abc1 = MealID;
                string[] abc = null;
                if (abc1.Contains("$$$"))
                {
                    abc1 = MealID.Replace("$$$", "$");
                    abc = abc1.Split('$');
                }
                #region Cancellation Policy and Price Check RQ
                string NationalityCode = "";
                StringBuilder Restel_CancelPolicyRQ = new StringBuilder();
                Restel_CancelPolicyRQ.Append("<peticion>");
                Restel_CancelPolicyRQ.Append("<tipo>24</tipo>");
                Restel_CancelPolicyRQ.Append("<nombre></nombre>");
                Restel_CancelPolicyRQ.Append("<agencia></agencia>");
                Restel_CancelPolicyRQ.Append("<parametros>");
                if (abc1.Contains("$"))
                {
                    for (int mi = 0; mi < abc.Length - 1; mi++)
                    {
                        Restel_CancelPolicyRQ.Append("<lin>" + abc[mi] + "</lin>");
                    }
                }
                else
                {
                    Restel_CancelPolicyRQ.Append("<lin>" + MealID + "</lin>");
                }
                //Restel_CancelPolicyRQ.Append("<lin>DU#1#C #1.17#0#BB#OK#20031011#20031012#EU#1-0#0#0#0#</lin>");
                Restel_CancelPolicyRQ.Append("</parametros>");
                Restel_CancelPolicyRQ.Append("</peticion>");

                string searchRQString = Restel_CancelPolicyRQ.ToString();
                return searchRQString;
                #endregion
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
                return "";
            }

        }

        public string Get_Restel_Policy_PriceCheckRQ(string PropertyId, string CheckIn, int NoOfNights, string RoomTypeId, string MealID, ref string RoomName, string Type, int RoomNo)
        {
            try
            {
                string abc1 = MealID;
                string[] abc = null;
                if (abc1.Contains("$$$"))
                {
                    abc1 = MealID.Replace("$$$", "$");
                    abc = abc1.Split('$');
                }
                #region Cancellation Policy and Price Check RQ
                string NationalityCode = "";
                StringBuilder Restel_CancelPolicyRQ = new StringBuilder();
                Restel_CancelPolicyRQ.Append("<!DOCTYPE peticion SYSTEM \"http://xml.hotelresb2b.com/xml/dtd/pet_algGastosCanc.dtd\">");
                Restel_CancelPolicyRQ.Append("<peticion>");
                Restel_CancelPolicyRQ.Append("<tipo>144</tipo>");
                Restel_CancelPolicyRQ.Append("<nombre></nombre>");
                Restel_CancelPolicyRQ.Append("<agencia></agencia>");
                Restel_CancelPolicyRQ.Append("<parametros>");
                Restel_CancelPolicyRQ.Append("<datos_reserva>");
                Restel_CancelPolicyRQ.Append("<hotel>" + PropertyId + "</hotel>");
                if (abc1.Contains("$"))
                {
                    for (int mi = 0; mi < abc.Length - 1; mi++)
                    {
                        Restel_CancelPolicyRQ.Append("<lin>" + abc[mi] + "</lin>");
                    }
                }
                else
                {
                    Restel_CancelPolicyRQ.Append("<lin>" + MealID + "</lin>");
                }
                //Restel_CancelPolicyRQ.Append("<lin>DU#1#C #1.17#0#BB#OK#20031011#20031012#EU#1-0#0#0#0#</lin>");
                Restel_CancelPolicyRQ.Append("</datos_reserva>");
                Restel_CancelPolicyRQ.Append("</parametros>");
                Restel_CancelPolicyRQ.Append("</peticion>");

                string searchRQString = Restel_CancelPolicyRQ.ToString();
                return searchRQString;
                #endregion
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
                return "";
            }

        }

        public async Task<ResponseDto> GetRestelResponse(string request, string TravcoURL, Int32 TimeOut, string Provider = "", HotelSearchParameter para = null, bool DebugMode = false, string Searchkey = "", string TechName = "")
        {
            //System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
            //timer.Start();

            ResponseDto responseDto = new ResponseDto();
            string Error = "";

            string response = null;
            HttpWebRequest httpWebRequest = null;
            HttpWebResponse httpWebResponse = null;
            try
            {
                byte[] bytes;
                bytes = Encoding.ASCII.GetBytes(request);
                //Set HttpWebRequest properties
                if (Provider.ToLower() == "restel")
                {
                   
                    string Key = TravcoURL + "?codigousu=" + _supplierConfig.Restel.codigousu + "&clausu=" + _supplierConfig.Restel.clausu + "&afiliacio=" + _supplierConfig.Restel.afiliacio + "&secacc=" + _supplierConfig.Restel.secacc;
                   // string Key = _supplierConfig.Restel.Key; //"http://xml.hotelresb2b.com/xml/listen_xml.jsp?codigousu=NEWL&clausu=xml493331&afiliacio=RS&secacc=124774";
                    httpWebRequest = (HttpWebRequest)HttpWebRequest.Create(Key);
                    httpWebRequest.Method = "POST";
                    httpWebRequest.UserAgent = "Mozilla/4.0 (compatible; MSIE 6.0; MS Web Services Client Protocol 1.1.4322.2300)";
                    httpWebRequest.ContentType = "application/x-www-form-urlencoded;";
                    //httpWebRequest.ContentType = "text/xml;charset=utf-8;";
                    //httpWebRequest.Accept = "text/xml";
                    httpWebRequest.Timeout = TimeOut;
                    httpWebRequest.ContentLength = bytes.Length;
                    httpWebRequest.Headers.Add("Accept-Encoding", "gzip");
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                    //httpWebRequest.Headers.Add("Accept-Encoding", "gzip");
                    //httpWebRequest.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
                }
                using (Stream requestStream = httpWebRequest.GetRequestStream())
                {
                    //Writes a sequence of bytes to the current stream                     
                    requestStream.Write(bytes, 0, bytes.Length);
                    requestStream.Close();//Close stream
                }
                //Sends the HttpWebRequest, and waits for a response.
                httpWebResponse = (HttpWebResponse)await httpWebRequest.GetResponseAsync();
                if (httpWebResponse.StatusCode == HttpStatusCode.OK)
                {
                    Stream responseStream = httpWebResponse.GetResponseStream();
                    //Get response stream into StreamReader 


                    if (httpWebResponse.ContentEncoding.ToLower().Contains("gzip"))
                        responseStream = new System.IO.Compression.GZipStream(responseStream, System.IO.Compression.CompressionMode.Decompress);
                    else if (httpWebResponse.ContentEncoding.ToLower().Contains("deflate"))
                        responseStream = new System.IO.Compression.DeflateStream(responseStream, System.IO.Compression.CompressionMode.Decompress);
                    using (StreamReader reader = new StreamReader(responseStream))
                        response = await reader.ReadToEndAsync();


                    //timer.Stop();
                    if (para != null)
                    {
                        //para.SupplierTime += ",SupplierName :" + Provider + " : " + "Execution Time :" + timer.Elapsed.ToString();
                        if (DebugMode)
                            _ = _commonMethod.XMLRequestResponseTrack(request, response, Provider, Searchkey);
                    }
                    //if (Convert.ToBoolean(System.Web.Configuration.WebConfigurationManager.AppSettings["ConnectionStringforlogtrack"]))
                    // {
                    //TimeSpan timeTaken = timer.Elapsed;
                    //ErrorLog(Convert.ToString(System.Web.Configuration.WebConfigurationManager.AppSettings["LogTrackPath"]), "Trvaco->" + HotelTrvacoProvider + "-> Execution Time : " + timer.Elapsed.ToString());
                    _ = _commonMethod.XMLRequestResponseTrack(request, response, Provider == "" ? "Travco" : Provider, Searchkey);
                    //}
                }
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
                _ = _commonMethod.XMLRequestResponseTrackError(request, result1, Provider == "" ? "restel" : Provider, Searchkey);
            }
            catch (Exception ex)
            {
                Error = ex.Message.ToString();
                //response = ex.Message + ex.InnerException;
                _ = _commonMethod.XMLRequestResponseTrack(request, ex.Message, Provider == "" ? "restel" : Provider, Searchkey);
                _ = _exception.Error(ex, Searchkey);
            }
            finally
            {
                if (httpWebResponse != null)
                {
                    httpWebResponse.Close();

                    httpWebResponse = null;

                    httpWebRequest = null;
                }
            }

            responseDto.Response = response;
            responseDto.Error = Error;

            return responseDto;
        }

        public async Task<PriceCheckCMDto> Get_Restel_PriceCheck(DataSet dsCartDetails, DataRow[] RestelDetails, int i, int j, string UniqueNo, string AgentUniqueNo, double FinalPrice, string a, double CompanyBuyingPrice, string PreBookingToken, DataTable dtCheckPriceReqStructure, string Searchkey, int AgentId = 0,TokenData tokenData=null,string TechName="")
        {
            PriceCheckCMDto priceCheckCMDto = new PriceCheckCMDto();

            string responsetype = "Success";
            string time = "";
            string SupplierName = Convert.ToString(dsCartDetails.Tables[1].DefaultView[0]["SupplierName"]);
            string Error = "";
            string Request = "";
            string ResponseData = "";

            int srphotelid = 0;
            string srphotelhotelname = "";
            try
            {
                srphotelid = Convert.ToInt32(dsCartDetails.Tables[0].Rows[0]["ServiceId"]);
                srphotelhotelname = Convert.ToString(dsCartDetails.Tables[0].Rows[0]["ServiceName"]);
            }
            catch (Exception ex)
            {
                _ = _exception.Error(ex, Searchkey);
            }


            try
            {
                string HotelId = Convert.ToString(dsCartDetails.Tables[0].DefaultView[i]["XMLHotelId"]);
                int UserCartId = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[j]["UserCartID"]);
                int UserCartDetailId = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[j]["UserCartDetailId"]);
                string RoomTypeId = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomTypeID"]);
                string MealId = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["MealPlanCode"]);
                string UserID = Convert.ToString(RestelDetails[0]["UserID"]);
                string TravcoURL = Convert.ToString(RestelDetails[0]["WebSiteURL"]);

                string RestelCurrency = Convert.ToString(RestelDetails[0]["CurrencyShortName"]);
                FinalPrice = CompanyBuyingPrice;
                //-New added code
                int NoOfRooms = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView.Count);
                int Child1Age = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[j]["ChildAge1"]);
                int Child2Age = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[j]["ChildAge2"]);
                string CheckIn = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["CheckInDate"]);
                string CheckIn1 = CheckIn.Substring(0, 4) + "-" + CheckIn.Substring(4, 2) + "-" + CheckIn.Substring(6, 2);
                string CheckIn2 = CheckIn.Substring(6, 2) + "/" + CheckIn.Substring(4, 2) + "/" + CheckIn.Substring(0, 4);
                string CheckOut = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["CheckOutDate"]);
                string CheckOut1 = CheckOut.Substring(0, 4) + "-" + CheckOut.Substring(4, 2) + "-" + CheckOut.Substring(6, 2);
                string CheckOut2 = CheckOut.Substring(6, 2) + "/" + CheckOut.Substring(4, 2) + "/" + CheckOut.Substring(0, 4);
                int NoOfNight = Convert.ToInt32((Convert.ToDateTime(CheckOut1) - Convert.ToDateTime(CheckIn1)).TotalDays);
                //End--

               
                double CurrencyFactorRestel = _commonMethod.GetROEByCurrency(RestelCurrency, tokenData.dtROE);


                StringBuilder resprice = new StringBuilder();
                #region Price Only and Room Only RQ            
                
                string chkin = String.Format("{0:MM/dd/yyyy}", DateTime.ParseExact(CheckIn2, "dd/MM/yyyy", null));
                string chkout = String.Format("{0:MM/dd/yyyy}", DateTime.ParseExact(CheckOut2, "dd/MM/yyyy", null));

                StringBuilder resreq = new StringBuilder();
                resreq.Append("<peticion><tipo>110</tipo><nombre></nombre><agencia></agencia>");
                resreq.Append("<parametros>");
                resreq.Append("<hotel>" + HotelId + "#</hotel>");
                resreq.Append("<pais></pais>");
                resreq.Append("<provincia></provincia>");
                resreq.Append("<categoria>0</categoria>");
                resreq.Append("<radio>9</radio>");
                resreq.Append("<fechaentrada>" + chkin + "</fechaentrada>");
                resreq.Append("<fechasalida>" + chkout + "</fechasalida>");
                resreq.Append("<marca></marca>");
                resreq.Append("<afiliacion>RS</afiliacion>");
                resreq.Append("<usuario>" + UserID + "</usuario>");

                for (int s = 0; s < NoOfRooms; s++)
                {

                    int TotalAdult = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[s]["NoOfAdults"]); //SearchParam.Rooms[s].NoofAdults;
                    int TotalChild = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[s]["NoOfChild"]); //SearchParam.Rooms[s].NoofChild;
                    string childage = "";

                    if (s == 0)
                    {
                        resreq.Append("<numhab" + (s + 1) + ">" + NoOfRooms + "</numhab" + (s + 1) + ">");
                        for (int k = 0; k < NoOfRooms; k++)
                        {
                            if (k == NoOfRooms - 1)
                                childage += Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[j]["ChildAge1"]);// SearchParam.Rooms[s].Child1Age;
                            else
                                childage += Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[j]["ChildAge1"]) + ",";//SearchParam.Rooms[s].Child1Age + ",";
                        }
                    }
                    else
                    {
                        resreq.Append("<numhab" + (s + 1) + ">0</numhab" + (s + 1) + ">");
                    }
                    resreq.Append("<paxes" + (s + 1) + ">" + TotalAdult + "-" + TotalChild + "</paxes" + (s + 1) + ">");
                    if (s == 0)
                    {
                        resreq.Append("<edades" + (s + 1) + ">" + childage + "</edades" + (s + 1) + ">");
                    }
                    else
                    {
                        resreq.Append("<edades" + (s + 1) + ">" + Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[s]["ChildAge2"]) + "</edades" + (s + 1) + ">");
                    }
                }

               
                resreq.Append("<idioma>2</idioma>");
                resreq.Append("<duplicidad>1</duplicidad>");
                resreq.Append("<comprimido>2</comprimido>");
                resreq.Append("<informacion_hotel>0</informacion_hotel>");
                resreq.Append("<tarifas_reembolsables>1</tarifas_reembolsables>");
                resreq.Append("</parametros>");
                resreq.Append("</peticion>");

                Request = "xml=" + resreq.ToString();

                _commonMethod.ThirdPartyBookingHistory(Convert.ToString(UniqueNo), Convert.ToString(AgentUniqueNo), resreq.ToString(), "", "Before get price " + SupplierName + " request hit " + j + "is for Price Check" + false, SupplierName, "response confirm", "-", "Withinearth",Searchkey);

                dtCheckPriceReqStructure.Rows.Add(_commonMethod.AddCheckPriceReq(dtCheckPriceReqStructure.NewRow(), j + 1, UserCartId, UserCartDetailId, RoomTypeId, SupplierName, resreq.ToString()));

                System.Diagnostics.Stopwatch timerroomprocess = new System.Diagnostics.Stopwatch();
                timerroomprocess.Start();

                //ResponseData = GetRestelResponse(ref Error, "xml=" + resreq.ToString(), "", 120000, "Restel", null, false, Searchkey);
                ResponseDto responseDto = await GetRestelResponse("xml=" + resreq.ToString(), TravcoURL, 120000, SupplierName, null, false, Searchkey);
                ResponseData = responseDto.Response;
                Error = responseDto.Error;

                timerroomprocess.Stop();
                time = timerroomprocess.Elapsed.ToString();

                _commonMethod.ThirdPartyBookingHistory(Convert.ToString(UniqueNo), Convert.ToString(AgentUniqueNo), resreq.ToString(), ResponseData + Error, "After get price " + SupplierName + " request hit " + j + "is for Price Check" + false, SupplierName, "response confirm", "-", "Withinearth",Searchkey);

                if (ResponseData == string.Empty || ResponseData.Contains("Error"))
                {
                    try
                    {
                        responsetype = "GeneralError";
                        string abc =  await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, Request, ResponseData, Error,Searchkey,TechName, srphotelid, srphotelhotelname);
                    }
                    catch (Exception e)
                    {
                        _ = _exception.Error(e, Searchkey);
                    }

                    priceCheckCMDto.Flag = false;
                    return priceCheckCMDto;
                }
                ResponseData = ResponseData.Replace("<?xml version=\"1.0\" encoding=\"ISO-8859-1\"?>", "");
                ResponseData = ResponseData.Replace("<!DOCTYPE respuesta SYSTEM \"http://xml.hotelresb2b.com/xml/dtd/res_disponibilidad_110.dtd\">", "");
                ResponseData = ResponseData.Replace("<![CDATA[", "");
                ResponseData = ResponseData.Replace("]]>", "");
                ResponseData = ResponseData.Replace("[ARROBA]", "@").Replace("&", "&amp;");

                DataSet Restel_Response = _commonMethod.GetDataSetFromXMLString(ResponseData);
                if (Convert.ToInt32(Restel_Response.Tables["reg"].Rows.Count) == 0)
                {
                    try
                    {
                        responsetype = "GeneralError";
                        string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, Request, ResponseData, Error, Searchkey, TechName, srphotelid, srphotelhotelname);
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
                    if (Restel_Response.Tables["reg"].Columns.Contains("lin") == true)
                    {
                        for (int rg = 0; rg < Restel_Response.Tables["reg"].Rows.Count; rg++)
                        {
                            string checkMealId = "";
                            if (Restel_Response.Tables["reg"].Columns.Contains("lin") == true)
                            { checkMealId = Convert.ToString(Restel_Response.Tables["reg"].Rows[rg]["lin"]); }
                            else
                            { checkMealId = Convert.ToString(Restel_Response.Tables["lin"].Rows[rg]["lin_Text"]); }

                            string newcheckid = checkMealId.Split('-')[0];
                            string newmealid = MealId.Split('-')[0];
                            if (newcheckid == newmealid)
                            {
                                FinalPrice = Convert.ToDouble(Restel_Response.Tables["reg"].Rows[rg]["prr"]);
                                FinalPrice = FinalPrice * CurrencyFactorRestel;
                                FinalPrice = Convert.ToDouble(Math.Round(FinalPrice, 2));
                                priceCheckCMDto.ItemPrice = FinalPrice;
                                if (Convert.ToDouble(Math.Round(FinalPrice, 2)) > Math.Round(CompanyBuyingPrice, 2))
                                {
                                    try
                                    {
                                        responsetype = "GeneralError";
                                        ResponseData = "#Price Changed " + "#Main Price = " + CompanyBuyingPrice + " #New Price = " + FinalPrice + " #Response= " + ResponseData;
                                        string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, Request, ResponseData, Error, Searchkey, TechName, srphotelid, srphotelhotelname);
                                    }
                                    catch (Exception e)
                                    {
                                        _ = _exception.Error(e, Searchkey);
                                    }
                                    priceCheckCMDto.Flag = true;
                                    priceCheckCMDto.ItemPrice = FinalPrice;
                                    return priceCheckCMDto;
                                }
                                else
                                {
                                    try
                                    {
                                        responsetype = "Success";
                                        string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, Request, ResponseData, Error, Searchkey, TechName, srphotelid, srphotelhotelname);
                                    }
                                    catch (Exception e)
                                    {
                                        _ = _exception.Error(e, Searchkey);
                                    }
                                    priceCheckCMDto.Flag = false;
                                    return priceCheckCMDto;
                                }
                            }
                        }
                    }
                    else
                    {
                        for (int rg = 0; rg < Restel_Response.Tables["reg"].Rows.Count; rg++)
                        {
                            string checkMealId = "";
                            if (Restel_Response.Tables["reg"].Columns.Contains("lin") == true)
                            { checkMealId = Convert.ToString(Restel_Response.Tables["reg"].Rows[rg]["lin"]); }
                            else
                            {
                                int reg_idd = Convert.ToInt32(Restel_Response.Tables["reg"].Rows[rg]["reg_Id"]);

                                DataRow[] drnewlin = Restel_Response.Tables["lin"].Select("reg_Id =" + reg_idd + " ");
                                if (drnewlin.Length > 0)
                                {
                                    for (int nll = 0; nll < drnewlin.Length; nll++)
                                    {
                                        checkMealId = Convert.ToString(drnewlin[nll]["lin_Text"]);

                                        string newcheckid = checkMealId.Split('-')[0];
                                        string newmealid = MealId.Split('-')[0];
                                        if (newcheckid == newmealid)
                                        {
                                            FinalPrice = Convert.ToDouble(Restel_Response.Tables["reg"].Rows[rg]["prr"]);
                                            FinalPrice = FinalPrice * CurrencyFactorRestel;
                                            FinalPrice = Convert.ToDouble(Math.Round(FinalPrice, 2));
                                            priceCheckCMDto.ItemPrice = FinalPrice;
                                            if (Convert.ToDouble(Math.Round(FinalPrice, 2)) > Math.Round(CompanyBuyingPrice, 2))
                                            {
                                                try
                                                {
                                                    ResponseData = "#Price Changed " + "#Main Price = " + CompanyBuyingPrice + " #New Price = " + FinalPrice + " #Response= " + ResponseData;
                                                    responsetype = "GeneralError";
                                                    string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, Request, ResponseData, Error, Searchkey, TechName, srphotelid, srphotelhotelname);
                                                }
                                                catch (Exception e)
                                                {
                                                    _ = _exception.Error(e, Searchkey);
                                                }
                                                priceCheckCMDto.Flag = true;
                                                return priceCheckCMDto;
                                            }
                                            else
                                            {
                                                try
                                                {
                                                    responsetype = "Success";
                                                    string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, Request, ResponseData, Error, Searchkey, TechName, srphotelid, srphotelhotelname);
                                                }
                                                catch (Exception e)
                                                {
                                                    _ = _exception.Error(e, Searchkey);
                                                }
                                                priceCheckCMDto.Flag = false;
                                                priceCheckCMDto.ItemPrice = FinalPrice;
                                                return priceCheckCMDto;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                #endregion
                try
                {
                    responsetype = "Success";
                    string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, Request, ResponseData, Error, Searchkey, TechName, srphotelid, srphotelhotelname);
                }
                catch (Exception e)
                {
                    _ = _exception.Error(e, Searchkey);
                }

                priceCheckCMDto.Flag = false;
                return priceCheckCMDto;
            }
            catch (Exception ex)
            {
                try
                {
                    Error = ex.Message + ex.StackTrace.ToString();
                    responsetype = "GeneralError";
                    string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, Request, ResponseData, Error, Searchkey, TechName, srphotelid, srphotelhotelname);
                }
                catch (Exception e)
                {
                    _ = _exception.Error(e, Searchkey);
                }
                _ = _exception.Error(ex, Searchkey);

                priceCheckCMDto.Flag = true;
                return priceCheckCMDto;
            }
        }

        public string Get_Restel_BookingRequest(DataSet dsCartDetails, DataTable dtPassengers, DataRow[] RestelDetails, string UniqueNo, bool IsPriceUpdate, int i, int j, string PreBookingToken, string UserID, string Password, string AgentUniqueNo)
        {
            try
            {
                #region Booking RQ

                string CheckIn = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["CheckInDate"]);
                CheckIn = CheckIn.Substring(0, 4) + "-" + CheckIn.Substring(4, 2) + "-" + CheckIn.Substring(6, 2);
                string CheckOut = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["CheckOutDate"]);
                CheckOut = CheckOut.Substring(0, 4) + "-" + CheckOut.Substring(4, 2) + "-" + CheckOut.Substring(6, 2);
                //string PropertyId = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["ContractID"]);
                string PropertyId = Convert.ToString(dsCartDetails.Tables[0].DefaultView[i]["XMLHotelId"]);
                string MealId = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["MealPlanCode"]);
                string ExternalReference = "BOOK_" + AgentUniqueNo + "_" + UniqueNo;
                int NoOfNoghts = Convert.ToInt32(dsCartDetails.Tables[0].Rows[i]["NoOfNight"]);

                string SPUI = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["spui"]);
                string RoomType = "";
                if (IsPriceUpdate)
                    RoomType = "";
                else
                    RoomType = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomTypeId"]);
                string NationalityCode = "";
                string abc1 = MealId;
                string[] abc = null;
                if (abc1.Contains("$$$"))
                {
                    abc1 = MealId.Replace("$$$", "$");
                    abc = abc1.Split('$');
                }
                #region  IsLead Filter Logic
                string isLead = string.Empty;
                dtPassengers.DefaultView.RowFilter = "PickUp='" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomTypeID"]) + "' and RoomNo='" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomNumber"]) + "' and IsLead=1";
                if (dtPassengers.DefaultView.Count == 0)
                    isLead = "true";
                dtPassengers.DefaultView.RowFilter = "";
                dtPassengers.DefaultView.RowFilter = "PickUp='" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomTypeID"]) + "' and RoomNo='" + (dsCartDetails.Tables[1].DefaultView[j]["RoomNumber"]) + "' and UserCartId='" + dsCartDetails.Tables[0].Rows[i]["UserCartId"] + "'";
                dtPassengers.DefaultView.Sort = "IsLead desc";
                #endregion

                StringBuilder LeadPax = new StringBuilder();
                if (dtPassengers.DefaultView.Count > 0)
                {
                    for (int p = 0; p < dtPassengers.DefaultView.Count; p++)
                    {
                        string FName = Convert.ToString(dtPassengers.DefaultView[p]["FirstName"]);
                        string LName = Convert.ToString(dtPassengers.DefaultView[p]["LastName"]);
                        int ChildAgex = Convert.ToInt32(dtPassengers.DefaultView[p]["ChildAge"]);
                        string Prefix = Convert.ToString(dtPassengers.DefaultView[p]["Prefix"]).Replace(".", "");
                        string PaxType = Convert.ToString(dtPassengers.DefaultView[p]["PaxType"]);

                        DataRow[] drPassengers = dtPassengers.DefaultView.ToTable().Select("IsLead='1'");
                        if (drPassengers.Length == 0)
                        {
                            if (Convert.ToString(dtPassengers.DefaultView[p]["PaxType"]).Trim().ToLower() == "adult")
                                dtPassengers.DefaultView[p]["IsLead"] = 1;
                        }
                        if (Convert.ToInt16(dtPassengers.DefaultView[p]["IsLead"]) == 1)
                        {
                            LeadPax.Append("<nombre_cliente>" + Prefix + " " + FName + " " + LName + "</nombre_cliente>");
                        }
                    }
                }

                StringBuilder BookRQ = new StringBuilder();
                BookRQ.Append("<?xml version=\"1.0\" encoding=\"ISO-8859-1\"?>");
                BookRQ.Append("<!DOCTYPE peticion SYSTEM \"http://xml.hotelresb2b.com/xml/dtd/pet_reserva_3.dtd\">");
                BookRQ.Append("<peticion>");
                BookRQ.Append("<nombre></nombre>");
                BookRQ.Append("<agencia></agencia>");
                BookRQ.Append("<tipo>202</tipo>");
                BookRQ.Append("<parametros>");
                BookRQ.Append("<codigo_hotel>" + PropertyId + "</codigo_hotel>");
                BookRQ.Append(LeadPax.ToString());
                BookRQ.Append("<observaciones></observaciones>");
                BookRQ.Append("<num_mensaje/>");
                BookRQ.Append("<num_expediente>11990</num_expediente>");
                BookRQ.Append("<forma_pago>25</forma_pago>");  //12 Hotel Payment  25 Credit 44 Prepayment 
                BookRQ.Append("<tipo_targeta></tipo_targeta>");
                BookRQ.Append("<num_targeta></num_targeta>");
                BookRQ.Append("<cvv_targeta></cvv_targeta>");
                BookRQ.Append("<mes_expiracion_targeta></mes_expiracion_targeta>");
                BookRQ.Append("<ano_expiracion_targeta></ano_expiracion_targeta>");
                BookRQ.Append("<titular_targeta></titular_targeta>");
                BookRQ.Append("<res>");
                if (abc1.Contains("$"))
                {
                    for (int mi = 0; mi < abc.Length - 1; mi++)
                    {
                        BookRQ.Append("<lin>" + abc[mi] + "</lin>");
                    }
                }
                else
                {
                    BookRQ.Append("<lin>" + MealId + "</lin>");
                }
                BookRQ.Append("</res>");
                BookRQ.Append("<paxes>");
                if (dtPassengers.DefaultView.Count > 0)
                {
                    dtPassengers.DefaultView.RowFilter = "";
                    dtPassengers.DefaultView.RowFilter = "PickUp='" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomTypeID"]) + "' and UserCartId='" + dsCartDetails.Tables[0].Rows[i]["UserCartId"] + "'";
                    dtPassengers.DefaultView.Sort = "IsLead desc";
                    for (int p = 0; p < dtPassengers.DefaultView.Count; p++)
                    {
                        string FName = Convert.ToString(dtPassengers.DefaultView[p]["FirstName"]);
                        string LName = Convert.ToString(dtPassengers.DefaultView[p]["LastName"]);
                        int ChildAgex = Convert.ToInt32(dtPassengers.DefaultView[p]["ChildAge"]);
                        string Prefix = Convert.ToString(dtPassengers.DefaultView[p]["Prefix"]).Replace(".", "");
                        string PaxType = Convert.ToString(dtPassengers.DefaultView[p]["PaxType"]);

                        BookRQ.Append("<pax>");
                        if (Convert.ToString(Prefix).Trim().ToLower() == "mr")
                            BookRQ.Append("<titulo>Sr.</titulo>"); //Guest title
                        else
                            BookRQ.Append("<titulo>Sra.</titulo>"); //Guest title
                        BookRQ.Append("<nombrePax>" + FName + "</nombrePax>"); //Client Name 
                        BookRQ.Append("<apellidos>" + LName + "</apellidos>");//Client Surname 
                        if (Convert.ToString(PaxType).Trim().ToLower() == "adult")
                            BookRQ.Append("<edad>32</edad>");//Age
                        else
                            BookRQ.Append("<edad>" + ChildAgex + "</edad>");//Age
                        BookRQ.Append("</pax>");
                    }
                }
                //<pax>
                //  <titulo>Sra.</titulo>
                //  <nombrePax>PruebaUno</nombrePax>
                //  <apellidos>PruebasUno PruebasUno </ apellidos >
                //  < edad ></ edad >
                //</ pax >
                BookRQ.Append("</paxes>");
                BookRQ.Append("</parametros>");
                BookRQ.Append("</peticion>");
                string BookingRQ = BookRQ.ToString();

                return BookingRQ;
                #endregion
            }
            catch (Exception ex)
            {
                _ =_exception.Error(ex);
                return "";
            }
        }
        public string Get_Restel_BookingRequest_newversion(BookRequest_newversion bookRequest, DataTable dtPassengers, DataRow[] RestelDetails, string UniqueNo, bool IsPriceUpdate, int i, int j, string PreBookingToken, string UserID, string Password, string AgentUniqueNo)
        {
            try
            {
                #region Booking RQ

                string MealId = Convert.ToString(bookRequest.userCart.UserCartDetails[j].MealPlan);

                string ExternalReference = "BOOK_" + AgentUniqueNo + "_" + UniqueNo;
               
                int NoOfNoghts = Convert.ToInt32(bookRequest.userCart.NoOfNight);

                string RoomType = "";
                if (IsPriceUpdate)
                    RoomType = "";
                else
                    RoomType = Convert.ToString(bookRequest.userCart.UserCartDetails[j].RoomTypeId);


                string[] spitidRoomTypeId = Convert.ToString(bookRequest.userCart.UserCartDetails[j].RoomTypeId).Split(new String[] { "@@@" }, StringSplitOptions.None);
                string PropertyId = spitidRoomTypeId[1];    

                string NationalityCode = "";
                string abc1 = MealId;
                string[] abc = null;
                if (abc1.Contains("$$$"))
                {
                    abc1 = MealId.Replace("$$$", "$");
                    abc = abc1.Split('$');
                }
                #region  IsLead Filter Logic
                string isLead = string.Empty;
                dtPassengers.DefaultView.RowFilter = "PickUp='" + Convert.ToString(bookRequest.userCart.UserCartDetails[j].RoomTypeId) + "' and RoomNo='" + Convert.ToString(bookRequest.userCart.UserCartDetails[j].RoomNo) + "' and IsLead=1";
                if (dtPassengers.DefaultView.Count == 0)
                    isLead = "true";
                dtPassengers.DefaultView.RowFilter = "";
                dtPassengers.DefaultView.RowFilter = "PickUp='" + Convert.ToString(bookRequest.userCart.UserCartDetails[j].RoomTypeId) + "' and RoomNo='" + (bookRequest.userCart.UserCartDetails[j].RoomNo) + "' and UserCartId='" + bookRequest.userCart.UserCartId + "'";
                dtPassengers.DefaultView.Sort = "IsLead desc";
                #endregion

                StringBuilder LeadPax = new StringBuilder();
                if (dtPassengers.DefaultView.Count > 0)
                {
                    for (int p = 0; p < dtPassengers.DefaultView.Count; p++)
                    {
                        string FName = Convert.ToString(dtPassengers.DefaultView[p]["FirstName"]);
                        string LName = Convert.ToString(dtPassengers.DefaultView[p]["LastName"]);
                        int ChildAgex = Convert.ToInt32(dtPassengers.DefaultView[p]["ChildAge"]);
                        string Prefix = Convert.ToString(dtPassengers.DefaultView[p]["Prefix"]).Replace(".", "");
                        string PaxType = Convert.ToString(dtPassengers.DefaultView[p]["PaxType"]);

                        DataRow[] drPassengers = dtPassengers.DefaultView.ToTable().Select("IsLead='1'");
                        if (drPassengers.Length == 0)
                        {
                            if (Convert.ToString(dtPassengers.DefaultView[p]["PaxType"]).Trim().ToLower() == "adult")
                                dtPassengers.DefaultView[p]["IsLead"] = 1;
                        }
                        if (Convert.ToInt16(dtPassengers.DefaultView[p]["IsLead"]) == 1)
                        {
                            LeadPax.Append("<nombre_cliente>" + Prefix + " " + FName + " " + LName + "</nombre_cliente>");
                        }
                    }
                }

                StringBuilder BookRQ = new StringBuilder();
                BookRQ.Append("<?xml version=\"1.0\" encoding=\"ISO-8859-1\"?>");
                BookRQ.Append("<!DOCTYPE peticion SYSTEM \"http://xml.hotelresb2b.com/xml/dtd/pet_reserva_3.dtd\">");
                BookRQ.Append("<peticion>");
                BookRQ.Append("<nombre></nombre>");
                BookRQ.Append("<agencia></agencia>");
                BookRQ.Append("<tipo>202</tipo>");
                BookRQ.Append("<parametros>");
                BookRQ.Append("<codigo_hotel>" + PropertyId + "</codigo_hotel>");
                BookRQ.Append(LeadPax.ToString());
                BookRQ.Append("<observaciones></observaciones>");
                BookRQ.Append("<num_mensaje/>");
               // BookRQ.Append("<num_expediente>11990</num_expediente>");
                BookRQ.Append("<num_expediente>"+ ExternalReference + "</num_expediente>");//added 13april
                BookRQ.Append("<forma_pago>25</forma_pago>");  //12 Hotel Payment  25 Credit 44 Prepayment 
                BookRQ.Append("<tipo_targeta></tipo_targeta>");
                BookRQ.Append("<num_targeta></num_targeta>");
                BookRQ.Append("<cvv_targeta></cvv_targeta>");
                BookRQ.Append("<mes_expiracion_targeta></mes_expiracion_targeta>");
                BookRQ.Append("<ano_expiracion_targeta></ano_expiracion_targeta>");
                BookRQ.Append("<titular_targeta></titular_targeta>");
                BookRQ.Append("<res>");
                if (abc1.Contains("$"))
                {
                    for (int mi = 0; mi < abc.Length - 1; mi++)
                    {
                        BookRQ.Append("<lin>" + abc[mi] + "</lin>");
                    }
                }
                else
                {
                    BookRQ.Append("<lin>" + MealId + "</lin>");
                }
                BookRQ.Append("</res>");
                BookRQ.Append("<paxes>");
                if (dtPassengers.DefaultView.Count > 0)
                {
                    dtPassengers.DefaultView.RowFilter = "";
                    dtPassengers.DefaultView.RowFilter = "PickUp='" + Convert.ToString(bookRequest.userCart.UserCartDetails[j].RoomTypeId) + "' and UserCartId='" + bookRequest.userCart.UserCartId + "'";
                    dtPassengers.DefaultView.Sort = "IsLead desc";
                    for (int p = 0; p < dtPassengers.DefaultView.Count; p++)
                    {
                        string FName = Convert.ToString(dtPassengers.DefaultView[p]["FirstName"]);
                        string LName = Convert.ToString(dtPassengers.DefaultView[p]["LastName"]);
                        int ChildAgex = Convert.ToInt32(dtPassengers.DefaultView[p]["ChildAge"]);
                        
                        string Prefix = Convert.ToString(dtPassengers.DefaultView[p]["Prefix"]).Replace(".", "");
                        string PaxType = Convert.ToString(dtPassengers.DefaultView[p]["PaxType"]);

                        BookRQ.Append("<pax>");
                        if (Convert.ToString(Prefix).Trim().ToLower() == "mr")
                            BookRQ.Append("<titulo>Sr.</titulo>"); //Guest title
                        else
                            BookRQ.Append("<titulo>Sra.</titulo>"); //Guest title
                        BookRQ.Append("<nombrePax>" + FName + "</nombrePax>"); //Client Name 
                        BookRQ.Append("<apellidos>" + LName + "</apellidos>");//Client Surname 
                        if (Convert.ToString(PaxType).Trim().ToLower() == "adult")
                            BookRQ.Append("<edad>32</edad>");//Age
                        else
                            BookRQ.Append("<edad>" + ChildAgex + "</edad>");//Age
                        BookRQ.Append("</pax>");
                    }
                }
               
                BookRQ.Append("</paxes>");
                BookRQ.Append("</parametros>");
                BookRQ.Append("</peticion>");
                string BookingRQ = BookRQ.ToString();

                return BookingRQ;
                #endregion
            }
            catch (Exception ex)
            {
                _ =_exception.Error(ex);
                return "";
            }
        }

        public string FinalRestelBookingRequest(string localizer, string confi)
        {

            StringBuilder sbfinal = new StringBuilder();
            sbfinal.Append("<?xml version=\"1.0\" encoding=\"iso-8859-1\" ?>");
            sbfinal.Append("<!DOCTYPE peticion SYSTEM \"http://xml.hotelresb2b.com/xml/dtd/pet_reserva.dtd\">");
            sbfinal.Append("<peticion>");
            sbfinal.Append("<nombre></nombre>");
            sbfinal.Append("<agencia></agencia>");
            sbfinal.Append("<tipo>3</tipo>");
            sbfinal.Append("<parametros>");
            sbfinal.Append("<localizador>" + localizer + "</localizador>");
            if (confi == "00")
            { sbfinal.Append("<accion>AE</accion>"); }
            else
            { sbfinal.Append("<accion>AI</accion>"); }
            sbfinal.Append("</parametros>");
            sbfinal.Append("</peticion>");

            return sbfinal.ToString();

        }

        public string Get_Restel_PreCancelRQRS(string BookingReference, DataRow[] RestelDetails, string UserID, string Password, DataTable DtCancellationError, ref double CancellationCost, ref string CancellationToken)
        {
            string PreCancellationRQ = "";
            try
            {
                #region Pre Booking Cancel RQ

                string Url = Convert.ToString(RestelDetails[0]["WebSiteURL"]);
                string BookRef = BookingReference.Split('|')[0];
                string BookRef1 = BookingReference.Split('|')[1];

                StringBuilder PreCancelRQ = new StringBuilder();
                PreCancelRQ.Append("<?xml version=\"1.0\" encoding=\"iso-8859-1\" ?>");
                PreCancelRQ.Append("<!DOCTYPE peticion SYSTEM \"http://xml.hotelresb2b.com/xml/dtd/pet_cancelacion.dtd\">");
                PreCancelRQ.Append("<peticion>");
                PreCancelRQ.Append("<nombre></nombre>");
                PreCancelRQ.Append("<agencia></agencia>");
                PreCancelRQ.Append("<tipo>401</tipo>");
                PreCancelRQ.Append("<parametros>");
                //PreCancelRQ.Append("<localizador_largo>"++"</localizador_largo>");
                //PreCancelRQ.Append("<localizador_corto>"++"</localizador_corto>");
                PreCancelRQ.Append("<localizador_largo>" + BookRef + "</localizador_largo>");
                PreCancelRQ.Append("<localizador_corto>" + BookRef1 + "</localizador_corto>");
                PreCancelRQ.Append("</parametros>");
                PreCancelRQ.Append("</peticion>");
                PreCancellationRQ = PreCancelRQ.ToString();
                #endregion
            }
            catch (Exception ex)
            {
                _ =_exception.ErrorNew(ex, _LogAPI.CancelBooking);
            }
            return PreCancellationRQ;
        }
        public string Get_Restel_PreCancelRQRS_newversion(string BookingReference, DataRow[] RestelDetails, string UserID, string Password, ref double CancellationCost, ref string CancellationToken)
        {
            string PreCancellationRQ = "";
            try
            {
                #region Pre Booking Cancel RQ

                string Url = Convert.ToString(RestelDetails[0]["WebSiteURL"]);
                string BookRef = BookingReference.Split('|')[0];
                string BookRef1 = BookingReference.Split('|')[1];

                StringBuilder PreCancelRQ = new StringBuilder();
                PreCancelRQ.Append("<?xml version=\"1.0\" encoding=\"iso-8859-1\" ?>");
                PreCancelRQ.Append("<!DOCTYPE peticion SYSTEM \"http://xml.hotelresb2b.com/xml/dtd/pet_cancelacion.dtd\">");
                PreCancelRQ.Append("<peticion>");
                PreCancelRQ.Append("<nombre></nombre>");
                PreCancelRQ.Append("<agencia></agencia>");
                PreCancelRQ.Append("<tipo>401</tipo>");
                PreCancelRQ.Append("<parametros>");
                //PreCancelRQ.Append("<localizador_largo>"++"</localizador_largo>");
                //PreCancelRQ.Append("<localizador_corto>"++"</localizador_corto>");
                PreCancelRQ.Append("<localizador_largo>" + BookRef + "</localizador_largo>");
                PreCancelRQ.Append("<localizador_corto>" + BookRef1 + "</localizador_corto>");
                PreCancelRQ.Append("</parametros>");
                PreCancelRQ.Append("</peticion>");
                PreCancellationRQ = PreCancelRQ.ToString();
                #endregion
            }
            catch (Exception ex)
            {
                _ = _exception.ErrorNew(ex, _LogAPI.CancelBooking);
            }
            return PreCancellationRQ;
        }
    }
}
