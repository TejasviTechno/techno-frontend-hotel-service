using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XConnect.Supplier.Welcomebeds
{
    public class WelcomebedsClass
    {
        public class Welcomebeds
        {
            public class welcombedsavailibilityRequest
            {

                public class Destination
                {
                    
                    public List<string> hotelCodes { get; set; }
                    public Location location { get; set; }
                }

                public class HotelAvailability
                {
                    public SearchAvail searchAvail { get; set; }
                    public int timeout { get; set; }
                }

                public class Location
                {
                    public string destinationCode { get; set; }
                    public string type { get; set; }
                }

                public class Room
                {
                    public int index { get; set; }
                    public List<int> passengerAges { get; set; }
                }

                public class Root
                {
                    public HotelAvailability hotelAvailability { get; set; }
                    public string token { get; set; }
                }

                public class SearchAvail
                {
                    public string checkIn { get; set; }
                    public string checkOut { get; set; }
                    public Destination destination { get; set; }
                    public List<Room> rooms { get; set; }
                }
            }

            public class welcomebedsavailabilityresponse
            {
                public class Address
                {
                    public string name { get; set; }
                    public string countryCode { get; set; }
                    public string postalCode { get; set; }
                    public string provinceCode { get; set; }
                    public Loc loc { get; set; }
                }

                public class CancellationPolicy
                {
                    public string from { get; set; }
                    public string to { get; set; }
                    public Pricing pricing { get; set; }
                }

                public class Cat
                {
                    public string id { get; set; }
                    public string name { get; set; }
                    public double rate { get; set; }
                    public string rateKey { get; set; }
                }

                public class ChainInformation
                {
                    public string id { get; set; }
                }

                public class ComAgency
                {
                    public double price { get; set; }
                    public string currency { get; set; }
                }

                public class Commission
                {
                    public ComAgency comAgency { get; set; }
                    public double comTaxPercent { get; set; }
                    public double comTaxAmount { get; set; }
                }

                public class Contact
                {
                    public string email { get; set; }
                    public string phoneNumber { get; set; }
                }

                public class Destination
                {
                    public List<string> hotelCodes { get; set; }
                    public Location location { get; set; }
                }

                public class FilterAvail
                {
                }

                public class Hotel
                {
                    public string hotelCode { get; set; }
                    public string name { get; set; }
                    public List<RoomRate> roomRates { get; set; }
                    public string checkIn { get; set; }
                    public string checkOut { get; set; }
                    public HotelDetails hotelDetails { get; set; }
                    public Info info { get; set; }
                    public Information information { get; set; }
                }

                public class HotelAvailability
                {
                    public SearchAvail searchAvail { get; set; }
                    public int timeout { get; set; }
                    public FilterAvail filterAvail { get; set; }
                }

                public class HotelDetails
                {
                }

                public class Info
                {
                    public Cat cat { get; set; }
                    public string chain { get; set; }
                    public Loc loc { get; set; }
                }

                public class Information
                {
                    public Cat cat { get; set; }
                    public string chain { get; set; }
                    public Address address { get; set; }
                    public Contact contact { get; set; }
                    public ChainInformation chainInformation { get; set; }
                }

                public class Loc
                {
                    public string code { get; set; }
                }

                public class Location
                {
                    public string type { get; set; }
                }

                public class Meal
                {
                    public string id { get; set; }
                    public string name { get; set; }
                }

                public class Net
                {
                    public double price { get; set; }
                    public string currency { get; set; }
                }

                public class Observation
                {
                    public string type { get; set; }
                    public string txt { get; set; }
                    public bool print { get; set; }
                }

                public class Pricing
                {
                    public Commission commission { get; set; }
                    public Sell sell { get; set; }
                    public Net net { get; set; }
                    public string currency { get; set; }
                }

                public class Request
                {
                    public HotelAvailability hotelAvailability { get; set; }
                    public string token { get; set; }
                }

                public class Room
                {
                    public List<int> passengerAges { get; set; }
                    public int index { get; set; }
                    public string id { get; set; }
                    public string name { get; set; }
                    public Pricing pricing { get; set; }
                    public string configuration { get; set; }
                }

                public class RoomRate
                {
                    public string rateID { get; set; }
                    public string rateName { get; set; }
                    public List<Room> rooms { get; set; }
                    public int roomQty { get; set; }
                    public Pricing pricing { get; set; }
                    public Meal meal { get; set; }
                    public List<CancellationPolicy> cancellationPolicies { get; set; }
                    public string bookToken { get; set; }
                    public string status { get; set; }
                    public List<Observation> observations { get; set; }
                }

                public class Root
                {
                    public Request request { get; set; }
                    public List<Hotel> hotels { get; set; }
                    public string token { get; set; }
                }

                public class SearchAvail
                {
                    public string checkIn { get; set; }
                    public string checkOut { get; set; }
                    public List<Room> rooms { get; set; }
                    public Destination destination { get; set; }
                    public string market { get; set; }
                }

                public class Sell
                {
                    public double price { get; set; }
                    public string currency { get; set; }
                }


            }


            public class welcombedsprebook
            {
                public class Root
                {
                    public string token { get; set; }
                    public string bookToken { get; set; }
                }

            }

            public class welcomebedsprebookresponse
            {
                public class Address
                {
                    public string name { get; set; }
                    public string countryCode { get; set; }
                    public string postalCode { get; set; }
                    public string provinceCode { get; set; }
                    public Loc loc { get; set; }
                    public string country { get; set; }
                    public string province { get; set; }
                }

                public class Annotations
                {
                    public List<Observation> observations { get; set; }
                    public List<object> remarks { get; set; }
                }

                public class CancellationPolicy
                {
                    public string from { get; set; }
                    public string to { get; set; }
                    public Pricing pricing { get; set; }
                    public string desc { get; set; }
                }

                public class Cat
                {
                    public string id { get; set; }
                    public string name { get; set; }
                    public int rate { get; set; }
                    public string rateKey { get; set; }
                }

                public class ChainInformation
                {
                    public string id { get; set; }
                    public string name { get; set; }
                    public string groupId { get; set; }
                    public string groupName { get; set; }
                }

                public class ComAgency
                {
                    public double price { get; set; }
                    public string currency { get; set; }
                }

                public class Commission
                {
                    public ComAgency comAgency { get; set; }
                    public int comTaxPercent { get; set; }
                    public int comTaxAmount { get; set; }
                }

                public class Contact
                {
                    public string email { get; set; }
                    public string phoneNumber { get; set; }
                }

                public class Hotel
                {
                    public string hotelCode { get; set; }
                    public string name { get; set; }
                    public List<RoomRate> roomRates { get; set; }
                    public string checkIn { get; set; }
                    public string checkOut { get; set; }
                    public HotelDetails hotelDetails { get; set; }
                    public Info info { get; set; }
                    public Information information { get; set; }
                }

                public class HotelDetails
                {
                    public TripAdvisor tripAdvisor { get; set; }
                    public List<Service> services { get; set; }
                    public List<Image> images { get; set; }
                    public Annotations annotations { get; set; }
                }

                public class Image
                {
                    public string url { get; set; }
                    public int order { get; set; }
                    public string section { get; set; }
                    public string name { get; set; }
                }

                public class Info
                {
                    public Cat cat { get; set; }
                    public string chain { get; set; }
                    public Loc loc { get; set; }
                }

                public class Information
                {
                    public Cat cat { get; set; }
                    public string chain { get; set; }
                    public Address address { get; set; }
                    public Contact contact { get; set; }
                    public ChainInformation chainInformation { get; set; }
                }

                public class Loc
                {
                    public string code { get; set; }
                    public string name { get; set; }
                    public string zCode { get; set; }
                    public string zName { get; set; }
                    public string address { get; set; }
                    public string lat { get; set; }
                    public string @long { get; set; }
                }

                public class Meal
                {
                    public string id { get; set; }
                    public string name { get; set; }
                }

                public class Net
                {
                    public double price { get; set; }
                    public string currency { get; set; }
                }

                public class Observation
                {
                    public string type { get; set; }
                    public string txt { get; set; }
                }

                public class Pricing
                {
                    public Commission commission { get; set; }
                    public Sell sell { get; set; }
                    public Net net { get; set; }
                    public string currency { get; set; }
                }

                public class Request
                {
                    public object hotel { get; set; }
                    public string token { get; set; }
                    public string bookToken { get; set; }
                    public List<object> travellers { get; set; }
                    public string clientCode { get; set; }
                    public List<object> providerOptions { get; set; }
                }

                public class Room
                {
                    public string id { get; set; }
                    public string name { get; set; }
                    public int index { get; set; }
                    public Pricing pricing { get; set; }
                    public string configuration { get; set; }
                }

                public class RoomRate
                {
                    public string rateID { get; set; }
                    public string rateName { get; set; }
                    public List<Observation> observations { get; set; }
                    public List<Room> rooms { get; set; }
                    public int roomQty { get; set; }
                    public bool binding { get; set; }
                    public Pricing pricing { get; set; }
                    public Meal meal { get; set; }
                    public List<CancellationPolicy> cancellationPolicies { get; set; }
                    public string bookToken { get; set; }
                    public string status { get; set; }
                }

                public class Root
                {
                    public Request request { get; set; }
                    public Hotel hotel { get; set; }
                    public object notes { get; set; }
                    public string token { get; set; }
                    public string status { get; set; }
                    public int ttl { get; set; }
                }

                public class Sell
                {
                    public double price { get; set; }
                    public string currency { get; set; }
                }

                public class Service
                {
                    public string type { get; set; }
                    public string id { get; set; }
                    public string name { get; set; }
                }

                public class TripAdvisor
                {
                    public int code { get; set; }
                    public int quant { get; set; }
                    public double avg { get; set; }
                    public List<string> url { get; set; }
                }


            }
           
            public class welcombedsbookingreq
            {
                public class BookingReference
                {
                    public string requestReferenceID { get; set; }
                }

                public class Person
                {
                    public string lastName { get; set; }
                    public string name { get; set; }
                }

                public class Root
                {
                    public string bookToken { get; set; }
                    public BookingReference bookingReference { get; set; }
                    public string remarks { get; set; }
                    public List<Traveller> travellers { get; set; }
                }

                public class Traveller
                {
                    public int age { get; set; }
                    public int index { get; set; }
                    public Person person { get; set; }
                }
            }


           
            public class welcomebedsbookResponse
            {
                public class Address
                {
                    public string name { get; set; }
                    public string countryCode { get; set; }
                    public string country { get; set; }
                    public string postalCode { get; set; }
                    public string provinceCode { get; set; }
                    public string province { get; set; }
                    public Loc loc { get; set; }
                }

                public class Annotations
                {
                    public List<Observation> observations { get; set; }
                    public List<object> remarks { get; set; }
                }

                public class BookingReference
                {
                    public string requestReferenceID { get; set; }
                    public string bookingReferenceID { get; set; }
                    public string consumerReferenceID { get; set; }
                    public string agencyReferenceID { get; set; }
                    public string fileReferenceID { get; set; }
                    public string hotelReferenceID { get; set; }
                }

                public class CancellationPolicy
                {
                    public string from { get; set; }
                    public string to { get; set; }
                    public Pricing pricing { get; set; }
                    public string desc { get; set; }
                }

                public class Cat
                {
                    public string id { get; set; }
                    public string name { get; set; }
                    public int rate { get; set; }
                    public string rateKey { get; set; }
                }

                public class ChainInformation
                {
                    public string id { get; set; }
                    public string name { get; set; }
                    public string groupId { get; set; }
                    public string groupName { get; set; }
                }

                public class ComAgency
                {
                    public double price { get; set; }
                    public string currency { get; set; }
                }

                public class Commission
                {
                    public ComAgency comAgency { get; set; }
                    public double comTaxPercent { get; set; }
                    public double comTaxAmount { get; set; }
                }

                public class Contact
                {
                    public string email { get; set; }
                    public string phoneNumber { get; set; }
                }

                public class Hotel
                {
                    public string hotelCode { get; set; }
                    public string name { get; set; }
                    public List<RoomRate> roomRates { get; set; }
                    public string checkIn { get; set; }
                    public string checkOut { get; set; }
                    public HotelDetails hotelDetails { get; set; }
                    public Info info { get; set; }
                    public Information information { get; set; }
                }

                public class HotelDetails
                {
                    public object tripAdvisor { get; set; }
                    public List<Service> services { get; set; }
                    public List<Image> images { get; set; }
                    public Annotations annotations { get; set; }
                }

                public class Image
                {
                    public string url { get; set; }
                    public int order { get; set; }
                    public string section { get; set; }
                    public string name { get; set; }
                }

                public class Info
                {
                    public Cat cat { get; set; }
                    public string chain { get; set; }
                    public Loc loc { get; set; }
                }

                public class Information
                {
                    public Cat cat { get; set; }
                    public string chain { get; set; }
                    public Address address { get; set; }
                    public Contact contact { get; set; }
                    public ChainInformation chainInformation { get; set; }
                }

                public class Loc
                {
                    public string code { get; set; }
                    public string name { get; set; }
                    public string zCode { get; set; }
                    public string zName { get; set; }
                    public string address { get; set; }
                    public string lat { get; set; }
                    public string @long { get; set; }
                }

                public class Meal
                {
                    public string id { get; set; }
                    public string name { get; set; }
                }

                public class Net
                {
                    public double price { get; set; }
                    public string currency { get; set; }
                }

                public class Observation
                {
                    public string type { get; set; }
                    public string txt { get; set; }
                    public bool print { get; set; }
                }

                public class Person
                {
                    public string name { get; set; }
                    public string lastName { get; set; }
                }

                public class Pricing
                {
                    public Commission commission { get; set; }
                    public Sell sell { get; set; }
                    public Net net { get; set; }
                    public string currency { get; set; }
                }

                public class Request
                {
                    public object hotel { get; set; }
                    public string token { get; set; }
                    public string bookToken { get; set; }
                    public object bookingReference { get; set; }
                    public List<Traveller> travellers { get; set; }
                    public object remarks { get; set; }
                    public object ovRequestor { get; set; }
                    public string clientCode { get; set; }
                }

                public class Room
                {
                    public string id { get; set; }
                    public string name { get; set; }
                    public int index { get; set; }
                    public Pricing pricing { get; set; }
                    public string configuration { get; set; }
                }

                public class RoomRate
                {
                    public string rateID { get; set; }
                    public string rateName { get; set; }
                    public List<Observation> observations { get; set; }
                    public List<Room> rooms { get; set; }
                    public int roomQty { get; set; }
                    public bool binding { get; set; }
                    public Pricing pricing { get; set; }
                    public Meal meal { get; set; }
                    public List<CancellationPolicy> cancellationPolicies { get; set; }
                    public string bookToken { get; set; }
                    public string status { get; set; }
                }

                public class Root
                {
                    public Request request { get; set; }
                    public Hotel hotel { get; set; }
                    public object notes { get; set; }
                    public string token { get; set; }
                    public string status { get; set; }
                    public BookingReference bookingReference { get; set; }
                    public Voucher voucher { get; set; }
                }

                public class Sell
                {
                    public double price { get; set; }
                    public string currency { get; set; }
                }

                public class Service
                {
                    public string type { get; set; }
                    public string id { get; set; }
                    public string name { get; set; }
                }

                public class Traveller
                {
                    public int index { get; set; }
                    public int age { get; set; }
                    public Person person { get; set; }
                }

                public class Voucher
                {
                    public string phone { get; set; }
                    public string payable { get; set; }
                    public string issuingBrand { get; set; }
                }


            }


            public class welcomebedscancelrequest
            {
                public class BookingReference
                {
                    public string requestReferenceID { get; set; }
                    public string bookingReferenceID { get; set; }
                }

                public class Root
                {
                    public BookingReference bookingReference { get; set; }
                    public string token { get; set; }
                }


            }


            public class welcomebedscancelresponse
            {
                // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
                public class Address
                {
                    public string name { get; set; }
                    public string countryCode { get; set; }
                    public string country { get; set; }
                    public string postalCode { get; set; }
                    public string provinceCode { get; set; }
                    public string province { get; set; }
                    public Loc loc { get; set; }
                }

                public class BookingReference
                {
                    public string requestReferenceID { get; set; }
                    public string bookingReferenceID { get; set; }
                    public string consumerReferenceID { get; set; }
                    public string agencyReferenceID { get; set; }
                    public string fileReferenceID { get; set; }
                    public string hotelReferenceID { get; set; }
                }

                public class CancellationPolicy
                {
                    public string from { get; set; }
                    public string to { get; set; }
                    public Pricing pricing { get; set; }
                    public string desc { get; set; }
                }

                public class Cat
                {
                    public string id { get; set; }
                    public string name { get; set; }
                    public int rate { get; set; }
                    public string rateKey { get; set; }
                }

                public class ChainInformation
                {
                    public string id { get; set; }
                    public string name { get; set; }
                    public string groupId { get; set; }
                    public string groupName { get; set; }
                }

                public class ComAgency
                {
                    public double price { get; set; }
                    public string currency { get; set; }
                }

                public class Commission
                {
                    public ComAgency comAgency { get; set; }
                    public double comTaxPercent { get; set; }
                    public double comTaxAmount { get; set; }
                }

                public class Contact
                {
                    public string email { get; set; }
                    public string phoneNumber { get; set; }
                }

                public class Hotel
                {
                    public string hotelCode { get; set; }
                    public string name { get; set; }
                    public List<RoomRate> roomRates { get; set; }
                    public string checkIn { get; set; }
                    public string checkOut { get; set; }
                    public HotelDetails hotelDetails { get; set; }
                    public Info info { get; set; }
                    public Information information { get; set; }
                }

                public class HotelDetails
                {
                    public object tripAdvisor { get; set; }
                    public List<object> services { get; set; }
                    public List<object> images { get; set; }
                    public object annotations { get; set; }
                }

                public class Info
                {
                    public Cat cat { get; set; }
                    public string chain { get; set; }
                    public Loc loc { get; set; }
                }

                public class Information
                {
                    public Cat cat { get; set; }
                    public string chain { get; set; }
                    public Address address { get; set; }
                    public Contact contact { get; set; }
                    public ChainInformation chainInformation { get; set; }
                }

                public class Loc
                {
                    public string code { get; set; }
                    public string name { get; set; }
                    public string zCode { get; set; }
                    public string zName { get; set; }
                    public string address { get; set; }
                    public string lat { get; set; }
                    public string @long { get; set; }
                }

                public class Meal
                {
                    public string id { get; set; }
                    public string name { get; set; }
                }

                public class Net
                {
                    public double price { get; set; }
                    public string currency { get; set; }
                }

                public class Observation
                {
                    public string type { get; set; }
                    public string txt { get; set; }
                    public bool print { get; set; }
                }

                public class Pricing
                {
                    public Commission commission { get; set; }
                    public Sell sell { get; set; }
                    public Net net { get; set; }
                    public string currency { get; set; }
                }

                public class Request
                {
                    public BookingReference bookingReference { get; set; }
                    public string token { get; set; }
                    public object hotel { get; set; }
                    public List<object> travellers { get; set; }
                    public string clientCode { get; set; }
                }

                public class Room
                {
                    public string id { get; set; }
                    public string name { get; set; }
                    public int index { get; set; }
                    public Pricing pricing { get; set; }
                    public string configuration { get; set; }
                }

                public class RoomRate
                {
                    public string rateID { get; set; }
                    public string rateName { get; set; }
                    public List<Observation> observations { get; set; }
                    public List<Room> rooms { get; set; }
                    public int roomQty { get; set; }
                    public bool binding { get; set; }
                    public Pricing pricing { get; set; }
                    public Meal meal { get; set; }
                    public List<CancellationPolicy> cancellationPolicies { get; set; }
                    public string bookToken { get; set; }
                    public string status { get; set; }
                }

                public class Root
                {
                    public Request request { get; set; }
                    public Hotel hotel { get; set; }
                    public object notes { get; set; }
                    public string token { get; set; }
                    public string status { get; set; }
                }

                public class Sell
                {
                    public double price { get; set; }
                    public string currency { get; set; }
                }


            }
          
        }
    }
}
