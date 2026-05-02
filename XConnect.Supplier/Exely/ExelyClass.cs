namespace XConnect.Supplier.Exely
{
    public class ExelyClass
    {
    }

    #region Search API

    #region Search for the lowest priced accommodation options across all available accommodations.The maximum number of accommodation to be searched Request 

    public class SimpleSearchCriteria
    {
        public List<string>? PropertyIds { get; set; } 
        public int Adults { get; set; }
        public List<int>? ChildAges { get; set; }
        public string? Include { get; set; }
        public DateTime ArrivalDate { get; set; }
        public DateTime DepartureDate { get; set; }
        public MealPreference? MealPreference { get; set; }
        public PricePreference? PricePreference { get; set; }
    }

    public class MealPreference
    {
        public MealPreferenceType MealType { get; set; }
        public MealsIncluded? MealsIncluded { get; set; }
    }

    public class MealsIncluded
    {
        public List<MealPlanType>? MealPlanCodes { get; set; }
    }

    public class PricePreference
    {
        public string? CurrencyCode { get; set; }
        public double? MinPrice { get; set; }
        public double? MaxPrice { get; set; }
    }

    #endregion

    #region Search for the lowest priced accommodation options across all available accommodations.The maximum number of accommodation to be searched Response

    public class AggregationSearchByPropertiesResponse
    {
        public List<ShortRoomStay> roomStays { get; set; }
        public List<Warning>? warnings { get; set; }
    }

    public class ShortRoomStay
    {
        public string propertyId { get; set; }
        public DetailedRoomType roomType { get; set; }
        public RatePlan ratePlan { get; set; }
        public GuestCount? guestCount { get; set; }
        public StayDates? stayDates { get; set; }
        public string currencyCode { get; set; }
        public Total total { get; set; }
        public CancellationPolicy? cancellationPolicy { get; set; }
        public List<IncludedRoomStayService>? includedServices { get; set; }
        public string mealPlanCode { get; set; }
        public string bookingFormLink { get; set; }
        public List<AvailableDiscount>? availableDiscounts { get; set; }
        public string fullPlacementsName { get; set; }
        public int availability { get; set; }
        public string? checksum { get; set; }
    }

    #endregion

    #region RoomStays

    #region Search for accommodation options by specific accommodation Request
    public class SearchByPropertyRequest
    {
        public string propertyId { get; set; }
        public string arrivalDate { get; set; }
        public string departureDate { get; set; }
        public int adults { get; set; }
        public List<int>? childAges { get; set; }
        public bool? includeExtraStays { get; set; }
        public bool? includeExtraServices { get; set; }

    }

    #endregion

    #region Search for accommodation options by specific accommodation Response

    public class SearchByPropertyResponse
    {
        public List<DetailedRoomStay> roomStays { get; set; }
        public List<Warning>? warnings { get; set; }
    }

    public class DetailedRoomStay
    {
        public string propertyId { get; set; }
        public DetailedRoomType roomType { get; set; }
        public RatePlan ratePlan { get; set; }
        public GuestCount guestCount { get; set; }
        public StayDates stayDates { get; set; }
        public int availability { get; set; }
        public string currencyCode { get; set; }
        public Total total { get; set; }
        public CancellationPolicy cancellationPolicy { get; set; }
        public List<IncludedRoomStayService>? includedServices { get; set; }
        public string mealPlanCode { get; set; }
        public string checksum { get; set; }
        public string fullPlacementsName { get; set; }
        public string bookingFormLink { get; set; }
        public List<AvailableDiscount>? availableDiscounts { get; set; }
        public bool extraServicesAvailable { get; set; }
        public ExtraStaysByPrice? extraStays { get; set; }
        public List<RoomStayExtraService>? extraServices { get; set; }
    }

    public class DetailedRoomType
    {
        public string id { get; set; }
        public List<Placement> placements { get; set; }
    }

    public class Placement
    {
        public string code { get; set; }
        public int count { get; set; }
        public BookingPlacementKind kind { get; set; }
        public int? minAge { get; set; }
        public int? maxAge { get; set; }
    }

    public class RatePlan
    {
        public string id { get; set; }
    }

    public class GuestCount
    {
        public int adultCount { get; set; }
        public List<int> childAges { get; set; }
    }

    public class StayDates
    {
        public DateTime arrivalDateTime { get; set; }
        public DateTime departureDateTime { get; set; }
    }

    public class Total
    {
        public double priceBeforeTax { get; set; }
        public double taxAmount { get; set; }
        public List<Tax>? taxes { get; set; }
    }

    public class Tax
    {
        public double amount { get; set; }
        public string name { get; set; }
        public string? description { get; set; }
    }

    public class CancellationPolicy
    {
        public bool freeCancellationPossible { get; set; }
        public DateTime? freeCancellationDeadlineLocal { get; set; }
        public DateTime? freeCancellationDeadlineUtc { get; set; }
        public double? penaltyAmount { get; set; }
    }

    public class IncludedRoomStayService
    {
        public string id { get; set; }
        public string? mealPlanCode { get; set; }
    }

    public class AvailableDiscount
    {
        public DiscountReason reason { get; set; }
        public double amount { get; set; }
    }

    public class ExtraStaysByPrice
    {
        public List<ExtraStayService> earlyCheckIn { get; set; }
        public List<ExtraStayService> lateCheckOut { get; set; }
    }

    public class ExtraStayService
    {
        public Total total { get; set; }
        public string currencyCode { get; set; }
        public List<string> dateTimesLocal { get; set; }
        public bool isOccupyQuota { get; set; }
    }

    public class RoomStayExtraService
    {
        public string id { get; set; }
        public ServiceKind kind { get; set; }
        public string mealPlanCode { get; set; }
        public int? maxQuantity { get; set; }
        public int? quantity { get; set; }
        public string currencyCode { get; set; }
        public CalculationMethod calculationMethod { get; set; }
        public Total total { get; set; }
        public QuantityByGuests? quantityByGuests { get; set; }
    }

    public class QuantityByGuests
    {
        public int? adultsServiceQuantity { get; set; }
        public List<ChildrenServiceCount>? childrenServiceQuantity { get; set; }
    }

    public class ChildrenServiceCount
    {
        public int age { get; set; }
        public int count { get; set; }
    }

    public class Warning
    {
        public string? Code { get; set; }
        public string? Message { get; set; }
    }

    #endregion

    #endregion

    #region Services

    #region Search for Extra Services by accommodation Conditions Requests

    public class SearchServicesCriteria
    {
        public StayDates stayDates { get; set; }
        public RoomTypeRq roomType { get; set; }
        public RatePlan ratePlan { get; set; }
        public GuestCount guestCount { get; set; }
    }


    public class RoomTypeRq
    {
        public string id { get; set; }
        public List<PlacementRq> placements { get; set; }
    }

    public class PlacementRq
    {
        public string code { get; set; }
    }

    #endregion

    #region Search for Extra Services by accommodation Conditions Response

    public class SearchExtraServicesByPropertyResponse
    {
        public List<RoomStayExtraService>? roomStayExtraServices { get; set; }
    }

    #endregion

    #endregion

    #region ExtraStays

    #region Search for Extra Stays by accommodation Conditions Requests
    public class SearchExtraStaysCriteria
    {
        public StayDates? stayDates { get; set; }
        public RoomTypeRq? roomType { get; set; }
        public RatePlan? ratePlan { get; set; }
        public GuestCount? guestCount { get; set; }
    }

    #endregion

    #region Search for Extra Stays by accommodation Conditions Response
    public class SearchExtraStaysByPropertyResponse
    {
        public ExtraStays? extraStays { get; set; }
    }
    public class ExtraStays
    {
        public List<ExtraStay>? earlyCheckIn { get; set; }
        public List<ExtraStay>? lateCheckOut { get; set; }
    }

    public class ExtraStay
    {
        public Total total { get; set; }
        public string currencyCode { get; set; }
        public DateTime dateTimeLocal { get; set; }
        public bool isOccupyQuota { get; set; }
    }
    #endregion

    #endregion

    #endregion

    #region Reservation API

    #region Create Booking Request

    public class CreateBookingRequest
    {
        public CreateBookingRq booking { get; set; }
    }

    public class CreateBookingRq
    {
        public string propertyId { get; set; }
        public List<BookingRoomStayRq> roomStays { get; set; }
        public List<BookingServiceRq>? services { get; set; }
        public Customer customer { get; set; }
        public Prepayment prepayment { get; set; }
        public List<string>? bookingComments { get; set; }
        public string createBookingToken { get; set; }
    }

    public class BookingPlacementRq
    {
        public string code { get; set; }
    }

    public class BookingServiceRq
    {
        public string id { get; set; }
    }

    public class RoomStayService
    {
        public string id { get; set; }
        public int quantity { get; set; }
        public object quantityByGuests { get; set; }
    }

    public class ExtraStayRequest
    {
        public ExtraStayDetails earlyArrival { get; set; }
        public ExtraStayDetails lateDeparture { get; set; }
    }

    public class ExtraStayDetails
    {
        public DateTime overriddenDateTime { get; set; }
    }

    public class BookingDiscountRq
    {
        public BookingDiscountKind kind { get; set; }
        public decimal amount { get; set; }
    }

    public class Customer
    {
        public string firstName { get; set; }
        public string lastName { get; set; }
        public string middleName { get; set; }
        public string citizenship { get; set; }
        public Contacts contacts { get; set; }
        public string comment { get; set; }
    }

    public class Contacts
    {
        public List<Phone> phones { get; set; }
        public List<Email> emails { get; set; }
    }

    //public class Phone
    //{
    //    public string phoneNumber { get; set; }
    //}

    public class Email
    {
        public string emailAddress { get; set; }
    }

    public class Prepayment
    {
        public string remark { get; set; }
        public string paymentType { get; set; }
        public decimal prepaidSum { get; set; }
    }

    #endregion

    #region Create Booking Response

    public class CreateBookingResult
    {
        public CreatedBooking booking { get; set; }
    }

    public class CreatedBooking
    {
        public string propertyId { get; set; }
        public List<BookingRoomStayRs> roomStays { get; set; }
        public List<BookingServiceRs>? services { get; set; }
        public BookingCustomer customer { get; set; }
        public BookingPrepayment? prepayment { get; set; }
        public List<string>? bookingComments { get; set; }
        public BookingTotal total { get; set; }
        public List<BookingTax> taxes { get; set; }
        public string? currencyCode { get; set; }
        public BookingCancellation cancellation { get; set; }
        public CancellationPolicy cancellationPolicy { get; set; }
        public string? number { get; set; }
        public BookingStatus status { get; set; }
        public DateTime createdDateTime { get; set; }
        public DateTime modifiedDateTime { get; set; }
        public string? version { get; set; }
    }

    public class BookingRoomStayRs
    {
        public BookingStayDates stayDates { get; set; }
        public BookingRatePlanRs ratePlan { get; set; }
        public BookingRoomTypeRs roomType { get; set; }
        public List<BookingGuest> guests { get; set; }
        public BookingGuestCount guestCount { get; set; }
        public string checksum { get; set; }
        public List<BookingDailyRate>? dailyRates { get; set; }
        public BookingTotal total { get; set; }
        public List<RoomStayServiceRs>? services { get; set; }
        public ExtraStaysCharge? extraStayCharge { get; set; }
    }

    public class BookingStayDates
    {
        public DateTime arrivalDateTime { get; set; }
        public DateTime departureDateTime { get; set; }
    }

    public class BookingRatePlanRs
    {
        public string id { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public Vat? vat { get; set; }
    }

    public class Vat
    {
        public bool applicable { get; set; }
        public bool? included { get; set; }
        public int? percent { get; set; }
    }

    public class BookingRoomTypeRs
    {
        public string id { get; set; }
        public List<BookingPlacementRs> placements { get; set; }
        public string name { get; set; }
    }

    public class BookingPlacementRs
    {
        public string code { get; set; }
        public int count { get; set; }
        public BookingPlacementKind kind { get; set; }
        public int? minAge { get; set; }
        public int? maxAge { get; set; }
    }

    public class BookingGuest
    {
        public string firstName { get; set; }
        public string lastName { get; set; }
        public string? middleName { get; set; }
        public string? citizenship { get; set; }

        public BookingGuestSexKind sex { get; set; }
    }

    public class BookingGuestCount
    {
        public int adultCount { get; set; }
        public List<int>? childAges { get; set; }
    }

    public class BookingDailyRate
    {
        public double? priceBeforeTax { get; set; }
        public DateTime? date { get; set; }
    }

    public class RoomStayServiceRs
    {
        public string id { get; set; }
        public int? quantity { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public double? totalPrice { get; set; }
        public ServiceTotal serviceTotal { get; set; }
        public bool inclusive { get; set; }
        public ServiceKind kind { get; set; }
        public string? mealPlanCode { get; set; }
        public string? mealPlanName { get; set; }
        public Vat vat { get; set; }
        public QuantityByGuests? quantityByGuests { get; set; }
    }

    public class BookingTotal
    {
        public double priceBeforeTax { get; set; }
        public double taxAmount { get; set; }
        public List<BookingTaxAmount>? taxes { get; set; }
        public double discountAmount { get; set; }
        public List<BookingDiscountAmount>? discounts { get; set; }
    }

    public class BookingTaxAmount
    {
        public double amount { get; set; }
        public int index { get; set; }
    }

    public class BookingDiscountAmount
    {
        public double amount { get; set; }
        public BookingDiscountKind kind { get; set; }
    }

    public class ExtraStaysCharge
    {
        public ExtraStayDetailsRs? earlyArrival { get; set; }
        public ExtraStayDetailsRs? lateDeparture { get; set; }
    }

    public class ExtraStayDetailsRs
    {
        public DateTime? overriddenDateTime { get; set; }
        public ExtraStaysTotal total { get; set; }
    }

    public class ExtraStaysTotal
    {
        public double priceBeforeTax { get; set; }
        public double taxAmount { get; set; }
        public List<BookingTaxAmount>? taxes { get; set; }
    }


    public class BookingServiceRs
    {
        public string id { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public double price { get; set; }
    }

    public class BookingCustomer
    {
        public string firstName { get; set; }
        public string lastName { get; set; }
        public string? middleName { get; set; }
        public string? citizenship { get; set; }
        public BookingPersonContacts contacts { get; set; }
        public string? comment { get; set; }
    }

    public class BookingPersonContacts
    {
        public List<BookingPersonPhone> phones { get; set; }
        public List<BookingPersonEmail> emails { get; set; }
    }

    public class BookingPersonPhone
    {
        public string phoneNumber { get; set; }
    }

    public class BookingPersonEmail
    {
        public string emailAddress { get; set; }
    }

    public class BookingPrepayment
    {
        public string? remark { get; set; }
        public PaymentType paymentType { get; set; }
        public double? prepaidSum { get; set; }
    }

    public class BookingTax
    {
        public int index { get; set; }
        public string? name { get; set; }
        public string? description { get; set; }
    }

    public class BookingCancellation
    {
        public double? penaltyAmount { get; set; }
        public string? reason { get; set; }
        public DateTime cancelledUtc { get; set; }
    }

    public class ExelyBookRsDto
    {
        public int BookingStatus { get; set; } = 2;
        public string AgentVoucherNo { get; set; } = string.Empty;
        public double TotalAmount { get; set; }
        public string CurrencyCode { get; set; } = string.Empty;
    }

    #endregion

    #region Receive Booking Response
    // Same As Modify Booking Response 
    //public class GetBookingRs
    //{
    //    public CreatedBooking Booking { get; set; }
    //}

    #endregion

    #region Modify Booking Request

    public class ModifyBookingRequest
    {
        public string description { get; set; }
        public ModifyBookingRq booking { get; set; }
    }

    public class ModifyBookingRq
    {
        public string propertyId { get; set; }
        public List<BookingRoomStayRq> roomStays { get; set; }
        public List<BookingServiceRq> services { get; set; }
        public BookingCustomer customer { get; set; }
        public BookingPrepayment prepayment { get; set; }
        public List<string> bookingComments { get; set; }
        public string version { get; set; }
    }

    public class BookingRoomStayRq
    {
        public BookingStayDates stayDates { get; set; }
        public BookingRatePlanRq ratePlan { get; set; }
        public BookingRoomTypeRq roomType { get; set; }
        public List<BookingGuest> guests { get; set; }
        public BookingGuestCount guestCount { get; set; }
        public string checksum { get; set; }
        public List<RoomStayServiceRq>? services { get; set; }
        public ExtraStayRequest? extraStay { get; set; }
        public List<BookingDiscountRq> discounts { get; set; }
    }

    public class BookingRatePlanRq
    {
        public string id { get; set; }
    }

    public class BookingRoomTypeRq
    {
        public string id { get; set; }
        public List<BookingPlacementRq> placements { get; set; }
    }

    public class RoomStayServiceRq
    {
        public string id { get; set; }
        public int? quantity { get; set; }
        public QuantityByGuests? quantityByGuests { get; set; }
    }


    #endregion

    #region Modify Booking Response

    public class GetBookingRs
    {
        public CreatedBooking Booking { get; set; }
    }

    public class ServiceTotal
    {
        public double priceBeforeTax { get; set; }

        public double taxAmount { get; set; }

        public List<BookingTaxAmount>? taxes { get; set; }
    }

    #endregion

    #region Checking Possibility of Booking Modification Request

    // same as modify Booking Request

    //public class ModifyBookingRequest
    //{
    //    public string description { get; set; }
    //    public ModifyBookingRq booking { get; set; }
    //}


    #endregion

    #region Checking Possibility of Booking Modification Response

    public class VerifyModificationRs
    {
        public VerifyModificationResult Booking { get; set; }
    }

    public class VerifyModificationResult
    {
        public string propertyId { get; set; }
        public List<BookingRoomStayRs> roomStays { get; set; }
        public List<BookingServiceRs>? services { get; set; }
        public BookingCustomer customer { get; set; }
        public BookingPrepayment? prepayment { get; set; }
        public List<string>? bookingComments { get; set; }
        public BookingTotal total { get; set; }
        public List<BookingTax> taxes { get; set; }
        public string? currencyCode { get; set; }
        public BookingCancellation cancellation { get; set; }
        public CancellationPolicy cancellationPolicy { get; set; }
        public string? number { get; set; }
        public BookingStatus status { get; set; }
        public DateTime createdDateTime { get; set; }
        public DateTime modifiedDateTime { get; set; }
    }

    #endregion

    #region Cancel Booking Request

    public class BookingCancellationRq
    {
        public string? reason { get; set; }

        public double? expectedPenaltyAmount { get; set; }
    }

    #endregion

    #region Cancel Booking Response
    // Same As Modify Booking Response 

    //public class GetBookingRs
    //{
    //    public CreatedBooking Booking { get; set; }
    //}

    #endregion

    #region Calculate Penalty Amount For Cancellation Response

    public class ResultOfCalculatePenalty
    {
        public double? penaltyAmount { get; set; }
    }


    #endregion

    #region Checking Possibility of Booking Creation Request

    public class VerifyBookingRequest
    {
        public BookingRq Booking { get; set; }
    }

    public class BookingRq
    {
        public string PropertyId { get; set; }
        public List<BookingRoomStayRq> RoomStays { get; set; }
        public List<BookingServiceRq> Services { get; set; }
        public BookingCustomer Customer { get; set; }
        public BookingPrepayment Prepayment { get; set; }
        public List<string> BookingComments { get; set; }
    }

    #endregion

    #region Checking Possibility of Booking Creation Response

    public class VerifyBookingResult
    {
        public VerifyBookingRs? booking { get; set; }

        public VerifyBookingRs? alternativeBooking { get; set; }

        public List<Warning>? warnings { get; set; }
    }

    public class VerifyBookingRs
    {
        public string propertyId { get; set; }
        public List<BookingRoomStayRs> roomStays { get; set; }
        public List<BookingServiceRs>? services { get; set; }
        public BookingCustomer customer { get; set; }
        public BookingPrepayment? prepayment { get; set; }
        public List<string>? bookingComments { get; set; }
        public BookingTotal total { get; set; }
        public List<BookingTax> taxes { get; set; }
        public string? currencyCode { get; set; }
        public BookingCancellation cancellation { get; set; }
        public CancellationPolicy cancellationPolicy { get; set; }
        public string createBookingToken { get; set; }
    }



    #endregion

    #endregion

    #region Contents API

    #region CancellationRules

    #region Receive Cancellation Rules

    public class CancellationRulesResponse
    {
        public List<CancellationRule> CancellationRules { get; set; }
    }

    public class CancellationRule
    {
        public string Id { get; set; }
        public string PropertyId { get; set; }
        public ReferencePointKind ReferencePointKind { get; set; }
        public string? ReferencePointTime { get; set; }
        public List<CancellationTerm> CancellationTerms { get; set; }
    }

    public class CancellationTerm
    {
        public BeforeArrivalMatching BeforeArrivalMatching { get; set; }
        public TimeUnit BeforeArrivalUnit { get; set; }
        public int BeforeArrivalValue { get; set; }
        public int BeforeArrivalValueMax { get; set; }
        public PenaltyCalculationMethod PenaltyCalculationMethod { get; set; }
        public double PenaltyValue { get; set; }
        public string PenaltyValueCurrency { get; set; }
        public List<ArrivalDatesInfo> ArrivalDates { get; set; }

    }

    public class ArrivalDatesInfo
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsEndless { get; set; }
    }

    #endregion

    #region Enums

    public enum BookingStatus
    {
        Confirmed,
        Cancelled
    }

    public enum PaymentType
    {
        Cash,
        PrePay
    }

    public enum BookingDiscountKind
    {
        AgentCommission
    }

    public enum BookingGuestSexKind
    {
        Male,
        Female
    }

    public enum BookingPlacementKind
    {
        Adult,
        ExtraAdult,
        Child,
        ExtraChild,
        ChildBandWithoutBed
    }

    public enum DiscountReason
    {
        StayDatesDiscount,
        RegistrationDiscount,
        LoyaltyDiscount
    }

    public enum ServiceKind
    {
        Common,
        Meal
    }

    public enum CalculationMethod
    {
        PerStay,
        PerPerson,
        PerPersonByAge,
        PerUse
    }

    public enum ReferencePointKind
    {
        ProviderArrivalTime,
        ProviderDepartureTime,
        GuestArrivalTime,
        CustomArrivalTime,
        BookingCreationTime
    }

    public enum BeforeArrivalMatching
    {
        NoMatter,
        AtLeast,
        NoMoreThan,
        Between
    }

    public enum TimeUnit
    {
        None,
        Day,
        Hour
    }

    public enum PenaltyCalculationMethod
    {
        PrepaymentPercent,
        FirstNights,
        NoPenalty,
        FirstNightPercent,
        Percent,
        Fixed
    }

    public enum ExtraStayChargeType
    {
        Free,
        Fixed,
        Percent,
        HourlyRate,
        HourlyRateAutoCalculated,
        Forbidden
    }

    public enum ExtraServiceSurchargeOption
    {
        None,
        All,
        MealOnly
    }

    public enum StayUnitKind
    {
        NightRate,
        DailyRate
    }

    public enum PlacementKind
    {
        Adult,
        ExtraAdult,
        Child,
        ExtraChild,
        ChildBandWithoutBed
    }

    public enum AmenityChargeType
    {
        None,
        Free,
        Chargeable
    }

    public enum EntityState
    {
        Inactive,
        Active
    }

    public enum RoomTypeAvailability
    {
        All,
        Selective
    }

    public enum MealPreferenceType
    {
        All,
        MealOnly,
        RoomOnly
    }
    public enum MealPlanType
    {
        AllInclusive,
        BuffetBreakfast,
        ContinentalBreakfast,
        EnglishBreakfast,
        FullBoard,
        HalfBoard,
        BreakFast,
        Lunch,
        Dinner,
        FullBoardBuffet,
        FullBoardCarte,
        BuffetDinner,
        DinnerCarte,
        Custom,
        HalfBoardLunch,
        HalfBoardDinner,
        AmericanBreakfast,
        HalfBoardLunchDinner,
        UltraAllInclusive
    }

    #endregion

    #endregion

    #region Content API

    #region Receive Property Entity Response 

    public class PropertyInfoPage
    {
        public string? Next { get; set; }
        public List<PropertyInfoTypeWithInclude> Properties { get; set; }
        public List<Warning>? Warnings { get; set; }
    }

    public class PropertyInfoTypeWithInclude
    {
        public string Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public List<Resource>? Images { get; set; }
        public int? Stars { get; set; }
        public StayUnitKind StayUnitKind { get; set; }
        public int PropertyKindId { get; set; }
        public ContactInfo ContactInfo { get; set; }
        public PolicyInfo Policy { get; set; }
        public TimeZoneInfo TimeZone { get; set; }
        public List<RatePlanInfo>? RatePlans { get; set; }
        public List<RoomTypeInfo>? RoomTypes { get; set; }
        public List<ServiceInfo>? Services { get; set; }
        public bool? MultiLocationProperty { get; set; }
        public CompanyDetails CompanyDetails { get; set; }
        public string? Currency { get; set; }
        public List<PropertyAmenity> Amenities { get; set; }
    }

    public class Resource
    {
        public string Url { get; set; }
    }

    public class ContactInfo
    {
        public Address Address { get; set; }
        public List<Phone>? Phones { get; set; }
        public List<string>? Emails { get; set; }
    }

    public class Address
    {
        public string? PostalCode { get; set; }
        public string CountryCode { get; set; }
        public string? Region { get; set; }
        public string? RegionId { get; set; }
        public string? CityName { get; set; }
        public string? CityId { get; set; }
        public string? AddressLine { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string? Remark { get; set; }
    }

    public class Phone
    {
        public string phoneNumber { get; set; }
        public string remark { get; set; }
    }

    public class PolicyInfo
    {
        public string CheckInTime { get; set; }
        public string CheckOutTime { get; set; }
    }

    public class TimeZoneInfo
    {
        public string Id { get; set; }
    }

    public class RatePlanInfo
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public string Currency { get; set; }
        public bool IsStayWithChildrenOnly { get; set; }
        public string CancellationRuleId { get; set; }
        public string? ExtraStayRuleId { get; set; }
        public Vat Vat { get; set; }
        public List<Service>? AvailableServices { get; set; }
        public bool CorporateOnly { get; set; }
    }

    public class RoomTypeInfo
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public List<Amenity> Amenities { get; set; }
        public List<Resource> Images { get; set; }
        public RoomTypeSquare Size { get; set; }
        public string CategoryCode { get; set; }
        public string CategoryName { get; set; }
        public Address Address { get; set; }
        public OccupancyInfo Occupancy { get; set; }
        public List<Placements> Placements { get; set; }
        public int? Position { get; set; }
    }

    public class ServiceInfo
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public ServiceKind Kind { get; set; }
        public string? MealPlanCode { get; set; }
        public string? MealPlanName { get; set; }
        public List<Resource>? Images { get; set; }
        public EntityState Status { get; set; }
        public Vat Vat { get; set; }
    }

    public class PropertyAmenity
    {
        public string Code { get; set; }
        public string DisplayName { get; set; }
        public AmenityChargeType ChargeType { get; set; }
    }

    public class Service
    {
        public int Id { get; set; }
        public bool Included { get; set; }
        public List<int> RoomTypeIds { get; set; }
        public RoomTypeAvailability RoomTypeAvailability { get; set; }
    }

    public class Amenity
    {
        public string Code { get; set; }
        public string Name { get; set; }
    }

    public class RoomTypeSquare
    {
        public int Value { get; set; }
    }
    public class OccupancyInfo
    {
        public int AdultBed { get; set; }
        public int ExtraBed { get; set; }
        public int ChildWithoutBed { get; set; }
    }

    public class Placements
    {
        public PlacementKind Kind { get; set; }
        public int Count { get; set; }
        public int? MinAge { get; set; }
        public int? MaxAge { get; set; }
    }

    public class CompanyDetails
    {
        public string? LegalEntityName { get; set; }
        public string? TaxpayerIdentificationNumber { get; set; }
        public string? CompanyRegistrationNumber { get; set; }
        public string? LegalAddress { get; set; }
    }

    #endregion

    #region Receive Description of a Property Response

    public class PropertyInfoType
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public string? Currency { get; set; }

        public List<Resource>? Images { get; set; }

        public int? Stars { get; set; }

        public StayUnitKind StayUnitKind { get; set; }

        public int PropertyKindId { get; set; }

        public ContactInfo ContactInfo { get; set; }

        public PolicyInfo Policy { get; set; }

        public TimeZoneInfo TimeZone { get; set; }

        public List<RatePlanInfo> RatePlans { get; set; }

        public List<RoomTypeInfo> RoomTypes { get; set; }

        public List<ServiceInfo> Services { get; set; }

        public bool MultiLocationProperty { get; set; }

        public CompanyDetails? CompanyDetails { get; set; }

        public List<PropertyAmenity> Amenities { get; set; }
    }

    #endregion

    #region Receive entity of Possible Meal options Response 

    public class MealPlan
    {
        public string? Code { get; set; }
        public string? Name { get; set; }
    }

    #endregion

    #region Receive entity of Possible Room Types Response

    public class RoomTypeCategory
    {
        public string? Code { get; set; }
        public string? Name { get; set; }
    }

    #endregion

    #region Receive entity of Possible Room Amenities Response

    public class RoomAmenitiesCategories
    {
        public string Name { get; set; }

        public List<AmenityInfo>? Amenities { get; set; }
    }

    public class AmenityInfo
    {
        public string Code { get; set; }

        public string? Name { get; set; }
    }

    #endregion

    #endregion

    #region ExtraStayRules

    #region Receive Early CheckIn and Late CheckOut Rules Response

    public class ExtraStayRulesResponse
    {
        public List<ExtraStayRule> ExtraStayRules { get; set; }
    }

    public class ExtraStayRule
    {
        public string Id { get; set; }

        public string PropertyId { get; set; }

        public string CheckInTime { get; set; }

        public bool OverridesPropertyCheckInTime { get; set; }

        public List<ExtraStayRulePeriod> EarlyCheckInPeriods { get; set; }

        public string CheckOutTime { get; set; }

        public bool OverridesPropertyCheckOutTime { get; set; }

        public List<ExtraStayRulePeriod> LateCheckOutPeriods { get; set; }
    }

    public class ExtraStayRulePeriod
    {
        public string StartTime { get; set; }

        public string EndTime { get; set; }

        public ExtraStayChargeType ChargeType { get; set; }

        public List<ExtraStayChargeRate> Rates { get; set; }

        public ExtraStayPercentOptions PercentOptions { get; set; }

        public bool OccupyQuota { get; set; }
    }

    public class ExtraStayChargeRate
    {
        public string Currency { get; set; }

        public double Rate { get; set; }
    }

    public class ExtraStayPercentOptions
    {
        public double Percentage { get; set; }

        public bool ExtraPlacementEnabled { get; set; }

        public ExtraServiceSurchargeOption ExtraServiceOption { get; set; }
    }

    #endregion

    #endregion

    #endregion

    public class AgentDetails
    {
        public string AgentCode { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public string NewPassword { get; set; }
        public string EmailId { get; set; }
        public string Url { get; set; }
        public string CompanyName { get; set; }
    }

    public class UserIDToken
    {
        public string UserName { get; set; }
        public string AgentCode { get; set; }
        public string Token { get; set; }
    }

    public class UserDetails
    {
        public string AgentId { get; set; }
        public string AgentName { get; set; }
        public string CompanyName { get; set; }
        public string EmailId { get; set; }
        public string AgentCode { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public int ParentId { get; set; }
        public string UserType { get; set; }
        public bool IsParent { get; set; }
        public decimal Percentage { get; set; }
        public decimal ParentMarkup { get; set; }
        public int MapCategoryId { get; set; }
        public string AgentCreditType { get; set; }
        //------sub user
        public bool IsSubuser { get; set; }

        public string SubUserID { get; set; }
        public bool? HasHotelRight { get; set; }
        public bool? HasTourRight { get; set; }
        public bool? HasVisaRight { get; set; }
        public bool? HasHolidaysRight { get; set; }

        public bool? HasTariffRight { get; set; }
        public bool CanVoucher { get; set; }
        public bool CanBook { get; set; }
        public bool CanBookWithinCancellationDeadlIne { get; set; }
        public string CurrencyShortName { get; set; }
        public int SelectCurrencyID { get; set; }
        public int GuestUserId { get; set; }
        public string WhatsappNo { get; set; }
        public string MobileNo { get; set; }
        public string TelePhoneNo { get; set; }
        public int Status { get; set; }
        public string Message { get; set; }
        public string Token { get; set; }
        public string Logo { get; set; }
        public bool IsShowSupplier { get; set; }
        public int CheckRedeem { get; set; }
        public bool IsInternetBanking { get; set; }
        public bool IsHotel { get; set; }
        public bool IsTransfer { get; set; }
        public bool IsTour { get; set; }
        public bool IsPackage { get; set; }
        public bool IsBuildPackage { get; set; }
        public bool IsRestaurant { get; set; }
        public int AutoVoucherDays { get; set; }
        public bool IsSpecialTariff { get; set; }
        public string CreditLimitCurrency { get; set; }
        public string CreditLimitCurrencyName { get; set; }
        public int ApiTimeOut { get; set; }

    }

    public class ResponseStatus
    {
        public int Status { get; set; }
        public string Message { get; set; }
        public string ReferenceNumber { get; set; }
    }

}
