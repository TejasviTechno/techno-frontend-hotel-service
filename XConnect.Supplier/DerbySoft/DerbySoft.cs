using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static XConnect.Supplier.GoGlobal.GoGlobal;
using static XConnect.Supplier.Omnibees.HotelSearchResponse;

namespace XConnect.Supplier.DerbySoft
{
    public class DerbySoft
    {
        #region Hotel list class
        public class StayRange
        {
            public string checkin { get; set; }
            public string checkout { get; set; }
        }
        public class RoomCriteria
        {
            public int roomCount { get; set; }
            public int adultCount { get; set; }
            public int childCount { get; set; }
            public IList<object> childAges { get; set; }
        }

        public class DateRange
        {
            public string startDate { get; set; }
            public string endDate { get; set; }
        }
        public class Fee
        {
            public string name { get; set; }
            public string type { get; set; }
            public double amount { get; set; }
            public string amountType { get; set; }
            public string chargeType { get; set; }
            public int effectivePerson { get; set; }
        }

        public class Fees
        {
            public DateRange dateRange { get; set; }
            public Fee fee { get; set; }
        }

        public class CancelDeadline
        {
            public string offsetTimeDropType { get; set; }
            public string offsetTimeUnit { get; set; }
            public int offsetTimeValue { get; set; }
            public string dealineTime { get; set; }
        }

        public class PenaltyCharge
        {
            public string chargeBase { get; set; }
            public int nights { get; set; }
            public int? percent { get; set; }
        }
        public class CancelPenalty
        {
            public bool noShow { get; set; }
            public bool cancellable { get; set; }
            public CancelDeadline cancelDeadline { get; set; }
            public PenaltyCharge penaltyCharge { get; set; }
        }
        public class AvailRoomRate
        {
            public RoomCriteria roomCriteria { get; set; }
            public int inventory { get; set; }
            public string roomId { get; set; }
            public string rateId { get; set; }
            public string currency { get; set; }
            public IList<double> amountBeforeTax { get; set; }
            public string mealPlan { get; set; }
            public IList<Fees> fees { get; set; }
            public IList<double> amountAfterTax { get; set; }
            public CancelPolicy cancelPolicy { get; set; }
        }

        public class AvailHotel
        {
            public StayRange stayRange { get; set; }
            public IList<AvailRoomRate> availRoomRates { get; set; }
            public string supplierId { get; set; }
            public string hotelId { get; set; }
            public string status { get; set; }
        }
        public class Header
        {
            public string version { get; set; }
            public string token { get; set; }
            public string supplierId { get; set; }
            public string distributorId { get; set; }
        }

        public class DSResponse
        {
            public IList<AvailHotel> availHotels { get; set; }
            public Header header { get; set; }
        }
        #endregion
        #region Room list class
        public class DSRoomResponse
        {
            public string hotelId { get; set; }
            public StayRange stayRange { get; set; }
            public RoomCriteria roomCriteria { get; set; }
            public IList<RoomRate> roomRates { get; set; }
            public Header header { get; set; }
        }
        public class cancelPolicy
        {
            public string code { get; set; }
            public string description { get; set; }
            public IList<CancelPenalty> cancelPenalties { get; set; }
        }
        public class CancelPolicy1
        {
            public string code { get; set; }
            public string description { get; set; }
            public IList<CancelPenalty> cancelPenalties { get; set; }
        }
        #endregion
        #region hotel room info
        public class Settings
        {

        }
        public class Occupancy
        {
            public int maxAdult { get; set; }
            public int maxChild { get; set; }
            public int maxOccupancy { get; set; }
        }
        public class Product
        {
            public string roomId { get; set; }
            public string rateId { get; set; }
            public string status { get; set; }
            public string roomName { get; set; }
            public string rateName { get; set; }
            public Occupancy occupancy { get; set; }
            public IList<CancelPolicy> cancelPolicies { get; set; }
            public IList<Fee> fees { get; set; }
        }
        public class CancelPolicy
        {
            public DateRange dateRange { get; set; }
            public cancelPolicy cancelPolicy { get; set; }
            public string code { get; set; }
        }
        public class DSHotelInfo
        {
            public Settings settings { get; set; }
            public string ariType { get; set; }
            public string timezone { get; set; }
            public string rateType { get; set; }
            public IList<Product> products { get; set; }
            public string hotelId { get; set; }
            public string hotelName { get; set; }
            public string supplierId { get; set; }
            public string status { get; set; }
        }
        #endregion
        public class DSPriceCheck
        {
            public string hotelId { get; set; }
            public StayRange stayRange { get; set; }
            public RoomCriteria roomCriteria { get; set; }
            public IList<RoomRate> roomRates { get; set; }
            public Header header { get; set; }
        }
        public class RoomRate
        {
            public RoomCriteria roomCriteria { get; set; }
            public string roomId { get; set; }
            public string rateId { get; set; }
            public string currency { get; set; }
            public int inventory { get; set; }
            public IList<double> amountBeforeTax { get; set; }
            public IList<double> amountAfterTax { get; set; }
            public string mealPlan { get; set; }
            public IList<Fees> fees { get; set; }
            public CancelPolicy cancelPolicy { get; set; }
        }
        public class DSPreBooking
        {
            public string bookingToken { get; set; }
            public Header header { get; set; }
        }

        public class ReservationIds
        {
            public string distributorResId { get; set; }
            public string derbyResId { get; set; }
            public string supplierResId { get; set; }
        }

        public class DSBooking
        {
            public Header header { get; set; }
            public ReservationIds reservationIds { get; set; }
        }
        public class DSBookingCancel
        {
            public Header header { get; set; }
            public ReservationIds reservationIds { get; set; }
            public string cancellationId { get; set; }
        }
        public class Reservation
        {
            public ReservationIds reservationIds { get; set; }
            public string iata { get; set; }
            public string hotelId { get; set; }
            public StayRange stayRange { get; set; }
            public RoomCriteria roomCriteria { get; set; }
            public IList<string> comments { get; set; }
            public IList<RoomRate> roomRates { get; set; }
            public string status { get; set; }
            public string cancellationId { get; set; }
            public string result { get; set; }
        }
        public class DSBookingDetails
        {
            public Header header { get; set; }
            public IList<Reservation> reservations { get; set; }
        }

    }

    public class DerbySoftPriceCheck
    {
        public class Header
        {
            public string supplierId { get; set; }
            public string distributorId { get; set; }
            public string version { get; set; }
            public string token { get; set; }
        }

        public class StayRange
        {
            public string checkin { get; set; }
            public string checkout { get; set; }
        }

        public class RoomCriteria
        {
            public int roomCount { get; set; }
            public int adultCount { get; set; }
            public int childCount { get; set; }
            public List<int> childAges { get; set; }
        }

        public class ProductCandidate
        {
            public string roomId { get; set; }
            public string rateId { get; set; }
        }

        public class LoyaltyAccount
        {
            public string programCode { get; set; }
            public string accountId { get; set; }
        }

        public class CorpAccount
        {
            public string corpProgramCode { get; set; }
            public string corpId { get; set; }
        }

        public class Extension
        {
            public string userName { get; set; }
            public string password { get; set; }
        }

        public class RootObject
        {
            public Header header { get; set; }
            public string hotelId { get; set; }
            public StayRange stayRange { get; set; }
            public RoomCriteria roomCriteria { get; set; }
            public ProductCandidate productCandidate { get; set; }
            public string iata { get; set; }
            public LoyaltyAccount loyaltyAccount { get; set; }
            public CorpAccount corpAccount { get; set; }
            public string promoteCode { get; set; }
            public Extension extension { get; set; }
        }
    }

    public class DerbySoftPreBook
    {
        public class Header
        {
            public string supplierId { get; set; }
            public string distributorId { get; set; }
            public string version { get; set; }
            public string token { get; set; }
        }

        public class ReservationIds
        {
            public string distributorResId { get; set; }
            public string derbyResId { get; set; }
            public string supplierResId { get; set; }
        }

        public class StayRange
        {
            public string checkin { get; set; }
            public string checkout { get; set; }
        }

        public class ContactPerson
        {
            public string firstName { get; set; }
            public string lastName { get; set; }
            public string email { get; set; }
            public string phone { get; set; }
            public string address { get; set; }
            public int age { get; set; }
            public string type { get; set; }
            public string index { get; set; }
        }

        public class RoomCriteria
        {
            public int roomCount { get; set; }
            public int adultCount { get; set; }
            public int childCount { get; set; }
            public List<int> childAges { get; set; }
        }

        public class Total
        {
            public double? amountBeforeTax { get; set; }
            public double? amountAfterTax { get; set; }
        }

        public class Payment
        {
            public string cardCode { get; set; }
            public string cardNumber { get; set; }
            public string cardHolderName { get; set; }
            public string expireDate { get; set; }
        }

        public class LoyaltyAccount
        {
            public string programCode { get; set; }
            public string accountId { get; set; }
        }

        public class CorpAccount
        {
            public string corpProgramCode { get; set; }
            public string corpId { get; set; }
        }

        public class Guest
        {
            public string firstName { get; set; }
            public string lastName { get; set; }
            public string email { get; set; }
            public string phone { get; set; }
            public string address { get; set; }
            public int age { get; set; }
            public string type { get; set; }
            public string index { get; set; }
        }

        public class DateRange
        {
            public string startDate { get; set; }
            public string endDate { get; set; }
        }

        public class Fee2
        {
            public string name { get; set; }
            public string type { get; set; }
            public double amount { get; set; }
            public string amountType { get; set; }
            public string chargeType { get; set; }
            public int effectivePerson { get; set; }
        }

        public class Fee
        {
            public DateRange dateRange { get; set; }
            public Fee2 fee { get; set; }
        }

        public class CancelDeadline
        {
            public string offsetTimeDropType { get; set; }
            public string offsetTimeUnit { get; set; }
            public int offsetTimeValue { get; set; }
            public string dealineTime { get; set; }
        }

        public class PenaltyCharge
        {
            public string chargeBase { get; set; }
            public int nights { get; set; }
            public double amount { get; set; }
            public int percent { get; set; }
        }

        public class CancelPenalty
        {
            public bool noShow { get; set; }
            public bool cancellable { get; set; }
            public CancelDeadline cancelDeadline { get; set; }
            public PenaltyCharge penaltyCharge { get; set; }
        }

        public class CancelPolicy
        {
            public string code { get; set; }
            public string description { get; set; }
            public List<CancelPenalty> cancelPenalties { get; set; }
        }

        public class RoomRate
        {
            public string roomId { get; set; }
            public string rateId { get; set; }
            public string currency { get; set; }
            public List<double> amountBeforeTax { get; set; }
            public List<double> amountAfterTax { get; set; }
            public string mealPlan { get; set; }
            public List<Fee> fees { get; set; }
            public CancelPolicy cancelPolicy { get; set; }
        }

        public class Extension
        {
            public string userName { get; set; }
            public string password { get; set; }
        }

        public class RootObject
        {
            public Header header { get; set; }
            public ReservationIds reservationIds { get; set; }
            public string iata { get; set; }
            public string hotelId { get; set; }
            public StayRange stayRange { get; set; }
            public ContactPerson contactPerson { get; set; }
            public RoomCriteria roomCriteria { get; set; }
            public Total total { get; set; }
            public Payment payment { get; set; }
            public LoyaltyAccount loyaltyAccount { get; set; }
            public CorpAccount corpAccount { get; set; }
            public string promoteCode { get; set; }
            public List<Guest> guests { get; set; }
            public List<string> comments { get; set; }
            public List<RoomRate> roomRates { get; set; }
            public Extension extension { get; set; }
        }
    }
    public class DerbySoftBooking
    {
        public class Header
        {
            public string supplierId { get; set; }
            public string distributorId { get; set; }
            public string version { get; set; }
            public string token { get; set; }
        }

        public class ReservationIds
        {
            public string distributorResId { get; set; }
            public string derbyResId { get; set; }
            public string supplierResId { get; set; }
        }

        public class StayRange
        {
            public string checkin { get; set; }
            public string checkout { get; set; }
        }

        public class ContactPerson
        {
            public string firstName { get; set; }
            public string lastName { get; set; }
            public string email { get; set; }
            public string phone { get; set; }
            public string address { get; set; }
            public int? age { get; set; }
            public string type { get; set; }
            public string index { get; set; }
        }

        public class RoomCriteria
        {
            public int roomCount { get; set; }
            public int adultCount { get; set; }
            public int childCount { get; set; }
            public List<int> childAges { get; set; }
        }

        public class Total
        {
            public double? amountBeforeTax { get; set; }
            public double? amountAfterTax { get; set; }
        }

        public class Payment
        {
            public string cardCode { get; set; }
            public string cardNumber { get; set; }
            public string cardHolderName { get; set; }
            public string expireDate { get; set; }
        }

        public class LoyaltyAccount
        {
            public string programCode { get; set; }
            public string accountId { get; set; }
        }

        public class CorpAccount
        {
            public string corpProgramCode { get; set; }
            public string corpId { get; set; }
        }

        public class Guest
        {
            public string firstName { get; set; }
            public string lastName { get; set; }
            public string email { get; set; }
            public string phone { get; set; }
            public string address { get; set; }
            public int? age { get; set; }
            public string type { get; set; }
            public string index { get; set; }
        }

        public class DateRange
        {
            public string startDate { get; set; }
            public string endDate { get; set; }
        }

        public class Fee2
        {
            public string name { get; set; }
            public string type { get; set; }
            public double amount { get; set; }
            public string amountType { get; set; }
            public string chargeType { get; set; }
            public int effectivePerson { get; set; }
        }

        public class Fee
        {
            public DateRange dateRange { get; set; }
            public Fee2 fee { get; set; }
        }

        public class CancelDeadline
        {
            public string offsetTimeDropType { get; set; }
            public string offsetTimeUnit { get; set; }
            public int offsetTimeValue { get; set; }
            public string dealineTime { get; set; }
        }

        public class PenaltyCharge
        {
            public string chargeBase { get; set; }
            public int nights { get; set; }
            public double amount { get; set; }
            public int percent { get; set; }
        }

        public class CancelPenalty
        {
            public bool noShow { get; set; }
            public bool cancellable { get; set; }
            public CancelDeadline cancelDeadline { get; set; }
            public PenaltyCharge penaltyCharge { get; set; }
        }

        public class CancelPolicy
        {
            public string code { get; set; }
            public string description { get; set; }
            public List<CancelPenalty> cancelPenalties { get; set; }
        }

        public class RoomRate
        {
            public string roomId { get; set; }
            public string rateId { get; set; }
            public string currency { get; set; }
            public List<double> amountBeforeTax { get; set; }
            public List<double> amountAfterTax { get; set; }
            public string mealPlan { get; set; }
            public List<Fee> fees { get; set; }
            public CancelPolicy cancelPolicy { get; set; }
        }

        public class Extension
        {
            public string userName { get; set; }
            public string password { get; set; }
        }

        public class RootObject
        {
            public Header header { get; set; }
            public ReservationIds reservationIds { get; set; }
            public string iata { get; set; }
            public string hotelId { get; set; }
            public StayRange stayRange { get; set; }
            public ContactPerson contactPerson { get; set; }
            public RoomCriteria roomCriteria { get; set; }
            public Total total { get; set; }
            public Payment payment { get; set; }
            public LoyaltyAccount loyaltyAccount { get; set; }
            public CorpAccount corpAccount { get; set; }
            public string promoteCode { get; set; }
            public List<Guest> guests { get; set; }
            public List<string> comments { get; set; }
            public List<RoomRate> roomRates { get; set; }
            public string bookingToken { get; set; }
            public Extension extension { get; set; }
        }
    }
    public class DerbySoftBookingDetail
    {
        public class Header
        {
            public string supplierId { get; set; }
            public string distributorId { get; set; }
            public string version { get; set; }
            public string token { get; set; }
        }

        public class ReservationIds
        {
            public string distributorResId { get; set; }
            public string derbyResId { get; set; }
            public string supplierResId { get; set; }
        }

        public class RootObject
        {
            public Header header { get; set; }
            public ReservationIds reservationIds { get; set; }
        }
    }
    public class DerbySoftCancelBooking
    {
        public class Header
        {
            public string supplierId { get; set; }
            public string distributorId { get; set; }
            public string version { get; set; }
            public string token { get; set; }
        }

        public class ReservationIds
        {
            public string distributorResId { get; set; }
            public string derbyResId { get; set; }
            public string supplierResId { get; set; }
        }

        public class Extension
        {
            public string userName { get; set; }
            public string password { get; set; }
        }

        public class RootObject
        {
            public Header header { get; set; }
            public ReservationIds reservationIds { get; set; }
            public Extension extension { get; set; }
        }
    }
}
