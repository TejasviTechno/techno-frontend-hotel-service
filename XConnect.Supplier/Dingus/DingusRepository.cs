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
                                                        Xhotel.ReturnRooms = 5; // IWTXDetails[0].ReturnRooms; //190225
                                                        Xhotel.TechName = DingusDetails[0].TechName; //190225
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
                                                    if (Dingus_Currency.ToLower() == "ars" || Dingus_Currency.ToLower() == "usd" || Dingus_Currency.ToLower() == "eur" || Dingus_Currency.ToLower() == "brl")
                                                    {
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
                                                    }
                                                    else
                                                    {
                                                        continue;
                                                    }
                                                    //end
                                                    //int Adult = Convert.ToInt32(SearchParam.Rooms[x].NoofAdults);
                                                    //int Child = Convert.ToInt32(SearchParam.Rooms[x].NoofChild);
                                                    int Adult = Convert.ToInt32(SearchParam.Rooms[RoomNo - 1].NoofAdults);
                                                    int Child = Convert.ToInt32(SearchParam.Rooms[RoomNo - 1].NoofChild);

                                                    string roomTypeId = Convert.ToString(10 + "$$$" + RoomTypeCode + "$$$" + MealPlanCodes + "$$$" + RatePlanID + "$$$" + RatePlanCode + "$$$" + HotelCode + "$$$" + PromotionCode + "$$$" + ChainCode);
                                                    string uniqid = Guid.NewGuid().ToString();
                                                    #region RateBrekUp
                                                    double Night_Price = 0.0;

                                                    for (int m = 0; m < SearchParam.NoofNights; m++)
                                                    {
                                                        Night_Price = itemprice / SearchParam.NoofNights;
                                                        string newCheckInDate = Convert.ToString(SearchParam.CheckInDate);
                                                        string newCheckOutDate = Convert.ToString(SearchParam.CheckOutDate);
                                                        DateTime dtCheckInDate = DateTime.ParseExact(newCheckInDate, "dd/MM/yyyy", null);
                                                        DateTime dtCheckOutDate = DateTime.ParseExact(newCheckOutDate, "dd/MM/yyyy", null);
                                                        TimeSpan t = dtCheckOutDate - dtCheckInDate;
                                                        #region Vat
                                                        Double VatOnRateBrackUp = 0;
                                                        double Pricenew_wl = 0;

                                                        double VatOnPerNight = 0;
                                                        double VatonfirstnightPrice = 0;
                                                        double ratePerNight = 0;

                                                        if (TaxPer != 0)
                                                            Night_Price = CalculateAmountWithTax(Night_Price, TaxPer);

                                                        ratePerNight = _markup.ProcessHotelRateBreakupPrice_WithMarkup(1, SearchParam.CityID, SearchParam.CountryID.ToString(), Night_Price, B2CMarkup, IsParentId, isPerce, AgentPercentage, AgentMarkupAmount, SubAgentMarkup, ref VatOnPerNight);
                                                        #endregion




                                                        VatonfirstnightPrice += VatOnPerNight;
                                                        DataRow drHotelBedsV1RoomRateBreackUp = dtHotelBedsV1RoomRateBreakup.NewRow();
                                                        drHotelBedsV1RoomRateBreackUp["SupplierName"] = Provider;
                                                        drHotelBedsV1RoomRateBreackUp["OwnsystemHotelId"] = ownsystemhotelId;
                                                        drHotelBedsV1RoomRateBreackUp["ContractID"] = 0;
                                                        drHotelBedsV1RoomRateBreackUp["RoomNo"] = RoomNo;
                                                        drHotelBedsV1RoomRateBreackUp["RoomTypeId"] = roomTypeId;
                                                        drHotelBedsV1RoomRateBreackUp["MealId"] = MealPlanDescription;
                                                        drHotelBedsV1RoomRateBreackUp["Meal"] = MealPlanDescription;
                                                        string rateWeekColumn = Convert.ToString(dtCheckInDate.Date.AddDays(m).DayOfWeek).Substring(0, 3);
                                                        drHotelBedsV1RoomRateBreackUp["DateOfWeek"] = rateWeekColumn;
                                                        DateTime rateDate = dtCheckInDate.Date.AddDays(m);
                                                        drHotelBedsV1RoomRateBreackUp["Date"] = rateDate;
                                                        drHotelBedsV1RoomRateBreackUp["Rate"] = ratePerNight;
                                                        drHotelBedsV1RoomRateBreackUp["Rate"] = Pricenew_wl;
                                                        drHotelBedsV1RoomRateBreackUp["OrderNo"] = 1;
                                                        drHotelBedsV1RoomRateBreackUp["RoomTypeName"] = RoomName;
                                                        drHotelBedsV1RoomRateBreackUp["RoomSequenceNo"] = uniqid;
                                                        drHotelBedsV1RoomRateBreackUp["DateOfWeek"] = rateWeekColumn;
                                                        drHotelBedsV1RoomRateBreackUp["Date"] = rateDate;
                                                        drHotelBedsV1RoomRateBreackUp["BookingStatus"] = "Available";
                                                        drHotelBedsV1RoomRateBreackUp["BoardTypeID"] = "";
                                                        drHotelBedsV1RoomRateBreackUp["VatdiffAmount"] = VatOnPerNight;
                                                        drHotelBedsV1RoomRateBreackUp["RateType"] = "A Rate";
                                                        drHotelBedsV1RoomRateBreackUp["SupplierCurrency"] = Currency;
                                                        drHotelBedsV1RoomRateBreackUp["BuyingPrice"] = ratePerNight / CurrencyFactor;
                                                        //drHotelBedsV1RoomRateBreackUp["BuyingPrice_wl"] = ratePerNight / CurrencyFactor;
                                                        dtHotelBedsV1RoomRateBreakup.Rows.Add(drHotelBedsV1RoomRateBreackUp);
                                                    }

                                                    #endregion

                                                    #region Room

                                                    DataRow drHotelBedsV1Room = dtHotelBedsV1Room.NewRow();
                                                    drHotelBedsV1Room["OwnsystemHotelId"] = ownsystemhotelId;
                                                    drHotelBedsV1Room["RoomTypeCode"] = "";
                                                    drHotelBedsV1Room["BoardType"] = MealPlanDescription;

                                                    #region Vat
                                                    double mainbuy = itemprice;


                                                    _markup.ProcessHotelRoom_WithMarkup_nites(CurrencyFactor, Currency, false, false, SearchParam.NoofNights, SearchParam.CityID, SearchParam.CountryID.ToString(), SearchParam.CheckInDate, SearchParam.CheckOutDate, ref itemprice, ref drHotelBedsV1Room, isPerce, AgentPercentage, AgentMarkupAmount, SubAgentMarkup, B2CMarkup, IsParentId, SupplierID, GiataId, dsAgentDetails, dtVatHotelList);

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
                                                    drHotelBedsV1Room["FinalSellingPrice"] = mainbuy; //03042026
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
                    if (Dingus_Currency1.ToLower() == "ars" || Dingus_Currency1.ToLower() == "usd" || Dingus_Currency1.ToLower() == "eur" || Dingus_Currency1.ToLower() == "brl")
                    {
                        if (Dingus_Currency1.ToLower() != commonSearch.tokenData.SiteCurrency.ToLower())
                        {
                            double CurrencyFactor1 = _commonMethod.GetROEByCurrency(Dingus_Currency1, dtROE);
                            room1Price = room1Price * CurrencyFactor1;
                        }
                    }
                    else
                    {
                        continue;
                    }
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
        public async Task<PriceCheckCMDto> PriceUpdateDingus(List<DingusRoom> DRList, string supplierName, string RoomTypeCode, string MealPlanCodes, string RatePlanID, string RatePlanCode, DataTable dtCheckPriceReqStructure, DataSet dsCartDetails, DataRow[] DingusDetails, string UniqueNo, string AgentUniqueNo, int i, int j, string IsFor, double CompanyBuyPrice, bool IsError, double itemprice, HotelSearchParameter SearchPara, TokenData tokenData, string Searchkey, int AgentId = 0)
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

                string DingusCurrency = Convert.ToString(DingusDetails[0]["CurrencyShortName"]);
                double CurrencyFactorDingus = 1;
                if (DingusCurrency.ToLower() != tokenData.SiteCurrency.ToLower())
                {
                    CurrencyFactorDingus = _commonMethod.GetROEByCurrency(DingusCurrency, tokenData.dtROE);
                }

                DataSet dsTemp = new DataSet();
                string DingusUrl = Convert.ToString(DingusDetails[0]["WebSiteURL"]);
                string UserId = Convert.ToString(DingusDetails[0]["UserID"]);
                string UserName = Convert.ToString(DingusDetails[0]["UserName"]);
                string Password = Convert.ToString(DingusDetails[0]["Password"]);
                string RoomId = "";
                //DateTime CheckInDate = Convert.ToDateTime(dsCartDetails.Tables[0].Rows[0]["StrCheckInDate"]);
                //DateTime CheckOutDate = Convert.ToDateTime(dsCartDetails.Tables[0].Rows[0]["StrCheckOutDate"]);
                //string checkIn = String.Format("{0:yyyy-MM-dd}", DateTime.ParseExact(CheckInDate, "dd/MM/yyyy", null));
                //string checkOut = String.Format("{0:yyyy-MM-dd}", DateTime.ParseExact(CheckOutDate, "dd/MM/yyyy", null));
                string HotelCode = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["XMLHotelId"]);

                string CheckIn = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["CheckInDate"]);
                CheckIn = CheckIn.Substring(0, 4) + "-" + CheckIn.Substring(4, 2) + "-" + CheckIn.Substring(6, 2);
                string CheckOut = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["CheckOutDate"]);
                CheckOut = CheckOut.Substring(0, 4) + "-" + CheckOut.Substring(4, 2) + "-" + CheckOut.Substring(6, 2);

                #region PriceCheckRequest

                String Request = DingusPriceCheckRequest(DRList, CheckIn, CheckOut, HotelCode, UserName, Password, DingusUrl, supplierName, language);

                #endregion

                _commonMethod.ThirdPartyBookingHistory(Convert.ToString(UniqueNo), Convert.ToString(AgentUniqueNo), Request, "-", "Before Request hit Price Check" + IsFor, Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["SupplierName"]), "Request Confirm", "-", "WithInEarth");

                dtCheckPriceReqStructure.Rows.Add(_commonMethod.AddCheckPriceReq(dtCheckPriceReqStructure.NewRow(), j + 1, Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[j]["UserCartID"]), Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[j]["UserCartDetailId"]), Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomTypeID"]), Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["SupplierName"]).ToLower(), Request));

                ResponseDto responseDto = await Get_Dingus_Response(DingusUrl, Request, supplierName, Error);

                _commonMethod.ThirdPartyBookingHistory(Convert.ToString(UniqueNo), Convert.ToString(AgentUniqueNo), Request, ResponseData + Error, "After Request hit Price Check" + IsFor, Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["SupplierName"]), "Response Confirm", "-", "WithInEarth");

                RequestSPR = Request;
                ResponseData = responseDto.Response;
                Error = responseDto.Error;


                if (ResponseData.ToLower().Contains("errors") || ResponseData.Trim() == "")
                {
                    #region SupplierPerformanceReportForPrebook
                    try
                    {
                        responsetype = "GeneralError";
                        string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(supplier, "PreBook", AgentId, time, responsetype, RequestSPR, ResponseSPR, Error);
                    }
                    catch (Exception e)
                    {
                        _ = _exception.Error(e, Searchkey);
                    }
                    #endregion
                    return priceCheckDingus;
                }

                DataSet Dingus_Response = _commonMethod.GetDataSetFromXMLString(ResponseData);

                if (Dingus_Response == null)
                {
                    #region SupplierPerformanceReportForPrebook
                    try
                    {
                        responsetype = "GeneralError";
                        string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(supplier, "PreBook", AgentId, time, responsetype, RequestSPR, ResponseSPR, Error);
                    }
                    catch (Exception e)
                    {
                        _ = _exception.Error(e, Searchkey);
                    }
                    #endregion
                    return priceCheckDingus;
                }

                if (Dingus_Response.Tables["RoomRate"] != null)
                {
                    DataRow[] drRoomRate = Dingus_Response.Tables["RoomRate"].Select("RoomTypeCode='" + RoomTypeCode + "' and MealPlanCodes='" + MealPlanCodes + "' and RatePlanID='" + RatePlanID + "' and RatePlanCode='" + RatePlanCode + "'");

                    if (drRoomRate.Length > 0)
                    {
                        string RoomRate_Id = Convert.ToString(drRoomRate[0]["RoomRate_Id"]);

                        DataRow[] drTotal = Dingus_Response.Tables["Total"].Select("RoomRate_Id='" + RoomRate_Id + "'");

                        if (drTotal.Length > 0)
                        {
                            double FinalPriceMain = 0;
                            double FinalPriceTemp = Convert.ToDouble(drTotal[0]["AmountAfterTax"]);
                            string Currency = Convert.ToString(drTotal[0]["CurrencyCode"]);


                            if (Currency.ToLower() != "usd")
                            {
                                DataTable dtInrCurrency1 = _commonMethod.GetCountryCurrency(Currency);
                                double CurrencyFactorAdonis1 = Convert.ToDouble(dtInrCurrency1.Rows[0]["AmtToExch"]);
                                FinalPriceMain = FinalPriceTemp * CurrencyFactorAdonis1;

                            }
                            else
                            {
                                FinalPriceMain = FinalPriceTemp * CurrencyFactorDingus;
                            }

                            itemprice = Convert.ToDouble(FinalPriceMain);

                            itemprice = Convert.ToDouble(Math.Round(itemprice, 2));
                            if (Convert.ToDouble(Math.Round(itemprice.TruncateDecimalPlaces(0))) > Math.Round(CompanyBuyPrice.TruncateDecimalPlaces(0)))
                            {
                                #region SupplierPerformanceReportForPrebook
                                try
                                {
                                    responsetype = "GeneralError";
                                    string ResponseData_Ex = "CompanyBuyingPrices = " + CompanyBuyPrice + " MainPrice = " + itemprice + " #Response: " + ResponseSPR;
                                    string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(supplier, "PreBook", AgentId, time, responsetype, RequestSPR, ResponseData_Ex, Error);
                                }
                                catch (Exception e)
                                {
                                    _ = _exception.Error(e, Searchkey);
                                }
                                #endregion
                                return priceCheckDingus;
                            }
                        }
                        else
                        {
                            #region SupplierPerformanceReportForPrebook
                            try
                            {
                                responsetype = "GeneralError";
                                string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(supplier, "PreBook", AgentId, time, responsetype, RequestSPR, ResponseSPR, Error);
                            }
                            catch (Exception e)
                            {
                                _ = _exception.Error(e, Searchkey);
                            }
                            #endregion
                            return priceCheckDingus;
                        }
                    }
                    else
                    {
                        #region SupplierPerformanceReportForPrebook
                        try
                        {
                            responsetype = "GeneralError";
                            string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(supplier, "PreBook", AgentId, time, responsetype, RequestSPR, ResponseSPR, Error);
                        }
                        catch (Exception e)
                        {
                            _ = _exception.Error(e, Searchkey);
                        }
                        #endregion
                        return priceCheckDingus;
                    }

                }
                else
                {
                    #region SupplierPerformanceReportForPrebook
                    try
                    {
                        responsetype = "GeneralError";
                        string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(supplier, "PreBook", AgentId, time, responsetype, RequestSPR, ResponseSPR, Error);
                    }
                    catch (Exception e)
                    {
                        _ = _exception.Error(e, Searchkey);
                    }
                    #endregion
                    return priceCheckDingus;
                }
                #region SupplierPerformanceReportForPrebook
                try
                {
                    responsetype = "Success";
                    string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(supplier, "PreBook", AgentId, time, responsetype, RequestSPR, ResponseSPR, Error);
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
            return priceCheckDingus;
        }


        public string DingusPriceCheckRequest(List<DingusRoom> DR, string checkIn, string checkOut, string HotelId, string Username, string Password, string DingusUrl, string SupplierName, string language)
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

                    for (int i = 0; i < DR.Count; i++)
                    {
                        HotelAvailRQ.Append("<m:RoomStayCandidate RPH=\"" + (i + 1) + "\" Quantity=\"" + 1 + "\" >");
                        HotelAvailRQ.Append("<m:GuestCounts>");
                        int NoofChild = DR[i].NoofChild;
                        int NoofAdults = Convert.ToInt32(DR[i].NoofAdults);

                        switch (NoofChild)
                        {
                            case 1:
                                if (DR[i].Child1Age > 12)
                                    NoofAdults = NoofAdults + 1;
                                break;
                            case 2:
                                if (DR[i].Child1Age > 12)
                                    NoofAdults = NoofAdults + 1;
                                if (DR[i].Child2Age > 12)
                                    NoofAdults = NoofAdults + 1;
                                break;
                            case 3:
                                if (DR[i].Child1Age > 12)
                                    NoofAdults = NoofAdults + 1;
                                if (DR[i].Child2Age > 12)
                                    NoofAdults = NoofAdults + 1;
                                if (DR[i].Child3Age > 12)
                                    NoofAdults = NoofAdults + 1;
                                break;
                            case 4:
                                if (DR[i].Child1Age > 12)
                                    NoofAdults = NoofAdults + 1;
                                if (DR[i].Child2Age > 12)
                                    NoofAdults = NoofAdults + 1;
                                if (DR[i].Child3Age > 12)
                                    NoofAdults = NoofAdults + 1;
                                if (DR[i].Child4Age > 12)
                                    NoofAdults = NoofAdults + 1;
                                break;
                        }


                        if (NoofAdults > 0)
                        {
                            HotelAvailRQ.Append("<m:GuestCount Count=\"" + NoofAdults + "\" AgeQualifyingCode=\"10\" Age=\"30\" />");
                        }
                        if (DR[i].NoofChild > 0)
                        {
                            if (DR[i].NoofChild == 1)
                            {
                                if (DR[i].Child1Age < 2)
                                {
                                    HotelAvailRQ.Append("<m:GuestCount Count=\"" + 1 + "\" AgeQualifyingCode=\"7\" Age=\"" + DR[i].Child1Age + "\" />");
                                }
                                else if (DR[i].Child1Age > 12)
                                {
                                    //HotelAvailRQ.Append("<m:GuestCount Count=\"" + 1 + "\" AgeQualifyingCode=\"7\" Age=\"" + DR[i].Child1Age + "\" />");
                                }
                                else
                                {
                                    HotelAvailRQ.Append("<m:GuestCount Count=\"" + 1 + "\" AgeQualifyingCode=\"8\" Age=\"" + DR[i].Child1Age + "\" />");
                                }

                            }
                            if (DR[i].NoofChild == 2)
                            {
                                #region Child1Age
                                if (DR[i].Child1Age < 2)
                                {
                                    HotelAvailRQ.Append("<m:GuestCount Count=\"" + 1 + "\" AgeQualifyingCode=\"7\" Age=\"" + DR[i].Child1Age + "\" />");
                                }
                                else if (DR[i].Child1Age > 12)
                                {
                                    //HotelAvailRQ.Append("<m:GuestCount Count=\"" + 1 + "\" AgeQualifyingCode=\"7\" Age=\"" + DR[i].Child1Age + "\" />");
                                }
                                else
                                {
                                    HotelAvailRQ.Append("<m:GuestCount Count=\"" + 1 + "\" AgeQualifyingCode=\"8\" Age=\"" + DR[i].Child1Age + "\" />");
                                }
                                #endregion
                                #region Child2Age
                                if (DR[i].Child2Age < 2)
                                {
                                    HotelAvailRQ.Append("<m:GuestCount Count=\"" + 1 + "\" AgeQualifyingCode=\"7\" Age=\"" + DR[i].Child2Age + "\" />");
                                }
                                else if (DR[i].Child2Age > 12)
                                {
                                    //HotelAvailRQ.Append("<m:GuestCount Count=\"" + 1 + "\" AgeQualifyingCode=\"7\" Age=\"" + DR[i].Child1Age + "\" />");
                                }
                                else
                                {
                                    HotelAvailRQ.Append("<m:GuestCount Count=\"" + 1 + "\" AgeQualifyingCode=\"8\" Age=\"" + DR[i].Child2Age + "\" />");
                                }
                                #endregion
                            }

                            if (DR[i].NoofChild == 3)
                            {
                                #region Child1Age
                                if (DR[i].Child1Age < 2)
                                {
                                    HotelAvailRQ.Append("<m:GuestCount Count=\"" + 1 + "\" AgeQualifyingCode=\"7\" Age=\"" + DR[i].Child1Age + "\" />");
                                }
                                else if (DR[i].Child1Age > 12)
                                {
                                    //HotelAvailRQ.Append("<m:GuestCount Count=\"" + 1 + "\" AgeQualifyingCode=\"7\" Age=\"" + DR[i].Child1Age + "\" />");
                                }
                                else
                                {
                                    HotelAvailRQ.Append("<m:GuestCount Count=\"" + 1 + "\" AgeQualifyingCode=\"8\" Age=\"" + DR[i].Child1Age + "\" />");
                                }
                                #endregion
                                #region Child2Age
                                if (DR[i].Child2Age < 2)
                                {
                                    HotelAvailRQ.Append("<m:GuestCount Count=\"" + 1 + "\" AgeQualifyingCode=\"7\" Age=\"" + DR[i].Child2Age + "\" />");
                                }
                                else if (DR[i].Child2Age > 12)
                                {
                                    //HotelAvailRQ.Append("<m:GuestCount Count=\"" + 1 + "\" AgeQualifyingCode=\"10\" Age=\"" + DR[i].Child2Age + "\" />");
                                    //DR[i].NoofAdults = +1;
                                }
                                else
                                {
                                    HotelAvailRQ.Append("<m:GuestCount Count=\"" + 1 + "\" AgeQualifyingCode=\"8\" Age=\"" + DR[i].Child2Age + "\" />");
                                }
                                #endregion
                                #region Child3Age
                                if (DR[i].Child3Age > 1)
                                {
                                    HotelAvailRQ.Append("<m:GuestCount Count=\"" + 1 + "\" AgeQualifyingCode=\"8\" Age=\"" + DR[i].Child3Age + "\" />");
                                }
                                else if (DR[i].Child3Age > 12)
                                {
                                    //HotelAvailRQ.Append("<m:GuestCount Count=\"" + 1 + "\" AgeQualifyingCode=\"10\" Age=\"" + DR[i].Child3Age + "\" />");
                                    //DR[i].NoofAdults = +1;
                                }
                                else
                                {
                                    HotelAvailRQ.Append("<m:GuestCount Count=\"" + 1 + "\" AgeQualifyingCode=\"7\" Age=\"" + DR[i].Child3Age + "\" />");
                                }
                                #endregion

                            }
                            if (DR[i].NoofChild == 4)
                            {
                                #region Child1Age
                                if (DR[i].Child1Age < 2)
                                {
                                    HotelAvailRQ.Append("<m:GuestCount Count=\"" + 1 + "\" AgeQualifyingCode=\"7\" Age=\"" + DR[i].Child1Age + "\" />");
                                }
                                else if (DR[i].Child1Age > 12)
                                {
                                    //HotelAvailRQ.Append("<m:GuestCount Count=\"" + 1 + "\" AgeQualifyingCode=\"7\" Age=\"" + DR[i].Child1Age + "\" />");
                                }
                                else
                                {
                                    HotelAvailRQ.Append("<m:GuestCount Count=\"" + 1 + "\" AgeQualifyingCode=\"8\" Age=\"" + DR[i].Child1Age + "\" />");
                                }
                                #endregion
                                #region Child2Age
                                if (DR[i].Child2Age < 2)
                                {
                                    HotelAvailRQ.Append("<m:GuestCount Count=\"" + 1 + "\" AgeQualifyingCode=\"7\" Age=\"" + DR[i].Child2Age + "\" />");
                                }
                                else if (DR[i].Child2Age > 12)
                                {
                                    //HotelAvailRQ.Append("<m:GuestCount Count=\"" + 1 + "\" AgeQualifyingCode=\"7\" Age=\"" + DR[i].Child1Age + "\" />");
                                }
                                else
                                {
                                    HotelAvailRQ.Append("<m:GuestCount Count=\"" + 1 + "\" AgeQualifyingCode=\"8\" Age=\"" + DR[i].Child2Age + "\" />");
                                }
                                #endregion
                                #region Child3Age
                                if (DR[i].Child3Age > 1)
                                {
                                    HotelAvailRQ.Append("<m:GuestCount Count=\"" + 1 + "\" AgeQualifyingCode=\"8\" Age=\"" + DR[i].Child3Age + "\" />");
                                }
                                else if (DR[i].Child3Age > 12)
                                {
                                    //HotelAvailRQ.Append("<m:GuestCount Count=\"" + 1 + "\" AgeQualifyingCode=\"7\" Age=\"" + DR[i].Child1Age + "\" />");
                                }
                                else
                                {
                                    HotelAvailRQ.Append("<m:GuestCount Count=\"" + 1 + "\" AgeQualifyingCode=\"7\" Age=\"" + DR[i].Child3Age + "\" />");
                                }
                                #endregion
                                #region Child4Age
                                if (DR[i].Child4Age > 1)
                                {
                                    HotelAvailRQ.Append("<m:GuestCount Count=\"" + 1 + "\" AgeQualifyingCode=\"8\" Age=\"" + DR[i].Child4Age + "\" />");
                                }
                                else if (DR[i].Child4Age > 12)
                                {
                                    //HotelAvailRQ.Append("<m:GuestCount Count=\"" + 1 + "\" AgeQualifyingCode=\"7\" Age=\"" + DR[i].Child1Age + "\" />");
                                }
                                else
                                {
                                    HotelAvailRQ.Append("<m:GuestCount Count=\"" + 1 + "\" AgeQualifyingCode=\"7\" Age=\"" + DR[i].Child4Age + "\" />");
                                }
                                #endregion
                            }
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

            return responseDto;

        }


    }
}
