using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XConnect.Supplier.Agoda
{
    public class AgodaClass
    {
        
    }
    #region Agoda_New Class

    public class Agoda_New
    {
        // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
        public class Benefit
        {
            public int id { get; set; }
            public string benefitName { get; set; }
            public string translatedBenefitName { get; set; }
        }
        public class CancellationPolicy
        {
            public string code { get; set; }
            public string cancellationText { get; set; }
            public string translatedCancellationText { get; set; }
            public List<Parameter> parameter { get; set; }
            public List<Date> date { get; set; }
        }
        public class DailyRate
        {
            public string date { get; set; }
            public double exclusive { get; set; }
            public double inclusive { get; set; }
            public double tax { get; set; }
            public double fees { get; set; }
        }
        public class Date
        {
            public DateTime before { get; set; }
            public Rate rate { get; set; }
            public DateTime? onward { get; set; }
        }

        public class Parameter
        {
            public int days { get; set; }
            public string charge { get; set; }
            public int value { get; set; }
        }
        public class Property
        {
            public int propertyId { get; set; }
            public string propertyName { get; set; }
            public string translatedPropertyName { get; set; }
            public List<Room> rooms { get; set; }
        }
        public class Rate
        {
            public string currency { get; set; }
            public double exclusive { get; set; }
            public double inclusive { get; set; }
            public double tax { get; set; }
            public double fees { get; set; }
            public string method { get; set; }
        }
        public class Room
        {
            public int roomId { get; set; }
            public string blockId { get; set; }
            public string roomName { get; set; }
            public string parentRoomName { get; set; }
            public string translatedRoomName { get; set; }
            public string blockIdBackup { get; set; }
            public int parentRoomId { get; set; }
            public int ratePlanId { get; set; }
            public bool freeWifi { get; set; }
            public int remainingRooms { get; set; }
            public int normalBedding { get; set; }
            public int extraBeds { get; set; }
            public bool freeBreakfast { get; set; }
            public bool freeCancellation { get; set; }
            public TotalPayment totalPayment { get; set; }
            public bool roomTypeNotGuaranteed { get; set; }
            public string paymentModel { get; set; }
            public Rate rate { get; set; }
            public List<DailyRate> dailyRate { get; set; }
            public List<Surcharge> surcharges { get; set; }
            public List<TaxBreakdown> taxBreakdown { get; set; }
            public CancellationPolicy cancellationPolicy { get; set; }
            public List<Benefit> benefits { get; set; }
        }
        public class Root
        {
            public long searchId { get; set; }
            public List<Property> properties { get; set; }
        }
        public class Surcharge
        {
            public int id { get; set; }
            public string method { get; set; }
            public string charge { get; set; }
            public string margin { get; set; }
            public string name { get; set; }
            public Rate rate { get; set; }
        }
        public class TaxBreakdown
        {
            public string id { get; set; }
            public string typeValue { get; set; }
            public string taxDescription { get; set; }
            public string translatedTaxDescription { get; set; }
            public string method { get; set; }
            public string currency { get; set; }
            public string @base { get; set; }
            public string taxable { get; set; }
            public double percent { get; set; }
            public double amount { get; set; }
        }
        public class TotalPayment
        {
            public double exclusive { get; set; }
            public double inclusive { get; set; }
            public double tax { get; set; }
            public double fees { get; set; }
            public int taxDueSupplier { get; set; }
        }
    }
    #region Room Search
    public class Agoda_NewRoomSearch
    {
        // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
        public class Benefit
        {
            public int id { get; set; }
            public string benefitName { get; set; }
            public string translatedBenefitName { get; set; }
        }
        public class CancellationPolicy
        {
            public string code { get; set; }
            public string cancellationText { get; set; }
            public string translatedCancellationText { get; set; }
            public List<Parameter> parameter { get; set; }
            public List<Date> date { get; set; }
            public List<PolicyParameter> policyparameter { get; set; }
        }
        public class DailyRate
        {
            public string date { get; set; }
            public double exclusive { get; set; }
            public double inclusive { get; set; }
            public double tax { get; set; }
            public double fees { get; set; }
        }
        public class PolicyParameter
        {
            public string FromDate { get; set; }
            public string ToDate { get; set; }
            public double ChargeAmount { get; set; }
            public bool IsZeroCharge { get; set; }
            public string DeadLine { get; set; }
            public double VatdiffAmount { get; set; }
        }
        public class Date
        {
            public DateTime before { get; set; }
            public Rate rate { get; set; }
            public DateTime onward { get; set; }
        }
        public class Parameter
        {
            public int days { get; set; }
            public string charge { get; set; }
            public int value { get; set; }
        }
        public class Property
        {
            public int propertyId { get; set; }
            public string propertyName { get; set; }
            public string translatedPropertyName { get; set; }
            public List<Room> rooms { get; set; }
        }

        public class Rate
        {
            public string currency { get; set; }
            public double exclusive { get; set; }
            public double inclusive { get; set; }
            public double tax { get; set; }
            public double fees { get; set; }
            public string method { get; set; }
        }
        public class PromotionDetail
        {
            public int promotionId { get; set; }
            public bool codeEligible { get; set; }
            public string description { get; set; }
            public double savingAmount { get; set; }
        }
        public class Room
        {
            public int roomId { get; set; }
            public string blockId { get; set; }
            public string roomName { get; set; }
            public string parentRoomName { get; set; }
            public string translatedRoomName { get; set; }
            public string blockIdBackup { get; set; }
            public int parentRoomId { get; set; }
            public int ratePlanId { get; set; }
            public bool freeWifi { get; set; }
            public int remainingRooms { get; set; }
            public int normalBedding { get; set; }
            public int extraBeds { get; set; }
            public bool freeBreakfast { get; set; }
            public bool freeCancellation { get; set; }
            public TotalPayment totalPayment { get; set; }
            public bool roomTypeNotGuaranteed { get; set; }
            public string paymentModel { get; set; }
            public Rate rate { get; set; }
            public PromotionDetail promotionDetail { get; set; }

            public List<DailyRate> dailyRate { get; set; }
            public List<Surcharge> surcharges { get; set; }
            public List<TaxBreakdown> taxBreakdown { get; set; }
            public CancellationPolicy cancellationPolicy { get; set; }
            public List<Benefit> benefits { get; set; }
        }
        public class Root
        {
            public long searchId { get; set; }
            public List<Property> properties { get; set; }

        }
        public class Surcharge
        {
            public int id { get; set; }
            public string method { get; set; }
            public string charge { get; set; }
            public string margin { get; set; }
            public string name { get; set; }
            public Rate rate { get; set; }
        }
        public class TaxBreakdown
        {
            public string id { get; set; }
            public string typeValue { get; set; }
            public string taxDescription { get; set; }
            public string translatedTaxDescription { get; set; }
            public string method { get; set; }
            public string currency { get; set; }
            public string @base { get; set; }
            public string taxable { get; set; }
            public double percent { get; set; }
            public double amount { get; set; }
        }
        public class TotalPayment
        {
            public double exclusive { get; set; }
            public double inclusive { get; set; }
            public double tax { get; set; }
            public double fees { get; set; }
            public int taxDueSupplier { get; set; }
        }
    }
    #endregion

    #region Ess
    public class Agoda_NewESS
    {
        // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
        public class Address
        {
            public int hotelId { get; set; }
            public string addressType { get; set; }
            public string addressLine1 { get; set; }
            public string addressLine2 { get; set; }
            public string postalCode { get; set; }
            public string state { get; set; }
            public string city { get; set; }
            public string country { get; set; }
        }
        public class Addresses
        {
            public List<Address> address { get; set; }
        }
        public class ChildAndExtrabedPolicy
        {
            public string infantAge { get; set; }
            public string childrenAgeFrom { get; set; }
            public string childrenAgeTo { get; set; }
            public string childrenStayFree { get; set; }
            public string minGuestAge { get; set; }
        }

        public class Description
        {
            public int hotelId { get; set; }
            public string overview { get; set; }
            public string snippet { get; set; }
        }
        public class Descriptions
        {
            public List<Description> description { get; set; }
        }

        public class Facilities
        {
            public List<Facility> facility { get; set; }
        }

        public class Facility
        {
            public int hotelId { get; set; }
            public string propertyGroupDescription { get; set; }
            public int propertyId { get; set; }
            public string propertyName { get; set; }
            public string propertyTranslatedName { get; set; }
        }
        public class Hotel
        {
            public int hotelId { get; set; }
            public string hotelName { get; set; }
            public string hotelFormerlyName { get; set; }
            public string translatedName { get; set; }
            public double starRating { get; set; }
            public int continentId { get; set; }
            public int countryId { get; set; }
            public int cityId { get; set; }
            public int areaId { get; set; }
            public double longitude { get; set; }
            public double latitude { get; set; }
            public string hotelUrl { get; set; }
            public int popularityScore { get; set; }
            public string remark { get; set; }
            public int numberOfReviews { get; set; }
            public double ratingAverage { get; set; }
            public ChildAndExtrabedPolicy childAndExtrabedPolicy { get; set; }
            public string accommodationType { get; set; }
            public string nationalityRestrictions { get; set; }
        }
        public class HotelFullFeed
        {
            public Hotels hotels { get; set; }
            public Addresses addresses { get; set; }
            public Descriptions descriptions { get; set; }
            public Facilities facilities { get; set; }
            public Pictures pictures { get; set; }
            public Roomtypes roomtypes { get; set; }
        }
        public class Hotels
        {
            public List<Hotel> hotel { get; set; }
        }
        public class Picture
        {
            public int hotelId { get; set; }
            public int pictureId { get; set; }
            public string caption { get; set; }
            public string captionTranslated { get; set; }
            public string URL { get; set; }
        }
        public class Pictures
        {
            public List<Picture> picture { get; set; }
        }
        public class Roomtype
        {
            public int hotelId { get; set; }
            public int hotelRoomtypeId { get; set; }
            public string standardCaption { get; set; }
            public string standardCaptionTranslated { get; set; }
            public int maxOccupancyPerRoom { get; set; }
            public int noOfRoom { get; set; }
            public double sizeOfRoom { get; set; }
            public bool roomSizeInclTerrace { get; set; }
            public string views { get; set; }
            public int maxExtrabeds { get; set; }
            public int maxInfantInRoom { get; set; }
            public string hotelRoomtypePicture { get; set; }
            public string bedType { get; set; }
            public int hotelMasterRoomtypeId { get; set; }
            public string hotelRoomtypeAlternateName { get; set; }
            public bool sharedBathroom { get; set; }
            public string gender { get; set; }
        }
        public class Roomtypes
        {
            public List<Roomtype> roomtype { get; set; }
        }
        public class Root
        {
            public HotelFullFeed hotelFullFeed { get; set; }
        }
    }
    #endregion

    #region Pre Check
    public class PreCheckAgodaNew
    {
        // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
        public class PrecheckDetails
        {
            public long searchId { get; set; }
            public string checkIn { get; set; }
            public string checkOut { get; set; }
            public Property property { get; set; }
            public string userCountry { get; set; }
            public string language { get; set; }
        }
        public class Property
        {
            public int propertyId { get; set; }
            public List<Room> rooms { get; set; }
        }
        public class Rate
        {
            public double inclusive { get; set; }
        }
        public class Room
        {
            public string currency { get; set; }
            public string paymentModel { get; set; }
            public string blockId { get; set; }
            public int count { get; set; }
            public int adults { get; set; }
            public int children { get; set; }
            public Rate rate { get; set; }
            public int promotionId { get; set; }
            public List<Surcharge> surcharges { get; set; }
        }
        public class Root
        {
            public PrecheckDetails precheckDetails { get; set; }
        }
        public class Surcharge
        {
            public int id { get; set; }
            public Rate rate { get; set; }
        }
    }
    #endregion
    public class Agoda_newBookingAvalibility
    {
        // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
        public class Benefit
        {
            public int id { get; set; }
            public string benefitName { get; set; }
            public string translatedBenefitName { get; set; }
        }
        public class CancellationPolicy
        {
            public string code { get; set; }
            public string cancellationText { get; set; }
            public string translatedCancellationText { get; set; }
            public List<Parameter> parameter { get; set; }
            public List<Date> date { get; set; }
        }
        public class DailyRate
        {
            public string date { get; set; }
            public double exclusive { get; set; }
            public double inclusive { get; set; }
            public double tax { get; set; }
            public double fees { get; set; }
        }
        public class Date
        {
            public DateTime before { get; set; }
            public Rate rate { get; set; }
            public DateTime? onward { get; set; }
        }
        public class Parameter
        {
            public int days { get; set; }
            public string charge { get; set; }
            public int value { get; set; }
        }
        public class Property
        {
            public int propertyId { get; set; }
            public string propertyName { get; set; }
            public string translatedPropertyName { get; set; }
            public List<Room> rooms { get; set; }
        }
        public class Rate
        {
            public string currency { get; set; }
            public double exclusive { get; set; }
            public double inclusive { get; set; }
            public double tax { get; set; }
            public double fees { get; set; }
            public string method { get; set; }
        }
        public class Room
        {
            public int roomId { get; set; }
            public string blockId { get; set; }
            public string roomName { get; set; }
            public string parentRoomName { get; set; }
            public string translatedRoomName { get; set; }
            public string blockIdBackup { get; set; }
            public int parentRoomId { get; set; }
            public int ratePlanId { get; set; }
            public bool freeWifi { get; set; }
            public int remainingRooms { get; set; }
            public int normalBedding { get; set; }
            public int extraBeds { get; set; }
            public bool freeBreakfast { get; set; }
            public bool freeCancellation { get; set; }
            public TotalPayment totalPayment { get; set; }
            public bool roomTypeNotGuaranteed { get; set; }
            public string paymentModel { get; set; }
            public Rate rate { get; set; }
            public List<DailyRate> dailyRate { get; set; }
            public List<Surcharge> surcharges { get; set; }
            public List<TaxBreakdown> taxBreakdown { get; set; }
            public CancellationPolicy cancellationPolicy { get; set; }
            public List<Benefit> benefits { get; set; }
        }
        public class Root
        {
            public long searchId { get; set; }
            public List<Property> properties { get; set; }
        }
        public class Surcharge
        {
            public int id { get; set; }
            public string method { get; set; }
            public string charge { get; set; }
            public string margin { get; set; }
            public string name { get; set; }
            public Rate rate { get; set; }
        }
        public class TaxBreakdown
        {
            public string id { get; set; }
            public string typeValue { get; set; }
            public string taxDescription { get; set; }
            public string translatedTaxDescription { get; set; }
            public string method { get; set; }
            public string currency { get; set; }
            public string @base { get; set; }
            public string taxable { get; set; }
            public double percent { get; set; }
            public double amount { get; set; }
        }
        public class TotalPayment
        {
            public double exclusive { get; set; }
            public double inclusive { get; set; }
            public double tax { get; set; }
            public double fees { get; set; }
            public int taxDueSupplier { get; set; }
        }
    }
    public class precheck_new
    {
        // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
        public class PrecheckDetails
        {
            public long searchId { get; set; }
            public string tag { get; set; }
            public bool allowDuplication { get; set; }
            public string checkIn { get; set; }
            public string checkOut { get; set; }
            public Property property { get; set; }
            public string language { get; set; }
            public string userCountry { get; set; }
        }
        public class Property
        {
            public int propertyId { get; set; }
            public List<Room> rooms { get; set; }
        }
        public class Rate
        {
            public double inclusive { get; set; }
        }
        public class Room
        {
            public string currency { get; set; }
            public string paymentModel { get; set; }
            public string blockId { get; set; }
            public int count { get; set; }
            public int adults { get; set; }
            public int children { get; set; }
            public List<int> childrenAges { get; set; }
            public Rate rate { get; set; }
            public List<Surcharge> surcharges { get; set; }
        }
        public class Root
        {
            public PrecheckDetails precheckDetails { get; set; }
        }
        public class Surcharge
        {
            public int id { get; set; }
            public Rate rate { get; set; }
        }
    }
    public class Agoda_newPreCheckResponse
    {

        public class Root
        {
            public int status { get; set; }
            public List<object> errorList { get; set; }
        }

    }
    public class Agoda_newBookingRQ
    {
        public class BookingDetails
        {
            public string userCountry { get; set; }
            public long searchId { get; set; }
            public string tag { get; set; }
            public bool allowDuplication { get; set; }
            public string checkIn { get; set; }
            public string checkOut { get; set; }
            public string language { get; set; }
            public Property property { get; set; }
        }

        public class CreditCardInfo
        {
            public string cardType { get; set; }
            public string number { get; set; }
            public string expiryDate { get; set; }
            public string cvc { get; set; }
            public string holderName { get; set; }
            public string countryOfIssue { get; set; }
            public string issuingBank { get; set; }
        }

        public class CustomerDetail
        {
            public string language { get; set; }
            public string title { get; set; }
            public string firstName { get; set; }
            public string lastName { get; set; }
            public string email { get; set; }
            public Phone phone { get; set; }
            public bool newsletter { get; set; }
        }

        public class GuestDetail
        {
            public string title { get; set; }
            public string firstName { get; set; }
            public string lastName { get; set; }
            public string countryOfResidence { get; set; }
            public string gender { get; set; }
            public int age { get; set; }
            public bool primary { get; set; }
        }

        public class PaymentDetails
        {
            public CreditCardInfo creditCardInfo { get; set; }
        }

        public class Phone
        {
            public string countryCode { get; set; }
            public string areaCode { get; set; }
            public string number { get; set; }
        }

        public class Property
        {
            public int propertyId { get; set; }
            public List<Room> rooms { get; set; }
        }

        public class Rate
        {
            public double inclusive { get; set; }
        }

        public class Room
        {
            public string blockId { get; set; }
            public Rate rate { get; set; }
            public List<Surcharge> surcharges { get; set; }
            public List<GuestDetail> guestDetails { get; set; }
            public string currency { get; set; }
            public string paymentModel { get; set; }
            public int count { get; set; }
            public int adults { get; set; }
            public int children { get; set; }
            public List<int> childrenAges { get; set; }
            public string specialRequest { get; set; }
        }

        public class Root
        {
            public int waitTime { get; set; }
            public BookingDetails bookingDetails { get; set; }
            public CustomerDetail customerDetail { get; set; }
            public PaymentDetails paymentDetails { get; set; }
        }

        public class Surcharge
        {
            public int id { get; set; }
            public Rate rate { get; set; }
        }


    }
    public class Agoda_newBookingRS
    {
        // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
        public class BookingDetail
        {
            public int id { get; set; }
            public int itineraryID { get; set; }
            public string selfService { get; set; }
            public bool processing { get; set; }
        }

        public class Root
        {
            public string status { get; set; }
            public List<BookingDetail> bookingDetails { get; set; }
        }
    }
    public class Agoda_newCancelRQ
    {
        // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
        public class Root
        {
            public int bookingId { get; set; }
        }
    }
    public class Agoda_newCancelRS
    {
        // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
        public class CancellationPolicy
        {
            public string language { get; set; }
            public string policyText { get; set; }
        }
        public class CancellationSummary
        {
            public int bookingId { get; set; }
            public int reference { get; set; }
            public List<CancellationPolicy> cancellationPolicy { get; set; }
            public List<PaymentRate> paymentRate { get; set; }
            public List<RefundRate> refundRate { get; set; }
        }
        public class PaymentRate
        {
            public string currency { get; set; }
            public double inclusive { get; set; }
        }
        public class RefundRate
        {
            public string currency { get; set; }
            public double inclusive { get; set; }
        }
        public class Root
        {
            public CancellationSummary cancellationSummary { get; set; }
        }
    }
    public class Agoda_newConfirmCancelRQ
    {
        // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
        public class RefundRate
        {
            public string currency { get; set; }
            public double inclusive { get; set; }
        }
        public class Root
        {
            public int bookingId { get; set; }
            public int reference { get; set; }
            public int cancelReason { get; set; }
            public RefundRate refundRate { get; set; }
        }
    }
    public class Agoda_newConfirmCancelRS
    {
        // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
        public class ErrorMessage
        {
            public string id { get; set; }
            public string message { get; set; }
        }
        public class Root
        {
            public ErrorMessage errorMessage { get; set; }
        }
    }
    #endregion
}
