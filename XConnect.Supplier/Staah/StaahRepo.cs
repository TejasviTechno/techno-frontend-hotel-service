using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Azure.Core.HttpHeader;
using XConnect.Models.Hotel;
using XConnect.Models;
using Microsoft.Extensions.Options;
using XConnect.CommonUtilities.Interface;
using XConnect.ErrorLogHandler.Interface;
using XConnect.Markup.Interface;
using Newtonsoft.Json;
using XConnect.CommonUtilities;
using XConnect.Supplier.Staah.Interface;
using Options = XConnect.Models.Options;
using Microsoft.IdentityModel.Protocols;
using System.Xml;
using System.Net;
using Microsoft.Data.SqlClient;
using XConnect.DAL;
using static XConnect.Supplier.Booking.Com.preebookrs;

namespace XConnect.Supplier.Staah
{
    public class StaahRepo : IStaah
    {
        private readonly ICommonMethod _commonMethod;
        private readonly IException _exception;
        private readonly IMarkup _markup;
        private readonly ISupplierRes _supplierRes;
        private readonly KeySettings _keySettings;

        public StaahRepo(ICommonMethod commonMethod, IException exception, IMarkup markup, ISupplierRes supplierRes, IOptions<KeySettings> keySettings)
        {
            _commonMethod = commonMethod;
            _exception = exception;
            _markup = markup;
            _supplierRes = supplierRes;
            _keySettings = keySettings.Value;
        }
        public void GetStaahRequestOnly_async(List<SupplierDetails> StaahDetails, DataRow[] commonCode, List<StarRating> listRating, DataTable dtPreferredDataHotel, HotelSearchParameter SearchPara, List<SupplierSearchConfig> StaahSearchConfig, ref ConcurrentBag<HotelX> AllTask, DataTable dtAgentDetail, DataSet dsAgentDetails, DataTable dtMarkupDetails, decimal agentPercent, DataTable OwnIDSList, int Hit)
        {
            try
            {
                DataRow[] drrayna = dtPreferredDataHotel.Select("ServiceName='Staah'");
                string Ids = string.Empty;
                DataSet dsTemp = new DataSet();
                dsTemp.Tables.Add(_commonMethod.GenerateDataTableNewV2());
                //File.WriteAllText(@"F:\techno\derbytxt.txt", "1");
                if (SearchPara.IsSearchFor.ToLower().IndexOf("preferred") >= 0 && drrayna.Count() == 0)
                    StaahSearchConfig = null;
                if (StaahSearchConfig != null)
                // double agentPercent = 0.0;
                {

                    //File.WriteAllText(@"F:\techno\derbytxt.txt", "2");
                    for (int s = 0; s < StaahSearchConfig.Count; s++)
                    {
                        List<StarRating> lstRating = new List<StarRating>();
                        lstRating = _commonMethod.GetStarRatingStringList(SearchPara);

                        if (Convert.ToInt32(StaahSearchConfig[s].CityID) == 0 || Convert.ToInt32(StaahSearchConfig[s].CityID) == SearchPara.CityID)//All City means 0
                        {
                            HotelSearchParameter para = _commonMethod.ResetSearchParameter(SearchPara);
                            HotelSearchParameter newsearchpar = null;
                            string NewPara = JsonConvert.SerializeObject(para);
                            if (!string.IsNullOrEmpty(NewPara))
                                newsearchpar = DeepCopy.HCloner.DeepCopy(JsonConvert.DeserializeObject<HotelSearchParameter>(NewPara));
                            bool IsSame = true;
                            if (SearchPara.Filters.IsShowRooms != 1)
                            {
                                int starcount = lstRating.Count();
                                starcount = 1;
                                if (IsSame)
                                {

                                    if (commonCode != null)
                                    {
                                        bool HitOth = true;
                                        starcount = 1;
                                        //if (OwnIDSList.Rows.Count > 0)
                                        //{
                                        //    string Request = Convert.ToString(OwnIDSList.Rows[0]["Ids"]).Replace("'", "");
                                        //    if (Request == "No")
                                        //        HitOth = false;
                                        //}
                                        //else
                                        //    HitOth = false;
                                        DataTable[] splittedtables = commonCode.AsEnumerable()
                                                            .Select((row, index) => new { row, index })
                                                            .GroupBy(x => x.index / StaahSearchConfig[s].Noofrecords)  // integer division, the fractional part is truncated
                                                            .Select(g => g.Select(x => x.row).CopyToDataTable())
                                                            .ToArray();
                                        for (int a = 0; a < splittedtables.Length; a++)
                                        {
                                            if (HitOth)
                                            {
                                                //for (int a = 0; a < 1; a++)
                                                {
                                                    //string HIDs = string.Empty;
                                                    //if (OwnIDSList.Columns.Contains("Ids"))
                                                    //{
                                                    //    if (OwnIDSList.Rows.Count > 0)
                                                    //    {
                                                    //        HIDs = Convert.ToString(OwnIDSList.Rows[0]["Ids"]).Replace("'", "");
                                                    //    }
                                                    //}
                                                    int Str = lstRating[a].Star;

                                                    var paramValues1 = String.Join(",", splittedtables[a].Select().Select(x => String.Join(",", Convert.ToString(x.ItemArray[0]))));
                                                    string IdsForSearch = Convert.ToString(paramValues1);

                                                    HotelX xhotel = new HotelX();
                                                    xhotel.StarRating = Str;
                                                    xhotel.Supplier = StaahDetails[0].SupplierName;
                                                    xhotel.DebugeMode = true;
                                                    xhotel.SearchTimeOut = Convert.ToInt32(StaahSearchConfig[s].HotelSearchTimeOut);
                                                    xhotel.ProcessTimeout = Convert.ToInt32(StaahSearchConfig[s].HotelSearchTimeOut2);
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
                                                        objrq.Request = (IdsForSearch);
                                                        objrq.Response = ""; RQRS.Add(objrq);
                                                    }
                                                    xhotel.RQRS = RQRS;
                                                    #endregion
                                                    xhotel.SearchPara = SearchPara;
                                                    if (dtAgentDetail != null && dtAgentDetail.Rows.Count > 0)
                                                        xhotel.CID = Convert.ToString(dtAgentDetail.Rows[0]["AgentID"]);
                                                    xhotel.NewPara = Ids;
                                                    xhotel.StrHotelIds = IdsForSearch;
                                                    xhotel.TechName = StaahDetails[0].TechName;
                                                    xhotel.ReturnRooms = StaahDetails[0].ReturnRooms;
                                                    AllTask.Add(xhotel);
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        for (int a = 0; a < starcount; a++)
                                        {
                                            string HIDs = string.Empty;
                                            if (OwnIDSList.Columns.Contains("Ids"))
                                            {
                                                if (OwnIDSList.Rows.Count > 0)
                                                {
                                                    HIDs = Convert.ToString(OwnIDSList.Rows[0]["Ids"]).Replace("'", "");
                                                }
                                            }
                                            int Str = lstRating[a].Star;
                                            HotelX xhotel = new HotelX();
                                            xhotel.StarRating = Str;
                                            xhotel.Supplier = StaahDetails[0].SupplierName;
                                            xhotel.DebugeMode = true;
                                            xhotel.SearchTimeOut = Convert.ToInt32(StaahSearchConfig[s].HotelSearchTimeOut);
                                            xhotel.ProcessTimeout = Convert.ToInt32(StaahSearchConfig[s].HotelSearchTimeOut2);
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
                                                objrq.Request = (SearchPara.HotelID);
                                                objrq.Response = ""; RQRS.Add(objrq);
                                            }
                                            xhotel.RQRS = RQRS;
                                            #endregion
                                            xhotel.SearchPara = SearchPara;
                                            if (dtAgentDetail != null && dtAgentDetail.Rows.Count > 0)
                                                xhotel.CID = Convert.ToString(dtAgentDetail.Rows[0]["AgentID"]);
                                            xhotel.NewPara = Ids;
                                            xhotel.StrHotelIds = HIDs;
                                            xhotel.TechName = StaahDetails[0].TechName;
                                            xhotel.ReturnRooms = StaahDetails[0].ReturnRooms;
                                            AllTask.Add(xhotel);
                                        }
                                    }
                                }
                            }
                            else

                            {
                                if (commonCode != null)
                                {
                                    string EchoToken = "", Platform = "", XmlNationality = "";
                                    //if (drhotelStaah.Length > 0 && isHitStaah == 0 && !IsHitOther)
                                    DataTable[] splittedtables = commonCode.AsEnumerable()
                                                      .Select((row, index) => new { row, index })
                                                      .GroupBy(x => x.index / StaahSearchConfig[s].Noofrecords)  // integer division, the fractional part is truncated
                                                      .Select(g => g.Select(x => x.row).CopyToDataTable())
                                                      .ToArray();

                                    for (int a = 0; a < splittedtables.Length; a++)
                                    {
                                        //isHitStaah = 1;
                                        //HotelSearchParameter para = Common.ResetSearchParameter(SearchPara);
                                        var paramValues1 = String.Join(",", splittedtables[a].Select().Select(x => String.Join(",", Convert.ToString(x.ItemArray[0]))));
                                        //var paramValues1 = String.Join(",", splittedtables[a].Select().Select(x => String.Join(",", Convert.ToString(x.ItemArray[1]))));
                                        string IdsForSearch = Convert.ToString(paramValues1);
                                        string ownsystemhotelId = "";
                                        //string ownsystemhotelId = Convert.ToString(drrayna[0]["OwnsystemHotelId"]);
                                        HotelX xhotel = new HotelX();
                                        para.HotelID = "";
                                        para.HotelName = "";
                                        NewPara = JsonConvert.SerializeObject(para);
                                        Ids = Convert.ToString(paramValues1);
                                        //HotelSearchParameter newsearchpar = null;
                                        //if (!string.IsNullOrEmpty(NewPara))
                                        //newsearchpar = DeepCopy.HCloner.DeepCopy(JsonConvert.DeserializeObject<HotelSearchParameter>(NewPara));
                                        string Provider = StaahDetails[0].SupplierName;
                                        //int TimeoutRoom = Convert.ToInt32(drrayna[0]["RoomSearchTimeOut"]);
                                        //if (Convert.ToInt32(drrayna[0]["Hit"]) > 0)
                                        // TimeoutRoom = Convert.ToInt32(drrayna[0]["RoomSearchTimeOut2"]);
                                        bool DebugMode = true;
                                        //string XMLReq = Common.GenerateGTARequestAsync(newsearchpar, agentPercent);
                                        // Common.GetXmlDataForSearch(newsearchpar.Nationality, dtXMLCity, "alphatours", ref xmlCity, ref xmlCountry, ref XmlNationality);
                                        //HotelX xhotel = new HotelX();
                                        xhotel.Supplier = Provider;
                                        xhotel.Action = "";
                                        xhotel.CurrencyCode = StaahDetails[0].CurrencyShortName; //"IDR" 
                                        xhotel.CurrencyFactor = 1;
                                        xhotel.DebugeMode = DebugMode;
                                        xhotel.EchoToken = EchoToken;
                                        xhotel.MethoodType = "";
                                        xhotel.PassWord = "";
                                        xhotel.PlatForm = Platform;
                                        //xhotel.Response = "";
                                        xhotel.SearchTimeOut = Convert.ToInt32(StaahSearchConfig[s].HotelSearchTimeOut);
                                        xhotel.ProcessTimeout = Convert.ToInt32(StaahSearchConfig[s].HotelSearchTimeOut2);
                                        xhotel.URL = "";
                                        xhotel.UserId = "";
                                        xhotel.UserName = "";
                                        xhotel.Version = "";
                                        xhotel.XmlCity = "";
                                        xhotel.XmlCountry = "";
                                        xhotel.XmlNationality = "";
                                        xhotel.ownsystemhotelId = ownsystemhotelId;
                                        //xhotel.xmlhotelcode = Ids;
                                        xhotel.xmlhotelcode = IdsForSearch;
                                        //xhotel.hotelRating = hotelRating;
                                        xhotel.hotelRating = 0;
                                        xhotel.GIATACode = "";
                                        xhotel.dsTemp = dsTemp;
                                        //xhotel.NewPara = NewPara;
                                        xhotel.NewPara = Ids;
                                        xhotel.SearchPara = newsearchpar;
                                        //xhotel.XmlCity = Convert.ToString(item["XMLBASEID"]);
                                        xhotel.XmlCity = "";
                                        xhotel.StrHotelIds = "";
                                        xhotel.XmlNationality = XmlNationality;
                                        //xhotel.Request = "";
                                        //xhotel.Response = "";
                                        xhotel.CheckInDate = newsearchpar.CheckInDate;
                                        xhotel.CheckOutDate = newsearchpar.CheckOutDate;
                                        xhotel.CityId = newsearchpar.CityID;
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
                                            objrq.Request = GenerateGTARequestAsync_Staah(newsearchpar, agentPercent, IdsForSearch);
                                            objrq.Response = ""; RQRS.Add(objrq);
                                        }
                                        //RQRS = XMLReq;
                                        xhotel.RQRS = RQRS;
                                        xhotel.TechName = StaahDetails[0].TechName;
                                        xhotel.ReturnRooms = StaahDetails[0].ReturnRooms;
                                        #endregion
                                        AllTask.Add(xhotel);
                                    }
                                }


                                //AllTask.Add(xhotel);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _exception.Error(ex, SearchPara.SearchKey);
            }
        }

        public string GenerateGTARequestAsync_Staah(HotelSearchParameter objSearchPara, decimal AgentPercentage, string HotelID = "")
        {
            try
            {
                StringBuilder objRequest = new StringBuilder();
                objRequest.Append("<Hotel_Availability_GetAvailability>");
                objRequest.Append("<Param>");
                objRequest.Append("<RequestSection>");
                objRequest.Append("<Hotel>");
                objRequest.Append("<StayDateRange Start='" + objSearchPara.CheckInDate + "' End='" + objSearchPara.CheckOutDate + "' TotalNights='" + objSearchPara.NoofNights + "' />");
                //objRequest.Append("<StayDateRange Start='30/11/2015' End='01/12/2015' TotalNights='" + objSearchPara.NoofNights + "' />");
                objRequest.Append("<Currency>0</Currency>");
                objRequest.Append("<AgentPercentage>" + AgentPercentage + "</AgentPercentage>");
                objRequest.Append("<AgentCompanyPercentage>" + Convert.ToString(objSearchPara.AgentCompanyPercentage) + "</AgentCompanyPercentage>");
                objRequest.Append("<NoOfRecords>200</NoOfRecords>");
                objRequest.Append("<PageIndex>1</PageIndex>");
                objRequest.Append("<HotelSearchCriteria>");

                objRequest.Append("<HotelRef HotelCityName='" + objSearchPara.CityName + "' HotelCityCode='" + objSearchPara.CityID + "' HotelCountryCode='" + objSearchPara.CountryCode + "' Rating='' />");

                objRequest.Append("<Rooms>");
                //Loop through Rooms.
                var result = objSearchPara.Rooms.GroupBy(x => new { x.RoomName, x.NoofAdults, x.NoofChild, x.Child1Age, x.Child2Age }).Select(n => new { RoomName = n.Key.RoomName, NoofAdults = n.Key.NoofAdults, NoofChild = n.Key.NoofChild, ChildAge = n.ToList(), Child1Age = n.Key.Child1Age, Child2Age = n.Key.Child2Age, Count = n.Count() });
                foreach (var item in result)//For each Item
                {
                    for (int i = 0; i < item.Count; i++)
                    {
                        objRequest.Append("<Room Type='" + _commonMethod.GetType(item.NoofAdults) + "' ChildCount='" + item.NoofChild + "' AdultsCount='" + item.NoofAdults + "' ExtraBed='0' RateBasis='10785'>");//Question: What is 10785
                        switch (item.NoofChild)
                        {
                            case 0:
                                objRequest.Append("<Ages></Ages>");
                                break;
                            case 1:
                                objRequest.Append("<Ages><Age>" + item.Child1Age.ToString() + "</Age></Ages>");
                                break;
                            case 2:
                                objRequest.Append("<Ages>");
                                objRequest.Append("<Age>" + item.Child1Age.ToString() + "</Age>");
                                objRequest.Append("<Age>" + item.Child2Age.ToString() + "</Age>");
                                objRequest.Append("</Ages>");
                                break;
                        }
                        objRequest.Append("</Room>");
                    }
                }

                objRequest.Append("</Rooms>");
                objRequest.Append("</HotelSearchCriteria>");

                objRequest.Append("<HotelAdvanacedSearchCriteria>");
                objRequest.Append("<HotelId>" + HotelID + "</HotelId>");
                objRequest.Append("<HotelName>" + objSearchPara.HotelName + "</HotelName>");
                objRequest.Append("<Available>2</Available>");
                objRequest.Append("<StarRatingId></StarRatingId>");
                objRequest.Append("<StarRating></StarRating>");

                objRequest.Append("<LocationId>" + objSearchPara.LocationID + "</LocationId>");
                objRequest.Append("<Location>" + objSearchPara.Location + " </Location>");

                objRequest.Append("</HotelAdvanacedSearchCriteria>");

                objRequest.Append("<IndianResident>true</IndianResident>");

                objRequest.Append("</Hotel>");
                objRequest.Append("</RequestSection>");

                objRequest.Append("<Header>");
                objRequest.Append("<IBNUniqueId>2131271b-e47f-4871-8daf-1d0093342169</IBNUniqueId>");

                if (objSearchPara.UserName == null || Convert.ToString(objSearchPara.UserName).ToLower() == "guest user")
                    objRequest.Append("<UserId></UserId>");
                else
                    objRequest.Append("<UserId>" + objSearchPara.UserName + "</UserId>");
                objRequest.Append("<Username>" + objSearchPara.BookForUserCode + "</Username>");
                objRequest.Append("<Password>abc@123</Password>");
                objRequest.Append("<UserTokenId>IBM</UserTokenId>");
                objRequest.Append("<IsB2bB2c>" + objSearchPara.IsB2BOrB2C.ToString() + "</IsB2bB2c>");
                objRequest.Append("<Nationality>" + objSearchPara.Nationality + "</Nationality>");
                objRequest.Append("<Residency>" + objSearchPara.Residency + "</Residency>");
                objRequest.Append("<Supplier>GT</Supplier>");
                objRequest.Append("</Header>");
                objRequest.Append("</Param>");
                objRequest.Append("</Hotel_Availability_GetAvailability>");
                return objRequest.ToString();
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
            }
            return string.Empty;
        }

        public  async Task<List<Options>> Staah_HotelSearchWithRooms(HotelX Item, CommonSearch commonSearch, DataSet SiteminderResponse, HotelSearchParameter SearchParam, DataTable dtAgentDetail, DataTable dtSuplierMarkup, DataTable dtMarkupDetails, string currency, double currencyfactor, DataTable dtROE, Int32 processTimeout, string ResProccessTimerLogActive, string ResProccessTimeValueforSaveLog, string StoreSupplierResErrorLog)
        {
            System.Diagnostics.Stopwatch timerroomx = new System.Diagnostics.Stopwatch();
            timerroomx.Start();
            List<Options> options = new List<Options>();

            //if (SiteminderResponse != null && SiteminderResponse.Tables.Count > 0 && SiteminderResponse.Tables[0].Rows.Count > 0)
            {
                double ROE = currencyfactor;

                DataTable DtHotel = _commonMethod.GenerateDataTableNewV2();
                DataTable dtHotelAllocation = _commonMethod.GenerateHotelAllocationTableNewV2();
                DataTable dtComplementary = _commonMethod.GenerateComplementaryTableNewV2();
                DataTable dtCommonInfo = _commonMethod.GenerateCommonInfoTableNewV2();
                DataTable dtHotelMinStay = _commonMethod.GenerateHotelMinStayNewV2();
                DataSet dsTemp = new DataSet();

                int CutOffDays = XMLHotelBookingCommon.CutOffDays;
                CutOffDays = _commonMethod.GetCutOffDays(commonSearch.dtSuplierMarkup, "Staah");

                Item.ResponseType = "NoCount";
                double SubAgentMarkup = 0;
                int IsParentId = 1;
                if (dtAgentDetail != null)
                {
                    IsParentId = Convert.ToInt32(dtAgentDetail.Rows[0]["IsParentId"]);
                    SubAgentMarkup = Convert.ToDouble(dtAgentDetail.Rows[0]["B2CMarkupPer"]);
                }

                try
                {
                    if (SiteminderResponse == null || SiteminderResponse.Tables.Count == 0 || SiteminderResponse.Tables[0].Rows.Count == 0)
                    {
                        Item.ResponseStatus = 404;
                        return options;
                    }

                    string Address = "";
                    string LocationDescription = "";
                    string Chain = "";
                    string Rating = "";
                    string CountryId = "";
                    string CityId = "";
                    string GTAImage1 = "";
                    string CityName = "";
                    string internalHotelId = "";
                    string RoomCancellation = "";
                    double LowestRate = 0;
                    string BookingStatus = "";
                    double OriginalLowestRate = 0;
                    string ownsystemhotelId = "";

                    string DiscountPerTtext = "";

                    string Longitude = "";
                    string Latitude = "";
                    int ReviewCount = 0;
                    int ReviewRating = 0;
                    int SortSeq = 9999;
                    DateTime curDate = _commonMethod.GetTimeZoneDate(SearchParam.CurrentDate);



                    double AgentPercentage = 0.0;
                    Boolean blnIsShowSupplier = false;
                    double AgentMarkupAmount = 0.0;
                    Boolean isPerce = true;
                    int MasterSupplierID = 0;
                    int SupplierId = 0;

                    if (dtSuplierMarkup != null)
                    {
                        //dtSuplierMarkup.DefaultView.RowFilter = "SupplierName='Staah'";
                        DataRow[] drSupplierMarkup = dtSuplierMarkup.Select("SupplierName='Staah'");
                        if (drSupplierMarkup.Length > 0) //if (dtSuplierMarkup.DefaultView.Count > 0)
                        {
                            try
                            {
                                AgentPercentage = Convert.ToDouble(drSupplierMarkup[0]["AgentWiseMarkup"]);
                                blnIsShowSupplier = (Convert.ToInt16(drSupplierMarkup[0]["IsShowSupplier"]) == 1 ? true : false);
                                MasterSupplierID = Convert.ToInt32(drSupplierMarkup[0]["MasterSupplierId"]);
                                SupplierId = Convert.ToInt32(drSupplierMarkup[0]["SupplierId"]);
                            }
                            catch (Exception x)//k2
                            {
                                return options;
                            }
                        }
                    }
                    for (int h = 0; h < SiteminderResponse.Tables[0].Rows.Count; h++)
                    {
                        Item.ResponseType = "Success";
                        ownsystemhotelId = Convert.ToString(SiteminderResponse.Tables[0].Rows[h]["raynaid"]);

                        DataTable DtHotelRoom = _commonMethod.GenerateRoomtypeDataTableNewV2();
                        DataTable dtCancellation = _commonMethod.GenerateCancellationDataTableNewV2();
                        DataTable DtHotelRoomRateBreackUp = _commonMethod.GenerateoomRateBreackUpDataTableNewV2();
                        ReviewCount = 0;
                        ReviewRating = 0;
                        int SupplierIdForRate = 0;
                        SupplierIdForRate = SupplierId;// Convert.ToInt32(SiteminderResponse.Tables[0].Rows[h]["SupplierId"]);
                        string Raynaid = Convert.ToString(SiteminderResponse.Tables[0].Rows[h]["raynaid"]);
                        Rating = Convert.ToString(SiteminderResponse.Tables[0].Rows[h]["Rating"]);
                        Int64 giitaCode = Convert.ToInt64(SiteminderResponse.Tables[0].Rows[h]["GiataId"]);

                        //int SupplierId = Convert.ToInt32(GTAResponse.Tables[0].Rows[h]["SupplierId"]);
                        //if (dtSuplierMarkup != null)
                        //{
                        //    if (dtSuplierMarkup.DefaultView.Count > 0)
                        //    {
                        //        MasterSupplierID = Convert.ToInt32(dtSuplierMarkup.DefaultView[0]["MasterSupplierId"]);

                        //    }
                        //}
                        //if (SupplierId > 0)
                        //   MasterSupplierID = SupplierId;
                        string agent = "";
                        if (dtAgentDetail != null && dtAgentDetail.Rows.Count > 0)
                            agent = Convert.ToString(dtAgentDetail.Rows[0]["AgentID"]);
                        _markup.getAgetMarkupSetting("Staah", Convert.ToString(giitaCode), commonSearch.dtSuplierMarkup, commonSearch.dtMarkupDetails, ref AgentPercentage, ref AgentMarkupAmount, ref blnIsShowSupplier, ref isPerce, Convert.ToInt32(agent), Convert.ToInt32(Rating), SearchParam.CityID, SearchParam.CountryID.ToString(), SearchParam.CheckInDate);
                        #region "When not Need Room"  

                        #region "Hotel Code Block "
                        DataRow drXmlHotel = DtHotel.NewRow();

                        string ItemCode = "0";

                        ItemCode = Convert.ToString(SiteminderResponse.Tables[0].Rows[h]["raynaid"]);
                        string HotelName = Convert.ToString(SiteminderResponse.Tables[0].Rows[h]["HotelName"]);
                        string CurrencyCode = Convert.ToString(SiteminderResponse.Tables[0].Rows[h]["CurrencyCode"]);

                        //@New
                        ROE = 1;
                        if (CurrencyCode.ToLower() != "usd")
                        {
                            ROE = _commonMethod.GetROEByCurrency(CurrencyCode, dtROE);
                        }
                        if (ROE == 0)
                            break;
                        //------

                        string OfferCode = "";
                        if (SearchParam.Filters.IsShowRooms == 0)
                        {
                            double price = Convert.ToDouble(SiteminderResponse.Tables[0].Rows[h]["Price"]) * ROE;
                            double b2cItemPriceMarkup = 0;
                            if (AgentPercentage != 0)
                            {
                                b2cItemPriceMarkup = Convert.ToDouble(price) * AgentPercentage / 100;
                                price = price + b2cItemPriceMarkup;
                            }
                            if (AgentMarkupAmount != 0)
                            {
                                b2cItemPriceMarkup = AgentMarkupAmount;
                                price = Math.Round(price + b2cItemPriceMarkup, 4);
                            }

                            drXmlHotel["XMLCountry"] = Convert.ToString(SearchParam.CountryID);
                            drXmlHotel["XMLCity"] = Convert.ToString(SearchParam.CityID);
                            drXmlHotel["XmlHotelCity"] = Convert.ToString(ItemCode);
                            drXmlHotel["XMLHotelCode"] = ItemCode;
                            drXmlHotel["EssentialInformation"] = "";
                            drXmlHotel["Description"] = "";
                            drXmlHotel["SupplierName"] = "Staah";
                            drXmlHotel["OfficeID"] = 0;
                            drXmlHotel["SupplierCurrency"] = CurrencyCode;
                            drXmlHotel["ContractID"] = 0;
                            drXmlHotel["Rating"] = Rating;
                            drXmlHotel["Reviews"] = 0;
                            drXmlHotel["UniqueNo"] = ItemCode;
                            drXmlHotel["IsShowSupplier"] = false;
                            drXmlHotel["BookingStatus"] = "Available";
                            drXmlHotel["AvailToken"] = ItemCode;
                            drXmlHotel["FinalSellingPrice"] = price;
                            drXmlHotel["SellingPrice"] = price;
                            drXmlHotel["GIITACode"] = giitaCode;
                            drXmlHotel["RaynaId"] = ItemCode;
                            drXmlHotel["OwnsystemHotelId"] = ownsystemhotelId;
                            drXmlHotel["SupplierId"] = SupplierId;
                            double FinalMinPrice = price;
                            if (FinalMinPrice > 0)
                            {
                                #region "Room code Block"
                                bool willAddOrNot = true;
                                Int64 RaynaID = 0;
                                //Int64 GiataId = 0;
                                int RatingMain = 0;
                                Int64 OwnsystemHotelId = 0;
                                price = FinalMinPrice;
                                _markup.GetHotelDataCommon(ref price, ref willAddOrNot, ref OwnsystemHotelId, ref RaynaID, ref giitaCode, ref RatingMain, ref drXmlHotel, commonSearch.AgentId, commonSearch.TariffID, commonSearch.dsAgentDetails, commonSearch.supplierGiataMaster, SearchParam, commonSearch.dtCountryCityMarkup, commonSearch.B2CMarkup, commonSearch.IsParentId, commonSearch.SubAgentMarkup, commonSearch.SearchByHotel, commonSearch.HotelIDS);
                                if (willAddOrNot)
                                {
                                    #region "Room Logic"
                                    DataTable dt_HotelRoom = _commonMethod.GenerateRoomtypeDataTableNewV2();
                                    DataTable dt_Cancellation = _commonMethod.GenerateCancellationDataTableNewV2();
                                    DataTable dt_RateBreackUp = _commonMethod.GenerateoomRateBreackUpDataTableNewV2();
                                    #endregion
                                    decimal LowestPrice = Convert.ToDecimal(price).CalculatePrice(commonSearch.tokenData, commonSearch.advancedOptions, CurrencyCode);
                                    Options ops1 = (await _supplierRes.GetCommonRoomForSuppliers(giitaCode, commonSearch.tokenData, commonSearch.advancedOptions, Convert.ToDouble(LowestPrice), drXmlHotel, dt_HotelRoom, dt_Cancellation, dt_RateBreackUp, SearchParam));
                                    if (ops1 != null)
                                        options.Add(ops1);
                                }
                                #endregion
                            }
                        }
                        #endregion

                        #endregion
                        else
                        {
                            ReviewCount = 0;
                            ReviewRating = 0;
                            //double AgentPercentage = 0.0;
                            //Boolean blnIsShowSupplier = false;
                            //double AgentMarkupAmount = 0.0;
                            //Boolean isPerce = true;
                            //if (dtSuplierMarkup != null)
                            //{
                            //    dtSuplierMarkup.DefaultView.RowFilter = "SupplierName='siteminder'";
                            //    if (dtSuplierMarkup.DefaultView.Count > 0)
                            //    {
                            //        AgentPercentage = Convert.ToDouble(dtSuplierMarkup.DefaultView[0]["AgentWiseMarkup"]);
                            //        blnIsShowSupplier = (Convert.ToInt16(dtSuplierMarkup.DefaultView[0]["IsShowSupplier"]) == 1 ? true : false);
                            //    }
                            //}
                            //need to check @viral
                            // double ROE = 1;
                            //DataTable dtROE = Common.GetCurrencyAllInOneROE("All");
                            //DataRow[] drROE = dtROE.Select("currencyshortname='" + SiteminderResponse.Tables[0].Rows[h]["CurrencyCode"] + "'");
                            //if (drROE.Length == 0)
                            //    continue;
                            //ROE = Common.GetROEByCurrency(Convert.ToString(SiteminderResponse.Tables[0].Rows[h]["CurrencyCode"]), dtROE, true);

                            //Common.getAgetMarkupSetting("SiteMinder", giitaCode, dtSuplierMarkup, dtMarkupDetails, ref AgentPercentage, ref AgentMarkupAmount, ref blnIsShowSupplier, ref isPerce, Convert.ToInt32(agent), hotelRating, SearchParam.CityID, SearchParam.CountryId, Convert.ToString(SearchParam.CheckInDate));
                            // int CBranchId = 0;
                            //Common.getAgetMarkupSetting("siteminder", GIATACode, dtSuplierMarkup, dtMarkupDetails, ref AgentPercentage, ref AgentMarkupAmount, ref blnIsShowSupplier, ref isPerce, ref CBranchId, Convert.ToInt32(agent), hotelRating, SearchParam.CityID, SearchParam.CountryId, SearchParam.Nationality);

                            //if (dtMarkupDetails != null && dtMarkupDetails.Rows.Count > 0)
                            //{
                            //    DataRow[] drHotelMarkup = dtMarkupDetails.Select("RaynaID=" + Convert.ToString(SiteminderResponse.Tables[0].Rows[h]["raynaid"]));
                            //    if (drHotelMarkup.GetLength(0) > 0)
                            //    {
                            //        if (Convert.ToBoolean(drHotelMarkup[0]["IsPercentage"]) && Convert.ToDouble(drHotelMarkup[0]["MarkupAmount"]) > 0)
                            //        {
                            //            AgentPercentage = Convert.ToDouble(drHotelMarkup[0]["MarkupAmount"]);
                            //            isPerce = true;
                            //        }
                            //        else if (!Convert.ToBoolean(drHotelMarkup[0]["IsPercentage"]) && Convert.ToDouble(drHotelMarkup[0]["MarkupAmount"]) > 0)
                            //        {
                            //            isPerce = false;
                            //            AgentMarkupAmount = Convert.ToDouble(drHotelMarkup[0]["MarkupAmount"]);
                            //        }
                            //    }
                            //}
                            double AgentCompanyPercentage = Convert.ToDouble(SearchParam.AgentCompanyPercentage);
                            bool IsB2C = Convert.ToBoolean(Convert.ToByte(SearchParam.IsB2BOrB2C));

                            double B2CMarkup = (IsB2C == false ? AgentCompanyPercentage : XMLHotelBookingCommon.B2CMarkup);
                            //if (IsB2C == true)
                            //{
                            //    AgentPercentage = 0;
                            //    AgentMarkupAmount = 0;
                            //}


                            //DataRow drXmlHotel = DtHotel.NewRow();

                            RoomCancellation = "";
                            LowestRate = 0;
                            string uniqueno = Guid.NewGuid().ToString();
                            //string ItemCode = Convert.ToString(SiteminderResponse.Tables[0].Rows[h]["raynaid"]);
                            string Meal = Convert.ToString(SiteminderResponse.Tables[0].Rows[h]["BoardBasis"]);
                            //string HotelName = Convert.ToString(SiteminderResponse.Tables[0].Rows[h]["HotelName"]);
                            //string OfferCode = "";
                            // ReviewCount = (Convert.ToString(SiteminderResponse.Tables[1].Rows[h]["COUNT"]) != string.Empty ? Convert.ToInt32(SiteminderResponse.Tables[1].Rows[h]["COUNT"]) : 0);
                            //ReviewRating = (Convert.ToString(SiteminderResponse.Tables[1].Rows[h]["RATING"]) != string.Empty ? Convert.ToInt32(SiteminderResponse.Tables[1].Rows[h]["RATING"]) : 0);
                            string HotelInformation = "test";//Convert.ToString(SiteminderResponse.Tables[0].Rows[h]["EssentialInformation"]);
                            string HotelDescription = Convert.ToString(SiteminderResponse.Tables[0].Rows[h]["Description"]);
                            internalHotelId = Convert.ToString(SiteminderResponse.Tables[0].Rows[h]["raynaid"]);

                            ownsystemhotelId = Convert.ToString(SiteminderResponse.Tables[0].Rows[h]["raynaid"]);
                            string XMlRateId = Convert.ToString(SiteminderResponse.Tables[0].Rows[h]["XMlRateId"]);

                            //giitaCode = Convert.ToString(SiteminderResponse.Tables[0].Rows[h]["GiataId"]);
                            Address = Convert.ToString(SiteminderResponse.Tables[0].Rows[h]["Address"]);
                            int Favorite = 0;// Convert.ToInt32(Favouritedt.Select("Favorite=" + ownsystemhotelId).GetLength(0) == 0 ? 0 : Convert.ToInt32(Favouritedt.Select("Favorite=" + ownsystemhotelId)[0]["Favorite"]));
                            drXmlHotel["Favorite"] = Favorite;
                            LocationDescription = Convert.ToString(SiteminderResponse.Tables[0].Rows[h]["Location"]);
                            Rating = Convert.ToString(SiteminderResponse.Tables[0].Rows[h]["Rating"]);
                            Chain = Convert.ToString(SiteminderResponse.Tables[0].Rows[h]["GroupName"]);
                            SortSeq = Convert.ToInt32(SiteminderResponse.Tables[0].Rows[h]["SortSequence"]);
                            if (Rating == "" || Rating == "0")
                                Rating = "1";
                            CityName = "Dubai City";
                            //GTAImage1 = (Convert.ToString(WebConfigurationManager.AppSettings["staticimagepath"]) + "/Images/Dubai/" + ItemCode + "/1-ThumbNail.jpg");
                            CountryId = Convert.ToString(SearchParam.CountryID);
                            CityId = Convert.ToString(SearchParam.CityID);


                            Latitude = Convert.ToString(SiteminderResponse.Tables[0].Rows[h]["Latitude"]);
                            Longitude = Convert.ToString(SiteminderResponse.Tables[0].Rows[h]["Longitude"]);

                            DataRow[] drComplementary = SiteminderResponse.Tables[5].Select("RaynaId=" + ItemCode + "");
                            //Need to Add Complementry Logic here
                            if (drComplementary.GetLength(0) > 0)
                            {
                                //for(int cm=0;cm<(drComplementary.GetLength(0) > 5?5: drComplementary.GetLength(0));cm++)
                                for (int cm = 0; cm < drComplementary.GetLength(0); cm++)
                                {
                                    DataRow dtr = dtComplementary.NewRow();
                                    dtr["OwnsystemHotelId"] = Convert.ToString(drComplementary[cm]["RaynaId"]);
                                    dtr["ComplementaryName"] = Convert.ToString(drComplementary[cm]["ComplementaryName"]);
                                    dtr["ComplementaryText"] = Convert.ToString(drComplementary[cm]["ComplementaryText"]);
                                    dtr["ImagePath"] = Convert.ToString(drComplementary[cm]["ImagePath"]);
                                    dtComplementary.Rows.Add(dtr);
                                }
                            }

                            //DataRow[] drRoomCategories = SiteminderResponse.Tables[3].Select("raynaid=" + internalHotelId + " and checkindate='" + SearchParam.CheckInDate.Substring(3, 2) + "/" + SearchParam.CheckInDate.Substring(0, 2) + "/" + SearchParam.CheckInDate.Substring(6, 4) + "'");
                            DataRow[] drRoomCategories = SiteminderResponse.Tables[3].Select("raynaid=" + internalHotelId + " and XMlRateId='" + XMlRateId + "' and checkindate='" + SearchParam.CheckInDate.Substring(3, 2) + "/" + SearchParam.CheckInDate.Substring(0, 2) + "/" + SearchParam.CheckInDate.Substring(6, 4) + "'"); //Added on 22-Feb-2025
                            int PreviousRoomNo = 1;
                            int LogicalCounter = 0;
                            for (int rc = 0; rc <= drRoomCategories.GetLength(0) - 1; rc++)
                            {
                                DataRow drhotelRoom = DtHotelRoom.NewRow();
                                if (Convert.ToInt32(drRoomCategories[rc]["RoomNo"]) != PreviousRoomNo)
                                {
                                    LogicalCounter = 0;
                                }
                                PreviousRoomNo = Convert.ToInt32(drRoomCategories[rc]["RoomNo"]);
                                LogicalCounter++;

                                string RoomDesc = Convert.ToString(drRoomCategories[rc]["RaynaRoomTypeName"]);
                                string RoomTypeID = Convert.ToString(drRoomCategories[rc]["RoomTypeID"]);
                                string BoardTypeID = Convert.ToString(drRoomCategories[rc]["BoardTypeID"]);
                                string RatePlanCode = Convert.ToString(drRoomCategories[rc]["RatePlanCode"]);
                                string TotalAmtforroom = Convert.ToString(drRoomCategories[rc]["TotalAmtforroom"]);
                                CurrencyCode = Convert.ToString(drRoomCategories[rc]["CurrencyCode"]);
                                double ComissionRate = Convert.ToDouble(drRoomCategories[rc]["ComissionRate"]);

                                string RaynaRoomTypeNameOriginal = Convert.ToString(drRoomCategories[rc]["RaynaRoomTypeNameOriginal"]);
                                string checkin = SearchParam.CheckInDate; //doc.SelectSingleNode("//StayDateRange/@Start").InnerText;
                                                                          //@newk
                                                                          // string RoomTypeID_New = Convert.ToString(drRoomCategories[rc]["RoomTypeId"]) + "$$$" + RatePlanCode + "$$$" + CurrencyCode + "$$$" + ItemSurchargePrice + "$$$" + ExtrabedQuantity + "$$$" + CountryId + "$$$" + RaynaRoomTypeNameOriginal;
                                                                          //string RoomTypeID_New = Convert.ToString(drRoomCategories[rc]["RoomTypeId"]) + "$$$" + RatePlanCode + "$$$" + CurrencyCode + "$$$" +  CountryId;
                                                                          //@newk
                                                                          //DataRow[] DTdSurchargerdate = SiteminderResponse.Tables[12].Select("RoomTypeID='" + RoomTypeID + "' and RaynaId=" + ItemCode + " and BoardTypeID='" + BoardTypeID + "' and RoomNo=" + Convert.ToDouble(drRoomCategories[rc]["RoomNo"]));
                                DataRow[] DTdSurchargerdate = SiteminderResponse.Tables[12].Select("RoomTypeID='" + RoomTypeID + "' and XMlRateId=" + XMlRateId + " and RaynaId=" + ItemCode + " and BoardTypeID='" + BoardTypeID + "' and RoomNo=" + Convert.ToDouble(drRoomCategories[rc]["RoomNo"])); //Added 22-feb-2025

                                double ItemSurchargePrice = 0;
                                double ItemSurchargePrice_new = 0;
                                int ExtrabedQuantity = 0;
                                string ExtraPerson = string.Empty;
                                if (DTdSurchargerdate.Length > 0)
                                {
                                    for (int su = 0; su < DTdSurchargerdate.GetLength(0); su++)
                                    {
                                        ExtraPerson = Convert.ToString(DTdSurchargerdate[su]["SurchargeName"]);
                                        ItemSurchargePrice += Math.Round(Convert.ToDouble(DTdSurchargerdate[su]["Price"]), 2) * ROE;
                                        ItemSurchargePrice_new += Math.Round(Convert.ToDouble(DTdSurchargerdate[su]["Price"]), 2);
                                        ExtrabedQuantity++;
                                    }
                                }

                                CountryId = Convert.ToString(SearchParam.CountryID);
                                OfferCode = "";//Convert.ToString(drRoomCategories[rc]["OfferCode"]);
                                string RoomId = Convert.ToString(drRoomCategories[rc]["RaynaRoomTypeName"]) + "-" + Convert.ToString(drRoomCategories[rc]["BoardBasis"]);
                                string MealId = Convert.ToString(drRoomCategories[rc]["BoardBasis"]);
                                drhotelRoom["OwnsystemHotelId"] = ownsystemhotelId;
                                drhotelRoom["ContractID"] = 0;
                                //drhotelRoom["ReviewCount"] = ReviewCount;
                                //drhotelRoom["ReviewRating"] = ReviewRating;
                                drhotelRoom["UniqueNo"] = uniqueno;
                                drhotelRoom["BookingStatus"] = Convert.ToString(drRoomCategories[rc]["BookingStatus"]);
                                drhotelRoom["RoomNo"] = Convert.ToString(drRoomCategories[rc]["RoomNo"]);
                                drhotelRoom["RoomTypeName"] = RoomDesc;
                                drhotelRoom["OfferCode"] = OfferCode;
                                //drhotelRoom["MealName"] = Common.getMealFullName(Convert.ToString(drRoomCategories[rc]["BoardBasis"]));
                                drhotelRoom["MealName"] = Convert.ToString(drRoomCategories[rc]["BoardBasis"]);
                                //drhotelRoom["RoomTypeId"] = RoomDesc;
                                // drhotelRoom["RoomTypeId"] = Convert.ToString(drRoomCategories[rc]["RoomTypeId"]) + "$$$" + RatePlanCode + "$$$" + CurrencyCode + "$$$" + ItemSurchargePrice + "$$$" + ExtrabedQuantity + "$$$" + CountryId;

                                drhotelRoom["MealId"] = Convert.ToString(drRoomCategories[rc]["BoardBasis"]);
                                //By manish
                                drhotelRoom["OfferDescription"] = "";//Convert.ToString(drRoomCategories[rc]["OfferDescription"]);
                                drhotelRoom["SupplierCurrency"] = CurrencyCode;
                                drhotelRoom["SHRUI"] = "";
                                if (ExtraPerson != string.Empty)
                                    drhotelRoom["EssentialInformation"] = ExtraPerson;
                                if (Convert.ToString(drRoomCategories[rc]["BoardBasis"]) == "")
                                {
                                    drRoomCategories[rc]["BoardBasis"] = "RO";
                                }
                                drXmlHotel["MealName"] = Convert.ToString(drRoomCategories[rc]["BoardBasis"]);

                                //DataRow[] DTdrdate = SiteminderResponse.Tables[3].Select("RoomTypeID='" + RoomTypeID + "' and RaynaId=" + ItemCode + " and RoomNo=" + Convert.ToDouble(drRoomCategories[rc]["RoomNo"]) + " and BoardTypeID='" + BoardTypeID + "'");
                                DataRow[] DTdrdate = SiteminderResponse.Tables[3].Select("RoomTypeID='" + RoomTypeID + "' and XMlRateId=" + XMlRateId + " and RaynaId=" + ItemCode + " and RoomNo=" + Convert.ToDouble(drRoomCategories[rc]["RoomNo"]) + " and BoardTypeID='" + BoardTypeID + "'"); //Added 22-feb-2025

                                double ItemOriginalPrice1 = 0;
                                for (int o = 0; o < DTdrdate.GetLength(0); o++)
                                {
                                    ItemOriginalPrice1 = ItemOriginalPrice1 + Math.Round(Convert.ToDouble(DTdrdate[o]["OriginalPrice"]), 2) * ROE;
                                }
                                ItemOriginalPrice1 = ItemOriginalPrice1 + ItemSurchargePrice;
                                //New Thing ADD k2
                                double ItemBasePrice1 = 0;
                                string RateBreakPrice = "";
                                for (int ip = 0; ip < DTdrdate.GetLength(0); ip++)
                                {
                                    ItemBasePrice1 = Math.Round(Convert.ToDouble(DTdrdate[ip]["Price"]), 2);
                                    if (ip == 0)
                                    {
                                        RateBreakPrice = Convert.ToString(ItemBasePrice1);
                                    }
                                    else
                                    {
                                        RateBreakPrice = RateBreakPrice + "#" + ItemBasePrice1;
                                    }
                                }
                                //string RoomTypeIdNew = Convert.ToString(drRoomCategories[rc]["RoomTypeID"]) + "$$$" + RatePlanCode + "$$$" + CurrencyCode + "$$$" + ItemSurchargePrice + "$$$" + ExtrabedQuantity + "$$$" + CountryId + "$$$" + RaynaRoomTypeNameOriginal + "$$$" + RateBreakPrice + "$$$" + BoardTypeID; //Added 22-feb-2025
                                //string RoomTypeIdNew = Convert.ToString(drRoomCategories[rc]["RoomTypeId"]) + "$$$" + RatePlanCode + "$$$" + CurrencyCode + "$$$" + ItemSurchargePrice_new + "$$$" + ExtrabedQuantity + "$$$" + CountryId + "$$$" + RoomDesc + "$$$" + BoardTypeID + "$$$" + MealId + "$$$" + RoomDesc + "$$$" + XMlRateId + "$$$" + ComissionRate + "$$$" + TotalAmtforroom; 
                                string RoomTypeIdNew = Convert.ToString(drRoomCategories[rc]["RoomTypeId"]) + "$$$" + RatePlanCode + "$$$" + CurrencyCode + "$$$" + ItemSurchargePrice_new + "$$$" + ExtrabedQuantity + "$$$" + CountryId + "$$$" + RoomDesc + "$$$" + BoardTypeID + "$$$" + MealId + "$$$" + RoomDesc + "$$$" + XMlRateId + "$$$" + ComissionRate + "$$$" + TotalAmtforroom + "$$$" + RateBreakPrice + "$$$" + ItemOriginalPrice1; //Added on 17-July-2025
                                drhotelRoom["RoomTypeId"] = RoomTypeIdNew;// Convert.ToString(drRoomCategories[rc]["RoomTypeID"]) + "$$$" + RatePlanCode + "$$$" + CurrencyCode + "$$$" + ItemSurchargePrice + "$$$" + ExtrabedQuantity + "$$$" + CountryId + "$$$" + RaynaRoomTypeNameOriginal;


                                int ActualNoOfNight = Convert.ToInt32(DTdrdate.GetLength(0));
                                int ActualSurchargeNoOfNight = Convert.ToInt32(DTdSurchargerdate.GetLength(0));
                                int noofadults = 0;
                                int noofchilds = 0;
                                if (ActualNoOfNight != SearchParam.NoofNights)
                                    continue;
                                for (int x = 0; x < SearchParam.Rooms.Count; x++)
                                {
                                    int ADT = SearchParam.Rooms[x].NoofAdults;
                                    int CHD = SearchParam.Rooms[x].NoofChild;
                                    noofadults = ADT + noofadults;
                                    noofchilds = CHD + noofchilds;
                                }

                                drhotelRoom["AdultsOccupancy"] = noofadults;
                                drhotelRoom["ChildOccupancy"] = noofchilds;
                                //drhotelRoom["AdultsOccupancy"] = Convert.ToString(DTdrdate[0]["RoomCapacity"]);
                                //drhotelRoom["ChildOccupancy"] = Convert.ToString(DTdrdate[0]["NoOfChild"]);
                                drhotelRoom["MaxExtrabed"] = Convert.ToString(DTdrdate[0]["NoOfExtraBeds"]);
                                drhotelRoom["BoardTypeID"] = BoardTypeID;

                                //DataRow[] drCancellationPolicy = SiteminderResponse.Tables[8].Select
                                //     ("raynaid=" + ItemCode + " and (RoomTypeID='" + RoomTypeID + "' or RoomTypeID='0') and (BoardTypeID='" + BoardTypeID + "' or (BoardTypeID='0' and " + BoardTypeID + ">0))");
                                //           DataRow[] drCancellationPolicy = null;
                                //           if (Convert.ToInt32(BoardTypeID) > 0)
                                //           {
                                //               drCancellationPolicy = SiteminderResponse.Tables[8].Select
                                //("raynaid=" + ItemCode + " and (RoomTypeID='" + RoomTypeID + "') and (BoardTypeID='" + BoardTypeID + "')");


                                //               if (drCancellationPolicy.GetLength(0) == 0)
                                //               {
                                //                   drCancellationPolicy = SiteminderResponse.Tables[8].Select
                                //                         ("raynaid=" + ItemCode + " and (RoomTypeID='0') and (BoardTypeID='0')");
                                //               }
                                //           }
                                //           else
                                //           {
                                //               drCancellationPolicy = SiteminderResponse.Tables[8].Select("BoardTypeID='11111110'");
                                //           }

                                DataRow[] drCancellationPolicy = null;
                                if (Convert.ToInt16(BoardTypeID) > 0)
                                {
                                    drCancellationPolicy = SiteminderResponse.Tables[8].Select
                                ("raynaid=" + ItemCode + " and (RoomTypeID='" + RoomTypeID + "') and (BoardTypeID='" + BoardTypeID + "') and(XMlRateId = '" + XMlRateId + "')");


                                    if (drCancellationPolicy.GetLength(0) == 0)
                                    {
                                        drCancellationPolicy = SiteminderResponse.Tables[8].Select
                                              ("raynaid=" + ItemCode + " and (RoomTypeID='0') and (BoardTypeID='0') and(XMlRateId = '" + XMlRateId + "')");
                                    }
                                }
                                else
                                {
                                    drCancellationPolicy = SiteminderResponse.Tables[8].Select("BoardTypeID='11111110'");
                                }
                                //Above Added 22-feb-2025


                                //if (drCancellationPolicy.GetLength(0) == 0)
                                //{
                                //    //drCancellationPolicy = SiteminderResponse.Tables[9].Select
                                //    //("raynaid=" + ItemCode + " and RaynaRoomTypeName=''");
                                //    drCancellationPolicy = SiteminderResponse.Tables[8].Select
                                //    ("raynaid=" + ItemCode + " and (RoomTypeID='0') and (BoardTypeID='0')");
                                //}
                                int intRoomCancellation = 0;

                                #region
                                if (drCancellationPolicy.GetLength(0) == 0)
                                {
                                    //Default non-refundable cancellation policy
                                    double ItemBasePrice = 0;
                                    for (int ip = 0; ip < DTdrdate.GetLength(0); ip++)
                                    {
                                        ItemBasePrice = ItemBasePrice + Math.Round(Convert.ToDouble(DTdrdate[ip]["Price"]), 2) * ROE;
                                    }
                                    //double ItemBasePrice = Math.Round(Convert.ToDouble(drRoomCategories[rc]["Price"]), 2);

                                    double ItemSurchargePriceBasePrice = ItemSurchargePrice;//Math.Round(Convert.ToDouble(DTdSurchargerdate[rc]["Price"]), 2);

                                    //if (ActualSurchargeNoOfNight != 0)
                                    //{
                                    //    ItemSurchargePriceBasePrice = (ItemSurchargePriceBasePrice / ActualSurchargeNoOfNight);
                                    //}
                                    double CacelationAmountData = ItemBasePrice;
                                    double ChargeAmount = CacelationAmountData;

                                    double ChargeSurchargeAmount = ItemSurchargePriceBasePrice;
                                    double AgentChargeAmount = 0;
                                    //Double CancellationMarkup = Math.Round(ChargeAmount, 2);
                                    Double CancellationMarkup = 0.0;
                                    if (isPerce)
                                    {
                                        if (IsB2C == false && (B2CMarkup == 0 || (AgentPercentage > 0)))
                                        {
                                            CancellationMarkup = Math.Round(((ChargeAmount * AgentPercentage) / 100), 2);
                                        }
                                    }
                                    else
                                        CancellationMarkup = (ChargeAmount == 0 ? ChargeAmount : AgentMarkupAmount);


                                    ChargeAmount = ChargeAmount + CancellationMarkup;


                                    if (IsParentId == 0)
                                    {
                                        Double SubCancellationAgentMarkup = Math.Round((Math.Round((ChargeAmount * SubAgentMarkup), 2) / 100), 2);
                                        ChargeAmount = Math.Round((ChargeAmount + SubCancellationAgentMarkup), 2);
                                    }
                                    Double CancellationAgentMarkup = 0;
                                    if (IsB2C == true)
                                    {
                                        CancellationAgentMarkup = Math.Round((Math.Round((ChargeAmount * AgentPercentage), 2) / 100), 2);
                                        ChargeAmount = Math.Round(ChargeAmount + CancellationAgentMarkup, 2);
                                    }

                                    ChargeAmount = Math.Round((ChargeAmount + CancellationAgentMarkup), 2);
                                    ChargeAmount = ChargeAmount + ChargeSurchargeAmount;

                                    DataRow drHotelCancellation = dtCancellation.NewRow();
                                    drHotelCancellation["SupplierName"] = "Staah";
                                    drHotelCancellation["OwnsystemHotelId"] = ownsystemhotelId;
                                    drHotelCancellation["RoomNo"] = Convert.ToString(drRoomCategories[rc]["RoomNo"]);
                                    //drHotelCancellation["RoomTypeId"] = Convert.ToString(drRoomCategories[rc]["RaynaRoomTypeName"]);
                                    // drHotelCancellation["RoomTypeId"] = Convert.ToString(drRoomCategories[rc]["RoomTypeID"]) + "$$$" + RatePlanCode + "$$$" + CurrencyCode + "$$$" + ItemSurchargePrice + "$$$" + ExtrabedQuantity + "$$$" + CountryId;
                                    drHotelCancellation["RoomTypeId"] = RoomTypeIdNew;// Convert.ToString(drRoomCategories[rc]["RoomTypeID"]) + "$$$" + RatePlanCode + "$$$" + CurrencyCode + "$$$" + ItemSurchargePrice + "$$$" + ExtrabedQuantity + "$$$" + CountryId + "$$$" + RaynaRoomTypeNameOriginal + "$$$" + RateBreakPrice;
                                    drHotelCancellation["RoomTypeName"] = RoomDesc;
                                    drHotelCancellation["MealId"] = Convert.ToString(drRoomCategories[rc]["BoardBasis"]);
                                    // drHotelCancellation["FromDate"] = FromDate;
                                    // drHotelCancellation["ToDate"] = ToDate;
                                    drHotelCancellation["FromDate"] = DateTime.Now.Date;
                                    drHotelCancellation["ToDate"] = DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null);
                                    drHotelCancellation["CompanyCancellationPrice"] = ChargeAmount;
                                    drHotelCancellation["AgentCancellationPrice"] = ChargeAmount;
                                    drHotelCancellation["BoardTypeID"] = BoardTypeID;
                                    // drHotelCancellation["Percentage"] = 100;
                                    drHotelCancellation["SupplierCurrency"] = CurrencyCode;
                                    drHotelCancellation["WithInCancellationDeadLine"] = "Yes";
                                    drHotelCancellation["BuyingPrice"] = ChargeAmount;
                                    dtCancellation.Rows.Add(drHotelCancellation);
                                }
                                else
                                {
                                    for (int c = drCancellationPolicy.GetLength(0) - 1; c >= 0; c--)
                                    {
                                        //double ItemBasePrice = Math.Round(Convert.ToDouble(drRoomCategories[rc]["Price"]), 2);
                                        double ItemBasePrice = 0;
                                        for (int ip = 0; ip < DTdrdate.GetLength(0); ip++)
                                        {
                                            ItemBasePrice = ItemBasePrice + Math.Round(Convert.ToDouble(DTdrdate[ip]["Price"]), 2) * ROE;
                                        }
                                        double ItemSurchargePriceBasePrice = ItemSurchargePrice;//Math.Round(Convert.ToDouble(drRoomCategories[rc]["SurchargePrice"]), 2);
                                        DateTime FromDate = Convert.ToDateTime(drCancellationPolicy[c]["FromDate"]);
                                        //DateTime ToDate = Convert.ToDateTime(drCancellationPolicy[c]["ToDate"]);
                                        DateTime ToDate = (Convert.ToString(drCancellationPolicy[c]["ToDate"]) != string.Empty ? Convert.ToDateTime(drCancellationPolicy[c]["ToDate"]) : DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null));

                                        int cancellationNights = Convert.ToInt32(drCancellationPolicy[c]["cancellationNights"]);
                                        int CancellationPercentage = Convert.ToInt32(drCancellationPolicy[c]["CancellationPercentage"]);
                                        if (cancellationNights > ActualNoOfNight)
                                            cancellationNights = ActualNoOfNight;
                                        //if (ActualNoOfNight == 1)
                                        //    cancellationNights = 1;
                                        if (ActualSurchargeNoOfNight != 0)
                                        {
                                            ItemSurchargePriceBasePrice = (ItemSurchargePriceBasePrice / ActualSurchargeNoOfNight);
                                        }
                                        double CacelationAmountData = ItemBasePrice;
                                        if (cancellationNights != 0)
                                        {
                                            CacelationAmountData = (CacelationAmountData / ActualNoOfNight) * cancellationNights;
                                        }
                                        double ChargeAmount = (CacelationAmountData * CancellationPercentage) / 100;
                                        double ChargeSurchargeAmount = (ItemSurchargePriceBasePrice * CancellationPercentage) / 100;
                                        double AgentChargeAmount = 0;
                                        //Double CancellationMarkup = Math.Round(ChargeAmount, 2);
                                        Double CancellationMarkup = 0.0;
                                        double MAinAgentCharge = ChargeAmount;
                                        double CompanyChargeAmount = ChargeAmount;
                                        double Vatpolicy = 0;
                                        ChargeAmount = _markup.ProcessHotelCancelationPrice_WithMarkup(ref MAinAgentCharge, SearchParam.NoofNights, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), ChargeAmount, isPerce, B2CMarkup, AgentPercentage, AgentMarkupAmount, IsParentId, SubAgentMarkup, ref Vatpolicy);


                                        if (c == 0 && c == drCancellationPolicy.GetLength(0) - 1)
                                        {
                                        }
                                        else
                                        {
                                            FromDate = FromDate.AddDays(CutOffDays);
                                        }
                                        ChargeAmount = ChargeAmount + ChargeSurchargeAmount;
                                        MAinAgentCharge = MAinAgentCharge + ChargeSurchargeAmount;
                                        CompanyChargeAmount = CompanyChargeAmount + ChargeSurchargeAmount;
                                        //if (drCancellationPolicy.GetLength(0) >= 3 && c == 0)
                                        //{
                                        //    FromDate = (Convert.ToString(drCancellationPolicy[c]["ToDate"]) != string.Empty ? Convert.ToDateTime(drCancellationPolicy[c]["ToDate"]) : DateTime.ParseExact(checkin, "dd/MM/yyyy", null));// Convert.ToDateTime(drCancellationPolicy[c]["ToDate"]);
                                        //}
                                        if (c != 0)
                                            ToDate = ToDate.AddDays(CutOffDays);
                                        else
                                            ToDate = DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null);

                                        if (FromDate >= DateTime.Now.Date || ToDate >= DateTime.Now.Date)
                                        {
                                            if (FromDate < DateTime.Now.Date)
                                            {
                                                FromDate = DateTime.Now.Date;
                                            }
                                            intRoomCancellation++;
                                            RoomCancellation += Convert.ToString(FromDate) + "  To " + Convert.ToString(ToDate) + " Charges : " + Convert.ToString(ChargeAmount) + "<br/>";
                                            DataRow drHotelCancellation = dtCancellation.NewRow();
                                            drHotelCancellation["SupplierName"] = "Staah";
                                            drHotelCancellation["OwnsystemHotelId"] = ownsystemhotelId;
                                            drHotelCancellation["RoomNo"] = Convert.ToString(drRoomCategories[rc]["RoomNo"]);
                                            //drHotelCancellation["RoomTypeId"] = Convert.ToString(drRoomCategories[rc]["RaynaRoomTypeName"]);
                                            // drHotelCancellation["RoomTypeId"] = Convert.ToString(drRoomCategories[rc]["RoomTypeID"]) + "$$$" + RatePlanCode + "$$$" + CurrencyCode + "$$$" + ItemSurchargePrice + "$$$" + ExtrabedQuantity + "$$$" + CountryId;
                                            drHotelCancellation["RoomTypeId"] = RoomTypeIdNew;// Convert.ToString(drRoomCategories[rc]["RoomTypeID"]) + "$$$" + RatePlanCode + "$$$" + CurrencyCode + "$$$" + ItemSurchargePrice + "$$$" + ExtrabedQuantity + "$$$" + CountryId + "$$$" + RaynaRoomTypeNameOriginal + "$$$" + RateBreakPrice;
                                            drHotelCancellation["RoomTypeName"] = RoomDesc;
                                            drHotelCancellation["MealId"] = Convert.ToString(drRoomCategories[rc]["BoardBasis"]);
                                            drHotelCancellation["FromDate"] = FromDate;
                                            drHotelCancellation["ToDate"] = ToDate;
                                            drHotelCancellation["CompanyCancellationPrice"] = ChargeAmount;
                                            drHotelCancellation["AgentCancellationPrice"] = ChargeAmount;
                                            drHotelCancellation["BoardTypeID"] = BoardTypeID;
                                            // drHotelCancellation["Percentage"] = Convert.ToDecimal(CancellationPercentage);
                                            drHotelCancellation["SupplierCurrency"] = CurrencyCode;
                                            drHotelCancellation["WithInCancellationDeadLine"] = "No";
                                            drHotelCancellation["BuyingPrice"] = ChargeAmount;
                                            dtCancellation.Rows.Add(drHotelCancellation);
                                        }
                                    }
                                }
                                #endregion

                                string RoomCategory_Id = Convert.ToString(RoomId);
                                string ItemPrice_Text = "";
                                double totSurchar = 0.0;
                                try
                                {
                                    int NightSplitCounter = 0;
                                    int surchageNightSplitCounter = 0;
                                    string WeekColumn = "";
                                    int cntno = 1;
                                    for (int dtdate = 0; dtdate <= DTdrdate.GetLength(0) - 1; dtdate++)
                                    {
                                        string freeStay = "false";
                                        string datetime = Convert.ToString(DTdrdate[dtdate]["checkindate"]);

                                        if (dtdate == 0)
                                        {
                                            if (Convert.ToInt32(DTdrdate.GetLength(0)) > ActualNoOfNight)
                                            {
                                                ActualNoOfNight = DTdrdate.GetLength(0);
                                            }
                                            for (int ratetemplate = 0; ratetemplate <= (ActualNoOfNight > 7 ? 7 : ActualNoOfNight) - 1; ratetemplate++)
                                            {
                                                WeekColumn = Convert.ToString(Convert.ToDateTime(DTdrdate[dtdate]["checkindate"]).Date.AddDays(ratetemplate).DayOfWeek).Substring(0, 3);
                                            }
                                        }

                                        double DateGross = 0;
                                        freeStay = "no";
                                        if (freeStay == "no")
                                        {
                                            DateGross = Math.Round(Convert.ToDouble(DTdrdate[dtdate]["PriceWithoutExtraBed"]), 2) * ROE;
                                            double b2cDateGrossMarkup = Math.Round((Math.Round((DateGross * B2CMarkup), 2) / 100), 2);
                                            DateGross = Math.Round(DateGross + b2cDateGrossMarkup, 2);
                                            if (IsB2C == false)
                                            {
                                                if (IsParentId == 0)
                                                {
                                                    double b2bSubDateGrossMarkup = Math.Round((Math.Round((DateGross * SubAgentMarkup), 2) / 100), 2);
                                                    DateGross = Math.Round(DateGross + b2bSubDateGrossMarkup, 2);
                                                }
                                                if (isPerce)
                                                {
                                                    double b2bDateGrossMarkup = Math.Round((Math.Round((DateGross * AgentPercentage), 2) / 100), 2);
                                                    DateGross = Math.Round(DateGross + b2bDateGrossMarkup, 2);
                                                }
                                                else
                                                    DateGross = (DateGross == 0 ? DateGross : Math.Round(DateGross + AgentMarkupAmount, 2));
                                            }
                                        }

                                        WeekColumn = Convert.ToString(Convert.ToDateTime(datetime).DayOfWeek).Substring(0, 3);
                                        DataRow drHotelRoomRateBreackUp = DtHotelRoomRateBreackUp.NewRow();
                                        drHotelRoomRateBreackUp["RoomNo"] = Convert.ToString(drRoomCategories[rc]["RoomNo"]);
                                        drHotelRoomRateBreackUp["SupplierName"] = "Staah";
                                        drHotelRoomRateBreackUp["OwnsystemHotelId"] = ownsystemhotelId;
                                        drHotelRoomRateBreackUp["ContractID"] = 0;
                                        drHotelRoomRateBreackUp["RoomTypeName"] = Convert.ToString(DTdrdate[dtdate]["RaynaRoomTypeName"]);
                                        //drHotelRoomRateBreackUp["RoomTypeId"] = Convert.ToString(DTdrdate[dtdate]["RaynaRoomTypeName"]);
                                        drHotelRoomRateBreackUp["RoomTypeId"] = RoomTypeIdNew;// Convert.ToString(DTdrdate[dtdate]["RoomTypeID"]) + "$$$" + RatePlanCode + "$$$" + CurrencyCode + "$$$" + ItemSurchargePrice + "$$$" + ExtrabedQuantity + "$$$" + CountryId + "$$$" + RateBreakPrice;
                                        drHotelRoomRateBreackUp["MealId"] = Convert.ToString(drRoomCategories[rc]["BoardBasis"]);
                                        drHotelRoomRateBreackUp["Meal"] = Convert.ToString(drRoomCategories[rc]["BoardBasis"]);
                                        drHotelRoomRateBreackUp["BookingStatus"] = Convert.ToString(DTdrdate[dtdate]["BookingStatus"]);
                                        drHotelRoomRateBreackUp["DateOfWeek"] = WeekColumn;
                                        drHotelRoomRateBreackUp["Date"] = datetime;
                                        drHotelRoomRateBreackUp["Rate"] = DateGross;
                                        drHotelRoomRateBreackUp["RateType"] = "A Rate";
                                        drHotelRoomRateBreackUp["OrderNo"] = cntno;
                                        drHotelRoomRateBreackUp["BoardTypeID"] = BoardTypeID;
                                        drHotelRoomRateBreackUp["SupplierCurrency"] = CurrencyCode;
                                        drHotelRoomRateBreackUp["CompanyRate"] = Math.Round(Convert.ToDecimal(DTdrdate[dtdate]["PriceWithoutExtraBed"]), 6);
                                        drHotelRoomRateBreackUp["BuyingPrice"] = DateGross;
                                        drHotelRoomRateBreackUp["Total"] = Math.Round(Convert.ToDecimal(DTdrdate[dtdate]["Price"]), 6); ;
                                        DtHotelRoomRateBreackUp.Rows.Add(drHotelRoomRateBreackUp);
                                        cntno++;
                                        NightSplitCounter++;
                                    }

                                    NightSplitCounter = 0;
                                    surchageNightSplitCounter = 0;

                                    for (int dtdate = 0; dtdate <= DTdSurchargerdate.GetLength(0) - 1; dtdate++)
                                    {
                                        string freeStay = "false";
                                        string datetime = Convert.ToString(DTdSurchargerdate[dtdate]["checkindate"]);

                                        double DateGross = 0;
                                        freeStay = "no";
                                        if (freeStay == "no")
                                            DateGross = Math.Round(Convert.ToDouble(DTdSurchargerdate[dtdate]["Price"]), 2) * ROE;
                                        if (dtdate == 0)
                                        {
                                            TimeSpan ts = Convert.ToDateTime(DTdSurchargerdate[dtdate]["checkindate"]) - Convert.ToDateTime(DTdrdate[dtdate]["checkindate"]);
                                            int numberOfDays = ts.Days;
                                            for (int daycount = 0; daycount < numberOfDays; daycount++)
                                                surchageNightSplitCounter++;
                                        }

                                        WeekColumn = Convert.ToString(Convert.ToDateTime(datetime).DayOfWeek).Substring(0, 3);
                                        DataRow drHotelRoomRateBreackUp = DtHotelRoomRateBreackUp.NewRow();
                                        drHotelRoomRateBreackUp["RoomNo"] = Convert.ToString(drRoomCategories[rc]["RoomNo"]);
                                        drHotelRoomRateBreackUp["SupplierName"] = "Staah";
                                        drHotelRoomRateBreackUp["OwnsystemHotelId"] = ownsystemhotelId;
                                        drHotelRoomRateBreackUp["ContractID"] = 0;
                                        drHotelRoomRateBreackUp["RoomTypeName"] = Convert.ToString(DTdrdate[0]["RaynaRoomTypeName"]);
                                        //drHotelRoomRateBreackUp["RoomTypeId"] = Convert.ToString(DTdrdate[0]["RaynaRoomTypeName"]);
                                        drHotelRoomRateBreackUp["RoomTypeId"] = RoomTypeIdNew;// Convert.ToString(DTdrdate[0]["RoomTypeID"]) + "$$$" + RatePlanCode + "$$$" + CurrencyCode + "$$$" + ItemSurchargePrice + "$$$" + ExtrabedQuantity + "$$$" + CountryId;
                                        drHotelRoomRateBreackUp["MealId"] = Convert.ToString(drRoomCategories[rc]["BoardBasis"]);
                                        drHotelRoomRateBreackUp["Meal"] = Convert.ToString(drRoomCategories[rc]["BoardBasis"]);
                                        drHotelRoomRateBreackUp["BookingStatus"] = Convert.ToString(drRoomCategories[rc]["BookingStatus"]);
                                        drHotelRoomRateBreackUp["DateOfWeek"] = WeekColumn;
                                        drHotelRoomRateBreackUp["Date"] = datetime;
                                        drHotelRoomRateBreackUp["Rate"] = DateGross;
                                        drHotelRoomRateBreackUp["RateType"] = Convert.ToString(DTdSurchargerdate[dtdate]["SurchargeName"]);
                                        drHotelRoomRateBreackUp["OrderNo"] = cntno;
                                        drHotelRoomRateBreackUp["BoardTypeID"] = BoardTypeID;
                                        drHotelRoomRateBreackUp["SupplierCurrency"] = CurrencyCode;
                                        drHotelRoomRateBreackUp["CompanyRate"] = Math.Round(Convert.ToDouble(DTdSurchargerdate[dtdate]["Price"]), 6);
                                        drHotelRoomRateBreackUp["BuyingPrice"] = DateGross;
                                        drHotelRoomRateBreackUp["Total"] = Math.Round(Convert.ToDouble(DTdSurchargerdate[dtdate]["Price"]), 6); ;
                                        DtHotelRoomRateBreackUp.Rows.Add(drHotelRoomRateBreackUp);
                                        totSurchar = totSurchar + DateGross;
                                        surchageNightSplitCounter++;
                                        cntno++;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    await _exception.Error(ex);
                                }
                                //new logic for rate breack up  end

                                //double ItemPrice = Math.Round(Convert.ToDouble(drRoomCategories[rc]["Price"]), 2);
                                double ItemPrice = 0;
                                for (int ip = 0; ip < DTdrdate.GetLength(0); ip++)
                                {
                                    ItemPrice = ItemPrice + Math.Round(Convert.ToDouble(DTdrdate[ip]["Price"]), 2) * ROE;
                                }
                                double ItemOriginalPrice = Math.Round(Convert.ToDouble(drRoomCategories[rc]["OriginalPrice"]), 2) * ROE;


                                drhotelRoom["CompanyBuyingPrice"] = ItemPrice + ItemSurchargePrice;

                                if (rc == 0)
                                {
                                    LowestRate = Math.Round(Convert.ToDouble(DTdrdate[0]["Price"]), 2);
                                    OriginalLowestRate = Math.Round(Convert.ToDouble(DTdrdate[0]["OriginalPrice"]), 2);
                                    //  BookingStatus = Convert.ToString(DTdrdate[0]["BookingStatus"]);
                                }
                                for (int dtdate = 0; dtdate <= DTdrdate.GetLength(0) - 1; dtdate++)
                                {
                                    if (BookingStatus == "" || BookingStatus == string.Empty || BookingStatus.ToLower().Trim() == "onrequest")
                                        BookingStatus = Convert.ToString(DTdrdate[dtdate]["BookingStatus"]);
                                }
                                if (Convert.ToString(SiteminderResponse.Tables[0].Rows[h]["FreeDiscount"]) != "" && Convert.ToString(SiteminderResponse.Tables[0].Rows[h]["FreeDiscount"]) != "0")
                                {
                                    string DiscountPer = Convert.ToString(SiteminderResponse.Tables[0].Rows[h]["FreeDiscount"]);
                                    DiscountPerTtext = Convert.ToString(SiteminderResponse.Tables[0].Rows[h]["FreeDiscount"]);// ""Save " + Convert.ToString(DiscountPer) + "% in discounts*";
                                    if (ItemPrice != ItemOriginalPrice)
                                    {
                                        if (Convert.ToString(drRoomCategories[rc]["OfferCode"]) != "")
                                        {
                                            drhotelRoom["IncudedOfferDiscount"] = ItemPrice;
                                            drhotelRoom["GrossWitohutDiscount"] = ItemOriginalPrice;
                                            drhotelRoom["DiscountPer"] = DiscountPer;
                                            drhotelRoom["DiscountPerText"] = DiscountPerTtext;
                                        }
                                        //   drhotelRoom["EssentialInformation"] = Convert.ToString(SiteminderResponse.Tables[0].Rows[h]["EssentialInformation"]);
                                    }
                                }
                                else
                                    DiscountPerTtext = "";
                                double b2cItemPriceMarkup = 0.0;
                                if (isPerce)
                                    b2cItemPriceMarkup = Math.Round((Math.Round((ItemPrice * AgentPercentage), 6) / 100), 6);
                                else
                                    b2cItemPriceMarkup = AgentMarkupAmount;

                                ItemPrice = Math.Round(ItemPrice + b2cItemPriceMarkup, 6);
                                //Before Discount  Start
                                if (Convert.ToString(drhotelRoom["IncudedOfferDiscount"]) != "" && Convert.ToDouble(drhotelRoom["IncudedOfferDiscount"]) != Convert.ToDouble(drhotelRoom["GrossWitohutDiscount"]))
                                {
                                    double b2cItemOriginalPriceMarkup = Math.Round((Math.Round((ItemOriginalPrice * B2CMarkup), 2) / 100), 2);
                                    ItemOriginalPrice = Math.Round(ItemOriginalPrice + b2cItemOriginalPriceMarkup, 2);
                                }
                                //Before Discount End 
                                ItemPrice = ItemPrice + ItemSurchargePrice;

                                drhotelRoom["CompanySellingPrice"] = ItemPrice;
                                //drhotelRoom["CompanyBranchBuyingPrice"] = ItemPrice;
                                //drhotelRoom["CompanyBranchSellingPrice"] = ItemPrice;

                                if (IsB2C == false)
                                {
                                    if (IsParentId == 0)
                                    {
                                        drhotelRoom["AgentBuyingPrice"] = ItemPrice;
                                        double b2bSubMarkup = Math.Round((Math.Round((ItemPrice * SubAgentMarkup), 2) / 100), 2);
                                        ItemPrice = Math.Round(ItemPrice + b2bSubMarkup, 2);

                                        //Before Discount  Start
                                        if (Convert.ToString(drhotelRoom["IncudedOfferDiscount"]) != "" && Convert.ToDouble(drhotelRoom["IncudedOfferDiscount"]) != Convert.ToDouble(drhotelRoom["GrossWitohutDiscount"]))
                                        {
                                            double b2bOriginalSubMarkup = Math.Round((Math.Round((ItemOriginalPrice * SubAgentMarkup), 2) / 100), 2);
                                            ItemOriginalPrice = Math.Round(ItemOriginalPrice + b2bOriginalSubMarkup, 2);
                                        }

                                        //Before Discount End 

                                        drhotelRoom["AgentSellingPrice"] = ItemPrice;
                                        //if (Convert.ToDouble(drhotelRoom["CompanyBuyingPrice"]) == Convert.ToDouble(drhotelRoom["CompanySellingPrice"]))
                                        //{
                                        //    drhotelRoom["CompanySellingPrice"] = drhotelRoom["AgentSellingPrice"];
                                        //    drhotelRoom["AgentBuyingPrice"] = drhotelRoom["AgentSellingPrice"];
                                        //}
                                    }
                                    if (IsParentId == 1)
                                        drhotelRoom["AgentBuyingPrice"] = ItemPrice;
                                    else
                                        drhotelRoom["SubAgentBuyingPice"] = ItemPrice;

                                    drhotelRoom["AgentBuyingPrice"] = Convert.ToDouble(drhotelRoom["AgentBuyingPrice"]);
                                    drhotelRoom["TotalSurcharge"] = totSurchar;
                                    double b2bMarkup = Math.Round((Math.Round((ItemPrice * B2CMarkup), 2) / 100), 2);

                                    ItemPrice = ItemPrice + b2bMarkup;

                                    //Before Discount  Start
                                    if (Convert.ToString(drhotelRoom["IncudedOfferDiscount"]) != "" && Convert.ToDouble(drhotelRoom["IncudedOfferDiscount"]) != Convert.ToDouble(drhotelRoom["GrossWitohutDiscount"]))
                                    {
                                        double b2bOriginalMarkup = 0.0;
                                        if (isPerce)
                                            b2bOriginalMarkup = Math.Round((Math.Round((ItemOriginalPrice * AgentPercentage), 2) / 100), 2);
                                        else
                                            b2bOriginalMarkup = AgentMarkupAmount;

                                    }
                                    //Before Discount End 

                                    if (IsParentId == 1)
                                    {
                                        drhotelRoom["AgentSellingPrice"] = ItemPrice;
                                        //if (Convert.ToDouble(drhotelRoom["CompanyBuyingPrice"]) == Convert.ToDouble(drhotelRoom["CompanySellingPrice"]))
                                        //{
                                        //    drhotelRoom["CompanySellingPrice"] = drhotelRoom["AgentSellingPrice"];
                                        //    drhotelRoom["AgentBuyingPrice"] = drhotelRoom["AgentSellingPrice"];
                                        //}
                                    }
                                    else
                                        drhotelRoom["SubAgentSellingPrice"] = ItemPrice;

                                    drhotelRoom["FinalSellingPrice"] = ItemPrice;
                                    //Before Discount  Start
                                    if (Convert.ToString(drhotelRoom["IncudedOfferDiscount"]) != "" && Convert.ToDouble(drhotelRoom["IncudedOfferDiscount"]) != Convert.ToDouble(drhotelRoom["GrossWitohutDiscount"]))
                                    {
                                        drhotelRoom["GrossWitohutDiscount"] = ItemOriginalPrice;
                                    }
                                    //Before Discount End 
                                    drhotelRoom["SupplierName"] = "Staah";
                                }
                                DtHotelRoom.Rows.Add(drhotelRoom);

                                ItemPrice_Text = Convert.ToString(ItemPrice);
                                if (rc == 0)
                                {
                                    double b2cMarkup = Math.Round((Math.Round((LowestRate * B2CMarkup), 2) / 100), 2);
                                    LowestRate = Math.Round(LowestRate + b2cMarkup, 2);

                                    double b2cOriginalMarkup = Math.Round((Math.Round((OriginalLowestRate * B2CMarkup), 2) / 100), 2);
                                    OriginalLowestRate = Math.Round(OriginalLowestRate + b2cOriginalMarkup, 2);

                                    if (IsParentId == 0)
                                    {
                                        double b2bSubMarkup = Math.Round((Math.Round((LowestRate * SubAgentMarkup), 2) / 100), 2);
                                        LowestRate = Math.Round(LowestRate + b2bSubMarkup, 2);
                                        double b2bOriginalSubMarkup = Math.Round((Math.Round((OriginalLowestRate * SubAgentMarkup), 2) / 100), 2);
                                        OriginalLowestRate = Math.Round(OriginalLowestRate + b2bOriginalSubMarkup, 2);
                                    }
                                    if (isPerce)
                                    {
                                        if (AgentPercentage > 0)
                                        {
                                            double b2bMarkup = Math.Round((Math.Round((LowestRate * AgentPercentage), 2) / 100), 2);
                                            LowestRate = Math.Round(LowestRate + b2bMarkup, 2);
                                            double b2bOriginalMarkup = Math.Round((Math.Round((OriginalLowestRate * AgentPercentage), 2) / 100), 2);
                                            OriginalLowestRate = Math.Round(OriginalLowestRate + b2bOriginalMarkup, 2);
                                        }
                                    }
                                    else
                                    {
                                        LowestRate = Math.Round(LowestRate + AgentMarkupAmount, 2);
                                        OriginalLowestRate = Math.Round(OriginalLowestRate + AgentMarkupAmount, 2);
                                    }
                                    //int NoOfNoght = Convert.ToInt32(xmlDocRequest.SelectSingleNode("//Hotel/StayDateRange/@TotalNights").InnerText);
                                }
                            }

                            string LocationText = CityName;
                            try
                            {
                                LocationText = Convert.ToString(SiteminderResponse.Tables[0].Rows[h]["Location"]);
                            }
                            catch (Exception ex)
                            {
                                await _exception.Error(ex);
                            }
                            drXmlHotel["GIITACode"] = giitaCode;
                            drXmlHotel["OwnsystemHotelId"] = ownsystemhotelId;
                            drXmlHotel["XmlHotelCity"] = CityId;
                            drXmlHotel["XMLHotelCode"] = ownsystemhotelId;
                            drXmlHotel["HotelName"] = HotelName;
                            drXmlHotel["EssentialInformation"] = HotelInformation;
                            drXmlHotel["Description"] = HotelDescription;
                            drXmlHotel["HotelFrontImage"] = GTAImage1;
                            drXmlHotel["Address"] = Address;
                            drXmlHotel["ReviewCount"] = ReviewCount;
                            drXmlHotel["ReviewRating"] = ReviewRating;
                            drXmlHotel["Location"] = LocationDescription;
                            drXmlHotel["XMLCountry"] = CountryId;
                            drXmlHotel["XMLCity"] = CityId;
                            drXmlHotel["Rating"] = Rating;
                            drXmlHotel["GroupName"] = Chain;
                            drXmlHotel["SortSequence"] = SortSeq;
                            drXmlHotel["latitude"] = Convert.ToString(Latitude).ToLower();
                            drXmlHotel["longitute"] = Convert.ToString(Longitude).ToLower();
                            drXmlHotel["IsOffer"] = Convert.ToString(SiteminderResponse.Tables[0].Rows[h]["FreeDiscount"]) == "" ? "false" : "true";
                            drXmlHotel["OfferText"] = Convert.ToString(SiteminderResponse.Tables[0].Rows[h]["FreeDiscount"]);
                            drXmlHotel["MealName"] = "";
                            drXmlHotel["BeforeDiscountPrice"] = OriginalLowestRate;
                            drXmlHotel["SellingPrice"] = LowestRate;
                            drXmlHotel["IsRecomondedHotel"] = Convert.ToString(SiteminderResponse.Tables[0].Rows[h]["Recommended"]).ToLower() == "1" ? true : false;
                            drXmlHotel["FinalSellingPrice"] = LowestRate;
                            drXmlHotel["BookingStatus"] = BookingStatus;
                            drXmlHotel["IsRefundable"] = "false";
                            drXmlHotel["SupplierName"] = "Staah";
                            drXmlHotel["OfficeID"] = 0;
                            drXmlHotel["ContractID"] = 0;
                            drXmlHotel["Reviews"] = 0;
                            drXmlHotel["UniqueNo"] = uniqueno;
                            drXmlHotel["IsShowSupplier"] = blnIsShowSupplier;
                            drXmlHotel["ReviewCount"] = ReviewCount;
                            drXmlHotel["ReviewRating"] = ReviewRating;
                            drXmlHotel["SupplierId"] = SupplierId;
                            drXmlHotel["RaynaId"] = ownsystemhotelId;
                            drXmlHotel["MasterSupplierId"] = Convert.ToInt32(MasterSupplierID);
                            drXmlHotel["ReservationEmail"] = Convert.ToString(SiteminderResponse.Tables[0].Rows[h]["ReservationEmail"]);
                            //drXmlHotel["HotelContactNo"] = Convert.ToString(SiteminderResponse.Tables[0].Rows[h]["HotelContactNo"]);
                            DtHotel.Rows.Add(drXmlHotel);
                            //BookingStatus = "";
                            //DtHotel.Rows.Add(drXmlHotel);
                            Options ops = (await _supplierRes.GetCommonRoomForSuppliers(Convert.ToInt64(giitaCode), commonSearch.tokenData, commonSearch.advancedOptions, LowestRate, drXmlHotel, DtHotelRoom, dtCancellation, DtHotelRoomRateBreackUp, SearchParam));
                            if (ops != null)
                                options.Add(ops);
                            BookingStatus = "";
                        }
                    }
                    return options;
                }
                catch (Exception ex)
                {
                    Item.ResponseStatus = 404;
                    await _exception.Error(ex, SearchParam.SearchKey);
                    return options;
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
                            
                            SearchParam.SupplierTime += ",|Finish:Staah Process Response  " + "|Time :" + timerroomx.Elapsed.ToString();
                            _= Task.Run(() => _commonMethod.FireAway_SQL_ResPorcessTimeLog("", timerroomx.Elapsed.ToString(), "Staah", SearchParam.SearchKey, 1, SearchParam.AgentID));

                        }
                    }
                    catch (Exception x)
                    {
                       await _exception.Error(x);
                    }
                    #endregion
                    //GC.Collect();
                    if (Item.ResponseType == "NoCount" && StoreSupplierResErrorLog == "1")
                        _ = Task.Run(() => _commonMethod.XMLRequestResponseTrack_Error(Item.RQRS[0].Request, "", Item.Supplier, timerroomx.Elapsed.ToString(), "", SearchParam.SearchKey));
                }
            }
            return options;

        }

        //prebook 23-07-25

        public async Task<PriceCheckDto> StaahPreBookingReq(string supplier,DataSet StaahPreeBookingResponse, string ReferenceNo, string ID, string Password, string HotelCode, string RCode, int NoOfAdults, int NoOfChild, string CheckInDate, string CheckOutDate, double Rate, double FinalPrice, int UserCartId, int UserCartDetailId, DataTable dtCheckPriceReqStructure, int j, string UniqueNo, string AgentUniqueNo, bool isForBooking, int ChildAge1, int ChildAge2, int NoOfRoom, string RateId, int ExtrabedQuantity, string SupplierCurrency, double CurrencyFactorStaah, double CommissionRate, double Offer, int AgentId)
        {
            PriceCheckDto priceCheckDto = new PriceCheckDto();
            int flag = 1;


            string Error = "";
            string ResponseData = "";
            string Req = "";
            try
            {

                CheckInDate = CheckInDate.Substring(0, 4) + "-" + CheckInDate.Substring(4, 2) + "-" + CheckInDate.Substring(6, 2);
                CheckOutDate = CheckOutDate.Substring(0, 4) + "-" + CheckOutDate.Substring(4, 2) + "-" + CheckOutDate.Substring(6, 2);
                StringBuilder PreBookRQ = new StringBuilder();
                string ARR_info = "ARR_info";
                PreBookRQ.Append("{\"propertyid\":\"" + HotelCode + "\",");
                PreBookRQ.Append("\"apikey\": \"" + Password + "\",");
                PreBookRQ.Append("\"room_id\": \"" + RCode + "\",");
                PreBookRQ.Append("\"rate_id\": \"" + RateId + "\",");
                PreBookRQ.Append("\"action\": \"" + ARR_info + "\",");
                PreBookRQ.Append("\"from_date\": \"" + CheckInDate + "\",");
                PreBookRQ.Append("\"to_date\": \"" + CheckOutDate + "\",");
                PreBookRQ.Append("\"version\": 2 }");



                Req = PreBookRQ.ToString();
                if (isForBooking == false)// hit if not from booking
                {
                    dtCheckPriceReqStructure.Rows.Add(_commonMethod.AddCheckPriceReq(dtCheckPriceReqStructure.NewRow(), j + 1, UserCartId, UserCartDetailId, RCode, "Staah", Req));
                }

                string Action = Convert.ToString(StahhInfo.StaahPreeBookUrl);


                System.Diagnostics.Stopwatch timerroomprocess = new System.Diagnostics.Stopwatch();
                timerroomprocess.Start();

                ResponseDto responseDto = await StaahGetResponse(Action, Password, Req, 180000);
                ResponseData = responseDto.Response;
                Error = responseDto.Error;

                timerroomprocess.Stop();
                 //time = timerroomprocess.Elapsed.ToString();

                _commonMethod.ThirdPartyBookingHistory(Convert.ToString(UniqueNo), Convert.ToString(AgentUniqueNo), Req, ResponseData, "After Get Price Staah Request hit preebook" + j + "isForBooking" + isForBooking, "Staah", "Response Confirm", "-", "Hotelinx");
                if (ResponseData == string.Empty || ResponseData == "")
                {
                    try
                    {

                        //string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(supplier, "PreBook", AgentId, time, responsetype, Req, ResponseData, Error);
                    }
                    catch (Exception e)
                    {
                        _ = _exception.Error(e);
                    }

                    priceCheckDto.Flag = 4;
                    return priceCheckDto;
                }
                if (ExtrabedQuantity > 0 && NoOfChild == 0)
                {
                    NoOfAdults = NoOfAdults - 1;
                }
                XmlDocument xd = new XmlDocument();
                ResponseData = "{ \"rootNode\": {" + ResponseData.Trim().TrimStart('{').TrimEnd('}') + "} }";
                xd = (XmlDocument)JsonConvert.DeserializeXmlNode(ResponseData);

                StaahPreeBookingResponse.ReadXml(new XmlNodeReader(xd));
                //DataSet StaahPreeBookingResponse = Common.GetDataSetFromXMLString(ResponseData);
                //List<Staah.Staahpreebook> StaahPreeBookingResponse = JsonConvert.DeserializeObject<List<Staah.Staahpreebook>>(ResponseData);
                double TotalPrice = 0;
                if (!ResponseData.ToLower().Contains("fail"))
                {
                    for (j = 0; j < StaahPreeBookingResponse.Tables["data"].Rows.Count - 1; j++)
                    {
                        int Allocation = Convert.ToInt32(StaahPreeBookingResponse.Tables["data"].Rows[j]["inventory"]);
                        string Stopsell = Convert.ToString(StaahPreeBookingResponse.Tables["data"].Rows[j]["stopsell"]);
                        if (Allocation > 0)
                        {
                            //if (StaahPreeBookingResponse.Tables["amountAfterTax"].Columns.Contains("Rate") == true)
                            //    TotalPrice += Convert.ToDouble(StaahPreeBookingResponse.Tables["amountAfterTax"].Rows[j]["Rate"]);
                            if (StaahPreeBookingResponse.Tables.Contains("obp") == true)
                            {
                                if (NoOfAdults == 1)
                                {
                                    if (StaahPreeBookingResponse.Tables["obp"].Columns.Contains("person1") == true)
                                        TotalPrice += Convert.ToDouble(StaahPreeBookingResponse.Tables["obp"].Rows[j]["person1"]);
                                }
                                else if (NoOfAdults == 2)
                                {
                                    if (StaahPreeBookingResponse.Tables["obp"].Columns.Contains("person2") == true)
                                        TotalPrice += Convert.ToDouble(StaahPreeBookingResponse.Tables["obp"].Rows[j]["person2"]);
                                }
                                else if (NoOfAdults == 3)
                                {
                                    if (StaahPreeBookingResponse.Tables["obp"].Columns.Contains("person3") == true)
                                        TotalPrice += Convert.ToDouble(StaahPreeBookingResponse.Tables["obp"].Rows[j]["person3"]);
                                }
                                else if (NoOfAdults == 4)
                                {
                                    if (StaahPreeBookingResponse.Tables["obp"].Columns.Contains("person4") == true)
                                        TotalPrice += Convert.ToDouble(StaahPreeBookingResponse.Tables["obp"].Rows[j]["person4"]);
                                }
                                else if (NoOfAdults == 5)
                                {
                                    if (StaahPreeBookingResponse.Tables["obp"].Columns.Contains("person5") == true)
                                        TotalPrice += Convert.ToDouble(StaahPreeBookingResponse.Tables["obp"].Rows[j]["person5"]);
                                }
                                else if (NoOfAdults == 6)
                                {
                                    if (StaahPreeBookingResponse.Tables["obp"].Columns.Contains("person6") == true)
                                        TotalPrice += Convert.ToDouble(StaahPreeBookingResponse.Tables["obp"].Rows[j]["person6"]);
                                }
                                else if (NoOfAdults == 7)
                                {
                                    if (StaahPreeBookingResponse.Tables["obp"].Columns.Contains("person7") == true)
                                        TotalPrice += Convert.ToDouble(StaahPreeBookingResponse.Tables["obp"].Rows[j]["person7"]);
                                }
                                else if (NoOfAdults == 8)
                                {
                                    if (StaahPreeBookingResponse.Tables["obp"].Columns.Contains("person8") == true)
                                        TotalPrice += Convert.ToDouble(StaahPreeBookingResponse.Tables["obp"].Rows[j]["person8"]);
                                }
                                else if (NoOfAdults == 9)
                                {
                                    if (StaahPreeBookingResponse.Tables["obp"].Columns.Contains("person9") == true)
                                        TotalPrice += Convert.ToDouble(StaahPreeBookingResponse.Tables["obp"].Rows[j]["person9"]);
                                }
                                else
                                {
                                    priceCheckDto.Flag = 4;
                                    return priceCheckDto;
                                }
                            }
                            if (ExtrabedQuantity > 0)
                            {
                                if (NoOfChild > 0)
                                {
                                    if (StaahPreeBookingResponse.Tables["amountAfterTax"].Columns.Contains("extrachild") == true)
                                        TotalPrice += Convert.ToDouble(StaahPreeBookingResponse.Tables["amountAfterTax"].Rows[j]["extrachild"]);
                                }
                                else
                                {
                                    if (StaahPreeBookingResponse.Tables["amountAfterTax"].Columns.Contains("extraadult") == true)
                                        TotalPrice += Convert.ToDouble(StaahPreeBookingResponse.Tables["amountAfterTax"].Rows[j]["extraadult"]);
                                }
                            }
                        }
                        else
                        {
                            priceCheckDto.Flag = 4;
                            return priceCheckDto;
                        }
                    }
                }
                else
                {
                    priceCheckDto.Flag = 4;
                    return priceCheckDto;
                }

                FinalPrice = TotalPrice * CurrencyFactorStaah;
                if (CommissionRate > 0)
                {
                    FinalPrice = FinalPrice - FinalPrice * CommissionRate / 100;
                }
                if (Offer > 0)
                {
                    FinalPrice = FinalPrice - FinalPrice * Offer / 100;
                }
                priceCheckDto.FinalPrice = FinalPrice;
                if (Math.Round(Rate, 2) != Math.Round(FinalPrice, 2))
                {
                    try
                    {

                        //int AgentId = Convert.ToInt32(Common.getSessionr("UserId") == "" ? "0" : Common.getSessionr("UserId"));
                        // string abc = Common.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, Req, ResponseData, Error);
                    }
                    catch (Exception e)
                    {
                        _ = _exception.Error(e);
                    }
                    priceCheckDto.Flag = 4;
                    return priceCheckDto;
                }
            }
            catch (Exception ex)
            {
                try
                {

                    //int AgentId = Convert.ToInt32(Common.getSessionr("UserId") == "" ? "0" : Common.getSessionr("UserId"));
                    //  string abc = Common.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, Req, ResponseData, Error);
                }
                catch (Exception e)
                {
                    _ = _exception.Error(e);
                }
                _ = _exception.Error(ex);

                priceCheckDto.Flag = 2;
                return priceCheckDto;
            }
            try
            {

                //int AgentId = Convert.ToInt32(Common.getSessionr("UserId") == "" ? "0" : Common.getSessionr("UserId"));
                // string abc = Common.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, Req, ResponseData, Error);
            }
            catch (Exception e)
            {
                _ = _exception.Error(e);
            }
            priceCheckDto.Flag = 0;
            priceCheckDto.FinalPrice = FinalPrice;

            return priceCheckDto;
        }

        public async Task<ResponseDto> StaahGetResponse(string URL, string apikey, string request, Int32 TimeOut)
        {
            ResponseDto responseDto = new ResponseDto();
            string Error = "";

            System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
            timer.Start();
            string strOutput = "";
            try
            {
                HttpWebRequest Request;
                Request = (HttpWebRequest)HttpWebRequest.Create(URL);
                Request.Timeout = TimeOut;
                Request.Method = "POST";
                Request.ContentType = "application/json";
                //Request.Headers.Add("x-api-key", apikey);
                Request.KeepAlive = false;
                //  Request.Headers.Add("Accept-Encoding", "gzip");
                // Request.Accept = "application/json";
                ServicePointManager.Expect100Continue = true;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls
                       | SecurityProtocolType.Tls11
                       | SecurityProtocolType.Tls12;
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
                    _ = _exception.Error(we);

                    
                }
            }
            catch (Exception ex)
            {
                Error = ex.Message.ToString();
                
                _ = _exception.Error(ex);
            }
            responseDto.Error = Error;
            responseDto.Response = strOutput;
            return responseDto;
        }

        public string Get_Staah_BookingRequest(DataSet dsCartDetails, DataTable dtPassengers, DataRow[] DerbySoftDetails, string UniqueNo, bool IsPriceUpdate, int i, int j, string PreBookingToken, string UserID, string Password, string ExternalReference, string Bookingtokan)
        {
            try
            {
                #region room Booking RQ

                string RoomTypeId = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomTypeID"]);
                string DerbySoftUserName = Convert.ToString(DerbySoftDetails[0]["UserName"]);
                string DerbySoftPassword = Convert.ToString(DerbySoftDetails[0]["Password"]);
                string version = Convert.ToString(DerbySoftDetails[0]["Version"]);
                string xmlhotelid = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["XMLHotelId"]);
                string xmlhotelname = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["ServiceName"]);
                string CheckIn = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["CheckInDate"]);//"20180327"
                CheckIn = CheckIn.Substring(0, 4) + "-" + CheckIn.Substring(4, 2) + "-" + CheckIn.Substring(6, 2);
                string CheckOut = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["CheckOutDate"]);
                CheckOut = CheckOut.Substring(0, 4) + "-" + CheckOut.Substring(4, 2) + "-" + CheckOut.Substring(6, 2);
                int NoOfNights = Convert.ToInt32(dsCartDetails.Tables[0].Rows[i]["NoOfNight"]);
                int NoOfRoom = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView.Count);
                string NewPriceString = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["EachoToken"]);
                string valcomm = "";
                #region Old
                if (dsCartDetails.Tables[0].Columns.Contains("OperationsDetails") == true)
                {
                    string comment = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["OperationsDetails"]);

                    if (comment != "")
                    {
                        string[] commetlist = comment.Split('/');
                        if (commetlist.Length > 1)
                        {
                            string commentval = Convert.ToString(commetlist[1]);
                            string[] commnetvalstring = commentval.Split(':');
                            if (commnetvalstring.Length > 0)
                            {
                                valcomm = Convert.ToString(commnetvalstring[1]);
                            }
                        }
                    }
                }
                #endregion
                string[] roomtype = RoomTypeId.Split(new String[] { "$$$" }, StringSplitOptions.None);

                if (roomtype.Length > 0)
                {
                    int AdultCount = 0;
                    int ChildCount = 0;
                    AdultCount = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[j]["NoOfAdults"]);
                    ChildCount = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[j]["NoOfChild"]);
                    string RoomId = roomtype[0];
                    string RateId = roomtype[1];
                    string MealId = roomtype[7];
                    string MealName = roomtype[8];
                    // string HotelId = roomtype[4];
                    string RoomName = roomtype[9].ToString();
                    //string RateBreakUp = roomtype[12].ToString();
                    string RateBreakUp = roomtype[13].ToString();//Added on 2-APR-2025 by k2
                    string MainPrice = roomtype[5];
                    string RateType = "amountaftertax";

                    // string Token = roomtype[7];

                    string StaahCurrency = roomtype[2];
                    string ExtraAdult = roomtype[3];
                    string Extrabedquantity = roomtype[4];
                    //  MainPrice = NewPriceString;

                    double TotalPrice = 0;
                    double ExtraAdultPrice = 0;
                    double ChildPrice = 0;
                    DataTable dtInrCurrency = _commonMethod.GetCountryCurrency(StaahCurrency);
                    double CurrencyFactorStaah = Convert.ToDouble(dtInrCurrency.Rows[0]["AmtToExch"]);
                    double companybuyingprice = Convert.ToDouble(dsCartDetails.Tables[1].DefaultView[j]["CompanyBuyingPrice"]) / CurrencyFactorStaah;
                    //string[] Totalpriceth = MainPrice.Split('-');
                    //string[] ExadultPrice = ExtraAdult.Split('-');
                    //for (int pi = 0; pi < Totalpriceth.GetLength(0); pi++)
                    ExtraAdultPrice = Math.Round(Convert.ToDouble(ExtraAdult), 2) / CurrencyFactorStaah;
                    {
                        TotalPrice = Math.Round(companybuyingprice, 2);// + Convert.ToDouble(ExtraAdultPrice);
                    }
                    #region Staah New Request

                    #region Reservation

                    List<Staah.StaahBookingRequest.Reservation> reservationList = new List<Staah.StaahBookingRequest.Reservation>();

                    Staah.StaahBookingRequest.Reservation reservation = new Staah.StaahBookingRequest.Reservation();
                    reservation.reservation_datetime = Convert.ToDateTime(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss"));
                    reservation.propertyname = xmlhotelname;
                    reservation.reservation_id = ExternalReference;
                    reservation.payment_required = "";
                    reservation.payment_type = "Channel Collect";
                    reservation.commissionamount = "0.00";
                    reservation.discountamount = "0.00";
                    reservation.deposit = Convert.ToString(Math.Round(Convert.ToDouble(TotalPrice), 2));
                    reservation.totalamountaftertax = Convert.ToString(Math.Round(Convert.ToDouble(TotalPrice), 2));//* Convert.ToInt16(NoOfRoom));
                    reservation.totaltax = "0.00";
                    reservation.currencycode = StaahCurrency;
                    reservation.status = "Confirm";

                    #region customer
                    dtPassengers.DefaultView.RowFilter = "PickUp='" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomTypeID"]) + "' and RoomNo='" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomNumber"] + "'");

                    //dtPassengers.DefaultView.RowFilter = "PickUp='" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[0]["RoomTypeId"]) + "' and UserCartID='" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[0]["UserCartID"]) + "'and RoomNo='" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomNumber"]) + "'";
                    Staah.StaahBookingRequest.Customer customer = new Staah.StaahBookingRequest.Customer();
                    for (int p = 0; p < dtPassengers.DefaultView.RowFilter.Count(); p++)
                    {
                        //if (Convert.ToString(dtPassengers.DefaultView[p]["IsLead"]) == "1")
                        {
                            customer.address = "";
                            customer.city = "";
                            customer.country = Convert.ToString(dtPassengers.DefaultView[p]["Nationality"]);
                            customer.email = Convert.ToString(dtPassengers.DefaultView[p]["Email"]);
                            customer.salutation = Convert.ToString(dtPassengers.DefaultView[p]["Prefix"]).Trim().Replace(".", "");
                            customer.first_name = Convert.ToString(dtPassengers.DefaultView[p]["FirstName"]);
                            customer.last_name = Convert.ToString(dtPassengers.DefaultView[p]["LastName"]);
                            customer.remarks = valcomm;
                            customer.telephone = Convert.ToString(dtPassengers.DefaultView[p]["Mobile"]);
                            customer.zip = "";
                            reservation.customer = customer;
                            break;
                        }
                    }

                    #endregion
                    #region "cmt"
                    //#region Paymentcarddetail

                    //#region APISO VC

                    //int usercartdetailsid = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[j]["UserCartDetailId"]);
                    ////get bookingid and detailsid

                    //DBConnection db = new DBConnection();
                    //SqlParameter[] param = new SqlParameter[]
                    //{
                    //new SqlParameter("@usercartdetailsid",usercartdetailsid)
                    //};

                    //DataTable dtbookingdata = null; // db.GetDataTable("getApisobookingdata", param);

                    //string Pan = "";
                    //string CVV = "0", Expiry = "0";

                    //if (dtbookingdata != null)
                    //{
                    //    // GetApisoServiceDatatoCreateVCN("", Convert.ToInt32(dtbookingdata.Rows[0]["OnlineBookingMasterId"]), Convert.ToInt32(dtbookingdata.Rows[0]["OnlineBookingDetailsId"]), ref Pan, ref CVV, ref Expiry);
                    //    //PANN = Pan;
                    //}
                    //#endregion

                    ////string ExpiryRegex = Convert.ToString(Expiry).Substring(2, 2);
                    ////string ExpiryRegexFisrt = Convert.ToString(Expiry).Substring(0, 2);


                    ////DerbySoftBooking.Payment Payment = new DerbySoftBooking.Payment();
                    ////Payment.cardCode = Convert.ToString(ConfigurationManager.AppSettings["cardCode"]);
                    ////Payment.cardNumber = Convert.ToString(Pan); //Convert.ToString(ConfigurationManager.AppSettings["cardNumber"]);
                    ////Payment.cardHolderName = Convert.ToString(ConfigurationManager.AppSettings["cardHolderName"]);
                    ////Payment.expireDate = Convert.ToString(ExpiryRegex + ExpiryRegexFisrt); //Convert.ToString(ConfigurationManager.AppSettings["expireDate"]);

                    //Staah.StaahBookingRequest.Paymentcarddetail Paymentcarddetail = new Staah.StaahBookingRequest.Paymentcarddetail();
                    //Paymentcarddetail.CardHolderName = "Test Staah";
                    //Paymentcarddetail.CardType = "MC";
                    //Paymentcarddetail.ExpireDate = "07/2022";
                    //Paymentcarddetail.CardNumber = "41111111111111111";
                    //Paymentcarddetail.cvv = "123";

                    //reservation.paymentcarddetail = Paymentcarddetail;

                    //#endregion
                    #endregion"cmt"


                    #region Staah Room
                    List<Staah.StaahBookingRequest.Room> RoomRates1 = new List<Staah.StaahBookingRequest.Room>();
                    // for (int Roomid = 0; Roomid < NoOfRoom; Roomid++)
                    {
                        Staah.StaahBookingRequest.Room RoomRate1 = new Staah.StaahBookingRequest.Room();
                        RoomRate1.arrival_date = CheckIn;
                        RoomRate1.departure_date = CheckOut;
                        RoomRate1.room_id = RoomId;
                        RoomRate1.room_name = RoomName;
                        List<Staah.StaahBookingRequest.Price> PriceList = new List<Staah.StaahBookingRequest.Price>();
                        //DataTable dtRateBreakup = ((DataTable)HttpContext.Current.Session["AddCartHotelRateBreakeUp"]);
                        //DataRow[] drrRateBreakup = dtRateBreakup.Select("CartId='" + Convert.ToInt32(dsCartDetails.Tables[0].Rows[i]["UserCartId"]) + "'and RoomTypeId='" + RoomTypeId + "' and RoomNo='" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomNumber"]) + "' ");
                        string[] RateBreakUpRow = RateBreakUp.Split('#');
                        DateTime EffectiveDate = Convert.ToDateTime(CheckIn).Date;
                        //DateTime EffectiveDate = Convert.ToDateTime(drrRateBreakup[0]["Date"]).Date;
                        double totalprice = 0;
                        //decimal perDayAmount = 0.00m;
                        // string ExadultPrice1 = Convert.ToString(Convert.ToDouble(ExtraAdult) / CurrencyFactorStaah) ; 
                        string ExA_pricenew = Convert.ToString(Math.Round(Convert.ToDouble(ExtraAdultPrice), 2) / NoOfNights);
                        for (int n = 0; n < NoOfNights; n++)
                        {
                            Staah.StaahBookingRequest.Price Price = new Staah.StaahBookingRequest.Price();
                            Price.date = Convert.ToDateTime(CheckIn).AddDays(n).ToString("yyyy-MM-dd");
                            Price.rate_id = RateId;
                            // string EffectiveDate1 = Convert.ToString(EffectiveDate);
                            // EffectiveDate1 = EffectiveDate.ToString("yyyy-MM-dd");
                            //perDayAmount = Math.Round(Convert.ToDecimal(drrRateBreakup[n]["CompanyRate"]), 2);
                            double perDayAmount = Math.Round(Convert.ToDouble(RateBreakUpRow[n]), 2);
                            // string RatePlanCode1 = Roomids1[1].ToString();
                            //DateTime ExpireDate = EffectiveDate.AddDays(1);
                            //string ExpireDate1 = Convert.ToString(ExpireDate);
                            //ExpireDate1 = ExpireDate.ToString("yyyy-MM-dd");
                            if (RateType.ToLower() == "amountaftertax")
                            {
                                //string[] priceth = MainPrice.Split('-');

                                //string pricenew = Convert.ToString(Convert.ToDouble(priceth[n]));
                                Price.amountaftertax = Convert.ToString(Math.Round(Convert.ToDouble(perDayAmount), 2));




                                totalprice = totalprice + Math.Round(Convert.ToDouble(perDayAmount), 2) + Math.Round(Convert.ToDouble(ExA_pricenew), 2);

                                if (ExA_pricenew != "0")
                                {
                                    Staah.StaahBookingRequest.ExtraGuests ExtraGuests = new Staah.StaahBookingRequest.ExtraGuests();
                                    if (ChildCount == 0)
                                    {
                                        ExtraGuests.extraAdult = Extrabedquantity;

                                    }
                                    else
                                    {
                                        ExtraGuests.extraAdult = "0";
                                    }
                                    ExtraGuests.extraChild = Convert.ToString(ChildCount);
                                    if (ChildCount == 0)
                                    {
                                        ExtraGuests.extraAdultRate = ExA_pricenew;

                                    }
                                    else
                                    {
                                        ExtraGuests.extraAdultRate = "0.00";
                                    }
                                    if (ChildCount > 0)
                                    {
                                        ExtraGuests.extraChildRate = ExA_pricenew;

                                    }
                                    else
                                    {
                                        ExtraGuests.extraChildRate = "0.00";

                                    }
                                    Price.extraGuests = ExtraGuests;
                                }
                            }
                            PriceList.Add(Price);
                            RoomRate1.price = PriceList;
                            // EffectiveDate = EffectiveDate.AddDays(1);
                        }





                        // dtPassengers.DefaultView.RowFilter = "PickUp='" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[0]["RoomTypeId"]) + "' and UserCartID='" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[0]["UserCartID"]) + "' and RoomNo='" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomNumber"]) + "'";
                        dtPassengers.DefaultView.RowFilter = "PickUp='" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomTypeID"]) + "' and RoomNo='" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomNumber"] + "'");


                        for (int p = 0; p < dtPassengers.DefaultView.RowFilter.Count(); p++)
                        {
                            RoomRate1.salutation = Convert.ToString(dtPassengers.DefaultView[0]["Prefix"]).Trim().Replace(".", "");
                            RoomRate1.first_name = Convert.ToString(dtPassengers.DefaultView[0]["FirstName"]);
                            RoomRate1.last_name = Convert.ToString(dtPassengers.DefaultView[0]["LastName"]);
                        }

                        //RoomRate1.first_name = Convert.ToString(dtPassengers.DefaultView[0]["FirstName"]); 
                        //RoomRate1.last_name = Convert.ToString(dtPassengers.DefaultView[0]["LastName"]); 
                        //List<Staah.StaahBookingRequest.Taxis> taxlist = new List<Staah.StaahBookingRequest.Taxis>();
                        //Staah.StaahBookingRequest.Taxis tax = new Staah.StaahBookingRequest.Taxis();
                        //tax.name = "";
                        //tax.value = "";
                        //taxlist.Add(tax);
                        //RoomRate1.taxes = taxlist;
                        //List<Staah.StaahBookingRequest.Addon> Addonlist = new List<Staah.StaahBookingRequest.Addon>();
                        //Staah.StaahBookingRequest.Addon Addon = new Staah.StaahBookingRequest.Addon();

                        //Addon.Name = "";
                        //Addon.Price = "";
                        //Addon.Type = "";
                        //Addon.Nights = "";
                        //Addon.Persons = "";
                        //Addon.Priceperunit = "";
                        //Addon.PriceMode = "";
                        //Addonlist.Add(Addon);
                        //RoomRate1.Addons = Addonlist;
                        if (RateType.ToLower() == "amountaftertax")
                        {
                            RoomRate1.amountaftertax = Convert.ToString(totalprice);
                        }
                        RoomRate1.remarks = "";
                        List<Staah.StaahBookingRequest.GuestCount> guestCountlist = new List<Staah.StaahBookingRequest.GuestCount>();
                        Staah.StaahBookingRequest.GuestCount guestCount = new Staah.StaahBookingRequest.GuestCount();



                        guestCount.AgeQualifyingCode = "10";
                        guestCount.Count = Convert.ToString(AdultCount);
                        guestCountlist.Add(guestCount);

                        if (ChildCount > 0)
                        {
                            Staah.StaahBookingRequest.GuestCount guestCount1 = new Staah.StaahBookingRequest.GuestCount();
                            guestCount1.AgeQualifyingCode = "8";
                            guestCount1.Count = Convert.ToString(ChildCount);
                            guestCountlist.Add(guestCount1);
                        }

                        RoomRate1.GuestCount = guestCountlist;
                        RoomRates1.Add(RoomRate1);
                    }
                    #endregion

                    reservation.room = RoomRates1;
                    reservation.POS = "Hotelinx";
                    reservationList.Add(reservation);

                    Staah.StaahBookingRequest.Reservations reservations = new Staah.StaahBookingRequest.Reservations();
                    reservations.reservation = reservationList;
                    #endregion

                    Staah.StaahBookingRequest.Root RootObject1 = new Staah.StaahBookingRequest.Root();
                    RootObject1.propertyid = xmlhotelid;
                    RootObject1.apikey = Password;
                    RootObject1.action = "reservation_info";
                    RootObject1.version = version;
                    RootObject1.reservations = reservations;

                    #endregion

                    JsonSerializerSettings settings = new JsonSerializerSettings();
                    settings.NullValueHandling = NullValueHandling.Ignore;
                    settings.DefaultValueHandling = DefaultValueHandling.Ignore;
                    string jsonBooking = JsonConvert.SerializeObject(RootObject1, settings);
                    return jsonBooking;

                }

                #endregion
            }
            catch (Exception ex)
            {
                _ = _exception.Error(ex);
            }
            return string.Empty;
        }

        public async Task<ResponseDto> GetStaahResponse(string Error, string request, string TravcoURL, Int32 TimeOut, string Provider = "", HotelSearchParameter para = null, bool DebugMode = false)
        {
            ResponseDto responseDto = new ResponseDto();
            System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
            timer.Start();
            string response = null;
            HttpWebRequest httpWebRequest = null;
            HttpWebResponse httpWebResponse = null;
            httpWebRequest = (HttpWebRequest)WebRequest.Create(TravcoURL);

            try
            {
                byte[] bytes;
                bytes = System.Text.Encoding.UTF8.GetBytes(request);

                httpWebRequest.Method = "POST";
                httpWebRequest.ContentType = "application/json";
                httpWebRequest.KeepAlive = false;
                httpWebRequest.Timeout = TimeOut;
                httpWebRequest.ContentLength = bytes.Length;
                ServicePointManager.Expect100Continue = true;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                httpWebRequest.Accept = "application/json";

                Stream newStream = null;
                byte[] data = Encoding.ASCII.GetBytes(request);
                httpWebRequest.ContentLength = data.Length;
                newStream = httpWebRequest.GetRequestStream();
                newStream.Write(data, 0, data.Length);

                //using (Stream requestStream = httpWebRequest.GetRequestStream())
                //{
                //    requestStream.Write(bytes, 0, bytes.Length);
                //    requestStream.Close();//Close stream
                //}

                httpWebResponse = (HttpWebResponse)await httpWebRequest.GetResponseAsync();
                if (httpWebResponse.StatusCode == HttpStatusCode.OK)
                {

                    using (Stream responseStream = httpWebResponse.GetResponseStream())
                    {

                        Stream st = httpWebResponse.GetResponseStream();
                        if (httpWebResponse.ContentEncoding.ToLower().Contains("gzip"))
                            st = new System.IO.Compression.GZipStream(st, System.IO.Compression.CompressionMode.Decompress);
                        else if (httpWebResponse.ContentEncoding.ToLower().Contains("deflate"))
                            st = new System.IO.Compression.DeflateStream(st, System.IO.Compression.CompressionMode.Decompress);
                        StreamReader stIn = new StreamReader(st, Encoding.Default);
                        response =await stIn.ReadToEndAsync();
                    }

                    timer.Stop();
                    if (para != null)
                    {
                        para.SupplierTime += ",SupplierName :" + Provider + " : " + "Execution Time :" + timer.Elapsed.ToString();
                        if (DebugMode)
                            _ = _commonMethod.XMLRequestResponseTrack(request, response, Provider, "Proper");
                    }
                    if (Convert.ToString(_keySettings.IsLogTrack) == "1")
                    {
                        TimeSpan timeTaken = timer.Elapsed;
                        //XMLHotelBookingCommon.ErrorLog(Convert.ToString(System.Web.Configuration.WebConfigurationManager.AppSettings["LogTrackPath"]), "Trvaco->" + Provider + "-> Execution Time : " + timer.Elapsed.ToString());
                        _ = _commonMethod.XMLRequestResponseTrack(request, response, Provider == "" ? "Travco" : Provider, "Proper");
                    }
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
                _ = _commonMethod.XMLRequestResponseTrackError(request, result1, Provider == "" ? "Travco" : Provider, "Proper");

               
            }
            catch (Exception ex)
            {
                Error = ex.Message.ToString();
                //response = ex.Message + ex.InnerException;
                _ = _commonMethod.XMLRequestResponseTrack(request, ex.Message, Provider == "" ? "Travco" : Provider, "Proper");
                //ExceptionManager.LogException(ex);
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

        public string Get_Staah_BookingCancelRQ(string AgentVoucherNo, string Username, string Password, string version, string Provider, string RoomTypeID, DataTable DtCancellation, DataTable DtPassenger, string ReferenceNo, DataSet dsratebreakup, TokenData tokenData)
        {
            try
            {
                string HotelCode = Convert.ToString(DtPassenger.Rows[0]["ProductId"]);

                #region Booking RQ


                StringBuilder CancelBookRQ = new StringBuilder();
               
                string[] roomtype = RoomTypeID.Split(new String[] { "$$$" }, StringSplitOptions.None);

                string RoomId = roomtype[0];
                string RateId = roomtype[1];
                string MealId = roomtype[7];
                string MealName = roomtype[8];

                string RoomName = roomtype[9].ToString();
                //string MainPrice = roomtype[5];
                string RateType = "amountaftertax";

                string StaahCurrency = roomtype[2];
                string ExtraAdult = roomtype[3];
                string ExtraBedquantity = roomtype[4];

                string Token = roomtype[7];

                DataView view = new DataView(DtPassenger);
                DataTable distinctValues = view.ToTable(true, "romnumber");

                int NoOfRooms = distinctValues.Rows.Count; // Convert.ToInt16(roomtype[9]);
                                                           //  MainPrice = NewPriceString;
                int AdultCount = 0;
                int ChildCount = 0;
                double BookingAmount = 0;
                DateTime CheckInDate = Convert.ToDateTime(DtCancellation.Rows[0]["BookingDate"]);
                string CheckIn = CheckInDate.ToString("yyyy-MM-dd");

                DateTime CheckOutDate = Convert.ToDateTime(DtCancellation.Rows[0]["BookingToDate"]);
                string CheckOut = CheckOutDate.ToString("yyyy-MM-dd");
                int NoOfNights = Convert.ToInt16(DtCancellation.Rows[0]["AllNightsFlag"]);
                if (DtCancellation.Rows[0]["BookingAmount"] != null)
                    BookingAmount = Math.Round(Convert.ToDouble(DtCancellation.Rows[0]["BookingAmount"]), 2);

                HotelCode = Convert.ToString(DtPassenger.Rows[0]["ProductId"]);
                string Prefix = "", Name = "", SurName = "";

                string guid = Guid.NewGuid().ToString().Substring(0, 20);

                string AgentVoucher = string.Empty;
                string UniqueID = "";
                string hotelcode = "";
                int OnlineBookingDetailsId = (int)Convert.ToUInt32(dsratebreakup.Tables[0].Rows[0]["OnlineBookingDetailsId"]);
                DataRow[] DrRatebreakup1 = dsratebreakup.Tables[1].Select("OnlineBookingDetailsId='" + OnlineBookingDetailsId + "'");
                double TotalPrice = 0;
                double ExtraAdultPrice = 0;
                DataTable dtRatebreakup1 = null;
                double CurrencyFactor = 1;

                if(StaahCurrency.ToLower() != tokenData.SiteCurrency.ToLower())
                {
                    CurrencyFactor = _commonMethod.GetROEByCurrency(StaahCurrency, tokenData.dtROE);
                }

                BookingAmount = BookingAmount / CurrencyFactor;
                ExtraAdultPrice = Math.Round(Convert.ToDouble(ExtraAdult), 2) / CurrencyFactor;
                TotalPrice = Math.Round(BookingAmount, 2);
                Double BookingAmount1 = 0;
                BookingAmount1 = Math.Round(BookingAmount - ExtraAdultPrice, 2);

                int NoOfAdult = Convert.ToInt32((dsratebreakup.Tables[0].Rows[0]["NoOfAdult"]));
                int NoOfChild = Convert.ToInt32((dsratebreakup.Tables[0].Rows[0]["NoOfChild"]));
                int totalpax = NoOfAdult + NoOfChild;
                string PassengerName = Convert.ToString(DtPassenger.Rows[0]["Name"]);//Convert.ToString(dsratebreakup.Tables[0].Rows[0]["Name"]);
                string[] splitpassenger = PassengerName.Split(new string[] { "." }, StringSplitOptions.None);
                string prefix = splitpassenger[0].ToString();
                string[] Name1 = splitpassenger[1].Split(new string[] { " " }, StringSplitOptions.None);
                string name = Name1[0].ToString();
                string Surname = Name1[1].ToString();
                string email = Convert.ToString(dsratebreakup.Tables[0].Rows[0]["EmailId"]);

                double TotalPriceNew = 0;
                for (int kk = 0; kk < dsratebreakup.Tables[1].Rows.Count; kk++)
                {
                    double Rate = Convert.ToDouble(dsratebreakup.Tables[1].Rows[kk]["rate"]);
                    TotalPriceNew = TotalPriceNew + Rate;
                }

                #region Reservation

                List<Staah.StaahBookingRequest.Reservation> reservationList = new List<Staah.StaahBookingRequest.Reservation>();

                Staah.StaahBookingRequest.Reservation reservation = new Staah.StaahBookingRequest.Reservation();
                reservation.reservation_datetime = Convert.ToDateTime(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss"));
                reservation.propertyname = Convert.ToString(dsratebreakup.Tables[0].Rows[0]["HotelName"]);
                reservation.reservation_id = AgentVoucherNo;
                reservation.payment_required = "";
                reservation.payment_type = "Channel Collect";
                reservation.commissionamount = "0.00";
                reservation.discountamount = "0.00";
                reservation.deposit = Convert.ToString(Math.Round(Convert.ToDouble(TotalPriceNew), 2));
                reservation.totalamountaftertax = Convert.ToString(Math.Round(Convert.ToDouble(TotalPriceNew), 2));
                reservation.totaltax = "0.00";
                reservation.currencycode = StaahCurrency;
                reservation.status = "Cancel";

                #region customer

                Staah.StaahBookingRequest.Customer customer = new Staah.StaahBookingRequest.Customer();
                for (int p = 0; p < DtPassenger.Rows.Count; p++)
                {
                    //if (Convert.ToString(DtPassenger.Rows[p]["IsLead"]) == "1")
                    {
                        customer.address = "";
                        customer.city = "";
                        customer.country = Convert.ToString(dsratebreakup.Tables[0].Rows[0]["ClientNatinality"]);
                        customer.email = email;
                        customer.salutation = prefix;
                        customer.first_name = name;
                        customer.last_name = Surname;
                        customer.remarks = "";
                        customer.telephone = Convert.ToString(dsratebreakup.Tables[0].Rows[0]["MobileNo"]);
                        customer.zip = "";
                        reservation.customer = customer;
                        break;

                    }
                }

                #endregion


                #region Paymentcarddetail

                #region APISO VC

                //int usercartdetailsid = Convert.ToInt32(DtCancellation.Rows[0]["UserCartDetailId"]);
                ////get bookingid and detailsid

                //DBConnection db = new DBConnection();
                //SqlParameter[] param = new SqlParameter[]
                //{
                //    new SqlParameter("@usercartdetailsid",usercartdetailsid)
                //};

                //DataTable dtbookingdata = null; // db.GetDataTable("getApisobookingdata", param);

                //string Pan = "";
                //string CVV = "0", Expiry = "0";

                //if (dtbookingdata != null)
                //{
                //    // GetApisoServiceDatatoCreateVCN("", Convert.ToInt32(dtbookingdata.Rows[0]["OnlineBookingMasterId"]), Convert.ToInt32(dtbookingdata.Rows[0]["OnlineBookingDetailsId"]), ref Pan, ref CVV, ref Expiry);
                //    //PANN = Pan;
                //}
                #endregion

                //Staah.StaahBookingRequest.Paymentcarddetail Paymentcarddetail = new Staah.StaahBookingRequest.Paymentcarddetail();
                //Paymentcarddetail.CardHolderName = "Test Staah";
                //Paymentcarddetail.CardType = "MC";
                //Paymentcarddetail.ExpireDate = "07/2022";
                //Paymentcarddetail.CardNumber = "41111111111111111";
                //Paymentcarddetail.cvv = "123";
                #endregion

                //reservation.paymentcarddetail = Paymentcarddetail;

                double totalprice = 0;
                double PerDayAmount = 0;

                #region Staah Room

                List<Staah.StaahBookingRequest.Room> RoomRates1 = new List<Staah.StaahBookingRequest.Room>();
                //for (int Roomid = 0; Roomid < NoOfRooms; Roomid++)
                {
                    Staah.StaahBookingRequest.Room RoomRate1 = new Staah.StaahBookingRequest.Room();

                    RoomRate1.arrival_date = CheckIn;
                    RoomRate1.departure_date = CheckOut;
                    RoomRate1.room_id = RoomId;
                    RoomRate1.room_name = RoomName;

                    List<Staah.StaahBookingRequest.Price> PriceList = new List<Staah.StaahBookingRequest.Price>();
                    // string ExadultPrice1 = Convert.ToString(Convert.ToDouble(ExtraAdult) / CurrencyFactor);
                    string ExA_pricenew = Convert.ToString(Math.Round(Convert.ToDouble(ExtraAdultPrice), 2) / NoOfNights);

                    for (int n = 0; n < NoOfNights; n++)
                    {
                        Staah.StaahBookingRequest.Price Price = new Staah.StaahBookingRequest.Price();
                        Price.date = Convert.ToDateTime(CheckIn).AddDays(n).ToString("yyyy-MM-dd");
                        string Date_new = Convert.ToDateTime(CheckIn).AddDays(n).ToString("yyyy-MM-dd");

                        Price.rate_id = RateId;
                        DataRow[] DRRateBreakup = dsratebreakup.Tables[1].Select("Date='" + Date_new + "'");
                        for (int i = 0; i < DRRateBreakup.Length; i++)
                        {
                            string ratetype = Convert.ToString(DRRateBreakup[i]["RateType"]);

                            if (ratetype == "A Rate")// (RateType.ToLower() == "amountaftertax")
                            {
                                //string[] priceth = BookingAmount;
                                //PerDayAmount = BookingAmount1 / NoOfNights;
                                PerDayAmount = Convert.ToDouble(DRRateBreakup[i]["rate"]);

                                //TotalPriceNew = TotalPriceNew + PerDayAmount;

                                string pricenew = Convert.ToString(Math.Round(Convert.ToDouble(PerDayAmount), 2));
                                Price.amountaftertax = pricenew;
                                totalprice = totalprice + Math.Round(Convert.ToDouble(pricenew), 2);
                            }

                            if (ratetype != "A Rate")// (ExA_pricenew != "0")
                            {
                                ExA_pricenew = Convert.ToString(Math.Round(Convert.ToDouble(DRRateBreakup[i]["rate"]), 2));
                                //TotalPriceNew = TotalPriceNew + ExA_pricenew;
                                Staah.StaahBookingRequest.ExtraGuests ExtraGuests = new Staah.StaahBookingRequest.ExtraGuests();
                                if (NoOfChild == 0)
                                {
                                    ExtraGuests.extraAdult = ExtraBedquantity;
                                }
                                else
                                {
                                    ExtraGuests.extraAdult = "0";
                                }
                                ExtraGuests.extraChild = Convert.ToString(NoOfChild);
                                if (NoOfChild == 0)
                                {
                                    ExtraGuests.extraAdultRate = ExA_pricenew;

                                }
                                else
                                {
                                    ExtraGuests.extraAdultRate = "0.00";
                                }
                                if (NoOfChild > 0)
                                {
                                    ExtraGuests.extraChildRate = ExA_pricenew;

                                }
                                else
                                {
                                    ExtraGuests.extraChildRate = "0.00";
                                }

                                //ExtraGuests.extraAdult = ExtraBedquantity;
                                // ExtraGuests.extraChild = "";
                                // ExtraGuests.extraAdultRate = ExA_pricenew;
                                // ExtraGuests.extraChildRate = "0.00";
                                Price.extraGuests = ExtraGuests;
                            }
                        }
                        PriceList.Add(Price);
                        RoomRate1.price = PriceList;
                    }

                    if (RateType.ToLower() == "amountaftertax")
                    {
                        //RoomRate1.amountaftertax = Convert.ToString(Math.Round(TotalPrice, 2));
                        RoomRate1.amountaftertax = Convert.ToString(Math.Round(TotalPriceNew, 2));

                    }


                    for (int p = 0; p < DtPassenger.DefaultView.Count; p++)
                    {
                        string PassName = Convert.ToString(DtPassenger.DefaultView[p]["Name"]);
                        string[] splitpass = PassName.Split(new string[] { "." }, StringSplitOptions.None);
                        string prefixx = splitpassenger[0].ToString();
                        string[] Name2 = splitpassenger[1].Split(new string[] { " " }, StringSplitOptions.None);
                        string name1 = Name2[0].ToString();
                        string Surname1 = Name2[1].ToString();
                        RoomRate1.salutation = prefixx;
                        RoomRate1.first_name = name1;// Convert.ToString(DtPassenger.DefaultView[0]["FirstName"]);
                        RoomRate1.last_name = Surname1;// Convert.ToString(DtPassenger.DefaultView[0]["LastName"]);
                    }

                    RoomRate1.remarks = "";
                    List<Staah.StaahBookingRequest.GuestCount> guestCountlist = new List<Staah.StaahBookingRequest.GuestCount>();
                    Staah.StaahBookingRequest.GuestCount guestCount = new Staah.StaahBookingRequest.GuestCount();


                    guestCount.AgeQualifyingCode = "10";
                    guestCount.Count = Convert.ToString(NoOfAdult);
                    guestCountlist.Add(guestCount);

                    if (NoOfChild > 0)
                    {
                        Staah.StaahBookingRequest.GuestCount guestCount1 = new Staah.StaahBookingRequest.GuestCount();
                        guestCount1.AgeQualifyingCode = "8";
                        guestCount1.Count = Convert.ToString(NoOfChild);
                        guestCountlist.Add(guestCount1);
                    }

                    RoomRate1.GuestCount = guestCountlist;
                    RoomRates1.Add(RoomRate1);
                }
                #endregion

                reservation.room = RoomRates1;
                reservation.POS = "Hotelinx";
                reservationList.Add(reservation);

                Staah.StaahBookingRequest.Reservations reservations = new Staah.StaahBookingRequest.Reservations();
                reservations.reservation = reservationList;
                #endregion

                Staah.StaahBookingRequest.Root RootObject1 = new Staah.StaahBookingRequest.Root();
                RootObject1.propertyid = HotelCode;
                RootObject1.apikey = Password;
                RootObject1.action = "reservation_info";
                RootObject1.version = version;
                RootObject1.reservations = reservations;



                JsonSerializerSettings settings = new JsonSerializerSettings();
                settings.NullValueHandling = NullValueHandling.Ignore;
                settings.DefaultValueHandling = DefaultValueHandling.Ignore;
                string jsonBooking = JsonConvert.SerializeObject(RootObject1, settings);
                return jsonBooking;
                #endregion
            }
            catch (Exception ex)
            {
                _exception.ErrorNew(ex, ErrorLog._LogAPI.CancelBooking);
                return "";
            }
        }
    }
}
