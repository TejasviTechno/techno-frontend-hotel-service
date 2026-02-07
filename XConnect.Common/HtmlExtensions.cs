using System.Collections.Concurrent;
using System.ComponentModel;
using System.Data;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Protocols;
using XConnect.CommonUtilities.Interface;
using XConnect.Models;
using XConnect.Models.Hotel;
using static Azure.Core.HttpHeader;
using static XConnect.Models.CacheMaster;


namespace XConnect.CommonUtilities
{
    public static class HtmlExtensions
    {
        private static readonly IConfiguration _config;



        public static double TruncateDecimalPlaces(this double val, int places)
        {
            if (places < 0)
            {
                throw new ArgumentException("places");
            }
            return Math.Round(val - Convert.ToDouble((0.5 / Math.Pow(10, places))), places);
        }
        public static List<TSource> ToList<TSource>(this DataTable dataTable) where TSource : new()
        {
            var dataList = new List<TSource>();

            const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic;
            var objFieldNames = (from PropertyInfo aProp in typeof(TSource).GetProperties(flags)
                                 select new
                                 {
                                     Name = aProp.Name,
                                     Type = Nullable.GetUnderlyingType(aProp.PropertyType) ?? aProp.PropertyType
                                 }).ToList();
            var dataTblFieldNames = (from DataColumn aHeader in dataTable.Columns
                                     select new { Name = aHeader.ColumnName, Type = aHeader.DataType }).ToList();
            var commonFields = objFieldNames.Intersect(dataTblFieldNames).ToList();

            foreach (DataRow dataRow in dataTable.AsEnumerable().ToList())
            {
                var aTSource = new TSource();
                foreach (var aField in commonFields)
                {
                    PropertyInfo propertyInfos = aTSource.GetType().GetProperty(aField.Name);
                    var value = (dataRow[aField.Name] == DBNull.Value) ? null : dataRow[aField.Name]; //if database field is nullable
                    //propertyInfos.SetValue(aTSource, dataRow[aField.Name], null);
                    propertyInfos.SetValue(aTSource, value, null);
                }
                dataList.Add(aTSource);
            }
            return dataList;
        }


        public static bool IsBase64String(this string s)
        {

            if (s.Length > 0 && s != null && s != "")
                s = s.Trim();
            return (s.Length % 4 == 0) && Regex.IsMatch(s, @"^[a-zA-Z0-9\+/]*={0,3}$", RegexOptions.None);

        }

        public static string AgentWiseValidation(this HotelSearchParameter RQ, Int32 AgentId)
        {
            try
            {
                if (AgentId == 4440)
                {
                    bool IsCityBlock = CountryBlock(RQ.CountryID);
                    if (IsCityBlock == true)
                    {
                        return "Requested City is not allow for this account !!";
                    }
                }
                return "";
            }
            catch (Exception ex)
            {
                //Common.Error(ex);
                return "";
            }
        }

        public static bool CountryBlock(Int32 CountryId)
        {
            try
            {
                bool IsBlock = true;
                switch (CountryId)// Only get response in Saudi Arabia Ticket#12938
                {
                    case 15471: { IsBlock = false; } break;

                    default:
                        return true;
                }
                return IsBlock;
            }
            catch (Exception ex)
            {
                //Common.Error(ex);
                return true;
            }
        }

        public static string EncryptSearchKey(this HotelSearchParameter SearchPara)
        {
            StringBuilder str = new StringBuilder();

            try
            {
                str.Append(SearchPara.CheckInDate + "|" + SearchPara.CheckOutDate + "|" + SearchPara.CityID + "|" + SearchPara.CountryID + "|" + SearchPara.Nationality + "|" + SearchPara.NoofNights + "|" + SearchPara.Rooms.Count + "|" + SearchPara.Filters.IsOnlyAvailable + "|" + SearchPara.CountryCode + "|" + SearchPara.Filters.IsRecommendedOnly + "|" + SearchPara.Filters.IsShowRooms);
                str.Append("|");
                for (int i = 0; i < SearchPara.Rooms.Count; i++)
                {
                    str.Append((i + 1) + "~" + SearchPara.Rooms[i].NoofAdults + "~" + SearchPara.Rooms[i].NoofChild + "~" + SearchPara.Rooms[i].Child1Age + "~" + SearchPara.Rooms[i].Child2Age + "]");
                }
                str.Append("|");
                str.Append(Guid.NewGuid().ToString().Substring(0, 8));
                str.Append("|" + SearchPara.AgentCompanyPercentage); //added by jay

            }
            catch (Exception ex)
            {
                //Common.Error(ex);
            }
            return str.ToString().ToBase64StringManual(false);
        }

        public static string ToBase64StringManual(this string str, bool Protected)
        {

            if (Protected)
                return Convert.ToBase64String(
                     ProtectedData.Protect(Encoding.Unicode.GetBytes(str), null, DataProtectionScope.LocalMachine));
            else
                return str;

        }

        public static string ReverseString(this string s)
        {
            char[] charArray = s.ToCharArray();
            Array.Reverse(charArray);
            return new string(charArray);
        }

        public static string SeperateID = "|7^|";
        public static string SeperatorForCacheRoom = "|";
        public static string SecretKey = "A27ca5898a4e4177bbce2XA2315a981X";//secret key for the symmetric algorithm
        public static string EncryptHotelToken(this DataRow dataRow)
        {
            try
            {

                string[] arrRoomToken = {
                    /*0*/Convert.ToString(dataRow["OwnsystemHotelId"]),
                    /*1*/Convert.ToString(dataRow["XmlHotelCity"]),
                    /*2*/Convert.ToString(dataRow["XMLHotelCode"]),
                    /*3*/Convert.ToString(dataRow["XMLCountry"]),
                    /*4*/Convert.ToString(dataRow["XMLCity"]),
                    /*5*/Convert.ToString(dataRow["XMLCode"]),
                    /*6*/Convert.ToString(dataRow["Rating"]),
                    /*7*/Convert.ToString(dataRow["FinalSellingPrice"]),
                    /*8*/Convert.ToString(dataRow["OfficeID"]),
                    /*9*/Convert.ToString(dataRow["ContractID"]),
                    /*10*/Convert.ToString(dataRow["ContractName"]),
                    /*11*/Convert.ToString(dataRow["AvailToken"]),
                    /*12*/Convert.ToString(dataRow["GIITACode"]),
                    /*13*/Convert.ToString(dataRow["RaynaId"]),
                    /*14*/Convert.ToString(dataRow["SupplierId"]),
                    /*15*/Convert.ToString(dataRow["SupplierCurrency"]),
                    /*16*/Convert.ToString(dataRow["MasterSupplierId"]),
                    /*17*/Convert.ToString(dataRow["HotelName"]),
                    /*18*/Convert.ToString(dataRow["HotelContactNo"]),
                    /*19*/Convert.ToString(dataRow["ReservationEmail"]),
                    /*20*/Convert.ToString(dataRow["SupplierName"])

                            };
                return string.Join(SeperateID, arrRoomToken);//.ToBase64StringManual();

            }
            catch (Exception ex)
            {
               // Common.Error(ex);
            }
            return "";
        }

        public static string SeperatorRow = "[~]";
        public static string EncryptPolicyToken(this X_PolicyDetails PolicyDetails)
        {
            try
            {

                //add item above seperator 
                string[] arrycancellationpolicy = {
                                        /*0*/PolicyDetails.RoomNo,
                                        /*1*/PolicyDetails.FromDate,
                                        /*2*/PolicyDetails. ToDate,
                                        /*3*/PolicyDetails.CompanyCancellationPrice,
                                        /*4*/PolicyDetails.AgentCancellationPrice,
                                        /*5*/PolicyDetails.PurchaseToken,
                                        /*6*/PolicyDetails.SPUI,
                                        /*7*/PolicyDetails.BuyingPrice,
                                        //add item here
                                        SeperatorRow
                                    };
                return string.Join(SeperateID, arrycancellationpolicy);//add to policy token

            }
            catch (Exception ex)
            {
                //Common.Error(ex);
            }
            return "";
        }

        public static string EncryptRateBreakUp(this X_RateBreakupDetails rateBreakup)
        {
            try
            {

                string[] arrRoomRateDetails = { 
                                        /*0*/rateBreakup.RoomNo ,
                                        /*1*/rateBreakup.Date,
                                        /*2*/rateBreakup.DateOfWeek,
                                        /*3*/rateBreakup.Rate,
                                        /*4*/rateBreakup.BoardTypeID,
                                        /*5*/rateBreakup.Surcharge,
                                        /*6*/rateBreakup.FinalRateType,
                                        /*7*/rateBreakup.RateType,
                                        /*8*/rateBreakup.CompanyRate,
                                        /*9*/rateBreakup.Total,

                                        //add item here
                                        SeperatorRow
                                    };
                return string.Join(SeperateID, arrRoomRateDetails);

            }
            catch (Exception ex)
            {
                //Common.Error(ex);
            }
            return "";
        }

        public static string EncryptRoomToken(this X_Roomdetails roomtypearr)
        {
            try
            {

                // do not change the order of the array ,always add item at last 
                string[] arrRoomToken = {
                                /*0*/roomtypearr.AgentBuyingPrice,  
                                /*1*/roomtypearr.AgentSellingPrice,
                                /*2*/roomtypearr.allocationDetails,
                                /*3*/roomtypearr.BoardTypeID,
                                /*4*/roomtypearr.BookingStatus,
                                /*5*/roomtypearr.CompanyBuyingPrice,
                                /*6*/Convert.ToString(roomtypearr.CompanyBuyingPriceExpedia.DecimalToString()),
                                /*7*/roomtypearr.CompanySellingPrice,
                                /*8*/roomtypearr.ContractID,
                                /*9*/roomtypearr.ContractName,
                                /*10*/roomtypearr.DateApplyMinStay,
                                /*11*/roomtypearr.DiscountPer,
                                /*12*/Convert.ToString(roomtypearr.FinalSellingPrice.DecimalToString()),
                                /*13*/roomtypearr.GIITACode,
                                /*14*/Convert.ToString(roomtypearr.GrossProfitOnline.DecimalToString()),
                                /*15*/Convert.ToString(roomtypearr.IsCombineRoom),
                                /*16*/Convert.ToString(roomtypearr.IsPolicyDynamic),
                                /*17*/Convert.ToString(roomtypearr.LoyaltyPoints),
                                /*18*/roomtypearr.MasterMealId,
                                /*19*/roomtypearr.MealId,
                                /*20*/roomtypearr.MealName,
                                /*21*/roomtypearr.minStay,
                                /*22*/roomtypearr.OfferCode,
                                /*23*/roomtypearr.OfficeId,
                                /*24*/roomtypearr.OriginalMeal,
                                /*25*/Convert.ToString(roomtypearr.OurProfit.DecimalToString()),
                                /*26*/roomtypearr.OwnsystemHotelId,
                                /*27*/roomtypearr.RaynaId,
                                /*28*/roomtypearr.RoomNo,
                                /*29*/roomtypearr.RoomTypeCode,
                                /*30*/roomtypearr.RoomTypeId,
                                /*31*/roomtypearr.RoomTypeName,
                                /*32*/roomtypearr.SHRUI,
                                /*33*/roomtypearr.SubAgentBuyingPice,
                                /*34*/roomtypearr.SubAgentSellingPrice,
                                /*35*/roomtypearr.SupplierCurrency,
                                /*36*/roomtypearr.SupplierId,
                                /*37*/roomtypearr.TotalSurcharge,
                                /*38*/roomtypearr.XMLCITY_ID,
                                /*39*/roomtypearr.XMLHotelCode,
                                /*40*/roomtypearr.SupplierName,
                                /*41*/roomtypearr.XMLHotelId,
                                /*42*/roomtypearr.OTHPromoCode,
                                /*43*/roomtypearr.Type,
                                  /*44*/roomtypearr.MappedMealName
                                // add item from here

                            };
                return string.Join(SeperateID, arrRoomToken).ToBase64StringManual(false);

            }
            catch (Exception ex)
            {
                //Common.Error(ex);
            }
            return "";
        }

        public static string DecimalToString(this decimal? dec)
        {
            return (dec == null ? "0" : Convert.ToString(dec));
        }

        public class SortRoomByPrice : IComparer<X_Roomdetails>
        {
            #region IComparer<Employee> Members

            public int Compare(X_Roomdetails x, X_Roomdetails y)
            {
                if (x.FinalSellingPrice > y.FinalSellingPrice) return 1;
                else if (x.FinalSellingPrice < y.FinalSellingPrice) return -1;
                else return 0;
            }

            #endregion
        }

        public static T CastList<T>(this Object myobj)
        {
            Type objectType = myobj.GetType();
            Type target = typeof(T);
            var x = Activator.CreateInstance(target, false);
            var z = from source in objectType.GetMembers().ToList()
                    where source.MemberType == MemberTypes.Property
                    select source;
            var d = from source in target.GetMembers().ToList()
                    where source.MemberType == MemberTypes.Property
                    select source;
            List<MemberInfo> members = d.Where(memberInfo => d.Select(c => c.Name)
               .ToList().Contains(memberInfo.Name)).ToList();
            PropertyInfo propertyInfo;
            object value;
            foreach (var memberInfo in members)
            {
                propertyInfo = typeof(T).GetProperty(memberInfo.Name);
                value = myobj.GetType().GetProperty(memberInfo.Name).GetValue(myobj, null);

                propertyInfo.SetValue(x, value, null);
            }
            return (T)x;
        }

        public static List<List<X_Roomdetails>> AddRoomToList(this List<List<X_Roomdetails>> temp, X_Roomdetails currentSubArray, List<X_Roomdetails> results)
        {
            try
            {
                List<X_Roomdetails> list = new List<X_Roomdetails>();
                list.Add(currentSubArray);
                temp.Add(results.Concat(list).ToList());
            }
            catch (Exception ex)
            {
                //Common.Error(ex);
            }
            return temp;
        }

        public static IEnumerable<T> ReturnOnly<T>(this List<T> locations, int nSize)
        {
            for (int i = 0; i < locations.Count; i++)
            {
                if (i < nSize)
                    yield return locations[i];
            }
        }

        public static decimal CalculatePrice(this decimal price, TokenData tokenData, AdvancedOptions advancedOptions, string SupplierCurrency, int DecimalDigitRound = 2)
        {
            if (advancedOptions.Hit > 0)
            {
                return price;
            }
            if (advancedOptions.Currency.Trim().ToLower() == tokenData.CreditLimitCurrencyName.Trim().ToLower() && SupplierCurrency.Trim().ToLower() == tokenData.CreditLimitCurrencyName.Trim().ToLower())
            {
                price = price / tokenData.apiCurrency.BuyingCurrencyFactor;
            }
            else
            {
                price = price * tokenData.apiCurrency.SellingCurrencyFactor;
            }

            //return Math.Round(price, 2);
            return Math.Round(price, DecimalDigitRound); // Due to Ticket ID#29678 // send 3 digit price at each room in prebook only

        }
        public static decimal Ignore_DecimalPlaces(this decimal val, int places)
        {
            string StrVal = Convert.ToString(val);
            if (StrVal.Contains("."))
            {
                StrVal = StrVal.Split('.')[0];
            }
            return Convert.ToDecimal(StrVal);
        }

        public static DataTable ConvertToDataTableforConcurrentBag<T>(ConcurrentBag<T> data)
        {
            PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(typeof(T));
            DataTable table = new DataTable();
            foreach (PropertyDescriptor prop in properties)
                table.Columns.Add(prop.Name, Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType);

            foreach (T item in data)
            {
                DataRow row = table.NewRow();
                foreach (PropertyDescriptor prop in properties)
                    row[prop.Name] = prop.GetValue(item) ?? DBNull.Value;
                table.Rows.Add(row);
            }
            return table;
        }

        public class SortOptionByPrice : IComparer<Options>
        {
            #region IComparer<Employee> Members

            public int Compare(Options x, Options y)
            {
                if (x.MinPrice > y.MinPrice) return 1;
                else if (x.MinPrice < y.MinPrice) return -1;
                else return 0;
            }

            #endregion
        }

        public class SortHotelByPrice : IComparer<_HotelListNew>
        {
            #region IComparer<Employee> Members

            public int Compare(_HotelListNew x, _HotelListNew y)
            {
                if (x.StartPrice > y.StartPrice) return 1;
                else if (x.StartPrice < y.StartPrice) return -1;
                else return 0;
            }

            #endregion
        }

        public enum _HotelToken
        {
            /*0*/
            OwnsystemHotelId = 0,
            /*1*/
            XmlHotelCity = 1,
            /*2*/
            XMLHotelCode = 2,
            /*3*/
            XMLCountry = 3,
            /*4*/
            XMLCity = 4,
            /*5*/
            XMLCode = 5,
            /*6*/
            Rating = 6,
            /*7*/
            FinalSellingPrice = 7,
            /*8*/
            OfficeID = 8,
            /*9*/
            ContractID = 9,
            /*10*/
            ContractName = 10,
            /*11*/
            AvailToken = 11,
            /*12*/
            GIITACode = 12,
            /*13*/
            RaynaId = 13,
            /*14*/
            SupplierId = 14,
            /*15*/
            SupplierCurrency = 15,
            /*16*/
            MasterSupplierId = 16,
            /*17*/
            HotelName = 17,
            /*18*/
            HotelContactNo = 18,
            /*19*/
            ReservationEmail = 19,
            SupplierName = 20

        }
        public static _HotelList DecryptHotelToken(this string hotelToken)
        {

            _HotelList _HotelList = new _HotelList();
            try
            {

                // string[] arr = hotelToken.ToStringFrom64Manual().Split(new string[] { HotelXML.SeperateID }, StringSplitOptions.None);
                string[] arr = hotelToken.Split(new string[] { SeperateID }, StringSplitOptions.None);

                /*0*/
                _HotelList.OwnsystemHotelId = arr[Convert.ToInt32(_HotelToken.OwnsystemHotelId)];
                /*1*/
                _HotelList.XmlHotelCity = arr[Convert.ToInt32(_HotelToken.XmlHotelCity)];
                /*2*/
                _HotelList.XMLHotelCode = arr[Convert.ToInt32(_HotelToken.XMLHotelCode)];
                /*3*/
                _HotelList.XMLCountry = arr[Convert.ToInt32(_HotelToken.XMLCountry)];
                /*4*/
                _HotelList.XMLCity = arr[Convert.ToInt32(_HotelToken.XMLCity)];
                /*5*/
                _HotelList.XMLCode = arr[Convert.ToInt32(_HotelToken.XMLCode)];
                /*6*/
                _HotelList.Rating = Convert.ToInt32(arr[Convert.ToInt32(_HotelToken.Rating)]);
                /*7*/
                _HotelList.FinalSellingPrice = Convert.ToDouble(arr[Convert.ToInt32(_HotelToken.FinalSellingPrice)]);
                /*8*/
                _HotelList.OfficeID = arr[Convert.ToInt32(_HotelToken.OfficeID)];
                /*9*/
                _HotelList.ContractID = Convert.ToInt32(arr[Convert.ToInt32(_HotelToken.ContractID)]);
                /*10*/
                _HotelList.ContractName = arr[Convert.ToInt32(_HotelToken.ContractName)];
                /*11*/
                _HotelList.AvailToken = arr[Convert.ToInt32(_HotelToken.AvailToken)];
                /*12*/
                _HotelList.GIITACode = arr[Convert.ToInt32(_HotelToken.GIITACode)];
                /*13*/
                _HotelList.RaynaId = arr[Convert.ToInt32(_HotelToken.RaynaId)];
                /*14*/
                _HotelList.SupplierId = arr[Convert.ToInt32(_HotelToken.SupplierId)];
                /*15*/
                _HotelList.SupplierCurrency = arr[Convert.ToInt32(_HotelToken.SupplierCurrency)];
                /*16*/
                _HotelList.MasterSupplierId = Convert.ToInt32(arr[Convert.ToInt32(_HotelToken.MasterSupplierId)]);
                /*17*/
                _HotelList.HotelName = Convert.ToString(arr[Convert.ToInt32(_HotelToken.HotelName)]);
                /*18*/
                _HotelList.HotelContactNo = Convert.ToString(arr[Convert.ToInt32(_HotelToken.HotelContactNo)]);
                /*19*/
                _HotelList.ReservationEmail = Convert.ToString(arr[Convert.ToInt32(_HotelToken.ReservationEmail)]);
                _HotelList.SupplierName = Convert.ToString(arr[Convert.ToInt32(_HotelToken.SupplierName)]);


            }
            catch (Exception ex)
            {
                //Common.Error(ex);
            }
            return _HotelList;

        }

        public static string EncryptCacheRoomToken(this CacheRoomToken roomToken)
        {
            try
            {
                //add item above seperator 
                int[] arr = {
                                  /*0*/roomToken.giataid_Seq,
                                   /*1*/roomToken.hotelid_Seq,
                                   /*2*/roomToken.Room_seqno,
                                    /*3*/roomToken.roomno
                                    };
                return Convert.ToBase64String(Encoding.UTF8.GetBytes(string.Join(SeperatorForCacheRoom, arr)));
                //return Convert.ToBase64String ();
            }
            catch (Exception ex)
            {
                //Common.Error(ex);
            }
            return "";
        }


        #region Recheck - 270525

        public enum _RoomToken
        {
            /*0*/
            AgentBuyingPrice,
            /*1*/
            AgentSellingPrice,
            /*2*/
            allocationDetails,
            /*3*/
            BoardTypeID,
            /*4*/
            BookingStatus,
            /*5*/
            CompanyBuyingPrice,
            /*6*/
            CompanyBuyingPriceExpedia,
            /*7*/
            CompanySellingPrice,
            /*8*/
            ContractID,
            /*9*/
            ContractName,
            /*10*/
            DateApplyMinStay,
            /*11*/
            DiscountPer,
            /*12*/
            FinalSellingPrice,
            /*13*/
            GIITACode,
            /*14*/
            GrossProfitOnline,
            /*15*/
            IsCombineRoom,
            /*16*/
            IsPolicyDynamic,
            /*17*/
            LoyaltyPoints,
            /*18*/
            MasterMealId,
            /*19*/
            MealId,
            /*20*/
            MealName,
            /*21*/
            minStay,
            /*22*/
            OfferCode,
            /*23*/
            OfficeId,
            /*24*/
            OriginalMeal,
            /*25*/
            OurProfit,
            /*26*/
            OwnsystemHotelId,
            /*27*/
            RaynaId,
            /*28*/
            RoomNo,
            /*29*/
            RoomTypeCode,
            /*30*/
            RoomTypeId,
            /*31*/
            RoomTypeName,
            /*32*/
            SHRUI,
            /*33*/
            SubAgentBuyingPice,
            /*34*/
            SubAgentSellingPrice,
            /*35*/
            SupplierCurrency,
            /*36*/
            SupplierId,
            /*37*/
            TotalSurcharge,
            /*38*/
            XMLCITY_ID,
            /*39*/
            XMLHotelCode,

            /*40*/
            SupplierName,
            /*41*/
            XMLHotelId,
            /*42*/
            OTHPromoCode,
            /*43*/
            Type,
            //44
            MappedMealName,


        }
        public static CacheRoomToken DecryptCacheRoomToken(this string RoomTokenFromRequest)
        {
            CacheRoomToken cacheRoomToken = new CacheRoomToken();
            try
            {
                RoomTokenFromRequest = Encoding.UTF8.GetString(Convert.FromBase64String(RoomTokenFromRequest));
                string[] arr = RoomTokenFromRequest.Split(new string[] { SeperatorForCacheRoom }, StringSplitOptions.None);
                if (arr.Length > 1)
                {
                    cacheRoomToken.giataid_Seq = Convert.ToInt32(arr[Convert.ToInt32(_cacheRoomToken.Giataid)]);
                    cacheRoomToken.hotelid_Seq = Convert.ToInt32(arr[Convert.ToInt32(_cacheRoomToken.HotelId)]);
                    cacheRoomToken.Room_seqno = Convert.ToInt32(arr[Convert.ToInt32(_cacheRoomToken.RoomSeq)]);
                    cacheRoomToken.roomno = Convert.ToInt32(arr[Convert.ToInt32(_cacheRoomToken.RoomNo)]);
                    return cacheRoomToken;
                }
                return null;
            }
            catch (Exception ex)
            {
                //Common.Error(ex);
            }
            return null;
        }

        public static string ToStringFrom64Manual(this string str, bool Protected)
        {
            if (Protected)
                return Encoding.Unicode.GetString(
                    ProtectedData.Unprotect(Convert.FromBase64String(str), null, DataProtectionScope.LocalMachine));
            else
                return str;


        }

        public static X_Roomdetails DecryptRoomToken(this string roomtoken)
        {
            X_Roomdetails roomtypearr = new X_Roomdetails();
            string[] arr = roomtoken.ToStringFrom64Manual(false).Split(new string[] { SeperateID }, StringSplitOptions.None);
            try
            {
                /*0*/
                roomtypearr.AgentBuyingPrice = arr[Convert.ToInt32(_RoomToken.AgentBuyingPrice)];
                /*1*/
                roomtypearr.AgentSellingPrice = arr[Convert.ToInt32(_RoomToken.AgentSellingPrice)];
                /*2*/
                roomtypearr.allocationDetails = arr[Convert.ToInt32(_RoomToken.allocationDetails)];
                /*3*/
                roomtypearr.BoardTypeID = arr[Convert.ToInt32(_RoomToken.BoardTypeID)];
                /*4*/
                roomtypearr.BookingStatus = arr[Convert.ToInt32(_RoomToken.BookingStatus)];
                /*5*/
                roomtypearr.CompanyBuyingPrice = arr[Convert.ToInt32(_RoomToken.CompanyBuyingPrice)];
                /*6*/
                (roomtypearr.CompanyBuyingPriceExpedia) = Convert.ToDecimal(arr[Convert.ToInt32(_RoomToken.CompanyBuyingPriceExpedia)]);
                /*7*/
                roomtypearr.CompanySellingPrice = arr[Convert.ToInt32(_RoomToken.CompanySellingPrice)];
                /*8*/
                roomtypearr.ContractID = arr[Convert.ToInt32(_RoomToken.ContractID)];
                /*9*/
                roomtypearr.ContractName = arr[Convert.ToInt32(_RoomToken.ContractName)];
                /*10*/
                roomtypearr.DateApplyMinStay = arr[Convert.ToInt32(_RoomToken.DateApplyMinStay)];
                /*11*/
                roomtypearr.DiscountPer = arr[Convert.ToInt32(_RoomToken.DiscountPer)];
                /*12*/
                (roomtypearr.FinalSellingPrice) = Convert.ToDecimal(arr[Convert.ToInt32(_RoomToken.FinalSellingPrice)]);
                /*13*/
                roomtypearr.GIITACode = arr[Convert.ToInt32(_RoomToken.GIITACode)];
                /*14*/
                (roomtypearr.GrossProfitOnline) = Convert.ToDecimal(arr[Convert.ToInt32(_RoomToken.GrossProfitOnline)]);
                /*15*/
                (roomtypearr.IsCombineRoom) = Convert.ToBoolean(arr[Convert.ToInt32(_RoomToken.IsCombineRoom)]);
                /*16*/
                (roomtypearr.IsPolicyDynamic) = Convert.ToBoolean(arr[Convert.ToInt32(_RoomToken.IsPolicyDynamic)]);
                /*17*/
                (roomtypearr.LoyaltyPoints) = Convert.ToInt32(arr[Convert.ToInt32(_RoomToken.LoyaltyPoints)]);
                /*18*/
                roomtypearr.MasterMealId = arr[Convert.ToInt32(_RoomToken.MasterMealId)];
                /*19*/
                roomtypearr.MealId = arr[Convert.ToInt32(_RoomToken.MealId)];
                /*20*/
                roomtypearr.MealName = arr[Convert.ToInt32(_RoomToken.MealName)];
                /*21*/
                roomtypearr.minStay = arr[Convert.ToInt32(_RoomToken.minStay)];
                /*22*/
                roomtypearr.OfferCode = arr[Convert.ToInt32(_RoomToken.OfferCode)];
                /*23*/
                roomtypearr.OfficeId = arr[Convert.ToInt32(_RoomToken.OfficeId)];
                /*24*/
                roomtypearr.OriginalMeal = arr[Convert.ToInt32(_RoomToken.OriginalMeal)];
                /*25*/
                (roomtypearr.OurProfit) = Convert.ToDecimal(arr[Convert.ToInt32(_RoomToken.OurProfit)]);
                /*26*/
                roomtypearr.OwnsystemHotelId = arr[Convert.ToInt32(_RoomToken.OwnsystemHotelId)];
                /*27*/
                roomtypearr.RaynaId = arr[Convert.ToInt32(_RoomToken.RaynaId)];
                /*28*/
                roomtypearr.RoomNo = arr[Convert.ToInt32(_RoomToken.RoomNo)];
                /*29*/
                roomtypearr.RoomTypeCode = arr[Convert.ToInt32(_RoomToken.RoomTypeCode)];
                /*30*/
                roomtypearr.RoomTypeId = arr[Convert.ToInt32(_RoomToken.RoomTypeId)];
                /*31*/
                roomtypearr.RoomTypeName = arr[Convert.ToInt32(_RoomToken.RoomTypeName)];
                /*32*/
                roomtypearr.SHRUI = arr[Convert.ToInt32(_RoomToken.SHRUI)];
                /*33*/
                roomtypearr.SubAgentBuyingPice = arr[Convert.ToInt32(_RoomToken.SubAgentBuyingPice)];
                /*34*/
                roomtypearr.SubAgentSellingPrice = arr[Convert.ToInt32(_RoomToken.SubAgentSellingPrice)];
                /*35*/
                roomtypearr.SupplierCurrency = arr[Convert.ToInt32(_RoomToken.SupplierCurrency)];
                /*36*/
                roomtypearr.SupplierId = arr[Convert.ToInt32(_RoomToken.SupplierId)];
                /*37*/
                roomtypearr.TotalSurcharge = arr[Convert.ToInt32(_RoomToken.TotalSurcharge)];
                /*38*/
                roomtypearr.XMLCITY_ID = arr[Convert.ToInt32(_RoomToken.XMLCITY_ID)];
                /*39*/
                roomtypearr.XMLHotelCode = arr[Convert.ToInt32(_RoomToken.XMLHotelCode)];

                /*40*/
                roomtypearr.SupplierName = arr[Convert.ToInt32(_RoomToken.SupplierName)];
                /*41*/
                roomtypearr.XMLHotelId = arr[Convert.ToInt32(_RoomToken.XMLHotelId)];
                /*42*/
                roomtypearr.OTHPromoCode = arr[Convert.ToInt32(_RoomToken.OTHPromoCode)];
                /*43*/
                roomtypearr.Type = arr[Convert.ToInt32(_RoomToken.Type)];
                //44
                roomtypearr.MappedMealName = arr[Convert.ToInt32(_RoomToken.MappedMealName)];
                //45

            }
            catch (Exception ex)
            {

                // Common.Error(ex);
            }
            return roomtypearr;
        }

        public enum _SearchToken
        {
            CheckInDate,//0
            CheckOutDate,//1
            CityID,//2
            CountryID,//3
            Nationality,//4
            NoofNights,//5
            RoomsCount,//6
            IsOnlyAvailable,//7
            CountryCode,//8
            IsRecommendedOnly,//9
            IsShowRooms,//10
            RoomString,//11
            UniqueNo
        }

        public static HotelSearchParameter DecryptSearchKey(this string Key)
        {
            HotelSearchParameter para = new HotelSearchParameter();

            try
            {
                string searchKey = Key.ToStringFrom64Manual(false);
                string[] arr = searchKey.Split('|');
                para.CheckInDate = arr[Convert.ToInt32(_SearchToken.CheckInDate)];
                para.CheckOutDate = arr[Convert.ToInt32(_SearchToken.CheckOutDate)];
                para.CityID = Convert.ToInt32(arr[Convert.ToInt32(_SearchToken.CityID)]);
                para.CountryID = Convert.ToInt32(arr[Convert.ToInt32(_SearchToken.CountryID)]);
                para.Nationality = arr[Convert.ToInt32(_SearchToken.Nationality)];
                para.Residency = arr[Convert.ToInt32(_SearchToken.Nationality)];
                para.IsB2BOrB2C = "0";
                para.NoofNights = Convert.ToInt32(arr[Convert.ToInt32(_SearchToken.NoofNights)]);
                int rooms = Convert.ToInt32(arr[Convert.ToInt32(_SearchToken.RoomsCount)]);
                para.CountryCode = arr[Convert.ToInt32(_SearchToken.CountryCode)];
                List<RoomDetails> roomlist = new List<RoomDetails>();
                string RoomString = arr[Convert.ToInt32(_SearchToken.RoomString)];
                for (int i = 0; i < rooms; i++)
                {
                    RoomDetails room = new RoomDetails();
                    string[] arrRoom = RoomString.Split(']')[i].Split('~');
                    room.RoomNo = Convert.ToInt32(arrRoom[0]);
                    room.NoofAdults = Convert.ToInt32(arrRoom[1]);
                    room.NoofChild = Convert.ToInt32(arrRoom[2]);
                    room.Child1Age = Convert.ToInt32(arrRoom[3]);
                    room.Child2Age = Convert.ToInt32(arrRoom[4]);
                    roomlist.Add(room);
                }

                para.Rooms = roomlist;
                para.CurrentDate = DateTime.Now.ToString("yyyy/MM/dd/HH/mm/ss");
                Filters filters = new Filters();
                filters.IsRecommendedOnly = Convert.ToInt32(arr[Convert.ToInt32(_SearchToken.IsRecommendedOnly)]);
                filters.IsShowRooms = Convert.ToInt32(arr[Convert.ToInt32(_SearchToken.IsShowRooms)]);
                filters.IsOnlyAvailable = Convert.ToInt32(arr[Convert.ToInt32(_SearchToken.IsOnlyAvailable)]);
                para.Filters = filters;
                para.UniqueNo = arr[Convert.ToInt32(_SearchToken.UniqueNo)];
            }
            catch (Exception ex)
            {
                //Common.Error(ex);
            }


            return para;
        }

        public enum _PolicyToken
        {
            /*0*/
            RoomNo = 0,
            /*1*/
            FromDate = 1,
            /*2*/
            ToDate = 2,
            /*3*/
            CompanyCancellationPrice = 3,
            /*4*/
            AgentCancellationPrice = 4,
            /*5*/
            PurchaseToken = 5,
            /*6*/
            SPUI = 6,
            /*7*/
            BuyingPrice = 7
        }

        public static X_PolicyDetails DecryptPolicyTokenNew(this string policyToken) // add for tbo_api
        {
            X_PolicyDetails PolicyDetails = new X_PolicyDetails();

            try
            {

                string[] arr = policyToken.ToStringFrom64Manual(false).Split(new string[] { SeperatorRow }, StringSplitOptions.None);
                for (int i = 0; i < arr.Length - 1; i++)
                {

                    string[] arrPolicy = arr[i].Split(new string[] { SeperateID }, StringSplitOptions.None);
                    /*0*/
                    PolicyDetails.RoomNo = arrPolicy[Convert.ToInt32(_PolicyToken.RoomNo)];
                    /*1*/
                    PolicyDetails.FromDate = arrPolicy[Convert.ToInt32(_PolicyToken.FromDate)];
                    /*2*/
                    PolicyDetails.ToDate = arrPolicy[Convert.ToInt32(_PolicyToken.ToDate)];
                    /*3*/
                    PolicyDetails.CompanyCancellationPrice = arrPolicy[Convert.ToInt32(_PolicyToken.CompanyCancellationPrice)];
                    /*4*/
                    PolicyDetails.AgentCancellationPrice = arrPolicy[Convert.ToInt32(_PolicyToken.AgentCancellationPrice)];
                    /*5*/
                    PolicyDetails.PurchaseToken = arrPolicy[Convert.ToInt32(_PolicyToken.PurchaseToken)];
                    /*6*/
                    PolicyDetails.SPUI = arrPolicy[Convert.ToInt32(_PolicyToken.SPUI)];
                    /*7*/
                    PolicyDetails.BuyingCancellationPrice = arrPolicy[Convert.ToInt32(_PolicyToken.BuyingPrice)];
                    PolicyDetails.BuyingPrice = arrPolicy[Convert.ToInt32(_PolicyToken.BuyingPrice)];

                }

            }
            catch (Exception ex)
            {
                //Common.Error(ex);
            }
            return PolicyDetails;
        }

        public static List<X_PolicyDetails> DecryptPolicyToken(this string policyToken)
        {
            List<X_PolicyDetails> list = new List<X_PolicyDetails>();

            try
            {

                string[] arr = policyToken.ToStringFrom64Manual(false).Split(new string[] { SeperatorRow }, StringSplitOptions.None);
                for (int i = 0; i < arr.Length - 1; i++)
                {
                    X_PolicyDetails PolicyDetails = new X_PolicyDetails();
                    string[] arrPolicy = arr[i].Split(new string[] { SeperateID }, StringSplitOptions.None);
                    /*0*/
                    PolicyDetails.RoomNo = arrPolicy[Convert.ToInt32(_PolicyToken.RoomNo)];
                    /*1*/
                    PolicyDetails.FromDate = arrPolicy[Convert.ToInt32(_PolicyToken.FromDate)];
                    /*2*/
                    PolicyDetails.ToDate = arrPolicy[Convert.ToInt32(_PolicyToken.ToDate)];
                    /*3*/
                    PolicyDetails.CompanyCancellationPrice = arrPolicy[Convert.ToInt32(_PolicyToken.CompanyCancellationPrice)];
                    /*4*/
                    PolicyDetails.AgentCancellationPrice = arrPolicy[Convert.ToInt32(_PolicyToken.AgentCancellationPrice)];
                    /*5*/
                    PolicyDetails.PurchaseToken = arrPolicy[Convert.ToInt32(_PolicyToken.PurchaseToken)];
                    /*6*/
                    PolicyDetails.SPUI = arrPolicy[Convert.ToInt32(_PolicyToken.SPUI)];
                    /*7*/
                    PolicyDetails.BuyingCancellationPrice = arrPolicy[Convert.ToInt32(_PolicyToken.BuyingPrice)];
                    PolicyDetails.BuyingPrice = arrPolicy[Convert.ToInt32(_PolicyToken.BuyingPrice)];

                    list.Add(PolicyDetails);
                }

            }
            catch (Exception ex)
            {
                //Common.Error(ex);
            }
            return list;
        }


        #endregion
        #region "RateBrekup Token"
        public enum _RateToken
        {
            /*0*/
            RoomNo,
            /*1*/
            Date,
            /*2*/
            DateOfWeek,
            /*3*/
            Rate,
            /*4*/
            BoardTypeID,
            /*5*/
            Surcharge,
            /*6*/
            FinalRateType,
            /*7*/
            RateType,
            /*8*/
            CompanyRate,
            /*9*/
            Total
        }
       
        public static List<X_RateBreakupDetails> DecryptRateBreakUp(this string RateToken)
        {
            List<X_RateBreakupDetails> list = new List<X_RateBreakupDetails>();

            try
            {

                string[] arr = RateToken.ToStringFrom64Manual(false).Split(new string[] { SeperatorRow }, StringSplitOptions.None);
                for (int i = 0; i < arr.Length - 1; i++)
                {
                    X_RateBreakupDetails rateBreakup = new X_RateBreakupDetails();
                    string[] arrRoom = arr[i].Split(new string[] { SeperateID }, StringSplitOptions.None);
                    /*0*/
                    rateBreakup.RoomNo = arrRoom[Convert.ToInt32(_RateToken.RoomNo)];
                    /*1*/
                    rateBreakup.Date = arrRoom[Convert.ToInt32(_RateToken.Date)];
                    /*2*/
                    rateBreakup.DateOfWeek = arrRoom[Convert.ToInt32(_RateToken.DateOfWeek)];
                    /*3*/
                    rateBreakup.Rate = arrRoom[Convert.ToInt32(_RateToken.Rate)];
                    /*4*/
                    rateBreakup.BoardTypeID = arrRoom[Convert.ToInt32(_RateToken.BoardTypeID)];
                    /*5*/
                    rateBreakup.Surcharge = arrRoom[Convert.ToInt32(_RateToken.Surcharge)];
                    /*6*/
                    rateBreakup.FinalRateType = arrRoom[Convert.ToInt32(_RateToken.FinalRateType)];
                    /*7*/
                    rateBreakup.RateType = arrRoom[Convert.ToInt32(_RateToken.RateType)];
                    /*8*/
                    rateBreakup.CompanyRate = arrRoom[Convert.ToInt32(_RateToken.CompanyRate)];
                    /*9*/
                    rateBreakup.Total = arrRoom[Convert.ToInt32(_RateToken.Total)];
                    list.Add(rateBreakup);
                }

            }
            catch (Exception ex)
            {
                //Common.Error(ex);
            }
            return list;
        }
        #endregion

        public static DataRow GetCartTableDataRow(this _FinalRoomList hotelRoom, DataTable dtCart, string GuestUserId, string UniqueId, string SearchKey)
        {
            DataRow dataRow = dtCart.NewRow();
            try
            {
                dataRow["UniqueID"] = UniqueId;
                dataRow["SearchKey"] = SearchKey;
                dataRow["RoomToken"] = hotelRoom.RoomToken;
                dataRow["PolicyToken"] = hotelRoom.CancellationToken;
                dataRow["RateToken"] = hotelRoom.RateDetailsToken;
                dataRow["RoomNo"] = hotelRoom.RoomNo;

                dataRow["GuestUserId"] = GuestUserId;
                dataRow["UserCartId"] = 0;

            }
            catch (Exception ex)
            {
                //Common.Error(ex);
            }
            return dataRow;
        }

        

        //public static string GetHotelImagePath(this string Raynaid)
        public static string GetHotelImagePath(this string Raynaid,string staticimagepath)
        {
           
            //return _config["KeySettings:staticimagepath"] + "Images/Dubai/" + Raynaid + "/1-ThumbNail.jpg";
            return staticimagepath + "Images/Dubai/" + Raynaid + "/1-ThumbNail.jpg";
        }

        public static DataTable ConvertToDataTable<T>(IList<T> data)
        {
            PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(typeof(T));
            DataTable table = new DataTable();
            foreach (PropertyDescriptor prop in properties)
                table.Columns.Add(prop.Name, Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType);

            foreach (T item in data)
            {
                DataRow row = table.NewRow();
                foreach (PropertyDescriptor prop in properties)
                    row[prop.Name] = prop.GetValue(item) ?? DBNull.Value;
                table.Rows.Add(row);
            }
            return table;
        }


        #region "BookToken"
        public static string EncryptBookingToken(this HotelOptionPreBook hotelOption, Int32 GuestUserId, Int32 UserCartId)
        {
            string BookingToken = "";
            List<string> str = new List<string>();
            try
            {
                str.Add(Convert.ToString(GuestUserId));//0
                str.Add(Convert.ToString(hotelOption.TotalPrice));//1
                str.Add(Convert.ToString(hotelOption.IsPriceChange));//2
                str.Add(hotelOption.Status);//3
                str.Add(Convert.ToString(UserCartId));//4

                var strData = string.Join(SeperateID, str.ToArray());
                var encryptedString = EncryptString(SecretKey, strData);
                BookingToken = encryptedString;

            }
            catch (Exception ex)
            {
                //Common.Error(ex);
            }
            return BookingToken;
        }
        public static string DecryptBookingToken(this string token, ref int GuestUserId, ref decimal TotalPrice, ref int IsPriceChange, ref string Status, ref int UserCartId)
        {
            string str = "1";
            try
            {
                var decryptedString = DecryptString(SecretKey, token);
                String[] delimiters = { SeperateID };

                string[] arr = decryptedString.Split(delimiters, StringSplitOptions.None); //token.ToStringFrom64Manual(true).Split(new string[] { HotelXML.SeperateID }, StringSplitOptions.None);
                GuestUserId = Convert.ToInt32(arr[0]);
                TotalPrice = Convert.ToDecimal(arr[1]);
                IsPriceChange = Convert.ToInt32(arr[2]);
                Status = arr[3];
                UserCartId = Convert.ToInt32(arr[4]);
            }
            catch (Exception ex)
            {
                //Common.Error(ex);
                //Common.ErrorBooking(ex, "0", "token:" + token + " ,GuestUserId:" + GuestUserId + " ,TotalPrice:" + TotalPrice + " ,IsPriceChange" + IsPriceChange + " ,Status" + Status + " ,UserCartId" + UserCartId, "-");
                return "";
            }
            return str;
        }
        #region NewBooking Token logic
        public static string EncryptString(string key, string plainText)
        {
            byte[] iv = new byte[16];
            byte[] array;

            using (Aes aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(key);
                aes.IV = iv;

                ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

                using (MemoryStream memoryStream = new MemoryStream())
                {
                    using (CryptoStream cryptoStream = new CryptoStream((Stream)memoryStream, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter streamWriter = new StreamWriter((Stream)cryptoStream))
                        {
                            streamWriter.Write(plainText);
                        }

                        array = memoryStream.ToArray();
                    }
                }
            }

            return Convert.ToBase64String(array);
        }

        public static string DecryptString(string key, string cipherText)
        {
            byte[] iv = new byte[16];
            byte[] buffer = Convert.FromBase64String(cipherText);

            using (Aes aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(key);
                aes.IV = iv;
                ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

                using (MemoryStream memoryStream = new MemoryStream(buffer))
                {
                    using (CryptoStream cryptoStream = new CryptoStream((Stream)memoryStream, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader streamReader = new StreamReader((Stream)cryptoStream))
                        {
                            return streamReader.ReadToEnd();
                        }
                    }
                }
            }
        }
        #endregion
        public static string EncryptBookingToken_Old(this HotelOptionPreBook hotelOption, Int32 GuestUserId, Int32 UserCartId)
        {
            string BookingToken = "";
            List<string> str = new List<string>();
            try
            {
                str.Add(Convert.ToString(GuestUserId));//0
                str.Add(Convert.ToString(hotelOption.TotalPrice));//1
                str.Add(Convert.ToString(hotelOption.IsPriceChange));//2
                str.Add(hotelOption.Status);//3
                str.Add(Convert.ToString(UserCartId));//4

                return string.Join(SeperateID, str.ToArray());
            }
            catch (Exception ex)
            {
                //Common.Error(ex);
            }
            return BookingToken;
        }
        public static string DecryptBookingToken_Old(this string token, ref int GuestUserId, ref decimal TotalPrice, ref int IsPriceChange, ref string Status, ref int UserCartId)
        {
            string str = "1";
            try
            {
                string[] arr = token.ToStringFrom64Manual(true).Split(new string[] { SeperateID }, StringSplitOptions.None);
                GuestUserId = Convert.ToInt32(arr[0]);
                TotalPrice = Convert.ToDecimal(arr[1]);
                IsPriceChange = Convert.ToInt32(arr[2]);
                Status = arr[3];
                UserCartId = Convert.ToInt32(arr[4]);
            }
            catch (Exception ex)
            {
                //Common.Error(ex);
                //Common.ErrorBooking(ex, "0", "token:" + token + " ,GuestUserId:" + GuestUserId + " ,TotalPrice:" + TotalPrice + " ,IsPriceChange" + IsPriceChange + " ,Status" + Status + " ,UserCartId" + UserCartId, "-");
                return "";
            }
            return str;
        }
        #endregion

    }


}
