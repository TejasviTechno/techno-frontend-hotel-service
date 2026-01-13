using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XConnect.Supplier.GRNC
{
    public class GRNCClass
    {
    }

    public class Roomgrnc
    {
        public string room_type { get; set; }
        public string room_reference { get; set; }
        public int no_of_rooms { get; set; }
        public int no_of_children { get; set; }
        public int no_of_adults { get; set; }
        public string description { get; set; }
        public IList<int> children_ages { get; set; }
    }

    public class RateCommentsgrnc
    {
        public string pax_comments { get; set; }
        public string comments { get; set; }
    }

    public class Netgrnc
    {
        public string name { get; set; }
        public bool included { get; set; }
        public string currency { get; set; }
        public string amount_type { get; set; }
        public double amount { get; set; }
    }

    public class PriceDetailsgrnc
    {
        public List<Netgrnc> net { get; set; }
    }

    public class NoShowFeegrnc
    {
        public int percent { get; set; }
        public string currency { get; set; }
        public string amount_type { get; set; }
    }

    public class Detailgrnc
    {
        public double percent { get; set; }
        public DateTime from { get; set; }
        public string currency { get; set; }
    }

    public class CancellationPolicygrnc
    {
        public bool under_cancellation { get; set; }
        public NoShowFeegrnc no_show_fee { get; set; }
        public List<Detailgrnc> details { get; set; }
        public DateTime cancel_by_date { get; set; }
        public string amount_type { get; set; }
    }

    public class Rategrnc
    {
        public bool supports_cancellation { get; set; }
        public bool supports_amendment { get; set; }
        public List<Roomgrnc> rooms { get; set; }
        public string room_code { get; set; }
        public string rate_type { get; set; }
        public string rate_key { get; set; }
        public RateCommentsgrnc rate_comments { get; set; }
        public PriceDetailsgrnc price_details { get; set; }
        public double price { get; set; }
        public List<string> payment_type { get; set; }
        public bool non_refundable { get; set; }
        public int no_of_rooms { get; set; }
        public bool includes_boarding { get; set; }
        public bool has_promotions { get; set; }
        public string group_code { get; set; }
        public string currency { get; set; }
        public string credit_deduction { get; set; }
        public CancellationPolicygrnc cancellation_policy { get; set; }
        public List<string> boarding_details { get; set; }
        public string availability_status { get; set; }
        public string cancellation_policy_code { get; set; }
        public List<string> other_inclusions { get; set; }
    }

    public class Imagesgrnc
    {
        public string url { get; set; }
        public string main_image { get; set; }
    }

    public class Geolocationgrnc
    {
        public double longitude { get; set; }
        public double latitude { get; set; }
    }
    public class Safe2stay
    {
        public string covid_19_safety_protocol { get; set; }
        public string covid_19_safe_to_stay { get; set; }
    }
    public class Hotelgrnc
    {
        public Safe2stay safe2stay { get; set; }
        public List<Rategrnc> rates { get; set; }
        public string name { get; set; }
        public Imagesgrnc images { get; set; }
        public string hotel_code { get; set; }
        public Geolocationgrnc geolocation { get; set; }
        public string facilities { get; set; }
        public string description { get; set; }
        public string country { get; set; }
        public string city_code { get; set; }
        public double category { get; set; }
        public string address { get; set; }
        public string acc_type { get; set; }

    }

    public class RootObjectgrnc
    {
        public string search_id { get; set; }
        public int no_of_rooms { get; set; }
        public int no_of_nights { get; set; }
        public int no_of_adults { get; set; }
        public int no_of_children { get; set; }
        public Hotelgrnc hotel { get; set; }
        public string checkout { get; set; }
        public string checkin { get; set; }
    }

    #region recheck
    public class recheck
    {
        public recheck()
        {
            //
            // TODO: Add constructor logic here
            //
        }
        public class BedType
        {
            public string type { get; set; }
            public int id { get; set; }
        }

        public class Room
        {
            public string room_type { get; set; }
            public string room_reference { get; set; }
            public int no_of_rooms { get; set; }
            public int no_of_children { get; set; }
            public int no_of_adults { get; set; }
            public string description { get; set; }
            public IList<BedType> bed_types { get; set; }
        }

        public class RateComments
        {
            public string spl_checkin_instructions { get; set; }
            public string checkin_instructions { get; set; }
            public string comments { get; set; }

        }

        public class SurchargeOrTax
        {
            public string name { get; set; }
            public bool included { get; set; }
            public string currency { get; set; }
            public string amount_type { get; set; }
            public double amount { get; set; }
        }

        public class HotelCharge
        {
            public string name { get; set; }
            public bool mandatory { get; set; }
            public bool included { get; set; }
            public string currency { get; set; }
            public string amount_type { get; set; }
            public string amount { get; set; }
        }

        public class Net
        {
            public string name { get; set; }
            public bool included { get; set; }
            public string currency { get; set; }
            public string amount_type { get; set; }
            public double amount { get; set; }
        }

        public class GST
        {
            public string name { get; set; }
            public bool included { get; set; }
            public string currency { get; set; }
            public string amount_type { get; set; }
            public double amount { get; set; }
        }

        public class PriceDetails
        {
            public IList<SurchargeOrTax> surcharge_or_tax { get; set; }
            public IList<HotelCharge> hotel_charges { get; set; }
            public IList<Net> net { get; set; }
            public IList<GST> GST { get; set; }
        }
        public class Detail
        {
            public float percent { get; set; }
            public float flat_fee { get; set; }
            public DateTime from { get; set; }
            public string currency { get; set; }
            public int nights { get; set; }
            public bool allowed_cancellation { get; set; }
        }
        public class RoomCancellationPolicy
        {
            public bool under_cancellation { get; set; }
            public IList<Detail> details { get; set; }
            public DateTime cancel_by_date { get; set; }
            public string amount_type { get; set; }
        }
        public class Rate
        {
            public bool supports_cancellation { get; set; }
            public bool supports_amendment { get; set; }
            public IList<Room> rooms { get; set; }
            public string room_code { get; set; }
            public string rate_type { get; set; }
            public string rate_key { get; set; }
            public RateComments rate_comments { get; set; }
            public PriceDetails price_details { get; set; }
            public double price { get; set; }
            public IList<string> payment_type { get; set; }
            public IList<string> other_inclusions { get; set; }
            public bool non_refundable { get; set; }
            public int no_of_rooms { get; set; }
            public bool mod_rates { get; set; }
            public bool includes_boarding { get; set; }
            public bool has_promotions { get; set; }
            public string group_code { get; set; }
            public string currency { get; set; }
            public string credit_deduction { get; set; }
            public string cancellation_policy_code { get; set; }
            public RoomCancellationPolicy cancellation_policy { get; set; }
            public IList<string> boarding_details { get; set; }
        }

        public class Images
        {
            public string url { get; set; }
            public string main_image { get; set; }
        }

        public class Geolocation
        {
            public double longitude { get; set; }
            public double latitude { get; set; }
        }

        public class Hotel
        {
            public Rate rate { get; set; }
            public string name { get; set; }
            public Images images { get; set; }
            public string hotel_code { get; set; }
            public Geolocation geolocation { get; set; }
            public string facilities { get; set; }
            public string description { get; set; }
            public string country { get; set; }
            public string city_code { get; set; }
            public double category { get; set; }
            public string address { get; set; }
        }

        public class Example
        {
            public string search_id { get; set; }
            public Hotel hotel { get; set; }
        }


    }
    #endregion

    #region GRNCSearch
    class GRNCsearch
    {
        public GRNCsearch()
        {
            //
            // TODO: Add constructor logic here
            //
        }
        #region Room


        public class Room
        {
            public int RoomId { get; set; }
            public string Description { get; set; }
        }

        public class Option
        {
            public string TID { get; set; }
            public double TotalPrice { get; set; }
            public string Currency { get; set; }
            public string MealType { get; set; }
            public string MealName { get; set; }
            public bool OnRequest { get; set; }
            public List<Room> Rooms { get; set; }
        }

        public class Datum
        {
            public string HotelCode { get; set; }
            public string HotelName { get; set; }
            public int DestinationId { get; set; }
            public string DestinationName { get; set; }
            public List<Option> Options { get; set; }
        }

        public class RootObject
        {
            public string VersionDescription { get; set; }
            public bool Success { get; set; }
            public List<string> Messages { get; set; }
            public List<Datum> Data { get; set; }
        }

        #endregion

        #region roomresponse

        public class BedType
        {
            public string type { get; set; }
            public int id { get; set; }
        }

        public class Rooms
        {
            public string room_type { get; set; }
            public string room_reference { get; set; }
            public int no_of_rooms { get; set; }
            public int no_of_children { get; set; }
            public int no_of_adults { get; set; }
            public string description { get; set; }
            public IList<BedType> bed_types { get; set; }
            public IList<int> children_ages { get; set; }
        }

        public class RateComments
        {
            public string pax_comments { get; set; }
        }

        public class SurchargeOrTax
        {
            public string name { get; set; }
            public bool included { get; set; }
            public string currency { get; set; }
            public string amount_type { get; set; }
            public double amount { get; set; }
        }

        public class HotelCharge
        {
            public string name { get; set; }
            public bool mandatory { get; set; }
            public bool included { get; set; }
            public string currency { get; set; }
            public string amount_type { get; set; }
            public double amount { get; set; }
        }

        public class Net
        {
            public string name { get; set; }
            public bool included { get; set; }
            public string currency { get; set; }
            public string amount_type { get; set; }
            public double amount { get; set; }
        }

        public class GST
        {
            public string name { get; set; }
            public bool included { get; set; }
            public string currency { get; set; }
            public string amount_type { get; set; }
            public double amount { get; set; }
        }

        public class PriceDetails
        {
            public IList<SurchargeOrTax> surcharge_or_tax { get; set; }
            public IList<HotelCharge> hotel_charges { get; set; }
            public IList<Net> net { get; set; }
            public IList<GST> GST { get; set; }
        }

        public class Detail
        {
            public int percent { get; set; }
            public DateTime from { get; set; }
            public string currency { get; set; }
        }

        public class RoomCancellationPolicy
        {
            public bool under_cancellation { get; set; }
            public IList<Detail> details { get; set; }
            public DateTime cancel_by_date { get; set; }
            public string amount_type { get; set; }
        }

        public class Rate
        {
            public bool supports_cancellation { get; set; }
            public bool supports_amendment { get; set; }
            public IList<Rooms> rooms { get; set; }
            public string room_code { get; set; }
            public string rate_type { get; set; }
            public string rate_key { get; set; }
            public RateComments rate_comments { get; set; }
            public PriceDetails price_details { get; set; }
            public int price { get; set; }
            public IList<string> payment_type { get; set; }
            public bool non_refundable { get; set; }
            public int no_of_rooms { get; set; }
            public bool mod_rates { get; set; }
            public bool includes_boarding { get; set; }
            public string hash { get; set; }
            public bool has_promotions { get; set; }
            public string group_code { get; set; }
            public string cancellation_policy_code { get; set; }
            public string currency { get; set; }
            public string credit_deduction { get; set; }
            public RoomCancellationPolicy cancellation_policy { get; set; }
            public IList<string> boarding_details { get; set; }
        }

        public class Images
        {
            public string url { get; set; }
            public string main_image { get; set; }
        }

        public class Geolocation
        {
            public double longitude { get; set; }
            public double latitude { get; set; }
        }

        public class Hotel
        {
            public IList<Rate> rates { get; set; }
            public string name { get; set; }
            public Images images { get; set; }
            public string hotel_code { get; set; }
            public Geolocation geolocation { get; set; }
            public string facilities { get; set; }
            public string description { get; set; }
            public string country { get; set; }
            public string city_code { get; set; }
            public double category { get; set; }
            public string address { get; set; }
        }

        public class RoomlistRS
        {
            public string search_id { get; set; }
            public int no_of_rooms { get; set; }
            public int no_of_nights { get; set; }
            public int no_of_children { get; set; }
            public int no_of_adults { get; set; }
            public Hotel hotel { get; set; }
            public string checkout { get; set; }
            public string checkin { get; set; }
        }
        #endregion

        //#region roomresponse

        //public class BedType
        //{
        //    public string type { get; set; }
        //    public int id { get; set; }
        //}

        //public class Rooms
        //{
        //    public string room_type { get; set; }
        //    public string room_reference { get; set; }
        //    public int no_of_rooms { get; set; }
        //    public int no_of_children { get; set; }
        //    public int no_of_adults { get; set; }
        //    public string description { get; set; }
        //    public IList<BedType> bed_types { get; set; }
        //    public IList<int> children_ages { get; set; }
        //}

        //public class RateComments
        //{
        //    public string pax_comments { get; set; }
        //    public string comments { get; set; }
        //}

        //public class SurchargeOrTax
        //{
        //    public string name { get; set; }
        //    public bool included { get; set; }
        //    public string currency { get; set; }
        //    public string amount_type { get; set; }
        //    public double amount { get; set; }
        //}

        //public class HotelCharge
        //{
        //    public string name { get; set; }
        //    public bool mandatory { get; set; }
        //    public bool included { get; set; }
        //    public string currency { get; set; }
        //    public string amount_type { get; set; }
        //    public double amount { get; set; }
        //}

        //public class Net
        //{
        //    public string name { get; set; }
        //    public bool included { get; set; }
        //    public string currency { get; set; }
        //    public string amount_type { get; set; }
        //    public double amount { get; set; }
        //}

        //public class GST
        //{
        //    public string name { get; set; }
        //    public bool included { get; set; }
        //    public string currency { get; set; }
        //    public string amount_type { get; set; }
        //    public double amount { get; set; }
        //}

        //public class PriceDetails
        //{
        //    public IList<SurchargeOrTax> surcharge_or_tax { get; set; }
        //    public IList<HotelCharge> hotel_charges { get; set; }
        //    public IList<Net> net { get; set; }
        //    public IList<GST> GST { get; set; }
        //}

        //public class Detail
        //{
        //    public int percent { get; set; }
        //    public DateTime from { get; set; }
        //    public string currency { get; set; }
        //}
        //public class NoShowFee
        //{
        //    public int percent { get; set; }
        //    public string currency { get; set; }
        //    public string amount_type { get; set; }
        //}
        //public class RoomCancellationPolicy
        //{
        //    public bool under_cancellation { get; set; }
        //    public NoShowFee no_show_fee { get; set; }
        //    public IList<Detail> details { get; set; }
        //    public DateTime cancel_by_date { get; set; }
        //    public string amount_type { get; set; }
        //}

        //public class Rate
        //{
        //    public bool supports_cancellation { get; set; }
        //    public bool supports_amendment { get; set; }
        //    public IList<Rooms> rooms { get; set; }
        //    public string room_code { get; set; }
        //    public string rate_type { get; set; }
        //    public string rate_key { get; set; }
        //    public RateComments rate_comments { get; set; }
        //    public PriceDetails price_details { get; set; }
        //    public int price { get; set; }
        //    public IList<string> payment_type { get; set; }
        //    public bool non_refundable { get; set; }
        //    public int no_of_rooms { get; set; }
        //    public bool mod_rates { get; set; }
        //    public bool includes_boarding { get; set; }
        //    public string hash { get; set; }
        //    public bool has_promotions { get; set; }
        //    public string group_code { get; set; }
        //    public string cancellation_policy_code { get; set; }
        //    public string currency { get; set; }
        //    public string credit_deduction { get; set; }
        //    public string availability_status { get; set; }
        //    public RoomCancellationPolicy cancellation_policy { get; set; }
        //    public IList<string> boarding_details { get; set; }
        //}

        //public class Images
        //{
        //    public string url { get; set; }
        //    public string main_image { get; set; }
        //}

        //public class Geolocation
        //{
        //    public double longitude { get; set; }
        //    public double latitude { get; set; }
        //}

        //public class Hotel
        //{
        //    public IList<Rate> rates { get; set; }
        //    public string name { get; set; }
        //    public Images images { get; set; }
        //    public string hotel_code { get; set; }
        //    public Geolocation geolocation { get; set; }
        //    public string facilities { get; set; }
        //    public string description { get; set; }
        //    public string country { get; set; }
        //    public string city_code { get; set; }
        //    public double category { get; set; }
        //    public string address { get; set; }
        //    public string acc_type { get; set; }
        //}

        //public class RoomlistRS
        //{
        //    public string search_id { get; set; }
        //    public int no_of_rooms { get; set; }
        //    public int no_of_nights { get; set; }
        //    public int no_of_children { get; set; }
        //    public int no_of_adults { get; set; }
        //    public Hotel hotel { get; set; }
        //    public string checkout { get; set; }
        //    public string checkin { get; set; }
        //}
        //#endregion

        #region cancellationpolicy
        public class Detailss
        {
            public double percent { get; set; }
            public DateTime from { get; set; }
            public string currency { get; set; }
        }

        public class RoomCancellationPolicyv1
        {
            public string search_id { get; set; }
            public bool under_cancellation { get; set; }
            public IList<Detailss> details { get; set; }
            public DateTime cancel_by_date { get; set; }
            public string amount_type { get; set; }
        }

        #endregion


        #region cacellation
        public class NoShowfee
        {
            public double flat_fee { get; set; }
            public string currency { get; set; }
            public string amount_type { get; set; }
        }

        public class Details
        {
            public DateTime from { get; set; }
            public double flat_fee { get; set; }
            public string currency { get; set; }
        }

        public class RoomsCancelltionPolicy1
        {
            public bool under_cancellation { get; set; }
            public NoShowfee no_show_fee { get; set; }
            public IList<Details> details { get; set; }
            public string cancellation_policy_code { get; set; }
            public string amount_type { get; set; }
        }

        public class bookingcancellation
        {
            public string comments { get; set; }
        }
        #endregion
    }

    #endregion
}
