using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XConnect.Supplier.Omnibees
{
    public class Ominibees
    {
        public class Address
        {
            public object AddressLine { get; set; }
            public object CityCode { get; set; }
            public object CityName { get; set; }
            public int CountryCode { get; set; }
            public object CountryName { get; set; }
            public object PostalCode { get; set; }
            public object StateProv { get; set; }
            public object StateProvCode { get; set; }
            public object ZoneCode { get; set; }
            public object ZoneName { get; set; }
        }

        public class GuestCount
        {
            public object Age { get; set; }
            public int AgeQualifyCode { get; set; }
            public int Count { get; set; }
            public object ResGuestRPH { get; set; }
        }

        public class GuestCountsType
        {
            public IList<GuestCount> GuestCounts { get; set; }
        }

        public class RoomStayCandidate
        {
            public object BookingCode { get; set; }
            public GuestCountsType GuestCountsType { get; set; }
            public int Quantity { get; set; }
            public int RPH { get; set; }
            public object RoomID { get; set; }
        }

        public class RoomStayCandidatesType
        {
            public IList<RoomStayCandidate> RoomStayCandidates { get; set; }
        }

        public class StayDateRange
        {
            public object Duration { get; set; }
            public DateTime End { get; set; }
            public DateTime Start { get; set; }
        }

        public class MultimediaObjects
        {
            public bool SendData { get; set; }
        }

        public class TPAExtensions
        {
            public object AmountIncludingMarkup { get; set; }
            public object AmountIsPackageRates { get; set; }
            public object AmountNotIncludingMarkup { get; set; }
            public bool IsForMobile { get; set; }
            public MultimediaObjects MultimediaObjects { get; set; }
            public object OnlyPreferredHotels { get; set; }
        }
        public class HotelRef
        {
            public object BrandName { get; set; }
            public object ChainCode { get; set; }
            public object ChainName { get; set; }
            public int HotelCode { get; set; }
            public object HotelName { get; set; }
        }
        public class Criterion
        {
            public Address Address { get; set; }
            public object Award { get; set; }
            public bool GetPricesPerGuest { get; set; }
            public object HotelAmenity { get; set; }
            public List<HotelRef> HotelRefs { get; set; }
            public object InfoSource { get; set; }
            public object Location { get; set; }
            public object Position { get; set; }
            public object ProfilesType { get; set; }
            public object Radius { get; set; }
            public object RatePlanCandidatesType { get; set; }
            public object RateRanges { get; set; }
            public object RoomAmenity { get; set; }
            public RoomStayCandidatesType RoomStayCandidatesType { get; set; }
            public StayDateRange StayDateRange { get; set; }
            public TPAExtensions TPA_Extensions { get; set; }
        }

        public class HotelSearchCriteria
        {
            public object AvailableOnlyIndicator { get; set; }
            public Criterion Criterion { get; set; }
        }
        public class HotelSearchRQ
        {
            public bool AvailRatesOnly { get; set; }
            public bool BestOnly { get; set; }
            public string EchoToken { get; set; }
            public HotelSearchCriteria HotelSearchCriteria { get; set; }
            public object IsModify { get; set; }
            public object MaxResponses { get; set; }
            public bool OnRequestInd { get; set; }
            public object POS { get; set; }
            public object PageNumber { get; set; }
            public int PrimaryLangID { get; set; }
            public object RequestedCurrency { get; set; }
            public int Target { get; set; }
            public DateTime TimeStamp { get; set; }
            public double Version { get; set; }
        }
    }

    public class HotelSearchResponse
    {
        // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse); 
        public class Success
        {
        }

        public class Position
        {
            public string Latitude { get; set; }
            public string Longitude { get; set; }
        }

        public class Address
        {
            public int CountryCode { get; set; }
            public int StateProvCode { get; set; }
            public int CityCode { get; set; }
            public string AddressLine { get; set; }
            public object CityName { get; set; }
            public object PostalCode { get; set; }
            public object StateProv { get; set; }
            public object CountryName { get; set; }
            public object ZoneCode { get; set; }
            public object ZoneName { get; set; }
        }

        public class Award
        {
            public int Rating { get; set; }
            public object Name { get; set; }
        }
        public class HotelRef2
        {
            public int HotelCode { get; set; }
            public int ChainCode { get; set; }
            public string HotelName { get; set; }
            public object BrandName { get; set; }
            public string ChainName { get; set; }
        }
        public class BasicPropertyInfo
        {
            public int CurrencyCode { get; set; }
            public Position Position { get; set; }
            public Address Address { get; set; }
            public object ContactNumbersType { get; set; }
            public Award Award { get; set; }
            public object HotelAmenities { get; set; }
            public object Policy { get; set; }
            public HotelRef HotelRef { get; set; }
            public object VendorMessagesType { get; set; }
            public string ImageURL { get; set; }
        }
        public class Price
        {
            //public DateTime Start { get; set; }
            public object Start { get; set; }
            public object Duration { get; set; }
            //public DateTime End { get; set; }
            public object End { get; set; }
            public double AmountBeforeTax { get; set; }
            public double AmountAfterTax { get; set; }
            public int CurrencyCode { get; set; }
        }
        public class TPAExtensions
        {
            public string SupplierCode { get; set; }
            public object AmountIncludingMarkup { get; set; }
            public object AmountNotIncludingMarkup { get; set; }
            public object AmountIsPackageRates { get; set; }
            public MultimediaObjects MultimediaObjects { get; set; }
            public bool IsForMobile { get; set; }
            public object OnlyPreferredHotels { get; set; }
            public POS POS { get; set; }
            public object ApprovalInvoiced { get; set; }
            public object Services { get; set; }
            public object GuestsTotalRate { get; set; }
            public object Rates { get; set; }
            public object TotalDiscountValue { get; set; }
            public object IsPreferredHotel { get; set; }
            public object Schedules { get; set; }
            public int SortOrder { get; set; }
        }
        public class HotelStay
        {
            public string Status { get; set; }
            public int RoomStayRPH { get; set; }
            public object Availability { get; set; }
            public BasicPropertyInfo BasicPropertyInfo { get; set; }
            public Price Price { get; set; }
            public TPAExtensions TPA_Extensions { get; set; }
        }
        public class HotelStaysType
        {
            public List<HotelStay> HotelStays { get; set; }
        }
        public class HotelRef
        {
            public int HotelCode { get; set; }
            public object ChainCode { get; set; }
            public object HotelName { get; set; }
            public object BrandName { get; set; }
            public object ChainName { get; set; }
        }
        public class StayDateRange
        {
            public DateTime Start { get; set; }
            public object Duration { get; set; }
            public DateTime End { get; set; }
        }
        public class GuestCount
        {
            public object Age { get; set; }
            public int AgeQualifyCode { get; set; }
            public int Count { get; set; }
            public object ResGuestRPH { get; set; }
        }
        public class GuestCountsType
        {
            public List<GuestCount> GuestCounts { get; set; }
        }
        public class RoomStayCandidate
        {
            public GuestCountsType GuestCountsType { get; set; }
            public object RoomID { get; set; }
            public int Quantity { get; set; }
            public int RPH { get; set; }
            public object BookingCode { get; set; }
        }
        public class RoomStayCandidatesType
        {
            public List<RoomStayCandidate> RoomStayCandidates { get; set; }
        }
        public class MultimediaObjects
        {
            public bool SendData { get; set; }
        }

        public class Criterion
        {
            public object Position { get; set; }
            public object Address { get; set; }
            public List<HotelRef> HotelRefs { get; set; }
            public object Radius { get; set; }
            public object HotelAmenity { get; set; }
            public object RoomAmenity { get; set; }
            public object Award { get; set; }
            public object ProfilesType { get; set; }
            public object RateRanges { get; set; }
            public StayDateRange StayDateRange { get; set; }
            public RoomStayCandidatesType RoomStayCandidatesType { get; set; }
            public object RatePlanCandidatesType { get; set; }
            public object Location { get; set; }
            public bool GetPricesPerGuest { get; set; }
            public TPAExtensions TPA_Extensions { get; set; }
            public object InfoSource { get; set; }
        }

        public class Criteria
        {
            public object AvailableOnlyIndicator { get; set; }
            public Criterion Criterion { get; set; }
        }

        public class RoomDescription
        {
            public int Language { get; set; }
            public string Description { get; set; }
        }

        public class Occupancy
        {
            public object MinOccupancy { get; set; }
            public object MaxOccupancy { get; set; }
            public int AgeQualifyingCode { get; set; }
            public object MinAge { get; set; }
            public object MaxAge { get; set; }
        }

        public class RoomType
        {
            public int RoomID { get; set; }
            public string RoomName { get; set; }
            public RoomDescription RoomDescription { get; set; }
            public object AmenitiesType { get; set; }
            public List<Occupancy> Occupancies { get; set; }
            public int NumberOfUnits { get; set; }
            public object MaxOccupancy { get; set; }
        }

        public class GuaranteeDescription
        {
            public string Name { get; set; }
            public int Language { get; set; }
            public string Description { get; set; }
        }

        public class GuaranteesAccepted
        {
            public int RPH { get; set; }
            public int GuaranteeTypeCode { get; set; }
            public object PaymentCard { get; set; }
            public object GuaranteeID { get; set; }
        }

        public class GuaranteesAcceptedType
        {
            public List<GuaranteesAccepted> GuaranteesAccepted { get; set; }
        }

        public class DeadLine
        {
            public object AbsoluteDeadline { get; set; }
            public int OffsetUnitMultiplier { get; set; }
            public int TimeUnitType { get; set; }
            public int OffsetDropTime { get; set; }
        }

        public class AmountPercent
        {
            public int NmbrOfNights { get; set; }
            public double Amount { get; set; }
            public double Percent { get; set; }
            public int CurrencyCode { get; set; }
        }

        public class Guarantee
        {
            public DateTime? Start { get; set; }
            public object Duration { get; set; }
            public DateTime? End { get; set; }
            public GuaranteeDescription GuaranteeDescription { get; set; }
            public GuaranteesAcceptedType GuaranteesAcceptedType { get; set; }
            public DeadLine DeadLine { get; set; }
            public AmountPercent AmountPercent { get; set; }
            public int GuaranteeCode { get; set; }
        }

        public class PenaltyDescription
        {
            public string Name { get; set; }
            public int Language { get; set; }
            public string Description { get; set; }
        }

        public class CancelPenalty
        {
            public bool NonRefundable { get; set; }
            public object Start { get; set; }
            public object Duration { get; set; }
            public object End { get; set; }
            public DeadLine DeadLine { get; set; }
            public AmountPercent AmountPercent { get; set; }
            public PenaltyDescription PenaltyDescription { get; set; }
            public object AmountRules { get; set; }
        }

        public class RatePlanInclusionDesciption
        {
            public string Name { get; set; }
            public int Language { get; set; }
            public string Description { get; set; }
        }

        public class RatePlanInclusion
        {
            public RatePlanInclusionDesciption RatePlanInclusionDesciption { get; set; }
            public object Start { get; set; }
            public object Duration { get; set; }
            public object End { get; set; }
            public int ID { get; set; }
        }

        public class MealsIncluded
        {
            public bool Breakfast { get; set; }
            public bool Lunch { get; set; }
            public bool Dinner { get; set; }
            public bool MealPlanIndicator { get; set; }
            public int MealPlanCode { get; set; }
            public int ID { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
        }

        public class TaxPolicy
        {
            public string Name { get; set; }
            public string Description { get; set; }
            public bool IsPerNight { get; set; }
            public bool IsPerStay { get; set; }
            public bool IsPerPerson { get; set; }
            public bool IsPerRoom { get; set; }
            public double Value { get; set; }
            public bool IsValuePercentage { get; set; }
            public int CurrencyCode { get; set; }
            public int ID { get; set; }
        }

        public class RatePlanDescription
        {
            public int Language { get; set; }
            public string Description { get; set; }
        }

        public class DetailDescription
        {
            public string Name { get; set; }
            public int Language { get; set; }
            public string Description { get; set; }
        }

        public class AdditionalDetail
        {
            public DetailDescription DetailDescription { get; set; }
        }

        public class AdditionalDetailsType
        {
            public List<AdditionalDetail> AdditionalDetails { get; set; }
        }

        public class AcceptedPayment
        {
            public int RPH { get; set; }
            public int GuaranteeTypeCode { get; set; }
            public object PaymentCard { get; set; }
            public object GuaranteeID { get; set; }
        }

        public class PaymentPolicies
        {
            public List<AcceptedPayment> AcceptedPayments { get; set; }
        }

        public class Commission
        {
            public double Percent { get; set; }
        }

        public class Source
        {
            public string ISOCountry { get; set; }
        }

        public class POS
        {
            public List<Source> Sources { get; set; }
        }

        public class RatePlan
        {
            public object EffectiveDate { get; set; }
            public object ExpireDate { get; set; }
            public string RatePlanName { get; set; }
            public List<Guarantee> Guarantees { get; set; }
            public List<CancelPenalty> CancelPenalties { get; set; }
            public List<RatePlanInclusion> RatePlanInclusions { get; set; }
            public MealsIncluded MealsIncluded { get; set; }
            public int RatePlanID { get; set; }
            public List<TaxPolicy> TaxPolicies { get; set; }
            public RatePlanDescription RatePlanDescription { get; set; }
            public AdditionalDetailsType AdditionalDetailsType { get; set; }
            public object Offers { get; set; }
            public PaymentPolicies PaymentPolicies { get; set; }
            public Commission Commission { get; set; }
            public int CurrencyCode { get; set; }
            //public int RatePlanTypeCode { get; set; }
            public string RatePlanTypeCode { get; set; }
            public int? SortOrder { get; set; }
            public bool PriceViewableInd { get; set; }
            public TPAExtensions TPA_Extensions { get; set; }
        }
        public class Total
        {
            public double AmountBeforeTax { get; set; }
            public double AmountAfterTax { get; set; }
            public int CurrencyCode { get; set; }
            public bool AmountIncludingMarkup { get; set; }
            public bool AmountIsPackage { get; set; }
            public int ChargeType { get; set; }
            public object TPA_Extensions { get; set; }
            public object Taxes { get; set; }
        }
        public class Rate
        {
            public DateTime EffectiveDate { get; set; }
            public object MinLOS { get; set; }
            public object MaxLOS { get; set; }
            public Total Total { get; set; }
            public object AgeQualifyingCode { get; set; }
            public object MinAge { get; set; }
            public object MaxAge { get; set; }
            public object MinGuestApplicable { get; set; }
            public object MaxGuestApplicable { get; set; }
            public object Status { get; set; }
            public int NumberOfUnits { get; set; }
            public object MinAdvancedBookingOffset { get; set; }
            public object StayThrough { get; set; }
            public object Duration { get; set; }
            public object FPLOS { get; set; }
        }
        public class RatesType
        {
            public List<Rate> Rates { get; set; }
        }

        public class Tax
        {
            public string Name { get; set; }
            public string Description { get; set; }
            public double Amount { get; set; }
            public int CurrencyCode { get; set; }
            public int ID { get; set; }
        }

        public class ServiceRPH
        {
            public int RPH { get; set; }
            public bool IsPerRoom { get; set; }
        }

        public class Availability
        {
            public string AvailabilityStatus { get; set; }
            public object WarningRPH { get; set; }
        }

        public class RoomRate
        {
            public int RoomStayCandidateRPH { get; set; }
            public DateTime EffectiveDate { get; set; }
            public DateTime ExpireDate { get; set; }
            public int RatePlanID { get; set; }
            public int RoomID { get; set; }
            public RatesType RatesType { get; set; }
            public Total Total { get; set; }
            public object AdvanceBookingRestriction { get; set; }
            public List<ServiceRPH> ServiceRPHs { get; set; }
            public object Discount { get; set; }
            public object PromotionCode { get; set; }
            public object GroupCode { get; set; }
            public object TPA_Extensions { get; set; }
            public List<Availability> Availability { get; set; }
        }

        public class RoomStay
        {
            public int RPH { get; set; }
            public List<RoomType> RoomTypes { get; set; }
            public List<RatePlan> RatePlans { get; set; }
            public List<RoomRate> RoomRates { get; set; }
            public Total Total { get; set; }
            public object GuestCountsType { get; set; }
            public BasicPropertyInfo BasicPropertyInfo { get; set; }
            public object Reference { get; set; }
            public object CommentsType { get; set; }
            public object SpecialRequestsType { get; set; }
            public object IndexNumber { get; set; }
            public object RoomStayStatus { get; set; }
            public TPAExtensions TPA_Extensions { get; set; }
            public object Guarantees { get; set; }
            public object RoomStayCandidateRPH { get; set; }
            public object AvailabilityStatus { get; set; }
            public object WarningRPH { get; set; }
            public int RoomStayLanguage { get; set; }
        }

        public class RoomStaysType
        {
            public List<RoomStay> RoomStays { get; set; }
            public bool MoreIndicator { get; set; }
        }

        public class ServiceDescription
        {
            public string Name { get; set; }
            public int Language { get; set; }
            public string Description { get; set; }
            public string ImageURL { get; set; }
        }

        public class Service
        {
            public int ServicePricingType { get; set; }
            public int ServiceRPH { get; set; }
            public object RatePlanCode { get; set; }
            public int Quantity { get; set; }
            public bool RequestIndicator { get; set; }
            public int ID { get; set; }
            public ServiceDescription ServiceDescription { get; set; }
            public List<Price> Price { get; set; }
            public int? ServiceCategoryCode { get; set; }
            public TPAExtensions TPA_Extensions { get; set; }
        }
        public class HotelSearchRS
        {
            public object MaxResponses { get; set; }
            public object PageNumber { get; set; }
            public string EchoToken { get; set; }
            public DateTime TimeStamp { get; set; }
            public int Target { get; set; }
            public double Version { get; set; }
            public int PrimaryLangID { get; set; }
            public object POS { get; set; }
            public object WarningsType { get; set; }
            public Success Success { get; set; }
            public object ErrorsType { get; set; }
            public HotelStaysType HotelStaysType { get; set; }
            public Criteria Criteria { get; set; }
            public RoomStaysType RoomStaysType { get; set; }
            public List<Service> Services { get; set; }
            public object TPA_Extensions { get; set; }
        }
    }

    public class OmnibeesBookRQ
    {
        // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse); 
        public class TPAExtensions
        {
            public string SupplierCode { get; set; }
            public object ApprovalInvoiced { get; set; }
            public object GuestsTotalRate { get; set; }
            public object IsPreferredHotel { get; set; }
            public List<Rate> Rates { get; set; }
            public object Services { get; set; }
            public object TotalDiscountValue { get; set; }
        }

        public class Comment
        {
            public string Description { get; set; }
            public object Language { get; set; }
            public object Name { get; set; }
        }

        public class CommentsType
        {
            public List<Comment> Comments { get; set; }
        }

        //public class GuaranteesAccepted
        //{
        //    public object GuaranteeID { get; set; }
        //    public int GuaranteeTypeCode { get; set; }
        //    public object PaymentCard { get; set; }
        //    public int RPH { get; set; }
        //}

        public class PaymentCard
        {
            public int CardCode { get; set; }
            public string CardHolderName { get; set; }
            public string CardNumber { get; set; }
            public DateTime EffectiveDate { get; set; }
            public DateTime ExpireDate { get; set; }
            public string SeriesCode { get; set; }
        }

        public class GuaranteesAccepted
        {
            public object GuaranteeID { get; set; }
            public int GuaranteeTypeCode { get; set; }
            public PaymentCard PaymentCard { get; set; }
            public int RPH { get; set; }
        }

        public class GuaranteesAcceptedType
        {
            public List<GuaranteesAccepted> GuaranteesAccepted { get; set; }
        }

        public class Guarantee
        {
            public object AmountPercent { get; set; }
            public object DeadLine { get; set; }
            public object Duration { get; set; }
            public object End { get; set; }
            public object GuaranteeCode { get; set; }
            public object GuaranteeDescription { get; set; }
            public GuaranteesAcceptedType GuaranteesAcceptedType { get; set; }
            public object Start { get; set; }
        }

        public class Total
        {
            public double AmountAfterTax { get; set; }
            public double AmountBeforeTax { get; set; }
            public bool AmountIncludingMarkup { get; set; }
            public bool AmountIsPackage { get; set; }
            public int ChargeType { get; set; }
            public int CurrencyCode { get; set; }
            public object TPA_Extensions { get; set; }
            public object Taxes { get; set; }
        }

        public class ResGlobalInfo
        {
            public CommentsType CommentsType { get; set; }
            public Guarantee Guarantee { get; set; }
            public object HotelReservationsIDsType { get; set; }
            public object TimeSpan { get; set; }
            public Total Total { get; set; }
        }

        public class Address
        {
            public object AddressLine { get; set; }
            public object CityCode { get; set; }
            public object CityName { get; set; }
            public int CountryCode { get; set; }
            public object CountryName { get; set; }
            public object PostalCode { get; set; }
            public object StateProv { get; set; }
            public object StateProvCode { get; set; }
            public object ZoneCode { get; set; }
            public object ZoneName { get; set; }
        }

        public class PersonName
        {
            public object GivenName { get; set; }
            public object MiddleName { get; set; }
            public int NamePrefix { get; set; }
            public object Surname { get; set; }
        }

        public class Customer
        {
            public Address Address { get; set; }
            public object BirthDate { get; set; }
            public object Document { get; set; }
            public object Email { get; set; }
            public int Gender { get; set; }
            public PersonName PersonName { get; set; }
            public object Telephones { get; set; }
        }

        public class Profile
        {
            public Customer Customer { get; set; }
        }

        public class ProfileInfo
        {
            public Profile Profile { get; set; }
            public object UniqueID { get; set; }
        }

        public class Profiles
        {
            public List<ProfileInfo> ProfileInfos { get; set; }
        }

        public class ResGuest
        {
            public int Age { get; set; }
            public int AgeQualifyingCode { get; set; }
            public bool PrimaryIndicator { get; set; }
            public Profiles Profiles { get; set; }
            public int ResGuestRPH { get; set; }
            public object SpecialRequests { get; set; }
        }

        public class ResGuestsType
        {
            public List<ResGuest> ResGuests { get; set; }
        }

        public class ResStatus
        {
            public object PMS_ResStatusType { get; set; }
            public int TransactionActionType { get; set; }
        }

        public class HotelRef
        {
            public object BrandName { get; set; }
            public object ChainCode { get; set; }
            public object ChainName { get; set; }
            public int HotelCode { get; set; }
            public object HotelName { get; set; }
        }

        public class BasicPropertyInfo
        {
            public object Address { get; set; }
            public object Award { get; set; }
            public object ContactNumbersType { get; set; }
            public object CurrencyCode { get; set; }
            public object HotelAmenities { get; set; }
            public HotelRef HotelRef { get; set; }
            public object ImageURL { get; set; }
            public object Policy { get; set; }
            public object Position { get; set; }
            public object VendorMessagesType { get; set; }
        }

        public class GuestCount
        {
            public object Age { get; set; }
            public int AgeQualifyCode { get; set; }
            public int Count { get; set; }
            public List<int> ResGuestRPH { get; set; }
        }

        public class GuestCountsType
        {
            public List<GuestCount> GuestCounts { get; set; }
        }

        public class AmountPercent
        {
            public double Amount { get; set; }
            public int CurrencyCode { get; set; }
            public int NmbrOfNights { get; set; }
            public double Percent { get; set; }
        }

        public class DeadLine
        {
            public object AbsoluteDeadline { get; set; }
            public int OffsetDropTime { get; set; }
            public int OffsetUnitMultiplier { get; set; }
            public int TimeUnitType { get; set; }
        }

        public class PenaltyDescription
        {
            public string Description { get; set; }
            public int Language { get; set; }
            public string Name { get; set; }
        }

        public class CancelPenalty
        {
            public AmountPercent AmountPercent { get; set; }
            public object AmountRules { get; set; }
            public DeadLine DeadLine { get; set; }
            public object Duration { get; set; }
            public object End { get; set; }
            public bool NonRefundable { get; set; }
            public PenaltyDescription PenaltyDescription { get; set; }
            public object Start { get; set; }
        }

        public class Commission
        {
            public double Percent { get; set; }
        }

        public class GuaranteeDescription
        {
            public string Description { get; set; }
            public int Language { get; set; }
            public string Name { get; set; }
        }

        public class Guarantee2
        {
            public object AmountPercent { get; set; }
            public object DeadLine { get; set; }
            public object Duration { get; set; }
            public object End { get; set; }
            public int GuaranteeCode { get; set; }
            public GuaranteeDescription GuaranteeDescription { get; set; }
            public object GuaranteesAcceptedType { get; set; }
            public object Start { get; set; }
        }

        public class MealsIncluded
        {
            public bool Breakfast { get; set; }
            public string Description { get; set; }
            public bool Dinner { get; set; }
            public int ID { get; set; }
            public bool Lunch { get; set; }
            public int MealPlanCode { get; set; }
            public bool MealPlanIndicator { get; set; }
            public string Name { get; set; }
        }

        public class AcceptedPayment
        {
            public object GuaranteeID { get; set; }
            public int GuaranteeTypeCode { get; set; }
            public object PaymentCard { get; set; }
            public int RPH { get; set; }
        }

        public class PaymentPolicies
        {
            public List<AcceptedPayment> AcceptedPayments { get; set; }
        }

        public class RatePlanDescription
        {
            public string Description { get; set; }
            public int Language { get; set; }
        }

        public class TaxPolicy
        {
            public int CurrencyCode { get; set; }
            public string Description { get; set; }
            public int ID { get; set; }
            public bool IsPerNight { get; set; }
            public bool IsPerPerson { get; set; }
            public bool IsPerRoom { get; set; }
            public bool IsPerStay { get; set; }
            public bool IsValuePercentage { get; set; }
            public string Name { get; set; }
            public double Value { get; set; }
        }

        public class RatePlan
        {
            public object AdditionalDetailsType { get; set; }
            public List<CancelPenalty> CancelPenalties { get; set; }
            public Commission Commission { get; set; }
            public int CurrencyCode { get; set; }
            public object EffectiveDate { get; set; }
            public object ExpireDate { get; set; }
            public List<Guarantee> Guarantees { get; set; }
            public MealsIncluded MealsIncluded { get; set; }
            public object Offers { get; set; }
            public PaymentPolicies PaymentPolicies { get; set; }
            public bool PriceViewableInd { get; set; }
            public RatePlanDescription RatePlanDescription { get; set; }
            public int RatePlanID { get; set; }
            public object RatePlanInclusions { get; set; }
            public string RatePlanName { get; set; }
            public int RatePlanTypeCode { get; set; }
            public object SortOrder { get; set; }
            public object TPA_Extensions { get; set; }
            public List<TaxPolicy> TaxPolicies { get; set; }
        }

        public class Availability
        {
            public string AvailabilityStatus { get; set; }
            public object WarningRPH { get; set; }
        }
        public class Rate
        {
            public object AgeQualifyingCode { get; set; }
            public object Duration { get; set; }
            public DateTime EffectiveDate { get; set; }
            public object FPLOS { get; set; }
            public object MaxAge { get; set; }
            public object MaxGuestApplicable { get; set; }
            public object MaxLOS { get; set; }
            public object MinAdvancedBookingOffset { get; set; }
            public object MinAge { get; set; }
            public object MinGuestApplicable { get; set; }
            public object MinLOS { get; set; }
            public object NumberOfUnits { get; set; }
            public object Status { get; set; }
            public object StayThrough { get; set; }
            public Total Total { get; set; }
        }

        public class RatesType
        {
            public List<Rate> Rates { get; set; }
        }
        public class ServiceRPH
        {
            public bool IsPerRoom { get; set; }
            public int RPH { get; set; }
        }
        public class RoomRate
        {
            public object AdvanceBookingRestriction { get; set; }
            public List<Availability> Availability { get; set; }
            public object Discount { get; set; }
            public DateTime EffectiveDate { get; set; }
            public DateTime ExpireDate { get; set; }
            public object GroupCode { get; set; }
            public object PromotionCode { get; set; }
            public int RatePlanID { get; set; }
            public RatesType RatesType { get; set; }
            public int RoomID { get; set; }
            public int RoomStayCandidateRPH { get; set; }
            public List<ServiceRPH> ServiceRPHs { get; set; }
            public TPAExtensions TPA_Extensions { get; set; }
            public Total Total { get; set; }
        }
        public class Occupancy
        {
            public int AgeQualifyingCode { get; set; }
            public object MaxAge { get; set; }
            public object MaxOccupancy { get; set; }
            public object MinAge { get; set; }
            public object MinOccupancy { get; set; }
        }
        public class RoomDescription
        {
            public string Description { get; set; }
            public int Language { get; set; }
        }
        public class RoomType
        {
            public object AmenitiesType { get; set; }
            public object MaxOccupancy { get; set; }
            public int NumberOfUnits { get; set; }
            public List<Occupancy> Occupancies { get; set; }
            public RoomDescription RoomDescription { get; set; }
            public int RoomID { get; set; }
            public string RoomName { get; set; }
        }
        public class SpecialRequest
        {
            public string Description { get; set; }
            public object Language { get; set; }
            public object Name { get; set; }
        }
        public class SpecialRequestsType
        {
            public List<SpecialRequest> SpecialRequests { get; set; }
        }
        public class RoomStay
        {
            public object AvailabilityStatus { get; set; }
            public BasicPropertyInfo BasicPropertyInfo { get; set; }
            public CommentsType CommentsType { get; set; }
            public object Guarantees { get; set; }
            public GuestCountsType GuestCountsType { get; set; }
            public object IndexNumber { get; set; }
            public int RPH { get; set; }
            public List<RatePlan> RatePlans { get; set; }
            public object Reference { get; set; }
            public List<RoomRate> RoomRates { get; set; }
            public int RoomStayCandidateRPH { get; set; }
            public object RoomStayStatus { get; set; }
            public List<RoomType> RoomTypes { get; set; }
            public SpecialRequestsType SpecialRequestsType { get; set; }
            public object TPA_Extensions { get; set; }
            public Total Total { get; set; }
            public int RoomStayLanguage { get; set; }
            public object WarningRPH { get; set; }
        }
        public class RoomStaysType
        {
            public bool MoreIndicator { get; set; }
            public List<RoomStay> RoomStays { get; set; }
        }
        public class Source
        {
            public string ISOCountry { get; set; }
        }

        public class POS
        {
            public List<Source> Sources { get; set; }
        }
        public class HotelReservation
        {
            public TPAExtensions TPA_Extensions { get; set; }
            public DateTime CreateDateTime { get; set; }
            public DateTime LastModifyDateTime { get; set; }
            public POS POS { get; set; }
            public ResGlobalInfo ResGlobalInfo { get; set; }
            public ResGuestsType ResGuestsType { get; set; }
            public ResStatus ResStatus { get; set; }
            public RoomStaysType RoomStaysType { get; set; }
            public object Services { get; set; }
            public object UniqueID { get; set; }
        }
        public class HotelReservationsType
        {
            public List<HotelReservation> HotelReservations { get; set; }
        }
        public class Request
        {
            public string EchoToken { get; set; }
            public HotelReservationsType HotelReservationsType { get; set; }
            public object PrimaryLangID { get; set; }
            public object ResStatus { get; set; }
            public object RetransmissionIndicator { get; set; }
            public int Target { get; set; }
            public DateTime TimeStamp { get; set; }
            public string TransactionIdentifier { get; set; }
            public double Version { get; set; }
        }
    }
    public class OmnibeesBookRS
    {
        public class Success
        {
        }

        public class TPAExtensions
        {
            public string SupplierCode { get; set; }
        }

        public class ResStatus
        {
            public int TransactionActionType { get; set; }
            public object PMS_ResStatusType { get; set; }
        }

        public class UniqueID
        {
            public string ID { get; set; }
            public int Type { get; set; }
            public object Reason { get; set; }
        }

        public class RoomDescription
        {
            public int Language { get; set; }
            public string Description { get; set; }
        }

        public class Occupancy
        {
            public int MinOccupancy { get; set; }
            public int MaxOccupancy { get; set; }
            public int AgeQualifyingCode { get; set; }
            public object MinAge { get; set; }
            public object MaxAge { get; set; }
        }

        public class RoomType
        {
            public int RoomID { get; set; }
            public string RoomName { get; set; }
            public RoomDescription RoomDescription { get; set; }
            public object AmenitiesType { get; set; }
            public IList<Occupancy> Occupancies { get; set; }
            public int NumberOfUnits { get; set; }
            public int MaxOccupancy { get; set; }
        }

        public class GuaranteeDescription
        {
            public string Name { get; set; }
            public int Language { get; set; }
            public string Description { get; set; }
        }


        public class DeadLine
        {
            public object AbsoluteDeadline { get; set; }
            public int OffsetUnitMultiplier { get; set; }
            public int TimeUnitType { get; set; }
            public int OffsetDropTime { get; set; }
        }

        public class AmountPercent
        {
            public int NmbrOfNights { get; set; }
            public double Amount { get; set; }
            public double Percent { get; set; }
            public int CurrencyCode { get; set; }
        }

        public class PenaltyDescription
        {
            public string Name { get; set; }
            public int Language { get; set; }
            public string Description { get; set; }
        }

        public class AmountRule
        {
            public DateTime Start { get; set; }
            public DateTime End { get; set; }
            public double Amount { get; set; }
            public int CurrencyCode { get; set; }
        }

        public class CancelPenalty
        {
            public bool NonRefundable { get; set; }
            public object Start { get; set; }
            public object Duration { get; set; }
            public object End { get; set; }
            public DeadLine DeadLine { get; set; }
            public AmountPercent AmountPercent { get; set; }
            public PenaltyDescription PenaltyDescription { get; set; }
            public IList<AmountRule> AmountRules { get; set; }
        }

        public class MealsIncluded
        {
            public bool Breakfast { get; set; }
            public bool Lunch { get; set; }
            public bool Dinner { get; set; }
            public bool MealPlanIndicator { get; set; }
            public int MealPlanCode { get; set; }
            public int ID { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
        }

        public class TaxPolicy
        {
            public string Name { get; set; }
            public string Description { get; set; }
            public bool IsPerNight { get; set; }
            public bool IsPerStay { get; set; }
            public bool IsPerPerson { get; set; }
            public bool IsPerRoom { get; set; }
            public double Value { get; set; }
            public bool IsValuePercentage { get; set; }
            public int CurrencyCode { get; set; }
            public int ID { get; set; }
        }

        public class RatePlanDescription
        {
            public int Language { get; set; }
            public string Description { get; set; }
        }

        public class AcceptedPayment
        {
            public int RPH { get; set; }
            public int GuaranteeTypeCode { get; set; }
            public object PaymentCard { get; set; }
            public object GuaranteeID { get; set; }
        }

        public class PaymentPolicies
        {
            public IList<AcceptedPayment> AcceptedPayments { get; set; }
        }

        public class Commission
        {
            public double Percent { get; set; }
        }

        public class RatePlan
        {
            public object EffectiveDate { get; set; }
            public object ExpireDate { get; set; }
            public string RatePlanName { get; set; }
            public IList<Guarantee> Guarantees { get; set; }
            public IList<CancelPenalty> CancelPenalties { get; set; }
            public object RatePlanInclusions { get; set; }
            public MealsIncluded MealsIncluded { get; set; }
            public int RatePlanID { get; set; }
            public IList<TaxPolicy> TaxPolicies { get; set; }
            public RatePlanDescription RatePlanDescription { get; set; }
            public object AdditionalDetailsType { get; set; }
            public object Offers { get; set; }
            public PaymentPolicies PaymentPolicies { get; set; }
            public Commission Commission { get; set; }
            public int CurrencyCode { get; set; }
            public int RatePlanTypeCode { get; set; }
            public object SortOrder { get; set; }
            public bool PriceViewableInd { get; set; }
            public object TPA_Extensions { get; set; }
        }

        public class Total
        {
            public double AmountBeforeTax { get; set; }
            public double AmountAfterTax { get; set; }
            public int CurrencyCode { get; set; }
            public bool AmountIncludingMarkup { get; set; }
            public bool AmountIsPackage { get; set; }
            public int ChargeType { get; set; }
            public object TPA_Extensions { get; set; }
            public object Taxes { get; set; }
        }

        public class Rate
        {
            public DateTime EffectiveDate { get; set; }
            public object MinLOS { get; set; }
            public object MaxLOS { get; set; }
            public Total Total { get; set; }
            public object AgeQualifyingCode { get; set; }
            public object MinAge { get; set; }
            public object MaxAge { get; set; }
            public object MinGuestApplicable { get; set; }
            public object MaxGuestApplicable { get; set; }
            public object Status { get; set; }
            public int NumberOfUnits { get; set; }
            public object MinAdvancedBookingOffset { get; set; }
            public object StayThrough { get; set; }
            public object Duration { get; set; }
            public object FPLOS { get; set; }
        }

        public class RatesType
        {
            public IList<Rate> Rates { get; set; }
        }

        public class ServiceRPH
        {
            public int RPH { get; set; }
            public bool IsPerRoom { get; set; }
        }

        public class Availability
        {
            public string AvailabilityStatus { get; set; }
            public object WarningRPH { get; set; }
        }

        public class RoomRate
        {
            public int RoomStayCandidateRPH { get; set; }
            public DateTime EffectiveDate { get; set; }
            public DateTime ExpireDate { get; set; }
            public int RatePlanID { get; set; }
            public int RoomID { get; set; }
            public RatesType RatesType { get; set; }
            public Total Total { get; set; }
            public object AdvanceBookingRestriction { get; set; }
            public IList<ServiceRPH> ServiceRPHs { get; set; }
            public object Discount { get; set; }
            public object PromotionCode { get; set; }
            public object GroupCode { get; set; }
            public TPAExtensions TPA_Extensions { get; set; }
            public IList<Availability> Availability { get; set; }
        }

        public class GuestCount
        {
            public object Age { get; set; }
            public int AgeQualifyCode { get; set; }
            public int Count { get; set; }
            public IList<int> ResGuestRPH { get; set; }
        }

        public class GuestCountsType
        {
            public IList<GuestCount> GuestCounts { get; set; }
        }

        public class HotelRef
        {
            public int HotelCode { get; set; }
            public object ChainCode { get; set; }
            public object HotelName { get; set; }
            public object BrandName { get; set; }
            public object ChainName { get; set; }
        }

        public class BasicPropertyInfo
        {
            public object CurrencyCode { get; set; }
            public object Position { get; set; }
            public object Address { get; set; }
            public object ContactNumbersType { get; set; }
            public object Award { get; set; }
            public object HotelAmenities { get; set; }
            public object Policy { get; set; }
            public HotelRef HotelRef { get; set; }
            public object VendorMessagesType { get; set; }
            public object ImageURL { get; set; }
        }

        public class Comment
        {
            public object Name { get; set; }
            public object Language { get; set; }
            public string Description { get; set; }
        }

        public class CommentsType
        {
            public IList<Comment> Comments { get; set; }
        }

        public class RoomStayStatus
        {
            public object TransactionActionType { get; set; }
            public int PMS_ResStatusType { get; set; }
        }

        public class RoomStay
        {
            public int RPH { get; set; }
            public IList<RoomType> RoomTypes { get; set; }
            public IList<RatePlan> RatePlans { get; set; }
            public IList<RoomRate> RoomRates { get; set; }
            public Total Total { get; set; }
            public GuestCountsType GuestCountsType { get; set; }
            public BasicPropertyInfo BasicPropertyInfo { get; set; }
            public object Reference { get; set; }
            public CommentsType CommentsType { get; set; }
            public object SpecialRequestsType { get; set; }
            public string IndexNumber { get; set; }
            public RoomStayStatus RoomStayStatus { get; set; }
            public object TPA_Extensions { get; set; }
            public object Guarantees { get; set; }
            public int RoomStayCandidateRPH { get; set; }
            public object AvailabilityStatus { get; set; }
            public object WarningRPH { get; set; }
            public int RoomStayLanguage { get; set; }
        }

        public class RoomStaysType
        {
            public IList<RoomStay> RoomStays { get; set; }
            public bool MoreIndicator { get; set; }
        }

        public class PersonName
        {
            public int NamePrefix { get; set; }
            public string GivenName { get; set; }
            public object MiddleName { get; set; }
            public string Surname { get; set; }
        }

        public class Customer
        {
            public int Gender { get; set; }
            public object BirthDate { get; set; }
            public object Email { get; set; }
            public object Telephones { get; set; }
            public PersonName PersonName { get; set; }
            public object Address { get; set; }
            public object Document { get; set; }
        }

        public class Profile
        {
            public Customer Customer { get; set; }
        }

        public class ProfileInfo
        {
            public object UniqueID { get; set; }
            public Profile Profile { get; set; }
        }

        public class Profiles
        {
            public IList<ProfileInfo> ProfileInfos { get; set; }
        }

        public class ResGuest
        {
            public int ResGuestRPH { get; set; }
            public int AgeQualifyingCode { get; set; }
            public int Age { get; set; }
            public Profiles Profiles { get; set; }
            public bool PrimaryIndicator { get; set; }
            public object SpecialRequests { get; set; }
        }

        public class ResGuestsType
        {
            public IList<ResGuest> ResGuests { get; set; }
        }

        public class GuaranteesAccepted
        {
            public int RPH { get; set; }
            public int GuaranteeTypeCode { get; set; }
            public object PaymentCard { get; set; }
            public object GuaranteeID { get; set; }
        }

        public class GuaranteesAcceptedType
        {
            public IList<GuaranteesAccepted> GuaranteesAccepted { get; set; }
        }

        public class Guarantee
        {
            public object Start { get; set; }
            public object Duration { get; set; }
            public object End { get; set; }
            public object GuaranteeDescription { get; set; }
            public GuaranteesAcceptedType GuaranteesAcceptedType { get; set; }
            public object DeadLine { get; set; }
            public object AmountPercent { get; set; }
            public object GuaranteeCode { get; set; }
        }


        public class ResGlobalInfo
        {
            public object TimeSpan { get; set; }
            public Total Total { get; set; }
            public object HotelReservationsIDsType { get; set; }
            public Guarantee Guarantee { get; set; }
            public CommentsType CommentsType { get; set; }
        }

        public class HotelReservation
        {
            //public TPAExtensions TPA_Extensions { get; set; }
            //public DateTime CreateDateTime { get; set; }
            //public DateTime LastModifyDateTime { get; set; }
            //public ResStatus ResStatus { get; set; }
            //public object POS { get; set; }
            public UniqueID UniqueID { get; set; }
            //public RoomStaysType RoomStaysType { get; set; }
            //public ResGuestsType ResGuestsType { get; set; }
            //public ResGlobalInfo ResGlobalInfo { get; set; }
            //public object Services { get; set; }
        }

        public class HotelReservationsType
        {
            public IList<HotelReservation> HotelReservations { get; set; }
        }

        public class OmnibeesBookingRS
        {
            //public string EchoToken { get; set; }
            //public DateTime TimeStamp { get; set; }
            //public int Target { get; set; }
            //public double Version { get; set; }
            //public int PrimaryLangID { get; set; }
            //public Success Success { get; set; }
            public object WarningsType { get; set; }
            //public object ErrorsType { get; set; }
            //public object POS { get; set; }
            public HotelReservationsType HotelReservationsType { get; set; }
            //public object ResResponseType { get; set; }
            //public object RetransmissionIndicator { get; set; }
            public object TransactionIdentifier { get; set; }
        }


    }


    public class OmnibeesCancelRQ
    {
        // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse); 
        public class Segment
        {
            public string ItinSegNbr { get; set; }
            public int Status { get; set; }
        }
        public class UniqueID
        {
            public string ID { get; set; }
            public object Reason { get; set; }
            public int Type { get; set; }
        }
        public class HotelRef
        {
            public object BrandName { get; set; }
            public object ChainCode { get; set; }
            public object ChainName { get; set; }
            public int HotelCode { get; set; }
            public object HotelName { get; set; }
        }
        public class ReservationTimeSpan
        {
            public object Duration { get; set; }
            public DateTime End { get; set; }
            public DateTime Start { get; set; }
        }
        public class Verification
        {
            public object Email { get; set; }
            public HotelRef HotelRef { get; set; }
            public ReservationTimeSpan ReservationTimeSpan { get; set; }
        }
        public class CancelRQ
        {
            public int CancelType { get; set; }
            public string EchoToken { get; set; }
            public object POS { get; set; }
            public int PrimaryLangID { get; set; }
            public object RetransmissionIndicator { get; set; }
            public List<Segment> Segment { get; set; }
            public int Target { get; set; }
            public DateTime TimeStamp { get; set; }
            public string TransactionIdentifier { get; set; }
            public List<UniqueID> UniqueID { get; set; }
            public Verification Verification { get; set; }
            public double Version { get; set; }
        }
    }

    public class OmnibeesBookingCancelResponse
    {
        public class Success
        {
        }

        public class CancelRule
        {
            public double Amount { get; set; }
            public int CurrencyCode { get; set; }
        }

        public class CancelInfoRS
        {
            public List<CancelRule> CancelRules { get; set; }
        }

        public class Segment
        {
            public string ItinSegNbr { get; set; }
            public int Status { get; set; }
        }

        public class CancelRS
        {
            public string EchoToken { get; set; }
            public DateTime TimeStamp { get; set; }
            public int Target { get; set; }
            public double Version { get; set; }
            public int PrimaryLangID { get; set; }
            public object UniqueID { get; set; }
            public object Comment { get; set; }
            public Success Success { get; set; }
            public object WarningsType { get; set; }
            public object ErrorsType { get; set; }
            public int Status { get; set; }
            public CancelInfoRS CancelInfoRS { get; set; }
            public List<Segment> Segment { get; set; }
            public object RetransmissionIndicator { get; set; }
            public object TransactionIdentifier { get; set; }
        }
    }

}
