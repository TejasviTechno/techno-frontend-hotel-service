using Microsoft.Data.SqlClient;
using System.Data;
using XConnect.CommonUtilities;
using XConnect.CommonUtilities.Interface;
using XConnect.DAL.Interface;
using XConnect.ErrorLogHandler.Interface;
using XConnect.Models;
using XConnect.Models.Hotel;
using static XConnect.CommonUtilities.HtmlExtensions;


namespace XConnect.CommonUtilities
{
    public class SupplierResRepo : ISupplierRes
    {
        private readonly IException _exception;
        private readonly IDLConnection _dlConnection;

        public SupplierResRepo(IException exception, IDLConnection dlConnection)
        {
            _exception = exception;
            _dlConnection = dlConnection;
        }

        public async Task<Options> GetCommonRoomForSuppliers(long GiataId, TokenData tokenData, AdvancedOptions advancedOptions, double LowestRate, DataRow drXmlHotel, DataTable dtHotelRoom, DataTable dt_Cancellation, DataTable dt_RateBreackUp, HotelSearchParameter SearchParam, bool IsRoomRestriction = false, int NoOfReturnRoom = 0, int ForMoreThenRooms = 1)
        {
            Options hotelListNew = GetDataFromHotelRow(drXmlHotel);

            _FinalRoom _finalRoom = new _FinalRoom();
            try
            {
                //Stopwatch timerroom2 = new System.Diagnostics.Stopwatch();
                //timerroom2.Start();
                //if (SearchParam != null)
                //{
                //    timerroom2.Stop();
                //    SearchParam.SupplierTime += ",|Process GetGiata : " + Provider + "  " + "|Execution Time :" + timerroom2.Elapsed.ToString();
                //}
                //timerroom2.Restart();
                hotelListNew.MinPrice = Convert.ToDecimal(LowestRate);
                hotelListNew.HotelCode = Convert.ToString(GiataId);
                decimal minprice = Convert.ToDecimal(LowestRate);
                bool IsCombineRoom = true;
                //hotelListNew.HotelRooms = await GetHotelRoomForOptions(advancedOptions, tokenData, dtHotelRoom, dt_Cancellation, dt_RateBreackUp, SearchParam, minprice, IsCombineRoom);
                //_finalRoom = await GetHotelRoomForOptions(advancedOptions, tokenData, dtHotelRoom, dt_Cancellation, dt_RateBreackUp, SearchParam, minprice, IsCombineRoom); //jay
                _finalRoom = await GetHotelRoomForOptions(advancedOptions, tokenData, dtHotelRoom, dt_Cancellation, dt_RateBreackUp, SearchParam, minprice, IsCombineRoom, IsRoomRestriction, NoOfReturnRoom, ForMoreThenRooms); //Added on 02-JAN-26

                hotelListNew.HotelRooms = _finalRoom._finalRoomList;//jay


                if (_finalRoom.MinPrice == 0)
                    return null;
                hotelListNew.MinPrice = _finalRoom.MinPrice;
                hotelListNew.IsCombineRoom = _finalRoom.IsRoomCombine;
                return hotelListNew;
            }
            catch (Exception ex)
            {
                await _exception.Error(ex);
                return null;

            }
        }

        public async Task<_FinalRoom> GetHotelRoomForOptions(AdvancedOptions advancedOptions, TokenData tokenData, DataTable RoomRow, DataTable PolicyTable, DataTable RateBreakup, HotelSearchParameter SearchPara, decimal MinPrice, bool IsRoomCombine /* will return Min Price */, bool IsRoomRestriction = false, int NoOfReturnRoom = 0, int ForMoreThenRooms = 1)
        {
            _FinalRoom _finalRoom = new _FinalRoom();

            List<List<_FinalRoomList>> rooms = new List<List<_FinalRoomList>>();
            try
            {
                string StrTempSupplier = "";
                //if (SearchPara.Filters.IsShowRooms == 0)
                // return rooms;
                List<X_Roomdetails> roomDetails = HtmlExtensions.ToList<X_Roomdetails>(RoomRow);
                if (roomDetails.Count == 0)
                {
                    _finalRoom.MinPrice = MinPrice; ; //jay
                    _finalRoom.IsRoomCombine = IsRoomCombine; //jay
                    return _finalRoom;

                }
                SortRoomByPrice sortRoom = new SortRoomByPrice();
                roomDetails.Sort(sortRoom);
                List<X_PolicyDetails> policyDetails = HtmlExtensions.ToList<X_PolicyDetails>(PolicyTable);
                List<X_RateBreakupDetails> rateBeckup = HtmlExtensions.ToList<X_RateBreakupDetails>(RateBreakup);
                List<X_Roomdetails>[] roomtypearr = new List<X_Roomdetails>[SearchPara.Rooms.Count + 1];

                for (int j = 1; j <= SearchPara.Rooms.Count; j++)
                {
                    for (int rr = 0; rr < roomDetails.Count; rr++)
                    {
                        //if (rr > CacheMaster.RoomLimit_10 && (advancedOptions.SearchByOneID == false || SearchPara.Rooms.Count > 1))
                        //    break;
                        if (tokenData.AgentCode.ToLower() == "amtj" || tokenData.AgentCode.ToLower() == "nigm")
                        {
                            if (rr > CacheMaster.RoomLimit_20 && (advancedOptions.SearchByOneID == false || SearchPara.Rooms.Count > 1))
                                break;
                        }
                        else
                        {
                            if (rr > CacheMaster.RoomLimit_10 && (advancedOptions.SearchByOneID == false || SearchPara.Rooms.Count > 1))
                                break;
                        }
                        if (roomDetails[rr].RoomNo == j.ToString() /* && item.GIITACode == GiataIdd && item.Hit == Hit*/)
                        {
                            if (roomtypearr[j] == null)
                            {
                                roomtypearr[j] = new List<X_Roomdetails>();
                            }
                            roomtypearr[j].Add(roomDetails[rr]);
                        }
                    }
                    //foreach (X_Roomdetails item in roomDetails)
                    //{

                    //}

                }
                IsRoomCombine = true;

                if (roomtypearr.Length > 0)
                {
                    if (roomtypearr[1].Count > 0)
                        IsRoomCombine = roomtypearr[1][0].IsCombineRoom;
                }
                List<List<X_Roomdetails>> Mainrooms = new List<List<X_Roomdetails>>();

                if (IsRoomCombine)
                {
                    for (var co = 0; co < roomtypearr[1].Count; co++)
                    {
                        List<X_Roomdetails> arrcom = new List<X_Roomdetails>();
                        arrcom.Add(roomtypearr[1][co].CastList<X_Roomdetails>());
                        Mainrooms.Add(arrcom);
                    }
                }
                else
                {
                    List<String> arrmeal = new List<string>();
                    List<string> lstt = roomtypearr[1].Select(s => s.MealName).Distinct().ToList();
                    if (lstt != null && lstt.Count > 0)
                        arrmeal.AddRange(lstt);   //Destinact meal
                                                  //  test1 = new List<List<X_RoomdetailsFinal>>();
                    #region FilterMealWise
                    for (var m = 0; m < arrmeal.Count; m++)
                    {
                        var MealName = arrmeal[m];
                        List<X_Roomdetails>[] roomtypearr1 = new List<X_Roomdetails>[SearchPara.Rooms.Count];
                        for (int rm = 1; rm <= SearchPara.Rooms.Count; rm++)
                        {
                            if (roomtypearr[rm] == null)//Added on 10-APR-2024
                                continue;
                            // List<X_Roomdetails> Suppliermeal = roomtypearr[rm].Where(A => A.MealName == MealName).ToList(); //list of meal + supplier
                            List<X_Roomdetails> xlist = roomtypearr[rm].Where(A => A.MealName == MealName).ToList();
                            List<X_Roomdetails> Suppliermeal = new List<X_Roomdetails>();
                            if (xlist != null)
                                Suppliermeal = xlist;
                            foreach (X_Roomdetails item in Suppliermeal)
                            {
                                if (roomtypearr1[rm - 1] == null)
                                {
                                    roomtypearr1[rm - 1] = new List<X_Roomdetails>();
                                }
                                roomtypearr1[rm - 1].Add(item.CastList<X_Roomdetails>());
                            }
                        }
                        List<List<X_Roomdetails>> dataV1 = MergeRooms(roomtypearr1, SearchPara);

                        if (dataV1.Count > 0)
                        {
                            //Mainrooms.AddRange(dataV1.ReturnOnly(CacheMaster.RoomLimit).ToList());
                            if (advancedOptions.SearchByOneID == false || SearchPara.Rooms.Count > 1)
                            {
                                if (tokenData.AgentCode.ToLower() == "amtj" || tokenData.AgentCode.ToLower() == "nigm")
                                {

                                    Mainrooms.AddRange(dataV1.ReturnOnly((CacheMaster.RoomLimit_20 + 1)).ToList());
                                }
                                else
                                {
                                    Mainrooms.AddRange(dataV1.ReturnOnly((CacheMaster.RoomLimit_10 + 1)).ToList());
                                }
                            }
                            else
                            {
                                if (advancedOptions.SearchByOneID == false)
                                    Mainrooms.AddRange(dataV1.ReturnOnly((30)).ToList());
                                else
                                    Mainrooms.AddRange(dataV1.ReturnOnly((100)).ToList());// added on 21-JUN-2024
                            }
                        }
                    }
                    #endregion
                }
                List<Decimal> arrMinPrice = new List<Decimal>();
                for (int i = 0; i < Mainrooms.Count; i++)
                {
                    //Below Added on 2-JAN-26
                    if (IsRoomRestriction && NoOfReturnRoom < (i + 1) && SearchPara.Rooms.Count > ForMoreThenRooms)//Added by Jay on 2-JAN-26
                        break;

                    List<_FinalRoomList> list = new List<_FinalRoomList>();
                    decimal price = 0;
                    for (int j = 0; j < Mainrooms[i].Count; j++)
                    {
                        string SupplierCurrency = Mainrooms[i][j].SupplierCurrency;
                        _FinalRoomList final = new _FinalRoomList();
                        final.BookingStatus = Mainrooms[i][j].BookingStatus;
                        if (final.BookingStatus == "PackageRate")
                            final.BookingStatus = "Available";
                        ////////////// @New mealName swap Cheanges for ostrovok XTG & other client

                        //if (tokenData.AgentCode.ToLower() == "klnb" || tokenData.AgentCode.ToLower() == "raku" || tokenData.AgentCode.ToLower() == "lnda" || tokenData.AgentCode.ToLower() == "inst" || tokenData.AgentCode.ToLower() == "mtbo" || tokenData.AgentCode.ToLower() == "spet" || tokenData.AgentCode.ToLower() == "fzcb" || tokenData.AgentCode.ToLower() == "mwrl" || tokenData.AgentCode.ToLower() == "expa" || tokenData.AgentCode.ToLower() == "guji" || tokenData.AgentCode.ToLower() == "ostr" || tokenData.AgentCode.ToLower() == "vibe" || tokenData.AgentCode.ToLower() == "dazz" || tokenData.AgentCode.ToLower() == "mapa" || tokenData.AgentCode.ToLower() == "reis" || tokenData.AgentCode.ToLower() == "pres" || tokenData.AgentCode.ToLower() == "nigm" || tokenData.AgentCode.ToLower() == "amtj")
                        if (SwapMealName(tokenData.AgentCode.ToLower()))
                        {
                            final.MappedMealName = "Room Only";
                            final.MealName = "Room Only";
                            final.EssentialInformation = Mainrooms[i][j].EssentialInformation;
                            final.MappedMealName = Mainrooms[i][j].MealName.Replace("'", "");

                            //if (tokenData.AgentCode.ToLower() == "dida" || tokenData.AgentCode.ToLower() == "bdtc")
                            //{
                            //    final.MealName = final.MappedMealName;// GetMealMappingDataSupplierMealTypestring(final.MappedMealName.Trim());
                            //}
                            //else
                            //{
                            final.MealName = GetMealMappingDataSupplierMealTypestring(final.MappedMealName.Trim());
                            //}

                            if (final.MealName == "")
                            {
                                DataTable DtSupplierMealType = await GetMealMappingDataSupplierMealType(final.MappedMealName.Trim());
                                if (DtSupplierMealType != null && DtSupplierMealType.Rows.Count > 0)
                                {
                                    final.MealName = Convert.ToString(DtSupplierMealType.Rows[0]["MealType"]);
                                }
                            }
                        }
                        else
                        {
                            final.MappedMealName = "NotMapped";
                            final.EssentialInformation = Mainrooms[i][j].EssentialInformation;
                            final.MealName = Mainrooms[i][j].MealName.Replace("'", "");

                            final.MappedMealName = GetMealMappingDataSupplierMealTypestring(final.MealName.Trim());
                            if (final.MappedMealName == "")
                            {
                                DataTable DtSupplierMealType = await GetMealMappingDataSupplierMealType(final.MealName.Trim());
                                if (DtSupplierMealType != null && DtSupplierMealType.Rows.Count > 0)
                                {
                                    final.MappedMealName = Convert.ToString(DtSupplierMealType.Rows[0]["MealType"]);
                                }
                            }

                        }

                        final.Price = Convert.ToDecimal(Mainrooms[i][j].FinalSellingPrice);
                        if (tokenData.IsFromAPI)
                            final.Price = final.Price.CalculatePrice(tokenData, advancedOptions, SupplierCurrency); //comment bcoz no need to return in agent currency 04-FEB-25 


                        price += Convert.ToDecimal(final.Price);
                        final.RoomNo = Mainrooms[i][j].RoomNo;
                        final.RoomTypeName = Mainrooms[i][j].RoomTypeName;
                        IsRoomCombine = Mainrooms[i][j].IsCombineRoom;


                        //Mainrooms[i][j].MealName = final.MealName; //@New
                        //Mainrooms[i][j].MappedMealName = final.MappedMealName;

                        //New added on 14-Mar-2024
                        Mainrooms[i][j].MealName = final.MealName.Replace("-", " ");
                        Mainrooms[i][j].MappedMealName = final.MappedMealName.Replace("-", " ");

                        final.RoomToken = Mainrooms[i][j].EncryptRoomToken();
                        List<_FinalPolicy> _FinalPolicy = new List<_FinalPolicy>();
                        final.CancellationToken = "";
                        if (policyDetails.Count == 0 && SearchPara.IsAvailbilityWithCancellation == 1)// send NRF policy if supplier not support.
                        {
                            _FinalPolicy obj = new _FinalPolicy();
                            //if ((policyDetails[0].OwnsystemHotelId) == (Mainrooms[i][j].OwnsystemHotelId) && policyDetails[0].RoomTypeId == Mainrooms[i][j].RoomTypeId && policyDetails[p].MealId == Mainrooms[i][j].MealId && policyDetails[p].RoomNo == Mainrooms[i][j].RoomNo)
                            {
                                obj.FromDate = Convert.ToDateTime(DateTime.Now).ToString("MM-dd-yyyy");
                                if (SearchPara.PastDate == true)
                                {
                                    obj.FromDate = Convert.ToDateTime(DateTime.Now).AddDays(-1).ToString("MM-dd-yyyy");
                                }//#Due Ticket 33069
                                obj.ToDate = Convert.ToDateTime(SearchPara.CheckInDate.Substring(3, 2) + "/" + SearchPara.CheckInDate.Substring(0, 2) + "/" + SearchPara.CheckInDate.Substring(6, 4)).Date.ToString("MM-dd-yyyy");
                                obj.CancellationPrice = Convert.ToDecimal(final.Price);

                                //For CancellationToken
                                X_PolicyDetails obj_temp = new X_PolicyDetails();
                                obj_temp.RoomNo = Mainrooms[i][j].RoomNo;
                                obj_temp.FromDate = obj.FromDate;
                                obj_temp.ToDate = obj.ToDate;
                                obj_temp.CompanyCancellationPrice = Mainrooms[i][j].CompanySellingPrice;
                                obj_temp.AgentCancellationPrice = Mainrooms[i][j].AgentSellingPrice;
                                obj_temp.PurchaseToken = "";
                                obj_temp.SPUI = Mainrooms[i][j].EssentialInformation;
                                obj_temp.BuyingPrice = Mainrooms[i][j].BuyingPrice;
                                final.CancellationToken += obj_temp.EncryptPolicyToken();//add to policy token
                                                                                         //End

                                //below line comment on 3-sep-2021
                                //obj.CancellationPrice = obj.CancellationPrice.CalculatePrice(tokenData, advancedOptions, SupplierCurrency);
                                _FinalPolicy.Add(obj);
                            }

                        }
                        else
                        {

                            for (int p = 0; p < policyDetails.Count; p++)
                            {
                                _FinalPolicy obj = new _FinalPolicy();
                                if ((policyDetails[p].OwnsystemHotelId) == (Mainrooms[i][j].OwnsystemHotelId) && policyDetails[p].RoomTypeId == Mainrooms[i][j].RoomTypeId && policyDetails[p].MealId == Mainrooms[i][j].MealId && policyDetails[p].RoomNo == Mainrooms[i][j].RoomNo)
                                {
                                    obj.FromDate = Convert.ToDateTime(policyDetails[p].FromDate).ToString("MM-dd-yyyy");
                                    if (SearchPara.PastDate == true)
                                    {
                                        obj.FromDate = Convert.ToDateTime(DateTime.Now).AddDays(-1).ToString("MM-dd-yyyy");
                                    }//#Due Ticket 33069
                                    obj.ToDate = Convert.ToDateTime(policyDetails[p].ToDate).ToString("MM-dd-yyyy");
                                    obj.CancellationPrice = Convert.ToDecimal(policyDetails[p].AgentCancellationPrice);
                                    final.CancellationToken += policyDetails[p].EncryptPolicyToken();//add to policy token
                                    if (tokenData.IsFromAPI)
                                        obj.CancellationPrice = obj.CancellationPrice.CalculatePrice(tokenData, advancedOptions, SupplierCurrency); //comment bcoz no need to return in agent currency 04-FEB-25 
                                    _FinalPolicy.Add(obj);
                                }
                            }
                        }
                        if (final.CancellationToken != "")
                            final.CancellationToken = final.CancellationToken.ToBase64StringManual(false);
                        final.CancellationPolicy = _FinalPolicy;
                        final.RateDetailsToken = "";
                        for (int r = 0; r < rateBeckup.Count; r++)
                        {
                            if ((rateBeckup[r].OwnsystemHotelId) == (Mainrooms[i][j].OwnsystemHotelId) && rateBeckup[r].RoomTypeId == Mainrooms[i][j].RoomTypeId && rateBeckup[r].MealId == Mainrooms[i][j].MealId && rateBeckup[r].RoomNo == Mainrooms[i][j].RoomNo)
                            {
                                final.RateDetailsToken += rateBeckup[r].EncryptRateBreakUp();
                            }
                        }
                        if (final.RateDetailsToken != "")
                            final.RateDetailsToken = final.RateDetailsToken.ToBase64StringManual(false);


                        final.SupplierCurrency = Mainrooms[i][j].SupplierCurrency; //Added on 4-FEB-26

                        list.Add(final);
                    }
                    arrMinPrice.Add(price);
                    rooms.Add(list);



                }



                MinPrice = arrMinPrice.Min();

                _finalRoom._finalRoomList = rooms; //jay
                _finalRoom.MinPrice = MinPrice; ; //jay
                _finalRoom.IsRoomCombine = IsRoomCombine; //jay
            }
            catch (Exception ex)
            {
                await _exception.Error(ex, SearchPara.SearchKey);
            }
            if (SearchPara.Filters.IsShowRooms == 0)
            {
                //return new List<List<_FinalRoomList>>();
                return new _FinalRoom();
            }
            else
                return _finalRoom;
        }

        public async Task<DataTable> GetMealMappingDataSupplierMealType(string SupplierMealType)
        {
            SqlParameter[] para = new SqlParameter[] {
                //new SqlParameter("@data",HtmlExtensions.ConvertToDataTable(list.IPsList)),
                new SqlParameter("@SupplierMealType",SupplierMealType),
                

                //new SqlParameter("@IsLive",Convert.ToInt32(ConfigurationManager.AppSettings["IsLive"]))

                };
            DataSet ds = await _dlConnection.GetDataSet("MealFilter_APIMealData_SupplierMealType", para);
            return ds.Tables[0];
        }

        public static string GetMealMappingDataSupplierMealTypestring(string SupplierMealType) //Update on 19 Nov 2025
        {
            string Meal = "";

            switch (SupplierMealType.ToLower())
            {
                case "all inclusive plus food and beverage": { Meal = "American Breakfast"; } break;
                case "american breakfast - non refundable": { Meal = "American Breakfast"; } break;
                case "american buffet breakfast": { Meal = "American Breakfast"; } break;
                case "american-breakfast": { Meal = "American Breakfast"; } break;
                case "bed and american breakfast": { Meal = "American Breakfast"; } break;
                case "diamond all inclusive": { Meal = "American Breakfast"; } break;
                case "full american buffet": { Meal = "American Breakfast"; } break;
                case "full board silver (drinks included)": { Meal = "American Breakfast"; } break;
                case "24 hours all inclusive dine around": { Meal = "All Inclusive"; } break;
                case "24 hours premium all inclusive": { Meal = "All Inclusive"; } break;
                case "3 meals daily , all inclusive , breakfast , buffet dinner , complimentary breakfast , complimentary dinner , complimentary lunch , dinner included , lunch included": { Meal = "All Inclusive"; } break;
                case "3 meals daily , all inclusive , breakfast , buffet dinner , complimentary breakfast , complimentary dinner , dinner included , local breakfast , lunch included , vegetarian dinner": { Meal = "All Inclusive"; } break;
                case "3 meals daily , all inclusive , breakfast , buffet dinner , dinner included , lunch included": { Meal = "All Inclusive"; } break;
                case "3 meals daily , all inclusive , breakfast , complimentary snacks , dinner included , lunch included": { Meal = "All Inclusive"; } break;
                case "3 meals daily , all inclusive , breakfast , dinner included , free room upgrade , lunch included": { Meal = "All Inclusive"; } break;
                case "3 meals daily , all inclusive , breakfast , dinner included , lunch included": { Meal = "All Inclusive"; } break;
                case "a la carte ultra all inclusive": { Meal = "All Inclusive"; } break;
                case "ai": { Meal = "All Inclusive"; } break;
                case "airport transfer (two ways) , all inclusive": { Meal = "All Inclusive"; } break;
                case "all": { Meal = "All Inclusive"; } break;
                case "all incl": { Meal = "All Inclusive"; } break;
                case "all incl [non refundable]": { Meal = "All Inclusive"; } break;
                case "all inclusion": { Meal = "All Inclusive"; } break;
                case "all inclusive": { Meal = "All Inclusive"; } break;
                case "all inclusive (ai)": { Meal = "All Inclusive"; } break;
                case "all inclusive , breakfast": { Meal = "All Inclusive"; } break;
                case "all inclusive , breakfast , buffet dinner , complimentary snacks , dinner included , lunch included": { Meal = "All Inclusive"; } break;
                case "all inclusive , breakfast , buffet dinner , dinner included , lunch included": { Meal = "All Inclusive"; } break;
                case "all inclusive , breakfast , complimentary snacks , dinner included , lunch included , snorkeling excursion , sunset cruise": { Meal = "All Inclusive"; } break;
                case "all inclusive , breakfast , dinner included , lunch included": { Meal = "All Inclusive"; } break;
                case "all inclusive , breakfast , dinner included , lunch included , sunset fishing excursion": { Meal = "All Inclusive"; } break;
                case "all inclusive , complimentary snacks , vegetarian breakfast": { Meal = "All Inclusive"; } break;
                case "all inclusive - s class": { Meal = "All Inclusive"; } break;
                case "all inclusive a la carte": { Meal = "All Inclusive"; } break;
                case "all inclusive brands": { Meal = "All Inclusive"; } break;
                case "all inclusive dine around": { Meal = "All Inclusive"; } break;
                case "all inclusive fun": { Meal = "All Inclusive"; } break;
                case "all inclusive gold": { Meal = "All Inclusive"; } break;
                case "all inclusive light": { Meal = "All Inclusive"; } break;
                case "all inclusive limited": { Meal = "All Inclusive"; } break;
                case "all inclusive lite": { Meal = "All Inclusive"; } break;
                case "all inclusive magnum": { Meal = "All Inclusive"; } break;
                case "all inclusive max": { Meal = "All Inclusive"; } break;
                case "all inclusive non refundable": { Meal = "All Inclusive"; } break;
                case "all inclusive plus": { Meal = "All Inclusive"; } break;
                case "all inclusive plus business included": { Meal = "All Inclusive"; } break;
                case "all inclusive plus dine around": { Meal = "All Inclusive"; } break;
                case "all inclusive plus island included": { Meal = "All Inclusive"; } break;
                case "all inclusive premier": { Meal = "All Inclusive"; } break;
                case "all inclusive premium": { Meal = "All Inclusive"; } break;
                case "all inclusive premium (dine around)": { Meal = "All Inclusive"; } break;
                case "all inclusive prestige": { Meal = "All Inclusive"; } break;
                case "all inclusive rise": { Meal = "All Inclusive"; } break;
                case "all inclusive shine": { Meal = "All Inclusive"; } break;
                case "all inclusive special": { Meal = "All Inclusive"; } break;
                case "all inclusive style": { Meal = "All Inclusive"; } break;
                case "all inclusive thalasso": { Meal = "All Inclusive"; } break;
                case "all inclusive unlimited luxury": { Meal = "All Inclusive"; } break;
                case "all inclusive with airport transfer [both ways]": { Meal = "All Inclusive"; } break;
                case "all inclusive without alcohol": { Meal = "All Inclusive"; } break;
                case "all inclusive-nf": { Meal = "All Inclusive"; } break;
                case "all meals , all-inclusive (food/beverages/snacks)": { Meal = "All Inclusive"; } break;
                case "all meals , all-inclusive (food/beverages/snacks) , buffet dinner": { Meal = "All Inclusive"; } break;
                case "all meals , all-inclusive (food/beverages/snacks) , buffet dinner , free lunch": { Meal = "All Inclusive"; } break;
                case "all meals , all-inclusive (food/beverages/snacks) , continental breakfast": { Meal = "All Inclusive"; } break;
                case "all meals , all-inclusive (food/beverages/snacks) , free dinner": { Meal = "All Inclusive"; } break;
                case "all meals , all-inclusive (food/beverages/snacks) , free dinner , free lunch": { Meal = "All Inclusive"; } break;
                case "all meals , all-inclusive (food/beverages/snacks) , free dinner , free lunch , full board": { Meal = "All Inclusive"; } break;
                case "all meals , all-inclusive (food/beverages/snacks) , full board": { Meal = "All Inclusive"; } break;
                case "all meals , all-inclusive (food/beverages/snacks) , vegetarian dinner": { Meal = "All Inclusive"; } break;
                case "all-inclusive": { Meal = "All Inclusive"; } break;
                case "all-inclusive (food/beverages/snacks)": { Meal = "All Inclusive"; } break;
                case "all-inclusive (food/beverages/snacks) , breakfast buffet": { Meal = "All Inclusive"; } break;
                case "all-inclusive (food/beverages/snacks) , breakfast buffet , buffet dinner": { Meal = "All Inclusive"; } break;
                case "all-inclusive (food/beverages/snacks) , breakfast buffet , buffet dinner , full board": { Meal = "All Inclusive"; } break;
                case "all-inclusive (food/beverages/snacks) , breakfast buffet , continental breakfast , full breakfast": { Meal = "All Inclusive"; } break;
                case "all-inclusive (food/beverages/snacks) , breakfast buffet , free dinner , free lunch": { Meal = "All Inclusive"; } break;
                case "all-inclusive (food/beverages/snacks) , breakfast buffet , half board (buffet dinner)": { Meal = "All Inclusive"; } break;
                case "all-inclusive (food/beverages/snacks) , buffet dinner": { Meal = "All Inclusive"; } break;
                case "all-inclusive (food/beverages/snacks) , buffet dinner , free dinner": { Meal = "All Inclusive"; } break;
                case "all-inclusive (food/beverages/snacks) , buffet dinner , free dinner , free lunch": { Meal = "All Inclusive"; } break;
                case "all-inclusive (food/beverages/snacks) , buffet dinner , free lunch": { Meal = "All Inclusive"; } break;
                case "all-inclusive (food/beverages/snacks) , buffet dinner , free lunch , full breakfast": { Meal = "All Inclusive"; } break;
                case "all-inclusive (food/beverages/snacks) , buffet dinner , full board": { Meal = "All Inclusive"; } break;
                case "all-inclusive (food/beverages/snacks) , buffet dinner , half board": { Meal = "All Inclusive"; } break;
                case "all-inclusive (food/beverages/snacks) , food/beverage credit": { Meal = "All Inclusive"; } break;
                case "all-inclusive (food/beverages/snacks) , free dinner": { Meal = "All Inclusive"; } break;
                case "all-inclusive (food/beverages/snacks) , free dinner , free lunch": { Meal = "All Inclusive"; } break;
                case "all-inclusive (food/beverages/snacks) , free dinner , free lunch , full breakfast , vegetarian dinner": { Meal = "All Inclusive"; } break;
                case "all-inclusive (food/beverages/snacks) , free dinner , free lunch , vegetarian dinner": { Meal = "All Inclusive"; } break;
                case "all-inclusive (food/beverages/snacks) , free lunch": { Meal = "All Inclusive"; } break;
                case "all-inclusive (food/beverages/snacks) , full board": { Meal = "All Inclusive"; } break;
                case "all-inclusive (food/beverages/snacks) , half board": { Meal = "All Inclusive"; } break;
                case "all-inclusive (food/beverages/snacks) , half board (buffet dinner)": { Meal = "All Inclusive"; } break;
                case "all-inclusive (food/beverages/snacks) , vegetarian dinner": { Meal = "All Inclusive"; } break;
                case "all_inclusive_all_meal": { Meal = "All Inclusive"; } break;
                case "allsoft all inclusive": { Meal = "All Inclusive"; } break;
                case "buffet all inclusive": { Meal = "All Inclusive"; } break;
                case "classic all inclusive": { Meal = "All Inclusive"; } break;
                case "club plus (room plus all inclusive drinks only)": { Meal = "All Inclusive"; } break;
                case "crystal all inclusive": { Meal = "All Inclusive"; } break;
                case "deluxe all inclusive": { Meal = "All Inclusive"; } break;
                case "extensive all inclusive": { Meal = "All Inclusive"; } break;
                case "extreme all inclusive": { Meal = "All Inclusive"; } break;
                case "gold all inclusive": { Meal = "All Inclusive"; } break;
                case "gourmet bliss all inclusive": { Meal = "All Inclusive"; } break;
                case "grand all inclusive": { Meal = "All Inclusive"; } break;
                case "grand all inclusive dine around": { Meal = "All Inclusive"; } break;
                case "hard all inclusive": { Meal = "All Inclusive"; } break;
                case "high class all inclusive": { Meal = "All Inclusive"; } break;
                case "lux me all inclusive": { Meal = "All Inclusive"; } break;
                case "luxury all inclusive": { Meal = "All Inclusive"; } break;
                case "mirage all inclusive": { Meal = "All Inclusive"; } break;
                case "platinum all inclusive": { Meal = "All Inclusive"; } break;
                case "preferred all inclusive": { Meal = "All Inclusive"; } break;
                case "premium mirage all-inclusive": { Meal = "All Inclusive"; } break;
                case "premium plus all-inclusive": { Meal = "All Inclusive"; } break;
                case "royal all inclusive": { Meal = "All Inclusive"; } break;
                case "select all inclusive": { Meal = "All Inclusive"; } break;
                case "semi all inclusive": { Meal = "All Inclusive"; } break;
                case "ti": { Meal = "All Inclusive"; } break;
                case "ultimate all inclusive": { Meal = "All Inclusive"; } break;
                case "ultra all inclusive ": { Meal = "All Inclusive"; } break;
                case "ultra-all-inclusive": { Meal = "All Inclusive"; } break;
                case "varu plan (all inclusive)": { Meal = "All Inclusive"; } break;
                case "2 meals included , 3 x breakfast": { Meal = "Breakfast Included"; } break;
                case "2 meals included , breakfast": { Meal = "Breakfast Included"; } break;
                case "2-for-1 buffet , breakfast buffet": { Meal = "Breakfast Included"; } break;
                case "2-for-1 buffet , breakfast buffet , free breakfast": { Meal = "Breakfast Included"; } break;
                case "2-for-1 buffet , buffet dinner , free breakfast": { Meal = "Breakfast Included"; } break;
                case "2-for-1 buffet , continental breakfast": { Meal = "Breakfast Included"; } break;
                case "2-for-1 buffet , free breakfast": { Meal = "Breakfast Included"; } break;
                case "airport transfer (one way) , airport transfer [one way] , breakfast , free room upgrade": { Meal = "Breakfast Included"; } break;
                case "airport transfer (one way) , breakfast": { Meal = "Breakfast Included"; } break;
                case "airport transfer (two ways) , airport transfer [both ways] , airport transfer [one way] , breakfast": { Meal = "Breakfast Included"; } break;
                case "airport transfer (two ways) , airport transfer [both ways] , breakfast , complimentary breakfast": { Meal = "Breakfast Included"; } break;
                case "airport transfer (two ways) , breakfast": { Meal = "Breakfast Included"; } break;
                case "airport transfer (two ways) , breakfast , complimentary breakfast , vegetarian breakfast": { Meal = "Breakfast Included"; } break;
                case "airport transfer [both ways] , airport transfer [one way] , breakfast": { Meal = "Breakfast Included"; } break;
                case "airport transfer [both ways] , breakfast": { Meal = "Breakfast Included"; } break;
                case "airport transfer [both ways] , breakfast , breakfast": { Meal = "Breakfast Included"; } break;
                case "airport transfer [both ways] , breakfast , complimentary breakfast": { Meal = "Breakfast Included"; } break;
                case "airport transfer [both ways] , breakfast , complimentary breakfast , complimentary snacks": { Meal = "Breakfast Included"; } break;
                case "airport transfer [both ways] , breakfast , food and beverage credit": { Meal = "Breakfast Included"; } break;
                case "airport transfer [both ways] , breakfast , theme park tickets": { Meal = "Breakfast Included"; } break;
                case "airport transfer [one way] , breakfast": { Meal = "Breakfast Included"; } break;
                case "airport transfer [one way] , breakfast , complimentary breakfast": { Meal = "Breakfast Included"; } break;
                case "airport transfer [one way] , breakfast , complimentary breakfast , food and beverage credit , local breakfast , vegetarian breakfast": { Meal = "Breakfast Included"; } break;
                case "american breakfast": { Meal = "Breakfast Included"; } break;
                case "asian-breakfast": { Meal = "Breakfast Included"; } break;
                case "babf": { Meal = "Breakfast Included"; } break;
                case "bb": { Meal = "Breakfast Included"; } break;
                case "bb [non refundable]": { Meal = "Breakfast Included"; } break;
                case "bed & breakfast": { Meal = "Breakfast Included"; } break;
                case "bed and breakfast": { Meal = "Breakfast Included"; } break;
                case "bed and breakfast (bb)": { Meal = "Breakfast Included"; } break;
                case "bed and breakfast – beverage package": { Meal = "Breakfast Included"; } break;
                case "bed and breakfast continental plus": { Meal = "Breakfast Included"; } break;
                case "bed and breakfast dine around": { Meal = "Breakfast Included"; } break;
                case "bed and breakfast gourmet": { Meal = "Breakfast Included"; } break;
                case "bed and buffet breakfast": { Meal = "Breakfast Included"; } break;
                case "bed and continental breakfast": { Meal = "Breakfast Included"; } break;
                case "bed&breakfast": { Meal = "Breakfast Included"; } break;
                case "bed, breakfast and taxes": { Meal = "Breakfast Included"; } break;
                case "bed, buffet breakfast and taxes": { Meal = "Breakfast Included"; } break;
                case "bf": { Meal = "Breakfast Included"; } break;
                case "breakfast": { Meal = "Breakfast Included"; } break;
                case "breakfast (adults only)": { Meal = "Breakfast Included"; } break;
                case "breakfast , breakfast": { Meal = "Breakfast Included"; } break;
                case "breakfast , breakfast , complimentary breakfast": { Meal = "Breakfast Included"; } break;
                case "breakfast , breakfast , complimentary breakfast , complimentary snacks": { Meal = "Breakfast Included"; } break;
                case "breakfast , breakfast , ferry transfer [both ways] , local island excursion , snorkeling excursion": { Meal = "Breakfast Included"; } break;
                case "breakfast , complimentary breakfast": { Meal = "Breakfast Included"; } break;
                case "breakfast , complimentary breakfast , complimentary snacks": { Meal = "Breakfast Included"; } break;
                case "breakfast , complimentary breakfast , complimentary snacks , local breakfast , vegetarian breakfast": { Meal = "Breakfast Included"; } break;
                case "breakfast , complimentary breakfast , complimentary tickets": { Meal = "Breakfast Included"; } break;
                case "breakfast , complimentary breakfast , food and beverage credit": { Meal = "Breakfast Included"; } break;
                case "breakfast , complimentary breakfast , free room upgrade": { Meal = "Breakfast Included"; } break;
                case "breakfast , complimentary breakfast , free room upgrade , local breakfast": { Meal = "Breakfast Included"; } break;
                case "breakfast , complimentary breakfast , free room upgrade , local breakfast , vegetarian breakfast": { Meal = "Breakfast Included"; } break;
                case "breakfast , complimentary breakfast , local breakfast": { Meal = "Breakfast Included"; } break;
                case "breakfast , complimentary breakfast , local breakfast , vegetarian breakfast": { Meal = "Breakfast Included"; } break;
                case "breakfast , complimentary breakfast , vegetarian breakfast": { Meal = "Breakfast Included"; } break;
                case "breakfast , complimentary breakfast , water park tickets": { Meal = "Breakfast Included"; } break;
                case "breakfast , complimentary snacks": { Meal = "Breakfast Included"; } break;
                case "breakfast , complimentary tickets": { Meal = "Breakfast Included"; } break;
                case "breakfast , ferry transfer [both ways]": { Meal = "Breakfast Included"; } break;
                case "breakfast , ferry transfer [both ways] , snorkeling excursion": { Meal = "Breakfast Included"; } break;
                case "breakfast , food and beverage credit": { Meal = "Breakfast Included"; } break;
                case "breakfast , free room upgrade": { Meal = "Breakfast Included"; } break;
                case "breakfast , free room upgrade , suhur": { Meal = "Breakfast Included"; } break;
                case "breakfast , local breakfast": { Meal = "Breakfast Included"; } break;
                case "breakfast , local breakfast , suhur , vegetarian breakfast": { Meal = "Breakfast Included"; } break;
                case "breakfast , theme park tickets": { Meal = "Breakfast Included"; } break;
                case "breakfast , vegetarian breakfast": { Meal = "Breakfast Included"; } break;
                case "breakfast , water park tickets": { Meal = "Breakfast Included"; } break;
                case "breakfast – nonrefundable": { Meal = "Breakfast Included"; } break;
                case "breakfast buffet": { Meal = "Breakfast Included"; } break;
                case "breakfast buffet , continental breakfast": { Meal = "Breakfast Included"; } break;
                case "breakfast buffet , continental breakfast , english breakfast": { Meal = "Breakfast Included"; } break;
                case "breakfast buffet , continental breakfast , english breakfast , full breakfast": { Meal = "Breakfast Included"; } break;
                case "breakfast buffet , continental breakfast , free breakfast": { Meal = "Breakfast Included"; } break;
                case "breakfast buffet , continental breakfast , free breakfast , full breakfast": { Meal = "Breakfast Included"; } break;
                case "breakfast buffet , continental breakfast , full breakfast": { Meal = "Breakfast Included"; } break;
                case "breakfast buffet , english breakfast": { Meal = "Breakfast Included"; } break;
                case "breakfast buffet , food/beverage credit": { Meal = "Breakfast Included"; } break;
                case "breakfast buffet , food/beverage credit , free breakfast": { Meal = "Breakfast Included"; } break;
                case "breakfast buffet , food/beverage credit , free breakfast , full breakfast": { Meal = "Breakfast Included"; } break;
                case "breakfast buffet , free breakfast": { Meal = "Breakfast Included"; } break;
                case "breakfast buffet , free breakfast , full breakfast": { Meal = "Breakfast Included"; } break;
                case "breakfast buffet , full breakfast": { Meal = "Breakfast Included"; } break;
                case "breakfast buffet , japanese-style breakfast": { Meal = "Breakfast Included"; } break;
                case "breakfast included": { Meal = "Breakfast Included"; } break;
                case "breakfast selections": { Meal = "Breakfast Included"; } break;
                case "breakfast set menu": { Meal = "Breakfast Included"; } break;
                case "breakfast without beverages": { Meal = "Breakfast Included"; } break;
                case "breakfast, parking, bike rental, coffee & tea, free wifi, drinking water, free fitness center access": { Meal = "Breakfast Included"; } break;
                case "breakfast-nf": { Meal = "Breakfast Included"; } break;
                case "bubbly breakfast": { Meal = "Breakfast Included"; } break;
                case "buffet breakfast - non refundable": { Meal = "Breakfast Included"; } break;
                case "buffet breakfast included": { Meal = "Breakfast Included"; } break;
                case "caribbean breakfast": { Meal = "Breakfast Included"; } break;
                case "complimentary breakfast": { Meal = "Breakfast Included"; } break;
                case "complimentary breakfast , free room upgrade": { Meal = "Breakfast Included"; } break;
                case "continental breakfast": { Meal = "Breakfast Included"; } break;
                case "continental breakfast , english breakfast": { Meal = "Breakfast Included"; } break;
                case "continental breakfast , english breakfast , free breakfast": { Meal = "Breakfast Included"; } break;
                case "continental breakfast , english breakfast , full breakfast": { Meal = "Breakfast Included"; } break;
                case "continental breakfast , food/beverage credit": { Meal = "Breakfast Included"; } break;
                case "continental breakfast , food/beverage credit , free breakfast": { Meal = "Breakfast Included"; } break;
                case "continental breakfast , free breakfast": { Meal = "Breakfast Included"; } break;
                case "continental breakfast , free breakfast , full breakfast": { Meal = "Breakfast Included"; } break;
                case "continental breakfast , full breakfast": { Meal = "Breakfast Included"; } break;
                case "continental breakfast , taiwanese-style breakfast": { Meal = "Breakfast Included"; } break;
                case "continental breakfast included": { Meal = "Breakfast Included"; } break;
                case "continental-breakfast": { Meal = "Breakfast Included"; } break;
                case "disney quick service breakfast": { Meal = "Breakfast Included"; } break;
                case "early breakfast (1h earlier)": { Meal = "Breakfast Included"; } break;
                case "english breakfast , food/beverage credit": { Meal = "Breakfast Included"; } break;
                case "english breakfast , free breakfast": { Meal = "Breakfast Included"; } break;
                case "english breakfast , free breakfast , full breakfast": { Meal = "Breakfast Included"; } break;
                case "english breakfast , full breakfast": { Meal = "Breakfast Included"; } break;
                case "food/beverage credit , free breakfast": { Meal = "Breakfast Included"; } break;
                case "food/beverage credit , free breakfast , full breakfast": { Meal = "Breakfast Included"; } break;
                case "food/beverage credit , full breakfast": { Meal = "Breakfast Included"; } break;
                case "free breakfast": { Meal = "Breakfast Included"; } break;
                case "free breakfast , full breakfast": { Meal = "Breakfast Included"; } break;
                case "free breakfast , japanese-style breakfast": { Meal = "Breakfast Included"; } break;
                case "free breakfast , japanese-style breakfast , taiwanese-style breakfast": { Meal = "Breakfast Included"; } break;
                case "free breakfast , taiwanese-style breakfast": { Meal = "Breakfast Included"; } break;
                case "full breakfast": { Meal = "Breakfast Included"; } break;
                case "full breakfast , japanese-style breakfast": { Meal = "Breakfast Included"; } break;
                case "gourmet american breakfast": { Meal = "Breakfast Included"; } break;
                case "grab & go breakfast": { Meal = "Breakfast Included"; } break;
                case "have breakfast": { Meal = "Breakfast Included"; } break;
                case "hot breakfast": { Meal = "Breakfast Included"; } break;
                case "hot buffet breakfast": { Meal = "Breakfast Included"; } break;
                case "japanese-style breakfast": { Meal = "Breakfast Included"; } break;
                case "light breakfast": { Meal = "Breakfast Included"; } break;
                case "local breakfast": { Meal = "Breakfast Included"; } break;
                case "rezlive dynamic rate bar with breakfast": { Meal = "Breakfast Included"; } break;
                case "room and breakfast": { Meal = "Breakfast Included"; } break;
                case "scandinavian-breakfast": { Meal = "Breakfast Included"; } break;
                case "scottish breakfast": { Meal = "Breakfast Included"; } break;
                case "scottish-breakfast": { Meal = "Breakfast Included"; } break;
                case "semi-buffet breakfast": { Meal = "Breakfast Included"; } break;
                case "taiwanese-style breakfast": { Meal = "Breakfast Included"; } break;
                case "vegetarian breakfast": { Meal = "Breakfast Included"; } break;
                case "vip only - free breakfast": { Meal = "Breakfast Included"; } break;
                case "with breakfast": { Meal = "Breakfast Included"; } break;
                case "breakfast-buffet": { Meal = "Buffet Breakfast"; } break;
                case "buffet breakfast": { Meal = "Buffet Breakfast"; } break;
                case "buffet breakfast, no beverages": { Meal = "Buffet Breakfast"; } break;
                case "complimentary buffet breakfast": { Meal = "Buffet Breakfast"; } break;
                case "continental breakfast - non refundable": { Meal = "Buffet Breakfast"; } break;
                case "breakfast continental": { Meal = "Continental Breakfast"; } break;
                case "con": { Meal = "Continental Breakfast"; } break;
                case "continental breakfast adults only": { Meal = "Continental Breakfast"; } break;
                case "continental breakfast buffet": { Meal = "Continental Breakfast"; } break;
                case "continental breakfast without beverages": { Meal = "Continental Breakfast"; } break;
                case "chinese-breakfast": { Meal = "Chinese Breakfast"; } break;
                case "bed and english breakfast": { Meal = "English Breakfast"; } break;
                case "bed and english breakfast (excluding drinks)": { Meal = "English Breakfast"; } break;
                case "breakfast english": { Meal = "English Breakfast"; } break;
                case "english breakfast": { Meal = "English Breakfast"; } break;
                case "english-breakfast": { Meal = "English Breakfast"; } break;
                case "3 meals daily": { Meal = "Full Board"; } break;
                case "3 meals daily , breakfast , buffet dinner , complimentary breakfast , dinner included , lunch included": { Meal = "Full Board"; } break;
                case "3 meals daily , breakfast , buffet dinner , dinner included , lunch included": { Meal = "Full Board"; } break;
                case "3 meals daily , breakfast , dinner included , lunch included": { Meal = "Full Board"; } break;
                case "airport transfer [both ways] , breakfast , dinner included , lunch included": { Meal = "Full Board"; } break;
                case "all meals": { Meal = "Full Board"; } break;
                case "all meals , breakfast buffet , buffet dinner": { Meal = "Full Board"; } break;
                case "all meals , breakfast buffet , free breakfast , free dinner , free lunch": { Meal = "Full Board"; } break;
                case "all meals , breakfast buffet , full board": { Meal = "Full Board"; } break;
                case "all meals , buffet dinner , free breakfast": { Meal = "Full Board"; } break;
                case "all meals , buffet dinner , free breakfast , free dinner , free lunch , full board": { Meal = "Full Board"; } break;
                case "all meals , buffet dinner , free breakfast , free lunch": { Meal = "Full Board"; } break;
                case "all meals , buffet dinner , free breakfast , free lunch , vegetarian dinner": { Meal = "Full Board"; } break;
                case "all meals , buffet dinner , free breakfast , full board": { Meal = "Full Board"; } break;
                case "all meals , buffet dinner , free breakfast , half board": { Meal = "Full Board"; } break;
                case "all meals , buffet dinner , free breakfast , vegetarian dinner": { Meal = "Full Board"; } break;
                case "all meals , continental breakfast , food/beverage credit": { Meal = "Full Board"; } break;
                case "all meals , continental breakfast , free breakfast , free dinner , free lunch": { Meal = "Full Board"; } break;
                case "all meals , food/beverage credit": { Meal = "Full Board"; } break;
                case "all meals , food/beverage credit , free breakfast , free dinner , free lunch": { Meal = "Full Board"; } break;
                case "all meals , food/beverage credit , free dinner , free lunch": { Meal = "Full Board"; } break;
                case "all meals , free breakfast": { Meal = "Full Board"; } break;
                case "all meals , free breakfast , free dinner , free lunch": { Meal = "Full Board"; } break;
                case "all meals , free breakfast , free dinner , free lunch , full board": { Meal = "Full Board"; } break;
                case "all meals , free breakfast , free dinner , free lunch , half board": { Meal = "Full Board"; } break;
                case "all meals , free breakfast , full board": { Meal = "Full Board"; } break;
                case "all meals , free breakfast , full breakfast": { Meal = "Full Board"; } break;
                case "all meals , free breakfast , japanese-style breakfast": { Meal = "Full Board"; } break;
                case "all meals , free breakfast , vegetarian dinner": { Meal = "Full Board"; } break;
                case "all meals , free dinner": { Meal = "Full Board"; } break;
                case "all meals , free lunch , full board , vegetarian dinner": { Meal = "Full Board"; } break;
                case "all meals , full board": { Meal = "Full Board"; } break;
                case "breakfast , buffet dinner , dinner included , lunch included": { Meal = "Full Board"; } break;
                case "breakfast , complimentary breakfast , complimentary dinner , complimentary lunch , complimentary tickets , cruise dinner , dinner included , local breakfast , local island excursion , lunch included , vegetarian breakfast": { Meal = "Full Board"; } break;
                case "breakfast , complimentary breakfast , complimentary dinner , complimentary lunch , cruise dinner , dinner included , local breakfast , lunch included , vegetarian breakfast , vegetarian dinner , western dinner": { Meal = "Full Board"; } break;
                case "breakfast , complimentary breakfast , complimentary dinner , complimentary lunch , dinner included , lunch included": { Meal = "Full Board"; } break;
                case "breakfast , complimentary breakfast , complimentary dinner , dinner included , lunch included": { Meal = "Full Board"; } break;
                case "breakfast , complimentary breakfast , dinner included , lunch included": { Meal = "Full Board"; } break;
                case "breakfast , dinner included , lunch included": { Meal = "Full Board"; } break;
                case "breakfast buffet , buffet dinner , free breakfast , free lunch": { Meal = "Full Board"; } break;
                case "breakfast buffet , buffet dinner , free lunch": { Meal = "Full Board"; } break;
                case "breakfast buffet , free breakfast , free dinner , free lunch": { Meal = "Full Board"; } break;
                case "breakfast buffet , free breakfast , full board": { Meal = "Full Board"; } break;
                case "breakfast buffet , free breakfast , full board , full breakfast": { Meal = "Full Board"; } break;
                case "breakfast buffet , free dinner , free lunch": { Meal = "Full Board"; } break;
                case "breakfast buffet , full board": { Meal = "Full Board"; } break;
                case "breakfast, lunch & dinner included": { Meal = "Full Board"; } break;
                case "breakfast, lunch & dinner included - non refundable": { Meal = "Full Board"; } break;
                case "breakfast, lunch & dinner, no drinks": { Meal = "Full Board"; } break;
                case "breakfast, lunch and dinner included": { Meal = "Full Board"; } break;
                case "breakfast, lunch, and dinner available only for adults": { Meal = "Full Board"; } break;
                case "breakfast, lunch, dinner with liquids": { Meal = "Full Board"; } break;
                case "buffet dinner , chinese-style dinner , free breakfast , full board , vegetarian dinner": { Meal = "Full Board"; } break;
                case "buffet dinner , continental breakfast , free lunch": { Meal = "Full Board"; } break;
                case "buffet dinner , free breakfast , free dinner , free lunch": { Meal = "Full Board"; } break;
                case "buffet dinner , free breakfast , free lunch": { Meal = "Full Board"; } break;
                case "buffet dinner , free breakfast , free lunch , full board": { Meal = "Full Board"; } break;
                case "buffet dinner , free breakfast , full board": { Meal = "Full Board"; } break;
                case "continental breakfast , free breakfast , full board": { Meal = "Full Board"; } break;
                case "continental breakfast , free dinner , free lunch": { Meal = "Full Board"; } break;
                case "continental breakfast , full board": { Meal = "Full Board"; } break;
                case "disney full board extra plus": { Meal = "Full Board"; } break;
                case "disney full board plus": { Meal = "Full Board"; } break;
                case "disney full board premium": { Meal = "Full Board"; } break;
                case "disney full board standard": { Meal = "Full Board"; } break;
                case "english breakfast , free dinner , free lunch": { Meal = "Full Board"; } break;
                case "english breakfast , full board": { Meal = "Full Board"; } break;
                case "fb": { Meal = "Full Board"; } break;
                case "fb [non refundable]": { Meal = "Full Board"; } break;
                case "food/beverage credit , free breakfast , free dinner , free lunch": { Meal = "Full Board"; } break;
                case "food/beverage credit , full board": { Meal = "Full Board"; } break;
                case "free breakfast , free dinner , free lunch": { Meal = "Full Board"; } break;
                case "free breakfast , free dinner , free lunch , full board": { Meal = "Full Board"; } break;
                case "free breakfast , free dinner , free lunch , half board (buffet dinner)": { Meal = "Full Board"; } break;
                case "free breakfast , free dinner , free lunch , vegetarian dinner": { Meal = "Full Board"; } break;
                case "free breakfast , free dinner , full board": { Meal = "Full Board"; } break;
                case "free breakfast , free lunch , kaiseki dinner": { Meal = "Full Board"; } break;
                case "free breakfast , full board": { Meal = "Full Board"; } break;
                case "free breakfast , full board , half board": { Meal = "Full Board"; } break;
                case "free breakfast , full board , half board (buffet dinner)": { Meal = "Full Board"; } break;
                case "free breakfast , full board , vegetarian dinner": { Meal = "Full Board"; } break;
                case "free dinner , free lunch , full board": { Meal = "Full Board"; } break;
                case "free dinner , free lunch , full board , full breakfast": { Meal = "Full Board"; } break;
                case "free dinner , free lunch , full breakfast": { Meal = "Full Board"; } break;
                case "free dinner , full board": { Meal = "Full Board"; } break;
                case "full board": { Meal = "Full Board"; } break;
                case "full board (drinks with meals)": { Meal = "Full Board"; } break;
                case "full board (fb)": { Meal = "Full Board"; } break;
                case "full board +": { Meal = "Full Board"; } break;
                case "full board , full breakfast": { Meal = "Full Board"; } break;
                case "full board , half board , half board (kaiseki dinner)": { Meal = "Full Board"; } break;
                case "full board , vegetarian dinner": { Meal = "Full Board"; } break;
                case "full board – beverage package": { Meal = "Full Board"; } break;
                case "full board beverages included": { Meal = "Full Board"; } break;
                case "full board club plus": { Meal = "Full Board"; } break;
                case "full board deluxe": { Meal = "Full Board"; } break;
                case "full board dine around": { Meal = "Full Board"; } break;
                case "full board dine around plus": { Meal = "Full Board"; } break;
                case "full board dine-around premium": { Meal = "Full Board"; } break;
                case "full board gold": { Meal = "Full Board"; } break;
                case "full board including special drinks": { Meal = "Full Board"; } break;
                case "full board plus": { Meal = "Full Board"; } break;
                case "full board premium": { Meal = "Full Board"; } break;
                case "full board with drinks": { Meal = "Full Board"; } break;
                case "full board-nf": { Meal = "Full Board"; } break;
                case "full-board": { Meal = "Full Board"; } break;
                case "full_board": { Meal = "Full Board"; } break;
                case "fulll board": { Meal = "Full Board"; } break;
                case "ultra full board": { Meal = "Full Board"; } break;
                case "2 meals included": { Meal = "Half Board"; } break;
                case "2 meals included , 3 x complimentary snacks , breakfast , dinner included": { Meal = "Half Board"; } break;
                case "2 meals included , airport transfer (two ways) , breakfast , dinner included": { Meal = "Half Board"; } break;
                case "2 meals included , breakfast , buffet dinner , complimentary breakfast , complimentary snacks , dinner included": { Meal = "Half Board"; } break;
                case "2 meals included , breakfast , buffet dinner , dinner included , local breakfast": { Meal = "Half Board"; } break;
                case "2 meals included , breakfast , complimentary breakfast , complimentary lunch , local breakfast , lunch included , vegetarian breakfast": { Meal = "Half Board"; } break;
                case "2 meals included , breakfast , complimentary dinner": { Meal = "Half Board"; } break;
                case "2 meals included , breakfast , complimentary dinner , complimentary snacks , dinner included": { Meal = "Half Board"; } break;
                case "2 meals included , breakfast , complimentary dinner , dinner included": { Meal = "Half Board"; } break;
                case "2 meals included , breakfast , complimentary dinner , dinner included , kaiseki dinner": { Meal = "Half Board"; } break;
                case "2 meals included , breakfast , complimentary dinner , dinner included , kaiseki dinner , wagyu beef dinner": { Meal = "Half Board"; } break;
                case "2 meals included , breakfast , complimentary snacks , dinner included": { Meal = "Half Board"; } break;
                case "2 meals included , breakfast , complimentary snacks , dinner included , wagyu beef dinner": { Meal = "Half Board"; } break;
                case "2 meals included , breakfast , dinner in private roo , dinner included , japanese dinner , kaiseki dinner": { Meal = "Half Board"; } break;
                case "2 meals included , breakfast , dinner included": { Meal = "Half Board"; } break;
                case "2 meals included , breakfast , dinner included , japanese dinner": { Meal = "Half Board"; } break;
                case "2 meals included , breakfast , dinner included , japanese dinner , kaiseki dinner , wagyu beef dinner": { Meal = "Half Board"; } break;
                case "2 meals included , breakfast , japanese dinner": { Meal = "Half Board"; } break;
                case "2 meals included , breakfast , kaiseki dinner": { Meal = "Half Board"; } break;
                case "2 meals included , breakfast , lunch included": { Meal = "Half Board"; } break;
                case "airport transfer (two ways) , breakfast , dinner included": { Meal = "Half Board"; } break;
                case "airport transfer (two ways) , breakfast , dinner included , lunch included": { Meal = "Half Board"; } break;
                case "airport transfer [both ways] , breakfast , breakfast , dinner included , dinner included": { Meal = "Half Board"; } break;
                case "airport transfer [both ways] , breakfast , buffet dinner , complimentary breakfast , complimentary snacks , dinner included": { Meal = "Half Board"; } break;
                case "airport transfer [both ways] , breakfast , buffet dinner , complimentary breakfast , dinner included": { Meal = "Half Board"; } break;
                case "airport transfer [both ways] , breakfast , dinner included": { Meal = "Half Board"; } break;
                case "airport transfer [one way] , breakfast , dinner included": { Meal = "Half Board"; } break;
                case "b&b + hb": { Meal = "Half Board"; } break;
                case "basic kaiseki dinner , breakfast , dinner included , japanese dinner , local breakfast": { Meal = "Half Board"; } break;
                case "bed, buffet breakfast and light dinner": { Meal = "Half Board"; } break;
                case "breakfast & dinner included": { Meal = "Half Board"; } break;
                case "breakfast & lunch": { Meal = "Half Board"; } break;
                case "breakfast + lunch / dinner": { Meal = "Half Board"; } break;
                case "breakfast , buffet dinner": { Meal = "Half Board"; } break;
                case "breakfast , buffet dinner , complimentary breakfast , dinner included": { Meal = "Half Board"; } break;
                case "breakfast , buffet dinner , dinner included": { Meal = "Half Board"; } break;
                case "breakfast , buffet dinner , dinner included , local breakfast": { Meal = "Half Board"; } break;
                case "breakfast , complimentary breakfast , complimentary dinner": { Meal = "Half Board"; } break;
                case "breakfast , complimentary breakfast , complimentary dinner , dinner included": { Meal = "Half Board"; } break;
                case "breakfast , complimentary breakfast , complimentary lunch , local breakfast , lunch included": { Meal = "Half Board"; } break;
                case "breakfast , complimentary breakfast , complimentary lunch , local breakfast , lunch included , vegetarian breakfast": { Meal = "Half Board"; } break;
                case "breakfast , complimentary breakfast , dinner included": { Meal = "Half Board"; } break;
                case "breakfast , complimentary dinner": { Meal = "Half Board"; } break;
                case "breakfast , complimentary dinner , dinner included , kaiseki dinner , wagyu beef dinner": { Meal = "Half Board"; } break;
                case "breakfast , complimentary dinner , japanese dinner": { Meal = "Half Board"; } break;
                case "breakfast , dinner included": { Meal = "Half Board"; } break;
                case "breakfast , dinner included , japanese dinner": { Meal = "Half Board"; } break;
                case "breakfast , dinner included , japanese dinner , kaiseki dinner": { Meal = "Half Board"; } break;
                case "breakfast , dinner included , japanese dinner , local breakfast": { Meal = "Half Board"; } break;
                case "breakfast , dinner included , kaiseki dinner": { Meal = "Half Board"; } break;
                case "breakfast , dinner included , kaiseki dinner , local breakfast": { Meal = "Half Board"; } break;
                case "breakfast , dinner included , local breakfast": { Meal = "Half Board"; } break;
                case "breakfast , dinner included , wagyu beef dinner": { Meal = "Half Board"; } break;
                case "breakfast , dinner included , wagyu beef dinner , western dinner": { Meal = "Half Board"; } break;
                case "breakfast , dinner included , western dinner": { Meal = "Half Board"; } break;
                case "breakfast , in-room dinner , kaiseki dinner": { Meal = "Half Board"; } break;
                case "breakfast , japanese dinner": { Meal = "Half Board"; } break;
                case "breakfast , lunch included": { Meal = "Half Board"; } break;
                case "breakfast and dinner": { Meal = "Half Board"; } break;
                case "breakfast and lunch": { Meal = "Half Board"; } break;
                case "breakfast and lunch or dinner included": { Meal = "Half Board"; } break;
                case "breakfast and lunch or dinner, beverages not included": { Meal = "Half Board"; } break;
                case "breakfast buffet , buffet dinner": { Meal = "Half Board"; } break;
                case "breakfast buffet , buffet dinner , free breakfast": { Meal = "Half Board"; } break;
                case "breakfast buffet , buffet dinner , free breakfast , free dinner": { Meal = "Half Board"; } break;
                case "breakfast buffet , buffet dinner , half board": { Meal = "Half Board"; } break;
                case "breakfast buffet , buffet dinner , half board (buffet dinner)": { Meal = "Half Board"; } break;
                case "breakfast buffet , buffet dinner , vegetarian dinner": { Meal = "Half Board"; } break;
                case "breakfast buffet , continental breakfast , english breakfast , free dinner": { Meal = "Half Board"; } break;
                case "breakfast buffet , continental breakfast , free dinner": { Meal = "Half Board"; } break;
                case "breakfast buffet , food/beverage credit , free breakfast , half board": { Meal = "Half Board"; } break;
                case "breakfast buffet , food/beverage credit , free dinner": { Meal = "Half Board"; } break;
                case "breakfast buffet , free breakfast , free dinner": { Meal = "Half Board"; } break;
                case "breakfast buffet , free breakfast , free dinner , full breakfast": { Meal = "Half Board"; } break;
                case "breakfast buffet , free breakfast , free dinner , half board": { Meal = "Half Board"; } break;
                case "breakfast buffet , free breakfast , free dinner , half board (buffet dinner)": { Meal = "Half Board"; } break;
                case "breakfast buffet , free breakfast , free lunch": { Meal = "Half Board"; } break;
                case "breakfast buffet , free breakfast , half board": { Meal = "Half Board"; } break;
                case "breakfast buffet , free dinner": { Meal = "Half Board"; } break;
                case "breakfast buffet , free dinner , half board": { Meal = "Half Board"; } break;
                case "breakfast buffet , free dinner , half board (buffet dinner)": { Meal = "Half Board"; } break;
                case "breakfast buffet , free lunch": { Meal = "Half Board"; } break;
                case "breakfast buffet , half board": { Meal = "Half Board"; } break;
                case "breakfast buffet , half board (buffet dinner)": { Meal = "Half Board"; } break;
                case "breakfast buffet , half board (kaiseki dinner)": { Meal = "Half Board"; } break;
                case "breakfast buffet , half board , kaiseki dinner": { Meal = "Half Board"; } break;
                case "breakfast buffet , kaiseki dinner": { Meal = "Half Board"; } break;
                case "breakfast buffet , kaiseki meat dinner": { Meal = "Half Board"; } break;
                case "breakfast, lunch or dinner included": { Meal = "Half Board"; } break;
                case "breakfast, lunch or dinner included, beverages not included": { Meal = "Half Board"; } break;
                case "breakfast, snack & dinner included": { Meal = "Half Board"; } break;
                case "buffet dinner , continental breakfast": { Meal = "Half Board"; } break;
                case "buffet dinner , english breakfast": { Meal = "Half Board"; } break;
                case "buffet dinner , free breakfast": { Meal = "Half Board"; } break;
                case "buffet dinner , free breakfast , free dinner": { Meal = "Half Board"; } break;
                case "buffet dinner , free breakfast , free dinner , half board (buffet dinner)": { Meal = "Half Board"; } break;
                case "buffet dinner , free breakfast , free dinner , half board , half board (buffet dinner) , vegetarian dinner": { Meal = "Half Board"; } break;
                case "buffet dinner , free breakfast , half board": { Meal = "Half Board"; } break;
                case "buffet dinner , free breakfast , half board (buffet dinner)": { Meal = "Half Board"; } break;
                case "buffet dinner , free breakfast , half board (buffet dinner) , vegetarian dinner": { Meal = "Half Board"; } break;
                case "buffet dinner , free breakfast , half board , half board (buffet dinner)": { Meal = "Half Board"; } break;
                case "buffet dinner , free breakfast , half board , half board (buffet dinner) , vegetarian dinner": { Meal = "Half Board"; } break;
                case "buffet dinner , free breakfast , vegetarian dinner": { Meal = "Half Board"; } break;
                case "buffet dinner , free lunch": { Meal = "Half Board"; } break;
                case "buffet dinner , full breakfast": { Meal = "Half Board"; } break;
                case "buffet dinner , half board": { Meal = "Half Board"; } break;
                case "buffet halfboard": { Meal = "Half Board"; } break;
                case "chinese-style dinner , free breakfast": { Meal = "Half Board"; } break;
                case "complimentary breakfast , complimentary dinner": { Meal = "Half Board"; } break;
                case "continental breakfast , free breakfast , free dinner": { Meal = "Half Board"; } break;
                case "continental breakfast , free breakfast , half board (buffet dinner)": { Meal = "Half Board"; } break;
                case "continental breakfast , free dinner": { Meal = "Half Board"; } break;
                case "continental breakfast , free lunch": { Meal = "Half Board"; } break;
                case "continental breakfast , half board": { Meal = "Half Board"; } break;
                case "continental breakfast , half board (buffet dinner)": { Meal = "Half Board"; } break;
                case "continental breakfast , japanese-style breakfast , kaiseki dinner": { Meal = "Half Board"; } break;
                case "dinner bed and breakfast plan": { Meal = "Half Board"; } break;
                case "disney half board plus": { Meal = "Half Board"; } break;
                case "disney half board premium": { Meal = "Half Board"; } break;
                case "disney half board standard": { Meal = "Half Board"; } break;
                case "english breakfast , free dinner": { Meal = "Half Board"; } break;
                case "english breakfast , free dinner , half board": { Meal = "Half Board"; } break;
                case "english breakfast , half board": { Meal = "Half Board"; } break;
                case "food/beverage credit , free breakfast , free dinner": { Meal = "Half Board"; } break;
                case "food/beverage credit , half board": { Meal = "Half Board"; } break;
                case "food/beverage credit , half board (buffet dinner)": { Meal = "Half Board"; } break;
                case "free breakfast , free dinner": { Meal = "Half Board"; } break;
                case "free breakfast , free dinner , full breakfast": { Meal = "Half Board"; } break;
                case "free breakfast , free dinner , half board": { Meal = "Half Board"; } break;
                case "free breakfast , free dinner , half board (buffet dinner)": { Meal = "Half Board"; } break;
                case "free breakfast , free dinner , half board (buffet dinner) , vegetarian dinner": { Meal = "Half Board"; } break;
                case "free breakfast , free dinner , half board (kaiseki dinner)": { Meal = "Half Board"; } break;
                case "free breakfast , free dinner , vegetarian dinner": { Meal = "Half Board"; } break;
                case "free breakfast , free lunch": { Meal = "Half Board"; } break;
                case "free breakfast , free lunch , half board": { Meal = "Half Board"; } break;
                case "free breakfast , free lunch , half board (buffet dinner)": { Meal = "Half Board"; } break;
                case "free breakfast , full breakfast , half board": { Meal = "Half Board"; } break;
                case "free breakfast , half board": { Meal = "Half Board"; } break;
                case "free breakfast , half board (buffet dinner)": { Meal = "Half Board"; } break;
                case "free breakfast , half board (buffet dinner) , vegetarian dinner": { Meal = "Half Board"; } break;
                case "free breakfast , half board (chinese-style dinner)": { Meal = "Half Board"; } break;
                case "free breakfast , half board (kaiseki dinner)": { Meal = "Half Board"; } break;
                case "free breakfast , half board , half board (buffet dinner)": { Meal = "Half Board"; } break;
                case "free breakfast , half board , half board (kaiseki dinner)": { Meal = "Half Board"; } break;
                case "free breakfast , half board , half board (kaiseki dinner) , japanese-style breakfast": { Meal = "Half Board"; } break;
                case "free breakfast , half board , japanese-style breakfast , kaiseki dinner": { Meal = "Half Board"; } break;
                case "free breakfast , half board , kaiseki dinner": { Meal = "Half Board"; } break;
                case "free breakfast , kaiseki dinner": { Meal = "Half Board"; } break;
                case "free breakfast , kaiseki meat dinner": { Meal = "Half Board"; } break;
                case "free breakfast , kaiseki seafood dinner": { Meal = "Half Board"; } break;
                case "free breakfast , kaiseki vegetarian dinner": { Meal = "Half Board"; } break;
                case "free breakfast , vegetarian dinner": { Meal = "Half Board"; } break;
                case "free dinner , free lunch": { Meal = "Half Board"; } break;
                case "free dinner , full breakfast": { Meal = "Half Board"; } break;
                case "free dinner , full breakfast , half board": { Meal = "Half Board"; } break;
                case "free dinner , half board": { Meal = "Half Board"; } break;
                case "free dinner , half board (buffet dinner)": { Meal = "Half Board"; } break;
                case "free dinner , japanese-style breakfast": { Meal = "Half Board"; } break;
                case "free lunch , full breakfast": { Meal = "Half Board"; } break;
                case "free lunch , half board": { Meal = "Half Board"; } break;
                case "full breakfast , half board": { Meal = "Half Board"; } break;
                case "full breakfast , half board (buffet dinner)": { Meal = "Half Board"; } break;
                case "full breakfast , vegetarian dinner": { Meal = "Half Board"; } break;
                case "full breakfast and 1st night dinner": { Meal = "Half Board"; } break;
                case "half  board (hb)": { Meal = "Half Board"; } break;
                case "half board": { Meal = "Half Board"; } break;
                case "half board (breakfast and dinner)": { Meal = "Half Board"; } break;
                case "half board (breakfast and lunch)": { Meal = "Half Board"; } break;
                case "half board (brunch and dinner)": { Meal = "Half Board"; } break;
                case "half board (buffet dinner)": { Meal = "Half Board"; } break;
                case "half board (buffet dinner) , japanese-style breakfast": { Meal = "Half Board"; } break;
                case "half board (chinese-style dinner)": { Meal = "Half Board"; } break;
                case "half board (drinks with meals)": { Meal = "Half Board"; } break;
                case "half board (kaiseki dinner)": { Meal = "Half Board"; } break;
                case "half board (kaiseki dinner) , japanese-style breakfast": { Meal = "Half Board"; } break;
                case "half board (kaiseki dinner) , japanese-style breakfast , kaiseki dinner": { Meal = "Half Board"; } break;
                case "half board (kaiseki dinner) , japanese-style breakfast , kaiseki seafood dinner": { Meal = "Half Board"; } break;
                case "half board (kaiseki dinner) , kaiseki meat dinner": { Meal = "Half Board"; } break;
                case "half board (kaiseki dinner) , kaiseki seafood dinner": { Meal = "Half Board"; } break;
                case "half board , half board (buffet dinner)": { Meal = "Half Board"; } break;
                case "half board , half board (kaiseki dinner)": { Meal = "Half Board"; } break;
                case "half board , half board (kaiseki dinner) , japanese-style breakfast": { Meal = "Half Board"; } break;
                case "half board , half board (kaiseki dinner) , japanese-style breakfast , kaiseki meat dinner": { Meal = "Half Board"; } break;
                case "half board , japanese-style breakfast": { Meal = "Half Board"; } break;
                case "half board , japanese-style breakfast , kaiseki dinner": { Meal = "Half Board"; } break;
                case "half board , japanese-style breakfast , kaiseki meat dinner": { Meal = "Half Board"; } break;
                case "half board , japanese-style breakfast , kaiseki seafood dinner": { Meal = "Half Board"; } break;
                case "half board , japanese-style breakfast , vegetarian dinner": { Meal = "Half Board"; } break;
                case "half board , kaiseki dinner": { Meal = "Half Board"; } break;
                case "half board , kaiseki meat dinner": { Meal = "Half Board"; } break;
                case "half board , kaiseki seafood dinner": { Meal = "Half Board"; } break;
                case "half board , vegetarian dinner": { Meal = "Half Board"; } break;
                case "half board - special menu": { Meal = "Half Board"; } break;
                case "half board – beverage package": { Meal = "Half Board"; } break;
                case "half board a la carte": { Meal = "Half Board"; } break;
                case "half board club plus": { Meal = "Half Board"; } break;
                case "half board deluxe": { Meal = "Half Board"; } break;
                case "half board dine around": { Meal = "Half Board"; } break;
                case "half board dine around plus": { Meal = "Half Board"; } break;
                case "half board dine-around premium": { Meal = "Half Board"; } break;
                case "half board flexible": { Meal = "Half Board"; } break;
                case "half board gold": { Meal = "Half Board"; } break;
                case "half board including special drinks": { Meal = "Half Board"; } break;
                case "half board lunch": { Meal = "Half Board"; } break;
                case "half board plus": { Meal = "Half Board"; } break;
                case "half board plus (drinks with meals)": { Meal = "Half Board"; } break;
                case "half board premium": { Meal = "Half Board"; } break;
                case "half board silver (drinks included)": { Meal = "Half Board"; } break;
                case "half board with beverages included": { Meal = "Half Board"; } break;
                case "half board with drinks": { Meal = "Half Board"; } break;
                case "half board-nf": { Meal = "Half Board"; } break;
                case "half-board": { Meal = "Half Board"; } break;
                case "half-board-dinner": { Meal = "Half Board"; } break;
                case "half-board-lunch": { Meal = "Half Board"; } break;
                case "half_board": { Meal = "Half Board"; } break;
                case "hb": { Meal = "Half Board"; } break;
                case "hb [non refundable]": { Meal = "Half Board"; } break;
                case "hc": { Meal = "Half Board"; } break;
                case "japanese-style breakfast , kaiseki dinner": { Meal = "Half Board"; } break;
                case "japanese-style breakfast , kaiseki meat dinner": { Meal = "Half Board"; } break;
                case "japanese-style breakfast , kaiseki seafood dinner": { Meal = "Half Board"; } break;
                case "lunch and dinner (ld)": { Meal = "Half Board"; } break;
                case "premium half board": { Meal = "Half Board"; } break;
                case "taiwanese-style breakfast , vegetarian dinner": { Meal = "Half Board"; } break;
                case "irish breakfast": { Meal = "Irish Breakfast"; } break;
                case "irish-breakfast": { Meal = "Irish Breakfast"; } break;
                case "israeli-breakfast": { Meal = "Israeli Breakfast"; } break;
                case "japanese-breakfast": { Meal = "Japanese Breakfast"; } break;
                case "breakfast_lunch": { Meal = "Breakfast and Lunch"; } break;
                case "breakfast & dinner with beberages": { Meal = "Breakfast and Dinner"; } break;
                case "breakfast & dinner, no drinks": { Meal = "Breakfast and Dinner"; } break;
                case "iftar & sohour": { Meal = "Iftar And Suhoor"; } break;
                case "iftar and suhoor": { Meal = "Iftar And Suhoor"; } break;
                case "iftar and suhoor-nf": { Meal = "Iftar And Suhoor"; } break;
                case "sohour and iftar": { Meal = "Iftar And Suhoor"; } break;
                case "suhur and iftar": { Meal = "Iftar And Suhoor"; } break;
                case "1 x breakfast": { Meal = "Breakfast For One"; } break;
                case "1 x breakfast , 1 x complimentary breakfast": { Meal = "Breakfast For One"; } break;
                case "1 x breakfast , 1 x complimentary breakfast , 1 x complimentary snacks , 1 x local breakfast , 1 x vegetarian breakfast , breakfast , breakfast , breakfast for 1 person , breakfast for 2 people": { Meal = "Breakfast For One"; } break;
                case "1 x breakfast , 1 x complimentary breakfast , 1 x complimentary tickets": { Meal = "Breakfast For One"; } break;
                case "1 x breakfast , 1 x complimentary breakfast , 1 x local breakfast": { Meal = "Breakfast For One"; } break;
                case "1 x breakfast , 1 x complimentary breakfast , 1 x local breakfast , 1 x vegetarian breakfast": { Meal = "Breakfast For One"; } break;
                case "1 x breakfast , 1 x complimentary breakfast , 1 x local breakfast , 1 x vegetarian breakfast , breakfast for 1 person , breakfast for 2 people": { Meal = "Breakfast For One"; } break;
                case "1 x breakfast , 1 x complimentary breakfast , airport transfer (two ways) , airport transfer [both ways]": { Meal = "Breakfast For One"; } break;
                case "1 x breakfast , 1 x complimentary breakfast , airport transfer (two ways) , airport transfer [both ways] , breakfast for 2 people": { Meal = "Breakfast For One"; } break;
                case "1 x breakfast , 1 x complimentary breakfast , breakfast for 2 people": { Meal = "Breakfast For One"; } break;
                case "1 x breakfast , 1 x complimentary snacks": { Meal = "Breakfast For One"; } break;
                case "1 x breakfast , 1 x complimentary tickets": { Meal = "Breakfast For One"; } break;
                case "1 x breakfast , 1 x dinner included , 1 x food and beverage credit , breakfast for 2 people": { Meal = "Breakfast For One"; } break;
                case "1 x breakfast , 1 x food and beverage credit": { Meal = "Breakfast For One"; } break;
                case "1 x breakfast , 1 x food and beverage credit , airport transfer [both ways]": { Meal = "Breakfast For One"; } break;
                case "1 x breakfast , 1 x local breakfast": { Meal = "Breakfast For One"; } break;
                case "1 x breakfast , 1 x local breakfast , 1 x vegetarian breakfast , breakfast for 2 people": { Meal = "Breakfast For One"; } break;
                case "1 x breakfast , 1 x local breakfast , breakfast for 2 people": { Meal = "Breakfast For One"; } break;
                case "1 x breakfast , 1 x vegetarian breakfast": { Meal = "Breakfast For One"; } break;
                case "1 x breakfast , 1 x vegetarian breakfast , breakfast for 1 person": { Meal = "Breakfast For One"; } break;
                case "1 x breakfast , 2 meals included": { Meal = "Breakfast For One"; } break;
                case "1 x breakfast , airport transfer (one way)": { Meal = "Breakfast For One"; } break;
                case "1 x breakfast , airport transfer (one way) , airport transfer [one way] , free room upgrade": { Meal = "Breakfast For One"; } break;
                case "1 x breakfast , airport transfer (two ways)": { Meal = "Breakfast For One"; } break;
                case "1 x breakfast , airport transfer [both ways]": { Meal = "Breakfast For One"; } break;
                case "1 x breakfast , airport transfer [both ways] , airport transfer [one way]": { Meal = "Breakfast For One"; } break;
                case "1 x breakfast , airport transfer [both ways] , breakfast": { Meal = "Breakfast For One"; } break;
                case "1 x breakfast , airport transfer [both ways] , breakfast for 2 people": { Meal = "Breakfast For One"; } break;
                case "1 x breakfast , airport transfer [one way]": { Meal = "Breakfast For One"; } break;
                case "1 x breakfast , breakfast": { Meal = "Breakfast For One"; } break;
                case "1 x breakfast , breakfast for 1 person": { Meal = "Breakfast For One"; } break;
                case "1 x breakfast , breakfast for 1 person , breakfast for 2 people": { Meal = "Breakfast For One"; } break;
                case "1 x breakfast , breakfast for 2 people": { Meal = "Breakfast For One"; } break;
                case "1 x breakfast , breakfast for 2 people , free room upgrade": { Meal = "Breakfast For One"; } break;
                case "1 x breakfast , complimentary breakfast for 3 guests": { Meal = "Breakfast For One"; } break;
                case "1 x breakfast , free flow beverages [non-alcoholic] for 2": { Meal = "Breakfast For One"; } break;
                case "1 x breakfast , free room upgrade": { Meal = "Breakfast For One"; } break;
                case "1 x complimentary breakfast": { Meal = "Breakfast For One"; } break;
                case "1 x complimentary breakfast , breakfast": { Meal = "Breakfast For One"; } break;
                case "1 x complimentary snacks , breakfast for 1 person": { Meal = "Breakfast For One"; } break;
                case "2 x breakfast , 2 x complimentary breakfast , 2 x complimentary snacks , 2 x local breakfast , 2 x vegetarian breakfast , breakfast , breakfast , breakfast for 1 person , breakfast for 2 people": { Meal = "Breakfast For One"; } break;
                case "2 x breakfast , 2 x complimentary breakfast , 2 x iftar , 2 x local breakfast , 2 x suhur , breakfast for 1 person , breakfast for 2 people , complimentary breakfast for 3 guests": { Meal = "Breakfast For One"; } break;
                case "2 x breakfast , 2 x local breakfast , breakfast for 1 person , breakfast for 2 people": { Meal = "Breakfast For One"; } break;
                case "2 x breakfast , 2 x vegetarian breakfast , breakfast for 1 person": { Meal = "Breakfast For One"; } break;
                case "2 x breakfast , airport transfer (one way)": { Meal = "Breakfast For One"; } break;
                case "2 x breakfast , breakfast for 1 person": { Meal = "Breakfast For One"; } break;
                case "2 x breakfast , breakfast for 1 person , breakfast for 2 people": { Meal = "Breakfast For One"; } break;
                case "2 x complimentary snacks , breakfast for 1 person": { Meal = "Breakfast For One"; } break;
                case "2 x food and beverage credit , breakfast for 1 person": { Meal = "Breakfast For One"; } break;
                case "2-for-1 buffet": { Meal = "Breakfast For One"; } break;
                case "2-for-1 buffet , breakfast for 1": { Meal = "Breakfast For One"; } break;
                case "2-for-1 buffet , full breakfast": { Meal = "Breakfast For One"; } break;
                case "3 x breakfast , breakfast for 1 person": { Meal = "Breakfast For One"; } break;
                case "3 x breakfast , breakfast for 1 person , breakfast for 2 people": { Meal = "Breakfast For One"; } break;
                case "3 x food and beverage credit , breakfast for 1 person": { Meal = "Breakfast For One"; } break;
                case "4 x breakfast , breakfast for 1 person": { Meal = "Breakfast For One"; } break;
                case "4 x food and beverage credit , breakfast for 1 person": { Meal = "Breakfast For One"; } break;
                case "airport transfer [both ways] , breakfast for 1 person": { Meal = "Breakfast For One"; } break;
                case "breakfast , breakfast , breakfast , breakfast for 1 person , breakfast for 2 people , complimentary breakfast , complimentary snacks , local breakfast , vegetarian breakfast": { Meal = "Breakfast For One"; } break;
                case "breakfast , breakfast for 1 person": { Meal = "Breakfast For One"; } break;
                case "breakfast , breakfast for 1 person , breakfast for 2 people , complimentary breakfast , complimentary breakfast for 3 guests , iftar , local breakfast , suhur": { Meal = "Breakfast For One"; } break;
                case "breakfast , breakfast for 1 person , breakfast for 2 people , complimentary breakfast , local breakfast , vegetarian breakfast": { Meal = "Breakfast For One"; } break;
                case "breakfast , breakfast for 1 person , breakfast for 2 people , local breakfast": { Meal = "Breakfast For One"; } break;
                case "breakfast , breakfast for 1 person , complimentary breakfast": { Meal = "Breakfast For One"; } break;
                case "breakfast , breakfast for 1 person , vegetarian breakfast": { Meal = "Breakfast For One"; } break;
                case "breakfast , complimentary breakfast , free flow beverages [non-alcoholic] for 1": { Meal = "Breakfast For One"; } break;
                case "breakfast buffet , breakfast for 1": { Meal = "Breakfast For One"; } break;
                case "breakfast buffet , breakfast for 1 , continental breakfast": { Meal = "Breakfast For One"; } break;
                case "breakfast buffet , breakfast for 1 , continental breakfast , full breakfast": { Meal = "Breakfast For One"; } break;
                case "breakfast buffet , breakfast for 1 , free breakfast": { Meal = "Breakfast For One"; } break;
                case "breakfast buffet , breakfast for 1 , full breakfast": { Meal = "Breakfast For One"; } break;
                case "breakfast for 1": { Meal = "Breakfast For One"; } break;
                case "breakfast for 1 , continental breakfast": { Meal = "Breakfast For One"; } break;
                case "breakfast for 1 , continental breakfast , free breakfast": { Meal = "Breakfast For One"; } break;
                case "breakfast for 1 , continental breakfast , free breakfast , full breakfast": { Meal = "Breakfast For One"; } break;
                case "breakfast for 1 , english breakfast": { Meal = "Breakfast For One"; } break;
                case "breakfast for 1 , food/beverage credit": { Meal = "Breakfast For One"; } break;
                case "breakfast for 1 , free breakfast": { Meal = "Breakfast For One"; } break;
                case "breakfast for 1 , full breakfast": { Meal = "Breakfast For One"; } break;
                case "breakfast for 1 adult": { Meal = "Breakfast For One"; } break;
                case "breakfast for 1 person": { Meal = "Breakfast For One"; } break;
                case "breakfast for 1 person , complimentary breakfast for 3 guests": { Meal = "Breakfast For One"; } break;
                case "breakfast for 1 person , complimentary snacks": { Meal = "Breakfast For One"; } break;
                case "breakfast for 1 person , food and beverage credit": { Meal = "Breakfast For One"; } break;
                case "breakfast for one": { Meal = "Breakfast For One"; } break;
                case "breakfast for one guest": { Meal = "Breakfast For One"; } break;
                case "breakfast-for-1": { Meal = "Breakfast For One"; } break;
                case "breakfast_for_1": { Meal = "Breakfast For One"; } break;
                case "1 x complimentary tickets , breakfast for 2 people": { Meal = "Breakfast For Two"; } break;
                case "1 x ferry transfer [both ways] , breakfast for 2 people": { Meal = "Breakfast For Two"; } break;
                case "2 adventure cove waterpark tickets , breakfast , breakfast for 2 people": { Meal = "Breakfast For Two"; } break;
                case "2 complimentary buffet credits , 2 x breakfast , airport transfer (one way)": { Meal = "Breakfast For Two"; } break;
                case "2 complimentary buffet credits , airport transfer (one way) , breakfast": { Meal = "Breakfast For Two"; } break;
                case "2 meals included , 2 x breakfast": { Meal = "Breakfast For Two"; } break;
                case "2 x breakfast": { Meal = "Breakfast For Two"; } break;
                case "2 x breakfast , 2 x complimentary breakfast": { Meal = "Breakfast For Two"; } break;
                case "2 x breakfast , 2 x complimentary breakfast , 2 x complimentary snacks": { Meal = "Breakfast For Two"; } break;
                case "2 x breakfast , 2 x complimentary breakfast , 2 x complimentary snacks , 2 x local breakfast , 2 x vegetarian breakfast": { Meal = "Breakfast For Two"; } break;
                case "2 x breakfast , 2 x complimentary breakfast , 2 x complimentary snacks , breakfast for 2 people": { Meal = "Breakfast For Two"; } break;
                case "2 x breakfast , 2 x complimentary breakfast , 2 x complimentary tickets": { Meal = "Breakfast For Two"; } break;
                case "2 x breakfast , 2 x complimentary breakfast , 2 x food and beverage credit": { Meal = "Breakfast For Two"; } break;
                case "2 x breakfast , 2 x complimentary breakfast , 2 x local breakfast": { Meal = "Breakfast For Two"; } break;
                case "2 x breakfast , 2 x complimentary breakfast , 2 x local breakfast , 2 x vegetarian breakfast": { Meal = "Breakfast For Two"; } break;
                case "2 x breakfast , 2 x complimentary breakfast , 2 x local breakfast , 2 x vegetarian breakfast , breakfast for 1 person , breakfast for 2 people": { Meal = "Breakfast For Two"; } break;
                case "2 x breakfast , 2 x complimentary breakfast , 2 x local breakfast , 2 x vegetarian breakfast , breakfast for 2 people": { Meal = "Breakfast For Two"; } break;
                case "2 x breakfast , 2 x complimentary breakfast , 2 x local breakfast , 2 x vegetarian breakfast , free room upgrade": { Meal = "Breakfast For Two"; } break;
                case "2 x breakfast , 2 x complimentary breakfast , 2 x local breakfast , breakfast for 2 people": { Meal = "Breakfast For Two"; } break;
                case "2 x breakfast , 2 x complimentary breakfast , 2 x local breakfast , free room upgrade": { Meal = "Breakfast For Two"; } break;
                case "2 x breakfast , 2 x complimentary breakfast , 2 x vegetarian breakfast": { Meal = "Breakfast For Two"; } break;
                case "2 x breakfast , 2 x complimentary breakfast , 2 x water park tickets": { Meal = "Breakfast For Two"; } break;
                case "2 x breakfast , 2 x complimentary breakfast , airport transfer (two ways) , airport transfer [both ways]": { Meal = "Breakfast For Two"; } break;
                case "2 x breakfast , 2 x complimentary breakfast , airport transfer (two ways) , airport transfer [both ways] , breakfast for 2 people": { Meal = "Breakfast For Two"; } break;
                case "2 x breakfast , 2 x complimentary breakfast , airport transfer [both ways]": { Meal = "Breakfast For Two"; } break;
                case "2 x breakfast , 2 x complimentary breakfast , airport transfer [one way]": { Meal = "Breakfast For Two"; } break;
                case "2 x breakfast , 2 x complimentary breakfast , breakfast": { Meal = "Breakfast For Two"; } break;
                case "2 x breakfast , 2 x complimentary breakfast , breakfast for 2 people": { Meal = "Breakfast For Two"; } break;
                case "2 x breakfast , 2 x complimentary breakfast , free room upgrade": { Meal = "Breakfast For Two"; } break;
                case "2 x breakfast , 2 x complimentary snacks": { Meal = "Breakfast For Two"; } break;
                case "2 x breakfast , 2 x complimentary tickets": { Meal = "Breakfast For Two"; } break;
                case "2 x breakfast , 2 x ferry transfer [both ways]": { Meal = "Breakfast For Two"; } break;
                case "2 x breakfast , 2 x ferry transfer [both ways] , 2 x local island excursion , 2 x snorkeling excursion , breakfast": { Meal = "Breakfast For Two"; } break;
                case "2 x breakfast , 2 x ferry transfer [both ways] , 2 x snorkeling excursion": { Meal = "Breakfast For Two"; } break;
                case "2 x breakfast , 2 x food and beverage credit": { Meal = "Breakfast For Two"; } break;
                case "2 x breakfast , 2 x food and beverage credit , airport transfer [both ways]": { Meal = "Breakfast For Two"; } break;
                case "2 x breakfast , 2 x local breakfast": { Meal = "Breakfast For Two"; } break;
                case "2 x breakfast , 2 x local breakfast , 2 x suhur , 2 x vegetarian breakfast": { Meal = "Breakfast For Two"; } break;
                case "2 x breakfast , 2 x local breakfast , 2 x vegetarian breakfast , breakfast for 2 people": { Meal = "Breakfast For Two"; } break;
                case "2 x breakfast , 2 x local breakfast , breakfast for 2 people": { Meal = "Breakfast For Two"; } break;
                case "2 x breakfast , 2 x suhur , free room upgrade": { Meal = "Breakfast For Two"; } break;
                case "2 x breakfast , 2 x theme park tickets , breakfast for 2 people": { Meal = "Breakfast For Two"; } break;
                case "2 x breakfast , 2 x vegetarian breakfast": { Meal = "Breakfast For Two"; } break;
                case "2 x breakfast , 2 x vegetarian breakfast , breakfast for 2 people": { Meal = "Breakfast For Two"; } break;
                case "2 x breakfast , 2 x water park tickets": { Meal = "Breakfast For Two"; } break;
                case "2 x breakfast , airport transfer (one way) , airport transfer [one way] , free room upgrade": { Meal = "Breakfast For Two"; } break;
                case "2 x breakfast , airport transfer (two ways)": { Meal = "Breakfast For Two"; } break;
                case "2 x breakfast , airport transfer [both ways]": { Meal = "Breakfast For Two"; } break;
                case "2 x breakfast , airport transfer [both ways] , airport transfer [one way]": { Meal = "Breakfast For Two"; } break;
                case "2 x breakfast , airport transfer [both ways] , breakfast": { Meal = "Breakfast For Two"; } break;
                case "2 x breakfast , airport transfer [both ways] , breakfast for 2 people": { Meal = "Breakfast For Two"; } break;
                case "2 x breakfast , airport transfer [one way]": { Meal = "Breakfast For Two"; } break;
                case "2 x breakfast , breakfast": { Meal = "Breakfast For Two"; } break;
                case "2 x breakfast , breakfast , breakfast for 2 people": { Meal = "Breakfast For Two"; } break;
                case "2 x breakfast , breakfast for 2 people": { Meal = "Breakfast For Two"; } break;
                case "2 x breakfast , breakfast for 2 people , free room upgrade": { Meal = "Breakfast For Two"; } break;
                case "2 x breakfast , complimentary breakfast for 3 guests": { Meal = "Breakfast For Two"; } break;
                case "2 x breakfast , free flow beverages [non-alcoholic] for 2": { Meal = "Breakfast For Two"; } break;
                case "2 x breakfast , free room upgrade": { Meal = "Breakfast For Two"; } break;
                case "2 x complimentary breakfast": { Meal = "Breakfast For Two"; } break;
                case "2 x complimentary breakfast , breakfast": { Meal = "Breakfast For Two"; } break;
                case "2 x complimentary breakfast , free room upgrade": { Meal = "Breakfast For Two"; } break;
                case "2 x complimentary tickets , breakfast for 2 people": { Meal = "Breakfast For Two"; } break;
                case "2 x local breakfast": { Meal = "Breakfast For Two"; } break;
                case "2 x vegetarian breakfast , breakfast for 2 people": { Meal = "Breakfast For Two"; } break;
                case "2-for-1 buffet , breakfast for 2": { Meal = "Breakfast For Two"; } break;
                case "2-for-1 buffet , breakfast for 2 , free breakfast": { Meal = "Breakfast For Two"; } break;
                case "3 x breakfast , 3 x complimentary breakfast , 3 x complimentary snacks , breakfast for 2 people": { Meal = "Breakfast For Two"; } break;
                case "3 x breakfast , 3 x complimentary breakfast , 3 x local breakfast , 3 x vegetarian breakfast , breakfast for 1 person , breakfast for 2 people": { Meal = "Breakfast For Two"; } break;
                case "3 x breakfast , 3 x complimentary breakfast , 3 x local breakfast , 3 x vegetarian breakfast , breakfast for 2 people": { Meal = "Breakfast For Two"; } break;
                case "3 x breakfast , 3 x complimentary breakfast , breakfast for 2 people": { Meal = "Breakfast For Two"; } break;
                case "3 x breakfast , 3 x local breakfast , breakfast for 2 people": { Meal = "Breakfast For Two"; } break;
                case "3 x breakfast , airport transfer [both ways] , breakfast for 2 people": { Meal = "Breakfast For Two"; } break;
                case "3 x breakfast , breakfast , breakfast for 2 people": { Meal = "Breakfast For Two"; } break;
                case "3 x breakfast , breakfast for 2 people": { Meal = "Breakfast For Two"; } break;
                case "3 x breakfast , free flow beverages [non-alcoholic] for 2": { Meal = "Breakfast For Two"; } break;
                case "3 x complimentary tickets , breakfast for 2 people": { Meal = "Breakfast For Two"; } break;
                case "4 x breakfast , 4 x buffet dinner , 4 x dinner included , breakfast , breakfast for 2 people , dinner included": { Meal = "Breakfast For Two"; } break;
                case "4 x breakfast , 4 x complimentary breakfast , 4 x local breakfast , 4 x vegetarian breakfast , breakfast for 2 people": { Meal = "Breakfast For Two"; } break;
                case "4 x breakfast , 4 x complimentary breakfast , breakfast for 2 people": { Meal = "Breakfast For Two"; } break;
                case "4 x breakfast , breakfast for 2 people": { Meal = "Breakfast For Two"; } break;
                case "4 x complimentary tickets , breakfast for 2 people": { Meal = "Breakfast For Two"; } break;
                case "airport transfer (two ways) , airport transfer [both ways] , breakfast , breakfast for 2 people , complimentary breakfast": { Meal = "Breakfast For Two"; } break;
                case "airport transfer (two ways) , breakfast for 2 people": { Meal = "Breakfast For Two"; } break;
                case "airport transfer [both ways] , breakfast , breakfast for 2 people": { Meal = "Breakfast For Two"; } break;
                case "airport transfer [both ways] , breakfast for 2 people": { Meal = "Breakfast For Two"; } break;
                case "bed & breakfast for 2 pax": { Meal = "Breakfast For Two"; } break;
                case "breakfast , breakfast , breakfast for 2 people": { Meal = "Breakfast For Two"; } break;
                case "breakfast , breakfast for 1 person , breakfast for 2 people": { Meal = "Breakfast For Two"; } break;
                case "breakfast , breakfast for 2 people": { Meal = "Breakfast For Two"; } break;
                case "breakfast , breakfast for 2 people , complimentary breakfast": { Meal = "Breakfast For Two"; } break;
                case "breakfast , breakfast for 2 people , complimentary breakfast , complimentary snacks": { Meal = "Breakfast For Two"; } break;
                case "breakfast , breakfast for 2 people , complimentary breakfast , local breakfast": { Meal = "Breakfast For Two"; } break;
                case "breakfast , breakfast for 2 people , complimentary breakfast , local breakfast , vegetarian breakfast": { Meal = "Breakfast For Two"; } break;
                case "breakfast , breakfast for 2 people , food and beverage credit": { Meal = "Breakfast For Two"; } break;
                case "breakfast , breakfast for 2 people , free room upgrade": { Meal = "Breakfast For Two"; } break;
                case "breakfast , breakfast for 2 people , local breakfast": { Meal = "Breakfast For Two"; } break;
                case "breakfast , breakfast for 2 people , local breakfast , vegetarian breakfast": { Meal = "Breakfast For Two"; } break;
                case "breakfast , breakfast for 2 people , theme park tickets": { Meal = "Breakfast For Two"; } break;
                case "breakfast , breakfast for 2 people , vegetarian breakfast": { Meal = "Breakfast For Two"; } break;
                case "breakfast , free flow beverages [non-alcoholic] for 2": { Meal = "Breakfast For Two"; } break;
                case "breakfast buffet , breakfast for 1 , breakfast for 2": { Meal = "Breakfast For Two"; } break;
                case "breakfast buffet , breakfast for 1 , breakfast for 2 , continental breakfast": { Meal = "Breakfast For Two"; } break;
                case "breakfast buffet , breakfast for 1 , breakfast for 2 , free breakfast": { Meal = "Breakfast For Two"; } break;
                case "breakfast buffet , breakfast for 1 , breakfast for 2 , full breakfast": { Meal = "Breakfast For Two"; } break;
                case "breakfast buffet , breakfast for 2": { Meal = "Breakfast For Two"; } break;
                case "breakfast buffet , breakfast for 2 , continental breakfast": { Meal = "Breakfast For Two"; } break;
                case "breakfast buffet , breakfast for 2 , continental breakfast , continental breakfast for 2": { Meal = "Breakfast For Two"; } break;
                case "breakfast buffet , breakfast for 2 , continental breakfast for 2": { Meal = "Breakfast For Two"; } break;
                case "breakfast buffet , breakfast for 2 , english breakfast , full breakfast": { Meal = "Breakfast For Two"; } break;
                case "breakfast buffet , breakfast for 2 , free breakfast": { Meal = "Breakfast For Two"; } break;
                case "breakfast buffet , breakfast for 2 , free breakfast , full breakfast": { Meal = "Breakfast For Two"; } break;
                case "breakfast buffet , breakfast for 2 , free breakfast , japanese-style breakfast , taiwanese-style breakfast": { Meal = "Breakfast For Two"; } break;
                case "breakfast buffet , breakfast for 2 , full breakfast": { Meal = "Breakfast For Two"; } break;
                case "breakfast buffet , breakfast for 2 , japanese-style breakfast": { Meal = "Breakfast For Two"; } break;
                case "breakfast buffet , continental breakfast , continental breakfast for 2": { Meal = "Breakfast For Two"; } break;
                case "breakfast buffet , continental breakfast , continental breakfast for 2 , english breakfast , full breakfast": { Meal = "Breakfast For Two"; } break;
                case "breakfast buffet , continental breakfast for 2": { Meal = "Breakfast For Two"; } break;
                case "breakfast buffet , continental breakfast for 2 , english breakfast , full breakfast": { Meal = "Breakfast For Two"; } break;
                case "breakfast buffet , continental breakfast for 2 , free breakfast": { Meal = "Breakfast For Two"; } break;
                case "breakfast buffet , continental breakfast for 2 , full breakfast": { Meal = "Breakfast For Two"; } break;
                case "breakfast for 1 , breakfast for 2": { Meal = "Breakfast For Two"; } break;
                case "breakfast for 1 , breakfast for 2 , continental breakfast": { Meal = "Breakfast For Two"; } break;
                case "breakfast for 1 , breakfast for 2 , continental breakfast for 2": { Meal = "Breakfast For Two"; } break;
                case "breakfast for 1 , breakfast for 2 , free breakfast": { Meal = "Breakfast For Two"; } break;
                case "breakfast for 1 , continental breakfast for 2": { Meal = "Breakfast For Two"; } break;
                case "breakfast for 2": { Meal = "Breakfast For Two"; } break;
                case "breakfast for 2 , continental breakfast": { Meal = "Breakfast For Two"; } break;
                case "breakfast for 2 , continental breakfast , continental breakfast for 2": { Meal = "Breakfast For Two"; } break;
                case "breakfast for 2 , continental breakfast , continental breakfast for 2 , full breakfast": { Meal = "Breakfast For Two"; } break;
                case "breakfast for 2 , continental breakfast , english breakfast": { Meal = "Breakfast For Two"; } break;
                case "breakfast for 2 , continental breakfast , english breakfast , full breakfast": { Meal = "Breakfast For Two"; } break;
                case "breakfast for 2 , continental breakfast , free breakfast": { Meal = "Breakfast For Two"; } break;
                case "breakfast for 2 , continental breakfast , free breakfast , full breakfast": { Meal = "Breakfast For Two"; } break;
                case "breakfast for 2 , continental breakfast for 2": { Meal = "Breakfast For Two"; } break;
                case "breakfast for 2 , continental breakfast for 2 , english breakfast , free breakfast": { Meal = "Breakfast For Two"; } break;
                case "breakfast for 2 , continental breakfast for 2 , free breakfast": { Meal = "Breakfast For Two"; } break;
                case "breakfast for 2 , english breakfast": { Meal = "Breakfast For Two"; } break;
                case "breakfast for 2 , english breakfast , free breakfast": { Meal = "Breakfast For Two"; } break;
                case "breakfast for 2 , food/beverage credit": { Meal = "Breakfast For Two"; } break;
                case "breakfast for 2 , free breakfast": { Meal = "Breakfast For Two"; } break;
                case "breakfast for 2 , free breakfast , full breakfast": { Meal = "Breakfast For Two"; } break;
                case "breakfast for 2 , full breakfast": { Meal = "Breakfast For Two"; } break;
                case "breakfast for 2 , taiwanese-style breakfast": { Meal = "Breakfast For Two"; } break;
                case "breakfast for 2 people": { Meal = "Breakfast For Two"; } break;
                case "breakfast for 2 people , complimentary tickets": { Meal = "Breakfast For Two"; } break;
                case "breakfast for 2 people , ferry transfer [both ways]": { Meal = "Breakfast For Two"; } break;
                case "breakfast for 2 people , free room upgrade": { Meal = "Breakfast For Two"; } break;
                case "breakfast for 2 people , theme park tickets": { Meal = "Breakfast For Two"; } break;
                case "breakfast for 2 people , vegetarian breakfast": { Meal = "Breakfast For Two"; } break;
                case "breakfast for 2 people, free wifi": { Meal = "Breakfast For Two"; } break;
                case "breakfast for 2 person": { Meal = "Breakfast For Two"; } break;
                case "breakfast for 2 persons": { Meal = "Breakfast For Two"; } break;
                case "breakfast for two": { Meal = "Breakfast For Two"; } break;
                case "breakfast for two guests": { Meal = "Breakfast For Two"; } break;
                case "breakfast-for-2": { Meal = "Breakfast For Two"; } break;
                case "breakfast_for_2": { Meal = "Breakfast For Two"; } break;
                case "continental breakfast , continental breakfast for 2": { Meal = "Breakfast For Two"; } break;
                case "continental breakfast , continental breakfast for 2 , free breakfast": { Meal = "Breakfast For Two"; } break;
                case "continental breakfast for 2": { Meal = "Breakfast For Two"; } break;
                case "continental breakfast for 2 , english breakfast": { Meal = "Breakfast For Two"; } break;
                case "continental breakfast for 2 , english breakfast , full breakfast": { Meal = "Breakfast For Two"; } break;
                case "continental breakfast for 2 , free breakfast": { Meal = "Breakfast For Two"; } break;
                case "continental breakfast for 2 , full breakfast": { Meal = "Breakfast For Two"; } break;
                case "2 complimentary buffet credits , 3 x breakfast , airport transfer (one way)": { Meal = "Breakfast For Three"; } break;
                case "3 x breakfast": { Meal = "Breakfast For Three"; } break;
                case "3 x breakfast , 3 x complimentary breakfast": { Meal = "Breakfast For Three"; } break;
                case "3 x breakfast , 3 x complimentary breakfast , 3 x complimentary snacks": { Meal = "Breakfast For Three"; } break;
                case "3 x breakfast , 3 x complimentary breakfast , 3 x complimentary tickets": { Meal = "Breakfast For Three"; } break;
                case "3 x breakfast , 3 x complimentary breakfast , 3 x food and beverage credit": { Meal = "Breakfast For Three"; } break;
                case "3 x breakfast , 3 x complimentary breakfast , 3 x food and beverage credit , 3 x local breakfast , 3 x vegetarian breakfast , airport transfer [one way]": { Meal = "Breakfast For Three"; } break;
                case "3 x breakfast , 3 x complimentary breakfast , 3 x local breakfast": { Meal = "Breakfast For Three"; } break;
                case "3 x breakfast , 3 x complimentary breakfast , 3 x local breakfast , 3 x vegetarian breakfast": { Meal = "Breakfast For Three"; } break;
                case "3 x breakfast , 3 x complimentary breakfast , 3 x local breakfast , 3 x vegetarian breakfast , free room upgrade": { Meal = "Breakfast For Three"; } break;
                case "3 x breakfast , 3 x complimentary breakfast , 3 x vegetarian breakfast": { Meal = "Breakfast For Three"; } break;
                case "3 x breakfast , 3 x complimentary breakfast , 3 x vegetarian breakfast , airport transfer [one way]": { Meal = "Breakfast For Three"; } break;
                case "3 x breakfast , 3 x complimentary breakfast , airport transfer [both ways]": { Meal = "Breakfast For Three"; } break;
                case "3 x breakfast , 3 x complimentary breakfast , airport transfer [one way]": { Meal = "Breakfast For Three"; } break;
                case "3 x breakfast , 3 x complimentary breakfast , breakfast": { Meal = "Breakfast For Three"; } break;
                case "3 x breakfast , 3 x complimentary breakfast , free room upgrade": { Meal = "Breakfast For Three"; } break;
                case "3 x breakfast , 3 x complimentary snacks": { Meal = "Breakfast For Three"; } break;
                case "3 x breakfast , 3 x complimentary tickets": { Meal = "Breakfast For Three"; } break;
                case "3 x breakfast , 3 x ferry transfer [both ways]": { Meal = "Breakfast For Three"; } break;
                case "3 x breakfast , 3 x ferry transfer [both ways] , 3 x snorkeling excursion": { Meal = "Breakfast For Three"; } break;
                case "3 x breakfast , 3 x food and beverage credit": { Meal = "Breakfast For Three"; } break;
                case "3 x breakfast , 3 x local breakfast": { Meal = "Breakfast For Three"; } break;
                case "3 x breakfast , 3 x local breakfast , 3 x suhur , 3 x vegetarian breakfast": { Meal = "Breakfast For Three"; } break;
                case "3 x breakfast , 3 x suhur , free room upgrade": { Meal = "Breakfast For Three"; } break;
                case "3 x breakfast , 3 x theme park tickets": { Meal = "Breakfast For Three"; } break;
                case "3 x breakfast , 3 x vegetarian breakfast": { Meal = "Breakfast For Three"; } break;
                case "3 x breakfast , 3 x water park tickets": { Meal = "Breakfast For Three"; } break;
                case "3 x breakfast , airport transfer (one way)": { Meal = "Breakfast For Three"; } break;
                case "3 x breakfast , airport transfer (two ways)": { Meal = "Breakfast For Three"; } break;
                case "3 x breakfast , airport transfer (two ways) , airport transfer [both ways] , airport transfer [one way]": { Meal = "Breakfast For Three"; } break;
                case "3 x breakfast , airport transfer [both ways]": { Meal = "Breakfast For Three"; } break;
                case "3 x breakfast , airport transfer [both ways] , breakfast": { Meal = "Breakfast For Three"; } break;
                case "3 x breakfast , airport transfer [one way]": { Meal = "Breakfast For Three"; } break;
                case "3 x breakfast , breakfast": { Meal = "Breakfast For Three"; } break;
                case "3 x breakfast , complimentary breakfast for 3 guests": { Meal = "Breakfast For Three"; } break;
                case "3 x breakfast , free room upgrade": { Meal = "Breakfast For Three"; } break;
                case "3 x complimentary breakfast": { Meal = "Breakfast For Three"; } break;
                case "airport transfer (one way) , airport transfer (two ways) , airport transfer [both ways] , breakfast , complimentary breakfast , complimentary breakfast for 3 guests , local breakfast , one half day excursion per person per stay , theme park tickets": { Meal = "Breakfast For Three"; } break;
                case "breakfast , complimentary breakfast for 3 guests": { Meal = "Breakfast For Three"; } break;
                case "breakfast for 3": { Meal = "Breakfast For Three"; } break;
                case "breakfast for three": { Meal = "Breakfast For Three"; } break;
                case "complimentary breakfast for 3 guests": { Meal = "Breakfast For Three"; } break;
                case "2 complimentary buffet credits , 4 x breakfast , airport transfer (one way)": { Meal = "Breakfast For Four"; } break;
                case "4 x breakfast": { Meal = "Breakfast For Four"; } break;
                case "4 x breakfast , 4 x complimentary breakfast": { Meal = "Breakfast For Four"; } break;
                case "4 x breakfast , 4 x complimentary breakfast , 4 x complimentary snacks": { Meal = "Breakfast For Four"; } break;
                case "4 x breakfast , 4 x complimentary breakfast , 4 x complimentary snacks , 4 x local breakfast , 4 x vegetarian breakfast": { Meal = "Breakfast For Four"; } break;
                case "4 x breakfast , 4 x complimentary breakfast , 4 x complimentary snacks , breakfast": { Meal = "Breakfast For Four"; } break;
                case "4 x breakfast , 4 x complimentary breakfast , 4 x local breakfast": { Meal = "Breakfast For Four"; } break;
                case "4 x breakfast , 4 x complimentary breakfast , 4 x local breakfast , 4 x vegetarian breakfast": { Meal = "Breakfast For Four"; } break;
                case "4 x breakfast , 4 x complimentary breakfast , 4 x vegetarian breakfast": { Meal = "Breakfast For Four"; } break;
                case "4 x breakfast , 4 x complimentary breakfast , 4 x water park tickets": { Meal = "Breakfast For Four"; } break;
                case "4 x breakfast , 4 x complimentary breakfast , free room upgrade": { Meal = "Breakfast For Four"; } break;
                case "4 x breakfast , 4 x complimentary snacks": { Meal = "Breakfast For Four"; } break;
                case "4 x breakfast , 4 x ferry transfer [both ways]": { Meal = "Breakfast For Four"; } break;
                case "4 x breakfast , 4 x ferry transfer [both ways] , 4 x snorkeling excursion": { Meal = "Breakfast For Four"; } break;
                case "4 x breakfast , 4 x local breakfast": { Meal = "Breakfast For Four"; } break;
                case "4 x breakfast , 4 x vegetarian breakfast": { Meal = "Breakfast For Four"; } break;
                case "4 x breakfast , 4 x water park tickets": { Meal = "Breakfast For Four"; } break;
                case "4 x breakfast , airport transfer (one way)": { Meal = "Breakfast For Four"; } break;
                case "4 x breakfast , airport transfer (two ways) , airport transfer [both ways] , airport transfer [one way]": { Meal = "Breakfast For Four"; } break;
                case "4 x breakfast , airport transfer [both ways]": { Meal = "Breakfast For Four"; } break;
                case "4 x breakfast , airport transfer [both ways] , breakfast": { Meal = "Breakfast For Four"; } break;
                case "4 x breakfast , breakfast": { Meal = "Breakfast For Four"; } break;
                case "4 x breakfast , free room upgrade": { Meal = "Breakfast For Four"; } break;
                case "4 x complimentary breakfast": { Meal = "Breakfast For Four"; } break;
                case "4 x complimentary breakfast , free room upgrade": { Meal = "Breakfast For Four"; } break;
                case "breakfast for four": { Meal = "Breakfast For Four"; } break;
                case "1 x breakfast , 1 x complimentary breakfast , 1 x dinner included": { Meal = "Half Board For One"; } break;
                case "1 x breakfast , 1 x dinner included": { Meal = "Half Board For One"; } break;
                case "1 x breakfast , 1 x dinner included , breakfast for 2 people": { Meal = "Half Board For One"; } break;
                case "1 x breakfast , 1 x lunch included": { Meal = "Half Board For One"; } break;
                case "breakfast for 1 , buffet dinner": { Meal = "Half Board For One"; } break;
                case "breakfast for 1 , buffet dinner , english breakfast": { Meal = "Half Board For One"; } break;
                case "breakfast for 1 , free dinner": { Meal = "Half Board For One"; } break;
                case "breakfast for 1 , free lunch": { Meal = "Half Board For One"; } break;
                case "breakfast for 1 , full breakfast , vegetarian dinner": { Meal = "Half Board For One"; } break;
                case "breakfast for 1 , half board (buffet dinner)": { Meal = "Half Board For One"; } break;
                case "breakfast for 1 , taiwanese-style breakfast": { Meal = "Half Board For One"; } break;
                case "breakfast for 1 , vegetarian dinner": { Meal = "Half Board For One"; } break;
                case "half board for 1 person": { Meal = "Half Board For One"; } break;
                case "half board for one": { Meal = "Half Board For One"; } break;
                case "2 meals included , 2 x breakfast , 2 x buffet dinner , 2 x complimentary breakfast , 2 x complimentary snacks , 2 x dinner included": { Meal = "Half Board For Two"; } break;
                case "2 meals included , 2 x breakfast , 2 x complimentary breakfast , 2 x complimentary lunch , 2 x local breakfast , 2 x lunch included , 2 x vegetarian breakfast": { Meal = "Half Board For Two"; } break;
                case "2 meals included , 2 x breakfast , 2 x complimentary dinner": { Meal = "Half Board For Two"; } break;
                case "2 meals included , 2 x breakfast , 2 x complimentary dinner , 2 x complimentary snacks , 2 x dinner included": { Meal = "Half Board For Two"; } break;
                case "2 meals included , 2 x breakfast , 2 x complimentary dinner , 2 x dinner included": { Meal = "Half Board For Two"; } break;
                case "2 meals included , 2 x breakfast , 2 x complimentary snacks , 2 x dinner included , 2 x wagyu beef dinner": { Meal = "Half Board For Two"; } break;
                case "2 meals included , 2 x breakfast , 2 x dinner in private roo , 2 x dinner included , 2 x japanese dinner , 2 x kaiseki dinner": { Meal = "Half Board For Two"; } break;
                case "2 meals included , 2 x breakfast , 2 x dinner included": { Meal = "Half Board For Two"; } break;
                case "2 meals included , 2 x breakfast , 2 x dinner included , 2 x japanese dinner": { Meal = "Half Board For Two"; } break;
                case "2 meals included , 2 x breakfast , 2 x dinner included , 2 x japanese dinner , 2 x kaiseki dinner , 2 x wagyu beef dinner": { Meal = "Half Board For Two"; } break;
                case "2 meals included , 2 x breakfast , 2 x dinner included , airport transfer (two ways)": { Meal = "Half Board For Two"; } break;
                case "2 meals included , 2 x breakfast , 2 x dinner included , breakfast for 2 people": { Meal = "Half Board For Two"; } break;
                case "2 meals included , 2 x breakfast , 2 x dinner included , breakfast for 2 people , dinner included": { Meal = "Half Board For Two"; } break;
                case "2 meals included , 2 x breakfast , 2 x japanese dinner": { Meal = "Half Board For Two"; } break;
                case "2 meals included , breakfast , breakfast for 2 people , dinner included": { Meal = "Half Board For Two"; } break;
                case "2 meals included , breakfast , breakfast for 2 people , dinner included , dinner included": { Meal = "Half Board For Two"; } break;
                case "2 x breakfast , 2 x buffet dinner": { Meal = "Half Board For Two"; } break;
                case "2 x breakfast , 2 x buffet dinner , 2 x complimentary breakfast , 2 x dinner included": { Meal = "Half Board For Two"; } break;
                case "2 x breakfast , 2 x buffet dinner , 2 x complimentary breakfast , 2 x dinner included , 2 x local breakfast , breakfast for 2 people": { Meal = "Half Board For Two"; } break;
                case "2 x breakfast , 2 x buffet dinner , 2 x dinner included": { Meal = "Half Board For Two"; } break;
                case "2 x breakfast , 2 x buffet dinner , 2 x dinner included , breakfast , breakfast for 2 people , dinner included": { Meal = "Half Board For Two"; } break;
                case "2 x breakfast , 2 x buffet dinner , 2 x dinner included , breakfast for 2 people": { Meal = "Half Board For Two"; } break;
                case "2 x breakfast , 2 x complimentary breakfast , 2 x complimentary dinner": { Meal = "Half Board For Two"; } break;
                case "2 x breakfast , 2 x complimentary breakfast , 2 x complimentary dinner , 2 x dinner included": { Meal = "Half Board For Two"; } break;
                case "2 x breakfast , 2 x complimentary breakfast , 2 x complimentary lunch , 2 x local breakfast , 2 x lunch included": { Meal = "Half Board For Two"; } break;
                case "2 x breakfast , 2 x complimentary breakfast , 2 x complimentary lunch , 2 x local breakfast , 2 x lunch included , 2 x vegetarian breakfast": { Meal = "Half Board For Two"; } break;
                case "2 x breakfast , 2 x complimentary breakfast , 2 x dinner included": { Meal = "Half Board For Two"; } break;
                case "2 x breakfast , 2 x complimentary dinner": { Meal = "Half Board For Two"; } break;
                case "2 x breakfast , 2 x dinner included": { Meal = "Half Board For Two"; } break;
                case "2 x breakfast , 2 x dinner included , 2 x food and beverage credit , breakfast for 2 people": { Meal = "Half Board For Two"; } break;
                case "2 x breakfast , 2 x dinner included , 2 x japanese dinner , 2 x kaiseki dinner": { Meal = "Half Board For Two"; } break;
                case "2 x breakfast , 2 x dinner included , 2 x kaiseki dinner": { Meal = "Half Board For Two"; } break;
                case "2 x breakfast , 2 x dinner included , 2 x wagyu beef dinner": { Meal = "Half Board For Two"; } break;
                case "2 x breakfast , 2 x dinner included , 2 x wagyu beef dinner , 2 x western dinner": { Meal = "Half Board For Two"; } break;
                case "2 x breakfast , 2 x dinner included , 2 x western dinner": { Meal = "Half Board For Two"; } break;
                case "2 x breakfast , 2 x dinner included , airport transfer (two ways)": { Meal = "Half Board For Two"; } break;
                case "2 x breakfast , 2 x dinner included , airport transfer [both ways]": { Meal = "Half Board For Two"; } break;
                case "2 x breakfast , 2 x dinner included , airport transfer [one way]": { Meal = "Half Board For Two"; } break;
                case "2 x breakfast , 2 x dinner included , breakfast for 2 people": { Meal = "Half Board For Two"; } break;
                case "2 x breakfast , 2 x in-room dinner , 2 x kaiseki dinner": { Meal = "Half Board For Two"; } break;
                case "2 x breakfast , 2 x japanese dinner": { Meal = "Half Board For Two"; } break;
                case "2 x breakfast , 2 x lunch included": { Meal = "Half Board For Two"; } break;
                case "2 x breakfast , 2 x lunch included , breakfast for 2 people": { Meal = "Half Board For Two"; } break;
                case "2 x breakfast , breakfast for 2 people , dinner included": { Meal = "Half Board For Two"; } break;
                case "2 x complimentary breakfast , 2 x complimentary dinner": { Meal = "Half Board For Two"; } break;
                case "3 x breakfast , 3 x buffet dinner , 3 x dinner included , breakfast , breakfast for 2 people , dinner included": { Meal = "Half Board For Two"; } break;
                case "3 x breakfast , 3 x buffet dinner , 3 x dinner included , breakfast for 2 people": { Meal = "Half Board For Two"; } break;
                case "4 x breakfast , 4 x buffet dinner , 4 x dinner included , breakfast for 2 people": { Meal = "Half Board For Two"; } break;
                case "4 x breakfast , 4 x dinner included , breakfast for 2 people , dinner included": { Meal = "Half Board For Two"; } break;
                case "breakfast , breakfast , breakfast for 2 people , buffet dinner , dinner included , dinner included": { Meal = "Half Board For Two"; } break;
                case "breakfast , breakfast for 2 people , buffet dinner , complimentary breakfast , dinner included , local breakfast": { Meal = "Half Board For Two"; } break;
                case "breakfast , breakfast for 2 people , buffet dinner , dinner included": { Meal = "Half Board For Two"; } break;
                case "breakfast , breakfast for 2 people , dinner included": { Meal = "Half Board For Two"; } break;
                case "breakfast , breakfast for 2 people , dinner included , dinner included": { Meal = "Half Board For Two"; } break;
                case "breakfast , breakfast for 2 people , dinner included , food and beverage credit": { Meal = "Half Board For Two"; } break;
                case "breakfast , breakfast for 2 people , dinner included , local breakfast , western dinner": { Meal = "Half Board For Two"; } break;
                case "breakfast , breakfast for 2 people , lunch included": { Meal = "Half Board For Two"; } break;
                case "breakfast buffet , breakfast for 2 , buffet dinner": { Meal = "Half Board For Two"; } break;
                case "breakfast buffet , breakfast for 2 , buffet dinner , free dinner": { Meal = "Half Board For Two"; } break;
                case "breakfast for 1 , breakfast for 2 , free dinner": { Meal = "Half Board For Two"; } break;
                case "breakfast for 2 , buffet dinner": { Meal = "Half Board For Two"; } break;
                case "breakfast for 2 , buffet dinner , free dinner": { Meal = "Half Board For Two"; } break;
                case "breakfast for 2 , buffet dinner , half board": { Meal = "Half Board For Two"; } break;
                case "breakfast for 2 , chinese-style dinner , continental breakfast": { Meal = "Half Board For Two"; } break;
                case "breakfast for 2 , food/beverage credit , half board": { Meal = "Half Board For Two"; } break;
                case "breakfast for 2 , free breakfast , free dinner": { Meal = "Half Board For Two"; } break;
                case "breakfast for 2 , free breakfast , free lunch": { Meal = "Half Board For Two"; } break;
                case "breakfast for 2 , free breakfast , half board": { Meal = "Half Board For Two"; } break;
                case "breakfast for 2 , free dinner": { Meal = "Half Board For Two"; } break;
                case "breakfast for 2 , free dinner , full breakfast": { Meal = "Half Board For Two"; } break;
                case "breakfast for 2 , free lunch": { Meal = "Half Board For Two"; } break;
                case "breakfast for 2 , half board": { Meal = "Half Board For Two"; } break;
                case "breakfast for 2 , half board (buffet dinner)": { Meal = "Half Board For Two"; } break;
                case "breakfast for 2 , half board (kaiseki dinner)": { Meal = "Half Board For Two"; } break;
                case "breakfast for 2 , kaiseki dinner": { Meal = "Half Board For Two"; } break;
                case "breakfast for 2 , vegetarian dinner": { Meal = "Half Board For Two"; } break;
                case "breakfast for 2 people , complimentary dinner , dinner included": { Meal = "Half Board For Two"; } break;
                case "continental breakfast for 2 , free dinner": { Meal = "Half Board For Two"; } break;
                case "continental breakfast for 2 , free lunch": { Meal = "Half Board For Two"; } break;
                case "continental breakfast for 2 , half board": { Meal = "Half Board For Two"; } break;
                case "continental breakfast for 2 , half board , half board (buffet dinner)": { Meal = "Half Board For Two"; } break;
                case "half board for 2": { Meal = "Half Board For Two"; } break;
                case "half board for 2 person": { Meal = "Half Board For Two"; } break;
                case "half board for two": { Meal = "Half Board For Two"; } break;
                case "2 meals included , 3 x breakfast , 3 x buffet dinner , 3 x complimentary breakfast , 3 x complimentary snacks , 3 x dinner included": { Meal = "Half Board For Three"; } break;
                case "2 meals included , 3 x breakfast , 3 x buffet dinner , 3 x dinner included , 3 x local breakfast": { Meal = "Half Board For Three"; } break;
                case "2 meals included , 3 x breakfast , 3 x complimentary breakfast , 3 x complimentary lunch , 3 x local breakfast , 3 x lunch included , 3 x vegetarian breakfast": { Meal = "Half Board For Three"; } break;
                case "2 meals included , 3 x breakfast , 3 x complimentary dinner": { Meal = "Half Board For Three"; } break;
                case "2 meals included , 3 x breakfast , 3 x complimentary dinner , 3 x complimentary snacks , 3 x dinner included": { Meal = "Half Board For Three"; } break;
                case "2 meals included , 3 x breakfast , 3 x complimentary dinner , 3 x dinner included": { Meal = "Half Board For Three"; } break;
                case "2 meals included , 3 x breakfast , 3 x complimentary snacks , 3 x dinner included , 3 x wagyu beef dinner": { Meal = "Half Board For Three"; } break;
                case "2 meals included , 3 x breakfast , 3 x dinner in private roo , 3 x dinner included , 3 x japanese dinner , 3 x kaiseki dinner": { Meal = "Half Board For Three"; } break;
                case "2 meals included , 3 x breakfast , 3 x dinner included": { Meal = "Half Board For Three"; } break;
                case "2 meals included , 3 x breakfast , 3 x dinner included , 3 x japanese dinner": { Meal = "Half Board For Three"; } break;
                case "2 meals included , 3 x breakfast , 3 x dinner included , 3 x japanese dinner , 3 x kaiseki dinner , 3 x wagyu beef dinner": { Meal = "Half Board For Three"; } break;
                case "2 meals included , 3 x breakfast , 3 x dinner included , airport transfer (two ways)": { Meal = "Half Board For Three"; } break;
                case "2 meals included , 3 x breakfast , 3 x japanese dinner": { Meal = "Half Board For Three"; } break;
                case "3 x breakfast , 3 x buffet dinner": { Meal = "Half Board For Three"; } break;
                case "3 x breakfast , 3 x buffet dinner , 3 x complimentary breakfast , 3 x dinner included": { Meal = "Half Board For Three"; } break;
                case "3 x breakfast , 3 x buffet dinner , 3 x complimentary breakfast , 3 x dinner included , 3 x local breakfast , breakfast for 2 people": { Meal = "Half Board For Three"; } break;
                case "3 x breakfast , 3 x buffet dinner , 3 x dinner included": { Meal = "Half Board For Three"; } break;
                case "3 x breakfast , 3 x complimentary breakfast , 3 x complimentary dinner": { Meal = "Half Board For Three"; } break;
                case "3 x breakfast , 3 x complimentary breakfast , 3 x complimentary dinner , 3 x dinner included": { Meal = "Half Board For Three"; } break;
                case "3 x breakfast , 3 x complimentary breakfast , 3 x complimentary lunch , 3 x local breakfast , 3 x lunch included , 3 x vegetarian breakfast": { Meal = "Half Board For Three"; } break;
                case "3 x breakfast , 3 x complimentary breakfast , 3 x dinner included": { Meal = "Half Board For Three"; } break;
                case "3 x breakfast , 3 x complimentary dinner": { Meal = "Half Board For Three"; } break;
                case "3 x breakfast , 3 x complimentary dinner , 3 x japanese dinner": { Meal = "Half Board For Three"; } break;
                case "3 x breakfast , 3 x dinner included": { Meal = "Half Board For Three"; } break;
                case "3 x breakfast , 3 x dinner included , 3 x japanese dinner": { Meal = "Half Board For Three"; } break;
                case "3 x breakfast , 3 x dinner included , 3 x kaiseki dinner": { Meal = "Half Board For Three"; } break;
                case "3 x breakfast , 3 x dinner included , 3 x local breakfast": { Meal = "Half Board For Three"; } break;
                case "3 x breakfast , 3 x dinner included , 3 x wagyu beef dinner": { Meal = "Half Board For Three"; } break;
                case "3 x breakfast , 3 x dinner included , 3 x wagyu beef dinner , 3 x western dinner": { Meal = "Half Board For Three"; } break;
                case "3 x breakfast , 3 x dinner included , 3 x western dinner": { Meal = "Half Board For Three"; } break;
                case "3 x breakfast , 3 x dinner included , airport transfer (two ways)": { Meal = "Half Board For Three"; } break;
                case "3 x breakfast , 3 x dinner included , airport transfer [both ways]": { Meal = "Half Board For Three"; } break;
                case "3 x breakfast , 3 x dinner included , airport transfer [both ways] , breakfast , dinner included": { Meal = "Half Board For Three"; } break;
                case "3 x breakfast , 3 x in-room dinner , 3 x kaiseki dinner": { Meal = "Half Board For Three"; } break;
                case "3 x breakfast , 3 x japanese dinner": { Meal = "Half Board For Three"; } break;
                case "3 x breakfast , 3 x lunch included": { Meal = "Half Board For Three"; } break;
                case "3 x breakfast , dinner included": { Meal = "Half Board For Three"; } break;
                case "3 x dinner included , 3 x lunch included": { Meal = "Half Board For Three"; } break;
                case "half board for three": { Meal = "Half Board For Three"; } break;
                case "4 x breakfast , 4 x buffet dinner , 4 x complimentary breakfast , 4 x dinner included": { Meal = "Half Board For Four"; } break;
                case "4 x breakfast , 4 x complimentary breakfast , 4 x complimentary dinner , 4 x dinner included": { Meal = "Half Board For Four"; } break;
                case "4 x breakfast , 4 x complimentary breakfast , 4 x dinner included": { Meal = "Half Board For Four"; } break;
                case "4 x breakfast , 4 x dinner included": { Meal = "Half Board For Four"; } break;
                case "4 x breakfast , 4 x dinner included , airport transfer [both ways] , breakfast , dinner included": { Meal = "Half Board For Four"; } break;
                case "4 x breakfast , 4 x lunch included": { Meal = "Half Board For Four"; } break;
                case "half board for four": { Meal = "Half Board For Four"; } break;
                case "1 x 3 meals daily , 1 x breakfast , 1 x dinner included , 1 x lunch included": { Meal = "Full Board For One"; } break;
                case "1 x breakfast , 1 x dinner included , 1 x lunch included": { Meal = "Full Board For One"; } break;
                case "full board for 1": { Meal = "Full Board For One"; } break;
                case "full board for 1 person": { Meal = "Full Board For One"; } break;
                case "full board for one": { Meal = "Full Board For One"; } break;
                case "2 x 3 meals daily , 2 x breakfast , 2 x buffet dinner , 2 x complimentary breakfast , 2 x dinner included , 2 x lunch included": { Meal = "Full Board For Two"; } break;
                case "2 x 3 meals daily , 2 x breakfast , 2 x buffet dinner , 2 x dinner included , 2 x lunch included": { Meal = "Full Board For Two"; } break;
                case "2 x 3 meals daily , 2 x breakfast , 2 x dinner included , 2 x lunch included": { Meal = "Full Board For Two"; } break;
                case "2 x breakfast , 2 x buffet dinner , 2 x complimentary lunch , 2 x dinner included , 2 x lunch included , 2 x vegetarian breakfast , breakfast for 2 people": { Meal = "Full Board For Two"; } break;
                case "2 x breakfast , 2 x buffet dinner , 2 x dinner included , 2 x lunch included": { Meal = "Full Board For Two"; } break;
                case "2 x breakfast , 2 x buffet dinner , 2 x dinner included , 2 x lunch included , breakfast for 2 people": { Meal = "Full Board For Two"; } break;
                case "2 x breakfast , 2 x complimentary breakfast , 2 x complimentary dinner , 2 x complimentary lunch , 2 x complimentary tickets , 2 x cruise dinner , 2 x dinner included , 2 x local breakfast , 2 x local island excursion , 2 x lunch included , 2 x vegetarian breakfast": { Meal = "Full Board For Two"; } break;
                case "2 x breakfast , 2 x complimentary breakfast , 2 x complimentary dinner , 2 x complimentary lunch , 2 x cruise dinner , 2 x dinner included , 2 x local breakfast , 2 x lunch included , 2 x vegetarian breakfast , 2 x vegetarian dinner , 2 x western dinner": { Meal = "Full Board For Two"; } break;
                case "2 x breakfast , 2 x complimentary breakfast , 2 x complimentary dinner , 2 x dinner included , 2 x lunch included": { Meal = "Full Board For Two"; } break;
                case "2 x breakfast , 2 x complimentary breakfast , 2 x dinner included , 2 x lunch included": { Meal = "Full Board For Two"; } break;
                case "2 x breakfast , 2 x dinner included , 2 x lunch included": { Meal = "Full Board For Two"; } break;
                case "2 x breakfast , 2 x dinner included , 2 x lunch included , airport transfer (two ways)": { Meal = "Full Board For Two"; } break;
                case "2 x breakfast , 2 x dinner included , 2 x lunch included , airport transfer [both ways]": { Meal = "Full Board For Two"; } break;
                case "2 x breakfast , 2 x dinner included , 2 x lunch included , breakfast for 2 people": { Meal = "Full Board For Two"; } break;
                case "3 x breakfast , 3 x buffet dinner , 3 x dinner included , 3 x lunch included , breakfast for 2 people": { Meal = "Full Board For Two"; } break;
                case "4 x breakfast , 4 x buffet dinner , 4 x dinner included , 4 x lunch included , breakfast for 2 people": { Meal = "Full Board For Two"; } break;
                case "all meals , breakfast for 2": { Meal = "Full Board For Two"; } break;
                case "breakfast , breakfast for 2 people , buffet dinner , complimentary lunch , dinner included , lunch included , vegetarian breakfast": { Meal = "Full Board For Two"; } break;
                case "breakfast , breakfast for 2 people , buffet dinner , dinner included , lunch included": { Meal = "Full Board For Two"; } break;
                case "breakfast , breakfast for 2 people , dinner included , lunch included": { Meal = "Full Board For Two"; } break;
                case "breakfast buffet , continental breakfast for 2 , english breakfast , full board , full breakfast": { Meal = "Full Board For Two"; } break;
                case "breakfast for 2 , buffet dinner , free lunch": { Meal = "Full Board For Two"; } break;
                case "breakfast for 2 , free breakfast , full board": { Meal = "Full Board For Two"; } break;
                case "breakfast for 2 , free dinner , free lunch": { Meal = "Full Board For Two"; } break;
                case "breakfast for 2 , full board": { Meal = "Full Board For Two"; } break;
                case "breakfast for 2 people , dinner included , lunch included": { Meal = "Full Board For Two"; } break;
                case "full board for 2": { Meal = "Full Board For Two"; } break;
                case "full board for 2 person": { Meal = "Full Board For Two"; } break;
                case "full board for two": { Meal = "Full Board For Two"; } break;
                case "3 x 3 meals daily , 3 x breakfast , 3 x buffet dinner , 3 x dinner included , 3 x lunch included": { Meal = "Full Board For Three"; } break;
                case "3 x 3 meals daily , 3 x breakfast , 3 x dinner included , 3 x lunch included": { Meal = "Full Board For Three"; } break;
                case "3 x breakfast , 3 x buffet dinner , 3 x dinner included , 3 x lunch included": { Meal = "Full Board For Three"; } break;
                case "3 x breakfast , 3 x complimentary breakfast , 3 x complimentary dinner , 3 x complimentary lunch , 3 x cruise dinner , 3 x dinner included , 3 x local breakfast , 3 x lunch included , 3 x vegetarian breakfast , 3 x vegetarian dinner , 3 x western dinner": { Meal = "Full Board For Three"; } break;
                case "3 x breakfast , 3 x complimentary breakfast , 3 x complimentary dinner , 3 x complimentary lunch , 3 x dinner included , 3 x lunch included": { Meal = "Full Board For Three"; } break;
                case "3 x breakfast , 3 x dinner included , 3 x lunch included": { Meal = "Full Board For Three"; } break;
                case "3 x breakfast , 3 x dinner included , 3 x lunch included , airport transfer (two ways)": { Meal = "Full Board For Three"; } break;
                case "full board for three": { Meal = "Full Board For Three"; } break;
                case "4 x breakfast , 4 x dinner included , 4 x lunch included": { Meal = "Full Board For Four"; } break;
                case "full board for four": { Meal = "Full Board For Four"; } break;
                case "1 x 3 meals daily , 1 x all inclusive , 1 x breakfast , 1 x complimentary snacks , 1 x dinner included , 1 x lunch included": { Meal = "All Inclusive For One"; } break;
                case "1 x all inclusive": { Meal = "All Inclusive For One"; } break;
                case "1 x all inclusive , 1 x breakfast": { Meal = "All Inclusive For One"; } break;
                case "1 x all inclusive , 1 x breakfast , 1 x dinner included , 1 x lunch included": { Meal = "All Inclusive For One"; } break;
                case "1 x all inclusive , 1 x breakfast , 1 x dinner included , 1 x lunch included , 1 x sunset fishing excursion": { Meal = "All Inclusive For One"; } break;
                case "all inclusive for 1": { Meal = "All Inclusive For One"; } break;
                case "all inclusive for one": { Meal = "All Inclusive For One"; } break;
                case "all meals , all-inclusive (food/beverages/snacks) , breakfast for 1 , vegetarian dinner": { Meal = "All Inclusive For One"; } break;
                case "all-inclusive (food/beverages/snacks) , breakfast for 1": { Meal = "All Inclusive For One"; } break;
                case "all-inclusive (food/beverages/snacks) , breakfast for 1 , full breakfast , vegetarian dinner": { Meal = "All Inclusive For One"; } break;
                case "2 x 3 meals daily , 2 x all inclusive , 2 x breakfast , 2 x buffet dinner , 2 x dinner included , 2 x lunch included": { Meal = "All Inclusive For Two"; } break;
                case "2 x 3 meals daily , 2 x all inclusive , 2 x breakfast , 2 x complimentary snacks , 2 x dinner included , 2 x lunch included": { Meal = "All Inclusive For Two"; } break;
                case "2 x 3 meals daily , 2 x all inclusive , 2 x breakfast , 2 x dinner included , 2 x lunch included": { Meal = "All Inclusive For Two"; } break;
                case "2 x all inclusive": { Meal = "All Inclusive For Two"; } break;
                case "2 x all inclusive , 2 x breakfast": { Meal = "All Inclusive For Two"; } break;
                case "2 x all inclusive , 2 x breakfast , 2 x complimentary snacks , 2 x dinner included , 2 x lunch included , 2 x snorkeling excursion , 2 x sunset cruise": { Meal = "All Inclusive For Two"; } break;
                case "2 x all inclusive , 2 x breakfast , 2 x dinner included , 2 x lunch included": { Meal = "All Inclusive For Two"; } break;
                case "2 x all inclusive , 2 x breakfast , 2 x dinner included , 2 x lunch included , 2 x sunset fishing excursion": { Meal = "All Inclusive For Two"; } break;
                case "2 x all inclusive , 2 x complimentary snacks , 2 x vegetarian breakfast": { Meal = "All Inclusive For Two"; } break;
                case "2 x all inclusive , airport transfer (two ways)": { Meal = "All Inclusive For Two"; } break;
                case "2-for-1 buffet , all meals , all-inclusive (food/beverages/snacks)": { Meal = "All Inclusive For Two"; } break;
                case "2-for-1 buffet , all meals , all-inclusive (food/beverages/snacks) , full board": { Meal = "All Inclusive For Two"; } break;
                case "4 x all inclusive , 4 x breakfast , 4 x buffet dinner , 4 x dinner included , 4 x lunch included , breakfast , breakfast for 2 people , dinner included , lunch included": { Meal = "All Inclusive For Two"; } break;
                case "all inclusive , breakfast , breakfast , breakfast for 2 people , buffet dinner , dinner included , dinner included , lunch included , lunch included": { Meal = "All Inclusive For Two"; } break;
                case "all inclusive for 2": { Meal = "All Inclusive For Two"; } break;
                case "all inclusive for 2 person": { Meal = "All Inclusive For Two"; } break;
                case "all inclusive for two": { Meal = "All Inclusive For Two"; } break;
                case "all meals , all-inclusive (food/beverages/snacks) , breakfast buffet , breakfast for 1 , breakfast for 2 , buffet dinner , continental breakfast , english breakfast": { Meal = "All Inclusive For Two"; } break;
                case "all-inclusive (food/beverages/snacks) , breakfast for 2": { Meal = "All Inclusive For Two"; } break;
                case "all-inclusive (food/beverages/snacks) , continental breakfast for 2 , food/beverage credit , free dinner , free lunch , full breakfast , half board": { Meal = "All Inclusive For Two"; } break;
                case "all-inclusive for 2 person": { Meal = "All Inclusive For Two"; } break;
                case "3 x 3 meals daily , 3 x all inclusive , 3 x breakfast , 3 x buffet dinner , 3 x complimentary breakfast , 3 x complimentary dinner , 3 x complimentary lunch , 3 x dinner included , 3 x lunch included": { Meal = "All Inclusive For Three"; } break;
                case "3 x 3 meals daily , 3 x all inclusive , 3 x breakfast , 3 x buffet dinner , 3 x dinner included , 3 x lunch included": { Meal = "All Inclusive For Three"; } break;
                case "3 x 3 meals daily , 3 x all inclusive , 3 x breakfast , 3 x complimentary snacks , 3 x dinner included , 3 x lunch included": { Meal = "All Inclusive For Three"; } break;
                case "3 x 3 meals daily , 3 x all inclusive , 3 x breakfast , 3 x dinner included , 3 x lunch included": { Meal = "All Inclusive For Three"; } break;
                case "3 x all inclusive": { Meal = "All Inclusive For Three"; } break;
                case "3 x all inclusive , 3 x breakfast": { Meal = "All Inclusive For Three"; } break;
                case "3 x all inclusive , 3 x breakfast , 3 x buffet dinner , 3 x dinner included , 3 x lunch included": { Meal = "All Inclusive For Three"; } break;
                case "3 x all inclusive , 3 x breakfast , 3 x complimentary snacks , 3 x dinner included , 3 x lunch included , 3 x snorkeling excursion , 3 x sunset cruise": { Meal = "All Inclusive For Three"; } break;
                case "3 x all inclusive , 3 x breakfast , 3 x dinner included , 3 x lunch included": { Meal = "All Inclusive For Three"; } break;
                case "3 x all inclusive , 3 x complimentary snacks , 3 x vegetarian breakfast": { Meal = "All Inclusive For Three"; } break;
                case "3 x all inclusive , airport transfer (two ways)": { Meal = "All Inclusive For Three"; } break;
                case "all inclusive for three": { Meal = "All Inclusive For Three"; } break;
                case "4 x 3 meals daily , 4 x all inclusive , 4 x breakfast , 4 x dinner included , 4 x lunch included": { Meal = "All Inclusive For Four"; } break;
                case "4 x all inclusive": { Meal = "All Inclusive For Four"; } break;
                case "4 x all inclusive , 4 x breakfast , 4 x dinner included , 4 x lunch included": { Meal = "All Inclusive For Four"; } break;
                case "all inclusive for four": { Meal = "All Inclusive For Four"; } break;
                case "dinner included , lunch included": { Meal = "Lunch And Dinner"; } break;
                case "free meal upgrade (dinner and lunch)": { Meal = "Lunch And Dinner"; } break;
                case "lunch and dinner": { Meal = "Lunch And Dinner"; } break;
                case "lunch and dinner with drinks": { Meal = "Lunch And Dinner"; } break;
                case "2 x lunch included": { Meal = "Lunch"; } break;
                case "3 x lunch included": { Meal = "Lunch"; } break;
                case "complimentary lunch": { Meal = "Lunch"; } break;
                case "free lunch": { Meal = "Lunch"; } break;
                case "lh": { Meal = "Lunch"; } break;
                case "lunch": { Meal = "Lunch"; } break;
                case "lunch (lun)": { Meal = "Lunch"; } break;
                case "lunch included": { Meal = "Lunch"; } break;
                case "lunch or dinner": { Meal = "Lunch"; } break;
                case "lunch without beverages": { Meal = "Lunch"; } break;
                case "2 x dinner included": { Meal = "Dinner"; } break;
                case "3 x dinner included": { Meal = "Dinner"; } break;
                case "3 x dinner included , 3 x japanese dinner": { Meal = "Dinner"; } break;
                case "basic dinner": { Meal = "Dinner"; } break;
                case "basic kaiseki dinner": { Meal = "Dinner"; } break;
                case "buffet dinner": { Meal = "Dinner"; } break;
                case "buffet dinner , free dinner": { Meal = "Dinner"; } break;
                case "casual kaiseki dinner": { Meal = "Dinner"; } break;
                case "chinese-style dinner": { Meal = "Dinner"; } break;
                case "complimentary dinner": { Meal = "Dinner"; } break;
                case "crab dinner": { Meal = "Dinner"; } break;
                case "cruise dinner": { Meal = "Dinner"; } break;
                case "dinner": { Meal = "Dinner"; } break;
                case "dinner (din)": { Meal = "Dinner"; } break;
                case "dinner in private room": { Meal = "Dinner"; } break;
                case "dinner included": { Meal = "Dinner"; } break;
                case "dinner included , japanese dinner": { Meal = "Dinner"; } break;
                case "dinner without beverages": { Meal = "Dinner"; } break;
                case "food/beverage credit , free dinner": { Meal = "Dinner"; } break;
                case "free dinner": { Meal = "Dinner"; } break;
                case "free dinner , kaiseki dinner": { Meal = "Dinner"; } break;
                case "free dinner , vegetarian dinner": { Meal = "Dinner"; } break;
                case "hot pot dinner": { Meal = "Dinner"; } break;
                case "in-room dinner": { Meal = "Dinner"; } break;
                case "japanese dinner": { Meal = "Dinner"; } break;
                case "japanese pufferfish dinner": { Meal = "Dinner"; } break;
                case "kaiseki dinner": { Meal = "Dinner"; } break;
                case "kaiseki meat dinner": { Meal = "Dinner"; } break;
                case "kaiseki seafood dinner": { Meal = "Dinner"; } break;
                case "local food dinner": { Meal = "Dinner"; } break;
                case "premium kaiseki dinner": { Meal = "Dinner"; } break;
                case "shrimp dinner": { Meal = "Dinner"; } break;
                case "special dinner": { Meal = "Dinner"; } break;
                case "vegetarian dinner": { Meal = "Dinner"; } break;
                case "vip only - free dinner": { Meal = "Dinner"; } break;
                case "wagyu beef dinner": { Meal = "Dinner"; } break;
                case "western dinner": { Meal = "Dinner"; } break;
                case "iftar": { Meal = "Iftar"; } break;
                case "iftar-nf": { Meal = "Iftar"; } break;
                case "sohour": { Meal = "Suhoor"; } break;
                case "sohour or iftar": { Meal = "Suhoor"; } break;
                case "suhoor": { Meal = "Suhoor"; } break;
                case "suhoor-nf": { Meal = "Suhoor"; } break;
                case "suhur": { Meal = "Suhoor"; } break;
                case "suhur or iftar": { Meal = "Suhoor"; } break;
                case "all inclusive soft": { Meal = "Soft Inclusive"; } break;
                case "all inclusive soft (as)": { Meal = "Soft Inclusive"; } break;
                case "soft all inclusive": { Meal = "Soft Inclusive"; } break;
                case "soft all inclusive dine around": { Meal = "Soft Inclusive"; } break;
                case "soft all inclusive-nf": { Meal = "Soft Inclusive"; } break;
                case "soft-all-inclusive": { Meal = "Soft Inclusive"; } break;
                case "ultra soft all inclusive": { Meal = "Soft Inclusive"; } break;
                default:
                    Meal = "Room Only";
                    break;
            }
            return Meal;
        }
        public bool SwapMealName(string AgentCode)
        {
            switch (AgentCode)
            {
                case "itrv":
                case "mgic":
                //case "scis": Temp undo dueto portalverification 5-jan-2024
                case "snap":
                case "stou":
                case "tjit":
                case "klnb":
                case "raku":
                case "lnda":
                case "inst":
                //case "mtbo"://Rollback on 28-JUN-2024 due to move direct (Iboosy)
                case "spet":
                case "fzcb":
                case "mwrl":
                case "expa":
                case "guji":
                //case "ostr"://due to move XTG to direct 26-APR-2024
                case "vibe":
                case "dazz":
                case "mapa":
                case "reis":
                case "pres":
                case "nigm":
                case "amtj":
                case "wbed":
                case "snpw":
                case "nutt":
                case "mgbb":
                //case "osmf"://due to move XTG to direct
                case "3pro":
                //case "oscu": //due to move XTG to direct 23-APR-2024
                case "mygo":
                //case "otti": //swap back on 29-APR-2025
                //case "otmh": //swap back on 21-JUL-2025
                case "fpk2":
                case "snmb":
                //case "fygi": //due to move direct 16-May-2025
                //case "lfly": /due to move XTG to direct 03-Dec-2024
                case "allb":
                case "bkag":
                case "dcs4":
                case "edro":
                case "cich":
                case "ntwn":
                case "smyr":
                //case "osrb"://due to move XTG to direct 25-APR-2024
                case "3nrd":
                case "2hsn":
                case "btgp":
                case "sspt"://Added on 19-JUN-2024
                case "t2lo"://Added on 19-JUN-2024
                case "spus"://Added on 19-JUN-2024
                case "t1vl"://Added on 19-JUN-2024
                case "sspm"://Added on 19-JUN-2024
                case "tatr"://Added on 24-JUN-2024
                case "dsud"://Added on 27-JUN-2024
                case "tnda"://Added on 17-JULY-2024
                case "raki"://Added on 25-JULY-2024
                case "gglb"://Added on 25-JULY-2024
                case "ease"://Added on 01-AUG-2024
                case "alnk"://Added on 25-SEP-2024
                //case "rois"://Added on 25-SEP-2024 swap back dueto moveto gimonix
                case "t2sl"://Added on 14-OCT-2024
                case "tr2g"://Added on 06-NOV-2024
                case "snmt"://Added on 12-NOV-2024
                case "riyh"://Added on 28-JAN-2025
                case "hhuk"://Added on 04-JUN-2025
                //case "citd"://Added on 17-JUN-2025  //swap back on 11-NOV-2024
                case "sebc"://Added on 12-JUL-2025
                case "ma05"://Added on 05-AUG-2025
                case "me02"://Added on 05-AUG-2025
                case "odeo"://Added on 14-AUG-2025
                case "t5vb"://Added on 14-AUG-2025
                case "ab7l"://Added on 28-AUG-2025
                case "mtq7"://Added on 05-SEP-2025
                case "cobd"://Added on 09-SEP-2025
                case "blbk"://Added on 18-SEP-2025
                case "bm7k"://Added on 18-SEP-2025
                case "m2tz"://Added on 24-SEP-2025
                case "joas"://Added on 30-SEP-2025
                case "lien"://Added on 30-SEP-2025
                case "g2ft"://Added on 27-OCT-2025
                case "ml6n"://Added on 28-NOV-2025
                case "slvb"://Added on 28-NOV-2025
                case "ch2n"://Added on 11-DEC-2025 

                    return true;
                default:
                    return false;
            }
        }

        public static List<List<X_Roomdetails>> MergeRooms(List<X_Roomdetails>[] array, HotelSearchParameter SearchPara)
        {
            List<List<X_Roomdetails>> results = new List<List<X_Roomdetails>>() {
                new List<X_Roomdetails>()
            };
            if (array.Length == SearchPara.Rooms.Count)
            {
                for (var i = 0; i < array.Length; i++)
                {
                    var currentSubArray = array[i];
                    if (currentSubArray == null)
                        return new List<List<X_Roomdetails>>() {
                new List<X_Roomdetails>()
            };
                    List<List<X_Roomdetails>> temp = new List<List<X_Roomdetails>>();
                    for (var j = 0; j < results.Count; j++)
                    {
                        for (var k = 0; k < currentSubArray.Count; k++)
                        {

                            if (currentSubArray[k].SupplierName.ToLower() == "dotw")
                            {
                                List<X_Roomdetails> test = results[j];
                                List<X_Roomdetails> list = new List<X_Roomdetails>();
                                list.Add(currentSubArray[k]);
                                test = test.Concat(list).ToList();
                                List<String> arrmeal = new List<string>();
                                arrmeal.AddRange(test.Select(s => s.RoomTypeCode).Distinct().ToList());
                                if (arrmeal.Count == 1)
                                    temp.AddRoomToList(currentSubArray[k], results[j]);


                            }
                            else if (currentSubArray[k].SupplierName.ToLower() == "oth")
                            {
                                List<X_Roomdetails> test = results[j];
                                List<X_Roomdetails> list = new List<X_Roomdetails>();
                                list.Add(currentSubArray[k]);
                                test = test.Concat(list).ToList();
                                List<String> arrmeal = new List<string>();
                                arrmeal.AddRange(test.Select(s => s.BookingStatus).Distinct().ToList());

                                List<String> arrRoom = new List<string>();
                                arrRoom.AddRange(test.Select(s => s.RoomTypeId).Distinct().ToList());

                                if (arrmeal.Count == 1 && arrRoom.Count == 1)
                                    temp.AddRoomToList(currentSubArray[k], results[j]);


                            }
                            else
                            {
                                //temp.AddRoomToList(currentSubArray[k], results[j]);
                                List<X_Roomdetails> test = results[j];
                                List<X_Roomdetails> list = new List<X_Roomdetails>();
                                list.Add(currentSubArray[k]);
                                test = test.Concat(list).ToList();
                                List<String> arrmeal = new List<string>();
                                arrmeal.AddRange(test.Select(s => s.RoomTypeName).Distinct().ToList());
                                if (arrmeal.Count == 1)
                                    temp.AddRoomToList(currentSubArray[k], results[j]);
                            }
                        }
                    }
                    results = temp;
                }
            }
            return results;
        }

        public Options GetDataFromHotelRow(DataRow dataRow)
        {
            Options hotelListNew = new Options();
            try
            {
                string OptionId = dataRow.EncryptHotelToken();
                hotelListNew.HotelCode = Convert.ToString(dataRow["GIITACode"]);
                hotelListNew.HotelId = Convert.ToInt64(dataRow["RaynaId"]);
                hotelListNew.HotelOptionId = OptionId;
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
            }
            return hotelListNew;
        }


    }
}
