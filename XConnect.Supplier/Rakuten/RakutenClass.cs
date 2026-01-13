using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XConnect.Supplier.Rakuten
{
    public class Rakuten
    {
        public class RakutenClass
        {
            #region RakutenClass
            public static class RakutenHotelListResponse
            {
                public class Search
                {
                    public List<string> hotel_id_list { get; set; }
                    public string check_in_date { get; set; }
                    public string check_out_date { get; set; }
                    public int room_count { get; set; }
                    public int adult_count { get; set; }
                    public string currency { get; set; }
                    public string locale { get; set; }
                    public string source_market { get; set; }
                    public List<int> children { get; set; }
                }

                public class Beds
                {
                    public int queen { get; set; }
                }

                public class RoomDetails
                {
                    public string room_code { get; set; }
                    public string rate_plan_code { get; set; }
                    public object rate_plan_description { get; set; }
                    public string description { get; set; }
                    public int food { get; set; }
                    public bool non_refundable { get; set; }
                    public string room_type { get; set; }
                    public string room_view { get; set; }
                    public Beds beds { get; set; }
                    public string supplier_description { get; set; }
                    public object non_smoking { get; set; }
                    public object room_gender { get; set; }
                    public object benefits { get; set; }
                    public object floor { get; set; }
                    public object amenitites { get; set; }
                }

                public class CancellationPolicy2
                {
                    public double penalty_percentage { get; set; }
                    public DateTime date_from { get; set; }
                    public DateTime date_to { get; set; }
                }

                public class Cancellation_policy
                {
                    public string remarks { get; set; }
                    public List<CancellationPolicy2> cancellation_policies { get; set; }
                }

                public class Package
                {
                    public string hotel_id { get; set; }
                    public RoomDetails room_details { get; set; }
                    public string booking_key { get; set; }
                    public double room_rate { get; set; }
                    public string room_rate_currency { get; set; }
                    public int client_commission { get; set; }
                    public string client_commission_currency { get; set; }
                    public double chargeable_rate { get; set; }
                    public string chargeable_rate_currency { get; set; }
                    public Cancellation_policy cancellation_policy { get; set; }
                    public string rate_type { get; set; }
                }

                public class Rates
                {
                    public List<Package> packages { get; set; }
                }

                public class Hotel
                {
                    public string id { get; set; }
                    public Rates rates { get; set; }
                }

                public class Root
                {
                    public string session_id { get; set; }
                    public string event_id { get; set; }
                    public string status { get; set; }
                    public Search search { get; set; }
                    public List<Hotel> hotels { get; set; }
                }
            }

            public static class RakutenHotelRoomResponse
            {
                public class Search
                {
                    public string hotel_id { get; set; }
                    public string check_in_date { get; set; }
                    public string check_out_date { get; set; }
                    public int room_count { get; set; }
                    public int adult_count { get; set; }
                    public string currency { get; set; }
                    public string locale { get; set; }
                    public string source_market { get; set; }
                    public object children { get; set; }
                }

                //public class Beds
                //{
                //    public int queen { get; set; }
                //}

                public class RoomDetails
                {
                    public string room_code { get; set; }
                    public string rate_plan_code { get; set; }
                    public object rate_plan_description { get; set; }
                    public string description { get; set; }
                    public int food { get; set; }
                    public bool non_refundable { get; set; }
                    public string room_type { get; set; }
                    public string room_view { get; set; }
                    //public Beds beds { get; set; }
                    public Dictionary<string, int> beds { get; set; }
                    public string supplier_description { get; set; }
                    public object non_smoking { get; set; }
                    public object room_gender { get; set; }
                    public object benefits { get; set; }
                    public object floor { get; set; }
                    public object amenitites { get; set; }
                }

                public class CancellationPolicy
                {
                    public double penalty_percentage { get; set; }
                    public DateTime date_from { get; set; }
                    public DateTime date_to { get; set; }
                }

                public class Cancellation_policy
                {
                    public string remarks { get; set; }
                    public List<CancellationPolicy> cancellation_policies { get; set; }
                }

                public class Package
                {
                    public string booking_key { get; set; }
                    public double chargeable_rate { get; set; }
                    public string chargeable_rate_currency { get; set; }
                    public int client_commission { get; set; }
                    public string client_commission_currency { get; set; }
                    public Cancellation_policy cancellation_policy { get; set; }
                    public string hotel_id { get; set; }
                    public string rate_type { get; set; }
                    public RoomDetails room_details { get; set; }
                    public double room_rate { get; set; }
                    public string room_rate_currency { get; set; }

                    public TaxesAndFees taxes_and_fees { get; set; } //11/12/25
                    //public List<int> daily_number_of_units { get; set; } //11/12/25
                    public string created_at { get; set; } //11/12/25
                }

                #region Added on 11/12/25
                public class TaxAndServiceFee
                {
                    public string currency { get; set; }
                    public double value { get; set; }
                }

                public class TaxesAndFees
                {
                    public TaxAndServiceFee TaxAndServiceFee { get; set; }
                }
                #endregion

                public class Rates
                {
                    public List<Package> packages { get; set; }
                }

                public class Hotel
                {
                    public string id { get; set; }
                    public Rates rates { get; set; }
                }

                public class Root
                {
                    public string session_id { get; set; }
                    public string event_id { get; set; }
                    public string status { get; set; }
                    public Search search { get; set; }
                    public List<Hotel> hotels { get; set; }
                }


            }

            public static class RakutenPriceCheckResponse
            {

                public class CancellationPolicy2
                {
                    public double penalty_percentage { get; set; }
                    public DateTime date_from { get; set; }
                    public DateTime date_to { get; set; }
                }

                public class CancellationPolicy
                {
                    public string remarks { get; set; }
                    public List<CancellationPolicy2> cancellation_policies { get; set; }
                }

                public class Beds
                {
                    public int queen { get; set; }
                }

                public class RoomDetails
                {
                    public string room_code { get; set; }
                    public string rate_plan_code { get; set; }
                    public object rate_plan_description { get; set; }
                    public string description { get; set; }
                    public int food { get; set; }
                    public bool non_refundable { get; set; }
                    public string room_type { get; set; }
                    public string room_view { get; set; }
                    public Beds beds { get; set; }
                    public string supplier_description { get; set; }
                    public object non_smoking { get; set; }
                    public object room_gender { get; set; }
                    public object benefits { get; set; }
                    public object floor { get; set; }
                    public object amenitites { get; set; }
                }

                public class Package
                {
                    public string hotel_id { get; set; }
                    public RoomDetails room_details { get; set; }
                    public string booking_key { get; set; }
                    public double room_rate { get; set; }
                    public string room_rate_currency { get; set; }
                    public int client_commission { get; set; }
                    public string client_commission_currency { get; set; }
                    public double chargeable_rate { get; set; }
                    public string chargeable_rate_currency { get; set; }
                    public string rate_type { get; set; }
                }

                public class Root
                {
                    public string session_id { get; set; }
                    public string event_id { get; set; }
                    public string booking_policy_id { get; set; }
                    public CancellationPolicy cancellation_policy { get; set; }
                    public Package package { get; set; }
                }


            }

            public static class RakutenGetBookingPolicyResponse
            {
                public class CancellationPolicy2
                {
                    public double penalty_percentage { get; set; }
                    public DateTime date_from { get; set; }
                    public DateTime date_to { get; set; }
                }

                public class CancellationPolicy
                {
                    public string remarks { get; set; }
                    public List<CancellationPolicy> cancellation_policies { get; set; }
                }

                public class Beds
                {
                    public int queen { get; set; }
                }

                public class RoomDetails
                {
                    public string room_code { get; set; }
                    public string rate_plan_code { get; set; }
                    public string description { get; set; }
                    public int food { get; set; }
                    public string room_type { get; set; }
                    public string room_view { get; set; }
                    public Beds beds { get; set; }
                    public string supplier_description { get; set; }
                }

                public class Package
                {
                    public string hotel_id { get; set; }
                    public RoomDetails room_details { get; set; }
                    public string booking_key { get; set; }
                    public double room_rate { get; set; }
                    public string room_rate_currency { get; set; }
                    public double client_commission { get; set; }
                    public string client_commission_currency { get; set; }
                    public double chargeable_rate { get; set; }
                    public string chargeable_rate_currency { get; set; }
                    public string rate_type { get; set; }
                    public CancellationPolicy cancellation_policy { get; set; }
                }

                public class Search
                {
                    public string check_in_date { get; set; }
                    public string check_out_date { get; set; }
                    public int room_count { get; set; }
                    public int adult_count { get; set; }
                    public string currency { get; set; }
                    public string locale { get; set; }
                    public string source_market { get; set; }
                }

                public class Request
                {
                    public Search search { get; set; }
                    public Package package { get; set; }
                    public object config { get; set; }
                }

                public class Root
                {
                    public string session_id { get; set; }
                    public string event_id { get; set; }
                    public string booking_policy_id { get; set; }
                    public CancellationPolicy cancellation_policy { get; set; }
                    public Package package { get; set; }
                    public Request request { get; set; }
                }
            }

            public static class RakutenPreBookRequest
            {
                public class RoomLeadGuest
                {
                    public string salutation { get; set; }
                    public string first_name { get; set; }
                    public string last_name { get; set; }
                    public string nationality { get; set; }
                }

                public class ContactPerson
                {
                    public string salutation { get; set; }
                    public string first_name { get; set; }
                    public string last_name { get; set; }
                    public string contact_no { get; set; }
                }

                public class Root
                {
                    public string booking_policy_id { get; set; }
                    public string client_reference { get; set; }
                    public List<RoomLeadGuest> room_lead_guests { get; set; }
                    public ContactPerson contact_person { get; set; }
                }
            }

            public static class RakutenPreBookResponse
            {

                public class Beds
                {
                    public int queen { get; set; }
                }

                public class RoomDetails
                {
                    public string room_code { get; set; }
                    public string rate_plan_code { get; set; }
                    public string description { get; set; }
                    public int food { get; set; }
                    public string room_type { get; set; }
                    public string room_view { get; set; }
                    public Beds beds { get; set; }
                    public string supplier_description { get; set; }
                }

                public class TaxesAndFees
                {
                }

                public class Package
                {
                    public string hotel_id { get; set; }
                    public RoomDetails room_details { get; set; }
                    public DateTime check_in_date { get; set; }
                    public DateTime check_out_date { get; set; }
                    public int adult_count { get; set; }
                    public int room_count { get; set; }
                    public double room_rate { get; set; }
                    public string room_rate_currency { get; set; }
                    public double chargeable_rate { get; set; }
                    public string chargeable_rate_currency { get; set; }
                    public double client_commission { get; set; }
                    public string client_commission_currency { get; set; }
                    public string rate_type { get; set; }
                    public TaxesAndFees taxes_and_fees { get; set; }
                }

                public class Guest
                {
                    public string salutation { get; set; }
                    public string first_name { get; set; }
                    public string last_name { get; set; }
                    public string email { get; set; }
                    public string city { get; set; }
                    public string state { get; set; }
                    public string street { get; set; }
                    public string postal_code { get; set; }
                    public string country { get; set; }
                    public string remarks { get; set; }
                    public string nationality { get; set; }
                    public string contact_no { get; set; }
                }

                public class Zumata
                {
                    public string currency { get; set; }
                    public double value { get; set; }
                }

                public class Client
                {
                    public string currency { get; set; }
                    public int value { get; set; }
                }

                public class Pricing
                {
                    public Zumata zumata { get; set; }
                    public Client client { get; set; }
                }

                public class CancellationPolicy2
                {
                    public double penalty_percentage { get; set; }
                    public DateTime date_from { get; set; }
                    public DateTime date_to { get; set; }
                }

                public class CancellationPolicy
                {
                    public string remarks { get; set; }
                    public List<CancellationPolicy> cancellation_policies { get; set; }
                }

                public class BookingDetails
                {
                    public string customer_reference { get; set; }
                    public string customer_booking_id { get; set; }
                }

                public class ContactPerson
                {
                    public string salutation { get; set; }
                    public string first_name { get; set; }
                    public string last_name { get; set; }
                    public string email { get; set; }
                    public string city { get; set; }
                    public string state { get; set; }
                    public string street { get; set; }
                    public string postal_code { get; set; }
                    public string country { get; set; }
                    public string remarks { get; set; }
                    public string nationality { get; set; }
                    public string contact_no { get; set; }
                }

                public class RoomLeadGuest
                {
                    public string salutation { get; set; }
                    public string first_name { get; set; }
                    public string last_name { get; set; }
                    public string nationality { get; set; }
                    public string remarks { get; set; }
                }

                public class Root
                {
                    public string session_id { get; set; }
                    public string event_id { get; set; }
                    public string booking_id { get; set; }
                    public string client_reference { get; set; }
                    public string status { get; set; }
                    public string status_payment { get; set; }
                    public DateTime requested_at { get; set; }
                    public Package package { get; set; }
                    public Guest guest { get; set; }
                    public Pricing pricing { get; set; }
                    public CancellationPolicy cancellation_policy { get; set; }
                    public BookingDetails booking_details { get; set; }
                    public ContactPerson contact_person { get; set; }
                    public List<RoomLeadGuest> room_lead_guests { get; set; }
                    public bool bundled_booking { get; set; }
                    public string locale { get; set; }
                    public string source_market { get; set; }
                    public bool is_custom { get; set; }
                }
            }

            public static class RakutenPostBookResponse
            {
                // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse); 
                public class Beds
                {
                    public int queen { get; set; }
                }

                public class RoomDetails
                {
                    public string room_code { get; set; }
                    public string rate_plan_code { get; set; }
                    public string description { get; set; }
                    public int food { get; set; }
                    public string room_type { get; set; }
                    public string room_view { get; set; }
                    public Beds beds { get; set; }
                    public string supplier_description { get; set; }
                }

                public class TaxesAndFees
                {
                }

                public class Package
                {
                    public string hotel_id { get; set; }
                    public RoomDetails room_details { get; set; }
                    public DateTime check_in_date { get; set; }
                    public DateTime check_out_date { get; set; }
                    public int adult_count { get; set; }
                    public int room_count { get; set; }
                    public double room_rate { get; set; }
                    public string room_rate_currency { get; set; }
                    public double chargeable_rate { get; set; }
                    public string chargeable_rate_currency { get; set; }
                    public double client_commission { get; set; }
                    public string client_commission_currency { get; set; }
                    public string rate_type { get; set; }
                    public TaxesAndFees taxes_and_fees { get; set; }
                }

                public class Guest
                {
                    public string salutation { get; set; }
                    public string first_name { get; set; }
                    public string last_name { get; set; }
                    public string email { get; set; }
                    public string city { get; set; }
                    public string state { get; set; }
                    public string street { get; set; }
                    public string postal_code { get; set; }
                    public string country { get; set; }
                    public string remarks { get; set; }
                    public string nationality { get; set; }
                    public string contact_no { get; set; }
                }

                public class Zumata
                {
                    public string currency { get; set; }
                    public double value { get; set; }
                }

                public class Client
                {
                    public string currency { get; set; }
                    public int value { get; set; }
                }

                public class Pricing
                {
                    public Zumata zumata { get; set; }
                    public Client client { get; set; }
                }

                public class CancellationPolicy2
                {
                    public double penalty_percentage { get; set; }
                    public DateTime date_from { get; set; }
                    public DateTime date_to { get; set; }
                }

                public class CancellationPolicy
                {
                    public string remarks { get; set; }
                    public List<CancellationPolicy> cancellation_policies { get; set; }
                }

                public class BookingDetails
                {
                    public string customer_reference { get; set; }
                    public string customer_booking_id { get; set; }
                }

                public class ContactPerson
                {
                    public string salutation { get; set; }
                    public string first_name { get; set; }
                    public string last_name { get; set; }
                    public string email { get; set; }
                    public string city { get; set; }
                    public string state { get; set; }
                    public string street { get; set; }
                    public string postal_code { get; set; }
                    public string country { get; set; }
                    public string remarks { get; set; }
                    public string nationality { get; set; }
                    public string contact_no { get; set; }
                }

                public class RoomLeadGuest
                {
                    public string salutation { get; set; }
                    public string first_name { get; set; }
                    public string last_name { get; set; }
                    public string nationality { get; set; }
                    public string remarks { get; set; }
                }

                public class Root
                {
                    public string session_id { get; set; }
                    public string event_id { get; set; }
                    public string booking_id { get; set; }
                    public string client_reference { get; set; }
                    public string status { get; set; }
                    public string status_payment { get; set; }
                    public DateTime requested_at { get; set; }
                    public Package package { get; set; }
                    public Guest guest { get; set; }
                    public Pricing pricing { get; set; }
                    public CancellationPolicy cancellation_policy { get; set; }
                    public BookingDetails booking_details { get; set; }
                    public ContactPerson contact_person { get; set; }
                    public List<RoomLeadGuest> room_lead_guests { get; set; }
                    public bool bundled_booking { get; set; }
                    public string locale { get; set; }
                    public string source_market { get; set; }
                    public bool is_custom { get; set; }
                }


            }

            public static class RakutenBookingStatusResponse
            {
                // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse); 
                public class Beds
                {
                    public int queen { get; set; }
                }

                public class RoomDetails
                {
                    public string room_code { get; set; }
                    public string rate_plan_code { get; set; }
                    public string description { get; set; }
                    public int food { get; set; }
                    public string room_type { get; set; }
                    public string room_view { get; set; }
                    public Beds beds { get; set; }
                    public string supplier_description { get; set; }
                }

                public class TaxesAndFees
                {
                }

                public class Package
                {
                    public string hotel_id { get; set; }
                    public RoomDetails room_details { get; set; }
                    public DateTime check_in_date { get; set; }
                    public DateTime check_out_date { get; set; }
                    public int adult_count { get; set; }
                    public int room_count { get; set; }
                    public double room_rate { get; set; }
                    public string room_rate_currency { get; set; }
                    public double chargeable_rate { get; set; }
                    public string chargeable_rate_currency { get; set; }
                    public double client_commission { get; set; }
                    public string client_commission_currency { get; set; }
                    public string rate_type { get; set; }
                    public TaxesAndFees taxes_and_fees { get; set; }
                }

                public class Guest
                {
                    public string salutation { get; set; }
                    public string first_name { get; set; }
                    public string last_name { get; set; }
                    public string email { get; set; }
                    public string city { get; set; }
                    public string state { get; set; }
                    public string street { get; set; }
                    public string postal_code { get; set; }
                    public string country { get; set; }
                    public string remarks { get; set; }
                    public string nationality { get; set; }
                    public string contact_no { get; set; }
                }

                public class Zumata
                {
                    public string currency { get; set; }
                    public double value { get; set; }
                }

                public class Client
                {
                    public string currency { get; set; }
                    public int value { get; set; }
                }

                public class Pricing
                {
                    public Zumata zumata { get; set; }
                    public Client client { get; set; }
                }

                public class CancellationPolicy2
                {
                    public double penalty_percentage { get; set; }
                    public DateTime date_from { get; set; }
                    public DateTime date_to { get; set; }
                }

                public class CancellationPolicy
                {
                    public string remarks { get; set; }
                    public List<CancellationPolicy> cancellation_policies { get; set; }
                }

                public class BookingDetails
                {
                    public string customer_reference { get; set; }
                    public string customer_booking_id { get; set; }
                }

                public class ContactPerson
                {
                    public string salutation { get; set; }
                    public string first_name { get; set; }
                    public string last_name { get; set; }
                    public string email { get; set; }
                    public string city { get; set; }
                    public string state { get; set; }
                    public string street { get; set; }
                    public string postal_code { get; set; }
                    public string country { get; set; }
                    public string remarks { get; set; }
                    public string nationality { get; set; }
                    public string contact_no { get; set; }
                }

                public class RoomLeadGuest
                {
                    public string salutation { get; set; }
                    public string first_name { get; set; }
                    public string last_name { get; set; }
                    public string nationality { get; set; }
                    public string remarks { get; set; }
                }

                public class Root
                {
                    public string session_id { get; set; }
                    public string event_id { get; set; }
                    public string booking_id { get; set; }
                    public string client_reference { get; set; }
                    public string status { get; set; }
                    public string status_payment { get; set; }
                    public DateTime requested_at { get; set; }
                    public DateTime confirmed_at { get; set; }
                    public Package package { get; set; }
                    public Guest guest { get; set; }
                    public Pricing pricing { get; set; }
                    public CancellationPolicy cancellation_policy { get; set; }
                    public BookingDetails booking_details { get; set; }
                    public ContactPerson contact_person { get; set; }
                    public List<RoomLeadGuest> room_lead_guests { get; set; }
                    public bool bundled_booking { get; set; }
                    public string locale { get; set; }
                    public string source_market { get; set; }
                    public bool is_custom { get; set; }
                }


            }

            public static class RakutenCancelRequest
            {
                public class Root
                {
                    public string booking_id { get; set; }
                }
            }

            public static class RakutenCancelResponse
            {
                // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse); 
                public class Beds
                {
                    public int queen { get; set; }
                }

                public class RoomDetails
                {
                    public string room_code { get; set; }
                    public string rate_plan_code { get; set; }
                    public string description { get; set; }
                    public int food { get; set; }
                    public string room_type { get; set; }
                    public string room_view { get; set; }
                    public Beds beds { get; set; }
                    public string supplier_description { get; set; }
                }

                public class TaxesAndFees
                {
                }

                public class Package
                {
                    public string hotel_id { get; set; }
                    public RoomDetails room_details { get; set; }
                    public DateTime check_in_date { get; set; }
                    public DateTime check_out_date { get; set; }
                    public int adult_count { get; set; }
                    public int room_count { get; set; }
                    public double room_rate { get; set; }
                    public string room_rate_currency { get; set; }
                    public double chargeable_rate { get; set; }
                    public string chargeable_rate_currency { get; set; }
                    public double client_commission { get; set; }
                    public string client_commission_currency { get; set; }
                    public string rate_type { get; set; }
                    public TaxesAndFees taxes_and_fees { get; set; }
                }

                public class Guest
                {
                    public string salutation { get; set; }
                    public string first_name { get; set; }
                    public string last_name { get; set; }
                    public string email { get; set; }
                    public string city { get; set; }
                    public string state { get; set; }
                    public string street { get; set; }
                    public string postal_code { get; set; }
                    public string country { get; set; }
                    public string remarks { get; set; }
                    public string nationality { get; set; }
                    public string contact_no { get; set; }
                }

                public class Zumata
                {
                    public string currency { get; set; }
                    public double value { get; set; }
                }

                public class Client
                {
                    public string currency { get; set; }
                    public int value { get; set; }
                }

                public class Pricing
                {
                    public Zumata zumata { get; set; }
                    public Client client { get; set; }
                }

                public class ApiPenalty
                {
                    public string currency { get; set; }
                    public double value { get; set; }
                }

                public class CancellationDetails
                {
                    public DateTime cancelled_at { get; set; }
                    public ApiPenalty api_penalty { get; set; }
                    public double api_penalty_percentage { get; set; }
                }

                public class CancellationPolicy2
                {
                    public double penalty_percentage { get; set; }
                    public DateTime date_from { get; set; }
                    public DateTime date_to { get; set; }
                }

                public class CancellationPolicy
                {
                    public string remarks { get; set; }
                    public List<CancellationPolicy> cancellation_policies { get; set; }
                }

                public class BookingDetails
                {
                    public string customer_reference { get; set; }
                    public string customer_booking_id { get; set; }
                }

                public class ContactPerson
                {
                    public string salutation { get; set; }
                    public string first_name { get; set; }
                    public string last_name { get; set; }
                    public string email { get; set; }
                    public string city { get; set; }
                    public string state { get; set; }
                    public string street { get; set; }
                    public string postal_code { get; set; }
                    public string country { get; set; }
                    public string remarks { get; set; }
                    public string nationality { get; set; }
                    public string contact_no { get; set; }
                }

                public class RoomLeadGuest
                {
                    public string salutation { get; set; }
                    public string first_name { get; set; }
                    public string last_name { get; set; }
                    public string nationality { get; set; }
                    public string remarks { get; set; }
                }

                public class Root
                {
                    public string session_id { get; set; }
                    public string event_id { get; set; }
                    public string booking_id { get; set; }
                    public string client_reference { get; set; }
                    public string status { get; set; }
                    public string status_payment { get; set; }
                    public DateTime requested_at { get; set; }
                    public DateTime confirmed_at { get; set; }
                    public Package package { get; set; }
                    public Guest guest { get; set; }
                    public Pricing pricing { get; set; }
                    public CancellationDetails cancellation_details { get; set; }
                    public CancellationPolicy cancellation_policy { get; set; }
                    public BookingDetails booking_details { get; set; }
                    public ContactPerson contact_person { get; set; }
                    public List<RoomLeadGuest> room_lead_guests { get; set; }
                    public bool bundled_booking { get; set; }
                    public string locale { get; set; }
                    public string source_market { get; set; }
                    public bool is_custom { get; set; }
                    public int penalty_percentage { get; set; }
                }


            }
            #endregion
        }
    }
}
