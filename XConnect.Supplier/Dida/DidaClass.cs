using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XConnect.Supplier.Dida
{
    public class DidaClass
    {
        public class AuditData
        {
            public string RequestClientIP { get; set; }
            public string SessionID { get; set; }
            public DateTime RequestTime { get; set; }
            public DateTime ResponseTime { get; set; }
            public int ProcessTime { get; set; }
        }

        public class Destination
        {
            public string CityCode { get; set; }
        }

        public class ExcludedFeeList
        {
            public string FeeTypeName { get; set; }
            public string Currency { get; set; }
            public double Amount { get; set; }
        }

        public class HotelList
        {
            public int HotelID { get; set; }
            public string HotelName { get; set; }
            public Destination Destination { get; set; }

            public LowestPrice LowestPrice { get; set; }
            public List<RatePlanList> RatePlanList { get; set; }
        }

        public class LowestPrice
        {
            public double Value { get; set; }
        }

        public class IncludedFeeList
        {
            public string FeeTypeName { get; set; }
            public string Currency { get; set; }
            public double Amount { get; set; }
        }

        public class PriceDetails
        {
            public string CheckInDate { get; set; }
            public string CheckOutDate { get; set; }
            public List<HotelList> HotelList { get; set; }
        }

        public class PriceList
        {
            public double Price { get; set; }
            public string StayDate { get; set; }
            public int MealType { get; set; }
            public int MealAmount { get; set; }
        }

        public class RatePlanCancellationPolicyList
        {
            public string FromDate { get; set; }
            public double Amount { get; set; }
        }

        public class RatePlanList
        {
            public RoomOccupancy RoomOccupancy { get; set; }
            public string RoomName { get; set; }
            public string RatePlanID { get; set; }
            public string RatePlanName { get; set; }
            public int BedType { get; set; }
            public int BreakfastType { get; set; }
            public int MaxOccupancy { get; set; }
            public int InventoryCount { get; set; }
            public int RoomStatus { get; set; }
            public string Currency { get; set; }
            public double TotalPrice { get; set; }
            public List<PriceList> PriceList { get; set; }
            public List<RatePlanCancellationPolicyList> RatePlanCancellationPolicyList { get; set; }
            public int StandardOccupancy { get; set; }
            public List<ExcludedFeeList> ExcludedFeeList { get; set; }
            public List<IncludedFeeList> IncludedFeeList { get; set; }

            public string PriceWithoutSupplement { get; set; } //add by jay
            public string Supplement { get; set; } //add by jay
        }

        public class RoomOccupancy
        {
            public int RoomNum { get; set; }
            public int AdultCount { get; set; }
            public int ChildCount { get; set; }
        }

        public class Root
        {
            public Success Success { get; set; }
            public AuditData AuditData { get; set; }
        }

        public class Success
        {
            public PriceDetails PriceDetails { get; set; }
        }
    }
}
