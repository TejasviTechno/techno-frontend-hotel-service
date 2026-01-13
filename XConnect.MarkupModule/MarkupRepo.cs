using System.Data;
using System.Globalization;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using XConnect.DAL.Interface;
using XConnect.ErrorLogHandler.Interface;
using XConnect.Markup.Interface;
using XConnect.Models;
using XConnect.Models.Hotel;


namespace XConnect.Markup
{
    public class MarkupRepo : IMarkup
    {
        private readonly IException _exception;
        private readonly KeySettings _keySettings;
        private readonly IDLConnection _dLConnection;


        public MarkupRepo(IException exception, IOptions<KeySettings> keySettings, IDLConnection dLConnection)
        {
            _exception = exception;
            _keySettings = keySettings.Value;
            _dLConnection = dLConnection;
        }

        public async Task<DataTable> GetCommonMarkup(int CountryId, int CityID, int MarkupCategaryID, int AgentID = 0)
        {
            try
            {
                SqlParameter[] para = new SqlParameter[]{
        new SqlParameter("@CityID",CityID),
        new SqlParameter("@CountryId",CountryId),
        new SqlParameter("@TariffID",MarkupCategaryID),
          new SqlParameter("@AgentID",AgentID)
        };

                DataTable dt = new DataTable();
                dt = await _dLConnection.GetDataTableAsync("GetMarkUpNew", para);
                return dt;
            }
            catch (Exception ex)
            {
                await _exception.Error(ex);
                return null;
            }
        }

        public void ProcessHotelRoom_WithMarkup_BookingDotComCommision(double Factor, string Currency, bool IsPolicyDynamic, bool IsCombineRoom, int Multi, int City, string Country, string checkin, string checkout, ref double RatePerStay, ref DataRow drHotelRoom, bool isPerce, double AgentPercentage, double AgentMarkupAmount, double SubAgentMarkup, double B2CMarkup, int IsParentId, double BeforeCommisionRoomRateForAgent)
        {
            try
            {
                DataTable dtvatlist = new DataTable();
                if (drHotelRoom.Table.Columns.Contains("BuyingPrice"))
                    drHotelRoom["BuyingPrice"] = BeforeCommisionRoomRateForAgent / Factor; // RatePerStay / Factor; 
                if (drHotelRoom.Table.Columns.Contains("SupplierCurrency"))
                    drHotelRoom["SupplierCurrency"] = Currency.ToUpper();

                // if (objSearPera != null)
                // dtvatlist = Common.GetVatCountrylist(Convert.ToString(objSearPera.CountryId) == "" ? 0 : Convert.ToInt32(objSearPera.CountryId), Convert.ToInt32(objSearPera.CityID), "hotel");
                drHotelRoom["CompanyBuyingPrice"] = BeforeCommisionRoomRateForAgent;// RatePerStay;

                double b2cItemPriceMarkup = 0;
                if (isPerce)
                {
                    b2cItemPriceMarkup = Convert.ToDouble(RatePerStay) * AgentPercentage / 100;
                }
                else
                    b2cItemPriceMarkup = AgentMarkupAmount * Multi;

                RatePerStay = RatePerStay + b2cItemPriceMarkup;

                drHotelRoom["CompanySellingPrice"] = RatePerStay;
                double VatSellingPric = RatePerStay;

                if (dtvatlist.Rows.Count > 0)
                {
                    drHotelRoom["CompanySellingPrice"] = Convert.ToDouble(drHotelRoom["CompanySellingPrice"]) + (Convert.ToDouble(GetcalVatAmount(Convert.ToDecimal(drHotelRoom["CompanySellingPrice"]) - Convert.ToDecimal(drHotelRoom["CompanyBuyingPrice"]), Convert.ToDecimal(dtvatlist.Rows[0]["IntRate"]))));
                    RatePerStay = Convert.ToDouble(drHotelRoom["CompanySellingPrice"]);
                }
                drHotelRoom["VatdiffAmount"] = Convert.ToDecimal(drHotelRoom["CompanySellingPrice"]) - Convert.ToDecimal(VatSellingPric);
                double AgentBuyingPrice = RatePerStay;
                if (1 == 1)
                {
                    if (IsParentId == 0)
                    {
                        drHotelRoom["AgentBuyingPrice"] = RatePerStay;
                        double subb2bMarkup = (RatePerStay * SubAgentMarkup) / 100;
                        RatePerStay = RatePerStay + subb2bMarkup;
                        drHotelRoom["AgentSellingPrice"] = RatePerStay;
                    }

                    if (IsParentId == 1)
                        drHotelRoom["AgentBuyingPrice"] = RatePerStay;
                    else
                        drHotelRoom["SubAgentBuyingPice"] = RatePerStay;

                    double b2bMarkup = (RatePerStay * B2CMarkup) / 100;
                    RatePerStay = RatePerStay + b2bMarkup;
                    if (IsParentId == 1)
                    {
                        drHotelRoom["AgentSellingPrice"] = RatePerStay;
                    }
                    else
                        drHotelRoom["SubAgentSellingPrice"] = RatePerStay;
                }

                drHotelRoom["FinalSellingPrice"] = RatePerStay;
                if (drHotelRoom.Table.Columns.Contains("IsCombineRoom"))
                    drHotelRoom["IsCombineRoom"] = IsCombineRoom;
                if (drHotelRoom.Table.Columns.Contains("IsPolicyDynamic"))
                    drHotelRoom["IsPolicyDynamic"] = IsPolicyDynamic;
                if (drHotelRoom.Table.Columns.Contains("CheckInDate"))
                {
                    drHotelRoom["CheckInDate"] = checkin;
                    drHotelRoom["CheckOutDate"] = checkout;
                    drHotelRoom["CityId"] = City;
                }
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
            }

        }

        public Double ProcessHotelCancelationPrice_WithMarkup(ref double MainAgentChage, int Multiply, int City, string Country, double CancellationAmount, bool isPerce, double B2CMarkup, double AgentPercentage, double AgentMarkupAmount, int IsParentId, double SubAgentMarkup, ref double TotalVat)
        {
            DataTable dtvatlist = new DataTable();
            // if (objSearPera != null)
            //  dtvatlist = Common.GetVatCountrylist(Convert.ToString(objSearPera.CountryID.ToString()) == "" ? 0 : Convert.ToInt32(objSearPera.CountryID.ToString()), Convert.ToInt32(objSearPera.CityID), "hotel");
            double ChargeAmount = Convert.ToDouble(CancellationAmount);
            double VatChargeAmount = ChargeAmount;
            Double CancellationMarkup = 0.0;
            if (isPerce)
            {
                if ((B2CMarkup == 0 || (AgentPercentage > 0)))
                    CancellationMarkup = (ChargeAmount * AgentPercentage) / 100;
            }
            else
                CancellationMarkup = (ChargeAmount == 0 ? ChargeAmount : (AgentMarkupAmount * Multiply));

            ChargeAmount = ChargeAmount + CancellationMarkup;
            TotalVat = ChargeAmount;
            if (dtvatlist.Rows.Count > 0)
            {
                ChargeAmount = ChargeAmount + (Convert.ToDouble(GetcalVatAmount(Convert.ToDecimal(ChargeAmount) - Convert.ToDecimal(VatChargeAmount), Convert.ToDecimal(dtvatlist.Rows[0]["IntRate"]))));
            }
            TotalVat = ChargeAmount - TotalVat;
            MainAgentChage = ChargeAmount;
            if (IsParentId == 0)
            {
                Double SubCancellationMarkup = (ChargeAmount * SubAgentMarkup) / 100;
                ChargeAmount = ChargeAmount + SubCancellationMarkup;
            }
            Double CancellationAgentMarkup = 0;
            ChargeAmount = ChargeAmount + CancellationAgentMarkup;
            return ChargeAmount;
        }

        public void ProcessHotelRoom_WithMarkup(double Factor, string Currency, bool IsPolicyDynamic, bool IsCombineRoom, int Multi, int City, string Country, string checkin, string checkout, ref double RatePerStay, ref DataRow drHotelRoom, bool isPerce, double AgentPercentage, double AgentMarkupAmount, double SubAgentMarkup, double B2CMarkup, int IsParentId)
        {
            try
            {
                DataTable dtvatlist = new DataTable();
                if (drHotelRoom.Table.Columns.Contains("BuyingPrice"))
                    drHotelRoom["BuyingPrice"] = RatePerStay / Factor;
                if (drHotelRoom.Table.Columns.Contains("SupplierCurrency"))
                    drHotelRoom["SupplierCurrency"] = Currency.ToUpper();

                // if (objSearPera != null)
                // dtvatlist = Common.GetVatCountrylist(Convert.ToString(Convert.ToString(objSearPera.CountryID)) == "" ? 0 : Convert.ToInt32(Convert.ToString(objSearPera.CountryID)), Convert.ToInt32(objSearPera.CityID), "hotel");
                drHotelRoom["CompanyBuyingPrice"] = RatePerStay;

                double b2cItemPriceMarkup = 0;
                if (isPerce)
                {
                    b2cItemPriceMarkup = Convert.ToDouble(RatePerStay) * AgentPercentage / 100;
                }
                else
                    b2cItemPriceMarkup = AgentMarkupAmount * Multi;

                RatePerStay = RatePerStay + b2cItemPriceMarkup;

                drHotelRoom["CompanySellingPrice"] = RatePerStay;
                double VatSellingPric = RatePerStay;

                if (dtvatlist.Rows.Count > 0)
                {
                    drHotelRoom["CompanySellingPrice"] = Convert.ToDouble(drHotelRoom["CompanySellingPrice"]) + (Convert.ToDouble(GetcalVatAmount(Convert.ToDecimal(drHotelRoom["CompanySellingPrice"]) - Convert.ToDecimal(drHotelRoom["CompanyBuyingPrice"]), Convert.ToDecimal(dtvatlist.Rows[0]["IntRate"]))));
                    RatePerStay = Convert.ToDouble(drHotelRoom["CompanySellingPrice"]);
                }
                drHotelRoom["VatdiffAmount"] = Convert.ToDecimal(drHotelRoom["CompanySellingPrice"]) - Convert.ToDecimal(VatSellingPric);
                double AgentBuyingPrice = RatePerStay;
                if (1 == 1)
                {
                    if (IsParentId == 0)
                    {
                        drHotelRoom["AgentBuyingPrice"] = RatePerStay;
                        double subb2bMarkup = (RatePerStay * SubAgentMarkup) / 100;
                        RatePerStay = RatePerStay + subb2bMarkup;
                        drHotelRoom["AgentSellingPrice"] = RatePerStay;
                    }

                    if (IsParentId == 1)
                        drHotelRoom["AgentBuyingPrice"] = RatePerStay;
                    else
                        drHotelRoom["SubAgentBuyingPice"] = RatePerStay;

                    double b2bMarkup = (RatePerStay * B2CMarkup) / 100;
                    RatePerStay = RatePerStay + b2bMarkup;
                    if (IsParentId == 1)
                    {
                        drHotelRoom["AgentSellingPrice"] = RatePerStay;
                    }
                    else
                        drHotelRoom["SubAgentSellingPrice"] = RatePerStay;
                }

                drHotelRoom["FinalSellingPrice"] = RatePerStay;
                if (drHotelRoom.Table.Columns.Contains("IsCombineRoom"))
                    drHotelRoom["IsCombineRoom"] = IsCombineRoom;
                if (drHotelRoom.Table.Columns.Contains("IsPolicyDynamic"))
                    drHotelRoom["IsPolicyDynamic"] = IsPolicyDynamic;
                if (drHotelRoom.Table.Columns.Contains("CheckInDate"))
                {
                    drHotelRoom["CheckInDate"] = checkin;
                    drHotelRoom["CheckOutDate"] = checkout;
                    drHotelRoom["CityId"] = City;
                }
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
            }

        }

        public double ProcessHotelRateBreakupPrice_WithMarkup(int Multi, int City, string CountryId, double Price, double B2CMarkup, int IsParentId, bool isPerce, double AgentPercentage, double AgentMarkupAmount, double SubAgentMarkup, ref double RateBrackUp)
        {
            DataTable dtvatlist = new DataTable();
            // if (objSearPera != null)
            // dtvatlist = Common.GetVatCountrylist(Convert.ToString(CountryId) == "" ? 0 : Convert.ToInt32(CountryId), Convert.ToInt32(objSearPera.CityID), "hotel");
            double DateGross = Price;

            if (isPerce)
            {
                double b2bDateGrossMarkup = (DateGross * AgentPercentage) / 100;
                DateGross = DateGross + b2bDateGrossMarkup;
            }
            else
                DateGross = (DateGross == 0 ? DateGross : DateGross + (AgentMarkupAmount * Convert.ToInt32(Multi)));
            RateBrackUp = DateGross;
            if (dtvatlist.Rows.Count > 0)
            {
                DateGross = DateGross + (Convert.ToDouble(GetcalVatAmount(Convert.ToDecimal(DateGross) - Convert.ToDecimal(Price), Convert.ToDecimal(dtvatlist.Rows[0]["IntRate"]))));
            }
            RateBrackUp = DateGross - RateBrackUp;

            double b2cDateGrossMarkup = (DateGross * B2CMarkup) / 100;
            DateGross = DateGross + b2cDateGrossMarkup;


            if (IsParentId == 0)
            {
                double b2bSubDateGrossMarkup = (DateGross * SubAgentMarkup) / 100;
                DateGross = DateGross + b2bSubDateGrossMarkup;
            }

            return DateGross;
        }

        public void GetHotelDataCommon(ref double LowestRate, ref bool willAddOrNot, ref long OwnsystemHotelId, ref long RaynaId, ref long GiataId, ref int Rating, ref DataRow dr, int AgentId, int TariffID, DataSet dsAgentDetails, DataSet dsGiatadata, HotelSearchParameter SearchPara, DataTable dtCountryCityMarkup, double B2CMarkup, int IsParentId, double SubAgentMarkup, bool SearchByHotel, List<long> HotelIDS)
        {
            string Sup = "";
            try
            {
                string Supplier = Convert.ToString(dr["SupplierName"]).ToLower();
                Sup = Supplier;
                string SupplierCondition = Supplier;
                bool isExpedia = false;
                if (Supplier.IndexOf("expedia") >= 0)
                {
                    SupplierCondition = "xpedia";
                    isExpedia = true;
                }
                if (Supplier.IndexOf("ean_") >= 0)
                {
                    SupplierCondition = "ean";
                    isExpedia = true;
                }

                #region "Markup Logic Supplierwise"
                double AgentPercentage = 0.0;
                Boolean blnIsShowSupplier = false;
                int SupplierID = 0;
                int MasterSupplierID = 0;
                if (dsAgentDetails.Tables[1] != null)
                {
                    DataRow[] drSupRow = null;
                    foreach (DataRow row in dsAgentDetails.Tables[1].Rows)
                    {
                        if (Convert.ToString(row["SupplierName"]).ToLower().Replace(" ", "") == (Supplier == "oth" ? Convert.ToString(_keySettings.IsWithinearth) == "1" ? "withinearth" : "oth" : Supplier))
                        {
                            drSupRow = new DataRow[1];
                            drSupRow[0] = row;
                            break;
                        }
                    }
                    if (drSupRow != null)
                    {
                        AgentPercentage = Convert.ToDouble(drSupRow[0]["AgentWiseMarkup"]);
                        blnIsShowSupplier = (Convert.ToInt16(drSupRow[0]["IsShowSupplier"]) == 1 ? true : false);
                        SupplierID = Convert.ToInt32(drSupRow[0]["SupplierID"]);
                        MasterSupplierID = Convert.ToInt32(drSupRow[0]["MasterSupplierId"]);
                    }
                    dr["SupplierId"] = SupplierID;
                    dr["MasterSupplierId"] = MasterSupplierID;

                }
                #endregion
                if (Supplier.ToLower() != "oth" && Supplier.ToLower() != "hotelrunner" && Supplier.ToLower() != "dedge" && Supplier.ToLower() != "ratetiger" && Supplier.ToLower() != "travelclick" && Supplier.ToLower() != "rategain" && Supplier.ToLower() != "siteminder" && Supplier.ToLower() != "staah")
                {
                    //Using main supplier mapping
                    //if (Supplier.ToLower() == "fitruums_b2c")
                    //    Supplier = "fitruums";
                    if (Supplier.ToLower() == "hotelsproapi")
                        Supplier = "hotelspro";
                    //if (Supplier.ToLower() == "hotelbedsv1_b2c")
                    //    Supplier = "hotelbedsv1";
                    if (Supplier.ToLower() == "agodanew")
                        Supplier = "agoda";
                    if (Supplier.ToLower() == "getaroom_api")
                        Supplier = "getaroom";
                    double AgentMarkupAmount = 0.0;
                    Boolean isPerce = true;
                    string XMLCityCode = Convert.ToString(dr["XmlCity"]);
                    string XMLHotelCode = Convert.ToString(dr["XMLHotelCode"]);
                    string Star = Convert.ToString(dr["Rating"]);
                    //if (IsB2C == true)
                    //	AgentPercentage = 0;
                    // List<GiataDetail> giataDetail = new List<GiataDetail>();
                    //  List<MappingDetail> mappingDetail = new List<MappingDetail>();
                    if (dsGiatadata.Tables["MappingDetails"].Columns.Contains("XmlId"))
                    {
                        DataRow[] dtdetails = dsGiatadata.Tables["MappingDetails"].Select("XmlId='" + XMLHotelCode + "' and SupplierName='" + (isExpedia ? SupplierCondition : Supplier) + "' ");
                        if (Supplier.ToLower() == "gta")
                            dtdetails = dsGiatadata.Tables["MappingDetails"].Select("XmlId='" + XMLHotelCode + "' and SupplierName='" + Convert.ToString(dr["SupplierName"]) + "' and XMLBASEID='" + XMLCityCode + "' ");
                        //if (Supplier.ToLower() == "xtg")
                        //    dtdetails = dsGiatadata.Tables["MappingDetails"].Select("XmlId='" + XMLHotelCode + "' and SupplierName='" + Convert.ToString("Fitruums") + "' ");
                        if (dtdetails.Length > 0)
                        {
                            RaynaId = Convert.ToInt32(dtdetails[0]["RaynaId"]);
                            if (SearchByHotel)
                            {
                                if (HotelIDS.Find(u => u == Convert.ToInt32(dtdetails[0]["RaynaId"])) > 0)
                                { }
                                else { willAddOrNot = false; };
                            }
                            DataRow[] drGiataDetails = dsGiatadata.Tables["GiataDetails"].Select("RaynaId='" + RaynaId + "'");
                            if (drGiataDetails.Length > 0)
                            {
                                #region "Get Deatils From Giata data"


                                dr["GIITACode"] = Convert.ToString(drGiataDetails[0]["GiataId"]);

                                dr["IsShowSupplier"] = blnIsShowSupplier;
                                dr["RaynaId"] = Convert.ToString(RaynaId);

                                //Rating = Convert.ToInt32(Convert.ToString(dtdetails[0]["Rating"]) == "" ? "0" : dtdetails[0]["Rating"]);
                                Rating = Convert.ToInt32(Convert.ToString(drGiataDetails[0]["Rating"]) == "" ? "0" : drGiataDetails[0]["Rating"]);  // Update on 02-DEC-2025 

                                //Get Third party data from db
                                //if (Supplier.ToLower() == "darina" || Supplier.ToLower() == "whitesands" || Supplier.ToLower() == "roomsxml" || Supplier.ToLower() == "miki" || Supplier.ToLower() == "agoda" || Supplier.ToLower() == "aot" || Supplier.ToLower() == "uk_travco" || Supplier.ToLower() == "hotelspro" || Supplier.ToLower() == "jactravel")
                                //{
                                //    int star = Convert.ToInt32(Rating == "" ? "0" : dtdetails[0]["Rating"]);
                                //    if (star == 0 || Convert.ToInt16(star) < Convert.ToInt16(SearchPara.StarStart) || Convert.ToInt16(star) > Convert.ToInt16(SearchPara.StarEnd))
                                //    {
                                //        willAddOrNot = false;
                                //    }
                                //}
                                //if (Supplier.ToLower() == "darina" || Supplier.ToLower() == "dotw" || Supplier.ToLower().IndexOf("_xtg") >= 0 || Supplier.ToLower() == "agoda" || Supplier.ToLower() == "roomsxml" || Supplier.ToLower() == "miki" || Supplier.ToLower() == "quantum" || Supplier.ToLower() == "aot" || Supplier.ToLower() == "uk_travco" || Supplier.ToLower() == "hotelspro" || Supplier.ToLower() == "jactravel")
                                //    dr["Rating"] = Rating;
                                //if (Supplier.ToLower() == "lotsofhotels" || Supplier.ToLower() == "w2m" || Supplier.ToLower() == "gte" || Supplier.ToLower() == "egyptexpress" || Supplier.ToLower() == "alphatours")
                                dr["Rating"] = Rating;
                                //dr["HotelName"] = Convert.ToString(dtdetails[0]["HotelName"]);
                                dr["HotelName"] = Convert.ToString(drGiataDetails[0]["HotelName"]); // Update on 02-DEC-2025 

                                dr["ReservationEmail"] = Convert.ToString(drGiataDetails[0]["ReservationEmail"]);
                                dr["HotelContactNo"] = Convert.ToString(drGiataDetails[0]["HotelContactNo"]);

                                OwnsystemHotelId = Convert.ToInt64(dtdetails[0]["OwnsystemHotelId"]);
                                //--------
                                dr["OwnsystemHotelId"] = Convert.ToString(dtdetails[0]["OwnsystemHotelId"]); ;

                                dr["Favorite"] = 0;
                                #endregion
                                LowestRate = Convert.ToDouble(dr["FinalSellingPrice"]);
                                GiataId = Convert.ToInt64(drGiataDetails[0]["GiataId"]);
                                #region "HOtelWise markup "
                                if (dtCountryCityMarkup != null && dtCountryCityMarkup.Rows.Count > 0)
                                {
                                    DateTime CurDate = DateTime.Now.Date;
                                    DateTime CheckinDate = DateTime.Now;
                                    try
                                    {
                                        CheckinDate = DateTime.ParseExact(SearchPara.CheckInDate, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                                        //CheckinDate = DateTime.Parse(CheckinDate1);
                                    }
                                    catch (Exception)
                                    {
                                        CheckinDate = DateTime.Now;
                                    }

                                    DataRow[] drHotelMarkup = null;
                                    #region "New Markup Logic"
                                    {
                                        drHotelMarkup = dtCountryCityMarkup.Select("CategoryWiseMarkup='Search Date Wish Markup' and AgentID='" + AgentId.ToString() + "' and fromdate <='" + CurDate + "' and todate >= '" + CurDate + "' and SupplierId='" + SupplierID + "'" + " and TariffSheetId='" + TariffID.ToString() + "'");

                                        if (drHotelMarkup.GetLength(0) == 0 || Convert.ToDouble(drHotelMarkup[0]["MarkupAmount"]) <= 0)
                                            drHotelMarkup = dtCountryCityMarkup.Select("CategoryWiseMarkup='Search Date Wish Markup' and AgentID='0' and fromdate <='" + CurDate + "' and todate >='" + CurDate + "' and SupplierId='" + SupplierID + "'" + " and TariffSheetId='" + TariffID.ToString() + "'");

                                        if (drHotelMarkup.GetLength(0) == 0 || Convert.ToDouble(drHotelMarkup[0]["MarkupAmount"]) <= 0)
                                            drHotelMarkup = dtCountryCityMarkup.Select("CategoryWiseMarkup='Check in Date Wish Markup' and AgentID='" + AgentId.ToString() + "' and fromdate <='" + CheckinDate + "' and todate >= '" + CheckinDate + "' and SupplierId='" + SupplierID + "'" + " and TariffSheetId='" + TariffID.ToString() + "'");

                                        if (drHotelMarkup.GetLength(0) == 0 || Convert.ToDouble(drHotelMarkup[0]["MarkupAmount"]) <= 0)
                                            drHotelMarkup = dtCountryCityMarkup.Select("CategoryWiseMarkup='Check in Date Wish Markup' and AgentID='0' and fromdate <='" + CheckinDate + "' and todate >='" + CheckinDate + "' and SupplierId='" + SupplierID + "'" + " and TariffSheetId='" + TariffID.ToString() + "'");
                                        //<1>[Supplier hotelname-Tariff markup]
                                        if (drHotelMarkup.GetLength(0) == 0 || Convert.ToDouble(drHotelMarkup[0]["MarkupAmount"]) <= 0)
                                            drHotelMarkup = dtCountryCityMarkup.Select("CategoryWiseMarkup='Hotel'and RaynaID='" + GiataId + "' and SupplierId='" + SupplierID + "'and TariffSheetId='" + TariffID.ToString() + "'");
                                        //<2>[Supplier hotelname markup]
                                        if (drHotelMarkup.GetLength(0) == 0 || Convert.ToDouble(drHotelMarkup[0]["MarkupAmount"]) <= 0)
                                            drHotelMarkup = dtCountryCityMarkup.Select("CategoryWiseMarkup='Hotel'and RaynaID='" + GiataId + "' and SupplierId='" + SupplierID + "'and TariffSheetId='0'");
                                        //<3>[Hotelname-Tariff markup]
                                        if (drHotelMarkup.GetLength(0) == 0 || Convert.ToDouble(drHotelMarkup[0]["MarkupAmount"]) <= 0)
                                            drHotelMarkup = dtCountryCityMarkup.Select("CategoryWiseMarkup='Hotel'and RaynaID='" + GiataId + "' and SupplierId='0'" + "and TariffSheetId='" + TariffID.ToString() + "'");
                                        //<4>[Hotelname markup]
                                        if (drHotelMarkup.GetLength(0) == 0 || Convert.ToDouble(drHotelMarkup[0]["MarkupAmount"]) <= 0)
                                            drHotelMarkup = dtCountryCityMarkup.Select("CategoryWiseMarkup='Hotel'and RaynaID='" + GiataId + "' and SupplierId='0'" + "and TariffSheetId='0'");
                                        //<5>[Star-Tarrif Markup]
                                        if (drHotelMarkup.GetLength(0) == 0 || Convert.ToDouble(drHotelMarkup[0]["MarkupAmount"]) <= 0)
                                            drHotelMarkup = dtCountryCityMarkup.Select("CategoryWiseMarkup='Star'and star='" + Rating + "' and CityID='" + SearchPara.CityID.ToString() + "' and SupplierID='" + SupplierID.ToString() + "' and TariffSheetId='" + TariffID.ToString() + "'");
                                        //<6>[Star markup]
                                        if (drHotelMarkup.GetLength(0) == 0 || Convert.ToDouble(drHotelMarkup[0]["MarkupAmount"]) <= 0)
                                            drHotelMarkup = dtCountryCityMarkup.Select("CategoryWiseMarkup='Star'and star='" + Rating + "' and CityID='" + SearchPara.CityID.ToString() + "' and SupplierID='" + SupplierID.ToString() + "' and TariffSheetId='0'");
                                        //<7>[City-Tarrif Markup]
                                        if (drHotelMarkup.GetLength(0) == 0 || Convert.ToDouble(drHotelMarkup[0]["MarkupAmount"]) <= 0)
                                            drHotelMarkup = dtCountryCityMarkup.Select("CategoryWiseMarkup='City'and CityID='" + SearchPara.CityID.ToString() + "' and SupplierID='" + SupplierID.ToString() + "' and TariffSheetId='" + TariffID.ToString() + "'");
                                        //<8>[City Markup]
                                        if (drHotelMarkup.GetLength(0) == 0 || Convert.ToDouble(drHotelMarkup[0]["MarkupAmount"]) <= 0)
                                            drHotelMarkup = dtCountryCityMarkup.Select("CategoryWiseMarkup='City'and CityID='" + SearchPara.CityID.ToString() + "' and SupplierID='" + SupplierID.ToString() + "' and TariffSheetId='0'");
                                        //<9>[Country-Tarrif Markup]
                                        if (drHotelMarkup.GetLength(0) == 0 || Convert.ToDouble(drHotelMarkup[0]["MarkupAmount"]) <= 0)
                                            drHotelMarkup = dtCountryCityMarkup.Select("CategoryWiseMarkup='Country'and CountryID='" + SearchPara.CountryID.ToString() + "' and SupplierID='" + SupplierID.ToString() + "' and TariffSheetId='" + TariffID.ToString() + "'");
                                        //<10>[Country Markup]
                                        if (drHotelMarkup.GetLength(0) == 0 || Convert.ToDouble(drHotelMarkup[0]["MarkupAmount"]) <= 0)
                                            drHotelMarkup = dtCountryCityMarkup.Select("CategoryWiseMarkup='Country'and CountryID='" + SearchPara.CountryID.ToString() + "' and SupplierID='" + SupplierID.ToString() + "' and TariffSheetId='0'");
                                        //<11>[Agent Markup]
                                        if (drHotelMarkup.GetLength(0) == 0 || Convert.ToDouble(drHotelMarkup[0]["MarkupAmount"]) <= 0)
                                            drHotelMarkup = dtCountryCityMarkup.Select("CategoryWiseMarkup='Agent'and SupplierID='" + SupplierID + "' and AgentID='" + AgentId.ToString() + "'");
                                        //<12>[Tariff Markup]
                                        if (drHotelMarkup.GetLength(0) == 0 || Convert.ToDouble(drHotelMarkup[0]["MarkupAmount"]) <= 0)
                                            drHotelMarkup = dtCountryCityMarkup.Select("CategoryWiseMarkup='Tariff'and TariffSheetId='" + TariffID.ToString() + "' and SupplierID='" + SupplierID.ToString() + "'");
                                        //<13>[Global Markup]
                                        if (drHotelMarkup.GetLength(0) == 0 || Convert.ToDouble(drHotelMarkup[0]["MarkupAmount"]) <= 0)
                                            drHotelMarkup = dtCountryCityMarkup.Select("CategoryWiseMarkup='Supplier'and SupplierID='" + SupplierID.ToString() + "'");
                                    }
                                    //else
                                    //drHotelMarkup = dtCountryCityMarkup.Select("RaynaID='" + OwnsystemHotelId + "'and SupplierID='" + SupplierID + "' ");
                                    #endregion
                                    if (drHotelMarkup.GetLength(0) > 0)
                                    {
                                        if (Convert.ToBoolean(drHotelMarkup[0]["IsPercentage"]) && Convert.ToDouble(drHotelMarkup[0]["MarkupAmount"]) > 0)
                                        {
                                            AgentPercentage = Convert.ToDouble(drHotelMarkup[0]["MarkupAmount"]);
                                            isPerce = true;
                                        }
                                        else if (!Convert.ToBoolean(drHotelMarkup[0]["IsPercentage"]) && Convert.ToDouble(drHotelMarkup[0]["MarkupAmount"]) > 0)
                                        {
                                            isPerce = false;
                                            AgentMarkupAmount = Convert.ToDouble(drHotelMarkup[0]["MarkupAmount"]);
                                        }
                                    }
                                }
                                #endregion
                                double VatLowestRate = 0;
                                int multi = 1;
                                //New For Total Price Markup Logic
                                if (Supplier.ToLower() == "uk_travco" || Supplier.ToLower() == "dotw" || Supplier.ToLower() == "gta" || Supplier.ToLower() == "hotelbedsV1" || Supplier.ToLower() == "w2m" || Supplier.ToLower() == "alphatours" || Supplier.ToLower() == "egyptexpress" || Supplier.ToLower() == "gte" || Supplier.ToLower() == "methabook_xtg" || Supplier.ToLower() == "welcomebeds_xtg" || Supplier.ToLower() == "fitruums_xtg" || Supplier.ToLower() == "whl_xtg" || Supplier.ToLower() == "rezlive_XTG" || Supplier.ToLower() == "teamamerica_xtg" || Supplier.ToLower() == "tourico_xtg" || Supplier.ToLower() == "bookohotel_xtg" || Supplier.ToLower() == "smyrooms_xtg" || Supplier.ToLower() == "rts_xtg" || Supplier.ToLower() == "restel_xtg" || Supplier.ToLower() == "rategain_xtg" || Supplier.ToLower() == "bonotel_xtg" || Supplier.ToLower() == "actours_xtg" || Supplier.ToLower() == "italcamel_xtg" || Supplier.ToLower() == "darina" || Supplier.ToLower() == "hotelspro" || Supplier.ToLower() == "travellanda" || Supplier.ToLower() == "jactravel" || Supplier.ToLower() == "miki")
                                {
                                    int NoOfRooms = SearchPara.Rooms.Count();
                                    int NoOfNight = SearchPara.NoofNights;
                                    AgentMarkupAmount = (AgentMarkupAmount * NoOfRooms) * NoOfNight;
                                }
                                //end
                                LowestRate = ProcessHotelRateBreakupPrice_WithMarkup(1, SearchPara.CityID, Convert.ToString(SearchPara.CountryID), LowestRate / multi, B2CMarkup, IsParentId, isPerce, AgentPercentage, AgentMarkupAmount, SubAgentMarkup, ref VatLowestRate);
                                dr["IsRecomondedHotel"] = Convert.ToBoolean(drGiataDetails[0]["IsRecomondedHotel"]);
                                // dr["VatdiffAmount"] = VatLowestRate;
                                //  dr["SellingPrice"] = LowestRate;
                                dr["FinalSellingPrice"] = LowestRate;
                            }
                        }
                        else
                        {
                            dr["SupplierId"] = Convert.ToString(SupplierID);
                            willAddOrNot = false;
                            //dr.Delete();
                        }
                    }
                    else
                        willAddOrNot = false;

                }
                else
                {
                    #region "HOtelWise markup "
                    double AgentMarkupAmount = 0.0;
                    double VatLowestRate = 0;
                    Boolean isPerce = true;
                    if (dtCountryCityMarkup != null && dtCountryCityMarkup.Rows.Count > 0)
                    {
                        DateTime CurDate = DateTime.Now.Date;
                        DateTime CheckinDate = DateTime.Now;
                        try
                        {
                            CheckinDate = DateTime.ParseExact(SearchPara.CheckInDate, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                            //CheckinDate = DateTime.Parse(CheckinDate1);
                        }
                        catch (Exception)
                        {
                            CheckinDate = DateTime.Now;
                        }

                        DataRow[] drHotelMarkup = null;
                        #region "New Markup Logic"
                        {
                            drHotelMarkup = dtCountryCityMarkup.Select("CategoryWiseMarkup='Search Date Wish Markup' and AgentID='" + AgentId.ToString() + "' and fromdate <='" + CurDate + "' and todate >= '" + CurDate + "' and SupplierId='" + SupplierID + "'" + " and TariffSheetId='" + TariffID.ToString() + "'");

                            if (drHotelMarkup.GetLength(0) == 0 || Convert.ToDouble(drHotelMarkup[0]["MarkupAmount"]) <= 0)
                                drHotelMarkup = dtCountryCityMarkup.Select("CategoryWiseMarkup='Search Date Wish Markup' and AgentID='0' and fromdate <='" + CurDate + "' and todate >='" + CurDate + "' and SupplierId='" + SupplierID + "'" + " and TariffSheetId='" + TariffID.ToString() + "'");

                            if (drHotelMarkup.GetLength(0) == 0 || Convert.ToDouble(drHotelMarkup[0]["MarkupAmount"]) <= 0)
                                drHotelMarkup = dtCountryCityMarkup.Select("CategoryWiseMarkup='Check in Date Wish Markup' and AgentID='" + AgentId.ToString() + "' and fromdate <='" + CheckinDate + "' and todate >= '" + CheckinDate + "' and SupplierId='" + SupplierID + "'" + " and TariffSheetId='" + TariffID.ToString() + "'");

                            if (drHotelMarkup.GetLength(0) == 0 || Convert.ToDouble(drHotelMarkup[0]["MarkupAmount"]) <= 0)
                                drHotelMarkup = dtCountryCityMarkup.Select("CategoryWiseMarkup='Check in Date Wish Markup' and AgentID='0' and fromdate <='" + CheckinDate + "' and todate >='" + CheckinDate + "' and SupplierId='" + SupplierID + "'" + " and TariffSheetId='" + TariffID.ToString() + "'");
                            //<1>[Supplier hotelname-Tariff markup]
                            if (drHotelMarkup.GetLength(0) == 0 || Convert.ToDouble(drHotelMarkup[0]["MarkupAmount"]) <= 0)
                                drHotelMarkup = dtCountryCityMarkup.Select("CategoryWiseMarkup='Hotel'and RaynaID='" + GiataId + "' and SupplierId='" + SupplierID + "'and TariffSheetId='" + TariffID.ToString() + "'");
                            //<2>[Supplier hotelname markup]
                            if (drHotelMarkup.GetLength(0) == 0 || Convert.ToDouble(drHotelMarkup[0]["MarkupAmount"]) <= 0)
                                drHotelMarkup = dtCountryCityMarkup.Select("CategoryWiseMarkup='Hotel'and RaynaID='" + GiataId + "' and SupplierId='" + SupplierID + "'and TariffSheetId='0'");
                            //<3>[Hotelname-Tariff markup]
                            if (drHotelMarkup.GetLength(0) == 0 || Convert.ToDouble(drHotelMarkup[0]["MarkupAmount"]) <= 0)
                                drHotelMarkup = dtCountryCityMarkup.Select("CategoryWiseMarkup='Hotel'and RaynaID='" + GiataId + "' and SupplierId='0'" + "and TariffSheetId='" + TariffID.ToString() + "'");
                            //<4>[Hotelname markup]
                            if (drHotelMarkup.GetLength(0) == 0 || Convert.ToDouble(drHotelMarkup[0]["MarkupAmount"]) <= 0)
                                drHotelMarkup = dtCountryCityMarkup.Select("CategoryWiseMarkup='Hotel'and RaynaID='" + GiataId + "' and SupplierId='0'" + "and TariffSheetId='0'");
                            //<5>[Star-Tarrif Markup]
                            if (drHotelMarkup.GetLength(0) == 0 || Convert.ToDouble(drHotelMarkup[0]["MarkupAmount"]) <= 0)
                                drHotelMarkup = dtCountryCityMarkup.Select("CategoryWiseMarkup='Star'and star='" + Rating + "' and CityID='" + SearchPara.CityID.ToString() + "' and SupplierID='" + SupplierID.ToString() + "' and TariffSheetId='" + TariffID.ToString() + "'");
                            //<6>[Star markup]
                            if (drHotelMarkup.GetLength(0) == 0 || Convert.ToDouble(drHotelMarkup[0]["MarkupAmount"]) <= 0)
                                drHotelMarkup = dtCountryCityMarkup.Select("CategoryWiseMarkup='Star'and star='" + Rating + "' and CityID='" + SearchPara.CityID.ToString() + "' and SupplierID='" + SupplierID.ToString() + "' and TariffSheetId='0'");
                            //<7>[City-Tarrif Markup]
                            if (drHotelMarkup.GetLength(0) == 0 || Convert.ToDouble(drHotelMarkup[0]["MarkupAmount"]) <= 0)
                                drHotelMarkup = dtCountryCityMarkup.Select("CategoryWiseMarkup='City'and CityID='" + SearchPara.CityID.ToString() + "' and SupplierID='" + SupplierID.ToString() + "' and TariffSheetId='" + TariffID.ToString() + "'");
                            //<8>[City Markup]
                            if (drHotelMarkup.GetLength(0) == 0 || Convert.ToDouble(drHotelMarkup[0]["MarkupAmount"]) <= 0)
                                drHotelMarkup = dtCountryCityMarkup.Select("CategoryWiseMarkup='City'and CityID='" + SearchPara.CityID.ToString() + "' and SupplierID='" + SupplierID.ToString() + "' and TariffSheetId='0'");
                            //<9>[Country-Tarrif Markup]
                            if (drHotelMarkup.GetLength(0) == 0 || Convert.ToDouble(drHotelMarkup[0]["MarkupAmount"]) <= 0)
                                drHotelMarkup = dtCountryCityMarkup.Select("CategoryWiseMarkup='Country'and CountryID='" + SearchPara.CountryID.ToString() + "' and SupplierID='" + SupplierID.ToString() + "' and TariffSheetId='" + TariffID.ToString() + "'");
                            //<10>[Country Markup]
                            if (drHotelMarkup.GetLength(0) == 0 || Convert.ToDouble(drHotelMarkup[0]["MarkupAmount"]) <= 0)
                                drHotelMarkup = dtCountryCityMarkup.Select("CategoryWiseMarkup='Country'and CountryID='" + SearchPara.CountryID.ToString() + "' and SupplierID='" + SupplierID.ToString() + "' and TariffSheetId='0'");
                            //<11>[Agent Markup]
                            if (drHotelMarkup.GetLength(0) == 0 || Convert.ToDouble(drHotelMarkup[0]["MarkupAmount"]) <= 0)
                                drHotelMarkup = dtCountryCityMarkup.Select("CategoryWiseMarkup='Agent'and SupplierID='" + SupplierID + "' and AgentID='" + AgentId.ToString() + "'");
                            //<12>[Tariff Markup]
                            if (drHotelMarkup.GetLength(0) == 0 || Convert.ToDouble(drHotelMarkup[0]["MarkupAmount"]) <= 0)
                                drHotelMarkup = dtCountryCityMarkup.Select("CategoryWiseMarkup='Tariff'and TariffSheetId='" + TariffID.ToString() + "' and SupplierID='" + SupplierID.ToString() + "'");
                            //<13>[Global Markup]
                            if (drHotelMarkup.GetLength(0) == 0 || Convert.ToDouble(drHotelMarkup[0]["MarkupAmount"]) <= 0)
                                drHotelMarkup = dtCountryCityMarkup.Select("CategoryWiseMarkup='Supplier'and SupplierID='" + SupplierID.ToString() + "'");
                        }
                        //else
                        //drHotelMarkup = dtCountryCityMarkup.Select("RaynaID='" + OwnsystemHotelId + "'and SupplierID='" + SupplierID + "' ");
                        #endregion
                        if (drHotelMarkup.GetLength(0) > 0)
                        {
                            if (Convert.ToBoolean(drHotelMarkup[0]["IsPercentage"]) && Convert.ToDouble(drHotelMarkup[0]["MarkupAmount"]) > 0)
                            {
                                AgentPercentage = Convert.ToDouble(drHotelMarkup[0]["MarkupAmount"]);
                                isPerce = true;
                            }
                            else if (!Convert.ToBoolean(drHotelMarkup[0]["IsPercentage"]) && Convert.ToDouble(drHotelMarkup[0]["MarkupAmount"]) > 0)
                            {
                                isPerce = false;
                                AgentMarkupAmount = Convert.ToDouble(drHotelMarkup[0]["MarkupAmount"]);
                            }
                        }
                    }
                    #endregion
                    LowestRate = ProcessHotelRateBreakupPrice_WithMarkup(1, SearchPara.CityID, Convert.ToString(SearchPara.CountryID), LowestRate / 1, B2CMarkup, IsParentId, isPerce, AgentPercentage, AgentMarkupAmount, SubAgentMarkup, ref VatLowestRate);
                    //dr["IsRecomondedHotel"] = Convert.ToBoolean(drGiataDetails[0]["IsRecomondedHotel"]);
                    // dr["VatdiffAmount"] = VatLowestRate;
                    //  dr["SellingPrice"] = LowestRate;
                    dr["FinalSellingPrice"] = LowestRate;
                }

            }
            catch (Exception ex)
            {
                willAddOrNot = false;
                _exception.Error(ex, "", Sup);
            }
        }

        

        public static decimal GetcalVatAmount(decimal amount, decimal percentage)
        {

            return (amount * percentage / 100);
        }

        public void getAgetMarkupSetting(string suppliername, string raynaid, DataTable dtSuplierMarkup, DataTable dtMarkupDetails, ref double AgentPercentage, ref double AgentMarkupAmount, ref Boolean blnIsShowSupplier, ref Boolean isPerce, int agentid, int hotelRating, int CityID, string CountryId, string CheckinDate1)
        {
            try
            {
                //if (CityID == 0)
                //    CityID = SearchPara.CityID;
                if (CountryId == "" || CountryId == null)
                    CountryId = "0";

                int SupplierID = 0, TariffID = 0;
                if (dtSuplierMarkup != null)
                {
                    DataRow[] drSuppMarkUp = dtSuplierMarkup.Select("SupplierName='" + suppliername + "'");
                    if (drSuppMarkUp.GetLength(0) > 0)
                    {
                        AgentPercentage = Convert.ToDouble(drSuppMarkUp[0]["AgentWiseMarkup"]);
                        blnIsShowSupplier = (Convert.ToInt16(drSuppMarkUp[0]["IsShowSupplier"]) == 1 ? true : false);
                        SupplierID = Convert.ToInt32(drSuppMarkUp[0]["SupplierID"]);
                        TariffID = Convert.ToInt32(drSuppMarkUp[0]["MarkupCategoryId"]);
                    }
                }
                if (dtMarkupDetails != null && dtMarkupDetails.Rows.Count > 0)
                {
                    //DataRow[] drHotelMarkup = dtMarkupDetails.Select("Raynaid='" + raynaid + "'and SupplierID='" + SupplierID + "' ");
                    DataRow[] drHotelMarkup = null;
                    #region "New Markup Logic"
                    //if (agentid == 100)
                    {

                        int Raynaid = raynaid == "" ? -1 : (Convert.ToInt32(raynaid));

                        DateTime CurDate = DateTime.Now.Date;
                        DateTime CheckinDate = DateTime.Now;
                        try
                        {
                            CheckinDate = DateTime.ParseExact(CheckinDate1, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                            //CheckinDate = DateTime.Parse(CheckinDate1);
                        }
                        catch (Exception)
                        {
                            CheckinDate = DateTime.Now;
                        }
                        //Added new TicketID #41481
                        int DayCount = Convert.ToInt32((CheckinDate - DateTime.Now.Date).TotalDays);
                        drHotelMarkup = dtMarkupDetails.Select("CategoryWiseMarkup='DayCountMarkup' and AgentID='" + agentid.ToString() + "' and TariffSheetId='" + TariffID.ToString() + "' and SupplierId='" + SupplierID + "' and Daycount<'" + DayCount + "'");
                        if (drHotelMarkup.GetLength(0) == 0 || Convert.ToDouble(drHotelMarkup[0]["MarkupAmount"]) <= 0)
                            drHotelMarkup = dtMarkupDetails.Select("CategoryWiseMarkup='DayCountMarkup_Tariff' and AgentID='0' and TariffSheetId='" + TariffID.ToString() + "' and SupplierId='" + SupplierID + "' and Daycount<'" + DayCount + "'");


                        if (drHotelMarkup.GetLength(0) == 0 || Convert.ToDouble(drHotelMarkup[0]["MarkupAmount"]) <= 0)
                            drHotelMarkup = dtMarkupDetails.Select("CategoryWiseMarkup='Search Date Wish Markup' and AgentID='" + agentid.ToString() + "' and fromdate <='" + CurDate + "' and todate >= '" + CurDate + "' and SupplierId='" + SupplierID + "'" + " and TariffSheetId='" + TariffID.ToString() + "'");

                        if (drHotelMarkup.GetLength(0) == 0 || Convert.ToDouble(drHotelMarkup[0]["MarkupAmount"]) <= 0)
                            drHotelMarkup = dtMarkupDetails.Select("CategoryWiseMarkup='Search Date Wish Markup' and AgentID='0' and fromdate <='" + CurDate + "' and todate >='" + CurDate + "' and SupplierId='" + SupplierID + "'" + " and TariffSheetId='" + TariffID.ToString() + "'");

                        if (drHotelMarkup.GetLength(0) == 0 || Convert.ToDouble(drHotelMarkup[0]["MarkupAmount"]) <= 0)
                            drHotelMarkup = dtMarkupDetails.Select("CategoryWiseMarkup='Check in Date Wish Markup' and AgentID='" + agentid.ToString() + "' and fromdate <='" + CheckinDate + "' and todate >= '" + CheckinDate + "' and SupplierId='" + SupplierID + "'" + " and TariffSheetId='" + TariffID.ToString() + "'");

                        if (drHotelMarkup.GetLength(0) == 0 || Convert.ToDouble(drHotelMarkup[0]["MarkupAmount"]) <= 0)
                            drHotelMarkup = dtMarkupDetails.Select("CategoryWiseMarkup='Check in Date Wish Markup' and AgentID='0' and fromdate <='" + CheckinDate + "' and todate >='" + CheckinDate + "' and SupplierId='" + SupplierID + "'" + " and TariffSheetId='" + TariffID.ToString() + "'");


                        //<1>[Supplier hotelname-Tariff markup]
                        if (drHotelMarkup.GetLength(0) == 0 || Convert.ToDouble(drHotelMarkup[0]["MarkupAmount"]) <= 0)
                            drHotelMarkup = dtMarkupDetails.Select("CategoryWiseMarkup='Hotel' and RaynaID='" + Raynaid + "'and SupplierId='" + SupplierID + "' and TariffSheetId='" + TariffID.ToString() + "'");
                        //<2>[Supplier hotelname markup]
                        if (drHotelMarkup.GetLength(0) == 0 || Convert.ToDouble(drHotelMarkup[0]["MarkupAmount"]) <= 0)
                            drHotelMarkup = dtMarkupDetails.Select("CategoryWiseMarkup='Hotel' and RaynaID='" + Raynaid + "'and SupplierId='" + SupplierID + "'and TariffSheetId='0'");
                        //<3>[Hotelname-Tariff markup]
                        if (drHotelMarkup.GetLength(0) == 0 || Convert.ToDouble(drHotelMarkup[0]["MarkupAmount"]) <= 0)
                            drHotelMarkup = dtMarkupDetails.Select("CategoryWiseMarkup='Hotel'and RaynaID='" + Raynaid + "' and SupplierId='0' and TariffSheetId='" + TariffID.ToString() + "'");
                        //<4>[Hotelname markup]
                        if (drHotelMarkup.GetLength(0) == 0 || Convert.ToDouble(drHotelMarkup[0]["MarkupAmount"]) <= 0)
                            drHotelMarkup = dtMarkupDetails.Select("CategoryWiseMarkup='Hotel'and RaynaID='" + Raynaid + "' and SupplierId='0' and TariffSheetId='0'");
                        //<5>[Star-Tarrif Markup]
                        if (drHotelMarkup.GetLength(0) == 0 || Convert.ToDouble(drHotelMarkup[0]["MarkupAmount"]) <= 0)
                            drHotelMarkup = dtMarkupDetails.Select("CategoryWiseMarkup='Star' and star='" + hotelRating.ToString() + "'and SupplierId='" + SupplierID + "'" + " and CityID='" + CityID.ToString() + "' and TariffSheetId='" + TariffID.ToString() + "'");
                        //<6>[Star markup]
                        if (drHotelMarkup.GetLength(0) == 0 || Convert.ToDouble(drHotelMarkup[0]["MarkupAmount"]) <= 0)
                            drHotelMarkup = dtMarkupDetails.Select("CategoryWiseMarkup='Star' and star='" + hotelRating.ToString() + "'and SupplierId='" + SupplierID + "'" + " and CityID='" + CityID.ToString() + "'and TariffSheetId='0'");
                        //<7>[City-Tarrif Markup]
                        if (drHotelMarkup.GetLength(0) == 0 || Convert.ToDouble(drHotelMarkup[0]["MarkupAmount"]) <= 0)
                            drHotelMarkup = dtMarkupDetails.Select("CategoryWiseMarkup='City' and CityID='" + CityID.ToString() + "'and SupplierId='" + SupplierID + "'" + " and TariffSheetId='" + TariffID.ToString() + "'");
                        //<8>[City Markup]
                        if (drHotelMarkup.GetLength(0) == 0 || Convert.ToDouble(drHotelMarkup[0]["MarkupAmount"]) <= 0)
                            drHotelMarkup = dtMarkupDetails.Select("CategoryWiseMarkup='City' and CityID='" + CityID.ToString() + "'and SupplierId='" + SupplierID + "'" + "and TariffSheetId='0'");
                        //<9>[Country-Tarrif Markup]
                        if (drHotelMarkup.GetLength(0) == 0 || Convert.ToDouble(drHotelMarkup[0]["MarkupAmount"]) <= 0)
                            drHotelMarkup = dtMarkupDetails.Select("CategoryWiseMarkup='Country' and CountryID='" + CountryId.ToString() + "'and SupplierId='" + SupplierID + "'" + " and TariffSheetId='" + TariffID.ToString() + "'");
                        //<10>[Country Markup]
                        if (drHotelMarkup.GetLength(0) == 0 || Convert.ToDouble(drHotelMarkup[0]["MarkupAmount"]) <= 0)
                            drHotelMarkup = dtMarkupDetails.Select("CategoryWiseMarkup='Country' and CountryID='" + CountryId.ToString() + "'and SupplierId='" + SupplierID + "'" + "and TariffSheetId='0'");
                        //<11>[Agent Markup]
                        if (drHotelMarkup.GetLength(0) == 0 || Convert.ToDouble(drHotelMarkup[0]["MarkupAmount"]) <= 0)
                            drHotelMarkup = dtMarkupDetails.Select("CategoryWiseMarkup='Agent' and SupplierID='" + SupplierID + "' and AgentID='" + agentid.ToString() + "'");
                        //<12>[Tariff Markup]
                        if (drHotelMarkup.GetLength(0) == 0 || Convert.ToDouble(drHotelMarkup[0]["MarkupAmount"]) <= 0)
                            drHotelMarkup = dtMarkupDetails.Select("CategoryWiseMarkup='Tariff' and TariffSheetId='" + TariffID.ToString() + "' and SupplierID='" + SupplierID.ToString() + "'");
                        //<13>[Global Markup]
                        if (drHotelMarkup.GetLength(0) == 0 || Convert.ToDouble(drHotelMarkup[0]["MarkupAmount"]) <= 0)
                            drHotelMarkup = dtMarkupDetails.Select("CategoryWiseMarkup='Supplier' and SupplierID='" + SupplierID.ToString() + "'");

                    }
                    //else
                    //drHotelMarkup = dtMarkupDetails.Select("RaynaID='" + raynaid + "'and SupplierID='" + SupplierID + "' ");
                    #endregion
                    if (drHotelMarkup.GetLength(0) > 0)
                    {
                        if (Convert.ToBoolean(drHotelMarkup[0]["IsPercentage"]) && Convert.ToDouble(drHotelMarkup[0]["MarkupAmount"]) > 0)
                        //if (Convert.ToBoolean(drHotelMarkup[0]["IsPercentage"]))
                        {
                            AgentPercentage = Convert.ToDouble(drHotelMarkup[0]["MarkupAmount"]);
                            isPerce = true;
                        }
                        else if (!Convert.ToBoolean(drHotelMarkup[0]["IsPercentage"]) && Convert.ToDouble(drHotelMarkup[0]["MarkupAmount"]) > 0)
                        // else if (!Convert.ToBoolean(drHotelMarkup[0]["IsPercentage"]))
                        {
                            isPerce = false;
                            AgentMarkupAmount = Convert.ToDouble(drHotelMarkup[0]["MarkupAmount"]);
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
}
