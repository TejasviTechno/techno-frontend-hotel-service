using System.Data;

namespace XConnect.Expedia
{
    public class ExpediaClass
    {
    }

    #region EAN class

    public class Amenity_EAN
    {
        public int id { get; set; }
        public string name { get; set; }
    }

    public class PaymentOptions_EAN
    {
        public string method { get; set; }
        public string href { get; set; }
    }

    public class Links_EAN
    {
        public PaymentOptions_EAN payment_options { get; set; }
    }

    public class PriceCheck_EAN
    {
        public string method { get; set; }
        public string href { get; set; }
    }

    public class Links2_EAN
    {
        public PriceCheck_EAN price_check { get; set; }
    }

    public class Configuration_EAN
    {
        public string type { get; set; }
        public string size { get; set; }
        public int quantity { get; set; }
    }

    public class BedGroup_EAN
    {
        public Links2_EAN links { get; set; }
        public List<Configuration_EAN> configuration { get; set; }
        public string description { get; set; }
    }

    public class CancelPenalty_EAN
    {
        public DateTime start { get; set; }
        public DateTime end { get; set; }
        public string amount { get; set; }
        public string nights { get; set; }
        public string percent { get; set; }
        public string currency { get; set; }
    }

    public class BillableCurrency_EAN
    {
        public string value { get; set; }
        public string currency { get; set; }
    }

    public class Inclusive_EAN
    {
        public BillableCurrency_EAN billable_currency { get; set; }
    }

    public class BillableCurrency2_EAN
    {
        public string value { get; set; }
        public string currency { get; set; }
    }

    public class Exclusive_EAN
    {
        public BillableCurrency2_EAN billable_currency { get; set; }
    }

    public class BillableCurrency3_EAN
    {
        public string value { get; set; }
        public string currency { get; set; }
    }

    public class MarketingFee_EAN
    {
        public BillableCurrency3_EAN billable_currency { get; set; }
    }

    public class Totals
    {
        public Inclusive_EAN inclusive { get; set; }
        public Exclusive_EAN exclusive { get; set; }
        public MarketingFee_EAN marketing_fee { get; set; }
    }

    //public class __invalid_type__2
    //{
    //    public List<List<>> nightly { get; set; }
    //    public Totals totals { get; set; }
    //}

    public class BillableCurrency4_EAN
    {
        public string value { get; set; }
        public string currency { get; set; }
    }

    public class Inclusive2_EAN
    {
        public BillableCurrency4_EAN billable_currency { get; set; }
    }

    public class BillableCurrency5_EAN
    {
        public string value { get; set; }
        public string currency { get; set; }
    }

    public class Exclusive2_EAN
    {
        public BillableCurrency5_EAN billable_currency { get; set; }
    }

    public class BillableCurrency6_EAN
    {
        public string value { get; set; }
        public string currency { get; set; }
    }

    public class MarketingFee2_EAN
    {
        public BillableCurrency6_EAN billable_currency { get; set; }
    }

    public class Totals2_EAN
    {
        public Inclusive2_EAN inclusive { get; set; }
        public Exclusive2_EAN exclusive { get; set; }
        public MarketingFee2_EAN marketing_fee { get; set; }
    }

    //public class __invalid_type__132
    //{
    //    public List<List<>> nightly { get; set; }
    //    public Totals2 totals { get; set; }
    //}

    //public class Occupancies
    //{
    //    public __invalid_type__2 __invalid_name__2 { get; set; }
    //    public __invalid_type__132 __invalid_name__1-3,2 { get; set; }
    //}

    public class Rate_EAN
    {
        public string id { get; set; }
        public string merchant_of_record { get; set; }
        public int available_rooms { get; set; }
        public string promo_id { get; set; }
        public bool refundable { get; set; }
        public bool deposit_required { get; set; }
        public bool fenced_deal { get; set; }
        //public List<Amenity_EAN> amenities { get; set; }
        public SortedList<string, Amenity_EAN> amenities { get; set; }
        public Links_EAN links { get; set; }
        //public List<BedGroup_EAN> bed_groups { get; set; }
        public SortedList<string, BedGroup_EAN> bed_groups { get; set; }

        public List<CancelPenalty_EAN> cancel_penalties { get; set; }
        public Object occupancy_pricing { get; set; }
        public Promotions promotions { get; set; }

    }
    public class Promotions
    {
        public SortedList<string, ValueAdds> value_adds { get; set; }
        public Deal deal { get; set; }
    }
    public class ValueAdds
    {
        public string id { get; set; }
        public string description { get; set; }
    }

    public class Deal
    {
        public string id { get; set; }
        public string description { get; set; }
    }
    public class Room_EAN2
    {
        public string id { get; set; }
        public string room_name { get; set; }
        public List<Rate_EAN> rates { get; set; }
    }

    public class AdditionalRates_EAN
    {
        public string method { get; set; }
        public string href { get; set; }
    }

    public class Links3_EAN
    {
        public AdditionalRates_EAN additional_rates { get; set; }
    }

    public class RootObject_EAN2
    {
        public string status { get; set; }
        public string property_id { get; set; }
        public List<Room_EAN2> rooms { get; set; }
        public Links3_EAN links { get; set; }
        public Links4_EAN links4 { get; set; }
        public Object occupancies { get; set; }

    }

    public class RootObject1_EAN
    {
        public string status { get; set; }
        public Links4_EAN links { get; set; }
        public Object occupancy_pricing { get; set; }
    }
    public class Links4_EAN
    {
        public Book_EAN book { get; set; }
    }

    public class Book_EAN
    {
        public string method { get; set; }
        public string href { get; set; }
    }

    public class ng
    {
        public List<DataTable> nightly = new List<DataTable>();
        public DataTable stay = new DataTable();

        public cltotal totals = new cltotal();
        public Fees fees { get; set; }

    }
    public class Fees
    {
        public MandatoryFee mandatory_fee { get; set; }
        public ResortFee resort_fee { get; set; }
        public MandatoryTax mandatory_tax { get; set; }
    }
    public class BillableCurrency6
    {
        public string value { get; set; }
        public string currency { get; set; }
    }
    public class RequestCurrency6
    {
        public string value { get; set; }
        public string currency { get; set; }
    }
    public class MandatoryFee
    {
        public BillableCurrency6 billable_currency { get; set; }
        public RequestCurrency6 request_currency { get; set; }
    }
    public class BillableCurrency7
    {
        public string value { get; set; }
        public string currency { get; set; }
    }
    public class RequestCurrency7
    {
        public string value { get; set; }
        public string currency { get; set; }
    }
    public class ResortFee
    {
        public BillableCurrency7 billable_currency { get; set; }
        public RequestCurrency7 request_currency { get; set; }
    }
    public class BillableCurrency8
    {
        public string value { get; set; }
        public string currency { get; set; }
    }
    public class RequestCurrency8
    {
        public string value { get; set; }
        public string currency { get; set; }
    }
    public class MandatoryTax
    {
        public BillableCurrency8 billable_currency { get; set; }
        public RequestCurrency8 request_currency { get; set; }
    }
    public class cltotal
    {
        public Inclusive inclusive { get; set; }
        public Exclusive exclusive { get; set; }
        public MarketingFee marketing_fee { get; set; }
        public Strikethrough strikethrough { get; set; }
        public GrossProfit gross_profit { get; set; }

    }
    public class GrossProfit
    {
        public BillableCurrency billable_currency { get; set; }
        //public RequestCurrency request_currency { get; set; }
    }

    public class Strikethrough
    {
        public BillableCurrency billable_currency { get; set; }
        public RequestCurrency6 request_currency { get; set; }
    }
    public class BillableCurrency
    {
        public string value { get; set; }
        public string currency { get; set; }
    }
    public class Inclusive
    {
        public BillableCurrency billable_currency { get; set; }
    }
    public class Exclusive
    {
        public BillableCurrency billable_currency { get; set; }
    }
    public class MarketingFee
    {
        public BillableCurrency billable_currency { get; set; }
    }

    #endregion

    #region EAN class New
    public class EAN
    {
        public class Amenity_EAN
        {
            public string id { get; set; }
            public string name { get; set; }
        }

        public class PaymentOptions_EAN
        {
            public string method { get; set; }
            public string href { get; set; }
        }

        public class Links_EAN
        {
            public PaymentOptions_EAN payment_options { get; set; }
        }

        public class PriceCheck_EAN
        {
            public string method { get; set; }
            public string href { get; set; }
        }

        public class Links2_EAN
        {
            public PriceCheck_EAN price_check { get; set; }
        }

        public class Configuration_EAN
        {
            public string type { get; set; }
            public string size { get; set; }
            public int quantity { get; set; }
        }

        public class BedGroup_EAN
        {
            public Links2_EAN links { get; set; }
            public List<Configuration_EAN> configuration { get; set; }
            public string description { get; set; }
        }

        public class CancelPenalty_EAN
        {
            public DateTime start { get; set; }
            public DateTime end { get; set; }
            public string amount { get; set; }
            public string nights { get; set; }
            public string percent { get; set; }
            public string currency { get; set; }
        }

        public class BillableCurrency_EAN
        {
            public string value { get; set; }
            public string currency { get; set; }
        }

        public class Inclusive_EAN
        {
            public BillableCurrency_EAN billable_currency { get; set; }
        }

        public class BillableCurrency2_EAN
        {
            public string value { get; set; }
            public string currency { get; set; }
        }

        public class Exclusive_EAN
        {
            public BillableCurrency2_EAN billable_currency { get; set; }
        }

        public class BillableCurrency3_EAN
        {
            public string value { get; set; }
            public string currency { get; set; }
        }

        public class MarketingFee_EAN
        {
            public BillableCurrency3_EAN billable_currency { get; set; }
        }

        public class Totals
        {
            public Inclusive_EAN inclusive { get; set; }
            public Exclusive_EAN exclusive { get; set; }
            public MarketingFee_EAN marketing_fee { get; set; }
        }

        //public class __invalid_type__2
        //{
        //    public List<List<>> nightly { get; set; }
        //    public Totals totals { get; set; }
        //}

        public class BillableCurrency4_EAN
        {
            public string value { get; set; }
            public string currency { get; set; }
        }

        public class Inclusive2_EAN
        {
            public BillableCurrency4_EAN billable_currency { get; set; }
        }

        public class BillableCurrency5_EAN
        {
            public string value { get; set; }
            public string currency { get; set; }
        }

        public class Exclusive2_EAN
        {
            public BillableCurrency5_EAN billable_currency { get; set; }
        }

        public class BillableCurrency6_EAN
        {
            public string value { get; set; }
            public string currency { get; set; }
        }

        public class MarketingFee2_EAN
        {
            public BillableCurrency6_EAN billable_currency { get; set; }
        }

        public class Totals2_EAN
        {
            public Inclusive2_EAN inclusive { get; set; }
            public Exclusive2_EAN exclusive { get; set; }
            public MarketingFee2_EAN marketing_fee { get; set; }
        }

        //public class __invalid_type__132
        //{
        //    public List<List<>> nightly { get; set; }
        //    public Totals2 totals { get; set; }
        //}

        //public class Occupancies
        //{
        //    public __invalid_type__2 __invalid_name__2 { get; set; }
        //    public __invalid_type__132 __invalid_name__1-3,2 { get; set; }
        //}

        public class Rate_EAN
        {
            public string id { get; set; }
            public string merchant_of_record { get; set; }
            public int available_rooms { get; set; }
            public string promo_id { get; set; }
            public bool refundable { get; set; }
            public bool deposit_required { get; set; }
            public bool fenced_deal { get; set; }
            //public List<Amenity_EAN> amenities { get; set; }
            public SortedList<string, Amenity_EAN> amenities { get; set; }
            public Links_EAN links { get; set; }
            //public List<BedGroup_EAN> bed_groups { get; set; }
            public SortedList<string, BedGroup_EAN> bed_groups { get; set; }

            public List<CancelPenalty_EAN> cancel_penalties { get; set; }
            public Dictionary<string, OccupancyPricing> occupancy_pricing { get; set; }
            public Promotions promotions { get; set; }

        }

        public class OccupancyPricing
        {
            public List<List<Nightly>> nightly { get; set; }
            public IEnumerable<Nightly> stay { get; set; }

            public Dictionary<string, FeeDetail> fees { get; set; }

            public Dictionary<string, FeeDetail> totals { get; set; }


        }

        public class FeeDetail
        {
            public Nightly billable_currency { get; set; }

            public Nightly request_currency { get; set; }
        }

        public class Nightly
        {
            public string type { get; set; }
            public string value { get; set; }
            public string currency { get; set; }
        }
        public class Promotions
        {
            public SortedList<string, ValueAdds> value_adds { get; set; }
            public Deal deal { get; set; }
        }
        public class ValueAdds
        {
            public string id { get; set; }
            public string description { get; set; }
        }

        public class Deal
        {
            public string id { get; set; }
            public string description { get; set; }
        }
        public class Room_EAN2
        {
            public string id { get; set; }
            public string room_name { get; set; }
            public List<Rate_EAN> rates { get; set; }
        }

        public class AdditionalRates_EAN
        {
            public string method { get; set; }
            public string href { get; set; }
        }

        public class Links3_EAN
        {
            public AdditionalRates_EAN additional_rates { get; set; }
        }

        public class RootObject_EAN2
        {
            public string status { get; set; }
            public string property_id { get; set; }
            public List<Room_EAN2> rooms { get; set; }
            public Links3_EAN links { get; set; }
            public Links4_EAN links4 { get; set; }
            public Object occupancies { get; set; }

        }

        public class RootObject1_EAN
        {
            public string status { get; set; }
            public Links4_EAN links { get; set; }
            public Object occupancy_pricing { get; set; }
        }
        public class Links4_EAN
        {
            public Book_EAN book { get; set; }
        }

        public class Book_EAN
        {
            public string method { get; set; }
            public string href { get; set; }
        }

        public class ng
        {
            public List<DataTable> nightly = new List<DataTable>();
            public DataTable stay = new DataTable();

            public cltotal totals = new cltotal();
            public Fees fees { get; set; }

        }
        public class Fees
        {
            public MandatoryFee mandatory_fee { get; set; }
            public ResortFee resort_fee { get; set; }
            public MandatoryTax mandatory_tax { get; set; }
        }
        public class BillableCurrency6
        {
            public string value { get; set; }
            public string currency { get; set; }
        }
        public class RequestCurrency6
        {
            public string value { get; set; }
            public string currency { get; set; }
        }
        public class MandatoryFee
        {
            public BillableCurrency6 billable_currency { get; set; }
            public RequestCurrency6 request_currency { get; set; }
        }
        public class BillableCurrency7
        {
            public string value { get; set; }
            public string currency { get; set; }
        }
        public class RequestCurrency7
        {
            public string value { get; set; }
            public string currency { get; set; }
        }
        public class ResortFee
        {
            public BillableCurrency7 billable_currency { get; set; }
            public RequestCurrency7 request_currency { get; set; }
        }
        public class BillableCurrency8
        {
            public string value { get; set; }
            public string currency { get; set; }
        }
        public class RequestCurrency8
        {
            public string value { get; set; }
            public string currency { get; set; }
        }
        public class MandatoryTax
        {
            public BillableCurrency8 billable_currency { get; set; }
            public RequestCurrency8 request_currency { get; set; }
        }
        public class cltotal
        {
            public Inclusive inclusive { get; set; }
            public Exclusive exclusive { get; set; }
            public MarketingFee marketing_fee { get; set; }
            public Strikethrough strikethrough { get; set; }
            public GrossProfit gross_profit { get; set; }

        }
        public class GrossProfit
        {
            public BillableCurrency billable_currency { get; set; }
            //public RequestCurrency request_currency { get; set; }
        }

        public class Strikethrough
        {
            public BillableCurrency billable_currency { get; set; }
            public RequestCurrency6 request_currency { get; set; }
        }
        public class BillableCurrency
        {
            public string value { get; set; }
            public string currency { get; set; }
        }
        public class Inclusive
        {
            public BillableCurrency billable_currency { get; set; }
        }
        public class Exclusive
        {
            public BillableCurrency billable_currency { get; set; }
        }
        public class MarketingFee
        {
            public BillableCurrency billable_currency { get; set; }
        }

    }

    #endregion

    #region Property Content Class
    public class CheckinPropertyContent_EAN
    {
        public string begin_time { get; set; }
        public string instructions { get; set; }
        public string special_instructions { get; set; }
        public string min_age { get; set; }
        public string end_time { get; set; }
    }

    public class PropertyContent_EAN
    {
        public CheckinPropertyContent_EAN checkin { get; set; }
        public Checkout checkout { get; set; }
        public Fees1 fees { get; set; }
        public Policies policies { get; set; }
        public AllInclusive all_inclusive { get; set; }
    }
    public class Checkout
    {
        public string time { get; set; }
    }
    public class Fees1
    {
        public string mandatory { get; set; }
        public string optional { get; set; }
    }
    public class Policies
    {
        public string know_before_you_go { get; set; }
    }
    public class AllInclusive
    {
        public bool all_rate_plans { get; set; }
        public bool some_rate_plans { get; set; }
        public string details { get; set; }
    }
    public class RootObjectPropertyContent_EAN
    {
        public PropertyContent_EAN HotelCode { get; set; }
    }
    #endregion


    #region EAN PropertyNew Clas
    public class EAN_Pro
    {
        public class RootObject1_EAN
        {
            public string status { get; set; }
            public Links4_EAN links { get; set; }
            public Dictionary<string, OccupancyPricing> occupancy_pricing { get; set; }
        }
        public class Links4_EAN
        {
            public Book_EAN book { get; set; }
        }

        public class Book_EAN
        {
            public string method { get; set; }
            public string href { get; set; }
        }

        public class OccupancyPricing
        {
            public List<List<Nightly>> nightly { get; set; }
            public IEnumerable<Nightly> stay { get; set; }

            public Dictionary<string, FeeDetail> fees { get; set; }

            public Dictionary<string, FeeDetail> totals { get; set; }


        }

        public class FeeDetail
        {
            public Nightly billable_currency { get; set; }

            public Nightly request_currency { get; set; }
        }

        public class Nightly
        {
            public string type { get; set; }
            public string value { get; set; }
            public string currency { get; set; }
        }
    }

    #endregion
    #region EAN Create Class
    public class Room_EAN
    {
        public string title { get; set; }
        public string given_name { get; set; }
        public string family_name { get; set; }
        public string email { get; set; }
        public Phone phone { get; set; }
        public bool smoking { get; set; }
        public string special_request { get; set; }
    }
    public class Phone
    {
        public string country_code { get; set; }
        public string area_code { get; set; }
        public string number { get; set; }
    }

    public class Address_EAN
    {
        public string line_1 { get; set; }
        public string line_2 { get; set; }
        public string line_3 { get; set; }
        public string city { get; set; }
        public string state_province_code { get; set; }
        public string postal_code { get; set; }
        public string country_code { get; set; }
    }

    public class BillingContact_EAN
    {
        public string given_name { get; set; }
        public string family_name { get; set; }
        public string email { get; set; }
        public Phone phone { get; set; }
        public Address_EAN address { get; set; }
    }

    public class Payment_EAN
    {
        public string type { get; set; }
        public string card_type { get; set; }
        public string number { get; set; }
        public string security_code { get; set; }
        public string expiration_month { get; set; }
        public string expiration_year { get; set; }
        public BillingContact_EAN billing_contact { get; set; }
    }

    public class RootObject_EAN
    {
        public string affiliate_reference_id { get; set; }
        public bool hold { get; set; }
        public List<Room_EAN> rooms { get; set; }
        public List<Payment_EAN> payments { get; set; }
        //public string affiliate_metadata { get; set; }
        //public string tax_registration_number { get; set; }
    }



    public class RetrieveCreate_EAN
    {
        public string method { get; set; }
        public string href { get; set; }
    }

    public class LinksCreate_EAN
    {
        public RetrieveCreate_EAN retrieve { get; set; }
    }

    public class RootObjectCreate_EAN
    {
        public string itinerary_id { get; set; }
        public LinksCreate_EAN links { get; set; }
    }


    #endregion
    #region EAN Booking Class
    public class ConfirmationIdBook_EAN
    {
        public string expedia { get; set; }
    }

    public class CancelRefundBook_EAN
    {
        public string amount { get; set; }
        public string currency { get; set; }
    }

    public class CancelPenaltyBook_EAN
    {
        public string currency { get; set; }
        public int nights { get; set; }
        public DateTime start { get; set; }
        public DateTime end { get; set; }
    }

    public class FeeBook_EAN
    {
        public string type { get; set; }
        public string value { get; set; }
        public string scope { get; set; }
        public string frequency { get; set; }
    }

    public class RateBook_EAN
    {
        public string id { get; set; }
        public string merchant_of_record { get; set; }
        public bool refundable { get; set; }
        public CancelRefundBook_EAN cancel_refund { get; set; }
        //public List<List<>> nightly { get; set; }
        public List<CancelPenaltyBook_EAN> cancel_penalties { get; set; }
        public List<FeeBook_EAN> fees { get; set; }
    }

    public class CancelBook_EAN
    {
        public string method { get; set; }
        public string href { get; set; }
    }

    public class ChangeBook_EAN
    {
        public string method { get; set; }
        public string href { get; set; }
    }

    public class LinksBook_EAN
    {
        public CancelBook_EAN cancel { get; set; }
        public ChangeBook_EAN change { get; set; }
    }

    public class RoomBook
    {
        public string id { get; set; }
        public string bed_group_id { get; set; }
        public ConfirmationIdBook_EAN confirmation_id { get; set; }
        public string checkin { get; set; }
        public string checkout { get; set; }
        public int number_of_adults { get; set; }
        public string given_name { get; set; }
        public string family_name { get; set; }
        public string status { get; set; }
        public bool smoking { get; set; }
        public RateBook_EAN rate { get; set; }
        public LinksBook_EAN links { get; set; }
    }

    public class AddressBook_EAN
    {
        public string line_1 { get; set; }
        public string city { get; set; }
        public string state_province_code { get; set; }
        public string postal_code { get; set; }
        public string country_code { get; set; }
    }

    public class BillingContactBook_EAN
    {
        public string email { get; set; }
        public string given_name { get; set; }
        public string family_name { get; set; }
        public Phone phone { get; set; }
        public AddressBook_EAN address { get; set; }
    }

    public class RootObjectBook_EAN
    {
        public string itinerary_id { get; set; }
        public string property_id { get; set; }
        public List<RoomBook> rooms { get; set; }
        public BillingContactBook_EAN billing_contact { get; set; }
        public DateTime creation_date_time { get; set; }
        public string affiliate_reference_id { get; set; }
    }
    #endregion
    #region EANV3 Booking Class
    public class EANV3BookRequest
    {
        public class Room
        {
            public string title { get; set; }
            public string given_name { get; set; }
            public string family_name { get; set; }
            public string email { get; set; }
            public Phone phone { get; set; }
            public bool smoking { get; set; }
            public string special_request { get; set; }
        }
        public class Address
        {
            public string line_1 { get; set; }
            public string line_2 { get; set; }
            public string line_3 { get; set; }
            public string city { get; set; }
            public string state_province_code { get; set; }
            public string postal_code { get; set; }
            public string country_code { get; set; }
        }

        public class BillingContact
        {
            public string given_name { get; set; }
            public string family_name { get; set; }
            public string email { get; set; }
            public Phone phone { get; set; }
            public Address address { get; set; }
        }
        public class Payment
        {
            public string type { get; set; }
            public string card_type { get; set; }
            public string number { get; set; }
            public string security_code { get; set; }
            public string expiration_month { get; set; }
            public string expiration_year { get; set; }
            public BillingContact billing_contact { get; set; }
        }
        public class Phone
        {
            public string country_code { get; set; }
            public string area_code { get; set; }
            public string number { get; set; }
        }
        public class RootObject
        {
            public string affiliate_reference_id { get; set; }
            public bool hold { get; set; }
            public List<Room> rooms { get; set; }
            public List<Payment> payments { get; set; }
            public Phone phone { get; set; }
            public string email { get; set; }

            //public string affiliate_metadata { get; set; }
            //public string tax_registration_number { get; set; }
        }
    }

    #endregion
}
