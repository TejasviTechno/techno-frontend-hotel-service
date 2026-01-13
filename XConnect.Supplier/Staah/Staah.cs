using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XConnect.Supplier.Staah
{
    public class Staah
    {
        public class StaahBookingRequest
        {
            public class Addon
            {
                public string Name { get; set; }
                public string Price { get; set; }
                public string Type { get; set; }
                public string Nights { get; set; }
                public string Persons { get; set; }
                public string Priceperunit { get; set; }
                public string PriceMode { get; set; }
            }

            public class Customer
            {
                public string address { get; set; }
                public string city { get; set; }
                public string country { get; set; }
                public string email { get; set; }
                public string salutation { get; set; }
                public string first_name { get; set; }
                public string last_name { get; set; }
                public string remarks { get; set; }
                public string telephone { get; set; }
                public string zip { get; set; }
            }

            public class ExtraGuests
            {
                public string extraAdult { get; set; }
                public string extraChild { get; set; }
                public string extraAdultRate { get; set; }
                public string extraChildRate { get; set; }
            }

            public class GuestCount
            {
                public string AgeQualifyingCode { get; set; }
                public string Count { get; set; }
            }

            public class Paymentcarddetail
            {
                public string CardHolderName { get; set; }
                public string CardType { get; set; }
                public string ExpireDate { get; set; }
                public string CardNumber { get; set; }
                public string cvv { get; set; }
            }

            public class Price
            {
                public string date { get; set; }
                public string rate_id { get; set; }
                public string rate_name { get; set; }
                public string amountaftertax { get; set; }
                public ExtraGuests extraGuests { get; set; }
            }

            public class Reservation
            {
                public DateTime reservation_datetime { get; set; }
                public string propertyname
                {
                    get; set;
                } //#@ Staah added new
                public string reservation_id { get; set; }
                public string payment_required { get; set; }
                public string payment_type { get; set; }
                public string commissionamount { get; set; }
                public string discountamount
                {
                    get; set;
                }//#@ Staah added new
                public string deposit { get; set; }
                public string totalamountaftertax { get; set; }
                public string totaltax { get; set; }
                public string currencycode { get; set; }
                public string status { get; set; }
                public Customer customer { get; set; }
                public Paymentcarddetail paymentcarddetail { get; set; }
                public List<Room> room { get; set; }
                public string POS { get; set; }
            }

            public class Reservations
            {
                public List<Reservation> reservation { get; set; }
            }

            public class Room
            {
                public string arrival_date { get; set; }
                public string departure_date { get; set; }
                public string room_id { get; set; }
                public string room_name { get; set; }
                public List<Price> price { get; set; }
                public string salutation { get; set; }
                public string first_name { get; set; }
                public string last_name { get; set; }
                public List<Taxis> taxes { get; set; }
                public List<Addon> Addons { get; set; }
                public string amountaftertax { get; set; }
                public string remarks { get; set; }
                public List<GuestCount> GuestCount { get; set; }
            }

            public class Root
            {
                public string propertyid { get; set; }
                public string apikey { get; set; }
                public string action { get; set; }
                public string version
                {
                    get; set;
                } //@# staah new added
                public Reservations reservations { get; set; }
            }

            public class Taxis
            {
                public string name { get; set; }
                public string value { get; set; }
            }
        }

        public class StaahBookingResponse
        {
            public string bookingId { get; set; }
            public string status { get; set; }
            public string error_desc { get; set; }
            public string tracking_id { get; set; }
        }
    }
}
