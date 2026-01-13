using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace XConnect.Supplier.Booking.Com
{
    public class BookingClass
    {
    }

    public class BookingDotCom
    {
        public class CheckinTime
        {
            public string from { get; set; }
            public string until { get; set; }
        }

        public class DealTagging
        {
            public string deal_name { get; set; }
            public double public_price { get; set; }
            public int discount_percentage { get; set; }
        }

        public class Meta
        {
            public string ruid { get; set; }
        }

        public class Result
        {
            public string default_language { get; set; }
            public string country { get; set; }
            public int hotel_id { get; set; }
            public string stars { get; set; }
            public string address { get; set; }
            public string hotel_currency_code { get; set; }
            public bool direct_payment { get; set; }
            public string hotel_name { get; set; }
            public string postcode { get; set; }
            public CheckinTime checkin_time { get; set; }
            public List<Room> rooms { get; set; }
            public double price { get; set; }


        }
        public class ExtraCharge
        {
            public double amount { get; set; }
            public string type { get; set; }
            public double charge_amount { get; set; }
            public bool excluded { get; set; }
            public string name { get; set; }
            public string charge_price_mode { get; set; }
        }
        public class Room
        {
            public int num_rooms_available_at_this_price { get; set; }
            public List<object> children { get; set; }
            public string pay_later_collection_date { get; set; }
            public string block_id { get; set; }
            public int adults { get; set; }
            public bool refundable { get; set; }
            public DealTagging deal_tagging { get; set; }
            public int room_id { get; set; }
            public string refundable_until { get; set; }
            public double price { get; set; }
            public string room_name { get; set; }
            public int room_type_id { get; set; }
            public bool deposit_required { get; set; }

            public List<ExtraCharge> extra_charge { get; set; }

        }

        public class Root
        {
            public Meta meta { get; set; }
            public List<Result> result { get; set; }
        }
    }

    public class BookingDotComRoom
    {
        public BookingDotComRoom()
        {
            //
            // TODO: Add constructor logic here
            //
        }
        public class Policy
        {
            public string content { get; set; }
            public string @class { get; set; }
        }

        public class BreakfastCost
        {
            public double price { get; set; }
            public string currency { get; set; }
        }
        public class BedConfiguration
        {
            public List<BedType> bed_types { get; set; }

        }
        public class BedType
        {
            public int count { get; set; }
            public string description_imperial { get; set; }
            public string name { get; set; }
            public string description { get; set; }

        }

        public class Photo
        {
            public int photo_id { get; set; }
            public string url_max300 { get; set; }
            public string url_original { get; set; }

        }
        public class ExtraCharge
        {
            public string name { get; set; }
            public string type { get; set; }
            public bool excluded { get; set; }
            public double charge_amount { get; set; }
            public double amount { get; set; }
            public string currency { get; set; }
            public int charge_price_mode { get; set; }
        }

        public class ExtraChargesBreakdown
        {
            public List<ExtraCharge> extra_charge { get; set; }
        }

        public class MinPrice
        {
            public string currency { get; set; }
            public ExtraChargesBreakdown extra_charges_breakdown { get; set; }
            public double price { get; set; }
            public double charge_price { get; set; }
        }

        public class ExtraBedAvailable
        {
            public bool available { get; set; }
        }

        public class PaymentTerms
        {
            public string name { get; set; }
            public string prepayment_description { get; set; }
            public string cancellation_description { get; set; }
        }

        public class CancellationInfo
        {
            public double fee { get; set; }
            public string until { get; set; }
            public string timezone { get; set; }
            public string currency { get; set; }
            public string from { get; set; }
        }

        public class RackRate
        {
            public string currency { get; set; }
            public double price { get; set; }
        }

        public class ExtraCharge2
        {
            public string name { get; set; }
            public string type { get; set; }
            public bool excluded { get; set; }
            public double charge_amount { get; set; }
            public double amount { get; set; }
            public string currency { get; set; }
            public int charge_price_mode { get; set; }
        }

        public class ExtraChargesBreakdown2
        {
            public List<ExtraCharge2> extra_charge { get; set; }
        }

        public class IncrementalPrice
        {
            public string currency { get; set; }
            public ExtraChargesBreakdown2 extra_charges_breakdown { get; set; }
            public double price { get; set; }
            public double charge_price { get; set; }
        }
        public class CommissionAmount
        {
            public string currency { get; set; }
            public double amount { get; set; }
        }

        public class CommissionDetails
        {
            public double commission_percentage { get; set; }
            public CommissionAmount commission_amount { get; set; }
            public double fee_percentage { get; set; }
            public double commission_amount_hotel_currency { get; set; }
        }
        public class Block
        {
            public int max_children_free { get; set; }
            public int max_children_free_age { get; set; }
            public int max_adults { get; set; }
            public string pay_later_collection_date { get; set; }
            public BreakfastCost breakfast_cost { get; set; }
            public bool is_flash_deal { get; set; }
            public MinPrice min_price { get; set; }
            public int max_occupancy { get; set; }
            public bool is_smart_deal { get; set; }
            public ExtraBedAvailable extra_bed_available { get; set; }
            public bool full_board { get; set; }
            public double room_surface_in_m2 { get; set; }
            public PaymentTerms payment_terms { get; set; }
            public bool half_board { get; set; }
            public int room_type_id { get; set; }
            public bool cot_available { get; set; }
            public List<CancellationInfo> cancellation_info { get; set; }
            public List<object> block_policies { get; set; }
            public CommissionDetails commission_details { get; set; }
            public string block_id { get; set; }
            public RackRate rack_rate { get; set; }
            public bool is_last_minute_deal { get; set; }
            public List<IncrementalPrice> incremental_price { get; set; }
            public bool deposit_required { get; set; }
            public string name { get; set; }
            public bool lunch_included { get; set; }
            public object photos { get; set; }
            public string refundable_until { get; set; }
            public string taxes { get; set; }
            public List<string> facilities { get; set; }
            public string mealplan_description { get; set; }
            public double room_surface_in_feet2 { get; set; }
            public bool dinner_included { get; set; }
            public int room_id { get; set; }
            public bool all_inclusive { get; set; }
            public bool breakfast_included { get; set; }
            public string room_description { get; set; }
            public bool refundable { get; set; }
            public List<BedConfiguration> bed_configurations { get; set; }
            public int min_rate_nr_available_blocks { get; set; }


        }

        public class Result
        {
            public bool direct_payment { get; set; }
            public bool can_pay_now { get; set; }
            public List<Policy> policies { get; set; }
            public int hotel_id { get; set; }
            public bool is_flash_deal { get; set; }
            public string checkin { get; set; }
            public string hotel_url { get; set; }
            public string important_information { get; set; }
            public List<Block> block { get; set; }
            public string license_number { get; set; }
            public string checkout { get; set; }
        }

        public class Meta
        {
            public string ruid { get; set; }
        }

        public class RootObject
        {
            public List<Result> result { get; set; }
            public Meta meta { get; set; }
        }
        //public class Meta
        //{
        //    public string ruid { get; set; }
        //}

        //public class Policy
        //{
        //    public string content { get; set; }
        //    public string @class { get; set; }
        //}

        //public class RackRate
        //{
        //    public double price { get; set; }
        //    public string currency { get; set; }
        //}

        //public class CancellationInfo
        //{
        //    public string from { get; set; }
        //    public double fee { get; set; }
        //    public string until { get; set; }
        //    public string timezone { get; set; }
        //    public string currency { get; set; }
        //}

        //public class IncrementalPrice
        //{
        //    public double price { get; set; }
        //    public string currency { get; set; }
        //}

        //public class MinPrice
        //{
        //    public double price { get; set; }
        //    public string currency { get; set; }
        //}

        //public class BreakfastCost
        //{
        //    public string currency { get; set; }
        //    public double price { get; set; }
        //}

        //public class Block
        //{
        //    public List<object> block_policies { get; set; }
        //    public bool half_board { get; set; }
        //    public int room_id { get; set; }
        //    public RackRate rack_rate { get; set; }
        //    public string mealplan_description { get; set; }
        //    public string photos { get; set; }
        //    public string taxes { get; set; }
        //    public bool all_inclusive { get; set; }
        //    public List<CancellationInfo> cancellation_info { get; set; }
        //    public string room_description { get; set; }
        //    public string block_id { get; set; }
        //    public bool dinner_included { get; set; }
        //    public List<IncrementalPrice> incremental_price { get; set; }
        //    public string name { get; set; }
        //    public int max_occupancy { get; set; }
        //    public bool lunch_included { get; set; }
        //    public MinPrice min_price { get; set; }
        //    public BreakfastCost breakfast_cost { get; set; }
        //    public bool full_board { get; set; }
        //    public bool deposit_required { get; set; }
        //    public bool is_flash_deal { get; set; }
        //    public string refundable_until { get; set; }
        //    public bool breakfast_included { get; set; }
        //    public int room_type_id { get; set; }
        //    public bool refundable { get; set; }
        //}

        //public class Result
        //{
        //    public int hotel_id { get; set; }
        //    public string checkout { get; set; }
        //    public List<Policy> policies { get; set; }
        //    public bool is_flash_deal { get; set; }
        //    public string hotel_url { get; set; }
        //    public string checkin { get; set; }
        //    public List<Block> block { get; set; }
        //}

        //public class RootObject
        //{
        //    public Meta meta { get; set; }
        //    public List<Result> result { get; set; }
        //}
    }

    public class BookingdotcomClass_v1
    {
        public class CheckinTime
        {
            public string from { get; set; }
            public string until { get; set; }
        }

        public class DealTagging
        {
            public string deal_name { get; set; }
            public double public_price { get; set; }
            public int discount_percentage { get; set; }
        }

        public class Meta
        {
            public string ruid { get; set; }
        }

        public class Result
        {
            public string default_language { get; set; }
            public string country { get; set; }
            public int hotel_id { get; set; }
            public string stars { get; set; }
            public string address { get; set; }
            public string hotel_currency_code { get; set; }
            public bool direct_payment { get; set; }
            public string hotel_name { get; set; }
            public string postcode { get; set; }
            public CheckinTime checkin_time { get; set; }
            public List<Room> rooms { get; set; }
            public double price { get; set; }


        }
        public class ExtraCharge
        {
            public double amount { get; set; }
            public string type { get; set; }
            public double charge_amount { get; set; }
            public bool excluded { get; set; }
            public string name { get; set; }
            public string charge_price_mode { get; set; }
        }
        public class Room
        {
            public int num_rooms_available_at_this_price { get; set; }
            public List<object> children { get; set; }
            public string pay_later_collection_date { get; set; }
            public string block_id { get; set; }
            public int adults { get; set; }
            public bool refundable { get; set; }
            public DealTagging deal_tagging { get; set; }
            public int room_id { get; set; }
            public string refundable_until { get; set; }
            public double price { get; set; }
            public string room_name { get; set; }
            public int room_type_id { get; set; }
            public bool deposit_required { get; set; }

            public List<ExtraCharge> extra_charge { get; set; }

        }

        public class Root
        {
            public Meta meta { get; set; }
            public List<Result> result { get; set; }
        }
    }

    public class BookingDotComRoomEss
    {
        // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
        public class AdditionalPolicies
        {
            public bool children_allowed { get; set; }
            public List<CotsAndExtraBed> cots_and_extra_beds { get; set; }
            public int child_min_age { get; set; }
        }

        public class CotsAndExtraBed
        {
            public double price { get; set; }
            public string price_mode { get; set; }
            public string rate_type { get; set; }
            public int to_age { get; set; }
            public int from_age { get; set; }
        }

        public class HotelData
        {
            public string license_number { get; set; }
            public AdditionalPolicies additional_policies { get; set; }
            public string hotel_description { get; set; }
            public string hotel_important_information { get; set; }
            public List<HotelPolicy> hotel_policies { get; set; }
        }

        public class HotelPolicy
        {
            public string content { get; set; }
            public string type { get; set; }
            public string name { get; set; }
        }

        public class Meta
        {
            public string ruid { get; set; }
        }

        public class Result
        {
            public HotelData hotel_data { get; set; }
            public List<object> room_data { get; set; }
            public int hotel_id { get; set; }
        }

        public class Root
        {
            public Meta meta { get; set; }
            public List<Result> result { get; set; }
        }


    }

    public class BookingDotComPriceCheck
    {
        // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
        public class Block
        {
            public int room_id { get; set; }
            public bool refundable { get; set; }
            public int max_children_free { get; set; }
            public string pay_later_collection_date { get; set; }
            public object deal_tagging { get; set; }
            public int max_occupancy { get; set; }
            public bool deposit_required { get; set; }
            public bool is_flash_deal { get; set; }
            public List<IncrementalPrice> incremental_price { get; set; }
            public RackRate rack_rate { get; set; }
            public int max_children_free_age { get; set; }
            public string refundable_until { get; set; }
            public int max_adults { get; set; }
            public string block_id { get; set; }
            public int min_rate_nr_available_blocks { get; set; }
            public string name { get; set; }
            public bool breakfast_included { get; set; }
            public MinPrice min_price { get; set; }
            public string room_description { get; set; }
            public string taxes { get; set; }
            public string photos { get; set; }
        }

        public class ExtraChargesBreakdown
        {
        }

        public class IncrementalPrice
        {
            public double price { get; set; }
            public double charge_price { get; set; }
            public ExtraChargesBreakdown extra_charges_breakdown { get; set; }
            public string currency { get; set; }
        }

        public class Meta
        {
            public string ruid { get; set; }
        }

        public class MinPrice
        {
            public double price { get; set; }
            public double charge_price { get; set; }
            public ExtraChargesBreakdown extra_charges_breakdown { get; set; }
            public string currency { get; set; }
        }

        public class RackRate
        {
            public string currency { get; set; }
            public double price { get; set; }
        }

        public class Result
        {
            public bool is_flash_deal { get; set; }
            public bool direct_payment { get; set; }
            public bool can_pay_now { get; set; }
            public int hotel_id { get; set; }
            public List<Block> block { get; set; }
            public string checkin { get; set; }
            public string checkout { get; set; }
            public string hotel_url { get; set; }
        }

        public class Root
        {
            public List<Result> result { get; set; }
            public Meta meta { get; set; }
        }
    }

    #region classbooking.comv3
    public class searchhotelnroom
    {
        public class Booker
        {
            public string country { get; set; }
            public string platform { get; set; }
        }

        public class Filters
        {
            public string meal_plan { get; set; }
            public string cancellation_type { get; set; }
        }

        public class Guests
        {
            public int number_of_adults { get; set; }
            public int number_of_rooms { get; set; }
            public List<int> children { get; set; }
        }

        public class Root
        {
            public string city { get; set; }
            public List<int> accommodations { get; set; }
            public Booker booker { get; set; }
            public string checkin { get; set; }
            public string checkout { get; set; }
            public List<string> extras { get; set; }
            public Filters filters { get; set; }
            public Guests guests { get; set; }
        }
    }

    public class searchhotelnroomG
    {
        public class Booker
        {
            public string country { get; set; }
            public string platform { get; set; }
            public List<string> user_groups { get; set; }
        }

        public class Filters
        {
            public string meal_plan { get; set; }
            public string cancellation_type { get; set; }
        }

        public class Guests
        {
            public int number_of_adults { get; set; }
            public int number_of_rooms { get; set; }
            public List<int> children { get; set; }
        }

        public class Root
        {
            public string city { get; set; }
            public List<int> accommodations { get; set; }
            public Booker booker { get; set; }
            public string checkin { get; set; }
            public string checkout { get; set; }
            public List<string> extras { get; set; }
            public Filters filters { get; set; }
            public Guests guests { get; set; }
        }
    }

    public class reshotelsearchnroom
    {
        public class Rootobject
        {
            public string request_id { get; set; }
            public Datum[] data { get; set; }
        }

        public class Datum
        {
            public int id { get; set; }
            public string currency { get; set; }
            public string deep_link_url { get; set; }
            public Product1[] products { get; set; }
            //public Recommendation recommendation { get; set; }
            public string url { get; set; }
        }

        public class Recommendation
        {
            public Price price { get; set; }
            public Product[] products { get; set; }
        }

        public class Price
        {
            public float _base { get; set; }
            public float book { get; set; }
            public Extra_Charges? extra_charges { get; set; }
            public float total { get; set; }
        }

        public class Extra_Charges
        {
            public float excluded { get; set; }
            public float included { get; set; }
        }

        public class Product
        {
            public string id { get; set; }
            public object[] children { get; set; }
            public int number_of_adults { get; set; }
            public Price1 price { get; set; }
        }

        public class Price1
        {
            public float _base { get; set; }
            public float book { get; set; }
            public Extra_Charges1? extra_charges { get; set; }
            public float total { get; set; }
        }

        public class Extra_Charges1
        {
            //public float excluded { get; set; }
            public float included { get; set; }
        }

        public class Product1
        {
            public string id { get; set; }
            public object deal { get; set; }
            public Commission commission { get; set; }
            public Maximum_Occupancy maximum_occupancy { get; set; }
            public int number_available { get; set; }
            public Policies policies { get; set; }
            public Price2 price { get; set; }
            public int room { get; set; }
        }

        public class Maximum_Occupancy
        {
            public int adults { get; set; }
            public object children { get; set; }
            public int total { get; set; }
        }

        public class Policies
        {
            public Cancellation cancellation { get; set; }
            public Meal_Plan meal_plan { get; set; }
            public Payment payment { get; set; }
        }

        public class Cancellation
        {
            public DateTime? free_cancellation_until { get; set; }
            public string type { get; set; }
        }

        public class Meal_Plan
        {
            public string[] meals { get; set; }
            public string plan { get; set; }
        }

        public class Payment
        {
            public bool prepayment_required { get; set; }
            public string[] timings { get; set; }
        }

        public class Price2
        {
            public float _base { get; set; }
            public float book { get; set; }
            public Extra_Charges2? extra_charges { get; set; }
            public float total { get; set; }
        }
        public class Commission
        {
            public double amount { get; set; }
            public double percentage { get; set; }
        }
        public class Extra_Charges2
        {
            public object[] conditional { get; set; }
            public excluded[] excluded { get; set; }
            public Included[] included { get; set; }
        }

        public class Included
        {
            public int charge { get; set; }
            public string mode { get; set; }
            public float? percentage { get; set; }
            public float? total_amount { get; set; }
            public float? unit_amount { get; set; }
        }
        public class excluded
        {
            public int charge { get; set; }
            public string mode { get; set; }
            public float? percentage { get; set; }
            public float? total_amount { get; set; }
            public float? unit_amount { get; set; }
        }

    }
    public class detailrq
    {
        public class Root
        {
            public List<int> accommodations { get; set; }
            public List<string> extras { get; set; }
            public List<string> languages { get; set; }
        }

    }
    //public class detailres
    //{

    //    public class Address
    //    {
    //        public string address_line { get; set; }
    //        public string city { get; set; }
    //        public string country { get; set; }
    //        public string post_code { get; set; }

    //        [JsonProperty("en-gb")]
    //        public string engb { get; set; }
    //    }

    //    public class BedConfiguration
    //    {
    //        public string id { get; set; }
    //        public List<Configuration> configuration { get; set; }
    //    }

    //    public class BedOption
    //    {
    //        public List<BedConfiguration> bed_configurations { get; set; }
    //        public bool has_bathroom { get; set; }
    //        public bool is_bedroom { get; set; }
    //    }

    //    public class CheckinCheckoutTimes
    //    {
    //        public string checkin_from { get; set; }
    //        public object checkin_to { get; set; }
    //        public string checkout_from { get; set; }
    //        public string checkout_to { get; set; }
    //    }

    //    public class Configuration
    //    {
    //        public int bed_type { get; set; }
    //        public int number_of_beds { get; set; }
    //    }

    //    public class Coordinates
    //    {
    //        public double latitude { get; set; }
    //        public double longitude { get; set; }
    //    }

    //    public class CotsAndExtraBeds
    //    {
    //        public bool are_allowed_together { get; set; }
    //        public int maximum_number_of_cots { get; set; }
    //        public int maximum_number_of_extra_beds { get; set; }
    //    }

    //    public class CribsAndExtraBeds
    //    {
    //        public bool are_allowed { get; set; }
    //        public int maximum_number_of_cribs { get; set; }
    //        public int maximum_number_of_extra_beds { get; set; }
    //    }

    //    public class Datum
    //    {
    //        public int id { get; set; }
    //        public Name name { get; set; }
    //        public int accommodation_type { get; set; }
    //        public List<object> brands { get; set; }
    //        public CheckinCheckoutTimes checkin_checkout_times { get; set; }
    //        public string currency { get; set; }
    //        public string deep_link_url { get; set; }
    //        public Description description { get; set; }
    //        public List<Facility> facilities { get; set; }
    //        public bool is_work_friendly { get; set; }
    //        public Location location { get; set; }
    //        public int number_of_rooms { get; set; }
    //        public string price_category { get; set; }
    //        public Programmes programmes { get; set; }
    //        public Rating rating { get; set; }
    //        public List<Room> rooms { get; set; }
    //        public List<string> spoken_languages { get; set; }
    //        public List<int> themes { get; set; }
    //        public string url { get; set; }
    //    }

    //    public class Description
    //    {
    //        public ImportantInformation important_information { get; set; }
    //        public List<object> license_numbers { get; set; }
    //        public Text text { get; set; }
    //        public Trader trader { get; set; }

    //        [JsonProperty("en-gb")]
    //        public string engb { get; set; }
    //    }

    //    public class Facility
    //    {
    //        public int id { get; set; }
    //        public List<string> attributes { get; set; }
    //    }

    //    public class ImportantInformation
    //    {
    //        [JsonProperty("en-gb")]
    //        public string engb { get; set; }
    //    }

    //    public class Location
    //    {
    //        public Address address { get; set; }
    //        public int city { get; set; }
    //        public Coordinates coordinates { get; set; }
    //        public string country { get; set; }
    //        public List<int> districts { get; set; }
    //        public string postal_code { get; set; }
    //        public List<int> regions { get; set; }
    //    }

    //    public class MaximumOccupancy
    //    {
    //        public int adults { get; set; }
    //        public int children { get; set; }
    //        public int total_guests { get; set; }
    //    }

    //    public class Name
    //    {
    //        [JsonProperty("en-gb")]
    //        public string engb { get; set; }
    //    }

    //    public class NumberOfRooms
    //    {
    //        public int bathrooms { get; set; }
    //        public int bedrooms { get; set; }
    //        public int living_rooms { get; set; }
    //    }

    //    public class Programmes
    //    {
    //        public bool travel_proud { get; set; }
    //    }

    //    public class Rating
    //    {
    //        public int number_of_reviews { get; set; }
    //        public bool preferred { get; set; }
    //        public string review_score { get; set; }
    //        public double stars { get; set; }
    //        public string stars_type { get; set; }
    //    }

    //    public class Room
    //    {
    //        public int id { get; set; }
    //        public Name name { get; set; }
    //        public List<string> attributes { get; set; }
    //        public List<BedOption> bed_options { get; set; }
    //        public CotsAndExtraBeds cots_and_extra_beds { get; set; }
    //        public CribsAndExtraBeds cribs_and_extra_beds { get; set; }
    //        public Description description { get; set; }
    //        public List<int> facilities { get; set; }
    //        public MaximumOccupancy maximum_occupancy { get; set; }
    //        public NumberOfRooms number_of_rooms { get; set; }
    //        public int room_type { get; set; }
    //        public int size { get; set; }
    //    }

    //    public class Root
    //    {
    //        public string request_id { get; set; }
    //        public List<Datum> data { get; set; }
    //    }

    //    public class Text
    //    {
    //        [JsonProperty("en-gb")]
    //        public string engb { get; set; }
    //    }

    //    public class Trader
    //    {
    //        public Address address { get; set; }
    //        public string email { get; set; }
    //        public string name { get; set; }
    //        public string registration_number { get; set; }
    //        public string telephone { get; set; }
    //        public string trade_register { get; set; }
    //    }


    //}
    public class detailres
    {
        public class Address
        {
            public string address_line { get; set; }
            public string city { get; set; }
            public string country { get; set; }
            public string post_code { get; set; }

            [JsonProperty("en-gb")]
            public string engb { get; set; }
        }

        public class Age
        {
            public int from { get; set; }
            public int? to { get; set; }
        }

        public class BedConfiguration
        {
            public string id { get; set; }
            public List<Configuration> configuration { get; set; }
        }

        public class BedOption
        {
            public List<BedConfiguration> bed_configurations { get; set; }
            public bool has_bathroom { get; set; }
            public bool is_bedroom { get; set; }
        }

        public class CheckinCheckoutTimes
        {
            public string checkin_from { get; set; }
            public object checkin_to { get; set; }
            public object checkout_from { get; set; }
            public string checkout_to { get; set; }
        }

        public class Configuration
        {
            public int bed_type { get; set; }
            public int number_of_beds { get; set; }
        }

        public class Coordinates
        {
            public double latitude { get; set; }
            public double longitude { get; set; }
        }

        public class CotsAndExtraBed
        {
            public Age age { get; set; }
            public string mode { get; set; }
            public object percentage { get; set; }
            public double price { get; set; }
            public string type { get; set; }
            public bool are_allowed_together { get; set; }
            public int maximum_number_of_cots { get; set; }
            public int maximum_number_of_extra_beds { get; set; }
        }

        public class CribsAndExtraBeds
        {
            public bool are_allowed { get; set; }
            public int maximum_number_of_cribs { get; set; }
            public int maximum_number_of_extra_beds { get; set; }
        }
        public class CotsAndExtraBeds
        {
            public bool are_allowed_together { get; set; }
            public int maximum_number_of_cots { get; set; }
            public int maximum_number_of_extra_beds { get; set; }
        }

        public class Datum
        {
            public int id { get; set; }
            public Name name { get; set; }
            public int accommodation_type { get; set; }
            public bool booker_address_required { get; set; }
            public List<object> brands { get; set; }
            public CheckinCheckoutTimes checkin_checkout_times { get; set; }
            public string currency { get; set; }
            public string deep_link_url { get; set; }
            public Description description { get; set; }
            public List<Facility> facilities { get; set; }
            public FacilityDetails facility_details { get; set; }
            public bool is_genius { get; set; }
            public bool is_work_friendly { get; set; }
            public Location location { get; set; }
            public MealPrices meal_prices { get; set; }
            public int number_of_rooms { get; set; }
            public Policies policies { get; set; }
            public string price_category { get; set; }
            public Programmes programmes { get; set; }
            public Rating rating { get; set; }
            public List<Room> rooms { get; set; }
            public List<string> spoken_languages { get; set; }
            public List<int> themes { get; set; }
            public string url { get; set; }
        }

        public class Description
        {
            public string host_type { get; set; }
            public ImportantInformation important_information { get; set; }
            public List<string> license_numbers { get; set; }
            public Text text { get; set; }
            public Trader trader { get; set; }

            [JsonProperty("en-gb")]
            public string engb { get; set; }
        }

        public class Facility
        {
            public int id { get; set; }
            public List<string> attributes { get; set; }
        }

        public class FacilityDetails
        {
            public InternetFacility internet_facility { get; set; }
            public List<ParkingFacility> parking_facilities { get; set; }
            public List<RestaurantFacility> restaurant_facilities { get; set; }
            public List<SwimmingPoolFacility> swimming_pool_facilities { get; set; }
        }

        public class ImportantInformation
        {
            [JsonProperty("en-gb")]
            public string engb { get; set; }
        }

        public class InternetFacility
        {
            public double price { get; set; }
            public string charge_mode { get; set; }
            public string coverage { get; set; }
            public string connection_type { get; set; }
        }

        public class Location
        {
            public Address address { get; set; }
            public int city { get; set; }
            public Coordinates coordinates { get; set; }
            public string country { get; set; }
            public List<int> districts { get; set; }
            public string postal_code { get; set; }
            public List<int> regions { get; set; }
        }

        public class MaximumOccupancy
        {
            public int adults { get; set; }
            public int children { get; set; }
            public int total_guests { get; set; }
        }

        public class MealPrices
        {
            public object breakfast { get; set; }
            public object lunch { get; set; }
            public object dinner { get; set; }
        }

        public class Name
        {
            [JsonProperty("en-gb")]
            public string engb { get; set; }
        }

        public class NumberOfRooms
        {
            public int bathrooms { get; set; }
            public int bedrooms { get; set; }
            public int living_rooms { get; set; }
        }

        public class ParkingFacility
        {
            public double price { get; set; }
            public string charge_mode { get; set; }
            public string type { get; set; }
            public string location { get; set; }
            public string reservation { get; set; }
        }

        public class Pets
        {
            public string allowed { get; set; }
            public object charge_mode { get; set; }
        }

        public class Policies
        {
            public List<CotsAndExtraBed> cots_and_extra_beds { get; set; }
            public object damage { get; set; }
            public object maximum_checkin_age { get; set; }
            public int minimum_checkin_age { get; set; }
            public int minimum_guest_age { get; set; }
            public Pets pets { get; set; }
        }

        public class Programmes
        {
            public bool travel_proud { get; set; }
        }

        public class Rating
        {
            public object number_of_reviews { get; set; }
            public object preferred { get; set; }
            public object review_score { get; set; }
            public object stars { get; set; }
            public object stars_type { get; set; }
        }

        public class RestaurantFacility
        {
            public string name { get; set; }
            public string status { get; set; }
            public bool guests_only { get; set; }
            public bool accept_reservations { get; set; }
        }

        public class Room
        {
            public int id { get; set; }
            public Name name { get; set; }
            public List<string> attributes { get; set; }
            public List<BedOption> bed_options { get; set; }
            public CotsAndExtraBeds cots_and_extra_beds { get; set; }
            public CribsAndExtraBeds cribs_and_extra_beds { get; set; }
            public Description description { get; set; }
            public List<int> facilities { get; set; }
            public MaximumOccupancy maximum_occupancy { get; set; }
            public NumberOfRooms number_of_rooms { get; set; }
            public int room_type { get; set; }
            public int size { get; set; }
        }

        public class Root
        {
            public string request_id { get; set; }
            public List<Datum> data { get; set; }
        }

        public class SwimmingPoolFacility
        {
            public string type { get; set; }
            public string payment_type { get; set; }
            public string allowed_age_type { get; set; }
        }

        public class Text
        {
            [JsonProperty("en-gb")]
            public string engb { get; set; }
        }

        public class Trader
        {
            public Address address { get; set; }
            public string email { get; set; }
            public string name { get; set; }
            public string registration_number { get; set; }
            public string telephone { get; set; }
            public string trade_register { get; set; }
            public bool trader_verified { get; set; }
        }



    }
    public class MergedRoom
    {
        public int id { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public decimal totalPrice { get; set; }
        public string cancellationPolicy { get; set; }
        public string mealInfo { get; set; }
    }
    public class preebookrq
    {
        public class Accommodation
        {
            public int id { get; set; }
            public string checkin { get; set; }
            public string checkout { get; set; }
            public List<Product> products { get; set; }
        }

        public class Allocation
        {
            public int number_of_adults { get; set; }
            public List<int> children { get; set; }
        }

        public class Booker
        {
            public string country { get; set; }
            public string platform { get; set; }
            public string travel_purpose { get; set; }
            public List<string> user_groups { get; set; }
        }

        public class Product
        {
            public string id { get; set; }
            public Allocation allocation { get; set; }

        }

        public class Root
        {
            public Booker booker { get; set; }
            public string currency { get; set; }
            public Accommodation accommodation { get; set; }
        }
    }
    public class preebookrs
    {
        // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
        // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
        public class Accommodation
        {
            public int id { get; set; }
            public Currency currency { get; set; }
            public GeneralPolicies general_policies { get; set; }
            public Price price { get; set; }
            public List<Product> products { get; set; }
        }

        public class Base
        {
            public double accommodation_currency { get; set; }
            public double booker_currency { get; set; }
        }

        public class Cancellation
        {
            public object from { get; set; }
            public Price price { get; set; }
        }

        public class Currency
        {
            public string accommodation { get; set; }
            public string booker { get; set; }
        }

        public class Data
        {
            public Accommodation accommodation { get; set; }
            public string order_token { get; set; }
        }

        public class Date
        {
            public string at { get; set; }
            public Price price { get; set; }
        }

        public class Deal
        {
            public int discount_percentage { get; set; }
            public double public_price { get; set; }
            public List<string> tags { get; set; }
        }

        public class ExtraCharges
        {
            public List<object> conditional { get; set; }
            public List<NonConditional> non_conditional { get; set; }
        }

        public class GeneralPolicies
        {
            public Payment payment { get; set; }
        }

        public class MealPlan
        {
            public List<object> meals { get; set; }
            public string plan { get; set; }
        }

        public class Methods
        {
            public bool airplus { get; set; }
            public bool wallet { get; set; }
            public List<object> cards { get; set; }
        }

        public class NonConditional
        {
            public int charge { get; set; }
            public TotalAmount total_amount { get; set; }
            public object chargeable_online { get; set; }
            public string mode { get; set; }
            public double? percentage { get; set; }
            public UnitAmount unit_amount { get; set; }
        }

        public class PayAtTheProperty
        {
            public bool method_required { get; set; }
            public List<Date> dates { get; set; }
            public Methods methods { get; set; }
        }

        public class Payment
        {
            public PayAtTheProperty pay_at_the_property { get; set; }
            public PayOnlineLater pay_online_later { get; set; }
            public PayOnlineNow pay_online_now { get; set; }
        }
        public class PayOnlineLater
        {
            public bool method_required { get; set; }
            public List<Date> dates { get; set; }
            public Methods methods { get; set; }
        }

        public class PayOnlineNow
        {
            public bool method_required { get; set; }
            public List<Date> dates { get; set; }
            public Methods methods { get; set; }
        }
        public class Policies
        {
            public List<Cancellation> cancellation { get; set; }
            public MealPlan meal_plan { get; set; }
        }

        public class Price
        {
            public double accommodation_currency { get; set; }
            public double booker_currency { get; set; }
            public Base @base { get; set; }
            public ExtraCharges extra_charges { get; set; }
            public object chargeable_online { get; set; }
            public Total total { get; set; }
        }

        public class Product
        {
            public string id { get; set; }
            public Deal deal { get; set; }
            public Policies policies { get; set; }
            public Price price { get; set; }
            public int room { get; set; }
        }

        public class Root
        {
            public string request_id { get; set; }
            public Data data { get; set; }
        }

        public class Total
        {
            public double accommodation_currency { get; set; }
            public double booker_currency { get; set; }
        }

        public class TotalAmount
        {
            public double accommodation_currency { get; set; }
            public double booker_currency { get; set; }
        }

        public class UnitAmount
        {
            public double? accommodation_currency { get; set; }
            public double? booker_currency { get; set; }
        }




    }
    //public class preebookrs_old
    //{
    //    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
    //    public class Accommodation
    //    {
    //        public int id { get; set; }
    //        public Currency currency { get; set; }
    //        public GeneralPolicies general_policies { get; set; }
    //        public Price price { get; set; }
    //        public List<Product> products { get; set; }
    //    }

    //    public class Base
    //    {
    //        public double accommodation_currency { get; set; }
    //        public double booker_currency { get; set; }
    //    }

    //    public class Cancellation
    //    {
    //        public object from { get; set; }
    //        public Price price { get; set; }
    //    }

    //    public class ChargeableOnline
    //    {
    //        public double accommodation_currency { get; set; }
    //        public double booker_currency { get; set; }
    //    }

    //    public class Currency
    //    {
    //        public string accommodation { get; set; }
    //        public string booker { get; set; }
    //    }

    //    public class Data
    //    {
    //        public Accommodation accommodation { get; set; }
    //        public string order_token { get; set; }
    //    }

    //    public class Date
    //    {
    //        public string at { get; set; }
    //        public Price price { get; set; }
    //    }

    //    public class ExtraCharges
    //    {
    //        public List<object> conditional { get; set; }
    //        public List<NonConditional> non_conditional { get; set; }
    //    }

    //    public class GeneralPolicies
    //    {
    //        public Payment payment { get; set; }
    //    }

    //    public class MealPlan
    //    {
    //        public List<string> meals { get; set; }
    //        public string plan { get; set; }
    //    }

    //    public class Methods
    //    {
    //        public bool airplus { get; set; }
    //        public bool wallet { get; set; }
    //        public List<int> cards { get; set; }
    //    }

    //    public class NonConditional
    //    {
    //        public int charge { get; set; }
    //        public TotalAmount total_amount { get; set; }
    //        public bool chargeable_online { get; set; }
    //        public string mode { get; set; }
    //        public double? percentage { get; set; }
    //        public UnitAmount unit_amount { get; set; }
    //    }

    //    public class PayAtTheProperty
    //    {
    //        public bool method_required { get; set; }
    //        public List<Date> dates { get; set; }
    //        public Methods methods { get; set; }
    //    }

    //    public class Payment
    //    {
    //        public PayAtTheProperty pay_at_the_property { get; set; }
    //        public PayOnlineLater pay_online_later { get; set; }
    //        public PayOnlineNow pay_online_now { get; set; }
    //    }

    //    public class PayOnlineLater
    //    {
    //        public bool method_required { get; set; }
    //        public List<Date> dates { get; set; }
    //        public Methods methods { get; set; }
    //    }

    //    public class PayOnlineNow
    //    {
    //        public bool method_required { get; set; }
    //        public List<Date> dates { get; set; }
    //        public Methods methods { get; set; }
    //    }

    //    public class Policies
    //    {
    //        public List<Cancellation> cancellation { get; set; }
    //        public MealPlan meal_plan { get; set; }
    //    }

    //    public class Price
    //    {
    //        public double accommodation_currency { get; set; }
    //        public double booker_currency { get; set; }
    //        public Base @base { get; set; }
    //        public ExtraCharges extra_charges { get; set; }
    //        public ChargeableOnline chargeable_online { get; set; }
    //        public Total total { get; set; }
    //    }

    //    public class Product
    //    {
    //        public string id { get; set; }
    //        public object deal { get; set; }
    //        public Policies policies { get; set; }
    //        public Price price { get; set; }
    //        public int room { get; set; }
    //    }

    //    public class Root
    //    {
    //        public string request_id { get; set; }
    //        public Data data { get; set; }
    //    }

    //    public class Total
    //    {
    //        public double accommodation_currency { get; set; }
    //        public double booker_currency { get; set; }
    //    }

    //    public class TotalAmount
    //    {
    //        public double accommodation_currency { get; set; }
    //        public double booker_currency { get; set; }
    //    }

    //    public class UnitAmount
    //    {
    //        public double? accommodation_currency { get; set; }
    //        public double? booker_currency { get; set; }
    //    }


    //}
    public class bookingrq
    {
        public class Accommodation
        {
            public string label { get; set; }
            public List<Product> products { get; set; }
            public Remarks remarks { get; set; }
        }

        public class Address
        {
            public string address_line { get; set; }
            public string city { get; set; }
            public string country { get; set; }
            public string post_code { get; set; }
        }

        public class Booker
        {
            public Address address { get; set; }
            public string company { get; set; }
            public string email { get; set; }
            public string language { get; set; }
            public Name name { get; set; }
            public string telephone { get; set; }
        }

        public class Card
        {
            public string cardholder { get; set; }
            public string cvc { get; set; }
            public string expiry_date { get; set; }
            public string number { get; set; }
            public authentication authentication { get; set; }
        }
        public class authentication
        {
            //public riskified riskified { get; set; }
            public string sca_exemption { get; set; }
        }
        //public class riskified
        //{
        //    public string ip_address { get; set; }
        //    public string session_id { get; set; }
        //}
        public class EstimatedArrivalTime
        {
            public int hour { get; set; }
        }

        public class Guest
        {
            public string email { get; set; }
            public string name { get; set; }
        }

        public class Name
        {
            public string first_name { get; set; }
            public string last_name { get; set; }
        }

        public class Payment
        {
            public Card card { get; set; }
            public bool include_receipt { get; set; }
            public string method { get; set; }
            public string timing { get; set; }
        }


        public class Product
        {
            public string id { get; set; }
            public string bed_configuration { get; set; }
            public List<Guest> guests { get; set; }
        }

        public class Remarks
        {
            public EstimatedArrivalTime estimated_arrival_time { get; set; }
            public string special_requests { get; set; }
        }

        public class Root
        {
            public Accommodation accommodation { get; set; }
            public Booker booker { get; set; }
            public string order_token { get; set; }
            public Payment payment { get; set; }
        }

    }
    public class bookingrs
    {
        public class Accommodation
        {
            public string order { get; set; }
            public string pincode { get; set; }
            public object reservation { get; set; }
        }

        public class Data
        {
            public Accommodation accommodation { get; set; }
            public Payment payment { get; set; }
        }

        public class Payment
        {
            public object authorisation_form_url { get; set; }

            public object receipt_url { get; set; }
        }

        public class Root
        {
            public string request_id { get; set; }
            public Data data { get; set; }
        }
    }
    public class cancelrq
    {
        public class Root
        {
            public string order { get; set; }
            public string reason { get; set; }
        }

    }
    public class cancelrs
    {
        public class Data
        {
            public string status { get; set; }
        }

        public class Root
        {
            public string request_id { get; set; }
            public Data data { get; set; }
        }

    }
    #endregion

    


}
