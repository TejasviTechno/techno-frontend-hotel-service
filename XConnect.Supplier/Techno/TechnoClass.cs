using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson.Serialization.Attributes;

namespace XConnect.Supplier.Techno
{
    public class TechnoClass
    {
        #region "Response"
        public class AvailHotelList
        {
            public AvailabilityRS AvailabilityRS { get; set; }
            public string Search_Key { get; set; }//#MongoDB
            public DateTime Time { get; set; }//#MongoDB
        }

        public class AvailHotelList1 //#MongoDB
        {
            [BsonId]
            [BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
            public string Id { get; set; }
            public AvailabilityRS AvailabilityRS { get; set; }
            public string Search_Key { get; set; }
            public DateTime Time { get; set; }
        }
        public class AvailabilityRS
        {
            public string SearchKey { get; set; }
            public Int64 Count { get; set; }
            public string Currency { get; set; }

            public List<_HotelListNew> HotelResult { get; set; }
        }
        public class _HotelListNew
        {
            public decimal StartPrice { get; set; }
            public Int64 HotelId { get; set; }
            public string HotelCode { get; set; }

            public List<Options> HotelOption { get; set; }
        }
        public class Options
        {
            public string HotelOptionId { get; set; }
            public string HotelCode { get; set; }
            public Int64 HotelId { get; set; }
            public decimal MinPrice { get; set; }
            public bool IsCombineRoom { get; set; }
            public List<List<_FinalRoomList>> HotelRooms { get; set; }
        }
        public class _FinalRoomList
        {
            //public string OwnsystemHotelId { get; set; }
            public int Sequence { get; set; } = 0; // ADD ON 29-JAN-2025
            public string RoomNo { get; set; }
            public string RoomTypeName { get; set; }
            public string MealName { get; set; }
            public decimal Price { get; set; }
            public string EssentialInformation { get; set; }
            public string BookingStatus { get; set; }
            public string RoomToken { get; set; }
            public List<_FinalPolicy> CancellationPolicy { get; set; }
            public string CancellationToken { get; set; }
            //public List<_FinalRate> RateDetails { get; set; }
            public string RateDetailsToken { get; set; }
            public string MappedMealName { get; set; }
        }
        public class _FinalPolicy
        {
            public string FromDate { get; set; }
            public string ToDate { get; set; }
            public decimal CancellationPrice { get; set; }

        }

        public class _FinalRate
        {
            public string Date { get; set; }
            public decimal Price { get; set; }
            public string BookingStatus { get; set; }
            public string RoomTypeName { get; set; }
            public string RateType { get; set; }
        }

        #endregion
    }
}
