using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XConnect.Supplier.PkFare
{
    public class PkFareClass
    {
    }
    #region "PkFare class"
    public class PkfsearchRQ
    {
        public class Hotels
        {
            public List<int> hotel { get; set; }
        }

        public class Occupancy
        {
            public int rooms { get; set; }
            public int adults { get; set; }
            public int children { get; set; }
            public List<Paxis> paxes { get; set; }
        }

        public class Paxis
        {
            public string type { get; set; }
            public int age { get; set; }
        }

        public class Root
        {
            public bool aifUse { get; set; }
            public bool dailyRate { get; set; }
            public Hotels hotels { get; set; }
            public List<Occupancy> occupancies { get; set; }
            public string nationality { get; set; }
            public Stay stay { get; set; }
        }

        public class Stay
        {
            public bool allowOnlyShift { get; set; }
            public string checkIn { get; set; }
            public string checkOut { get; set; }
        }

    }
    public class PkfsearchRS
    {
        public class CancellationPolicy
        {
            public double amount { get; set; }
            public object hotelAmount { get; set; }
            public object hotelCurrency { get; set; }
            public DateTime? from { get; set; }
            public object percent { get; set; }
            public object numberOfNights { get; set; }
        }

        public class DailyRate
        {
            public int offset { get; set; }
            public double dailyNet { get; set; }
            public object dailySellingRate { get; set; }
        }

        public class Hotel
        {
            public string checkOut { get; set; }
            public string checkIn { get; set; }
            public int code { get; set; }
            public string name { get; set; }
            public object description { get; set; }
            public object exclusiveDeal { get; set; }
            public object categoryCode { get; set; }
            public object categoryName { get; set; }
            public object destinationCode { get; set; }
            public object destinationName { get; set; }
            public object zoneCode { get; set; }
            public object zoneName { get; set; }
            public string latitude { get; set; }
            public string longitude { get; set; }
            public double minRate { get; set; }
            public double maxRate { get; set; }
            public object totalSellingRate { get; set; }
            public object totalNet { get; set; }
            public object pendingAmount { get; set; }
            public string currency { get; set; }
            public object supplier { get; set; }
            public object clientComments { get; set; }
            public object cancellationAmount { get; set; }
            public object upselling { get; set; }
            public object keyword { get; set; }
            public object review { get; set; }
            public List<Room> room { get; set; }
            public object creditCard { get; set; }
            public bool paymentDataRequired { get; set; }
        }

        public class Hotels
        {
            public string checkIn { get; set; }
            public string checkOut { get; set; }
            public List<Hotel> hotel { get; set; }
            public int total { get; set; }
        }

        public class Rate
        {
            public string rateKey { get; set; }
            public object rateClass { get; set; }
            public object rateType { get; set; }
            public double net { get; set; }
            public object discount { get; set; }
            public object discountPCT { get; set; }
            public object sellingRate { get; set; }
            public object hotelSellingRate { get; set; }
            public object hotelCurrency { get; set; }
            public bool hotelMandatory { get; set; }
            public int allotment { get; set; }
            public object commission { get; set; }
            public object commissionVAT { get; set; }
            public object commissionPCT { get; set; }
            public object rateCommentsId { get; set; }
            public object rateComments { get; set; }
            public string paymentType { get; set; }
            public bool packaging { get; set; }
            public string boardCode { get; set; }
            public object boardName { get; set; }
            public object rateBreakDown { get; set; }
            public object rooms { get; set; }
            public object adults { get; set; }
            public object children { get; set; }
            public object childrenAges { get; set; }
            public object rateup { get; set; }
            public object brand { get; set; }
            public bool resident { get; set; }
            public List<CancellationPolicy> cancellationPolicy { get; set; }
            public object taxes { get; set; }
            public object promotion { get; set; }
            public object offer { get; set; }
            public object shiftRate { get; set; }
            public List<DailyRate> dailyRate { get; set; }
            public List<TaxesAndFee> taxesAndFees { get; set; }
            public List<string> rateRemarks { get; set; }
        }
        public class TaxesAndFee
        {
            public string description { get; set; }
            public bool include { get; set; }
            public double amount { get; set; }
            public string currency { get; set; }
        }

        public class Room
        {
            public object status { get; set; }
            public object id { get; set; }
            public string code { get; set; }
            public string name { get; set; }
            public object supplierReference { get; set; }
            public object pax { get; set; }
            public List<Rate> rate { get; set; }
        }

        public class Root
        {
            public object error { get; set; }
            public Hotels hotels { get; set; }
        }
    }
    public class PkfprbookRQ
    {
        public class Room
        {
            public string rateKey { get; set; }
            public object paxes { get; set; }
        }

        public class Root
        {
            public bool upselling { get; set; }
            public object language { get; set; }
            public List<Room> room { get; set; }
        }
    }
    public class PkfprbookRS
    {
        public class CancellationPolicy
        {
            public double amount { get; set; }
            public object hotelAmount { get; set; }
            public object hotelCurrency { get; set; }
            public DateTime? from { get; set; }
            public object percent { get; set; }
            public object numberOfNights { get; set; }
        }

        public class DailyRate
        {
            public int offset { get; set; }
            public double dailyNet { get; set; }
            public object dailySellingRate { get; set; }
        }

        public class Hotels
        {
            public string checkOut { get; set; }
            public string checkIn { get; set; }
            public int code { get; set; }
            public string name { get; set; }
            public object description { get; set; }
            public object exclusiveDeal { get; set; }
            public object categoryCode { get; set; }
            public object categoryName { get; set; }
            public object destinationCode { get; set; }
            public object destinationName { get; set; }
            public object zoneCode { get; set; }
            public object zoneName { get; set; }
            public string latitude { get; set; }
            public string longitude { get; set; }
            public object minRate { get; set; }
            public object maxRate { get; set; }
            public object totalSellingRate { get; set; }
            public object totalNet { get; set; }
            public object pendingAmount { get; set; }
            public string currency { get; set; }
            public object supplier { get; set; }
            public object clientComments { get; set; }
            public object cancellationAmount { get; set; }
            public object upselling { get; set; }
            public object keyword { get; set; }
            public object review { get; set; }
            public List<Room> room { get; set; }
            public object creditCard { get; set; }
            public bool paymentDataRequired { get; set; }
        }

        public class Rate
        {
            public string rateKey { get; set; }
            public object rateClass { get; set; }
            public object rateType { get; set; }
            public double net { get; set; }
            public object discount { get; set; }
            public object discountPCT { get; set; }
            public object sellingRate { get; set; }
            public object hotelSellingRate { get; set; }
            public object hotelCurrency { get; set; }
            public bool hotelMandatory { get; set; }
            public int allotment { get; set; }
            public object commission { get; set; }
            public object commissionVAT { get; set; }
            public object commissionPCT { get; set; }
            public object rateCommentsId { get; set; }
            public object rateComments { get; set; }
            public string paymentType { get; set; }
            public bool packaging { get; set; }
            public string boardCode { get; set; }
            public object boardName { get; set; }
            public object rateBreakDown { get; set; }
            public object rooms { get; set; }
            public object adults { get; set; }
            public object children { get; set; }
            public object childrenAges { get; set; }
            public object rateup { get; set; }
            public object brand { get; set; }
            public bool resident { get; set; }
            public List<CancellationPolicy> cancellationPolicy { get; set; }
            public object taxes { get; set; }
            public object promotion { get; set; }
            public object offer { get; set; }
            public object shiftRate { get; set; }
            public List<DailyRate> dailyRate { get; set; }
        }

        public class Room
        {
            public object status { get; set; }
            public object id { get; set; }
            public string code { get; set; }
            public string name { get; set; }
            public object supplierReference { get; set; }
            public object pax { get; set; }
            public List<Rate> rate { get; set; }
        }

        public class Root
        {
            public object error { get; set; }
            public Hotels hotels { get; set; }
        }
    }
    public class PkfBookRQ
    {
        public class Holder
        {
            public string name { get; set; }
            public string surname { get; set; }
        }

        public class Paxis
        {
            public int roomId { get; set; }
            public string type { get; set; }
            public string name { get; set; }
            public string surname { get; set; }
        }

        public class Room
        {
            public string rateKey { get; set; }
            public List<Paxis> paxes { get; set; }
        }

        public class Root
        {
            public Holder holder { get; set; }
            public object paymentData { get; set; }
            public string clientReference { get; set; }
            public string remark { get; set; }
            public object language { get; set; }
            public List<Room> room { get; set; }
        }
    }
    public class PkfBookRS
    {
        public class Booking
        {
            public string reference { get; set; }
            public object cancellationReference { get; set; }
            public string clientReference { get; set; }
            public string creationDate { get; set; }
            public string status { get; set; }
            public ModificationPolicies modificationPolicies { get; set; }
            public object agCommision { get; set; }
            public object commisionVAT { get; set; }
            public object creationUser { get; set; }
            public Holder holder { get; set; }
            public string remark { get; set; }
            public object invoiceCompany { get; set; }
            public object totalSellingRate { get; set; }
            public double totalNet { get; set; }
            public object pendingAmount { get; set; }
            public string currency { get; set; }
            public Hotel hotel { get; set; }
        }

        public class Holder
        {
            public string name { get; set; }
            public string surname { get; set; }
        }

        public class Hotel
        {
            public string checkOut { get; set; }
            public string checkIn { get; set; }
            public int code { get; set; }
            public object name { get; set; }
            public object description { get; set; }
            public object exclusiveDeal { get; set; }
            public object categoryCode { get; set; }
            public object categoryName { get; set; }
            public object destinationCode { get; set; }
            public object destinationName { get; set; }
            public object zoneCode { get; set; }
            public object zoneName { get; set; }
            public object latitude { get; set; }
            public object longitude { get; set; }
            public object minRate { get; set; }
            public object maxRate { get; set; }
            public object totalSellingRate { get; set; }
            public double totalNet { get; set; }
            public object pendingAmount { get; set; }
            public string currency { get; set; }
            public object supplier { get; set; }
            public object clientComments { get; set; }
            public object cancellationAmount { get; set; }
            public object upselling { get; set; }
            public object keyword { get; set; }
            public object review { get; set; }
            public List<Room> room { get; set; }
            public object creditCard { get; set; }
            public bool paymentDataRequired { get; set; }
        }

        public class ModificationPolicies
        {
            public bool cancellation { get; set; }
            public bool modification { get; set; }
        }

        public class Pax
        {
            public int roomId { get; set; }
            public string type { get; set; }
            public object age { get; set; }
            public string name { get; set; }
            public string surname { get; set; }
        }

        public class Room
        {
            public object code { get; set; }
            public object name { get; set; }
            public object supplierReference { get; set; }
            public List<Pax> pax { get; set; }
            public object rate { get; set; }
        }

        public class Root
        {
            public object error { get; set; }
            public Booking booking { get; set; }
        }
    }
    public class PkfCancelRS
    {
        public class Booking
        {
            public string reference { get; set; }
            public object confirmNum { get; set; }
            public object cancellationReference { get; set; }
            public string clientReference { get; set; }
            public string creationDate { get; set; }
            public string status { get; set; }
            public ModificationPolicies modificationPolicies { get; set; }
            public object agCommision { get; set; }
            public object commisionVAT { get; set; }
            public object creationUser { get; set; }
            public Holder holder { get; set; }
            public string remark { get; set; }
            public object invoiceCompany { get; set; }
            public object totalSellingRate { get; set; }
            public double totalNet { get; set; }
            public object pendingAmount { get; set; }
            public string currency { get; set; }
            public Hotel hotel { get; set; }
        }

        public class Holder
        {
            public object name { get; set; }
            public object surname { get; set; }
        }

        public class Hotel
        {
            public string checkOut { get; set; }
            public string checkIn { get; set; }
            public int code { get; set; }
            public string name { get; set; }
            public object description { get; set; }
            public object exclusiveDeal { get; set; }
            public object categoryCode { get; set; }
            public object categoryName { get; set; }
            public object destinationCode { get; set; }
            public object destinationName { get; set; }
            public object zoneCode { get; set; }
            public object zoneName { get; set; }
            public object latitude { get; set; }
            public object longitude { get; set; }
            public object minRate { get; set; }
            public object maxRate { get; set; }
            public object totalSellingRate { get; set; }
            public double totalNet { get; set; }
            public object pendingAmount { get; set; }
            public string currency { get; set; }
            public object supplier { get; set; }
            public object clientComments { get; set; }
            public object cancellationAmount { get; set; }
            public object upselling { get; set; }
            public object keyword { get; set; }
            public object review { get; set; }
            public List<Room> room { get; set; }
            public object creditCard { get; set; }
            public bool paymentDataRequired { get; set; }
        }

        public class ModificationPolicies
        {
            public bool cancellation { get; set; }
            public bool modification { get; set; }
        }

        public class Pax
        {
            public int roomId { get; set; }
            public string type { get; set; }
            public object age { get; set; }
            public string name { get; set; }
            public string surname { get; set; }
        }

        public class Room
        {
            public object status { get; set; }
            public object id { get; set; }
            public object code { get; set; }
            public object name { get; set; }
            public object supplierReference { get; set; }
            public List<Pax> pax { get; set; }
            public object rate { get; set; }
        }

        public class Root
        {
            public object error { get; set; }
            public Booking booking { get; set; }
        }
    }
    public class PkfInfoRes
    {
        public class AccommodationType
        {
            public object code { get; set; }
        }

        public class Address
        {
            public object content { get; set; }
            public object languageCode { get; set; }
        }

        public class BedList
        {
            public object roomId { get; set; }
            public object bedId { get; set; }
            public object bedType { get; set; }
            public object bedNum { get; set; }
            public object size { get; set; }
            public object description { get; set; }
            public object languageCode { get; set; }
        }

        public class Chain
        {
            public object code { get; set; }
        }

        public class Checkin
        {
            public object twentyFourHour { get; set; }
            public object beginTime { get; set; }
            public object endTime { get; set; }
            public object instructions { get; set; }
            public object specialInstructions { get; set; }
            public object minAge { get; set; }
        }

        public class Checkout
        {
            public object time { get; set; }
        }

        public class City
        {
            public object content { get; set; }
            public object languageCode { get; set; }
        }

        public class Coordinates
        {
            public object longitude { get; set; }
            public object latitude { get; set; }
            public object radius { get; set; }
            public object unit { get; set; }
            public object secondaryLatitude { get; set; }
            public object secondaryLongitude { get; set; }
            public object positionType { get; set; }
        }

        public class Description
        {
            public object content { get; set; }
            public object languageCode { get; set; }
        }

        public class Facility
        {
            public object facilityCode { get; set; }
            public object facilityName { get; set; }
            public object appendText { get; set; }
        }

        public class Fees
        {
            public object mandatory { get; set; }
            public object optional { get; set; }
        }

        public class Hotel
        {
            public AccommodationType accommodationType { get; set; }
            public Address address { get; set; }
            public Chain chain { get; set; }
            public City city { get; set; }
            public object code { get; set; }
            public object languageCode { get; set; }
            public Coordinates coordinates { get; set; }
            public object countryCode { get; set; }
            public List<Facility> facilities { get; set; }
            public object lastUpdate { get; set; }
            public Name name { get; set; }
            public List<Phone> phones { get; set; }
            public object postalCode { get; set; }
            public List<Room> rooms { get; set; }
            public List<Image> images { get; set; }
            public object star { get; set; }
            public object brandCode { get; set; }
            public object cityRegionId { get; set; }
            public object description { get; set; }
            public Checkin checkin { get; set; }
            public Checkout checkout { get; set; }
            public Policies policies { get; set; }
            public Fees fees { get; set; }
        }

        public class Image
        {
            public object characteristicCode { get; set; }
            public object imageTypeCode { get; set; }
            public object order { get; set; }
            public object path { get; set; }
            public object roomCode { get; set; }
            public object roomType { get; set; }
            public ImageType imageType { get; set; }
            public object visualOrder { get; set; }
            public object PMSRoomCode { get; set; }
        }

        public class ImageType
        {
            public object code { get; set; }
            public Description description { get; set; }
        }

        public class Name
        {
            public object content { get; set; }
            public object languageCode { get; set; }
        }

        public class Phone
        {
            public object phoneNumber { get; set; }
        }

        public class Policies
        {
            public object knowBeforeYouGo { get; set; }
        }

        public class Room
        {
            public object description { get; set; }
            public object roomCode { get; set; }
            public RoomDetail roomDetail { get; set; }
        }

        public class RoomAmenityReferenceList
        {
            public object amenityId { get; set; }
            public object amenityName { get; set; }
            public object facilityGroupcode { get; set; }
            public object indFee { get; set; }
            public object indLogic { get; set; }
            public object indYesorNO { get; set; }
            public object number { get; set; }
            public object order { get; set; }
            public object voucher { get; set; }
        }

        public class RoomDetail
        {
            public object roomCode { get; set; }
            public object area { get; set; }
            public object maxAdultsNum { get; set; }
            public object capacity { get; set; }
            public List<RoomAmenityReferenceList> roomAmenityReferenceList { get; set; }
            public List<BedList> bedList { get; set; }
            public List<RoomImage> roomImages { get; set; }
            public object window { get; set; }
            public object view { get; set; }
            public object smoking { get; set; }
        }

        public class RoomImage
        {
            public object imageTypeCode { get; set; }
            public object imageType { get; set; }
            public object path { get; set; }
        }

        public class Root
        {
            public object error { get; set; }
            public object from { get; set; }
            public object to { get; set; }
            public List<Hotel> hotels { get; set; }
            public object total { get; set; }
        }
    }

    #endregion
}
