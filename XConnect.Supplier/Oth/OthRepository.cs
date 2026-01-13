using MongoDB.Bson.IO;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XConnect.CommonUtilities;
using XConnect.CommonUtilities.Interface;
using XConnect.ErrorLogHandler.Interface;
using XConnect.Markup.Interface;
using XConnect.Models.Hotel;
using XConnect.Models;
using XConnect.Supplier.Oth.Interface;
using Microsoft.Extensions.Options;
using Options = XConnect.Models.Options;
using System.Text.Json;

namespace XConnect.Supplier.Oth
{
    public class OthRepository:IOth
    {

        private readonly ICommonMethod _commonMethod;
        private readonly IException _exception;
        private readonly IMarkup _markup;
        private readonly ISupplierRes _supplierRes;
        private readonly KeySettings _keySettings;

        public OthRepository(ICommonMethod commonMethod, IMarkup markup, ISupplierRes supplierRes, IOptions<KeySettings> keySettings)
        {
            _commonMethod = commonMethod;
            _exception = null;
            _markup = markup;
            _supplierRes = supplierRes;
            _keySettings = keySettings.Value;
        }

        public void GetWithinearthRequestOnlyNew(bool SearchByHotel, DataTable dtPreferredDataHotel, HotelSearchParameter SearchPara, List<SupplierDetails> OTHDetails, List<SupplierSearchConfig> OTHSearchConfig, ref ConcurrentBag<HotelX> AllTask, DataTable dtAgentDetail, DataSet dsAgentDetails, DataTable dtMarkupDetails, int Hit, DataTable OwnIDSList, DataRow[] commonCode)
        {
            try
            {
                DataRow[] drrayna = dtPreferredDataHotel.Select("ServiceName='withinearth'");
                string Ids = string.Empty;
                if (drrayna.Count() > 0)
                {
                    var paramValues1 = String.Join(",", drrayna.Select(x => String.Join(",", Convert.ToString(x.ItemArray[0]))));
                    Ids = Convert.ToString(paramValues1);
                }
                if (SearchPara.IsSearchFor.ToLower().IndexOf("preferred") >= 0 && drrayna.Count() == 0)
                    OTHSearchConfig = null;
                if (OTHSearchConfig != null)
                {

                    for (int s = 0; s < OTHSearchConfig.Count; s++)
                    {
                        //if (Convert.ToInt32(OTHSearchConfig[s]["Hit"]) != Hit)
                        //    continue;
                        if (Convert.ToInt32(OTHSearchConfig[s].CityID) == 0 || Convert.ToInt32(OTHSearchConfig[s].CityID) == SearchPara.CityID)//All City means 0
                        {
                            if (SearchByHotel)
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
                                                         .GroupBy(x => x.index / OTHSearchConfig[s].Noofrecords)  // integer division, the fractional part is truncated
                                                         .Select(g => g.Select(x => x.row).CopyToDataTable())
                                                         .ToArray();
                                for (int a = 0; a < splittedtables.Length; a++)
                                {
                                    if (HitOth)
                                    {
                                        var paramValues1 = String.Join(",", splittedtables[a].Select().Select(x => String.Join(",", Convert.ToString(x.ItemArray[0]))));
                                        string IdsForSearch = Convert.ToString(paramValues1);

                                        HotelX xhotel = new HotelX();
                                        //xhotel.Supplier = Convert.ToString(ConfigurationManager.AppSettings["IsWithinearth"]) == "1" ? "withinearth" : "oth";
                                        xhotel.Supplier = (Convert.ToString(_keySettings.IsWithinearth) == "1" ? "withinearth" : "oth");

                                        xhotel.DebugeMode = true;
                                        xhotel.SearchTimeOut = Convert.ToInt32(OTHSearchConfig[s].HotelSearchTimeOut);
                                        xhotel.ProcessTimeout = Convert.ToInt32(OTHSearchConfig[s].HotelSearchTimeOut2);
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
                                            //objrq.Request = (SearchPara.HotelID);
                                            objrq.Request = (IdsForSearch);
                                            objrq.Response = ""; RQRS.Add(objrq);
                                        }
                                        if (IdsForSearch != "")
                                            xhotel.StrHotelIds = IdsForSearch;
                                        xhotel.RQRS = RQRS;
                                        #endregion
                                        xhotel.SearchPara = SearchPara;
                                        if (dtAgentDetail != null && dtAgentDetail.Rows.Count > 0)
                                            xhotel.CID = Convert.ToString(dtAgentDetail.Rows[0]["AgentID"]);
                                        xhotel.NewPara = Ids;
                                        xhotel.ReturnRooms = 5;// Convert.ToInt32(OTHDetails[0].ReturnRooms);
                                        xhotel.TechName = "OTH"; //Convert.ToString(OTHDetails[0].TechName);
                                        AllTask.Add(xhotel);
                                        //else
                                        //{
                                        //    if (SearchPara.StarStart <= Convert.ToInt32(OTHSearchConfig[s]["Stars"]) && SearchPara.StarEnd >= Convert.ToInt32(OTHSearchConfig[s]["Stars"]))
                                        //    {
                                        //        tsOTH = GetOTHDataset(XMLReq, SearchPara.IsB2BOrB2C, dtAgentDetail, Convert.ToString(SearchPara.CityName), Convert.ToString(SearchPara.CountryCode), Convert.ToString(OTHSearchConfig[s]["Stars"]), Convert.ToString(OTHSearchConfig[s]["Stars"]), "", dsAgentDetails.Tables[1], SearchPara, dtMarkupDetails, Favouritedt, Ids);
                                        //        Array.Resize(ref AllTask, (AllTask != null ? AllTask.Length + 1 : 1));
                                        //        AllTask[AllTask.Length - 1] = tsOTH;
                                        //    }
                                        //}
                                    }
                                }
                            }
                            else
                            {
                                List<StarRating> lstRating = new List<StarRating>();
                                lstRating = _commonMethod.GetStarRatingStringList(SearchPara);
                                int starcount = lstRating.Count();
                                for (int a = 0; a < starcount; a++)
                                {
                                    int Str = lstRating[a].Star;

                                    if (Str == 0)
                                        continue;
                                    if (Str == 1 || Str == 2)//New Added as per Vinodsir&Pinakin 4-SEP-2024 Hit SP 5 times for 3,4,5 star
                                    {
                                        HotelX xhotel = new HotelX();
                                        xhotel.Supplier = (Convert.ToString(_keySettings.IsWithinearth) == "1" ? "withinearth" : "oth");
                                        xhotel.DebugeMode = true;
                                        xhotel.StarRating = Str;
                                        xhotel.Version = "1";
                                        xhotel.SearchTimeOut = Convert.ToInt32(OTHSearchConfig[s].HotelSearchTimeOut);
                                        xhotel.ProcessTimeout = Convert.ToInt32(OTHSearchConfig[s].HotelSearchTimeOut2);
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
                                        if (SearchPara.HotelID != "")
                                            xhotel.StrHotelIds = SearchPara.HotelID;
                                        xhotel.RQRS = RQRS;
                                        #endregion
                                        xhotel.SearchPara = SearchPara;
                                        if (dtAgentDetail != null && dtAgentDetail.Rows.Count > 0)
                                            xhotel.CID = Convert.ToString(dtAgentDetail.Rows[0]["AgentID"]);
                                        xhotel.NewPara = Ids;
                                        xhotel.ReturnRooms = 5;// Convert.ToInt32(OTHDetails[0].ReturnRooms);
                                        xhotel.TechName = "OTH"; //Convert.ToString(OTHDetails[0].TechName);
                                        AllTask.Add(xhotel);
                                    }
                                    else //New Added as per Vinodsir&Pinakin 4-SEP-2024 Hit SP 5 times for 3,4,5 star
                                    {
                                        for (int i = 0; i < 5; i++)
                                        {
                                            HotelX xhotel = new HotelX();
                                            xhotel.Supplier = (Convert.ToString(_keySettings.IsWithinearth) == "1" ? "withinearth" : "oth");
                                            xhotel.DebugeMode = true;
                                            xhotel.StarRating = Str;
                                            xhotel.Version = "" + (i + 1) + "";
                                            xhotel.SearchTimeOut = Convert.ToInt32(OTHSearchConfig[s].HotelSearchTimeOut);
                                            xhotel.ProcessTimeout = Convert.ToInt32(OTHSearchConfig[s].HotelSearchTimeOut2);
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
                                            if (SearchPara.HotelID != "")
                                                xhotel.StrHotelIds = SearchPara.HotelID;
                                            xhotel.RQRS = RQRS;
                                            #endregion
                                            xhotel.SearchPara = SearchPara;
                                            if (dtAgentDetail != null && dtAgentDetail.Rows.Count > 0)
                                                xhotel.CID = Convert.ToString(dtAgentDetail.Rows[0]["AgentID"]);
                                            xhotel.NewPara = Ids;
                                            xhotel.ReturnRooms = 5;// Convert.ToInt32(OTHDetails[0].ReturnRooms);
                                            xhotel.TechName = "OTH"; //Convert.ToString(OTHDetails[0].TechName);
                                            AllTask.Add(xhotel);
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


        //public List<StarRating> GetStarRatingStringList(HotelSearchParameter SearchPara)
        //{
        //    List<StarRating> rating = new List<StarRating>(); ;

        //    try
        //    {
        //        if (SearchPara.StarStart <= 1 && SearchPara.StarEnd >= 1)
        //            rating.Add(new StarRating { Star = 1 });
        //        if (SearchPara.StarStart <= 2 && SearchPara.StarEnd >= 2)
        //            rating.Add(new StarRating { Star = 2 });

        //        if (SearchPara.StarStart <= 3 && SearchPara.StarEnd >= 3)
        //            rating.Add(new StarRating { Star = 3 });

        //        if (SearchPara.StarStart <= 4 && SearchPara.StarEnd >= 4)
        //            rating.Add(new StarRating { Star = 4 });

        //        if (SearchPara.StarStart <= 5 && SearchPara.StarEnd >= 5)
        //            rating.Add(new StarRating { Star = 5 });

        //        if (SearchPara.StarStart <= 0 && SearchPara.StarEnd >= 0)
        //            rating.Add(new StarRating { Star = 0 });
        //        return rating;
        //    }
        //    catch (Exception ex)
        //    {
        //        _exception.Error(ex);
        //        return rating;
        //    }
        //}

        public async Task<List<Options>> Oth_HotelSearchWithRooms_050325(HotelX item, AdvancedOptions advancedOptions, TokenData tokenData, DataSet GTAResponse, DataTable dtAgentDetail, DataTable dtSuplierMarkup, HotelSearchParameter SearchParam, DataTable dtMarkupDetails, int agent, CommonSearch commonSearch, string Response, string ProccessCutOffTime, Int32 processTimeout, string ResProccessTimerLogActive, string ResProccessTimeValueforSaveLog)
        {
            System.Diagnostics.Stopwatch timer_room_Oth = new System.Diagnostics.Stopwatch();
            timer_room_Oth.Start();
            List<Options> options = new List<Options>();

            if (GTAResponse != null && GTAResponse.Tables.Count > 0 && GTAResponse.Tables[0].Rows.Count > 0 || Response != "")
            {
                DataTable DtHotel = _commonMethod.GenerateDataTableNewV2();

                DataTable dtHotelAllocation = _commonMethod.GenerateHotelAllocationTableNewV2();
                DataTable dtComplementary = _commonMethod.GenerateComplementaryTableNewV2();
                DataTable dtCommonInfo = _commonMethod.GenerateCommonInfoTableNewV2();
                DataTable dtHotelMinStay = _commonMethod.GenerateHotelMinStayNewV2();
                DataSet dsTemp = new DataSet();


                int CutOffDays = CacheMaster.CutOffDays;
                CutOffDays = _commonMethod.GetCutOffDays(commonSearch.dtSuplierMarkup, Convert.ToString(_keySettings.IsOTHActive) == "1" ? "withinearth" : "oth");
                double SubAgentMarkup = 0;
                int IsParentId = 1;
                if (dtAgentDetail != null)
                {
                    IsParentId = Convert.ToInt32(dtAgentDetail.Rows[0]["IsParentId"]);
                    SubAgentMarkup = Convert.ToDouble(dtAgentDetail.Rows[0]["B2CMarkupPer"]);
                }
                try
                {
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
                    string giitaCode = "";
                    string DiscountPerTtext = "";

                    string Longitude = "";
                    string Latitude = "";
                    int ReviewCount = 0;
                    int ReviewRating = 0;
                    int SortSeq = 9999;
                    DateTime curDate = _commonMethod.GetTimeZoneDate(SearchParam.CurrentDate);
                    if (SearchParam.Filters.IsShowRooms == 1)
                    {
                        if (GTAResponse == null || GTAResponse.Tables[0] == null || GTAResponse.Tables[0].Rows.Count == 0)
                            return options;
                    }
                    double AgentPercentage = 0.0;
                    Boolean blnIsShowSupplier = false;
                    double AgentMarkupAmount = 0.0;
                    Boolean isPerce = true;
                    int MasterSupplierID = 0;
                    if (dtSuplierMarkup != null)
                    {
                        dtSuplierMarkup.DefaultView.RowFilter = "SupplierName='" + (Convert.ToString(_keySettings.IsOTHActive) == "1" ? "withinearth" : "oth") + "'";
                        if (dtSuplierMarkup.DefaultView.Count > 0)
                        {
                            try
                            {
                                AgentPercentage = Convert.ToDouble(dtSuplierMarkup.DefaultView[0]["AgentWiseMarkup"]);
                                blnIsShowSupplier = (Convert.ToInt16(dtSuplierMarkup.DefaultView[0]["IsShowSupplier"]) == 1 ? true : false);
                                MasterSupplierID = Convert.ToInt32(dtSuplierMarkup.DefaultView[0]["MasterSupplierId"]);
                            }
                            catch (Exception x)//k2
                            {
                                try
                                {
                                    AgentPercentage = Convert.ToDouble(dtSuplierMarkup.DefaultView[0]["AgentWiseMarkup"]);
                                    blnIsShowSupplier = (Convert.ToInt16(dtSuplierMarkup.DefaultView[0]["IsShowSupplier"]) == 1 ? true : false);
                                    MasterSupplierID = Convert.ToInt32(dtSuplierMarkup.DefaultView[0]["MasterSupplierId"]);
                                }
                                catch (Exception y)
                                {
                                    //_exception.Error(y, SearchParam.SearchKey);
                                    await _exception.Error(x);

                                }
                            }
                        }
                    }
                    ///////////@New List Code Added on 15-12-19


                    #region "When not Need Room"  

                    #region "Hotel Code Block "

                    if (SearchParam.Filters.IsShowRooms == 0)
                    {
                        List<OthListResponse> ResponseOth = new List<OthListResponse>();
                        if (Response != "")
                            ResponseOth = JsonSerializer.Deserialize<List<OthListResponse>>(Response);

                        if (ResponseOth == null || ResponseOth.Count == 0)
                            return options;
                        if (dtSuplierMarkup != null)
                        {
                            dtSuplierMarkup.DefaultView.RowFilter = "SupplierName='withinearth'";
                            if (dtSuplierMarkup.DefaultView.Count > 0)
                            {
                                AgentPercentage = Convert.ToDouble(dtSuplierMarkup.DefaultView[0]["AgentWiseMarkup"]);
                                blnIsShowSupplier = (Convert.ToInt16(dtSuplierMarkup.DefaultView[0]["IsShowSupplier"]) == 1 ? true : false);
                            }
                        }
                        if (ResponseOth.Count > 0)
                        {
                            item.ResponseType = "Success";
                        }
                        for (int h = 0; h <= ResponseOth.Count - 1; h++)
                        {
                            DataTable DtHotelRoom = _commonMethod.GenerateRoomtypeDataTableNewV2();
                            DataTable dtCancellation = _commonMethod.GenerateCancellationDataTableNewV2();
                            DataTable DtHotelRoomRateBreackUp = _commonMethod.GenerateoomRateBreackUpDataTableNewV2();
                            if (dtSuplierMarkup != null)
                            {
                                if (dtSuplierMarkup.DefaultView.Count > 0)
                                {
                                    MasterSupplierID = Convert.ToInt32(dtSuplierMarkup.DefaultView[0]["MasterSupplierId"]);
                                }
                            }
                            if (Convert.ToString(ResponseOth[h].GiitaId) == string.Empty || Convert.ToString(ResponseOth[h].GiitaId) == "0")
                                continue;
                            if (ResponseOth[h].SupplierId > 0)
                                MasterSupplierID = ResponseOth[h].SupplierId;

                            _markup.getAgetMarkupSetting("withinearth", Convert.ToString(ResponseOth[h].OwnsystemHotelId), dtSuplierMarkup, dtMarkupDetails, ref AgentPercentage, ref AgentMarkupAmount, ref blnIsShowSupplier, ref isPerce, tokenData.AgentId, Convert.ToInt32(ResponseOth[h].Rating), SearchParam.CityID, Convert.ToString(SearchParam.CountryID), SearchParam.CheckInDate);
                            double AgentCompanyPercentage = SearchParam.AgentCompanyPercentage;
                            bool IsB2C = Convert.ToBoolean(Convert.ToByte(SearchParam.IsB2BOrB2C));

                            double B2CMarkup = (IsB2C == false ? AgentCompanyPercentage : XMLHotelBookingCommon.B2CMarkup);
                            DataRow drXmlHotel = DtHotel.NewRow();
                            Int64 GiataId = ResponseOth[h].GiitaId;
                            drXmlHotel["Favorite"] = 0;

                            if (Convert.ToString(ResponseOth[h].Rating) == "" || Convert.ToString(ResponseOth[h].Rating) == "0")
                                ResponseOth[h].Rating = 1;
                            Rating = Convert.ToString(ResponseOth[h].Rating);
                            DataTable DtHotelPolicy = _commonMethod.GenerateCancellationDataTableNewV2();
                            double price = Convert.ToDouble(ResponseOth[h].FinalSellingPrice); //* ROE;
                            string CurrencyCode = Convert.ToString(ResponseOth[h].CurrencyCode);
                            Int32 ItemCode = ResponseOth[h].OwnsystemHotelId;
                            drXmlHotel["XMLCountry"] = Convert.ToString(SearchParam.CountryID);
                            drXmlHotel["XMLCity"] = Convert.ToString(SearchParam.CityID);
                            drXmlHotel["XmlHotelCity"] = Convert.ToString(ItemCode);
                            drXmlHotel["XMLHotelCode"] = ItemCode;
                            drXmlHotel["EssentialInformation"] = "";
                            drXmlHotel["Description"] = "";
                            drXmlHotel["SupplierName"] = "oth";
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
                            drXmlHotel["GIITACode"] = GiataId;
                            drXmlHotel["RaynaId"] = ItemCode;

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

                                _markup.GetHotelDataCommon(ref price, ref willAddOrNot, ref OwnsystemHotelId, ref RaynaID, ref GiataId, ref RatingMain, ref drXmlHotel, commonSearch.AgentId, commonSearch.TariffID, commonSearch.dsAgentDetails, commonSearch.supplierGiataMaster, SearchParam, commonSearch.dtCountryCityMarkup, commonSearch.B2CMarkup, commonSearch.IsParentId, commonSearch.SubAgentMarkup, commonSearch.SearchByHotel, commonSearch.HotelIDS);
                                if (willAddOrNot)
                                {
                                    #region "Room Logic"
                                    DataTable dt_HotelRoom = _commonMethod.GenerateRoomtypeDataTableNewV2();
                                    DataTable dt_Cancellation = _commonMethod.GenerateCancellationDataTableNewV2();
                                    DataTable dt_RateBreackUp = _commonMethod.GenerateoomRateBreackUpDataTableNewV2();
                                    #endregion
                                    decimal LowestPrice = Convert.ToDecimal(price).CalculatePrice(commonSearch.tokenData, commonSearch.advancedOptions, CurrencyCode);
                                    // Options ops = (await _supplierRes.GetCommonRoomForSuppliers(Convert.ToInt64(GiataId), tokenData, advancedOptions, Convert.ToDouble(LowestPrice), drXmlHotel, DtHotelRoom, dtCancellation, DtHotelRoomRateBreackUp, SearchParam));
                                    Options ops = (await _supplierRes.GetCommonRoomForSuppliers(GiataId, commonSearch.tokenData, commonSearch.advancedOptions, Convert.ToDouble(LowestPrice), drXmlHotel, DtHotelRoom, dtCancellation, DtHotelRoomRateBreackUp, SearchParam));

                                    // Options ops = (await _supplierRes.GetCommonRoomForSuppliers(GiataId, commonSearch.tokenData, commonSearch.advancedOptions, Convert.ToDouble(LowestPrice), drXmlHotel, dt_HotelRoom, dt_Cancellation, dt_RateBreackUp, SearchParam));

                                    if (ops != null)
                                        options.Add(ops);

                                }
                                #endregion
                            }
                        }
                    }
                    #endregion

                    #endregion
                    #region "When Need Room" 
                    else
                    {
                        if (GTAResponse.Tables[0].Rows.Count > 0)
                        {
                            item.ResponseType = "Success";
                        }
                        for (int h = 0; h < GTAResponse.Tables[0].Rows.Count; h++)
                        {
                            //Check Time If High then return NoCount
                            //if (ProccessCutOffTime == "1" && timer_room_Oth.ElapsedMilliseconds > processTimeout) // Disable (API group)3,4-Mar-2023
                            //{
                            //    //timer_room_Oth.Stop();
                            //    break;
                            //}
                            //--end
                            DataTable DtHotelRoom = _commonMethod.GenerateRoomtypeDataTableNewV2();
                            DataTable dtCancellation = _commonMethod.GenerateCancellationDataTableNewV2();
                            DataTable DtHotelRoomRateBreackUp = _commonMethod.GenerateoomRateBreackUpDataTableNewV2();

                            ReviewCount = 0;
                            ReviewRating = 0;

                            giitaCode = Convert.ToString(GTAResponse.Tables[0].Rows[h]["GiitaId"]);
                            Rating = Convert.ToString(GTAResponse.Tables[0].Rows[h]["Rating"]);
                            int SupplierId = Convert.ToInt32(GTAResponse.Tables[0].Rows[h]["SupplierId"]);
                            if (dtSuplierMarkup != null)
                            {
                                if (dtSuplierMarkup.DefaultView.Count > 0)
                                {
                                    MasterSupplierID = Convert.ToInt32(dtSuplierMarkup.DefaultView[0]["MasterSupplierId"]);

                                }
                            }
                            if (SupplierId > 0)
                                MasterSupplierID = SupplierId;
                            // Common.getAgetMarkupSetting(Convert.ToString(ConfigurationManager.AppSettings["IsWithinearth"]) == "1" ? "withinearth" : "oth", giitaCode, dtSuplierMarkup, dtMarkupDetails, ref AgentPercentage, ref AgentMarkupAmount, ref blnIsShowSupplier, ref isPerce, Convert.ToInt32(agent), Convert.ToInt32(Rating), SearchParam.CityID, Convert.ToString(SearchParam.CountryID));
                            string markupsuppliername = Convert.ToString(GTAResponse.Tables[0].Rows[0]["SupplierName"]) == "oth" ? ((Convert.ToString(_keySettings.IsOTHActive) == "1" ? "withinearth" : "oth")) : Convert.ToString(GTAResponse.Tables[0].Rows[0]["SupplierName"]);
                            _markup.getAgetMarkupSetting("withinearth", giitaCode, dtSuplierMarkup, dtMarkupDetails, ref AgentPercentage, ref AgentMarkupAmount, ref blnIsShowSupplier, ref isPerce, Convert.ToInt32(agent), Convert.ToInt32(Rating), SearchParam.CityID, Convert.ToString(SearchParam.CountryID), SearchParam.CheckInDate);

                            double AgentCompanyPercentage = Convert.ToDouble(SearchParam.AgentCompanyPercentage);
                            bool IsB2C = Convert.ToBoolean(Convert.ToByte(SearchParam.IsB2BOrB2C));

                            double B2CMarkup = (IsB2C == false ? AgentCompanyPercentage : XMLHotelBookingCommon.B2CMarkup);
                            //if (IsB2C == true)
                            //{
                            //	AgentPercentage = 0;
                            //	AgentMarkupAmount = 0;
                            //}


                            DataRow drXmlHotel = DtHotel.NewRow();

                            RoomCancellation = "";
                            LowestRate = 0;
                            string uniqueno = Guid.NewGuid().ToString();
                            string ItemCode = Convert.ToString(GTAResponse.Tables[0].Rows[h]["raynaid"]);
                            //if (GTAResponse.Tables[11] != null && GTAResponse.Tables[11].Rows.Count > 0)
                            //{
                            //    DataRow[] drMin = GTAResponse.Tables[11].Select("raynaid='" + ItemCode + "'");
                            //    if (drMin.Length > 0)
                            //        continue;
                            //}
                            string Meal = Convert.ToString(GTAResponse.Tables[0].Rows[h]["BoardBasis"]);
                            string HotelName = Convert.ToString(GTAResponse.Tables[0].Rows[h]["HotelName"]);
                            string CurrencyCode = Convert.ToString(GTAResponse.Tables[0].Rows[h]["CurrencyCode"]);

                            string OfferCode = "";
                            ReviewCount = (Convert.ToString(GTAResponse.Tables[0].Rows[h]["ReviewCount"]) != string.Empty ? Convert.ToInt32(GTAResponse.Tables[0].Rows[h]["ReviewCount"]) : 0);
                            ReviewRating = (Convert.ToString(GTAResponse.Tables[0].Rows[h]["ReviewRating"]) != string.Empty ? Convert.ToInt32(GTAResponse.Tables[0].Rows[h]["ReviewRating"]) : 0);
                            string HotelInformation = Convert.ToString(GTAResponse.Tables[0].Rows[h]["SPUI"]);
                            string HotelDescription = Convert.ToString(GTAResponse.Tables[0].Rows[h]["Description"]);
                            internalHotelId = Convert.ToString(GTAResponse.Tables[0].Rows[h]["raynaid"]);

                            //ownsystemhotelId = Convert.ToString(GTAResponse.Tables[0].Rows[h]["raynaid"]);
                            ownsystemhotelId = Convert.ToString(GTAResponse.Tables[0].Rows[h]["OriginalRaynaid"]);//#Due to Ticket#13957

                            giitaCode = Convert.ToString(GTAResponse.Tables[0].Rows[h]["GiitaId"]);
                            Address = Convert.ToString(GTAResponse.Tables[0].Rows[h]["Address"]);
                            int Favorite = 0;// Convert.ToInt32(Favouritedt.Select("Favorite=" + ownsystemhotelId).GetLength(0) == 0 ? 0 : Convert.ToInt32(Favouritedt.Select("Favorite=" + ownsystemhotelId)[0]["Favorite"]));
                            drXmlHotel["Favorite"] = Favorite;
                            LocationDescription = Convert.ToString(GTAResponse.Tables[0].Rows[h]["Location"]);
                            Rating = Convert.ToString(GTAResponse.Tables[0].Rows[h]["Rating"]);
                            Chain = Convert.ToString(GTAResponse.Tables[0].Rows[h]["GroupName"]);
                            SortSeq = Convert.ToInt32(GTAResponse.Tables[0].Rows[h]["SortSequence"]);
                            if (Rating == "" || Rating == "0")
                                Rating = "1";
                            CityName = "Dubai City";
                            CountryId = "13063";
                            CityId = "13668";
                            Latitude = Convert.ToString(GTAResponse.Tables[0].Rows[h]["Latitude"]);
                            Longitude = Convert.ToString(GTAResponse.Tables[0].Rows[h]["Longitude"]);

                            //DataRow[] drComplementary = GTAResponse.Tables[6].Select("RaynaId=" + ItemCode + "");
                            ////Need to Add Complementry Logic here
                            //if (drComplementary.GetLength(0) > 0)
                            //{
                            //    //for(int cm=0;cm<(drComplementary.GetLength(0) > 5?5: drComplementary.GetLength(0));cm++)
                            //    for (int cm = 0; cm < drComplementary.GetLength(0); cm++)
                            //    {
                            //        DataRow dtr = dtComplementary.NewRow();
                            //        dtr["OwnsystemHotelId"] = Convert.ToString(drComplementary[cm]["RaynaId"]);
                            //        dtr["ComplementaryName"] = Convert.ToString(drComplementary[cm]["ComplementaryName"]);
                            //        dtr["ComplementaryText"] = Convert.ToString(drComplementary[cm]["ComplementaryText"]);
                            //        dtr["ImagePath"] = Convert.ToString(drComplementary[cm]["ImagePath"]);
                            //        dtComplementary.Rows.Add(dtr);
                            //    }
                            //}

                            //DataRow[] drCommonInfo = GTAResponse.Tables[12].Select("RaynaId=" + ItemCode + "");
                            ////Need to Add common information(hotel description, common policy etc) Logic here

                            //if (drCommonInfo.GetLength(0) > 0)
                            //{
                            //    //for(int cm=0;cm<(drComplementary.GetLength(0) > 5?5: drComplementary.GetLength(0));cm++)
                            //    for (int cm = 0; cm < drCommonInfo.GetLength(0); cm++)
                            //    {
                            //        DataRow dtr = dtCommonInfo.NewRow();
                            //        dtr["OwnsystemHotelId"] = Convert.ToString(drCommonInfo[cm]["RaynaId"]);
                            //        dtr["CommonPolicyName"] = Convert.ToString(drCommonInfo[cm]["CommonPolicyName"]);
                            //        dtr["CommonPolicyManagementText"] = Convert.ToString(drCommonInfo[cm]["CommonPolicyManagementText"]);
                            //        dtCommonInfo.Rows.Add(dtr);
                            //    }
                            //}

                            DataRow[] drRoomCategories = GTAResponse.Tables[8].Select("RaynaId=" + internalHotelId + "");
                            int PreviousRoomNo = 1;
                            int LogicalCounter = 0;
                            for (int rc = 0; rc <= drRoomCategories.GetLength(0) - 1; rc++)
                            {
                                //Room wise MLOS added on 29-6-2022
                                if (GTAResponse.Tables[11] != null && GTAResponse.Tables[11].Rows.Count > 0)
                                {
                                    DataRow[] drMinx = GTAResponse.Tables[11].Select("raynaid='" + ItemCode + "' and RoomTypeId='" + Convert.ToString(0) + "'");//RoomTypeId=0 means for All rooms
                                    if (drMinx.Length > 0)
                                        continue;
                                    DataRow[] drMin = GTAResponse.Tables[11].Select("raynaid='" + ItemCode + "' and RoomTypeId='" + Convert.ToString(drRoomCategories[rc]["RoomTypeID"]) + "'");
                                    if (drMin.Length > 0)
                                        continue;
                                }

                                DataRow drhotelRoom = DtHotelRoom.NewRow();
                                if (Convert.ToInt32(drRoomCategories[rc]["RoomNo"]) != PreviousRoomNo)
                                {
                                    LogicalCounter = 0;
                                }
                                PreviousRoomNo = Convert.ToInt32(drRoomCategories[rc]["RoomNo"]);
                                LogicalCounter++;
                                // BookingStatus = "";
                                string RoomDesc = Convert.ToString(drRoomCategories[rc]["RaynaRoomTypeName"]);
                                string RoomTypeID = Convert.ToString(drRoomCategories[rc]["RoomTypeID"]);
                                string BoardTypeID = Convert.ToString(drRoomCategories[rc]["BoardTypeID"]);
                                OfferCode = Convert.ToString(drRoomCategories[rc]["OfferCode"]);
                                string RoomId = Convert.ToString(drRoomCategories[rc]["RaynaRoomTypeName"]) + "-" + Convert.ToString(drRoomCategories[rc]["BoardBasis"]);
                                string MealId = Convert.ToString(drRoomCategories[rc]["BoardBasis"]);
                                drhotelRoom["OwnsystemHotelId"] = internalHotelId; //ownsystemhotelId; //for extanet 27-08-2024 smit
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
                                drhotelRoom["RoomTypeId"] = Convert.ToString(drRoomCategories[rc]["RoomTypeId"]); ;
                                drhotelRoom["MealId"] = Convert.ToString(drRoomCategories[rc]["BoardBasis"]);
                                //By manish
                                drhotelRoom["IsCombineRoom"] = false;
                                drhotelRoom["IsPolicyDynamic"] = false;
                                drhotelRoom["OfferDescription"] = Convert.ToString(drRoomCategories[rc]["OfferDescription"]) != "" ? Convert.ToString(drRoomCategories[rc]["OfferDescription"]) : OfferCode;

                                if (Convert.ToString(drRoomCategories[rc]["BoardBasis"]) == "")
                                {
                                    drRoomCategories[rc]["BoardBasis"] = "RO";
                                }
                                drXmlHotel["MealName"] = Convert.ToString(drRoomCategories[rc]["BoardBasis"]);

                                DataRow[] DTdrdate = GTAResponse.Tables[3].Select("RoomTypeID='" + RoomTypeID + "' and RaynaId=" + ItemCode + " and RoomNo=" + Convert.ToDouble(drRoomCategories[rc]["RoomNo"]) + " and BoardTypeID='" + BoardTypeID + "'");
                                DataRow[] DTdSurchargerdate = GTAResponse.Tables[4].Select("RoomTypeID='" + RoomTypeID + "' and RaynaId=" + ItemCode + " and BoardTypeID='" + BoardTypeID + "' and RoomNo=" + Convert.ToDouble(drRoomCategories[rc]["RoomNo"]));

                                int ActualNoOfNight = Convert.ToInt32(DTdrdate.GetLength(0));
                                int ActualSurchargeNoOfNight = Convert.ToInt32(DTdSurchargerdate.GetLength(0));

                                drhotelRoom["AdultsOccupancy"] = Convert.ToString(DTdrdate[0]["RoomCapacity"]);
                                drhotelRoom["ChildOccupancy"] = Convert.ToString(DTdrdate[0]["NoOfChild"]);
                                drhotelRoom["MaxExtrabed"] = Convert.ToString(DTdrdate[0]["NoOfExtraBeds"]);
                                drhotelRoom["BoardTypeID"] = BoardTypeID;
                                drhotelRoom["OTHPromoCode"] = Convert.ToString(DTdrdate[0]["AppliedOfferCode"]);
                                //drhotelRoom["ExtraNight"] = IsFromPkg == 1 ? Convert.ToString(DTdrdate[0]["ExtraNight"]) : "0";
                                //drhotelRoom["ExtraNightDifference"] = IsFromPkg == 1 ? Convert.ToString(DTdrdate[0]["ExtraNightDifference"]) : "0";
                                DataRow[] drCancellationPolicy = GTAResponse.Tables[9].Select
                                    ("raynaid=" + ItemCode + " and (RoomTypeID='" + RoomTypeID + "' or RoomTypeID='0') and (BoardTypeID='" + BoardTypeID + "' or (BoardTypeID='0' and " + BoardTypeID + ">0))");

                                if (drCancellationPolicy.GetLength(0) == 0)
                                {
                                    //drCancellationPolicy = GTAResponse.Tables[9].Select
                                    //("raynaid=" + ItemCode + " and RaynaRoomTypeName=''");
                                    drCancellationPolicy = GTAResponse.Tables[9].Select
                                    ("raynaid=" + ItemCode + " and (RoomTypeID='0') and (BoardTypeID='0')");
                                }
                                int intRoomCancellation = 0;

                                if (drCancellationPolicy.GetLength(0) == 0)
                                {
                                    //Default non-refundable cancellation policy
                                    double ItemBasePrice = Math.Round(Convert.ToDouble(drRoomCategories[rc]["Price"]), 6);
                                    double ItemSurchargePriceBasePrice = Math.Round(Convert.ToDouble(drRoomCategories[rc]["SurchargePrice"]), 6);

                                    if (ActualSurchargeNoOfNight != 0)
                                    {
                                        //ItemSurchargePriceBasePrice = (ItemSurchargePriceBasePrice / ActualSurchargeNoOfNight);
                                        ItemSurchargePriceBasePrice = (ItemSurchargePriceBasePrice);

                                    }
                                    double CacelationAmountData = ItemBasePrice;
                                    double ChargeAmount = CacelationAmountData;
                                    double CompanyChargeAmount = ChargeAmount;


                                    double Vatpolicy = ChargeAmount;
                                    double ChargeSurchargeAmount = ItemSurchargePriceBasePrice;
                                    double AgentChargeAmount = 0;
                                    //Double CancellationMarkup = Math.Round(ChargeAmount, 2);
                                    Double CancellationMarkup = 0.0;
                                    if (isPerce)
                                    {
                                        if (IsB2C == false && (B2CMarkup == 0 || (AgentPercentage > 0)))
                                        {
                                            CancellationMarkup = Math.Round(((ChargeAmount * AgentPercentage) / 100), 6);
                                        }
                                    }
                                    else
                                        CancellationMarkup = (ChargeAmount == 0 ? ChargeAmount : AgentMarkupAmount);
                                    ChargeAmount = ChargeAmount + CancellationMarkup;
                                    #region Vat
                                    double VatChargeAmount = ChargeAmount;
                                    DataTable dtvatlist = await _commonMethod.GetVatCountrylist(Convert.ToString(SearchParam.CountryID) == "" ? 0 : Convert.ToInt32(SearchParam.CountryID), Convert.ToInt32(SearchParam.CityID), "hotel");
                                    if (dtvatlist.Rows.Count > 0)
                                    {
                                        ChargeAmount = ChargeAmount + (Convert.ToDouble(_commonMethod.GetcalVatAmount(Convert.ToDecimal(ChargeAmount) - Convert.ToDecimal(Vatpolicy), Convert.ToDecimal(dtvatlist.Rows[0]["IntRate"]))));
                                    }
                                    VatChargeAmount = ChargeAmount - VatChargeAmount;
                                    #endregion
                                    AgentChargeAmount = ChargeAmount;


                                    double MainAgentPrice = ChargeAmount;
                                    if (IsParentId == 0)
                                    {
                                        Double SubCancellationAgentMarkup = Math.Round((Math.Round((ChargeAmount * SubAgentMarkup), 6) / 100), 6);
                                        ChargeAmount = Math.Round((ChargeAmount + SubCancellationAgentMarkup), 6);
                                    }
                                    Double CancellationAgentMarkup = 0;
                                    if (IsB2C == true)
                                    {
                                        CancellationAgentMarkup = Math.Round((Math.Round((ChargeAmount * AgentPercentage), 6) / 100), 6);
                                        ChargeAmount = Math.Round(ChargeAmount + CancellationAgentMarkup, 6);
                                    }

                                    ChargeAmount = Math.Round((ChargeAmount + CancellationAgentMarkup), 6);
                                    ChargeAmount = ChargeAmount + ChargeSurchargeAmount;
                                    CompanyChargeAmount = CompanyChargeAmount + ChargeSurchargeAmount;
                                    DataRow drHotelCancellation = dtCancellation.NewRow();
                                    drHotelCancellation["SupplierName"] = "oth";
                                    drHotelCancellation["OwnsystemHotelId"] = internalHotelId; //ownsystemhotelId; //for extanet 27-08-2024 smit
                                    drHotelCancellation["RoomNo"] = Convert.ToString(drRoomCategories[rc]["RoomNo"]);
                                    //drHotelCancellation["RoomTypeId"] = Convert.ToString(drRoomCategories[rc]["RaynaRoomTypeName"]);
                                    drHotelCancellation["RoomTypeId"] = Convert.ToString(drRoomCategories[rc]["RoomTypeID"]);
                                    drHotelCancellation["RoomTypeName"] = RoomDesc;
                                    drHotelCancellation["MealId"] = Convert.ToString(drRoomCategories[rc]["BoardBasis"]);
                                    //drHotelCancellation["FromDate"] = FromDate;
                                    //drHotelCancellation["ToDate"] = ToDate;
                                    drHotelCancellation["FromDate"] = DateTime.Now.Date;
                                    drHotelCancellation["ToDate"] = DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null);
                                    drHotelCancellation["CompanyCancellationPrice"] = MainAgentPrice;
                                    drHotelCancellation["AgentCancellationPrice"] = ChargeAmount;
                                    //drHotelCancellation["BoardTypeID"] = VatChargeAmount;
                                    drHotelCancellation["VatdiffAmount"] = Vatpolicy;
                                    drHotelCancellation["BoardTypeID"] = BoardTypeID;
                                    drHotelCancellation["SupplierCurrency"] = CurrencyCode;
                                    drHotelCancellation["BuyingPrice"] = CompanyChargeAmount;

                                    dtCancellation.Rows.Add(drHotelCancellation);
                                }
                                else
                                {
                                    for (int c = drCancellationPolicy.GetLength(0) - 1; c >= 0; c--)
                                    {
                                        double ItemBasePrice = Math.Round(Convert.ToDouble(drRoomCategories[rc]["Price"]), 6);
                                        double ItemSurchargePriceBasePrice = Math.Round(Convert.ToDouble(drRoomCategories[rc]["SurchargePrice"]), 6);
                                        double VatOnRateBrackUp_new = 0;
                                        double surchargewithmarkup_policy = _markup.ProcessHotelRateBreakupPrice_WithMarkup(1, SearchParam.CityID, SearchParam.CountryID.ToString(), ItemSurchargePriceBasePrice, B2CMarkup, IsParentId, isPerce, AgentPercentage, AgentMarkupAmount, SubAgentMarkup, ref VatOnRateBrackUp_new);

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
                                            ItemSurchargePriceBasePrice = (ItemSurchargePriceBasePrice);
                                        }
                                        double CacelationAmountData = ItemBasePrice;
                                        if (cancellationNights != 0)
                                        {
                                            CacelationAmountData = (CacelationAmountData / ActualNoOfNight) * cancellationNights;
                                        }
                                        double ChargeAmount = (CacelationAmountData * CancellationPercentage) / 100;

                                        double ChargeSurchargeAmount = (surchargewithmarkup_policy * CancellationPercentage) / 100;
                                        double AgentChargeAmount = 0;
                                        double Vatpolicy = 0;
                                        double MAinAgentCharge = ChargeAmount;
                                        double CompanyChargeAmount = ChargeAmount;
                                        ChargeAmount = _markup.ProcessHotelCancelationPrice_WithMarkup(ref MAinAgentCharge, SearchParam.NoofNights, SearchParam.CityID, SearchParam.CountryID.ToString(), ChargeAmount, isPerce, B2CMarkup, AgentPercentage, AgentMarkupAmount, IsParentId, SubAgentMarkup, ref Vatpolicy);


                                        //Double CancellationMarkup = Math.Round(ChargeAmount, 2);
                                        //Double CancellationMarkup = 0.0;
                                        //if (isPerce)
                                        //{
                                        //    if (IsB2C == false && (B2CMarkup == 0 || (AgentPercentage > 0)))
                                        //    {
                                        //        CancellationMarkup = Math.Round(((ChargeAmount * AgentPercentage) / 100), 2);
                                        //    }
                                        //}
                                        //else
                                        //    CancellationMarkup = (ChargeAmount == 0 ? ChargeAmount : AgentMarkupAmount);
                                        //ChargeAmount = ChargeAmount + CancellationMarkup;
                                        //AgentChargeAmount = ChargeAmount;

                                        //if (IsParentId == 0)
                                        //{
                                        //    Double SubCancellationAgentMarkup = Math.Round((Math.Round((ChargeAmount * SubAgentMarkup), 2) / 100), 2);
                                        //    ChargeAmount = Math.Round((ChargeAmount + SubCancellationAgentMarkup), 2);
                                        //}
                                        //Double CancellationAgentMarkup = 0;
                                        //if (IsB2C == true)
                                        //{
                                        //    CancellationAgentMarkup = Math.Round((Math.Round((ChargeAmount * AgentPercentage), 2) / 100), 2);
                                        //    ChargeAmount = Math.Round(ChargeAmount + CancellationAgentMarkup, 2);
                                        //}

                                        //ChargeAmount = Math.Round((ChargeAmount + CancellationAgentMarkup), 2);
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
                                        //    FromDate = (Convert.ToString(drCancellationPolicy[c]["ToDate"]) != string.Empty ? Convert.ToDateTime(drCancellationPolicy[c]["ToDate"]) : DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null));// Convert.ToDateTime(drCancellationPolicy[c]["ToDate"]);
                                        //}
                                        //if (c != 0 && c == drCancellationPolicy.GetLength(0) - 1)
                                        //    ToDate = ToDate.AddDays(CutOffDays);
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
                                            drHotelCancellation["SupplierName"] = "oth";
                                            drHotelCancellation["OwnsystemHotelId"] = internalHotelId; //ownsystemhotelId; //for extanet 27-08-2024 smit
                                            drHotelCancellation["RoomNo"] = Convert.ToString(drRoomCategories[rc]["RoomNo"]);
                                            //drHotelCancellation["RoomTypeId"] = Convert.ToString(drRoomCategories[rc]["RaynaRoomTypeName"]);
                                            drHotelCancellation["RoomTypeId"] = Convert.ToString(drRoomCategories[rc]["RoomTypeID"]);
                                            drHotelCancellation["RoomTypeName"] = RoomDesc;
                                            drHotelCancellation["MealId"] = Convert.ToString(drRoomCategories[rc]["BoardBasis"]);
                                            drHotelCancellation["FromDate"] = FromDate;
                                            drHotelCancellation["ToDate"] = ToDate;
                                            drHotelCancellation["CompanyCancellationPrice"] = MAinAgentCharge;
                                            drHotelCancellation["AgentCancellationPrice"] = ChargeAmount;
                                            drHotelCancellation["VatdiffAmount"] = Vatpolicy;
                                            drHotelCancellation["BoardTypeID"] = BoardTypeID;
                                            drHotelCancellation["SupplierCurrency"] = CurrencyCode;
                                            drHotelCancellation["BuyingPrice"] = CompanyChargeAmount;
                                            dtCancellation.Rows.Add(drHotelCancellation);
                                        }
                                    }
                                }



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
                                        double VarRateBreackUP = 0;
                                        freeStay = "no";
                                        if (freeStay == "no")
                                        {
                                            DateGross = Math.Round(Convert.ToDouble(DTdrdate[dtdate]["PriceWithoutExtraBed"]), 6);
                                            #region Vat

                                            DateGross = _markup.ProcessHotelRateBreakupPrice_WithMarkup(SearchParam.NoofNights, SearchParam.CityID, SearchParam.CountryID.ToString(), DateGross, B2CMarkup, IsParentId, isPerce, AgentPercentage, AgentMarkupAmount, SubAgentMarkup, ref VarRateBreackUP);

                                            #endregion
                                            //double b2cDateGrossMarkup = Math.Round((Math.Round((DateGross * B2CMarkup), 2) / 100), 2);
                                            //DateGross = Math.Round(DateGross + b2cDateGrossMarkup, 2);
                                            //if (IsB2C == false)
                                            //{
                                            //    if (IsParentId == 0)
                                            //    {
                                            //        double b2bSubDateGrossMarkup = Math.Round((Math.Round((DateGross * SubAgentMarkup), 2) / 100), 2);
                                            //        DateGross = Math.Round(DateGross + b2bSubDateGrossMarkup, 2);
                                            //    }
                                            //    if (isPerce)
                                            //    {
                                            //        double b2bDateGrossMarkup = Math.Round((Math.Round((DateGross * AgentPercentage), 2) / 100), 2);
                                            //        DateGross = Math.Round(DateGross + b2bDateGrossMarkup, 2);
                                            //    }
                                            //    else
                                            //        DateGross = (DateGross == 0 ? DateGross : Math.Round(DateGross + AgentMarkupAmount, 2));
                                            //}
                                        }

                                        WeekColumn = Convert.ToString(Convert.ToDateTime(datetime).DayOfWeek).Substring(0, 3);
                                        DataRow drHotelRoomRateBreackUp = DtHotelRoomRateBreackUp.NewRow();
                                        drHotelRoomRateBreackUp["RoomNo"] = Convert.ToString(drRoomCategories[rc]["RoomNo"]);
                                        drHotelRoomRateBreackUp["SupplierName"] = "oth";
                                        drHotelRoomRateBreackUp["OwnsystemHotelId"] = internalHotelId; //ownsystemhotelId; //for extanet 27-08-2024 smit
                                        drHotelRoomRateBreackUp["ContractID"] = 0;
                                        drHotelRoomRateBreackUp["RoomTypeName"] = Convert.ToString(DTdrdate[dtdate]["RaynaRoomTypeName"]);
                                        //drHotelRoomRateBreackUp["RoomTypeId"] = Convert.ToString(DTdrdate[dtdate]["RaynaRoomTypeName"]);
                                        drHotelRoomRateBreackUp["RoomTypeId"] = Convert.ToString(DTdrdate[dtdate]["RoomTypeID"]);
                                        drHotelRoomRateBreackUp["MealId"] = Convert.ToString(drRoomCategories[rc]["BoardBasis"]);
                                        drHotelRoomRateBreackUp["Meal"] = Convert.ToString(drRoomCategories[rc]["BoardBasis"]);
                                        drHotelRoomRateBreackUp["BookingStatus"] = Convert.ToString(DTdrdate[dtdate]["BookingStatus"]);
                                        drHotelRoomRateBreackUp["DateOfWeek"] = WeekColumn;
                                        drHotelRoomRateBreackUp["Date"] = datetime;
                                        drHotelRoomRateBreackUp["Rate"] = DateGross;
                                        drHotelRoomRateBreackUp["VatdiffAmount"] = VarRateBreackUP;
                                        drHotelRoomRateBreackUp["RateType"] = "A Rate";
                                        drHotelRoomRateBreackUp["OrderNo"] = cntno;
                                        drHotelRoomRateBreackUp["BoardTypeID"] = BoardTypeID;
                                        drHotelRoomRateBreackUp["FinalRateType"] = Convert.ToString(DTdrdate[dtdate]["FinalRateType"]);
                                        drHotelRoomRateBreackUp["CompanyRate"] = Math.Round(Convert.ToDecimal(DTdrdate[dtdate]["PriceWithoutExtraBed"]), 6);
                                        drHotelRoomRateBreackUp["SupplierCurrency"] = CurrencyCode;
                                        drHotelRoomRateBreackUp["BuyingPrice"] = DateGross;
                                        drHotelRoomRateBreackUp["Total"] = Math.Round(Convert.ToDecimal(DTdrdate[dtdate]["PriceWithoutROE"]), 6); ;

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
                                        {
                                            DateGross = Math.Round(Convert.ToDouble(DTdSurchargerdate[dtdate]["Price"]), 6);
                                            double VarRateBreackUP = 0;
                                            DateGross = _markup.ProcessHotelRateBreakupPrice_WithMarkup(SearchParam.NoofNights, SearchParam.CityID, SearchParam.CountryID.ToString(), DateGross, B2CMarkup, IsParentId, isPerce, AgentPercentage, AgentMarkupAmount, SubAgentMarkup, ref VarRateBreackUP);
                                        }

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
                                        drHotelRoomRateBreackUp["SupplierName"] = "oth";
                                        drHotelRoomRateBreackUp["OwnsystemHotelId"] = internalHotelId; //ownsystemhotelId; //for extanet 27-08-2024 smit
                                        drHotelRoomRateBreackUp["ContractID"] = 0;
                                        drHotelRoomRateBreackUp["RoomTypeName"] = Convert.ToString(DTdrdate[0]["RaynaRoomTypeName"]);
                                        //drHotelRoomRateBreackUp["RoomTypeId"] = Convert.ToString(DTdrdate[0]["RaynaRoomTypeName"]);
                                        drHotelRoomRateBreackUp["RoomTypeId"] = Convert.ToString(DTdrdate[0]["RoomTypeID"]);
                                        drHotelRoomRateBreackUp["MealId"] = Convert.ToString(drRoomCategories[rc]["BoardBasis"]);
                                        drHotelRoomRateBreackUp["Meal"] = Convert.ToString(drRoomCategories[rc]["BoardBasis"]);
                                        drHotelRoomRateBreackUp["BookingStatus"] = Convert.ToString(drRoomCategories[rc]["BookingStatus"]);
                                        drHotelRoomRateBreackUp["DateOfWeek"] = WeekColumn;
                                        drHotelRoomRateBreackUp["Date"] = datetime;
                                        drHotelRoomRateBreackUp["Rate"] = DateGross;
                                        drHotelRoomRateBreackUp["RateType"] = Convert.ToString(DTdSurchargerdate[dtdate]["SurchargeName"]);
                                        drHotelRoomRateBreackUp["OrderNo"] = cntno;
                                        drHotelRoomRateBreackUp["BoardTypeID"] = BoardTypeID;
                                        drHotelRoomRateBreackUp["FinalRateType"] = Convert.ToString(DTdSurchargerdate[dtdate]["FinalRateType"]);
                                        drHotelRoomRateBreackUp["VatdiffAmount"] = 0;
                                        drHotelRoomRateBreackUp["CompanyRate"] = Math.Round(Convert.ToDecimal(DTdSurchargerdate[dtdate]["Price"]), 6);
                                        drHotelRoomRateBreackUp["SupplierCurrency"] = CurrencyCode;
                                        drHotelRoomRateBreackUp["BuyingPrice"] = DateGross;
                                        if (GTAResponse.Tables[4].Columns.Contains("PriceWithoutROE"))
                                            drHotelRoomRateBreackUp["Total"] = Math.Round(Convert.ToDecimal(DTdSurchargerdate[dtdate]["PriceWithoutROE"]), 6);
                                        else
                                            drHotelRoomRateBreackUp["Total"] = Math.Round(Convert.ToDecimal(DTdSurchargerdate[dtdate]["Price"]), 6);

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

                                double ItemPrice = Math.Round(Convert.ToDouble(drRoomCategories[rc]["Price"]), 6);
                                double ItemOriginalPrice = Math.Round(Convert.ToDouble(drRoomCategories[rc]["OriginalPrice"]), 6);

                                double ItemSurchargePrice = Math.Round(Convert.ToDouble(drRoomCategories[rc]["SurchargePrice"]), 6);
                                double VatOnRateBrackUpnew = 0;
                                //double surchargewithmarkup = Common.ProcessHotelRateBreakupPrice_WithMarkup(1, SearchParam.CityID, SearchParam.CountryId, ItemSurchargePrice, B2CMarkup, IsParentId, isPerce, AgentPercentage, AgentMarkupAmount, SubAgentMarkup, ref VatOnRateBrackUpnew);
                                drhotelRoom["CompanyBuyingPrice"] = ItemPrice + ItemSurchargePrice;
                                drhotelRoom["SupplierCurrency"] = CurrencyCode;
                                drhotelRoom["BuyingPrice"] = Convert.ToString(drRoomCategories[rc]["PriceWithoutROE"]);

                                drhotelRoom["EssentialInformation"] = Convert.ToString(GTAResponse.Tables[0].Rows[h]["SPUI"]);
                                if (rc == 0)
                                {
                                    LowestRate = Math.Round(Convert.ToDouble(DTdrdate[0]["Price"]), 6);
                                    OriginalLowestRate = Math.Round(Convert.ToDouble(DTdrdate[0]["OriginalPrice"]), 6);
                                    //  BookingStatus = Convert.ToString(DTdrdate[0]["BookingStatus"]);
                                }
                                for (int dtdate = 0; dtdate <= DTdrdate.GetLength(0) - 1; dtdate++)
                                {
                                    if (BookingStatus == "" || BookingStatus == string.Empty || BookingStatus.ToLower().Trim() == "onrequest")
                                        BookingStatus = Convert.ToString(DTdrdate[dtdate]["BookingStatus"]);
                                }
                                if (Convert.ToString(GTAResponse.Tables[0].Rows[h]["FreeDiscount"]) != "" && Convert.ToString(GTAResponse.Tables[0].Rows[h]["FreeDiscount"]) != "0")
                                {
                                    string DiscountPer = Convert.ToString(GTAResponse.Tables[0].Rows[h]["FreeDiscount"]);
                                    DiscountPerTtext = Convert.ToString(GTAResponse.Tables[0].Rows[h]["FreeDiscount"]);// ""Save " + Convert.ToString(DiscountPer) + "% in discounts*";
                                    if (ItemPrice != ItemOriginalPrice)
                                    {
                                        drhotelRoom["IncudedOfferDiscount"] = ItemPrice;
                                        drhotelRoom["GrossWitohutDiscount"] = ItemOriginalPrice;
                                        drhotelRoom["DiscountPer"] = DiscountPer;
                                        drhotelRoom["DiscountPerText"] = DiscountPerTtext;

                                        //drhotelRoom["EssentialInformation"] = Convert.ToString(GTAResponse.Tables[0].Rows[h]["EssentialInformation"]);
                                        //drhotelRoom["EssentialInformation"] = Convert.ToString(GTAResponse.Tables[0].Rows[h]["SPUI"]);
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
                                    double b2cItemOriginalPriceMarkup = Math.Round((Math.Round((ItemOriginalPrice * B2CMarkup), 6) / 100), 6);
                                    ItemOriginalPrice = Math.Round(ItemOriginalPrice + b2cItemOriginalPriceMarkup, 6);
                                }
                                //Before Discount End 
                                drhotelRoom["CheckInDate"] = SearchParam.CheckInDate;
                                drhotelRoom["CheckOutDate"] = SearchParam.CheckOutDate;
                                drhotelRoom["CityId"] = SearchParam.CityID;
                                double surchargewithmarkup = _markup.ProcessHotelRateBreakupPrice_WithMarkup(1, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), ItemSurchargePrice, B2CMarkup, IsParentId, isPerce, AgentPercentage, AgentMarkupAmount, SubAgentMarkup, ref VatOnRateBrackUpnew);
                                drhotelRoom["CompanySellingPrice"] = ItemPrice + surchargewithmarkup;
                                #region  VAt
                                double VatdiffAmount = Convert.ToDouble(drhotelRoom["CompanySellingPrice"]);
                                DataTable dtvatlist1 = await _commonMethod.GetVatCountrylist(Convert.ToString(SearchParam.CountryID) == "" ? 0 : Convert.ToInt32(SearchParam.CountryID), Convert.ToInt32(SearchParam.CityID), "hotel");
                                if (dtvatlist1.Rows.Count > 0)
                                {
                                    drhotelRoom["CompanySellingPrice"] = Convert.ToDouble(drhotelRoom["CompanySellingPrice"]) + (Convert.ToDouble(_commonMethod.GetcalVatAmount(Convert.ToDecimal(drhotelRoom["CompanySellingPrice"]) - Convert.ToDecimal(drhotelRoom["CompanyBuyingPrice"]), Convert.ToDecimal(dtvatlist1.Rows[0]["IntRate"]))));
                                    //ItemPrice = Convert.ToDouble(drhotelRoom["CompanySellingPrice"]);
                                }
                                VatdiffAmount = Convert.ToDouble(drhotelRoom["CompanySellingPrice"]) - VatdiffAmount;
                                ItemPrice = ItemPrice + VatdiffAmount;
                                #endregion
                                if (IsB2C == false)
                                {
                                    if (IsParentId == 0)
                                    {
                                        drhotelRoom["AgentBuyingPrice"] = ItemPrice;
                                        double b2bSubMarkup = Math.Round((Math.Round(((ItemPrice) * SubAgentMarkup), 6) / 100), 6);
                                        ItemPrice = Math.Round(ItemPrice + b2bSubMarkup, 6);

                                        //Before Discount  Start
                                        if (Convert.ToString(drhotelRoom["IncudedOfferDiscount"]) != "" && Convert.ToDouble(drhotelRoom["IncudedOfferDiscount"]) != Convert.ToDouble(drhotelRoom["GrossWitohutDiscount"]))
                                        {
                                            double b2bOriginalSubMarkup = Math.Round((Math.Round((ItemOriginalPrice * SubAgentMarkup), 6) / 100), 6);
                                            ItemOriginalPrice = Math.Round(ItemOriginalPrice + b2bOriginalSubMarkup, 6);
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
                                    double b2bMarkup = Math.Round((Math.Round((ItemPrice * B2CMarkup), 6) / 100), 6);
                                    ItemPrice = ItemPrice + surchargewithmarkup;

                                    ItemPrice = ItemPrice + b2bMarkup;

                                    //Before Discount  Start
                                    if (Convert.ToString(drhotelRoom["IncudedOfferDiscount"]) != "" && Convert.ToDouble(drhotelRoom["IncudedOfferDiscount"]) != Convert.ToDouble(drhotelRoom["GrossWitohutDiscount"]))
                                    {
                                        double b2bOriginalMarkup = 0.0;
                                        if (isPerce)
                                            b2bOriginalMarkup = Math.Round((Math.Round((ItemOriginalPrice * AgentPercentage), 6) / 100), 6);
                                        else
                                            b2bOriginalMarkup = AgentMarkupAmount;
                                        ItemOriginalPrice = ItemOriginalPrice + ItemSurchargePrice;
                                        ItemOriginalPrice = ItemOriginalPrice + b2bOriginalMarkup;
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
                                    drhotelRoom["VatdiffAmount"] = VatdiffAmount;
                                    //Before Discount  Start
                                    if (Convert.ToString(drhotelRoom["IncudedOfferDiscount"]) != "" && Convert.ToDouble(drhotelRoom["IncudedOfferDiscount"]) != Convert.ToDouble(drhotelRoom["GrossWitohutDiscount"]))
                                    {
                                        drhotelRoom["GrossWitohutDiscount"] = ItemOriginalPrice;
                                    }
                                    //Before Discount End 
                                    drhotelRoom["SupplierName"] = "oth";
                                }
                                DtHotelRoom.Rows.Add(drhotelRoom);

                                ItemPrice_Text = Convert.ToString(ItemPrice);
                                if (rc == 0)
                                {
                                    if (isPerce)
                                    {
                                        if (AgentPercentage > 0)
                                        {
                                            double b2bMarkup = Math.Round((Math.Round((LowestRate * AgentPercentage), 6) / 100), 6);
                                            LowestRate = Math.Round(LowestRate + b2bMarkup, 6);
                                            double b2bOriginalMarkup = Math.Round((Math.Round((OriginalLowestRate * AgentPercentage), 6) / 100), 6);
                                            OriginalLowestRate = Math.Round(OriginalLowestRate + b2bOriginalMarkup, 6);
                                        }
                                    }
                                    else
                                    {
                                        LowestRate = Math.Round(LowestRate + AgentMarkupAmount, 6);
                                        OriginalLowestRate = Math.Round(OriginalLowestRate + AgentMarkupAmount, 6);
                                    }
                                    double b2cMarkup = Math.Round((Math.Round((LowestRate * B2CMarkup), 6) / 100), 6);
                                    LowestRate = Math.Round(LowestRate + b2cMarkup, 6);

                                    double b2cOriginalMarkup = Math.Round((Math.Round((OriginalLowestRate * B2CMarkup), 6) / 100), 6);
                                    OriginalLowestRate = Math.Round(OriginalLowestRate + b2cOriginalMarkup, 6);
                                    #region  VAt
                                    VatdiffAmount = LowestRate;
                                    dtvatlist1 = await _commonMethod.GetVatCountrylist(Convert.ToString(SearchParam.CountryID) == "" ? 0 : Convert.ToInt32(SearchParam.CountryID), Convert.ToInt32(SearchParam.CityID), "hotel");
                                    if (dtvatlist1.Rows.Count > 0)
                                    {
                                        LowestRate = Convert.ToDouble(LowestRate) + (Convert.ToDouble(_commonMethod.GetcalVatAmount(Convert.ToDecimal(LowestRate) - Convert.ToDecimal(drhotelRoom["CompanyBuyingPrice"]), Convert.ToDecimal(dtvatlist1.Rows[0]["IntRate"]))));
                                    }
                                    #endregion
                                    if (IsParentId == 0)
                                    {
                                        double b2bSubMarkup = Math.Round((Math.Round((LowestRate * SubAgentMarkup), 6) / 100), 6);
                                        LowestRate = Math.Round(LowestRate + b2bSubMarkup, 6);
                                        double b2bOriginalSubMarkup = Math.Round((Math.Round((OriginalLowestRate * SubAgentMarkup), 6) / 100), 6);
                                        OriginalLowestRate = Math.Round(OriginalLowestRate + b2bOriginalSubMarkup, 6);
                                    }


                                    int NoOfNoght = Convert.ToInt32(SearchParam.NoofNights);
                                }
                            }

                            string LocationText = CityName;
                            try
                            {
                                LocationText = Convert.ToString(GTAResponse.Tables[0].Rows[h]["Location"]);
                            }
                            catch (Exception ex)
                            {
                                await _exception.Error(ex);

                            }
                            //#region "Markup Calulation"
                            //double VatLowestRate = 0;
                            //LowestRate = Common.ProcessHotelRateBreakupPrice_WithMarkup(SearchParam.NoofNights, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), LowestRate, B2CMarkup, IsParentId, isPerce, AgentPercentage, AgentMarkupAmount, SubAgentMarkup, ref VatLowestRate);

                            //#endregion
                            drXmlHotel["Favorite"] = 0;
                            if (Rating == "" || Rating == "0")
                                Rating = "1";
                            CityName = Convert.ToString(SearchParam.CityName);
                            //GTAImage1 = (ConfigurationManager.AppSettings["staticimagepath"] + "/Images/Dubai/" + ownsystemhotelId + "/1-ThumbNail.jpg");
                            GTAImage1 = Convert.ToString(_keySettings.staticimagepath).ToLower() + "/Images/Dubai/" + ownsystemhotelId + "/1-ThumbNail.jpg";

                            //string CurrencyCode = "USD";// Convert.ToString(ResponseOth[h].CurrencyCode);

                            drXmlHotel["GIITACode"] = giitaCode;
                            drXmlHotel["OwnsystemHotelId"] = internalHotelId; //ownsystemhotelId; //for extanet 27-08-2024 smit
                            drXmlHotel["RaynaId"] = ownsystemhotelId;
                            drXmlHotel["SupplierCurrency"] = CurrencyCode;
                            drXmlHotel["XmlHotelCity"] = CityId;
                            drXmlHotel["XMLHotelCode"] = internalHotelId;//#Due to Ticket#13957
                            drXmlHotel["HotelName"] = HotelName;
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
                            drXmlHotel["SellingPrice"] = LowestRate;
                            drXmlHotel["FinalSellingPrice"] = LowestRate;
                            drXmlHotel["BookingStatus"] = BookingStatus;
                            drXmlHotel["SupplierName"] = "oth";
                            drXmlHotel["OfficeID"] = 0;
                            drXmlHotel["ContractID"] = 0;
                            drXmlHotel["Reviews"] = 0;
                            drXmlHotel["UniqueNo"] = uniqueno;
                            drXmlHotel["IsShowSupplier"] = blnIsShowSupplier;
                            drXmlHotel["VatdiffAmount"] = 0;
                            drXmlHotel["MasterSupplierId"] = Convert.ToInt32(MasterSupplierID);

                            drXmlHotel["ReservationEmail"] = Convert.ToString(GTAResponse.Tables[0].Rows[h]["ReservationEmail"]);
                            drXmlHotel["HotelContactNo"] = Convert.ToString(GTAResponse.Tables[0].Rows[h]["HotelContactNo"]);
                            DtHotel.Rows.Add(drXmlHotel);
                            //Int64 giataid2 = 0;
                            // Options ops = (await _supplierRes.GetCommonRoomForSuppliers(Convert.ToInt64(giitaCode), tokenData, advancedOptions, LowestRate, drXmlHotel, DtHotelRoom, dtCancellation, DtHotelRoomRateBreackUp, SearchParam));
                            Options ops = (await _supplierRes.GetCommonRoomForSuppliers(Convert.ToInt64(giitaCode), commonSearch.tokenData, commonSearch.advancedOptions, LowestRate, drXmlHotel, DtHotelRoom, dtCancellation, DtHotelRoomRateBreackUp, SearchParam));

                            if (ops != null)
                                options.Add(ops);
                            BookingStatus = "";
                        }
                    }
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
                        if (SearchParam != null && ResProccessTimerLogActive == "1" && timer_room_Oth.ElapsedMilliseconds > Convert.ToInt32(ResProccessTimeValueforSaveLog))
                        {
                            timer_room_Oth.Stop();
                            SearchParam.SupplierTime += ",|Finish:OTH Process Response  " + "|Time :" + timer_room_Oth.Elapsed.ToString();
                            // Task.Run(() => FireAway_SQL_ResPorcessTimeLog("", timer_room_Oth.Elapsed.ToString(), "OTH", SearchParam.SearchKey, 1, SearchParam.AgentID)); //plzchange
                        }
                    }
                    catch (Exception x)
                    {
                        await _exception.Error(x);
                    }
                    #endregion
                    //GC.Collect();


                }
            }
            return options;

        }
        public async Task<List<Options>> Oth_HotelSearchWithRooms(HotelX item, AdvancedOptions advancedOptions, TokenData tokenData, DataSet GTAResponse, DataTable dtAgentDetail, DataTable dtSuplierMarkup, HotelSearchParameter SearchParam, DataTable dtMarkupDetails, int agent, CommonSearch commonSearch, string Response, string ProccessCutOffTime, Int32 processTimeout, string ResProccessTimerLogActive, string ResProccessTimeValueforSaveLog)
        {
            System.Diagnostics.Stopwatch timer_room_Oth = new System.Diagnostics.Stopwatch();
            timer_room_Oth.Start();
            List<Options> options = new List<Options>();

            if (GTAResponse != null && GTAResponse.Tables.Count > 0 && GTAResponse.Tables[0].Rows.Count > 0 || Response != "")
            {
                DataTable DtHotel = _commonMethod.GenerateDataTableNewV2();

                DataTable dtHotelAllocation = _commonMethod.GenerateHotelAllocationTableNewV2();
                DataTable dtComplementary = _commonMethod.GenerateComplementaryTableNewV2();
                DataTable dtCommonInfo = _commonMethod.GenerateCommonInfoTableNewV2();
                DataTable dtHotelMinStay = _commonMethod.GenerateHotelMinStayNewV2();
                DataSet dsTemp = new DataSet();

                int CutOffDays = XMLHotelBookingCommon.CutOffDays;
                CutOffDays = _commonMethod.GetCutOffDays(dtSuplierMarkup, Convert.ToString(_keySettings.IsWithinearth) == "1" ? "withinearth" : "oth");
                double SubAgentMarkup = 0;
                int IsParentId = 1;
                if (dtAgentDetail != null)
                {
                    IsParentId = Convert.ToInt32(dtAgentDetail.Rows[0]["IsParentId"]);
                    SubAgentMarkup = Convert.ToDouble(dtAgentDetail.Rows[0]["B2CMarkupPer"]);
                }
                try
                {
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
                    string giitaCode = "";
                    string DiscountPerTtext = "";

                    string Longitude = "";
                    string Latitude = "";
                    int ReviewCount = 0;
                    int ReviewRating = 0;
                    int SortSeq = 9999;
                    DateTime curDate = _commonMethod.GetTimeZoneDate(SearchParam.CurrentDate);
                    if (SearchParam.Filters.IsShowRooms == 1)
                    {
                        if (GTAResponse == null || GTAResponse.Tables[0] == null || GTAResponse.Tables[0].Rows.Count == 0)
                            return options;
                    }
                    double AgentPercentage = 0.0;
                    Boolean blnIsShowSupplier = false;
                    double AgentMarkupAmount = 0.0;
                    Boolean isPerce = true;
                    int MasterSupplierID = 0;
                    if (dtSuplierMarkup != null)
                    {
                        dtSuplierMarkup.DefaultView.RowFilter = "SupplierName='" + (Convert.ToString(_keySettings.IsWithinearth) == "1" ? "withinearth" : "oth") + "'";
                        if (dtSuplierMarkup.DefaultView.Count > 0)
                        {
                            try
                            {
                                AgentPercentage = Convert.ToDouble(dtSuplierMarkup.DefaultView[0]["AgentWiseMarkup"]);
                                blnIsShowSupplier = (Convert.ToInt16(dtSuplierMarkup.DefaultView[0]["IsShowSupplier"]) == 1 ? true : false);
                                MasterSupplierID = Convert.ToInt32(dtSuplierMarkup.DefaultView[0]["MasterSupplierId"]);
                            }
                            catch (Exception x)//k2
                            {
                                try
                                {
                                    AgentPercentage = Convert.ToDouble(dtSuplierMarkup.DefaultView[0]["AgentWiseMarkup"]);
                                    blnIsShowSupplier = (Convert.ToInt16(dtSuplierMarkup.DefaultView[0]["IsShowSupplier"]) == 1 ? true : false);
                                    MasterSupplierID = Convert.ToInt32(dtSuplierMarkup.DefaultView[0]["MasterSupplierId"]);
                                }
                                catch (Exception y)
                                {
                                    await _exception.Error(y, SearchParam.SearchKey);
                                }
                            }
                        }
                    }
                    ///////////@New List Code Added on 15-12-19


                    #region "When not Need Room"  

                    #region "Hotel Code Block "

                    if (SearchParam.Filters.IsShowRooms == 0)
                    {
                        List<OthListResponse> ResponseOth = new List<OthListResponse>();
                        if (Response != "")
                            ResponseOth = JsonSerializer.Deserialize<List<OthListResponse>>(Response);

                        if (ResponseOth == null || ResponseOth.Count == 0)
                            return options;
                        if (dtSuplierMarkup != null)
                        {
                            dtSuplierMarkup.DefaultView.RowFilter = "SupplierName='withinearth'";
                            if (dtSuplierMarkup.DefaultView.Count > 0)
                            {
                                AgentPercentage = Convert.ToDouble(dtSuplierMarkup.DefaultView[0]["AgentWiseMarkup"]);
                                blnIsShowSupplier = (Convert.ToInt16(dtSuplierMarkup.DefaultView[0]["IsShowSupplier"]) == 1 ? true : false);
                            }
                        }
                        if (ResponseOth.Count > 0)
                        {
                            item.ResponseType = "Success";
                        }
                        for (int h = 0; h <= ResponseOth.Count - 1; h++)
                        {
                            DataTable DtHotelRoom = _commonMethod.GenerateRoomtypeDataTableNewV2();
                            DataTable dtCancellation = _commonMethod.GenerateCancellationDataTableNewV2();
                            DataTable DtHotelRoomRateBreackUp = _commonMethod.GenerateoomRateBreackUpDataTableNewV2();
                            if (dtSuplierMarkup != null)
                            {
                                if (dtSuplierMarkup.DefaultView.Count > 0)
                                {
                                    MasterSupplierID = Convert.ToInt32(dtSuplierMarkup.DefaultView[0]["MasterSupplierId"]);
                                }
                            }
                            if (Convert.ToString(ResponseOth[h].GiitaId) == string.Empty || Convert.ToString(ResponseOth[h].GiitaId) == "0")
                                continue;
                            if (ResponseOth[h].SupplierId > 0)
                                MasterSupplierID = ResponseOth[h].SupplierId;

                            _markup.getAgetMarkupSetting("withinearth", Convert.ToString(ResponseOth[h].OwnsystemHotelId), dtSuplierMarkup, dtMarkupDetails, ref AgentPercentage, ref AgentMarkupAmount, ref blnIsShowSupplier, ref isPerce, tokenData.AgentId, Convert.ToInt32(ResponseOth[h].Rating), SearchParam.CityID, Convert.ToString(SearchParam.CountryID), SearchParam.CheckInDate);
                            double AgentCompanyPercentage = SearchParam.AgentCompanyPercentage;
                            bool IsB2C = Convert.ToBoolean(Convert.ToByte(SearchParam.IsB2BOrB2C));

                            double B2CMarkup = (IsB2C == false ? AgentCompanyPercentage : XMLHotelBookingCommon.B2CMarkup);
                            DataRow drXmlHotel = DtHotel.NewRow();
                            Int64 GiataId = ResponseOth[h].GiitaId;
                            drXmlHotel["Favorite"] = 0;

                            if (Convert.ToString(ResponseOth[h].Rating) == "" || Convert.ToString(ResponseOth[h].Rating) == "0")
                                ResponseOth[h].Rating = 1;
                            Rating = Convert.ToString(ResponseOth[h].Rating);
                            DataTable DtHotelPolicy = _commonMethod.GenerateCancellationDataTableNewV2();
                            double price = Convert.ToDouble(ResponseOth[h].FinalSellingPrice); //* ROE;
                            string CurrencyCode = Convert.ToString(ResponseOth[h].CurrencyCode);
                            Int32 ItemCode = ResponseOth[h].OwnsystemHotelId;
                            drXmlHotel["XMLCountry"] = Convert.ToString(SearchParam.CountryID);
                            drXmlHotel["XMLCity"] = Convert.ToString(SearchParam.CityID);
                            drXmlHotel["XmlHotelCity"] = Convert.ToString(ItemCode);
                            drXmlHotel["XMLHotelCode"] = ItemCode;
                            drXmlHotel["EssentialInformation"] = "";
                            drXmlHotel["Description"] = "";
                            drXmlHotel["SupplierName"] = "oth";
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
                            drXmlHotel["GIITACode"] = GiataId;
                            drXmlHotel["RaynaId"] = ItemCode;

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

                                _markup.GetHotelDataCommon(ref price, ref willAddOrNot, ref OwnsystemHotelId, ref RaynaID, ref GiataId, ref RatingMain, ref drXmlHotel, commonSearch.AgentId, commonSearch.TariffID, commonSearch.dsAgentDetails, commonSearch.supplierGiataMaster, SearchParam, commonSearch.dtCountryCityMarkup, commonSearch.B2CMarkup, commonSearch.IsParentId, commonSearch.SubAgentMarkup, commonSearch.SearchByHotel, commonSearch.HotelIDS);
                                if (willAddOrNot)
                                {
                                    #region "Room Logic"
                                    DataTable dt_HotelRoom = _commonMethod.GenerateRoomtypeDataTableNewV2();
                                    DataTable dt_Cancellation = _commonMethod.GenerateCancellationDataTableNewV2();
                                    DataTable dt_RateBreackUp = _commonMethod.GenerateoomRateBreackUpDataTableNewV2();
                                    #endregion
                                    decimal LowestPrice = Convert.ToDecimal(price).CalculatePrice(commonSearch.tokenData, commonSearch.advancedOptions, CurrencyCode);
                                    Options ops = (await _supplierRes.GetCommonRoomForSuppliers(Convert.ToInt64(GiataId), tokenData, advancedOptions, Convert.ToDouble(LowestPrice), drXmlHotel, DtHotelRoom, dtCancellation, DtHotelRoomRateBreackUp, SearchParam));
                                    if (ops != null)
                                        options.Add(ops);
                                }
                                #endregion
                            }
                        }
                    }
                    #endregion

                    #endregion
                    #region "When Need Room" 
                    else
                    {
                        if (GTAResponse.Tables[0].Rows.Count > 0)
                        {
                            item.ResponseType = "Success";
                        }
                        for (int h = 0; h < GTAResponse.Tables[0].Rows.Count; h++)
                        {
                            //Check Time If High then return NoCount
                            //if (ProccessCutOffTime == "1" && timer_room_Oth.ElapsedMilliseconds > processTimeout) // Disable (API group)3,4-Mar-2023
                            //{
                            //    //timer_room_Oth.Stop();
                            //    break;
                            //}
                            //--end
                            DataTable DtHotelRoom = _commonMethod.GenerateRoomtypeDataTableNewV2();
                            DataTable dtCancellation = _commonMethod.GenerateCancellationDataTableNewV2();
                            DataTable DtHotelRoomRateBreackUp = _commonMethod.GenerateoomRateBreackUpDataTableNewV2();

                            ReviewCount = 0;
                            ReviewRating = 0;

                            giitaCode = Convert.ToString(GTAResponse.Tables[0].Rows[h]["GiitaId"]);
                            Rating = Convert.ToString(GTAResponse.Tables[0].Rows[h]["Rating"]);
                            int SupplierId = Convert.ToInt32(GTAResponse.Tables[0].Rows[h]["SupplierId"]);
                            if (dtSuplierMarkup != null)
                            {
                                if (dtSuplierMarkup.DefaultView.Count > 0)
                                {
                                    MasterSupplierID = Convert.ToInt32(dtSuplierMarkup.DefaultView[0]["MasterSupplierId"]);

                                }
                            }
                            if (SupplierId > 0)
                                MasterSupplierID = SupplierId;
                            // Common.getAgetMarkupSetting(Convert.ToString(ConfigurationManager.AppSettings["IsWithinearth"]) == "1" ? "withinearth" : "oth", giitaCode, dtSuplierMarkup, dtMarkupDetails, ref AgentPercentage, ref AgentMarkupAmount, ref blnIsShowSupplier, ref isPerce, Convert.ToInt32(agent), Convert.ToInt32(Rating), SearchParam.CityID, Convert.ToString(SearchParam.CountryID));
                            string markupsuppliername = Convert.ToString(GTAResponse.Tables[0].Rows[0]["SupplierName"]) == "oth" ? ((_keySettings.IsWithinearth) == "1" ? "withinearth" : "oth") : Convert.ToString(GTAResponse.Tables[0].Rows[0]["SupplierName"]);
                            _markup.getAgetMarkupSetting("withinearth", giitaCode, dtSuplierMarkup, dtMarkupDetails, ref AgentPercentage, ref AgentMarkupAmount, ref blnIsShowSupplier, ref isPerce, Convert.ToInt32(agent), Convert.ToInt32(Rating), SearchParam.CityID, Convert.ToString(SearchParam.CountryID), SearchParam.CheckInDate);

                            double AgentCompanyPercentage = Convert.ToDouble(SearchParam.AgentCompanyPercentage);
                            bool IsB2C = Convert.ToBoolean(Convert.ToByte(SearchParam.IsB2BOrB2C));

                            double B2CMarkup = (IsB2C == false ? AgentCompanyPercentage : XMLHotelBookingCommon.B2CMarkup);
                            //if (IsB2C == true)
                            //{
                            //	AgentPercentage = 0;
                            //	AgentMarkupAmount = 0;
                            //}


                            DataRow drXmlHotel = DtHotel.NewRow();

                            RoomCancellation = "";
                            LowestRate = 0;
                            string uniqueno = Guid.NewGuid().ToString();
                            string ItemCode = Convert.ToString(GTAResponse.Tables[0].Rows[h]["raynaid"]);
                            //if (GTAResponse.Tables[11] != null && GTAResponse.Tables[11].Rows.Count > 0)
                            //{
                            //    DataRow[] drMin = GTAResponse.Tables[11].Select("raynaid='" + ItemCode + "'");
                            //    if (drMin.Length > 0)
                            //        continue;
                            //}
                            string Meal = Convert.ToString(GTAResponse.Tables[0].Rows[h]["BoardBasis"]);
                            string HotelName = Convert.ToString(GTAResponse.Tables[0].Rows[h]["HotelName"]);
                            string CurrencyCode = Convert.ToString(GTAResponse.Tables[0].Rows[h]["CurrencyCode"]);

                            string OfferCode = "";
                            ReviewCount = (Convert.ToString(GTAResponse.Tables[0].Rows[h]["ReviewCount"]) != string.Empty ? Convert.ToInt32(GTAResponse.Tables[0].Rows[h]["ReviewCount"]) : 0);
                            ReviewRating = (Convert.ToString(GTAResponse.Tables[0].Rows[h]["ReviewRating"]) != string.Empty ? Convert.ToInt32(GTAResponse.Tables[0].Rows[h]["ReviewRating"]) : 0);
                            string HotelInformation = Convert.ToString(GTAResponse.Tables[0].Rows[h]["SPUI"]);
                            string HotelDescription = Convert.ToString(GTAResponse.Tables[0].Rows[h]["Description"]);
                            internalHotelId = Convert.ToString(GTAResponse.Tables[0].Rows[h]["raynaid"]);

                            //ownsystemhotelId = Convert.ToString(GTAResponse.Tables[0].Rows[h]["raynaid"]);
                            ownsystemhotelId = Convert.ToString(GTAResponse.Tables[0].Rows[h]["OriginalRaynaid"]);//#Due to Ticket#13957

                            giitaCode = Convert.ToString(GTAResponse.Tables[0].Rows[h]["GiitaId"]);
                            Address = Convert.ToString(GTAResponse.Tables[0].Rows[h]["Address"]);
                            int Favorite = 0;// Convert.ToInt32(Favouritedt.Select("Favorite=" + ownsystemhotelId).GetLength(0) == 0 ? 0 : Convert.ToInt32(Favouritedt.Select("Favorite=" + ownsystemhotelId)[0]["Favorite"]));
                            drXmlHotel["Favorite"] = Favorite;
                            LocationDescription = Convert.ToString(GTAResponse.Tables[0].Rows[h]["Location"]);
                            Rating = Convert.ToString(GTAResponse.Tables[0].Rows[h]["Rating"]);
                            Chain = Convert.ToString(GTAResponse.Tables[0].Rows[h]["GroupName"]);
                            SortSeq = Convert.ToInt32(GTAResponse.Tables[0].Rows[h]["SortSequence"]);
                            if (Rating == "" || Rating == "0")
                                Rating = "1";
                            CityName = "Dubai City";
                            CountryId = "13063";
                            CityId = "13668";
                            Latitude = Convert.ToString(GTAResponse.Tables[0].Rows[h]["Latitude"]);
                            Longitude = Convert.ToString(GTAResponse.Tables[0].Rows[h]["Longitude"]);

                            //DataRow[] drComplementary = GTAResponse.Tables[6].Select("RaynaId=" + ItemCode + "");
                            ////Need to Add Complementry Logic here
                            //if (drComplementary.GetLength(0) > 0)
                            //{
                            //    //for(int cm=0;cm<(drComplementary.GetLength(0) > 5?5: drComplementary.GetLength(0));cm++)
                            //    for (int cm = 0; cm < drComplementary.GetLength(0); cm++)
                            //    {
                            //        DataRow dtr = dtComplementary.NewRow();
                            //        dtr["OwnsystemHotelId"] = Convert.ToString(drComplementary[cm]["RaynaId"]);
                            //        dtr["ComplementaryName"] = Convert.ToString(drComplementary[cm]["ComplementaryName"]);
                            //        dtr["ComplementaryText"] = Convert.ToString(drComplementary[cm]["ComplementaryText"]);
                            //        dtr["ImagePath"] = Convert.ToString(drComplementary[cm]["ImagePath"]);
                            //        dtComplementary.Rows.Add(dtr);
                            //    }
                            //}

                            //DataRow[] drCommonInfo = GTAResponse.Tables[12].Select("RaynaId=" + ItemCode + "");
                            ////Need to Add common information(hotel description, common policy etc) Logic here

                            //if (drCommonInfo.GetLength(0) > 0)
                            //{
                            //    //for(int cm=0;cm<(drComplementary.GetLength(0) > 5?5: drComplementary.GetLength(0));cm++)
                            //    for (int cm = 0; cm < drCommonInfo.GetLength(0); cm++)
                            //    {
                            //        DataRow dtr = dtCommonInfo.NewRow();
                            //        dtr["OwnsystemHotelId"] = Convert.ToString(drCommonInfo[cm]["RaynaId"]);
                            //        dtr["CommonPolicyName"] = Convert.ToString(drCommonInfo[cm]["CommonPolicyName"]);
                            //        dtr["CommonPolicyManagementText"] = Convert.ToString(drCommonInfo[cm]["CommonPolicyManagementText"]);
                            //        dtCommonInfo.Rows.Add(dtr);
                            //    }
                            //}

                            DataRow[] drRoomCategories = GTAResponse.Tables[8].Select("RaynaId=" + internalHotelId + "");
                            int PreviousRoomNo = 1;
                            int LogicalCounter = 0;
                            for (int rc = 0; rc <= drRoomCategories.GetLength(0) - 1; rc++)
                            {
                                //Room wise MLOS added on 29-6-2022
                                if (GTAResponse.Tables[11] != null && GTAResponse.Tables[11].Rows.Count > 0)
                                {
                                    DataRow[] drMinx = GTAResponse.Tables[11].Select("raynaid='" + ItemCode + "' and RoomTypeId='" + Convert.ToString(0) + "'");//RoomTypeId=0 means for All rooms
                                    if (drMinx.Length > 0)
                                        continue;
                                    DataRow[] drMin = GTAResponse.Tables[11].Select("raynaid='" + ItemCode + "' and RoomTypeId='" + Convert.ToString(drRoomCategories[rc]["RoomTypeID"]) + "'");
                                    if (drMin.Length > 0)
                                        continue;
                                }

                                DataRow drhotelRoom = DtHotelRoom.NewRow();
                                if (Convert.ToInt32(drRoomCategories[rc]["RoomNo"]) != PreviousRoomNo)
                                {
                                    LogicalCounter = 0;
                                }
                                PreviousRoomNo = Convert.ToInt32(drRoomCategories[rc]["RoomNo"]);
                                LogicalCounter++;
                                // BookingStatus = "";
                                string RoomDesc = Convert.ToString(drRoomCategories[rc]["RaynaRoomTypeName"]);
                                string RoomTypeID = Convert.ToString(drRoomCategories[rc]["RoomTypeID"]);
                                string BoardTypeID = Convert.ToString(drRoomCategories[rc]["BoardTypeID"]);
                                OfferCode = Convert.ToString(drRoomCategories[rc]["OfferCode"]);
                                string RoomId = Convert.ToString(drRoomCategories[rc]["RaynaRoomTypeName"]) + "-" + Convert.ToString(drRoomCategories[rc]["BoardBasis"]);
                                string MealId = Convert.ToString(drRoomCategories[rc]["BoardBasis"]);
                                drhotelRoom["OwnsystemHotelId"] = internalHotelId; //ownsystemhotelId; //for extanet 27-08-2024 smit
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
                                drhotelRoom["RoomTypeId"] = Convert.ToString(drRoomCategories[rc]["RoomTypeId"]); ;
                                drhotelRoom["MealId"] = Convert.ToString(drRoomCategories[rc]["BoardBasis"]);
                                //By manish
                                drhotelRoom["IsCombineRoom"] = false;
                                drhotelRoom["IsPolicyDynamic"] = false;
                                drhotelRoom["OfferDescription"] = Convert.ToString(drRoomCategories[rc]["OfferDescription"]) != "" ? Convert.ToString(drRoomCategories[rc]["OfferDescription"]) : OfferCode;

                                if (Convert.ToString(drRoomCategories[rc]["BoardBasis"]) == "")
                                {
                                    drRoomCategories[rc]["BoardBasis"] = "RO";
                                }
                                drXmlHotel["MealName"] = Convert.ToString(drRoomCategories[rc]["BoardBasis"]);

                                DataRow[] DTdrdate = GTAResponse.Tables[3].Select("RoomTypeID='" + RoomTypeID + "' and RaynaId=" + ItemCode + " and RoomNo=" + Convert.ToDouble(drRoomCategories[rc]["RoomNo"]) + " and BoardTypeID='" + BoardTypeID + "'");
                                //DataRow[] DTdSurchargerdate = GTAResponse.Tables[4].Select("RoomTypeID='" + RoomTypeID + "' and RaynaId=" + ItemCode + " and BoardTypeID='" + BoardTypeID + "' and RoomNo=" + Convert.ToDouble(drRoomCategories[rc]["RoomNo"]));
                                //New changes for Radis cache 27-MAY-2025
                                DataRow[] DTdSurchargerdate = Array.Empty<DataRow>();

                                if (GTAResponse.Tables.Count > 4 && GTAResponse.Tables[4] != null && GTAResponse.Tables[4].Rows.Count > 0)
                                {
                                    DTdSurchargerdate = GTAResponse.Tables[4].Select("RoomTypeID='" + RoomTypeID + "' and RaynaId=" + ItemCode + " and BoardTypeID='" + BoardTypeID + "' and RoomNo=" + Convert.ToDouble(drRoomCategories[rc]["RoomNo"]));
                                }

                                int ActualNoOfNight = Convert.ToInt32(DTdrdate.GetLength(0));
                                int ActualSurchargeNoOfNight = Convert.ToInt32(DTdSurchargerdate.GetLength(0));

                                drhotelRoom["AdultsOccupancy"] = Convert.ToString(DTdrdate[0]["RoomCapacity"]);
                                drhotelRoom["ChildOccupancy"] = Convert.ToString(DTdrdate[0]["NoOfChild"]);
                                drhotelRoom["MaxExtrabed"] = Convert.ToString(DTdrdate[0]["NoOfExtraBeds"]);
                                drhotelRoom["BoardTypeID"] = BoardTypeID;
                                drhotelRoom["OTHPromoCode"] = Convert.ToString(DTdrdate[0]["AppliedOfferCode"]);
                                //drhotelRoom["ExtraNight"] = IsFromPkg == 1 ? Convert.ToString(DTdrdate[0]["ExtraNight"]) : "0";
                                //drhotelRoom["ExtraNightDifference"] = IsFromPkg == 1 ? Convert.ToString(DTdrdate[0]["ExtraNightDifference"]) : "0";


                                //DataRow[] drCancellationPolicy = GTAResponse.Tables[9].Select
                                //    ("raynaid=" + ItemCode + " and (RoomTypeID='" + RoomTypeID + "' or RoomTypeID='0') and (BoardTypeID='" + BoardTypeID + "' or (BoardTypeID='0' and " + BoardTypeID + ">0))");

                                DataRow[] drCancellationPolicy = Array.Empty<DataRow>();//New changes for Radis cache 05-JUN-2025
                                if (GTAResponse.Tables.Count > 8 && GTAResponse.Tables[9] != null && GTAResponse.Tables[9].Rows.Count > 0)
                                    drCancellationPolicy = GTAResponse.Tables[9].Select
                                    ("raynaid=" + ItemCode + " and (RoomTypeID='" + RoomTypeID + "' or RoomTypeID='0') and (BoardTypeID='" + BoardTypeID + "' or (BoardTypeID='0' and " + BoardTypeID + ">0))");

                                //if (drCancellationPolicy.GetLength(0) == 0) //###### Comment this code on 05-JUN-2025 Due to Radis Cache. common policy case need to test if it's manage in above block or not. for now this code is comment.
                                //{
                                //    //drCancellationPolicy = GTAResponse.Tables[9].Select
                                //    //("raynaid=" + ItemCode + " and RaynaRoomTypeName=''");
                                //    drCancellationPolicy = GTAResponse.Tables[9].Select
                                //    ("raynaid=" + ItemCode + " and (RoomTypeID='0') and (BoardTypeID='0')");
                                //}
                                int intRoomCancellation = 0;

                                if (drCancellationPolicy.GetLength(0) == 0)
                                {
                                    //Default non-refundable cancellation policy
                                    double ItemBasePrice = Math.Round(Convert.ToDouble(drRoomCategories[rc]["Price"]), 6);
                                    double ItemSurchargePriceBasePrice = Math.Round(Convert.ToDouble(drRoomCategories[rc]["SurchargePrice"]), 6);

                                    if (ActualSurchargeNoOfNight != 0)
                                    {
                                        //ItemSurchargePriceBasePrice = (ItemSurchargePriceBasePrice / ActualSurchargeNoOfNight);
                                        ItemSurchargePriceBasePrice = (ItemSurchargePriceBasePrice);

                                    }
                                    double CacelationAmountData = ItemBasePrice;
                                    double ChargeAmount = CacelationAmountData;
                                    double CompanyChargeAmount = ChargeAmount;


                                    double Vatpolicy = ChargeAmount;
                                    double ChargeSurchargeAmount = ItemSurchargePriceBasePrice;
                                    double AgentChargeAmount = 0;
                                    //Double CancellationMarkup = Math.Round(ChargeAmount, 2);
                                    Double CancellationMarkup = 0.0;
                                    if (isPerce)
                                    {
                                        if (IsB2C == false && (B2CMarkup == 0 || (AgentPercentage > 0)))
                                        {
                                            CancellationMarkup = Math.Round(((ChargeAmount * AgentPercentage) / 100), 6);
                                        }
                                    }
                                    else
                                        CancellationMarkup = (ChargeAmount == 0 ? ChargeAmount : AgentMarkupAmount);
                                    ChargeAmount = ChargeAmount + CancellationMarkup;
                                    #region Vat
                                    double VatChargeAmount = ChargeAmount;
                                    //DataTable dtvatlist = await _commonMethod.GetVatCountrylist(Convert.ToString(SearchParam.CountryID) == "" ? 0 : Convert.ToInt32(SearchParam.CountryID), Convert.ToInt32(SearchParam.CityID), "hotel");
                                    //if (dtvatlist.Rows.Count > 0)
                                    //{
                                    //    ChargeAmount = ChargeAmount + (Convert.ToDouble(_commonMethod.GetcalVatAmount(Convert.ToDecimal(ChargeAmount) - Convert.ToDecimal(Vatpolicy), Convert.ToDecimal(dtvatlist.Rows[0]["IntRate"]))));
                                    //}
                                    VatChargeAmount = ChargeAmount - VatChargeAmount;
                                    #endregion
                                    AgentChargeAmount = ChargeAmount;


                                    double MainAgentPrice = ChargeAmount;
                                    if (IsParentId == 0)
                                    {
                                        Double SubCancellationAgentMarkup = Math.Round((Math.Round((ChargeAmount * SubAgentMarkup), 6) / 100), 6);
                                        ChargeAmount = Math.Round((ChargeAmount + SubCancellationAgentMarkup), 6);
                                    }
                                    Double CancellationAgentMarkup = 0;
                                    if (IsB2C == true)
                                    {
                                        CancellationAgentMarkup = Math.Round((Math.Round((ChargeAmount * AgentPercentage), 6) / 100), 6);
                                        ChargeAmount = Math.Round(ChargeAmount + CancellationAgentMarkup, 6);
                                    }

                                    ChargeAmount = Math.Round((ChargeAmount + CancellationAgentMarkup), 6);
                                    ChargeAmount = ChargeAmount + ChargeSurchargeAmount;
                                    CompanyChargeAmount = CompanyChargeAmount + ChargeSurchargeAmount;
                                    DataRow drHotelCancellation = dtCancellation.NewRow();
                                    drHotelCancellation["SupplierName"] = "oth";
                                    drHotelCancellation["OwnsystemHotelId"] = internalHotelId; //ownsystemhotelId; //for extanet 27-08-2024 smit
                                    drHotelCancellation["RoomNo"] = Convert.ToString(drRoomCategories[rc]["RoomNo"]);
                                    //drHotelCancellation["RoomTypeId"] = Convert.ToString(drRoomCategories[rc]["RaynaRoomTypeName"]);
                                    drHotelCancellation["RoomTypeId"] = Convert.ToString(drRoomCategories[rc]["RoomTypeID"]);
                                    drHotelCancellation["RoomTypeName"] = RoomDesc;
                                    drHotelCancellation["MealId"] = Convert.ToString(drRoomCategories[rc]["BoardBasis"]);
                                    //drHotelCancellation["FromDate"] = FromDate;
                                    //drHotelCancellation["ToDate"] = ToDate;
                                    drHotelCancellation["FromDate"] = DateTime.Now.Date;
                                    drHotelCancellation["ToDate"] = DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null);
                                    drHotelCancellation["CompanyCancellationPrice"] = MainAgentPrice;
                                    drHotelCancellation["AgentCancellationPrice"] = ChargeAmount;
                                    //drHotelCancellation["BoardTypeID"] = VatChargeAmount;
                                    drHotelCancellation["VatdiffAmount"] = Vatpolicy;
                                    drHotelCancellation["BoardTypeID"] = BoardTypeID;
                                    drHotelCancellation["SupplierCurrency"] = CurrencyCode;
                                    drHotelCancellation["BuyingPrice"] = CompanyChargeAmount;

                                    dtCancellation.Rows.Add(drHotelCancellation);
                                }
                                else
                                {
                                    for (int c = drCancellationPolicy.GetLength(0) - 1; c >= 0; c--)
                                    {
                                        double ItemBasePrice = Math.Round(Convert.ToDouble(drRoomCategories[rc]["Price"]), 6);
                                        double ItemSurchargePriceBasePrice = Math.Round(Convert.ToDouble(drRoomCategories[rc]["SurchargePrice"]), 6);
                                        double VatOnRateBrackUp_new = 0;
                                        double surchargewithmarkup_policy = _markup.ProcessHotelRateBreakupPrice_WithMarkup(1, SearchParam.CityID, SearchParam.CountryID.ToString(), ItemSurchargePriceBasePrice, B2CMarkup, IsParentId, isPerce, AgentPercentage, AgentMarkupAmount, SubAgentMarkup, ref VatOnRateBrackUp_new);

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
                                            ItemSurchargePriceBasePrice = (ItemSurchargePriceBasePrice);
                                        }
                                        double CacelationAmountData = ItemBasePrice;
                                        if (cancellationNights != 0)
                                        {
                                            CacelationAmountData = (CacelationAmountData / ActualNoOfNight) * cancellationNights;
                                        }
                                        double ChargeAmount = (CacelationAmountData * CancellationPercentage) / 100;

                                        double ChargeSurchargeAmount = (surchargewithmarkup_policy * CancellationPercentage) / 100;
                                        double AgentChargeAmount = 0;
                                        double Vatpolicy = 0;
                                        double MAinAgentCharge = ChargeAmount;
                                        double CompanyChargeAmount = ChargeAmount;
                                        ChargeAmount = _markup.ProcessHotelCancelationPrice_WithMarkup(ref MAinAgentCharge, SearchParam.NoofNights, SearchParam.CityID, SearchParam.CountryID.ToString(), ChargeAmount, isPerce, B2CMarkup, AgentPercentage, AgentMarkupAmount, IsParentId, SubAgentMarkup, ref Vatpolicy);


                                        //Double CancellationMarkup = Math.Round(ChargeAmount, 2);
                                        //Double CancellationMarkup = 0.0;
                                        //if (isPerce)
                                        //{
                                        //    if (IsB2C == false && (B2CMarkup == 0 || (AgentPercentage > 0)))
                                        //    {
                                        //        CancellationMarkup = Math.Round(((ChargeAmount * AgentPercentage) / 100), 2);
                                        //    }
                                        //}
                                        //else
                                        //    CancellationMarkup = (ChargeAmount == 0 ? ChargeAmount : AgentMarkupAmount);
                                        //ChargeAmount = ChargeAmount + CancellationMarkup;
                                        //AgentChargeAmount = ChargeAmount;

                                        //if (IsParentId == 0)
                                        //{
                                        //    Double SubCancellationAgentMarkup = Math.Round((Math.Round((ChargeAmount * SubAgentMarkup), 2) / 100), 2);
                                        //    ChargeAmount = Math.Round((ChargeAmount + SubCancellationAgentMarkup), 2);
                                        //}
                                        //Double CancellationAgentMarkup = 0;
                                        //if (IsB2C == true)
                                        //{
                                        //    CancellationAgentMarkup = Math.Round((Math.Round((ChargeAmount * AgentPercentage), 2) / 100), 2);
                                        //    ChargeAmount = Math.Round(ChargeAmount + CancellationAgentMarkup, 2);
                                        //}

                                        //ChargeAmount = Math.Round((ChargeAmount + CancellationAgentMarkup), 2);
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
                                        //    FromDate = (Convert.ToString(drCancellationPolicy[c]["ToDate"]) != string.Empty ? Convert.ToDateTime(drCancellationPolicy[c]["ToDate"]) : DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null));// Convert.ToDateTime(drCancellationPolicy[c]["ToDate"]);
                                        //}
                                        //if (c != 0 && c == drCancellationPolicy.GetLength(0) - 1)
                                        //    ToDate = ToDate.AddDays(CutOffDays);
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
                                            drHotelCancellation["SupplierName"] = "oth";
                                            drHotelCancellation["OwnsystemHotelId"] = internalHotelId; //ownsystemhotelId; //for extanet 27-08-2024 smit
                                            drHotelCancellation["RoomNo"] = Convert.ToString(drRoomCategories[rc]["RoomNo"]);
                                            //drHotelCancellation["RoomTypeId"] = Convert.ToString(drRoomCategories[rc]["RaynaRoomTypeName"]);
                                            drHotelCancellation["RoomTypeId"] = Convert.ToString(drRoomCategories[rc]["RoomTypeID"]);
                                            drHotelCancellation["RoomTypeName"] = RoomDesc;
                                            drHotelCancellation["MealId"] = Convert.ToString(drRoomCategories[rc]["BoardBasis"]);
                                            drHotelCancellation["FromDate"] = FromDate;
                                            drHotelCancellation["ToDate"] = ToDate;
                                            drHotelCancellation["CompanyCancellationPrice"] = MAinAgentCharge;
                                            drHotelCancellation["AgentCancellationPrice"] = ChargeAmount;
                                            drHotelCancellation["VatdiffAmount"] = Vatpolicy;
                                            drHotelCancellation["BoardTypeID"] = BoardTypeID;
                                            drHotelCancellation["SupplierCurrency"] = CurrencyCode;
                                            drHotelCancellation["BuyingPrice"] = CompanyChargeAmount;
                                            dtCancellation.Rows.Add(drHotelCancellation);
                                        }
                                    }
                                }



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
                                        double VarRateBreackUP = 0;
                                        freeStay = "no";
                                        if (freeStay == "no")
                                        {
                                            DateGross = Math.Round(Convert.ToDouble(DTdrdate[dtdate]["PriceWithoutExtraBed"]), 6);
                                            #region Vat

                                            DateGross = _markup.ProcessHotelRateBreakupPrice_WithMarkup(SearchParam.NoofNights, SearchParam.CityID, SearchParam.CountryID.ToString(), DateGross, B2CMarkup, IsParentId, isPerce, AgentPercentage, AgentMarkupAmount, SubAgentMarkup, ref VarRateBreackUP);

                                            #endregion
                                            //double b2cDateGrossMarkup = Math.Round((Math.Round((DateGross * B2CMarkup), 2) / 100), 2);
                                            //DateGross = Math.Round(DateGross + b2cDateGrossMarkup, 2);
                                            //if (IsB2C == false)
                                            //{
                                            //    if (IsParentId == 0)
                                            //    {
                                            //        double b2bSubDateGrossMarkup = Math.Round((Math.Round((DateGross * SubAgentMarkup), 2) / 100), 2);
                                            //        DateGross = Math.Round(DateGross + b2bSubDateGrossMarkup, 2);
                                            //    }
                                            //    if (isPerce)
                                            //    {
                                            //        double b2bDateGrossMarkup = Math.Round((Math.Round((DateGross * AgentPercentage), 2) / 100), 2);
                                            //        DateGross = Math.Round(DateGross + b2bDateGrossMarkup, 2);
                                            //    }
                                            //    else
                                            //        DateGross = (DateGross == 0 ? DateGross : Math.Round(DateGross + AgentMarkupAmount, 2));
                                            //}
                                        }

                                        WeekColumn = Convert.ToString(Convert.ToDateTime(datetime).DayOfWeek).Substring(0, 3);
                                        DataRow drHotelRoomRateBreackUp = DtHotelRoomRateBreackUp.NewRow();
                                        drHotelRoomRateBreackUp["RoomNo"] = Convert.ToString(drRoomCategories[rc]["RoomNo"]);
                                        drHotelRoomRateBreackUp["SupplierName"] = "oth";
                                        drHotelRoomRateBreackUp["OwnsystemHotelId"] = internalHotelId; //ownsystemhotelId; //for extanet 27-08-2024 smit
                                        drHotelRoomRateBreackUp["ContractID"] = 0;
                                        drHotelRoomRateBreackUp["RoomTypeName"] = Convert.ToString(DTdrdate[dtdate]["RaynaRoomTypeName"]);
                                        //drHotelRoomRateBreackUp["RoomTypeId"] = Convert.ToString(DTdrdate[dtdate]["RaynaRoomTypeName"]);
                                        drHotelRoomRateBreackUp["RoomTypeId"] = Convert.ToString(DTdrdate[dtdate]["RoomTypeID"]);
                                        drHotelRoomRateBreackUp["MealId"] = Convert.ToString(drRoomCategories[rc]["BoardBasis"]);
                                        drHotelRoomRateBreackUp["Meal"] = Convert.ToString(drRoomCategories[rc]["BoardBasis"]);
                                        drHotelRoomRateBreackUp["BookingStatus"] = Convert.ToString(DTdrdate[dtdate]["BookingStatus"]);
                                        drHotelRoomRateBreackUp["DateOfWeek"] = WeekColumn;
                                        drHotelRoomRateBreackUp["Date"] = datetime;
                                        drHotelRoomRateBreackUp["Rate"] = DateGross;
                                        drHotelRoomRateBreackUp["VatdiffAmount"] = VarRateBreackUP;
                                        drHotelRoomRateBreackUp["RateType"] = "A Rate";
                                        drHotelRoomRateBreackUp["OrderNo"] = cntno;
                                        drHotelRoomRateBreackUp["BoardTypeID"] = BoardTypeID;
                                        drHotelRoomRateBreackUp["FinalRateType"] = Convert.ToString(DTdrdate[dtdate]["FinalRateType"]);
                                        drHotelRoomRateBreackUp["CompanyRate"] = Math.Round(Convert.ToDecimal(DTdrdate[dtdate]["PriceWithoutExtraBed"]), 6);
                                        drHotelRoomRateBreackUp["SupplierCurrency"] = CurrencyCode;
                                        drHotelRoomRateBreackUp["BuyingPrice"] = DateGross;
                                        drHotelRoomRateBreackUp["Total"] = Math.Round(Convert.ToDecimal(DTdrdate[dtdate]["PriceWithoutROE"]), 6); ;

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
                                        {
                                            DateGross = Math.Round(Convert.ToDouble(DTdSurchargerdate[dtdate]["Price"]), 6);
                                            double VarRateBreackUP = 0;
                                            DateGross = _markup.ProcessHotelRateBreakupPrice_WithMarkup(SearchParam.NoofNights, SearchParam.CityID, SearchParam.CountryID.ToString(), DateGross, B2CMarkup, IsParentId, isPerce, AgentPercentage, AgentMarkupAmount, SubAgentMarkup, ref VarRateBreackUP);
                                        }

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
                                        drHotelRoomRateBreackUp["SupplierName"] = "oth";
                                        drHotelRoomRateBreackUp["OwnsystemHotelId"] = internalHotelId; //ownsystemhotelId; //for extanet 27-08-2024 smit
                                        drHotelRoomRateBreackUp["ContractID"] = 0;
                                        drHotelRoomRateBreackUp["RoomTypeName"] = Convert.ToString(DTdrdate[0]["RaynaRoomTypeName"]);
                                        //drHotelRoomRateBreackUp["RoomTypeId"] = Convert.ToString(DTdrdate[0]["RaynaRoomTypeName"]);
                                        drHotelRoomRateBreackUp["RoomTypeId"] = Convert.ToString(DTdrdate[0]["RoomTypeID"]);
                                        drHotelRoomRateBreackUp["MealId"] = Convert.ToString(drRoomCategories[rc]["BoardBasis"]);
                                        drHotelRoomRateBreackUp["Meal"] = Convert.ToString(drRoomCategories[rc]["BoardBasis"]);
                                        drHotelRoomRateBreackUp["BookingStatus"] = Convert.ToString(drRoomCategories[rc]["BookingStatus"]);
                                        drHotelRoomRateBreackUp["DateOfWeek"] = WeekColumn;
                                        drHotelRoomRateBreackUp["Date"] = datetime;
                                        drHotelRoomRateBreackUp["Rate"] = DateGross;
                                        drHotelRoomRateBreackUp["RateType"] = Convert.ToString(DTdSurchargerdate[dtdate]["SurchargeName"]);
                                        drHotelRoomRateBreackUp["OrderNo"] = cntno;
                                        drHotelRoomRateBreackUp["BoardTypeID"] = BoardTypeID;
                                        drHotelRoomRateBreackUp["FinalRateType"] = Convert.ToString(DTdSurchargerdate[dtdate]["FinalRateType"]);
                                        drHotelRoomRateBreackUp["VatdiffAmount"] = 0;
                                        drHotelRoomRateBreackUp["CompanyRate"] = Math.Round(Convert.ToDecimal(DTdSurchargerdate[dtdate]["Price"]), 6);
                                        drHotelRoomRateBreackUp["SupplierCurrency"] = CurrencyCode;
                                        drHotelRoomRateBreackUp["BuyingPrice"] = DateGross;
                                        if (GTAResponse.Tables[4].Columns.Contains("PriceWithoutROE"))
                                            drHotelRoomRateBreackUp["Total"] = Math.Round(Convert.ToDecimal(DTdSurchargerdate[dtdate]["PriceWithoutROE"]), 6);
                                        else
                                            drHotelRoomRateBreackUp["Total"] = Math.Round(Convert.ToDecimal(DTdSurchargerdate[dtdate]["Price"]), 6);

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

                                double ItemPrice = Math.Round(Convert.ToDouble(drRoomCategories[rc]["Price"]), 6);
                                double ItemOriginalPrice = Math.Round(Convert.ToDouble(drRoomCategories[rc]["OriginalPrice"]), 6);

                                double ItemSurchargePrice = Math.Round(Convert.ToDouble(drRoomCategories[rc]["SurchargePrice"]), 6);
                                double VatOnRateBrackUpnew = 0;
                                //double surchargewithmarkup = Common.ProcessHotelRateBreakupPrice_WithMarkup(1, SearchParam.CityID, SearchParam.CountryId, ItemSurchargePrice, B2CMarkup, IsParentId, isPerce, AgentPercentage, AgentMarkupAmount, SubAgentMarkup, ref VatOnRateBrackUpnew);
                                drhotelRoom["CompanyBuyingPrice"] = ItemPrice + ItemSurchargePrice;
                                drhotelRoom["SupplierCurrency"] = CurrencyCode;
                                drhotelRoom["BuyingPrice"] = Convert.ToString(drRoomCategories[rc]["PriceWithoutROE"]);

                                drhotelRoom["EssentialInformation"] = Convert.ToString(GTAResponse.Tables[0].Rows[h]["SPUI"]);
                                if (rc == 0)
                                {
                                    LowestRate = Math.Round(Convert.ToDouble(DTdrdate[0]["Price"]), 6);
                                    OriginalLowestRate = Math.Round(Convert.ToDouble(DTdrdate[0]["OriginalPrice"]), 6);
                                    //  BookingStatus = Convert.ToString(DTdrdate[0]["BookingStatus"]);
                                }
                                for (int dtdate = 0; dtdate <= DTdrdate.GetLength(0) - 1; dtdate++)
                                {
                                    if (BookingStatus == "" || BookingStatus == string.Empty || BookingStatus.ToLower().Trim() == "onrequest")
                                        BookingStatus = Convert.ToString(DTdrdate[dtdate]["BookingStatus"]);
                                }
                                if (Convert.ToString(GTAResponse.Tables[0].Rows[h]["FreeDiscount"]) != "" && Convert.ToString(GTAResponse.Tables[0].Rows[h]["FreeDiscount"]) != "0")
                                {
                                    string DiscountPer = Convert.ToString(GTAResponse.Tables[0].Rows[h]["FreeDiscount"]);
                                    DiscountPerTtext = Convert.ToString(GTAResponse.Tables[0].Rows[h]["FreeDiscount"]);// ""Save " + Convert.ToString(DiscountPer) + "% in discounts*";
                                    if (ItemPrice != ItemOriginalPrice)
                                    {
                                        drhotelRoom["IncudedOfferDiscount"] = ItemPrice;
                                        drhotelRoom["GrossWitohutDiscount"] = ItemOriginalPrice;
                                        drhotelRoom["DiscountPer"] = DiscountPer;
                                        drhotelRoom["DiscountPerText"] = DiscountPerTtext;

                                        //drhotelRoom["EssentialInformation"] = Convert.ToString(GTAResponse.Tables[0].Rows[h]["EssentialInformation"]);
                                        //drhotelRoom["EssentialInformation"] = Convert.ToString(GTAResponse.Tables[0].Rows[h]["SPUI"]);
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
                                    double b2cItemOriginalPriceMarkup = Math.Round((Math.Round((ItemOriginalPrice * B2CMarkup), 6) / 100), 6);
                                    ItemOriginalPrice = Math.Round(ItemOriginalPrice + b2cItemOriginalPriceMarkup, 6);
                                }
                                //Before Discount End 
                                drhotelRoom["CheckInDate"] = SearchParam.CheckInDate;
                                drhotelRoom["CheckOutDate"] = SearchParam.CheckOutDate;
                                drhotelRoom["CityId"] = SearchParam.CityID;
                                double surchargewithmarkup = _markup.ProcessHotelRateBreakupPrice_WithMarkup(1, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), ItemSurchargePrice, B2CMarkup, IsParentId, isPerce, AgentPercentage, AgentMarkupAmount, SubAgentMarkup, ref VatOnRateBrackUpnew);
                                drhotelRoom["CompanySellingPrice"] = ItemPrice + surchargewithmarkup;
                                #region  VAt
                                double VatdiffAmount = Convert.ToDouble(drhotelRoom["CompanySellingPrice"]);
                                //DataTable dtvatlist1 = await _commonMethod.GetVatCountrylist(Convert.ToString(SearchParam.CountryID) == "" ? 0 : Convert.ToInt32(SearchParam.CountryID), Convert.ToInt32(SearchParam.CityID), "hotel");
                                //if (dtvatlist1.Rows.Count > 0)
                                //{
                                //    drhotelRoom["CompanySellingPrice"] = Convert.ToDouble(drhotelRoom["CompanySellingPrice"]) + (Convert.ToDouble(_commonMethod.GetcalVatAmount(Convert.ToDecimal(drhotelRoom["CompanySellingPrice"]) - Convert.ToDecimal(drhotelRoom["CompanyBuyingPrice"]), Convert.ToDecimal(dtvatlist1.Rows[0]["IntRate"]))));
                                //    //ItemPrice = Convert.ToDouble(drhotelRoom["CompanySellingPrice"]);
                                //}
                                VatdiffAmount = Convert.ToDouble(drhotelRoom["CompanySellingPrice"]) - VatdiffAmount;
                                ItemPrice = ItemPrice + VatdiffAmount;
                                #endregion
                                if (IsB2C == false)
                                {
                                    if (IsParentId == 0)
                                    {
                                        drhotelRoom["AgentBuyingPrice"] = ItemPrice;
                                        double b2bSubMarkup = Math.Round((Math.Round(((ItemPrice) * SubAgentMarkup), 6) / 100), 6);
                                        ItemPrice = Math.Round(ItemPrice + b2bSubMarkup, 6);

                                        //Before Discount  Start
                                        if (Convert.ToString(drhotelRoom["IncudedOfferDiscount"]) != "" && Convert.ToDouble(drhotelRoom["IncudedOfferDiscount"]) != Convert.ToDouble(drhotelRoom["GrossWitohutDiscount"]))
                                        {
                                            double b2bOriginalSubMarkup = Math.Round((Math.Round((ItemOriginalPrice * SubAgentMarkup), 6) / 100), 6);
                                            ItemOriginalPrice = Math.Round(ItemOriginalPrice + b2bOriginalSubMarkup, 6);
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
                                    double b2bMarkup = Math.Round((Math.Round((ItemPrice * B2CMarkup), 6) / 100), 6);
                                    ItemPrice = ItemPrice + surchargewithmarkup;

                                    ItemPrice = ItemPrice + b2bMarkup;

                                    //Before Discount  Start
                                    if (Convert.ToString(drhotelRoom["IncudedOfferDiscount"]) != "" && Convert.ToDouble(drhotelRoom["IncudedOfferDiscount"]) != Convert.ToDouble(drhotelRoom["GrossWitohutDiscount"]))
                                    {
                                        double b2bOriginalMarkup = 0.0;
                                        if (isPerce)
                                            b2bOriginalMarkup = Math.Round((Math.Round((ItemOriginalPrice * AgentPercentage), 6) / 100), 6);
                                        else
                                            b2bOriginalMarkup = AgentMarkupAmount;
                                        ItemOriginalPrice = ItemOriginalPrice + ItemSurchargePrice;
                                        ItemOriginalPrice = ItemOriginalPrice + b2bOriginalMarkup;
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
                                    drhotelRoom["VatdiffAmount"] = VatdiffAmount;
                                    //Before Discount  Start
                                    if (Convert.ToString(drhotelRoom["IncudedOfferDiscount"]) != "" && Convert.ToDouble(drhotelRoom["IncudedOfferDiscount"]) != Convert.ToDouble(drhotelRoom["GrossWitohutDiscount"]))
                                    {
                                        drhotelRoom["GrossWitohutDiscount"] = ItemOriginalPrice;
                                    }
                                    //Before Discount End 
                                    drhotelRoom["SupplierName"] = "oth";
                                }
                                DtHotelRoom.Rows.Add(drhotelRoom);

                                ItemPrice_Text = Convert.ToString(ItemPrice);
                                if (rc == 0)
                                {
                                    if (isPerce)
                                    {
                                        if (AgentPercentage > 0)
                                        {
                                            double b2bMarkup = Math.Round((Math.Round((LowestRate * AgentPercentage), 6) / 100), 6);
                                            LowestRate = Math.Round(LowestRate + b2bMarkup, 6);
                                            double b2bOriginalMarkup = Math.Round((Math.Round((OriginalLowestRate * AgentPercentage), 6) / 100), 6);
                                            OriginalLowestRate = Math.Round(OriginalLowestRate + b2bOriginalMarkup, 6);
                                        }
                                    }
                                    else
                                    {
                                        LowestRate = Math.Round(LowestRate + AgentMarkupAmount, 6);
                                        OriginalLowestRate = Math.Round(OriginalLowestRate + AgentMarkupAmount, 6);
                                    }
                                    double b2cMarkup = Math.Round((Math.Round((LowestRate * B2CMarkup), 6) / 100), 6);
                                    LowestRate = Math.Round(LowestRate + b2cMarkup, 6);

                                    double b2cOriginalMarkup = Math.Round((Math.Round((OriginalLowestRate * B2CMarkup), 6) / 100), 6);
                                    OriginalLowestRate = Math.Round(OriginalLowestRate + b2cOriginalMarkup, 6);
                                    #region  VAt
                                    VatdiffAmount = LowestRate;
                                    //dtvatlist1 = await _commonMethod.GetVatCountrylist(Convert.ToString(SearchParam.CountryID) == "" ? 0 : Convert.ToInt32(SearchParam.CountryID), Convert.ToInt32(SearchParam.CityID), "hotel");
                                    //if (dtvatlist1.Rows.Count > 0)
                                    //{
                                    //    LowestRate = Convert.ToDouble(LowestRate) + (Convert.ToDouble(_commonMethod.GetcalVatAmount(Convert.ToDecimal(LowestRate) - Convert.ToDecimal(drhotelRoom["CompanyBuyingPrice"]), Convert.ToDecimal(dtvatlist1.Rows[0]["IntRate"]))));
                                    //}
                                    #endregion
                                    if (IsParentId == 0)
                                    {
                                        double b2bSubMarkup = Math.Round((Math.Round((LowestRate * SubAgentMarkup), 6) / 100), 6);
                                        LowestRate = Math.Round(LowestRate + b2bSubMarkup, 6);
                                        double b2bOriginalSubMarkup = Math.Round((Math.Round((OriginalLowestRate * SubAgentMarkup), 6) / 100), 6);
                                        OriginalLowestRate = Math.Round(OriginalLowestRate + b2bOriginalSubMarkup, 6);
                                    }


                                    int NoOfNoght = Convert.ToInt32(SearchParam.NoofNights);
                                }
                            }

                            string LocationText = CityName;
                            try
                            {
                                LocationText = Convert.ToString(GTAResponse.Tables[0].Rows[h]["Location"]);
                            }
                            catch (Exception ex)
                            {
                                await _exception.Error(ex);
                            }
                            //#region "Markup Calulation"
                            //double VatLowestRate = 0;
                            //LowestRate = Common.ProcessHotelRateBreakupPrice_WithMarkup(SearchParam.NoofNights, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), LowestRate, B2CMarkup, IsParentId, isPerce, AgentPercentage, AgentMarkupAmount, SubAgentMarkup, ref VatLowestRate);

                            //#endregion
                            drXmlHotel["Favorite"] = 0;
                            if (Rating == "" || Rating == "0")
                                Rating = "1";
                            CityName = Convert.ToString(SearchParam.CityName);
                            GTAImage1 = (_keySettings.staticimagepath + "/Images/Dubai/" + ownsystemhotelId + "/1-ThumbNail.jpg");
                            //string CurrencyCode = "USD";// Convert.ToString(ResponseOth[h].CurrencyCode);

                            drXmlHotel["GIITACode"] = giitaCode;
                            drXmlHotel["OwnsystemHotelId"] = internalHotelId; //ownsystemhotelId; //for extanet 27-08-2024 smit
                            drXmlHotel["RaynaId"] = ownsystemhotelId;
                            drXmlHotel["SupplierCurrency"] = CurrencyCode;
                            drXmlHotel["XmlHotelCity"] = CityId;
                            drXmlHotel["XMLHotelCode"] = internalHotelId;//#Due to Ticket#13957
                            drXmlHotel["HotelName"] = HotelName;
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
                            drXmlHotel["SellingPrice"] = LowestRate;
                            drXmlHotel["FinalSellingPrice"] = LowestRate;
                            drXmlHotel["BookingStatus"] = BookingStatus;
                            drXmlHotel["SupplierName"] = "oth";
                            drXmlHotel["OfficeID"] = 0;
                            drXmlHotel["ContractID"] = 0;
                            drXmlHotel["Reviews"] = 0;
                            drXmlHotel["UniqueNo"] = uniqueno;
                            drXmlHotel["IsShowSupplier"] = blnIsShowSupplier;
                            drXmlHotel["VatdiffAmount"] = 0;
                            drXmlHotel["MasterSupplierId"] = Convert.ToInt32(MasterSupplierID);

                            drXmlHotel["ReservationEmail"] = Convert.ToString(GTAResponse.Tables[0].Rows[h]["ReservationEmail"]);
                            drXmlHotel["HotelContactNo"] = Convert.ToString(GTAResponse.Tables[0].Rows[h]["HotelContactNo"]);
                            DtHotel.Rows.Add(drXmlHotel);
                            Options ops = (await _supplierRes.GetCommonRoomForSuppliers(Convert.ToInt64(giitaCode), tokenData, advancedOptions, LowestRate, drXmlHotel, DtHotelRoom, dtCancellation, DtHotelRoomRateBreackUp, SearchParam));
                            if (ops != null)
                                options.Add(ops);
                            BookingStatus = "";
                        }
                    }
                    #endregion

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
                        if (SearchParam != null && ResProccessTimerLogActive == "1" && timer_room_Oth.ElapsedMilliseconds > Convert.ToInt32(ResProccessTimeValueforSaveLog))
                        {
                            timer_room_Oth.Stop();
                            SearchParam.SupplierTime += ",|Finish:OTH Process Response  " + "|Time :" + timer_room_Oth.Elapsed.ToString();
                            _ = Task.Run(() => _commonMethod.FireAway_SQL_ResPorcessTimeLog("", timer_room_Oth.Elapsed.ToString(), "OTH", SearchParam.SearchKey, 1, SearchParam.AgentID));
                        }
                    }
                    catch (Exception x)
                    {
                        await _exception.Error(x);
                    }
                    #endregion
                    //GC.Collect();


                }
            }
            return options;

        }

        public async Task<List<Options>> Oth_HotelSearchWithRooms_beforeRedisCache26AUG2025(HotelX item, AdvancedOptions advancedOptions, TokenData tokenData, DataSet GTAResponse, DataTable dtAgentDetail, DataTable dtSuplierMarkup, HotelSearchParameter SearchParam, DataTable dtMarkupDetails, int agent, CommonSearch commonSearch, string Response, string ProccessCutOffTime, Int32 processTimeout, string ResProccessTimerLogActive, string ResProccessTimeValueforSaveLog)
        {
            System.Diagnostics.Stopwatch timer_room_Oth = new System.Diagnostics.Stopwatch();
            timer_room_Oth.Start();
            List<Options> options = new List<Options>();

            if (GTAResponse != null && GTAResponse.Tables.Count > 0 && GTAResponse.Tables[0].Rows.Count > 0 || Response != "")
            {
                DataTable DtHotel = _commonMethod.GenerateDataTableNewV2();

                DataTable dtHotelAllocation = _commonMethod.GenerateHotelAllocationTableNewV2();
                DataTable dtComplementary = _commonMethod.GenerateComplementaryTableNewV2();
                DataTable dtCommonInfo = _commonMethod.GenerateCommonInfoTableNewV2();
                DataTable dtHotelMinStay = _commonMethod.GenerateHotelMinStayNewV2();
                DataSet dsTemp = new DataSet();

                int CutOffDays = XMLHotelBookingCommon.CutOffDays;
                CutOffDays = _commonMethod.GetCutOffDays(dtSuplierMarkup, Convert.ToString(_keySettings.IsWithinearth) == "1" ? "withinearth" : "oth");
                double SubAgentMarkup = 0;
                int IsParentId = 1;
                if (dtAgentDetail != null)
                {
                    IsParentId = Convert.ToInt32(dtAgentDetail.Rows[0]["IsParentId"]);
                    SubAgentMarkup = Convert.ToDouble(dtAgentDetail.Rows[0]["B2CMarkupPer"]);
                }
                try
                {
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
                    string giitaCode = "";
                    string DiscountPerTtext = "";

                    string Longitude = "";
                    string Latitude = "";
                    int ReviewCount = 0;
                    int ReviewRating = 0;
                    int SortSeq = 9999;
                    DateTime curDate = _commonMethod.GetTimeZoneDate(SearchParam.CurrentDate);
                    if (SearchParam.Filters.IsShowRooms == 1)
                    {
                        if (GTAResponse == null || GTAResponse.Tables[0] == null || GTAResponse.Tables[0].Rows.Count == 0)
                            return options;
                    }
                    double AgentPercentage = 0.0;
                    Boolean blnIsShowSupplier = false;
                    double AgentMarkupAmount = 0.0;
                    Boolean isPerce = true;
                    int MasterSupplierID = 0;
                    if (dtSuplierMarkup != null)
                    {
                        dtSuplierMarkup.DefaultView.RowFilter = "SupplierName='" + (Convert.ToString(_keySettings.IsWithinearth) == "1" ? "withinearth" : "oth") + "'";
                        if (dtSuplierMarkup.DefaultView.Count > 0)
                        {
                            try
                            {
                                AgentPercentage = Convert.ToDouble(dtSuplierMarkup.DefaultView[0]["AgentWiseMarkup"]);
                                blnIsShowSupplier = (Convert.ToInt16(dtSuplierMarkup.DefaultView[0]["IsShowSupplier"]) == 1 ? true : false);
                                MasterSupplierID = Convert.ToInt32(dtSuplierMarkup.DefaultView[0]["MasterSupplierId"]);
                            }
                            catch (Exception x)//k2
                            {
                                try
                                {
                                    AgentPercentage = Convert.ToDouble(dtSuplierMarkup.DefaultView[0]["AgentWiseMarkup"]);
                                    blnIsShowSupplier = (Convert.ToInt16(dtSuplierMarkup.DefaultView[0]["IsShowSupplier"]) == 1 ? true : false);
                                    MasterSupplierID = Convert.ToInt32(dtSuplierMarkup.DefaultView[0]["MasterSupplierId"]);
                                }
                                catch (Exception y)
                                {
                                    await _exception.Error(y, SearchParam.SearchKey);
                                }
                            }
                        }
                    }
                    ///////////@New List Code Added on 15-12-19


                    #region "When not Need Room"  

                    #region "Hotel Code Block "

                    if (SearchParam.Filters.IsShowRooms == 0)
                    {
                        List<OthListResponse> ResponseOth = new List<OthListResponse>();
                        if (Response != "")
                            ResponseOth = JsonSerializer.Deserialize<List<OthListResponse>>(Response);

                        if (ResponseOth == null || ResponseOth.Count == 0)
                            return options;
                        if (dtSuplierMarkup != null)
                        {
                            dtSuplierMarkup.DefaultView.RowFilter = "SupplierName='withinearth'";
                            if (dtSuplierMarkup.DefaultView.Count > 0)
                            {
                                AgentPercentage = Convert.ToDouble(dtSuplierMarkup.DefaultView[0]["AgentWiseMarkup"]);
                                blnIsShowSupplier = (Convert.ToInt16(dtSuplierMarkup.DefaultView[0]["IsShowSupplier"]) == 1 ? true : false);
                            }
                        }
                        if (ResponseOth.Count > 0)
                        {
                            item.ResponseType = "Success";
                        }
                        for (int h = 0; h <= ResponseOth.Count - 1; h++)
                        {
                            DataTable DtHotelRoom = _commonMethod.GenerateRoomtypeDataTableNewV2();
                            DataTable dtCancellation = _commonMethod.GenerateCancellationDataTableNewV2();
                            DataTable DtHotelRoomRateBreackUp = _commonMethod.GenerateoomRateBreackUpDataTableNewV2();
                            if (dtSuplierMarkup != null)
                            {
                                if (dtSuplierMarkup.DefaultView.Count > 0)
                                {
                                    MasterSupplierID = Convert.ToInt32(dtSuplierMarkup.DefaultView[0]["MasterSupplierId"]);
                                }
                            }
                            if (Convert.ToString(ResponseOth[h].GiitaId) == string.Empty || Convert.ToString(ResponseOth[h].GiitaId) == "0")
                                continue;
                            if (ResponseOth[h].SupplierId > 0)
                                MasterSupplierID = ResponseOth[h].SupplierId;

                            _markup.getAgetMarkupSetting("withinearth", Convert.ToString(ResponseOth[h].OwnsystemHotelId), dtSuplierMarkup, dtMarkupDetails, ref AgentPercentage, ref AgentMarkupAmount, ref blnIsShowSupplier, ref isPerce, tokenData.AgentId, Convert.ToInt32(ResponseOth[h].Rating), SearchParam.CityID, Convert.ToString(SearchParam.CountryID), SearchParam.CheckInDate);
                            double AgentCompanyPercentage = SearchParam.AgentCompanyPercentage;
                            bool IsB2C = Convert.ToBoolean(Convert.ToByte(SearchParam.IsB2BOrB2C));

                            double B2CMarkup = (IsB2C == false ? AgentCompanyPercentage : XMLHotelBookingCommon.B2CMarkup);
                            DataRow drXmlHotel = DtHotel.NewRow();
                            Int64 GiataId = ResponseOth[h].GiitaId;
                            drXmlHotel["Favorite"] = 0;

                            if (Convert.ToString(ResponseOth[h].Rating) == "" || Convert.ToString(ResponseOth[h].Rating) == "0")
                                ResponseOth[h].Rating = 1;
                            Rating = Convert.ToString(ResponseOth[h].Rating);
                            DataTable DtHotelPolicy = _commonMethod.GenerateCancellationDataTableNewV2();
                            double price = Convert.ToDouble(ResponseOth[h].FinalSellingPrice); //* ROE;
                            string CurrencyCode = Convert.ToString(ResponseOth[h].CurrencyCode);
                            Int32 ItemCode = ResponseOth[h].OwnsystemHotelId;
                            drXmlHotel["XMLCountry"] = Convert.ToString(SearchParam.CountryID);
                            drXmlHotel["XMLCity"] = Convert.ToString(SearchParam.CityID);
                            drXmlHotel["XmlHotelCity"] = Convert.ToString(ItemCode);
                            drXmlHotel["XMLHotelCode"] = ItemCode;
                            drXmlHotel["EssentialInformation"] = "";
                            drXmlHotel["Description"] = "";
                            drXmlHotel["SupplierName"] = "oth";
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
                            drXmlHotel["GIITACode"] = GiataId;
                            drXmlHotel["RaynaId"] = ItemCode;

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

                                _markup.GetHotelDataCommon(ref price, ref willAddOrNot, ref OwnsystemHotelId, ref RaynaID, ref GiataId, ref RatingMain, ref drXmlHotel, commonSearch.AgentId, commonSearch.TariffID, commonSearch.dsAgentDetails, commonSearch.supplierGiataMaster, SearchParam, commonSearch.dtCountryCityMarkup, commonSearch.B2CMarkup, commonSearch.IsParentId, commonSearch.SubAgentMarkup, commonSearch.SearchByHotel, commonSearch.HotelIDS);
                                if (willAddOrNot)
                                {
                                    #region "Room Logic"
                                    DataTable dt_HotelRoom = _commonMethod.GenerateRoomtypeDataTableNewV2();
                                    DataTable dt_Cancellation = _commonMethod.GenerateCancellationDataTableNewV2();
                                    DataTable dt_RateBreackUp = _commonMethod.GenerateoomRateBreackUpDataTableNewV2();
                                    #endregion
                                    decimal LowestPrice = Convert.ToDecimal(price).CalculatePrice(commonSearch.tokenData, commonSearch.advancedOptions, CurrencyCode);
                                    Options ops = (await _supplierRes.GetCommonRoomForSuppliers(Convert.ToInt64(GiataId), tokenData, advancedOptions, Convert.ToDouble(LowestPrice), drXmlHotel, DtHotelRoom, dtCancellation, DtHotelRoomRateBreackUp, SearchParam));
                                    if (ops != null)
                                        options.Add(ops);
                                }
                                #endregion
                            }
                        }
                    }
                    #endregion

                    #endregion
                    #region "When Need Room" 
                    else
                    {
                        if (GTAResponse.Tables[0].Rows.Count > 0)
                        {
                            item.ResponseType = "Success";
                        }
                        for (int h = 0; h < GTAResponse.Tables[0].Rows.Count; h++)
                        {
                            //Check Time If High then return NoCount
                            //if (ProccessCutOffTime == "1" && timer_room_Oth.ElapsedMilliseconds > processTimeout) // Disable (API group)3,4-Mar-2023
                            //{
                            //    //timer_room_Oth.Stop();
                            //    break;
                            //}
                            //--end
                            DataTable DtHotelRoom = _commonMethod.GenerateRoomtypeDataTableNewV2();
                            DataTable dtCancellation = _commonMethod.GenerateCancellationDataTableNewV2();
                            DataTable DtHotelRoomRateBreackUp = _commonMethod.GenerateoomRateBreackUpDataTableNewV2();

                            ReviewCount = 0;
                            ReviewRating = 0;

                            giitaCode = Convert.ToString(GTAResponse.Tables[0].Rows[h]["GiitaId"]);
                            Rating = Convert.ToString(GTAResponse.Tables[0].Rows[h]["Rating"]);
                            int SupplierId = Convert.ToInt32(GTAResponse.Tables[0].Rows[h]["SupplierId"]);
                            if (dtSuplierMarkup != null)
                            {
                                if (dtSuplierMarkup.DefaultView.Count > 0)
                                {
                                    MasterSupplierID = Convert.ToInt32(dtSuplierMarkup.DefaultView[0]["MasterSupplierId"]);

                                }
                            }
                            if (SupplierId > 0)
                                MasterSupplierID = SupplierId;
                            // Common.getAgetMarkupSetting(Convert.ToString(ConfigurationManager.AppSettings["IsWithinearth"]) == "1" ? "withinearth" : "oth", giitaCode, dtSuplierMarkup, dtMarkupDetails, ref AgentPercentage, ref AgentMarkupAmount, ref blnIsShowSupplier, ref isPerce, Convert.ToInt32(agent), Convert.ToInt32(Rating), SearchParam.CityID, Convert.ToString(SearchParam.CountryID));
                            string markupsuppliername = Convert.ToString(GTAResponse.Tables[0].Rows[0]["SupplierName"]) == "oth" ? ((_keySettings.IsWithinearth) == "1" ? "withinearth" : "oth") : Convert.ToString(GTAResponse.Tables[0].Rows[0]["SupplierName"]);
                            _markup.getAgetMarkupSetting("withinearth", giitaCode, dtSuplierMarkup, dtMarkupDetails, ref AgentPercentage, ref AgentMarkupAmount, ref blnIsShowSupplier, ref isPerce, Convert.ToInt32(agent), Convert.ToInt32(Rating), SearchParam.CityID, Convert.ToString(SearchParam.CountryID), SearchParam.CheckInDate);

                            double AgentCompanyPercentage = Convert.ToDouble(SearchParam.AgentCompanyPercentage);
                            bool IsB2C = Convert.ToBoolean(Convert.ToByte(SearchParam.IsB2BOrB2C));

                            double B2CMarkup = (IsB2C == false ? AgentCompanyPercentage : XMLHotelBookingCommon.B2CMarkup);
                            //if (IsB2C == true)
                            //{
                            //	AgentPercentage = 0;
                            //	AgentMarkupAmount = 0;
                            //}


                            DataRow drXmlHotel = DtHotel.NewRow();

                            RoomCancellation = "";
                            LowestRate = 0;
                            string uniqueno = Guid.NewGuid().ToString();
                            string ItemCode = Convert.ToString(GTAResponse.Tables[0].Rows[h]["raynaid"]);
                            //if (GTAResponse.Tables[11] != null && GTAResponse.Tables[11].Rows.Count > 0)
                            //{
                            //    DataRow[] drMin = GTAResponse.Tables[11].Select("raynaid='" + ItemCode + "'");
                            //    if (drMin.Length > 0)
                            //        continue;
                            //}
                            string Meal = Convert.ToString(GTAResponse.Tables[0].Rows[h]["BoardBasis"]);
                            string HotelName = Convert.ToString(GTAResponse.Tables[0].Rows[h]["HotelName"]);
                            string CurrencyCode = Convert.ToString(GTAResponse.Tables[0].Rows[h]["CurrencyCode"]);

                            string OfferCode = "";
                            ReviewCount = (Convert.ToString(GTAResponse.Tables[0].Rows[h]["ReviewCount"]) != string.Empty ? Convert.ToInt32(GTAResponse.Tables[0].Rows[h]["ReviewCount"]) : 0);
                            ReviewRating = (Convert.ToString(GTAResponse.Tables[0].Rows[h]["ReviewRating"]) != string.Empty ? Convert.ToInt32(GTAResponse.Tables[0].Rows[h]["ReviewRating"]) : 0);
                            string HotelInformation = Convert.ToString(GTAResponse.Tables[0].Rows[h]["SPUI"]);
                            string HotelDescription = Convert.ToString(GTAResponse.Tables[0].Rows[h]["Description"]);
                            internalHotelId = Convert.ToString(GTAResponse.Tables[0].Rows[h]["raynaid"]);

                            //ownsystemhotelId = Convert.ToString(GTAResponse.Tables[0].Rows[h]["raynaid"]);
                            ownsystemhotelId = Convert.ToString(GTAResponse.Tables[0].Rows[h]["OriginalRaynaid"]);//#Due to Ticket#13957

                            giitaCode = Convert.ToString(GTAResponse.Tables[0].Rows[h]["GiitaId"]);
                            Address = Convert.ToString(GTAResponse.Tables[0].Rows[h]["Address"]);
                            int Favorite = 0;// Convert.ToInt32(Favouritedt.Select("Favorite=" + ownsystemhotelId).GetLength(0) == 0 ? 0 : Convert.ToInt32(Favouritedt.Select("Favorite=" + ownsystemhotelId)[0]["Favorite"]));
                            drXmlHotel["Favorite"] = Favorite;
                            LocationDescription = Convert.ToString(GTAResponse.Tables[0].Rows[h]["Location"]);
                            Rating = Convert.ToString(GTAResponse.Tables[0].Rows[h]["Rating"]);
                            Chain = Convert.ToString(GTAResponse.Tables[0].Rows[h]["GroupName"]);
                            SortSeq = Convert.ToInt32(GTAResponse.Tables[0].Rows[h]["SortSequence"]);
                            if (Rating == "" || Rating == "0")
                                Rating = "1";
                            CityName = "Dubai City";
                            CountryId = "13063";
                            CityId = "13668";
                            Latitude = Convert.ToString(GTAResponse.Tables[0].Rows[h]["Latitude"]);
                            Longitude = Convert.ToString(GTAResponse.Tables[0].Rows[h]["Longitude"]);

                            //DataRow[] drComplementary = GTAResponse.Tables[6].Select("RaynaId=" + ItemCode + "");
                            ////Need to Add Complementry Logic here
                            //if (drComplementary.GetLength(0) > 0)
                            //{
                            //    //for(int cm=0;cm<(drComplementary.GetLength(0) > 5?5: drComplementary.GetLength(0));cm++)
                            //    for (int cm = 0; cm < drComplementary.GetLength(0); cm++)
                            //    {
                            //        DataRow dtr = dtComplementary.NewRow();
                            //        dtr["OwnsystemHotelId"] = Convert.ToString(drComplementary[cm]["RaynaId"]);
                            //        dtr["ComplementaryName"] = Convert.ToString(drComplementary[cm]["ComplementaryName"]);
                            //        dtr["ComplementaryText"] = Convert.ToString(drComplementary[cm]["ComplementaryText"]);
                            //        dtr["ImagePath"] = Convert.ToString(drComplementary[cm]["ImagePath"]);
                            //        dtComplementary.Rows.Add(dtr);
                            //    }
                            //}

                            //DataRow[] drCommonInfo = GTAResponse.Tables[12].Select("RaynaId=" + ItemCode + "");
                            ////Need to Add common information(hotel description, common policy etc) Logic here

                            //if (drCommonInfo.GetLength(0) > 0)
                            //{
                            //    //for(int cm=0;cm<(drComplementary.GetLength(0) > 5?5: drComplementary.GetLength(0));cm++)
                            //    for (int cm = 0; cm < drCommonInfo.GetLength(0); cm++)
                            //    {
                            //        DataRow dtr = dtCommonInfo.NewRow();
                            //        dtr["OwnsystemHotelId"] = Convert.ToString(drCommonInfo[cm]["RaynaId"]);
                            //        dtr["CommonPolicyName"] = Convert.ToString(drCommonInfo[cm]["CommonPolicyName"]);
                            //        dtr["CommonPolicyManagementText"] = Convert.ToString(drCommonInfo[cm]["CommonPolicyManagementText"]);
                            //        dtCommonInfo.Rows.Add(dtr);
                            //    }
                            //}

                            DataRow[] drRoomCategories = GTAResponse.Tables[8].Select("RaynaId=" + internalHotelId + "");
                            int PreviousRoomNo = 1;
                            int LogicalCounter = 0;
                            for (int rc = 0; rc <= drRoomCategories.GetLength(0) - 1; rc++)
                            {
                                //Room wise MLOS added on 29-6-2022
                                if (GTAResponse.Tables[11] != null && GTAResponse.Tables[11].Rows.Count > 0)
                                {
                                    DataRow[] drMinx = GTAResponse.Tables[11].Select("raynaid='" + ItemCode + "' and RoomTypeId='" + Convert.ToString(0) + "'");//RoomTypeId=0 means for All rooms
                                    if (drMinx.Length > 0)
                                        continue;
                                    DataRow[] drMin = GTAResponse.Tables[11].Select("raynaid='" + ItemCode + "' and RoomTypeId='" + Convert.ToString(drRoomCategories[rc]["RoomTypeID"]) + "'");
                                    if (drMin.Length > 0)
                                        continue;
                                }

                                DataRow drhotelRoom = DtHotelRoom.NewRow();
                                if (Convert.ToInt32(drRoomCategories[rc]["RoomNo"]) != PreviousRoomNo)
                                {
                                    LogicalCounter = 0;
                                }
                                PreviousRoomNo = Convert.ToInt32(drRoomCategories[rc]["RoomNo"]);
                                LogicalCounter++;
                                // BookingStatus = "";
                                string RoomDesc = Convert.ToString(drRoomCategories[rc]["RaynaRoomTypeName"]);
                                string RoomTypeID = Convert.ToString(drRoomCategories[rc]["RoomTypeID"]);
                                string BoardTypeID = Convert.ToString(drRoomCategories[rc]["BoardTypeID"]);
                                OfferCode = Convert.ToString(drRoomCategories[rc]["OfferCode"]);
                                string RoomId = Convert.ToString(drRoomCategories[rc]["RaynaRoomTypeName"]) + "-" + Convert.ToString(drRoomCategories[rc]["BoardBasis"]);
                                string MealId = Convert.ToString(drRoomCategories[rc]["BoardBasis"]);
                                drhotelRoom["OwnsystemHotelId"] = internalHotelId; //ownsystemhotelId; //for extanet 27-08-2024 smit
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
                                drhotelRoom["RoomTypeId"] = Convert.ToString(drRoomCategories[rc]["RoomTypeId"]); ;
                                drhotelRoom["MealId"] = Convert.ToString(drRoomCategories[rc]["BoardBasis"]);
                                //By manish
                                drhotelRoom["IsCombineRoom"] = false;
                                drhotelRoom["IsPolicyDynamic"] = false;
                                drhotelRoom["OfferDescription"] = Convert.ToString(drRoomCategories[rc]["OfferDescription"]) != "" ? Convert.ToString(drRoomCategories[rc]["OfferDescription"]) : OfferCode;

                                if (Convert.ToString(drRoomCategories[rc]["BoardBasis"]) == "")
                                {
                                    drRoomCategories[rc]["BoardBasis"] = "RO";
                                }
                                drXmlHotel["MealName"] = Convert.ToString(drRoomCategories[rc]["BoardBasis"]);

                                DataRow[] DTdrdate = GTAResponse.Tables[3].Select("RoomTypeID='" + RoomTypeID + "' and RaynaId=" + ItemCode + " and RoomNo=" + Convert.ToDouble(drRoomCategories[rc]["RoomNo"]) + " and BoardTypeID='" + BoardTypeID + "'");
                                DataRow[] DTdSurchargerdate = GTAResponse.Tables[4].Select("RoomTypeID='" + RoomTypeID + "' and RaynaId=" + ItemCode + " and BoardTypeID='" + BoardTypeID + "' and RoomNo=" + Convert.ToDouble(drRoomCategories[rc]["RoomNo"]));

                                int ActualNoOfNight = Convert.ToInt32(DTdrdate.GetLength(0));
                                int ActualSurchargeNoOfNight = Convert.ToInt32(DTdSurchargerdate.GetLength(0));

                                drhotelRoom["AdultsOccupancy"] = Convert.ToString(DTdrdate[0]["RoomCapacity"]);
                                drhotelRoom["ChildOccupancy"] = Convert.ToString(DTdrdate[0]["NoOfChild"]);
                                drhotelRoom["MaxExtrabed"] = Convert.ToString(DTdrdate[0]["NoOfExtraBeds"]);
                                drhotelRoom["BoardTypeID"] = BoardTypeID;
                                drhotelRoom["OTHPromoCode"] = Convert.ToString(DTdrdate[0]["AppliedOfferCode"]);
                                //drhotelRoom["ExtraNight"] = IsFromPkg == 1 ? Convert.ToString(DTdrdate[0]["ExtraNight"]) : "0";
                                //drhotelRoom["ExtraNightDifference"] = IsFromPkg == 1 ? Convert.ToString(DTdrdate[0]["ExtraNightDifference"]) : "0";
                                DataRow[] drCancellationPolicy = GTAResponse.Tables[9].Select
                                    ("raynaid=" + ItemCode + " and (RoomTypeID='" + RoomTypeID + "' or RoomTypeID='0') and (BoardTypeID='" + BoardTypeID + "' or (BoardTypeID='0' and " + BoardTypeID + ">0))");

                                if (drCancellationPolicy.GetLength(0) == 0)
                                {
                                    //drCancellationPolicy = GTAResponse.Tables[9].Select
                                    //("raynaid=" + ItemCode + " and RaynaRoomTypeName=''");
                                    drCancellationPolicy = GTAResponse.Tables[9].Select
                                    ("raynaid=" + ItemCode + " and (RoomTypeID='0') and (BoardTypeID='0')");
                                }
                                int intRoomCancellation = 0;

                                if (drCancellationPolicy.GetLength(0) == 0)
                                {
                                    //Default non-refundable cancellation policy
                                    double ItemBasePrice = Math.Round(Convert.ToDouble(drRoomCategories[rc]["Price"]), 6);
                                    double ItemSurchargePriceBasePrice = Math.Round(Convert.ToDouble(drRoomCategories[rc]["SurchargePrice"]), 6);

                                    if (ActualSurchargeNoOfNight != 0)
                                    {
                                        //ItemSurchargePriceBasePrice = (ItemSurchargePriceBasePrice / ActualSurchargeNoOfNight);
                                        ItemSurchargePriceBasePrice = (ItemSurchargePriceBasePrice);

                                    }
                                    double CacelationAmountData = ItemBasePrice;
                                    double ChargeAmount = CacelationAmountData;
                                    double CompanyChargeAmount = ChargeAmount;


                                    double Vatpolicy = ChargeAmount;
                                    double ChargeSurchargeAmount = ItemSurchargePriceBasePrice;
                                    double AgentChargeAmount = 0;
                                    //Double CancellationMarkup = Math.Round(ChargeAmount, 2);
                                    Double CancellationMarkup = 0.0;
                                    if (isPerce)
                                    {
                                        if (IsB2C == false && (B2CMarkup == 0 || (AgentPercentage > 0)))
                                        {
                                            CancellationMarkup = Math.Round(((ChargeAmount * AgentPercentage) / 100), 6);
                                        }
                                    }
                                    else
                                        CancellationMarkup = (ChargeAmount == 0 ? ChargeAmount : AgentMarkupAmount);
                                    ChargeAmount = ChargeAmount + CancellationMarkup;
                                    #region Vat
                                    double VatChargeAmount = ChargeAmount;
                                    //DataTable dtvatlist = await _commonMethod.GetVatCountrylist(Convert.ToString(SearchParam.CountryID) == "" ? 0 : Convert.ToInt32(SearchParam.CountryID), Convert.ToInt32(SearchParam.CityID), "hotel");
                                    //if (dtvatlist.Rows.Count > 0)
                                    //{
                                    //    ChargeAmount = ChargeAmount + (Convert.ToDouble(_commonMethod.GetcalVatAmount(Convert.ToDecimal(ChargeAmount) - Convert.ToDecimal(Vatpolicy), Convert.ToDecimal(dtvatlist.Rows[0]["IntRate"]))));
                                    //}
                                    VatChargeAmount = ChargeAmount - VatChargeAmount;
                                    #endregion
                                    AgentChargeAmount = ChargeAmount;


                                    double MainAgentPrice = ChargeAmount;
                                    if (IsParentId == 0)
                                    {
                                        Double SubCancellationAgentMarkup = Math.Round((Math.Round((ChargeAmount * SubAgentMarkup), 6) / 100), 6);
                                        ChargeAmount = Math.Round((ChargeAmount + SubCancellationAgentMarkup), 6);
                                    }
                                    Double CancellationAgentMarkup = 0;
                                    if (IsB2C == true)
                                    {
                                        CancellationAgentMarkup = Math.Round((Math.Round((ChargeAmount * AgentPercentage), 6) / 100), 6);
                                        ChargeAmount = Math.Round(ChargeAmount + CancellationAgentMarkup, 6);
                                    }

                                    ChargeAmount = Math.Round((ChargeAmount + CancellationAgentMarkup), 6);
                                    ChargeAmount = ChargeAmount + ChargeSurchargeAmount;
                                    CompanyChargeAmount = CompanyChargeAmount + ChargeSurchargeAmount;
                                    DataRow drHotelCancellation = dtCancellation.NewRow();
                                    drHotelCancellation["SupplierName"] = "oth";
                                    drHotelCancellation["OwnsystemHotelId"] = internalHotelId; //ownsystemhotelId; //for extanet 27-08-2024 smit
                                    drHotelCancellation["RoomNo"] = Convert.ToString(drRoomCategories[rc]["RoomNo"]);
                                    //drHotelCancellation["RoomTypeId"] = Convert.ToString(drRoomCategories[rc]["RaynaRoomTypeName"]);
                                    drHotelCancellation["RoomTypeId"] = Convert.ToString(drRoomCategories[rc]["RoomTypeID"]);
                                    drHotelCancellation["RoomTypeName"] = RoomDesc;
                                    drHotelCancellation["MealId"] = Convert.ToString(drRoomCategories[rc]["BoardBasis"]);
                                    //drHotelCancellation["FromDate"] = FromDate;
                                    //drHotelCancellation["ToDate"] = ToDate;
                                    drHotelCancellation["FromDate"] = DateTime.Now.Date;
                                    drHotelCancellation["ToDate"] = DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null);
                                    drHotelCancellation["CompanyCancellationPrice"] = MainAgentPrice;
                                    drHotelCancellation["AgentCancellationPrice"] = ChargeAmount;
                                    //drHotelCancellation["BoardTypeID"] = VatChargeAmount;
                                    drHotelCancellation["VatdiffAmount"] = Vatpolicy;
                                    drHotelCancellation["BoardTypeID"] = BoardTypeID;
                                    drHotelCancellation["SupplierCurrency"] = CurrencyCode;
                                    drHotelCancellation["BuyingPrice"] = CompanyChargeAmount;

                                    dtCancellation.Rows.Add(drHotelCancellation);
                                }
                                else
                                {
                                    for (int c = drCancellationPolicy.GetLength(0) - 1; c >= 0; c--)
                                    {
                                        double ItemBasePrice = Math.Round(Convert.ToDouble(drRoomCategories[rc]["Price"]), 6);
                                        double ItemSurchargePriceBasePrice = Math.Round(Convert.ToDouble(drRoomCategories[rc]["SurchargePrice"]), 6);
                                        double VatOnRateBrackUp_new = 0;
                                        double surchargewithmarkup_policy = _markup.ProcessHotelRateBreakupPrice_WithMarkup(1, SearchParam.CityID, SearchParam.CountryID.ToString(), ItemSurchargePriceBasePrice, B2CMarkup, IsParentId, isPerce, AgentPercentage, AgentMarkupAmount, SubAgentMarkup, ref VatOnRateBrackUp_new);

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
                                            ItemSurchargePriceBasePrice = (ItemSurchargePriceBasePrice);
                                        }
                                        double CacelationAmountData = ItemBasePrice;
                                        if (cancellationNights != 0)
                                        {
                                            CacelationAmountData = (CacelationAmountData / ActualNoOfNight) * cancellationNights;
                                        }
                                        double ChargeAmount = (CacelationAmountData * CancellationPercentage) / 100;

                                        double ChargeSurchargeAmount = (surchargewithmarkup_policy * CancellationPercentage) / 100;
                                        double AgentChargeAmount = 0;
                                        double Vatpolicy = 0;
                                        double MAinAgentCharge = ChargeAmount;
                                        double CompanyChargeAmount = ChargeAmount;
                                        ChargeAmount = _markup.ProcessHotelCancelationPrice_WithMarkup(ref MAinAgentCharge, SearchParam.NoofNights, SearchParam.CityID, SearchParam.CountryID.ToString(), ChargeAmount, isPerce, B2CMarkup, AgentPercentage, AgentMarkupAmount, IsParentId, SubAgentMarkup, ref Vatpolicy);


                                        //Double CancellationMarkup = Math.Round(ChargeAmount, 2);
                                        //Double CancellationMarkup = 0.0;
                                        //if (isPerce)
                                        //{
                                        //    if (IsB2C == false && (B2CMarkup == 0 || (AgentPercentage > 0)))
                                        //    {
                                        //        CancellationMarkup = Math.Round(((ChargeAmount * AgentPercentage) / 100), 2);
                                        //    }
                                        //}
                                        //else
                                        //    CancellationMarkup = (ChargeAmount == 0 ? ChargeAmount : AgentMarkupAmount);
                                        //ChargeAmount = ChargeAmount + CancellationMarkup;
                                        //AgentChargeAmount = ChargeAmount;

                                        //if (IsParentId == 0)
                                        //{
                                        //    Double SubCancellationAgentMarkup = Math.Round((Math.Round((ChargeAmount * SubAgentMarkup), 2) / 100), 2);
                                        //    ChargeAmount = Math.Round((ChargeAmount + SubCancellationAgentMarkup), 2);
                                        //}
                                        //Double CancellationAgentMarkup = 0;
                                        //if (IsB2C == true)
                                        //{
                                        //    CancellationAgentMarkup = Math.Round((Math.Round((ChargeAmount * AgentPercentage), 2) / 100), 2);
                                        //    ChargeAmount = Math.Round(ChargeAmount + CancellationAgentMarkup, 2);
                                        //}

                                        //ChargeAmount = Math.Round((ChargeAmount + CancellationAgentMarkup), 2);
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
                                        //    FromDate = (Convert.ToString(drCancellationPolicy[c]["ToDate"]) != string.Empty ? Convert.ToDateTime(drCancellationPolicy[c]["ToDate"]) : DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null));// Convert.ToDateTime(drCancellationPolicy[c]["ToDate"]);
                                        //}
                                        //if (c != 0 && c == drCancellationPolicy.GetLength(0) - 1)
                                        //    ToDate = ToDate.AddDays(CutOffDays);
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
                                            drHotelCancellation["SupplierName"] = "oth";
                                            drHotelCancellation["OwnsystemHotelId"] = internalHotelId; //ownsystemhotelId; //for extanet 27-08-2024 smit
                                            drHotelCancellation["RoomNo"] = Convert.ToString(drRoomCategories[rc]["RoomNo"]);
                                            //drHotelCancellation["RoomTypeId"] = Convert.ToString(drRoomCategories[rc]["RaynaRoomTypeName"]);
                                            drHotelCancellation["RoomTypeId"] = Convert.ToString(drRoomCategories[rc]["RoomTypeID"]);
                                            drHotelCancellation["RoomTypeName"] = RoomDesc;
                                            drHotelCancellation["MealId"] = Convert.ToString(drRoomCategories[rc]["BoardBasis"]);
                                            drHotelCancellation["FromDate"] = FromDate;
                                            drHotelCancellation["ToDate"] = ToDate;
                                            drHotelCancellation["CompanyCancellationPrice"] = MAinAgentCharge;
                                            drHotelCancellation["AgentCancellationPrice"] = ChargeAmount;
                                            drHotelCancellation["VatdiffAmount"] = Vatpolicy;
                                            drHotelCancellation["BoardTypeID"] = BoardTypeID;
                                            drHotelCancellation["SupplierCurrency"] = CurrencyCode;
                                            drHotelCancellation["BuyingPrice"] = CompanyChargeAmount;
                                            dtCancellation.Rows.Add(drHotelCancellation);
                                        }
                                    }
                                }



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
                                        double VarRateBreackUP = 0;
                                        freeStay = "no";
                                        if (freeStay == "no")
                                        {
                                            DateGross = Math.Round(Convert.ToDouble(DTdrdate[dtdate]["PriceWithoutExtraBed"]), 6);
                                            #region Vat

                                            DateGross = _markup.ProcessHotelRateBreakupPrice_WithMarkup(SearchParam.NoofNights, SearchParam.CityID, SearchParam.CountryID.ToString(), DateGross, B2CMarkup, IsParentId, isPerce, AgentPercentage, AgentMarkupAmount, SubAgentMarkup, ref VarRateBreackUP);

                                            #endregion
                                            //double b2cDateGrossMarkup = Math.Round((Math.Round((DateGross * B2CMarkup), 2) / 100), 2);
                                            //DateGross = Math.Round(DateGross + b2cDateGrossMarkup, 2);
                                            //if (IsB2C == false)
                                            //{
                                            //    if (IsParentId == 0)
                                            //    {
                                            //        double b2bSubDateGrossMarkup = Math.Round((Math.Round((DateGross * SubAgentMarkup), 2) / 100), 2);
                                            //        DateGross = Math.Round(DateGross + b2bSubDateGrossMarkup, 2);
                                            //    }
                                            //    if (isPerce)
                                            //    {
                                            //        double b2bDateGrossMarkup = Math.Round((Math.Round((DateGross * AgentPercentage), 2) / 100), 2);
                                            //        DateGross = Math.Round(DateGross + b2bDateGrossMarkup, 2);
                                            //    }
                                            //    else
                                            //        DateGross = (DateGross == 0 ? DateGross : Math.Round(DateGross + AgentMarkupAmount, 2));
                                            //}
                                        }

                                        WeekColumn = Convert.ToString(Convert.ToDateTime(datetime).DayOfWeek).Substring(0, 3);
                                        DataRow drHotelRoomRateBreackUp = DtHotelRoomRateBreackUp.NewRow();
                                        drHotelRoomRateBreackUp["RoomNo"] = Convert.ToString(drRoomCategories[rc]["RoomNo"]);
                                        drHotelRoomRateBreackUp["SupplierName"] = "oth";
                                        drHotelRoomRateBreackUp["OwnsystemHotelId"] = internalHotelId; //ownsystemhotelId; //for extanet 27-08-2024 smit
                                        drHotelRoomRateBreackUp["ContractID"] = 0;
                                        drHotelRoomRateBreackUp["RoomTypeName"] = Convert.ToString(DTdrdate[dtdate]["RaynaRoomTypeName"]);
                                        //drHotelRoomRateBreackUp["RoomTypeId"] = Convert.ToString(DTdrdate[dtdate]["RaynaRoomTypeName"]);
                                        drHotelRoomRateBreackUp["RoomTypeId"] = Convert.ToString(DTdrdate[dtdate]["RoomTypeID"]);
                                        drHotelRoomRateBreackUp["MealId"] = Convert.ToString(drRoomCategories[rc]["BoardBasis"]);
                                        drHotelRoomRateBreackUp["Meal"] = Convert.ToString(drRoomCategories[rc]["BoardBasis"]);
                                        drHotelRoomRateBreackUp["BookingStatus"] = Convert.ToString(DTdrdate[dtdate]["BookingStatus"]);
                                        drHotelRoomRateBreackUp["DateOfWeek"] = WeekColumn;
                                        drHotelRoomRateBreackUp["Date"] = datetime;
                                        drHotelRoomRateBreackUp["Rate"] = DateGross;
                                        drHotelRoomRateBreackUp["VatdiffAmount"] = VarRateBreackUP;
                                        drHotelRoomRateBreackUp["RateType"] = "A Rate";
                                        drHotelRoomRateBreackUp["OrderNo"] = cntno;
                                        drHotelRoomRateBreackUp["BoardTypeID"] = BoardTypeID;
                                        drHotelRoomRateBreackUp["FinalRateType"] = Convert.ToString(DTdrdate[dtdate]["FinalRateType"]);
                                        drHotelRoomRateBreackUp["CompanyRate"] = Math.Round(Convert.ToDecimal(DTdrdate[dtdate]["PriceWithoutExtraBed"]), 6);
                                        drHotelRoomRateBreackUp["SupplierCurrency"] = CurrencyCode;
                                        drHotelRoomRateBreackUp["BuyingPrice"] = DateGross;
                                        drHotelRoomRateBreackUp["Total"] = Math.Round(Convert.ToDecimal(DTdrdate[dtdate]["PriceWithoutROE"]), 6); ;

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
                                        {
                                            DateGross = Math.Round(Convert.ToDouble(DTdSurchargerdate[dtdate]["Price"]), 6);
                                            double VarRateBreackUP = 0;
                                            DateGross = _markup.ProcessHotelRateBreakupPrice_WithMarkup(SearchParam.NoofNights, SearchParam.CityID, SearchParam.CountryID.ToString(), DateGross, B2CMarkup, IsParentId, isPerce, AgentPercentage, AgentMarkupAmount, SubAgentMarkup, ref VarRateBreackUP);
                                        }

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
                                        drHotelRoomRateBreackUp["SupplierName"] = "oth";
                                        drHotelRoomRateBreackUp["OwnsystemHotelId"] = internalHotelId; //ownsystemhotelId; //for extanet 27-08-2024 smit
                                        drHotelRoomRateBreackUp["ContractID"] = 0;
                                        drHotelRoomRateBreackUp["RoomTypeName"] = Convert.ToString(DTdrdate[0]["RaynaRoomTypeName"]);
                                        //drHotelRoomRateBreackUp["RoomTypeId"] = Convert.ToString(DTdrdate[0]["RaynaRoomTypeName"]);
                                        drHotelRoomRateBreackUp["RoomTypeId"] = Convert.ToString(DTdrdate[0]["RoomTypeID"]);
                                        drHotelRoomRateBreackUp["MealId"] = Convert.ToString(drRoomCategories[rc]["BoardBasis"]);
                                        drHotelRoomRateBreackUp["Meal"] = Convert.ToString(drRoomCategories[rc]["BoardBasis"]);
                                        drHotelRoomRateBreackUp["BookingStatus"] = Convert.ToString(drRoomCategories[rc]["BookingStatus"]);
                                        drHotelRoomRateBreackUp["DateOfWeek"] = WeekColumn;
                                        drHotelRoomRateBreackUp["Date"] = datetime;
                                        drHotelRoomRateBreackUp["Rate"] = DateGross;
                                        drHotelRoomRateBreackUp["RateType"] = Convert.ToString(DTdSurchargerdate[dtdate]["SurchargeName"]);
                                        drHotelRoomRateBreackUp["OrderNo"] = cntno;
                                        drHotelRoomRateBreackUp["BoardTypeID"] = BoardTypeID;
                                        drHotelRoomRateBreackUp["FinalRateType"] = Convert.ToString(DTdSurchargerdate[dtdate]["FinalRateType"]);
                                        drHotelRoomRateBreackUp["VatdiffAmount"] = 0;
                                        drHotelRoomRateBreackUp["CompanyRate"] = Math.Round(Convert.ToDecimal(DTdSurchargerdate[dtdate]["Price"]), 6);
                                        drHotelRoomRateBreackUp["SupplierCurrency"] = CurrencyCode;
                                        drHotelRoomRateBreackUp["BuyingPrice"] = DateGross;
                                        if (GTAResponse.Tables[4].Columns.Contains("PriceWithoutROE"))
                                            drHotelRoomRateBreackUp["Total"] = Math.Round(Convert.ToDecimal(DTdSurchargerdate[dtdate]["PriceWithoutROE"]), 6);
                                        else
                                            drHotelRoomRateBreackUp["Total"] = Math.Round(Convert.ToDecimal(DTdSurchargerdate[dtdate]["Price"]), 6);

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

                                double ItemPrice = Math.Round(Convert.ToDouble(drRoomCategories[rc]["Price"]), 6);
                                double ItemOriginalPrice = Math.Round(Convert.ToDouble(drRoomCategories[rc]["OriginalPrice"]), 6);

                                double ItemSurchargePrice = Math.Round(Convert.ToDouble(drRoomCategories[rc]["SurchargePrice"]), 6);
                                double VatOnRateBrackUpnew = 0;
                                //double surchargewithmarkup = Common.ProcessHotelRateBreakupPrice_WithMarkup(1, SearchParam.CityID, SearchParam.CountryId, ItemSurchargePrice, B2CMarkup, IsParentId, isPerce, AgentPercentage, AgentMarkupAmount, SubAgentMarkup, ref VatOnRateBrackUpnew);
                                drhotelRoom["CompanyBuyingPrice"] = ItemPrice + ItemSurchargePrice;
                                drhotelRoom["SupplierCurrency"] = CurrencyCode;
                                drhotelRoom["BuyingPrice"] = Convert.ToString(drRoomCategories[rc]["PriceWithoutROE"]);

                                drhotelRoom["EssentialInformation"] = Convert.ToString(GTAResponse.Tables[0].Rows[h]["SPUI"]);
                                if (rc == 0)
                                {
                                    LowestRate = Math.Round(Convert.ToDouble(DTdrdate[0]["Price"]), 6);
                                    OriginalLowestRate = Math.Round(Convert.ToDouble(DTdrdate[0]["OriginalPrice"]), 6);
                                    //  BookingStatus = Convert.ToString(DTdrdate[0]["BookingStatus"]);
                                }
                                for (int dtdate = 0; dtdate <= DTdrdate.GetLength(0) - 1; dtdate++)
                                {
                                    if (BookingStatus == "" || BookingStatus == string.Empty || BookingStatus.ToLower().Trim() == "onrequest")
                                        BookingStatus = Convert.ToString(DTdrdate[dtdate]["BookingStatus"]);
                                }
                                if (Convert.ToString(GTAResponse.Tables[0].Rows[h]["FreeDiscount"]) != "" && Convert.ToString(GTAResponse.Tables[0].Rows[h]["FreeDiscount"]) != "0")
                                {
                                    string DiscountPer = Convert.ToString(GTAResponse.Tables[0].Rows[h]["FreeDiscount"]);
                                    DiscountPerTtext = Convert.ToString(GTAResponse.Tables[0].Rows[h]["FreeDiscount"]);// ""Save " + Convert.ToString(DiscountPer) + "% in discounts*";
                                    if (ItemPrice != ItemOriginalPrice)
                                    {
                                        drhotelRoom["IncudedOfferDiscount"] = ItemPrice;
                                        drhotelRoom["GrossWitohutDiscount"] = ItemOriginalPrice;
                                        drhotelRoom["DiscountPer"] = DiscountPer;
                                        drhotelRoom["DiscountPerText"] = DiscountPerTtext;

                                        //drhotelRoom["EssentialInformation"] = Convert.ToString(GTAResponse.Tables[0].Rows[h]["EssentialInformation"]);
                                        //drhotelRoom["EssentialInformation"] = Convert.ToString(GTAResponse.Tables[0].Rows[h]["SPUI"]);
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
                                    double b2cItemOriginalPriceMarkup = Math.Round((Math.Round((ItemOriginalPrice * B2CMarkup), 6) / 100), 6);
                                    ItemOriginalPrice = Math.Round(ItemOriginalPrice + b2cItemOriginalPriceMarkup, 6);
                                }
                                //Before Discount End 
                                drhotelRoom["CheckInDate"] = SearchParam.CheckInDate;
                                drhotelRoom["CheckOutDate"] = SearchParam.CheckOutDate;
                                drhotelRoom["CityId"] = SearchParam.CityID;
                                double surchargewithmarkup = _markup.ProcessHotelRateBreakupPrice_WithMarkup(1, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), ItemSurchargePrice, B2CMarkup, IsParentId, isPerce, AgentPercentage, AgentMarkupAmount, SubAgentMarkup, ref VatOnRateBrackUpnew);
                                drhotelRoom["CompanySellingPrice"] = ItemPrice + surchargewithmarkup;
                                #region  VAt
                                double VatdiffAmount = Convert.ToDouble(drhotelRoom["CompanySellingPrice"]);
                                //DataTable dtvatlist1 = await _commonMethod.GetVatCountrylist(Convert.ToString(SearchParam.CountryID) == "" ? 0 : Convert.ToInt32(SearchParam.CountryID), Convert.ToInt32(SearchParam.CityID), "hotel");
                                //if (dtvatlist1.Rows.Count > 0)
                                //{
                                //    drhotelRoom["CompanySellingPrice"] = Convert.ToDouble(drhotelRoom["CompanySellingPrice"]) + (Convert.ToDouble(_commonMethod.GetcalVatAmount(Convert.ToDecimal(drhotelRoom["CompanySellingPrice"]) - Convert.ToDecimal(drhotelRoom["CompanyBuyingPrice"]), Convert.ToDecimal(dtvatlist1.Rows[0]["IntRate"]))));
                                //    //ItemPrice = Convert.ToDouble(drhotelRoom["CompanySellingPrice"]);
                                //}
                                VatdiffAmount = Convert.ToDouble(drhotelRoom["CompanySellingPrice"]) - VatdiffAmount;
                                ItemPrice = ItemPrice + VatdiffAmount;
                                #endregion
                                if (IsB2C == false)
                                {
                                    if (IsParentId == 0)
                                    {
                                        drhotelRoom["AgentBuyingPrice"] = ItemPrice;
                                        double b2bSubMarkup = Math.Round((Math.Round(((ItemPrice) * SubAgentMarkup), 6) / 100), 6);
                                        ItemPrice = Math.Round(ItemPrice + b2bSubMarkup, 6);

                                        //Before Discount  Start
                                        if (Convert.ToString(drhotelRoom["IncudedOfferDiscount"]) != "" && Convert.ToDouble(drhotelRoom["IncudedOfferDiscount"]) != Convert.ToDouble(drhotelRoom["GrossWitohutDiscount"]))
                                        {
                                            double b2bOriginalSubMarkup = Math.Round((Math.Round((ItemOriginalPrice * SubAgentMarkup), 6) / 100), 6);
                                            ItemOriginalPrice = Math.Round(ItemOriginalPrice + b2bOriginalSubMarkup, 6);
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
                                    double b2bMarkup = Math.Round((Math.Round((ItemPrice * B2CMarkup), 6) / 100), 6);
                                    ItemPrice = ItemPrice + surchargewithmarkup;

                                    ItemPrice = ItemPrice + b2bMarkup;

                                    //Before Discount  Start
                                    if (Convert.ToString(drhotelRoom["IncudedOfferDiscount"]) != "" && Convert.ToDouble(drhotelRoom["IncudedOfferDiscount"]) != Convert.ToDouble(drhotelRoom["GrossWitohutDiscount"]))
                                    {
                                        double b2bOriginalMarkup = 0.0;
                                        if (isPerce)
                                            b2bOriginalMarkup = Math.Round((Math.Round((ItemOriginalPrice * AgentPercentage), 6) / 100), 6);
                                        else
                                            b2bOriginalMarkup = AgentMarkupAmount;
                                        ItemOriginalPrice = ItemOriginalPrice + ItemSurchargePrice;
                                        ItemOriginalPrice = ItemOriginalPrice + b2bOriginalMarkup;
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
                                    drhotelRoom["VatdiffAmount"] = VatdiffAmount;
                                    //Before Discount  Start
                                    if (Convert.ToString(drhotelRoom["IncudedOfferDiscount"]) != "" && Convert.ToDouble(drhotelRoom["IncudedOfferDiscount"]) != Convert.ToDouble(drhotelRoom["GrossWitohutDiscount"]))
                                    {
                                        drhotelRoom["GrossWitohutDiscount"] = ItemOriginalPrice;
                                    }
                                    //Before Discount End 
                                    drhotelRoom["SupplierName"] = "oth";
                                }
                                DtHotelRoom.Rows.Add(drhotelRoom);

                                ItemPrice_Text = Convert.ToString(ItemPrice);
                                if (rc == 0)
                                {
                                    if (isPerce)
                                    {
                                        if (AgentPercentage > 0)
                                        {
                                            double b2bMarkup = Math.Round((Math.Round((LowestRate * AgentPercentage), 6) / 100), 6);
                                            LowestRate = Math.Round(LowestRate + b2bMarkup, 6);
                                            double b2bOriginalMarkup = Math.Round((Math.Round((OriginalLowestRate * AgentPercentage), 6) / 100), 6);
                                            OriginalLowestRate = Math.Round(OriginalLowestRate + b2bOriginalMarkup, 6);
                                        }
                                    }
                                    else
                                    {
                                        LowestRate = Math.Round(LowestRate + AgentMarkupAmount, 6);
                                        OriginalLowestRate = Math.Round(OriginalLowestRate + AgentMarkupAmount, 6);
                                    }
                                    double b2cMarkup = Math.Round((Math.Round((LowestRate * B2CMarkup), 6) / 100), 6);
                                    LowestRate = Math.Round(LowestRate + b2cMarkup, 6);

                                    double b2cOriginalMarkup = Math.Round((Math.Round((OriginalLowestRate * B2CMarkup), 6) / 100), 6);
                                    OriginalLowestRate = Math.Round(OriginalLowestRate + b2cOriginalMarkup, 6);
                                    #region  VAt
                                    VatdiffAmount = LowestRate;
                                    //dtvatlist1 = await _commonMethod.GetVatCountrylist(Convert.ToString(SearchParam.CountryID) == "" ? 0 : Convert.ToInt32(SearchParam.CountryID), Convert.ToInt32(SearchParam.CityID), "hotel");
                                    //if (dtvatlist1.Rows.Count > 0)
                                    //{
                                    //    LowestRate = Convert.ToDouble(LowestRate) + (Convert.ToDouble(_commonMethod.GetcalVatAmount(Convert.ToDecimal(LowestRate) - Convert.ToDecimal(drhotelRoom["CompanyBuyingPrice"]), Convert.ToDecimal(dtvatlist1.Rows[0]["IntRate"]))));
                                    //}
                                    #endregion
                                    if (IsParentId == 0)
                                    {
                                        double b2bSubMarkup = Math.Round((Math.Round((LowestRate * SubAgentMarkup), 6) / 100), 6);
                                        LowestRate = Math.Round(LowestRate + b2bSubMarkup, 6);
                                        double b2bOriginalSubMarkup = Math.Round((Math.Round((OriginalLowestRate * SubAgentMarkup), 6) / 100), 6);
                                        OriginalLowestRate = Math.Round(OriginalLowestRate + b2bOriginalSubMarkup, 6);
                                    }


                                    int NoOfNoght = Convert.ToInt32(SearchParam.NoofNights);
                                }
                            }

                            string LocationText = CityName;
                            try
                            {
                                LocationText = Convert.ToString(GTAResponse.Tables[0].Rows[h]["Location"]);
                            }
                            catch (Exception ex)
                            {
                                await _exception.Error(ex);
                            }
                            //#region "Markup Calulation"
                            //double VatLowestRate = 0;
                            //LowestRate = Common.ProcessHotelRateBreakupPrice_WithMarkup(SearchParam.NoofNights, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), LowestRate, B2CMarkup, IsParentId, isPerce, AgentPercentage, AgentMarkupAmount, SubAgentMarkup, ref VatLowestRate);

                            //#endregion
                            drXmlHotel["Favorite"] = 0;
                            if (Rating == "" || Rating == "0")
                                Rating = "1";
                            CityName = Convert.ToString(SearchParam.CityName);
                            GTAImage1 = (_keySettings.staticimagepath + "/Images/Dubai/" + ownsystemhotelId + "/1-ThumbNail.jpg");
                            //string CurrencyCode = "USD";// Convert.ToString(ResponseOth[h].CurrencyCode);

                            drXmlHotel["GIITACode"] = giitaCode;
                            drXmlHotel["OwnsystemHotelId"] = internalHotelId; //ownsystemhotelId; //for extanet 27-08-2024 smit
                            drXmlHotel["RaynaId"] = ownsystemhotelId;
                            drXmlHotel["SupplierCurrency"] = CurrencyCode;
                            drXmlHotel["XmlHotelCity"] = CityId;
                            drXmlHotel["XMLHotelCode"] = internalHotelId;//#Due to Ticket#13957
                            drXmlHotel["HotelName"] = HotelName;
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
                            drXmlHotel["SellingPrice"] = LowestRate;
                            drXmlHotel["FinalSellingPrice"] = LowestRate;
                            drXmlHotel["BookingStatus"] = BookingStatus;
                            drXmlHotel["SupplierName"] = "oth";
                            drXmlHotel["OfficeID"] = 0;
                            drXmlHotel["ContractID"] = 0;
                            drXmlHotel["Reviews"] = 0;
                            drXmlHotel["UniqueNo"] = uniqueno;
                            drXmlHotel["IsShowSupplier"] = blnIsShowSupplier;
                            drXmlHotel["VatdiffAmount"] = 0;
                            drXmlHotel["MasterSupplierId"] = Convert.ToInt32(MasterSupplierID);

                            drXmlHotel["ReservationEmail"] = Convert.ToString(GTAResponse.Tables[0].Rows[h]["ReservationEmail"]);
                            drXmlHotel["HotelContactNo"] = Convert.ToString(GTAResponse.Tables[0].Rows[h]["HotelContactNo"]);
                            DtHotel.Rows.Add(drXmlHotel);
                            Options ops = (await _supplierRes.GetCommonRoomForSuppliers(Convert.ToInt64(giitaCode), tokenData, advancedOptions, LowestRate, drXmlHotel, DtHotelRoom, dtCancellation, DtHotelRoomRateBreackUp, SearchParam));
                            if (ops != null)
                                options.Add(ops);
                            BookingStatus = "";
                        }
                    }
                    #endregion

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
                        if (SearchParam != null && ResProccessTimerLogActive == "1" && timer_room_Oth.ElapsedMilliseconds > Convert.ToInt32(ResProccessTimeValueforSaveLog))
                        {
                            timer_room_Oth.Stop();
                            SearchParam.SupplierTime += ",|Finish:OTH Process Response  " + "|Time :" + timer_room_Oth.Elapsed.ToString();
                            _= Task.Run(() => _commonMethod.FireAway_SQL_ResPorcessTimeLog("", timer_room_Oth.Elapsed.ToString(), "OTH", SearchParam.SearchKey, 1, SearchParam.AgentID));
                        }
                    }
                    catch (Exception x)
                    {
                        await _exception.Error(x);
                    }
                    #endregion
                    //GC.Collect();


                }
            }
            return options;

        }
        public class OthListResponse
        {
            public string HotelName { get; set; }
            public int OwnsystemHotelId { get; set; }
            public int Rating { get; set; }
            public string Description { get; set; }
            public string Location { get; set; }
            public string Address { get; set; }
            public string Latitude { get; set; }
            public string Longitude { get; set; }
            public double BeforeDiscountPrice { get; set; }
            public double FinalSellingPrice { get; set; }
            public int Recommended { get; set; }
            public string FreeDiscount { get; set; }
            public string EssentialInformation { get; set; }
            public int GiitaId { get; set; }
            public int SortSequence { get; set; }
            public string GroupName { get; set; }
            public int ReviewCount { get; set; }
            public int ReviewRating { get; set; }
            public bool IsVATInclusive { get; set; }
            public double VATRate { get; set; }
            public string VATFromDate { get; set; }
            public string VATToDate { get; set; }
            public string CurrencyCode { get; set; }
            public string HotelContactNo { get; set; }
            public string ReservationEmail { get; set; }
            public int SupplierId { get; set; }
        }
    }
}
