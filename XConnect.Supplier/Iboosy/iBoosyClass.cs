using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XConnect.Supplier.Iboosy
{
    public class iBoosyClass
    {
        public class MethaBook
        {
            public MethaBook()
            {

            }
            #region "Hotel List And Room"
            public class Zone
            {
                public string code { get; set; }
                public string name { get; set; }
            }

            public class MealPlan
            {
                public string code { get; set; }
                public string name { get; set; }
            }

            public class Pax2
            {
                public int paxId { get; set; }
                public string name { get; set; }
                public string surname { get; set; }
            }

            public class Room
            {
                public string code { get; set; }
                public string name { get; set; }
                public string description { get; set; }
                public int roomRefId { get; set; }
                public List<Pax2> paxes { get; set; }
            }

            public class Promotion
            {
                public string code { get; set; }
                public string name { get; set; }
            }

            public class RateCondition
            {
                public string code { get; set; }
            }

            public class AccommodationInfo
            {
                public string code { get; set; }
                public string name { get; set; }
                public Zone zone { get; set; }
            }

            public class Price
            {
                public string gross { get; set; }
                public string net { get; set; }
                public string currencyCode { get; set; }
            }

            public class Price2
            {
                public string gross { get; set; }
                public string net { get; set; }
                public string currencyCode { get; set; }
            }

            public class PenaltyRule
            {
                public string type { get; set; }
                public string dateFrom { get; set; }
                public Price2 price { get; set; }
                public string percentage { get; set; }
                public string nights { get; set; }
            }

            public class CancellationPolicy
            {
                public string description { get; set; }
                public List<PenaltyRule> penaltyRules { get; set; }
            }

            public class Pax
            {
                public string name { get; set; }
                public string surname { get; set; }
                public bool holder { get; set; }
                public string type { get; set; }
                public string age { get; set; }
            }

            public class RequiredFields
            {
                public List<Pax> paxes { get; set; }
            }

            public class Booking
            {
                public string bookingCode { get; set; }
                public string expirationDate { get; set; }
                public RequiredFields requiredFields { get; set; }
                public string nationality { get; set; }
                public string locator { get; set; }
                public string status { get; set; }
                public string clientReference { get; set; }
                public List<Pax> paxes { get; set; }
                public Services services { get; set; }
            }

            public class Availability
            {
                public Booking booking { get; set; }
                public string code { get; set; }
                public string paymentType { get; set; }
                public string nonRefundable { get; set; }
                public string status { get; set; }
                public MealPlan mealPlan { get; set; }
                public List<Room> rooms { get; set; }
                public List<Fee> fees { get; set; }
                public List<Promotion> promotions { get; set; }
                public List<RateCondition> rateConditions { get; set; }
                public AccommodationInfo accommodationInfo { get; set; }
                public Price price { get; set; }
                public CancellationPolicy cancellationPolicy { get; set; }
                public List<string> remarks { get; set; }
            }

            public class Fee
            {
                public string description { get; set; }
                public bool includedInPrice { get; set; }
                public double amount { get; set; }
                public string currencyCode { get; set; }
            }

            public class Accommodation
            {
                public string code { get; set; }
                public string name { get; set; }
                public Zone zone { get; set; }
                public List<Availability> availabilities { get; set; }
            }

            public class AccommodationService
            {
                public string externalLocator { get; set; }
                public string accommodationCode { get; set; }
                public string description { get; set; }
                public string checkInDate { get; set; }
                public string checkOutDate { get; set; }
                public Price price { get; set; }
                public MealPlan mealPlan { get; set; }
                public List<Room> rooms { get; set; }
                public CancellationPolicy cancellationPolicy { get; set; }
            }

            public class Services
            {
                public List<AccommodationService> accommodationServices { get; set; }
            }

            public class BookingRs
            {
                public List<Booking> bookings { get; set; }
            }

            public class AvailabilityRs
            {
                public string language { get; set; }
                public List<Accommodation> accommodations { get; set; }
            }

            public class ValuationRS
            {
                public string language { get; set; }
                public Availability availability { get; set; }
            }

            public class RootObject
            {
                public string track { get; set; }
                public AvailabilityRs availabilityRs { get; set; }
                public ValuationRS valuationRS { get; set; }
                public BookingRs bookingRs { get; set; }
            }
            #endregion
        }
        public class RoomData
        {
            public double FinalSellingPrice { set; get; }
            public int RoomNo { set; get; }
            public string MealID { set; get; }
        }
    }
}
