using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using XConnect.CommonUtilities.Interface;
using XConnect.CommonUtilities;
using XConnect.DAL.Interface;
using XConnect.ErrorLogHandler.Interface;
using XConnect.Markup.Interface;
using XConnect.Models.Hotel;
using XConnect.Models;
using XConnect.Supplier.Travelclick.Interface;
using Options = XConnect.Models.Options;
using Microsoft.IdentityModel.Protocols;
using static Azure.Core.HttpHeader;
using System.Net;
using System.Xml;
using static XConnect.Supplier.Booking.Com.preebookrs;

namespace XConnect.Supplier.Travelclick
{
    public class TravelclickRepository : ITravelclick
    {
        private readonly ICommonMethod _commonMethod;
        private readonly IException _exception;
        private readonly IMarkup _markup;
        private readonly ISupplierRes _supplierRes;
        private readonly KeySettings _keySettings;
        private readonly IDLConnection _dLConnection;
        public TravelclickRepository(ICommonMethod commonMethod, IMarkup markup, ISupplierRes supplierRes, IException exception, IOptions<KeySettings> keySettings, IDLConnection dLConnection)
        {
            _commonMethod = commonMethod;
            _markup = markup;
            _supplierRes = supplierRes;
            _exception = exception;
            _keySettings = keySettings.Value;
            _dLConnection = dLConnection;
        }

        public void TravelClickRequestOnly(bool SearchByHotel, DataTable dtPreferredDataHotel, HotelSearchParameter SearchPara, List<SupplierDetails> TravelClickDetails, List<SupplierSearchConfig> TravelClickSearchConfig, ref ConcurrentBag<HotelX> AllTask, DataTable dtAgentDetail, DataSet dsAgentDetails, DataTable dtMarkupDetails, int Hit, DataTable OwnIDSList, DataRow[] commonCode)
        {
            try
            {
                DataRow[] drrayna = dtPreferredDataHotel.Select("ServiceName='travelclick'");
                string Ids = string.Empty;

                if (drrayna.Count() > 0)
                {
                    var paramValues1 = String.Join(",", drrayna.Select(x => String.Join(",", Convert.ToString(x.ItemArray[0]))));
                    Ids = Convert.ToString(paramValues1);
                }
                if (SearchPara.IsSearchFor.ToLower().IndexOf("preferred") >= 0 && drrayna.Count() == 0)
                    TravelClickSearchConfig = null;
                if (TravelClickSearchConfig != null)
                {
                    List<StarRating> lstRating = new List<StarRating>();
                    lstRating = _commonMethod.GetStarRatingStringList(SearchPara);

                    for (int s = 0; s < TravelClickSearchConfig.Count; s++)
                    {
                        //if (Convert.ToInt32(OTHSearchConfig[s]["Hit"]) != Hit)
                        //    continue;
                        if (Convert.ToInt32(TravelClickSearchConfig[s].CityID) == 0 || Convert.ToInt32(TravelClickSearchConfig[s].CityID) == SearchPara.CityID)//All City means 0
                        {
                            if (SearchPara.Filters.IsShowRooms != 1)
                            {

                                int starcount = lstRating.Count();
                                if (SearchByHotel)
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

                                        if (HitOth)
                                        {
                                            DataTable[] splittedtables = commonCode.AsEnumerable()
                                              .Select((row, index) => new { row, index })
                                              .GroupBy(x => x.index / TravelClickSearchConfig[s].Noofrecords)  // integer division, the fractional part is truncated
                                              .Select(g => g.Select(x => x.row).CopyToDataTable())
                                              .ToArray();
                                            for (int a = 0; a < splittedtables.Length; a++)
                                            {
                                                var paramValues1 = String.Join(",", splittedtables[a].Select().Select(x => String.Join(",", Convert.ToString(x.ItemArray[0]))));
                                                string IdsForSearch = Convert.ToString(paramValues1);
                                                int Str = lstRating[a].Star;
                                                HotelX xhotel = new HotelX();
                                                xhotel.StarRating = Str;
                                                xhotel.Supplier = "travelclick";
                                                xhotel.DebugeMode = true;
                                                xhotel.SearchTimeOut = Convert.ToInt32(TravelClickSearchConfig[s].HotelSearchTimeOut);
                                                xhotel.ProcessTimeout = Convert.ToInt32(TravelClickSearchConfig[s].HotelSearchTimeOut2);
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
                                                xhotel.ReturnRooms = 5; 
                                                xhotel.TechName = "Travelclick";
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
                                        xhotel.Supplier = "travelclick";
                                        xhotel.DebugeMode = true;
                                        xhotel.SearchTimeOut = Convert.ToInt32(TravelClickSearchConfig[s].HotelSearchTimeOut);
                                        xhotel.ProcessTimeout = Convert.ToInt32(TravelClickSearchConfig[s].HotelSearchTimeOut2);
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
                                        xhotel.ReturnRooms = 5;
                                        xhotel.TechName = "Travelclick";
                                        AllTask.Add(xhotel);
                                    }
                                }
                            }
                            else
                            {
                                bool HitOth = true;
                                if (commonCode == null)
                                    return;
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
                                                  .GroupBy(x => x.index / TravelClickSearchConfig[s].Noofrecords)  // integer division, the fractional part is truncated
                                                  .Select(g => g.Select(x => x.row).CopyToDataTable())
                                                  .ToArray();
                                for (int a = 0; a < splittedtables.Length; a++)
                                    if (HitOth)
                                    {
                                        string Request = "";


                                        //if (OwnIDSList.Columns.Contains("Ids"))
                                        {
                                            //if (OwnIDSList.Rows.Count > 0)
                                            //{
                                            //    Request = Convert.ToString(OwnIDSList.Rows[0]["Ids"]).Replace("'", "");
                                            //}


                                            //string[] ss = new string[0];
                                            //if (!string.IsNullOrEmpty(Request))
                                            //{
                                            //    ss = Request.Split(',');
                                            ////}
                                            //int Noofrecords = TravelClickSearchConfig[s].Noofrecords;
                                            //double count = ss.Count();
                                            //int NoOfDiv = Convert.ToInt32(Math.Ceiling(count / Convert.ToDouble(Noofrecords)));
                                            //List<List<XMLID_search>> lstxml = GetXMLIDString_New(NoOfDiv, Noofrecords, count, ss);
                                            var paramValues1 = String.Join(",", splittedtables[a].Select().Select(x => String.Join(",", Convert.ToString(x.ItemArray[0]))));
                                            string IdsForSearch = Convert.ToString(paramValues1);

                                            DataTable dttempRoomTable3 = new DataTable();
                                            dttempRoomTable3.Columns.Add("RoomNo");
                                            dttempRoomTable3.Columns.Add("NoOfAdult");
                                            dttempRoomTable3.Columns.Add("NoOfChild");
                                            dttempRoomTable3.Columns.Add("ChildAge1");
                                            dttempRoomTable3.Columns.Add("ChildAge2");
                                            string IncludeOnRequest3 = SearchPara.HotelType;
                                            if (IncludeOnRequest3.Trim().ToLower() == "available")
                                                IncludeOnRequest3 = "N";
                                            else
                                                IncludeOnRequest3 = "Y";
                                            for (int r = 0; r < SearchPara.Rooms.Count; r++)
                                            {
                                                DataRow dr = dttempRoomTable3.NewRow();
                                                dr["RoomNo"] = r + 1;
                                                dr["NoOfAdult"] = SearchPara.Rooms[r].NoofAdults;
                                                dr["NoOfChild"] = SearchPara.Rooms[r].NoofChild;
                                                dr["ChildAge1"] = SearchPara.Rooms[r].Child1Age;
                                                dr["ChildAge2"] = SearchPara.Rooms[r].Child2Age;
                                                dttempRoomTable3.Rows.Add(dr);
                                            }
                                            string Natinality3 = "";
                                            Natinality3 = SearchPara.Nationality;
                                            string citycode3 = Convert.ToString(SearchPara.CityID); //doc.SelectSingleNode("//HotelRef/@HotelCityCode").InnerText;
                                            string checkin3 = SearchPara.CheckInDate; //doc.SelectSingleNode("//StayDateRange/@Start").InnerText;
                                            string checkout3 = SearchPara.CheckOutDate; //doc.SelectSingleNode("//StayDateRange/@End").InnerText;

                                            string agent3 = "";
                                            if (dtAgentDetail != null && dtAgentDetail.Rows.Count > 0)
                                                agent3 = Convert.ToString(dtAgentDetail.Rows[0]["AgentID"]);

                                            string MinStar3 = "";
                                            string MaxStar3 = "";
                                            bool HitHR = true;

                                            if (SearchPara.Filters.IsShowRooms == 1)
                                            {
                                                MinStar3 = Convert.ToString(SearchPara.StarStart);
                                                MaxStar3 = Convert.ToString(SearchPara.StarEnd);
                                            }
                                            else
                                            {
                                                MinStar3 = Convert.ToString(SearchPara.StarStart);
                                                MaxStar3 = Convert.ToString(SearchPara.StarEnd);
                                            }

                                            if (MinStar3 == "")
                                                MinStar3 = "1";
                                            if (MaxStar3 == "")
                                                MaxStar3 = "5";

                                            SqlParameter[] para2 = new SqlParameter[]{
                                                         new SqlParameter("@CityId",citycode3),
                                                         new SqlParameter("@CheckInDate",checkin3.Substring(3, 2) + "/" + checkin3.Substring(0, 2) + "/" + checkin3.Substring(6, 4)),
                                                         new SqlParameter("@CheckOutDate",checkout3.Substring(3, 2) + "/" + checkout3.Substring(0, 2) + "/" + checkout3.Substring(6, 4)),
                                                         new SqlParameter("@RatingStartEnd",Convert.ToInt32(MinStar3)),
                                                         new SqlParameter("@RatingEnd",Convert.ToInt32(MaxStar3)),
                                                         new SqlParameter("@tempRM",dttempRoomTable3),
                                                         new SqlParameter("@PassangerNatinality",Natinality3),
                                                         new SqlParameter("@AgentId",agent3),
                                                         new SqlParameter("@HotelId",Ids),
                                                         new SqlParameter("@IsAvailable",1),//IncludeOnRequest2 == "N"?1:0),
                                                         new SqlParameter("@HotelIDs",IdsForSearch),
                                                         
                                                       //  new SqlParameter("@CompanyId",commonSearch.CompanyId)
                                                        };

                                            //System.Diagnostics.Stopwatch timerroom6 = new System.Diagnostics.Stopwatch();
                                            //timerroom6.Start();

                                            //DataSet ds3 = await db2.GetDataSetAsync_Timeout("GetOwnChannelManagerRates_Karnav_HotelRunner_Roomlist_testk", para2, cts.Token, item.SearchTimeOut).ConfigureAwait(false);
                                            //DataSet ds3 = await db2.GetDataSetAsync_Timeout("GetOwnChannelManagerRates_vishakha_Rategain_Roomlist_API_WithOutCityCode_MaxRoomCount", para2, cts.Token, item.SearchTimeOut).ConfigureAwait(false);

                                            DataSet ds3 = _dLConnection.GetDataSet_Limit_CM_2("GetChannelManager_TravelClick_Roomlist_API_WithOutCityCode_MaxRoomCount", para2, 1);
                                            if (ds3 != null && ds3.Tables.Count > 0 && ds3.Tables[0].Rows.Count > 0)
                                            {
                                                int NoofroomCount = Convert.ToInt32(ds3.Tables[0].Rows[0]["roomcount"]);
                                                NoofroomCount = NoofroomCount / 17;
                                                for (int Ro = 0; Ro < NoofroomCount + 1; Ro++) //Added by k2 26-26-2024 
                                                {
                                                    HotelX xhotel = new HotelX();

                                                    int Str = lstRating[a].Star;

                                                    //var paramValues1 = String.Join(",", splittedtables[a].Select().Select(x => String.Join(",", Convert.ToString(x.ItemArray[0]))));
                                                    ///string IdsForSearch = Convert.ToString(paramValues1);

                                                    if (Ro == 0)
                                                    {
                                                        xhotel.MinRoomCount = 1;
                                                        xhotel.MaxRoomCount = 17;
                                                    }
                                                    else
                                                    {
                                                        int minRoomC = Ro * 17;
                                                        xhotel.MinRoomCount = minRoomC + 1;
                                                        xhotel.MaxRoomCount = minRoomC + 17;
                                                    }

                                                    //for (int a = 0; a < NoOfDiv; a++)
                                                    {
                                                        //string IdsForSearch = string.Empty;
                                                        //IdsForSearch = Convert.ToString(GetHotelCodeByCOmma(lstxml[a]));

                                                        //HotelX xhotel = new HotelX();
                                                        xhotel.Supplier = "travelclick";
                                                        xhotel.DebugeMode = true;
                                                        xhotel.SearchTimeOut = Convert.ToInt32(TravelClickSearchConfig[s].HotelSearchTimeOut);
                                                        xhotel.ProcessTimeout = Convert.ToInt32(TravelClickSearchConfig[s].HotelSearchTimeOut2);
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
                                                        xhotel.ReturnRooms = 5;
                                                        xhotel.TechName = "Travelclick";
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
            catch (Exception ex)
            {
                //ExceptionManager.LogException(ex);
                _exception.Error(ex);
            }
        }

        public async Task<List<Options>> Travelclick_HotelSearchWithRooms(HotelX Item, CommonSearch commonSearch, DataSet SiteminderResponse, HotelSearchParameter SearchParam, DataTable dtAgentDetail, DataTable dtSuplierMarkup, DataTable dtMarkupDetails, string currency, double currencyfactor, DataTable dtROE, Int32 processTimeout, string ResProccessTimerLogActive, string ResProccessTimeValueforSaveLog, string StoreSupplierResErrorLog)
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
                CutOffDays = _commonMethod.GetCutOffDays(commonSearch.dtSuplierMarkup, "travelclick");

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
                        //dtSuplierMarkup.DefaultView.RowFilter = "SupplierName='travelclick'";
                        DataRow[] drSupplierMarkup = dtSuplierMarkup.Select("SupplierName='travelclick'");
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
                        _markup.getAgetMarkupSetting("travelclick", Convert.ToString(giitaCode), commonSearch.dtSuplierMarkup, commonSearch.dtMarkupDetails, ref AgentPercentage, ref AgentMarkupAmount, ref blnIsShowSupplier, ref isPerce, Convert.ToInt32(agent), Convert.ToInt32(Rating), SearchParam.CityID, SearchParam.CountryID.ToString(), SearchParam.CheckInDate);
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
                            drXmlHotel["SupplierName"] = "travelclick";
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
                                CurrencyCode = Convert.ToString(drRoomCategories[rc]["CurrencyCode"]);
                                string RaynaRoomTypeNameOriginal = Convert.ToString(drRoomCategories[rc]["RaynaRoomTypeNameOriginal"]);
                                string checkin = SearchParam.CheckInDate; //doc.SelectSingleNode("//StayDateRange/@Start").InnerText;
                                                                          //@newk
                                                                          // string RoomTypeID_New = Convert.ToString(drRoomCategories[rc]["RoomTypeId"]) + "$$$" + RatePlanCode + "$$$" + CurrencyCode + "$$$" + ItemSurchargePrice + "$$$" + ExtrabedQuantity + "$$$" + CountryId + "$$$" + RaynaRoomTypeNameOriginal;
                                                                          //string RoomTypeID_New = Convert.ToString(drRoomCategories[rc]["RoomTypeId"]) + "$$$" + RatePlanCode + "$$$" + CurrencyCode + "$$$" +  CountryId;
                                                                          //@newk
                                                                          //DataRow[] DTdSurchargerdate = SiteminderResponse.Tables[12].Select("RoomTypeID='" + RoomTypeID + "' and RaynaId=" + ItemCode + " and BoardTypeID='" + BoardTypeID + "' and RoomNo=" + Convert.ToDouble(drRoomCategories[rc]["RoomNo"]));
                                DataRow[] DTdSurchargerdate = SiteminderResponse.Tables[12].Select("RoomTypeID='" + RoomTypeID + "' and XMlRateId=" + XMlRateId + " and RaynaId=" + ItemCode + " and BoardTypeID='" + BoardTypeID + "' and RoomNo=" + Convert.ToDouble(drRoomCategories[rc]["RoomNo"])); //Added 22-feb-2025

                                double ItemSurchargePrice = 0;
                                int ExtrabedQuantity = 0;
                                string ExtraPerson = string.Empty;
                                if (DTdSurchargerdate.Length > 0)
                                {
                                    for (int su = 0; su < DTdSurchargerdate.GetLength(0); su++)
                                    {
                                        ExtraPerson = Convert.ToString(DTdSurchargerdate[su]["SurchargeName"]);
                                        ItemSurchargePrice += Math.Round(Convert.ToDouble(DTdSurchargerdate[su]["Price"]), 2) * ROE;
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
                                string RoomTypeIdNew = Convert.ToString(drRoomCategories[rc]["RoomTypeID"]) + "$$$" + RatePlanCode + "$$$" + CurrencyCode + "$$$" + ItemSurchargePrice + "$$$" + ExtrabedQuantity + "$$$" + CountryId + "$$$" + RaynaRoomTypeNameOriginal + "$$$" + RateBreakPrice + "$$$" + BoardTypeID; //Added 22-feb-2025

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
                                    drHotelCancellation["SupplierName"] = "travelclick";
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
                                            drHotelCancellation["SupplierName"] = "travelclick";
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
                                        drHotelRoomRateBreackUp["SupplierName"] = "travelclick";
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
                                        drHotelRoomRateBreackUp["SupplierName"] = "travelclick";
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
                                    b2cItemPriceMarkup = Math.Round((Math.Round((ItemPrice * AgentPercentage), 2) / 100), 2);
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
                                    drhotelRoom["SupplierName"] = "travelclick";
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
                            drXmlHotel["SupplierName"] = "travelclick";
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
                    await _exception.Error(ex, SearchParam.SearchKey);
                    return options;
                }
                finally
                {
                    #region ResProcessTimer
                    try
                    {
                        if (SearchParam != null && ResProccessTimerLogActive == "1" && timerroomx.ElapsedMilliseconds > Convert.ToInt32(ResProccessTimeValueforSaveLog))
                        {
                            timerroomx.Stop();
                            SearchParam.SupplierTime += ",|Finish:travelclick Process Response  " + "|Time :" + timerroomx.Elapsed.ToString();
                            _= Task.Run(() => _commonMethod.FireAway_SQL_ResPorcessTimeLog("", timerroomx.Elapsed.ToString(), "travelclick", SearchParam.SearchKey, 1, SearchParam.AgentID));
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

        public string Get_TravelClick_BookingRequest(DataSet dsCartDetails, DataTable dtPassengers, DataRow[] TravelClickDetails, string UniqueNo, int i, int jj, string ReferenceNo)
        {
            try
            {
                #region Booking RQ
                string CheckIn = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["CheckInDate"]);
                CheckIn = CheckIn.Substring(0, 4) + "-" + CheckIn.Substring(4, 2) + "-" + CheckIn.Substring(6, 2);
                string CheckOut = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["CheckOutDate"]);
                CheckOut = CheckOut.Substring(0, 4) + "-" + CheckOut.Substring(4, 2) + "-" + CheckOut.Substring(6, 2);
                string PropertyId = Convert.ToString(dsCartDetails.Tables[1].DefaultView[jj]["ContractID"]);
                string Meal = Convert.ToString(dsCartDetails.Tables[1].DefaultView[jj]["MealPlanCode"]);

                //string currencycode = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["SupplierCurrency"]);
                string HotelCode = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["XMLHotelId"]);

                int NoOfNights = Convert.ToInt32(dsCartDetails.Tables[0].Rows[i]["NoOfNight"]);
                string guid = Guid.NewGuid().ToString().Substring(0, 20);

                string SPUI = Convert.ToString(dsCartDetails.Tables[1].DefaultView[jj]["spui"]);
                string Roomidsplit = Convert.ToString(dsCartDetails.Tables[1].DefaultView[jj]["RoomTypeId"]);
                string[] Roomids = Roomidsplit.Split(new String[] { "$$$" }, StringSplitOptions.None);
                string Roomid = Roomids[0].ToString();
                string RatePlanCode = Roomids[1].ToString();
                //string RoomTypeCode = Roomids[2].ToString();
                string currencycode = Roomids[2].ToString();
                string RateBreakUp = Roomids[7].ToString();  //@rategain

                string countrycode = "USD";//Convert.ToString(dtPassengers.DefaultView[0]["CountryCode"]);
                string country = "IND"; //Convert.ToString(dtPassengers.DefaultView[0]["Nationality "]);
                string usercartid = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["UserCartID"]);
                int UserCartDetailId = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[jj]["UserCartDetailId"]);
                int TotalUnit = Convert.ToInt16(dsCartDetails.Tables[1].DefaultView[jj]["NoOfAdults"]) + Convert.ToInt16(dsCartDetails.Tables[1].DefaultView[jj]["NoOfChild"]);

                string UserID = Convert.ToString(TravelClickDetails[0]["UserName"]);
                string Password = Convert.ToString(TravelClickDetails[0]["Password"]);
                string Prefix = "", Name = "", SurName = "", phonenumber = "", email = "", address = "", city = "", pincode = "";
                dtPassengers.DefaultView.RowFilter = "PickUp='" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[jj]["RoomTypeID"]) + "' and RoomNo='" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[jj]["RoomNumber"] + "'");

                ReferenceNo = ReferenceNo + "_" + UserCartDetailId;
                DataTable dtInrCurrency = _commonMethod.GetCountryCurrency(currencycode);
                double CurrencyFactortravelclick = Convert.ToDouble(dtInrCurrency.Rows[0]["AmtToExch"]);
                double companybuyingprice = Math.Round(Convert.ToDouble(dsCartDetails.Tables[1].DefaultView[jj]["CompanyBuyingPrice"]) / CurrencyFactortravelclick, 2);
                string requestTime = DateTime.Now.ToString("yyyy-MM-ddThh:mm:ss");
                //DataRow[] drHotelRateRow = (Genting_AllRoomRate.Tables["Row"].Select("Row_Id='" + x + "'"));
                //string RoomType = Convert.ToString(drHotelRateRow[0]["PromotionCode"]);
                dtPassengers.DefaultView.RowFilter = "PickUp='" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[jj]["RoomTypeID"]) + "' and RoomNo='" + Convert.ToString(dsCartDetails.Tables[1].DefaultView[jj]["RoomNumber"] + "'");

                //DataTable dtRateBreakup = ((DataTable)HttpContext.Current.Session["AddCartHotelRateBreakeUp"]);
                //DataRow[] drrRateBreakup = dtRateBreakup.Select("CartId='" + Convert.ToInt32(dsCartDetails.Tables[0].Rows[i]["UserCartId"]) + "'and RoomTypeId='" + Roomidsplit + "' ");
                //DateTime EffectiveDate = Convert.ToDateTime(drrRateBreakup[0]["Date"]).Date;
                //DateTime EffectiveDate = Convert.ToDateTime(drrRateBreakup[0]["Date"]).Date;
                //DateTime ExpireDate = EffectiveDate.AddDays(1);
                //DateTime ExpireDate = EffectiveDate.AddDays(1);
                string Guestcount = Convert.ToString(dsCartDetails.Tables[1].DefaultView[jj]["NoOfAdults"]);
                int Child = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[jj]["NoOfChild"]);
                double perDayAmount = 0.00;
                double totalAmount = 0;
                //DataTable roomInformation = new DataTable();
                DataRow[] CartDetails = dsCartDetails.Tables[1].Select("UserCartDetailId=" + UserCartDetailId + "");
                string BoardBasis = Convert.ToString(CartDetails[0]["MealPlanCode"]);
                //string BookUrl = Convert.ToString(TravelClickDetails[0]["WebSiteURL"]);
                string travelcliclusername = Convert.ToString(TravelClickDetails[0]["UserName"]);
                string Travelclickpassword = Convert.ToString(TravelClickDetails[0]["Password"]);

                //DataRow[] roomRatePlanWise = roomInformation.Select("BoardBasis='" + Convert.ToString(CartDetails[0]["MealPlanCode"]) + "'");
                // DataTable roomInformation = new DataTable();
                //DataSet hotelInformation = new DataSet();
                //if (hotelInformation.Tables.Contains("Table3") && hotelInformation.Tables["Table3"].Rows.Count > 0)
                //  roomInformation = hotelInformation.Tables["Table3"].DefaultView.ToTable();
                //DataRow[] roomRatePlanWise = roomInformation.Select("BoardBasis='" + Convert.ToString(CartDetails[0]["MealPlanCode"]) + "'");


                //ReferenceNo = ReferenceNo + "|" + UserCartDetailId;
                Int64 AgentVoucherNo = _commonMethod.GetRandomNo();
                StringBuilder BookRQ = new StringBuilder();
                BookRQ.Append("<OTA_HotelResNotifRQ  Version=\"1.0\"  TimeStamp=\"" + String.Format("{0:s}", DateTime.Now) + "\" EchoToken=\"" + guid + "\"  xmlns=\"http://www.opentravel.org/OTA/2003/05\">");
                BookRQ.Append("<POS>");
                BookRQ.Append("<Source>");
                BookRQ.Append("<BookingChannel Type=\"7\">");
                //BookRQ.Append("<CompanyName Code=\"" + XMLHotelBookingCommon.Travelclickpassword + "\">" + XMLHotelBookingCommon.travelcliclusername + "</CompanyName>");
                //BookRQ.Append("<CompanyName Code=\"" + Convert.ToString(ConfigurationManager.AppSettings["Travelclickpassword"]) + "\">" + Convert.ToString(ConfigurationManager.AppSettings["travelcliclusername"]) + "</CompanyName>");
                BookRQ.Append("<CompanyName Code=\"" + Travelclickpassword + "\">" + travelcliclusername + "</CompanyName>");//Added ON 29-SEP 2025

                BookRQ.Append("</BookingChannel>");
                BookRQ.Append("</Source>");
                BookRQ.Append("</POS>");
                BookRQ.Append("<HotelReservations>");
                BookRQ.Append("<HotelReservation ResStatus =\"Commit\" CreateDateTime=\"" + requestTime + "" + "\">");
                BookRQ.Append("<ResGlobalInfo>");
                BookRQ.Append("<HotelReservationIDs>");
                BookRQ.Append("<HotelReservationID  ResID_Type=\"13\"  ResID_Value=\"" + ReferenceNo + "\"/>");
                BookRQ.Append("</HotelReservationIDs>");
                BookRQ.Append("</ResGlobalInfo>");

                BookRQ.Append("<ResGuests>");
                for (int d = 0; d < dtPassengers.DefaultView.Count; d++)
                {
                    Prefix = Convert.ToString(dtPassengers.DefaultView[d]["Prefix"]).Replace(".", "");
                    Name = Convert.ToString(dtPassengers.DefaultView[d]["FirstName"]).Replace(".", "");
                    SurName = Convert.ToString(dtPassengers.DefaultView[d]["LastName"]).Replace(".", "");
                    phonenumber = ""; //Convert.ToString(dtPassengers.DefaultView[0]["Mobile "]).Replace(".", "");
                    email = Convert.ToString(dtPassengers.DefaultView[0]["Email"]);
                    address = ""; //Convert.ToString(dtPassengers.DefaultView[0]["Address"]);
                    city = "";//Convert.ToString(dtPassengers.DefaultView[0]["city"]);
                    pincode = "";// Convert.ToString(dtPassengers.DefaultView[0]["PostCode "]);
                    if (d == 0)
                    {


                        BookRQ.Append("<ResGuest   ResGuestRPH=\"0\">");
                        BookRQ.Append("<Profiles>");
                        BookRQ.Append("<ProfileInfo>");
                        BookRQ.Append("<Profile>");
                        BookRQ.Append("<Customer>");
                        BookRQ.Append("<PersonName>");
                        BookRQ.Append("<GivenName>" + Name + "</GivenName>");
                        BookRQ.Append("<Surname>" + SurName + "</Surname>");
                        BookRQ.Append("</PersonName>");
                        //BookRQ.Append("<Telephone>" + 15555555555 + "</Telephone>");
                        //BookRQ.Append("<Email>" + email + "</Email>");
                        //BookRQ.Append("<Address>");
                        //BookRQ.Append("<AddressLine>" + "1 Deep Ln" + "</AddressLine>");
                        //BookRQ.Append("<CityName>" + "Innsmouth" + "</CityName>");
                        //BookRQ.Append("<StateProv>" + "MA" + "</StateProv>");
                        //BookRQ.Append("<CountryName>" + "US" + "</CountryName>");
                        //BookRQ.Append("<PostalCode>" + 01966 + "</PostalCode>");
                        //BookRQ.Append("</Address>");
                        BookRQ.Append("</Customer>");
                        BookRQ.Append("</Profile>");
                        BookRQ.Append("</ProfileInfo>");
                        BookRQ.Append("</Profiles>");
                        BookRQ.Append("<SpecialRequests>");
                        BookRQ.Append("<SpecialRequest>");
                        BookRQ.Append("<Text>" + "No Comment" + "</Text>");
                        BookRQ.Append("</SpecialRequest>");
                        BookRQ.Append("</SpecialRequests>");
                        BookRQ.Append("<Comments>");
                        BookRQ.Append("<Comment>");
                        //string comment = TotalUnit.ToString() + "Comment";
                        BookRQ.Append("<Text>" + "no comment" + "</Text>");
                        BookRQ.Append("</Comment>");
                        BookRQ.Append("</Comments>");
                        BookRQ.Append("</ResGuest>");
                    }

                }
                BookRQ.Append("</ResGuests>");
                BookRQ.Append("<RoomStays>");
                BookRQ.Append("<RoomStay   IndexNumber=\"1\"  PromotionCode=\"" + "AAA" + "\">");
                BookRQ.Append("<BasicPropertyInfo     HotelCode=\"" + HotelCode + "\" />");
                BookRQ.Append("<TimeSpan   Start=\"" + CheckIn + "\" End=\"" + CheckOut + "\"/>");
                //BookRQ.Append("<Guarantee>");
                //BookRQ.Append("<GuaranteesAccepted>");
                //BookRQ.Append("<GuaranteeAccepted>");
                //BookRQ.Append("<PaymentCard  ExpireDate=\"0424\"  >");
                //BookRQ.Append("<CardType   Code=\"" + "VI" + "\" />");
                //BookRQ.Append("<CardNumber>");
                //BookRQ.Append("<PlainText>" + 1234123412341234 + "</PlainText>");
                //BookRQ.Append("</CardNumber>");
                //// BookRQ.Append("<CardHolderName>" + XMLHotelBookingCommon.TravelclickCardHolderName + "</CardHolderName>");
                //BookRQ.Append("<CardHolderName>" + Convert.ToString(ConfigurationManager.AppSettings["TravelclickCardHolderName"]) + "</CardHolderName>");

                //BookRQ.Append("<Address>");
                //BookRQ.Append("<AddressLine>" + "1 Deep Ln" + "</AddressLine>");
                //BookRQ.Append("<CityName>" + "Innsmouth" + "</CityName>");
                //BookRQ.Append("<StateProv>" + "MA" + "</StateProv>");
                //BookRQ.Append("<CountryName>" + "US" + "</CountryName>");
                //BookRQ.Append("<PostalCode>" + 01966 + "</PostalCode>");
                //BookRQ.Append("</Address>");
                //BookRQ.Append("</PaymentCard>");
                //BookRQ.Append("</GuaranteeAccepted>");
                //BookRQ.Append("</GuaranteesAccepted>");
                //BookRQ.Append("</Guarantee>");
                BookRQ.Append("<GuestCounts>");
                BookRQ.Append("<GuestCount  Count=\"" + Guestcount + "\" AgeQualifyingCode=\"10\"/>");
                if (Child > 0)
                {
                    BookRQ.Append("<GuestCount Count=\"" + Child + "\" AgeQualifyingCode=\"8\"/>");

                }
                BookRQ.Append("</GuestCounts>");
                BookRQ.Append("<Total CurrencyCode=\"" + currencycode + "\" AmountBeforeTax=\"" + companybuyingprice + "\" AmountAfterTax=\"" + companybuyingprice + "\"/>");
                BookRQ.Append("<Comments>");
                BookRQ.Append("<Comment>");
                //ookRQ.Append("<Text>" + "one-room-two-adults-one-child" + "</Text>");
                //BookRQ.Append("<Text>" + "two-rooms" + "</Text>");
                BookRQ.Append("<Text>" + "one-room-different-rates" + "</Text>");
                BookRQ.Append("</Comment>");
                BookRQ.Append("</Comments>");
                BookRQ.Append("<RoomRates>");
                BookRQ.Append("<RoomRate  NumberOfUnits=\"1\" RoomTypeCode=\"" + Roomid + "\" RatePlanCode=\"" + RatePlanCode + "\">");
                BookRQ.Append("<Rates>");

                //for (int rr = 0; rr < roomRatePlanWise.GetLength(0); rr++)
                {
                    //string EffectiveDate = Convert.ToDateTime(roomRatePlanWise[rr]["checkindate"]).ToString("yyyy-MM-dd");

                    //@comment
                    // DataTable dtRateBreakup = ((DataTable)HttpContext.Current.Session["AddCartHotelRateBreakeUp"]);
                    //  DataRow[] drrRateBreakup = dtRateBreakup.Select("CartId='" + Convert.ToInt32(dsCartDetails.Tables[0].Rows[i]["UserCartId"]) + "'and RoomTypeId='" + Roomidsplit + "' ");
                    //------

                    //DataTable dtRateBreakup = ((DataTable)HttpContext.Current.Session["AddCartHotelRateBreakeUp"]);
                    //    DataRow[] drrRateBreakup = dtRateBreakup.Select("RoomNo='" + (roomNo + 1) + "' ");
                    //    DataTable dtrRateBreakup1 = drrRateBreakup.CopyToDataTable();

                    // DateTime EffectiveDate = Convert.ToDateTime(drrRateBreakup[0]["Date"]).Date;

                    //decimal perDayAmount = 0.00m;

                    //BookingRQ.Append("<stays>");

                    //--------------------
                    //decimal perDayAmount = 0.00m;

                    //for (int R = 0; R < drrRateBreakup.Length; R++)
                    //{
                    //    DateTime EffectiveDate = Convert.ToDateTime(drrRateBreakup[R]["Date"]).Date;
                    //    DateTime ExpireDate = EffectiveDate.AddDays(1);
                    //    // string EffectiveDate1 = Convert.ToString(EffectiveDate);
                    //    string EffectiveDate1 = EffectiveDate.ToString("yyyy-MM-dd");
                    //    // string ExpireDate1 = Convert.ToString(ExpireDate);
                    //    string ExpireDate1 = ExpireDate.ToString("yyyy-MM-dd");

                    //    perDayAmount = Math.Round(Convert.ToDecimal(drrRateBreakup[R]["CompanyRate"]), 2);
                    //    // string RatePlanCode1 = Roomids1[1].ToString();

                    //    BookRQ.Append("<Rate RoomPricingType=\"Per Night\"  EffectiveDate=\"" + EffectiveDate1 + "\" ExpireDate=\"" + ExpireDate1 + "\">");
                    //    BookRQ.Append("<Base AmountAfterTax=\"" + companybuyingprice + "\"/>");
                    //    BookRQ.Append("</Rate>");
                    //}

                    //@travelclickchange
                    //string[] RateBreakUpRow = RateBreakUp.Split('#');

                    //for (int R = 0; R < RateBreakUpRow.Length; R++)
                    //{

                    //    DateTime EffectiveDate = Convert.ToDateTime(CheckIn).Date;
                    //    DateTime ExpireDate = EffectiveDate.AddDays(1);

                    //    perDayAmount = Math.Round(Convert.ToDecimal(RateBreakUpRow[R]), 2);

                    //    string EffectiveDate1 = Convert.ToString(EffectiveDate.AddDays(R));
                    //    string ExpireDate2 = Convert.ToString(ExpireDate);

                    //    //double Rate = Math.Round(Convert.ToDouble(drrRateBreakup[R]["Rate"]) / CurrencyFactorHotelRunner, 2);
                    //    //double Rate = Math.Round(Convert.ToDouble(drrRateBreakup[R]["CompanyRate"]), 2);
                    //    EffectiveDate1 = EffectiveDate.ToString("yyyy-MM-dd");
                    //    ExpireDate2 = EffectiveDate1;

                    //    // DateTime date = DateTime.ParseExact(EffectiveDate1, "MM/dd/yyyy h-m-s t", System.Globalization.CultureInfo.InvariantCulture);
                    //    // ExpireDate2 = date.ToString("yyyy-MM-dd");


                    //    BookRQ.Append("<Rate RoomPricingType=\"Per Night\"  EffectiveDate=\"" + EffectiveDate1 + "\" ExpireDate=\"" + ExpireDate2 + "\">");
                    //    BookRQ.Append("<Base AmountAfterTax=\"" + companybuyingprice + "\"/>");
                    //    BookRQ.Append("</Rate>");
                    //}

                    //------------------
                    //---------------
                    string[] RateBreakUpRow = RateBreakUp.Split('#');
                    DateTime EffectiveDate = Convert.ToDateTime(CheckIn).Date;

                    for (int R = 0; R < RateBreakUpRow.Length; R++)
                    {
                        DateTime ExpireDate = EffectiveDate.AddDays(1);
                        perDayAmount = Math.Round(Convert.ToDouble(RateBreakUpRow[R]), 2);

                        // BookRQ.Append("<Rate RoomPricingType=\"Per Night\"  EffectiveDate=\"" + EffectiveDate + "\" ExpireDate=\"" + ExpireDate + "\">");
                        BookRQ.Append("<Rate RoomPricingType=\"Per Night\"  EffectiveDate=\"" + EffectiveDate.ToString("yyyy-MM-dd") + "\" ExpireDate=\"" + ExpireDate.ToString("yyyy-MM-dd") + "\">");
                        BookRQ.Append("<Base AmountAfterTax=\"" + perDayAmount + "\"/>");
                        BookRQ.Append("</Rate>");
                        EffectiveDate = EffectiveDate.AddDays(1);
                    }
                    //------------------

                    totalAmount = totalAmount + perDayAmount;
                }
                //BookRQ.Append("<Rate  RoomPricingType=\"Per Night\"  EffectiveDate=\"" + CheckIn + "\" ExpireDate=\"" + CheckOut + "\" / >");
                //    BookRQ.Append("<Base   AmountBeforeTax=\"" + Convert.ToString(roomRatePlanWise[rr]["Price"]) + "\"/>");
                //    BookRQ.Append("</Rate>");
                BookRQ.Append("</Rates>");
                BookRQ.Append("</RoomRate>");
                BookRQ.Append("</RoomRates>");
                BookRQ.Append("<ResGuestRPHs>" + 0 + "</ResGuestRPHs>");
                BookRQ.Append("</RoomStay>");
                BookRQ.Append("</RoomStays>");
                BookRQ.Append("</HotelReservation>");
                BookRQ.Append("</HotelReservations>");
                BookRQ.Append("</OTA_HotelResNotifRQ>");
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

        private static XmlDocument CreateSoapEnvelope_TravelClick(string xml)
        {
            try
            {
                XmlDocument soapEnvelop = new XmlDocument();
                soapEnvelop.LoadXml(xml);
                return soapEnvelop;
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }
        private static HttpWebRequest CreateWebRequest_travelclick(string url)
        {
            //HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(url);
            //// webRequest.Credentials = new NetworkCredential(userName, password);
            //// webRequest.Headers.Add("SOAPAction", action);
            //webRequest.ContentType = "text/xml;charset=\"utf-8\"";
            //webRequest.Accept = "text/xml";
            //webRequest.Method = "POST";
            //return webRequest;

            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(url);
            string UserNAme = "WithinEarth";
            string Password = "withinearth@2022";
            webRequest.Credentials = new NetworkCredential(UserNAme, Password);
            // webRequest.Headers.Add("SOAPAction", action);
            webRequest.ContentType = "text/xml;charset=\"utf-8\"";
            webRequest.Accept = "text/xml";
            webRequest.Method = "POST";
            return webRequest;
        }

        private static void InsertSoapEnvelopeIntoWebRequest_travelclick(XmlDocument soapEnvelopeXml, HttpWebRequest webRequest)
        {
            using (Stream stream = webRequest.GetRequestStream())
            {
                soapEnvelopeXml.Save(stream);
            }
        }
        public string GetTravelClick_SupplierResponse(ref string Error, string _url, string Request)
        {
            try
            {
                XmlDocument soapEnvelopeXml = CreateSoapEnvelope_TravelClick(Request);
                ServicePointManager.Expect100Continue = true;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                HttpWebRequest webRequest = CreateWebRequest_travelclick(_url);
                webRequest.Timeout = 60000;
                InsertSoapEnvelopeIntoWebRequest_travelclick(soapEnvelopeXml, webRequest);
                IAsyncResult asyncResult = webRequest.BeginGetResponse(null, null);
                asyncResult.AsyncWaitHandle.WaitOne();
                string soapResult;

                using (WebResponse webResponse = webRequest.EndGetResponse(asyncResult))
                {
                    using (StreamReader rd = new StreamReader(webResponse.GetResponseStream()))
                    {
                        soapResult = rd.ReadToEnd();
                    }
                    Console.Write(soapResult);
                }
                return soapResult;
            }
            catch (Exception ex)
            {
                Error = ex.Message.ToString();
                _ = _exception.Error(ex);
                return "";
            }
        }

        public string Get_TravelClick_BookingCancelRQ(string AgentVoucherNo, string Username, string Password, string Provider, string RoomTypeID, DataTable DtCancellation, DataTable DtPassenger, string ReferenceNo, TokenData tokenData)
        {
            try
            {
                #region TravelClick  Booking Deatils request
                string[] spitid = RoomTypeID.Split(new String[] { "$$$" }, StringSplitOptions.None);
                string Roomid = spitid[0].ToString();
                string RatePlanCode = spitid[1].ToString();
                string currencycode = spitid[2].ToString();
                string ExtrabedPrice = spitid[3].ToString();
                string ExtrabedQuantity = spitid[4].ToString();
                DateTime CheckInDate = Convert.ToDateTime(DtCancellation.Rows[0]["BookingDate"]);
                string CheckIn = CheckInDate.ToString("yyyy-MM-dd");
                //CheckIn = CheckIn.Substring(0, 4) + "-" + CheckIn.Substring(4, 2) + "-" + CheckIn.Substring(6, 2);
                DateTime CheckOutDate = Convert.ToDateTime(DtCancellation.Rows[0]["BookingToDate"]);
                string CheckOut = CheckOutDate.ToString("yyyy-MM-dd");
                double BookingAmount = 0;
                if (DtCancellation.Rows[0]["BookingAmount"] != null)
                    BookingAmount = Convert.ToDouble(DtCancellation.Rows[0]["BookingAmount"]);

                var now = DateTime.Now;
                var utcNow = now.ToUniversalTime();
                var ts = utcNow - now;

                string guid = Guid.NewGuid().ToString().Substring(0, 12);
                // string UniqueNo = Guid.NewGuid().ToString().Substring(0, 8);
                StringBuilder TravelClick_CancelBook_RQ = new StringBuilder();
                TravelClick_CancelBook_RQ.Append("<OTA_HotelResNotifRQ   Version=\"1.0\" TimeStamp=\"" + String.Format("{0:s}", DateTime.Now) + "\" EchoToken=\"" + guid + "\"  xmlns=\"http://www.opentravel.org/OTA/2003/05\">");
                TravelClick_CancelBook_RQ.Append("<POS>");
                TravelClick_CancelBook_RQ.Append("<Source>");
                TravelClick_CancelBook_RQ.Append("<BookingChannel Type=\"7\">");
                TravelClick_CancelBook_RQ.Append("<CompanyName Code=\"" + Password + "\">" + Username + "</CompanyName>");
                TravelClick_CancelBook_RQ.Append("</BookingChannel>");
                TravelClick_CancelBook_RQ.Append("</Source>");
                TravelClick_CancelBook_RQ.Append("</POS>");
                TravelClick_CancelBook_RQ.Append("<HotelReservations>");
                TravelClick_CancelBook_RQ.Append("<HotelReservation  ResStatus=\"Cancel\" LastModifyDateTime=\"" + DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss") + "\">");
                TravelClick_CancelBook_RQ.Append("<ResGlobalInfo>");
                TravelClick_CancelBook_RQ.Append("<HotelReservationIDs>");
                TravelClick_CancelBook_RQ.Append("<HotelReservationID  ResID_Type=\"13\"  ResID_Value =\"" + AgentVoucherNo + "\"/>");
                TravelClick_CancelBook_RQ.Append("</HotelReservationIDs>");
                TravelClick_CancelBook_RQ.Append("</ResGlobalInfo>");

                double CurrencyFactor = 1; 
                if(currencycode.ToLower()  != tokenData.SiteCurrency.ToLower())
                {
                    CurrencyFactor = _commonMethod.GetROEByCurrency(currencycode, tokenData.dtROE);
                }

                BookingAmount = BookingAmount / CurrencyFactor;

                TravelClick_CancelBook_RQ.Append("</HotelReservation>");
                TravelClick_CancelBook_RQ.Append("</HotelReservations>");
                TravelClick_CancelBook_RQ.Append("</OTA_HotelResNotifRQ>");

                string BookingDeatilsRQString = TravelClick_CancelBook_RQ.ToString();
                return BookingDeatilsRQString;
                #endregion
            }
            catch (Exception ex)
            {
                _ =_exception.ErrorNew(ex, ErrorLog._LogAPI.CancelBooking);
                return "";
            }

        }
    }
}
