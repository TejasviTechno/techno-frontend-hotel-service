using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XConnect.Supplier.IWTX
{
    public class IWTXClass
    {
        public class IWIXHotelList
        {
            public class BlackOut
            {
                public string Status { get; set; }
                public string Msg { get; set; }
            }

            public class Cancellation
            {
                public int FromDate { get; set; }
                public int ToDate { get; set; }
                public int NightToCharge { get; set; }
            }

            public class CancellationPolicyDetails
            {
                public List<Cancellation> Cancellation { get; set; }
            }

            public class DiscountDetails
            {
            }

            public class GeoLocation
            {
                public string Longitude { get; set; }
                public string Latitude { get; set; }
            }

            public class Hotel
            {
                public int SourceId { get; set; }
                public int HotelId { get; set; }
                public string HotelName { get; set; }
                public string PreferredStatus { get; set; }
                public string PropertyType { get; set; }
                public int StarRating { get; set; }
                public GeoLocation GeoLocation { get; set; }
                public string Chain { get; set; }
                public string HotelCode { get; set; }
                public string TimeZone { get; set; }
                public string City { get; set; }
                public RoomTypeDetails RoomTypeDetails { get; set; }
                public int StartDate { get; set; }
                public int EndDate { get; set; }
                public string Country { get; set; }
                public string Restriction { get; set; }
                public string BrandName { get; set; }
            }

            public class Hotels
            {
                public List<Hotel> Hotel { get; set; }
            }

            public class Room
            {
                public int RoomNo { get; set; }
                public string RoomType { get; set; }
                public int RoomTypeCode { get; set; }
                public string RoomTypeSupplierCode { get; set; }
                public string RatePromoCode { get; set; }
                public string MealPlanSupplierCode { get; set; }
                public string RoomStatus { get; set; }
                public BlackOut BlackOut { get; set; }
                public string CurrCode { get; set; }
                public string ContractTokenId { get; set; }
                public int RoomConfigurationId { get; set; }
                public string RatePlanCode { get; set; }
                public int RatePlanId { get; set; }
                public string MealPlan { get; set; }
                public string MealPlanCode { get; set; }
                public int NumberOfMeals { get; set; }
                public int RoomNumber { get; set; }
                public double Rate { get; set; }
                public RoomStatusDetails RoomStatusDetails { get; set; }
                public DiscountDetails DiscountDetails { get; set; }
                public CancellationPolicyDetails CancellationPolicyDetails { get; set; }
                public string PromotionalContract { get; set; }
                public string PackageYN { get; set; }
                public string NonRefundable { get; set; }
                public string DynamicYN { get; set; }
                public double TotalRate { get; set; }
                public double RateBeforeTax { get; set; }
                public double TotalDiscount { get; set; }
                public double RecommendedRetailPrice { get; set; }
                public string ContractLabel { get; set; }
                public int MealPlanCodeLong { get; set; }
            }

            public class Rooms
            {
                public List<Room> Room { get; set; }
            }

            public class RoomStatusDetails
            {
                public List<string> Status { get; set; }
            }

            public class RoomTypeDetails
            {
                public Rooms Rooms { get; set; }
            }

            public class Iwtx_main
            {
                public Hotels Hotels { get; set; }
            }

        }
    }
}
