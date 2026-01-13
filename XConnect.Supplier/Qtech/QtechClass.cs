using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XConnect.Supplier.Qtech
{
    public class QtechClass
    {
    }

    public class Hotel
    {
        public class Room_Qtech
        {
            public int RoomCount { get; set; }
            public string RoomName { get; set; }
            public bool status { get; set; }
        }
    }

    public class QtechListrs
    {


        public class CancellationPolicy
        {
            public string Start { get; set; }
            public string End { get; set; }
            public double Charges { get; set; }
        }

        public class HotelList
        {
            public string HotelId { get; set; }
            public string HotelName { get; set; }
            public string LocalHotelId { get; set; }
            public string PropertyRating { get; set; }
            public string Available { get; set; }
            public string ThumbNailUrl { get; set; }
            public string RateCurrencyCode { get; set; }
            public double TotalCharges { get; set; }
            public List<HotelProperty> HotelProperty { get; set; }
        }

        public class HotelProperty
        {
            public double DisplayRoomRate { get; set; }
            public string SectionUniqueId { get; set; }
            public string Type { get; set; }
            public List<RoomRatee> RoomRates { get; set; }
            public List<object> RoomDetails { get; set; }
            public bool Refundable { get; set; }
            public Policies Policies { get; set; }
        }

        public class Policies
        {
            public List<CancellationPolicy> CancellationPolicy { get; set; }
        }

        public class RateBreakup
        {
            public string Date { get; set; }
            public string Day { get; set; }
            public double DisplayNightlyRate { get; set; }
        }

        public class RoomRatee
        {
            public int Available { get; set; }
            public int NumberOfRooms { get; set; }
            public int NumberOfAdults { get; set; }
            public string NumberOfChild { get; set; }
            public object ChildAges { get; set; }
            public double RoomRate { get; set; }
            public string RoomType { get; set; }
            public string RoomCategory { get; set; }
            public string MealBasis { get; set; }
            public string MealCode { get; set; }
            public string Note { get; set; }
            public string ClassUniqueId { get; set; }
            public List<RateBreakup> RateBreakup { get; set; }
        }

        public class Root
        {
            public int TotalCount { get; set; }
            public string WebServiceVersion { get; set; }
            public string Message { get; set; }
            public List<HotelList> HotelList { get; set; }
            public string StartTime { get; set; }
            public string EndTime { get; set; }
            public string SearchUniqueId { get; set; }
        }




    }
}
