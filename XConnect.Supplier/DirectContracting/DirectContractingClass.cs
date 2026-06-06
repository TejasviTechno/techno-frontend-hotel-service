using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XConnect.Supplier.DirectContracting
{
    public class DirectContractingClass
    {

        #region "Class"
        //public class RootObject_DC
        //{

        //    public class HotelOption
        //    {
        //        public int hotelDetailId { get; set; }
        //        public int hotelMasterId { get; set; }
        //        public int supplierMasterId { get; set; }
        //        public double minPrice { get; set; }
        //        public bool isCombineRoom { get; set; }
        //        public List<List<HotelRoom_DC>> hotelRooms { get; set; }
        //    }

        //    public class HotelResult
        //    {
        //        public double startPrice { get; set; }
        //        public int hotelId { get; set; }
        //        public List<HotelOption> hotelOption { get; set; }
        //    }

        //    public class Result
        //    {
        //        public int count { get; set; }
        //        public string currency { get; set; }
        //        public List<HotelResult> hotelResult { get; set; }
        //    }

        //    public class Root
        //    {
        //        public List<object> errors { get; set; }
        //        public Result result { get; set; }
        //    }
        //    public class HotelRoom_DC
        //    {
        //        public string roomNo { get; set; }
        //        public int roomId { get; set; }
        //        public int roomTypeId { get; set; }
        //        public string roomTypeName { get; set; }
        //        public int bedTypeId { get; set; }
        //        public string bedTypeName { get; set; }
        //        public int ratePlanId { get; set; }
        //        public int mealTypeId { get; set; }
        //        public string mealTypeName { get; set; }
        //        public string mealTypeCode { get; set; }
        //        public int boardTypeId { get; set; }
        //        public string boardTypeName { get; set; }
        //        public string boardTypeCode { get; set; }
        //        public double price { get; set; }
        //        public string bookingStatus { get; set; }
        //        public int? promotionId { get; set; }
        //        public string promotionCode { get; set; }
        //        public string promotionName { get; set; }
        //        public bool isRefundable { get; set; }
        //        public List<CancellationPolicy_DC> cancellationPolicy { get; set; }
        //    }
        //    public class CancellationPolicy_DC
        //    {
        //        public string fromDate { get; set; }
        //        public string toDate { get; set; }
        //        public double cancellationPrice { get; set; }
        //    }
        //}


        public class RootObject_DC
        {

            public class Root
            {
                public List<object> errors { get; set; }
                public Result result { get; set; }
            }

            public class Result
            {
                public string checkIn { get; set; }
                public string checkOut { get; set; }
                public int nights { get; set; }
                public string currency { get; set; }
                public int totalHotels { get; set; }
                public List<HotelResult> hotelResult { get; set; }
            }

            public class HotelResult
            {
                public int hotelId { get; set; }
                public HotelDetail hotelDetail { get; set; }
                public double startPrice { get; set; }
                public List<HotelOption> hotelOption { get; set; }
            }

            public class HotelDetail
            {
                public Location location { get; set; }
                public int id { get; set; }
                public string name { get; set; }
                public string phoneNumber { get; set; }
                public string faxNumber { get; set; }
                public int starRating { get; set; }
                public string zipPostalCode { get; set; }
                public string airportCode { get; set; }
            }

            public class Location
            {
                public Country country { get; set; }
                public City city { get; set; }
                public Coordinate coordinate { get; set; }
                public string address { get; set; }
                public string stateCode { get; set; }
            }

            public class Country
            {
                public int id { get; set; }
                public string name { get; set; }
                public string code { get; set; }
            }

            public class City
            {
                public int? id { get; set; }
                public string name { get; set; }
            }

            public class Coordinate
            {
                public string longitude { get; set; }
                public string latitude { get; set; }
            }

            public class HotelOption
            {
                public int hotelMasterId { get; set; }
                public int supplierMasterId { get; set; }
                public string currency { get; set; }
                public bool isPreferred { get; set; }
                public double startPrice { get; set; }
                public List<List<HotelRoom>> hotelRooms { get; set; }
            }

            public class HotelRoom
            {
                public int roomNo { get; set; }
                public string propertyCode { get; set; }
                public RoomTBZv2 room { get; set; }
                public RatePlan ratePlan { get; set; }
                public Occupancy occupancy { get; set; }
                public string bookingStatus { get; set; }
                public double price { get; set; }
                public PriceBreakdown priceBreakdown { get; set; }
                public Promotions promotions { get; set; }
                public List<object> extraFees { get; set; }
                public bool isRefundable { get; set; }
                public List<CancellationPolicy_TBZ> cancellationPolicy { get; set; }
            }

            public class RoomTBZv2
            {
                public int roomId { get; set; }
                public string roomCode { get; set; }
                public RoomType roomType { get; set; }
                public BedType bedType { get; set; }
            }

            public class RoomType
            {
                public int id { get; set; }
                public string name { get; set; }
            }

            public class BedType
            {
                public int id { get; set; }
                public string name { get; set; }
            }

            public class RatePlan
            {
                public int ratePlanId { get; set; }
                public string ratePlanCode { get; set; }
                public MealType mealType { get; set; }
                public bool isMealIncluded { get; set; }
                public BoardType boardType { get; set; }
            }

            public class MealType
            {
                public int id { get; set; }
                public string name { get; set; }
                public string code { get; set; }
            }

            public class BoardType
            {
                public int id { get; set; }
                public string name { get; set; }
                public string code { get; set; }
            }

            public class Occupancy
            {
                public int noOfGuest { get; set; }
                public int basePerson { get; set; }
                public int noOfAdult { get; set; }
                public int noOfChild { get; set; }
                public bool hasExtraPerson { get; set; }
                public int noOfExtraAdult { get; set; }
                public int noOfExtraChild { get; set; }
            }

            public class PriceBreakdown
            {
                public double baseRate { get; set; }
                public double extraAdultRate { get; set; }
                public double extraChildRate { get; set; }
                public double extraPersonRate { get; set; }
                public double discountAmount { get; set; }
                public double extraFee { get; set; }
                public double finalRate { get; set; }
                public List<DailyRate> dailyRates { get; set; }
            }

            public class DailyRate
            {
                public string dateOfStay { get; set; }
                public int allocationTypeId { get; set; }
                public int availability { get; set; }
                public bool closedToArrival { get; set; }
                public bool closedToDeparture { get; set; }
                public bool hasExtraPerson { get; set; }
                public double baseRate { get; set; }
                public double extraAdultRate { get; set; }
                public double extraChildRate { get; set; }
                public double extraPersonRate { get; set; }
                public List<ExtraPerson> extraPersons { get; set; }
                public List<int> discountIds { get; set; }
                public bool isCombined { get; set; }
                public int? combinationId { get; set; }
                public double discountAmount { get; set; }
                public double finalRate { get; set; }
            }

            public class ExtraPerson
            {
                public int chargeTypeId { get; set; }
                public int fromAge { get; set; }
                public int toAge { get; set; }
                public int numberOfExtra { get; set; }
                public double extraRate { get; set; }
                public bool isMealIncluded { get; set; }
                public double mealRate { get; set; }
                public bool needExtraBed { get; set; }
                public double extraBedRate { get; set; }
                public double finalRate { get; set; }
            }

            public class Promotions
            {
                public List<Discount> discounts { get; set; }
                public object combination { get; set; }  // keep object since it's null (or define later if structure comes)
                public List<ValueAdd> valueAdds { get; set; }
            }

            public class CancellationPolicy_TBZ
            {
                public string fromDate { get; set; }
                public string toDate { get; set; }
                public double cancellationPrice { get; set; }
            }
            public class Discount
            {
                public int discountId { get; set; }
                public string discountName { get; set; }
                public string discountCode { get; set; }
                public int discountType { get; set; }
                public bool isCombinable { get; set; }
                public bool isSeriesOffer { get; set; }
            }

            public class ValueAdd
            {
                public int valueAddId { get; set; }
                public string valueAddName { get; set; }
                public string valueAddCode { get; set; }
                public DateTime validFromDate { get; set; }
                public DateTime validToDate { get; set; }
                public bool applyOnExtraPerson { get; set; }
                public List<ValueAddItem> valueAddItems { get; set; }
            }

            public class ValueAddItem
            {
                public int valueAddItemId { get; set; }
                public string name { get; set; }
                public string shortDescription { get; set; }
                public string description { get; set; }
                public object upgrade { get; set; }
            }
        }
        #endregion
    }
}
