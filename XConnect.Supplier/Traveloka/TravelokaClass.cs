using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XConnect.Supplier.Traveloka
{
    public class TravelokaClass
    {
    }
    #region Traveloka Class
    public class TravelokaToken
    {
        public class TravelokaTokenRequest
        {
            public string client_id { get; set; }
            public string client_secret { get; set; }
        }
        public class TravelokaTokenResponse
        {
            public string refresh_token_expires_in { get; set; }
            public string originating_environment { get; set; }
            public string api_product_list { get; set; }
            public List<string> api_product_list_json { get; set; }
            public string organization_name { get; set; }
            public string developer_email { get; set; }
            public string token_type { get; set; }
            public string issued_at { get; set; }
            public string client_id { get; set; }
            public string access_token { get; set; }
            public string application_name { get; set; }
            public string scope { get; set; }
            //public string expires_in { get; set; }
            public int expires_in { get; set; }
            public string refresh_count { get; set; }
            public string status { get; set; }
        }
    }
    public class TravelokaHotelListRequest
    {
        public class Room
        {
            public string numOfAdults { get; set; }
        }

        public class RoomGroup
        {
            public List<Room> rooms { get; set; }
        }

        public class Metadata
        {
            public string customerSessionId { get; set; }
            public string customerIpAddress { get; set; }
            public string customerUserAgent { get; set; }
            public string locale { get; set; }
            public string currencyCode { get; set; }
            public string clientInterface { get; set; }
        }

        public class TravelokaListRequest
        {
            public string checkInDate { get; set; }
            public string checkOutDate { get; set; }
            public List<string> rateTypes { get; set; }
            public RoomGroup roomGroup { get; set; }
            public List<string> options { get; set; }
            public List<string> hotelIds { get; set; }
            public string sortType { get; set; }
            public List<bool> stars { get; set; }
            public string limit { get; set; }
            public string offset { get; set; }
            public Metadata metadata { get; set; }
        }
    }
    public class TravelokaListResponse
    {
        public class Address
        {
            public List<string> lines { get; set; }
            public string city { get; set; }
            public string countryName { get; set; }
            public string postalCode { get; set; }
        }

        public class LocalAddress
        {
            public List<string> lines { get; set; }
            public string city { get; set; }
            public string countryName { get; set; }
            public string postalCode { get; set; }
        }

        public class GeoLocation
        {
            public string lat { get; set; }
            public string lon { get; set; }
        }

        public class PropertySummary
        {
            public string name { get; set; }
            public object formerName { get; set; }
            public Address address { get; set; }
            public LocalAddress localAddress { get; set; }
            public string starRating { get; set; }
            public string reviewScore { get; set; }
            public string accommodationType { get; set; }
            public GeoLocation geoLocation { get; set; }
        }

        public class Entry
        {
            public string imageType { get; set; }
            public string url { get; set; }
        }

        public class PropertyImage
        {
            public List<Entry> entries { get; set; }
            public bool isMain { get; set; }
        }

        public class Surcharge
        {
            public string type { get; set; }
            public string amount { get; set; }
        }

        public class NightlyRate
        {
            public string date { get; set; }
            public string baseRate { get; set; }
            public bool promo { get; set; }
            public string nightRate { get; set; }
        }

        public class ChargeableRateInfo
        {
            public string netPrice { get; set; }
        }

        public class ChargeableRate
        {
            public string currencyCode { get; set; }
            public string averageBaseRate { get; set; }
            public string averageRate { get; set; }
            public string nightlyRateTotal { get; set; }
            public string surchargeTotal { get; set; }
            public string total { get; set; }
            public List<Surcharge> surcharges { get; set; }
            public List<NightlyRate> nightlyRates { get; set; }
            public ChargeableRateInfo chargeableRateInfo { get; set; }
        }

        public class Surcharge2
        {
            public string type { get; set; }
            public string amount { get; set; }
        }

        public class NightlyRate2
        {
            public string date { get; set; }
            public string baseRate { get; set; }
            public bool promo { get; set; }
            public string nightRate { get; set; }
        }

        public class ChargeableRateInfo2
        {
            public string netPrice { get; set; }
        }

        public class ConvertedChargeableRate
        {
            public string currencyCode { get; set; }
            public string averageBaseRate { get; set; }
            public string averageRate { get; set; }
            public string nightlyRateTotal { get; set; }
            public string surchargeTotal { get; set; }
            public string total { get; set; }
            public List<Surcharge2> surcharges { get; set; }
            public List<NightlyRate2> nightlyRates { get; set; }
            public ChargeableRateInfo2 chargeableRateInfo { get; set; }
        }

        public class CheapestRoom
        {
            public ChargeableRate chargeableRate { get; set; }
            public ConvertedChargeableRate convertedChargeableRate { get; set; }
            public string numOfRooms { get; set; }
        }

        public class Property
        {
            public string propertyId { get; set; }
            public PropertySummary propertySummary { get; set; }
            public List<PropertyImage> propertyImages { get; set; }
            public CheapestRoom cheapestRoom { get; set; }
            public string cheapestRoomName { get; set; }
            public string rateKey { get; set; }
        }

        public class RootObject
        {
            public string totalProperties { get; set; }
            public string maximalOffset { get; set; }
            public List<Property> properties { get; set; }
        }
    }
    public class TravelokaBulkRoomRequest
    {
        public class Metadata
        {
            public string customerIpAddress { get; set; }
            public string customerUserAgent { get; set; }
            public string locale { get; set; }
            public string currencyCode { get; set; }
            public string clientInterface { get; set; }
            public string customerSessionId { get; set; }
        }

        public class Room
        {
            public string numOfAdults { get; set; }
        }

        public class RoomGroup
        {
            public List<Room> rooms { get; set; }
        }

        public class Root
        {
            public Metadata metadata { get; set; }
            public List<string> propertyIds { get; set; }
            public string checkInDate { get; set; }
            public string checkOutDate { get; set; }
            public List<string> rateTypes { get; set; }
            public RoomGroup roomGroup { get; set; }
            public List<string> options { get; set; }
        }
    }
    public class TravelokaRoomRequest
    {
        public class Metadata
        {
            public string customerIpAddress { get; set; }
            public string customerUserAgent { get; set; }
            public string locale { get; set; }
            public string currencyCode { get; set; }
            public string clientInterface { get; set; }
            public string customerSessionId { get; set; }
        }

        public class Room
        {
            public string numOfAdults { get; set; }
        }

        public class RoomGroup
        {
            public List<Room> rooms { get; set; }
        }

        public class RootObject
        {
            public Metadata metadata { get; set; }
            public string propertyId { get; set; }
            public string checkInDate { get; set; }
            public string checkOutDate { get; set; }
            public List<string> rateTypes { get; set; }
            public RoomGroup roomGroup { get; set; }
            public List<string> options { get; set; }
        }
    }
    public class TravelokaRoomResponse
    {
        public class Entry
        {
            public string imageType { get; set; }
            public string url { get; set; }
        }

        public class Image
        {
            public List<Entry> entries { get; set; }
        }

        public class CancellationPolicy
        {
            public string currencyCode { get; set; }
            public string timeZoneDisplay { get; set; }
            public string cancelDateTime { get; set; }
            public string fees { get; set; }
            public bool isAfter { get; set; }
            public bool after { get; set; }
        }

        public class Surcharge
        {
            public string type { get; set; }
            public string amount { get; set; }
        }

        public class NightlyRate
        {
            public string date { get; set; }
            public string baseRate { get; set; }
            public bool promo { get; set; }
            public string nightRate { get; set; }
        }

        public class ChargeableRateInfo
        {
            public string netPrice { get; set; }
        }

        public class ChargeableRate
        {
            public string currencyCode { get; set; }
            public string averageBaseRate { get; set; }
            public string averageRate { get; set; }
            public string nightlyRateTotal { get; set; }
            public string surchargeTotal { get; set; }
            public string total { get; set; }
            public List<Surcharge> surcharges { get; set; }
            public List<NightlyRate> nightlyRates { get; set; }
            public ChargeableRateInfo chargeableRateInfo { get; set; }
        }

        public class Surcharge2
        {
            public string type { get; set; }
            public string amount { get; set; }
        }

        public class NightlyRate2
        {
            public string date { get; set; }
            public string baseRate { get; set; }
            public bool promo { get; set; }
            public string nightRate { get; set; }
        }

        public class ChargeableRateInfo2
        {
            public string netPrice { get; set; }
        }

        public class ConvertedChargeableRate
        {
            public string currencyCode { get; set; }
            public string averageBaseRate { get; set; }
            public string averageRate { get; set; }
            public string nightlyRateTotal { get; set; }
            public string surchargeTotal { get; set; }
            public string total { get; set; }
            public List<Surcharge2> surcharges { get; set; }
            public List<NightlyRate2> nightlyRates { get; set; }
            public ChargeableRateInfo2 chargeableRateInfo { get; set; }
        }

        public class Rates
        {
            public string rateKey { get; set; }
            public string numOfAdults { get; set; }
            public string rateType { get; set; }
            public List<Promos> promos { get; set; }
            public string currentAllotments { get; set; }
            public List<CancellationPolicy> cancellationPolicies { get; set; }
            public string cancellationPolicyDisplay { get; set; }
            public ChargeableRate chargeableRate { get; set; }
            public ConvertedChargeableRate convertedChargeableRate { get; set; }
        }
        public class Promos
        {
            public string id { get; set; } // change int to string 16042025
            public string name { get; set; }
            public string description { get; set; }
        }

        public class Arrangement
        {
            public string bedType { get; set; }
            public string total { get; set; }
        }

        public class BedroomLayout
        {
            public List<Arrangement> arrangements { get; set; }
            public string arrangementType { get; set; }
        }

        public class Bedroom
        {
            public List<BedroomLayout> bedroomLayouts { get; set; }
        }

        public class RoomSize
        {
            public string size { get; set; }
            public string unit { get; set; }
        }

        public class Room
        {
            public string id { get; set; }
            public string name { get; set; }
            public object roomTypeName { get; set; }
            public string description { get; set; }
            public List<Image> images { get; set; }
            public object amenities { get; set; }
            public string quotedOccupancy { get; set; }
            public string maxOccupancy { get; set; }
            public object smokingPreference { get; set; }
            public Rates rates { get; set; }
            public bool breakfastIncluded { get; set; }
            public bool wifiIncluded { get; set; }
            public string bedType { get; set; }
            public string bedroomSummary { get; set; }
            public List<Bedroom> bedrooms { get; set; }
            public RoomSize roomSize { get; set; }
        }

        public class RootObject
        {
            public string propertyId { get; set; }
            public string status { get; set; }
            public object propertySummary { get; set; }
            public propertyDetail propertyDetail { get; set; }
            public object propertyImages { get; set; }
            public object propertyAmenities { get; set; }
            public List<Room> rooms { get; set; }
        }
        public class propertyDetail
        {
            public string checkInTime { get; set; }
            public string checkOutTime { get; set; }
            public string hotelPolicy { get; set; }
            public string importanceNotice { get; set; }
            public string checkInInstruction { get; set; }
            public List<string> availableRateTypes { get; set; }
        }
    }
    public class TravelokaPreBookRequest
    {
        public class Room
        {
            public string numOfAdults { get; set; }
        }

        public class RoomGroup
        {
            public string roomId { get; set; }
            public List<Room> rooms { get; set; }
            public string rateKey { get; set; }
        }

        public class Metadata
        {
            public string customerSessionId { get; set; }
            public string customerIpAddress { get; set; }
            public string customerUserAgent { get; set; }
            public string locale { get; set; }
            public string currencyCode { get; set; }
            public string clientInterface { get; set; }
        }

        public class RootObject
        {
            public string propertyId { get; set; }
            public string checkInDate { get; set; }
            public string checkOutDate { get; set; }
            public List<string> rateTypes { get; set; }
            public RoomGroup roomGroup { get; set; }
            public Metadata metadata { get; set; }
        }
    }
    public class TravelokaPreBookResponse
    {
        public class Entry
        {
            public string imageType { get; set; }
            public string url { get; set; }
        }

        public class Image
        {
            public List<Entry> entries { get; set; }
        }

        public class Amenity
        {
            public string id { get; set; }
            public string name { get; set; }
        }

        public class CancellationPolicy
        {
            public string currencyCode { get; set; }
            public string timeZoneDisplay { get; set; }
            public string cancelDateTime { get; set; }
            public string fees { get; set; }
            public bool isAfter { get; set; }
            public bool after { get; set; }
        }

        public class Surcharge
        {
            public string type { get; set; }
            public string amount { get; set; }
        }

        public class NightlyRate
        {
            public string date { get; set; }
            public string baseRate { get; set; }
            public bool promo { get; set; }
            public string nightRate { get; set; }
        }

        public class ChargeableRateInfo
        {
            public string netPrice { get; set; }
        }

        public class ChargeableRate
        {
            public string currencyCode { get; set; }
            public string averageBaseRate { get; set; }
            public string averageRate { get; set; }
            public string nightlyRateTotal { get; set; }
            public string surchargeTotal { get; set; }
            public string total { get; set; }
            public List<Surcharge> surcharges { get; set; }
            public List<NightlyRate> nightlyRates { get; set; }
            public ChargeableRateInfo chargeableRateInfo { get; set; }
        }

        public class Surcharge2
        {
            public string type { get; set; }
            public string amount { get; set; }
        }

        public class NightlyRate2
        {
            public string date { get; set; }
            public string baseRate { get; set; }
            public bool promo { get; set; }
            public string nightRate { get; set; }
        }

        public class ChargeableRateInfo2
        {
            public string netPrice { get; set; }
        }

        public class ConvertedChargeableRate
        {
            public string currencyCode { get; set; }
            public string averageBaseRate { get; set; }
            public string averageRate { get; set; }
            public string nightlyRateTotal { get; set; }
            public string surchargeTotal { get; set; }
            public string total { get; set; }
            public List<Surcharge2> surcharges { get; set; }
            public List<NightlyRate2> nightlyRates { get; set; }
            public ChargeableRateInfo2 chargeableRateInfo { get; set; }
        }

        public class Rates
        {
            public string rateKey { get; set; }
            public string numOfAdults { get; set; }
            public string rateType { get; set; }
            public object promos { get; set; }
            public string currentAllotments { get; set; }
            public List<CancellationPolicy> cancellationPolicies { get; set; }
            public string cancellationPolicyDisplay { get; set; }
            public ChargeableRate chargeableRate { get; set; }
            public ConvertedChargeableRate convertedChargeableRate { get; set; }
        }

        public class Arrangement
        {
            public string bedType { get; set; }
            public string total { get; set; }
        }

        public class BedroomLayout
        {
            public List<Arrangement> arrangements { get; set; }
            public string arrangementType { get; set; }
        }

        public class Bedroom
        {
            public List<BedroomLayout> bedroomLayouts { get; set; }
        }

        public class RoomSize
        {
            public string size { get; set; }
            public string unit { get; set; }
        }

        public class Room
        {
            public string id { get; set; }
            public string name { get; set; }
            public object roomTypeName { get; set; }
            public string description { get; set; }
            public List<Image> images { get; set; }
            public List<Amenity> amenities { get; set; }
            public string quotedOccupancy { get; set; }
            public string maxOccupancy { get; set; }
            public object smokingPreference { get; set; }
            public Rates rates { get; set; }
            public bool breakfastIncluded { get; set; }
            public bool wifiIncluded { get; set; }
            public string bedType { get; set; }
            public string bedroomSummary { get; set; }
            public List<Bedroom> bedrooms { get; set; }
            public RoomSize roomSize { get; set; }
        }

        public class RootObject
        {
            public string status { get; set; }
            public List<Room> rooms { get; set; }
        }
    }
    public class TravelokaBookRequest
    {
        public class Metadata
        {
            public string customerSessionId { get; set; }
            public string customerIpAddress { get; set; }
            public string customerUserAgent { get; set; }
            public string locale { get; set; }
            public string currencyCode { get; set; }
            public string clientInterface { get; set; }
        }

        public class Guest
        {
            public string title { get; set; }
            public string firstName { get; set; }
            public string lastName { get; set; }
        }

        public class Room
        {
            public string roomId { get; set; }
            public string rateKey { get; set; }
            public string numOfAdult { get; set; }
            public List<Guest> guests { get; set; }
        }

        public class ExpectedTotalChargeableRate
        {
            public string currencyCode { get; set; }
            public string amount { get; set; }
        }

        public class ChargeableRateInfo
        {
            public string netPrice { get; set; }
        }

        public class UserContact
        {
            public List<string> phones { get; set; }
            public string email { get; set; }
            public string title { get; set; }
            public string firstName { get; set; }
            public string lastName { get; set; }
        }

        public class AffiliateContact
        {
            public List<string> affiliatePhoneNumber { get; set; }
            public string affiliateEmail { get; set; }
        }

        public class RootObject
        {
            public Metadata metadata { get; set; }
            public string affiliateConfirmationIds { get; set; }
            public string propertyId { get; set; }
            public string checkInDate { get; set; }
            public string checkOutDate { get; set; }
            public List<string> rateTypes { get; set; }
            public List<Room> rooms { get; set; }
            public ExpectedTotalChargeableRate expectedTotalChargeableRate { get; set; }
            public ChargeableRateInfo chargeableRateInfo { get; set; }
            public UserContact userContact { get; set; }
            public AffiliateContact affiliateContact { get; set; }
            public string userPaymentMethod { get; set; }
            public string specialRequest { get; set; }
            public string loginId { get; set; }
            public string loginType { get; set; }
        }
    }
    public class TravelokaBookResponse
    {
        public class Address
        {
            public List<string> lines { get; set; }
            public string city { get; set; }
            public string countryName { get; set; }
            public string postalCode { get; set; }
        }

        public class LocalAddress
        {
            public List<string> lines { get; set; }
            public string city { get; set; }
            public string countryName { get; set; }
            public string postalCode { get; set; }
        }

        public class GeoLocation
        {
            public string lat { get; set; }
            public string lon { get; set; }
        }

        public class PropertySummary
        {
            public string name { get; set; }
            public object formerName { get; set; }
            public Address address { get; set; }
            public LocalAddress localAddress { get; set; }
            public string starRating { get; set; }
            public string reviewScore { get; set; }
            public string accommodationType { get; set; }
            public GeoLocation geoLocation { get; set; }
        }

        public class Guest
        {
            public string title { get; set; }
            public string firstName { get; set; }
            public string lastName { get; set; }
        }

        public class Room
        {
            public string id { get; set; }
            public string name { get; set; }
            public object roomTypeName { get; set; }
            public string numOfAdults { get; set; }
            public List<Guest> guests { get; set; }
            public bool wifiIncluded { get; set; }
            public bool breakfastIncluded { get; set; }
            public object bedType { get; set; }
        }

        public class TotalChargeableRate
        {
            public string currencyCode { get; set; }
            public string amount { get; set; }
        }

        public class ChargeableRateInfo
        {
            public string netPrice { get; set; }
        }

        public class ConvertedTotalChargeableRate
        {
            public string currencyCode { get; set; }
            public string amount { get; set; }
        }

        public class SurchargeTotal
        {
            public string currencyCode { get; set; }
            public string amount { get; set; }
        }

        public class ConvertedSurchargeTotal
        {
            public string currencyCode { get; set; }
            public string amount { get; set; }
        }

        public class RootObject
        {
            public string status { get; set; }
            public string bookingId { get; set; }
            public string itineraryId { get; set; }
            public string issuerName { get; set; }
            public string propertyId { get; set; }
            public PropertySummary propertySummary { get; set; }
            public string checkInDate { get; set; }
            public string checkOutDate { get; set; }
            public List<Room> rooms { get; set; }
            public TotalChargeableRate totalChargeableRate { get; set; }
            public ChargeableRateInfo chargeableRateInfo { get; set; }
            public List<object> additionalRates { get; set; }
            public ConvertedTotalChargeableRate convertedTotalChargeableRate { get; set; }
            public SurchargeTotal surchargeTotal { get; set; }
            public ConvertedSurchargeTotal convertedSurchargeTotal { get; set; }
        }
    }
    public class TravelokaBookingCancelRQ
    {
        public class RootObject
        {
            public string itineraryId { get; set; }
            public string email { get; set; }
            public string affiliateConfirmationId { get; set; }
            public string reason { get; set; }
        }
    }
    public class TravelokaBookingCancelRS
    {
        public class RootObject
        {
            public string cancellationId { get; set; }
            public string refundedAmount { get; set; }
            public string currency { get; set; }
            public string cancelBookingStatus { get; set; }
        }
    }
    public class TravelokaViewItineraryRQ
    {
        public class Metadata
        {
            public string locale { get; set; }
            public string customerSessionId { get; set; }
            public string customerIpAddress { get; set; }
            public string currencyCode { get; set; }
        }

        //public class BookingTimeRange
        //{
        //    public string startTime { get; set; }
        //    public string endTime { get; set; }
        //    public string limit { get; set; }
        //    public string offset { get; set; }
        //}

        public class RootObject
        {
            public Metadata metadata { get; set; }
            public string itineraryId { get; set; }
            public string affiliateConfirmationId { get; set; }
            //  public BookingTimeRange bookingTimeRange { get; set; }
            public string email { get; set; }
        }
    }
    public class TravelokaViewItinenaryRS
    {
        public class UserContact
        {
            public string title { get; set; }
            public string firstName { get; set; }
            public string lastName { get; set; }
            public IList<string> phones { get; set; }
            public string email { get; set; }
        }

        public class Guest
        {
            public string title { get; set; }
            public string firstName { get; set; }
            public string lastName { get; set; }
        }

        public class Room
        {
            public string roomId { get; set; }
            public string name { get; set; }
            public object roomTypeName { get; set; }
            public string rateType { get; set; }
            public string numOfAdults { get; set; }
            public IList<Guest> guests { get; set; }
            public bool wifiIncluded { get; set; }
            public bool breakfastIncluded { get; set; }
            public string roomStatus { get; set; }
        }

        public class CancellationPolicy
        {
            public string currencyCode { get; set; }
            public string timeZoneDisplay { get; set; }
            public string cancelDateTime { get; set; }
            public string fees { get; set; }
            public bool isAfter { get; set; }
            public bool after { get; set; }
        }

        public class TotalChargeableRate
        {
            public string currencyCode { get; set; }
            public string amount { get; set; }
        }

        public class ChargeableRateInfo
        {
            public string netPrice { get; set; }
        }

        public class ConvertedTotalChargeableRate
        {
            public string currencyCode { get; set; }
            public string amount { get; set; }
        }

        public class SurchargeTotal
        {
            public string currencyCode { get; set; }
            public string amount { get; set; }
        }

        public class ConvertedSurchargeTotal
        {
            public string currencyCode { get; set; }
            public string amount { get; set; }
        }

        public class Itinerary
        {
            public string itineraryId { get; set; }
            public string bookingId { get; set; }
            public string affiliateConfirmationId { get; set; }
            public string issuerName { get; set; }
            public string cid { get; set; }
            public string propertyId { get; set; }
            public string creationTime { get; set; }
            public string checkInDate { get; set; }
            public string checkOutDate { get; set; }
            public UserContact userContact { get; set; }
            public string currencyCode { get; set; }
            public object checkInInstruction { get; set; }
            public object importantNotice { get; set; }
            public IList<Room> rooms { get; set; }
            public IList<CancellationPolicy> cancellationPolicies { get; set; }
            public string cancellationPolicyDisplay { get; set; }
            public TotalChargeableRate totalChargeableRate { get; set; }
            public ChargeableRateInfo chargeableRateInfo { get; set; }
            public ConvertedTotalChargeableRate convertedTotalChargeableRate { get; set; }
            public object specialRequest { get; set; }
            public string status { get; set; }
            public SurchargeTotal surchargeTotal { get; set; }
            public ConvertedSurchargeTotal convertedSurchargeTotal { get; set; }
            public IList<object> additionalRate { get; set; }
        }

        public class RootObject
        {
            public string totalItineraries { get; set; }
            public IList<Itinerary> itineraries { get; set; }
        }
    }
    public class TravelokaErrorRS
    {
        public class RootObject
        {
            public string category { get; set; }
            public string handling { get; set; }
            public string errorCode { get; set; }
            public string message { get; set; }
            public string verboseMessage { get; set; }
            public string serverTimestamp { get; set; }
            public object data { get; set; }
        }
    }
    #endregion
}
