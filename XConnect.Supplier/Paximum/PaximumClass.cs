using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XConnect.Supplier.Paximum
{
    public class PaximumClass
    {
    }

    #region paximum class 

    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
    public class Paximum
    {


        public class CancellationPolicy
        {
            public Fee fee { get; set; }
            public DateTime afterDate { get; set; }
        }

        public class City
        {
            public string id { get; set; }
            public string name { get; set; }
        }

        public class Country
        {
            public string id { get; set; }
            public string name { get; set; }
        }

        public class Fee
        {
            public double amount { get; set; }
            public string currency { get; set; }
        }

        public class Geolocation
        {
            public double longitude { get; set; }
            public double latitude { get; set; }
        }

        public class Hotel
        {
            public string id { get; set; }
            public string name { get; set; }
            public string description { get; set; }
            public City city { get; set; }
            public Country country { get; set; }
            public double stars { get; set; }
            public double rating { get; set; }
            public string reviewUrl { get; set; }
            public string thumbnail { get; set; }
            public string address { get; set; }
            public List<object> themes { get; set; }
            public List<string> facilities { get; set; }
            public Geolocation geolocation { get; set; }
            public bool isFavorite { get; set; }
            public List<Offer> offers { get; set; }
            public bool ttydMember { get; set; }
            public List<object> boardIdsOfHotel { get; set; }
            public MinimumPrice minimumPrice { get; set; }
            public MaximumPrice maximumPrice { get; set; }
        }

        public class MaximumPrice
        {
            public double amount { get; set; }
            public string currency { get; set; }
        }

        public class MinimumPrice
        {
            public double amount { get; set; }
            public string currency { get; set; }
        }

        public class MinimumSalePrice
        {
            public double amount { get; set; }
            public string currency { get; set; }
        }

        public class Offer
        {
            public string id { get; set; }
            public DateTime expiresOn { get; set; }
            public string hotelId { get; set; }
            public string boardId { get; set; }
            public string board { get; set; }
            public List<string> boardCategories { get; set; }
            public List<Room> rooms { get; set; }
            public Price price { get; set; }
            public MinimumSalePrice minimumSalePrice { get; set; }
            public bool isB2CPrice { get; set; }
            public bool isSpecial { get; set; }
            public bool isAvailable { get; set; }
            public List<CancellationPolicy> cancellationPolicies { get; set; }
        }

        public class Price
        {
            public double amount { get; set; }
            public string currency { get; set; }
        }

        public class Room
        {
            public string id { get; set; }
            public string typeId { get; set; }
            public string type { get; set; }
            public List<object> categories { get; set; }
            public List<object> promotions { get; set; }
            public List<Traveller> travellers { get; set; }
            public Price price { get; set; }
        }

        public class Root
        {
            public string searchId { get; set; }
            public DateTime expiresOn { get; set; }
            public List<Hotel> hotels { get; set; }
        }

        public class Traveller
        {
            public string type { get; set; }
            public int age { get; set; }
            public string nationality { get; set; }
        }


    }

    public class Paximum_prebook
    {
        public class CancellationPolicy
        {
            public Fee fee { get; set; }
            public DateTime afterDate { get; set; }
        }

        public class Fee
        {
            public double amount { get; set; }
            public string currency { get; set; }
        }

        public class Price
        {
            public double amount { get; set; }
            public string currency { get; set; }
        }

        public class PriceBreakdown
        {
            public string roomId { get; set; }
            public DateTime date { get; set; }
            public Price price { get; set; }
        }

        public class Room
        {
            public string id { get; set; }
            public string typeId { get; set; }
            public string type { get; set; }
            public string typeEn { get; set; }
            public List<object> categories { get; set; }
            public List<object> promotions { get; set; }
            public List<Traveller> travellers { get; set; }
            public Price price { get; set; }
        }

        public class Root
        {
            public DateTime expiresOn { get; set; }
            public string searchId { get; set; }
            public DateTime checkinDate { get; set; }
            public DateTime checkoutDate { get; set; }
            public string hotelId { get; set; }
            public string id { get; set; }
            public string boardId { get; set; }
            public List<string> boardCategories { get; set; }
            public string board { get; set; }
            public string boardEn { get; set; }
            public List<Room> rooms { get; set; }
            public Price price { get; set; }
            public bool isB2CPrice { get; set; }
            public List<PriceBreakdown> priceBreakdown { get; set; }
            public List<CancellationPolicy> cancellationPolicies { get; set; }
            public List<object> supplements { get; set; }
            public bool isSpecial { get; set; }
            public bool isAvailable { get; set; }
            public List<string> notes { get; set; }
            public List<object> restrictions { get; set; }
            public bool isOwnProvider { get; set; }
            public List<object> warnings { get; set; }
        }

        public class Traveller
        {
            public string type { get; set; }
            public int age { get; set; }
            public string nationality { get; set; }
        }

    }

    public class Paximum_Booking
    {
        // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
        public class Booking
        {
            public string id { get; set; }
            public string bookingNumber { get; set; }
            public string offerId { get; set; }
            public string documentUrl { get; set; }
            public Price price { get; set; }
            public List<CancellationPolicy> cancellationPolicies { get; set; }
            public string status { get; set; }
            public string paymentStatus { get; set; }
            public string type { get; set; }
            public DateTime checkIn { get; set; }
            public DateTime checkOut { get; set; }
            public DateTime cancellationDeadline { get; set; }
        }

        public class CancellationPolicy
        {
            public DateTime permittedDate { get; set; }
            public Fee fee { get; set; }
        }

        public class Fee
        {
            public double amount { get; set; }
            public string currency { get; set; }
        }

        public class Price
        {
            public double amount { get; set; }
            public string currency { get; set; }
        }

        public class Root
        {
            public string orderId { get; set; }
            public string orderNumber { get; set; }
            public List<Booking> bookings { get; set; }
        }
    }

    public class Paximum_CancelBooking
    {
        public class Root
        {
            public string bookingId { get; set; }
        }

    }
    public class Paximum_CancelResponse
    {
        public class Cancellation
        {
            public string bookingId { get; set; }
            public string status { get; set; }
        }

        public class Root
        {
            public string bookingId { get; set; }
            public Cancellation cancellation { get; set; }
        }
    }

    #endregion
}
