using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XConnect.Supplier.TBO
{
    public class TBOClass
    {
        #region "Tboclass"
        public class hotelserach
        {
            public class Filters
            {
                public string MealType { get; set; }
                public string Refundable { get; set; }
                public int NoOfRooms { get; set; }
                public string StarRating { get; set; }
            }

            public class PaxRoom
            {
                public int Adults { get; set; }
                public int Children { get; set; }
                public List<int> ChildrenAges { get; set; }
            }

            public class Root
            {
                public string CheckIn { get; set; }
                public string CheckOut { get; set; }
                public string HotelCodes { get; set; }
                public string CityCode { get; set; }
                public string CityName { get; set; }
                public string CountryName { get; set; }
                public string GuestNationality { get; set; }
                public string PreferredCurrencyCode { get; set; }
                public List<PaxRoom> PaxRooms { get; set; }
                public bool IsDetailedResponse { get; set; }
                public int ResponseTime { get; set; }
                public Filters Filters { get; set; }
            }
        }
        public class hotelroomsearchres
        {


            public class Rootobject
            {
                public Status Status { get; set; }
                public Hotelresult[] HotelResult { get; set; }
            }

            public class Status
            {
                public int Code { get; set; }
                public string Description { get; set; }
            }

            public class Hotelresult
            {
                public string HotelCode { get; set; }
                public string Currency { get; set; }
                public Room[] Rooms { get; set; }
            }

            public class Room
            {
                public string[] Name { get; set; }
                public string BookingCode { get; set; }
                public string Inclusion { get; set; }
                public Dayrate[][] DayRates { get; set; }
                public double TotalFare { get; set; }
                public double TotalTax { get; set; }
                public string[] RoomPromotion { get; set; }
                public Supplement[][] Supplements { get; set; }
                public Cancelpolicy[] CancelPolicies { get; set; }
                public string MealType { get; set; }
                public bool IsRefundable { get; set; }
                public bool WithTransfers { get; set; }
            }

            public class Dayrate
            {
                public double BasePrice { get; set; }
            }

            public class Cancelpolicy
            {
                public string Index { get; set; }
                public string FromDate { get; set; }
                public string ChargeType { get; set; }
                public double CancellationCharge { get; set; }
            }

            public class Supplement
            {
                public int Index { get; set; }
                public string Type { get; set; }
                public string Description { get; set; }
                public double Price { get; set; }
                public string Currency { get; set; }
            }




        }
        public class Preebook
        {
            public class Root
            {
                public string BookingCode { get; set; }
                public string PaymentMode { get; set; }
            }
        }
        public class Preebookres
        {




            public class Rootobject
            {
                public Status Status { get; set; }
                public Hotelresult[] HotelResult { get; set; }
            }

            public class Status
            {
                public int Code { get; set; }
                public string Description { get; set; }
            }

            public class Hotelresult
            {
                public string HotelCode { get; set; }
                public string Currency { get; set; }
                public Room[] Rooms { get; set; }
                public string[] RateConditions { get; set; }
            }

            public class Room
            {
                public string[] Name { get; set; }
                public string BookingCode { get; set; }
                public string Inclusion { get; set; }
                public Dayrate[][] DayRates { get; set; }
                public double TotalFare { get; set; }
                public double TotalTax { get; set; }
                public string[] RoomPromotion { get; set; }
                public Cancelpolicy[] CancelPolicies { get; set; }
                public string MealType { get; set; }
                public bool IsRefundable { get; set; }
                public Supplement[][] Supplements { get; set; }
                public bool WithTransfers { get; set; }
                public string[] Amenities { get; set; }
            }

            public class Dayrate
            {
                public double BasePrice { get; set; }
            }

            public class Cancelpolicy
            {
                public int Index { get; set; }
                public string FromDate { get; set; }
                public string ChargeType { get; set; }
                public double CancellationCharge { get; set; }
            }

            public class Supplement
            {
                public int Index { get; set; }
                public string Type { get; set; }
                public string Description { get; set; }
                public double Price { get; set; }
                public string Currency { get; set; }
            }

        }
        public class BookingTbo
        {
            public class CustomerDetail
            {
                public List<CustomerName> CustomerNames { get; set; }
            }

            public class CustomerName
            {
                public string Title { get; set; }
                public string FirstName { get; set; }
                public string LastName { get; set; }
                public string Type { get; set; }
            }

            public class Root
            {
                public string BookingCode { get; set; }
                public List<CustomerDetail> CustomerDetails { get; set; }
                public string ClientReferenceId { get; set; }
                public string BookingReferenceId { get; set; }
                public double TotalFare { get; set; }
                public string EmailId { get; set; }
                public string PhoneNumber { get; set; }
                public string BookingType { get; set; }
                public string PaymentMode { get; set; }

            }
        }
        public class BookingresTbo
        {
            public class Root
            {
                public Status Status { get; set; }
                public string ClientReferenceId { get; set; }
                public string ConfirmationNumber { get; set; }
            }

            public class Status
            {
                public int Code { get; set; }
                public string Description { get; set; }
            }
        }
        public class BookingDetailTbo
        {
            public class Root
            {
                public string BookingReferenceId { get; set; }
                public string PaymentMode { get; set; }
            }

        }
        public class BookingDetailTbores
        {
            public class Supplement
            {
                public int Index { get; set; }
                public string Type { get; set; }
                public string Description { get; set; }
                public double Price { get; set; }
                public string Currency { get; set; }
            }
            public class BookingDetail
            {
                public string BookingStatus { get; set; }
                public bool VoucherStatus { get; set; }
                public string ConfirmationNumber { get; set; }
                public DateTime CheckIn { get; set; }
                public DateTime CheckOut { get; set; }
                public DateTime BookingDate { get; set; }
                public int NoOfRooms { get; set; }
                public HotelDetails HotelDetails { get; set; }
                public List<Room> Rooms { get; set; }
                public List<string> RateConditions { get; set; }
            }

            public class CancelPolicy
            {
                public string FromDate { get; set; }
                public string ChargeType { get; set; }
                public double CancellationCharge { get; set; }
            }

            public class CustomerDetail
            {
                public List<CustomerName> CustomerNames { get; set; }
            }

            public class CustomerName
            {
                public string Title { get; set; }
                public string FirstName { get; set; }
                public string LastName { get; set; }
                public string Type { get; set; }
            }

            public class HotelDetails
            {
                public string HotelName { get; set; }
                public string Rating { get; set; }
                public string Map { get; set; }
                public string City { get; set; }
            }

            public class Room
            {
                public string Currency { get; set; }
                public string Status { get; set; }
                public List<string> Name { get; set; }
                public string Inclusion { get; set; }
                public double TotalFare { get; set; }
                public double TotalTax { get; set; }
                public string RoomPromotion { get; set; }
                public List<CancelPolicy> CancelPolicies { get; set; }
                public string MealType { get; set; }
                public bool IsRefundable { get; set; }
                public Supplement[][] Supplements { get; set; }
                public List<CustomerDetail> CustomerDetails { get; set; }
            }

            public class Root
            {
                public Status Status { get; set; }
                public BookingDetail BookingDetail { get; set; }
            }

            public class Status
            {
                public int Code { get; set; }
                public string Description { get; set; }
            }
        }
        public class BookingCancelTbo
        {
            public class Root
            {
                public string ConfirmationNumber { get; set; }
            }

        }
        public class BookingCancelTbores
        {
            public class Root
            {
                public Status Status { get; set; }
                public string ConfirmationNumber { get; set; }
            }

            public class Status
            {
                public int Code { get; set; }
                public string Description { get; set; }
            }

        }
        #endregion "Tboclass"
        
    }
}
