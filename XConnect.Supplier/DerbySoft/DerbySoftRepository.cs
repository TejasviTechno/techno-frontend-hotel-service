using System;
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
using XConnect.Supplier.DerbySoft.Interface;
using Microsoft.Extensions.Options;
using Options = XConnect.Models.Options;
using static Azure.Core.HttpHeader;
using Microsoft.IdentityModel.Protocols;
using Microsoft.Data.SqlClient;
using XConnect.DAL;
using ZstdSharp.Unsafe;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Net;
using Azure;
using static XConnect.Models.ErrorLog;
using System.Data.Common;
using System.ComponentModel.DataAnnotations;

namespace XConnect.Supplier.DerbySoft
{
    public class DerbySoftRepository: IDerbySoft
    {
        private readonly ICommonMethod _commonMethod;
        private readonly IVCN _vcn;
        private readonly IException _exception;
        private readonly IMarkup _markup;
        private readonly ISupplierRes _supplierRes;
        private readonly KeySettings _keySettings;


        public DerbySoftRepository(ICommonMethod commonMethod, IMarkup markup, ISupplierRes supplierRes, IOptions<KeySettings> keySettings, IVCN vcn, IException exception)
        {
            _commonMethod = commonMethod;
            _markup = markup;
            _supplierRes = supplierRes;
            _keySettings = keySettings.Value;
            _vcn = vcn;
            _exception = exception;
        }

        public void GetDerbySoftRequestOnly_async(List<SupplierDetails> DerbySoftDetails, DataRow[] commonCode, List<StarRating> listRating, DataTable dtPreferredDataHotel, HotelSearchParameter SearchPara, List<SupplierSearchConfig> DerbySoftSearchConfig, ref ConcurrentBag<HotelX> AllTask, DataTable dtAgentDetail, DataSet dsAgentDetails, DataTable dtMarkupDetails, int Hit)
        {
            try
            {
                DataRow[] drrayna = dtPreferredDataHotel.Select("ServiceName='DerbySoft'");
                if (SearchPara.IsSearchFor.ToLower().IndexOf("preferred") >= 0 && drrayna.Count() == 0)
                    DerbySoftSearchConfig = null;
                //@Added on 29-5-21
                bool IsSame = true;

                for (int i = 0; i < SearchPara.Rooms.Count; i++)
                {
                    int NoOfAdt = SearchPara.Rooms[0].NoofAdults;
                    int NoOfChd = SearchPara.Rooms[0].NoofChild;
                    int ChildAge1 = SearchPara.Rooms[0].Child1Age;
                    int ChildAge2 = SearchPara.Rooms[0].Child2Age;
                    if (NoOfAdt != SearchPara.Rooms[i].NoofAdults || NoOfChd != SearchPara.Rooms[i].NoofChild || ChildAge1 != SearchPara.Rooms[i].Child1Age || ChildAge2 != SearchPara.Rooms[i].Child2Age)
                        IsSame = false;
                }
                //--
                if (IsSame)
                {
                    if (DerbySoftSearchConfig != null)
                    {
                        for (int s = 0; s < DerbySoftSearchConfig.Count; s++)
                        {
                            if (Convert.ToInt32(DerbySoftSearchConfig[s].CityID) == 0 || Convert.ToInt32(DerbySoftSearchConfig[s].CityID) == SearchPara.CityID)//All City means 0
                            {

                                //List<HotelCodeByCity> DtHotelDAta = FilterFinalList(commonCode, listRating);
                                if (commonCode.Length > 0)
                                {
                                    int Noofrecords = Convert.ToInt32(DerbySoftSearchConfig[s].Noofrecords);
                                    DataTable[] splittedtables = commonCode.AsEnumerable()
                                                         .Select((row, index) => new { row, index })
                                                         .GroupBy(x => x.index / Noofrecords)  // integer division, the fractional part is truncated
                                                         .Select(g => g.Select(x => x.row).CopyToDataTable())
                                                         .ToArray();

                                    for (int a = 0; a < splittedtables.Length; a++)
                                    {
                                        var paramValues1 = String.Join(",", splittedtables[a].Select().Select(x => String.Join(",", Convert.ToString(x.ItemArray[0]))));
                                        string IdsForSearch = Convert.ToString(paramValues1);

                                        HotelX xhotel = new HotelX();
                                        xhotel.Supplier = "derbysoft";
                                        xhotel.DebugeMode = true;
                                        xhotel.SearchTimeOut = Convert.ToInt32(DerbySoftSearchConfig[s].HotelSearchTimeOut);
                                        xhotel.ProcessTimeout = Convert.ToInt32(DerbySoftSearchConfig[s].HotelSearchTimeOut2);
                                        xhotel.SearchPara = SearchPara;
                                        if (dtAgentDetail != null && dtAgentDetail.Rows.Count > 0)
                                            xhotel.CID = Convert.ToString(dtAgentDetail.Rows[0]["AgentID"]);
                                        xhotel.NewPara = IdsForSearch;
                                        //xhotel.AgentWiseMarkup = DerbySoftDetails[0].AgentWiseMarkup;
                                        //xhotel.IsShowSupplier = Convert.ToInt16(DerbySoftDetails[0].IsShowSupplier) == 1 ? true : false;
                                        //xhotel.SupplierID = DerbySoftDetails[0].SupplierID;
                                        //xhotel.MasterSupplierID = DerbySoftDetails[0].MasterSupplierId;

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
                                        xhotel.TechName = "Derbysoft";
                                        xhotel.ReturnRooms = 5;
                                        #endregion
                                        AllTask.Add(xhotel);
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

        public string GenerateGTARequestAsync(HotelSearchParameter objSearchPara, decimal AgentPercentage)
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
                        objRequest.Append("<Room Type='" + GetType(item.NoofAdults) + "' ChildCount='" + item.NoofChild + "' AdultsCount='" + item.NoofAdults + "' ExtraBed='0' RateBasis='10785'>");//Question: What is 10785
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
                objRequest.Append("<HotelId>" + objSearchPara.HotelID.ToString() + "</HotelId>");
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
        public string GetType(int NoofAdults)
        {
            string rmtype = string.Empty;
            switch (NoofAdults)
            {
                case 1:
                    rmtype = "Single";
                    break;
                case 2:
                    rmtype = "Double";
                    break;
                case 3:
                    rmtype = "Triple";
                    break;
                case 4:
                    rmtype = "Quad";
                    break;
            }
            return rmtype;
        }

        public async Task<List<Options>> Derbysoft_HotelSearchWithRooms_prtk(HotelX Item, CommonSearch commonSearch, string currency, string Response, HotelSearchParameter SearchParam, DataTable dtAgentDetail, DataTable dtSuplierMarkup, DataTable dtMarkupDetails, string Supplier, DataSet dsAgentDetails, DataTable dtCountryCityMarkup, DataTable dtROE, DataSet DerbyResponse, Int32 processTimeout, string ResProccessTimerLogActive, string ResProccessTimeValueforSaveLog)
        {
            System.Diagnostics.Stopwatch timerroomx = new System.Diagnostics.Stopwatch();
            timerroomx.Start();
            List<Options> options = new List<Options>();

            if (DerbyResponse != null)
            {


                DataTable DtHotel = _commonMethod.GenerateDataTableNewV2();
                DataTable dtHotelAllocation = _commonMethod.GenerateHotelAllocationTableNewV2();
                DataTable dtComplementary = _commonMethod.GenerateComplementaryTableNewV2();
                DataTable dtCommonInfo = _commonMethod.GenerateCommonInfoTableNewV2();
                DataTable dtHotelMinStay = _commonMethod.GenerateHotelMinStayNewV2();

                DataSet dsTemp = new DataSet();

                int CutOffDays = CacheMaster.CutOffDays;
                CutOffDays = _commonMethod.GetCutOffDays(commonSearch.dtSuplierMarkup, "derbysoft");

                double SubAgentMarkup = 0;
                int IsParentId = 1;
                if (dtAgentDetail != null)
                {
                    IsParentId = Convert.ToInt32(dtAgentDetail.Rows[0]["IsParentId"]);
                    SubAgentMarkup = Convert.ToDouble(dtAgentDetail.Rows[0]["B2CMarkupPer"]);
                }

                try
                {
                    string Rating = "";
                    double AgentPercentage = 0.0;
                    Boolean blnIsShowSupplier = false;
                    double AgentMarkupAmount = 0.0;
                    Boolean isPerce = true;
                    int MasterSupplierID = 0;
                    int SupplierID = 0;

                    if (dtSuplierMarkup != null)
                    {
                        dtSuplierMarkup.DefaultView.RowFilter = "SupplierName='derbysoft'";
                        if (dtSuplierMarkup.DefaultView.Count > 0)
                        {
                            AgentPercentage = Convert.ToDouble(dtSuplierMarkup.DefaultView[0]["AgentWiseMarkup"]);
                            blnIsShowSupplier = (Convert.ToInt16(dtSuplierMarkup.DefaultView[0]["IsShowSupplier"]) == 1 ? true : false);
                            MasterSupplierID = Convert.ToInt32(dtSuplierMarkup.DefaultView[0]["MasterSupplierId"]);
                            SupplierID = Convert.ToInt32(dtSuplierMarkup.DefaultView[0]["SupplierId"]);
                        }
                    }

                    int NoOfNight = Convert.ToInt32(SearchParam.NoofNights);
                    int NoOfRoom = Convert.ToInt32(SearchParam.Rooms.Count);
                    int RoomNightMultiply = NoOfNight * NoOfRoom;

                    int Adult = 0;
                    int ChildNo = 0;

                    for (int x = 0; x < NoOfRoom; x++)
                    {
                        if (x == 0)
                        {
                            Adult = Convert.ToInt32(SearchParam.Rooms[x].NoofAdults);
                            ChildNo = Convert.ToInt32(SearchParam.Rooms[x].NoofChild);
                        }
                        else
                        {
                            Adult = Adult + Convert.ToInt32(SearchParam.Rooms[x].NoofAdults);
                            ChildNo = ChildNo + Convert.ToInt32(SearchParam.Rooms[x].NoofChild);
                        }
                    }
                    int TotalPerson = Adult + ChildNo;


                    int hotelCount = 0;
                    DataView Hotelview = new DataView();
                    DataTable distinctHotelView = new DataTable();

                    if (DerbyResponse == null)
                    {
                        Item.ResponseType = "NoCount";
                        return options;
                    }
                    Hotelview = new DataView(DerbyResponse.Tables[0]);
                    distinctHotelView = Hotelview.ToTable(true, "HotelId", "Currency", "SupplierId");
                    hotelCount = distinctHotelView.Rows.Count;

                    DataView DistinctHotelRoom = Hotelview = new DataView(DerbyResponse.Tables["Table"]);

                    DataTable DistinctHotelRoomlist = DistinctHotelRoom.ToTable(true, "RoomName", "HotelId", "SupplierId", "RateId", "RoomId", "MealPlans_Text", "Currency", "RateType");

                    //if (hotelCount != 0)
                    //{
                    //    item.ResponseType = "Success";
                    //}
                    //else
                    //{
                    //    item.ResponseType = "NoCount";
                    //}

                    for (int h = 0; h < hotelCount; h++)
                    {
                        Item.ResponseType = "Success";
                        DataRow[] drDerbySoftRoom = DistinctHotelRoomlist.Select("HotelId='" + Convert.ToString(distinctHotelView.Rows[h]["HotelId"]) + "'");

                        Int64 giitaCode = 0;
                        string CurrencyCode = currency;

                        if (SupplierID > 0)
                            MasterSupplierID = SupplierID;
                        string agent = "";
                        if (dtAgentDetail != null && dtAgentDetail.Rows.Count > 0)
                            agent = Convert.ToString(dtAgentDetail.Rows[0]["AgentID"]);
                        #region "When not Need Room"  

                        #region "Hotel Code Block "
                        DataRow drXmlHotel = DtHotel.NewRow();
                        string ItemCode = "0";
                        string OfferCode = "";
                        string DerbySoftCurrency = "";

                        if (SearchParam.Filters.IsShowRooms == 0)
                        {
                            #region PriceOnly RS
                            if (distinctHotelView != null)
                            {
                                double FinalPricenew = 0;

                                if (DerbyResponse.Tables.Contains("Table") == true)
                                {
                                    double RoomPrice = 0;
                                    double FinalPrice = 0;
                                    double CurrencyFactorDerbySoft = 1;
                                    DerbySoftCurrency = Convert.ToString(distinctHotelView.Rows[h]["Currency"]);
                                    if (DerbySoftCurrency.ToLower() != "usd")
                                    {
                                        CurrencyFactorDerbySoft = _commonMethod.GetROEByCurrency(DerbySoftCurrency, dtROE);
                                        if (CurrencyFactorDerbySoft == 0)
                                            break;
                                    }

                                    bool flag = true;

                                    if (drDerbySoftRoom.Length > 0)
                                    {
                                        for (int rid = 0; rid < drDerbySoftRoom.GetLength(0); rid++)
                                        {

                                            DataRow[] drhotellist = DerbyResponse.Tables["Table"].Select("HotelId='" + drDerbySoftRoom[rid]["HotelId"] + "' and SupplierId ='" + drDerbySoftRoom[rid]["SupplierId"] + "' and RateId ='" + drDerbySoftRoom[rid]["RateId"] + "' and RoomId ='" + drDerbySoftRoom[rid]["RoomId"] + "' and MealPlans_Text ='" + drDerbySoftRoom[rid]["MealPlans_Text"] + "' ");
                                            if (drhotellist.Length > 0)
                                            {
                                                RoomPrice = 0;

                                                if (drhotellist.Length == NoOfNight)
                                                {

                                                    for (int i = 0; i < drhotellist.GetLength(0); i++)
                                                    {
                                                        FinalPrice = 0;

                                                        {
                                                            double TotalFeees = 0;
                                                            if (DerbyResponse.Tables["Table"].Columns.Contains("NightRate"))
                                                            {
                                                                FinalPrice = Convert.ToDouble(drhotellist[i]["NightRate"]);

                                                                DataRow[] DrFeesList = DerbyResponse.Tables["Table1"].Select("RateId='" + Convert.ToString(drhotellist[i]["RateId"]) + "' and RoomId='" + Convert.ToString(drhotellist[i]["RoomId"]) + "' and HotelId='" + Convert.ToString(drhotellist[i]["HotelId"]) + "'");
                                                                if (DrFeesList.Length > 0)
                                                                {

                                                                    for (int Fid = 0; Fid < DrFeesList.GetLength(0); Fid++)
                                                                    {
                                                                        string FeeType = Convert.ToString(DrFeesList[Fid]["Type"]);
                                                                        string amounttype = Convert.ToString(DrFeesList[Fid]["AmountType"]);
                                                                        string chargetype = Convert.ToString(DrFeesList[Fid]["ChargeType"]);
                                                                        double Fees = 0;
                                                                        double fee = Convert.ToDouble(DrFeesList[Fid]["Amount"]);
                                                                        if (FeeType != "Inclusive")
                                                                        {
                                                                            if (amounttype == "Fix")
                                                                            {
                                                                                if (chargetype == "PerRoomPerNight")
                                                                                {
                                                                                    Fees = fee * NoOfRoom * 1;// NoOfNight;
                                                                                    TotalFeees = TotalFeees + Fees;
                                                                                }
                                                                                else if (chargetype == "PerPersonPerNight")
                                                                                {
                                                                                    Fees = fee * TotalPerson * 1;// NoOfNight;
                                                                                    TotalFeees = TotalFeees + Fees;
                                                                                }
                                                                                else if (chargetype == "PerRoomPerStay")
                                                                                {
                                                                                    Fees = fee * NoOfRoom;
                                                                                    TotalFeees = TotalFeees + Fees;
                                                                                }
                                                                                else if (chargetype == "PerPersonPerStay")
                                                                                {
                                                                                    Fees = fee * TotalPerson;
                                                                                    TotalFeees = TotalFeees + Fees;
                                                                                }
                                                                            }
                                                                            else if (amounttype == "Percent")
                                                                            {
                                                                                double feev = (FinalPrice * fee) / 100;
                                                                                if (chargetype == "PerRoomPerNight")
                                                                                {
                                                                                    Fees = feev * NoOfRoom * 1;// NoOfNight;
                                                                                    TotalFeees = TotalFeees + Fees;
                                                                                }
                                                                                else if (chargetype == "PerPersonPerNight")
                                                                                {
                                                                                    Fees = feev * TotalPerson * 1;// NoOfNight;
                                                                                    TotalFeees = TotalFeees + Fees;
                                                                                }
                                                                                else if (chargetype == "PerRoomPerStay")
                                                                                {
                                                                                    Fees = feev * NoOfRoom;
                                                                                    TotalFeees = TotalFeees + Fees;
                                                                                }
                                                                                else if (chargetype == "PerPersonPerStay")
                                                                                {
                                                                                    Fees = feev * TotalPerson;
                                                                                    TotalFeees = TotalFeees + Fees;
                                                                                }
                                                                            }
                                                                        }
                                                                    }
                                                                }

                                                                if (i == 0)
                                                                {
                                                                    RoomPrice = (TotalFeees + Convert.ToDouble(Convert.ToDouble(drhotellist[i]["NightRate"])) * NoOfRoom) * CurrencyFactorDerbySoft;

                                                                }
                                                                else
                                                                {
                                                                    RoomPrice = RoomPrice + (TotalFeees + Convert.ToDouble(Convert.ToDouble(drhotellist[i]["NightRate"])) * NoOfRoom) * CurrencyFactorDerbySoft;
                                                                }
                                                            }
                                                        }
                                                    }
                                                    if (RoomPrice != 0)
                                                    {
                                                        if (flag)
                                                        {
                                                            FinalPricenew = RoomPrice;
                                                            flag = false;
                                                        }
                                                        else if (FinalPricenew > RoomPrice)
                                                        {
                                                            FinalPricenew = RoomPrice;
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    if (FinalPricenew == 0)
                                    {
                                        continue;
                                    }

                                    drXmlHotel["XMLCountry"] = Convert.ToString(SearchParam.CountryID);
                                    drXmlHotel["XMLCity"] = Convert.ToString(Convert.ToString(distinctHotelView.Rows[h]["SupplierId"]));
                                    drXmlHotel["XmlHotelCity"] = Convert.ToString(Convert.ToString(distinctHotelView.Rows[h]["SupplierId"]));
                                    drXmlHotel["XMLHotelCode"] = Convert.ToString(Convert.ToString(distinctHotelView.Rows[h]["HotelId"]));
                                    drXmlHotel["EssentialInformation"] = "";
                                    drXmlHotel["Description"] = "";
                                    drXmlHotel["SupplierName"] = "derbysoft";
                                    drXmlHotel["OfficeID"] = 0;
                                    drXmlHotel["SupplierCurrency"] = DerbySoftCurrency;
                                    drXmlHotel["ContractID"] = 0;
                                    //drXmlHotel["Rating"] = Rating;
                                    drXmlHotel["Reviews"] = 0;
                                    drXmlHotel["UniqueNo"] = Convert.ToString(Convert.ToString(distinctHotelView.Rows[h]["HotelId"]));
                                    drXmlHotel["IsShowSupplier"] = false;
                                    drXmlHotel["BookingStatus"] = "Available";
                                    drXmlHotel["AvailToken"] = ItemCode;
                                    drXmlHotel["FinalSellingPrice"] = FinalPricenew;
                                    drXmlHotel["SellingPrice"] = FinalPricenew;
                                    //drXmlHotel["GIITACode"] = giitaCode;
                                    //drXmlHotel["RaynaId"] = ItemCode;
                                    DtHotel.Rows.Add(drXmlHotel);
                                    double FinalMinPrice = FinalPricenew;
                                    if (FinalMinPrice > 0)
                                    {
                                        #region "Room code Block"
                                        bool willAddOrNot = true;
                                        Int64 RaynaID = 0;
                                        //Int64 GiataId = 0;
                                        int RatingMain = 0;
                                        Int64 OwnsystemHotelId = 0;
                                        _markup.GetHotelDataCommon(ref FinalMinPrice, ref willAddOrNot, ref OwnsystemHotelId, ref RaynaID, ref giitaCode, ref RatingMain, ref drXmlHotel, commonSearch.AgentId, commonSearch.TariffID, commonSearch.dsAgentDetails, commonSearch.supplierGiataMaster, SearchParam, commonSearch.dtCountryCityMarkup, commonSearch.B2CMarkup, commonSearch.IsParentId, commonSearch.SubAgentMarkup, commonSearch.SearchByHotel, commonSearch.HotelIDS);
                                        if (willAddOrNot)
                                        {
                                            #region "Room Logic"
                                            DataTable dt_HotelRoom = _commonMethod.GenerateRoomtypeDataTableNewV2();
                                            DataTable dt_Cancellation = _commonMethod.GenerateCancellationDataTableNewV2();
                                            DataTable dt_RateBreackUp = _commonMethod.GenerateoomRateBreackUpDataTableNewV2();
                                            #endregion
                                            decimal LowestPrice = Convert.ToDecimal(FinalMinPrice).CalculatePrice(commonSearch.tokenData, commonSearch.advancedOptions, DerbySoftCurrency);
                                            Options ops1 = (await _supplierRes.GetCommonRoomForSuppliers(giitaCode, commonSearch.tokenData, commonSearch.advancedOptions, Convert.ToDouble(LowestPrice), drXmlHotel, dt_HotelRoom, dt_Cancellation, dt_RateBreackUp, SearchParam));
                                            if (ops1 != null)
                                                options.Add(ops1);
                                        }
                                        #endregion
                                    }
                                }
                            }
                        }
                        #endregion

                        #endregion

                        #endregion
                        else
                        {
                            #region Hotels
                            #region "Hotel Code Block "
                            double minRate = 1;
                            //DataRow drXmlHotel = DtHotel.NewRow();
                            drXmlHotel["XMLHotelCode"] = Convert.ToString(distinctHotelView.Rows[h]["HotelId"]);
                            drXmlHotel["HotelName"] = "";
                            drXmlHotel["SupplierName"] = "Derbysoft";
                            drXmlHotel["OfficeID"] = 0;
                            drXmlHotel["ContractID"] = 0;
                            drXmlHotel["Rating"] = 0;
                            drXmlHotel["Reviews"] = 0;
                            drXmlHotel["UniqueNo"] = 0;
                            drXmlHotel["IsShowSupplier"] = false;
                            drXmlHotel["BookingStatus"] = "Available";
                            drXmlHotel["XMLCity"] = "";
                            drXmlHotel["SupplierCurrency"] = Convert.ToString(distinctHotelView.Rows[h]["Currency"]); ;
                            drXmlHotel["FinalSellingPrice"] = minRate;
                            drXmlHotel["SellingPrice"] = minRate;
                            drXmlHotel["OfferText"] = "";
                            DtHotel.Rows.Add(drXmlHotel);
                            //FinalTotal = minRate;
                            #endregion
                            double Pricenew = minRate;
                            #region "GetRooms for this hotel"
                            bool willAddOrNot = true;
                            Int64 RaynaID = 0;
                            Int64 GiataId = 0;
                            int RatingHotel = 0;
                            Int64 OwnsystemHotelId = 0;
                            string Provider = "Derbysoft";
                            double MinPrice = Pricenew;
                            _markup.GetHotelDataCommon(ref MinPrice, ref willAddOrNot, ref OwnsystemHotelId, ref RaynaID, ref GiataId, ref RatingHotel, ref drXmlHotel, commonSearch.AgentId, commonSearch.TariffID, commonSearch.dsAgentDetails, commonSearch.supplierGiataMaster, SearchParam, commonSearch.dtCountryCityMarkup, commonSearch.B2CMarkup, commonSearch.IsParentId, commonSearch.SubAgentMarkup, commonSearch.SearchByHotel, commonSearch.HotelIDS);
                            if (willAddOrNot)
                            {
                                #region "Room Logic"
                                if (SearchParam.Filters.IsShowRooms == 1)
                                {
                                    #region
                                    DataTable DtHotelRoom = _commonMethod.GenerateRoomtypeDataTableNewV2();
                                    DataTable dtDerbyCancellation = _commonMethod.GenerateCancellationDataTableNewV2();
                                    DataTable DtHotelRoomRateBreackUp = _commonMethod.GenerateoomRateBreackUpDataTableNewV2();

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
                                    #endregion
                                    #region RoomCode
                                    // Room Code Here
                                    int noofnight = Convert.ToInt32(SearchParam.NoofNights);
                                    int noofRooms = Convert.ToInt32(SearchParam.Rooms.Count);
                                    if (DerbyResponse != null && DerbyResponse.Tables.Count > 0)
                                    {
                                        if (DerbyResponse.Tables.Contains("Table") == true)
                                        {
                                            double CurrencyFactorDerbySoft = 1;

                                            if (DerbyResponse.Tables["Table"].Columns.Contains("Currency") == true)
                                            {
                                                DerbySoftCurrency = Convert.ToString(distinctHotelView.Rows[h]["Currency"]);
                                            }
                                            if (DerbySoftCurrency.ToLower() != "usd")
                                            {
                                                CurrencyFactorDerbySoft = _commonMethod.GetROEByCurrency(DerbySoftCurrency, dtROE);
                                            }


                                            for (int Rid = 0; Rid < drDerbySoftRoom.Length; Rid++)
                                            {
                                                string HotelId = "", RoomName = "", RateName = "", RoomId = "", RateId = "";
                                                string MealId = "", MealName = "";
                                                string SupplierId = "";
                                                string Type = "";
                                                string RateType = "";
                                                double MainPrice = 0;

                                                string DistributorId = "";
                                                string Version = "";

                                                if (DerbyResponse.Tables["Table"].Columns.Contains("HotelId") == true)
                                                {
                                                    HotelId = Convert.ToString(drDerbySoftRoom[Rid]["HotelId"]);
                                                }
                                                if (DerbyResponse.Tables["Table"].Columns.Contains("RoomId") == true)
                                                {
                                                    RoomId = Convert.ToString(drDerbySoftRoom[Rid]["RoomId"]);
                                                }
                                                if (DerbyResponse.Tables["Table"].Columns.Contains("RateId") == true)
                                                {
                                                    RateId = Convert.ToString(drDerbySoftRoom[Rid]["RateId"]);
                                                }
                                                if (DerbyResponse.Tables["Table"].Columns.Contains("RoomName") == true)
                                                {
                                                    RoomName = Convert.ToString(drDerbySoftRoom[Rid]["RoomName"]);
                                                }

                                                if (DerbyResponse.Tables["Table"].Columns.Contains("MealPlans_Text"))
                                                {
                                                    MealId = Convert.ToString(drDerbySoftRoom[Rid]["MealPlans_Text"]);
                                                }
                                                if (DerbyResponse.Tables["Table"].Columns.Contains("MealPlans_Text"))
                                                {
                                                    MealName = Convert.ToString(drDerbySoftRoom[Rid]["MealPlans_Text"]);
                                                }
                                                if (DerbyResponse.Tables["Table"].Columns.Contains("SupplierId"))
                                                {
                                                    SupplierId = Convert.ToString(drDerbySoftRoom[Rid]["SupplierId"]);
                                                }
                                                if (DerbyResponse.Tables["Table"].Columns.Contains("RateType"))
                                                {
                                                    RateType = Convert.ToString(drDerbySoftRoom[Rid]["RateType"]);
                                                }
                                                if (DerbyResponse.Tables["Table"].Columns.Contains("Type"))
                                                {
                                                    Type = Convert.ToString(drDerbySoftRoom[Rid]["Type"]);
                                                }
                                                DistributorId = "WITHINEARTH"; //add static
                                                Version = "v4";  //add static

                                                string RoomTypeId = "";

                                                double RoomPrice = 0;
                                                string WeekColumn = "";

                                                string uniqid = Guid.NewGuid().ToString();
                                                if (DerbyResponse.Tables["Table"].Columns.Contains("RoomName")
                                                         && DerbyResponse.Tables["Table"].Columns.Contains("RateId") && DerbyResponse.Tables["Table"].Columns.Contains("RoomId")
                                                         && DerbyResponse.Tables["Table"].Columns.Contains("HotelId")
                                                         && DerbyResponse.Tables["Table"].Columns.Contains("MainDate")
                                                         )
                                                {
                                                    DataRow[] DrRoomListcheck = DerbyResponse.Tables["Table"].Select("RoomName='" + RoomName + "' and RateId='" + RateId + "' and RoomId='" + RoomId + "' and HotelId='" + HotelId + "'");
                                                    if (DrRoomListcheck.Length > 0)
                                                    {
                                                        if (DrRoomListcheck.Length != SearchParam.NoofNights)
                                                        {
                                                            continue;
                                                        }
                                                    }
                                                }
                                                string PriceTh = ""; string Token = "";
                                                for (int Nid = 0; Nid < noofnight; Nid++)
                                                {
                                                    string dtcc = SearchParam.CheckInDate.Substring(3, 2) + "/" + SearchParam.CheckInDate.Substring(0, 2) + "/" + SearchParam.CheckInDate.Substring(6, 4);
                                                    DateTime dt = Convert.ToDateTime(dtcc);
                                                    string datetime = Convert.ToString(dt.Date.AddDays(Nid));
                                                    WeekColumn = Convert.ToString(dt.Date.AddDays(Nid).DayOfWeek).Substring(0, 3);

                                                    DateTime sdate = Convert.ToDateTime(SearchParam.CheckInDate.Substring(3, 2) + "/" + SearchParam.CheckInDate.Substring(0, 2) + "/" + SearchParam.CheckInDate.Substring(6, 4)).AddDays(Nid);
                                                    if (DerbyResponse.Tables.Contains("Table") == true)
                                                    {
                                                        if (DerbyResponse.Tables["Table"].Columns.Contains("RoomName")
                                                            && DerbyResponse.Tables["Table"].Columns.Contains("RateId") && DerbyResponse.Tables["Table"].Columns.Contains("RoomId")
                                                            && DerbyResponse.Tables["Table"].Columns.Contains("HotelId")
                                                            && DerbyResponse.Tables["Table"].Columns.Contains("MainDate")
                                                            )
                                                        {
                                                            DataRow[] DrRoomList = DerbyResponse.Tables["Table"].Select("RoomName='" + RoomName + "' and RateId='" + RateId + "' and RoomId='" + RoomId + "' and HotelId='" + HotelId + "'and MainDate='" + sdate + "'");
                                                            if (DrRoomList.Length > 0)
                                                            {
                                                                if (DerbyResponse.Tables["Table"].Columns.Contains("NightRate"))
                                                                {
                                                                    if (Nid == 0)
                                                                    {
                                                                        PriceTh = Convert.ToString(DrRoomList[0]["NightRate"]);
                                                                    }
                                                                    else
                                                                    {
                                                                        PriceTh = PriceTh + "-" + Convert.ToString(DrRoomList[0]["NightRate"]);
                                                                    }
                                                                    Token = Convert.ToString(DrRoomList[0]["Token"]);
                                                                }
                                                            }
                                                        }
                                                    }
                                                }

                                                RoomTypeId = RoomId + "$" + RateId + "$" + MealId + "$" + MealName + "$" + HotelId + "$" + SupplierId + "$" + Convert.ToString(PriceTh) + "$" + RateType + "$" + Type + "$" + Token + "$" + DistributorId + "$" + Version + "$" + DerbySoftCurrency;

                                                for (int Nid = 0; Nid < noofnight; Nid++)
                                                {
                                                    string dtcc = SearchParam.CheckInDate.Substring(3, 2) + "/" + SearchParam.CheckInDate.Substring(0, 2) + "/" + SearchParam.CheckInDate.Substring(6, 4);
                                                    DateTime dt = Convert.ToDateTime(dtcc);
                                                    string datetime = Convert.ToString(dt.Date.AddDays(Nid));
                                                    WeekColumn = Convert.ToString(dt.Date.AddDays(Nid).DayOfWeek).Substring(0, 3);

                                                    double DateGross = 0;
                                                    DateTime sdate = Convert.ToDateTime(SearchParam.CheckInDate.Substring(3, 2) + "/" + SearchParam.CheckInDate.Substring(0, 2) + "/" + SearchParam.CheckInDate.Substring(6, 4)).AddDays(Nid);
                                                    if (DerbyResponse.Tables.Contains("Table") == true)
                                                    {
                                                        if (DerbyResponse.Tables["Table"].Columns.Contains("RoomName")
                                                            && DerbyResponse.Tables["Table"].Columns.Contains("RateId") && DerbyResponse.Tables["Table"].Columns.Contains("RoomId")
                                                            && DerbyResponse.Tables["Table"].Columns.Contains("HotelId")
                                                            && DerbyResponse.Tables["Table"].Columns.Contains("MainDate")
                                                            )
                                                        {
                                                            DataRow[] DrRoomList = DerbyResponse.Tables["Table"].Select("RoomName='" + RoomName + "' and RateId='" + RateId + "' and RoomId='" + RoomId + "' and HotelId='" + HotelId + "'and MainDate='" + sdate + "'");
                                                            if (DrRoomList.Length > 0)
                                                            {
                                                                NoOfNight = Convert.ToInt32(SearchParam.NoofNights);
                                                                NoOfRoom = Convert.ToInt32(SearchParam.Rooms.Count);
                                                                RoomNightMultiply = NoOfNight * NoOfRoom;

                                                                Adult = 0;
                                                                ChildNo = 0;

                                                                for (int x = 0; x < NoOfRoom; x++)
                                                                {
                                                                    if (x == 0)
                                                                    {
                                                                        Adult = Convert.ToInt32(SearchParam.Rooms[x].NoofAdults);
                                                                        ChildNo = Convert.ToInt32(SearchParam.Rooms[x].NoofChild);
                                                                    }
                                                                    else
                                                                    {
                                                                        Adult = Adult + Convert.ToInt32(SearchParam.Rooms[x].NoofAdults);
                                                                        ChildNo = ChildNo + Convert.ToInt32(SearchParam.Rooms[x].NoofChild);
                                                                    }
                                                                }
                                                                TotalPerson = Adult + ChildNo;
                                                                double TotalFeees = 0;
                                                                if (DerbyResponse.Tables["Table"].Columns.Contains("NightRate"))
                                                                {
                                                                    MainPrice = Convert.ToDouble(DrRoomList[0]["NightRate"]);

                                                                    DataRow[] DrFeesList = DerbyResponse.Tables["Table1"].Select("RateId='" + RateId + "' and RoomId='" + RoomId + "' and HotelId='" + HotelId + "'");
                                                                    if (DrFeesList.Length > 0)
                                                                    {

                                                                        for (int Fid = 0; Fid < DrFeesList.GetLength(0); Fid++)
                                                                        {
                                                                            string FeeType = Convert.ToString(DrFeesList[Fid]["Type"]);
                                                                            string amounttype = Convert.ToString(DrFeesList[Fid]["AmountType"]);
                                                                            string chargetype = Convert.ToString(DrFeesList[Fid]["ChargeType"]);
                                                                            double Fees = 0;
                                                                            double fee = Convert.ToDouble(DrFeesList[Fid]["Amount"]);
                                                                            if (FeeType != "Inclusive")
                                                                            {
                                                                                if (amounttype == "Fix")
                                                                                {
                                                                                    if (chargetype == "PerRoomPerNight")
                                                                                    {
                                                                                        Fees = fee * NoOfRoom * 1;// NoOfNight;
                                                                                        TotalFeees = TotalFeees + Fees;
                                                                                    }
                                                                                    else if (chargetype == "PerPersonPerNight")
                                                                                    {
                                                                                        Fees = fee * TotalPerson * 1;// NoOfNight;
                                                                                        TotalFeees = TotalFeees + Fees;
                                                                                    }
                                                                                    else if (chargetype == "PerRoomPerStay")
                                                                                    {
                                                                                        Fees = fee * NoOfRoom;
                                                                                        TotalFeees = TotalFeees + Fees;
                                                                                    }
                                                                                    else if (chargetype == "PerPersonPerStay")
                                                                                    {
                                                                                        Fees = fee * TotalPerson;
                                                                                        TotalFeees = TotalFeees + Fees;
                                                                                    }
                                                                                }
                                                                                else if (amounttype == "Percent")
                                                                                {
                                                                                    double feev = (MainPrice * fee) / 100;
                                                                                    if (chargetype == "PerRoomPerNight")
                                                                                    {
                                                                                        Fees = feev * NoOfRoom * 1;// NoOfNight;
                                                                                        TotalFeees = TotalFeees + Fees;
                                                                                    }
                                                                                    else if (chargetype == "PerPersonPerNight")
                                                                                    {
                                                                                        Fees = feev * TotalPerson * 1;// NoOfNight;
                                                                                        TotalFeees = TotalFeees + Fees;
                                                                                    }
                                                                                    else if (chargetype == "PerRoomPerStay")
                                                                                    {
                                                                                        Fees = feev * NoOfRoom;
                                                                                        TotalFeees = TotalFeees + Fees;
                                                                                    }
                                                                                    else if (chargetype == "PerPersonPerStay")
                                                                                    {
                                                                                        Fees = feev * TotalPerson;
                                                                                        TotalFeees = TotalFeees + Fees;
                                                                                    }
                                                                                }
                                                                            }
                                                                        }
                                                                    }

                                                                    if (Nid == 0)
                                                                    {
                                                                        RoomPrice = (TotalFeees + Convert.ToDouble(DrRoomList[0]["NightRate"]) * noofRooms) * CurrencyFactorDerbySoft;
                                                                        DateGross = (TotalFeees + Convert.ToDouble(DrRoomList[0]["NightRate"]) * noofRooms) * CurrencyFactorDerbySoft;
                                                                    }
                                                                    else
                                                                    {
                                                                        RoomPrice = RoomPrice + (TotalFeees + Convert.ToDouble(DrRoomList[0]["NightRate"]) * noofRooms) * CurrencyFactorDerbySoft;
                                                                        DateGross = (TotalFeees + Convert.ToDouble(DrRoomList[0]["NightRate"]) * noofRooms) * CurrencyFactorDerbySoft;
                                                                    }
                                                                }

                                                            }
                                                        }

                                                        #region Vat
                                                        double VatOnRateBrackup = 0;
                                                        DateGross = _markup.ProcessHotelRateBreakupPrice_WithMarkup(1, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), DateGross, commonSearch.B2CMarkup, IsParentId, isPerce, AgentPercentage, AgentMarkupAmount, SubAgentMarkup, ref VatOnRateBrackup);
                                                        #endregion


                                                        DataRow drDerbyRoomRateBreackUp = DtHotelRoomRateBreackUp.NewRow();
                                                        drDerbyRoomRateBreackUp["RoomNo"] = Convert.ToString("1");
                                                        drDerbyRoomRateBreackUp["SupplierName"] = "DerbySoft";
                                                        drDerbyRoomRateBreackUp["OwnsystemHotelId"] = OwnsystemHotelId;
                                                        drDerbyRoomRateBreackUp["ContractID"] = 0;
                                                        drDerbyRoomRateBreackUp["RoomTypeName"] = RoomName;
                                                        drDerbyRoomRateBreackUp["RoomTypeId"] = RoomTypeId;
                                                        drDerbyRoomRateBreackUp["MealId"] = MealId;
                                                        drDerbyRoomRateBreackUp["Meal"] = MealName;
                                                        drDerbyRoomRateBreackUp["BookingStatus"] = "Available";
                                                        drDerbyRoomRateBreackUp["DateOfWeek"] = WeekColumn;
                                                        drDerbyRoomRateBreackUp["Date"] = datetime;
                                                        drDerbyRoomRateBreackUp["Rate"] = DateGross;
                                                        drDerbyRoomRateBreackUp["VatdiffAmount"] = VatOnRateBrackup;
                                                        drDerbyRoomRateBreackUp["RateType"] = "A Rate";
                                                        drDerbyRoomRateBreackUp["OrderNo"] = 1;
                                                        drDerbyRoomRateBreackUp["RoomSequenceNo"] = uniqid;
                                                        drDerbyRoomRateBreackUp["BoardTypeID"] = "";
                                                        drDerbyRoomRateBreackUp["SupplierCurrency"] = DerbySoftCurrency;
                                                        drDerbyRoomRateBreackUp["BuyingPrice"] = DateGross / CurrencyFactorDerbySoft;
                                                        DtHotelRoomRateBreackUp.Rows.Add(drDerbyRoomRateBreackUp);
                                                        DtHotelRoomRateBreackUp.AcceptChanges();
                                                    }
                                                }

                                                #region Policy
                                                if (DerbyResponse.Tables.Contains("Table2") == true)
                                                {
                                                    if (DerbyResponse.Tables["Table2"].Columns.Contains("RateId") && DerbyResponse.Tables["Table2"].Columns.Contains("RoomId")
                                                           && DerbyResponse.Tables["Table2"].Columns.Contains("HotelId")
                                                           )
                                                    {
                                                        DataRow[] DrPolicyList = DerbyResponse.Tables["Table2"].Select("RateId='" + RateId + "' and RoomId='" + RoomId + "' and HotelId='" + HotelId + "'");
                                                        if (DrPolicyList.Length > 0)
                                                        {
                                                            string PolicyCode = "";
                                                            if (DerbyResponse.Tables["Table2"].Columns.Contains("Code"))
                                                            {
                                                                PolicyCode = Convert.ToString(DrPolicyList[0]["Code"]);

                                                                #region DataRaw

                                                                DateTime Date = DateTime.Now.Date;
                                                                DateTime CurentDay = DateTime.Now.Date;
                                                                DateTime todate = DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null);
                                                                double Penalty = 0;
                                                                bool fixedcharge = false;
                                                                bool nonrefendeble = false;
                                                                if (PolicyCode == "" || PolicyCode == null)
                                                                {
                                                                    nonrefendeble = true;
                                                                }
                                                                if (PolicyCode == "AD100P_100P")
                                                                {
                                                                    nonrefendeble = true;
                                                                }
                                                                try
                                                                {



                                                                    if (nonrefendeble == false)
                                                                    {
                                                                        string RoomTypeName = string.Empty;
                                                                        string CanPol = PolicyCode;
                                                                        double amouth = 0;
                                                                        double perce = 0;
                                                                        int Night = 0;
                                                                        int days = 0;
                                                                        string Aday = "";
                                                                        bool pm = Convert.ToBoolean(CanPol.Contains("PM"));
                                                                        bool am = Convert.ToBoolean(CanPol.Contains("AM"));
                                                                        if (am != false || pm != false)
                                                                        {
                                                                            string[] APM = CanPol.Split('M');
                                                                            string[] D = APM[1].Split('D');
                                                                            days = Convert.ToInt32(D[0]);
                                                                            String[] DAY = D[1].Split('_');
                                                                            bool p = Convert.ToBoolean(DAY[0].Contains("P"));
                                                                            bool N = Convert.ToBoolean(DAY[0].Contains("N"));
                                                                            if (p != false)
                                                                            {
                                                                                string[] noofPert = DAY[0].Split('N');
                                                                                perce = Convert.ToInt32(noofPert[0]);
                                                                            }
                                                                            else if (N != false)
                                                                            {
                                                                                string[] noofnight1 = DAY[0].Split('N');
                                                                                Night = Convert.ToInt32(noofnight1[0]);
                                                                            }
                                                                            else
                                                                            {
                                                                                amouth = Convert.ToInt32(DAY[0]);
                                                                            }
                                                                        }
                                                                        else
                                                                        {
                                                                            string day = Convert.ToString(CanPol.Contains("D"));
                                                                            if (day != null && day.ToLower() == "true")
                                                                            {
                                                                                string[] D = CanPol.Split('D');
                                                                                bool day1 = Convert.ToBoolean(D[0].Contains("A"));
                                                                                if (day1 == true)
                                                                                {
                                                                                    Aday = Convert.ToString(D[0]);
                                                                                    string p = Convert.ToString(D[1].Contains("P"));
                                                                                    string N = Convert.ToString(D[1].Contains("N"));
                                                                                    if (p != null && p.ToLower() == "true")
                                                                                    {
                                                                                        perce = Convert.ToInt32(100);
                                                                                    }
                                                                                    else if (N != null && N.ToLower() == "true")
                                                                                    {
                                                                                        Night = Convert.ToInt32(noofnight);
                                                                                    }
                                                                                    else
                                                                                    {
                                                                                        try
                                                                                        {
                                                                                            amouth = Convert.ToInt32(D[1]);
                                                                                        }
                                                                                        catch (Exception)//added on 6-SEP-2024
                                                                                        {
                                                                                            continue;
                                                                                        }

                                                                                    }
                                                                                }
                                                                                else
                                                                                {
                                                                                    days = Convert.ToInt32(D[0]);
                                                                                    string p = Convert.ToString(D[1].Contains("P"));
                                                                                    string N = Convert.ToString(D[1].Contains("N"));
                                                                                    if (p != null && p.ToLower() == "true")
                                                                                    {
                                                                                        perce = Convert.ToInt32(100);
                                                                                    }
                                                                                    else if (N != null && N.ToLower() == "true")
                                                                                    {
                                                                                        Night = Convert.ToInt32(noofnight);
                                                                                    }
                                                                                    else
                                                                                    {
                                                                                        amouth = Convert.ToInt32(D[1]);
                                                                                    }
                                                                                }
                                                                            }
                                                                        }
                                                                        if (perce != 0)
                                                                        {
                                                                            double Percentage = perce;
                                                                            Penalty = (RoomPrice * Percentage) / 100;

                                                                        }
                                                                        else if (amouth != 0)
                                                                        {
                                                                            double Amount = amouth;
                                                                            Penalty = Amount;

                                                                            if (Penalty > 0)
                                                                            {
                                                                                fixedcharge = true;
                                                                            }
                                                                        }
                                                                        else if (Night != 0)
                                                                        {
                                                                            double night = Night;
                                                                            Penalty = (RoomPrice / night);

                                                                        }
                                                                        else
                                                                        {
                                                                            Penalty = (RoomPrice);

                                                                        }

                                                                        DateTime fromdate = DateTime.Now.Date;
                                                                        if (Aday != "A")
                                                                        {
                                                                            int d = Convert.ToInt32(days);
                                                                            fromdate = DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null);
                                                                            fromdate = fromdate.AddDays(-d);
                                                                        }
                                                                        Date = fromdate;

                                                                        DateTime curDate = _commonMethod.GetTimeZoneDate(SearchParam.CurrentDate);
                                                                        uniqid = Guid.NewGuid().ToString();
                                                                        #endregion

                                                                        #region Cancellation Policy Logic and Row
                                                                        DateTime CancelDate = DateTime.Now.Date;
                                                                        if (Penalty == 0)
                                                                            CancelDate = todate;
                                                                        else
                                                                            CancelDate = fromdate;

                                                                        #endregion

                                                                        #region Vat
                                                                        double VatOnPolicy = 0;

                                                                        Penalty = _markup.ProcessHotelCancelationPrice_WithMarkup(ref VatOnPolicy, SearchParam.NoofNights, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), Penalty, isPerce, commonSearch.B2CMarkup, AgentPercentage, AgentMarkupAmount, IsParentId, SubAgentMarkup, ref VatOnPolicy);
                                                                        #endregion

                                                                        DateTime NowDate = DateTime.Now.Date;
                                                                        if (CancelDate.AddDays(CutOffDays).Date > DateTime.Now.Date)
                                                                        {
                                                                            todate = todate.AddDays(CutOffDays).Date;
                                                                            fromdate = fromdate.AddDays(CutOffDays).Date;
                                                                            CancelDate = CancelDate.AddDays(CutOffDays).Date;
                                                                        }
                                                                        else
                                                                        {
                                                                            todate = DateTime.Now;
                                                                            fromdate = DateTime.Now;
                                                                            CancelDate = DateTime.Now;
                                                                        }


                                                                        double zero = 0;
                                                                        if (NowDate.Date < CancelDate.AddDays(-1).Date)
                                                                        {

                                                                            dtDerbyCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref zero, dtDerbyCancellation.NewRow(), CurrencyFactorDerbySoft, DerbySoftCurrency, "DerbySoft", Convert.ToString(OwnsystemHotelId), Convert.ToString(1),
                                                                                                       Convert.ToString(RoomTypeId), RoomName, MealId, DateTime.Now.Date, Convert.ToDateTime(CancelDate.AddDays(-1)), 0, 0, "0", VatOnPolicy, "", "NO", "", "", "", ""));
                                                                            dtDerbyCancellation.AcceptChanges();


                                                                            dtDerbyCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref zero, dtDerbyCancellation.NewRow(), CurrencyFactorDerbySoft, DerbySoftCurrency, "DerbySoft", Convert.ToString(OwnsystemHotelId), Convert.ToString(1),
                                                                                 Convert.ToString(RoomTypeId), RoomName, MealId, Convert.ToDateTime(CancelDate), DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null), Penalty, Penalty, "", VatOnPolicy, "", "NO", "", "", "", ""));
                                                                            dtDerbyCancellation.AcceptChanges();
                                                                        }
                                                                        else
                                                                        {
                                                                            dtDerbyCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref zero, dtDerbyCancellation.NewRow(), CurrencyFactorDerbySoft, DerbySoftCurrency, "DerbySoft", Convert.ToString(OwnsystemHotelId), Convert.ToString(1),
                                                                             Convert.ToString(RoomTypeId), RoomName, MealId, DateTime.Now.Date, DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null), Penalty, Penalty, "", VatOnPolicy, "", "YES", "", "", "", ""));
                                                                            dtDerbyCancellation.AcceptChanges();
                                                                        }
                                                                    }
                                                                    else
                                                                    {
                                                                        double zero = 0;
                                                                        Penalty = (RoomPrice);
                                                                        double VatOnPolicy = 0;

                                                                        Penalty = _markup.ProcessHotelCancelationPrice_WithMarkup(ref VatOnPolicy, SearchParam.NoofNights, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), Penalty, isPerce, commonSearch.B2CMarkup, AgentPercentage, AgentMarkupAmount, IsParentId, SubAgentMarkup, ref VatOnPolicy);

                                                                        dtDerbyCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref zero, dtDerbyCancellation.NewRow(), CurrencyFactorDerbySoft, DerbySoftCurrency, "DerbySoft", Convert.ToString(OwnsystemHotelId), Convert.ToString(1),
                                                                         Convert.ToString(RoomTypeId), RoomName, MealId, DateTime.Now.Date, DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null), Penalty, Penalty, "", VatOnPolicy, "", "YES", "", "", "", ""));
                                                                        dtDerbyCancellation.AcceptChanges();
                                                                    }

                                                                }
                                                                catch (Exception ex)
                                                                {
                                                                    _exception.Error(ex);

                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                                #endregion

                                                #region Room
                                                double RatePerStay = Convert.ToDouble(RoomPrice);
                                                DataRow drDerbyRoom = DtHotelRoom.NewRow();
                                                drDerbyRoom["OwnsystemHotelId"] = OwnsystemHotelId;
                                                drDerbyRoom["ContractID"] = 0;
                                                drDerbyRoom["UniqueNo"] = OwnsystemHotelId;
                                                drDerbyRoom["RoomSequenceNo"] = uniqid;
                                                drDerbyRoom["MealName"] = MealName;
                                                drDerbyRoom["allocationDetails"] = "";
                                                drDerbyRoom["minStay"] = 0;
                                                drDerbyRoom["DateApplyMinStay"] = "";
                                                drDerbyRoom["MealId"] = MealId;
                                                drDerbyRoom["RoomNo"] = Convert.ToString("1");
                                                drDerbyRoom["EssentialInformation"] = RoomId + "-" + RateId + "-" + RoomName;
                                                drDerbyRoom["RoomTypeName"] = RoomName;// RoomName;
                                                drDerbyRoom["RoomTypeId"] = RoomTypeId;
                                                drDerbyRoom["BookingStatus"] = "Available";
                                                drDerbyRoom["AdultsOccupancy"] = SearchParam.Rooms[0].NoofAdults;
                                                drDerbyRoom["ChildOccupancy"] = SearchParam.Rooms[0].NoofChild;
                                                drDerbyRoom["MaxExtrabed"] = 0;
                                                drDerbyRoom["BoardTypeID"] = "";
                                                drDerbyRoom["CompanyBuyingPrice"] = RatePerStay;
                                                drDerbyRoom["OfferDescription"] = "";
                                                drDerbyRoom["offerCode"] = "";

                                                double AgentBuyingPrice = RatePerStay;

                                                #region Vat
                                                Double VatOnTotal = 0;
                                                _markup.ProcessHotelRoom_WithMarkup(CurrencyFactorDerbySoft, DerbySoftCurrency, false, true, SearchParam.NoofNights, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), SearchParam.CheckInDate, SearchParam.CheckOutDate, ref RatePerStay, ref drDerbyRoom, isPerce, AgentPercentage, AgentMarkupAmount, SubAgentMarkup, commonSearch.B2CMarkup, IsParentId);
                                                VatOnTotal = Convert.ToDouble(drDerbyRoom["VatdiffAmount"]);
                                                #endregion

                                                drDerbyRoom["SupplierName"] = "DerbySoft";
                                                drDerbyRoom["IsCombineRoom"] = true;
                                                DtHotelRoom.Rows.Add(drDerbyRoom);
                                                DtHotelRoom.AcceptChanges();
                                                #endregion
                                            }

                                        }
                                    }

                                    if (DtHotelRoom.Rows.Count > 0)
                                    {
                                        Options ops1 = (await _supplierRes.GetCommonRoomForSuppliers(GiataId, commonSearch.tokenData, commonSearch.advancedOptions, Pricenew, drXmlHotel, DtHotelRoom, dtDerbyCancellation, DtHotelRoomRateBreackUp, SearchParam));
                                        if (ops1 != null)
                                            options.Add(ops1);
                                    }
                                    #endregion
                                }
                            }
                            #endregion
                            #endregion

                        }
                        #endregion
                    }
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
                        if (SearchParam != null && ResProccessTimerLogActive == "1" && timerroomx.ElapsedMilliseconds > Convert.ToInt32(ResProccessTimeValueforSaveLog))
                        {
                            timerroomx.Stop();
                            SearchParam.SupplierTime += ",|Finish:DerbySoft Process Response  " + "|Time :" + timerroomx.Elapsed.ToString();
                            _ = Task.Run(() => _commonMethod.FireAway_SQL_ResPorcessTimeLog("", timerroomx.Elapsed.ToString(), "DerbySoft", SearchParam.SearchKey, 1, SearchParam.AgentID));
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

        //recheck

        public async Task<CancellationDto> Get_derbysoft_CancellationPolicy(double RoomPrice, string rating, string ContractID, string Type, DataSet dsHotel, string hotelcode, string citycode, string OwnsystemHotelId, string RoomTypeId, string MealID, string Board_Text, DataTable dtAgentDetail, DataTable dtSuplierMarkup, DataTable dtSearchConfig, HotelSearchParameter SearchParam, string roomno, string provider, int AgentId, DataTable dtMarkupDetails,string SearchKey, string GiataId = "0", int hotelRating = 0,string SiteCurrency="USD")
        {
            CancellationDto cancellationDto = new CancellationDto();
            try
            {
                DataTable dtCancellation = _commonMethod.GenerateCancellationDataTableNewV2();
                int CutOffDays = XMLHotelBookingCommon.CutOffDays;
                CutOffDays = _commonMethod.GetCutOffDays(dtSuplierMarkup, provider);
                string RoomName = "";
                Boolean isPerce = true;
                double AgentCompanyPercentage = Convert.ToDouble(SearchParam.AgentCompanyPercentage);
                bool IsB2C = Convert.ToBoolean(Convert.ToByte(SearchParam.IsB2BOrB2C));
                double B2CMarkup = (IsB2C == false ? AgentCompanyPercentage : XMLHotelBookingCommon.B2CMarkup);
                double AgentPercentage = 0.0;
                double AgentMarkupAmount = 0.0;
                double SubAgentMarkup = 0;
                int IsParentId = 1;
                Boolean blnIsShowSupplier = false;


                string CheckinDate = SearchParam.CheckInDate;
                _markup.getAgetMarkupSetting(provider, GiataId, dtSuplierMarkup, dtMarkupDetails, ref AgentPercentage, ref AgentMarkupAmount, ref blnIsShowSupplier, ref isPerce, AgentId, hotelRating, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), CheckinDate);
                if (dtAgentDetail != null)
                {
                    IsParentId = Convert.ToInt32(dtAgentDetail.Rows[0]["IsParentId"]);
                    SubAgentMarkup = Convert.ToDouble(dtAgentDetail.Rows[0]["B2CMarkupPer"]);
                }

                string[] roomtype = RoomTypeId.Split('$');
                if (roomtype.Length > 0)
                {
                    string RoomId = roomtype[0];
                    string RateId = roomtype[1];
                    string MealId = roomtype[2];
                    string MealName = roomtype[3];
                    string HotelId = roomtype[4];
                    string SupplierId = roomtype[5];
                    string MainPrice = roomtype[6];
                    string RateType = roomtype[7];
                    string RType = roomtype[8];
                    string Token = roomtype[9];
                    string DistributorId = roomtype[10];
                    string Version = roomtype[11];
                    string DerbySoftCurrency = roomtype[12];

                    double CurrencyFactorDerbySoft = 1;
                    if (DerbySoftCurrency != Convert.ToString(SiteCurrency))
                    {
                        DataTable dtInrCurrency = _commonMethod.GetCountryCurrency(DerbySoftCurrency);
                        CurrencyFactorDerbySoft = Convert.ToDouble(dtInrCurrency.Rows[0]["AmtToExch"]);
                    }
                    else
                        CurrencyFactorDerbySoft = 1;
                    DataTable DerbyResponse = await _commonMethod.GetDerbysoftPolicy(HotelId, RoomId, RateId);

                    #region Policy
                    // if (DerbyResponse.Tables.Contains("Table") == true)
                    {

                        string PolicyCode = "";
                        if (DerbyResponse.Columns.Contains("Code"))
                        {
                            PolicyCode = Convert.ToString(DerbyResponse.Rows[0]["Code"]);

                            #region DataRaw

                            DateTime Date = DateTime.Now.Date;
                            DateTime CurentDay = DateTime.Now.Date;
                            DateTime todate = DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null);
                            double Penalty = 0;
                            bool fixedcharge = false;
                            bool nonrefendeble = false;
                            if (PolicyCode == "" || PolicyCode == null)
                            {
                                nonrefendeble = true;
                            }
                            if (PolicyCode == "AD100P_100P")
                            {
                                nonrefendeble = true;
                            }
                            try
                            {
                                if (nonrefendeble == false)
                                {
                                    string RoomTypeName = string.Empty;
                                    string CanPol = PolicyCode;
                                    double amouth = 0;
                                    double perce = 0;
                                    int Night = 0;
                                    int days = 0;
                                    string Aday = "";
                                    bool pm = Convert.ToBoolean(CanPol.Contains("PM"));
                                    bool am = Convert.ToBoolean(CanPol.Contains("AM"));
                                    if (am != false || pm != false)
                                    {
                                        string[] APM = CanPol.Split('M');
                                        string[] D = APM[1].Split('D');
                                        days = Convert.ToInt32(D[0]);
                                        String[] DAY = D[1].Split('_');
                                        bool p = Convert.ToBoolean(DAY[0].Contains("P"));
                                        bool N = Convert.ToBoolean(DAY[0].Contains("N"));
                                        if (p != false)
                                        {
                                            string[] noofPert = DAY[0].Split('N');
                                            perce = Convert.ToInt32(noofPert[0]);
                                        }
                                        else if (N != false)
                                        {
                                            string[] noofnight1 = DAY[0].Split('N');
                                            Night = Convert.ToInt32(noofnight1[0]);
                                        }
                                        else
                                        {
                                            amouth = Convert.ToInt32(DAY[0]);
                                        }
                                    }
                                    else
                                    {
                                        string day = Convert.ToString(CanPol.Contains("D"));
                                        if (day != null && day.ToLower() == "true")
                                        {
                                            string[] D = CanPol.Split('D');
                                            bool day1 = Convert.ToBoolean(D[0].Contains("A"));
                                            if (day1 == true)
                                            {
                                                Aday = Convert.ToString(D[0]);
                                                string p = Convert.ToString(D[1].Contains("P"));
                                                string N = Convert.ToString(D[1].Contains("N"));
                                                if (p != null && p.ToLower() == "true")
                                                {
                                                    perce = Convert.ToInt32(D[1]);
                                                }
                                                else if (N != null && N.ToLower() == "true")
                                                {
                                                    Night = Convert.ToInt32(D[1]);
                                                }
                                                else
                                                {
                                                    amouth = Convert.ToInt32(D[1]);
                                                }
                                            }
                                            else
                                            {
                                                days = Convert.ToInt32(D[0]);
                                                string p = Convert.ToString(D[1].Contains("P"));
                                                string N = Convert.ToString(D[1].Contains("N"));
                                                if (p != null && p.ToLower() == "true")
                                                {
                                                    perce = Convert.ToInt32(D[1]);
                                                }
                                                else if (N != null && N.ToLower() == "true")
                                                {
                                                    Night = Convert.ToInt32(D[1]);
                                                }
                                                else
                                                {
                                                    amouth = Convert.ToInt32(D[1]);
                                                }
                                            }
                                        }
                                    }
                                    if (perce != 0)
                                    {
                                        double Percentage = perce;
                                        Penalty = (RoomPrice * Percentage) / 100;

                                    }
                                    else if (amouth != 0)
                                    {
                                        double Amount = amouth;
                                        Penalty = Amount;

                                        if (Penalty > 0)
                                        {
                                            fixedcharge = true;
                                        }
                                    }
                                    else if (Night != 0)
                                    {
                                        double night = Night;
                                        Penalty = (RoomPrice / night);
                                    }
                                    else
                                    {
                                        Penalty = (RoomPrice);
                                    }

                                    DateTime fromdate = DateTime.Now.Date;
                                    if (Aday != "A")
                                    {
                                        int d = Convert.ToInt32(days);
                                        fromdate = DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null);
                                        fromdate = fromdate.AddDays(-d);
                                    }
                                    Date = fromdate;

                                    DateTime curDate = _commonMethod.GetTimeZoneDate(SearchParam.CurrentDate);
                                    string uniqid = Guid.NewGuid().ToString();
                                    #endregion

                                    #region Cancellation Policy Logic and Row
                                    DateTime CancelDate = DateTime.Now.Date;
                                    if (Penalty == 0)
                                        CancelDate = todate;
                                    else
                                        CancelDate = fromdate;

                                    #endregion

                                    #region Vat
                                    double VatOnPolicy = 0;

                                    Penalty = _markup.ProcessHotelCancelationPrice_WithMarkup(ref VatOnPolicy, SearchParam.NoofNights, SearchParam.CityID, Convert.ToString(SearchParam.CountryID), Penalty, isPerce, B2CMarkup, AgentPercentage, AgentMarkupAmount, IsParentId, SubAgentMarkup, ref VatOnPolicy);
                                    #endregion

                                    DateTime NowDate = DateTime.Now.Date;
                                    if (CancelDate.AddDays(CutOffDays).Date > DateTime.Now.Date)
                                    {
                                        todate = todate.AddDays(CutOffDays).Date;
                                        fromdate = fromdate.AddDays(CutOffDays).Date;
                                        CancelDate = CancelDate.AddDays(CutOffDays).Date;
                                    }
                                    else
                                    {
                                        CancelDate = DateTime.Now.Date;
                                    }


                                    double zero = 0;
                                    if (NowDate.Date < CancelDate.AddDays(-1).Date)
                                    {

                                        dtCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref zero, dtCancellation.NewRow(), CurrencyFactorDerbySoft, DerbySoftCurrency, provider, Convert.ToString(OwnsystemHotelId), Convert.ToString(1),
                                                                   Convert.ToString(RoomTypeId), RoomName, MealId, DateTime.Now.Date, Convert.ToDateTime(CancelDate.AddDays(-1)), 0, 0, "0", VatOnPolicy, "", "NO", "", "", "", ""));
                                        dtCancellation.AcceptChanges();


                                        dtCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref zero, dtCancellation.NewRow(), CurrencyFactorDerbySoft, DerbySoftCurrency, provider, Convert.ToString(OwnsystemHotelId), Convert.ToString(1),
                                             Convert.ToString(RoomTypeId), RoomName, MealId, Convert.ToDateTime(CancelDate), DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null), Penalty, Penalty, "", VatOnPolicy, "", "NO", "", "", "", ""));
                                        dtCancellation.AcceptChanges();
                                    }
                                    else
                                    {
                                        dtCancellation.Rows.Add(_commonMethod.setCancellationPolicyRow(ref zero, dtCancellation.NewRow(), CurrencyFactorDerbySoft, DerbySoftCurrency, provider, Convert.ToString(OwnsystemHotelId), Convert.ToString(1),
                                         Convert.ToString(RoomTypeId), RoomName, MealId, DateTime.Now.Date, DateTime.ParseExact(SearchParam.CheckInDate, "dd/MM/yyyy", null), Penalty, Penalty, "", VatOnPolicy, "", "YES", "", "", "", ""));
                                        dtCancellation.AcceptChanges();
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                _ = _exception.ErrorNew(ex, _LogAPI.Recheck, SearchKey);
                            }
                        }
                    }
                }
                #endregion


                dsHotel.Tables[2].Merge(dtCancellation);

                cancellationDto.dsHotelData = dsHotel;
                cancellationDto.ISBooking = "Available";
                return cancellationDto;

            }
            catch (Exception ex)
            {
                _ =_exception.Error(ex);
                cancellationDto.dsHotelData = dsHotel;
                cancellationDto.ISBooking = "NotAvailable";
                return cancellationDto;
            }
        }


        //prebook
        public async Task<PriceCheckCMDto> DerbySoft_PriceCheckRQRS(DataSet dsCartDetails, DataRow[] DerbySoftDetails, string UserId, string Password, int i, int j, DataTable dtCheckPriceReqStructure, string UniqueNo, string AgentUniqueNo, double FinalPrice, bool isForBooking, double CompanyBuyingPrice, string PreBookingToken, string xmlhotelid, string Searchkey = "", int AgentId = 0)
        {
            PriceCheckCMDto priceCheckCMDto = new PriceCheckCMDto();

            string responsetype = "Success";
            string time = "";
            string SupplierName = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["SupplierName"]).ToLower();
            string Error = "";
            string xslRequest = "";
            string ResponseData = "";
            try
            {
                #region Price Check RQ and Declaration

                double CurrencyFactorDerbySoft = 1;
                string DerbySoftCurrency = Convert.ToString(dsCartDetails.Tables[0].DefaultView[j]["SupplierCurrency"]);
                if (DerbySoftCurrency.ToLower() != "usd")
                {
                    DataTable dtInrCurrency = _commonMethod.GetCountryCurrency(DerbySoftCurrency);
                    CurrencyFactorDerbySoft = Convert.ToDouble(dtInrCurrency.Rows[0]["AmtToExch"]);
                }
                DataSet dsTemp = new DataSet();
                string DerbySoftUrl = Convert.ToString(DerbySoftDetails[0]["WebSiteURL"] + Convert.ToString(DerbySoftInfo.DerbySoftPriceCheck));
                string UserID = Convert.ToString(DerbySoftDetails[0]["UserId"]);
                string Username = Convert.ToString(DerbySoftDetails[0]["UserName"]);
                Password = Convert.ToString(DerbySoftDetails[0]["Password"]);
                string version = Convert.ToString(DerbySoftDetails[0]["Version"]);
                string SearchTimeOut = Convert.ToString(DerbySoftDetails[0]["Password"]);
                string HotelId = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["ContractID"]);
                int UserCartId = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[j]["UserCartID"]);
                int UserCartDetailId = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[j]["UserCartDetailId"]);
                string RoomTypeId = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomTypeID"]);
                string CheckIn = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["CheckInDate"]);
                CheckIn = CheckIn.Substring(0, 4) + "-" + CheckIn.Substring(4, 2) + "-" + CheckIn.Substring(6, 2);
                string CheckOut = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["CheckOutDate"]);
                CheckOut = CheckOut.Substring(0, 4) + "-" + CheckOut.Substring(4, 2) + "-" + CheckOut.Substring(6, 2);
                int NoOfNoghts = Convert.ToInt32(dsCartDetails.Tables[0].Rows[i]["NoOfNight"]);
                string MealId = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["MealPlanCode"]);
                string RoomName = "";
                string TypeSPUI = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["spui"]);
                int RoomNo = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[j]["RoomNumber"]);
                int NoOfRoom = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView.Count);
                xslRequest = Get_DerbySoft_PriceCheckRQ(dsCartDetails, DerbySoftDetails, UserID, Username, Password, HotelId, CheckIn, CheckOut, RoomTypeId, MealId, ref RoomName, TypeSPUI, RoomNo, xmlhotelid, NoOfRoom, version, j);
                _commonMethod.ThirdPartyBookingHistory(Convert.ToString(UniqueNo), Convert.ToString(AgentUniqueNo), xslRequest, "", "Before get price " + SupplierName + " request hit " + j + "is for Price Check" + isForBooking, SupplierName, "response confirm", "-", "WithinEarth");
                #endregion

                if (xslRequest != "")
                {
                    if (!isForBooking)
                    {
                        dtCheckPriceReqStructure.Rows.Add(_commonMethod.AddCheckPriceReq(dtCheckPriceReqStructure.NewRow(), j + 1, UserCartId, UserCartDetailId, RoomTypeId, SupplierName, xslRequest));
                    }
                    #region Price Check RS

                    System.Diagnostics.Stopwatch timerroomprocess = new System.Diagnostics.Stopwatch();
                    timerroomprocess.Start();

                    string DerbySoftAuthToken = Convert.ToString(DerbySoftDetails[0]["Password"]);
                    //ResponseData = GetDerbySoftResponse(ref Error, xslRequest, DerbySoftUrl, 120000, "DerbySoft");
                    ResponseDto responseDto = await GetDerbySoftResponse(Error, xslRequest, DerbySoftUrl, 120000, "DerbySoft", Password);
                    ResponseData = responseDto.Response;
                    Error = responseDto.Error;


                    timerroomprocess.Stop();
                    time = timerroomprocess.Elapsed.ToString();

                    _commonMethod.ThirdPartyBookingHistory(Convert.ToString(UniqueNo), Convert.ToString(AgentUniqueNo), xslRequest, ResponseData, "After get price " + SupplierName + " request hit " + j + "is for Price Check" + isForBooking, SupplierName, "response confirm", "-", "WithinEarth");

                    if (ResponseData == string.Empty || ResponseData.Contains("Error"))
                    {
                        try
                        {
                            responsetype = "GeneralError";
                            string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, xslRequest, ResponseData, Error);
                        }
                        catch (Exception e)
                        {
                            _ = _exception.Error(e, Searchkey);
                        }

                        priceCheckCMDto.Flag = false;
                        return priceCheckCMDto;
                    }
                    DerbySoft.DSPriceCheck DSPriceCheckResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<DerbySoft.DSPriceCheck>(ResponseData);
                    #endregion


                    if (DSPriceCheckResponse.roomRates != null)
                    {
                        if (DSPriceCheckResponse.roomRates.Count == 0)
                        {
                            try
                            {
                                responsetype = "GeneralError";
                                string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, xslRequest, ResponseData, Error);
                            }
                            catch (Exception e)
                            {
                                _ = _exception.Error(e, Searchkey);
                            }
                            priceCheckCMDto.Flag = false;
                            return priceCheckCMDto;
                        }
                        for (int h = 0; h < DSPriceCheckResponse.roomRates.Count; h++)
                        {

                            string[] roomtype = RoomTypeId.Split('$');
                            string roomid1 = roomtype[0];
                            string roomTypeCode1 = roomtype[1];

                            string roomid = DSPriceCheckResponse.roomRates[h].roomId;
                            string roomTypeCode = DSPriceCheckResponse.roomRates[h].rateId;

                            if (roomid == roomid1 && roomTypeCode1 == roomTypeCode)
                            {

                                int NoOfNight = Convert.ToInt32(dsCartDetails.Tables[0].Rows[i]["NoOfNight"]);
                                int RoomNightMultiply = NoOfNight * NoOfRoom;
                                int NoofAdults = 0;
                                int NoofChilds = 0;

                                for (int x = 0; x < NoOfRoom; x++)
                                {
                                    if (x == 0)
                                    {
                                        NoofAdults = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[x]["NoOfAdults"]);
                                        NoofChilds = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[x]["NoOfChild"]);
                                    }
                                    else
                                    {
                                        NoofAdults = NoofAdults + Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[x]["NoOfAdults"]);
                                        NoofChilds = NoofChilds + Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[x]["NoOfChild"]);
                                    }
                                }
                                int TotalPerson = NoofAdults + NoofChilds;
                                double Mainprice = 0;
                                string PriceTh = "";
                                if (DSPriceCheckResponse.roomRates[h].amountBeforeTax != null)
                                {
                                    for (int a = 0; a < DSPriceCheckResponse.roomRates[h].amountBeforeTax.Count; a++)
                                    {

                                        double TotalFeees = 0;
                                        double Price = Convert.ToDouble(DSPriceCheckResponse.roomRates[h].amountBeforeTax[a]);
                                        if (DSPriceCheckResponse.roomRates[h].fees != null)
                                        {
                                            int feecount = DSPriceCheckResponse.roomRates[h].fees.Count;
                                            for (int x = 0; x < feecount; x++)
                                            {
                                                double Fees = 0;
                                                double fee = Convert.ToDouble(DSPriceCheckResponse.roomRates[h].fees[x].fee.amount);
                                                string amounttype = DSPriceCheckResponse.roomRates[h].fees[x].fee.amountType;
                                                string chargetype = DSPriceCheckResponse.roomRates[h].fees[x].fee.chargeType;
                                                int Effectiveperson = 1;
                                                string FeeType = DSPriceCheckResponse.roomRates[h].fees[x].fee.type;
                                                if (DSPriceCheckResponse.roomRates[h].fees[x].fee.effectivePerson != 0)
                                                {
                                                    Effectiveperson = DSPriceCheckResponse.roomRates[h].fees[x].fee.effectivePerson;
                                                }
                                                if (FeeType != "Inclusive")
                                                {
                                                    if (amounttype == "Fix")
                                                    {
                                                        if (chargetype == "PerRoomPerNight")
                                                        {
                                                            Fees = fee * NoOfRoom * 1;// NoOfNight;
                                                            TotalFeees = TotalFeees + Fees;
                                                        }
                                                        else if (chargetype == "PerPersonPerNight")
                                                        {
                                                            Fees = fee * TotalPerson * 1;// NoOfNight;
                                                            TotalFeees = TotalFeees + Fees;
                                                        }
                                                        else if (chargetype == "PerRoomPerStay")
                                                        {
                                                            Fees = fee * NoOfRoom;
                                                            TotalFeees = TotalFeees + Fees;
                                                        }
                                                        else if (chargetype == "PerPersonPerStay")
                                                        {
                                                            Fees = fee * TotalPerson;
                                                            TotalFeees = TotalFeees + Fees;
                                                        }
                                                    }
                                                    else if (amounttype == "Percent")
                                                    {
                                                        double feev = (Price * fee) / 100;
                                                        if (chargetype == "PerRoomPerNight")
                                                        {
                                                            Fees = feev * NoOfRoom * 1;// NoOfNight;
                                                            TotalFeees = TotalFeees + Fees;
                                                        }
                                                        else if (chargetype == "PerPersonPerNight")
                                                        {
                                                            Fees = feev * TotalPerson * 1;// NoOfNight;
                                                            TotalFeees = TotalFeees + Fees;
                                                        }
                                                        else if (chargetype == "PerRoomPerStay")
                                                        {
                                                            Fees = feev * NoOfRoom;
                                                            TotalFeees = TotalFeees + Fees;
                                                        }
                                                        else if (chargetype == "PerPersonPerStay")
                                                        {
                                                            Fees = feev * TotalPerson;
                                                            TotalFeees = TotalFeees + Fees;
                                                        }
                                                    }
                                                }
                                            }
                                        }

                                        if (a == 0)
                                        {
                                            Mainprice = (TotalFeees + Price * NoOfRoom) * CurrencyFactorDerbySoft;
                                            PriceTh = Convert.ToString(DSPriceCheckResponse.roomRates[h].amountBeforeTax[a]);
                                        }
                                        else
                                        {
                                            Mainprice = Mainprice + (TotalFeees + Price * NoOfRoom) * CurrencyFactorDerbySoft;
                                            PriceTh = PriceTh + "-" + Convert.ToString(DSPriceCheckResponse.roomRates[h].amountBeforeTax[a]);
                                        }
                                    }
                                }
                                else
                                {
                                    for (int a = 0; a < DSPriceCheckResponse.roomRates[h].amountAfterTax.Count; a++)
                                    {
                                        double Price = Convert.ToDouble(DSPriceCheckResponse.roomRates[h].amountAfterTax[a]);

                                        if (a == 0)
                                        {
                                            Mainprice = Price * CurrencyFactorDerbySoft * NoOfRoom;
                                            PriceTh = Convert.ToString(DSPriceCheckResponse.roomRates[h].amountBeforeTax[a]);
                                        }
                                        else
                                        {
                                            Mainprice = Mainprice + (Price * CurrencyFactorDerbySoft * NoOfRoom);
                                            PriceTh = PriceTh + "-" + Convert.ToString(DSPriceCheckResponse.roomRates[h].amountBeforeTax[a]);
                                        }
                                    }
                                }
                                FinalPrice = Mainprice;
                                await _commonMethod.UpdateDerbySoftPrice(PriceTh, UserCartId);
                            }
                            else
                            {
                                FinalPrice = CompanyBuyingPrice;
                            }
                        }
                    }
                    else
                    {
                        FinalPrice = CompanyBuyingPrice;
                    }
                }
                else
                {
                    FinalPrice = 0;
                }

                if (Convert.ToDouble(Math.Round(FinalPrice.TruncateDecimalPlaces(0))) > Math.Round(CompanyBuyingPrice.TruncateDecimalPlaces(0)))
                {
                    try
                    {
                        ResponseData = "#Price Changed " + "#Main Price = " + CompanyBuyingPrice + " #New Price = " + FinalPrice + " #Response= " + ResponseData;
                        responsetype = "GeneralError";
                        string abc =await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, xslRequest, ResponseData, Error);
                    }
                    catch (Exception e)
                    {
                        _ = _exception.Error(e, Searchkey);
                    }

                    priceCheckCMDto.ItemPrice = FinalPrice;
                    priceCheckCMDto.Flag = true;
                    return priceCheckCMDto;
                }
                else
                {
                    try
                    {
                        responsetype = "Success";
                        string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, xslRequest, ResponseData, Error);
                    }
                    catch (Exception e)
                    {
                        _ = _exception.Error(e, Searchkey);
                    }
                    priceCheckCMDto.ItemPrice = FinalPrice;
                    priceCheckCMDto.Flag = false;
                    return priceCheckCMDto;
                }
            }
            catch (Exception e)
            {
                try
                {
                    Error = e.Message + e.StackTrace.ToString();
                    responsetype = "GeneralError";
                    string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, xslRequest, ResponseData, Error);
                }
                catch (Exception ex)
                {
                    _ = _exception.Error(ex, Searchkey);
                }
                _ = _exception.Error(e);
            }
            try
            {
                responsetype = "Success";
                string abc = await _commonMethod.SupplierPerformanceReportForPrebookAndBook(SupplierName, "PreBook", AgentId, time, responsetype, xslRequest, ResponseData, Error);
            }
            catch (Exception e)
            {
                _ = _exception.Error(e, Searchkey);
            }

            priceCheckCMDto.ItemPrice = FinalPrice;
            priceCheckCMDto.IsError = false;
            priceCheckCMDto.Flag = false;
            return priceCheckCMDto;
        }

        public string Get_DerbySoft_PriceCheckRQ(DataSet dsCartDetails, DataRow[] DerbySoftDetails, string DerbySoftUserId, string DerbySoftUserName, string DerbySoftPassword, string PropertyId, string CheckIn, string checkout, string RoomTypeId, string MealID, ref string RoomName, string Type, int RoomNo, string xmlhotelid, int NoOfRoom, string version, int j)
        {
            try
            {
                #region "Room Request"

                string[] roomtype = RoomTypeId.Split('$');
                if (roomtype.Length > 0)
                {
                    string RoomId = roomtype[0];
                    string RateId = roomtype[1];
                    string MealId = roomtype[2];
                    string MealName = roomtype[3];
                    string HotelId = roomtype[4];
                    string SupplierId = roomtype[5];
                    string MainPrice = roomtype[6];
                    string RateType = roomtype[7];
                    string RType = roomtype[8];
                    string Token = roomtype[9];
                    string DistributorId = roomtype[10];
                    string Version = roomtype[11];
                    string DerbySoftCurrency = roomtype[12];

                    DerbySoftPriceCheck.Header Header = new DerbySoftPriceCheck.Header();
                    Header.supplierId = SupplierId;
                    Header.distributorId = DistributorId;
                    Header.version = Version;
                    Header.token = Token;

                    DerbySoftPriceCheck.StayRange StayRange = new DerbySoftPriceCheck.StayRange();
                    StayRange.checkin = CheckIn;
                    StayRange.checkout = checkout;

                    int AdultCount = 0;
                    int ChildCount = 0;

                    List<int> childAge = new List<int>();

                    AdultCount = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[0]["NoOfAdults"]);
                    ChildCount = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[0]["NoOfChild"]);
                    switch (Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[0]["NoOfChild"]))
                    {
                        case 1:
                            {
                                childAge.Add(Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[0]["ChildAge1"]));
                                break;
                            }
                        case 2:
                            {
                                childAge.Add(Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[0]["ChildAge1"]));
                                childAge.Add(Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[0]["ChildAge2"]));
                                break;
                            }
                    }
                    //for (int i = 0; i < NoOfRoom; i++)
                    //{
                    //    if (i == 0)
                    //    {
                    //        AdultCount = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[i]["NoOfAdults"]);
                    //        ChildCount = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[i]["NoOfChild"]);
                    //        switch (Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[i]["NoOfChild"]))
                    //        {
                    //            case 1:
                    //                {
                    //                    childAge.Add(Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[i]["ChildAge1"]));
                    //                    break;
                    //                }
                    //            case 2:
                    //                {
                    //                    childAge.Add(Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[i]["ChildAge1"]));
                    //                    childAge.Add(Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[i]["ChildAge2"]));
                    //                    break;
                    //                }
                    //        }
                    //    }
                    //    else
                    //    {
                    //        AdultCount = AdultCount + Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[i]["NoOfAdults"]);
                    //        ChildCount = ChildCount + Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[i]["NoOfChild"]);
                    //        switch (Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[i]["NoOfChild"]))
                    //        {
                    //            case 1:
                    //                {
                    //                    childAge.Add(Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[i]["ChildAge1"]));
                    //                    break;
                    //                }
                    //            case 2:
                    //                {
                    //                    childAge.Add(Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[i]["ChildAge1"]));
                    //                    childAge.Add(Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[i]["ChildAge2"]));
                    //                    break;
                    //                }
                    //        }
                    //    }
                    //}

                    DerbySoftPriceCheck.RoomCriteria RoomCriteria = new DerbySoftPriceCheck.RoomCriteria();
                    RoomCriteria.roomCount = NoOfRoom;
                    RoomCriteria.adultCount = AdultCount;
                    RoomCriteria.childCount = ChildCount;
                    RoomCriteria.childAges = childAge;

                    DerbySoftPriceCheck.ProductCandidate ProductCandidate = new DerbySoftPriceCheck.ProductCandidate();
                    ProductCandidate.roomId = RoomId;
                    ProductCandidate.rateId = RateId;

                    DerbySoftPriceCheck.Extension Extension = new DerbySoftPriceCheck.Extension();
                    Extension.userName = DerbySoftUserName;
                    Extension.password = DerbySoftPassword;

                    DerbySoftPriceCheck.RootObject RootObject = new DerbySoftPriceCheck.RootObject();
                    RootObject.header = Header;
                    RootObject.hotelId = HotelId;
                    RootObject.stayRange = StayRange;
                    RootObject.roomCriteria = RoomCriteria;
                    RootObject.productCandidate = ProductCandidate;
                    RootObject.iata = Convert.ToString(DerbySoftInfo.IATAnumber);
                    RootObject.promoteCode = "";
                    RootObject.extension = Extension;
                    JsonSerializerSettings settings = new JsonSerializerSettings();
                    settings.NullValueHandling = NullValueHandling.Ignore;
                    settings.DefaultValueHandling = DefaultValueHandling.Ignore;
                    string jsonPricecheck = JsonConvert.SerializeObject(RootObject, settings);
                    return jsonPricecheck;
                }
                #endregion
            }
            catch (Exception ex)
            {
                _ = _exception.Error(ex);
            }
            return string.Empty;
        }

        public async Task<ResponseDto> GetDerbySoftResponse(string request, string TravcoURL, Int32 TimeOut, string Provider = "", HotelSearchParameter para = null, bool DebugMode = false, string DerbySoftAuthorization = "")
        {
            ResponseDto responseDto = new ResponseDto();
            string Error = "";

            try
            {
                System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
                timer.Start();
                string response = "";
                HttpWebRequest httpWebRequest = null;
                HttpWebResponse httpWebResponse = null;
                httpWebRequest = (HttpWebRequest)WebRequest.Create(TravcoURL);

                try
                {
                    byte[] bytes;
                    bytes = System.Text.Encoding.UTF8.GetBytes(request);

                    httpWebRequest.Method = "POST";
                    httpWebRequest.Timeout = TimeOut;
                    httpWebRequest.ContentLength = bytes.Length;
                    ServicePointManager.DefaultConnectionLimit = 1000;
                    httpWebRequest.ContentType = "application/x-www-form-urlencoded";
                    if (Provider.ToLower() == "derbysoft")
                    {
                        httpWebRequest.Headers.Add("Authorization", Convert.ToString(DerbySoftInfo.DerbySoftAuthorization));
                        httpWebRequest.Accept = "application/json";
                        httpWebRequest.Method = "POST";
                        httpWebRequest.ContentType = "application/json";
                        httpWebRequest.Headers.Add("Accept-Encoding", "gzip");
                        httpWebRequest.KeepAlive = true;
                        ServicePointManager.Expect100Continue = true;
                        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                    }

                    using (Stream requestStream = httpWebRequest.GetRequestStream())
                    {
                        requestStream.Write(bytes, 0, bytes.Length);
                        requestStream.Close();//Close stream
                    }

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
                            response = await stIn.ReadToEndAsync();
                        }

                        timer.Stop();
                        if (para != null)
                        {
                            para.SupplierTime += ",SupplierName :" + Provider + " : " + "Execution Time :" + timer.Elapsed.ToString();
                            if (DebugMode)
                                _= _commonMethod.XMLRequestResponseTrack(request, response, Provider, "Proper");
                        }
                        if (Convert.ToBoolean(_keySettings.IsLogTrack))
                        {
                            TimeSpan timeTaken = timer.Elapsed;
                            _exception.ErrorLog(Convert.ToString(_keySettings.LogTrackPath), "Trvaco->" + Provider + "-> Execution Time : " + timer.Elapsed.ToString());
                            _= _commonMethod.XMLRequestResponseTrack(request, response, Provider == "" ? "Travco" : Provider, "Proper");
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
                    _= _commonMethod.XMLRequestResponseTrackError(request, result1, Provider == "" ? "Travco" : Provider, "Proper");

                }
                catch (Exception ex)
                {
                    Error = ex.Message.ToString();
                    //response = ex.Message + ex.InnerException;
                    _=_commonMethod.XMLRequestResponseTrack(request, ex.Message, Provider == "" ? "Travco" : Provider, "Proper");
                    //ExceptionManager.LogException(ex);
                    _= _exception.Error(ex);
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
            catch (Exception ex)
            {
                Error = ex.Message.ToString();
                _ = _exception.Error(ex);

                responseDto.Response = "";
                responseDto.Error = Error;
                return responseDto;
            }
        }


        public string DerbySoft_PriBookingRQ(DataSet dsCartDetails, DataTable dtPassengers, DataRow[] DerbySoftDetails, string UniqueNo, bool IsPriceUpdate, int i, int j, string PreBookingToken, string UserName, string Password, string ExternalReference)
        {
            try
            {
                #region room PreBooking RQ


                string RoomTypeId = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomTypeID"]);
                string DerbySoftUserName = Convert.ToString(DerbySoftDetails[0]["UserName"]);
                string DerbySoftPassword = Convert.ToString(DerbySoftDetails[0]["Password"]);
                string version = Convert.ToString(DerbySoftDetails[0]["Version"]);
                string xmlhotelid = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["XMLHotelId"]);
                string CheckIn = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["CheckInDate"]);//"20180327"
                CheckIn = CheckIn.Substring(0, 4) + "-" + CheckIn.Substring(4, 2) + "-" + CheckIn.Substring(6, 2);
                string CheckOut = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["CheckOutDate"]);
                CheckOut = CheckOut.Substring(0, 4) + "-" + CheckOut.Substring(4, 2) + "-" + CheckOut.Substring(6, 2);
                int NoOfNights = Convert.ToInt32(dsCartDetails.Tables[0].Rows[i]["NoOfNight"]);
                int NoOfRoom = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView.Count);
                string NewPriceString = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["EachoToken"]);

                string valcomm = "";
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
                string[] roomtype = RoomTypeId.Split('$');
                if (roomtype.Length > 0)
                {
                    string RoomId = roomtype[0];
                    string RateId = roomtype[1];
                    string MealId = roomtype[2];
                    string MealName = roomtype[3];
                    string HotelId = roomtype[4];
                    string SupplierId = roomtype[5];
                    string MainPrice = roomtype[6];
                    string RateType = roomtype[7];
                    string RType = roomtype[8];
                    string Token = roomtype[9];
                    string DistributorId = roomtype[10];
                    string Version = roomtype[11];
                    string DerbySoftCurrency = roomtype[12];

                    MainPrice = NewPriceString;


                    DerbySoftPreBook.Header Header = new DerbySoftPreBook.Header();
                    Header.supplierId = SupplierId;
                    Header.distributorId = DistributorId;
                    Header.version = Version;
                    Header.token = Token;

                    DerbySoftPreBook.ReservationIds ReservationIds = new DerbySoftPreBook.ReservationIds();
                    ReservationIds.distributorResId = ExternalReference;


                    DerbySoftPreBook.StayRange StayRange = new DerbySoftPreBook.StayRange();
                    StayRange.checkin = CheckIn;
                    StayRange.checkout = CheckOut;

                    DerbySoftPreBook.ContactPerson ContactPerson = new DerbySoftPreBook.ContactPerson();
                    ContactPerson.firstName = Convert.ToString(DerbySoftInfo.ContactPerson_firstName);
                    ContactPerson.lastName = Convert.ToString(DerbySoftInfo.ContactPerson_lastName);
                    ContactPerson.email = Convert.ToString(DerbySoftInfo.ContactPerson_email);
                    ContactPerson.phone = Convert.ToString(DerbySoftInfo.ContactPerson_phone);

                    int AdultCount = 0;
                    int ChildCount = 0;

                    List<int> childAge = new List<int>();
                    AdultCount = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[0]["NoOfAdults"]);
                    ChildCount = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[0]["NoOfChild"]);
                    switch (Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[0]["NoOfChild"]))
                    {
                        case 1:
                            {
                                childAge.Add(Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[0]["ChildAge1"]));
                                break;
                            }
                        case 2:
                            {
                                childAge.Add(Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[0]["ChildAge1"]));
                                childAge.Add(Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[0]["ChildAge2"]));
                                break;
                            }
                    }
                    //for (int Roomid = 0; Roomid < NoOfRoom; Roomid++)
                    //{
                    //    if (Roomid == 0)
                    //    {
                    //        AdultCount = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[Roomid]["NoOfAdults"]);
                    //        ChildCount = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[Roomid]["NoOfChild"]);
                    //        switch (Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[Roomid]["NoOfChild"]))
                    //        {
                    //            case 1:
                    //                {
                    //                    childAge.Add(Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[Roomid]["ChildAge1"]));
                    //                    break;
                    //                }
                    //            case 2:
                    //                {
                    //                    childAge.Add(Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[Roomid]["ChildAge1"]));
                    //                    childAge.Add(Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[Roomid]["ChildAge2"]));
                    //                    break;
                    //                }
                    //        }
                    //    }
                    //    else
                    //    {
                    //        AdultCount = AdultCount + Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[Roomid]["NoOfAdults"]);
                    //        ChildCount = ChildCount + Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[Roomid]["NoOfChild"]);
                    //        switch (Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[i]["NoOfChild"]))
                    //        {
                    //            case 1:
                    //                {
                    //                    childAge.Add(Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[Roomid]["ChildAge1"]));
                    //                    break;
                    //                }
                    //            case 2:
                    //                {
                    //                    childAge.Add(Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[Roomid]["ChildAge1"]));
                    //                    childAge.Add(Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[Roomid]["ChildAge2"]));
                    //                    break;
                    //                }
                    //        }
                    //    }
                    //}

                    DerbySoftPreBook.RoomCriteria RoomCriteria = new DerbySoftPreBook.RoomCriteria();
                    RoomCriteria.roomCount = NoOfRoom;
                    RoomCriteria.adultCount = AdultCount;
                    RoomCriteria.childCount = ChildCount;
                    RoomCriteria.childAges = childAge;


                    DerbySoftPreBook.Total Total = new DerbySoftPreBook.Total();

                    if (RateType.ToLower() == "amountaftertax")
                    {
                        double Price = 0;
                        string[] priceth = MainPrice.Split('-');
                        for (int pi = 0; pi < priceth.GetLength(0); pi++)
                        {
                            Price += Convert.ToDouble(priceth[pi]) * NoOfRoom;
                        }
                        Total.amountAfterTax = Price;
                    }

                    if (RateType.ToLower() == "amountbeforetax")
                    {
                        double Price = 0;
                        string[] priceth = MainPrice.Split('-');
                        for (int pi = 0; pi < priceth.GetLength(0); pi++)
                        {
                            Price += Convert.ToDouble(priceth[pi]) * NoOfRoom;
                        }
                        Total.amountBeforeTax = Price;
                    }
                    List<DerbySoftPreBook.Guest> guests = new List<DerbySoftPreBook.Guest>();
                    if (dtPassengers.DefaultView.Count > 0)
                    {
                        for (int p = 0; p < dtPassengers.DefaultView.Count; p++)
                        {
                            string FName = Convert.ToString(dtPassengers.DefaultView[p]["FirstName"]);
                            string LName = Convert.ToString(dtPassengers.DefaultView[p]["LastName"]);
                            int ChildAgex = Convert.ToInt32(dtPassengers.DefaultView[p]["ChildAge"]);
                            string Prefix = Convert.ToString(dtPassengers.DefaultView[p]["Prefix"]).Replace(".", "");
                            string PaxType = Convert.ToString(dtPassengers.DefaultView[p]["PaxType"]);

                            DerbySoftPreBook.Guest Guest = new DerbySoftPreBook.Guest();
                            Guest.firstName = FName;
                            Guest.lastName = LName;
                            if (PaxType.ToLower() == "child")
                            {
                                Guest.type = "Child";
                                Guest.age = ChildAgex;
                            }
                            else
                            {
                                Guest.type = "Adult";
                            }
                            guests.Add(Guest);
                        }
                    }
                    DerbySoftPreBook.DateRange DateRange = new DerbySoftPreBook.DateRange();
                    StayRange.checkin = CheckIn;
                    StayRange.checkout = CheckOut;

                    List<DerbySoftPreBook.RoomRate> roomRates = new List<DerbySoftPreBook.RoomRate>();
                    for (int k = 0; k < 1; k++)
                    {
                        DerbySoftPreBook.RoomRate RoomRate = new DerbySoftPreBook.RoomRate();
                        RoomRate.roomId = RoomId;
                        RoomRate.rateId = RateId;
                        RoomRate.currency = DerbySoftCurrency;
                        List<double> amountBeforeTax = new List<double>();
                        if (RateType.ToLower() == "amountbeforetax")
                        {
                            string[] priceth = MainPrice.Split('-');
                            for (int pi = 0; pi < priceth.GetLength(0); pi++)
                            {
                                double Price = Convert.ToDouble(priceth[pi]);// * NoOfRoom;
                                amountBeforeTax.Add(Price);
                                RoomRate.amountBeforeTax = amountBeforeTax;
                            }
                        }

                        List<double> amountAfterTax = new List<double>();
                        if (RateType.ToLower() == "amountaftertax")
                        {
                            string[] priceth = MainPrice.Split('-');
                            for (int pi = 0; pi < priceth.GetLength(0); pi++)
                            {
                                double Price = Convert.ToDouble(priceth[pi]);// * NoOfRoom;
                                amountAfterTax.Add(Price);
                                RoomRate.amountAfterTax = amountAfterTax;
                            }
                        }

                        RoomRate.mealPlan = MealId;
                        roomRates.Add(RoomRate);
                    }
                    DerbySoftPreBook.Extension Extension = new DerbySoftPreBook.Extension();
                    Extension.userName = DerbySoftUserName;
                    Extension.password = DerbySoftPassword;

                    DerbySoftPreBook.RootObject RootObject = new DerbySoftPreBook.RootObject();
                    RootObject.header = Header;

                    List<string> comments = new List<string>();
                    comments.Add(valcomm);

                    RootObject.reservationIds = ReservationIds;
                    RootObject.iata = Convert.ToString(DerbySoftInfo.IATAnumber);
                    RootObject.hotelId = HotelId;
                    RootObject.stayRange = StayRange;
                    RootObject.contactPerson = ContactPerson;
                    RootObject.roomCriteria = RoomCriteria;
                    RootObject.total = Total;
                    RootObject.promoteCode = "";
                    RootObject.guests = guests;
                    RootObject.comments = comments;
                    RootObject.roomRates = roomRates;
                    RootObject.extension = Extension;

                    JsonSerializerSettings settings = new JsonSerializerSettings();
                    settings.NullValueHandling = NullValueHandling.Ignore;
                    settings.DefaultValueHandling = DefaultValueHandling.Ignore;
                    string jsonPrebook = JsonConvert.SerializeObject(RootObject, settings);
                    return jsonPrebook;
                }
                #endregion
            }
            catch (Exception ex)
            {
                _ =_exception.Error(ex);
            }
            return string.Empty;
        }

        public async Task<ResponseDto> GetDerbySoftResponse(string Error, string request, string TravcoURL, Int32 TimeOut, string Provider = "", string Password = "", HotelSearchParameter para = null, bool DebugMode = false)
        {
            ResponseDto responseDto = new ResponseDto();
            try
            {
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
                    httpWebRequest.Timeout = TimeOut;
                    httpWebRequest.ContentLength = bytes.Length;
                    ServicePointManager.DefaultConnectionLimit = 1000;
                    httpWebRequest.ContentType = "application/x-www-form-urlencoded";
                    if (Provider.ToLower() == "derbysoft")
                    {
                        //httpWebRequest.Headers.Add("Authorization", Convert.ToString(ConfigurationManager.AppSettings["DerbySoftAuthorization"]));
                        httpWebRequest.Headers.Add("Authorization", Password);
                        httpWebRequest.Accept = "application/json";
                        httpWebRequest.Method = "POST";
                        httpWebRequest.ContentType = "application/json";
                        httpWebRequest.Headers.Add("Accept-Encoding", "gzip");
                        httpWebRequest.KeepAlive = true;
                        ServicePointManager.Expect100Continue = true;
                        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                    }

                    using (Stream requestStream = httpWebRequest.GetRequestStream())
                    {
                        requestStream.Write(bytes, 0, bytes.Length);
                        requestStream.Close();//Close stream
                    }

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
                    _ = _exception.Error(we);
                }
                catch (Exception ex)
                {
                    Error = ex.Message.ToString();
                    //response = ex.Message + ex.InnerException;
                    _ = _commonMethod.XMLRequestResponseTrack(request, ex.Message, Provider == "" ? "Travco" : Provider, "Proper");
                    //ExceptionManager.LogException(ex);
                    _ = _exception.Error(ex);
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
            catch (Exception ex)
            {
                Error = ex.Message.ToString();
                responseDto.Error = Error;
                responseDto.Response = "";
                _ = _exception.Error(ex);
                return responseDto;
            }
        }

        public string Get_DerbySoft_BookingRequest(DataSet dsCartDetails, DataTable dtPassengers, DataRow[] DerbySoftDetails, string UniqueNo, bool IsPriceUpdate, int i, int j, string PreBookingToken, string UserID, string Password, string ExternalReference, string Bookingtokan, bool IsPruvo)
        {
            try
            {
                #region room Booking RQ

                string RoomTypeId = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["RoomTypeID"]);
                string DerbySoftUserName = Convert.ToString(DerbySoftDetails[0]["UserName"]);
                string DerbySoftPassword = Convert.ToString(DerbySoftDetails[0]["Password"]);
                string version = Convert.ToString(DerbySoftDetails[0]["Version"]);
                string xmlhotelid = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["XMLHotelId"]);
                string CheckIn = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["CheckInDate"]);//"20180327"
                CheckIn = CheckIn.Substring(0, 4) + "-" + CheckIn.Substring(4, 2) + "-" + CheckIn.Substring(6, 2);
                string CheckOut = Convert.ToString(dsCartDetails.Tables[0].Rows[i]["CheckOutDate"]);
                CheckOut = CheckOut.Substring(0, 4) + "-" + CheckOut.Substring(4, 2) + "-" + CheckOut.Substring(6, 2);
                int NoOfNights = Convert.ToInt32(dsCartDetails.Tables[0].Rows[i]["NoOfNight"]);
                int NoOfRoom = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView.Count);
                string NewPriceString = Convert.ToString(dsCartDetails.Tables[1].DefaultView[j]["EachoToken"]);
                string valcomm = "";
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
                string[] roomtype = RoomTypeId.Split('$');
                if (roomtype.Length > 0)
                {
                    string RoomId = roomtype[0];
                    string RateId = roomtype[1];
                    string MealId = roomtype[2];
                    string MealName = roomtype[3];
                    string HotelId = roomtype[4];
                    string SupplierId = roomtype[5];
                    string MainPrice = roomtype[6];
                    string RateType = roomtype[7];
                    string RType = roomtype[8];
                    string Token = roomtype[9];
                    string DistributorId = roomtype[10];
                    string Version = roomtype[11];
                    string DerbySoftCurrency = roomtype[12];
                    MainPrice = NewPriceString;

                    DerbySoftBooking.Header Header = new DerbySoftBooking.Header();
                    Header.supplierId = SupplierId;
                    Header.distributorId = DistributorId;
                    Header.version = Version;
                    Header.token = Token;
                    DerbySoftBooking.ReservationIds ReservationIds = new DerbySoftBooking.ReservationIds();
                    ReservationIds.distributorResId = ExternalReference;
                    DerbySoftBooking.StayRange StayRange = new DerbySoftBooking.StayRange();
                    StayRange.checkin = CheckIn;
                    StayRange.checkout = CheckOut;
                    DerbySoftBooking.ContactPerson ContactPerson = new DerbySoftBooking.ContactPerson();
                    ContactPerson.firstName = Convert.ToString(DerbySoftInfo.ContactPerson_firstName);
                    ContactPerson.lastName = Convert.ToString(DerbySoftInfo.ContactPerson_lastName);
                    ContactPerson.email = Convert.ToString(DerbySoftInfo.ContactPerson_email);
                    ContactPerson.phone = Convert.ToString(DerbySoftInfo.ContactPerson_phone);
                    List<int> childAges = new List<int>();
                    int AdultCount = 0;
                    int ChildCount = 0;

                    AdultCount = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[0]["NoOfAdults"]);
                    ChildCount = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[0]["NoOfChild"]);
                    switch (Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[0]["NoOfChild"]))
                    {
                        case 1:
                            {
                                childAges.Add(Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[0]["ChildAge1"]));
                                break;
                            }
                        case 2:
                            {
                                childAges.Add(Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[0]["ChildAge1"]));
                                childAges.Add(Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[0]["ChildAge2"]));
                                break;
                            }
                    }
                    //for (int Roomid = 0; Roomid < NoOfRoom; Roomid++)
                    //{
                    //    if (Roomid == 0)
                    //    {
                    //        AdultCount = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[Roomid]["NoOfAdults"]);
                    //        ChildCount = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[Roomid]["NoOfChild"]);
                    //        switch (Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[Roomid]["NoOfChild"]))
                    //        {
                    //            case 1:
                    //                {
                    //                    childAges.Add(Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[Roomid]["ChildAge1"]));
                    //                    break;
                    //                }
                    //            case 2:
                    //                {
                    //                    childAges.Add(Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[Roomid]["ChildAge1"]));
                    //                    childAges.Add(Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[Roomid]["ChildAge2"]));
                    //                    break;
                    //                }
                    //        }
                    //    }
                    //    else
                    //    {
                    //        AdultCount = AdultCount + Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[Roomid]["NoOfAdults"]);
                    //        ChildCount = ChildCount + Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[Roomid]["NoOfChild"]);
                    //        switch (Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[i]["NoOfChild"]))
                    //        {
                    //            case 1:
                    //                {
                    //                    childAges.Add(Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[Roomid]["ChildAge1"]));
                    //                    break;
                    //                }
                    //            case 2:
                    //                {
                    //                    childAges.Add(Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[Roomid]["ChildAge1"]));
                    //                    childAges.Add(Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[Roomid]["ChildAge2"]));
                    //                    break;
                    //                }
                    //        }
                    //    }
                    //}

                    DerbySoftBooking.RoomCriteria RoomCriteria = new DerbySoftBooking.RoomCriteria();
                    RoomCriteria.roomCount = NoOfRoom;
                    RoomCriteria.adultCount = AdultCount;
                    RoomCriteria.childCount = ChildCount;
                    RoomCriteria.childAges = childAges;

                    DerbySoftBooking.Total Total = new DerbySoftBooking.Total();

                    if (RateType.ToLower() == "amountaftertax")
                    {
                        double Price = 0;
                        string[] priceth = MainPrice.Split('-');
                        for (int pi = 0; pi < priceth.GetLength(0); pi++)
                        {
                            Price += Convert.ToDouble(priceth[pi]) * NoOfRoom;
                        }
                        Total.amountAfterTax = Price;
                    }

                    if (RateType.ToLower() == "amountbeforetax")
                    {
                        double Price = 0;
                        string[] priceth = MainPrice.Split('-');
                        for (int pi = 0; pi < priceth.GetLength(0); pi++)
                        {
                            Price += Convert.ToDouble(priceth[pi]) * NoOfRoom;
                        }
                        Total.amountBeforeTax = Price;
                    }






                    #region APISO VC

                    int usercartdetailsid = Convert.ToInt32(dsCartDetails.Tables[1].DefaultView[j]["UserCartDetailId"]);
                    //get bookingid and detailsid

                    DataTable dtbookingdata = _commonMethod.GetApisoBookingData(usercartdetailsid);


                    string Pan = "";
                    string CVV = "0", Expiry = "0";

                    if (dtbookingdata != null)
                    {
                        _vcn.GetApisoServiceDatatoCreateVCN("", Convert.ToInt32(dtbookingdata.Rows[0]["OnlineBookingMasterId"]), Convert.ToInt32(dtbookingdata.Rows[0]["OnlineBookingDetailsId"]), ref Pan, ref CVV, ref Expiry, IsPruvo);
                        //PANN = Pan;
                    }
                    #endregion

                    string ExpiryRegex = Convert.ToString(Expiry).Substring(2, 2);
                    string ExpiryRegexFisrt = Convert.ToString(Expiry).Substring(0, 2);


                    DerbySoftBooking.Payment Payment = new DerbySoftBooking.Payment();
                    Payment.cardCode = Convert.ToString(DerbySoftInfo.cardCode);
                    Payment.cardNumber = Convert.ToString(Pan); //Convert.ToString(ConfigurationManager.AppSettings["cardNumber"]);
                    Payment.cardHolderName = Convert.ToString(DerbySoftInfo.cardHolderName);
                    Payment.expireDate = Convert.ToString(ExpiryRegex + ExpiryRegexFisrt); //Convert.ToString(ConfigurationManager.AppSettings["expireDate"]);

                    List<DerbySoftBooking.Guest> Guests = new List<DerbySoftBooking.Guest>();
                    if (dtPassengers.DefaultView.Count > 0)
                    {
                        for (int p = 0; p < dtPassengers.DefaultView.Count; p++)
                        {
                            string FName = Convert.ToString(dtPassengers.DefaultView[p]["FirstName"]);
                            string LName = Convert.ToString(dtPassengers.DefaultView[p]["LastName"]);
                            int ChildAgex = Convert.ToInt32(dtPassengers.DefaultView[p]["ChildAge"]);
                            string Prefix = Convert.ToString(dtPassengers.DefaultView[p]["Prefix"]).Replace(".", "");
                            string PaxType = Convert.ToString(dtPassengers.DefaultView[p]["PaxType"]);

                            DerbySoftBooking.Guest Guest = new DerbySoftBooking.Guest();
                            Guest.firstName = FName;
                            Guest.lastName = LName;
                            Guests.Add(Guest);
                        }
                    }

                    List<DerbySoftBooking.RoomRate> RoomRates = new List<DerbySoftBooking.RoomRate>();
                    for (int k = 0; k < 1; k++)
                    {
                        DerbySoftBooking.RoomRate RoomRate = new DerbySoftBooking.RoomRate();
                        RoomRate.roomId = RoomId;
                        RoomRate.rateId = RateId;
                        RoomRate.currency = DerbySoftCurrency;

                        List<double> amountBeforeTax = new List<double>();
                        if (RateType.ToLower() == "amountbeforetax")
                        {
                            string[] priceth = MainPrice.Split('-');
                            for (int pi = 0; pi < priceth.GetLength(0); pi++)
                            {
                                double Price = Convert.ToDouble(priceth[pi]);// * NoOfRoom;
                                amountBeforeTax.Add(Price);
                                RoomRate.amountBeforeTax = amountBeforeTax;
                            }
                        }
                        List<double> amountAfterTax = new List<double>();
                        if (RateType.ToLower() == "amountaftertax")
                        {
                            string[] priceth = MainPrice.Split('-');
                            for (int pi = 0; pi < priceth.GetLength(0); pi++)
                            {
                                double Price = Convert.ToDouble(priceth[pi]);// * NoOfRoom;
                                amountAfterTax.Add(Price);
                                RoomRate.amountAfterTax = amountAfterTax;
                            }
                        }

                        RoomRate.mealPlan = MealId;
                        RoomRates.Add(RoomRate);
                    }
                    DerbySoftBooking.Extension Extension = new DerbySoftBooking.Extension();
                    Extension.userName = DerbySoftUserName;
                    Extension.password = DerbySoftPassword;

                    List<string> comments = new List<string>();
                    comments.Add(valcomm);

                    DerbySoftBooking.RootObject RootObject = new DerbySoftBooking.RootObject();
                    RootObject.header = Header;
                    RootObject.reservationIds = ReservationIds;
                    RootObject.iata = Convert.ToString(DerbySoftInfo.IATAnumber);
                    RootObject.hotelId = HotelId;
                    RootObject.stayRange = StayRange;
                    RootObject.contactPerson = ContactPerson;
                    RootObject.roomCriteria = RoomCriteria;
                    RootObject.total = Total;
                    RootObject.payment = Payment;
                    RootObject.guests = Guests;
                    RootObject.comments = comments;
                    RootObject.roomRates = RoomRates;
                    RootObject.bookingToken = Bookingtokan;
                    RootObject.extension = Extension;

                    JsonSerializerSettings settings = new JsonSerializerSettings();
                    settings.NullValueHandling = NullValueHandling.Ignore;
                    settings.DefaultValueHandling = DefaultValueHandling.Ignore;
                    string jsonBooking = JsonConvert.SerializeObject(RootObject, settings);
                    return jsonBooking;

                }

                #endregion
            }
            catch (Exception ex)
            {
                _ =_exception.Error(ex);
            }
            return string.Empty;
        }

        public string Get_DerbySoft_BookingDetailsRQ(string AgentVoucherNo, DataRow[] SupplierDetails, DataSet dsCartDetails)
        {
            try
            {
                #region Booking Cancel RQ
                string RoomTypeId = Convert.ToString(dsCartDetails.Tables[1].DefaultView[0]["RoomTypeID"]);

                string jsonBookingDetails = "";
                string[] roomtype = RoomTypeId.Split('$');
                if (roomtype.Length > 0)
                {
                    string RoomId = roomtype[0];
                    string RateId = roomtype[1];
                    string MealId = roomtype[2];
                    string MealName = roomtype[3];
                    string HotelId = roomtype[4];
                    string SupplierId = roomtype[5];
                    string MainPrice = roomtype[6];
                    string RateType = roomtype[7];
                    string RType = roomtype[8];
                    string Token = roomtype[9];
                    string DistributorId = roomtype[10];
                    string Version = roomtype[11];
                    string DerbySoftCurrency = roomtype[12];

                    string[] AgentVoucherNoVal = AgentVoucherNo.Split('$');
                    if (AgentVoucherNoVal.Length > 0)
                    {
                        string DerbyResId = AgentVoucherNoVal[0];
                        string SupplierID = AgentVoucherNoVal[1];
                        string distributorResId = AgentVoucherNoVal[2];

                        DerbySoftBookingDetail.Header Header = new DerbySoftBookingDetail.Header();
                        Header.supplierId = SupplierId;
                        Header.distributorId = DistributorId;
                        Header.version = Version;
                        Header.token = Token;

                        DerbySoftBookingDetail.ReservationIds ReservationIds = new DerbySoftBookingDetail.ReservationIds();
                        ReservationIds.distributorResId = distributorResId;
                        ReservationIds.derbyResId = DerbyResId;
                        ReservationIds.supplierResId = SupplierID;

                        DerbySoftBookingDetail.RootObject RootObject = new DerbySoftBookingDetail.RootObject();
                        RootObject.header = Header;
                        RootObject.reservationIds = ReservationIds;
                        jsonBookingDetails = JsonConvert.SerializeObject(RootObject);


                    }
                }
                return jsonBookingDetails;
                #endregion
            }
            catch (Exception ex)
            {
                _ =_exception.Error(ex);
                return "";
            }
        }

        public string Get_DerbySoft_BookingCancelRQ(string AgentVoucherNo, DataRow[] SupplierDetails, string RoomTypeId)
        {
            try
            {
                string CancellationRQ = "";
                string[] roomtype = RoomTypeId.Split('$');
                if (roomtype.Length > 0)
                {
                    string RoomId = roomtype[0];
                    string RateId = roomtype[1];
                    string MealId = roomtype[2];
                    string MealName = roomtype[3];
                    string HotelId = roomtype[4];
                    string SupplierId = roomtype[5];
                    string MainPrice = roomtype[6];
                    string RateType = roomtype[7];
                    string RType = roomtype[8];
                    string Token = roomtype[9];
                    string DistributorId = roomtype[10];
                    string Version = roomtype[11];
                    string DerbySoftCurrency = roomtype[12];
                    #region Booking Cancel RQ
                    string UserID = Convert.ToString(SupplierDetails[0]["UserId"]);
                    string UserName = Convert.ToString(SupplierDetails[0]["UserName"]);
                    string Password = Convert.ToString(SupplierDetails[0]["Password"]);
                    string version = Convert.ToString(SupplierDetails[0]["version"]);

                    string[] AgentVoucherNoVal = AgentVoucherNo.Split('$');
                    if (AgentVoucherNoVal.Length > 0)
                    {
                        string DerbyResId = AgentVoucherNoVal[0];
                        string SupplierID = AgentVoucherNoVal[1];
                        string distributorResId = AgentVoucherNoVal[2];

                        DerbySoftCancelBooking.Header Header = new DerbySoftCancelBooking.Header();
                        Header.supplierId = SupplierId;
                        Header.distributorId = DistributorId;
                        Header.version = Version;
                        Header.token = Token;

                        DerbySoftCancelBooking.ReservationIds ReservationIds = new DerbySoftCancelBooking.ReservationIds();
                        ReservationIds.distributorResId = distributorResId;
                        ReservationIds.derbyResId = DerbyResId;
                        ReservationIds.supplierResId = SupplierID;

                        DerbySoftCancelBooking.Extension Extension = new DerbySoftCancelBooking.Extension();
                        Extension.userName = UserName;
                        Extension.password = Password;

                        DerbySoftCancelBooking.RootObject RootObject = new DerbySoftCancelBooking.RootObject();
                        RootObject.header = Header;
                        RootObject.reservationIds = ReservationIds;
                        RootObject.extension = Extension;

                        CancellationRQ = JsonConvert.SerializeObject(RootObject);
                    }
                }
                return CancellationRQ;
                #endregion
            }
            catch (Exception ex)
            {
                _ =_exception.ErrorNew(ex, _LogAPI.CancelBooking);
                return "";
            }

        }

        public string Get_DerbySoft_BookingDetailsRQ_AFTC(string AgentVoucherNo, DataRow[] SupplierDetails, string RoomTypeId)
        {
            try
            {
                #region Booking Cancel RQ
                string jsonBookingDetails = "";
                string[] roomtype = RoomTypeId.Split('$');
                if (roomtype.Length > 0)
                {
                    string RoomId = roomtype[0];
                    string RateId = roomtype[1];
                    string MealId = roomtype[2];
                    string MealName = roomtype[3];
                    string HotelId = roomtype[4];
                    string SupplierId = roomtype[5];
                    string MainPrice = roomtype[6];
                    string RateType = roomtype[7];
                    string RType = roomtype[8];
                    string Token = roomtype[9];
                    string DistributorId = roomtype[10];
                    string Version = roomtype[11];
                    string DerbySoftCurrency = roomtype[12];
                    string[] AgentVoucherNoVal = AgentVoucherNo.Split('$');
                    if (AgentVoucherNoVal.Length > 0)
                    {
                        string DerbyResId = AgentVoucherNoVal[0];
                        string SupplierID = AgentVoucherNoVal[1];
                        string distributorResId = AgentVoucherNoVal[2];

                        DerbySoftBookingDetail.Header Header = new DerbySoftBookingDetail.Header();
                        Header.supplierId = SupplierId;
                        Header.distributorId = DistributorId;
                        Header.version = Version;
                        Header.token = Token;

                        DerbySoftBookingDetail.ReservationIds ReservationIds = new DerbySoftBookingDetail.ReservationIds();
                        ReservationIds.distributorResId = distributorResId;
                        ReservationIds.derbyResId = DerbyResId;
                        ReservationIds.supplierResId = SupplierID;

                        DerbySoftBookingDetail.RootObject RootObject = new DerbySoftBookingDetail.RootObject();
                        RootObject.header = Header;
                        RootObject.reservationIds = ReservationIds;
                        jsonBookingDetails = JsonConvert.SerializeObject(RootObject);

                        #endregion
                    }
                }
                return jsonBookingDetails;
            }
            catch (Exception ex)
            {
                _ = _exception.ErrorNew(ex, _LogAPI.CancelBooking);

                return "";
            }
        }
    }
}
